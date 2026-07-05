"""Terminator_V2 core port (from Terminator_V2.cs v2.4.2, NT8).

The NT8 original is an ATR trailing-stop stop-and-reverse: a chandelier-style
trail line follows price at ATRMult * ATR(ATRPeriod); a close crossing above
the line signals long, below signals short. Always-in by default (no TP/SL),
with a "clean-split" reversal: flatten on the signal bar, re-enter the new
direction on a later bar once flat (max 5 bars, fresh cross overrides).

Ported here: the signal engine (exact line-update rules), clean-split
reversal timing, optional fixed SL/TP brackets, cooldown, long/short enables.
NOT ported (off by default in the original): VWMA gate/source, volume filter,
EMA stop/trail modes, breakeven, trail-trigger arming, currency-mode exits,
risk-based sizing, second time window. Session handling: the original trades
the full ETH session unless its time filter is on; here we default to RTH
with flat-at-session-end (prop-firm context). User runs it on MNQ
ninZaRenko 100/4 -> period "r100-4".
"""
from backtester import ATR, Strategy


class TerminatorV2(Strategy):
    symbol = "MNQ"
    period = "r100-4"                 # ninZaRenko brick 100 ticks / trend 4
    session = ("08:30", "15:55")      # US/Central; original default is ETH
    flat_at_session_end = True
    qty = 1

    atr_period = 20
    atr_mult = 4.0
    sl_ticks = 0                      # 0 = off (original default: pure SAR)
    tp_ticks = 0
    cooldown_bars = 0
    enable_longs = True
    enable_shorts = True
    reverse_max_delay_bars = 5

    def on_start(self):
        self.atr = ATR(self.atr_period)
        self.trail = None             # xAtrTrailingStop
        self.prev_close = None
        self.pending_reverse = 0      # +1/-1 queued after a clean-split flatten
        self.pending_reverse_bar = -1
        self.last_entry_bar = -1

    def on_session_end(self, date):
        self.pending_reverse = 0

    def _go(self, direction, bar):
        if direction > 0 and not self.enable_longs:
            return
        if direction < 0 and not self.enable_shorts:
            return
        if (self.cooldown_bars > 0 and self.last_entry_bar >= 0
                and bar.index - self.last_entry_bar < self.cooldown_bars):
            return
        kw = {}
        if self.sl_ticks > 0:
            kw["stop_ticks"] = self.sl_ticks
        if self.tp_ticks > 0:
            kw["target_ticks"] = self.tp_ticks
        if direction > 0:
            self.buy_bracket(**kw) if kw else self.buy(tag="long")
        else:
            self.sell_bracket(**kw) if kw else self.sell(tag="short")
        self.last_entry_bar = bar.index

    def on_bar(self, bar, bars):
        atr = self.atr.update(bar.high, bar.low, bar.close)
        c0 = bar.close
        c1 = self.prev_close

        if self.trail is None:
            self.trail = c0
            self.prev_close = c0
            return
        if not self.atr.ready or c1 is None:
            self.prev_close = c0
            return

        # --- exact NT8 line-update + cross rules ---
        last = self.trail
        n = self.atr_mult * atr
        if c0 > last and c1 > last:
            new = max(last, c0 - n)
        elif c0 < last and c1 < last:
            new = min(last, c0 + n)
        elif c0 > last:
            new = c0 - n
        else:
            new = c0 + n

        direction = 0
        if c1 < last and c0 > last:
            direction = 1
        elif c1 > last and c0 < last:
            direction = -1

        self.trail = new
        self.prev_close = c0

        pos = self.position
        if pos == 0:
            if direction != 0:
                self._go(direction, bar)       # fresh cross overrides queue
                self.pending_reverse = 0
            elif self.pending_reverse != 0:
                if bar.index - self.pending_reverse_bar > self.reverse_max_delay_bars:
                    self.pending_reverse = 0      # stale — drop it
                else:
                    self._go(self.pending_reverse, bar)
                    self.pending_reverse = 0
        elif direction != 0 and ((direction > 0) != (pos > 0)):
            # clean-split reversal: flatten now, re-enter once flat
            if self.pending_reverse != direction:
                self.cancel_all()
                self.close_position(tag="reverse")
                self.pending_reverse = direction
                self.pending_reverse_bar = bar.index

