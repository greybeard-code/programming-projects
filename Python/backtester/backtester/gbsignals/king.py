"""gbKingOrderBlock port — smart-money order blocks (swings/BOS-CHoCH/FVG).

Source: nt8 code/GodZillaKilla/indicators/gbKingOrderBlock.cs (OnBarUpdate
1559-1599, swing finders 2246-2422, BOS/CHoCH 2424-2514, imbalance 1888-1998,
order blocks 2000-2152, IsReversalBar 2957-2968), OnBarClose semantics.

Signal_Trade codes:
  +1/-1 RETURN  — price dips into an active order block's signal edge on a
                  3-close V-reversal (split/quantity gated)
  +2/-2 BREAKOUT — close breaks through an order block's far edge
                  (+2 = a bearish (top) block failing = bullish breakout)

Pipeline per closed bar: find swing points (3 detector types over a
2*Neighborhood window, incl. flat-plateau and double-top variants), break
them with body-cross bars -> BOS/CHoCH records; build imbalances from runs of
ImbalanceQualifying full-marubozu bars (common on renko!), broken when price
re-enters; an order block forms at the last unbroken swing when a fresh
imbalance and an opposite-side BOS/CHoCH confirm within
OrderBlockFindingBosChochPeriod bars, offset-gated against existing blocks.

Only the Signal_Trade path is ported (Signal_State/zones/rendering skipped).
C# loop quirks kept: removing a zone mid-scan skips the next zone that bar;
a duplicate BOS key aborts that record (swallowed exception in the C#).
"""
from __future__ import annotations

from dataclasses import dataclass, field

from .nt8math import approx_compare as _ac


@dataclass
class _Swing:
    is_top: bool
    price: float
    bar_start: int
    bar_end: int
    is_broken: bool = False
    has_ob: bool = False


@dataclass
class _BosChoch:
    is_top: bool
    price: float
    bar_start: int
    is_choch: bool
    bar_end: int


@dataclass
class _Imbalance:
    is_top: bool
    bar_start: int
    bar_end: int
    top: float
    bottom: float
    is_fixed: bool = False
    is_broken: bool = False

    @property
    def sign(self) -> int:
        return -1 if self.is_top else 1

    @property
    def price_broken(self) -> float:      # ImbalanceInfo.PriceBroken
        return self.bottom if self.is_top else self.top


@dataclass
class _OrderBlock:
    is_top: bool
    bar_start: int
    bar_end: int
    top: float
    bottom: float
    key: int = 0
    count_return: int = 0

    @property
    def sign(self) -> int:
        return -1 if self.is_top else 1

    @property
    def price_broken(self) -> float:      # OrderBlockInfo.PriceBroken
        return self.top if self.is_top else self.bottom

    @property
    def price_signal(self) -> float:      # OrderBlockInfo.PriceSignal
        return self.bottom if self.is_top else self.top


class _ValueData:
    """C# ValueData: min/max of positive prices, -1 sentinel when empty."""

    def __init__(self):
        self.min = -1.0
        self.max = -1.0

    def add(self, price: float) -> None:
        if _ac(price, 0.0) > 0:
            if _ac(self.min, 0.0) >= 0 and _ac(self.max, 0.0) >= 0:
                self.min = min(self.min, price)
                self.max = max(self.max, price)
            else:
                self.min = self.max = price


