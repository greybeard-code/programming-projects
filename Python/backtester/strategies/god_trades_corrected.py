"""God Trades — the CORRECTED PredatorX config (session gate + 120t/50t fixed).

Mirrors Z-GodTrade-Pred-Corrected-fixed.xml as run in Playback:
BG+FC (OBR off), 10:15-15:00 ET session gate, fixed 50t stop, fixed 120t
target, breakeven off, 1 contract. Used to reproduce the replay grid and check
the 120t-target decision.
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


class GodTradesCorrected(GodTrades):
    period = "1000t"
    enable_bg = True
    enable_fc = True
    enable_obr = False
    # default entry_window ("10:15","15:00") + flatten_at_window_end kept
    qty = 1
    exit_mode = "fixed"
    fixed_sl_ticks = 50
    fixed_tp_ticks = 120
