using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Core;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.Tools;
using NinjaTrader.NinjaScript.DrawingTools;
using NinjaTrader.NinjaScript.Indicators.DimDim;
namespace NinjaTrader.NinjaScript.Indicators.DimDim
{
	[CategoryOrder("General", 1000010)]
	[CategoryOrder("Alerts", 1000040)]
	[CategoryOrder("Graphics", 1000020)]
	[CategoryOrder("Critical", 1000070)]
	[CategoryOrder("Developer", 0)]
	[CategoryOrder("Toggle", 1000050)]
	[CategoryOrder("Special", 1000060)]
	[CategoryOrder("Gradient", 1000030)]
	public class DDNobleCloud : Indicator
	{
		[Display(Name = "Marker: Enabled", Order = 30, GroupName = "Alerts")]
		public bool MarkerEnabled { get; set; }
		[XmlIgnore]
		[Display(Name = "Marker: Color Bullish", Order = 31, GroupName = "Alerts")]
		public Brush MarkerBrushBullish { get; set; }
		[Browsable(false)]
		public string MarkerBrushBullish_Serialize
		{
			get
			{
				return Serialize.BrushToString(this.MarkerBrushBullish);
			}
			set
			{
				this.MarkerBrushBullish = Serialize.StringToBrush(value);
			}
		}
		[XmlIgnore]
		[Display(Name = "Marker: Color Bearish", Order = 32, GroupName = "Alerts")]
		public Brush MarkerBrushBearish { get; set; }
		[Browsable(false)]
		public string MarkerBrushBearish_Serialize
		{
			get
			{
				return Serialize.BrushToString(this.MarkerBrushBearish);
			}
			set
			{
				this.MarkerBrushBearish = Serialize.StringToBrush(value);
			}
		}
		[Display(Name = "Marker: String Bullish", Order = 33, GroupName = "Alerts")]
		public string MarkerStringBullish { get; set; }
		[Display(Name = "Marker: String Bearish", Order = 34, GroupName = "Alerts")]
		public string MarkerStringBearish { get; set; }
		[Display(Name = "Marker: Font", Order = 40, GroupName = "Alerts")]
		public SimpleFont MarkerFont { get; set; }
		[Display(Name = "Marker: Offset", Order = 41, GroupName = "Alerts")]
		public int MarkerOffset { get; set; }
		[Range(0, 2147483647)]
		[Display(Name = "Alert Blocking (Seconds)", Order = 50, GroupName = "Alerts", Description = "The minimum interval between 2 consecutive alerts")]
		public int AlertBlockingSeconds { get; set; }
		[Display(Name = "Website", Order = 0, GroupName = "Developer")]
		public string Website
		{
			get
			{
				return "DD.com";
			}
		}
		[Display(Name = "Update", Order = 10, GroupName = "Developer")]
		public new string Update
		{
			get
			{
				return "03 Mar 2023";
			}
		}
		[Display(Name = "Logo: Enabled", Order = 20, GroupName = "Developer")]
		public bool LogoEnabled { get; set; }
		[Display(Name = "Instruction: Enabled", Order = 30, GroupName = "Developer")]
		public bool InstructionEnabled { get; set; }
		[Range(99, 500)]
		[Display(Name = "Screen DPI", Order = 100, GroupName = "General")]
		public int ScreenDPI { get; set; }
		[Display(Name = "Plot: Enabled", Order = 10, GroupName = "Graphics")]
		public bool PlotEnabled { get; set; }
		[XmlIgnore]
		[Display(Name = "Plot: Rise", Order = 11, GroupName = "Graphics")]
		public Brush PlotRise { get; set; }
		[Browsable(false)]
		public string PlotRise_Serialize
		{
			get
			{
				return Serialize.BrushToString(this.PlotRise);
			}
			set
			{
				this.PlotRise = Serialize.StringToBrush(value);
			}
		}
		[XmlIgnore]
		[Display(Name = "Plot: Fall", Order = 12, GroupName = "Graphics")]
		public Brush PlotFall { get; set; }
		[Browsable(false)]
		public string PlotFall_Serialize
		{
			get
			{
				return Serialize.BrushToString(this.PlotFall);
			}
			set
			{
				this.PlotFall = Serialize.StringToBrush(value);
			}
		}
		[Display(Name = "Bar: Enabled", Order = 20, GroupName = "Graphics")]
		public bool BarEnabled { get; set; }
		[Display(Name = "Bar: Bullish", Order = 21, GroupName = "Graphics")]
		[XmlIgnore]
		public Brush BarBullish { get; set; }
		[Browsable(false)]
		public string BarBullish_Serialize
		{
			get
			{
				return Serialize.BrushToString(this.BarBullish);
			}
			set
			{
				this.BarBullish = Serialize.StringToBrush(value);
			}
		}
		[Display(Name = "Bar: Bearish", Order = 22, GroupName = "Graphics")]
		[XmlIgnore]
		public Brush BarBearish { get; set; }
		[Browsable(false)]
		public string BarBearish_Serialize
		{
			get
			{
				return Serialize.BrushToString(this.BarBearish);
			}
			set
			{
				this.BarBearish = Serialize.StringToBrush(value);
			}
		}
		[Display(Name = "Bar: Outline Enabled", Order = 23, GroupName = "Graphics")]
		public bool BarOutlineEnabled { get; set; }
		[Display(Name = "Bar: Bias Based", Order = 24, GroupName = "Graphics")]
		public bool BarBiasBased { get; set; }
		[Display(Name = "Cloud: Enabled", Order = 30, GroupName = "Graphics")]
		public bool CloudEnabled { get; set; }
		[XmlIgnore]
		[Display(Name = "Cloud: Bullish", Order = 31, GroupName = "Graphics")]
		public Brush CloudBullish { get; set; }
		[Browsable(false)]
		public string CloudBullish_Serialize
		{
			get
			{
				return Serialize.BrushToString(this.CloudBullish);
			}
			set
			{
				this.CloudBullish = Serialize.StringToBrush(value);
			}
		}
		[XmlIgnore]
		[Display(Name = "Cloud: Bearish", Order = 32, GroupName = "Graphics")]
		public Brush CloudBearish { get; set; }
		[Browsable(false)]
		public string CloudBearish_Serialize
		{
			get
			{
				return Serialize.BrushToString(this.CloudBearish);
			}
			set
			{
				this.CloudBearish = Serialize.StringToBrush(value);
			}
		}
		[Range(0, 100)]
		[Display(Name = "Cloud: Opacity", Order = 33, GroupName = "Graphics")]
		public int CloudOpacity { get; set; }
		[Display(Name = "Sensitivity", Order = 10, GroupName = "Parameters")]
		[Range(0.0, 1.7976931348623157E+308)]
		[NinjaScriptProperty]
		public double Sensitivity { get; set; }
		[NinjaScriptProperty]
		[Range(1, 2147483647)]
		[Display(Name = "Smoothness", Order = 11, GroupName = "Parameters")]
		public int Smoothness { get; set; }
		[NinjaScriptProperty]
		[Display(Name = "Baseline: MA Type", Order = 20, GroupName = "Parameters")]
		public DD_MAType BaselineMAType { get; set; }
		[Display(Name = "Baseline: Period", Order = 21, GroupName = "Parameters")]
		[Range(1, 2147483647)]
		[NinjaScriptProperty]
		public int BaselinePeriod { get; set; }
		[Display(Name = "Baseline: Smoothing Enabled", Order = 22, GroupName = "Parameters")]
		[NinjaScriptProperty]
		public bool BaselineSmoothingEnabled { get; set; }
		[NinjaScriptProperty]
		[Display(Name = "Baseline: Smoothing Method", Order = 23, GroupName = "Parameters")]
		public DD_MAType BaselineSmoothingMethod { get; set; }
		[Display(Name = "Baseline: Smoothing Period", Order = 24, GroupName = "Parameters")]
		[Range(1, 2147483647)]
		[NinjaScriptProperty]
		public int BaselineSmoothingPeriod { get; set; }
		[NinjaScriptProperty]
		[Display(Name = "Kernel: MA Type", Order = 40, GroupName = "Parameters")]
		public DD_MAType KernelMAType { get; set; }
		[NinjaScriptProperty]
		[Display(Name = "Kernel: Period", Order = 41, GroupName = "Parameters")]
		[Range(1, 2147483647)]
		public int KernelPeriod { get; set; }
		[NinjaScriptProperty]
		[Display(Name = "Kernel: Smoothing Enabled", Order = 42, GroupName = "Parameters")]
		public bool KernelSmoothingEnabled { get; set; }
		[NinjaScriptProperty]
		[Display(Name = "Kernel: Smoothing Method", Order = 43, GroupName = "Parameters")]
		public DD_MAType KernelSmoothingMethod { get; set; }
		[Display(Name = "Kernel: Smoothing Period", Order = 44, GroupName = "Parameters")]
		[Range(1, 2147483647)]
		[NinjaScriptProperty]
		public int KernelSmoothingPeriod { get; set; }
		[Range(0, 2147483647)]
		[Display(Name = "Signal Split (Bars)", Order = 50, GroupName = "Parameters")]
		[NinjaScriptProperty]
		public int SignalSplit { get; set; }
		[NinjaScriptProperty]
		[Display(Name = "Filter: Enabled", Order = 60, GroupName = "Parameters")]
		public bool FilterEnabled { get; set; }
		[Range(1, 2147483647)]
		[NinjaScriptProperty]
		[Display(Name = "Filter: Bar Min", Order = 61, GroupName = "Parameters")]
		public int FilterBarMin { get; set; }
		[Display(Name = "Filter: Bar Max", Order = 62, GroupName = "Parameters")]
		[NinjaScriptProperty]
		[Range(1, 2147483647)]
		public int FilterBarMax { get; set; }
		[Display(Name = "Z Order", Order = 0, GroupName = "Special")]
		public int IndicatorZOrder { get; set; }
		[Display(Name = "User Note", Order = 10, GroupName = "Special")]
		public string UserNote { get; set; }
		[Browsable(false)]
		[XmlIgnore]
		public Series<double> Baseline
		{
			get
			{
				return base.Values[0];
			}
		}
		[XmlIgnore]
		[Browsable(false)]
		public Series<double> Signal_Cloud
		{
			get
			{
				return base.Values[1];
			}
		}
		[XmlIgnore]
		[Browsable(false)]
		public Series<double> Signal_Trade
		{
			get
			{
				return base.Values[2];
			}
		}
		public override string DisplayName
		{
			
			get
			{
				if (base.Parent is MarketAnalyzerColumnBase)
				{
					return base.DisplayName;
				}
				return "DDNoble Cloud";
			}
		}
		
