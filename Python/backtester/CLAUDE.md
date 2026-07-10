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
  `strategy.on_bar`. Sessions are "HH:MM" **US/Eastern** (user preference —
  ET everywhere user-facing; zoneinfo, needs tzdata on Windows); bars
  outside the session are skipped entirely, orders are cancelled and
  positions flattened at session end (`flat_at_session_end`).
  Overnight sessions (start > end, e.g. ("18:00","16:55") = Globex day) are
  supported: day files are split into segments (_segments), the trading day
  flushes at the END time, and positions/orders carry across the UTC-
  midnight file boundary; anything open at end-of-data is force-flattened.
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
  PropFirmTracker (floor trails intratrade equity peak by `threshold`, locks
  at start+`lock_buffer`; breach = equity touches floor; optional halt).
  Naming: generic "prop firm" everywhere (user preference) — Apex is the
  modeled rule set, mention it only as provenance. CLI keeps
  --apex-threshold/--apex-halt as hidden aliases of --prop-*.
- **montecarlo.py** — trade-P&L resampling (iid, or circular block bootstrap
  auto-selected when trade autocorr |r|>0.2 per Davey). Vectorized
  (sims × trades) equity matrices. Prop-firm breach model: trade's MAE tested
  vs floor from *prior* trades; close tested vs floor including own MFE (MFE
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

- Timestamps are int64 ns UTC everywhere *after reduction*. CRITICAL: the
  raw M:\ repo's stamps are the recording PC's **US/Eastern wall clock**,
  NOT UTC as its README claims (verified 2026-07-05: CME halt sits at
  stamped 17:00, cash open at 09:30, year-round; tick prices align with an
  NT8 chart export to seconds only under ET). `_reduce_raw` converts ET→UTC
  per day (`_eastern_offset_ns`); cache metadata b"btcache"=CACHE_VERSION
  forces rebuilds when this logic changes. Day files are ET calendar days.
  ALL session-based results computed before 2026-07-05 used windows
  mislabeled by 4-5 h — see strategy/ report revision notes.
- ninZaRenko parity: validated against five real chart exports (10/3, 36/2,
  40/10, 64/16, 100/4 — see research/ninZaRenko_spec.md). Geometry exact on
  NT8's own bars (zero invariant violations; 2B−T parametric); re-anchor
  only at real session opens (trade-gap >30 min reset in build_renko_bars —
  audited: all 131 repo gaps are true halts/weekends/holiday early closes).
  Breakout is STRICT (`>`/`<`, per ninZaRenko.cs — a close exactly AT the
  threshold does not emit; an earlier inclusive `>=` printed spurious
  touch-and-reverse bricks), and the breakout tick belongs to the NEXT bar
  (it opens that bar / is its first H/L; the completing bar clamps to the
  threshold, that final step's volume = 0). With both (bars cache v6)
  fresh-load parity is bit-identical OHLC: 100% (40/10 every bar, 36/2 all but
  one) / 96.4% (10/3, T=3 = residual feed noise, ±T self-healing, OHLC tracks
  close). Rule 9 shifts one tick's volume/fill per bar, so it moves r100-4
  strategy numbers (Terminator re-validated). Live-accumulated charts add
  persistent reconnect re-anchor offsets (never a multiple of T),
  irreproducible by any backtest. compare_bars matching is one-to-one
  monotonic (gap sweeps emit same-ts bars; feeds skew ~6 s, so
  --tolerance-s 10 for small-T settings).
- **CME trading day**: runs **18:00 ET (prior calendar day) → 17:00 ET**,
  with the 17:00–18:00 ET daily maintenance halt marking the boundary
  between one trading day and the next (verified across all 3 DST
  transitions in the repo — halt always sits at stamped 17:00 ET, reopen
  always 18:00:00 ET). A session spanning `("18:00","16:55")` (the engine's
  overnight-session support) is therefore ONE compliant trading day: it
  flattens once, before the *next* halt, and never holds a position through
  any halt — this is a materially different (and much better) framing than
  restricting trading to a short daytime box. See TerminatorV2.md §3 and
  TerminatorV2_ETH.md §4 for why this matters.
- **Apex rule set** (the modeled prop-firm rules, per user 2026-07-09):
  trailing drawdown **$2,000** — `PropFirmConfig.threshold` in account.py now
  defaults to $2,000 (corrected from $2,500 on 2026-07-09; the CLI
  `--prop-threshold`, sweep, and walk-forward defaults were moved too). The
  Terminator champion has been re-validated against the real floor: survives
  the actual sequence with $678 headroom, MC P(breach) 1.4% (was $1,178 /
  0.4% at the wrong $2,500). Flat **5 minutes before close** (close = the
  17:00 ET halt, so flat by 16:55 ET — matches terminator_rec.py). Max
  position size **6 full-size minis or 60 micros** — now enforced by the
  broker (`ContractSpec.apex_max_position`, clamps net position; auto-applied
  per symbol, override via `Strategy.max_position`, 0 disables). **Minimum
  trade duration 30 seconds** (a trade closed faster doesn't count / may be a
  rule violation): every run now REPORTS sub-30s exposure (metrics.py
  `sub30s_*`, shown in console + tearsheet). On the champion: 11.1% (110/990)
  close sub-30s, median 577s, and those trades are collectively **−$4,211**
  (a net drag, not hidden profit). Enforcement is also modeled:
  `Strategy.min_hold_s` (engine sets `strat._now_ts` each bar; `hold_ok()` /
  `position_age_s()` gate `close_position`, `force=True` bypasses for risk
  stand-downs). Terminator reversals defer to the 30s mark via a
  `want_reverse` intent. Enforcing it on the champion costs only **−$78
  (0.3%)** (net $22,331) — the edge does not rely on sub-30s exits.
  terminator_rec keeps `min_hold_s=0` for NT8-port parity (the C# has no 30s
  logic); the $78 is the compliance cost, not a config change. ~101 sub-30s
  trades REMAIN even when enforced because they are hard **stop-outs** (the
  100-tick bracket stop is not deferred — a firm-rule matter, not a
  fill-model one; worth confirming with Apex whether a sub-30s stop fill is
  voided the way a manual quick close is).
- **US/Eastern ONLY in everything user-facing** (sessions, entry windows,
  reports, hour attributions) — explicit user preference 2026-07-05; their
  PC/NT8/community all run ET. Do NOT express times in CT, even though CME
  is a Chicago exchange. Internals remain int64 ns UTC.
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
- Commissions in contracts.py are the Apex **Tradovate** all-in round-turns
  (per apextraderfunding.com help center, 2026-07-09): minis $3.10, equity
  micros $1.04, CL $3.34/MCL $1.34, GC $3.54/MGC $1.34. Apex's Rithmic rates
  differ (minis $3.98, micros $1.02) — override per run if applicable. MNQ
  was already $1.04 before this calibration, so pre-existing MNQ results
  (incl. the Terminator champion) are unaffected.

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

Validation reference run (EmaCross, MNQ 1m, defaults, cache v2/v3 —
post-timestamp-fix 2026-07-05): net ~-$2,125, 2102 trades, WR 33.4%,
Sharpe -2.05, maxDD -$3,244, breach 2026-04-22. (The strategy is a loser on
true RTH; it's a regression canary, not an edge.) If a refactor moves these
numbers materially without an intentional fill-model/data change, something
broke. Terminator corrected headline: session 14:00-20:55 ET + 200t stop =
net ~$7,142, Sharpe 2.87 (see strategy/ reports).
