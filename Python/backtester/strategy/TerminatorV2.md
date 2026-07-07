# Terminator_V2 — Evaluation Report

> **REVISED 2026-07-05 — read this first.** The raw data repo's timestamps
> turned out to be US/Eastern wall clock, not UTC (found via ninZaRenko bar
> parity validation). Every session label below is therefore shifted: the
> "RTH 08:30–15:55 CT" runs in this report actually traded ≈12:30–19:55 CT.
> **The P&L was real; the window label was wrong.** On corrected data:
> true-RTH-morning SAR **loses** (−$7,666); the edge lives in the
> **US afternoon + evening reopen**. Corrected recommendation:
> **session 14:00–20:55 ET, ATR 20×4, 200-tick stop, 1 contract** →
> net $7,142, Sharpe 2.87, maxDD −$1,282, MC breach 6.2%,
> eval-pass 92.1%. All times in these reports are US/Eastern from here on.
> Full corrected analysis:
> [TerminatorV2_ETH.md](TerminatorV2_ETH.md) revision section. The
> parameter-robustness conclusions below (period plateau, mult band,
> stop benefit, cost insensitivity) all re-verified on corrected data.

> **REVISED (2) 2026-07-05 — bar-fidelity re-validation.** ninZaRenko bars
> are now bit-identical OHLC to NT8 fresh-load charts after two builder
> corrections: strict breakout (`>`/`<`) and the breakout tick belonging to
> the next bar (bars cache v6; see
> [research/ninZaRenko_spec.md](../research/ninZaRenko_spec.md) rules 8–9).
> Re-ran the recommended config (session 14:00–20:55 ET, ATR 20×4, 200-tick
> stop, 1 contract) on the corrected bars — the edge **holds, marginally
> stronger**: net **$7,677** (was $7,142), Sharpe **2.92** (2.87),
> maxDD −$1,286 (−$1,282), MC P(breach) **5.0%** (6.2%), P(pass $3K eval)
> **93.2%** (92.1%), 320 trades. The +$535 is cleanly attributable to the bar
> corrections (the timestamp fix was already in place when $7,142 was
> computed). NOTE: the §3 default-session baseline below ($7,060 / 346
> trades) is a **pre-timestamp-fix artifact** — the true default session
> (09:30–16:55 ET) loses **−$8,172** on corrected bars, exactly as the first
> revision note predicted; the edge lives only in the afternoon+evening
> window.

> **REVISED (3) 2026-07-06 — full-dataset rerun.** The Parquet repo was
> re-converted (UTC-tagged; round-trip verified bit-identical to the old
> data on all 15 overlap dates) and expanded to **510 MNQ days,
> 2024-12-16 → 2026-07-03** (this report was originally written on 163 days).
> Backtester data discovery was extended to find the importer's year-nested
> layout (`Parquet\<YEAR>\<SYM>-<YEAR>_L1`). Recommended config (14:00–20:55
> ET, ATR 20×4, 200t) over the full ~19 months (471 in-session days): net
> **$12,858**, Sharpe **1.82**, PF 1.25, maxDD −$2,618, 950 trades,
> consistency 16.9%, survived Apex (min headroom $860); MC P(profit) 100%,
> P(breach $2.5K) 24.8%, eval-pass 78.6%. The edge **generalizes
> out-of-sample** — Sharpe is below the Dec2025–Jun2026 slice (2.92) and
> breach risk higher (24.8% vs 5%), but net/PF/consistency hold across
> multiple regimes and a full year of data the strategy never saw when tuned.
> Default 09:30–16:55 still loses (Sharpe −0.23, breached 2025-02-12).

**Source:** `Terminator_v2.4.2\Strategies\Terminator_V2.cs` (NT8, v2.4.2, 2026-07-03)
**Tested as:** MNQ, ninZaRenko 100/4 (`r100-4`), RTH 08:30–15:55 CT, 1 contract,
flat at session end, $50K account, Apex $2,500 trailing threshold.
**Data:** 2025-12-15 → 2026-06-17 (154 trading days, tick-level L1 replay).
**Python port:** `strategies/terminator_v2.py` · Tearsheet:
`reports/TerminatorV2_MNQ_r100-4.html` · Evaluated 2026-07-04.

