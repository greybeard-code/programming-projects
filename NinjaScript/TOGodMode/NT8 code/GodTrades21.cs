#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Xml.Serialization;
using System.Windows.Media;
using NinjaTrader.Data;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Tools;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

namespace NinjaTrader.NinjaScript
{
	public enum GodTrades21EarlyTouchHandling
	{
		StopLineImmediately,
		IgnoreTouchesUntilValid
	}

	public enum GodTrades21ValidTouchBehavior
	{
		StopLineOnly,
		StopLineAndMarkContinuation
	}

	public enum GodTrades21ContinuationConfirmationMode
	{
		TouchOnly,
		RequireCloseBeyondLine,
		RequireCloseBeyondFullZone
	}

	public enum GodTrades21LinePriceMode
	{
		Midpoint,
		PreviousCloseEdge,
		CurrentOpenEdge
	}

	public enum GodTrades21TargetMode
	{
		None,
		OppositeBollingerBand,
		FixedTicks
	}

	public enum GodTrades21FcBollingerLocationSource
	{
		Close,
		WickExtreme,
		HLC3,
		BodyMidpoint
	}
}

namespace NinjaTrader.NinjaScript.Indicators
{
	public class GodTrades21 : Indicator
	{
		private class GapInfo
		{
			public int		Direction;		// 1 = bullish gap-up, -1 = bearish gap-down
			public int		CreationBar;
			public double	PreviousClose;
			public double	CurrentOpen;
			public double	ZoneLow;
			public double	ZoneHigh;
			public double	LinePrice;
			public bool		IsValid;
			public bool		Touched;
			public bool		InvalidTooEarly;
			public string	TagBase;
			public string	LineTag;
			public string	ZoneTag;
		}

		private class PendingContinuation
		{
			public GapInfo	Gap;
			public int		TouchBar;
		}

		private List<GapInfo> activeGaps;
		private List<PendingContinuation> pendingContinuations;
		private Bollinger bollinger;
		private int signalDrawCounter;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Name						= "GodTrades21";
				Description					= "GodTrades20 base with OBR redefined as an opposite-direction body engulf. Includes an option to allow or reject OBR signal bars that extend outside the Bollinger Bands.";
				Calculate					= Calculate.OnBarClose;
				IsOverlay					= true;
				DrawOnPricePanel			= true;
				DisplayInDataBox			= true;
				ShowTransparentPlotsInDataBox = true;
				PaintPriceMarkers			= false;
				IsAutoScale					= false;
				IsSuspendedWhileInactive	= true;
				BarsRequiredToPlot			= 20;

				MinimumGapSizeTicks			= 1;
				MinimumBarsBeforeValid		= 3;
				MinimumBodyTicks			= 0;
				MaximumGapBarRangeTicks		= 0;
				MaximumActiveGapsToTrack	= 300;

				EarlyTouchHandling			= GodTrades21EarlyTouchHandling.StopLineImmediately;
				ValidTouchBehavior			= GodTrades21ValidTouchBehavior.StopLineAndMarkContinuation;

				EnableContinuationSignals	= true;
				UseBollingerMidpointFilterForContinuation = true;
				FcBollingerLocationSource	= GodTrades21FcBollingerLocationSource.WickExtreme;
				FcLongBelowMidpointPercent	= 50.0;
				FcShortAboveMidpointPercent	= 50.0;
				ContinuationConfirmationMode = GodTrades21ContinuationConfirmationMode.RequireCloseBeyondFullZone;
				ConfirmationBarsAfterTouch	= 2;
				RequireSignalCandleDirection = true;
				RequireCorrectContinuationApproach = true;

				EnableOutsideBarReversalSignals = true;
				PaintOutsideBarReversalBars = true;
				ShowOutsideBarReversalMarkers = true;
				UseBollingerMidpointFilterForOutsideBarReversal = true;
				AllowObrBarOutsideBollingerBand = true;
				BearishObrUpperBandTouchToleranceTicks = 4;
				BullishObrLowerBandTouchToleranceTicks = 4;

				LinePriceMode				= GodTrades21LinePriceMode.Midpoint;
				ShowGapLine					= true;
				ShowGapZone					= false;
				ShowTouchMarker				= true;
				ShowContinuationMarkers		= true;
				ShowBollingerGapMarkers		= true;
				ShowSignalLabels			= true;
				UseTouchedLineColor			= false;
				GapLineWidth				= 2;
				GapLineStyle				= DashStyleHelper.Solid;
				ZoneOpacity					= 12;
				SignalMarkerOffsetTicks		= 3;
				SignalLabelOffsetTicks		= 7;

				EnableBollingerGapSignals	= true;
				BollingerPeriod				= 20;
				BollingerStdDev				= 2.0;
				BollingerBandProximityTicks	= 8;

				EnableSpiderwebWarning		= true;
				ShowSpiderwebWarningText	= true;
				SpiderwebDistanceTicks		= 100;
				SpiderwebLineCount			= 5;
				SpiderwebTextFontSize		= 15;

				UseSignalTimeFilter			= false;
				SignalStartTime				= 101500;
				SignalEndTime				= 150000;

				SuggestedStopOffsetTicks		= 0;
				TargetMode					= GodTrades21TargetMode.OppositeBollingerBand;
				FixedTargetTicks			= 40;

				BullishGapLineBrush			= Brushes.White;
				BearishGapLineBrush			= Brushes.White;
				TouchedGapLineBrush			= Brushes.Gray;
				InvalidGapBrush				= Brushes.Red;
				BullishZoneBrush			= Brushes.DodgerBlue;
				BearishZoneBrush			= Brushes.Magenta;
				TouchMarkerBrush			= Brushes.Gold;

				BollingerLongBrush			= Brushes.Lime;
				BollingerShortBrush			= Brushes.Red;
				ContinuationLongBrush		= Brushes.LimeGreen;
				ContinuationShortBrush		= Brushes.OrangeRed;
				OutsideBarBullishPaintBrush	= Brushes.LimeGreen;
				OutsideBarBearishPaintBrush	= Brushes.Red;
				OutsideBarBullishMarkerBrush = Brushes.Lime;
				OutsideBarBearishMarkerBrush = Brushes.Red;
				SpiderwebWarningBrush		= Brushes.Orange;

				AddPlot(Brushes.Transparent, "BullishGapFormed");
				AddPlot(Brushes.Transparent, "BearishGapFormed");
				AddPlot(Brushes.Transparent, "BullishGapTouched");
				AddPlot(Brushes.Transparent, "BearishGapTouched");
				AddPlot(Brushes.Transparent, "InvalidGapTouched");

				AddPlot(Brushes.Transparent, "ContinuationLong");
				AddPlot(Brushes.Transparent, "ContinuationShort");

				AddPlot(Brushes.Transparent, "BollingerGapLong");
				AddPlot(Brushes.Transparent, "BollingerGapShort");

				AddPlot(Brushes.Transparent, "ActiveGapCount");
				AddPlot(Brushes.Transparent, "ValidActiveGapCount");
				AddPlot(Brushes.Transparent, "PendingContinuationCount");
				AddPlot(Brushes.Transparent, "SpiderwebCount");
				AddPlot(Brushes.Transparent, "SpiderwebWarning");
				AddPlot(Brushes.Transparent, "NearestGapPrice");

				AddPlot(Brushes.Transparent, "SuggestedEntryPrice");
				AddPlot(Brushes.Transparent, "SuggestedStopPrice");
				AddPlot(Brushes.Transparent, "SuggestedTargetPrice");

