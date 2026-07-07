import pandas as pd
import pyarrow.parquet as pq
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


def test_year_dir_re():
    assert rc.YEAR_DIR_RE.fullmatch("2025")
    assert rc.YEAR_DIR_RE.fullmatch("2026")
    assert rc.YEAR_DIR_RE.fullmatch("Compress-FoldersTo7z.ps1") is None
    assert rc.YEAR_DIR_RE.fullmatch("20250") is None


def test_instrument_re():
    assert rc.INSTRUMENT_RE.match("MNQ ##-##").group(1) == "MNQ"
    assert rc.INSTRUMENT_RE.match("ES ##-##").group(1) == "ES"
    assert rc.INSTRUMENT_RE.match("no_space_junk") is None


def test_chunk_to_table_l1():
    raw = [line[3:] for line in SAMPLE_LINES if line.startswith("L1;")]
    table = rc.chunk_to_table(raw, "L1")
    assert table.schema == rc.L1_SCHEMA
    df = table.to_pandas()
    assert len(df) == 3
    # 05:00:50.23 US/Eastern (EST, UTC-5 in January) -> 10:00:50.23 UTC.
    assert df["Timestamp"].iloc[0] == pd.Timestamp("2021-01-20 10:00:50.230000", tz="UTC")
    assert df["MarketDataType"].tolist() == [0, 1, 2]
    assert df["Price"].tolist() == [1855.8, 1855.4, 1856.0]
    assert df["Volume"].tolist() == [2, 8, 1]


def test_chunk_to_table_utc_conversion_dst_aware():
    # A July stamp is EDT (UTC-4); January is EST (UTC-5). Same wall clock,
    # different UTC offset -> proves per-timestamp localization, not a fixed shift.
    jan = rc.chunk_to_table(["2;0;20210115120000;0;100.0;1"], "L1").to_pandas()
    jul = rc.chunk_to_table(["2;0;20210715120000;0;100.0;1"], "L1").to_pandas()
    assert jan["Timestamp"].iloc[0] == pd.Timestamp("2021-01-15 17:00:00", tz="UTC")
    assert jul["Timestamp"].iloc[0] == pd.Timestamp("2021-07-15 16:00:00", tz="UTC")


def test_chunk_to_table_nonexistent_wall_time_raises():
    # 2021-03-14 02:30 ET does not exist (spring-forward) -> strict policy raises.
    with pytest.raises(Exception):
        rc.chunk_to_table(["2;0;20210314023000;0;100.0;1"], "L1")


def test_chunk_to_table_ambiguous_fallback_resolves_to_edt():
    # 2025-11-02 01:39:59 ET is inside the fall-back repeated hour (ambiguous).
    # Sparse overnight ticks land here as isolated stamps; we resolve to the
    # first pass (EDT, UTC-4) deterministically instead of raising, so
    # 01:39:59 EDT -> 05:39:59 UTC. (ambiguous="infer" would raise "no repeated
    # times" on a lone stamp -- see the DST policy note in chunk_to_table.)
    df = rc.chunk_to_table(["2;0;20251102013959;0;100.0;1"], "L1").to_pandas()
    assert df["Timestamp"].iloc[0] == pd.Timestamp("2025-11-02 05:39:59", tz="UTC")


def test_chunk_to_table_source_tz_override():
    # UTC source -> no shift.
    df = rc.chunk_to_table(["2;0;20210115120000;0;100.0;1"], "L1", source_tz="UTC").to_pandas()
    assert df["Timestamp"].iloc[0] == pd.Timestamp("2021-01-15 12:00:00", tz="UTC")


def test_chunk_to_table_l2():
    raw = [line[3:] for line in SAMPLE_LINES if line.startswith("L2;")]
    table = rc.chunk_to_table(raw, "L2")
    assert table.schema == rc.L2_SCHEMA
    df = table.to_pandas()
    assert len(df) == 4
    assert df["Timestamp"].iloc[0] == pd.Timestamp("2021-01-20 10:00:00.007000", tz="UTC")
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
    assert row_counts == {"L1": expected_l1, "L2": expected_l2, "_skipped": 0}

    for level, path in targets.items():
        assert path.exists()
        # no leftover pid-unique temp for this level
        assert not list(path.parent.glob(path.name + ".*.tmp"))

    l1 = pd.read_parquet(targets["L1"])
    l2 = pd.read_parquet(targets["L2"])
    assert len(l1) == expected_l1
    assert len(l2) == expected_l2
    assert list(l1.columns) == [f.name for f in rc.L1_SCHEMA]
    assert list(l2.columns) == [f.name for f in rc.L2_SCHEMA]


def _make_csv_root(tmp_path):
    csv_root = tmp_path / "CSV"
    folder = csv_root / "2025" / "MNQ ##-##"   # <YEAR>/<SYMBOL ##-##>/<day>.csv
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

    assert (output_root / "2025" / "MNQ-2025_L1" / "20250115.parquet").exists()
    assert (output_root / "2025" / "MNQ-2025_L2" / "20250116.parquet").exists()


def test_discover_work_symbol_and_year_filters(tmp_path):
    csv_root = _make_csv_root(tmp_path)
    output_root = tmp_path / "Parquet"

    assert rc.discover_work(csv_root, output_root, {"NQ"}, None, ["L1"], force=False) == []
    assert len(rc.discover_work(csv_root, output_root, {"MNQ"}, {"2025"}, ["L1"], force=False)) == 2
    assert rc.discover_work(csv_root, output_root, {"MNQ"}, {"2024"}, ["L1"], force=False) == []


