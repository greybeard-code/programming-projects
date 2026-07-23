#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using NinjaTrader.Cbi;
using NinjaTrader.Data;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Tools;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.DrawingTools;
using NinjaTrader.NinjaScript.Indicators;
#endregion

// gbZeus — native GreyBeard executor for the GodTrades gap system.
//
// Replaces the PredatorX template. PredatorX cannot make a target track the
// OPPOSITE Bollinger band re-priced every bar, so it was stuck with fixed-tick
// targets — the losing regime. This strategy owns its exits:
//   * entry:  GodTrades indicator SignalCode (+/-1 BG, +/-2 FC), window-gated;
//             fills next bar open, one position at a time.
//   * stop:   back of the signal candle (static; never auto-moved to BE).
//   * target: opposite Bollinger band, re-priced each bar (BandTarget mode).
//
// REQUIRES the GodTrades indicator, version 16.6 — it is the signal source.
// The indicator is third-party work, redistributed UNMODIFIED with the author's
// permission. Credit: <INDICATOR AUTHOR — fill in before distributing>.
// Do not edit GodTrades.cs; keep its Bollinger settings identical to the ones
// configured below.
//
// VERSION COUPLING (important): this strategy calls the indicator through the
// NinjaScript-generated constructor, whose argument list is built from the
// indicator's [NinjaScriptProperty] fields. That signature is pinned to v16.6.
// If the indicator author adds, removes, or reorders any NinjaScriptProperty in
// a later build, THIS STRATEGY WILL NO LONGER COMPILE until the call in
// State.DataLoaded is updated to match. Ship the two files together and do not
// mix indicator versions.
//
// ---------------------------------------------------------------------------
// CHANGELOG
// v1.1.0  Release hardening. OnBarUpdate wrapped in try/catch so a runtime
//         error can no longer strand an open position. The indicator's gap /
//         FC-confirmation / spiderweb parameters are now user-settable instead
//         of hard-coded into the call (needed for any chart other than the NQ
//         1000-tick design point). Added a startup configuration log with
//         warnings when the chart doesn't match that design point or when no
//         daily loss limit is set.
// v1.0.2  Session filter reworked to the GreyBeard standard: Enable checkbox +
//         DateTime time pickers (TimeEditorKey) instead of raw int HHmmss.
// v1.0.1  Risk guardrails: daily max loss / max profit (realized, per session),
//         flatten-on-limit, max trades per day, and a max-stop-ticks cap that
//         skips signals whose candle-back stop would be oversized.
// v1.0.0  Signal->entry->faithful exits, plus the standard GreyBeard dashboard
//         & control-button suite (AUTO/LONG/SHORT, MOVE SL TO BE, SL/TP nudges,
//         FLATTEN ALL). Manual commands use the house pattern: UI-thread click
//         handlers only set volatile flags; the strategy thread drains them in
//         ProcessDashboardCommands from OnMarketData. (No REV button — the
//         Terminator_V2 reference suite doesn't wire one either.)
// ---------------------------------------------------------------------------
//
// Defaults are tuned for NQ on a 1000-tick chart, 10:15-15:00 ET. Tick-based
// settings (band proximity, gap size, spiderweb distance, stop caps) do NOT
// carry over to other instruments or bar types without review.

namespace NinjaTrader.NinjaScript.Strategies.GreyBeard
{
	public enum ZeusExitMode
	{
		BandTarget,     // methodology: opposite Bollinger band, re-priced each bar
		FixedTicks      // A/B only: fixed stop/target in ticks
	}

	public enum ZeusPanelCorner { TopLeft, TopRight, BottomLeft, BottomRight }

	public class gbZeus : Strategy
	{
		private const string VERSION = "1.1.0";

		private Indicators.GodTrades	gt;         // signal source (single source of truth)
		private Bollinger				bb;         // live bands for the moving target

		// live bracket state (also drives the dashboard + manual overrides)
		private string	currentSignalName = "";
		private double	currentStopPrice;
		private double	currentTargetPrice;
		private bool	_targetManuallyMoved;       // user overrode target -> stop auto band-tracking
		private bool	_stopManuallyMoved;
		private double	_lastTradePrice;

		// ---- Phase 2: daily risk (realized, this strategy's closed trades) ----
		private double			sessionStartCumProfit;
		private double			dailyPnl;
		private volatile bool	tradingBlocked;
		private int				dailyEntryCount;
		private int				_onBarErrors;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Name							= "gbZeus";
				Description						= "GodTrades gap system — native GreyBeard executor. Reads the GodTrades indicator's SignalCode and applies methodology-faithful exits (candle-back stop + dynamic opposite-Bollinger-band target, no break-even). Full dashboard + control panel.";
				Calculate						= Calculate.OnBarClose;
				EntriesPerDirection				= 1;
				EntryHandling					= EntryHandling.AllEntries;
				IsExitOnSessionCloseStrategy	= true;
				ExitOnSessionCloseSeconds		= 30;
				IsFillLimitOnTouch				= false;
				MaximumBarsLookBack				= MaximumBarsLookBack.TwoHundredFiftySix;
				OrderFillResolution				= OrderFillResolution.Standard;
				Slippage						= 0;
				StartBehavior					= StartBehavior.WaitUntilFlat;
				TimeInForce						= TimeInForce.Gtc;
				TraceOrders						= false;
				RealtimeErrorHandling			= RealtimeErrorHandling.StopCancelClose;
				StopTargetHandling				= StopTargetHandling.PerEntryExecution;   // GreyBeard house pattern
				BarsRequiredToTrade				= 20;
				IsInstantiatedOnEachOptimizationIteration = true;

				// ---- inputs / defaults ----
				Quantity						= 1;

				EnableBollingerGap				= true;
				EnableContinuation				= true;
				BollingerPeriod					= 20;
				BollingerStdDev					= 2.0;
				BollingerBandProximityTicks		= 8;
				MinimumBarsBeforeValid			= 3;
				ConfirmationBarsAfterTouch		= 2;
				MinimumGapSizeTicks				= 1;         // raise on minute charts (clock-boundary micro-gaps)
				MinimumBodyTicks				= 4;         // BG doji guard
				ContinuationConfirmation		= GodTradesContinuationConfirmationMode.RequireCloseBeyondFullZone;
				RequireSignalCandleDirection	= true;
				RequireCorrectApproach			= true;
				UseFcMidpointFilter				= true;
				FcLongBelowMidPct				= 50.0;
				FcShortAboveMidPct				= 50.0;

				SpiderwebDistanceTicks			= 100;
				SpiderwebLineCount				= 5;

				EnableSession					= true;      // restrict entries to the window below (set chart TZ to Eastern)
				SessionStart					= DateTime.Parse("10:15", System.Globalization.CultureInfo.InvariantCulture);
				SessionEnd						= DateTime.Parse("15:00", System.Globalization.CultureInfo.InvariantCulture);
				FlattenAtWindowEnd				= true;

				ExitModeInput					= ZeusExitMode.BandTarget;
				StopOffsetTicks					= 0;
				FixedStopTicks					= 50;
				FixedTargetTicks				= 120;

				SpiderwebSuppress				= true;

				ManualNudgeTicks				= 4;
				ManualBeOffsetTicks				= 0;

				ShowDashboard					= true;
				DashboardCorner					= ZeusPanelCorner.TopLeft;
				DashboardStartMinimized			= false;

