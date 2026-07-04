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
from pathlib import Path

import numpy as np
import pyarrow as pa
import pyarrow.parquet as pq

DEFAULT_DATA_ROOT = Path(os.environ.get(
    "BACKTESTER_DATA_ROOT", r"M:\NinjaTrader_DataRepo\RawData\Parquet"))
DEFAULT_CACHE_ROOT = Path(os.environ.get(
    "BACKTESTER_CACHE", str(Path(__file__).resolve().parent.parent / ".cache")))

MDT_ASK, MDT_BID, MDT_LAST = 0, 1, 2

_DIR_RE = re.compile(r"^(?P<sym>[A-Z0-9]+)-(?P<year>\d{4})_L1$")


@dataclass
class DayL1:
    """One trading day's trade events with prevailing quotes (all same length)."""
    date: str            # YYYYMMDD
    ts: np.ndarray       # int64 ns UTC
    price: np.ndarray    # float64 trade price
    volume: np.ndarray   # int64 trade size
    ask: np.ndarray      # float64 prevailing ask (nan before first quote)
    bid: np.ndarray      # float64 prevailing bid

    def __len__(self) -> int:
        return len(self.ts)


@dataclass
class BarDay:
    """Bars for one day, with each bar's span into the DayL1 arrays."""
    ts_end: np.ndarray   # int64 ns UTC, bar close time (right edge)
    open: np.ndarray
    high: np.ndarray
    low: np.ndarray
    close: np.ndarray
    volume: np.ndarray
    i0: np.ndarray       # int64, first DayL1 index of the bar
    i1: np.ndarray       # int64, one past the last DayL1 index

    def __len__(self) -> int:
        return len(self.ts_end)


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
        """Load reduced trade events for one day, building the cache on first use."""
        rp = self._reduced_path(symbol, date)
        if rp.exists():
            t = pq.read_table(rp)
            return DayL1(
                date=date,
                ts=t.column("ts").to_numpy(),
                price=t.column("price").to_numpy(),
                volume=t.column("volume").to_numpy(),
                ask=t.column("ask").to_numpy(),
                bid=t.column("bid").to_numpy(),
            )
        day = self._reduce_raw(symbol, date)
        rp.parent.mkdir(parents=True, exist_ok=True)
        tmp = rp.with_suffix(".tmp")
        pq.write_table(pa.table({
            "ts": day.ts, "price": day.price, "volume": day.volume,
            "ask": day.ask, "bid": day.bid,
        }), tmp, compression="zstd")
        os.replace(tmp, rp)
        return day

    def _reduce_raw(self, symbol: str, date: str) -> DayL1:
        raw = self.day_files(symbol).get(date)
        if raw is None:
            raise FileNotFoundError(f"No raw file for {symbol} {date}")
        t = pq.read_table(raw, columns=["Timestamp", "MarketDataType",
                                        "Price", "Volume"])
        ts = t.column("Timestamp").to_numpy().astype("int64")
        mdt = t.column("MarketDataType").to_numpy()
        price = t.column("Price").to_numpy()
        vol = t.column("Volume").to_numpy()

        last_idx = np.flatnonzero(mdt == MDT_LAST)

        def prevailing(quote_type: int) -> np.ndarray:
            qidx = np.flatnonzero(mdt == quote_type)
            qprice = price[qidx]
            pos = np.searchsorted(qidx, last_idx, side="right") - 1
            out = np.full(len(last_idx), np.nan)
            ok = pos >= 0
            out[ok] = qprice[pos[ok]]
            return out

        return DayL1(
            date=date,
            ts=ts[last_idx],
            price=price[last_idx],
            volume=vol[last_idx],
            ask=prevailing(MDT_ASK),
            bid=prevailing(MDT_BID),
        )

    # ---------------- bars ----------------

    def _bars_path(self, symbol: str, date: str, period_s: int) -> Path:
        return (self.cache_root / "bars" / symbol.upper() / f"{period_s}s"
                / f"{date}.parquet")

    def load_bars(self, symbol: str, date: str, period_s: int,
                  day: DayL1 | None = None) -> BarDay:
        bp = self._bars_path(symbol, date, period_s)
        if bp.exists():
            t = pq.read_table(bp)
            return BarDay(*(t.column(c).to_numpy() for c in
                            ("ts_end", "open", "high", "low", "close",
                             "volume", "i0", "i1")))
        if day is None:
            day = self.load_day(symbol, date)
        bars = build_bars(day, period_s)
        bp.parent.mkdir(parents=True, exist_ok=True)
        tmp = bp.with_suffix(".tmp")
        pq.write_table(pa.table({
            "ts_end": bars.ts_end, "open": bars.open, "high": bars.high,
            "low": bars.low, "close": bars.close, "volume": bars.volume,
            "i0": bars.i0, "i1": bars.i1,
        }), tmp, compression="zstd")
        os.replace(tmp, bp)
        return bars


def build_bars(day: DayL1, period_s: int) -> BarDay:
    """Time bars from trade events. Bars with no trades are omitted (NT8-style).

    Bar timestamp is the close (right edge) of the interval, matching NT8.
    """
    if len(day) == 0:
        z = np.array([], dtype="int64")
        zf = np.array([], dtype="float64")
        return BarDay(z, zf, zf, zf, zf, z.copy(), z.copy(), z.copy())
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

    n = len(i0)
    o = day.price[i0]
    c = day.price[i1 - 1]
    h = np.maximum.reduceat(day.price, i0) if n else np.array([])
    l = np.minimum.reduceat(day.price, i0) if n else np.array([])
    v = np.add.reduceat(day.volume, i0) if n else np.array([], dtype="int64")
    # reduceat with increasing i0 reduces [i0[k], i0[k+1]) — but consecutive
    # bars may have gaps (empty bars removed), so segments [i0[k], i0[k+1])
    # can span more events than the bar. Recompute segment ends correctly:
    if n and not np.array_equal(i0[1:], i1[:-1]):
        h = np.empty(n)
        l = np.empty(n)
        v = np.empty(n, dtype="int64")
        for k in range(n):
            seg_p = day.price[i0[k]:i1[k]]
            h[k] = seg_p.max()
            l[k] = seg_p.min()
            v[k] = int(day.volume[i0[k]:i1[k]].sum())
    return BarDay(ts_end.astype("int64"), o.astype("float64"), h, l,
                  c.astype("float64"), v.astype("int64"),
                  i0.astype("int64"), i1.astype("int64"))
