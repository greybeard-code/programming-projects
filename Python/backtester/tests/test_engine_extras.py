import numpy as np
import pytest

from backtester.data import build_time_bars
from backtester.engine import Backtest
from backtester.sizing import carver_contracts
from backtester.strategy import Strategy

from conftest import make_day


def test_carver_contracts():
    # 50K * 15% / 16 = $468.75 daily vol target; MNQ pv=2:
    # ATR 100 pts -> $200/contract -> 2 contracts
    assert carver_contracts(50_000, 100.0, 2.0) == 2
    assert carver_contracts(50_000, 500.0, 2.0) == 1          # floor
    assert carver_contracts(50_000, 10.0, 2.0, max_contracts=5) == 5
    assert carver_contracts(50_000, 0.0, 2.0) == 1            # degenerate


class _StubCatalog:
    def __init__(self, day):
        self._day = day

    def days(self, symbol, start=None, end=None):
        return [self._day.date]

    def load_day(self, symbol, date):
        return self._day

    def load_bars(self, symbol, date, spec, tick_size, day=None):
        return build_time_bars(self._day, spec.seconds)


class BuyOnce(Strategy):
    symbol = "MNQ"
    period = "1m"
    session = None
    qty = 1

    def on_bar(self, bar, bars):
        if self.flat and bar.index == 0:
            self.buy()


def test_daily_loss_limit_flattens_and_stands_down():
    # bar 1: flat at 100; then decline 0.25/s for 4 minutes (-60 pts total)
    prices = [100.0] * 60 + [100.0 - 0.25 * i for i in range(1, 241)]
    day = make_day(prices)
    strat = BuyOnce()
    bt = Backtest(strat, apex=None, daily_loss_limit=30.0, progress=False)
    bt.catalog = _StubCatalog(day)
    res = bt.run()
    assert res.dll_days == [day.date]
    assert len(res.trades) == 1
    t = res.trades[0]
    assert t.exit_tag == "dll"
    # loss capped near the limit (checked at bar closes): pv=2, so -$30 is
    # 15 points; one bar of decline = 15 pts -> stopped at first check beyond
    assert t.pnl <= -30.0
    assert t.pnl > -80.0                     # but not run to the -$120 bottom


def test_no_dll_when_not_hit():
    prices = [100.0] * 60 + [100.0 + 0.25 * (i % 4) for i in range(240)]
    day = make_day(prices)
    strat = BuyOnce()
    bt = Backtest(strat, apex=None, daily_loss_limit=500.0, progress=False)
    bt.catalog = _StubCatalog(day)
    res = bt.run()
    assert res.dll_days == []
