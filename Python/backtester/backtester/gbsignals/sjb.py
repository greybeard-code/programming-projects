"""gbSuperJumpBoost port — supply/demand zones from stalled trailing vectors.

Source: nt8 code/GodZillaKilla/indicators/gbSuperJumpBoost.cs (OnBarUpdate
1051-1066, ComputeJumpBoostInfo 1570-1690, FindZoneInfo 1457-1529,
CheckBrokenAndFindSignal 1323-1436, IsCloseOk 1438-1455).

Signal_Trade codes: +-2 zone start, +-1 return-to-zone.

Four trailing "jump boost" engines (offset levels 1-4 x ATR(100) around a
reference price) each emit a per-bar trend signal: +-1 when their vector
stalled cleanly, +-2 when it moved or went weak. When two ADJACENT engines
both read +-1 in the same direction, their vector prices define a zone
(top/bottom of the collected stack) -> +-2 fires. Afterwards, a candle
returning to the zone's near edge (dip-through-and-recover, or close inside
having crossed the last opposing bar's open) fires +-1, gated by IsCloseOk
(close in the outer SignalCloseThreshold% of the bar), SignalQuantityPerZone
per zone and a SignalSplit bar debounce per direction.

Only the Signal_Trade path is ported: the market-extremes lists, the fifth
(main) engine and signalState feed rendering/Signal_State only — verified
against the source and skipped. NT8 warmup quirks kept (expanding Wilder
ATR from bar 0, partial-window MAX/MIN).
"""
from __future__ import annotations

from collections import deque
from dataclasses import dataclass

from .nt8math import approx_compare as _ac, round_to_tick


class _Nt8Atr:
    """@ATR.cs: expanding Wilder true-range average from bar 0."""

    def __init__(self, period: int):
        self.period = period
        self.n = -1
        self._c1 = float("nan")
        self.value = 0.0

    def update(self, h: float, l: float, c: float) -> float:
        self.n += 1
        if self.n == 0:
            self.value = h - l
        else:
            tr = max(abs(l - self._c1), h - l, abs(h - self._c1))
            m = min(self.n + 1, self.period)
            self.value = ((m - 1) * self.value + tr) / m
        self._c1 = c
        return self.value


class _JumpEngine:
    """One JumpBoostInfo trailing-vector engine."""

    def __init__(self, offset_base: float, offset_level: float,
                 slowdown_scan: int, weak_split: int):
        self.offset_base = offset_base
        self.offset_level = offset_level
        self.slowdown_scan = slowdown_scan
        self.weak_split = weak_split
        self.up = False
        self.stop = 0.0                  # StopCurrentValue
        self.vec = float("nan")          # SeriesTrendVector[0]
        self.count_slowdown = 0
        self.next_weak = 0
        self.signal = 0                  # SeriesSignalTrend[0]

    def update(self, n: int, o: float, c: float, ref: float, atr: float,
               tick: float) -> None:
        base_off = round_to_tick(self.offset_base * atr, tick)
        stop_dn = round_to_tick(ref - base_off, tick)
        stop_up = round_to_tick(ref + base_off, tick)
        lvl_off = round_to_tick(self.offset_level * atr, tick)
        vec_dn, vec_up = ref - lvl_off, ref + lvl_off
        if n == 0:
            self.up = _ac(c, o) > 0
            self.vec = vec_dn if self.up else vec_up
            self.stop = stop_dn if self.up else stop_up
            return
        weak = False
        v1 = self.vec
        if not self.up:
            if _ac(c, self.stop) <= 0:                 # downtrend continues
                if _ac(vec_up, v1) >= 0:               # vector stalled
                    self.count_slowdown += 1
                    if (self.count_slowdown < self.slowdown_scan
                            or n < self.next_weak):
                        weak = True
                else:
                    self.vec = vec_up
                    self.count_slowdown = 0
                self.stop = min(stop_up, self.stop)
            else:                                      # flip up
                self.up = True
                if _ac(vec_dn, v1) <= 0:
                    weak = True
                else:
                    self.vec = vec_dn
                self.next_weak = n + self.weak_split
                self.count_slowdown = 0
                self.stop = stop_dn
        elif _ac(c, self.stop) >= 0:                   # uptrend continues
            if _ac(vec_dn, v1) <= 0:                   # vector stalled
                self.count_slowdown += 1
                if (self.count_slowdown < self.slowdown_scan
                        or n < self.next_weak):
                    weak = True
            else:
                self.vec = vec_dn
                self.count_slowdown = 0
            self.stop = max(stop_dn, self.stop)
        else:                                          # flip down
            self.up = False
            if _ac(vec_up, v1) >= 0:
                weak = True
            else:
                self.vec = vec_up
            self.next_weak = n + self.weak_split
            self.count_slowdown = 0
            self.stop = stop_up
        s = 1 if self.up else -1
        moved = _ac(self.vec, v1) != 0
        self.signal = s * 2 if (moved or weak) else s


@dataclass
class _Zone:
    top: float
    bottom: float
    is_bullish: bool
    count_return: int = 0

    @property
    def sign(self) -> int:
        return 1 if self.is_bullish else -1


