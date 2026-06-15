replay_importer - NinjaTrader Replay CSV -> Parquet
====================================================

Converts gbNRDtoCSV market-replay exports
(M:\NinjaTrader_DataRepo\RawData\CSV\<Instrument>\<YYYYMMDD>.csv) into
per-day Parquet datasets, split into L1 (quote/trade ticks) and L2
(order-book depth) outputs.

See the docstring at the top of replay_csv_to_parquet.py for the full
file-format and schema details.


Setup (Windows)
---------------
1. Install Python 3.10 or later (https://www.python.org/downloads/).

2. Open a command prompt in this folder and create a virtual environment:

       python -m venv .venv
       .venv\Scripts\activate

3. Install dependencies:

       pip install -r requirements.txt


Running the script
-------------------
With the venv activated:

       python replay_csv_to_parquet.py

By default this reads from M:\NinjaTrader_DataRepo\RawData\CSV and writes
to M:\NinjaTrader_DataRepo\RawData\Parquet, processing every instrument/day
that hasn't been converted yet.

Useful options:

       --csv-root PATH        override the input root
       --output-root PATH     override the output root
       --symbols MNQ NQ       only process these symbols
       --years 2024 2025      only process these years
       --levels L1            only extract L1 (skip the much larger L2 data)
       --force                reprocess days even if output already exists
       --chunk-rows N         rows per Parquet write batch (default 1,000,000)
       --compression zstd     Parquet codec (default zstd)
       -v                     verbose logging

Example - convert only MNQ L1 data for 2025:

       python replay_csv_to_parquet.py --symbols MNQ --years 2025 --levels L1

The script is safe to interrupt and re-run: it skips any day/level whose
output Parquet file already exists, and writes to a .tmp file that is only
renamed into place once a day's conversion completes successfully.

Given the dataset size (hundreds of GB per instrument/year, mostly L2), a
first full run can take several hours.


Output layout
--------------
       <output-root>\<SYMBOL>-<YEAR>_L1\<YYYYMMDD>.parquet
       <output-root>\<SYMBOL>-<YEAR>_L2\<YYYYMMDD>.parquet

Each dataset directory can be read as a single DataFrame:

       import pandas as pd
       df = pd.read_parquet(r"M:\NinjaTrader_DataRepo\RawData\Parquet\MNQ-2025_L1")


Running the tests
-----------------
       pip install pytest
       pytest
