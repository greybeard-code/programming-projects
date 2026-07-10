"""Simulated broker: order matching against the trade-event stream.

Fill model (tick-level L1):
- MARKET: fills on the next trade event, at the prevailing ask (buy) / bid
  (sell) plus configured slippage ticks.
- LIMIT: fills when a trade prints *through* the limit (strictly better than
  the limit price) — touch alone never fills, approximating queue risk. If the
  order is already marketable when first evaluated (ask <= buy limit), it fills
  immediately at the quote.
- STOP: triggers when a trade prints at/through the stop, then fills like a
  market order at the prevailing quote (plus slippage) on that event.

Orders are resolved chronologically within each bar span, so an entry fill
activates its bracket children and those children can fill later in the same
bar, or even on the same event.
"""
from __future__ import annotations

from typing import Callable

import numpy as np

from .account import Account, PropFirmTracker, TradeRecorder
from .contracts import ContractSpec
from .data import DayL1
from .orders import BUY, SELL, BracketSpec, Fill, Order, OrderState, OrderType


class SimBroker:
    def __init__(self, spec: ContractSpec, account: Account,
                 recorder: TradeRecorder, prop: PropFirmTracker | None = None,
                 slippage_ticks: float = 0.0,
                 on_fill: Callable[[Fill], None] | None = None,
                 max_position: int | None = None):
        self.spec = spec
        self.account = account
        self.recorder = recorder
        self.prop = prop
        self.slippage = slippage_ticks * spec.tick_size
        self.on_fill = on_fill
        # Apex net-position cap (contracts). None/0 = off. A fill is clamped so
        # |net position| can never exceed it (models the 6-mini/60-micro rule).
        self.max_position = max_position or None
        self._cap_warned = False
        self.working: list[Order] = []
        self.fills: list[Fill] = []
        self._brackets: dict[int, BracketSpec] = {}   # entry order id -> spec
        self._day: DayL1 | None = None
        self.halted = False

    # ---------------- order entry ----------------

    def submit(self, order: Order, bracket: BracketSpec | None = None) -> Order:
        self.working.append(order)
        if bracket and (bracket.stop_ticks or bracket.target_ticks):
            self._brackets[order.id] = bracket
        return order

    def cancel(self, order: Order) -> None:
        if order.state is OrderState.WORKING and order in self.working:
            order.state = OrderState.CANCELLED
            self.working.remove(order)

    def cancel_all(self, entries_only: bool = False) -> None:
        for o in list(self.working):
            if entries_only and o.is_exit:
                continue
            self.cancel(o)

    def modify(self, order: Order, price: float) -> bool:
        """Change a working limit/stop price. Takes effect from the next event
        the order is evaluated against (no retroactive fills)."""
        if order.state is not OrderState.WORKING or order.type is OrderType.MARKET:
            return False
        order.price = float(price)
        return True

    # ---------------- day lifecycle ----------------

    def begin_day(self, day: DayL1) -> None:
        self._day = day

    def flatten(self, idx: int, tag: str = "flatten") -> None:
        """Close any open position at the quotes of event `idx`."""
        pos = self.account.position
        if pos == 0:
            return
        side = SELL if pos > 0 else BUY
        o = Order(side=side, qty=abs(pos), type=OrderType.MARKET,
                  tag=tag, is_exit=True)
        self._fill(o, idx)

    # ---------------- span resolution ----------------

    def resolve_span(self, i0: int, i1: int) -> None:
        """Resolve fills and mark equity over trade events [i0, i1)."""
        if self.halted or self._day is None or i0 >= i1:
            return
        i = i0
        guard = 0
        while i < i1 and self.working:
            best_k, best_o = None, None
            for o in self.working:
                k = self._trigger_index(o, i, i1)
                if k is not None and (best_k is None or k < best_k):
                    best_k, best_o = k, o
            if best_o is None:
                break
            self._mark(i, best_k)
            if self.halted:
                return
            self._fill(best_o, best_k)
            i = best_k
            guard += 1
            if guard > 10000:
                raise RuntimeError("resolve_span: runaway fill loop")
        self._mark(i, i1)

    def resolve_span_ticks(self, i0: int, i1: int, on_tick) -> None:
        """resolve_span variant that calls on_tick(ts, price, idx) after each
        trade event, so orders a strategy submits inside on_tick fill on LATER
        events (no look-ahead). Fills/marks are identical to resolve_span when
        on_tick submits nothing — only finer-grained. Per-event Python, so
        slower; the engine uses it only when a Strategy defines on_tick.
        """
        if self.halted or self._day is None or i0 >= i1:
            return
        d = self._day
        i = i0
        for e in range(i0, i1):
            # fill every working order that triggers at or before event e
            while self.working:
                best_k, best_o = None, None
                for o in self.working:
                    k = self._trigger_index(o, i, e + 1)
                    if k is not None and (best_k is None or k < best_k):
                        best_k, best_o = k, o
                if best_o is None:
                    break
                self._mark(i, best_k)
                if self.halted:
                    return
                self._fill(best_o, best_k)
                i = best_k
            self._mark(i, e + 1)
            if self.halted:
                return
            i = e + 1
            on_tick(int(d.ts[e]), float(d.price[e]), e)

    def _trigger_index(self, o: Order, i: int, i1: int) -> int | None:
        d = self._day
        p = d.price
        if o.type is OrderType.MARKET:
            return i
        if o.type is OrderType.LIMIT:
            quote = d.ask[i] if o.side == BUY else d.bid[i]
            if not np.isnan(quote) and (
                    quote <= o.price if o.side == BUY else quote >= o.price):
                return i  # marketable at first evaluation
            seg = p[i:i1]
            hit = seg < o.price if o.side == BUY else seg > o.price
        else:  # STOP
            seg = p[i:i1]
            hit = seg >= o.price if o.side == BUY else seg <= o.price
        k = int(np.argmax(hit))
        return i + k if hit[k if k < len(hit) else 0] else None

    def _fill_price(self, o: Order, idx: int) -> float:
        d = self._day
        if o.type is OrderType.LIMIT:
            quote = d.ask[idx] if o.side == BUY else d.bid[idx]
            if not np.isnan(quote) and (
                    quote <= o.price if o.side == BUY else quote >= o.price):
                return float(quote)     # marketable limit: fill at the quote
            return float(o.price)
        # market / triggered stop: prevailing quote +/- slippage
        quote = d.ask[idx] if o.side == BUY else d.bid[idx]
        base = float(quote) if not np.isnan(quote) else float(d.price[idx])
        px = base + o.side * self.slippage
        if o.type is OrderType.STOP:
            # never fill a stop better than its trigger price
            px = max(px, o.price) if o.side == BUY else min(px, o.price)
        return px

    def _fill(self, o: Order, idx: int) -> None:
        d = self._day
        px = self._fill_price(o, idx)
        ts = int(d.ts[idx])
        o.state = OrderState.FILLED
        o.fill_price, o.fill_ts = px, ts
        if o in self.working:
            self.working.remove(o)

        # Apex max-position guard: clamp this fill so the resulting net
        # position never exceeds the cap. Correct for both adds and reversals
        # (a reversal only clamps if the far side would overshoot the cap).
        pos_before = self.account.position
        after = pos_before + o.side * o.qty
        if (self.max_position is not None and not o.is_exit
                and abs(after) > self.max_position):
            o.qty = max(0, o.qty - (abs(after) - self.max_position))
            if not self._cap_warned:
                print(f"  [broker] max-position cap {self.max_position} hit — "
                      f"clamping entry to keep |position| <= cap")
                self._cap_warned = True
            if o.qty == 0:
                o.state = OrderState.CANCELLED
                return
            after = pos_before + o.side * o.qty

        # split fills that reverse through flat so trade recording stays clean
        parts = [(o.qty, pos_before)]
        if pos_before != 0 and after != 0 and (after > 0) != (pos_before > 0):
            parts = [(abs(pos_before), pos_before), (abs(after), 0)]
        for qty, pb in parts:
            self.account.apply_fill(o.side, qty, px)
            self.recorder.on_fill(self.account, ts, o.side, qty, px,
                                  o.tag, pb)

        fill = Fill(order=o, ts=ts, price=px, qty=o.qty, side=o.side,
                    commission=self.spec.commission_side * o.qty)
        self.fills.append(fill)

        # OCO sibling cancellation
        if o.oco_id is not None:
            for sib in list(self.working):
                if sib.id == o.oco_id:
                    self.cancel(sib)

        # bracket children activation
        br = self._brackets.pop(o.id, None)
        if br is not None and self.account.position != 0:
            exit_side = -o.side
            stop_o = target_o = None
            if br.stop_ticks:
                stop_px = px - o.side * br.stop_ticks * self.spec.tick_size
                stop_o = Order(side=exit_side, qty=o.qty, type=OrderType.STOP,
                               price=stop_px, tag="stop", is_exit=True)
            if br.target_ticks:
                tgt_px = px + o.side * br.target_ticks * self.spec.tick_size
                target_o = Order(side=exit_side, qty=o.qty,
                                 type=OrderType.LIMIT, price=tgt_px,
                                 tag="target", is_exit=True)
            if stop_o and target_o:
                stop_o.oco_id, target_o.oco_id = target_o.id, stop_o.id
            for child in (stop_o, target_o):
                if child is not None:
                    self.submit(child)

        if self.on_fill:
            self.on_fill(fill)

    # ---------------- equity marking ----------------

    def _mark(self, i: int, k: int) -> None:
        """Mark equity/prop/trade-extremes over events [i, k) with constant position."""
        if i >= k or self._day is None:
            return
        d = self._day
        pos = self.account.position
        if pos == 0:
            if self.prop is not None:
                breach = self.prop.update_scalar(self.account.balance,
                                                 int(d.ts[i]))
                if breach and self.prop.cfg.halt_on_breach:
                    self.halted = True
            return
        seg = d.price[i:k]
        unreal = pos * (seg - self.account.avg_price) * self.spec.point_value
        self.recorder.mark(float(unreal.min()), float(unreal.max()))
        if self.prop is not None:
            eq = self.account.balance + unreal
            breach = self.prop.update_series(eq, d.ts[i:k])
            if breach and self.prop.cfg.halt_on_breach:
                # flatten at the breach event; engine sees halted and stops
                self.halted = True
                floor = self.prop.floor
                hit = eq <= floor
                idx = i + (int(np.argmax(hit)) if hit.any() else k - 1 - i)
                self.flatten(idx, tag="prop-halt")
