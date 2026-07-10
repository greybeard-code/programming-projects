"""Terminator_V2 — evening-inclusive config (SUPERSEDED, kept for reference only).

This is the 2026-07-08 pre-compliance-check "recommended" config: ATR(20) x
4.0 SAR on ninZaRenko 100/4, a 100-tick hard stop, session **15:00 -> 20:55
ET**, 1 contract. It holds positions THROUGH the 17:00-18:00 ET daily
maintenance halt into the 18:00 ET Globex reopen (the single best hour in
the whole 510-day dataset, +$4,278) and does not flatten until 20:55 ET.

Full-run: net $15,623, Sharpe 2.76, PF 1.62, maxDD -$1,969, 730 trades,
survived Apex $2.5K (min headroom $527). MC(2000): P(breach) 0.9%,
P(pass $3K eval) 99.1%.

This config was flagged as NOT prop-firm compliant (Apex-style accounts
require flat by ~16:59 ET, before the daily halt -- holding through it into
the evening breaks that rule). It has now ALSO been superseded on pure
performance: strategies/terminator_rec.py reframes the session as one
continuous Globex trading day (18:00 ET prior day -> 16:55 ET, ONE
flatten -- never holds through a halt) with entries restricted to the
profitable hours, and beats this config outright: net $22,409, Sharpe 3.90,
MC breach 0.4%, pass $3K eval 99.6%, walk-forward validated (5/5 OOS
windows profitable). There is no longer a reason to use this file over
terminator_rec.py for any purpose other than historical comparison. See
strategy/TerminatorV2.md revision (6).
"""
import importlib.util
from pathlib import Path

_spec = importlib.util.spec_from_file_location(
    "terminator_v2_base", Path(__file__).with_name("terminator_v2.py"))
_mod = importlib.util.module_from_spec(_spec)
_spec.loader.exec_module(_mod)


class TerminatorEvening(_mod.TerminatorV2):
    symbol = "MNQ"
    period = "r100-4"
    session = ("15:00", "20:55")      # holds THROUGH the 17:00-18:00 ET halt
    flat_at_session_end = True
    qty = 1

    atr_period = 20
    atr_mult = 4.0
    sl_ticks = 100
