# GodZillaKilla v1.10.0 — Python port, validation + full-sample results

**Status: signal engines validated against a real NT8 export (5 of 6 pass
cleanly); trade-list compare still pending.** All times US/Eastern.

## 1. What was ported

| Piece | Where |
|---|---|
| Source snapshot (v1.10.0 + 6 indicators + templates) | `nt8 code/GodZillaKilla/` |
| ATM + strategy template XML parsers | `backtester/nt8config.py` |
| Multi-bracket ATM engine (scale-out, auto-BE, tiered trail) | `backtester/atm.py`, broker `StopPlan` |
| Six Signal_Trade engines (KO/PA/TH/SJ/SU/NC) | `backtester/gbsignals/` |
| Strategy (2 trigger sets, operators, require veto, ConfirmationBars, EMA filter, TF windows + skip, reversal, `from_template`) | `strategies/godzilla_killa.py` |
| Engine daily profit target (tag `dpt`) + loss limit from strategy attrs | `backtester/engine.py` |
| Confluence sweep (subsets × count × requires league table) | `sweep_confluence.py` |

Excluded by design: News filter, HUD/buttons/audio/markers (arm buttons
modeled always-on), martingale (v1), Signal_State/zone plots.

## 2. Signal-engine parity vs a real NT8 export (2026-07-11)

`tools/gbSignalExporter.cs` on an MNQ ninZaRenko 60/3 chart, 2026-04-30 →
2026-05-19 (61,682 bars), compared via `tools/compare_signals.py`.

**First pass surfaced a repo-wide bar-construction bug, not a signal-port
bug.** Bar OHLC parity was only 61.1% identical (vs. 96–100% for the five
previously-validated renko settings) — root cause: `build_renko_bars`
unconditionally reset its brick anchor at the start of every calendar-day
*file*, but an overnight session (18:00–16:55 ET, this preset and the
Terminator champion both use it) trades straight through midnight ET with
no real gap there. Confirmed by hour-of-day mismatch rate: 0–7% right after
the real 17:00–18:00 halt reset, jumping instantly to 45–69% at midnight ET
and staying broken until the next real halt. Fixed by threading the brick
anchor across day-file boundaries (`Catalog.load_bars_sequence`, reset only
on a genuine >30min gap) — see `backtester/data.py`. Bar parity after the
fix: **99.8% identical OHLC**.

Signal parity on the corrected bars (60,709 matched bars, first 500 skipped
as warmup):

| Engine | Exact | On signal bars |
|---|---|---|
| KO (order blocks) | 99.99% | **98.5%** |
| PA (Keltner) | 99.99% | **98.9%** |
| SJ (zones) | 99.97% | **98.7%** |
| SU (multi-MA) | 99.99% | **97.1%** |
| NC (cloud) | 99.99% | **97.3%** |
| TH (trend/OBOS) | 99.34% | 82.1% |

