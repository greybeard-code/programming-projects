"""ATM bracket execution: turn an :class:`~backtester.nt8config.AtmSpec`
into broker exit orders.

NT8 ATM semantics reproduced here:

* Each ``<Bracket>`` is an independent exit leg — its own target limit and
  stop, OCO-paired per leg (bracket 2's target filling cancels bracket 2's
  stop, nobody else's).
* ``Target=0`` = runner: stop only, no target order.
* Auto-breakeven / auto-trail live on the leg's stop as a
  :class:`~backtester.orders.StopPlan` (level ratchets only, favorable
  ticks from entry; trail advances in Frequency increments anchored at the
  tier's ProfitTrigger).
* Quantities come from ``AtmSpec.exit_split()`` (EntryQuantity authoritative,
  last bracket absorbs any mismatch).

The strategy submits the entry itself (market, qty = ``spec.entry_qty``) and
calls :func:`submit_atm_exits` from its ``on_fill``.
"""
from __future__ import annotations

from .nt8config import AtmSpec
from .orders import Order, OrderType, StopPlan, TrailRule


def atm_exit_orders(spec: AtmSpec, side: int, entry_price: float,
                    tick_size: float) -> list[Order]:
    """Build the exit legs for a filled entry (side=+1 long / -1 short)."""
    exit_side = -side
    orders: list[Order] = []
    for n, b in enumerate(spec.exit_split(), 1):
        plan = None
        if b.be_trigger_ticks or b.trail_steps:
            plan = StopPlan(
                entry_price, side, tick_size, b.stop_ticks,
                be_trigger_ticks=b.be_trigger_ticks,
                be_plus_ticks=b.be_plus_ticks,
                trails=tuple(TrailRule(s.profit_trigger, s.stop_loss,
                                       s.frequency) for s in b.trail_steps),
            )
        stop = Order(side=exit_side, qty=b.qty, type=OrderType.STOP,
                     price=entry_price - side * b.stop_ticks * tick_size,
                     tag=f"atm-stop{n}", is_exit=True, plan=plan)
        orders.append(stop)
        if b.target_ticks:            # 0 = runner (no fixed target)
            tgt = Order(side=exit_side, qty=b.qty, type=OrderType.LIMIT,
                        price=entry_price + side * b.target_ticks * tick_size,
                        tag=f"atm-target{n}", is_exit=True)
            stop.oco_id, tgt.oco_id = tgt.id, stop.id
            orders.append(tgt)
    return orders


def submit_atm_exits(broker, spec: AtmSpec, side: int,
                     entry_price: float) -> list[Order]:
    """Build and submit the legs against a live broker."""
    orders = atm_exit_orders(spec, side, entry_price, broker.spec.tick_size)
    for o in orders:
        broker.submit(o)
    return orders
