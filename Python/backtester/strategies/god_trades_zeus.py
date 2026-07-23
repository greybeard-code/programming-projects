"""God Trades — Python mirror of the NT8 `gbZeus` strategy (Phase-4 parity).

Matches gbZeus's live configuration so the two can be compared trade-for-trade:
BG + FC only (the v16.6 indicator dropped OBR), methodology-faithful exits
(candle-back stop + opposite-Bollinger-band target re-priced each bar),
10:15-15:00 ET entry window with flatten at the end, spiderweb stand-aside,
1 contract, NQ 1000-tick.

Everything else is the god_trades.py default, which already mirrors the
indicator's defaults (Bollinger 20/2, band proximity 8t, min gap age 3 bars,
confirm within 2 bars closing beyond the full zone, signal-candle direction +
correct-approach + midpoint filters on at 50/50).
"""
from __future__ import annotations

import importlib.util
import sys
from pathlib import Path

_here = Path(__file__).resolve().parent
_spec = importlib.util.spec_from_file_location("god_trades", _here / "god_trades.py")
_mod = importlib.util.module_from_spec(_spec)
sys.modules["god_trades"] = _mod
_spec.loader.exec_module(_mod)
GodTrades = _mod.GodTrades


class GodTradesZeus(GodTrades):
    period = "1000t"
    qty = 1

    # v16.6 indicator emits BG (+/-1) and FC (+/-2) only
    enable_bg = True
    enable_fc = True
    enable_obr = False

    # methodology-faithful exits (what gbZeus does)
    exit_mode = "band"
    track_band_target = True
    stop_offset_ticks = 0

    # entry window + flatten (gbZeus defaults 10:15-15:00 ET)
    entry_window = ("10:15", "15:00")
    flatten_at_window_end = True

    # gbZeus has SpiderwebSuppress ON by default
    spiderweb_suppress = True
    spiderweb_distance_ticks = 100
    spiderweb_line_count = 5
