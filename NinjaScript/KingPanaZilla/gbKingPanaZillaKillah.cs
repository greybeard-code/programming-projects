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
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
#endregion

//This namespace holds Strategies in this folder and is required. Do not change it.
namespace NinjaTrader.NinjaScript.Strategies.Playr101
{
    #region GUI Categories
    [CategoryOrder ("Strategy Information", 0)]
    [CategoryOrder ("ATM Parameters", 1)]
    [CategoryOrder ("Signals", 2)]
    [CategoryOrder ("Risk Management", 3)]
    [CategoryOrder ("Session Parameters", 4)]
    #endregion

    public class gbKingPanaZillaKillah : Strategy, ICustomTypeDescriptor
    {
        public override string DisplayName
        {
            get
            {
                return Name;
            }
        }

        #region Variables
        // Drawing
        SimpleFont title = new SimpleFont("Agency Fb", 16) { Size = 20, Bold = true };

        // ATM
        private string atmStrategyId = string.Empty;
        private string orderId = string.Empty;
        private bool isAtmStrategyCreated = false;

        // Internal tracker
        private double dailyRealizedPnL = 0.0;
        private double dailyUnrealizedPnL = 0.0;
        private double totalRealizedPnL = 0.0;
        private double sessionStartTotalRealizedPnL = 0.0;
        private double totalRunningPnL = 0.0;
        private double lastAtmRealizedPnL = 0.0;
        private DateTime lastPnlSessionDate = Core.Globals.MinDate;
        private bool dailyLimitHit = false;
        private string dailyPnlStatusMessage = string.Empty;

        // Naked-position watchdog
        private DateTime lastNakedCheck = Core.Globals.MinDate;
        private const int NakedCheckIntervalSeconds = 3;

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

        private SignalTradeStats panaZilliaStats = new SignalTradeStats();
        private SignalTradeStats kingZillaStats = new SignalTradeStats();
        private SignalTradeStats kingPanaStats = new SignalTradeStats();

        private bool activeTradeUsesPanaZillia = false;
        private bool activeTradeUsesKingZilla = false;
        private bool activeTradeUsesKingPana = false;
        private MarketPosition activeTradeDirection = MarketPosition.Flat;
        private string lastTradeClosedSummary = string.Empty;

        // Indicator
        private gbKingPanaZilla _gbIndicator;
        #endregion