		private string GetUserNote()
		{
			string text = this.UserNote.Trim();
			if (string.IsNullOrWhiteSpace(text))
			{
				return string.Empty;
			}
			text = text.ToLower();
			if (text.Contains("instrument") && base.Instrument != null)
			{
				text = text.Replace("instrument", base.Instrument.FullName);
			}
			if (text.Contains("period") && base.BarsPeriod != null)
			{
				text = text.Replace("period", base.BarsPeriod.ToString());
			}
			return " (" + text + ")";
		}
		
		protected override void OnStateChange()
		{
			try
			{
				if (base.State == State.SetDefaults)
				{
					base.Description = string.Empty;
					base.Name = "DDNobleCloud";
					base.Calculate = Calculate.OnBarClose;
					base.IsOverlay = true;
					base.DisplayInDataBox = true;
					base.DrawOnPricePanel = true;
					base.DrawHorizontalGridLines = true;
					base.DrawVerticalGridLines = true;
					base.PaintPriceMarkers = true;
					base.ScaleJustification = ScaleJustification.Right;
					base.IsSuspendedWhileInactive = false;
					base.BarsRequiredToPlot = 0;
					this.MarkerEnabled = true;
					this.MarkerBrushBullish = Brushes.Lime;
					this.MarkerBrushBearish = Brushes.DarkOrange;
					this.MarkerStringBullish = "⯭";
					this.MarkerStringBearish = "⯯";
					this.MarkerFont = new SimpleFont("Arial", 26);
					this.MarkerOffset = 10;
					this.AlertBlockingSeconds = 60;
					this.LogoEnabled = true;
					this.InstructionEnabled = true;
					this.ScreenDPI = 99;
					this.PlotEnabled = true;
					this.PlotRise = Brushes.DodgerBlue;
					this.PlotFall = Brushes.DeepPink;
					this.BarEnabled = true;
					this.BarBullish = Brushes.DodgerBlue;
					this.BarBearish = Brushes.HotPink;
					this.BarOutlineEnabled = true;
					this.BarBiasBased = true;
					this.CloudEnabled = true;
					this.CloudBullish = Brushes.Teal;
					this.CloudBearish = Brushes.DarkSlateBlue;
					this.CloudOpacity = 100;
					this.Sensitivity = 100.0;
					this.Smoothness = 1;
					this.BaselineMAType = DD_MAType.SMA;
					this.BaselinePeriod = 60;
					this.BaselineSmoothingEnabled = true;
					this.BaselineSmoothingMethod = DD_MAType.EMA;
					this.BaselineSmoothingPeriod = 60;
					this.KernelMAType = DD_MAType.SMA;
					this.KernelPeriod = 20;
					this.KernelSmoothingEnabled = true;
					this.KernelSmoothingMethod = DD_MAType.EMA;
					this.KernelSmoothingPeriod = 5;
					this.SignalSplit = 5;
					this.FilterEnabled = true;
					this.FilterBarMin = 10;
					this.FilterBarMax = 300;
					this.IndicatorZOrder = 0;
					this.UserNote = "instrument (period)";
					base.AddPlot(new Stroke(Brushes.Orange, DashStyleHelper.Dash, 2f), PlotStyle.Line, "Baseline");
					base.AddPlot(new Stroke(Brushes.Transparent, DashStyleHelper.Solid, 1f), PlotStyle.Line, "Signal: Cloud");
					base.AddPlot(new Stroke(Brushes.Transparent, DashStyleHelper.Solid, 1f), PlotStyle.Line, "Signal: Trade");
					base.ShowTransparentPlotsInDataBox = true;
				}
				else if (base.State == State.Configure)
				{
					this.indicatorNameFull = "Noble Cloud by DD.com";
					base.Calculate = Calculate.OnBarClose;
					this.seriesUpperThresholdRaw = new Series<double>(this, MaximumBarsLookBack.Infinite);
					this.seriesLowerThresholdRaw = new Series<double>(this, MaximumBarsLookBack.Infinite);
					this.seriesUpperThresholdSmoothed = new Series<double>(this, MaximumBarsLookBack.Infinite);
					this.seriesLowerThresholdSmoothed = new Series<double>(this, MaximumBarsLookBack.Infinite);
					this.thresholdSmoothingEnabled = this.Smoothness > 1;
					if (this.FilterBarMin > this.FilterBarMax)
					{
						int filterBarMin = this.FilterBarMin;
						this.FilterBarMin = this.FilterBarMax;
						this.FilterBarMax = filterBarMin;
					}
					this.effectiveSensitivity = this.Sensitivity / 50.0;
					this.nextAcceptedBar_Bearish = 0;
					this.nextAcceptedBar_Bullish = 0;
				}
				else if (base.State == State.DataLoaded)
				{
					this.kernelSmoothed = this.GetMASeries_Smoothed(base.Input, this.KernelMAType, this.KernelPeriod, this.KernelSmoothingEnabled, this.KernelSmoothingMethod, this.KernelSmoothingPeriod);
					this.baselineSmoothed = this.GetMASeries_Smoothed(base.Input, this.BaselineMAType, this.BaselinePeriod, this.BaselineSmoothingEnabled, this.BaselineSmoothingMethod, this.BaselineSmoothingPeriod);
				}
				else if (base.State == State.Historical)
				{
						if (this.ScreenDPI < 100)
						{
							this.ScreenDPI = this.GetDPI();
						}
						if (this.IndicatorZOrder != 0)
						{
							base.SetZOrder(this.IndicatorZOrder);
						}
						this.isCharting = base.ChartControl != null;
				}
			}
			catch (Exception ex)
			{
				this.PrintException(ex);
			}
		}
		