Five of six engines pass at the same bar the renko geometry itself was held
to (≥96%). **TH has one isolated residual**: broken down by signal code,
every TH code matches at 96.5–98.9% *except* the OBOS overbought-exit code
(`-2`, "UptrendSlowdown"), which matches only 13.8% — while its mirror code
(`+2`, oversold-exit) matches 98.9%. Root cause narrowed to MFI's
overbought-detection rate being far lower in our port than in NT8's real
data (all-three-oscillators-overbought triggers on ~2% of bars here vs. an
implied much higher rate in NT8's actual `-2` frequency), but not yet
confirmed — the formula, parameter order (`Stochastics(Input,14,7,3)` =
periodD/periodK/smooth), and volume feed were all checked against the C#
source and match. Needs raw MFI/RSI/Stochastic values from NT8 (a small
addition to the exporter) to pin down further; not blocking, since it's one
signal code in one engine.

## 3. Reference preset, full 510-day sample ($2,000 floor)

`OneSet_3ofAll_BestTime` — MNQ ninZaRenko 60/3, Set 1 = 3-of-N (Equal
operators), ATM `6QTY25.50.100BE30+10TP100SL` (6 lots: 2 × T25/T50/T100,
SL100, BE+10@30), windows 07:00–11:30 / 13:01–15:30(flatten) / 18:04–21:00,
skip 09:28–09:35, daily PT $500 / LL $200. Numbers below use the
renko-fix-corrected bars (§2).

| Config | Net | Trades | WR | PF | Sharpe | maxDD | Breach |
|---|---|---|---|---|---|---|---|
| As NT8 loads it (**3-of-6**, NC silently enabled) | **−$10,411** | 1796 | 77.7% | 0.91 | −1.26 | −$15,734 | **YES** |
| As designed (**3-of-5**, NC off — control) | −$19,246 | 1518 | 76.2% | 0.82 | −2.59 | −$21,390 | YES |

(Pre-fix numbers, on the broken bars, were −$10,363/−$16,246 respectively —
close on the 3-of-6 line, further off on 3-of-5, since the six-engine
confluence vote is naturally more sensitive to exact bar geometry than a
single-signal-line strategy. Conclusion is unchanged either way.)

The NC silent-enable is not the problem — the extra NobleCloud vote actually
*improved* the preset by ~$8.8k. Both variants lose decisively across the
full sample.

Daily brackets dominated the shape of the run: the $200 loss limit fired on
**203 of ~510 days** (40%), the $500 profit target on 66. High win rate with
PF < 1 is the classic scale-out signature — many small target wins, and the
shared 100-tick stop (×6 lots ≈ $300/hit) plus $200-lock days eat them.
Sub-30s exposure is high (30.5% of trades, median hold 77 s) — r60-3 bricks
churn fast; this config would also have Apex 30-second-rule friction live.

**Interpretation:** the preset's saved From/To span was five days
(2026-05-06 → 05-11) — this configuration looks tuned to a specific week,
and it does not generalize across the 510-day sample under the real $2,000
trailing floor. That is precisely the question the confluence sweep exists
to answer properly (§5).

## 4. Faithfulness notes

- Missing template props = compiled v1.10 defaults, exactly like NT8 —
  which is why NC ends up enabled on this pre-NC preset. The run header
  prints the effective signal set so this is never silent here.
- Entries queue on bar close and fill on the next tick (engine fast path =
  GZK's 1-tick execution series). Windows gate NEW entries only; the
  per-window Flatten flags force-flat at window close; the session template
  ("18:00"–"16:55") enforces flat-by-16:55, as in NT8.
- Fills resolve on real ticks — NT8's renko fantasy-fill problem does not
  apply. Position cap (6 minis) auto-enforced.
- Daily P&L check uses equity (realized + unrealized) = GZK's
  UseUnrealizedPnl=true default.

## 5. Remaining validation

1. ~~Signal-engine parity~~ DONE (§2) — 5/6 pass, TH has one isolated
   residual (tracked, not blocking).
2. Still needed: a Strategy Analyzer (or Playback) run of GodZillaKilla with
   the same preset over the same dates → trade export → `tools/compare_nt8.py`
   for a full trade-list certification.

## 6. Research next steps

```powershell
.venv\Scripts\python sweep_confluence.py strategies\godzilla_killa.py `
  --template "nt8 code\GodZillaKilla\templates\OneSet_3ofAll_BestTime.xml" `
  --min-size 2 --requires none
```
192 combos (6 sources), multiprocess; ranks which engines / how many / which
required actually carry edge across the full sample instead of one week.
Worth adding after that: ATM-template comparison (the ladder vs simpler
brackets), window sweep, and slippage stress on any surviving combo.

## Reproduce

```powershell
.venv\Scripts\python -c "import importlib.util,sys; sys.path.insert(0,'.'); \
sp=importlib.util.spec_from_file_location('g','strategies/godzilla_killa.py'); \
m=importlib.util.module_from_spec(sp); sp.loader.exec_module(m); \
from backtester import Backtest, PropFirmConfig, metrics; \
s=m.GodZillaKilla.from_template(r'nt8 code/GodZillaKilla/templates/OneSet_3ofAll_BestTime.xml'); \
print(metrics.compute(Backtest(s, prop=PropFirmConfig(threshold=2000.0)).run())['net_pnl'])"
```
