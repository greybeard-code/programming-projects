"""Strategy base class and bar history container."""
from __future__ import annotations

from dataclasses import dataclass

import numpy as np

from .orders import BUY, SELL, BracketSpec, Fill, Order, OrderType


@dataclass
class Bar:
    ts: int          # close time, ns UTC
    open: float
    high: float
    low: float
    close: float
    volume: int
    index: int       # global bar index across the run


class _GrowArray:
    def __init__(self, dtype):
        self._buf = np.empty(4096, dtype=dtype)
        self.n = 0

    def append(self, v):
        if self.n == len(self._buf):
            self._buf = np.concatenate([self._buf, np.empty_like(self._buf)])
        self._buf[self.n] = v
        self.n += 1

    @property
    def values(self) -> np.ndarray:
        return self._buf[:self.n]


class BarHistory:
    """Append-only bar series spanning the whole run (all days)."""

    def __init__(self):
        self.ts = _GrowArray("int64")
        self.open = _GrowArray("float64")
        self.high = _GrowArray("float64")
        self.low = _GrowArray("float64")
        self.close = _GrowArray("float64")
        self.volume = _GrowArray("int64")

    def append(self, bar: Bar) -> None:
        self.ts.append(bar.ts)
        self.open.append(bar.open)
        self.high.append(bar.high)
        self.low.append(bar.low)
        self.close.append(bar.close)
        self.volume.append(bar.volume)

    def __len__(self) -> int:
        return self.ts.n

    @property
    def closes(self) -> np.ndarray:
        return self.close.values

    @property
    def highs(self) -> np.ndarray:
        return self.high.values

    @property
    def lows(self) -> np.ndarray:
        return self.low.values


class Strategy:
    """Subclass this. Set class attributes, implement on_bar().

    Times are "HH:MM" in US/Central (CME exchange time).
    """

    symbol: str = "MNQ"
    period: str = "1m"                      # e.g. "30s", "1m", "5m", "1h"
    session: tuple[str, str] | None = ("08:30", "15:00")
    flat_at_session_end: bool = True
    qty: int = 1

    def __init__(self):
        self._broker = None      # wired by the engine
        self._account = None

    # ---- lifecycle hooks ----
    def on_start(self) -> None: ...
    def on_bar(self, bar: Bar, bars: BarHistory) -> None: ...
    def on_fill(self, fill: Fill) -> None: ...
    def on_session_end(self, date: str) -> None: ...
    def on_finish(self) -> None: ...

    # ---- state ----
    @property
    def position(self) -> int:
        return self._account.position

    @property
    def flat(self) -> bool:
        return self._account.position == 0

    @property
    def avg_price(self) -> float:
        return self._account.avg_price

    @property
    def balance(self) -> float:
        return self._account.balance

    # ---- orders ----
    def buy(self, qty: int | None = None, tag: str = "") -> Order:
        return self._enter(BUY, qty, tag)

    def sell(self, qty: int | None = None, tag: str = "") -> Order:
        return self._enter(SELL, qty, tag)

    def buy_bracket(self, qty: int | None = None, stop_ticks: float | None = None,
                    target_ticks: float | None = None, tag: str = "") -> Order:
        return self._enter(BUY, qty, tag,
                           BracketSpec(stop_ticks, target_ticks))

    def sell_bracket(self, qty: int | None = None, stop_ticks: float | None = None,
                     target_ticks: float | None = None, tag: str = "") -> Order:
        return self._enter(SELL, qty, tag,
                           BracketSpec(stop_ticks, target_ticks))

    def buy_limit(self, price: float, qty: int | None = None, tag: str = "",
                  stop_ticks: float | None = None,
                  target_ticks: float | None = None) -> Order:
        o = Order(side=BUY, qty=qty or self.qty, type=OrderType.LIMIT,
                  price=price, tag=tag)
        return self._broker.submit(o, BracketSpec(stop_ticks, target_ticks))

    def sell_limit(self, price: float, qty: int | None = None, tag: str = "",
                   stop_ticks: float | None = None,
                   target_ticks: float | None = None) -> Order:
        o = Order(side=SELL, qty=qty or self.qty, type=OrderType.LIMIT,
                  price=price, tag=tag)
        return self._broker.submit(o, BracketSpec(stop_ticks, target_ticks))

    def buy_stop(self, price: float, qty: int | None = None, tag: str = "",
                 stop_ticks: float | None = None,
                 target_ticks: float | None = None) -> Order:
        o = Order(side=BUY, qty=qty or self.qty, type=OrderType.STOP,
                  price=price, tag=tag)
        return self._broker.submit(o, BracketSpec(stop_ticks, target_ticks))

    def sell_stop(self, price: float, qty: int | None = None, tag: str = "",
                  stop_ticks: float | None = None,
                  target_ticks: float | None = None) -> Order:
        o = Order(side=SELL, qty=qty or self.qty, type=OrderType.STOP,
                  price=price, tag=tag)
        return self._broker.submit(o, BracketSpec(stop_ticks, target_ticks))

    # ---- working-order access / modification ----
    @property
    def working_orders(self) -> list[Order]:
        return list(self._broker.working)

    @property
    def stop_order(self) -> Order | None:
        """First working protective stop (exit-side stop order)."""
        for o in self._broker.working:
            if o.is_exit and o.type is OrderType.STOP:
                return o
        return None

    @property
    def target_order(self) -> Order | None:
        """First working profit target (exit-side limit order)."""
        for o in self._broker.working:
            if o.is_exit and o.type is OrderType.LIMIT:
                return o
        return None

    def move_stop(self, price: float) -> bool:
        """Move all working protective stops to `price` (e.g. trailing)."""
        moved = False
        for o in self._broker.working:
            if o.is_exit and o.type is OrderType.STOP:
                moved = self._broker.modify(o, price) or moved
        return moved

    def move_target(self, price: float) -> bool:
        moved = False
        for o in self._broker.working:
            if o.is_exit and o.type is OrderType.LIMIT:
                moved = self._broker.modify(o, price) or moved
        return moved

    def move_stop_to_breakeven(self, offset_ticks: float = 0.0) -> bool:
        """Stop to entry price +/- offset (offset in the profit direction)."""
        pos = self._account.position
        if pos == 0:
            return False
        tick = self._broker.spec.tick_size
        price = self._account.avg_price + (1 if pos > 0 else -1) * offset_ticks * tick
        return self.move_stop(price)

    def close_position(self, tag: str = "exit") -> Order | None:
        """Flatten with a market order (fills next tick)."""
        pos = self._account.position
        if pos == 0:
            return None
        o = Order(side=SELL if pos > 0 else BUY, qty=abs(pos),
                  type=OrderType.MARKET, tag=tag, is_exit=True)
        return self._broker.submit(o)

    def cancel_all(self) -> None:
        self._broker.cancel_all()

    def _enter(self, side: int, qty: int | None, tag: str,
               bracket: BracketSpec | None = None) -> Order:
        o = Order(side=side, qty=qty or self.qty, type=OrderType.MARKET,
                  tag=tag or ("long" if side == BUY else "short"))
        return self._broker.submit(o, bracket)


def parse_period(period: str) -> int:
    """'30s' -> 30, '1m' -> 60, '5m' -> 300, '1h' -> 3600."""
    period = period.strip().lower()
    units = {"s": 1, "m": 60, "h": 3600}
    if period and period[-1] in units:
        return int(float(period[:-1]) * units[period[-1]])
    return int(period)  # plain seconds
