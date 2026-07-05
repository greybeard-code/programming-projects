# Data-Conversion Pipeline Improvements — Plan (2026-07-05)

Scope: the NRD→CSV→Parquet chain feeding `M:\NinjaTrader_DataRepo`.
Reviewed: `C:\Dev\gbNRDtoCSV\AddOns\gbNRDtoCSV.cs` (NT8 add-on),
`C:\Dev\programming-projects\Python\replay_importer\replay_csv_to_parquet.py`,
and the Parquet repo README. Focus: the timestamp bug found 2026-07-05
(repo stamps are recording-PC ET wall clock; README falsely claims UTC).

## Where the wall clock enters

- The add-on only calls NT8's `MarketReplay.DumpMarketDepth` — NT8 itself
  writes the CSV with local-wall-clock stamps (`yyyyMMddHHmmss` + 100 ns
  offset units). Not fixable at the add-on; the CSV format is NT8's.
- `replay_csv_to_parquet.py::chunk_to_table` parses tz-naive and writes
  `pa.timestamp("ns")` unchanged. **This is the fix point.**
- The Parquet README's "All timestamps are UTC" claim is false — it cost a
  full re-evaluation of every session-based backtest result.

## Plan (priority order)

1. **Importer: convert ET → true UTC.**
   - New arg `--source-tz` (default `America/New_York`) = the RECORDING
     PC's tz; never inferred from the converting PC.
   - Per-timestamp `tz_localize` with strict DST policy: ambiguous
     (fall-back) and nonexistent (spring-forward) wall times both occur
     2:00 AM Sunday when CME is closed → raise/assert-empty, don't guess.
   - Output schema `pa.timestamp("ns", tz="UTC")` + parquet key-value
     metadata: `replay_importer.source_tz`, `.timestamps=UTC`, `.version`.
   - Post-conversion assert: timestamps monotonic non-decreasing.
   - Day files keep their ET-calendar-date names (a "day" = ET date).

2. **Backtester handshake** (`backtester/data.py::_reduce_raw`):
   read parquet KV metadata — UTC-tagged files skip `_eastern_offset_ns`;
   untagged legacy files keep the current ET→UTC correction. Bump
   CACHE_VERSION (b"2" → b"3"). Old/new parquet coexist; no flag day.

3. **Migration**: re-run importer with `--force` (source CSVs all still on
   `M:\...\RawData\CSV`; several hours, run overnight). Validation pass:
   per day, CME halt at 17:00 ET and cash open at 09:30 ET after
   conversion — the fingerprint that exposed the original bug.

4. **Manifests** (importer writes `manifest.json` per dataset dir):
   - Integrity per day: event counts by MarketDataType, first/last ts,
     largest intraday gap, price min/max, row counts vs CSV lines.
   - Contract identity: NRD folders are the rolling "MNQ ##-##" series —
     the actual front contract per day is recorded NOWHERE. Proper fix:
     add-on resolves date→contract from the instrument's rollover table.
     Interim: importer flags likely roll days (large close-to-open jump).

5. **Docs + add-on fixes**:
   - Rewrite the Parquet README timestamp section (UTC after vN of the
     importer; ET wall clock in legacy files; metadata tells which).
   - Add-on bug: when the CSV root dir doesn't exist it creates it then
     `return`s unconditionally (~line 363) — first Convert click does
     nothing. Also `RandomDispatcher` may serialize the "4 parallel"
     workers onto one dispatcher.
   - Optional: add-on writes a sidecar with `TimeZoneInfo.Local.Id` at
     dump time, so future recordings are self-describing.

## Effort

Importer change + tests: small. Backtester handshake: small (one metadata
check + cache bump; 59-test suite + EmaCross reference run as regression).
Re-conversion: hours of unattended runtime. Manifests: medium. Add-on
(C#, needs NT8 import to test): small.
