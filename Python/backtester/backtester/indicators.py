"""Incremental (streaming) indicators, updated once per bar — NT8-style.

Each indicator exposes .update(...) returning the new value, .value for the
latest value, and .ready once enough bars have been seen.
"""
from __future__ import annotations

from collections import deque

import math


class EMA:
    def __init__(self, period: int):
        self.period = period
        self.k = 2.0 / (period + 1)
        self.value = math.nan
        self._seed: list[float] = []

    @property
    def ready(self) -> bool:
        return not math.isnan(self.value)

    def update(self, price: float) -> float:
        if math.isnan(self.value):
            self._seed.append(price)
            if len(self._seed) >= self.period:
                self.value = sum(self._seed) / len(self._seed)
                self._seed.clear()
        else:
            self.value += self.k * (price - self.value)
        return self.value


class SMA:
    def __init__(self, period: int):
        self.period = period
        self._win: deque[float] = deque(maxlen=period)
        self._sum = 0.0
        self.value = math.nan

    @property
    def ready(self) -> bool:
        return len(self._win) == self.period

    def update(self, price: float) -> float:
        if len(self._win) == self.period:
            self._sum -= self._win[0]
        self._win.append(price)
        self._sum += price
        if self.ready:
            self.value = self._sum / self.period
        return self.value


class ATR:
    """Wilder's ATR. update(high, low, close) once per bar."""

    def __init__(self, period: int):
        self.period = period
        self.value = math.nan
        self._prev_close = math.nan
        self._seed: list[float] = []

    @property
    def ready(self) -> bool:
        return not math.isnan(self.value)

    def update(self, high: float, low: float, close: float) -> float:
        if math.isnan(self._prev_close):
            tr = high - low
        else:
            tr = max(high - low, abs(high - self._prev_close),
                     abs(low - self._prev_close))
        self._prev_close = close
        if math.isnan(self.value):
            self._seed.append(tr)
            if len(self._seed) >= self.period:
                self.value = sum(self._seed) / len(self._seed)
                self._seed.clear()
        else:
            self.value += (tr - self.value) / self.period
        return self.value


class RSI:
    """Wilder's RSI. update(close) once per bar."""

    def __init__(self, period: int):
        self.period = period
        self.value = math.nan
        self._prev = math.nan
        self._avg_gain = math.nan
        self._avg_loss = math.nan
        self._seed_g: list[float] = []
        self._seed_l: list[float] = []

    @property
    def ready(self) -> bool:
        return not math.isnan(self.value)

    def update(self, close: float) -> float:
        if math.isnan(self._prev):
            self._prev = close
            return self.value
        chg = close - self._prev
        self._prev = close
        gain, loss = max(chg, 0.0), max(-chg, 0.0)
        if math.isnan(self._avg_gain):
            self._seed_g.append(gain)
            self._seed_l.append(loss)
            if len(self._seed_g) >= self.period:
                self._avg_gain = sum(self._seed_g) / self.period
                self._avg_loss = sum(self._seed_l) / self.period
                self._seed_g.clear()
                self._seed_l.clear()
            else:
                return self.value
        else:
            self._avg_gain += (gain - self._avg_gain) / self.period
            self._avg_loss += (loss - self._avg_loss) / self.period
        if self._avg_loss == 0:
            self.value = 100.0
        else:
            self.value = 100.0 - 100.0 / (1.0 + self._avg_gain / self._avg_loss)
        return self.value


class EfficiencyRatio:
    """Kaufman's Efficiency Ratio: |net change| / sum of |bar changes|.

    0 = pure chop, 1 = perfect trend. TSM guidance: < 0.12 suppress entries
    (chop), > 0.4 trending (tighten trails). update(close) once per bar.
    """

    def __init__(self, period: int = 20):
        self.period = period
        self._win: deque[float] = deque(maxlen=period + 1)
        self.value = math.nan

    @property
    def ready(self) -> bool:
        return len(self._win) == self.period + 1

    def update(self, close: float) -> float:
        self._win.append(close)
        if self.ready:
            w = list(self._win)
            noise = sum(abs(w[i] - w[i - 1]) for i in range(1, len(w)))
            self.value = abs(w[-1] - w[0]) / noise if noise > 0 else 0.0
        return self.value


class Highest:
    def __init__(self, period: int):
        self.period = period
        self._win: deque[float] = deque(maxlen=period)
        self.value = math.nan

    @property
    def ready(self) -> bool:
        return len(self._win) == self.period

    def update(self, price: float) -> float:
        self._win.append(price)
        self.value = max(self._win)
        return self.value


class Lowest:
    def __init__(self, period: int):
        self.period = period
        self._win: deque[float] = deque(maxlen=period)
        self.value = math.nan

    @property
    def ready(self) -> bool:
        return len(self._win) == self.period

    def update(self, price: float) -> float:
        self._win.append(price)
        self.value = min(self._win)
        return self.value
