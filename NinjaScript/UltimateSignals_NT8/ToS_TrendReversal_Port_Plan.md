# ToS Trend Reversal → NinjaScript: Evaluation and Port Plan

**Date:** 2026-08-01
**Sources evaluated:**
- [Trend Reversal for ThinkorSwim](https://usethinkscript.com/threads/trend-reversal-for-thinkorswim.183/) — the base study
- [Enhanced Trend Reversal Indicator](https://usethinkscript.com/threads/enhanced-trend-reversal-indicator-for-thinkorswim.393/) — the community extension

**Context:** [UltimateSignals_Review.md](UltimateSignals_Review.md). The vendor DLL is packed,
HWID-licensed, has zero parameters, and paints both trade directions in one colour so its sell side
cannot be captured. Rebuilding from the public ToS source removes all four problems at once.

---

## 1. Which script is UltimateSignals actually a port of?

Neither, exactly. The DLL's private field names (recovered from metadata — the names survived packing
even though the code did not) map cleanly onto **the base Trend Reversal study plus three ToS
built-ins**, and contain **nothing** from the Enhanced variant.

| DLL field / type | ToS origin | Verdict |
|---|---|---|
| `superfast_length`, `fast_length`, `slow_length`, `mov_avg9`, `mov_avg14`, `mov_avg21` | Trend Reversal — EMA 9 / 14 / 21 | **base study** |
| `buy`, `sell`, `buysignal`, `sellsignal`, `Colorbars` | Trend Reversal — signal state + bar colouring | **base study** |
| `revLineTop`, `revLineBot`, `drawTopLine`, `drawBotLine`, `XPlotTopLine`, `XPlotBotLine` | the stop-loss lines | **base study** |
| `TosZigZagHighLow` with `PPercentageReversal`, `PAbsoluteReversal`, `PAtrLength`, `PAtrReversal`, `PTickReversal`; fields `EI`, `EISave`, `EIH`, `EIL`, `dir` | ToS built-in `ZigZagHighLow` | built-in |
| `percentamount`, `revAmount`, `atrreversal`, `atrlength`, `averagelength` | ZigZagHighLow inputs — Enhanced thread quotes defaults 0.01 / 0.05 / ATR len 5 / ATR rev 2.0 | built-in |
| `TosStochastics` with `PeriodK`, `PeriodD`, `Smooth`, `priceHSelect`, `priceLSelect`, `priceCSelect`, `avgType`, `overbought`, `oversold` | ToS built-in `StochasticFull` | built-in |
| `fastLength`, `slowLength`, `MACDLength`, `averageType`, `macd`, `macdup`, `macddown` | ToS built-in `MACD` | built-in |
| `TosWildersMa` | ToS `WildersAverage` | built-in |
| `trendLength`, `sequentialLength`, `minBars`, `method`, `fastMa`, `slowMa` | a trend-strength / sequential-count layer | unidentified |
| **absent:** any VWAP field, `numDevUp`/`numDevDn`, any engulfing field | Enhanced adds VWAP + engulfing | **not the Enhanced variant** |

**Conclusion:** UltimateSignals = base Trend Reversal + ZigZagHighLow + StochasticFull + MACD + a
sequential/trend-strength counter. That is the spec to rebuild. The Enhanced thread is still useful —
it is where the ZigZagHighLow default values are documented — but its VWAP and engulfing filters are
not in this product and should not be ported for parity.

This also explains the three-tier arrow set in the renderer (`renderArrowUpSignal`/`Down`,
`renderArrowLong`/`Short`, `renderArrowBuy`/`Sell`): one tier per engine.

---

## 2. Evaluation of the base study

### 2.1 The logic

Three EMAs of close — 9 (superfast), 14 (fast), 21 (slow), displacement 0 — and a two-state machine:

```
buy      = mov_avg9 > mov_avg14  and  mov_avg14 > mov_avg21  and  low  > mov_avg9
sell     = mov_avg9 < mov_avg14  and  mov_avg14 < mov_avg21  and  high < mov_avg9

stopbuy  = mov_avg9 <= mov_avg14
stopsell = mov_avg9 >= mov_avg14

signal fires on the transition  !buy[1] and buy   (resp. !sell[1] and sell)
state is held until the matching stop condition clears it
```

Bar colouring: green while long-signal is active, red while short-signal is active, plum when
neither. Plus a stop-loss line — light green under an uptrend, light red over a downtrend. Those are
the green and red horizontal rails visible in the supplied screenshots.

**It is perfectly symmetric.** Every long condition has an exactly mirrored short condition. This
matters: it means the sell-side capture failure in the vendor DLL is a *port* defect, not something
inherited from the source, and a correct port will not reproduce it.

### 2.2 It repaints — and the author says so

From the source thread: **"This indicator will repaint."** The stated mechanism is that a buy or sell
signal disappears if a candle closes through the stop-loss line.

That is a design property, not a bug, and it independently confirms finding F6 of the review from the
source side. Any port must decide explicitly what to do about it (§4.4).

### 2.3 Is it worth porting?

Honestly: **port it to own it, not because it has a proven edge.**

- The entry condition is a trend-following pullback filter (three MAs stacked, and price has pulled
  back to but not through the fastest). That is reasonable and unremarkable.
- The layers the vendor added on top — Stochastic overbought/oversold, ZigZag pivots — are
  *mean-reverting*. Stacking a fade on top of a trend filter is where the observed behaviour comes
  from: screenshot 2 shows 10 sell signals fired into a ~280-point MNQ advance.
- Signal frequency is high. In the two supplied screenshots: 5 up / 5 down over a 6-hour span, and
  8 up / **10 down** over a 53-minute span.

So the case for porting is not "this study makes money". It is:

| Problem with the DLL | Fixed by porting? |
|---|---|
| F1 sell side uncapturable (one brush for both directions) | **Yes** — we choose the brushes, tags and plots |
| F3 packed, HWID licence, self-terminates, vendor-server dependency | **Yes** — no licence, no phone-home |
| F4 zero parameters, untunable | **Yes** — every ToS input becomes an NT8 property |
| F6 repaint | **Controllable** — we decide whether the stop line retracts signals |
| F7 bundles a private NinjaTrader.Custom, collides with other vendor DLLs | **Yes** — one small file |
| Unauditable | **Yes** — source in the repo |

Then run [UltimateSignals_Validation_Process.md](UltimateSignals_Validation_Process.md) against
*your own* build, where the regime split (Stage 4) can actually be acted on with a filter — which is
impossible on a zero-parameter black box.

### 2.4 The Enhanced variant

Adds three things over the base:

1. **VWAP positioning** — `timeFrame` (DAY default), `numDevDn` −2.0, `numDevUp` 2.0. Cyan arrows
   mark reversals confirmed by price position vs VWAP bands.
2. **Engulfing candle detection** — body max above prior body max *and* body min below prior body
   min, with matching close direction. White arrows.
3. **"Advanced Market Moves"** — explicitly VIP-only, **not present in the public code**.

Toggles `bEngulf`, `sEngulf`, `bVWAP`, `sVWAP` control arrow visibility.

Two notes. First, item 3 means the Enhanced study **cannot be fully reproduced from the public
thread** — anyone claiming parity with it is claiming parity with code they do not have. Second,
none of these three appear in UltimateSignals, so porting them is a *feature request*, not parity
work. Park them in Phase 4.

---

## 3. Provenance

The base study is community-posted thinkScript of unattributed origin, credited on useThinkScript to
SkinnyFry and "Bayside of Enhanced Investor". `ZigZagHighLow`, `StochasticFull`, `MACD` and
`WildersAverage` are ThinkOrSwim built-ins (Schwab).

Port practice for this repo, consistent with how the ninZa work was handled: implement from the
*described behaviour* and the published inputs, keep our own implementation, and attribute the
source in the file header. Do not paste vendor DLL output or claim the result is the vendor's
indicator. The ToS built-ins' authoritative source is viewable in the platform itself
(*Studies → Edit Studies*) if exact formulas are needed — that is the reference to check parity
against, not a forum re-post.

---

## 4. NinjaScript translation decisions

These are the places where a naive line-by-line port goes wrong.

### 4.1 Construct mapping

| thinkScript | NinjaScript | Risk |
|---|---|---|
| `ExpAverage(close, 9)` | `EMA(Close, 9)` | **Seeding differs.** ToS seeds its EMA differently from NT8, which seeds from the first value. The first ~3×length bars will not match. Irrelevant intraday, material for a short backtest window. |
| `price[-displace]` | negative barsAgo is not addressable in NT8 | Support `Displacement = 0` only, or implement as a plot offset. The default is 0; do not over-engineer. |
| `CompoundValue(1, expr, 0)` | a recursive `Series<double>` guarded by `if (CurrentBar < 1) { s[0] = 0; return; }` | Standard; just do not read `[1]` on bar 0. |
| `!buy[1] and buy` | keep `buy` as a `Series<bool>`-equivalent (`Series<double>` 1/0) and compare `[0]` vs `[1]` | Do **not** use a plain C# field — it will not survive NT8 re-calculation on historical bars. |
| `AssignPriceColor` | `BarBrush` / `CandleOutlineBrush` | Only works on standard chart styles; note it for Renko. |
| ToS chart bubbles | `Draw.Text` | Use stable tags (§4.3). |
| `Alert()` | `Alert(id, priority, message, sound, rearm, backBrush, foreBrush)` | Rearm seconds matter; the vendor has `useAlerts`, `waitForNextBarAlertUp/Dn`. |
| `ZigZagHighLow` | no NT8 equivalent — NT8's `ZigZag` uses a different reversal model | **Hand-port.** Inputs: percentage reversal, absolute reversal, ATR length, ATR reversal, tick reversal (the DLL's `TosZigZagHighLow` carries all five). |
| `StochasticFull` | NT8 `Stochastics(periodD, periodK, smooth)` | ToS exposes `priceH`/`priceL`/`priceC` **selectors** and a configurable `avgType`; NT8's does not. Hand-roll to match, or accept divergence and document it. |
| `MACD` | NT8 `MACD(fast, slow, signal)` | NT8's is EMA-only; ToS's `averageType` is configurable. Same choice as above. |

