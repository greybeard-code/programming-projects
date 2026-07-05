"""Data access: catalog of raw Parquet days, reduced L1 caching, bar building.

Raw layout (see M:\\NinjaTrader_DataRepo\\RawData\\Parquet\\README.txt):
    <SYMBOL>-<YEAR>_L1\\<YYYYMMDD>.parquet
    columns: Timestamp (ns UTC), MarketDataType (int8), Price, Volume

A raw day is ~24M events, mostly bid/ask quote updates. The engine only needs
trade (Last) events with the prevailing bid/ask attached, so the first touch of
a day reduces it to ~8% of the rows and caches that locally. Bars are built
from the reduced data and cached per (symbol, day, period).
"""
from __future__ import annotations

import os
import re
from dataclasses import dataclass
from datetime import datetime
from pathlib import Path
from zoneinfo import ZoneInfo

import numpy as np
import pyarrow as pa
import pyarrow.parquet as pq

DEFAULT_DATA_ROOT = Path(os.environ.get(
    "BACKTESTER_DATA_ROOT", r"M:\NinjaTrader_DataRepo\RawData\Parquet"))
DEFAULT_CACHE_ROOT = Path(os.environ.get(
    "BACKTESTER_CACHE", str(Path(__file__).resolve().parent.parent / ".cache")))

MDT_ASK, MDT_BID, MDT_LAST = 0, 1, 2

_DIR_RE = re.compile(r"^(?P<sym>[A-Z0-9]+)-(?P<year>\d{4})_L1$")

_EASTERN = ZoneInfo("America/New_York")


def _eastern_offset_ns(date: str) -> int:
    """UTC offset (ns, negative for US) of Eastern wall time on `date`.

    The raw repo's Timestamps are the recording PC's LOCAL (US/Eastern) wall
    clock, NOT UTC as its README claims — verified empirically 2026-07-05:
    the 17:00-18:00 CME halt and the 09:30 cash-open volume spike sit at
    those literal hours in every file, summer and winter, and tick prices
    align with an NT8 chart export to within seconds only under the ET
    interpretation. True UTC = stamp - offset. DST transitions happen 02:00
    Sunday when the market is closed, so a per-date offset (sampled at the
    file's midday) is exact for every trade.
    """
    d = datetime.strptime(date, "%Y%m%d")
    midday = datetime(d.year, d.month, d.day, 12, tzinfo=_EASTERN)
    return int(midday.utcoffset().total_seconds() * 1e9)


CACHE_VERSION = b"3"   # reduced: v3 = trust replay_importer UTC tag, else ET->UTC
BARS_VERSION = b"6"    # bars: v6 = renko breakout tick belongs to next bar (H/L parity)

# The replay_importer (v>=2) stamps output Parquet with these keys and stores
# true UTC. Files carrying timestamps=UTC skip the _eastern_offset_ns
# correction; legacy/untagged files are the ET-wall-clock stamps and still get
# corrected. This lets old and new Parquet coexist with no flag day.
IMPORTER_TS_KEY = b"replay_importer.timestamps"
IMPORTER_TS_UTC = b"UTC"

REDUCED_COLS = ("ts", "price", "volume", "ask", "bid",
                "ask_size", "bid_size", "aggr")
BAR_COLS = ("ts_end", "open", "high", "low", "close", "volume",
            "buy_volume", "sell_volume", "i0", "i1")


@dataclass
class DayL1:
    """One trading day's trade events with prevailing quotes (all same length)."""
    date: str            # YYYYMMDD
    ts: np.ndarray       # int64 ns UTC
    price: np.ndarray    # float64 trade price
    volume: np.ndarray   # int64 trade size
    ask: np.ndarray      # float64 prevailing ask (nan before first quote)
    bid: np.ndarray      # float64 prevailing bid
    ask_size: np.ndarray # int64 prevailing ask queue size (0 before first quote)
    bid_size: np.ndarray # int64 prevailing bid queue size
    aggr: np.ndarray     # int8 aggressor: +1 buy (at/above ask), -1 sell, 0 mid

    def __len__(self) -> int:
        return len(self.ts)


