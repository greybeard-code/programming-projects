#region Using declarations
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Text;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Core;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.NinjaScript;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

// ============================================================
//  gbKingPanaZilla  — GreyBeard composite signal indicator
//
//  Loads gbKingOrderBlock, gbPANAKanal, and gbThunderZilla and
//  emits three cross-system trade signal plots:
//
//  PanaZillia_Trade  — PanaKanal >= 2  AND Thunder >= 3  →  +1
//                    — PanaKanal <= -2 AND Thunder <= -3 →  -1
//  KingZilla_Trade   — Thunder  >= 3   AND KingOrder >= 1  →  +1
//                    — Thunder  <= -3  AND KingOrder <= -1 →  -1
//  KingPana_Trade    — PanaKanal >= 2  AND KingOrder >= 1  →  +1
//                    — PanaKanal <= -2 AND KingOrder <= -1 →  -1
//
//  Plot values: +1 = buy signal, -1 = sell signal, 0 = no signal.
//
//  gbPANAKanal Signal_Trade:
//    1 = Trend Start Up,   -1 = Trend Start Down
//    2 = Break Up,         -2 = Break Down
//    3 = Pullback Bullish, -3 = Pullback Bearish
//
//  gbThunderZilla Signal_Trade:
//    1 = Uptrend Start,    -1 = Downtrend Start
//    2 = Downtrend Slowdn, -2 = Uptrend Slowdown
//    3 = Uptrend Pullback, -3 = Downtrend Pullback
//    4 = Move Stop Up,     -4 = Move Stop Down
//
//  gbKingOrderBlock Signal_Trade:
//    1 = Return Bullish,   -1 = Return Bearish
//    2 = Breakout Bullish, -2 = Breakout Bearish
// ============================================================

namespace NinjaTrader.NinjaScript.Indicators.GreyBeard.KingPanaZilla
{
[CategoryOrder("General",                    1000010)]
[CategoryOrder("KingOrderBlock Parameters",  1000020)]
[CategoryOrder("PANAKanal Parameters",       1000030)]
[CategoryOrder("ThunderZilla Parameters",    1000040)]
[CategoryOrder("Visuals",                    1000050)]
[CategoryOrder("Logging",                    1000060)]
public class gbKingPanaZilla : Indicator
{
	// ---- child indicator references -------------------------
	// Cache arrays let us call CacheIndicator<T> directly, bypassing the
	// named factory methods (gbKingOrderBlock(...) etc.). This prevents NT8's
	// compiler from injecting duplicate factory declarations into this file's
	// generated-code section when all four files are compiled together.
	private GreyBeard.KingPanaZilla.gbKingOrderBlock   _king;
	private GreyBeard.KingPanaZilla.gbPANAKanal        _pana;
	private GreyBeard.KingPanaZilla.gbThunderZilla     _thunder;
	private GreyBeard.KingPanaZilla.gbKingOrderBlock[] _cacheKing;
	private GreyBeard.KingPanaZilla.gbPANAKanal[]      _cachePana;
	private GreyBeard.KingPanaZilla.gbThunderZilla[]   _cacheThunder;

	// ---- CSV logging ----------------------------------------
	private StreamWriter _logWriter;

	// ---- signal output series (Values[0..2]) ----------------
	// +1 = buy signal, -1 = sell signal, 0 = no signal
	[Browsable(false)]
	[XmlIgnore]
	public NinjaTrader.NinjaScript.Series<double> PanaZillia_Trade => Values[0];

	[Browsable(false)]
	[XmlIgnore]
	public NinjaTrader.NinjaScript.Series<double> KingZilla_Trade  => Values[1];

	[Browsable(false)]
	[XmlIgnore]
	public NinjaTrader.NinjaScript.Series<double> KingPana_Trade   => Values[2];

