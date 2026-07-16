"""GodTrades GapSignalEngine: gap lifecycle, BG/FC/OBR triggers, filters."""
import importlib.util
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
_spec = importlib.util.spec_from_file_location(
    "god_trades", ROOT / "strategies" / "god_trades.py")
_mod = importlib.util.module_from_spec(_spec)
sys.modules["god_trades"] = _mod           # dataclasses need this on 3.14
_spec.loader.exec_module(_mod)
GapSignalEngine = _mod.GapSignalEngine
GapLine = _mod.GapLine

TICK = 0.25


def eng(**kw):
    """Engine with the noisy filters off so tests isolate one mechanism."""
    kw.setdefault("midpoint_filter", False)
    kw.setdefault("enable_bg", False)
    kw.setdefault("enable_obr", False)
    return GapSignalEngine(TICK, **kw)


def feed(e, bars):
    out = []
    for b in bars:
        out.append(e.update(*b))
    return out


# (o, h, l, c) builders --------------------------------------------------
BASE = (99.0, 100.2, 98.8, 100.0)          # green
GAP_UP = (100.75, 102.0, 100.6, 101.5)     # green, opens 3 ticks above BASE close
HOLD = (101.5, 101.8, 101.0, 101.6)        # stays above the zone (100.0-100.75)
TOUCH_GREEN = (100.9, 101.2, 100.5, 101.0) # dips into zone, closes >= zone_high
TOUCH_RED = (101.0, 101.1, 100.5, 100.6)   # dips into zone, red close
CONFIRM_GREEN = (100.8, 101.3, 100.7, 101.2)


# ---------------- gap detection -----------------------------------------

def test_gap_created_on_two_greens_with_body_gap():
    e = eng()
    feed(e, [BASE, GAP_UP])
    assert len(e.gaps) == 1
    g = e.gaps[0]
    assert g.direction == 1
    assert g.zone_low == 100.0 and g.zone_high == 100.75
    assert g.line == 100.375


def test_no_gap_when_directions_differ_or_gap_too_small():
    e = eng()
    feed(e, [BASE, (100.75, 102.0, 100.6, 100.7)])   # red second candle
    assert not e.gaps
    e = eng(min_gap_ticks=4)
    feed(e, [BASE, GAP_UP])                          # only 3 ticks
    assert not e.gaps


def test_bearish_gap_mirror():
    e = eng()
    red1 = (101.0, 101.2, 99.9, 100.0)
    red2 = (99.25, 99.4, 98.0, 98.5)                 # opens 3 ticks below
    feed(e, [red1, red2])
    assert len(e.gaps) == 1 and e.gaps[0].direction == -1
    assert e.gaps[0].zone_low == 99.25 and e.gaps[0].zone_high == 100.0


# ---------------- line lifecycle -----------------------------------------

def test_early_touch_retires_line_without_signal():
    e = eng()
    # touch at age 1 (< min_gap_age_bars=3): line dies, no signal
    sigs = feed(e, [BASE, GAP_UP, TOUCH_GREEN])
    assert sigs == [None, None, None]
    assert not e.gaps and not e.pending


def test_valid_touch_fires_fc_on_touch_bar():
    e = eng()
    sigs = feed(e, [BASE, GAP_UP, HOLD, HOLD, HOLD, TOUCH_GREEN])
    sig = sigs[-1]
    assert sig is not None and sig.source == "FC" and sig.direction == 1
    assert sig.stop == TOUCH_GREEN[2]                # back of signal candle
    assert not e.gaps                                # line retired


def test_red_touch_defers_then_confirm_bar_fires():
    e = eng(confirm_bars=2)
    sigs = feed(e, [BASE, GAP_UP, HOLD, HOLD, HOLD, TOUCH_RED, CONFIRM_GREEN])
    assert sigs[-2] is None                          # red touch bar defers
    sig = sigs[-1]
    assert sig is not None and sig.source == "FC" and sig.direction == 1


def test_pending_expires_after_confirm_bars():
    e = eng(confirm_bars=1)
    red_hold = (101.0, 101.2, 100.8, 100.9)          # red, outside zone
    sigs = feed(e, [BASE, GAP_UP, HOLD, HOLD, HOLD, TOUCH_RED,
                    red_hold, CONFIRM_GREEN])
    assert all(s is None for s in sigs)
    assert not e.pending


def test_wrong_approach_side_rejected():
    e = eng()
    # sneak below the zone, then touch from BELOW -> approach filter rejects
    below = (99.5, 99.8, 99.3, 99.6)
    up_through = (99.7, 101.0, 99.6, 100.9)
    sigs = feed(e, [BASE, GAP_UP, HOLD, HOLD, HOLD])
    # jump below without touching is impossible here (zone would be crossed),
    # so verify directly: prev close below zone_low fails the approach test
    e2 = eng(require_correct_approach=True)
    feed(e2, [BASE, GAP_UP, HOLD, HOLD, HOLD])
    gap = e2.gaps[0]
    e2._prev = (99.5, 99.8, 99.3, 99.6)              # below-zone approach
    assert not e2._passes_approach(gap, (*up_through, 0))
    assert sigs[-1] is None


