#region Using declarations
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Text;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Core;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.NinjaScript;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.DrawingTools;
using NinjaTrader.NinjaScript.Indicators.GreyBeard.KingPanaZilla;
#endregion

// ============================================================
//  gbKingPanaZilla  — GreyBeard composite STRATEGY
//
//  Loads gbKingOrderBlock, gbPANAKanal, and gbThunderZilla via
//  CacheIndicator<T> + AddChartIndicator so their signals drive
//  trade entries AND their full visual drawings appear on chart.
//
//  Three cross-system signal combinations:
//
//  PanaZillia  — PanaKanal >= 2  AND ThunderZilla >= 3
//  KingZilla   — ThunderZilla >= 3  AND KingOrderBlock >= 1
//  KingPana    — PanaKanal >= 2  AND KingOrderBlock >= 1
//
//  Any enabled BUY combination  → EnterLong  (exit prior Short)
//  Any enabled SELL combination → EnterShort (exit prior Long)
//
//  Signal scale reference:
//    gbPANAKanal Signal_Trade   : 1=Trend Start, 2=Break, 3=Pullback  (mirror negative)
//    gbThunderZilla Signal_Trade: 1=Trend Start, 2=Slowdown, 3=Pullback, 4=MoveStop (mirror)
//    gbKingOrderBlock Signal_Trade: 1=Return, 2=Breakout  (mirror negative)
// ============================================================

namespace NinjaTrader.NinjaScript.Strategies
{
[CategoryOrder("General",                    1000010)]
[CategoryOrder("Signal Selection",           1000015)]
[CategoryOrder("KingOrderBlock Parameters",  1000020)]
[CategoryOrder("PANAKanal Parameters",       1000030)]
[CategoryOrder("ThunderZilla Parameters",    1000040)]
[CategoryOrder("Visuals",                    1000050)]
[CategoryOrder("Logging",                    1000060)]
public class gbKingPanaZilla : Strategy
{
	// ---- child indicator references -------------------------
	// CacheIndicator<T> is used (not named factory methods) so NT8's scanner
	// does not inject duplicate factory declarations into this file's
	// generated-code section when all four files are compiled together.
	private gbKingOrderBlock   _king;
	private gbPANAKanal        _pana;
	private gbThunderZilla     _thunder;
	private gbKingOrderBlock[] _cacheKing;
	private gbPANAKanal[]      _cachePana;
	private gbThunderZilla[]   _cacheThunder;

	// ---- CSV logging ----------------------------------------
	private StreamWriter _logWriter;

