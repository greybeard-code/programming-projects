"""Fill missing L1 Parquet days from Databento when no .nrd exists.

The NRD->Parquet converter can only produce days we actually recorded. A
handful of 2025 days were never captured (failed replay downloads). This
fetches those days from Databento's GLBX.MDP3 dataset and writes them in the
repo's existing L1 Parquet layout, so data.py picks them up with no changes.

Schema is TBBO ("BBO on trade"): every trade plus the top-of-book immediately
before it. That is exactly the information data._reduce_raw keeps -- it
discards standalone quote updates and retains only the quote prevailing at
each trade -- so TBBO is lossless for this engine and far cheaper than mbp-1.

Uses the plain HTTP API via stdlib urllib rather than the `databento` package,
which depends on pandas (CLAUDE.md keeps pandas out of this venv).

    set DATABENTO_API_KEY=db-...
    python tools/databento_fill.py --cost            # price it, download nothing
    python tools/databento_fill.py --fetch           # download and write
    python tools/databento_fill.py --cost --include-cl
"""
from __future__ import annotations

import argparse
import base64
import csv
import io
import json
import os
import sys
import urllib.parse
import urllib.request
from datetime import date, datetime, timedelta
from pathlib import Path
from zoneinfo import ZoneInfo

import numpy as np
import pyarrow as pa
import pyarrow.parquet as pq

API = "https://hist.databento.com/v0"
DATASET = "GLBX.MDP3"
SCHEMA = "tbbo"

NRD_ROOT = Path(r"M:\NinjaTrader_DataRepo\RawData\Continuous")
PARQUET_ROOT = Path(r"M:\NinjaTrader_DataRepo\RawData\Parquet")

EASTERN = ZoneInfo("America/New_York")

MDT_ASK, MDT_BID, MDT_LAST = 0, 1, 2

QUARTERLY = {"ES", "MES", "NQ", "MNQ", "YM", "MYM", "RTY", "M2K"}
METALS = {"GC", "MGC"}
MAINTAINED = QUARTERLY | METALS
MONTH_CODE = "FGHJKMNQUVXZ"          # Jan..Dec

# Days on which the market itself was shut -- no vendor has data for these, so
# they are never fetch candidates even though the audit reports them missing.
MARKET_CLOSED = {"20250418"}          # Good Friday 2025

# Explicit front-month overrides for days where the repo's roll rule diverges
# from where the volume actually was. COMEX gold's first notice day is the last
# business day of the month BEFORE delivery, so liquidity leaves the June
# contract in late May -- but Build-ContinuousContracts.ps1 rolls metals on the
# 1st of the expiry month, holding the dying contract ~3 trading days too long.
# Confirmed via metadata.get_cost on 2026-07-26: MGCM5 collapsed to $0.0002 on
# 2025-05-30 while MGCQ5 still billed $0.2175 (full-size GC shows the same).
# Fetching M5 here would write near-empty files that look present -- worse than
# leaving the day missing. Same reasoning applies to every GC/MGC roll.
CONTRACT_OVERRIDE = {
    ("MGC", "20250528"): "MGCQ5",
    ("MGC", "20250529"): "MGCQ5",
    ("MGC", "20250530"): "MGCQ5",
}


# --------------------------------------------------------------------------
# contract roll -- mirrors Get-ActiveContractForDate in
# M:\NinjaTrader_DataRepo\Scripts\Audit-ContinuousContracts.ps1 so the fetched
# day comes from the same contract the rest of the continuous series uses.
# --------------------------------------------------------------------------

def third_friday(year: int, month: int) -> date:
    d = date(year, month, 1)
    while d.weekday() != 4:
        d += timedelta(days=1)
    return d + timedelta(days=14)


def nth_business_day_before(d: date, n: int) -> date:
    cur, count = d - timedelta(days=1), 0
    while True:
        if cur.weekday() < 5:
            count += 1
            if count >= n:
                return cur
        cur -= timedelta(days=1)


def active_contract(symbol: str, d: date) -> tuple[int, int]:
    """-> (expiry_year, expiry_month) of the front contract covering `d`."""
    if symbol in QUARTERLY:
        months, rule = (3, 6, 9, 12), "quarterly"
    elif symbol in METALS:
        months, rule = (2, 4, 6, 8, 10, 12), "monthly1st"
    else:                                   # CL/MCL and anything else
        months, rule = tuple(range(1, 13)), "crudeoil"

    cands = []
    for y in range(d.year - 2, d.year + 2):
        for m in months:
            if rule == "quarterly":
                roll = third_friday(y, m) - timedelta(days=4)
            elif rule == "crudeoil":
                pm, py = (12, y - 1) if m == 1 else (m - 1, y)
                roll = nth_business_day_before(date(py, pm, 25), 3)
            else:
                roll = date(y, m, 1)
            cands.append((y, m, roll))
    cands.sort(key=lambda c: c[0] * 12 + c[1])

    prev = date.min
    for y, m, roll in cands:
        if prev <= d < roll:
            return y, m
        prev = roll
    return cands[-1][0], cands[-1][1]