@dataclass
class BarDay:
    """Bars for one day, with each bar's span into the DayL1 arrays."""
    ts_end: np.ndarray       # int64 ns UTC, bar close time (right edge)
    open: np.ndarray
    high: np.ndarray
    low: np.ndarray
    close: np.ndarray
    volume: np.ndarray
    buy_volume: np.ndarray   # aggressor-buy volume (trades at/above the ask)
    sell_volume: np.ndarray  # aggressor-sell volume (trades at/below the bid)
    i0: np.ndarray           # int64, first DayL1 index of the bar
    i1: np.ndarray           # int64, one past the last DayL1 index

    def __len__(self) -> int:
        return len(self.ts_end)


def classify_aggressor(price: np.ndarray, ask: np.ndarray,
                       bid: np.ndarray) -> np.ndarray:
    """+1 where the trade lifted the offer, -1 where it hit the bid, 0 mid.

    This is what NT8 backtests structurally cannot see — it enables volume
    delta / order-imbalance research on historical data.
    """
    aggr = np.zeros(len(price), dtype="int8")
    with np.errstate(invalid="ignore"):
        aggr[price >= ask] = 1
        aggr[price <= bid] = -1
    return aggr


class Catalog:
    """Finds available days for a symbol across contract-year folders."""

    def __init__(self, data_root: Path | str | None = None,
                 cache_root: Path | str | None = None):
        self.data_root = Path(data_root) if data_root else DEFAULT_DATA_ROOT
        self.cache_root = Path(cache_root) if cache_root else DEFAULT_CACHE_ROOT

    def day_files(self, symbol: str) -> dict[str, Path]:
        """Map YYYYMMDD -> raw parquet path, merged across contract years."""
        symbol = symbol.upper()
        out: dict[str, Path] = {}
        if not self.data_root.exists():
            raise FileNotFoundError(f"Data root not found: {self.data_root}")
        for d in sorted(self.data_root.iterdir()):
            m = _DIR_RE.match(d.name)
            if not m or m.group("sym") != symbol:
                continue
            for f in d.glob("*.parquet"):
                if re.fullmatch(r"\d{8}", f.stem):
                    out[f.stem] = f
        if not out:
            raise FileNotFoundError(
                f"No L1 data for {symbol} under {self.data_root}")
        return dict(sorted(out.items()))

    def days(self, symbol: str, start: str | None = None,
             end: str | None = None) -> list[str]:
        """Available YYYYMMDD dates, optionally clipped to [start, end]."""
        s = start.replace("-", "") if start else "00000000"
        e = end.replace("-", "") if end else "99999999"
        return [d for d in self.day_files(symbol) if s <= d <= e]

    # ---------------- reduced L1 ----------------

    def _reduced_path(self, symbol: str, date: str) -> Path:
        return self.cache_root / "reduced" / symbol.upper() / f"{date}.parquet"

    def load_day(self, symbol: str, date: str) -> DayL1:
        """Load reduced trade events for one day, building the cache on first
        use. Files from an older schema/version are rebuilt transparently."""
        rp = self._reduced_path(symbol, date)
        if rp.exists():
            t = pq.read_table(rp)
            meta = t.schema.metadata or {}
            if (set(REDUCED_COLS).issubset(t.column_names)
                    and meta.get(b"btcache") == CACHE_VERSION):
                return DayL1(date, *(t.column(c).to_numpy()
                                     for c in REDUCED_COLS))
        day = self._reduce_raw(symbol, date)
        rp.parent.mkdir(parents=True, exist_ok=True)
        tmp = rp.with_suffix(f".{os.getpid()}.tmp")   # unique per process
        pq.write_table(pa.table({
            "ts": day.ts, "price": day.price, "volume": day.volume,
            "ask": day.ask, "bid": day.bid, "ask_size": day.ask_size,
            "bid_size": day.bid_size, "aggr": day.aggr,
        }).replace_schema_metadata({b"btcache": CACHE_VERSION}),
            tmp, compression="zstd")
        os.replace(tmp, rp)
        return day

    def _reduce_raw(self, symbol: str, date: str) -> DayL1:
        raw = self.day_files(symbol).get(date)
        if raw is None:
            raise FileNotFoundError(f"No raw file for {symbol} {date}")
        t = pq.read_table(raw, columns=["Timestamp", "MarketDataType",
                                        "Price", "Volume"])
        meta = t.schema.metadata or {}
        utc_tagged = meta.get(IMPORTER_TS_KEY) == IMPORTER_TS_UTC
        ts = t.column("Timestamp").to_numpy(zero_copy_only=False).astype("int64")
        if not utc_tagged:
            ts = ts - _eastern_offset_ns(date)   # legacy: raw stamps are ET wall clock
        mdt = t.column("MarketDataType").to_numpy()
        price = t.column("Price").to_numpy()
        vol = t.column("Volume").to_numpy()

        last_idx = np.flatnonzero(mdt == MDT_LAST)

        def prevailing(quote_type: int) -> tuple[np.ndarray, np.ndarray]:
            qidx = np.flatnonzero(mdt == quote_type)
            pos = np.searchsorted(qidx, last_idx, side="right") - 1
            ok = pos >= 0
            out_p = np.full(len(last_idx), np.nan)
            out_p[ok] = price[qidx][pos[ok]]
            out_s = np.zeros(len(last_idx), dtype="int64")
            out_s[ok] = vol[qidx][pos[ok]]
            return out_p, out_s

        ask_p, ask_s = prevailing(MDT_ASK)
        bid_p, bid_s = prevailing(MDT_BID)
        lp = price[last_idx]
        return DayL1(
            date=date,
            ts=ts[last_idx],
            price=lp,
            volume=vol[last_idx],
            ask=ask_p,
            bid=bid_p,
            ask_size=ask_s,
            bid_size=bid_s,
            aggr=classify_aggressor(lp, ask_p, bid_p),
        )

    # ---------------- bars ----------------

    def _bars_path(self, symbol: str, date: str, key: str) -> Path:
        return (self.cache_root / "bars" / symbol.upper() / key
                / f"{date}.parquet")

    def load_bars(self, symbol: str, date: str, spec, tick_size: float,
                  day: DayL1 | None = None) -> BarDay:
        bp = self._bars_path(symbol, date, spec.key)
        if bp.exists():
            t = pq.read_table(bp)
            meta = t.schema.metadata or {}
            if (set(BAR_COLS).issubset(t.column_names)
                    and meta.get(b"btcache") == BARS_VERSION):
                return BarDay(*(t.column(c).to_numpy() for c in BAR_COLS))
        if day is None:
            day = self.load_day(symbol, date)
        bars = build_bars(day, spec, tick_size)
        bp.parent.mkdir(parents=True, exist_ok=True)
        tmp = bp.with_suffix(f".{os.getpid()}.tmp")
        pq.write_table(pa.table({c: getattr(bars, c) for c in BAR_COLS})
                       .replace_schema_metadata({b"btcache": BARS_VERSION}),
                       tmp, compression="zstd")
        os.replace(tmp, bp)
        return bars


