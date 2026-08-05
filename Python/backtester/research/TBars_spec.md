# TBars — Specification and Backtester Port

Status: **ported 2026-08-04**. Parity gate run 2026-08-04 against an MNQ
Speed-120 chart export (§8): geometry and bar timing certified, OHLC parity
79.3% with a characterised ±1-tick residual. Not yet fully certified.
Python: `backtester/data.py::build_tbar_bars`, grammar `tb<N>`, tests in
`tests/test_tbars.py`.

## 0. Provenance and which TBars this is

Three builds of the same vendor bar type were on hand; all three compile to
one class, `NinjaTrader.NinjaScript.BarsTypes.TBars`, and share an identical
`OnDataPoint`. This port models **TBarsNEW.dll** — the build actually
installed in `Documents\NinjaTrader 8\bin\Custom\` and the one in common use.

| | Tbars (2020) | NT8_Tbars (`.cs`) | **TBarsNEW** |
|---|---|---|---|
| `BarsPeriodType` id | 15 — **collides with NT8's built-in `Delta`** | 2015 | **98765** |
| License machinery | `LicenseCheck` field + `doCheckLicense()` | stripped | none |
| `DaysToLoad` | 2 | 2 | **5** |
| `ApplyDefaultBasePeriodValue` | empty | empty | **sets 2** |
| Debug `Print()` per tick | yes | commented out | yes |

The `.cs` is **not** vendor source — it is a decompilation (`int num =
bars.Count - 1`, `high1`/`heikinAshiClose1`, `Print((object) ex.ToString())`)
that someone patched to strip the license check and change the type id. Only
behavioural facts are recorded here; no decompiled source is kept in the repo,
same policy as ninZaRenko and SaberRenko.

`ApplyDefaultBasePeriodValue` matters: the two older builds leave it empty, so
switching a chart to TBars inherits whatever `BaseBarsPeriodValue` the previous
bar type had — arrive from a 1-minute chart and N=1 gives a zero-tick trend
threshold. TBarsNEW pins it to 2. `DaysToLoad = 5` also matters more than it
looks, because this bar type carries anchor state across bars, so a longer
preload converges the visible geometry.

## 1. Parameters

**One** user-facing knob: NT8's **"Speed Settings"** — the property grid renames
`BaseBarsPeriodValue` via `SetPropertyName`, and `Value`/`Value2` are *removed*
from the grid entirely. `State.Configure` derives them on every load:

```
Value  = BaseBarsPeriodValue / 2      <- INTEGER division
Value2 = BaseBarsPeriodValue * 2
```

and `OnDataPoint` turns those into price distances:

| distance | ticks | role |
|---|---|---|
| trend offset | `N // 2` | with-trend continuation |
| reversal | `N * 2` | against-trend |
| open offset | `N` | synthetic open, back from the close |

So **a reversal costs 4x a continuation** — that asymmetry is the "T". All three
are whole tick counts, so every threshold stays on the tick grid.

Grammar here is `tb<N>`, e.g. `tb120`. `parse_barspec` rejects `N < 2`: the
integer division makes `N=1` a zero-tick trend threshold (a bar per uptick).
Odd N is legal but non-obvious — N and N+1 share a trend offset while their
reversal and open offsets differ, so the parameter is not linear in N.

Reference config (the chart this port was checked against): **MGC, Speed 120**
→ trend $6.00, reversal $24.00, open offset $12.00 at tick 0.10; ≈21 bars/day.

## 2. State and the per-tick rule

State is `(bar_open, bar_max, bar_min, bar_dir, run_hi, run_lo)`.

Per incoming trade at price `p`:

- `max_exc = p > bar_max`, `min_exc = p < bar_min` — **strict** (`Compare(...)
  > 0`). A tick landing exactly ON a threshold does not complete the bar.
- **Neither**: extend `run_hi`/`run_lo` with `p`, and rewrite the forming bar's
  close as `ha_close(bar_open, run_hi, run_lo, p)`.
- **Either**: complete the bar and open its successor (§3).

