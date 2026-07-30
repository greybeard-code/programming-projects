"""SaberRenko bar geometry, cache, and cross-day carry.

Reverse-engineered spec: research/SaberRenko_spec.md. All hand-computed
cases here use bar_ticks=4, offset_ticks=2 (tick 0.25 -> bar_size=1.0,
offset_size=0.5) unless noted, matching the small round numbers
test_renko_carry.py/test_bars_account.py use for ninZaRenko. `parse_barspec`
grammar/validation ("s64-16", O>B, B%O!=0) is tested in
test_bars_account.py::test_parse_barspec, alongside renko's own grammar
tests, not here.
"""
import numpy as np
import pytest

from backtester.data import (
    Catalog, DayL1, RENKO_RESET_GAP_NS, build_saber_bars, classify_aggressor,
)
from backtester.strategy import BarSpec

from conftest import make_day

B, O, TICK = 4, 2, 0.25          # bar_size=1.0, offset_size=0.5
FILTER_NS = 1_000_000_000        # 1s — the parse_barspec-enforced minimum


def _day(ts_s, prices, volumes=None, tick=TICK):
    """DayL1 with explicit (possibly sub-second, possibly non-uniform)
    timestamps and volumes — conftest.make_day only offers 1-tick-per-second
    spacing and uniform volume=1, too coarse for the time-filter and
    volume-handoff cases below."""
    ts = (np.asarray(ts_s, dtype="float64") * 1e9).astype("int64")
    p = np.asarray(prices, dtype="float64")
    v = (np.asarray(volumes, dtype="int64") if volumes is not None
         else np.ones(len(p), dtype="int64"))
    ask, bid = p + tick, p - tick
    return DayL1("20260101", ts, p, v, ask, bid,
                 np.ones(len(p), dtype="int64"), np.ones(len(p), dtype="int64"),
                 classify_aggressor(p, ask, bid))


# ---------------- single-bar geometry (build_saber_bars) -------------------

def test_seed_bar_body_equals_bar_size_no_wick():
    # session/day seed: anchor == open, so trigger distance is B (not O) and
    # high/low never extend past {open, close} — no wick, by construction.
    day = make_day([100.0, 100.25, 100.5, 100.75, 101.0])
    bars = build_saber_bars(day, B, O, TICK, FILTER_NS)
    assert list(bars.close) == [101.0]
    assert bars.open[0] == pytest.approx(100.0)
    assert bars.high[0] == pytest.approx(101.0)
    assert bars.low[0] == pytest.approx(100.0)          # no wick


def test_continuation_body_is_offset_range_is_bar_size():
    # carry mimics "just closed an up bar at 101.0, anchor 100.5" (open_ts
    # far in the past so the time filter is trivially already satisfied —
    # these geometry tests are not about the filter).
    carry = (101.0, 100.5, 0, 101.0, 101.0)
    day = make_day([101.25, 101.5])
    bars = build_saber_bars(day, B, O, TICK, FILTER_NS, carry=carry)
    assert list(bars.close) == [101.5]
    assert bars.open[0] == pytest.approx(101.0)
    assert bars.high[0] == pytest.approx(101.5)
    assert bars.low[0] == pytest.approx(100.5)           # anchor, not a real trade
    body = bars.close[0] - bars.open[0]
    rng = bars.high[0] - bars.low[0]
    assert body == pytest.approx(O * TICK)               # 0.5 = 2 ticks
    assert rng == pytest.approx(B * TICK)                 # 1.0 = 4 ticks
    assert (rng - body) == pytest.approx((B - O) * TICK)  # the tail, on the
    assert bars.low[0] < bars.open[0]                    # down side (came-from)


def test_reversal_body_is_2b_minus_offset_no_wick():
    # carry mimics "just closed an up bar at 101.5, anchor 101.0" (the state
    # AFTER the continuation bar above completes).
    carry = (101.5, 101.0, 0, 101.5, 101.5)
    day = make_day([101.25, 101.0, 100.75, 100.5, 100.25, 100.0])
    bars = build_saber_bars(day, B, O, TICK, FILTER_NS, carry=carry)
    assert list(bars.close) == [100.0]
    assert bars.open[0] == pytest.approx(101.5)
    assert bars.high[0] == pytest.approx(101.5)           # == open: no wick
    assert bars.low[0] == pytest.approx(100.0)            # == close: no wick
    body = bars.open[0] - bars.close[0]
    assert body == pytest.approx((2 * B - O) * TICK)      # 1.5 = 6 ticks


