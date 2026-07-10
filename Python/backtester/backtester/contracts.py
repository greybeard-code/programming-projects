"""CME futures contract specifications.

Commission defaults are the Apex Trader Funding **Tradovate** all-in
round-turn rates (broker + exchange + NFA), per
https://apextraderfunding.com/help-center/tradovate/tradovate-commission-instruments/
(as of 2026-07-09). Apex's Rithmic schedule differs (e.g. minis $3.98 RT,
micros $1.02 RT) — override per run if trading through Rithmic.
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
    "ES":  ContractSpec("ES",  0.25, 50.0,   3.10, "E-mini S&P 500"),
    "NQ":  ContractSpec("NQ",  0.25, 20.0,   3.10, "E-mini Nasdaq-100"),
    "YM":  ContractSpec("YM",  1.00,  5.0,   3.10, "E-mini Dow"),
    "RTY": ContractSpec("RTY", 0.10, 50.0,   3.10, "E-mini Russell 2000"),
    "CL":  ContractSpec("CL",  0.01, 1000.0, 3.34, "Crude Oil"),
    "GC":  ContractSpec("GC",  0.10, 100.0,  3.54, "Gold"),
    "MES": ContractSpec("MES", 0.25,  5.0,   1.04, "Micro E-mini S&P 500"),
    "MNQ": ContractSpec("MNQ", 0.25,  2.0,   1.04, "Micro E-mini Nasdaq-100"),
    "MYM": ContractSpec("MYM", 1.00,  0.5,   1.04, "Micro E-mini Dow"),
    "M2K": ContractSpec("M2K", 0.10,  5.0,   1.04, "Micro E-mini Russell 2000"),
    "MCL": ContractSpec("MCL", 0.01, 100.0,  1.34, "Micro Crude Oil"),
    "MGC": ContractSpec("MGC", 0.10, 10.0,   1.34, "Micro Gold"),
}


def get_spec(symbol: str) -> ContractSpec:
    try:
        return SPECS[symbol.upper()]
    except KeyError:
        raise KeyError(
            f"No contract spec for {symbol!r}. Known: {', '.join(sorted(SPECS))}. "
            "Add it to backtester/contracts.py."
        ) from None
