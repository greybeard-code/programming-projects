"""gbPANAKanal port — Keltner-style channel with break & pullback signals.

Source: nt8 code/GodZillaKilla/indicators/gbPANAKanal.cs (v1.1.1),
OnBarClose semantics (OnBarUpdate lines 737-1041, CheckBreakoutAndPullback-
Signal 1075-1221, GetStateKeltner 1526-1542, ComputeKeltner 1342-1347).

Signal_Trade codes: +1/-1 trend start (channel flip), +2/-2 break
(close crosses the middle band out of the pullback zone), +3/-3 pullback
(counter-trend candle pair touching the 61.8% fib of the swing arm).

NT8 Series quirks reproduced deliberately:
- bar 0 leaves diff-high-low unset (0.0) and Extremum unset (0.0); the SMA
  window includes that zero and bar 1 always flips to uptrend on positive
  prices (close > down-line 0).
- the middle band is an EMA(2) of an EMA(MiddlePeriod) of closes, rounded to
  tick size; the Wilder MA of the modified true range is seeded with 0.
- trend decisions compare the close against the *previous* bar's channel
  lines; the trailing stop / fibs use the *current* lines.
"""
from __future__ import annotations

from .nt8math import Nt8Ema, Nt8Sma, approx_compare as _ac, round_to_tick

FIB1 = 0.618           # swingArmFibonacciLevel1
FIB2 = 0.786           # swingArmFibonacciLevel2 (rendering only)


