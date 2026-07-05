"""Reduced-cache handshake with the replay_importer UTC metadata tag.

A raw Parquet day tagged ``replay_importer.timestamps=UTC`` is already true
UTC and must skip the ET->UTC correction; a legacy/untagged day is the
recording PC's Eastern wall clock and still gets corrected. Both paths must
yield the same UTC instant for the same market event.
"""
import numpy as np
import pyarrow as pa
import pyarrow.parquet as pq

from backtester.data import Catalog, IMPORTER_TS_KEY, IMPORTER_TS_UTC

# 2025-01 is EST (UTC-5): 10:00 ET == 15:00 UTC.
_ET_WALL = np.datetime64("2025-01-15T10:00:00", "ns")
_UTC_INSTANT = np.datetime64("2025-01-15T15:00:00", "ns")
_EXPECTED_NS = _UTC_INSTANT.astype("int64")

# One Ask, one Bid, then a Last trade (only the Last survives reduction).
_MDT = np.array([0, 1, 2], dtype="int8")
_PRICE = np.array([100.25, 100.00, 100.00], dtype="float64")
_VOL = np.array([5, 7, 3], dtype="int64")


def _write_raw(path, wall, tz, meta):
    ts = pa.array(np.repeat(wall, 3), type=pa.timestamp("ns", tz=tz))
    schema = pa.schema([
        ("Timestamp", pa.timestamp("ns", tz=tz)),
        ("MarketDataType", pa.int8()),
        ("Price", pa.float64()),
        ("Volume", pa.int64()),
    ])
    table = pa.table([ts, pa.array(_MDT), pa.array(_PRICE), pa.array(_VOL)],
                     schema=schema.with_metadata(meta))
    path.parent.mkdir(parents=True, exist_ok=True)
    pq.write_table(table, path)


def _reduce(tmp_path, wall, tz, meta):
    data_root = tmp_path / "data"
    _write_raw(data_root / "MNQ-2025_L1" / "20250115.parquet", wall, tz, meta)
    cat = Catalog(data_root=data_root, cache_root=tmp_path / "cache")
    return cat.load_day("MNQ", "20250115")


def test_utc_tagged_day_skips_eastern_correction(tmp_path):
    day = _reduce(tmp_path, _UTC_INSTANT, "UTC", {IMPORTER_TS_KEY: IMPORTER_TS_UTC})
    assert day.ts.tolist() == [int(_EXPECTED_NS)]
    assert day.price.tolist() == [100.0]


def test_legacy_untagged_day_gets_eastern_correction(tmp_path):
    day = _reduce(tmp_path, _ET_WALL, None, {})   # tz-naive ET wall clock, no tag
    assert day.ts.tolist() == [int(_EXPECTED_NS)]


def test_both_paths_agree(tmp_path):
    utc = _reduce(tmp_path / "a", _UTC_INSTANT, "UTC", {IMPORTER_TS_KEY: IMPORTER_TS_UTC})
    legacy = _reduce(tmp_path / "b", _ET_WALL, None, {})
    assert utc.ts.tolist() == legacy.ts.tolist()
