# Terminator_V2 — Evaluation Report

**Source:** `Terminator_v2.4.2\Strategies\Terminator_V2.cs` (NT8, v2.4.2)
**Data:** 2024-12-16 → 2026-07-03 (510 calendar days, 400 in-session trading
days), tick-level L1 replay, MNQ.
**Python port:** `strategies/terminator_v2.py` (base engine) ·
`strategies/terminator_rec.py` (recommended config below).
Tearsheet: `reports/TerminatorRec_MNQ.html` · Evaluated 2026-07-09 (all
times US/Eastern).

## 1. What the strategy is

An **ATR trailing-stop stop-and-reverse**. A chandelier-style line trails
price at `ATRMult × ATR(ATRPeriod)`; a close crossing above the line
signals long, crossing below signals short. It is always-in by default —
no profit target, no protective stop; the opposite signal is the exit.
Reversals are "clean-split": flatten on the signal bar, re-enter once flat
(≤5 bars later, fresh signals override).

The C# implements far more (all off by default): VWMA gate/source, volume
filter, TP/SL in ATR/ticks/$/EMA modes, breakeven, three trail modes with an
arming trigger, dual time windows, daily loss/profit locks, risk-based
sizing, manual draggable brackets, and a dashboard. Code quality is high —
OCO-safe single-signal stop management, oversize/orphan guards, correct
historical→realtime P&L handling. The *edge*, however, lives entirely in the
signal line + the r100-4 bar geometry, which is what was tested.

On r100-4 bars (body 25 pts, closes 1 pt apart, reversal 49 pts) the closes
are extremely smoothed, so the SAR rides long trends and reverses only on
major turns.

## 2. Port fidelity

Ported exactly: line-update rules, cross detection, clean-split reversal
timing, cooldown, long/short enables, optional fixed SL/TP, dual entry-time
windows (`entry_window`/`entry_window2`, gate new entries only — exits
always manage). Not ported (all default-off in the source): VWMA/volume
filters, EMA/currency exit modes, breakeven, trail-trigger, risk sizing.

Known differences vs an NT8 run:
- **Session:** the raw port class (`terminator_v2.py`) defaults to plain
  RTH (09:30–16:55 ET) with EOD flatten. The recommended config below
  overrides this with a full Globex-trading-day session — see §3.
- **Renko:** built per the published ninZaRenko manual (body B, trend T,
  reversal 2B−T, open offset B−T); validated bit-identical OHLC against
  real NT8 chart exports (see `research/ninZaRenko_spec.md`).
- **Fills:** market orders fill at the real prevailing quote (spread paid);
  NT8 Renko backtests fill at synthetic prices. Our numbers should be the
  more honest of the two.

## 3. Recommended configuration & full-sample result

**Session:** full Globex trading day, `session=("18:00","16:55")` — one
continuous session spanning the prior evening's 18:00 ET open through the
same trading day's 16:55 ET flatten (before the next 17:00 ET halt). This
matches CME's own trading-day boundary and never holds a position through
any halt.

**Entries** (gate new entries only; exits/stops always manage, so a
position can carry from the evening leg into the next afternoon before the
16:55 flatten):
- Afternoon: **15:30–16:55 ET**
- Evening reopen: **18:00–22:55 ET**

**Signal:** ATR(28) × 3.25 SAR on ninZaRenko 100/4 (`r100-4`), 100-tick hard
stop, 1 contract.

| Metric | Value |
|---|---|
| Net P&L | **$22,409** (gross $23,439, commission $1,030) |
| Trades / win rate | 990 / 33.1% |
| Profit factor | 1.69 |
| Avg win / avg loss | $167 / −$49 |
| Sharpe / Sortino / Calmar | 3.90 / 11.20 / 9.49 |
| Max drawdown | −$1,488 (−2.55%) |
| Best / worst day | +$1,797 / −$479 |
| Consistency (largest day % of profit) | 8.0% |
| Prop-firm trailing threshold ($2,000 real Apex floor) | survived, min headroom **$678** |

