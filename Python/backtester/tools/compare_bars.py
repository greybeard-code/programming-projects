"""Compare an NT8 bar export (from tools/gbBarExporter.cs) against this
backtester's bars for the same symbol/period — the ninZaRenko parity check.

Usage:
    python tools\\compare_bars.py "bars_MNQ_....csv" --symbol MNQ --period r100-4
        [--tz America/New_York] [--tolerance-s 2]

NT8 bar times are in the chart/PC timezone (user's machine: US/Eastern —
the default --tz). Bars are matched by close timestamp within the tolerance;
OHLC deltas are reported in ticks.

Interpretation guide:
- high match rate + zero-tick deltas mid-session = the Renko geometry agrees.
- mismatches clustered at session opens usually mean a different bar-reset
  convention (NT8 resets the brick anchor per session template; we reset per
  UTC day file) — expected, and it washes out after the first bars.
"""
from __future__ import annotations

import argparse
import csv
import sys
from datetime import datetime
from pathlib import Path
from zoneinfo import ZoneInfo

import numpy as np

sys.path.insert(0, str(Path(__file__).resolve().parent.parent))
from backtester.contracts import get_spec          # noqa: E402
from backtester.data import Catalog                # noqa: E402
from backtester.strategy import parse_barspec      # noqa: E402


def load_nt8(path: str, tz: ZoneInfo) -> dict[str, np.ndarray]:
    ts, o, h, l, c = [], [], [], [], []
    with open(path, newline="", encoding="utf-8-sig") as f:
        for row in csv.DictReader(f):
            t = datetime.strptime(row["time"], "%Y-%m-%d %H:%M:%S.%f")
            ts.append(int(t.replace(tzinfo=tz).timestamp() * 1e9))
            o.append(float(row["open"]))
            h.append(float(row["high"]))
            l.append(float(row["low"]))
            c.append(float(row["close"]))
    order = np.argsort(np.asarray(ts))
    return {"ts": np.asarray(ts, dtype="int64")[order],
            "o": np.asarray(o)[order], "h": np.asarray(h)[order],
            "l": np.asarray(l)[order], "c": np.asarray(c)[order]}


def main() -> None:
    ap = argparse.ArgumentParser()
    ap.add_argument("nt8_csv", help="export from gbBarExporter.cs")
    ap.add_argument("--symbol", default="MNQ")
    ap.add_argument("--period", default="r100-4")
    ap.add_argument("--tz", default="America/New_York",
                    help="timezone of the NT8 export times")
    ap.add_argument("--tolerance-s", type=float, default=2.0)
    ap.add_argument("--show", type=int, default=15,
                    help="max mismatch rows to print")
    args = ap.parse_args()

    nt8 = load_nt8(args.nt8_csv, ZoneInfo(args.tz))
    spec = parse_barspec(args.period)
    contract = get_spec(args.symbol)
    tick = contract.tick_size

    # collect our bars for the UTC dates the export spans (plus neighbors,
    # since ET evenings land in the next UTC day file)
    d0 = np.datetime64(nt8["ts"].min(), "ns").astype("datetime64[D]")
    d1 = np.datetime64(nt8["ts"].max(), "ns").astype("datetime64[D]")
    dates = np.arange(d0, d1 + 2).astype(str)
    catalog = Catalog()
    have = set(catalog.days(args.symbol))
    ts, o, h, l, c = [], [], [], [], []
    for d in dates:
        date = d.replace("-", "")
        if date not in have:
            continue
        b = catalog.load_bars(args.symbol, date, spec, tick)
        ts.append(b.ts_end)
        o.append(b.open)
        h.append(b.high)
        l.append(b.low)
        c.append(b.close)
    if not ts:
        raise SystemExit("No backtester data for the export's date range.")
    ours = {k: np.concatenate(v) for k, v in
            zip("ts o h l c".split(), (ts, o, h, l, c))}

    tol = int(args.tolerance_s * 1e9)
    pos = np.searchsorted(ours["ts"], nt8["ts"])
    matched = same_close = same_ohlc = 0
    mismatches = []
    for i in range(len(nt8["ts"])):
        best, best_dt = -1, tol + 1
        for j in (pos[i] - 1, pos[i]):
            if 0 <= j < len(ours["ts"]):
                dt = abs(int(ours["ts"][j]) - int(nt8["ts"][i]))
                if dt < best_dt:
                    best, best_dt = j, dt
        if best < 0 or best_dt > tol:
            mismatches.append((i, None, "no bar within tolerance"))
            continue
        matched += 1
        dc = (nt8["c"][i] - ours["c"][best]) / tick
        do = (nt8["o"][i] - ours["o"][best]) / tick
        dh = (nt8["h"][i] - ours["h"][best]) / tick
        dl = (nt8["l"][i] - ours["l"][best]) / tick
        if dc == 0:
            same_close += 1
        if dc == do == dh == dl == 0:
            same_ohlc += 1
        elif len(mismatches) < 1000:
            mismatches.append((i, best,
                               f"dO {do:+.0f} dH {dh:+.0f} dL {dl:+.0f} "
                               f"dC {dc:+.0f} ticks"))

    n = len(nt8["ts"])
    print(f"NT8 bars: {n}   ours in range: {len(ours['ts'])}")
    print(f"matched by time: {matched}/{n} ({100 * matched / n:.1f}%)")
    if matched:
        print(f"identical close: {same_close}/{matched} "
              f"({100 * same_close / matched:.1f}%)")
        print(f"identical OHLC:  {same_ohlc}/{matched} "
              f"({100 * same_ohlc / matched:.1f}%)")
    if mismatches:
        print(f"\nfirst {min(args.show, len(mismatches))} of "
              f"{len(mismatches)} mismatches:")
        for i, j, why in mismatches[:args.show]:
            t = np.datetime64(int(nt8["ts"][i]), "ns")
            print(f"  nt8[{i}] {t} c={nt8['c'][i]}: {why}")
        print("\nIf mismatches cluster at session opens, it's the bar-reset "
              "convention (see module docstring); mid-session disagreement "
              "means real geometry differences worth investigating.")
    else:
        print("\nPerfect parity.")


if __name__ == "__main__":
    main()
