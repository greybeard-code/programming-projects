#!/usr/bin/env python3
"""Convert NinjaTrader Market Replay CSV exports to per-day Parquet datasets.

The input CSVs are produced by the gbNRDtoCSV add-on (NinjaTrader
``MarketReplay.DumpMarketDepth``):

    <csv-root>/<YEAR>/<Instrument FullName>/<YYYYMMDD>.csv

e.g. ``M:\\NinjaTrader_DataRepo\\RawData\\CSV\\2026\\MNQ ##-##\\20251215.csv``

<YEAR> is the CONTRACT year (the roll season), not the calendar date: the
2026\\... folders hold days from mid-December 2025 onward.

Each CSV has no header, is ';'-delimited, and mixes two record layouts:

    L1;MarketDataType;Timestamp;TimestampOffset;Price;Volume
    L2;MarketDataType;Timestamp;TimestampOffset;Operation;Position;MarketMaker;Price;Volume

Output layout mirrors the CSV tree's <YEAR> nesting: one Parquet "dataset"
directory per symbol/year/level under its contract-year folder, one part file
per source day (read a whole dataset with
``pd.read_parquet("<YEAR>/<SYMBOL>-<YEAR>_L1")``):

    <output-root>/<YEAR>/<SYMBOL>-<YEAR>_L1/<YYYYMMDD>.parquet
    <output-root>/<YEAR>/<SYMBOL>-<YEAR>_L2/<YYYYMMDD>.parquet

YEAR is the top-level folder; SYMBOL is the leading whitespace-separated token
of the instrument folder inside it (e.g. ``2026/MNQ ##-##`` -> symbol="MNQ",
year="2026").
"""

from __future__ import annotations

import argparse
import io
import logging
import os
import re
import sys
from concurrent.futures import ProcessPoolExecutor, as_completed
from pathlib import Path

import pandas as pd
import pyarrow as pa
import pyarrow.parquet as pq

LOG = logging.getLogger("replay_csv_to_parquet")

YEAR_DIR_RE = re.compile(r"\d{4}")             # top-level contract-year folder
INSTRUMENT_RE = re.compile(r"^(\S+)\s")        # symbol = leading token of "SYM ##-##"
DAY_FILE_RE = re.compile(r"\d{8}")

LEVELS = ("L1", "L2")

# Provenance embedded in every output file's Parquet key-value metadata.
# CRITICAL: the recording PC stamps its LOCAL wall clock (US/Eastern), NOT UTC
# as the legacy repo README claimed. This importer localizes with --source-tz
# and stores UTC; the metadata below is how the backtester tells UTC-tagged
# files (skip its ET->UTC correction) from legacy untagged ones (still ET).
IMPORTER_VERSION = "2"
DEFAULT_SOURCE_TZ = "America/New_York"

META_VERSION = b"replay_importer.version"
META_SOURCE_TZ = b"replay_importer.source_tz"
META_TIMESTAMPS = b"replay_importer.timestamps"     # always b"UTC" for v>=2
META_SOURCE_SIZE = b"replay_importer.source_size"   # source CSV st_size (bytes)
META_SOURCE_MTIME = b"replay_importer.source_mtime_ns"  # source CSV st_mtime_ns
META_SOURCE_NAME = b"replay_importer.source_name"   # source CSV file name

# NinjaTrader.Data.MarketDataType
MARKET_DATA_TYPE = {
    0: "Ask", 1: "Bid", 2: "Last", 3: "DailyHigh", 4: "DailyLow",
    5: "DailyVolume", 6: "LastClose", 7: "Opening", 8: "OpenInterest",
    9: "Settlement", 10: "Unknown",
}
# NinjaTrader.Cbi.Operation
OPERATION = {0: "Add", 1: "Update", 2: "Remove"}

