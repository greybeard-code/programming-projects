import numpy as np
import pytest

from backtester.account import PropFirmConfig, PropFirmTracker
from backtester.orders import BUY, BracketSpec, Order, OrderType

from conftest import make_day


def series(vals):
    v = np.asarray(vals, dtype="float64")
    ts = np.arange(len(v), dtype="int64") * 1_000_000_000
    return v, ts


def test_floor_trails_peak():
    a = PropFirmTracker(PropFirmConfig(threshold=2500, lock=False), 50_000)
    eq, ts = series([50_000, 51_000, 50_500])
    a.update_series(eq, ts)
    assert a.peak == 51_000
    assert a.floor == pytest.approx(48_500)
    assert not a.breached


def test_breach_on_touch():
    a = PropFirmTracker(PropFirmConfig(threshold=2500, lock=False), 50_000)
    eq, ts = series([50_000, 51_000, 48_500])   # touches peak - 2500
    a.update_series(eq, ts)
    assert a.breached
    assert a.breach_ts == ts[2]
    assert a.breach_equity == pytest.approx(48_500)


def test_breach_within_single_series_order_matters():
    # peak then breach inside the same segment
    a = PropFirmTracker(PropFirmConfig(threshold=1000, lock=False), 50_000)
    eq, ts = series([50_000, 50_800, 49_900, 49_799])
    a.update_series(eq, ts)
    assert a.breached
    assert a.breach_ts == ts[3]                 # 49_799 <= 50_800 - 1000


def test_no_breach_when_drop_precedes_peak():
    a = PropFirmTracker(PropFirmConfig(threshold=1000, lock=False), 50_000)
    eq, ts = series([49_100, 50_800])           # drawdown from start, not peak
    a.update_series(eq, ts)
    assert not a.breached


def test_lock_freezes_floor():
    a = PropFirmTracker(PropFirmConfig(threshold=2500, lock_buffer=100, lock=True),
                    50_000)
    eq, ts = series([50_000, 54_000, 60_000])
    a.update_series(eq, ts)
    # unlocked floor would be 57_500; locked at start + 100
    assert a.floor == pytest.approx(50_100)
    eq2, ts2 = series([50_200])
    a.update_series(eq2, ts2)
    assert not a.breached
    eq3, ts3 = series([50_100])
    a.update_series(eq3, ts3)
    assert a.breached                            # touching locked floor


def test_min_headroom_tracked():
    a = PropFirmTracker(PropFirmConfig(threshold=2500, lock=False), 50_000)
    eq, ts = series([50_000, 51_000, 49_000])   # headroom at end: 500
    a.update_series(eq, ts)
    assert a.min_headroom == pytest.approx(500)


def test_intratrade_peak_counts(rig):
    """Apex trails the unrealized peak, not just closed P&L."""
    cfg = PropFirmConfig(threshold=10.0, lock=False)   # tiny threshold, $ terms
    broker, account, recorder, prop = rig(apex_cfg=cfg)
    # pv=2: long from 100.25; run to 110 (unreal 9.75*2=19.5) sets peak
    # ~50_019 -> floor ~50_009; fall back to 100 (eq ~49_999) breaches
    day = make_day([100.0, 105.0, 110.0, 100.0, 100.0])
    broker.begin_day(day)
    broker.submit(Order(side=BUY, qty=1, type=OrderType.MARKET))
    broker.resolve_span(0, len(day))
    assert prop.breached


def test_halt_on_breach_flattens(rig):
    cfg = PropFirmConfig(threshold=10.0, lock=False, halt_on_breach=True)
    broker, account, recorder, prop = rig(apex_cfg=cfg)
    day = make_day([100.0, 104.0, 99.0, 98.0, 97.0])
    broker.begin_day(day)
    broker.submit(Order(side=BUY, qty=1, type=OrderType.MARKET))
    broker.resolve_span(0, len(day))
    assert prop.breached
    assert broker.halted
    assert account.position == 0
    assert recorder.trades[-1].exit_tag == "prop-halt"
