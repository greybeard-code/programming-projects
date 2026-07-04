"""Performance metrics from a backtest Result."""
from __future__ import annotations

import numpy as np

TRADING_DAYS = 252


def compute(result) -> dict:
    trades = result.trades
    pnl = np.array([t.pnl for t in trades]) if trades else np.array([])
    wins = pnl[pnl > 0]
    losses = pnl[pnl < 0]

    daily = np.array(list(result.daily_pnl.values()))
    daily_ret = daily / result.start_balance if len(daily) else np.array([])

    sharpe = sortino = float("nan")
    if len(daily_ret) > 1 and daily_ret.std(ddof=1) > 0:
        sharpe = float(daily_ret.mean() / daily_ret.std(ddof=1)
                       * np.sqrt(TRADING_DAYS))
    if len(daily_ret) > 1:
        downside = daily_ret[daily_ret < 0]
        if len(downside) > 1 and downside.std(ddof=1) > 0:
            sortino = float(daily_ret.mean() / downside.std(ddof=1)
                            * np.sqrt(TRADING_DAYS))

    # max drawdown on the bar-close equity curve
    max_dd = dd_pct = 0.0
    if len(result.equity):
        peak = np.maximum.accumulate(result.equity)
        dd = result.equity - peak
        max_dd = float(dd.min())
        troughs = peak > 0
        dd_pct = float((dd[troughs] / peak[troughs]).min() * 100) if troughs.any() else 0.0

    total_pnl = float(pnl.sum()) if len(pnl) else 0.0
    gross_win = float(wins.sum()) if len(wins) else 0.0
    gross_loss = float(-losses.sum()) if len(losses) else 0.0

    stats = {
        "net_pnl": total_pnl,
        "total_trades": len(trades),
        "win_rate": float(len(wins) / len(pnl) * 100) if len(pnl) else float("nan"),
        "profit_factor": gross_win / gross_loss if gross_loss > 0 else float("inf") if gross_win > 0 else float("nan"),
        "avg_trade": float(pnl.mean()) if len(pnl) else float("nan"),
        "avg_win": float(wins.mean()) if len(wins) else float("nan"),
        "avg_loss": float(losses.mean()) if len(losses) else float("nan"),
        "largest_win": float(pnl.max()) if len(pnl) else float("nan"),
        "largest_loss": float(pnl.min()) if len(pnl) else float("nan"),
        "expectancy": float(pnl.mean()) if len(pnl) else float("nan"),
        "sharpe": sharpe,
        "sortino": sortino,
        "max_drawdown": max_dd,
        "max_drawdown_pct": dd_pct,
        "commission": result.total_commission,
        "trading_days": len(result.daily_pnl),
        "winning_days": int((daily > 0).sum()) if len(daily) else 0,
        "losing_days": int((daily < 0).sum()) if len(daily) else 0,
        "best_day": float(daily.max()) if len(daily) else float("nan"),
        "worst_day": float(daily.min()) if len(daily) else float("nan"),
        "avg_day": float(daily.mean()) if len(daily) else float("nan"),
    }

    # prop-firm consistency: largest profitable day as % of total profit
    if len(daily) and total_pnl > 0:
        stats["consistency_pct"] = float(daily.max() / total_pnl * 100)
    else:
        stats["consistency_pct"] = float("nan")

    if result.apex is not None:
        a = result.apex
        stats["apex_breached"] = a.breached
        stats["apex_breach_ts"] = a.breach_ts
        stats["apex_min_headroom"] = (
            float(a.min_headroom) if np.isfinite(a.min_headroom) else float("nan"))
        stats["apex_threshold"] = a.cfg.threshold
    return stats


def format_console(result, stats: dict) -> str:
    def d(x):
        return f"${x:,.2f}" if x == x else "n/a"   # NaN-safe

    lines = [
        "",
        f"=== {result.strategy_name} | {result.symbol} {result.period_s}s bars | "
        f"{result.days[0]}..{result.days[-1]} ({stats['trading_days']} days) ===",
        f"Net P&L:        {d(stats['net_pnl'])}   (commission {d(stats['commission'])})",
        f"Trades:         {stats['total_trades']}   win rate {stats['win_rate']:.1f}%   "
        f"profit factor {stats['profit_factor']:.2f}",
        f"Avg trade:      {d(stats['avg_trade'])}   avg win {d(stats['avg_win'])}   "
        f"avg loss {d(stats['avg_loss'])}",
        f"Sharpe:         {stats['sharpe']:.2f}   Sortino: {stats['sortino']:.2f}",
        f"Max drawdown:   {d(stats['max_drawdown'])} ({stats['max_drawdown_pct']:.2f}%)",
        f"Days:           +{stats['winning_days']} / -{stats['losing_days']}   "
        f"best {d(stats['best_day'])}   worst {d(stats['worst_day'])}",
        f"Consistency:    largest day = {stats['consistency_pct']:.1f}% of profit"
        if stats["consistency_pct"] == stats["consistency_pct"] else
        "Consistency:    n/a",
    ]
    if "apex_breached" in stats:
        if stats["apex_breached"]:
            ts = np.datetime64(stats["apex_breach_ts"], "ns")
            lines.append(f"APEX:           *** BREACHED trailing threshold at {ts} UTC ***")
        else:
            lines.append(
                f"APEX:           survived; min headroom to threshold "
                f"{d(stats['apex_min_headroom'])} (threshold {d(stats['apex_threshold'])})")
    if result.halted_on:
        lines.append(f"HALTED on {result.halted_on} (apex breach, halt_on_breach=True)")
    lines.append(f"Runtime:        {result.runtime_s:.1f}s")
    return "\n".join(lines)
