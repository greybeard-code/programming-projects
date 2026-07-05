"""Terminator_V2 — ETH (full Globex session) variant of the recommended
config: ATR(20) x 4.0 SAR + 200-tick hard stop, 1 contract.

Session 18:00 -> 16:55 ET (wraps overnight; positions carry across the file
boundary, flat before the 17:00 ET close like the template's
exit-on-session-close). Set entry_window to restrict entry times while
keeping the full session for exits, e.g. ("03:00", "15:00").
"""
import importlib.util
from pathlib import Path

_spec = importlib.util.spec_from_file_location(
    "terminator_v2_base", Path(__file__).with_name("terminator_v2.py"))
_mod = importlib.util.module_from_spec(_spec)
_spec.loader.exec_module(_mod)


class TerminatorETH(_mod.TerminatorV2):
    symbol = "MNQ"
    period = "r100-4"
    session = ("18:00", "16:55")     # full Globex trading day, ET
    qty = 1

    atr_period = 20
    atr_mult = 4.0
    sl_ticks = 200
