"""gbNobleCloud port — baseline vs kernel-band cloud with reversal signals.

Source: nt8 code/GodZillaKilla/indicators/gbNobleCloud.cs (OnBarUpdate
416-527). Signal_Trade codes: +1 bullish / -1 bearish.

Cloud state: baseline (smoothed slow MA) vs a band around the kernel MA
(+- Sensitivity/50 * StdDev(kernel period)). Baseline BELOW the band = +1
(bullish cloud), ABOVE = -1, inside = 0. A bullish trade signal is a
red-then-green candle pair whose low tags the lower band while the close
recovers above it, gated by the bar-count filter (bars in the current cloud
state within [FilterBarMin, FilterBarMax]) and a SignalSplit debounce per
direction.

NT8 quirks kept: StdDev is population over the partial window (0 at bar 0);
NC's WilderMA maps to EMA(2*period) (unlike Thunder/Sumo's 2p-1) — faithful
to each file. Threshold smoothing only engages when Smoothness > 1.
"""
from __future__ import annotations

import math
from collections import deque

from .nt8math import Nt8Ema, Nt8Sma, approx_compare as _ac


def _make_ma(ma_type: str, period: int):
    t = ma_type.upper()
    if t == "SMA":
        return Nt8Sma(period)
    if t == "EMA":
        return Nt8Ema(period)
    if t == "WILDERMA":
        return Nt8Ema(2 * period)      # NC's mapping (EMA(2*period))
    raise NotImplementedError(f"NobleCloud MA type {ma_type!r} not ported "
                              f"(SMA/EMA/WilderMA only)")


class _Chain:
    """MA optionally followed by a smoothing MA of its output series."""

    def __init__(self, ma_type, period, smooth_enabled, smooth_method,
                 smooth_period):
        self._ma = _make_ma(ma_type, period)
        self._smooth = (_make_ma(smooth_method, smooth_period)
                        if smooth_enabled else None)

    def update(self, x: float) -> float:
        v = self._ma.update(x)
        return self._smooth.update(v) if self._smooth is not None else v


class _Nt8StdDev:
    """@StdDev.cs: population stddev over min(bars, period); 0 at bar 0."""

    def __init__(self, period: int):
        self._win: deque[float] = deque(maxlen=period)
        self.value = 0.0

    def update(self, x: float) -> float:
        self._win.append(x)
        if len(self._win) == 1:
            self.value = 0.0
        else:
            avg = sum(self._win) / len(self._win)
            self.value = math.sqrt(
                sum((v - avg) ** 2 for v in self._win) / len(self._win))
        return self.value


class NobleCloud:
    def __init__(self, sensitivity: float = 60.0, smoothness: int = 1,
                 baseline_ma_type: str = "SMA", baseline_period: int = 60,
                 baseline_smoothing_enabled: bool = True,
                 baseline_smoothing_method: str = "EMA",
                 baseline_smoothing_period: int = 60,
                 kernel_ma_type: str = "SMA", kernel_period: int = 20,
                 kernel_smoothing_enabled: bool = True,
                 kernel_smoothing_method: str = "EMA",
                 kernel_smoothing_period: int = 5,
                 signal_split: int = 5, filter_enabled: bool = True,
                 filter_bar_min: int = 10, filter_bar_max: int = 300):
        self._baseline = _Chain(baseline_ma_type, baseline_period,
                                baseline_smoothing_enabled,
                                baseline_smoothing_method,
                                baseline_smoothing_period)
        self._kernel = _Chain(kernel_ma_type, kernel_period,
                              kernel_smoothing_enabled,
                              kernel_smoothing_method,
                              kernel_smoothing_period)
        self._std = _Nt8StdDev(kernel_period)
        self._eff_sens = sensitivity / 50.0
        self._smooth_up = Nt8Ema(smoothness) if smoothness > 1 else None
        self._smooth_dn = Nt8Ema(smoothness) if smoothness > 1 else None
        self.signal_split = signal_split
        self.filter_enabled = filter_enabled
        if filter_bar_min > filter_bar_max:
            filter_bar_min, filter_bar_max = filter_bar_max, filter_bar_min
        self.filter_min, self.filter_max = filter_bar_min, filter_bar_max

        self.n = -1
        self.cloud_state = 0
        self.baseline = float("nan")
        self._bar_count = 0
        self._cloud_prev = 0               # Signal_Cloud[1]
        self._next_bull = 0
        self._next_bear = 0
        self._o1 = self._c1 = float("nan")

    def update(self, o: float, h: float, l: float, c: float) -> int:
        self.n += 1
        std = self._std.update(c)
        kern = self._kernel.update(c)
        upper_raw = kern + self._eff_sens * std
        lower_raw = kern - self._eff_sens * std
        upper = (self._smooth_up.update(upper_raw)
                 if self._smooth_up is not None else upper_raw)
        lower = (self._smooth_dn.update(lower_raw)
                 if self._smooth_dn is not None else lower_raw)
        self.baseline = self._baseline.update(c)

        if self.n == 0:
            if _ac(self.baseline, upper) > 0:
                self._bar_count, self.cloud_state = 1, -1
            elif _ac(self.baseline, lower) < 0:
                self._bar_count, self.cloud_state = 1, 1
            else:
                self._bar_count, self.cloud_state = -1, 0
            self._cloud_prev = self.cloud_state
            self._o1, self._c1 = o, c
            return 0
        if (self.cloud_state != 0 and _ac(self.baseline, upper) <= 0
                and _ac(self.baseline, lower) >= 0):
            self._bar_count, self.cloud_state = -1, 0
        elif self.cloud_state <= 0 and _ac(self.baseline, lower) < 0:
            self._bar_count, self.cloud_state = 1, 1
        elif self.cloud_state >= 0 and _ac(self.baseline, upper) > 0:
            self._bar_count, self.cloud_state = 1, -1
        if self._cloud_prev == self.cloud_state and self._bar_count >= 1:
            self._bar_count += 1

        code = 0
        if (not self.filter_enabled
                or self.filter_min <= self._bar_count <= self.filter_max):
            if (self.cloud_state > 0
                    and _ac(l, lower) <= 0 and _ac(c, lower) > 0
                    and _ac(self._c1, self._o1) < 0 and _ac(c, o) > 0
                    and self.n >= self._next_bull):
                code = 1
                self._next_bull = self.n + self.signal_split
            elif (self.cloud_state < 0
                    and _ac(h, upper) >= 0 and _ac(c, upper) < 0
                    and _ac(self._c1, self._o1) > 0 and _ac(c, o) < 0
                    and self.n >= self._next_bear):
                code = -1
                self._next_bear = self.n + self.signal_split
        self._cloud_prev = self.cloud_state
        self._o1, self._c1 = o, c
        return code
