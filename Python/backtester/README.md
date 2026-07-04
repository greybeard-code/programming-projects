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
    period = "1m"                    # 30s / 1m / 5m / ...
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

## Prop-firm (Apex) simulation

The trailing threshold trails the **intratrade** equity peak (unrealized
included) and, by default, locks at start balance + $100. A breach is equity
touching the floor.

```
--balance 50000 --apex-threshold 2500     # 50K account defaults
--apex-halt                               # stop the test at the breach
--apex-threshold 0                        # disable
```

The console summary reports either the breach timestamp or the minimum
headroom that survived; the tearsheet plots the floor under the equity curve.

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