	// =========================================================
	protected override void OnStateChange()
	{
		switch (State)
		{
		case State.SetDefaults:
			Description = "Composite strategy — combines gbKingOrderBlock, gbPANAKanal, and gbThunderZilla "
			            + "signals into three cross-system entry signals. Child indicator drawings are "
			            + "rendered on the chart via AddChartIndicator.";
			Name                                       = "gbKingPanaZilla";
			Calculate                                  = Calculate.OnBarClose;
			EntriesPerDirection                        = 1;
			EntryHandling                              = EntryHandling.AllEntries;
			IsExitOnSessionCloseStrategy               = true;
			ExitOnSessionCloseSeconds                  = 30;
			IsFillLimitOnTouch                         = false;
			MaximumBarsLookBack                        = MaximumBarsLookBack.TwoHundredFiftySix;
			OrderFillResolution                        = OrderFillResolution.Standard;
			Slippage                                   = 0;
			StartBehavior                              = StartBehavior.WaitUntilFlat;
			TimeInForce                                = TimeInForce.Gtc;
			TraceOrders                                = false;
			RealtimeErrorHandling                      = RealtimeErrorHandling.StopCancelClose;
			StopTargetHandling                         = StopTargetHandling.PerEntryExecution;
			BarsRequiredToTrade                        = 20;
			IsInstantiatedOnEachOptimizationIteration  = true;

			// ---- Signal selection defaults ------------------
			TradePanaZillia = true;
			TradeKingZilla  = true;
			TradeKingPana   = true;
			TradeLong       = true;
			TradeShort      = true;

			// ---- KingOrderBlock defaults --------------------
			King_SwingPointNeighborhood              = 5;
			King_ImbalanceQualifying                 = 3;
			King_OrderBlockFindingBosChochPeriod     = 50;
			King_OrderBlockAge                       = 500;
			King_OrderBlocksSameDirectionOffset      = 10;
			King_OrderBlocksDifferenceDirectionOffset= 10;
			King_SignalTradeQuantityPerOrderBlock     = 3;
			King_SignalTradeSplitBars                 = 6;

			// ---- PANAKanal defaults -------------------------
			Pana_Period                              = 20;
			Pana_Factor                              = 4.0;
			Pana_MiddlePeriod                        = 14;
			Pana_SignalBreakSplit                     = 20;
			Pana_SignalPullbackFindingPeriod          = 10;

			// ---- ThunderZilla defaults ----------------------
			Thunder_TrendMAType                      = gbThunderZillaMAType.SMA;
			Thunder_TrendPeriod                      = 100;
			Thunder_TrendSmoothingEnabled            = false;
			Thunder_TrendSmoothingMethod             = gbThunderZillaMAType.EMA;
			Thunder_TrendSmoothingPeriod             = 10;
			Thunder_StopOffsetMultiplierStop         = 60.0;
			Thunder_SignalQuantityPerFlat             = 2;
			Thunder_SignalQuantityPerTrend            = 999;

			// ---- Visual defaults ----------------------------
			PanaZilliaBrush = Brushes.Cyan;
			KingZillaBrush  = Brushes.DodgerBlue;
			KingPanaBrush   = Brushes.LimeGreen;
			ArrowOffset     = 3;

			// ---- Logging defaults ---------------------------
			LogEnabled = false;
			break;

		case State.Configure:
			// CacheIndicator<T> wires each child into the data pipeline
			// (OnBarUpdate). AddChartIndicator adds each to the chart panel
			// so its OnRender fires and all visual drawings appear on screen.
			_king = CacheIndicator<gbKingOrderBlock>(
				new gbKingOrderBlock
				{
					SwingPointNeighborhood               = King_SwingPointNeighborhood,
					ImbalanceQualifying                  = King_ImbalanceQualifying,
					OrderBlockFindingBosChochPeriod      = King_OrderBlockFindingBosChochPeriod,
					OrderBlockAge                        = King_OrderBlockAge,
					OrderBlocksSameDirectionOffset       = King_OrderBlocksSameDirectionOffset,
					OrderBlocksDifferenceDirectionOffset = King_OrderBlocksDifferenceDirectionOffset,
					SignalTradeQuantityPerOrderBlock      = King_SignalTradeQuantityPerOrderBlock,
					SignalTradeSplitBars                 = King_SignalTradeSplitBars
				},
				Input, ref _cacheKing);
			AddChartIndicator(_king);

			_pana = CacheIndicator<gbPANAKanal>(
				new gbPANAKanal
				{
					Period                     = Pana_Period,
					Factor                     = Pana_Factor,
					MiddlePeriod               = Pana_MiddlePeriod,
					SignalBreakSplit            = Pana_SignalBreakSplit,
					SignalPullbackFindingPeriod = Pana_SignalPullbackFindingPeriod
				},
				Input, ref _cachePana);
			AddChartIndicator(_pana);

			_thunder = CacheIndicator<gbThunderZilla>(
				new gbThunderZilla
				{
					TrendMAType              = Thunder_TrendMAType,
					TrendPeriod              = Thunder_TrendPeriod,
					TrendSmoothingEnabled    = Thunder_TrendSmoothingEnabled,
					TrendSmoothingMethod     = Thunder_TrendSmoothingMethod,
					TrendSmoothingPeriod     = Thunder_TrendSmoothingPeriod,
					StopOffsetMultiplierStop = Thunder_StopOffsetMultiplierStop,
					SignalQuantityPerFlat    = Thunder_SignalQuantityPerFlat,
					SignalQuantityPerTrend   = Thunder_SignalQuantityPerTrend
				},
				Input, ref _cacheThunder);
			AddChartIndicator(_thunder);
			break;

		case State.DataLoaded:
			if (LogEnabled)
			{
				string logPath = Path.Combine(
					Globals.UserDataDir,
					"gbKPZlog_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".csv");
				_logWriter = new StreamWriter(logPath, append: false, encoding: Encoding.UTF8);
				_logWriter.WriteLine("DateTime,Instrument,Price,PanaZillia_Trade,KingZilla_Trade,KingPana_Trade");
				_logWriter.Flush();
			}
			break;

		case State.Terminated:
			_king         = null;
			_pana         = null;
			_thunder      = null;
			_cacheKing    = null;
			_cachePana    = null;
			_cacheThunder = null;
			if (_logWriter != null)
			{
				_logWriter.Flush();
				_logWriter.Dispose();
				_logWriter = null;
			}
			break;
		}
	}

	// =========================================================
	protected override void OnBarUpdate()
	{
		if (BarsInProgress != 0) return;
		if (CurrentBar < BarsRequiredToTrade) return;
		if (_pana == null || _thunder == null || _king == null) return;

		double pkSig  = _pana.Signal_Trade[0];
		double tzSig  = _thunder.Signal_Trade[0];
		double koSig  = _king.Signal_Trade[0];
		double offset = ArrowOffset * TickSize;

		// ---- Evaluate the three signal combinations ----------
		bool pzBuy  = TradePanaZillia && pkSig >= 2 && tzSig >= 3;
		bool pzSell = TradePanaZillia && pkSig <= -2 && tzSig <= -3;
		bool kzBuy  = TradeKingZilla  && tzSig >= 3 && koSig >= 1;
		bool kzSell = TradeKingZilla  && tzSig <= -3 && koSig <= -1;
		bool kpBuy  = TradeKingPana   && pkSig >= 2 && koSig >= 1;
		bool kpSell = TradeKingPana   && pkSig <= -2 && koSig <= -1;

		bool anyBuy  = pzBuy  || kzBuy  || kpBuy;
		bool anySell = pzSell || kzSell || kpSell;

		// Priority for arrow colour: PanaZillia > KingZilla > KingPana
		Brush buyBrush  = pzBuy  ? PanaZilliaBrush : kzBuy  ? KingZillaBrush : KingPanaBrush;
		Brush sellBrush = pzSell ? PanaZilliaBrush : kzSell ? KingZillaBrush : KingPanaBrush;

		// ---- Order entries -----------------------------------
		if (anyBuy && TradeLong)
		{
			if (Position.MarketPosition == MarketPosition.Short)
				ExitShort("ExitShort", "EnterLong");
			EnterLong("EnterLong");
			Draw.ArrowUp(this, "KPZ_buy_" + CurrentBar, false,
				0, Low[0] - offset, buyBrush);
		}
		else if (anySell && TradeShort)
		{
			if (Position.MarketPosition == MarketPosition.Long)
				ExitLong("ExitLong", "EnterShort");
			EnterShort("EnterShort");
			Draw.ArrowDown(this, "KPZ_sell_" + CurrentBar, false,
				0, High[0] + offset, sellBrush);
		}

		// ---- CSV log (any signal fires) ----------------------
		int pzVal = pzBuy ? 1 : (pzSell ? -1 : 0);
		int kzVal = kzBuy ? 1 : (kzSell ? -1 : 0);
		int kpVal = kpBuy ? 1 : (kpSell ? -1 : 0);

		if (_logWriter != null && (pzVal != 0 || kzVal != 0 || kpVal != 0))
		{
			_logWriter.WriteLine(string.Format("{0},\"{1}\",{2},{3},{4},{5}",
				Time[0].ToString("yyyy-MM-dd HH:mm:ss"),
				Instrument.FullName,
				Close[0].ToString(System.Globalization.CultureInfo.InvariantCulture),
				pzVal, kzVal, kpVal));
			_logWriter.Flush();
		}
	}

	// =========================================================
	#region Properties

	// ---- Signal selection -----------------------------------
	[Display(Name = "Trade PanaZillia", Order = 0, GroupName = "Signal Selection",
		Description = "Enable entries on the PanaZillia signal (PANAKanal ≥ 2 AND ThunderZilla ≥ 3).")]
	public bool TradePanaZillia { get; set; }

	[Display(Name = "Trade KingZilla", Order = 1, GroupName = "Signal Selection",
		Description = "Enable entries on the KingZilla signal (ThunderZilla ≥ 3 AND KingOrderBlock ≥ 1).")]
	public bool TradeKingZilla { get; set; }

	[Display(Name = "Trade KingPana", Order = 2, GroupName = "Signal Selection",
		Description = "Enable entries on the KingPana signal (PANAKanal ≥ 2 AND KingOrderBlock ≥ 1).")]
	public bool TradeKingPana { get; set; }

	[Display(Name = "Trade Long", Order = 3, GroupName = "Signal Selection",
		Description = "Allow long entries when a buy signal fires.")]
	public bool TradeLong { get; set; }

	[Display(Name = "Trade Short", Order = 4, GroupName = "Signal Selection",
		Description = "Allow short entries when a sell signal fires.")]
	public bool TradeShort { get; set; }

	// ---- KingOrderBlock parameters --------------------------
	[Display(Name = "Swing Point: Neighborhood", Order = 0, GroupName = "KingOrderBlock Parameters")]
	[Range(1, int.MaxValue)]
	public int King_SwingPointNeighborhood { get; set; }

	[Display(Name = "Imbalance: Qualifying (Bars)", Order = 10, GroupName = "KingOrderBlock Parameters")]
	[Range(1, int.MaxValue)]
	public int King_ImbalanceQualifying { get; set; }

	[Display(Name = "Order Block: Finding BOS/CHoCH Period", Order = 20, GroupName = "KingOrderBlock Parameters")]
	[Range(1, int.MaxValue)]
	public int King_OrderBlockFindingBosChochPeriod { get; set; }

	[Display(Name = "Order Block: Age (Bars)", Order = 30, GroupName = "KingOrderBlock Parameters")]
	[Range(0, int.MaxValue)]
	public int King_OrderBlockAge { get; set; }

	[Display(Name = "Order Blocks: Same Direction Offset (Ticks)", Order = 40, GroupName = "KingOrderBlock Parameters")]
	[Range(0, int.MaxValue)]
	public int King_OrderBlocksSameDirectionOffset { get; set; }

	[Display(Name = "Order Blocks: Diff Direction Offset (Ticks)", Order = 50, GroupName = "KingOrderBlock Parameters")]
	[Range(0, int.MaxValue)]
	public int King_OrderBlocksDifferenceDirectionOffset { get; set; }

	[Display(Name = "Signal Trade: Quantity Per OB", Order = 60, GroupName = "KingOrderBlock Parameters")]
	[Range(1, int.MaxValue)]
	public int King_SignalTradeQuantityPerOrderBlock { get; set; }

	[Display(Name = "Signal Trade: Split (Bars)", Order = 70, GroupName = "KingOrderBlock Parameters")]
	[Range(1, int.MaxValue)]
	public int King_SignalTradeSplitBars { get; set; }

	// ---- PANAKanal parameters --------------------------------
	[Display(Name = "Period", Order = 0, GroupName = "PANAKanal Parameters")]
	[Range(1, int.MaxValue)]
	public int Pana_Period { get; set; }

	[Display(Name = "Factor", Order = 10, GroupName = "PANAKanal Parameters")]
	[Range(0.01, double.MaxValue)]
	public double Pana_Factor { get; set; }

	[Display(Name = "Middle Period", Order = 20, GroupName = "PANAKanal Parameters")]
	[Range(1, int.MaxValue)]
	public int Pana_MiddlePeriod { get; set; }

	[Display(Name = "Signal Break Split (Bars)", Order = 30, GroupName = "PANAKanal Parameters")]
	[Range(1, int.MaxValue)]
	public int Pana_SignalBreakSplit { get; set; }

	[Display(Name = "Signal Pullback Finding Period", Order = 40, GroupName = "PANAKanal Parameters")]
	[Range(1, int.MaxValue)]
	public int Pana_SignalPullbackFindingPeriod { get; set; }

	// ---- ThunderZilla parameters ----------------------------
	[Display(Name = "Trend: MA Type", Order = 0, GroupName = "ThunderZilla Parameters")]
	public gbThunderZillaMAType Thunder_TrendMAType { get; set; }

	[Display(Name = "Trend: Period", Order = 10, GroupName = "ThunderZilla Parameters")]
	[Range(1, int.MaxValue)]
	public int Thunder_TrendPeriod { get; set; }

	[Display(Name = "Trend: Smoothing Enabled", Order = 20, GroupName = "ThunderZilla Parameters")]
	public bool Thunder_TrendSmoothingEnabled { get; set; }

	[Display(Name = "Trend: Smoothing Method", Order = 30, GroupName = "ThunderZilla Parameters")]
	public gbThunderZillaMAType Thunder_TrendSmoothingMethod { get; set; }

	[Display(Name = "Trend: Smoothing Period", Order = 40, GroupName = "ThunderZilla Parameters")]
	[Range(1, int.MaxValue)]
	public int Thunder_TrendSmoothingPeriod { get; set; }

	[Display(Name = "Stop: Offset Multiplier (Ticks)", Order = 50, GroupName = "ThunderZilla Parameters")]
	[Range(0.0, double.MaxValue)]
	public double Thunder_StopOffsetMultiplierStop { get; set; }

	[Display(Name = "Signal: Quantity Per Flat", Order = 60, GroupName = "ThunderZilla Parameters")]
	[Range(1, int.MaxValue)]
	public int Thunder_SignalQuantityPerFlat { get; set; }

	[Display(Name = "Signal: Quantity Per Trend", Order = 70, GroupName = "ThunderZilla Parameters")]
	[Range(1, int.MaxValue)]
	public int Thunder_SignalQuantityPerTrend { get; set; }

	// ---- Visual parameters -----------------------------------
	[Display(Name = "PanaZillia Color", Order = 0, GroupName = "Visuals")]
	[XmlIgnore]
	public Brush PanaZilliaBrush { get; set; }
	[Browsable(false)]
	public string PanaZilliaBrushSerialize
	{
		get { return Serialize.BrushToString(PanaZilliaBrush); }
		set { PanaZilliaBrush = Serialize.StringToBrush(value); }
	}

	[Display(Name = "KingZilla Color", Order = 1, GroupName = "Visuals")]
	[XmlIgnore]
	public Brush KingZillaBrush { get; set; }
	[Browsable(false)]
	public string KingZillaBrushSerialize
	{
		get { return Serialize.BrushToString(KingZillaBrush); }
		set { KingZillaBrush = Serialize.StringToBrush(value); }
	}

	[Display(Name = "KingPana Color", Order = 2, GroupName = "Visuals")]
	[XmlIgnore]
	public Brush KingPanaBrush { get; set; }
	[Browsable(false)]
	public string KingPanaBrushSerialize
	{
		get { return Serialize.BrushToString(KingPanaBrush); }
		set { KingPanaBrush = Serialize.StringToBrush(value); }
	}

	[Display(Name = "Arrow Offset (Ticks)", Order = 3, GroupName = "Visuals")]
	[Range(0, int.MaxValue)]
	public int ArrowOffset { get; set; }

	// ---- Logging properties ---------------------------------
	[Display(Name = "Enabled", Order = 0, GroupName = "Logging",
		Description = "Write a CSV signal log to the NinjaTrader user data folder. "
		            + "File is named gbKPZlog_YYYYMMDD_HHmmss.csv and created when the strategy loads. "
		            + "One row is written per bar on which at least one trade signal fires.")]
	public bool LogEnabled { get; set; }

	#endregion

} // class gbKingPanaZilla

} // namespace NinjaTrader.NinjaScript.Strategies

#region NinjaScript generated code. Neither change nor remove.

// Strategy file: no indicator factory method injections needed here.
// Each child indicator file (gbKingOrderBlock.cs, gbPANAKanal.cs,
// gbThunderZilla.cs) carries its own generated section covering all
// three partial classes (Indicator, MarketAnalyzerColumn, Strategy).
// This file uses CacheIndicator<T> directly — NT8's scanner does not
// inject factory declarations for generic CacheIndicator calls.

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
	}
}

#endregion