### 4.2 Bake in the capturability fix

This is the whole reason for the exercise. Non-negotiable for the port:

- **Two separate, user-configurable brushes** for the buy and sell arrows, defaulting to
  *different* colours (Lime / Red), with a startup check that warns if they are set equal.
- **Stable, side-distinct draw tags** — `GB_TR_BUY_<bar>` / `GB_TR_SELL_<bar>` — so PredatorX's
  `LongSignalTag1` / `ShortSignalTag1` can match a fixed prefix.
- **Named plots carrying 1/0, not NaN.** Condition builders offer only
  `> < >= <= == !=`; none of them can express `IsNaN`. A price-or-NaN plot is a trap.
- Plots to expose: `BuySignal`, `SellSignal`, `TrendState` (+1/0/−1), `StopLineLong`, `StopLineShort`.

### 4.3 Repaint policy — make it an explicit property

Three modes, user-selectable, defaulting to the safe one:

| Mode | Behaviour |
|---|---|
| `Faithful` | Reproduce ToS exactly — the signal is withdrawn when price closes through the stop line. Matches the source; unsafe to automate. |
| `Locked` (default) | Once a signal prints on a closed bar it is never retracted. The plot is write-once. |
| `Confirmed` | Emit only after `ConfirmationBars` have passed and the signal is still present — the `gbUaiWrapperStrategy` pattern. |

