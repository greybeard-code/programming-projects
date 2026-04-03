// MaverickAIpro_PostConsolidation.cs
// Based on MaverickAIpro by MaverickIndicators.com
// Modified: Arrows only fire on the FIRST momentum bar after a qualifying consolidation period (grey bars).
// Arrows are drawn with Draw.ArrowUp/Down and DotSignal plot is set for Predator compatibility.

// Greybeard Edits:
// Added ShowTransparentPlotsInDataBox = true;

using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Threading;
using System.Xml.Serialization;
using NinjaTrader.Core;
using NinjaTrader.Data;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.NinjaScript;
using NinjaTrader.NinjaScript.DrawingTools;
using SharpDX;
using SharpDX.Direct2D1;
using NinjaTrader.NinjaScript.Indicators;

using DXBrush          = SharpDX.Direct2D1.Brush;
using DXSolidColorBrush = SharpDX.Direct2D1.SolidColorBrush;
using Brush            = System.Windows.Media.Brush;
using Color            = System.Windows.Media.Color;
using SolidColorBrush  = System.Windows.Media.SolidColorBrush;

namespace NinjaTrader.NinjaScript.Indicators.MaverickIndicators
{
    public class MaverickAIpro_PostConsolidation : Indicator
    {
        // ── Internal sub-indicators ─────────────────────────────────────────────
        private MACD macdHigherTF1;
        private MACD macdHigherTF2;
        private MACD macdHigherTF3;
        private MACD macdHigherTF4;

        private EMA ema5;
        private EMA ema9;
        private EMA ema200;
        private VMA vma;

        private const int higherTF1BarsIndex = 1;
        private const int higherTF2BarsIndex = 2;
        private const int higherTF3BarsIndex = 3;
        private const int higherTF4BarsIndex = 4;

        // ── Sequence state machine ──────────────────────────────────────────────
        // Tracks the Grey → (Directional) → Momentum sequence.
        //
        //  State values:
        //    0 = Idle          : not enough grey bars yet
        //    1 = Armed         : enough grey bars accumulated, watching for directional/momentum
        //    2 = Directional   : at least one 3/4 directional bar seen after arming
        //
        // sequenceDirection:  0 = undecided, 1 = bullish, -1 = bearish
        // Once set by the first non-grey bar, any flip resets the whole sequence.

        private int consecutiveGreyBars  = 0;
        private int sequenceState        = 0;   // 0=Idle, 1=Armed, 2=Directional
        private int sequenceDirection    = 0;   // 0=none, 1=bull, -1=bear

        // ── UI button ────────────────────────────────────────────────────────────
        private System.Windows.Controls.Button trendToggleButton;
        private bool isButtonCreated;

        // ── Cached brushes ───────────────────────────────────────────────────────
        private Brush cachedBgUpBrush;
        private Brush cachedBgDownBrush;

        // ════════════════════════════════════════════════════════════════════════
        //  Plot series (must match AddPlot order in SetDefaults)
        // ════════════════════════════════════════════════════════════════════════
        [Browsable(false)] [XmlIgnore]
        public Series<double> CandleSignal    => Values[0];

        [Browsable(false)] [XmlIgnore]
        public Series<double> DotSignal       => Values[1];   // ← Predator reads this

        [Browsable(false)] [XmlIgnore]
        public Series<double> EMA5            => Values[2];

        [Browsable(false)] [XmlIgnore]
        public Series<double> EMA9            => Values[3];

        [Browsable(false)] [XmlIgnore]
        public Series<double> MaverickTradeLine => Values[4];

        [Browsable(false)] [XmlIgnore]
        public Series<double> MaverickGuide   => Values[5];

        // ════════════════════════════════════════════════════════════════════════
        //  Properties – MACD (hidden, fixed)
        // ════════════════════════════════════════════════════════════════════════
        [Browsable(false)] public int FastPeriod   { get; set; }
        [Browsable(false)] public int SlowPeriod   { get; set; }
        [Browsable(false)] public int SmoothPeriod { get; set; }