def test_process_day_counts_skipped_lines(tmp_path):
    lines = SAMPLE_LINES + ["L3;garbage;line", "", "junk"]
    csv_path = tmp_path / "20210120.csv"
    csv_path.write_text("\n".join(lines) + "\n")
    targets = {"L1": tmp_path / "out" / "SYM-2021_L1" / "20210120.parquet"}
    row_counts = rc.process_day(csv_path, targets, chunk_rows=100, compression="snappy")
    # L2 lines are "skipped" when only L1 requested, plus the two junk lines.
    n_l1 = sum(1 for l in SAMPLE_LINES if l.startswith("L1;"))
    n_other = sum(1 for l in lines if l and not l.startswith("L1;"))
    assert row_counts["L1"] == n_l1
    assert row_counts["_skipped"] == n_other


def test_process_day_drops_truncated_lines(tmp_path):
    # Partial-write corruption (recorder writes to the final path): a line cut
    # short so a required integer field is missing must be dropped, not crash,
    # and counted as skipped.
    lines = SAMPLE_LINES + [
        "L1;1;",                                   # truncated right after type
        "L1;2;20210120050500;3160000;1857.",       # truncated, Volume missing
    ]
    csv_path = tmp_path / "20210120.csv"
    csv_path.write_text("\n".join(lines) + "\n")
    targets = {"L1": tmp_path / "out" / "SYM-2021_L1" / "20210120.parquet"}
    row_counts = rc.process_day(csv_path, targets, chunk_rows=100, compression="snappy")

    n_l1_good = sum(1 for l in SAMPLE_LINES if l.startswith("L1;"))
    n_l2 = sum(1 for l in SAMPLE_LINES if l.startswith("L2;"))
    assert row_counts["L1"] == n_l1_good                 # only well-formed L1 kept
    assert row_counts["_skipped"] == n_l2 + 2            # 4 L2 (not requested) + 2 truncated
    assert pd.read_parquet(targets["L1"]).shape[0] == n_l1_good


def test_process_day_embeds_utc_provenance_metadata(tmp_path):
    csv_path = tmp_path / "20210120.csv"
    csv_path.write_text("\n".join(SAMPLE_LINES) + "\n")
    out = tmp_path / "out" / "SYM-2021_L1" / "20210120.parquet"
    rc.process_day(csv_path, {"L1": out}, chunk_rows=100, compression="snappy")

    meta = pq.read_metadata(out).metadata
    assert meta[rc.META_TIMESTAMPS] == b"UTC"
    assert meta[rc.META_SOURCE_TZ] == rc.DEFAULT_SOURCE_TZ.encode()
    assert meta[rc.META_VERSION] == rc.IMPORTER_VERSION.encode()
    st = csv_path.stat()
    assert meta[rc.META_SOURCE_SIZE] == str(st.st_size).encode()
    assert meta[rc.META_SOURCE_MTIME] == str(st.st_mtime_ns).encode()


def test_process_day_monotonic_assert(tmp_path):
    # Second trade stamped a second BEFORE the first -> must raise, no output.
    lines = ["L1;2;20210120050200;0;100.0;1", "L1;2;20210120050100;0;100.0;1"]
    csv_path = tmp_path / "20210120.csv"
    csv_path.write_text("\n".join(lines) + "\n")
    out = tmp_path / "out" / "SYM-2021_L1" / "20210120.parquet"
    with pytest.raises(ValueError):
        rc.process_day(csv_path, {"L1": out}, chunk_rows=100, compression="snappy")
    assert not out.exists()


def test_discover_work_reprocesses_stale_source(tmp_path):
    csv_root = _make_csv_root(tmp_path)
    output_root = tmp_path / "Parquet"
    for csv_path, targets in rc.discover_work(csv_root, output_root, None, None, ["L1"], force=False):
        rc.process_day(csv_path, targets, chunk_rows=100, compression="snappy")
    # Fresh outputs -> nothing to do.
    assert rc.discover_work(csv_root, output_root, None, None, ["L1"], force=False) == []

    # Re-export one source CSV (changes size + mtime) -> only that day is stale.
    changed = csv_root / "2025" / "MNQ ##-##" / "20250115.csv"
    changed.write_text("\n".join(SAMPLE_LINES + ["L1;2;20250115060000;0;1.0;1"]) + "\n")
    work = rc.discover_work(csv_root, output_root, None, None, ["L1"], force=False)
    assert len(work) == 1
    assert work[0][0] == changed


def test_main_end_to_end(tmp_path, capsys):
    csv_root = _make_csv_root(tmp_path)
    output_root = tmp_path / "Parquet"

    rc_argv = ["--csv-root", str(csv_root), "--output-root", str(output_root),
               "--chunk-rows", "100", "--jobs", "1"]
    assert rc.main(rc_argv) == 0
    assert (output_root / "2025" / "MNQ-2025_L1" / "20250115.parquet").exists()
    assert (output_root / "2025" / "MNQ-2025_L1" / "20250116.parquet").exists()
    assert (output_root / "2025" / "MNQ-2025_L2" / "20250115.parquet").exists()

    # Second run is a no-op.
    assert rc.main(rc_argv) == 0
