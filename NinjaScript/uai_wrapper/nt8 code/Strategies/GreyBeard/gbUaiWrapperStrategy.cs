#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
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
using NinjaTrader.NinjaScript.Indicators;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

//This namespace holds Strategies in this folder and is required. Do not change it.
namespace NinjaTrader.NinjaScript.Strategies.GreyBeard
{
	public enum GbPanelCorner { TopLeft, TopRight, BottomLeft, BottomRight }

	public class gbUaiWrapperStrategy : Strategy
	{
		private const string EntrySignalLong = "UaiLong";
		private const string EntrySignalShort = "UaiShort";

		private UltimateAI2 uai2;

		// Manual dashboard trade-management state. Click handlers (UI thread) only set/accumulate
		// these; ProcessManualCommands() (data thread, driven from OnMarketData for an immediate
		// reaction rather than waiting for the next bar close) drains and applies them.
		private string _entrySignalName = "";
		private double _currentStopPrice;
		private double _currentTargetPrice;
		private bool _breakEvenSet;
		private volatile bool _pendingBE;
		private int _pendingSlNudgeTicks;
		private int _pendingTpNudgeTicks;

		[Display(Name = "Author",  Order = 0, GroupName = "0. Developer")]
		public string Author => "GreyBeard";

		[Display(Name = "Version", Order = 1, GroupName = "0. Developer")]
		public string Version => "1.0.1";

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "Profit Target (ticks)", Order = 1, GroupName = "1. Risk Management")]
		public int ProfitTargetTicks
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "Stop Loss (ticks)", Order = 2, GroupName = "1. Risk Management")]
		public int StopLossTicks
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "Contract Quantity", Order = 3, GroupName = "1. Risk Management")]
		public int ContractQty
		{ get; set; }

		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name = "Confirmation Bars", Order = 4, GroupName = "1. Risk Management",
			Description = "Bars to wait after a signal appears before trading it. Since UltimateAI2 can revise (repaint) a signal's value after the fact, re-reading it N bars later both delays entry and re-validates the signal is still standing before acting on it. Every one of those N bars must also close in the signal's own direction (green for long, red for short) or the entry is skipped. 0 = act immediately on the signal bar's close (no direction check).")]
		public int ConfirmationBars
		{ get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Reversing Signal", Order = 5, GroupName = "1. Risk Management",
			Description = "Requires the bar immediately before the signal bar to close in the opposite direction of the signal (e.g. a red/bearish bar, then a bullish signal, then a bullish confirmation bar). Checked in addition to Confirmation Bars. Default off.")]
		public bool ReversingSignal
		{ get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Enable Indicator Alerts", Order = 6, GroupName = "1. Risk Management",
			Description = "Turns UltimateAI2's own upSignal/dnSignal/long/short sound alerts on or off (all four together).")]
		public bool EnableIndicatorAlerts
		{ get; set; }

		[NinjaScriptProperty]
		[Range(-500, 500)]
		[Display(Name = "Manual BE Offset Ticks", Order = 7, GroupName = "1. Risk Management",
			Description = "Signed offset for the MOVE SL TO BE button. Long stop = entry + offset ticks; short stop = entry - offset ticks. 0 = exact entry.")]
		public int ManualBeOffsetTicks
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, 500)]
		[Display(Name = "Manual Nudge Ticks", Order = 8, GroupName = "1. Risk Management",
			Description = "Ticks each SL/TP nudge button click moves the price. Rapid clicks accumulate into a single order change.")]
		public int ManualNudgeTicks
		{ get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Time Filter Enabled", Order = 6, GroupName = "2. Time Filter")]
		public bool TimeFilterEnabled
		{ get; set; }

		[NinjaScriptProperty]
		[PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
		[Display(Name = "Start Time", Order = 7, GroupName = "2. Time Filter")]
		public DateTime StartTime
		{ get; set; }

		[NinjaScriptProperty]
		[PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
		[Display(Name = "End Time", Order = 8, GroupName = "2. Time Filter",
			Description = "If End Time is earlier than Start Time, the window is treated as wrapping past midnight (e.g. 10:00 PM to 2:00 AM).")]
		public DateTime EndTime
		{ get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show Dashboard", Order = 0, GroupName = "3. Dashboard")]
		public bool ShowDashboard
		{ get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Dashboard Corner", Order = 1, GroupName = "3. Dashboard")]
		public GbPanelCorner DashboardCorner
		{ get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Start Minimized", Order = 2, GroupName = "3. Dashboard")]
		public bool DashboardStartMinimized
		{ get; set; }

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Wrapper strategy that trades UltimateAI2 long/short signals, entering one bar after the signal appears.";
				Name										= "gbUaiWrapperStrategy";
				Calculate									= Calculate.OnBarClose;
				EntriesPerDirection							= 1;
				EntryHandling								= EntryHandling.AllEntries;
				IsExitOnSessionCloseStrategy				= true;
				ExitOnSessionCloseSeconds					= 30;
				IsFillLimitOnTouch							= false;
				MaximumBarsLookBack							= MaximumBarsLookBack.TwoHundredFiftySix;
				OrderFillResolution							= OrderFillResolution.Standard;
				Slippage									= 0;
				StartBehavior								= StartBehavior.WaitUntilFlat;
				TimeInForce									= TimeInForce.Gtc;
				TraceOrders									= false;
				RealtimeErrorHandling						= RealtimeErrorHandling.StopCancelClose;
				StopTargetHandling							= StopTargetHandling.PerEntryExecution;
				BarsRequiredToTrade							= 20;
				IsInstantiatedOnEachOptimizationIteration	= true;
				ProfitTargetTicks							= 40;
				StopLossTicks								= 80;
				ContractQty									= 1;
				ConfirmationBars							= 1;
				ReversingSignal								= false;
				EnableIndicatorAlerts						= false;
				ManualBeOffsetTicks							= 0;
				ManualNudgeTicks							= 4;
				TimeFilterEnabled							= true;
				StartTime									= DateTime.Parse("9:30 AM", System.Globalization.CultureInfo.InvariantCulture);
				EndTime										= DateTime.Parse("10:00 AM", System.Globalization.CultureInfo.InvariantCulture);
				ShowDashboard								= true;
				DashboardCorner								= GbPanelCorner.TopLeft;
				DashboardStartMinimized						= false;
			}
			else if (State == State.Configure)
			{
				// Signal-name-keyed, not the anonymous overload -- ProcessManualCommands() later
				// overrides these by signal name (SetStopLoss(_entrySignalName, ...)), which only
				// reliably latches onto an existing bracket that was itself placed by signal name.
				SetProfitTarget(EntrySignalLong, CalculationMode.Ticks, ProfitTargetTicks);
				SetStopLoss(EntrySignalLong, CalculationMode.Ticks, StopLossTicks, false);
				SetProfitTarget(EntrySignalShort, CalculationMode.Ticks, ProfitTargetTicks);
				SetStopLoss(EntrySignalShort, CalculationMode.Ticks, StopLossTicks, false);
			}
			else if (State == State.DataLoaded)
			{
				uai2 = UltimateAI2(4, 4, 6, 6, EnableIndicatorAlerts, @"C:\Program Files\NinjaTrader 8\sounds\Alert2.wav", EnableIndicatorAlerts, @"C:\Program Files\NinjaTrader 8\sounds\Alert2.wav", EnableIndicatorAlerts, @"C:\Program Files\NinjaTrader 8\sounds\Alert2.wav", EnableIndicatorAlerts, @"C:\Program Files\NinjaTrader 8\sounds\Alert2.wav");
			}
			else if (State == State.Realtime)
			{
				if (ShowDashboard)
					CreateDashboard();
			}
			else if (State == State.Terminated)
			{
				RemoveDashboard();
			}
		}

		protected override void OnBarUpdate()
		{
			UpdateDashboard();

			if (CurrentBar < BarsRequiredToTrade + ConfirmationBars + (ReversingSignal ? 1 : 0))
				return;

			if (!IsWithinTimeWindow())
				return;

			// UltimateAI2 can revise (repaint) longS/shortS after the fact -- a value can be
			// non-zero at the moment a bar closes and later read back as 0 on that same bar.
			// Re-reading at lookback index ConfirmationBars, rather than acting immediately,
			// both delays entry and re-validates the signal is still standing before trading it.
			// On top of that, every one of the ConfirmationBars candles since the signal bar must
			// also close in the signal's own direction (green bars confirming a long signal, red
			// bars confirming a short signal) -- a signal followed by opposite-colored candles is
			// not considered confirmed even if the raw signal value itself never went back to 0.
			// ReversingSignal adds one more check: the bar immediately BEFORE the signal bar must
			// have closed in the opposite direction of the signal (a reversal setup), e.g. a red
			// bar, then the bullish signal fires, then the bullish confirmation bar(s).
			bool rawLong  = uai2.longS[ConfirmationBars]  != 0;
			bool rawShort = uai2.shortS[ConfirmationBars] != 0;
			bool longConfirmOk  = ConfirmationBarsMatchDirection(true);
			bool shortConfirmOk = ConfirmationBarsMatchDirection(false);
			bool longReverseOk  = ReversingBarConfirmed(true);
			bool shortReverseOk = ReversingBarConfirmed(false);

			if (rawLong)
				PrintSignalDebug("LONG", longConfirmOk, longReverseOk);
			if (rawShort)
				PrintSignalDebug("SHORT", shortConfirmOk, shortReverseOk);

			bool longSignal  = rawLong  && longConfirmOk  && longReverseOk;
			bool shortSignal = rawShort && shortConfirmOk && shortReverseOk;

			if (longSignal && Position.MarketPosition != MarketPosition.Long)
			{
				Print(string.Format("{0} Bar={1} LONG ENTRY, triggered by longS[{2}]={3}", Time[0], CurrentBar, ConfirmationBars, uai2.longS[ConfirmationBars]));
				EnterLong(ContractQty, EntrySignalLong);
			}
			else if (shortSignal && Position.MarketPosition != MarketPosition.Short)
			{
				Print(string.Format("{0} Bar={1} SHORT ENTRY, triggered by shortS[{2}]={3}", Time[0], CurrentBar, ConfirmationBars, uai2.shortS[ConfirmationBars]));
				EnterShort(ContractQty, EntrySignalShort);
			}
		}

		// Debug line showing the pass/fail status of every gate a raw signal has to clear before
		// it becomes a trade: the raw signal itself (always YES here, since this is only called
		// when it fired), the confirmation-bar direction check, and the reversing-bar check
		// (N/A when ReversingSignal is off). Printed for both taken and skipped signals so a
		// skipped entry's reason is visible in the Output window.
		private void PrintSignalDebug(string direction, bool confirmOk, bool reverseOk)
		{
			string reverseText = ReversingSignal ? (reverseOk ? "PASS" : "FAIL") : "N/A";
			Print(string.Format("{0} Bar={1} {2} signal check -- Signal=YES  ConfirmBars({3})={4}  ReversingBar={5}",
				Time[0], CurrentBar, direction, ConfirmationBars, confirmOk ? "PASS" : "FAIL", reverseText));
		}

		// Checks that every confirmation candle between the signal bar and the current bar
		// (lookback indices 0..ConfirmationBars-1) closed in the signal's direction -- green
		// (Close > Open) for a long signal, red (Close < Open) for a short signal. With
		// ConfirmationBars = 0 there are no confirmation candles to check, so this is trivially true.
		private bool ConfirmationBarsMatchDirection(bool isLong)
		{
			for (int i = 0; i < ConfirmationBars; i++)
			{
				bool barIsGreen = Close[i] > Open[i];
				if (isLong != barIsGreen)
					return false;
			}
			return true;
		}

		// When ReversingSignal is on, requires the bar immediately before the signal bar
		// (lookback index ConfirmationBars + 1) to have closed opposite the signal's direction --
		// a red/bearish bar ahead of a bullish signal, or a green/bullish bar ahead of a bearish
		// signal. Off by default; returns true unconditionally when disabled.
		private bool ReversingBarConfirmed(bool isLong)
		{
			if (!ReversingSignal)
				return true;

			int idx = ConfirmationBars + 1;
			bool barIsGreen = Close[idx] > Open[idx];
			return isLong ? !barIsGreen : barIsGreen;
		}

		protected override void OnMarketData(MarketDataEventArgs marketDataUpdate)
		{
			if (State != State.Realtime) return;
			if (marketDataUpdate.MarketDataType != MarketDataType.Last) return;
			ProcessManualCommands();
		}

		protected override void OnPositionUpdate(NinjaTrader.Cbi.Position position, double averagePrice, int quantity, MarketPosition marketPosition)
		{
			if (marketPosition == MarketPosition.Flat)
			{
				_entrySignalName = "";
				_currentStopPrice = 0;
				_currentTargetPrice = 0;
				_breakEvenSet = false;
			}
			else if (_currentStopPrice == 0 && averagePrice > 0)
			{
				// First fill of a new position -- seed the manual-override baseline at the
				// same price SetStopLoss/SetProfitTarget already placed the live orders at,
				// so the first SL/TP nudge click adjusts from the real starting point.
				_entrySignalName = marketPosition == MarketPosition.Long ? EntrySignalLong : EntrySignalShort;
				int tdir = marketPosition == MarketPosition.Long ? 1 : -1;
				_currentStopPrice = averagePrice - tdir * StopLossTicks * TickSize;
				_currentTargetPrice = averagePrice + tdir * ProfitTargetTicks * TickSize;
			}
			UpdateDashboard(true);
		}

		// Gates new entries to the Start/End Time window (using the bar timeline's own time,
		// i.e. whatever timezone the chart/instrument is currently displaying). Existing
		// positions are still managed as normal outside the window -- this only blocks new
		// entries, not stops/targets/session-close. Handles windows that cross midnight
		// (e.g. Start=10:00 PM, End=2:00 AM) by wrapping when End < Start.
		private bool IsWithinTimeWindow()
		{
			if (!TimeFilterEnabled)
				return true;

			int barTime   = ToTime(Time[0]);
			int startTime = ToTime(StartTime);
			int endTime   = ToTime(EndTime);

			if (startTime <= endTime)
				return barTime >= startTime && barTime <= endTime;

			return barTime >= startTime || barTime <= endTime;
		}

		private double MarkPrice()
		{
			return Close[0];
		}

		// Drains the volatile flags the dashboard buttons set and applies them to the live
		// stop/target orders. Runs from OnMarketData so a click acts immediately rather than
		// waiting for the next bar close. BE and an SL nudge landing in the same drain window
		// are ambiguous -- BE wins, the SL nudge is dropped; a TP nudge still applies either way.
		private void ProcessManualCommands()
		{
			if (!_pendingBE && _pendingSlNudgeTicks == 0 && _pendingTpNudgeTicks == 0)
				return;

			if (Position.MarketPosition == MarketPosition.Flat || _entrySignalName.Length == 0)
			{
				Print(string.Format("[gbUaiWrapperStrategy] MANUAL command DISCARDED -- flat or no entry signal (pos={0}, signal='{1}')", Position.MarketPosition, _entrySignalName));
				_pendingBE = false;
				System.Threading.Interlocked.Exchange(ref _pendingSlNudgeTicks, 0);
				System.Threading.Interlocked.Exchange(ref _pendingTpNudgeTicks, 0);
				return;
			}

			bool doBe = _pendingBE;
			_pendingBE = false;
			int slTicks = System.Threading.Interlocked.Exchange(ref _pendingSlNudgeTicks, 0);
			int tpTicks = System.Threading.Interlocked.Exchange(ref _pendingTpNudgeTicks, 0);

			if (doBe && slTicks != 0)
			{
				Print("[gbUaiWrapperStrategy] MANUAL BE takes precedence -- dropped SL nudge of " + slTicks + " ticks");
				slTicks = 0;
			}

			bool isLong = Position.MarketPosition == MarketPosition.Long;
			double entry = Position.AveragePrice;
			double market = MarkPrice();
			if (entry <= 0 || market <= 0) return;

			// Stop move (BE or nudge). ▲ raises the price, ▼ lowers it -- same convention
			// regardless of direction, matching the other GreyBeard dashboards.
			if (doBe || slTicks != 0)
			{
				double newStop = doBe
					? (isLong ? entry + ManualBeOffsetTicks * TickSize : entry - ManualBeOffsetTicks * TickSize)
					: (_currentStopPrice > 0 ? _currentStopPrice : (isLong ? entry - StopLossTicks * TickSize : entry + StopLossTicks * TickSize)) + slTicks * TickSize;
				newStop = Instrument.MasterInstrument.RoundToTickSize(newStop);

				bool validSide = isLong ? newStop <= market - TickSize : newStop >= market + TickSize;
				if (!validSide)
				{
					Print(string.Format("[gbUaiWrapperStrategy] MANUAL {0} SKIPPED -- wrong side (new={1:F2} market={2:F2})", doBe ? "BE" : "SL", newStop, market));
				}
				else
				{
					SetStopLoss(_entrySignalName, CalculationMode.Price, newStop, false);
					_currentStopPrice = newStop;
					if (doBe) _breakEvenSet = true;
					Print(string.Format("[gbUaiWrapperStrategy] MANUAL {0} APPLIED -- signal={1} newStop={2:F2}", doBe ? "BE" : "SL", _entrySignalName, newStop));
				}
			}

			// Target nudge.
			if (tpTicks != 0)
			{
				double newTgt = (_currentTargetPrice > 0 ? _currentTargetPrice : (isLong ? entry + ProfitTargetTicks * TickSize : entry - ProfitTargetTicks * TickSize)) + tpTicks * TickSize;
				newTgt = Instrument.MasterInstrument.RoundToTickSize(newTgt);

				bool validSide = isLong ? newTgt >= market + TickSize : newTgt <= market - TickSize;
				if (!validSide)
				{
					Print(string.Format("[gbUaiWrapperStrategy] MANUAL TP SKIPPED -- wrong side (new={0:F2} market={1:F2})", newTgt, market));
				}
				else
				{
					SetProfitTarget(_entrySignalName, CalculationMode.Price, newTgt);
					_currentTargetPrice = newTgt;
					Print(string.Format("[gbUaiWrapperStrategy] MANUAL TP APPLIED -- signal={0} newTgt={1:F2}", _entrySignalName, newTgt));
				}
			}

			UpdateDashboard(true);
		}

		// ====================================================================
		//  Dashboard (Terminator_V2-style: draggable, minimizable, corner-anchored)
		// ====================================================================
		#region Dashboard fields
		private Border _dashPanel;
		private Border _dashTitleBar;
		private StackPanel _dashBody;
		private StackPanel _dashTradeInfo;
		private Thumb _dragThumb;
		private Border _pillBtn;
		private TextBlock _pillText;
		private TextBlock _dashStatus;
		private TextBlock _dashInstrument, _dashConfirm, _dashWindow, _dashWindowState, _dashWindowNote, _dashAlerts;
		private TextBlock _dashEntry, _dashStop, _dashTarget, _dashQty, _dashPnl;
		private Button _beBtn, _slDownBtn, _slUpBtn, _tpDownBtn, _tpUpBtn;
		private bool _dashMinimized;
		private bool _uiInitialized;
		private volatile bool _dashTornDown;
		private DateTime _lastDashPushUtc = DateTime.MinValue;
		private const int DASH_PUSH_MIN_MS = 150;
		private volatile bool _dashPushInFlight;

		private static readonly SolidColorBrush DashBg      = MakeFrozen(0xF0, 0x12, 0x16, 0x1C);
		private static readonly SolidColorBrush DashBorder  = MakeFrozen(0xFF, 0x1E, 0x3C, 0x4A);
		private static readonly SolidColorBrush DashTitleBg = MakeFrozen(0xFF, 0x12, 0x24, 0x2E);
		private static readonly SolidColorBrush DashTitleFg = MakeFrozen(0xFF, 0x44, 0xC8, 0xF2);
		private static readonly SolidColorBrush DashDimFg   = MakeFrozen(0xFF, 0x80, 0x9A, 0xA6);
		private static readonly SolidColorBrush DashSep     = MakeFrozen(0xFF, 0x22, 0x33, 0x3C);

		private static readonly SolidColorBrush BtnBeBg     = MakeFrozen(0xFF, 0x3A, 0x29, 0x0A);
		private static readonly SolidColorBrush BtnBeBdr    = MakeFrozen(0xFF, 0xE6, 0xA8, 0x3A);
		private static readonly SolidColorBrush BtnSlBg     = MakeFrozen(0xFF, 0x30, 0x0D, 0x0D);
		private static readonly SolidColorBrush BtnSlBdr    = MakeFrozen(0xFF, 0xC8, 0x20, 0x28);
		private static readonly SolidColorBrush BtnTpBg     = MakeFrozen(0xFF, 0x0D, 0x30, 0x1A);
		private static readonly SolidColorBrush BtnTpBdr    = MakeFrozen(0xFF, 0x28, 0xC8, 0x60);
		private static readonly SolidColorBrush BtnFg       = MakeFrozen(0xFF, 0xDA, 0xE4, 0xE8);

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
			GbPanelCorner corner = DashboardCorner;

			ChartControl.Dispatcher.InvokeAsync(() =>
			{
				try
				{
					if (_uiInitialized || _dashTornDown || State == State.Terminated)
						return;

					_dragThumb = new Thumb { Background = Brushes.Transparent, Cursor = Cursors.SizeAll };
					var thumbFac = new FrameworkElementFactory(typeof(Border));
					thumbFac.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Control.BackgroundProperty));
					_dragThumb.Template = new ControlTemplate(typeof(Thumb)) { VisualTree = thumbFac };
					_dragThumb.DragDelta += OnPanelDragDelta;

					var titleText = new TextBlock
					{
						Text = "UAI WRAPPER  v" + Version,
						Foreground = DashTitleFg,
						FontSize = 11,
						FontWeight = FontWeights.Bold,
						VerticalAlignment = VerticalAlignment.Center,
						HorizontalAlignment = HorizontalAlignment.Center,
						Margin = new Thickness(4, 0, 22, 0),
						IsHitTestVisible = false
					};

					_pillText = new TextBlock
					{
						Text = _dashMinimized ? "+" : "−",
						Foreground = DashDimFg,
						FontSize = 12,
						FontWeight = FontWeights.Bold,
						HorizontalAlignment = HorizontalAlignment.Center,
						VerticalAlignment = VerticalAlignment.Center,
						IsHitTestVisible = false
					};
					_pillBtn = new Border
					{
						Width = 18,
						Height = 16,
						Margin = new Thickness(0, 0, 6, 0),
						HorizontalAlignment = HorizontalAlignment.Right,
						VerticalAlignment = VerticalAlignment.Center,
						Background = Brushes.Transparent,
						Cursor = Cursors.Hand,
						ToolTip = _dashMinimized ? "Click to restore" : "Click to minimize",
						Child = _pillText
					};
					_pillBtn.MouseLeftButtonUp += OnPillMouseUp;

					var titleGrid = new Grid();
					titleGrid.Children.Add(_dragThumb);
					titleGrid.Children.Add(titleText);
					titleGrid.Children.Add(_pillBtn);

					_dashTitleBar = new Border
					{
						Background = DashTitleBg,
						Height = 22,
						CornerRadius = new CornerRadius(8, 8, 0, 0),
						Child = titleGrid,
						ToolTip = "Drag to move"
					};

					var statusRow = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 3, 0, 2) };
					_dashStatus = new TextBlock { Text = "FLAT", Foreground = Brushes.DimGray, FontSize = 12, FontWeight = FontWeights.Bold };
					statusRow.Children.Add(_dashStatus);

					_dashInstrument  = MakeInfoRow(DashDimFg);
					_dashConfirm     = MakeInfoRow(Brushes.WhiteSmoke);
					_dashWindow      = MakeInfoRow(Brushes.WhiteSmoke);
					_dashWindowState = MakeInfoRow(Brushes.Orange);
					_dashWindowNote  = MakeInfoRow(DashDimFg);
					_dashWindowNote.Text = "Note: window gates new entries only, not exits";
					_dashWindowNote.FontStyle = FontStyles.Italic;
					_dashAlerts      = MakeInfoRow(DashDimFg);

					_dashTradeInfo = new StackPanel { Orientation = Orientation.Vertical, Visibility = Visibility.Collapsed };
					_dashTradeInfo.Children.Add(MakeSeparator());
					_dashEntry  = MakeInfoRow(Brushes.WhiteSmoke); _dashTradeInfo.Children.Add(_dashEntry);
					_dashStop   = MakeInfoRow(Brushes.Salmon);     _dashTradeInfo.Children.Add(_dashStop);
					_dashTarget = MakeInfoRow(Brushes.LimeGreen);  _dashTradeInfo.Children.Add(_dashTarget);
					_dashQty    = MakeInfoRow(Brushes.WhiteSmoke); _dashTradeInfo.Children.Add(_dashQty);
					_dashPnl    = MakeInfoRow(Brushes.WhiteSmoke); _dashTradeInfo.Children.Add(_dashPnl);

					_beBtn = MakeDashButton("MOVE SL TO BE", 190, 24, BtnBeBg, BtnBeBdr);
					_beBtn.Click += OnBeClick;

					_slDownBtn = MakeDashButton("SL ▼", 44, 26, BtnSlBg, BtnSlBdr);
					_slUpBtn   = MakeDashButton("SL ▲", 44, 26, BtnSlBg, BtnSlBdr);
					_tpDownBtn = MakeDashButton("TP ▼", 44, 26, BtnTpBg, BtnTpBdr);
					_tpUpBtn   = MakeDashButton("TP ▲", 44, 26, BtnTpBg, BtnTpBdr);
					_slDownBtn.Click += OnSlDownClick;
					_slUpBtn.Click   += OnSlUpClick;
					_tpDownBtn.Click += OnTpDownClick;
					_tpUpBtn.Click   += OnTpUpClick;
					var nudgeRow = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 2, 0, 0) };
					nudgeRow.Children.Add(_slDownBtn);
					nudgeRow.Children.Add(_slUpBtn);
					nudgeRow.Children.Add(_tpDownBtn);
					nudgeRow.Children.Add(_tpUpBtn);

					_dashTradeInfo.Children.Add(MakeSeparator());
					_dashTradeInfo.Children.Add(_beBtn);
					_dashTradeInfo.Children.Add(nudgeRow);

					_dashBody = new StackPanel { Orientation = Orientation.Vertical, MinWidth = 200, Margin = new Thickness(0, 0, 0, 6) };
					_dashBody.Children.Add(statusRow);
					_dashBody.Children.Add(MakeSeparator());
					_dashBody.Children.Add(_dashInstrument);
					_dashBody.Children.Add(_dashConfirm);
					_dashBody.Children.Add(_dashWindow);
					_dashBody.Children.Add(_dashWindowState);
					_dashBody.Children.Add(_dashWindowNote);
					_dashBody.Children.Add(_dashAlerts);
					_dashBody.Children.Add(_dashTradeInfo);
					if (_dashMinimized)
						_dashBody.Visibility = Visibility.Collapsed;

					var main = new StackPanel { Orientation = Orientation.Vertical, MinWidth = 200 };
					main.Children.Add(_dashTitleBar);
					main.Children.Add(_dashBody);

					_dashPanel = new Border
					{
						HorizontalAlignment = HorizontalAlignment.Left,
						VerticalAlignment = VerticalAlignment.Top,
						Margin = new Thickness(10, 10, 0, 0),
						Background = DashBg,
						BorderBrush = DashBorder,
						BorderThickness = new Thickness(2),
						CornerRadius = new CornerRadius(10),
						ClipToBounds = true,
						Child = main
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
							case GbPanelCorner.TopRight:
								left = parent.ActualWidth - _dashPanel.ActualWidth - 10; break;
							case GbPanelCorner.BottomLeft:
								top = parent.ActualHeight - _dashPanel.ActualHeight - 10; break;
							case GbPanelCorner.BottomRight:
								left = parent.ActualWidth - _dashPanel.ActualWidth - 10;
								top = parent.ActualHeight - _dashPanel.ActualHeight - 10; break;
						}
						_dashPanel.Margin = new Thickness(Math.Max(0, left), Math.Max(0, top), 0, 0);
						_dashPanel.LayoutUpdated -= layoutHandler;
					};
					_dashPanel.LayoutUpdated += layoutHandler;

					UserControlCollection.Add(_dashPanel);
					_uiInitialized = true;
				}
				catch (Exception ex)
				{
					Print("[gbUaiWrapperStrategy] Dashboard create error: " + ex.Message);
				}
			});
		}

		private void RemoveDashboard()
		{
			_dashTornDown = true;
			if (ChartControl == null || (_dashPanel == null && _dragThumb == null))
				return;

			Action teardown = () =>
			{
				try
				{
					if (_dragThumb != null) _dragThumb.DragDelta -= OnPanelDragDelta;
					if (_pillBtn != null) _pillBtn.MouseLeftButtonUp -= OnPillMouseUp;
					if (_beBtn != null) _beBtn.Click -= OnBeClick;
					if (_slDownBtn != null) _slDownBtn.Click -= OnSlDownClick;
					if (_slUpBtn != null) _slUpBtn.Click -= OnSlUpClick;
					if (_tpDownBtn != null) _tpDownBtn.Click -= OnTpDownClick;
					if (_tpUpBtn != null) _tpUpBtn.Click -= OnTpUpClick;
					if (_dashPanel != null && UserControlCollection.Contains(_dashPanel))
						UserControlCollection.Remove(_dashPanel);
				}
				catch { }
				finally
				{
					_dragThumb = null;
					_pillBtn = null; _pillText = null;
					_beBtn = _slDownBtn = _slUpBtn = _tpDownBtn = _tpUpBtn = null;
					_dashStatus = null;
					_dashInstrument = _dashConfirm = _dashWindow = _dashWindowState = _dashWindowNote = _dashAlerts = null;
					_dashEntry = _dashStop = _dashTarget = _dashQty = _dashPnl = null;
					_dashTradeInfo = null;
					_dashBody = null;
					_dashTitleBar = null;
					_dashPanel = null;
					_uiInitialized = false;
				}
			};

			try
			{
				if (ChartControl.Dispatcher.CheckAccess()) teardown();
				else ChartControl.Dispatcher.Invoke(teardown);
			}
			catch { }
		}

		private void UpdateDashboard(bool force = false)
		{
			if (ChartControl == null || !_uiInitialized) return;

			DateTime nowUtc = DateTime.UtcNow;
			if (!force && (nowUtc - _lastDashPushUtc).TotalMilliseconds < DASH_PUSH_MIN_MS)
				return;
			if (_dashPushInFlight)
				return;
			_lastDashPushUtc = nowUtc;
			_dashPushInFlight = true;

			bool isLong = Position.MarketPosition == MarketPosition.Long;
			bool isShort = Position.MarketPosition == MarketPosition.Short;
			bool inPos = isLong || isShort;

			string posText = inPos ? (isLong ? "LONG" : "SHORT") : "FLAT";
			Brush posBrush = isLong ? Brushes.LimeGreen : isShort ? Brushes.Crimson : Brushes.DimGray;

			string instrText = "Instr  " + (Instrument != null && Instrument.MasterInstrument != null ? Instrument.MasterInstrument.Name : "?")
				+ "   Acct  " + (Account != null ? Account.Name : "?");
			string confirmText = string.Format("Confirm  {0} bar{1}", ConfirmationBars, ConfirmationBars == 1 ? "" : "s");

			string windowText = TimeFilterEnabled
				? string.Format("Window  {0:HH:mm}-{1:HH:mm}", StartTime, EndTime)
				: "Window  always on";
			string windowStateText;
			Brush windowStateBrush;
			if (!TimeFilterEnabled)
			{
				windowStateText = "Filter off — always armed";
				windowStateBrush = Brushes.LimeGreen;
			}
			else
			{
				bool inWin = IsWithinTimeWindow();
				windowStateText = inWin ? "Armed — entries enabled" : "Outside window";
				windowStateBrush = inWin ? Brushes.LimeGreen : Brushes.Orange;
			}

			string alertsText = "Alerts  " + (EnableIndicatorAlerts ? "ON" : "OFF");

			string entryText = "", stopText = "", tgtText = "", qtyText = "", pnlText = "";
			Brush pnlBrush = Brushes.WhiteSmoke;
			if (inPos)
			{
				double avg = Position.AveragePrice;
				int tdir = isLong ? 1 : -1;
				double stopPx = _currentStopPrice > 0 ? _currentStopPrice : avg - tdir * StopLossTicks * TickSize;
				double tgtPx = _currentTargetPrice > 0 ? _currentTargetPrice : avg + tdir * ProfitTargetTicks * TickSize;
				entryText = string.Format("Entry   {0:F2}", avg);
				stopText = string.Format("Stop    {0:F2}{1}", stopPx, _breakEvenSet ? "  (BE)" : "");
				tgtText = string.Format("Target  {0:F2}", tgtPx);
				qtyText = string.Format("Qty     {0}", Position.Quantity);
				double upnl = 0;
				try { upnl = Position.GetUnrealizedProfitLoss(PerformanceUnit.Currency, MarkPrice()); } catch { }
				pnlText = string.Format("uPnL    {0}${1:F0}", upnl < 0 ? "-" : "+", Math.Abs(upnl));
				pnlBrush = upnl >= 0 ? Brushes.LimeGreen : Brushes.Salmon;
			}

			string pt = posText, it = instrText, ct = confirmText, wt = windowText, wst = windowStateText, at = alertsText;
			string et = entryText, st = stopText, tgt = tgtText, qt = qtyText, plt = pnlText;
			Brush pb = posBrush, wsb = windowStateBrush, plb = pnlBrush;
			bool ip = inPos;

			ChartControl.Dispatcher.InvokeAsync(() =>
			{
				try
				{
					if (!_uiInitialized) return;
					if (_dashStatus != null) { _dashStatus.Text = pt; _dashStatus.Foreground = pb; }
					if (_dashInstrument != null) _dashInstrument.Text = it;
					if (_dashConfirm != null) _dashConfirm.Text = ct;
					if (_dashWindow != null) _dashWindow.Text = wt;
					if (_dashWindowState != null) { _dashWindowState.Text = wst; _dashWindowState.Foreground = wsb; }
					if (_dashAlerts != null) _dashAlerts.Text = at;
					if (_dashTradeInfo != null)
						_dashTradeInfo.Visibility = ip ? Visibility.Visible : Visibility.Collapsed;
					if (ip)
					{
						if (_dashEntry != null) _dashEntry.Text = et;
						if (_dashStop != null) _dashStop.Text = st;
						if (_dashTarget != null) _dashTarget.Text = tgt;
						if (_dashQty != null) _dashQty.Text = qt;
						if (_dashPnl != null) { _dashPnl.Text = plt; _dashPnl.Foreground = plb; }
					}
				}
				catch { }
				finally { _dashPushInFlight = false; }
			});
		}
		#endregion

		#region Dashboard widgets + handlers
		private static TextBlock MakeInfoRow(Brush foreground)
		{
			return new TextBlock
			{
				Text = "",
				Foreground = foreground,
				FontSize = 11,
				FontFamily = new System.Windows.Media.FontFamily("Consolas"),
				HorizontalAlignment = HorizontalAlignment.Left,
				Margin = new Thickness(12, 1, 12, 1)
			};
		}

		private static Border MakeSeparator()
		{
			return new Border { Height = 1, Background = DashSep, Margin = new Thickness(6, 3, 6, 3) };
		}

		private static Button MakeDashButton(string label, double width, double height, Brush bg, Brush bdr)
		{
			var btn = new Button
			{
				Width = width,
				Height = height,
				Margin = new Thickness(2),
				MinWidth = 0,
				Cursor = Cursors.Hand,
				FocusVisualStyle = null,
				Padding = new Thickness(0),
				Background = bg,
				BorderBrush = bdr,
				BorderThickness = new Thickness(1),
				Foreground = BtnFg,
				FontSize = 11,
				FontWeight = FontWeights.Bold,
				Content = label
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
			hf.SetValue(Border.BackgroundProperty, new SolidColorBrush(Color.FromArgb(40, 0xFF, 0xFF, 0xFF)));
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

		private void OnBeClick(object sender, RoutedEventArgs e)
		{
			_pendingBE = true;
		}

		private void OnSlDownClick(object sender, RoutedEventArgs e)
		{
			System.Threading.Interlocked.Add(ref _pendingSlNudgeTicks, -Math.Max(1, ManualNudgeTicks));
		}

		private void OnSlUpClick(object sender, RoutedEventArgs e)
		{
			System.Threading.Interlocked.Add(ref _pendingSlNudgeTicks, Math.Max(1, ManualNudgeTicks));
		}

		private void OnTpDownClick(object sender, RoutedEventArgs e)
		{
			System.Threading.Interlocked.Add(ref _pendingTpNudgeTicks, -Math.Max(1, ManualNudgeTicks));
		}

		private void OnTpUpClick(object sender, RoutedEventArgs e)
		{
			System.Threading.Interlocked.Add(ref _pendingTpNudgeTicks, Math.Max(1, ManualNudgeTicks));
		}

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

		private void OnPillMouseUp(object sender, MouseButtonEventArgs e)
		{
			e.Handled = true;
			_dashMinimized = !_dashMinimized;
			if (_dashBody != null)
				_dashBody.Visibility = _dashMinimized ? Visibility.Collapsed : Visibility.Visible;
			if (_pillText != null)
				_pillText.Text = _dashMinimized ? "+" : "−";
			if (_pillBtn != null)
				_pillBtn.ToolTip = _dashMinimized ? "Click to restore" : "Click to minimize";
		}
		#endregion
	}
}