		protected override void OnBarUpdate()
		{
			try
			{
					double num = base.StdDev(base.Input, this.KernelPeriod)[0];
					this.seriesUpperThresholdRaw[0] = this.kernelSmoothed[0] + this.effectiveSensitivity * num;
					this.seriesLowerThresholdRaw[0] = this.kernelSmoothed[0] - this.effectiveSensitivity * num;
					this.seriesUpperThresholdSmoothed[0] = (this.thresholdSmoothingEnabled ? this.ComputeMAValue(this.seriesUpperThresholdRaw, DD_MAType.EMA, this.Smoothness) : this.seriesUpperThresholdRaw[0]);
					this.seriesLowerThresholdSmoothed[0] = (this.thresholdSmoothingEnabled ? this.ComputeMAValue(this.seriesLowerThresholdRaw, DD_MAType.EMA, this.Smoothness) : this.seriesLowerThresholdRaw[0]);
					this.Baseline[0] = this.baselineSmoothed[0];
					if (base.CurrentBar != 0)
					{
						if (base.CurrentBar == 1)
						{
							this.isRisingBaseline = this.Baseline[0].ApproxCompare(this.Baseline[1]) > 0;
						}
						else if (base.CurrentBar >= 2)
						{
							if (this.isRisingBaseline)
							{
								if (this.Baseline[0].ApproxCompare(this.Baseline[1]) < 0)
								{
									this.isRisingBaseline = false;
								}
							}
							else if (this.Baseline[0].ApproxCompare(this.Baseline[1]) > 0)
							{
								this.isRisingBaseline = true;
							}
						}
					}
					if (base.CurrentBar == 0)
					{
						if (this.Baseline[0].ApproxCompare(this.seriesUpperThresholdSmoothed[0]) > 0)
						{
							this.barCount_Baseline = 1;
							this.cloudState = -1;
							this.cloudIndex = base.CurrentBar;
						}
						else if (this.Baseline[0].ApproxCompare(this.seriesLowerThresholdSmoothed[0]) < 0)
						{
							this.barCount_Baseline = 1;
							this.cloudState = 1;
							this.cloudIndex = base.CurrentBar;
						}
						else
						{
							this.barCount_Baseline = -1;
							this.cloudState = 0;
							this.cloudIndex = -1;
						}
					}
					else
					{
						if (this.cloudState != 0 && this.Baseline[0].ApproxCompare(this.seriesUpperThresholdSmoothed[0]) <= 0 && this.Baseline[0].ApproxCompare(this.seriesLowerThresholdSmoothed[0]) >= 0)
						{
							this.barCount_Baseline = -1;
							this.cloudState = 0;
							this.cloudIndex = -1;
						}
						else if (this.cloudState <= 0 && this.Baseline[0].ApproxCompare(this.seriesLowerThresholdSmoothed[0]) < 0)
						{
							this.barCount_Baseline = 1;
							this.cloudState = 1;
							this.cloudIndex = base.CurrentBar;
						}
						else if (this.cloudState >= 0 && this.Baseline[0].ApproxCompare(this.seriesUpperThresholdSmoothed[0]) > 0)
						{
							this.barCount_Baseline = 1;
							this.cloudState = -1;
							this.cloudIndex = base.CurrentBar;
						}
						if (this.Signal_Cloud[1] == (double)this.cloudState && this.barCount_Baseline >= 1)
						{
							this.barCount_Baseline++;
						}
					}
					int num2 = 0;
					if (!this.FilterEnabled || (this.barCount_Baseline >= this.FilterBarMin && this.barCount_Baseline <= this.FilterBarMax))
					{
						if (this.cloudState > 0)
						{
							if (base.Low[0].ApproxCompare(this.seriesLowerThresholdSmoothed[0]) <= 0 && base.Close[0].ApproxCompare(this.seriesLowerThresholdSmoothed[0]) > 0 && base.Close[1].ApproxCompare(base.Open[1]) < 0 && base.Close[0].ApproxCompare(base.Open[0]) > 0 && base.CurrentBar >= this.nextAcceptedBar_Bullish)
							{
								num2 = 1;
								this.nextAcceptedBar_Bullish = base.CurrentBar + this.SignalSplit;
								this.PaintBar(true);
								this.PrintMarker(true);
							}
						}
						else if (this.cloudState < 0 && base.High[0].ApproxCompare(this.seriesUpperThresholdSmoothed[0]) >= 0 && base.Close[0].ApproxCompare(this.seriesUpperThresholdSmoothed[0]) < 0 && base.Close[1].ApproxCompare(base.Open[1]) > 0 && base.Close[0].ApproxCompare(base.Open[0]) < 0 && base.CurrentBar >= this.nextAcceptedBar_Bearish)
						{
							num2 = -1;
							this.nextAcceptedBar_Bearish = base.CurrentBar + this.SignalSplit;
							this.PaintBar(false);
							this.PrintMarker(false);
						}
					}
					if (this.CloudEnabled && this.CloudOpacity > 0 && base.CurrentBar >= base.BarsRequiredToPlot && this.cloudState != 0)
					{
						ISeries<double> baseline = this.Baseline;
						string text;
						Brush brush;
						ISeries<double> series;
						if (this.cloudState > 0)
						{
							text = "cloud.bullish";
							brush = this.CloudBullish;
							series = this.seriesLowerThresholdSmoothed;
						}
						else
						{
							text = "cloud.bearish";
							brush = this.CloudBearish;
							series = this.seriesUpperThresholdSmoothed;
						}
						string text2 = string.Format("{0}.{1}.{2}", "DDNobleCloud", text, this.cloudIndex);
						NinjaTrader.NinjaScript.DrawingTools.Draw.Region(this, text2, base.CurrentBar - this.cloudIndex, 0, series, baseline, Brushes.Transparent, brush, this.CloudOpacity, 0);
					}
					if (this.PlotEnabled)
					{
						base.PlotBrushes[0][0] = (this.isRisingBaseline ? this.PlotRise : this.PlotFall);
					}
					this.Signal_Cloud[0] = (double)this.cloudState;
					this.Signal_Trade[0] = (double)num2;
			}
			catch (Exception ex)
			{
				this.PrintException(ex);
			}
		}
	
		
		private void TestSignal()
		{
			NinjaTrader.NinjaScript.DrawingTools.Draw.Text(this, "DDNobleCloud.test.barCount_Baseline." + base.CurrentBar, base.IsAutoScale, this.barCount_Baseline.ToString(), 0, base.Low[0], -10, (this.barCount_Baseline <= 0) ? Brushes.Gold : Brushes.DodgerBlue, new SimpleFont("Arial", 12), TextAlignment.Center, Brushes.Transparent, Brushes.Transparent, 0);
		}
		
