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
    r = parse_barspec("r8")                     # trend defaults to brick/2
    assert r.kind == "renko" and r.brick_ticks == 8 and r.trend_ticks == 4
    assert r.key == "r8-4"
    r155 = parse_barspec("r15-5")
    assert r155.brick_ticks == 15 and r155.trend_ticks == 5
    with pytest.raises(ValueError):
        parse_barspec("r4-8")                   # trend > brick
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


def test_renko_uptrend_closes_spaced_by_trend():
    # ninZaRenko B=1.0, T=0.5: first bar closes T from start, then closes
    # every T with-trend; every body is exactly B (open = close - B).
    # Breakout is STRICT (`>`): each threshold emits on the first tick that
    # EXCEEDS it, so a bar closes one tick after price passes the level.
    prices = [100 + 0.25 * i for i in range(10)]   # 100.00 .. 102.25
    day = make_day(prices)
    bars = build_renko_bars(day, brick=1.0, trend=0.5)
    assert list(bars.close) == [100.5, 101.0, 101.5, 102.0]
    # first bar of a session: body = T, open = anchor (ninZa rule);
    # thereafter open sits B - T = 0.5 below the previous close
    assert list(bars.open) == [100.0, 100.0, 100.5, 101.0]
    assert bars.open[1] == bars.close[0] - 0.5
    # spans partition the tick stream; the 100.50 tick (exactly +T) does NOT
    # close bar 0, and the 100.75 breakout tick belongs to bar 1 (it opens
    # bar 1 in NT8), so bar 0 owns only 100.00..100.50
    assert bars.i0[0] == 0 and bars.i1[0] == 3     # 100.00..100.50
    assert bars.i0[1] == 3 and bars.i1[1] == 5     # 100.75..101.00


def test_renko_reversal_at_2b_minus_t():
    # B=1.0, T=0.5 -> reversal threshold 2B-T = 1.5 below prev close.
    # Up bar closes 100.5; pullback to 99.25 (1.25 down) must NOT reverse
    # (must break strictly BELOW 99.0); 98.75 breaks it, closing a down bar
    # at exactly 99.0 with body B.
    prices = [100.0, 100.75, 99.25, 98.75]
    day = make_day(prices)
    bars = build_renko_bars(day, brick=1.0, trend=0.5)
    assert list(bars.close) == [100.5, 99.0]
    assert bars.open[1] == pytest.approx(100.0)    # close + B
    # the up-break tick (100.75) that closed bar 0 opens bar 1, so it is
    # bar 1's high; the low is clamped to the reversal threshold (99.0) and
    # the 98.75 overshoot tick is handed to the (unformed) next bar
    assert bars.high[1] == pytest.approx(100.75)
    assert bars.low[1] == pytest.approx(99.0)


def test_renko_no_reversal_before_threshold():
    prices = [100.0, 100.75, 99.05]                # -1.45 < 1.5 threshold
    day = make_day(prices)
    bars = build_renko_bars(day, brick=1.0, trend=0.5)
    assert list(bars.close) == [100.5]


def test_renko_touch_threshold_emits_no_bar():
    # STRICT breakout guard (ninZaRenko.cs `close > upperLimit`): a tick that
    # touches the +T level EXACTLY, then pulls back, must NOT print a brick.
    # An inclusive `>=` here would emit a spurious bar and offset the grid.
    prices = [100.0, 100.5, 100.5, 100.25]
    day = make_day(prices)
    bars = build_renko_bars(day, brick=1.0, trend=0.5)
    assert len(bars) == 0


def test_renko_gap_emits_multiple_bars():
    prices = [100.0, 102.1]                        # gap: 0.5, 1.0, 1.5, 2.0
    day = make_day(prices)
    bars = build_renko_bars(day, brick=1.0, trend=0.5)
    assert list(bars.close) == [100.5, 101.0, 101.5, 102.0]
    assert bars.i1[0] == 1                         # first bar owns tick 0 only
    assert bars.i0[1] == bars.i1[1] == 1           # synthetic gap bars (empty)


def test_renko_first_bar_can_form_downward():
    prices = [100.0, 99.75, 99.25]                 # 99.25 breaks below -T
    day = make_day(prices)
    bars = build_renko_bars(day, brick=1.0, trend=0.5)
    assert list(bars.close) == [99.5]
    assert bars.open[0] == pytest.approx(100.0)    # anchor; body = T


def test_renko_session_reset_partial_bar():
    import numpy as np

    # uptrend to 101.0, price drifts to 101.4, then a >30 min trade gap:
    # forming bar's open = anchor - (B - T) = 101.0 - 0.5 = 100.5; the
    # partial closes at the last pre-gap trade (101.4). After the gap the
    # grid re-anchors at the first new trade (200.0): first bar +/- T.
    prices = [100.0, 100.75, 101.25, 101.4, 200.0, 200.75]
    day = make_day(prices)
    day.ts[4:] += 45 * 60 * 1_000_000_000          # 45-minute gap
    bars = build_renko_bars(day, brick=1.0, trend=0.5)
    assert list(bars.close) == [100.5, 101.0, 101.4, 200.5]
    assert bars.open[2] == pytest.approx(100.5)    # forming-bar open
    assert bars.open[3] == pytest.approx(200.0)    # fresh anchor; body = T
    # no 99-brick march across the gap:
    assert len(bars) == 4


def test_renko_down_then_reversal_up():
    # downtrend at T steps, then reversal up at 2B-T above last close.
    # STRICT breakout: 100.75 (not 100.5) is the tick that breaks up.
    prices = [100.0, 99.25, 98.75, 100.75]
    day = make_day(prices)
    bars = build_renko_bars(day, brick=1.0, trend=0.5)
    assert list(bars.close) == [99.5, 99.0, 100.5]
    assert bars.open[2] == pytest.approx(99.5)     # 100.5 - B


def test_aggressor_classification_and_bar_delta():
    import numpy as np
    from backtester.data import classify_aggressor

    price = np.array([100.25, 100.0, 100.10, 100.25])
    ask = np.array([100.25, 100.25, 100.25, 100.25])
    bid = np.array([100.0, 100.0, 100.0, 100.0])
    aggr = classify_aggressor(price, ask, bid)
    assert list(aggr) == [1, -1, 0, 1]     # lift, hit, mid, lift

    day = make_day([100.25, 100.0, 100.10, 100.25],
                   ask=[100.25] * 4, bid=[100.0] * 4)
    bars = build_time_bars(day, 60)         # all in one bar
    assert bars.buy_volume[0] == 2
    assert bars.sell_volume[0] == 1
    assert bars.volume[0] == 4


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
