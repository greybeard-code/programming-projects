#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.SuperDom;
using NinjaTrader.Gui.Tools;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

// ============================================================
//  BSCExitSignal
//  Exit companion for BreakSignalCombined
//
//  HOW IT WORKS:
//  1. Reads BreakSignalCombined LongSignal / ShortSignal plots
//     to know when an entry occurred
//  2. After entry, tracks the trade with ATR trailing stop
//  3. Fires exit arrow ONLY when:
//     A. ATR trail is breached (primary)
//     B. Price crosses back through smoothed MA (secondary)
//     C. MinExitBars has passed AND opposing color bar appears
//  4. Resets and waits for next entry signal
// ============================================================

namespace NinjaTrader.NinjaScript.Indicators
{
    public class BSCExitSignal : Indicator
    {
        // Internal series
        private Series<double> trendMA;
        private Series<double> smoothedMA;

        // Reference to BreakSignalCombined
        private Indicators.BreakSignalCombined bsc;

        // State machine
        private int    tradeDirection = 0;   //  1=in long, -1=in short, 0=flat
        private double trailLevel     = 0;
        private double highestHigh    = 0;
        private double lowestLow      = double.MaxValue;
        private int    entryBar       = -999;

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description                 = "Exit signal companion for BreakSignalCombined. "
                                            + "Activates ONLY after a BreakSignalCombined entry fires. "
                                            + "Exits on ATR trail breach or MA cross.";
                Name                        = "BSCExitSignal";
                Calculate                   = Calculate.OnBarClose;
                IsOverlay                   = true;
                DisplayInDataBox            = true;
                DrawOnPricePanel            = true;
                PaintPriceMarkers           = true;
                ScaleJustification          = NinjaTrader.Gui.Chart.ScaleJustification.Right;
                IsSuspendedWhileInactive    = true;

                // ---- Match these to your BreakSignalCombined settings ----
                TZ_TrendMAType           = ThunderZillaMAType.SMA;
                TZ_TrendPeriod           = 100;
                TZ_TrendSmoothingEnabled = false;
                TZ_TrendSmoothingMethod  = ThunderZillaMAType.EMA;
                TZ_TrendSmoothingPeriod  = 10;
                TZ_StopOffsetMultiplier  = 60;
                TZ_SignalQtyPerFlat      = 2;
                TZ_SignalQtyPerTrend     = 999;
                TZ_BullishThreshold      = 3;
                TZ_BearishThreshold      = -3;
                PK_Period                = 20;
                PK_Factor                = 4;
                PK_MiddlePeriod          = 14;
                PK_SignalBreakSplitBars  = 20;
                PK_SignalPullbackFindPeriod = 10;

                // ---- Exit Settings ----
                ATR_Period               = 14;
                ATR_Multiplier           = 2.0;
                UseMAExit                = true;
                MinBarsInTrade           = 3;   // min bars before color flip exit allowed

