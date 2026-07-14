#region Using declarations
using NinjaTrader.NinjaScript;
#endregion

// UltimateScalperSuite2.dll ships UltimateAI2 as a pre-compiled indicator (no source in
// bin\Custom\Indicators), so NinjaTrader's NinjaScript generator never created the usual
// "#region NinjaScript generated code" call-syntax block for it. This file hand-writes that
// missing block, following the exact pattern NinjaTrader itself generates for source-based
// indicators (verified against UltimateMA's generated code and compiled IL).
#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private UltimateAI2[] cacheUltimateAI2;
		public UltimateAI2 UltimateAI2(int pmBuffer_upSignal_Offset, int pmBuffer_dnSignal_Offset, int pmBuffer_long_Offset, int pmBuffer_short_Offset, bool pmAlerts_upSignal_Enable, string pmAlerts_upSignal_File, bool pmAlerts_dnSignal_Enable, string pmAlerts_dnSignal_File, bool pmAlerts_long_Enable, string pmAlerts_long_File, bool pmAlerts_short_Enable, string pmAlerts_short_File)
		{
			return UltimateAI2(Input, pmBuffer_upSignal_Offset, pmBuffer_dnSignal_Offset, pmBuffer_long_Offset, pmBuffer_short_Offset, pmAlerts_upSignal_Enable, pmAlerts_upSignal_File, pmAlerts_dnSignal_Enable, pmAlerts_dnSignal_File, pmAlerts_long_Enable, pmAlerts_long_File, pmAlerts_short_Enable, pmAlerts_short_File);
		}

		public UltimateAI2 UltimateAI2(ISeries<double> input, int pmBuffer_upSignal_Offset, int pmBuffer_dnSignal_Offset, int pmBuffer_long_Offset, int pmBuffer_short_Offset, bool pmAlerts_upSignal_Enable, string pmAlerts_upSignal_File, bool pmAlerts_dnSignal_Enable, string pmAlerts_dnSignal_File, bool pmAlerts_long_Enable, string pmAlerts_long_File, bool pmAlerts_short_Enable, string pmAlerts_short_File)
		{
			if (cacheUltimateAI2 != null)
				for (int idx = 0; idx < cacheUltimateAI2.Length; idx++)
					if (cacheUltimateAI2[idx] != null
						&& cacheUltimateAI2[idx].pmBuffer_upSignal_Offset == pmBuffer_upSignal_Offset
						&& cacheUltimateAI2[idx].pmBuffer_dnSignal_Offset == pmBuffer_dnSignal_Offset
						&& cacheUltimateAI2[idx].pmBuffer_long_Offset == pmBuffer_long_Offset
						&& cacheUltimateAI2[idx].pmBuffer_short_Offset == pmBuffer_short_Offset
						&& cacheUltimateAI2[idx].pmAlerts_upSignal_Enable == pmAlerts_upSignal_Enable
						&& cacheUltimateAI2[idx].pmAlerts_upSignal_File == pmAlerts_upSignal_File
						&& cacheUltimateAI2[idx].pmAlerts_dnSignal_Enable == pmAlerts_dnSignal_Enable
						&& cacheUltimateAI2[idx].pmAlerts_dnSignal_File == pmAlerts_dnSignal_File
						&& cacheUltimateAI2[idx].pmAlerts_long_Enable == pmAlerts_long_Enable
						&& cacheUltimateAI2[idx].pmAlerts_long_File == pmAlerts_long_File
						&& cacheUltimateAI2[idx].pmAlerts_short_Enable == pmAlerts_short_Enable
						&& cacheUltimateAI2[idx].pmAlerts_short_File == pmAlerts_short_File
						&& cacheUltimateAI2[idx].EqualsInput(input))
						return cacheUltimateAI2[idx];
			return CacheIndicator<UltimateAI2>(new UltimateAI2()
			{
				pmBuffer_upSignal_Offset = pmBuffer_upSignal_Offset,
				pmBuffer_dnSignal_Offset = pmBuffer_dnSignal_Offset,
				pmBuffer_long_Offset = pmBuffer_long_Offset,
				pmBuffer_short_Offset = pmBuffer_short_Offset,
				pmAlerts_upSignal_Enable = pmAlerts_upSignal_Enable,
				pmAlerts_upSignal_File = pmAlerts_upSignal_File,
				pmAlerts_dnSignal_Enable = pmAlerts_dnSignal_Enable,
				pmAlerts_dnSignal_File = pmAlerts_dnSignal_File,
				pmAlerts_long_Enable = pmAlerts_long_Enable,
				pmAlerts_long_File = pmAlerts_long_File,
				pmAlerts_short_Enable = pmAlerts_short_Enable,
				pmAlerts_short_File = pmAlerts_short_File,
			}, input, ref cacheUltimateAI2);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.UltimateAI2 UltimateAI2(int pmBuffer_upSignal_Offset, int pmBuffer_dnSignal_Offset, int pmBuffer_long_Offset, int pmBuffer_short_Offset, bool pmAlerts_upSignal_Enable, string pmAlerts_upSignal_File, bool pmAlerts_dnSignal_Enable, string pmAlerts_dnSignal_File, bool pmAlerts_long_Enable, string pmAlerts_long_File, bool pmAlerts_short_Enable, string pmAlerts_short_File)
		{
			return indicator.UltimateAI2(Input, pmBuffer_upSignal_Offset, pmBuffer_dnSignal_Offset, pmBuffer_long_Offset, pmBuffer_short_Offset, pmAlerts_upSignal_Enable, pmAlerts_upSignal_File, pmAlerts_dnSignal_Enable, pmAlerts_dnSignal_File, pmAlerts_long_Enable, pmAlerts_long_File, pmAlerts_short_Enable, pmAlerts_short_File);
		}

		public Indicators.UltimateAI2 UltimateAI2(ISeries<double> input, int pmBuffer_upSignal_Offset, int pmBuffer_dnSignal_Offset, int pmBuffer_long_Offset, int pmBuffer_short_Offset, bool pmAlerts_upSignal_Enable, string pmAlerts_upSignal_File, bool pmAlerts_dnSignal_Enable, string pmAlerts_dnSignal_File, bool pmAlerts_long_Enable, string pmAlerts_long_File, bool pmAlerts_short_Enable, string pmAlerts_short_File)
		{
			return indicator.UltimateAI2(input, pmBuffer_upSignal_Offset, pmBuffer_dnSignal_Offset, pmBuffer_long_Offset, pmBuffer_short_Offset, pmAlerts_upSignal_Enable, pmAlerts_upSignal_File, pmAlerts_dnSignal_Enable, pmAlerts_dnSignal_File, pmAlerts_long_Enable, pmAlerts_long_File, pmAlerts_short_Enable, pmAlerts_short_File);
		}
	}
}

#endregion