		private void PrintMarker(bool isBullish)
		{
			if (!this.isCharting)
			{
				return;
			}
			if (!this.MarkerEnabled)
			{
				return;
			}
			if (base.CurrentBar < base.BarsRequiredToPlot)
			{
				return;
			}
			string text = "DDNobleCloud.marker." + base.CurrentBar;
			Brush brush = (isBullish ? this.MarkerBrushBullish : this.MarkerBrushBearish);
			double num = (isBullish ? base.Low[0] : base.High[0]);
			string text2 = (isBullish ? this.MarkerStringBullish : this.MarkerStringBearish);
			text2 = this.FormatMarkerString(text2);
			int num2 = Convert.ToInt32(this.ComputeTextSize(text2, this.MarkerFont, this.ScreenDPI).Height);
			int num3 = (isBullish ? (-1) : 1);
			int num4 = num3 * (this.MarkerOffset + num2 / 2);
			NinjaTrader.NinjaScript.DrawingTools.Draw.Text(this, text, base.IsAutoScale, text2, 0, num, num4, brush, this.MarkerFont, TextAlignment.Center, Brushes.Transparent, Brushes.Transparent, 0);
		}
		
		private void PaintBar(bool isBullish)
		{
			if (!this.isCharting)
			{
				return;
			}
			if (!this.BarEnabled)
			{
				return;
			}
			Brush brush = (isBullish ? this.BarBullish : this.BarBearish);
			int num = base.Close[0].ApproxCompare(base.Open[0]);
			int num2 = (isBullish ? 1 : (-1));
			if (this.BarOutlineEnabled && !brush.IsTransparent())
			{
				base.CandleOutlineBrush = brush;
			}
			if (this.BarBiasBased)
			{
				if (brush.IsTransparent())
				{
					if (num2 * num < 0)
					{
						base.BarBrush = Brushes.Transparent;
						return;
					}
				}
				else if (num != 0)
				{
					base.BarBrush = ((num2 * num > 0) ? brush : Brushes.Transparent);
					return;
				}
			}
			else if (num != 0)
			{
				base.BarBrush = brush;
			}
		}
		
