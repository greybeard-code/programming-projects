#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
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

#endregion



#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		
		private UltimateSignalsIndicator[] cacheUltimateSignalsIndicator;

		
		public UltimateSignalsIndicator UltimateSignalsIndicator()
		{
			return UltimateSignalsIndicator(Input);
		}


		
		public UltimateSignalsIndicator UltimateSignalsIndicator(ISeries<double> input)
		{
			if (cacheUltimateSignalsIndicator != null)
				for (int idx = 0; idx < cacheUltimateSignalsIndicator.Length; idx++)
					if ( cacheUltimateSignalsIndicator[idx].EqualsInput(input))
						return cacheUltimateSignalsIndicator[idx];
			return CacheIndicator<UltimateSignalsIndicator>(new UltimateSignalsIndicator(), input, ref cacheUltimateSignalsIndicator);
		}

	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		
		public Indicators.UltimateSignalsIndicator UltimateSignalsIndicator()
		{
			return indicator.UltimateSignalsIndicator(Input);
		}


		
		public Indicators.UltimateSignalsIndicator UltimateSignalsIndicator(ISeries<double> input )
		{
			return indicator.UltimateSignalsIndicator(input);
		}
	
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		
		public Indicators.UltimateSignalsIndicator UltimateSignalsIndicator()
		{
			return indicator.UltimateSignalsIndicator(Input);
		}


		
		public Indicators.UltimateSignalsIndicator UltimateSignalsIndicator(ISeries<double> input )
		{
			return indicator.UltimateSignalsIndicator(input);
		}

	}
}

#endregion
