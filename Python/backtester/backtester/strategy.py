"""Strategy base class and bar history container."""
from __future__ import annotations

import re
from dataclasses import dataclass

import numpy as np

from .orders import BUY, SELL, BracketSpec, Fill, Order, OrderType


@dataclass
class Bar:
    ts: int              # close time, ns UTC
    open: float
    high: float
    low: float
    close: float
    volume: int
    index: int           # global bar index across the run
    buy_volume: int = 0  # aggressor buys (trades at/above the ask)
    sell_volume: int = 0 # aggressor sells (trades at/below the bid)

    @property
    def delta(self) -> int:
        """Order-flow delta: aggressor buy volume minus sell volume."""
        return self.buy_volume - self.sell_volume


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
        self.delta = _GrowArray("int64")
        self.cum_delta = _GrowArray("int64")   # session-cumulative delta
        self._cum = 0

    def append(self, bar: Bar) -> None:
        self.ts.append(bar.ts)
        self.open.append(bar.open)
        self.high.append(bar.high)
        self.low.append(bar.low)
        self.close.append(bar.close)
        self.volume.append(bar.volume)
        self.delta.append(bar.delta)
        self._cum += bar.delta
        self.cum_delta.append(self._cum)

    def reset_cum_delta(self) -> None:
        """Called by the engine at each session start."""
        self._cum = 0

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

    Times are "HH:MM" in US/Eastern (the user's PC/NT8 timezone).
    """

    symbol: str = "MNQ"
    period: str = "1m"                      # e.g. "30s", "1m", "5m", "1h"
    session: tuple[str, str] | None = ("09:30", "16:00")
    flat_at_session_end: bool = True
    qty: int = 1
    # Net-position cap (contracts). None = use the symbol's Apex cap
    # (6 minis / 60 micros); 0 disables the guard entirely.
    max_position: int | None = None
    # Apex minimum trade duration (seconds). > 0 makes strategy-initiated
    # exits (close_position / reversals) wait until the position is this old;
    # hard bracket stops and session/DLL flattens are NOT gated. 0 = off.
    min_hold_s: float = 0.0
    # Extra bar series for multi-timeframe logic, e.g. ["5m", "15m"]. Each is
    # a period string (same grammar as `period`). During on_bar, read the
    # completed secondary bars via self.secondary(period) — only bars that
    # closed at/before the current primary bar are present (no look-ahead).
    secondary_periods: list[str] = []

    def __init__(self):
        self._broker = None      # wired by the engine
        self._account = None
        self._now_ts = 0         # current bar close ts, set by the engine
        self._secondary = {}     # period -> BarHistory, wired by the engine

    # ---- lifecycle hooks ----
    def on_start(self) -> None: ...
    def on_bar(self, bar: Bar, bars: BarHistory) -> None: ...
    def on_fill(self, fill: Fill) -> None: ...
    def on_session_end(self, date: str) -> None: ...
    def on_finish(self) -> None: ...
    # Optional: fires when a secondary-series bar completes, just before the
    # primary on_bar that first sees it. Override to update HTF indicators.
    def on_secondary_bar(self, bar: Bar, bars: BarHistory, period: str) -> None: ...
    # Optional: fires per reduced trade event within a bar span. Defining it
    # switches the engine to a slower per-event resolver; orders submitted
    # here fill on later events (no look-ahead). Read-only price at `ts`.
    def on_tick(self, ts: int, price: float, index: int) -> None: ...

    def secondary(self, period: str) -> BarHistory:
        """Completed bars of a declared secondary series (as of now)."""
        return self._secondary[period]

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

    def position_age_s(self) -> float:
        """Seconds the current open position has been held (inf if flat)."""
        rec = self._broker.recorder
        if rec.open is None or self._account.position == 0:
            return float("inf")
        return (self._now_ts - rec.open.entry_ts) / 1e9

    def hold_ok(self) -> bool:
        """True if the Apex min-hold has elapsed (or is disabled)."""
        return self.min_hold_s <= 0 or self.position_age_s() >= self.min_hold_s

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

    def vol_target_contracts(self, daily_atr_points: float,
                             annual_vol_target: float = 0.15,
                             max_contracts: int | None = None) -> int:
        """Carver vol-target size from current balance (see sizing.py).
        Pass a DAILY ATR in points, not an intraday one."""
        from .sizing import carver_contracts
        return carver_contracts(self._account.balance, daily_atr_points,
                                self._broker.spec.point_value,
                                annual_vol_target,
                                max_contracts=max_contracts)

    def close_position(self, tag: str = "exit",
                       force: bool = False) -> Order | None:
        """Flatten with a market order (fills next tick).

        Blocked (returns None) if the Apex min-hold hasn't elapsed, unless
        `force` (risk stand-downs like a daily-loss lock pass force=True).
        """
        pos = self._account.position
        if pos == 0:
            return None
        if not force and not self.hold_ok():
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


@dataclass(frozen=True)
class BarSpec:
    """Parsed bar type. kind: 'time' | 'tick' | 'renko'."""
    kind: str
    seconds: int = 0          # time bars
    ticks: int = 0            # tick-count bars: trades per bar
    brick_ticks: int = 0      # renko: bar body height in ticks
    trend_ticks: int = 0      # renko: with-trend close distance from prev close

    @property
    def key(self) -> str:
        if self.kind == "time":
            return f"{self.seconds}s"
        if self.kind == "tick":
            return f"{self.ticks}t"
        return f"r{self.brick_ticks}-{self.trend_ticks}"


def parse_barspec(period: str) -> BarSpec:
    """'30s'/'1m'/'5m'/'1h' time bars; '500t' tick bars; 'r8-4' ninZaRenko
    (brick 8 ticks, trend threshold 4; 'r8' defaults trend to brick/2)."""
    p = period.strip().lower()
    m = re.fullmatch(r"r(\d+)(?:-(\d+))?", p)
    if m:
        brick = int(m.group(1))
        trend = int(m.group(2)) if m.group(2) else max(1, brick // 2)
        if trend > brick:
            raise ValueError(
                f"renko trend threshold ({trend}) must not exceed brick "
                f"size ({brick}) — manual best practice is brick a multiple "
                "of trend, e.g. r8-4, r15-5, r20-5")
        return BarSpec("renko", brick_ticks=brick, trend_ticks=trend)
    m = re.fullmatch(r"(\d+)t", p)
    if m:
        return BarSpec("tick", ticks=int(m.group(1)))
    m = re.fullmatch(r"(\d+(?:\.\d+)?)([smh])", p)
    if m:
        units = {"s": 1, "m": 60, "h": 3600}
        return BarSpec("time", seconds=int(float(m.group(1)) * units[m.group(2)]))
    if p.isdigit():
        return BarSpec("time", seconds=int(p))
    raise ValueError(f"Unrecognized bar period {period!r} "
                     "(examples: 30s, 1m, 5m, 500t, r8, r8x3)")


def parse_period(period: str) -> int:
    """Back-compat: seconds of a time-bar spec."""
    spec = parse_barspec(period)
    if spec.kind != "time":
        raise ValueError(f"{period!r} is not a time-bar period")
    return spec.seconds
