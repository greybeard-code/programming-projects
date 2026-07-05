"""Example: EMA cross with bracket exits. A validation strategy, not an edge."""
from backtester import EMA, Strategy


class EmaCross(Strategy):
    symbol = "MNQ"
    period = "1m"
    session = ("09:30", "16:00")     # RTH, US/Eastern
    qty = 1

    fast_period = 9
    slow_period = 21
    stop_ticks = 40      # 10 pts on MNQ
    target_ticks = 80    # 20 pts

    def on_start(self):
        self.fast = EMA(self.fast_period)
        self.slow = EMA(self.slow_period)
        self.prev_diff = None

    def on_bar(self, bar, bars):
        f = self.fast.update(bar.close)
        s = self.slow.update(bar.close)
        if not self.slow.ready:
            return
        diff = f - s
        if self.prev_diff is not None and self.flat:
            if self.prev_diff <= 0 < diff:
                self.buy_bracket(stop_ticks=self.stop_ticks,
                                 target_ticks=self.target_ticks)
            elif self.prev_diff >= 0 > diff:
                self.sell_bracket(stop_ticks=self.stop_ticks,
                                  target_ticks=self.target_ticks)
        self.prev_diff = diff
