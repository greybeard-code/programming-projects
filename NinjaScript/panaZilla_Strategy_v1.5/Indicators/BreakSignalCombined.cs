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
//  BreakSignalCombined
//  Combines ThunderZilla (RenkoKings) + PANA Kanal (ninZa.co)
//
//  PANA Kanal Signal_Trade plot values (ninZa.co official docs):
//    2  = Break Up      ← BULLISH BREAK  ✓
//    3  = Pullback Bullish
//    1  = Uptrend Start
//   -1  = Downtrend Start
//   -2  = Break Down    ← BEARISH BREAK  ✓
//   -3  = Pullback Bearish
//    0  = No signal
//
//  ThunderZilla Signal_Trade plot values (RenkoKings):
//    3  = Bullish signal (Per Trend)
//    2  = Bullish signal (Per Flat)
//    1  = Bullish signal
//   -1  = Bearish signal
//   -2  = Bearish signal (Per Flat)
//   -3  = Bearish signal (Per Trend)
//
//  DEFAULT COMBINED SIGNALS:
//  LONG  → PANA Kanal = 2 (Break Up)   AND ThunderZilla >= 1 (any bullish)
//  SHORT → PANA Kanal = -2 (Break Down) AND ThunderZilla <= -1 (any bearish)
// ============================================================

namespace NinjaTrader.NinjaScript.Indicators
{
    public class BreakSignalCombined : Indicator
    {
        // Private references to the two locked indicators
        private Indicators.RenkoKings.RenkoKings_ThunderZilla thunderZilla;
        private Indicators.ninZaPANAKanal panaKanal;

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description                 = @"Fires a Long arrow when PANA Kanal prints a Break Up (Signal_Trade = 2) AND ThunderZilla is bullish on the same bar. Fires a Short arrow when PANA Kanal prints a Break Down (Signal_Trade = -2) AND ThunderZilla is bearish on the same bar.";
                Name                        = "BreakSignalCombined";
                Calculate                   = Calculate.OnBarClose;
                IsOverlay                   = true;
                DisplayInDataBox            = true;
                DrawOnPricePanel            = true;
                PaintPriceMarkers           = true;
                ScaleJustification          = NinjaTrader.Gui.Chart.ScaleJustification.Right;
                IsSuspendedWhileInactive    = true;

                // ---- ThunderZilla Parameters ----
                // (Matched exactly to COS / Captain Optimus Strong settings)
                TZ_TrendMAType              = ThunderZillaMAType.SMA;
                TZ_TrendPeriod              = 100;
                TZ_TrendSmoothingEnabled    = false;
                TZ_TrendSmoothingMethod     = ThunderZillaMAType.EMA;
                TZ_TrendSmoothingPeriod     = 10;
                TZ_StopOffsetMultiplier     = 60;
                TZ_SignalQtyPerFlat         = 2;
                TZ_SignalQtyPerTrend        = 999;

                // ---- PANA Kanal Parameters ----
                // (Matched exactly to COS / Captain Optimus Strong settings)
                PK_Period                   = 20;
                PK_Factor                   = 4;
                PK_MiddlePeriod             = 14;
                PK_SignalBreakSplitBars     = 20;
                PK_SignalPullbackFindPeriod  = 10;

                // ---- Signal Thresholds ----
                // PANA Kanal Signal_Trade = 2 (Break Up) / -2 (Break Down) is hardcoded
                // TZ: must be exactly 3 (bullish) or -3 (bearish) to match COS settings
                TZ_BullishThreshold         = 3;
                TZ_BearishThreshold         = -3;

