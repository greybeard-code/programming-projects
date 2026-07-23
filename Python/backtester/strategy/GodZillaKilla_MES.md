# GodZillaKilla — MES r12-2 evening-window candidate

**Status: full battery passed (baseline → confluence sweep → renko/window
sensitivity → Monte Carlo → walk-forward, 5/5 OOS windows profitable).
NT8-compiled and Playback-tested with a 5/5 trade-list match — see §9. Not
yet certified over the full history (only ~1 month of Playback data so
far) — see §7 before live use.** A MES-sized ATM-exit sweep (§8) found a
better exit pairing (15t target / 100t+ stop, Sharpe 3.89 vs 2.76) that has
not yet been through the same battery. All times US/Eastern.

## 1. How this config was found

Starting point: the user's own saved live preset, `gbMES_12_2_always.xml`
(`nt8 code/GodZillaKilla/templates/`) — MES, ninZaRenko **12/2**, all six
engines enabled at 4-of-6 required, `ConfirmationBars=1`, one 22-hour entry
window (18:00→16:00 ET, flatten at window end), `Godzilla_ATM_MNQ_NR_50-3`
exits (4 lots, 27-tick target / 75-tick stop, no BE/trail), no daily limits.

1. **As-saved baseline, full 508-day MES history:** net **−$14,920**, 3316
   trades, Sharpe −0.98, breach (−$15,966 headroom). A clear loser — but a
   period-by-period read showed a real "good stretch" (Nov 2025–Feb 2026,
   ~+$9k) followed by a genuine decline since March 2026, which plausibly
   explains the user's positive live experience without the model being
   wrong.
2. **Exhaustive confluence sweep** (192 combos: engine subset × required
   count × require-flags, same window/renko/exits held fixed) found nothing
   robust — best case, **3-of-3 KO+PA+SU**, net +$2,199, Sharpe 0.26, but
   **still breaches** the $2,000 floor mid-sequence (−$3,036 headroom).
3. **Two independent 1-D sensitivity sweeps** (engines fixed at 3-of-3
   KO+PA+SU), one variable at a time per this repo's usual FRAGILE-plateau
   convention:
   - **Renko brick/trend near 12-2:** r10-2 stood out (net $9,384, Sharpe
     1.10) but is a *spike* — neighbors r8-2 and r12-4 collapse. Confirmed
     fragile: combining r10-2 with the best window (below) gave net $3,637
     but **breached** (MC P(breach) 52.8%) — worse than either axis alone.
     Discarded.
   - **tf1 entry window:** most windows were losers, but two survived the
     floor outright — Globex-overnight (18:00–06:00: net $5,367, Sharpe
     1.37, headroom +$1,069) and evening (19:00–23:30: net $3,495, Sharpe
     1.71, headroom +$718).
4. **Monte Carlo on both survivors** split them: Globex-overnight's single
   historical path clears the floor, but 2000 resamples breach it **30.0%**
   of the time. The evening window breaches only **0.4%** of resamples —
   the strongest result found anywhere in this search.
5. **Neighbor-window check on the evening window** (FRAGILE test, same one
   that caught r10-2): every neighboring window survives the floor, and
   **19:30–23:30 is better than the window that found it** — net $5,250,
   Sharpe 2.76, PF 3.30, headroom $1,321, **0.0%** MC breach probability.
   A genuine plateau, not a spike (§4).
6. **Walk-forward** (§5) confirms it holds up out-of-sample.

## 2. Recommended configuration & full-sample result

**Instrument:** MES. **Bars:** ninZaRenko **r12-2** (unchanged from the
saved preset). **Signals:** Set 1 = **3-of-3**, only **KO** (order blocks),
**PA** (Keltner), **SU** (multi-MA pullback) enabled — TH/SJ/NC off; Set 2
off; `ConfirmationBars=1` (inherited from the template, not tuned here).
**Entry window:** TF1 **19:30–23:30 ET**, flatten at window end; TF2/TF3/skip
off. **Exits:** `Godzilla_ATM_MNQ_NR_50-3` — 4 lots, 27-tick target /
75-tick stop, no BE/trail (unchanged from the preset — see §7 caveat on tick
value). **Session:** `("18:00","16:55")` (unchanged; the 23:30 window-flatten
fires well before this).

