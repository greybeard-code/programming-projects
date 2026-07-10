// ============================================================================
//  Terminator_V2 — v2.4.2  (2026-07-09)
//  ATR trailing-stop stop-and-reverse strategy with full risk/exit/filter
//  controls, an on-chart dashboard, and engine plots.
//
//  v2.4.2: TIME FILTER ENTRIES-ONLY mode (Time Filter Entries Only). When on,
//   the time window gates only NEW entries — an opposite SAR signal still
//   flattens the live position outside the window (the reversal EXIT always
//   manages), and the window-end auto-flatten is disabled so a position may
//   carry across the out-of-window gap until a signal, a hard stop, or the
//   session close. This reproduces the Python backtester's recommended config
//   (entries-only windows), which the prior window logic could NOT: the old
//   FlattenAtEnd=true force-flattened at window end (measured -28% P&L on the
//   MNQ r100-4 champion) and FlattenAtEnd=false blocked the reversal exit
//   outside the window (measured: breached the $2,000 Apex trailing floor).
//   Default OFF — existing templates behave exactly as before.
//
//  v2.4.1: fix historical Day PnL bleeding into realtime when a position is CARRIED across the
//   historical->realtime boundary. The carried-position unrealized baseline was captured inside
//   OnStateChange(Realtime), where GetUnrealizedProfitLoss is unreliable (read 0), so the
//   position's full pre-live unrealized leaked into Day PnL — and if it crossed the daily-profit
//   target it falsely latched tradingBlocked and halted entries. Baseline is now captured at the
//   FIRST realtime evaluation (EnsureCarriedBaseline), before the discard/close path can fire.
//   Also: a DISCARDED carried position now contributes exactly $0 (Day PnL held at 0 while the
//   discard is pending + re-baseline on completion); previously the virtual drift between go-live
//   and the discard fill leaked in.
//
//  v2.4.0: MANUAL LIVE BRACKETS + EMA-SL buffer.
//   - Move a live trade's SL/TP by DRAGGING a chart line OR with dashboard nudge
//     buttons (SL ▼/▲, TP ▼/▲, step = Manual Nudge Ticks). Both funnel through one
//     OCO-safe apply path (SetStopLoss/SetProfitTarget on the entry signal name) with
//     the same wrong-side guard as MOVE SL TO BE. A line == a real working order.
//   - Manual-vs-auto conflict is a user setting (Manual Stop Mode): ManualTakesOver
//     (auto BE/trail stops managing once you touch the stop), AutoKeepsTightening
//     (auto can still ratchet tighter past your level), or ManualTightenOnly
//     (manual may only tighten). Latches reset on each new entry.
//   - EMA buffers: SL EMA (SlEmaBufferTicks) places the INITIAL stop X ticks BEYOND the EMA;
//     Trail EMA (TrailEmaBufferTicks) RIDES the stop X ticks beyond the trail EMA each bar —
//     both below for longs / above for shorts so noise around the line doesn't stop you out.
//     Each is independent; both folded into RiskBased sizing.
//
//  v2.3.3: draw Buy/Sell tags at the SIGNAL cross (OnBarUpdate) so they render on historical
//  bars (a chart strategy doesn't execute historical trades, so entry-time tags only showed live).
//
//  v2.3.2: historical Buy/Sell tags fix - the rolling-cleanup cap (400) was deleting older
//  historical entry tags when entries are frequent; raised to 5000 and never trim during the
//  historical replay (only bound live growth).
//
//  v2.3.1: plot the SL EMA line when SL Mode = EMA (Salmon).
//
//  v2.3.0: settings grid now hides irrelevant rows per mode/toggle (ICustomTypeDescriptor);
//  Buy/Sell entry tags draw in OnBarUpdate again so they render in historical too.
//
//  v2.2.1: review fixes - optimization-iteration state reset, realtime-only filter markers,
//  DailyLoss/Profit=0 guard, Day PnL committed once (no compute on the display path).
//
//  v2.2.0: SL EMA mode (stop rides an EMA, own period); correct historical->realtime handling
//  of the daily PnL limit + dashboard (live Day PnL starts at $0; a carried position shows as
//  HIST and is excluded); refreshed amber/graphite dashboard theme.
//
//  v2.1.0 highlights (see CHANGELOG.md):
//   - Reversal is now a CLEAN SPLIT (close on the signal bar, re-enter the new
//     direction once flat) so every order shows your configured Quantity — no
//     more 2x close+reverse order. Net size is hard-capped; flip/oversize guard
//     flattens anything beyond Quantity. (There is NO martingale anywhere.)
//   - Orphan-order sweep cancels any lingering TP/SL when the position is flat
//     (defense-in-depth; the TP+stop already share an OCO via one signal name).
//   - Hardened per AGENTS.md: every lifecycle override is try/caught, frozen
//     WPF brushes, dispatcher throttle + in-flight gate, rolling draw cleanup.
//   - New EMA profit-trail (TrailMode = EMA, EMA 50 default) alongside ATR/Ticks
//     trails; the stop rides the EMA line, tightening only.
//   - Plots: ATR trailing-stop line + VWMA (when enabled) + Trail EMA (when used);
//     reliable Buy/Sell entry tags at fill locations; filter-block markers explain
//     "no entry".
//   - MafiaFlipSwitch-style dashboard: AUTO / LONG / SHORT / MOVE SL TO BE /
//     FLATTEN ALL + live status rows.
// ============================================================================
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