class PanaKanal:
    def __init__(self, period: int = 20, factor: float = 4.0,
                 middle_period: int = 14, signal_break_split: int = 20,
                 signal_pullback_finding_period: int = 10,
                 tick_size: float = 0.25):
        self.period = period
        self.factor = factor
        self.break_split = signal_break_split
        self.pullback_period = signal_pullback_finding_period
        self.tick = tick_size

        self._ema_sk = Nt8Ema(middle_period)     # EMA(Input, MiddlePeriod)
        self._ema_mid = Nt8Ema(2)                # EMA(seriesSK, 2)
        self._sma_diff = Nt8Sma(period)          # SMA(high-low, Period)

        self.n = -1                              # CurrentBar
        self.is_uptrend = False
        self.trend = 0                           # Signal_Trend (+1/-1)
        self.middle = float("nan")
        self.trailing_stop = float("nan")

        self._wild = 0.0                         # seriesWildMA[1]
        self._up = 0.0                           # seriesUp[1]
        self._down = 0.0                         # seriesDown[1]
        self._extremum = 0.0                     # Extremum[1]
        self._o1 = self._h1 = self._l1 = self._c1 = float("nan")

        self._state = 0                          # signalStateKeltner
        self._prev_state = 0
        # pullback bookkeeping
        self._has_pb = False
        self._reset_pb = False
        self._pb_index = 0
        # break bookkeeping
        self._has_break = False
        self._break_up_idx = 0
        self._break_down_idx = 0
        self._check_zone = True                  # isCheckPriceInZone
        self._in_zone = False                    # isPriceInZone (latched)
        self._reset_break = False                # isResetConditionBreak

    # ------------------------------------------------------------------
    def update(self, o: float, h: float, l: float, c: float) -> int:
        """Advance one closed bar; returns the Signal_Trade code."""
        self.n += 1
        # ComputeKeltner
        sk = self._ema_sk.update(c)
        self.middle = round_to_tick(self._ema_mid.update(sk), self.tick)
        # Keltner state (close vs middle, hold previous on exact equality)
        self._prev_state = self._state
        self._state = self._get_state(self._prev_state, c)

        if self.n == 0:
            self.is_uptrend = False
            self.trend = -1
            self._sma_diff.update(0.0)           # series default at bar 0
            self._remember(o, h, l, c)
            return 0

        h1, l1, c1, o1 = self._h1, self._l1, self._c1, self._o1
        # modified true range -> Wilder MA -> channel lines
        diff = h - l
        val = min(diff, 1.5 * self._sma_diff.update(diff))
        val2 = (h - c1 - 0.5 * (l - h1)) if _ac(l, h1) > 0 else (h - c1)
        val3 = (c1 - l - 0.5 * (l1 - h)) if _ac(h, l1) < 0 else (c1 - l)
        tr = max(val, val2, val3)
        self._wild = self._wild + (tr - self._wild) / self.period
        band = self.factor * self._wild
        up_c, dn_c = c - band, c + band
        up1, dn1 = self._up, self._down          # previous-bar lines
        up = up_c if _ac(c1, up1) <= 0 else max(up_c, up1)
        down = dn_c if _ac(c1, dn1) >= 0 else min(dn_c, dn1)
        self._up, self._down = up, down

        # first-tick latches
        self._in_zone = (not self._reset_break) and self._check_zone
        break_idx = self._break_up_idx if self.is_uptrend else self._break_down_idx
        split_ok = (not self._has_break
                    or self.n - (break_idx - 1) >= self.break_split)
        if not self._has_pb and self._reset_pb:
            self._has_pb = True
            self._reset_pb = False
        ext1 = self._extremum

        code = 0
        if not self.is_uptrend:
            if _ac(c, dn1) <= 0:                 # downtrend continues
                ext = min(ext1, l)
                fib1 = self._fibs(ext)
                code = self._check_break_pullback(o, h, l, c, o1, c1,
                                                  fib1, split_ok)
            else:                                # flip to uptrend
                self.is_uptrend = True
                code = 1
                self._on_flip(ext := h)
                self._fibs(ext)
        else:
            if _ac(c, up1) >= 0:                 # uptrend continues
                ext = max(ext1, h)
                fib1 = self._fibs(ext)
                code = self._check_break_pullback(o, h, l, c, o1, c1,
                                                  fib1, split_ok)
            else:                                # flip to downtrend
                self.is_uptrend = False
                code = -1
                self._on_flip(ext := l)
                self._fibs(ext)

        self._extremum = ext
        self.trend = 1 if self.is_uptrend else -1
        self._remember(o, h, l, c)
        return code

    # ------------------------------------------------------------------
    def _get_state(self, prev: int, close: float) -> int:
        cmp = _ac(close, self.middle)
        if cmp > 0:
            return 1
        if cmp < 0:
            return -1
        return 1 if prev == 1 else -1

    def _fibs(self, extremum: float) -> float:
        """Trailing stop + 61.8% fib of the swing arm; returns fib1."""
        ts = self._up if self.is_uptrend else self._down
        self.trailing_stop = ts
        return extremum + (ts - extremum) * FIB1

    def _on_flip(self, extremum: float) -> None:
        self._check_zone = True
        self._reset_break = False
        self._has_pb = False
        self._pb_index = 0
        self._has_break = False
        if self.is_uptrend:
            self._break_up_idx = self.n
        else:
            self._break_down_idx = self.n

    def _check_break_pullback(self, o, h, l, c, o1, c1, fib1,
                              split_ok: bool) -> int:
        # zone tracking for the NEXT bar (only refreshed while out of zone)
        if not self._in_zone:
            if self.is_uptrend:
                self._check_zone = _ac(c, fib1) < 0
            else:
                self._check_zone = _ac(c, fib1) > 0
        code = 0
        # pullback: counter-trend candle followed by with-trend candle
        # touching the fib, within the finding window after a flip
        if not self._has_pb:
            self._pb_index += 1
            if self._pb_index < self.pullback_period:
                cur, prev = _ac(c, o), _ac(c1, o1)
                pattern = ((prev < 0 and cur > 0) if self.is_uptrend
                           else (prev > 0 and cur < 0))
                touch = (_ac(l, fib1) <= 0 if self.is_uptrend
                         else _ac(h, fib1) >= 0)
                if pattern and touch:
                    code = 3 if self.is_uptrend else -3
                    self._reset_pb = True
        # break: middle-band state turns with-trend while price sits in the
        # pullback zone (skipped on a bar that just fired a pullback)
        if not self._reset_pb:
            dstate = self._state - self._prev_state
            ok = (self._in_zone and split_ok
                  and (dstate > 0 if self.is_uptrend else dstate < 0))
            if not ok:
                self._reset_break = False
            else:
                code = 2 if self.is_uptrend else -2
                if self.is_uptrend:
                    self._break_up_idx = self.n
                else:
                    self._break_down_idx = self.n
                self._has_break = True
                self._reset_break = True
        return code

    def _remember(self, o, h, l, c) -> None:
        self._o1, self._h1, self._l1, self._c1 = o, h, l, c