def test_merge_snaps_to_offset_lattice_and_never_cascades():
    # a single violent tick, 5 ticks past the continuation trigger (101.5):
    # k = 5 // 2 = 2 whole offset units; close snaps to 101.5 + 2*0.5 = 102.5,
    # NOT to the traded price 102.75 — and exactly one bar prints regardless
    # of how far past the trigger the tick landed (spec §2's no-cascade proof).
    carry = (101.0, 100.5, 0, 101.0, 101.0)
    day = make_day([102.75])
    bars = build_saber_bars(day, B, O, TICK, FILTER_NS, carry=carry)
    assert len(bars) == 1
    assert bars.close[0] == pytest.approx(101.5 + 2 * O * TICK)   # 102.5
    body = bars.close[0] - bars.open[0]
    rng = bars.high[0] - bars.low[0]
    assert body == pytest.approx(O * (1 + 2) * TICK)      # O*(1+k) = 6 ticks
    assert rng == pytest.approx((B + 2 * O) * TICK)        # B+kO = 8 ticks
    assert bars.volume[0] == 0                             # completing tick's
    # volume goes to the NEXT bar, not this one (confirmed via end_state):
    assert bars.end_state[3] == pytest.approx(102.75)      # seed_high: the
    # actual traded price (beyond the snapped close) is not lost


def test_breakout_is_inclusive_touch_completes_strict_short_does_not():
    # opposite of ninZaRenko's STRICT `>` (test_renko_touch_threshold_emits_
    # no_bar): a tick landing EXACTLY on the trigger DOES complete the bar.
    carry = (101.0, 100.5, 0, 101.0, 101.0)
    at_trigger = build_saber_bars(make_day([101.5]), B, O, TICK, FILTER_NS,
                                  carry=carry)
    assert len(at_trigger) == 1
    assert at_trigger.close[0] == pytest.approx(101.5)

    short_of_trigger = build_saber_bars(make_day([101.25]), B, O, TICK,
                                        FILTER_NS, carry=carry)
    assert len(short_of_trigger) == 0


def test_completing_ticks_volume_goes_to_next_bar():
    # bar0's own completing tick (index 4, vol=100) must NOT count toward
    # bar0 — it belongs to bar1, which itself completes one tick later.
    day = _day(ts_s=[0, 1, 2, 3, 4, 5],
              prices=[100.0, 100.25, 100.5, 100.75, 101.0, 101.5],
              volumes=[1, 1, 1, 1, 100, 1])
    bars = build_saber_bars(day, B, O, TICK, FILTER_NS)
    assert list(bars.close) == [101.0, 101.5]
    assert bars.volume[0] == 4                # ticks 0..3, excludes index 4
    assert bars.volume[1] == 100               # index 4's volume, handed over


def test_time_filter_absorbs_whipsaw_vs_immediate_completion():
    # up_trigger from the seed (anchor=open=100.0) is 101.0. A poke to
    # 101.75 at t=5s, with F=30s, arrives before the gate opens (t=0..30s)
    # and is fully absorbed as the price retreats; the bar only completes
    # later, cleanly at the trigger. With F=1s the SAME poke is past the
    # gate already and completes immediately, WITH a merge (k=1, since
    # overshoot=3 ticks > offset=2 ticks) to 101.5 — whose OWN down-trigger
    # (100.5-1.0=99.5... anchor=101.0, so 100.0) then sits exactly on the
    # very next tick (t=6, price 100.0), so it immediately reverses too
    # (inclusive breakout) — two materially different bars where F=30
    # absorbed the whole excursion into one.
    ticks = ([0, 5, 6, 40, 41], [100.0, 101.75, 100.0, 100.0, 101.0])

    absorbed = build_saber_bars(_day(*ticks), B, O, TICK, 30 * 1_000_000_000)
    assert list(absorbed.close) == [101.0]
    assert absorbed.ts_end[0] == 41_000_000_000

    immediate = build_saber_bars(_day(*ticks), B, O, TICK, 1_000_000_000)
    assert list(immediate.close) == [101.0 + O * TICK, 100.0]  # 101.5, 100.0
    assert list(immediate.ts_end) == [5_000_000_000, 6_000_000_000]


