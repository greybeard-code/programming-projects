"""Monte Carlo trade resampling (Davey BWATS / Masters SSML).

Resamples the closed-trade P&L sequence with replacement and rebuilds equity
paths to answer: how much of the backtest outcome is the luck of trade
ordering? Outputs the distribution of final P&L and max drawdown, and — the
prop-firm question — the probability of breaching a prop-firm (Apex-style) trailing
threshold across orderings, optionally racing a profit target (eval pass).

Prop-firm breach detection uses each trade's recorded MFE/MAE (dollars), so
intratrade excursions count, matching how the threshold trails live. The true
intra-trade ordering of MFE vs MAE is unknowable after resampling, so the
model assumes the adverse excursion comes first (the usual shape): a trade's
MAE is tested against the floor trailed from *prior* trades, and its close is
additionally tested against the floor raised by the trade's own MFE (catching
peaked-then-collapsed trades to the extent of their close).

Serial correlation: if the trade P&L autocorrelation exceeds |r| > 0.2 at any
lag 1..10, plain resampling is invalid (Davey) and a circular block bootstrap
is used instead. `method="auto"` (default) picks this automatically.
"""
from __future__ import annotations

from dataclasses import dataclass

import numpy as np


@dataclass
class MonteCarloResult:
    n_sims: int
    n_trades: int
    method: str                      # "iid" or "block"
    max_autocorr: float
    block_size: int | None
    # final P&L distribution ($):
    pnl_p5: float
    pnl_median: float
    pnl_p95: float
    prob_profitable: float
    # max drawdown distribution ($, negative):
    dd_median: float
    dd_p95: float                    # 95th percentile *worst* drawdown
    # prop-firm trailing threshold:
    prop_threshold: float | None = None
    prob_breach: float | None = None
    # eval race (profit target vs breach), if a target was given:
    profit_target: float | None = None
    prob_pass: float | None = None   # target reached before any breach
    prob_fail: float | None = None   # breached before target
    # unresolved = sequence ended without target or breach


def trade_autocorr(pnl: np.ndarray, max_lag: int = 10) -> float:
    """Max |autocorrelation| of the trade P&L sequence over lags 1..max_lag."""
    n = len(pnl)
    if n < 10:
        return 0.0
    x = pnl - pnl.mean()
    denom = float(x @ x)
    if denom == 0:
        return 0.0
    best = 0.0
    for k in range(1, min(max_lag, n - 2) + 1):
        r = float(x[:-k] @ x[k:]) / denom
        best = max(best, abs(r))
    return best


def _resample_iid(rng, n_trades: int, n_sims: int) -> np.ndarray:
    return rng.integers(0, n_trades, size=(n_sims, n_trades))


def _resample_block(rng, n_trades: int, n_sims: int,
                    block: int) -> np.ndarray:
    """Circular block bootstrap: sample random start offsets, take
    consecutive runs of `block` trades (wrapping), until n_trades drawn."""
    n_blocks = int(np.ceil(n_trades / block))
    starts = rng.integers(0, n_trades, size=(n_sims, n_blocks))
    offsets = np.arange(block)
    idx = (starts[:, :, None] + offsets[None, None, :]) % n_trades
    return idx.reshape(n_sims, n_blocks * block)[:, :n_trades]


