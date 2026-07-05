import numpy as np
import pytest
from datetime import datetime
from zoneinfo import ZoneInfo

from backtester.engine import _segments, _session_bounds_utc

ET = ZoneInfo("America/New_York")


def ns_at(date: str, hhmm: str, tz=ET) -> int:
    d = datetime.strptime(date, "%Y%m%d")
    h, m = map(int, hhmm.split(":"))
    return int(datetime(d.year, d.month, d.day, h, m, tzinfo=tz)
               .timestamp() * 1e9)


def test_bounds_normal_and_wrap():
    s, e, wrap = _session_bounds_utc("20260610", ("09:30", "16:00"))
    assert not wrap and s < e
    s, e, wrap = _session_bounds_utc("20260610", ("18:00", "16:55"))
    assert wrap and e < s                     # both on the same calendar date


def test_segments_none_session():
    ts = np.arange(5, dtype="int64")
    segs = _segments(None, "20260610", ts)
    assert len(segs) == 1
    idx, day_end = segs[0]
    assert day_end and len(idx) == 5


def test_segments_rth():
    date = "20260610"
    ts = np.array([ns_at(date, "08:00"), ns_at(date, "10:00"),
                   ns_at(date, "15:00"), ns_at(date, "16:30")], dtype="int64")
    segs = _segments(("09:30", "16:00"), date, ts)
    assert len(segs) == 1
    idx, day_end = segs[0]
    assert day_end
    assert list(idx) == [1, 2]                # 10:00 and 15:00 only


def test_segments_overnight_wrap():
    date = "20260610"                          # a Wednesday
    ts = np.array([
        ns_at(date, "03:00"),                  # overnight (prev day's session)
        ns_at(date, "11:00"),                  # RTH
        ns_at(date, "16:30"),                  # late (<= 16:55)
        ns_at(date, "17:30"),                  # maintenance halt gap
        ns_at(date, "18:30"),                  # new session's evening
        ns_at(date, "19:50"),                  # still evening (< midnight)
    ], dtype="int64")
    segs = _segments(("18:00", "16:55"), date, ts)
    assert len(segs) == 2
    idx1, end1 = segs[0]
    idx2, end2 = segs[1]
    assert end1 == True                        # closes the trading day
    assert list(idx1) == [0, 1, 2]             # up to 16:55 ET
    assert end2 == False                       # carries to the next file
    assert list(idx2) == [4, 5]                # from 18:00 on; halt excluded


def test_segments_sunday_evening_only():
    # Sunday file: nothing before 16:55 (closed since Friday), evening only
    date = "20260607"                          # a Sunday
    ts = np.array([ns_at(date, "18:05"), ns_at(date, "19:30")], dtype="int64")
    segs = _segments(("18:00", "16:55"), date, ts)
    assert len(segs) == 1
    idx, day_end = segs[0]
    assert day_end == False
    assert list(idx) == [0, 1]
