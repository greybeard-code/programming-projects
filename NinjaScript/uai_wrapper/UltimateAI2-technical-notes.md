# UltimateAI2 — Technical Notes

Findings from reverse-engineering the public API surface of the `UltimateAI2` indicator
(part of the licensed `UltimateScalperSuite2` product) to build
[gbUaiWrapperStrategy.cs](nt8%20code/Strategies/GreyBeard/gbUaiWrapperStrategy.cs). Everything below
was derived by reflecting on the compiled DLL — no vendor documentation or source was available.

Environment: NinjaTrader 8, build `8.1.5.2`. Live install: `Documents\NinjaTrader 8\bin\Custom\`.

## 1. Files involved

| File | Role |
|---|---|
| `UltimateScalperSuite2.dll` | Main indicator suite. Contains `UltimateAI2` plus other indicators (`UltimateAIPro`, `StochasticFull`, `WiZigZagHighLow`, etc). |
| `UltimateScalperSuite2_Telegram@Val1312q.dll` | Telegram alert add-on. Fully obfuscated (see §2); not needed for this task and not investigated further. |

Both are dropped directly into `bin\Custom` (not `bin\Custom\Indicators`) — this turned out to matter (§4).

## 2. Code protection

Both DLLs are wrapped in an **Agile.NET / CliSecure-style protector** (`<AgileDotNetRT>` type
present in both assemblies' metadata). Practical consequences:

- `Assembly.LoadFile()` + `Assembly.GetTypes()` on the raw DLL **in isolation** (e.g. from a
  standalone PowerShell process) throws `ReflectionTypeLoadException` for a large fraction of
  types, and `ReflectionOnlyLoadFrom` crashes the host process with `StackOverflowException` if
  the `AssemblyResolve`/`ReflectionOnlyAssemblyResolve` handler isn't carefully cached (naive
  handlers re-enter and blow the stack).
- Loading the **live, already-installed copy** from `Documents\NinjaTrader 8\bin\Custom\` (i.e.
  the exact file NinjaTrader itself runs, with all sibling assemblies resolvable from the same
  directory plus `Program Files\NinjaTrader 8\bin`) avoids the partial-load problem — full,
  successful `GetTypes()` only worked once resolution matched NinjaTrader's own runtime
  environment. Reflecting on the copy sitting in this repo's `nt8 code\` folder in isolation did
  **not** work reliably.
- The `UltimateScalperSuite2_Telegram@Val1312q.dll` companion is protected so heavily that even
  its type *names* are obfuscated garbage (`lc0=.lM0=`, etc.) — it was not usable for reflection
  at all and wasn't needed.

Working PowerShell pattern (Windows PowerShell / .NET Framework, not PowerShell Core — pwsh
choked on these assemblies with "Enclosing type(s) not found" errors):

```powershell
$ntBin     = "C:\Program Files\NinjaTrader 8\bin"
$customBin = "C:\Users\<user>\Documents\NinjaTrader 8\bin\Custom"
$cache = @{}
[System.AppDomain]::CurrentDomain.add_AssemblyResolve({
    param($sender, $e)
    $name = ($e.Name -split ",")[0]
    if ($cache.ContainsKey($name)) { return $cache[$name] }
    $cache[$name] = $null
    foreach ($dir in @($customBin, $ntBin)) {
        $candidate = Join-Path $dir ($name + ".dll")
        if (Test-Path $candidate) {
            try { $a = [System.Reflection.Assembly]::LoadFrom($candidate); $cache[$name] = $a; return $a } catch { return $null }
        }
    }
    return $null
})
$asm = [System.Reflection.Assembly]::LoadFrom((Join-Path $customBin "UltimateScalperSuite2.dll"))
$t = $asm.GetType("NinjaTrader.NinjaScript.Indicators.UltimateAI2")
```

The `$cache` hashtable is required — without memoizing resolved assemblies, the resolver
re-enters on repeated lookups of the same name and the process stack-overflows.

## 3. `UltimateAI2` class identity

- Full name: `NinjaTrader.NinjaScript.Indicators.UltimateAI2`
- Base type: `NinjaTrader.NinjaScript.Indicators.Indicator` (standard NinjaScript indicator base — nothing unusual here)
- Assembly: `UltimateScalperSuite2.dll`

## 4. Public API surface

### 4.1 Constructor-eligible properties (`[NinjaScriptProperty]`)

These are the properties NinjaTrader's codegen includes in an indicator's generated
factory-method parameter list, **in class declaration order** (confirmed empirically — see §5).
Order attribute values (`Display(Order=N)`) are UI-grid-only and do **not** determine codegen
order; declaration order does.

| # | Property | Type | Default | Display Name | Group |
|---|---|---|---|---|---|
| 1 | `pmBuffer_upSignal_Offset` | `int` | `4` | — | — |
| 2 | `pmBuffer_dnSignal_Offset` | `int` | `4` | — | — |
| 3 | `pmBuffer_long_Offset` | `int` | `6` | — | — |
| 4 | `pmBuffer_short_Offset` | `int` | `6` | — | — |
| 5 | `pmAlerts_upSignal_Enable` | `bool` | `true` | "upSignal" | Alerts |
| 6 | `pmAlerts_upSignal_File` | `string` | `...\sounds\Alert2.wav` | "upSignal Sound" | Alerts |
| 7 | `pmAlerts_dnSignal_Enable` | `bool` | `true` | "dnSignal" | Alerts |
| 8 | `pmAlerts_dnSignal_File` | `string` | `...\sounds\Alert2.wav` | "dnSignal Sound" | Alerts |
| 9 | `pmAlerts_long_Enable` | `bool` | `true` | "long" | Alerts |
| 10 | `pmAlerts_long_File` | `string` | `...\sounds\Alert2.wav` | "long Sound" | Alerts |
| 11 | `pmAlerts_short_Enable` | `bool` | `true` | "short" | Alerts |
| 12 | `pmAlerts_short_File` | `string` | `...\sounds\Alert2.wav` | "short Sound" | Alerts |

The naming of the Alerts group (`upSignal` / `dnSignal` / `long` / `short`) is the strongest
signal (no pun intended) tying `pmAlerts_long_*` / `pmAlerts_short_*` to the `longS` / `shortS`
output series below, as opposed to the separate `upSignal` / `dnSignal` series.

### 4.2 Cosmetic properties (excluded from codegen)

Not `[NinjaScriptProperty]`-tagged — settable only via the Properties panel, not via code:

- `pmBuffer_long_Stroke_stroke` (`Brush`) + `pmBuffer_long_Stroke_strokeColorSerialize` (`string`, XML-serialization helper for the Brush)
- `pmBuffer_Long_WithDot_Stroke_stroke` (`Brush`) + `...strokeSerialize`
- `pmBuffer_short_Stroke_stroke` (`Brush`) + `...strokeSerialize`
- `pmBuffer_Short_WithDot_Stroke_stroke` (`Brush`) + `...strokeSerialize`
- `DisplayName` (`string`), `Version` (`string`, read-only)

### 4.3 Output series (`Series<double>`, `Browsable(false)`, runtime-only)

| Series | Likely meaning |
|---|---|
| `longS` | **Long signal.** `0` normally (not `NaN`); set to a price value for exactly one bar when a long signal fires, then back to `0` the next bar. Confirmed via `Print`-based debug logging against the live chart. Used by `gbUaiWrapperStrategy`. |
| `shortS` | **Short signal.** Same shape as `longS` (single-bar pulse, `0` sentinel), opposite direction. Used by `gbUaiWrapperStrategy`. |
| `upSignal` | A *different* signal type (separate offset/alert config from long/short — see §4.1). Not used by the wrapper. |
| `dnSignal` | Down counterpart to `upSignal`. Not used. |
| `BUY` / `SELL` | Purpose not determined — not investigated, not needed for this task. |
| `Colorbars` / `EnhancedLines` / `botLine` / `topLine` | Visual/channel-line series — not investigated. |

## 5. The missing factory method (root cause of the CS1955 compile error)

Referencing an indicator from your own NinjaScript source normally looks like
`myVar = SomeIndicator(param1, param2, ...)`. This convenience syntax is **not** part of the
indicator class itself — NinjaTrader's `NinjaScriptGenerator` auto-writes it as a
`#region NinjaScript generated code` block, appended to the bottom of the indicator's own `.cs`
**source file**, which extends the partial `Indicator`, `Strategy`, and `MarketAnalyzerColumn`
classes with matching overloads.

