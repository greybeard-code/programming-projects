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
- **sweep.py / walkforward.py** (module + root CLI each) — parameter grid
  via ProcessPoolExecutor (Windows spawn-safe: worker `_run_one` is
  module-level, strategies re-loaded by file path in workers, cache tmp
  files are pid-unique). Sensitivity report flags FRAGILE params (neighbor
  metric < 50% of best). Walk-forward: rolling windows, OOS = days //
  (ratio + n_windows), stitched-OOS stats + WFE verdict.
- **sizing.py** — Carver vol targeting; `Strategy.vol_target_contracts`
  (expects a DAILY ATR). Engine `daily_loss_limit` flattens + stands down
  for the day (exit tag "dll", days listed in Result.dll_days).
- **strategy.py** — Strategy base (on_start/on_bar/on_fill/on_session_end/
  on_finish; buy_bracket, move_stop, move_stop_to_breakeven, ...);
  indicators.py has incremental NT8-style indicators (EMA, SMA, ATR, RSI,
  EfficiencyRatio, Highest, Lowest). Bar types via `period` / BarSpec:
  time ("1m"), tick ("500t"), renko ("r8-4" = brick 8 ticks / trend 4;
  "r8" defaults trend to brick/2). Renko follows the published ninZaRenko
  manual (Scribd doc 392092944): body always = brick B; with-trend close at
  prev_close ± T; open = close ∓ B (open offset B−T, overlapping bars);
  reversal closes at prev_close ∓ (2B−T). H/L include the synthetic open
  (NT8-indicator parity — deliberate). Gap moves emit extra bars with
  zero-length spans. Fills always resolve on real ticks regardless of bar
  type, so NT8's Renko fantasy-fill problem does not apply here. Do NOT
  accept decompiled ninZa source into this repo; validate bar parity via
  chart export / compare_nt8 instead.

## Conventions & gotchas

- Timestamps are int64 ns UTC everywhere; convert to CT only at the session
  boundary. Day files are UTC calendar days and include Globex overnight.
- Order flow: reduced cache stores prevailing bid/ask sizes and per-trade
  aggressor side (+1 at/above ask, -1 at/below bid); bars carry
  buy_volume/sell_volume, `bar.delta`, `bars.cum_delta` (reset per session).
  NT8 backtests structurally cannot do this — it's this repo's data edge.
  Cache schema is column-checked on read; old files rebuild transparently.
- Tests build synthetic DayL1 streams via tests/conftest.py `make_day`
  (quotes straddle each trade by 1 tick). Fill assertions are hand-computed —
  keep that style; it caught real bugs.
- The repo root is the parent monorepo (`C:\Dev\programming-projects`); only
  `git add` paths inside this folder. There are unrelated worktree deletions
  in the repo — leave them alone.
- Commissions in contracts.py are approximate all-in round-turns; user may
  recalibrate to their prop firm's rates.

## State / roadmap (updated 2026-07-04)

Done: engine + fills + brackets + order modification, Apex tracker + daily
loss limit, metrics (Sharpe/Sortino/Calmar/gross-vs-net), tearsheet + trades
CSV, NT8 comparison tool (tools/compare_nt8.py — awaiting a real NT8 export
to validate against), Monte Carlo with Apex breach / eval-pass probability,
bar types (time/tick/ninZaRenko per the published manual), order-flow data
(aggressor delta, cum_delta, quote sizes), parameter sweep + sensitivity,
walk-forward runner, Carver sizing, 53 unit tests.

Next (order per research/22_Books_Summary.md, distilled from the 22-book
docx in research/):
1. Port a real user strategy (GodZillaKilla-style) — expect gaps:
   multi-timeframe bars (secondary series), possibly on_tick.
2. Per-day trade chart (candles + entry/exit markers) for visual debugging.
3. Validate ninZaRenko bar parity against an NT8 chart export.
4. OIB/delta example strategy using bars.cum_delta.

Validation reference run (EmaCross, MNQ 1m, 156 days, defaults): net ~$1,780,
1457 trades, WR 33%, Sharpe 1.53, maxDD −$1,838, MC P(breach $2.5k) ~1.8%.
If a refactor moves these numbers materially without an intentional
fill-model change, something broke.