        protected override void OnStateChange ()
        {
            if (State == State.SetDefaults)
            {
                Description = "Strategy utilizing gbKingPanaZilla signals.";
                Name = "gbKingPanaZillaKillah";
                StrategyName = Name;
                StrategyVersion = "1.4.1";
                Author = "Playr101";
                Credits = "GreyBeard";

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

                AtmStrategy = string.Empty;

                EnableSignalTracking = false;
                UsePanaZilliaSignals = true;
                UseKingZillaSignals = true;
                UseKingPanaSignals = true;

                LogEnabled = false;

                // Risk Defaults
                UseUnrealizedPnl = true;
                UseDailyProfitTarget = false;
                DailyProfitTarget = 500;
                UseDailyLossLimit = true;
                DailyLossLimit = 200;

                EnableTF1 = true;
                StartTime1 = DateTime.Parse ("09:30", System.Globalization.CultureInfo.InvariantCulture);
                EndTime1 = DateTime.Parse ("12:00", System.Globalization.CultureInfo.InvariantCulture);
                FlattenTF1 = true;

                EnableTF2 = true;
                StartTime2 = DateTime.Parse ("13:00", System.Globalization.CultureInfo.InvariantCulture);
                EndTime2 = DateTime.Parse ("15:30", System.Globalization.CultureInfo.InvariantCulture);
                FlattenTF2 = true;
            }
            else if (State == State.Configure)
            {
                ClearOutputWindow ();
                AddDataSeries (BarsPeriodType.Tick, 1);
            }
            else if (State == State.DataLoaded)
            {
                if (Instrument != null && Instrument.MasterInstrument != null)
                    Draw.TextFixed (this, "Name", Name + "\n" + "Instrument -- " + Instrument.MasterInstrument.Name,
                        TextPosition.TopLeft, Brushes.DeepSkyBlue, title, Brushes.Transparent, Brushes.Transparent, 0);

                _gbIndicator = gbKingPanaZilla ();
                AddChartIndicator (_gbIndicator);
                _gbIndicator.Name = "";

                if (LogEnabled)
                {
                    string logPath = Path.Combine(
                        NinjaTrader.Core.Globals.UserDataDir,
                        "gbKPZKillah_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".csv");
                    _logWriter = new StreamWriter (logPath, append: false, encoding: Encoding.UTF8);
                    _logWriter.WriteLine ("OpenTime,Instrument,OpenPrice,Qty,CloseTime,Trigger,Direction,AtmStrategy,RealizedPnL");
                    _logWriter.Flush ();
                }

                DrawPnlDisplay ();
            }
            else if (State == State.Terminated)
            {
                if (_logWriter != null)
                {
                    _logWriter.Flush ();
                    _logWriter.Dispose ();
                    _logWriter = null;
                }
            }
            else if (State == State.Realtime)
            {
                sessionStartTotalRealizedPnL = totalRealizedPnL;
                lastPnlSessionDate = Core.Globals.MinDate;
                lastAtmRealizedPnL = 0.0;
                dailyRealizedPnL = 0.0;
                dailyUnrealizedPnL = 0.0;
                totalRunningPnL = totalRealizedPnL;
                dailyLimitHit = false;
                dailyPnlStatusMessage = string.Empty;
                lastTradeClosedSummary = string.Empty;
                _atmPositionConfirmed = false;
                Print ($"{Name} entered realtime. ATM mode active.");
            }
        }

        protected override void OnBarUpdate ()
        {
            if (_gbIndicator == null || State == State.Historical)
                return;

            if (BarsInProgress == 1)
            {
                if (CurrentBars[1] < 1)
                    return;

                UpdateDailyPnlOnTickSeries ();
                return;
            }

            if (BarsInProgress != 0)
                return;

            if (CurrentBars[0] < BarsRequiredToTrade)
                return;

            // Flatten logic at end of TimeFrames
            int currentTime = ToTime(Time[0]);
            CheckFlattenTimeframes (currentTime);

            if (dailyLimitHit)
                return;

            bool isWithinTradingTime = CheckTradingTimeframes(currentTime);

            if (Position.MarketPosition != MarketPosition.Flat)
                return; // Do not enter new trades while in a position

            if (!isWithinTradingTime || State != State.Realtime)
                return;

            // Entry Logic
            double pz = _gbIndicator.PanaZillia_Trade[0];
            double kz = _gbIndicator.KingZilla_Trade[0];
            double kp = _gbIndicator.KingPana_Trade[0];

            bool panaLong = pz == 1 && UsePanaZilliaSignals;
            bool kingZillaLong = kz == 1 && UseKingZillaSignals;
            bool kingPanaLong = kp == 1 && UseKingPanaSignals;
            bool panaShort = pz == -1 && UsePanaZilliaSignals;
            bool kingZillaShort = kz == -1 && UseKingZillaSignals;
            bool kingPanaShort = kp == -1 && UseKingPanaSignals;

            bool goLong = panaLong || kingZillaLong || kingPanaLong;
            bool goShort = panaShort || kingZillaShort || kingPanaShort;

            // Submit ATM entry only when both ids are reset
            if (orderId.Length == 0 && atmStrategyId.Length == 0 && goLong)
            {
                string trigger = string.Join("+", new[] {
                    panaLong ? "PZ" : null,
                    kingZillaLong ? "KZ" : null,
                    kingPanaLong ? "KP" : null,
                }.Where(s => s != null));

                isAtmStrategyCreated = false;
                SetActiveTradeSignalSources (panaLong, kingZillaLong, kingPanaLong, MarketPosition.Long);
                atmStrategyId = GetAtmStrategyUniqueId ();
                orderId = GetAtmStrategyUniqueId ();
                _tradeMap[atmStrategyId] = new TradeRecord
                {
                    Trigger = trigger,
                    Direction = "Long",
                    OpenTime = Time[0],
                    Instrument = FormatInstrumentName (),
                    OpenPrice = 0.0,
                    Qty = 0,
                    AtmStrategyName = AtmStrategy
                };
                AtmStrategyCreate (OrderAction.Buy, OrderType.Market, 0, 0, TimeInForce.Day, orderId, AtmStrategy, atmStrategyId, (atmCallbackErrorCode, atmCallBackId) =>
                {
                    if (atmCallbackErrorCode == ErrorCode.NoError && atmCallBackId == atmStrategyId)
                        isAtmStrategyCreated = true;
                });
            }
            else if (orderId.Length == 0 && atmStrategyId.Length == 0 && goShort)
            {
                string trigger = string.Join("+", new[] {
                    panaShort ? "PZ" : null,
                    kingZillaShort ? "KZ" : null,
                    kingPanaShort ? "KP" : null,
                }.Where(s => s != null));

                isAtmStrategyCreated = false;
                SetActiveTradeSignalSources (panaShort, kingZillaShort, kingPanaShort, MarketPosition.Short);
                atmStrategyId = GetAtmStrategyUniqueId ();
                orderId = GetAtmStrategyUniqueId ();
                _tradeMap[atmStrategyId] = new TradeRecord
                {
                    Trigger = trigger,
                    Direction = "Short",
                    OpenTime = Time[0],
                    Instrument = FormatInstrumentName (),
                    OpenPrice = 0.0,
                    Qty = 0,
                    AtmStrategyName = AtmStrategy
                };
                AtmStrategyCreate (OrderAction.SellShort, OrderType.Market, 0, 0, TimeInForce.Day, orderId, AtmStrategy, atmStrategyId, (atmCallbackErrorCode, atmCallBackId) =>
                {
                    if (atmCallbackErrorCode == ErrorCode.NoError && atmCallBackId == atmStrategyId)
                        isAtmStrategyCreated = true;
                });
            }

            if (!isAtmStrategyCreated)
                return;

            if (orderId.Length > 0)
            {
                string[] status = GetAtmStrategyEntryOrderStatus(orderId);

                if (status != null && status.Length > 0)
                {
                    Print ("The entry order average fill price is: " + status[0]);
                    Print ("The entry order filled amount is: " + status[1]);
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

            if (atmStrategyId.Length > 0)
            {
                Print ("The current ATM Strategy market position is: " + GetAtmStrategyMarketPosition (atmStrategyId));
                Print ("The current ATM Strategy position quantity is: " + GetAtmStrategyPositionQuantity (atmStrategyId));
                Print ("The current ATM Strategy average price is: " + GetAtmStrategyPositionAveragePrice (atmStrategyId));
                Print ("The current ATM Strategy Unrealized PnL is: " + GetAtmStrategyUnrealizedProfitLoss (atmStrategyId));
            }
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
            if (State != State.Realtime)
                return;

            DateTime tickTime = Times[1][0];

            if (lastPnlSessionDate == Core.Globals.MinDate || Bars.IsFirstBarOfSession || tickTime.Date != lastPnlSessionDate.Date)
            {
                sessionStartTotalRealizedPnL = totalRealizedPnL;
                lastAtmRealizedPnL = 0.0;
                dailyRealizedPnL = 0.0;
                dailyUnrealizedPnL = 0.0;
                totalRunningPnL = totalRealizedPnL;
                dailyLimitHit = false;
                dailyPnlStatusMessage = string.Empty;
                lastPnlSessionDate = tickTime.Date;
            }

            if (orderId.Length > 0)
            {
                string[] status = GetAtmStrategyEntryOrderStatus (orderId);

                if (status != null && status.Length >= 3)
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
                // If status is null the order may not be registered yet — leave orderId set
                // and let the position check below detect the fill via GetAtmStrategyMarketPosition.
            }

            if (atmStrategyId.Length > 0)
            {
                Cbi.MarketPosition atmPos = GetAtmStrategyMarketPosition (atmStrategyId);

                if (atmPos != Cbi.MarketPosition.Flat)
                {
                    // Position is live — entry filled regardless of orderId / callback state.
                    _atmPositionConfirmed = true;
                    isAtmStrategyCreated  = true;
                    orderId = string.Empty;

                    if (_tradeMap.TryGetValue (atmStrategyId, out TradeRecord rec) && rec.OpenPrice == 0.0)
                    {
                        double avgPrice = GetAtmStrategyPositionAveragePrice (atmStrategyId);
                        if (avgPrice > 0)
                        {
                            rec.OpenPrice = Instrument.MasterInstrument.RoundToTickSize (avgPrice);
                            rec.Qty       = (int)GetAtmStrategyPositionQuantity (atmStrategyId);
                        }
                    }

                    dailyUnrealizedPnL = Instrument.MasterInstrument.RoundToTickSize (GetAtmStrategyUnrealizedProfitLoss (atmStrategyId));
                }
                else if (_atmPositionConfirmed)
                {
                    // Was open, now flat — trade closed normally.
                    double atmRealized = GetAtmStrategyRealizedProfitLoss (atmStrategyId);

                    if (!double.IsNaN (atmRealized))
                        lastAtmRealizedPnL = Instrument.MasterInstrument.RoundToTickSize (atmRealized);
                    else
                        lastAtmRealizedPnL = 0.0;

                    totalRealizedPnL += lastAtmRealizedPnL;
                    UpdateSignalTrackingOnTradeClose (lastAtmRealizedPnL);
                    UpdateLastTradeClosedSummary (lastAtmRealizedPnL);
                    PrintSignalTrackingOnTradeClose (lastAtmRealizedPnL);

                    if (_logWriter != null && _tradeMap.TryGetValue (atmStrategyId, out TradeRecord tr))
                    {
                        _logWriter.WriteLine ($"{tr.OpenTime:yyyy-MM-dd HH:mm:ss},{tr.Instrument},{tr.OpenPrice:F2},{tr.Qty},{tickTime:yyyy-MM-dd HH:mm:ss},{tr.Trigger},{tr.Direction},{tr.AtmStrategyName},{lastAtmRealizedPnL:F2}");
                        _logWriter.Flush ();
                    }

                    _tradeMap.Remove (atmStrategyId);
                    ClearActiveTradeSignalSources ();
                    atmStrategyId         = string.Empty;
                    orderId               = string.Empty;
                    isAtmStrategyCreated  = false;
                    _atmPositionConfirmed = false;
                    dailyUnrealizedPnL    = 0.0;
                }
                else
                {
                    // Position not yet confirmed open and shows flat — ATM still registering.
                    // Suppress ATM method calls to avoid error spam during registration window.
                    dailyUnrealizedPnL = 0.0;
                }
            }
            else
                dailyUnrealizedPnL = 0.0;

            dailyRealizedPnL = totalRealizedPnL - sessionStartTotalRealizedPnL;
            totalRunningPnL = totalRealizedPnL + (UseUnrealizedPnl ? dailyUnrealizedPnL : 0.0);

            double dailyPnlToCheck = dailyRealizedPnL + (UseUnrealizedPnl ? dailyUnrealizedPnL : 0.0);

            if (!dailyLimitHit)
            {
                if (UseDailyProfitTarget && dailyPnlToCheck >= DailyProfitTarget)
                {
                    dailyLimitHit = true;
                    dailyPnlStatusMessage = $"DAILY PROFIT TARGET HIT: {dailyPnlToCheck:C}";
                    FlattenAll ();
                }
                else if (UseDailyLossLimit && dailyPnlToCheck <= -DailyLossLimit)
                {
                    dailyLimitHit = true;
                    dailyPnlStatusMessage = $"DAILY LOSS LIMIT HIT: {dailyPnlToCheck:C}";
                    FlattenAll ();
                }
            }

            CheckForNakedPositions (tickTime);
            DrawPnlDisplay ();
        }

        private void DrawPnlDisplay ()
        {
            int displayTime = 0;

            if (BarsArray.Length > 1 && CurrentBars.Length > 1 && CurrentBars[1] >= 0)
                displayTime = ToTime (Times[1][0]);
            else if (CurrentBar >= 0)
                displayTime = ToTime (Time[0]);

            bool timeFilterEnabled = EnableTF1 || EnableTF2;
            bool inSession = !timeFilterEnabled || CheckTradingTimeframes(displayTime);

            string sessionText = timeFilterEnabled
                ? (inSession ? "Trading: IN SESSION" : "Trading: OUT OF SESSION")
                : "Trading: IN SESSION (Time Filter Disabled)";

            string targetStr = UseDailyProfitTarget ? "$" + DailyProfitTarget.ToString("F0") : "~";
            string lossStr = UseDailyLossLimit ? "-$" + DailyLossLimit.ToString("F0") : "~";
            double currentDisplayUnrealized = UseUnrealizedPnl ? dailyUnrealizedPnL : 0.0;

            string pnlLine1 = dailyLimitHit && !string.IsNullOrEmpty(dailyPnlStatusMessage)
                ? dailyPnlStatusMessage
                : "Total: $" + totalRunningPnL.ToString("F2")
                + "  |  Closed: $" + dailyRealizedPnL.ToString("F2")
                + (UseUnrealizedPnl ? "  |  Open: $" + currentDisplayUnrealized.ToString("F2") : string.Empty);

            string pnlLine2 = "Target: " + targetStr + "  |  Max Loss: " + lossStr;

            List<string> lines = new List<string>
            {
                "--- " + Name + " v" + StrategyVersion + " ---",
                sessionText,
                pnlLine1,
                pnlLine2
            };

            if (EnableSignalTracking)
            {
                if (!string.IsNullOrEmpty (lastTradeClosedSummary))
                    lines.Add (lastTradeClosedSummary);

                lines.AddRange (BuildSignalTrackingDisplayLines ());
            }

            string labelText = string.Join("\n", lines);

            Brush textBrush = dailyLimitHit
                ? (dailyPnlStatusMessage.Contains("PROFIT") ? Brushes.Lime : Brushes.Red)
                : ((dailyRealizedPnL + currentDisplayUnrealized) >= 0 ? Brushes.Lime : Brushes.Red);

            Draw.TextFixed (this, "PnlDisplay", labelText,
                TextPosition.BottomRight, textBrush, new SimpleFont ("Arial", 11),
                Brushes.Transparent, Brushes.Black, 80);

            if (dailyLimitHit && !string.IsNullOrEmpty (dailyPnlStatusMessage))
            {
                Brush statusBrush = dailyPnlStatusMessage.Contains("PROFIT") ? Brushes.Lime : Brushes.Red;

                Draw.TextFixed (this, "PnlStatus", dailyPnlStatusMessage,
                    TextPosition.Center, statusBrush, title, Brushes.Black, Brushes.Black, 0);
            }
            else
            {
                RemoveDrawObject ("PnlStatus");
            }
        }

        private void SetActiveTradeSignalSources (bool usePanaZillia, bool useKingZilla, bool useKingPana, MarketPosition direction)
        {
            activeTradeUsesPanaZillia = usePanaZillia;
            activeTradeUsesKingZilla = useKingZilla;
            activeTradeUsesKingPana = useKingPana;
            activeTradeDirection = direction;
        }

        private void ClearActiveTradeSignalSources ()
        {
            activeTradeUsesPanaZillia = false;
            activeTradeUsesKingZilla = false;
            activeTradeUsesKingPana = false;
            activeTradeDirection = MarketPosition.Flat;
        }

        private void UpdateSignalTrackingOnTradeClose (double tradePnl)
        {
            if (!EnableSignalTracking)
                return;

            bool isWinner = tradePnl > 0;
            bool isLoser = tradePnl < 0;

            if (activeTradeUsesPanaZillia)
                IncrementSignalStats (panaZilliaStats, activeTradeDirection, isWinner, isLoser);

            if (activeTradeUsesKingZilla)
                IncrementSignalStats (kingZillaStats, activeTradeDirection, isWinner, isLoser);

            if (activeTradeUsesKingPana)
                IncrementSignalStats (kingPanaStats, activeTradeDirection, isWinner, isLoser);
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
            Print ($"Trade Closed | Last ATM P/L: {tradePnl:C} | Direction: {activeTradeDirection} | Signals: {tradeSignalText}{statsText}");
        }

        private string BuildActiveSignalListForPrint ()
        {
            List<string> activeSignals = new List<string>();

            if (activeTradeUsesPanaZillia)
                activeSignals.Add ("PanaZillia");
            if (activeTradeUsesKingZilla)
                activeSignals.Add ("KingZilla");
            if (activeTradeUsesKingPana)
                activeSignals.Add ("KingPana");

            return activeSignals.Count > 0 ? string.Join (", ", activeSignals) : "None";
        }

        private string BuildActiveSignalAbbreviationList ()
        {
            List<string> activeSignals = new List<string>();

            if (activeTradeUsesPanaZillia)
                activeSignals.Add ("PZ");
            if (activeTradeUsesKingZilla)
                activeSignals.Add ("KZ");
            if (activeTradeUsesKingPana)
                activeSignals.Add ("KP");

            return activeSignals.Count > 0 ? string.Join ("+", activeSignals) : "None";
        }

        private string BuildSignalTrackingDisplayText ()
        {
            if (!EnableSignalTracking)
                return string.Empty;

            // Join the display lines directly with a new line instead of an empty string
            return Environment.NewLine + string.Join (Environment.NewLine, BuildSignalTrackingDisplayLines ());
        }

        private List<string> BuildSignalTrackingDisplayLines ()
        {
            List<string> enabledSignals = new List<string>();
            List<string> lines = new List<string>();

            if (UsePanaZilliaSignals)
            {
                enabledSignals.Add ("PZ");
                lines.Add ($"PZ T:{panaZilliaStats.TotalTrades} Lg:{panaZilliaStats.LongTrades} Sh:{panaZilliaStats.ShortTrades} W:{panaZilliaStats.Winners} L:{panaZilliaStats.Losers}");
            }

            if (UseKingZillaSignals)
            {
                enabledSignals.Add ("KZ");
                lines.Add ($"KZ T:{kingZillaStats.TotalTrades} Lg:{kingZillaStats.LongTrades} Sh:{kingZillaStats.ShortTrades} W:{kingZillaStats.Winners} L:{kingZillaStats.Losers}");
            }

            if (UseKingPanaSignals)
            {
                enabledSignals.Add ("KP");
                lines.Add ($"KP T:{kingPanaStats.TotalTrades} Lg:{kingPanaStats.LongTrades} Sh:{kingPanaStats.ShortTrades} W:{kingPanaStats.Winners} L:{kingPanaStats.Losers}");
            }

            lines.Insert (0, enabledSignals.Count > 0 ? "Enabled: " + string.Join (", ", enabledSignals) : "Enabled: None");
            return lines;
        }

        private bool CheckTradingTimeframes (int currentTime)
        {
            // If no TF filters are enabled, allow trading all day
            if (!EnableTF1 && !EnableTF2)
                return true;

            bool tf1 = EnableTF1 && IsTimeInWindow(currentTime, ToTime(StartTime1), ToTime(EndTime1));
            bool tf2 = EnableTF2 && IsTimeInWindow(currentTime, ToTime(StartTime2), ToTime(EndTime2));

            return tf1 || tf2;
        }

        private void CheckFlattenTimeframes (int currentTime)
        {
            // If no TF filters are enabled, do not do any TF-based flattening
            if (!EnableTF1 && !EnableTF2)
                return;

            if (CurrentBar < 1)
                return;

            int previousTime = ToTime(Time[1]);

            bool flatten1 = EnableTF1 && FlattenTF1
                && IsTimeInWindow(previousTime, ToTime(StartTime1), ToTime(EndTime1))
                && !IsTimeInWindow(currentTime, ToTime(StartTime1), ToTime(EndTime1));

            bool flatten2 = EnableTF2 && FlattenTF2
                && IsTimeInWindow(previousTime, ToTime(StartTime2), ToTime(EndTime2))
                && !IsTimeInWindow(currentTime, ToTime(StartTime2), ToTime(EndTime2));

            if (flatten1 || flatten2)
                FlattenAll ();
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

        private void FlattenAll ()
        {
            FlattenEverything ("FlattenAll requested");
        }

        private void FlattenEverything (string reason)
        {
            if (State != State.Realtime)
                return;

            Print ($"[{Name}] FlattenEverything: {reason}");

            // 1) Close ATM if we have one tracked
            if (!string.IsNullOrEmpty (atmStrategyId))
            {
                try
                {
                    AtmStrategyClose (atmStrategyId);
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

        private void CheckForNakedPositions (DateTime tickTime)
        {
            if (State != State.Realtime)
                return;

            if ((tickTime - lastNakedCheck).TotalSeconds < NakedCheckIntervalSeconds)
                return;

            lastNakedCheck = tickTime;

            // Nothing to check if we're flat
            if (Position.MarketPosition == MarketPosition.Flat)
                return;

            bool hasActiveAtm = !string.IsNullOrEmpty(atmStrategyId)
                && GetAtmStrategyMarketPosition(atmStrategyId) != Cbi.MarketPosition.Flat;

            bool hasProtectiveOrders = HasWorkingProtectiveOrders();

            if (!hasActiveAtm || !hasProtectiveOrders)
            {
                Print ($"[{Name}] NAKED POSITION DETECTED at {tickTime:HH:mm:ss} | "
                    + $"Position={Position.MarketPosition} {Position.Quantity} | "
                    + $"HasActiveAtm={hasActiveAtm} HasProtectiveOrders={hasProtectiveOrders}. Flattening.");

                FlattenEverything ("Naked position watchdog");
            }
        }

        private bool HasWorkingProtectiveOrders ()
        {
            if (Account == null)
                return false;

            lock (Account.Orders)
            {
                foreach (Order o in Account.Orders)
                {
                    if (o.Instrument != Instrument)
                        continue;

                    if (o.OrderState != OrderState.Working
                        && o.OrderState != OrderState.Accepted
                        && o.OrderState != OrderState.Submitted)
                        continue;

                    // Exit-side stop or target relative to current position
                    bool isExitSide =
                        (Position.MarketPosition == MarketPosition.Long && (o.OrderAction == OrderAction.Sell || o.OrderAction == OrderAction.SellShort))
                     || (Position.MarketPosition == MarketPosition.Short && (o.OrderAction == OrderAction.Buy || o.OrderAction == OrderAction.BuyToCover));

                    if (!isExitSide)
                        continue;

                    if (o.OrderType == OrderType.StopMarket
                        || o.OrderType == OrderType.StopLimit
                        || o.OrderType == OrderType.Limit)
                        return true;
                }
            }

            return false;
        }

        #region Custom Property Manipulation
        private void ModifyPNLProperties (PropertyDescriptorCollection col)
        {
            if (!UseDailyProfitTarget)
                col.Remove (col["DailyProfitTarget"]);
            if (!UseDailyLossLimit)
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
            ModifyPNLProperties (col);
            ModifySessionProperties (col);
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

        [NinjaScriptProperty]
        [ReadOnly (true)]
        [Display (Name = "Strategy Version", GroupName = "Strategy Information", Order = 1)]
        public string StrategyVersion
        {
            get; set;
        }

        [NinjaScriptProperty]
        [ReadOnly (true)]
        [Display (Name = "Author", GroupName = "Strategy Information", Order = 2)]
        public string Author
        {
            get; set;
        }

        [NinjaScriptProperty]
        [ReadOnly (true)]
        [Display (Name = "Credits", GroupName = "Strategy Information", Order = 3)]
        public string Credits
        {
            get; set;
        }

        [TypeConverter (typeof (FriendlyAtmConverter))]
        [PropertyEditor ("NinjaTrader.Gui.Tools.StringStandardValuesEditorKey")]
        [Display (Name = "Atm Strategy", Order = 0, GroupName = "ATM Parameters", Description = "Select an existing NT8 ATM template.")]
        public string AtmStrategy
        {
            get; set;
        }

        [NinjaScriptProperty]
        [Display (Name = "Enable Signal Tracking", Order = 0, GroupName = "Signals")]
        [RefreshProperties (RefreshProperties.All)]
        public bool EnableSignalTracking
        {
            get; set;
        }

        [NinjaScriptProperty]
        [Display (Name = "Use PanaZillia Signals", Order = 1, GroupName = "Signals")]
        [RefreshProperties (RefreshProperties.All)]
        public bool UsePanaZilliaSignals
        {
            get; set;
        }

        [NinjaScriptProperty]
        [Display (Name = "Use KingZilla Signals", Order = 2, GroupName = "Signals")]
        [RefreshProperties (RefreshProperties.All)]
        public bool UseKingZillaSignals
        {
            get; set;
        }

        [NinjaScriptProperty]
        [Display (Name = "Use KingPana Signals", Order = 3, GroupName = "Signals")]
        [RefreshProperties (RefreshProperties.All)]
        public bool UseKingPanaSignals
        {
            get; set;
        }

        [NinjaScriptProperty]
        [Display (Name = "Use Unrealized PNL", Order = 0, GroupName = "Risk Management", Description = "If true, checks limits tick-by-tick including ATM open profit.")]
        [RefreshProperties (RefreshProperties.All)]
        public bool UseUnrealizedPnl
        {
            get; set;
        }

        [NinjaScriptProperty]
        [Display (Name = "Use Daily Profit Target", Order = 1, GroupName = "Risk Management")]
        [RefreshProperties (RefreshProperties.All)]
        public bool UseDailyProfitTarget
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
        [Display (Name = "Use Daily Loss Limit", Order = 3, GroupName = "Risk Management")]
        [RefreshProperties (RefreshProperties.All)]
        public bool UseDailyLossLimit
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

        [NinjaScriptProperty]
        [Display (Name = "Log Trades", Order = 9, GroupName = "Session Parameters", Description = "Write a CSV trade log to the NinjaTrader user data folder.")]
        public bool LogEnabled
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
                        NinjaTrader.Code.Output.Process (atmName, PrintTo.OutputTab1);
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