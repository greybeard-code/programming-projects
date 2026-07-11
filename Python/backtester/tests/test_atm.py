"""ATM bracket engine: multi-leg scale-out, auto-breakeven, tiered trailing.

All fill assertions hand-computed (tick 0.25, point value $2, $0.50/side).
Synthetic quotes straddle each trade by 1 tick (conftest make_day).
"""
import pytest

from backtester.atm import atm_exit_orders, submit_atm_exits
from backtester.nt8config import AtmBracket, AtmSpec, TrailStep, load_atm_template
from backtester.orders import BUY, SELL, Order, OrderType, StopPlan, TrailRule

from conftest import make_day
from test_nt8config import ATM_REF


def _enter_long(broker, day, qty):
    """Market entry at event 0 (fills at the ask = price + 0.25)."""
    broker.begin_day(day)
    broker.submit(Order(side=BUY, qty=qty, type=OrderType.MARKET, tag="entry"))
    broker.resolve_span(0, 1)
    assert broker.account.position == qty
    return broker.fills[0].price


def test_multileg_scaleout_per_leg_oco(rig):
    """3 legs 1 lot each: T1/T2 fill, leg-3 stop takes the remainder."""
    spec = AtmSpec(name="t", entry_qty=3, brackets=(
        AtmBracket(qty=1, stop_ticks=8, target_ticks=4),
        AtmBracket(qty=1, stop_ticks=8, target_ticks=8),
        AtmBracket(qty=1, stop_ticks=8, target_ticks=12),
    ))
    broker, account, recorder, _ = rig()
    day = make_day([100.0, 100.0, 101.0, 101.5, 102.0, 102.5, 101.0, 99.0, 98.0])
    entry_px = _enter_long(broker, day, 3)      # 100.25
    assert entry_px == pytest.approx(100.25)
    submit_atm_exits(broker, spec, BUY, entry_px)
    assert len(broker.working) == 6             # 3 stops + 3 targets

    broker.resolve_span(1, len(day))
    # T1 101.25: through at 101.5; T2 102.25: through at 102.5;
    # leg-3 stop 98.25: triggered at 98.0, fills at bid 97.75.
    tags = [f.order.tag for f in broker.fills]
    assert tags == ["entry", "atm-target1", "atm-target2", "atm-stop3"]
    assert broker.fills[1].price == pytest.approx(101.25)
    assert broker.fills[2].price == pytest.approx(102.25)
    assert broker.fills[3].price == pytest.approx(97.75)
    assert account.position == 0
    assert broker.working == []                 # every OCO sibling cancelled
    # P&L: (+1.0 +2.0 -2.5) pts * $2 = $1.00 gross, 6 sides * $0.50 = $3.00
    assert account.balance - account.start_balance == pytest.approx(1.0 - 3.0)


def test_auto_breakeven_moves_stop(rig):
    """BE +2t @ 8t: after 8 ticks favorable, stop rests at entry+2t."""
    spec = AtmSpec(name="t", entry_qty=1, brackets=(
        AtmBracket(qty=1, stop_ticks=8, target_ticks=40,
                   be_trigger_ticks=8, be_plus_ticks=2),))
    broker, account, _, _ = rig()
    # profit high-water 102.25 = +8t -> BE stop 100.75; 100.5 triggers it
    day = make_day([100.0, 101.0, 102.25, 100.5])
    entry_px = _enter_long(broker, day, 1)
    submit_atm_exits(broker, spec, BUY, entry_px)
    broker.resolve_span(1, len(day))
    assert account.position == 0
    f = broker.fills[-1]
    assert f.order.tag == "atm-stop1"
    assert f.order.price == pytest.approx(100.75)   # effective stop level
    assert f.price == pytest.approx(100.25)         # fills at the bid
    assert broker.working == []


def test_breakeven_not_armed_below_trigger(rig):
    """Before the BE trigger is reached the base stop governs."""
    spec = AtmSpec(name="t", entry_qty=1, brackets=(
        AtmBracket(qty=1, stop_ticks=8, target_ticks=40,
                   be_trigger_ticks=8, be_plus_ticks=2),))
    broker, account, _, _ = rig()
    # high-water 101.75 = +6t (< 8t trigger); dip to 100.5 must NOT exit
    day = make_day([100.0, 101.75, 100.5, 101.0])
    entry_px = _enter_long(broker, day, 1)
    submit_atm_exits(broker, spec, BUY, entry_px)
    broker.resolve_span(1, len(day))
    assert account.position == 1                 # still in the trade
    # base stop 98.25 finally hit
    day2 = make_day([98.0], start_ts=2_000_000_000_000)
    broker.begin_day(day2)
    broker.resolve_span(0, 1)
    assert account.position == 0
    assert broker.fills[-1].order.price == pytest.approx(98.25)


