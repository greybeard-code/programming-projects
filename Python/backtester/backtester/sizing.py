"""Position sizing helpers.

Carver volatility targeting (Systematic Trading): size so the position's
expected daily dollar volatility matches a target fraction of capital.

    AnnualCashVolTarget = capital * annual_vol_target      (e.g. 15%)
    DailyCashVolTarget  = AnnualCashVolTarget / 16         (~sqrt(252))
    InstrumentVolDollars = ATR * point_value               (per contract/day)
    contracts = DailyCashVolTarget / InstrumentVolDollars

Use ATR computed on *daily* ranges (or a daily-equivalent estimate) — feeding
a 1-minute ATR here will massively oversize.
"""
from __future__ import annotations


def carver_contracts(capital: float, atr_points: float, point_value: float,
                     annual_vol_target: float = 0.15,
                     min_contracts: int = 1,
                     max_contracts: int | None = None) -> int:
    """Contracts for a vol-target position. atr_points: daily ATR in points."""
    if atr_points <= 0 or capital <= 0:
        return min_contracts
    daily_cash_vol_target = capital * annual_vol_target / 16.0
    instrument_vol = atr_points * point_value
    n = int(daily_cash_vol_target / instrument_vol)
    n = max(n, min_contracts)
    if max_contracts is not None:
        n = min(n, max_contracts)
    return n