# Raw field names/dtypes after stripping the "L1;"/"L2;" tag, in file order.
L1_RAW_COLUMNS = ["MarketDataType", "Timestamp", "TimestampOffset", "Price", "Volume"]
L2_RAW_COLUMNS = ["MarketDataType", "Timestamp", "TimestampOffset", "Operation", "Position", "MarketMaker", "Price", "Volume"]

L1_RAW_DTYPES = {
    "MarketDataType": "int8",
    "Timestamp": "string",
    "TimestampOffset": "int64",
    "Price": "float64",
    "Volume": "int64",
}
L2_RAW_DTYPES = {
    "MarketDataType": "int8",
    "Timestamp": "string",
    "TimestampOffset": "int64",
    "Operation": "int8",
    "Position": "int32",
    "MarketMaker": "string",
    "Price": "float64",
    "Volume": "int64",
}

# Output schema: Timestamp+TimestampOffset are combined into one Timestamp
# column, localized from --source-tz and stored as true UTC (tz-aware).
L1_SCHEMA = pa.schema([
    ("Timestamp", pa.timestamp("ns", tz="UTC")),
    ("MarketDataType", pa.int8()),
    ("Price", pa.float64()),
    ("Volume", pa.int64()),
])
L2_SCHEMA = pa.schema([
    ("Timestamp", pa.timestamp("ns", tz="UTC")),
    ("MarketDataType", pa.int8()),
    ("Operation", pa.int8()),
    ("Position", pa.int32()),
    ("MarketMaker", pa.string()),
    ("Price", pa.float64()),
    ("Volume", pa.int64()),
])

RAW_COLUMNS = {"L1": L1_RAW_COLUMNS, "L2": L2_RAW_COLUMNS}
RAW_DTYPES = {"L1": L1_RAW_DTYPES, "L2": L2_RAW_DTYPES}
SCHEMAS = {"L1": L1_SCHEMA, "L2": L2_SCHEMA}

# For reading: nullable integer dtypes so a truncated/short line yields <NA>
# for the missing fields instead of crashing the C parser ("Integer column has
# NA values"). Malformed rows are then dropped and counted as skipped. The
# recorder writes to the final path, so an interrupted dump leaves exactly such
# a partial last line.
_NULLABLE = {"int8": "Int8", "int16": "Int16", "int32": "Int32", "int64": "Int64"}
READ_DTYPES = {
    lvl: {c: _NULLABLE.get(t, t) for c, t in RAW_DTYPES[lvl].items()}
    for lvl in RAW_DTYPES
}
# A row is kept only if every field except MarketMaker (a legitimately-empty
# L2 string) parsed; a truncated line leaves one of these <NA>.
REQUIRED_COLS = {lvl: [c for c in cols if c != "MarketMaker"]
                 for lvl, cols in RAW_COLUMNS.items()}


def chunk_to_table(lines: list[str], level: str,
                   source_tz: str = DEFAULT_SOURCE_TZ) -> pa.Table:
    """Parse a batch of raw (tag-stripped) ';'-delimited lines for one record
    level into a pyarrow Table matching that level's output schema.

    The raw stamps are the recording PC's ``source_tz`` wall clock; they are
    localized and converted to true UTC. DST policy:
    - Fall-back repeated hour (01:00-01:59 ET on the first Sunday in November):
      genuinely ambiguous, and sparse overnight ticks DO land there (isolated,
      one per instrument, not a live two-pass session). Resolve to the first
      pass (EDT) with ``ambiguous=True`` -- deterministic and monotonicity-safe.
      The 1-hour ambiguity is immaterial: 01:00-01:59 ET on a Sunday is outside
      every trading session, and _monotonic_check backstops the (unobserved for
      CME index futures) live-two-pass case. ``ambiguous="infer"`` is NOT used:
      it needs the repeated pair present in one array and raises "no repeated
      times" on the isolated ticks this data actually has.
    - Spring-forward gap (02:00-02:59 ET): a NONEXISTENT wall time a real clock
      never stamps, so ``nonexistent="raise"`` stays a data-integrity guard.
    """
    blob = "\n".join(lines)
    df = pd.read_csv(
        io.StringIO(blob),
        sep=";",
        header=None,
        names=RAW_COLUMNS[level],
        dtype=READ_DTYPES[level],
    )
    # Drop truncated/malformed rows (a required field is <NA>). The caller
    # counts them as skipped via len(chunk) - table.num_rows.
    req = REQUIRED_COLS[level]
    valid = df[req].notna().all(axis=1)
    if not valid.all():
        df = df[valid].reset_index(drop=True)
    naive = (
        pd.to_datetime(df["Timestamp"], format="%Y%m%d%H%M%S")
        + pd.to_timedelta(df["TimestampOffset"].astype("int64") * 100, unit="ns")
    )
    timestamp = (
        naive.dt.tz_localize(source_tz, ambiguous=True, nonexistent="raise")
             .dt.tz_convert("UTC")
    )
    df = df.drop(columns=["Timestamp", "TimestampOffset"])
    df.insert(0, "Timestamp", timestamp)
    return pa.Table.from_pandas(df, schema=SCHEMAS[level], preserve_index=False)


