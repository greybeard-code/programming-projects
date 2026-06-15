#!/usr/bin/env python3
"""Convert NinjaTrader Market Replay CSV exports to per-day Parquet datasets.

The input CSVs are produced by the gbNRDtoCSV add-on (NinjaTrader
``MarketReplay.DumpMarketDepth``):

    <csv-root>/<Instrument FullName>/<YYYYMMDD>.csv

e.g. ``M:\\NinjaTrader_DataRepo\\RawData\\CSV\\MNQ ##-## 2025\\20250115.csv``

Each CSV has no header, is ';'-delimited, and mixes two record layouts:

    L1;MarketDataType;Timestamp;TimestampOffset;Price;Volume
    L2;MarketDataType;Timestamp;TimestampOffset;Operation;Position;MarketMaker;Price;Volume

Output layout (one Parquet "dataset" directory per symbol/year/level, with
one part file per source day - read the whole thing with
``pd.read_parquet("<SYMBOL>-<YEAR>_L1")``):

    <output-root>/<SYMBOL>-<YEAR>_L1/<YYYYMMDD>.parquet
    <output-root>/<SYMBOL>-<YEAR>_L2/<YYYYMMDD>.parquet

SYMBOL/YEAR come from the input folder name: the leading whitespace-separated
token is the symbol, the trailing 4-digit token is the year (e.g.
"MNQ ##-## 2025" -> symbol="MNQ", year="2025").
"""

from __future__ import annotations

import argparse
import io
import logging
import re
import sys
from pathlib import Path

import pandas as pd
import pyarrow as pa
import pyarrow.parquet as pq

LOG = logging.getLogger("replay_csv_to_parquet")

FOLDER_RE = re.compile(r"^(\S+)\s+.*\s(\d{4})$")
DAY_FILE_RE = re.compile(r"\d{8}")

LEVELS = ("L1", "L2")

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

# Output schema: Timestamp+TimestampOffset are combined into one Timestamp column.
L1_SCHEMA = pa.schema([
    ("Timestamp", pa.timestamp("ns")),
    ("MarketDataType", pa.int8()),
    ("Price", pa.float64()),
    ("Volume", pa.int64()),
])
L2_SCHEMA = pa.schema([
    ("Timestamp", pa.timestamp("ns")),
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


def chunk_to_table(lines: list[str], level: str) -> pa.Table:
    """Parse a batch of raw (tag-stripped) ';'-delimited lines for one record
    level into a pyarrow Table matching that level's output schema."""
    blob = "\n".join(lines)
    df = pd.read_csv(
        io.StringIO(blob),
        sep=";",
        header=None,
        names=RAW_COLUMNS[level],
        dtype=RAW_DTYPES[level],
    )
    timestamp = (
        pd.to_datetime(df["Timestamp"], format="%Y%m%d%H%M%S")
        + pd.to_timedelta(df["TimestampOffset"].astype("int64") * 100, unit="ns")
    )
    df = df.drop(columns=["Timestamp", "TimestampOffset"])
    df.insert(0, "Timestamp", timestamp)
    return pa.Table.from_pandas(df, schema=SCHEMAS[level], preserve_index=False)


def process_day(csv_path: Path, targets: dict[str, Path], chunk_rows: int, compression: str) -> dict[str, int]:
    """Stream csv_path once, writing the requested record levels to
    temporary Parquet files, then atomically rename them into place.

    ``targets`` maps level ("L1"/"L2") to its final output path. On any
    error, partial/temporary files are removed and the exception propagates
    so the day is retried on the next run.
    """
    tmp_paths = {level: path.with_name(path.name + ".tmp") for level, path in targets.items()}
    writers: dict[str, pq.ParquetWriter] = {}
    buffers: dict[str, list[str]] = {level: [] for level in targets}
    row_counts: dict[str, int] = {level: 0 for level in targets}

    def flush(level: str) -> None:
        buf = buffers[level]
        if not buf:
            return
        table = chunk_to_table(buf, level)
        writers[level].write_table(table)
        row_counts[level] += table.num_rows
        buf.clear()

    try:
        for level, tmp_path in tmp_paths.items():
            tmp_path.parent.mkdir(parents=True, exist_ok=True)
            writers[level] = pq.ParquetWriter(tmp_path, SCHEMAS[level], compression=compression)

        with open(csv_path, "r", newline="") as f:
            for line in f:
                line = line.rstrip("\n")
                if not line:
                    continue
                buf = buffers.get(line[:2])
                if buf is None:
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

    return row_counts


def discover_work(
    csv_root: Path,
    output_root: Path,
    symbols: set[str] | None,
    years: set[str] | None,
    levels: list[str],
    force: bool,
) -> list[tuple[Path, dict[str, Path]]]:
    """Find (csv_path, targets) pairs for every source day that still needs
    one or more of the requested output levels."""
    work: list[tuple[Path, dict[str, Path]]] = []
    for folder in sorted(p for p in csv_root.iterdir() if p.is_dir()):
        m = FOLDER_RE.match(folder.name)
        if not m:
            LOG.warning("Skipping folder with unrecognized name: %s", folder.name)
            continue
        symbol, year = m.group(1), m.group(2)
        if symbols and symbol not in symbols:
            continue
        if years and year not in years:
            continue

        for csv_path in sorted(folder.glob("*.csv")):
            date = csv_path.stem
            if not DAY_FILE_RE.fullmatch(date):
                LOG.warning("Skipping file with unrecognized name: %s", csv_path)
                continue
            targets = {}
            for level in levels:
                out_path = output_root / f"{symbol}-{year}_{level}" / f"{date}.parquet"
                if out_path.exists() and not force:
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

    LOG.info("Processing %d day file(s) into %s", len(work), output_root)
    for i, (csv_path, targets) in enumerate(work, 1):
        LOG.info("[%d/%d] %s -> %s", i, len(work), csv_path, ", ".join(sorted(targets)))
        row_counts = process_day(csv_path, targets, args.chunk_rows, args.compression)
        LOG.info("    wrote %s", ", ".join(f"{lvl}={n:,} rows" for lvl, n in row_counts.items()))

    return 0


if __name__ == "__main__":
    sys.exit(main())
