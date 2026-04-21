# KingPanaZilla — NinjaTrader 8 Indicator Suite

**Namespace:** `NinjaTrader.NinjaScript.Indicators.GreyBeard.KingPanaZilla`

A curated collection of five NinjaTrader 8 indicators unified under the GreyBeard namespace. Four core indicators were reworked from their original vendor sources into a consistent `gb`-prefixed naming convention, sharing a common visual style (toggle button, gradient zones, custom marker rendering, popup/sound/email alerts). A fifth composite indicator — `gbKingPanaZilla` — loads the three signal-emitting indicators and combines their outputs into three cross-system trade signals.

---

## Indicators

### gbKingPanaZilla — Composite Signal Indicator

A **meta-indicator** that loads `gbKingOrderBlock`, `gbPANAKanal`, and `gbThunderZilla` and cross-combines their `Signal_Trade` values into three unified trade signals. Add this single indicator to a chart to get all three child indicators' signals in one place, or reference it from a Strategy via `gbKingPanaZilla()`.

**How it works:**

The three child indicators are instantiated via their factory methods (`CacheIndicator`) in `State.DataLoaded`, placing them in NinjaTrader's `NinjaScripts` collection so their `OnBarUpdate` runs automatically each bar.

Each of the three output plots uses **+1 (buy), −1 (sell), 0 (no signal)**:

| Plot | Condition (buy) | Condition (sell) |
|---|---|---|
| `PanaZillia_Trade` | PanaKanal `Signal_Trade ≥ 2` **AND** ThunderZilla `Signal_Trade ≥ 3` | PanaKanal `≤ −2` AND ThunderZilla `≤ −3` |
| `KingZilla_Trade` | ThunderZilla `Signal_Trade ≥ 3` **AND** KingOrderBlock `Signal_Trade ≥ 1` | ThunderZilla `≤ −3` AND KingOrderBlock `≤ −1` |
| `KingPana_Trade` | PanaKanal `Signal_Trade ≥ 2` **AND** KingOrderBlock `Signal_Trade ≥ 1` | PanaKanal `≤ −2` AND KingOrderBlock `≤ −1` |

The signal thresholds capture the **highest-conviction sub-signals** from each child:
- PanaKanal ≥ 2 = Break or Pullback confirmation (not just a raw trend flip)
- ThunderZilla ≥ 3 = Pullback signal (both SolarWind + Sumo aligned with a pullback)
- KingOrderBlock ≥ 1 = any Return or Breakout signal

**Output plots:** `PanaZillia_Trade`, `KingZilla_Trade`, `KingPana_Trade`

**Child indicator signal scales (for reference):**

| Indicator | Positive value | Negative value |
|---|---|---|
| `gbPANAKanal Signal_Trade` | 1=Trend Start, 2=Break, 3=Pullback | −1/−2/−3 mirror |
| `gbThunderZilla Signal_Trade` | 1=Trend Start, 2=Slowdown, 3=Pullback, 4=Move Stop | −1/−2/−3/−4 mirror |
| `gbKingOrderBlock Signal_Trade` | 1=Return, 2=Breakout | −1/−2 mirror |

**Visual output:** `Draw.ArrowUp` / `Draw.ArrowDown` markers painted on the price panel at configurable tick offsets above/below each signal bar.

**Key parameters:** all `King_*`, `Pana_*`, and `Thunder_*` parameters mirror the child indicator defaults. Visual: `PanaZilliaBrush`, `KingZillaBrush`, `KingPanaBrush`, `ArrowOffset`

---

### gbKingOrderBlock — King Order Block

Identifies **Order Blocks** (institutional supply and demand zones) using Structure Break analysis.

**How it works:**

1. **Swing Point Detection** — scans for swing highs and lows using a configurable *neighborhood* period. Three detection algorithms handle flat-top/bottom plateaus and mixed-width structures.
2. **BOS / CHoCH Classification** — when price breaks a swing, the indicator classifies it as either a *Break of Structure* (BOS, trend continuation) or *Change of Character* (CHoCH, potential reversal) based on the sequence of prior breaks.
3. **Imbalance (Fair Value Gap) Detection** — looks for consecutive marubozu candles in the same direction to mark imbalanced price zones.
4. **Order Block Formation** — an Order Block is confirmed when a swing point is paired with a nearby imbalance and a BOS/CHoCH in the opposite direction, within a configurable look-back period.
5. **Signals** — two signal types are emitted:
   - **Return** — price wicks back into an active Order Block zone (entry signal).
   - **Breakout** — price closes through the invalidation level of an Order Block.