				UseDailyLoss					= false;
				DailyLoss						= 500;
				UseDailyProfit					= false;
				DailyProfit						= 500;
				DailyFlatten					= true;
				MaxTradesPerDay					= 0;
				MaxStopTicks					= 0;
			}
			else if (State == State.DataLoaded)
			{
				// Host the GodTrades indicator for signals. UseSignalTimeFilter is OFF
				// (the STRATEGY owns the entry window); draw toggles OFF (add the
				// indicator to the chart separately if you want the arrows/lines).
				gt = GodTrades(
					MinimumGapSizeTicks,                                    // MinimumGapSizeTicks
					MinimumBarsBeforeValid,                                 // MinimumBarsBeforeValid
					MinimumBodyTicks,                                       // MinimumBodyTicks (BG doji filter)
					0,                                                      // MaximumGapBarRangeTicks (ignored: indicator's ATR filter is on)
					300,                                                    // MaximumActiveGapsToTrack (performance guard)
					GodTradesEarlyTouchHandling.StopLineImmediately,
					GodTradesValidTouchBehavior.StopLineAndMarkContinuation,
					EnableContinuation,                                     // EnableContinuationSignals
					UseFcMidpointFilter,                                    // UseBollingerMidpointFilterForContinuation
					GodTradesFcBollingerLocationSource.WickExtreme,
					FcLongBelowMidPct,                                      // FcLongBelowMidpointPercent
					FcShortAboveMidPct,                                     // FcShortAboveMidpointPercent
					ContinuationConfirmation,                               // ContinuationConfirmationMode
					ConfirmationBarsAfterTouch,
					RequireSignalCandleDirection,
					RequireCorrectApproach,
					false,                                                  // UseSignalTimeFilter (strategy owns it)
					101500,                                                 // SignalStartTime
					150000,                                                 // SignalEndTime
					EnableBollingerGap,                                     // EnableBollingerGapSignals
					BollingerPeriod,
					BollingerStdDev,
					BollingerBandProximityTicks,
					true,                                                   // EnableSpiderwebWarning
					false,                                                  // ShowSpiderwebWarningText
					SpiderwebDistanceTicks,
					SpiderwebLineCount,
					15,                                                     // SpiderwebTextFontSize
					StopOffsetTicks,                                        // SuggestedStopOffsetTicks
					GodTradesTargetMode.OppositeBollingerBand,
					40,                                                     // FixedTargetTicks (indicator's; unused here)
					GodTradesLinePriceMode.Midpoint,
					false, false, false, false, false, false,               // Show* draw toggles
					false,                                                  // UseTouchedLineColor
					2,                                                      // GapLineWidth
					DashStyleHelper.Solid,
					12,                                                     // ZoneOpacity
					6,                                                      // SignalMarkerOffsetTicks
					20);                                                    // SignalLabelOffsetTicks

				bb = Bollinger(BollingerStdDev, BollingerPeriod);
			}
			else if (State == State.Realtime)
			{
				sessionStartCumProfit = SystemPerformance.AllTrades.TradesPerformance.Currency.CumProfit;
				LogStartupConfig();
				if (ShowDashboard)
					CreateDashboard();
			}
			else if (State == State.Terminated)
			{
				RemoveDashboard();
			}
		}

		// A runtime error must never kill the strategy while a position is open —
		// NT8 disables the script on an unhandled exception, stranding the trade.
		protected override void OnBarUpdate()
		{
			try { ProcessBarUpdate(); }
			catch (Exception ex)
			{
				if (_onBarErrors++ < 5)
					Print("[gbZeus] OnBarUpdate error: " + ex.GetType().Name + " " + ex.Message);
			}
		}

		private void ProcessBarUpdate()
		{
			if (BarsInProgress != 0)
				return;
			if (State == State.Realtime)
				ProcessDashboardCommands();   // also drained per-tick in OnMarketData
			if (CurrentBar < Math.Max(BarsRequiredToTrade, BollingerPeriod + 1))
				return;

			// ---------------- daily session reset + risk ----------------
			if (Bars.IsFirstBarOfSession)
			{
				sessionStartCumProfit = SystemPerformance.AllTrades.TradesPerformance.Currency.CumProfit;
				tradingBlocked = false;
				dailyEntryCount = 0;
			}
			ComputeDayPnL();
			UpdateDailyRisk();

			// ---------------- manage an open position ----------------
			if (Position.MarketPosition != MarketPosition.Flat)
			{
				// re-price the moving band target (unless the user manually moved it)
				if (currentSignalName.Length > 0 && ExitModeInput == ZeusExitMode.BandTarget
					&& !_targetManuallyMoved)
				{
					if (Position.MarketPosition == MarketPosition.Long)
					{
						double band = bb.Upper[0];
						if (band > Close[0]) { SetProfitTarget(currentSignalName, CalculationMode.Price, band); currentTargetPrice = band; }
						else ExitLong("ZeusBandHit", currentSignalName);
					}
					else
					{
						double band = bb.Lower[0];
						if (band < Close[0]) { SetProfitTarget(currentSignalName, CalculationMode.Price, band); currentTargetPrice = band; }
						else ExitShort("ZeusBandHit", currentSignalName);
					}
				}

				if (FlattenAtWindowEnd && !InEntryWindow())
				{
					if (Position.MarketPosition == MarketPosition.Long)  ExitLong("ZeusFlat", currentSignalName);
					else                                                 ExitShort("ZeusFlat", currentSignalName);
				}

				UpdateDashboard();
				return;   // one position at a time; ignore opposite signals while in a trade
			}

			// ---------------- flat: look for a new entry ----------------
			if (_autoEnabled && !tradingBlocked && InEntryWindow()
				&& (MaxTradesPerDay <= 0 || dailyEntryCount < MaxTradesPerDay)
				&& !(SpiderwebSuppress && gt.SpiderwebWarning[0] == 1))
			{
				double code = gt.SignalCode[0];
				if (code != 0)
				{
					int dir = code > 0 ? 1 : -1;
					if (((dir > 0 && _longEnabled) || (dir < 0 && _shortEnabled)) && PassesStopRisk(dir))
					{
						SubmitEntry(dir);
						dailyEntryCount++;
					}
				}
			}

			UpdateDashboard();
		}

		private void SubmitEntry(int dir)
		{
			double tick = TickSize;
			double stopPx, tgtPx;

			if (ExitModeInput == ZeusExitMode.BandTarget)
			{
				stopPx = dir > 0 ? Low[0]  - StopOffsetTicks * tick
								 : High[0] + StopOffsetTicks * tick;
				tgtPx  = dir > 0 ? bb.Upper[0] : bb.Lower[0];
				if (dir > 0 && tgtPx <= Close[0]) tgtPx = Close[0] + FixedTargetTicks * tick;   // fallback: band on wrong side
				if (dir < 0 && tgtPx >= Close[0]) tgtPx = Close[0] - FixedTargetTicks * tick;
			}
			else
			{
				stopPx = dir > 0 ? Close[0] - FixedStopTicks   * tick : Close[0] + FixedStopTicks   * tick;
				tgtPx  = dir > 0 ? Close[0] + FixedTargetTicks * tick : Close[0] - FixedTargetTicks * tick;
			}

			string sig = dir > 0 ? "ZeusLong" : "ZeusShort";
			SetStopLoss(sig, CalculationMode.Price, stopPx, false);   // static candle-back stop; never auto-moved to BE
			SetProfitTarget(sig, CalculationMode.Price, tgtPx);
			currentSignalName		= sig;
			currentStopPrice		= stopPx;
			currentTargetPrice		= tgtPx;
			_targetManuallyMoved	= false;
			_stopManuallyMoved		= false;

			if (dir > 0) EnterLong(Quantity, sig);
			else         EnterShort(Quantity, sig);
		}

		protected override void OnMarketData(MarketDataEventArgs marketDataUpdate)
		{
			try
			{
				if (State != State.Realtime || BarsInProgress != 0) return;
				if (marketDataUpdate.MarketDataType != MarketDataType.Last) return;
				if (marketDataUpdate.Price > 0) _lastTradePrice = marketDataUpdate.Price;
				if (CurrentBars == null || CurrentBars.Length == 0 || CurrentBars[0] < 2) return;

				ProcessDashboardCommands();   // manual commands act immediately, not at next bar close
				ComputeDayPnL();
				UpdateDailyRisk();
				UpdateDashboard();
			}
			catch { /* never let a tick throw into NT8 */ }
		}

		protected override void OnExecutionUpdate(Execution execution, string executionId,
			double price, int quantity, MarketPosition marketPosition, string orderId, DateTime time)
		{
			if (Position.MarketPosition == MarketPosition.Flat)
			{
				currentSignalName		= "";
				currentStopPrice		= 0.0;
				currentTargetPrice		= 0.0;
				_targetManuallyMoved	= false;
				_stopManuallyMoved		= false;
			}
			UpdateDashboard(true);
		}

		private bool InEntryWindow()
		{
			if (!EnableSession) return true;   // filter off -> trade the whole session
			int t = ToTime(Time[0]);
			int s = ToTime(SessionStart);
			int e = ToTime(SessionEnd);
			if (s <= e)
				return t >= s && t < e;
			return t >= s || t < e;            // window wraps midnight
		}

		private double MarkPrice()
		{
			if (_lastTradePrice > 0) return _lastTradePrice;
			return Close[0];
		}

		// Startup banner — makes the live configuration visible in the Output window
		// and warns when the chart or the risk settings depart from the design point
		// (NQ, 1000-tick, 10:15-15:00 ET). Realtime only, so it never spams a backtest.
		private void LogStartupConfig()
		{
			try
			{
				string instr = Instrument != null && Instrument.MasterInstrument != null
					? Instrument.MasterInstrument.Name : "?";
				string bars = BarsPeriod != null
					? BarsPeriod.BarsPeriodType + " " + BarsPeriod.Value : "?";

				Print("---------------------------------------------------------------");
				Print("[gbZeus] v" + VERSION + "   " + instr + "   " + bars
					+ "   acct " + (Account != null ? Account.Name : "?"));
				Print("[gbZeus] Signals: BG=" + EnableBollingerGap + "  FC=" + EnableContinuation
					+ "   Bollinger " + BollingerPeriod + "/" + BollingerStdDev
					+ "   band proximity " + BollingerBandProximityTicks + "t");
				Print("[gbZeus] Exit: " + ExitModeInput + "   Qty " + Quantity + "   Window "
					+ (EnableSession ? SessionStart.ToString("HH:mm") + "-" + SessionEnd.ToString("HH:mm")
									 : "always on"));
				Print("[gbZeus] Risk: DailyLoss " + (UseDailyLoss ? "$" + DailyLoss : "OFF")
					+ "   DailyProfit " + (UseDailyProfit ? "$" + DailyProfit : "OFF")
					+ "   MaxTrades/day " + (MaxTradesPerDay > 0 ? MaxTradesPerDay.ToString() : "off")
					+ "   MaxStopTicks " + (MaxStopTicks > 0 ? MaxStopTicks.ToString() : "off"));

				bool tickChart = BarsPeriod != null && BarsPeriod.BarsPeriodType == BarsPeriodType.Tick;
				if (!tickChart || BarsPeriod.Value != 1000)
					Print("[gbZeus] WARNING: designed for a 1000-tick chart. Tick-based settings "
						+ "(gap size, band proximity, spiderweb distance, stop caps) need review here.");
				if (Math.Abs(TickSize - 0.25) > 1e-9)
					Print("[gbZeus] WARNING: tick size " + TickSize
						+ " differs from NQ (0.25) — re-check every tick-based setting.");
				if (!UseDailyLoss)
					Print("[gbZeus] WARNING: no daily loss limit set — a run of stops is "
						+ "unbounded for the session.");
				if (MaxStopTicks <= 0 && ExitModeInput == ZeusExitMode.BandTarget)
					Print("[gbZeus] NOTE: Max Stop Ticks is off — a large signal candle can "
						+ "produce an oversized per-trade stop.");
				Print("---------------------------------------------------------------");
			}
			catch { /* logging must never throw */ }
		}

		#region Phase 2: daily risk + per-trade stop cap
		// Realized P&L for THIS strategy's closed trades since the session start
		// (SystemPerformance = closed trades only; intraday unrealized is not counted).
		private void ComputeDayPnL()
		{
			dailyPnl = SystemPerformance.AllTrades.TradesPerformance.Currency.CumProfit - sessionStartCumProfit;
		}

		private void UpdateDailyRisk()
		{
			if (UseDailyLoss   && DailyLoss   > 0 && dailyPnl <= -DailyLoss)  tradingBlocked = true;
			if (UseDailyProfit && DailyProfit > 0 && dailyPnl >=  DailyProfit) tradingBlocked = true;
			if (tradingBlocked && DailyFlatten && Position.MarketPosition != MarketPosition.Flat)
				FlattenAll();
		}

		// Skip a signal whose candle-back stop would risk more than MaxStopTicks
		// (bounds per-trade risk; the taken trades keep the true candle-back stop).
		private bool PassesStopRisk(int dir)
		{
			if (MaxStopTicks <= 0 || ExitModeInput != ZeusExitMode.BandTarget) return true;
			double tick = TickSize;
			double stopPx = dir > 0 ? Low[0] - StopOffsetTicks * tick : High[0] + StopOffsetTicks * tick;
			return Math.Abs(Close[0] - stopPx) / tick <= MaxStopTicks;
		}
		#endregion

		#region Manual command processing (strategy thread)
		// The UI thread only sets the volatile flags below; the real order work happens here.
		private void ProcessDashboardCommands()
		{
			if (_pendingFlatten)
			{
				_pendingFlatten = false;
				if (Position.MarketPosition != MarketPosition.Flat) FlattenAll();
				UpdateDashboard(true);
			}
			if (_pendingBE)   // BE wins over a same-window SL nudge; TP nudge still applies
			{
				_pendingBE = false;
				System.Threading.Interlocked.Exchange(ref _pendingStopNudgeTicks, 0);
				if (Position.MarketPosition != MarketPosition.Flat) MoveToBreakeven();
				UpdateDashboard(true);
			}
			DrainManualNudges();
		}

		private void DrainManualNudges()
		{
			int sn = System.Threading.Interlocked.Exchange(ref _pendingStopNudgeTicks, 0);
			int tn = System.Threading.Interlocked.Exchange(ref _pendingTargetNudgeTicks, 0);
			if (Position.MarketPosition == MarketPosition.Flat || currentSignalName.Length == 0) return;
			if (sn != 0) ApplyManualStop((currentStopPrice   > 0 ? currentStopPrice   : Position.AveragePrice) + sn * TickSize);
			if (tn != 0) ApplyManualTarget((currentTargetPrice > 0 ? currentTargetPrice : Position.AveragePrice) + tn * TickSize);
		}

		private void FlattenAll()
		{
			string sig = currentSignalName.Length > 0 ? currentSignalName
					   : (Position.MarketPosition == MarketPosition.Long ? "ZeusLong" : "ZeusShort");
			if (Position.MarketPosition == MarketPosition.Long)       ExitLong("ZeusFlat", sig);
			else if (Position.MarketPosition == MarketPosition.Short) ExitShort("ZeusFlat", sig);
		}

		private void MoveToBreakeven()
		{
			if (currentSignalName.Length == 0) return;
			bool isLong = Position.MarketPosition == MarketPosition.Long;
			double be = Position.AveragePrice + (isLong ? 1 : -1) * ManualBeOffsetTicks * TickSize;
			ApplyManualStop(be);
		}

		private void ApplyManualStop(double px)
		{
			if (currentSignalName.Length == 0 || Position.MarketPosition == MarketPosition.Flat) return;
			bool isLong = Position.MarketPosition == MarketPosition.Long;
			double mark = MarkPrice();
			px = Instrument.MasterInstrument.RoundToTickSize(px);
			if (isLong  && px >= mark) { Print("[gbZeus] SL move skipped: long stop >= market"); return; }
			if (!isLong && px <= mark) { Print("[gbZeus] SL move skipped: short stop <= market"); return; }
			SetStopLoss(currentSignalName, CalculationMode.Price, px, false);
			currentStopPrice = px;
			_stopManuallyMoved = true;
		}

		private void ApplyManualTarget(double px)
		{
			if (currentSignalName.Length == 0 || Position.MarketPosition == MarketPosition.Flat) return;
			bool isLong = Position.MarketPosition == MarketPosition.Long;
			double mark = MarkPrice();
			px = Instrument.MasterInstrument.RoundToTickSize(px);
			if (isLong  && px <= mark) { Print("[gbZeus] TP move skipped: long target <= market"); return; }
			if (!isLong && px >= mark) { Print("[gbZeus] TP move skipped: short target >= market"); return; }
			SetProfitTarget(currentSignalName, CalculationMode.Price, px);
			currentTargetPrice = px;
			_targetManuallyMoved = true;   // stop auto band-tracking for this trade
		}
		#endregion

		#region Dashboard fields + palette
		private Border _dashPanel, _dashTitleBar;
		private StackPanel _dashBody, _dashTradeInfo;
		private Thumb _dragThumb;
		private System.Windows.Shapes.Path _pillPath;
		private Border _pillBtn;
		private TextBlock _dashStatus, _dashInstrument, _dashWindow, _dashWindowState, _dashState, _dashDaily;
		private TextBlock _dashEntry, _dashStop, _dashTarget, _dashQty, _dashPnl;
		private Button _autoBtn, _longBtn, _shortBtn, _beBtn, _flattenBtn;
		private Button _slDownBtn, _slUpBtn, _tpDownBtn, _tpUpBtn;
		private StackPanel _nudgeRow;
		private bool _dashMinimized, _uiInitialized;
		private volatile bool _dashTornDown;
		private DateTime _lastDashPushUtc = DateTime.MinValue;
		private const int DASH_PUSH_MIN_MS = 150;
		private volatile bool _dashPushInFlight;

		private volatile bool _autoEnabled  = true;
		private volatile bool _longEnabled  = true;
		private volatile bool _shortEnabled = true;
		private volatile bool _pendingFlatten;
		private volatile bool _pendingBE;
		private int _pendingStopNudgeTicks;     // UI thread accumulates via Interlocked; strategy thread drains
		private int _pendingTargetNudgeTicks;

		private static readonly SolidColorBrush DashBg      = MakeFrozen(0xF0, 0x12, 0x16, 0x14);
		private static readonly SolidColorBrush DashBorder  = MakeFrozen(0xFF, 0x2A, 0x4A, 0x3C);
		private static readonly SolidColorBrush DashTitleBg = MakeFrozen(0xFF, 0x12, 0x22, 0x1C);
		private static readonly SolidColorBrush DashTitleFg = MakeFrozen(0xFF, 0x53, 0xD9, 0x9A);
		private static readonly SolidColorBrush DashDimFg   = MakeFrozen(0xFF, 0x8A, 0xA6, 0x9A);
		private static readonly SolidColorBrush DashSep     = MakeFrozen(0xFF, 0x24, 0x33, 0x2C);

		private static readonly SolidColorBrush BtnInactBg  = MakeFrozen(0xFF, 0x1C, 0x24, 0x20);
		private static readonly SolidColorBrush BtnInactBdr = MakeFrozen(0xFF, 0x33, 0x4A, 0x42);
		private static readonly SolidColorBrush BtnLongBg   = MakeFrozen(0xFF, 0x0D, 0x30, 0x1A);
		private static readonly SolidColorBrush BtnLongBdr  = MakeFrozen(0xFF, 0x28, 0xC8, 0x60);
		private static readonly SolidColorBrush BtnShortBg  = MakeFrozen(0xFF, 0x30, 0x0D, 0x0D);
		private static readonly SolidColorBrush BtnShortBdr = MakeFrozen(0xFF, 0xC8, 0x20, 0x28);
		private static readonly SolidColorBrush BtnAutoBg   = MakeFrozen(0xFF, 0x0A, 0x2E, 0x2A);
		private static readonly SolidColorBrush BtnAutoBdr  = MakeFrozen(0xFF, 0x3A, 0xC8, 0xB0);
		private static readonly SolidColorBrush BtnFg       = MakeFrozen(0xFF, 0xCC, 0xDA, 0xD2);
		private static readonly SolidColorBrush BtnFlatFg   = MakeFrozen(0xFF, 0xFF, 0x60, 0x50);

		private static SolidColorBrush MakeFrozen(byte a, byte r, byte g, byte b)
		{
			var br = new SolidColorBrush(Color.FromArgb(a, r, g, b));
			br.Freeze();
			return br;
		}
		#endregion

		#region Dashboard build / teardown / update
		private void CreateDashboard()
		{
			if (ChartControl == null || _uiInitialized) return;
			_dashTornDown = false;
			_dashMinimized = DashboardStartMinimized;
			ZeusPanelCorner corner = DashboardCorner;

			ChartControl.Dispatcher.InvokeAsync(() =>
			{
				try
				{
					if (_uiInitialized || _dashTornDown || State == State.Terminated) return;

					_dragThumb = new Thumb { Background = Brushes.Transparent, Cursor = Cursors.SizeAll };
					var thumbFac = new FrameworkElementFactory(typeof(Border));
					thumbFac.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Control.BackgroundProperty));
					_dragThumb.Template = new ControlTemplate(typeof(Thumb)) { VisualTree = thumbFac };
					_dragThumb.DragDelta += OnPanelDragDelta;

					var titleText = new TextBlock
					{
						Text = "GB ZEUS  v" + VERSION,
						Foreground = DashTitleFg, FontSize = 11, FontWeight = FontWeights.Bold,
						VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center,
						Margin = new Thickness(4, 0, 30, 0), IsHitTestVisible = false
					};

					_pillPath = new System.Windows.Shapes.Path
					{
						Stroke = DashDimFg, StrokeThickness = 1.5, Fill = null, StrokeLineJoin = PenLineJoin.Round,
						Opacity = _dashMinimized ? 0.9 : 0.5, IsHitTestVisible = false,
						Data = Geometry.Parse("M 3,0 L 15,0 A 3,3 0 0 1 15,6 L 3,6 A 3,3 0 0 1 3,0 Z"),
						HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center
					};
					_pillBtn = new Border
					{
						Width = 22, Height = 12, Margin = new Thickness(0, 0, 8, 0),
						HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Center,
						Background = Brushes.Transparent, Cursor = Cursors.Hand,
						ToolTip = _dashMinimized ? "Click to restore" : "Click to minimize", Child = _pillPath
					};
					_pillBtn.MouseLeftButtonDown += OnPillMouseDown;
					_pillBtn.MouseLeftButtonUp += OnPillMouseUp;
					_pillBtn.MouseEnter += OnPillMouseEnter;
					_pillBtn.MouseLeave += OnPillMouseLeave;

					var titleGrid = new Grid();
					titleGrid.Children.Add(_dragThumb);
					titleGrid.Children.Add(titleText);
					titleGrid.Children.Add(_pillBtn);
					_dashTitleBar = new Border { Background = DashTitleBg, Height = 24, CornerRadius = new CornerRadius(8, 8, 0, 0), Child = titleGrid, ToolTip = "Drag to move" };

					var statusRow = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 3, 0, 2) };
					_dashStatus = new TextBlock { Text = "FLAT", Foreground = Brushes.DimGray, FontSize = 12, FontWeight = FontWeights.Bold };
					statusRow.Children.Add(_dashStatus);

					_dashInstrument = MakeInfoRow(DashDimFg);
					_dashWindow = MakeInfoRow(Brushes.WhiteSmoke);
					_dashWindowState = MakeInfoRow(Brushes.Orange);
					_dashState = MakeInfoRow(Brushes.WhiteSmoke);
					_dashDaily = MakeInfoRow(Brushes.WhiteSmoke);

					_dashTradeInfo = new StackPanel { Orientation = Orientation.Vertical, Visibility = Visibility.Collapsed };
					_dashTradeInfo.Children.Add(MakeSeparator());
					_dashEntry  = MakeInfoRow(Brushes.WhiteSmoke); _dashTradeInfo.Children.Add(_dashEntry);
					_dashStop   = MakeInfoRow(Brushes.Salmon);     _dashTradeInfo.Children.Add(_dashStop);
					_dashTarget = MakeInfoRow(Brushes.LimeGreen);  _dashTradeInfo.Children.Add(_dashTarget);
					_dashQty    = MakeInfoRow(Brushes.WhiteSmoke); _dashTradeInfo.Children.Add(_dashQty);
					_dashPnl    = MakeInfoRow(Brushes.WhiteSmoke); _dashTradeInfo.Children.Add(_dashPnl);

					_autoBtn  = MakeDashButton("AUTO: ON", 94, 26);
					_longBtn  = MakeDashButton("LONG: ON", 94, 26);
					_shortBtn = MakeDashButton("SHORT: ON", 94, 26);
					RestyleToggle(_autoBtn, "AUTO", _autoEnabled, BtnAutoBg, BtnAutoBdr);
					RestyleToggle(_longBtn, "LONG", _longEnabled, BtnLongBg, BtnLongBdr);
					RestyleToggle(_shortBtn, "SHORT", _shortEnabled, BtnShortBg, BtnShortBdr);
					_longBtn.IsEnabled = _autoEnabled;
					_shortBtn.IsEnabled = _autoEnabled;
					_autoBtn.Click += OnAutoToggleClick;
					_longBtn.Click += OnLongToggleClick;
					_shortBtn.Click += OnShortToggleClick;
					var toggleRow = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 2, 0, 0) };
					toggleRow.Children.Add(_autoBtn);
					toggleRow.Children.Add(_longBtn);
					toggleRow.Children.Add(_shortBtn);

					_beBtn = MakeDashButton("MOVE SL TO BE", 294, 24);
					_beBtn.Click += OnBEClick;

					_nudgeRow = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center };
					_slDownBtn = MakeDashButton("SL ▼", 70, 24);
					_slUpBtn   = MakeDashButton("SL ▲", 70, 24);
					_tpDownBtn = MakeDashButton("TP ▼", 70, 24);
					_tpUpBtn   = MakeDashButton("TP ▲", 70, 24);
					_slDownBtn.Click += OnSlDownClick;
					_slUpBtn.Click   += OnSlUpClick;
					_tpDownBtn.Click += OnTpDownClick;
					_tpUpBtn.Click   += OnTpUpClick;
					_nudgeRow.Children.Add(_slDownBtn);
					_nudgeRow.Children.Add(_slUpBtn);
					_nudgeRow.Children.Add(_tpDownBtn);
					_nudgeRow.Children.Add(_tpUpBtn);

					_flattenBtn = MakeDashButton("FLATTEN ALL", 294, 26);
					_flattenBtn.Background = BtnShortBg;
					_flattenBtn.BorderBrush = BtnShortBdr;
					_flattenBtn.Foreground = BtnFlatFg;
					_flattenBtn.Click += OnFlattenClick;

					_dashBody = new StackPanel { Orientation = Orientation.Vertical, MinWidth = 210, Margin = new Thickness(0, 0, 0, 6) };
					_dashBody.Children.Add(statusRow);
					_dashBody.Children.Add(MakeSeparator());
					_dashBody.Children.Add(_dashInstrument);
					_dashBody.Children.Add(_dashWindow);
					_dashBody.Children.Add(_dashWindowState);
					_dashBody.Children.Add(_dashState);
					_dashBody.Children.Add(_dashDaily);
					_dashBody.Children.Add(_dashTradeInfo);
					_dashBody.Children.Add(MakeSeparator());
					_dashBody.Children.Add(toggleRow);
					_dashBody.Children.Add(_beBtn);
					_dashBody.Children.Add(_nudgeRow);
					_dashBody.Children.Add(_flattenBtn);
					if (_dashMinimized) _dashBody.Visibility = Visibility.Collapsed;

					var main = new StackPanel { Orientation = Orientation.Vertical, MinWidth = 210 };
					main.Children.Add(_dashTitleBar);
					main.Children.Add(_dashBody);

					_dashPanel = new Border
					{
						HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top,
						Margin = new Thickness(10, 10, 0, 0), Background = DashBg, BorderBrush = DashBorder,
						BorderThickness = new Thickness(2), CornerRadius = new CornerRadius(10), ClipToBounds = true, Child = main
					};

					EventHandler layoutHandler = null;
					layoutHandler = (ls, le) =>
					{
						if (_dashPanel == null) return;
						var parent = _dashPanel.Parent as FrameworkElement;
						if (parent == null || _dashPanel.ActualWidth <= 0 || parent.ActualWidth <= 0) return;
						double left = 10, top = 10;
						switch (corner)
						{
							case ZeusPanelCorner.TopRight:    left = parent.ActualWidth - _dashPanel.ActualWidth - 10; break;
							case ZeusPanelCorner.BottomLeft:  top = parent.ActualHeight - _dashPanel.ActualHeight - 10; break;
							case ZeusPanelCorner.BottomRight: left = parent.ActualWidth - _dashPanel.ActualWidth - 10;
															top = parent.ActualHeight - _dashPanel.ActualHeight - 10; break;
						}
						_dashPanel.Margin = new Thickness(Math.Max(0, left), Math.Max(0, top), 0, 0);
						_dashPanel.LayoutUpdated -= layoutHandler;
					};
					_dashPanel.LayoutUpdated += layoutHandler;

					UserControlCollection.Add(_dashPanel);
					_uiInitialized = true;
				}
				catch (Exception ex) { Print("[gbZeus] Dashboard create error: " + ex.Message); }
			});
		}

		private void RemoveDashboard()
		{
			_dashTornDown = true;
			if (ChartControl == null || (_dashPanel == null && _dragThumb == null)) return;

			Action teardown = () =>
			{
				try
				{
					if (_dragThumb != null) _dragThumb.DragDelta -= OnPanelDragDelta;
					if (_pillBtn != null)
					{
						_pillBtn.MouseLeftButtonDown -= OnPillMouseDown;
						_pillBtn.MouseLeftButtonUp -= OnPillMouseUp;
						_pillBtn.MouseEnter -= OnPillMouseEnter;
						_pillBtn.MouseLeave -= OnPillMouseLeave;
					}
					if (_autoBtn != null) _autoBtn.Click -= OnAutoToggleClick;
					if (_longBtn != null) _longBtn.Click -= OnLongToggleClick;
					if (_shortBtn != null) _shortBtn.Click -= OnShortToggleClick;
					if (_beBtn != null) _beBtn.Click -= OnBEClick;
					if (_slDownBtn != null) _slDownBtn.Click -= OnSlDownClick;
					if (_slUpBtn != null) _slUpBtn.Click -= OnSlUpClick;
					if (_tpDownBtn != null) _tpDownBtn.Click -= OnTpDownClick;
					if (_tpUpBtn != null) _tpUpBtn.Click -= OnTpUpClick;
					if (_flattenBtn != null) _flattenBtn.Click -= OnFlattenClick;
					if (_dashPanel != null && UserControlCollection.Contains(_dashPanel))
						UserControlCollection.Remove(_dashPanel);
				}
				catch { }
				finally
				{
					_dragThumb = null; _pillBtn = null; _pillPath = null;
					_autoBtn = _longBtn = _shortBtn = _beBtn = _flattenBtn = null;
					_slDownBtn = _slUpBtn = _tpDownBtn = _tpUpBtn = null;
					_nudgeRow = null; _dashStatus = null;
					_dashInstrument = _dashWindow = _dashWindowState = _dashState = _dashDaily = null;
					_dashEntry = _dashStop = _dashTarget = _dashQty = _dashPnl = null;
					_dashTradeInfo = null; _dashBody = null; _dashTitleBar = null; _dashPanel = null;
					_uiInitialized = false;
				}
			};

			try
			{
				if (ChartControl.Dispatcher.CheckAccess()) teardown();
				else ChartControl.Dispatcher.Invoke(teardown);
			}
			catch { /* Terminated must never throw */ }
		}

		private void UpdateDashboard(bool force = false)
		{
			if (ChartControl == null || !_uiInitialized) return;

			DateTime nowUtc = DateTime.UtcNow;
			if (!force && (nowUtc - _lastDashPushUtc).TotalMilliseconds < DASH_PUSH_MIN_MS) return;
			if (_dashPushInFlight) return;
			_lastDashPushUtc = nowUtc;
			_dashPushInFlight = true;

			bool isLong = Position.MarketPosition == MarketPosition.Long;
			bool isShort = Position.MarketPosition == MarketPosition.Short;
			bool inPos = isLong || isShort;

			string posText = inPos ? (isLong ? "LONG" : "SHORT") : "FLAT";
			Brush posBrush = isLong ? Brushes.LimeGreen : isShort ? Brushes.Crimson : Brushes.DimGray;

			string instrText = "Instr  " + (Instrument != null && Instrument.MasterInstrument != null ? Instrument.MasterInstrument.Name : "?")
				+ "   Acct  " + (Account != null ? Account.Name : "?");
			string windowText = EnableSession
				? string.Format("Window  {0:HH:mm}-{1:HH:mm} ET", SessionStart, SessionEnd)
				: "Window  always on";

			string windowStateText; Brush windowStateBrush;
			if (tradingBlocked)         { windowStateText = "BLOCKED — daily limit"; windowStateBrush = Brushes.Crimson; }
			else if (!_autoEnabled)     { windowStateText = "AUTO OFF — manual only"; windowStateBrush = Brushes.Orange; }
			else if (InEntryWindow())   { windowStateText = "Armed — entries enabled"; windowStateBrush = Brushes.LimeGreen; }
			else                        { windowStateText = "Outside window"; windowStateBrush = Brushes.Orange; }

			int web = gt != null && CurrentBar >= 0 ? (int)gt.SpiderwebCount[0] : 0;
			string stateText = string.Format("Exit {0}   Web {1}/5{2}", ExitModeInput, web, web >= 5 ? "  STAND-ASIDE" : "");
			string dailyText = string.Format("Day  {0}${1:F0}   Trades {2}", dailyPnl < 0 ? "-" : "+", Math.Abs(dailyPnl), dailyEntryCount);
			Brush dailyBrush = dailyPnl >= 0 ? Brushes.LimeGreen : Brushes.Salmon;

			string entryText = "", stopText = "", tgtText = "", qtyText = "", pnlText = "";
			Brush pnlBrush = Brushes.WhiteSmoke;
			if (inPos)
			{
				double avg = Position.AveragePrice;
				entryText = string.Format("Entry   {0:F2}", avg);
				stopText  = currentStopPrice   > 0 ? string.Format("Stop    {0:F2}{1}", currentStopPrice,  _stopManuallyMoved   ? "  (M)" : "") : "Stop    none";
				tgtText   = currentTargetPrice > 0 ? string.Format("Target  {0:F2}{1}", currentTargetPrice, _targetManuallyMoved ? "  (M)" : "  (band)") : "Target  none";
				qtyText   = string.Format("Qty     {0}", Position.Quantity);
				double upnl = 0;
				try { upnl = Position.GetUnrealizedProfitLoss(PerformanceUnit.Currency, MarkPrice()); } catch { }
				pnlText  = string.Format("uPnL    {0}${1:F0}", upnl < 0 ? "-" : "+", Math.Abs(upnl));
				pnlBrush = upnl >= 0 ? Brushes.LimeGreen : Brushes.Salmon;
			}

			string pt = posText, it = instrText, wt = windowText, wst = windowStateText, st2 = stateText, dt = dailyText;
			string et = entryText, st = stopText, tgt = tgtText, qt = qtyText, plt = pnlText;
			Brush pb = posBrush, wsb = windowStateBrush, plb = pnlBrush, db = dailyBrush;
			bool ip = inPos, autoOn = _autoEnabled, longOn = _longEnabled, shortOn = _shortEnabled;

			ChartControl.Dispatcher.InvokeAsync(() =>
			{
				try
				{
					if (!_uiInitialized) return;
					if (_dashStatus != null) { _dashStatus.Text = pt; _dashStatus.Foreground = pb; }
					if (_dashInstrument != null) _dashInstrument.Text = it;
					if (_dashWindow != null) _dashWindow.Text = wt;
					if (_dashWindowState != null) { _dashWindowState.Text = wst; _dashWindowState.Foreground = wsb; }
					if (_dashState != null) _dashState.Text = st2;
					if (_dashDaily != null) { _dashDaily.Text = dt; _dashDaily.Foreground = db; }
					if (_dashTradeInfo != null) _dashTradeInfo.Visibility = ip ? Visibility.Visible : Visibility.Collapsed;
					if (ip)
					{
						if (_dashEntry != null) _dashEntry.Text = et;
						if (_dashStop != null) _dashStop.Text = st;
						if (_dashTarget != null) _dashTarget.Text = tgt;
						if (_dashQty != null) _dashQty.Text = qt;
						if (_dashPnl != null) { _dashPnl.Text = plt; _dashPnl.Foreground = plb; }
					}
					RestyleToggle(_autoBtn, "AUTO", autoOn, BtnAutoBg, BtnAutoBdr);
					RestyleToggle(_longBtn, "LONG", longOn, BtnLongBg, BtnLongBdr);
					RestyleToggle(_shortBtn, "SHORT", shortOn, BtnShortBg, BtnShortBdr);
					if (_longBtn != null) _longBtn.IsEnabled = autoOn;
					if (_shortBtn != null) _shortBtn.IsEnabled = autoOn;
					if (_slDownBtn != null) _slDownBtn.IsEnabled = ip;
					if (_slUpBtn != null) _slUpBtn.IsEnabled = ip;
					if (_tpDownBtn != null) _tpDownBtn.IsEnabled = ip;
					if (_tpUpBtn != null) _tpUpBtn.IsEnabled = ip;
					if (_beBtn != null) _beBtn.IsEnabled = ip;
				}
				catch { }
				finally { _dashPushInFlight = false; }
			});
		}
		#endregion

		#region Dashboard widgets + handlers
		private static Button MakeDashButton(string label, double width, double height)
		{
			var btn = new Button
			{
				Width = width, Height = height, Margin = new Thickness(2), MinWidth = 0, Cursor = Cursors.Hand,
				FocusVisualStyle = null, Padding = new Thickness(0), Background = BtnInactBg, BorderBrush = BtnInactBdr,
				BorderThickness = new Thickness(1), Foreground = BtnFg, FontSize = 11, FontWeight = FontWeights.Bold, Content = label
			};

			var ct = new ControlTemplate(typeof(Button));
			var grid = new FrameworkElementFactory(typeof(Grid), "RootGrid");

			var bf = new FrameworkElementFactory(typeof(Border), "BaseBorder");
			bf.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Control.BackgroundProperty));
			bf.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(Control.BorderBrushProperty));
			bf.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(Control.BorderThicknessProperty));
			bf.SetValue(Border.CornerRadiusProperty, new CornerRadius(4));
			grid.AppendChild(bf);

			var hf = new FrameworkElementFactory(typeof(Border), "HoverOverlay");
			hf.SetValue(Border.BackgroundProperty, new SolidColorBrush(Color.FromArgb(40, 0x40, 0xE0, 0xB0)));
			hf.SetValue(Border.BorderBrushProperty, new SolidColorBrush(Color.FromRgb(0x40, 0xE0, 0xB0)));
			hf.SetValue(Border.BorderThicknessProperty, new Thickness(1.5));
			hf.SetValue(Border.CornerRadiusProperty, new CornerRadius(4));
			hf.SetValue(UIElement.OpacityProperty, 0.0);
			hf.SetValue(UIElement.IsHitTestVisibleProperty, false);
			grid.AppendChild(hf);

			var cp = new FrameworkElementFactory(typeof(TextBlock));
			cp.SetValue(TextBlock.HorizontalAlignmentProperty, HorizontalAlignment.Center);
			cp.SetValue(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center);
			cp.SetValue(TextBlock.TextProperty, new TemplateBindingExtension(ContentControl.ContentProperty));
			cp.SetValue(TextBlock.ForegroundProperty, new TemplateBindingExtension(Control.ForegroundProperty));
			cp.SetValue(TextBlock.FontSizeProperty, new TemplateBindingExtension(Control.FontSizeProperty));
			cp.SetValue(TextBlock.FontWeightProperty, new TemplateBindingExtension(Control.FontWeightProperty));
			grid.AppendChild(cp);

			ct.VisualTree = grid;
			var hoverTrigger = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
			hoverTrigger.Setters.Add(new Setter { TargetName = "HoverOverlay", Property = UIElement.OpacityProperty, Value = 1.0 });
			ct.Triggers.Add(hoverTrigger);
			var disabledTrigger = new Trigger { Property = UIElement.IsEnabledProperty, Value = false };
			disabledTrigger.Setters.Add(new Setter { TargetName = "RootGrid", Property = UIElement.OpacityProperty, Value = 0.35 });
			ct.Triggers.Add(disabledTrigger);
			btn.Template = ct;
			return btn;
		}

		private static void RestyleToggle(Button btn, string label, bool on, Brush onBg, Brush onBdr)
		{
			if (btn == null) return;
			btn.Content = label + (on ? ": ON" : ": OFF");
			btn.Background = on ? onBg : BtnInactBg;
			btn.BorderBrush = on ? onBdr : BtnInactBdr;
		}

		private static TextBlock MakeInfoRow(Brush foreground)
		{
			return new TextBlock
			{
				Text = "", Foreground = foreground, FontSize = 11,
				FontFamily = new System.Windows.Media.FontFamily("Consolas"),
				HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(12, 1, 12, 1)
			};
		}

		private static Border MakeSeparator()
		{
			return new Border { Height = 1, Background = DashSep, Margin = new Thickness(6, 3, 6, 3) };
		}

		// Click handlers — only flip volatile flags / restyle; order actions run on the strategy thread.
		private void OnAutoToggleClick(object sender, RoutedEventArgs e)
		{
			_autoEnabled = !_autoEnabled;
			RestyleToggle(_autoBtn, "AUTO", _autoEnabled, BtnAutoBg, BtnAutoBdr);
			if (_longBtn != null) _longBtn.IsEnabled = _autoEnabled;
			if (_shortBtn != null) _shortBtn.IsEnabled = _autoEnabled;
		}

		private void OnLongToggleClick(object sender, RoutedEventArgs e)
		{
			_longEnabled = !_longEnabled;
			RestyleToggle(_longBtn, "LONG", _longEnabled, BtnLongBg, BtnLongBdr);
		}

		private void OnShortToggleClick(object sender, RoutedEventArgs e)
		{
			_shortEnabled = !_shortEnabled;
			RestyleToggle(_shortBtn, "SHORT", _shortEnabled, BtnShortBg, BtnShortBdr);
		}

		private void OnBEClick(object sender, RoutedEventArgs e) { _pendingBE = true; }

		// ▲ raises the price, ▼ lowers it, by ManualNudgeTicks (unambiguous for long & short).
		private void OnSlDownClick(object sender, RoutedEventArgs e) { System.Threading.Interlocked.Add(ref _pendingStopNudgeTicks,   -Math.Max(1, ManualNudgeTicks)); }
		private void OnSlUpClick(object sender, RoutedEventArgs e)   { System.Threading.Interlocked.Add(ref _pendingStopNudgeTicks,    Math.Max(1, ManualNudgeTicks)); }
		private void OnTpDownClick(object sender, RoutedEventArgs e) { System.Threading.Interlocked.Add(ref _pendingTargetNudgeTicks, -Math.Max(1, ManualNudgeTicks)); }
		private void OnTpUpClick(object sender, RoutedEventArgs e)   { System.Threading.Interlocked.Add(ref _pendingTargetNudgeTicks,  Math.Max(1, ManualNudgeTicks)); }

		private void OnFlattenClick(object sender, RoutedEventArgs e)
		{
			_pendingFlatten = true;
			_autoEnabled = false;   // flatten also pauses auto entries — re-arm via AUTO
			RestyleToggle(_autoBtn, "AUTO", false, BtnAutoBg, BtnAutoBdr);
			if (_longBtn != null) _longBtn.IsEnabled = false;
			if (_shortBtn != null) _shortBtn.IsEnabled = false;
		}

		private void OnPillMouseDown(object sender, MouseButtonEventArgs e) { e.Handled = true; }

		private void OnPillMouseUp(object sender, MouseButtonEventArgs e)
		{
			e.Handled = true;
			_dashMinimized = !_dashMinimized;
			if (_dashBody != null) _dashBody.Visibility = _dashMinimized ? Visibility.Collapsed : Visibility.Visible;
			if (_pillPath != null) _pillPath.Opacity = _dashMinimized ? 0.9 : 0.5;
			if (_pillBtn != null) _pillBtn.ToolTip = _dashMinimized ? "Click to restore" : "Click to minimize";
		}

		private void OnPillMouseEnter(object sender, MouseEventArgs e) { if (_pillPath != null) _pillPath.Opacity = 1.0; }
		private void OnPillMouseLeave(object sender, MouseEventArgs e) { if (_pillPath != null) _pillPath.Opacity = _dashMinimized ? 0.9 : 0.5; }

		private void OnPanelDragDelta(object sender, DragDeltaEventArgs e)
		{
			if (_dashPanel == null) return;
			double newLeft = Math.Max(0, _dashPanel.Margin.Left + e.HorizontalChange);
			double newTop = Math.Max(0, _dashPanel.Margin.Top + e.VerticalChange);
			if (ChartControl != null)
			{
				newLeft = Math.Min(newLeft, Math.Max(0, ChartControl.ActualWidth - _dashPanel.ActualWidth));
				newTop = Math.Min(newTop, Math.Max(0, ChartControl.ActualHeight - _dashPanel.ActualHeight));
			}
			_dashPanel.Margin = new Thickness(newLeft, newTop, 0, 0);
		}
		#endregion

		#region Developer (0)
		[Display(Name = "Author",  Order = 0, GroupName = "0. Developer")]
		public string Author => "GreyBeard";

		[Display(Name = "Version", Order = 1, GroupName = "0. Developer")]
		public string Version => VERSION;

		[Display(Name = "Website", Order = 2, GroupName = "0. Developer")]
		public string Website => "https://greybeardconsulting.net/";
		#endregion

		#region Signal (1)
		[NinjaScriptProperty]
		[Display(Name = "Enable Bollinger Gap (BG)", Description = "Trade SignalCode +/-1 (momentum gap at the band).", Order = 1, GroupName = "1. Signal")]
		public bool EnableBollingerGap { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Enable Fill Continuation (FC)", Description = "Trade SignalCode +/-2 (gap-fill continuation).", Order = 2, GroupName = "1. Signal")]
		public bool EnableContinuation { get; set; }

		[NinjaScriptProperty]
		[Range(2, int.MaxValue)]
		[Display(Name = "Bollinger Period", Description = "Band period; keep identical to the GodTrades indicator.", Order = 3, GroupName = "1. Signal")]
		public int BollingerPeriod { get; set; }

		[NinjaScriptProperty]
		[Range(0.1, double.MaxValue)]
		[Display(Name = "Bollinger Std Dev", Description = "Band std-dev; keep identical to the GodTrades indicator (course 20/2).", Order = 4, GroupName = "1. Signal")]
		public double BollingerStdDev { get; set; }

		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name = "BG Band Proximity Ticks", Description = "How close both gap candles must sit to the band for a BG signal.", Order = 5, GroupName = "1. Signal")]
		public int BollingerBandProximityTicks { get; set; }

		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name = "Minimum Bars Before Valid", Description = "A gap line must survive this many candles before a fill counts (course: 3).", Order = 6, GroupName = "1. Signal")]
		public int MinimumBarsBeforeValid { get; set; }

		[NinjaScriptProperty]
		[Range(0, 20)]
		[Display(Name = "Confirmation Bars After Touch", Description = "Bars allowed for an FC to confirm after a valid fill (course: 2).", Order = 7, GroupName = "1. Signal")]
		public int ConfirmationBarsAfterTouch { get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "Minimum Gap Size Ticks", Description = "Smallest close-to-open body gap that creates a gap line. 1 suits tick charts; raise on minute charts to skip clock-boundary micro-gaps.", Order = 8, GroupName = "1. Signal")]
		public int MinimumGapSizeTicks { get; set; }

		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name = "Minimum Body Ticks (BG doji filter)", Description = "Bollinger Gap entries only: both candles need at least this body. 0 disables. Gap lines are still created either way.", Order = 9, GroupName = "1. Signal")]
		public int MinimumBodyTicks { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "FC Confirmation Mode", Description = "How decisively the candle must reject the gap zone for a Fill-Continuation. RequireCloseBeyondFullZone is the course reading.", Order = 10, GroupName = "1. Signal")]
		public GodTradesContinuationConfirmationMode ContinuationConfirmation { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Require Signal Candle Direction", Description = "Longs require a bullish signal candle; shorts require a bearish one.", Order = 11, GroupName = "1. Signal")]
		public bool RequireSignalCandleDirection { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Require Correct Approach", Description = "Bullish gaps must be touched from above; bearish gaps from below.", Order = 12, GroupName = "1. Signal")]
		public bool RequireCorrectApproach { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Use FC Midpoint Filter", Description = "ON: a Fill-Continuation only confirms if the candle sits in the correct half of the Bollinger envelope.", Order = 13, GroupName = "1. Signal")]
		public bool UseFcMidpointFilter { get; set; }

		[NinjaScriptProperty]
		[Range(0.0, 100.0)]
		[Display(Name = "FC Long Below Midpoint %", Description = "Long FC requires the candle low this far below the middle band, as a percent of middle-to-lower distance.", Order = 14, GroupName = "1. Signal")]
		public double FcLongBelowMidPct { get; set; }

		[NinjaScriptProperty]
		[Range(0.0, 100.0)]
		[Display(Name = "FC Short Above Midpoint %", Description = "Short FC requires the candle high this far above the middle band, as a percent of middle-to-upper distance.", Order = 15, GroupName = "1. Signal")]
		public double FcShortAboveMidPct { get; set; }
		#endregion

		#region Session (2)
		[NinjaScriptProperty]
		[RefreshProperties(RefreshProperties.All)]
		[Display(Name = "Enable Session Filter", Description = "ON = only take entries inside the window below. OFF = trade signals across the whole session.", Order = 1, GroupName = "2. Session")]
		public bool EnableSession { get; set; }

		[NinjaScriptProperty]
		[PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
		[Display(Name = "Session Start", Description = "First entry time (chart timezone). Set the chart to Eastern.", Order = 2, GroupName = "2. Session")]
		public DateTime SessionStart { get; set; }

		[NinjaScriptProperty]
		[PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
		[Display(Name = "Session End", Description = "Last entry time (chart timezone). Open trades are flattened here if 'Flatten At Window End' is on.", Order = 3, GroupName = "2. Session")]
		public DateTime SessionEnd { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Flatten At Window End", Description = "Close any open position when the session window ends.", Order = 4, GroupName = "2. Session")]
		public bool FlattenAtWindowEnd { get; set; }
		#endregion

		#region Exits (3)
		[NinjaScriptProperty]
		[Display(Name = "Exit Mode", Description = "BandTarget = methodology (opposite band, re-priced each bar). FixedTicks = A/B only.", Order = 1, GroupName = "3. Exits")]
		public ZeusExitMode ExitModeInput { get; set; }

		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name = "Stop Offset Ticks", Description = "Extra ticks beyond the signal candle extreme for the (static) stop.", Order = 2, GroupName = "3. Exits")]
		public int StopOffsetTicks { get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "Fixed Stop Ticks", Description = "FixedTicks mode only: stop distance in ticks.", Order = 3, GroupName = "3. Exits")]
		public int FixedStopTicks { get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "Fixed Target Ticks", Description = "FixedTicks mode only (also the fallback when the band is on the wrong side): target distance in ticks.", Order = 4, GroupName = "3. Exits")]
		public int FixedTargetTicks { get; set; }
		#endregion

		#region Filters (4)
		[NinjaScriptProperty]
		[Display(Name = "Spiderweb Stand-Aside", Description = "Skip entries while the indicator's spiderweb warning is active (>= 5 clustered gap lines).", Order = 1, GroupName = "4. Filters")]
		public bool SpiderwebSuppress { get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "Spiderweb Distance Ticks", Description = "Gap lines within this distance of price count toward the spiderweb cluster.", Order = 2, GroupName = "4. Filters")]
		public int SpiderwebDistanceTicks { get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "Spiderweb Line Count", Description = "This many clustered lines = spiderweb; entries stand aside while it is active.", Order = 3, GroupName = "4. Filters")]
		public int SpiderwebLineCount { get; set; }
		#endregion

		#region General (5)
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "Quantity", Description = "Contracts per entry.", Order = 1, GroupName = "5. General")]
		public int Quantity { get; set; }
		#endregion

		#region Dashboard (6)
		[NinjaScriptProperty]
		[Display(Name = "Show Dashboard", Description = "Show the on-chart control panel (live/sim only, never in Strategy Analyzer).", Order = 1, GroupName = "6. Dashboard")]
		public bool ShowDashboard { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Dashboard Corner", Description = "Chart corner the panel anchors to on first layout.", Order = 2, GroupName = "6. Dashboard")]
		public ZeusPanelCorner DashboardCorner { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Dashboard Start Minimized", Description = "Start the panel collapsed to its title bar.", Order = 3, GroupName = "6. Dashboard")]
		public bool DashboardStartMinimized { get; set; }
		#endregion

		#region Manual Control (7)
		[NinjaScriptProperty]
		[Range(1, 500)]
		[Display(Name = "Manual Nudge Ticks", Description = "Tick step per SL/TP nudge-button click.", Order = 1, GroupName = "7. Manual Control")]
		public int ManualNudgeTicks { get; set; }

		[NinjaScriptProperty]
		[Range(-500, 500)]
		[Display(Name = "Manual BE Offset Ticks", Description = "Signed offset for MOVE SL TO BE (negative locks in a small loss).", Order = 2, GroupName = "7. Manual Control")]
		public int ManualBeOffsetTicks { get; set; }
		#endregion

		#region Risk (8)
		[NinjaScriptProperty]
		[Display(Name = "Use Daily Max Loss", Description = "Lock out new entries for the session once realized day P&L hits the max loss.", Order = 1, GroupName = "8. Risk")]
		public bool UseDailyLoss { get; set; }

		[NinjaScriptProperty]
		[Range(0, double.MaxValue)]
		[Display(Name = "Daily Max Loss ($)", Description = "Realized loss for the session that halts new entries (positive dollars).", Order = 2, GroupName = "8. Risk")]
		public double DailyLoss { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Use Daily Max Profit", Description = "Lock out new entries for the session once realized day P&L hits the max profit.", Order = 3, GroupName = "8. Risk")]
		public bool UseDailyProfit { get; set; }

		[NinjaScriptProperty]
		[Range(0, double.MaxValue)]
		[Display(Name = "Daily Max Profit ($)", Description = "Realized profit for the session that halts new entries (positive dollars).", Order = 4, GroupName = "8. Risk")]
		public double DailyProfit { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Flatten On Daily Limit", Description = "When a daily limit is hit, also flatten any open position (not just block new entries).", Order = 5, GroupName = "8. Risk")]
		public bool DailyFlatten { get; set; }

		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name = "Max Trades Per Day", Description = "Cap entries per session (0 = no cap).", Order = 6, GroupName = "8. Risk")]
		public int MaxTradesPerDay { get; set; }

		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name = "Max Stop Ticks (risk cap)", Description = "BandTarget mode: skip a signal whose candle-back stop would be wider than this many ticks (0 = no cap). Bounds per-trade risk from oversized signal candles.", Order = 7, GroupName = "8. Risk")]
		public int MaxStopTicks { get; set; }
		#endregion
	}
}
