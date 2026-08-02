# UltimateSignals — Technical Summary from Decoded Source

**Date:** 2026-08-01
**Source:** `UltimateScalper/` — decompiled C# for the previously-packed `UltimateSignals.dll`
(4,861 lines across 7 files)
**Supersedes:** the inference-based sections of [UltimateSignals_Review.md](UltimateSignals_Review.md)
and [UltimateSignals_Signals.md](UltimateSignals_Signals.md). Everything below is read directly from
source — no `CONFIRM` tags remain.

| File | Lines | Role |
|---|---:|---|
| `UltimateSignalsIndicator.cs` | 706 | **The product.** Orchestrates all three signal tiers. |
| `UltimateSignalsHelpers.cs` | 1368 | `UltimateSignalsNamespace` — private MA/ATR/Stochastic/ZigZag engines + renderers |
| `UltimateAIProV3.cs` | 1887 | A *different, later* product (see §7) |
| `WiZigZagHighLowTOS1v0.cs` | 465 | Standalone ZigZag indicator, **not used** by UltimateSignals |
| `StochasticFullTOS1v0.cs` | 245 | Standalone Stochastic, **not used** by UltimateSignals |
| `WilderMA1v0.cs` | 109 | Standalone Wilder MA, **not used** by UltimateSignals |
| `UltimateAIProV3_Enums.cs` | 81 | Enums for V3 |

Only the first two files are UltimateSignals. The `*TOS1v0.cs` files are separate NinjaScript
indicators that ship in the same assembly; `UltimateSignalsIndicator` uses its own private
`UltimateSignalsNamespace.TosZigZagHighLow` / `TosStochastics` instead.

---

## 1. Verdict on the root cause — confirmed in one line

`UltimateSignalsIndicator.cs:248-249`:

```csharp
AddPlot(new Stroke(Brushes.Magenta, DashStyleHelper.Solid, 5f), PlotStyle.TriangleUp, "Buy Text Marker");
AddPlot(new Stroke(Brushes.Magenta, DashStyleHelper.Solid, 5f), PlotStyle.Dot,        "Sell Text Marker");
```

**Both `Brushes.Magenta`, hard-coded, no property exposed.** This is the exact defect measured off
the screenshots (modal RGB `255,0,255` on both arrow directions) and is now proven rather than
inferred. `OnRenderTargetChanged` feeds those same brushes into the custom arrow renderer:

```csharp
renderArrowBuy  = new TosArrowDraw(buyTextMarker,  1, Plots[7].Width * 5f, Plots[7].Brush);
renderArrowSell = new TosArrowDraw(sellTextMarker, -1, Plots[8].Width * 5f, Plots[8].Brush);
```

A consumer keying on colour cannot separate the sides. Confirmed.

### 1.1 Correction to an earlier claim

The review previously said the vendor's draw-object tags "churn per bar and there is no stable
per-side tag string to type into `ShortSignalTag1`." **That was wrong.** The tag generator is:

```csharp
private string textTag(string text, int barAgo) => text + "_" + (CurrentBar - barAgo);
// → "BUY_10432", "SELL_10457"
```

The **prefix is stable and side-distinct** — `BUY_` vs `SELL_`. If PredatorX does prefix or
substring matching on `LongSignalTag1`/`ShortSignalTag1`, **tag capture works today with no bridge
at all.** Try `BUY` / `SELL` first — it is a two-field change.

The caveat that remains real: these objects are genuinely transient. `calculateBarIndicator2` calls
`removeText(...)` on every bar where the condition is false, and the ZigZag rewrite loop (§4)
re-runs historical bars, so a `BUY_10432` object can be created and destroyed repeatedly as the
pivot revises.

### 1.2 The better free workaround, also confirmed

Tiers 1 and 2 are **already colour-separable** (`:241-245`):

| Plot | Idx | Brush | Style |
|---|:--:|---|---|
| `Up` (`upSignalPlot`) | 0 | **Lime** | TriangleUp |
| `Down` (`dnSignalPlot`) | 1 | **Red** | Dot |
| `EnhancedLines` | 2 | Transparent | Hash |
| `Long` (`long_`) | 3 | **Lime** | TriangleUp |
| `Short` (`short_`) | 4 | **Red** | Dot |
| `botLine` | 5 | LightGreen | Line |
| `topLine` | 6 | Red | Line |
| `Buy Text Marker` (`buyTextMarker`) | 7 | **Magenta** | TriangleUp |
| `Sell Text Marker` (`sellTextMarker`) | 8 | **Magenta** | Dot |

