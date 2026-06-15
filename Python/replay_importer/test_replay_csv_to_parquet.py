import pandas as pd
import pytest

import replay_csv_to_parquet as rc

# Sample lines mirror real gbNRDtoCSV output (see README L1/L2 examples).
SAMPLE_LINES = [
    "L1;0;20210120050050;2300000;1855.8;2",
    "L1;1;20210120050107;2140000;1855.4;8",
    "L2;0;20210120050000;70000;0;0;;1855.5;1",
    "L2;1;20210120050000;70000;0;1;MM1;1855.25;3",
    "L1;2;20210120050200;0;1856.0;1",
    "L2;0;20210120050300;500000;1;2;;1856.5;5",
    "L2;1;20210120050400;1000000;2;0;;1855.0;0",
]


def test_folder_re():
    assert rc.FOLDER_RE.match("MNQ ##-## 2025").groups() == ("MNQ", "2025")
    assert rc.FOLDER_RE.match("NQ ##-## 2025").groups() == ("NQ", "2025")
    assert rc.FOLDER_RE.match("some_other_dir") is None


def test_chunk_to_table_l1():
    raw = [line[3:] for line in SAMPLE_LINES if line.startswith("L1;")]
    table = rc.chunk_to_table(raw, "L1")
    assert table.schema == rc.L1_SCHEMA
    df = table.to_pandas()
    assert len(df) == 3
    assert df["Timestamp"].iloc[0] == pd.Timestamp("2021-01-20 05:00:50.230000")
    assert df["MarketDataType"].tolist() == [0, 1, 2]
    assert df["Price"].tolist() == [1855.8, 1855.4, 1856.0]
    assert df["Volume"].tolist() == [2, 8, 1]


def test_chunk_to_table_l2():
    raw = [line[3:] for line in SAMPLE_LINES if line.startswith("L2;")]
    table = rc.chunk_to_table(raw, "L2")
    assert table.schema == rc.L2_SCHEMA
    df = table.to_pandas()
    assert len(df) == 4
    assert df["Timestamp"].iloc[0] == pd.Timestamp("2021-01-20 05:00:00.007000")
    assert df["Operation"].tolist() == [0, 0, 1, 2]
    assert df["Position"].tolist() == [0, 1, 2, 0]
    # empty MarketMaker field -> null, non-empty -> preserved
    assert pd.isna(df["MarketMaker"].iloc[0])
    assert df["MarketMaker"].iloc[1] == "MM1"
    assert pd.isna(df["MarketMaker"].iloc[2])


def test_process_day_round_trip(tmp_path):
    csv_path = tmp_path / "20210120.csv"
    csv_path.write_text("\n".join(SAMPLE_LINES) + "\n")

    targets = {
        "L1": tmp_path / "out" / "SYM-2021_L1" / "20210120.parquet",
        "L2": tmp_path / "out" / "SYM-2021_L2" / "20210120.parquet",
    }
    row_counts = rc.process_day(csv_path, targets, chunk_rows=2, compression="snappy")

    expected_l1 = sum(1 for l in SAMPLE_LINES if l.startswith("L1;"))
    expected_l2 = sum(1 for l in SAMPLE_LINES if l.startswith("L2;"))
    assert row_counts == {"L1": expected_l1, "L2": expected_l2}

    for level, path in targets.items():
        assert path.exists()
        assert not path.with_name(path.name + ".tmp").exists()

    l1 = pd.read_parquet(targets["L1"])
    l2 = pd.read_parquet(targets["L2"])
    assert len(l1) == expected_l1
    assert len(l2) == expected_l2
    assert list(l1.columns) == [f.name for f in rc.L1_SCHEMA]
    assert list(l2.columns) == [f.name for f in rc.L2_SCHEMA]


def _make_csv_root(tmp_path):
    csv_root = tmp_path / "CSV"
    folder = csv_root / "MNQ ##-## 2025"
    folder.mkdir(parents=True)
    (folder / "20250115.csv").write_text("\n".join(SAMPLE_LINES) + "\n")
    (folder / "20250116.csv").write_text("\n".join(SAMPLE_LINES) + "\n")
    return csv_root


def test_discover_work_and_incremental(tmp_path):
    csv_root = _make_csv_root(tmp_path)
    output_root = tmp_path / "Parquet"

    work = rc.discover_work(csv_root, output_root, None, None, ["L1", "L2"], force=False)
    assert len(work) == 2  # two day files, nothing converted yet

    for csv_path, targets in work:
        rc.process_day(csv_path, targets, chunk_rows=100, compression="snappy")

    # Re-running with the same options finds nothing left to do.
    work_again = rc.discover_work(csv_root, output_root, None, None, ["L1", "L2"], force=False)
    assert work_again == []

    # force=True reprocesses everything regardless of existing output.
    work_forced = rc.discover_work(csv_root, output_root, None, None, ["L1", "L2"], force=True)
    assert len(work_forced) == 2

    assert (output_root / "MNQ-2025_L1" / "20250115.parquet").exists()
    assert (output_root / "MNQ-2025_L2" / "20250116.parquet").exists()


def test_discover_work_symbol_and_year_filters(tmp_path):
    csv_root = _make_csv_root(tmp_path)
    output_root = tmp_path / "Parquet"

    assert rc.discover_work(csv_root, output_root, {"NQ"}, None, ["L1"], force=False) == []
    assert len(rc.discover_work(csv_root, output_root, {"MNQ"}, {"2025"}, ["L1"], force=False)) == 2
    assert rc.discover_work(csv_root, output_root, {"MNQ"}, {"2024"}, ["L1"], force=False) == []


def test_main_end_to_end(tmp_path, capsys):
    csv_root = _make_csv_root(tmp_path)
    output_root = tmp_path / "Parquet"

    rc_argv = ["--csv-root", str(csv_root), "--output-root", str(output_root), "--chunk-rows", "100"]
    assert rc.main(rc_argv) == 0
    assert (output_root / "MNQ-2025_L1" / "20250115.parquet").exists()
    assert (output_root / "MNQ-2025_L1" / "20250116.parquet").exists()
    assert (output_root / "MNQ-2025_L2" / "20250115.parquet").exists()

    # Second run is a no-op.
    assert rc.main(rc_argv) == 0