def test_session_reset_partial_bar_then_reseed():
    # a >30 min trade gap closes the forming bar as a PARTIAL with REAL
    # extremes (not the synthetic {open,close,anchor} formula), then
    # re-anchors fresh at the next trade — same rule/threshold as renko.
    # First 3 ticks never reach the seed's +B=1.0 trigger (101.0) -> stays
    # forming until the gap; the reseed then runs a clean B-tick bar.
    prices = [100.0, 100.25, 100.5, 200.0, 200.25, 200.5, 200.75, 201.0]
    day = make_day(prices)
    day.ts[3:] += RENKO_RESET_GAP_NS + 1_000_000_000

    bars = build_saber_bars(day, B, O, TICK, FILTER_NS)
    assert list(bars.close) == [100.5, 201.0]
    assert bars.high[0] == pytest.approx(100.5)     # real extremes (not the
    assert bars.low[0] == pytest.approx(100.0)      # synthetic formula)
    assert bars.open[1] == pytest.approx(200.0)     # fresh re-seed anchor
    assert bars.high[1] == pytest.approx(201.0)      # normal seed bar: body B
    assert bars.low[1] == pytest.approx(200.0)       # no wick


def test_o_equals_b_reproduces_classic_renko():
    # O == B: anchor always equals open (offset-bar_size == 0), so every bar
    # is a plain symmetric B-tick step with zero wick either direction —
    # textbook (non-overlapping) classic renko.
    prices = ([100 + TICK * i for i in range(5)]           # up to 101.0
             + [100.75 - TICK * i for i in range(4)]        # down to 100.0
             + [100.25 + TICK * i for i in range(4)])       # up to 101.0
    day = make_day(prices)
    bars = build_saber_bars(day, bar_ticks=4, offset_ticks=4,
                            tick_size=TICK, filter_ns=FILTER_NS)
    assert list(bars.close) == [101.0, 100.0, 101.0]
    for i in range(len(bars)):
        assert bars.high[i] == pytest.approx(max(bars.open[i], bars.close[i]))
        assert bars.low[i] == pytest.approx(min(bars.open[i], bars.close[i]))
        assert abs(bars.close[i] - bars.open[i]) == pytest.approx(B * TICK)


# ---------------- Catalog.load_bars_sequence (cross-day carry) -------------

def test_sequence_carries_across_continuous_days(tmp_path):
    cat = Catalog(data_root=tmp_path, cache_root=tmp_path / "cache")
    spec = BarSpec("saber", bar_ticks=B, offset_ticks=O, filter_s=1)

    day1 = make_day([100 + TICK * i for i in range(10)],
                    start_ts=1_000_000_000_000)     # closes 101.0,101.5,102.0
    # day2 starts immediately after (no real gap) and completes the bar left
    # forming at day1's end (open=102.0, anchor=101.5, up_trigger=102.5) on
    # its very own first tick.
    day2 = make_day([102.5, 102.75], start_ts=2_000_000_000_000)

    b1, b2 = cat.load_bars_sequence("TEST", ["20260101", "20260102"], spec,
                                    TICK, days=[day1, day2])
    assert list(b1.close) == [101.0, 101.5, 102.0]
    assert list(b2.close) == [102.5]              # carried, no fresh reseed
    assert b2.open[0] == pytest.approx(102.0)


def test_sequence_resets_on_genuine_gap(tmp_path):
    cat = Catalog(data_root=tmp_path, cache_root=tmp_path / "cache")
    spec = BarSpec("saber", bar_ticks=B, offset_ticks=O, filter_s=1)

    day1 = make_day([100 + TICK * i for i in range(10)],
                    start_ts=1_000_000_000_000)
    gap_start = int(day1.ts[-1]) + RENKO_RESET_GAP_NS + 1_000_000_000
    day2 = make_day([300.0, 300.25, 300.5, 300.75, 301.0], start_ts=gap_start)

    b1, b2 = cat.load_bars_sequence("TEST", ["20260101", "20260102"], spec,
                                    TICK, days=[day1, day2])
    assert list(b1.close) == [101.0, 101.5, 102.0]
    assert list(b2.close) == [301.0]               # fresh seed, not carried
    assert b2.open[0] == pytest.approx(300.0)


def test_sequence_cache_hit_matches_uncached(tmp_path):
    cat = Catalog(data_root=tmp_path, cache_root=tmp_path / "cache")
    spec = BarSpec("saber", bar_ticks=B, offset_ticks=O, filter_s=1)
    day1 = make_day([100 + TICK * i for i in range(10)],
                    start_ts=1_000_000_000_000)
    day2 = make_day([102.5, 102.75], start_ts=2_000_000_000_000)

    first = cat.load_bars_sequence("TEST", ["20260101", "20260102"], spec,
                                   TICK, days=[day1, day2])
    second = cat.load_bars_sequence("TEST", ["20260101", "20260102"], spec,
                                    TICK, days=[day1, day2])
    for a, b in zip(first, second):
        assert np.array_equal(a.close, b.close)
        assert np.array_equal(a.open, b.open)
        assert np.array_equal(a.ts_end, b.ts_end)
