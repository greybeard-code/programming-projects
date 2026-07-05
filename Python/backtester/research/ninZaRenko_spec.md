# ninZaRenko — Reverse-Engineered Specification

Status 2026-07-05: **complete** to the limit of available evidence.
Validated against a real ninZaRenko 100/4 MNQ chart export (May 5–18, 2026,
19,868 bars; `tools/bars_MNQ_ninZaRenko_100-4.csv`), cross-checked with the
published trader manual (Scribd 392092944). Implemented in
`backtester/data.py::build_renko_bars` (bars cache v4). No decompiled ninZa
code was used or accepted.

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

- Clean sessions (no live-feed interruptions): **97–99.3% bar-for-bar
  identity** with our builder over full sessions of 700–4,100 bars.
- Sessions that disagree start in perfect sync then jump offset mid-session
  (0 → +10 → ±2 ticks): the live chart re-anchoring after **data-feed
  reconnects** — path-dependent chart-instance artifacts that no rebuild
  from historical data (ours or NT8's own) can reproduce.
- Bonus finding: the parity work exposed that the raw data repo's
  timestamps were **ET wall clock, not UTC** (fixed in reduction, cache v2).

## Remaining (needs fresh NT8 exports)

1. **Reload test (decisive)**: same 100/4 chart, right-click → Reload All
   Historical Data (or reopen the chart), re-export with gbBarExporter.
   Prediction: reload-built bars match ours ~99% in EVERY session, because
   the reconnect re-anchors vanish. Confirms mismatches are chart artifacts.
2. **Cross-parameter confirmation**: 2–3 days of a second setting
   (e.g. 40/10; an odd pair like 10-3 sharpens the 2B−T test).
   Run: `python tools\compare_bars.py <csv> --symbol MNQ --period r40-10`.

## Implications for backtesting

- Signals computed on our bars are faithful to what a (deterministic,
  reload-built) ninZaRenko chart shows.
- A LIVE chart's bars are not even faithful to themselves across reconnects
  — two traders running the same live chart config can hold different bars.
  Renko-signal strategies inherit that nondeterminism in live trading;
  expect occasional live signals that a rebuilt chart wouldn't show.
- Fills in this backtester never depend on renko prices (they resolve on
  real ticks), so none of this affects fill fidelity — only signal timing.