Because `p` is strictly beyond the threshold, the DLL's `Math.Min(p, bar_max)`
clamp always collapses to the threshold itself. **Completing closes are always
exactly on a threshold**, hence always on the tick grid.

## 3. Completion geometry

At the completing tick, with `c1` = the crossed threshold and
`d = +1` if `max_exc` else `-1`:

```
fake_open = c1 - open_off * d

closing bar:  high  = c1 if max_exc else run_hi
              low   = c1 if min_exc else run_lo
              close = ha_close(bar_open, high, low, c1)

next bar:     open   = (fake_open + closing_close) / 2      <- Heikin-Ashi open
              run_hi = c1 if max_exc else fake_open
              run_lo = c1 if min_exc else fake_open
              bar_max = c1 + (trend if d > 0 else rev)
              bar_min = c1 - (rev   if d > 0 else trend)
```

Replacing (rather than `max`-ing) the high on an up breakout is lossless: every
updating tick was `<= bar_max` by definition, and `c1 == bar_max`.

Each new bar therefore *starts* spanning `open_off` ticks — overlapping-bar
geometry, same family as ninZaRenko's `B - T` synthetic open.

### 3.1 The output is Heikin-Ashi, and that is faithful

`close = (open + high + low + close) / 4`; `open = (fake_open + prev_close)/2`.
High/low are the **real** running extremes (unioned with the synthetic open) —
the DLL defines `GetHeikinAshiHigh`/`GetHeikinAshiLow` and **never calls them**.

Breakout detection runs on **raw** tick prices; only the emitted OHLC is
transformed. So a strategy reading `bars.close` sees a synthetic value — but it
sees exactly what the same strategy would see in NT8, which is the point. Fills
are unaffected: the engine resolves orders on real ticks via the `[i0, i1)`
spans, so the HA smoothing never reaches the fill model.

Measured invariant check: 20k-tick random walk plus 640k ticks of standalone
simulation, **zero** bars with close/open outside `[low, high]` — the 4-way
average is self-bounding. The one exception is §5.2.

### 3.2 Tick rounding — inside the loop, not at the end