		private void PrintException(Exception exception)
		{
			string text = string.Concat(new string[]
			{
				"DDNobleCloud: ",
				exception.ToString(),
				" (",
				exception.StackTrace,
				")"
			});
			base.Print(text);
			NinjaScript.Log(text, LogLevel.Error);
		}
		
		public override string FormatPriceMarker(double price)
		{
			return base.Instrument.MasterInstrument.FormatPrice(base.Instrument.MasterInstrument.RoundToTickSize(price), true);
		}

		private ISeries<double> GetMASeries(ISeries<double> inputSeries, DD_MAType maType, int period)
		{
			if (period < 1) return null;
			switch (maType)
			{
				case DD_MAType.DEMA:     return DEMA(inputSeries, period);
				case DD_MAType.EMA:      return EMA(inputSeries, period);
				case DD_MAType.HMA:      return HMA(inputSeries, period);
				case DD_MAType.LinReg:   return LinReg(inputSeries, period);
				case DD_MAType.SMA:      return SMA(inputSeries, period);
				case DD_MAType.TEMA:     return TEMA(inputSeries, period);
				case DD_MAType.TMA:      return TMA(inputSeries, period);
				case DD_MAType.VWMA:     return VWMA(inputSeries, period);
				case DD_MAType.WMA:      return WMA(inputSeries, period);
				case DD_MAType.WilderMA: return EMA(inputSeries, 2 * period);
				case DD_MAType.ZLEMA:    return ZLEMA(inputSeries, period);
			}
			return null;
		}