class SuperJumpBoost:
    def __init__(self, sensitive_mode: bool = True,
                 offset_level1: float = 1.0, offset_level2: float = 2.0,
                 offset_level3: float = 3.0, offset_level4: float = 4.0,
                 offset_base: float = 4.0, reference_price_period: int = 2,
                 line_levels_offset: int = 100,
                 signal_close_threshold: int = 70,
                 signal_qty_per_zone: int = 2, signal_split: int = 20,
                 tick_size: float = 0.25):
        self.tick = tick_size
        # State.Configure clamps: levels capped by the base, base raised to L4
        l1 = min(offset_level1, offset_base)
        l2 = min(offset_level2, offset_base)
        l3 = min(offset_level3, offset_base)
        l4 = min(offset_level4, offset_base)
        base = max(l4, offset_base)
        scan = 5 if sensitive_mode else 1
        split = 10 if sensitive_mode else 1
        self._engines = [_JumpEngine(base, lv, scan, split)
                         for lv in (l1, l2, l3, l4)]
        self._atr = _Nt8Atr(100)                       # OffsetATRPeriod
        self.ref_period = reference_price_period
        self.close_weight = 1                          # ReferencePriceCloseWeight
        self._line_off = line_levels_offset * tick_size
        self._close_thr = signal_close_threshold / 100.0
        self.qty_per_zone = signal_qty_per_zone
        self.signal_split = signal_split

        self.n = -1
        self._highs: deque[float] = deque(maxlen=reference_price_period)
        self._lows: deque[float] = deque(maxlen=reference_price_period)
        self._closes: deque[float] = deque(maxlen=reference_price_period)
        self._zone: _Zone | None = None                # single active zone
        self._last_zone: _Zone | None = None
        self._is_new = False
        self._last_bull = -1
        self._last_bear = -1
        self._open_up = 0.0                            # openPriceUpBar
        self._open_dn = 0.0

    # ------------------------------------------------------------------
    def update(self, o: float, h: float, l: float, c: float) -> int:
        self.n += 1
        self._highs.append(h)
        self._lows.append(l)
        self._closes.append(c)
        ref = (max(self._highs) + min(self._lows)
               + self.close_weight * (max(self._closes) + min(self._closes)) / 2.0
               ) / (2 + self.close_weight)
        atr = self._atr.update(h, l, c)
        for eng in self._engines:
            eng.update(self.n, o, c, ref, atr, self.tick)
        code = self._find_zone(c)
        ret = self._check_return(o, h, l, c)
        return ret if ret else code

    # ------------------------------------------------------------------
    def _find_zone(self, c: float) -> int:
        prices: list[float] = []
        bullish = False
        first = True
        for e1, e2 in zip(self._engines, self._engines[1:]):
            if e1.signal == e2.signal and abs(e1.signal) == 1:
                if first:
                    bullish = e1.signal == 1
                    prices += [e1.vec, e2.vec]
                    first = False
                else:
                    prices.append(e2.vec)
        if not prices:
            return 0
        top, bottom = max(prices), min(prices)
        last = self._last_zone
        if last is not None:
            if _ac(last.top, top) == 0 and _ac(last.bottom, bottom) == 0:
                return 0
            if bullish == last.is_bullish:
                near_old = last.top if last.is_bullish else last.bottom
                near_new = top if bullish else bottom
                if _ac(abs(near_old - near_new), self._line_off) < 0:
                    return 0
        far = bottom if bullish else top
        sign = 1 if bullish else -1
        if _ac((c - far) * sign, 0.0) < 0:
            return 0
        zone = _Zone(top=top, bottom=bottom, is_bullish=bullish)
        self._last_zone = zone
        self._zone = zone                              # evicts any older zone
        self._is_new = True
        return sign * 2

    # ------------------------------------------------------------------
    def _check_return(self, o: float, h: float, l: float, c: float) -> int:
        if self._zone is None:
            return 0
        green, red = _ac(c, o) > 0, _ac(c, o) < 0
        if green:
            self._open_up = o
        elif red:
            self._open_dn = o
        if self._is_new:                               # skip the add bar
            self._is_new = False
            return 0
        z = self._zone
        near = z.bottom if z.is_bullish else z.top
        if _ac((near - c) * z.sign, 0.0) > 0:          # closed through far side
            self._zone = None
            self._last_zone = None
            return 0
        edge = z.top if z.is_bullish else z.bottom
        if not self._close_ok(o, h, l, c):
            return 0
        if z.count_return >= self.qty_per_zone:
            return 0
        if z.is_bullish:
            if not (green and self.n - self._last_bull > self.signal_split):
                return 0
            dip = _ac(c, edge) > 0 and _ac(l, edge) < 0
            inside = _ac(c, edge) <= 0 and _ac(c, self._open_dn) > 0
            if dip or inside:
                self._last_bull = self.n
                z.count_return += 1
                return 1
        else:
            if not (red and self.n - self._last_bear > self.signal_split):
                return 0
            dip = _ac(c, edge) < 0 and _ac(h, edge) > 0
            inside = _ac(c, edge) >= 0 and _ac(c, self._open_up) < 0
            if dip or inside:
                self._last_bear = self.n
                z.count_return += 1
                return -1
        return 0

    def _close_ok(self, o: float, h: float, l: float, c: float) -> bool:
        if _ac(c, o) == 0:
            return False
        if _ac(c, o) <= 0:
            return _ac(c, h - self._close_thr * (h - l)) < 0
        return _ac(c, l + self._close_thr * (h - l)) > 0