	// =========================================================
	protected override void OnStateChange()
	{
		switch (State)
		{
		case State.SetDefaults:
			Description              = "Composite signal indicator — combines gbKingOrderBlock, gbPANAKanal, and gbThunderZilla into three cross-system trade signals (+1 buy / -1 sell).";
			Name                     = "gbKingPanaZilla";
			Calculate                = Calculate.OnBarClose;
			IsOverlay                = true;
			DisplayInDataBox         = true;
			DrawOnPricePanel         = true;
			PaintPriceMarkers        = false;
			ScaleJustification       = ScaleJustification.Right;
			IsSuspendedWhileInactive = false;
			BarsRequiredToPlot       = 0;
			ShowTransparentPlotsInDataBox = true;

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
			// Transparent plots — +1/−1/0 readable from DataBox and other scripts.
			// Visual arrows are drawn in OnBarUpdate via Draw.ArrowUp/Down.
			AddPlot(new Stroke(Brushes.Transparent, 1), PlotStyle.Dot, "PanaZillia Trade");
			AddPlot(new Stroke(Brushes.Transparent, 1), PlotStyle.Dot, "KingZilla Trade");
			AddPlot(new Stroke(Brushes.Transparent, 1), PlotStyle.Dot, "KingPana Trade");
			break;

		case State.DataLoaded:
			// Factory methods (CacheIndicator) add child indicators to NinjaScripts so
			// their OnBarUpdate runs automatically. AddChartIndicator is Strategy-only;
			// add the three child indicators directly to the chart if visual rendering
			// of their zones/stops is also needed.
			if (LogEnabled)
			{
				string logPath = Path.Combine(
					Globals.UserDataDir,
					"gbKPZlog_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".csv");
				_logWriter = new StreamWriter(logPath, append: false, encoding: Encoding.UTF8);
				_logWriter.WriteLine("DateTime,Instrument,Price,PanaZillia_Trade,KingZilla_Trade,KingPana_Trade");
				_logWriter.Flush();
			}

			_king = CacheIndicator<GreyBeard.KingPanaZilla.gbKingOrderBlock>(
				new GreyBeard.KingPanaZilla.gbKingOrderBlock
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

			_pana = CacheIndicator<GreyBeard.KingPanaZilla.gbPANAKanal>(
				new GreyBeard.KingPanaZilla.gbPANAKanal
				{
					Period                      = Pana_Period,
					Factor                      = Pana_Factor,
					MiddlePeriod                = Pana_MiddlePeriod,
					SignalBreakSplit             = Pana_SignalBreakSplit,
					SignalPullbackFindingPeriod  = Pana_SignalPullbackFindingPeriod
				},
				Input, ref _cachePana);

			_thunder = CacheIndicator<GreyBeard.KingPanaZilla.gbThunderZilla>(
				new GreyBeard.KingPanaZilla.gbThunderZilla
				{
					TrendMAType               = Thunder_TrendMAType,
					TrendPeriod               = Thunder_TrendPeriod,
					TrendSmoothingEnabled     = Thunder_TrendSmoothingEnabled,
					TrendSmoothingMethod      = Thunder_TrendSmoothingMethod,
					TrendSmoothingPeriod      = Thunder_TrendSmoothingPeriod,
					StopOffsetMultiplierStop  = Thunder_StopOffsetMultiplierStop,
					SignalQuantityPerFlat      = Thunder_SignalQuantityPerFlat,
					SignalQuantityPerTrend     = Thunder_SignalQuantityPerTrend
				},
				Input, ref _cacheThunder);
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
		if (_pana == null || _thunder == null || _king == null)
			return;

		// Reset all signal plots each bar
		Values[0][0] = Values[1][0] = Values[2][0] = 0;

		double pkSig  = _pana.Signal_Trade[0];
		double tzSig  = _thunder.Signal_Trade[0];
		double koSig  = _king.Signal_Trade[0];
		double offset = ArrowOffset * TickSize;

		// ---- PanaZillia Trade ----
		if (pkSig >= 2 && tzSig >= 3)
		{
			Values[0][0] = 1;
			Draw.ArrowUp(this, "KPZ_PZ_" + CurrentBar, false,
				0, Low[0] - offset, PanaZilliaBrush);
		}
		else if (pkSig <= -2 && tzSig <= -3)
		{
			Values[0][0] = -1;
			Draw.ArrowDown(this, "KPZ_PZ_" + CurrentBar, false,
				0, High[0] + offset, PanaZilliaBrush);
		}

		// ---- KingZilla Trade ----
		if (tzSig >= 3 && koSig >= 1)
		{
			Values[1][0] = 1;
			Draw.ArrowUp(this, "KPZ_KZ_" + CurrentBar, false,
				0, Low[0] - offset, KingZillaBrush);
		}
		else if (tzSig <= -3 && koSig <= -1)
		{
			Values[1][0] = -1;
			Draw.ArrowDown(this, "KPZ_KZ_" + CurrentBar, false,
				0, High[0] + offset, KingZillaBrush);
		}

		// ---- KingPana Trade ----
		if (pkSig >= 2 && koSig >= 1)
		{
			Values[2][0] = 1;
			Draw.ArrowUp(this, "KPZ_KP_" + CurrentBar, false,
				0, Low[0] - offset, KingPanaBrush);
		}
		else if (pkSig <= -2 && koSig <= -1)
		{
			Values[2][0] = -1;
			Draw.ArrowDown(this, "KPZ_KP_" + CurrentBar, false,
				0, High[0] + offset, KingPanaBrush);
		}

		// ---- CSV log (any signal fires) ---------------------
		if (_logWriter != null && (Values[0][0] != 0 || Values[1][0] != 0 || Values[2][0] != 0))
		{
			_logWriter.WriteLine(string.Format("{0},\"{1}\",{2},{3},{4},{5}",
				Time[0].ToString("yyyy-MM-dd HH:mm:ss"),
				Instrument.FullName,
				Close[0].ToString(System.Globalization.CultureInfo.InvariantCulture),
				(int)Values[0][0],
				(int)Values[1][0],
				(int)Values[2][0]));
			_logWriter.Flush();
		}
	}

	// =========================================================
	#region Properties

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
		            + "File is named gbKPZlog_YYYYMMDD_HHmmss.csv and created when the indicator loads. "
		            + "One row is written per bar on which at least one trade signal fires.")]
	public bool LogEnabled { get; set; }

	#endregion

} // class gbKingPanaZilla

} // namespace NinjaTrader.NinjaScript.Indicators.GreyBeard.KingPanaZilla

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private GreyBeard.KingPanaZilla.gbKingPanaZilla[] cachegbKingPanaZilla;

