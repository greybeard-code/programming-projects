"""PanaKanal (gbPANAKanal port) — structural invariants + regression trace.

Exact NT8 parity is validated separately against chart exports
(tools/compare_signals.py); these tests pin the port's mechanics and guard
against regressions in the meantime.
"""
import numpy as np
import pytest

from backtester.gbsignals import PanaKanal


def _walk(n=400, seed=42):
    rng = np.random.default_rng(seed)
    steps = rng.normal(0, 2.0, n) + np.where(np.arange(n) % 160 < 80, 0.6, -0.6)
    closes = np.round((15000 + np.cumsum(steps)) / 0.25) * 0.25
    return closes


def _run(closes, **kw):
    eng = PanaKanal(**kw)
    codes, trends = [], []
    o = closes[0]
    for c in closes:
        h, l = max(o, c) + 1.0, min(o, c) - 1.0
        codes.append(eng.update(o, h, l, c))
        trends.append(eng.trend)
        o = c
    return np.array(codes), np.array(trends), eng


def test_bar0_and_bar1_nt8_defaults():
    """Bar 0 emits nothing / trend -1; bar 1 always flips up on positive
    prices (NT8 unset-Series zeros make close > down-line 0)."""
    eng = PanaKanal()
    assert eng.update(100.0, 101.0, 99.0, 100.5) == 0
    assert eng.trend == -1
    assert eng.update(100.5, 101.5, 99.5, 101.0) == 1
    assert eng.trend == 1


def test_flip_codes_alternate_and_match_trend():
    codes, trends, _ = _run(_walk())
    flips = [(i, c) for i, c in enumerate(codes) if c in (1, -1)]
    assert len(flips) >= 4
    for (i, c), (j, d) in zip(flips, flips[1:]):
        assert c == -d                       # strict alternation
    for i, c in flips:
        assert trends[i] == c                # flip bar carries the new trend


def test_break_and_pullback_signs_follow_trend():
    codes, trends, _ = _run(_walk())
    for i, c in enumerate(codes):
        if abs(c) in (2, 3):
            assert np.sign(c) == trends[i]


def test_break_split_spacing():
    """Two breaks in the same trend leg must be >= SignalBreakSplit apart."""
    codes, trends, _ = _run(_walk())
    last_break, last_flip = None, 0
    for i, c in enumerate(codes):
        if c in (1, -1):
            last_flip, last_break = i, None
        elif abs(c) == 2:
            if last_break is not None:
                assert i - last_break >= 20
            last_break = i


def test_regression_trace():
    """Deterministic signal sequence on the fixed walk (change detector —
    re-derive only for an intentional model change, then re-validate parity)."""
    codes, _, eng = _run(_walk())
    nonzero = [(int(i), int(c)) for i, c in enumerate(codes) if c]
    assert nonzero == [
        (1, 1), (2, 3), (4, -1), (9, -3), (11, 1), (73, 2), (94, -1),
        (127, -2), (165, -2), (180, 1), (210, 2), (242, -1), (249, -2),
        (317, -2), (324, 1), (386, 2),
    ]
    assert eng.trend == 1
    assert eng.middle == pytest.approx(15040.0)


def test_pullback_window_expires():
    """No +-3 possible SignalPullbackFindingPeriod bars after a flip."""
    codes, trends, _ = _run(_walk(), signal_pullback_finding_period=3)
    last_flip = None
    for i, c in enumerate(codes):
        if c in (1, -1):
            last_flip = i
        elif abs(c) == 3 and last_flip is not None:
            assert i - last_flip < 3
