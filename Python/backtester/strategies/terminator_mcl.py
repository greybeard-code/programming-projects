"""Terminator_V2 -- MCL (micro crude oil) config, gap-conscious evening-only.

Champion SAR signal (ATR 28 x 3.25) on ninZaRenko r40-2 -- oil's tuned brick
per the 2026-07-26 per-instrument brick sweep (MNQ wants 100t; the index micros
~60t; oil ~40t). Entry window is OIL-SPECIFIC, not the inherited MNQ windows:

  * EVENING ONLY 18:00-22:55 ET. The equity afternoon window (15:30-16:55) is a
    net LOSER on oil per the hour-of-day attribution -- oil doesn't trade the
    RTH close the way Nasdaq does. Dropping it improved net, PF, and breach.
  * flatten_at_window_end=True -> the position closes at ~22:55 instead of
    carrying through the thin, gap-prone overnight session. Deliberate: costs
    ~8% net for -29% max drawdown, chosen because oil carries geopolitical gap
    risk (Middle-East war regime, Apr 2026+). Held up through the war in-sample.

NOTE (renko flatten): flatten-at-window-end fires on the first BRICK that closes
after 22:55, not a hard clock time. In thin overnight oil a brick can lag, so a
few overnight holds survive (5 of 147 in testing). Same limitation on the NT8
side. This is NOT a hard time-stop.

*** DATA-BLOCKED / FAILED VALIDATION (2026-07-26) — DO NOT DEPLOY. ***
MCL has only ~4 months of parquet data (2026-02-01..2026-05-24; no 2025, and
2026 stops in May). The headline "full-sample" numbers below were that 4-month,
171-trade window — NOT a real history. The one genuine out-of-sample test,
walk-forward, FAILED on it: WFE 0.41 ("discard", curve-fit), no param
convergence, OOS Sharpes swinging -12.9..+18.4 on 10-44-trade windows. So the
Sharpe 3.58 / evening-only tuning / "held through the war" story was all fit on
a tiny sample and is not trustworthy. This file is kept only so the failure is
recorded. Re-evaluate ONLY if MCL gets a full multi-year history recorded.

(For reference, the 4-month in-sample figures were: net ~$7,098, PF 2.36,
Sharpe 3.58, maxDD -$826, headroom $1,641, MC breach 0.1%, 147 trades.)
"""
import importlib.util
from pathlib import Path

_spec = importlib.util.spec_from_file_location(
    "terminator_v2_base", Path(__file__).with_name("terminator_v2.py"))
_mod = importlib.util.module_from_spec(_spec)
_spec.loader.exec_module(_mod)


class TerminatorMCL(_mod.TerminatorV2):
    symbol = "MCL"
    period = "r40-2"
    session = ("18:00", "16:55")
    flat_at_session_end = True
    qty = 1

    entry_window = ("18:00", "22:55")   # oil evening only (drops the equity afternoon)
    entry_window2 = None
    flatten_at_window_end = True         # flat at ~22:55 (gap-conscious; renko-lag caveat)

    atr_period = 28
    atr_mult = 3.25
    sl_ticks = 100
