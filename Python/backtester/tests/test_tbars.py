"""TBars bar geometry, Heikin-Ashi output, cache, and cross-day carry.

Port spec: research/TBars_spec.md. All hand-computed cases here use
speed_ticks=4 with tick 0.25, so the three derived distances are small round
numbers:

    trend offset  = (4 // 2) * 0.25 = 0.50   (with-trend continuation)
    reversal      = (4 *  2) * 0.25 = 2.00   (against-trend)
    open offset   =  4       * 0.25 = 1.00   (synthetic open, back from close)

`parse_barspec` grammar ("tb120", speed < 2) is tested in
test_bars_account.py::test_parse_barspec alongside the other bar grammars.
"""
import numpy as np
import pytest

from backtester.data import (
    Catalog, DayL1, RENKO_RESET_GAP_NS, build_tbar_bars, classify_aggressor,
)
from backtester.strategy import BarSpec, parse_barspec

from conftest import make_day

N, TICK = 4, 0.25
TREND, REV, OPEN_OFF = 0.50, 2.00, 1.00


def _day(ts_s, prices, volumes=None, tick=TICK):
    """DayL1 with explicit timestamps — make_day is 1 tick/second, too coarse
    for the session-gap cases below."""
    ts = (np.asarray(ts_s, dtype="float64") * 1e9).astype("int64")
    p = np.asarray(prices, dtype="float64")
    v = (np.asarray(volumes, dtype="int64") if volumes is not None
         else np.ones(len(p), dtype="int64"))
    ask, bid = p + tick, p - tick
    return DayL1("20260101", ts, p, v, ask, bid,
                 np.ones(len(p), dtype="int64"), np.ones(len(p), dtype="int64"),
                 classify_aggressor(p, ask, bid))


def _ha(o, h, l, c):
    """HA close, tick-rounded the way NT8 stores it (banker's, per the
    2026-08-04 chart-export parity run — see research/TBars_spec.md §8)."""
    return float(np.round((o + h + l + c) * 0.25 / TICK) * TICK)


# ---------------- seed + single-bar geometry ------------------------------

def test_fresh_seed_is_a_one_tick_doji():
    # bars.Count == 0 branch: bar_dir is 0, so bar_max == bar_min == open and
    # the next differing tick immediately completes a zero-range bar.
    day = make_day([100.0, 100.25, 100.5])
    bars = build_tbar_bars(day, N, TICK)
    assert bars.open[0] == pytest.approx(100.0)
    assert bars.high[0] == pytest.approx(100.0)
    assert bars.low[0] == pytest.approx(100.0)
    assert bars.close[0] == pytest.approx(100.0)
    assert bars.i0[0] == 0 and bars.i1[0] == 1


def test_with_trend_bar_is_heikin_ashi_not_raw_price():
    # After the seed doji at 100.0 completes on tick 1, the live bar is:
    #   ha_open = (fake_open 99.0 + prev_close 100.0)/2 = 99.5
    #   run_hi/run_lo seeded to c1 100.0 / fake_open 99.0
    #   bar_max = 100.0 + 0.50 = 100.50, bar_min = 100.0 - 2.00 = 98.00
    # tick 2 (100.50) does NOT break (strict >), tick 3 (100.75) does.
    day = make_day([100.0, 100.25, 100.5, 100.75])
    bars = build_tbar_bars(day, N, TICK)
    assert len(bars) == 2
    o, h, l, c = (bars.open[1], bars.high[1], bars.low[1], bars.close[1])
    assert o == pytest.approx(99.5)      # HA open, not a traded price
    assert h == pytest.approx(100.5)     # clamped to the threshold exactly
    assert l == pytest.approx(99.0)      # synthetic open offset, N ticks back
    assert c == pytest.approx(_ha(99.5, 100.5, 99.0, 100.5))
    # hand-computed: 399.5/4 = 99.875 = 399.5 ticks -> exact midpoint, and
    # NT8's tick rounding is half-to-even, so 400 ticks = 100.0
    assert c == pytest.approx(100.0)


def test_breakout_is_strict_a_tick_on_the_threshold_does_not_complete():
    # bar_max after the seed bar is exactly 100.50; a tick landing there must
    # leave the bar forming (DLL: MasterInstrument.Compare(...) > 0).
    day = make_day([100.0, 100.25, 100.5])
    assert len(build_tbar_bars(day, N, TICK)) == 1
    day = make_day([100.0, 100.25, 100.75])
    assert len(build_tbar_bars(day, N, TICK)) == 2


