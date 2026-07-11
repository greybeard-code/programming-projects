"""Ports of the GreyBeard NT8 signal indicators (Signal_Trade streams only).

Each engine is an incremental per-closed-bar class:

    eng = PanaKanal(...)
    code = eng.update(open, high, low, close)   # int signal code, 0 = none

Sources: nt8 code/GodZillaKilla/indicators/*.cs (user's own gb* code).
Only the OnBarClose signal math is ported — rendering, alerts, markers and
intrabar (OnEachTick) revert paths are omitted by design.
"""
from .pana import PanaKanal
from .thunder import ThunderZilla

__all__ = ["PanaKanal", "ThunderZilla"]