Tested against the **real $2,000** Apex trailing drawdown (the project default
was corrected from $2,500 to $2,000 on 2026-07-09). The actual 400-day
sequence survives with $678 of headroom to spare; Monte Carlo breach
probability is in §4.

**Compliance verified directly:** 0 of 990 trades have any entry or exit
timestamp inside the 17:00–18:00 ET daily maintenance halt.

**Minimum-hold exposure:** 110 of 990 trades (11.1%) close in under 30
seconds, which Apex does not count as valid trades. Those sub-30s trades are
collectively **−$4,211** — a net drag, not a hidden edge — so enforcing the
rule does not remove profit (see §7 item 2 and CLAUDE.md).

## 4. Monte Carlo (2,000 resamples, iid resampling)

| | Value |
|---|---|
| Final P&L 5% / median / 95% | $15,143 / $22,559 / $30,036 |
| P(profitable) | 100% |
| Max drawdown median / 5%-worst | −$1,513 / −$2,314 |
| **P(breach $2,000 trailing)** | **1.4%** |
| **P(pass $3,000 eval before breach)** | **98.7%** |

## 5. Robustness

- **Split-half:** H1 (Dec 2024–Sep 2025) net $8,465, Sharpe 2.89; H2 (Oct
  2025–Jul 2026) net $13,848, Sharpe 4.84 — both halves profitable,
  stronger in the recent half (consistent with every other cut of this
  data checked in this project).
- **`atr_mult`:** genuine interior peak at 3.0–3.25 (Sharpe 3.7–3.74).
  Checked down to 2.0 (worse, Sharpe ~3.2) and up to 4.5 (much worse,
  Sharpe ~2.3) to confirm this is a real plateau, not a grid-edge artifact.
- **`atr_period`:** 20–60 all cluster Sharpe 3.65–3.81 — a free parameter.
- **Entry-window boundaries:** afternoon start 14:30–16:00 ET and
  evening-leg end 21:55–23:55 ET are both broad plateaus, not knife-edges.
- **Slippage:** the headline pays the spread but no extra slippage. Adding
  1 / 2 ticks of slippage on every market and stop fill (~1,980 fills):

  | extra slippage | net | Sharpe | max DD | $2,000 headroom |
  |---|---|---|---|---|
  | 0 ticks (base) | $22,409 | 3.90 | −$1,488 | $678 |
  | 1 tick | $20,722 | 3.66 | −$1,512 | $618 |
  | 2 ticks | $20,077 | 3.56 | −$1,537 | $591 |

  Still profitable, high-Sharpe, and non-breaching at 2 ticks — the edge is
  not spread-fragile.

## 6. Walk-forward (5 windows, IS/OOS 5:1)

Grid: `atr_mult` ∈ {2.75, 3.0, 3.25, 3.5} × `atr_period` ∈ {20, 28} ×
`sl_ticks` ∈ {100, 125, 150}, session + entry-window structure held fixed.

| win | IS Sharpe | OOS Sharpe | OOS net | best params |
|---|---|---|---|---|
| 1 | 3.54 | 3.40 | $1,834 | mult 3.25 / period 28 / sl 150 |
| 2 | 3.53 | 4.39 | $2,173 | mult 3.25 / period 28 / sl 100 |
| 3 | 3.95 | 3.07 | $1,260 | mult 3.25 / period 28 / sl 100 |
| 4 | 3.42 | 7.68 | $4,479 | mult 3.25 / period 28 / sl 100 |
| 5 | 4.68 | 4.04 | $3,463 | mult 3.25 / period 28 / sl 100 |

**Stitched OOS: net $13,034 over 203 unseen days, Sharpe 4.45. Walk-forward
efficiency 1.18** (OOS beats IS on average — strong evidence against
curve-fit). **5/5 windows profitable.** Every window independently
converged on `atr_mult`=3.25/`atr_period`=28 (only `sl_ticks` varied,
itself a plateau) — the recommended config uses these walk-forward
converged params rather than the marginally higher full-sample-only peak
(mult=3.0/period=20, Sharpe 3.69 full-sample vs 3.90 for the WF pick).