`Locked` and `Confirmed` are what make the output safe to hand to an order engine.

### 4.4 GreyBeard house conventions

Per `NinjaScript/GreyBeard-Typical-NinjaTrader.md`: `gb`-prefixed file under an `Indicators/GreyBeard`
subfolder, namespace `NinjaTrader.NinjaScript.Indicators.GreyBeard`, read-only `"0. Developer"`
property group (Author / Version / Website), `IsSuspendedWhileInactive = false`,
`ShowTransparentPlotsInDataBox = true`. Give any enum a unique `Gtr*` prefix — everything compiles
into one assembly and bare enum names collide across strategies.

Property groups: `1. Trend Reversal`, `2. ZigZag`, `3. Confirmation`, `4. Signal Output`,
`5. Display`, `6. Alerts`.

---

## 5. Phasing

Each phase is independently useful and independently testable.

| Phase | Scope | Why this order |
|---|---|---|
| **1** | Base Trend Reversal: 3 EMAs, buy/sell state machine, stop lines, bar colouring, capturable signal output (§4.2), repaint mode (§4.3) | This alone reproduces the BUY/SELL tier — the arrows actually being traded — and solves the capture problem. Everything else is confirmation. |
| **2** | `ZigZagHighLow` port: reversal rails + the tier-2 long/short pivot arrows | Biggest single chunk of new algorithm; also the main repaint source, so it needs Phase 1's repaint plumbing already in place. |
| **3** | `StochasticFull` + `MACD` confirmation tiers → the tier-1 small arrows; the `trendLength` / `sequentialLength` counter | Pure filters. Cheap once the framework exists, and the first thing to reach for when Stage 4 says the counter-trend bucket loses. |
| **4** | Optional: Enhanced-variant VWAP and engulfing filters | Feature work, not parity. Note the VIP-only third component cannot be reproduced. |

