# backtester

Tick-level futures backtester for NinjaTrader Market Replay data
(Parquet, see `M:\NinjaTrader_DataRepo\RawData\Parquet\README.txt`).
Built for fast iteration on intraday prop-firm strategies before porting
them to NinjaTrader 8.

## Quick start

```powershell
.venv\Scripts\python cli.py strategies\ema_cross.py --start 2026-06-01 --end 2026-06-17
```

Produces a console summary and an HTML tearsheet in `reports\`
(equity curve with the Apex trailing floor overlaid, drawdown, daily P&L,
trade distribution, full trade list).

First touch of each day reduces the raw ~24M-event file to trade events with
prevailing bid/ask attached and caches it under `.cache\` (plus per-period bar
caches). First pass over a day costs a few seconds; cached runs are ~0.1 s/day.

## Writing a strategy

```python
from backtester import EMA, Strategy

class MyStrat(Strategy):
    symbol = "MNQ"
    period = "1m"                    # time: 30s/1m/5m; tick: 500t; renko: r8
    session = ("08:30", "15:00")     # US/Central; None = full day
    flat_at_session_end = True
    qty = 2

    def on_start(self):
        self.fast, self.slow = EMA(9), EMA(21)

    def on_bar(self, bar, bars):     # bar.open/high/low/close/volume/ts
        f, s = self.fast.update(bar.close), self.slow.update(bar.close)
        if self.slow.ready and self.flat and f > s:
            self.buy_bracket(stop_ticks=40, target_ticks=80)
```

Hooks: `on_start`, `on_bar(bar, bars)`, `on_fill(fill)`,
`on_session_end(date)`, `on_finish`.
Orders: `buy/sell` (market), `buy_bracket/sell_bracket`,
`buy_limit/sell_limit`, `buy_stop/sell_stop` (all accept
`stop_ticks`/`target_ticks` brackets), `close_position()`, `cancel_all()`.
Order management (ATM-style): `move_stop(price)`, `move_target(price)`,
`move_stop_to_breakeven(offset_ticks=)`, and `stop_order` / `target_order` /
`working_orders` for direct inspection — call from `on_bar` to trail stops.
State: `self.position`, `self.flat`, `self.avg_price`, `self.balance`.
Indicators (incremental, NT8-style): `EMA, SMA, ATR, RSI, Highest, Lowest`.

## Bar types

- **Time** — `30s`, `1m`, `5m`, `1h` (bar timestamp = close time, NT8-style;
  empty bars omitted).
- **Tick** — `500t`: fixed trade-count bars.
- **Renko (ninZaRenko)** — `r8-4`: brick size 8 ticks (every bar's body
  height), trend threshold 4 ticks (with-trend close distance from the
  previous close). `r8` defaults trend to brick/2. Implements the published
  ninZaRenko manual: open offset = brick − trend (bars overlap), reversal
  threshold = 2·brick − trend, equal bodies both directions. Manual's
  recommended configs: 8-4, 15-5, 12-4, 20-5, 30-10. High/low include the
  synthetic open — matching what NT8 indicators see on ninZaRenko bars.

Bar type only changes *when the strategy is asked to decide*. Orders always
fill against the real tick stream, so none of NT8's Renko fantasy-fill
problem applies — a Renko strategy backtested here gets honest fills.

## Order flow (what NT8 backtests can't see)

Every trade in the reduced cache is classified by aggressor side (at/above
ask = buy, at/below bid = sell), and every bar — any type — carries
`bar.buy_volume`, `bar.sell_volume`, and `bar.delta`. `bars.delta` /
`bars.cum_delta` (session-cumulative) are available as history arrays for
delta-divergence and order-flow filters. Prevailing bid/ask queue sizes are
also cached per trade for order-imbalance (OIB) research.

## Position sizing & risk

- `self.vol_target_contracts(daily_atr_points)` — Carver volatility
  targeting (15% annual default). Pass a *daily* ATR.
- `--daily-loss-limit 600` — flatten and stand down for the rest of the day
  when the day's loss touches the limit; hit days are listed in the summary.

## Parameter sweeps

```
python sweep.py strategies\ema_cross.py --param fast_period=6,9,12 ^
    --param slow_period=18,21,27 --start 2026-03-01 --end 2026-06-17
