"""Parameter sweep CLI.

    python sweep.py strategies\\ema_cross.py --param fast_period=6,9,12 ^
        --param slow_period=18,21,27 --start 2026-03-01 --end 2026-06-17

Ranks combos by --metric (default sharpe), writes reports\\sweep_*.csv, and
prints a per-parameter sensitivity plateau around the best combo.
"""
from __future__ import annotations

import argparse
from pathlib import Path

from backtester import sweep as sw
from backtester.loader import load_strategy_class


def parse_value(s: str):
    for cast in (int, float):
        try:
            return cast(s)
        except ValueError:
            pass
    return s


def parse_params(items: list[str]) -> dict[str, list]:
    grid = {}
    for it in items:
        name, _, vals = it.partition("=")
        if not vals:
            raise SystemExit(f"--param must be name=v1,v2,... (got {it!r})")
        grid[name.strip()] = [parse_value(v) for v in vals.split(",")]
    return grid


def main() -> None:
    ap = argparse.ArgumentParser(description="Parameter sweep")
    ap.add_argument("strategy")
    ap.add_argument("--param", action="append", required=True,
                    metavar="name=v1,v2,...")
    ap.add_argument("--symbol")
    ap.add_argument("--period")
    ap.add_argument("--start")
    ap.add_argument("--end")
    ap.add_argument("--balance", type=float, default=50_000.0)
    ap.add_argument("--prop-threshold", type=float, default=2500.0)
    ap.add_argument("--slippage", type=float, default=0.0)
    ap.add_argument("--daily-loss-limit", type=float, default=None)
    ap.add_argument("--metric", default="sharpe",
                    choices=["sharpe", "calmar", "net_pnl", "profit_factor"])
    ap.add_argument("--min-trades", type=int, default=10,
                    help="combos with fewer trades rank last (default 10)")
    ap.add_argument("--workers", type=int, default=None)
    args = ap.parse_args()

    grid = parse_params(args.param)
    n = 1
    for v in grid.values():
        n *= len(v)
    print(f"Sweeping {n} combos of {list(grid)} ...")

    rows = sw.run_sweep(
        args.strategy, grid, start=args.start, end=args.end,
        symbol=args.symbol, period=args.period, start_balance=args.balance,
        prop_threshold=args.prop_threshold if args.prop_threshold > 0 else None,
        slippage_ticks=args.slippage, daily_loss_limit=args.daily_loss_limit,
        workers=args.workers)

    ranked = sw.rank(rows, args.metric, args.min_trades)
    params = list(grid)
    print(f"\n{'rank':<5}" + "".join(f"{p:<14}" for p in params)
          + f"{'net_pnl':>10}{'sharpe':>8}{'calmar':>8}{'pf':>6}"
          + f"{'win%':>6}{'maxDD':>10}{'trades':>8}{'propHR':>9}")
    for i, r in enumerate(ranked[:25], 1):
        print(f"{i:<5}" + "".join(f"{r[p]!s:<14}" for p in params)
              + f"{r['net_pnl']:>10,.0f}{r['sharpe']:>8.2f}"
              + f"{r['calmar']:>8.2f}{r['profit_factor']:>6.2f}"
              + f"{r['win_rate']:>6.1f}{r['max_drawdown']:>10,.0f}"
              + f"{r['total_trades']:>8}"
              + (f"{r['prop_min_headroom']:>9,.0f}"
                 if r['prop_min_headroom'] == r['prop_min_headroom']
                 else f"{'n/a':>9}"))

    best = ranked[0]
    print(sw.format_sensitivity(sw.sensitivity(rows, best, grid, args.metric),
                                args.metric))

    name = load_strategy_class(args.strategy).__name__
    out = sw.write_csv(ranked, Path("reports") / f"sweep_{name}.csv")
    print(f"\nFull results: {out.resolve()}")


if __name__ == "__main__":
    main()
