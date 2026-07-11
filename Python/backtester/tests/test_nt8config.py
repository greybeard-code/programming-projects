"""NT8 template parsers (backtester/nt8config.py) against real template files."""
from pathlib import Path

import pytest

from backtester.nt8config import (
    AtmBracket, AtmSpec, TrailStep, load_atm_template, load_strategy_template,
)

DATA = Path(__file__).resolve().parent.parent / "nt8 code" / "GodZillaKilla" / "templates"
ATM_REF = DATA / "6QTY25.50.100BE30+10TP100SL.xml"
ATM_TRAIL = DATA / "Godzilla3MNQ.5.27.26.xml"
STRAT_REF = DATA / "OneSet_3ofAll_BestTime.xml"


# -- ATM templates ----------------------------------------------------------

def test_atm_reference_ladder():
    spec = load_atm_template(ATM_REF)
    assert spec.name == "6QTY25.50.100BE30+10TP100SL"
    assert spec.entry_qty == 6
    assert len(spec.brackets) == 3
    b1, b2, b3 = spec.brackets
    # bracket 1: static 2 @ T25/SL100, no stop strategy
    assert b1 == AtmBracket(qty=2, stop_ticks=100, target_ticks=25)
    # brackets 2+3: BE +10 @ 30t, no trail
    for b, tgt in ((b2, 50), (b3, 100)):
        assert (b.qty, b.stop_ticks, b.target_ticks) == (2, 100, tgt)
        assert (b.be_trigger_ticks, b.be_plus_ticks) == (30, 10)
        assert b.trail_steps == ()
    # quantities already match EntryQuantity -> split unchanged
    assert spec.exit_split() == spec.brackets


def test_atm_trailing_multistep():
    spec = load_atm_template(ATM_TRAIL)
    assert spec.entry_qty == 4
    assert [b.qty for b in spec.brackets] == [1, 1, 1]
    assert [b.target_ticks for b in spec.brackets] == [23, 50, 75]
    assert all(b.stop_ticks == 100 for b in spec.brackets)
    assert all((b.be_trigger_ticks, b.be_plus_ticks) == (23, 2)
               for b in spec.brackets)
    assert spec.brackets[0].trail_steps == (
        TrailStep(profit_trigger=40, stop_loss=1, frequency=2),)
    assert spec.brackets[1].trail_steps == (
        TrailStep(profit_trigger=38, stop_loss=6, frequency=2),)
    # multi-step tier sorted by profit trigger
    assert spec.brackets[2].trail_steps == (
        TrailStep(profit_trigger=40, stop_loss=32, frequency=2),
        TrailStep(profit_trigger=65, stop_loss=20, frequency=2),
    )
    # EntryQuantity 4 vs bracket sum 3 -> last bracket absorbs the extra lot
    split = spec.exit_split()
    assert [b.qty for b in split] == [1, 1, 2]
    assert split[2].target_ticks == 75


def _atm_xml(tmp_path, **overrides):
    fields = {"CalculationMode": "Ticks", "ReverseAtStop": "false"}
    fields.update(overrides)
    extra = "".join(f"<{k}>{v}</{k}>" for k, v in fields.items())
    p = tmp_path / "t.xml"
    p.write_text(
        "<NinjaTrader><AtmStrategy><Template>t</Template>"
        "<EntryQuantity>1</EntryQuantity>"
        "<Brackets><Bracket><Quantity>1</Quantity><StopLoss>10</StopLoss>"
        "<Target>10</Target></Bracket></Brackets>"
        f"{extra}</AtmStrategy></NinjaTrader>")
    return p


def test_atm_rejects_currency_mode(tmp_path):
    with pytest.raises(NotImplementedError, match="Currency"):
        load_atm_template(_atm_xml(tmp_path, CalculationMode="Currency"))


def test_atm_rejects_unmodeled_flags(tmp_path):
    with pytest.raises(NotImplementedError, match="ReverseAtStop"):
        load_atm_template(_atm_xml(tmp_path, ReverseAtStop="true"))


def test_atm_wrong_schema_detected():
    with pytest.raises(ValueError, match="load_atm_template"):
        load_strategy_template(ATM_REF)
    with pytest.raises(ValueError, match="load_strategy_template"):
        load_atm_template(STRAT_REF)


def test_exit_split_drops_empty_brackets():
    spec = AtmSpec(name="x", entry_qty=1, brackets=(
        AtmBracket(qty=2, stop_ticks=10, target_ticks=5),
        AtmBracket(qty=2, stop_ticks=10, target_ticks=15),
    ))
    split = spec.exit_split()
    assert [b.qty for b in split] == [1]
    assert split[0].target_ticks == 5


# -- Strategy templates -----------------------------------------------------

def test_strategy_template_reference():
    t = load_strategy_template(STRAT_REF)
    assert t.strategy_type.endswith("Playr101.GodZillaKilla")
    assert t.name == "OneSet_3ofAll_BestTime"
    assert t.bar_spec == "r60-3"                # ninZaRenko 60/3
    assert t.instrument == "MNQ 06-26"
    assert t.symbol == "MNQ"
    assert t.get("AtmStrategy") == "6QTY25.50.100BE30+10TP100SL"
    assert t.get("OrderMode") == "AtmStrategy"
    assert t.get_int("GroupTriggerSet1RequiredCount") == 3
    assert t.get_bool("EnableGroupTriggerSet2") is False
    assert t.get_bool("UseKOSignals") is True
    assert t.get("KO_LongOperator") == "Equal"
    assert t.get_int("PA_LongValue") == 2
    # this preset predates NobleCloud -> absent, caller's default applies
    assert "UseNCSignals" not in t.props
    assert t.get_bool("UseNCSignals", True) is True
    # time-of-day extraction from serialized DateTimes (ET wall clock)
    assert t.get_time("StartTime1") == "07:00"
    assert t.get_time("EndTime1") == "11:30"
    assert t.get_time("SkipStartTime") == "09:28"
    assert t.get_bool("EnableDailyLossLimit") is True
    assert t.get_float("DailyLossLimit") == 200.0
    # missing prop without default raises
    with pytest.raises(KeyError):
        t.get("NoSuchProperty")
