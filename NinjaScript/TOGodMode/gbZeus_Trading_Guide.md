# Zeus — God Trades Automated Strategy · Trader's Guide

A plain-English guide to the **GodTrades indicator** and the **gbZeus strategy** that trades it. Written for traders, not programmers — no code, just what it does and how to run it.

## Credits

Zeus automates other people's work. It does not originate the method or the signals:

- **The "God Trades" methodology is the work of [TraderOracle](https://www.youtube.com/watch?v=Saa2YTtIV7E).** Everything Zeus trades — candle gaps, the 3-candle validity rule, the Bollinger-band entries, the candle-back stop, the band-to-band target, the spiderweb stand-aside — comes from his teaching. **Watch the source material first:** [God Trade Masterclass](https://www.youtube.com/watch?v=Saa2YTtIV7E). Don't automate a method you haven't learned by eye.
- **The GodTrades indicator is the work of Sneaky_Zekey**, included unmodified with his permission. It does all the setup detection; Zeus only executes what it reports.
- **gbZeus** (the strategy, dashboard, risk controls) is the only part original here.

---

## The big picture

This is an automated version of TraderOracle's **"God Trades"** candle-gap method for **NQ on a 1000-tick chart**. It comes in two parts that work together:

1. **The GodTrades indicator** — the *eyes*. It watches the chart, finds the setups, and marks them (arrows, gap lines, a signal value).
2. **The Zeus strategy (`gbZeus`)** — the *hands*. It reads the indicator's signals and actually places the trades, manages the stop and target, and gives you an on-chart control panel.

The indicator finds; Zeus trades.

> **Required before anything else:** Zeus **cannot run without the GodTrades indicator, version 16.6.** Both scripts must be installed and compiled in NinjaTrader — Zeus reads the indicator's signals internally, so it doesn't have to be *added to the chart*, but it must be **present and compiled** or Zeus won't build.
>
> The indicator is the work of **Sneaky_Zekey**, included **unmodified with his permission**. **Do not edit it.** Beyond the courtesy, there's a technical reason: Zeus calls the indicator through a generated constructor whose argument list is built from the indicator's settings. That signature is **pinned to v16.6** — if you swap in a different indicator version that added, removed, or reordered a setting, **Zeus will stop compiling.** Always keep the two files together as a matched pair.
>
> If you also drop the indicator on the chart you can watch the arrows and gap lines line up with your fills; keep its Bollinger settings identical to Zeus's (20 period, 2.0 std-dev).

---

## The idea behind it

Modern markets are driven by algorithms that move price in bursts. When they rush, they **skip over prices** — you see it as a **gap between the bodies of two candles** (the close of one candle and the open of the next don't overlap). 

The theory: price tends to **come back to "fill" those gaps**, and when it does, it often **reacts** — bounces or accelerates. So each gap becomes a line on the chart worth watching. The **older** a gap line is before price returns to it, the **stronger** the reaction tends to be. A gap only "counts" once it has survived at least **3 candles** unfilled.

Zeus trades two flavors of this idea.

---

## The two setups it trades

### 1. Bollinger Gap (BG) — momentum
Two same-color candles with a gap between them, forming **right at the outer Bollinger band**. Price has stretched to an extreme and gapped — Zeus enters **in the direction of the gap** as the move kicks off. (A gap **up** off the **lower** band → **long**; a gap **down** off the **upper** band → **short**.)

### 2. Fill & Continue (FC) — the gap-fill reaction
An **older, valid gap line** (3+ candles old) gets **touched** — price returns to fill it — and then a candle **confirms** the reaction by closing back through the zone within a couple of bars. Zeus enters in the gap's direction on that confirmation.

You can turn each setup **on or off** independently.

### The "spiderweb" — when it stands aside
When **too many gap lines cluster together** near price (5 or more within a tight range), the chart is a tangled mess and reactions are unreliable. The indicator flashes a **spiderweb warning**, and Zeus will **skip new entries** until it clears. This is a feature, not a bug — it keeps you out of chop.

---

## How Zeus trades a signal

Every trade follows the same three rules — this is the heart of the method:

- **Entry:** one contract, at market, right after the signal candle closes. **One position at a time** — while in a trade, Zeus ignores new signals.
- **Stop loss:** the **far end of the signal candle** (the low for a long, the high for a short). It's a *natural* stop the market drew, so it's **small on tight candles and wider on big ones**. Zeus **never automatically slides it to break-even** — that's deliberate; the method says break-even stops kill good trades.
- **Profit target:** the **opposite Bollinger band** — and this is the key. The target is **not a fixed number of ticks**; it **moves with the band every bar**. A long aims for the upper band; as the bands shift, the target shifts with them. This lets a strong move **run band-to-band** instead of getting capped, while a stalling move gets taken near the band. *This moving target is the single most important thing Zeus does that a normal fixed bracket cannot.*

**Trading window:** by default Zeus only takes **new entries between 10:15 AM and 3:00 PM Eastern** — the meat of the US session. Gap lines still build up overnight, but entries wait for the window. Any open trade is closed when the window ends.

**What a day looks like:** a handful of trades. Winners come in **different sizes** because the band moves — some small, and occasionally a big **runner** when price trends band-to-band. Losers are the small-to-medium candle-back stops. The edge comes from **winners being bigger than losers on average**, not from a high win rate.

---

## The on-chart dashboard

When Zeus is running on a live or sim chart, a control panel appears (drag it anywhere; click the pill in the title bar to minimize). It shows:

**Status rows**
- **FLAT / LONG / SHORT** — current position
- **Instrument & account**
- **Window** — your entry hours, and whether it's *Armed*, *Outside window*, *AUTO OFF*, or *BLOCKED* (daily limit hit)
- **Exit mode & spiderweb count** — how many gap lines are clustered (STAND-ASIDE if 5+)
- **Day P&L & trade count** — realized for the session
- **While in a trade:** Entry, Stop, Target (marked *band* or *M* if you moved it manually), Qty, and live **uPnL**

**Buttons**
- **AUTO: ON/OFF** — master switch for automatic entries. Turn it off to trade purely by hand.
- **LONG: ON/OFF** and **SHORT: ON/OFF** — let Zeus take only longs, only shorts, or both (greyed out while AUTO is off)
- **MOVE SL TO BE** — manually move your stop to break-even (plus/minus your chosen offset). This is *your* call — the automation won't do it for you.
- **SL ▼ / SL ▲** and **TP ▼ / TP ▲** — nudge the live stop or target by a fixed tick step. **▲ always raises the price, ▼ always lowers it** (same for longs and shorts). Moving the target by hand turns off the automatic band-tracking for that trade.
- **FLATTEN ALL** — closes the position immediately **and pauses AUTO** (re-arm with the AUTO button).

All buttons act instantly, tick-by-tick — a FLATTEN doesn't wait for the next bar.

---

## Risk controls (guardrails)

These are **off by default** — turn them on and set your dollar amounts before they do anything.

| Control | What it does |
|---|---|
| **Daily Max Loss ($)** | Once the session's **realized** loss hits this, Zeus stops taking new trades for the rest of the day. |
| **Daily Max Profit ($)** | Same idea on the upside — lock in a good day and stop. |
| **Flatten On Daily Limit** | When a daily limit trips, also close any open position (not just block new ones). |
| **Max Trades Per Day** | Hard cap on entries per session (0 = no cap). |
| **Max Stop Ticks** | Skips any signal whose candle-back stop would be **wider than this** — keeps you out of oversized-risk trades from huge signal candles. |

**Important — how the daily limits behave:** they stop the **next** entry once the day crosses your limit. They do **not** cap a single trade that's already open. Because the stop is the back of the signal candle, one big-candle trade can lose more than your daily limit in a single shot. If you want to bound **per-trade** risk (recommended for a prop/eval account with a trailing threshold), use **Max Stop Ticks** — that's the lever that actually prevents oversized single trades.

---

## Recommended chart setup

| Setting | Value | Why |
|---|---|---|
| **Instrument** | NQ (front month) | The method is built for NQ. Use the full-size contract for signals, not MNQ. |
| **Bar type** | **1000 tick** | The method's standard chart. (500-tick is noisier and trades worse.) |
| **Chart time zone** | **Eastern (US)** | The 10:15–3:00 window is read in chart time — set it to ET or the window shifts. |
| **Trading hours** | 24-hour / Globex session template | Gap lines need the overnight session to build; only *entries* are limited to your window. |
| **Days to load** | 3–5 | So the indicator has history and warmed-up bands before the window opens. |

You don't strictly need to add the GodTrades indicator to the chart for Zeus to trade (it reads it internally), but adding it lets you **see** the arrows and gap lines line up with your fills. If you do, keep its Bollinger settings identical (20 period, 2.0 std-dev).

---

## Quick-start checklist

1. Chart: NQ, 1000-tick, **time zone Eastern**, 24h session, load 3–5 days.
2. Add the **gbZeus** strategy to the chart.
3. Set your **Daily Max Loss** (and optionally **Max Stop Ticks**) to sensible dollar/tick amounts for your account.
4. Confirm the account is your **sim** account while testing.
5. Enable the strategy. Nothing trades before **10:15 AM ET** — that's normal.
6. Watch the dashboard: the **Window** row should read *Armed* during your hours.
7. Let it run and review the executions at the end of the day.

---

## What to realistically expect

Be honest with yourself about this one:

- **It trades the method faithfully — that's what it's for.** The variable candle-back stop and the moving band target are doing exactly what the training teaches. Early sim sessions have looked clean and been modestly green.
- **It is not a proven money printer.** The numbers, plainly:
  - Over **417 trading days** of historical testing the faithful configuration came out roughly **break-even** (profit factor ~1.00), and rolling walk-forward testing showed profit factor **below 1.0 in 4 of 5 windows**.
  - The best sample we have (7 sessions, +$8.4k, profit factor 2.01) got **86% of its profit from a single day**. Excluding that one day it made **+$1,145 over six sessions** — essentially flat.
  - Monte Carlo on that sample puts the chance of breaching a **$2,000 trailing drawdown at roughly 45%** — a coin flip on a typical prop evaluation.
  
  A few good days are encouraging but are **not** proof of an edge.
- **The edge, if it holds, is in the fat tail.** Most days are small band-to-band moves; the real money is the occasional **runner** that trends from one band to the other. Win *rate* is ordinary (roughly 40–60%); winners simply need to average bigger than losers.
- **Respect the guardrails and size small.** One contract, a real daily loss limit, and (for eval accounts) a Max Stop Ticks cap. Don't scale up off a handful of green days.
- **Forward-test before you trust it.** Run it on sim across many sessions, compare the fills to what you'd expect from the chart, and only then consider risking real money — with size that assumes it might *not* work.

Treat Zeus as a disciplined execution tool for a real method — not a guarantee. Used with tight risk control, it takes the emotion and hesitation out of trading the God Trades setups. That discipline is the point.
