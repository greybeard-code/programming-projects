# GodTrades (TraderOracle) — methodology port + validation

**Status: signal engine implemented, unit-tested, swept, and walk-forward
tested. Bottom line: the mechanical port is nowhere near the advertised 91%
win rate (~32-40% depending on filters), AND the May–Jun 2026 window that
first looked profitable turns out to be a favorable pocket, not the
system's true character — over the full 417-day sample (2024-12 to
2026-07) the tuned config is roughly break-even, and walk-forward shows
in-sample optimization itself can't find a robustly profitable combo in
3 of 5 windows. See §6 before trusting any single-window number in this
file, including the ones in §4.** All times US/Eastern.

## 1. Sources

| Piece | Where |
|---|---|
| Rule set | "The God Trades" Canva deck (14 slides, TraderOracle) |
| Discretionary filters | "god trade masterclass" video (28 min, transcript) |
| NT8 signal indicator (reference semantics) | `NinjaScript/TOGodMode/NT8 code/GodTrades21.cs` |
| NT8 wrapper strategy (exits deviate — see §4) | `NinjaScript/TOGodMode/NT8 code/GodTradesStrategy.cs` |
| Python port | `strategies/god_trades.py` (`GapSignalEngine` + `GodTrades`) |
| Unit tests | `tests/test_god_trades.py` (16 tests, synthetic bars) |

## 2. The methodology (as taught)

Premise: algos rush price and skip prices, leaving *candle gaps* (body gap
between two consecutive same-direction candles). Price returns to examine
("fill") these gaps later, then reacts. Chart: **NQ 1000-tick** (ES =
2000-tick), signals only **10:15–15:00 ET**. A gap line only counts if it
survived **≥ 3 candles** before the fill; older line = harder reaction.

Setups (all against a 20/2 Bollinger):

* **BG — Bollinger Gap**: two same-color candles with a body gap between
  them forming while hugging the band the gap points away from (gap-up at
  the lower band). "Price is in a hurry" — momentum entry on the second
  candle's close.
* **FC — fill continuation** (deck's "Fill & Reverse" + "Fill & Continue",
  claimed 84–91%): old gap line touched from the correct side; a candle in
  the gap's direction closes beyond the zone within 2 bars → enter.
* **OBR — outside-bar reversal**: opposite-direction body engulf at the
  band edge.