# ---------------- BG ------------------------------------------------------

def test_bg_fires_on_gap_at_band():
    e = GapSignalEngine(TICK, midpoint_filter=False, enable_bg=True,
                        enable_fc=False, enable_obr=False, bb_period=3,
                        bb_proximity_ticks=100000)
    sigs = feed(e, [BASE, BASE, BASE, BASE, BASE, GAP_UP])
    sig = sigs[-1]
    assert sig is not None and sig.source == "BG" and sig.direction == 1
    assert sig.stop == GAP_UP[2]


def test_bg_respects_proximity():
    e = GapSignalEngine(TICK, midpoint_filter=False, enable_bg=True,
                        enable_fc=False, enable_obr=False, bb_period=3,
                        bb_proximity_ticks=0)
    # flat closes -> tight bands at 100; gap candle lows are ~100.6 above band
    sigs = feed(e, [BASE, BASE, BASE, BASE, BASE, GAP_UP])
    assert sigs[-1] is None
    assert len(e.gaps) == 1                          # gap still tracked


# ---------------- OBR -----------------------------------------------------

def test_obr_bearish_engulf_at_band():
    e = GapSignalEngine(TICK, midpoint_filter=False, enable_bg=False,
                        enable_fc=False, enable_obr=True, bb_period=3,
                        obr_band_tolerance_ticks=100000)
    green = (100.0, 101.0, 99.8, 100.8)
    engulf = (100.9, 101.1, 99.5, 99.9)              # red body covers green body
    sigs = feed(e, [BASE, BASE, BASE, green, engulf])
    sig = sigs[-1]
    assert sig is not None and sig.source == "OBR" and sig.direction == -1
    assert sig.stop == engulf[1]                     # back = signal candle high


def test_obr_requires_body_engulf():
    e = GapSignalEngine(TICK, midpoint_filter=False, enable_bg=False,
                        enable_fc=False, enable_obr=True, bb_period=3,
                        obr_band_tolerance_ticks=100000)
    green = (100.0, 101.0, 99.8, 100.8)
    partial = (100.5, 101.1, 99.9, 100.1)            # opens inside prior body
    sigs = feed(e, [BASE, BASE, BASE, green, partial])
    assert sigs[-1] is None


# ---------------- quality filters -----------------------------------------

def test_doji_filter_rejects_small_body():
    e = eng(max_doji_body_frac=0.5)
    doji_touch = (100.7, 101.4, 100.4, 100.75)       # body 0.05 of range 1.0
    sigs = feed(e, [BASE, GAP_UP, HOLD, HOLD, HOLD, doji_touch])
    assert sigs[-1] is None


def test_shaved_close_filter():
    ok = eng(shaved_close_frac=0.25)
    # TOUCH_GREEN: close 101.0, high 101.2, range 0.7 -> wick frac ~0.29 > 0.25
    sigs = feed(ok, [BASE, GAP_UP, HOLD, HOLD, HOLD, TOUCH_GREEN])
    assert sigs[-1] is None
    shaved = (100.9, 101.05, 100.5, 101.0)           # wick 0.05 of range 0.55
    ok2 = eng(shaved_close_frac=0.25)
    sigs = feed(ok2, [BASE, GAP_UP, HOLD, HOLD, HOLD, shaved])
    assert sigs[-1] is not None


def test_delta_sign_filter():
    e = eng(require_delta_sign=True)
    bars = [BASE, GAP_UP, HOLD, HOLD, HOLD]
    for b in bars:
        e.update(*b)
    assert e.update(*TOUCH_GREEN, delta=-50) is None  # selling into a long
    e2 = eng(require_delta_sign=True)
    for b in bars:
        e2.update(*b)
    assert e2.update(*TOUCH_GREEN, delta=50) is not None


# ---------------- spiderweb ------------------------------------------------

def test_spiderweb_count():
    e = eng(spiderweb_distance_ticks=100, spiderweb_line_count=5)
    e.index = 50
    for i in range(5):
        e.gaps.append(GapLine(1, i, 100.0 + i * 0.5, 100.25 + i * 0.5))
    assert e.spiderweb_count(101.0) == 5
    assert e.spiderweb_count(1000.0) == 0
    # too-young lines don't count
    e.gaps.append(GapLine(1, 49, 101.0, 101.25))
    assert e.spiderweb_count(101.0) == 5