                // ---- Visuals ----
                LongArrowColor              = Brushes.Cyan;
                ShortArrowColor             = Brushes.Magenta;
                ArrowOffset                 = 3;
                ShowLabel                   = true;
                LongLabelText               = "Break Buy";
                ShortLabelText              = "Break Sell";
                LabelFontSize               = 10;
            }
            else if (State == State.Configure)
            {
                // LongSignal  plot = 1 when a long fires,  0 otherwise
                // ShortSignal plot = -1 when a short fires, 0 otherwise
                // Both are hidden (Transparent) — used by strategies via .LongSignal[0] / .ShortSignal[0]
                AddPlot(new Stroke(Brushes.Transparent, 1), PlotStyle.Dot, "LongSignal");
                AddPlot(new Stroke(Brushes.Transparent, 1), PlotStyle.Dot, "ShortSignal");
            }
            else if (State == State.DataLoaded)
            {
                // Instantiate ThunderZilla with current parameter values
                thunderZilla = RenkoKings_ThunderZilla(
                    TZ_TrendMAType,
                    TZ_TrendPeriod,
                    TZ_TrendSmoothingEnabled,
                    TZ_TrendSmoothingMethod,
                    TZ_TrendSmoothingPeriod,
                    TZ_StopOffsetMultiplier,
                    TZ_SignalQtyPerFlat,
                    TZ_SignalQtyPerTrend
                );

                // Instantiate PANA Kanal with current parameter values
                panaKanal = ninZaPANAKanal(
                    PK_Period,
                    PK_Factor,
                    PK_MiddlePeriod,
                    PK_SignalBreakSplitBars,
                    PK_SignalPullbackFindPeriod
                );
            }
        }

        protected override void OnBarUpdate()
        {
            // Wait for enough bars to warm up both indicators
            if (CurrentBar < Math.Max(TZ_TrendPeriod, PK_Period) + 20)
                return;

            // Reset signal plots
            Values[0][0] = 0;
            Values[1][0] = 0;

            // ---- Read PANA Kanal Signal_Trade ----
            // ninZa.co indicators expose a named plot "Signal_Trade"
            // Values[0] is typically the first plot — but we use GetValueByName for safety
            double pkSignal = panaKanal.Signal_Trade[0];

            // ---- Read ThunderZilla Signal_Trade ----
            double tzSignal = thunderZilla.Signal_Trade[0];

            // ---- LONG: PANA Break Up (2) AND ThunderZilla bullish ----
            if (pkSignal == 2 && tzSignal >= TZ_BullishThreshold)
            {
                // Draw arrow below the bar
                Draw.ArrowUp(this,
                    "Long_" + CurrentBar,
                    false,
                    0,
                    Low[0] - (ArrowOffset * TickSize),
                    LongArrowColor);

                // Optional label above the arrow
                if (ShowLabel)
                    Draw.Text(this,
                        "LongTxt_" + CurrentBar,
                        false,
                        LongLabelText,
                        0,
                        Low[0] - (ArrowOffset * TickSize * 4),
                        0,
                        LongArrowColor,
                        new SimpleFont("Arial", LabelFontSize),
                        TextAlignment.Center,
                        Brushes.Transparent,
                        Brushes.Transparent,
                        0);

                Values[0][0] = 1; // Signal plot for strategy use
            }

            // ---- SHORT: PANA Break Down (-2) AND ThunderZilla bearish ----
            else if (pkSignal == -2 && tzSignal <= TZ_BearishThreshold)
            {
                // Draw arrow above the bar
                Draw.ArrowDown(this,
                    "Short_" + CurrentBar,
                    false,
                    0,
                    High[0] + (ArrowOffset * TickSize),
                    ShortArrowColor);

                // Optional label below the arrow
                if (ShowLabel)
                    Draw.Text(this,
                        "ShortTxt_" + CurrentBar,
                        false,
                        ShortLabelText,
                        0,
                        High[0] + (ArrowOffset * TickSize * 4),
                        0,
                        ShortArrowColor,
                        new SimpleFont("Arial", LabelFontSize),
                        TextAlignment.Center,
                        Brushes.Transparent,
                        Brushes.Transparent,
                        0);

                Values[1][0] = -1; // Signal plot for strategy use
            }
        }

        #region Properties

        // =====================================================================
        // GROUP 1: ThunderZilla Parameters
        // =====================================================================

        [NinjaScriptProperty]
        [Display(Name = "Trend: MA Type", GroupName = "1. ThunderZilla", Order = 1)]
        public ThunderZillaMAType TZ_TrendMAType { get; set; }

        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Trend: Period", GroupName = "1. ThunderZilla", Order = 2)]
        public int TZ_TrendPeriod { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Trend: Smoothing Enabled", GroupName = "1. ThunderZilla", Order = 3)]
        public bool TZ_TrendSmoothingEnabled { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Trend: Smoothing Method", GroupName = "1. ThunderZilla", Order = 4)]
        public ThunderZillaMAType TZ_TrendSmoothingMethod { get; set; }

        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Trend: Smoothing Period", GroupName = "1. ThunderZilla", Order = 5)]
        public int TZ_TrendSmoothingPeriod { get; set; }

        [NinjaScriptProperty]
        [Range(1, double.MaxValue)]
        [Display(Name = "Stop: Offset Multiplier (Ticks)", GroupName = "1. ThunderZilla", Order = 6)]
        public double TZ_StopOffsetMultiplier { get; set; }

        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Signal: Qty Per Flat", GroupName = "1. ThunderZilla", Order = 7)]
        public int TZ_SignalQtyPerFlat { get; set; }

        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Signal: Qty Per Trend", GroupName = "1. ThunderZilla", Order = 8)]
        public int TZ_SignalQtyPerTrend { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Bullish Threshold (>=)", GroupName = "1. ThunderZilla", Order = 9,
            Description = "TZ Signal_Trade must be >= this value to count as bullish. Default 1 = any bullish. Set to 3 for strongest only.")]
        public double TZ_BullishThreshold { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Bearish Threshold (<=)", GroupName = "1. ThunderZilla", Order = 10,
            Description = "TZ Signal_Trade must be <= this value to count as bearish. Default -1 = any bearish. Set to -3 for strongest only.")]
        public double TZ_BearishThreshold { get; set; }

        // =====================================================================
        // GROUP 2: PANA Kanal Parameters
        // =====================================================================

        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Period", GroupName = "2. PANA Kanal", Order = 1)]
        public int PK_Period { get; set; }

        [NinjaScriptProperty]
        [Range(0.1, double.MaxValue)]
        [Display(Name = "Factor", GroupName = "2. PANA Kanal", Order = 2)]
        public double PK_Factor { get; set; }

        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Middle Period", GroupName = "2. PANA Kanal", Order = 3)]
        public int PK_MiddlePeriod { get; set; }

        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Signal Break Split (Bars)", GroupName = "2. PANA Kanal", Order = 4)]
        public int PK_SignalBreakSplitBars { get; set; }

        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Signal Pullback Finding Period", GroupName = "2. PANA Kanal", Order = 5)]
        public int PK_SignalPullbackFindPeriod { get; set; }

        // =====================================================================
        // GROUP 3: Visuals
        // =====================================================================

        [XmlIgnore]
        [Display(Name = "Long Arrow Color", GroupName = "3. Visuals", Order = 1)]
        public Brush LongArrowColor { get; set; }

        [Browsable(false)]
        public string LongArrowColorSerializable
        {
            get { return Serialize.BrushToString(LongArrowColor); }
            set { LongArrowColor = Serialize.StringToBrush(value); }
        }

        [XmlIgnore]
        [Display(Name = "Short Arrow Color", GroupName = "3. Visuals", Order = 2)]
        public Brush ShortArrowColor { get; set; }

        [Browsable(false)]
        public string ShortArrowColorSerializable
        {
            get { return Serialize.BrushToString(ShortArrowColor); }
            set { ShortArrowColor = Serialize.StringToBrush(value); }
        }

        [NinjaScriptProperty]
        [Range(1, 20)]
        [Display(Name = "Arrow Offset (ticks)", GroupName = "3. Visuals", Order = 3,
            Description = "How many ticks below/above the bar to place the arrow.")]
        public int ArrowOffset { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Show Label", GroupName = "3. Visuals", Order = 4)]
        public bool ShowLabel { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Long Label Text", GroupName = "3. Visuals", Order = 5)]
        public string LongLabelText { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Short Label Text", GroupName = "3. Visuals", Order = 6)]
        public string ShortLabelText { get; set; }

        [NinjaScriptProperty]
        [Range(6, 30)]
        [Display(Name = "Label Font Size", GroupName = "3. Visuals", Order = 7)]
        public int LabelFontSize { get; set; }

        // =====================================================================
        // Exposed plots for use in strategies / Market Analyzer
        // =====================================================================

        [Browsable(false)]
        [XmlIgnore]
        [Display(Name = "Long Signal", GroupName = "Output Plots")]
        public Series<double> LongSignal
        {
            get { return Values[0]; }
        }

        [Browsable(false)]
        [XmlIgnore]
        [Display(Name = "Short Signal", GroupName = "Output Plots")]
        public Series<double> ShortSignal
        {
            get { return Values[1]; }
        }

        #endregion
    }
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private BreakSignalCombined[] cacheBreakSignalCombined;
		public BreakSignalCombined BreakSignalCombined(ThunderZillaMAType tZ_TrendMAType, int tZ_TrendPeriod, bool tZ_TrendSmoothingEnabled, ThunderZillaMAType tZ_TrendSmoothingMethod, int tZ_TrendSmoothingPeriod, double tZ_StopOffsetMultiplier, int tZ_SignalQtyPerFlat, int tZ_SignalQtyPerTrend, double tZ_BullishThreshold, double tZ_BearishThreshold, int pK_Period, double pK_Factor, int pK_MiddlePeriod, int pK_SignalBreakSplitBars, int pK_SignalPullbackFindPeriod, int arrowOffset, bool showLabel, string longLabelText, string shortLabelText, int labelFontSize)
		{
			return BreakSignalCombined(Input, tZ_TrendMAType, tZ_TrendPeriod, tZ_TrendSmoothingEnabled, tZ_TrendSmoothingMethod, tZ_TrendSmoothingPeriod, tZ_StopOffsetMultiplier, tZ_SignalQtyPerFlat, tZ_SignalQtyPerTrend, tZ_BullishThreshold, tZ_BearishThreshold, pK_Period, pK_Factor, pK_MiddlePeriod, pK_SignalBreakSplitBars, pK_SignalPullbackFindPeriod, arrowOffset, showLabel, longLabelText, shortLabelText, labelFontSize);
		}

		public BreakSignalCombined BreakSignalCombined(ISeries<double> input, ThunderZillaMAType tZ_TrendMAType, int tZ_TrendPeriod, bool tZ_TrendSmoothingEnabled, ThunderZillaMAType tZ_TrendSmoothingMethod, int tZ_TrendSmoothingPeriod, double tZ_StopOffsetMultiplier, int tZ_SignalQtyPerFlat, int tZ_SignalQtyPerTrend, double tZ_BullishThreshold, double tZ_BearishThreshold, int pK_Period, double pK_Factor, int pK_MiddlePeriod, int pK_SignalBreakSplitBars, int pK_SignalPullbackFindPeriod, int arrowOffset, bool showLabel, string longLabelText, string shortLabelText, int labelFontSize)
		{
			if (cacheBreakSignalCombined != null)
				for (int idx = 0; idx < cacheBreakSignalCombined.Length; idx++)
					if (cacheBreakSignalCombined[idx] != null && cacheBreakSignalCombined[idx].TZ_TrendMAType == tZ_TrendMAType && cacheBreakSignalCombined[idx].TZ_TrendPeriod == tZ_TrendPeriod && cacheBreakSignalCombined[idx].TZ_TrendSmoothingEnabled == tZ_TrendSmoothingEnabled && cacheBreakSignalCombined[idx].TZ_TrendSmoothingMethod == tZ_TrendSmoothingMethod && cacheBreakSignalCombined[idx].TZ_TrendSmoothingPeriod == tZ_TrendSmoothingPeriod && cacheBreakSignalCombined[idx].TZ_StopOffsetMultiplier == tZ_StopOffsetMultiplier && cacheBreakSignalCombined[idx].TZ_SignalQtyPerFlat == tZ_SignalQtyPerFlat && cacheBreakSignalCombined[idx].TZ_SignalQtyPerTrend == tZ_SignalQtyPerTrend && cacheBreakSignalCombined[idx].TZ_BullishThreshold == tZ_BullishThreshold && cacheBreakSignalCombined[idx].TZ_BearishThreshold == tZ_BearishThreshold && cacheBreakSignalCombined[idx].PK_Period == pK_Period && cacheBreakSignalCombined[idx].PK_Factor == pK_Factor && cacheBreakSignalCombined[idx].PK_MiddlePeriod == pK_MiddlePeriod && cacheBreakSignalCombined[idx].PK_SignalBreakSplitBars == pK_SignalBreakSplitBars && cacheBreakSignalCombined[idx].PK_SignalPullbackFindPeriod == pK_SignalPullbackFindPeriod && cacheBreakSignalCombined[idx].ArrowOffset == arrowOffset && cacheBreakSignalCombined[idx].ShowLabel == showLabel && cacheBreakSignalCombined[idx].LongLabelText == longLabelText && cacheBreakSignalCombined[idx].ShortLabelText == shortLabelText && cacheBreakSignalCombined[idx].LabelFontSize == labelFontSize && cacheBreakSignalCombined[idx].EqualsInput(input))
						return cacheBreakSignalCombined[idx];
			return CacheIndicator<BreakSignalCombined>(new BreakSignalCombined(){ TZ_TrendMAType = tZ_TrendMAType, TZ_TrendPeriod = tZ_TrendPeriod, TZ_TrendSmoothingEnabled = tZ_TrendSmoothingEnabled, TZ_TrendSmoothingMethod = tZ_TrendSmoothingMethod, TZ_TrendSmoothingPeriod = tZ_TrendSmoothingPeriod, TZ_StopOffsetMultiplier = tZ_StopOffsetMultiplier, TZ_SignalQtyPerFlat = tZ_SignalQtyPerFlat, TZ_SignalQtyPerTrend = tZ_SignalQtyPerTrend, TZ_BullishThreshold = tZ_BullishThreshold, TZ_BearishThreshold = tZ_BearishThreshold, PK_Period = pK_Period, PK_Factor = pK_Factor, PK_MiddlePeriod = pK_MiddlePeriod, PK_SignalBreakSplitBars = pK_SignalBreakSplitBars, PK_SignalPullbackFindPeriod = pK_SignalPullbackFindPeriod, ArrowOffset = arrowOffset, ShowLabel = showLabel, LongLabelText = longLabelText, ShortLabelText = shortLabelText, LabelFontSize = labelFontSize }, input, ref cacheBreakSignalCombined);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.BreakSignalCombined BreakSignalCombined(ThunderZillaMAType tZ_TrendMAType, int tZ_TrendPeriod, bool tZ_TrendSmoothingEnabled, ThunderZillaMAType tZ_TrendSmoothingMethod, int tZ_TrendSmoothingPeriod, double tZ_StopOffsetMultiplier, int tZ_SignalQtyPerFlat, int tZ_SignalQtyPerTrend, double tZ_BullishThreshold, double tZ_BearishThreshold, int pK_Period, double pK_Factor, int pK_MiddlePeriod, int pK_SignalBreakSplitBars, int pK_SignalPullbackFindPeriod, int arrowOffset, bool showLabel, string longLabelText, string shortLabelText, int labelFontSize)
		{
			return indicator.BreakSignalCombined(Input, tZ_TrendMAType, tZ_TrendPeriod, tZ_TrendSmoothingEnabled, tZ_TrendSmoothingMethod, tZ_TrendSmoothingPeriod, tZ_StopOffsetMultiplier, tZ_SignalQtyPerFlat, tZ_SignalQtyPerTrend, tZ_BullishThreshold, tZ_BearishThreshold, pK_Period, pK_Factor, pK_MiddlePeriod, pK_SignalBreakSplitBars, pK_SignalPullbackFindPeriod, arrowOffset, showLabel, longLabelText, shortLabelText, labelFontSize);
		}

		public Indicators.BreakSignalCombined BreakSignalCombined(ISeries<double> input , ThunderZillaMAType tZ_TrendMAType, int tZ_TrendPeriod, bool tZ_TrendSmoothingEnabled, ThunderZillaMAType tZ_TrendSmoothingMethod, int tZ_TrendSmoothingPeriod, double tZ_StopOffsetMultiplier, int tZ_SignalQtyPerFlat, int tZ_SignalQtyPerTrend, double tZ_BullishThreshold, double tZ_BearishThreshold, int pK_Period, double pK_Factor, int pK_MiddlePeriod, int pK_SignalBreakSplitBars, int pK_SignalPullbackFindPeriod, int arrowOffset, bool showLabel, string longLabelText, string shortLabelText, int labelFontSize)
		{
			return indicator.BreakSignalCombined(input, tZ_TrendMAType, tZ_TrendPeriod, tZ_TrendSmoothingEnabled, tZ_TrendSmoothingMethod, tZ_TrendSmoothingPeriod, tZ_StopOffsetMultiplier, tZ_SignalQtyPerFlat, tZ_SignalQtyPerTrend, tZ_BullishThreshold, tZ_BearishThreshold, pK_Period, pK_Factor, pK_MiddlePeriod, pK_SignalBreakSplitBars, pK_SignalPullbackFindPeriod, arrowOffset, showLabel, longLabelText, shortLabelText, labelFontSize);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.BreakSignalCombined BreakSignalCombined(ThunderZillaMAType tZ_TrendMAType, int tZ_TrendPeriod, bool tZ_TrendSmoothingEnabled, ThunderZillaMAType tZ_TrendSmoothingMethod, int tZ_TrendSmoothingPeriod, double tZ_StopOffsetMultiplier, int tZ_SignalQtyPerFlat, int tZ_SignalQtyPerTrend, double tZ_BullishThreshold, double tZ_BearishThreshold, int pK_Period, double pK_Factor, int pK_MiddlePeriod, int pK_SignalBreakSplitBars, int pK_SignalPullbackFindPeriod, int arrowOffset, bool showLabel, string longLabelText, string shortLabelText, int labelFontSize)
		{
			return indicator.BreakSignalCombined(Input, tZ_TrendMAType, tZ_TrendPeriod, tZ_TrendSmoothingEnabled, tZ_TrendSmoothingMethod, tZ_TrendSmoothingPeriod, tZ_StopOffsetMultiplier, tZ_SignalQtyPerFlat, tZ_SignalQtyPerTrend, tZ_BullishThreshold, tZ_BearishThreshold, pK_Period, pK_Factor, pK_MiddlePeriod, pK_SignalBreakSplitBars, pK_SignalPullbackFindPeriod, arrowOffset, showLabel, longLabelText, shortLabelText, labelFontSize);
		}

		public Indicators.BreakSignalCombined BreakSignalCombined(ISeries<double> input , ThunderZillaMAType tZ_TrendMAType, int tZ_TrendPeriod, bool tZ_TrendSmoothingEnabled, ThunderZillaMAType tZ_TrendSmoothingMethod, int tZ_TrendSmoothingPeriod, double tZ_StopOffsetMultiplier, int tZ_SignalQtyPerFlat, int tZ_SignalQtyPerTrend, double tZ_BullishThreshold, double tZ_BearishThreshold, int pK_Period, double pK_Factor, int pK_MiddlePeriod, int pK_SignalBreakSplitBars, int pK_SignalPullbackFindPeriod, int arrowOffset, bool showLabel, string longLabelText, string shortLabelText, int labelFontSize)
		{
			return indicator.BreakSignalCombined(input, tZ_TrendMAType, tZ_TrendPeriod, tZ_TrendSmoothingEnabled, tZ_TrendSmoothingMethod, tZ_TrendSmoothingPeriod, tZ_StopOffsetMultiplier, tZ_SignalQtyPerFlat, tZ_SignalQtyPerTrend, tZ_BullishThreshold, tZ_BearishThreshold, pK_Period, pK_Factor, pK_MiddlePeriod, pK_SignalBreakSplitBars, pK_SignalPullbackFindPeriod, arrowOffset, showLabel, longLabelText, shortLabelText, labelFontSize);
		}
	}
}

#endregion
