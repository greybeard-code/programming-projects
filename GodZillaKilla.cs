#region Using declarations
using NinjaTrader.Cbi;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.Data;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.SuperDom;
using NinjaTrader.Gui.Tools;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.DrawingTools;
using NinjaTrader.NinjaScript.Indicators;
using NinjaTrader.NinjaScript.Indicators.GreyBeard.KingPanaZilla;
using SharpDX;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Serialization;
// SharpDX collides with WPF on Brush/Color/FontWeight/FontStyle - alias per AGENTS.md
using Brush = System.Windows.Media.Brush;
using Color = System.Windows.Media.Color;
using FontStyle = SharpDX.DirectWrite.FontStyle;
using FontWeight = SharpDX.DirectWrite.FontWeight;
using NewsPrintLocation = NinjaTrader.NinjaScript.Indicators.Playr101.NewsSignals.NewsPrintLocation;
#endregion

//This namespace holds Strategies in this folder and is required. Do not change it.
namespace NinjaTrader.NinjaScript.Strategies.Playr101
{
    #region GUI Categories
    [CategoryOrder ("Strategy Information", 0)]
    [CategoryOrder ("ATM Parameters", 1)]
    [CategoryOrder ("Signals", 2)]
    [CategoryOrder ("Filters", 4)]
    [CategoryOrder ("Risk Management", 5)]
    [CategoryOrder ("Session Parameters", 6)]
    [CategoryOrder ("Indicator Settings", 7)]
    [CategoryOrder ("Indicator: KingOrderBlock", 8)]
    [CategoryOrder ("Indicator: PANAKanal", 9)]
    [CategoryOrder ("Indicator: ThunderZilla", 10)]
    [CategoryOrder ("Indicator: SuperJumpBoost", 11)]
    [CategoryOrder ("Indicator: SumoPullback", 12)]
    [CategoryOrder ("Display", 13)]
    [CategoryOrder ("Logging", 14)]
    #endregion

    public class GodZillaKilla : Strategy, ICustomTypeDescriptor
    {
        public override string DisplayName => Name;

        // Enums
        public enum HudCorner
        {
            TopLeft,
            TopRight,
            BottomLeft,
            BottomRight,
            Center,
            Hidden        // alias for "do not render"; kept here so the dropdown shows it
        }

        // Same Tiny/Small/Normal/Large/Huge ladder LiquidityDeltaProfiler uses for its
        // dashboard. Drives both font size AND box width (per-preset table) so the box
        // tightens with the text — no more empty real-estate to the right of the panel.
        public enum GodZillaHudSize
        {
            Tiny,
            Small,
            Normal,
            Large,
            Huge
        }

        public enum OrderManagementMode
        {
            AtmStrategy,
            FixedTicks
        }

        #region Variables
        // Drawing
        private SimpleFont title = new SimpleFont("Agency Fb", 16) { Size = 20, Bold = true };
        private SimpleFont signalArrowFont = new SimpleFont("Arial", 10) { Bold = true };

        // ATM
        private string atmStrategyId = string.Empty;
        private string orderId = string.Empty;
        private bool isAtmStrategyCreated = false;

        // Fixed PT/SL order mode
        private string fixedEntrySignalName = string.Empty;
        private MarketPosition fixedEntryDirection = MarketPosition.Flat;
        private double fixedEntryAvgPrice = 0.0;
        private int fixedEntryQty = 0;
        private bool fixedPositionConfirmed = false;
        private bool fixedTradeCloseProcessed = false;
        private string fixedMartingaleSignalName = string.Empty;
        private MarketPosition fixedMartingaleDirection = MarketPosition.Flat;
        private double fixedMartingaleEntryAvgPrice = 0.0;
        private int fixedMartingaleEntryQty = 0;
        private bool fixedMartingalePositionConfirmed = false;
        private bool fixedMartingaleCloseProcessed = false;
        private bool fixedBreakevenMoved = false;
        private bool fixedMartingaleBreakevenMoved = false;
        private double lastFixedExitCandidatePrice = 0.0;
        private DateTime lastFixedExitCandidateTime = Core.Globals.MinDate;

        // Martingale ATM recovery
        private string martingaleAtmStrategyId = string.Empty;
        private string martingaleOrderId = string.Empty;
        private bool martingaleAtmStrategyCreated = false;
        private bool martingalePositionConfirmed = false;
        private bool martingaleRecoveryActive = false;
        private MarketPosition martingaleRecoveryDirection = MarketPosition.Flat;
        private double martingaleLastRealizedPnL = 0.0;

        // Reverse-on-opposite-signal
        private bool pendingReverseActive = false;
        private MarketPosition pendingReverseDirection = MarketPosition.Flat;
        private bool pendingReverseUsesKO = false;
        private bool pendingReverseUsesPA = false;
        private bool pendingReverseUsesTH = false;
        private bool pendingReverseUsesSJ = false;
        private bool pendingReverseUsesSU = false;
        private int pendingReverseGroupSize = 0;
        private string pendingReverseGroupName = string.Empty;

        // Internal tracker
        private double dailyRealizedPnL = 0.0;
        private double dailyUnrealizedPnL = 0.0;
        private double totalRealizedPnL = 0.0;
        private double sessionStartTotalRealizedPnL = 0.0;
        private double totalRunningPnL = 0.0;
        private double lastAtmRealizedPnL = 0.0;
        private bool dailyLimitHit = false;
        private string dailyPnlStatusMessage = string.Empty;
        private DateTime lastFixedStateSanityCheckUtc = DateTime.MinValue;
        private const int FixedStateSanityCheckSeconds = 5;
        private DateTime lastPnlSessionDate = Core.Globals.MinDate;
        private bool pendingSessionPnlPrint = false;
        private string pendingSessionPnlPrintLabel = string.Empty;
        private DateTime pendingSessionPnlPrintTime = Core.Globals.MinDate;
        private string lastSessionPnlPrintKey = string.Empty;

        // Naked-position watchdog (wall-clock throttled — playback time is unreliable in fast-forward)
        private DateTime lastNakedCheckUtc = DateTime.MinValue;
        private const int NakedCheckIntervalSeconds = 30;

        // Wall-clock throttle for tick-series UI work. Without this, fast-forward
        // playback drives OnBarUpdate(BIP=1) hundreds of times per wall-second and
        // the dispatcher queue grows faster than the UI thread can drain it →
        // visible lockup ~3-4 minutes in. Limit-hit / PnL boundary checks still
        // fire every tick (they MUST), but UI snapshot+invalidate are throttled.
        private DateTime _lastTickUiSyncUtc = DateTime.MinValue;
        private const int TICK_UI_SYNC_INTERVAL_MS = 100;

        // Trade logging
        private StreamWriter _logWriter;
        private Dictionary<string, TradeRecord> _tradeMap = new Dictionary<string, TradeRecord>();
        private bool _atmPositionConfirmed = false;

        private class TradeRecord
        {
            public string Trigger;
            public string Direction;
            public DateTime OpenTime;
            public string Instrument;
            public double OpenPrice;
            public int Qty;
            public string AtmStrategyName;
        }

        // Signal tracking
        private class SignalTradeStats
        {
            public int TotalTrades;
            public int LongTrades;
            public int ShortTrades;
            public int Winners;
            public int Losers;
        }

        private class GroupTriggerResult
        {
            public bool Long;
            public bool Short;
            public int GroupSize;
            public string TriggerName;
            public bool UsesKO;
            public bool UsesPA;
            public bool UsesTH;
            public bool UsesSJ;
            public bool UsesSU;
        }

        private SignalTradeStats koStats = new SignalTradeStats();
        private SignalTradeStats paStats = new SignalTradeStats();
        private SignalTradeStats thStats = new SignalTradeStats();
        private SignalTradeStats suStats = new SignalTradeStats();
        private SignalTradeStats sjStats = new SignalTradeStats();
        private SignalTradeStats groupSet1Stats = new SignalTradeStats();
        private SignalTradeStats groupSet2Stats = new SignalTradeStats();

        private int activeTradeGroupSize = 0;
        private string activeTradeGroupName = string.Empty;
        private bool activeTradeUsesKO = false;
        private bool activeTradeUsesPA = false;
        private bool activeTradeUsesTH = false;
        private bool activeTradeUsesSU = false;
        private bool activeTradeUsesSJ = false;
        private MarketPosition activeTradeDirection = MarketPosition.Flat;
        private string lastTradeClosedSummary = string.Empty;

        private string _strategyVersion = "";
        private string Credits = "";

        // Indicators
        private gbKingOrderBlock _king;
        private gbPANAKanal _pana;
        private gbThunderZilla _thunder;
        private gbSumoPullback _sumo;
        private gbSuperJumpBoost _sjb;

        // Normalized signal series used by entries, tracking, and visuals
        private Series<double> _koSignalSeries;
        private Series<double> _paSignalSeries;
        private Series<double> _thSignalSeries;
        private Series<double> _sjSignalSeries;
        private Series<double> _suSignalSeries;

        // ---- HUD (SharpDX boxed dashboard, AlightenLite-style) ----
        // Snapshot fields written by data thread (BuildDashboardSnapshot) and
        // read by UI thread (OnRender). Strings are immutable so a torn read
        // gives stale-but-consistent text rather than corrupt frames.
        private string _hudTitle      = "GodZilla";
        private string _hudVersion    = "";
        private string _hudArm        = "";
        private string _hudSession    = "";
        private string _hudNews       = "";
        private string _hudPnl        = "";
        private string _hudPnlOpen    = "";
        private string _hudTargets    = "";
        private string _hudStatus     = "";
        private string _hudLastTrade  = "";
        private List<string> _hudSignalLines = new List<string>();
        private bool   _hudPnlPositive = true;
        private bool   _hudSessionOutside = false;
        private bool   _hudKillHit = false;
        private bool   _hudKillProfit = false;
        private bool   _hudNewsBlocked = false;
        private bool   _hudShowNews = false;
        private bool   _hudShowSignals = false;
        private string _hudKillBanner = "";

        // SharpDX brush + format (UI thread owned, recreated on RT change)
        private SharpDX.DirectWrite.TextFormat       _dashFormat;
        private SharpDX.DirectWrite.TextFormat       _dashTitleFormat;
        private SharpDX.Direct2D1.SolidColorBrush    _bTextWhite;
        private SharpDX.Direct2D1.SolidColorBrush    _bTextDim;
        private SharpDX.Direct2D1.SolidColorBrush    _bTextGreen;
        private SharpDX.Direct2D1.SolidColorBrush    _bTextRed;
        private SharpDX.Direct2D1.SolidColorBrush    _bTextYellow;
        private SharpDX.Direct2D1.SolidColorBrush    _bTextOrange;
        private SharpDX.Direct2D1.SolidColorBrush    _bTextCyan;
        private SharpDX.Direct2D1.SolidColorBrush    _bBackground;
        private SharpDX.Direct2D1.SolidColorBrush    _bBorder;
        private SharpDX.Direct2D1.SolidColorBrush    _bBorderHot;

        // Same-target-skip per AGENTS.md lifecycle defense rule
        // _dxInitialized must gate both the same-target-skip AND the lazy re-init
        // in OnRender — otherwise NT8 firing OnRenderTargetChanged with the same
        // RT during playback transitions can null brushes and the next OnRender's
        // lazy init may fail silently in its own try/catch, leaving the dashboard
        // dead for the rest of the session (per feedback_nt8_lifecycle_defense.md).
        private SharpDX.Direct2D1.RenderTarget _lastSeenRenderTarget;
        private bool _dxInitialized;
        private int _hudErrors;
        private bool _hudFirstRenderLogged;

        // Tracks last-applied DashboardSize so EnsureDashboardFonts() can detect
        // a runtime size change and rebuild only the font formats (cheaper than
        // tearing down all brushes — they're size-independent).
        private GodZillaHudSize _lastSizeApplied = (GodZillaHudSize)(-1);

        // Throttled invalidate to avoid UI starvation in playback
        private DateTime _lastHudInvalidateUtc = DateTime.MinValue;
        private const int HUD_MIN_INVALIDATE_MS = 100;

        // EMA Filter
        private EMA _emaShortFilter;
        private EMA _emaLongFilter;

        // News Filter
        private NinjaTrader.NinjaScript.Indicators.Playr101.NewsSignals newsIndicator;
        private bool _lastNewsBlockActive = false;
        private bool _newsRuntimeDisabledPrinted = false;

        // Button Panel
        private Border _controlPanel;
        private Button _armLongBtn, _armShortBtn, _revBtn, _autoArmBtn, _closeBtn;
        private Label _statusLabel;
        private bool _uiInitialized = false;
        private bool _armLong = true;
        private bool _armShort = true;
        private bool _autoArm = true;
        private bool _reverseOnAlternateSignal = true;
        private volatile bool _strategyEnabled = true;

        // ATMPlotMarkers integration — ATM mode only
        private class MarkerEntryExitData
        {
            public int    EntryBar;
            public double EntryPrice;
            public int    ExitBar = -1;
            public double ExitPrice;
            public bool   IsLong;
            public bool   IsComplete;
            public string LineTag;
            public string EntryLabelTag;
            public string ExitLabelTag;
            public double InitialPosition;
            public double RemainingPosition;
        }

        private List<MarkerEntryExitData> _markerList = new List<MarkerEntryExitData>();
        private MarkerEntryExitData _markerCurrent;
        private int            _markerLineCounter;
        private Execution      _markerLastExecution;
        private double         _markerLastQty;
        private double         _markerCurrentQty;
        private MarketPosition _markerLastMP    = MarketPosition.Flat;
        private MarketPosition _markerCurrentMP = MarketPosition.Flat;
        private Account        _markerHookedAccount;
        private bool           _markerHooked;
        #endregion

