# GodTrades (TraderOracle) — methodology port + first validation

**Status: signal engine implemented and unit-tested; first full-fidelity
backtest done on NQ 1000t. The mechanical baseline is nowhere near the
advertised 91% win rate, but it IS net positive over May–Jun 2026 thanks to
band-to-band winners ~2.5x the single-candle stops.** All times US/Eastern.

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

## 5. Open items

1. **Sweep** (`sweep.py`): quality-filter grid (doji/shaved/delta/engulf),
   signal subsets (BG-only, BG+FC), `bb_proximity_ticks`, confirm modes,
   session sub-windows, `exit_mode` band vs fixed, spiderweb suppression,
   `reenter_after_stop`. Then walk-forward the survivor.
2. Longer sample (data available 2024-12 → 2026-07) + Monte Carlo prop-firm
   survival on MNQ-sized risk (1 NQ breaches a $2k trailing floor fast).
3. NT8 parity pass: patch `GodTradesStrategy.cs` with the band exit mode +
   window defaults, Playback run, `tools/compare_nt8.py`.
4. Not implemented v1: "Final Line" cascade, bounce-line confluence,
   attack-angle filter.
