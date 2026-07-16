#region Using declarations
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Data;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Tools;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.DrawingTools;
using NinjaTrader.NinjaScript.Indicators;
#endregion

namespace NinjaTrader.NinjaScript
{
    public enum GodTradesStrategyEntryMode
    {
        Market,
        Limit,
        StopMarket
    }

    public enum GodTradesStrategyDirectionMode
    {
        Both,
        LongOnly,
        ShortOnly
    }
}

namespace NinjaTrader.NinjaScript.Strategies
{
    public class GodTradesStrategy : Strategy
    {
        private const string LongEntryPrefix  = "GTS_Long_";
        private const string ShortEntryPrefix = "GTS_Short_";

        private GodTrades21 godTrades;
        private Order entryOrder;
        private int entrySubmitBar = -1;
        private int lastProcessedSignalBar = -1;
        private int entryCounter;

        private string activeEntrySignalName = string.Empty;
        private string lastSignalSource = "None";
        private string lastBlockReason = "Ready";

        private MarketPosition priorMarketPosition = MarketPosition.Flat;
        private double currentStopPrice = double.NaN;
        private double highestSinceEntry = double.MinValue;
        private double lowestSinceEntry = double.MaxValue;
        private bool breakEvenMoved;

        private double sessionStartCumProfit;
        private bool dailyPnlInitialized;
        private bool pnlCutoffHit;
        private int lastEodFlatDate = -1;

        private int pendingReverseDirection;
        private string pendingReverseSource = string.Empty;
        private double pendingReverseClose;
        private double pendingReverseHigh;
        private double pendingReverseLow;

        private SimpleFont statusFont;

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Name = "GodTradesStrategy";
                Description = "Trades GodTrades21 Bollinger Gap (BG), Failed Continuation/Continuation (FC), and opposite-direction body-engulf OBR signals. Each signal type can be enabled independently. Includes market/limit/stop-market entries, fixed TP/SL, breakeven, trailing stop, day and time filters, skip windows, daily PnL cutoffs, end-of-day flattening, and optional account-flat enforcement.";

                Calculate = Calculate.OnEachTick;
                EntriesPerDirection = 1;
                EntryHandling = EntryHandling.UniqueEntries;
                IsExitOnSessionCloseStrategy = false;
                ExitOnSessionCloseSeconds = 30;
                IsFillLimitOnTouch = false;
                MaximumBarsLookBack = MaximumBarsLookBack.TwoHundredFiftySix;
                OrderFillResolution = OrderFillResolution.Standard;
                Slippage = 0;
                StartBehavior = StartBehavior.WaitUntilFlat;
                TimeInForce = TimeInForce.Gtc;
                TraceOrders = false;
                RealtimeErrorHandling = RealtimeErrorHandling.StopCancelClose;
                StopTargetHandling = StopTargetHandling.PerEntryExecution;
                BarsRequiredToTrade = 20;
                IsInstantiatedOnEachOptimizationIteration = true;
				IncludeTradeHistoryInBacktest = true;
                DirectionMode = GodTradesStrategyDirectionMode.Both;
                Contracts = 1;
                EntryMode = GodTradesStrategyEntryMode.Market;
                EntryOffsetTicks = 0;
                CancelUnfilledEntryAfterBars = 1;
                TradeOnlyWhenFlat = true;
                RequireAccountFlat = true;
                ReverseOnOppositeSignal = false;
                RealTimeOnly = false;
                IgnoreConflictingSignals = true;

                EnableBGTrades = true;
                EnableFCTrades = true;
                EnableOBRTrades = true;

                UseProfitTarget = true;
                ProfitTargetTicks = 40;
                UseStopLoss = true;
                StopLossTicks = 30;

                UseBreakEven = false;
                BreakEvenTriggerTicks = 20;
                BreakEvenPlusTicks = 1;

                UseTrailingStop = false;
                TrailingTriggerTicks = 30;
                TrailingDistanceTicks = 20;
                TrailingStepTicks = 4;

                UseDailyProfitCutoff = false;
                DailyProfitCutoff = 500;
                UseDailyLossCutoff = false;
                DailyLossCutoff = 500;
                IncludeUnrealizedPnlInCutoff = true;
                FlattenOnPnlCutoff = true;

                UseEndOfDayFlatten = false;
                EndOfDayFlattenTime = 155500;

                TradeMonday = true;
                TradeTuesday = true;
                TradeWednesday = true;
                TradeThursday = true;
                TradeFriday = true;
                TradeSaturday = false;
                TradeSunday = false;

                UseTradeWindow1 = true;
                TradeWindow1Start = 0;
                TradeWindow1End = 235959;
                UseTradeWindow2 = false;
                TradeWindow2Start = 0;
                TradeWindow2End = 235959;
                UseTradeWindow3 = false;
                TradeWindow3Start = 0;
                TradeWindow3End = 235959;

                UseSkipWindow1 = false;
                SkipWindow1Start = 74000;
                SkipWindow1End = 84000;
                UseSkipWindow2 = false;
                SkipWindow2Start = 110000;
                SkipWindow2End = 122500;
                UseSkipWindow3 = false;
                SkipWindow3Start = 0;
                SkipWindow3End = 0;

                ShowUnderlyingIndicatorOnChart = true;
                ShowStrategySignalMarkers = false;
                ShowStatusPanel = true;
                StatusPanelPosition = TextPosition.TopRight;
                StatusPanelFontSize = 12;
                LongStrategyMarkerBrush = Brushes.Lime;
                ShortStrategyMarkerBrush = Brushes.Magenta;
                StrategyMarkerOffsetTicks = 5;

                MinimumGapSizeTicks = 1;
                MinimumBarsBeforeValid = 3;
                MinimumBodyTicks = 0;
                MaximumGapBarRangeTicks = 0;
                MaximumActiveGapsToTrack = 300;
                EarlyTouchHandling = GodTrades21EarlyTouchHandling.StopLineImmediately;
                ValidTouchBehavior = GodTrades21ValidTouchBehavior.StopLineAndMarkContinuation;

                UseBollingerMidpointFilterForContinuation = true;
                FcBollingerLocationSource = GodTrades21FcBollingerLocationSource.WickExtreme;
                FcLongBelowMidpointPercent = 50.0;
                FcShortAboveMidpointPercent = 50.0;
                ContinuationConfirmationMode = GodTrades21ContinuationConfirmationMode.RequireCloseBeyondFullZone;
                ConfirmationBarsAfterTouch = 2;
                RequireSignalCandleDirection = true;
                RequireCorrectContinuationApproach = true;

                UseBollingerMidpointFilterForOutsideBarReversal = true;
                AllowObrBarOutsideBollingerBand = true;
                BearishObrUpperBandTouchToleranceTicks = 4;
                BullishObrLowerBandTouchToleranceTicks = 4;

