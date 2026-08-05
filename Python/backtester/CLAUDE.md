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
  on_finish; buy_bracket, move_stop, move_stop_to_breakeven, ...).
  Multi-timeframe: declare `secondary_periods` (e.g. ["15m"]); the engine
  appends each secondary bar the instant it closes (ts_end <= primary ts, no
  look-ahead), fires optional `on_secondary_bar(bar, bars, period)`, and
  exposes completed bars via `self.secondary(period)` — see
  strategies/mtf_example.py. Optional `on_tick(ts, price, index)`: defining
  it switches that run to a per-event resolver (broker.resolve_span_ticks,
  slower Python-per-event) where orders submitted in on_tick fill on LATER
  events (no look-ahead); strategies that define neither stay on the exact
  fast path (champion re-run bit-identical). indicators.py has incremental
  NT8-style indicators (EMA, SMA, ATR, RSI, EfficiencyRatio, Highest,
  Lowest). Bar types via `period` / BarSpec:
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
- **TBars ("tb120")** — LANDED 2026-08-04, parity gate RUN 2026-08-04. Port of
  the vendor `TBars` bar type as shipped in **TBarsNEW.dll** (the build
  installed in NT8 here; custom BarsPeriodType **98765**). Full writeup:
  research/TBars_spec.md. ONE parameter, NT8's "Speed Settings" N, from
  which Configure derives everything — trend `N//2` ticks, reversal `N*2`,
  synthetic open offset `N` — so **a reversal costs 4x a continuation**
  (that asymmetry is the "T"). Breakout is STRICT and the completing tick
  clamps exactly to the threshold, so closes stay on the tick grid. The
  emitted **OHLC is Heikin-Ashi transformed** (close = 4-way average, open
  = midpoint of synthetic open and prior close; H/L are the real extremes —
  the DLL's HA high/low helpers are dead code): that is FAITHFUL, since it
  is what NT8 charts, and fills are unaffected because the engine still
  resolves on real ticks. Two deliberate divergences: (1) the breakout
  tick's volume goes to the NEW bar only, because NT8 counts it in BOTH
  (UpdateBar *and* AddBar) and this repo requires non-overlapping `[i0,i1)`
  spans or an order could fill twice — the double-count is CONFIRMED, not
  inferred: 97.8% of bars satisfy `nt8_vol == our_vol + vol[breakout tick]`
  and NT8's bar volumes sum to 2,292 contracts MORE than were traded; (2) at a gap re-seed `bar_dir` is
  dropped by default (`reset_carries_dir=False`) — the DLL carries it and
  seeds `open ± trend*dir`, which INVERTS when the prior direction was down
  and emits a bar whose OPEN sits above its own high (hand-verified:
  O=98.00 H=97.50 L=97.50 C=97.50), which every indicator would consume. Reset is
  gap-driven here, as for renko/saber; NT8's own reset is the Data Series
  **"Break at EOD"** toggle (`Bars.IsResetOnNewTradingDay`), which
  DEFAULTS TO ENABLED (user-confirmed 2026-08-04) — so the re-seed path IS
  live on a stock chart, and its reset POINT is the trading-hours template
  boundary, not a trade gap.  That difference is residual (b) above. Expect
  ~1 doji stub bar per reset (faithful — the seed collapses both thresholds
  onto the open); MGC sees ~1.8 gaps/day, MNQ ~0.7. Reference config, and
  what the parity gate should use: **MGC Speed 120** (trend $6 / reversal
  $24 / open offset $12, ~21 bars/day). The three TBars builds found in
  `nt8 code/TBarsNew/` are one class with identical math; the `.cs` one is
  a DECOMPILATION with the license check stripped, not vendor source — same
  no-decompiled-source policy as ninZa applies.
  **CRITICAL, found by the parity gate:** NT8 stores every bar price on the
  TICK GRID, rounded **half-to-even** (.NET Math.Round default), and that
  rounding is INSIDE the state loop (the next bar's HA open reads the stored
  close), so rounding only at the end drifts. Without it OHLC parity was
  **0.1%**; with it 79.3%. Half-up scores 71.6%, half-down 72.7% — to-even
  is right. **Parity gate result** (MNQ 09-26 Speed 120, 2026-07-19..07-31,
  2232 bars, Break at EOD ON): timing 99.1%, high/low 98.7%, close 91.5%,
  full OHLC 79.3%. Geometry + bar TIMING are certified (bar 1 verified by
  hand to the cent); residual is (a) ±1 tick on the two HA averages on ~9%
  of bars, which is error PROPAGATION not a wrong tie-break — both formulas
  were verified against NT8's own stored values — and (b) 29 bars (1.3%) at
  session boundaries, because NT8 with Break at EOD ON re-seeds at the
  trading-hours TEMPLATE boundary while this port re-seeds on a >30 min trade
  gap. CORRECTED 2026-08-05: an earlier note here blamed the reset POINT,
  because `reset_carries_dir=True` moved the total only 78.8%->79.5%. Wrong
  inference — that reproduces the bug at the PORT's reset points, which don't
  coincide with NT8's. `NinjaScript/gbTBars/` fixes the direction carry on the
  NT8 side and geometry mismatches fall 29 -> 2 (high/low 99.9%, OHLC 80.5%);
  the reset-point difference is benign once neither side emits a malformed
  bar. What remains is purely the +-1 tick HA rounding propagation. The volume gate also exposed a hole on OUR
  side, now FIXED (2026-08-04): a bar still forming at a day-file boundary
  carried its geometry but not its accumulated volume, so bar volumes summed
  0.64% below traded volume; SaberRenko had the identical hole. Both carry
  tuples grew `(volume, buy_volume, sell_volume)` and BARS_VERSION went 7->8
  (an old cache's end_state no longer unpacks). Residual is now -0.086%,
  which is just the bar still forming when data ends and is correct.
  Don't compare TBars parity to ninZaRenko's
  96-100%: TBars emits a 4-way average of four rounded quantities, so one
  tick of divergence anywhere is structurally far more visible.

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
- **Renko day-boundary fix (2026-07-11, major):** the five settings above
  were validated on single-session exports and missed a real bug — day
  files are ET calendar days, but an overnight session (18:00-16:55 ET;
  GodZillaKilla AND the Terminator champion both use this shape) trades
  straight through midnight ET with no real gap there, while
  `build_renko_bars` reset its brick anchor at the start of every file
  regardless. Confirmed via a fresh NT8 export (MNQ r60-3): mismatch rate
  0-7% right after the real 17:00-18:00 halt reset, jumping to 45-69% at
  midnight ET, broken until the next real halt. Fixed: `build_renko_bars`
  takes optional `carry_anchor`/`carry_dir` (backward compatible; default
  = original fresh-start), and `Catalog.load_bars_sequence` threads that
  state across a contiguous day range, resetting ONLY on a genuine gap
  (>30 min) — never merely because a new file started. `engine.py`,
  `tools/compare_bars.py`, `tools/compare_signals.py` all use the
  sequence-aware loader now; BARS_VERSION bumped (rebuilds the bar cache
  transparently). Verified: identical-OHLC 61.1% → 99.8% on the r60-3
  export. Headline numbers barely moved (Terminator champion $22,409 →
  $22,422; see NinjaScript/TerminatorV2/TerminatorV2.md and
  strategy/GodZillaKilla.md for
  full before/after) — the bug was real but the champion's SAR signal
  turned out robust to it. If you see this bug pattern again (bar mismatch
  clustering right at midnight ET) on some OTHER as-yet-untested renko
  setting, this is the fix, not a new investigation.
- **CME trading day**: runs **18:00 ET (prior calendar day) → 17:00 ET**,
  with the 17:00–18:00 ET daily maintenance halt marking the boundary
  between one trading day and the next (verified across all 3 DST
  transitions in the repo — halt always sits at stamped 17:00 ET, reopen
  always 18:00:00 ET). A session spanning `("18:00","16:55")` (the engine's
  overnight-session support) is therefore ONE compliant trading day: it
  flattens once, before the *next* halt, and never holds a position through
  any halt — this is a materially different (and much better) framing than
  restricting trading to a short daytime box. See
  NinjaScript/TerminatorV2/TerminatorV2.md §3 and TerminatorV2_ETH.md §4
  for why this matters.
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

## GodZillaKilla confluence backtest — validated configs (2026-07-26)

A multi-day study (MNQ, r70-4 ninZaRenko, full history 2024-12-16..2026-07-17,
1 contract unless noted) starting from a user-specified 4-of-6-engine gate
ended up producing the most validated, actionable result in this repo. Full
narrative is in the session's Word docs (`reports/GodZillaKilla_Backtest_
Findings.docx`, `GodZillaKilla_RealMoney_ReEvaluation.docx` — both gitignored,
regenerate via the scratch scripts noted below if needed); this is the
distilled, load-bearing summary. **No committed `strategies/*.py` file
encodes this yet** — configure a `GodZillaKilla()` instance per the settings
below (see `strategies/godzilla_killa.py` for the attribute names), same
pattern as the `_run` functions in the (uncommitted, scratchpad-only) sweep
scripts this study used.

**Methodology, so the numbers below can be trusted without rederiving them:**
the strategy's raw backtest looked profitable almost everywhere at first, but
most of that P&L was sub-30-second scalps that Apex's real 30s minimum-hold
rule would void (measured as "rule-adjusted net" = raw net minus sub-30s
*winners*, a conservative estimate — this rule cannot be enforced in the sim
itself because GZK's exits are broker-side ATM brackets, `min_hold_s` only
gates `close_position()`). Only an evening entry window survives that rule
honestly; walk-forward on the original strict gate was UNINFORMATIVE
(some OOS folds had exactly 1 trade); relaxing to a looser gate unlocked
enough trades for a real out-of-sample verdict; a **zero-optimization
fixed-config check** (one pinned config, run once, sliced into time segments
it was never tuned on) is what actually validated the edge — it overturned
two false alarms from the walk-forward stage (an apparent Q1'26 "regime
failure" and a low walk-forward-efficiency score both turned out to be
optimizer selection-bias artifacts, not real problems, once optimization was
removed entirely).

**Gate (both configs below):** all 6 signal engines *disabled except*
TH (ThunderZilla) + PA (PanaKanal) + SJ (SuperJump); `set1_required=3`
(all 3 must agree — looser than the user's original 4-of-6-with-KO/SU/NC-
available ask, which only fired ~2.3x/month, too rare to validate).
1-bar confirmation. Entry window **20:00-20:45 ET** (8:00-8:45pm). Windows
past ~21:30 ET decay (trades get slower but the edge disappears); the
9:30-10:15am morning window is a real but materially worse-risk-adjusted
alternative (see below).

- **Prop-firm version** (Apex $2,000 trailing floor; this is what the CLI's
  `--prop-threshold` / `PropFirmConfig` model): flat exits, **TP 60 / SL 200
  ticks**, **3 contracts** on a 50K account. Backtested: net $2,803/19mo,
  WR 82.9%, PF 1.39, maxDD -$830 (survives w/ $1,159 headroom), Monte Carlo
  (5000 sims) P(breach $2k)=13%, **P(pass a $3k eval before breaching)=56%**
  — the first config in this whole project with a real eval-pass
  probability (1 contract alone makes too little to ever reach $3k: 0% pass).
  6 contracts barely improves pass odds (59%) for 3x the breach risk (45%);
  9 contracts breaches outright (net/DD math: -$277/contract maxDD x N
  crosses the $2k floor around 7x). **Do not run this on NQ** — full-size NQ
  is 10x MNQ's dollar value, so even 1 NQ contract overshoots the floor
  (flat exit: maxDD -$4,531, 75% MC breach; even a hand-tuned tight SL60
  survives by only $213, a coin-flip 48% breach) — micros are the only way
  to size into the validated 3-contract sweet spot.
- **Real-money version** (no prop-firm rules — no 30s min-hold, no trailing
  floor; this is `prop=None` in `Backtest(...)`, i.e. omit `--prop-threshold`
  or pass 0): the stop TIGHTENS from 200 to **TP 60 / SL 150 ticks** (no
  floor to protect, so the tighter stop wins outright: more net, better
  Sharpe, smaller drawdown, at any contract count you're comfortable
  sizing to your own risk tolerance — no external cap). Backtested at 1
  contract: net $1,111/19mo (~140 trades, ~7.4/mo), WR 80%, PF 1.52,
  Sharpe 1.83, maxDD -$227. **Further upgrade, confirmed real:** set
  ThunderZilla's `thunder_params['trend_period'] = 300` (default 200, SMA)
  — net rises to $1,178, Sharpe to 2.05, maxDD unchanged, on ~4% fewer
  trades (skips a few marginal signals) — a clean improvement with no
  downside on this window. Trade logs (ET timestamps) for all combos:
  `reports/GZK_realmoney_MNQ_8pm_TP60SL150{,_TH300}_trades.csv` (gitignored,
  regenerate if missing).
- **Morning window (9:30-10:15am ET, TP80/SL200)** is a real, higher-
  frequency (~48 trades/mo), higher-absolute-dollar alternative under
  either prop or real-money framing, but materially riskier: real-money net
  $1,973 (SMA=300: $2,793) vs. evening's $1,111-$1,178, at **7.7x the max
  drawdown** for less than 2x the profit and roughly half the Sharpe.
  Prefer evening for capital preservation; morning only if raw trade
  frequency/dollars matter more than risk-adjusted quality.

## State / roadmap (updated 2026-07-26)

Done: engine + fills + brackets + order modification, Apex tracker + daily
loss limit, metrics (Sharpe/Sortino/Calmar/gross-vs-net), tearsheet + trades
CSV, NT8 comparison tool (tools/compare_nt8.py — awaiting a real NT8 export
to validate against), Monte Carlo with Apex breach / eval-pass probability,
bar types (time/tick/ninZaRenko per the published manual), order-flow data
(aggressor delta, cum_delta, quote sizes), parameter sweep + sensitivity,
walk-forward runner, Carver sizing, 53 unit tests.

GodZillaKilla port — LANDED 2026-07-10 (Phases 0-4+6 of the plan; 136 tests).
Authoritative source: Documents\NinjaTrader 8\bin\Custom\Strategies\Playr101\
GodZillaKilla.cs v1.10.0 (repo snapshot + six gb indicator sources + reference
templates in `nt8 code/GodZillaKilla/`). Pieces:
- backtester/nt8config.py — parses ATM template XML (brackets/BE/trail; Ticks
  only) and NT8 strategy-template XML (flat <Strategy> block; BarsPeriod
  type id 12345 = ninZaRenko -> r{brick}-{trend}; missing props = compiled
  defaults, faithful to NT8 load — old presets silently enable NC).
- backtester/atm.py + broker StopPlan — multi-bracket scale-out, per-leg OCO,
  auto-breakeven, tiered auto-trail (vectorized cummax in span resolution;
  champion re-run bit-identical after the change).
- backtester/gbsignals/ — six Signal_Trade engine ports (KO order blocks,
  PA Keltner, TH trend+OBOS, SJ zones, SU multi-MA, NC cloud), NT8-exact
  primitives in nt8math.py (EMA first-value seed, partial-window SMA/StdDev,
  ApproxCompare 1e-10) — deliberately NOT indicators.py semantics.
- strategies/godzilla_killa.py — two trigger sets w/ operators + require
  veto, ConfirmationBars, EMA filter, TF1-3 + skip windows (entries-only;
  per-window flatten), reversal, `from_template()`. Engine gained
  daily_profit_target (tag "dpt") mirroring daily_loss_limit; both resolve
  from strategy attrs.
- sweep_confluence.py — league table over engine subsets x required-count x
  require flags (the user's stated research focus).
Phase 5 signal-parity DONE 2026-07-11 (tools/gbSignalExporter.cs on MNQ
r60-3, 2026-04-30..05-19, via tools/compare_signals.py): 5/6 engines
(KO/PA/SJ/SU/NC) at 97-99% signal-bar match. TH sits at 82% with one
isolated residual (its OBOS overbought-exit code -2 at 13.8% vs mirror +2 at
98.9%; every other TH code 96.5%+) — narrowed to an MFI overbought-detection
gap, not yet root-caused, not blocking. This pass also SURFACED AND FIXED the
renko day-boundary reset bug above — most of what first looked like signal-
port bugs was actually that. Full-sample re-run (OneSet_3ofAll_BestTime,
$2,000 floor): 3-of-6 net -$10,411 (was -$10,363 pre-fix), 3-of-5 control
net -$19,246 (was -$16,246) — both still decisive losers, breach either way;
see strategy/GodZillaKilla.md for the full writeup. REMAINING: a Strategy
Analyzer trade export -> tools/compare_nt8.py for full trade-list
certification.

Next (order per research/22_Books_Summary.md, distilled from the 22-book
docx in research/):
2. ~~Per-day trade chart~~ DONE: tools/plot_day.py (candles + entry/exit
   markers + entry-window shading, ET; warm-up lead-in so signals match a
   full run). Note: small-T renko (r100-4) prints thousands of overlapping
   bricks/day — dense but correct.
3. Validate ninZaRenko bar parity against an NT8 chart export.
4. OIB/delta example strategy using bars.cum_delta.
NT8 time-filter reconciliation — DONE 2026-07-09. Measured all three window
semantics over 510 days ($2k floor): entries-only $22,409 (survives);
FlattenAtEnd=true $16,146 (−28%, the carry is real profit); FlattenAtEnd=
false $21,907 but BREACHES (−$26 headroom, holds through out-of-window
reversal signals). So the champion needs entries-only semantics. Modeled in
Python via terminator_v2.py flags `flatten_at_window_end` /
`window_blocks_reversal` (both default off). Terminator_V2.cs has a **Time
Filter Entries Only** mode (gates entries, reversal exit always fires,
window-end flatten disabled) — see NinjaScript/TerminatorV2/TerminatorV2.md §9.
- **Terminator_V2.cs branch merge — v2.4.3, 2026-07-23.** The .cs had FORKED
  into two lineages that each shipped a **"v2.4.1" for different changes**:
  `NinjaScript/TerminatorV2/` (the live line — manual brackets, dashboard,
  2nd time window) and `Python/backtester/nt8 code/Terminatorv2/` (labeled
  "v2.4.2" — entries-only mode + the carried-position Day-PnL fix). Neither
  was a superset. The two backtester-side features were merged INTO the live
  line as **v2.4.3**, which compiles clean, and the duplicate `.cs` under
  `nt8 code/Terminatorv2/` was **DELETED** — it had taken three real commits
  of feature development (`91ad407`, `11724d0`, `67748b1`), which is exactly
  how the collision happened. Recoverable from git history if ever needed.
  Entries-only overrides BOTH windows' flatten flags (the live line has an
  independent flag per window; the old 2.4.2 had one shared flag).
  REMAINING: Playback + compare_nt8 trade-list certification before live —
  the merge is compile-verified, not behavior-verified.
- **Terminator lives OUTSIDE this project.** As of 2026-07-23 all Terminator
  material — `Terminator_V2.cs`, the evaluation reports (`TerminatorV2.md`,
  `_ETH`, `_PKfunded`), NT8 templates, and committed trade lists/sweeps —
  is in **`NinjaScript/TerminatorV2/`** at the monorepo root, NOT in
  `strategy/` here. This backtester still owns the Python ports
  (`strategies/terminator_*.py`) and generates its runs into `reports/`
  (gitignored); write findings up in the reports over there. Do not create a
  second copy of the `.cs` or the docs under `Python/backtester/`.
  General rule this follows: **your own NinjaScript lives in
  `NinjaScript/<Project>/`; other people's code** (e.g. Playr101's
  GodZillaKilla) **gets snapshotted under `nt8 code/`** — which is why
  `nt8 code/GodZillaKilla/` legitimately stays.

- **Recent small additions (2026-07-26):** `strategies/god_trades_zeus.py`
  gained `max_stop_ticks` (0=off) — skips a signal whose candle-back stop
  would be wider than N ticks, mirroring gbZeus's `MaxStopTicks`.
  `strategies/terminator_mcl.py` (MCL/micro-crude Terminator_V2 config,
  r40-2, evening-only) **FAILED validation and must not be deployed**: MCL
  only has ~4 months of parquet history, and the one real out-of-sample
  test (walk-forward) came back WFE 0.41 with no parameter convergence —
  the in-sample "Sharpe 3.58" story doesn't survive contact with OOS data;
  kept in the repo only as a recorded negative result, re-evaluate only if
  MCL gets a multi-year history. `strategies/terminator_scaleout.py` is an
  unfinished research build testing the ATM scale-out exit path (TP1/TP2/
  runner via `backtester/atm.py`) on the validated Terminator champion's
  signal/session — not yet swept. `tools/databento_fill.py` fills gaps in
  the L1 Parquet cache from Databento's TBBO schema when the NT8 recorder
  missed a day (stdlib HTTP only, no pandas dependency, needs
  `DATABENTO_API_KEY`). Note: L2 (market depth) was investigated and
  abandoned — the NRD recorder never actually captured depth (recorded L2
  parquet files are effectively empty rows), so L2-anything is a dead end,
  not a roadmap item; the repo's real order-flow edge (aggressor side +
  quote sizes, see below) is L1-only.

Validation reference run (EmaCross, MNQ 1m, defaults, cache v2/v3 —
post-timestamp-fix 2026-07-05): net ~-$2,125, 2102 trades, WR 33.4%,
Sharpe -2.05, maxDD -$3,244, breach 2026-04-22. (The strategy is a loser on
true RTH; it's a regression canary, not an edge.) If a refactor moves these
numbers materially without an intentional fill-model/data change, something
broke. Terminator corrected headline: session 14:00-20:55 ET + 200t stop =
net ~$7,142, Sharpe 2.87 (see strategy/ reports).
