"""God Trades (TraderOracle) port — candle-gap reversal system, NQ 1000-tick.

Sources: NinjaScript/TOGodMode/NT8 code/GodTrades21.cs (signal engine
semantics — gap lifecycle, BG/FC/OBR conditions and defaults) plus the
"The God Trades" Canva deck and masterclass video (exit model: stop at the
back of the signal candle, take profit at the OPPOSITE Bollinger band,
never move to break-even).

Three signal types, independently toggleable:

* BG  — Bollinger Gap: two same-direction candles with a body gap between
  them, both hugging the Bollinger band the gap points AWAY from (a gap-up
  at the lower band / gap-down at the upper band). Momentum entry in the
  gap direction on the second candle's close.
* FC  — Fill continuation ("Fill & Reverse" / "Fill & Continue" in the
  deck): an old gap line (>= min_gap_age_bars candles old) is touched from
  the correct side; within confirm_bars a candle in the gap's direction
  closes beyond the zone -> enter in the gap direction.
* OBR — Outside-bar reversal: an opposite-direction BODY engulf at the
  Bollinger band edge.

The signal logic lives in :class:`GapSignalEngine` (pure, bar-in/signal-out)
so unit tests can drive it with synthetic bars; :class:`GodTrades` wires it
to the broker with the methodology-faithful exits.

Session is the full Globex day so gap lines accumulate exactly as they do
on the streamer's 24h chart; NEW entries are gated to entry_window
(10:15-15:00 ET per the deck) and open positions are flattened when the
window closes.
"""
from __future__ import annotations

import math
from dataclasses import dataclass
from datetime import datetime
from zoneinfo import ZoneInfo

from backtester.indicators import Bollinger
from backtester.orders import Order, OrderType
from backtester.strategy import Strategy

ET = ZoneInfo("America/New_York")


def _mins(hhmm: str) -> int:
    h, m = map(int, hhmm.split(":"))
    return h * 60 + m


@dataclass
class GapLine:
    direction: int          # +1 bullish gap-up, -1 bearish gap-down
    created: int            # engine bar index of the gap (second) candle
    zone_low: float
    zone_high: float

    @property
    def line(self) -> float:
        return (self.zone_low + self.zone_high) * 0.5


@dataclass
class Signal:
    direction: int          # +1 long / -1 short
    source: str             # "BG" | "FC" | "OBR"
    stop: float             # back of the signal candle (+/- offset)
    target: float           # opposite Bollinger band at signal time (nan if band mode off)


@dataclass
class _Pending:
    gap: GapLine
    touch_bar: int