The plot index map I inferred from metadata declaration order was **correct in full** — all nine,
in order. Values[3]/`long_` and Values[4]/`short_` are Lime/Red and directly capturable by colour.

---

## 2. Confirmed value convention

Signal plots carry **a price when active, `NaN` when idle**, via `Series.Reset()`:

```csharp
if (flag) upSignalPlot[0] = Low[0] - TickSize;  else upSignalPlot.Reset(0);
...
buyTextMarker[num] = Low[num]  - TickSize;      // active
buyTextMarker.Reset(num);                        // idle → NaN
sellTextMarker[num] = High[num] + TickSize;
```

So `!double.IsNaN(x)` is the correct active test and `> 0` is wrong — as documented. The
bridge's `IdleIsNaN = true` default is right.

---

## 3. The three tiers — exact logic

All parameters are `private` fields or `const`. **There is not one `[NinjaScriptProperty]` on the
type.** Hard-coded values, verbatim:

```
trendLength 5   sequentialLength 3   useAlerts false
fastLength 5    slowLength 26        MACDLength 9 (declared, NEVER USED — no signal line)
averageType EMA  avgType SMA         method Method.average
overbought 80    oversold 20         KPeriod 10   DPeriod 10   Smooth 3
priceH High      priceL Low          priceC Close
superfast 9      fast 14             slow 21
ZigZag: percentamount 0.01  revAmount 0.05  atrreversal 2.0  atrlength 5  averagelength 5
Calculate = Calculate.OnEachTick    IsOverlay = true    MaximumBarsLookBack = Infinite
```

### Tier 1 — `upSignalPlot` / `dnSignalPlot` (MACD reversal + Stochastic gate)

```csharp
macd[0] = EMA(Close,5)[0] - EMA(Close,26)[0];           // no signal line anywhere

// 3 consecutive rises AND above the value 5 bars back
macdup[0]   = (rises over last 3) == 3 && macd[0] >= macd[5];
macddown[0] = (falls over last 3) == 3 && macd[0] <  macd[5];

up   = macddown[1] && macd[0] > macd[1] && StochK[0] < 80;   // → upSignalPlot = Low  - 1 tick
down = macdup[1]   && macd[0] < macd[1] && StochK[0] > 20;   // → dnSignalPlot = High + 1 tick
```

A momentum-exhaustion reversal: MACD was falling for 3 bars, ticks up, and Stochastic is not yet
overbought. **Symmetric.** Fires often — this is the small red/green arrow tier.

### Tier 2 — `long_` / `short_` (ZigZag pivot flip, trend established)

```csharp
flag2 = signal[num] > 0 && signal[num+1] <= 0;    // ZigZag state flipped up
flag3 = signal[num] < 0 && signal[num+1] >= 0;    // flipped down

if (flag2 && Colorbars[num] != 3) long_[num]  = Low[num];
if (flag3 && Colorbars[num] != 3) short_[num] = High[num];
```

### Tier 3 — `buyTextMarker` / `sellTextMarker` (the traded BUY/SELL labels)

```csharp
if (flag2 && Colorbars[num] == 3 && signal[num] > 0 && signal[num+1] <= 0) {
    buyTextMarker[num] = Low[num] - TickSize;
    addText("BUY", num, location, color, -1);
} else { buyTextMarker.Reset(num); removeText("BUY", num); }
```

**Non-obvious and important:** `Colorbars[num] == 3` means *neither* the 3-EMA buy state nor the
sell state is active — the trend machine is **neutral**. Tier 2 requires `!= 3` (trend established).
So **tiers 2 and 3 are mutually exclusive by construction**: a ZigZag flip produces a `long_`/`short_`
arrow when a 3-EMA trend is running, and a `BUY`/`SELL` label when it is not. That is why the
magenta BUY/SELL markers are so much rarer than the Lime/Red arrows on the chart.

### The 3-EMA state machine (`Colorbars`)

