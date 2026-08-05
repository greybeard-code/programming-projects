# gbTBars

A GreyBeard build of the **TBars** bar type for NinjaTrader 8 — asymmetric
trend/reversal bars with a Heikin-Ashi presentation layer.

**Status:** v1.0.0, compiles clean, **not yet validated on a chart.** The
algorithm is certified against a real NT8 export (see [Evidence](#evidence)),
but that gate was run against the *vendor* build. gbTBars' own chart-parity run
is the next step.

Registered on `BarsPeriodType` **91001**, so it **coexists** with the vendor's
TBars (98765) rather than replacing it. Existing charts keep resolving the
vendor build; switch a chart to `gbTBars` to use this one.

---

## Install

```powershell
copy gbTBars.cs "$env:USERPROFILE\Documents\NinjaTrader 8\bin\Custom\BarsTypes\"
```

Then **F5** in the NinjaScript Editor. No DLL to install and nothing to
uninstall first — it's ordinary NinjaScript source that NT8 compiles into your
custom assembly.

On a chart: Data Series → Bar type **gbTBars** → **Speed Settings**.

---

## How TBars works

One user-facing parameter, **Speed Settings** (`N`). Everything else is derived
in `State.Configure`, integer division included:

| distance | ticks | role |
|---|---|---|
| trend offset | `N / 2` | with-trend continuation |
| reversal | `N * 2` | against-trend |
| open offset | `N` | synthetic open, back from the close |

**A reversal costs 4× a continuation.** That asymmetry is the whole point — the
"T". Price continuing in the established direction prints bars cheaply; turning
around is expensive, so the bar type is structurally reluctant to flip.

At `N = 120` on MNQ (tick 0.25) that's trend $15 / reversal $60 / open offset
$30 — about 163 bars/day. On MGC (tick 0.10): $6 / $24 / $12, about 21 bars/day.

### The per-tick rule

State is `(barOpen, upThreshold, downThreshold, barDir, runHigh, runLow)`.

- **Inside the band** — extend the bar's real high/low, and rewrite its close.
- **Outside** — the bar completes and its successor opens.

The breakout test is **strict** (`Compare(...) > 0`): a tick landing *exactly*
on a threshold does not complete the bar. And because the triggering tick is
strictly beyond the threshold, the clamp `Math.Min(close, upThreshold)` always
collapses onto the threshold itself — so **completing closes are always exactly
on the tick grid**.

### Completion geometry

With `c` = the crossed threshold and `d` = `+1` up / `-1` down:

```
syntheticOpen = c - openOffset * d

closing bar:  high  = c if broke up   else runHigh
              low   = c if broke down else runLow
              close = HA(barOpen, high, low, c)

next bar:     open  = (syntheticOpen + closingClose) / 2
              high  = c if broke up   else syntheticOpen
              low   = c if broke down else syntheticOpen
              upThreshold   = c + (d > 0 ? trend : reversal)
              downThreshold = c - (d > 0 ? reversal : trend)
```

Each new bar *starts* spanning `openOffset` ticks — that's what produces the
overlapping look, the same family as ninZaRenko's `B − T` synthetic open.

### The output is Heikin-Ashi — deliberately

`close = (open + high + low + close) / 4`, recomputed every tick;
`open = (syntheticOpen + priorClose) / 2`. **High and low are the real traded
extremes**, not HA-derived — the vendor DLL defines HA high/low helpers and
never calls them.

Breakout detection always runs on **raw traded prices**. Only the emitted OHLC
is transformed. So a strategy reading `Close[0]` sees a synthetic value — but it
sees exactly what the chart shows, which is the point.

### Tick rounding matters more than it looks

NT8 stores every bar price on the instrument's tick grid, rounded
**half-to-even** (.NET `Math.Round`'s default), and that rounding sits *inside*
the state loop — the next bar's HA open reads the stored close. Rounding only at
the end drifts. This is not cosmetic: reproducing the bars without it scored
**0.1%** OHLC parity; with it, **79.3%**.

### Session reset

`bars.IsResetOnNewTradingDay` is the Data Series **"Break at EOD"** toggle
(NT8's own doc comment names it), and it **defaults ON** — so the re-seed path
is live on a stock chart. With it off, the grid runs continuously.

A re-seed collapses both thresholds onto the open, so it emits a one-tick doji
that the next differing tick completes. That's by design, and it also describes
the chart's very first bar.

---

## The three vendor builds

Three TBars packages were on hand. **All three compile to one class** —
`NinjaTrader.NinjaScript.BarsTypes.TBars` — with an identical `OnDataPoint`.
They are three generations of one product, not three designs.

| | Tbars (2020) | NT8_Tbars (`.cs`) | **TBarsNEW** (2025) |
|---|---|---|---|
| `BarsPeriodType` id | **15** | 2015 | **98765** |
| License machinery | `LicenseCheck` field + `doCheckLicense()` | stripped | none |
| `DaysToLoad` | 2 | 2 | **5** |
| `ApplyDefaultBasePeriodValue` | empty | empty | **sets 2** |
| Debug `Print()` per tick | yes | commented out | **yes** |
| Provenance | vendor DLL | **decompilation** | vendor DLL |

**TBarsNEW is the one in common use, and it is the best of the three** — on the
merits, not just by popularity.

- **Id 15 collides with NT8's built-in `Delta`** bar type (`BarsPeriodType` runs
  `Tick=0 … Volumetric=14, Delta=15, PriceOnVolume=16`). That is almost
  certainly why the 2020 build was reissued.
- **`ApplyDefaultBasePeriodValue` empty** means switching a chart to TBars
  inherits whatever `BaseBarsPeriodValue` the previous bar type had. Arrive from
  a 1-minute chart and `N=1` gives a **zero-tick trend threshold** — a bar per
  uptick. TBarsNEW pins it to 2.
- **`DaysToLoad = 5` vs 2** matters more than it looks: this bar type carries
  anchor state across bars, so a longer preload converges the visible geometry.
- The 2020 build **declares a field of type `LicenseCheck`**, a type the CLR must
  resolve when the class loads. If that indicator isn't installed, that's a
  latent load failure.

### `NT8_Tbars` is a decompilation, not vendor source

Worth stating plainly, because it circulates as if it were source. Tells:
`int num = bars.Count - 1`, locals named `high1` / `heikinAshiClose1`, and
conclusively `Print((object) ex.ToString())` — nobody hand-writes an explicit
`(object)` cast on a `string`. Someone decompiled the 2020 binary, stripped the
license check, changed the type id to 2015, and commented out the debug prints.

Running it means running unvetted, patched code of unknown origin for **zero**
functional gain — the math is identical — while being *behind* TBarsNEW on
`ApplyDefaultBasePeriodValue`.

---

## Issues found

| # | issue | severity |
|---|---|---|
| 1 | `Print("else")` / `Print("maxExceeded \|\| minExceeded")` fire on **every tick** | high |
| 2 | Session re-seed carries `barDirection` → **inverted thresholds** | high |
| 3 | `catch (Exception)` swallows failures, then keeps building bars from corrupt state | medium |
| 4 | `ApplyDefaultBasePeriodValue` empty (2020 + `.cs` builds only) | medium |
| 5 | Id 15 collides with built-in `Delta` (2020 build only) | medium |
| 6 | `GetPercentComplete` always returns 0 | cosmetic |
| 7 | Dead members: `hasInitialized`, `licenseErrorReported`, both HA high/low helpers | cosmetic |
| 8 | `State == 2 && State == 2` — duplicated condition | cosmetic |

### Issue 2, in detail

The re-seed seeds `upThreshold = open + trend*dir`, `downThreshold = open −
trend*dir`, and **`barDirection` is a field that survives the reset**. Carrying
a `-1` inverts them:

```
re-seed after a down session, N=4, tick 0.25, open 98.00:
    upThreshold = 97.50   downThreshold = 98.50      <- inverted

next tick at 98.00 satisfies BOTH tests; maxExceeded is
evaluated first and wins:
    O=98.00  H=97.50  L=97.50  C=97.50
    -> the bar's own open sits 2 ticks ABOVE its high
```

That bar violates OHLC ordering, and every indicator downstream (ATR,
Highest/Lowest, …) consumes it silently. It fires at roughly every *other*
session boundary — whenever the prior session closed downward — and with
Break at EOD defaulting ON, that's a live path on a stock chart.

To spot it yourself: find a session that closed *downward* into the 17:00 ET
break and look at the first bar after the 18:00 reopen.

---

## What gbTBars changes

1. **Both `Print` calls removed.** On MNQ that's ~2.3M calls per day of chart
   data loaded.
2. **`barDir = 0` on re-seed.** Collapses both thresholds symmetrically onto the
   open — exactly how the chart's first bar behaves — so inversion is
   structurally impossible.
3. **Errors surface.** The blanket `catch` now logs once at `LogLevel.Error`
   instead of silently continuing.
4. **Dead code gone**, duplicated state test gone, meaningful identifiers.

Everything else is deliberately unchanged, **including the integer division** in
`N / 2`. A chart at a given Speed prints the same bars on either bar type
(session boundaries aside), which is what makes the comparison in
[Evidence](#evidence) meaningful.

Fix 2 is the only behavioural divergence, and it's confined to session
boundaries. It is worth knowing that it makes gbTBars agree with the Python
port's *default*, where stock TBars does not.

### Not done (deliberately)

- **No parameter expansion.** Trend/reversal/open-offset stay locked to `N`.
  Splitting them into three independent settings is the single most interesting
  upgrade — the 4× ratio is currently unexplorable — but it breaks
  Speed-for-Speed comparability, so it's a v2 decision.
- **No raw-price toggle.** A `UseHeikinAshi` switch would give real prices in
  `Close[0]`, which would help strategy work considerably. Additive and cheap;
  just not in scope for a fix release.

---

## Evidence

Everything above is measured, not asserted. Parity gate: `gbBarExporter` on an
**MNQ 09-26 / TBars / Speed 120** chart, 2026-07-19 → 07-31, Break at EOD ON,
**2232 bars**, compared against the Python port.

| metric | result |
|---|---|
| bars matched by close time | **99.1%** |
| identical high/low | **98.7%** |
| identical close | **91.5%** |
| identical full OHLC | **79.3%** |

Geometry and bar **timing** are certified. Bar 1 was verified by hand to the
cent: seed doji 28747.5, then `open = (28717.5 + 28747.5)/2 = 28732.5`,
`close = 114975/4 = 28743.75`.

**Rounding mode, established end-to-end:**

| mode | OHLC parity |
|---|---|
| none | 0.1% |
| half-up | 71.6% |
| half-down | 72.7% |
| **half-to-even** | **79.3%** |

**Volume — NT8 double-counts the breakout tick.** It calls `UpdateBar(…, volume)`
for the completing tick *and* `AddBar(…, volume)` for the bar it opens. 98.3% of
bars satisfy `nt8_vol == port_vol + volume[breakout tick]`, and NT8's bar volumes
sum to **2,292 contracts more than were actually traded** in the window. Not
inferred — measured.

**Residuals** (both characterised, neither a geometry error):

1. **±1 tick on the two HA averages, ~9% of bars.** Both formulas were verified
   against NT8's *own* stored values, where residuals are only 0, ±0.25, ±0.5
   ticks — exactly what tick-rounding predicts. So this is error *propagation*:
   one divergent bar shifts the next open, which shifts its close, until it
   self-heals.
2. **29 bars (1.3%) at session boundaries.** NT8 with Break at EOD on re-seeds at
   the *trading-hours template* boundary; the port re-seeds on a >30 min trade
   gap. The direction-carry is not the driver — forcing it moved the total only
   78.8% → 79.5%.

For calibration: ninZaRenko landed at 96-100% and SaberRenko at 96-97%, but both
emit raw prices. TBars emits a 4-way average of four rounded quantities, so a
single tick of divergence anywhere is structurally far more visible.

---

## Backtester

The Python tick-level backtester (`Python/backtester/`) has a matching port:
`build_tbar_bars`, bar-spec grammar **`tb<N>`** (e.g. `tb120`).

```powershell
.venv\Scripts\python cli.py strategies\my_strategy.py --symbol MNQ --period tb120
```

`nt8config.py` maps **both** 98765 (vendor) and 91001 (gbTBars) to the same
`tb<N>` spec. Two deliberate divergences from NT8, both documented in the spec:

- The breakout tick's volume goes to the **new bar only** — the engine resolves
  non-overlapping `[i0, i1)` spans, and a double-covered tick could fill one
  order twice.
- Session reset is **gap-driven** (>30 min) rather than template-driven.

Full spec, including the worked geometry and the parity methodology:
**`Python/backtester/research/TBars_spec.md`**.

---

## Validation status

- [x] Algorithm characterised and reproduced independently (Python port)
- [x] Parity gate vs. real NT8 chart export — **vendor** build
- [x] Volume behaviour confirmed
- [x] gbTBars compiles clean
- [ ] **gbTBars chart-parity run** ← next
- [ ] Confirm fix 2 on a live chart (down-session → 18:00 ET reopen)
- [ ] Session-boundary residual: match the trading-hours template boundary
- [ ] MGC parity (needs contract **MGC 08-26**, window ending 2026-07-28 — the
      recorded contract rolls out after that)

Testable prediction for the next run: because fix 2 aligns gbTBars with the
port's default reset behaviour, a gbTBars export should score **better than
79.3%**, with the 29 session-boundary bars shrinking.

---

## Provenance and licensing

TBars is a **third-party commercial NinjaTrader product**. This directory
contains no vendor code: `gbTBars.cs` was written from the behavioural
specification derived and validated above, not by editing decompiled source.

The decompilations used to derive that specification are kept **locally only**
and are gitignored — same policy this repo already applies to ninZaRenko. Two
things follow, and they are worth being explicit about:

- **Don't commit vendor DLLs, zips, or installers to a public repository.**
  Redistributing a paid product is not cured by it being "just a backup".
- **Don't hand gbTBars to other people as a drop-in replacement for a product
  they haven't licensed.** It's an independent implementation, but the
  surrounding etiquette still applies.
