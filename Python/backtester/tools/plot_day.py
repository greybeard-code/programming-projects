"""Per-day trade chart: candles + entry/exit markers, for visual debugging.

    python tools\\plot_day.py strategies\\terminator_rec.py --day 2026-06-17

Runs the strategy over a short lead-in window (so indicators are warm) through
the target day, then plots that day's bars with entry/exit markers and the
entry-window shading. All times US/Eastern. Writes a self-contained HTML.
"""
from __future__ import annotations

import argparse
from datetime import datetime, timedelta
from pathlib import Path
from zoneinfo import ZoneInfo

import numpy as np
import plotly.graph_objects as go

import sys
sys.path.insert(0, str(Path(__file__).resolve().parent.parent))

from backtester.contracts import get_spec
from backtester.data import Catalog
from backtester.engine import Backtest
from backtester.loader import load_strategy
from backtester.strategy import parse_barspec

ET = ZoneInfo("America/New_York")


def _norm_day(s: str) -> str:
    s = s.replace("-", "")
    if len(s) != 8:
        raise SystemExit(f"--day must be YYYY-MM-DD or YYYYMMDD, got {s!r}")
    return s


def _et_naive(ts_ns) -> list[datetime]:
    """ns UTC -> naive datetime in ET wall clock (for an ET-labelled axis)."""
    return [datetime.fromtimestamp(int(t) / 1e9, ET).replace(tzinfo=None)
            for t in ts_ns]


def _window_shapes(date: str, windows) -> list[dict]:
    d = datetime.strptime(date, "%Y%m%d")
    shapes = []
    for w in windows:
        if not w:
            continue
        sh, sm = map(int, w[0].split(":"))
        eh, em = map(int, w[1].split(":"))
        x0 = datetime(d.year, d.month, d.day, sh, sm)
        x1 = datetime(d.year, d.month, d.day, eh, em)
        if x1 <= x0:               # overnight window: clamp to end of day
            x1 = datetime(d.year, d.month, d.day, 23, 59)
        shapes.append(dict(type="rect", xref="x", yref="paper",
                           x0=x0, x1=x1, y0=0, y1=1, line_width=0,
                           fillcolor="rgba(31,119,180,0.07)", layer="below"))
    return shapes


def main() -> None:
    ap = argparse.ArgumentParser(description="Per-day trade chart")
    ap.add_argument("strategy")
    ap.add_argument("--day", required=True, help="YYYY-MM-DD or YYYYMMDD")
    ap.add_argument("--symbol")
    ap.add_argument("--period")
    ap.add_argument("--lead-days", type=int, default=7,
                    help="calendar days of warm-up before the target day")
    ap.add_argument("--out", default=None)
    ap.add_argument("--data-root", default=None)
    args = ap.parse_args()

    day = _norm_day(args.day)
    strat = load_strategy(args.strategy)
    symbol = (args.symbol or strat.symbol).upper()
    barspec = parse_barspec(args.period or strat.period)
    spec = get_spec(symbol)

    d = datetime.strptime(day, "%Y%m%d")
    start = (d - timedelta(days=args.lead_days)).strftime("%Y%m%d")

    # run with warm-up so the target day's signals match a full run
    bt = Backtest(strat, start=start, end=day, symbol=symbol,
                  period=args.period, prop=None, progress=False,
                  data_root=args.data_root)
    res = bt.run()

    # target day's bars (reloaded from cache) for the candles
    cat = Catalog(args.data_root)
    dayl1 = cat.load_day(symbol, day)
    if len(dayl1) == 0:
        raise SystemExit(f"No data for {symbol} on {day}")
    bars = cat.load_bars(symbol, day, barspec, spec.tick_size, dayl1)
    if len(bars) == 0:
        raise SystemExit(f"No {barspec.key} bars for {symbol} on {day}")

    x = _et_naive(bars.ts_end)
    lo, hi = min(x), max(x)

    fig = go.Figure()
    fig.add_trace(go.Candlestick(
        x=x, open=bars.open, high=bars.high, low=bars.low, close=bars.close,
        name=barspec.key, increasing_line_color="#26a69a",
        decreasing_line_color="#ef5350", showlegend=False))

    # markers for trades whose entry OR exit falls inside the plotted range
    def _in_range(ts):
        t = datetime.fromtimestamp(int(ts) / 1e9, ET).replace(tzinfo=None)
        return lo <= t <= hi, t

    n_shown = 0
    for tr in res.trades:
        e_ok, e_t = _in_range(tr.entry_ts)
        x_ok, x_t = _in_range(tr.exit_ts)
        if not (e_ok or x_ok):
            continue
        n_shown += 1
        if e_ok:
            fig.add_trace(go.Scatter(
                x=[e_t], y=[tr.entry_price], mode="markers",
                marker=dict(size=11, color="#1a7f37",
                            symbol="triangle-up" if tr.direction > 0
                            else "triangle-down"),
                name="entry", showlegend=False,
                hovertext=(f"{'LONG' if tr.direction > 0 else 'SHORT'} "
                           f"{tr.qty} @ {tr.entry_price:.2f}"),
                hoverinfo="text"))
        if x_ok:
            fig.add_trace(go.Scatter(
                x=[x_t], y=[tr.exit_price], mode="markers",
                marker=dict(size=9, symbol="circle",
                            color="#1a7f37" if tr.pnl >= 0 else "#c62828",
                            line=dict(width=1, color="#333")),
                name="exit", showlegend=False,
                hovertext=(f"exit @ {tr.exit_price:.2f}  "
                           f"P&L ${tr.pnl:,.2f}  [{tr.exit_tag}]"),
                hoverinfo="text"))

    windows = [getattr(strat, "entry_window", None),
               getattr(strat, "entry_window2", None)]
    shapes = _window_shapes(day, [w for w in windows if w])

    fig.update_layout(
        title=(f"{type(strat).__name__} — {symbol} {barspec.key} — "
               f"{d:%Y-%m-%d} ET  ({n_shown} trades shown)"),
        template="plotly_white", height=720, shapes=shapes,
        xaxis=dict(title="Time (ET)", rangeslider_visible=False),
        yaxis=dict(title="Price"),
        margin=dict(l=60, r=30, t=60, b=40))

    out = Path(args.out or (Path("reports")
               / f"day_{type(strat).__name__}_{symbol}_{day}.html"))
    out.parent.mkdir(parents=True, exist_ok=True)
    out.write_text(fig.to_html(full_html=True, include_plotlyjs="cdn"),
                   encoding="utf-8")
    print(f"Wrote {out.resolve()}  ({n_shown} trades, {len(bars)} bars, "
          f"entry windows {[w for w in windows if w]})")


if __name__ == "__main__":
    main()