class GapSignalEngine:
    """GodTrades21.cs signal semantics, one update(o,h,l,c[,delta]) per
    closed bar. Returns a :class:`Signal` or None. Same-bar long+short
    conflicts return None (the NT8 strategy ignores conflicting bars)."""

    def __init__(self, tick_size: float, *,
                 min_gap_ticks: int = 1,
                 min_gap_age_bars: int = 3,
                 max_active_gaps: int = 300,
                 ignore_early_touches: bool = False,
                 enable_bg: bool = True,
                 enable_fc: bool = True,
                 enable_obr: bool = True,
                 bb_period: int = 20,
                 bb_stddev: float = 2.0,
                 bb_proximity_ticks: int = 8,
                 confirm_bars: int = 2,
                 confirm_mode: str = "zone",       # "touch" | "line" | "zone"
                 require_signal_candle_direction: bool = True,
                 require_correct_approach: bool = True,
                 midpoint_filter: bool = True,
                 fc_long_below_mid_pct: float = 50.0,
                 fc_short_above_mid_pct: float = 50.0,
                 obr_allow_outside_band: bool = True,
                 obr_band_tolerance_ticks: int = 4,
                 spiderweb_distance_ticks: int = 100,
                 spiderweb_line_count: int = 5,
                 stop_offset_ticks: int = 0,
                 # ---- video quality filters (0/False = off) ----
                 max_doji_body_frac: float = 0.0,   # reject body/range < frac
                 shaved_close_frac: float = 0.0,    # close within frac*range of extreme
                 require_engulf: bool = False,      # signal body engulfs prior body
                 require_delta_sign: bool = False): # signal bar delta agrees
        self.tick = tick_size
        self.min_gap_ticks = min_gap_ticks
        self.min_gap_age_bars = min_gap_age_bars
        self.max_active_gaps = max_active_gaps
        self.ignore_early_touches = ignore_early_touches
        self.enable_bg = enable_bg
        self.enable_fc = enable_fc
        self.enable_obr = enable_obr
        self.bb = Bollinger(bb_period, bb_stddev)
        self.bb_proximity_ticks = bb_proximity_ticks
        self.confirm_bars = confirm_bars
        self.confirm_mode = confirm_mode
        self.require_signal_candle_direction = require_signal_candle_direction
        self.require_correct_approach = require_correct_approach
        self.midpoint_filter = midpoint_filter
        self.fc_long_below_mid_pct = fc_long_below_mid_pct
        self.fc_short_above_mid_pct = fc_short_above_mid_pct
        self.obr_allow_outside_band = obr_allow_outside_band
        self.obr_band_tolerance_ticks = obr_band_tolerance_ticks
        self.spiderweb_distance_ticks = spiderweb_distance_ticks
        self.spiderweb_line_count = spiderweb_line_count
        self.stop_offset_ticks = stop_offset_ticks
        self.max_doji_body_frac = max_doji_body_frac
        self.shaved_close_frac = shaved_close_frac
        self.require_engulf = require_engulf
        self.require_delta_sign = require_delta_sign

        self.gaps: list[GapLine] = []
        self.pending: list[_Pending] = []
        self.index = -1                     # engine bar counter
        self._prev = None                   # (o, h, l, c) of prior bar
        self._prev_upper = math.nan
        self._prev_lower = math.nan

    # ---- public per-bar state -------------------------------------------
    def spiderweb_count(self, ref_price: float) -> int:
        """Valid gap lines within spiderweb_distance_ticks of ref_price."""
        dist = self.spiderweb_distance_ticks * self.tick
        return sum(1 for g in self.gaps
                   if self.index - g.created >= self.min_gap_age_bars
                   and abs(g.line - ref_price) <= dist)

    @property
    def spiderweb(self) -> bool:
        if self._prev is None:
            return False
        return (self.spiderweb_count(self._last_close)
                >= self.spiderweb_line_count)

    # ---- update ----------------------------------------------------------
    def update(self, o: float, h: float, l: float, c: float,
               delta: int = 0) -> Signal | None:
        self.index += 1
        prev_upper, prev_lower = self.bb.upper, self.bb.lower
        self.bb.update(c)
        self._last_close = c

        signals: list[Signal] = []
        if self._prev is not None:
            bar = (o, h, l, c, delta)
            signals += self._eval_pending(bar)
            signals += self._update_gaps(bar)
            new_gap_dir = self._detect_new_gap(bar, prev_upper, prev_lower,
                                               signals)
            if not signals and new_gap_dir == 0:
                obr = self._detect_obr(bar)
                if obr is not None:
                    signals.append(obr)
        self._prev = (o, h, l, c)
        self._prev_upper, self._prev_lower = prev_upper, prev_lower
        while len(self.gaps) > self.max_active_gaps:
            self.gaps.pop(0)

        if not signals:
            return None
        dirs = {s.direction for s in signals}
        if len(dirs) > 1:
            return None                     # conflicting long+short bar
        for src in ("BG", "FC", "OBR"):     # NT8 code precedence for labeling
            for s in signals:
                if s.source == src:
                    return s
        return signals[0]

    # ---- gap lifecycle ---------------------------------------------------
    def _detect_new_gap(self, bar, prev_upper, prev_lower,
                        signals: list[Signal]) -> int:
        o, h, l, c, delta = bar
        po, ph, pl, pc = self._prev
        prev_bull, prev_bear = pc > po, pc < po
        cur_bull, cur_bear = c > o, c < o
        min_gap = self.min_gap_ticks * self.tick

        direction = 0
        if prev_bull and cur_bull and o > pc and (o - pc) >= min_gap:
            direction = 1
            zone_low, zone_high = pc, o
        elif prev_bear and cur_bear and o < pc and (pc - o) >= min_gap:
            direction = -1
            zone_low, zone_high = o, pc
        if direction == 0:
            return 0

        self.gaps.append(GapLine(direction, self.index, zone_low, zone_high))

        # BG: gap forming while both bars hug the band the gap points away
        # from (GodTrades21.MarkBollingerGapIfNeeded)
        if self.enable_bg and self.bb.ready and not math.isnan(prev_upper):
            prox = self.bb_proximity_ticks * self.tick
            if direction > 0:
                near = (pl <= prev_lower + prox and l <= self.bb.lower + prox)
            else:
                near = (ph >= prev_upper - prox and h >= self.bb.upper - prox)
            if near and self._passes_quality(bar, direction):
                signals.append(self._make_signal(direction, "BG", bar))
        return direction

    def _update_gaps(self, bar) -> list[Signal]:
        o, h, l, c, delta = bar
        out: list[Signal] = []
        for i in range(len(self.gaps) - 1, -1, -1):
            gap = self.gaps[i]
            if gap.created >= self.index:
                continue
            valid = self.index - gap.created >= self.min_gap_age_bars
            if not valid and self.ignore_early_touches:
                continue
            touched = h >= gap.zone_low and l <= gap.zone_high
            if not touched:
                continue
            if valid and self.enable_fc and self._passes_approach(gap, bar):
                pend = _Pending(gap, self.index)
                sig = self._eval_continuation(pend, bar)
                if sig is not None:
                    out.append(sig)
                elif self.confirm_bars > 0:
                    self.pending.append(pend)
            self.gaps.pop(i)                # touched lines always retire
        return out

    def _passes_approach(self, gap: GapLine, bar) -> bool:
        if not self.require_correct_approach:
            return True
        o, h, l, c, delta = bar
        po, ph, pl, pc = self._prev
        if gap.direction > 0:
            return pc >= gap.zone_high or o >= gap.zone_high
        return pc <= gap.zone_low or o <= gap.zone_low

    # ---- FC confirmation ---------------------------------------------------
    def _eval_pending(self, bar) -> list[Signal]:
        out: list[Signal] = []
        for i in range(len(self.pending) - 1, -1, -1):
            p = self.pending[i]
            age = self.index - p.touch_bar
            if age <= 0:
                continue
            if age > self.confirm_bars:
                self.pending.pop(i)
                continue
            sig = self._eval_continuation(p, bar)
            if sig is not None:
                out.append(sig)
                self.pending.pop(i)
        return out

    def _eval_continuation(self, p: _Pending, bar) -> Signal | None:
        o, h, l, c, delta = bar
        d = p.gap.direction
        if self.require_signal_candle_direction:
            if d > 0 and c <= o:
                return None
            if d < 0 and c >= o:
                return None
        if not self._passes_midpoint(d, bar):
            return None
        if self.confirm_mode == "line":
            ok = c >= p.gap.line if d > 0 else c <= p.gap.line
        elif self.confirm_mode == "zone":
            ok = c >= p.gap.zone_high if d > 0 else c <= p.gap.zone_low
        else:                               # "touch"
            ok = True
        if not ok or not self._passes_quality(bar, d):
            return None
        return self._make_signal(d, "FC", bar)

    def _passes_midpoint(self, direction: int, bar) -> bool:
        """Wick-extreme location filter (GodTrades21 midpoint filter,
        WickExtreme source, pct toward the entry-side band)."""
        if not self.midpoint_filter:
            return True
        if not self.bb.ready:
            return False
        o, h, l, c, delta = bar
        mid, up, lo = self.bb.middle, self.bb.upper, self.bb.lower
        if direction > 0:
            threshold = mid - (mid - lo) * (self.fc_long_below_mid_pct / 100.0)
            return l <= threshold
        threshold = mid + (up - mid) * (self.fc_short_above_mid_pct / 100.0)
        return h >= threshold

    # ---- OBR ---------------------------------------------------------------
    def _detect_obr(self, bar) -> Signal | None:
        if not self.enable_obr or not self.bb.ready:
            return None
        o, h, l, c, delta = bar
        po, ph, pl, pc = self._prev
        prev_bull, prev_bear = pc > po, pc < po
        cur_bull, cur_bear = c > o, c < o
        tol = self.obr_band_tolerance_ticks * self.tick

        bear_engulf = prev_bull and cur_bear and o >= pc and c <= po
        bull_engulf = prev_bear and cur_bull and o <= pc and c >= po

        if bear_engulf:
            at_band = (h >= self.bb.upper - tol
                       and (self.obr_allow_outside_band or h <= self.bb.upper))
            if (at_band and self._passes_midpoint(-1, bar)
                    and self._passes_quality(bar, -1)):
                return self._make_signal(-1, "OBR", bar)
        elif bull_engulf:
            at_band = (l <= self.bb.lower + tol
                       and (self.obr_allow_outside_band or l >= self.bb.lower))
            if (at_band and self._passes_midpoint(1, bar)
                    and self._passes_quality(bar, 1)):
                return self._make_signal(1, "OBR", bar)
        return None

    # ---- quality filters (masterclass video; all off by default) -----------
    def _passes_quality(self, bar, direction: int) -> bool:
        o, h, l, c, delta = bar
        rng = h - l
        body = abs(c - o)
        if self.max_doji_body_frac > 0:
            if rng <= 0 or body / rng < self.max_doji_body_frac:
                return False
        if self.shaved_close_frac > 0 and rng > 0:
            wick = (h - c) if direction > 0 else (c - l)
            if wick / rng > self.shaved_close_frac:
                return False
        if self.require_engulf:
            po, ph, pl, pc = self._prev
            if body <= abs(pc - po):
                return False
        if self.require_delta_sign and delta * direction <= 0:
            return False
        return True

    def _make_signal(self, direction: int, source: str, bar) -> Signal:
        o, h, l, c, delta = bar
        off = self.stop_offset_ticks * self.tick
        stop = (l - off) if direction > 0 else (h + off)
        if self.bb.ready:
            target = self.bb.upper if direction > 0 else self.bb.lower
        else:
            target = math.nan
        return Signal(direction, source, stop, target)


