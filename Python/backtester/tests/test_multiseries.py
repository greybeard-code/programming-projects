"""Secondary (multi-timeframe) series + on_tick hook."""
import pytest

from backtester.data import build_time_bars
from backtester.engine import Backtest
from backtester.orders import BUY, Order, OrderType
from backtester.strategy import Strategy

from conftest import make_day


class _StubCatalog:
    def __init__(self, day):
        self._day = day

    def days(self, symbol, start=None, end=None):
        return [self._day.date]

    def load_day(self, symbol, date):
        return self._day

    def load_bars(self, symbol, date, spec, tick_size, day=None):
        return build_time_bars(self._day, spec.seconds)

    def load_bars_sequence(self, symbol, dates, spec, tick_size, days=None):
        return [self.load_bars(symbol, d, spec, tick_size) for d in dates]


# ---------------- secondary series ----------------

class _MTF(Strategy):
    symbol = "MNQ"
    period = "1m"
    session = None
    secondary_periods = ["5s"]

    def on_start(self):
        self.violations = 0
        self.sec_fires = 0

    def on_secondary_bar(self, bar, bars, period):
        assert period == "5s"
        self.sec_fires += 1

    def on_bar(self, bar, bars):
        h = self.secondary("5s")
        if h.ts.n:                      # newest completed secondary bar
            if int(h.ts.values[-1]) > bar.ts:   # would be look-ahead
                self.violations += 1


def test_secondary_no_lookahead_and_fires():
    # 5 minutes of 1s trades
    day = make_day([100.0 + 0.01 * i for i in range(300)])
    strat = _MTF()
    bt = Backtest(strat, prop=None, progress=False)
    bt.catalog = _StubCatalog(day)
    res = bt.run()
    assert strat.violations == 0                 # never sees a future 5s bar
    assert strat.sec_fires > 0
    # every fired secondary bar landed in the accessible history
    assert strat.secondary("5s").ts.n == strat.sec_fires


def test_no_secondary_by_default_is_untouched():
    # a plain strategy with no secondary_periods runs the fast path
    class Plain(Strategy):
        symbol = "MNQ"; period = "1m"; session = None
        def on_bar(self, bar, bars):
            pass
    day = make_day([100.0] * 120)
    bt = Backtest(Plain(), prop=None, progress=False)
    bt.catalog = _StubCatalog(day)
    res = bt.run()
    assert res.trades == []


# ---------------- on_tick ----------------

def test_on_tick_order_fills_next_event(rig):
    broker, account, _, _ = rig()
    day = make_day([100.0, 100.0, 100.0, 100.0])
    broker.begin_day(day)
    state = {"done": False}

    def on_tick(ts, price, idx):
        if idx == 1 and not state["done"]:
            broker.submit(Order(side=BUY, qty=1, type=OrderType.MARKET))
            state["done"] = True

    broker.resolve_span_ticks(0, len(day), on_tick)
    assert account.position == 1
    # submitted while processing event 1 -> fills at the NEXT event (2)
    assert broker.fills[0].order.fill_ts == int(day.ts[2])


def test_on_tick_noop_matches_resolve_span(rig):
    for use_ticks in (False, True):
        broker, account, _, _ = rig()
        day = make_day([100.0, 99.5, 99.0, 98.75, 99.5])
        broker.begin_day(day)
        broker.submit(Order(side=BUY, qty=1, type=OrderType.LIMIT, price=99.0))
        if use_ticks:
            broker.resolve_span_ticks(0, len(day), lambda *a: None)
        else:
            broker.resolve_span(0, len(day))
        assert account.position == 1
        assert broker.fills[0].price == pytest.approx(99.0)


def test_on_tick_visits_every_event(rig):
    broker, account, _, _ = rig()
    day = make_day([100.0, 100.5, 101.0, 100.5])
    broker.begin_day(day)
    seen = []
    broker.resolve_span_ticks(0, len(day),
                              lambda ts, px, idx: seen.append((idx, px)))
    assert [s[0] for s in seen] == [0, 1, 2, 3]
    assert seen[2][1] == pytest.approx(101.0)


class _TickStrat(Strategy):
    symbol = "MNQ"; period = "1m"; session = None

    def on_start(self):
        self.ticks = 0
        self.last_ts = 0

    def on_tick(self, ts, price, index):
        self.ticks += 1
        self.last_ts = ts

    def on_bar(self, bar, bars):
        pass


def test_engine_drives_on_tick():
    day = make_day([100.0] * 120)                # 120 one-second events
    s = _TickStrat()
    bt = Backtest(s, prop=None, progress=False)
    bt.catalog = _StubCatalog(day)
    bt.run()
    assert s.ticks == 120                        # one on_tick per event
    assert s.last_ts == int(day.ts[-1])          # engine set _now_ts per tick
