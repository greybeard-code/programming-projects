"""Order objects and lifecycle."""
from __future__ import annotations

import itertools
from dataclasses import dataclass, field
from enum import Enum

import numpy as np

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


@dataclass(frozen=True)
class TrailRule:
    """One auto-trail tier (NT8 ATM AutoTrailStep), in ticks.

    Once favorable excursion reaches `trigger_ticks`, the stop trails
    `dist_ticks` behind the high-water mark, advancing only in
    `freq_ticks` increments (anchored at the trigger).
    """
    trigger_ticks: float
    dist_ticks: float
    freq_ticks: float = 1.0


class StopPlan:
    """Dynamic stop schedule for an exit STOP order (auto-breakeven +
    tiered trailing, NT8-ATM style). The level only ever ratchets in the
    position's favor.

    All rules are expressed in *favorable ticks from entry*, so one formula
    serves longs and shorts: stop price = entry + direction * level * tick.
    The high-water mark persists across bar spans via `advance` (the broker
    commits each consumed span segment); `levels` is a pure function of the
    prices seen so far and never mutates state.
    """

    def __init__(self, entry_price: float, direction: int, tick_size: float,
                 stop_ticks: float, be_trigger_ticks: float = 0.0,
                 be_plus_ticks: float = 0.0,
                 trails: tuple[TrailRule, ...] = ()):
        self.entry = float(entry_price)
        self.dir = int(direction)              # +1 protects a long
        self.tick = float(tick_size)
        self.stop_ticks = float(stop_ticks)    # base stop distance
        self.be_trigger = float(be_trigger_ticks)
        self.be_plus = float(be_plus_ticks)
        self.trails = tuple(sorted(trails, key=lambda t: t.trigger_ticks))
        self.hw = self.entry                   # favorable high-water price

    def advance(self, prices: np.ndarray) -> None:
        """Commit a consumed span segment into the high-water mark."""
        if len(prices):
            ext = prices.max() if self.dir > 0 else prices.min()
            self.hw = max(self.hw, ext) if self.dir > 0 else min(self.hw, ext)

    def levels(self, prices: np.ndarray) -> np.ndarray:
        """Stop PRICE per event for this slice (committed hw + running max)."""
        if self.dir > 0:
            hw = np.maximum.accumulate(np.maximum(prices, self.hw))
        else:
            hw = np.minimum.accumulate(np.minimum(prices, self.hw))
        profit = (hw - self.entry) * self.dir / self.tick   # favorable ticks
        lvl = np.full(len(prices), -self.stop_ticks)        # base stop
        if self.be_trigger > 0:
            lvl = np.where(profit >= self.be_trigger,
                           np.maximum(lvl, self.be_plus), lvl)
        for tr in self.trails:
            t_lvl = (tr.trigger_ticks - tr.dist_ticks
                     + np.floor((profit - tr.trigger_ticks) / tr.freq_ticks)
                     * tr.freq_ticks)
            lvl = np.where(profit >= tr.trigger_ticks,
                           np.maximum(lvl, t_lvl), lvl)
        return self.entry + self.dir * lvl * self.tick


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
    plan: StopPlan | None = None   # dynamic stop schedule (BE/trail)
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
