using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;

namespace NinjaTrader.NinjaScript.Indicators
{
	public class StochasticFullTOS1v0 : Indicator
	{
		private Series<double> fastK { get; set; }

		[XmlIgnore]
		[Browsable(false)]
		public Series<double> FullKSeries => Values[0];

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> FullDSeries => Values[1];

		[NinjaScriptProperty]
		[Display(Name = "OverBought", Description = "", Order = 10, GroupName = "Parameters")]
		[Range(1, int.MaxValue)]
		public int OverBought { get; set; }

		[Display(Name = "OverSold", Description = "", Order = 20, GroupName = "Parameters")]
		[Range(1, int.MaxValue)]
		[NinjaScriptProperty]
		public int OverSold { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "KPeriod", Description = "", Order = 30, GroupName = "Parameters")]
		[Range(1, int.MaxValue)]
		public int KPeriod { get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "DPeriod", Description = "", Order = 40, GroupName = "Parameters")]
		public int DPeriod { get; set; }

		[Display(Name = "ShowBreakoutSignals", Description = "", Order = 60, GroupName = "Parameters")]
		[Browsable(false)]
		public StochasticFullTOS1v0_ShowBreakoutSignals ShowBreakoutSignals { get; set; }

		[Display(Name = "priceH", Description = "", Order = 80, GroupName = "Parameters")]
		[NinjaScriptProperty]
		public StochasticFullTOS1v0_Price priceH { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "priceL", Description = "", Order = 90, GroupName = "Parameters")]
		public StochasticFullTOS1v0_Price priceL { get; set; }

		[Display(Name = "priceC", Description = "", Order = 100, GroupName = "Parameters")]
		[NinjaScriptProperty]
		public StochasticFullTOS1v0_Price priceC { get; set; }

		[Range(1, int.MaxValue)]
		[NinjaScriptProperty]
		[Display(Name = "SlowingPeriod", Description = "", Order = 50, GroupName = "Parameters")]
		public int SlowingPeriod { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "avgType", Description = "", Order = 70, GroupName = "Parameters")]
		public StochasticFullTOS1v0_AverageType averageType { get; set; }

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description = "Stochastic Full TOS";
				Calculate = Calculate.OnPriceChange;
				OverBought = 80;
				OverSold = 20;
				KPeriod = 10;
				DPeriod = 10;
				SlowingPeriod = 3;
				ShowBreakoutSignals = StochasticFullTOS1v0_ShowBreakoutSignals.No;
				averageType = StochasticFullTOS1v0_AverageType.SIMPLE;
				priceH = StochasticFullTOS1v0_Price.High;
				priceL = StochasticFullTOS1v0_Price.Low;
				priceC = StochasticFullTOS1v0_Price.Close;
				AddPlot(new Stroke(Brushes.DeepSkyBlue, 1f), PlotStyle.TriangleDown, "FullK");
				AddPlot(new Stroke(Brushes.Red, 1f), PlotStyle.TriangleDown, "FullD");
				AddLine(Brushes.Green, (double)OverBought, "OverBought");
				AddLine(Brushes.Green, (double)OverSold, "OverSold");
				fastK = new Series<double>(this, MaximumBarsLookBack.Infinite);
			}
		}

		protected override void OnBarUpdate()
		{
			if (CurrentBar < KPeriod)
			{
				return;
			}
			double num = MIN(Price(priceL), KPeriod)[0];
			double num2 = Price(priceC)[0] - num;
			double num3 = MAX(Price(priceH), KPeriod)[0] - num;
			fastK[0] = ((num3 != 0.0) ? (num2 / num3 * 100.0) : 0.0);
			FullKSeries[0] = MovingAverage(fastK, averageType, SlowingPeriod)[0];
			FullDSeries[0] = MovingAverage(FullKSeries, averageType, DPeriod)[0];
			bool flag = CrossAbove(FullKSeries, (double)OverSold, 1);
			bool flag2 = CrossAbove(FullDSeries, (double)OverSold, 1);
			bool flag3 = CrossBelow(FullKSeries, (double)OverBought, 1);
			bool flag4 = CrossBelow(FullDSeries, (double)OverBought, 1);
			switch (ShowBreakoutSignals)
			{
			case StochasticFullTOS1v0_ShowBreakoutSignals.OnFullK:
				if (flag)
				{
					_ = OverSold;
				}
				if (flag3)
				{
					_ = OverBought;
				}
				break;
			case StochasticFullTOS1v0_ShowBreakoutSignals.OnFullD:
				if (flag2)
				{
					_ = OverSold;
				}
				if (flag4)
				{
					_ = OverBought;
				}
				break;
			case StochasticFullTOS1v0_ShowBreakoutSignals.OnFullKAndFullD:
				if (flag || flag2)
				{
					_ = OverSold;
				}
				if (flag3 || flag4)
				{
					_ = OverBought;
				}
				break;
			case StochasticFullTOS1v0_ShowBreakoutSignals.No:
				break;
			}
		}

		private ISeries<double> MovingAverage(ISeries<double> input, StochasticFullTOS1v0_AverageType averageType, int length)
		{
			switch (averageType)
			{
			default:
				return SMA(Input, length);
			case StochasticFullTOS1v0_AverageType.HULL:
				return HMA(input, length);
			case StochasticFullTOS1v0_AverageType.WILDERS:
				return WilderMA1v0(input, length);
			case StochasticFullTOS1v0_AverageType.WEIGHTED:
				return WMA(input, length);
			case StochasticFullTOS1v0_AverageType.EXPONENTIAL:
				return EMA(input, length);
			case StochasticFullTOS1v0_AverageType.SIMPLE:
				return SMA(input, length);
			}
		}

		private ISeries<double> Price(StochasticFullTOS1v0_Price averageType, int input = 0)
		{
			switch (averageType)
			{
			default:
				return Closes[input];
			case StochasticFullTOS1v0_Price.OHLC4:
				return Weighteds[input];
			case StochasticFullTOS1v0_Price.HLC3:
				return Typicals[input];
			case StochasticFullTOS1v0_Price.HL2:
				return Medians[input];
			case StochasticFullTOS1v0_Price.Close:
				return Closes[input];
			case StochasticFullTOS1v0_Price.Open:
				return Opens[input];
			case StochasticFullTOS1v0_Price.Low:
				return Lows[input];
			case StochasticFullTOS1v0_Price.High:
				return Highs[input];
			}
		}
	}
}