def _empty_bars() -> BarDay:
    z = np.array([], dtype="int64")
    zf = np.array([], dtype="float64")
    return BarDay(z, zf, zf, zf, zf, z.copy(), z.copy(), z.copy(),
                  z.copy(), z.copy())


def _flow_volumes(day: DayL1) -> tuple[np.ndarray, np.ndarray]:
    buy = np.where(day.aggr == 1, day.volume, 0)
    sell = np.where(day.aggr == -1, day.volume, 0)
    return buy, sell


def build_bars(day: DayL1, spec, tick_size: float) -> BarDay:
    """Dispatch on BarSpec.kind: time / tick / renko."""
    if spec.kind == "time":
        return build_time_bars(day, spec.seconds)
    if spec.kind == "tick":
        return build_tick_bars(day, spec.ticks)
    if spec.kind == "renko":
        return build_renko_bars(day, spec.brick_ticks * tick_size,
                                spec.trend_ticks * tick_size)
    raise ValueError(f"Unknown bar kind {spec.kind!r}")


def build_time_bars(day: DayL1, period_s: int) -> BarDay:
    """Time bars from trade events. Bars with no trades are omitted (NT8-style).

    Bar timestamp is the close (right edge) of the interval, matching NT8.
    """
    if len(day) == 0:
        return _empty_bars()
    period_ns = int(period_s) * 1_000_000_000
    first_edge = (day.ts[0] // period_ns) * period_ns
    last_edge = (day.ts[-1] // period_ns + 1) * period_ns
    edges = np.arange(first_edge, last_edge + period_ns, period_ns)
    # boundaries[j] = first event index at/after edges[j]
    bounds = np.searchsorted(day.ts, edges, side="left")
    i0, i1 = bounds[:-1], bounds[1:]
    nonempty = i1 > i0
    i0, i1 = i0[nonempty], i1[nonempty]
    ts_end = edges[1:][nonempty]

    if len(i0) == 0:
        return _empty_bars()
    # Note: removed empty bars have no events, so i0[k+1] == i1[k] always;
    # segments are contiguous and reduceat over i0 aggregates exactly per bar.
    buy, sell = _flow_volumes(day)
    o = day.price[i0]
    c = day.price[i1 - 1]
    h = np.maximum.reduceat(day.price, i0)
    l = np.minimum.reduceat(day.price, i0)
    v = np.add.reduceat(day.volume, i0)
    bv = np.add.reduceat(buy, i0)
    sv = np.add.reduceat(sell, i0)
    return BarDay(ts_end.astype("int64"), o.astype("float64"), h, l,
                  c.astype("float64"), v.astype("int64"),
                  bv.astype("int64"), sv.astype("int64"),
                  i0.astype("int64"), i1.astype("int64"))


def build_tick_bars(day: DayL1, ticks_per_bar: int) -> BarDay:
    """Fixed trade-count bars (NT8 tick bars). Last partial bar is kept."""
    n = len(day)
    if n == 0:
        return _empty_bars()
    buy, sell = _flow_volumes(day)
    i0 = np.arange(0, n, ticks_per_bar, dtype="int64")
    i1 = np.minimum(i0 + ticks_per_bar, n)
    o = day.price[i0]
    c = day.price[i1 - 1]
    h = np.maximum.reduceat(day.price, i0)
    l = np.minimum.reduceat(day.price, i0)
    v = np.add.reduceat(day.volume, i0)
    bv = np.add.reduceat(buy, i0)
    sv = np.add.reduceat(sell, i0)
    return BarDay(day.ts[i1 - 1].astype("int64"), o.astype("float64"), h, l,
                  c.astype("float64"), v.astype("int64"),
                  bv.astype("int64"), sv.astype("int64"), i0, i1)


RENKO_RESET_GAP_NS = 30 * 60 * 1_000_000_000   # trade gap that resets the anchor


def build_renko_bars(day: DayL1, brick: float, trend: float) -> BarDay:
    """ninZaRenko bars, per the published trader manual.

    Parameters (price units here; ticks at the BarSpec level):
    - `brick` B — every bar's body height, both directions.
    - `trend` T — with-trend close: price reaching `prev_close + T` (up
      trend) closes an up bar at exactly that level.
    Derived, as the manual specifies:
    - open offset = B - T: each bar's synthetic open sits B-T *inside* the
      prior bar (open = close -/+ B), producing the overlapping look.
    - reversal threshold = 2B - T from the previous close: price reaching
      `prev_close - (2B - T)` in an uptrend closes a down reversal bar
      there, whose body is then exactly B as well.

    The very first bar (no trend yet) closes T away in either direction.
    High/low span the synthetic open/close AND the real trade extremes —
    matching what NT8 indicators (e.g. ATR) see on ninZaRenko bars. Price
    gaps can emit several bars on one tick; the extra bars get zero-length
    spans (purely synthetic).

    Session reset (verified against a real ninZaRenko chart export): a trade
    gap > 30 min (the 17:00-18:00 ET halt, weekends) closes a partial bar at
    the last traded price and re-anchors the brick grid at the next trade —
    ninZa restarts its geometry each session rather than running a
    continuous grid.

    NOTE: signal timing on these bars is realistic; the engine still fills
    orders on real ticks, so none of NT8's Renko fantasy-fill problem applies.
    """
    n = len(day)
    if n == 0:
        return _empty_bars()
    prices = day.price
    rev = 2 * brick - trend

    buy, sell = _flow_volumes(day)
    ts_end, opens, highs, lows, closes, vols = [], [], [], [], [], []
    bvols, svols, i0s, i1s = [], [], [], []

    # consider only ticks where price changed (big speedup, same bars),
    # plus the first tick after any session gap (reset points)
    chg = np.flatnonzero(np.diff(prices) != 0) + 1
    gap_starts = np.flatnonzero(np.diff(day.ts) > RENKO_RESET_GAP_NS) + 1
    walk = np.unique(np.concatenate([[0], chg, gap_starts]))
    gap_set = set(gap_starts.tolist())

    anchor = prices[0]      # close of the last emitted bar
    d = 0                   # current trend: +1 / -1 / 0 (no bar yet)
    span_start = 0          # first tick index of the bar being built

    def emit(close: float, direction: int, k: int,
             body: float | None = None, partial: bool = False) -> None:
        # A threshold breakout EXCLUDES its triggering tick k: that tick
        # overshoots the level and belongs to the NEXT bar — ninZaRenko.cs
        # clamps the completing bar to the threshold and opens the new bar ON
        # the breakout tick (which becomes the new bar's first high/low). So
        # the completing bar's H/L, volume and fill span all stop before k.
        # A `partial` close (session halt / EOD) has no breakout tick: its k
        # IS the last real trade and its close price, so include it.
        nonlocal anchor, span_start, d
        i0 = span_start
        i1 = k + 1 if partial else k
        seg = prices[i0:i1] if i1 > i0 else None
        o = close - direction * (brick if body is None else body)
        opens.append(o)
        closes.append(close)
        highs.append(max(seg.max() if seg is not None else close, o, close))
        lows.append(min(seg.min() if seg is not None else close, o, close))
        vols.append(int(day.volume[i0:i1].sum()) if i1 > i0 else 0)
        bvols.append(int(buy[i0:i1].sum()) if i1 > i0 else 0)
        svols.append(int(sell[i0:i1].sum()) if i1 > i0 else 0)
        ts_end.append(int(day.ts[k]))
        i0s.append(i0)
        i1s.append(i1)
        anchor = close
        span_start = i1
        d = direction

    for k in walk:
        p = prices[k]
        # session reset: close the FORMING bar as a partial at the last
        # pre-gap trade, then re-anchor the grid at this trade. The forming
        # bar's open is anchor - d*(B - T) (unique regardless of whether it
        # would have closed with-trend or reversed) — verified against a
        # real ninZaRenko chart export.
        if k in gap_set:
            if d != 0:
                last_p = prices[k - 1]
                form_open = anchor - d * (brick - trend)
                direction = 1 if last_p >= form_open else -1
                emit(last_p, direction, k - 1,
                     body=abs(last_p - form_open), partial=True)
            anchor = p
            d = 0
            span_start = k
        # Breakouts are STRICT (`>` / `<`), matching ninZaRenko.cs
        # (`breakUp = close > upperLimit`): a close *exactly at* a threshold
        # updates the forming bar but does not emit a brick — price must
        # exceed the level. An inclusive `>=` here prints an extra brick on a
        # touch-and-reverse, offsetting the grid by +/-T (self-healing, but a
        # real divergence from NT8's own bars).
        if d == 0:                                   # no trend yet: T either way
            # first bar of a session: body = T, open = the anchor itself
            # (verified against a real ninZaRenko chart export)
            if p > anchor + trend:
                emit(anchor + trend, 1, k, body=trend)
            elif p < anchor - trend:
                emit(anchor - trend, -1, k, body=trend)
        while d > 0 and (p > anchor + trend or p < anchor - rev):
            if p > anchor + trend:
                emit(anchor + trend, 1, k)
            else:
                emit(anchor - rev, -1, k)
        while d < 0 and (p < anchor - trend or p > anchor + rev):
            if p < anchor - trend:
                emit(anchor - trend, -1, k)
            else:
                emit(anchor + rev, 1, k)             # exits loop (d flipped)
        while d > 0 and p > anchor + trend:          # ups after an up-reversal
            emit(anchor + trend, 1, k)

    return BarDay(
        np.asarray(ts_end, dtype="int64"),
        np.asarray(opens, dtype="float64"),
        np.asarray(highs, dtype="float64"),
        np.asarray(lows, dtype="float64"),
        np.asarray(closes, dtype="float64"),
        np.asarray(vols, dtype="int64"),
        np.asarray(bvols, dtype="int64"),
        np.asarray(svols, dtype="int64"),
        np.asarray(i0s, dtype="int64"),
        np.asarray(i1s, dtype="int64"),
    )
