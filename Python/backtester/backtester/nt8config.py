"""NT8 template parsers: ATM strategy templates + NinjaScript strategy templates.

Two distinct schemas live under Documents\\NinjaTrader 8\\templates:

* ``AtmStrategy\\*.xml`` — root ``<NinjaTrader><AtmStrategy>``: bracket exits
  (scale-out targets/stops, auto-breakeven, multi-step auto-trail).
  -> :func:`load_atm_template` -> :class:`AtmSpec`.
* ``Strategy\\<Type>\\*.xml`` — root ``<StrategyTemplate>``: a saved parameter
  set for a compiled NinjaScript strategy. The authoritative values are the
  flat ``<Strategy><TypeName>`` property block (NOT ``<OptimizationParameters>``,
  which is optimizer grid config).
  -> :func:`load_strategy_template` -> :class:`StrategyTemplate`.

Both are parsed strictly by tag name — NT8 re-orders elements between saves,
so element position is meaningless. Values in ATM templates are ticks
(``CalculationMode=Ticks``); Currency mode and the chase/MIT/stop-limit/
reverse flags are parsed but rejected (not modeled in v1).

Properties absent from a strategy template keep the strategy's compiled-in
defaults on load (NT8 behavior) — callers merge ``props`` over their own
defaults, mirroring that.
"""
from __future__ import annotations

import xml.etree.ElementTree as ET
from dataclasses import dataclass
from pathlib import Path

_MISSING = object()

# ---------------------------------------------------------------------------
# ATM strategy templates
# ---------------------------------------------------------------------------


@dataclass(frozen=True)
class TrailStep:
    """One tier of an auto-trail schedule (all values in ticks)."""

    profit_trigger: int  # open profit at which this tier activates
    stop_loss: int       # trailing distance maintained once active
    frequency: int       # stop advances in increments of this many ticks


@dataclass(frozen=True)
class AtmBracket:
    """One exit leg: a slice of the position with its own target/stop."""

    qty: int
    stop_ticks: int
    target_ticks: int            # 0 = runner (no fixed target)
    be_trigger_ticks: int = 0    # 0 = no auto-breakeven
    be_plus_ticks: int = 0       # stop -> entry +/- this when BE fires
    trail_steps: tuple[TrailStep, ...] = ()


@dataclass(frozen=True)
class AtmSpec:
    name: str
    entry_qty: int
    brackets: tuple[AtmBracket, ...]

    def exit_split(self) -> tuple[AtmBracket, ...]:
        """Brackets with quantities reconciled to ``entry_qty``.

        NT8 fills ``EntryQuantity`` contracts regardless of what the bracket
        quantities sum to; the exit split absorbs any mismatch in the LAST
        bracket (dropping brackets that would go to zero or negative).
        """
        total = sum(b.qty for b in self.brackets)
        if total == self.entry_qty:
            return self.brackets
        out = list(self.brackets)
        diff = self.entry_qty - total
        while out:
            last = out[-1]
            q = last.qty + diff
            if q > 0:
                out[-1] = AtmBracket(q, last.stop_ticks, last.target_ticks,
                                     last.be_trigger_ticks, last.be_plus_ticks,
                                     last.trail_steps)
                break
            diff = q          # still short after removing this bracket
            out.pop()
        return tuple(out)


# ATM features we deliberately do not model yet: reject loudly rather than
# silently mis-simulate.
_UNSUPPORTED_ATM_FLAGS = (
    "IsChase", "IsChaseIfTouched", "IsTargetChase",
    "ReverseAtStop", "ReverseAtTarget",
    "UseMitForProfit", "UseStopLimitForStopLossOrders",
)


def _text(el: ET.Element | None, default: str = "") -> str:
    return el.text.strip() if el is not None and el.text else default


def _int(el: ET.Element | None, default: int = 0) -> int:
    t = _text(el)
    return int(float(t)) if t else default


def load_atm_template(path: str | Path) -> AtmSpec:
    """Parse an NT8 ATM strategy template XML into an :class:`AtmSpec`."""
    path = Path(path)
    root = ET.parse(path).getroot()
    atm = root.find("AtmStrategy")
    if atm is None:
        raise ValueError(
            f"{path.name}: not an ATM template (no <AtmStrategy>; a "
            f"<StrategyTemplate> root is a NinjaScript strategy template — "
            f"use load_strategy_template)")

    mode = _text(atm.find("CalculationMode"), "Ticks")
    if mode != "Ticks":
        raise NotImplementedError(
            f"{path.name}: CalculationMode={mode} not modeled (only Ticks)")
    for flag in _UNSUPPORTED_ATM_FLAGS:
        if _text(atm.find(flag)).lower() == "true":
            raise NotImplementedError(f"{path.name}: {flag}=true not modeled")

    brackets = []
    for br in atm.findall("Brackets/Bracket"):
        be_trigger = be_plus = 0
        steps: list[TrailStep] = []
        ss = br.find("StopStrategy")
        if ss is not None:
            be_trigger = _int(ss.find("AutoBreakEvenProfitTrigger"))
            be_plus = _int(ss.find("AutoBreakEvenPlus"))
            for st in ss.findall("AutoTrailSteps/AutoTrailStep"):
                steps.append(TrailStep(
                    profit_trigger=_int(st.find("ProfitTrigger")),
                    stop_loss=_int(st.find("StopLoss")),
                    frequency=max(1, _int(st.find("Frequency"), 1)),
                ))
            steps.sort(key=lambda s: s.profit_trigger)
        brackets.append(AtmBracket(
            qty=_int(br.find("Quantity")),
            stop_ticks=_int(br.find("StopLoss")),
            target_ticks=_int(br.find("Target")),
            be_trigger_ticks=be_trigger,
            be_plus_ticks=be_plus,
            trail_steps=tuple(steps),
        ))
    if not brackets:
        raise ValueError(f"{path.name}: ATM template has no <Bracket> entries")

    return AtmSpec(
        name=_text(atm.find("Template"), path.stem),
        entry_qty=_int(atm.find("EntryQuantity")),
        brackets=tuple(brackets),
    )


