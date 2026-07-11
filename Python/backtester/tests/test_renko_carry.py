"""Renko brick-state carry across day-file boundaries.

Day files are ET calendar days, but an overnight session keeps trading
through midnight ET with no real gap there. build_renko_bars alone resets
its anchor at the start of every call; Catalog.load_bars_sequence must
carry the still-forming brick's anchor/direction across days UNLESS the
actual tick gap between them is a genuine reset (> RENKO_RESET_GAP_NS).
"""
import numpy as np
import pytest

from backtester.data import (
    Catalog, RENKO_RESET_GAP_NS, build_renko_bars,
)
from backtester.strategy import BarSpec

from conftest import make_day


# ---------------- build_renko_bars carry-in/out ----------------------------

def test_default_carry_matches_fresh_start():
    """No carry args -> identical to today's fresh-start behavior."""
    prices = [100 + 0.25 * i for i in range(10)]
    day = make_day(prices)
    bars = build_renko_bars(day, brick=1.0, trend=0.5)
    assert list(bars.close) == [100.5, 101.0, 101.5, 102.0]
    assert bars.end_anchor == pytest.approx(102.0)   # last emitted close
    assert bars.end_dir == 1


def test_carry_in_continues_uptrend_without_resetting():
    """Continuing an uptrend: no 'first bar of session' (body=T) bar prints;
    the next bar is a normal with-trend close from the carried anchor."""
    day = make_day([100.75, 101.25])     # strictly clears each +T threshold
    bars = build_renko_bars(day, brick=1.0, trend=0.5,
                            carry_anchor=100.0, carry_dir=1)
    assert list(bars.close) == [100.5, 101.0]
    assert bars.open[0] == pytest.approx(99.5)       # B-T below carried anchor
    assert bars.end_anchor == pytest.approx(101.0)
    assert bars.end_dir == 1


def test_carry_in_can_reverse_immediately():
    day = make_day([98.0])               # breaks the 2B-T=1.5 reversal down
    bars = build_renko_bars(day, brick=1.0, trend=0.5,
                            carry_anchor=100.0, carry_dir=1)
    assert list(bars.close) == [98.5]
    assert bars.end_dir == -1


def test_carry_in_no_signal_yet_returns_unclosed_state():
    """A day that never breaks a threshold still reports the (unclosed)
    carried state back out unchanged, for the next day to pick up."""
    day = make_day([100.1, 100.2])       # never reaches +/- T = 0.5
    bars = build_renko_bars(day, brick=1.0, trend=0.5,
                            carry_anchor=100.0, carry_dir=1)
    assert len(bars) == 0
    assert bars.end_anchor == pytest.approx(100.0)
    assert bars.end_dir == 1


# ---------------- Catalog.load_bars_sequence --------------------------------

class _Recorder:
    """Wraps Catalog to log every build_renko_bars call's carry-in args."""


def test_sequence_carries_across_continuous_days(tmp_path):
    cat = Catalog(data_root=tmp_path, cache_root=tmp_path / "cache")
    spec = BarSpec("renko", brick_ticks=4, trend_ticks=2)   # brick=1.0, trend=0.5 @ tick .25
    tick = 0.25

    day1 = make_day([100 + 0.25 * i for i in range(10)],
                    start_ts=1_000_000_000_000)             # closes at 102.0, dir +1
    # day2 starts immediately (no real gap) and continues the uptrend
    day2 = make_day([102.75, 103.25], start_ts=2_000_000_000_000)

    bars_list = cat.load_bars_sequence("TEST", ["20260101", "20260102"], spec,
                                       tick, days=[day1, day2])
    b1, b2 = bars_list
    assert list(b1.close) == [100.5, 101.0, 101.5, 102.0]
    # day2 continues from 102.0 with NO fresh-start (no T-only body=trend bar)
    assert list(b2.close) == [102.5, 103.0]
    assert b2.open[0] == pytest.approx(101.5)        # B-T below carried close