                UseIndicatorSignalTimeFilter = false;
                IndicatorSignalStartTime = 101500;
                IndicatorSignalEndTime = 150000;

                BollingerPeriod = 20;
                BollingerStdDev = 2.0;
                BollingerBandProximityTicks = 8;

                EnableSpiderwebWarning = true;
                ShowSpiderwebWarningText = true;
                SpiderwebDistanceTicks = 100;
                SpiderwebLineCount = 5;
                SpiderwebTextFontSize = 15;
            }
            else if (State == State.DataLoaded)
            {
                statusFont = new SimpleFont("Arial", StatusPanelFontSize);
                entryCounter = 0;
                priorMarketPosition = MarketPosition.Flat;
				sessionStartCumProfit = 0;
				dailyPnlInitialized = false;
				pnlCutoffHit = false;

                godTrades = GodTrades21(
                    MinimumGapSizeTicks,
                    MinimumBarsBeforeValid,
                    MinimumBodyTicks,
                    MaximumGapBarRangeTicks,
                    MaximumActiveGapsToTrack,
                    EarlyTouchHandling,
                    ValidTouchBehavior,
                    true,
                    UseBollingerMidpointFilterForContinuation,
                    FcBollingerLocationSource,
                    FcLongBelowMidpointPercent,
                    FcShortAboveMidpointPercent,
                    ContinuationConfirmationMode,
                    ConfirmationBarsAfterTouch,
                    RequireSignalCandleDirection,
                    RequireCorrectContinuationApproach,
                    true,
                    ShowUnderlyingIndicatorOnChart,
                    ShowUnderlyingIndicatorOnChart,
                    UseBollingerMidpointFilterForOutsideBarReversal,
                    AllowObrBarOutsideBollingerBand,
                    BearishObrUpperBandTouchToleranceTicks,
                    BullishObrLowerBandTouchToleranceTicks,
                    UseIndicatorSignalTimeFilter,
                    IndicatorSignalStartTime,
                    IndicatorSignalEndTime,
                    true,
                    BollingerPeriod,
                    BollingerStdDev,
                    BollingerBandProximityTicks,
                    EnableSpiderwebWarning && ShowUnderlyingIndicatorOnChart,
                    ShowSpiderwebWarningText && ShowUnderlyingIndicatorOnChart,
                    SpiderwebDistanceTicks,
                    SpiderwebLineCount,
                    SpiderwebTextFontSize,
                    0,
                    GodTrades21TargetMode.None,
                    40,
                    GodTrades21LinePriceMode.Midpoint,
                    ShowUnderlyingIndicatorOnChart,
                    false,
                    ShowUnderlyingIndicatorOnChart,
                    ShowUnderlyingIndicatorOnChart,
                    ShowUnderlyingIndicatorOnChart,
                    ShowUnderlyingIndicatorOnChart,
                    false,
                    2,
                    DashStyleHelper.Solid,
                    12,
                    3,
                    7);

                if (ShowUnderlyingIndicatorOnChart)
                    AddChartIndicator(godTrades);
            }
            else if (State == State.Terminated)
            {
                RemoveDrawObject("GodTradesStrategy_Status");
            }
        }

        protected override void OnBarUpdate()
        {
            if (BarsInProgress != 0)
                return;

            if (CurrentBar < Math.Max(BarsRequiredToTrade, BollingerPeriod + 2))
                return;

            ResetDailyPnlIfNeeded();
            UpdatePositionState();
            ManageWorkingEntryOrder();
            ManageOpenPosition();
            HandleEndOfDayFlatten();
            UpdatePnlCutoffState();

            if (pnlCutoffHit || IsPastEndOfDayFlattenTime())
            {
                ClearPendingReversal();
            }
            else
            {
                TrySubmitPendingReversal();
            }

            ProcessGodTradesSignals();
            DrawStatusPanel();
        }

        private void ProcessGodTradesSignals()
        {
            if (godTrades == null)
                return;

            bool bgLong = EnableBGTrades && IsPositive(godTrades.BollingerGapLong[0]);
            bool bgShort = EnableBGTrades && IsNegative(godTrades.BollingerGapShort[0]);
            bool fcLong = EnableFCTrades && IsPositive(godTrades.ContinuationLong[0]);
            bool fcShort = EnableFCTrades && IsNegative(godTrades.ContinuationShort[0]);
            bool obrLong = EnableOBRTrades && IsPositive(godTrades.OutsideBarReversalSignal[0]);
            bool obrShort = EnableOBRTrades && IsNegative(godTrades.OutsideBarReversalSignal[0]);

            bool longSignal = bgLong || fcLong || obrLong;
            bool shortSignal = bgShort || fcShort || obrShort;

            if (!longSignal && !shortSignal)
                return;

            if (CurrentBar == lastProcessedSignalBar)
                return;

            lastProcessedSignalBar = CurrentBar;

            string longSource = BuildSignalSource(bgLong, fcLong, obrLong);
            string shortSource = BuildSignalSource(bgShort, fcShort, obrShort);

            if (ShowStrategySignalMarkers)
                DrawStrategySignalMarkers(longSignal, shortSignal, longSource, shortSource);

            if (longSignal && shortSignal && IgnoreConflictingSignals)
            {
                lastSignalSource = "Conflict: " + longSource + " / " + shortSource;
                lastBlockReason = "Long and short signals on same bar";
                return;
            }

            if (RealTimeOnly && State != State.Realtime)
            {
                lastBlockReason = "Real-time only";
                return;
            }

            if (pnlCutoffHit)
            {
                lastBlockReason = "Daily PnL cutoff";
                return;
            }

            if (!IsTradingTimeAllowed())
            {
                lastBlockReason = "Day/time filter";
                return;
            }

            if (HasWorkingEntryOrder())
            {
                lastBlockReason = "Pending entry already working";
                return;
            }

            int direction = 0;
            string source = string.Empty;

            if (longSignal && !shortSignal)
            {
                direction = 1;
                source = longSource;
            }
            else if (shortSignal && !longSignal)
            {
                direction = -1;
                source = shortSource;
            }
            else
            {
                int masterDirection = NormalizeSignal(godTrades.SignalDirection[0]);
                if (masterDirection > 0 && longSignal)
                {
                    direction = 1;
                    source = longSource;
                }
                else if (masterDirection < 0 && shortSignal)
                {
                    direction = -1;
                    source = shortSource;
                }
                else
                {
                    lastBlockReason = "Conflicting signal direction";
                    return;
                }
            }

            if (!IsDirectionEnabled(direction))
            {
                lastBlockReason = direction > 0 ? "Long direction disabled" : "Short direction disabled";
                return;
            }

            lastSignalSource = source;

            if (Position.MarketPosition == MarketPosition.Flat)
            {
                if (RequireAccountFlat && !IsAccountFlatForInstrument())
                {
                    lastBlockReason = "Account not flat for instrument";
                    return;
                }

                SubmitEntry(direction, Close[0], High[0], Low[0], source);
                return;
            }

            bool oppositeSignal =
                (Position.MarketPosition == MarketPosition.Long && direction < 0)
                || (Position.MarketPosition == MarketPosition.Short && direction > 0);

            if (oppositeSignal && ReverseOnOppositeSignal)
            {
                QueueReversal(direction, source, Close[0], High[0], Low[0]);
                FlattenOpenPosition("Reverse");
                lastBlockReason = "Reversing on opposite signal";
                return;
            }

            if (TradeOnlyWhenFlat)
                lastBlockReason = "Position not flat";
            else
                lastBlockReason = "Entries per direction already limited";
        }

        private void SubmitEntry(int direction, double referenceClose, double referenceHigh, double referenceLow, string source)
        {
            if (direction == 0 || HasWorkingEntryOrder())
                return;

            string cleanSource = string.IsNullOrWhiteSpace(source) ? "Signal" : source.Replace("+", "_");
            string entryName = (direction > 0 ? LongEntryPrefix : ShortEntryPrefix)
                + cleanSource + "_" + CurrentBar + "_" + entryCounter++;

            activeEntrySignalName = entryName;
            ConfigureProtection(entryName);

            if (EntryMode == GodTradesStrategyEntryMode.Market)
            {
                if (direction > 0)
                    EnterLong(Contracts, entryName);
                else
                    EnterShort(Contracts, entryName);
            }
            else if (EntryMode == GodTradesStrategyEntryMode.Limit)
            {
                double price = direction > 0
                    ? referenceClose - EntryOffsetTicks * TickSize
                    : referenceClose + EntryOffsetTicks * TickSize;
                price = Instrument.MasterInstrument.RoundToTickSize(price);

                if (direction > 0)
                    EnterLongLimit(Contracts, price, entryName);
                else
                    EnterShortLimit(Contracts, price, entryName);
            }
            else
            {
                double price = direction > 0
                    ? referenceHigh + EntryOffsetTicks * TickSize
                    : referenceLow - EntryOffsetTicks * TickSize;
                price = Instrument.MasterInstrument.RoundToTickSize(price);

                if (direction > 0)
                    EnterLongStopMarket(Contracts, price, entryName);
                else
                    EnterShortStopMarket(Contracts, price, entryName);
            }

            entrySubmitBar = CurrentBar;
            lastBlockReason = "Entry submitted: " + source;
        }

        private void ConfigureProtection(string entryName)
        {
            if (UseProfitTarget)
                SetProfitTarget(entryName, CalculationMode.Ticks, ProfitTargetTicks);

            if (UseStopLoss)
                SetStopLoss(entryName, CalculationMode.Ticks, StopLossTicks, false);
        }

        private void ManageWorkingEntryOrder()
        {
            if (!HasWorkingEntryOrder())
                return;

            if (CancelUnfilledEntryAfterBars > 0
                && entrySubmitBar >= 0
                && CurrentBar - entrySubmitBar >= CancelUnfilledEntryAfterBars)
            {
                CancelOrder(entryOrder);
                lastBlockReason = "Unfilled entry expired";
            }

            if (!IsTradingTimeAllowed() || pnlCutoffHit || IsPastEndOfDayFlattenTime())
            {
                CancelOrder(entryOrder);
                lastBlockReason = "Pending entry canceled by filter";
            }
        }

        private void ManageOpenPosition()
        {
            if (Position.MarketPosition == MarketPosition.Flat || string.IsNullOrEmpty(activeEntrySignalName))
                return;

            double averagePrice = Position.AveragePrice;
            double proposedStop = currentStopPrice;

            if (Position.MarketPosition == MarketPosition.Long)
            {
                highestSinceEntry = Math.Max(highestSinceEntry, High[0]);
                double favorableTicks = (highestSinceEntry - averagePrice) / TickSize;

                if (UseBreakEven && !breakEvenMoved && favorableTicks >= BreakEvenTriggerTicks)
                {
                    double breakEvenStop = averagePrice + BreakEvenPlusTicks * TickSize;
                    proposedStop = double.IsNaN(proposedStop) ? breakEvenStop : Math.Max(proposedStop, breakEvenStop);
                    breakEvenMoved = true;
                }

                if (UseTrailingStop && favorableTicks >= TrailingTriggerTicks)
                {
                    double trailStop = highestSinceEntry - TrailingDistanceTicks * TickSize;
                    if (double.IsNaN(proposedStop)
                        || trailStop >= proposedStop + TrailingStepTicks * TickSize)
                        proposedStop = trailStop;
                }

                if (!double.IsNaN(proposedStop))
                {
                    double referenceBid = State == State.Realtime ? GetCurrentBid() : Close[0];
                    if (referenceBid <= 0)
                        referenceBid = Close[0];

                    proposedStop = Math.Min(proposedStop, referenceBid - TickSize);
                    proposedStop = Instrument.MasterInstrument.RoundToTickSize(proposedStop);

                    if (double.IsNaN(currentStopPrice) || proposedStop > currentStopPrice + TickSize * 0.1)
                    {
                        currentStopPrice = proposedStop;
                        SetStopLoss(activeEntrySignalName, CalculationMode.Price, currentStopPrice, false);
                    }
                }
            }
            else if (Position.MarketPosition == MarketPosition.Short)
            {
                lowestSinceEntry = Math.Min(lowestSinceEntry, Low[0]);
                double favorableTicks = (averagePrice - lowestSinceEntry) / TickSize;

                if (UseBreakEven && !breakEvenMoved && favorableTicks >= BreakEvenTriggerTicks)
                {
                    double breakEvenStop = averagePrice - BreakEvenPlusTicks * TickSize;
                    proposedStop = double.IsNaN(proposedStop) ? breakEvenStop : Math.Min(proposedStop, breakEvenStop);
                    breakEvenMoved = true;
                }

                if (UseTrailingStop && favorableTicks >= TrailingTriggerTicks)
                {
                    double trailStop = lowestSinceEntry + TrailingDistanceTicks * TickSize;
                    if (double.IsNaN(proposedStop)
                        || trailStop <= proposedStop - TrailingStepTicks * TickSize)
                        proposedStop = trailStop;
                }

                if (!double.IsNaN(proposedStop))
                {
                    double referenceAsk = State == State.Realtime ? GetCurrentAsk() : Close[0];
                    if (referenceAsk <= 0)
                        referenceAsk = Close[0];

                    proposedStop = Math.Max(proposedStop, referenceAsk + TickSize);
                    proposedStop = Instrument.MasterInstrument.RoundToTickSize(proposedStop);

                    if (double.IsNaN(currentStopPrice) || proposedStop < currentStopPrice - TickSize * 0.1)
                    {
                        currentStopPrice = proposedStop;
                        SetStopLoss(activeEntrySignalName, CalculationMode.Price, currentStopPrice, false);
                    }
                }
            }
        }

        private void UpdatePositionState()
        {
            MarketPosition current = Position.MarketPosition;
            if (current == priorMarketPosition)
                return;

            if (current == MarketPosition.Long)
            {
                highestSinceEntry = Math.Max(Position.AveragePrice, High[0]);
                lowestSinceEntry = Low[0];
                currentStopPrice = UseStopLoss
                    ? Instrument.MasterInstrument.RoundToTickSize(Position.AveragePrice - StopLossTicks * TickSize)
                    : double.NaN;
                breakEvenMoved = false;
            }
            else if (current == MarketPosition.Short)
            {
                lowestSinceEntry = Math.Min(Position.AveragePrice, Low[0]);
                highestSinceEntry = High[0];
                currentStopPrice = UseStopLoss
                    ? Instrument.MasterInstrument.RoundToTickSize(Position.AveragePrice + StopLossTicks * TickSize)
                    : double.NaN;
                breakEvenMoved = false;
            }
            else
            {
                currentStopPrice = double.NaN;
                highestSinceEntry = double.MinValue;
                lowestSinceEntry = double.MaxValue;
                breakEvenMoved = false;
                activeEntrySignalName = string.Empty;
            }

            priorMarketPosition = current;
        }

        private void QueueReversal(int direction, string source, double referenceClose, double referenceHigh, double referenceLow)
        {
            pendingReverseDirection = direction;
            pendingReverseSource = source;
            pendingReverseClose = referenceClose;
            pendingReverseHigh = referenceHigh;
            pendingReverseLow = referenceLow;
        }

        private void TrySubmitPendingReversal()
        {
            if (pendingReverseDirection == 0)
                return;

            if (Position.MarketPosition != MarketPosition.Flat || HasWorkingEntryOrder())
                return;

            if (!IsTradingTimeAllowed() || pnlCutoffHit)
            {
                ClearPendingReversal();
                return;
            }

            if (RequireAccountFlat && !IsAccountFlatForInstrument())
                return;

            int direction = pendingReverseDirection;
            string source = pendingReverseSource;
            double referenceClose = pendingReverseClose;
            double referenceHigh = pendingReverseHigh;
            double referenceLow = pendingReverseLow;
            ClearPendingReversal();
            SubmitEntry(direction, referenceClose, referenceHigh, referenceLow, source);
        }

        private void ClearPendingReversal()
        {
            pendingReverseDirection = 0;
            pendingReverseSource = string.Empty;
            pendingReverseClose = 0;
            pendingReverseHigh = 0;
            pendingReverseLow = 0;
        }

        private void FlattenOpenPosition(string reason)
        {
            if (Position.MarketPosition == MarketPosition.Long)
                ExitLong("GTS_Exit_" + reason, activeEntrySignalName);
            else if (Position.MarketPosition == MarketPosition.Short)
                ExitShort("GTS_Exit_" + reason, activeEntrySignalName);
        }

        private void ResetDailyPnlIfNeeded()
        {
            bool newSession = Bars.IsFirstBarOfSession && IsFirstTickOfBar;
            if (!dailyPnlInitialized || newSession)
            {
                sessionStartCumProfit = GetCumProfit();
                pnlCutoffHit = false;
                dailyPnlInitialized = true;
            }
        }

        private void UpdatePnlCutoffState()
        {
            if (pnlCutoffHit)
                return;

            double dailyPnl = GetDailyPnl();
            bool profitHit = UseDailyProfitCutoff && DailyProfitCutoff > 0 && dailyPnl >= DailyProfitCutoff;
            bool lossHit = UseDailyLossCutoff && DailyLossCutoff > 0 && dailyPnl <= -DailyLossCutoff;

            if (!profitHit && !lossHit)
                return;

            pnlCutoffHit = true;
            ClearPendingReversal();

            if (HasWorkingEntryOrder())
                CancelOrder(entryOrder);

            lastBlockReason = profitHit ? "Daily profit cutoff reached" : "Daily loss cutoff reached";

            if (FlattenOnPnlCutoff)
                FlattenOpenPosition("PnlCutoff");
        }

        private double GetDailyPnl()
        {
            double realized = GetCumProfit() - sessionStartCumProfit;
            double unrealized = 0;

            if (IncludeUnrealizedPnlInCutoff && Position.MarketPosition != MarketPosition.Flat)
                unrealized = Position.GetUnrealizedProfitLoss(PerformanceUnit.Currency, Close[0]);

            return realized + unrealized;
        }

        private double GetCumProfit()
        {
            return SystemPerformance.AllTrades.TradesPerformance.Currency.CumProfit;
        }

        private void HandleEndOfDayFlatten()
        {
            if (!UseEndOfDayFlatten)
                return;

            int now = ToTime(Time[0]);
            int today = Time[0].Year * 10000 + Time[0].Month * 100 + Time[0].Day;

            if (now >= EndOfDayFlattenTime && lastEodFlatDate != today)
            {
                if (HasWorkingEntryOrder())
                    CancelOrder(entryOrder);

                ClearPendingReversal();
                FlattenOpenPosition("EOD");
                lastEodFlatDate = today;
                lastBlockReason = "End-of-day flatten";
            }
        }

        private bool IsPastEndOfDayFlattenTime()
        {
            return UseEndOfDayFlatten && ToTime(Time[0]) >= EndOfDayFlattenTime;
        }

        private bool IsTradingTimeAllowed()
        {
            if (!IsAllowedDay(Time[0].DayOfWeek))
                return false;

            if (IsPastEndOfDayFlattenTime())
                return false;

            int now = ToTime(Time[0]);
            bool anyTradeWindowEnabled = UseTradeWindow1 || UseTradeWindow2 || UseTradeWindow3;
            bool inTradeWindow = !anyTradeWindowEnabled
                || (UseTradeWindow1 && IsTimeInside(now, TradeWindow1Start, TradeWindow1End))
                || (UseTradeWindow2 && IsTimeInside(now, TradeWindow2Start, TradeWindow2End))
                || (UseTradeWindow3 && IsTimeInside(now, TradeWindow3Start, TradeWindow3End));

            if (!inTradeWindow)
                return false;

            bool inSkipWindow =
                (UseSkipWindow1 && IsTimeInside(now, SkipWindow1Start, SkipWindow1End))
                || (UseSkipWindow2 && IsTimeInside(now, SkipWindow2Start, SkipWindow2End))
                || (UseSkipWindow3 && IsTimeInside(now, SkipWindow3Start, SkipWindow3End));

            return !inSkipWindow;
        }

        private bool IsAllowedDay(DayOfWeek day)
        {
            switch (day)
            {
                case DayOfWeek.Monday: return TradeMonday;
                case DayOfWeek.Tuesday: return TradeTuesday;
                case DayOfWeek.Wednesday: return TradeWednesday;
                case DayOfWeek.Thursday: return TradeThursday;
                case DayOfWeek.Friday: return TradeFriday;
                case DayOfWeek.Saturday: return TradeSaturday;
                case DayOfWeek.Sunday: return TradeSunday;
                default: return false;
            }
        }

        private bool IsTimeInside(int time, int start, int end)
        {
            if (start <= end)
                return time >= start && time <= end;

            return time >= start || time <= end;
        }

        private bool IsDirectionEnabled(int direction)
        {
            if (DirectionMode == GodTradesStrategyDirectionMode.Both)
                return true;

            if (direction > 0)
                return DirectionMode == GodTradesStrategyDirectionMode.LongOnly;

            return DirectionMode == GodTradesStrategyDirectionMode.ShortOnly;
        }

        private bool IsAccountFlatForInstrument()
        {
            if (State != State.Realtime || Account == null)
                return Position.MarketPosition == MarketPosition.Flat;

            try
            {
                foreach (NinjaTrader.Cbi.Position accountPosition in Account.Positions)
                {
                    if (accountPosition == null || accountPosition.Instrument == null)
                        continue;

                    if (string.Equals(accountPosition.Instrument.FullName, Instrument.FullName, StringComparison.OrdinalIgnoreCase)
                        && accountPosition.MarketPosition != MarketPosition.Flat)
                        return false;
                }
            }
            catch
            {
                return Position.MarketPosition == MarketPosition.Flat;
            }

            return true;
        }

        private bool HasWorkingEntryOrder()
        {
            return entryOrder != null
                && entryOrder.OrderState != OrderState.Filled
                && entryOrder.OrderState != OrderState.Cancelled
                && entryOrder.OrderState != OrderState.Rejected;
        }

        private bool IsEntryOrderName(string name)
        {
            return !string.IsNullOrEmpty(name)
                && (name.StartsWith(LongEntryPrefix, StringComparison.Ordinal)
                    || name.StartsWith(ShortEntryPrefix, StringComparison.Ordinal));
        }

        protected override void OnOrderUpdate(Order order, double limitPrice, double stopPrice,
            int quantity, int filled, double averageFillPrice, OrderState orderState,
            DateTime time, ErrorCode error, string nativeError)
        {
            if (order == null || !IsEntryOrderName(order.Name))
                return;

            if (orderState == OrderState.Filled
                || orderState == OrderState.Cancelled
                || orderState == OrderState.Rejected)
            {
                entryOrder = null;
                entrySubmitBar = -1;

                if ((orderState == OrderState.Cancelled || orderState == OrderState.Rejected)
                    && Position.MarketPosition == MarketPosition.Flat
                    && string.Equals(activeEntrySignalName, order.Name, StringComparison.Ordinal))
                    activeEntrySignalName = string.Empty;

                if (orderState == OrderState.Rejected)
                    lastBlockReason = "Entry rejected: " + nativeError;
            }
            else
            {
                entryOrder = order;
            }
        }

        private int NormalizeSignal(double value)
        {
            if (double.IsNaN(value))
                return 0;
            if (value > 0.5)
                return 1;
            if (value < -0.5)
                return -1;
            return 0;
        }

        private bool IsPositive(double value)
        {
            return !double.IsNaN(value) && value > 0.5;
        }

        private bool IsNegative(double value)
        {
            return !double.IsNaN(value) && value < -0.5;
        }

        private string BuildSignalSource(bool bg, bool fc, bool obr)
        {
            string source = string.Empty;
            if (bg)
                source = "BG";
            if (fc)
                source += (source.Length > 0 ? "+" : string.Empty) + "FC";
            if (obr)
                source += (source.Length > 0 ? "+" : string.Empty) + "OBR";
            return source.Length == 0 ? "None" : source;
        }

        private void DrawStrategySignalMarkers(bool longSignal, bool shortSignal, string longSource, string shortSource)
        {
            if (longSignal)
            {
                string tag = "GTS_LongSignal_" + CurrentBar;
                Draw.ArrowUp(this, tag, false, 0,
                    Low[0] - StrategyMarkerOffsetTicks * TickSize,
                    LongStrategyMarkerBrush);
                Draw.Text(this, tag + "_Text", longSource, 0,
                    Low[0] - (StrategyMarkerOffsetTicks + 4) * TickSize,
                    LongStrategyMarkerBrush);
            }

            if (shortSignal)
            {
                string tag = "GTS_ShortSignal_" + CurrentBar;
                Draw.ArrowDown(this, tag, false, 0,
                    High[0] + StrategyMarkerOffsetTicks * TickSize,
                    ShortStrategyMarkerBrush);
                Draw.Text(this, tag + "_Text", shortSource, 0,
                    High[0] + (StrategyMarkerOffsetTicks + 4) * TickSize,
                    ShortStrategyMarkerBrush);
            }
        }

        private void DrawStatusPanel()
        {
            if (!ShowStatusPanel)
            {
                RemoveDrawObject("GodTradesStrategy_Status");
                return;
            }

            string workingOrder = HasWorkingEntryOrder()
                ? entryOrder.OrderState + " " + entryOrder.Name
                : "None";

            string status =
                "GodTradesStrategy"
                + "\nPosition: " + Position.MarketPosition + " x " + Position.Quantity
                + "\nSignals: BG " + OnOff(EnableBGTrades)
                + " | FC " + OnOff(EnableFCTrades)
                + " | OBR " + OnOff(EnableOBRTrades)
                + "\nDirection: " + DirectionMode
                + "\nEntry: " + EntryMode + "  Offset: " + EntryOffsetTicks
                + "\nWorking: " + workingOrder
                + "\nLast source: " + lastSignalSource
                + "\nDaily PnL: " + GetDailyPnl().ToString("C2")
                + "\nState: " + (pnlCutoffHit ? "LOCKED" : "ACTIVE")
                + "\nReason: " + lastBlockReason;

            Draw.TextFixed(
                this,
                "GodTradesStrategy_Status",
                status,
                StatusPanelPosition,
                Brushes.White,
                statusFont,
                Brushes.DimGray,
                Brushes.Black,
                70);
        }

        private string OnOff(bool value)
        {
            return value ? "ON" : "OFF";
        }

        #region 01. Strategy Controls

        [NinjaScriptProperty]
        [Display(Name = "Direction Mode", GroupName = "01. Strategy Controls", Order = 1)]
        public GodTradesStrategyDirectionMode DirectionMode { get; set; }

        [NinjaScriptProperty]
        [Range(1, 100)]
        [Display(Name = "Contracts", GroupName = "01. Strategy Controls", Order = 2)]
        public int Contracts { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Entry Mode", GroupName = "01. Strategy Controls", Order = 3)]
        public GodTradesStrategyEntryMode EntryMode { get; set; }

        [NinjaScriptProperty]
        [Range(0, 1000)]
        [Display(Name = "Entry Offset Ticks", Description = "Limit: long below/short above signal close. Stop-market: long above signal high/short below signal low.", GroupName = "01. Strategy Controls", Order = 4)]
        public int EntryOffsetTicks { get; set; }

        [NinjaScriptProperty]
        [Range(0, 100)]
        [Display(Name = "Cancel Unfilled Entry After Bars (0 = Off)", GroupName = "01. Strategy Controls", Order = 5)]
        public int CancelUnfilledEntryAfterBars { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Trade Only When Flat", GroupName = "01. Strategy Controls", Order = 6)]
        public bool TradeOnlyWhenFlat { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Require Account Flat For Instrument", GroupName = "01. Strategy Controls", Order = 7)]
        public bool RequireAccountFlat { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Reverse On Opposite Signal", GroupName = "01. Strategy Controls", Order = 8)]
        public bool ReverseOnOppositeSignal { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Real-Time Only", GroupName = "01. Strategy Controls", Order = 9)]
        public bool RealTimeOnly { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Ignore Conflicting Long/Short Signals", GroupName = "01. Strategy Controls", Order = 10)]
        public bool IgnoreConflictingSignals { get; set; }

        #endregion

        #region 02. Signal Selection

        [NinjaScriptProperty]
        [Display(Name = "Enable BG Trades", Description = "Trade Bollinger Gap signals.", GroupName = "02. Signal Selection", Order = 1)]
        public bool EnableBGTrades { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Enable FC Trades", Description = "Trade continuation/FC signals.", GroupName = "02. Signal Selection", Order = 2)]
        public bool EnableFCTrades { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Enable OBR Trades", Description = "Trade opposite-direction body-engulf OBR signals.", GroupName = "02. Signal Selection", Order = 3)]
        public bool EnableOBRTrades { get; set; }

        #endregion

        #region 03. Profit Target and Stop Loss

        [NinjaScriptProperty]
        [Display(Name = "Use Profit Target", GroupName = "03. Profit Target / Stop Loss", Order = 1)]
        public bool UseProfitTarget { get; set; }

        [NinjaScriptProperty]
        [Range(1, 10000)]
        [Display(Name = "Profit Target Ticks", GroupName = "03. Profit Target / Stop Loss", Order = 2)]
        public int ProfitTargetTicks { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Use Stop Loss", GroupName = "03. Profit Target / Stop Loss", Order = 3)]
        public bool UseStopLoss { get; set; }

        [NinjaScriptProperty]
        [Range(1, 10000)]
        [Display(Name = "Stop Loss Ticks", GroupName = "03. Profit Target / Stop Loss", Order = 4)]
        public int StopLossTicks { get; set; }

        #endregion

        #region 04. Break Even and Trailing

        [NinjaScriptProperty]
        [Display(Name = "Use Break Even", GroupName = "04. Break Even / Trailing", Order = 1)]
        public bool UseBreakEven { get; set; }

        [NinjaScriptProperty]
        [Range(1, 10000)]
        [Display(Name = "Break Even Trigger Ticks", GroupName = "04. Break Even / Trailing", Order = 2)]
        public int BreakEvenTriggerTicks { get; set; }

        [NinjaScriptProperty]
        [Range(-1000, 10000)]
        [Display(Name = "Break Even Plus Ticks", GroupName = "04. Break Even / Trailing", Order = 3)]
        public int BreakEvenPlusTicks { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Use Trailing Stop", GroupName = "04. Break Even / Trailing", Order = 4)]
        public bool UseTrailingStop { get; set; }

        [NinjaScriptProperty]
        [Range(1, 10000)]
        [Display(Name = "Trailing Trigger Ticks", GroupName = "04. Break Even / Trailing", Order = 5)]
        public int TrailingTriggerTicks { get; set; }

        [NinjaScriptProperty]
        [Range(1, 10000)]
        [Display(Name = "Trailing Distance Ticks", GroupName = "04. Break Even / Trailing", Order = 6)]
        public int TrailingDistanceTicks { get; set; }

        [NinjaScriptProperty]
        [Range(1, 10000)]
        [Display(Name = "Trailing Step Ticks", GroupName = "04. Break Even / Trailing", Order = 7)]
        public int TrailingStepTicks { get; set; }

        #endregion

        #region 05. Daily PnL and EOD

        [NinjaScriptProperty]
        [Display(Name = "Use Daily Profit Cutoff", GroupName = "05. Daily PnL / EOD", Order = 1)]
        public bool UseDailyProfitCutoff { get; set; }

        [NinjaScriptProperty]
        [Range(0, double.MaxValue)]
        [Display(Name = "Daily Profit Cutoff ($)", GroupName = "05. Daily PnL / EOD", Order = 2)]
        public double DailyProfitCutoff { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Use Daily Loss Cutoff", GroupName = "05. Daily PnL / EOD", Order = 3)]
        public bool UseDailyLossCutoff { get; set; }

        [NinjaScriptProperty]
        [Range(0, double.MaxValue)]
        [Display(Name = "Daily Loss Cutoff ($)", GroupName = "05. Daily PnL / EOD", Order = 4)]
        public double DailyLossCutoff { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Include Unrealized PnL In Cutoff", GroupName = "05. Daily PnL / EOD", Order = 5)]
        public bool IncludeUnrealizedPnlInCutoff { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Flatten When PnL Cutoff Reached", GroupName = "05. Daily PnL / EOD", Order = 6)]
        public bool FlattenOnPnlCutoff { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Use End Of Day Flatten", GroupName = "05. Daily PnL / EOD", Order = 7)]
        public bool UseEndOfDayFlatten { get; set; }

        [NinjaScriptProperty]
        [Range(0, 235959)]
        [Display(Name = "End Of Day Flatten Time", Description = "HHmmss", GroupName = "05. Daily PnL / EOD", Order = 8)]
        public int EndOfDayFlattenTime { get; set; }

        #endregion

        #region 06. Trading Days

        [NinjaScriptProperty]
        [Display(Name = "Trade Monday", GroupName = "06. Trading Days", Order = 1)]
        public bool TradeMonday { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Trade Tuesday", GroupName = "06. Trading Days", Order = 2)]
        public bool TradeTuesday { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Trade Wednesday", GroupName = "06. Trading Days", Order = 3)]
        public bool TradeWednesday { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Trade Thursday", GroupName = "06. Trading Days", Order = 4)]
        public bool TradeThursday { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Trade Friday", GroupName = "06. Trading Days", Order = 5)]
        public bool TradeFriday { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Trade Saturday", GroupName = "06. Trading Days", Order = 6)]
        public bool TradeSaturday { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Trade Sunday", GroupName = "06. Trading Days", Order = 7)]
        public bool TradeSunday { get; set; }

        #endregion

        #region 07. Trade Windows

        [NinjaScriptProperty]
        [Display(Name = "Use Trade Window 1", GroupName = "07. Trade Windows", Order = 1)]
        public bool UseTradeWindow1 { get; set; }

        [NinjaScriptProperty]
        [Range(0, 235959)]
        [Display(Name = "Trade Window 1 Start", Description = "HHmmss", GroupName = "07. Trade Windows", Order = 2)]
        public int TradeWindow1Start { get; set; }

        [NinjaScriptProperty]
        [Range(0, 235959)]
        [Display(Name = "Trade Window 1 End", Description = "HHmmss", GroupName = "07. Trade Windows", Order = 3)]
        public int TradeWindow1End { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Use Trade Window 2", GroupName = "07. Trade Windows", Order = 4)]
        public bool UseTradeWindow2 { get; set; }

        [NinjaScriptProperty]
        [Range(0, 235959)]
        [Display(Name = "Trade Window 2 Start", Description = "HHmmss", GroupName = "07. Trade Windows", Order = 5)]
        public int TradeWindow2Start { get; set; }

        [NinjaScriptProperty]
        [Range(0, 235959)]
        [Display(Name = "Trade Window 2 End", Description = "HHmmss", GroupName = "07. Trade Windows", Order = 6)]
        public int TradeWindow2End { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Use Trade Window 3", GroupName = "07. Trade Windows", Order = 7)]
        public bool UseTradeWindow3 { get; set; }

        [NinjaScriptProperty]
        [Range(0, 235959)]
        [Display(Name = "Trade Window 3 Start", Description = "HHmmss", GroupName = "07. Trade Windows", Order = 8)]
        public int TradeWindow3Start { get; set; }

        [NinjaScriptProperty]
        [Range(0, 235959)]
        [Display(Name = "Trade Window 3 End", Description = "HHmmss", GroupName = "07. Trade Windows", Order = 9)]
        public int TradeWindow3End { get; set; }

        #endregion

        #region 08. Skip Windows

        [NinjaScriptProperty]
        [Display(Name = "Use Skip Window 1", GroupName = "08. Skip Windows", Order = 1)]
        public bool UseSkipWindow1 { get; set; }

        [NinjaScriptProperty]
        [Range(0, 235959)]
        [Display(Name = "Skip Window 1 Start", Description = "HHmmss", GroupName = "08. Skip Windows", Order = 2)]
        public int SkipWindow1Start { get; set; }

        [NinjaScriptProperty]
        [Range(0, 235959)]
        [Display(Name = "Skip Window 1 End", Description = "HHmmss", GroupName = "08. Skip Windows", Order = 3)]
        public int SkipWindow1End { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Use Skip Window 2", GroupName = "08. Skip Windows", Order = 4)]
        public bool UseSkipWindow2 { get; set; }

        [NinjaScriptProperty]
        [Range(0, 235959)]
        [Display(Name = "Skip Window 2 Start", Description = "HHmmss", GroupName = "08. Skip Windows", Order = 5)]
        public int SkipWindow2Start { get; set; }

        [NinjaScriptProperty]
        [Range(0, 235959)]
        [Display(Name = "Skip Window 2 End", Description = "HHmmss", GroupName = "08. Skip Windows", Order = 6)]
        public int SkipWindow2End { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Use Skip Window 3", GroupName = "08. Skip Windows", Order = 7)]
        public bool UseSkipWindow3 { get; set; }

        [NinjaScriptProperty]
        [Range(0, 235959)]
        [Display(Name = "Skip Window 3 Start", Description = "HHmmss", GroupName = "08. Skip Windows", Order = 8)]
        public int SkipWindow3Start { get; set; }

        [NinjaScriptProperty]
        [Range(0, 235959)]
        [Display(Name = "Skip Window 3 End", Description = "HHmmss", GroupName = "08. Skip Windows", Order = 9)]
        public int SkipWindow3End { get; set; }

        #endregion

        #region 09. Display

        [NinjaScriptProperty]
        [Display(Name = "Show GodTrades21 On Chart", GroupName = "09. Display", Order = 1)]
        public bool ShowUnderlyingIndicatorOnChart { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Show Strategy Signal Markers", GroupName = "09. Display", Order = 2)]
        public bool ShowStrategySignalMarkers { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Show Status Panel", GroupName = "09. Display", Order = 3)]
        public bool ShowStatusPanel { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Status Panel Position", GroupName = "09. Display", Order = 4)]
        public TextPosition StatusPanelPosition { get; set; }

        [NinjaScriptProperty]
        [Range(8, 40)]
        [Display(Name = "Status Panel Font Size", GroupName = "09. Display", Order = 5)]
        public int StatusPanelFontSize { get; set; }

        [XmlIgnore]
        [Display(Name = "Long Strategy Marker Brush", GroupName = "09. Display", Order = 6)]
        public Brush LongStrategyMarkerBrush { get; set; }

        [Browsable(false)]
        public string LongStrategyMarkerBrushSerializable
        {
            get { return Serialize.BrushToString(LongStrategyMarkerBrush); }
            set { LongStrategyMarkerBrush = Serialize.StringToBrush(value); }
        }

        [XmlIgnore]
        [Display(Name = "Short Strategy Marker Brush", GroupName = "09. Display", Order = 7)]
        public Brush ShortStrategyMarkerBrush { get; set; }

        [Browsable(false)]
        public string ShortStrategyMarkerBrushSerializable
        {
            get { return Serialize.BrushToString(ShortStrategyMarkerBrush); }
            set { ShortStrategyMarkerBrush = Serialize.StringToBrush(value); }
        }

        [NinjaScriptProperty]
        [Range(0, 100)]
        [Display(Name = "Strategy Marker Offset Ticks", GroupName = "09. Display", Order = 8)]
        public int StrategyMarkerOffsetTicks { get; set; }

        #endregion

        #region 10. GodTrades Gap and FC Settings

        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Minimum Gap Size Ticks", GroupName = "10. GodTrades Gap / FC", Order = 1)]
        public int MinimumGapSizeTicks { get; set; }

        [NinjaScriptProperty]
        [Range(0, int.MaxValue)]
        [Display(Name = "Minimum Bars Before Valid", GroupName = "10. GodTrades Gap / FC", Order = 2)]
        public int MinimumBarsBeforeValid { get; set; }

        [NinjaScriptProperty]
        [Range(0, int.MaxValue)]
        [Display(Name = "Minimum Body Ticks", GroupName = "10. GodTrades Gap / FC", Order = 3)]
        public int MinimumBodyTicks { get; set; }

        [NinjaScriptProperty]
        [Range(0, int.MaxValue)]
        [Display(Name = "Maximum Gap Bar Range Ticks", GroupName = "10. GodTrades Gap / FC", Order = 4)]
        public int MaximumGapBarRangeTicks { get; set; }

        [NinjaScriptProperty]
        [Range(1, 3000)]
        [Display(Name = "Maximum Active Gaps To Track", GroupName = "10. GodTrades Gap / FC", Order = 5)]
        public int MaximumActiveGapsToTrack { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Early Touch Handling", GroupName = "10. GodTrades Gap / FC", Order = 6)]
        public GodTrades21EarlyTouchHandling EarlyTouchHandling { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Valid Touch Behavior", GroupName = "10. GodTrades Gap / FC", Order = 7)]
        public GodTrades21ValidTouchBehavior ValidTouchBehavior { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Use Bollinger Midpoint Filter For FC", GroupName = "10. GodTrades Gap / FC", Order = 8)]
        public bool UseBollingerMidpointFilterForContinuation { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "FC Bollinger Location Source", GroupName = "10. GodTrades Gap / FC", Order = 9)]
        public GodTrades21FcBollingerLocationSource FcBollingerLocationSource { get; set; }

        [NinjaScriptProperty]
        [Range(0.0, 100.0)]
        [Display(Name = "FC Long Below Midpoint Percent", GroupName = "10. GodTrades Gap / FC", Order = 10)]
        public double FcLongBelowMidpointPercent { get; set; }

        [NinjaScriptProperty]
        [Range(0.0, 100.0)]
        [Display(Name = "FC Short Above Midpoint Percent", GroupName = "10. GodTrades Gap / FC", Order = 11)]
        public double FcShortAboveMidpointPercent { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "FC Confirmation Mode", GroupName = "10. GodTrades Gap / FC", Order = 12)]
        public GodTrades21ContinuationConfirmationMode ContinuationConfirmationMode { get; set; }

        [NinjaScriptProperty]
        [Range(0, 20)]
        [Display(Name = "Confirmation Bars After Touch", GroupName = "10. GodTrades Gap / FC", Order = 13)]
        public int ConfirmationBarsAfterTouch { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Require FC Signal Candle Direction", GroupName = "10. GodTrades Gap / FC", Order = 14)]
        public bool RequireSignalCandleDirection { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Require Correct FC Approach", GroupName = "10. GodTrades Gap / FC", Order = 15)]
        public bool RequireCorrectContinuationApproach { get; set; }

        #endregion

        #region 11. GodTrades OBR Settings

        [NinjaScriptProperty]
        [Display(Name = "Use Bollinger Midpoint Filter For OBR", GroupName = "11. GodTrades OBR", Order = 1)]
        public bool UseBollingerMidpointFilterForOutsideBarReversal { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Allow OBR Bar Outside Bollinger Band", GroupName = "11. GodTrades OBR", Order = 2)]
        public bool AllowObrBarOutsideBollingerBand { get; set; }

        [NinjaScriptProperty]
        [Range(0, int.MaxValue)]
        [Display(Name = "Bearish OBR Upper Band Tolerance Ticks", GroupName = "11. GodTrades OBR", Order = 3)]
        public int BearishObrUpperBandTouchToleranceTicks { get; set; }

        [NinjaScriptProperty]
        [Range(0, int.MaxValue)]
        [Display(Name = "Bullish OBR Lower Band Tolerance Ticks", GroupName = "11. GodTrades OBR", Order = 4)]
        public int BullishObrLowerBandTouchToleranceTicks { get; set; }

        #endregion

        #region 12. GodTrades Bollinger and Signal Time

        [NinjaScriptProperty]
        [Display(Name = "Use Indicator Signal Time Filter", GroupName = "12. GodTrades Bollinger / Time", Order = 1)]
        public bool UseIndicatorSignalTimeFilter { get; set; }

        [NinjaScriptProperty]
        [Range(0, 235959)]
        [Display(Name = "Indicator Signal Start Time", Description = "HHmmss", GroupName = "12. GodTrades Bollinger / Time", Order = 2)]
        public int IndicatorSignalStartTime { get; set; }

        [NinjaScriptProperty]
        [Range(0, 235959)]
        [Display(Name = "Indicator Signal End Time", Description = "HHmmss", GroupName = "12. GodTrades Bollinger / Time", Order = 3)]
        public int IndicatorSignalEndTime { get; set; }

        [NinjaScriptProperty]
        [Range(2, int.MaxValue)]
        [Display(Name = "Bollinger Period", GroupName = "12. GodTrades Bollinger / Time", Order = 4)]
        public int BollingerPeriod { get; set; }

        [NinjaScriptProperty]
        [Range(0.1, double.MaxValue)]
        [Display(Name = "Bollinger Std Dev", GroupName = "12. GodTrades Bollinger / Time", Order = 5)]
        public double BollingerStdDev { get; set; }

        [NinjaScriptProperty]
        [Range(0, int.MaxValue)]
        [Display(Name = "Bollinger Band Proximity Ticks", GroupName = "12. GodTrades Bollinger / Time", Order = 6)]
        public int BollingerBandProximityTicks { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Enable Spiderweb Warning", GroupName = "12. GodTrades Bollinger / Time", Order = 7)]
        public bool EnableSpiderwebWarning { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Show Spiderweb Warning Text", GroupName = "12. GodTrades Bollinger / Time", Order = 8)]
        public bool ShowSpiderwebWarningText { get; set; }

        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Spiderweb Distance Ticks", GroupName = "12. GodTrades Bollinger / Time", Order = 9)]
        public int SpiderwebDistanceTicks { get; set; }

        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Spiderweb Line Count", GroupName = "12. GodTrades Bollinger / Time", Order = 10)]
        public int SpiderwebLineCount { get; set; }

        [NinjaScriptProperty]
        [Range(6, 60)]
        [Display(Name = "Spiderweb Text Font Size", GroupName = "12. GodTrades Bollinger / Time", Order = 11)]
        public int SpiderwebTextFontSize { get; set; }

        #endregion
    }
}