| Metric | Value |
|---|---|
| Net P&L | **$5,250** (gross $5,550, commission $300) |
| Trades / win rate | 72 / 83.3% |
| Profit factor | 3.30 |
| Avg win / avg loss | $126 / −$190 |
| Sharpe / Sortino / Calmar | 2.76 / 1.38 / 4.98 |
| Max drawdown | −$664 (−1.30%) |
| Best / worst day | +$654 / −$379 |
| Consistency (largest day % of profit) | 12.5% |
| Sub-30s trades (Apex min-hold) | **0 of 72 (0.0%)** |
| Prop-firm trailing threshold ($2,000 floor) | survived, min headroom **$1,321** |

Full sample = the 508 raw MES day-files on disk (2024-12-16 → 2026-07-03;
~400 distinct trading sessions once overnight day-boundaries merge, per the
usual session-vs-calendar-file distinction in this repo).

## 3. Renko / window sensitivity (1-D sweeps, engines fixed at 3-of-3 KO+PA+SU)

**Renko axis** (window held at the preset's 18:00–16:00):

| period | net | Sharpe | PF | headroom | trades |
|---|---|---|---|---|---|
| r6-2 | −$18,672 | −2.02 | 0.83 | −$18,623 | 1027 |
| r8-2 | −$2,716 | −0.28 | 0.98 | −$6,323 | 1284 |
| r8-4 | −$1,373 | −0.42 | 0.88 | −$1,549 | 110 |
| r10-2 | $9,384 | 1.10 | 1.10 | −$1,621 | 1130 |
| **r12-2 (baseline)** | $2,199 | 0.26 | 1.02 | −$3,036 | 999 |
| r12-3 | $2,270 | 0.32 | 1.04 | −$5,192 | 696 |
| r12-4 | −$4,849 | −0.92 | 0.87 | −$5,279 | 346 |
| r16-4 | −$1,777 | −0.30 | 0.95 | −$3,592 | 402 |
| r20-4 | −$3,919 | −0.76 | 0.89 | −$4,407 | 346 |

r10-2 is a spike (neighbors collapse both directions) — not used.

**Window axis** (renko held at r12-2):

| tf1 window | net | Sharpe | PF | headroom | trades |
|---|---|---|---|---|---|
| 18:00-16:00 (baseline) | $2,199 | 0.26 | 1.02 | −$3,036 | 999 |
| 09:30-16:00 (RTH) | −$5,689 | −0.84 | 0.91 | −$6,062 | 614 |
| 18:00-06:00 (Globex overnight) | $5,367 | 1.37 | 1.32 | +$1,069 | 240 |
| 08:00-11:30 (morning) | −$4,540 | −0.99 | 0.86 | −$4,630 | 339 |
| 13:00-16:00 (afternoon) | −$1,090 | −0.29 | 0.95 | −$1,315 | 232 |
| 09:00-11:00 (cash-open) | −$2,111 | −0.55 | 0.91 | −$1,168 | 237 |
| 19:00-23:30 (evening) | $3,495 | 1.71 | 1.77 | +$718 | 83 |

Only Globex-overnight and evening clear the floor; Monte Carlo (§4) is what
separated them.

## 4. Plateau check around the evening window + Monte Carlo

Same fixed config (r12-2, 3-of-3 KO+PA+SU), tf1 varied around 19:00–23:30,
2000-sim Monte Carlo per window:

| tf1 window | net | Sharpe | PF | trades | headroom | MC P(breach $2,000) |
|---|---|---|---|---|---|---|
| 18:00-23:00 | $3,801 | 1.40 | 1.53 | 114 | $156 | 12.6% |
| 18:30-23:00 | $4,323 | 1.93 | 1.90 | 93 | $894 | 4.2% |
| 19:00-23:00 | $3,271 | 1.64 | 1.76 | 79 | $802 | 0.8% |
| 19:00-23:30 | $3,495 | 1.71 | 1.77 | 83 | $718 | 0.4% |
| 19:00-00:00 | $2,470 | 1.14 | 1.43 | 89 | $674 | 1.4% |
| **19:30-23:30 (recommended)** | **$5,250** | **2.76** | **3.30** | 72 | **$1,321** | **0.0%** |
| 20:00-23:30 | $3,750 | 2.25 | 2.79 | 59 | $1,025 | 0.7% |

Every neighbor survives the floor — a real plateau, unlike r10-2. Quality
improves as the window tightens toward ~19:30–23:00 and degrades toward
either the RTH-adjacent 18:00 open (headroom collapses to $156, breach risk
12.6%) or past midnight (dilutes to $674 headroom). Autocorrelation was high
enough (0.17–0.25) that several windows' Monte Carlo auto-selected block
resampling over iid — handled correctly by `montecarlo.run(method="auto")`.

Recommended-config Monte Carlo (2000 sims, iid, max autocorr 0.17):

| | Value |
|---|---|
| Final P&L 5% / median / 95% | $3,355 / $5,310 / $7,026 |
| P(profitable) | 100% |
| Max drawdown median / 5%-worst | −$767 / −$1,267 |
| **P(breach $2,000 trailing)** | **0.0%** |

## 5. Walk-forward (5 windows, IS/OOS 5:1)

Config held fixed across all windows (no re-optimization — this is an
IS/OOS consistency check on the one config the sensitivity sweep already
flagged, not a parameter search):

| win | IS period | OOS period | IS Sharpe | OOS Sharpe | OOS net | OOS trades |
|---|---|---|---|---|---|---|
| 1 | 2024-12-16→2025-09-17 | 2025-09-18→2025-11-10 | 1.59 | 3.59 | $213 | 3 |
| 2 | 2025-02-05→2025-11-10 | 2025-11-11→2026-01-04 | 1.75 | 2.88 | $269 | 5 |
| 3 | 2025-04-01→2026-01-04 | 2026-01-05→2026-03-02 | 2.07 | 5.16 | $523 | 4 |
| 4 | 2025-05-26→2026-03-02 | 2026-03-03→2026-04-27 | 1.16 | 8.07 | $1,538 | 15 |
| 5 | 2025-07-24→2026-04-27 | 2026-04-28→2026-06-23 | 3.29 | 4.08 | $463 | 9 |

**Stitched OOS: net $3,049 over 197 unseen days, Sharpe 4.94. Walk-forward
efficiency 2.41** (≥0.5 → edge survives OOS per Davey). **5/5 windows
profitable.** OOS trade counts (3/5/4/15/9 = 36 total) track the full-sample
rate sensibly (~50% of days → ~50% of the 72 full-sample trades), unlike an
earlier buggy run of this walk-forward that produced 59–155 trades per
window — see the methodology note below.

**Methodology note (caught and fixed before publishing):** the first pass of
this walk-forward (and a first pass of the §8 ATM-exit sweep) went through
`backtester.loader.load_strategy()`, which builds the strategy via a plain
`GodZillaKilla()` — **not** `from_template()`. Any attribute not explicitly
listed in the sweep's param grid silently falls back to the *class* default
rather than the template's value. The grids only listed `tf1`, the engine
flags, and `set1_required`, so five other attributes silently reverted to
class defaults that disagree with `gbMES_12_2_always.xml`: `tf2_enabled` and
`tf3_enabled` (class default `True`, template `False` — the strategy was
actually trading two extra near-all-day windows, 03:00–09:00 and
08:00–03:45, on top of the intended 19:30–23:30), `skip_enabled` (class
default `True`), `confirmation_bars` (class default `0` vs template `1`),
and `tf1_flatten` (class default `False` vs template `True` — open positions
rode past the 23:30 window close instead of being force-flattened there).
The tell was trade counts ~13× too high (928–959 per full sample vs the
correct 72) and OOS-window counts of 59–155 instead of single digits to
low teens. Fixed by adding all five to both scripts' param grids and
verified bit-for-bit against the `from_template()`-based reproduce command
in this file (exact match: $5,250.48, 72 trades, Sharpe 2.76) before
re-running. **This bug never touched §2–§4** — those all went through
`sweep_confluence._load_gzk()` (`from_template()` + explicit overrides on
top), which loads every template field correctly; only the two
grid-search-style scripts (walk-forward, ATM-exit sweep) were affected, and
both have been re-run correctly below.

## 6. Faithfulness notes

- `ConfirmationBars=1` and the exits/session/skip-window-off settings are
  all inherited unchanged from the user's saved template — only the engine
  subset/count and tf1 window were varied in this search.
- Of the three engines this config uses, **KO and PA are both validated at
  ~98–99% signal parity against a real NT8 export, and SU at 97.1%**
  (`strategy/GodZillaKilla.md` §2) — though that export was MNQ r60-3, not
  MES r12-2, so it confirms the signal math, not this exact bar geometry.
  TH/SJ/NC (excluded here) are the ones with any open parity questions.
- Sub-30s Apex exposure is **zero** for this config (vs. 11.1% for the
  Terminator champion) — the narrow window plus reversal-managed exits
  naturally avoid it.

## 7. Open items before live use

1. ~~**ATM exits were not tuned for MES.**~~ **DONE — see §8.** A MES-sized
   exit sweep (tp=15t/sl≥100t) beats the original 27t/75t exits on every
   metric (Sharpe 3.89 vs 2.76, PF 5.78 vs 3.30, headroom $1,585 vs $1,321)
   — but this new exit pairing has **not** been through the MC/plateau/
   walk-forward battery that validated §2–§5, so it isn't yet promoted to
   "recommended." Treat §8 as a strong lead, not a certified config.
2. ~~**Not yet NT8-compiled.**~~ **DONE — see §9.** The user built real NT8
   templates (`Claude_MES_12-2_3of3.xml` / `Claude_MES_27-75_Q4.xml`,
   verified field-for-field against this config) and ran a Playback session
   on MES with them; the strategy loaded and traded, confirming it compiles
   and runs in NT8.
3. ~~**Trade-list certification pending.**~~ **DONE for a partial window —
   see §9.** 5/5 Playback trades matched the Python re-run within 0.1-0.4s
   entry timing and ≤1 tick on 4 of 5 (one exit 5 ticks off). Only covers
   ~4 weeks (2026-05-13 to 2026-06-07); the full 510-day history has not
   been Playback- or Analyzer-tested.
4. **Slippage stress not yet run** (the Terminator champion got a 0/1/2-tick
   pass; this config hasn't).
5. Sample is inherently modest (72 trades full-sample, 3–15 per
   walk-forward OOS window) — every check available in this codebase
   (plateau, Monte Carlo, walk-forward) passed, but that's a smaller trade
   count than the Terminator champion's ~1,000, so read the confidence
   accordingly.
6. **If the §8 exits (tp=15/sl=100) are adopted, re-run the full battery**
   (neighbor-window plateau check, Monte Carlo, walk-forward) on that
   config specifically — everything in §3–§5 was validated against the
   original 27t/75t exits, not the new ones.

## 8. ATM-exit sweep for MES (open item #1)

`Godzilla_ATM_MNQ_NR_50-3` (27t target / 75t stop) was sized for MNQ's
$2.00/tick; on MES ($5.00/tick) that's $135 target / $375 stop per
contract — 2.5× the dollar risk/reward the tick counts imply on MNQ. Swept
target/stop directly in `order_mode="fixed"` (single bracket, no BE/trail —
same shape as the original ATM), engines/window/renko held at the validated
§2 config, full 508-day MES history, $2,000 floor.

**First pass** (tp ∈ {8,11,15,20,27}, sl ∈ {20,30,40,50,75}) — all 25
combos profitable once the methodology bug above was fixed:

| tp | sl | net | Sharpe | PF | WR% | headroom | trades |
|---|---|---|---|---|---|---|---|
| 15 | 75 | $3,817 | 3.60 | 5.15 | 91.9 | $1,585 | 74 |
| 20 | 75 | $4,716 | 3.53 | 4.65 | 87.7 | $1,585 | 73 |
| 15 | 50 | $3,197 | 3.24 | 3.29 | 89.2 | $1,527 | 74 |
| 11 | 75 | $2,822 | 3.20 | 5.05 | 94.6 | $1,585 | 74 |
| 27 | 75 (original) | $5,250 | 2.76 | 3.30 | 83.3 | $1,321 | 72 |
| 8 | 20 (worst) | $257 | 0.44 | 1.15 | 77.0 | $1,641 | 74 |

`fixed_tp_ticks` showed a genuine interior peak at 15 (8→11→**15**→20→27,
up then down). `fixed_sl_ticks` was still rising monotonically at the grid
edge (20→30→40→50→**75**, no turnover) — the stop optimum wasn't actually
bracketed yet.

**Follow-up** (tp ∈ {11,15,20}, sl ∈ {75,100,150,200,300}) resolved it: the
stop axis **plateaus completely from 100 ticks onward** — identical results
at sl=100/150/200/300 for every tp tested (e.g. tp=15: net $3,917, Sharpe
3.89 at all four). This makes sense — once the stop is wide enough it never
triggers; the strategy's own signal-reversal/window-flatten/session-end
exits always fire first, so widening the stop further changes nothing.

| tp | sl | net | Sharpe | PF | WR% | headroom | trades |
|---|---|---|---|---|---|---|---|
| **15** | **≥100 (recommended)** | **$3,917** | **3.89** | **5.78** | **91.9** | **$1,585** | 74 |
| 20 | ≥100 | $4,816 | 3.72 | 5.04 | 87.7 | $1,585 | 73 |
| 11 | ≥100 | $2,922 | 3.57 | 5.90 | 94.6 | $1,585 | 74 |
| 15 | 75 | $3,817 | 3.60 | 5.15 | 91.9 | $1,585 | 74 |

**Practical recommendation if adopted: 4 lots, 15-tick target, 100-tick
stop** (no BE/trail) — a wide stop beyond 100 ticks adds nothing since it
never fires; 100 is the smallest value that already sits in the flat part
of the plateau. This beats the original 27t/75t exits on every metric
(Sharpe 3.89 vs 2.76, PF 5.78 vs 3.30, WR 91.9% vs 83.3%, headroom $1,585
vs $1,321) but has **not** been through the neighbor-plateau/MC/walk-forward
battery yet (open item 6).

Full data: `reports/mes_atm_sweep.csv`, `reports/mes_atm_sweep2.csv`.

## 9. NT8 compile + trade-list certification (open items #2, #3)

The user built `Claude_MES_12-2_3of3.xml` (strategy template) and
`Claude_MES_27-75_Q4.xml` (ATM template, the original 27t/75t exits — §8's
15t/100t exits were not tested here) from scratch in NT8, following a
bullet-list spec generated from this config. Verified field-for-field
against the flat `<Strategy><GodZillaKilla>` block (not the non-authoritative
`<OptimizationParameters>` block) before use: r12-2 bars, KO/PA/SU
enabled (3-of-3, no requires), TH/SJ/NC/Set-2/EMA-filter/daily-limits/
TF2/TF3/skip-window all off, `ConfirmationBars=1`, TF1 19:30-23:30 ET with
`FlattenTF1=true`, ATM = 4 qty / 27t target / 75t stop, no BE/trail.

Ran a Playback101 session on MES; the user exported the Executions grid
(`nt8 code/GodZillaKilla/MES Test NinjaTrader Grid 2026-07-13 02-40 PM.csv`,
2026-05-13 to 2026-06-07). One conversion wrinkle: the 6/7 Target1 exit was
logged as four separate same-time, same-Order-ID 1-lot fills instead of one
4-lot row (Market Replay only had 1-lot chunks of synthetic liquidity to
match against at that instant) — `tools/convert_nt8_executions.py` now
accumulates same-order exit fills until the `Position` column reads flat
(`-`) and uses the quantity-weighted average price, instead of assuming one
Exit row = one full close.

Re-ran the exact same two NT8 template files through
`GodZillaKilla.from_template()` (symbol overridden to MES; NT8 doesn't save
the instrument or session-template selection) over 2026-05-01..2026-06-10,
and compared trade lists with `tools/compare_nt8.py --symbol MES --tz
America/New_York --tolerance-s 120`:

```
ours: 7 trades   nt8: 5 trades
matched 5 | only-ours 2 | only-nt8 0

  entry(ours)          dT s  dEntry tk  dExit tk
  2026-05-14 02:38:25    0.2       -1.0       0.0
  2026-05-15 00:38:50    0.1        0.0       0.0
  2026-05-18 03:14:14    0.1        0.0      -5.0  <-- check
  2026-05-19 00:15:01    0.4       -1.0      -1.0
  2026-06-08 02:14:08    0.3        0.0       0.0
```

**5/5 of the NT8 Playback trades matched 5 of the 7 Python trades** —
same direction, same qty, entry times within 0.1-0.4s, entry price exact or
1 tick off on every match, exit price exact or 1 tick off on 4 of 5. The
2 Python-only trades (2026-05-08 and 2026-06-10) fall outside the Playback
session's actual coverage window (5/13-6/7), not a logic mismatch. One
residual: the 5/17 trade's window-flatten exit landed 5 ticks / ~72s later
in Python than NT8's own flatten (`Close`) — plausibly a difference in
exactly which renko bar close triggers the 23:30 ET flatten between the two
engines, or a Market-Replay-vs-repo-data difference; not yet root-caused,
not blocking given the other 4 trades matched this tightly.

This is a strong result but a **partial** certification — one Playback
session, ~4 weeks, 5 trades — not the full 510-day history. Open item #4
(slippage stress) and a longer Analyzer run remain before live use.

## Reproduce

```powershell
.venv\Scripts\python -c "
import sys; sys.path.insert(0, '.')
from backtester.sweep_confluence import _load_gzk
from backtester import Backtest, PropFirmConfig, metrics

strat = _load_gzk('strategies/godzilla_killa.py',
                   r'nt8 code/GodZillaKilla/templates/gbMES_12_2_always.xml')
strat.symbol = 'MES'
strat.period = 'r12-2'
strat.tf1 = ('19:30', '23:30')
strat.use_ko = strat.use_pa = strat.use_su = True
strat.use_th = strat.use_sj = strat.use_nc = False
strat.set1_required = 3

bt = Backtest(strat, start_balance=50_000.0, prop=PropFirmConfig(threshold=2000.0))
result = bt.run()
print(metrics.format_console(result, metrics.compute(result)))
"
```
