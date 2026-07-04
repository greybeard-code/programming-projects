# backtester — project notes for Claude

Tick-level L1 futures backtester over the NinjaTrader Market Replay Parquet
repo (`M:\NinjaTrader_DataRepo\RawData\Parquet`, schema in its README.txt).
Purpose: iterate on intraday prop-firm (Apex) strategies in Python fast, then
have Claude port winners to NinjaTrader 8 C#. The Python API is deliberately
pythonic, NOT NT8-mimicking — the port is a translation step, by design.

## Commands

```powershell
.venv\Scripts\python -m pytest tests -q            # unit tests (fast, no data)
.venv\Scripts\python cli.py strategies\ema_cross.py --start 2026-06-01 --end 2026-06-17
.venv\Scripts\python tools\compare_nt8.py reports\X_trades.csv nt8_export.csv
```

Env: `BACKTESTER_DATA_ROOT` (default M:\ repo), `BACKTESTER_CACHE`
(default `.cache\` here). Venv is `.venv` (Python 3.14; numpy, pyarrow,
plotly, tzdata, pytest — no pandas/polars, keep it that way unless needed).

## Architecture (read this before touching the engine)

- **data.py** — raw day (~24M L1 events) is reduced on first touch to trade
  events + prevailing bid/ask ("reduced cache"), plus per-period bar caches
  with each bar's index span `[i0, i1)` into the reduced arrays. Cached runs
  ~0.05 s/day; first touch ~1.5 s/day. Cache ~1 GB/symbol (gitignored).
- **engine.py** — day loop → per-bar: `broker.resolve_span(i0, i1)` FIRST
  (fills happen before the strategy sees the bar — no look-ahead), then
  `strategy.on_bar`. Sessions are "HH:MM" US/Central (zoneinfo, needs tzdata
  on Windows); bars outside the session are skipped entirely, orders are
  cancelled and positions flattened at session end (`flat_at_session_end`).
- **broker.py** — span resolution: repeatedly find the earliest-triggering
  working order in the remaining span (vectorized argmax on the trade-price
  slice), mark equity up to the fill, apply the fill, continue from that
  event. Fill semantics: market = opposite quote (+slippage ticks); limit =
  trade *through* the price (touch never fills; marketable at first
  evaluation fills at the quote); stop = trigger on last, fill at quote,
  never better than the stop. Reversal fills are split at flat so the trade
  recorder sees clean round trips.
- **account.py** — Account (signed position, avg price, realized net of
  commission), TradeRecorder (round trips with MAE/MFE in dollars),
  ApexTracker (floor trails intratrade equity peak by `threshold`, locks at
  start+`lock_buffer`; breach = equity touches floor; optional halt).
- **montecarlo.py** — trade-P&L resampling (iid, or circular block bootstrap
  auto-selected when trade autocorr |r|>0.2 per Davey). Vectorized
  (sims × trades) equity matrices. Apex breach model: trade's MAE tested vs
  floor from *prior* trades; close tested vs floor including own MFE (MFE
  applied to own trough fabricates breaches — learned the hard way, see
  test_montecarlo.py). Eval race: P(hit target before breach), tie = breach.
- **metrics.py / report.py** — stats dict + console formatter; self-contained
  HTML tearsheet (plotly CDN) + trades CSV per run.
- **strategy.py** — Strategy base (on_start/on_bar/on_fill/on_session_end/
  on_finish; buy_bracket, move_stop, move_stop_to_breakeven, ...);
  indicators.py has incremental NT8-style indicators (EMA, SMA, ATR, RSI,
  EfficiencyRatio, Highest, Lowest).

## Conventions & gotchas

- Timestamps are int64 ns UTC everywhere; convert to CT only at the session
  boundary. Day files are UTC calendar days and include Globex overnight.
- Data has L1 *and* L2 (depth) with bid/ask sizes — the reduced cache
  currently drops sizes. Adding prevailing bid/ask sizes would enable OIB
  (order imbalance) research, which NT8 backtests structurally cannot do.
- Tests build synthetic DayL1 streams via tests/conftest.py `make_day`
  (quotes straddle each trade by 1 tick). Fill assertions are hand-computed —
  keep that style; it caught real bugs.
- The repo root is the parent monorepo (`C:\Dev\programming-projects`); only
  `git add` paths inside this folder. There are unrelated worktree deletions
  in the repo — leave them alone.
- Commissions in contracts.py are approximate all-in round-turns; user may
  recalibrate to their prop firm's rates.

## State / roadmap (updated 2026-07-04)

Done: engine + fills + brackets + order modification, Apex tracker, metrics
(Sharpe/Sortino/Calmar/gross-vs-net), tearsheet + trades CSV, NT8 comparison
tool (tools/compare_nt8.py — awaiting a real NT8 export to validate against),
Monte Carlo with Apex breach / eval-pass probability, 35 unit tests.

Next (order per research/22_Books_Summary.md, distilled from the 22-book
docx in research/):
1. Walk-forward runner (IS/OOS 5:1, ≥5 windows).
2. Parameter sweep with ±20% sensitivity plateaus (multiprocessing).
3. Bid/ask sizes in reduced cache → OIB features.
4. Carver vol-target sizing helper.
5. Port a real user strategy (GodZillaKilla-style) — expect gaps:
   multi-timeframe bars, possibly on_tick.

Validation reference run (EmaCross, MNQ 1m, 156 days, defaults): net ~$1,780,
1457 trades, WR 33%, Sharpe 1.53, maxDD −$1,838, MC P(breach $2.5k) ~1.8%.
If a refactor moves these numbers materially without an intentional
fill-model change, something broke.