---

## 6. Verification, given you cannot run the original

You have no licence for the DLL and may not have ToS open. That rules out bit-exact parity — say so
up front rather than pretending otherwise. Four things you *can* do, in increasing strength:

1. **Hand-check the maths.** EMA(9/14/21) of close is trivially reproducible in a spreadsheet from a
   bar export. Verify `buy` / `sell` / `stopbuy` / `stopsell` on ~30 bars, including at least one
   state transition in each direction. This needs no ToS and no licence, and it catches the errors
   that actually happen (off-by-one on `[1]`, wrong comparison operator, state not latching).
2. **Signal-count sanity** against the measured screenshots — 5 up / 5 down over 03:08–09:10, and
   8 up / 10 down over 10:12–11:05 ET, MNQ 09-26 SaberRenko 70/4. Your Phase-1 build should land in
   the same order of magnitude on comparable data. A 10× discrepancy means a wrong length or a wrong
   operator; an exact match is not expected, because the vendor's hidden parameters are unknown.
3. **Side-by-side on the licence holder's chart.** Send them the gb build; run it on the same chart as
   UltimateSignals and compare arrow-for-arrow. This is the strongest available check, and it needs
   nothing from you but the file.
4. **`gbSignalProbe` on both** — it is late-bound and target-agnostic, so it will log the vendor's
   series and your port's side by side into two CSVs for a diff.

**Explicit non-goal:** matching the DLL exactly. Its parameters are hard-coded and hidden, and it
contains an unidentified `trendLength`/`sequentialLength` layer. Aim for *the published study,
correctly implemented and fully parameterised* — which is strictly more useful than a faithful clone
of a black box.

---

## 7. Risks