        protected override void OnStateChange ()
        {
            if (State == State.SetDefaults)
            {
                Description = "GodZillaKilla — strategy using direct KingOrderBlock/PANAKanal/ThunderZilla/SuperJumpBoost/SumoPullback child indicator signals.";
                Name = "GodZillaKilla";
                StrategyName = Name;
                _strategyVersion = "1.4.0";

                Author = "Playr101";
                Credits = "GreyBeard, ninZa.co, RenkoKings, ES, rbro999";

                IsExitOnSessionCloseStrategy = true;
                ExitOnSessionCloseSeconds = 30;
                IsFillLimitOnTouch = true;
                MaximumBarsLookBack = MaximumBarsLookBack.TwoHundredFiftySix;
                OrderFillResolution = OrderFillResolution.Standard;
                Slippage = 0;
                StartBehavior = StartBehavior.WaitUntilFlat;
                TimeInForce = TimeInForce.Day;
                TraceOrders = true;
                RealtimeErrorHandling = RealtimeErrorHandling.IgnoreAllErrors;
                BarsRequiredToTrade = 2;
                IsInstantiatedOnEachOptimizationIteration = false;
                IsUnmanaged = false;
                IsAdoptAccountPositionAware = true;
                // NOTE: NT8 strategies continue trading while the chart tab is hidden
                // (only OnRender is paused — OnBarUpdate, position management, and ATM
                // calls keep firing on the data thread). No `IsSuspendedWhileInactive`
                // toggle exists on the Strategy class; the SharpDX HUD below never
                // gates trading because it only reads snapshot strings on the UI thread.

                //--------- User Configurable Parameters ---------//
                // ATM Strategy / Fixed PT-SL mode
                OrderMode = OrderManagementMode.AtmStrategy;
                AtmStrategy = string.Empty;
                MartingaleAtmStrategy = string.Empty;
                FixedOrderQuantity = 4;
                FixedStopLossTicks = 75;
                FixedProfitTargetTicks = 25;
                EnableFixedBreakeven = false;
                FixedBreakevenTriggerTicks = 25;
                FixedBreakevenOffsetTicks = 1;

                // Signal Usage
                // Display panels — defaults: both visible at the standard corners.
                // Set ShowDashboard = false (or DashboardPosition = Hidden) to disable
                // the SharpDX info panel entirely (stability mode — see AGENTS.md
                // gotcha #17). The control panel can also be turned off independently.
                ShowDashboard = true;
                DashboardPosition = HudCorner.BottomRight;
                DashboardSize = GodZillaHudSize.Normal;
                ShowControlPanel = true;
                ControlPanelPosition = HudCorner.TopLeft;

                EnableSignalTracking = true;
                UseKOSignals = false;
                KO_LongValue = 1;
                KO_ShortValue = -1;
                UsePASignals = true;
                PA_LongValue = 2;
                PA_ShortValue = -2;
                UseTHSignals = true;
                TH_LongValue = 2;
                TH_ShortValue = -2;
                UseSJSignals = true;
                SJ_LongValue = 1;
                SJ_ShortValue = -1;
                UseSUSignals = false;
                SU_LongValue = 1;
                SU_ShortValue = -1;

                // Same-bar group entry trigger sets
                GroupTriggerSet1RequiredCount = 1;

                // Optional second same-bar group trigger set
                EnableGroupTriggerSet2 = false;
                GroupTriggerSet2RequiredCount = 3;
                G2_UseKOSignals = false;
                G2_KO_LongValue = 1;
                G2_KO_ShortValue = -1;
                G2_UsePASignals = true;
                G2_PA_LongValue = 3;
                G2_PA_ShortValue = -3;
                G2_UseTHSignals = true;
                G2_TH_LongValue = 3;
                G2_TH_ShortValue = -3;
                G2_UseSJSignals = true;
                G2_SJ_LongValue = 1;
                G2_SJ_ShortValue = -1;
                G2_UseSUSignals = false;
                G2_SU_LongValue = 1;
                G2_SU_ShortValue = -1;

                // EMA Filter
                EnableEmaFilter = false;
                EmaShortPeriod = 21;
                EmaLongPeriod = 50;

                // Logging
                LogEnabled = false;
                EnableDebug = false;

                // Risk Defaults
                UseUnrealizedPnl = true;
                EnableDailyProfitTarget = false;
                DailyProfitTarget = 500;
                EnableDailyLossLimit = false;
                DailyLossLimit = 200;
                EnableMartingaleOnStopLoss = false;

                // News Filter Defaults
                EnableNewsFilter = false;
                NewsFlattenAtWarningTime = false;

                NewsShowDisplay = true;
                NewsDisplayLocation = NewsPrintLocation.TopRight;
                NewsDisplayXOffsetPixels = 20;
                NewsDisplayYOffsetPixels = 60;
                NewsUse24HourTime = false;
                NewsShowBackground = true;
                NewsShowTimeBackBrush = false;
                NewsTimeBackBrush = Brushes.DimGray;

                NewsUSOnlyEvents = true;
                NewsTodaysNewsOnly = true;
                NewsShowLowPriority = false;
                NewsMaxNewsItems = 10;
                NewsRefreshInterval = 15;

                NewsPreBlockMinutes = 5;
                NewsPostBlockMinutes = 5;
                NewsBlockHighImpact = true;
                NewsBlockMediumImpact = true;
                NewsBlockLowImpact = false;

                NewsSendAlerts = true;
                NewsAlertInterval = 15;
                NewsAlertWavFileName = "Alert1.wav";

                NewsDefaultTextColor = Brushes.White;
                NewsWarningTextColor = Brushes.Yellow;
                var newsBg = new System.Windows.Media.SolidColorBrush (System.Windows.Media.Color.FromArgb (170, 0, 0, 0));
                newsBg.Freeze ();
                NewsBackgroundColor = newsBg;
                NewsHeaderColor = Brushes.White;
                NewsHighImpactColor = Brushes.Red;
                NewsMediumImpactColor = Brushes.DarkGreen;
                NewsLowImpactColor = Brushes.Blue;
                NewsDefaultFont = new SimpleFont ("Arial", 10);
                NewsWarningFont = new SimpleFont ("Arial", 10) { Bold = true, Italic = true };
                NewsDebug = false;

                // Session Times
                EnableTF1 = true;
                StartTime1 = DateTime.Parse ("19:00", System.Globalization.CultureInfo.InvariantCulture);
                EndTime1 = DateTime.Parse ("23:30", System.Globalization.CultureInfo.InvariantCulture);
                FlattenTF1 = false;
                EnableTF2 = true;
                StartTime2 = DateTime.Parse ("03:00", System.Globalization.CultureInfo.InvariantCulture);
                EndTime2 = DateTime.Parse ("09:00", System.Globalization.CultureInfo.InvariantCulture);
                FlattenTF2 = false;
                EnableTF3 = true;
                StartTime3 = DateTime.Parse ("08:00", System.Globalization.CultureInfo.InvariantCulture);
                EndTime3 = DateTime.Parse ("03:45", System.Globalization.CultureInfo.InvariantCulture);
                FlattenTF3 = true;
                EnableSkipTimeWindow = true;
                SkipStartTime = DateTime.Parse ("11:45", System.Globalization.CultureInfo.InvariantCulture);
                SkipEndTime = DateTime.Parse ("13:00", System.Globalization.CultureInfo.InvariantCulture);

                // Indicator Settings master toggle
                ShowIndicatorSettings = false;

                // KingOrderBlock indicator defaults
                King_SwingPointNeighborhood = 5;
                King_ImbalanceQualifying = 3;
                King_OrderBlockFindingBosChochPeriod = 50;
                King_OrderBlockAge = 500;
                King_OrderBlocksSameDirectionOffset = 10;
                King_OrderBlocksDifferenceDirectionOffset = 10;
                King_SignalTradeQuantityPerOrderBlock = 3;
                King_SignalTradeSplitBars = 6;

                // PANAKanal indicator defaults
                Pana_Period = 20;
                Pana_Factor = 4.0;
                Pana_MiddlePeriod = 14;
                Pana_SignalBreakSplit = 20;
                Pana_SignalPullbackFindingPeriod = 10;

                // ThunderZilla indicator defaults
                Thunder_TrendMAType = gbThunderZillaMAType.SMA;
                Thunder_TrendPeriod = 200;
                Thunder_TrendSmoothingEnabled = false;
                Thunder_TrendSmoothingMethod = gbThunderZillaMAType.EMA;
                Thunder_TrendSmoothingPeriod = 10;
                Thunder_StopOffsetMultiplierStop = 60.0;
                Thunder_SignalQuantityPerFlat = 2;
                Thunder_SignalQuantityPerTrend = 999;

                // SumoPullback indicator defaults (mirror gbSumoPullback)
                SU_SlowMAType = gbSumoPullbackMAType.SMA;
                SU_SlowMAPeriod = 60;
                SU_SlowMASmoothingEnabled = false;
                SU_SlowMASmoothingMethod = gbSumoPullbackMAType.EMA;
                SU_SlowMASmoothingPeriod = 10;
                SU_FastMA1Type = gbSumoPullbackMAType.EMA;
                SU_FastMA1Period = 14;
                SU_FastMA1SmoothingEnabled = false;
                SU_FastMA1SmoothingMethod = gbSumoPullbackMAType.SMA;
                SU_FastMA1SmoothingPeriod = 5;
                SU_FastMA2Type = gbSumoPullbackMAType.EMA;
                SU_FastMA2Period = 30;
                SU_FastMA2SmoothingEnabled = false;
                SU_FastMA2SmoothingMethod = gbSumoPullbackMAType.SMA;
                SU_FastMA2SmoothingPeriod = 10;
                SU_FastMA3Type = gbSumoPullbackMAType.EMA;
                SU_FastMA3Period = 45;
                SU_FastMA3SmoothingEnabled = false;
                SU_FastMA3SmoothingMethod = gbSumoPullbackMAType.SMA;
                SU_FastMA3SmoothingPeriod = 15;
                SU_SignalSplitFirst = 15;
                SU_SignalSplitSecond = 30;

                // SuperJumpBoost indicator defaults (mirror gbSuperJumpBoost)
                SJ_SensitiveModeEnabled = true;
                SJ_OffsetLevel1 = 1.0;
                SJ_OffsetLevel2 = 2.0;
                SJ_OffsetLevel3 = 3.0;
                SJ_OffsetLevel4 = 4.0;
                SJ_OffsetBase = 4.0;
                SJ_ReferencePricePeriod = 2;
                SJ_LineLevelsOffset = 100;
                SJ_ExtremeNeighborhood = 30;
                SJ_SignalCloseThreshold = 70;
                SJ_SignalQuantityPerZone = 2;
                SJ_SignalSplit = 20;

                // Indicator visual defaults
                KO_Brush = Brushes.DodgerBlue;
                PA_Brush = Brushes.Cyan;
                TH_Brush = Brushes.LimeGreen;
                SJ_Brush = Brushes.Orange;
                SU_Brush = Brushes.Magenta;
                GroupTriggerBrush = Brushes.Gold;
                ArrowOffset = 5;

                // Signal arrow defaults
                ShowKOSignalArrows = false;
                ShowPASignalArrows = true;
                ShowTHSignalArrows = true;
                ShowSJSignalArrows = true;
                ShowSUSignalArrows = false;
                ShowGroupTriggerArrows = true;

                ShowKOSignalArrowLabels = true;
                ShowPASignalArrowLabels = true;
                ShowTHSignalArrowLabels = true;
                ShowSJSignalArrowLabels = true;
                ShowSUSignalArrowLabels = true;
                ShowGroupTriggerArrowLabel = true;

                KOSignalArrowText = "KO";
                PASignalArrowText = "PA";
                THSignalArrowText = "TH";
                SJSignalArrowText = "SJ";
                SUSignalArrowText = "SU";
                GroupTriggerArrowText = "GODZILLA";
                SignalArrowTextOffsetTicks = 20;

                KOSignalArrowBrush = Brushes.DodgerBlue;
                PASignalArrowBrush = Brushes.Cyan;
                THSignalArrowBrush = Brushes.LimeGreen;
                SJSignalArrowBrush = Brushes.Orange;
                SUSignalArrowBrush = Brushes.Magenta;

                EnableGroupTriggerBackBrush = true;
                GroupTriggerBackBrush = MakeFrozenBrush (55, 255, 215, 0);

                // Trade Markers - ATM mode only
                ShowEntryExitMarkers = true;
                EntryExitLongColor = Brushes.SteelBlue;
                EntryExitShortColor = Brushes.DarkOrange;
                EntryExitLineWidth = 2;
                ShowEntryExitLabels = true;
                EntryExitTextSize = 8;
            }
            else if (State == State.Configure)
            {
                ClearOutputWindow ();
                AddDataSeries (BarsPeriodType.Tick, 1);
            }
            else if (State == State.DataLoaded)
            {
                // Build the five child indicators directly
                // and mirrors the gbGodZillaSignals input model.
                _king = gbKingOrderBlock (
                    King_SwingPointNeighborhood,
                    King_ImbalanceQualifying,
                    King_OrderBlockFindingBosChochPeriod,
                    King_OrderBlockAge,
                    King_OrderBlocksSameDirectionOffset,
                    King_OrderBlocksDifferenceDirectionOffset,
                    King_SignalTradeQuantityPerOrderBlock,
                    King_SignalTradeSplitBars);
                if (UseKOSignals && ShowKOIndicator)
                    AddChartIndicator (_king);
                _king.Name = "";

                _pana = gbPANAKanal (
                    Pana_Period,
                    Pana_Factor,
                    Pana_MiddlePeriod,
                    Pana_SignalBreakSplit,
                    Pana_SignalPullbackFindingPeriod);
                if (UsePASignals && ShowPAIndicator)
                    AddChartIndicator (_pana);
                _pana.Name = "";

                _thunder = gbThunderZilla (
                    Thunder_TrendMAType,
                    Thunder_TrendPeriod,
                    Thunder_TrendSmoothingEnabled,
                    Thunder_TrendSmoothingMethod,
                    Thunder_TrendSmoothingPeriod,
                    Thunder_StopOffsetMultiplierStop,
                    Thunder_SignalQuantityPerFlat,
                    Thunder_SignalQuantityPerTrend);
                if (UseTHSignals && ShowTHIndicator)
                    AddChartIndicator (_thunder);
                _thunder.Name = "";

                _sumo = gbSumoPullback (
                    SU_SlowMAType,
                    SU_SlowMAPeriod,
                    SU_SlowMASmoothingEnabled,
                    SU_SlowMASmoothingMethod,
                    SU_SlowMASmoothingPeriod,
                    SU_FastMA1Type,
                    SU_FastMA1Period,
                    SU_FastMA1SmoothingEnabled,
                    SU_FastMA1SmoothingMethod,
                    SU_FastMA1SmoothingPeriod,
                    SU_FastMA2Type,
                    SU_FastMA2Period,
                    SU_FastMA2SmoothingEnabled,
                    SU_FastMA2SmoothingMethod,
                    SU_FastMA2SmoothingPeriod,
                    SU_FastMA3Type,
                    SU_FastMA3Period,
                    SU_FastMA3SmoothingEnabled,
                    SU_FastMA3SmoothingMethod,
                    SU_FastMA3SmoothingPeriod,
                    SU_SignalSplitFirst,
                    SU_SignalSplitSecond);
                if (UseSUSignals && ShowSUIndicator)
                    AddChartIndicator (_sumo);
                _sumo.Name = "";

                _sjb = gbSuperJumpBoost (
                    SJ_SensitiveModeEnabled,
                    SJ_OffsetLevel1,
                    SJ_OffsetLevel2,
                    SJ_OffsetLevel3,
                    SJ_OffsetLevel4,
                    SJ_OffsetBase,
                    SJ_ReferencePricePeriod,
                    SJ_LineLevelsOffset,
                    SJ_ExtremeNeighborhood,
                    SJ_SignalCloseThreshold,
                    SJ_SignalQuantityPerZone,
                    SJ_SignalSplit);
                if (UseSJSignals && ShowSJIndicator)
                    AddChartIndicator (_sjb);
                _sjb.Name = "";

                _koSignalSeries = new Series<double> (this);
                _paSignalSeries = new Series<double> (this);
                _thSignalSeries = new Series<double> (this);
                _sjSignalSeries = new Series<double> (this);
                _suSignalSeries = new Series<double> (this);

                // Optional EMA trade filter
                if (EnableEmaFilter)
                {
                    _emaShortFilter = EMA (EmaShortPeriod);
                    AddChartIndicator (_emaShortFilter);
                    _emaShortFilter.Name = "";
                    _emaShortFilter.Plots[0].Brush = Brushes.DodgerBlue;
                    _emaShortFilter.Plots[0].Width = 2;

                    _emaLongFilter = EMA (EmaLongPeriod);
                    AddChartIndicator (_emaLongFilter);
                    _emaLongFilter.Name = "";
                    _emaLongFilter.Plots[0].Brush = Brushes.HotPink;
                    _emaLongFilter.Plots[0].Width = 2;
                }

                // Optional News Filter - live chart only
                if (EnableNewsFilter)
                {
                    if (IsNewsFilterRuntimeDisabledContext ())
                    {
                        newsIndicator = null;

                        if (!_newsRuntimeDisabledPrinted)
                        {
                            if (EnableDebug)
                                Print ($"[{Name}] News Filter disabled for this runtime context. It is live-chart only and will not run in Strategy Analyzer/backtest or Playback/Market Replay.");
                            _newsRuntimeDisabledPrinted = true;
                        }
                    }
                    else
                    {
                        newsIndicator = NewsSignals (
                            NewsShowDisplay,                    // ShowNewsDisplay
                            NewsDisplayLocation,                // DisplayLocation
                            NewsDisplayXOffsetPixels,           // DisplayXOffsetPixels
                            NewsDisplayYOffsetPixels,           // DisplayYOffsetPixels
                            NewsUse24HourTime,                  // Use24timeFormat
                            NewsShowBackground,                 // ShowBackground
                            NewsShowTimeBackBrush,              // ShowNewsTimeBackBrush
                            NewsTimeBackBrush,                  // NewsTimeBackBrush
                            NewsUSOnlyEvents,                   // USOnlyEvents
                            NewsTodaysNewsOnly,                 // TodaysNewsOnly
                            NewsShowLowPriority,                // ShowLowPriority
                            NewsMaxNewsItems,                   // MaxNewsItems
                            NewsRefreshInterval,                // NewsRefreshInterval
                            NewsPreBlockMinutes,                // PreNewsBlockMinutes
                            NewsPostBlockMinutes,               // PostNewsBlockMinutes
                            NewsBlockHighImpact,                // BlockHighImpact
                            NewsBlockMediumImpact,              // BlockMediumImpact
                            NewsBlockLowImpact,                 // BlockLowImpact
                            NewsSendAlerts,                     // SendAlerts
                            NewsAlertInterval,                  // AlertInterval
                            NewsAlertWavFileName,               // AlertWavFileName
                            NewsDefaultTextColor,               // DefaultTextColor
                            NewsWarningTextColor,               // WarningTextColor
                            NewsBackgroundColor,                // BackgroundColor
                            NewsHeaderColor,                    // HeaderColor
                            NewsHighImpactColor,                // HighPriorityColor
                            NewsMediumImpactColor,              // MediumPriorityColor
                            NewsLowImpactColor,                 // LowPriorityColor
                            NewsDefaultFont,                    // DefaultFont
                            NewsWarningFont,                    // WarningFont
                            NewsDebug                           // Debug
                        );

                        AddChartIndicator (newsIndicator);
                        newsIndicator.Name = "";
                    }
                }

                if (LogEnabled)
                {
                    // Close any existing writer before opening a new one (handles replay re-entry)
                    if (_logWriter != null)
                    {
                        _logWriter.Flush ();
                        _logWriter.Dispose ();
                        _logWriter = null;
                    }

                    // Sanitize the account name so it's safe to embed in a file path.
                    string accountName = (Account != null && !string.IsNullOrEmpty (Account.Name)) ? Account.Name : "NoAccount";
                    string safeAccount = string.Concat (accountName.Split (System.IO.Path.GetInvalidFileNameChars ())).Replace (" ", "_");

                    string logPath = Path.Combine(
                        NinjaTrader.Core.Globals.UserDataDir,
                        "GodZilla_" + safeAccount + "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".csv");
                    _logWriter = new StreamWriter (logPath, append: false, encoding: Encoding.UTF8);
                    _logWriter.WriteLine ("OpenTime,Account,Instrument,OpenPrice,Qty,CloseTime,Trigger,Direction,AtmStrategy,RealizedPnL");
                    _logWriter.Flush ();
                }

                if (CurrentBar >= 0)
                    DrawPnlDisplay ();
            }
            else if (State == State.Realtime)
            {
                bool preserveFixedTicksHistoricalPnl =
                    OrderMode == OrderManagementMode.FixedTicks
                    && SystemPerformance != null
                    && SystemPerformance.AllTrades != null
                    && SystemPerformance.AllTrades.Count > 0;

                if (preserveFixedTicksHistoricalPnl)
                {
                    DateTime pnlDate = lastPnlSessionDate != Core.Globals.MinDate
                        ? lastPnlSessionDate
                        : (CurrentBar >= 0 ? Time[0].Date : DateTime.Now.Date);

                    SyncFixedTicksPnlFromSystemPerformance (pnlDate);

                    dailyUnrealizedPnL = 0.0;
                    totalRunningPnL = totalRealizedPnL + (UseUnrealizedPnl ? dailyUnrealizedPnL : 0.0);
                    dailyLimitHit = false;
                    dailyPnlStatusMessage = string.Empty;
                }
                else
                {
                    sessionStartTotalRealizedPnL = totalRealizedPnL;
                    lastAtmRealizedPnL = 0.0;
                    dailyRealizedPnL = 0.0;
                    dailyUnrealizedPnL = 0.0;
                    totalRunningPnL = totalRealizedPnL;
                    dailyLimitHit = false;
                    dailyPnlStatusMessage = string.Empty;
                    lastTradeClosedSummary = string.Empty;
                }

                _atmPositionConfirmed = false;
                ResetFixedOrderState ();
                ResetMartingaleRecovery ();

                _strategyEnabled = true;
                _armLong = true;
                _armShort = true;
                _autoArm = true;
                _reverseOnAlternateSignal = true;

                if (EnableDebug)
                    Print ($"{Name} entered realtime. ATM mode active.");

                if (ChartControl != null && !_uiInitialized)
                    CreateRBroControlPanel ();

                UpdateRBroButtons ();
                UpdateRBroStatusUI ();

                if (OrderMode == OrderManagementMode.AtmStrategy)
                    HookMarkerAccountEvents ();
            }
            else if (State == State.Terminated)
            {
                try
                {
                    if (_logWriter != null)
                    {
                        _logWriter.Flush ();
                        _logWriter.Dispose ();
                        _logWriter = null;
                    }
                }
                catch { }

                try
                {
                    RemoveRBroControlPanel ();
                }
                catch { }
                try
                {
                    DisposeSharpDxResources ();
                }
                catch { }
                try
                {
                    UnhookMarkerAccountEvents ();
                }
                catch { }

                // Finalize any open trade marker so it doesn't leak as an unterminated line
                try
                {
                    if (_markerCurrent != null)
                    {
                        double lastPrice = 0.0;
                        try { lastPrice = Close[0]; } catch { }
                        if (lastPrice <= 0.0) lastPrice = _markerCurrent.EntryPrice;

                        _markerCurrent.ExitBar   = Math.Max (CurrentBar, _markerCurrent.EntryBar);
                        _markerCurrent.ExitPrice  = lastPrice;
                        _markerCurrent.IsComplete = true;
                        _markerList.Add (_markerCurrent);
                        _markerCurrent = null;
                    }
                }
                catch { }
            }
        }

        protected override void OnBarUpdate ()
        {
            if (_king == null || _pana == null || _thunder == null || _sjb == null || _sumo == null)
                return;

            if (BarsInProgress == 1)
            {
                if (CurrentBars[1] < 1)
                    return;

                UpdateDailyPnlOnTickSeries ();
                ManageFixedBreakeven ();
                SanityCheckFixedTicksState ();
                ProcessPendingSessionPnlPrint ();
                return;
            }

            if (BarsInProgress != 0)
                return;

            if (CurrentBars[0] < BarsRequiredToTrade)
                return;

            UpdateNormalizedSignalSeriesAndDrawSignalArrows ();

            if (OrderMode == OrderManagementMode.AtmStrategy)
            {
                CheckMarkerPositionChange ();
                RedrawCompletedMarkers ();
            }

            if (State == State.Historical && OrderMode != OrderManagementMode.FixedTicks)
                return;

            // Flatten logic at end of TimeFrames
            int currentTime = ToTime(Time[0]);
            CheckFlattenTimeframes (currentTime);

            // News filter management/flatten check
            ManageNewsFilter ();

            ManageFixedBreakeven ();
            SanityCheckFixedTicksState ();
            ProcessPendingSessionPnlPrint ();

            if (dailyLimitHit)
                return;

            if (martingaleRecoveryActive)
                return;

            bool isWithinTradingTime = CheckTradingTimeframes(currentTime);

            MarketPosition preSignalPosition = GetCurrentTradePosition ();

            if (preSignalPosition != MarketPosition.Flat && !_reverseOnAlternateSignal)
                return; // Do not process entry/reversal logic while in a position unless REV is ON

            if (!isWithinTradingTime)
                return;

            if (State != State.Realtime && OrderMode != OrderManagementMode.FixedTicks)
                return;

            // Honor the on-chart enable/disable button — block new entries when off.
            if (!_strategyEnabled)
                return;

            // Block new entries during the active news warning/post-news window.
            if (IsNewsTradingBlocked ())
                return;

            // Entry Logic
            double koRaw = (_king != null) ? _king.Signal_Trade[0] : 0;
            double paRaw = (_pana != null) ? _pana.Signal_Trade[0] : 0;
            double thRaw = (_thunder != null) ? _thunder.Signal_Trade[0] : 0;
            double sjRaw = (_sjb != null) ? _sjb.Signal_Trade[0] : 0;
            double suRaw = (_sumo != null) ? _sumo.Signal_Trade[0] : 0;

            double ko = ComputeSignal (UseKOSignals, koRaw, KO_LongValue, KO_ShortValue);
            double pa = ComputeSignal (UsePASignals, paRaw, PA_LongValue, PA_ShortValue);
            double th = ComputeSignal (UseTHSignals, thRaw, TH_LongValue, TH_ShortValue);
            double sj = ComputeSignal (UseSJSignals, sjRaw, SJ_LongValue, SJ_ShortValue);
            double su = ComputeSignal (UseSUSignals, suRaw, SU_LongValue, SU_ShortValue);

            if (_koSignalSeries != null)
                _koSignalSeries[0] = ko;
            if (_paSignalSeries != null)
                _paSignalSeries[0] = pa;
            if (_thSignalSeries != null)
                _thSignalSeries[0] = th;
            if (_sjSignalSeries != null)
                _sjSignalSeries[0] = sj;
            if (_suSignalSeries != null)
                _suSignalSeries[0] = su;

            bool koLong = ko == 1 && UseKOSignals;
            bool paLong = pa == 1 && UsePASignals;
            bool thLong = th == 1 && UseTHSignals;
            bool sjLong = sj == 1 && UseSJSignals;
            bool suLong = su == 1 && UseSUSignals;
            bool koShort = ko == -1 && UseKOSignals;
            bool paShort = pa == -1 && UsePASignals;
            bool thShort = th == -1 && UseTHSignals;
            bool sjShort = sj == -1 && UseSJSignals;
            bool suShort = su == -1 && UseSUSignals;

            bool goLong = false, goShort = false;
            int  groupTriggeredSize = 0;       // 1/2/3/4/5 = required same-bar group count
            string groupTriggeredName = string.Empty;

            // Same-bar group consensus only. Use Required Count = 1 on either
            // trigger set for a one-signal trigger.
            GroupTriggerResult primaryGroup = EvaluatePrimaryGroupTriggerSet (koLong, paLong, thLong, sjLong, suLong, koShort, paShort, thShort, sjShort, suShort);
            GroupTriggerResult secondaryGroup = EvaluateSecondaryGroupTriggerSet (koRaw, paRaw, thRaw, sjRaw, suRaw);

            goLong = (primaryGroup != null && primaryGroup.Long) || (secondaryGroup != null && secondaryGroup.Long);
            goShort = (primaryGroup != null && primaryGroup.Short) || (secondaryGroup != null && secondaryGroup.Short);

            GroupTriggerResult activeGroup = null;

            if (primaryGroup != null && (primaryGroup.Long || primaryGroup.Short))
                activeGroup = primaryGroup;
            else if (secondaryGroup != null && (secondaryGroup.Long || secondaryGroup.Short))
                activeGroup = secondaryGroup;

            if (goLong && goShort)
            {
                goLong = false;
                goShort = false;
                groupTriggeredSize = 0;
                groupTriggeredName = string.Empty;
                koLong = paLong = thLong = sjLong = suLong = false;
                koShort = paShort = thShort = sjShort = suShort = false;
            }
            else if (activeGroup != null)
            {
                groupTriggeredSize = activeGroup.GroupSize;
                groupTriggeredName = activeGroup.TriggerName;

                if (goLong)
                {
                    koLong = activeGroup.UsesKO;
                    paLong = activeGroup.UsesPA;
                    thLong = activeGroup.UsesTH;
                    sjLong = activeGroup.UsesSJ;
                    suLong = activeGroup.UsesSU;
                }

                if (goShort)
                {
                    koShort = activeGroup.UsesKO;
                    paShort = activeGroup.UsesPA;
                    thShort = activeGroup.UsesTH;
                    sjShort = activeGroup.UsesSJ;
                    suShort = activeGroup.UsesSU;
                }
            }

            // Optional EMA trade filter: longs require short EMA > long EMA, shorts require short EMA < long EMA.
            if (EnableEmaFilter && _emaShortFilter != null && _emaLongFilter != null && CurrentBar >= Math.Max (EmaShortPeriod, EmaLongPeriod))
            {
                bool bullishEmaAlignment = _emaShortFilter[0] > _emaLongFilter[0];
                bool bearishEmaAlignment = _emaShortFilter[0] < _emaLongFilter[0];

                if (goLong && !bullishEmaAlignment)
                {
                    goLong = false;
                    koLong = paLong = thLong = suLong = sjLong = false;
                }
                if (goShort && !bearishEmaAlignment)
                {
                    goShort = false;
                    koShort = paShort = thShort = suShort = sjShort = false;
                }
            }

            MarketPosition currentTradePosition = GetCurrentTradePosition ();

            // If a reverse was queued, wait until the current ATM is fully flat/cleared before submitting the new ATM.
            if (pendingReverseActive 
                && !CanUseReverseOnAlternateSignal ())
            {
                ClearPendingReverse ();
            }
            else if (pendingReverseActive
                && currentTradePosition == MarketPosition.Flat
                && !HasActiveEntryState ())
            {
                if (pendingReverseDirection == MarketPosition.Long && _armLong)
                {
                    SubmitTradeEntry (true, pendingReverseUsesKO, pendingReverseUsesPA, pendingReverseUsesTH, pendingReverseUsesSJ, pendingReverseUsesSU, pendingReverseGroupSize, pendingReverseGroupName, "Pending reverse entry");
                    ClearPendingReverse ();
                    return;
                }
                else if (pendingReverseDirection == MarketPosition.Short && _armShort)
                {
                    SubmitTradeEntry (false, pendingReverseUsesKO, pendingReverseUsesPA, pendingReverseUsesTH, pendingReverseUsesSJ, pendingReverseUsesSU, pendingReverseGroupSize, pendingReverseGroupName, "Pending reverse entry");
                    ClearPendingReverse ();
                    return;
                }

                ClearPendingReverse ();
            }

            // If already in a position, optionally reverse on the opposite qualified signal.
            if (CanUseReverseOnAlternateSignal () && !pendingReverseActive && !HasPendingEntryOrder ())
            {
                if (currentTradePosition == MarketPosition.Long && goShort && _armShort)
                {
                    QueuePendingReverse (MarketPosition.Short, koShort, paShort, thShort, sjShort, suShort, groupTriggeredSize, groupTriggeredName);
                    FlattenEverything ("Reverse on alternate SHORT signal");
                    return;
                }

                if (currentTradePosition == MarketPosition.Short && goLong && _armLong)
                {
                    QueuePendingReverse (MarketPosition.Long, koLong, paLong, thLong, sjLong, suLong, groupTriggeredSize, groupTriggeredName);
                    FlattenEverything ("Reverse on alternate LONG signal");
                    return;
                }
            }

            // If still in a position and no reverse happened, do not stack entries.
            if (currentTradePosition != MarketPosition.Flat)
                return;

            // Normal flat-position entry logic.
            if (!HasActiveEntryState () && goLong && _armLong)
            {
                SubmitTradeEntry (true, koLong, paLong, thLong, sjLong, suLong, groupTriggeredSize, groupTriggeredName, "Normal long entry");
            }
            else if (!HasActiveEntryState () && goShort && _armShort)
            {
                SubmitTradeEntry (false, koShort, paShort, thShort, sjShort, suShort, groupTriggeredSize, groupTriggeredName, "Normal short entry");
            }

            // FixedTicks uses NinjaTrader managed orders. Do not run ATM tracking logic.
            if (OrderMode == OrderManagementMode.FixedTicks)
                return;

            if (!isAtmStrategyCreated)
                return;

            if (orderId.Length > 0)
            {
                string[] status;

                if (TryGetAtmEntryOrderStatusSafe (ref orderId, "Normal", out status)
                    && status != null
                    && status.Length >= 3)
                {
                    if (EnableDebug)
                        Print ("The entry order average fill price is: " + status[0]);
                    if (EnableDebug)
                        Print ("The entry order filled amount is: " + status[1]);
                    if (EnableDebug)
                        Print ("The entry order order state is: " + status[2]);

                    if (status[2] == "Filled")
                    {
                        CaptureFill (status);
                        orderId = string.Empty;
                    }
                    else if (status[2] == "Cancelled" || status[2] == "Rejected")
                    {
                        orderId = string.Empty;
                        ClearActiveTradeSignalSources ();
                    }
                }
            }

            if (atmStrategyId.Length > 0 && EnableDebug)
            {
                MarketPosition atmMp;
                double avgPrice;
                double unrealized;
                int qty;

                if (TryGetAtmMarketPositionSafe (ref atmStrategyId, "Normal Debug", out atmMp))
                    Print ("The current ATM Strategy market position is: " + atmMp);
                else
                {
                    ClearNormalAtmState ("Normal ATM debug read failed");
                    return;
                }

                if (TryGetAtmQuantitySafe (atmStrategyId, "Normal Debug", out qty))
                    Print ("The current ATM Strategy position quantity is: " + qty);

                if (TryGetAtmAveragePriceSafe (atmStrategyId, "Normal Debug", out avgPrice))
                    Print ("The current ATM Strategy average price is: " + avgPrice);

                if (TryGetAtmUnrealizedPnlSafe (atmStrategyId, "Normal Debug", out unrealized))
                    Print ("The current ATM Strategy Unrealized PnL is: " + unrealized);
            }
        }

        protected override void OnExecutionUpdate (Execution execution, string executionId, double price, int quantity, MarketPosition marketPosition, string orderId, DateTime time)
        {
            if (OrderMode != OrderManagementMode.FixedTicks)
                return;

            if (execution == null || execution.Order == null || quantity <= 0)
                return;

            string orderName = execution.Order.Name ?? string.Empty;

            if (!string.IsNullOrEmpty (fixedEntrySignalName) && orderName == fixedEntrySignalName)
            {
                fixedPositionConfirmed = true;
                fixedTradeCloseProcessed = false;
                fixedEntryAvgPrice = AveragePriceFromExecution (fixedEntryAvgPrice, fixedEntryQty, price, quantity);
                fixedEntryQty += quantity;

                if (_tradeMap.TryGetValue (fixedEntrySignalName, out TradeRecord rec))
                {
                    if (rec.OpenPrice == 0.0)
                        rec.OpenPrice = Instrument.MasterInstrument.RoundToTickSize (fixedEntryAvgPrice);

                    rec.Qty = fixedEntryQty;
                }

                if (EnableDebug)
                    Print ($"[{Name}] FIXED ENTRY FILLED | Signal={orderName} | Price={price:F2} | Qty={quantity} | Avg={fixedEntryAvgPrice:F2}");

                return;
            }

            if (!string.IsNullOrEmpty (fixedMartingaleSignalName) && orderName == fixedMartingaleSignalName)
            {
                fixedMartingalePositionConfirmed = true;
                fixedMartingaleCloseProcessed = false;
                fixedMartingaleEntryAvgPrice = AveragePriceFromExecution (fixedMartingaleEntryAvgPrice, fixedMartingaleEntryQty, price, quantity);
                fixedMartingaleEntryQty += quantity;

                if (_tradeMap.TryGetValue (fixedMartingaleSignalName, out TradeRecord rec))
                {
                    if (rec.OpenPrice == 0.0)
                        rec.OpenPrice = Instrument.MasterInstrument.RoundToTickSize (fixedMartingaleEntryAvgPrice);

                    rec.Qty = fixedMartingaleEntryQty;
                }

                if (EnableDebug)
                    Print ($"[{Name}] FIXED MARTINGALE ENTRY FILLED | Signal={orderName} | Price={price:F2} | Qty={quantity} | Avg={fixedMartingaleEntryAvgPrice:F2}");

                return;
            }

            if ((fixedPositionConfirmed && !string.IsNullOrEmpty (fixedEntrySignalName))
                || (fixedMartingalePositionConfirmed && !string.IsNullOrEmpty (fixedMartingaleSignalName)))
            {
                if (price > 0)
                {
                    lastFixedExitCandidatePrice = price;
                    lastFixedExitCandidateTime = time;
                }
            }

            bool strategyFlat = IsFixedStrategyFlat ();

            if (fixedPositionConfirmed
                && !fixedTradeCloseProcessed
                && strategyFlat
                && !string.IsNullOrEmpty (fixedEntrySignalName))
            {
                ProcessFixedTradeClosed (GetFixedExitFallbackPrice (price), time);
                return;
            }

            if (fixedMartingalePositionConfirmed
                && !fixedMartingaleCloseProcessed
                && strategyFlat
                && !string.IsNullOrEmpty (fixedMartingaleSignalName))
            {
                ProcessFixedMartingaleClosed (GetFixedExitFallbackPrice (price), time);
                return;
            }
        }

        private bool IsNewsFilterRuntimeDisabledContext ()
        {
            // Strategy Analyzer/backtest usually has no chart context.
            if (ChartControl == null)
                return true;

            // Disable in Playback/Market Replay.
            if (IsPlaybackConnectionActive ())
                return true;

            return false;
        }

        private bool IsNewsFilterRuntimeActive ()
        {
            if (!EnableNewsFilter)
                return false;

            if (newsIndicator == null)
                return false;

            if (State != State.Realtime)
                return false;

            if (IsPlaybackConnectionActive ())
                return false;

            return true;
        }

        private bool IsPlaybackConnectionActive ()
        {
            try
            {
                Type connectionType = typeof (NinjaTrader.Cbi.Connection);

                System.Reflection.PropertyInfo playbackProp = connectionType.GetProperty (
            "PlaybackConnection",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

                if (playbackProp != null)
                {
                    object playbackConnection = playbackProp.GetValue (null, null);

                    if (playbackConnection != null)
                        return true;
                }

                if (Account != null && Account.Connection != null && Account.Connection.Options != null)
                {
                    string provider = Account.Connection.Options.Provider.ToString ();

                    if (!string.IsNullOrEmpty (provider)
                        && provider.IndexOf ("Playback", StringComparison.OrdinalIgnoreCase) >= 0)
                        return true;
                }
            }
            catch
            {
                // Reflection path failed (NT8 version mismatch or permission issue).
                // Fall through and return false (treat as not-playback). This is the
                // more-permissive path — news filter could activate in a replay context
                // if both detection methods fail. Acceptable since this is defensive
                // detection only; the user can disable EnableNewsFilter manually.
            }

            return false;
        }

        private bool IsNewsTradingBlocked ()
        {
            try
            {
                if (!IsNewsFilterRuntimeActive ())
                    return false;

                if (newsIndicator.NewsBlock == null)
                    return false;

                return newsIndicator.NewsBlock[0] >= 0.5;
            }
            catch
            {
                return false;
            }
        }

        private double GetNewsMinutesToNext ()
        {
            try
            {
                if (!IsNewsFilterRuntimeActive ())
                    return -1.0;

                if (newsIndicator.MinutesToNextNews == null)
                    return -1.0;

                return newsIndicator.MinutesToNextNews[0];
            }
            catch
            {
                return -1.0;
            }
        }

        private void ManageNewsFilter ()
        {
            if (!IsNewsFilterRuntimeActive ())
            {
                _lastNewsBlockActive = false;
                return;
            }

            bool newsBlocked = IsNewsTradingBlocked ();
            double minutesToNext = GetNewsMinutesToNext ();

            bool enteringPreNewsWarningWindow =
        newsBlocked
        && !_lastNewsBlockActive
        && minutesToNext >= 0;

            if (enteringPreNewsWarningWindow && NewsFlattenAtWarningTime)
                FlattenEverything ("News warning window started - flatten/cancel enabled");

            _lastNewsBlockActive = newsBlocked;
        }

        private string BuildNewsFilterDisplayLine ()
        {
            if (!EnableNewsFilter)
                return "News Filter: OFF";

            if (!IsNewsFilterRuntimeActive ())
                return "News Filter: DISABLED";

            bool blocked = IsNewsTradingBlocked ();
            double mins = GetNewsMinutesToNext ();

            if (blocked)
                return mins >= 0
                    ? "News Filter: BLOCKED | Next: " + mins.ToString ("F1") + " min"
                    : "News Filter: BLOCKED | Post-News";

            return mins >= 0
                ? "News Filter: CLEAR | Next: " + mins.ToString ("F1") + " min"
                : "News Filter: CLEAR";
        }

        private string FormatInstrumentName ()
        {
            string name = Instrument.MasterInstrument.Name;
            if (Instrument.MasterInstrument.InstrumentType == InstrumentType.Future
                && Instrument.Expiry != Core.Globals.MaxDate)
                name += " " + Instrument.Expiry.ToString ("MM-yy");
            return name;
        }

        private void CaptureFill (string[] status)
        {
            if (!_tradeMap.TryGetValue (atmStrategyId, out TradeRecord rec))
                return;

            if (rec.OpenPrice == 0.0
                && double.TryParse (status[0], System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out double price)
                && price > 0)
                rec.OpenPrice = Instrument.MasterInstrument.RoundToTickSize (price);

            if (rec.Qty == 0
                && int.TryParse (status[1], out int qty))
                rec.Qty = qty;
        }

        private void UpdateDailyPnlOnTickSeries ()
        {
            if (State == State.Historical && OrderMode != OrderManagementMode.FixedTicks)
                return;

            if (State != State.Historical && State != State.Realtime)
                return;

            DateTime tickTime = Times[1][0];
            double normalUnrealizedPnL = 0.0;
            bool fixedTicksPnlSyncedFromPerformance = false;

            ManageNewsFilter ();
            UpdateMartingaleRecoveryOnTick (tickTime);

            if (lastPnlSessionDate == Core.Globals.MinDate || Bars.IsFirstBarOfSession || tickTime.Date != lastPnlSessionDate.Date)
            {
                if (EnableDebug)
                    Print ($"{tickTime:yyyy-MM-dd HH:mm:ss} | DAILY PNL RESET | "
                        + $"PrevSessionDate={lastPnlSessionDate} | "
                        + $"Bars.IsFirstBarOfSession={Bars.IsFirstBarOfSession} | "
                        + $"TotalRealizedBeforeReset={totalRealizedPnL:F2}");

                sessionStartTotalRealizedPnL = totalRealizedPnL;
                lastAtmRealizedPnL = 0.0;
                dailyRealizedPnL = 0.0;
                dailyUnrealizedPnL = 0.0;
                totalRunningPnL = totalRealizedPnL;
                dailyLimitHit = false;
                dailyPnlStatusMessage = string.Empty;
                lastPnlSessionDate = tickTime.Date;
            }

            if (OrderMode == OrderManagementMode.FixedTicks)
            {
                try
                {
                    if (Position.MarketPosition != MarketPosition.Flat)
                        normalUnrealizedPnL = Position.GetUnrealizedProfitLoss (PerformanceUnit.Currency, Closes[1][0]);
                    else
                        normalUnrealizedPnL = 0.0;
                }
                catch
                {
                    normalUnrealizedPnL = 0.0;
                }

                SyncFixedTicksPnlFromSystemPerformance (tickTime);
                fixedTicksPnlSyncedFromPerformance = true;
            }
            else
            {
                if (orderId.Length > 0)
                {
                    string[] status;

                    if (TryGetAtmEntryOrderStatusSafe (ref orderId, "Normal", out status)
                        && status != null
                        && status.Length >= 3)
                    {
                        if (status[2] == "Filled")
                        {
                            CaptureFill (status);
                            orderId = string.Empty;
                        }
                        else if (status[2] == "Cancelled" || status[2] == "Rejected")
                        {
                            orderId = string.Empty;
                            ClearActiveTradeSignalSources ();
                        }
                    }
                }

                if (atmStrategyId.Length > 0)
                {
                    Cbi.MarketPosition atmPos;

                    if (!TryGetAtmMarketPositionSafe (ref atmStrategyId, "Normal", out atmPos))
                    {
                        ClearNormalAtmState ("Normal ATM ID no longer exists");
                        dailyUnrealizedPnL = 0.0;
                        return;
                    }

                    if (atmPos != Cbi.MarketPosition.Flat)
                    {
                        _atmPositionConfirmed = true;
                        isAtmStrategyCreated = true;
                        orderId = string.Empty;

                        if (_tradeMap.TryGetValue (atmStrategyId, out TradeRecord rec) && rec.OpenPrice == 0.0)
                        {
                            double avgPrice;
                            int qty;

                            if (TryGetAtmAveragePriceSafe (atmStrategyId, "Normal", out avgPrice) && avgPrice > 0)
                            {
                                rec.OpenPrice = Instrument.MasterInstrument.RoundToTickSize (avgPrice);

                                if (TryGetAtmQuantitySafe (atmStrategyId, "Normal", out qty))
                                    rec.Qty = qty;
                            }
                        }

                        double unrealized;

                        if (TryGetAtmUnrealizedPnlSafe (atmStrategyId, "Normal", out unrealized))
                            normalUnrealizedPnL = Instrument.MasterInstrument.RoundToTickSize (unrealized);
                        else
                            normalUnrealizedPnL = 0.0;
                    }
                    else if (_atmPositionConfirmed)
                    {
                        double atmRealized;

                        if (TryGetAtmRealizedPnlSafe (atmStrategyId, "Normal", out atmRealized) && !double.IsNaN (atmRealized))
                            lastAtmRealizedPnL = Instrument.MasterInstrument.RoundToTickSize (atmRealized);
                        else
                            lastAtmRealizedPnL = 0.0;

                        MarketPosition closedDirection = activeTradeDirection;
                        bool startMartingale = ShouldStartMartingaleRecovery (lastAtmRealizedPnL, closedDirection);
                        MarketPosition martingaleDirection = startMartingale ? GetOppositeDirection (closedDirection) : MarketPosition.Flat;

                        totalRealizedPnL += lastAtmRealizedPnL;
                        UpdateSignalTrackingOnTradeClose (lastAtmRealizedPnL);
                        UpdateLastTradeClosedSummary (lastAtmRealizedPnL);
                        PrintSignalTrackingOnTradeClose (lastAtmRealizedPnL);

                        if (_logWriter != null && _tradeMap.TryGetValue (atmStrategyId, out TradeRecord tr))
                        {
                            string acct = (Account != null && !string.IsNullOrEmpty (Account.Name)) ? Account.Name : "NoAccount";
                            _logWriter.WriteLine ($"{tr.OpenTime:yyyy-MM-dd HH:mm:ss},{acct},{tr.Instrument},{tr.OpenPrice:F2},{tr.Qty},{tickTime:yyyy-MM-dd HH:mm:ss},{tr.Trigger},{tr.Direction},{tr.AtmStrategyName},{lastAtmRealizedPnL:F2}");
                            _logWriter.Flush ();
                        }

                        _tradeMap.Remove (atmStrategyId);
                        ClearActiveTradeSignalSources ();
                        atmStrategyId = string.Empty;
                        orderId = string.Empty;
                        isAtmStrategyCreated = false;
                        _atmPositionConfirmed = false;
                        dailyUnrealizedPnL = 0.0;

                        if (startMartingale && martingaleDirection != MarketPosition.Flat)
                            SubmitMartingaleRecoveryEntry (martingaleDirection);
                    }
                    else
                    {
                        dailyUnrealizedPnL = 0.0;
                    }
                }
            }
            dailyUnrealizedPnL = normalUnrealizedPnL + GetMartingaleUnrealizedPnL ();

            if (!fixedTicksPnlSyncedFromPerformance)
                dailyRealizedPnL = totalRealizedPnL - sessionStartTotalRealizedPnL;

            totalRunningPnL = totalRealizedPnL + (UseUnrealizedPnl ? dailyUnrealizedPnL : 0.0);

            double dailyPnlToCheck = dailyRealizedPnL + (UseUnrealizedPnl ? dailyUnrealizedPnL : 0.0);

            if (EnableDebug)
                Print ($"{tickTime:yyyy-MM-dd HH:mm:ss} | DAILY PNL CHECK | "
                    + $"Closed={dailyRealizedPnL:F2} | "
                    + $"Open={dailyUnrealizedPnL:F2} | "
                    + $"Check={dailyPnlToCheck:F2} | "
                    + $"TotalRunning={totalRunningPnL:F2} | "
                    + $"PTEnabled={EnableDailyProfitTarget} | "
                    + $"PT={DailyProfitTarget:F2} | "
                    + $"LLEnabled={EnableDailyLossLimit} | "
                    + $"LL={DailyLossLimit:F2} | "
                    + $"LimitHit={dailyLimitHit} | "
                    + $"ATM={atmStrategyId} | "
                    + $"Order={orderId}");

            if (!dailyLimitHit)
            {
                if (EnableDailyProfitTarget && dailyPnlToCheck >= DailyProfitTarget)
                {
                    dailyLimitHit = true;
                    dailyPnlStatusMessage = $"DAILY PROFIT TARGET HIT: {dailyPnlToCheck:C}";

                    if (EnableDebug)
                        Print ($"{tickTime:yyyy-MM-dd HH:mm:ss} | DAILY PROFIT TARGET HIT | "
                            + $"Check={dailyPnlToCheck:F2} >= PT={DailyProfitTarget:F2} | "
                            + $"Closed={dailyRealizedPnL:F2} | Open={dailyUnrealizedPnL:F2} | "
                            + $"Calling FlattenEverything()");

                    FlattenEverything ("Daily profit target hit");
                }
                else if (EnableDailyLossLimit && dailyPnlToCheck <= -DailyLossLimit)
                {
                    dailyLimitHit = true;
                    dailyPnlStatusMessage = $"DAILY LOSS LIMIT HIT: {dailyPnlToCheck:C}";

                    if (EnableDebug)
                        Print ($"{tickTime:yyyy-MM-dd HH:mm:ss} | DAILY LOSS LIMIT HIT | "
                            + $"Check={dailyPnlToCheck:F2} <= LL=-{DailyLossLimit:F2} | "
                            + $"Closed={dailyRealizedPnL:F2} | Open={dailyUnrealizedPnL:F2} | "
                            + $"Calling FlattenEverything()");

                    FlattenEverything ("Daily loss limit hit");
                }
            }

            // Naked-position check + dashboard refresh are wall-clock throttled.
            // Both are O(N) work (Account.Orders enumeration, SharpDX snapshot,
            // dispatcher InvokeAsync) and don't need per-tick fidelity. Without
            // throttling, fast-forward playback queues thousands of dispatcher
            // jobs per wall-second → UI thread lockup.
            DateTime nowUtc = DateTime.UtcNow;
            if ((nowUtc - lastNakedCheckUtc).TotalSeconds >= NakedCheckIntervalSeconds)
            {
                lastNakedCheckUtc = nowUtc;
                CheckForNakedPositions (tickTime);
            }
            if ((nowUtc - _lastTickUiSyncUtc).TotalMilliseconds >= TICK_UI_SYNC_INTERVAL_MS)
            {
                _lastTickUiSyncUtc = nowUtc;
                DrawPnlDisplay ();
                UpdateRBroStatusUI ();
            }
        }

        // ============================================================
        //  HUD — SharpDX boxed dashboard (AlightenLite-style)
        //
        //  Data thread (OnBarUpdate) builds the snapshot strings here.
        //  UI thread (OnRender) reads them and paints the box. Strings are
        //  swapped atomically (single reference assignment), so a torn read
        //  during a frame gives stale-but-consistent text.
        // ============================================================
        private void DrawPnlDisplay ()
        {
            BuildDashboardSnapshot ();
            RequestHudRepaint ();
        }

        private void BuildDashboardSnapshot ()
        {
            try
            {
                int displayTime = ToTime (Time[0]);

                bool timeFilterEnabled = EnableTF1 || EnableTF2 || EnableTF3 || EnableSkipTimeWindow;
                bool inSession = !timeFilterEnabled || CheckTradingTimeframes (displayTime);
                _hudSessionOutside = timeFilterEnabled && !inSession;
                _hudSession = timeFilterEnabled
                    ? (inSession ? "IN SESSION" : "OUT OF SESSION")
                    : "24H (filter off)";

                _hudTitle = Name ?? "GodZilla";
                _hudVersion = StrategyVersion ?? "";

                _hudArm = _strategyEnabled
                    ? "ENABLED  L:" + (_armLong ? "ON " : "OFF")
                        + "  S:" + (_armShort ? "ON " : "OFF")
                        + "  REV:" + (_reverseOnAlternateSignal ? "ON" : "OFF")
                    : "DISABLED  REV:" + (_reverseOnAlternateSignal ? "ON" : "OFF");

                string targetStr = EnableDailyProfitTarget ? "+$" + DailyProfitTarget.ToString ("F0") : "off";
                string lossStr   = EnableDailyLossLimit    ? "-$" + DailyLossLimit.ToString ("F0")   : "off";
                _hudTargets = "Target " + targetStr + "   MaxLoss " + lossStr;

                double openPnl = UseUnrealizedPnl ? dailyUnrealizedPnL : 0.0;
                double sumPnl  = dailyRealizedPnL + openPnl;
                _hudPnlPositive = sumPnl >= 0;

                _hudPnl = string.Format ("Total {0,10}   Closed {1,10}",
                    FormatMoney (totalRunningPnL),
                    FormatMoney (dailyRealizedPnL));
                _hudPnlOpen = UseUnrealizedPnl
                    ? "Open  " + FormatMoney (openPnl).PadLeft (10)
                    : "";

                _hudKillHit = dailyLimitHit;
                _hudKillProfit = dailyLimitHit
                    && !string.IsNullOrEmpty (dailyPnlStatusMessage)
                    && dailyPnlStatusMessage.IndexOf ("PROFIT", StringComparison.OrdinalIgnoreCase) >= 0;
                _hudKillBanner = dailyLimitHit ? (dailyPnlStatusMessage ?? "") : "";

                MarketPosition mp = MarketPosition.Flat;
                try
                {
                    mp = Position != null ? Position.MarketPosition : MarketPosition.Flat;
                }
                catch { }

                if (dailyLimitHit)
                    _hudStatus = _hudKillProfit ? "PROFIT TARGET HIT — flat" : "LOSS LIMIT HIT — flat";
                else if (mp == MarketPosition.Flat)
                    _hudStatus = "IDLE — waiting for signal";
                else
                    _hudStatus = "IN POSITION " + mp.ToString ().ToUpper ();

                _hudShowNews = EnableNewsFilter;
                _hudNews = EnableNewsFilter ? BuildNewsFilterDisplayLine () : "";
                try
                {
                    _hudNewsBlocked = IsNewsTradingBlocked ();
                }
                catch { _hudNewsBlocked = false; }

                _hudLastTrade = string.IsNullOrEmpty (lastTradeClosedSummary) ? "Last: —" : lastTradeClosedSummary;

                _hudShowSignals = EnableSignalTracking;
                if (EnableSignalTracking)
                {
                    var lines = BuildSignalTrackingDisplayLines ();
                    // Atomic swap (assign reference) — UI thread sees old or new list cleanly.
                    _hudSignalLines = lines ?? new List<string> ();
                }
            }
            catch (Exception ex)
            {
                if (_hudErrors < 3)
                {
                    _hudErrors++;
                    if (EnableDebug)
                        Print ("[" + Name + "] HUD snapshot error: " + ex.Message);
                }
            }
        }

        private static string FormatMoney (double v)
        {
            string sign = v >= 0 ? "+$" : "-$";
            return sign + Math.Abs (v).ToString ("F2");
        }

        private void RequestHudRepaint ()
        {
            // Stability-mode short-circuit: if the user disabled the dashboard,
            // there's nothing to paint, so skip the dispatcher InvalidateVisual()
            // entirely. Removes its contribution to dispatcher pressure during
            // fast-forward playback (per AGENTS.md gotcha #17).
            if (!ShowDashboard || DashboardPosition == HudCorner.Hidden)
                return;

            ChartControl cc = ChartControl;
            if (cc == null)
                return;
            DateTime now = DateTime.UtcNow;
            if ((now - _lastHudInvalidateUtc).TotalMilliseconds < HUD_MIN_INVALIDATE_MS)
                return;
            _lastHudInvalidateUtc = now;
            try
            {
                cc.InvalidateVisual ();
            }
            catch { }
        }

        // OnRenderTargetChanged: NT8 calls this when chart resizes / device lost.
        // Per AGENTS.md lifecycle defense + feedback_nt8_lifecycle_defense.md:
        // try/catch wrap, same-target-skip *gated by _dxInitialized*, eager re-init
        // (never lazy in OnRender) so brushes are never null between dispose and
        // the next frame.
        public override void OnRenderTargetChanged ()
        {
            try
            {
                base.OnRenderTargetChanged ();
            }
            catch { }

            try
            {
                // Same-target-skip: if NT8 fires this with the same RT we're already
                // bound to AND brushes are valid, do nothing. Disposing/recreating
                // brushes against the same RT can hard-kill the dashboard during
                // playback transitions (historical→realtime). Gating on
                // _dxInitialized covers the case where a prior init failed mid-flight.
                if (RenderTarget != null
                    && _dxInitialized
                    && object.ReferenceEquals (RenderTarget, _lastSeenRenderTarget))
                    return;

                DisposeSharpDxResources ();   // sets _dxInitialized = false

                if (RenderTarget == null)
                    return;
                CreateSharpDxResources ();    // sets _dxInitialized = true on success
                _lastSeenRenderTarget = RenderTarget;
            }
            catch (Exception ex)
            {
                if (_hudErrors < 3)
                {
                    _hudErrors++;
                    if (EnableDebug)
                        Print ("[" + Name + "] HUD RTC err: " + ex.Message);
                }
            }
        }

        // Per-DashboardSize layout table. Body font is monospace (column alignment),
        // title font is proportional (Segoe UI Bold) — but we still budget BOX_W off
        // the body font since that's what drives the longest line.
        // Char width approx (Consolas): 10pt≈6, 11pt≈6.6, 12pt≈7.2, 14pt≈8.4, 17pt≈10.2
        // The widest content line is ~36 chars: "Total +$X,XXX.XX   Closed +$X,XXX.XX"
        private float HudBodyFontSize ()
        {
            switch (DashboardSize)
            {
                case GodZillaHudSize.Tiny:
                    return 10f;
                case GodZillaHudSize.Small:
                    return 11f;
                case GodZillaHudSize.Large:
                    return 14f;
                case GodZillaHudSize.Huge:
                    return 17f;
                default:
                    return 12f;
            }
        }
        private float HudTitleFontSize ()
        {
            switch (DashboardSize)
            {
                case GodZillaHudSize.Tiny:
                    return 12f;
                case GodZillaHudSize.Small:
                    return 13f;
                case GodZillaHudSize.Large:
                    return 17f;
                case GodZillaHudSize.Huge:
                    return 21f;
                default:
                    return 14f;
            }
        }
        private float HudBoxWidth ()
        {
            switch (DashboardSize)
            {
                case GodZillaHudSize.Tiny:
                    return 250f;
                case GodZillaHudSize.Small:
                    return 280f;
                case GodZillaHudSize.Large:
                    return 380f;
                case GodZillaHudSize.Huge:
                    return 460f;
                default:
                    return 320f;
            }
        }
        private float HudRowHeight ()
        {
            switch (DashboardSize)
            {
                case GodZillaHudSize.Tiny:
                    return 13f;
                case GodZillaHudSize.Small:
                    return 14f;
                case GodZillaHudSize.Large:
                    return 19f;
                case GodZillaHudSize.Huge:
                    return 22f;
                default:
                    return 16f;
            }
        }
        private float HudTitleHeight ()
        {
            switch (DashboardSize)
            {
                case GodZillaHudSize.Tiny:
                    return 18f;
                case GodZillaHudSize.Small:
                    return 20f;
                case GodZillaHudSize.Large:
                    return 25f;
                case GodZillaHudSize.Huge:
                    return 28f;
                default:
                    return 22f;
            }
        }

        // Builds (or rebuilds) just the TextFormats. Cheap — fonts are
        // recreated on size change without touching the brush set.
        private void EnsureDashboardFonts ()
        {
            if (_lastSizeApplied == DashboardSize && _dashFormat != null && _dashTitleFormat != null)
                return;

            try
            {
                if (_dashFormat != null)
                {
                    _dashFormat.Dispose ();
                    _dashFormat = null;
                }
            }
            catch { _dashFormat = null; }
            try
            {
                if (_dashTitleFormat != null)
                {
                    _dashTitleFormat.Dispose ();
                    _dashTitleFormat = null;
                }
            }
            catch { _dashTitleFormat = null; }

            var dwf = NinjaTrader.Core.Globals.DirectWriteFactory;

            _dashFormat = new SharpDX.DirectWrite.TextFormat (
                dwf, "Consolas", FontWeight.Normal, FontStyle.Normal, HudBodyFontSize ());
            _dashFormat.WordWrapping = SharpDX.DirectWrite.WordWrapping.NoWrap;
            _dashFormat.TextAlignment = SharpDX.DirectWrite.TextAlignment.Leading;
            _dashFormat.ParagraphAlignment = SharpDX.DirectWrite.ParagraphAlignment.Near;

            _dashTitleFormat = new SharpDX.DirectWrite.TextFormat (
                dwf, "Segoe UI", FontWeight.Bold, FontStyle.Normal, HudTitleFontSize ());
            _dashTitleFormat.WordWrapping = SharpDX.DirectWrite.WordWrapping.NoWrap;
            _dashTitleFormat.TextAlignment = SharpDX.DirectWrite.TextAlignment.Leading;
            _dashTitleFormat.ParagraphAlignment = SharpDX.DirectWrite.ParagraphAlignment.Near;

            _lastSizeApplied = DashboardSize;
        }

        private void CreateSharpDxResources ()
        {
            if (RenderTarget == null)
                return;

            EnsureDashboardFonts ();

            _bTextWhite = new SharpDX.Direct2D1.SolidColorBrush (RenderTarget, new Color4 (0.95f, 0.95f, 0.95f, 1f));
            _bTextDim = new SharpDX.Direct2D1.SolidColorBrush (RenderTarget, new Color4 (0.65f, 0.65f, 0.70f, 1f));
            _bTextGreen = new SharpDX.Direct2D1.SolidColorBrush (RenderTarget, new Color4 (0.20f, 1.00f, 0.30f, 1f));
            _bTextRed = new SharpDX.Direct2D1.SolidColorBrush (RenderTarget, new Color4 (1.00f, 0.30f, 0.30f, 1f));
            _bTextYellow = new SharpDX.Direct2D1.SolidColorBrush (RenderTarget, new Color4 (1.00f, 0.85f, 0.20f, 1f));
            _bTextOrange = new SharpDX.Direct2D1.SolidColorBrush (RenderTarget, new Color4 (1.00f, 0.55f, 0.10f, 1f));
            _bTextCyan = new SharpDX.Direct2D1.SolidColorBrush (RenderTarget, new Color4 (0.30f, 0.85f, 1.00f, 1f));
            _bBackground = new SharpDX.Direct2D1.SolidColorBrush (RenderTarget, new Color4 (0.05f, 0.06f, 0.10f, 0.86f));
            _bBorder = new SharpDX.Direct2D1.SolidColorBrush (RenderTarget, new Color4 (0.35f, 0.40f, 0.55f, 1.00f));
            _bBorderHot = new SharpDX.Direct2D1.SolidColorBrush (RenderTarget, new Color4 (1.00f, 0.55f, 0.10f, 1.00f));

            _dxInitialized = true;
        }

        private void DisposeSharpDxResources ()
        {
            _dxInitialized = false;
            try
            {
                if (_dashFormat != null)
                {
                    _dashFormat.Dispose ();
                    _dashFormat = null;
                }
            }
            catch { _dashFormat = null; }
            try
            {
                if (_dashTitleFormat != null)
                {
                    _dashTitleFormat.Dispose ();
                    _dashTitleFormat = null;
                }
            }
            catch { _dashTitleFormat = null; }
            try
            {
                if (_bTextWhite != null)
                {
                    _bTextWhite.Dispose ();
                    _bTextWhite = null;
                }
            }
            catch { _bTextWhite = null; }
            try
            {
                if (_bTextDim != null)
                {
                    _bTextDim.Dispose ();
                    _bTextDim = null;
                }
            }
            catch { _bTextDim = null; }
            try
            {
                if (_bTextGreen != null)
                {
                    _bTextGreen.Dispose ();
                    _bTextGreen = null;
                }
            }
            catch { _bTextGreen = null; }
            try
            {
                if (_bTextRed != null)
                {
                    _bTextRed.Dispose ();
                    _bTextRed = null;
                }
            }
            catch { _bTextRed = null; }
            try
            {
                if (_bTextYellow != null)
                {
                    _bTextYellow.Dispose ();
                    _bTextYellow = null;
                }
            }
            catch { _bTextYellow = null; }
            try
            {
                if (_bTextOrange != null)
                {
                    _bTextOrange.Dispose ();
                    _bTextOrange = null;
                }
            }
            catch { _bTextOrange = null; }
            try
            {
                if (_bTextCyan != null)
                {
                    _bTextCyan.Dispose ();
                    _bTextCyan = null;
                }
            }
            catch { _bTextCyan = null; }
            try
            {
                if (_bBackground != null)
                {
                    _bBackground.Dispose ();
                    _bBackground = null;
                }
            }
            catch { _bBackground = null; }
            try
            {
                if (_bBorder != null)
                {
                    _bBorder.Dispose ();
                    _bBorder = null;
                }
            }
            catch { _bBorder = null; }
            try
            {
                if (_bBorderHot != null)
                {
                    _bBorderHot.Dispose ();
                    _bBorderHot = null;
                }
            }
            catch { _bBorderHot = null; }
        }

        // OnRender — UI thread. NEVER touch bar series, Position, ATM APIs here
        // (they may throw on the UI thread). Read only from snapshot fields.
        protected override void OnRender (ChartControl chartControl, ChartScale chartScale)
        {
            try
            {
                base.OnRender (chartControl, chartScale);
            }
            catch { }

            if (RenderTarget == null)
                return;

            // User-disabled the HUD entirely — skip all SharpDX work. Stability mode.
            if (!ShowDashboard || DashboardPosition == HudCorner.Hidden)
                return;

            // Eager re-init: OnRenderTargetChanged may not have fired yet on
            // some NT8 builds (notably during playback restart), so brushes
            // can be null on the first frame. Build them now if missing.
            if (_dashFormat == null || _bTextWhite == null)
            {
                try
                {
                    CreateSharpDxResources ();
                }
                catch (Exception ex) { if (_hudErrors < 3) { _hudErrors++; if (EnableDebug) Print ("[" + Name + "] HUD lazy-init err: " + ex.Message); } }
                if (_dashFormat == null)
                    return;
            }

            try
            {
                // Snapshot copies (single ref read each — cannot be mid-mutated).
                string title       = _hudTitle;
                string version     = _hudVersion;
                string arm         = _hudArm;
                string sess        = _hudSession;
                string news        = _hudNews;
                string pnl         = _hudPnl;
                string pnlOpen     = _hudPnlOpen;
                string targets     = _hudTargets;
                string status      = _hudStatus;
                string last        = _hudLastTrade;
                string killBanner  = _hudKillBanner;
                bool   pnlPositive = _hudPnlPositive;
                bool   sessOutside = _hudSessionOutside;
                bool   killHit     = _hudKillHit;
                bool   killProfit  = _hudKillProfit;
                bool   newsBlocked = _hudNewsBlocked;
                bool   showNews    = _hudShowNews;
                bool   showSignals = _hudShowSignals;
                List<string> sigLines = _hudSignalLines;  // ref snapshot

                // Make sure the fonts match the current DashboardSize. Cheap when
                // size hasn't changed (single bool compare); rebuilds the two text
                // formats when the user picks a new preset at runtime.
                EnsureDashboardFonts ();

                // Per-size layout table — drives both font sizes (above) and the box
                // dimensions (below). Tightened from the old fixed 460px so the box
                // hugs the actual content width at each preset.
                const float PAD     = 8f;       // inner padding (was 10) — saves ~4px each side
                const float SEP_H   = 4f;
                float ROW_H   = HudRowHeight   ();
                float TITLE_H = HudTitleHeight ();
                float BOX_W   = HudBoxWidth    ();
                const float MARGIN_X = 18f;     // horizontal margin from chart edge
                const float MARGIN_Y = 35f;     // vertical margin from chart edge
                                                // (35f from bottom dodges the time axis;
                                                //  same value at top stays under axis labels)

                // Count rows
                int rows = 0;
                rows++; // title
                rows++; // separator
                rows++; // arm
                rows++; // session
                if (showNews && !string.IsNullOrEmpty (news))
                    rows++;
                rows++; // pnl
                if (!string.IsNullOrEmpty (pnlOpen))
                    rows++;
                rows++; // targets
                rows++; // status
                rows++; // last trade
                if (showSignals && sigLines != null && sigLines.Count > 0)
                    rows += sigLines.Count;

                float boxH = PAD * 2f + TITLE_H + SEP_H + ROW_H * (rows - 1) + 4f;

                Size2F rtSize = RenderTarget.Size;
                float bx, by;
                switch (DashboardPosition)
                {
                    case HudCorner.TopLeft:
                        bx = MARGIN_X;
                        by = MARGIN_Y;
                        break;
                    case HudCorner.TopRight:
                        bx = rtSize.Width - BOX_W - MARGIN_X;
                        by = MARGIN_Y;
                        break;
                    case HudCorner.BottomLeft:
                        bx = MARGIN_X;
                        by = rtSize.Height - boxH - MARGIN_Y;
                        break;
                    case HudCorner.Center:
                        bx = (rtSize.Width - BOX_W) * 0.5f;
                        by = (rtSize.Height - boxH) * 0.5f;
                        break;
                    case HudCorner.BottomRight:
                    default:
                        bx = rtSize.Width - BOX_W - MARGIN_X;
                        by = rtSize.Height - boxH - MARGIN_Y;
                        break;
                }
                // Clamp to chart bounds — never let the box leak off-screen on
                // very narrow/short charts (workspace tab tiles, etc.).
                if (bx < 4f)
                    bx = 4f;
                if (by < 4f)
                    by = 4f;
                if (bx + BOX_W > rtSize.Width - 4f)
                    bx = rtSize.Width - BOX_W - 4f;
                if (by + boxH > rtSize.Height - 4f)
                    by = rtSize.Height - boxH - 4f;

                RectangleF box = new RectangleF (bx, by, BOX_W, boxH);
                RenderTarget.FillRectangle (box, _bBackground);
                RenderTarget.DrawRectangle (box, killHit ? _bBorderHot : _bBorder, killHit ? 2f : 1f);

                float x = bx + PAD;
                float y = by + PAD;
                float w = BOX_W - PAD * 2f;

                // Title row
                string titleLine = "🐲 " + title + (string.IsNullOrEmpty (version) ? "" : "  v" + version);
                DrawHudLine (titleLine, x, y, w, TITLE_H, _bTextCyan, _dashTitleFormat);
                y += TITLE_H;

                // Thin separator
                RenderTarget.DrawLine (
                    new Vector2 (x, y + 1f),
                    new Vector2 (x + w, y + 1f),
                    _bBorder, 1f);
                y += SEP_H;

                // ARM line — green if enabled, dim if not
                DrawHudLine (arm, x, y, w, ROW_H, _strategyEnabled ? _bTextGreen : _bTextRed, _dashFormat);
                y += ROW_H;

                // Session
                DrawHudLine ("Session: " + sess, x, y, w, ROW_H,
                    sessOutside ? _bTextOrange : _bTextGreen, _dashFormat);
                y += ROW_H;

                // News (if enabled)
                if (showNews && !string.IsNullOrEmpty (news))
                {
                    DrawHudLine (news, x, y, w, ROW_H,
                        newsBlocked ? _bTextRed : _bTextDim, _dashFormat);
                    y += ROW_H;
                }

                // PnL totals
                DrawHudLine (pnl, x, y, w, ROW_H,
                    pnlPositive ? _bTextGreen : _bTextRed, _dashFormat);
                y += ROW_H;

                if (!string.IsNullOrEmpty (pnlOpen))
                {
                    DrawHudLine (pnlOpen, x, y, w, ROW_H, _bTextWhite, _dashFormat);
                    y += ROW_H;
                }

                // Targets
                DrawHudLine (targets, x, y, w, ROW_H, _bTextDim, _dashFormat);
                y += ROW_H;

                // Status
                SharpDX.Direct2D1.SolidColorBrush statusBrush;
                if (killHit)
                    statusBrush = killProfit ? _bTextGreen : _bTextRed;
                else if (status != null && status.StartsWith ("IN POSITION", StringComparison.Ordinal))
                    statusBrush = _bTextCyan;
                else
                    statusBrush = _bTextYellow;
                DrawHudLine (status, x, y, w, ROW_H, statusBrush, _dashFormat);
                y += ROW_H;

                // Last trade
                DrawHudLine (last, x, y, w, ROW_H, _bTextDim, _dashFormat);
                y += ROW_H;

                // Signal-tracking lines
                if (showSignals && sigLines != null)
                {
                    for (int i = 0; i < sigLines.Count; i++)
                    {
                        DrawHudLine (sigLines[i], x, y, w, ROW_H, _bTextWhite, _dashFormat);
                        y += ROW_H;
                    }
                }

                // Kill-switch banner overlay (centered atop chart)
                if (killHit && !string.IsNullOrEmpty (killBanner))
                {
                    var kb = new RectangleF (rtSize.Width * 0.18f, rtSize.Height * 0.42f,
                                              rtSize.Width * 0.64f, 60f);
                    RenderTarget.FillRectangle (kb, _bBackground);
                    RenderTarget.DrawRectangle (kb, killProfit ? _bTextGreen : _bTextRed, 2f);
                    RenderTarget.DrawText (killBanner, _dashTitleFormat,
                        new RectangleF (kb.X + 12f, kb.Y + 18f, kb.Width - 24f, kb.Height - 18f),
                        killProfit ? _bTextGreen : _bTextRed,
                        SharpDX.Direct2D1.DrawTextOptions.Clip,
                        SharpDX.Direct2D1.MeasuringMode.Natural);
                }

                if (!_hudFirstRenderLogged && EnableDebug)
                {
                    _hudFirstRenderLogged = true;
                    Print ("[" + Name + "] HUD: first frame rendered ("
                        + rtSize.Width.ToString ("F0") + "x" + rtSize.Height.ToString ("F0") + ")");
                }
            }
            catch (Exception ex)
            {
                if (_hudErrors < 3)
                {
                    _hudErrors++;
                    if (EnableDebug)
                        Print ("[" + Name + "] HUD render err: " + ex.Message);
                }
            }
        }

        private void DrawHudLine (string text, float x, float y, float w, float h,
                                   SharpDX.Direct2D1.SolidColorBrush brush,
                                   SharpDX.DirectWrite.TextFormat format)
        {
            RectangleF rect = new RectangleF (x, y, w, h + 4f);
            RenderTarget.DrawText (text ?? "", format, rect, brush,
                SharpDX.Direct2D1.DrawTextOptions.Clip,
                SharpDX.Direct2D1.MeasuringMode.Natural);
        }

        private void UpdateNormalizedSignalSeriesAndDrawSignalArrows ()
        {
            try
            {
                double koRaw = _king != null ? SafeSignalRead (() => _king.Signal_Trade[0], "KO") : 0.0;
                double paRaw = _pana != null ? SafeSignalRead (() => _pana.Signal_Trade[0], "PA") : 0.0;
                double thRaw = _thunder != null ? SafeSignalRead (() => _thunder.Signal_Trade[0], "TH") : 0.0;
                double sjRaw = _sjb != null ? SafeSignalRead (() => _sjb.Signal_Trade[0], "SJ") : 0.0;
                double suRaw = _sumo != null ? SafeSignalRead (() => _sumo.Signal_Trade[0], "SU") : 0.0;

                double ko = ComputeSignal (UseKOSignals, koRaw, KO_LongValue, KO_ShortValue);
                double pa = ComputeSignal (UsePASignals, paRaw, PA_LongValue, PA_ShortValue);
                double th = ComputeSignal (UseTHSignals, thRaw, TH_LongValue, TH_ShortValue);
                double sj = ComputeSignal (UseSJSignals, sjRaw, SJ_LongValue, SJ_ShortValue);
                double su = ComputeSignal (UseSUSignals, suRaw, SU_LongValue, SU_ShortValue);

                if (_koSignalSeries != null)
                    _koSignalSeries[0] = ko;
                if (_paSignalSeries != null)
                    _paSignalSeries[0] = pa;
                if (_thSignalSeries != null)
                    _thSignalSeries[0] = th;
                if (_sjSignalSeries != null)
                    _sjSignalSeries[0] = sj;
                if (_suSignalSeries != null)
                    _suSignalSeries[0] = su;

                if (!SignalVisualFilterPassed (ko))
                    ko = 0;

                if (!SignalVisualFilterPassed (pa))
                    pa = 0;

                if (!SignalVisualFilterPassed (th))
                    th = 0;

                if (!SignalVisualFilterPassed (sj))
                    sj = 0;

                if (!SignalVisualFilterPassed (su))
                    su = 0;

                DrawSignalArrow ("GZ_KO_SIG_", ko, UseKOSignals && ShowKOSignalArrows, KOSignalArrowBrush, 0, ShowKOSignalArrowLabels, KOSignalArrowText);
                DrawSignalArrow ("GZ_PA_SIG_", pa, UsePASignals && ShowPASignalArrows, PASignalArrowBrush, 2, ShowPASignalArrowLabels, PASignalArrowText);
                DrawSignalArrow ("GZ_TH_SIG_", th, UseTHSignals && ShowTHSignalArrows, THSignalArrowBrush, 4, ShowTHSignalArrowLabels, THSignalArrowText);
                DrawSignalArrow ("GZ_SJ_SIG_", sj, UseSJSignals && ShowSJSignalArrows, SJSignalArrowBrush, 6, ShowSJSignalArrowLabels, SJSignalArrowText);
                DrawSignalArrow ("GZ_SU_SIG_", su, UseSUSignals && ShowSUSignalArrows, SUSignalArrowBrush, 8, ShowSUSignalArrowLabels, SUSignalArrowText);

                int groupSize = 0;
                int groupSignal = GetSameBarGroupTriggerSignal (koRaw, paRaw, thRaw, sjRaw, suRaw, ko, pa, th, sj, su, out groupSize);

                SetSignalBackBrush (groupSignal);

                if (ShowGroupTriggerArrows)
                {
                    if (groupSignal != 0 && groupSize > 0)
                        DrawSignalArrow ("GZ_GROUP_" + groupSize + "_", groupSignal, true, GroupTriggerBrush, 12, ShowGroupTriggerArrowLabel, BuildGroupTriggerArrowLabel (groupSize));
                }
            }
            catch (Exception ex)
            {
                if (EnableDebug)
                    Print ($"{Time[0]} | UpdateNormalizedSignalSeriesAndDrawSignalArrows ERROR | Bar={CurrentBar} | Error={ex.Message}");
            }
        }

        private bool SignalVisualFilterPassed (double signal)
        {
            if (signal == 0)
                return false;

            if (EnableNewsFilter && IsNewsTradingBlocked ())
                return false;

            if (EnableEmaFilter && _emaShortFilter != null && _emaLongFilter != null && CurrentBar >= Math.Max (EmaShortPeriod, EmaLongPeriod))
            {
                if (signal > 0 && _emaShortFilter[0] <= _emaLongFilter[0])
                    return false;

                if (signal < 0 && _emaShortFilter[0] >= _emaLongFilter[0])
                    return false;
            }

            return true;
        }
        private double SafeSignalRead (Func<double> getter, string sourceName)
        {
            try
            {
                return getter != null ? getter () : 0.0;
            }
            catch (Exception ex)
            {
                if (EnableDebug)
                    Print ($"{Time[0]} | SafeSignalRead ERROR | Source={sourceName} | Bar={CurrentBar} | Error={ex.Message}");

                return 0.0;
            }
        }

        private string BuildGroupTriggerArrowLabel (int groupSize)
        {
            string label = string.IsNullOrWhiteSpace (GroupTriggerArrowText) ? "GODZILLA " : GroupTriggerArrowText;

            if (label.Contains ("{0}"))
                return string.Format (label, groupSize);

            return label + groupSize.ToString ();
        }

        private void DrawSignalArrow (string tagPrefix, double signal, bool draw, Brush brush, int extraOffsetTicks, bool showLabel, string labelText)
        {
            if (!draw || signal == 0 || brush == null)
                return;

            if (CurrentBar < 0 || TickSize <= 0)
                return;

            if (double.IsNaN (High[0]) || double.IsNaN (Low[0]))
                return;

            int arrowOffsetTicks = Math.Max (0, ArrowOffset) + Math.Max (0, extraOffsetTicks);
            int textOffsetTicks = Math.Max (1, SignalArrowTextOffsetTicks);

            double arrowOffset = arrowOffsetTicks * TickSize;
            double labelOffset = (arrowOffsetTicks + textOffsetTicks) * TickSize;

            string tag = tagPrefix + CurrentBar;
            string textTag = tagPrefix + "TXT_" + CurrentBar;

            try
            {
                if (signal > 0)
                {
                    Draw.ArrowUp (this, tag, false, 0, Low[0] - arrowOffset, brush);
                    DrawSignalArrowLabel (textTag, showLabel, labelText, Low[0] - labelOffset, brush);
                }
                else if (signal < 0)
                {
                    Draw.ArrowDown (this, tag, false, 0, High[0] + arrowOffset, brush);
                    DrawSignalArrowLabel (textTag, showLabel, labelText, High[0] + labelOffset, brush);
                }
            }
            catch (Exception ex)
            {
                if (EnableDebug)
                    Print ($"{Time[0]} | DrawSignalArrow ERROR | Tag={tagPrefix} | Signal={signal} | ArrowOffset={ArrowOffset} | TextOffset={SignalArrowTextOffsetTicks} | Error={ex.Message}");
            }
        }

        private void DrawSignalArrowLabel (string tag, bool showLabel, string labelText, double price, Brush brush)
        {
            if (!showLabel || string.IsNullOrWhiteSpace (labelText) || brush == null)
                return;

            if (double.IsNaN (price) || double.IsInfinity (price))
                return;

            SimpleFont font = signalArrowFont ?? new SimpleFont ("Arial", 10) { Bold = true };

            try
            {
                Draw.Text (this, tag, false, labelText, 0, price, 0, brush, font, TextAlignment.Center, Brushes.Transparent, Brushes.Transparent, 0);
            }
            catch (Exception ex)
            {
                if (EnableDebug)
                    Print ($"{Time[0]} | DrawSignalArrowLabel ERROR | Tag={tag} | Text={labelText} | Price={price} | Error={ex.Message}");
            }
        }

        private void SetSignalBackBrush (int groupSignal)
        {
            if (BarsInProgress != 0 || CurrentBar < 0)
                return;

            try
            {
                if (EnableGroupTriggerBackBrush && groupSignal != 0 && GroupTriggerBackBrush != null)
                    BackBrushes[0] = GroupTriggerBackBrush;
            }
            catch (Exception ex)
            {
                if (EnableDebug)
                    Print ($"{Time[0]} | SetSignalBackBrush ERROR | Bar={CurrentBar} | Error={ex.Message}");
            }
        }

        private int GetSameBarGroupTriggerSignal (double koRaw, double paRaw, double thRaw, double sjRaw, double suRaw, double ko, double pa, double th, double sj, double su, out int groupSize)
        {
            groupSize = 0;

            GroupTriggerResult primaryGroup = EvaluatePrimaryGroupTriggerSet (ko == 1 && UseKOSignals, pa == 1 && UsePASignals, th == 1 && UseTHSignals, sj == 1 && UseSJSignals, su == 1 && UseSUSignals, ko == -1 && UseKOSignals, pa == -1 && UsePASignals, th == -1 && UseTHSignals, sj == -1 && UseSJSignals, su == -1 && UseSUSignals);
            GroupTriggerResult secondaryGroup = EvaluateSecondaryGroupTriggerSet (koRaw, paRaw, thRaw, sjRaw, suRaw);

            bool goLong = (primaryGroup != null && primaryGroup.Long) || (secondaryGroup != null && secondaryGroup.Long);
            bool goShort = (primaryGroup != null && primaryGroup.Short) || (secondaryGroup != null && secondaryGroup.Short);

            if (goLong && goShort)
                return 0;

            if (primaryGroup != null && primaryGroup.Long)
            {
                groupSize = primaryGroup.GroupSize;
                return 1;
            }
            if (secondaryGroup != null && secondaryGroup.Long)
            {
                groupSize = secondaryGroup.GroupSize;
                return 1;
            }
            if (primaryGroup != null && primaryGroup.Short)
            {
                groupSize = primaryGroup.GroupSize;
                return -1;
            }
            if (secondaryGroup != null && secondaryGroup.Short)
            {
                groupSize = secondaryGroup.GroupSize;
                return -1;
            }

            return 0;
        }

        private GroupTriggerResult EvaluatePrimaryGroupTriggerSet (bool koLong, bool paLong, bool thLong, bool sjLong, bool suLong, bool koShort, bool paShort, bool thShort, bool sjShort, bool suShort)
        {
            GroupTriggerResult result = new GroupTriggerResult ();
            result.TriggerName = "SET1";

            if (!IsPrimaryGroupModeActive ())
                return result;

            int enabledCount = CountEnabledSignals ();
            int needed = Math.Min (Math.Max (1, GroupTriggerSet1RequiredCount), enabledCount);
            int longAgree = 0, shortAgree = 0;

            if (UseKOSignals)
            {
                result.UsesKO = true;
                if (koLong)
                    longAgree++;
                else if (koShort)
                    shortAgree++;
            }
            if (UsePASignals)
            {
                result.UsesPA = true;
                if (paLong)
                    longAgree++;
                else if (paShort)
                    shortAgree++;
            }
            if (UseTHSignals)
            {
                result.UsesTH = true;
                if (thLong)
                    longAgree++;
                else if (thShort)
                    shortAgree++;
            }
            if (UseSJSignals)
            {
                result.UsesSJ = true;
                if (sjLong)
                    longAgree++;
                else if (sjShort)
                    shortAgree++;
            }
            if (UseSUSignals)
            {
                result.UsesSU = true;
                if (suLong)
                    longAgree++;
                else if (suShort)
                    shortAgree++;
            }

            if (longAgree >= needed && shortAgree >= needed)
                return result;

            if (longAgree >= needed)
            {
                result.Long = true;
                result.GroupSize = needed;
            }
            else if (shortAgree >= needed)
            {
                result.Short = true;
                result.GroupSize = needed;
            }

            return result;
        }

        private GroupTriggerResult EvaluateSecondaryGroupTriggerSet (double koRaw, double paRaw, double thRaw, double sjRaw, double suRaw)
        {
            GroupTriggerResult result = new GroupTriggerResult ();
            result.TriggerName = "SET2";

            if (!IsSecondaryGroupModeActive ())
                return result;

            int enabledCount = CountEnabledGroupTriggerSet2Signals ();
            int needed = Math.Min (Math.Max (1, GroupTriggerSet2RequiredCount), enabledCount);
            int longAgree = 0, shortAgree = 0;

            if (G2_UseKOSignals)
            {
                result.UsesKO = true;
                int signal = ComputeSignal (true, koRaw, G2_KO_LongValue, G2_KO_ShortValue);
                if (signal > 0)
                    longAgree++;
                else if (signal < 0)
                    shortAgree++;
            }
            if (G2_UsePASignals)
            {
                result.UsesPA = true;
                int signal = ComputeSignal (true, paRaw, G2_PA_LongValue, G2_PA_ShortValue);
                if (signal > 0)
                    longAgree++;
                else if (signal < 0)
                    shortAgree++;
            }
            if (G2_UseTHSignals)
            {
                result.UsesTH = true;
                int signal = ComputeSignal (true, thRaw, G2_TH_LongValue, G2_TH_ShortValue);
                if (signal > 0)
                    longAgree++;
                else if (signal < 0)
                    shortAgree++;
            }
            if (G2_UseSJSignals)
            {
                result.UsesSJ = true;
                int signal = ComputeSignal (true, sjRaw, G2_SJ_LongValue, G2_SJ_ShortValue);
                if (signal > 0)
                    longAgree++;
                else if (signal < 0)
                    shortAgree++;
            }
            if (G2_UseSUSignals)
            {
                result.UsesSU = true;
                int signal = ComputeSignal (true, suRaw, G2_SU_LongValue, G2_SU_ShortValue);
                if (signal > 0)
                    longAgree++;
                else if (signal < 0)
                    shortAgree++;
            }

            if (longAgree >= needed && shortAgree >= needed)
                return result;

            if (longAgree >= needed)
            {
                result.Long = true;
                result.GroupSize = needed;
            }
            else if (shortAgree >= needed)
            {
                result.Short = true;
                result.GroupSize = needed;
            }

            return result;
        }

        private bool IsPrimaryGroupModeActive ()
        {
            int enabledSignalCount = CountEnabledSignals ();

            return enabledSignalCount >= 1 && GroupTriggerSet1RequiredCount >= 1 && GroupTriggerSet1RequiredCount <= enabledSignalCount;
        }

        private bool IsSecondaryGroupModeActive ()
        {
            if (!EnableGroupTriggerSet2)
                return false;

            int enabledCount = CountEnabledGroupTriggerSet2Signals ();

            return enabledCount >= 1 && GroupTriggerSet2RequiredCount >= 1 && GroupTriggerSet2RequiredCount <= enabledCount;
        }

        private int ComputeSignal (bool enabled, double value, int longValue, int shortValue)
        {
            if (!enabled)
                return 0;
            if (value >= longValue && longValue != 0)
                return 1;
            if (value <= shortValue && shortValue != 0)
                return -1;
            return 0;
        }

        private void SetActiveTradeSignalSources (bool useKO, bool usePA, bool useTH, bool useSJ, bool useSU, MarketPosition direction, int groupSize = 0, string groupName = "")
        {
            activeTradeUsesKO = useKO;
            activeTradeUsesPA = usePA;
            activeTradeUsesTH = useTH;
            activeTradeUsesSJ = useSJ;
            activeTradeUsesSU = useSU;
            activeTradeDirection = direction;
            activeTradeGroupSize = groupSize;
            activeTradeGroupName = groupName ?? string.Empty;
        }

        private void ClearActiveTradeSignalSources ()
        {
            activeTradeUsesKO = false;
            activeTradeUsesPA = false;
            activeTradeUsesTH = false;
            activeTradeUsesSU = false;
            activeTradeUsesSJ = false;
            activeTradeDirection = MarketPosition.Flat;
            activeTradeGroupSize = 0;
            activeTradeGroupName = string.Empty;
        }

        private void UpdateSignalTrackingOnTradeClose (double tradePnl)
        {
            if (!EnableSignalTracking)
                return;

            bool isWinner = tradePnl > 0;
            bool isLoser = tradePnl < 0;

            if (activeTradeUsesKO)
                IncrementSignalStats (koStats, activeTradeDirection, isWinner, isLoser);

            if (activeTradeUsesPA)
                IncrementSignalStats (paStats, activeTradeDirection, isWinner, isLoser);

            if (activeTradeUsesTH)
                IncrementSignalStats (thStats, activeTradeDirection, isWinner, isLoser);

            if (activeTradeUsesSU)
                IncrementSignalStats (suStats, activeTradeDirection, isWinner, isLoser);

            if (activeTradeUsesSJ)
                IncrementSignalStats (sjStats, activeTradeDirection, isWinner, isLoser);

            if (activeTradeGroupSize > 0 && string.Equals (activeTradeGroupName, "SET1", StringComparison.OrdinalIgnoreCase))
                IncrementSignalStats (groupSet1Stats, activeTradeDirection, isWinner, isLoser);
            else if (activeTradeGroupSize > 0 && string.Equals (activeTradeGroupName, "SET2", StringComparison.OrdinalIgnoreCase))
                IncrementSignalStats (groupSet2Stats, activeTradeDirection, isWinner, isLoser);
        }

        private void IncrementSignalStats (SignalTradeStats stats, MarketPosition direction, bool isWinner, bool isLoser)
        {
            stats.TotalTrades++;

            if (direction == MarketPosition.Long)
                stats.LongTrades++;
            else if (direction == MarketPosition.Short)
                stats.ShortTrades++;

            if (isWinner)
                stats.Winners++;
            else if (isLoser)
                stats.Losers++;
        }

        private void UpdateLastTradeClosedSummary (double tradePnl)
        {
            if (!EnableSignalTracking)
                return;

            string dir = activeTradeDirection == MarketPosition.Long ? "Long" : activeTradeDirection == MarketPosition.Short ? "Short" : "Flat";
            string sig = BuildActiveSignalAbbreviationList();
            lastTradeClosedSummary = $"Last: {tradePnl:C} | {dir} | {sig}";
        }

        private void PrintSignalTrackingOnTradeClose (double tradePnl)
        {
            if (!EnableSignalTracking)
                return;

            string tradeSignalText = BuildActiveSignalListForPrint();
            string statsText = BuildSignalTrackingDisplayText();

            // Removed the invalid .Replace("", Environment.NewLine) call
            if (EnableDebug)
                Print ($"Trade Closed | Last ATM P/L: {tradePnl:C} | Direction: {activeTradeDirection} | Signals: {tradeSignalText}{statsText}");
        }

        private string BuildActiveSignalListForPrint ()
        {
            return BuildSignalTriggerName (activeTradeUsesKO, activeTradeUsesPA, activeTradeUsesTH, activeTradeUsesSJ, activeTradeUsesSU, activeTradeGroupSize, activeTradeGroupName);
        }

        private string BuildActiveSignalAbbreviationList ()
        {
            return BuildSignalTriggerName (activeTradeUsesKO, activeTradeUsesPA, activeTradeUsesTH, activeTradeUsesSJ, activeTradeUsesSU, activeTradeGroupSize, activeTradeGroupName);
        }

        private string BuildSignalTrackingDisplayText ()
        {
            if (!EnableSignalTracking)
                return string.Empty;

            List<string> lines = BuildSignalTrackingDisplayLines ();

            return lines.Count > 0
                ? Environment.NewLine + string.Join (Environment.NewLine, lines)
                : string.Empty;
        }

        private List<string> BuildSignalTrackingDisplayLines ()
        {
            List<string> enabledSignals = new List<string>();
            List<string> enabledGroups = new List<string>();
            List<string> lines = new List<string>();

            if (UseKOSignals)
            {
                enabledSignals.Add ("KO");
                lines.Add ($"KO T:{koStats.TotalTrades} Lg:{koStats.LongTrades} Sh:{koStats.ShortTrades} W:{koStats.Winners} L:{koStats.Losers}");
            }

            if (UsePASignals)
            {
                enabledSignals.Add ("PA");
                lines.Add ($"PA T:{paStats.TotalTrades} Lg:{paStats.LongTrades} Sh:{paStats.ShortTrades} W:{paStats.Winners} L:{paStats.Losers}");
            }

            if (UseTHSignals)
            {
                enabledSignals.Add ("TH");
                lines.Add ($"TH T:{thStats.TotalTrades} Lg:{thStats.LongTrades} Sh:{thStats.ShortTrades} W:{thStats.Winners} L:{thStats.Losers}");
            }

            if (UseSJSignals)
            {
                enabledSignals.Add ("SJ");
                lines.Add ($"SJ T:{sjStats.TotalTrades} Lg:{sjStats.LongTrades} Sh:{sjStats.ShortTrades} W:{sjStats.Winners} L:{sjStats.Losers}");
            }

            if (UseSUSignals)
            {
                enabledSignals.Add ("SU");
                lines.Add ($"SU T:{suStats.TotalTrades} Lg:{suStats.LongTrades} Sh:{suStats.ShortTrades} W:{suStats.Winners} L:{suStats.Losers}");
            }

            lines.Insert (0, enabledSignals.Count > 0 ? "Enabled Signals: " + string.Join (", ", enabledSignals) : "Enabled Signals: None");

            if (IsPrimaryGroupModeActive ())
                enabledGroups.Add ("SET1-G" + GroupTriggerSet1RequiredCount.ToString ());
            if (IsSecondaryGroupModeActive ())
                enabledGroups.Add ("SET2-G" + GroupTriggerSet2RequiredCount.ToString ());

            if (enabledGroups.Count > 0)
            {
                lines.Add ("Enabled Groups: " + string.Join (", ", enabledGroups));

                if (IsPrimaryGroupModeActive ())
                    lines.Add ($"SET1-G{GroupTriggerSet1RequiredCount} T:{groupSet1Stats.TotalTrades} Lg:{groupSet1Stats.LongTrades} Sh:{groupSet1Stats.ShortTrades} W:{groupSet1Stats.Winners} L:{groupSet1Stats.Losers}");

                if (IsSecondaryGroupModeActive ())
                    lines.Add ($"SET2-G{GroupTriggerSet2RequiredCount} T:{groupSet2Stats.TotalTrades} Lg:{groupSet2Stats.LongTrades} Sh:{groupSet2Stats.ShortTrades} W:{groupSet2Stats.Winners} L:{groupSet2Stats.Losers}");
            }

            return lines;
        }

        private bool CheckTradingTimeframes (int currentTime)
        {
            bool anyTradingWindowEnabled = EnableTF1 || EnableTF2 || EnableTF3;

            bool tf1 = EnableTF1 && IsTimeInWindow (currentTime, ToTime (StartTime1), ToTime (EndTime1));
            bool tf2 = EnableTF2 && IsTimeInWindow (currentTime, ToTime (StartTime2), ToTime (EndTime2));
            bool tf3 = EnableTF3 && IsTimeInWindow (currentTime, ToTime (StartTime3), ToTime (EndTime3));

            bool allowedByTradingWindows = !anyTradingWindowEnabled || tf1 || tf2 || tf3;

            if (!allowedByTradingWindows)
                return false;

            if (EnableSkipTimeWindow && IsTimeInWindow (currentTime, ToTime (SkipStartTime), ToTime (SkipEndTime)))
                return false;

            return true;
        }

        private void PrintSessionEndPnlOrQueue (string sessionLabel, bool willFlatten)
        {
            string printKey = sessionLabel + "_" + Time[0].ToString ("yyyyMMdd_HHmmss");

            if (printKey == lastSessionPnlPrintKey)
                return;

            lastSessionPnlPrintKey = printKey;

            bool hasOpenPosition = false;

            try
            {
                hasOpenPosition = Position != null && Position.MarketPosition != MarketPosition.Flat;
            }
            catch
            {
                hasOpenPosition = false;
            }

            if (willFlatten && hasOpenPosition)
            {
                pendingSessionPnlPrint = true;
                pendingSessionPnlPrintLabel = sessionLabel;
                pendingSessionPnlPrintTime = Time[0];

                if (EnableDebug)
                    Print ($"[{Name}] SESSION PNL PRINT QUEUED | Session={sessionLabel} | Time={Time[0]:yyyy-MM-dd HH:mm:ss} | Waiting for flatten/close processing");

                return;
            }

            PrintSessionPnlSummary (sessionLabel, "SESSION END");
        }

        private void ProcessPendingSessionPnlPrint ()
        {
            if (!pendingSessionPnlPrint)
                return;

            bool isFlat = true;

            try
            {
                isFlat = Position == null || Position.MarketPosition == MarketPosition.Flat;
            }
            catch
            {
                isFlat = true;
            }

            if (!isFlat)
                return;

            PrintSessionPnlSummary (pendingSessionPnlPrintLabel, "SESSION END AFTER FLATTEN");

            pendingSessionPnlPrint = false;
            pendingSessionPnlPrintLabel = string.Empty;
            pendingSessionPnlPrintTime = Core.Globals.MinDate;
        }

        private void PrintSessionPnlSummary (string sessionLabel, string reason)
        {
            double dailyOpenPnl = UseUnrealizedPnl ? dailyUnrealizedPnL : 0.0;
            double dailyTotalPnl = dailyRealizedPnL + dailyOpenPnl;

            string lastTrade = string.IsNullOrEmpty (lastTradeClosedSummary)
        ? "Last Trade: —"
        : "Last Trade: " + lastTradeClosedSummary;

            Print ("");
            Print ($"[{Name}] ===== {reason} PNL SUMMARY =====");
            Print ($"[{Name}] Session={sessionLabel} | Time={Time[0]:yyyy-MM-dd HH:mm:ss} | OrderMode={OrderMode}");
            Print ($"[{Name}] {lastTrade}");
            Print ($"[{Name}] Daily Closed PnL={dailyRealizedPnL:F2} | Daily Open PnL={dailyUnrealizedPnL:F2} | Daily Total PnL={dailyTotalPnl:F2}");
            Print ($"[{Name}] Total Running PnL={totalRunningPnL:F2} | DailyLimitHit={dailyLimitHit} | Status={dailyPnlStatusMessage}");
            Print ($"[{Name}] =================================");
            Print ("");
        }

        private void CheckFlattenTimeframes (int currentTime)
        {
            // If no TF filters are enabled, do not do any TF-based flattening/session-end printing
            if (!EnableTF1 && !EnableTF2 && !EnableTF3)
                return;

            if (CurrentBar < 1)
                return;

            int previousTime = ToTime (Time[1]);

            bool ended1 = EnableTF1
        && IsTimeInWindow (previousTime, ToTime (StartTime1), ToTime (EndTime1))
        && !IsTimeInWindow (currentTime, ToTime (StartTime1), ToTime (EndTime1));

            bool ended2 = EnableTF2
        && IsTimeInWindow (previousTime, ToTime (StartTime2), ToTime (EndTime2))
        && !IsTimeInWindow (currentTime, ToTime (StartTime2), ToTime (EndTime2));

            bool ended3 = EnableTF3
        && IsTimeInWindow (previousTime, ToTime (StartTime3), ToTime (EndTime3))
        && !IsTimeInWindow (currentTime, ToTime (StartTime3), ToTime (EndTime3));

            bool flatten1 = ended1 && FlattenTF1;
            bool flatten2 = ended2 && FlattenTF2;
            bool flatten3 = ended3 && FlattenTF3;

            if (ended1)
                PrintSessionEndPnlOrQueue ("TF1", flatten1);

            if (ended2)
                PrintSessionEndPnlOrQueue ("TF2", flatten2);

            if (ended3)
                PrintSessionEndPnlOrQueue ("TF3", flatten3);

            if (flatten1 || flatten2 || flatten3)
                FlattenEverything ("Trading window closed");
        }

        private bool IsTimeInWindow (int currentTime, int startTime, int endTime)
        {
            // Treat equal start/end as always on
            if (startTime == endTime)
                return true;

            // Normal same-day window
            if (startTime < endTime)
                return currentTime >= startTime && currentTime < endTime;

            // Overnight window, ex: 180000 -> 040000
            return currentTime >= startTime || currentTime < endTime;
        }

        private MarketPosition GetCurrentTradePosition ()
        {
            try
            {
                if (OrderMode == OrderManagementMode.AtmStrategy && !string.IsNullOrEmpty (atmStrategyId))
                    return GetAtmStrategyMarketPosition (atmStrategyId);
            }
            catch { }

            try
            {
                return Position != null ? Position.MarketPosition : MarketPosition.Flat;
            }
            catch { }

            return MarketPosition.Flat;
        }

        private string BuildSignalTriggerName (bool useKO, bool usePA, bool useTH, bool useSJ, bool useSU, int groupSize, string groupName = "")
        {
            string trigger = string.Join("+", new[] {
        useKO ? "KO" : null,
        usePA ? "PA" : null,
        useTH ? "TH" : null,
        useSJ ? "SJ" : null,
        useSU ? "SU" : null,
    }.Where(s => s != null));

            if (string.IsNullOrEmpty (trigger))
                trigger = "None";

            if (groupSize > 0)
            {
                string cleanGroupName = string.IsNullOrWhiteSpace (groupName) ? "GROUP" : groupName.Trim ().ToUpperInvariant ();
                trigger = cleanGroupName + "-G" + groupSize + ":" + trigger;
            }

            return trigger;
        }

        private void QueuePendingReverse (MarketPosition direction, bool useKO, bool usePA, bool useTH, bool useSJ, bool useSU, int groupSize, string groupName)
        {
            pendingReverseActive = true;
            pendingReverseDirection = direction;
            pendingReverseUsesKO = useKO;
            pendingReverseUsesPA = usePA;
            pendingReverseUsesTH = useTH;
            pendingReverseUsesSJ = useSJ;
            pendingReverseUsesSU = useSU;
            pendingReverseGroupSize = groupSize;
            pendingReverseGroupName = groupName ?? string.Empty;

            if (EnableDebug)
                Print ($"{Time[0]} | REVERSE QUEUED | Direction={direction} | Trigger={BuildSignalTriggerName (useKO, usePA, useTH, useSJ, useSU, groupSize, groupName)}");
        }

        private void ClearPendingReverse ()
        {
            pendingReverseActive = false;
            pendingReverseDirection = MarketPosition.Flat;
            pendingReverseUsesKO = false;
            pendingReverseUsesPA = false;
            pendingReverseUsesTH = false;
            pendingReverseUsesSJ = false;
            pendingReverseUsesSU = false;
            pendingReverseGroupSize = 0;
            pendingReverseGroupName = string.Empty;
        }

        private bool HasPendingEntryOrder ()
        {
            if (OrderMode == OrderManagementMode.FixedTicks)
            {
                if (State == State.Historical)
                    return false;

                return !string.IsNullOrEmpty (fixedEntrySignalName) && !fixedPositionConfirmed;
            }

            return orderId.Length > 0;
        }

        private bool HasActiveEntryState ()
        {
            if (OrderMode == OrderManagementMode.FixedTicks)
            {
                if (State == State.Historical)
                    return Position.MarketPosition != MarketPosition.Flat;

                if (Position.MarketPosition == MarketPosition.Flat
                    && !string.IsNullOrEmpty (fixedEntrySignalName)
                    && fixedPositionConfirmed
                    && fixedTradeCloseProcessed)
                    return false;

                return !string.IsNullOrEmpty (fixedEntrySignalName)
                    || fixedPositionConfirmed
                    || Position.MarketPosition != MarketPosition.Flat;
            }

            return orderId.Length > 0 || atmStrategyId.Length > 0;
        }

        private void SubmitTradeEntry (bool isLong, bool useKO, bool usePA, bool useTH, bool useSJ, bool useSU, int groupSize, string groupName, string reason)
        {
            if (OrderMode == OrderManagementMode.FixedTicks)
                SubmitFixedEntry (isLong, useKO, usePA, useTH, useSJ, useSU, groupSize, groupName, reason);
            else
                SubmitAtmEntry (isLong, useKO, usePA, useTH, useSJ, useSU, groupSize, groupName, reason);
        }

        private void SanityCheckFixedTicksState ()
        {
            if (OrderMode != OrderManagementMode.FixedTicks)
                return;

            if (State != State.Realtime && State != State.Historical)
                return;

            DateTime nowUtc = DateTime.UtcNow;

            if ((nowUtc - lastFixedStateSanityCheckUtc).TotalSeconds < FixedStateSanityCheckSeconds)
                return;

            lastFixedStateSanityCheckUtc = nowUtc;

            if (!IsFixedStrategyFlat ())
                return;

            bool fixedStateStale =
        !string.IsNullOrEmpty (fixedEntrySignalName)
        || fixedEntryDirection != MarketPosition.Flat
        || fixedPositionConfirmed
        || fixedTradeCloseProcessed
        || fixedEntryAvgPrice != 0.0
        || fixedEntryQty != 0
        || fixedBreakevenMoved;

            bool fixedMartingaleStateStale =
        martingaleRecoveryActive
        || !string.IsNullOrEmpty (fixedMartingaleSignalName)
        || fixedMartingaleDirection != MarketPosition.Flat
        || fixedMartingalePositionConfirmed
        || fixedMartingaleCloseProcessed
        || fixedMartingaleEntryAvgPrice != 0.0
        || fixedMartingaleEntryQty != 0
        || fixedMartingaleBreakevenMoved;

            if (!fixedStateStale && !fixedMartingaleStateStale)
                return;

            double fallbackExitPrice = GetFixedExitFallbackPrice ();
            DateTime fallbackExitTime = GetFixedExitFallbackTime ();

            if (!string.IsNullOrEmpty (fixedEntrySignalName)
                && fixedPositionConfirmed
                && !fixedTradeCloseProcessed
                && fixedEntryDirection != MarketPosition.Flat
                && fixedEntryAvgPrice > 0
                && fallbackExitPrice > 0)
            {
                if (EnableDebug)
                    Print ($"[{Name}] FIXEDTICKS STALE CLOSE BOOKING | Signal={fixedEntrySignalName} | Direction={fixedEntryDirection} | Entry={fixedEntryAvgPrice:F2} | Exit={fallbackExitPrice:F2}");

                ProcessFixedTradeClosed (fallbackExitPrice, fallbackExitTime);
            }

            if (!string.IsNullOrEmpty (fixedMartingaleSignalName)
                && fixedMartingalePositionConfirmed
                && !fixedMartingaleCloseProcessed
                && fixedMartingaleDirection != MarketPosition.Flat
                && fixedMartingaleEntryAvgPrice > 0
                && fallbackExitPrice > 0)
            {
                if (EnableDebug)
                    Print ($"[{Name}] FIXEDTICKS STALE MARTINGALE CLOSE BOOKING | Signal={fixedMartingaleSignalName} | Direction={fixedMartingaleDirection} | Entry={fixedMartingaleEntryAvgPrice:F2} | Exit={fallbackExitPrice:F2}");

                ProcessFixedMartingaleClosed (fallbackExitPrice, fallbackExitTime);
            }

            bool stillStale =
        !string.IsNullOrEmpty (fixedEntrySignalName)
        || fixedEntryDirection != MarketPosition.Flat
        || fixedPositionConfirmed
        || fixedTradeCloseProcessed
        || fixedEntryAvgPrice != 0.0
        || fixedEntryQty != 0
        || fixedBreakevenMoved
        || martingaleRecoveryActive
        || !string.IsNullOrEmpty (fixedMartingaleSignalName)
        || fixedMartingaleDirection != MarketPosition.Flat
        || fixedMartingalePositionConfirmed
        || fixedMartingaleCloseProcessed
        || fixedMartingaleEntryAvgPrice != 0.0
        || fixedMartingaleEntryQty != 0
        || fixedMartingaleBreakevenMoved;

            if (!stillStale)
                return;

            if (EnableDebug)
                Print ($"[{Name}] FIXEDTICKS STALE STATE CLEARED | "
                    + $"FixedSignal={fixedEntrySignalName} | "
                    + $"FixedConfirmed={fixedPositionConfirmed} | "
                    + $"FixedClosedProcessed={fixedTradeCloseProcessed} | "
                    + $"MartingaleActive={martingaleRecoveryActive} | "
                    + $"MartingaleSignal={fixedMartingaleSignalName}");

            ResetFixedOrderState ();
            ResetMartingaleRecovery ();
            ClearPendingReverse ();
            ClearActiveTradeSignalSources ();
        }

        private void SubmitFixedEntry (bool isLong, bool useKO, bool usePA, bool useTH, bool useSJ, bool useSU, int groupSize, string groupName, string reason)
        {
            if (OrderMode != OrderManagementMode.FixedTicks)
                return;

            if (State != State.Historical && State != State.Realtime)
                return;

            if (State == State.Historical)
            {
                if (Position.MarketPosition != MarketPosition.Flat)
                    return;
            }
            else
            {
                if (martingaleRecoveryActive || HasActiveEntryState ())
                    return;
            }

            if (FixedStopLossTicks <= 0 || FixedProfitTargetTicks <= 0 || FixedOrderQuantity <= 0)
            {
                Print ($"[{Name}] FIXED ENTRY BLOCKED | Invalid fixed order settings. Qty={FixedOrderQuantity} SL={FixedStopLossTicks} PT={FixedProfitTargetTicks}");
                return;
            }

            if (EnableFixedBreakeven && FixedBreakevenOffsetTicks >= FixedBreakevenTriggerTicks)
            {
                Print ($"[{Name}] FIXED ENTRY BLOCKED | Breakeven offset must be less than trigger ticks. Trigger={FixedBreakevenTriggerTicks} Offset={FixedBreakevenOffsetTicks}");
                return;
            }

            string trigger = BuildSignalTriggerName (useKO, usePA, useTH, useSJ, useSU, groupSize, groupName);
            string signalName = isLong ? "FixedLongEntry" : "FixedShortEntry";

            fixedEntrySignalName = signalName;
            fixedEntryDirection = isLong ? MarketPosition.Long : MarketPosition.Short;
            fixedEntryAvgPrice = 0.0;
            fixedEntryQty = 0;
            fixedPositionConfirmed = false;
            fixedTradeCloseProcessed = false;
            fixedBreakevenMoved = false;

            SetActiveTradeSignalSources (useKO, usePA, useTH, useSJ, useSU, fixedEntryDirection, groupSize, groupName);

            _tradeMap[fixedEntrySignalName] = new TradeRecord
            {
                Trigger = trigger,
                Direction = isLong ? "Long" : "Short",
                OpenTime = Time[0],
                Instrument = FormatInstrumentName (),
                OpenPrice = 0.0,
                Qty = FixedOrderQuantity,
                AtmStrategyName = "FixedTicks SL " + FixedStopLossTicks + " / PT " + FixedProfitTargetTicks + (EnableFixedBreakeven ? " / BE " + FixedBreakevenTriggerTicks + "+" + FixedBreakevenOffsetTicks : "")
            };

            SetStopLoss (signalName, CalculationMode.Ticks, FixedStopLossTicks, false);
            SetProfitTarget (signalName, CalculationMode.Ticks, FixedProfitTargetTicks);

            if (EnableDebug)
                Print ($"{Time[0]} | FIXED ENTRY SUBMIT | State={State} | Reason={reason} | Direction={(isLong ? "Long" : "Short")} | Trigger={trigger} | Qty={FixedOrderQuantity} | SL={FixedStopLossTicks} | PT={FixedProfitTargetTicks} | BE={(EnableFixedBreakeven ? "ON" : "OFF")}");

            if (isLong)
                EnterLong (FixedOrderQuantity, signalName);
            else
                EnterShort (FixedOrderQuantity, signalName);
        }

        private void ResetFixedOrderState ()
        {
            fixedEntrySignalName = string.Empty;
            fixedEntryDirection = MarketPosition.Flat;
            fixedEntryAvgPrice = 0.0;
            fixedEntryQty = 0;
            fixedPositionConfirmed = false;
            fixedTradeCloseProcessed = false;
            fixedBreakevenMoved = false;

            fixedMartingaleSignalName = string.Empty;
            fixedMartingaleDirection = MarketPosition.Flat;
            fixedMartingaleEntryAvgPrice = 0.0;
            fixedMartingaleEntryQty = 0;
            fixedMartingalePositionConfirmed = false;
            fixedMartingaleCloseProcessed = false;
            fixedMartingaleBreakevenMoved = false;

            lastFixedExitCandidatePrice = 0.0;
            lastFixedExitCandidateTime = Core.Globals.MinDate;
        }

        private double GetFixedBreakevenCurrentPrice ()
        {
            try
            {
                if (BarsArray.Length > 1 && CurrentBars.Length > 1 && CurrentBars[1] >= 0)
                    return Closes[1][0];

                if (CurrentBar >= 0)
                    return Close[0];
            }
            catch { }

            return 0.0;
        }

        private void ManageFixedBreakeven ()
        {
            if (OrderMode != OrderManagementMode.FixedTicks)
                return;

            if (!EnableFixedBreakeven)
                return;

            if (FixedBreakevenTriggerTicks <= 0 || FixedBreakevenOffsetTicks < 0)
                return;

            if (FixedBreakevenOffsetTicks >= FixedBreakevenTriggerTicks)
                return;

            if (State != State.Historical && State != State.Realtime)
                return;

            double currentPrice = GetFixedBreakevenCurrentPrice ();

            if (currentPrice <= 0)
                return;

            if (!fixedBreakevenMoved
                && !string.IsNullOrEmpty (fixedEntrySignalName)
                && fixedEntryDirection != MarketPosition.Flat
                && fixedEntryAvgPrice > 0
                && (fixedPositionConfirmed || Position.MarketPosition == fixedEntryDirection))
            {
                if (TryMoveFixedBreakevenStop (fixedEntrySignalName, fixedEntryDirection, fixedEntryAvgPrice, currentPrice, "Fixed"))
                    fixedBreakevenMoved = true;
            }

            if (!fixedMartingaleBreakevenMoved
                && !string.IsNullOrEmpty (fixedMartingaleSignalName)
                && fixedMartingaleDirection != MarketPosition.Flat
                && fixedMartingaleEntryAvgPrice > 0
                && (fixedMartingalePositionConfirmed || Position.MarketPosition == fixedMartingaleDirection))
            {
                if (TryMoveFixedBreakevenStop (fixedMartingaleSignalName, fixedMartingaleDirection, fixedMartingaleEntryAvgPrice, currentPrice, "Fixed Martingale"))
                    fixedMartingaleBreakevenMoved = true;
            }
        }

        private bool TryMoveFixedBreakevenStop (string signalName, MarketPosition direction, double entryPrice, double currentPrice, string label)
        {
            if (string.IsNullOrEmpty (signalName))
                return false;

            if (direction == MarketPosition.Flat || entryPrice <= 0 || currentPrice <= 0)
                return false;

            double favorableTicks = direction == MarketPosition.Long
        ? (currentPrice - entryPrice) / TickSize
        : (entryPrice - currentPrice) / TickSize;

            if (favorableTicks < FixedBreakevenTriggerTicks)
                return false;

            double newStopPrice = direction == MarketPosition.Long
        ? entryPrice + (FixedBreakevenOffsetTicks * TickSize)
        : entryPrice - (FixedBreakevenOffsetTicks * TickSize);

            newStopPrice = Instrument.MasterInstrument.RoundToTickSize (newStopPrice);

            SetStopLoss (signalName, CalculationMode.Price, newStopPrice, false);

            if (EnableDebug)
                Print ($"[{Name}] {label} BREAKEVEN MOVED | Signal={signalName} | Direction={direction} | Entry={entryPrice:F2} | Current={currentPrice:F2} | TriggerTicks={FixedBreakevenTriggerTicks} | OffsetTicks={FixedBreakevenOffsetTicks} | NewStop={newStopPrice:F2}");

            return true;
        }

        private void SyncFixedTicksPnlFromSystemPerformance (DateTime tickTime)
        {
            if (OrderMode != OrderManagementMode.FixedTicks)
                return;

            double cumulativeClosed = 0.0;
            double dailyClosed = 0.0;
            Trade lastTrade = null;

            try
            {
                if (SystemPerformance == null || SystemPerformance.AllTrades == null)
                    return;

                cumulativeClosed = SystemPerformance.AllTrades.TradesPerformance.Currency.CumProfit;

                for (int i = 0; i < SystemPerformance.AllTrades.Count; i++)
                {
                    Trade trade = SystemPerformance.AllTrades[i];

                    if (trade == null || trade.Exit == null)
                        continue;

                    if (trade.Exit.Time.Date == tickTime.Date)
                    {
                        dailyClosed += trade.ProfitCurrency;

                        if (lastTrade == null || trade.Exit.Time > lastTrade.Exit.Time)
                            lastTrade = trade;
                    }
                }

                totalRealizedPnL = Math.Round (cumulativeClosed, 2);
                dailyRealizedPnL = Math.Round (dailyClosed, 2);

                if (lastTrade != null)
                {
                    string result = lastTrade.ProfitCurrency >= 0 ? "WIN" : "LOSS";

                    lastTradeClosedSummary =
                        "Last: " + result
                        + " | PnL " + lastTrade.ProfitCurrency.ToString ("C")
                        + " | Exit " + lastTrade.Exit.Time.ToString ("HH:mm:ss");
                }
            }
            catch (Exception ex)
            {
                if (EnableDebug)
                    Print ($"[{Name}] SyncFixedTicksPnlFromSystemPerformance ERROR | {ex.Message}");
            }
        }

        private double AveragePriceFromExecution (double currentAverage, int currentQuantity, double fillPrice, int fillQuantity)
        {
            int newQuantity = currentQuantity + fillQuantity;

            if (newQuantity <= 0)
                return fillPrice;

            if (currentQuantity <= 0)
                return fillPrice;

            return ((currentAverage * currentQuantity) + (fillPrice * fillQuantity)) / newQuantity;
        }

        private double CalculateFixedTradePnl (MarketPosition direction, double entryPrice, double exitPrice, int quantity)
        {
            if (direction == MarketPosition.Flat || entryPrice <= 0 || exitPrice <= 0 || quantity <= 0 || Instrument == null || Instrument.MasterInstrument == null)
                return 0.0;

            double points = direction == MarketPosition.Long
                ? exitPrice - entryPrice
                : entryPrice - exitPrice;

            return Math.Round (points * quantity * Instrument.MasterInstrument.PointValue, 2);
        }

        private void ProcessFixedTradeClosed (double exitPrice, DateTime closeTime)
        {
            if (fixedTradeCloseProcessed || string.IsNullOrEmpty (fixedEntrySignalName))
                return;

            fixedTradeCloseProcessed = true;

            double roundedExit = Instrument.MasterInstrument.RoundToTickSize (exitPrice);
            double tradePnl = CalculateFixedTradePnl (fixedEntryDirection, fixedEntryAvgPrice, roundedExit, Math.Max (fixedEntryQty, FixedOrderQuantity));
            MarketPosition closedDirection = fixedEntryDirection;
            bool startMartingale = ShouldStartMartingaleRecovery (tradePnl, closedDirection);
            MarketPosition martingaleDirection = startMartingale ? GetOppositeDirection (closedDirection) : MarketPosition.Flat;

            totalRealizedPnL += tradePnl;
            lastAtmRealizedPnL = tradePnl;
            UpdateSignalTrackingOnTradeClose (tradePnl);
            UpdateLastTradeClosedSummary (tradePnl);
            PrintSignalTrackingOnTradeClose (tradePnl);

            if (_logWriter != null && _tradeMap.TryGetValue (fixedEntrySignalName, out TradeRecord tr))
            {
                string acct = (Account != null && !string.IsNullOrEmpty (Account.Name)) ? Account.Name : "NoAccount";
                _logWriter.WriteLine ($"{tr.OpenTime:yyyy-MM-dd HH:mm:ss},{acct},{tr.Instrument},{tr.OpenPrice:F2},{tr.Qty},{closeTime:yyyy-MM-dd HH:mm:ss},{tr.Trigger},{tr.Direction},{tr.AtmStrategyName},{tradePnl:F2}");
                _logWriter.Flush ();
            }

            if (EnableDebug)
                Print ($"[{Name}] FIXED TRADE CLOSED | Direction={closedDirection} | Entry={fixedEntryAvgPrice:F2} | Exit={roundedExit:F2} | Qty={Math.Max (fixedEntryQty, FixedOrderQuantity)} | PnL={tradePnl:F2}");

            _tradeMap.Remove (fixedEntrySignalName);
            fixedEntrySignalName = string.Empty;
            fixedEntryDirection = MarketPosition.Flat;
            fixedEntryAvgPrice = 0.0;
            fixedEntryQty = 0;
            fixedPositionConfirmed = false;
            fixedTradeCloseProcessed = false;
            fixedBreakevenMoved = false;
            lastFixedExitCandidatePrice = 0.0;
            lastFixedExitCandidateTime = Core.Globals.MinDate;
            ClearActiveTradeSignalSources ();

            if (startMartingale && martingaleDirection != MarketPosition.Flat)
                SubmitMartingaleRecoveryEntry (martingaleDirection);
        }

        private void SubmitFixedMartingaleRecoveryEntry (MarketPosition direction)
        {
            if (State != State.Realtime)
                return;

            if (direction == MarketPosition.Flat || martingaleRecoveryActive)
                return;

            if (FixedStopLossTicks <= 0 || FixedProfitTargetTicks <= 0 || FixedOrderQuantity <= 0)
            {
                Print ($"[{Name}] FIXED MARTINGALE BLOCKED | Invalid fixed settings. Qty={FixedOrderQuantity} SL={FixedStopLossTicks} PT={FixedProfitTargetTicks}");
                return;
            }

            if (EnableFixedBreakeven && FixedBreakevenOffsetTicks >= FixedBreakevenTriggerTicks)
            {
                Print ($"[{Name}] FIXED MARTINGALE BLOCKED | Breakeven offset must be less than trigger ticks. Trigger={FixedBreakevenTriggerTicks} Offset={FixedBreakevenOffsetTicks}");
                return;
            }

            bool isLong = direction == MarketPosition.Long;
            int martingaleQty = Math.Max (1, FixedOrderQuantity * 2);

            martingaleRecoveryActive = true;
            martingaleRecoveryDirection = direction;
            martingaleLastRealizedPnL = 0.0;
            fixedMartingaleSignalName = isLong ? "FixedMartingaleLongEntry" : "FixedMartingaleShortEntry";
            fixedMartingaleDirection = direction;
            fixedMartingaleEntryAvgPrice = 0.0;
            fixedMartingaleEntryQty = 0;
            fixedMartingalePositionConfirmed = false;
            fixedMartingaleCloseProcessed = false;
            fixedMartingaleBreakevenMoved = false;

            _tradeMap[fixedMartingaleSignalName] = new TradeRecord
            {
                Trigger = "FIXED-MARTINGALE-REVERSAL",
                Direction = isLong ? "Long" : "Short",
                OpenTime = Time[0],
                Instrument = FormatInstrumentName (),
                OpenPrice = 0.0,
                Qty = martingaleQty,
                AtmStrategyName = "FixedTicks Martingale SL " + FixedStopLossTicks + " / PT " + FixedProfitTargetTicks + (EnableFixedBreakeven ? " / BE " + FixedBreakevenTriggerTicks + "+" + FixedBreakevenOffsetTicks : "")
            };

            SetStopLoss (fixedMartingaleSignalName, CalculationMode.Ticks, FixedStopLossTicks, false);
            SetProfitTarget (fixedMartingaleSignalName, CalculationMode.Ticks, FixedProfitTargetTicks);

            Print ($"[{Name}] FIXED MARTINGALE SUBMIT | Direction={direction} | Qty={martingaleQty} | SL={FixedStopLossTicks} | PT={FixedProfitTargetTicks} | BE={(EnableFixedBreakeven ? "ON" : "OFF")}");

            if (isLong)
                EnterLong (martingaleQty, fixedMartingaleSignalName);
            else
                EnterShort (martingaleQty, fixedMartingaleSignalName);
        }

        private void ProcessFixedMartingaleClosed (double exitPrice, DateTime closeTime)
        {
            if (fixedMartingaleCloseProcessed || string.IsNullOrEmpty (fixedMartingaleSignalName))
                return;

            fixedMartingaleCloseProcessed = true;

            double roundedExit = Instrument.MasterInstrument.RoundToTickSize (exitPrice);
            double tradePnl = CalculateFixedTradePnl (fixedMartingaleDirection, fixedMartingaleEntryAvgPrice, roundedExit, Math.Max (fixedMartingaleEntryQty, FixedOrderQuantity * 2));
            martingaleLastRealizedPnL = tradePnl;
            totalRealizedPnL += tradePnl;
            UpdateLastTradeClosedSummary (tradePnl);

            if (_logWriter != null && _tradeMap.TryGetValue (fixedMartingaleSignalName, out TradeRecord tr))
            {
                string acct = (Account != null && !string.IsNullOrEmpty (Account.Name)) ? Account.Name : "NoAccount";
                _logWriter.WriteLine ($"{tr.OpenTime:yyyy-MM-dd HH:mm:ss},{acct},{tr.Instrument},{tr.OpenPrice:F2},{tr.Qty},{closeTime:yyyy-MM-dd HH:mm:ss},{tr.Trigger},{tr.Direction},{tr.AtmStrategyName},{tradePnl:F2}");
                _logWriter.Flush ();
            }

            Print ($"[{Name}] FIXED MARTINGALE CLOSED | PnL={tradePnl:F2} | Recovery complete. Normal signal entries resumed.");

            _tradeMap.Remove (fixedMartingaleSignalName);
            ResetMartingaleRecovery ();
        }

        private bool IsFixedStrategyFlat ()
        {
            try
            {
                return Position == null || Position.MarketPosition == MarketPosition.Flat;
            }
            catch
            {
                return true;
            }
        }

        private double GetFixedExitFallbackPrice (double preferredPrice = 0.0)
        {
            if (preferredPrice > 0)
                return Instrument.MasterInstrument.RoundToTickSize (preferredPrice);

            if (lastFixedExitCandidatePrice > 0)
                return Instrument.MasterInstrument.RoundToTickSize (lastFixedExitCandidatePrice);

            double currentPrice = GetFixedBreakevenCurrentPrice ();

            if (currentPrice > 0)
                return Instrument.MasterInstrument.RoundToTickSize (currentPrice);

            try
            {
                if (CurrentBar >= 0)
                    return Instrument.MasterInstrument.RoundToTickSize (Close[0]);
            }
            catch { }

            return 0.0;
        }

        private DateTime GetFixedExitFallbackTime ()
        {
            if (lastFixedExitCandidateTime != Core.Globals.MinDate)
                return lastFixedExitCandidateTime;

            try
            {
                if (CurrentBar >= 0)
                    return Time[0];
            }
            catch { }

            return DateTime.Now;
        }

        private bool CanUseReverseOnAlternateSignal ()
        {
            if (!_reverseOnAlternateSignal)
                return false;

            // Do not run FlattenEverything-based reversal logic during historical FixedTicks processing.
            if (OrderMode == OrderManagementMode.FixedTicks && State == State.Historical)
                return false;

            return true;
        }

        private void SubmitAtmEntry (bool isLong, bool useKO, bool usePA, bool useTH, bool useSJ, bool useSU, int groupSize, string groupName, string reason)
        {
            if (State != State.Realtime)
                return;

            if (orderId.Length > 0 || atmStrategyId.Length > 0 || martingaleRecoveryActive)
                return;

            string trigger = BuildSignalTriggerName (useKO, usePA, useTH, useSJ, useSU, groupSize, groupName);

            isAtmStrategyCreated = false;
            SetActiveTradeSignalSources (useKO, usePA, useTH, useSJ, useSU, isLong ? MarketPosition.Long : MarketPosition.Short, groupSize, groupName);

            atmStrategyId = GetAtmStrategyUniqueId ();
            orderId = GetAtmStrategyUniqueId ();

            _tradeMap[atmStrategyId] = new TradeRecord
            {
                Trigger = trigger,
                Direction = isLong ? "Long" : "Short",
                OpenTime = Time[0],
                Instrument = FormatInstrumentName (),
                OpenPrice = 0.0,
                Qty = 0,
                AtmStrategyName = AtmStrategy
            };

            if (EnableDebug)
                Print ($"{Time[0]} | ATM ENTRY SUBMIT | Reason={reason} | Direction={(isLong ? "Long" : "Short")} | Trigger={trigger} | ATM={AtmStrategy}");

            AtmStrategyCreate (isLong ? OrderAction.Buy : OrderAction.SellShort, OrderType.Market, 0, 0, TimeInForce.Day, orderId, AtmStrategy, atmStrategyId, (atmCallbackErrorCode, atmCallBackId) =>
            {
                if (atmCallbackErrorCode == ErrorCode.NoError && atmCallBackId == atmStrategyId)
                    isAtmStrategyCreated = true;
            });
        }

        private bool IsMissingAtmIdError (Exception ex)
        {
            if (ex == null || string.IsNullOrEmpty (ex.Message))
                return false;

            return ex.Message.IndexOf ("does not exist", StringComparison.OrdinalIgnoreCase) >= 0
                || ex.Message.IndexOf ("ATM strategy ID", StringComparison.OrdinalIgnoreCase) >= 0
                || ex.Message.IndexOf ("Order ID", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private bool TryGetAtmEntryOrderStatusSafe (ref string entryOrderId, string label, out string[] status)
        {
            status = null;

            if (string.IsNullOrEmpty (entryOrderId))
                return false;

            string id = entryOrderId;

            try
            {
                status = GetAtmStrategyEntryOrderStatus (id);

                if (status == null || status.Length == 0)
                    return false;

                return true;
            }
            catch (Exception ex)
            {
                if (IsMissingAtmIdError (ex))
                {
                    Print ($"[{Name}] STALE ATM ENTRY ORDER ID CLEARED | Label={label} | OrderId={id} | Error={ex.Message}");
                    entryOrderId = string.Empty;
                    return false;
                }

                Print ($"[{Name}] ATM ENTRY ORDER STATUS ERROR | Label={label} | OrderId={id} | Error={ex.Message}");
                return false;
            }
        }

        private bool TryGetAtmMarketPositionSafe (ref string strategyId, string label, out MarketPosition marketPosition)
        {
            marketPosition = MarketPosition.Flat;

            if (string.IsNullOrEmpty (strategyId))
                return false;

            string id = strategyId;

            try
            {
                marketPosition = GetAtmStrategyMarketPosition (id);
                return true;
            }
            catch (Exception ex)
            {
                if (IsMissingAtmIdError (ex))
                {
                    Print ($"[{Name}] STALE ATM STRATEGY ID CLEARED | Label={label} | AtmId={id} | Error={ex.Message}");
                    strategyId = string.Empty;
                    return false;
                }

                Print ($"[{Name}] ATM MARKET POSITION ERROR | Label={label} | AtmId={id} | Error={ex.Message}");
                return false;
            }
        }

        private bool TryGetAtmRealizedPnlSafe (string strategyId, string label, out double realizedPnl)
        {
            realizedPnl = 0.0;

            if (string.IsNullOrEmpty (strategyId))
                return false;

            try
            {
                realizedPnl = GetAtmStrategyRealizedProfitLoss (strategyId);
                return true;
            }
            catch (Exception ex)
            {
                if (IsMissingAtmIdError (ex))
                    Print ($"[{Name}] STALE ATM REALIZED PNL ID DETECTED | Label={label} | AtmId={strategyId} | Error={ex.Message}");
                else
                    Print ($"[{Name}] ATM REALIZED PNL READ FAILED | Label={label} | AtmId={strategyId} | Error={ex.Message}");

                return false;
            }
        }

        private bool TryGetAtmUnrealizedPnlSafe (string strategyId, string label, out double unrealizedPnl)
        {
            unrealizedPnl = 0.0;

            if (string.IsNullOrEmpty (strategyId))
                return false;

            try
            {
                unrealizedPnl = GetAtmStrategyUnrealizedProfitLoss (strategyId);
                return true;
            }
            catch (Exception ex)
            {
                if (EnableDebug || IsMissingAtmIdError (ex))
                    Print ($"[{Name}] ATM UNREALIZED PNL READ FAILED | Label={label} | AtmId={strategyId} | Error={ex.Message}");

                return false;
            }
        }

        private bool TryGetAtmAveragePriceSafe (string strategyId, string label, out double averagePrice)
        {
            averagePrice = 0.0;

            if (string.IsNullOrEmpty (strategyId))
                return false;

            try
            {
                averagePrice = GetAtmStrategyPositionAveragePrice (strategyId);
                return true;
            }
            catch (Exception ex)
            {
                if (EnableDebug || IsMissingAtmIdError (ex))
                    Print ($"[{Name}] ATM AVG PRICE READ FAILED | Label={label} | AtmId={strategyId} | Error={ex.Message}");

                return false;
            }
        }

        private bool TryGetAtmQuantitySafe (string strategyId, string label, out int quantity)
        {
            quantity = 0;

            if (string.IsNullOrEmpty (strategyId))
                return false;

            try
            {
                quantity = (int)GetAtmStrategyPositionQuantity (strategyId);
                return true;
            }
            catch (Exception ex)
            {
                if (EnableDebug || IsMissingAtmIdError (ex))
                    Print ($"[{Name}] ATM QTY READ FAILED | Label={label} | AtmId={strategyId} | Error={ex.Message}");

                return false;
            }
        }

        private void ClearNormalAtmState (string reason)
        {
            Print ($"[{Name}] CLEAR NORMAL ATM STATE | Reason={reason} | ATM={atmStrategyId} | Order={orderId}");

            if (!string.IsNullOrEmpty (atmStrategyId))
                _tradeMap.Remove (atmStrategyId);

            atmStrategyId = string.Empty;
            orderId = string.Empty;
            isAtmStrategyCreated = false;
            _atmPositionConfirmed = false;
            dailyUnrealizedPnL = 0.0;

            ClearActiveTradeSignalSources ();
        }

        private MarketPosition GetOppositeDirection (MarketPosition direction)
        {
            if (direction == MarketPosition.Long)
                return MarketPosition.Short;

            if (direction == MarketPosition.Short)
                return MarketPosition.Long;

            return MarketPosition.Flat;
        }

        private bool ShouldStartMartingaleRecovery (double closedPnl, MarketPosition closedDirection)
        {
            if (!EnableMartingaleOnStopLoss)
                return false;

            if (martingaleRecoveryActive)
                return false;

            if (closedDirection == MarketPosition.Flat)
                return false;

            if (closedPnl >= 0)
                return false;

            if (dailyLimitHit)
                return false;

            if (OrderMode == OrderManagementMode.AtmStrategy && string.IsNullOrWhiteSpace (MartingaleAtmStrategy))
            {
                Print ($"[{Name}] MARTINGALE BLOCKED | Normal ATM closed negative, but no Martingale ATM Strategy is selected.");
                return false;
            }

            return true;
        }

        private void ResetMartingaleRecovery ()
        {
            martingaleRecoveryActive = false;
            martingaleRecoveryDirection = MarketPosition.Flat;
            martingaleLastRealizedPnL = 0.0;
            martingaleAtmStrategyId = string.Empty;
            martingaleOrderId = string.Empty;
            martingaleAtmStrategyCreated = false;
            martingalePositionConfirmed = false;
            fixedMartingaleSignalName = string.Empty;
            fixedMartingaleDirection = MarketPosition.Flat;
            fixedMartingaleEntryAvgPrice = 0.0;
            fixedMartingaleEntryQty = 0;
            fixedMartingalePositionConfirmed = false;
            fixedMartingaleCloseProcessed = false;
        }

        private void SubmitMartingaleRecoveryEntry (MarketPosition direction)
        {
            if (State != State.Realtime)
                return;

            if (direction == MarketPosition.Flat)
                return;

            if (martingaleRecoveryActive)
                return;

            if (OrderMode == OrderManagementMode.FixedTicks)
            {
                SubmitFixedMartingaleRecoveryEntry (direction);
                return;
            }

            if (!string.IsNullOrEmpty (martingaleAtmStrategyId) || !string.IsNullOrEmpty (martingaleOrderId))
                return;

            if (string.IsNullOrWhiteSpace (MartingaleAtmStrategy))
            {
                Print ($"[{Name}] MARTINGALE BLOCKED | No Martingale ATM Strategy selected.");
                return;
            }

            bool isLong = direction == MarketPosition.Long;

            martingaleRecoveryActive = true;
            martingaleRecoveryDirection = direction;
            martingaleLastRealizedPnL = 0.0;
            martingaleAtmStrategyCreated = false;
            martingalePositionConfirmed = false;
            martingaleAtmStrategyId = GetAtmStrategyUniqueId ();
            martingaleOrderId = GetAtmStrategyUniqueId ();

            string trigger = "MARTINGALE-REVERSAL";

            _tradeMap[martingaleAtmStrategyId] = new TradeRecord
            {
                Trigger = trigger,
                Direction = isLong ? "Long" : "Short",
                OpenTime = Time[0],
                Instrument = FormatInstrumentName (),
                OpenPrice = 0.0,
                Qty = 0,
                AtmStrategyName = MartingaleAtmStrategy
            };

            Print ($"[{Name}] MARTINGALE SUBMIT | Direction={direction} | ATM={MartingaleAtmStrategy}");

            AtmStrategyCreate (isLong ? OrderAction.Buy : OrderAction.SellShort, OrderType.Market, 0, 0, TimeInForce.Day, martingaleOrderId, MartingaleAtmStrategy, martingaleAtmStrategyId, (atmCallbackErrorCode, atmCallBackId) =>
            {
                if (atmCallbackErrorCode == ErrorCode.NoError && atmCallBackId == martingaleAtmStrategyId)
                    martingaleAtmStrategyCreated = true;
            });
        }

        private void CaptureMartingaleFill (string[] status)
        {
            if (!_tradeMap.TryGetValue (martingaleAtmStrategyId, out TradeRecord rec))
                return;

            if (rec.OpenPrice == 0.0
                && double.TryParse (status[0], System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out double price)
                && price > 0)
                rec.OpenPrice = Instrument.MasterInstrument.RoundToTickSize (price);

            if (rec.Qty == 0
                && int.TryParse (status[1], out int qty))
                rec.Qty = qty;
        }

        private double GetMartingaleUnrealizedPnL ()
        {
            if (!martingaleRecoveryActive)
                return 0.0;

            if (OrderMode == OrderManagementMode.FixedTicks)
                return 0.0;

            if (string.IsNullOrEmpty (martingaleAtmStrategyId))
                return 0.0;

            try
            {
                if (GetAtmStrategyMarketPosition (martingaleAtmStrategyId) != Cbi.MarketPosition.Flat)
                    return Instrument.MasterInstrument.RoundToTickSize (GetAtmStrategyUnrealizedProfitLoss (martingaleAtmStrategyId));
            }
            catch { }

            return 0.0;
        }

        private void UpdateMartingaleRecoveryOnTick (DateTime tickTime)
        {
            if (!martingaleRecoveryActive)
                return;

            if (OrderMode == OrderManagementMode.FixedTicks)
                return;

            if (!string.IsNullOrEmpty (martingaleOrderId))
            {
                string[] status = GetAtmStrategyEntryOrderStatus (martingaleOrderId);

                if (status != null && status.Length >= 3)
                {
                    if (status[2] == "Filled")
                    {
                        CaptureMartingaleFill (status);
                        martingaleOrderId = string.Empty;
                    }
                    else if (status[2] == "Cancelled" || status[2] == "Rejected")
                    {
                        Print ($"[{Name}] MARTINGALE ENTRY {status[2]} | Resetting recovery state.");
                        _tradeMap.Remove (martingaleAtmStrategyId);
                        ResetMartingaleRecovery ();
                        return;
                    }
                }
            }

            if (string.IsNullOrEmpty (martingaleAtmStrategyId))
                return;

            Cbi.MarketPosition atmPos = Cbi.MarketPosition.Flat;

            try
            {
                atmPos = GetAtmStrategyMarketPosition (martingaleAtmStrategyId);
            }
            catch
            {
                return;
            }

            if (atmPos != Cbi.MarketPosition.Flat)
            {
                martingalePositionConfirmed = true;
                martingaleAtmStrategyCreated = true;
                martingaleOrderId = string.Empty;

                if (_tradeMap.TryGetValue (martingaleAtmStrategyId, out TradeRecord rec) && rec.OpenPrice == 0.0)
                {
                    double avgPrice = GetAtmStrategyPositionAveragePrice (martingaleAtmStrategyId);
                    if (avgPrice > 0)
                    {
                        rec.OpenPrice = Instrument.MasterInstrument.RoundToTickSize (avgPrice);
                        rec.Qty = (int)GetAtmStrategyPositionQuantity (martingaleAtmStrategyId);
                    }
                }

                return;
            }

            if (martingalePositionConfirmed)
            {
                double realized = GetAtmStrategyRealizedProfitLoss (martingaleAtmStrategyId);

                if (!double.IsNaN (realized))
                    martingaleLastRealizedPnL = Instrument.MasterInstrument.RoundToTickSize (realized);
                else
                    martingaleLastRealizedPnL = 0.0;

                totalRealizedPnL += martingaleLastRealizedPnL;
                UpdateLastTradeClosedSummary (martingaleLastRealizedPnL);

                if (_logWriter != null && _tradeMap.TryGetValue (martingaleAtmStrategyId, out TradeRecord tr))
                {
                    string acct = (Account != null && !string.IsNullOrEmpty (Account.Name)) ? Account.Name : "NoAccount";
                    _logWriter.WriteLine ($"{tr.OpenTime:yyyy-MM-dd HH:mm:ss},{acct},{tr.Instrument},{tr.OpenPrice:F2},{tr.Qty},{tickTime:yyyy-MM-dd HH:mm:ss},{tr.Trigger},{tr.Direction},{tr.AtmStrategyName},{martingaleLastRealizedPnL:F2}");
                    _logWriter.Flush ();
                }

                Print ($"[{Name}] MARTINGALE CLOSED | PnL={martingaleLastRealizedPnL:F2} | Recovery complete. Normal signal entries resumed.");

                _tradeMap.Remove (martingaleAtmStrategyId);
                ResetMartingaleRecovery ();
            }
        }

        private void FlattenEverything (string reason)
        {
            if (State != State.Realtime)
                return;

            // Always print the reason — this is a high-signal, low-frequency event
            // (only fires on actual flattens). Diagnosing "why did my position close
            // immediately after opening?" requires knowing which trigger fired.
            Print ($"[{Name}] FlattenEverything ({Time[0]:HH:mm:ss}): {reason}  "
                + $"strategyPos={Position.MarketPosition} qty={Position.Quantity}  "
                + $"atmId={(string.IsNullOrEmpty (atmStrategyId) ? "<none>" : atmStrategyId)} "
                + $"martingaleAtmId={(string.IsNullOrEmpty (martingaleAtmStrategyId) ? "<none>" : martingaleAtmStrategyId)}");

            // 1) Close ATM if we have one tracked
            if (!string.IsNullOrEmpty (atmStrategyId))
            {
                try
                {
                    AtmStrategyClose (atmStrategyId);
                }
                catch { /* swallow */ }
            }

            if (!string.IsNullOrEmpty (martingaleAtmStrategyId))
            {
                try
                {
                    AtmStrategyClose (martingaleAtmStrategyId);
                }
                catch { /* swallow */ }
            }

            // 2) Belt-and-suspenders: if account still shows a position, hit it directly.
            //    With IsAdoptAccountPositionAware = true, ExitLong/ExitShort will flatten
            //    the adopted position via a market order.
            if (Position.MarketPosition == MarketPosition.Long)
                ExitLong ("NakedFlat", "");
            else if (Position.MarketPosition == MarketPosition.Short)
                ExitShort ("NakedFlat", "");

            // 3) Cancel any orphaned working orders on this instrument
            if (Account != null)
            {
                List<Order> toCancel = new List<Order>();
                lock (Account.Orders)
                {
                    foreach (Order o in Account.Orders)
                    {
                        if (o.Instrument != Instrument)
                            continue;
                        if (o.OrderState == OrderState.Working
                            || o.OrderState == OrderState.Accepted
                            || o.OrderState == OrderState.Submitted)
                            toCancel.Add (o);
                    }
                }
                foreach (Order o in toCancel)
                {
                    try
                    {
                        Account.Cancel (new[] { o });
                    }
                    catch { /* swallow */ }
                }
            }
        }

        private void FlattenNakedAccountPosition (MarketPosition positionState, int quantity, string reason)
        {
            if (State != State.Realtime)
                return;

            Print ($"[{Name}] FlattenNakedAccountPosition: {reason} | Position={positionState} Qty={quantity}");

            try
            {
                if (!string.IsNullOrEmpty (atmStrategyId))
                    AtmStrategyClose (atmStrategyId);

                if (!string.IsNullOrEmpty (martingaleAtmStrategyId))
                    AtmStrategyClose (martingaleAtmStrategyId);
            }
            catch { }

            try
            {
                if (Account != null && Instrument != null)
                {
                    Account.Flatten (new[] { Instrument });
                    pendingReverseActive = false;
                    ClearPendingReverse ();
                    ResetMartingaleRecovery ();
                    return;
                }
            }
            catch (Exception ex)
            {
                if (EnableDebug)
                    Print ($"[{Name}] Account.Flatten failed: {ex.Message}");
            }

            try
            {
                if (positionState == MarketPosition.Long)
                    ExitLong ("NakedFlat", "");
                else if (positionState == MarketPosition.Short)
                    ExitShort ("NakedFlat", "");
            }
            catch { }

            pendingReverseActive = false;
            ClearPendingReverse ();
            ResetMartingaleRecovery ();
        }

        private void CheckForNakedPositions (DateTime tickTime)
        {
            // Throttle is handled by caller using lastNakedCheckUtc.
            if (State != State.Realtime)
                return;

            Cbi.Position accountPosition = GetAccountPositionForInstrument ();

            MarketPosition positionState = MarketPosition.Flat;
            int quantity = 0;

            if (accountPosition != null && accountPosition.MarketPosition != MarketPosition.Flat)
            {
                positionState = accountPosition.MarketPosition;
                quantity = accountPosition.Quantity;
            }
            else
            {
                try
                {
                    positionState = Position != null ? Position.MarketPosition : MarketPosition.Flat;
                    quantity = Position != null ? Position.Quantity : 0;
                }
                catch
                {
                    positionState = MarketPosition.Flat;
                    quantity = 0;
                }
            }

            if (positionState == MarketPosition.Flat || quantity <= 0)
                return;

            bool hasProtectiveOrders = HasWorkingProtectiveOrders (positionState);

            if (!hasProtectiveOrders)
            {
                Print ($"[{Name}] NAKED POSITION DETECTED at {tickTime:HH:mm:ss} | "
                    + $"Position={positionState} {quantity} | "
                    + $"HasProtectiveOrders={hasProtectiveOrders}. FLATTENING IMMEDIATELY.");

                FlattenNakedAccountPosition (positionState, quantity, "Naked position watchdog - no exit orders present");
            }
        }

        private bool HasWorkingProtectiveOrders (MarketPosition positionState)
        {
            if (Account == null || Instrument == null)
                return false;

            try
            {
                lock (Account.Orders)
                {
                    foreach (Order o in Account.Orders)
                    {
                        if (o == null || o.Instrument == null)
                            continue;

                        if (o.Instrument.FullName != Instrument.FullName)
                            continue;

                        if (o.OrderState != OrderState.Working
                            && o.OrderState != OrderState.Accepted
                            && o.OrderState != OrderState.Submitted)
                            continue;

                        bool isExitSide =
                    (positionState == MarketPosition.Long && (o.OrderAction == OrderAction.Sell || o.OrderAction == OrderAction.SellShort))
                    || (positionState == MarketPosition.Short && (o.OrderAction == OrderAction.Buy || o.OrderAction == OrderAction.BuyToCover));

                        if (!isExitSide)
                            continue;

                        if (o.OrderType == OrderType.StopMarket
                            || o.OrderType == OrderType.StopLimit
                            || o.OrderType == OrderType.Limit)
                            return true;
                    }
                }
            }
            catch { }

            return false;
        }

        private Cbi.Position GetAccountPositionForInstrument ()
        {
            if (Account == null || Instrument == null)
                return null;

            try
            {
                lock (Account.Positions)
                {
                    foreach (Cbi.Position p in Account.Positions)
                    {
                        if (p == null || p.Instrument == null)
                            continue;

                        if (p.Instrument.FullName == Instrument.FullName)
                            return p;
                    }
                }
            }
            catch { }

            return null;
        }

        #region ATMPlotMarkers integration
        private void HookMarkerAccountEvents ()
        {
            if (OrderMode != OrderManagementMode.AtmStrategy)
                return;

            if (_markerHooked || Account == null)
                return;

            _markerHookedAccount = Account;
            _markerHookedAccount.PositionUpdate += OnMarkerAccountPositionUpdate;
            _markerHookedAccount.ExecutionUpdate += OnMarkerAccountExecutionUpdate;
            _markerHooked = true;

            try
            {
                Position seed = _markerHookedAccount.Positions
                    .FirstOrDefault (p => p.Instrument != null
                        && Instrument != null
                        && p.Instrument.FullName == Instrument.FullName);

                if (seed != null && seed.MarketPosition != MarketPosition.Flat)
                {
                    _markerCurrentQty = Math.Abs (seed.Quantity);
                    _markerLastQty = _markerCurrentQty;
                    _markerCurrentMP = seed.MarketPosition;
                    _markerLastMP = _markerCurrentMP;

                    _markerCurrent = new MarkerEntryExitData
                    {
                        EntryBar = CurrentBar,
                        EntryPrice = seed.AveragePrice,
                        IsLong = seed.MarketPosition == MarketPosition.Long,
                        IsComplete = false,
                        LineTag = "GZK_EE_Line_" + _markerLineCounter,
                        EntryLabelTag = "GZK_EE_Entry_" + _markerLineCounter,
                        ExitLabelTag = "GZK_EE_Exit_" + _markerLineCounter,
                        InitialPosition = _markerCurrentQty,
                        RemainingPosition = _markerCurrentQty
                    };

                    _markerLineCounter++;
                }
            }
            catch { }
        }

        private void UnhookMarkerAccountEvents ()
        {
            if (!_markerHooked || _markerHookedAccount == null)
                return;

            try
            {
                _markerHookedAccount.PositionUpdate -= OnMarkerAccountPositionUpdate;
                _markerHookedAccount.ExecutionUpdate -= OnMarkerAccountExecutionUpdate;
            }
            catch { }

            _markerHookedAccount = null;
            _markerHooked = false;
        }

        private void OnMarkerAccountPositionUpdate (object sender, PositionEventArgs e)
        {
            if (OrderMode != OrderManagementMode.AtmStrategy)
                return;

            try
            {
                if (e == null || e.Position == null || e.Position.Instrument == null)
                    return;

                if (Instrument == null || e.Position.Instrument.FullName != Instrument.FullName)
                    return;

                double newQty = Math.Abs (e.Position.Quantity);
                MarketPosition newMP = e.Position.MarketPosition;

                if (Math.Abs (newQty - _markerCurrentQty) > 0.001 || newMP != _markerCurrentMP)
                {
                    _markerCurrentQty = newQty;
                    _markerCurrentMP = newMP;
                    HandleMarkerPositionChange ();
                    _markerLastQty = _markerCurrentQty;
                    _markerLastMP = _markerCurrentMP;
                }
            }
            catch { }
        }

        private void OnMarkerAccountExecutionUpdate (object sender, ExecutionEventArgs e)
        {
            if (OrderMode != OrderManagementMode.AtmStrategy)
                return;

            try
            {
                if (e == null || e.Execution == null || e.Execution.Instrument == null)
                    return;

                if (Instrument == null || e.Execution.Instrument.FullName != Instrument.FullName)
                    return;

                _markerLastExecution = e.Execution;
            }
            catch { }
        }

        private void CheckMarkerPositionChange ()
        {
            if (OrderMode != OrderManagementMode.AtmStrategy)
                return;

            if (!_markerHooked || _markerHookedAccount == null)
                return;

            try
            {
                Position pos = _markerHookedAccount.Positions
                    .FirstOrDefault (p => p.Instrument != null
                        && Instrument != null
                        && p.Instrument.FullName == Instrument.FullName);

                double newQty = 0.0;
                MarketPosition newMP = MarketPosition.Flat;

                if (pos != null)
                {
                    newQty = Math.Abs (pos.Quantity);
                    newMP = pos.MarketPosition;
                }

                _markerCurrentQty = newQty;
                _markerCurrentMP = newMP;

                if (Math.Abs (_markerCurrentQty - _markerLastQty) > 0.001 || _markerCurrentMP != _markerLastMP)
                {
                    HandleMarkerPositionChange ();
                    _markerLastQty = _markerCurrentQty;
                    _markerLastMP = _markerCurrentMP;
                }
            }
            catch { }
        }

        private void HandleMarkerPositionChange ()
        {
            if (_markerLastMP == MarketPosition.Flat && _markerCurrentMP != MarketPosition.Flat)
            {
                HandleMarkerEntry ();
                return;
            }

            if (_markerLastMP != MarketPosition.Flat && _markerCurrentMP == MarketPosition.Flat)
            {
                HandleMarkerCompleteExit ();
                return;
            }

            if (_markerLastMP == _markerCurrentMP
                && _markerCurrentMP != MarketPosition.Flat
                && _markerLastQty > _markerCurrentQty)
            {
                HandleMarkerPartialExit ();
                return;
            }

            if ((_markerLastMP == MarketPosition.Long && _markerCurrentMP == MarketPosition.Short)
                || (_markerLastMP == MarketPosition.Short && _markerCurrentMP == MarketPosition.Long))
            {
                HandleMarkerCompleteExit ();
                HandleMarkerEntry ();
            }
        }

        private double GetMarkerExecutionPrice ()
        {
            if (_markerLastExecution != null && _markerLastExecution.Price > 0)
                return _markerLastExecution.Price;

            try
            {
                return Close[0];
            }
            catch { }

            return 0.0;
        }

        private void HandleMarkerEntry ()
        {
            double entryPrice = 0.0;
            bool isLong = _markerCurrentMP == MarketPosition.Long;

            try
            {
                if (_markerHookedAccount != null)
                {
                    Position pos = _markerHookedAccount.Positions
                        .FirstOrDefault (p => p.Instrument != null
                            && Instrument != null
                            && p.Instrument.FullName == Instrument.FullName);

                    if (pos != null)
                    {
                        entryPrice = pos.AveragePrice;
                        isLong = pos.MarketPosition == MarketPosition.Long;
                    }
                }
            }
            catch { }

            if (entryPrice <= 0.0)
                entryPrice = GetMarkerExecutionPrice ();

            _markerCurrent = new MarkerEntryExitData
            {
                EntryBar = CurrentBar,
                EntryPrice = entryPrice,
                IsLong = isLong,
                IsComplete = false,
                LineTag = "GZK_EE_Line_" + _markerLineCounter,
                EntryLabelTag = "GZK_EE_Entry_" + _markerLineCounter,
                ExitLabelTag = "GZK_EE_Exit_" + _markerLineCounter,
                InitialPosition = Math.Abs (_markerCurrentQty),
                RemainingPosition = Math.Abs (_markerCurrentQty)
            };

            _markerLineCounter++;
        }

        private void HandleMarkerPartialExit ()
        {
            if (_markerCurrent == null)
                return;

            // Use best available price; never bail — a partial with price=0 is better
            // than a leak. Drawing is deferred to RedrawCompletedMarkers (data thread).
            double exitPrice = GetMarkerExecutionPrice ();
            if (exitPrice <= 0.0) exitPrice = _markerCurrent.EntryPrice; // last resort

            MarkerEntryExitData scale = new MarkerEntryExitData
            {
                EntryBar = _markerCurrent.EntryBar,
                EntryPrice = _markerCurrent.EntryPrice,
                ExitBar = CurrentBar,
                ExitPrice = exitPrice,
                IsLong = _markerCurrent.IsLong,
                IsComplete = true,
                LineTag = "GZK_EE_Line_" + _markerLineCounter,
                EntryLabelTag = "GZK_EE_Entry_" + _markerLineCounter,
                ExitLabelTag = "GZK_EE_Exit_" + _markerLineCounter,
                InitialPosition = _markerCurrent.InitialPosition,
                RemainingPosition = Math.Abs (_markerCurrentQty)
            };

            _markerList.Add (scale);
            _markerLineCounter++;

            _markerCurrent.RemainingPosition = Math.Abs (_markerCurrentQty);
            _markerLastExecution = null;
            // Drawing happens in RedrawCompletedMarkers on the next OnBarUpdate (data thread)
        }

        private void HandleMarkerCompleteExit ()
        {
            if (_markerCurrent == null)
                return;

            // Use best available price; never bail — a closed marker with an approximate
            // exit price is better than _markerCurrent leaking and never terminating.
            // Drawing is deferred to RedrawCompletedMarkers (data thread).
            double exitPrice = GetMarkerExecutionPrice ();
            if (exitPrice <= 0.0) exitPrice = _markerCurrent.EntryPrice; // last resort

            _markerCurrent.ExitBar = CurrentBar;
            _markerCurrent.ExitPrice = exitPrice;
            _markerCurrent.IsComplete = true;

            _markerList.Add (_markerCurrent);
            _markerCurrent = null;
            _markerLastExecution = null;
            // Drawing happens in RedrawCompletedMarkers on the next OnBarUpdate (data thread)
        }

        private void RedrawCompletedMarkers ()
        {
            if (OrderMode != OrderManagementMode.AtmStrategy)
                return;

            if (!ShowEntryExitMarkers)
                return;

            // Redraw all completed (historical) trade lines
            for (int i = 0; i < _markerList.Count; i++)
            {
                MarkerEntryExitData m = _markerList[i];

                if (m.IsComplete)
                    DrawMarkerEntryExitLine (m);
            }

            // Draw the live open trade line extending to the current bar
            if (_markerCurrent != null && _markerCurrent.EntryBar >= 0)
            {
                MarkerEntryExitData live = new MarkerEntryExitData
                {
                    EntryBar   = _markerCurrent.EntryBar,
                    EntryPrice = _markerCurrent.EntryPrice,
                    ExitBar    = CurrentBar,
                    ExitPrice  = Close[0],
                    IsLong     = _markerCurrent.IsLong,
                    IsComplete = true,  // lets DrawMarkerEntryExitLine render it
                    LineTag         = _markerCurrent.LineTag,
                    EntryLabelTag   = _markerCurrent.EntryLabelTag,
                    ExitLabelTag    = _markerCurrent.ExitLabelTag,
                    InitialPosition  = _markerCurrent.InitialPosition,
                    RemainingPosition = _markerCurrent.RemainingPosition
                };

                DrawMarkerEntryExitLine (live);
            }
        }

        private void DrawMarkerEntryExitLine (MarkerEntryExitData data)
        {
            if (OrderMode != OrderManagementMode.AtmStrategy)
                return;

            if (!ShowEntryExitMarkers || data == null)
                return;

            if (data.EntryBar < 0 || data.ExitBar < 0)
                return;

            int entryBarsAgo = Math.Max (CurrentBar - data.EntryBar, 0);
            int exitBarsAgo = Math.Max (CurrentBar - data.ExitBar, 0);
            Brush brush = data.IsLong ? EntryExitLongColor : EntryExitShortColor;
            int width = Math.Max (1, EntryExitLineWidth);

            try
            {
                Draw.Line (this, data.LineTag, false,
                    entryBarsAgo, data.EntryPrice,
                    exitBarsAgo, data.ExitPrice,
                    brush, DashStyleHelper.Solid, width);
            }
            catch { }

            if (!ShowEntryExitLabels)
                return;

            try
            {
                string side = data.IsLong ? "LONG" : "SHORT";
                int textSize = Math.Max (6, EntryExitTextSize);
                SimpleFont font = new SimpleFont ("Arial", textSize);

                string entryText = string.Format ("{0} ENTRY\n{1:F2}", side, data.EntryPrice);

                Draw.Text (this, data.EntryLabelTag, false, entryText,
                    entryBarsAgo, data.EntryPrice, -15, brush, font,
                    System.Windows.TextAlignment.Center, Brushes.Transparent, Brushes.Transparent, 0);

                string exitText = string.Format ("EXIT\nEntry: {0:F2}\nExit: {1:F2}",
                    data.EntryPrice, data.ExitPrice);

                Draw.Text (this, data.ExitLabelTag, false, exitText,
                    exitBarsAgo, data.ExitPrice, 15, brush, font,
                    System.Windows.TextAlignment.Center, Brushes.Transparent, Brushes.Transparent, 0);
            }
            catch { }
        }
        #endregion

        #region RBro Button Panel

        private void CreateRBroControlPanel ()
        {
            if (ChartControl == null || _uiInitialized)
                return;

            if (!ShowControlPanel || ControlPanelPosition == HudCorner.Hidden)
                return;

            ChartControl.Dispatcher.InvokeAsync (() =>
            {
                try
                {
                    if (_uiInitialized)
                        return;

                    _controlPanel = new Border
                    {
                        Background = new SolidColorBrush (Color.FromArgb (220, 20, 20, 35)),
                        BorderBrush = Brushes.DodgerBlue,
                        BorderThickness = new Thickness (2),
                        CornerRadius = new CornerRadius (5),
                        Padding = new Thickness (10),
                        Margin = new Thickness (10, 10, 10, 10)
                    };

                    switch (ControlPanelPosition)
                    {
                        case HudCorner.TopRight:
                            _controlPanel.HorizontalAlignment = HorizontalAlignment.Right;
                            _controlPanel.VerticalAlignment = VerticalAlignment.Top;
                            break;

                        case HudCorner.BottomLeft:
                            _controlPanel.HorizontalAlignment = HorizontalAlignment.Left;
                            _controlPanel.VerticalAlignment = VerticalAlignment.Bottom;
                            break;

                        case HudCorner.BottomRight:
                            _controlPanel.HorizontalAlignment = HorizontalAlignment.Right;
                            _controlPanel.VerticalAlignment = VerticalAlignment.Bottom;
                            break;

                        case HudCorner.Center:
                            _controlPanel.HorizontalAlignment = HorizontalAlignment.Center;
                            _controlPanel.VerticalAlignment = VerticalAlignment.Center;
                            break;

                        default:
                            _controlPanel.HorizontalAlignment = HorizontalAlignment.Left;
                            _controlPanel.VerticalAlignment = VerticalAlignment.Top;
                            break;
                    }

                    StackPanel main = new StackPanel { Orientation = Orientation.Vertical };

                    main.Children.Add (new TextBlock
                    {
                        Text = "⚡ GodZilla Killa ⚡",
                        Foreground = Brushes.Cyan,
                        FontWeight = FontWeights.Bold,
                        FontSize = 14,
                        TextAlignment = TextAlignment.Center,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Margin = new Thickness (0, 0, 0, 0)
                    });

                    main.Children.Add (new TextBlock
                    {
                        Text = "Instrument -- " + ((Instrument != null && Instrument.MasterInstrument != null) ? Instrument.MasterInstrument.Name : "Unknown"),
                        Foreground = Brushes.DeepSkyBlue,
                        FontWeight = FontWeights.Bold,
                        FontSize = 12,
                        TextAlignment = TextAlignment.Center,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Margin = new Thickness (0, 0, 0, 4)
                    });

                    _statusLabel = new Label
                    {
                        Content = "Initializing...",
                        Foreground = Brushes.Yellow,
                        FontSize = 11,
                        Padding = new Thickness (0),
                        Margin = new Thickness (0, 0, 0, 4),
                        HorizontalContentAlignment = HorizontalAlignment.Center
                    };
                    main.Children.Add (_statusLabel);

                    StackPanel btnRow = new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Margin = new Thickness (0, 0, 0, 0)
                    };

                    _armLongBtn = new Button
                    {
                        Content = "ARM LONG",
                        Width = 150,
                        Height = 30,
                        Margin = new Thickness (2),
                        Background = Brushes.DarkGreen,
                        Foreground = Brushes.White,
                        FontWeight = FontWeights.Bold,
                        HorizontalContentAlignment = HorizontalAlignment.Center
                    };
                    _armLongBtn.Click += ArmLongBtn_Click;
                    btnRow.Children.Add (_armLongBtn);

                    _armShortBtn = new Button
                    {
                        Content = "ARM SHORT",
                        Width = 150,
                        Height = 30,
                        Margin = new Thickness (2),
                        Background = Brushes.DarkRed,
                        Foreground = Brushes.White,
                        FontWeight = FontWeights.Bold,
                        HorizontalContentAlignment = HorizontalAlignment.Center
                    };
                    _armShortBtn.Click += ArmShortBtn_Click;
                    btnRow.Children.Add (_armShortBtn);

                    main.Children.Add (btnRow);

                    _revBtn = new Button
                    {
                        Content = "REV: ON",
                        Width = 304,
                        Height = 30,
                        Margin = new Thickness (2, 4, 2, 0),
                        Background = Brushes.DarkOrange,
                        Foreground = Brushes.White,
                        FontWeight = FontWeights.Bold,
                        HorizontalContentAlignment = HorizontalAlignment.Center
                    };
                    _revBtn.Click += RevBtn_Click;
                    main.Children.Add (_revBtn);

                    _autoArmBtn = new Button
                    {
                        Content = "AUTO ARM: OFF",
                        Width = 304,
                        Height = 30,
                        Margin = new Thickness (2, 4, 2, 0),
                        Background = Brushes.DimGray,
                        Foreground = Brushes.White,
                        FontWeight = FontWeights.Bold,
                        HorizontalContentAlignment = HorizontalAlignment.Center
                    };
                    _autoArmBtn.Click += AutoArmBtn_Click;
                    main.Children.Add (_autoArmBtn);

                    _closeBtn = new Button
                    {
                        Content = "CLOSE ALL",
                        Width = 304,
                        Height = 30,
                        Margin = new Thickness (2, 4, 2, 0),
                        Background = Brushes.Maroon,
                        Foreground = Brushes.White,
                        FontWeight = FontWeights.Bold,
                        HorizontalContentAlignment = HorizontalAlignment.Center
                    };
                    _closeBtn.Click += CloseBtn_Click;
                    main.Children.Add (_closeBtn);

                    _controlPanel.Child = main;
                    UserControlCollection.Add (_controlPanel);
                    _uiInitialized = true;

                    UpdateRBroButtons ();
                    UpdateRBroStatusUI ();
                }
                catch (Exception ex)
                {
                    if (EnableDebug)
                        Print ($"[{Name}] Button panel error: {ex.Message}");
                }
            });
        }

        private void RemoveRBroControlPanel ()
        {
            if (ChartControl == null || _controlPanel == null)
                return;

            ChartControl.Dispatcher.InvokeAsync (() =>
            {
                try
                {
                    if (_controlPanel != null && UserControlCollection.Contains (_controlPanel))
                        UserControlCollection.Remove (_controlPanel);

                    _controlPanel = null;
                    _uiInitialized = false;
                }
                catch { }
            });
        }

        private void ArmLongBtn_Click (object sender, RoutedEventArgs e)
        {
            _armLong = !_armLong;
            UpdateRBroButtons ();
            UpdateRBroStatusUI ();
        }

        private void ArmShortBtn_Click (object sender, RoutedEventArgs e)
        {
            _armShort = !_armShort;
            UpdateRBroButtons ();
            UpdateRBroStatusUI ();
        }

        private void RevBtn_Click (object sender, RoutedEventArgs e)
        {
            _reverseOnAlternateSignal = !_reverseOnAlternateSignal;
            UpdateRBroButtons ();
            UpdateRBroStatusUI ();
        }

        private void AutoArmBtn_Click (object sender, RoutedEventArgs e)
        {
            _autoArm = !_autoArm;

            // When AutoArm is turned ON, re-arm both directions.
            // When turned OFF, leave the individual arm flags as-is so the
            // trader can still selectively arm via the Long/Short buttons.
            if (_autoArm)
                _armLong = _armShort = true;

            UpdateRBroButtons ();
            UpdateRBroStatusUI ();
        }

        private void CloseBtn_Click (object sender, RoutedEventArgs e)
        {
            FlattenEverything ("CLOSE ALL button clicked");
            UpdateRBroStatusUI ();
        }

        private void UpdateRBroButtons ()
        {
            if (_armLongBtn == null || _armShortBtn == null || _revBtn == null || _autoArmBtn == null)
                return;

            _armLongBtn.Background = _armLong ? Brushes.LimeGreen : Brushes.DarkGreen;
            _armLongBtn.Content = _armLong ? "ARMED LONG" : "ARM LONG";

            _armShortBtn.Background = _armShort ? Brushes.Red : Brushes.DarkRed;
            _armShortBtn.Content = _armShort ? "ARMED SHORT" : "ARM SHORT";

            _revBtn.Background = _reverseOnAlternateSignal ? Brushes.DarkOrange : Brushes.DimGray;
            _revBtn.Content = _reverseOnAlternateSignal ? "REV: ON" : "REV: OFF";

            _autoArmBtn.Background = _autoArm ? Brushes.DodgerBlue : Brushes.DimGray;
            _autoArmBtn.Content = _autoArm ? "AUTO ARM: ON" : "AUTO ARM: OFF";
        }

        private void UpdateRBroStatusUI ()
        {
            if (_statusLabel == null || ChartControl == null)
                return;

            ChartControl.Dispatcher.InvokeAsync (() =>
            {
                try
                {
                    string pos = Position.MarketPosition.ToString ();
                    bool usingAtmMode = OrderMode == OrderManagementMode.AtmStrategy;
                    bool noAtmSelected = usingAtmMode && string.IsNullOrWhiteSpace (AtmStrategy);
                    string fixedModeText = "Fixed SL " + FixedStopLossTicks + " / PT " + FixedProfitTargetTicks + (EnableFixedBreakeven ? " / BE " + FixedBreakevenTriggerTicks + "+" + FixedBreakevenOffsetTicks : "");
                    string selectedMode = usingAtmMode
                        ? (noAtmSelected ? "No ATM Selected" : AtmStrategy)
                        : fixedModeText;

                    string suffix = usingAtmMode
                        ? (orderId.Length > 0 ? " [ORDER: " + selectedMode + "]" : " [ATM: " + selectedMode + "]")
                        : " [" + selectedMode + "]";

                    _statusLabel.Content = pos + suffix;

                    if (noAtmSelected)
                        _statusLabel.Foreground = Brushes.Red;
                    else
                        _statusLabel.Foreground =
                            dailyLimitHit ? Brushes.Red :
                            Position.MarketPosition != MarketPosition.Flat ? Brushes.Cyan :
                            (_armLong || _armShort) ? Brushes.LimeGreen : Brushes.Orange;
                }
                catch { }
            });
        }

        #endregion

        #region Custom Property Manipulation
        private void ModifyOrderManagementProperties (PropertyDescriptorCollection col)
        {
            if (OrderMode == OrderManagementMode.FixedTicks)
            {
                RemoveProperties (col, "AtmStrategy", "MartingaleAtmStrategy");

                if (!EnableFixedBreakeven)
                    RemoveProperties (col, "FixedBreakevenTriggerTicks", "FixedBreakevenOffsetTicks");
            }
            else
            {
                RemoveProperties (col, "FixedOrderQuantity", "FixedStopLossTicks", "FixedProfitTargetTicks", "EnableFixedBreakeven", "FixedBreakevenTriggerTicks", "FixedBreakevenOffsetTicks");

                if (!EnableMartingaleOnStopLoss)
                    RemoveProperties (col, "MartingaleAtmStrategy");
            }
        }

        private void ModifyPNLProperties (PropertyDescriptorCollection col)
        {
            if (!EnableDailyProfitTarget)
                col.Remove (col["DailyProfitTarget"]);
            if (!EnableDailyLossLimit)
                col.Remove (col["DailyLossLimit"]);
        }
        private void ModifySessionProperties (PropertyDescriptorCollection col)
        {
            if (!EnableTF1)
            {
                col.Remove (col["StartTime1"]);
                col.Remove (col["EndTime1"]);
                col.Remove (col["FlattenTF1"]);
            }
            if (!EnableTF2)
            {
                col.Remove (col["StartTime2"]);
                col.Remove (col["EndTime2"]);
                col.Remove (col["FlattenTF2"]);
            }
            if (!EnableTF3)
            {
                col.Remove (col["StartTime3"]);
                col.Remove (col["EndTime3"]);
                col.Remove (col["FlattenTF3"]);
            }
            if (!EnableSkipTimeWindow)
            {
                col.Remove (col["SkipStartTime"]);
                col.Remove (col["SkipEndTime"]);
            }
        }

        private void ModifyEmaFilterProperties (PropertyDescriptorCollection col)
        {
            if (!EnableEmaFilter)
            {
                col.Remove (col["EmaShortPeriod"]);
                col.Remove (col["EmaLongPeriod"]);
            }
        }

        private void ModifyNewsFilterProperties (PropertyDescriptorCollection col)
        {
            if (EnableNewsFilter)
                return;

            string[] toRemove = new[]
            {
                "NewsFlattenAtWarningTime",
                "NewsShowDisplay",
                "NewsDisplayLocation",
                "NewsDisplayXOffsetPixels",
                "NewsDisplayYOffsetPixels",
                "NewsUse24HourTime",
                "NewsShowBackground",
                "NewsShowTimeBackBrush",
                "NewsTimeBackBrush",
                "NewsUSOnlyEvents",
                "NewsTodaysNewsOnly",
                "NewsShowLowPriority",
                "NewsMaxNewsItems",
                "NewsRefreshInterval",
                "NewsPreBlockMinutes",
                "NewsPostBlockMinutes",
                "NewsBlockHighImpact",
                "NewsBlockMediumImpact",
                "NewsBlockLowImpact",
                "NewsSendAlerts",
                "NewsAlertInterval",
                "NewsAlertWavFileName",
                "NewsDefaultTextColor",
                "NewsWarningTextColor",
                "NewsBackgroundColor",
                "NewsHeaderColor",
                "NewsHighImpactColor",
                "NewsMediumImpactColor",
                "NewsLowImpactColor",
                "NewsDefaultFont",
                "NewsWarningFont",
                "NewsDebug"
            };

            foreach (string p in toRemove)
            {
                if (col[p] != null)
                    col.Remove (col[p]);
            }
        }

        private void RemoveProperties (PropertyDescriptorCollection col, params string[] names)
        {
            foreach (string p in names)
            {
                if (col[p] != null)
                    col.Remove (col[p]);
            }
        }

        private void ModifySignalProperties (PropertyDescriptorCollection col)
        {
            if (!UseKOSignals)
            {
                RemoveProperties (col,
                    "KO_LongValue",
                    "KO_ShortValue",
                    "ShowKOIndicator",
                    "ShowKOSignalArrows",
                    "ShowKOSignalArrowLabels",
                    "KOSignalArrowText",
                    "KOSignalArrowBrush");
            }
            else
            {
                if (!ShowKOSignalArrows)
                    RemoveProperties (col, "ShowKOSignalArrowLabels", "KOSignalArrowText", "KOSignalArrowBrush");
                else if (!ShowKOSignalArrowLabels)
                    RemoveProperties (col, "KOSignalArrowText");
            }

            if (!UsePASignals)
            {
                RemoveProperties (col,
                    "PA_LongValue",
                    "PA_ShortValue",
                    "ShowPAIndicator",
                    "ShowPASignalArrows",
                    "ShowPASignalArrowLabels",
                    "PASignalArrowText",
                    "PASignalArrowBrush");
            }
            else
            {
                if (!ShowPASignalArrows)
                    RemoveProperties (col, "ShowPASignalArrowLabels", "PASignalArrowText", "PASignalArrowBrush");
                else if (!ShowPASignalArrowLabels)
                    RemoveProperties (col, "PASignalArrowText");
            }

            if (!UseTHSignals)
            {
                RemoveProperties (col,
                    "TH_LongValue",
                    "TH_ShortValue",
                    "ShowTHIndicator",
                    "ShowTHSignalArrows",
                    "ShowTHSignalArrowLabels",
                    "THSignalArrowText",
                    "THSignalArrowBrush");
            }
            else
            {
                if (!ShowTHSignalArrows)
                    RemoveProperties (col, "ShowTHSignalArrowLabels", "THSignalArrowText", "THSignalArrowBrush");
                else if (!ShowTHSignalArrowLabels)
                    RemoveProperties (col, "THSignalArrowText");
            }

            if (!UseSJSignals)
            {
                RemoveProperties (col,
                    "SJ_LongValue",
                    "SJ_ShortValue",
                    "ShowSJIndicator",
                    "ShowSJSignalArrows",
                    "ShowSJSignalArrowLabels",
                    "SJSignalArrowText",
                    "SJSignalArrowBrush");
            }
            else
            {
                if (!ShowSJSignalArrows)
                    RemoveProperties (col, "ShowSJSignalArrowLabels", "SJSignalArrowText", "SJSignalArrowBrush");
                else if (!ShowSJSignalArrowLabels)
                    RemoveProperties (col, "SJSignalArrowText");
            }

            if (!UseSUSignals)
            {
                RemoveProperties (col,
                    "SU_LongValue",
                    "SU_ShortValue",
                    "ShowSUIndicator",
                    "ShowSUSignalArrows",
                    "ShowSUSignalArrowLabels",
                    "SUSignalArrowText",
                    "SUSignalArrowBrush");
            }
            else
            {
                if (!ShowSUSignalArrows)
                    RemoveProperties (col, "ShowSUSignalArrowLabels", "SUSignalArrowText", "SUSignalArrowBrush");
                else if (!ShowSUSignalArrowLabels)
                    RemoveProperties (col, "SUSignalArrowText");
            }
        }

        private void ModifyIndicatorSettingsProperties (PropertyDescriptorCollection col)
        {
            if (!ShowIndicatorSettings || !UseKOSignals)
                RemoveProperties (col,
                    "King_SwingPointNeighborhood",
                    "King_ImbalanceQualifying",
                    "King_OrderBlockFindingBosChochPeriod",
                    "King_OrderBlockAge",
                    "King_OrderBlocksSameDirectionOffset",
                    "King_OrderBlocksDifferenceDirectionOffset",
                    "King_SignalTradeQuantityPerOrderBlock",
                    "King_SignalTradeSplitBars",
                    "KO_Brush");

            if (!ShowIndicatorSettings || !UsePASignals)
                RemoveProperties (col,
                    "Pana_Period",
                    "Pana_Factor",
                    "Pana_MiddlePeriod",
                    "Pana_SignalBreakSplit",
                    "Pana_SignalPullbackFindingPeriod",
                    "PA_Brush");

            if (!ShowIndicatorSettings || !UseTHSignals)
                RemoveProperties (col,
                    "Thunder_TrendMAType",
                    "Thunder_TrendPeriod",
                    "Thunder_TrendSmoothingEnabled",
                    "Thunder_TrendSmoothingMethod",
                    "Thunder_TrendSmoothingPeriod",
                    "Thunder_StopOffsetMultiplierStop",
                    "Thunder_SignalQuantityPerFlat",
                    "Thunder_SignalQuantityPerTrend",
                    "TH_Brush");

            if (!ShowIndicatorSettings || !UseSUSignals)
                RemoveProperties (col,
                    "SU_SlowMAType",
                    "SU_SlowMAPeriod",
                    "SU_SlowMASmoothingEnabled",
                    "SU_SlowMASmoothingMethod",
                    "SU_SlowMASmoothingPeriod",
                    "SU_FastMA1Type",
                    "SU_FastMA1Period",
                    "SU_FastMA1SmoothingEnabled",
                    "SU_FastMA1SmoothingMethod",
                    "SU_FastMA1SmoothingPeriod",
                    "SU_FastMA2Type",
                    "SU_FastMA2Period",
                    "SU_FastMA2SmoothingEnabled",
                    "SU_FastMA2SmoothingMethod",
                    "SU_FastMA2SmoothingPeriod",
                    "SU_FastMA3Type",
                    "SU_FastMA3Period",
                    "SU_FastMA3SmoothingEnabled",
                    "SU_FastMA3SmoothingMethod",
                    "SU_FastMA3SmoothingPeriod",
                    "SU_SignalSplitFirst",
                    "SU_SignalSplitSecond",
                    "SU_Brush");

            if (!ShowIndicatorSettings || !UseSJSignals)
                RemoveProperties (col,
                    "SJ_SensitiveModeEnabled",
                    "SJ_OffsetLevel1",
                    "SJ_OffsetLevel2",
                    "SJ_OffsetLevel3",
                    "SJ_OffsetLevel4",
                    "SJ_OffsetBase",
                    "SJ_ReferencePricePeriod",
                    "SJ_LineLevelsOffset",
                    "SJ_ExtremeNeighborhood",
                    "SJ_SignalCloseThreshold",
                    "SJ_SignalQuantityPerZone",
                    "SJ_SignalSplit",
                    "SJ_Brush");

            if (!ShowIndicatorSettings)
                RemoveProperties (col);
        }

        private void ModifySignalTextOffsetProperties (PropertyDescriptorCollection col)
        {
            bool individualTextVisible =
        (UseKOSignals && ShowKOSignalArrows && ShowKOSignalArrowLabels)
        || (UsePASignals && ShowPASignalArrows && ShowPASignalArrowLabels)
        || (UseTHSignals && ShowTHSignalArrows && ShowTHSignalArrowLabels)
        || (UseSJSignals && ShowSJSignalArrows && ShowSJSignalArrowLabels)
        || (UseSUSignals && ShowSUSignalArrows && ShowSUSignalArrowLabels);

            bool groupModeVisible = IsPrimaryGroupModeActive () || IsSecondaryGroupModeActive ();

            bool groupTextVisible = groupModeVisible && ShowGroupTriggerArrows && ShowGroupTriggerArrowLabel;

            if (!individualTextVisible && !groupTextVisible)
                RemoveProperties (col, "SignalArrowTextOffsetTicks");
        }

        private int CountEnabledSignals ()
        {
            int count = 0;

            if (UseKOSignals)
                count++;
            if (UsePASignals)
                count++;
            if (UseTHSignals)
                count++;
            if (UseSJSignals)
                count++;
            if (UseSUSignals)
                count++;

            return count;
        }

        private int CountEnabledGroupTriggerSet2Signals ()
        {
            int count = 0;

            if (G2_UseKOSignals)
                count++;
            if (G2_UsePASignals)
                count++;
            if (G2_UseTHSignals)
                count++;
            if (G2_UseSJSignals)
                count++;
            if (G2_UseSUSignals)
                count++;

            return count;
        }

        private bool AnyIndividualSignalEnabled ()
        {
            return UseKOSignals
                || UsePASignals
                || UseTHSignals
                || UseSJSignals
                || UseSUSignals;
        }

        private bool AnyIndividualSignalArrowEnabled ()
        {
            return (UseKOSignals && ShowKOSignalArrows)
                || (UsePASignals && ShowPASignalArrows)
                || (UseTHSignals && ShowTHSignalArrows)
                || (UseSJSignals && ShowSJSignalArrows)
                || (UseSUSignals && ShowSUSignalArrows);
        }

        private bool AnySignalArrowEnabled ()
        {
            bool anyIndividualArrowEnabled = AnyIndividualSignalArrowEnabled ();

            bool anyGroupArrowEnabled =
        (IsPrimaryGroupModeActive () || IsSecondaryGroupModeActive ())
        && ShowGroupTriggerArrows;

            return anyIndividualArrowEnabled || anyGroupArrowEnabled;
        }

        private void ModifyGroupTriggerProperties (PropertyDescriptorCollection col)
        {
            int enabledSignals = CountEnabledSignals ();

            if (enabledSignals < 1)
                RemoveProperties (col, "GroupTriggerSet1RequiredCount");

            if (!EnableGroupTriggerSet2)
            {
                RemoveProperties (col,
                    "GroupTriggerSet2RequiredCount",
                    "G2_UseKOSignals",
                    "G2_KO_LongValue",
                    "G2_KO_ShortValue",
                    "G2_UsePASignals",
                    "G2_PA_LongValue",
                    "G2_PA_ShortValue",
                    "G2_UseTHSignals",
                    "G2_TH_LongValue",
                    "G2_TH_ShortValue",
                    "G2_UseSJSignals",
                    "G2_SJ_LongValue",
                    "G2_SJ_ShortValue",
                    "G2_UseSUSignals",
                    "G2_SU_LongValue",
                    "G2_SU_ShortValue");
            }
            else
            {
                if (!G2_UseKOSignals)
                    RemoveProperties (col, "G2_KO_LongValue", "G2_KO_ShortValue");
                if (!G2_UsePASignals)
                    RemoveProperties (col, "G2_PA_LongValue", "G2_PA_ShortValue");
                if (!G2_UseTHSignals)
                    RemoveProperties (col, "G2_TH_LongValue", "G2_TH_ShortValue");
                if (!G2_UseSJSignals)
                    RemoveProperties (col, "G2_SJ_LongValue", "G2_SJ_ShortValue");
                if (!G2_UseSUSignals)
                    RemoveProperties (col, "G2_SU_LongValue", "G2_SU_ShortValue");
            }

            bool anyVisibleGroupEnabled = IsPrimaryGroupModeActive () || IsSecondaryGroupModeActive ();

            if (!anyVisibleGroupEnabled)
            {
                RemoveProperties (col,
                    "ShowGroupTriggerArrows",
                    "ShowGroupTriggerArrowLabel",
                    "GroupTriggerArrowText",
                    "GroupTriggerBrush");
                return;
            }

            if (!ShowGroupTriggerArrows)
                RemoveProperties (col, "ShowGroupTriggerArrowLabel", "GroupTriggerArrowText", "GroupTriggerBrush");
            else if (!ShowGroupTriggerArrowLabel)
                RemoveProperties (col, "GroupTriggerArrowText");
        }

        private void ModifySignalBackBrushProperties (PropertyDescriptorCollection col)
        {
            bool anyValidGroupTriggerAvailable = IsPrimaryGroupModeActive () || IsSecondaryGroupModeActive ();

            if (!anyValidGroupTriggerAvailable)
            {
                RemoveProperties (col,
                    "EnableGroupTriggerBackBrush",
                    "GroupTriggerBackBrush");
            }
            else if (!EnableGroupTriggerBackBrush)
            {
                RemoveProperties (col, "GroupTriggerBackBrush");
            }
        }

        private void ModifyArrowOffsetProperties (PropertyDescriptorCollection col)
        {
            if (!AnySignalArrowEnabled ())
                RemoveProperties (col, "ArrowOffset");
        }

        private void ModifyTradeMarkerProperties (PropertyDescriptorCollection col)
        {
            if (OrderMode != OrderManagementMode.AtmStrategy)
            {
                RemoveProperties (col,
                    "ShowEntryExitMarkers",
                    "EntryExitLineWidth",
                    "EntryExitLongColor",
                    "EntryExitShortColor",
                    "ShowEntryExitLabels",
                    "EntryExitTextSize");

                return;
            }

            if (!ShowEntryExitMarkers)
            {
                RemoveProperties (col,
                    "EntryExitLineWidth",
                    "EntryExitLongColor",
                    "EntryExitShortColor",
                    "ShowEntryExitLabels",
                    "EntryExitTextSize");

                return;
            }

            if (!ShowEntryExitLabels)
                RemoveProperties (col, "EntryExitTextSize");
        }

        public AttributeCollection GetAttributes () => TypeDescriptor.GetAttributes (GetType ());
        public string GetClassName () => TypeDescriptor.GetClassName (GetType ());
        public string GetComponentName () => TypeDescriptor.GetComponentName (GetType ());
        public TypeConverter GetConverter () => TypeDescriptor.GetConverter (GetType ());
        public EventDescriptor GetDefaultEvent () => TypeDescriptor.GetDefaultEvent (GetType ());
        public PropertyDescriptor GetDefaultProperty () => TypeDescriptor.GetDefaultProperty (GetType ());
        public object GetEditor (Type editorBaseType) => TypeDescriptor.GetEditor (GetType (), editorBaseType);
        public EventDescriptorCollection GetEvents (Attribute[] attributes) => TypeDescriptor.GetEvents (GetType (), attributes);
        public EventDescriptorCollection GetEvents () => TypeDescriptor.GetEvents (GetType ());

        public PropertyDescriptorCollection GetProperties (Attribute[] attributes)
        {
            PropertyDescriptorCollection orig = TypeDescriptor.GetProperties(GetType(), attributes);
            PropertyDescriptor[] arr = new PropertyDescriptor[orig.Count];
            orig.CopyTo (arr, 0);
            PropertyDescriptorCollection col = new PropertyDescriptorCollection(arr);
            ModifyOrderManagementProperties (col);
            ModifyPNLProperties (col);
            ModifySignalProperties (col);
            ModifyGroupTriggerProperties (col);
            ModifyArrowOffsetProperties (col);
            ModifySignalTextOffsetProperties (col);
            ModifySignalBackBrushProperties (col);
            ModifySessionProperties (col);
            ModifyEmaFilterProperties (col);
            ModifyNewsFilterProperties (col);
            ModifyIndicatorSettingsProperties (col);
            ModifyTradeMarkerProperties (col);
            return col;
        }

        public PropertyDescriptorCollection GetProperties () => GetProperties (new Attribute[0]);
        public object GetPropertyOwner (PropertyDescriptor pd) => this;
        #endregion

        #region Properties
        [NinjaScriptProperty]
        [ReadOnly (true)]
        [Display (Name = "Strategy Name", GroupName = "Strategy Information", Order = 0)]
        public string StrategyName
        {
            get; set;
        }

        [ReadOnly (true)]
        [Display (Name = "Strategy Version", GroupName = "Strategy Information", Order = 1)]
        public string StrategyVersion => _strategyVersion;

        [NinjaScriptProperty]
        [ReadOnly (true)]
        [Display (Name = "Author", GroupName = "Strategy Information", Order = 2)]
        public string Author
        {
            get; set;
        }

        [NinjaScriptProperty]
        [ReadOnly (true)]
        [Display (Name = "Strategy Credits", GroupName = "Strategy Information", Order = 3)]
        public string StrategyCredits => Credits;

        [NinjaScriptProperty]
        [Display (Name = "Order Management Mode", Order = 0, GroupName = "ATM Parameters", Description = "AtmStrategy uses a selected NT8 ATM template. FixedTicks uses strategy-managed market entries with fixed tick stop loss and profit target.")]
        [RefreshProperties (RefreshProperties.All)]
        public OrderManagementMode OrderMode
        {
            get; set;
        }

        [TypeConverter (typeof (FriendlyAtmConverter))]
        [PropertyEditor ("NinjaTrader.Gui.Tools.StringStandardValuesEditorKey")]
        [Display (Name = "Atm Strategy", Order = 1, GroupName = "ATM Parameters", Description = "Select an existing NT8 ATM template.")]
        public string AtmStrategy
        {
            get; set;
        }

        [NinjaScriptProperty]
        [Range (1, int.MaxValue)]
        [Display (Name = "Fixed Order Quantity", Order = 2, GroupName = "ATM Parameters", Description = "Order quantity used when Order Management Mode is FixedTicks.")]
        public int FixedOrderQuantity
        {
            get; set;
        }

        [NinjaScriptProperty]
        [Range (1, int.MaxValue)]
        [Display (Name = "Fixed Stop Loss Ticks", Order = 3, GroupName = "ATM Parameters", Description = "Fixed strategy-managed stop loss distance in ticks.")]
        public int FixedStopLossTicks
        {
            get; set;
        }

        [NinjaScriptProperty]
        [Range (1, int.MaxValue)]
        [Display (Name = "Fixed Profit Target Ticks", Order = 4, GroupName = "ATM Parameters", Description = "Fixed strategy-managed profit target distance in ticks.")]
        public int FixedProfitTargetTicks
        {
            get; set;
        }

        [NinjaScriptProperty]
        [Display (Name = "Enable Fixed Breakeven", Order = 5, GroupName = "ATM Parameters", Description = "Only used when Order Management Mode is FixedTicks. Moves the fixed stop to breakeven plus/minus offset after price moves in favor by the trigger ticks.")]
        [RefreshProperties (RefreshProperties.All)]
        public bool EnableFixedBreakeven
        {
            get; set;
        }

        [NinjaScriptProperty]
        [Range (1, int.MaxValue)]
        [Display (Name = "Fixed Breakeven Trigger Ticks", Order = 6, GroupName = "ATM Parameters", Description = "Favorable tick distance required before moving the fixed stop to breakeven.")]
        public int FixedBreakevenTriggerTicks
        {
            get; set;
        }

        [NinjaScriptProperty]
        [Range (0, int.MaxValue)]
        [Display (Name = "Fixed Breakeven Offset Ticks", Order = 7, GroupName = "ATM Parameters", Description = "Offset from entry after breakeven is triggered. Long stop = entry + offset. Short stop = entry - offset.")]
        public int FixedBreakevenOffsetTicks
        {
            get; set;
        }

        [TypeConverter (typeof (FriendlyAtmConverter))]
        [PropertyEditor ("NinjaTrader.Gui.Tools.StringStandardValuesEditorKey")]
        [Display (Name = "Martingale ATM Strategy", Order = 8, GroupName = "ATM Parameters", Description = "Select the ATM template used for the one-time opposite-direction martingale recovery entry. Hidden unless Enable Martingale On StopLoss is enabled and Order Management Mode is AtmStrategy.")]
        public string MartingaleAtmStrategy
        {
            get; set;
        }

        // ==================== Signals ====================
        [NinjaScriptProperty]
        [Display (Name = "Enable Signal Tracking", Order = 0, GroupName = "Signals")]
        [RefreshProperties (RefreshProperties.All)]
        public bool EnableSignalTracking
        {
            get; set;
        }

        // -------------------- Trigger Set 1 --------------------
        [NinjaScriptProperty]
        [Range (1, 5)]
        [Display (Name = "Set 1 Required Count", Order = 10, GroupName = "Signals",
            Description = "Number of enabled Set 1 signals that must align on the same bar. Use 1 for a one-signal trigger.")]
        public int GroupTriggerSet1RequiredCount
        {
            get; set;
        }

        [NinjaScriptProperty]
        [Display (Name = "Set 1 Use KingOrderBlock", Order = 12, GroupName = "Signals", Description = "Use gbKingOrderBlock normalized signal as a Set 1 entry source.")]
        [RefreshProperties (RefreshProperties.All)]
        public bool UseKOSignals
        {
            get; set;
        }

        [NinjaScriptProperty]
        [Display (Name = "Set 1 KingOrderBlock Long Value", Order = 13, GroupName = "Signals", Description = "Bullish threshold. +1 when gbKingOrderBlock Signal_Trade >= this value. Valid: 1 = Return Bullish, 2 = Breakout Bullish.")]
        public int KO_LongValue
        {
            get; set;
        }

        [NinjaScriptProperty]
        [Display (Name = "Set 1 KingOrderBlock Short Value", Order = 14, GroupName = "Signals", Description = "Bearish threshold. -1 when gbKingOrderBlock Signal_Trade <= this value. Valid: -1 = Return Bearish, -2 = Breakout Bearish.")]
        public int KO_ShortValue
        {
            get; set;
        }

        [NinjaScriptProperty]
        [Display (Name = "Set 1 Use PANAKanal", Order = 20, GroupName = "Signals", Description = "Use gbPANAKanal normalized signal as a Set 1 entry source.")]
        [RefreshProperties (RefreshProperties.All)]
        public bool UsePASignals
        {
            get; set;
        }

        [NinjaScriptProperty]
        [Display (Name = "Set 1 PANAKanal Long Value", Order = 21, GroupName = "Signals", Description = "Bullish threshold. +1 when gbPANAKanal Signal_Trade >= this value. Valid: 1 = Trend Start Up, 2 = Break Up, 3 = Pullback Bullish.")]
        public int PA_LongValue
        {
            get; set;
        }

        [NinjaScriptProperty]
        [Display (Name = "Set 1 PANAKanal Short Value", Order = 22, GroupName = "Signals", Description = "Bearish threshold. -1 when gbPANAKanal Signal_Trade <= this value. Valid: -1 = Trend Start Down, -2 = Break Down, -3 = Pullback Bearish.")]
        public int PA_ShortValue
        {
            get; set;
        }

        [NinjaScriptProperty]
        [Display (Name = "Set 1 Use ThunderZilla", Order = 30, GroupName = "Signals", Description = "Use gbThunderZilla normalized signal as a Set 1 entry source.")]
        [RefreshProperties (RefreshProperties.All)]
        public bool UseTHSignals
        {
            get; set;
        }

        [NinjaScriptProperty]
        [Display (Name = "Set 1 ThunderZilla Long Value", Order = 31, GroupName = "Signals", Description = "Bullish threshold. +1 when gbThunderZilla Signal_Trade >= this value. Valid: 1 = Uptrend Start, 2 = Downtrend Slowdown, 3 = Uptrend Pullback, 4 = Move Stop Up.")]
        public int TH_LongValue
        {
            get; set;
        }

        [NinjaScriptProperty]
        [Display (Name = "Set 1 ThunderZilla Short Value", Order = 32, GroupName = "Signals", Description = "Bearish threshold. -1 when gbThunderZilla Signal_Trade <= this value. Valid: -1 = Downtrend Start, -2 = Uptrend Slowdown, -3 = Downtrend Pullback, -4 = Move Stop Down.")]
        public int TH_ShortValue
        {
            get; set;
        }

        [NinjaScriptProperty]
        [Display (Name = "Set 1 Use SuperJumpBoost", Order = 40, GroupName = "Signals", Description = "Use gbSuperJumpBoost normalized signal as a Set 1 entry source.")]
        [RefreshProperties (RefreshProperties.All)]
        public bool UseSJSignals
        {
            get; set;
        }

        [NinjaScriptProperty]
        [Display (Name = "Set 1 SuperJumpBoost Long Value", Order = 41, GroupName = "Signals", Description = "Bullish threshold. +1 when gbSuperJumpBoost Signal_Trade >= this value. Valid: 1 = Bullish Return, 2 = Bullish Zone Start.")]
        public int SJ_LongValue
        {
            get; set;
        }

        [NinjaScriptProperty]
        [Display (Name = "Set 1 SuperJumpBoost Short Value", Order = 42, GroupName = "Signals", Description = "Bearish threshold. -1 when gbSuperJumpBoost Signal_Trade <= this value. Valid: -1 = Bearish Return, -2 = Bearish Zone Start.")]
        public int SJ_ShortValue
        {
            get; set;
        }

        [NinjaScriptProperty]
        [Display (Name = "Set 1 Use SumoPullback", Order = 50, GroupName = "Signals", Description = "Use gbSumoPullback normalized signal as a Set 1 entry source.")]
        [RefreshProperties (RefreshProperties.All)]
        public bool UseSUSignals
        {
            get; set;
        }

        [NinjaScriptProperty]
        [Display (Name = "Set 1 SumoPullback Long Value", Order = 51, GroupName = "Signals", Description = "Bullish threshold. +1 when gbSumoPullback Signal_Trade >= this value. Valid: 1 = Bullish Sumo.")]
        public int SU_LongValue
        {
            get; set;
        }

        [NinjaScriptProperty]
        [Display (Name = "Set 1 SumoPullback Short Value", Order = 52, GroupName = "Signals", Description = "Bearish threshold. -1 when gbSumoPullback Signal_Trade <= this value. Valid: -1 = Bearish Sumo.")]
        public int SU_ShortValue
        {
            get; set;
        }

        // -------------------- Trigger Set 2 --------------------
        [NinjaScriptProperty]
        [Display (Name = "Set 2 Enable Group Trigger", Order = 100, GroupName = "Signals",
            Description = "Optional second same-bar group trigger set with its own selected indicators and signal values. Use Set 2 Required Count = 1 for a one-signal trigger.")]
        [RefreshProperties (RefreshProperties.All)]
        public bool EnableGroupTriggerSet2
        {
            get; set;
        }

        [NinjaScriptProperty]
        [Range (1, 5)]
        [Display (Name = "Set 2 Required Count", Order = 101, GroupName = "Signals",
            Description = "Number of enabled Set 2 signals that must align on the same bar. Use 1 for a one-signal trigger.")]
        public int GroupTriggerSet2RequiredCount
        {
            get; set;
        }

        [NinjaScriptProperty]
        [Display (Name = "Set 2 Use KingOrderBlock", Order = 102, GroupName = "Signals")]
        [RefreshProperties (RefreshProperties.All)]
        public bool G2_UseKOSignals
        {
            get; set;
        }

        [NinjaScriptProperty]
        [Display (Name = "Set 2 KingOrderBlock Long Value", Order = 103, GroupName = "Signals")]
        public int G2_KO_LongValue
        {
            get; set;
        }

        [NinjaScriptProperty]
        [Display (Name = "Set 2 KingOrderBlock Short Value", Order = 104, GroupName = "Signals")]
        public int G2_KO_ShortValue
        {
            get; set;
        }

        [NinjaScriptProperty]
        [Display (Name = "Set 2 Use PANAKanal", Order = 110, GroupName = "Signals")]
        [RefreshProperties (RefreshProperties.All)]
        public bool G2_UsePASignals
        {
            get; set;
        }

        [NinjaScriptProperty]
        [Display (Name = "Set 2 PANAKanal Long Value", Order = 111, GroupName = "Signals")]
        public int G2_PA_LongValue
        {
            get; set;
        }

        [NinjaScriptProperty]
        [Display (Name = "Set 2 PANAKanal Short Value", Order = 112, GroupName = "Signals")]
        public int G2_PA_ShortValue
        {
            get; set;
        }

        [NinjaScriptProperty]
        [Display (Name = "Set 2 Use ThunderZilla", Order = 120, GroupName = "Signals")]
        [RefreshProperties (RefreshProperties.All)]
        public bool G2_UseTHSignals
        {
            get; set;
        }

        [NinjaScriptProperty]
        [Display (Name = "Set 2 ThunderZilla Long Value", Order = 121, GroupName = "Signals")]
        public int G2_TH_LongValue
        {
            get; set;
        }

        [NinjaScriptProperty]
        [Display (Name = "Set 2 ThunderZilla Short Value", Order = 122, GroupName = "Signals")]
        public int G2_TH_ShortValue
        {
            get; set;
        }

        [NinjaScriptProperty]
        [Display (Name = "Set 2 Use SuperJumpBoost", Order = 130, GroupName = "Signals")]
        [RefreshProperties (RefreshProperties.All)]
        public bool G2_UseSJSignals
        {
            get; set;
        }

        [NinjaScriptProperty]
        [Display (Name = "Set 2 SuperJumpBoost Long Value", Order = 131, GroupName = "Signals")]
        public int G2_SJ_LongValue
        {
            get; set;
        }

        [NinjaScriptProperty]
        [Display (Name = "Set 2 SuperJumpBoost Short Value", Order = 132, GroupName = "Signals")]
        public int G2_SJ_ShortValue
        {
            get; set;
        }

        [NinjaScriptProperty]
        [Display (Name = "Set 2 Use SumoPullback", Order = 140, GroupName = "Signals")]
        [RefreshProperties (RefreshProperties.All)]
        public bool G2_UseSUSignals
        {
            get; set;
        }

        [NinjaScriptProperty]
        [Display (Name = "Set 2 SumoPullback Long Value", Order = 141, GroupName = "Signals")]
        public int G2_SU_LongValue
        {
            get; set;
        }

        [NinjaScriptProperty]
        [Display (Name = "Set 2 SumoPullback Short Value", Order = 142, GroupName = "Signals")]
        public int G2_SU_ShortValue
        {
            get; set;
        }

        // ==================== Filters ====================
        [NinjaScriptProperty]
        [Display (Name = "Enable News Filter", Order = 0, GroupName = "Filters", Description = "Enable NewsSignals live news filter. Disabled automatically during Strategy Analyzer/backtest and Playback/Market Replay.")]
        [RefreshProperties (RefreshProperties.All)]
        public bool EnableNewsFilter
        {
            get; set;
        }

        [NinjaScriptProperty]
        [Display (Name = "Flatten/Cancel At News Warning", Order = 1, GroupName = "Filters", Description = "If enabled, closes tracked ATM position and cancels working orders when the pre-news warning window starts.")]
        public bool NewsFlattenAtWarningTime
        {
            get; set;
        }

        [NinjaScriptProperty]
        [Display (Name = "Show News Display", Order = 2, GroupName = "Filters", Description = "Show or hide the NewsSignals chart display.")]
        public bool NewsShowDisplay
        {
            get; set;
        }

        [NinjaScriptProperty]
        [Display (Name = "Display Location", Order = 3, GroupName = "Filters")]
        public NewsPrintLocation NewsDisplayLocation
        {
            get; set;
        }

        [NinjaScriptProperty]
        [Range (0, 5000)]
        [Display (Name = "Display X Offset Pixels", Order = 4, GroupName = "Filters")]
        public int NewsDisplayXOffsetPixels
        {
            get; set;
        }

        [NinjaScriptProperty]
        [Range (0, 5000)]
        [Display (Name = "Display Y Offset Pixels", Order = 5, GroupName = "Filters")]
        public int NewsDisplayYOffsetPixels
        {
            get; set;
        }

        [NinjaScriptProperty]
        [Display (Name = "Use 24-Hour Time", Order = 6, GroupName = "Filters")]
        public bool NewsUse24HourTime
        {
            get; set;
        }

        [NinjaScriptProperty]
        [Display (Name = "Show Background", Order = 7, GroupName = "Filters")]
        public bool NewsShowBackground
        {
            get; set;
        }

        [NinjaScriptProperty]
        [Display (Name = "Show Time BackBrush", Order = 8, GroupName = "Filters")]
        public bool NewsShowTimeBackBrush
        {
            get; set;
        }

        [XmlIgnore ()]
        [NinjaScriptProperty]
        [Display (Name = "Time BackBrush", Order = 9, GroupName = "Filters")]
        public Brush NewsTimeBackBrush
        {
            get; set;
        }

        [Browsable (false)]
        public string NewsTimeBackBrushSerialize
        {
            get
            {
                return Serialize.BrushToString (NewsTimeBackBrush);
            }
            set
            {
                NewsTimeBackBrush = Serialize.StringToBrush (value);
            }
        }

        [NinjaScriptProperty]
        [Display (Name = "US Events Only", Order = 10, GroupName = "Filters")]
        public bool NewsUSOnlyEvents
        {
            get; set;
        }

        [NinjaScriptProperty]
        [Display (Name = "Today's News Only", Order = 11, GroupName = "Filters")]
        public bool NewsTodaysNewsOnly
        {
            get; set;
        }

        [NinjaScriptProperty]
        [Display (Name = "Show Low Priority", Order = 12, GroupName = "Filters")]
        public bool NewsShowLowPriority
        {
            get; set;
        }

        [NinjaScriptProperty]
        [Range (1, int.MaxValue)]
        [Display (Name = "Max News Items", Order = 13, GroupName = "Filters")]
        public int NewsMaxNewsItems
        {
            get; set;
        }

        [NinjaScriptProperty]
        [Range (1, int.MaxValue)]
        [Display (Name = "Refresh Interval Minutes", Order = 14, GroupName = "Filters")]
        public int NewsRefreshInterval
        {
            get; set;
        }

        [NinjaScriptProperty]
        [Range (0, 240)]
        [Display (Name = "Pre-News Block Minutes", Order = 15, GroupName = "Filters")]
        public int NewsPreBlockMinutes
        {
            get; set;
        }

        [NinjaScriptProperty]
        [Range (0, 240)]
        [Display (Name = "Post-News Block Minutes", Order = 16, GroupName = "Filters")]
        public int NewsPostBlockMinutes
        {
            get; set;
        }

        [NinjaScriptProperty]
        [Display (Name = "Block High Impact", Order = 17, GroupName = "Filters")]
        public bool NewsBlockHighImpact
        {
            get; set;
        }

        [NinjaScriptProperty]
        [Display (Name = "Block Medium Impact", Order = 18, GroupName = "Filters")]
        public bool NewsBlockMediumImpact
        {
            get; set;
        }

        [NinjaScriptProperty]
        [Display (Name = "Block Low Impact", Order = 19, GroupName = "Filters")]
        public bool NewsBlockLowImpact
        {
            get; set;
        }

        [NinjaScriptProperty]
        [Display (Name = "Send Alerts", Order = 20, GroupName = "Filters")]
        public bool NewsSendAlerts
        {
            get; set;
        }

        [NinjaScriptProperty]
        [Range (0, 240)]
        [Display (Name = "Alert Interval Minutes", Order = 21, GroupName = "Filters")]
        public int NewsAlertInterval
        {
            get; set;
        }

        [NinjaScriptProperty]
        [Display (Name = "Alert WAV File", Order = 22, GroupName = "Filters")]
        public string NewsAlertWavFileName
        {
            get; set;
        }

        [XmlIgnore ()]
        [NinjaScriptProperty]
        [Display (Name = "Default Text Color", Order = 23, GroupName = "Filters")]
        public Brush NewsDefaultTextColor
        {
            get; set;
        }

        [Browsable (false)]
        public string NewsDefaultTextColorSerialize
        {
            get
            {
                return Serialize.BrushToString (NewsDefaultTextColor);
            }
            set
            {
                NewsDefaultTextColor = Serialize.StringToBrush (value);
            }
        }

        [XmlIgnore ()]
        [NinjaScriptProperty]
        [Display (Name = "Warning Text Color", Order = 24, GroupName = "Filters")]
        public Brush NewsWarningTextColor
        {
            get; set;
        }

        [Browsable (false)]
        public string NewsWarningTextColorSerialize
        {
            get
            {
                return Serialize.BrushToString (NewsWarningTextColor);
            }
            set
            {
                NewsWarningTextColor = Serialize.StringToBrush (value);
            }
        }

        private static readonly Brush _newsBackgroundColorDefault = MakeFrozenBrush (170, 0, 0, 0);

        private static Brush MakeFrozenBrush (byte a, byte r, byte g, byte b)
        {
            var br = new SolidColorBrush (Color.FromArgb (a, r, g, b));
            br.Freeze ();
            return br;
        }

        private Brush _newsBackgroundColor;

        [XmlIgnore ()]
        [NinjaScriptProperty]
        [Display (Name = "Background Color", Order = 25, GroupName = "Filters")]
        public Brush NewsBackgroundColor
        {
            get
            {
                return _newsBackgroundColor ?? _newsBackgroundColorDefault;
            }
            set
            {
                _newsBackgroundColor = value;
            }
        }

        [Browsable (false)]
        public string NewsBackgroundColorSerialize
        {
            get
            {
                return Serialize.BrushToString (_newsBackgroundColor ?? _newsBackgroundColorDefault);
            }
            set
            {
                _newsBackgroundColor = Serialize.StringToBrush (value) ?? _newsBackgroundColorDefault;
            }
        }

        [XmlIgnore ()]
        [NinjaScriptProperty]
        [Display (Name = "Header Text Color", Order = 26, GroupName = "Filters")]
        public Brush NewsHeaderColor
        {
            get; set;
        }

        [Browsable (false)]
        public string NewsHeaderColorSerialize
        {
            get
            {
                return Serialize.BrushToString (NewsHeaderColor);
            }
            set
            {
                NewsHeaderColor = Serialize.StringToBrush (value);
            }
        }

        [XmlIgnore ()]
        [NinjaScriptProperty]
        [Display (Name = "High Impact Text Color", Order = 27, GroupName = "Filters")]
        public Brush NewsHighImpactColor
        {
            get; set;
        }

        [Browsable (false)]
        public string NewsHighImpactColorSerialize
        {
            get
            {
                return Serialize.BrushToString (NewsHighImpactColor);
            }
            set
            {
                NewsHighImpactColor = Serialize.StringToBrush (value);
            }
        }

        [XmlIgnore ()]
        [NinjaScriptProperty]
        [Display (Name = "Medium Impact Text Color", Order = 28, GroupName = "Filters")]
        public Brush NewsMediumImpactColor
        {
            get; set;
        }

        [Browsable (false)]
        public string NewsMediumImpactColorSerialize
        {
            get
            {
                return Serialize.BrushToString (NewsMediumImpactColor);
            }
            set
            {
                NewsMediumImpactColor = Serialize.StringToBrush (value);
            }
        }

        [XmlIgnore ()]
        [NinjaScriptProperty]
        [Display (Name = "Low Impact Text Color", Order = 29, GroupName = "Filters")]
        public Brush NewsLowImpactColor
        {
            get; set;
        }

        [Browsable (false)]
        public string NewsLowImpactColorSerialize
        {
            get
            {
                return Serialize.BrushToString (NewsLowImpactColor);
            }
            set
            {
                NewsLowImpactColor = Serialize.StringToBrush (value);
            }
        }

        [XmlIgnore ()]
        [NinjaScriptProperty]
        [Display (Name = "Default Font", Order = 30, GroupName = "Filters")]
        public SimpleFont NewsDefaultFont
        {
            get; set;
        }

        [Browsable (false)]
        public string NewsDefaultFontSerialize
        {
            get
            {
                return NewsDefaultFont.FamilySerialize;
            }
            set
            {
                NewsDefaultFont = new SimpleFont (value, NewsDefaultFontSizeSerialize);
            }
        }

        [Browsable (false)]
        public double NewsDefaultFontSizeSerialize
        {
            get
            {
                return NewsDefaultFont.Size;
            }
            set
            {
                NewsDefaultFont.Size = value;
            }
        }

        [XmlIgnore ()]
        [NinjaScriptProperty]
        [Display (Name = "Warning Font", Order = 31, GroupName = "Filters")]
        public SimpleFont NewsWarningFont
        {
            get; set;
        }

        [Browsable (false)]
        public string NewsWarningFontSerialize
        {
            get
            {
                return NewsWarningFont.FamilySerialize;
            }
            set
            {
                NewsWarningFont = new SimpleFont (value, NewsWarningFontSizeSerialize);
            }
        }

        [Browsable (false)]
        public double NewsWarningFontSizeSerialize
        {
            get
            {
                return NewsWarningFont.Size;
            }
            set
            {
                NewsWarningFont.Size = value;
            }
        }

        [NinjaScriptProperty]
        [Display (Name = "Debug", Order = 32, GroupName = "Filters")]
        public bool NewsDebug
        {
            get; set;
        }

        // ==================== EMA Filter ====================
        [NinjaScriptProperty]
        [Display (Name = "Enable EMA Filter", Order = 33, GroupName = "Filters", Description = "When enabled, longs require short EMA above long EMA and shorts require short EMA below long EMA.")]
        [RefreshProperties (RefreshProperties.All)]
        public bool EnableEmaFilter
        {
            get; set;
        }

        [NinjaScriptProperty]
        [Range (1, int.MaxValue)]
        [Display (Name = "Short EMA Period", Order = 34, GroupName = "Filters")]
        public int EmaShortPeriod
        {
            get; set;
        }

        [NinjaScriptProperty]
        [Range (1, int.MaxValue)]
        [Display (Name = "Long EMA Period", Order = 35, GroupName = "Filters")]
        public int EmaLongPeriod
        {
            get; set;
        }

        // ==================== Risk Management ====================
        [NinjaScriptProperty]
        [Display (Name = "Use Unrealized PNL", Order = 0, GroupName = "Risk Management", Description = "If true, checks limits tick-by-tick including ATM open profit.")]
        [RefreshProperties (RefreshProperties.All)]
        public bool UseUnrealizedPnl
        {
            get; set;
        }

        [NinjaScriptProperty]
        [Display (Name = "Enable Daily Profit Target", Order = 1, GroupName = "Risk Management")]
        [RefreshProperties (RefreshProperties.All)]
        public bool EnableDailyProfitTarget
        {
            get; set;
        }

        [NinjaScriptProperty]
        [Range (1, double.MaxValue)]
        [Display (Name = "Daily Profit Target ($)", Order = 2, GroupName = "Risk Management")]
        public double DailyProfitTarget
        {
            get; set;
        }

        [NinjaScriptProperty]
        [Display (Name = "Enable Daily Loss Limit", Order = 3, GroupName = "Risk Management")]
        [RefreshProperties (RefreshProperties.All)]
        public bool EnableDailyLossLimit
        {
            get; set;
        }

        [NinjaScriptProperty]
        [Range (1, double.MaxValue)]
        [Display (Name = "Daily Loss Limit ($)", Order = 4, GroupName = "Risk Management", Description = "Positive Number (e.g. 500 for -$500 limit)")]
        public double DailyLossLimit
        {
            get; set;
        }

        [NinjaScriptProperty]
        [Display (Name = "Enable Martingale On StopLoss", Order = 5, GroupName = "Risk Management", Description = "If enabled, a losing normal ATM trade submits one opposite-direction recovery entry using the Martingale ATM Strategy. A losing martingale entry does not trigger another martingale.")]
        [RefreshProperties (RefreshProperties.All)]
        public bool EnableMartingaleOnStopLoss
        {
            get; set;
        }

        // ==================== Session Parameters ====================
        [Display (Name = "Enable TF 1", Order = 1, GroupName = "Session Parameters")]
        [RefreshProperties (RefreshProperties.All)]
        public bool EnableTF1
        {
            get; set;
        }

        [NinjaScriptProperty]
        [PropertyEditor ("NinjaTrader.Gui.Tools.TimeEditorKey")]
        [Display (Name = "Start Time 1", Order = 2, GroupName = "Session Parameters")]
        public DateTime StartTime1
        {
            get; set;
        }

        [NinjaScriptProperty]
        [PropertyEditor ("NinjaTrader.Gui.Tools.TimeEditorKey")]
        [Display (Name = "End Time 1", Order = 3, GroupName = "Session Parameters")]
        public DateTime EndTime1
        {
            get; set;
        }

        [Display (Name = "Flatten at End TF 1", Order = 4, GroupName = "Session Parameters")]
        public bool FlattenTF1
        {
            get; set;
        }

        [Display (Name = "Enable TF 2", Order = 5, GroupName = "Session Parameters")]
        [RefreshProperties (RefreshProperties.All)]
        public bool EnableTF2
        {
            get; set;
        }

        [NinjaScriptProperty]
        [PropertyEditor ("NinjaTrader.Gui.Tools.TimeEditorKey")]
        [Display (Name = "Start Time 2", Order = 6, GroupName = "Session Parameters")]
        public DateTime StartTime2
        {
            get; set;
        }

        [NinjaScriptProperty]
        [PropertyEditor ("NinjaTrader.Gui.Tools.TimeEditorKey")]
        [Display (Name = "End Time 2", Order = 7, GroupName = "Session Parameters")]
        public DateTime EndTime2
        {
            get; set;
        }

        [Display (Name = "Flatten at End TF 2", Order = 8, GroupName = "Session Parameters")]
        public bool FlattenTF2
        {
            get; set;
        }

        [Display (Name = "Enable TF 3", Order = 9, GroupName = "Session Parameters")]
        [RefreshProperties (RefreshProperties.All)]
        public bool EnableTF3
        {
            get; set;
        }

        [NinjaScriptProperty]
        [PropertyEditor ("NinjaTrader.Gui.Tools.TimeEditorKey")]
        [Display (Name = "Start Time 3", Order = 10, GroupName = "Session Parameters")]
        public DateTime StartTime3
        {
            get; set;
        }

        [NinjaScriptProperty]
        [PropertyEditor ("NinjaTrader.Gui.Tools.TimeEditorKey")]
        [Display (Name = "End Time 3", Order = 11, GroupName = "Session Parameters")]
        public DateTime EndTime3
        {
            get; set;
        }

        [Display (Name = "Flatten at End TF 3", Order = 12, GroupName = "Session Parameters")]
        public bool FlattenTF3
        {
            get; set;
        }

        [Display (Name = "Enable Skip Window", Order = 13, GroupName = "Session Parameters")]
        [RefreshProperties (RefreshProperties.All)]
        public bool EnableSkipTimeWindow
        {
            get; set;
        }

        [NinjaScriptProperty]
        [PropertyEditor ("NinjaTrader.Gui.Tools.TimeEditorKey")]
        [Display (Name = "Skip Start Time", Order = 14, GroupName = "Session Parameters")]
        public DateTime SkipStartTime
        {
            get; set;
        }

        [NinjaScriptProperty]
        [PropertyEditor ("NinjaTrader.Gui.Tools.TimeEditorKey")]
        [Display (Name = "Skip End Time", Order = 15, GroupName = "Session Parameters")]
        public DateTime SkipEndTime
        {
            get; set;
        }

        // ==================== Indicator Settings ====================
        [NinjaScriptProperty]
        [Display (Name = "Show Indicator Settings", Order = 0, GroupName = "Indicator Settings", Description = "When checked, exposes enabled child indicator parameters below.")]
        [RefreshProperties (RefreshProperties.All)]
        public bool ShowIndicatorSettings
        {
            get; set;
        }

        // ==================== KingOrderBlock ====================
        [NinjaScriptProperty]
        [Range (1, int.MaxValue)]
        [Display (Name = "Swing Point: Neighborhood", Order = 0, GroupName = "Indicator: KingOrderBlock")]
        public int King_SwingPointNeighborhood
        {
            get; set;
        }

        [NinjaScriptProperty]
        [Range (1, int.MaxValue)]
        [Display (Name = "Imbalance: Qualifying (Bars)", Order = 10, GroupName = "Indicator: KingOrderBlock")]
        public int King_ImbalanceQualifying
        {
            get; set;
        }

        [NinjaScriptProperty]
        [Range (1, int.MaxValue)]
        [Display (Name = "Order Block: Finding BOS/CHoCH Period", Order = 20, GroupName = "Indicator: KingOrderBlock")]
        public int King_OrderBlockFindingBosChochPeriod
        {
            get; set;
        }

        [NinjaScriptProperty]
        [Range (0, int.MaxValue)]
        [Display (Name = "Order Block: Age (Bars)", Order = 30, GroupName = "Indicator: KingOrderBlock")]
        public int King_OrderBlockAge
        {
            get; set;
        }

        [NinjaScriptProperty]
        [Range (0, int.MaxValue)]
        [Display (Name = "Order Blocks: Same Direction Offset (Ticks)", Order = 40, GroupName = "Indicator: KingOrderBlock")]
        public int King_OrderBlocksSameDirectionOffset
        {
            get; set;
        }

        [NinjaScriptProperty]
        [Range (0, int.MaxValue)]
        [Display (Name = "Order Blocks: Diff Direction Offset (Ticks)", Order = 50, GroupName = "Indicator: KingOrderBlock")]
        public int King_OrderBlocksDifferenceDirectionOffset
        {
            get; set;
        }

        [NinjaScriptProperty]
        [Range (1, int.MaxValue)]
        [Display (Name = "Signal Trade: Quantity Per OB", Order = 60, GroupName = "Indicator: KingOrderBlock")]
        public int King_SignalTradeQuantityPerOrderBlock
        {
            get; set;
        }

        [NinjaScriptProperty]
        [Range (1, int.MaxValue)]
        [Display (Name = "Signal Trade: Split (Bars)", Order = 70, GroupName = "Indicator: KingOrderBlock")]
        public int King_SignalTradeSplitBars
        {
            get; set;
        }

        // ==================== PANAKanal ====================
        [NinjaScriptProperty]
        [Range (1, int.MaxValue)]
        [Display (Name = "Period", Order = 0, GroupName = "Indicator: PANAKanal")]
        public int Pana_Period
        {
            get; set;
        }

        [NinjaScriptProperty]
        [Range (0.01, double.MaxValue)]
        [Display (Name = "Factor", Order = 10, GroupName = "Indicator: PANAKanal")]
        public double Pana_Factor
        {
            get; set;
        }

        [NinjaScriptProperty]
        [Range (1, int.MaxValue)]
        [Display (Name = "Middle Period", Order = 20, GroupName = "Indicator: PANAKanal")]
        public int Pana_MiddlePeriod
        {
            get; set;
        }

        [NinjaScriptProperty]
        [Range (1, int.MaxValue)]
        [Display (Name = "Signal Break Split (Bars)", Order = 30, GroupName = "Indicator: PANAKanal")]
        public int Pana_SignalBreakSplit
        {
            get; set;
        }

        [NinjaScriptProperty]
        [Range (1, int.MaxValue)]
        [Display (Name = "Signal Pullback Finding Period", Order = 40, GroupName = "Indicator: PANAKanal")]
        public int Pana_SignalPullbackFindingPeriod
        {
            get; set;
        }

        // ==================== ThunderZilla ====================
        [NinjaScriptProperty]
        [Display (Name = "Trend: MA Type", Order = 0, GroupName = "Indicator: ThunderZilla")]
        public gbThunderZillaMAType Thunder_TrendMAType
        {
            get; set;
        }

        [NinjaScriptProperty]
        [Range (1, int.MaxValue)]
        [Display (Name = "Trend: Period", Order = 10, GroupName = "Indicator: ThunderZilla")]
        public int Thunder_TrendPeriod
        {
            get; set;
        }

        [NinjaScriptProperty]
        [Display (Name = "Trend: Smoothing Enabled", Order = 20, GroupName = "Indicator: ThunderZilla")]
        public bool Thunder_TrendSmoothingEnabled
        {
            get; set;
        }

        [NinjaScriptProperty]
        [Display (Name = "Trend: Smoothing Method", Order = 30, GroupName = "Indicator: ThunderZilla")]
        public gbThunderZillaMAType Thunder_TrendSmoothingMethod
        {
            get; set;
        }

        [NinjaScriptProperty]
        [Range (1, int.MaxValue)]
        [Display (Name = "Trend: Smoothing Period", Order = 40, GroupName = "Indicator: ThunderZilla")]
        public int Thunder_TrendSmoothingPeriod
        {
            get; set;
        }

        [NinjaScriptProperty]
        [Range (0.0, double.MaxValue)]
        [Display (Name = "Stop: Offset Multiplier (Ticks)", Order = 50, GroupName = "Indicator: ThunderZilla")]
        public double Thunder_StopOffsetMultiplierStop
        {
            get; set;
        }

        [NinjaScriptProperty]
        [Range (1, int.MaxValue)]
        [Display (Name = "Signal: Quantity Per Flat", Order = 60, GroupName = "Indicator: ThunderZilla")]
        public int Thunder_SignalQuantityPerFlat
        {
            get; set;
        }

        [NinjaScriptProperty]
        [Range (1, int.MaxValue)]
        [Display (Name = "Signal: Quantity Per Trend", Order = 70, GroupName = "Indicator: ThunderZilla")]
        public int Thunder_SignalQuantityPerTrend
        {
            get; set;
        }

        // ==================== SumoPullback ====================
        [NinjaScriptProperty]
        [Display (Name = "Slow MA: Type", Order = 0, GroupName = "Indicator: SumoPullback")]
        public gbSumoPullbackMAType SU_SlowMAType
        {
            get; set;
        }

        [NinjaScriptProperty]
        [Range (1, int.MaxValue)]
        [Display (Name = "Slow MA: Period", Order = 1, GroupName = "Indicator: SumoPullback")]
        public int SU_SlowMAPeriod
        {
            get; set;
        }

        [NinjaScriptProperty]
        [Display (Name = "Slow MA: Smoothing Enabled", Order = 2, GroupName = "Indicator: SumoPullback")]
        public bool SU_SlowMASmoothingEnabled
        {
            get; set;
        }

        [NinjaScriptProperty]
        [Display (Name = "Slow MA: Smoothing Method", Order = 3, GroupName = "Indicator: SumoPullback")]
        public gbSumoPullbackMAType SU_SlowMASmoothingMethod
        {
            get; set;
        }

        [NinjaScriptProperty]
        [Range (1, int.MaxValue)]
        [Display (Name = "Slow MA: Smoothing Period", Order = 4, GroupName = "Indicator: SumoPullback")]
        public int SU_SlowMASmoothingPeriod
        {
            get; set;
        }

        [NinjaScriptProperty]
        [Display (Name = "Fast MA #1: Type", Order = 10, GroupName = "Indicator: SumoPullback")]
        public gbSumoPullbackMAType SU_FastMA1Type
        {
            get; set;
        }

        [NinjaScriptProperty]
        [Range (1, int.MaxValue)]
        [Display (Name = "Fast MA #1: Period", Order = 11, GroupName = "Indicator: SumoPullback")]
        public int SU_FastMA1Period
        {
            get; set;
        }

        [NinjaScriptProperty]
        [Display (Name = "Fast MA #1: Smoothing Enabled", Order = 12, GroupName = "Indicator: SumoPullback")]
        public bool SU_FastMA1SmoothingEnabled
        {
            get; set;
        }

        [NinjaScriptProperty]
        [Display (Name = "Fast MA #1: Smoothing Method", Order = 13, GroupName = "Indicator: SumoPullback")]
        public gbSumoPullbackMAType SU_FastMA1SmoothingMethod
        {
            get; set;
        }

        [NinjaScriptProperty]
        [Range (1, int.MaxValue)]
        [Display (Name = "Fast MA #1: Smoothing Period", Order = 14, GroupName = "Indicator: SumoPullback")]
        public int SU_FastMA1SmoothingPeriod
        {
            get; set;
        }

        [NinjaScriptProperty]
        [Display (Name = "Fast MA #2: Type", Order = 20, GroupName = "Indicator: SumoPullback")]
        public gbSumoPullbackMAType SU_FastMA2Type
        {
            get; set;
        }

        [NinjaScriptProperty]
        [Range (1, int.MaxValue)]
        [Display (Name = "Fast MA #2: Period", Order = 21, GroupName = "Indicator: SumoPullback")]
        public int SU_FastMA2Period
        {
            get; set;
        }

        [NinjaScriptProperty]
        [Display (Name = "Fast MA #2: Smoothing Enabled", Order = 22, GroupName = "Indicator: SumoPullback")]
        public bool SU_FastMA2SmoothingEnabled
        {
            get; set;
        }

        [NinjaScriptProperty]
        [Display (Name = "Fast MA #2: Smoothing Method", Order = 23, GroupName = "Indicator: SumoPullback")]
        public gbSumoPullbackMAType SU_FastMA2SmoothingMethod
        {
            get; set;
        }

        [NinjaScriptProperty]
        [Range (1, int.MaxValue)]
        [Display (Name = "Fast MA #2: Smoothing Period", Order = 24, GroupName = "Indicator: SumoPullback")]
        public int SU_FastMA2SmoothingPeriod
        {
            get; set;
        }

        [NinjaScriptProperty]
        [Display (Name = "Fast MA #3: Type", Order = 30, GroupName = "Indicator: SumoPullback")]
        public gbSumoPullbackMAType SU_FastMA3Type
        {
            get; set;
        }

        [NinjaScriptProperty]
        [Range (1, int.MaxValue)]
        [Display (Name = "Fast MA #3: Period", Order = 31, GroupName = "Indicator: SumoPullback")]
        public int SU_FastMA3Period
        {
            get; set;
        }

        [NinjaScriptProperty]
        [Display (Name = "Fast MA #3: Smoothing Enabled", Order = 32, GroupName = "Indicator: SumoPullback")]
        public bool SU_FastMA3SmoothingEnabled
        {
            get; set;
        }

        [NinjaScriptProperty]
        [Display (Name = "Fast MA #3: Smoothing Method", Order = 33, GroupName = "Indicator: SumoPullback")]
        public gbSumoPullbackMAType SU_FastMA3SmoothingMethod
        {
            get; set;
        }

        [NinjaScriptProperty]
        [Range (1, int.MaxValue)]
        [Display (Name = "Fast MA #3: Smoothing Period", Order = 34, GroupName = "Indicator: SumoPullback")]
        public int SU_FastMA3SmoothingPeriod
        {
            get; set;
        }

        [NinjaScriptProperty]
        [Range (1, int.MaxValue)]
        [Display (Name = "Signal Split: First", Order = 40, GroupName = "Indicator: SumoPullback")]
        public int SU_SignalSplitFirst
        {
            get; set;
        }

        [NinjaScriptProperty]
        [Range (1, int.MaxValue)]
        [Display (Name = "Signal Split: Second", Order = 41, GroupName = "Indicator: SumoPullback")]
        public int SU_SignalSplitSecond
        {
            get; set;
        }

        // ==================== SuperJumpBoost ====================
        [NinjaScriptProperty]
        [Display (Name = "Sensitive Mode Enabled", Order = 0, GroupName = "Indicator: SuperJumpBoost")]
        public bool SJ_SensitiveModeEnabled
        {
            get; set;
        }

        [NinjaScriptProperty]
        [Range (0.01, double.MaxValue)]
        [Display (Name = "Offset Level 1", Order = 10, GroupName = "Indicator: SuperJumpBoost")]
        public double SJ_OffsetLevel1
        {
            get; set;
        }

        [NinjaScriptProperty]
        [Range (0.01, double.MaxValue)]
        [Display (Name = "Offset Level 2", Order = 11, GroupName = "Indicator: SuperJumpBoost")]
        public double SJ_OffsetLevel2
        {
            get; set;
        }

        [NinjaScriptProperty]
        [Range (0.01, double.MaxValue)]
        [Display (Name = "Offset Level 3", Order = 12, GroupName = "Indicator: SuperJumpBoost")]
        public double SJ_OffsetLevel3
        {
            get; set;
        }

        [NinjaScriptProperty]
        [Range (0.01, double.MaxValue)]
        [Display (Name = "Offset Level 4", Order = 13, GroupName = "Indicator: SuperJumpBoost")]
        public double SJ_OffsetLevel4
        {
            get; set;
        }

        [NinjaScriptProperty]
        [Range (0.01, double.MaxValue)]
        [Display (Name = "Offset Base", Order = 14, GroupName = "Indicator: SuperJumpBoost")]
        public double SJ_OffsetBase
        {
            get; set;
        }

        [NinjaScriptProperty]
        [Range (1, int.MaxValue)]
        [Display (Name = "Reference Price Period", Order = 20, GroupName = "Indicator: SuperJumpBoost")]
        public int SJ_ReferencePricePeriod
        {
            get; set;
        }

        [NinjaScriptProperty]
        [Range (0, int.MaxValue)]
        [Display (Name = "Line Levels Offset", Order = 30, GroupName = "Indicator: SuperJumpBoost")]
        public int SJ_LineLevelsOffset
        {
            get; set;
        }

        [NinjaScriptProperty]
        [Range (1, int.MaxValue)]
        [Display (Name = "Extreme Neighborhood", Order = 40, GroupName = "Indicator: SuperJumpBoost")]
        public int SJ_ExtremeNeighborhood
        {
            get; set;
        }

        [NinjaScriptProperty]
        [Range (1, 100)]
        [Display (Name = "Signal Close Threshold (%)", Order = 50, GroupName = "Indicator: SuperJumpBoost")]
        public int SJ_SignalCloseThreshold
        {
            get; set;
        }

        [NinjaScriptProperty]
        [Range (1, int.MaxValue)]
        [Display (Name = "Signal Quantity Per Zone", Order = 60, GroupName = "Indicator: SuperJumpBoost")]
        public int SJ_SignalQuantityPerZone
        {
            get; set;
        }

        [NinjaScriptProperty]
        [Range (1, int.MaxValue)]
        [Display (Name = "Signal Split (Bars)", Order = 70, GroupName = "Indicator: SuperJumpBoost")]
        public int SJ_SignalSplit
        {
            get; set;
        }

        // ==================== Display ====================
        [NinjaScriptProperty]
        [Display (Name = "Show Dashboard", Order = 0, GroupName = "Display",
            Description = "Show the SharpDX info panel (PnL/session/status). Turn off for stability mode — disables all dashboard rendering and dispatcher invalidate work. Position can be set via Dashboard Position; setting Position = Hidden has the same effect.")]
        public bool ShowDashboard
        {
            get; set;
        }

        [NinjaScriptProperty]
        [Display (Name = "Dashboard Position", Order = 1, GroupName = "Display",
            Description = "Where to anchor the SharpDX info panel on the chart. Hidden = same as Show Dashboard = false.")]
        public HudCorner DashboardPosition
        {
            get; set;
        }

        [NinjaScriptProperty]
        [Display (Name = "Dashboard Size", Order = 2, GroupName = "Display",
            Description = "Tiny / Small / Normal / Large / Huge. Drives both font size AND box width — the box tightens to fit the actual text at each preset, no empty real-estate. Changes apply on the next frame (free to tweak at runtime).")]
        public GodZillaHudSize DashboardSize
        {
            get; set;
        }

        [NinjaScriptProperty]
        [Display (Name = "Show Control Panel", Order = 3, GroupName = "Display",
            Description = "Show the WPF arm/auto-arm/close-all button panel. Disable if you only need automated entries and don't want manual override controls.")]
        public bool ShowControlPanel
        {
            get; set;
        }

        [NinjaScriptProperty]
        [Display (Name = "Control Panel Position", Order = 4, GroupName = "Display",
            Description = "Where to anchor the button panel on the chart. Position changes apply on next strategy enable. Hidden = same as Show Control Panel = false.")]
        public HudCorner ControlPanelPosition
        {
            get; set;
        }

        // -------------------- Display: KingOrderBlock --------------------
        [NinjaScriptProperty]
        [Display (Name = "KingOrderBlock: Show Indicator", Order = 5, GroupName = "Display")]
        [RefreshProperties (RefreshProperties.All)]
        public bool ShowKOIndicator
        {
            get; set;
        }

        [NinjaScriptProperty]
        [Display (Name = "KingOrderBlock: Show Signal Arrows", Order = 6, GroupName = "Display")]
        [RefreshProperties (RefreshProperties.All)]
        public bool ShowKOSignalArrows
        {
            get; set;
        }

        [NinjaScriptProperty]
        [Display (Name = "KingOrderBlock: Show Signal Arrow Label", Order = 7, GroupName = "Display")]
        [RefreshProperties (RefreshProperties.All)]
        public bool ShowKOSignalArrowLabels
        {
            get; set;
        }

        [NinjaScriptProperty]
        [Display (Name = "KingOrderBlock: Signal Arrow Text", Order = 8, GroupName = "Display")]
        public string KOSignalArrowText
        {
            get; set;
        }

        [XmlIgnore]
        [Display (Name = "KingOrderBlock: Indicator Color", Order = 9, GroupName = "Display")]
        public Brush KO_Brush
        {
            get; set;
        }

        [Browsable (false)]
        public string KO_BrushSerialize
        {
            get
            {
                return Serialize.BrushToString (KO_Brush);
            }
            set
            {
                KO_Brush = Serialize.StringToBrush (value);
            }
        }

        [XmlIgnore]
        [Display (Name = "KingOrderBlock: Signal Arrow Color", Order = 10, GroupName = "Display")]
        public Brush KOSignalArrowBrush
        {
            get; set;
        }

        [Browsable (false)]
        public string KOSignalArrowBrushSerialize
        {
            get
            {
                return Serialize.BrushToString (KOSignalArrowBrush);
            }
            set
            {
                KOSignalArrowBrush = Serialize.StringToBrush (value);
            }
        }

        // -------------------- Display: PA --------------------
        [NinjaScriptProperty]
        [Display (Name = "PA: Show Indicator", Order = 11, GroupName = "Display")]
        [RefreshProperties (RefreshProperties.All)]
        public bool ShowPAIndicator
        {
            get; set;
        }

        [NinjaScriptProperty]
        [Display (Name = "PA: Show Signal Arrows", Order = 12, GroupName = "Display")]
        [RefreshProperties (RefreshProperties.All)]
        public bool ShowPASignalArrows
        {
            get; set;
        }

        [NinjaScriptProperty]
        [Display (Name = "PA: Show Signal Arrow Label", Order = 13, GroupName = "Display")]
        [RefreshProperties (RefreshProperties.All)]
        public bool ShowPASignalArrowLabels
        {
            get; set;
        }

        [NinjaScriptProperty]
        [Display (Name = "PA: Signal Arrow Text", Order = 14, GroupName = "Display")]
        public string PASignalArrowText
        {
            get; set;
        }

        [XmlIgnore]
        [Display (Name = "PA: Indicator Color", Order = 15, GroupName = "Display")]
        public Brush PA_Brush
        {
            get; set;
        }

        [Browsable (false)]
        public string PA_BrushSerialize
        {
            get
            {
                return Serialize.BrushToString (PA_Brush);
            }
            set
            {
                PA_Brush = Serialize.StringToBrush (value);
            }
        }

        [XmlIgnore]
        [Display (Name = "PA: Signal Arrow Color", Order = 16, GroupName = "Display")]
        public Brush PASignalArrowBrush
        {
            get; set;
        }

        [Browsable (false)]
        public string PASignalArrowBrushSerialize
        {
            get
            {
                return Serialize.BrushToString (PASignalArrowBrush);
            }
            set
            {
                PASignalArrowBrush = Serialize.StringToBrush (value);
            }
        }

        // -------------------- Display: ThunderZilla --------------------
        [NinjaScriptProperty]
        [Display (Name = "ThunderZilla: Show Indicator", Order = 17, GroupName = "Display")]
        [RefreshProperties (RefreshProperties.All)]
        public bool ShowTHIndicator
        {
            get; set;
        }

        [NinjaScriptProperty]
        [Display (Name = "ThunderZilla: Show Signal Arrows", Order = 18, GroupName = "Display")]
        [RefreshProperties (RefreshProperties.All)]
        public bool ShowTHSignalArrows
        {
            get; set;
        }

        [NinjaScriptProperty]
        [Display (Name = "ThunderZilla: Show Signal Arrow Label", Order = 19, GroupName = "Display")]
        [RefreshProperties (RefreshProperties.All)]
        public bool ShowTHSignalArrowLabels
        {
            get; set;
        }

        [NinjaScriptProperty]
        [Display (Name = "ThunderZilla: Signal Arrow Text", Order = 20, GroupName = "Display")]
        public string THSignalArrowText
        {
            get; set;
        }

        [XmlIgnore]
        [Display (Name = "ThunderZilla: Indicator Color", Order = 21, GroupName = "Display")]
        public Brush TH_Brush
        {
            get; set;
        }

        [Browsable (false)]
        public string TH_BrushSerialize
        {
            get
            {
                return Serialize.BrushToString (TH_Brush);
            }
            set
            {
                TH_Brush = Serialize.StringToBrush (value);
            }
        }

        [XmlIgnore]
        [Display (Name = "ThunderZilla: Signal Arrow Color", Order = 22, GroupName = "Display")]
        public Brush THSignalArrowBrush
        {
            get; set;
        }

        [Browsable (false)]
        public string THSignalArrowBrushSerialize
        {
            get
            {
                return Serialize.BrushToString (THSignalArrowBrush);
            }
            set
            {
                THSignalArrowBrush = Serialize.StringToBrush (value);
            }
        }

        // -------------------- Display: SuperJumpBoost --------------------
        [NinjaScriptProperty]
        [Display (Name = "SuperJumpBoost: Show Indicator", Order = 23, GroupName = "Display")]
        [RefreshProperties (RefreshProperties.All)]
        public bool ShowSJIndicator
        {
            get; set;
        }

        [NinjaScriptProperty]
        [Display (Name = "SuperJumpBoost: Show Signal Arrows", Order = 24, GroupName = "Display")]
        [RefreshProperties (RefreshProperties.All)]
        public bool ShowSJSignalArrows
        {
            get; set;
        }

        [NinjaScriptProperty]
        [Display (Name = "SuperJumpBoost: Show Signal Arrow Label", Order = 25, GroupName = "Display")]
        [RefreshProperties (RefreshProperties.All)]
        public bool ShowSJSignalArrowLabels
        {
            get; set;
        }

        [NinjaScriptProperty]
        [Display (Name = "SuperJumpBoost: Signal Arrow Text", Order = 26, GroupName = "Display")]
        public string SJSignalArrowText
        {
            get; set;
        }

        [XmlIgnore]
        [Display (Name = "SuperJumpBoost: Indicator Color", Order = 27, GroupName = "Display")]
        public Brush SJ_Brush
        {
            get; set;
        }

        [Browsable (false)]
        public string SJ_BrushSerialize
        {
            get
            {
                return Serialize.BrushToString (SJ_Brush);
            }
            set
            {
                SJ_Brush = Serialize.StringToBrush (value);
            }
        }

        [XmlIgnore]
        [Display (Name = "SuperJumpBoost: Signal Arrow Color", Order = 28, GroupName = "Display")]
        public Brush SJSignalArrowBrush
        {
            get; set;
        }

        [Browsable (false)]
        public string SJSignalArrowBrushSerialize
        {
            get
            {
                return Serialize.BrushToString (SJSignalArrowBrush);
            }
            set
            {
                SJSignalArrowBrush = Serialize.StringToBrush (value);
            }
        }

        // -------------------- Display: SumoPullback --------------------
        [NinjaScriptProperty]
        [Display (Name = "SumoPullback: Show Indicator", Order = 29, GroupName = "Display")]
        [RefreshProperties (RefreshProperties.All)]
        public bool ShowSUIndicator
        {
            get; set;
        }

        [NinjaScriptProperty]
        [Display (Name = "SumoPullback: Show Signal Arrows", Order = 30, GroupName = "Display")]
        [RefreshProperties (RefreshProperties.All)]
        public bool ShowSUSignalArrows
        {
            get; set;
        }

        [NinjaScriptProperty]
        [Display (Name = "SumoPullback: Show Signal Arrow Label", Order = 31, GroupName = "Display")]
        [RefreshProperties (RefreshProperties.All)]
        public bool ShowSUSignalArrowLabels
        {
            get; set;
        }

        [NinjaScriptProperty]
        [Display (Name = "SumoPullback: Signal Arrow Text", Order = 32, GroupName = "Display")]
        public string SUSignalArrowText
        {
            get; set;
        }

        [XmlIgnore]
        [Display (Name = "SumoPullback: Indicator Color", Order = 33, GroupName = "Display")]
        public Brush SU_Brush
        {
            get; set;
        }

        [Browsable (false)]
        public string SU_BrushSerialize
        {
            get
            {
                return Serialize.BrushToString (SU_Brush);
            }
            set
            {
                SU_Brush = Serialize.StringToBrush (value);
            }
        }

        [XmlIgnore]
        [Display (Name = "SumoPullback: Signal Arrow Color", Order = 34, GroupName = "Display")]
        public Brush SUSignalArrowBrush
        {
            get; set;
        }

        [Browsable (false)]
        public string SUSignalArrowBrushSerialize
        {
            get
            {
                return Serialize.BrushToString (SUSignalArrowBrush);
            }
            set
            {
                SUSignalArrowBrush = Serialize.StringToBrush (value);
            }
        }

        // -------------------- Display: Group Trigger --------------------
        [NinjaScriptProperty]
        [Display (Name = "Group: Show Trigger Arrows", Order = 35, GroupName = "Display")]
        [RefreshProperties (RefreshProperties.All)]
        public bool ShowGroupTriggerArrows
        {
            get; set;
        }

        [NinjaScriptProperty]
        [Display (Name = "Group: Show Trigger Arrow Label", Order = 36, GroupName = "Display")]
        [RefreshProperties (RefreshProperties.All)]
        public bool ShowGroupTriggerArrowLabel
        {
            get; set;
        }

        [NinjaScriptProperty]
        [Display (Name = "Group: Trigger Arrow Text", Order = 37, GroupName = "Display")]
        public string GroupTriggerArrowText
        {
            get; set;
        }

        [XmlIgnore]
        [Display (Name = "Group: Trigger Arrow Color", Order = 38, GroupName = "Display")]
        public Brush GroupTriggerBrush
        {
            get; set;
        }

        [Browsable (false)]
        public string GroupTriggerBrushSerialize
        {
            get
            {
                return Serialize.BrushToString (GroupTriggerBrush);
            }
            set
            {
                GroupTriggerBrush = Serialize.StringToBrush (value);
            }
        }

        // -------------------- Display: Global Arrow Settings --------------------
        [NinjaScriptProperty]
        [Range (0, int.MaxValue)]
        [Display (Name = "Arrow Offset (Ticks)", Order = 39, GroupName = "Display")]
        public int ArrowOffset
        {
            get; set;
        }

        [NinjaScriptProperty]
        [Range (1, int.MaxValue)]
        [Display (Name = "Signal Arrow Text Offset From Arrow (Ticks)", Order = 40, GroupName = "Display")]
        public int SignalArrowTextOffsetTicks
        {
            get; set;
        }

        [NinjaScriptProperty]
        [Display (Name = "Enable Group Trigger BackBrush", Order = 41, GroupName = "Display")]
        [RefreshProperties (RefreshProperties.All)]
        public bool EnableGroupTriggerBackBrush
        {
            get; set;
        }

        [XmlIgnore]
        [Display (Name = "Group Trigger BackBrush", Order = 42, GroupName = "Display")]
        public Brush GroupTriggerBackBrush
        {
            get; set;
        }

        [Browsable (false)]
        public string GroupTriggerBackBrushSerialize
        {
            get
            {
                return Serialize.BrushToString (GroupTriggerBackBrush);
            }
            set
            {
                GroupTriggerBackBrush = Serialize.StringToBrush (value);
            }
        }

        // ==================== Trade Markers - ATMPlotMarkers integration ====================
        [NinjaScriptProperty]
        [Display (Name = "Show Entry/Exit Markers", Order = 43, GroupName = "Display", Description = "ATM mode only. Draw lines from entry to exit on every closed or scaled-out trade leg.")]
        [RefreshProperties (RefreshProperties.All)]
        public bool ShowEntryExitMarkers
        {
            get; set;
        }

        [NinjaScriptProperty]
        [Range (1, int.MaxValue)]
        [Display (Name = "Line Width", Order = 44, GroupName = "Display", Description = "Width of the entry-to-exit line.")]
        public int EntryExitLineWidth
        {
            get; set;
        }

        [XmlIgnore]
        [Display (Name = "Long Color", Order = 45, GroupName = "Display", Description = "Line and label color for long ATM trades.")]
        public Brush EntryExitLongColor
        {
            get; set;
        }

        [Browsable (false)]
        public string EntryExitLongColorSerialize
        {
            get
            {
                return Serialize.BrushToString (EntryExitLongColor);
            }
            set
            {
                EntryExitLongColor = Serialize.StringToBrush (value);
            }
        }

        [XmlIgnore]
        [Display (Name = "Short Color", Order = 46, GroupName = "Display", Description = "Line and label color for short ATM trades.")]
        public Brush EntryExitShortColor
        {
            get; set;
        }

        [Browsable (false)]
        public string EntryExitShortColorSerialize
        {
            get
            {
                return Serialize.BrushToString (EntryExitShortColor);
            }
            set
            {
                EntryExitShortColor = Serialize.StringToBrush (value);
            }
        }

        [NinjaScriptProperty]
        [Display (Name = "Show Text Labels", Order = 47, GroupName = "Display", Description = "Show entry and exit price labels next to the line endpoints.")]
        [RefreshProperties (RefreshProperties.All)]
        public bool ShowEntryExitLabels
        {
            get; set;
        }

        [NinjaScriptProperty]
        [Range (6, 24)]
        [Display (Name = "Text Size", Order = 48, GroupName = "Display", Description = "Font size of the entry/exit price labels.")]
        public int EntryExitTextSize
        {
            get; set;
        }

        // ==================== Logging ====================
        [NinjaScriptProperty]
        [Display (Name = "Log Trades", Order = 0, GroupName = "Logging", Description = "Write a CSV trade log to the NinjaTrader user data folder.")]
        public bool LogEnabled
        {
            get; set;
        }

        [NinjaScriptProperty]
        [Display (Name = "Enable Debug", Order = 1, GroupName = "Logging", Description = "Print diagnostic messages to the NinjaTrader output window.")]
        public bool EnableDebug
        {
            get; set;
        }
        #endregion


        #region AtmStrategySelector converter
        public class FriendlyAtmConverter : TypeConverter
        {
            public override StandardValuesCollection GetStandardValues (ITypeDescriptorContext context)
            {
                List<string> values = new List<string>();
                string atmDir = System.IO.Path.Combine(NinjaTrader.Core.Globals.UserDataDir, "templates", "AtmStrategy");

                if (System.IO.Directory.Exists (atmDir))
                {
                    string[] files = System.IO.Directory.GetFiles(atmDir, "*.xml");
                    foreach (string atm in files)
                    {
                        string atmName = System.IO.Path.GetFileNameWithoutExtension(atm);
                        values.Add (atmName);
                        NinjaTrader.Code.Output.Process (atmName, PrintTo.OutputTab2);
                    }
                }

                return new StandardValuesCollection (values);
            }

            public override object ConvertFrom (ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value)
            {
                return value?.ToString () ?? string.Empty;
            }

            public override object ConvertTo (ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType)
            {
                return value;
            }

            public override bool CanConvertFrom (ITypeDescriptorContext context, Type sourceType)
            {
                return true;
            }

            public override bool CanConvertTo (ITypeDescriptorContext context, Type destinationType)
            {
                return true;
            }

            public override bool GetStandardValuesExclusive (ITypeDescriptorContext context)
            {
                return true;
            }

            public override bool GetStandardValuesSupported (ITypeDescriptorContext context)
            {
                return true;
            }
        }
        #endregion
    }
}