#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private StochasticFullTOS1v0[] cacheStochasticFullTOS1v0;
		public StochasticFullTOS1v0 StochasticFullTOS1v0(int overBought, int overSold, int kPeriod, int dPeriod, StochasticFullTOS1v0_Price priceH, StochasticFullTOS1v0_Price priceL, StochasticFullTOS1v0_Price priceC, int slowingPeriod, StochasticFullTOS1v0_AverageType averageType)
		{
			return StochasticFullTOS1v0(Input, overBought, overSold, kPeriod, dPeriod, priceH, priceL, priceC, slowingPeriod, averageType);
		}

		public StochasticFullTOS1v0 StochasticFullTOS1v0(ISeries<double> input, int overBought, int overSold, int kPeriod, int dPeriod, StochasticFullTOS1v0_Price priceH, StochasticFullTOS1v0_Price priceL, StochasticFullTOS1v0_Price priceC, int slowingPeriod, StochasticFullTOS1v0_AverageType averageType)
		{
			if (cacheStochasticFullTOS1v0 != null)
				for (int idx = 0; idx < cacheStochasticFullTOS1v0.Length; idx++)
					if (cacheStochasticFullTOS1v0[idx] != null && cacheStochasticFullTOS1v0[idx].OverBought == overBought && cacheStochasticFullTOS1v0[idx].OverSold == overSold && cacheStochasticFullTOS1v0[idx].KPeriod == kPeriod && cacheStochasticFullTOS1v0[idx].DPeriod == dPeriod && cacheStochasticFullTOS1v0[idx].priceH == priceH && cacheStochasticFullTOS1v0[idx].priceL == priceL && cacheStochasticFullTOS1v0[idx].priceC == priceC && cacheStochasticFullTOS1v0[idx].SlowingPeriod == slowingPeriod && cacheStochasticFullTOS1v0[idx].averageType == averageType && cacheStochasticFullTOS1v0[idx].EqualsInput(input))
						return cacheStochasticFullTOS1v0[idx];
			return CacheIndicator<StochasticFullTOS1v0>(new StochasticFullTOS1v0(){ OverBought = overBought, OverSold = overSold, KPeriod = kPeriod, DPeriod = dPeriod, priceH = priceH, priceL = priceL, priceC = priceC, SlowingPeriod = slowingPeriod, averageType = averageType }, input, ref cacheStochasticFullTOS1v0);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.StochasticFullTOS1v0 StochasticFullTOS1v0(int overBought, int overSold, int kPeriod, int dPeriod, StochasticFullTOS1v0_Price priceH, StochasticFullTOS1v0_Price priceL, StochasticFullTOS1v0_Price priceC, int slowingPeriod, StochasticFullTOS1v0_AverageType averageType)
		{
			return indicator.StochasticFullTOS1v0(Input, overBought, overSold, kPeriod, dPeriod, priceH, priceL, priceC, slowingPeriod, averageType);
		}

		public Indicators.StochasticFullTOS1v0 StochasticFullTOS1v0(ISeries<double> input, int overBought, int overSold, int kPeriod, int dPeriod, StochasticFullTOS1v0_Price priceH, StochasticFullTOS1v0_Price priceL, StochasticFullTOS1v0_Price priceC, int slowingPeriod, StochasticFullTOS1v0_AverageType averageType)
		{
			return indicator.StochasticFullTOS1v0(input, overBought, overSold, kPeriod, dPeriod, priceH, priceL, priceC, slowingPeriod, averageType);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.StochasticFullTOS1v0 StochasticFullTOS1v0(int overBought, int overSold, int kPeriod, int dPeriod, StochasticFullTOS1v0_Price priceH, StochasticFullTOS1v0_Price priceL, StochasticFullTOS1v0_Price priceC, int slowingPeriod, StochasticFullTOS1v0_AverageType averageType)
		{
			return indicator.StochasticFullTOS1v0(Input, overBought, overSold, kPeriod, dPeriod, priceH, priceL, priceC, slowingPeriod, averageType);
		}

		public Indicators.StochasticFullTOS1v0 StochasticFullTOS1v0(ISeries<double> input, int overBought, int overSold, int kPeriod, int dPeriod, StochasticFullTOS1v0_Price priceH, StochasticFullTOS1v0_Price priceL, StochasticFullTOS1v0_Price priceC, int slowingPeriod, StochasticFullTOS1v0_AverageType averageType)
		{
			return indicator.StochasticFullTOS1v0(input, overBought, overSold, kPeriod, dPeriod, priceH, priceL, priceC, slowingPeriod, averageType);
		}
	}
}

#endregion