		public GreyBeard.KingPanaZilla.gbKingPanaZilla gbKingPanaZilla()
		{
			return gbKingPanaZilla(Input);
		}

		public GreyBeard.KingPanaZilla.gbKingPanaZilla gbKingPanaZilla(ISeries<double> input)
		{
			if (cachegbKingPanaZilla != null)
				for (int idx = 0; idx < cachegbKingPanaZilla.Length; idx++)
					if (cachegbKingPanaZilla[idx] != null && cachegbKingPanaZilla[idx].EqualsInput(input))
						return cachegbKingPanaZilla[idx];
			return CacheIndicator<GreyBeard.KingPanaZilla.gbKingPanaZilla>(new GreyBeard.KingPanaZilla.gbKingPanaZilla(){}, input, ref cachegbKingPanaZilla);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.GreyBeard.KingPanaZilla.gbKingPanaZilla gbKingPanaZilla()
		{
			return indicator.gbKingPanaZilla(Input);
		}

		public Indicators.GreyBeard.KingPanaZilla.gbKingPanaZilla gbKingPanaZilla(ISeries<double> input)
		{
			return indicator.gbKingPanaZilla(input);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.GreyBeard.KingPanaZilla.gbKingPanaZilla gbKingPanaZilla()
		{
			return indicator.gbKingPanaZilla(Input);
		}

		public Indicators.GreyBeard.KingPanaZilla.gbKingPanaZilla gbKingPanaZilla(ISeries<double> input)
		{
			return indicator.gbKingPanaZilla(input);
		}
	}
}

#endregion
