"""Terminator_V2 — "PK funded(1)" template config.

From NT8 template PK funded(1).xml (MNQ 09-26, ninZaRenko 100/4):
ATR(14) x 2.0 signal line, 2 contracts, SL = 1 x ATR, TP = 2 x ATR,
breakeven at 1 x ATR (offset 0), daily lock at -$500 / +$400 with flatten.
Template trades full ETH with exit-on-session-close and no time filter;
here RTH 08:30-15:55 CT (prop context) — see the evaluation md for the
session caveat. Template's NT8 backtest had IncludeCommission=false and
Standard fill resolution on Renko (fantasy fills); this test pays real
spread + commission.
"""
import importlib.util
from pathlib import Path

_spec = importlib.util.spec_from_file_location(
    "terminator_v2_base", Path(__file__).with_name("terminator_v2.py"))
_mod = importlib.util.module_from_spec(_spec)
_spec.loader.exec_module(_mod)


class TerminatorPKFunded(_mod.TerminatorV2):
    symbol = "MNQ"
    period = "r100-4"
    session = ("08:30", "15:55")
    qty = 2

    atr_period = 14
    atr_mult = 2.0
    sl_atr = 1.0
    tp_atr = 2.0
    be_atr = 1.0
    be_offset_ticks = 0
    daily_loss = 500.0
    daily_profit = 400.0
