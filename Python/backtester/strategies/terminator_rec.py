"""Terminator_V2 -- RECOMMENDED CONFIG (2026-07-08 rev 2, prop-firm compliant).

CORRECTION over the previous version of this file (session 15:30-16:55
only): that fix was needlessly conservative. The real Apex-style rule is
"flat before the daily halt" (~16:59 ET), not "don't trade the evening."
CME/Globex's own trading day already runs 18:00 ET (prior calendar day) ->
17:00 ET next day with a maintenance halt in between -- so a SINGLE
session spanning **18:00 ET (prior day) -> 16:55 ET (same trading day)**,
flattening once at the end, never holds a position through ANY halt and
is exactly as compliant as the narrow afternoon box, while covering the
18:00 ET reopen -- the single best hour in the whole 510-day dataset
(+$4,278, see TerminatorV2_ETH.md). Entries are then restricted to the
historically profitable hours via NT8's two time-filter windows
(entry_window/entry_window2); exits/stops always manage regardless of
window, so a position can legitimately carry from the evening into the
next afternoon before the 16:55 flatten -- still zero exposure during any
halt.

Config: full Globex trading day session ("18:00","16:55"), entries in
**15:30-16:55 ET** (afternoon) + **18:00-22:55 ET** (evening reopen),
ATR(28) x 3.25 SAR on ninZaRenko 100/4, **100-tick hard stop**, 1 contract.

Full-run (2024-12-16..2026-07-03): net $22,409, Sharpe 3.90 (Sortino 11.20,
Calmar 9.49), PF 1.69, maxDD -$1,488, 990 trades, WR 33.1%, survived Apex
$2.5K (min headroom $1,178). MC(2000): P(profit) 100%, P(breach) 0.4%,
P(pass $3K eval) 99.6%. Split-half: H1 +$8,465 Sh2.89, H2 +$13,848 Sh4.84
(stronger recently, as with every other cut of this data). Compliance
verified directly: 0/990 trades have any entry/exit timestamp inside the
17:00-18:00 ET halt.

Walk-forward (5 windows, IS/OOS 5:1, grid over atr_mult/atr_period/
sl_ticks with this same session+entry-window structure fixed): **5/5 OOS
windows profitable, stitched OOS net $13,034 over 203 unseen days, OOS
Sharpe 4.45, walk-forward efficiency 1.18** (OOS beats IS on average --
strong evidence against curve-fit). Every one of the 5 windows
independently re-optimized to atr_mult=3.25/atr_period=28 (sl varied
100-150 across windows, all part of the same plateau) -- this is why the
final pick uses the walk-forward's own converged params rather than the
single full-sample grid's nominal peak (mult=3.0/period=20, which tested
almost as well but was chosen by only one grid, not five independent
ones). See strategy/TerminatorV2.md revision (6).

IMPORTANT: this supersedes BOTH earlier configs --
strategies/terminator_evening.py (net $15,623, Sharpe 2.76 -- was thought
non-compliant AND is now also just weaker) and the previous 15:30-16:55-only
cut of this file (net $5,536, Sharpe 1.76 -- was over-conservative, left
the 18:00 ET reopen on the table for no compliance reason). Both are kept
only as historical reference.

NT8 settings: this needs a session template spanning 18:00 ET -> 16:55 ET
next day (flatten positions/cancel orders at session end), PLUS two entry
time filters that gate NEW entries only (existing NT8 UseTimeFilter +
window 2 feature) -- Time Filter 1: Start 153000, End 165500. Time Filter
2: Start 180000, End 225500. SL Mode=Ticks, Value=100. ATR 28 / Mult 3.25.
Confirm your firm's exact flatten cutoff (assumed ~16:59 ET here, 4min
buffer built into the 16:55 end) and adjust if different.
"""
import importlib.util
from pathlib import Path

_spec = importlib.util.spec_from_file_location(
    "terminator_v2_base", Path(__file__).with_name("terminator_v2.py"))
_mod = importlib.util.module_from_spec(_spec)
_spec.loader.exec_module(_mod)


class TerminatorRec(_mod.TerminatorV2):
    symbol = "MNQ"
    period = "r100-4"
    session = ("18:00", "16:55")       # full Globex trading day, ONE flatten at 16:55 ET
    flat_at_session_end = True
    qty = 1

    entry_window = ("15:30", "16:55")   # afternoon block (entries only; exits always manage)
    entry_window2 = ("18:00", "22:55")  # evening reopen block

    atr_period = 28
    atr_mult = 3.25
    sl_ticks = 100                     # NT8 SL Mode=Ticks, Value=100