		private ISeries<double> GetMASeries_Smoothed(ISeries<double> inputSeries, DD_MAType maType, int period, bool smoothingEnabled, DD_MAType smoothingMethod, int smoothingPeriod)
		{
			ISeries<double> maseries = GetMASeries(inputSeries, maType, period);
			if (smoothingEnabled) return GetMASeries(maseries, smoothingMethod, smoothingPeriod);
			return maseries;
		}

		private int GetDPI()
		{
			System.Reflection.PropertyInfo property = typeof(SystemParameters).GetProperty("DpiX", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
			if (property == null) return 100;
			return Math.Max(100, Convert.ToInt32(property.GetValue(null, null)) * 100 / 96);
		}

		private string FormatMarkerString(string input)
		{
			if (string.IsNullOrWhiteSpace(input)) return string.Empty;
			string text = input.Trim();
			string[] array = text.Split(new char[] { '+' });
			array = array.Select(x => x.Trim()).ToArray();
			text = string.Join("\n", array);
			return text.Trim();
		}

		private double ComputeMAValue(ISeries<double> input, DD_MAType maType, int period)
		{
			ISeries<double> series = GetMASeries(input, maType, period);
			return series != null ? series[0] : input[0];
		}

		private System.Windows.Size ComputeTextSize(string text, SimpleFont font, int dpi)
		{
			double height = font != null ? font.Size * 1.5 : 12.0;
			double width = (text != null && font != null) ? text.Length * font.Size * 0.7 : 0;
			return new System.Windows.Size(width, height);
		}

		private const int defaultMargin = 5;
		private const string prefix = "DDNobleCloud";
		private const string indicatorName = "DDNobleCloud";
		private Series<double> seriesUpperThresholdRaw;
		private Series<double> seriesLowerThresholdRaw;
		private Series<double> seriesUpperThresholdSmoothed;
		private Series<double> seriesLowerThresholdSmoothed;
		private double effectiveSensitivity;
		private int nextAcceptedBar_Bearish;
		private int nextAcceptedBar_Bullish;
		private ISeries<double> baselineSmoothed;
		private ISeries<double> kernelSmoothed;
		private int cloudState;
		private int cloudIndex;
		private bool isRisingBaseline;
		private int barCount_Baseline;
		private bool thresholdSmoothingEnabled;
		private bool isUptrend;
		private string indicatorNameFull;
		private bool isCharting;
		private List<ChartAnchor> pathUpperThresholdSmoothed;
		private List<ChartAnchor> pathLowerThresholdSmoothed;
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private DimDim.DDNobleCloud[] cacheDDNobleCloud;
		public DimDim.DDNobleCloud DDNobleCloud(double sensitivity, int smoothness, DD_MAType baselineMAType, int baselinePeriod, bool baselineSmoothingEnabled, DD_MAType baselineSmoothingMethod, int baselineSmoothingPeriod, DD_MAType kernelMAType, int kernelPeriod, bool kernelSmoothingEnabled, DD_MAType kernelSmoothingMethod, int kernelSmoothingPeriod, int signalSplit, bool filterEnabled, int filterBarMin, int filterBarMax)
		{
			return DDNobleCloud(Input, sensitivity, smoothness, baselineMAType, baselinePeriod, baselineSmoothingEnabled, baselineSmoothingMethod, baselineSmoothingPeriod, kernelMAType, kernelPeriod, kernelSmoothingEnabled, kernelSmoothingMethod, kernelSmoothingPeriod, signalSplit, filterEnabled, filterBarMin, filterBarMax);
		}

		public DimDim.DDNobleCloud DDNobleCloud(ISeries<double> input, double sensitivity, int smoothness, DD_MAType baselineMAType, int baselinePeriod, bool baselineSmoothingEnabled, DD_MAType baselineSmoothingMethod, int baselineSmoothingPeriod, DD_MAType kernelMAType, int kernelPeriod, bool kernelSmoothingEnabled, DD_MAType kernelSmoothingMethod, int kernelSmoothingPeriod, int signalSplit, bool filterEnabled, int filterBarMin, int filterBarMax)
		{
			if (cacheDDNobleCloud != null)
				for (int idx = 0; idx < cacheDDNobleCloud.Length; idx++)
					if (cacheDDNobleCloud[idx] != null && cacheDDNobleCloud[idx].Sensitivity == sensitivity && cacheDDNobleCloud[idx].Smoothness == smoothness && cacheDDNobleCloud[idx].BaselineMAType == baselineMAType && cacheDDNobleCloud[idx].BaselinePeriod == baselinePeriod && cacheDDNobleCloud[idx].BaselineSmoothingEnabled == baselineSmoothingEnabled && cacheDDNobleCloud[idx].BaselineSmoothingMethod == baselineSmoothingMethod && cacheDDNobleCloud[idx].BaselineSmoothingPeriod == baselineSmoothingPeriod && cacheDDNobleCloud[idx].KernelMAType == kernelMAType && cacheDDNobleCloud[idx].KernelPeriod == kernelPeriod && cacheDDNobleCloud[idx].KernelSmoothingEnabled == kernelSmoothingEnabled && cacheDDNobleCloud[idx].KernelSmoothingMethod == kernelSmoothingMethod && cacheDDNobleCloud[idx].KernelSmoothingPeriod == kernelSmoothingPeriod && cacheDDNobleCloud[idx].SignalSplit == signalSplit && cacheDDNobleCloud[idx].FilterEnabled == filterEnabled && cacheDDNobleCloud[idx].FilterBarMin == filterBarMin && cacheDDNobleCloud[idx].FilterBarMax == filterBarMax && cacheDDNobleCloud[idx].EqualsInput(input))
						return cacheDDNobleCloud[idx];
			return CacheIndicator<DimDim.DDNobleCloud>(new DimDim.DDNobleCloud(){ Sensitivity = sensitivity, Smoothness = smoothness, BaselineMAType = baselineMAType, BaselinePeriod = baselinePeriod, BaselineSmoothingEnabled = baselineSmoothingEnabled, BaselineSmoothingMethod = baselineSmoothingMethod, BaselineSmoothingPeriod = baselineSmoothingPeriod, KernelMAType = kernelMAType, KernelPeriod = kernelPeriod, KernelSmoothingEnabled = kernelSmoothingEnabled, KernelSmoothingMethod = kernelSmoothingMethod, KernelSmoothingPeriod = kernelSmoothingPeriod, SignalSplit = signalSplit, FilterEnabled = filterEnabled, FilterBarMin = filterBarMin, FilterBarMax = filterBarMax }, input, ref cacheDDNobleCloud);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.DimDim.DDNobleCloud DDNobleCloud(double sensitivity, int smoothness, DD_MAType baselineMAType, int baselinePeriod, bool baselineSmoothingEnabled, DD_MAType baselineSmoothingMethod, int baselineSmoothingPeriod, DD_MAType kernelMAType, int kernelPeriod, bool kernelSmoothingEnabled, DD_MAType kernelSmoothingMethod, int kernelSmoothingPeriod, int signalSplit, bool filterEnabled, int filterBarMin, int filterBarMax)
		{
			return indicator.DDNobleCloud(Input, sensitivity, smoothness, baselineMAType, baselinePeriod, baselineSmoothingEnabled, baselineSmoothingMethod, baselineSmoothingPeriod, kernelMAType, kernelPeriod, kernelSmoothingEnabled, kernelSmoothingMethod, kernelSmoothingPeriod, signalSplit, filterEnabled, filterBarMin, filterBarMax);
		}

		public Indicators.DimDim.DDNobleCloud DDNobleCloud(ISeries<double> input , double sensitivity, int smoothness, DD_MAType baselineMAType, int baselinePeriod, bool baselineSmoothingEnabled, DD_MAType baselineSmoothingMethod, int baselineSmoothingPeriod, DD_MAType kernelMAType, int kernelPeriod, bool kernelSmoothingEnabled, DD_MAType kernelSmoothingMethod, int kernelSmoothingPeriod, int signalSplit, bool filterEnabled, int filterBarMin, int filterBarMax)
		{
			return indicator.DDNobleCloud(input, sensitivity, smoothness, baselineMAType, baselinePeriod, baselineSmoothingEnabled, baselineSmoothingMethod, baselineSmoothingPeriod, kernelMAType, kernelPeriod, kernelSmoothingEnabled, kernelSmoothingMethod, kernelSmoothingPeriod, signalSplit, filterEnabled, filterBarMin, filterBarMax);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.DimDim.DDNobleCloud DDNobleCloud(double sensitivity, int smoothness, DD_MAType baselineMAType, int baselinePeriod, bool baselineSmoothingEnabled, DD_MAType baselineSmoothingMethod, int baselineSmoothingPeriod, DD_MAType kernelMAType, int kernelPeriod, bool kernelSmoothingEnabled, DD_MAType kernelSmoothingMethod, int kernelSmoothingPeriod, int signalSplit, bool filterEnabled, int filterBarMin, int filterBarMax)
		{
			return indicator.DDNobleCloud(Input, sensitivity, smoothness, baselineMAType, baselinePeriod, baselineSmoothingEnabled, baselineSmoothingMethod, baselineSmoothingPeriod, kernelMAType, kernelPeriod, kernelSmoothingEnabled, kernelSmoothingMethod, kernelSmoothingPeriod, signalSplit, filterEnabled, filterBarMin, filterBarMax);
		}

		public Indicators.DimDim.DDNobleCloud DDNobleCloud(ISeries<double> input , double sensitivity, int smoothness, DD_MAType baselineMAType, int baselinePeriod, bool baselineSmoothingEnabled, DD_MAType baselineSmoothingMethod, int baselineSmoothingPeriod, DD_MAType kernelMAType, int kernelPeriod, bool kernelSmoothingEnabled, DD_MAType kernelSmoothingMethod, int kernelSmoothingPeriod, int signalSplit, bool filterEnabled, int filterBarMin, int filterBarMax)
		{
			return indicator.DDNobleCloud(input, sensitivity, smoothness, baselineMAType, baselinePeriod, baselineSmoothingEnabled, baselineSmoothingMethod, baselineSmoothingPeriod, kernelMAType, kernelPeriod, kernelSmoothingEnabled, kernelSmoothingMethod, kernelSmoothingPeriod, signalSplit, filterEnabled, filterBarMin, filterBarMax);
		}
	}
}

#endregion
