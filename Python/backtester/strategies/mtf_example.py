"""Multi-timeframe example — a plumbing template, NOT a tuned strategy.

Demonstrates `secondary_periods` + `on_secondary_bar`: a fast EMA cross on the
primary 1m series, gated by a higher-timeframe (15m) EMA trend filter. The 15m
bars are a secondary series; on_secondary_bar updates the HTF EMA as each 15m
bar completes, and on_bar reads that trend to allow/deny primary entries. The
engine guarantees the secondary bars visible in on_bar have already closed
(no look-ahead). Use this as a reference when porting multi-timeframe NT8
strategies (e.g. GodZillaKilla-style bias + entry series).
"""
from backtester import EMA, Strategy


class MTFExample(Strategy):
    symbol = "MNQ"
    period = "1m"
    session = ("09:30", "16:00")
    secondary_periods = ["15m"]

    fast_period = 9
    slow_period = 21
    htf_period = 20

    def on_start(self):
        self.ema_f = EMA(self.fast_period)
        self.ema_s = EMA(self.slow_period)
        self.htf_ema = EMA(self.htf_period)
        self.htf_up = False

    def on_secondary_bar(self, bar, bars, period):
        self.htf_ema.update(bar.close)
        if self.htf_ema.ready:
            self.htf_up = bar.close > self.htf_ema.value

    def on_bar(self, bar, bars):
        f = self.ema_f.update(bar.close)
        s = self.ema_s.update(bar.close)
        if not (self.ema_s.ready and self.htf_ema.ready):
            return
        if f > s and self.htf_up and self.flat:
            self.buy(tag="mtf-long")
        elif f < s and self.position > 0:
            self.close_position(tag="exit")
