"""Backtest engine: replays days chronologically, drives broker + strategy."""
from __future__ import annotations

import time
from dataclasses import dataclass, field
from datetime import datetime, timedelta
from zoneinfo import ZoneInfo

import numpy as np

from .account import Account, ApexConfig, ApexTracker, Trade, TradeRecorder
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
    apex_floor: np.ndarray = field(default_factory=lambda: np.array([]))
    daily_pnl: dict[str, float] = field(default_factory=dict)
    apex: ApexTracker | None = None
    total_commission: float = 0.0
    runtime_s: float = 0.0
    strategy_name: str = ""
    halted_on: str | None = None
    dll_days: list[str] = field(default_factory=list)


def _session_bounds_utc(date: str, session: tuple[str, str]) -> tuple[int, int]:
    """UTC ns bounds of the CT session window on the file's calendar date."""
    d = datetime.strptime(date, "%Y%m%d")

    def to_ns(hhmm: str) -> int:
        h, m = map(int, hhmm.split(":"))
        local = datetime(d.year, d.month, d.day, h, m, tzinfo=CT)
        return int(local.timestamp() * 1e9)

    start, end = to_ns(session[0]), to_ns(session[1])
    if end <= start:                       # overnight session, e.g. 17:00-16:00
        end += int(timedelta(days=1).total_seconds() * 1e9)
    return start, end


class Backtest:
    def __init__(self, strategy: Strategy, start: str | None = None,
                 end: str | None = None, symbol: str | None = None,
                 period: str | None = None, start_balance: float = 50_000.0,
                 apex: ApexConfig | None = ApexConfig(),
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
        self.apex = ApexTracker(apex, start_balance) if apex else None
        self.broker = SimBroker(self.spec, self.account, self.recorder,
                                self.apex, slippage_ticks,
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
            day_start_balance = self.account.balance

            if strat.session:
                s_ns, e_ns = _session_bounds_utc(date, strat.session)
                in_sess = (bars.ts_end > s_ns) & (bars.ts_end <= e_ns)
            else:
                in_sess = np.ones(len(bars), dtype=bool)
            sess_idx = np.flatnonzero(in_sess)
            if len(sess_idx) == 0:
                continue
            hist.reset_cum_delta()

            for j in sess_idx:
                self.broker.resolve_span(int(bars.i0[j]), int(bars.i1[j]))
                if self.broker.halted:
                    break
                bar = Bar(ts=int(bars.ts_end[j]), open=float(bars.open[j]),
                          high=float(bars.high[j]), low=float(bars.low[j]),
                          close=float(bars.close[j]),
                          volume=int(bars.volume[j]), index=bar_index,
                          buy_volume=int(bars.buy_volume[j]),
                          sell_volume=int(bars.sell_volume[j]))
                hist.append(bar)
                bar_index += 1
                strat.on_bar(bar, hist)
                eq_ts.append(bar.ts)
                eq.append(self.account.equity(bar.close))
                floor.append(self.apex.floor if self.apex else float("nan"))

                # daily loss limit: flatten and stand down for the day
                if (self.daily_loss_limit is not None
                        and self.account.equity(bar.close) - day_start_balance
                        <= -self.daily_loss_limit):
                    self.broker.cancel_all()
                    self.broker.flatten(int(bars.i1[j]) - 1, tag="dll")
                    eq[-1] = self.account.equity(bar.close)
                    dll_days.append(date)
                    break

            # session end: cancel working orders, flatten if configured
            last_j = int(sess_idx[-1])
            self.broker.cancel_all()
            if strat.flat_at_session_end and not self.broker.halted:
                self.broker.flatten(int(bars.i1[last_j]) - 1, tag="eod")
                if eq:
                    eq[-1] = self.account.equity(float(bars.close[last_j]))
            strat.on_session_end(date)
            daily[date] = self.account.balance - day_start_balance

            if self.broker.halted:
                halted_on = date
                break
            if self.progress and (di % 20 == 19 or di == len(days) - 1):
                print(f"  ... {date} ({di + 1}/{len(days)} days) "
                      f"balance={self.account.balance:,.2f}")

        strat.on_finish()
        return Result(
            symbol=self.symbol, spec=self.spec,
            start_balance=self.start_balance, period=self.barspec.key,
            days=days, trades=self.recorder.trades,
            fills=len(self.broker.fills),
            equity_ts=np.asarray(eq_ts, dtype="int64"),
            equity=np.asarray(eq, dtype="float64"),
            apex_floor=np.asarray(floor, dtype="float64"),
            daily_pnl=daily, apex=self.apex,
            total_commission=self.account.total_commission,
            runtime_s=time.perf_counter() - t_start,
            strategy_name=type(self.strategy).__name__,
            halted_on=halted_on,
            dll_days=dll_days,
        )
