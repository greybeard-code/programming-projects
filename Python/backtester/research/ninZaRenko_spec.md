# ninZaRenko — Reverse-Engineered Specification

Status 2026-07-05: **complete and cross-parameter validated; breakout
corrected to strict `>`/`<`.** Validated against five real ninZaRenko MNQ
chart exports (`tools/bars_MNQ_ninZaRenko_*.csv`): 100/4 (May 5–18, 19,868
bars), 64/16 (May 4–19, 12,841), and single-day fresh loads 10/3, 36/2,
40/10 (21,618 bars combined), cross-checked with the published trader manual
(Scribd 392092944) and — as an independent white-box check — a behavioral
summary of `ninZaRenko.cs`
(`research/ninZaRenko_BarType_Engineering_Summary.md`). Our
`backtester/data.py::build_renko_bars` (bars cache v5) predates that summary
and was built clean-room from the manual + chart exports; the summary is
post-hoc confirmation, not a source. No decompiled ninZa code is in the repo.

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
8. **Strict breakout** (`ninZaRenko.cs`: `close > upperLimit` /
   `close < lowerLimit`): a tick landing *exactly on* a threshold updates the
   forming bar but does NOT emit — price must EXCEED the level. Our builder
   originally used inclusive `>=`, which printed a spurious brick on a
   touch-and-reverse and offset the grid by ±T; correcting it lifted
   fresh-load close-parity to 100% (see Validation). One consequence: the
   breakout tick necessarily overshoots, so our completing bar's breakout-side
   extreme (up-bar high / down-bar low) runs 1 tick past NT8's, which clamps
   that bar to the threshold and starts the overshoot tick in the next bar.
   Open and close are exact; only that one extreme differs, by 1 tick.

## Validation results

**Geometry — parametrically exact.** Invariant check on NT8's *own* bars
(independent of our tick data): across all five settings — 32,587
with-trend steps, 4,216 reversals — **zero violations**. Every step is
exactly ±T or ±(2B−T) (17t, 70t, 70t, 112t, 196t), every regular body is
exactly B, and the multi-day files contain exactly one body=T bar per
trading day (10/10 and 12/12), confirming re-anchor happens **only at the
real 18:00 ET session open**, never on mere quiet spells.

**Path parity vs our builder** (one-to-one matcher, 10 s tolerance — the two
tick feeds skew by up to ~6 s). "→ strict" = after the rule-8 correction
(bars cache v5):

| setting | chart type | matched | identical close (`>=` → strict) |
|---|---|---|---|
| 40/10 | fresh load, 1 day | 100% | 95.8% → **100.0%** |
| 36/2 | fresh load, 1 day | 100% | 68.4% → **100.0%** |
| 10/3 | fresh load, 1 day | 90.4% | 93.0% → **96.7%** |
| 100/4 | live-accumulated, 10 days | 97.3% | aggregate 66.5% (reconnect-dominated) |
| 64/16 | live-accumulated, 12 days | 88.3% | aggregate 31.3% (reconnect-dominated) |

Identical *OHLC* is ~0% by construction after the strict fix — every bar's
breakout-side extreme differs by exactly 1 tick (rule 8); open and close are
exact. The live-accumulated rows are all-sessions aggregates and read low
because reconnect re-anchors corrupt whole sessions (mechanism 2 below); the
earlier "clean sessions 93–99%" per-session slices are unchanged by this fix.

The strict-breakout correction reassigned most of what an earlier draft
blamed on "feed differences" to our own inclusive `>=`. What genuinely
remains are two divergence mechanisms, distinguishable by the offset value:

1. **Feed differences** (NT8 historical tick data vs our Market Replay
   recordings; single-tick extremes and a few seconds of timing skew,
   verified by tick-level forensics). These flip a bar and offset the grids
   by exactly **±T — a multiple of the shared price lattice — so they
   self-heal**. After the strict fix they are negligible at 40/10 and 36/2
   (100% close) and visible only at very small T: 10/3 (T=3 ticks = 0.75 pt)
   keeps ~3% close mismatch and a lower match rate, because a single-tick
   feed disagreement flips a brick before it can heal.
2. **Live-feed reconnect re-anchors** (live-accumulated charts only). The
   new anchor is an arbitrary trade price, so the offset is **never a
   multiple of T** (observed: −1, ±2 with T=4; +5, −6, −4 with T=16) — a
   different lattice that persists to session end, and the reason the 100/4
   and 64/16 aggregates are low. Fresh-loaded charts show no persistent
   offsets at all, which settles the reload question without a separate test.

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
- Close-driven signals (renko is a close-driven bar type) get exact parity on
  fresh-load charts. A strategy keying off a renko bar's HIGH/LOW instead sees
  our breakout-side extreme 1 tick beyond NT8's (rule 8). Matching NT8's
  clamped extreme would mean assigning the breakout tick to the *next* bar's
  span — which also shifts that tick's volume and fill resolution, so it would
  move r100-4 strategy numbers. Deferred, not yet applied.
- Fills in this backtester never depend on renko prices (they resolve on
  real ticks), so none of this affects fill fidelity — only signal timing.
