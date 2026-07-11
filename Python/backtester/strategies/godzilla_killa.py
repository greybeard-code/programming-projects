"""GodZillaKilla v1.10.0 port — six-engine confluence voting strategy.

Source: nt8 code/GodZillaKilla/GodZillaKilla.cs (signal aggregation
3900-4141 + 1500-1662, windows 4641-4905, ATM dispatch 5256+/6361+).

Per closed bar, six GreyBeard signal engines each emit an integer code;
per-source operator/value rules turn codes into long/short votes; a group
trigger set fires when >= `required` enabled sources agree (an enabled
source flagged `require` that did NOT agree vetoes the fire; simultaneous
long+short fires abort). Two independent sets (Set 1 priority) OR together.
Optional EMA(21/50) filter and a ConfirmationBars deferred re-fire gate the
entry; TF1-3 time windows (+ skip window) gate NEW entries (per-window
flatten flags force-flat when their window closes); reversal-on-opposite-
signal is always active (arm buttons modeled as always-on).

Exits: an NT8 ATM template parsed from XML (multi-bracket scale-out,
auto-BE, trailing) or FixedTicks mode (modeled as a synthetic single-bracket
ATM so breakeven stays tick-accurate). Daily profit target / loss limit are
engine-level stand-downs (read from the strategy attributes below).

Times are "HH:MM" US/Eastern. Configure from a saved NT8 strategy template:

    strat = GodZillaKilla.from_template(r"...\\OneSet_3ofAll_BestTime.xml")

Missing template properties keep v1.10 defaults (NT8 load semantics) — the
effective enabled set is printed so an old preset silently enabling a newer
engine (e.g. NobleCloud) is never a surprise.
"""
from __future__ import annotations

from datetime import datetime
from pathlib import Path
from zoneinfo import ZoneInfo

from backtester.atm import submit_atm_exits
from backtester.gbsignals import (
    KingOrderBlock, NobleCloud, PanaKanal, SuperJumpBoost, SumoPullback,
    ThunderZilla,
)
from backtester.gbsignals.nt8math import Nt8Ema
from backtester.nt8config import (
    AtmBracket, AtmSpec, load_atm_template, load_strategy_template,
)
from backtester.strategy import Strategy

ET = ZoneInfo("America/New_York")
SOURCES = ("ko", "pa", "th", "sj", "su", "nc")

# NT8 SignalComparisonOperator (GZK.cs 4190-4214)
_OPS = {
    "Equal": lambda a, b: a == b,
    "GreaterOrEqual": lambda a, b: a >= b,
    "GreaterThan": lambda a, b: a > b,
    "LessOrEqual": lambda a, b: a <= b,
    "LessThan": lambda a, b: a < b,
    "NotEqual": lambda a, b: a != b,
}

_ATM_DIR = Path.home() / "Documents" / "NinjaTrader 8" / "templates" / "AtmStrategy"


def _mins(hhmm: str) -> int:
    h, m = map(int, hhmm.split(":"))
    return h * 60 + m


def _in_window(tod: int, win: tuple[str, str]) -> bool:
    s, e = _mins(win[0]), _mins(win[1])
    return (s <= tod <= e) if s <= e else (tod >= s or tod <= e)


