"""NT8-exact numeric primitives for the gb signal ports.

These deliberately differ from backtester/indicators.py (which warms EMA up
with an SMA seed): NT8's EMA is seeded with the raw first value at bar 0 and
its SMA averages the partial window from bar 0 — bar-exact parity with NT8
chart exports needs those semantics.
"""
from __future__ import annotations

import math
from collections import deque

APPROX_EPS = 1e-10   # NT8 MathExtentions.ApproxCompare epsilon


def approx_compare(a: float, b: float) -> int:
    """NT8 double comparison: 0 within 1e-10, else sign of a-b."""
    if abs(a - b) < APPROX_EPS:
        return 0
    return -1 if a < b else 1


def round_to_tick(value: float, tick: float) -> float:
    """NT8 Instrument.RoundToTickSize (round-half-away-from-zero)."""
    if tick <= 0:
        return value
    return math.floor(value / tick + 0.5) * tick


class Nt8Ema:
    """NT8 EMA: value = input at bar 0, then prev + k*(input-prev)."""

    def __init__(self, period: int):
        self.k = 2.0 / (period + 1)
        self.value = math.nan

    def update(self, x: float) -> float:
        if math.isnan(self.value):
            self.value = x
        else:
            self.value += self.k * (x - self.value)
        return self.value


class Nt8Sma:
    """NT8 SMA: mean of the last min(bars-seen, period) inputs (no warmup)."""

    def __init__(self, period: int):
        self._win: deque[float] = deque(maxlen=period)
        self._sum = 0.0
        self.value = math.nan

    def update(self, x: float) -> float:
        if len(self._win) == self._win.maxlen:
            self._sum -= self._win[0]
        self._win.append(x)
        self._sum += x
        self.value = self._sum / len(self._win)
        return self.value