```csharp
buy[0]  = ma9 > ma14 && ma14 > ma21 && Low[0]  > ma9;
sell[0] = ma9 < ma14 && ma14 < ma21 && High[0] < ma9;

// latched: fires on the transition into the condition, clears on the MA cross
buysignal[0]  = (!buy[1]  && buy[0]  && !(ma9 <= ma14)) || (!(buysignal[1]  && ma9 <= ma14) && buysignal[1]);
sellsignal[0] = (!sell[1] && sell[0] && !(ma9 >= ma14)) || (!(sellsignal[1] && ma9 >= ma14) && sellsignal[1]);

Colorbars[0] = buysignal[0] ? 1 : sellsignal[0] ? 2 : 3;
```

This is the ToS Trend Reversal study, matching the public thinkScript exactly. **Symmetric** — as
[ToS_TrendReversal_Port_Plan.md](ToS_TrendReversal_Port_Plan.md) §2.1 predicted.

### The ZigZag runs on *smoothed* high/low

Because `method == Method.average` (hard-coded):

```csharp
mah = EMA(High, 5);   mal = EMA(Low, 5);
priceh = (method != Method.high_low) ? mah.Values[0] : High;   // → EMA(High,5)
pricel = (method != Method.high_low) ? mal.Values[0] : Low;    // → EMA(Low,5)
EI.highSeries = priceh;  EI.lowSeries = pricel;
```

The ZigZag never sees raw `High`/`Low`. Any reimplementation that feeds it raw extremes will not
match. Note this also means the *rendered* `long_`/`short_`/BUY/SELL prices come from raw
`Low[num]`/`High[num]` while the *pivot detection* uses the smoothed series.

---

## 4. Repaint — mechanism identified precisely

Three compounding sources, all confirmed:

**1. `Calculate = Calculate.OnEachTick`, hard-coded.** Every tick re-evaluates the forming bar.

**2. The historical rewrite loop** (`OnBarUpdateIndicator2`):

```csharp
for (int i = EI.xLastChangedBar; i <= CurrentBar; i++)
    calculateBarIndicator2(i);
```

Every bar update recomputes **every bar back to `xLastChangedBar`** — rewriting `long_`, `short_`,
`buyTextMarker`, `sellTextMarker`, `botLine`, `topLine` on already-closed bars.

**3. The ZigZag retracts confirmed pivots** (`TosZigZagHighLow.updateBar`):

```csharp
if (flag) {                                     // new high pivot
    base.Value[num] = highSeries[num];
    extremumDir[num] = 1;
    if (num2 > 0 && num3 >= 0) {                // a prior high pivot existed
        base.Value.Reset(CurrentBar - num3);    // ERASE it
        extremumDir[CurrentBar - num3] = 0;
        updateLastChangedBar(num3);             // pull the rewrite window further back
    }
}
```

A previously-printed pivot is deleted and `xLastChangedBar` is pulled backward, widening the
rewrite window on the next pass. **This is unbounded in principle** — the depth depends on how far
back the superseded pivot sits.

**Consequence:** a `BUY`/`SELL` label can appear, then be removed by `removeText`, on a bar that
already closed. Backtests and historical chart reads show the revised record, not what a live
trader saw. `ConfirmationBars ≥ 1` in the bridge is mandatory, and `gbSignalProbe`'s REVISION count
is the way to size it.

---

## 5. Vendor bug found in passing

`UltimateSignalsHelpers.cs`, `LineDraw.render()`:

```csharp
int yByValue  = chartScale.GetYByValue(tRM_.GetValueAt(i));
int yByValue2 = chartScale.GetYByValue(tRM_.GetValueAt(i));   // ← should be i + 1
drawLine(renderTarget, xByBarIndex, yByValue, xByBarIndex2, yByValue2, thM_);
```

`yByValue2` reads bar `i` instead of `i+1`, so `topLine`/`botLine` render as a staircase of flat
segments rather than a connected sloping line. Cosmetic only — the plot **values** are correct, so
anything consuming `Values[5]`/`Values[6]` programmatically is unaffected.

Also dead code: `OnRenderZigzag` is defined but never called from `OnRender`, and `MACDLength = 9`
is assigned but never read.

---

## 6. Corrections to my earlier analysis

