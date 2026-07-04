# 22 Algo Trading Books — Distilled Summary

Condensed from `22_Algo_Trading_Books_Summary_and_Analysis.docx` (May 2026,
v3.0 Unified Edition), which synthesizes 22 books and evaluates the
Ranger/AlgoTrader NT8 codebase against them. This file keeps the actionable
findings for quick reference.

## The eight high-confidence consensus findings

1. **Size at 25% of theoretical optimal leverage** (Kelly / Optimal-f) —
   confirmed by 7 independent books. Half-Kelly is the ceiling; quarter-Kelly
   when max one-day drawdown at half-Kelly exceeds tolerance.
2. **Walk-forward validation, IS/OOS = 5:1, ≥5 windows** is the minimum
   standard before live deployment. Backtests without it are unreliable.
3. **Transaction costs destroy more strategies than bad signals.** Always
   report gross vs net. Chan's example: Sharpe 4.47 gross → −3.19 net after
   5 bp costs. Gross ≥ 2× net Sharpe is a red flag (WorldQuant).
4. **ATR-based stops beat fixed-tick stops** in all conditions. Trending:
   SL 1.0–1.5× ATR(14), TP 2.0–3.0× ATR(14). Never < 1.5× ATR (Carver).
5. **ML works as a signal *filter* (meta-labeling), not a signal generator.**
   Primary signal should be structural; the model estimates P(win | features)
   and gates/sizes entries.
6. **U-shaped intraday volume** in equity index futures: first 60 min and
   last 90 min are the quality windows; 11:30–13:30 ET is noise — suppress
   midday entries.
7. **Ensembles beat single models** (RF + LR: `0.4·P_lr + 0.6·P_rf`),
   60–70% accuracy vs 55–66% for singles.
8. **Monte Carlo permutation/resampling testing is mandatory** before
   trusting any backtest (Davey BWATS + Masters SSML).

## Key formulas

- **Kelly**: `f* = Sharpe / annualized vol`. Use ≤ half-Kelly. Practical cap:
  `max safe leverage = L × (tolerable DD / historical DD at L)`.
  Maximizing OOS Sharpe ≡ maximizing Kelly growth (Chan MT proof).
- **Carver vol targeting**: `DailyCashVolTarget = Capital × 0.15 / 16`;
  `BaseContracts = DailyCashVolTarget / (ATR(25)ticks × tick$)`.
  Rolling capital: +50% of gains, −100% of losses; recalc every 10 trades.
- **Risk of ruin (TSM)**: `P = ((1−A)/(1+A))^(Capital/AvgLoss)`,
  `A = WR − LR×(AvgLoss/AvgWin)`. Min safe capital ≈ 50× avg loss.
  5 consecutive losses at 40% WR is *normal* (7.8% per 100 trades).
- **Efficiency Ratio (Kaufman)**: `|C[n]−C[0]| / Σ|C[i]−C[i−1]|`.
  < 0.12 chop → no entries; > 0.4 trending → tighten trail.
- **Hurst exponent** (Chan QT): H<0.45 mean-revert, H>0.55 trend,
  else chop. Simpler intraday regime detector than HMM.
- **Order imbalance**: `OIB = (BidQty−AskQty)/(BidQty+AskQty)` (queue
  *volumes*, not prices). |OIB| > 0.3 persistent over 3 bars → 60–65%
  short-term directional accuracy (Cartea AHF + Aldridge HFT).
- **Triple-barrier labeling** (Lopez de Prado): label = which of
  {target, stop, time} barrier fires first. Purged K-fold CV with 10–20 bar
  embargo for training.

## Davey's system-development workflow (BWATS)

Idea (hypothesis, not data mining) → fixed stop/target test → refine exits →
walk-forward → **Monte Carlo** (resample trade P&L 1,000+×; outputs P(ruin),
max-DD distribution, 5/95 percentiles; use **block bootstrap** if trade
autocorrelation |r| > 0.2 at any lag) → 30-day incubation → live at minimum
size, scale after 20+ trades. Breakeven stops limit profit — use only to
free-ride. Calmar (CAGR/maxDD) alongside Sharpe; target > 1, quality > 2.

## Market-structure findings for MNQ

- Gap > 0.5%: counter-gap entries in first 30 min fill only ~35% — block them.
- Daily high/low set at open or close in 2.5:1 ratio.
- Spreads widest first 15–30 min of RTH and around news — avoid entries.
- Stops exactly at swing highs/lows get swept; place 3–7+ ticks beyond.
- 1-min momentum edge decays in 1–5 min; 15-min signals capture the
  sustained portion.
- Backtest = **upper bound**: expect live at 50–60% of backtest performance
  (QuantStart SAT). Model ≥ 2 ticks slippage for MNQ if not paying spread.

## Biases taxonomy (SAT + Chan QT)

Optimization/data-snooping (walk-forward is the only immunity; sensitivity
test: vary each param ±20%, demand plateaus), look-ahead, survivorship,
psychological (automation fixes), transaction-cost naivety, regime shift
(test across market periods).

## Ranger (NT8 bot) roadmap from the doc — status snapshot

Already right: partial-class architecture, 4-stage trail, LR meta-label gate,
swing stop buffer 10t, Globex warmup, JSONL/CSV logging with MFE/MAE/ER,
ADX filter, max-daily-trades and consecutive-loss gates.

- P1 (critical, AlgoTrainer.py): f2 dead feature → SessionTrend_at_Entry;
  add RF+LR ensemble; enable MC permutation test. (Doc Part V records these
  as applied in the Ranger build; retrain required — feature slots 2 & 11
  changed meaning.)
- P2 (high): ER chop gate; midday block 11:30–13:30; gap filter; Calmar in
  logger.
- P3 (medium): half-size rebuild after consec-loss gate; OIB feature
  (realtime-only in NT8 — zero in NT8 backtests); ER-adaptive trail.

## Application to the Python backtester (this repo)

Already aligned: event-driven engine with tick fills (SAT gold standard),
spread paid at fill, gross-vs-net reporting, MAE/MFE per trade, Calmar,
EfficiencyRatio indicator.

Build order informed by the books:
1. **Monte Carlo trade resampling** with Apex-breach probability across
   orderings (block bootstrap when autocorrelated) — `montecarlo.py`.
2. Walk-forward runner (IS/OOS 5:1, ≥5 windows).
3. Parameter sweep with ±20% sensitivity plateaus built in.
4. OIB research support: add prevailing bid/ask *sizes* to the reduced cache
   — our L1/L2 data can backtest OIB where NT8 cannot.
5. Carver vol-target sizing helper.