## 1. What the strategy is

An **ATR trailing-stop stop-and-reverse**. A chandelier-style line trails
price at `ATRMult × ATR(ATRPeriod)` (defaults 4.0 × ATR(20)); a close
crossing above the line signals long, crossing below signals short. It is
always-in by default — no profit target, no protective stop; the opposite
signal is the exit. Reversals are "clean-split": flatten on the signal bar,
re-enter once flat (≤5 bars later, fresh signals override).

The C# implements far more (all off by default): VWMA gate/source, volume
filter, TP/SL in ATR/ticks/$/EMA modes, breakeven, three trail modes with an
arming trigger, dual time windows, daily loss/profit locks, risk-based
sizing, manual draggable brackets, and a dashboard. Code quality is high —
OCO-safe single-signal stop management, oversize/orphan guards, correct
historical→realtime P&L handling. The *edge*, however, lives entirely in the
signal line + the r100-4 bar geometry, which is what was tested.

On r100-4 bars (body 25 pts, closes 1 pt apart, reversal 49 pts) the closes
are extremely smoothed, so the SAR rides long trends and reverses only on
major turns — ~2.2 trades/day.

## 2. Port fidelity

Ported exactly: line-update rules, cross detection, clean-split reversal
timing, cooldown, long/short enables, optional fixed SL/TP. Not ported (all
default-off in the source): VWMA/volume filters, EMA/currency exit modes,
breakeven, trail-trigger, risk sizing, window 2.

Known differences vs an NT8 run:
- **Session:** original defaults to full ETH with exit-on-session-close;
  tested here RTH-only with EOD flatten (prop context). ETH results will
  differ — retest before trading an ETH config.
- **Renko:** built per the published ninZaRenko manual (body B, trend T,
  reversal 2B−T, open offset B−T). Parity vs real ninZaRenko bars is
  **unvalidated** — export a day of bars from NT8 and compare before
  trusting fine detail.
- **Fills:** market orders fill at the real prevailing quote (spread paid);
  NT8 Renko backtests fill at synthetic prices. Our numbers should be the
  more honest of the two.

## 3. Baseline result (defaults: ATR 20, mult 4.0, no SL/TP)

| Metric | Value |
|---|---|
| Net P&L (154 days, 1 contract) | **$7,060** (gross $7,420, commission $360) |
| Trades / win rate | 346 / 48.3% |
| Profit factor | 1.34 |
| Avg win / avg loss | $166 / −$115 |
| Sharpe / Sortino / Calmar | 2.49 / 4.76 / 4.43 |
| Max drawdown | −$2,607 (bar-close basis) |
| Best / worst day | +$1,771 / −$745 |
| Consistency (largest day % of profit) | 25.1% |
| Apex $2.5K trailing | survived, min headroom **$719** |

## 4. Monte Carlo (2,000 resamples, iid — trade autocorr 0.09)

| | default (no SL) | with sl_ticks=200 |
|---|---|---|
| Final P&L 5% / median / 95% | $1,049 / $6,996 / $12,839 | $2,485 / $7,570 / $12,936 |
| P(profitable) | 98% | 99% |
| Max DD median / 5%-worst | −$2,212 / −$3,914 | −$1,772 / −$2,962 |
| **P(breach $2.5K trailing)** | **16.6%** | **6.3%** |
| **P(pass $3K eval before breach)** | **83.9%** | **92.0%** |

The actual sequence survived Apex, but 1-in-6 orderings of the same trades
breach — the no-stop drawdowns are eval-killers when they come early,
before profits lock the floor at start+$100. (A June-1 start breached on
2026-06-16 in a 2-week sub-test; the full run survived only because
earlier profits had locked the floor.)

## 5. Parameter sensitivity (full range, Sharpe)

- `atr_period` 14 / 20 / 28 → **2.50 / 2.49 / 2.46** — a perfect plateau;
  the period barely matters. Strong robustness signal.