def test_sequence_resets_on_genuine_gap(tmp_path):
    cat = Catalog(data_root=tmp_path, cache_root=tmp_path / "cache")
    spec = BarSpec("renko", brick_ticks=4, trend_ticks=2)
    tick = 0.25

    day1 = make_day([100 + 0.25 * i for i in range(10)], start_ts=1_000_000_000_000)
    # day2 starts long after RENKO_RESET_GAP_NS -> must reset fresh (body=T)
    gap_start = int(day1.ts[-1]) + RENKO_RESET_GAP_NS + 1_000_000_000
    day2 = make_day([300.0, 300.75], start_ts=gap_start)

    b1, b2 = cat.load_bars_sequence("TEST", ["20260101", "20260102"], spec,
                                    tick, days=[day1, day2])
    assert list(b1.close) == [100.5, 101.0, 101.5, 102.0]
    # fresh reset: first bar closes exactly T from the new anchor (300.0)
    assert list(b2.close) == [300.5]
    assert b2.open[0] == pytest.approx(300.0)


def test_sequence_cache_hit_matches_uncached(tmp_path):
    """A second call with the identical day sequence must hit the cache and
    return numerically identical bars (round-trip through parquet)."""
    cat = Catalog(data_root=tmp_path, cache_root=tmp_path / "cache")
    spec = BarSpec("renko", brick_ticks=4, trend_ticks=2)
    tick = 0.25
    day1 = make_day([100 + 0.25 * i for i in range(10)], start_ts=1_000_000_000_000)
    day2 = make_day([102.75, 103.25], start_ts=2_000_000_000_000)

    first = cat.load_bars_sequence("TEST", ["20260101", "20260102"], spec,
                                   tick, days=[day1, day2])
    second = cat.load_bars_sequence("TEST", ["20260101", "20260102"], spec,
                                    tick, days=[day1, day2])
    for a, b in zip(first, second):
        assert np.array_equal(a.close, b.close)
        assert np.array_equal(a.open, b.open)


def test_sequence_empty_day_carries_through(tmp_path):
    """A zero-tick day (e.g. a holiday) is skipped without resetting the
    carried state for the day after it."""
    cat = Catalog(data_root=tmp_path, cache_root=tmp_path / "cache")
    spec = BarSpec("renko", brick_ticks=4, trend_ticks=2)
    tick = 0.25
    day1 = make_day([100 + 0.25 * i for i in range(10)], start_ts=1_000_000_000_000)
    from backtester.data import DayL1
    empty = DayL1("20260102", *([np.array([], dtype=t) for t in
                                 ("int64", "float64", "int64", "float64",
                                  "float64", "int64", "int64", "int8")]))
    # starts 5s after day1's last tick (1_009_000_000_000) — well within the
    # 30-min reset threshold, so the empty day in between must not matter
    day3 = make_day([102.75, 103.25], start_ts=1_014_000_000_000)

    bars_list = cat.load_bars_sequence(
        "TEST", ["20260101", "20260102", "20260103"], spec, tick,
        days=[day1, empty, day3])
    b1, b2, b3 = bars_list
    assert len(b2) == 0
    assert list(b3.close) == [102.5, 103.0]     # carried through the empty day
    assert b3.open[0] == pytest.approx(101.5)


def test_non_renko_spec_delegates_to_load_bars(tmp_path, monkeypatch):
    cat = Catalog(data_root=tmp_path, cache_root=tmp_path / "cache")
    spec = BarSpec("time", seconds=60)
    day1 = make_day([100.0, 100.25], start_ts=1_000_000_000_000)
    calls = []
    orig = cat.load_bars
    def spy(symbol, date, spec, tick_size, day=None):
        calls.append(date)
        return orig(symbol, date, spec, tick_size, day)
    monkeypatch.setattr(cat, "load_bars", spy)
    cat.load_bars_sequence("TEST", ["20260101"], spec, 0.25, days=[day1])
    assert calls == ["20260101"]
