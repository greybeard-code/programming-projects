"""Confluence sweep enumeration + plumbing (no data needed)."""
from backtester.sweep_confluence import (
    SOURCES, _run_one, enumerate_combos, run_confluence,
)


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


def test_run_confluence_passes_symbol_payload():
    calls = []

    def stub(payload):
        calls.append(payload)
        return {"combo": payload["label"], "net_pnl": 1.0, "sharpe": 1.0,
                "calmar": 1.0, "profit_factor": 1.0, "win_rate": 50.0,
                "max_drawdown": -1.0, "total_trades": 20,
                "prop_min_headroom": 100.0}

    run_confluence("strategies/godzilla_killa.py", symbol="MES",
                  sources=("ko",), _runner=stub)
    assert all(c["symbol"] == "MES" for c in calls)


def test_symbol_override_applied_before_backtest(monkeypatch):
    """Regression: a template with an empty saved instrument field left the
    strategy on the class default symbol (MNQ) even when --symbol MES was
    given, since from_template() has nothing to infer from. _run_one must
    apply payload["symbol"] itself, before Backtest ever sees the strategy."""
    seen_symbols = []

    class _FakeBacktest:
        def __init__(self, strat, **kw):
            seen_symbols.append(strat.symbol)
            self.strat = strat

        def run(self):
            return object()

    monkeypatch.setattr("backtester.sweep_confluence.Backtest", _FakeBacktest)
    monkeypatch.setattr("backtester.sweep_confluence.metrics.compute",
                        lambda res: {})

    _run_one({
        "strategy_path": "strategies/godzilla_killa.py", "template": None,
        "symbol": "MES", "label": "x",
        "overrides": {"use_ko": True, "set1_required": 1},
        "start": None, "end": None, "start_balance": 50_000.0,
        "prop_threshold": None, "slippage_ticks": 0.0,
    })
    assert seen_symbols == ["MES"]
