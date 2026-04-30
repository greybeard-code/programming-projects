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

**Display parameters:** `ShowKingOrderBlock`, `ShowPANAKanal`, `ShowThunderZilla` (all default `true`) — when enabled, the corresponding child indicator's drawings (zones, channels, clouds) are rendered on the same chart panel as `gbKingPanaZilla`. Each can be toggled off independently to reduce visual clutter while retaining its signal in the combined plots.

**CSV Logging:**

Enable the **Logging → Enabled** toggle to write a signal log to the NinjaTrader user data folder (`Documents\NinjaTrader 8\`). The file is named with the activation timestamp:

```
gbKPZlog_YYYYMMDD_HHmmss.csv
```

The file is created at indicator load with a header row. One row is appended for each bar on which at least one trade signal fires:

```
DateTime,Instrument,Price,PanaZillia_Trade,KingZilla_Trade,KingPana_Trade
2026-04-26 09:31:00,"NQ 06-26",21483.50,1,1,0
2026-04-26 10:15:00,"NQ 06-26",21510.25,0,0,-1
```

| Column | Content |
|---|---|
| `DateTime` | Bar close time — `yyyy-MM-dd HH:mm:ss` |
| `Instrument` | Full contract name (quoted) |
| `Price` | Close price formatted to instrument tick precision |
| `PanaZillia_Trade` | −1 / 0 / 1 |
| `KingZilla_Trade` | −1 / 0 / 1 |
| `KingPana_Trade` | −1 / 0 / 1 |

The writer is flushed after every row and closed cleanly when the indicator is removed or the chart is closed. Logging is **off by default**.

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

## Strategy

### gbKingPanaZillaKillah — ATM Strategy by Playr101

An **ATM-mode strategy** that drives NinjaTrader 8's native ATM Strategy engine from the three `gbKingPanaZilla` signals. Selecting any combination of the three signal outputs (PZ, KZ, KP) is controlled via per-signal toggles. Risk is managed by a daily profit target and daily loss limit evaluated tick-by-tick against both realized and (optionally) open equity.

**How it works:**

1. **Signal evaluation** — on each primary bar, `PanaZillia_Trade`, `KingZilla_Trade`, and `KingPana_Trade` are read from `gbKingPanaZilla`. Any enabled signal at +1 triggers a long entry; at −1 a short entry.
2. **ATM order submission** — entries are placed via `AtmStrategyCreate` with a user-selected ATM template. Only one ATM position is open at a time; new signals are ignored while a position is active.
3. **Session filters** — two independent time windows (TF1, TF2) restrict trading hours. Each window can optionally flatten all positions at its close.
4. **Risk management** — realized and unrealized PnL are tracked every tick. When the daily profit target or daily loss limit is breached, all positions are flattened and new entries are blocked for the session.
5. **Naked-position watchdog** — every 3 seconds in realtime the strategy confirms that any open position has an active ATM with working protective orders. If a naked position is detected (e.g. ATM dropped), it is immediately flattened.

**Key parameters:**

| Group | Parameter | Default |
|---|---|---|
| Signals | `UsePanaZilliaSignals`, `UseKingZillaSignals`, `UseKingPanaSignals` | all `true` |
| ATM | `AtmStrategy` | *(select from installed ATM templates)* |
| Risk | `UseDailyProfitTarget` / `DailyProfitTarget` | `false` / $500 |
| Risk | `UseDailyLossLimit` / `DailyLossLimit` | `true` / $200 |
| Risk | `UseUnrealizedPnl` | `true` |
| Session | `EnableTF1` / `StartTime1`–`EndTime1` / `FlattenTF1` | `true` / 09:30–12:00 / `true` |
| Session | `EnableTF2` / `StartTime2`–`EndTime2` / `FlattenTF2` | `true` / 13:00–15:30 / `true` |
| Session | `LogEnabled` | `false` |

**CSV Trade Log:**

Enable **Log Trades** in Session Parameters to write a trade log to the NinjaTrader user data folder. The file is named with the activation timestamp:

```
gbKPZKillah_YYYYMMDD_HHmmss.csv
```

One row is appended when each ATM position closes:

```
OpenTime,Instrument,OpenPrice,Qty,CloseTime,Trigger,Direction,AtmStrategy,RealizedPnL
2026-04-29 10:31:00,MNQ 06-26,21245.75,1,2026-04-29 10:44:22,PZ+KZ,Long,MyATM_2pt,125.00
```

| Column | Content |
|---|---|
| `OpenTime` | Bar time at signal — `yyyy-MM-dd HH:mm:ss` |
| `Instrument` | Contract name including expiry (e.g. `MNQ 06-26`) |
| `OpenPrice` | ATM entry fill price |
| `Qty` | Filled quantity |
| `CloseTime` | Tick time when position went flat |
| `Trigger` | Which signal(s) fired: `PZ`, `KZ`, `KP`, or combinations such as `PZ+KZ` |
| `Direction` | `Long` or `Short` |
| `AtmStrategy` | Name of the ATM template used |
| `RealizedPnL` | Realized P&L for the trade in account currency |

The writer is flushed after every row and closed cleanly when the strategy is removed. Logging is **off by default**.

---

## File Structure

```
KingPanaZilla/
├── gbKingPanaZilla.cs          — Composite signal indicator (loads the three child indicators)
├── gbKingOrderBlock.cs         — King Order Block indicator
├── gbPANAKanal.cs              — PANA Kanal indicator
├── gbThunderZilla.cs           — ThunderZilla indicator
├── gbKingPanaZillaKillah.cs    — ATM strategy driven by gbKingPanaZilla signals (Playr101)
├── gbBarStatus.cs              — Bar completion progress display (standalone)
└── originals/                  — Unmodified vendor source files
    ├── RenkoKings_KingOrderBlock.cs
    ├── RenkoKings_ThunderZilla.cs
    ├── ninZaPANAKanal.cs
    └── ninZaBarStatus.cs
```

Each indicator is its own file. `gbKingPanaZilla` references the three child indicators and must be compiled after them. `gbKingPanaZillaKillah` references the indicator namespace and must be compiled last.

## Installation

Copy the indicator and strategy files into their respective NinjaTrader 8 custom folders:

```
Documents\NinjaTrader 8\bin\Custom\Indicators\   ← gbKingOrderBlock.cs, gbPANAKanal.cs,
                                                      gbThunderZilla.cs, gbKingPanaZilla.cs,
                                                      gbBarStatus.cs
Documents\NinjaTrader 8\bin\Custom\Strategies\   ← gbKingPanaZillaKillah.cs
```

Then compile via **NinjaTrader → Tools → Edit NinjaScript → Compile**.

Compile order matters: the three child indicators (`gbKingOrderBlock`, `gbPANAKanal`, `gbThunderZilla`) must compile before `gbKingPanaZilla`, and `gbKingPanaZillaKillah` must compile after all indicators.

---

*GreyBeard — KingPanaZilla indicator suite | gbKingPanaZillaKillah strategy by Playr101*