**This generation only happens for indicators compiled from source sitting in
`bin\Custom\Indicators`.** `UltimateAI2` ships as a pre-compiled DLL dropped straight into
`bin\Custom` (not the `Indicators` subfolder, and with no companion `.cs` file) — so this step
never ran for it. Exhaustively searching every member literally named `UltimateAI2` across both
`UltimateScalperSuite2.dll` and the user's own compiled `NinjaTrader.Custom.dll` turned up
**nothing** except the bare class — confirming no wrapper exists anywhere, pre-compiled or
generated.

This was confirmed by disassembling (`ildasm`) the equivalent generated method for `UltimateMA`
(a *different* indicator that does ship with source, for direct comparison). Its compiled IL
shows:

```
Strategy.UltimateMA(...)
  -> loads private field `Strategy.indicator` (type NinjaTrader.NinjaScript.Indicators.Indicator)
  -> calls indicator.UltimateMA(Input, ...)
       -> which internally calls the protected generic
          IndicatorBase.CacheIndicator<T>(T indicator, ISeries<double> input, ref T[] cache)
```

`CacheIndicator<T>` (constraint: `where T : IndicatorBase`) lives on
`NinjaTrader.NinjaScript.IndicatorBase` in `NinjaTrader.Core.dll` — it's the actual mechanism
behind every indicator factory call in NinjaScript; the generated per-indicator methods are just
thin, mechanically-identical wrappers around it (dedupe/cache against previous calls with
identical parameters, then construct + register a new instance).