```

Runs the full grid in parallel, ranks by `--metric` (sharpe default), writes
`reports\sweep_*.csv` (columns include prop-firm min headroom), and prints a
per-parameter **sensitivity plateau**
around the best combo — a spike at one value with collapse next door is
flagged FRAGILE (data-snooping, per Chan). Combos with fewer than
`--min-trades` rank last.

## Walk-forward analysis

```
python walkforward.py strategies\ema_cross.py --param fast_period=6,9,12 ^
    --param slow_period=18,21,27 --windows 5 --ratio 5
```

Rolling IS/OOS windows (5:1 default): optimize the grid in-sample, run the
best combo out-of-sample, roll forward. Reports per-window IS vs OOS, the
stitched OOS net/Sharpe (the only numbers that haven't seen their own data),
and walk-forward efficiency with Davey's verdict (< 0.5 = likely curve-fit).

## Fill model

- Strategy logic runs on bar closes; orders resolve against the underlying
  trade-event stream inside each bar (no look-ahead — fills happen before the
  strategy sees the bar).
- Market orders fill on the next trade event at the prevailing **ask** (buy) /
  **bid** (sell), plus `--slippage` ticks if set. The spread is a real cost.
- Limit orders fill when price trades **through** the limit (touch alone never
  fills — approximates queue risk). Marketable limits fill at the quote.
- Stops trigger on last, fill at the quote, never better than the stop price.
- Commissions are per-contract round-turn defaults in
  `backtester/contracts.py` — adjust to your firm's rates.

## Prop-firm simulation

The trailing threshold (modeled on Apex's rule set) trails the **intratrade**
equity peak (unrealized included) and, by default, locks at start balance +
$100. A breach is equity touching the floor.

```
--balance 50000 --prop-threshold 2500     # 50K account defaults
--prop-halt                               # stop the test at the breach
--prop-threshold 0                        # disable
```

The console summary reports either the breach timestamp or the minimum
headroom that survived; the tearsheet plots the floor under the equity curve.

## Monte Carlo

Every run (unless `--mc 0`) resamples the closed-trade P&L 2,000× to separate
skill from ordering luck: 5/50/95th-percentile final P&L, max-drawdown
distribution, **P(breaching the Apex trailing threshold)** across orderings,
and with `--mc-target 3000` the eval race — P(hitting the target before a
breach). Block bootstrap is used automatically when trade returns are
serially correlated (|r| > 0.2, per Davey).

## CLI

```
python cli.py <strategy.py> [--symbol MNQ] [--period 1m] [--start D] [--end D]
              [--balance 50000] [--apex-threshold 2500] [--apex-halt]
              [--slippage 0] [--out report.html] [--no-report] [--data-root P]
```

Env overrides: `BACKTESTER_DATA_ROOT`, `BACKTESTER_CACHE`.

Each report also writes `<name>_trades.csv`. To validate fills against
NinjaTrader, export the same strategy's trades from NT8 Strategy Analyzer
(tick replay) and run:

```
python tools\compare_nt8.py reports\MyStrat_MNQ_trades.csv nt8_export.csv --symbol MNQ
```

It matches trades by direction + entry time and reports entry/exit price
deltas in ticks.

## Tests

```powershell
.venv\Scripts\python -m pytest tests -q
```

Covers fill semantics (market/limit/stop/bracket/OCO/reversals), account
math, bar building, and the Apex trailing/lock/halt behavior on synthetic
tick streams.

## Not yet implemented

- `on_tick` strategies (bar-driven only; tick-accurate fills already handled)
- L2 depth / queue-position simulation
- Parameter sweeps (run the CLI in a loop for now)
- Multi-symbol portfolios
