"""Apex net-position cap (6 minis / 60 micros) — broker-level clamp."""
from backtester.account import Account, TradeRecorder
from backtester.broker import SimBroker
from backtester.contracts import ContractSpec, get_spec
from backtester.orders import BUY, SELL, Order, OrderState, OrderType

from conftest import make_day


def _broker(max_position):
    spec = ContractSpec("TEST", 0.25, 2.0, 1.00)   # mini-like, cap set explicitly
    account = Account(spec, 50_000.0)
    recorder = TradeRecorder(spec)
    broker = SimBroker(spec, account, recorder, max_position=max_position)
    return broker, account, recorder


def test_fresh_entry_clamped_to_cap():
    broker, account, _ = _broker(max_position=6)
    day = make_day([100.0, 100.0])
    broker.begin_day(day)
    broker.submit(Order(side=BUY, qty=10, type=OrderType.MARKET))
    broker.resolve_span(0, len(day))
    assert account.position == 6                 # clamped from 10


def test_add_clamped_to_remaining_headroom():
    broker, account, _ = _broker(max_position=6)
    day = make_day([100.0, 100.0, 100.0])
    broker.begin_day(day)
    broker.submit(Order(side=BUY, qty=5, type=OrderType.MARKET))
    broker.resolve_span(0, 1)
    assert account.position == 5
    broker.submit(Order(side=BUY, qty=4, type=OrderType.MARKET))   # would be 9
    broker.resolve_span(1, len(day))
    assert account.position == 6                 # only +1 of the 4 allowed


def test_add_when_full_is_cancelled_noop():
    broker, account, _ = _broker(max_position=6)
    day = make_day([100.0, 100.0, 100.0])
    broker.begin_day(day)
    broker.submit(Order(side=BUY, qty=6, type=OrderType.MARKET))
    broker.resolve_span(0, 1)
    o = Order(side=BUY, qty=2, type=OrderType.MARKET)
    broker.submit(o)
    broker.resolve_span(1, len(day))
    assert account.position == 6
    assert o.state is OrderState.CANCELLED
    assert sum(1 for f in broker.fills if f.order is o) == 0


def test_reversal_within_cap_not_clamped():
    broker, account, _ = _broker(max_position=6)
    day = make_day([100.0, 100.0, 100.0])
    broker.begin_day(day)
    broker.submit(Order(side=SELL, qty=3, type=OrderType.MARKET))
    broker.resolve_span(0, 1)
    assert account.position == -3
    broker.submit(Order(side=BUY, qty=6, type=OrderType.MARKET))   # reverse to +3
    broker.resolve_span(1, len(day))
    assert account.position == 3                 # full reversal, within cap


def test_reversal_far_side_overshoot_clamped():
    broker, account, _ = _broker(max_position=6)
    day = make_day([100.0, 100.0, 100.0])
    broker.begin_day(day)
    broker.submit(Order(side=SELL, qty=2, type=OrderType.MARKET))
    broker.resolve_span(0, 1)
    assert account.position == -2
    broker.submit(Order(side=BUY, qty=10, type=OrderType.MARKET))  # +8 uncapped
    broker.resolve_span(1, len(day))
    assert account.position == 6                 # clamped to +6 on the far side


def test_exit_not_clamped():
    broker, account, _ = _broker(max_position=6)
    day = make_day([100.0, 100.0])
    broker.begin_day(day)
    broker.submit(Order(side=BUY, qty=6, type=OrderType.MARKET))
    broker.resolve_span(0, 1)
    broker.flatten(1)                            # is_exit -> untouched
    assert account.position == 0


def test_symbol_default_caps():
    assert get_spec("MNQ").apex_max_position == 60   # micro
    assert get_spec("NQ").apex_max_position == 6     # mini