- `atr_mult` 3 / 3.5 / 4 / 4.5 / 5 → **1.99 / 1.83 / 2.49 / 0.57 / −0.25** —
  profitable across 3–4 (net $5.5–7.1K everywhere in that band), then a
  cliff: 4.5 collapses, 5 loses money (trail too wide → reverses far too
  late, gives back whole trends). **The 4.0 default performs best but sits
  one step from the cliff.** 3.0–4.0 is the safe zone; do not "optimize"
  upward.

## 6. Walk-forward (9-combo grid, 5 windows, IS/OOS 5:1)

| win | IS Sharpe | OOS Sharpe | OOS net | best params |
|---|---|---|---|---|
| 1 | 1.10 | 1.48 | $427 | 14 / 3.5 |
| 2 | 1.25 | 3.81 | $1,157 | 14 / 3.5 |
| 3 | 2.75 | 5.80 | $1,756 | 28 / 4 |
| 4 | 3.39 | 5.35 | $1,208 | 28 / 4 |
| 5 | 4.41 | 0.44 | $162 | 20 / 4 |

**Stitched OOS: net $4,709 over 77 unseen days, Sharpe 3.19. WFE 1.31.
5/5 windows profitable.** Re-optimized params stay inside the plateau every
window. This is the strongest robustness evidence in the whole report.

## 7. Stress tests

- **Slippage +1 tick on every market/stop fill:** net $7,060 → $6,714,
  Sharpe 2.37. Barely dented — 346 trades in 6 months means execution cost
  is a rounding error. (Compare: the 1-min EMA cross lost 46% of gross to
  costs.)
- **Hard protective stop:** `sl_ticks=200` (50 pts, $100/contract) is a
  strict improvement — net **$7,620**, Sharpe **2.96**, maxDD −$2,142,
  Apex headroom $1,276, breach probability 16.6%→**6.3%**. `sl_ticks=400`
  is neutral (the stop almost never hits). In NT8 terms:
  **SL Mode = Ticks, SL Value = 200.**

## 8. Verdict

**This is a real edge on the tested data, and the most robust strategy
tested in this project so far.** Trend-riding SAR on heavily-smoothed Renko
closes, ~2 trades/day, cost-insensitive, parameter-insensitive across a
broad plateau, and it survives walk-forward with OOS performance *above*
in-sample (WFE 1.31, 5/5 windows).

Recommendations, in order:
1. **Add the 200-tick hard stop** (SL Mode=Ticks, 200). Same trade count,
   more P&L, and 2.6× lower probability of blowing an Apex eval. Pure SAR
   with no stop is the single biggest risk in the current configuration.
1b. **Entry window 09:30–15:30 CT** (see
   [TerminatorV2_ETH.md](TerminatorV2_ETH.md)): net $7,998, Sharpe 3.52,
   MC breach probability 0.7%, eval-pass 97.9%. ETH/overnight trading of
   this strategy loses badly — never run it without the time filter.
2. Trade it in the mult 3.0–4.0 band; never above 4. Period is free (14–28).
3. Validate bar parity: export one day of ninZaRenko 100/4 OHLC from NT8
   and diff against our `r100-4` bars; then run the NT8 Strategy Analyzer
   over a matching window and use `tools/compare_nt8.py` on the trade lists.
4. Retest the ETH (overnight) config separately before running it — all
   results above are RTH-only.
5. Caveats: 6 months of data, one regime (the 2026 uptrend + spring
   correction); micro contract only. Rerun the walk-forward as new data
   accumulates; per the books, expect live ≈ 50–60% of backtest.

## Reproduce

```powershell
.venv\Scripts\python cli.py strategies\terminator_v2.py --mc-target 3000
.venv\Scripts\python sweep.py strategies\terminator_v2.py --param atr_mult=3,3.5,4,4.5,5
.venv\Scripts\python walkforward.py strategies\terminator_v2.py --param atr_period=14,20,28 --param atr_mult=3,3.5,4
```
