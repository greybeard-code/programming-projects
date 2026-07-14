# GreyBeard — Typical NinjaTrader 8 Conventions

Reference doc for what to add to a GreyBeard-branded NT8 indicator or strategy by default,
independent of whatever that script's specific trading logic is. Point Claude at this file at the
start of a new NinjaScript project so it applies these without being asked each time.

Derived from reading actual GreyBeard source (`gbBarStatus.cs`, `gbThunderZilla.cs`,
`gbKingOrderBlock.cs`, `gbNobleCloud.cs`, `gbPANAKanal.cs`, `gbSumoPullback.cs`,
`gbSuperJumpBoost.cs`), plus `Terminator_V2.cs` and `Playr101/GodZillaKilla.cs` for the dashboard
pattern — not from a separate style guide (none exists as of 2026-07-14; this doc is now that
reference).

## 1. Namespace & file naming

- Namespace: `NinjaTrader.NinjaScript.Indicators.GreyBeard` / `NinjaTrader.NinjaScript.Strategies.GreyBeard`
- File name: `gb` prefix — `gbSomethingName.cs`. (Observed inconsistently in older strategy files
  like `Terminator_V2.cs`, `GodZillaKilla.cs`, and this session's `UaiWrapperStrategy.cs` — those
  predate/didn't follow the convention. Treat `gb`-prefixed as the standard for new files.)
- Both indicators and strategies live under a `GreyBeard` subfolder of `bin\Custom\Indicators` /
  `bin\Custom\Strategies`.

## 2. Developer properties section (indicators AND strategies)

Read-only, informational, no `[NinjaScriptProperty]` (not user-editable, not serialized) —
expression-bodied getters:

```csharp
[Display(Name = "Author",  Order = 0, GroupName = "0. Developer")]
public string Author => "GreyBeard";

[Display(Name = "Version", Order = 1, GroupName = "0. Developer")]
public string Version => "1.0.0";

[Display(Name = "Website", Order = 2, GroupName = "0. Developer")]
public string Website => "https://greybeardconsulting.net/";
```

Website is optional per-project (some scripts omit it) — include it unless told otherwise.

**Group ordering gotcha:** NT8's Properties grid does not reliably order groups by source
declaration order. Prefix every `GroupName` with a number to force deterministic order — the
Developer group should be `"0. Developer"` so it always sorts first, with every other group
numbered after it (`"1. Risk Management"`, `"2. Time Filter"`, etc). This is the actual mechanism
`Terminator_V2.cs` uses (`"11. Misc"`, `"12. Dashboard"`, `"13. Plots"`) — adopt it everywhere, not
just for scripts with many groups.

## 3. Strategies get a full on-chart dashboard + control panel

Every GreyBeard strategy gets a draggable, minimizable, corner-anchored WPF dashboard — not just
an info readout, the **full control-button suite** by default (trim only if a specific button
genuinely doesn't apply to that strategy's model):

- **Info rows**: instrument/account, position status (FLAT/LONG/SHORT), relevant strategy
  state (whatever settings matter for that script), and — only visible while in a position —
  Entry / Stop / Target / Qty / uPnL.
- **Buttons**:
  - `AUTO: ON/OFF`, `LONG: ON/OFF`, `SHORT: ON/OFF` — per-direction auto-entry toggles (LONG/SHORT
    buttons disabled/greyed while AUTO is off)
  - `REV` — manual reverse
  - `MOVE SL TO BE` — moves the live stop to entry ± a signed tick offset
  - `SL ▼` `SL ▲` `TP ▼` `TP ▲` — nudge the live stop/target price by a fixed tick step per click.
    **▲ always raises the price, ▼ always lowers it** — same convention for long and short, not
    "tighten/widen". SL pair themed red, TP pair themed green.
  - `FLATTEN ALL` / `CLOSE ALL` — closes the position immediately and pauses AUTO (re-arm via the
    AUTO button)
- **Dashboard-level properties** (own numbered group, e.g. `"N. Dashboard"`):
  ```csharp
  public enum GbPanelCorner { TopLeft, TopRight, BottomLeft, BottomRight }

  [NinjaScriptProperty] public bool ShowDashboard { get; set; }          // default true
  [NinjaScriptProperty] public GbPanelCorner DashboardCorner { get; set; } // default TopLeft
  [NinjaScriptProperty] public bool DashboardStartMinimized { get; set; } // default false
  ```
- **Trade-management properties** (in the Risk Management group):
  ```csharp
  [Range(1, 500)]
  public int ManualNudgeTicks { get; set; }     // default 4 -- SL/TP button step size

  [Range(-500, 500)]
  public int ManualBeOffsetTicks { get; set; }  // default 0 -- signed BE offset; negative = lock in a small loss
  ```
  These are the standardized names (GodZillaKilla's convention) — use them verbatim rather than
  inventing project-specific names like `BreakEvenTicks`/`NudgeTicks`.

### Dashboard mechanics (copy this pattern, don't reinvent it)

- Only created in `State.Realtime` (`if (ShowDashboard) CreateDashboard();`), torn down in
  `State.Terminated`. Never shown during Strategy Analyzer backtests.
- `Border` (outer panel, rounded corners, 2px border) → `StackPanel` (title bar + body) →
  title bar is a `Grid` with a `Thumb` (drag handle, transparent background templated to the
  Border's background so the whole bar is draggable) + centered `TextBlock` title + a small
  minimize toggle (`Border` + `TextBlock` showing `−`/`+`, click toggles body `Visibility`).
- Corner anchoring: position via a one-shot `LayoutUpdated` handler that reads `ActualWidth`/
  `ActualHeight` once laid out, computes the margin for the chosen corner, then unsubscribes
  itself. After that, position is driven by manual drag (`Thumb.DragDelta` adjusting `Margin`,
  clamped to the chart bounds).
- Palette: `static readonly SolidColorBrush` fields, built via a `MakeFrozen(a,r,g,b)` helper that
  calls `.Freeze()` — never allocate brushes per-update, only reuse frozen statics.
- Buttons use a hand-built `ControlTemplate` (`FrameworkElementFactory` + `TemplateBindingExtension`
  for Background/BorderBrush/BorderThickness/Foreground/FontSize/FontWeight, a semi-transparent
  hover overlay `Trigger` on `IsMouseOver`, and an opacity `Trigger` on `IsEnabled=false`) —
  **not** a bare `Button`, which carries NT8's default theme padding/chrome and looks
  inconsistent against the dark panel.
- **Manual command pattern (critical — don't call order APIs from the UI thread):**
  1. Button `Click` handlers run on the WPF UI thread. They ONLY set/accumulate flags —
     `volatile bool _pendingBE`, `int _pendingSlNudgeTicks` via
     `Interlocked.Add(ref _pendingSlNudgeTicks, ±ManualNudgeTicks)` — never touch `SetStopLoss`,
     `EnterLong`, etc. directly.
  2. A `ProcessManualCommands()` method on the data/strategy thread atomically drains them
     (`Interlocked.Exchange(ref field, 0)`) and does the actual work.
  3. Call `ProcessManualCommands()` from **`OnMarketData`** (tick-level), not just `OnBarUpdate` —
     a manual FLATTEN or BE move must act immediately, not wait for the next bar close (which
     could be a minute+ away).
  4. If BE and an SL nudge land in the same drain window, BE wins and the SL nudge is dropped
     (ambiguous otherwise); a TP nudge still applies regardless.
  5. Validate the resulting stop/target price is still on the correct side of the market before
     submitting (long stop below market, short stop above; long target above, short target below)
     — silently skip with a `Print` if not, rather than submitting a guaranteed-reject order.

### Stop/target management gotchas

- To move an **already-open** position's live stop/target to a specific price, call
  `SetStopLoss(signalName, CalculationMode.Price, price, false)` /
  `SetProfitTarget(signalName, CalculationMode.Price, price)` — the signal-name overload updates
  the live order tied to that entry. This requires giving entries **explicit signal names**
  (`EnterLong(qty, "MyLong")`, not the anonymous default) so later calls can target them.
- **Never call `SetTrailStop` once `SetStopLoss` is in play — NT8 silently ignores it.** This is a
  documented gotcha from `Terminator_V2.cs`; all stop movement (BE, trailing, manual nudge) must
  go through repeated `SetStopLoss(..., CalculationMode.Price, ...)` calls instead.
- Automatic stop moves (BE trigger, trailing) should generally be tighten-only — never drag a
  stop backward into more risk. A manual override button is the one place that's allowed to be an
  explicit exception, since the user is intentionally overriding.

## 4. Other conventions observed

- `SetProfitTarget`/`SetStopLoss` base distances (the "default" bracket every entry gets) are set
  once in `State.Configure`, in ticks. Manual dashboard nudges then override the specific live
  order's price for that entry via the signal-name overload — the two mechanisms compose rather
  than conflict.
- `StopTargetHandling = StopTargetHandling.PerEntryExecution` is the standard setting when using
  this stop/target pattern.
- Alert sound properties typically pair a `bool ...Enable` with a `string ...File` (sound file
  path), sometimes with a master on/off toggle controlling several at once.

## 5. Open items / things to confirm per-project

This doc reflects what's been observed in existing source, not an exhaustive spec. Things that
may still need a case-by-case decision when starting a new script:
- Whether AUTO/LONG/SHORT toggles make sense for a given strategy's entry model (a strategy with
  only one entry direction, for instance, wouldn't need both LONG and SHORT toggles).
- Whether to carry over a versioned changelog comment block at the top of the file
  (`Terminator_V2.cs` has an extensive one) — not yet confirmed as a hard requirement.