## 7. Verdict

This is the strongest, most thoroughly validated config found in this
project: real interior parameter plateaus (not edges), both data halves
profitable and improving, 5/5 walk-forward OOS windows profitable with
OOS beating IS, and directly verified compliance (never holds through the
daily halt).

Both prior open items are now resolved; one live-account question remains
(item 2, for the user to confirm with Apex):
1. ~~Re-run against the real $2,000 Apex trailing threshold~~ **DONE
   (2026-07-09):** survives the actual sequence with $678 headroom; MC
   P(breach $2,000) = 1.4%. Still comfortably safe.
2. **30-second minimum trade duration** — modeled and measured
   (2026-07-09). Enforcing the rule (deferring the strategy's own reversal
   exits until the position is 30s old; engine `min_hold_s=30`) moves the
   headline by only **−$78 (0.3%)**: net $22,331, Sharpe 3.89, same max
   drawdown, still survives ($678 headroom). **The edge does not depend on
   sub-30s exits** (they were −$4,211 anyway). Caveat: even with enforcement
   ~101 trades still close sub-30s because those are **hard stop-outs** (the
   100-tick stop), which are not deferred — whether Apex voids a sub-30s
   *stop* fill (vs a manual quick close) is a rule question worth confirming
   with them. terminator_rec itself keeps `min_hold_s=0` to stay bit-for-bit
   with the NT8 port (which has no 30s logic); the $78 figure is the cost of
   compliance, not a change to the recommended config.

## 8. NT8 settings

**Requires Terminator_V2 v2.4.2+** (adds the *Time Filter Entries Only* mode —
see §9; earlier versions cannot reproduce this config, they lose 28% of P&L
or breach the floor).

- **Session template** spanning **18:00 ET → 16:55 ET next day** (flatten
  positions / cancel orders at session end — the actual trading-day
  boundary, not a time filter).
- **Use Time Filter = true**, **Time Filter Entries Only = true**,
  **Flatten At Window End = false**.
- One window: **Start 153000, End 225500** (v2.4.2 has a single window; with
  the 18:00→16:55 session the 16:55–18:00 halt has no bars, so a single
  15:30–22:55 window reproduces both the afternoon and evening entry blocks).
- SL Mode = Ticks, Value = **100**. ATR **28** / Mult **3.25**. 1 contract.

## 9. Port-fidelity: why entries-only mode is required

The recommended config depends on **entries-only** window semantics: the
window blocks new entries, but an opposite SAR signal still exits the live
position, and a position may carry across the out-of-window gap until a
signal / hard stop / session flatten. The two window modes the C# had before
v2.4.2 were measured against this over the full 510 days ($2,000 floor):

| Window semantics | Net | Sharpe | max DD | $2,000 floor |
|---|---|---|---|---|
| **entries-only** (v2.4.2, recommended) | $22,409 | 3.90 | −$1,488 | survives ($678) |
| FlattenAtEnd=true (force-flat at window end) | $16,146 | 3.23 | −$1,530 | survives ($808) |
| FlattenAtEnd=false (reversal exit blocked out of window) | $21,907 | 3.00 | −$2,003 | **BREACHES (−$26)** |

FlattenAtEnd=true throws away the overnight/morning carry (−$6,263, −28%);
FlattenAtEnd=false keeps the P&L but holds through reversal signals outside
the window, deepening drawdowns until it breaches. v2.4.2's *Time Filter
Entries Only* gates entries while letting the reversal exit always fire and
disabling the window-end flatten — reproducing the entries-only column. The
C# change is unit-inspected but **not yet NT8-compiled**; build it and
validate with `tools/compare_nt8.py` against a Python trade export before
trading it live.

## Reproduce

```powershell
.venv\Scripts\python cli.py strategies\terminator_rec.py --mc-target 3000
.venv\Scripts\python walkforward.py strategies\terminator_rec.py --param atr_mult=2.75,3.0,3.25,3.5 --param atr_period=20,28 --param sl_ticks=100,125,150
```