def _provenance_meta(csv_path: Path, source_tz: str) -> dict[bytes, bytes]:
    """Key-value metadata embedded in every output file: enough for the
    backtester to trust the UTC stamps and for discover_work to detect a
    re-exported source CSV."""
    st = csv_path.stat()
    return {
        META_VERSION: IMPORTER_VERSION.encode(),
        META_SOURCE_TZ: source_tz.encode(),
        META_TIMESTAMPS: b"UTC",
        META_SOURCE_SIZE: str(st.st_size).encode(),
        META_SOURCE_MTIME: str(st.st_mtime_ns).encode(),
        META_SOURCE_NAME: csv_path.name.encode(),
    }


def _monotonic_check(table: pa.Table, level: str, prev_last: int | None) -> int:
    """Assert a written chunk's UTC timestamps are non-decreasing (within the
    chunk and across the previous chunk's last value). Returns the last ts (ns).
    The tz-aware timestamp's storage is UTC int64 ns, so cast is exact."""
    ns = table.column("Timestamp").cast(pa.int64()).to_numpy(zero_copy_only=False)
    if len(ns) == 0:
        return prev_last if prev_last is not None else 0
    if (ns[1:] < ns[:-1]).any() or (prev_last is not None and ns[0] < prev_last):
        raise ValueError(f"{level}: timestamps are not monotonic non-decreasing")
    return int(ns[-1])


def process_day(csv_path: Path, targets: dict[str, Path], chunk_rows: int,
                compression: str, source_tz: str = DEFAULT_SOURCE_TZ) -> dict[str, int]:
    """Stream csv_path once, writing the requested record levels to
    temporary Parquet files, then atomically rename them into place.

    ``targets`` maps level ("L1"/"L2") to its final output path. On any
    error, partial/temporary files are removed and the exception propagates
    so the day is retried on the next run.

    Returns per-level row counts plus a ``"_skipped"`` entry counting lines
    that matched no requested level PLUS malformed/truncated L1/L2 rows dropped
    during parsing, so callers can still reconcile
    total = sum(levels) + skipped against the raw line count.
    """
    # pid-unique temp so concurrent runs / retries never collide on one name.
    suffix = f".{os.getpid()}.tmp"
    tmp_paths = {level: path.with_name(path.name + suffix) for level, path in targets.items()}
    meta = _provenance_meta(csv_path, source_tz)
    writers: dict[str, pq.ParquetWriter] = {}
    buffers: dict[str, list[str]] = {level: [] for level in targets}
    row_counts: dict[str, int] = {level: 0 for level in targets}
    last_ts: dict[str, int | None] = {level: None for level in targets}
    skipped = 0

    def flush(level: str) -> None:
        nonlocal skipped
        buf = buffers[level]
        if not buf:
            return
        table = chunk_to_table(buf, level, source_tz)
        skipped += len(buf) - table.num_rows   # malformed rows dropped in parse
        last_ts[level] = _monotonic_check(table, level, last_ts[level])
        writers[level].write_table(table)
        row_counts[level] += table.num_rows
        buf.clear()

    try:
        for level, tmp_path in tmp_paths.items():
            tmp_path.parent.mkdir(parents=True, exist_ok=True)
            schema = SCHEMAS[level].with_metadata(meta)
            writers[level] = pq.ParquetWriter(tmp_path, schema, compression=compression)

        with open(csv_path, "r", newline="") as f:
            for line in f:
                line = line.rstrip("\r\n")   # tolerate CRLF; no stray \r in the last field
                if not line:
                    continue
                buf = buffers.get(line[:2])
                if buf is None:
                    skipped += 1
                    continue
                buf.append(line[3:])
                if len(buf) >= chunk_rows:
                    flush(line[:2])

        for level in targets:
            flush(level)
            writers[level].close()

        for level, tmp_path in tmp_paths.items():
            tmp_path.replace(targets[level])
    except BaseException:
        for writer in writers.values():
            try:
                writer.close()
            except Exception:
                pass
        for tmp_path in tmp_paths.values():
            tmp_path.unlink(missing_ok=True)
        raise

    row_counts["_skipped"] = skipped
    return row_counts


