"""Convert an NT8 Market Replay Executions-grid export (one row per
fill: Action/E-X/Position/Name columns) into the Trades-grid shape that
tools/compare_nt8.py expects (one row per round trip, Market pos./Entry
price/Exit price/Entry time/Exit time).

Usage:
    python tools\\convert_nt8_executions.py executions.csv trades_out.csv
"""
from __future__ import annotations

import csv
import sys
from datetime import datetime


def main() -> None:
    src, dst = sys.argv[1], sys.argv[2]
    with open(src, newline="", encoding="utf-8-sig") as f:
        rows = list(csv.DictReader(f))

    # The export is in strict reverse-chronological order, including within
    # same-second ties (verified against reversal pairs and same-second
    # stop-then-reenter clusters) -- the displayed Time is second-only
    # resolution, so re-sorting by parsed datetime loses true tie order.
    # A plain reversal of row order recovers exact chronological order.
    rows.reverse()

    trades = []
    open_trade = None
    exit_fills = None
    for r in rows:
        side = r["E/X"].strip()
        if side == "Entry":
            if open_trade is not None:
                raise SystemExit(f"overlapping entry at {r['Time']}: "
                                  f"{open_trade} still open")
            open_trade = {
                "dir": "Long" if r["Action"].strip() == "Buy" else "Short",
                "qty": r["Quantity"],
                "entry_p": r["Price"],
                "entry_t": r["Time"],
            }
            exit_fills = []
        elif side == "Exit":
            if open_trade is None:
                raise SystemExit(f"exit with no open entry at {r['Time']}")
            # A single bracket order can be reported as several same-time,
            # same-Order-ID partial fills (Market Replay only had 1-lot
            # chunks of synthetic liquidity to match against) instead of
            # one row for the full quantity. Accumulate fills and only
            # close the round trip once the Position column shows flat
            # ("-"), using the quantity-weighted average exit price.
            qty = float(r["Quantity"])
            price = float(r["Price"].replace(",", ""))
            exit_fills.append((qty, price))
            if r["Position"].strip() == "-":
                total_qty = sum(q for q, _ in exit_fills)
                avg_price = sum(q * p for q, p in exit_fills) / total_qty
                open_trade["exit_p"] = avg_price
                open_trade["exit_t"] = r["Time"]
                open_trade["exit_name"] = r["Name"]
                trades.append(open_trade)
                open_trade = None
                exit_fills = None
        else:
            raise SystemExit(f"unknown E/X value {side!r}")

    if open_trade is not None:
        print(f"warning: trailing open entry with no exit: {open_trade}",
              file=sys.stderr)

    with open(dst, "w", newline="", encoding="utf-8") as f:
        w = csv.writer(f)
        w.writerow(["Market pos.", "Qty", "Entry price", "Exit price",
                    "Entry time", "Exit time", "Exit name"])
        for t in trades:
            w.writerow([t["dir"], t["qty"], t["entry_p"], t["exit_p"],
                        t["entry_t"], t["exit_t"], t["exit_name"]])

    print(f"wrote {len(trades)} round-trip trades to {dst}")


if __name__ == "__main__":
    main()