        // ════════════════════════════════════════════════════════════════════════
        //  Properties – TimeFrames
        // ════════════════════════════════════════════════════════════════════════
        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "TimeFrame 1", Order = 1, GroupName = "TimeFrames")]
        public int HigherTimeframe1Period { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "TimeFrame 1 Type", Order = 2, GroupName = "TimeFrames")]
        public BarsPeriodType HigherTimeframe1Type { get; set; }

        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "TimeFrame 2", Order = 3, GroupName = "TimeFrames")]
        public int HigherTimeframe2Period { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "TimeFrame 2 Type", Order = 4, GroupName = "TimeFrames")]
        public BarsPeriodType HigherTimeframe2Type { get; set; }

        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "TimeFrame 3", Order = 5, GroupName = "TimeFrames")]
        public int HigherTimeframe3Period { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "TimeFrame 3 Type", Order = 6, GroupName = "TimeFrames")]
        public BarsPeriodType HigherTimeframe3Type { get; set; }

        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "TimeFrame 4", Order = 7, GroupName = "TimeFrames")]
        public int HigherTimeframe4Period { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "TimeFrame 4 Type", Order = 8, GroupName = "TimeFrames")]
        public BarsPeriodType HigherTimeframe4Type { get; set; }

        // ════════════════════════════════════════════════════════════════════════
        //  Properties – Bar Colors
        // ════════════════════════════════════════════════════════════════════════
        [NinjaScriptProperty] [XmlIgnore]
        [Display(Name = "All 4 Bullish Color", Order = 1, GroupName = "Bar Colors")]
        public Brush AllFourBullishColor { get; set; }
        [Browsable(false)]
        public string AllFourBullishColorSerializable
        { get => Serialize.BrushToString(AllFourBullishColor); set => AllFourBullishColor = Serialize.StringToBrush(value); }

        [NinjaScriptProperty] [XmlIgnore]
        [Display(Name = "All 4 Bearish Color", Order = 2, GroupName = "Bar Colors")]
        public Brush AllFourBearishColor { get; set; }
        [Browsable(false)]
        public string AllFourBearishColorSerializable
        { get => Serialize.BrushToString(AllFourBearishColor); set => AllFourBearishColor = Serialize.StringToBrush(value); }

        [NinjaScriptProperty] [XmlIgnore]
        [Display(Name = "3 Bullish Color", Order = 3, GroupName = "Bar Colors")]
        public Brush ThreeBullishColor { get; set; }
        [Browsable(false)]
        public string ThreeBullishColorSerializable
        { get => Serialize.BrushToString(ThreeBullishColor); set => ThreeBullishColor = Serialize.StringToBrush(value); }

        [NinjaScriptProperty] [XmlIgnore]
        [Display(Name = "3 Bearish Color", Order = 4, GroupName = "Bar Colors")]
        public Brush ThreeBearishColor { get; set; }
        [Browsable(false)]
        public string ThreeBearishColorSerializable
        { get => Serialize.BrushToString(ThreeBearishColor); set => ThreeBearishColor = Serialize.StringToBrush(value); }

        [NinjaScriptProperty] [XmlIgnore]
        [Display(Name = "Default (Grey) Color", Order = 5, GroupName = "Bar Colors")]
        public Brush DefaultBarColor { get; set; }
        [Browsable(false)]
        public string DefaultBarColorSerializable
        { get => Serialize.BrushToString(DefaultBarColor); set => DefaultBarColor = Serialize.StringToBrush(value); }

        // ════════════════════════════════════════════════════════════════════════
        //  Properties – Consolidation-Filtered Arrows  ← NEW GROUP
        // ════════════════════════════════════════════════════════════════════════
        [NinjaScriptProperty]
        [Display(Name = "Show Arrows", Description = "Enable/disable post-consolidation momentum arrows", Order = 1, GroupName = "Arrows")]
        public bool ShowDots { get; set; }

        [NinjaScriptProperty]
        [Range(1, 200)]
        [Display(Name = "Min Consolidation Bars",
                 Description = "Minimum consecutive grey bars required before an arrow is drawn on the next momentum bar",
                 Order = 2, GroupName = "Arrows")]
        public int MinConsolidationBars { get; set; }

        [NinjaScriptProperty] [XmlIgnore]
        [Display(Name = "Bullish Arrow Color", Order = 3, GroupName = "Arrows")]
        public Brush BullishDotColor { get; set; }
        [Browsable(false)]
        public string BullishDotColorSerializable
        { get => Serialize.BrushToString(BullishDotColor); set => BullishDotColor = Serialize.StringToBrush(value); }

        [NinjaScriptProperty] [XmlIgnore]
        [Display(Name = "Bearish Arrow Color", Order = 4, GroupName = "Arrows")]
        public Brush BearishDotColor { get; set; }
        [Browsable(false)]
        public string BearishDotColorSerializable
        { get => Serialize.BrushToString(BearishDotColor); set => BearishDotColor = Serialize.StringToBrush(value); }

        // ════════════════════════════════════════════════════════════════════════
        //  Properties – Background Trend
        // ════════════════════════════════════════════════════════════════════════
        [NinjaScriptProperty]
        [Display(Name = "Show Background Trend", Order = 1, GroupName = "Background Trend")]
        public bool ShowBackgroundTrend { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "QuickTrend Background", Order = 2, GroupName = "Background Trend")]
        public bool QuickTrendBackground { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "MaverickGuide Background", Order = 3, GroupName = "Background Trend")]
        public bool MaverickGuideBackground { get; set; }

        [NinjaScriptProperty] [XmlIgnore]
        [Display(Name = "Trend Up Color", Order = 4, GroupName = "Background Trend")]
        public Brush BackgroundTrendUpColor { get; set; }
        [Browsable(false)]
        public string BackgroundTrendUpColorSerializable
        { get => Serialize.BrushToString(BackgroundTrendUpColor); set => BackgroundTrendUpColor = Serialize.StringToBrush(value); }

        [NinjaScriptProperty] [XmlIgnore]
        [Display(Name = "Trend Down Color", Order = 5, GroupName = "Background Trend")]
        public Brush BackgroundTrendDownColor { get; set; }
        [Browsable(false)]
        public string BackgroundTrendDownColorSerializable
        { get => Serialize.BrushToString(BackgroundTrendDownColor); set => BackgroundTrendDownColor = Serialize.StringToBrush(value); }

        [NinjaScriptProperty]
        [Range(1, 100)]
        [Display(Name = "Background Opacity", Order = 6, GroupName = "Background Trend")]
        public int BackgroundOpacity { get; set; }

        // ════════════════════════════════════════════════════════════════════════
        //  Properties – Cloud
        // ════════════════════════════════════════════════════════════════════════
        [NinjaScriptProperty]
        [Display(Name = "Show Cloud", Order = 1, GroupName = "Cloud")]
        public bool ShowEmaCloud { get; set; }

        [NinjaScriptProperty] [XmlIgnore]
        [Display(Name = "Cloud Up Color", Order = 2, GroupName = "Cloud")]
        public Brush EmaCloudUpColor { get; set; }
        [Browsable(false)]
        public string EmaCloudUpColorSerializable
        { get => Serialize.BrushToString(EmaCloudUpColor); set => EmaCloudUpColor = Serialize.StringToBrush(value); }

        [NinjaScriptProperty] [XmlIgnore]
        [Display(Name = "Cloud Down Color", Order = 3, GroupName = "Cloud")]
        public Brush EmaCloudDownColor { get; set; }
        [Browsable(false)]
        public string EmaCloudDownColorSerializable
        { get => Serialize.BrushToString(EmaCloudDownColor); set => EmaCloudDownColor = Serialize.StringToBrush(value); }

        [NinjaScriptProperty]
        [Range(1, 100)]
        [Display(Name = "Cloud Opacity", Order = 4, GroupName = "Cloud")]
        public int EmaCloudOpacity { get; set; }

        // ════════════════════════════════════════════════════════════════════════
        //  Properties – Maverick200
        // ════════════════════════════════════════════════════════════════════════
        [NinjaScriptProperty]
        [Display(Name = "Show Maverick200", Order = 1, GroupName = "Maverick200")]
        public bool Show200EMA { get; set; }

        [NinjaScriptProperty] [XmlIgnore]
        [Display(Name = "Line Above Color", Order = 2, GroupName = "Maverick200")]
        public Brush EMA200AboveColor { get; set; }
        [Browsable(false)]
        public string EMA200AboveColorSerializable
        { get => Serialize.BrushToString(EMA200AboveColor); set => EMA200AboveColor = Serialize.StringToBrush(value); }

        [NinjaScriptProperty] [XmlIgnore]
        [Display(Name = "Line Below Color", Order = 3, GroupName = "Maverick200")]
        public Brush EMA200BelowColor { get; set; }
        [Browsable(false)]
        public string EMA200BelowColorSerializable
        { get => Serialize.BrushToString(EMA200BelowColor); set => EMA200BelowColor = Serialize.StringToBrush(value); }

        // ════════════════════════════════════════════════════════════════════════
        //  Properties – MaverickGuide
        // ════════════════════════════════════════════════════════════════════════
        [NinjaScriptProperty]
        [Display(Name = "Show MaverickGuide", Order = 1, GroupName = "MaverickGuide")]
        public bool ShowMaverickGuide { get; set; }

        [Browsable(false)] public int VMAPeriod           { get; set; }
        [Browsable(false)] public int VMAVolatilityPeriod { get; set; }

        [NinjaScriptProperty] [XmlIgnore]
        [Display(Name = "Line Color", Order = 2, GroupName = "MaverickGuide")]
        public Brush MaverickGuideColor { get; set; }
        [Browsable(false)]
        public string MaverickGuideColorSerializable
        { get => Serialize.BrushToString(MaverickGuideColor); set => MaverickGuideColor = Serialize.StringToBrush(value); }

        [NinjaScriptProperty]
        [Range(1, 10)]
        [Display(Name = "Line Thickness", Order = 3, GroupName = "MaverickGuide")]
        public int MaverickGuideThickness { get; set; }

        // ════════════════════════════════════════════════════════════════════════
        //  Display name
        // ════════════════════════════════════════════════════════════════════════
        public override string DisplayName => "MaverickAIpro PostConsolidation";

        // ════════════════════════════════════════════════════════════════════════
        //  OnStateChange
        // ════════════════════════════════════════════════════════════════════════
        protected override void OnStateChange()
        {
            switch (State)
            {
                case State.SetDefaults:
                    Description = "MaverickAIpro – arrows only appear after a qualifying consolidation period (grey bars).";
                    Name        = "MaverickAIpro_PostConsolidation";
                    Calculate   = Calculate.OnBarClose;   // OnBarClose keeps arrow logic clean
                    IsOverlay   = true;
                    DisplayInDataBox    = true;
                    DrawOnPricePanel    = true;
                    PaintPriceMarkers   = true;
                    ScaleJustification  = ScaleJustification.Right;
                    IsSuspendedWhileInactive = true;
					ShowTransparentPlotsInDataBox = true;

                    // Plots (order MUST match Values[] indices above)
                    AddPlot(Brushes.Transparent, "CandleSignal");
                    AddPlot(Brushes.Transparent, "DotSignal");
                    AddPlot(Brushes.Transparent, "EMA1");
                    AddPlot(Brushes.Transparent, "EMA2");
                    AddPlot(new Stroke(Brushes.Green,       2f), PlotStyle.Line, "Maverick200");
                    AddPlot(new Stroke(Brushes.DodgerBlue,  1f), PlotStyle.Line, "MaverickGuide");

                    // MACD params (fixed, hidden)
                    FastPeriod   = 3;
                    SlowPeriod   = 10;
                    SmoothPeriod = 9;

                    // Timeframes
                    HigherTimeframe1Period = 1;  HigherTimeframe1Type = BarsPeriodType.Minute;
                    HigherTimeframe2Period = 3;  HigherTimeframe2Type = BarsPeriodType.Minute;
                    HigherTimeframe3Period = 5;  HigherTimeframe3Type = BarsPeriodType.Minute;
                    HigherTimeframe4Period = 10; HigherTimeframe4Type = BarsPeriodType.Minute;

                    // Bar colors
                    AllFourBullishColor = Brushes.LimeGreen;
                    AllFourBearishColor = Brushes.Magenta;
                    ThreeBullishColor   = Brushes.DarkGreen;
                    ThreeBearishColor   = Brushes.Red;
                    DefaultBarColor     = Brushes.Gray;

                    // Arrows
                    ShowDots            = true;
                    MinConsolidationBars = 3;          // ← default: require ≥3 grey bars
                    BullishDotColor     = Brushes.Lime;
                    BearishDotColor     = Brushes.Red;

                    // Background
                    ShowBackgroundTrend     = true;
                    QuickTrendBackground    = false;
                    MaverickGuideBackground = true;
                    BackgroundTrendUpColor  = Brushes.Green;
                    BackgroundTrendDownColor = Brushes.Red;
                    BackgroundOpacity       = 30;

                    // Cloud
                    ShowEmaCloud     = false;
                    EmaCloudUpColor  = Brushes.Green;
                    EmaCloudDownColor = Brushes.Red;
                    EmaCloudOpacity  = 30;

                    // Maverick200
                    Show200EMA        = false;
                    EMA200AboveColor  = Brushes.Green;
                    EMA200BelowColor  = Brushes.Red;

                    // MaverickGuide
                    ShowMaverickGuide      = true;
                    VMAPeriod              = 9;
                    VMAVolatilityPeriod    = 9;
                    MaverickGuideColor     = Brushes.DodgerBlue;
                    MaverickGuideThickness = 3;
                    break;

                case State.Configure:
                    AddDataSeries(HigherTimeframe1Type, HigherTimeframe1Period);
                    AddDataSeries(HigherTimeframe2Type, HigherTimeframe2Period);
                    AddDataSeries(HigherTimeframe3Type, HigherTimeframe3Period);
                    AddDataSeries(HigherTimeframe4Type, HigherTimeframe4Period);
                    break;

                case State.DataLoaded:
                    macdHigherTF1 = MACD(BarsArray[higherTF1BarsIndex], FastPeriod, SlowPeriod, SmoothPeriod);
                    macdHigherTF2 = MACD(BarsArray[higherTF2BarsIndex], FastPeriod, SlowPeriod, SmoothPeriod);
                    macdHigherTF3 = MACD(BarsArray[higherTF3BarsIndex], FastPeriod, SlowPeriod, SmoothPeriod);
                    macdHigherTF4 = MACD(BarsArray[higherTF4BarsIndex], FastPeriod, SlowPeriod, SmoothPeriod);
                    ema5   = EMA(5);
                    ema9   = EMA(9);
                    ema200 = EMA(200);
                    vma    = VMA(VMAPeriod, VMAVolatilityPeriod);
                    cachedBgUpBrush   = CreateFrozenBrush(BackgroundTrendUpColor,   BackgroundOpacity);
                    cachedBgDownBrush = CreateFrozenBrush(BackgroundTrendDownColor, BackgroundOpacity);
                    CreateTrendToggleButton();
                    break;

                case State.Terminated:
                    if (trendToggleButton != null && ChartControl?.Properties != null)
                    {
                        ChartControl.Dispatcher.InvokeAsync(() =>
                        {
                            if (UserControlCollection.Contains(trendToggleButton))
                                UserControlCollection.Remove(trendToggleButton);
                        });
                    }
                    break;
            }
        }

        // ════════════════════════════════════════════════════════════════════════
        //  OnBarUpdate
        // ════════════════════════════════════════════════════════════════════════
        protected override void OnBarUpdate()
        {
            // ── Guard: need enough bars on every series ──────────────────────────
            if (BarsInProgress != 0
                || CurrentBars[0] < 2
                || CurrentBars[higherTF1BarsIndex] < Math.Max(SlowPeriod, SmoothPeriod)
                || CurrentBars[higherTF2BarsIndex] < Math.Max(SlowPeriod, SmoothPeriod)
                || CurrentBars[higherTF3BarsIndex] < Math.Max(SlowPeriod, SmoothPeriod)
                || CurrentBars[higherTF4BarsIndex] < Math.Max(SlowPeriod, SmoothPeriod))
            {
                return;
            }

            // ── MACD values ──────────────────────────────────────────────────────
            double macd1    = macdHigherTF1.Default[0];
            double macd1Avg = macdHigherTF1.Avg[0];
            double macd2    = macdHigherTF2.Default[0];
            double macd2Avg = macdHigherTF2.Avg[0];
            double macd3    = macdHigherTF3.Default[0];
            double macd3Avg = macdHigherTF3.Avg[0];
            double macd4    = macdHigherTF4.Default[0];
            double macd4Avg = macdHigherTF4.Avg[0];

            double macd1Prev    = macdHigherTF1.Default[1];
            double macd1AvgPrev = macdHigherTF1.Avg[1];

            // ── Alignment counts ─────────────────────────────────────────────────
            bool tf1Bullish = macd1 > 0.0 && macd1Avg > 0.0;
            bool tf1Bearish = macd1 < 0.0 && macd1Avg < 0.0;
            bool tf2Bullish = macd2 > 0.0 && macd2Avg > 0.0;
            bool tf2Bearish = macd2 < 0.0 && macd2Avg < 0.0;
            bool tf3Bullish = macd3 > 0.0 && macd3Avg > 0.0;
            bool tf3Bearish = macd3 < 0.0 && macd3Avg < 0.0;
            bool tf4Bullish = macd4 > 0.0 && macd4Avg > 0.0;
            bool tf4Bearish = macd4 < 0.0 && macd4Avg < 0.0;

            int bullishCount = (tf1Bullish ? 1 : 0) + (tf2Bullish ? 1 : 0)
                             + (tf3Bullish ? 1 : 0) + (tf4Bullish ? 1 : 0);
            int bearishCount = (tf1Bearish ? 1 : 0) + (tf2Bearish ? 1 : 0)
                             + (tf3Bearish ? 1 : 0) + (tf4Bearish ? 1 : 0);

            // ── Bar color / candle signal ─────────────────────────────────────────
            Brush  barColor     = DefaultBarColor;
            double candleSignal = 0.0;
            bool   isGreyBar    = true;   // assume grey unless overridden below

            if (bullishCount == 4)
            {
                barColor     = AllFourBullishColor;
                candleSignal = 2.0;
                isGreyBar    = false;
            }
            else if (bearishCount == 4)
            {
                barColor     = AllFourBearishColor;
                candleSignal = -2.0;
                isGreyBar    = false;
            }
            else if (bullishCount == 3)
            {
                barColor     = ThreeBullishColor;
                candleSignal = 1.0;
                isGreyBar    = false;
            }
            else if (bearishCount == 3)
            {
                barColor     = ThreeBearishColor;
                candleSignal = -1.0;
                isGreyBar    = false;
            }

            Values[0][0] = candleSignal;

            // ── EMA cloud values ──────────────────────────────────────────────────
            if (CurrentBars[0] >= 9)
            {
                Values[2][0] = ema5[0];
                Values[3][0] = ema9[0];
            }

            // ── Maverick200 ───────────────────────────────────────────────────────
            if (!Show200EMA)
            {
                Values[4][0] = double.NaN;
            }
            else if (CurrentBars[0] >= 200)
            {
                Values[4][0]          = ema200[0];
                PlotBrushes[4][0]     = Close[0] > ema200[0] ? EMA200AboveColor : EMA200BelowColor;
            }

            // ── MaverickGuide ─────────────────────────────────────────────────────
            if (!ShowMaverickGuide)
            {
                Values[5][0] = double.NaN;
            }
            else if (CurrentBars[0] >= Math.Max(VMAPeriod, VMAVolatilityPeriod))
            {
                Values[5][0]      = vma[0];
                PlotBrushes[5][0] = MaverickGuideColor;
                Plots[5].Pen      = new Pen(MaverickGuideColor, MaverickGuideThickness);
            }

            // ── Bar coloring ──────────────────────────────────────────────────────
            BarBrushes[0]          = barColor;
            CandleOutlineBrushes[0] = barColor;

            // ── Background trend ──────────────────────────────────────────────────
            if (!ShowBackgroundTrend)
            {
                BackBrush = null;
            }
            else
            {
                bool? isTrendUp = null;

                if (QuickTrendBackground && CurrentBars[0] >= 9)
                    isTrendUp = ema5[0] > ema9[0];
                else if (MaverickGuideBackground && CurrentBars[0] >= Math.Max(VMAPeriod, VMAVolatilityPeriod))
                    isTrendUp = Close[0] > vma[0];

                if (isTrendUp.HasValue)
                    BackBrush = isTrendUp.Value ? cachedBgUpBrush : cachedBgDownBrush;
            }

            // ════════════════════════════════════════════════════════════════════
            //  POST-CONSOLIDATION SEQUENCE STATE MACHINE
            //
            //  Sequence: Grey(≥min) → [optional Directional 3/4] → Momentum 4/4
            //
            //  States:
            //    Idle (0)        : grey bar count < MinConsolidationBars
            //    Armed (1)       : enough grey bars seen, waiting for non-grey
            //    Directional (2) : one or more 3/4 bars seen, direction locked
            //
            //  Rules:
            //    • Grey bar always increments consecutiveGreyBars.
            //    • Reaching MinConsolidationBars on a grey bar → state = Armed.
            //    • Going back to grey while Armed/Directional keeps state Armed
            //      (re-consolidation is fine, direction lock is cleared back to 0).
            //    • First non-grey bar sets sequenceDirection; a flip at any point
            //      resets state to Idle and direction to 0.
            //    • 3/4 bar while Armed/Directional → state = Directional.
            //    • 4/4 bar while Armed or Directional, matching sequenceDirection
            //      (or direction not yet set) → FIRE arrow, then reset to Idle.
            // ════════════════════════════════════════════════════════════════════
            double dotSignal = 0.0;

            // Classify current bar
            bool isFullBullish        = (bullishCount == 4);
            bool isFullBearish        = (bearishCount == 4);
            bool isDirectionalBullish = (bullishCount == 3);
            bool isDirectionalBearish = (bearishCount == 3);
            int  currentDirection     = isFullBullish || isDirectionalBullish ?  1
                                      : isFullBearish || isDirectionalBearish ? -1
                                      : 0; // grey = 0

            // ── State transitions ─────────────────────────────────────────────
            if (isGreyBar)
            {
                consecutiveGreyBars++;

                if (consecutiveGreyBars >= MinConsolidationBars)
                {
                    // Enough consolidation — arm (or stay armed).
                    // If we were Directional, going grey clears the direction lock
                    // so a fresh directional phase can begin.
                    if (sequenceState == 2)
                        sequenceDirection = 0;
                    sequenceState = 1; // Armed
                }
                // else stay Idle, keep counting
            }
            else
            {
                // Non-grey bar — check for direction flip
                if (sequenceDirection != 0 && currentDirection != 0 && currentDirection != sequenceDirection)
                {
                    // Direction flipped — full reset
                    sequenceState     = 0;
                    sequenceDirection = 0;
                    consecutiveGreyBars = 0;
                }
                else if (sequenceState >= 1) // Armed or Directional
                {
                    // Lock direction if not yet set
                    if (sequenceDirection == 0 && currentDirection != 0)
                        sequenceDirection = currentDirection;

                    if (isFullBullish || isFullBearish)
                    {
                        // MOMENTUM bar — fire arrow if direction matches (or undecided)
                        bool directionOk = (sequenceDirection == 0)
                                        || (isFullBullish && sequenceDirection == 1)
                                        || (isFullBearish && sequenceDirection == -1);

                        if (ShowDots && directionOk)
                        {
                            RemoveDrawObject("BullishArrow" + CurrentBar);
                            RemoveDrawObject("BearishArrow" + CurrentBar);

                            if (isFullBullish)
                            {
                                Draw.ArrowUp(this, "BullishArrow" + CurrentBar, false, 0,
                                             Low[0] - 2.0 * TickSize, BullishDotColor);
                                dotSignal = 1.0;
                            }
                            else
                            {
                                Draw.ArrowDown(this, "BearishArrow" + CurrentBar, false, 0,
                                               High[0] + 2.0 * TickSize, BearishDotColor);
                                dotSignal = -1.0;
                            }
                        }

                        // Reset after firing (or after a mismatched momentum bar)
                        sequenceState       = 0;
                        sequenceDirection   = 0;
                        consecutiveGreyBars = 0;
                    }
                    else if (isDirectionalBullish || isDirectionalBearish)
                    {
                        // Directional bar — advance to Directional state
                        sequenceState = 2;
                    }
                    else
                    {
                        // Shouldn't happen, but guard: unknown bar type, reset
                        sequenceState       = 0;
                        sequenceDirection   = 0;
                        consecutiveGreyBars = 0;
                    }
                }
                else
                {
                    // Idle and non-grey — reset grey counter, stay Idle
                    consecutiveGreyBars = 0;
                }
            }

            // ── DotSignal plot – Predator reads Values[1] ─────────────────────────
            Values[1][0] = dotSignal;
        }

        // ════════════════════════════════════════════════════════════════════════
        //  OnRender – EMA Cloud (unchanged from original)
        // ════════════════════════════════════════════════════════════════════════
        protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
        {
            base.OnRender(chartControl, chartScale);

            if (!ShowEmaCloud || Bars == null || chartControl == null || CurrentBars[0] < 9)
                return;

            int firstBarIdx = Math.Max(0, ChartBars.GetBarIdxByX(chartControl, 0));
            int lastBarIdx  = Math.Min(CurrentBars[0] - 1,
                                ChartBars.GetBarIdxByX(chartControl, (int)chartControl.ActualWidth));

            if (firstBarIdx > lastBarIdx || lastBarIdx < 9)
                return;

            bool? isEma5Above  = null;
            int   segmentStart = firstBarIdx;

            for (int i = firstBarIdx; i <= lastBarIdx; i++)
            {
                double ema5Value = Values[2].GetValueAt(i);
                double ema9Value = Values[3].GetValueAt(i);
                bool   currAbove = ema5Value > ema9Value;

                if (isEma5Above.HasValue)
                {
                    if (isEma5Above != currAbove || i == lastBarIdx)
                    {
                        int endBar = (i == lastBarIdx && isEma5Above == currAbove) ? i : (i - 1);
                        DrawCloudSegment(chartControl, chartScale, segmentStart, endBar, isEma5Above.Value);
                        isEma5Above  = currAbove;
                        segmentStart = i;
                    }
                }
                else
                {
                    isEma5Above  = currAbove;
                    segmentStart = i;
                }
            }

            if (segmentStart <= lastBarIdx)
                DrawCloudSegment(chartControl, chartScale, segmentStart, lastBarIdx, isEma5Above.Value);
        }

        private void DrawCloudSegment(ChartControl chartControl, ChartScale chartScale,
                                       int startBar, int endBar, bool isEma5Above)
        {
            if (startBar > endBar) return;

            Color color;
            if (isEma5Above)
            {
                SolidColorBrush b = (SolidColorBrush)EmaCloudUpColor;
                color = Color.FromArgb((byte)(EmaCloudOpacity * 255 / 100), b.Color.R, b.Color.G, b.Color.B);
            }
            else
            {
                SolidColorBrush b = (SolidColorBrush)EmaCloudDownColor;
                color = Color.FromArgb((byte)(EmaCloudOpacity * 255 / 100), b.Color.R, b.Color.G, b.Color.B);
            }

            DXSolidColorBrush fillBrush = new DXSolidColorBrush(RenderTarget,
                new SharpDX.Color(color.R, color.G, color.B, color.A));

            SharpDX.Direct2D1.PathGeometry pg   = new SharpDX.Direct2D1.PathGeometry(Globals.D2DFactory);
            GeometrySink                   sink = pg.Open();

            double ema5Val = Values[2].GetValueAt(startBar);
            float  x = chartControl.GetXByBarIndex(ChartBars, startBar);
            float  y = chartScale.GetYByValue(ema5Val);
            sink.BeginFigure(new Vector2(x, y), FigureBegin.Filled);

            for (int i = startBar + 1; i <= endBar; i++)
            {
                ema5Val = Values[2].GetValueAt(i);
                x = chartControl.GetXByBarIndex(ChartBars, i);
                y = chartScale.GetYByValue(ema5Val);
                sink.AddLine(new Vector2(x, y));
            }

            for (int j = endBar; j >= startBar; j--)
            {
                double ema9Val = Values[3].GetValueAt(j);
                x = chartControl.GetXByBarIndex(ChartBars, j);
                y = chartScale.GetYByValue(ema9Val);
                sink.AddLine(new Vector2(x, y));
            }

            sink.EndFigure(FigureEnd.Closed);
            sink.Close();
            RenderTarget.FillGeometry(pg, fillBrush);
            pg.Dispose();
            sink.Dispose();
            fillBrush.Dispose();
        }

        // ════════════════════════════════════════════════════════════════════════
        //  Helpers
        // ════════════════════════════════════════════════════════════════════════
        private Brush CreateFrozenBrush(Brush baseBrush, int opacity)
        {
            SolidColorBrush src = (SolidColorBrush)baseBrush;
            SolidColorBrush b   = new SolidColorBrush(Color.FromArgb(
                (byte)(opacity * 255 / 100), src.Color.R, src.Color.G, src.Color.B));
            b.Freeze();
            return b;
        }

        private void CreateTrendToggleButton()
        {
            if (ChartControl == null || isButtonCreated) return;
            ChartControl.Dispatcher.InvokeAsync(() =>
            {
                trendToggleButton = new System.Windows.Controls.Button
                {
                    Name             = "TrendToggleButton",
                    Content          = ShowBackgroundTrend ? "Trend On" : "Trend Off",
                    FontSize         = 11.0,
                    FontWeight       = FontWeights.Bold,
                    Foreground       = Brushes.White,
                    Background       = ShowBackgroundTrend ? Brushes.Black : Brushes.DarkGray,
                    BorderBrush      = Brushes.Black,
                    BorderThickness  = new Thickness(1.0),
                    Margin           = new Thickness(5.0),
                    Padding          = new Thickness(8.0, 4.0, 8.0, 4.0),
                    Width            = 80.0,
                    Height           = 25.0,
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
                    VerticalAlignment   = VerticalAlignment.Bottom
                };
                Canvas.SetLeft(trendToggleButton, 10.0);
                Canvas.SetBottom(trendToggleButton, 10.0);
                trendToggleButton.Click += OnTrendToggleButtonClick;
                UserControlCollection.Add(trendToggleButton);
                isButtonCreated = true;
            });
        }

        private void OnTrendToggleButtonClick(object sender, RoutedEventArgs e)
        {
            ShowBackgroundTrend = !ShowBackgroundTrend;
            if (trendToggleButton != null)
            {
                trendToggleButton.Content    = ShowBackgroundTrend ? "Trend On" : "Trend Off";
                trendToggleButton.Background = ShowBackgroundTrend ? Brushes.Black : Brushes.DarkGray;
            }
            ChartControl?.Dispatcher.InvokeAsync(() => SendKeys.SendWait("{F5}"));
        }
    }
}