                // ---- Visuals ----
                LongExitColor            = Brushes.Orange;
                ShortExitColor           = Brushes.Orange;
                ArrowOffset              = 3;
                ShowLabel                = true;
                LongExitLabel            = "Exit Long";
                ShortExitLabel           = "Exit Short";
                LabelFontSize            = 10;
                ShowTrailLine            = true;
            }
            else if (State == State.Configure)
            {
                AddPlot(new Stroke(Brushes.Transparent, 1), PlotStyle.Dot,  "LongExit");
                AddPlot(new Stroke(Brushes.Transparent, 1), PlotStyle.Dot,  "ShortExit");
                AddPlot(new Stroke(Brushes.Transparent, 2), PlotStyle.Line, "TrailStop");
            }
            else if (State == State.DataLoaded)
            {
                trendMA    = new Series<double>(this);
                smoothedMA = new Series<double>(this);

                // Load BreakSignalCombined with matching parameters
                bsc = BreakSignalCombined(
                    TZ_TrendMAType,
                    TZ_TrendPeriod,
                    TZ_TrendSmoothingEnabled,
                    TZ_TrendSmoothingMethod,
                    TZ_TrendSmoothingPeriod,
                    TZ_StopOffsetMultiplier,
                    TZ_SignalQtyPerFlat,
                    TZ_SignalQtyPerTrend,
                    TZ_BullishThreshold,
                    TZ_BearishThreshold,
                    PK_Period,
                    PK_Factor,
                    PK_MiddlePeriod,
                    PK_SignalBreakSplitBars,
                    PK_SignalPullbackFindPeriod,
                    3, true, "Break Buy", "Break Sell", 10
                );
            }
        }

        protected override void OnBarUpdate()
        {
            int warmup = TZ_TrendPeriod + TZ_TrendSmoothingPeriod + 5;
            if (CurrentBar < warmup)
                return;

            // ---- Smoothed MA ----
            trendMA[0]    = SMA(Close, TZ_TrendPeriod)[0];
            smoothedMA[0] = EMA(trendMA, TZ_TrendSmoothingPeriod)[0];

            // ---- Reset outputs ----
            Values[0][0] = 0;
            Values[1][0] = 0;

            // ============================================================
            //  STEP 1: Detect new entry from BreakSignalCombined
            //  Only activate tracking AFTER an entry fires
            // ============================================================
            if (tradeDirection == 0)
            {
                if (bsc.LongSignal[0] == 1)
                {
                    tradeDirection = 1;
                    entryBar       = CurrentBar;
                    highestHigh    = High[0];
                    trailLevel     = High[0] - (ATR(ATR_Period)[0] * ATR_Multiplier);
                    Print(Time[0] + " | BSCExit: LONG entry detected at bar " + CurrentBar);
                }
                else if (bsc.ShortSignal[0] == -1)
                {
                    tradeDirection = -1;
                    entryBar       = CurrentBar;
                    lowestLow      = Low[0];
                    trailLevel     = Low[0] + (ATR(ATR_Period)[0] * ATR_Multiplier);
                    Print(Time[0] + " | BSCExit: SHORT entry detected at bar " + CurrentBar);
                }
            }

            // ============================================================
            //  STEP 2: If in a trade, update trail and check exit
            // ============================================================
            double atr           = ATR(ATR_Period)[0];
            bool   longExit      = false;
            bool   shortExit     = false;
            string exitReason    = "";
            int    barsInTrade   = CurrentBar - entryBar;

            if (tradeDirection == 1)
            {
                // Update highest high and trail
                highestHigh = Math.Max(highestHigh, High[0]);
                trailLevel  = highestHigh - (atr * ATR_Multiplier);
                Values[2][0] = trailLevel;

                // Exit A: ATR trail breached
                if (Close[0] < trailLevel)
                {
                    longExit   = true;
                    exitReason = "Trail";
                }
                // Exit B: MA cross (price closes below smoothed MA)
                else if (UseMAExit && Close[0] < smoothedMA[0] && Close[1] >= smoothedMA[1])
                {
                    longExit   = true;
                    exitReason = "MA Cross";
                }
                // Exit C: Color flip after MinBarsInTrade
                else if (barsInTrade >= MinBarsInTrade && Close[0] < Open[0] && Close[1] > Open[1])
                {
                    longExit   = true;
                    exitReason = "Flip";
                }
            }
            else if (tradeDirection == -1)
            {
                // Update lowest low and trail
                lowestLow   = Math.Min(lowestLow, Low[0]);
                trailLevel  = lowestLow + (atr * ATR_Multiplier);
                Values[2][0] = trailLevel;

                // Exit A: ATR trail breached
                if (Close[0] > trailLevel)
                {
                    shortExit  = true;
                    exitReason = "Trail";
                }
                // Exit B: MA cross
                else if (UseMAExit && Close[0] > smoothedMA[0] && Close[1] <= smoothedMA[1])
                {
                    shortExit  = true;
                    exitReason = "MA Cross";
                }
                // Exit C: Color flip after MinBarsInTrade
                else if (barsInTrade >= MinBarsInTrade && Close[0] > Open[0] && Close[1] < Open[1])
                {
                    shortExit  = true;
                    exitReason = "Flip";
                }
            }
            else
            {
                Values[2][0] = smoothedMA[0];
            }

            // ============================================================
            //  STEP 3: Fire exit signal and reset state
            // ============================================================
            if (longExit)
            {
                Draw.ArrowDown(this, "LongExit_" + CurrentBar, false, 0,
                    High[0] + (ArrowOffset * TickSize), LongExitColor);

                if (ShowLabel)
                    Draw.Text(this, "LongExitTxt_" + CurrentBar, false,
                        LongExitLabel + " (" + exitReason + ")", 0,
                        High[0] + (ArrowOffset * TickSize * 4),
                        0, LongExitColor,
                        new SimpleFont("Arial", LabelFontSize),
                        TextAlignment.Center,
                        Brushes.Transparent, Brushes.Transparent, 0);

                Values[0][0]   = 1;
                tradeDirection = 0;
                highestHigh    = 0;
                Print(Time[0] + " | BSCExit: LONG EXIT fired — " + exitReason);
            }

            if (shortExit)
            {
                Draw.ArrowUp(this, "ShortExit_" + CurrentBar, false, 0,
                    Low[0] - (ArrowOffset * TickSize), ShortExitColor);

                if (ShowLabel)
                    Draw.Text(this, "ShortExitTxt_" + CurrentBar, false,
                        ShortExitLabel + " (" + exitReason + ")", 0,
                        Low[0] - (ArrowOffset * TickSize * 4),
                        0, ShortExitColor,
                        new SimpleFont("Arial", LabelFontSize),
                        TextAlignment.Center,
                        Brushes.Transparent, Brushes.Transparent, 0);

                Values[1][0]   = -1;
                tradeDirection = 0;
                lowestLow      = double.MaxValue;
                Print(Time[0] + " | BSCExit: SHORT EXIT fired — " + exitReason);
            }
        }

        #region Properties

        // ---- BSC Parameters (must match BreakSignalCombined) ----
        [NinjaScriptProperty]
        [Display(Name = "Trend: MA Type", GroupName = "1. BSC Parameters", Order = 1)]
        public ThunderZillaMAType TZ_TrendMAType { get; set; }

        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Trend: Period", GroupName = "1. BSC Parameters", Order = 2)]
        public int TZ_TrendPeriod { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Trend: Smoothing Enabled", GroupName = "1. BSC Parameters", Order = 3)]
        public bool TZ_TrendSmoothingEnabled { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Trend: Smoothing Method", GroupName = "1. BSC Parameters", Order = 4)]
        public ThunderZillaMAType TZ_TrendSmoothingMethod { get; set; }

        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Trend: Smoothing Period", GroupName = "1. BSC Parameters", Order = 5)]
        public int TZ_TrendSmoothingPeriod { get; set; }

        [NinjaScriptProperty]
        [Range(1, double.MaxValue)]
        [Display(Name = "Stop: Offset Multiplier", GroupName = "1. BSC Parameters", Order = 6)]
        public double TZ_StopOffsetMultiplier { get; set; }

        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Signal: Qty Per Flat", GroupName = "1. BSC Parameters", Order = 7)]
        public int TZ_SignalQtyPerFlat { get; set; }

        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Signal: Qty Per Trend", GroupName = "1. BSC Parameters", Order = 8)]
        public int TZ_SignalQtyPerTrend { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Bullish Threshold", GroupName = "1. BSC Parameters", Order = 9)]
        public double TZ_BullishThreshold { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Bearish Threshold", GroupName = "1. BSC Parameters", Order = 10)]
        public double TZ_BearishThreshold { get; set; }

        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "PK: Period", GroupName = "1. BSC Parameters", Order = 11)]
        public int PK_Period { get; set; }

        [NinjaScriptProperty]
        [Range(0.1, double.MaxValue)]
        [Display(Name = "PK: Factor", GroupName = "1. BSC Parameters", Order = 12)]
        public double PK_Factor { get; set; }

        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "PK: Middle Period", GroupName = "1. BSC Parameters", Order = 13)]
        public int PK_MiddlePeriod { get; set; }

        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "PK: Signal Break Split Bars", GroupName = "1. BSC Parameters", Order = 14)]
        public int PK_SignalBreakSplitBars { get; set; }

        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "PK: Pullback Finding Period", GroupName = "1. BSC Parameters", Order = 15)]
        public int PK_SignalPullbackFindPeriod { get; set; }

        // ---- Exit Settings ----
        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "ATR Period", GroupName = "2. Exit Settings", Order = 1)]
        public int ATR_Period { get; set; }

        [NinjaScriptProperty]
        [Range(0.1, double.MaxValue)]
        [Display(Name = "ATR Multiplier", GroupName = "2. Exit Settings", Order = 2,
            Description = "Trail distance = ATR x this. Default = 2.0.")]
        public double ATR_Multiplier { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Use MA Cross Exit", GroupName = "2. Exit Settings", Order = 3)]
        public bool UseMAExit { get; set; }

        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Min Bars In Trade", GroupName = "2. Exit Settings", Order = 4,
            Description = "Min bars before color flip exit is allowed. Default = 3.")]
        public int MinBarsInTrade { get; set; }

        // ---- Visuals ----
        [XmlIgnore]
        [Display(Name = "Long Exit Color", GroupName = "3. Visuals", Order = 1)]
        public Brush LongExitColor { get; set; }

        [Browsable(false)]
        public string LongExitColorSerializable
        {
            get { return Serialize.BrushToString(LongExitColor); }
            set { LongExitColor = Serialize.StringToBrush(value); }
        }

        [XmlIgnore]
        [Display(Name = "Short Exit Color", GroupName = "3. Visuals", Order = 2)]
        public Brush ShortExitColor { get; set; }

        [Browsable(false)]
        public string ShortExitColorSerializable
        {
            get { return Serialize.BrushToString(ShortExitColor); }
            set { ShortExitColor = Serialize.StringToBrush(value); }
        }

        [NinjaScriptProperty]
        [Range(1, 20)]
        [Display(Name = "Arrow Offset (ticks)", GroupName = "3. Visuals", Order = 3)]
        public int ArrowOffset { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Show Label", GroupName = "3. Visuals", Order = 4)]
        public bool ShowLabel { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Long Exit Label", GroupName = "3. Visuals", Order = 5)]
        public string LongExitLabel { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Short Exit Label", GroupName = "3. Visuals", Order = 6)]
        public string ShortExitLabel { get; set; }

        [NinjaScriptProperty]
        [Range(6, 30)]
        [Display(Name = "Label Font Size", GroupName = "3. Visuals", Order = 7)]
        public int LabelFontSize { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Show Trail Line", GroupName = "3. Visuals", Order = 8)]
        public bool ShowTrailLine { get; set; }

        // ---- Output plots ----
        [Browsable(false)]
        [XmlIgnore]
        public Series<double> LongExit      { get { return Values[0]; } }

        [Browsable(false)]
        [XmlIgnore]
        public Series<double> ShortExit     { get { return Values[1]; } }

        [Browsable(false)]
        [XmlIgnore]
        public Series<double> TrailStopLine { get { return Values[2]; } }

        #endregion
    }
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private BSCExitSignal[] cacheBSCExitSignal;
		public BSCExitSignal BSCExitSignal(ThunderZillaMAType tZ_TrendMAType, int tZ_TrendPeriod, bool tZ_TrendSmoothingEnabled, ThunderZillaMAType tZ_TrendSmoothingMethod, int tZ_TrendSmoothingPeriod, double tZ_StopOffsetMultiplier, int tZ_SignalQtyPerFlat, int tZ_SignalQtyPerTrend, double tZ_BullishThreshold, double tZ_BearishThreshold, int pK_Period, double pK_Factor, int pK_MiddlePeriod, int pK_SignalBreakSplitBars, int pK_SignalPullbackFindPeriod, int aTR_Period, double aTR_Multiplier, bool useMAExit, int minBarsInTrade, int arrowOffset, bool showLabel, string longExitLabel, string shortExitLabel, int labelFontSize, bool showTrailLine)
		{
			return BSCExitSignal(Input, tZ_TrendMAType, tZ_TrendPeriod, tZ_TrendSmoothingEnabled, tZ_TrendSmoothingMethod, tZ_TrendSmoothingPeriod, tZ_StopOffsetMultiplier, tZ_SignalQtyPerFlat, tZ_SignalQtyPerTrend, tZ_BullishThreshold, tZ_BearishThreshold, pK_Period, pK_Factor, pK_MiddlePeriod, pK_SignalBreakSplitBars, pK_SignalPullbackFindPeriod, aTR_Period, aTR_Multiplier, useMAExit, minBarsInTrade, arrowOffset, showLabel, longExitLabel, shortExitLabel, labelFontSize, showTrailLine);
		}

		public BSCExitSignal BSCExitSignal(ISeries<double> input, ThunderZillaMAType tZ_TrendMAType, int tZ_TrendPeriod, bool tZ_TrendSmoothingEnabled, ThunderZillaMAType tZ_TrendSmoothingMethod, int tZ_TrendSmoothingPeriod, double tZ_StopOffsetMultiplier, int tZ_SignalQtyPerFlat, int tZ_SignalQtyPerTrend, double tZ_BullishThreshold, double tZ_BearishThreshold, int pK_Period, double pK_Factor, int pK_MiddlePeriod, int pK_SignalBreakSplitBars, int pK_SignalPullbackFindPeriod, int aTR_Period, double aTR_Multiplier, bool useMAExit, int minBarsInTrade, int arrowOffset, bool showLabel, string longExitLabel, string shortExitLabel, int labelFontSize, bool showTrailLine)
		{
			if (cacheBSCExitSignal != null)
				for (int idx = 0; idx < cacheBSCExitSignal.Length; idx++)
					if (cacheBSCExitSignal[idx] != null && cacheBSCExitSignal[idx].TZ_TrendMAType == tZ_TrendMAType && cacheBSCExitSignal[idx].TZ_TrendPeriod == tZ_TrendPeriod && cacheBSCExitSignal[idx].TZ_TrendSmoothingEnabled == tZ_TrendSmoothingEnabled && cacheBSCExitSignal[idx].TZ_TrendSmoothingMethod == tZ_TrendSmoothingMethod && cacheBSCExitSignal[idx].TZ_TrendSmoothingPeriod == tZ_TrendSmoothingPeriod && cacheBSCExitSignal[idx].TZ_StopOffsetMultiplier == tZ_StopOffsetMultiplier && cacheBSCExitSignal[idx].TZ_SignalQtyPerFlat == tZ_SignalQtyPerFlat && cacheBSCExitSignal[idx].TZ_SignalQtyPerTrend == tZ_SignalQtyPerTrend && cacheBSCExitSignal[idx].TZ_BullishThreshold == tZ_BullishThreshold && cacheBSCExitSignal[idx].TZ_BearishThreshold == tZ_BearishThreshold && cacheBSCExitSignal[idx].PK_Period == pK_Period && cacheBSCExitSignal[idx].PK_Factor == pK_Factor && cacheBSCExitSignal[idx].PK_MiddlePeriod == pK_MiddlePeriod && cacheBSCExitSignal[idx].PK_SignalBreakSplitBars == pK_SignalBreakSplitBars && cacheBSCExitSignal[idx].PK_SignalPullbackFindPeriod == pK_SignalPullbackFindPeriod && cacheBSCExitSignal[idx].ATR_Period == aTR_Period && cacheBSCExitSignal[idx].ATR_Multiplier == aTR_Multiplier && cacheBSCExitSignal[idx].UseMAExit == useMAExit && cacheBSCExitSignal[idx].MinBarsInTrade == minBarsInTrade && cacheBSCExitSignal[idx].ArrowOffset == arrowOffset && cacheBSCExitSignal[idx].ShowLabel == showLabel && cacheBSCExitSignal[idx].LongExitLabel == longExitLabel && cacheBSCExitSignal[idx].ShortExitLabel == shortExitLabel && cacheBSCExitSignal[idx].LabelFontSize == labelFontSize && cacheBSCExitSignal[idx].ShowTrailLine == showTrailLine && cacheBSCExitSignal[idx].EqualsInput(input))
						return cacheBSCExitSignal[idx];
			return CacheIndicator<BSCExitSignal>(new BSCExitSignal(){ TZ_TrendMAType = tZ_TrendMAType, TZ_TrendPeriod = tZ_TrendPeriod, TZ_TrendSmoothingEnabled = tZ_TrendSmoothingEnabled, TZ_TrendSmoothingMethod = tZ_TrendSmoothingMethod, TZ_TrendSmoothingPeriod = tZ_TrendSmoothingPeriod, TZ_StopOffsetMultiplier = tZ_StopOffsetMultiplier, TZ_SignalQtyPerFlat = tZ_SignalQtyPerFlat, TZ_SignalQtyPerTrend = tZ_SignalQtyPerTrend, TZ_BullishThreshold = tZ_BullishThreshold, TZ_BearishThreshold = tZ_BearishThreshold, PK_Period = pK_Period, PK_Factor = pK_Factor, PK_MiddlePeriod = pK_MiddlePeriod, PK_SignalBreakSplitBars = pK_SignalBreakSplitBars, PK_SignalPullbackFindPeriod = pK_SignalPullbackFindPeriod, ATR_Period = aTR_Period, ATR_Multiplier = aTR_Multiplier, UseMAExit = useMAExit, MinBarsInTrade = minBarsInTrade, ArrowOffset = arrowOffset, ShowLabel = showLabel, LongExitLabel = longExitLabel, ShortExitLabel = shortExitLabel, LabelFontSize = labelFontSize, ShowTrailLine = showTrailLine }, input, ref cacheBSCExitSignal);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.BSCExitSignal BSCExitSignal(ThunderZillaMAType tZ_TrendMAType, int tZ_TrendPeriod, bool tZ_TrendSmoothingEnabled, ThunderZillaMAType tZ_TrendSmoothingMethod, int tZ_TrendSmoothingPeriod, double tZ_StopOffsetMultiplier, int tZ_SignalQtyPerFlat, int tZ_SignalQtyPerTrend, double tZ_BullishThreshold, double tZ_BearishThreshold, int pK_Period, double pK_Factor, int pK_MiddlePeriod, int pK_SignalBreakSplitBars, int pK_SignalPullbackFindPeriod, int aTR_Period, double aTR_Multiplier, bool useMAExit, int minBarsInTrade, int arrowOffset, bool showLabel, string longExitLabel, string shortExitLabel, int labelFontSize, bool showTrailLine)
		{
			return indicator.BSCExitSignal(Input, tZ_TrendMAType, tZ_TrendPeriod, tZ_TrendSmoothingEnabled, tZ_TrendSmoothingMethod, tZ_TrendSmoothingPeriod, tZ_StopOffsetMultiplier, tZ_SignalQtyPerFlat, tZ_SignalQtyPerTrend, tZ_BullishThreshold, tZ_BearishThreshold, pK_Period, pK_Factor, pK_MiddlePeriod, pK_SignalBreakSplitBars, pK_SignalPullbackFindPeriod, aTR_Period, aTR_Multiplier, useMAExit, minBarsInTrade, arrowOffset, showLabel, longExitLabel, shortExitLabel, labelFontSize, showTrailLine);
		}

		public Indicators.BSCExitSignal BSCExitSignal(ISeries<double> input , ThunderZillaMAType tZ_TrendMAType, int tZ_TrendPeriod, bool tZ_TrendSmoothingEnabled, ThunderZillaMAType tZ_TrendSmoothingMethod, int tZ_TrendSmoothingPeriod, double tZ_StopOffsetMultiplier, int tZ_SignalQtyPerFlat, int tZ_SignalQtyPerTrend, double tZ_BullishThreshold, double tZ_BearishThreshold, int pK_Period, double pK_Factor, int pK_MiddlePeriod, int pK_SignalBreakSplitBars, int pK_SignalPullbackFindPeriod, int aTR_Period, double aTR_Multiplier, bool useMAExit, int minBarsInTrade, int arrowOffset, bool showLabel, string longExitLabel, string shortExitLabel, int labelFontSize, bool showTrailLine)
		{
			return indicator.BSCExitSignal(input, tZ_TrendMAType, tZ_TrendPeriod, tZ_TrendSmoothingEnabled, tZ_TrendSmoothingMethod, tZ_TrendSmoothingPeriod, tZ_StopOffsetMultiplier, tZ_SignalQtyPerFlat, tZ_SignalQtyPerTrend, tZ_BullishThreshold, tZ_BearishThreshold, pK_Period, pK_Factor, pK_MiddlePeriod, pK_SignalBreakSplitBars, pK_SignalPullbackFindPeriod, aTR_Period, aTR_Multiplier, useMAExit, minBarsInTrade, arrowOffset, showLabel, longExitLabel, shortExitLabel, labelFontSize, showTrailLine);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.BSCExitSignal BSCExitSignal(ThunderZillaMAType tZ_TrendMAType, int tZ_TrendPeriod, bool tZ_TrendSmoothingEnabled, ThunderZillaMAType tZ_TrendSmoothingMethod, int tZ_TrendSmoothingPeriod, double tZ_StopOffsetMultiplier, int tZ_SignalQtyPerFlat, int tZ_SignalQtyPerTrend, double tZ_BullishThreshold, double tZ_BearishThreshold, int pK_Period, double pK_Factor, int pK_MiddlePeriod, int pK_SignalBreakSplitBars, int pK_SignalPullbackFindPeriod, int aTR_Period, double aTR_Multiplier, bool useMAExit, int minBarsInTrade, int arrowOffset, bool showLabel, string longExitLabel, string shortExitLabel, int labelFontSize, bool showTrailLine)
		{
			return indicator.BSCExitSignal(Input, tZ_TrendMAType, tZ_TrendPeriod, tZ_TrendSmoothingEnabled, tZ_TrendSmoothingMethod, tZ_TrendSmoothingPeriod, tZ_StopOffsetMultiplier, tZ_SignalQtyPerFlat, tZ_SignalQtyPerTrend, tZ_BullishThreshold, tZ_BearishThreshold, pK_Period, pK_Factor, pK_MiddlePeriod, pK_SignalBreakSplitBars, pK_SignalPullbackFindPeriod, aTR_Period, aTR_Multiplier, useMAExit, minBarsInTrade, arrowOffset, showLabel, longExitLabel, shortExitLabel, labelFontSize, showTrailLine);
		}

		public Indicators.BSCExitSignal BSCExitSignal(ISeries<double> input , ThunderZillaMAType tZ_TrendMAType, int tZ_TrendPeriod, bool tZ_TrendSmoothingEnabled, ThunderZillaMAType tZ_TrendSmoothingMethod, int tZ_TrendSmoothingPeriod, double tZ_StopOffsetMultiplier, int tZ_SignalQtyPerFlat, int tZ_SignalQtyPerTrend, double tZ_BullishThreshold, double tZ_BearishThreshold, int pK_Period, double pK_Factor, int pK_MiddlePeriod, int pK_SignalBreakSplitBars, int pK_SignalPullbackFindPeriod, int aTR_Period, double aTR_Multiplier, bool useMAExit, int minBarsInTrade, int arrowOffset, bool showLabel, string longExitLabel, string shortExitLabel, int labelFontSize, bool showTrailLine)
		{
			return indicator.BSCExitSignal(input, tZ_TrendMAType, tZ_TrendPeriod, tZ_TrendSmoothingEnabled, tZ_TrendSmoothingMethod, tZ_TrendSmoothingPeriod, tZ_StopOffsetMultiplier, tZ_SignalQtyPerFlat, tZ_SignalQtyPerTrend, tZ_BullishThreshold, tZ_BearishThreshold, pK_Period, pK_Factor, pK_MiddlePeriod, pK_SignalBreakSplitBars, pK_SignalPullbackFindPeriod, aTR_Period, aTR_Multiplier, useMAExit, minBarsInTrade, arrowOffset, showLabel, longExitLabel, shortExitLabel, labelFontSize, showTrailLine);
		}
	}
}

#endregion