6. **Zone Rendering** — active and inactive Order Blocks and Imbalances are drawn as gradient-shaded rectangles. BOS/CHoCH levels are drawn as dashed lines with optional text labels. Swing points are rendered as filled ellipses.

**Output plots:** `Signal_Trade`, `Signal_State`, `Signal_Zone_Bullish`, `Signal_Zone_Bearish`

**Key parameters:** `SwingPointNeighborhood`, `ImbalanceQualifying`, `OrderBlockFindingBosChochPeriod`, `OrderBlockAge`, `OrderBlocksSameDirectionOffset`, `OrderBlocksDifferenceDirectionOffset`, `SignalTradeQuantityPerOrderBlock`, `SignalTradeSplitBars`

---

### gbPANAKanal — PANA Kanal

A **Keltner-style adaptive channel** that defines trend direction and generates pullback / breakout trade signals.

**How it works:**

1. **Channel Calculation** — uses a Wilder-style ATR multiplied by a configurable `Factor` to build dynamic upper and lower bands around the close. Once the close crosses a band, the band locks in place (ratchet behaviour) and only moves in the trend direction.
2. **Middle Band** — a double-smoothed EMA (EMA of EMA) over a configurable `MiddlePeriod` acts as a Keltner centre line used for Keltner-state transitions.
3. **Trend Direction** — the indicator is in *uptrend* when close is above the lower channel band; in *downtrend* when below the upper band. Direction flips are the Trend Start signal.
4. **Fibonacci Pullback Zones** — at each trend flip the indicator calculates the range from the trend extremum to the trailing stop and places Fibonacci levels at 61.8 % and 78.6 % retracement as pullback target zones.
5. **Signals:**
   - **Trend Start** — candle that crosses and closes past the channel band (trend reversal).
   - **Trend Pullback** — inside the pullback-finding period, a reversal candle (prior candle opposite direction, current candle trend direction) that wicks into the 61.8 % Fibonacci zone.
   - **Break** — price crosses the Keltner middle band in the trend direction after a pullback reset, confirming trend continuation.
6. **Bar Painting & Region Fill** — bars are coloured by trend direction; two gradient fill regions are drawn between the Fibonacci levels and the trailing stop.

**Output plots:** `Extremum`, `Middle`, `TrailingStop`, `Signal_Trend`, `Signal_Trade`

**Key parameters:** `Period`, `Factor`, `MiddlePeriod`, `SignalBreakSplit`, `SignalPullbackFindingPeriod`

---

### gbThunderZilla — ThunderZilla

A **dual-system trend + pullback indicator** that combines a SolarWind trailing stop with a Sumo multi-MA pullback detector and a multi-oscillator overbought/oversold slowdown filter.

**How it works:**

1. **SolarWind (SW) System** — computes a trailing stop offset by `StopOffsetMultiplierStop × TickSize`. The trend flips when close crosses the trailing stop. A secondary "trend vector" line (30 ticks above/below close) tracks the swing's momentum direction. Both SW trend direction and stop level are tracked independently.
2. **Sumo Pullback System** — aligns four moving averages: the configurable Trend MA (default SMA 100) plus EMA 14, EMA 30, and EMA 45. When all four are stacked (max = Trend MA in uptrend, min = Trend MA in downtrend) and a two-candle reversal pattern occurs (prior candle opposite, current candle trend direction) with price entirely inside the MA stack, a Sumo pullback signal fires. A spacing system (first signal at 15 bars, second at 30 bars) limits signal frequency.
3. **Combined Trend State** — the indicator is in *uptrend* only when both SW and Sumo agree; *downtrend* when both point down; *neutral* when they disagree. Transitions emit Trend Start signals.
4. **Multi-Oscillator OBOS Slowdown** — MFI (14), RSI (14 / smooth 3), and Stochastics (14/7/3) are evaluated simultaneously. When all three are overbought (> 70) simultaneously the indicator flags a *Slowdown*; when all three are oversold (< 30) it flags the reverse. The signal fires on the exit bar from the overlap zone within a 3-bar window.
5. **Move Stop Signal** — fires when the trailing stop crosses above the Trend MA in uptrend (or below in downtrend), signalling that the stop has been upgraded to a profit-protecting level.
6. **Cloud Rendering** — a shaded cloud is drawn between the Trend MA and the bar's body extremum, coloured by trend state.

