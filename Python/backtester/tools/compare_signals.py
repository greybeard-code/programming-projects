"""Compare an NT8 signal export (tools/gbSignalExporter.cs) against the
Python gbsignals ports — the GodZillaKilla per-engine parity check.

Usage:
    python tools\\compare_signals.py "signals_MNQ_....csv" --symbol MNQ
        --period r60-3 [--tz America/New_York] [--tolerance-s 10]
        [--engines ko,pa,th,sj,su,nc]

Bars are matched one-to-one monotonically by close timestamp (same
convention as compare_bars; feeds skew a few seconds, so small-T renko needs
--tolerance-s 10). Signal codes are compared only on MATCHED bars — bar
mismatches are a bar-parity issue (compare_bars), not a signal-port issue.

The export runs all six engines at GodZillaKilla v1.10 DEFAULT parameters,
so the ports are run with their defaults too. Expect early-history
divergence while warmups differ (NT8 loads N days back; we start at
--start): judge parity from the stabilized region, like the renko work.
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
from backtester.gbsignals import (                 # noqa: E402
    KingOrderBlock, NobleCloud, PanaKanal, SuperJumpBoost, SumoPullback,
    ThunderZilla,
)
from backtester.strategy import parse_barspec      # noqa: E402

ENGINES = ("ko", "pa", "th", "sj", "su", "nc")


def load_nt8(path: str, tz: ZoneInfo) -> list[dict]:
    rows = []
    with open(path, newline="", encoding="utf-8-sig") as f:
        for row in csv.DictReader(f):
            t = datetime.strptime(row["time"], "%Y-%m-%d %H:%M:%S.%f")
            rows.append({
                "ts": int(t.replace(tzinfo=tz).timestamp() * 1e9),
                **{e: int(row[e]) for e in ENGINES if e in row},
            })
    rows.sort(key=lambda r: r["ts"])
    return rows


def run_ports(symbol: str, period: str, start: str, end: str):
    """Our bars + the six ported engines' codes per bar."""
    spec = get_spec(symbol)
    cat = Catalog()
    barspec = parse_barspec(period)
    engines = {
        "ko": KingOrderBlock(tick_size=spec.tick_size),
        "pa": PanaKanal(tick_size=spec.tick_size),
        "th": ThunderZilla(tick_size=spec.tick_size),
        "sj": SuperJumpBoost(tick_size=spec.tick_size),
        "su": SumoPullback(),
        "nc": NobleCloud(),
    }
    out = []
    for date in cat.days(symbol, start, end):
        day = cat.load_day(symbol, date)
        if len(day) == 0:
            continue
        bars = cat.load_bars(symbol, date, barspec, spec.tick_size, day)
        for j in range(len(bars)):
            o, h = float(bars.open[j]), float(bars.high[j])
            l, c = float(bars.low[j]), float(bars.close[j])
            v = float(bars.volume[j])
            row = {"ts": int(bars.ts_end[j])}
            for name, eng in engines.items():
                row[name] = (eng.update(o, h, l, c, v) if name == "th"
                             else eng.update(o, h, l, c))
            out.append(row)
    return out


def match(nt8: list[dict], ours: list[dict], tol_ns: int):
    """One-to-one monotonic timestamp matching (compare_bars convention)."""
    pairs = []
    i = j = 0
    while i < len(nt8) and j < len(ours):
        d = nt8[i]["ts"] - ours[j]["ts"]
        if abs(d) <= tol_ns:
            pairs.append((nt8[i], ours[j]))
            i += 1
            j += 1
        elif d < 0:
            i += 1
        else:
            j += 1
    return pairs


def main() -> None:
    ap = argparse.ArgumentParser()
    ap.add_argument("nt8_csv", help="export from gbSignalExporter.cs")
    ap.add_argument("--symbol", default="MNQ")
    ap.add_argument("--period", default="r60-3")
    ap.add_argument("--tz", default="America/New_York")
    ap.add_argument("--tolerance-s", type=float, default=10.0)
    ap.add_argument("--engines", default=",".join(ENGINES))
    ap.add_argument("--skip-warmup", type=int, default=500,
                    help="ignore the first N matched bars (warmup divergence)")
    ap.add_argument("--show", type=int, default=15)
    args = ap.parse_args()

    nt8 = load_nt8(args.nt8_csv, ZoneInfo(args.tz))
    if not nt8:
        raise SystemExit("empty NT8 export")
    engines = [e.strip().lower() for e in args.engines.split(",")]
    missing = [e for e in engines if e not in nt8[0]]
    if missing:
        raise SystemExit(f"export lacks columns {missing}")

    d0 = datetime.fromtimestamp(nt8[0]["ts"] / 1e9).strftime("%Y-%m-%d")
    d1 = datetime.fromtimestamp(nt8[-1]["ts"] / 1e9).strftime("%Y-%m-%d")
    print(f"NT8 export: {len(nt8)} bars, {d0} .. {d1}")
    ours = run_ports(args.symbol, args.period, d0, d1)
    print(f"backtester: {len(ours)} bars ({args.period})")

    pairs = match(nt8, ours, int(args.tolerance_s * 1e9))
    print(f"matched: {len(pairs)} bars "
          f"({100 * len(pairs) / max(1, len(nt8)):.1f}% of export)")
    pairs = pairs[args.skip_warmup:]
    if not pairs:
        raise SystemExit("nothing left after warmup skip")

    print(f"\nSignal parity over {len(pairs)} matched bars "
          f"(first {args.skip_warmup} skipped as warmup):")
    for e in engines:
        a = np.array([p[0][e] for p in pairs])
        b = np.array([p[1][e] for p in pairs])
        ok = a == b
        nz = (a != 0) | (b != 0)
        nz_ok = (a[nz] == b[nz]) if nz.any() else np.array([True])
        print(f"  {e.upper():<3} exact {100 * ok.mean():6.2f}%   "
              f"on signal bars {100 * nz_ok.mean():6.2f}% "
              f"({int(nz.sum())} bars with a signal on either side)")
        bad = np.nonzero(~ok)[0][:args.show]
        for k in bad:
            ts = datetime.fromtimestamp(pairs[k][0]["ts"] / 1e9,
                                        ZoneInfo(args.tz))
            print(f"      {ts:%Y-%m-%d %H:%M:%S} ET  nt8={pairs[k][0][e]:+d} "
                  f"ours={pairs[k][1][e]:+d}")


if __name__ == "__main__":
    main()
