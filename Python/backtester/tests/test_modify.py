import pytest

from backtester.orders import BUY, SELL, BracketSpec, Order, OrderState, OrderType

from conftest import make_day


def test_modify_stop_price_triggers_at_new_level(rig):
    broker, account, recorder, _ = rig()
    # long from ask 100.25, bracket stop 8 ticks (98.25)
    day = make_day([100.0, 101.0, 100.0, 99.5, 99.0, 98.5])
    broker.begin_day(day)
    broker.submit(Order(side=BUY, qty=1, type=OrderType.MARKET),
                  BracketSpec(stop_ticks=8, target_ticks=40))
    broker.resolve_span(0, 2)
    assert account.position == 1
    stop = next(o for o in broker.working if o.type is OrderType.STOP)
    assert stop.price == pytest.approx(98.25)
    # tighten stop to 99.5: triggers on the 99.5 trade
    assert broker.modify(stop, 99.5)
    broker.resolve_span(2, len(day))
    assert account.position == 0
    f = broker.fills[-1]
    assert f.order.tag == "stop"
    assert f.price <= 99.5


def test_modify_rejected_for_filled_or_market(rig):
    broker, account, _, _ = rig()
    day = make_day([100.0, 100.0])
    broker.begin_day(day)
    mkt = broker.submit(Order(side=BUY, qty=1, type=OrderType.MARKET))
    assert not broker.modify(mkt, 99.0)          # market orders have no price
    broker.resolve_span(0, len(day))
    assert mkt.state is OrderState.FILLED
    assert not broker.modify(mkt, 99.0)          # already filled


def test_breakeven_helper(rig, spec):
    from backtester.strategy import Strategy

    broker, account, recorder, _ = rig()
    strat = Strategy()
    strat._broker, strat._account = broker, account

    day = make_day([100.0, 102.0, 100.30, 100.20, 105.0])
    broker.begin_day(day)
    broker.submit(Order(side=BUY, qty=1, type=OrderType.MARKET),
                  BracketSpec(stop_ticks=40, target_ticks=80))
    broker.resolve_span(0, 2)                     # entry at 100.25
    assert strat.move_stop_to_breakeven(offset_ticks=0)
    assert strat.stop_order.price == pytest.approx(100.25)
    broker.resolve_span(2, len(day))
    # 100.20 trade <= 100.25 stop -> flat at ~breakeven, not at 105
    assert account.position == 0
    assert recorder.trades[0].exit_tag == "stop"


def test_move_target(rig):
    broker, account, _, _ = rig()
    day = make_day([100.0, 101.0, 101.30, 102.0])
    broker.begin_day(day)
    broker.submit(Order(side=BUY, qty=1, type=OrderType.MARKET),
                  BracketSpec(stop_ticks=40, target_ticks=80))
    broker.resolve_span(0, 2)
    target = next(o for o in broker.working if o.type is OrderType.LIMIT)
    broker.modify(target, 101.25)                 # pull target in
    broker.resolve_span(2, len(day))
    assert account.position == 0
    assert broker.fills[-1].order.tag == "target"
    assert broker.fills[-1].price == pytest.approx(101.25)
