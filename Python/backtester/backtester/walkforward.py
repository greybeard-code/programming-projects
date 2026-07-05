"""Walk-forward analysis (SSML / Davey BWATS).

Rolling windows over the available days: optimize the parameter grid on the
in-sample segment (IS), run the single best combo on the following out-of-
sample segment (OOS), roll forward, repeat. The stitched OOS results are the
only performance estimate that hasn't seen its own data.

Layout: OOS length = total_days // (ratio + n_windows); IS = ratio * OOS.
Window i:  IS = days[i*OOS : i*OOS + IS],  OOS = the next OOS-many days.

Verdict per Davey: if OOS performance is far below IS character, the system
is curve-fit. Walk-forward efficiency (WFE) = mean(OOS metric) / mean(IS
metric); > ~0.5 is commonly considered acceptable.
"""
from __future__ import annotations

from dataclasses import dataclass, field

import numpy as np

from . import metrics as metrics_mod
from . import sweep as sweep_mod
from .account import PropFirmConfig
from .data import Catalog
from .engine import Backtest
from .loader import load_strategy


@dataclass
class Window:
    index: int
    is_days: list[str]
    oos_days: list[str]
    best_params: dict = field(default_factory=dict)
    is_metric: float = float("nan")
    oos_metric: float = float("nan")
    oos_stats: dict = field(default_factory=dict)
    oos_daily: dict = field(default_factory=dict)


def split_windows(days: list[str], n_windows: int, ratio: int) -> list[Window]:
    oos_len = len(days) // (ratio + n_windows)
    if oos_len < 1:
        raise ValueError(
            f"Not enough days ({len(days)}) for {n_windows} windows at "
            f"{ratio}:1 — need at least {ratio + n_windows}")
    is_len = ratio * oos_len
    out = []
    for i in range(n_windows):
        a = i * oos_len
        b = a + is_len
        c = b + oos_len
        out.append(Window(index=i + 1, is_days=days[a:b], oos_days=days[b:c]))
    return out


def run_walkforward(strategy_path: str, param_grid: dict[str, list],
                    n_windows: int = 5, ratio: int = 5,
                    metric: str = "sharpe", min_trades: int = 10,
                    start: str | None = None, end: str | None = None,
                    symbol: str | None = None, period: str | None = None,
                    start_balance: float = 50_000.0,
                    prop_threshold: float | None = 2500.0,
                    slippage_ticks: float = 0.0,
                    daily_loss_limit: float | None = None,
                    workers: int | None = None,
                    data_root=None, cache_root=None) -> list[Window]:
    strat0 = load_strategy(strategy_path)
    sym = (symbol or strat0.symbol).upper()
    catalog = Catalog(data_root, cache_root)
    days = catalog.days(sym, start, end)
    windows = split_windows(days, n_windows, ratio)
    print(f"Walk-forward: {len(days)} days -> {n_windows} windows, "
          f"IS {len(windows[0].is_days)} / OOS {len(windows[0].oos_days)} "
          f"days ({ratio}:1), optimizing {metric}")

    for w in windows:
        print(f"\n--- window {w.index}: IS {w.is_days[0]}..{w.is_days[-1]}, "
              f"OOS {w.oos_days[0]}..{w.oos_days[-1]} ---")
        rows = sweep_mod.run_sweep(
            strategy_path, param_grid,
            start=w.is_days[0], end=w.is_days[-1], symbol=sym, period=period,
            start_balance=start_balance, prop_threshold=prop_threshold,
            slippage_ticks=slippage_ticks, daily_loss_limit=daily_loss_limit,
            workers=workers)
        ranked = sweep_mod.rank(rows, metric, min_trades)
        best = ranked[0]
        w.best_params = {k: best[k] for k in param_grid}
        w.is_metric = best.get(metric, float("nan"))

        strat = load_strategy(strategy_path, w.best_params)
        prop = PropFirmConfig(threshold=prop_threshold) if prop_threshold else None
        bt = Backtest(strat, start=w.oos_days[0], end=w.oos_days[-1],
                      symbol=sym, period=period, start_balance=start_balance,
                      prop=prop, slippage_ticks=slippage_ticks,
                      daily_loss_limit=daily_loss_limit, progress=False,
                      data_root=data_root, cache_root=cache_root)
        res = bt.run()
        w.oos_stats = metrics_mod.compute(res)
        w.oos_daily = res.daily_pnl
        w.oos_metric = w.oos_stats.get(metric, float("nan"))
        print(f"  best {w.best_params}: IS {metric} {w.is_metric:.2f} -> "
              f"OOS {metric} {w.oos_metric:.2f}, "
              f"OOS net ${w.oos_stats['net_pnl']:,.0f}")
    return windows


def summarize(windows: list[Window], metric: str,
              start_balance: float) -> str:
    lines = ["", "=== Walk-forward summary ===",
             f"{'win':<4}{'IS ' + metric:>10}{'OOS ' + metric:>11}"
             f"{'OOS net':>12}{'OOS trades':>12}  best params"]
    for w in windows:
        lines.append(
            f"{w.index:<4}{w.is_metric:>10.2f}{w.oos_metric:>11.2f}"
            f"{w.oos_stats['net_pnl']:>12,.0f}"
            f"{w.oos_stats['total_trades']:>12}  {w.best_params}")

    # stitched OOS: combined daily P&L across all OOS segments
    all_daily: dict[str, float] = {}
    for w in windows:
        all_daily.update(w.oos_daily)
    dvals = np.array(list(all_daily.values()))
    net = float(dvals.sum()) if len(dvals) else 0.0
    sharpe = float("nan")
    if len(dvals) > 1 and dvals.std(ddof=1) > 0:
        ret = dvals / start_balance
        sharpe = float(ret.mean() / ret.std(ddof=1) * np.sqrt(252))
    lines.append(f"\nStitched OOS: net ${net:,.0f} over {len(dvals)} days, "
                 f"Sharpe {sharpe:.2f}")

    is_vals = [w.is_metric for w in windows if w.is_metric == w.is_metric]
    oos_vals = [w.oos_metric for w in windows if w.oos_metric == w.oos_metric]
    if is_vals and oos_vals and np.mean(is_vals) > 0:
        wfe = float(np.mean(oos_vals) / np.mean(is_vals))
        lines.append(f"Walk-forward efficiency: {wfe:.2f} "
                     f"({'OK — edge survives OOS' if wfe >= 0.5 else 'POOR — likely curve-fit (Davey: discard)'})")
    pos = sum(1 for w in windows if w.oos_stats.get("net_pnl", 0) > 0)
    lines.append(f"Profitable OOS windows: {pos}/{len(windows)}")
    return "\n".join(lines)