// Enums used as [NinjaScriptProperty] params live in the parent NinjaTrader.NinjaScript
// namespace so NT8's zip-import cache-accessor regen (which emits unqualified type refs)
// resolves them — see AGENTS.md "NinjaScript Export" gotcha, Fix #2.
namespace NinjaTrader.NinjaScript
{
	public enum TtExitMode { Off, ATR, Ticks, Currency }
	public enum TtSimpleMode { Off, ATR, Ticks }
	// TrailMode adds EMA (profit-trail the stop along an EMA line). Off/ATR/Ticks keep the
	// same ordinals as TtSimpleMode so existing templates deserialize unchanged.
	public enum TtTrailMode { Off, ATR, Ticks, EMA }
	// SL adds EMA (place the stop at an EMA level). Off/ATR/Ticks/Currency keep the same
	// ordinals as TtExitMode so existing SL templates deserialize unchanged.
	public enum TtSlMode { Off, ATR, Ticks, Currency, EMA }
	public enum TtVwmaMode { Off, TrendGate, SignalSource }
	public enum TtSizingMode { Fixed, RiskBased }
	public enum TtPanelCorner { TopLeft, TopRight, BottomLeft, BottomRight }
	// How a manual SL move interacts with auto BE/trail (see ManageStops / ApplyManualStop):
	//   ManualTakesOver     — once the stop is moved by hand, auto BE/trail stop managing it.
	//   AutoKeepsTightening — manual applies now; auto can still ratchet tighter past it later.
	//   ManualTightenOnly   — a manual move may only tighten (never loosen); auto runs normally.
	public enum TtManualStopConflict { ManualTakesOver, AutoKeepsTightening, ManualTightenOnly }
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public class Terminator_V2 : Strategy, ICustomTypeDescriptor
	{
		private ATR atr;
		private VWMA outVwma;
		private SMA volAvg;
		private EMA trailEma;       // profit-trail basis when TrailMode == EMA
		private EMA slEma;          // stop basis when SlMode == EMA

		// ATR trailing-stop signal engine (the entry trigger line)
		private double xAtrTrailingStop;
		private double xAtrTrailingStopLast;
		private bool _isInitialized;
		private int lastEntryBar = -1;

		// Clean-split reversal: on an opposite signal we flatten on the signal bar
		// and queue the new-direction entry for when the position is confirmed flat.
		private int pendingReverseDir;          // +1 / -1 / 0
		private int pendingReverseBar = -1;
		private const int REVERSE_MAX_DELAY_BARS = 5;   // drop a stale queued reverse

		// Daily risk
		private volatile bool tradingBlocked;   // read/written from OnBarUpdate AND OnMarketData
		private double sessionStartCumProfit;
		private double dailyPnl;   // committed once by ComputeDayPnL; the dashboard reads this (no compute on the display path)
		private bool firstSession = true;
		private int dailyEntryCount;
		private int lastSessionResetBar = -1;   // one reset per session bar

		// Historical->realtime transition: a position carried across the boundary was simulated
		// during warmup, not opened live (MafiaFlipSwitch pattern — the tricky bit).
		private bool carryingPosition;
		private double carriedUnrealizedBaseline;  // its unrealized PnL at the boundary, excluded from live Day PnL
		private bool _discardCarriedPending;       // flatten the carried position on the first realtime bar
		private bool _carriedBaselinePending;      // capture the carried baseline on the FIRST realtime eval, not in OnStateChange (where GetUnrealizedProfitLoss is unreliable -> 0 -> historical bleed)

		// Managed-stop state (one unified stop, per pattern doc §5 / Template D)
		private double entryPrice;
		private double currentStopPrice;
		private string currentSignalName = "";
		private int initialStopTicks;
		private bool breakEvenSet;
		private bool _riskWarned;
		private bool _capWarned;

		// Size / orphan guards
		private int _intendedQty;               // size we asked for; net must never exceed it
		private int _lastOrphanSweepBar = -1;
		private bool _oversizeFlattenAttempted; // latch so the guard flattens once, not every tick

		// Live price for marking PnL between bar closes (Calculate.OnBarClose primary)
		private double _lastTradePrice;

		// Manual live brackets (draggable lines + nudge buttons). A line == a real working order.
		private double currentTargetPrice;          // tracked TP price (0 = none/unknown); dashboard + TP line read this
		private bool _stopManuallyMoved;            // latch: stop moved by hand this trade (ManualTakesOver / "(M)" marker)
		private bool _targetManuallyMoved;
		private int _pendingStopNudgeTicks;         // UI thread accumulates via Interlocked; strategy thread drains
		private int _pendingTargetNudgeTicks;
		private double _slLineLastPrice;            // shadow of what WE last wrote to each line (drag-detection baseline)
		private double _tpLineLastPrice;
		private double _slPendingPx;                // 1-cycle debounce of an in-progress drag (0 = none)
		private double _tpPendingPx;
		private bool _manualLinesPresent;           // gate so we don't RemoveDrawObject every flat tick
		private DateTime _lastManualSvcUtc = DateTime.MinValue;
		private const int MANUAL_SVC_MIN_MS = 150;  // wall-clock throttle for drag read-back (playback-lockup rule)
		private const string SL_LINE_TAG = "TtManualSL";
		private const string TP_LINE_TAG = "TtManualTP";

		// Plot indexes
		private const int PLOT_TRAIL    = 0;
		private const int PLOT_VWMA     = 1;
		private const int PLOT_TRAILEMA = 2;
		private const int PLOT_SLEMA    = 3;

		// Rolling draw-object cleanup (unique-per-bar tags would otherwise accumulate
		// into a "Not Enough Quota" crash — AGENTS.md WPF quota rule).
		private readonly Queue<string> _drawTags = new Queue<string>();
		private const int MAX_DRAW_TAGS = 5000;
		private SimpleFont _markerFont;

		// Error budgets (a flood of identical Prints saturates the logger mutex)
		private int _onStateErrors;
		private int _onBarErrors;
		private int _onOrderRejects;

		#region Parameters
		[NinjaScriptProperty]
		[Display(Name = "ATR Period", GroupName = "1. Core", Order = 0)]
		public int ATRPeriod { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "ATR Multiplier", GroupName = "1. Core", Order = 1)]
		public double ATRMult { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Quantity", GroupName = "1. Core", Order = 2)]
		public int Quantity { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Source", GroupName = "2. Signal", Order = 0)]
		public PriceType Source { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "VWMA Mode", GroupName = "2. Signal", Order = 1)]
		[RefreshProperties(RefreshProperties.All)]
		public TtVwmaMode VwmaMode { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "VWMA Period", GroupName = "2. Signal", Order = 2)]
		public int VwmaPeriod { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Volume Filter", GroupName = "3. Filters", Order = 0)]
		[RefreshProperties(RefreshProperties.All)]
		public bool VolumeFilter { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Volume SMA Period", GroupName = "3. Filters", Order = 1)]
		public int VolumeSmaPeriod { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Volume Multiplier", GroupName = "3. Filters", Order = 2)]
		public double VolumeMult { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Enable Longs", GroupName = "3. Filters", Order = 3)]
		public bool EnableLongs { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Enable Shorts", GroupName = "3. Filters", Order = 4)]
		public bool EnableShorts { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "TP Mode", GroupName = "4. Take-Profit", Order = 0)]
		[RefreshProperties(RefreshProperties.All)]
		public TtExitMode TpMode { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "TP Value", GroupName = "4. Take-Profit", Order = 1)]
		public double TpValue { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "SL Mode", GroupName = "5. Stop-Loss", Order = 0)]
		[RefreshProperties(RefreshProperties.All)]
		public TtSlMode SlMode { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "SL Value (ATR/Ticks/$)", GroupName = "5. Stop-Loss", Order = 1)]
		public double SlValue { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "SL EMA Period", GroupName = "5. Stop-Loss", Order = 2)]
		public int SlEmaPeriod { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "SL EMA Buffer (ticks)", GroupName = "5. Stop-Loss", Order = 3)]
		public int SlEmaBufferTicks { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Breakeven Mode", GroupName = "6. Breakeven", Order = 0)]
		[RefreshProperties(RefreshProperties.All)]
		public TtSimpleMode BeMode { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "BE Trigger", GroupName = "6. Breakeven", Order = 1)]
		public double BeTrigger { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "BE Offset (ticks)", GroupName = "6. Breakeven", Order = 2)]
		public int BeOffsetTicks { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Trail Mode", GroupName = "7. Auto-Trail", Order = 0)]
		[RefreshProperties(RefreshProperties.All)]
		public TtTrailMode TrailMode { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Trail Value (ATR/Ticks)", GroupName = "7. Auto-Trail", Order = 1)]
		public double TrailValue { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Trail EMA Period", GroupName = "7. Auto-Trail", Order = 2)]
		public int TrailEmaPeriod { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Trail EMA Buffer (ticks)", GroupName = "7. Auto-Trail", Order = 3)]
		public int TrailEmaBufferTicks { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Use Daily Loss Limit", GroupName = "8. Daily Risk", Order = 0)]
		[RefreshProperties(RefreshProperties.All)]
		public bool UseDailyLoss { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Daily Loss ($)", GroupName = "8. Daily Risk", Order = 1)]
		public double DailyLoss { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Use Daily Profit Target", GroupName = "8. Daily Risk", Order = 2)]
		[RefreshProperties(RefreshProperties.All)]
		public bool UseDailyProfit { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Daily Profit ($)", GroupName = "8. Daily Risk", Order = 3)]
		public double DailyProfit { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Flatten On Daily Limit", GroupName = "8. Daily Risk", Order = 4)]
		public bool DailyFlatten { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Max Trades Per Day (0=off)", GroupName = "8. Daily Risk", Order = 5)]
		public int MaxTradesPerDay { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Discard Carried Pos At Realtime", GroupName = "8. Daily Risk", Order = 6)]
		public bool DiscardCarriedAtRealtime { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Sizing Mode", GroupName = "9. Sizing", Order = 0)]
		[RefreshProperties(RefreshProperties.All)]
		public TtSizingMode SizingMode { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Risk Per Trade ($)", GroupName = "9. Sizing", Order = 1)]
		public double RiskPerTrade { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Max Contracts (hard cap)", GroupName = "9. Sizing", Order = 2)]
		public int MaxContracts { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Use Time Filter", GroupName = "10. Session", Order = 0)]
		[RefreshProperties(RefreshProperties.All)]
		public bool UseTimeFilter { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Start Time (HHMMSS)", GroupName = "10. Session", Order = 1)]
		public int StartTime { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "End Time (HHMMSS)", GroupName = "10. Session", Order = 2)]
		public int EndTime { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Flatten At Window End", GroupName = "10. Session", Order = 3)]
		public bool FlattenAtEnd { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Time Filter Entries Only", GroupName = "10. Session", Order = 4)]
		public bool TimeFilterEntriesOnly { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Cooldown Bars", GroupName = "11. Misc", Order = 0)]
		public int CooldownBars { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show Entry Tags", GroupName = "11. Misc", Order = 1)]
		public bool ShowMarkers { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Use Alerts", GroupName = "11. Misc", Order = 2)]
		public bool UseAlerts { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show Dashboard", GroupName = "12. Dashboard", Order = 0)]
		[RefreshProperties(RefreshProperties.All)]
		public bool ShowDashboard { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Start Minimized", GroupName = "12. Dashboard", Order = 1)]
		public bool DashboardStartMinimized { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Dashboard Corner", GroupName = "12. Dashboard", Order = 2)]
		public TtPanelCorner DashboardCorner { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Plot ATR Trail Line", GroupName = "13. Plots", Order = 0)]
		public bool ShowTrailPlot { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Plot VWMA Line", GroupName = "13. Plots", Order = 1)]
		public bool ShowVwmaPlot { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show Filter-Block Markers", GroupName = "13. Plots", Order = 2)]
		public bool ShowFilterMarkers { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Enable Manual Brackets", GroupName = "14. Manual Control", Order = 0)]
		[RefreshProperties(RefreshProperties.All)]
		public bool EnableManualBrackets { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Manual Nudge Ticks", GroupName = "14. Manual Control", Order = 1)]
		public int ManualNudgeTicks { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Manual Stop Mode", GroupName = "14. Manual Control", Order = 2)]
		public TtManualStopConflict ManualStopMode { get; set; }

		// Read-only build stamp — shows in the settings grid, never serialized (no setter,
		// no [NinjaScriptProperty]). Keep in sync with the header banner and CHANGELOG.md.
		[Display(Name = "Version", GroupName = "ZZ. About", Order = 0)]
		public string Version
		{
			get { return "2.4.2"; }
		}
		#endregion

		// ====================================================================
		//  Lifecycle (every override try/caught — an unhandled throw counts
		//  toward NT8's MaxRestarts and can silently disable the strategy)
		// ====================================================================
		protected override void OnStateChange()
		{
			try { ProcessStateChange(); }
			catch (Exception ex)
			{
				if (_onStateErrors++ < 5)
					Print("[Terminator_V2] OnStateChange(" + State + ") error: " + ex.GetType().Name + " " + ex.Message);
			}
		}

		private void ProcessStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description = "ATR trailing-stop stop-and-reverse with risk/exit/filter controls, dashboard, and engine plots (V2)";
				Name = "Terminator_V2";
				Calculate = Calculate.OnBarClose;
				EntriesPerDirection = 1;
				EntryHandling = EntryHandling.AllEntries;
				IsExitOnSessionCloseStrategy = true;
				ExitOnSessionCloseSeconds = 30;
				StartBehavior = StartBehavior.WaitUntilFlat;
				TimeInForce = TimeInForce.Gtc;
				// ByStrategyPosition + single managed stop (see Order Management Pattern §1/§5)
				StopTargetHandling = StopTargetHandling.ByStrategyPosition;
				// IgnoreAllErrors (not StopCancelClose): a stray reject must never auto-flatten
				// and TERMINATE the strategy — that self-termination is what froze the dashboard
				// and looked like "the strategy stopped working". Rejects are surfaced in
				// OnOrderUpdate; wrong-side stops are prevented by clamping in ManageStops.
				RealtimeErrorHandling = RealtimeErrorHandling.IgnoreAllErrors;
				BarsRequiredToTrade = 20;
				IsInstantiatedOnEachOptimizationIteration = true;   // fresh instance per optimization pass (no stale daily-risk/session state leak)
				IsOverlay = true;   // render the AddPlot lines on the PRICE panel (Strategy plots default to a sub-panel)

				ATRPeriod = 20;
				ATRMult = 4.0;
				Quantity = 1;

				Source = PriceType.Close;
				VwmaMode = TtVwmaMode.Off;
				VwmaPeriod = 20;

				VolumeFilter = false;
				VolumeSmaPeriod = 20;
				VolumeMult = 1.0;
				EnableLongs = true;
				EnableShorts = true;

				TpMode = TtExitMode.Off;
				TpValue = 2.0;
				SlMode = TtSlMode.Off;
				SlValue = 2.0;
				SlEmaPeriod = 50;
				SlEmaBufferTicks = 0;   // 0 = stop sits exactly on the EMA (backward-compatible)
				BeMode = TtSimpleMode.Off;
				BeTrigger = 1.0;
				BeOffsetTicks = 2;
				TrailMode = TtTrailMode.Off;
				TrailValue = 2.0;
				TrailEmaPeriod = 50;
				TrailEmaBufferTicks = 0;   // 0 = stop rides exactly on the trail EMA (backward-compatible)

				UseDailyLoss = false;
				DailyLoss = 500;
				UseDailyProfit = false;
				DailyProfit = 500;
				DailyFlatten = false;
				MaxTradesPerDay = 0;
				DiscardCarriedAtRealtime = true;

				SizingMode = TtSizingMode.Fixed;
				RiskPerTrade = 200;
				MaxContracts = 10;

				UseTimeFilter = false;
				StartTime = 93000;
				EndTime = 160000;
				FlattenAtEnd = true;
				TimeFilterEntriesOnly = false;

				CooldownBars = 0;
				ShowMarkers = true;
				UseAlerts = false;

				ShowDashboard = true;
				DashboardStartMinimized = false;
				DashboardCorner = TtPanelCorner.TopLeft;

				ShowTrailPlot = true;
				ShowVwmaPlot = true;
				ShowFilterMarkers = true;

				EnableManualBrackets = true;
				ManualNudgeTicks = 4;
				ManualStopMode = TtManualStopConflict.ManualTakesOver;

				// Engine plots (rendered on the price panel). Unused lines are NaN-gapped.
				AddPlot(new Stroke(Brushes.DodgerBlue, 2), PlotStyle.Line, "ATR Trail");
				AddPlot(new Stroke(Brushes.Goldenrod, 1), PlotStyle.Line, "VWMA");
				AddPlot(new Stroke(Brushes.MediumPurple, 1), PlotStyle.Line, "Trail EMA");
				AddPlot(new Stroke(Brushes.Salmon, 1), PlotStyle.Line, "SL EMA");
			}
			else if (State == State.Configure)
			{
				// Seed the runtime dashboard toggles from the persisted properties.
				_autoEnabled  = true;
				_longEnabled  = EnableLongs;
				_shortEnabled = EnableShorts;
			}
			else if (State == State.DataLoaded)
			{
				atr = ATR(Math.Max(1, ATRPeriod));
				if (VolumeFilter)
					volAvg = SMA(Volume, Math.Max(1, VolumeSmaPeriod));
				if (VwmaMode != TtVwmaMode.Off)
					outVwma = VWMA(GetSourceSeries(), Math.Max(1, VwmaPeriod));
				if (TrailMode == TtTrailMode.EMA)
					trailEma = EMA(Math.Max(1, TrailEmaPeriod));
				if (SlMode == TtSlMode.EMA)
					slEma = EMA(Math.Max(1, SlEmaPeriod));
				_markerFont = new SimpleFont("Montserrat", 12);
			}
			else if (State == State.Realtime)
			{
				// Day PnL starts at $0 the moment we go live — trades SIMULATED during the
				// enable-time historical replay must NOT leak into the live daily limit
				// (the historical->realtime gotcha; MafiaFlipSwitch / GodZilla pattern).
				sessionStartCumProfit = SystemPerformance.AllTrades.TradesPerformance.Currency.CumProfit;
				dailyEntryCount = 0;
				tradingBlocked = false;
				pendingReverseDir = 0;
				ResetTradeState();

				// A position still open at the boundary was inherited/simulated under WaitUntilFlat,
				// not opened live. Exclude its unrealized PnL from the live Day PnL, and (optionally)
				// flatten it so live trading starts truly flat.
				carryingPosition = Position != null && Position.MarketPosition != MarketPosition.Flat;
				carriedUnrealizedBaseline = 0.0;
				// DEFER the baseline capture: GetUnrealizedProfitLoss() inside OnStateChange(Realtime) can
				// return 0/stale (position + market data not ready), which leaks the carried position's
				// pre-live unrealized into Day PnL. EnsureCarriedBaseline() captures it at the first
				// realtime evaluation instead, BEFORE the discard/close path can fire.
				_carriedBaselinePending = carryingPosition;
				_discardCarriedPending = carryingPosition && DiscardCarriedAtRealtime;

				if (ShowDashboard)
					CreateDashboard();
			}
			else if (State == State.Terminated)
			{
				RemoveManualLines();
				RemoveDashboard();
			}
		}

		// ====================================================================
		//  Signal-engine + filter helpers
		// ====================================================================
		private ISeries<double> GetSourceSeries()
		{
			switch (Source)
			{
				case PriceType.High: return High;
				case PriceType.Low: return Low;
				case PriceType.Open: return Open;
				case PriceType.Median: return Median;
				case PriceType.Typical: return Typical;
				case PriceType.Weighted: return Weighted;
				default: return Close;
			}
		}

		private double SignalPrice(int barsAgo)
		{
			return (VwmaMode == TtVwmaMode.SignalSource && outVwma != null) ? outVwma[barsAgo] : Close[barsAgo];
		}

		private bool InWindow()
		{
			int t = ToTime(Time[0]);
			if (StartTime <= EndTime) return t >= StartTime && t <= EndTime;
			return t >= StartTime || t <= EndTime;
		}

		private double TickValue()
		{
			return Instrument.MasterInstrument.PointValue * TickSize;
		}

		private int ToTicks(TtExitMode mode, double value)
		{
			double ticks;
			switch (mode)
			{
				case TtExitMode.ATR: ticks = value * atr[0] / TickSize; break;
				case TtExitMode.Ticks: ticks = value; break;
				case TtExitMode.Currency: ticks = value / Math.Max(TickValue(), 1e-9); break;
				default: return 0;
			}
			return Math.Max(1, (int)Math.Round(ticks));
		}

		// SL ATR/Ticks/$ -> tick distance. EMA returns 0 (its level is the EMA line itself,
		// handled in InitialStopTicks / EnterDirection).
		private int ToTicks(TtSlMode mode, double value)
		{
			switch (mode)
			{
				case TtSlMode.ATR: return Math.Max(1, (int)Math.Round(value * atr[0] / TickSize));
				case TtSlMode.Ticks: return Math.Max(1, (int)Math.Round(value));
				case TtSlMode.Currency: return Math.Max(1, (int)Math.Round(value / Math.Max(TickValue(), 1e-9)));
				default: return 0;
			}
		}

		private int ToTicks(TtSimpleMode mode, double value)
		{
			switch (mode)
			{
				case TtSimpleMode.ATR: return Math.Max(1, (int)Math.Round(value * atr[0] / TickSize));
				case TtSimpleMode.Ticks: return Math.Max(1, (int)Math.Round(value));
				default: return 0;
			}
		}

		// Tick distance for the ATR/Ticks trail modes. EMA returns 0 — its stop level is the
		// EMA line itself (handled directly in ManageStops/InitialStopTicks), not a fixed distance.
		private int ToTicks(TtTrailMode mode, double value)
		{
			switch (mode)
			{
				case TtTrailMode.ATR: return Math.Max(1, (int)Math.Round(value * atr[0] / TickSize));
				case TtTrailMode.Ticks: return Math.Max(1, (int)Math.Round(value));
				default: return 0;
			}
		}

		// The protective stop distance actually used at entry: SL if set, else the Trail
		// distance, else the BE trigger distance. 0 means no managed stop (rely on reversal/TP).
		private int InitialStopTicks()
		{
			if (SlMode == TtSlMode.EMA)
			{
				// SL distance = price-to-EMA gap PLUS the buffer (the buffer always pushes the stop
				// further from price), used for RiskBased sizing + the stop seed.
				if (slEma != null && slEma.Count > 0)
					return Math.Max(1, (int)Math.Round(Math.Abs(Close[0] - slEma[0]) / TickSize) + Math.Max(0, SlEmaBufferTicks));
				return 0;
			}
			if (SlMode != TtSlMode.Off) return ToTicks(SlMode, SlValue);
			if (TrailMode == TtTrailMode.EMA)
			{
				// Initial protective distance for an EMA trail = price-to-EMA gap PLUS the trail buffer
				// (the buffer always pushes the stop further from price), so the seed matches the trail.
				if (trailEma != null && trailEma.Count > 0)
					return Math.Max(1, (int)Math.Round(Math.Abs(Close[0] - trailEma[0]) / TickSize) + Math.Max(0, TrailEmaBufferTicks));
				return 0;
			}
			if (TrailMode != TtTrailMode.Off) return ToTicks(TrailMode, TrailValue);
			if (BeMode != TtSimpleMode.Off) return ToTicks(BeMode, BeTrigger);
			return 0;
		}

		// "" = entry allowed; otherwise a short reason for the filter-block marker.
		// Cooldown is handled by the caller (it must only gate fresh-from-flat entries).
		private string EntryBlockReason(int dir, bool ignoreWindow = false)
		{
			if (!_autoEnabled) return "auto-off";
			if (!ignoreWindow && UseTimeFilter && !InWindow()) return "window";
			if (dir > 0 && (!EnableLongs || !_longEnabled)) return "long-off";
			if (dir < 0 && (!EnableShorts || !_shortEnabled)) return "short-off";
			if (tradingBlocked) return "daily";
			if (MaxTradesPerDay > 0 && dailyEntryCount >= MaxTradesPerDay) return "max-trades";
			if (VolumeFilter && volAvg != null && !(Volume[0] > volAvg[0] * VolumeMult)) return "vol";
			if (VwmaMode == TtVwmaMode.TrendGate && outVwma != null)
			{
				if (dir > 0 && !(Close[0] > outVwma[0])) return "vwma";
				if (dir < 0 && !(Close[0] < outVwma[0])) return "vwma";
			}
			MarketPosition mp = Position.MarketPosition;
			if (dir > 0 && mp == MarketPosition.Long) return "in-long";
			if (dir < 0 && mp == MarketPosition.Short) return "in-short";
			return "";
		}

		private int ComputeQuantity(int dir)
		{
			int qty;
			if (SizingMode == TtSizingMode.RiskBased)
			{
				int stopTicks = InitialStopTicks();
				if (stopTicks <= 0)
				{
					if (!_riskWarned)
					{
						Print("Terminator_V2: RiskBased sizing needs a stop (SL/Trail/BE) enabled; using fixed Quantity.");
						_riskWarned = true;
					}
					qty = Math.Max(1, Quantity);
				}
				else
				{
					double perContractRisk = stopTicks * TickValue();
					qty = perContractRisk <= 0
						? Math.Max(1, Quantity)
						: Math.Max(1, (int)Math.Floor(RiskPerTrade / perContractRisk));
				}
			}
			else
			{
				qty = Math.Max(1, Quantity);
			}
			// HARD CEILING. RiskBased sizing off a tiny stop (e.g. price near the EMA trail, or a
			// 1-tick stop) can blow the contract count sky-high; the size guard can only cap at the
			// size we ASK for, so the absolute bound must live here. Applies to Fixed too (fat-finger).
			if (MaxContracts > 0 && qty > MaxContracts)
			{
				if (!_capWarned)
				{
					Print("Terminator_V2: sizing " + qty + " capped to MaxContracts=" + MaxContracts + ".");
					_capWarned = true;
				}
				qty = MaxContracts;
			}
			return qty;
		}

		// ====================================================================
		//  Stop management (BE + trailing) — all via SetStopLoss(Price) so the
		//  stop and TP keep ONE signal name and stay an OCO pair (a stop fill
		//  auto-cancels the TP). Never SetTrailStop (silently ignored once
		//  SetStopLoss is in play — AGENTS.md gotcha #0). Stops only TIGHTEN.
		// ====================================================================
		private void ManageStops()
		{
			if (Position.MarketPosition == MarketPosition.Flat) return;
			if (BeMode == TtSimpleMode.Off && TrailMode == TtTrailMode.Off) return;
			if (currentSignalName.Length == 0 || entryPrice <= 0) return;
			// ManualTakesOver: once the operator moved the stop by hand, auto BE/trail stop managing
			// this position (the manual stop stands until fill/flatten). Latch resets on next entry.
			if (ManualStopMode == TtManualStopConflict.ManualTakesOver && _stopManuallyMoved) return;

			bool isLong = Position.MarketPosition == MarketPosition.Long;
			double price = MarkPrice();
			double profitTicks = isLong ? (price - entryPrice) / TickSize : (entryPrice - price) / TickSize;

			if (currentStopPrice == 0.0)
			{
				int seed = InitialStopTicks();
				currentStopPrice = isLong ? entryPrice - seed * TickSize : entryPrice + seed * TickSize;
			}

			// Breakeven — arm once when profit reaches the trigger; move stop to entry ± offset.
			if (BeMode != TtSimpleMode.Off && !breakEvenSet)
			{
				int trig = ToTicks(BeMode, BeTrigger);
				if (trig > 0 && profitTicks >= trig)
				{
					double be = isLong ? entryPrice + BeOffsetTicks * TickSize : entryPrice - BeOffsetTicks * TickSize;
					be = Instrument.MasterInstrument.RoundToTickSize(be);
					bool better = isLong ? be > currentStopPrice : be < currentStopPrice;
					if (better)
					{
						SetStopLoss(currentSignalName, CalculationMode.Price, be, false);
						currentStopPrice = be;
						breakEvenSet = true;
					}
				}
			}

			// Trailing — ratchet the stop behind price (ATR/Ticks) or along the EMA line,
			// never widening and never to the wrong side of the market.
			if (TrailMode != TtTrailMode.Off)
			{
				double cand = 0;
				bool have = false;
				if (TrailMode == TtTrailMode.EMA)
				{
					// Ride the stop TrailEmaBufferTicks BEYOND the EMA (below for longs, above for shorts)
					// so the buffer is maintained through the trail, not collapsed onto the raw EMA.
					if (trailEma != null && trailEma.Count > 0)
					{
						int tbuf = Math.Max(0, TrailEmaBufferTicks);
						cand = trailEma[0] + (isLong ? -tbuf : tbuf) * TickSize;
						have = true;
					}
				}
				else
				{
					int td = ToTicks(TrailMode, TrailValue);
					if (td > 0) { cand = isLong ? price - td * TickSize : price + td * TickSize; have = true; }
				}
				if (have)
				{
					cand = Instrument.MasterInstrument.RoundToTickSize(cand);
					bool better = isLong ? cand > currentStopPrice : cand < currentStopPrice;
					// EMA can sit on the wrong side of price (pullback through it) — only trail when
					// the candidate stays a real stop (below price for longs, above for shorts).
					bool validSide = isLong ? cand < price : cand > price;
					if (better && validSide)
					{
						SetStopLoss(currentSignalName, CalculationMode.Price, cand, false);
						currentStopPrice = cand;
					}
				}
			}
		}

		// Manual "MOVE SL TO BE" from the dashboard — create-or-tighten the stop to
		// entry ± BeOffsetTicks. Works even if SlMode/BeMode are Off.
		private void MoveToBreakeven()
		{
			if (Position.MarketPosition == MarketPosition.Flat) return;
			double avg = Position.AveragePrice;
			if (avg <= 0) return;
			bool isLong = Position.MarketPosition == MarketPosition.Long;
			double be = isLong ? avg + BeOffsetTicks * TickSize : avg - BeOffsetTicks * TickSize;
			be = Instrument.MasterInstrument.RoundToTickSize(be);
			// Don't submit a wrong-side stop (price already past BE). Under IgnoreAllErrors that reject
			// is silent, so the operator would wrongly believe BE is set.
			double px = MarkPrice();
			bool validSide = isLong ? be < px : be > px;
			if (!validSide)
			{
				if (_onOrderRejects++ < 10)
					Print("[Terminator_V2] MOVE SL TO BE skipped — price already past breakeven (would be a wrong-side stop).");
				return;
			}
			string sig = currentSignalName.Length > 0 ? currentSignalName : (isLong ? "TtLong" : "TtShort");
			SetStopLoss(sig, CalculationMode.Price, be, false);
			currentStopPrice = be;
			breakEvenSet = true;
		}

		// ====================================================================
		//  Manual live brackets — apply path (strategy thread only), shared by
		//  the dashboard nudge buttons and the draggable chart lines. Reuses the
		//  MoveToBreakeven plumbing: SetStopLoss/SetProfitTarget on the entry
		//  signal name so the stop+target stay an OCO pair. A line == a real order.
		// ====================================================================
		private string ActiveSignal()
		{
			if (currentSignalName.Length > 0) return currentSignalName;
			return Position.MarketPosition == MarketPosition.Long ? "TtLong" : "TtShort";
		}

		// Move/create the protective stop to an absolute price. Returns false and changes nothing on
		// a wrong-side price or a loosen blocked by ManualTightenOnly (so callers can snap a line back).
		private bool ApplyManualStop(double price)
		{
			if (!EnableManualBrackets || Position.MarketPosition == MarketPosition.Flat) return false;
			bool isLong = Position.MarketPosition == MarketPosition.Long;
			double px = Instrument.MasterInstrument.RoundToTickSize(price);
			double mk = MarkPrice();
			bool validSide = isLong ? px < mk : px > mk;
			if (!validSide)
			{
				if (_onOrderRejects++ < 10)
					Print("[Terminator_V2] Manual SL ignored — wrong side of price (" + px.ToString("F2") + " vs " + mk.ToString("F2") + ").");
				return false;
			}
			if (ManualStopMode == TtManualStopConflict.ManualTightenOnly && currentStopPrice > 0)
			{
				bool loosens = isLong ? px < currentStopPrice : px > currentStopPrice;
				if (loosens)
				{
					if (_onOrderRejects++ < 10)
						Print("[Terminator_V2] Manual SL ignored — Manual Stop Mode = ManualTightenOnly (loosening blocked).");
					return false;
				}
			}
			SetStopLoss(ActiveSignal(), CalculationMode.Price, px, false);
			currentStopPrice = px;
			_stopManuallyMoved = true;
			SetManualLine(SL_LINE_TAG, px, Brushes.Salmon, ref _slLineLastPrice);
			return true;
		}

		// Move/create the profit target to an absolute price (manual TP is always price-mode).
		private bool ApplyManualTarget(double price)
		{
			if (!EnableManualBrackets || Position.MarketPosition == MarketPosition.Flat) return false;
			bool isLong = Position.MarketPosition == MarketPosition.Long;
			double px = Instrument.MasterInstrument.RoundToTickSize(price);
			double mk = MarkPrice();
			bool profitSide = isLong ? px > mk : px < mk;
			if (!profitSide)
			{
				if (_onOrderRejects++ < 10)
					Print("[Terminator_V2] Manual TP ignored — not on the profit side (" + px.ToString("F2") + " vs " + mk.ToString("F2") + ").");
				return false;
			}
			SetProfitTarget(ActiveSignal(), CalculationMode.Price, px);
			currentTargetPrice = px;
			_targetManuallyMoved = true;
			SetManualLine(TP_LINE_TAG, px, Brushes.LimeGreen, ref _tpLineLastPrice);
			return true;
		}

		// Base price a nudge moves from: the existing bracket if any, else a sensible seed
		// (entry; a Currency TP converts to its equivalent price so the first touch keeps the distance).
		private double NudgeStopBase()
		{
			return currentStopPrice > 0 ? currentStopPrice : Position.AveragePrice;
		}

		private double NudgeTargetBase()
		{
			if (currentTargetPrice > 0) return currentTargetPrice;
			if (TpMode == TtExitMode.Currency)
			{
				double tv = TickValue();
				int q = Math.Abs(Position.Quantity);
				if (tv > 0 && q > 0)
				{
					int tt = Math.Max(1, (int)Math.Round(TpValue / (tv * q)));
					bool isLong = Position.MarketPosition == MarketPosition.Long;
					return isLong ? Position.AveragePrice + tt * TickSize : Position.AveragePrice - tt * TickSize;
				}
			}
			return Position.AveragePrice;
		}

		// Drain the nudge-button clicks the UI thread accumulated (net ticks) and apply once.
		private void DrainManualNudges()
		{
			int sn = System.Threading.Interlocked.Exchange(ref _pendingStopNudgeTicks, 0);
			int tn = System.Threading.Interlocked.Exchange(ref _pendingTargetNudgeTicks, 0);
			if (Position.MarketPosition == MarketPosition.Flat) return;
			if (sn != 0) ApplyManualStop(NudgeStopBase() + sn * TickSize);
			if (tn != 0) ApplyManualTarget(NudgeTargetBase() + tn * TickSize);
		}

		// Write a draggable horizontal line at price and remember what WE wrote (the drag baseline).
		private void SetManualLine(string tag, double price, Brush brush, ref double shadow)
		{
			if (State != State.Realtime) return;
			double px = Instrument.MasterInstrument.RoundToTickSize(price);
			if (px <= 0) return;
			try
			{
				HorizontalLine hl = Draw.HorizontalLine(this, tag, px, brush, DashStyleHelper.Dash, 2);
				if (hl != null) ((DrawingTool)hl).IsLocked = false;   // keep it user-draggable
			}
			catch { }
			shadow = px;
			_manualLinesPresent = true;
		}

		private void RemoveManualLines()
		{
			if (!_manualLinesPresent) return;
			try { RemoveDrawObject(SL_LINE_TAG); } catch { }
			try { RemoveDrawObject(TP_LINE_TAG); } catch { }
			_slLineLastPrice = 0.0; _tpLineLastPrice = 0.0;
			_slPendingPx = 0.0; _tpPendingPx = 0.0;
			_manualLinesPresent = false;
		}

		// Read back any user drag of the SL/TP lines and keep the lines synced to the real brackets.
		// Called every tick (throttled) and at each bar close (forced). A 1-cycle debounce keeps a
		// drag from firing SetStopLoss on every intermediate position.
		private void ServiceManualBrackets(bool force)
		{
			if (State != State.Realtime) return;
			if (!EnableManualBrackets || Position.MarketPosition == MarketPosition.Flat)
			{
				RemoveManualLines();
				return;
			}
			DateTime nowUtc = DateTime.UtcNow;
			if (!force && (nowUtc - _lastManualSvcUtc).TotalMilliseconds < MANUAL_SVC_MIN_MS) return;
			_lastManualSvcUtc = nowUtc;
			double tol = TickSize * 0.5;

			// --- Stop line: detect drag (debounced), then reconcile the line to currentStopPrice ---
			HorizontalLine sl = DrawObjects[SL_LINE_TAG] as HorizontalLine;
			if (sl != null)
			{
				double linePx = Instrument.MasterInstrument.RoundToTickSize(sl.StartAnchor.Price);
				if (Math.Abs(linePx - _slLineLastPrice) >= tol)            // moved from what we last wrote
				{
					if (_slPendingPx > 0 && Math.Abs(linePx - _slPendingPx) < tol)
					{
						if (!ApplyManualStop(linePx) && currentStopPrice > 0)
							SetManualLine(SL_LINE_TAG, currentStopPrice, Brushes.Salmon, ref _slLineLastPrice);  // snap back
						_slPendingPx = 0.0;
					}
					else _slPendingPx = linePx;                            // record, commit next cycle once settled
				}
				else _slPendingPx = 0.0;
			}
			if (currentStopPrice > 0)
			{
				if (sl == null || Math.Abs(currentStopPrice - _slLineLastPrice) >= tol)
					SetManualLine(SL_LINE_TAG, currentStopPrice, Brushes.Salmon, ref _slLineLastPrice);
			}
			else if (sl != null) { try { RemoveDrawObject(SL_LINE_TAG); } catch { } _slLineLastPrice = 0.0; }

			// --- Target line: same pattern ---
			HorizontalLine tp = DrawObjects[TP_LINE_TAG] as HorizontalLine;
			if (tp != null)
			{
				double linePx = Instrument.MasterInstrument.RoundToTickSize(tp.StartAnchor.Price);
				if (Math.Abs(linePx - _tpLineLastPrice) >= tol)
				{
					if (_tpPendingPx > 0 && Math.Abs(linePx - _tpPendingPx) < tol)
					{
						if (!ApplyManualTarget(linePx) && currentTargetPrice > 0)
							SetManualLine(TP_LINE_TAG, currentTargetPrice, Brushes.LimeGreen, ref _tpLineLastPrice);
						_tpPendingPx = 0.0;
					}
					else _tpPendingPx = linePx;
				}
				else _tpPendingPx = 0.0;
			}
			if (currentTargetPrice > 0)
			{
				if (tp == null || Math.Abs(currentTargetPrice - _tpLineLastPrice) >= tol)
					SetManualLine(TP_LINE_TAG, currentTargetPrice, Brushes.LimeGreen, ref _tpLineLastPrice);
			}
			else if (tp != null) { try { RemoveDrawObject(TP_LINE_TAG); } catch { } _tpLineLastPrice = 0.0; }
		}

		private void FlattenAll(string reason)
		{
			// ByStrategyPosition → exit with empty fromEntrySignal (pattern doc §10 / Pitfall 5).
			if (Position.MarketPosition == MarketPosition.Long) ExitLong("Flat_" + reason, "");
			else if (Position.MarketPosition == MarketPosition.Short) ExitShort("Flat_" + reason, "");
		}

		// Defense-in-depth: when flat, cancel any of OUR working TP/SL orders that linger
		// (the OCO already handles the normal case; this catches the reporter's orphan-TP
		// scenario from manual/partial closes). Realtime + once-per-bar.
		private void SweepOrphansIfFlat()
		{
			if (State != State.Realtime) return;
			if (Position.MarketPosition != MarketPosition.Flat) return;
			if (_lastOrphanSweepBar == CurrentBar) return;
			_lastOrphanSweepBar = CurrentBar;

			// Iterate THIS strategy's own Orders (not Account.Orders) so we can never cancel another
			// strategy's identically-named "Stop loss"/"Profit target" on the same instrument and
			// leave IT naked. Protective names only — an entry briefly Working while flat is left alone.
			List<Order> toCancel = null;
			foreach (Order o in Orders)
			{
				if (o.OrderState != OrderState.Working && o.OrderState != OrderState.Accepted) continue;
				string n = o.Name ?? "";
				if (n.StartsWith("Stop loss") || n.StartsWith("Profit target"))
				{
					if (toCancel == null) toCancel = new List<Order>();
					toCancel.Add(o);
				}
			}
			if (toCancel != null)
				foreach (Order o in toCancel)
					try { CancelOrder(o); } catch { }
		}

		// Hard size cap / flip guard: net position must never exceed the size we asked for.
		// With clean-split reversal this should never trip, but it converts any stray
		// overfill into an immediate flatten instead of a silently compounded position.
		private void CheckSizeGuard()
		{
			if (Position.MarketPosition == MarketPosition.Flat) { _oversizeFlattenAttempted = false; return; }
			if (_intendedQty <= 0) return;
			if (Math.Abs(Position.Quantity) > _intendedQty)
			{
				if (_oversizeFlattenAttempted) return;   // flatten ONCE — don't re-submit every tick
				_oversizeFlattenAttempted = true;
				if (_onOrderRejects++ < 10)
					Print("[Terminator_V2] OVERSIZE GUARD: pos " + Position.Quantity + " > intended " + _intendedQty + " — flattening.");
				FlattenAll("OversizeGuard");
			}
		}

		private void ResetTradeState()
		{
			entryPrice = 0.0;
			currentStopPrice = 0.0;
			currentSignalName = "";
			initialStopTicks = 0;
			breakEvenSet = false;
			_intendedQty = 0;
			currentTargetPrice = 0.0;
			_stopManuallyMoved = false;
			_targetManuallyMoved = false;
		}

		private double MarkPrice()
		{
			return _lastTradePrice > 0 ? _lastTradePrice : Close[0];
		}

		// Capture the carried-position baseline at the FIRST realtime evaluation, where Position +
		// market data are reliably ready. MUST run before the carried position is discarded/closed,
		// else its pre-live unrealized leaks into Day PnL (and can falsely trip the daily-profit lock).
		// Idempotent — latches _carriedBaselinePending false.
		private void EnsureCarriedBaseline()
		{
			if (!_carriedBaselinePending) return;
			if (carryingPosition && Position.MarketPosition != MarketPosition.Flat)
			{
				try { carriedUnrealizedBaseline = Position.GetUnrealizedProfitLoss(PerformanceUnit.Currency, MarkPrice()); }
				catch { carriedUnrealizedBaseline = 0.0; }
			}
			else carriedUnrealizedBaseline = 0.0;
			_carriedBaselinePending = false;
		}

		// Day PnL = realized-since-session-start + open PnL, EXCLUDING any position carried across
		// the historical->realtime boundary (its pre-live portion is not ours). When that carried
		// position finally closes its full realized PnL lands in CumProfit, so we fold the baseline
		// into the anchor once to keep the pre-live part excluded. The fold runs once (carryingPosition
		// latches false); OnBarUpdate and OnMarketData both call this on the single strategy thread.
		private double ComputeDayPnL()
		{
			EnsureCarriedBaseline();
			// A carried position being DISCARDED contributes nothing to live Day PnL (the discard sends
			// no real order — it's a pre-live virtual artifact, including any drift while it's flattened).
			// Hold at 0 until the discard completes; the discard handler then re-baselines so it stays 0.
			if (_discardCarriedPending) { dailyPnl = 0.0; return 0.0; }
			double pnl = SystemPerformance.AllTrades.TradesPerformance.Currency.CumProfit - sessionStartCumProfit;
			if (Position.MarketPosition != MarketPosition.Flat)
			{
				double open = 0.0;
				try { open = Position.GetUnrealizedProfitLoss(PerformanceUnit.Currency, MarkPrice()); } catch { }
				pnl += open - carriedUnrealizedBaseline;
			}
			else if (carryingPosition)
			{
				sessionStartCumProfit += carriedUnrealizedBaseline;
				carryingPosition = false;
				carriedUnrealizedBaseline = 0.0;
				_carriedBaselinePending = false;
				pnl = SystemPerformance.AllTrades.TradesPerformance.Currency.CumProfit - sessionStartCumProfit;
			}
			dailyPnl = pnl;
			return pnl;
		}

		private void UpdateDailyRisk()
		{
			double dayPnL = ComputeDayPnL();
			if (UseDailyLoss && DailyLoss > 0 && dayPnL <= -DailyLoss) tradingBlocked = true;
			if (UseDailyProfit && DailyProfit > 0 && dayPnL >= DailyProfit) tradingBlocked = true;
			if (tradingBlocked && DailyFlatten && Position.MarketPosition != MarketPosition.Flat)
				FlattenAll("DailyLimit");
		}

		private void Notify(int dir)
		{
			if (!UseAlerts) return;
			string msg = dir > 0 ? "Terminator_V2 LONG entry" : "Terminator_V2 SHORT entry";
			Alert("Tt" + CurrentBar, Priority.Medium, msg, "", 10, Brushes.DimGray, Brushes.White);
		}

		// ====================================================================
		//  Drawing helpers (rolling cleanup — never accumulate draw objects)
		// ====================================================================
		private void TrackTag(string tag)
		{
			_drawTags.Enqueue(tag);
			// Keep ALL historical-replay tags so historical entries stay visible; only bound LIVE
			// growth (the quota risk is a long realtime session, not the one-time historical draw).
			if (State != State.Realtime) return;
			while (_drawTags.Count > MAX_DRAW_TAGS)
			{
				string old = _drawTags.Dequeue();
				try { RemoveDrawObject(old); } catch { }
			}
		}

		private void DrawEntryTag(int dir)
		{
			if (!ShowMarkers) return;
			string tag = (dir > 0 ? "LONG" : "SHORT") + CurrentBar;
			if (dir > 0)
				Draw.Text(this, tag, true, "Buy", 0, Low[0], -12, Brushes.Lime, _markerFont, TextAlignment.Center, Brushes.Black, Brushes.Lime, 8);
			else
				Draw.Text(this, tag, true, "Sell", 0, High[0], 12, Brushes.Red, _markerFont, TextAlignment.Center, Brushes.Black, Brushes.Red, 8);
			TrackTag(tag);
		}

		private void DrawBlockMarker(int dir, string reason)
		{
			if (!ShowFilterMarkers) return;
			if (State != State.Realtime) return;   // don't litter the chart with warmup-replay block markers
			string tag = "BLK" + CurrentBar;
			double y = dir > 0 ? Low[0] - 2 * TickSize : High[0] + 2 * TickSize;
			Draw.Text(this, tag, reason, 0, y, Brushes.Gray);
			TrackTag(tag);
		}

		// ====================================================================
		//  Order / market-data events
		// ====================================================================
		protected override void OnOrderUpdate(Order order, double limitPrice, double stopPrice,
			int quantity, int filled, double averageFillPrice, OrderState orderState,
			DateTime time, ErrorCode error, string comment)
		{
			try
			{
				if (order == null) return;

				if (orderState == OrderState.Rejected && _onOrderRejects++ < 10)
					Print("[Terminator_V2] Order REJECTED: " + order.Name + " " + error + " " + comment);

				// Capture the actual fill price so Price-mode trailing/BE compute from the real entry.
				if (orderState == OrderState.Filled && (order.Name == "TtLong" || order.Name == "TtShort"))
				{
					entryPrice = averageFillPrice;
					currentSignalName = order.Name;
					breakEvenSet = false;
					if (SlMode != TtSlMode.EMA)
						currentStopPrice = initialStopTicks > 0
							? (order.Name == "TtLong"
								? entryPrice - initialStopTicks * TickSize
								: entryPrice + initialStopTicks * TickSize)
							: 0.0;
				}
			}
			catch (Exception ex)
			{
				if (_onOrderRejects++ < 10)
					Print("[Terminator_V2] OnOrderUpdate error: " + ex.GetType().Name + " " + ex.Message);
			}
		}

		protected override void OnMarketData(MarketDataEventArgs marketDataUpdate)
		{
			try
			{
				if (State != State.Realtime || BarsInProgress != 0) return;
				if (marketDataUpdate.MarketDataType != MarketDataType.Last) return;
				if (marketDataUpdate.Price > 0)
					_lastTradePrice = marketDataUpdate.Price;
				if (CurrentBars == null || CurrentBars.Length == 0 || CurrentBars[0] < 2) return;

				// Manual dashboard commands run on the strategy thread, immediately (don't wait
				// for the next bar close — a panic FLATTEN must fire now).
				ProcessDashboardCommands();
				UpdateDailyRisk();
				CheckSizeGuard();
				ServiceManualBrackets(false);   // read back any SL/TP line drag (throttled)
				UpdateDashboard();
			}
			catch { /* never let a tick throw into NT8 */ }
		}

		// Process the volatile flags the UI thread sets (Daxton command pattern).
		private void ProcessDashboardCommands()
		{
			if (_pendingFlatten)
			{
				_pendingFlatten = false;
				pendingReverseDir = 0;
				if (Position.MarketPosition != MarketPosition.Flat)
					FlattenAll("Manual");
				UpdateDashboard(true);
			}
			if (_pendingBE)
			{
				_pendingBE = false;
				if (Position.MarketPosition != MarketPosition.Flat)
					MoveToBreakeven();
				UpdateDashboard(true);
			}
			DrainManualNudges();
		}

		// ====================================================================
		//  Main bar loop
		// ====================================================================
		protected override void OnBarUpdate()
		{
			try { ProcessBarUpdate(); }
			catch (Exception ex)
			{
				if (_onBarErrors++ < 5)
					Print("[Terminator_V2] OnBarUpdate error: " + ex.GetType().Name + " " + ex.Message);
			}
		}

		private void ProcessBarUpdate()
		{
			if (BarsInProgress != 0) return;

			// Warm-up: gap the plots (so they don't draw a line at 0) and bail.
			if (CurrentBar < BarsRequiredToTrade)
			{
				Values[PLOT_TRAIL][0] = double.NaN;
				Values[PLOT_VWMA][0] = double.NaN;
				Values[PLOT_TRAILEMA][0] = double.NaN;
				Values[PLOT_SLEMA][0] = double.NaN;
				return;
			}

			// Lock in the carried-position baseline BEFORE the discard/close path below can fire
			// (reliable here: Position + market data are ready). Without this, the carried position's
			// historical unrealized leaks into Day PnL and can falsely trip the daily-profit lock.
			EnsureCarriedBaseline();

			// Daily session reset — driven by the actual session boundary (Bars.IsFirstBarOfSession),
			// NOT calendar date. A calendar-midnight reset inside an overnight Globex session (e.g.
			// 17:00->16:00) would clear tradingBlocked and re-baseline PnL mid-session, letting the
			// DailyLoss limit be hit twice in one trading day.
			// Discard a warmup-carried position on the first realtime bar (DiscardCarriedAtRealtime).
			// Under WaitUntilFlat the carry is virtual so this exit sends no live order; it just
			// brings the strategy flat so normal flow resumes. Re-issued until flat.
			if (_discardCarriedPending)
			{
				if (Position.MarketPosition == MarketPosition.Long) ExitLong("DiscardCarried", "");
				else if (Position.MarketPosition == MarketPosition.Short) ExitShort("DiscardCarried", "");
				if (Position.MarketPosition == MarketPosition.Flat)
				{
					_discardCarriedPending = false;
					// Discard = the carried position never counts. Re-baseline to "now" so its full
					// (virtual) realized, incl. the drift taken while flattening, drops out of Day PnL.
					sessionStartCumProfit = SystemPerformance.AllTrades.TradesPerformance.Currency.CumProfit;
					carryingPosition = false;
					carriedUnrealizedBaseline = 0.0;
					_carriedBaselinePending = false;
				}
				else { UpdateDashboard(); return; }
			}

			if ((Bars.IsFirstBarOfSession && CurrentBar != lastSessionResetBar) || firstSession)
			{
				firstSession = false;
				lastSessionResetBar = CurrentBar;
				sessionStartCumProfit = SystemPerformance.AllTrades.TradesPerformance.Currency.CumProfit;
				tradingBlocked = false;
				dailyEntryCount = 0;
				pendingReverseDir = 0;
				carryingPosition = false;
				carriedUnrealizedBaseline = 0.0;
				_carriedBaselinePending = false;
			}

			// Manual commands also serviced here (covers any gap between ticks).
			ProcessDashboardCommands();
			UpdateDailyRisk();
			CheckSizeGuard();

			// Intraday window flatten
			if (UseTimeFilter && FlattenAtEnd && !TimeFilterEntriesOnly && !InWindow() && Position.MarketPosition != MarketPosition.Flat)
			{
				FlattenAll("WindowEnd");
				pendingReverseDir = 0;
				UpdateDashboard();
				return;
			}

			// Position went flat via stop/TP/session — clear trade state + sweep any orphans.
			if (Position.MarketPosition == MarketPosition.Flat && currentSignalName.Length > 0)
				ResetTradeState();
			SweepOrphansIfFlat();

			// Manage the live trade's stop (BE/trail) every bar while in position.
			ManageStops();

			// --- ATR trailing-stop signal engine (entry trigger) ---
			double price0 = SignalPrice(0);
			double price1 = SignalPrice(1);

			if (!_isInitialized)
			{
				xAtrTrailingStop = price0;
				_isInitialized = true;
			}

			double num = ATRMult * atr[0];
			xAtrTrailingStopLast = xAtrTrailingStop;
			double last = xAtrTrailingStopLast;
			if (price0 > last && price1 > last)
				xAtrTrailingStop = Math.Max(last, price0 - num);
			else if (price0 < last && price1 < last)
				xAtrTrailingStop = Math.Min(last, price0 + num);
			else if (price0 > last)
				xAtrTrailingStop = price0 - num;
			else
				xAtrTrailingStop = price0 + num;

			// Plots: engine line + (optional) VWMA. VWMA is NaN-gapped when unused.
			Values[PLOT_TRAIL][0] = ShowTrailPlot ? xAtrTrailingStop : double.NaN;
			Values[PLOT_VWMA][0] = (ShowVwmaPlot && outVwma != null && outVwma.Count > 0) ? outVwma[0] : double.NaN;
			Values[PLOT_TRAILEMA][0] = (TrailMode == TtTrailMode.EMA && trailEma != null && trailEma.Count > 0) ? trailEma[0] : double.NaN;
			Values[PLOT_SLEMA][0] = (SlMode == TtSlMode.EMA && slEma != null && slEma.Count > 0) ? slEma[0] : double.NaN;

			int dir = 0;
			if (price1 < last && price0 > last) dir = 1;
			else if (price1 > last && price0 < last) dir = -1;

			// Mark every reversal SIGNAL (cross) HERE in OnBarUpdate so Buy/Sell tags render on
			// HISTORICAL bars too. A chart strategy computes signals over history but does NOT execute
			// historical trades, so a tag tied to the executed entry only ever shows up live.
			if (dir != 0) DrawEntryTag(dir);

			MarketPosition mp = Position.MarketPosition;

			if (mp == MarketPosition.Flat)
			{
				if (dir != 0)
				{
					// A fresh cross from flat takes priority over any queued reverse.
					string reason = EntryBlockReason(dir);
					bool cool = CooldownBars > 0 && lastEntryBar >= 0 && (CurrentBar - lastEntryBar) < CooldownBars;
					if (reason.Length == 0 && !cool)
						EnterDirection(dir);
					else
						DrawBlockMarker(dir, cool ? "cool" : reason);
					pendingReverseDir = 0;
				}
				else if (pendingReverseDir != 0)
				{
					// Execute the queued reverse now that we're confirmed flat (clean single-lot
					// order). Drop it if stale or a filter now blocks it.
					bool stale = (CurrentBar - pendingReverseBar) > REVERSE_MAX_DELAY_BARS;
					if (stale || EntryBlockReason(pendingReverseDir).Length != 0)
						pendingReverseDir = 0;
					else
					{
						EnterDirection(pendingReverseDir);
						pendingReverseDir = 0;
					}
				}
			}
			else if (dir != 0)
			{
				bool opposite = (dir > 0 && mp == MarketPosition.Short) || (dir < 0 && mp == MarketPosition.Long);
				// Skip if a reverse to this side is already queued/flattening — don't re-issue the
				// flatten or reset the staleness clock while the exit is still working.
				if (opposite && pendingReverseDir != dir)
				{
					// CLEAN-SPLIT REVERSAL: close on this bar, queue the new-direction entry for
					// when we're flat — so each order is exactly Quantity (no 2x close+reverse).
					string reason = EntryBlockReason(dir, TimeFilterEntriesOnly);
					if (reason.Length == 0)
					{
						FlattenAll("reverse");
						pendingReverseDir = dir;
						pendingReverseBar = CurrentBar;
					}
					else
					{
						DrawBlockMarker(dir, reason);
					}
				}
				// same-direction cross while in position → ignore (already positioned)
			}

			ServiceManualBrackets(true);   // sync the SL/TP lines to the brackets after auto-trail/BE
			UpdateDashboard();
		}

		// Single entry funnel: arm managed exits, size, enter, tag, count.
		private void EnterDirection(int dir)
		{
			string sig = dir > 0 ? "TtLong" : "TtShort";
			initialStopTicks = InitialStopTicks();

			// Arm the initial stop. SL=EMA places it AT the EMA level (Price mode) on the protective
			// side; otherwise a tick distance. stopPx is what we track/display.
			double stopPx = 0.0;
			if (SlMode == TtSlMode.EMA && slEma != null && slEma.Count > 0)
			{
				// Push the stop SlEmaBufferTicks BEYOND the EMA (away from price): below for longs,
				// above for shorts, so normal noise around the line doesn't stop the trade out at once.
				int buf = Math.Max(0, SlEmaBufferTicks);
				double slLvl = slEma[0] + (dir > 0 ? -buf : buf) * TickSize;
				slLvl = Instrument.MasterInstrument.RoundToTickSize(slLvl);
				bool okSide = dir > 0 ? slLvl < Close[0] : slLvl > Close[0];
				if (okSide) { SetStopLoss(sig, CalculationMode.Price, slLvl, false); stopPx = slLvl; }
				else if (initialStopTicks > 0)
				{
					SetStopLoss(sig, CalculationMode.Ticks, initialStopTicks, false);
					stopPx = dir > 0 ? Close[0] - initialStopTicks * TickSize : Close[0] + initialStopTicks * TickSize;
				}
			}
			else if (initialStopTicks > 0)
			{
				SetStopLoss(sig, CalculationMode.Ticks, initialStopTicks, false);
				stopPx = dir > 0 ? Close[0] - initialStopTicks * TickSize : Close[0] + initialStopTicks * TickSize;
			}
			if (TpMode != TtExitMode.Off)
			{
				if (TpMode == TtExitMode.Currency)
					SetProfitTarget(sig, CalculationMode.Currency, TpValue);
				else
					SetProfitTarget(sig, CalculationMode.Ticks, ToTicks(TpMode, TpValue));
			}

			currentSignalName = sig;
			entryPrice = Close[0];
			currentStopPrice = stopPx;
			breakEvenSet = false;

			// Manual-bracket bookkeeping: clear the per-trade latches and seed the tracked TP price
			// (price-based modes directly; Currency stays $-display until the first manual touch).
			_stopManuallyMoved = false;
			_targetManuallyMoved = false;
			currentTargetPrice = (TpMode != TtExitMode.Off && TpMode != TtExitMode.Currency)
				? Instrument.MasterInstrument.RoundToTickSize(dir > 0 ? entryPrice + ToTicks(TpMode, TpValue) * TickSize : entryPrice - ToTicks(TpMode, TpValue) * TickSize)
				: 0.0;

			int qty = ComputeQuantity(dir);
			_intendedQty = qty;

			if (dir > 0) EnterLong(qty, "TtLong");
			else EnterShort(qty, "TtShort");

			lastEntryBar = CurrentBar;
			dailyEntryCount++;
			Notify(dir);
		}

		// ====================================================================
		//  Dashboard (MafiaFlipSwitch-style; AGENTS.md WPF hardening throughout)
		// ====================================================================
		#region Dashboard fields
		private Border _dashPanel;
		private Border _dashTitleBar;
		private StackPanel _dashBody;
		private StackPanel _dashTradeInfo;
		private Thumb _dragThumb;
		private System.Windows.Shapes.Path _pillPath;
		private Border _pillBtn;
		private TextBlock _dashStatus, _dashBias;
		private TextBlock _dashInstrument, _dashWindow, _dashWindowState, _dashDaily;
		private TextBlock _dashEntry, _dashStop, _dashTarget, _dashQty, _dashPnl;
		private bool _dashMinimized;
		private bool _uiInitialized;
		private volatile bool _dashTornDown;     // set by RemoveDashboard BEFORE null checks so a
		                                         // still-queued CreateDashboard lambda can't orphan a panel
		private DateTime _lastDashPushUtc = DateTime.MinValue;
		private const int DASH_PUSH_MIN_MS = 150;
		private volatile bool _dashPushInFlight; // max ONE queued dispatcher op (quota defense)

		private Button _autoBtn, _longBtn, _shortBtn, _beBtn, _flattenBtn;
		private Button _slDownBtn, _slUpBtn, _tpDownBtn, _tpUpBtn;
		private StackPanel _nudgeRow;
		private volatile bool _autoEnabled = true;
		private volatile bool _longEnabled = true;
		private volatile bool _shortEnabled = true;
		private volatile bool _pendingFlatten;
		private volatile bool _pendingBE;

		// Frozen palette — created once, shared, never mutated (AGENTS.md WPF quota rules)
		// Terminator theme: amber/gold on warm graphite (distinct from MafiaFlipSwitch's navy/blue).
		private static readonly SolidColorBrush DashBg      = MakeFrozen(0xF0, 0x16, 0x15, 0x12);
		private static readonly SolidColorBrush DashBorder  = MakeFrozen(0xFF, 0x4A, 0x3C, 0x1E);
		private static readonly SolidColorBrush DashTitleBg = MakeFrozen(0xFF, 0x24, 0x1E, 0x12);
		private static readonly SolidColorBrush DashTitleFg = MakeFrozen(0xFF, 0xF2, 0xB0, 0x44);
		private static readonly SolidColorBrush DashDimFg   = MakeFrozen(0xFF, 0xA6, 0x9A, 0x80);
		private static readonly SolidColorBrush DashSep     = MakeFrozen(0xFF, 0x3C, 0x33, 0x22);

		private static readonly SolidColorBrush BtnInactBg  = MakeFrozen(0xFF, 0x24, 0x20, 0x18);
		private static readonly SolidColorBrush BtnInactBdr = MakeFrozen(0xFF, 0x4A, 0x42, 0x30);
		private static readonly SolidColorBrush BtnLongBg   = MakeFrozen(0xFF, 0x0D, 0x30, 0x1A);
		private static readonly SolidColorBrush BtnLongBdr  = MakeFrozen(0xFF, 0x28, 0xC8, 0x60);
		private static readonly SolidColorBrush BtnShortBg  = MakeFrozen(0xFF, 0x30, 0x0D, 0x0D);
		private static readonly SolidColorBrush BtnShortBdr = MakeFrozen(0xFF, 0xC8, 0x20, 0x28);
		private static readonly SolidColorBrush BtnAutoBg   = MakeFrozen(0xFF, 0x3A, 0x29, 0x0A);
		private static readonly SolidColorBrush BtnAutoBdr  = MakeFrozen(0xFF, 0xE6, 0xA8, 0x3A);
		private static readonly SolidColorBrush BtnFg       = MakeFrozen(0xFF, 0xDA, 0xCC, 0xB0);
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
			TtPanelCorner corner = DashboardCorner;

			ChartControl.Dispatcher.InvokeAsync(() =>
			{
				try
				{
					if (_uiInitialized || _dashTornDown || State == State.Terminated)
						return;

					// Title bar: drag thumb + title + pill minimize
					_dragThumb = new Thumb { Background = Brushes.Transparent, Cursor = Cursors.SizeAll };
					var thumbFac = new FrameworkElementFactory(typeof(Border));
					thumbFac.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Control.BackgroundProperty));
					_dragThumb.Template = new ControlTemplate(typeof(Thumb)) { VisualTree = thumbFac };
					_dragThumb.DragDelta += OnPanelDragDelta;

					var titleText = new TextBlock
					{
						Text = "TERMINATOR V2  v" + Version,
						Foreground = DashTitleFg,
						FontSize = 11,
						FontWeight = FontWeights.Bold,
						VerticalAlignment = VerticalAlignment.Center,
						HorizontalAlignment = HorizontalAlignment.Center,
						Margin = new Thickness(4, 0, 30, 0),
						IsHitTestVisible = false
					};

					_pillPath = new System.Windows.Shapes.Path
					{
						Stroke = DashDimFg,
						StrokeThickness = 1.5,
						Fill = null,
						StrokeLineJoin = PenLineJoin.Round,
						Opacity = _dashMinimized ? 0.9 : 0.5,
						IsHitTestVisible = false,
						Data = Geometry.Parse("M 3,0 L 15,0 A 3,3 0 0 1 15,6 L 3,6 A 3,3 0 0 1 3,0 Z"),
						HorizontalAlignment = HorizontalAlignment.Center,
						VerticalAlignment = VerticalAlignment.Center
					};
					_pillBtn = new Border
					{
						Width = 22,
						Height = 12,
						Margin = new Thickness(0, 0, 8, 0),
						HorizontalAlignment = HorizontalAlignment.Right,
						VerticalAlignment = VerticalAlignment.Center,
						Background = Brushes.Transparent,
						Cursor = Cursors.Hand,
						ToolTip = _dashMinimized ? "Click to restore" : "Click to minimize",
						Child = _pillPath
					};
					_pillBtn.MouseLeftButtonDown += OnPillMouseDown;
					_pillBtn.MouseLeftButtonUp += OnPillMouseUp;
					_pillBtn.MouseEnter += OnPillMouseEnter;
					_pillBtn.MouseLeave += OnPillMouseLeave;

					var titleGrid = new Grid();
					titleGrid.Children.Add(_dragThumb);
					titleGrid.Children.Add(titleText);
					titleGrid.Children.Add(_pillBtn);

					_dashTitleBar = new Border
					{
						Background = DashTitleBg,
						Height = 24,
						CornerRadius = new CornerRadius(8, 8, 0, 0),
						Child = titleGrid,
						ToolTip = "Drag to move"
					};

					// Status row
					var statusRow = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 3, 0, 2) };
					_dashStatus = new TextBlock { Text = "FLAT", Foreground = Brushes.DimGray, FontSize = 12, FontWeight = FontWeights.Bold };
					_dashBias = new TextBlock { Text = "", Foreground = Brushes.DimGray, FontSize = 12 };
					statusRow.Children.Add(_dashStatus);
					statusRow.Children.Add(_dashBias);

					// Info rows
					_dashInstrument = MakeInfoRow(DashDimFg);
					_dashWindow = MakeInfoRow(Brushes.WhiteSmoke);
					_dashWindowState = MakeInfoRow(Brushes.Orange);
					_dashDaily = MakeInfoRow(Brushes.WhiteSmoke);

					// Trade info (visible only in a position)
					_dashTradeInfo = new StackPanel { Orientation = Orientation.Vertical, Visibility = Visibility.Collapsed };
					_dashTradeInfo.Children.Add(MakeSeparator());
					_dashEntry = MakeInfoRow(Brushes.WhiteSmoke); _dashTradeInfo.Children.Add(_dashEntry);
					_dashStop = MakeInfoRow(Brushes.Salmon); _dashTradeInfo.Children.Add(_dashStop);
					_dashTarget = MakeInfoRow(Brushes.LimeGreen); _dashTradeInfo.Children.Add(_dashTarget);
					_dashQty = MakeInfoRow(Brushes.WhiteSmoke); _dashTradeInfo.Children.Add(_dashQty);
					_dashPnl = MakeInfoRow(Brushes.WhiteSmoke); _dashTradeInfo.Children.Add(_dashPnl);

					// Buttons
					_autoBtn = MakeDashButton("AUTO: ON", 94, 26);
					_longBtn = MakeDashButton("LONG: ON", 94, 26);
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

					// Manual SL/TP nudge row (▲ = raise price, ▼ = lower price — unambiguous long & short)
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
					if (!EnableManualBrackets) _nudgeRow.Visibility = Visibility.Collapsed;

					_flattenBtn = MakeDashButton("FLATTEN ALL", 294, 26);
					_flattenBtn.Background = BtnShortBg;
					_flattenBtn.BorderBrush = BtnShortBdr;
					_flattenBtn.Foreground = BtnFlatFg;
					_flattenBtn.Click += OnFlattenClick;

					// Assemble
					_dashBody = new StackPanel { Orientation = Orientation.Vertical, MinWidth = 210, Margin = new Thickness(0, 0, 0, 6) };
					_dashBody.Children.Add(statusRow);
					_dashBody.Children.Add(MakeSeparator());
					_dashBody.Children.Add(_dashInstrument);
					_dashBody.Children.Add(_dashWindow);
					_dashBody.Children.Add(_dashWindowState);
					_dashBody.Children.Add(_dashDaily);
					_dashBody.Children.Add(_dashTradeInfo);
					_dashBody.Children.Add(MakeSeparator());
					_dashBody.Children.Add(toggleRow);
					_dashBody.Children.Add(_beBtn);
					_dashBody.Children.Add(_nudgeRow);
					_dashBody.Children.Add(_flattenBtn);
					if (_dashMinimized)
						_dashBody.Visibility = Visibility.Collapsed;

					var main = new StackPanel { Orientation = Orientation.Vertical, MinWidth = 210 };
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

					// Position panel after first layout (needed for right / bottom corners)
					EventHandler layoutHandler = null;
					layoutHandler = (ls, le) =>
					{
						if (_dashPanel == null) return;
						var parent = _dashPanel.Parent as FrameworkElement;
						if (parent == null || _dashPanel.ActualWidth <= 0 || parent.ActualWidth <= 0) return;
						double left = 10, top = 10;
						switch (corner)
						{
							case TtPanelCorner.TopRight:
								left = parent.ActualWidth - _dashPanel.ActualWidth - 10; break;
							case TtPanelCorner.BottomLeft:
								top = parent.ActualHeight - _dashPanel.ActualHeight - 10; break;
							case TtPanelCorner.BottomRight:
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
					Print("[Terminator_V2] Dashboard create error: " + ex.Message);
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
					if (_flattenBtn != null) _flattenBtn.Click -= OnFlattenClick;
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
					_pillBtn = null; _pillPath = null;
					_autoBtn = _longBtn = _shortBtn = _beBtn = _flattenBtn = null;
					_slDownBtn = _slUpBtn = _tpDownBtn = _tpUpBtn = null;
					_nudgeRow = null;
					_dashStatus = _dashBias = null;
					_dashInstrument = _dashWindow = _dashWindowState = _dashDaily = null;
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
			catch { /* Terminated must never throw — counts toward MaxRestarts */ }
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

			bool carried = carryingPosition;
			bool isLong = Position.MarketPosition == MarketPosition.Long;
			bool isShort = Position.MarketPosition == MarketPosition.Short;
			bool inPos = (isLong || isShort) && !carried;

			// A carried (warmup-inherited) position shows as HIST, not a live trade, and its trade
			// rows stay hidden so a screenshot can't be mistaken for a real live position.
			string posText = carried ? "HIST" : (inPos ? (isLong ? "LONG" : "SHORT") : "FLAT");
			Brush posBrush = carried ? Brushes.Goldenrod : isLong ? Brushes.LimeGreen : isShort ? Brushes.Crimson : Brushes.DimGray;

			string biasText = "";
			Brush biasBrush = Brushes.DimGray;
			if (_isInitialized)
			{
				bool bull = Close[0] > xAtrTrailingStop;
				biasText = "  |  Trend: " + (bull ? "UP" : "DOWN");
				biasBrush = bull ? Brushes.LimeGreen : Brushes.Crimson;
			}

			string instrText = "Instr  " + (Instrument != null && Instrument.MasterInstrument != null ? Instrument.MasterInstrument.Name : "?")
				+ "   Acct  " + (Account != null ? Account.Name : "?");

			string windowText = UseTimeFilter
				? string.Format("Window  {0:000000}-{1:000000}", StartTime, EndTime)
				: "Window  always on";
			string windowStateText;
			Brush windowStateBrush;
			if (tradingBlocked)
			{
				windowStateText = "BLOCKED — daily limit hit";
				windowStateBrush = Brushes.Crimson;
			}
			else if (!_autoEnabled)
			{
				windowStateText = "AUTO OFF — manual only";
				windowStateBrush = Brushes.Orange;
			}
			else
			{
				bool inWin = !UseTimeFilter || InWindow();
				windowStateText = inWin ? "Armed — entries enabled" : "Outside window";
				windowStateBrush = inWin ? Brushes.LimeGreen : Brushes.Orange;
			}

			double dpnl = dailyPnl;
			string dailyText = string.Format("Day PnL {0}${1:F0}   Entries {2}", dpnl < 0 ? "-" : "+", Math.Abs(dpnl), dailyEntryCount);
			Brush dailyBrush = dpnl >= 0 ? Brushes.LimeGreen : Brushes.Salmon;

			string entryText = "", stopText = "", tgtText = "", qtyText = "", pnlText = "";
			Brush pnlBrush = Brushes.WhiteSmoke;
			if (inPos)
			{
				double avg = Position.AveragePrice;
				entryText = string.Format("Entry   {0:F2}", avg);
				if (currentStopPrice > 0)
					stopText = string.Format("Stop    {0:F2}{1}{2}", currentStopPrice, breakEvenSet ? "  (BE)" : "", _stopManuallyMoved ? "  (M)" : "");
				else
					stopText = "Stop    none";
				if (currentTargetPrice > 0)
					tgtText = string.Format("Target  {0:F2}{1}", currentTargetPrice, _targetManuallyMoved ? "  (M)" : "");
				else if (TpMode != TtExitMode.Off)
				{
					int tdir = isLong ? 1 : -1;
					double tp = TpMode == TtExitMode.Currency ? 0 : avg + tdir * ToTicks(TpMode, TpValue) * TickSize;
					tgtText = TpMode == TtExitMode.Currency ? string.Format("Target  ${0:F0}", TpValue) : string.Format("Target  {0:F2}", tp);
				}
				else tgtText = "Target  none";
				qtyText = string.Format("Qty     {0}", Position.Quantity);
				double upnl = 0;
				try { upnl = Position.GetUnrealizedProfitLoss(PerformanceUnit.Currency, MarkPrice()); } catch { }
				pnlText = string.Format("uPnL    {0}${1:F0}", upnl < 0 ? "-" : "+", Math.Abs(upnl));
				pnlBrush = upnl >= 0 ? Brushes.LimeGreen : Brushes.Salmon;
			}

			// Snapshots for the UI lambda
			string pt = posText, bt = biasText, it = instrText, wt = windowText, wst = windowStateText, dt = dailyText;
			string et = entryText, st = stopText, tgt = tgtText, qt = qtyText, plt = pnlText;
			Brush pb = posBrush, bb = biasBrush, wsb = windowStateBrush, plb = pnlBrush, db = dailyBrush;
			bool ip = inPos;
			bool autoOn = _autoEnabled, longOn = _longEnabled, shortOn = _shortEnabled;
			bool manualOn = EnableManualBrackets;

			ChartControl.Dispatcher.InvokeAsync(() =>
			{
				try
				{
					if (!_uiInitialized) return;
					if (_dashStatus != null) { _dashStatus.Text = pt; _dashStatus.Foreground = pb; }
					if (_dashBias != null) { _dashBias.Text = bt; _dashBias.Foreground = bb; }
					if (_dashInstrument != null) _dashInstrument.Text = it;
					if (_dashWindow != null) _dashWindow.Text = wt;
					if (_dashWindowState != null) { _dashWindowState.Text = wst; _dashWindowState.Foreground = wsb; }
					if (_dashDaily != null) { _dashDaily.Text = dt; _dashDaily.Foreground = db; }
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
					// Keep button styling in sync with the live flags
					RestyleToggle(_autoBtn, "AUTO", autoOn, BtnAutoBg, BtnAutoBdr);
					RestyleToggle(_longBtn, "LONG", longOn, BtnLongBg, BtnLongBdr);
					RestyleToggle(_shortBtn, "SHORT", shortOn, BtnShortBg, BtnShortBdr);
					if (_longBtn != null) _longBtn.IsEnabled = autoOn;
					if (_shortBtn != null) _shortBtn.IsEnabled = autoOn;
					// Manual nudge buttons: shown when the feature is on, active only in a position
					if (_nudgeRow != null) _nudgeRow.Visibility = manualOn ? Visibility.Visible : Visibility.Collapsed;
					if (_slDownBtn != null) _slDownBtn.IsEnabled = ip;
					if (_slUpBtn != null) _slUpBtn.IsEnabled = ip;
					if (_tpDownBtn != null) _tpDownBtn.IsEnabled = ip;
					if (_tpUpBtn != null) _tpUpBtn.IsEnabled = ip;
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
				Width = width,
				Height = height,
				Margin = new Thickness(2),
				MinWidth = 0,           // NT8 Button theme carries MinWidth ~98 → clears clipping
				Cursor = Cursors.Hand,
				FocusVisualStyle = null,
				Padding = new Thickness(0),
				Background = BtnInactBg,
				BorderBrush = BtnInactBdr,
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
			hf.SetValue(Border.BackgroundProperty, new SolidColorBrush(Color.FromArgb(40, 0xE0, 0xA8, 0x40)));
			hf.SetValue(Border.BorderBrushProperty, new SolidColorBrush(Color.FromRgb(0xE0, 0xA8, 0x40)));
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

		// Click handlers — named methods so teardown can unsubscribe them. They only flip
		// volatile flags / restyle; order actions run on the strategy thread.
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

		private void OnBEClick(object sender, RoutedEventArgs e)
		{
			_pendingBE = true;
		}

		private void OnFlattenClick(object sender, RoutedEventArgs e)
		{
			_pendingFlatten = true;
			_autoEnabled = false;   // flatten also pauses auto entries — re-arm via AUTO
			RestyleToggle(_autoBtn, "AUTO", false, BtnAutoBg, BtnAutoBdr);
			if (_longBtn != null) _longBtn.IsEnabled = false;
			if (_shortBtn != null) _shortBtn.IsEnabled = false;
		}

		// Nudge handlers — the UI thread only ACCUMULATES net ticks (Interlocked); the strategy
		// thread applies them in DrainManualNudges. ▲ raises the price, ▼ lowers it, by ManualNudgeTicks.
		private void OnSlDownClick(object sender, RoutedEventArgs e)
		{ System.Threading.Interlocked.Add(ref _pendingStopNudgeTicks, -Math.Max(1, ManualNudgeTicks)); }
		private void OnSlUpClick(object sender, RoutedEventArgs e)
		{ System.Threading.Interlocked.Add(ref _pendingStopNudgeTicks, Math.Max(1, ManualNudgeTicks)); }
		private void OnTpDownClick(object sender, RoutedEventArgs e)
		{ System.Threading.Interlocked.Add(ref _pendingTargetNudgeTicks, -Math.Max(1, ManualNudgeTicks)); }
		private void OnTpUpClick(object sender, RoutedEventArgs e)
		{ System.Threading.Interlocked.Add(ref _pendingTargetNudgeTicks, Math.Max(1, ManualNudgeTicks)); }

		private void OnPillMouseDown(object sender, MouseButtonEventArgs e) { e.Handled = true; }

		private void OnPillMouseUp(object sender, MouseButtonEventArgs e)
		{
			e.Handled = true;
			_dashMinimized = !_dashMinimized;
			if (_dashBody != null)
				_dashBody.Visibility = _dashMinimized ? Visibility.Collapsed : Visibility.Visible;
			if (_pillPath != null)
				_pillPath.Opacity = _dashMinimized ? 0.9 : 0.5;
			if (_pillBtn != null)
				_pillBtn.ToolTip = _dashMinimized ? "Click to restore" : "Click to minimize";
		}

		private void OnPillMouseEnter(object sender, MouseEventArgs e)
		{
			if (_pillPath != null) _pillPath.Opacity = 1.0;
		}

		private void OnPillMouseLeave(object sender, MouseEventArgs e)
		{
			if (_pillPath != null) _pillPath.Opacity = _dashMinimized ? 0.9 : 0.5;
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
		#endregion

		#region ICustomTypeDescriptor - per-instance property hiding (AGENTS.md pattern)
		// NT8's WpfPropertyGrid queries ICustomTypeDescriptor on the live instance, so this is the
		// only place per-instance visibility works (a TypeConverter is ignored here). Mode/toggle
		// params hide their dependent rows when not applicable; triggers carry [RefreshProperties(All)]
		// so the grid updates immediately. Pattern: GodZillaKilla / whiskysTPSLAdjuster / MafiaFlipSwitch.
		AttributeCollection ICustomTypeDescriptor.GetAttributes() => TypeDescriptor.GetAttributes(GetType());
		string ICustomTypeDescriptor.GetClassName() => TypeDescriptor.GetClassName(GetType());
		string ICustomTypeDescriptor.GetComponentName() => TypeDescriptor.GetComponentName(GetType());
		TypeConverter ICustomTypeDescriptor.GetConverter() => TypeDescriptor.GetConverter(GetType());
		EventDescriptor ICustomTypeDescriptor.GetDefaultEvent() => TypeDescriptor.GetDefaultEvent(GetType());
		PropertyDescriptor ICustomTypeDescriptor.GetDefaultProperty() => TypeDescriptor.GetDefaultProperty(GetType());
		object ICustomTypeDescriptor.GetEditor(Type editorBaseType) => TypeDescriptor.GetEditor(GetType(), editorBaseType);
		EventDescriptorCollection ICustomTypeDescriptor.GetEvents() => TypeDescriptor.GetEvents(GetType());
		EventDescriptorCollection ICustomTypeDescriptor.GetEvents(Attribute[] attrs) => TypeDescriptor.GetEvents(GetType(), attrs);
		object ICustomTypeDescriptor.GetPropertyOwner(PropertyDescriptor pd) => this;

		PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties() => ((ICustomTypeDescriptor)this).GetProperties(new Attribute[0]);

		PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties(Attribute[] attributes)
		{
			PropertyDescriptorCollection orig = TypeDescriptor.GetProperties(GetType(), attributes);
			PropertyDescriptor[] arr = new PropertyDescriptor[orig.Count];
			orig.CopyTo(arr, 0);
			PropertyDescriptorCollection col = new PropertyDescriptorCollection(arr);

			if (TpMode == TtExitMode.Off) RemoveProperties(col, nameof(TpValue));
			if (SlMode == TtSlMode.Off || SlMode == TtSlMode.EMA) RemoveProperties(col, nameof(SlValue));
			if (SlMode != TtSlMode.EMA) RemoveProperties(col, nameof(SlEmaPeriod), nameof(SlEmaBufferTicks));
			if (BeMode == TtSimpleMode.Off) RemoveProperties(col, nameof(BeTrigger), nameof(BeOffsetTicks));
			if (TrailMode == TtTrailMode.Off || TrailMode == TtTrailMode.EMA) RemoveProperties(col, nameof(TrailValue));
			if (TrailMode != TtTrailMode.EMA) RemoveProperties(col, nameof(TrailEmaPeriod), nameof(TrailEmaBufferTicks));
			if (VwmaMode == TtVwmaMode.Off) RemoveProperties(col, nameof(VwmaPeriod), nameof(ShowVwmaPlot));
			if (!VolumeFilter) RemoveProperties(col, nameof(VolumeSmaPeriod), nameof(VolumeMult));
			if (!UseDailyLoss) RemoveProperties(col, nameof(DailyLoss));
			if (!UseDailyProfit) RemoveProperties(col, nameof(DailyProfit));
			if (!UseDailyLoss && !UseDailyProfit) RemoveProperties(col, nameof(DailyFlatten));
			if (SizingMode != TtSizingMode.RiskBased) RemoveProperties(col, nameof(RiskPerTrade));
			if (!UseTimeFilter) RemoveProperties(col, nameof(StartTime), nameof(EndTime), nameof(FlattenAtEnd));
			if (!ShowDashboard) RemoveProperties(col, nameof(DashboardStartMinimized), nameof(DashboardCorner));
			if (!EnableManualBrackets) RemoveProperties(col, nameof(ManualNudgeTicks), nameof(ManualStopMode));

			return col;
		}

		private void RemoveProperties(PropertyDescriptorCollection col, params string[] names)
		{
			foreach (string n in names)
				if (col[n] != null) col.Remove(col[n]);
		}
		#endregion
	}
}
