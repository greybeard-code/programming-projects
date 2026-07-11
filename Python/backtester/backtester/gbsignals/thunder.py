"""gbThunderZilla port — trend-strength state machine with pullback,
move-stop and OBOS-slowdown signals.

Source: nt8 code/GodZillaKilla/indicators/gbThunderZilla.cs (OnBarUpdate
962-1265, ComputeSolarWindRK 1267-1347, ComputeSumoPullback 1349-1442,
ComputeMultiOscOBOSOverlap 1444-1501), OnBarClose semantics.

Signal_Trade codes (enum lines 64-75):
  +1/-1 trend start   (solar-wind AND sumo stacks agree, trend flips)
  +2/-2 slowdown      (+2 = DowntrendSlowdown = bullish OBOS exit!)
  +3/-3 pullback      (with-trend candle after counter-trend candle at MAs)
  +4/-4 move stop     (solar-wind stop crosses the trend MA)

Sub-indicators are NT8-exact ports of the user's installed @RSI/@MFI/
@Stochastics sources, including their warmup quirks (RSI plot is 0 until
Period bars; MFI starts at 50; unset bar-0 series values enter the sums).
GetMASmoothed in the C# ignores its smoothing arguments (dead code) — same
here. Trend MA types beyond SMA/EMA/WilderMA raise NotImplementedError.
"""
from __future__ import annotations

from collections import deque

from .nt8math import Nt8Ema, Nt8Sma, approx_compare as _ac, round_to_tick


# ---------------------------------------------------------------------------
# NT8-exact sub-indicators (warmup behavior matters for parity)
# ---------------------------------------------------------------------------

class _Nt8Rsi:
    """@RSI.cs: Wilder averages seeded by SMA(Period) (bar-0 change = 0);
    the RSI plot stays 0.0 until CurrentBar+1 == Period."""

    def __init__(self, period: int):
        self.period = period
        self._sma_dn = Nt8Sma(period)
        self._sma_up = Nt8Sma(period)
        self._avg_dn = self._avg_up = 0.0
        self._prev = float("nan")
        self.n = -1
        self.value = 0.0

    def update(self, x: float) -> float:
        self.n += 1
        if self.n == 0:
            self._prev = x
            self._sma_dn.update(0.0)
            self._sma_up.update(0.0)
            return self.value                      # 0.0 — plot unset
        dn, up = max(self._prev - x, 0.0), max(x - self._prev, 0.0)
        self._prev = x
        sd, su = self._sma_dn.update(dn), self._sma_up.update(up)
        if self.n + 1 < self.period:
            return self.value                      # still 0.0
        if self.n + 1 == self.period:
            self._avg_dn, self._avg_up = sd, su
        else:
            k = self.period - 1
            self._avg_dn = (self._avg_dn * k + dn) / self.period
            self._avg_up = (self._avg_up * k + up) / self.period
        self.value = (100.0 if self._avg_dn == 0
                      else 100.0 - 100.0 / (1 + self._avg_up / self._avg_dn))
        return self.value


class _Nt8Mfi:
    """@MFI.cs: typical-price money flow, rolling SUM(Period), 50 until the
    negative sum is nonzero."""

    def __init__(self, period: int):
        self._neg: deque[float] = deque(maxlen=period)
        self._pos: deque[float] = deque(maxlen=period)
        self._typ_prev = float("nan")
        self.n = -1
        self.value = 50.0

    def update(self, h: float, l: float, c: float, vol: float) -> float:
        self.n += 1
        typ = (h + l + c) / 3.0
        if self.n == 0:
            self._typ_prev = typ
            self._neg.append(0.0)                 # unset bar-0 series values
            self._pos.append(0.0)
            self.value = 50.0
            return self.value
        self._neg.append(typ * vol if typ < self._typ_prev else 0.0)
        self._pos.append(typ * vol if typ > self._typ_prev else 0.0)
        self._typ_prev = typ
        sn, sp = sum(self._neg), sum(self._pos)
        self.value = 50.0 if sn == 0 else 100.0 - 100.0 / (1 + sp / sn)
        return self.value


