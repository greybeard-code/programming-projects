"""Catalog.day_files discovers contract folders both at the data root and
nested one level under a 4-digit year folder (the importer's newer
``<YEAR>\\<SYM>-<YEAR>_L1`` layout), and filters by symbol."""
import numpy as np
import pyarrow as pa
import pyarrow.parquet as pq

from backtester.data import Catalog


def _write_day(path):
    path.parent.mkdir(parents=True, exist_ok=True)
    pq.write_table(pa.table({
        "Timestamp": pa.array(np.zeros(1, dtype="int64"), pa.timestamp("ns")),
        "MarketDataType": pa.array([2], pa.int8()),
        "Price": pa.array([100.0]),
        "Volume": pa.array([1], pa.int64()),
    }), path)


def test_day_files_finds_root_and_year_nested(tmp_path):
    root = tmp_path / "data"
    _write_day(root / "MNQ-2025_L1" / "20250115.parquet")           # root-level
    _write_day(root / "2026" / "MNQ-2026_L1" / "20260103.parquet")  # year-nested
    _write_day(root / "2026" / "ES-2026_L1" / "20260103.parquet")   # other symbol
    cat = Catalog(data_root=root, cache_root=tmp_path / "cache")
    assert cat.days("MNQ") == ["20250115", "20260103"]
    assert cat.days("ES") == ["20260103"]