def _is_stale(out_path: Path, csv_path: Path) -> bool:
    """True if an existing output must be rebuilt: legacy files with no
    provenance metadata (pre-UTC importer) and files whose recorded source
    size/mtime no longer match the current CSV (re-exported) both qualify."""
    try:
        meta = pq.read_metadata(out_path).metadata or {}
    except Exception:
        return True  # unreadable/corrupt -> rebuild
    stored_size = meta.get(META_SOURCE_SIZE)
    stored_mtime = meta.get(META_SOURCE_MTIME)
    if meta.get(META_TIMESTAMPS) != b"UTC" or stored_size is None or stored_mtime is None:
        return True  # legacy / untagged output
    st = csv_path.stat()
    return stored_size != str(st.st_size).encode() or stored_mtime != str(st.st_mtime_ns).encode()


def discover_work(
    csv_root: Path,
    output_root: Path,
    symbols: set[str] | None,
    years: set[str] | None,
    levels: list[str],
    force: bool,
) -> list[tuple[Path, dict[str, Path]]]:
    """Find (csv_path, targets) pairs for every source day that still needs
    one or more of the requested output levels. A day is (re)processed when
    its output is missing, ``force`` is set, or the output is stale relative
    to the source CSV (see ``_is_stale``)."""
    work: list[tuple[Path, dict[str, Path]]] = []
    for year_dir in sorted(p for p in csv_root.iterdir() if p.is_dir()):
        year = year_dir.name
        if not YEAR_DIR_RE.fullmatch(year):
            LOG.warning("Skipping non-year folder: %s", year_dir.name)
            continue
        if years and year not in years:
            continue

        for folder in sorted(p for p in year_dir.iterdir() if p.is_dir()):
            m = INSTRUMENT_RE.match(folder.name)
            if not m:
                LOG.warning("Skipping folder with unrecognized name: %s", folder)
                continue
            symbol = m.group(1)
            if symbols and symbol not in symbols:
                continue

            for csv_path in sorted(folder.glob("*.csv")):
                date = csv_path.stem
                if not DAY_FILE_RE.fullmatch(date):
                    LOG.warning("Skipping file with unrecognized name: %s", csv_path)
                    continue
                targets = {}
                for level in levels:
                    out_path = output_root / year / f"{symbol}-{year}_{level}" / f"{date}.parquet"
                    if out_path.exists() and not force and not _is_stale(out_path, csv_path):
                        continue
                    targets[level] = out_path
                if targets:
                    work.append((csv_path, targets))
    return work


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--csv-root", type=Path, default=Path(r"M:\NinjaTrader_DataRepo\RawData\CSV"),
                         help="root directory containing one folder per instrument (default: %(default)s)")
    parser.add_argument("--output-root", type=Path, default=None,
                         help="root directory for output datasets (default: <csv-root>/../Parquet)")
    parser.add_argument("--symbols", nargs="+", default=None, help="only process these symbols, e.g. MNQ NQ")
    parser.add_argument("--years", nargs="+", default=None, help="only process these years, e.g. 2024 2025")
    parser.add_argument("--levels", nargs="+", choices=LEVELS, default=list(LEVELS), help="record levels to extract")
    parser.add_argument("--chunk-rows", type=int, default=1_000_000, help="rows per Parquet write batch (default: %(default)s)")
    parser.add_argument("--compression", default="zstd", help="Parquet compression codec (default: %(default)s)")
    parser.add_argument("--source-tz", default=DEFAULT_SOURCE_TZ,
                         help="IANA tz of the RECORDING PC's wall clock in the source CSVs; "
                              "stamps are localized from this and stored as UTC (default: %(default)s)")
    parser.add_argument("--jobs", type=int, default=None,
                         help="parallel worker processes for per-day conversion (default: CPU count; 1 = serial)")
    parser.add_argument("--force", action="store_true", help="reprocess a day even if its output already exists")
    parser.add_argument("-v", "--verbose", action="store_true")
    args = parser.parse_args(argv)

    logging.basicConfig(level=logging.DEBUG if args.verbose else logging.INFO, format="%(asctime)s %(levelname)s %(message)s")

    if not args.csv_root.is_dir():
        LOG.error("CSV root not found: %s", args.csv_root)
        return 1

    output_root = args.output_root or (args.csv_root.parent / "Parquet")
    symbols = set(args.symbols) if args.symbols else None
    years = set(args.years) if args.years else None

    work = discover_work(args.csv_root, output_root, symbols, years, list(args.levels), args.force)
    if not work:
        LOG.info("Nothing to do - all requested outputs already exist.")
        return 0

    jobs = args.jobs if args.jobs is not None else (os.cpu_count() or 1)
    jobs = max(1, min(jobs, len(work)))
    LOG.info("Processing %d day file(s) into %s (source_tz=%s -> UTC, jobs=%d)",
             len(work), output_root, args.source_tz, jobs)

    def report(i: int, csv_path: Path, targets: dict[str, Path], row_counts: dict[str, int]) -> None:
        skipped = row_counts.get("_skipped", 0)
        levels = {k: v for k, v in row_counts.items() if k != "_skipped"}
        LOG.info("[%d/%d] %s -> %s%s", i, len(work), csv_path,
                 ", ".join(f"{lvl}={n:,} rows" for lvl, n in sorted(levels.items())),
                 f", skipped={skipped:,} lines (unrecognized or malformed)" if skipped else "")

    if jobs == 1:
        for i, (csv_path, targets) in enumerate(work, 1):
            row_counts = process_day(csv_path, targets, args.chunk_rows, args.compression, args.source_tz)
            report(i, csv_path, targets, row_counts)
        return 0

    # Days are independent; fan out across processes. process_day is a
    # module-level function with picklable args, so this is spawn-safe.
    failures = 0
    with ProcessPoolExecutor(max_workers=jobs) as pool:
        futures = {
            pool.submit(process_day, csv_path, targets, args.chunk_rows, args.compression, args.source_tz):
                (csv_path, targets)
            for csv_path, targets in work
        }
        for i, fut in enumerate(as_completed(futures), 1):
            csv_path, targets = futures[fut]
            try:
                report(i, csv_path, targets, fut.result())
            except Exception as exc:
                failures += 1
                LOG.error("[%d/%d] %s FAILED: %s", i, len(work), csv_path, exc)

    if failures:
        LOG.error("%d day file(s) failed; re-run to retry.", failures)
        return 1
    return 0


if __name__ == "__main__":
    sys.exit(main())
