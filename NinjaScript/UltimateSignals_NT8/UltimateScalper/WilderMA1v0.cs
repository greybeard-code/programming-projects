// WilderMA1v0.cs - companion for UltimateAIProV3

using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;

namespace NinjaTrader.NinjaScript.Indicators
{
	public class WilderMA1v0 : Indicator
	{
		private double alpha;

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "Period", Order = 1, GroupName = "Parameters")]
		public int Period { get; set; }

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description = "Change color of Moving Average.";
				Name = "WilderMA1v0";
				Calculate = Calculate.OnBarClose;
				IsOverlay = true;
				DisplayInDataBox = true;
				DrawOnPricePanel = true;
				DrawHorizontalGridLines = true;
				DrawVerticalGridLines = true;
				PaintPriceMarkers = true;
				ScaleJustification = ScaleJustification.Right;
				IsSuspendedWhileInactive = true;
				Period = 14;
			}
			else if (State == State.Configure)
			{
				alpha = 1.0 / (double)Period;
				AddPlot(Brushes.Orange, "WilderMA1v0");
			}
		}

		protected override void OnBarUpdate()
		{
			Value[0] = ((CurrentBar == 0) ? Input[0] : (alpha * Input[0] + (1.0 - alpha) * Value[1]));
		}
	}
}


#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private WilderMA1v0[] cacheWilderMA1v0;
		public WilderMA1v0 WilderMA1v0(int period)
		{
			return WilderMA1v0(Input, period);
		}

		public WilderMA1v0 WilderMA1v0(ISeries<double> input, int period)
		{
			if (cacheWilderMA1v0 != null)
				for (int idx = 0; idx < cacheWilderMA1v0.Length; idx++)
					if (cacheWilderMA1v0[idx] != null && cacheWilderMA1v0[idx].Period == period && cacheWilderMA1v0[idx].EqualsInput(input))
						return cacheWilderMA1v0[idx];
			return CacheIndicator<WilderMA1v0>(new WilderMA1v0(){ Period = period }, input, ref cacheWilderMA1v0);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.WilderMA1v0 WilderMA1v0(int period)
		{
			return indicator.WilderMA1v0(Input, period);
		}

		public Indicators.WilderMA1v0 WilderMA1v0(ISeries<double> input, int period)
		{
			return indicator.WilderMA1v0(input, period);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.WilderMA1v0 WilderMA1v0(int period)
		{
			return indicator.WilderMA1v0(Input, period);
		}

		public Indicators.WilderMA1v0 WilderMA1v0(ISeries<double> input, int period)
		{
			return indicator.WilderMA1v0(input, period);
		}
	}
}

#endregion
