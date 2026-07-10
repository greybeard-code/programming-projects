"""Champion + order-flow delta-agreement entry filter (exploratory).

The repo tags each trade's aggressor side, so every bar carries
buy_volume/sell_volume (bar.delta) and a session-cumulative bars.cum_delta —
data an NT8 backtest structurally cannot produce. This gates the Terminator
champion's ENTRIES on delta agreement to test whether order-flow confirmation
adds anything:

  delta_mode = "bar"  -> take a long only if the signal bar's delta > delta_min
                         (aggressive buyers backing the breakout); short mirror.
  delta_mode = "cum"  -> take a long only if session cum_delta > delta_min
                         (net buying regime); short mirror.

Exits/reversals are untouched — this only filters new entries, exactly like the
entry windows. Base class (champion) is unchanged; this is a thin subclass so
the $22,409 baseline is a pure control.
"""
import importlib.util
from pathlib import Path

_spec = importlib.util.spec_from_file_location(
    "terminator_rec", Path(__file__).with_name("terminator_rec.py"))
_mod = importlib.util.module_from_spec(_spec)
_spec.loader.exec_module(_mod)


class TerminatorDelta(_mod.TerminatorRec):
    delta_mode = "bar"     # "bar" | "cum"
    delta_min = 0          # require |delta| > delta_min to enter

    def on_bar(self, bar, bars):
        self._bars = bars              # stash for cum-delta access in _go
        super().on_bar(bar, bars)

    def _delta_ok(self, direction, bar):
        if self.delta_mode == "off":       # pure champion control
            return True
        # "cum"/"cum_fade" use session cumulative delta; "bar"/"bar_fade" the
        # signal bar's delta. *_fade require delta to DISAGREE (contrarian).
        if self.delta_mode.startswith("cum"):
            cd = self._bars.cum_delta
            d = int(cd.values[-1]) if cd.n else 0
        else:
            d = bar.delta
        agree = d > self.delta_min if direction > 0 else d < -self.delta_min
        return (not agree) if self.delta_mode.endswith("_fade") else agree

    def _go(self, direction, bar):
        if not self._delta_ok(direction, bar):
            return
        super()._go(direction, bar)
