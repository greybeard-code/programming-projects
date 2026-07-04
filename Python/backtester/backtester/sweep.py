"""Parameter sweep with sensitivity analysis.

Runs a strategy across the cartesian product of parameter values (in
parallel), ranks results, and — per Chan's guard against data-snooping —
reports each parameter's performance plateau around the best combo: if a
±1-step neighbor collapses, the parameter is fragile and the "optimum" is
likely curve-fit.
"""
from __future__ import annotations

import csv
import itertools
import os
from concurrent.futures import ProcessPoolExecutor
from pathlib import Path

from . import metrics
from .account import ApexConfig
from .engine import Backtest
from .loader import load_strategy

RESULT_FIELDS = ("net_pnl", "sharpe", "calmar", "profit_factor", "win_rate",
                 "max_drawdown", "total_trades", "apex_min_headroom")


def _run_one(payload: dict) -> dict:
    """Worker: one backtest for one parameter combo (module-level for spawn)."""
    strat = load_strategy(payload["strategy_path"], payload["overrides"])
    apex = (ApexConfig(threshold=payload["apex_threshold"])
            if payload["apex_threshold"] else None)
    bt = Backtest(strat,
                  start=payload["start"], end=payload["end"],
                  symbol=payload["symbol"], period=payload["period"],
                  start_balance=payload["start_balance"], apex=apex,
                  slippage_ticks=payload["slippage_ticks"],
                  daily_loss_limit=payload["daily_loss_limit"],
                  progress=False)
    stats = metrics.compute(bt.run())
    row = dict(payload["overrides"])
    for f in RESULT_FIELDS:
        row[f] = stats.get(f, float("nan"))
    return row


def expand_grid(param_grid: dict[str, list]) -> list[dict]:
    keys = list(param_grid)
    return [dict(zip(keys, combo))
            for combo in itertools.product(*(param_grid[k] for k in keys))]


def run_sweep(strategy_path: str, param_grid: dict[str, list],
              start: str | None = None, end: str | None = None,
              symbol: str | None = None, period: str | None = None,
              start_balance: float = 50_000.0,
              apex_threshold: float | None = 2500.0,
              slippage_ticks: float = 0.0,
              daily_loss_limit: float | None = None,
              workers: int | None = None,
              _runner=None) -> list[dict]:
    """Returns one row per combo: {param values..., stats...} (unsorted)."""
    load_strategy(strategy_path, {k: v[0] for k, v in param_grid.items()})
    combos = expand_grid(param_grid)
    payloads = [{
        "strategy_path": str(Path(strategy_path).resolve()),
        "overrides": c, "start": start, "end": end, "symbol": symbol,
        "period": period, "start_balance": start_balance,
        "apex_threshold": apex_threshold, "slippage_ticks": slippage_ticks,
        "daily_loss_limit": daily_loss_limit,
    } for c in combos]

    runner = _runner or _run_one
    if _runner is not None or len(payloads) == 1:
        return [runner(p) for p in payloads]
    workers = workers or min(len(payloads), os.cpu_count() or 4)
    rows = []
    with ProcessPoolExecutor(max_workers=workers) as ex:
        for i, row in enumerate(ex.map(_run_one, payloads)):
            rows.append(row)
            print(f"  sweep {i + 1}/{len(payloads)}: "
                  + ", ".join(f"{k}={v}" for k, v in combos[i].items())
                  + f" -> net {row['net_pnl']:,.0f}, sharpe {row['sharpe']:.2f}")
    return rows


def rank(rows: list[dict], metric: str = "sharpe",
         min_trades: int = 10) -> list[dict]:
    """Sort by metric desc; combos with too few trades sink to the bottom."""
    def sortkey(r):
        v = r.get(metric, float("nan"))
        eligible = r.get("total_trades", 0) >= min_trades
        return (eligible, v == v, v if v == v else float("-inf"))
    return sorted(rows, key=sortkey, reverse=True)


def sensitivity(rows: list[dict], best: dict, param_grid: dict[str, list],
                metric: str = "sharpe") -> dict[str, list[tuple]]:
    """For each param: metric across its values, others held at the best
    combo. Returns {param: [(value, metric, is_best), ...]}."""
    out: dict[str, list[tuple]] = {}
    for p, values in param_grid.items():
        if len(values) < 2:
            continue
        line = []
        for v in values:
            match = next(
                (r for r in rows
                 if r[p] == v and all(r[q] == best[q] for q in param_grid
                                      if q != p)), None)
            if match:
                line.append((v, match.get(metric, float("nan")),
                             v == best[p]))
        out[p] = line
    return out


def format_sensitivity(sens: dict[str, list[tuple]], metric: str) -> str:
    lines = [f"\nSensitivity ({metric} across each parameter, others at best):"]
    for p, line in sens.items():
        vals = "   ".join(
            f"[{v}: {m:.2f}]" if isbest else f"{v}: {m:.2f}"
            for v, m, isbest in line)
        finite = [m for _, m, _ in line if m == m]
        fragile = ""
        if len(finite) >= 2:
            best_m = max(finite)
            worst_m = min(finite)
            if best_m > 0 and worst_m < 0.5 * best_m:
                fragile = "   <-- FRAGILE: no plateau, likely curve-fit"
        lines.append(f"  {p:<20} {vals}{fragile}")
    lines.append("  (a robust edge shows a plateau; a spike at one value "
                 "is data-snooping)")
    return "\n".join(lines)


def write_csv(rows: list[dict], path: str | Path) -> Path:
    path = Path(path)
    path.parent.mkdir(parents=True, exist_ok=True)
    if rows:
        with open(path, "w", newline="", encoding="utf-8") as f:
            w = csv.DictWriter(f, fieldnames=list(rows[0].keys()))
            w.writeheader()
            w.writerows(rows)
    return path
