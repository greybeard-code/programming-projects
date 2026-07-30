# SaberRenko — Specification, Evaluation, and Backtester Integration Plan

Status 2026-07-29: **specification complete and self-consistent; NOT yet
validated against a real NT8 chart export.** Every geometry law below was
derived from the shipped bar type's own logic and then reproduced in a
standalone reference simulator (scratch, not committed) — the worked examples
in §4 are that simulator's output, not hand-derivation. What is still missing
is the same thing ninZaRenko needed: a `gbBarExporter` dump off a real
SaberRenko chart run through `tools/compare_bars.py`. Treat §1–§4 as
authoritative-pending-parity, §6 as the honest evaluation, §7 as the plan.

**Provenance.** Source material is `nt8 code/SaberRenko/` — `SaberRenko.dll`
(assembly `SaberRenko` v1.0.0.1, NT8 export 8.1.7.2) plus an empty
NinjaScript stub `.cs` (the vendor ships no source). The bar type is
`NinjaTrader.NinjaScript.BarsTypes.SaberRenko`, 9 instance fields and one
`OnDataPoint`. Its behavior was read locally in the session scratch
directory; **no decompiled vendor source is committed to this repo**, per the
same policy applied to ninZa (see `research/ninZaRenko_spec.md` and
`ninZaRenko_BarType_Engineering_Summary.md`). The vendor is TradeSaber
(https://tradesaber.com/indicators/, an NT8 order-entry/automation shop);
their listing gives no parameter documentation beyond the default "Bar Size +
Offset (ticks) · e.g. 64 / 16", but its two marketing claims **independently
corroborate** the reverse-engineered geometry rather than adding new
information: *"These Renko bars have an actual opening price, which
increases backtesting accuracy"* matches §3.2 (bars are contiguous —
`open == previous close` — unlike ninZa's overlapping B−T geometry), and
*"No more 'Ghost' multiple candles on the same time stamp"* matches §6.1's
headline finding (the merge guarantees exactly one bar completes per tick,
eliminating the cascade ninZaRenko emits on gap moves). No documentation of
the Time Filter parameter or the merge arithmetic was found anywhere public.
The vendor's tutorial video (https://www.youtube.com/watch?v=sdsLZMp2fLY) was
not reviewable here (no transcript/description text was fetchable); worth a
manual watch before Phase 6 parity work in case it demonstrates edge-case
behavior (e.g. a real merge or time-filter event on a live chart) that the
reference simulator should be checked against.

---

## 1. Parameters

Three integers, all in **ticks** except the filter. NT8 property-panel names
are set by the bar type itself:

| NT8 UI label | NT8 `BarsPeriod` field | Symbol | Default |
|---|---|---|---|
| Bar Size (Ticks) | `Value` | **B** | 64 |
| Offset (Ticks) | `BaseBarsPeriodValue` | **O** | 16 |
| Time Filter (Sec) | `Value2` | **F** | 1 |

- Registered custom `BarsPeriodType` id = **20821** (ninZaRenko's is 12345 —
  relevant to `nt8config.py` template parsing).
- `BuiltFrom = Tick`, `IsIntraday = true`, `DaysToLoad = 3`,
  `GetInitialLookBackDays() = 3`.
- `BaseBarsPeriodType`, `PointAndFigurePriceType` and `ReversalType` are
  removed from the property grid — they are unused.
- Chart label is the bar's clock time; series name is `SaberRenko B/O`
  (the time filter does not appear in the name — a config-collision hazard
  for anything that keys on the display string).

Effective values are clamped: `B ← max(1, Value)`, `O ← max(1, BaseBarsPeriodValue)`,
`F ← max(1, Value2)`. **A SaberRenko bar can therefore never be shorter than
one second, even with Time Filter set to 0.**

### Constraints the parameters must satisfy

- **O ≤ B is mandatory.** With O > B the counter-trend trigger lands *above*
  a new up bar's own open, so essentially every tick completes a bar. Verified:
  B=16/O=64 prints an alternating ±32-tick bar on ~every tick for the whole
  run. This is not a crash, it is silent garbage — the parser must reject it.
- **B should be an exact multiple of O.** This is what makes the vendor's
  "every close stays on the renko grid" claim true (§3.4). B=64/O=16 (4×) is
  the shipped default.
- **O = B degenerates to classic renko**, cleanly: symmetric ±B bodies, no
  wicks, contiguous bars. Verified.

---

## 2. State and the per-tick rule

Five pieces of state matter (the bar type keeps them as fields; the trigger
levels are redundant with `cur_open`):

| State | Meaning |
|---|---|
| `cur_open` | the **anchor**: the virtual price the current bar's ±B triggers hang off |
| `bar_open_price` | actual open of the bar being formed (= the previous bar's close) |
| `bar_open_ts` | timestamp the current bar opened — the time-filter clock |
| `form_high` / `form_low` | running true extremes of the forming bar (display only, see §3.3) |

Derived every tick: `up_trigger = cur_open + B·tick`,
`down_trigger = cur_open − B·tick`.

**Per tick (price `p`, time `t`, volume `v`):**

1. **Session seed** — if this is the first bar, or the trading day rolled over
   and the series has *Break at EOD* set (`Bars.IsResetOnNewTradingDay`):
   set `cur_open = bar_open_price = form_high = form_low = p`,
   `bar_open_ts = t`, open a doji bar at `p` carrying this tick's volume, and
   stop. **The grid re-anchors at the session's first trade** — same class of
   day-boundary hazard as ninZa (see §5.3).
2. **Completion test** — a bar completes only if **both** hold:
   - *breakout*: `p ≥ up_trigger` **or** `p ≤ down_trigger`
     — **inclusive**, via NT8 `ApproxCompare` (1e-10). A tick landing exactly
     on the trigger **does** complete a bar. This is the opposite of
     ninZaRenko, whose breakout is strict `>`/`<`.
   - *time gate*: `t − bar_open_ts ≥ F` seconds.
3. **If both hold** — let `d = +1` for an up breakout (checked first,
   so an ambiguous tick resolves up), `trig` = the trigger crossed:
   - `overshoot = round((p − trig)·d / tick)` ticks past the trigger
   - `k = overshoot // O` — **whole Offset units of overshoot** (floor, ≥ 0)
   - `close = trig + d·k·O·tick` ← **the merge**: the completing close is
     snapped down to the lattice rather than printing one odd bar or N bricks
   - the bar closes with `high = max(bar_open_price, close, cur_open)` and
     `low = min(bar_open_price, close, cur_open)` and **zero additional
     volume** — this tick's volume is not its own
   - a new bar opens at `close` carrying this tick's volume, and:
     `bar_open_price = close`, `cur_open = close + d·(O − B)·tick`,
     `bar_open_ts = t`
   - the new bar is immediately updated to `close = p` with H/L spanning
     `[min(open, p), max(open, p)]` — i.e. the leftover `overshoot mod O`
     ticks are carried into the new bar, not discarded.
4. **Otherwise** — extend the forming bar: `form_high/form_low` absorb `p`,
   the bar's close becomes `p`, and its volume accumulates `v`.

**Exactly one bar can complete per tick.** Proof: the residual after snapping
is `overshoot mod O ∈ [0, O−1]` ticks, and the new bar's with-trend trigger
sits a full `O` away, so the new bar cannot also be in breakout on the same
tick. There is no cascade — this is the headline difference from every other
renko (§6.1).

### Reload path

If the bar type is re-instantiated over an existing bar array (chart reload,
series rebuild) it detects zeroed triggers and reconstructs the anchor from
the last two bars: `bar_open_price` = last bar's open, and the direction is
taken from whether the *second-to-last* bar closed at or above its own open
(a doji counts as up), giving `cur_open = bar_open_price ± (O − B)·tick`.
This is a real live-vs-historical divergence surface: a chart that has been
running continuously and one that was just reloaded can disagree about the
anchor after an ambiguous bar. It is irrelevant to a from-scratch backtest.

---

## 3. Derived geometry

Let `k = overshoot // O ≥ 0` (`k = 0` on a normal, non-merged bar). All values
in ticks. Verified against the reference simulator; §4 shows the raw output.

### 3.1 Bar shapes

| Bar | Body | Range (H−L) | Wick |
|---|---|---|---|
| Session's first bar | `B + k·O` | `B + k·O` | none |
| With-trend continuation | `O·(1 + k)` | `B + k·O` | `B − O`, on the side price came *from* |
| Reversal | `(2B − O) + k·O` | `(2B − O) + k·O` | none |

With the shipped defaults (B=64, O=16) that is: a continuation bar has a
**16-tick body and a 48-tick tail**, total range exactly 64; a reversal bar is
a **solid 112-tick** bar. Continuation and reversal are asymmetric **7:1**.

### 3.2 Trigger levels

After a bar closes at `C` in direction `d`, the next bar opens **at `C`**
(bars are contiguous — no overlap, unlike ninZa's B−T overlap) with anchor
`a = C − d·(B − O)`, giving:

- **with-trend** completion at `C + d·O` — only **O** ticks away
- **counter-trend** completion at `C − d·(2B − O)` — **2B − O** ticks away

Those are exactly ninZaRenko's thresholds with `T := O`. **SaberRenko and
ninZaRenko fire on the same price levels; they draw completely different
candles from them.** ninZa forces every body to B and overlaps the bars;
Saber draws the true move contiguously.

### 3.3 High/Low are entirely synthetic

A completed bar's H/L come **only** from `{open, close, cur_open}`. The true
traded extremes tracked in `form_high`/`form_low` are **overwritten at
completion and lost.** Consequences:

- A completed SaberRenko bar's OHLC is a pure function of (open, close,
  direction, B, O). **High and Low carry zero information beyond the close
  sequence.** ninZaRenko's H/L, by contrast, include the real trade extremes
  of the bar's tick span (verified against exports; `build_renko_bars` does
  this deliberately).
- Verified example (§4, bar #5): the real high inside that bar was 11 ticks
  above the printed high. The excursion is simply not in the bar.
- Every with-trend bar has range exactly B. **ATR on a trending SaberRenko
  series is very nearly constant** — which quietly guts any ATR-scaled stop,
  Keltner channel (PanaKanal), Donchian, or order-block (KO) logic.
- The *forming* bar shows real H/L; the *completed* bar does not. A
  `Calculate.OnEachTick` strategy reading `High[0]`/`Low[0]` intrabar sees
  values that the same bar will not have once closed — a genuine live-vs-
  backtest mismatch that is intrinsic to the bar type, not to our port.

### 3.4 The close lattice

Continuation steps are `±O`, reversal steps are `∓(2B − O)`, merges add `k·O`,
and the seed bar's step is `B`. If **B is a multiple of O**, all of those are
multiples of O, so every close in a session sits on a single lattice
`P_seed + n·O·tick`. That is the vendor's "every close stays on the renko
grid / every body is an exact multiple of the step" claim, and it is true —
*conditional on B % O == 0*, which the bar type does not enforce.

---

## 4. Worked examples (reference-simulator output)

**A. Steady 1-tick/second walk, B=64 O=16 F=1** (MNQ tick 0.25). Note bar 0
is the session seed (body 64, no wick), bars 1–4 are continuations (body 16,
range 64, 48-tick tail), bar 5 is the reversal (body −112, no wick):

```
  #     t      open      high       low     close  body_tk  rng_tk  upwick  dnwick
  0    65  20000.25  20016.25  20000.25  20016.25       64      64       0       0
  1    81  20016.25  20020.25  20004.25  20020.25       16      64       0      48
  2    97  20020.25  20024.25  20008.25  20024.25       16      64       0      48
  3   113  20024.25  20028.25  20012.25  20028.25       16      64       0      48
  4   129  20028.25  20032.25  20016.25  20032.25       16      64       0      48
  5   263  20032.25  20032.25  20004.25  20004.25    -112     112       0       0
  6   279  20004.25  20016.25  20000.25  20000.25     -16      64      48       0
 ...
 17   553  19960.25  19988.25  19960.25  19988.25     112     112       0       0
 18   569  19988.25  19992.25  19976.25  19992.25      16      64       0      48
```

Bar 5's printed high is its own open, 20032.25 — the market actually traded
up to 20035.00 inside that bar. That 11-tick excursion is gone (§3.3).

**B. The merge — 200 ticks up inside one second**, same parameters. A classic
or ninZa renko prints a cascade here; SaberRenko prints **one** bar with a
`8 × O = 128`-tick body and carries the 8-tick residual into the next bar:

```
  #     t      open      high       low     close  body_tk  rng_tk  upwick  dnwick
  0 2.063  20000.00  20016.00  20000.00  20016.00       64      64       0       0
  1    10  20016.00  20048.00  20004.00  20048.00      128     176       0      48
  2    20  20048.00  20050.00  20048.00  20050.00        8       8       0       0
```

**C. The time filter — poke past the trigger and retreat.** Price runs 67
ticks past the seed trigger at t=5–6 and is back at the open by t=7. With
F=30 the whole excursion is absorbed and the bar completes later, on the
lattice; with F=1 it prints:

```
F=30:   0    41  20000.00  20016.00  20000.00  20016.00       64      64       0       0
F=1:    0     6  20000.00  20016.00  20000.00  20016.00       64      64       0       0   <- completes at t=6
```

(The F=30 bar's printed high is 20016.00 even though 20016.75 traded — §3.3
again.)

**D. O = B = 64** reduces to textbook classic renko: bodies ±64, ranges 64,
no wicks, contiguous.

---

## 5. Behavior at boundaries

### 5.1 Volume
The completing tick's volume goes to the **next** bar, not the completing one.
This is the same convention as ninZaRenko rule 9, already implemented in
`build_renko_bars` (bars cache v6) — so our span convention `i1 = k`
(exclusive of the breakout tick) transfers unchanged.

### 5.2 Partial bars
At a session reset the forming bar is simply left as-is and a new one is
seeded. Unlike a completed bar, that partial keeps its **real** `form_high`/
`form_low` and its last traded close. So the last bar of every session is the
one bar in the series with honest wicks.

### 5.3 Day boundary
The reset fires on the **trading-day rollover of the session template**, when
*Break at EOD* is on. For CME instruments that is 17:00 ET, and since no ticks
print 17:00–18:00 ET it coincides exactly with the >30 min trade-gap rule that
`build_renko_bars` already uses. **This is the identical hazard that produced
the 2026-07-11 renko day-boundary bug** (day files are ET calendar days; an
18:00–16:55 session trades straight through midnight ET with no gap, and
resetting per file corrupted everything after midnight). The port must use
`Catalog.load_bars_sequence`'s carry machinery from day one — do not
re-litigate this.

If *Break at EOD* is off, there is no reset at all and the grid runs
continuously. The exporting chart's setting must be recorded when we do
parity work, because it changes the bars.

---

## 6. Evaluation: is it a better renko?

**For this repo's purposes: better in the ways that have actually cost us
time, worse in a way that matters to half our existing signal code.** It is
not a drop-in upgrade.

### 6.1 Genuine advantages over ninZaRenko

1. **No cascades.** Exactly one bar per tick, always. Our ninZa port has to
   emit extra bars with zero-length spans on gap moves, and `plot_day.py`
   notes that r100-4 prints thousands of overlapping bricks per day. Every
   SaberRenko bar maps 1:1 to a real, non-empty tick span. Bar-indexed logic
   (ConfirmationBars, `BarsSinceEntry`, lookback windows) becomes trustworthy.
2. **A violent move stays legible.** A 200-tick impulse is one bar with a
   128-tick body, not eight identical 16-tick bricks. The magnitude survives
   into the bar series instead of being flattened into a count.
3. **Minimum bar duration.** ≥ F seconds (≥ 1 always) bounds bars/day and
   filters the microstructure noise that small-T renko drowns in. §4C shows
   a full poke-and-retreat absorbed with no bar printed.
4. **Contiguous bars.** `open == previous close` exactly, so close-to-close
   and bar-to-bar arithmetic is exact and plots are gap-free. ninZa's
   overlapping B−T geometry makes both awkward.
5. **Deterministic lattice.** Given B % O == 0, every close is on
   `P_seed + n·O`. Reproducible and cache-friendly.
6. **Same trigger levels as ninZa** (§3.2) — so a signal that keys on
   threshold crossings, not on candle shape, should port across with the
   mapping `T → O`, which makes A/B comparison cheap.

### 6.2 Real costs and risks

1. **H/L are fabricated (§3.3).** This is the big one. Range is a constant B
   on every trend bar, so ATR is near-degenerate; and the true intrabar
   excursion is unrecoverable from the bar series. Our GodZillaKilla engines
   lean on H/L: PanaKanal (Keltner/ATR), KO (order blocks off bar highs and
   lows), SuperJump zones. **Do not expect GZK or Terminator numbers to
   transfer to SaberRenko** — those engines would be reading synthetic data.
   ninZa at least folds real extremes into H/L.
2. **Live/backtest divergence is built in.** The forming bar's H/L are real
   and get overwritten on close. Any `OnEachTick` strategy reading `High[0]`
   sees one thing live and another in the historical series.
3. **Bars depend on wall-clock time.** Completion is timestamp-driven, so the
   series is sensitive to tick timestamp fidelity and to missing ticks. Our
   Market-Replay parquet and NT8's own tick DB are different recordings
   (~6 s feed skew is already documented). Expect **lower NT8 parity than
   ninZa's 99.8%** and validate before trusting any number.
4. **Asymmetric bodies.** Continuation O vs reversal 2B−O — 7:1 at the
   defaults. Anything assuming "each brick = N ticks" breaks. It is a
   strongly trend-persistent bar: cheap to continue, expensive to turn.
   That's a deliberate design, but it must be sized deliberately too.
5. **Same day-boundary trap as ninZa** (§5.3) — mitigated, not absent.
6. **Black box, v1.0.0.1, no docs, no vendor found, no source.** One
   undocumented DLL. The reload-path anchor reconstruction (§2) is a
   divergence surface we cannot test from the Python side.
7. **Silent misconfiguration.** O > B is degenerate and B % O ≠ 0 quietly
   breaks the lattice; the bar type enforces neither.

### 6.3 Verdict

Worth adding as a **first-class bar type and running head-to-head**, not
worth adopting on sight. The cascade and noise problems it fixes are real
problems we have hit; the synthetic H/L is a real regression for our existing
signal stack. The cheap, honest experiment is: port it (§7), then re-run the
Terminator champion signal — which is close-based (SAR) and therefore the
fair test — on `s100-25-1` against its validated `r100-4`, same session, same
prop rules. If a close-driven strategy holds up or improves, the bar type
earns its place and the H/L-dependent engines get evaluated separately.

---

## 7. Backtester integration plan

Design goals: NT8-exact by default, zero disturbance to the existing renko
cache (~1 GB/symbol), and reuse of the carry machinery rather than a parallel
invention.

### Phase 1 — BarSpec + period grammar (`backtester/strategy.py`)

- `BarSpec` gains `bar_ticks`, `offset_ticks`, `filter_s`; `kind == "saber"`;
  `key = f"s{bar_ticks}-{offset_ticks}-{filter_s}"`.
- `parse_barspec`: new branch `s(\d+)-(\d+)(?:-(\d+))?`, filter defaulting to
  1 — e.g. `"s64-16"`, `"s64-16-2"`. No collision with time bars (`30s` puts
  the digits first) or renko (`r…`); add the saber branch before the others.
- Validate, in the style of the existing renko message: raise if `O > B`
  (degenerate, §1), and raise if `B % O != 0` (breaks the lattice, §3.4) —
  the second is stricter than the bar type itself, and deliberately so;
  loosen it only if a real chart export shows a non-multiple config in use.

### Phase 2 — builder (`backtester/data.py`)

- `build_saber_bars(day, bar_size, offset, filter_ns, carry=None) -> BarDay`,
  implementing §2 literally, with `approx_compare` semantics for the
  inclusive breakout (reuse `gbsignals/nt8math.py::approx_compare`, 1e-10).
- **Not** a price-change-only walk. The renko builder scans only ticks where
  price changed; that is wrong here, because a bar can sit in breakout waiting
  on the time gate and must complete on a later tick at the *same* price. Use
  the "earliest trigger in the remaining span" idiom already proven in
  `broker.resolve_span`: per bar,
  `start = max(open_idx + 1, searchsorted(ts, bar_open_ts + filter_ns))`,
  then a chunked `argmax` over `(price >= up) | (price <= down)` from `start`.
  That is O(bars) bounded scans, and it is exact.
- Spans: `i0 = open tick index`, `i1 = completing tick index` (exclusive —
  §5.1, same as renko v6). Volume/buy/sell sum over `[i0, i1)`.
- H/L per §3.1, from `{open, close, cur_open}` only. At a >30 min gap, emit
  the forming bar as a **partial with real extremes** (§5.2) and re-seed.
- `BarDay` gains a generic `end_state: tuple | None = None` for the carry —
  do **not** overload `end_anchor`/`end_dir`, leave the renko fields alone.
  Carry payload: `(cur_open, bar_open_price, bar_open_ts, form_high, form_low)`.
- `build_bars()` dispatch gains the `"saber"` branch.

### Phase 3 — cache + sequence loading (`backtester/data.py::Catalog`)

- `load_bars_sequence`: the carry-threading path currently keys off
  `spec.kind != "renko"`; widen to `kind in ("renko", "saber")` and dispatch
  to a new `_load_saber_cached` alongside `_load_renko_cached`, same
  cache-hit discipline (a hit must also match the stored carry-in), with the
  carry tuple JSON-encoded in the parquet metadata.
- **Do not bump `BARS_VERSION`.** Saber bars live under brand-new cache keys
  (`s64-16-1/`), so there is nothing to invalidate, and a bump would force a
  full rebuild of the existing renko cache for no reason.
- Engine, `compare_bars`, `compare_signals` and `plot_day` all already go
  through `load_bars_sequence` / `BarDay` — no changes needed in any of them.
  Sweeps, walk-forward and `secondary_periods` take period strings, so they
  pick the new type up for free.

### Phase 4 — NT8 template mapping (`backtester/nt8config.py`)

- Add `_BAR_TYPE_SABER = 20821`.
- `_parse_bar_spec` must additionally read `<BaseBarsPeriodValue>` and emit
  `f"s{value}-{base_value}-{value2}"`. Note `Value2` means *trend* for
  ninZaRenko and *time filter* for SaberRenko — dispatch on the type id, and
  keep the existing "not mapped" error for anything else.

### Phase 5 — tests (`tests/test_saber_bars.py`)

Hand-computed, in the style of `tests/test_renko_carry.py` and
`tests/conftest.py::make_day`:

1. seed bar: body B, no wick; 2. continuation: body O, range B, tail B−O;
3. reversal: body 2B−O, no wick; 4. merge: `k>0` gives body `O·(1+k)` and the
residual carries into the next bar; 5. time filter absorbs a poke-and-retreat
(§4C, both F values); 6. never more than one bar completes on a tick;
7. inclusive breakout — a tick exactly on the trigger **does** complete
(the ninZa test asserts the opposite; keep both, they document the
difference); 8. completing tick's volume lands on the next bar;
9. carry across a day-file boundary with no midnight reset, and a reset after
a >30 min gap; 10. `O == B` reproduces classic renko; 11. `O > B` and
`B % O != 0` raise from `parse_barspec`.

### Phase 6 — NT8 parity certification (the gate)

Nothing here is trustworthy until this passes. Same rig as the ninZa work:

1. Add `gbBarExporter` (already in `tools/`) to a SaberRenko chart —
   MNQ, 64/16/1, a date range inside the parquet repo, tick-built.
2. **Record the chart's session template and its *Break at EOD* setting**
   (§5.3) — it changes the bars.
3. `python tools\compare_bars.py bars_MNQ_SaberRenko_64-16.csv --symbol MNQ
   --period s64-16-1 --tolerance-s 2` (2 s is enough — bars are ≥1 s apart,
   unlike small-T renko which needed 10).
4. Accept at **≥99% identical OHLC mid-session**, matching what ninZa reached
   (99.8%). Because completion is time-driven (§6.2.3), expect residual
   mismatch from feed skew; mismatch **clustered at midnight ET** means the
   carry threading is wrong, not the geometry — see §5.3 before investigating
   anything else.
5. Repeat on a second config (e.g. 100/25/1) before declaring parity, as we
   did across five ninZa settings.

### Phase 7 — optional, after parity: the true-wick variant

Because §3.3 throws away real extremes, offer an opt-in
`BarSpec(..., real_wicks=True)` (key suffix `w`, e.g. `s64-16-1w`) that keeps
`form_high`/`form_low` in the completed bar. This **breaks NT8 parity by
construction** and must default off — but it gives strategies the intrabar
excursion that no NT8 SaberRenko user can see, which is exactly the kind of
L1 data edge this repo exists to exploit. Worth doing only if the bar type
survives Phase 6 and the head-to-head in §6.3.

### Effort

Phases 1–5 are roughly a day: the builder is ~120 lines and materially
simpler than `build_renko_bars` (no overlapping geometry, no multi-emit
loop, one bar per tick). Phase 6 depends on the user producing a chart
export. Phase 7 is an hour on top of a passing Phase 6.
