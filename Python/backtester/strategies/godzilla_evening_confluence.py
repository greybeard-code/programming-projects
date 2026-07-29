"""GodZillaKilla -- evening confluence, Apex prop-firm version (validated).

Encodes the settings from `CLAUDE.md`'s "GodZillaKilla confluence backtest --
validated configs" section: TH (ThunderZilla) + PA (PanaKanal) + SJ
(SuperJumpBoost) engines only, all 3 required to agree (`set1_required=3`),
1-bar confirmation, entries gated to the 20:00-20:45 ET evening window (see
CLAUDE.md for why -- the morning window and looser gates are real but worse
risk-adjusted / harder to validate). MNQ, ninZaRenko r70-4, full history.

Run with the CLI default `--prop-threshold 2000` (Apex's $2,000 trailing
floor) for the validated result: net $2,803/19mo, WR 82.9%, PF 1.39,
maxDD -$830 (survives w/ $1,159 headroom), Monte Carlo P(breach)=13%,
P(pass a $3k eval before breaching)=56% at 3 contracts on a 50K account.
Do NOT run this on NQ (full-size) -- see CLAUDE.md, MNQ micros are the only
way to size into the validated sweet spot.

For the real-money framing (no prop-firm rules, tighter stop, slower
ThunderZilla trend filter) see `godzilla_evening_confluence_realmoney.py`.

This is a pinned, already-validated config, not a starting point for a fresh
sweep -- see CLAUDE.md for the zero-optimization methodology that validated
it before changing anything.
"""
import importlib.util
from pathlib import Path

_spec = importlib.util.spec_from_file_location(
    "godzilla_killa_base", Path(__file__).with_name("godzilla_killa.py"))
_mod = importlib.util.module_from_spec(_spec)
_spec.loader.exec_module(_mod)


class GodZillaEveningConfluence(_mod.GodZillaKilla):
    symbol = "MNQ"
    period = "r70-4"

    # 3-of-3 gate: TH + PA + SJ only, all three must agree
    set1_required = 3
    use_ko, use_su, use_nc = False, False, False
    use_pa, use_th, use_sj = True, True, True
    confirmation_bars = 1

    # entries-only evening window (window gates NEW entries; reversal exit
    # and session-end flatten stay always-active -- see CLAUDE.md's NT8
    # time-filter reconciliation notes on why entries-only semantics matter)
    tf1_enabled, tf1, tf1_flatten = True, ("20:00", "20:45"), False
    tf2_enabled = False
    tf3_enabled = False
    skip_enabled = False

    # fixed-ticks exits (synthetic single-bracket ATM)
    order_mode = "fixed"
    fixed_qty = 3
    fixed_tp_ticks = 60
    fixed_sl_ticks = 200