def run(trades, start_balance: float, n_sims: int = 2000,
        prop_threshold: float | None = None, prop_lock_buffer: float = 100.0,
        prop_lock: bool = True, profit_target: float | None = None,
        method: str = "auto", seed: int | None = 7,
        block_size: int | None = None) -> MonteCarloResult:
    """trades: list of account.Trade (needs .pnl, .mae, .mfe)."""
    pnl = np.array([t.pnl for t in trades], dtype="float64")
    mae = np.array([min(t.mae, min(t.pnl, 0.0)) for t in trades])
    mfe = np.array([max(t.mfe, max(t.pnl, 0.0)) for t in trades])
    n = len(pnl)
    if n < 2:
        raise ValueError("Monte Carlo needs at least 2 closed trades")

    rng = np.random.default_rng(seed)
    ac = trade_autocorr(pnl)
    use_block = method == "block" or (method == "auto" and ac > 0.2)
    if use_block:
        blk = block_size or max(2, int(round(np.sqrt(n))))
        idx = _resample_block(rng, n, n_sims, blk)
        method_used = "block"
    else:
        blk = None
        idx = _resample_iid(rng, n, n_sims)
        method_used = "iid"

    sim_pnl = pnl[idx]                                   # (sims, n)
    equity_after = np.cumsum(sim_pnl, axis=1)            # net P&L after trade k
    equity_before = equity_after - sim_pnl               # net P&L entering trade k
    final = equity_after[:, -1]

    # max drawdown: each trade's trough vs the peak reached *before* it
    # (a winner's own MFE must not count as a peak preceding its own dip)
    trough = equity_before + mae[idx]                    # worst point in trade k
    peak_incl = np.maximum(equity_before + mfe[idx], equity_after)
    peak_run = np.maximum.accumulate(peak_incl, axis=1)
    peak_prior = np.concatenate(
        [np.zeros((n_sims, 1)), peak_run[:, :-1]], axis=1)
    peak_prior = np.maximum(peak_prior, 0.0)             # start equity is a peak
    dd = np.minimum((trough - peak_prior).min(axis=1), 0.0)

    r = MonteCarloResult(
        n_sims=n_sims, n_trades=n, method=method_used, max_autocorr=ac,
        block_size=blk,
        pnl_p5=float(np.percentile(final, 5)),
        pnl_median=float(np.percentile(final, 50)),
        pnl_p95=float(np.percentile(final, 95)),
        prob_profitable=float((final > 0).mean()),
        dd_median=float(np.percentile(dd, 50)),
        dd_p95=float(np.percentile(dd, 5)),   # 5th pct of negatives = worst tail
    )

    if prop_threshold and prop_threshold > 0:
        # trailing floor in net-P&L-from-zero terms: MAE vs prior-trade floor,
        # close vs floor including the trade's own MFE (see module docstring)
        floor_prior = peak_prior - prop_threshold
        floor_incl = peak_run - prop_threshold
        if prop_lock:
            floor_prior = np.minimum(floor_prior, prop_lock_buffer)
            floor_incl = np.minimum(floor_incl, prop_lock_buffer)
        breach_m = (trough <= floor_prior) | (equity_after <= floor_incl)
        breached = breach_m.any(axis=1)
        r.prop_threshold = prop_threshold
        r.prob_breach = float(breached.mean())

        if profit_target and profit_target > 0:
            hit_m = equity_after >= profit_target
            first_breach = np.where(breached, breach_m.argmax(axis=1), n + 1)
            hit_any = hit_m.any(axis=1)
            first_hit = np.where(hit_any, hit_m.argmax(axis=1), n + 1)
            r.profit_target = profit_target
            # tie (target and breach in the same trade) counts as a breach
            r.prob_pass = float((first_hit < first_breach).mean())
            r.prob_fail = float(((first_breach <= first_hit) & breached).mean())
    return r


def format_console(r: MonteCarloResult) -> str:
    lines = [
        f"Monte Carlo:    {r.n_sims} sims of {r.n_trades} trades "
        f"({r.method} resampling"
        + (f", block={r.block_size}" if r.block_size else "")
        + f", max autocorr {r.max_autocorr:.2f})",
        f"  final P&L:    5% ${r.pnl_p5:,.0f}   median ${r.pnl_median:,.0f}   "
        f"95% ${r.pnl_p95:,.0f}   P(profit) {r.prob_profitable * 100:.0f}%",
        f"  max drawdown: median ${r.dd_median:,.0f}   "
        f"5%-worst ${r.dd_p95:,.0f}",
    ]
    if r.prob_breach is not None:
        lines.append(
            f"  PROP:         P(breach ${r.prop_threshold:,.0f} trailing) = "
            f"{r.prob_breach * 100:.1f}%")
    if r.prob_pass is not None:
        unresolved = 1.0 - r.prob_pass - r.prob_fail
        lines.append(
            f"  EVAL:         P(hit ${r.profit_target:,.0f} before breach) = "
            f"{r.prob_pass * 100:.1f}%   P(breach first) = "
            f"{r.prob_fail * 100:.1f}%   unresolved = {unresolved * 100:.1f}%")
    return "\n".join(lines)
