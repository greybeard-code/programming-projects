import numpy as np
import pytest

from backtester.orders import BUY, SELL, BracketSpec, Order, OrderState, OrderType

from conftest import make_day


def test_market_buy_fills_at_ask(rig):
    broker, account, recorder, _ = rig()
    day = make_day([100.0, 100.0, 100.25])
    broker.begin_day(day)
    broker.submit(Order(side=BUY, qty=1, type=OrderType.MARKET))
    broker.resolve_span(0, len(day))
    assert len(broker.fills) == 1
    f = broker.fills[0]
    assert f.price == pytest.approx(100.25)   # ask at event 0
    assert account.position == 1


def test_market_sell_fills_at_bid_with_slippage(rig):
    broker, account, _, _ = rig(slippage=2)   # 2 ticks = 0.50
    day = make_day([100.0])
    broker.begin_day(day)
    broker.submit(Order(side=SELL, qty=1, type=OrderType.MARKET))
    broker.resolve_span(0, len(day))
    assert broker.fills[0].price == pytest.approx(99.75 - 0.50)


def test_limit_buy_requires_trade_through(rig):
    broker, account, _, _ = rig()
    # limit buy 99.0: price touches 99.0 (no fill), then 98.75 (fill)
    day = make_day([100.0, 99.5, 99.0, 99.0, 98.75, 99.5])
    broker.begin_day(day)
    broker.submit(Order(side=BUY, qty=1, type=OrderType.LIMIT, price=99.0))
    broker.resolve_span(0, 4)          # touch only
    assert account.position == 0
    broker.resolve_span(4, len(day))   # trade-through at 98.75
    assert account.position == 1
    assert broker.fills[0].price == pytest.approx(99.0)


def test_marketable_limit_fills_at_quote(rig):
    broker, account, _, _ = rig()
    day = make_day([100.0])            # ask = 100.25
    broker.begin_day(day)
    broker.submit(Order(side=BUY, qty=1, type=OrderType.LIMIT, price=101.0))
    broker.resolve_span(0, len(day))
    assert broker.fills[0].price == pytest.approx(100.25)


def test_stop_buy_triggers_and_never_fills_better_than_stop(rig):
    broker, account, _, _ = rig()
    # stop buy 101.0; trades run up through it
    day = make_day([100.0, 100.5, 101.0, 101.5])
    broker.begin_day(day)
    broker.submit(Order(side=BUY, qty=1, type=OrderType.STOP, price=101.0))
    broker.resolve_span(0, len(day))
    f = broker.fills[0]
    assert f.price == pytest.approx(101.25)    # ask at trigger event
    assert f.price >= 101.0


def test_bracket_entry_activates_children_and_oco(rig):
    broker, account, recorder, _ = rig()
    # long entry at ~100.25, stop 4 ticks (99.25), target 8 ticks (102.25)
    prices = [100.0, 100.5, 101.0, 102.0, 102.5, 103.0]
    day = make_day(prices)
    broker.begin_day(day)
    broker.submit(Order(side=BUY, qty=1, type=OrderType.MARKET),
                  BracketSpec(stop_ticks=4, target_ticks=8))
    broker.resolve_span(0, len(day))
    assert account.position == 0               # target hit at 102.25 -> flat
    assert len(broker.fills) == 2
    assert broker.fills[1].order.tag == "target"
    assert broker.working == []                # OCO sibling cancelled
    assert len(recorder.trades) == 1
    t = recorder.trades[0]
    # entry 100.25 (ask@0), exit at 102.25 limit: 8 ticks * 0.25 * pv 2 = $4 - $1 comm
    assert t.pnl == pytest.approx(2.0 * 2.0 - 1.0)


def test_bracket_stop_side(rig):
    broker, account, recorder, _ = rig()
    prices = [100.0, 99.5, 99.0, 98.0]
    day = make_day(prices)
    broker.begin_day(day)
    broker.submit(Order(side=BUY, qty=1, type=OrderType.MARKET),
                  BracketSpec(stop_ticks=4, target_ticks=8))
    broker.resolve_span(0, len(day))
    # entry 100.25, stop at 99.25 triggers on trade 99.0, fills at bid 98.75
    # but never better than stop for the holder? (stop-sell fills at min(bid, stop))
    assert account.position == 0
    assert broker.fills[1].order.tag == "stop"
    assert broker.fills[1].price <= 99.25
    assert len(recorder.trades) == 1
    assert recorder.trades[0].pnl < 0
    assert recorder.trades[0].mae < 0


def test_reversal_splits_into_two_trades(rig):
    broker, account, recorder, _ = rig()
    day = make_day([100.0, 101.0, 102.0, 103.0])
    broker.begin_day(day)
    broker.submit(Order(side=BUY, qty=1, type=OrderType.MARKET))
    broker.resolve_span(0, 2)
    assert account.position == 1
    broker.submit(Order(side=SELL, qty=3, type=OrderType.MARKET))
    broker.resolve_span(2, 4)
    assert account.position == -2
    assert len(recorder.trades) == 1           # long closed; short still open
    assert recorder.open is not None
    assert recorder.open.direction == SELL


def test_flatten(rig):
    broker, account, recorder, _ = rig()
    day = make_day([100.0, 101.0, 102.0])
    broker.begin_day(day)
    broker.submit(Order(side=BUY, qty=2, type=OrderType.MARKET))
    broker.resolve_span(0, 2)
    broker.flatten(2, tag="eod")
    assert account.position == 0
    assert recorder.trades[0].exit_tag == "eod"
    # entry ask@0 = 100.25, exit bid@2 = 101.75 -> 1.5 pts * pv2 * 2 qty - $2 comm
    assert recorder.trades[0].pnl == pytest.approx(1.5 * 2 * 2 - 2.0)


def test_cancel_all_entries_keeps_exits(rig):
    broker, account, _, _ = rig()
    day = make_day([100.0, 100.0])
    broker.begin_day(day)
    exit_o = Order(side=SELL, qty=1, type=OrderType.LIMIT, price=105.0,
                   is_exit=True)
    entry_o = Order(side=BUY, qty=1, type=OrderType.LIMIT, price=95.0)
    broker.submit(exit_o)
    broker.submit(entry_o)
    broker.cancel_all(entries_only=True)
    assert exit_o.state is OrderState.WORKING
    assert entry_o.state is OrderState.CANCELLED
