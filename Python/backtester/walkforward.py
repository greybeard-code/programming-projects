"""Walk-forward analysis CLI.

    python walkforward.py strategies\\ema_cross.py --param fast_period=6,9,12 ^
        --param slow_period=18,21,27 --windows 5 --ratio 5

Optimizes each in-sample window over the parameter grid, runs the best combo
out-of-sample, and reports stitched OOS performance + walk-forward efficiency.
"""
from __future__ import annotations

import argparse

from backtester import walkforward as wf
from sweep import parse_params


def main() -> None:
    ap = argparse.ArgumentParser(description="Walk-forward analysis")
    ap.add_argument("strategy")
    ap.add_argument("--param", action="append", required=True,
                    metavar="name=v1,v2,...")
    ap.add_argument("--windows", type=int, default=5)
    ap.add_argument("--ratio", type=int, default=5, help="IS:OOS ratio")
    ap.add_argument("--symbol")
    ap.add_argument("--period")
    ap.add_argument("--start")
    ap.add_argument("--end")
    ap.add_argument("--balance", type=float, default=50_000.0)
    ap.add_argument("--prop-threshold", type=float, default=2000.0)
    ap.add_argument("--slippage", type=float, default=0.0)
    ap.add_argument("--daily-loss-limit", type=float, default=None)
    ap.add_argument("--metric", default="sharpe",
                    choices=["sharpe", "calmar", "net_pnl", "profit_factor"])
    ap.add_argument("--min-trades", type=int, default=10)
    ap.add_argument("--workers", type=int, default=None)
    args = ap.parse_args()

    windows = wf.run_walkforward(
        args.strategy, parse_params(args.param),
        n_windows=args.windows, ratio=args.ratio, metric=args.metric,
        min_trades=args.min_trades, start=args.start, end=args.end,
        symbol=args.symbol, period=args.period, start_balance=args.balance,
        prop_threshold=args.prop_threshold if args.prop_threshold > 0 else None,
        slippage_ticks=args.slippage, daily_loss_limit=args.daily_loss_limit,
        workers=args.workers)
    print(wf.summarize(windows, args.metric, args.balance))


if __name__ == "__main__":
    main()
