"""Order objects and lifecycle."""
from __future__ import annotations

import itertools
from dataclasses import dataclass, field
from enum import Enum

BUY, SELL = 1, -1


class OrderType(Enum):
    MARKET = "market"
    LIMIT = "limit"
    STOP = "stop"


class OrderState(Enum):
    WORKING = "working"
    FILLED = "filled"
    CANCELLED = "cancelled"


_ids = itertools.count(1)


@dataclass
class Order:
    side: int                      # BUY=+1 / SELL=-1
    qty: int
    type: OrderType
    price: float = float("nan")    # limit or stop price
    tag: str = ""
    id: int = field(default_factory=lambda: next(_ids))
    state: OrderState = OrderState.WORKING
    oco_id: int | None = None      # cancel this order when sibling fills
    is_exit: bool = False          # exit orders may flatten, never reverse
    fill_price: float = float("nan")
    fill_ts: int = 0

    def __repr__(self) -> str:
        px = "" if self.type is OrderType.MARKET else f" @{self.price}"
        s = "BUY" if self.side == BUY else "SELL"
        return (f"Order#{self.id} {s} {self.qty} {self.type.value}{px}"
                f" [{self.state.value}]{' ' + self.tag if self.tag else ''}")


@dataclass
class Fill:
    order: Order
    ts: int
    price: float
    qty: int
    side: int
    commission: float


@dataclass
class BracketSpec:
    """Attached to an entry order; children are created at entry fill."""
    stop_ticks: float | None = None
    target_ticks: float | None = None
