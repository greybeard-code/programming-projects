"""God Trades — "Zekey" PredatorX execution config, for empirical evaluation.

This reproduces, as faithfully as the backtester allows, the execution
template shipped in
``NinjaScript/TOGodMode/NT8 code/1.0.Godtrades-Predator.xml`` driving the
``GodTrades.cs`` v16.6 signal indicator. It exists to put a hard P&L number
on that specific configuration rather than inferring it from the sweep.

What the PredatorX template actually does (vs. the methodology-faithful
:class:`GodTrades` defaults):

* Entries: SignalCode == +/-1 (BG) and +/-2 (FC). OBR is not emitted by
  v16.6, so it is off here too.                       -> enable_obr = False
* Stop:   FIXED 50 ticks both sides (StopLossOffsetValueProp=50), NOT the
          back of the signal candle.                  -> exit_mode="fixed",
                                                          fixed_sl_ticks=50
* Target: FIXED ticks. The template scales out 2/1/1/1 at 60/50/100/200t;
          we model the dominant (2-contract) leg at 60t as a single target.
                                                       -> fixed_tp_ticks=60
* Breakeven: ON at 40t in profit, stop -> entry +3t, on bar close
          (BreakevenTickTriggerAmountSync=40, offset 3, BETriggerBarClose).
          The methodology says NEVER go to break-even.
                                                       -> breakeven_* below
* Session: NO time filter anywhere (indicator UseSignalTimeFilter=false,
          PredatorX TimeFilterSelector=NoTimeFilters). Trades all session.
                                                       -> all-day window
* Size:   template is 5 contracts (2/1/1/1 scale-out). We test 1 contract
          for a clean per-unit edge read; multiply for the template's size.

Fidelity gaps worth noting in any writeup:
* The indicator's tick-body BG filter (MinimumBodyTicks=4) and ATR huge-
  candle filter have no exact Python equivalent (BG-entry-only; minor).
* Multi-target scale-out is modeled as a single 60t target.
* PredatorX enters one bar after the signal bar (ObjectEntryBarsAgo=1);
  the base strategy enters on the signal bar's close (~1 bar earlier).
"""
from __future__ import annotations

import importlib.util
import sys
from pathlib import Path

_here = Path(__file__).resolve().parent
_spec = importlib.util.spec_from_file_location(
    "god_trades", _here / "god_trades.py")
_mod = importlib.util.module_from_spec(_spec)
sys.modules["god_trades"] = _mod
_spec.loader.exec_module(_mod)
GodTrades = _mod.GodTrades


class GodTradesZekey(GodTrades):
    period = "1000t"                 # Zekey's chart is 500t; override via --period

    # ---- signal selection (v16.6 emits BG + FC only) ----
    enable_bg = True
    enable_fc = True
    enable_obr = False

    # ---- no session filter (trade the whole loaded session) ----
    entry_window = ("00:00", "23:59")
    flatten_at_window_end = False

    qty = 1

    # ---- PredatorX exits: fixed stop + fixed target ----
    exit_mode = "fixed"
    fixed_sl_ticks = 50
    fixed_tp_ticks = 60

    # ---- breakeven (methodology forbids this; template turns it on) ----
    breakeven_trigger_ticks = 40
    breakeven_offset_ticks = 3

    def on_start(self):
        super().on_start()
        self._be_done = False

    def on_bar(self, bar, bars):
        if self.flat:
            self._be_done = False
        super().on_bar(bar, bars)
        # break-even on bar close once price is trigger-ticks in profit
        if (self.breakeven_trigger_ticks and not self.flat
                and not self._be_done and self.position != 0):
            tick = self._broker.spec.tick_size
            side = 1 if self.position > 0 else -1
            profit_ticks = (bar.close - self.avg_price) * side / tick
            if profit_ticks >= self.breakeven_trigger_ticks:
                if self.move_stop_to_breakeven(self.breakeven_offset_ticks):
                    self._be_done = True