# ---------------------------------------------------------------------------
# NinjaScript strategy templates
# ---------------------------------------------------------------------------

# BarsPeriodTypeSerialize values -> BarSpec strings. 12345 is the registered
# custom-type id of ninZaRenko (Value=brick, Value2=trend); verified against
# the Terminator PK funded template (100/4 -> r100-4).
_BAR_TYPE_RENKO = 12345


@dataclass
class StrategyTemplate:
    strategy_type: str           # e.g. "...Strategies.Playr101.GodZillaKilla"
    props: dict[str, str]        # flat leaf tags from the <Strategy> block
    bar_spec: str | None         # e.g. "r60-3"; None if series not present
    instrument: str | None       # e.g. "MNQ 06-26"
    name: str                    # template file stem

    @property
    def symbol(self) -> str | None:
        """Instrument root, e.g. 'MNQ 06-26' -> 'MNQ'."""
        return self.instrument.split()[0] if self.instrument else None

    # -- typed accessors ----------------------------------------------------
    def get(self, prop: str, default=_MISSING) -> str:
        if prop in self.props:
            return self.props[prop]
        if default is _MISSING:
            raise KeyError(f"{self.name}: property {prop!r} not in template")
        return default

    def get_bool(self, prop: str, default=_MISSING) -> bool:
        v = self.get(prop, default)
        return v.strip().lower() == "true" if isinstance(v, str) else bool(v)

    def get_int(self, prop: str, default=_MISSING) -> int:
        v = self.get(prop, default)
        return int(float(v)) if isinstance(v, str) else int(v)

    def get_float(self, prop: str, default=_MISSING) -> float:
        v = self.get(prop, default)
        return float(v)

    def get_time(self, prop: str, default=_MISSING) -> str:
        """DateTime property -> 'HH:MM' wall-clock (NT8 stores a full date)."""
        v = self.get(prop, default)
        if not isinstance(v, str):
            return v
        clock = v.split("T", 1)[1] if "T" in v else v
        return clock[:5]


def _parse_bar_spec(el: ET.Element, name: str) -> str:
    type_id = _int(el.find("BarsPeriodTypeSerialize"), -1)
    value = _int(el.find("Value"))
    value2 = _int(el.find("Value2"))
    if type_id == _BAR_TYPE_RENKO:
        return f"r{value}-{value2}"
    raise ValueError(
        f"{name}: BarsPeriodTypeSerialize={type_id} not mapped to a BarSpec "
        f"(known: {_BAR_TYPE_RENKO}=ninZaRenko). Add the mapping when needed.")


def load_strategy_template(path: str | Path) -> StrategyTemplate:
    """Parse an NT8 NinjaScript strategy template XML.

    Returns the flat property block; properties NOT present keep the caller's
    defaults (that is exactly what NT8 does when loading an old template into
    a newer strategy build — note this can silently enable newer features).
    """
    path = Path(path)
    root = ET.parse(path).getroot()
    if root.tag != "StrategyTemplate":
        raise ValueError(
            f"{path.name}: not a strategy template (root <{root.tag}>; an "
            f"<NinjaTrader><AtmStrategy> file is an ATM template — use "
            f"load_atm_template)")

    strategy_type = _text(root.find("StrategyType"))
    holder = root.find("Strategy")
    body = holder[0] if holder is not None and len(holder) else None
    if body is None:
        raise ValueError(f"{path.name}: no <Strategy> property block")

    props: dict[str, str] = {}
    bar_spec = None
    for child in body:
        if child.tag == "BarsPeriodSerializable":
            bar_spec = _parse_bar_spec(child, path.name)
        elif len(child) == 0:                       # leaf property
            props[child.tag] = _text(child)

    return StrategyTemplate(
        strategy_type=strategy_type,
        props=props,
        bar_spec=bar_spec,
        instrument=props.get("InstrumentOrInstrumentList") or None,
        name=path.stem,
    )