class _Nt8Stochastics:
    """@Stochastics.cs: D = SMA(SMA(fastK, smooth), periodD),
    fastK over MIN(Low,periodK)/MAX(High,periodK), holds on zero range."""

    def __init__(self, period_d: int, period_k: int, smooth: int):
        self._lows: deque[float] = deque(maxlen=period_k)
        self._highs: deque[float] = deque(maxlen=period_k)
        self._sma_fast = Nt8Sma(smooth)
        self._sma_k = Nt8Sma(period_d)
        self._fast_prev = 50.0
        self.n = -1
        self.d = float("nan")

    def update(self, h: float, l: float, c: float) -> float:
        self.n += 1
        self._lows.append(l)
        self._highs.append(h)
        mn, mx = min(self._lows), max(self._highs)
        den = mx - mn
        if _ac(den, 0.0) == 0:
            fast = 50.0 if self.n == 0 else self._fast_prev
        else:
            fast = min(100.0, max(0.0, 100.0 * (c - mn) / den))
        self._fast_prev = fast
        self.d = self._sma_k.update(self._sma_fast.update(fast))
        return self.d


def _make_ma(ma_type: str, period: int):
    t = ma_type.upper()
    if t == "SMA":
        return Nt8Sma(period)
    if t == "EMA":
        return Nt8Ema(period)
    if t == "WILDERMA":
        return Nt8Ema(2 * period - 1)
    raise NotImplementedError(f"ThunderZilla trend MA type {ma_type!r} "
                              f"not ported (SMA/EMA/WilderMA only)")


# ---------------------------------------------------------------------------

