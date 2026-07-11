"""GodZillaKilla strategy: template loading, voting, window gating."""
import importlib.util
from pathlib import Path

import pytest

ROOT = Path(__file__).resolve().parent.parent
_spec = importlib.util.spec_from_file_location(
    "godzilla_killa", ROOT / "strategies" / "godzilla_killa.py")
_mod = importlib.util.module_from_spec(_spec)
_spec.loader.exec_module(_mod)
GodZillaKilla = _mod.GodZillaKilla

TEMPLATE = ROOT / "nt8 code" / "GodZillaKilla" / "templates" / "OneSet_3ofAll_BestTime.xml"


# ---------------- from_template --------------------------------------------

def test_from_template_reference_preset():
    s = GodZillaKilla.from_template(TEMPLATE)
    assert s.period == "r60-3"
    assert s.symbol == "MNQ"
    assert s.order_mode == "atm"
    assert s.atm_template == "6QTY25.50.100BE30+10TP100SL"
    assert s.set1_required == 3
    assert s.set2_enabled is False
    # this preset saves Equal operators (not the GE/LE defaults)
    assert s.ko_long == ("Equal", 1) and s.ko_short == ("Equal", -1)
    assert s.pa_long == ("Equal", 2) and s.pa_short == ("Equal", -2)
    assert s.th_long == ("Equal", 2)
    # preset predates NobleCloud -> v1.10 default leaves NC ENABLED (GE/LE)
    assert s.use_nc is True
    assert s.nc_long == ("GreaterOrEqual", 1)
    # windows (ET) + skip + flatten flags
    assert s.tf1 == ("07:00", "11:30") and s.tf1_flatten is False
    assert s.tf2 == ("13:01", "15:30") and s.tf2_flatten is True
    assert s.tf3 == ("18:04", "21:00") and s.tf3_flatten is False
    assert s.skip_enabled is True and s.skip_window == ("09:28", "09:35")
    # daily limits enabled in this preset -> engine-level stand-downs
    assert s.daily_profit_target == 500.0
    assert s.daily_loss_limit == 200.0
    # engine param pass-through
    assert s.thunder_params["trend_period"] == 200
    assert s.pana_params["factor"] == 4.0
    assert s.sj_params["signal_close_threshold"] == 70


# ---------------- voting ----------------------------------------------------

def _strat(**kw):
    s = GodZillaKilla()
    for src in ("ko", "pa", "th", "sj", "su", "nc"):
        setattr(s, f"use_{src}", False)
        setattr(s, f"require_{src}", False)
    for k, v in kw.items():
        setattr(s, k, v)
    return s


def test_vote_counting_and_threshold():
    s = _strat(use_ko=True, use_pa=True, use_th=True, set1_required=2,
               ko_long=("Equal", 1), ko_short=("Equal", -1),
               pa_long=("Equal", 2), pa_short=("Equal", -2),
               th_long=("Equal", 2), th_short=("Equal", -2))
    codes = {"ko": 1, "pa": 2, "th": 0, "sj": None, "su": None, "nc": None}
    assert s._vote(codes, "", 2) == 1          # 2 of 3 agree long
    codes = {"ko": 1, "pa": 0, "th": 0, "sj": None, "su": None, "nc": None}
    assert s._vote(codes, "", 2) == 0          # only 1 vote


def test_required_veto():
    s = _strat(use_ko=True, use_pa=True, require_pa=True,
               ko_long=("Equal", 1), ko_short=("Equal", -1),
               pa_long=("Equal", 2), pa_short=("Equal", -2))
    codes = {"ko": 1, "pa": 0, "sj": None, "su": None, "nc": None, "th": None}
    # KO alone reaches count 1, but required PA did not agree -> veto
    assert s._vote(codes, "", 1) == 0
    codes["pa"] = 2
    assert s._vote(codes, "", 1) == 1


def test_conflict_abort_within_set():
    s = _strat(use_ko=True, use_pa=True, set1_required=1,
               ko_long=("Equal", 1), ko_short=("Equal", -1),
               pa_long=("Equal", 2), pa_short=("Equal", -2))
    codes = {"ko": 1, "pa": -2, "sj": None, "su": None, "nc": None, "th": None}
    assert s._vote(codes, "", 1) == 0          # long and short both >= needed


def test_operator_semantics():
    s = _strat(use_ko=True, ko_long=("GreaterOrEqual", 1),
               ko_short=("LessOrEqual", -1))
    base = {"pa": None, "th": None, "sj": None, "su": None, "nc": None}
    assert s._vote({**base, "ko": 2}, "", 1) == 1    # 2 >= 1
    assert s._vote({**base, "ko": -2}, "", 1) == -1
    s.ko_long = ("Equal", 1)
    assert s._vote({**base, "ko": 2}, "", 1) == 0    # Equal excludes 2


def test_required_count_above_enabled_disables_set():
    s = _strat(use_ko=True, ko_long=("Equal", 1), ko_short=("Equal", -1))
    base = {"pa": None, "th": None, "sj": None, "su": None, "nc": None}
    assert s._vote({**base, "ko": 1}, "", 5) == 0    # 5 > 1 enabled


# ---------------- windows ---------------------------------------------------

def _tod(hhmm):
    h, m = map(int, hhmm.split(":"))
    return h * 60 + m


def test_entry_windows_and_skip():
    s = GodZillaKilla()          # defaults: TF1 19:00-23:30 etc, skip 11:45-13:00
    assert s._entry_allowed(_tod("20:00")) is True       # TF1
    assert s._entry_allowed(_tod("05:00")) is True       # TF2
    assert s._entry_allowed(_tod("12:00")) is False      # skip window
    assert s._entry_allowed(_tod("16:30")) is True       # inside TF3 wrap
    # TF3 wraps midnight (08:00 -> 03:45)
    assert s._entry_allowed(_tod("02:00")) is True
    s.tf3_enabled = False
    assert s._entry_allowed(_tod("16:30")) is False      # now outside all


def test_no_windows_means_always_allowed():
    s = GodZillaKilla()
    s.tf1_enabled = s.tf2_enabled = s.tf3_enabled = False
    s.skip_enabled = False
    assert s._entry_allowed(_tod("16:30")) is True
    s.skip_enabled = True
    assert s._entry_allowed(_tod("12:00")) is False      # skip still applies
