"""Terminator_V2 SCALE-OUT variant — SAR signal + ATM scale-out exits.

Research build to test the NT8 `UseScaleOut` feature in the backtester. The
signal, session, and entry windows are the validated champion (r100-4,
ATR 28 x 3.25, 15:30-16:55 + 18:00-22:55 ET, entries-only carry). Only the
EXIT model changes:

  * enter N contracts on the ATR-SAR cross (N = entry_qty),
  * bank partial targets TP1 / TP2 (limit orders, per-leg OCO with a stop),
  * the final RUNNER (target_ticks=0) rides the SAR — it is closed by the
    opposite cross (parent clean-split reversal) or the shared hard stop,
  * optional per-leg auto-BE / auto-trail via the backtester's ATM StopPlan
    engine (the same one GodZillaKilla uses) — applied to the runner here.

All exits go through backtester/atm.py, so this reuses the validated
scale-out / OCO / BE / trail machinery rather than reimplementing it. The
parent's single-bracket SL/TP and its single-stop breakeven are turned OFF
(sl_ticks=tp_ticks=be_atr=0) — the ATM legs own every exit.

Base config (user pick 2026-07-25): 3 MNQ, 1 @ TP1 + 1 @ TP2 + 1 runner,
100-tick shared stop, no BE/trail. Sweep target ticks / BE / trail from here.
NOTE: 3 contracts is ~3x the 1-contract champion's drawdown against the
$2,000 prop floor — read the PROP line, not just net P&L.
"""
import importlib.util
from pathlib import Path

_spec = importlib.util.spec_from_file_location(
    "terminator_v2_base", Path(__file__).with_name("terminator_v2.py"))
_mod = importlib.util.module_from_spec(_spec)
_spec.loader.exec_module(_mod)

from backtester.nt8config import AtmSpec, AtmBracket
from backtester.atm import submit_atm_exits


class TerminatorScaleOut(_mod.TerminatorV2):
    # --- champion signal / session / windows (== terminator_rec) ---
    symbol = "MNQ"
    period = "r100-4"
    session = ("18:00", "16:55")
    flat_at_session_end = True
    entry_window = ("15:30", "16:55")
    entry_window2 = ("18:00", "22:55")
    atr_period = 28
    atr_mult = 3.25

    # --- scale-out spec ---
    entry_qty = 3
    qty = 3                       # keep base attr consistent with entry_qty
    stop_ticks = 100              # shared hard stop (each leg, same distance)
    tp1_ticks = 40
    tp1_qty = 1
    tp2_ticks = 80
    tp2_qty = 1
    runner_qty = 1                # rides the SAR reversal (no fixed target)

    # per-leg auto-BE / trail on the RUNNER (0 = off; swept later)
    runner_be_trigger_ticks = 0
    runner_be_plus_ticks = 0
    runner_trail = ()             # tuple[TrailStep, ...], () = off

    # parent single-bracket exits OFF — the ATM legs own all exits
    sl_ticks = 0
    tp_ticks = 0
    be_atr = 0                    # parent single-stop BE disabled (BE is per-leg via ATM)

    def on_start(self):
        super().on_start()
        brackets = []
        if self.tp1_qty > 0 and self.tp1_ticks > 0:
            brackets.append(AtmBracket(qty=self.tp1_qty, stop_ticks=self.stop_ticks,
                                       target_ticks=self.tp1_ticks))
        if self.tp2_qty > 0 and self.tp2_ticks > 0:
            brackets.append(AtmBracket(qty=self.tp2_qty, stop_ticks=self.stop_ticks,
                                       target_ticks=self.tp2_ticks))
        brackets.append(AtmBracket(qty=self.runner_qty, stop_ticks=self.stop_ticks,
                                   target_ticks=0,
                                   be_trigger_ticks=self.runner_be_trigger_ticks,
                                   be_plus_ticks=self.runner_be_plus_ticks,
                                   trail_steps=tuple(self.runner_trail)))
        self._atm = AtmSpec(name="term-scaleout", entry_qty=self.entry_qty,
                            brackets=tuple(brackets))

    # Override the entry submission only; parent gating replicated verbatim
    # (TerminatorV2._go) so entry timing stays identical to the champion.
    def _go(self, direction, bar):
        if direction > 0 and not self.enable_longs:
            return
        if direction < 0 and not self.enable_shorts:
            return
        if (self.cooldown_bars > 0 and self.last_entry_bar >= 0
                and bar.index - self.last_entry_bar < self.cooldown_bars):
            return
        if self.day_blocked:
            return
        if not self._in_entry_window(bar.ts):
            return
        if direction > 0:
            self.buy(qty=self._atm.entry_qty, tag="term-long")
        else:
            self.sell(qty=self._atm.entry_qty, tag="term-short")
        self.last_entry_bar = bar.index
        self.be_done = False

    def on_fill(self, fill):
        # attach the ATM scale-out legs when the entry fills
        if not fill.order.is_exit and fill.order.tag.startswith("term-"):
            side = 1 if fill.side > 0 else -1
            submit_atm_exits(self._broker, self._atm, side, fill.price)
