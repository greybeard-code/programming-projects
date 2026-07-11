"""ThunderZilla (gbThunderZilla port) — invariants + regression trace."""
import numpy as np

from backtester.gbsignals.thunder import ThunderZilla


def _run(n=800, seed=7, **kw):
    rng = np.random.default_rng(seed)
    steps = rng.normal(0, 2.0, n) + np.where(np.arange(n) % 200 < 100, 0.8, -0.8)
    closes = np.round((15000 + np.cumsum(steps)) / 0.25) * 0.25
    eng = ThunderZilla(**kw)
    codes, trends = [], []
    o = closes[0]
    for c in closes:
        h, l = max(o, c) + 1.0, min(o, c) - 1.0
        codes.append(eng.update(o, h, l, c, float(rng.integers(50, 500))))
        trends.append(eng.trend)
        o = c
    return np.array(codes), np.array(trends), eng


def test_code_domain_and_bar0():
    codes, trends, _ = _run()
    assert set(np.unique(codes)) <= {-4, -3, -2, -1, 0, 1, 2, 3, 4}
    assert codes[0] == 0            # bar 0 emits nothing
    assert trends[0] == 0


def test_trend_start_matches_trend_flip():
    codes, trends, _ = _run()
    for i in range(2, len(codes)):
        if codes[i] in (1, -1):
            assert trends[i] == codes[i]
            assert trends[i - 1] != trends[i]


def test_pullback_and_movestop_signs_follow_trend():
    codes, trends, _ = _run()
    for i, c in enumerate(codes):
        if abs(c) in (3, 4):
            assert np.sign(c) == trends[i]


def test_movestop_requires_trend():
    codes, trends, _ = _run()
    for i, c in enumerate(codes):
        if abs(c) == 4:
            assert trends[i] != 0


def test_regression_trace():
    """Deterministic sequence on the fixed walk (change detector)."""
    codes, _, eng = _run()
    nonzero = [(int(i), int(c)) for i, c in enumerate(codes) if c][:25]
    assert nonzero == [
        (1, -1), (3, -3), (42, -3), (49, 1), (51, -2), (57, 4), (71, -2),
        (99, -2), (118, 2), (129, -1), (133, -4), (148, 2), (192, 2),
        (195, 2), (247, -2), (277, -2), (288, 1), (290, -2), (294, 4),
        (304, -2), (319, 2), (321, 2), (329, 2), (339, 2), (351, -1),
    ]
    assert eng.trend == -1


def test_unported_ma_type_rejected():
    import pytest
    with pytest.raises(NotImplementedError, match="HMA"):
        ThunderZilla(trend_ma_type="HMA")
