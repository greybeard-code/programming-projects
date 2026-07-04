import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent.parent))

import numpy as np
import pytest

from backtester.account import Account, ApexConfig, ApexTracker, TradeRecorder
from backtester.broker import SimBroker
from backtester.contracts import ContractSpec
from backtester.data import DayL1


@pytest.fixture
def spec():
    # simple math: tick 0.25, point value 2 (MNQ-like), $1.00 round turn
    return ContractSpec("TEST", 0.25, 2.0, 1.00)


def make_day(prices, tick=0.25, start_ts=1_000_000_000_000,
             ask=None, bid=None):
    """Synthetic DayL1: each trade price with bid/ask straddling it by 1 tick
    (aggressor 0), unless explicit ask/bid arrays are given."""
    from backtester.data import classify_aggressor

    p = np.asarray(prices, dtype="float64")
    n = len(p)
    ask = np.asarray(ask, dtype="float64") if ask is not None else p + tick
    bid = np.asarray(bid, dtype="float64") if bid is not None else p - tick
    return DayL1(
        date="20260101",
        ts=start_ts + np.arange(n, dtype="int64") * 1_000_000_000,
        price=p,
        volume=np.ones(n, dtype="int64"),
        ask=ask,
        bid=bid,
        ask_size=np.ones(n, dtype="int64"),
        bid_size=np.ones(n, dtype="int64"),
        aggr=classify_aggressor(p, ask, bid),
    )


@pytest.fixture
def rig(spec):
    """(broker, account, recorder, apex_factory) wired together."""
    def build(start_balance=50_000.0, apex_cfg=None, slippage=0.0):
        account = Account(spec, start_balance)
        recorder = TradeRecorder(spec)
        apex = ApexTracker(apex_cfg, start_balance) if apex_cfg else None
        broker = SimBroker(spec, account, recorder, apex, slippage)
        return broker, account, recorder, apex
    return build
