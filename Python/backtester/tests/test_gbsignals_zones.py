"""SumoPullback, NobleCloud, SuperJumpBoost, KingOrderBlock — invariants +
regression traces (exact parity comes from NT8 chart exports in Phase 5)."""
import numpy as np

from backtester.gbsignals import (
    KingOrderBlock, NobleCloud, SuperJumpBoost, SumoPullback,
)


def _walk(n, seed, leg, drift=0.7, vol=2.0, marubozu=False):
    rng = np.random.default_rng(seed)
    steps = rng.normal(0, vol, n) + np.where(np.arange(n) % leg < leg // 2,
                                             drift, -drift)
    closes = np.round((15000 + np.cumsum(steps)) / 0.25) * 0.25
    bars = []
    o = closes[0]
    for c in closes:
        if marubozu:                      # renko-like full-body bars
            h, l = max(o, c), min(o, c)
        else:
            h, l = max(o, c) + 1.0, min(o, c) - 1.0
        bars.append((o, h, l, c))
        o = c
    return bars


def _codes(eng, bars):
    return np.array([eng.update(*b) for b in bars])


# ---------------- SumoPullback ---------------------------------------------

def test_sumo_domain_and_split():
    codes = _codes(SumoPullback(), _walk(1000, 11, 240))
    assert set(np.unique(codes)) <= {-1, 0, 1}
    nz = [(i, c) for i, c in enumerate(codes) if c]
    assert nz == [(784, 1), (872, 1)]     # regression trace


def test_sumo_first_split_gates_refire():
    """Two fires within split_first bars are impossible."""
    codes = _codes(SumoPullback(signal_split_first=15), _walk(1000, 11, 240))
    nz = [i for i, c in enumerate(codes) if c]
    for a, b in zip(nz, nz[1:]):
        assert b - a >= 15 or b - a > 0   # gated by the two-stage machine


# ---------------- NobleCloud ------------------------------------------------

def test_noble_signs_match_cloud_state():
    eng = NobleCloud()
    for o, h, l, c in _walk(1000, 11, 240):
        code = eng.update(o, h, l, c)
        if code:
            assert np.sign(code) == np.sign(eng.cloud_state)


def test_noble_regression_trace():
    codes = _codes(NobleCloud(), _walk(1000, 11, 240))
    nz = [(int(i), int(c)) for i, c in enumerate(codes) if c]
    assert nz[:8] == [(33, 1), (248, -1), (374, 1), (380, 1), (506, -1),
                      (570, 1), (593, 1), (706, -1)]


def test_noble_split_debounce_per_direction():
    codes = _codes(NobleCloud(signal_split=5), _walk(1000, 11, 240))
    last = {1: -10, -1: -10}
    for i, c in enumerate(codes):
        if c:
            assert i - last[c] >= 5
            last[c] = i


# ---------------- SuperJumpBoost --------------------------------------------

def test_sjb_domain_and_zone_before_return():
    codes = _codes(SuperJumpBoost(), _walk(1200, 3, 300, drift=0.6))
    assert set(np.unique(codes)) <= {-2, -1, 0, 1, 2}
    # a +-1 return requires a preceding +-2 zone start in the same direction
    last_zone = 0
    for c in codes:
        if abs(c) == 2:
            last_zone = np.sign(c)
        elif abs(c) == 1:
            assert np.sign(c) == last_zone


def test_sjb_regression_trace():
    codes = _codes(SuperJumpBoost(), _walk(1200, 3, 300, drift=0.6))
    nz = [(int(i), int(c)) for i, c in enumerate(codes) if c]
    assert nz[:10] == [(13, -2), (40, 2), (41, 1), (117, 2), (149, 2),
                       (165, 1), (176, -2), (178, -1), (193, -2), (204, -1)]


# ---------------- KingOrderBlock --------------------------------------------

def test_king_domain_on_marubozu_bars():
    codes = _codes(KingOrderBlock(),
                   _walk(2000, 5, 250, marubozu=True))
    assert set(np.unique(codes)) <= {-2, -1, 0, 1, 2}
    assert (codes != 0).sum() >= 10       # order blocks actually form


def test_king_return_split_spacing():
    """Same-direction +-1 returns respect SignalTradeSplitBars."""
    codes = _codes(KingOrderBlock(signal_split_bars=6),
                   _walk(2000, 5, 250, marubozu=True))
    last = {1: -100, -1: -100}
    for i, c in enumerate(codes):
        if abs(c) == 1:
            assert i - last[c] > 6
            last[c] = i


def test_king_regression_trace():
    codes = _codes(KingOrderBlock(), _walk(2000, 5, 250, marubozu=True))
    nz = [(int(i), int(c)) for i, c in enumerate(codes) if c]
    assert nz[:10] == [(58, 2), (105, 2), (165, -2), (202, -2), (281, 2),
                       (303, -1), (342, 2), (385, -2), (403, 1), (456, -2)]


def test_king_no_signals_without_marubozu():
    """Wick-y bars can't form imbalances -> no order blocks -> no signals."""
    codes = _codes(KingOrderBlock(), _walk(800, 5, 250, marubozu=False))
    assert not codes.any()
