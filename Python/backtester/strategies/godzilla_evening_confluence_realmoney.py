"""GodZillaKilla -- evening confluence, real-money version (validated).

Same gate and window as `godzilla_evening_confluence.py` (TH+PA+SJ, 3-of-3,
1-bar confirmation, 20:00-20:45 ET, MNQ r70-4) but for an account with no
Apex prop-firm rules to obey: run with `--prop-threshold 0`. With no 30-second
minimum-hold rule and no trailing floor to protect, the optimal exit tightens
(SL200 -> SL150) and ThunderZilla's trend filter can afford to slow down
(SMA 200 -> 300) -- both are confirmed upgrades, not just "less caution".

Validated at 1 contract: net $1,178/19mo (~140 trades, ~7.4/mo), WR 80%,
PF 1.52, Sharpe 2.05, maxDD -$227. There is no external risk cap here, so
size contracts to your own risk tolerance -- see CLAUDE.md for the
zero-optimization methodology that validated this before changing anything.
"""
import importlib.util
from pathlib import Path

_spec = importlib.util.spec_from_file_location(
    "godzilla_evening_confluence_base",
    Path(__file__).with_name("godzilla_evening_confluence.py"))
_mod = importlib.util.module_from_spec(_spec)
_spec.loader.exec_module(_mod)


class GodZillaEveningConfluenceRealMoney(_mod.GodZillaEveningConfluence):
    fixed_qty = 1
    fixed_sl_ticks = 150
    thunder_params = dict(_mod.GodZillaEveningConfluence.thunder_params,
                           trend_period=300)