class GodZillaKilla(Strategy):
    symbol = "MNQ"
    period = "r60-3"
    # one CME trading day; the session template (not the windows) enforces
    # the flat-by-16:55 rule, exactly as in NT8
    session = ("18:00", "16:55")
    flat_at_session_end = True

    # ---- order management (GZK "ATM Parameters") ----
    order_mode = "atm"            # "atm" | "fixed"
    atm_template = ""             # name (resolved in atm_dir) or full path
    atm_dir: str | None = None    # None -> NT8 templates\AtmStrategy
    fixed_qty = 4
    fixed_sl_ticks = 75
    fixed_tp_ticks = 25
    fixed_be_enabled = False
    fixed_be_trigger_ticks = 25
    fixed_be_offset_ticks = 1

    # ---- signals: Set 1 ----
    confirmation_bars = 0
    set1_required = 1
    use_ko, require_ko = True, False
    ko_long, ko_short = ("GreaterOrEqual", 1), ("LessOrEqual", -1)
    use_pa, require_pa = True, False
    pa_long, pa_short = ("GreaterOrEqual", 2), ("LessOrEqual", -2)
    use_th, require_th = True, False
    th_long, th_short = ("GreaterOrEqual", 2), ("LessOrEqual", -2)
    use_sj, require_sj = True, False
    sj_long, sj_short = ("GreaterOrEqual", 1), ("LessOrEqual", -1)
    use_su, require_su = True, False
    su_long, su_short = ("GreaterOrEqual", 1), ("LessOrEqual", -1)
    use_nc, require_nc = True, False
    nc_long, nc_short = ("GreaterOrEqual", 1), ("LessOrEqual", -1)

    # ---- signals: Set 2 (independent copy, disabled by default) ----
    set2_enabled = False
    set2_required = 3
    g2_use_ko, g2_require_ko = True, False
    g2_ko_long, g2_ko_short = ("GreaterOrEqual", 1), ("LessOrEqual", -1)
    g2_use_pa, g2_require_pa = True, False
    g2_pa_long, g2_pa_short = ("GreaterOrEqual", 3), ("LessOrEqual", -3)
    g2_use_th, g2_require_th = True, False
    g2_th_long, g2_th_short = ("GreaterOrEqual", 3), ("LessOrEqual", -3)
    g2_use_sj, g2_require_sj = True, False
    g2_sj_long, g2_sj_short = ("GreaterOrEqual", 1), ("LessOrEqual", -1)
    g2_use_su, g2_require_su = True, False
    g2_su_long, g2_su_short = ("GreaterOrEqual", 1), ("LessOrEqual", -1)
    g2_use_nc, g2_require_nc = True, False
    g2_nc_long, g2_nc_short = ("GreaterOrEqual", 1), ("LessOrEqual", -1)

    # ---- filters ----
    ema_filter = False
    ema_short_period = 21
    ema_long_period = 50

    # ---- trading windows (ET), per-window flatten, skip window ----
    tf1_enabled, tf1, tf1_flatten = True, ("19:00", "23:30"), False
    tf2_enabled, tf2, tf2_flatten = True, ("03:00", "09:00"), False
    tf3_enabled, tf3, tf3_flatten = True, ("08:00", "03:45"), True
    skip_enabled, skip_window = True, ("11:45", "13:00")

    # ---- risk (engine-level stand-downs; read by Backtest) ----
    daily_loss_limit: float | None = None
    daily_profit_target: float | None = None

    # ---- behavior ----
    reverse_on_signal = True      # RBro buttons modeled as always armed

    # ---- per-engine parameters (GZK SetDefaults 762-843) ----
    king_params = dict(swing_point_neighborhood=5, imbalance_qualifying=3,
                       finding_boschoch_period=50, order_block_age=500,
                       same_direction_offset=10, difference_direction_offset=10,
                       signal_qty_per_order_block=3, signal_split_bars=6)
    pana_params = dict(period=20, factor=4.0, middle_period=14,
                       signal_break_split=20, signal_pullback_finding_period=10)
    thunder_params = dict(trend_ma_type="SMA", trend_period=200,
                          stop_offset_mult=60.0, signal_qty_per_flat=2,
                          signal_qty_per_trend=999)
    su_params = dict(slow_ma_type="SMA", slow_ma_period=60,
                     fast_ma1_type="EMA", fast_ma1_period=14,
                     fast_ma2_type="EMA", fast_ma2_period=30,
                     fast_ma3_type="EMA", fast_ma3_period=45,
                     signal_split_first=15, signal_split_second=30)
    nc_params = dict(sensitivity=60.0, smoothness=1,
                     baseline_ma_type="SMA", baseline_period=60,
                     baseline_smoothing_enabled=True,
                     baseline_smoothing_method="EMA",
                     baseline_smoothing_period=60,
                     kernel_ma_type="SMA", kernel_period=20,
                     kernel_smoothing_enabled=True,
                     kernel_smoothing_method="EMA", kernel_smoothing_period=5,
                     signal_split=5, filter_enabled=True,
                     filter_bar_min=10, filter_bar_max=300)
    sj_params = dict(sensitive_mode=True, offset_level1=1.0, offset_level2=2.0,
                     offset_level3=3.0, offset_level4=4.0, offset_base=4.0,
                     reference_price_period=2, line_levels_offset=100,
                     signal_close_threshold=70, signal_qty_per_zone=2,
                     signal_split=20)

    # ------------------------------------------------------------------
    def on_start(self):
        tick = self._broker.spec.tick_size
        need = {s: (getattr(self, f"use_{s}")
                    or (self.set2_enabled and getattr(self, f"g2_use_{s}")))
                for s in SOURCES}
        self._engines = {}
        if need["ko"]:
            self._engines["ko"] = KingOrderBlock(tick_size=tick,
                                                 **self.king_params)
        if need["pa"]:
            self._engines["pa"] = PanaKanal(tick_size=tick, **self.pana_params)
        if need["th"]:
            self._engines["th"] = ThunderZilla(tick_size=tick,
                                               **self.thunder_params)
        if need["sj"]:
            self._engines["sj"] = SuperJumpBoost(tick_size=tick,
                                                 **self.sj_params)
        if need["su"]:
            self._engines["su"] = SumoPullback(**self.su_params)
        if need["nc"]:
            self._engines["nc"] = NobleCloud(**self.nc_params)

        self._atm: AtmSpec | None = None
        if self.order_mode == "atm":
            self._atm = self._resolve_atm()
        else:                       # FixedTicks as a synthetic one-bracket ATM
            self._atm = AtmSpec(name="fixed", entry_qty=self.fixed_qty,
                                brackets=(AtmBracket(
                                    qty=self.fixed_qty,
                                    stop_ticks=self.fixed_sl_ticks,
                                    target_ticks=self.fixed_tp_ticks,
                                    be_trigger_ticks=(self.fixed_be_trigger_ticks
                                                      if self.fixed_be_enabled
                                                      else 0),
                                    be_plus_ticks=(self.fixed_be_offset_ticks
                                                   if self.fixed_be_enabled
                                                   else 0)),))
        self._ema_s = Nt8Ema(self.ema_short_period) if self.ema_filter else None
        self._ema_l = Nt8Ema(self.ema_long_period) if self.ema_filter else None
        self._pending_dir = 0        # ConfirmationBars deferral
        self._pending_bar = -1
        self._pending_close = 0.0
        self._prev_in_flatten = {}   # window name -> bool
        enabled = [s.upper() for s in SOURCES if getattr(self, f"use_{s}")]
        print(f"  [GZK] Set 1: {self.set1_required} of {enabled}"
              + (f" | Set 2 ON ({self.set2_required})" if self.set2_enabled
                 else "")
              + f" | exits: {self._atm.name} x{self._atm.entry_qty}")

    def _resolve_atm(self) -> AtmSpec:
        name = self.atm_template
        if not name:
            raise ValueError("order_mode='atm' needs atm_template")
        # template names may contain dots ("6QTY25.50.100...") — don't trust
        # Path.suffix, just ensure the .xml extension
        if not name.lower().endswith(".xml"):
            name += ".xml"
        p = Path(name)
        if not p.is_absolute():
            base = Path(self.atm_dir) if self.atm_dir else _ATM_DIR
            local = (Path(__file__).resolve().parent.parent / "nt8 code"
                     / "GodZillaKilla" / "templates" / p.name)
            p = local if local.exists() else base / p.name
        return load_atm_template(p)

    # ------------------------------------------------------------------
    @classmethod
    def from_template(cls, path, atm_dir: str | None = None):
        """Build a configured strategy from a saved NT8 strategy template."""
        t = load_strategy_template(path)
        s = cls()
        if t.bar_spec:
            s.period = t.bar_spec
        if t.symbol:
            s.symbol = t.symbol
        g = t.get_bool
        gi, gf, gt = t.get_int, t.get_float, t.get_time
        s.order_mode = ("fixed" if t.get("OrderMode", "AtmStrategy")
                        == "FixedTicks" else "atm")
        s.atm_template = t.get("AtmStrategy", s.atm_template)
        s.atm_dir = atm_dir
        s.fixed_qty = gi("FixedOrderQuantity", s.fixed_qty)
        s.fixed_sl_ticks = gi("FixedStopLossTicks", s.fixed_sl_ticks)
        s.fixed_tp_ticks = gi("FixedProfitTargetTicks", s.fixed_tp_ticks)
        s.fixed_be_enabled = g("EnableFixedBreakeven", s.fixed_be_enabled)
        s.fixed_be_trigger_ticks = gi("FixedBreakevenTriggerTicks",
                                      s.fixed_be_trigger_ticks)
        s.fixed_be_offset_ticks = gi("FixedBreakevenOffsetTicks",
                                     s.fixed_be_offset_ticks)
        s.confirmation_bars = gi("ConfirmationBars", s.confirmation_bars)
        s.set1_required = gi("GroupTriggerSet1RequiredCount", s.set1_required)
        s.set2_enabled = g("EnableGroupTriggerSet2", s.set2_enabled)
        s.set2_required = gi("GroupTriggerSet2RequiredCount", s.set2_required)
        for src in SOURCES:
            u = src.upper()
            setattr(s, f"use_{src}", g(f"Use{u}Signals",
                                       getattr(s, f"use_{src}")))
            setattr(s, f"require_{src}", g(f"Require{u}Signal",
                                           getattr(s, f"require_{src}")))
            for side in ("Long", "Short"):
                op = t.get(f"{u}_{side}Operator", None)
                val = t.get(f"{u}_{side}Value", None)
                cur = getattr(s, f"{src}_{side.lower()}")
                setattr(s, f"{src}_{side.lower()}",
                        (op or cur[0], int(val) if val is not None else cur[1]))
            setattr(s, f"g2_use_{src}", g(f"G2_Use{u}Signals",
                                          getattr(s, f"g2_use_{src}")))
            setattr(s, f"g2_require_{src}", g(f"G2_Require{u}Signal",
                                              getattr(s, f"g2_require_{src}")))
            for side in ("Long", "Short"):
                op = t.get(f"G2_{u}_{side}Operator", None)
                val = t.get(f"G2_{u}_{side}Value", None)
                cur = getattr(s, f"g2_{src}_{side.lower()}")
                setattr(s, f"g2_{src}_{side.lower()}",
                        (op or cur[0], int(val) if val is not None else cur[1]))
        s.ema_filter = g("EnableEmaFilter", s.ema_filter)
        s.ema_short_period = gi("EmaShortPeriod", s.ema_short_period)
        s.ema_long_period = gi("EmaLongPeriod", s.ema_long_period)
        for i, name in ((1, "tf1"), (2, "tf2"), (3, "tf3")):
            setattr(s, f"{name}_enabled", g(f"EnableTF{i}",
                                            getattr(s, f"{name}_enabled")))
            st = gt(f"StartTime{i}", None)
            en = gt(f"EndTime{i}", None)
            if st and en:
                setattr(s, name, (st, en))
            setattr(s, f"{name}_flatten", g(f"FlattenTF{i}",
                                            getattr(s, f"{name}_flatten")))
        s.skip_enabled = g("EnableSkipTimeWindow", s.skip_enabled)
        st, en = gt("SkipStartTime", None), gt("SkipEndTime", None)
        if st and en:
            s.skip_window = (st, en)
        if g("EnableDailyLossLimit", False):
            s.daily_loss_limit = gf("DailyLossLimit", 200.0)
        if g("EnableDailyProfitTarget", False):
            s.daily_profit_target = gf("DailyProfitTarget", 500.0)
        # engine parameter pass-through (King_* etc.)
        _map = {
            "king_params": [("King_SwingPointNeighborhood", "swing_point_neighborhood", gi),
                            ("King_ImbalanceQualifying", "imbalance_qualifying", gi),
                            ("King_OrderBlockFindingBosChochPeriod", "finding_boschoch_period", gi),
                            ("King_OrderBlockAge", "order_block_age", gi),
                            ("King_OrderBlocksSameDirectionOffset", "same_direction_offset", gi),
                            ("King_OrderBlocksDifferenceDirectionOffset", "difference_direction_offset", gi),
                            ("King_SignalTradeQuantityPerOrderBlock", "signal_qty_per_order_block", gi),
                            ("King_SignalTradeSplitBars", "signal_split_bars", gi)],
            "pana_params": [("Pana_Period", "period", gi),
                            ("Pana_Factor", "factor", gf),
                            ("Pana_MiddlePeriod", "middle_period", gi),
                            ("Pana_SignalBreakSplit", "signal_break_split", gi),
                            ("Pana_SignalPullbackFindingPeriod", "signal_pullback_finding_period", gi)],
            "thunder_params": [("Thunder_TrendMAType", "trend_ma_type", t.get),
                               ("Thunder_TrendPeriod", "trend_period", gi),
                               ("Thunder_StopOffsetMultiplierStop", "stop_offset_mult", gf),
                               ("Thunder_SignalQuantityPerFlat", "signal_qty_per_flat", gi),
                               ("Thunder_SignalQuantityPerTrend", "signal_qty_per_trend", gi)],
            "su_params": [("SU_SlowMAType", "slow_ma_type", t.get),
                          ("SU_SlowMAPeriod", "slow_ma_period", gi),
                          ("SU_FastMA1Type", "fast_ma1_type", t.get),
                          ("SU_FastMA1Period", "fast_ma1_period", gi),
                          ("SU_FastMA2Type", "fast_ma2_type", t.get),
                          ("SU_FastMA2Period", "fast_ma2_period", gi),
                          ("SU_FastMA3Type", "fast_ma3_type", t.get),
                          ("SU_FastMA3Period", "fast_ma3_period", gi),
                          ("SU_SignalSplitFirst", "signal_split_first", gi),
                          ("SU_SignalSplitSecond", "signal_split_second", gi)],
            "nc_params": [("NC_Sensitivity", "sensitivity", gf),
                          ("NC_Smoothness", "smoothness", gi),
                          ("NC_BaselineMAType", "baseline_ma_type", t.get),
                          ("NC_BaselinePeriod", "baseline_period", gi),
                          ("NC_BaselineSmoothingEnabled", "baseline_smoothing_enabled", g),
                          ("NC_BaselineSmoothingMethod", "baseline_smoothing_method", t.get),
                          ("NC_BaselineSmoothingPeriod", "baseline_smoothing_period", gi),
                          ("NC_KernelMAType", "kernel_ma_type", t.get),
                          ("NC_KernelPeriod", "kernel_period", gi),
                          ("NC_KernelSmoothingEnabled", "kernel_smoothing_enabled", g),
                          ("NC_KernelSmoothingMethod", "kernel_smoothing_method", t.get),
                          ("NC_KernelSmoothingPeriod", "kernel_smoothing_period", gi),
                          ("NC_SignalSplit", "signal_split", gi),
                          ("NC_FilterEnabled", "filter_enabled", g),
                          ("NC_FilterBarMin", "filter_bar_min", gi),
                          ("NC_FilterBarMax", "filter_bar_max", gi)],
            "sj_params": [("SJ_SensitiveModeEnabled", "sensitive_mode", g),
                          ("SJ_OffsetLevel1", "offset_level1", gf),
                          ("SJ_OffsetLevel2", "offset_level2", gf),
                          ("SJ_OffsetLevel3", "offset_level3", gf),
                          ("SJ_OffsetLevel4", "offset_level4", gf),
                          ("SJ_OffsetBase", "offset_base", gf),
                          ("SJ_ReferencePricePeriod", "reference_price_period", gi),
                          ("SJ_LineLevelsOffset", "line_levels_offset", gi),
                          ("SJ_SignalCloseThreshold", "signal_close_threshold", gi),
                          ("SJ_SignalQuantityPerZone", "signal_qty_per_zone", gi),
                          ("SJ_SignalSplit", "signal_split", gi)],
        }
        for attr, rows in _map.items():
            params = dict(getattr(s, attr))
            for prop, key, getter in rows:
                if prop in t.props:
                    params[key] = getter(prop)
            setattr(s, attr, params)
        s._template_name = t.name
        return s

    # ------------------------------------------------------------------
    def _vote(self, codes: dict[str, int | None], prefix: str,
              required: int) -> int:
        """One group trigger set; returns +1/-1/0 (GZK.cs 3900-4141)."""
        enabled = [s for s in SOURCES
                   if getattr(self, f"{prefix}use_{s}")
                   and codes.get(s) is not None]
        if not enabled or not (1 <= required <= len(enabled)):
            return 0
        needed = min(max(1, required), len(enabled))
        long_n = short_n = 0
        long_req = short_req = True
        for s in enabled:
            code = codes[s]
            lop, lval = getattr(self, f"{prefix}{s}_long")
            sop, sval = getattr(self, f"{prefix}{s}_short")
            is_l = _OPS[lop](code, lval)
            is_s = _OPS[sop](code, sval)
            long_n += is_l
            short_n += is_s
            if getattr(self, f"{prefix}require_{s}"):
                long_req &= is_l
                short_req &= is_s
        if long_n >= needed and short_n >= needed:
            return 0                          # conflict abort
        if long_n >= needed and long_req:
            return 1
        if short_n >= needed and short_req:
            return -1
        return 0

    def _tod(self, ts_ns: int) -> int:
        t = datetime.fromtimestamp(ts_ns / 1e9, ET)
        return t.hour * 60 + t.minute

    def _entry_allowed(self, tod: int) -> bool:
        wins = [(self.tf1_enabled, self.tf1), (self.tf2_enabled, self.tf2),
                (self.tf3_enabled, self.tf3)]
        any_enabled = any(en for en, _ in wins)
        in_any = any(en and _in_window(tod, w) for en, w in wins)
        if any_enabled and not in_any:        # no windows enabled = always on
            return False
        return not (self.skip_enabled and _in_window(tod, self.skip_window))

    def _check_window_flatten(self, tod: int) -> None:
        """Force-flat when a Flatten-flagged window closes (GZK 4852-4890)."""
        for name, en, win, fl in (("tf1", self.tf1_enabled, self.tf1, self.tf1_flatten),
                                  ("tf2", self.tf2_enabled, self.tf2, self.tf2_flatten),
                                  ("tf3", self.tf3_enabled, self.tf3, self.tf3_flatten)):
            inside = en and fl and _in_window(tod, win)
            was = self._prev_in_flatten.get(name, False)
            if was and not inside and not self.flat:
                self.cancel_all()
                self.close_position(tag="window-flat", force=True)
                self._pending_dir = 0
            self._prev_in_flatten[name] = inside

    # ------------------------------------------------------------------
    def on_bar(self, bar, bars):
        codes: dict[str, int | None] = {}
        for s in SOURCES:
            eng = self._engines.get(s)
            if eng is None:
                codes[s] = None
            elif s == "th":
                codes[s] = eng.update(bar.open, bar.high, bar.low, bar.close,
                                      bar.volume)
            else:
                codes[s] = eng.update(bar.open, bar.high, bar.low, bar.close)
        if self._ema_s is not None:
            self._ema_s.update(bar.close)
            self._ema_l.update(bar.close)

        tod = self._tod(bar.ts)
        self._check_window_flatten(tod)

        s1 = self._vote(codes, "", self.set1_required)
        s2 = (self._vote(codes, "g2_", self.set2_required)
              if self.set2_enabled else 0)
        if s1 and s2 and s1 != s2:
            return                            # cross-set conflict (GZK 1512)
        go = s1 or s2                         # Set 1 priority (GZK 1509)

        # EMA filter (GZK 1519-1525)
        if go != 0 and self._ema_s is not None:
            es, el = self._ema_s.value, self._ema_l.value
            if es != es or el != el:          # not ready yet
                go = 0
            elif go > 0 and not es > el:
                go = 0
            elif go < 0 and not es < el:
                go = 0

        # ConfirmationBars deferral (GZK 1527-1578)
        if self.confirmation_bars > 0:
            if go != 0:
                self._pending_dir = go
                self._pending_bar = bar.index
                self._pending_close = bar.close
                return
            if (self._pending_dir != 0
                    and bar.index - self._pending_bar >= self.confirmation_bars):
                d = self._pending_dir
                self._pending_dir = 0
                if (bar.close - self._pending_close) * d > 0:
                    go = d
                else:
                    return
            elif self._pending_dir != 0:
                return

        if go == 0:
            return
        # reversal on opposite signal (always manages the position)
        if not self.flat:
            if self.reverse_on_signal and go * self.position < 0:
                self.cancel_all()
                self.close_position(tag="reverse", force=True)
                if self._entry_allowed(tod):
                    self._go(go)
            return
        if self.working_orders:
            return                            # entry already pending
        if self._entry_allowed(tod):
            self._go(go)

    def _go(self, direction: int) -> None:
        qty = self._atm.entry_qty
        if direction > 0:
            self.buy(qty=qty, tag="gzk-long")
        else:
            self.sell(qty=qty, tag="gzk-short")

    def on_fill(self, fill):
        # attach ATM exit legs when the entry fills
        if not fill.order.is_exit and fill.order.tag.startswith("gzk-"):
            side = 1 if fill.side > 0 else -1
            submit_atm_exits(self._broker, self._atm, side, fill.price)
