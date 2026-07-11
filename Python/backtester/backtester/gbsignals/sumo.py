"""gbSumoPullback port — multi-MA fair-value pullback signals.

Source: nt8 code/GodZillaKilla/indicators/gbSumoPullback.cs (OnBarUpdate
668-767). Signal_Trade codes: +1 bullish sumo / -1 bearish sumo.

A signal fires when a counter-then-with-trend candle pair straddles the whole
four-MA stack (bar low below the lowest MA, bar high above the highest) and
the slow MA is the stack's extreme (lowest -> bullish, highest -> bearish),
debounced by the two-stage split machine (first fire, then re-fires gated at
SignalSplitFirst / SignalSplitSecond bars; a quiet gap resets it).

GetMASmoothed in the C# ignores its smoothing arguments (dead code) — same
here. MA types beyond SMA/EMA/WilderMA raise NotImplementedError.
"""
from __future__ import annotations

from .nt8math import Nt8Ema, Nt8Sma, approx_compare as _ac


def _make_ma(ma_type: str, period: int):
    t = ma_type.upper()
    if t == "SMA":
        return Nt8Sma(period)
    if t == "EMA":
        return Nt8Ema(period)
    if t == "WILDERMA":
        return Nt8Ema(2 * period - 1)
    raise NotImplementedError(f"SumoPullback MA type {ma_type!r} not ported "
                              f"(SMA/EMA/WilderMA only)")


class SumoPullback:
    def __init__(self, slow_ma_type: str = "SMA", slow_ma_period: int = 60,
                 fast_ma1_type: str = "EMA", fast_ma1_period: int = 14,
                 fast_ma2_type: str = "EMA", fast_ma2_period: int = 30,
                 fast_ma3_type: str = "EMA", fast_ma3_period: int = 45,
                 signal_split_first: int = 15, signal_split_second: int = 30):
        self._slow = _make_ma(slow_ma_type, slow_ma_period)
        self._f1 = _make_ma(fast_ma1_type, fast_ma1_period)
        self._f2 = _make_ma(fast_ma2_type, fast_ma2_period)
        self._f3 = _make_ma(fast_ma3_type, fast_ma3_period)
        self.split_first = signal_split_first
        self.split_second = signal_split_second

        self.n = -1
        self.fair_value = float("nan")
        self._count = 0                    # countSignalBars
        self._next = -1                    # nextBar
        self._o1 = self._c1 = float("nan")

    def update(self, o: float, h: float, l: float, c: float) -> int:
        self.n += 1
        slow = self._slow.update(c)
        f1, f2, f3 = self._f1.update(c), self._f2.update(c), self._f3.update(c)
        if self.n == 0:
            self._o1, self._c1 = o, c
            return 0
        mas = (slow, f1, f2, f3)
        mx, mn = max(mas), min(mas)
        code = 0
        straddle = _ac(mn, l) > 0 and _ac(mx, h) < 0
        if _ac(c, o) <= 0 or _ac(self._c1, self._o1) >= 0:
            # bearish: red candle after green, slow MA on top of the stack
            if (_ac(c, o) < 0 and _ac(self._c1, self._o1) > 0
                    and straddle and _ac(slow, mx) == 0):
                code = self._fire(-1)
        elif straddle and _ac(slow, mn) == 0:
            # bullish: green candle after red, slow MA at the bottom
            code = self._fire(1)
        if self.n > self._next and self._count != 0:
            self._count = 0
            self._next = -1
        self.fair_value = (slow + f1 + f2 + f3) / 4.0
        self._o1, self._c1 = o, c
        return code

    def _fire(self, direction: int) -> int:
        if self._count == 0 and self._next < 0:
            self._count = 1
            self._next = self.n + self.split_first
            return direction
        if self._count == 1:
            if self.n >= self._next:
                self._count = 2
                self._next = self.n + self.split_second
                return direction
            return 0
        if self._count == 2 and self.n > self._next:
            return direction
        return 0
