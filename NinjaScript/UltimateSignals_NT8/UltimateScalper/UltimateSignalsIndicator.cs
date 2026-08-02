using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Core;
using NinjaTrader.Custom;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.NinjaScript;
using NinjaTrader.NinjaScript.DrawingTools;
using SharpDX;
using SharpDX.Direct2D1;
using UltimateSignalsNamespace;
namespace NinjaTrader.NinjaScript.Indicators
{
	public class UltimateSignalsIndicator : Indicator
	{
		private int trendLength;
		private int sequentialLength;
		private bool useAlerts;
		private int fastLength;
		private int slowLength;
		private int MACDLength;
		private MaMode averageType;
		private double overbought;
		private double oversold;
		private int KPeriod;
		private int DPeriod;
		private PriceMode priceH;
		private PriceMode priceL;
		private PriceMode priceC;
		private MaMode avgType;
		private Method method;
		private TosStochastics stochastic;
		private int minBars;
		private WaitForNextBar waitForNextBarAlertUp;
		private WaitForNextBar waitForNextBarAlertDn;
		private IndicatorEngine fastMa;
		private IndicatorEngine slowMa;
		private const int superfast_length = 9;
		private const int fast_length = 14;
		private const int slow_length = 21;
		private const double bubbleoffset = 0.0005;
		private const double percentamount = 0.01;
		private const double revAmount = 0.05;
		private const double atrreversal = 2.0;
		private const int atrlength = 5;
		private const int averagelength = 5;
		private const bool showarrows = true;
		private UltimateSignalsNamespace.EMA mov_avg9;
		private UltimateSignalsNamespace.EMA mov_avg14;
		private UltimateSignalsNamespace.EMA mov_avg21;
		private Series<bool> buy;
		private Series<bool> buysignal;
		private Series<bool> sell;
		private Series<bool> sellsignal;
		private Series<int> Colorbars;
		private UltimateSignalsNamespace.EMA mah;
		private UltimateSignalsNamespace.EMA mal;
		private ISeries<double> priceh;
		private ISeries<double> pricel;
		private TosZigZagHighLow EI;
		private Series<double> EISave;
		private Series<double> EIL;
		private Series<double> EIH;
		private Series<int> dir;
		private Series<int> signal;
		private Series<double> revLineTop;
		private Series<double> revLineBot;
		private System.Windows.Media.Brush UPTICKBrush;
		private System.Windows.Media.Brush DOWNTICKBrush;
		private Series<double> macd;
		private Series<bool> macdup;
		private Series<bool> macddown;
		private TosArrowDraw renderArrowUpSignal;
		private TosArrowDraw renderArrowDownSignal;
		private TosArrowDraw renderArrowLong;
		private TosArrowDraw renderArrowShort;
		private TosArrowDraw renderArrowBuy;
		private TosArrowDraw renderArrowSell;
		private LineDraw drawBotLine;
		private LineDraw drawTopLine;
		private const int XPlotBotLine = 5;
		private const int XPlotTopLine = 6;
		[XmlIgnore]
		[Browsable(false)]
		public Series<double> upSignalPlot
		{
			get
			{
				return Values[0];
			}
		}
		[XmlIgnore]
		[Browsable(false)]
		public Series<double> dnSignalPlot
		{
			get
			{
				return Values[1];
			}
		}
		[Browsable(false)]
		[XmlIgnore]
		public Series<double> EnhancedLines
		{
			get
			{
				return Values[2];
			}
		}
		[Browsable(false)]
		[XmlIgnore]
		public Series<double> long_
		{
			get
			{
				return Values[3];
			}
		}
		[XmlIgnore]
		[Browsable(false)]
		public Series<double> short_
		{
			get
			{
				return Values[4];
			}
		}
		[XmlIgnore]
		[Browsable(false)]
		public Series<double> botLine
		{
			get
			{
				return Values[5];
			}
		}
		[Browsable(false)]
		[XmlIgnore]
		public Series<double> topLine
		{
			get
			{
				return Values[6];
			}
		}
		[XmlIgnore]
		[Browsable(false)]
		public Series<double> buyTextMarker
		{
			get
			{
				return Values[7];
			}
		}
		[XmlIgnore]
		[Browsable(false)]
		public Series<double> sellTextMarker
		{
			get
			{
				return Values[8];
			}
		}
		public UltimateSignalsIndicator()
		{
			C();
		}
		private void C()
		{
		}
		#region Lifecycle
		protected override void OnStateChange()
		{
			OSC();
		}
		private void OSC()
		{
			if (EI != null)
			{
				EI.SetState(State);
			}
			if (mov_avg9 != null)
			{
				mov_avg9.SetState(State);
			}
			if (mov_avg14 != null)
			{
				mov_avg14.SetState(State);
			}
			if (mov_avg21 != null)
			{
				mov_avg21.SetState(State);
			}
			if (mah != null)
			{
				mah.SetState(State);
			}
			if (mal != null)
			{
				mal.SetState(State);
			}
			if (stochastic != null)
			{
				stochastic.SetState(State);
			}
			if (fastMa != null)
			{
				fastMa.SetState(State);
			}
			if (slowMa != null)
			{
				slowMa.SetState(State);
			}
			if (State == State.SetDefaults)
			{
				Name = "Ultimate Signals";
				Description = Name;
				IsOverlay = true;
				IsSuspendedWhileInactive = false;
				Calculate = Calculate.OnEachTick;
				IsAutoScale = true;
				trendLength = 5;
				sequentialLength = 3;
				useAlerts = false;
				fastLength = 5;
				slowLength = 26;
				MACDLength = 9;
				averageType = MaMode.EMA;
				overbought = 80.0;
				oversold = 20.0;
				KPeriod = 10;
				DPeriod = 10;
				priceH = PriceMode.High;
				priceL = PriceMode.Low;
				priceC = PriceMode.Close;
				avgType = MaMode.SMA;
				method = Method.average;
				AddPlot(new Stroke(Brushes.Lime, DashStyleHelper.Solid, 3f), PlotStyle.TriangleUp, "Up");
				AddPlot(new Stroke(Brushes.Red, DashStyleHelper.Solid, 3f), PlotStyle.Dot, "Down");
				AddPlot(new Stroke(Brushes.Transparent, DashStyleHelper.Solid, 2f), PlotStyle.Hash, "EnhancedLines");
				AddPlot(new Stroke(Brushes.Lime, DashStyleHelper.Solid, 5f), PlotStyle.TriangleUp, "Long");
				AddPlot(new Stroke(Brushes.Red, DashStyleHelper.Solid, 5f), PlotStyle.Dot, "Short");
				AddPlot(Brushes.LightGreen, "botLine");
				AddPlot(Brushes.Red, "topLine");
				AddPlot(new Stroke(Brushes.Magenta, DashStyleHelper.Solid, 5f), PlotStyle.TriangleUp, "Buy Text Marker");
				AddPlot(new Stroke(Brushes.Magenta, DashStyleHelper.Solid, 5f), PlotStyle.Dot, "Sell Text Marker");
			}
			else if (State == State.Configure)
			{
				MaximumBarsLookBack = MaximumBarsLookBack.Infinite;
			}
			else if (State == State.DataLoaded)
			{
				stochastic = new TosStochastics(this);
				stochastic.overbought = overbought;
				stochastic.oversold = oversold;
				stochastic.PeriodK = KPeriod;
				stochastic.PeriodD = DPeriod;
				stochastic.Smooth = 3;
				stochastic.priceHSelect = priceH;
				stochastic.priceLSelect = priceL;
				stochastic.priceCSelect = priceC;
				stochastic.avgType = avgType;
				stochastic.SetState(State.Configure);
				stochastic.SetState(State);
				macd = new Series<double>(this);
				macdup = new Series<bool>(this);
				macddown = new Series<bool>(this);
				fastMa = MovingAverageFactory.Create(this, averageType, Close, fastLength);
				slowMa = MovingAverageFactory.Create(this, averageType, Close, slowLength);
				minBars = Math.Max(sequentialLength + 1, trendLength + 1);
				waitForNextBarAlertUp = new WaitForNextBar(this);
				waitForNextBarAlertDn = new WaitForNextBar(this);
				initializeIndicator2();
			}
			else if (State == State.Historical)
			{
				stateHistoricalIndicator2();
			}
		}
		#endregion
		#region BarUpdate
		protected override void OnBarUpdate()
		{
			mov_avg9.OnBarUpdate();
			mov_avg14.OnBarUpdate();
			mov_avg21.OnBarUpdate();
			mah.OnBarUpdate();
			mal.OnBarUpdate();
			EI.OnBarUpdate();
			stochastic.OnBarUpdate();
			fastMa.OnBarUpdate();
			slowMa.OnBarUpdate();
			OnBarUpdateIndicator1();
			OnBarUpdateIndicator2();
		}
		private void OnBarUpdateIndicator1()
		{
			if (CurrentBar < minBars)
			{
				return;
			}
			macd[0] = fastMa[0] - slowMa[0];
			int num = 0;
			int num2 = 0;
			for (int i = 0; i < sequentialLength; i++)
			{
				if (macd[i] >= macd[i + 1])
				{
					num++;
				}
				if (macd[i] < macd[i + 1])
				{
					num2++;
				}
			}
			macdup[0] = num == sequentialLength && macd[0] >= macd[trendLength];
			macddown[0] = num2 == sequentialLength && macd[0] < macd[trendLength];
			bool flag = macddown[1] && macd[0] > macd[1] && stochastic.K[0] < overbought;
			bool flag2 = macdup[1] && macd[0] < macd[1] && stochastic.K[0] > oversold;
			if (flag)
			{
				upSignalPlot[0] = Low[0] - TickSize;
			}
			else
			{
				upSignalPlot.Reset(0);
			}
			if (flag2)
			{
				dnSignalPlot[0] = High[0] + TickSize;
			}
			else
			{
				dnSignalPlot.Reset(0);
			}
			if (useAlerts && flag && waitForNextBarAlertUp.isNewBar)
			{
				Alert("Up", Priority.Medium, "Up", Globals.InstallDir + "\\sounds\\Alert1.wav", 1, Brushes.Black, Brushes.Yellow);
				waitForNextBarAlertUp.activate();
			}
			if (useAlerts && flag2 && waitForNextBarAlertDn.isNewBar)
			{
				Alert("Dn", Priority.Medium, "Dn", Globals.InstallDir + "\\sounds\\Alert1.wav", 1, Brushes.Black, Brushes.LightBlue);
				waitForNextBarAlertDn.activate();
			}
		}
		private void initializeIndicator2()
		{
			//IL_0185: Unknown result type (might be due to invalid IL or missing references)
			mov_avg9 = UltimateSignalsNamespace.EMA.create(this, Close, 9);
			mov_avg14 = UltimateSignalsNamespace.EMA.create(this, Close, 14);
			mov_avg21 = UltimateSignalsNamespace.EMA.create(this, Close, 21);
			buy = new Series<bool>(this);
			sell = new Series<bool>(this);
			buysignal = new Series<bool>(this);
			sellsignal = new Series<bool>(this);
			Colorbars = new Series<int>(this);
			mah = UltimateSignalsNamespace.EMA.create(this, High, 5);
			mal = UltimateSignalsNamespace.EMA.create(this, Low, 5);
			ISeries<double> obj;
			if (method != Method.high_low)
			{
				ISeries<double> val = mah.Values[0];
				obj = val;
			}
			else
			{
				obj = High;
			}
			priceh = obj;
			ISeries<double> obj2;
			if (method != Method.high_low)
			{
				ISeries<double> val = mal.Values[0];
				obj2 = val;
			}
			else
			{
				obj2 = Low;
			}
			pricel = obj2;
			EI = new TosZigZagHighLow(this);
			EI.PPriceH = PriceMode.High;
			EI.PPriceL = PriceMode.Low;
			EI.PPercentageReversal = 0.01;
			EI.PAbsoluteReversal = 0.05;
			EI.PAtrLength = 5;
			EI.PAtrReversal = 2.0;
			EI.PTickReversal = 0.0;
			EI.SetState((State)2);
			EI.SetState(State);
			EISave = new Series<double>(this);
			EIL = new Series<double>(this);
			EIH = new Series<double>(this);
			dir = new Series<int>(this);
			signal = new Series<int>(this);
			revLineTop = new Series<double>(this);
			revLineBot = new Series<double>(this);
			UPTICKBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(byte.MaxValue, 3, 128, 0));
			DOWNTICKBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(byte.MaxValue, 204, 0, 0));
		}
		private void stateHistoricalIndicator2()
		{
			EI.highSeries = priceh;
			EI.lowSeries = pricel;
		}
		private void OnBarUpdateIndicator2()
		{
			if (CurrentBar < 1)
			{
				buysignal[0] = false;
				dir[0] = 0;
				signal[0] = 0;
				return;
			}
			buy[0] = mov_avg9[0] > mov_avg14[0] && mov_avg14[0] > mov_avg21[0] && Low[0] > mov_avg9[0];
			bool flag = mov_avg9[0] <= mov_avg14[0];
			bool flag2 = !buy[1] && buy[0];
			buysignal[0] = (flag2 && !flag) || (!(buysignal[1] && flag) && buysignal[1]);
			sell[0] = mov_avg9[0] < mov_avg14[0] && mov_avg14[0] < mov_avg21[0] && High[0] < mov_avg9[0];
			bool flag3 = mov_avg9[0] >= mov_avg14[0];
			bool flag4 = !sell[1] && sell[0];
			if (CurrentBar < 1)
			{
				sellsignal[0] = false;
			}
			else
			{
				sellsignal[0] = (flag4 && !flag3) || (!(sellsignal[1] && flag3) && sellsignal[1]);
			}
			Colorbars[0] = (buysignal[0] ? 1 : (sellsignal[0] ? 2 : ((!buysignal[0] || !sellsignal[0]) ? 3 : 0)));
			for (int i = EI.xLastChangedBar; i <= CurrentBar; i++)
			{
				calculateBarIndicator2(i);
			}
		}
		private void calculateBarIndicator2(int xBar)
		{
			if (xBar >= 1)
			{
				int num = CurrentBar - xBar;
				EISave[num] = (EI.IsValidDataPoint(num) ? EI[num] : EISave[num + 1]);
				bool flag = ((EISave.GetValueAt(xBar) == priceh.GetValueAt(xBar)) ? priceh[num] : pricel[num]) - EISave[num + 1] >= 0.0;
				if (EI.IsValidDataPoint(num))
				{
					EnhancedLines[num] = EI.Values[0].GetValueAt(xBar);
				}
				else
				{
					EnhancedLines.Reset(num);
				}
				EIL[num] = ((!EI.IsValidDataPoint(num) || flag) ? EIL[num + 1] : pricel[num]);
				EIH[num] = ((EI.IsValidDataPoint(num) && flag) ? priceh[num] : EIH[num + 1]);
				dir[num] = ((EIL[num] != EIL[num + 1] || (pricel[num] == EIL[num + 1] && pricel[num] == EISave[num])) ? 1 : ((EIH[num] != EIH[num + 1] || (priceh[num] == EIH[num + 1] && priceh[num] == EISave[num])) ? (-1) : dir[num + 1]));
				signal[num] = ((dir[num] > 0 && !(pricel[num] <= EIL[num])) ? ((signal[num + 1] <= 0) ? 1 : signal[num + 1]) : ((dir[num] >= 0 || priceh[num] >= EIH[num]) ? signal[num + 1] : ((signal[num + 1] >= 0) ? (-1) : signal[num + 1])));
				bool flag2 = signal[num] > 0 && signal[num + 1] <= 0;
				bool flag3 = signal[num] < 0 && signal[num + 1] >= 0;
				if (flag2 && Colorbars[num] != 3)
				{
					long_[num] = Low[num];
				}
				else
				{
					long_.Reset(num);
				}
				if (flag3 && Colorbars[num] != 3)
				{
					short_[num] = High[num];
				}
				else
				{
					short_.Reset(num);
				}
				if (flag3)
				{
					revLineBot.Reset(num);
					revLineTop[num] = High[num + 1];
				}
				else if (flag2)
				{
					revLineTop.Reset(num);
					revLineBot[num] = Low[num + 1];
				}
				else if (revLineBot.IsValidDataPoint(num + 1) && (Colorbars[num + 2] == 2 || Colorbars[num + 1] == 2))
				{
					revLineBot[num] = revLineBot[num + 1];
					revLineTop.Reset(num);
				}
				else if (revLineTop.IsValidDataPoint(num + 1) && (Colorbars[num + 2] == 1 || Colorbars[num + 1] == 1))
				{
					revLineTop[num] = revLineTop[num + 1];
					revLineBot.Reset(num);
				}
				else
				{
					revLineTop.Reset(num);
					revLineBot.Reset(num);
				}
				if (revLineBot.IsValidDataPoint(num))
				{
					botLine[num] = revLineBot[num];
				}
				else
				{
					botLine.Reset(num);
				}
				if (revLineTop.IsValidDataPoint(num))
				{
					topLine[num] = revLineTop[num];
				}
				else
				{
					topLine.Reset(num);
				}
				double location = ((!flag) ? High[num] : Low[num]);
				System.Windows.Media.Brush color = ((Colorbars[num] != 3) ? UPTICKBrush : Brushes.White);
				if (flag2 && Colorbars[num] == 3 && signal[num] > 0 && signal[num + 1] <= 0)
				{
					buyTextMarker[num] = Low[num] - TickSize;
					addText("BUY", num, location, color, -1);
				}
				else
				{
					buyTextMarker.Reset(num);
					removeText("BUY", num);
				}
				location = ((!flag) ? High[num] : Low[num]);
				color = ((Colorbars[num] != 3) ? DOWNTICKBrush : Brushes.White);
				if (flag3 && Colorbars[num] == 3 && signal[num] < 0 && signal[num + 1] >= 0)
				{
					sellTextMarker[num] = High[num] + TickSize;
					addText("SELL", num, location, color, 1);
				}
				else
				{
					sellTextMarker.Reset(num);
					removeText("SELL", num);
				}
			}
		}
		private void addText(string text, int barAgo, double location, System.Windows.Media.Brush color, int pixelOffsetDir)
		{
			Draw.Text(this, textTag(text, barAgo), text, barAgo, location, color).YPixelOffset = 50 * pixelOffsetDir;
		}
		private void removeText(string text, int barAgo)
		{
			RemoveDrawObject(textTag(text, barAgo));
		}
		private string textTag(string text, int barAgo)
		{
			return text + "_" + (CurrentBar - barAgo);
		}
		public override void OnRenderTargetChanged()
		{
			renderArrowUpSignal = new TosArrowDraw(upSignalPlot, 1, Plots[0].Width * 5f, Plots[0].Brush);
			renderArrowDownSignal = new TosArrowDraw(dnSignalPlot, -1, Plots[1].Width * 5f, Plots[1].Brush);
			renderArrowLong = new TosArrowDraw(long_, 1, Plots[3].Width * 5f, Plots[3].Brush);
			renderArrowShort = new TosArrowDraw(short_, -1, Plots[4].Width * 5f, Plots[4].Brush);
			renderArrowBuy = new TosArrowDraw(buyTextMarker, 1, Plots[7].Width * 5f, Plots[7].Brush);
			renderArrowSell = new TosArrowDraw(sellTextMarker, -1, Plots[8].Width * 5f, Plots[8].Brush);
			drawTopLine = new LineDraw(topLine, Plots[6].Width, Plots[6].Brush);
			drawBotLine = new LineDraw(botLine, Plots[5].Width, Plots[5].Brush);
			renderArrowBuy.onRenderTargetChange(RenderTarget);
			renderArrowSell.onRenderTargetChange(RenderTarget);
			renderArrowUpSignal.onRenderTargetChange(RenderTarget);
			renderArrowDownSignal.onRenderTargetChange(RenderTarget);
			renderArrowLong.onRenderTargetChange(RenderTarget);
			renderArrowShort.onRenderTargetChange(RenderTarget);
			drawTopLine.onRenderTargetChange(RenderTarget);
			drawBotLine.onRenderTargetChange(RenderTarget);
		}
		#endregion
		#region OnRender
		protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
		{
			if (RenderTarget != null && ChartBars != null)
			{
				renderArrowUpSignal.render(RenderTarget, chartControl, chartScale, ChartBars);
				renderArrowDownSignal.render(RenderTarget, chartControl, chartScale, ChartBars);
				renderArrowLong.render(RenderTarget, chartControl, chartScale, ChartBars);
				renderArrowShort.render(RenderTarget, chartControl, chartScale, ChartBars);
				renderArrowBuy.render(RenderTarget, chartControl, chartScale, ChartBars);
				renderArrowSell.render(RenderTarget, chartControl, chartScale, ChartBars);
				drawTopLine.render(RenderTarget, chartControl, chartScale, ChartBars);
				drawBotLine.render(RenderTarget, chartControl, chartScale, ChartBars);
			}
		}
		protected void OnRenderZigzag(ChartControl chartControl, ChartScale chartScale)
		{
			int num = 1;
			int num2 = ChartBars.FromIndex - 1;
			while (num2 >= 0 && num2 - Displacement >= EI.startIndex && num2 - Displacement <= Bars.Count - 1 - ((Calculate == Calculate.OnBarClose) ? 1 : 0) && !EI.Values[0].IsValidDataPointAt(num2 - Displacement))
			{
				num++;
				num2--;
			}
			num -= ((Displacement < 0) ? Displacement : (-Displacement));
			int num3 = 0;
			for (int i = ChartBars.ToIndex; i <= Value.Count && i - Displacement >= EI.startIndex && i - Displacement <= Bars.Count - 1 - ((Calculate == Calculate.OnBarClose) ? 1 : 0) && !EI.Values[0].IsValidDataPointAt(i - Displacement); i++)
			{
				num3++;
			}
			num3 += ((Displacement < 0) ? (-Displacement) : Displacement);
			int num4 = -1;
			double num5 = -1.0;
			SharpDX.Direct2D1.PathGeometry val = null;
			GeometrySink val2 = null;
			for (int j = ChartBars.FromIndex - num; j <= ChartBars.ToIndex + num3; j++)
			{
				if (j < EI.startIndex || j > Bars.Count - ((Calculate != Calculate.OnBarClose) ? 1 : 2) || j < Math.Max(BarsRequiredToPlot - Displacement, Displacement) || !EI.Values[0].IsValidDataPointAt(j))
				{
					continue;
				}
				double valueAt = EI.Values[0].GetValueAt(j);
				if (num4 >= EI.startIndex)
				{
					float num6 = (((int)chartControl.BarSpacingType == 3 || ((int)chartControl.BarSpacingType == 1 && j + Displacement >= ChartBars.Count)) ? chartControl.GetXByTime(ChartBars.GetTimeByBarIdx(chartControl, j + Displacement)) : chartControl.GetXByBarIndex(ChartBars, j + Displacement));
					float num7 = chartScale.GetYByValue(valueAt);
					if (val2 == null)
					{
						float num8 = (((int)chartControl.BarSpacingType == 3 || ((int)chartControl.BarSpacingType == 1 && num4 + Displacement >= ChartBars.Count)) ? chartControl.GetXByTime(ChartBars.GetTimeByBarIdx(chartControl, num4 + Displacement)) : chartControl.GetXByBarIndex(ChartBars, num4 + Displacement));
						float num9 = chartScale.GetYByValue(num5);
						val = new SharpDX.Direct2D1.PathGeometry(Globals.D2DFactory);
						val2 = val.Open();
						((SimplifiedGeometrySink)val2).BeginFigure(new Vector2(num8, num9), FigureBegin.Hollow);
					}
					val2.AddLine(new Vector2(num6, num7));
				}
				num4 = j;
				num5 = valueAt;
			}
			if (val2 != null)
			{
				((SimplifiedGeometrySink)val2).EndFigure(FigureEnd.Open);
				((SimplifiedGeometrySink)val2).Close();
			}
			if (val != null)
			{
				AntialiasMode antialiasMode = RenderTarget.AntialiasMode;
				RenderTarget.AntialiasMode = (AntialiasMode)0;
				RenderTarget.DrawGeometry(val, Plots[0].BrushDX, Plots[0].Width, Plots[0].StrokeStyle);
				RenderTarget.AntialiasMode = antialiasMode;
				((DisposeBase)val).Dispose();
				RemoveDrawObject("NinjaScriptInfo");
			}
			else
			{
				Draw.TextFixed(this, "NinjaScriptInfo", "ZigZag deviation value error", TextPosition.BottomRight);
			}
		}
		#endregion
	}
}
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
					if (cacheUltimateSignalsIndicator[idx] != null &&  cacheUltimateSignalsIndicator[idx].EqualsInput(input))
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
