"""CME futures contract specifications.

Commission defaults are approximate all-in round-turn costs (broker + exchange
+ NFA) typical of a Rithmic prop-firm setup. Override per run if yours differ.
"""
from __future__ import annotations

from dataclasses import dataclass


@dataclass(frozen=True)
class ContractSpec:
    symbol: str
    tick_size: float
    point_value: float
    commission_rt: float  # round-turn, per contract, dollars
    description: str = ""

    @property
    def tick_value(self) -> float:
        return self.tick_size * self.point_value

    @property
    def commission_side(self) -> float:
        return self.commission_rt / 2.0


SPECS: dict[str, ContractSpec] = {
    "ES":  ContractSpec("ES",  0.25, 50.0,  4.06, "E-mini S&P 500"),
    "NQ":  ContractSpec("NQ",  0.25, 20.0,  4.06, "E-mini Nasdaq-100"),
    "YM":  ContractSpec("YM",  1.00,  5.0,  4.06, "E-mini Dow"),
    "RTY": ContractSpec("RTY", 0.10, 50.0,  4.06, "E-mini Russell 2000"),
    "GC":  ContractSpec("GC",  0.10, 100.0, 5.50, "Gold"),
    "MES": ContractSpec("MES", 0.25,  5.0,  1.04, "Micro E-mini S&P 500"),
    "MNQ": ContractSpec("MNQ", 0.25,  2.0,  1.04, "Micro E-mini Nasdaq-100"),
    "MYM": ContractSpec("MYM", 1.00,  0.5,  1.04, "Micro E-mini Dow"),
    "M2K": ContractSpec("M2K", 0.10,  5.0,  1.04, "Micro E-mini Russell 2000"),
    "MGC": ContractSpec("MGC", 0.10, 10.0,  1.54, "Micro Gold"),
}


def get_spec(symbol: str) -> ContractSpec:
    try:
        return SPECS[symbol.upper()]
    except KeyError:
        raise KeyError(
            f"No contract spec for {symbol!r}. Known: {', '.join(sorted(SPECS))}. "
            "Add it to backtester/contracts.py."
        ) from None
