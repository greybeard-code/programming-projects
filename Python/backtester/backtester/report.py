"""Self-contained HTML tearsheet (plotly via CDN)."""
from __future__ import annotations

import html
from pathlib import Path

import numpy as np
import plotly.graph_objects as go
from plotly.subplots import make_subplots


def _dt(ts_ns) -> np.ndarray:
    return np.asarray(ts_ns, dtype="int64").astype("datetime64[ns]")


def _fmt(x, money=True) -> str:
    if x != x:   # NaN
        return "n/a"
    if isinstance(x, float) and money:
        return f"${x:,.2f}"
    if isinstance(x, float):
        return f"{x:.2f}"
    return str(x)


def write_trades_csv(result, out_path: str | Path) -> Path:
    """Trade log CSV — also the input format for tools/compare_nt8.py."""
    import csv

    out_path = Path(out_path)
    out_path.parent.mkdir(parents=True, exist_ok=True)
    with open(out_path, "w", newline="", encoding="utf-8") as f:
        w = csv.writer(f)
        w.writerow(["direction", "qty", "entry_time_utc", "exit_time_utc",
                    "entry_price", "exit_price", "pnl", "commission",
                    "mae", "mfe", "entry_tag", "exit_tag"])
        for t in result.trades:
            w.writerow([
                "long" if t.direction > 0 else "short", t.qty,
                str(np.datetime64(t.entry_ts, "ns"))[:26],
                str(np.datetime64(t.exit_ts, "ns"))[:26],
                f"{t.entry_price:.4f}", f"{t.exit_price:.4f}",
                f"{t.pnl:.2f}", f"{t.commission:.2f}",
                f"{t.mae:.2f}", f"{t.mfe:.2f}", t.entry_tag, t.exit_tag])
    return out_path


