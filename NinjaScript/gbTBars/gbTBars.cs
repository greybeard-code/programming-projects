// gbTBars — GreyBeard build of the TBars bar type.
//
// v1.0.0  2026-08-04
//
// WHAT THIS IS
//   A trend/reversal renko-family bar with deliberately asymmetric thresholds
//   and a Heikin-Ashi presentation layer. One user parameter, "Speed Settings"
//   (N), from which everything derives:
//
//       trend offset  = N / 2 ticks   (with-trend continuation)
//       reversal      = N * 2 ticks   (against-trend)
//       open offset   = N     ticks   (synthetic open, back from the close)
//
//   So a reversal costs 4x a continuation — that asymmetry is the "T".
//   Behavioural spec, and the NT8-vs-backtester parity numbers behind it:
//   Python/backtester/research/TBars_spec.md.
//
// WHY IT IS SEPARATE FROM "TBars"
//   The vendor bar type registers class NinjaTrader.NinjaScript.BarsTypes.TBars
//   on BarsPeriodType 98765. This one is a distinct class on its own id, so the
//   two COEXIST — existing charts keep resolving the vendor build, and nothing
//   here can collide with it. Switch a chart to "gbTBars" to use this one.
//
// FIXES vs the vendor build (both verified 2026-08-04)
//   1. Removed two leftover debug Print() calls that fired on EVERY tick
//      ("else" and "maxExceeded || minExceeded"). On MNQ that is ~2.3M calls
//      per day of chart data loaded.
//   2. The session re-seed carried barDirection across the reset and seeded
//      the thresholds as open +/- trend*dir. A carried -1 put the up threshold
//      BELOW the down threshold, so the next tick satisfied both tests at once;
//      maxExceeded is evaluated first and won, and the bar printed with its
//      open above its own high (hand-verified: O=98.00 H=97.50 L=97.50).
//      Zeroing the direction collapses both thresholds symmetrically onto the
//      open — exactly how the chart's very first bar behaves — and matches the
//      Python port's reset_carries_dir=false path, so NT8 and the backtester
//      stay in agreement.
//   Also dropped: an unused license-check field, two never-called Heikin-Ashi
//   high/low helpers, and a duplicated state test. The blanket catch that
//   silently swallowed every exception now logs once, at Error level, so a
//   failure is visible instead of quietly producing corrupt bars.
//
// NOTE ON THE OUTPUT
//   The emitted OHLC is Heikin-Ashi transformed, and that is intentional — it
//   is what the chart shows. Close is a 4-way average; open is the midpoint of
//   the synthetic open and the prior close. High/low are the REAL extremes.
//   Breakout detection always runs on raw traded prices, never on the HA values.

#region Using declarations
using System;
using System.ComponentModel.DataAnnotations;
using NinjaTrader.Cbi;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
#endregion

namespace NinjaTrader.NinjaScript.BarsTypes
{
	public class gbTBars : BarsType
	{
		// GreyBeard custom BarsPeriodType id block: 91000-91099. Must not clash
		// with NT8's own (0-16), ninZaRenko (12345), SaberRenko (20821), or the
		// vendor TBars builds (98765, and the older 2015 / 15 — note 15 is NT8's
		// built-in Delta, which is why that build was reissued).
		private const int GbBarsPeriodTypeId = 91001;

		#region Developer
		[Display(Name = "Author", Order = 0, GroupName = "0. Developer")]
		public string Author => "GreyBeard";

		[Display(Name = "Version", Order = 1, GroupName = "0. Developer")]
		public string Version => "1.0.0";

		[Display(Name = "Website", Order = 2, GroupName = "0. Developer")]
		public string Website => "https://greybeardconsulting.net/";
		#endregion

