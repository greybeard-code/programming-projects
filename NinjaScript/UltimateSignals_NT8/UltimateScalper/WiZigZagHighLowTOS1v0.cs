using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;

namespace NinjaTrader.NinjaScript.Indicators
{
	public class WiZigZagHighLowTOS1v0 : Indicator
	{
		[XmlIgnore]
		public ISeries<double> priceH;

		[XmlIgnore]
		public ISeries<double> priceL;

		private Series<double> maxPriceH;

		private Series<double> minPriceL;

		private Series<int> state;

		private Series<bool> newState;

		private Series<bool> newMax;

		private Series<bool> newMin;

		private Series<int> prevPointBar;

		private Series<double> prevPointY;

		private Series<int> lastPot;

		private Series<int> lastPointBar;

		private Series<int> lastHighPointBar;

		private Series<int> lastLowPointBar;

		private int CurrentBarOffset;

		private int lastBar;

		protected int WiCurrentBar => Math.Max(0, (((State != State.Historical || (IsTickReplays[0] ?? false)) && Calculate != Calculate.OnBarClose) ? 1 : 0) + CurrentBarOffset - 1);

		[ReadOnly(true)]
		[XmlIgnore]
		[Display(Name = "Version", Description = "Version", Order = 0, GroupName = "Parameters")]
		public string Version { get; set; }

		[Display(Name = "PriceH", Order = 1, GroupName = "Parameters")]
		[NinjaScriptProperty]
		public WiZigZagHighLowTOS1v0_PriceTypeHL PriceH { get; set; }

		[Display(Name = "PriceL", Order = 2, GroupName = "Parameters")]
		[NinjaScriptProperty]
		public WiZigZagHighLowTOS1v0_PriceTypeHL PriceL { get; set; }

		[Range(0.0, double.MaxValue)]
		[Display(Name = "PercentageReversal", Order = 3, GroupName = "Parameters")]
		[NinjaScriptProperty]
		public double percentageReversal { get; set; }

		[NinjaScriptProperty]
		[Range(0.0, double.MaxValue)]
		[Display(Name = "AbsoluteReversal", Order = 4, GroupName = "Parameters")]
		public double absoluteReversal { get; set; }

		[Display(Name = "ATRLength", Order = 5, GroupName = "Parameters")]
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		public int atrLength { get; set; }

		[Range(0.0, double.MaxValue)]
		[NinjaScriptProperty]
		[Display(Name = "ATRReversal", Order = 6, GroupName = "Parameters")]
		public double atrReversal { get; set; }

		[Range(0, int.MaxValue)]
		[Display(Name = "TickReversal", Order = 7, GroupName = "Parameters")]
		[NinjaScriptProperty]
		public int tickReversal { get; set; }

		[XmlIgnore]
		[Browsable(false)]
		public Series<double> ZZ => Values[0];

		[XmlIgnore]
		[Browsable(false)]
		public Series<double> ZZLine => Values[1];

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> ZZDot => Values[2];