| Risk | Mitigation |
|---|---|
| EMA seeding differs between ToS and NT8 | Expect divergence on the first ~3×21 bars. Set `BarsRequiredToPlot` accordingly and never judge parity on the left edge of a chart. |
| `low > mov_avg9` on SaberRenko / Renko bars | Renko synthesises OHLC and the forming brick is provisional, so this condition flickers. Validate on time bars first, then re-check on Renko (Validation §5). |
| Repaint by design | §4.3 — default to `Locked`. |
| Unidentified `trendLength` / `sequentialLength` | Not in either public script. Do not guess; leave it out of Phase 1–3 and note the known gap rather than inventing behaviour. |
| ToS `StochasticFull` price selectors / `avgType` have no NT8 equivalent | Hand-roll, or use NT8's `Stochastics` and document the divergence in the header. Decide before Phase 3, not during. |
| Enum name collisions in the shared Custom assembly | `Gtr*` prefix on every enum. |

---

## 8. Status

**Phase 1 is built:** [gbTrendReversal.cs](gbTrendReversal.cs). Base 3-EMA latched state machine,
fully parameterised (`1. Moving Averages`), distinct configurable buy/sell brushes with a
same-colour startup warning (`2. Signal Output`), stable `GTR_BUY_<bar>` / `GTR_SELL_<bar>` tags,
1/0 `BuyTrigger`/`SellTrigger` plots plus a continuous `TrendState` plot, an approximate stop-line
pair riding EMA9 (flagged as unverified against the exact source formula — §2.2/§7 already call out
that the "closes through the stop line" description couldn't be pinned to an exact formula from the
fetched thread text), bar colouring matching the source's green/red/plum, and optional alerts.

One deliberate deviation from §4.3 of this plan: no `RepaintMode` property. On inspection, this base
engine has nothing to make faithful/locked/confirmed variants *of* — `buy`/`sell`/`stopbuy`/`stopsell`
are pure functions of already-closed Close/High/Low, so under `Calculate.OnBarClose` a closed bar's
`BuyTrigger`/`SellTrigger` value is final by construction, full stop. Real repaint risk enters with
the ZigZag layer (Phase 2), whose pivots are inherently forward-looking — that is where
`ConfirmationBars` plumbing actually earns its keep, not here.

Verification followed §6: every NT8 API call (`EMA` factory, `Draw.ArrowUp/ArrowDown`, `Alert`,
`Serialize.BrushToString/StringToBrush`, `BarBrush`) was checked against the metadata of the
licence holder's installed assemblies (`NinjaTrader.Core.dll`, `NinjaTrader.Custom.dll` — note `Draw`
and the built-in `EMA` live in `bin\Custom\NinjaTrader.Custom.dll`, not `NinjaTrader.Core.dll`) before
being treated as final. That check confirmed the *members* exist but missed one namespace-resolution
detail metadata inspection can't see: `Draw` lives in `NinjaTrader.NinjaScript.DrawingTools`, a
sibling of `...Indicators`, not an ancestor of `...Indicators.GreyBeard` — so it needs an explicit
`using NinjaTrader.NinjaScript.DrawingTools;`, which the first draft omitted (compile error "The name
'Draw' does not exist in the current context", fixed 2026-08-01; `gbUltimateSignalsBridge.cs` already
had it). **Confirmed compiling clean on the licence holder's install as of 2026-08-01.**

Hand-check-the-maths (§6.1) and the signal-count sanity check (§6.2) are still outstanding — do those
before trusting live output.

**Next:** Phase 2 (ZigZagHighLow port) once Phase 1 has been run side-by-side against
UltimateSignals on the licence holder's chart.

Related: [UltimateSignals_Review.md](UltimateSignals_Review.md) ·
[UltimateSignals_Signals.md](UltimateSignals_Signals.md) ·
[UltimateSignals_Validation_Process.md](UltimateSignals_Validation_Process.md) ·
[gbSignalProbe.cs](gbSignalProbe.cs) · [gbUltimateSignalsBridge.cs](gbUltimateSignalsBridge.cs)

Sources: [Trend Reversal for ThinkorSwim](https://usethinkscript.com/threads/trend-reversal-for-thinkorswim.183/) ·
[Enhanced Trend Reversal Indicator for ThinkorSwim](https://usethinkscript.com/threads/enhanced-trend-reversal-indicator-for-thinkorswim.393/)
