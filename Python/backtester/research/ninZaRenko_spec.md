# ninZaRenko — Reverse-Engineered Specification

Status 2026-07-05: **complete and cross-parameter validated.**
Validated against five real ninZaRenko MNQ chart exports
(`tools/bars_MNQ_ninZaRenko_*.csv`): 100/4 (May 5–18, 19,868 bars),
64/16 (May 4–19, 12,841), and single-day fresh loads 10/3, 36/2, 40/10
(21,618 bars combined), cross-checked with the published trader manual
(Scribd 392092944). Implemented in `backtester/data.py::build_renko_bars`
(bars cache v4). No decompiled ninZa code was used or accepted.

## Parameters

- **B** — brick size, ticks: the body height of every regular bar.
- **T** — trend threshold, ticks: with-trend close distance from prev close.
- Derived: open offset = B − T; reversal threshold = 2B − T.

## Bar rules (all verified 100% on the export where noted)

1. **With-trend bar**: closes at `prev_close ± T`; open = `close ∓ B`
   (body exactly B; consecutive bars overlap by B − T).
   [export: 99.92% of steps are exactly ±T]
2. **Reversal bar**: closes at `prev_close ∓ (2B − T)`; open = `close ± B`
   (body exactly B, both directions equal).
   [export: reversal steps exactly ±(2B−T), 384/384]
3. **Gap moves** emit multiple bars on one tick (same timestamp).
4. **High/Low** include the synthetic open and close plus the real traded
   extremes of the bar's tick span (what NT8 indicators see).
5. **Session close (17:00 ET halt / weekend)**: the *forming* bar is closed
   as a partial at the last traded price. Its open is the forming bar's
   true open, `anchor ∓ (B − T)` (unique regardless of eventual direction).
   [export: 16:59:59 partials match when the session was in sync]
6. **Session open (18:00 ET)**: the grid **re-anchors at the session's
   first trade**. The first bar closes at `anchor ± T` with **body = T and
   open = the anchor itself** (not body B).
   [export: every 18:00:00 first bar has body = T; closes match ours]
7. No continuous grid across sessions; anchors are session-local.

## Validation results

**Geometry — parametrically exact.** Invariant check on NT8's *own* bars
(independent of our tick data): across all five settings — 32,587
with-trend steps, 4,216 reversals — **zero violations**. Every step is
exactly ±T or ±(2B−T) (17t, 70t, 70t, 112t, 196t), every regular body is
exactly B, and the multi-day files contain exactly one body=T bar per
trading day (10/10 and 12/12), confirming re-anchor happens **only at the
real 18:00 ET session open**, never on mere quiet spells.

**Path parity vs our builder** (one-to-one matcher, 10 s tolerance —
the two tick feeds skew by up to ~6 s):

| setting | chart type | matched by time | identical close |
|---|---|---|---|
| 40/10 | fresh load, 1 day | 96.9% | **95.8%** |
| 10/3 | fresh load, 1 day | 98.6% | **93.0%** |
| 36/2 | fresh load, 1 day | 96.6% | 68.4% |
| 100/4 | live-accumulated, 10 days | ~94% | clean sessions 97–99.3% |
| 64/16 | live-accumulated, 12 days | ~85% | clean sessions 93–95% |

Two — and only two — divergence mechanisms, distinguishable by the offset
value:

1. **Feed differences** (NT8 historical tick data vs our Market Replay
   recordings; single-tick extremes and a few seconds of timing skew,
   verified by tick-level forensics). These flip a bar and offset the grids
   by exactly **±T — a multiple of the shared price lattice — so they
   self-heal**: the lag closes as soon as price traverses both thresholds.
   Impact scales inversely with T (36/2's half-point steps are maximally
   sensitive; its 126 reversals per 4,655 bars heal slowly).
2. **Live-feed reconnect re-anchors** (live-accumulated charts only). The
   new anchor is an arbitrary trade price, so the offset is **never a
   multiple of T** (observed: −1, ±2 with T=4; +5, −6, −4 with T=16) — a
   different lattice that persists to session end. Fresh-loaded charts show
   no persistent offsets at all, which settles the reload question without
   a separate reload test.

**Session-reset heuristic audit**: every >30-minute trade gap in the repo
(131 across Dec 2025–Jun 2026) is a genuine session boundary — the daily
17:00 ET halt, Sunday pre-open, or a holiday early close (13:00 ET halts on
MLK/Presidents'/Memorial Day, Good Friday). Zero false positives, and the
gap rule handles early-close holidays that a fixed-18:00 rule would miss.

Bonus finding: the parity work exposed that the raw data repo's
timestamps were **ET wall clock, not UTC** (fixed in reduction, cache v2).

## Implications for backtesting

- Signals computed on our bars are faithful to what a (deterministic,
  reload-built) ninZaRenko chart shows. Prefer T ≥ 8–10 ticks on MNQ if
  chart-vs-backtest reproducibility matters: sub-point trend thresholds
  amplify feed-level noise in ANY implementation, including NT8's own.
- A LIVE chart's bars are not even faithful to themselves across reconnects
  — two traders running the same live chart config can hold different bars.
  Renko-signal strategies inherit that nondeterminism in live trading;
  expect occasional live signals that a rebuilt chart wouldn't show.
- Fills in this backtester never depend on renko prices (they resolve on
  real ticks), so none of this affects fill fidelity — only signal timing.