| Earlier claim | Status |
|---|---|
| Plot indices 0–8 in metadata declaration order | **Correct** — all nine confirmed |
| Both BUY/SELL arrows are magenta `#FF00FF` | **Correct** — hard-coded `Brushes.Magenta` |
| Idle sentinel is `NaN` | **Correct** — `Series.Reset()` |
| Zero user parameters | **Correct** — no `[NinjaScriptProperty]` anywhere on the type |
| Repaints | **Correct**, and mechanism now precisely located (§4) |
| ToS study is symmetric; defect is in the port | **Correct** |
| Tier-1 arrows are colour-separable | **Correct** — Lime / Red |
| "No stable per-side tag exists" | **WRONG** — `BUY_`/`SELL_` prefixes are stable and side-distinct (§1.1) |
| "It is a ToS port" | **Correct**, and it is the *base* Trend Reversal, not the Enhanced variant — no VWAP, no engulfing anywhere in the source |
| Tier-2 `long_`/`short_` semantics | **Refined** — tiers 2 and 3 are mutually exclusive via `Colorbars == 3` (§3) |

---

## 7. `UltimateAIProV3` is the successor — and it already fixes this

`UltimateAIProV3.cs` is a *different, later* indicator in the same assembly, exposing the same
signal family plus more:

```
upSignal, dnSignal, Colorbars, EnhancedLines, longS, shortS,
botLine, topLine, BUY, SELL, glBuffer_Dot_longS, glBuffer_Dot_shortS,
botLine_htf, topLine_htf          ← higher-timeframe lines
```

Two things matter here.

**It is the `UltimateAI2` family already wrapped by `gbUaiWrapperStrategy`.** That strategy reads
`uai2.longS[...]` / `uai2.shortS[...]` — the exact property names above. The repaint behaviour
recorded for UltimateAI2 and the repaint mechanism decoded in §4 are the same lineage, which
retroactively explains why `ConfirmationBars` was needed there.

**Its arrow colours are user-configurable.** V3 declares `[CategoryOrder]` groups
`"Small Arrow Settings"` and `"Large Arrow Settings"`, and its plots take configurable `Stroke`
objects (`pmBuffer_upSignal_Stroke_stroke`, `pmBuffer_long_Stroke_stroke`, …) rather than the
hard-coded `Brushes.Magenta` of UltimateSignals.

**Therefore: if the licence covers UltimateAIProV3, switching to it makes the sell side capturable
with no bridge and no port** — set the large buy and sell arrows to different colours in its own
settings. That is the cheapest path to a fix and should be checked before anything else.

---

## 8. Revised recommendation, in order of cost

1. **Check whether UltimateAIProV3 is licensed and available** (§7). If yes, use it and set distinct
   large-arrow colours. Problem solved in the vendor's own UI.
2. **Try tag capture on UltimateSignals** — `LongSignalTag1 = BUY`, `ShortSignalTag1 = SELL` (§1.1).
   Two fields, no code. Works if PredatorX does prefix/substring matching.
3. **Capture tier 2 by colour** — `long_` is Lime, `short_` is Red (§1.2). Note tier 2 fires only
   while a 3-EMA trend is established, which is the complement of when BUY/SELL labels fire.
4. **[gbUltimateSignalsBridge.cs](gbUltimateSignalsBridge.cs)** — now fully specified: set
   `BuyPlotIndex = 7`, `SellPlotIndex = 8`, `IdleIsNaN = true`, `ConfirmationBars ≥ 1`.
5. **[gbTrendReversal.cs](gbTrendReversal.cs)** — own the 3-EMA engine outright, fully
   parameterised, non-repainting, distinct colours by construction.

Whichever path is taken, §4 means **no historical or backtested evaluation of UltimateSignals is
trustworthy.** Forward-only, via `gbSignalProbe`.

---

## 9. Licensing note

This source is decompiled from a commercially licensed, deliberately packed assembly. Keep it out of
any public distribution, and keep [gbTrendReversal.cs](gbTrendReversal.cs) clean-room — it is written
from the **public thinkScript** study (useThinkScript threads 183/393) and the vendor's own published
parameter values, not from this decompilation. That separation is what makes the port distributable;
do not paste vendor code into it.

---

Related: [UltimateSignals_Review.md](UltimateSignals_Review.md) ·
[UltimateSignals_Signals.md](UltimateSignals_Signals.md) ·
[UltimateSignals_Validation_Process.md](UltimateSignals_Validation_Process.md) ·
[ToS_TrendReversal_Port_Plan.md](ToS_TrendReversal_Port_Plan.md)