NT8 stores every bar price on the instrument's tick grid. The 2026-08-04
chart export (§8) is conclusive: all 2232 exported bars have OHLC on the 0.25
grid, whereas an unrounded 4-way average lands on 1/16ths. Rounding is
**half-to-even** (.NET `Math.Round`'s default `MidpointRounding.ToEven`),
established end-to-end — half-up scored 71.6% OHLC parity, half-down 72.7%,
to-even **79.3%**.

The rounding sits *inside* the state loop, not on the output: the next bar's
HA open reads `bars.GetClose(...)` and each tick's HA close reads
`bars.GetOpen(...)`, so a rounded value feeds the next computation and
rounding only at the end would drift. Only the two HA averages need it —
high/low come from real trades, the clamped threshold, or the synthetic open,
all already on the grid.

This is worth ~79 points of parity on its own: without it, identical-OHLC was
**0.1%**.

## 4. Volume and spans — a deliberate divergence

NT8 calls `UpdateBar(..., volume)` for the completing tick **and**
`AddBar(..., volume)` for the bar it opens, so that tick's volume lands in
**both** bars and total bar volume exceeds traded volume.

**CONFIRMED by the 2026-08-04 volume gate** (§8.1), not inferred: 97.8% of
matched bars satisfy `nt8_vol == our_vol + volume[breakout_tick]` exactly, and
NT8's bar volumes sum to **2,292 contracts MORE** than were actually traded in
the window. The platform really does over-count.

This repo requires bar spans to be contiguous and non-overlapping — the engine
resolves `[i0, i1)` per bar, and a double-covered tick could fill one order
twice. So the breakout tick goes to the **new** bar only (`i1 = k`, next
`i0 = k`), matching `build_renko_bars` and `build_saber_bars`. Consequence: a
completing bar's volume is short by its breakout tick versus NT8. Engine
correctness wins; the NT8 behaviour is arguably wrong anyway.

Volume accrued by a bar that was still forming at the end of a day file is not
recoverable from the next day's arrays and is dropped — same accepted
approximation as SaberRenko.

## 5. Boundaries

### 5.1 Reset is gap-driven, not calendar-driven

The DLL re-seeds when `bars.IsResetOnNewTradingDay` fires on a new session.
That property is **not** a bar-type property — `Bars.IsResetOnNewTradingDay`
forwards to the data series, and NT8's own doc comment names it: *"Indicates if
the bars series is using the Break EOD data series property."* So the reset
branch only runs with **Break at EOD ON** — which is NT8's default
(user-confirmed 2026-08-04, and the setting on the chart used for the §8
parity run). With it off, stock TBars never re-seeds at all and runs one
continuous grid.

The port resets only on a genuine trade gap `> RENKO_RESET_GAP_NS` (30 min),
which for this repo's data is the real CME halt — identical to renko/saber, and
the same day-boundary hazard applies: day files are ET calendar days but an
overnight session runs through midnight ET with no real gap, so
`Catalog.load_bars_sequence` threads `end_state` across file boundaries and
resets only on a real gap.

At a gap the DLL does **not** close the forming bar; it just `AddBar`s a fresh
one, so the forming bar is completed by abandonment, keeping the HA close it
last held. The port emits it that way.

Reset frequency is instrument-dependent — measured over 10 recent days:
MGC 1.8 gaps/day, MNQ 0.7/day. Each reset emits a one-tick doji stub (§5.3), so
on MGC roughly 9% of bars are stubs. That is faithful, not a defect.

### 5.2 The inverted re-seed — the one place this port refuses parity

The DLL's re-seed carries `barDirection` across the reset:

```
bar_max = open + trend * dir
bar_min = open - trend * dir
```

With `dir = -1` this is **inverted** (`bar_max < bar_min`), so a tick between
them satisfies **both** tests; `maxExceeded` is checked first and wins, and the
emitted bar takes `high = low = c1` with `c1` on the wrong side of its own open.
Hand-computed at N=4, tick 0.25, prior direction down, re-seed at 98.00:

```
bar_max = 97.50   bar_min = 98.50        <- inverted
next tick 98.00 -> O=98.00 H=97.50 L=97.50 C=97.50
                   open sits 2 ticks ABOVE the bar's own high
```

(Pre-rounding the close came out 97.625, i.e. `close > high` as well; tick
rounding, §3.2, happens to pull it back onto the high. The bar is still
invalid via `open > high` — the rounding masks one symptom, not the cause.)

That bar violates OHLC ordering and every indicator in `indicators.py` (ATR,
Highest/Lowest, ...) would silently consume it. `build_tbar_bars` therefore
defaults `reset_carries_dir=False`, seeding `dir = 0` exactly as a fresh start
does — thresholds collapse symmetrically onto the open and only a valid doji
can result. `reset_carries_dir=True` reproduces the DLL verbatim for parity
work. This costs no fidelity in the common case, because with Break at EOD OFF
this whole branch is already a modelling choice rather than a reproduction.

To spot it on a live chart: find a session that closed *downward* into the
17:00 ET break and look at the first bar after the 18:00 reopen.

### 5.3 The seed stub

A fresh start reproduces the DLL's `bars.Count == 0` branch: `bar_dir` is 0, so
`bar_max == bar_min == open` and the next differing tick immediately completes a
zero-range bar. One doji stub per fresh start and per reset, by construction, in
NT8 too.

## 6. Known-unverified

- **The per-tick `Print()`** in TBarsNEW (`"else"` / `"maxExceeded ||
  minExceeded"`) is real in the IL, but whether it reaches the NT8 Output
  window could not be established statically — `NinjaTrader.Core.dll` is
  protected and its method bodies decompile empty. Irrelevant to this port.
- `OnDataPoint` wraps everything in `catch (Exception) { Print(...) }`, so a
  failure mid-build leaves corrupt bar state and continues silently. Not
  reproduced.

## 7. NT8 template mapping

`BarsPeriodTypeSerialize = 98765` → `tb{BaseBarsPeriodValue}`
(`backtester/nt8config.py`). `Value`/`Value2` are read back only to assert they
equal `N//2` and `N*2`; a template disagreeing was written by some other bar
type. Ids 2015 and 15 (the older builds) are **not** mapped — 15 collides with
NT8's built-in `Delta`.

Reference workspace block (MGC chart, `workspaces/Khahn.xml`):

```xml
<BarsPeriodTypeSerialize>98765</BarsPeriodTypeSerialize>
<BaseBarsPeriodValue>120</BaseBarsPeriodValue>
<Value>60</Value><Value2>240</Value2>
```

`BaseBarsPeriodType` serialises as `Minute` and is vestigial — `BuiltFrom` is
`Tick`, and Configure removes `BaseBarsPeriodType` from the grid.

## 8. Parity gate — run 2026-08-04, MNQ Speed 120

Export: `gbBarExporter` on an **MNQ 09-26 / TBars / Speed 120** chart,
2026-07-19 18:00 → 2026-07-31, **Break at EOD ENABLED** (user-confirmed
default), 2232 bars. Command:

```
python tools\compare_bars.py "...\export\bars_MNQ_TBars.csv" \
    --symbol MNQ --period tb120 --tolerance-s 10
```

| metric | result |
|---|---|
| bars matched by close time | **2212 / 2232 (99.1%)** |
| identical high/low | **2183 / 2212 (98.7%)** |
| identical close | **2024 / 2212 (91.5%)** |
| identical full OHLC | **1755 / 2212 (79.3%)** |

**What this certifies.** The geometry is right: thresholds, the strict
breakout test, the exact-threshold clamp, the synthetic open offset, the 4x
trend/reversal asymmetry, and bar *timing* all reproduce. Bar 1 was verified
by hand against the export to the cent — seed doji 28747.5, then
`open = (28717.5 + 28747.5)/2 = 28732.5`, `close = 114975/4 = 28743.75`,
both exact. Timing at 99.1% means signal timing is faithful, which is what
actually drives strategy behaviour; the HA values are a cosmetic overlay and
fills never touch them.

**What it does not.** Two residuals remain:

1. **±1 tick on the HA averages, ~9% of bars** (86 bars at −1 tick, 78 at
   +1 on close; 140/153 on open). Every one is exactly one tick, and the
   ±directions are near-symmetric. Both HA formulas were verified directly
   against NT8's *own* stored values — residuals there are only 0, ±0.25,
   ±0.5 ticks, i.e. exactly what tick-rounding predicts — so the formulas
   are right and this is **error propagation**: one divergent bar shifts the
   next bar's open, which shifts its close, until it self-heals. Chasing the
   tie-break mode further is the wrong lever (all three modes were tested
   end-to-end; see §3.2).
2. **29 bars (1.3%) with genuinely different high/low**, carrying the large
   deltas (−94 to +71 ticks) — the session-boundary re-seeds.

   **CORRECTED 2026-08-05.** This section originally concluded the reset
   *point* (trading-hours template vs. this port's >30 min gap) was the
   driver, because `reset_carries_dir=True` moved the total only 78.8% →
   79.5%. That inference was wrong. The gbTBars parity run (§8.2) fixed the
   direction carry on the NT8 side and geometry mismatches fell **29 → 2**.
   The earlier test misled because it reproduced the inversion at the
   *port's* reset points, which do not coincide with NT8's — reproducing a
   bug in the wrong places cannot improve agreement. The direction carry was
   the driver all along; the reset-point difference is almost entirely
   benign once neither side emits a malformed bar.

**Judgement.** Good enough to use, not yet fully certified. For comparison
ninZaRenko landed at 96-100% and SaberRenko at 96-97%, but both emit raw
prices; TBars' output is a 4-way average of four rounded quantities, so a
single tick of divergence anywhere is structurally far more visible. If
tighter parity is wanted, attack residual 2 first: match NT8's session
template boundary instead of the gap heuristic. That is a bounded change and
would likely also remove part of residual 1, since resets are where
propagation starts.

### 8.1 Volume gate — 2026-08-04

Separate run (`compare_bars.py` only reads OHLC), 2224/2232 bars matched:

| outcome | bars | before carry fix | **after** |
|---|---|---|---|
| `nt8_vol == our_vol + volume[breakout tick]` | | 2176 (97.8%) | **2186 (98.3%)** |
| neither candidate | | 39 (1.8%) | **29 (1.3%)** |
| `nt8_vol == our_vol` (breakout tick had 0 volume) | | 9 (0.4%) | 9 (0.4%) |

| | contracts |
|---|---|
| actually traded in window | 28,872,342 |
| sum of NT8 bar volumes | 28,874,634 (**+2,292**) |
| sum of our bar volumes, before carry fix | 28,688,359 (−183,983, −0.64%) |
| sum of our bar volumes, **after** | **28,847,577 (−24,765, −0.086%)** |

The first run exposed a hole on OUR side: a bar still forming at the end of a
day file carried its geometry into the next day but not its accumulated
volume. Fixed by growing the carry tuple with
`(volume, buy_volume, sell_volume)` — SaberRenko had the identical hole and
was fixed in the same change (`BARS_VERSION` 7 → 8, since an old cache's
`end_state` no longer unpacks).

That recovered 159,218 of the 183,983 missing contracts. The residual −24,765
is correct behaviour, not a bug: it is the bar still forming when the data
ends, which never completes and so is never emitted.

The remaining 29 "neither" bars are now exactly the session-boundary
population as OHLC residual 2 — same root cause, one fix away.

### 8.2 Both charts re-exported after repairing the 07-24 tick data, so vendor and
gbTBars ran on **identical** data over an identical window (MNQ 09-26, Speed
120, 2026-07-19 18:00 → 07-31 16:59, Break at EOD ON), each compared against
the Python port.

| metric | vendor TBars | **gbTBars** |
|---|---|---|
| bars emitted | 2232 | **2223** (port: 2222) |
| matched by close time | 99.2% | **99.9%** |
| **identical high/low** | 98.9% — 24 bad | **99.8% — 4 bad** |
| identical close | 91.1% | **92.5%** |
| identical full OHLC | 79.5% | **80.7%** |

Chart-to-chart: **2184 bars byte-identical**, 39 differ, 9 exist only in the
vendor build.

**Fix 2 confirmed.** The vendor's 24 bad bars cluster at **18:00:00** — the
session reopen. gbTBars has 4, none of them at a reopen. The 9 vendor-only bars
all fall within ~3 minutes of 18:00 too. The bar stream diverges for a handful
of bars after each reopen and then re-syncs.

**Correction to an earlier prediction.** I expected the vendor chart to show a
visibly malformed bar (open above its own high). **It does not** — NT8's
`UpdateBar` clamps high/low so they cannot cross the open. Verified side by side
at 07-20 18:00:00: the port reproducing the DLL emits `O=28783.25 H=28768.25
L=28768.25`, NT8 stores `O=H=L=28783.25`. So the arithmetic really is wrong, but
NT8 masks the worst symptom and turns it into a doji at the open. The observable
damage is **wrong** bars at session reopens, not **invalid** ones.

That also means the Python port, which has no such clamp, would emit genuinely
malformed OHLC under `reset_carries_dir=True` — 10 such bars in this window.
Another reason the `False` default is right.

**The remaining gap is not geometry.** ±1 tick on the two Heikin-Ashi averages,
essentially unchanged between the builds (open 13.2% vendor / 13.1% gbTBars;
close 7.5% / 7.3%) because the fix does not touch the HA math. That propagation
is the only thing standing between 80.7% and ~100%.

## 9. Remaining after the gate

Done for MNQ (§9). Still open:

1. **Session-boundary re-seed** — match NT8's trading-hours template boundary
   instead of the >30 min gap heuristic. This is residual 2 in §9 and the
   highest-value remaining fix.
2. ~~**Carried-bar volume loss**~~ — DONE 2026-08-04, saber included (§8.1).
3. **MGC parity** — the instrument actually traded on this bar type. Needs
   contract **MGC 08-26** (NOT the 12-26 on the Khahn workspace) and a window
   ending **2026-07-28**: the recorded MGC contract rolls out after that
   (154k trades on 07-28, 4.2k on 07-30, 155 on 07-31).