				AddPlot(Brushes.Transparent, "SignalDirection");
				AddPlot(Brushes.Transparent, "SignalCode");
				AddPlot(Brushes.Transparent, "MasterLongSignal");
				AddPlot(Brushes.Transparent, "MasterShortSignal");
				AddPlot(Brushes.Transparent, "OutsideBarReversalSignal");
			}
			else if (State == State.DataLoaded)
			{
				activeGaps = new List<GapInfo>();
				pendingContinuations = new List<PendingContinuation>();
				bollinger = Bollinger(BollingerStdDev, BollingerPeriod);
				signalDrawCounter = 0;
			}
		}

		protected override void OnBarUpdate()
		{
			ResetPlotValues();

			if (CurrentBar < 1)
				return;

			UpdatePendingContinuations();
			UpdateActiveGaps();
			DetectNewGap();
			DetectOutsideBarReversal();
			TrimOldestActiveGaps();
			UpdateContextPlots();
		}

		private void ResetPlotValues()
		{
			BullishGapFormed[0]			= 0;
			BearishGapFormed[0]			= 0;
			BullishGapTouched[0]		= 0;
			BearishGapTouched[0]		= 0;
			InvalidGapTouched[0]		= 0;

			ContinuationLong[0]			= 0;
			ContinuationShort[0]		= 0;

			BollingerGapLong[0]			= 0;
			BollingerGapShort[0]		= 0;

			ActiveGapCount[0]			= 0;
			ValidActiveGapCount[0]		= 0;
			PendingContinuationCount[0]	= 0;
			SpiderwebCount[0]			= 0;
			SpiderwebWarning[0]			= 0;
			NearestGapPrice[0]			= 0;

			SuggestedEntryPrice[0]		= 0;
			SuggestedStopPrice[0]		= 0;
			SuggestedTargetPrice[0]		= 0;

			SignalDirection[0]			= 0;
			SignalCode[0]				= 0;
			MasterLongSignal[0]			= 0;
			MasterShortSignal[0]		= 0;
			OutsideBarReversalSignal[0]	= 0;

			BarBrushes[0]				= null;
			CandleOutlineBrushes[0]		= null;
		}

		private void DetectNewGap()
		{
			bool previousBullish = Close[1] > Open[1];
			bool currentBullish  = Close[0] > Open[0];

			bool previousBearish = Close[1] < Open[1];
			bool currentBearish  = Close[0] < Open[0];

			if (!PassesBodyFilter())
				return;

			if (!PassesGapBarRangeFilter())
				return;

			bool bullishGap =
				previousBullish &&
				currentBullish &&
				Open[0] > Close[1] &&
				((Open[0] - Close[1]) / TickSize) >= MinimumGapSizeTicks;

			bool bearishGap =
				previousBearish &&
				currentBearish &&
				Open[0] < Close[1] &&
				((Close[1] - Open[0]) / TickSize) >= MinimumGapSizeTicks;

			if (bullishGap)
			{
				CreateGap(1, Close[1], Open[0]);
				BullishGapFormed[0] = 1;
				MarkBollingerGapIfNeeded(1);
			}
			else if (bearishGap)
			{
				CreateGap(-1, Open[0], Close[1]);
				BearishGapFormed[0] = -1;
				MarkBollingerGapIfNeeded(-1);
			}
		}


		private void DetectOutsideBarReversal()
		{
			if (!EnableOutsideBarReversalSignals)
				return;

			if (!IsSignalTimeAllowed())
				return;

			if (bollinger == null || CurrentBar < BollingerPeriod)
				return;

			bool previousBullish = Close[1] > Open[1];
			bool previousBearish = Close[1] < Open[1];
			bool currentBullish  = Close[0] > Open[0];
			bool currentBearish  = Close[0] < Open[0];

			// Opposite-direction BODY engulf:
			// Bearish: the current bearish body fully covers the previous bullish body.
			// Bullish: the current bullish body fully covers the previous bearish body.
			// Equal body edges are accepted as an engulf.
			bool bearishBodyEngulf =
				previousBullish &&
				currentBearish &&
				Open[0] >= Close[1] &&
				Close[0] <= Open[1];

			bool bullishBodyEngulf =
				previousBearish &&
				currentBullish &&
				Open[0] <= Close[1] &&
				Close[0] >= Open[1];

			double upperBand = bollinger.Upper[0];
			double lowerBand = bollinger.Lower[0];

			// The tolerance still permits a signal that approaches the band from inside.
			// When AllowObrBarOutsideBollingerBand is ON, a wick may also pierce beyond the band.
			// When it is OFF, the complete signal bar must remain inside the applicable band.
			bool bearishBandLocation =
				High[0] >= upperBand - BearishObrUpperBandTouchToleranceTicks * TickSize &&
				(AllowObrBarOutsideBollingerBand || High[0] <= upperBand);

			bool bullishBandLocation =
				Low[0] <= lowerBand + BullishObrLowerBandTouchToleranceTicks * TickSize &&
				(AllowObrBarOutsideBollingerBand || Low[0] >= lowerBand);

			bool bearishOutsideReversal =
				bearishBodyEngulf &&
				bearishBandLocation &&
				PassesOutsideBarBollingerMidpointFilter(-1);

			bool bullishOutsideReversal =
				bullishBodyEngulf &&
				bullishBandLocation &&
				PassesOutsideBarBollingerMidpointFilter(1);

			if (bearishOutsideReversal)
			{
				OutsideBarReversalSignal[0] = -1.0;

				// OBR does not overwrite Bollinger Gap or FC shared signals on the same bar.
				if (SignalCode[0] == 0)
				{
					SignalDirection[0] = -1;
					SignalCode[0] = -3;
					MasterShortSignal[0] = -1;
					SetSuggestedPrices(-1);
				}

				if (PaintOutsideBarReversalBars)
				{
					BarBrushes[0] = OutsideBarBearishPaintBrush;
					CandleOutlineBrushes[0] = OutsideBarBearishPaintBrush;
				}

				if (ShowOutsideBarReversalMarkers)
				{
					string tag = "GodTrades21_OBR_Short_" + CurrentBar + "_" + signalDrawCounter++;
					Draw.Diamond(this, tag, false, 0, High[0] + SignalMarkerOffsetTicks * TickSize, OutsideBarBearishMarkerBrush);

					if (ShowSignalLabels)
						Draw.Text(this, tag + "_Label", "OBR", 0, High[0] + SignalLabelOffsetTicks * TickSize, OutsideBarBearishMarkerBrush);
				}
			}
			else if (bullishOutsideReversal)
			{
				OutsideBarReversalSignal[0] = 1.0;

				// OBR does not overwrite Bollinger Gap or FC shared signals on the same bar.
				if (SignalCode[0] == 0)
				{
					SignalDirection[0] = 1;
					SignalCode[0] = 3;
					MasterLongSignal[0] = 1;
					SetSuggestedPrices(1);
				}

				if (PaintOutsideBarReversalBars)
				{
					BarBrushes[0] = OutsideBarBullishPaintBrush;
					CandleOutlineBrushes[0] = OutsideBarBullishPaintBrush;
				}

				if (ShowOutsideBarReversalMarkers)
				{
					string tag = "GodTrades21_OBR_Long_" + CurrentBar + "_" + signalDrawCounter++;
					Draw.Diamond(this, tag, false, 0, Low[0] - SignalMarkerOffsetTicks * TickSize, OutsideBarBullishMarkerBrush);

					if (ShowSignalLabels)
						Draw.Text(this, tag + "_Label", "OBR", 0, Low[0] - SignalLabelOffsetTicks * TickSize, OutsideBarBullishMarkerBrush);
				}
			}
		}

		private bool PassesBodyFilter()
		{
			if (MinimumBodyTicks <= 0)
				return true;

			double previousBodyTicks = Math.Abs(Close[1] - Open[1]) / TickSize;
			double currentBodyTicks  = Math.Abs(Close[0] - Open[0]) / TickSize;

			return previousBodyTicks >= MinimumBodyTicks && currentBodyTicks >= MinimumBodyTicks;
		}

		private bool PassesGapBarRangeFilter()
		{
			if (MaximumGapBarRangeTicks <= 0)
				return true;

			double currentRangeTicks = (High[0] - Low[0]) / TickSize;
			return currentRangeTicks <= MaximumGapBarRangeTicks;
		}

		private void CreateGap(int direction, double zoneLow, double zoneHigh)
		{
			GapInfo gap = new GapInfo();

			gap.Direction		= direction;
			gap.CreationBar		= CurrentBar;
			gap.PreviousClose	= Close[1];
			gap.CurrentOpen		= Open[0];
			gap.ZoneLow			= Math.Min(zoneLow, zoneHigh);
			gap.ZoneHigh		= Math.Max(zoneLow, zoneHigh);
			gap.LinePrice		= GetLinePrice(gap);
			gap.IsValid			= false;
			gap.Touched			= false;
			gap.InvalidTooEarly	= false;
			gap.TagBase			= "GodTrades21_Gap_" + CurrentBar + "_" + (direction > 0 ? "Bull" : "Bear");
			gap.LineTag			= gap.TagBase + "_Line";
			gap.ZoneTag			= gap.TagBase + "_Zone";

			activeGaps.Add(gap);
			DrawGap(gap, false, false);
		}

		private double GetLinePrice(GapInfo gap)
		{
			if (LinePriceMode == GodTrades21LinePriceMode.PreviousCloseEdge)
				return gap.PreviousClose;

			if (LinePriceMode == GodTrades21LinePriceMode.CurrentOpenEdge)
				return gap.CurrentOpen;

			return (gap.ZoneLow + gap.ZoneHigh) * 0.5;
		}

		private void UpdateActiveGaps()
		{
			if (activeGaps == null || activeGaps.Count == 0)
				return;

			for (int i = activeGaps.Count - 1; i >= 0; i--)
			{
				GapInfo gap = activeGaps[i];

				int age = CurrentBar - gap.CreationBar;
				gap.IsValid = age >= MinimumBarsBeforeValid;

				bool shouldCheckTouch = CurrentBar > gap.CreationBar;

				if (EarlyTouchHandling == GodTrades21EarlyTouchHandling.IgnoreTouchesUntilValid && !gap.IsValid)
					shouldCheckTouch = false;

				bool touched = shouldCheckTouch && BarOverlapsZone(gap);

				if (touched)
				{
					gap.Touched = true;
					gap.InvalidTooEarly = !gap.IsValid;

					if (gap.Direction > 0)
						BullishGapTouched[0] = 1;
					else
						BearishGapTouched[0] = -1;

					if (gap.InvalidTooEarly)
					{
						InvalidGapTouched[0] = gap.Direction;
						DrawGap(gap, true, true);
					}
					else
					{
						DrawGap(gap, true, false);
					}

					if (ShowTouchMarker)
						Draw.Dot(this, gap.TagBase + "_Touch_" + CurrentBar, false, 0, gap.LinePrice, TouchMarkerBrush);

					if (gap.IsValid)
						CreatePendingContinuationAndEvaluate(gap);

					activeGaps.RemoveAt(i);
				}
				else
				{
					DrawGap(gap, false, false);
				}
			}
		}

		private bool BarOverlapsZone(GapInfo gap)
		{
			return High[0] >= gap.ZoneLow && Low[0] <= gap.ZoneHigh;
		}

		private void DrawGap(GapInfo gap, bool touched, bool invalid)
		{
			int startBarsAgo = CurrentBar - gap.CreationBar;
			Brush lineBrush = GetLineBrush(gap, touched, invalid);

			if (ShowGapLine)
			{
				Draw.Line(
					this,
					gap.LineTag,
					false,
					startBarsAgo,
					gap.LinePrice,
					0,
					gap.LinePrice,
					lineBrush,
					GapLineStyle,
					GapLineWidth);
			}
			else
			{
				RemoveDrawObject(gap.LineTag);
			}

			if (ShowGapZone)
			{
				Draw.Rectangle(
					this,
					gap.ZoneTag,
					false,
					startBarsAgo,
					gap.ZoneHigh,
					0,
					gap.ZoneLow,
					Brushes.Transparent,
					invalid ? InvalidGapBrush : GetZoneBrush(gap),
					ZoneOpacity);
			}
			else
			{
				RemoveDrawObject(gap.ZoneTag);
			}
		}

		private Brush GetLineBrush(GapInfo gap, bool touched, bool invalid)
		{
			if (invalid)
				return InvalidGapBrush;

			if (touched && UseTouchedLineColor)
				return TouchedGapLineBrush;

			return gap.Direction > 0 ? BullishGapLineBrush : BearishGapLineBrush;
		}

		private Brush GetZoneBrush(GapInfo gap)
		{
			return gap.Direction > 0 ? BullishZoneBrush : BearishZoneBrush;
		}

		private void CreatePendingContinuationAndEvaluate(GapInfo gap)
		{
			if (!EnableContinuationSignals)
				return;

			if (ValidTouchBehavior != GodTrades21ValidTouchBehavior.StopLineAndMarkContinuation)
				return;

			if (!IsSignalTimeAllowed())
				return;

			if (!PassesContinuationApproach(gap))
				return;

			PendingContinuation pending = new PendingContinuation();
			pending.Gap = gap;
			pending.TouchBar = CurrentBar;

			bool signaled = EvaluatePendingContinuation(pending);

			if (!signaled && ConfirmationBarsAfterTouch > 0)
				pendingContinuations.Add(pending);
		}

		private bool PassesContinuationApproach(GapInfo gap)
		{
			if (!RequireCorrectContinuationApproach)
				return true;

			if (gap.Direction > 0)
				return Close[1] >= gap.ZoneHigh || Open[0] >= gap.ZoneHigh;

			return Close[1] <= gap.ZoneLow || Open[0] <= gap.ZoneLow;
		}

		private void UpdatePendingContinuations()
		{
			if (pendingContinuations == null || pendingContinuations.Count == 0)
				return;

			for (int i = pendingContinuations.Count - 1; i >= 0; i--)
			{
				PendingContinuation pending = pendingContinuations[i];
				int barsSinceTouch = CurrentBar - pending.TouchBar;

				if (barsSinceTouch <= 0)
					continue;

				if (barsSinceTouch > ConfirmationBarsAfterTouch)
				{
					pendingContinuations.RemoveAt(i);
					continue;
				}

				bool signaled = EvaluatePendingContinuation(pending);

				if (signaled)
					pendingContinuations.RemoveAt(i);
			}
		}

		private bool EvaluatePendingContinuation(PendingContinuation pending)
		{
			if (pending == null || pending.Gap == null)
				return false;

			if (!IsSignalTimeAllowed())
				return false;

			if (pending.Gap.Direction > 0)
			{
				if (IsLongContinuation(pending.Gap))
				{
					SetLongContinuationSignal();
					return true;
				}
			}
			else
			{
				if (IsShortContinuation(pending.Gap))
				{
					SetShortContinuationSignal();
					return true;
				}
			}

			return false;
		}

		private double GetFcBollingerTestPrice(int direction)
		{
			if (FcBollingerLocationSource == GodTrades21FcBollingerLocationSource.Close)
				return Close[0];

			if (FcBollingerLocationSource == GodTrades21FcBollingerLocationSource.HLC3)
				return (High[0] + Low[0] + Close[0]) / 3.0;

			if (FcBollingerLocationSource == GodTrades21FcBollingerLocationSource.BodyMidpoint)
				return (Open[0] + Close[0]) / 2.0;

			// WickExtreme default:
			// Long FC checks Low. Short FC checks High.
			return direction > 0 ? Low[0] : High[0];
		}

		private bool PassesBollingerMidpointContinuationFilter(int direction)
		{
			if (!UseBollingerMidpointFilterForContinuation)
				return true;

			if (bollinger == null || CurrentBar < BollingerPeriod)
				return false;

			double middle = bollinger.Middle[0];
			double upper  = bollinger.Upper[0];
			double lower  = bollinger.Lower[0];
			double testPrice = GetFcBollingerTestPrice(direction);

			if (direction > 0)
			{
				double pct = FcLongBelowMidpointPercent / 100.0;
				double threshold = middle - ((middle - lower) * pct);

				return testPrice <= threshold;
			}
			else
			{
				double pct = FcShortAboveMidpointPercent / 100.0;
				double threshold = middle + ((upper - middle) * pct);

				return testPrice >= threshold;
			}
		}


		private bool PassesOutsideBarBollingerMidpointFilter(int direction)
		{
			if (!UseBollingerMidpointFilterForOutsideBarReversal)
				return true;

			if (bollinger == null || CurrentBar < BollingerPeriod)
				return false;

			double middle = bollinger.Middle[0];
			double upper  = bollinger.Upper[0];
			double lower  = bollinger.Lower[0];
			double testPrice = GetFcBollingerTestPrice(direction);

			if (direction > 0)
			{
				double pct = FcLongBelowMidpointPercent / 100.0;
				double threshold = middle - ((middle - lower) * pct);

				return testPrice <= threshold;
			}
			else
			{
				double pct = FcShortAboveMidpointPercent / 100.0;
				double threshold = middle + ((upper - middle) * pct);

				return testPrice >= threshold;
			}
		}

		private bool IsLongContinuation(GapInfo gap)
		{
			if (RequireSignalCandleDirection && Close[0] <= Open[0])
				return false;

			if (!PassesBollingerMidpointContinuationFilter(1))
				return false;

			if (ContinuationConfirmationMode == GodTrades21ContinuationConfirmationMode.TouchOnly)
				return true;

			if (ContinuationConfirmationMode == GodTrades21ContinuationConfirmationMode.RequireCloseBeyondLine)
				return Close[0] >= gap.LinePrice;

			return Close[0] >= gap.ZoneHigh;
		}

		private bool IsShortContinuation(GapInfo gap)
		{
			if (RequireSignalCandleDirection && Close[0] >= Open[0])
				return false;

			if (!PassesBollingerMidpointContinuationFilter(-1))
				return false;

			if (ContinuationConfirmationMode == GodTrades21ContinuationConfirmationMode.TouchOnly)
				return true;

			if (ContinuationConfirmationMode == GodTrades21ContinuationConfirmationMode.RequireCloseBeyondLine)
				return Close[0] <= gap.LinePrice;

			return Close[0] <= gap.ZoneLow;
		}

		private void SetLongContinuationSignal()
		{
			ContinuationLong[0] = 1;
			SignalDirection[0] = 1;
			SignalCode[0] = 2;
			MasterLongSignal[0] = 1;
			SetSuggestedPrices(1);

			if (ShowContinuationMarkers)
			{
				string tag = "GodTrades21_CONT_Long_" + CurrentBar + "_" + signalDrawCounter++;
				double markerPrice = Low[0] - SignalMarkerOffsetTicks * TickSize;

				Draw.TriangleUp(this, tag, false, 0, markerPrice, ContinuationLongBrush);

				if (ShowSignalLabels)
					Draw.Text(this, tag + "_Label", "FC", 0, Low[0] - SignalLabelOffsetTicks * TickSize, ContinuationLongBrush);
			}
		}

		private void SetShortContinuationSignal()
		{
			ContinuationShort[0] = -1;
			SignalDirection[0] = -1;
			SignalCode[0] = -2;
			MasterShortSignal[0] = -1;
			SetSuggestedPrices(-1);

			if (ShowContinuationMarkers)
			{
				string tag = "GodTrades21_CONT_Short_" + CurrentBar + "_" + signalDrawCounter++;
				double markerPrice = High[0] + SignalMarkerOffsetTicks * TickSize;

				Draw.TriangleDown(this, tag, false, 0, markerPrice, ContinuationShortBrush);

				if (ShowSignalLabels)
					Draw.Text(this, tag + "_Label", "FC", 0, High[0] + SignalLabelOffsetTicks * TickSize, ContinuationShortBrush);
			}
		}

		private void MarkBollingerGapIfNeeded(int direction)
		{
			if (!EnableBollingerGapSignals)
				return;

			if (!IsSignalTimeAllowed())
				return;

			if (bollinger == null || CurrentBar < BollingerPeriod + 1)
				return;

			double proximity = BollingerBandProximityTicks * TickSize;

			if (direction > 0)
			{
				bool nearLowerBand =
					Low[1] <= bollinger.Lower[1] + proximity &&
					Low[0] <= bollinger.Lower[0] + proximity;

				if (nearLowerBand)
				{
					BollingerGapLong[0] = 1;
					MasterLongSignal[0] = 1;
					SignalDirection[0] = 1;
					SignalCode[0] = 1;
					SetSuggestedPrices(1);

					if (ShowBollingerGapMarkers)
						DrawBollingerMarker(1);
				}
			}
			else
			{
				bool nearUpperBand =
					High[1] >= bollinger.Upper[1] - proximity &&
					High[0] >= bollinger.Upper[0] - proximity;

				if (nearUpperBand)
				{
					BollingerGapShort[0] = -1;
					MasterShortSignal[0] = -1;
					SignalDirection[0] = -1;
					SignalCode[0] = -1;
					SetSuggestedPrices(-1);

					if (ShowBollingerGapMarkers)
						DrawBollingerMarker(-1);
				}
			}
		}

		private void DrawBollingerMarker(int direction)
		{
			if (direction > 0)
			{
				string tag = "GodTrades21_BG_Long_" + CurrentBar + "_" + signalDrawCounter++;
				double markerPrice = Low[0] - SignalMarkerOffsetTicks * TickSize;

				Draw.ArrowUp(this, tag, false, 0, markerPrice, BollingerLongBrush);

				if (ShowSignalLabels)
					Draw.Text(this, tag + "_Label", "BG", 0, Low[0] - SignalLabelOffsetTicks * TickSize, BollingerLongBrush);
			}
			else
			{
				string tag = "GodTrades21_BG_Short_" + CurrentBar + "_" + signalDrawCounter++;
				double markerPrice = High[0] + SignalMarkerOffsetTicks * TickSize;

				Draw.ArrowDown(this, tag, false, 0, markerPrice, BollingerShortBrush);

				if (ShowSignalLabels)
					Draw.Text(this, tag + "_Label", "BG", 0, High[0] + SignalLabelOffsetTicks * TickSize, BollingerShortBrush);
			}
		}

		private void SetSuggestedPrices(int direction)
		{
			SuggestedEntryPrice[0] = Close[0];

			if (direction > 0)
			{
				SuggestedStopPrice[0] = Low[0] - SuggestedStopOffsetTicks * TickSize;

				if (TargetMode == GodTrades21TargetMode.OppositeBollingerBand && bollinger != null && CurrentBar >= BollingerPeriod)
					SuggestedTargetPrice[0] = bollinger.Upper[0];
				else if (TargetMode == GodTrades21TargetMode.FixedTicks)
					SuggestedTargetPrice[0] = Close[0] + FixedTargetTicks * TickSize;
				else
					SuggestedTargetPrice[0] = 0;
			}
			else
			{
				SuggestedStopPrice[0] = High[0] + SuggestedStopOffsetTicks * TickSize;

				if (TargetMode == GodTrades21TargetMode.OppositeBollingerBand && bollinger != null && CurrentBar >= BollingerPeriod)
					SuggestedTargetPrice[0] = bollinger.Lower[0];
				else if (TargetMode == GodTrades21TargetMode.FixedTicks)
					SuggestedTargetPrice[0] = Close[0] - FixedTargetTicks * TickSize;
				else
					SuggestedTargetPrice[0] = 0;
			}
		}

		private bool IsSignalTimeAllowed()
		{
			if (!UseSignalTimeFilter)
				return true;

			int t = ToTime(Time[0]);

			if (SignalStartTime <= SignalEndTime)
				return t >= SignalStartTime && t <= SignalEndTime;

			return t >= SignalStartTime || t <= SignalEndTime;
		}

		private void TrimOldestActiveGaps()
		{
			if (activeGaps == null)
				return;

			while (activeGaps.Count > MaximumActiveGapsToTrack)
			{
				GapInfo oldest = activeGaps[0];
				DrawGap(oldest, false, false);
				activeGaps.RemoveAt(0);
			}
		}

		private void UpdateContextPlots()
		{
			int activeCount = activeGaps == null ? 0 : activeGaps.Count;
			int pendingCount = pendingContinuations == null ? 0 : pendingContinuations.Count;

			ActiveGapCount[0] = activeCount;
			PendingContinuationCount[0] = pendingCount;

			if (activeGaps == null || activeGaps.Count == 0)
			{
				ValidActiveGapCount[0] = 0;
				SpiderwebCount[0] = 0;
				SpiderwebWarning[0] = 0;
				NearestGapPrice[0] = 0;
				RemoveDrawObject("GodTrades21_SpiderwebWarning");
				return;
			}

			double nearestDistance = double.MaxValue;
			double nearestPrice = 0;
			int validActiveCount = 0;
			int spiderwebCount = 0;

			foreach (GapInfo gap in activeGaps)
			{
				int age = CurrentBar - gap.CreationBar;
				bool valid = age >= MinimumBarsBeforeValid;

				if (valid)
					validActiveCount++;

				double distance = DistanceFromPriceToZone(Close[0], gap);
				double distanceTicks = distance / TickSize;

				if (distance < nearestDistance)
				{
					nearestDistance = distance;
					nearestPrice = gap.LinePrice;
				}

				if (valid && distanceTicks <= SpiderwebDistanceTicks)
					spiderwebCount++;
			}

			ValidActiveGapCount[0] = validActiveCount;
			SpiderwebCount[0] = spiderwebCount;
			NearestGapPrice[0] = nearestPrice;

			bool warning = EnableSpiderwebWarning && spiderwebCount >= SpiderwebLineCount;
			SpiderwebWarning[0] = warning ? 1 : 0;

			if (warning && ShowSpiderwebWarningText)
			{
				string msg = "SPIDERWEB WARNING\n" + spiderwebCount + " valid gaps within " + SpiderwebDistanceTicks + " ticks";

				Draw.TextFixed(
					this,
					"GodTrades21_SpiderwebWarning",
					msg,
					TextPosition.TopRight,
					SpiderwebWarningBrush,
					new SimpleFont("Arial", SpiderwebTextFontSize),
					Brushes.Transparent,
					Brushes.Transparent,
					0);
			}
			else
			{
				RemoveDrawObject("GodTrades21_SpiderwebWarning");
			}
		}

		private double DistanceFromPriceToZone(double price, GapInfo gap)
		{
			if (price >= gap.ZoneLow && price <= gap.ZoneHigh)
				return 0;

			if (price < gap.ZoneLow)
				return gap.ZoneLow - price;

			return price - gap.ZoneHigh;
		}

		#region Inputs

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "Minimum Gap Size Ticks", GroupName = "01. Gap Detection", Order = 1)]
		public int MinimumGapSizeTicks { get; set; }

		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name = "Minimum Bars Before Valid", GroupName = "01. Gap Detection", Order = 2)]
		public int MinimumBarsBeforeValid { get; set; }

		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name = "Minimum Body Ticks", Description = "0 disables this filter.", GroupName = "01. Gap Detection", Order = 3)]
		public int MinimumBodyTicks { get; set; }

		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name = "Maximum Gap Bar Range Ticks", Description = "0 disables this filter.", GroupName = "01. Gap Detection", Order = 4)]
		public int MaximumGapBarRangeTicks { get; set; }

		[NinjaScriptProperty]
		[Range(1, 3000)]
		[Display(Name = "Maximum Active Gaps To Track", GroupName = "01. Gap Detection", Order = 5)]
		public int MaximumActiveGapsToTrack { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Early Touch Handling", GroupName = "02. Touch Logic", Order = 1)]
		public GodTrades21EarlyTouchHandling EarlyTouchHandling { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Valid Touch Behavior", GroupName = "02. Touch Logic", Order = 2)]
		public GodTrades21ValidTouchBehavior ValidTouchBehavior { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Enable Continuation Signals", GroupName = "03. Continuation Signals", Order = 1)]
		public bool EnableContinuationSignals { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Use Bollinger Midpoint Filter For Continuation", GroupName = "03. Continuation Signals", Order = 2)]
		public bool UseBollingerMidpointFilterForContinuation { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "FC Bollinger Location Source", GroupName = "03. Continuation Signals", Order = 3)]
		public GodTrades21FcBollingerLocationSource FcBollingerLocationSource { get; set; }

		[NinjaScriptProperty]
		[Range(0.0, 100.0)]
		[Display(Name = "FC Long Below Midpoint Percent", Description = "Long FC requires selected price source below midpoint by this percent of the distance from middle to lower band.", GroupName = "03. Continuation Signals", Order = 4)]
		public double FcLongBelowMidpointPercent { get; set; }

		[NinjaScriptProperty]
		[Range(0.0, 100.0)]
		[Display(Name = "FC Short Above Midpoint Percent", Description = "Short FC requires selected price source above midpoint by this percent of the distance from middle to upper band.", GroupName = "03. Continuation Signals", Order = 5)]
		public double FcShortAboveMidpointPercent { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Continuation Confirmation Mode", GroupName = "03. Continuation Signals", Order = 6)]
		public GodTrades21ContinuationConfirmationMode ContinuationConfirmationMode { get; set; }

		[NinjaScriptProperty]
		[Range(0, 20)]
		[Display(Name = "Confirmation Bars After Touch", GroupName = "03. Continuation Signals", Order = 7)]
		public int ConfirmationBarsAfterTouch { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Require Signal Candle Direction", Description = "Long signals require bullish candle; short signals require bearish candle.", GroupName = "03. Continuation Signals", Order = 8)]
		public bool RequireSignalCandleDirection { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Require Correct Continuation Approach", Description = "Bullish gaps must be touched from above; bearish gaps must be touched from below.", GroupName = "03. Continuation Signals", Order = 9)]
		public bool RequireCorrectContinuationApproach { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Enable Outside Bar Reversal Signals", GroupName = "04. Outside Bar Reversal", Order = 1)]
		public bool EnableOutsideBarReversalSignals { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Paint Outside Bar Reversal Bars", GroupName = "04. Outside Bar Reversal", Order = 2)]
		public bool PaintOutsideBarReversalBars { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show Outside Bar Reversal Markers", GroupName = "04. Outside Bar Reversal", Order = 3)]
		public bool ShowOutsideBarReversalMarkers { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Use Bollinger Midpoint Filter For Outside Bar Reversal", GroupName = "04. Outside Bar Reversal", Order = 4)]
		public bool UseBollingerMidpointFilterForOutsideBarReversal { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Allow OBR Bar Outside Bollinger Band", Description = "ON allows the OBR signal bar high/low to pierce beyond the applicable Bollinger Band. OFF requires the signal bar to remain inside the band while still meeting the touch-tolerance setting.", GroupName = "04. Outside Bar Reversal", Order = 5)]
		public bool AllowObrBarOutsideBollingerBand { get; set; }

		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name = "Bearish OBR Upper Band Touch Tolerance Ticks", GroupName = "04. Outside Bar Reversal", Order = 6)]
		public int BearishObrUpperBandTouchToleranceTicks { get; set; }

		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name = "Bullish OBR Lower Band Touch Tolerance Ticks", GroupName = "04. Outside Bar Reversal", Order = 7)]
		public int BullishObrLowerBandTouchToleranceTicks { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Use Signal Time Filter", GroupName = "03. Continuation Signals", Order = 10)]
		public bool UseSignalTimeFilter { get; set; }

		[NinjaScriptProperty]
		[Range(0, 235959)]
		[Display(Name = "Signal Start Time HHmmss", GroupName = "03. Continuation Signals", Order = 11)]
		public int SignalStartTime { get; set; }

		[NinjaScriptProperty]
		[Range(0, 235959)]
		[Display(Name = "Signal End Time HHmmss", GroupName = "03. Continuation Signals", Order = 12)]
		public int SignalEndTime { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Enable Bollinger Gap Signals", GroupName = "04. Bollinger Gap", Order = 1)]
		public bool EnableBollingerGapSignals { get; set; }

		[NinjaScriptProperty]
		[Range(2, int.MaxValue)]
		[Display(Name = "Bollinger Period", GroupName = "04. Bollinger Gap", Order = 2)]
		public int BollingerPeriod { get; set; }

		[NinjaScriptProperty]
		[Range(0.1, double.MaxValue)]
		[Display(Name = "Bollinger Std Dev", GroupName = "04. Bollinger Gap", Order = 3)]
		public double BollingerStdDev { get; set; }

		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name = "Bollinger Band Proximity Ticks", GroupName = "04. Bollinger Gap", Order = 4)]
		public int BollingerBandProximityTicks { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Enable Spiderweb Warning", GroupName = "05. Spiderweb", Order = 1)]
		public bool EnableSpiderwebWarning { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show Spiderweb Warning Text", GroupName = "05. Spiderweb", Order = 2)]
		public bool ShowSpiderwebWarningText { get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "Spiderweb Distance Ticks", GroupName = "05. Spiderweb", Order = 3)]
		public int SpiderwebDistanceTicks { get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "Spiderweb Line Count", GroupName = "05. Spiderweb", Order = 4)]
		public int SpiderwebLineCount { get; set; }

		[NinjaScriptProperty]
		[Range(6, 60)]
		[Display(Name = "Spiderweb Text Font Size", GroupName = "05. Spiderweb", Order = 5)]
		public int SpiderwebTextFontSize { get; set; }

		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name = "Suggested Stop Offset Ticks", GroupName = "06. Trade Prices", Order = 1)]
		public int SuggestedStopOffsetTicks { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Target Mode", GroupName = "06. Trade Prices", Order = 2)]
		public GodTrades21TargetMode TargetMode { get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "Fixed Target Ticks", GroupName = "06. Trade Prices", Order = 3)]
		public int FixedTargetTicks { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Line Price Mode", GroupName = "07. Visuals", Order = 1)]
		public GodTrades21LinePriceMode LinePriceMode { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show Gap Line", GroupName = "07. Visuals", Order = 2)]
		public bool ShowGapLine { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show Gap Zone", GroupName = "07. Visuals", Order = 3)]
		public bool ShowGapZone { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show Touch Marker", GroupName = "07. Visuals", Order = 4)]
		public bool ShowTouchMarker { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show Continuation Markers", GroupName = "07. Visuals", Order = 5)]
		public bool ShowContinuationMarkers { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show Bollinger Gap Markers", GroupName = "07. Visuals", Order = 6)]
		public bool ShowBollingerGapMarkers { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show Signal Labels", GroupName = "07. Visuals", Order = 7)]
		public bool ShowSignalLabels { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Use Touched Line Color", GroupName = "07. Visuals", Order = 8)]
		public bool UseTouchedLineColor { get; set; }

		[NinjaScriptProperty]
		[Range(1, 10)]
		[Display(Name = "Gap Line Width", GroupName = "07. Visuals", Order = 9)]
		public int GapLineWidth { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Gap Line Style", GroupName = "07. Visuals", Order = 10)]
		public DashStyleHelper GapLineStyle { get; set; }

		[NinjaScriptProperty]
		[Range(0, 100)]
		[Display(Name = "Zone Opacity", GroupName = "07. Visuals", Order = 11)]
		public int ZoneOpacity { get; set; }

		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name = "Signal Marker Offset Ticks", GroupName = "07. Visuals", Order = 12)]
		public int SignalMarkerOffsetTicks { get; set; }

		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name = "Signal Label Offset Ticks", GroupName = "07. Visuals", Order = 13)]
		public int SignalLabelOffsetTicks { get; set; }

		#endregion

		#region Brushes

		[XmlIgnore]
		[Display(Name = "Bullish Gap Line Brush", GroupName = "08. Brushes", Order = 1)]
		public Brush BullishGapLineBrush { get; set; }

		[Browsable(false)]
		public string BullishGapLineBrushSerializable
		{
			get { return Serialize.BrushToString(BullishGapLineBrush); }
			set { BullishGapLineBrush = Serialize.StringToBrush(value); }
		}

		[XmlIgnore]
		[Display(Name = "Bearish Gap Line Brush", GroupName = "08. Brushes", Order = 2)]
		public Brush BearishGapLineBrush { get; set; }

		[Browsable(false)]
		public string BearishGapLineBrushSerializable
		{
			get { return Serialize.BrushToString(BearishGapLineBrush); }
			set { BearishGapLineBrush = Serialize.StringToBrush(value); }
		}

		[XmlIgnore]
		[Display(Name = "Touched Gap Line Brush", GroupName = "08. Brushes", Order = 3)]
		public Brush TouchedGapLineBrush { get; set; }

		[Browsable(false)]
		public string TouchedGapLineBrushSerializable
		{
			get { return Serialize.BrushToString(TouchedGapLineBrush); }
			set { TouchedGapLineBrush = Serialize.StringToBrush(value); }
		}

		[XmlIgnore]
		[Display(Name = "Invalid Gap Color", GroupName = "08. Brushes", Order = 4)]
		public Brush InvalidGapBrush { get; set; }

		[Browsable(false)]
		public string InvalidGapBrushSerializable
		{
			get { return Serialize.BrushToString(InvalidGapBrush); }
			set { InvalidGapBrush = Serialize.StringToBrush(value); }
		}

		[XmlIgnore]
		[Display(Name = "Bullish Zone Brush", GroupName = "08. Brushes", Order = 5)]
		public Brush BullishZoneBrush { get; set; }

		[Browsable(false)]
		public string BullishZoneBrushSerializable
		{
			get { return Serialize.BrushToString(BullishZoneBrush); }
			set { BullishZoneBrush = Serialize.StringToBrush(value); }
		}

		[XmlIgnore]
		[Display(Name = "Bearish Zone Brush", GroupName = "08. Brushes", Order = 6)]
		public Brush BearishZoneBrush { get; set; }

		[Browsable(false)]
		public string BearishZoneBrushSerializable
		{
			get { return Serialize.BrushToString(BearishZoneBrush); }
			set { BearishZoneBrush = Serialize.StringToBrush(value); }
		}

		[XmlIgnore]
		[Display(Name = "Touch Marker Brush", GroupName = "08. Brushes", Order = 7)]
		public Brush TouchMarkerBrush { get; set; }

		[Browsable(false)]
		public string TouchMarkerBrushSerializable
		{
			get { return Serialize.BrushToString(TouchMarkerBrush); }
			set { TouchMarkerBrush = Serialize.StringToBrush(value); }
		}

		[XmlIgnore]
		[Display(Name = "Bollinger Long Brush", GroupName = "08. Brushes", Order = 8)]
		public Brush BollingerLongBrush { get; set; }

		[Browsable(false)]
		public string BollingerLongBrushSerializable
		{
			get { return Serialize.BrushToString(BollingerLongBrush); }
			set { BollingerLongBrush = Serialize.StringToBrush(value); }
		}

		[XmlIgnore]
		[Display(Name = "Bollinger Short Brush", GroupName = "08. Brushes", Order = 9)]
		public Brush BollingerShortBrush { get; set; }

		[Browsable(false)]
		public string BollingerShortBrushSerializable
		{
			get { return Serialize.BrushToString(BollingerShortBrush); }
			set { BollingerShortBrush = Serialize.StringToBrush(value); }
		}

		[XmlIgnore]
		[Display(Name = "Continuation Long Brush", GroupName = "08. Brushes", Order = 10)]
		public Brush ContinuationLongBrush { get; set; }

		[Browsable(false)]
		public string ContinuationLongBrushSerializable
		{
			get { return Serialize.BrushToString(ContinuationLongBrush); }
			set { ContinuationLongBrush = Serialize.StringToBrush(value); }
		}

		[XmlIgnore]
		[Display(Name = "Continuation Short Brush", GroupName = "08. Brushes", Order = 11)]
		public Brush ContinuationShortBrush { get; set; }

		[Browsable(false)]
		public string ContinuationShortBrushSerializable
		{
			get { return Serialize.BrushToString(ContinuationShortBrush); }
			set { ContinuationShortBrush = Serialize.StringToBrush(value); }
		}

		[XmlIgnore]
		[Display(Name = "Outside Bar Bullish Paint Brush", GroupName = "08. Brushes", Order = 12)]
		public Brush OutsideBarBullishPaintBrush { get; set; }

		[Browsable(false)]
		public string OutsideBarBullishPaintBrushSerializable
		{
			get { return Serialize.BrushToString(OutsideBarBullishPaintBrush); }
			set { OutsideBarBullishPaintBrush = Serialize.StringToBrush(value); }
		}

		[XmlIgnore]
		[Display(Name = "Outside Bar Bearish Paint Brush", GroupName = "08. Brushes", Order = 13)]
		public Brush OutsideBarBearishPaintBrush { get; set; }

		[Browsable(false)]
		public string OutsideBarBearishPaintBrushSerializable
		{
			get { return Serialize.BrushToString(OutsideBarBearishPaintBrush); }
			set { OutsideBarBearishPaintBrush = Serialize.StringToBrush(value); }
		}

		[XmlIgnore]
		[Display(Name = "Outside Bar Bullish Marker Brush", GroupName = "08. Brushes", Order = 14)]
		public Brush OutsideBarBullishMarkerBrush { get; set; }

		[Browsable(false)]
		public string OutsideBarBullishMarkerBrushSerializable
		{
			get { return Serialize.BrushToString(OutsideBarBullishMarkerBrush); }
			set { OutsideBarBullishMarkerBrush = Serialize.StringToBrush(value); }
		}

		[XmlIgnore]
		[Display(Name = "Outside Bar Bearish Marker Brush", GroupName = "08. Brushes", Order = 15)]
		public Brush OutsideBarBearishMarkerBrush { get; set; }

		[Browsable(false)]
		public string OutsideBarBearishMarkerBrushSerializable
		{
			get { return Serialize.BrushToString(OutsideBarBearishMarkerBrush); }
			set { OutsideBarBearishMarkerBrush = Serialize.StringToBrush(value); }
		}

		[XmlIgnore]
		[Display(Name = "Spiderweb Warning Brush", GroupName = "08. Brushes", Order = 16)]
		public Brush SpiderwebWarningBrush { get; set; }

		[Browsable(false)]
		public string SpiderwebWarningBrushSerializable
		{
			get { return Serialize.BrushToString(SpiderwebWarningBrush); }
			set { SpiderwebWarningBrush = Serialize.StringToBrush(value); }
		}

		#endregion

		#region Data Box Outputs

		[Browsable(false)] [XmlIgnore] public Series<double> BullishGapFormed			{ get { return Values[0]; } }
		[Browsable(false)] [XmlIgnore] public Series<double> BearishGapFormed			{ get { return Values[1]; } }
		[Browsable(false)] [XmlIgnore] public Series<double> BullishGapTouched			{ get { return Values[2]; } }
		[Browsable(false)] [XmlIgnore] public Series<double> BearishGapTouched			{ get { return Values[3]; } }
		[Browsable(false)] [XmlIgnore] public Series<double> InvalidGapTouched			{ get { return Values[4]; } }

		[Browsable(false)] [XmlIgnore] public Series<double> ContinuationLong			{ get { return Values[5]; } }
		[Browsable(false)] [XmlIgnore] public Series<double> ContinuationShort			{ get { return Values[6]; } }

		[Browsable(false)] [XmlIgnore] public Series<double> BollingerGapLong			{ get { return Values[7]; } }
		[Browsable(false)] [XmlIgnore] public Series<double> BollingerGapShort			{ get { return Values[8]; } }

		[Browsable(false)] [XmlIgnore] public Series<double> ActiveGapCount				{ get { return Values[9]; } }
		[Browsable(false)] [XmlIgnore] public Series<double> ValidActiveGapCount		{ get { return Values[10]; } }
		[Browsable(false)] [XmlIgnore] public Series<double> PendingContinuationCount	{ get { return Values[11]; } }
		[Browsable(false)] [XmlIgnore] public Series<double> SpiderwebCount				{ get { return Values[12]; } }
		[Browsable(false)] [XmlIgnore] public Series<double> SpiderwebWarning			{ get { return Values[13]; } }
		[Browsable(false)] [XmlIgnore] public Series<double> NearestGapPrice			{ get { return Values[14]; } }

		[Browsable(false)] [XmlIgnore] public Series<double> SuggestedEntryPrice		{ get { return Values[15]; } }
		[Browsable(false)] [XmlIgnore] public Series<double> SuggestedStopPrice			{ get { return Values[16]; } }
		[Browsable(false)] [XmlIgnore] public Series<double> SuggestedTargetPrice		{ get { return Values[17]; } }

		[Browsable(false)] [XmlIgnore] public Series<double> SignalDirection			{ get { return Values[18]; } }
		[Browsable(false)] [XmlIgnore] public Series<double> SignalCode					{ get { return Values[19]; } }
		[Browsable(false)] [XmlIgnore] public Series<double> MasterLongSignal			{ get { return Values[20]; } }
		[Browsable(false)] [XmlIgnore] public Series<double> MasterShortSignal			{ get { return Values[21]; } }
		[Browsable(false)] [XmlIgnore] public Series<double> OutsideBarReversalSignal	{ get { return Values[22]; } }

		#endregion
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private GodTrades21[] cacheGodTrades21;
		public GodTrades21 GodTrades21(int minimumGapSizeTicks, int minimumBarsBeforeValid, int minimumBodyTicks, int maximumGapBarRangeTicks, int maximumActiveGapsToTrack, GodTrades21EarlyTouchHandling earlyTouchHandling, GodTrades21ValidTouchBehavior validTouchBehavior, bool enableContinuationSignals, bool useBollingerMidpointFilterForContinuation, GodTrades21FcBollingerLocationSource fcBollingerLocationSource, double fcLongBelowMidpointPercent, double fcShortAboveMidpointPercent, GodTrades21ContinuationConfirmationMode continuationConfirmationMode, int confirmationBarsAfterTouch, bool requireSignalCandleDirection, bool requireCorrectContinuationApproach, bool enableOutsideBarReversalSignals, bool paintOutsideBarReversalBars, bool showOutsideBarReversalMarkers, bool useBollingerMidpointFilterForOutsideBarReversal, bool allowObrBarOutsideBollingerBand, int bearishObrUpperBandTouchToleranceTicks, int bullishObrLowerBandTouchToleranceTicks, bool useSignalTimeFilter, int signalStartTime, int signalEndTime, bool enableBollingerGapSignals, int bollingerPeriod, double bollingerStdDev, int bollingerBandProximityTicks, bool enableSpiderwebWarning, bool showSpiderwebWarningText, int spiderwebDistanceTicks, int spiderwebLineCount, int spiderwebTextFontSize, int suggestedStopOffsetTicks, GodTrades21TargetMode targetMode, int fixedTargetTicks, GodTrades21LinePriceMode linePriceMode, bool showGapLine, bool showGapZone, bool showTouchMarker, bool showContinuationMarkers, bool showBollingerGapMarkers, bool showSignalLabels, bool useTouchedLineColor, int gapLineWidth, DashStyleHelper gapLineStyle, int zoneOpacity, int signalMarkerOffsetTicks, int signalLabelOffsetTicks)
		{
			return GodTrades21(Input, minimumGapSizeTicks, minimumBarsBeforeValid, minimumBodyTicks, maximumGapBarRangeTicks, maximumActiveGapsToTrack, earlyTouchHandling, validTouchBehavior, enableContinuationSignals, useBollingerMidpointFilterForContinuation, fcBollingerLocationSource, fcLongBelowMidpointPercent, fcShortAboveMidpointPercent, continuationConfirmationMode, confirmationBarsAfterTouch, requireSignalCandleDirection, requireCorrectContinuationApproach, enableOutsideBarReversalSignals, paintOutsideBarReversalBars, showOutsideBarReversalMarkers, useBollingerMidpointFilterForOutsideBarReversal, allowObrBarOutsideBollingerBand, bearishObrUpperBandTouchToleranceTicks, bullishObrLowerBandTouchToleranceTicks, useSignalTimeFilter, signalStartTime, signalEndTime, enableBollingerGapSignals, bollingerPeriod, bollingerStdDev, bollingerBandProximityTicks, enableSpiderwebWarning, showSpiderwebWarningText, spiderwebDistanceTicks, spiderwebLineCount, spiderwebTextFontSize, suggestedStopOffsetTicks, targetMode, fixedTargetTicks, linePriceMode, showGapLine, showGapZone, showTouchMarker, showContinuationMarkers, showBollingerGapMarkers, showSignalLabels, useTouchedLineColor, gapLineWidth, gapLineStyle, zoneOpacity, signalMarkerOffsetTicks, signalLabelOffsetTicks);
		}

		public GodTrades21 GodTrades21(ISeries<double> input, int minimumGapSizeTicks, int minimumBarsBeforeValid, int minimumBodyTicks, int maximumGapBarRangeTicks, int maximumActiveGapsToTrack, GodTrades21EarlyTouchHandling earlyTouchHandling, GodTrades21ValidTouchBehavior validTouchBehavior, bool enableContinuationSignals, bool useBollingerMidpointFilterForContinuation, GodTrades21FcBollingerLocationSource fcBollingerLocationSource, double fcLongBelowMidpointPercent, double fcShortAboveMidpointPercent, GodTrades21ContinuationConfirmationMode continuationConfirmationMode, int confirmationBarsAfterTouch, bool requireSignalCandleDirection, bool requireCorrectContinuationApproach, bool enableOutsideBarReversalSignals, bool paintOutsideBarReversalBars, bool showOutsideBarReversalMarkers, bool useBollingerMidpointFilterForOutsideBarReversal, bool allowObrBarOutsideBollingerBand, int bearishObrUpperBandTouchToleranceTicks, int bullishObrLowerBandTouchToleranceTicks, bool useSignalTimeFilter, int signalStartTime, int signalEndTime, bool enableBollingerGapSignals, int bollingerPeriod, double bollingerStdDev, int bollingerBandProximityTicks, bool enableSpiderwebWarning, bool showSpiderwebWarningText, int spiderwebDistanceTicks, int spiderwebLineCount, int spiderwebTextFontSize, int suggestedStopOffsetTicks, GodTrades21TargetMode targetMode, int fixedTargetTicks, GodTrades21LinePriceMode linePriceMode, bool showGapLine, bool showGapZone, bool showTouchMarker, bool showContinuationMarkers, bool showBollingerGapMarkers, bool showSignalLabels, bool useTouchedLineColor, int gapLineWidth, DashStyleHelper gapLineStyle, int zoneOpacity, int signalMarkerOffsetTicks, int signalLabelOffsetTicks)
		{
			if (cacheGodTrades21 != null)
				for (int idx = 0; idx < cacheGodTrades21.Length; idx++)
					if (cacheGodTrades21[idx] != null && cacheGodTrades21[idx].MinimumGapSizeTicks == minimumGapSizeTicks && cacheGodTrades21[idx].MinimumBarsBeforeValid == minimumBarsBeforeValid && cacheGodTrades21[idx].MinimumBodyTicks == minimumBodyTicks && cacheGodTrades21[idx].MaximumGapBarRangeTicks == maximumGapBarRangeTicks && cacheGodTrades21[idx].MaximumActiveGapsToTrack == maximumActiveGapsToTrack && cacheGodTrades21[idx].EarlyTouchHandling == earlyTouchHandling && cacheGodTrades21[idx].ValidTouchBehavior == validTouchBehavior && cacheGodTrades21[idx].EnableContinuationSignals == enableContinuationSignals && cacheGodTrades21[idx].UseBollingerMidpointFilterForContinuation == useBollingerMidpointFilterForContinuation && cacheGodTrades21[idx].FcBollingerLocationSource == fcBollingerLocationSource && cacheGodTrades21[idx].FcLongBelowMidpointPercent == fcLongBelowMidpointPercent && cacheGodTrades21[idx].FcShortAboveMidpointPercent == fcShortAboveMidpointPercent && cacheGodTrades21[idx].ContinuationConfirmationMode == continuationConfirmationMode && cacheGodTrades21[idx].ConfirmationBarsAfterTouch == confirmationBarsAfterTouch && cacheGodTrades21[idx].RequireSignalCandleDirection == requireSignalCandleDirection && cacheGodTrades21[idx].RequireCorrectContinuationApproach == requireCorrectContinuationApproach && cacheGodTrades21[idx].EnableOutsideBarReversalSignals == enableOutsideBarReversalSignals && cacheGodTrades21[idx].PaintOutsideBarReversalBars == paintOutsideBarReversalBars && cacheGodTrades21[idx].ShowOutsideBarReversalMarkers == showOutsideBarReversalMarkers && cacheGodTrades21[idx].UseBollingerMidpointFilterForOutsideBarReversal == useBollingerMidpointFilterForOutsideBarReversal && cacheGodTrades21[idx].AllowObrBarOutsideBollingerBand == allowObrBarOutsideBollingerBand && cacheGodTrades21[idx].BearishObrUpperBandTouchToleranceTicks == bearishObrUpperBandTouchToleranceTicks && cacheGodTrades21[idx].BullishObrLowerBandTouchToleranceTicks == bullishObrLowerBandTouchToleranceTicks && cacheGodTrades21[idx].UseSignalTimeFilter == useSignalTimeFilter && cacheGodTrades21[idx].SignalStartTime == signalStartTime && cacheGodTrades21[idx].SignalEndTime == signalEndTime && cacheGodTrades21[idx].EnableBollingerGapSignals == enableBollingerGapSignals && cacheGodTrades21[idx].BollingerPeriod == bollingerPeriod && cacheGodTrades21[idx].BollingerStdDev == bollingerStdDev && cacheGodTrades21[idx].BollingerBandProximityTicks == bollingerBandProximityTicks && cacheGodTrades21[idx].EnableSpiderwebWarning == enableSpiderwebWarning && cacheGodTrades21[idx].ShowSpiderwebWarningText == showSpiderwebWarningText && cacheGodTrades21[idx].SpiderwebDistanceTicks == spiderwebDistanceTicks && cacheGodTrades21[idx].SpiderwebLineCount == spiderwebLineCount && cacheGodTrades21[idx].SpiderwebTextFontSize == spiderwebTextFontSize && cacheGodTrades21[idx].SuggestedStopOffsetTicks == suggestedStopOffsetTicks && cacheGodTrades21[idx].TargetMode == targetMode && cacheGodTrades21[idx].FixedTargetTicks == fixedTargetTicks && cacheGodTrades21[idx].LinePriceMode == linePriceMode && cacheGodTrades21[idx].ShowGapLine == showGapLine && cacheGodTrades21[idx].ShowGapZone == showGapZone && cacheGodTrades21[idx].ShowTouchMarker == showTouchMarker && cacheGodTrades21[idx].ShowContinuationMarkers == showContinuationMarkers && cacheGodTrades21[idx].ShowBollingerGapMarkers == showBollingerGapMarkers && cacheGodTrades21[idx].ShowSignalLabels == showSignalLabels && cacheGodTrades21[idx].UseTouchedLineColor == useTouchedLineColor && cacheGodTrades21[idx].GapLineWidth == gapLineWidth && cacheGodTrades21[idx].GapLineStyle == gapLineStyle && cacheGodTrades21[idx].ZoneOpacity == zoneOpacity && cacheGodTrades21[idx].SignalMarkerOffsetTicks == signalMarkerOffsetTicks && cacheGodTrades21[idx].SignalLabelOffsetTicks == signalLabelOffsetTicks && cacheGodTrades21[idx].EqualsInput(input))
						return cacheGodTrades21[idx];
			return CacheIndicator<GodTrades21>(new GodTrades21(){ MinimumGapSizeTicks = minimumGapSizeTicks, MinimumBarsBeforeValid = minimumBarsBeforeValid, MinimumBodyTicks = minimumBodyTicks, MaximumGapBarRangeTicks = maximumGapBarRangeTicks, MaximumActiveGapsToTrack = maximumActiveGapsToTrack, EarlyTouchHandling = earlyTouchHandling, ValidTouchBehavior = validTouchBehavior, EnableContinuationSignals = enableContinuationSignals, UseBollingerMidpointFilterForContinuation = useBollingerMidpointFilterForContinuation, FcBollingerLocationSource = fcBollingerLocationSource, FcLongBelowMidpointPercent = fcLongBelowMidpointPercent, FcShortAboveMidpointPercent = fcShortAboveMidpointPercent, ContinuationConfirmationMode = continuationConfirmationMode, ConfirmationBarsAfterTouch = confirmationBarsAfterTouch, RequireSignalCandleDirection = requireSignalCandleDirection, RequireCorrectContinuationApproach = requireCorrectContinuationApproach, EnableOutsideBarReversalSignals = enableOutsideBarReversalSignals, PaintOutsideBarReversalBars = paintOutsideBarReversalBars, ShowOutsideBarReversalMarkers = showOutsideBarReversalMarkers, UseBollingerMidpointFilterForOutsideBarReversal = useBollingerMidpointFilterForOutsideBarReversal, AllowObrBarOutsideBollingerBand = allowObrBarOutsideBollingerBand, BearishObrUpperBandTouchToleranceTicks = bearishObrUpperBandTouchToleranceTicks, BullishObrLowerBandTouchToleranceTicks = bullishObrLowerBandTouchToleranceTicks, UseSignalTimeFilter = useSignalTimeFilter, SignalStartTime = signalStartTime, SignalEndTime = signalEndTime, EnableBollingerGapSignals = enableBollingerGapSignals, BollingerPeriod = bollingerPeriod, BollingerStdDev = bollingerStdDev, BollingerBandProximityTicks = bollingerBandProximityTicks, EnableSpiderwebWarning = enableSpiderwebWarning, ShowSpiderwebWarningText = showSpiderwebWarningText, SpiderwebDistanceTicks = spiderwebDistanceTicks, SpiderwebLineCount = spiderwebLineCount, SpiderwebTextFontSize = spiderwebTextFontSize, SuggestedStopOffsetTicks = suggestedStopOffsetTicks, TargetMode = targetMode, FixedTargetTicks = fixedTargetTicks, LinePriceMode = linePriceMode, ShowGapLine = showGapLine, ShowGapZone = showGapZone, ShowTouchMarker = showTouchMarker, ShowContinuationMarkers = showContinuationMarkers, ShowBollingerGapMarkers = showBollingerGapMarkers, ShowSignalLabels = showSignalLabels, UseTouchedLineColor = useTouchedLineColor, GapLineWidth = gapLineWidth, GapLineStyle = gapLineStyle, ZoneOpacity = zoneOpacity, SignalMarkerOffsetTicks = signalMarkerOffsetTicks, SignalLabelOffsetTicks = signalLabelOffsetTicks }, input, ref cacheGodTrades21);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.GodTrades21 GodTrades21(int minimumGapSizeTicks, int minimumBarsBeforeValid, int minimumBodyTicks, int maximumGapBarRangeTicks, int maximumActiveGapsToTrack, GodTrades21EarlyTouchHandling earlyTouchHandling, GodTrades21ValidTouchBehavior validTouchBehavior, bool enableContinuationSignals, bool useBollingerMidpointFilterForContinuation, GodTrades21FcBollingerLocationSource fcBollingerLocationSource, double fcLongBelowMidpointPercent, double fcShortAboveMidpointPercent, GodTrades21ContinuationConfirmationMode continuationConfirmationMode, int confirmationBarsAfterTouch, bool requireSignalCandleDirection, bool requireCorrectContinuationApproach, bool enableOutsideBarReversalSignals, bool paintOutsideBarReversalBars, bool showOutsideBarReversalMarkers, bool useBollingerMidpointFilterForOutsideBarReversal, bool allowObrBarOutsideBollingerBand, int bearishObrUpperBandTouchToleranceTicks, int bullishObrLowerBandTouchToleranceTicks, bool useSignalTimeFilter, int signalStartTime, int signalEndTime, bool enableBollingerGapSignals, int bollingerPeriod, double bollingerStdDev, int bollingerBandProximityTicks, bool enableSpiderwebWarning, bool showSpiderwebWarningText, int spiderwebDistanceTicks, int spiderwebLineCount, int spiderwebTextFontSize, int suggestedStopOffsetTicks, GodTrades21TargetMode targetMode, int fixedTargetTicks, GodTrades21LinePriceMode linePriceMode, bool showGapLine, bool showGapZone, bool showTouchMarker, bool showContinuationMarkers, bool showBollingerGapMarkers, bool showSignalLabels, bool useTouchedLineColor, int gapLineWidth, DashStyleHelper gapLineStyle, int zoneOpacity, int signalMarkerOffsetTicks, int signalLabelOffsetTicks)
		{
			return indicator.GodTrades21(Input, minimumGapSizeTicks, minimumBarsBeforeValid, minimumBodyTicks, maximumGapBarRangeTicks, maximumActiveGapsToTrack, earlyTouchHandling, validTouchBehavior, enableContinuationSignals, useBollingerMidpointFilterForContinuation, fcBollingerLocationSource, fcLongBelowMidpointPercent, fcShortAboveMidpointPercent, continuationConfirmationMode, confirmationBarsAfterTouch, requireSignalCandleDirection, requireCorrectContinuationApproach, enableOutsideBarReversalSignals, paintOutsideBarReversalBars, showOutsideBarReversalMarkers, useBollingerMidpointFilterForOutsideBarReversal, allowObrBarOutsideBollingerBand, bearishObrUpperBandTouchToleranceTicks, bullishObrLowerBandTouchToleranceTicks, useSignalTimeFilter, signalStartTime, signalEndTime, enableBollingerGapSignals, bollingerPeriod, bollingerStdDev, bollingerBandProximityTicks, enableSpiderwebWarning, showSpiderwebWarningText, spiderwebDistanceTicks, spiderwebLineCount, spiderwebTextFontSize, suggestedStopOffsetTicks, targetMode, fixedTargetTicks, linePriceMode, showGapLine, showGapZone, showTouchMarker, showContinuationMarkers, showBollingerGapMarkers, showSignalLabels, useTouchedLineColor, gapLineWidth, gapLineStyle, zoneOpacity, signalMarkerOffsetTicks, signalLabelOffsetTicks);
		}

		public Indicators.GodTrades21 GodTrades21(ISeries<double> input , int minimumGapSizeTicks, int minimumBarsBeforeValid, int minimumBodyTicks, int maximumGapBarRangeTicks, int maximumActiveGapsToTrack, GodTrades21EarlyTouchHandling earlyTouchHandling, GodTrades21ValidTouchBehavior validTouchBehavior, bool enableContinuationSignals, bool useBollingerMidpointFilterForContinuation, GodTrades21FcBollingerLocationSource fcBollingerLocationSource, double fcLongBelowMidpointPercent, double fcShortAboveMidpointPercent, GodTrades21ContinuationConfirmationMode continuationConfirmationMode, int confirmationBarsAfterTouch, bool requireSignalCandleDirection, bool requireCorrectContinuationApproach, bool enableOutsideBarReversalSignals, bool paintOutsideBarReversalBars, bool showOutsideBarReversalMarkers, bool useBollingerMidpointFilterForOutsideBarReversal, bool allowObrBarOutsideBollingerBand, int bearishObrUpperBandTouchToleranceTicks, int bullishObrLowerBandTouchToleranceTicks, bool useSignalTimeFilter, int signalStartTime, int signalEndTime, bool enableBollingerGapSignals, int bollingerPeriod, double bollingerStdDev, int bollingerBandProximityTicks, bool enableSpiderwebWarning, bool showSpiderwebWarningText, int spiderwebDistanceTicks, int spiderwebLineCount, int spiderwebTextFontSize, int suggestedStopOffsetTicks, GodTrades21TargetMode targetMode, int fixedTargetTicks, GodTrades21LinePriceMode linePriceMode, bool showGapLine, bool showGapZone, bool showTouchMarker, bool showContinuationMarkers, bool showBollingerGapMarkers, bool showSignalLabels, bool useTouchedLineColor, int gapLineWidth, DashStyleHelper gapLineStyle, int zoneOpacity, int signalMarkerOffsetTicks, int signalLabelOffsetTicks)
		{
			return indicator.GodTrades21(input, minimumGapSizeTicks, minimumBarsBeforeValid, minimumBodyTicks, maximumGapBarRangeTicks, maximumActiveGapsToTrack, earlyTouchHandling, validTouchBehavior, enableContinuationSignals, useBollingerMidpointFilterForContinuation, fcBollingerLocationSource, fcLongBelowMidpointPercent, fcShortAboveMidpointPercent, continuationConfirmationMode, confirmationBarsAfterTouch, requireSignalCandleDirection, requireCorrectContinuationApproach, enableOutsideBarReversalSignals, paintOutsideBarReversalBars, showOutsideBarReversalMarkers, useBollingerMidpointFilterForOutsideBarReversal, allowObrBarOutsideBollingerBand, bearishObrUpperBandTouchToleranceTicks, bullishObrLowerBandTouchToleranceTicks, useSignalTimeFilter, signalStartTime, signalEndTime, enableBollingerGapSignals, bollingerPeriod, bollingerStdDev, bollingerBandProximityTicks, enableSpiderwebWarning, showSpiderwebWarningText, spiderwebDistanceTicks, spiderwebLineCount, spiderwebTextFontSize, suggestedStopOffsetTicks, targetMode, fixedTargetTicks, linePriceMode, showGapLine, showGapZone, showTouchMarker, showContinuationMarkers, showBollingerGapMarkers, showSignalLabels, useTouchedLineColor, gapLineWidth, gapLineStyle, zoneOpacity, signalMarkerOffsetTicks, signalLabelOffsetTicks);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.GodTrades21 GodTrades21(int minimumGapSizeTicks, int minimumBarsBeforeValid, int minimumBodyTicks, int maximumGapBarRangeTicks, int maximumActiveGapsToTrack, GodTrades21EarlyTouchHandling earlyTouchHandling, GodTrades21ValidTouchBehavior validTouchBehavior, bool enableContinuationSignals, bool useBollingerMidpointFilterForContinuation, GodTrades21FcBollingerLocationSource fcBollingerLocationSource, double fcLongBelowMidpointPercent, double fcShortAboveMidpointPercent, GodTrades21ContinuationConfirmationMode continuationConfirmationMode, int confirmationBarsAfterTouch, bool requireSignalCandleDirection, bool requireCorrectContinuationApproach, bool enableOutsideBarReversalSignals, bool paintOutsideBarReversalBars, bool showOutsideBarReversalMarkers, bool useBollingerMidpointFilterForOutsideBarReversal, bool allowObrBarOutsideBollingerBand, int bearishObrUpperBandTouchToleranceTicks, int bullishObrLowerBandTouchToleranceTicks, bool useSignalTimeFilter, int signalStartTime, int signalEndTime, bool enableBollingerGapSignals, int bollingerPeriod, double bollingerStdDev, int bollingerBandProximityTicks, bool enableSpiderwebWarning, bool showSpiderwebWarningText, int spiderwebDistanceTicks, int spiderwebLineCount, int spiderwebTextFontSize, int suggestedStopOffsetTicks, GodTrades21TargetMode targetMode, int fixedTargetTicks, GodTrades21LinePriceMode linePriceMode, bool showGapLine, bool showGapZone, bool showTouchMarker, bool showContinuationMarkers, bool showBollingerGapMarkers, bool showSignalLabels, bool useTouchedLineColor, int gapLineWidth, DashStyleHelper gapLineStyle, int zoneOpacity, int signalMarkerOffsetTicks, int signalLabelOffsetTicks)
		{
			return indicator.GodTrades21(Input, minimumGapSizeTicks, minimumBarsBeforeValid, minimumBodyTicks, maximumGapBarRangeTicks, maximumActiveGapsToTrack, earlyTouchHandling, validTouchBehavior, enableContinuationSignals, useBollingerMidpointFilterForContinuation, fcBollingerLocationSource, fcLongBelowMidpointPercent, fcShortAboveMidpointPercent, continuationConfirmationMode, confirmationBarsAfterTouch, requireSignalCandleDirection, requireCorrectContinuationApproach, enableOutsideBarReversalSignals, paintOutsideBarReversalBars, showOutsideBarReversalMarkers, useBollingerMidpointFilterForOutsideBarReversal, allowObrBarOutsideBollingerBand, bearishObrUpperBandTouchToleranceTicks, bullishObrLowerBandTouchToleranceTicks, useSignalTimeFilter, signalStartTime, signalEndTime, enableBollingerGapSignals, bollingerPeriod, bollingerStdDev, bollingerBandProximityTicks, enableSpiderwebWarning, showSpiderwebWarningText, spiderwebDistanceTicks, spiderwebLineCount, spiderwebTextFontSize, suggestedStopOffsetTicks, targetMode, fixedTargetTicks, linePriceMode, showGapLine, showGapZone, showTouchMarker, showContinuationMarkers, showBollingerGapMarkers, showSignalLabels, useTouchedLineColor, gapLineWidth, gapLineStyle, zoneOpacity, signalMarkerOffsetTicks, signalLabelOffsetTicks);
		}

		public Indicators.GodTrades21 GodTrades21(ISeries<double> input , int minimumGapSizeTicks, int minimumBarsBeforeValid, int minimumBodyTicks, int maximumGapBarRangeTicks, int maximumActiveGapsToTrack, GodTrades21EarlyTouchHandling earlyTouchHandling, GodTrades21ValidTouchBehavior validTouchBehavior, bool enableContinuationSignals, bool useBollingerMidpointFilterForContinuation, GodTrades21FcBollingerLocationSource fcBollingerLocationSource, double fcLongBelowMidpointPercent, double fcShortAboveMidpointPercent, GodTrades21ContinuationConfirmationMode continuationConfirmationMode, int confirmationBarsAfterTouch, bool requireSignalCandleDirection, bool requireCorrectContinuationApproach, bool enableOutsideBarReversalSignals, bool paintOutsideBarReversalBars, bool showOutsideBarReversalMarkers, bool useBollingerMidpointFilterForOutsideBarReversal, bool allowObrBarOutsideBollingerBand, int bearishObrUpperBandTouchToleranceTicks, int bullishObrLowerBandTouchToleranceTicks, bool useSignalTimeFilter, int signalStartTime, int signalEndTime, bool enableBollingerGapSignals, int bollingerPeriod, double bollingerStdDev, int bollingerBandProximityTicks, bool enableSpiderwebWarning, bool showSpiderwebWarningText, int spiderwebDistanceTicks, int spiderwebLineCount, int spiderwebTextFontSize, int suggestedStopOffsetTicks, GodTrades21TargetMode targetMode, int fixedTargetTicks, GodTrades21LinePriceMode linePriceMode, bool showGapLine, bool showGapZone, bool showTouchMarker, bool showContinuationMarkers, bool showBollingerGapMarkers, bool showSignalLabels, bool useTouchedLineColor, int gapLineWidth, DashStyleHelper gapLineStyle, int zoneOpacity, int signalMarkerOffsetTicks, int signalLabelOffsetTicks)
		{
			return indicator.GodTrades21(input, minimumGapSizeTicks, minimumBarsBeforeValid, minimumBodyTicks, maximumGapBarRangeTicks, maximumActiveGapsToTrack, earlyTouchHandling, validTouchBehavior, enableContinuationSignals, useBollingerMidpointFilterForContinuation, fcBollingerLocationSource, fcLongBelowMidpointPercent, fcShortAboveMidpointPercent, continuationConfirmationMode, confirmationBarsAfterTouch, requireSignalCandleDirection, requireCorrectContinuationApproach, enableOutsideBarReversalSignals, paintOutsideBarReversalBars, showOutsideBarReversalMarkers, useBollingerMidpointFilterForOutsideBarReversal, allowObrBarOutsideBollingerBand, bearishObrUpperBandTouchToleranceTicks, bullishObrLowerBandTouchToleranceTicks, useSignalTimeFilter, signalStartTime, signalEndTime, enableBollingerGapSignals, bollingerPeriod, bollingerStdDev, bollingerBandProximityTicks, enableSpiderwebWarning, showSpiderwebWarningText, spiderwebDistanceTicks, spiderwebLineCount, spiderwebTextFontSize, suggestedStopOffsetTicks, targetMode, fixedTargetTicks, linePriceMode, showGapLine, showGapZone, showTouchMarker, showContinuationMarkers, showBollingerGapMarkers, showSignalLabels, useTouchedLineColor, gapLineWidth, gapLineStyle, zoneOpacity, signalMarkerOffsetTicks, signalLabelOffsetTicks);
		}
	}
}

#endregion