Exits (the defining feature): **stop = back of the single signal candle**,
**target = opposite Bollinger band touch** (dynamic), **never move to
break-even** ("break-even is suicidal … you'll increase profits 20% if you
stop going break-even"). Expect 3–4 full stops/day; winners must out-earn
~3 candle-stops. Extras: re-enter after a stop-hunt if the original entry
price prints again; stand aside in "spiderwebs" (≥5 lines within ~100
ticks); the "Final Line" (last of a gap cascade filling) → violent reverse.

Video quality filters (implemented as toggles, default OFF): shaved close,
doji rejection, engulfing signal candle, delta agreement, steep "attack
angle" (not implemented v1).

## 3. Port notes

* `GapSignalEngine` mirrors GodTrades21.cs semantics: same gap definition
  (`MinimumGapSizeTicks=1`), 3-bar validity, early touch retires the line
  silently, FC approach-side + wick-extreme Bollinger-midpoint filters,
  same OBR band tolerance (4 ticks, wick may pierce), NT8 signal precedence
  (BG > FC > OBR), conflicting long+short bars skipped.
* Session is the full Globex day (18:00–16:55) so lines accumulate around
  the clock as on the streamer's chart; **entries** are window-gated
  (10:15–15:00) and open positions flatten at 15:00.
* Exits are OCO absolute-price orders placed on the entry fill; the target
  limit is re-priced to the opposite band every bar (band touch = fill).
* `Bollinger` (SMA ± mult·population σ, NT8-style) added to
  `backtester/indicators.py`.
* NEW vs the NT8 strategy: the NT8 wrapper trades fixed 40/30 ticks and
  ignores the indicator's suggested candle-stop/band-target — the Python
  port implements the taught exit model (`exit_mode="band"`), with
  `exit_mode="fixed"` kept for comparison.

## 4. First results — NQ 1000t, 1 contract, 2026-05-01 → 2026-06-30 (43 days)

Defaults (= GodTrades21 defaults, quality filters off, band exits):

```
Net P&L   $22,076   (commission $1,243)   profit factor 1.20
Trades    401   win rate 31.9%   avg win $1,056   avg loss $-414
Sharpe    3.69   max DD $-8,855 (-16%)   days +26/-17
PROP      breached $2k trailing threshold (1 NQ is far too big for it)
```

Per signal type:

| src | n | win% | net | avg/trade |
|---|---|---|---|---|
| BG  | 27  | 40.7% | $10,962 | **$406** |
| FC  | 206 | 31.6% | $8,890  | $43 |
| OBR | 168 | 31.0% | $2,223  | $13 |

Read: **the 91% claim does not survive mechanical execution** — win rate is
~32% because the single-candle stop is genuinely tiny on a 1000t chart.
The system still nets positive here because band-to-band winners average
2.5x the stops (exactly the shape the deck promises, minus the win rate).
BG is the only setup with real per-trade expectancy; OBR is noise that adds
168 trades of commission/risk for ~nothing. ~9 trades/day matches the
video's "5–10 per ticker" claim, so signal frequency is faithful.

Trade duration median 222 s; 50 trades < 30 s (all quick stop-outs,
−$17.9k of the gross losses) — these also violate the Apex min-hold rule,
so a prop-firm config wants `min_hold_s` or entry throttling looked at.

Quick config comparison, same period (video quality filters earn their
keep; `doji0.3` = body ≥ 30% of range, `shaved0.33` = entry-side wick ≤ 33%
of range, `delta` = signal-bar order-flow delta must agree):

| config | trades | win% | net | PF |
|---|---|---|---|---|
| baseline (all signals, filters off) | 401 | 31.9% | $22,076 | 1.20 |
| + doji0.3 + shaved0.33 | 310 | 36.1% | $22,539 | 1.25 |
| + delta agreement | 284 | 38.0% | $25,753 | 1.31 |
| same, OBR off | 160 | 39.4% | $18,600 | 1.39 |
| **BG only, filters off** | 35 | 40.0% | $11,557 | **2.79** |

Spiderweb suppression changed nothing on this sample (never triggered with
the 5-line/100-tick defaults).

## 5. Sweep results (`sweep.py`, same May–Jun window)

Two grids, ranked by profit factor, `--min-trades 20` (see
`reports/sweep_GodTrades.csv` — each sweep overwrites the previous, values
below are transcribed at time of run):

**Quality filters + OBR** (`max_doji_body_frac`, `shaved_close_frac`,
`require_delta_sign`, `enable_obr`; 36 combos):

* Best PF: `doji=0.3, shaved=0.25, delta=1, obr=0` → PF 1.45, net $19,858,
  145 trades, win% 40.7.
* Best net $: `doji=0, shaved=0, delta=1, obr=0` → PF 1.45, net **$26,975**,
  214 trades, win% 35.5 — same PF, more trades, more money. Sample size
  favors this one over the "best PF" row.
* Sensitivity plateau (robust, not a spike): `doji` 0/0.2/0.3 → 1.40/1.38/
  1.45; `shaved` 0/0.25/0.33 → 1.36/**1.45**/1.39; `delta_sign` 0/1 →
  1.39/**1.45**; `enable_obr` **0**/1 → 1.45/1.31. `require_delta_sign=1`
  and `enable_obr=0` are the two clean, consistent wins. Doji/shaved help a
  little but the effect is mild — don't over-read either one.
* OBR is confirmed noise: turning it off consistently raises PF even though
  it removes ~160 trades of volume, matching §4's per-signal breakdown.

**Exit mechanics** (`exit_mode`, `bb_proximity_ticks`, `confirm_mode`; 24
combos, quality filters fixed at the winning row above):

* **`exit_mode=fixed` is a flat loser at every other-parameter combination
  tried** — net P&L negative in all 12 fixed-mode rows (PF 0.88–0.96, same
  ~42% win rate as band mode). `exit_mode=band` is unambiguously required;
  this isn't a stylistic choice, fixed ticks kills the edge outright because
  it caps the band-touch winners that pay for the single-candle stops.
* Best band-mode combo: `bb_proximity_ticks=12, confirm_mode=zone` → PF
  1.49, net $21,352, Sharpe 5.49, 147 trades. Plateau confirmed: proximity
  4/8/12/16 → 1.39/1.45/**1.49**/1.45 (12 is a real local max, not a spike);
  `confirm_mode` line/zone tie at 1.49, touch trails at 1.43.
* `spiderweb_suppress` never fires on this sample (identical to off).
  `reenter_after_stop` adds trades (219 vs 147) and a touch more net $ but
  drags PF/Sharpe down (1.35/4.73 vs 1.49/5.49) — left off by default.

**Adopted "tuned" config for §6 testing:** `require_delta_sign=True,
max_doji_body_frac=0.3, shaved_close_frac=0.25, enable_obr=False,
bb_proximity_ticks=12, confirm_mode="zone"`, exit_mode="band" (already
default).

## 6. Longer sample + walk-forward — the honest picture

Everything above was tuned and validated on **one 43-day window
(2026-05-01 → 2026-06-30)**. Data is available 2024-12-16 → 2026-07-03
(417 trading days) on NQ, so that window is checked against the full
history — this is exactly the check the plan called for, and it changes
the conclusion.

**Baseline (no filters) over the full 417 days:**

```
Net P&L   $-1,331   (gross $9,423, commission $10,754)   profit factor 1.00
Trades    3,469   win rate 30.8%   avg win $730   avg loss $-326
Sharpe    -0.03   max DD $-39,355 (-68%)
Monte Carlo P(profit) 47%
```

Commission ($10,754) very nearly equals gross profit ($9,423) — 1-contract
NQ churns through ~8.3 signals/day and the edge, such as it is, barely
survives execution cost.

**Tuned config (the §5 winner) over the full 417 days:**

```
Net P&L   $3,801   (gross $7,270, commission $3,469)   profit factor 1.01
Trades    1,119   win rate 34.3%   avg win $758   avg loss $-391
Sharpe    0.14   Sortino 0.20   Calmar 0.09   max DD $-26,854 (-49%)
```

Compare to the May–Jun window alone: PF 1.49, Sharpe 5.49. **Over the full
history the same config is essentially break-even (PF 1.01, Sharpe 0.14).**
May–Jun 2026 was a favorable pocket for this signal set, not its normal
behavior — this is precisely the data-snooping trap `sweep.py`'s own
docstring warns about, now confirmed empirically rather than just flagged
as a risk.

**Walk-forward** (`walkforward.py`, 5 windows, IS:OOS ratio 4:1, grid =
`bb_proximity_ticks` × `shaved_close_frac`, other params fixed at the §5
winner, metric = profit factor):

| window | IS period | OOS period | IS PF | OOS PF | OOS net |
|---|---|---|---|---|---|
| 1 | 2024-12-16→2025-08-11 | 2025-08-12→2025-10-17 | 0.93 | 0.78 | $-5,140 |
| 2 | 2025-02-11→2025-10-17 | 2025-10-19→2025-12-19 | 0.92 | 0.85 | $-4,957 |
| 3 | 2025-04-14→2025-12-19 | 2025-12-21→2026-02-19 | 0.82 | 1.14 | $3,631 |
| 4 | 2025-06-14→2026-02-19 | 2026-02-20→2026-04-21 | 0.92 | 0.97 | $-791 |
| 5 | 2025-08-19→2026-04-21 | 2026-04-22→2026-06-24 | 0.95 | **1.39** | **$17,994** |

Stitched OOS: net **$10,737** over 223 days, Sharpe 0.74. Walk-forward
efficiency 1.13 (formally "OK," OOS not worse than IS) — but this is a
red herring here: **in-sample profit factor is below 1.0 in 4 of 5
windows**, meaning the grid search couldn't find a genuinely profitable
combo on most in-sample slices to begin with. A WFE near/above 1 on an
unprofitable IS just means OOS wasn't dramatically worse than an already-
bad baseline; it is not evidence of a validated edge. Only window 5 — which
happens to contain the original May–Jun 2026 test window — shows real
out-of-sample strength (PF 1.39, +$17,994), and it alone accounts for all
of the stitched OOS profit (sum of the other four OOS windows is
$-7,257).

**Verdict:** the mechanical God Trades port, best config found, is *at
best* marginal over 1.5+ years of NQ 1000-tick data — flat-to-slightly-
positive with severe drawdowns (max DD 49-68% of a $50k account across
different config/period combos), and its one strong period is the same
window used to tune it. This does not mean the trainer's discretionary
version is worthless — his read of "shaved close," "attack angle," and
"no snapback" (not implemented here) may be doing real work that this port
can't capture — but the codified rules alone, run mechanically, are not a
validated edge. Do not size this for live/sim trading without either (a)
a materially different result on fresh data, or (b) explicit acceptance
that this is a research artifact, not a strategy.

## 7. Prop-firm / sizing note

Every full-history and May–Jun run above breaches a $2,000 Apex-style
trailing floor at 1 NQ contract (point value $20/pt) — the instrument is
simply too large for that risk budget at this stop size. The deck itself
says "use NQ, not MNQ" for signal quality but implies cross-trading the
micro for execution size; that requires wiring a second, differently-sized
instrument to the same NQ-derived signals (not done here — the backtester
doesn't currently support signal-on-one-instrument / fill-on-another). Not
pursued further given §6's verdict makes contract-sizing a secondary
concern until the core edge question is resolved.

## 8. Open items

1. ~~Sweep~~ — done, §5.
2. ~~Longer sample + walk-forward~~ — done, §6. Verdict: marginal/fragile.
3. NT8 parity pass: patch `GodTradesStrategy.cs` with the band exit mode +
   window defaults, Playback run, `tools/compare_nt8.py`. **Given §6, worth
   doing only if the goal is faithful-methodology tooling for further
   manual/discretionary use — not as a "deploy this" step.**
4. Not implemented: "Final Line" cascade, bounce-line confluence,
   attack-angle / no-snapback candle-formation filter, cross-instrument
   (signal on NQ, fill on MNQ) sizing.
