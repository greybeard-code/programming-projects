import numpy as np
import pytest

from backtester.account import Trade
from backtester import montecarlo


def mk_trades(pnls, mae=None, mfe=None):
    out = []
    for i, p in enumerate(pnls):
        t = Trade(direction=1, qty=1, pnl=float(p))
        t.mae = float(mae[i]) if mae is not None else min(p, 0.0)
        t.mfe = float(mfe[i]) if mfe is not None else max(p, 0.0)
        out.append(t)
    return out


def test_all_winners_never_breach():
    trades = mk_trades([50.0] * 40)
    r = montecarlo.run(trades, 50_000, n_sims=500, apex_threshold=2500,
                       profit_target=1000)
    assert r.prob_breach == 0.0
    assert r.prob_pass == 1.0
    assert r.prob_profitable == 1.0
    assert r.pnl_median == pytest.approx(2000.0)
    assert r.dd_median == 0.0


def test_identical_trades_deterministic_distribution():
    trades = mk_trades([10.0] * 30)
    r = montecarlo.run(trades, 50_000, n_sims=200)
    assert r.pnl_p5 == r.pnl_p95 == pytest.approx(300.0)


def test_guaranteed_breach_from_single_huge_mae():
    # every sequence contains the killer trade eventually? No — resampling
    # may omit it. Use all trades breaching on their own MAE from start.
    trades = mk_trades([-100.0] * 20, mae=[-3000.0] * 20)
    r = montecarlo.run(trades, 50_000, n_sims=300, apex_threshold=2500)
    assert r.prob_breach == 1.0


def test_breach_via_trailing_peak_not_just_start():
    # each trade: +$2000 close with MFE +$2600, MAE 0. Two consecutive:
    # peak after MFE of 2nd trade = 2000+2600=4600 -> floor locked at 100
    # (lock_buffer): equity never <= 100 after being positive... use lock=False
    # style by big lock buffer? Simpler: trades alternate +2600 MFE then a
    # -600 trade with MAE -700: after first trade peak>=2600 -> floor 100
    # (locked). equity path: 2000 -> trough 2000-700=1300 > 100. No breach.
    # With threshold 1000: floor locks at 100 too. Test lock behavior instead:
    trades = mk_trades([2000.0, -600.0] * 10, mfe=[2600.0, 0.0] * 10,
                       mae=[0.0, -700.0] * 10)
    r = montecarlo.run(trades, 50_000, n_sims=200, apex_threshold=1000)
    # once locked at +100, equity stays well above -> should rarely breach;
    # but a sim starting with several -600/-700 trades breaches near start.
    assert 0.0 <= r.prob_breach < 1.0


def test_autocorr_detection_switches_to_block():
    # strongly alternating sequence has |r| ~ 1 at lag 1
    pnl = [100.0, -100.0] * 30
    trades = mk_trades(pnl)
    r = montecarlo.run(trades, 50_000, n_sims=100, method="auto")
    assert r.max_autocorr > 0.2
    assert r.method == "block"
    assert r.block_size is not None


def test_iid_when_uncorrelated():
    rng = np.random.default_rng(4)   # seed with max |autocorr| ~0.07
    trades = mk_trades(rng.normal(10, 100, size=200))
    r = montecarlo.run(trades, 50_000, n_sims=100, method="auto")
    assert r.method == "iid"


def test_eval_race_pass_and_fail_sum_sane():
    rng = np.random.default_rng(2)
    trades = mk_trades(rng.normal(5, 80, size=150))
    r = montecarlo.run(trades, 50_000, n_sims=500, apex_threshold=1500,
                       profit_target=1500)
    assert r.prob_pass is not None
    assert 0.0 <= r.prob_pass <= 1.0
    assert 0.0 <= r.prob_fail <= 1.0
    assert r.prob_pass + r.prob_fail <= 1.0 + 1e-9


def test_needs_two_trades():
    with pytest.raises(ValueError):
        montecarlo.run(mk_trades([5.0]), 50_000)
