"""Confluence sweep: which signal engines, how many must agree, which are
required — the GodZillaKilla research questions, as one command.

Enumerates indicator subsets x required-count x require-flag combos (pruned:
count <= subset size; require flags only over enabled sources), runs each on
the multiprocess pool, and emits a ranked league table + CSV. The base
configuration (operators, windows, ATM exits, daily limits, bar series)
comes from a saved NT8 strategy template so the sweep varies ONLY the
confluence knobs.
"""
from __future__ import annotations

import importlib.util
import itertools
import os
from concurrent.futures import ProcessPoolExecutor
from pathlib import Path

from . import metrics
from .account import PropFirmConfig
from .engine import Backtest
from .sweep import RESULT_FIELDS, rank, write_csv

SOURCES = ("ko", "pa", "th", "sj", "su", "nc")


def _load_gzk(strategy_path: str, template: str | None):
    spec = importlib.util.spec_from_file_location("gzk_sweep", strategy_path)
    mod = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(mod)
    cls = mod.GodZillaKilla
    return cls.from_template(template) if template else cls()


def _run_one(payload: dict) -> dict:
    """Worker (module-level for Windows spawn)."""
    strat = _load_gzk(payload["strategy_path"], payload["template"])
    if payload.get("symbol"):
        strat.symbol = payload["symbol"]     # explicit override wins; some
        # saved templates have an empty InstrumentOrInstrumentList, in which
        # case from_template() can't infer it and the class default (MNQ)
        # would otherwise silently apply.
    for k, v in payload["overrides"].items():
        setattr(strat, k, v)
    prop = (PropFirmConfig(threshold=payload["prop_threshold"])
            if payload["prop_threshold"] else None)
    bt = Backtest(strat, start=payload["start"], end=payload["end"],
                  start_balance=payload["start_balance"], prop=prop,
                  slippage_ticks=payload["slippage_ticks"], progress=False)
    stats = metrics.compute(bt.run())
    row = {"combo": payload["label"]}
    row.update(payload["overrides"])
    for f in RESULT_FIELDS:
        row[f] = stats.get(f, float("nan"))
    return row


def enumerate_combos(sources=SOURCES, counts: str | list[int] = "all",
                     requires: str = "none",
                     min_size: int = 1) -> list[tuple[str, dict]]:
    """(label, overrides) per combo.

    counts: "all" (1..len(subset)) or explicit list of required counts.
    requires: "none" | "single" (each enabled source required in turn)
              | "all" (full require-flag powerset — explodes, use with care).
    """
    sources = [s.lower() for s in sources]
    out: list[tuple[str, dict]] = []
    for r in range(max(1, min_size), len(sources) + 1):
        for subset in itertools.combinations(sources, r):
            base = {f"use_{s}": (s in subset) for s in SOURCES}
            base.update({f"require_{s}": False for s in SOURCES})
            base["set2_enabled"] = False
            cnts = (range(1, len(subset) + 1) if counts == "all"
                    else [c for c in counts if c <= len(subset)])
            for c in cnts:
                names = "+".join(s.upper() for s in subset)
                req_sets: list[tuple[str, ...]] = [()]
                if requires == "single":
                    req_sets += [(s,) for s in subset]
                elif requires == "all":
                    req_sets = [rs for n in range(len(subset) + 1)
                                for rs in itertools.combinations(subset, n)]
                for rs in req_sets:
                    ov = dict(base)
                    ov["set1_required"] = c
                    for s in rs:
                        ov[f"require_{s}"] = True
                    label = f"{c}-of-{len(subset)} {names}"
                    if rs:
                        label += " req:" + "+".join(s.upper() for s in rs)
                    out.append((label, ov))
    return out


def run_confluence(strategy_path: str, template: str | None = None,
                   symbol: str | None = None,
                   sources=SOURCES, counts="all", requires: str = "none",
                   min_size: int = 1,
                   start: str | None = None, end: str | None = None,
                   start_balance: float = 50_000.0,
                   prop_threshold: float | None = 2000.0,
                   slippage_ticks: float = 0.0,
                   workers: int | None = None, _runner=None) -> list[dict]:
    combos = enumerate_combos(sources, counts, requires, min_size)
    payloads = [{
        "strategy_path": str(Path(strategy_path).resolve()),
        "template": str(Path(template).resolve()) if template else None,
        "symbol": symbol,
        "label": label, "overrides": ov, "start": start, "end": end,
        "start_balance": start_balance, "prop_threshold": prop_threshold,
        "slippage_ticks": slippage_ticks,
    } for label, ov in combos]
    print(f"confluence sweep: {len(payloads)} combos "
          f"({len(list(sources))} sources, counts={counts}, "
          f"requires={requires})")

    runner = _runner or _run_one
    if _runner is not None or len(payloads) == 1:
        return [runner(p) for p in payloads]
    workers = workers or min(len(payloads), os.cpu_count() or 4)
    rows = []
    with ProcessPoolExecutor(max_workers=workers) as ex:
        for i, row in enumerate(ex.map(_run_one, payloads)):
            rows.append(row)
            print(f"  {i + 1}/{len(payloads)} {row['combo']:<40} "
                  f"net {row['net_pnl']:>10,.0f}  sharpe {row['sharpe']:5.2f}"
                  f"  trades {row['total_trades']:4d}")
    return rows


def format_league(rows: list[dict], metric: str = "sharpe",
                  top: int = 25, min_trades: int = 10) -> str:
    ranked = rank(rows, metric, min_trades)
    lines = [f"\nConfluence league table (by {metric}, min {min_trades} "
             f"trades; {len(rows)} combos):",
             f"  {'combo':<42} {'net':>10} {'sharpe':>7} {'PF':>6} "
             f"{'WR%':>6} {'maxDD':>9} {'trades':>7}"]
    for r in ranked[:top]:
        lines.append(
            f"  {r['combo']:<42} {r['net_pnl']:>10,.0f} {r['sharpe']:>7.2f} "
            f"{r['profit_factor']:>6.2f} {r['win_rate']:>6.1f} "
            f"{r['max_drawdown']:>9,.0f} {r['total_trades']:>7d}")
    return "\n".join(lines)