def test_reversal_costs_four_times_a_continuation():
    # Up bar in force: continuation needs +0.50, reversal needs -2.00.
    # 99.0 is 1.00 below the anchor -- far enough for a continuation-sized
    # move, nowhere near the reversal threshold, so no bar completes.
    day = make_day([100.0, 100.25, 99.0])
    assert len(build_tbar_bars(day, N, TICK)) == 1
    day = make_day([100.0, 100.25, 97.5])          # 2.50 down, past 98.00
    bars = build_tbar_bars(day, N, TICK)
    assert len(bars) == 2
    assert bars.low[1] == pytest.approx(98.0)      # clamped to bar_min
    assert (REV / TREND) == 4.0


# ---------------- span / volume accounting --------------------------------

def test_spans_are_contiguous_and_never_overlap():
    # The engine resolves [i0, i1) per bar; a tick covered twice could fill
    # one order twice. NT8 double-counts the breakout tick (UpdateBar AND
    # AddBar) -- this port deliberately gives it to the new bar only.
    day = make_day([100.0, 100.25, 100.75, 101.25, 99.0, 98.0, 97.0, 96.0])
    bars = build_tbar_bars(day, N, TICK)
    assert len(bars) >= 3
    assert list(bars.i1[:-1]) == list(bars.i0[1:])
    assert bars.i0[0] == 0


def test_volume_totals_never_exceed_traded_volume():
    vols = [5, 3, 7, 2, 9, 4, 6, 1]
    day = _day(list(range(len(vols))),
               [100.0, 100.25, 100.75, 101.25, 99.0, 98.0, 97.0, 96.0], vols)
    bars = build_tbar_bars(day, N, TICK)
    assert bars.volume.sum() <= sum(vols)
    for i in range(len(bars)):
        assert bars.volume[i] == sum(vols[bars.i0[i]:bars.i1[i]])


# ---------------- OHLC invariants -----------------------------------------

def _assert_ohlc_sane(bars, label):
    for i in range(len(bars)):
        o, h, l, c = bars.open[i], bars.high[i], bars.low[i], bars.close[i]
        assert l <= h, f"{label} bar {i}: low {l} > high {h}"
        assert l - 1e-9 <= o <= h + 1e-9, f"{label} bar {i}: open {o} outside [{l},{h}]"
        assert l - 1e-9 <= c <= h + 1e-9, f"{label} bar {i}: close {c} outside [{l},{h}]"


def test_ohlc_invariants_hold_on_a_random_walk():
    rng = np.random.default_rng(7)
    px = 100.0
    prices = []
    for _ in range(20_000):
        px += rng.choice([-1, 0, 1]) * TICK
        prices.append(round(px, 2))
    bars = build_tbar_bars(_day(list(range(len(prices))), prices), N, TICK)
    assert len(bars) > 50
    _assert_ohlc_sane(bars, "random walk")


def test_gap_reseed_default_cannot_emit_an_invalid_bar():
    # Prior session ends DOWN (bar_dir = -1). The DLL would seed
    # bar_max = open - 0.50 < bar_min = open + 0.50 -- inverted, so the next
    # tick satisfies BOTH tests, high and low both collapse onto c1 = 97.5,
    # and the bar's own open (98.0) ends up ABOVE its high.
    gap = RENKO_RESET_GAP_NS // 1_000_000_000 + 60
    ts = [0, 1, 2, 3, 3 + gap, 4 + gap, 5 + gap]
    prices = [100.0, 100.25, 97.5, 97.0, 98.0, 98.0, 98.25]
    bars = build_tbar_bars(_day(ts, prices), N, TICK)
    _assert_ohlc_sane(bars, "gap reseed (default)")

    faithful = build_tbar_bars(_day(ts, prices), N, TICK,
                               reset_carries_dir=True)
    bad = [i for i in range(len(faithful))
           if faithful.open[i] > faithful.high[i] + 1e-9]
    assert bad, "reset_carries_dir=True should reproduce the DLL's broken bar"
    i = bad[0]
    assert faithful.open[i] == pytest.approx(98.0)
    assert faithful.high[i] == pytest.approx(97.5)
    assert faithful.low[i] == pytest.approx(97.5)
    # ha_close is 97.625 = 390.5 ticks, an exact midpoint -> 390 ticks = 97.5.
    # Tick rounding is what stops this from ALSO being close > high; the bar
    # is still invalid, via open > high.
    assert faithful.close[i] == pytest.approx(97.5)


# ---------------- cross-day carry ------------------------------------------

