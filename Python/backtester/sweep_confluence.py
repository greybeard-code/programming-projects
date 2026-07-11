"""Confluence sweep CLI — which engines, how many must agree, which required.

    python sweep_confluence.py strategies\\godzilla_killa.py ^
        --template "nt8 code\\GodZillaKilla\\templates\\OneSet_3ofAll_BestTime.xml" ^
        --sources KO,PA,TH,SJ,SU,NC --requires none --min-size 2 ^
        --start 2026-03-01 --end 2026-06-17

The template supplies the base config (operators, windows, ATM exits, daily
limits, bar series); the sweep varies only the confluence knobs. Ranks by
--metric, prints the league table, writes reports\\confluence_*.csv.
"""
from __future__ import annotations

import argparse
from pathlib import Path

from backtester import sweep_confluence as sc
from backtester.sweep import rank, write_csv


def main() -> None:
    ap = argparse.ArgumentParser(description="Confluence sweep")
    ap.add_argument("strategy", help="strategies\\godzilla_killa.py")
    ap.add_argument("--template", help="NT8 strategy template XML (base config)")
    ap.add_argument("--symbol",
                    help="override the run symbol (some templates save an "
                         "empty instrument field, in which case the class "
                         "default silently applies without this)")
    ap.add_argument("--sources", default="KO,PA,TH,SJ,SU,NC",
                    help="comma list of engines to consider")
    ap.add_argument("--counts", default="all",
                    help='"all" or comma list, e.g. 2,3,4')
    ap.add_argument("--requires", default="none",
                    choices=["none", "single", "all"],
                    help="require-flag enumeration (all = powerset, explodes)")
    ap.add_argument("--min-size", type=int, default=1,
                    help="smallest engine subset to test")
    ap.add_argument("--start")
    ap.add_argument("--end")
    ap.add_argument("--balance", type=float, default=50_000.0)
    ap.add_argument("--prop-threshold", type=float, default=2000.0)
    ap.add_argument("--slippage", type=float, default=0.0)
    ap.add_argument("--metric", default="sharpe",
                    choices=["sharpe", "calmar", "net_pnl", "profit_factor"])
    ap.add_argument("--min-trades", type=int, default=10)
    ap.add_argument("--top", type=int, default=25)
    ap.add_argument("--workers", type=int, default=None)
    args = ap.parse_args()

    sources = [s.strip().lower() for s in args.sources.split(",") if s.strip()]
    counts = (args.counts if args.counts == "all"
              else [int(c) for c in args.counts.split(",")])

    rows = sc.run_confluence(
        args.strategy, template=args.template, symbol=args.symbol,
        sources=sources,
        counts=counts, requires=args.requires, min_size=args.min_size,
        start=args.start, end=args.end, start_balance=args.balance,
        prop_threshold=args.prop_threshold if args.prop_threshold > 0 else None,
        slippage_ticks=args.slippage, workers=args.workers)

    print(sc.format_league(rows, args.metric, args.top, args.min_trades))
    out = write_csv(rank(rows, args.metric, args.min_trades),
                    Path("reports") / "confluence_GodZillaKilla.csv")
    print(f"\nFull results: {out.resolve()}")


if __name__ == "__main__":
    main()
