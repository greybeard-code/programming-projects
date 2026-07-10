"""Position/P&L accounting, round-trip trade recording, prop-firm trailing threshold."""
from __future__ import annotations

from dataclasses import dataclass, field

import numpy as np

from .contracts import ContractSpec


class Account:
    """Signed-position futures account. `realized` is net of commissions."""

    def __init__(self, spec: ContractSpec, start_balance: float):
        self.spec = spec
        self.start_balance = float(start_balance)
        self.position = 0          # signed contracts
        self.avg_price = 0.0
        self.realized = 0.0        # dollars, net of commissions
        self.total_commission = 0.0

    @property
    def balance(self) -> float:
        return self.start_balance + self.realized

    def unrealized(self, last_price: float) -> float:
        if self.position == 0:
            return 0.0
        return self.position * (last_price - self.avg_price) * self.spec.point_value

    def equity(self, last_price: float) -> float:
        return self.balance + self.unrealized(last_price)

    def apply_fill(self, side: int, qty: int, price: float) -> float:
        """Apply a fill, return realized P&L delta (net of commission)."""
        commission = self.spec.commission_side * qty
        self.total_commission += commission
        pnl = -commission
        signed = side * qty
        pos = self.position
        if pos == 0 or (pos > 0) == (signed > 0):
            # opening / adding
            new_pos = pos + signed
            self.avg_price = (self.avg_price * pos + price * signed) / new_pos
            self.position = new_pos
        else:
            closing = min(abs(signed), abs(pos))
            pnl += (price - self.avg_price) * (1 if pos > 0 else -1) \
                * closing * self.spec.point_value
            self.position = pos + signed
            if self.position == 0:
                self.avg_price = 0.0
            elif (self.position > 0) != (pos > 0):
                # reversed through flat: remainder opens at fill price
                self.avg_price = price
        self.realized += pnl
        return pnl


@dataclass
class Trade:
    """One round trip: position leaves flat, later returns to flat."""
    direction: int                 # +1 long / -1 short
    qty: int                       # max contracts held during the trip
    entry_ts: int = 0
    exit_ts: int = 0
    entry_price: float = 0.0       # qty-weighted average
    exit_price: float = 0.0
    pnl: float = 0.0               # net of commission, dollars
    commission: float = 0.0
    mae: float = 0.0               # worst open P&L during trade, dollars (<= 0)
    mfe: float = 0.0               # best open P&L during trade, dollars (>= 0)
    entry_tag: str = ""
    exit_tag: str = ""


class TradeRecorder:
    """Aggregates fills into round-trip trades."""

    def __init__(self, spec: ContractSpec):
        self.spec = spec
        self.trades: list[Trade] = []
        self.open: Trade | None = None
        self._entry_qty = 0
        self._exit_qty = 0
        self._realized_at_open = 0.0

    def on_fill(self, account: Account, ts: int, side: int, qty: int,
                price: float, tag: str, pos_before: int) -> None:
        if pos_before == 0 and self.open is None:
            self.open = Trade(direction=side, qty=0, entry_ts=ts, entry_tag=tag)
            self._entry_qty = self._exit_qty = 0
            self._realized_at_open = account.realized + account.spec.commission_side * qty
            # realized_at_open is captured before this fill's commission lands;
            # apply_fill already ran, so add the entry commission back.
        t = self.open
        if t is None:
            return
        if side == t.direction:
            t.entry_price = (t.entry_price * self._entry_qty + price * qty) \
                / (self._entry_qty + qty)
            self._entry_qty += qty
            t.qty = max(t.qty, abs(account.position))
        else:
            t.exit_price = (t.exit_price * self._exit_qty + price * qty) \
                / (self._exit_qty + qty)
            self._exit_qty += qty
            if not t.exit_tag:
                t.exit_tag = tag
        if account.position == 0:
            t.exit_ts = ts
            t.pnl = account.realized - self._realized_at_open
            t.commission = self.spec.commission_rt / 2 * (self._entry_qty + self._exit_qty)
            self.trades.append(t)
            self.open = None

    def mark(self, unreal_min: float, unreal_max: float) -> None:
        if self.open is not None:
            self.open.mae = min(self.open.mae, unreal_min)
            self.open.mfe = max(self.open.mfe, unreal_max)


@dataclass
class PropFirmConfig:
    threshold: float = 2000.0      # trailing drawdown amount (Apex real floor)
    lock_buffer: float = 100.0     # floor freezes at start_balance + lock_buffer
    lock: bool = True              # False = trail forever (some eval styles)
    halt_on_breach: bool = False   # True = stop the backtest at breach


class PropFirmTracker:
    """Prop-firm trailing threshold (models the Apex rule set).

    The floor trails the *intratrade* equity peak (unrealized included) by
    `threshold`, and — if lock=True — freezes once it reaches
    start_balance + lock_buffer. A breach is equity touching the floor.
    """

    def __init__(self, cfg: PropFirmConfig, start_balance: float):
        self.cfg = cfg
        self.start = start_balance
        self.peak = start_balance
        self.breached = False
        self.breach_ts: int | None = None
        self.breach_equity: float | None = None
        self.min_headroom = float("inf")   # min(equity - floor) over the run

    @property
    def floor(self) -> float:
        f = self.peak - self.cfg.threshold
        if self.cfg.lock:
            f = min(f, self.start + self.cfg.lock_buffer)
        return f

    def update_scalar(self, equity: float, ts: int) -> bool:
        return self.update_series(np.asarray([equity]), np.asarray([ts]))

    def update_series(self, equity: np.ndarray, ts: np.ndarray) -> bool:
        """Feed a chronological equity segment. Returns True on first breach."""
        if len(equity) == 0:
            return False
        peak_run = np.maximum.accumulate(np.maximum(equity, self.peak))
        floor_run = peak_run - self.cfg.threshold
        if self.cfg.lock:
            floor_run = np.minimum(floor_run, self.start + self.cfg.lock_buffer)
        headroom = equity - floor_run
        m = float(headroom.min())
        self.min_headroom = min(self.min_headroom, m)
        first_breach = False
        if not self.breached and m <= 0:
            k = int(np.argmax(headroom <= 0))
            self.breached = True
            self.breach_ts = int(ts[k])
            self.breach_equity = float(equity[k])
            first_breach = True
        self.peak = float(peak_run[-1])
        return first_breach
