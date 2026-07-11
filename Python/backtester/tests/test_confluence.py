"""Confluence sweep enumeration + plumbing (no data needed)."""
from backtester.sweep_confluence import SOURCES, enumerate_combos, run_confluence


def test_enumeration_counts():
    # 2 sources: subsets {a},{b},{a,b}; counts 1 / 1 / 1,2 -> 4 combos
    combos = enumerate_combos(sources=("ko", "pa"))
    assert len(combos) == 4
    labels = [l for l, _ in combos]
    assert "1-of-1 KO" in labels and "2-of-2 KO+PA" in labels


def test_enumeration_min_size_and_counts():
    combos = enumerate_combos(sources=("ko", "pa", "th"), counts=[2, 3],
                              min_size=2)
    # subsets of size 2 (3x, count 2) + size 3 (counts 2,3) -> 5
    assert len(combos) == 5
    for label, ov in combos:
        assert ov["set1_required"] >= 2
        assert ov["set2_enabled"] is False
        enabled = [s for s in SOURCES if ov[f"use_{s}"]]
        assert ov["set1_required"] <= len(enabled)


def test_enumeration_single_requires():
    combos = enumerate_combos(sources=("ko", "pa"), counts=[1], min_size=2)
    base = len(combos)                       # no requires: 1 combo
    combos_r = enumerate_combos(sources=("ko", "pa"), counts=[1], min_size=2,
                                requires="single")
    assert len(combos_r) == base * 3         # none + req KO + req PA
    req = [ov for l, ov in combos_r if "req:PA" in l]
    assert req and req[0]["require_pa"] is True and req[0]["require_ko"] is False


def test_run_confluence_stub_runner():
    calls = []

    def stub(payload):
        calls.append(payload)
        return {"combo": payload["label"], **payload["overrides"],
                "net_pnl": 1.0, "sharpe": 1.0, "calmar": 1.0,
                "profit_factor": 1.0, "win_rate": 50.0, "max_drawdown": -1.0,
                "total_trades": 20, "prop_min_headroom": 100.0}

    rows = run_confluence("strategies/godzilla_killa.py",
                          sources=("ko", "pa"), _runner=stub)
    assert len(rows) == 4
    assert calls[0]["overrides"]["use_ko"] is True
    assert calls[0]["template"] is None
