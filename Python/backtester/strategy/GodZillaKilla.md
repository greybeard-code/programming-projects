# GodZillaKilla v1.10.0 — Python port, first full-sample results

**Status: ported and running end-to-end; NOT yet NT8-parity-certified.**
The six signal engines are faithful transcriptions of the gb sources
(OnBarClose paths, NT8-exact numeric primitives), but per-engine validation
against a real chart export (§4) hasn't run yet. Treat every number below as
directionally correct, not certified. All times US/Eastern.

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

## 2. Reference preset, full 510-day sample ($2,000 floor)

`OneSet_3ofAll_BestTime` — MNQ ninZaRenko 60/3, Set 1 = 3-of-N (Equal
operators), ATM `6QTY25.50.100BE30+10TP100SL` (6 lots: 2 × T25/T50/T100,
SL100, BE+10@30), windows 07:00–11:30 / 13:01–15:30(flatten) / 18:04–21:00,
skip 09:28–09:35, daily PT $500 / LL $200.

| Config | Net | Trades | WR | PF | Sharpe | maxDD | Breach |
|---|---|---|---|---|---|---|---|
| As NT8 loads it (**3-of-6**, NC silently enabled) | **−$10,363** | 1776 | 78.0% | 0.91 | −1.29 | −$15,403 | **YES** |

Daily brackets dominated the shape of the run: the $200 loss limit fired on
**202 of ~510 days** (40%), the $500 profit target on 64. High win rate with
PF < 1 is the classic scale-out signature — many small target wins, and the
shared 100-tick stop (×6 lots ≈ $300/hit) plus $200-lock days eat them.
Sub-30s exposure is high (30.2% of trades, median hold 78 s) — r60-3 bricks
churn fast; this config would also have Apex 30-second-rule friction live.

**Interpretation (preliminary):** the preset's saved From/To span was five
days (2026-05-06 → 05-11) — this configuration looks tuned to a specific
week, and it does not generalize across the 510-day sample under the real
$2,000 trailing floor. That is precisely the question the confluence sweep
exists to answer properly (§5). An NC-disabled control (the 3-of-5 the
preset was designed as, before v1.10 added NobleCloud) is being measured to
size the silent-enable effect.

## 3. Faithfulness notes

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

## 4. Validation runbook (Phase 5 — needs the NT8 side)

1. Copy `tools/gbSignalExporter.cs` to
   `Documents\NinjaTrader 8\bin\Custom\Indicators\`, compile (F5), add
   **gbSignalExporter** to an MNQ ninZaRenko 60/3 chart (all six engines run
   at v1.10 defaults; more loaded days = better).
   → `Documents\NinjaTrader 8\export\signals_MNQ_....csv`
2. `python tools\compare_signals.py "<that csv>" --symbol MNQ --period r60-3`
   — per-engine exact/signal-bar agreement, mismatches listed. Gate: match
   the renko-parity standard (≥96% on signal bars, mismatches explainable).
3. Then a Strategy Analyzer (or Playback) run of GodZillaKilla with the same
   preset over the same dates → trade export → `tools/compare_nt8.py`.
4. Only then: promote the numbers here to certified, run MC + tearsheet.

## 5. Research next steps

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