def test_trailing_stop_steps_with_frequency(rig):
    """Trail trigger 8t / dist 4t / freq 2t, resolved across split spans."""
    spec = AtmSpec(name="t", entry_qty=1, brackets=(
        AtmBracket(qty=1, stop_ticks=20, target_ticks=0,   # runner
                   trail_steps=(TrailStep(profit_trigger=8, stop_loss=4,
                                          frequency=2),)),))
    broker, account, _, _ = rig()
    # hw 102.25 (+8t): stop 101.25 | hw 102.5 (+9t): floor(1/2)=0, still 101.25
    # hw 102.75 (+10t): floor(2/2)*2=2 -> stop 101.75 | 102.0 no; 101.75 fires
    day = make_day([100.0, 101.0, 102.25, 102.5, 102.75, 102.0, 101.75])
    entry_px = _enter_long(broker, day, 1)
    submit_atm_exits(broker, spec, BUY, entry_px)
    assert len(broker.working) == 1              # runner: stop only, no target
    broker.resolve_span(1, 5)                    # commit hw mid-flight
    assert account.position == 1
    broker.resolve_span(5, len(day))             # hw must persist across spans
    assert account.position == 0
    f = broker.fills[-1]
    assert f.order.price == pytest.approx(101.75)   # stepped stop level
    assert f.price == pytest.approx(101.50)         # bid at the trigger event


def test_multistep_trail_tiers(rig):
    """Second tier (16t -> dist 2t) overtakes the first (8t -> dist 4t)."""
    spec = AtmSpec(name="t", entry_qty=1, brackets=(
        AtmBracket(qty=1, stop_ticks=20, target_ticks=0, trail_steps=(
            TrailStep(profit_trigger=8, stop_loss=4, frequency=2),
            TrailStep(profit_trigger=16, stop_loss=2, frequency=2),)),))
    broker, account, _, _ = rig()
    # hw 104.25 (+16t): tier1 level 12t, tier2 level 14t -> stop 103.75
    day = make_day([100.0, 102.25, 104.25, 103.75])
    entry_px = _enter_long(broker, day, 1)
    submit_atm_exits(broker, spec, BUY, entry_px)
    broker.resolve_span(1, len(day))
    assert account.position == 0
    f = broker.fills[-1]
    assert f.order.price == pytest.approx(103.75)
    assert f.price == pytest.approx(103.50)


def test_short_side_mirror(rig):
    """Short entry: BE plus lands BELOW entry; trail high-water is a low-water."""
    spec = AtmSpec(name="t", entry_qty=1, brackets=(
        AtmBracket(qty=1, stop_ticks=8, target_ticks=40,
                   be_trigger_ticks=8, be_plus_ticks=2),))
    broker, account, _, _ = rig()
    day = make_day([100.0, 99.0, 97.75, 99.5])
    broker.begin_day(day)
    broker.submit(Order(side=SELL, qty=1, type=OrderType.MARKET, tag="entry"))
    broker.resolve_span(0, 1)                    # fills at bid 99.75
    entry_px = broker.fills[0].price
    assert entry_px == pytest.approx(99.75)
    submit_atm_exits(broker, spec, SELL, entry_px)
    broker.resolve_span(1, len(day))
    # low-water 97.75 = +8t -> BE stop 99.75 - 2t = 99.25; 99.5 >= 99.25 fires
    assert account.position == 0
    f = broker.fills[-1]
    assert f.order.price == pytest.approx(99.25)
    assert f.price == pytest.approx(99.75)       # ask at trigger, >= stop


def test_reference_template_end_to_end(rig):
    """The real 6QTY25.50.100BE30+10TP100SL file drives a full scale-out."""
    spec = load_atm_template(ATM_REF)
    broker, account, _, _ = rig()
    # entry 100.25; T1 +25t=106.5, BE trigger +30t=107.75 -> stops to +10t=102.75
    day = make_day([100.0, 106.75, 108.0, 102.5])
    entry_px = _enter_long(broker, day, 6)
    submit_atm_exits(broker, spec, BUY, entry_px)
    broker.resolve_span(1, len(day))
    tags = [f.order.tag for f in broker.fills]
    assert tags == ["entry", "atm-target1", "atm-stop2", "atm-stop3"]
    assert broker.fills[1].price == pytest.approx(106.50)   # 2 lots at T1
    for f in broker.fills[2:]:
        assert f.order.price == pytest.approx(102.75)       # BE stops
        assert f.price == pytest.approx(102.25)             # fill at the bid
    assert account.position == 0
    assert broker.working == []
    # 2*(+6.25) + 4*(+2.0) = 20.5 pts * $2 = $41 gross - 12 sides * $0.50
    assert account.balance - account.start_balance == pytest.approx(41.0 - 6.0)


def test_stopplan_levels_pure():
    """levels() must not mutate state; advance() commits the high-water."""
    import numpy as np
    plan = StopPlan(100.0, BUY, 0.25, stop_ticks=8,
                    trails=(TrailRule(8, 4, 2),))
    seg = np.array([100.0, 102.0, 101.0])
    lv1 = plan.levels(seg)
    lv2 = plan.levels(seg)
    assert np.array_equal(lv1, lv2)              # pure
    assert plan.hw == 100.0
    plan.advance(seg)
    assert plan.hw == 102.0                      # committed
    # +8t reached -> trail level 4t above entry from here on
    assert plan.levels(np.array([100.5]))[0] == pytest.approx(101.0)
