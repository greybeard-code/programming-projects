"""Backtest engine: replays days chronologically, drives broker + strategy."""
from __future__ import annotations

import time
from dataclasses import dataclass, field
from datetime import datetime, timedelta
from zoneinfo import ZoneInfo

import numpy as np

from .account import Account, PropFirmConfig, PropFirmTracker, Trade, TradeRecorder
from .broker import SimBroker
from .contracts import ContractSpec, get_spec
from .data import Catalog
from .strategy import Bar, BarHistory, Strategy, parse_barspec

CT = ZoneInfo("America/Chicago")


@dataclass
class Result:
    symbol: str
    spec: ContractSpec
    start_balance: float
    period: str
    days: list[str]
    trades: list[Trade]
    fills: int
    # sampled at bar closes:
    equity_ts: np.ndarray = field(default_factory=lambda: np.array([], "int64"))
    equity: np.ndarray = field(default_factory=lambda: np.array([]))
    prop_floor: np.ndarray = field(default_factory=lambda: np.array([]))
    daily_pnl: dict[str, float] = field(default_factory=dict)
    prop: PropFirmTracker | None = None
    total_commission: float = 0.0
    runtime_s: float = 0.0
    strategy_name: str = ""
    halted_on: str | None = None
    dll_days: list[str] = field(default_factory=list)


def _session_bounds_utc(date: str,
                        session: tuple[str, str]) -> tuple[int, int, bool]:
    """UTC ns of the CT session times on the file's calendar date.

    Returns (start_ns, end_ns, wrap). wrap=True means an overnight session
    (start > end, e.g. Globex 17:00 -> 15:55): the trading day that *ends* on
    this date began at `start` on the PREVIOUS date, and a new one begins at
    `start` on this date.
    """
    d = datetime.strptime(date, "%Y%m%d")

    def to_ns(hhmm: str) -> int:
        h, m = map(int, hhmm.split(":"))
        local = datetime(d.year, d.month, d.day, h, m, tzinfo=CT)
        return int(local.timestamp() * 1e9)

    start, end = to_ns(session[0]), to_ns(session[1])
    return start, end, end <= start


def _segments(session: tuple[str, str] | None, date: str,
              ts_end: np.ndarray) -> list[tuple[np.ndarray, bool]]:
    """Split one day file's bars into (indices, is_trading_day_end) segments.

    - session None: whole file, day ends with the file.
    - normal session (start < end): one in-window segment, day ends there.
    - overnight session: bars up to `end` finish the trading day that started
      yesterday (day_end=True); bars from `start` on begin the next trading
      day, which carries across the UTC-midnight file boundary
      (day_end=False — no flatten, orders stay working).
    """
    n = len(ts_end)
    if session is None:
        return [(np.arange(n), True)]
    s_ns, e_ns, wrap = _session_bounds_utc(date, session)
    if not wrap:
        idx = np.flatnonzero((ts_end > s_ns) & (ts_end <= e_ns))
        return [(idx, True)] if len(idx) else []
    out: list[tuple[np.ndarray, bool]] = []
    idx1 = np.flatnonzero(ts_end <= e_ns)
    if len(idx1):
        out.append((idx1, True))
    idx2 = np.flatnonzero(ts_end > s_ns)
    if len(idx2):
        out.append((idx2, False))
    return out


