"""Compare a NinjaTrader 8 Strategy Analyzer trade export against this
backtester's trade log CSV.

Usage:
    python tools\\compare_nt8.py reports\\EmaCross_MNQ_trades.csv nt8_export.csv
        [--symbol MNQ] [--tz America/Chicago] [--tolerance-s 90]

NT8 export: Strategy Analyzer -> Trades grid -> right-click -> Export.
Column names vary slightly by NT8 version/locale; this parser sniffs for the
usual ones (Market pos., Qty, Entry price, Exit price, Entry time, Exit time).
NT8 times are exchange-local (assumed US/Central unless --tz says otherwise);
ours are UTC.

Trades are matched greedily by direction + nearest entry time within the
tolerance. Output: per-trade entry/exit deltas in ticks, plus a summary.
"""
from __future__ import annotations

import argparse
import csv
import sys
from datetime import datetime
from pathlib import Path
from zoneinfo import ZoneInfo

sys.path.insert(0, str(Path(__file__).resolve().parent.parent))
from backtester.contracts import get_spec  # noqa: E402

UTC = ZoneInfo("UTC")

TIME_FORMATS = [
    "%Y-%m-%d %H:%M:%S", "%Y-%m-%dT%H:%M:%S", "%m/%d/%Y %I:%M:%S %p",
    "%m/%d/%Y %H:%M:%S", "%d/%m/%Y %H:%M:%S", "%Y-%m-%d %H:%M",
]


def parse_time(s: str, tz: ZoneInfo) -> datetime:
    s = s.strip()
    for fmt in TIME_FORMATS:
        try:
            return datetime.strptime(s, fmt).replace(tzinfo=tz)
        except ValueError:
            continue
    # ISO with fractional seconds (our own format)
    return datetime.fromisoformat(s).replace(tzinfo=tz)


def parse_price(s: str) -> float:
    return float(s.replace("$", "").replace(",", "").replace("'", "").strip())


def sniff(fieldnames: list[str], *candidates: str) -> str:
    low = {f.lower().strip(): f for f in fieldnames}
    for c in candidates:
        if c in low:
            return low[c]
    for f in fieldnames:                       # substring fallback
        for c in candidates:
            if c in f.lower():
                return f
    raise SystemExit(f"Could not find any of {candidates} in columns "
                     f"{fieldnames}")


def load_ours(path: str) -> list[dict]:
    out = []
    with open(path, newline="", encoding="utf-8-sig") as f:
        for row in csv.DictReader(f):
            out.append({
                "dir": 1 if row["direction"] == "long" else -1,
                "qty": int(row["qty"]),
                "entry_t": parse_time(row["entry_time_utc"], UTC),
                "exit_t": parse_time(row["exit_time_utc"], UTC),
                "entry_p": float(row["entry_price"]),
                "exit_p": float(row["exit_price"]),
            })
    return out


def load_nt8(path: str, tz: ZoneInfo) -> list[dict]:
    with open(path, newline="", encoding="utf-8-sig") as f:
        rd = csv.DictReader(f)
        cols = rd.fieldnames or []
        c_pos = sniff(cols, "market pos.", "market pos", "position")
        c_qty = sniff(cols, "qty", "quantity")
        c_ep = sniff(cols, "entry price")
        c_xp = sniff(cols, "exit price")
        c_et = sniff(cols, "entry time")
        c_xt = sniff(cols, "exit time")
        out = []
        for row in rd:
            pos = row[c_pos].strip().lower()
            out.append({
                "dir": 1 if pos.startswith("l") else -1,
                "qty": int(float(row[c_qty])),
                "entry_t": parse_time(row[c_et], tz),
                "exit_t": parse_time(row[c_xt], tz),
                "entry_p": parse_price(row[c_ep]),
                "exit_p": parse_price(row[c_xp]),
            })
    return out


def main() -> None:
    ap = argparse.ArgumentParser()
    ap.add_argument("ours", help="backtester *_trades.csv")
    ap.add_argument("nt8", help="NT8 Strategy Analyzer trade export CSV")
    ap.add_argument("--symbol", default="MNQ")
    ap.add_argument("--tz", default="America/Chicago",
                    help="timezone of the NT8 export times")
    ap.add_argument("--tolerance-s", type=float, default=90.0,
                    help="max entry-time difference to consider a match")
    args = ap.parse_args()

    spec = get_spec(args.symbol)
    tick = spec.tick_size
    ours = load_ours(args.ours)
    theirs = load_nt8(args.nt8, ZoneInfo(args.tz))
    print(f"ours: {len(ours)} trades   nt8: {len(theirs)} trades")

    unmatched_theirs = list(theirs)
    matches, unmatched_ours = [], []
    for o in ours:
        best, best_dt = None, args.tolerance_s
        for t in unmatched_theirs:
            if t["dir"] != o["dir"]:
                continue
            dt = abs((t["entry_t"] - o["entry_t"]).total_seconds())
            if dt <= best_dt:
                best, best_dt = t, dt
        if best is None:
            unmatched_ours.append(o)
        else:
            unmatched_theirs.remove(best)
            matches.append((o, best, best_dt))

    ent_d, ext_d = [], []
    print(f"\nmatched {len(matches)} | only-ours {len(unmatched_ours)} "
          f"| only-nt8 {len(unmatched_theirs)}\n")
    print(f"{'entry(ours)':>20} {'dT s':>6} {'dEntry tk':>10} {'dExit tk':>9}")
    for o, t, dt in matches:
        de = (o["entry_p"] - t["entry_p"]) / tick
        dx = (o["exit_p"] - t["exit_p"]) / tick
        ent_d.append(de)
        ext_d.append(dx)
        flag = "  <-- check" if abs(de) > 2 or abs(dx) > 2 else ""
        print(f"{o['entry_t']:%Y-%m-%d %H:%M:%S} {dt:6.1f} {de:10.1f} "
              f"{dx:9.1f}{flag}")

    if matches:
        import statistics as st
        print(f"\nentry delta ticks: mean {st.mean(ent_d):+.2f}  "
              f"median {st.median(ent_d):+.2f}  max|.| {max(map(abs, ent_d)):.1f}")
        print(f"exit  delta ticks: mean {st.mean(ext_d):+.2f}  "
              f"median {st.median(ext_d):+.2f}  max|.| {max(map(abs, ext_d)):.1f}")
        print("\nInterpretation: consistent +/- deltas on market/stop fills "
              "usually mean a slippage-model difference; scattered large "
              "deltas mean a logic/data mismatch worth digging into.")
    for label, lst in (("only-ours", unmatched_ours),
                       ("only-nt8", unmatched_theirs)):
        for x in lst[:10]:
            d = "L" if x["dir"] > 0 else "S"
            print(f"{label}: {d} {x['qty']} entry {x['entry_t']} @ {x['entry_p']}")


if __name__ == "__main__":
    main()
