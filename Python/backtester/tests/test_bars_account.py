import numpy as np
import pytest

from backtester.account import Account
from backtester.data import build_bars
from backtester.strategy import parse_period

from conftest import make_day


def test_parse_period():
    assert parse_period("30s") == 30
    assert parse_period("1m") == 60
    assert parse_period("5m") == 300
    assert parse_period("1h") == 3600
    assert parse_period("45") == 45


def test_build_bars_basic():
    # 10 trades, 1 per second starting at epoch+1000s; 5s bars -> 2 bars
    day = make_day([1, 2, 3, 4, 5, 6, 7, 8, 9, 10],
                   start_ts=1_000_000_000_000)
    bars = build_bars(day, 5)
    assert len(bars) == 2
    assert bars.open[0] == 1 and bars.close[0] == 5
    assert bars.high[0] == 5 and bars.low[0] == 1
    assert bars.open[1] == 6 and bars.close[1] == 10
    assert bars.volume[0] == 5
    # spans index back into the trade arrays
    assert bars.i0[0] == 0 and bars.i1[0] == 5
    assert bars.i0[1] == 5 and bars.i1[1] == 10
    # bar timestamp is the right edge
    assert bars.ts_end[0] == 1_000_000_000_000 + 5 * 1_000_000_000


def test_build_bars_gap_skips_empty():
    ts0 = 1_000_000_000_000
    day = make_day([1.0, 2.0, 3.0], start_ts=ts0)
    # push third trade far ahead: 100s gap
    day.ts[2] = ts0 + 100 * 1_000_000_000
    bars = build_bars(day, 5)
    assert len(bars) == 2          # empty bars omitted
    assert bars.i0[1] == 2 and bars.i1[1] == 3


def test_account_scale_in_out(spec):
    a = Account(spec, 10_000)
    a.apply_fill(1, 2, 100.0)      # long 2 @ 100
    a.apply_fill(1, 2, 102.0)      # long 4 @ avg 101
    assert a.position == 4
    assert a.avg_price == pytest.approx(101.0)
    pnl = a.apply_fill(-1, 4, 103.0)
    # 2 pts * pv 2 * 4 = $16, minus commission 0.5*4 = $2 -> +14 on this fill
    assert pnl == pytest.approx(16.0 - 2.0)
    assert a.position == 0


def test_account_reverse_through_flat(spec):
    a = Account(spec, 10_000)
    a.apply_fill(1, 1, 100.0)
    a.apply_fill(-1, 3, 101.0)     # close 1 (+2 gross), open short 2 @ 101
    assert a.position == -2
    assert a.avg_price == pytest.approx(101.0)