class ThunderZilla:
    def __init__(self, trend_ma_type: str = "SMA", trend_period: int = 200,
                 stop_offset_mult: float = 60.0,
                 signal_qty_per_flat: int = 2,
                 signal_qty_per_trend: int = 999,
                 tick_size: float = 0.25):
        self.tick = tick_size
        self.qty_flat = signal_qty_per_flat
        self.qty_trend = signal_qty_per_trend
        self._stop_off = round_to_tick(stop_offset_mult * tick_size, tick_size)
        self._vec_off = round_to_tick(30.0 * tick_size, tick_size)

        self._trend_ma = _make_ma(trend_ma_type, trend_period)
        self._ema14, self._ema30, self._ema45 = Nt8Ema(14), Nt8Ema(30), Nt8Ema(45)
        self._rsi = _Nt8Rsi(14)
        self._mfi = _Nt8Mfi(14)
        self._stoch = _Nt8Stochastics(14, 7, 3)

        self.n = -1
        self.trend = 0                    # Signal_Trend (trendState)
        self._trend_prev = 0              # Signal_Trend[1]
        # solar wind
        self._sw_up = False
        self._sw_vec = 0.0                # seriesSWTrendVector
        self.stop = 0.0                   # Stop plot
        self._stop_prev = 0.0
        # sumo
        self._sumo_up = False
        self._sumo_signal = 0             # sumoSignalTrade
        self._sumo_count = 0
        self._sumo_next = -1
        self._sumo_fair = 0.0             # seriesSumoFair[0]
        self.trend_ma = 0.0               # Trend plot
        self._trend_ma_prev = 0.0
        self._max_prev = self._min_prev = 0.0
        # obos
        self._obos_state_prev = 0
        self._obos_exit_idx = -1
        self._obos_last = -1
        self._obos_signal = 0
        # counters
        self._count_sw = self._count_sm = 0
        self._count_flat = 0.0
        self._count_trend = 0.0
        self._o1 = self._c1 = float("nan")

    # ------------------------------------------------------------------
    def update(self, o: float, h: float, l: float, c: float,
               volume: float) -> int:
        self.n += 1
        self._stop_prev_r = round_to_tick(self.stop, self.tick)
        self._trend_prev_r = round_to_tick(self.trend_ma, self.tick)
        self._solar_wind(o, c)
        self._sumo(o, h, l, c)
        self._obos(h, l, c, volume)
        code = self._trend_machine(o, h, l, c)
        self._o1, self._c1 = o, c
        return code

    # ------------------------------------------------------------------
    def _solar_wind(self, o: float, c: float) -> None:
        dn_stop = round_to_tick(c - self._stop_off, self.tick)
        up_stop = round_to_tick(c + self._stop_off, self.tick)
        vec_dn, vec_up = c - self._vec_off, c + self._vec_off
        if self.n == 0:
            self._sw_up = _ac(c, o) > 0
            if self._sw_up:
                self._sw_vec, self.stop = vec_dn, dn_stop
            else:
                self._sw_vec, self.stop = vec_up, up_stop
            return
        v1, s1 = self._sw_vec, self.stop
        if not self._sw_up:
            if _ac(c, s1) <= 0:                       # downtrend continues
                self._sw_vec = v1 if _ac(vec_up, v1) >= 0 else vec_up
                self.stop = min(up_stop, s1)
            else:                                     # flip up
                self._sw_up = True
                self._sw_vec = v1 if _ac(vec_dn, v1) <= 0 else vec_dn
                self.stop = dn_stop
        elif _ac(c, s1) >= 0:                         # uptrend continues
            self._sw_vec = v1 if _ac(vec_dn, v1) <= 0 else vec_dn
            self.stop = max(dn_stop, s1)
        else:                                         # flip down
            self._sw_up = False
            self._sw_vec = v1 if _ac(vec_up, v1) >= 0 else vec_up
            self.stop = up_stop

    # ------------------------------------------------------------------
    def _sumo(self, o: float, h: float, l: float, c: float) -> None:
        ma_t = self._trend_ma.update(c)
        e14 = self._ema14.update(c)
        e30 = self._ema30.update(c)
        e45 = self._ema45.update(c)
        if self.n == 0:
            self.trend_ma = ma_t
            self._sumo_fair = (ma_t + e14 + e30 + e45) / 4.0
            self._max_prev = max(ma_t, e14, e30, e45)
            self._min_prev = min(ma_t, e14, e30, e45)
            self._trend_ma_prev = ma_t
            return
        self._sumo_signal = 0
        mas = (ma_t, e14, e30, e45)
        mx, mn = max(mas), min(mas)
        red_after_green = (_ac(c, o) < 0 and _ac(self._c1, self._o1) > 0)
        green_after_red = (_ac(c, o) > 0 and _ac(self._c1, self._o1) < 0)
        straddle = _ac(mn, l) > 0 and _ac(mx, h) < 0
        if _ac(c, o) <= 0 or _ac(self._c1, self._o1) >= 0:
            if red_after_green and straddle and _ac(ma_t, mx) == 0:
                self._sumo_fire(-1)
        elif straddle and _ac(ma_t, mn) == 0:
            self._sumo_fire(1)
        if self.n > self._sumo_next and self._sumo_count != 0:
            self._sumo_count = 0
            self._sumo_next = -1
        trend_prev = self.trend_ma
        max_prev, min_prev = self._max_prev, self._min_prev
        self.trend_ma = ma_t
        if self.n == 1:
            self._sumo_up = _ac(mx, mn) > 0
        if (self._sumo_up and _ac(mn, ma_t) < 0
                and not (_ac(mx, ma_t) != 0 and _ac(min_prev, trend_prev) != 0)):
            self._sumo_up = False                     # trend MA on top
        elif (not self._sumo_up and _ac(mx, ma_t) > 0
                and (_ac(mn, ma_t) == 0 or _ac(max_prev, trend_prev) == 0)):
            self._sumo_up = True                      # trend MA on bottom
        self._sumo_fair = (ma_t + e14 + e30 + e45) / 4.0
        self._max_prev, self._min_prev = mx, mn
        self._trend_ma_prev = trend_prev

    def _sumo_fire(self, direction: int) -> None:
        if self._sumo_count == 0 and self._sumo_next < 0:
            self._sumo_signal = direction
            self._sumo_count = 1
            self._sumo_next = self.n + 15
        elif self._sumo_count == 1:
            if self.n >= self._sumo_next:
                self._sumo_signal = direction
                self._sumo_count = 2
                self._sumo_next = self.n + 30
        elif self._sumo_count == 2 and self.n > self._sumo_next:
            self._sumo_signal = direction

    # ------------------------------------------------------------------
    def _obos(self, h: float, l: float, c: float, volume: float) -> None:
        mfi = self._mfi.update(h, l, c, volume)
        rsi = self._rsi.update(c)
        sto = self._stoch.update(h, l, c)
        if self.n == 0:
            self._obos_signal = 0
            return
        s1, s2, s3 = (self._state(mfi), self._state(rsi), self._state(sto))
        if s1 > 0 and s2 > 0 and s3 > 0:
            state = 1
        elif s1 < 0 and s2 < 0 and s3 < 0:
            state = -1
        else:
            state = 0
        if state == 0 and self._obos_state_prev != 0:
            self._obos_exit_idx = self.n
            self._obos_last = self._obos_state_prev
        self._obos_signal = 0
        if (self._obos_exit_idx > 0 and state == 0
                and self.n - self._obos_exit_idx < 3):
            if self._obos_last > 0 and _ac(c, self._c1) < 0:
                self._obos_exit_idx = -1
                self._obos_signal = -1
            if self._obos_last < 0 and _ac(c, self._c1) > 0:
                self._obos_exit_idx = -1
                self._obos_signal = 1
        self._obos_state_prev = state

    @staticmethod
    def _state(v: float) -> int:
        if _ac(v, 70.0) < 0:
            return 0 if _ac(v, 30.0) > 0 else -1
        return 1

    # ------------------------------------------------------------------
    def _trend_machine(self, o: float, h: float, l: float, c: float) -> int:
        if self.n == 0:
            return 0
        sw, sm = self._sw_up, self._sumo_up
        if self.n == 1:
            self.trend = 1 if (sw and sm) else (-1 if (not sw and not sm) else 0)
            if self.trend != 0:
                self._count_sw = self._count_sm = 1
            self._trend_prev = 0
            code = self.trend
            self._trend_prev = self.trend
            return code

        prev_signal_trend = self._trend_prev          # Signal_Trend[1]
        if self.trend != 0:
            if self.trend == -1:                      # downtrend held
                if sw and sm:
                    self.trend = 1
                    self._count_sw = self._count_sm = 1
                elif sw and not sm:
                    self._count_sw = 0
                elif not sw and sm:
                    self._count_sm = 0
                if self._count_sw + self._count_sm == 0:
                    self.trend = 0
            else:                                     # uptrend held
                if not sw and not sm:
                    self.trend = -1
                    self._count_sw = self._count_sm = 1
                elif sw and not sm:
                    self._count_sm = 0
                elif not sw and sm:
                    self._count_sw = 0
                if self._count_sw + self._count_sm == 0:
                    self.trend = 0
        else:
            if sw and sm:
                self.trend = 1
                self._count_sw = self._count_sm = 1
            elif not sw and not sm:
                self.trend = -1
                self._count_sw = self._count_sm = 1

        if self.trend != prev_signal_trend and abs(self.trend) == 1:
            self._count_trend = 0.0
        trend0_r = round_to_tick(self.trend_ma, self.tick)
        trend1_r = self._trend_prev_r
        stop0_r = round_to_tick(self.stop, self.tick)
        stop1_r = self._stop_prev_r
        if _ac(stop0_r, stop1_r) != 0:
            self._count_flat = 0.0

        code = 0
        if self.trend != 0:
            up = self.trend == 1
            sign = 1 if up else -1
            if self.trend == prev_signal_trend:
                candle_ok = (_ac(c, o) * sign > 0
                             and _ac(self._c1, self._o1) * sign < 0)
                if (self._count_trend < self.qty_trend
                        and self._count_flat < self.qty_flat and candle_ok):
                    if self._sumo_signal == 0:
                        vec_r = round_to_tick(self._sw_vec, self.tick)
                        fair_r = round_to_tick(self._sumo_fair, self.tick)
                        at_fair = _ac(h, fair_r) >= 0 and _ac(l, fair_r) <= 0
                        at_trend = _ac(h, trend0_r) >= 0 and _ac(l, trend0_r) <= 0
                        at_vec = _ac(h, vec_r) >= 0 and _ac(l, vec_r) <= 0
                        if (at_fair and at_vec) or (at_fair and at_trend):
                            self._count_trend += 1.0
                            self._count_flat += 1.0
                            code = 3 if up else -3
                    else:
                        self._count_trend += 1.0
                        self._count_flat += 1.0
                        code = 3 if up else -3
                if (self._sw_up == up
                        and _ac(stop0_r, trend0_r) * sign > 0
                        and _ac(stop1_r, trend1_r) * sign <= 0):
                    code = 4 if up else -4
            else:
                self._count_flat = 0.0
                code = 1 if up else -1
        if self._obos_signal != 0:
            code = 2 if self._obos_signal == 1 else -2
        self._trend_prev = self.trend
        return code
