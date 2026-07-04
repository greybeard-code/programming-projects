import numpy as np
import pytest

from backtester.account import Account
from backtester.data import build_renko_bars, build_tick_bars, build_time_bars
from backtester.strategy import parse_barspec, parse_period

from conftest import make_day


def test_parse_period():
    assert parse_period("30s") == 30
    assert parse_period("1m") == 60
    assert parse_period("5m") == 300
    assert parse_period("1h") == 3600
    assert parse_period("45") == 45


def test_parse_barspec():
    assert parse_barspec("1m") == parse_barspec("60s")
    t = parse_barspec("500t")
    assert t.kind == "tick" and t.ticks == 500 and t.key == "500t"
    r = parse_barspec("r8")
    assert r.kind == "renko" and r.brick_ticks == 8 and r.reversal_mult == 2
    assert r.key == "r8"
    r3 = parse_barspec("r8x3")
    assert r3.reversal_mult == 3 and r3.key == "r8x3"
    with pytest.raises(ValueError):
        parse_barspec("8 renko")


def test_build_bars_basic():
    # 10 trades, 1 per second starting at epoch+1000s; 5s bars -> 2 bars
    day = make_day([1, 2, 3, 4, 5, 6, 7, 8, 9, 10],
                   start_ts=1_000_000_000_000)
    bars = build_time_bars(day, 5)
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
    bars = build_time_bars(day, 5)
    assert len(bars) == 2          # empty bars omitted
    assert bars.i0[1] == 2 and bars.i1[1] == 3


def test_tick_bars():
    day = make_day([1, 2, 3, 4, 5, 6, 7])
    bars = build_tick_bars(day, 3)
    assert len(bars) == 3                      # 3 + 3 + 1 (partial kept)
    assert bars.open[0] == 1 and bars.close[0] == 3
    assert bars.open[1] == 4 and bars.close[1] == 6
    assert bars.open[2] == 7 and bars.close[2] == 7
    assert bars.i0[1] == 3 and bars.i1[1] == 6
    assert bars.volume[0] == 3
    assert bars.ts_end[0] == day.ts[2]         # close time = last trade's ts


def test_renko_uptrend_bricks():
    # brick = 1.0; prices walk up 0.25 at a time to 103
    prices = [100 + 0.25 * i for i in range(13)]   # 100.00 .. 103.00
    day = make_day(prices)
    bars = build_renko_bars(day, brick=1.0, reversal_mult=2)
    assert list(bars.close) == [101.0, 102.0, 103.0]
    assert list(bars.open) == [100.0, 101.0, 102.0]
    # spans partition the tick stream up to each closing tick
    assert bars.i0[0] == 0 and bars.i1[0] == 5     # 100.00..101.00 incl
    assert bars.i0[1] == 5 and bars.i1[1] == 9


def test_renko_reversal_needs_two_bricks():
    # up to 101 (brick), then pull back: 1 brick down (100.0) must NOT
    # reverse; 2 bricks down from anchor (99.0) closes a reversal bar
    prices = [100.0, 100.5, 101.0, 100.5, 100.0, 99.5, 99.0]
    day = make_day(prices)
    bars = build_renko_bars(day, brick=1.0, reversal_mult=2)
    assert list(bars.close) == [101.0, 99.0]
    assert bars.open[1] == 101.0                   # synthetic open = prev close
    # high includes the synthetic open (ninZaRenko/NT8-indicator behavior);
    # real ticks in the span only reached 100.5
    assert bars.high[1] == pytest.approx(101.0)
    assert bars.low[1] == pytest.approx(99.0)


def test_renko_gap_emits_multiple_bricks():
    prices = [100.0, 103.2]                        # gap through 3 bricks
    day = make_day(prices)
    bars = build_renko_bars(day, brick=1.0, reversal_mult=2)
    assert list(bars.close) == [101.0, 102.0, 103.0]
    assert bars.i1[0] == 2                         # first brick owns the span
    assert bars.i0[1] == bars.i1[1] == 2           # synthetic gap bricks


def test_renko_first_bar_can_form_downward():
    prices = [100.0, 99.5, 99.0]
    day = make_day(prices)
    bars = build_renko_bars(day, brick=1.0, reversal_mult=2)
    assert list(bars.close) == [99.0]


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