def raw_symbol(symbol: str, d: date) -> str:
    """CME/Databento raw symbol, e.g. YM 2025-05-28 -> YMM5."""
    override = CONTRACT_OVERRIDE.get((symbol, d.strftime("%Y%m%d")))
    if override:
        return override
    y, m = active_contract(symbol, d)
    return f"{symbol}{MONTH_CODE[m - 1]}{y % 10}"


# --------------------------------------------------------------------------
# gap detection
# --------------------------------------------------------------------------

def year_range(symbol: str, year: int) -> tuple[date, date]:
    if symbol in QUARTERLY:
        return (third_friday(year - 1, 12) - timedelta(days=4),
                third_friday(year, 12) - timedelta(days=5))
    return date(year, 1, 1), date(year, 12, 31)


def find_gaps(year: int, symbols: list[str] | None) -> dict[str, list[str]]:
    """Absent weekdays per symbol in the NRD continuous archive.

    Only *absent* files count. Small-but-present files are the thin holiday
    evening sessions (Christmas, New Year) which are complete as recorded --
    treating them as gaps would refetch data we already have.
    """
    folder_root = NRD_ROOT / str(year)
    out: dict[str, list[str]] = {}
    for p in sorted(folder_root.iterdir()):
        if not p.is_dir():
            continue
        sym = p.name.split()[0]
        if symbols and sym not in symbols:
            continue
        present = {f.stem for f in p.glob("*.nrd")
                   if len(f.stem) == 8 and f.stem.isdigit()}
        start, end = year_range(sym, year)
        gaps = []
        d = start
        while d <= end:
            k = d.strftime("%Y%m%d")
            if d.weekday() < 5 and k not in present and k not in MARKET_CLOSED:
                gaps.append(k)
            d += timedelta(days=1)
        if gaps:
            out[sym] = gaps
    return out


# --------------------------------------------------------------------------
# Databento HTTP
# --------------------------------------------------------------------------

def _auth_header(key: str) -> str:
    return "Basic " + base64.b64encode(f"{key}:".encode()).decode()


def _request(method: str, endpoint: str, key: str, params: dict) -> bytes:
    body = urllib.parse.urlencode(params).encode()
    if method == "GET":
        req = urllib.request.Request(f"{API}/{endpoint}?{body.decode()}")
    else:
        req = urllib.request.Request(f"{API}/{endpoint}", data=body)
    req.add_header("Authorization", _auth_header(key))
    with urllib.request.urlopen(req, timeout=600) as r:
        return r.read()


def day_window(day: str) -> tuple[str, str]:
    """ET calendar day -> [start, end) as UTC ISO strings.

    Repo day files are ET calendar days (see CLAUDE.md), so the fetch window
    must be the ET midnight-to-midnight span expressed in UTC.
    """
    d = datetime.strptime(day, "%Y%m%d").replace(tzinfo=EASTERN)
    lo = d.astimezone(ZoneInfo("UTC"))
    hi = (d + timedelta(days=1)).astimezone(ZoneInfo("UTC"))
    return lo.strftime("%Y-%m-%dT%H:%M:%S"), hi.strftime("%Y-%m-%dT%H:%M:%S")


def get_cost(key: str, symbol: str, day: str) -> float:
    lo, hi = day_window(day)
    raw = _request("GET", "metadata.get_cost", key, {
        "dataset": DATASET, "symbols": raw_symbol(
            symbol, datetime.strptime(day, "%Y%m%d").date()),
        "schema": SCHEMA, "start": lo, "end": hi,
        "stype_in": "raw_symbol",
    })
    return float(json.loads(raw))


def fetch_tbbo(key: str, symbol: str, day: str) -> list[dict]:
    lo, hi = day_window(day)
    raw = _request("POST", "timeseries.get_range", key, {
        "dataset": DATASET, "symbols": raw_symbol(
            symbol, datetime.strptime(day, "%Y%m%d").date()),
        "schema": SCHEMA, "start": lo, "end": hi,
        "stype_in": "raw_symbol",
        "encoding": "csv",
        "pretty_px": "true",     # decimal prices, not 1e-9 fixed point
        "pretty_ts": "false",    # keep raw int64 ns -- what we store
        "map_symbols": "false",
    })
    return list(csv.DictReader(io.StringIO(raw.decode())))


# --------------------------------------------------------------------------
# TBBO -> NT8-style L1 Parquet
# --------------------------------------------------------------------------