		private double upThreshold;      // break above this -> with-trend close (up)
		private double downThreshold;    // break below this -> close down
		private double trendOffset;      // N/2 ticks, in price
		private double reversalOffset;   // N*2 ticks, in price
		private double openOffset;       // N   ticks, in price
		private int barDir;              // +1 up, -1 down, 0 = freshly seeded
		private bool faultLogged;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description = "GreyBeard TBars — asymmetric trend/reversal bars (Heikin-Ashi presentation)";
				Name = "gbTBars";
				BarsPeriod = new BarsPeriod
				{
					BarsPeriodType = (BarsPeriodType)GbBarsPeriodTypeId,
					BarsPeriodTypeName = Name
				};
				BuiltFrom = BarsPeriodType.Tick;
				DaysToLoad = 5;
				IsIntraday = true;
			}
			else if (State == State.Configure)
			{
				// Only "Speed Settings" is user-facing; Value/Value2 are derived
				// from it on every load, so hide them rather than let them drift.
				Properties.Remove(Properties.Find("BaseBarsPeriodType", true));
				Properties.Remove(Properties.Find("PointAndFigurePriceType", true));
				Properties.Remove(Properties.Find("ReversalType", true));
				Properties.Remove(Properties.Find("Value", true));
				Properties.Remove(Properties.Find("Value2", true));
				SetPropertyName("BaseBarsPeriodValue", "Speed Settings");

				// Integer division is deliberate — it is what the vendor build
				// does, and keeping it means a chart at a given Speed prints the
				// same bars on either bar type. N < 2 gives a zero-tick trend
				// threshold (a bar per uptick), so ApplyDefaultBasePeriodValue
				// pins the default to 2.
				BarsPeriod.Value = BarsPeriod.BaseBarsPeriodValue / 2;
				BarsPeriod.Value2 = BarsPeriod.BaseBarsPeriodValue * 2;
			}
		}

		public override int GetInitialLookBackDays(BarsPeriod barsPeriod, TradingHours tradingHours, int barsBack)
		{
			return 3;
		}

		protected override void OnDataPoint(Bars bars, double open, double high, double low, double close,
											DateTime time, long volume, bool isBar, double bid, double ask)
		{
			try
			{
				if (SessionIterator == null)
					SessionIterator = new SessionIterator(bars);

				bool isNewSession = SessionIterator.IsNewSession(time, isBar);
				if (isNewSession)
					SessionIterator.CalculateTradingDay(time, isBar);

				// bars.IsResetOnNewTradingDay is the Data Series "Break at EOD"
				// toggle (it defaults ON). With it off, the grid runs continuously.
				if (bars.Count == 0 || (bars.IsResetOnNewTradingDay && isNewSession))
				{
					SeedGrid(bars, open, high, low, close, time, volume);
					bars.LastPrice = close;
					return;
				}

				bool maxExceeded = bars.Instrument.MasterInstrument.Compare(close, upThreshold) > 0;
				bool minExceeded = bars.Instrument.MasterInstrument.Compare(close, downThreshold) < 0;
				int last = bars.Count - 1;

				if (!maxExceeded && !minExceeded)
				{
					// Inside the band: extend the forming bar's real extremes and
					// rewrite its Heikin-Ashi close off the raw traded price.
					double runHigh = Math.Max(close, bars.GetHigh(last));
					double runLow = Math.Min(close, bars.GetLow(last));
					UpdateBar(bars, runHigh, runLow,
							  HeikinAshiClose(bars.GetOpen(last), runHigh, runLow, close), time, volume);
					bars.LastPrice = close;
					return;
				}

				// Breakout. The tick is strictly beyond the threshold, so the clamp
				// always collapses onto the threshold itself — completing closes
				// therefore stay exactly on the tick grid.
				double clamped = maxExceeded
					? Math.Min(close, upThreshold)
					: Math.Max(close, downThreshold);
				barDir = maxExceeded ? 1 : -1;
				double syntheticOpen = clamped - openOffset * barDir;

				// Close the bar out. Replacing (not Max-ing) the breakout side is
				// lossless: every updating tick sat inside the band by definition.
				double closedHigh = maxExceeded ? clamped : bars.GetHigh(last);
				double closedLow = minExceeded ? clamped : bars.GetLow(last);
				double closedHaClose = HeikinAshiClose(bars.GetOpen(last), closedHigh, closedLow, clamped);
				UpdateBar(bars, closedHigh, closedLow, closedHaClose, time, volume);

				// Re-arm the thresholds around the close: cheap with-trend, dear
				// against-trend. This is the whole point of the bar type.
				upThreshold = clamped + (barDir > 0 ? trendOffset : reversalOffset);
				downThreshold = clamped - (barDir > 0 ? reversalOffset : trendOffset);

				// Open the successor. It starts spanning openOffset ticks, which is
				// what produces the overlapping look.
				double newOpen = HeikinAshiOpen(syntheticOpen, bars.GetClose(last));
				double newHigh = maxExceeded ? clamped : syntheticOpen;
				double newLow = minExceeded ? clamped : syntheticOpen;
				AddBar(bars, newOpen, newHigh, newLow,
					   HeikinAshiClose(newOpen, newHigh, newLow, clamped), time, volume);

				bars.LastPrice = close;
			}
			catch (Exception ex)
			{
				// Stay alive so a single bad data point cannot kill the chart, but
				// say so once — the vendor build swallowed this silently and then
				// carried on building bars from corrupt state.
				if (!faultLogged)
				{
					faultLogged = true;
					Log("gbTBars: " + ex, LogLevel.Error);
				}
			}
		}

		/// <summary>
		/// Start (or restart) the grid at this data point. Used for the very first
		/// bar and, when "Break at EOD" is on, at each new session.
		/// </summary>
		private void SeedGrid(Bars bars, double open, double high, double low, double close,
							  DateTime time, long volume)
		{
			double tickSize = bars.Instrument.MasterInstrument.TickSize;
			trendOffset = bars.BarsPeriod.Value * tickSize;
			reversalOffset = bars.BarsPeriod.Value2 * tickSize;
			openOffset = bars.BarsPeriod.BaseBarsPeriodValue * tickSize;

			// FIX (see header note 2): drop the carried direction. Leaving it set
			// inverts the thresholds after a down session and emits a bar whose
			// open sits above its own high.
			barDir = 0;
			upThreshold = open + trendOffset * barDir;
			downThreshold = open - trendOffset * barDir;

			// Both thresholds now sit on the open, so this seed bar is a one-tick
			// doji that the next differing tick completes. That is by design and
			// matches the vendor build's very first bar.
			AddBar(bars, open, high, low, HeikinAshiClose(open, high, low, close), time, volume);
		}

		public override void ApplyDefaultBasePeriodValue(BarsPeriod period)
		{
			// Without this, switching a chart to this bar type inherits whatever
			// BaseBarsPeriodValue the previous type had — arrive from a 1-minute
			// chart and N=1 gives a zero-tick trend threshold.
			period.BaseBarsPeriodValue = 2;
		}

		public override void ApplyDefaultValue(BarsPeriod period)
		{
			period.Value = 1;
			period.Value2 = 4;
			period.BaseBarsPeriodValue = 2;
		}

		public override string ChartLabel(DateTime dateTime)
		{
			return Name;
		}

		public override double GetPercentComplete(Bars bars, DateTime now)
		{
			return 0.0;
		}

		private static double HeikinAshiOpen(double open, double close)
		{
			return (open + close) * 0.5;
		}

		private static double HeikinAshiClose(double open, double high, double low, double close)
		{
			return (open + high + low + close) * 0.25;
		}
	}
}
