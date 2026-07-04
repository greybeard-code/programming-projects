"""Run a backtest from the command line.

Usage:
    python cli.py strategies\\ema_cross.py --start 2026-06-01 --end 2026-06-17
    python cli.py strategies\\ema_cross.py --symbol MNQ --period 1m --out report.html
"""
from __future__ import annotations

import argparse
import importlib.util
import inspect
import sys
from pathlib import Path

from backtester import ApexConfig, Backtest, Strategy
from backtester import metrics, montecarlo, report


def load_strategy(path: str) -> Strategy:
    p = Path(path).resolve()
    spec = importlib.util.spec_from_file_location(p.stem, p)
    mod = importlib.util.module_from_spec(spec)
    sys.modules[p.stem] = mod
    spec.loader.exec_module(mod)
    classes = [c for _, c in inspect.getmembers(mod, inspect.isclass)
               if issubclass(c, Strategy) and c is not Strategy
               and c.__module__ == mod.__name__]
    if not classes:
        raise SystemExit(f"No Strategy subclass found in {p}")
    if len(classes) > 1:
        print(f"Multiple strategies in {p.name}; using {classes[0].__name__}")
    return classes[0]()


def main() -> None:
    ap = argparse.ArgumentParser(description="Tick-level futures backtester")
    ap.add_argument("strategy", help="path to a .py file with a Strategy subclass")
    ap.add_argument("--symbol", help="override strategy symbol (e.g. MNQ)")
    ap.add_argument("--period", help="override bar period (e.g. 30s, 1m, 5m)")
    ap.add_argument("--start", help="first day, YYYY-MM-DD or YYYYMMDD")
    ap.add_argument("--end", help="last day, YYYY-MM-DD or YYYYMMDD")
    ap.add_argument("--balance", type=float, default=50_000.0,
                    help="starting balance (default 50000)")
    ap.add_argument("--apex-threshold", type=float, default=2500.0,
                    help="Apex trailing threshold in $ (default 2500; 0 disables)")
    ap.add_argument("--apex-halt", action="store_true",
                    help="stop the backtest when the threshold is breached")
    ap.add_argument("--slippage", type=float, default=0.0,
                    help="extra slippage in ticks on market/stop fills")
    ap.add_argument("--mc", type=int, default=2000, metavar="N",
                    help="Monte Carlo simulations (default 2000; 0 disables)")
    ap.add_argument("--mc-target", type=float, default=None, metavar="$",
                    help="eval profit target for P(pass before breach), "
                         "e.g. 3000 for a 50K Apex eval")
    ap.add_argument("--out", default=None,
                    help="tearsheet path (default reports\\<strategy>_<symbol>.html)")
    ap.add_argument("--no-report", action="store_true", help="console output only")
    ap.add_argument("--data-root", default=None)
    args = ap.parse_args()

    strat = load_strategy(args.strategy)
    apex = None
    if args.apex_threshold > 0:
        apex = ApexConfig(threshold=args.apex_threshold,
                          halt_on_breach=args.apex_halt)

    bt = Backtest(strat, start=args.start, end=args.end, symbol=args.symbol,
                  period=args.period, start_balance=args.balance, apex=apex,
                  slippage_ticks=args.slippage, data_root=args.data_root)
    print(f"Running {type(strat).__name__} on {bt.symbol}, {bt.period_s}s bars ...")
    result = bt.run()
    stats = metrics.compute(result)
    print(metrics.format_console(result, stats))

    mc = None
    if args.mc > 0 and len(result.trades) >= 2:
        mc = montecarlo.run(
            result.trades, start_balance=args.balance, n_sims=args.mc,
            apex_threshold=args.apex_threshold if args.apex_threshold > 0 else None,
            profit_target=args.mc_target)
        print()
        print(montecarlo.format_console(mc))

    if not args.no_report:
        out = Path(args.out or (Path("reports")
                                / f"{result.strategy_name}_{result.symbol}.html"))
        path = report.render(result, stats, out, mc=mc)
        csv_path = report.write_trades_csv(
            result, out.with_name(out.stem + "_trades.csv"))
        print(f"\nTearsheet: {path.resolve()}")
        print(f"Trade log: {csv_path.resolve()}")


if __name__ == "__main__":
    main()