		[XmlIgnore]
		[Browsable(false)]
		public Series<double> ZZPot => Values[3];

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Version = "1.00.6";
				Description = "";
				Name = "WiZigZagHighLowTOS1v0" + Version;
				Calculate = Calculate.OnEachTick;
				IsOverlay = true;
				DisplayInDataBox = true;
				DrawOnPricePanel = true;
				DrawHorizontalGridLines = true;
				DrawVerticalGridLines = true;
				PaintPriceMarkers = true;
				ScaleJustification = ScaleJustification.Right;
				IsSuspendedWhileInactive = true;
				ShowTransparentPlotsInDataBox = true;
				percentageReversal = 0.15;
				absoluteReversal = 1.0;
				atrLength = 1;
				atrReversal = 1.0;
				tickReversal = 0;
				PriceH = WiZigZagHighLowTOS1v0_PriceTypeHL.Close;
				PriceL = WiZigZagHighLowTOS1v0_PriceTypeHL.Close;
				AddPlot(Brushes.Transparent, "ZZ");
				AddPlot(Brushes.DodgerBlue, "ZZLine");
				AddPlot(Brushes.Transparent, "ZZDot");
				AddPlot(Brushes.Transparent, "ZZPot");
			}
			else if (State == State.Configure)
			{
				maxPriceH = new Series<double>(this, MaximumBarsLookBack);
				minPriceL = new Series<double>(this, MaximumBarsLookBack);
				state = new Series<int>(this, MaximumBarsLookBack);
				newState = new Series<bool>(this, MaximumBarsLookBack);
				newMax = new Series<bool>(this, MaximumBarsLookBack);
				newMin = new Series<bool>(this, MaximumBarsLookBack);
				prevPointBar = new Series<int>(this, MaximumBarsLookBack);
				prevPointY = new Series<double>(this, MaximumBarsLookBack);
				lastPot = new Series<int>(this, MaximumBarsLookBack);
				lastPointBar = new Series<int>(this, MaximumBarsLookBack);
				lastHighPointBar = new Series<int>(this, MaximumBarsLookBack);
				lastLowPointBar = new Series<int>(this, MaximumBarsLookBack);
				priceH = Type2Price(PriceH);
				priceL = Type2Price(PriceL);
			}
		}

		private ISeries<double> Type2Price(WiZigZagHighLowTOS1v0_PriceTypeHL type)
		{
			switch (type)
			{
			default:
				return null;
			case WiZigZagHighLowTOS1v0_PriceTypeHL.OHLC4:
				return Weighted;
			case WiZigZagHighLowTOS1v0_PriceTypeHL.HLC3:
				return Typical;
			case WiZigZagHighLowTOS1v0_PriceTypeHL.HL2:
				return Median;
			case WiZigZagHighLowTOS1v0_PriceTypeHL.Open:
				return Open;
			case WiZigZagHighLowTOS1v0_PriceTypeHL.Low:
				return Low;
			case WiZigZagHighLowTOS1v0_PriceTypeHL.High:
				return High;
			case WiZigZagHighLowTOS1v0_PriceTypeHL.Close:
				return Close;
			}
		}

		protected override void OnBarUpdate()
		{
			if (CurrentBar <= 1)
			{
				return;
			}
			if (IsFirstTickOfBar)
			{
				if (lastPot[1] != 0)
				{
					ResetPlot(ZZPot, CurrentBar - lastPot[1]);
					ResetPlot(ZZ, CurrentBar - lastPot[1]);
				}
				prevPointBar[0] = prevPointBar[1];
				prevPointY[0] = prevPointY[1];
				lastPot[0] = lastPot[1];
				lastPointBar[0] = lastPointBar[1];
				lastHighPointBar[0] = lastHighPointBar[1];
				lastLowPointBar[0] = lastLowPointBar[1];
			}
			else
			{
				if (lastPot[0] != 0)
				{
					ResetPlot(ZZPot, CurrentBar - lastPot[0]);
					ResetPlot(ZZ, CurrentBar - lastPot[0]);
				}
				ResetPlot(ZZPot, 0);
				ResetPlot(ZZ, 0);
				prevPointBar[0] = prevPointBar[1];
				prevPointY[0] = prevPointY[1];
				lastPot[0] = lastPot[1];
				lastPointBar[0] = lastPointBar[1];
				lastHighPointBar[0] = lastHighPointBar[1];
				lastLowPointBar[0] = lastLowPointBar[1];
			}
			lastBar = CurrentBar;
			int wiCurrentBar = WiCurrentBar;
			state[wiCurrentBar] = 0;
			Series<double> obj = maxPriceH;
			Series<double> obj2 = minPriceL;
			double num = 0.0;
			obj2[wiCurrentBar] = 0.0;
			obj[wiCurrentBar] = num;
			Series<bool> obj3 = newState;
			Series<bool> obj4 = newMax;
			newMin[wiCurrentBar] = false;
			obj4[wiCurrentBar] = false;
			obj3[wiCurrentBar] = false;
			double num2 = ((absoluteReversal != 0.0) ? absoluteReversal : ((double)tickReversal * TickSize));
			double num3 = ((atrReversal != 0.0) ? (percentageReversal / 100.0 + WilderMA1v0(ATR(1), atrLength)[wiCurrentBar] / Close[wiCurrentBar] * atrReversal) : (percentageReversal / 100.0));
			double num4 = maxPriceH[wiCurrentBar + 1];
			double num5 = minPriceL[wiCurrentBar + 1];
			if (state[wiCurrentBar + 1] == 0)
			{
				maxPriceH[wiCurrentBar] = priceH[wiCurrentBar];
				minPriceL[wiCurrentBar] = priceL[wiCurrentBar];
				newMax[wiCurrentBar] = true;
				newMin[wiCurrentBar] = true;
				state[wiCurrentBar] = -2;
			}
			else if (state[wiCurrentBar + 1] == -2)
			{
				if (priceH[wiCurrentBar] >= num4)
				{
					state[wiCurrentBar] = 1;
					maxPriceH[wiCurrentBar] = priceH[wiCurrentBar];
					minPriceL[wiCurrentBar] = num5;
					newMax[wiCurrentBar] = true;
					newMin[wiCurrentBar] = false;
				}
				else if (priceL[wiCurrentBar] <= num5)
				{
					state[wiCurrentBar] = -1;
					maxPriceH[wiCurrentBar] = num4;
					minPriceL[wiCurrentBar] = priceL[wiCurrentBar];
					newMax[wiCurrentBar] = false;
					newMin[wiCurrentBar] = true;
				}
				else
				{
					state[wiCurrentBar] = -2;
					maxPriceH[wiCurrentBar] = num4;
					minPriceL[wiCurrentBar] = num5;
					newMax[wiCurrentBar] = false;
					newMin[wiCurrentBar] = false;
				}
			}
			else if (state[wiCurrentBar + 1] == 1)
			{
				if (priceL[wiCurrentBar] <= num4 - num4 * num3 - num2)
				{
					state[wiCurrentBar] = -1;
					maxPriceH[wiCurrentBar] = num4;
					minPriceL[wiCurrentBar] = priceL[wiCurrentBar];
					newMax[wiCurrentBar] = false;
					newMin[wiCurrentBar] = true;
				}
				else
				{
					state[wiCurrentBar] = 1;
					if (priceH[wiCurrentBar] >= num4)
					{
						maxPriceH[wiCurrentBar] = priceH[wiCurrentBar];
						newMax[wiCurrentBar] = true;
					}
					else
					{
						maxPriceH[wiCurrentBar] = num4;
						newMax[wiCurrentBar] = false;
					}
					minPriceL[wiCurrentBar] = num5;
					newMin[wiCurrentBar] = false;
				}
			}
			else if (priceH[wiCurrentBar] >= num5 + num5 * num3 + num2)
			{
				state[wiCurrentBar] = 1;
				maxPriceH[wiCurrentBar] = priceH[wiCurrentBar];
				minPriceL[wiCurrentBar] = num5;
				newMax[wiCurrentBar] = true;
				newMin[wiCurrentBar] = false;
			}
			else
			{
				state[wiCurrentBar] = -1;
				maxPriceH[wiCurrentBar] = num4;
				newMax[wiCurrentBar] = false;
				if (priceL[wiCurrentBar] <= num5)
				{
					minPriceL[wiCurrentBar] = priceL[wiCurrentBar];
					newMin[wiCurrentBar] = true;
				}
				else
				{
					minPriceL[wiCurrentBar] = num5;
					newMin[wiCurrentBar] = false;
				}
			}
			newState[wiCurrentBar] = state[wiCurrentBar] != state[wiCurrentBar + 1];
			bool flag = state[wiCurrentBar] == 1 && priceH[wiCurrentBar] == maxPriceH[wiCurrentBar];
			bool flag2 = state[wiCurrentBar] == -1 && priceL[wiCurrentBar] == minPriceL[wiCurrentBar];
			double num6 = double.NaN;
			if (flag)
			{
				lastHighPointBar[0] = CurrentBar - wiCurrentBar;
			}
			if (newState[wiCurrentBar] || newMax[wiCurrentBar])
			{
				num6 = (newMax[wiCurrentBar] ? double.NaN : priceH[CurrentBar - lastHighPointBar[0]]);
			}
			_ = newState[wiCurrentBar];
			_ = newMax[wiCurrentBar];
			double num7 = double.NaN;
			if (flag2)
			{
				lastLowPointBar[0] = CurrentBar - wiCurrentBar;
			}
			if (newState[wiCurrentBar] || newMin[wiCurrentBar])
			{
				num7 = (newMin[wiCurrentBar] ? double.NaN : priceL[CurrentBar - lastLowPointBar[0]]);
			}
			_ = newState[wiCurrentBar];
			_ = newMin[wiCurrentBar];
			if (lastPointBar[0] == 0)
			{
				if (state[wiCurrentBar] == 1)
				{
					ZZDot[wiCurrentBar] = priceL[wiCurrentBar];
				}
				else
				{
					num6 = priceH[wiCurrentBar];
				}
				lastPointBar[0] = CurrentBar - wiCurrentBar;
				return;
			}
			int num8 = -1;
			if (!double.IsNaN(num6))
			{
				num8 = CurrentBar - lastHighPointBar[0];
				ZZDot[num8] = num6;
			}
			else if (!double.IsNaN(num7))
			{
				num8 = CurrentBar - lastLowPointBar[0];
				ZZDot[num8] = num7;
			}
			if (num8 >= 0)
			{
				DrawLine(CurrentBar - prevPointBar[0], prevPointY[0], num8, ZZDot[num8]);
				prevPointBar[0] = CurrentBar - num8;
				prevPointY[0] = ZZDot[num8];
			}
			if (num8 < 0)
			{
				lastPot[0] = ((lastHighPointBar[0] > lastLowPointBar[0]) ? lastHighPointBar : lastLowPointBar)[0];
				num8 = CurrentBar - lastPot[0];
			}
			if (!flag && (state[wiCurrentBar] != -1 || priceL[wiCurrentBar] <= minPriceL[wiCurrentBar]))
			{
				if (flag2 || (state[wiCurrentBar] == 1 && priceH[wiCurrentBar] < maxPriceH[wiCurrentBar]))
				{
					ZZPot[num8] = priceH[num8];
				}
			}
			else
			{
				ZZPot[num8] = priceL[num8];
			}
			double endY = ((lastHighPointBar[0] > lastLowPointBar[0]) ? priceH[0] : priceL[0]);
			DrawLine(CurrentBar - prevPointBar[0], prevPointY[0], num8, ZZPot[num8]);
			DrawLine(num8, ZZPot[num8], 0, endY);
		}

		private void ResetPlot(Series<double> series, int index)
		{
			series[index] = 0.0;
			series.Reset(index);
		}

		private void DrawLine(int startBarsAgo, double startY, int endBarsAgo, double endY)
		{
			int num = Math.Abs(endBarsAgo - startBarsAgo);
			double num2 = ((num != 0) ? ((0.0 - endY + startY) / (double)num) : 0.0);
			ZZ[startBarsAgo] = startY;
			for (int i = 0; i <= num; i++)
			{
				int num3 = Math.Min(CurrentBar, endBarsAgo + i);
				ZZLine[num3] = endY + num2 * (double)i;
			}
		}
	}
}


