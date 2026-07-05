# Terminator_V2 — ETH Session & Time-Filter Analysis

**Config tested:** the main report's recommended engine (ATR 20 × 4.0 SAR,
200-tick hard stop, 1 contract, MNQ `r100-4`), now on the **full Globex
session** (17:00 → 15:55 CT, positions carry overnight), 2025-12-15 →
2026-06-17. Ports: `strategies/terminator_eth.py`; entry windows via
`entry_window` on `strategies/terminator_v2.py`. Evaluated 2026-07-04.
Companion reports: [TerminatorV2.md](TerminatorV2.md),
[TerminatorV2_PKfunded.md](TerminatorV2_PKfunded.md).

## 1. ETH verdict: do not trade this strategy overnight

| | RTH (08:30–15:55 CT) | ETH (17:00–15:55 CT) |
|---|---|---|
| Net P&L | **+$7,620** | **−$4,449** |
| Trades / win rate | 346 / 43% | 1,343 / 31% |
| Sharpe | 2.96 | −0.88 |
| Max drawdown | −$2,142 | −$9,097 |
| Apex $2.5K | survived | **breached 2026-01-09** |
| MC P(breach) | 6.3% | **96.2%** |

The overnight/European tape generates 4× the signals with no follow-through:
r100-4 closes print on 1-point moves, so thin-session chop crosses the trail
line constantly while real overnight trends are rare. The PK template's
default (no time filter, full ETH) is trading straight through this.

## 2. Where the money actually lives (P&L by CT entry hour, ETH run)

| Zone (CT) | Trades | Net | Verdict |
|---|---|---|---|
| 17:00–01:59 (Globex evening/Asia) | 221 | +$913 | breakeven churn — commissions eat it |
| **02:00–08:59 (Europe + US pre-open)** | **825** | **−$14,798** | the death zone; every hour negative |
| **09:00–15:59 (US day)** | **297** | **+$9,437** | every single hour positive |

Details that matter:
- Worst avg/trade hour: **08:00–08:59 CT (−$38/trade, 19% WR)** — the hour
  *containing* the 08:30 open. Aldridge's "avoid the first 30 minutes"
  (wide spreads, informed flow) shows up perfectly in the data.
- Heaviest bleeding: 04:00–05:59 CT (European cash) — 349 trades, −$6,222.
- Midday (11:00–13:59 CT) is *positive* here (+$3,950) — TSM's midday block
  applies to fast scalpers, not to this slow SAR. **No midday filter needed.**
- 19:00–20:59 CT shows +$1,673 on 30 trades — too small a sample to chase.

## 3. Recommended time filter — tested, plateau-checked, split-half-checked

Entry window **09:30–15:30 CT** on the RTH session (entries only; exits and
the 15:55 flatten unchanged):

| Entries from | Net | Sharpe | MaxDD | Trades | Apex headroom |
|---|---|---|---|---|---|
| 08:30 (baseline) | $7,620 | 2.96 | −$2,142 | 346 | $1,276 |
| 09:00 | $7,673 | 3.32 | −$1,202 | 273 | $1,578 |
| **09:30** | **$7,998** | **3.52** | −$1,675 | 243 | $1,628 |
| 10:00 | $7,830 | 3.41 | — | — | — |
| 10:30 | $6,454 | 2.87 | — | — | — |

Monte Carlo for 09:30–15:30: **P(Apex breach) = 0.7%** (from 6.3%),
**P(pass $3K eval before breach) = 97.9%**, P(profitable 6 months) = 100%
of 2,000 sims, 5th-percentile outcome +$3,491.

Anti-data-snooping checks (the window was derived from this same data):
- **Plateau:** 09:00/09:30/10:00 starts all land Sharpe 3.3–3.5 — the edge
  is the zone, not a magic minute. Degradation at 10:30 is gradual.
- **Split halves:** windowed beats baseline in the *weak* half
  (Dec–Mar: Sharpe 1.22 → 2.61) and matches it in the strong half
  (Mar–Jun: 4.15 → 4.22). The filter helps exactly when the edge is thin.
- Independently predicted by the literature (Aldridge open-spread window,
  TSM U-shaped volume) before this data was examined.

## 4. NT8 settings translation

In Terminator_V2's settings (times are your **PC/chart timezone** — the
values below assume Central; shift +1h if your machine runs Eastern):

- Use Time Filter: **true**
- Start Time: **093000** CT (= 103000 ET)
- End Time: **153000** CT (= 163000 ET)
- Flatten At Window End: **true**
- Use Time Filter 2: false (the evening pocket is not statistically worth it)
- Keep: SL Mode=Ticks, SL Value=200; ATR 20 / Mult in the 3–4 band; 1
  contract under an Apex trailing threshold.

## Reproduce

```powershell
.venv\Scripts\python cli.py strategies\terminator_eth.py --mc-target 3000
# hourly attribution: see strategy/TerminatorV2_ETH.md commit, or bucket
# reports\TerminatorETH_MNQ_r100-4_trades.csv by CT entry hour
```