#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private MaverickIndicators.MaverickAIpro_PostConsolidation[] cacheMaverickAIpro_PostConsolidation;
		public MaverickIndicators.MaverickAIpro_PostConsolidation MaverickAIpro_PostConsolidation(int higherTimeframe1Period, BarsPeriodType higherTimeframe1Type, int higherTimeframe2Period, BarsPeriodType higherTimeframe2Type, int higherTimeframe3Period, BarsPeriodType higherTimeframe3Type, int higherTimeframe4Period, BarsPeriodType higherTimeframe4Type, Brush allFourBullishColor, Brush allFourBearishColor, Brush threeBullishColor, Brush threeBearishColor, Brush defaultBarColor, bool showDots, int minConsolidationBars, Brush bullishDotColor, Brush bearishDotColor, bool showBackgroundTrend, bool quickTrendBackground, bool maverickGuideBackground, Brush backgroundTrendUpColor, Brush backgroundTrendDownColor, int backgroundOpacity, bool showEmaCloud, Brush emaCloudUpColor, Brush emaCloudDownColor, int emaCloudOpacity, bool show200EMA, Brush eMA200AboveColor, Brush eMA200BelowColor, bool showMaverickGuide, Brush maverickGuideColor, int maverickGuideThickness)
		{
			return MaverickAIpro_PostConsolidation(Input, higherTimeframe1Period, higherTimeframe1Type, higherTimeframe2Period, higherTimeframe2Type, higherTimeframe3Period, higherTimeframe3Type, higherTimeframe4Period, higherTimeframe4Type, allFourBullishColor, allFourBearishColor, threeBullishColor, threeBearishColor, defaultBarColor, showDots, minConsolidationBars, bullishDotColor, bearishDotColor, showBackgroundTrend, quickTrendBackground, maverickGuideBackground, backgroundTrendUpColor, backgroundTrendDownColor, backgroundOpacity, showEmaCloud, emaCloudUpColor, emaCloudDownColor, emaCloudOpacity, show200EMA, eMA200AboveColor, eMA200BelowColor, showMaverickGuide, maverickGuideColor, maverickGuideThickness);
		}

		public MaverickIndicators.MaverickAIpro_PostConsolidation MaverickAIpro_PostConsolidation(ISeries<double> input, int higherTimeframe1Period, BarsPeriodType higherTimeframe1Type, int higherTimeframe2Period, BarsPeriodType higherTimeframe2Type, int higherTimeframe3Period, BarsPeriodType higherTimeframe3Type, int higherTimeframe4Period, BarsPeriodType higherTimeframe4Type, Brush allFourBullishColor, Brush allFourBearishColor, Brush threeBullishColor, Brush threeBearishColor, Brush defaultBarColor, bool showDots, int minConsolidationBars, Brush bullishDotColor, Brush bearishDotColor, bool showBackgroundTrend, bool quickTrendBackground, bool maverickGuideBackground, Brush backgroundTrendUpColor, Brush backgroundTrendDownColor, int backgroundOpacity, bool showEmaCloud, Brush emaCloudUpColor, Brush emaCloudDownColor, int emaCloudOpacity, bool show200EMA, Brush eMA200AboveColor, Brush eMA200BelowColor, bool showMaverickGuide, Brush maverickGuideColor, int maverickGuideThickness)
		{
			if (cacheMaverickAIpro_PostConsolidation != null)
				for (int idx = 0; idx < cacheMaverickAIpro_PostConsolidation.Length; idx++)
					if (cacheMaverickAIpro_PostConsolidation[idx] != null && cacheMaverickAIpro_PostConsolidation[idx].HigherTimeframe1Period == higherTimeframe1Period && cacheMaverickAIpro_PostConsolidation[idx].HigherTimeframe1Type == higherTimeframe1Type && cacheMaverickAIpro_PostConsolidation[idx].HigherTimeframe2Period == higherTimeframe2Period && cacheMaverickAIpro_PostConsolidation[idx].HigherTimeframe2Type == higherTimeframe2Type && cacheMaverickAIpro_PostConsolidation[idx].HigherTimeframe3Period == higherTimeframe3Period && cacheMaverickAIpro_PostConsolidation[idx].HigherTimeframe3Type == higherTimeframe3Type && cacheMaverickAIpro_PostConsolidation[idx].HigherTimeframe4Period == higherTimeframe4Period && cacheMaverickAIpro_PostConsolidation[idx].HigherTimeframe4Type == higherTimeframe4Type && cacheMaverickAIpro_PostConsolidation[idx].AllFourBullishColor == allFourBullishColor && cacheMaverickAIpro_PostConsolidation[idx].AllFourBearishColor == allFourBearishColor && cacheMaverickAIpro_PostConsolidation[idx].ThreeBullishColor == threeBullishColor && cacheMaverickAIpro_PostConsolidation[idx].ThreeBearishColor == threeBearishColor && cacheMaverickAIpro_PostConsolidation[idx].DefaultBarColor == defaultBarColor && cacheMaverickAIpro_PostConsolidation[idx].ShowDots == showDots && cacheMaverickAIpro_PostConsolidation[idx].MinConsolidationBars == minConsolidationBars && cacheMaverickAIpro_PostConsolidation[idx].BullishDotColor == bullishDotColor && cacheMaverickAIpro_PostConsolidation[idx].BearishDotColor == bearishDotColor && cacheMaverickAIpro_PostConsolidation[idx].ShowBackgroundTrend == showBackgroundTrend && cacheMaverickAIpro_PostConsolidation[idx].QuickTrendBackground == quickTrendBackground && cacheMaverickAIpro_PostConsolidation[idx].MaverickGuideBackground == maverickGuideBackground && cacheMaverickAIpro_PostConsolidation[idx].BackgroundTrendUpColor == backgroundTrendUpColor && cacheMaverickAIpro_PostConsolidation[idx].BackgroundTrendDownColor == backgroundTrendDownColor && cacheMaverickAIpro_PostConsolidation[idx].BackgroundOpacity == backgroundOpacity && cacheMaverickAIpro_PostConsolidation[idx].ShowEmaCloud == showEmaCloud && cacheMaverickAIpro_PostConsolidation[idx].EmaCloudUpColor == emaCloudUpColor && cacheMaverickAIpro_PostConsolidation[idx].EmaCloudDownColor == emaCloudDownColor && cacheMaverickAIpro_PostConsolidation[idx].EmaCloudOpacity == emaCloudOpacity && cacheMaverickAIpro_PostConsolidation[idx].Show200EMA == show200EMA && cacheMaverickAIpro_PostConsolidation[idx].EMA200AboveColor == eMA200AboveColor && cacheMaverickAIpro_PostConsolidation[idx].EMA200BelowColor == eMA200BelowColor && cacheMaverickAIpro_PostConsolidation[idx].ShowMaverickGuide == showMaverickGuide && cacheMaverickAIpro_PostConsolidation[idx].MaverickGuideColor == maverickGuideColor && cacheMaverickAIpro_PostConsolidation[idx].MaverickGuideThickness == maverickGuideThickness && cacheMaverickAIpro_PostConsolidation[idx].EqualsInput(input))
						return cacheMaverickAIpro_PostConsolidation[idx];
			return CacheIndicator<MaverickIndicators.MaverickAIpro_PostConsolidation>(new MaverickIndicators.MaverickAIpro_PostConsolidation(){ HigherTimeframe1Period = higherTimeframe1Period, HigherTimeframe1Type = higherTimeframe1Type, HigherTimeframe2Period = higherTimeframe2Period, HigherTimeframe2Type = higherTimeframe2Type, HigherTimeframe3Period = higherTimeframe3Period, HigherTimeframe3Type = higherTimeframe3Type, HigherTimeframe4Period = higherTimeframe4Period, HigherTimeframe4Type = higherTimeframe4Type, AllFourBullishColor = allFourBullishColor, AllFourBearishColor = allFourBearishColor, ThreeBullishColor = threeBullishColor, ThreeBearishColor = threeBearishColor, DefaultBarColor = defaultBarColor, ShowDots = showDots, MinConsolidationBars = minConsolidationBars, BullishDotColor = bullishDotColor, BearishDotColor = bearishDotColor, ShowBackgroundTrend = showBackgroundTrend, QuickTrendBackground = quickTrendBackground, MaverickGuideBackground = maverickGuideBackground, BackgroundTrendUpColor = backgroundTrendUpColor, BackgroundTrendDownColor = backgroundTrendDownColor, BackgroundOpacity = backgroundOpacity, ShowEmaCloud = showEmaCloud, EmaCloudUpColor = emaCloudUpColor, EmaCloudDownColor = emaCloudDownColor, EmaCloudOpacity = emaCloudOpacity, Show200EMA = show200EMA, EMA200AboveColor = eMA200AboveColor, EMA200BelowColor = eMA200BelowColor, ShowMaverickGuide = showMaverickGuide, MaverickGuideColor = maverickGuideColor, MaverickGuideThickness = maverickGuideThickness }, input, ref cacheMaverickAIpro_PostConsolidation);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.MaverickIndicators.MaverickAIpro_PostConsolidation MaverickAIpro_PostConsolidation(int higherTimeframe1Period, BarsPeriodType higherTimeframe1Type, int higherTimeframe2Period, BarsPeriodType higherTimeframe2Type, int higherTimeframe3Period, BarsPeriodType higherTimeframe3Type, int higherTimeframe4Period, BarsPeriodType higherTimeframe4Type, Brush allFourBullishColor, Brush allFourBearishColor, Brush threeBullishColor, Brush threeBearishColor, Brush defaultBarColor, bool showDots, int minConsolidationBars, Brush bullishDotColor, Brush bearishDotColor, bool showBackgroundTrend, bool quickTrendBackground, bool maverickGuideBackground, Brush backgroundTrendUpColor, Brush backgroundTrendDownColor, int backgroundOpacity, bool showEmaCloud, Brush emaCloudUpColor, Brush emaCloudDownColor, int emaCloudOpacity, bool show200EMA, Brush eMA200AboveColor, Brush eMA200BelowColor, bool showMaverickGuide, Brush maverickGuideColor, int maverickGuideThickness)
		{
			return indicator.MaverickAIpro_PostConsolidation(Input, higherTimeframe1Period, higherTimeframe1Type, higherTimeframe2Period, higherTimeframe2Type, higherTimeframe3Period, higherTimeframe3Type, higherTimeframe4Period, higherTimeframe4Type, allFourBullishColor, allFourBearishColor, threeBullishColor, threeBearishColor, defaultBarColor, showDots, minConsolidationBars, bullishDotColor, bearishDotColor, showBackgroundTrend, quickTrendBackground, maverickGuideBackground, backgroundTrendUpColor, backgroundTrendDownColor, backgroundOpacity, showEmaCloud, emaCloudUpColor, emaCloudDownColor, emaCloudOpacity, show200EMA, eMA200AboveColor, eMA200BelowColor, showMaverickGuide, maverickGuideColor, maverickGuideThickness);
		}

		public Indicators.MaverickIndicators.MaverickAIpro_PostConsolidation MaverickAIpro_PostConsolidation(ISeries<double> input , int higherTimeframe1Period, BarsPeriodType higherTimeframe1Type, int higherTimeframe2Period, BarsPeriodType higherTimeframe2Type, int higherTimeframe3Period, BarsPeriodType higherTimeframe3Type, int higherTimeframe4Period, BarsPeriodType higherTimeframe4Type, Brush allFourBullishColor, Brush allFourBearishColor, Brush threeBullishColor, Brush threeBearishColor, Brush defaultBarColor, bool showDots, int minConsolidationBars, Brush bullishDotColor, Brush bearishDotColor, bool showBackgroundTrend, bool quickTrendBackground, bool maverickGuideBackground, Brush backgroundTrendUpColor, Brush backgroundTrendDownColor, int backgroundOpacity, bool showEmaCloud, Brush emaCloudUpColor, Brush emaCloudDownColor, int emaCloudOpacity, bool show200EMA, Brush eMA200AboveColor, Brush eMA200BelowColor, bool showMaverickGuide, Brush maverickGuideColor, int maverickGuideThickness)
		{
			return indicator.MaverickAIpro_PostConsolidation(input, higherTimeframe1Period, higherTimeframe1Type, higherTimeframe2Period, higherTimeframe2Type, higherTimeframe3Period, higherTimeframe3Type, higherTimeframe4Period, higherTimeframe4Type, allFourBullishColor, allFourBearishColor, threeBullishColor, threeBearishColor, defaultBarColor, showDots, minConsolidationBars, bullishDotColor, bearishDotColor, showBackgroundTrend, quickTrendBackground, maverickGuideBackground, backgroundTrendUpColor, backgroundTrendDownColor, backgroundOpacity, showEmaCloud, emaCloudUpColor, emaCloudDownColor, emaCloudOpacity, show200EMA, eMA200AboveColor, eMA200BelowColor, showMaverickGuide, maverickGuideColor, maverickGuideThickness);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.MaverickIndicators.MaverickAIpro_PostConsolidation MaverickAIpro_PostConsolidation(int higherTimeframe1Period, BarsPeriodType higherTimeframe1Type, int higherTimeframe2Period, BarsPeriodType higherTimeframe2Type, int higherTimeframe3Period, BarsPeriodType higherTimeframe3Type, int higherTimeframe4Period, BarsPeriodType higherTimeframe4Type, Brush allFourBullishColor, Brush allFourBearishColor, Brush threeBullishColor, Brush threeBearishColor, Brush defaultBarColor, bool showDots, int minConsolidationBars, Brush bullishDotColor, Brush bearishDotColor, bool showBackgroundTrend, bool quickTrendBackground, bool maverickGuideBackground, Brush backgroundTrendUpColor, Brush backgroundTrendDownColor, int backgroundOpacity, bool showEmaCloud, Brush emaCloudUpColor, Brush emaCloudDownColor, int emaCloudOpacity, bool show200EMA, Brush eMA200AboveColor, Brush eMA200BelowColor, bool showMaverickGuide, Brush maverickGuideColor, int maverickGuideThickness)
		{
			return indicator.MaverickAIpro_PostConsolidation(Input, higherTimeframe1Period, higherTimeframe1Type, higherTimeframe2Period, higherTimeframe2Type, higherTimeframe3Period, higherTimeframe3Type, higherTimeframe4Period, higherTimeframe4Type, allFourBullishColor, allFourBearishColor, threeBullishColor, threeBearishColor, defaultBarColor, showDots, minConsolidationBars, bullishDotColor, bearishDotColor, showBackgroundTrend, quickTrendBackground, maverickGuideBackground, backgroundTrendUpColor, backgroundTrendDownColor, backgroundOpacity, showEmaCloud, emaCloudUpColor, emaCloudDownColor, emaCloudOpacity, show200EMA, eMA200AboveColor, eMA200BelowColor, showMaverickGuide, maverickGuideColor, maverickGuideThickness);
		}

		public Indicators.MaverickIndicators.MaverickAIpro_PostConsolidation MaverickAIpro_PostConsolidation(ISeries<double> input , int higherTimeframe1Period, BarsPeriodType higherTimeframe1Type, int higherTimeframe2Period, BarsPeriodType higherTimeframe2Type, int higherTimeframe3Period, BarsPeriodType higherTimeframe3Type, int higherTimeframe4Period, BarsPeriodType higherTimeframe4Type, Brush allFourBullishColor, Brush allFourBearishColor, Brush threeBullishColor, Brush threeBearishColor, Brush defaultBarColor, bool showDots, int minConsolidationBars, Brush bullishDotColor, Brush bearishDotColor, bool showBackgroundTrend, bool quickTrendBackground, bool maverickGuideBackground, Brush backgroundTrendUpColor, Brush backgroundTrendDownColor, int backgroundOpacity, bool showEmaCloud, Brush emaCloudUpColor, Brush emaCloudDownColor, int emaCloudOpacity, bool show200EMA, Brush eMA200AboveColor, Brush eMA200BelowColor, bool showMaverickGuide, Brush maverickGuideColor, int maverickGuideThickness)
		{
			return indicator.MaverickAIpro_PostConsolidation(input, higherTimeframe1Period, higherTimeframe1Type, higherTimeframe2Period, higherTimeframe2Type, higherTimeframe3Period, higherTimeframe3Type, higherTimeframe4Period, higherTimeframe4Type, allFourBullishColor, allFourBearishColor, threeBullishColor, threeBearishColor, defaultBarColor, showDots, minConsolidationBars, bullishDotColor, bearishDotColor, showBackgroundTrend, quickTrendBackground, maverickGuideBackground, backgroundTrendUpColor, backgroundTrendDownColor, backgroundOpacity, showEmaCloud, emaCloudUpColor, emaCloudDownColor, emaCloudOpacity, show200EMA, eMA200AboveColor, eMA200BelowColor, showMaverickGuide, maverickGuideColor, maverickGuideThickness);
		}
	}
}

#endregion
