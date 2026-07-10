"""Order-flow delta entry filter (terminator_delta._delta_ok)."""
import importlib.util
from pathlib import Path
from types import SimpleNamespace

from backtester.strategy import Bar

_p = Path(__file__).resolve().parent.parent / "strategies" / "terminator_delta.py"
_spec = importlib.util.spec_from_file_location("terminator_delta", _p)
_mod = importlib.util.module_from_spec(_spec)
_spec.loader.exec_module(_mod)
TerminatorDelta = _mod.TerminatorDelta


def _bar(delta):
    return Bar(ts=0, open=0, high=0, low=0, close=0, volume=0, index=0,
               buy_volume=max(delta, 0), sell_volume=max(-delta, 0))


def _strat(**kw):
    s = TerminatorDelta()
    for k, v in kw.items():
        setattr(s, k, v)
    return s


def test_off_allows_everything():
    s = _strat(delta_mode="off")
    assert s._delta_ok(1, _bar(-999)) is True
    assert s._delta_ok(-1, _bar(999)) is True


def test_bar_agree():
    s = _strat(delta_mode="bar", delta_min=0)
    assert s._delta_ok(1, _bar(10)) is True       # long + buying
    assert s._delta_ok(1, _bar(-10)) is False      # long + selling -> block
    assert s._delta_ok(-1, _bar(-10)) is True      # short + selling
    assert s._delta_ok(-1, _bar(10)) is False


def test_bar_fade_inverts():
    s = _strat(delta_mode="bar_fade", delta_min=0)
    assert s._delta_ok(1, _bar(-10)) is True       # long into selling (fade)
    assert s._delta_ok(1, _bar(10)) is False


def test_threshold():
    s = _strat(delta_mode="bar", delta_min=100)
    assert s._delta_ok(1, _bar(150)) is True
    assert s._delta_ok(1, _bar(50)) is False       # below the min magnitude


def test_cum_uses_session_cumulative():
    s = _strat(delta_mode="cum", delta_min=0)
    s._bars = SimpleNamespace(cum_delta=SimpleNamespace(n=1, values=[500]))
    assert s._delta_ok(1, _bar(0)) is True         # cum>0 regardless of bar
    s._bars = SimpleNamespace(cum_delta=SimpleNamespace(n=1, values=[-500]))
    assert s._delta_ok(1, _bar(0)) is False