**Output plots:** `Trend`, `Stop`, `Signal_Trend`, `Signal_Trade`

**Key parameters:** `TrendMAType`, `TrendPeriod`, `TrendSmoothingEnabled`, `TrendSmoothingMethod`, `TrendSmoothingPeriod`, `StopOffsetMultiplierStop`, `SignalQuantityPerFlat`, `SignalQuantityPerTrend`

---

### gbBarStatus — Bar Status

A **bar-completion progress indicator** that shows how far through the current bar you are, and optionally draws boundary lines for price-based chart types.

**How it works:**

1. **Time/Tick/Volume Charts** — on Second, Minute, Tick, and Volume charts the indicator tracks elapsed vs. total bar units and renders a horizontal progress bar with optional count-up / count-down text.
   - *Seconds / Minutes* — a DispatcherTimer fires every 500 ms, computing remaining time from `Bars.GetTime(CurrentBar)` versus the platform clock.
   - *Ticks* — `Bars.TickCount` is compared against the bar's tick period.
   - *Volume* — current bar volume is compared against the period volume.
2. **Price-Based Charts (Renko / Range / ninZaRenko / KingRenko)** — instead of a progress bar the indicator calculates and plots the upper and lower price bounds at which the current bar would close, accounting for a configurable tick offset and the asymmetric brick sizes of KingRenko.
3. **Gradient Colouring** — the progress bar cycles through five configurable gradient stops (0 %, 25 %, 50 %, 75 %, 100 % complete) to give an at-a-glance sense of urgency.
4. **Interactive Click** — clicking within the progress bar area cycles through three display modes (bar + text → bar only → text only). Clicking the count-mode symbol toggles between count-up and count-down.

**Output plots:** `UpperBound`, `LowerBound`

**Key parameters:** `BoundOffset`

---

## Common Features (all indicators)

| Feature | Details |
|---|---|
| **Toggle button** | Draggable on-chart button to show/hide indicator drawings without removing it |
| **Alert system** | Popup window, WAV sound (with rearm timer), and email alerts per signal condition |
| **Custom marker rendering** | Text markers drawn via DirectWrite for crisp DPI-aware rendering |
| **Gradient zone fills** | SharpDX LinearGradientBrush rendering for Order Block and Imbalance zones |
| **Bar colouring** | Optional bias-based bar painting by trend state |
| **User Note** | `instrument (period)` token substitution in the indicator title bar |
| **Z-Order control** | Configurable `IndicatorZOrder` so panels layer correctly |

---

## File Structure

```
KingPanaZilla/
├── gbKingPanaZilla.cs      — Composite: combines the three signal indicators below
├── gbKingOrderBlock.cs     — Order Block / Imbalance / BOS/CHoCH zones
├── gbPANAKanal.cs          — Keltner channel trend + Fibonacci pullback signals
├── gbThunderZilla.cs       — SolarWind + Sumo dual-system trend indicator
├── gbBarStatus.cs          — Bar completion progress display
└── originals/              — Unmodified vendor source files
    ├── RenkoKings_KingOrderBlock.cs
    ├── RenkoKings_ThunderZilla.cs
    ├── ninZaPANAKanal.cs
    └── ninZaBarStatus.cs
```

## Installation

Copy all five `gb*.cs` files into your NinjaTrader 8 custom indicators folder:

```
Documents\NinjaTrader 8\bin\Custom\Indicators\
```

Then compile via **NinjaTrader → Tools → Edit NinjaScript → Compile**.

`gbKingPanaZilla` requires the other three signal indicators (`gbKingOrderBlock`, `gbPANAKanal`, `gbThunderZilla`) to be present — all five files must be compiled together. `gbBarStatus` is standalone.

---

*GreyBeard — KingPanaZilla indicator suite*