def render(result, stats: dict, out_path: str | Path, mc=None) -> Path:
    out_path = Path(out_path)

    fig = make_subplots(
        rows=3, cols=2, shared_xaxes=False,
        specs=[[{"colspan": 2}, None],
               [{"colspan": 2}, None],
               [{}, {}]],
        row_heights=[0.45, 0.2, 0.35],
        subplot_titles=("Equity curve", "Drawdown",
                        "Daily P&L", "Trade P&L distribution"),
        vertical_spacing=0.09,
    )

    t = _dt(result.equity_ts)
    eq = result.equity
    fig.add_trace(go.Scatter(x=t, y=eq, name="Equity", mode="lines",
                             line=dict(width=1.2, color="#1f77b4")), 1, 1)
    if result.apex is not None and len(result.apex_floor):
        fig.add_trace(go.Scatter(x=t, y=result.apex_floor, name="Apex floor",
                                 mode="lines",
                                 line=dict(width=1, color="#d62728", dash="dot")), 1, 1)
        if result.apex.breached and result.apex.breach_ts:
            bt = _dt([result.apex.breach_ts])[0]
            fig.add_trace(go.Scatter(
                x=[bt], y=[result.apex.breach_equity], mode="markers+text",
                marker=dict(size=11, color="#d62728", symbol="x"),
                text=["BREACH"], textposition="top center", name="Apex breach"), 1, 1)

    if len(eq):
        peak = np.maximum.accumulate(eq)
        fig.add_trace(go.Scatter(x=t, y=eq - peak, name="Drawdown",
                                 fill="tozeroy", mode="lines",
                                 line=dict(width=1, color="#ff7f0e")), 2, 1)

    days = list(result.daily_pnl.keys())
    dvals = list(result.daily_pnl.values())
    if days:
        ddates = [f"{d[:4]}-{d[4:6]}-{d[6:]}" for d in days]
        colors = ["#2ca02c" if v >= 0 else "#d62728" for v in dvals]
        fig.add_trace(go.Bar(x=ddates, y=dvals, marker_color=colors,
                             name="Daily P&L"), 3, 1)

    tpnl = [tr.pnl for tr in result.trades]
    if tpnl:
        fig.add_trace(go.Histogram(x=tpnl, nbinsx=60, name="Trade P&L",
                                   marker_color="#1f77b4"), 3, 2)

    fig.update_layout(height=900, showlegend=True, template="plotly_white",
                      legend=dict(orientation="h", y=1.04),
                      margin=dict(l=60, r=30, t=80, b=40))

    stat_rows = [
        ("Net P&L", _fmt(stats["net_pnl"])),
        ("Sharpe (daily, ann.)", _fmt(stats["sharpe"], money=False)),
        ("Sortino", _fmt(stats["sortino"], money=False)),
        ("Max drawdown", f"{_fmt(stats['max_drawdown'])} ({stats['max_drawdown_pct']:.2f}%)"),
        ("Trades", str(stats["total_trades"])),
        ("Win rate", f"{stats['win_rate']:.1f}%" if stats["win_rate"] == stats["win_rate"] else "n/a"),
        ("Profit factor", _fmt(stats["profit_factor"], money=False)),
        ("Expectancy / trade", _fmt(stats["expectancy"])),
        ("Avg win / loss", f"{_fmt(stats['avg_win'])} / {_fmt(stats['avg_loss'])}"),
        ("Commission", _fmt(stats["commission"])),
        ("Trading days", f"{stats['trading_days']} (+{stats['winning_days']} / -{stats['losing_days']})"),
        ("Best / worst day", f"{_fmt(stats['best_day'])} / {_fmt(stats['worst_day'])}"),
        ("Largest day % of profit", f"{stats['consistency_pct']:.1f}%"
         if stats["consistency_pct"] == stats["consistency_pct"] else "n/a"),
    ]
    if "apex_breached" in stats:
        if stats["apex_breached"]:
            apex_txt = f"BREACHED at {np.datetime64(stats['apex_breach_ts'], 'ns')} UTC"
        else:
            apex_txt = f"survived (min headroom {_fmt(stats['apex_min_headroom'])})"
        stat_rows.append((f"Apex trailing ({_fmt(stats['apex_threshold'])})", apex_txt))

    if mc is not None:
        stat_rows.append((f"MC ({mc.n_sims} sims, {mc.method})",
                          f"P&L 5/50/95%: {_fmt(mc.pnl_p5)} / "
                          f"{_fmt(mc.pnl_median)} / {_fmt(mc.pnl_p95)}"))
        stat_rows.append(("MC max drawdown",
                          f"median {_fmt(mc.dd_median)}, "
                          f"5%-worst {_fmt(mc.dd_p95)}"))
        if mc.prob_breach is not None:
            stat_rows.append(("MC P(Apex breach)",
                              f"{mc.prob_breach * 100:.1f}%"))
        if mc.prob_pass is not None:
            stat_rows.append((f"MC P(pass {_fmt(mc.profit_target)} eval)",
                              f"{mc.prob_pass * 100:.1f}% "
                              f"(fail {mc.prob_fail * 100:.1f}%)"))

    stats_html = "".join(
        f"<tr><td>{html.escape(k)}</td><td class='v'>{html.escape(v)}</td></tr>"
        for k, v in stat_rows)

    trade_rows = []
    for tr in result.trades:
        e = np.datetime64(tr.entry_ts, "ns")
        x = np.datetime64(tr.exit_ts, "ns")
        cls = "pos" if tr.pnl >= 0 else "neg"
        trade_rows.append(
            f"<tr><td>{'L' if tr.direction > 0 else 'S'}</td>"
            f"<td>{tr.qty}</td><td>{e}</td><td>{x}</td>"
            f"<td>{tr.entry_price:.2f}</td><td>{tr.exit_price:.2f}</td>"
            f"<td class='{cls}'>{tr.pnl:,.2f}</td>"
            f"<td>{tr.mae:,.0f}</td><td>{tr.mfe:,.0f}</td>"
            f"<td>{html.escape(tr.exit_tag)}</td></tr>")

    plot_html = fig.to_html(full_html=False, include_plotlyjs="cdn")

    doc = f"""<!DOCTYPE html>
<html><head><meta charset="utf-8">
<title>{html.escape(result.strategy_name)} — {html.escape(result.symbol)}</title>
<style>
 body {{ font-family: Segoe UI, Arial, sans-serif; margin: 20px; color: #222; }}
 h1 {{ font-size: 20px; }} h2 {{ font-size: 16px; margin-top: 28px; }}
 table {{ border-collapse: collapse; font-size: 13px; }}
 td, th {{ padding: 4px 12px; border-bottom: 1px solid #e5e5e5; text-align: left; }}
 td.v {{ font-weight: 600; text-align: right; }}
 .pos {{ color: #1a7f37; }} .neg {{ color: #c62828; }}
 .cols {{ display: flex; gap: 40px; flex-wrap: wrap; }}
 .trades {{ max-height: 480px; overflow-y: auto; border: 1px solid #ddd; }}
 .trades th {{ position: sticky; top: 0; background: #fafafa; }}
</style></head><body>
<h1>{html.escape(result.strategy_name)} — {html.escape(result.symbol)},
{result.period_s}s bars, {result.days[0]}&ndash;{result.days[-1]}</h1>
<div class="cols">
<table>{stats_html}</table>
</div>
{plot_html}
<h2>Trades ({len(result.trades)})</h2>
<div class="trades">
<table><thead><tr><th>Dir</th><th>Qty</th><th>Entry (UTC)</th><th>Exit (UTC)</th>
<th>Entry px</th><th>Exit px</th><th>P&amp;L $</th><th>MAE $</th><th>MFE $</th>
<th>Exit</th></tr></thead>
<tbody>{''.join(trade_rows)}</tbody></table>
</div>
<p style="color:#888;font-size:12px">Generated by backtester. Times UTC.
Fills: tick-level L1 (market at opposite quote, limits fill on trade-through).</p>
</body></html>"""

    out_path.parent.mkdir(parents=True, exist_ok=True)
    out_path.write_text(doc, encoding="utf-8")
    return out_path