def to_l1_table(rows: list[dict]) -> pa.Table:
    """Expand each TBBO record into Ask, Bid, Last events.

    data._reduce_raw takes, for each trade, the most recent preceding Ask/Bid
    by array position -- so the two quote events must precede their trade.
    """
    ts_out, mdt_out, px_out, vol_out = [], [], [], []
    for r in rows:
        ts = int(r["ts_recv"])
        for px_key, sz_key, mdt in (("ask_px_00", "ask_sz_00", MDT_ASK),
                                    ("bid_px_00", "bid_sz_00", MDT_BID)):
            px = r.get(px_key, "")
            if px in ("", "nan"):
                continue
            px = float(px)
            if not np.isfinite(px) or abs(px) > 1e15:   # unset-book sentinel
                continue
            ts_out.append(ts)
            mdt_out.append(mdt)
            px_out.append(px)
            vol_out.append(int(r.get(sz_key) or 0))
        ts_out.append(ts)
        mdt_out.append(MDT_LAST)
        px_out.append(float(r["price"]))
        vol_out.append(int(r["size"]))

    table = pa.table({
        "Timestamp": pa.array(np.asarray(ts_out, dtype="int64")
                              .view("datetime64[ns]"),
                              type=pa.timestamp("ns", tz="UTC")),
        "MarketDataType": pa.array(np.asarray(mdt_out, dtype="int8")),
        "Price": pa.array(np.asarray(px_out, dtype="float64")),
        "Volume": pa.array(np.asarray(vol_out, dtype="int64")),
    })
    # Tagged UTC so data._reduce_raw skips the legacy ET->UTC correction.
    return table.replace_schema_metadata({
        b"replay_importer.timestamps": b"UTC",
        b"replay_importer.source_tz": b"UTC",
        b"replay_importer.version": b"2",
        b"replay_importer.source_name": b"databento GLBX.MDP3 tbbo",
    })


def out_path(symbol: str, day: str, year: int) -> Path:
    return PARQUET_ROOT / str(year) / f"{symbol}-{year}_L1" / f"{day}.parquet"


# --------------------------------------------------------------------------

def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__,
                                 formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--year", type=int, default=2025)
    ap.add_argument("--symbols", help="comma list; default = maintained symbols")
    ap.add_argument("--include-cl", action="store_true",
                    help="also fill CL/MCL (not in the maintained set)")
    mode = ap.add_mutually_exclusive_group(required=True)
    mode.add_argument("--cost", action="store_true", help="price it, download nothing")
    mode.add_argument("--fetch", action="store_true", help="download and write Parquet")
    ap.add_argument("--overwrite", action="store_true")
    args = ap.parse_args()

    key = os.environ.get("DATABENTO_API_KEY")
    if not key:
        print("DATABENTO_API_KEY is not set.", file=sys.stderr)
        return 2

    syms = ([s.strip().upper() for s in args.symbols.split(",")]
            if args.symbols else None)
    gaps = find_gaps(args.year, syms)
    if not syms:
        allow = MAINTAINED | ({"CL", "MCL"} if args.include_cl else set())
        gaps = {s: v for s, v in gaps.items() if s in allow}

    if not gaps:
        print("No fillable gaps found.")
        return 0

    total_days = sum(len(v) for v in gaps.values())
    print(f"Fillable gaps: {total_days} day-fetches across {len(gaps)} symbols\n")

    if args.cost:
        grand = 0.0
        for sym in sorted(gaps):
            sub = 0.0
            for day in gaps[sym]:
                d = datetime.strptime(day, "%Y%m%d").date()
                c = get_cost(key, sym, day)
                sub += c
                print(f"  {sym:<4} {day}  {raw_symbol(sym, d):<6} ${c:>8.4f}")
            grand += sub
            print(f"  {sym:<4} subtotal{'':<15}${sub:>8.4f}\n")
        print(f"TOTAL ESTIMATE: ${grand:.2f}  ({total_days} day-fetches, "
              f"schema={SCHEMA})")
        print("No data downloaded. Re-run with --fetch to download.")
        return 0

    written = 0
    for sym in sorted(gaps):
        for day in gaps[sym]:
            dst = out_path(sym, day, args.year)
            if dst.exists() and not args.overwrite:
                print(f"  {sym} {day}  exists, skipping")
                continue
            rows = fetch_tbbo(key, sym, day)
            if not rows:
                print(f"  {sym} {day}  NO DATA returned (market closed?)")
                continue
            table = to_l1_table(rows)
            dst.parent.mkdir(parents=True, exist_ok=True)
            tmp = dst.with_suffix(f".{os.getpid()}.tmp")
            pq.write_table(table, tmp, compression="zstd")
            os.replace(tmp, dst)
            written += 1
            print(f"  {sym} {day}  {len(rows):>8,} trades -> "
                  f"{table.num_rows:>9,} L1 events  {dst}")
    print(f"\nWrote {written} day files.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