class GodTrades(Strategy):
    symbol = "NQ"
    period = "1000t"                 # deck: NQ 1000 tick (ES would be 2000t)
    session = ("18:00", "16:55")     # full Globex day: lines accumulate 24h
    flat_at_session_end = True
    qty = 1

    # ---- entry window (ET) ----
    entry_window = ("10:15", "15:00")   # deck: only trade 10:15am-3pm ET
    flatten_at_window_end = True

    # ---- signal selection ----
    enable_bg = True
    enable_fc = True
    enable_obr = True

    # ---- gap / signal parameters (GodTrades21 defaults) ----
    min_gap_ticks = 1
    min_gap_age_bars = 3
    max_active_gaps = 300
    ignore_early_touches = False
    bb_period = 20
    bb_stddev = 2.0
    bb_proximity_ticks = 8
    confirm_bars = 2
    confirm_mode = "zone"            # "touch" | "line" | "zone"
    require_signal_candle_direction = True
    require_correct_approach = True
    midpoint_filter = True
    fc_long_below_mid_pct = 50.0
    fc_short_above_mid_pct = 50.0
    obr_allow_outside_band = True
    obr_band_tolerance_ticks = 4

    # ---- spiderweb stand-down (indicator only warns; here it can gate) ----
    spiderweb_suppress = False
    spiderweb_distance_ticks = 100
    spiderweb_line_count = 5

    # ---- video quality filters (0/False = off) ----
    max_doji_body_frac = 0.0
    shaved_close_frac = 0.0
    require_engulf = False
    require_delta_sign = False

    # ---- exits ----
    exit_mode = "band"               # "band" (methodology) | "fixed"
    stop_offset_ticks = 0
    track_band_target = True         # re-price target to the band each bar
    fixed_sl_ticks = 30              # exit_mode="fixed" only
    fixed_tp_ticks = 40

    # ---- stop-hunt re-entry (video: "I'll always re-enter after being
    # stopped" if the original entry price prints again) ----
    reenter_after_stop = False
    reenter_window_bars = 10

    def on_start(self):
        self.engine = GapSignalEngine(
            self._broker.spec.tick_size,
            min_gap_ticks=self.min_gap_ticks,
            min_gap_age_bars=self.min_gap_age_bars,
            max_active_gaps=self.max_active_gaps,
            ignore_early_touches=self.ignore_early_touches,
            enable_bg=self.enable_bg,
            enable_fc=self.enable_fc,
            enable_obr=self.enable_obr,
            bb_period=self.bb_period,
            bb_stddev=self.bb_stddev,
            bb_proximity_ticks=self.bb_proximity_ticks,
            confirm_bars=self.confirm_bars,
            confirm_mode=self.confirm_mode,
            require_signal_candle_direction=self.require_signal_candle_direction,
            require_correct_approach=self.require_correct_approach,
            midpoint_filter=self.midpoint_filter,
            fc_long_below_mid_pct=self.fc_long_below_mid_pct,
            fc_short_above_mid_pct=self.fc_short_above_mid_pct,
            obr_allow_outside_band=self.obr_allow_outside_band,
            obr_band_tolerance_ticks=self.obr_band_tolerance_ticks,
            spiderweb_distance_ticks=self.spiderweb_distance_ticks,
            spiderweb_line_count=self.spiderweb_line_count,
            stop_offset_ticks=self.stop_offset_ticks,
            max_doji_body_frac=self.max_doji_body_frac,
            shaved_close_frac=self.shaved_close_frac,
            require_engulf=self.require_engulf,
            require_delta_sign=self.require_delta_sign,
        )
        self._entry_ctx: Signal | None = None
        self._was_in_window = False
        self._reentry_dir = 0
        self._reentry_price = math.nan
        self._reentry_bar = -1
        self._cur_bar = -1
        self._last_entry_price = math.nan
        self._last_entry_dir = 0

    # ------------------------------------------------------------------
    def _tod(self, ts_ns: int) -> int:
        t = datetime.fromtimestamp(ts_ns / 1e9, ET)
        return t.hour * 60 + t.minute

    def _in_window(self, tod: int) -> bool:
        s, e = _mins(self.entry_window[0]), _mins(self.entry_window[1])
        return s <= tod < e

    def on_bar(self, bar, bars):
        self._cur_bar = bar.index
        sig = self.engine.update(bar.open, bar.high, bar.low, bar.close,
                                 bar.delta)
        tod = self._tod(bar.ts)
        in_window = self._in_window(tod)

        # flatten when the trading window closes (deck: 10:15-15:00 only)
        if (self.flatten_at_window_end and self._was_in_window
                and not in_window):
            self.cancel_all()
            self.close_position(tag="window-flat", force=True)
            self._reentry_dir = 0
        self._was_in_window = in_window

        # band-to-band target tracks the (moving) opposite band
        if (not self.flat and self.exit_mode == "band"
                and self.track_band_target and self.engine.bb.ready):
            band = (self.engine.bb.upper if self.position > 0
                    else self.engine.bb.lower)
            self.move_target(band)

        if not in_window:
            return

        # stop-hunt re-entry: original entry price prints again soon after
        # a stop-out -> re-enter, stop at this candle's back
        if (self.reenter_after_stop and self._reentry_dir != 0 and self.flat
                and not self.working_orders):
            if bar.index - self._reentry_bar > self.reenter_window_bars:
                self._reentry_dir = 0
            elif (bar.low <= self._reentry_price <= bar.high
                  and (bar.close - bar.open) * self._reentry_dir > 0):
                d = self._reentry_dir
                self._reentry_dir = 0
                off = self.stop_offset_ticks * self._broker.spec.tick_size
                stop = bar.low - off if d > 0 else bar.high + off
                target = (self.engine.bb.upper if d > 0
                          else self.engine.bb.lower)
                self._submit_entry(Signal(d, "RE", stop, target))
                return

        if sig is None or not self.flat or self.working_orders:
            return
        if self.spiderweb_suppress and self.engine.spiderweb:
            return
        self._submit_entry(sig)

    def _submit_entry(self, sig: Signal) -> None:
        self._entry_ctx = sig
        tag = f"gt-{sig.source}-{'long' if sig.direction > 0 else 'short'}"
        if sig.direction > 0:
            self.buy(tag=tag)
        else:
            self.sell(tag=tag)

    # ------------------------------------------------------------------
    def on_fill(self, fill):
        o = fill.order
        if not o.is_exit and o.tag.startswith("gt-"):
            ctx = self._entry_ctx
            if ctx is None:
                return
            self._last_entry_price = fill.price
            self._last_entry_dir = ctx.direction
            side = ctx.direction
            tick = self._broker.spec.tick_size
            if self.exit_mode == "band" and not math.isnan(ctx.target):
                stop_px, tgt_px = ctx.stop, ctx.target
            else:
                stop_px = fill.price - side * self.fixed_sl_ticks * tick
                tgt_px = fill.price + side * self.fixed_tp_ticks * tick
            stop = Order(side=-side, qty=fill.qty, type=OrderType.STOP,
                         price=stop_px, tag="gt-stop", is_exit=True)
            tgt = Order(side=-side, qty=fill.qty, type=OrderType.LIMIT,
                        price=tgt_px, tag="gt-target", is_exit=True)
            stop.oco_id, tgt.oco_id = tgt.id, stop.id
            self._broker.submit(stop)
            self._broker.submit(tgt)
            self._entry_ctx = None
        elif (o.is_exit and o.type is OrderType.STOP
              and self.reenter_after_stop and self._last_entry_dir != 0):
            self._reentry_dir = self._last_entry_dir
            self._reentry_price = self._last_entry_price
            self._reentry_bar = self._cur_bar