class Backtest:
    def __init__(self, strategy: Strategy, start: str | None = None,
                 end: str | None = None, symbol: str | None = None,
                 period: str | None = None, start_balance: float = 50_000.0,
                 prop: PropFirmConfig | None = PropFirmConfig(),
                 slippage_ticks: float = 0.0,
                 daily_loss_limit: float | None = None,
                 data_root=None, cache_root=None,
                 progress: bool = True):
        self.strategy = strategy
        self.symbol = (symbol or strategy.symbol).upper()
        self.barspec = parse_barspec(period or strategy.period)
        self.start, self.end = start, end
        self.start_balance = start_balance
        self.spec = get_spec(self.symbol)
        self.catalog = Catalog(data_root, cache_root)
        self.account = Account(self.spec, start_balance)
        self.recorder = TradeRecorder(self.spec)
        self.prop = PropFirmTracker(prop, start_balance) if prop else None
        self.broker = SimBroker(self.spec, self.account, self.recorder,
                                self.prop, slippage_ticks,
                                on_fill=self._notify_fill)
        self.daily_loss_limit = daily_loss_limit
        self.progress = progress
        self._in_bar_cb = False

    def _notify_fill(self, fill) -> None:
        self.strategy.on_fill(fill)

    def run(self) -> Result:
        t_start = time.perf_counter()
        strat = self.strategy
        strat._broker = self.broker
        strat._account = self.account

        days = self.catalog.days(self.symbol, self.start, self.end)
        if not days:
            raise FileNotFoundError(
                f"No {self.symbol} days in range {self.start}..{self.end}")

        hist = BarHistory()
        eq_ts, eq, floor = [], [], []
        daily: dict[str, float] = {}
        dll_days: list[str] = []
        bar_index = 0
        halted_on = None
        day_start_balance = self.account.balance
        dll_pending = False        # DLL fired mid-trading-day; skip to flush
        new_trading_day = True
        last_bar_ref = None        # (DayL1, event idx) of last processed bar

        strat.on_start()
        for di, date in enumerate(days):
            day = self.catalog.load_day(self.symbol, date)
            if len(day) == 0:
                continue
            bars = self.catalog.load_bars(self.symbol, date, self.barspec,
                                          self.spec.tick_size, day)
            if len(bars) == 0:
                continue
            self.broker.begin_day(day)

            for seg_idx, day_end in _segments(strat.session, date,
                                              bars.ts_end):
                if self.broker.halted:
                    break
                if not dll_pending:
                    if new_trading_day and len(seg_idx):
                        hist.reset_cum_delta()
                        new_trading_day = False
                    for j in seg_idx:
                        self.broker.resolve_span(int(bars.i0[j]),
                                                 int(bars.i1[j]))
                        if self.broker.halted:
                            break
                        bar = Bar(ts=int(bars.ts_end[j]),
                                  open=float(bars.open[j]),
                                  high=float(bars.high[j]),
                                  low=float(bars.low[j]),
                                  close=float(bars.close[j]),
                                  volume=int(bars.volume[j]), index=bar_index,
                                  buy_volume=int(bars.buy_volume[j]),
                                  sell_volume=int(bars.sell_volume[j]))
                        hist.append(bar)
                        bar_index += 1
                        strat.on_bar(bar, hist)
                        eq_ts.append(bar.ts)
                        eq.append(self.account.equity(bar.close))
                        floor.append(self.prop.floor if self.prop
                                     else float("nan"))
                        last_bar_ref = (day, int(bars.i1[j]) - 1)

                        # daily loss limit: flatten, stand down until flush
                        if (self.daily_loss_limit is not None
                                and self.account.equity(bar.close)
                                - day_start_balance <= -self.daily_loss_limit):
                            self.broker.cancel_all()
                            self.broker.flatten(int(bars.i1[j]) - 1,
                                                tag="dll")
                            eq[-1] = self.account.equity(bar.close)
                            dll_days.append(date)
                            dll_pending = True
                            break

                if day_end:
                    # trading-day flush: cancel orders, flatten, book P&L
                    self.broker.cancel_all()
                    if (strat.flat_at_session_end and not self.broker.halted
                            and len(seg_idx)):
                        last_j = int(seg_idx[-1])
                        self.broker.flatten(int(bars.i1[last_j]) - 1,
                                            tag="eod")
                        if eq:
                            eq[-1] = self.account.equity(
                                float(bars.close[last_j]))
                    strat.on_session_end(date)
                    daily[date] = self.account.balance - day_start_balance
                    day_start_balance = self.account.balance
                    dll_pending = False
                    new_trading_day = True

            if self.broker.halted:
                halted_on = date
                break
            if self.progress and (di % 20 == 19 or di == len(days) - 1):
                print(f"  ... {date} ({di + 1}/{len(days)} days) "
                      f"balance={self.account.balance:,.2f}")

        # end of data: an overnight session can leave a live position open
        if (self.account.position != 0 and not self.broker.halted
                and last_bar_ref is not None):
            d, idx = last_bar_ref
            self.broker.begin_day(d)
            self.broker.flatten(idx, tag="end-of-data")
            if eq:
                eq[-1] = self.account.balance

        strat.on_finish()
        return Result(
            symbol=self.symbol, spec=self.spec,
            start_balance=self.start_balance, period=self.barspec.key,
            days=days, trades=self.recorder.trades,
            fills=len(self.broker.fills),
            equity_ts=np.asarray(eq_ts, dtype="int64"),
            equity=np.asarray(eq, dtype="float64"),
            prop_floor=np.asarray(floor, dtype="float64"),
            daily_pnl=daily, prop=self.prop,
            total_commission=self.account.total_commission,
            runtime_s=time.perf_counter() - t_start,
            strategy_name=type(self.strategy).__name__,
            halted_on=halted_on,
            dll_days=dll_days,
        )