**Fix applied:** hand-wrote the missing generated-code block for `UltimateAI2`
([nt8 code/Indicators/UltimateAI2.WrapperMethods.cs](nt8%20code/Indicators/UltimateAI2.WrapperMethods.cs)),
following the `UltimateMA` pattern exactly, using the 12-parameter list from §4.1 in declaration
order. This is a standard, forum-documented workaround for indicators whose generated region is
missing or wasn't produced.

## 6. Pitfall: `0`, not `NaN`, is the "no signal" sentinel

Initial versions of the wrapper strategy assumed `longS`/`shortS` used `double.NaN` as their
idle value (the common convention for NinjaScript signal series). That assumption was wrong and
produced two different failure modes before being caught:

- Checking `!double.IsNaN(longS[1])` alone: **true on every bar** (since `0` is not `NaN`) →
  39,340 trades in a ~70-day backtest, net loss ~$513k, purely from constant no-signal entries.
- Adding a same-wrong-assumption edge filter (`!IsNaN(longS[1]) && IsNaN(longS[2])`): **never
  true** (since it's never actually `NaN`) → 0 trades.

Ground truth was confirmed by adding a temporary `Print()` of `longS[0]`/`shortS[0]`/`longS[1]`/
`shortS[1]` on every bar where either was non-`NaN`-*looking*, then eyeballing the NinjaScript
Output log: the idle value prints as literal `0`, and each signal is a genuine one-bar pulse
(price value on the signal bar, `0` immediately before and after). The Data Box corroborates
this — it shows `n/a` for idle bars because NinjaTrader's Data Box treats `0` on these
Browsable-false plot series as "no value" for display purposes, which is what made the sentinel
easy to mis-read as `NaN` from the UI alone.

**Takeaway:** don't assume `NaN` semantics for a third-party indicator's output series without
verifying — the Data Box display convention can be misleading. Temporary `Print()`-based logging
of raw values across a range of bars is a fast, reliable way to confirm actual sentinel/pulse
behavior before wiring signal logic against it.

## 7. Live signal values do not reproduce on historical recalculation

Confirmed 2026-07-13: a `LONG ENTRY` fired live off `uai2.longS[1] = 29475.25` at the 9:33 PM
1-minute NQ bar (captured via a temporary `Print()` in `OnBarUpdate`, cross-checked against the
chart's visible triangle at the time). Scrolling back to that exact bar afterward and reading the
Data Box shows `long = n/a` for the same bar — the indicator now computes no signal there at all.

**Implication:** `UltimateAI2` (Setup: "Calculate: On each tick") appears to produce different
output for the same closed bar depending on whether it's processing live ticks incrementally in
real time, versus being recalculated in one historical batch pass (which is how a standard
Strategy Analyzer "Backtest" run works). The live/real-time value is the one that actually drove
the trade and should be trusted; the batch-recalculated historical value should not be.

**Consequence:** standard Strategy Analyzer **Backtest** runs against this strategy are not
reliable — they may show substantially different (often far fewer, or wildly more, depending on
what else is confounded) signals than what the strategy would actually see live. This is a
plausible *additional* contributor to the extreme 39,340-trades / 0-trades swings seen earlier in
development, on top of the already-identified `0`-vs-`NaN` sentinel bug (§6).

**Recommended validation path going forward:** use NinjaTrader's **Replay** feature (tick-by-tick
historical replay) rather than a standard Backtest run, since Replay feeds bars through
incrementally the way a live session does, and should reproduce the same values the strategy
actually saw in real-time trading. Not yet verified whether Replay mode fully resolves this —
worth confirming before relying on any Replay-based backtest numbers either.

### 7.1 Upgraded finding: this is live repainting, not just a batch-vs-live discrepancy

Confirmed 2026-07-13, same session: a `LONG ENTRY` fired live off `uai2.longS[0] = 29388.75` at
the 10:23:00 PM bar (Bar=5782). Checking the Data Box on that *exact* bar moments later — no
reload, no historical recalculation, still the same live real-time chart — showed `long = n/a`.

This rules out "historical batch recalculation differs from live" as the *only* explanation.
`UltimateAI2` revises its own computed signal values **during live, real-time progression**, not
only when NinjaTrader recomputes history from scratch. It most likely uses some forward-looking
confirmation logic internally (e.g. requiring subsequent price action to "confirm" a signal
before letting it stand) and retroactively clears signals that don't hold up — classic
repainting-indicator behavior.

**Implication for live trading:** this is not a strategy bug and is not fixable in strategy code
— a live system can only ever act on a value as it exists at the moment of decision, and by
definition cannot know in advance which signals will later be revised away. Some "false start"
entries on signals that get repainted moments later are an inherent cost of trading against this
signal source in real time.

**Implication for backtesting:** this actually strengthens the Replay-over-Backtest recommendation
above. A standard Backtest computes all of history in one hindsight-aware pass, so it likely shows
only the *final, already-revised* state for every bar — systematically different from, and
probably showing fewer signals than, what live trading actually acts on. Replay, since it
processes bars/ticks incrementally without hindsight, should be much more likely to reproduce the
same repainting behavior live trading experiences, and is therefore the more trustworthy
validation path for this specific indicator.

**Mitigation added:** `gbUaiWrapperStrategy` now exposes a `ConfirmationBars` parameter (default
`1`). Rather than acting on `longS[0]`/`shortS[0]` immediately, the strategy reads
`longS[ConfirmationBars]`/`shortS[ConfirmationBars]` — re-fetching whatever the *current* value is
for that bar, N bars later. This does double duty: it delays entry by N bars, and it re-validates
that the signal wasn't repainted away in the meantime (a repainted value reads back as `0` and the
trade is skipped). `ConfirmationBars = 0` reverts to immediate/unconfirmed reaction on the signal
bar's own close.

## 8. Open questions / things not investigated

- Semantics of `BUY`, `SELL`, `Colorbars`, `EnhancedLines`, `botLine`, `topLine` series.
- Full behavior of `UltimateAIPro` (a related, larger indicator in the same DLL) — out of scope, not touched.
- Whether `upSignal`/`dnSignal` are meant to be traded independently of `longS`/`shortS`, or are a secondary/confirmation signal.
