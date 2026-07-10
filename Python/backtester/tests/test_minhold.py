"""Apex 30-second minimum-hold gate on strategy-initiated exits."""
from backtester.account import Account, TradeRecorder
from backtester.broker import SimBroker
from backtester.contracts import ContractSpec
from backtester.orders import BUY, Order, OrderType
from backtester.strategy import Strategy

from conftest import make_day

NS = 1_000_000_000


def _wire(min_hold_s):
    spec = ContractSpec("TEST", 0.25, 2.0, 1.00)
    account = Account(spec, 50_000.0)
    recorder = TradeRecorder(spec)
    broker = SimBroker(spec, account, recorder)
    s = Strategy()
    s._broker, s._account = broker, account
    s.min_hold_s = min_hold_s
    day = make_day([100.0, 100.0, 100.0])
    broker.begin_day(day)
    broker.submit(Order(side=BUY, qty=1, type=OrderType.MARKET))
    broker.resolve_span(0, 1)                    # fill at event 0
    assert account.position == 1
    return s, account, recorder.open.entry_ts


def test_close_blocked_until_min_hold():
    s, account, entry_ts = _wire(min_hold_s=30.0)
    s._now_ts = entry_ts + 10 * NS               # 10s old — too young
    assert s.hold_ok() is False
    assert s.close_position() is None
    assert account.position == 1                 # still open
    s._now_ts = entry_ts + 30 * NS               # exactly 30s — allowed
    assert s.hold_ok() is True
    assert s.close_position() is not None


def test_force_bypasses_min_hold():
    s, account, entry_ts = _wire(min_hold_s=30.0)
    s._now_ts = entry_ts + 5 * NS
    assert s.close_position(force=True) is not None   # risk stand-down


def test_min_hold_off_by_default():
    s, account, entry_ts = _wire(min_hold_s=0.0)
    s._now_ts = entry_ts                         # age 0
    assert s.hold_ok() is True
    assert s.close_position() is not None


def test_position_age_inf_when_flat():
    s, account, entry_ts = _wire(min_hold_s=30.0)
    s._now_ts = entry_ts + 100 * NS
    s._broker.flatten(2)                         # force flat (is_exit, ungated)
    assert account.position == 0
    assert s.position_age_s() == float("inf")
    assert s.hold_ok() is True
