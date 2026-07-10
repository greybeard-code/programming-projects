# Order-flow delta filter on the Terminator champion — exploratory result

**Question:** the repo tags each trade's aggressor side, so every bar carries
`bar.delta` (buy − sell volume) and a session-cumulative `bars.cum_delta` —
data an NT8 backtest structurally cannot produce. Does gating the champion's
entries on order-flow **delta agreement** add anything?

**Method:** `strategies/terminator_delta.py` — a thin subclass of the champion
that only filters NEW entries (exits/reversals untouched); the base class is
unchanged, so `delta_mode="off"` reproduces the $22,409 baseline exactly as a
control. Full 510-day run, MNQ r100-4, $2,000 floor. All times US/Eastern.

## Result — the filter does not help (it strictly hurts)

| Entry filter | Net | Sharpe | Trades | WR | max DD |
|---|---|---|---|---|---|
| **off (champion control)** | **$22,409** | **3.90** | 990 | 33.1% | −$1,488 |
| bar delta agrees (>0) | $18,749 | 3.51 | 857 | 32.2% | −$1,467 |
| bar delta > 200 | $2,418 | 1.44 | 26 | 42.3% | −$415 |
| bar delta fades (<0) | $4,832 | 2.08 | 145 | 40.7% | — |
| cum delta agrees | $14,088 | 3.18 | 550 | 32.7% | −$1,645 |
| cum delta > 500 | $8,377 | 2.39 | 425 | 32.5% | −$1,604 |
| cum delta > 2000 | $2,589 | 1.17 | 217 | 35.0% | −$1,082 |
| cum delta fades | $8,852 | 2.39 | 442 | 34.4% | — |

Every delta-gated variant is **below the control** on net and Sharpe.
Agreement removes trades that were about average (~$27/trade), and *fading*
does not beat agreement either — so delta sign at the breakout is essentially
**noise for this strategy**, in both directions. Higher thresholds just shrink
the sample (the high-PF/26-trade cell is over-fit to a handful of trades, not
an edge). Requiring delta confirmation costs money.

## Interpretation & recommendation

The Terminator SAR already enters on a *confirmed* r100-4 renko breakout; the
aggressor delta on that bar carries no extra directional information about the
trade's outcome, so filtering on it only reduces the sample and adds variance.

**Do not bolt a delta-confirmation filter onto Terminator.** This is a clean
negative result, not a condemnation of the data edge — it says delta is not a
free *confirmation* overlay on an already-selective breakout signal. A
purpose-built order-flow strategy (delta divergence / absorption at levels,
OIB sweeps, cum-delta trend with its OWN entries) is a different hypothesis and
a separate signal-design effort; this cheap test just rules out the "add a
filter" shortcut before investing in that.

## Reproduce

```powershell
# each mode: delta_mode in {off, bar, bar_fade, cum, cum_fade}, delta_min=N
.venv\Scripts\python cli.py strategies\terminator_delta.py --mc 0 --no-report
```