def test_carry_across_a_day_boundary_matches_one_continuous_build():
    # Day FILES are ET calendar days; an overnight session runs through
    # midnight ET with no real gap, so splitting there must be invisible.
    prices = [100.0, 100.25, 100.75, 101.25, 101.0, 99.0, 98.5, 97.0,
              96.5, 96.0, 97.5, 98.5, 99.5, 100.5, 101.5]
    ts = list(range(len(prices)))
    whole = build_tbar_bars(_day(ts, prices), N, TICK)

    cut = 6
    a = build_tbar_bars(_day(ts[:cut], prices[:cut]), N, TICK)
    b = build_tbar_bars(_day(ts[cut:], prices[cut:]), N, TICK,
                        carry=a.end_state)

    assert len(a) + len(b) == len(whole)
    joined_close = list(a.close) + list(b.close)
    assert joined_close == pytest.approx(list(whole.close))
    joined_open = list(a.open) + list(b.open)
    assert joined_open == pytest.approx(list(whole.open))
    joined_high = list(a.high) + list(b.high)
    assert joined_high == pytest.approx(list(whole.high))
    joined_low = list(a.low) + list(b.low)
    assert joined_low == pytest.approx(list(whole.low))


def test_carry_preserves_volume_across_a_day_boundary():
    # A bar still forming at the end of a day file must report ALL its volume
    # on the day it completes, not just the post-boundary part. Measured at
    # 0.6% of traded volume on real MNQ data before the carry tuple grew its
    # (volume, buy_volume, sell_volume) fields -- research/TBars_spec.md §9.
    prices = [100.0, 100.25, 100.75, 101.25, 101.0, 99.0, 98.5, 97.0,
              96.5, 96.0, 97.5, 98.5, 99.5, 100.5, 101.5]
    vols = [3, 7, 2, 9, 4, 6, 1, 8, 5, 2, 7, 3, 9, 4, 6]
    ts = list(range(len(prices)))
    whole = build_tbar_bars(_day(ts, prices, vols), N, TICK)

    cut = 6
    a = build_tbar_bars(_day(ts[:cut], prices[:cut], vols[:cut]), N, TICK)
    b = build_tbar_bars(_day(ts[cut:], prices[cut:], vols[cut:]), N, TICK,
                        carry=a.end_state)

    assert list(a.volume) + list(b.volume) == list(whole.volume)
    assert list(a.buy_volume) + list(b.buy_volume) == list(whole.buy_volume)
    assert list(a.sell_volume) + list(b.sell_volume) == list(whole.sell_volume)
    # and the split loses nothing overall
    assert a.volume.sum() + b.volume.sum() == whole.volume.sum()


def test_carried_bar_may_complete_on_the_very_first_tick_of_the_next_day():
    # The carried bar was opened on the PRIOR day, so index 0 here is an
    # ordinary updating tick and must stay eligible to complete it at once
    # (contrast the fresh seed, whose own opening tick cannot complete it).
    carry = (99.5, 100.5, 98.0, 1, 100.0, 99.0, 0, 0, 0)   # bar_max 100.5
    bars = build_tbar_bars(_day([0, 1], [100.75, 101.0]), N, TICK, carry=carry)
    assert len(bars) >= 1
    assert bars.i0[0] == 0 and bars.i1[0] == 0     # completed before any tick
    assert bars.high[0] == pytest.approx(100.5)


def test_session_gap_closes_the_forming_bar_as_a_partial():
    gap = RENKO_RESET_GAP_NS // 1_000_000_000 + 60
    ts = [0, 1, 2, 2 + gap, 3 + gap]
    prices = [100.0, 100.25, 100.4, 105.0, 105.25]
    bars = build_tbar_bars(_day(ts, prices), N, TICK)
    # seed doji, then the forming bar abandoned at the halt, then the re-seed
    assert len(bars) >= 2
    assert bars.i1[1] == 3                         # stops at the last pre-gap tick
    assert bars.ts_end[1] == 2 * 1_000_000_000
    _assert_ohlc_sane(bars, "partial at halt")


# ---------------- BarSpec / cache wiring -----------------------------------

def test_barspec_key_and_parse_roundtrip():
    spec = parse_barspec("tb120")
    assert spec.kind == "tbars" and spec.speed_ticks == 120
    assert spec.key == "tb120"
    assert BarSpec("tbars", speed_ticks=8).key == "tb8"


def test_build_bars_dispatches_on_kind():
    from backtester.data import build_bars
    day = make_day([100.0, 100.25, 100.75, 101.25])
    viaspec = build_bars(day, parse_barspec("tb4"), TICK)
    direct = build_tbar_bars(day, 4, TICK)
    assert list(viaspec.close) == pytest.approx(list(direct.close))