class KingOrderBlock:
    def __init__(self, swing_point_neighborhood: int = 5,
                 imbalance_qualifying: int = 3,
                 finding_boschoch_period: int = 50,
                 order_block_age: int = 500,
                 same_direction_offset: int = 10,
                 difference_direction_offset: int = 10,
                 signal_qty_per_order_block: int = 3,
                 signal_split_bars: int = 6, tick_size: float = 0.25):
        self.nb = swing_point_neighborhood
        self.imb_q = imbalance_qualifying
        self.find_period = finding_boschoch_period
        self.ob_age = order_block_age
        self.off_same = same_direction_offset * tick_size
        self.off_diff = difference_direction_offset * tick_size
        self.qty_per_ob = signal_qty_per_order_block
        self.split_bars = signal_split_bars

        self.n = -1
        self._o: list[float] = []
        self._h: list[float] = []
        self._l: list[float] = []
        self._c: list[float] = []
        self._last_top: _Swing | None = None
        self._last_bottom: _Swing | None = None
        self._last_swing: _Swing | None = None
        self._last_bos: _BosChoch | None = None
        self._bos_keys_top: set[int] = set()
        self._bos_keys_bottom: set[int] = set()
        self._last_imb: _Imbalance | None = None
        self._imbs: list[_Imbalance] = []       # active, key order
        self._obs: list[_OrderBlock] = []       # active, key order
        self._ret_bull = -1                     # returnBullishBarIndex
        self._ret_bear = -1

    # ------------------------------------------------------------------
    def update(self, o: float, h: float, l: float, c: float) -> int:
        self.n += 1
        self._o.append(o)
        self._h.append(h)
        self._l.append(l)
        self._c.append(c)
        self._find_swing(True)
        self._find_swing(False)
        self._break_swing(True)
        self._break_swing(False)
        if self.n < 3:
            return 0
        self._find_imbalance()
        self._break_imbalances()
        code = self._ob_signal()
        self._add_order_block()
        return code

    # ---------------- swings -----------------------------------------
    def _price(self, is_top: bool, idx: int) -> float:
        return self._h[idx] if is_top else self._l[idx]

    def _find_swing(self, is_top: bool) -> None:
        sp = (self._swing_type1(is_top) or self._swing_type2(is_top)
              or self._swing_type3(is_top))
        if sp is None:
            return
        if is_top:
            if self._last_top is None or self._last_top.bar_end != sp.bar_end:
                self._last_top = sp
            else:
                sp = self._last_top          # key already present: keep old
        else:
            if (self._last_bottom is None
                    or self._last_bottom.bar_end != sp.bar_end):
                self._last_bottom = sp
            else:
                sp = self._last_bottom
        self._last_swing = sp

    def _swing_type1(self, is_top: bool) -> _Swing | None:
        n, nb = self.n, self.nb
        if n < 2 * nb:
            return None
        j = n - nb
        s = 1 if is_top else -1
        price = self._price(is_top, j)
        for i in range(n - 2 * nb, n + 1):
            if i != j and s * _ac(self._price(is_top, i), price) >= 0:
                return None
        return _Swing(is_top, price, j, j)

    def _swing_type2(self, is_top: bool) -> _Swing | None:
        n, nb = self.n, self.nb
        if n < 3 * nb:
            return None
        j = n - nb
        lo = j - nb
        s = 1 if is_top else -1
        price = self._price(is_top, j)
        count = 1
        i = j - 1
        while i >= lo and _ac(self._price(is_top, i), price) == 0:
            count += 1
            i -= 1
        if count < 2:
            return None
        start = j - count + 1
        for k in range(start - nb, n + 1):
            if (k < start or k > j) and s * _ac(self._price(is_top, k), price) >= 0:
                return None
        return _Swing(is_top, price, start, j)

    def _swing_type3(self, is_top: bool) -> _Swing | None:
        n, nb = self.n, self.nb
        if n < 3 * nb:
            return None
        j = n - nb
        lo = j - nb
        s = 1 if is_top else -1
        price = self._price(is_top, j)
        count, deepest = 1, 0
        i = j - 1
        while i >= lo:
            cmp = _ac(self._price(is_top, i), price)
            if s * cmp > 0:
                return None
            if cmp == 0:
                deepest = i
                count += 1
            i -= 1
        if count < 2:
            return None
        start = deepest
        inner, before, after = _ValueData(), _ValueData(), _ValueData()
        for k in range(start - nb, n + 1):
            p = self._price(is_top, k)
            if start <= k <= j:
                inner.add(p)
            else:
                if s * _ac(p, price) >= 0:
                    return None
                (before if k < start else after).add(p)
        if _ac(inner.max, inner.min) == 0:
            return None
        edge = (max(before.min, after.min) if is_top
                else min(before.max, after.max))
        cmp = _ac(inner.min if is_top else inner.max, edge)
        if s * cmp <= 0:
            return None
        return _Swing(is_top, price, start, j)

    # ---------------- BOS / CHoCH -------------------------------------
    def _break_swing(self, is_top: bool) -> None:
        sp = self._last_top if is_top else self._last_bottom
        if sp is None or sp.is_broken:
            return
        price = sp.price
        o, c = self._o[self.n], self._c[self.n]
        body = abs(o - c)
        no_cross = ((_ac(o, price) < 0 or _ac(c, price) > 0)
                    and (_ac(c, price) < 0 or _ac(o, price) > 0))
        if is_top:
            if _ac(price, o) > 0 and (not body > 0.0 or no_cross):
                return
        else:
            if _ac(price, o) < 0 and (not body > 0.0 or no_cross):
                return
        sp.is_broken = True
        keys = self._bos_keys_top if is_top else self._bos_keys_bottom
        if sp.bar_end in keys:
            return                       # C# SortedList.Add throws, swallowed
        last = self._last_bos
        if last is None:
            is_choch = False             # first break is a BOS
        elif is_top:
            is_choch = not last.is_top   # breaking a top after a bottom event
        else:
            is_choch = last.is_top       # breaking a bottom after a top event
        info = _BosChoch(is_top, price, sp.bar_end, is_choch, self.n - 1)
        keys.add(sp.bar_end)
        self._last_bos = info

    # ---------------- imbalances (FVG from marubozu runs) --------------
    def _marubozu(self, bars_ago: int, is_top: bool):
        idx = self.n - bars_ago
        a = self._o[idx] if is_top else self._c[idx]
        b = self._c[idx] if is_top else self._o[idx]
        if _ac(a, b) == 0:
            return None
        if _ac(a, self._h[idx]) == 0 and _ac(b, self._l[idx]) == 0:
            return (a, b)                # (top, bottom)
        return None

    def _find_imbalance(self) -> None:
        o, c = self._o[self.n], self._c[self.n]
        if _ac(c, o) == 0:
            return
        is_top = _ac(c, o) < 0           # red run = bearish (top) imbalance
        mb = self._marubozu(0, is_top)
        last = self._last_imb
        if last is not None:
            if mb is None:
                last.is_fixed = True
                return
            if (not last.is_fixed and not last.is_broken
                    and last.is_top == is_top and last.bar_end == self.n - 1):
                if is_top:
                    last.bottom = c
                else:
                    last.top = c
                last.bar_end = self.n
                return
        top, bottom = -2147483648.0, 2147483647.0
        for q in range(self.imb_q - 1, -1, -1):
            mb2 = self._marubozu(q, is_top)
            if mb2 is None:
                return
            top = max(top, mb2[0])
            bottom = min(bottom, mb2[1])
        imb = _Imbalance(is_top, self.n - self.imb_q + 1, self.n, top, bottom)
        # AddElementToList: overwrite on key collision (key = bar_start)
        self._imbs = [z for z in self._imbs if z.bar_start != imb.bar_start]
        self._imbs.append(imb)
        self._imbs.sort(key=lambda z: z.bar_start)
        self._last_imb = imb

    def _break_imbalances(self) -> None:
        i = 0
        while i < len(self._imbs):
            imb = self._imbs[i]
            probe = self._h[self.n] if imb.is_top else self._l[self.n]
            broken = ((_ac((probe - imb.price_broken) * imb.sign, 0.0) < 0
                       and imb.bar_end != self.n)
                      or self.n - imb.bar_start > self.ob_age)
            if broken:
                imb.is_broken = True
                del self._imbs[i]
                i += 1                   # C# index-skip after removal
            else:
                imb.bar_end = self.n
                i += 1

    # ---------------- order blocks -------------------------------------
    def _ob_signal(self) -> int:
        if not self._obs:
            return 0
        num = 0
        c0 = self._c[self.n]
        i = 0
        while i < len(self._obs):
            ob = self._obs[i]
            ob.bar_end = self.n - 1
            aged = self.n - ob.bar_start > self.ob_age
            if _ac(c0, ob.price_broken) * ob.sign < 0 or aged:
                if ob.count_return == 0 and abs(num) != 1 and not aged:
                    num = -2 * ob.sign               # breakout
                del self._obs[i]
                i += 1                               # C# index-skip
                continue
            if ob.is_top:
                split_ok = (self._ret_bear == -1
                            or self.n - self._ret_bear > self.split_bars)
            else:
                split_ok = (self._ret_bull == -1
                            or self.n - self._ret_bull > self.split_bars)
            exhausted = ob.count_return >= self.qty_per_ob
            probe = self._l[self.n] if not ob.is_top else self._h[self.n]
            touch = _ac((probe - ob.price_signal) * ob.sign, 0.0) < 0
            if (touch and self._reversal(-ob.sign) and split_ok
                    and not exhausted and abs(num) != 1):
                num = ob.sign                        # return signal
                if num == 1:
                    self._ret_bull = self.n
                else:
                    self._ret_bear = self.n
                ob.count_return += 1
            i += 1
        return num

    def _reversal(self, sign: int) -> bool:
        if self.n < 2:
            return False
        c0, c1, c2 = self._c[self.n], self._c[self.n - 1], self._c[self.n - 2]
        return (sign * _ac(c1, c0) > 0) and (sign * _ac(c2, c1) < 0)

    def _add_order_block(self) -> None:
        sp, bos, imb = self._last_swing, self._last_bos, self._last_imb
        if (imb is None or sp is None or bos is None or sp.has_ob
                or self.n - sp.bar_end > self.find_period
                or sp.is_top != imb.is_top or sp.is_top == bos.is_top
                or bos.bar_end < sp.bar_end or imb.bar_start < sp.bar_start):
            return
        ss = sp.bar_start
        hi, lo = self._h[ss], self._l[ss]
        if not self._offset_ok(sp, hi, lo):
            return
        edge_imb = imb.top if sp.is_top else imb.bottom
        edge_sp = hi if sp.is_top else lo
        if (edge_imb - edge_sp) * imb.sign < 0.0:
            return
        ob = _OrderBlock(sp.is_top, sp.bar_start, self.n, hi, lo,
                         key=sp.bar_end)
        self._obs = [z for z in self._obs if z.key != ob.key]
        self._obs.append(ob)
        self._obs.sort(key=lambda z: z.key)
        sp.has_ob = True

    def _offset_ok(self, sp: _Swing, hi: float, lo: float) -> bool:
        if not self._obs:
            return True
        top_ob = bottom_ob = None
        for ob in reversed(self._obs):
            if top_ob is not None and bottom_ob is not None:
                break
            if ob.is_top and top_ob is None:
                top_ob = ob
            elif not ob.is_top and bottom_ob is None:
                bottom_ob = ob
        if top_ob is not None and _ac(
                top_ob.bottom - hi,
                self.off_same if sp.is_top else self.off_diff) <= 0:
            return False
        if bottom_ob is not None and _ac(
                lo - bottom_ob.top,
                self.off_diff if sp.is_top else self.off_same) <= 0:
            return False
        return True