#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private WiZigZagHighLowTOS1v0[] cacheWiZigZagHighLowTOS1v0;
		public WiZigZagHighLowTOS1v0 WiZigZagHighLowTOS1v0(WiZigZagHighLowTOS1v0_PriceTypeHL priceH, WiZigZagHighLowTOS1v0_PriceTypeHL priceL, double percentageReversal, double absoluteReversal, int atrLength, double atrReversal, int tickReversal)
		{
			return WiZigZagHighLowTOS1v0(Input, priceH, priceL, percentageReversal, absoluteReversal, atrLength, atrReversal, tickReversal);
		}

		public WiZigZagHighLowTOS1v0 WiZigZagHighLowTOS1v0(ISeries<double> input, WiZigZagHighLowTOS1v0_PriceTypeHL priceH, WiZigZagHighLowTOS1v0_PriceTypeHL priceL, double percentageReversal, double absoluteReversal, int atrLength, double atrReversal, int tickReversal)
		{
			if (cacheWiZigZagHighLowTOS1v0 != null)
				for (int idx = 0; idx < cacheWiZigZagHighLowTOS1v0.Length; idx++)
					if (cacheWiZigZagHighLowTOS1v0[idx] != null && cacheWiZigZagHighLowTOS1v0[idx].PriceH == priceH && cacheWiZigZagHighLowTOS1v0[idx].PriceL == priceL && cacheWiZigZagHighLowTOS1v0[idx].percentageReversal == percentageReversal && cacheWiZigZagHighLowTOS1v0[idx].absoluteReversal == absoluteReversal && cacheWiZigZagHighLowTOS1v0[idx].atrLength == atrLength && cacheWiZigZagHighLowTOS1v0[idx].atrReversal == atrReversal && cacheWiZigZagHighLowTOS1v0[idx].tickReversal == tickReversal && cacheWiZigZagHighLowTOS1v0[idx].EqualsInput(input))
						return cacheWiZigZagHighLowTOS1v0[idx];
			return CacheIndicator<WiZigZagHighLowTOS1v0>(new WiZigZagHighLowTOS1v0(){ PriceH = priceH, PriceL = priceL, percentageReversal = percentageReversal, absoluteReversal = absoluteReversal, atrLength = atrLength, atrReversal = atrReversal, tickReversal = tickReversal }, input, ref cacheWiZigZagHighLowTOS1v0);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.WiZigZagHighLowTOS1v0 WiZigZagHighLowTOS1v0(WiZigZagHighLowTOS1v0_PriceTypeHL priceH, WiZigZagHighLowTOS1v0_PriceTypeHL priceL, double percentageReversal, double absoluteReversal, int atrLength, double atrReversal, int tickReversal)
		{
			return indicator.WiZigZagHighLowTOS1v0(Input, priceH, priceL, percentageReversal, absoluteReversal, atrLength, atrReversal, tickReversal);
		}

		public Indicators.WiZigZagHighLowTOS1v0 WiZigZagHighLowTOS1v0(ISeries<double> input, WiZigZagHighLowTOS1v0_PriceTypeHL priceH, WiZigZagHighLowTOS1v0_PriceTypeHL priceL, double percentageReversal, double absoluteReversal, int atrLength, double atrReversal, int tickReversal)
		{
			return indicator.WiZigZagHighLowTOS1v0(input, priceH, priceL, percentageReversal, absoluteReversal, atrLength, atrReversal, tickReversal);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.WiZigZagHighLowTOS1v0 WiZigZagHighLowTOS1v0(WiZigZagHighLowTOS1v0_PriceTypeHL priceH, WiZigZagHighLowTOS1v0_PriceTypeHL priceL, double percentageReversal, double absoluteReversal, int atrLength, double atrReversal, int tickReversal)
		{
			return indicator.WiZigZagHighLowTOS1v0(Input, priceH, priceL, percentageReversal, absoluteReversal, atrLength, atrReversal, tickReversal);
		}

		public Indicators.WiZigZagHighLowTOS1v0 WiZigZagHighLowTOS1v0(ISeries<double> input, WiZigZagHighLowTOS1v0_PriceTypeHL priceH, WiZigZagHighLowTOS1v0_PriceTypeHL priceL, double percentageReversal, double absoluteReversal, int atrLength, double atrReversal, int tickReversal)
		{
			return indicator.WiZigZagHighLowTOS1v0(input, priceH, priceL, percentageReversal, absoluteReversal, atrLength, atrReversal, tickReversal);
		}
	}
}

#endregion
