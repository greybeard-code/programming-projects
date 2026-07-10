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

# All user-facing session times are US/Eastern (user preference — their PC,
# NT8, and trading community all run ET). Internals remain ns UTC.
SESSION_TZ = ZoneInfo("America/New_York")


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
    """UTC ns of the ET session times on the file's calendar date.

    Returns (start_ns, end_ns, wrap). wrap=True means an overnight session
    (start > end, e.g. Globex 18:00 -> 16:55 ET): the trading day that
    *ends* on this date began at `start` on the PREVIOUS date, and a new one
    begins at `start` on this date.
    """
    d = datetime.strptime(date, "%Y%m%d")

    def to_ns(hhmm: str) -> int:
        h, m = map(int, hhmm.split(":"))
        local = datetime(d.year, d.month, d.day, h, m, tzinfo=SESSION_TZ)
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
                 max_position: int | None = None,
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
        # Net-position cap resolution: explicit arg > strategy attr > symbol's
        # Apex default (6 minis / 60 micros). A resolved 0 disables the guard.
        mp = max_position if max_position is not None \
            else getattr(strategy, "max_position", None)
        if mp is None:
            mp = self.spec.apex_max_position
        self.broker = SimBroker(self.spec, self.account, self.recorder,
                                self.prop, slippage_ticks,
                                on_fill=self._notify_fill,
                                max_position=mp)
        self.daily_loss_limit = daily_loss_limit
        self.progress = progress
        self._in_bar_cb = False

    def _notify_fill(self, fill) -> None:
        self.strategy.on_fill(fill)

    @staticmethod
    def _advance_secondary(strat, primary_ts, sec_periods, sec_bars,
                           sec_cursor, sec_hist, sec_index, has_on_sec):
        """Append every secondary bar that closed at/before the current
        primary bar (ts_end <= primary_ts) — no look-ahead — and fire
        on_secondary_bar for each, before the primary on_bar runs."""
        for p in sec_periods:
            sb = sec_bars[p]
            h = sec_hist[p]
            cur = sec_cursor[p]
            n = len(sb)
            while cur < n and int(sb.ts_end[cur]) <= primary_ts:
                sbar = Bar(ts=int(sb.ts_end[cur]),
                           open=float(sb.open[cur]), high=float(sb.high[cur]),
                           low=float(sb.low[cur]), close=float(sb.close[cur]),
                           volume=int(sb.volume[cur]), index=sec_index[p],
                           buy_volume=int(sb.buy_volume[cur]),
                           sell_volume=int(sb.sell_volume[cur]))
                h.append(sbar)
                sec_index[p] += 1
                if has_on_sec:
                    strat.on_secondary_bar(sbar, h, p)
                cur += 1
            sec_cursor[p] = cur

    def run(self) -> Result:
        t_start = time.perf_counter()
        strat = self.strategy
        strat._broker = self.broker
        strat._account = self.account

        # secondary (multi-timeframe) series + optional on_tick. Both stay
        # entirely off the fast path unless a strategy opts in.
        sec_periods = list(getattr(strat, "secondary_periods", []) or [])
        sec_specs = {p: parse_barspec(p) for p in sec_periods}
        sec_hist = {p: BarHistory() for p in sec_periods}
        sec_index = {p: 0 for p in sec_periods}
        strat._secondary = sec_hist
        has_on_sec = type(strat).on_secondary_bar is not Strategy.on_secondary_bar
        has_on_tick = type(strat).on_tick is not Strategy.on_tick

        def _tick_cb(ts, price, idx):
            strat._now_ts = ts
            strat.on_tick(ts, price, idx)

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
            sec_bars = {p: self.catalog.load_bars(self.symbol, date,
                                                  sec_specs[p],
                                                  self.spec.tick_size, day)
                        for p in sec_periods}
            sec_cursor = {p: 0 for p in sec_periods}
            self.broker.begin_day(day)

            for seg_idx, day_end in _segments(strat.session, date,
                                              bars.ts_end):
                if self.broker.halted:
                    break
                if not dll_pending:
                    if new_trading_day and len(seg_idx):
                        hist.reset_cum_delta()
                        for h in sec_hist.values():
                            h.reset_cum_delta()
                        new_trading_day = False
                    for j in seg_idx:
                        if has_on_tick:
                            self.broker.resolve_span_ticks(
                                int(bars.i0[j]), int(bars.i1[j]), _tick_cb)
                        else:
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
                        strat._now_ts = bar.ts
                        if sec_periods:
                            self._advance_secondary(
                                strat, bar.ts, sec_periods, sec_bars,
                                sec_cursor, sec_hist, sec_index, has_on_sec)
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
