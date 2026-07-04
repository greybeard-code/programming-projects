import pytest

from backtester.sweep import expand_grid, rank, sensitivity, format_sensitivity
from backtester.walkforward import split_windows


def test_expand_grid():
    combos = expand_grid({"a": [1, 2], "b": [10, 20, 30]})
    assert len(combos) == 6
    assert {"a": 2, "b": 30} in combos


def test_rank_prefers_metric_and_min_trades():
    rows = [
        {"a": 1, "sharpe": 2.0, "total_trades": 5},    # too few trades
        {"a": 2, "sharpe": 1.0, "total_trades": 50},
        {"a": 3, "sharpe": 1.5, "total_trades": 50},
        {"a": 4, "sharpe": float("nan"), "total_trades": 50},
    ]
    ranked = rank(rows, "sharpe", min_trades=10)
    assert [r["a"] for r in ranked[:2]] == [3, 2]
    assert ranked[-1]["a"] in (1, 4)                   # ineligible/nan sink


def test_sensitivity_holds_others_at_best():
    grid = {"a": [1, 2], "b": [10, 20]}
    rows = [
        {"a": 1, "b": 10, "sharpe": 0.5},
        {"a": 1, "b": 20, "sharpe": 0.7},
        {"a": 2, "b": 10, "sharpe": 0.9},
        {"a": 2, "b": 20, "sharpe": 1.5},   # best
    ]
    best = rows[3]
    sens = sensitivity(rows, best, grid, "sharpe")
    # varying a with b at 20: values 0.7 (a=1) and 1.5 (a=2)
    assert [(v, m) for v, m, _ in sens["a"]] == [(1, 0.7), (2, 1.5)]
    assert [(v, m) for v, m, _ in sens["b"]] == [(10, 0.9), (20, 1.5)]
    txt = format_sensitivity(sens, "sharpe")
    assert "FRAGILE" in txt                 # 0.7 < 0.5 * 1.5


def test_sensitivity_plateau_not_fragile():
    grid = {"a": [1, 2, 3]}
    rows = [{"a": v, "sharpe": s} for v, s in [(1, 1.3), (2, 1.5), (3, 1.4)]]
    sens = sensitivity(rows, rows[1], grid, "sharpe")
    assert "FRAGILE" not in format_sensitivity(sens, "sharpe")


def test_split_windows_5to1():
    days = [f"d{i:03}" for i in range(100)]
    ws = split_windows(days, n_windows=5, ratio=5)
    # oos = 100 // 10 = 10, is = 50
    assert len(ws) == 5
    assert len(ws[0].is_days) == 50 and len(ws[0].oos_days) == 10
    # windows roll by OOS length; OOS follows IS immediately
    assert ws[0].oos_days[0] == "d050"
    assert ws[1].is_days[0] == "d010"
    assert ws[1].oos_days[0] == "d060"
    assert ws[4].oos_days[-1] == "d099"
    # OOS segments are disjoint and consecutive
    all_oos = [d for w in ws for d in w.oos_days]
    assert len(all_oos) == len(set(all_oos)) == 50


def test_split_windows_insufficient_days():
    with pytest.raises(ValueError):
        split_windows(["d1", "d2"], n_windows=5, ratio=5)
