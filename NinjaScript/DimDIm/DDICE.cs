using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using System.Xml.Serialization;
using NinjaTrader.Core;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.Data;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.Tools;
using NinjaTrader.NinjaScript.DrawingTools;
using SharpDX;
using SharpDX.Direct2D1;
namespace NinjaTrader.NinjaScript.Indicators.DimDim
{
	[CategoryOrder("Toggle", 1000050)]
	[CategoryOrder("Special", 1000060)]
	[TypeConverter("NinjaTrader.NinjaScript.Indicators.DimDim.DDSonarlikeIcebergFinder_Converter")]
	[CategoryOrder("Critical", 1000070)]
	[CategoryOrder("Graphics", 1000020)]
	[CategoryOrder("General", 1000010)]
	[CategoryOrder("Developer", 0)]
	[CategoryOrder("Alerts", 1000040)]
	[CategoryOrder("Gradient", 1000030)]
	public class DDSonarlikeIcebergFinder : Indicator
	{
		public string EmailReceiver { get; set; }
		[Display(Name = "Marker: Enabled", Order = 70, GroupName = "Alerts")]
		public bool MarkerEnabled { get; set; }
		[Display(Name = "Marker: Rendering Method", Order = 72, GroupName = "Alerts", Description = "\"Custom\" rendering provides a better appearance, and is therefore recommended in most cases.\nIf your computer is weak or slow, please switch to \"Builtin\" rendering.")]
		public DDSonarlikeIcebergFinder_RenderingMethod MarkerRenderingMethod { get; set; }
		[Display(Name = "Marker: Color Bullish", Order = 74, GroupName = "Alerts")]
		[XmlIgnore]
		public global::System.Windows.Media.Brush MarkerBrushBullish { get; set; }
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
		[Display(Name = "Marker: Color Bearish", Order = 76, GroupName = "Alerts")]
		[XmlIgnore]
		public global::System.Windows.Media.Brush MarkerBrushBearish { get; set; }
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
		[Display(Name = "Marker: String Bullish", Order = 78, GroupName = "Alerts")]
		public string MarkerStringBullish { get; set; }
		[Display(Name = "Marker: String Bearish", Order = 80, GroupName = "Alerts")]
		public string MarkerStringBearish { get; set; }
		[Display(Name = "Marker: Font", Order = 82, GroupName = "Alerts")]
		public SimpleFont MarkerFont { get; set; }
		[Display(Name = "Marker: Offset", Order = 84, GroupName = "Alerts")]
		public int MarkerOffset { get; set; }
		[Range(0, 2147483647)]
		[Display(Name = "Alert Blocking (Seconds)", Order = 100, GroupName = "Alerts", Description = "The minimum interval between 2 consecutive alerts")]
		public int AlertBlockingSeconds { get; set; }
		[Display(Name = "Switched On", Order = 0, GroupName = "Critical")]
		public bool SwitchedOn { get; set; }
		[Display(Name = "Bar: Enabled", Order = 10, GroupName = "Graphics")]
		public bool BarEnabled { get; set; }
		[Display(Name = "Bar: Range Bullish", Order = 12, GroupName = "Graphics")]
		[XmlIgnore]
		public global::System.Windows.Media.Brush BarRangeBullish { get; set; }
		[Browsable(false)]
		public string BarRangeBullish_Serialize
		{
			get
			{
				return Serialize.BrushToString(this.BarRangeBullish);
			}
			set
			{
				this.BarRangeBullish = Serialize.StringToBrush(value);
			}
		}
		[Display(Name = "Bar: Range Bearish", Order = 14, GroupName = "Graphics")]
		[XmlIgnore]
		public global::System.Windows.Media.Brush BarRangeBearish { get; set; }
		[Browsable(false)]
		public string BarRangeBearish_Serialize
		{
			get
			{
				return Serialize.BrushToString(this.BarRangeBearish);
			}
			set
			{
				this.BarRangeBearish = Serialize.StringToBrush(value);
			}
		}
		[XmlIgnore]
		[Display(Name = "Bar: Trigger Bullish", Order = 16, GroupName = "Graphics")]
		public global::System.Windows.Media.Brush BarTriggerBullish { get; set; }
		[Browsable(false)]
		public string BarTriggerBullish_Serialize
		{
			get
			{
				return Serialize.BrushToString(this.BarTriggerBullish);
			}
			set
			{
				this.BarTriggerBullish = Serialize.StringToBrush(value);
			}
		}
		[XmlIgnore]
		[Display(Name = "Bar: Trigger Bearish", Order = 18, GroupName = "Graphics")]
		public global::System.Windows.Media.Brush BarTriggerBearish { get; set; }
		[Browsable(false)]
		public string BarTriggerBearish_Serialize
		{
			get
			{
				return Serialize.BrushToString(this.BarTriggerBearish);
			}
			set
			{
				this.BarTriggerBearish = Serialize.StringToBrush(value);
			}
		}
		[Display(Name = "Bar: Outline Enabled", Order = 20, GroupName = "Graphics")]
		public bool BarOutlineEnabled { get; set; }
		[Display(Name = "Bar: Bias Based", Order = 22, GroupName = "Graphics")]
		public bool BarBiasBased { get; set; }
		[Display(Name = "Summary Range: Enabled", Order = 70, GroupName = "Graphics")]
		public bool SummaryRangeEnabled { get; set; }
		[Display(Name = "Summary Range: Delta Positive Color", Order = 72, GroupName = "Graphics")]
		[XmlIgnore]
		public global::System.Windows.Media.Brush SummaryRangeDeltaPositiveBrush { get; set; }
		[Browsable(false)]
		public string SummaryRangeDeltaPositiveBrush_Serialize
		{
			get
			{
				return Serialize.BrushToString(this.SummaryRangeDeltaPositiveBrush);
			}
			set
			{
				this.SummaryRangeDeltaPositiveBrush = Serialize.StringToBrush(value);
			}
		}
		[XmlIgnore]
		[Display(Name = "Summary Range: Delta Negative Color", Order = 74, GroupName = "Graphics")]
		public global::System.Windows.Media.Brush SummaryRangeDeltaNegativeBrush { get; set; }
		[Browsable(false)]
		public string SummaryRangeDeltaNegativeBrush_Serialize
		{
			get
			{
				return Serialize.BrushToString(this.SummaryRangeDeltaNegativeBrush);
			}
			set
			{
				this.SummaryRangeDeltaNegativeBrush = Serialize.StringToBrush(value);
			}
		}
		[XmlIgnore]
		[Display(Name = "Summary Range: Delta Neutral Color", Order = 76, GroupName = "Graphics")]
		public global::System.Windows.Media.Brush SummaryRangeDeltaNeutralBrush { get; set; }
		[Browsable(false)]
		public string SummaryRangeDeltaNeutralBrush_Serialize
		{
			get
			{
				return Serialize.BrushToString(this.SummaryRangeDeltaNeutralBrush);
			}
			set
			{
				this.SummaryRangeDeltaNeutralBrush = Serialize.StringToBrush(value);
			}
		}
		[Display(Name = "Summary Range: Force Bar Positive Color", Order = 78, GroupName = "Graphics")]
		[XmlIgnore]
		public global::System.Windows.Media.Brush SummaryRangeForceBarPositiveBrush { get; set; }
		[Browsable(false)]
		public string SummaryRangeForceBarPositiveBrush_Serialize
		{
			get
			{
				return Serialize.BrushToString(this.SummaryRangeForceBarPositiveBrush);
			}
			set
			{
				this.SummaryRangeForceBarPositiveBrush = Serialize.StringToBrush(value);
			}
		}
		[XmlIgnore]
		[Display(Name = "Summary Range: Force Bar Negative Color", Order = 80, GroupName = "Graphics")]
		public global::System.Windows.Media.Brush SummaryRangeForceBarNegativeBrush { get; set; }
		[Browsable(false)]
		public string SummaryRangeForceBarNegativeBrush_Serialize
		{
			get
			{
				return Serialize.BrushToString(this.SummaryRangeForceBarNegativeBrush);
			}
			set
			{
				this.SummaryRangeForceBarNegativeBrush = Serialize.StringToBrush(value);
			}
		}
		[Display(Name = "Summary Range: Number Only", Order = 82, GroupName = "Graphics")]
		public bool SummaryRangeNumberOnly { get; set; }
		[Display(Name = "Summary Range: Number Font", Order = 84, GroupName = "Graphics")]
		public SimpleFont SummaryRangeNumberFont { get; set; }
		[Display(Name = "Summary Range: Height", Order = 86, GroupName = "Graphics")]
		[Range(0, 2147483647)]
		public int SummaryRangeHeight { get; set; }
		[Range(0, 2147483647)]
		[Display(Name = "Summary Range: Margin", Order = 88, GroupName = "Graphics")]
		public int SummaryRangeMargin { get; set; }
		[Display(Name = "Summary Trigger: Enabled", Order = 90, GroupName = "Graphics")]
		public bool SummaryTriggerEnabled { get; set; }
		[XmlIgnore]
		[Display(Name = "Summary Trigger: Buy Volume Color", Order = 92, GroupName = "Graphics")]
		public global::System.Windows.Media.Brush SummaryTriggerBuyVolumeBrush { get; set; }
		[Browsable(false)]
		public string SummaryTriggerVolumeBuyBrush_Serialize
		{
			get
			{
				return Serialize.BrushToString(this.SummaryTriggerBuyVolumeBrush);
			}
			set
			{
				this.SummaryTriggerBuyVolumeBrush = Serialize.StringToBrush(value);
			}
		}
		[Display(Name = "Summary Trigger: Buy Volume String", Order = 94, GroupName = "Graphics")]
		public string SummaryTriggerBuyVolumeString { get; set; }
		[Display(Name = "Summary Trigger: Buy Force Bar Color", Order = 96, GroupName = "Graphics")]
		[XmlIgnore]
		public global::System.Windows.Media.Brush SummaryTriggerBuyForceBarBrush { get; set; }
		[Browsable(false)]
		public string SummaryTriggerBuyForceBarBrush_Serialize
		{
			get
			{
				return Serialize.BrushToString(this.SummaryTriggerBuyForceBarBrush);
			}
			set
			{
				this.SummaryTriggerBuyForceBarBrush = Serialize.StringToBrush(value);
			}
		}
		[Display(Name = "Summary Trigger: Sell Volume Color", Order = 98, GroupName = "Graphics")]
		[XmlIgnore]
		public global::System.Windows.Media.Brush SummaryTriggerSellVolumeBrush { get; set; }
		[Browsable(false)]
		public string SummaryTriggerSellVolumeBrush_Serialize
		{
			get
			{
				return Serialize.BrushToString(this.SummaryTriggerSellVolumeBrush);
			}
			set
			{
				this.SummaryTriggerSellVolumeBrush = Serialize.StringToBrush(value);
			}
		}
		[Display(Name = "Summary Trigger: Sell Volume String", Order = 100, GroupName = "Graphics")]
		public string SummaryTriggerSellVolumeString { get; set; }
		[XmlIgnore]
		[Display(Name = "Summary Trigger: Sell Force Bar Color", Order = 102, GroupName = "Graphics")]
		public global::System.Windows.Media.Brush SummaryTriggerSellForceBarBrush { get; set; }
		[Browsable(false)]
		public string SummaryTriggerSellForceBarBrush_Serialize
		{
			get
			{
				return Serialize.BrushToString(this.SummaryTriggerSellForceBarBrush);
			}
			set
			{
				this.SummaryTriggerSellForceBarBrush = Serialize.StringToBrush(value);
			}
		}
		[Display(Name = "Summary Trigger: Number Only", Order = 104, GroupName = "Graphics")]
		public bool SummaryTriggerNumberOnly { get; set; }
		[Display(Name = "Summary Trigger: Number Font", Order = 106, GroupName = "Graphics")]
		public SimpleFont SummaryTriggerNumberFont { get; set; }
		[Display(Name = "Summary Trigger: Force Bar Blocks", Order = 108, GroupName = "Graphics")]
		[Range(0, 2147483647)]
		public int SummaryTriggerForceBarBlocks { get; set; }
		[Range(0, 2147483647)]
		[Display(Name = "Summary Trigger: Margin", Order = 110, GroupName = "Graphics")]
		public int SummaryTriggerMargin { get; set; }
		[Display(Name = "Enabled", Order = 30, GroupName = "Gradient")]
		public bool GradientEnabled { get; set; }
		[Display(Name = "Range Active: Bullish Start", Order = 32, GroupName = "Gradient")]
		[XmlIgnore]
		public global::System.Windows.Media.Brush RangeActiveBullishStart { get; set; }
		[Browsable(false)]
		public string RangeActiveBullishStart_Serialize
		{
			get
			{
				return Serialize.BrushToString(this.RangeActiveBullishStart);
			}
			set
			{
				this.RangeActiveBullishStart = Serialize.StringToBrush(value);
			}
		}
		[Display(Name = "Range Active: Bullish End", Order = 34, GroupName = "Gradient")]
		[XmlIgnore]
		public global::System.Windows.Media.Brush RangeActiveBullishEnd { get; set; }
		[Browsable(false)]
		public string RangeActiveBullishEnd_Serialize
		{
			get
			{
				return Serialize.BrushToString(this.RangeActiveBullishEnd);
			}
			set
			{
				this.RangeActiveBullishEnd = Serialize.StringToBrush(value);
			}
		}
		[XmlIgnore]
		[Display(Name = "Range Active: Bearish Start", Order = 36, GroupName = "Gradient")]
		public global::System.Windows.Media.Brush RangeActiveBearishStart { get; set; }
		[Browsable(false)]
		public string RangeActiveBearishStart_Serialize
		{
			get
			{
				return Serialize.BrushToString(this.RangeActiveBearishStart);
			}
			set
			{
				this.RangeActiveBearishStart = Serialize.StringToBrush(value);
			}
		}
		[XmlIgnore]
		[Display(Name = "Range Active: Bearish End", Order = 38, GroupName = "Gradient")]
		public global::System.Windows.Media.Brush RangeActiveBearishEnd { get; set; }
		[Browsable(false)]
		public string RangeActiveBearishEnd_Serialize
		{
			get
			{
				return Serialize.BrushToString(this.RangeActiveBearishEnd);
			}
			set
			{
				this.RangeActiveBearishEnd = Serialize.StringToBrush(value);
			}
		}
		[Display(Name = "Range Active: Line Bullish", Order = 40, GroupName = "Gradient")]
		public Stroke RangeActiveLineBullishStroke { get; set; }
		[Display(Name = "Range Active: Line Bearish", Order = 42, GroupName = "Gradient")]
		public Stroke RangeActiveLineBearishStroke { get; set; }
		[Range(0, 100)]
		[Display(Name = "Range Active: Opacity", Order = 44, GroupName = "Gradient")]
		public int RangeActiveOpacity { get; set; }
		[Display(Name = "Range Inactive: Enabled", Order = 50, GroupName = "Gradient")]
		public bool RangeInactiveEnabled { get; set; }
		[XmlIgnore]
		[Display(Name = "Range Inactive: Bullish Start", Order = 52, GroupName = "Gradient")]
		public global::System.Windows.Media.Brush RangeInactiveBullishStart { get; set; }
		[Browsable(false)]
		public string RangeInactiveBullishStart_Serialize
		{
			get
			{
				return Serialize.BrushToString(this.RangeInactiveBullishStart);
			}
			set
			{
				this.RangeInactiveBullishStart = Serialize.StringToBrush(value);
			}
		}
		[XmlIgnore]
		[Display(Name = "Range Inactive: Bullish End", Order = 54, GroupName = "Gradient")]
		public global::System.Windows.Media.Brush RangeInactiveBullishEnd { get; set; }
		[Browsable(false)]
		public string RangeInactiveBullishEnd_Serialize
		{
			get
			{
				return Serialize.BrushToString(this.RangeInactiveBullishEnd);
			}
			set
			{
				this.RangeInactiveBullishEnd = Serialize.StringToBrush(value);
			}
		}
		[Display(Name = "Range Inactive: Bearish Start", Order = 56, GroupName = "Gradient")]
		[XmlIgnore]
		public global::System.Windows.Media.Brush RangeInactiveBearishStart { get; set; }
		[Browsable(false)]
		public string RangeInactiveBearishStart_Serialize
		{
			get
			{
				return Serialize.BrushToString(this.RangeInactiveBearishStart);
			}
			set
			{
				this.RangeInactiveBearishStart = Serialize.StringToBrush(value);
			}
		}
		[Display(Name = "Range Inactive: Bearish End", Order = 58, GroupName = "Gradient")]
		[XmlIgnore]
		public global::System.Windows.Media.Brush RangeInactiveBearishEnd { get; set; }
		[Browsable(false)]
		public string RangeInactiveBearishEnd_Serialize
		{
			get
			{
				return Serialize.BrushToString(this.RangeInactiveBearishEnd);
			}
			set
			{
				this.RangeInactiveBearishEnd = Serialize.StringToBrush(value);
			}
		}
		[Display(Name = "Range Inactive: Line Bullish", Order = 60, GroupName = "Gradient")]
		public Stroke RangeInactiveLineBullishStroke { get; set; }
		[Display(Name = "Range Inactive: Line Bearish", Order = 62, GroupName = "Gradient")]
		public Stroke RangeInactiveLineBearishStroke { get; set; }
		[Display(Name = "Range Inactive: Opacity", Order = 64, GroupName = "Gradient")]
		[Range(0, 100)]
		public int RangeInactiveOpacity { get; set; }
		[Display(Name = "Volume Base", Order = 10, GroupName = "Parameters")]
		
		public DDSonarlikeIcebergFinder_VolumeBase VolumeBase { get; set; }
		[Display(Name = "Volume Ratio Strong", Order = 12, GroupName = "Parameters")]
		[Range(1, 100)]
		[NinjaScriptProperty]
		public double VolumeRatioStrong { get; set; }
		[Display(Name = "Volume Delta Period", Order = 14, GroupName = "Parameters")]
		[Range(1, 2147483647)]
		[NinjaScriptProperty]
		public int VolumeDeltaPeriod { get; set; }
		[Display(Name = "Trigger Min (Bars)", Order = 16, GroupName = "Parameters")]
		[NinjaScriptProperty]
		[Range(1, 2147483647)]
		public int TriggerMinBars { get; set; }
		[Display(Name = "Range: Unit", Order = 18, GroupName = "Parameters")]
		[RefreshProperties(RefreshProperties.All)]
		public DDSonarlikeIcebergFinder_Unit RangeUnit { get; set; }
		[Range(0.0, 1.7976931348623157E+308)]
		[NinjaScriptProperty]
		[Display(Name = "Range: Factor", Order = 20, GroupName = "Parameters")]
		public double RangeFactorATR { get; set; }
		[Display(Name = "Range: Factor", Order = 21, GroupName = "Parameters")]
		[NinjaScriptProperty]
		[Range(0.0, 1.7976931348623157E+308)]
		public double RangeFactorTicks { get; set; }
		[Display(Name = "Range: DDATR Period", Order = 22, GroupName = "Parameters")]
		[NinjaScriptProperty]
		[Range(1, 2147483647)]
		public int RangeATRPeriod { get; set; }
		[Display(Name = "Range: Finding Period (Bars)", Order = 24, GroupName = "Parameters")]
		[NinjaScriptProperty]
		[Range(1, 2147483647)]
		public int RangeFindingPeriod { get; set; }
		[Display(Name = "Range: Age Max (Bars)", Order = 26, GroupName = "Parameters")]
		[NinjaScriptProperty]
		[Range(1, 2147483647)]
		public int RangeMaxAge { get; set; }
		[Display(Name = "Range: Age Min (Bars)", Order = 28, GroupName = "Parameters")]
		[NinjaScriptProperty]
		[Range(1, 2147483647)]
		public int RangeMinAge { get; set; }
		[Display(Name = "Range: Filter Enabled", Order = 30, GroupName = "Parameters")]
		[NinjaScriptProperty]
		public bool RangeFilterEnabled { get; set; }
		[Display(Name = "Signal Split (Bars)", Order = 40, GroupName = "Parameters")]
		[NinjaScriptProperty]
		[Range(1, 2147483647)]
		public int SignalSplit { get; set; }
		[Display(Name = "Screen DPI", Order = 100, GroupName = "General")]
		[Range(99, 500)]
		public int ScreenDPI { get; set; }
		[Display(Name = "Z Order", Order = 0, GroupName = "Special")]
		public int IndicatorZOrder { get; set; }
		[Display(Name = "User Note", Order = 10, GroupName = "Special")]
		public string UserNote { get; set; }
		[XmlIgnore]
		[Browsable(false)]
		public Series<double> Signal_Trade
		{
			get
			{
				return base.Values[0];
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
				return "DDSonarlikeIceberg";
			}
		}

		
		protected override void OnStateChange()
		{
			try
			{
				if (base.State == State.SetDefaults)
				{
					base.Description = string.Empty;
					base.Name = "DDSonarlikeIcebergFinder";
					base.Calculate = Calculate.OnEachTick;
					base.IsOverlay = true;
					base.DisplayInDataBox = true;
					base.DrawOnPricePanel = true;
					base.DrawHorizontalGridLines = true;
					base.DrawVerticalGridLines = true;
					base.PaintPriceMarkers = true;
					base.ScaleJustification = ScaleJustification.Right;
					base.IsSuspendedWhileInactive = false;
					base.BarsRequiredToPlot = 0;
					base.AddPlot(Brushes.Transparent, "Signal: Trade");
					this.MarkerEnabled = true;
					this.MarkerRenderingMethod = DDSonarlikeIcebergFinder_RenderingMethod.Custom;
					this.MarkerBrushBullish = Brushes.DodgerBlue;
					this.MarkerBrushBearish = Brushes.HotPink;
					this.MarkerStringBullish = "❆ + ICE";
					this.MarkerStringBearish = "ICE + ❆";
					this.MarkerFont = new SimpleFont("Arial", 20);
					this.MarkerOffset = 10;
					this.SwitchedOn = true;
					this.VolumeBase = DDSonarlikeIcebergFinder_VolumeBase.BidAskPrice_RealVolume;
					this.VolumeRatioStrong = 3.0;
					this.VolumeDeltaPeriod = 20;
					this.TriggerMinBars = 3;
					this.RangeFactorATR = 1.0;
					this.RangeFactorTicks = 20.0;
					this.RangeUnit = DDSonarlikeIcebergFinder_Unit.DDATR;
					this.RangeATRPeriod = 14;
					this.RangeFindingPeriod = 10;
					this.RangeMaxAge = 20;
					this.RangeMinAge = 2;
					this.RangeFilterEnabled = false;
					this.SignalSplit = 10;
					this.ScreenDPI = 99;
					this.BarEnabled = true;
					this.BarRangeBullish = Brushes.Silver;
					this.BarRangeBearish = Brushes.Silver;
					this.BarTriggerBullish = Brushes.DeepSkyBlue;
					this.BarTriggerBearish = Brushes.HotPink;
					this.BarOutlineEnabled = true;
					this.BarBiasBased = true;
					this.GradientEnabled = true;
					this.RangeActiveBearishStart = Brushes.Violet;
					this.RangeActiveBearishEnd = Brushes.Transparent;
					this.RangeActiveBullishStart = Brushes.MediumSpringGreen;
					this.RangeActiveBullishEnd = Brushes.Transparent;
					this.RangeActiveOpacity = 90;
					this.RangeActiveLineBullishStroke = new Stroke(Brushes.LimeGreen, DashStyleHelper.Solid, 3f, 50);
					this.RangeActiveLineBearishStroke = new Stroke(Brushes.MediumOrchid, DashStyleHelper.Solid, 3f, 50);
					this.RangeInactiveEnabled = true;
					this.RangeInactiveBearishStart = Brushes.Violet;
					this.RangeInactiveBearishEnd = Brushes.Transparent;
					this.RangeInactiveBullishStart = Brushes.MediumSpringGreen;
					this.RangeInactiveBullishEnd = Brushes.Transparent;
					this.RangeInactiveOpacity = 70;
					this.RangeInactiveLineBullishStroke = new Stroke(Brushes.LimeGreen, DashStyleHelper.Dash, 3f, 50);
					this.RangeInactiveLineBearishStroke = new Stroke(Brushes.MediumOrchid, DashStyleHelper.Dash, 3f, 50);
					this.SummaryRangeEnabled = true;
					this.SummaryRangeDeltaPositiveBrush = Brushes.LimeGreen;
					this.SummaryRangeDeltaNegativeBrush = Brushes.HotPink;
					this.SummaryRangeDeltaNeutralBrush = Brushes.SlateGray;
					this.SummaryRangeForceBarPositiveBrush = Brushes.LimeGreen;
					this.SummaryRangeForceBarNegativeBrush = Brushes.HotPink;
					this.SummaryRangeNumberOnly = false;
					this.SummaryRangeNumberFont = new SimpleFont("Arial", 10)
					{
						Bold = false
					};
					this.SummaryRangeHeight = 4;
					this.SummaryRangeMargin = 4;
					this.SummaryTriggerEnabled = true;
					this.SummaryTriggerBuyVolumeBrush = Brushes.DeepSkyBlue;
					this.SummaryTriggerBuyVolumeString = "∑ = ";
					this.SummaryTriggerSellVolumeBrush = Brushes.HotPink;
					this.SummaryTriggerSellVolumeString = "∑ = ";
					this.SummaryTriggerBuyForceBarBrush = Brushes.DeepSkyBlue;
					this.SummaryTriggerSellForceBarBrush = Brushes.HotPink;
					this.SummaryTriggerForceBarBlocks = 10;
					this.SummaryTriggerNumberOnly = false;
					this.SummaryTriggerNumberFont = new SimpleFont("Arial", 12)
					{
						Bold = false
					};
					this.SummaryTriggerMargin = 8;
					this.IndicatorZOrder = -1;
					this.UserNote = "instrument (period)";
				}
				else if (base.State == State.Configure)
				{
					this.dictPresentations = new Dictionary<int, DDSonarlikeIcebergFinder.Presentation>();
					this.queuePresentationCell = new Queue<DDSonarlikeIcebergFinder.PresentationCell>();
					this.queueCurrentBar1 = new Queue<int>();
					this.listStartBarIndex1 = new List<int>();
					this.dictPresentations.Add(0, new DDSonarlikeIcebergFinder.Presentation());
					base.AddDataSeries(BarsPeriodType.Tick, 1);
					this.ComputeUnit();
					this.isUnitATR = this.RangeUnit == DDSonarlikeIcebergFinder_Unit.DDATR;
					double num = (this.isUnitATR ? this.RangeFactorATR : this.RangeFactorTicks);
					this.OffsetMultiplierTrend = num / 2.0;
					this.seriesSignalTrend = new Series<int>(this, MaximumBarsLookBack.Infinite);
					this.seriesTrendVector = new Series<double>(this, MaximumBarsLookBack.Infinite);
					this.seriesTrailingStop = new Series<double>(this, MaximumBarsLookBack.Infinite);
					this.solarWaveInfo = new DDSonarlikeIcebergFinder.SolarWaveInfo(num, this.OffsetMultiplierTrend, 0, 5, 10, this.seriesTrailingStop, this.seriesTrendVector, this.seriesSignalTrend);
					this.VolumePeriod = (int)((float)this.VolumeDeltaPeriod * 1.5f);
					this.RangeWithoutGroupEnabled = !this.RangeFilterEnabled;
					this.isUpDownTickMode = this.VolumeBase > DDSonarlikeIcebergFinder_VolumeBase.BidAskPrice_RealVolume;
					this.isMarkerCustomRenderingMethod = this.MarkerRenderingMethod == DDSonarlikeIcebergFinder_RenderingMethod.Custom;
					if (this.isMarkerCustomRenderingMethod)
					{
						this.dictMarkers = new Dictionary<int, DDSonarlikeIcebergFinder.MarkerInfo>();
					}
					this.originalDrawOnPricePanel = base.DrawOnPricePanel;
					this.seriesRawBuyVolume = new Series<double>(this, MaximumBarsLookBack.Infinite);
					this.seriesRawSellVolume = new Series<double>(this, MaximumBarsLookBack.Infinite);
					this.volumeBuy = new Series<double>(this, MaximumBarsLookBack.Infinite);
					this.volumeSell = new Series<double>(this, MaximumBarsLookBack.Infinite);
					this.volumeBuyAvg = new Series<double>(this, MaximumBarsLookBack.Infinite);
					this.volumeSellAvg = new Series<double>(this, MaximumBarsLookBack.Infinite);
					this.thresholdPositiveStrong = new Series<double>(this, MaximumBarsLookBack.Infinite);
					this.thresholdPositiveModerate = new Series<double>(this, MaximumBarsLookBack.Infinite);
					this.thresholdNegativeModerate = new Series<double>(this, MaximumBarsLookBack.Infinite);
					this.thresholdNegativeStrong = new Series<double>(this, MaximumBarsLookBack.Infinite);
					this.seriesSignalState = new Series<double>(this, MaximumBarsLookBack.Infinite);
					this.seriesBarState = new Series<int>(this, MaximumBarsLookBack.Infinite);
					this.seriesDiffHighLow = new Series<double>(this, MaximumBarsLookBack.Infinite);
					this.listRawPositiveDelta = new List<double>();
					this.listRawNegativeDelta = new List<double>();
					this.listRangeInfoActive = new SortedList<int, DDSonarlikeIcebergFinder.RangeInfo>();
					this.listRangeInfoInactive = new SortedList<int, DDSonarlikeIcebergFinder.RangeInfo>();
					this.listTriggerBarInfo = new List<DDSonarlikeIcebergFinder.TriggerBarInfo>();
					if (this.GradientEnabled)
					{
						this.rangeActiveBottomGradientStop = this.CreateGradientStopArr(this.RangeActiveBullishStart, this.RangeActiveBullishEnd, this.RangeActiveOpacity);
						this.rangeActiveTopGradientStop = this.CreateGradientStopArr(this.RangeActiveBearishStart, this.RangeActiveBearishEnd, this.RangeActiveOpacity);
						this.rangeInactiveBottomGradientStop = this.CreateGradientStopArr(this.RangeInactiveBullishStart, this.RangeInactiveBullishEnd, this.RangeInactiveOpacity);
						this.rangeInactiveTopGradientStop = this.CreateGradientStopArr(this.RangeInactiveBearishStart, this.RangeInactiveBearishEnd, this.RangeInactiveOpacity);
					}
					this.rangeActiveBearish = DD_BrushManager.CreateOpacityBrush(this.RangeActiveBearishStart, this.RangeActiveOpacity);
					this.rangeActiveBullish = DD_BrushManager.CreateOpacityBrush(this.RangeActiveBullishStart, this.RangeActiveOpacity);
					this.rangeInactiveBearish = DD_BrushManager.CreateOpacityBrush(this.RangeInactiveBearishStart, this.RangeInactiveOpacity);
					this.rangeInactiveBullish = DD_BrushManager.CreateOpacityBrush(this.RangeInactiveBullishStart, this.RangeInactiveOpacity);
					this.summaryRangeForceBarPositiveActive = DD_BrushManager.CreateOpacityBrush(this.SummaryRangeForceBarPositiveBrush, this.RangeActiveOpacity);
					this.summaryRangeForceBarNegativeActive = DD_BrushManager.CreateOpacityBrush(this.SummaryRangeForceBarNegativeBrush, this.RangeActiveOpacity);
					this.summaryRangeForceBarPositiveInactive = DD_BrushManager.CreateOpacityBrush(this.SummaryRangeForceBarPositiveBrush, this.RangeInactiveOpacity);
					this.summaryRangeForceBarNegativeInactive = DD_BrushManager.CreateOpacityBrush(this.SummaryRangeForceBarNegativeBrush, this.RangeInactiveOpacity);
					this.summaryRangeDeltaPositiveActive = DD_BrushManager.CreateOpacityBrush(this.SummaryRangeDeltaPositiveBrush, this.RangeActiveOpacity);
					this.summaryRangeDeltaNegativeActive = DD_BrushManager.CreateOpacityBrush(this.SummaryRangeDeltaNegativeBrush, this.RangeActiveOpacity);
					this.summaryRangeDeltaNeutralActive = DD_BrushManager.CreateOpacityBrush(this.SummaryRangeDeltaNeutralBrush, this.RangeActiveOpacity);
					this.summaryRangeDeltaPositiveInactive = DD_BrushManager.CreateOpacityBrush(this.SummaryRangeDeltaPositiveBrush, this.RangeInactiveOpacity);
					this.summaryRangeDeltaNegativeInactive = DD_BrushManager.CreateOpacityBrush(this.SummaryRangeDeltaNegativeBrush, this.RangeInactiveOpacity);
					this.summaryRangeDeltaNeutralInactive = DD_BrushManager.CreateOpacityBrush(this.SummaryRangeDeltaNeutralBrush, this.RangeInactiveOpacity);
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
						base.Calculate = Calculate.OnBarClose;
						this.isCharting = base.ChartControl != null;
				}
			}
			catch
			{
			}
		}
		
		private global::SharpDX.Direct2D1.GradientStop[] CreateGradientStopArr(global::System.Windows.Media.Brush brushStart, global::System.Windows.Media.Brush brushEnd, int opacity)
		{
			if (brushStart.IsTransparent() && brushEnd.IsTransparent())
			{
				return null;
			}
			global::System.Windows.Media.SolidColorBrush solidColorBrush = (global::System.Windows.Media.SolidColorBrush)DD_BrushManager.CreateOpacityBrush(brushStart, opacity);
			Color4 color = new Color4((float)solidColorBrush.Color.R / 255f, (float)solidColorBrush.Color.G / 255f, (float)solidColorBrush.Color.B / 255f, (float)solidColorBrush.Color.A / 255f);
			global::System.Windows.Media.SolidColorBrush solidColorBrush2 = (global::System.Windows.Media.SolidColorBrush)DD_BrushManager.CreateOpacityBrush(brushEnd, opacity);
			Color4 color2 = new Color4((float)solidColorBrush2.Color.R / 255f, (float)solidColorBrush2.Color.G / 255f, (float)solidColorBrush2.Color.B / 255f, (float)solidColorBrush2.Color.A / 255f);
			return new global::SharpDX.Direct2D1.GradientStop[]
			{
				new global::SharpDX.Direct2D1.GradientStop
				{
					Position = -0.2f,
					Color = color
				},
				new global::SharpDX.Direct2D1.GradientStop
				{
					Position = 0.5f,
					Color = color2
				},
				new global::SharpDX.Direct2D1.GradientStop
				{
					Position = 1.2f,
					Color = color
				}
			};
		}
		
		protected override void OnBarUpdate()
		{
			try
			{
					this.ComputeSolarWave(this.solarWaveInfo);
					this.ComputeVolumes();
					if (base.BarsInProgress == 0)
					{
						this.signalTrade = 0;
						this.ComputeData();
						this.DoPlotJobs();
						this.FilterTriggerBarActive(this.listTriggerBarInfo);
						this.FindRangeInfo();
						this.CheckBrokenAndFindSignal();
						this.FindAndInactiveTriggerBar();
						this.Signal_Trade[0] = (double)this.signalTrade;
					}
			}
			catch (Exception ex)
			{
				this.PrintException(ex);
			}
		}
		
		private void CheckBrokenAndFindSignal()
		{
			if (this.lastRangeInfo == null)
			{
				return;
			}
			int priceState = this.lastRangeInfo.GetPriceState(base.Closes[0][0]);
			if (priceState != 0)
			{
				DDSonarlikeIcebergFinder.Presentation presentation = this.dictPresentations[base.CurrentBars[0]];
				this.lastRangeInfo.BarEnd = base.CurrentBars[0];
				this.lastRangeInfo.VolumeBuy += (long)presentation.VolBuy;
				this.lastRangeInfo.VolumeSell += (long)presentation.VolSell;
				bool flag;
				if (this.lastRangeInfo.IsBullish)
				{
					flag = this.bullishBarIndex == -1 || base.CurrentBars[0] - this.bullishBarIndex > this.SignalSplit;
				}
				else
				{
					flag = this.bearishBarIndex == -1 || base.CurrentBars[0] - this.bearishBarIndex > this.SignalSplit;
				}
				if (flag)
				{
					if (this.isMarkerCustomRenderingMethod)
					{
						this.AddMarker(base.CurrentBars[0], this.lastRangeInfo.IsBullish);
					}
					else
					{
						this.PrintMarker(this.lastRangeInfo.IsBullish);
					}
					if (this.lastRangeInfo.IsBullish)
					{
						this.bullishBarIndex = base.CurrentBars[0];
					}
					else
					{
						this.bearishBarIndex = base.CurrentBars[0];
					}
					this.seriesBarState[0] = 3 * this.lastRangeInfo.Sign;
					this.PaintOneBar(false, this.seriesBarState[0], base.CurrentBars[0]);
					this.signalTrade = priceState;
				}
				else
				{
					this.seriesBarState[0] = this.lastRangeInfo.Sign;
					this.PaintOneBar(false, this.seriesBarState[0], base.CurrentBars[0]);
				}
				this.MoveDataFromListToList<DDSonarlikeIcebergFinder.RangeInfo>(this.lastRangeInfo.BarStart, this.lastRangeInfo, this.listRangeInfoActive, this.listRangeInfoInactive);
				this.lastRangeInfo = null;
				this.triggerBarInfo = null;
				return;
			}
			bool flag2 = this.solarWaveInfo.SeriesTrendVector[0].ApproxCompare(this.solarWaveInfo.SeriesTrendVector[1]) != 0;
			bool flag3 = base.CurrentBars[0] - this.lastRangeInfo.BarStart >= this.RangeMaxAge;
			if (flag2 || flag3)
			{
				this.lastRangeInfo.BarEnd = base.CurrentBars[0];
				this.MoveDataFromListToList<DDSonarlikeIcebergFinder.RangeInfo>(this.lastRangeInfo.BarStart, this.lastRangeInfo, this.listRangeInfoActive, this.listRangeInfoInactive);
				this.lastRangeInfo = null;
				return;
			}
			this.UpdateRange(this.lastRangeInfo);
		}
		
		private void UpdateRange(DDSonarlikeIcebergFinder.RangeInfo rangeInfo)
		{
			if (rangeInfo == null)
			{
				return;
			}
			DDSonarlikeIcebergFinder.Presentation presentation = this.dictPresentations[base.CurrentBars[0]];
			this.lastRangeInfo.BarEnd = base.CurrentBars[0];
			this.lastRangeInfo.VolumeBuy += (long)presentation.VolBuy;
			this.lastRangeInfo.VolumeSell += (long)presentation.VolSell;
			if (rangeInfo.IsBullish)
			{
				rangeInfo.PriceBottom = Math.Min(rangeInfo.PriceBottom, base.Lows[0][0]);
			}
			else
			{
				rangeInfo.PriceTop = Math.Max(rangeInfo.PriceTop, base.Highs[0][0]);
			}
			this.seriesBarState[0] = rangeInfo.Sign;
			this.PaintOneBar(false, this.seriesBarState[0], base.CurrentBars[0]);
		}
		
		private void FindRangeInfo()
		{
			if (this.triggerBarInfo == null || this.solarWaveInfo == null || this.lastRangeInfo != null)
			{
				return;
			}
			for (int i = 0; i < this.RangeMinAge - 1; i++)
			{
				if (this.solarWaveInfo.SeriesTrendVector[i].ApproxCompare(this.solarWaveInfo.SeriesTrendVector[i + 1]) != 0)
				{
					return;
				}
			}
			if (base.CurrentBars[0] - this.triggerBarInfo.BarEnd == 0)
			{
				return;
			}
			double num = this.solarWaveInfo.SeriesTrendVector[0];
			double num2 = this.solarWaveInfo.SeriesTrailingStop[0];
			double num3 = num - num2;
			double num4 = (this.solarWaveInfo.IsUptrend ? (num + num3) : num2);
			double num5 = (this.solarWaveInfo.IsUptrend ? num2 : (num + num3));
			for (int j = 0; j <= this.RangeMinAge - 1; j++)
			{
				if (this.triggerBarInfo.IsBullish)
				{
					num4 = Math.Max(num4, base.Highs[0][j]);
				}
				else
				{
					num5 = Math.Min(num4, base.Lows[0][j]);
				}
			}
			if (!this.IsTriggerBarInRange(num4, num5, this.triggerBarInfo))
			{
				if (this.listTriggerBarInfo.Count == 0)
				{
					return;
				}
				for (int k = this.listTriggerBarInfo.Count - 1; k >= 0; k--)
				{
					DDSonarlikeIcebergFinder.TriggerBarInfo triggerBarInfo = this.listTriggerBarInfo[k];
					if (triggerBarInfo.IsBullish == this.triggerBarInfo.IsBullish)
					{
						if (this.IsTriggerBarInRange(num4, num5, triggerBarInfo))
						{
							break;
						}
						if (k == 0)
						{
							return;
						}
					}
				}
			}
			this.lastRangeInfo = new DDSonarlikeIcebergFinder.RangeInfo(this.triggerBarInfo, base.CurrentBars[0] - (this.RangeMinAge - 1), base.CurrentBars[0], 0.0, 0.0, !this.triggerBarInfo.IsBullish);
			this.lastRangeInfo.UpdateTriggerBarCollection(this.listTriggerBarInfo, base.CurrentBars[0]);
			this.lastRangeInfo.PriceTop = num4;
			this.lastRangeInfo.PriceBottom = num5;
			this.lastRangeInfo.TriggerBarIndex = Math.Min(this.triggerBarInfo.BarEnd, this.lastRangeInfo.BarStart);
			for (int l = this.lastRangeInfo.BarStart; l <= this.lastRangeInfo.BarEnd; l++)
			{
				DDSonarlikeIcebergFinder.Presentation presentation = this.dictPresentations[base.CurrentBars[0]];
				this.lastRangeInfo.VolumeBuy += (long)presentation.VolBuy;
				this.lastRangeInfo.VolumeSell += (long)presentation.VolSell;
			}
			this.triggerBarInfo.HasRange = true;
			this.UpdateBarStatePaintBars(NinjaTrader.NinjaScript.Indicators.DimDim.DDSonarlikeIcebergFinder.BarStateType.TriggerBar, this.triggerBarInfo.BarStart, this.triggerBarInfo.BarEnd, this.triggerBarInfo.IsBullish);
			this.UpdateBarStatePaintBars(NinjaTrader.NinjaScript.Indicators.DimDim.DDSonarlikeIcebergFinder.BarStateType.Range, this.lastRangeInfo.BarStart, this.lastRangeInfo.BarEnd, this.lastRangeInfo.IsBullish);
			this.AddDataToList<DDSonarlikeIcebergFinder.RangeInfo>(this.lastRangeInfo.BarStart, this.lastRangeInfo, this.listRangeInfoActive);
		}
		
		private bool IsTriggerBarInRange(double priceTop, double priceBottom, DDSonarlikeIcebergFinder.TriggerBarInfo triggerBarInfo)
		{
			return triggerBarInfo != null && ((this.RangeWithoutGroupEnabled && triggerBarInfo.IsGroup) || (triggerBarInfo.Highest.ApproxCompare(priceBottom) >= 0 && triggerBarInfo.Lowest.ApproxCompare(priceTop) <= 0));
		}
		
		private void ComputeDiffHighLow()
		{
			double num = base.Highs[0][0] - base.Lows[0][0];
			this.seriesDiffHighLow[0] = num;
			if (base.CurrentBars[0] < 1)
			{
				return;
			}
			this.sumDiffHighLow = this.sumDiffHighLow + this.seriesDiffHighLow[1] - ((base.CurrentBars[0] > 10) ? this.seriesDiffHighLow[11] : 0.0);
			double num2 = this.sumDiffHighLow / (double)((base.CurrentBars[0] <= 10) ? base.CurrentBars[0] : 10);
			if (num.ApproxCompare(num2) >= 0)
			{
				this.lastDiffBarIndex = base.CurrentBars[0];
			}
		}
		
		private void FindAndInactiveTriggerBar()
		{
			int num = (int)this.seriesSignalState[0];
			int num2 = (this.solarWaveInfo.IsUptrend ? 1 : (-1));
			int num3 = base.CurrentBars[0];
			double num4 = base.Closes[0][0];
			double num5 = base.Opens[0][0];
			double num6 = base.Highs[0][0];
			double num7 = base.Lows[0][0];
			this.ComputeDiffHighLow();
			if (this.triggerBarInfo != null && this.triggerBarInfo.SearchLimitReached(this.RangeFindingPeriod, num3))
			{
				this.triggerBarInfo = null;
			}
			if (Math.Abs(num) < 2 || num * num2 < 0)
			{
				return;
			}
			DDSonarlikeIcebergFinder.Presentation presentation = this.dictPresentations[num3];
			if (this.triggerBarInfo != null && this.triggerBarInfo.BarEnd == num3 - 1 && num2 * this.triggerBarInfo.Sign > 0)
			{
				this.triggerBarInfo.Update(false, num3, num6, num7, Math.Max(num4, num5), Math.Min(num4, num5), presentation.VolDelta, presentation.VolBuy, presentation.VolSell, (this.triggerBarInfo.IsBullish ? this.volumeBuyAvg : this.volumeSellAvg)[0]);
				if (this.triggerBarInfo.HasRange && Math.Abs(this.seriesBarState[0]) != 1)
				{
					this.seriesBarState[0] = 2 * this.triggerBarInfo.Sign;
					this.PaintOneBar(false, this.seriesBarState[0], num3);
					return;
				}
			}
			else
			{
				DDSonarlikeIcebergFinder.TriggerBarInfo triggerBarInfo = new DDSonarlikeIcebergFinder.TriggerBarInfo(num3, num3, this.solarWaveInfo.IsUptrend, new List<int> { num3 }, presentation.VolDelta, presentation.VolBuy, presentation.VolSell, (this.solarWaveInfo.IsUptrend ? this.volumeBuyAvg : this.volumeSellAvg)[0], num6, num7, Math.Max(num4, num5), Math.Min(num4, num5));
				bool flag = num3 == this.lastDiffBarIndex;
				if (Math.Abs(num) == 3)
				{
					for (int i = 1; i < Math.Min(10, num3 - 1); i++)
					{
						int num8 = (int)this.seriesSignalState[i];
						if (Math.Abs(num8) < 2 || num8 * num2 < 0)
						{
							break;
						}
						if (!flag)
						{
							flag = num3 - i == this.lastDiffBarIndex;
						}
						DDSonarlikeIcebergFinder.Presentation presentation2 = this.dictPresentations[num3 - i];
						triggerBarInfo.Update(true, num3 - i, base.Highs[0][i], base.Lows[0][i], Math.Max(base.Closes[0][i], base.Opens[0][i]), Math.Min(base.Closes[0][i], base.Opens[0][i]), presentation2.VolDelta, presentation2.VolBuy, presentation2.VolSell, (triggerBarInfo.IsBullish ? this.volumeBuyAvg : this.volumeSellAvg)[i]);
					}
					if (!flag)
					{
						return;
					}
					this.BackupTriggerBarActive(this.listTriggerBarInfo, this.triggerBarInfo);
					this.triggerBarInfo = triggerBarInfo;
					return;
				}
				else
				{
					for (int j = 1; j < Math.Min(10, num3 - 1); j++)
					{
						int num9 = (int)this.seriesSignalState[j];
						if (Math.Abs(num9) < 2 || num9 * num2 < 0)
						{
							break;
						}
						if (!flag)
						{
							flag = num3 - j == this.lastDiffBarIndex;
						}
						DDSonarlikeIcebergFinder.Presentation presentation3 = this.dictPresentations[num3 - j];
						triggerBarInfo.Update(true, num3 - j, base.Highs[0][j], base.Lows[0][j], Math.Max(base.Closes[0][j], base.Opens[0][j]), Math.Min(base.Closes[0][j], base.Opens[0][j]), presentation3.VolDelta, presentation3.VolBuy, presentation3.VolSell, (triggerBarInfo.IsBullish ? this.volumeBuyAvg : this.volumeSellAvg)[j]);
					}
					if (triggerBarInfo.ListBarIndex.Count < this.TriggerMinBars || !flag)
					{
						return;
					}
					double num10 = (triggerBarInfo.IsBullish ? this.thresholdPositiveStrong[0] : this.thresholdNegativeStrong[0]);
					if ((triggerBarInfo.VolumeDelta - num10) * (double)triggerBarInfo.Sign <= 0.0)
					{
						return;
					}
					this.BackupTriggerBarActive(this.listTriggerBarInfo, this.triggerBarInfo);
					this.triggerBarInfo = triggerBarInfo;
				}
			}
		}
		
		private void BackupTriggerBarActive(List<DDSonarlikeIcebergFinder.TriggerBarInfo> listItem, DDSonarlikeIcebergFinder.TriggerBarInfo triggerBarInfo)
		{
			if (triggerBarInfo == null)
			{
				return;
			}
			if (!triggerBarInfo.SearchLimitReached(this.RangeFindingPeriod, base.CurrentBars[0]))
			{
				listItem.Add(triggerBarInfo);
			}
		}
		
		private void FilterTriggerBarActive(List<DDSonarlikeIcebergFinder.TriggerBarInfo> listTriggerBar)
		{
			int count = listTriggerBar.Count;
			if (count == 0)
			{
				return;
			}
			bool flag = false;
			for (int i = count - 1; i >= 0; i--)
			{
				DDSonarlikeIcebergFinder.TriggerBarInfo triggerBarInfo = listTriggerBar[i];
				if (flag)
				{
					listTriggerBar.RemoveAt(i);
				}
				else if (triggerBarInfo.SearchLimitReached(this.RangeFindingPeriod, base.CurrentBars[0]))
				{
					flag = true;
					listTriggerBar.RemoveAt(i);
				}
			}
		}
		
		private void ComputeSolarWave(DDSonarlikeIcebergFinder.SolarWaveInfo solarWaveInfo)
		{
			if (base.BarsInProgress != 0)
			{
				return;
			}
			bool flag = false;
			double num = base.Opens[0][0];
			double num2 = base.Closes[0][0];
			double num3;
			double num4;
			if (this.isDDRenkoOrKingRenkoBarType)
			{
				num3 = num2;
				num4 = num2;
			}
			else
			{
				double num5 = base.MAX(base.Highs[0], 2)[0];
				double num6 = base.MIN(base.Lows[0], 2)[0];
				double num7 = (base.MAX(base.Closes[0], 2)[0] + base.MIN(base.Closes[0], 2)[0]) / 2.0;
				num3 = (num5 + num6 + 1.0 * num7) / 3.0;
				num4 = num3;
			}
			double num8 = (this.isUnitATR ? base.DDATR(this.RangeATRPeriod)[0] : base.TickSize);
			double num9 = base.Instrument.MasterInstrument.RoundToTickSize(solarWaveInfo.OffsetBase * num8);
			double num10 = base.Instrument.MasterInstrument.RoundToTickSize(num3 - num9);
			double num11 = base.Instrument.MasterInstrument.RoundToTickSize(num4 + num9);
			double num12 = base.Instrument.MasterInstrument.RoundToTickSize(solarWaveInfo.OffsetLevel * num8);
			double num13 = num3 - num12;
			double num14 = num4 + num12;
			if (base.CurrentBars[0] != 0)
			{
				if (solarWaveInfo.IsUptrend)
				{
					if (num2.ApproxCompare(solarWaveInfo.StopCurrentValue) < 0)
					{
						solarWaveInfo.IsUptrend = false;
						if (num14.ApproxCompare(solarWaveInfo.SeriesTrendVector[1]) < 0)
						{
							solarWaveInfo.SeriesTrendVector[0] = num14;
						}
						else
						{
							solarWaveInfo.SeriesTrendVector[0] = solarWaveInfo.SeriesTrendVector[1];
							flag = true;
						}
						solarWaveInfo.NextWeakTrendBar = base.CurrentBars[0] + solarWaveInfo.WeakWeakSplit;
						if (solarWaveInfo.CountSlowdown != 0)
						{
							solarWaveInfo.CountSlowdown = 0;
						}
						solarWaveInfo.SeriesTrailingStop[0] = (solarWaveInfo.StopCurrentValue = num11);
					}
					else
					{
						if (num13.ApproxCompare(solarWaveInfo.SeriesTrendVector[1]) > 0)
						{
							solarWaveInfo.SeriesTrendVector[0] = num13;
							if (solarWaveInfo.CountSlowdown != 0)
							{
								solarWaveInfo.CountSlowdown = 0;
							}
						}
						else
						{
							int num15 = solarWaveInfo.CountSlowdown;
							solarWaveInfo.CountSlowdown = num15 + 1;
							if (solarWaveInfo.CountSlowdown < solarWaveInfo.SlowdownScan || base.CurrentBars[0] < solarWaveInfo.NextWeakTrendBar)
							{
								flag = true;
							}
							solarWaveInfo.SeriesTrendVector[0] = solarWaveInfo.SeriesTrendVector[1];
						}
						solarWaveInfo.SeriesTrailingStop[0] = (solarWaveInfo.StopCurrentValue = Math.Max(num10, solarWaveInfo.StopCurrentValue));
					}
				}
				else if (num2.ApproxCompare(solarWaveInfo.StopCurrentValue) > 0)
				{
					solarWaveInfo.IsUptrend = true;
					if (num13.ApproxCompare(solarWaveInfo.SeriesTrendVector[1]) > 0)
					{
						solarWaveInfo.SeriesTrendVector[0] = num13;
					}
					else
					{
						solarWaveInfo.SeriesTrendVector[0] = solarWaveInfo.SeriesTrendVector[1];
						flag = true;
					}
					solarWaveInfo.NextWeakTrendBar = base.CurrentBars[0] + solarWaveInfo.WeakWeakSplit;
					if (solarWaveInfo.CountSlowdown != 0)
					{
						solarWaveInfo.CountSlowdown = 0;
					}
					solarWaveInfo.SeriesTrailingStop[0] = (solarWaveInfo.StopCurrentValue = num10);
				}
				else
				{
					if (num14.ApproxCompare(solarWaveInfo.SeriesTrendVector[1]) < 0)
					{
						solarWaveInfo.SeriesTrendVector[0] = num14;
						if (solarWaveInfo.CountSlowdown != 0)
						{
							solarWaveInfo.CountSlowdown = 0;
						}
					}
					else
					{
						int num15 = solarWaveInfo.CountSlowdown;
						solarWaveInfo.CountSlowdown = num15 + 1;
						if (solarWaveInfo.CountSlowdown < solarWaveInfo.SlowdownScan || base.CurrentBars[0] < solarWaveInfo.NextWeakTrendBar)
						{
							flag = true;
						}
						solarWaveInfo.SeriesTrendVector[0] = solarWaveInfo.SeriesTrendVector[1];
					}
					solarWaveInfo.SeriesTrailingStop[0] = (solarWaveInfo.StopCurrentValue = Math.Min(num11, solarWaveInfo.StopCurrentValue));
				}
				int num16 = (solarWaveInfo.IsUptrend ? 1 : (-1));
				int num17;
				if (solarWaveInfo.SeriesTrendVector[0].ApproxCompare(solarWaveInfo.SeriesTrendVector[1]) == 0)
				{
					num17 = (flag ? (num16 * 2) : num16);
				}
				else
				{
					num17 = num16 * 2;
				}
				solarWaveInfo.SeriesSignalTrend[0] = num17;
				return;
			}
			solarWaveInfo.IsUptrend = num2.ApproxCompare(num) > 0;
			if (solarWaveInfo.IsUptrend)
			{
				solarWaveInfo.SeriesTrendVector[0] = num13;
				solarWaveInfo.SeriesTrailingStop[0] = (solarWaveInfo.StopCurrentValue = num10);
				return;
			}
			solarWaveInfo.SeriesTrendVector[0] = num14;
			solarWaveInfo.SeriesTrailingStop[0] = (solarWaveInfo.StopCurrentValue = num11);
		}
		
		private int BarBias()
		{
			bool flag = base.Close[0].ApproxCompare(base.Open[0]) > 0;
			double num = (base.High[0] - base.Low[0]) * 40.0 / 100.0;
			double num2 = base.Median[0] + num / 2.0;
			bool flag2 = base.Close[0].ApproxCompare(num2) > 0;
			bool flag3 = base.Close[0].ApproxCompare(base.Open[0]) < 0;
			double num3 = base.Median[0] - num / 2.0;
			bool flag4 = base.Close[0].ApproxCompare(num3) < 0;
			if (flag && flag2)
			{
				return 1;
			}
			if (flag3 && flag4)
			{
				return -1;
			}
			return 0;
		}
		
		private void ComputeData()
		{
			bool flag = base.CurrentBars[0] == 0;
			int num = base.CurrentBars[0];
			DDSonarlikeIcebergFinder.Presentation presentation = this.dictPresentations[num];
			if (presentation.VolDelta.ApproxCompare(0.0) > 0)
			{
				this.volumeBuy[0] = presentation.VolBuy;
				this.volumeSellAvg[0] = (flag ? 0.0 : this.volumeSellAvg[1]);
				this.listRawPositiveDelta.Add(presentation.VolDelta);
				this.thresholdPositiveModerate[0] = this.ComputeSMA(ref this.sumPositiveDelta, this.listRawPositiveDelta, this.VolumeDeltaPeriod);
				this.thresholdPositiveStrong[0] = this.thresholdPositiveModerate[0] * this.VolumeRatioStrong;
				this.thresholdNegativeModerate[0] = (flag ? 0.0 : this.thresholdNegativeModerate[1]);
				this.thresholdNegativeStrong[0] = (flag ? 0.0 : this.thresholdNegativeStrong[1]);
			}
			else if (presentation.VolDelta.ApproxCompare(0.0) < 0)
			{
				this.volumeSell[0] = -presentation.VolSell;
				this.volumeBuyAvg[0] = (flag ? 0.0 : this.volumeBuyAvg[1]);
				this.listRawNegativeDelta.Add(presentation.VolDelta);
				this.thresholdNegativeModerate[0] = this.ComputeSMA(ref this.sumNegativeDelta, this.listRawNegativeDelta, this.VolumeDeltaPeriod);
				this.thresholdNegativeStrong[0] = this.thresholdNegativeModerate[0] * this.VolumeRatioStrong;
				this.thresholdPositiveModerate[0] = (flag ? 0.0 : this.thresholdPositiveModerate[1]);
				this.thresholdPositiveStrong[0] = (flag ? 0.0 : this.thresholdPositiveStrong[1]);
			}
			else
			{
				this.thresholdNegativeModerate[0] = (flag ? 0.0 : this.thresholdNegativeModerate[1]);
				this.thresholdNegativeStrong[0] = (flag ? 0.0 : this.thresholdNegativeStrong[1]);
				this.thresholdPositiveModerate[0] = (flag ? 0.0 : this.thresholdPositiveModerate[1]);
				this.thresholdPositiveStrong[0] = (flag ? 0.0 : this.thresholdPositiveStrong[1]);
			}
			this.seriesRawBuyVolume[0] = presentation.VolBuy;
			this.seriesRawSellVolume[0] = -presentation.VolSell;
			this.volumeBuyAvg[0] = base.SMA(this.seriesRawBuyVolume, this.VolumePeriod)[0];
			this.volumeSellAvg[0] = base.SMA(this.seriesRawSellVolume, this.VolumePeriod)[0];
		}
		
		private double ComputeSMA(ref double sum, List<double> inputList, int period)
		{
			int count = inputList.Count;
			sum = sum + inputList[count - 1] - ((count > period) ? inputList[count - period - 1] : 0.0);
			return sum / (double)((count < period) ? count : period);
		}
		
		private void DoPlotJobs()
		{
			int num = base.CurrentBars[0];
			if (this.dictPresentations.ContainsKey(num))
			{
				DDSonarlikeIcebergFinder.Presentation presentation = this.dictPresentations[num];
				long num2 = (long)presentation.VolDelta;
				double volBuy = presentation.VolBuy;
				double volSell = presentation.VolSell;
				int num3;
				if (num2 > 0L)
				{
					num3 = 1;
				}
				else if (num2 == 0L)
				{
					num3 = 0;
				}
				else
				{
					num3 = -1;
				}
				this.BarBias();
				bool flag = true;
				bool flag2 = true;
				if ((double)num2 >= this.thresholdPositiveStrong[0] && flag)
				{
					num3 = 3;
				}
				else if ((double)num2 >= this.thresholdPositiveModerate[0] && flag)
				{
					num3 = 2;
				}
				if ((double)num2 <= this.thresholdNegativeStrong[0] && flag2)
				{
					num3 = -3;
				}
				else if ((double)num2 <= this.thresholdNegativeModerate[0] && flag2)
				{
					num3 = -2;
				}
				this.seriesSignalState[0] = (double)num3;
			}
		}
		
		protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
		{
			try
			{
				if (this.isCharting && this.SwitchedOn)
				{
					if (!base.IsInHitTest)
					{
						base.OnRender(chartControl, chartScale);
						this.barDistance = base.ChartControl.Properties.BarDistance;
						this.DrawRanges(chartControl, chartScale, this.listRangeInfoActive, true);
						this.DrawRanges(chartControl, chartScale, this.listRangeInfoInactive, false);
						if (this.isMarkerCustomRenderingMethod)
						{
							this.DrawMarkers(chartScale);
						}
					}
				}
			}
			catch (Exception ex)
			{
				this.PrintException(ex);
			}
		}
		
		private void DrawTriggerSummary(ChartControl chartControl, ChartScale chartScale, DDSonarlikeIcebergFinder.RangeInfo rangeInfo)
		{
			if (rangeInfo == null || rangeInfo.TriggerBar == null)
			{
				return;
			}
			if (this.SummaryTriggerEnabled)
			{
				DDSonarlikeIcebergFinder.TriggerBarInfo triggerBar = rangeInfo.TriggerBar;
				global::System.Windows.Media.Brush brush = (triggerBar.IsBullish ? this.SummaryTriggerBuyForceBarBrush : this.SummaryTriggerSellForceBarBrush);
				int num = Math.Max(0, triggerBar.BarEnd - this.SummaryTriggerForceBarBlocks);
				float num2 = (float)(chartScale.GetYByValue(triggerBar.IsBullish ? triggerBar.Highest : triggerBar.Lowest) - triggerBar.Sign * this.SummaryTriggerMargin);
				float num3 = (float)chartControl.GetXByBarIndex(base.ChartBars, num);
				float num4 = (float)chartControl.GetXByBarIndex(base.ChartBars, triggerBar.BarEnd);
				Vector2 vector = new Vector2(num3, num2 + 2f);
				global::SharpDX.Direct2D1.Brush brush2 = brush.ToDxBrush(base.RenderTarget);
				float num5 = this.barDistance * 0.7f;
				float num6 = num4 - num3;
				float num7 = this.barDistance / 10f;
				RectangleF rectangleF = new RectangleF(vector.X - 2f * num7, vector.Y, num6 + 2f * num7, -num5 * (float)triggerBar.Sign);
				base.RenderTarget.DrawRectangle(rectangleF, brush2);
				brush2.Dispose();
				float num8 = (num6 / (float)this.SummaryTriggerForceBarBlocks - 1f) / 2f;
				int num9 = (int)Math.Ceiling(triggerBar.VolumeBuy * (double)this.SummaryTriggerForceBarBlocks / triggerBar.TotalVolume);
				for (int i = 0; i < this.SummaryTriggerForceBarBlocks; i++)
				{
					Vector2 vector2 = new Vector2(num4 - this.barDistance * (float)i - num7, vector.Y - (float)triggerBar.Sign * num7);
					bool flag = (triggerBar.IsBullish ? (num9 <= 0) : (num9 > 0));
					this.DrawDiamond(chartControl, chartScale, vector2, flag ? Brushes.Transparent : brush, num8, num5 - 2f * num7, -1, triggerBar.Sign, flag, 1, brush, 2f);
					if (num9 > 0)
					{
						num9--;
					}
				}
				global::System.Windows.Media.Brush brush3 = (triggerBar.IsBullish ? this.SummaryTriggerBuyVolumeBrush : this.SummaryTriggerSellVolumeBrush);
				DisposeBase disposeBase = brush3.ToDxBrush(base.RenderTarget);
				long num10 = Convert.ToInt64(triggerBar.IsBullish ? triggerBar.VolumeBuy : triggerBar.VolumeSell);
				string text = (triggerBar.IsBullish ? this.SummaryTriggerBuyVolumeString : this.SummaryTriggerSellVolumeString) + num10.ToString();
				float num11 = num2 - (float)triggerBar.Sign * ((float)this.SummaryTriggerMargin + num5);
				this.DrawText(text, this.SummaryTriggerNumberFont, rectangleF.X, num11, 1, -triggerBar.Sign, brush3, this.ScreenDPI, base.RenderTarget);
				disposeBase.Dispose();
			}
		}
		
		private void DrawRanges(ChartControl chartControl, ChartScale chartScale, SortedList<int, DDSonarlikeIcebergFinder.RangeInfo> listRangeInfo, bool isActive)
		{
			if (listRangeInfo.Count == 0 || (!isActive && !this.RangeInactiveEnabled))
			{
				return;
			}
			for (int i = 0; i < listRangeInfo.Count; i++)
			{
				DDSonarlikeIcebergFinder.RangeInfo rangeInfo = listRangeInfo.Values[i];
				DDSonarlikeIcebergFinder.TriggerBarInfo triggerBar = rangeInfo.TriggerBar;
				if (triggerBar.BarStart <= base.ChartBars.ToIndex && triggerBar.BarEnd >= base.ChartBars.FromIndex)
				{
					this.DrawTriggerSummary(chartControl, chartScale, rangeInfo);
				}
				if (rangeInfo.BarStart <= base.ChartBars.ToIndex && rangeInfo.BarEnd >= base.ChartBars.FromIndex)
				{
					this.DrawOneRange(chartControl, chartScale, rangeInfo, isActive);
				}
			}
		}
		
		private void DrawOneRange(ChartControl chartControl, ChartScale chartScale, DDSonarlikeIcebergFinder.RangeInfo rangeInfo, bool isActive)
		{
			global::System.Windows.Media.Brush brush = (isActive ? (rangeInfo.IsBullish ? this.rangeActiveBullish : this.rangeActiveBearish) : (rangeInfo.IsBullish ? this.rangeInactiveBullish : this.rangeInactiveBearish));
			if (brush.IsTransparent())
			{
				return;
			}
			float num = (float)chartControl.GetXByBarIndex(base.ChartBars, rangeInfo.BarStart);
			float num2 = (float)chartScale.GetYByValue(rangeInfo.PriceTop);
			Vector2 vector = new Vector2(num, num2 + 2f);
			float num3 = (float)chartControl.GetXByBarIndex(base.ChartBars, rangeInfo.BarEnd);
			float num4 = (float)chartScale.GetYByValue(rangeInfo.PriceBottom);
			Vector2 vector2 = new Vector2(num, num4 - 2f);
			Vector2 vector3 = new Vector2(num3, num2 + 2f);
			Vector2 vector4 = new Vector2(num3, num4 - 2f);
			global::SharpDX.Direct2D1.GradientStop[] array = null;
			if (this.GradientEnabled)
			{
				array = ((!isActive) ? (rangeInfo.IsBullish ? this.rangeInactiveBottomGradientStop : this.rangeInactiveTopGradientStop) : (rangeInfo.IsBullish ? this.rangeActiveBottomGradientStop : this.rangeActiveTopGradientStop));
				if (array == null)
				{
					return;
				}
			}
			global::SharpDX.Direct2D1.Brush brush2 = brush.ToDxBrush(base.RenderTarget);
			RectangleF rectangleF = new RectangleF(vector.X, vector.Y, vector3.X - vector.X, vector2.Y - vector.Y);
			if (this.GradientEnabled)
			{
				global::SharpDX.Direct2D1.GradientStopCollection gradientStopCollection = new global::SharpDX.Direct2D1.GradientStopCollection(base.RenderTarget, array);
				LinearGradientBrushProperties linearGradientBrushProperties = new LinearGradientBrushProperties
				{
					StartPoint = vector,
					EndPoint = vector2
				};
				global::SharpDX.Direct2D1.LinearGradientBrush linearGradientBrush = new global::SharpDX.Direct2D1.LinearGradientBrush(base.RenderTarget, linearGradientBrushProperties, gradientStopCollection);
				base.RenderTarget.FillRectangle(rectangleF, linearGradientBrush);
				linearGradientBrush.Dispose();
				gradientStopCollection.Dispose();
			}
			else
			{
				base.RenderTarget.FillRectangle(rectangleF, brush2);
				brush2.Dispose();
			}
			Stroke stroke = (isActive ? (rangeInfo.IsBullish ? this.RangeActiveLineBullishStroke : this.RangeActiveLineBearishStroke) : (rangeInfo.IsBullish ? this.RangeInactiveLineBullishStroke : this.RangeInactiveLineBearishStroke));
			global::System.Windows.Media.Brush brush3 = stroke.Brush;
			if (!brush3.IsTransparent())
			{
				global::SharpDX.Direct2D1.Brush brush4 = brush3.ToDxBrush(base.RenderTarget);
				base.RenderTarget.DrawLine(vector, vector3, brush4, stroke.Width);
				base.RenderTarget.DrawLine(vector2, vector4, brush4, stroke.Width);
				brush4.Dispose();
			}
			if (this.SummaryRangeEnabled)
			{
				global::System.Windows.Media.Brush brush5 = (isActive ? (rangeInfo.IsBullish ? this.summaryRangeForceBarPositiveActive : this.summaryRangeForceBarNegativeActive) : (rangeInfo.IsBullish ? this.summaryRangeForceBarPositiveInactive : this.summaryRangeForceBarNegativeInactive));
				if (brush5.IsTransparent())
				{
					return;
				}
				global::SharpDX.Direct2D1.Brush brush6 = brush5.ToDxBrush(base.RenderTarget);
				RectangleF rectangleF2 = new RectangleF(vector.X, (rangeInfo.IsBullish ? vector2.Y : vector.Y) + (float)(rangeInfo.Sign * this.SummaryRangeMargin), vector3.X - vector.X, (float)(rangeInfo.Sign * this.SummaryRangeHeight));
				base.RenderTarget.DrawRectangle(rectangleF2, brush6);
				RectangleF rectangleF3 = new RectangleF(rectangleF2.X, rectangleF2.Y, rectangleF2.Width, rectangleF2.Height);
				float num5 = (float)(rangeInfo.VolumeBuy + rangeInfo.VolumeSell);
				if (rangeInfo.IsBullish)
				{
					rectangleF3.Width = rectangleF2.Width * (float)rangeInfo.VolumeBuy / num5;
					rectangleF3.X += rectangleF2.Width - rectangleF3.Width;
				}
				else
				{
					rectangleF3.Width = rectangleF2.Width * (float)rangeInfo.VolumeSell / num5;
				}
				base.RenderTarget.FillRectangle(rectangleF3, brush6);
				brush6.Dispose();
				bool flag = rangeInfo.VolumeDelta == 0L;
				global::System.Windows.Media.Brush brush7 = (isActive ? (flag ? this.summaryRangeDeltaNeutralActive : ((rangeInfo.VolumeDelta > 0L) ? this.summaryRangeDeltaPositiveActive : this.summaryRangeDeltaNegativeActive)) : (flag ? this.summaryRangeDeltaNeutralInactive : ((rangeInfo.VolumeDelta > 0L) ? this.summaryRangeDeltaPositiveInactive : this.summaryRangeDeltaNegativeInactive)));
				if (brush7.IsTransparent())
				{
					return;
				}
				DisposeBase disposeBase = brush7.ToDxBrush(base.RenderTarget);
				long num6 = Convert.ToInt64(rangeInfo.VolumeBuy - rangeInfo.VolumeSell);
				string text = "Δ = " + ((num6 > 0L) ? "+" : "") + num6.ToString();
				float num7 = rectangleF2.Y + (float)(rangeInfo.Sign * (this.SummaryRangeMargin + this.SummaryRangeHeight));
				this.ComputeTextSize(text, this.SummaryRangeNumberFont, this.ScreenDPI);
				this.DrawText(text, this.SummaryRangeNumberFont, num, num7, 1, rangeInfo.Sign, brush7, this.ScreenDPI, base.RenderTarget);
				disposeBase.Dispose();
			}
		}
		
		private void DrawDiamond(ChartControl chartControl, ChartScale chartScale, Vector2 startPoint, global::System.Windows.Media.Brush brush, float width, float height, int horizontalAlignment, int verticalAlignment, bool borderEnable = false, int borderWidth = 0, global::System.Windows.Media.Brush borderBrush = null, float slope = 2f)
		{
			if (brush.IsTransparent() && borderEnable && borderBrush.IsTransparent())
			{
				return;
			}
			if (width == 0f && height == 0f)
			{
				return;
			}
			verticalAlignment = -verticalAlignment;
			Vector2 vector = new Vector2(startPoint.X + width * (float)horizontalAlignment, startPoint.Y);
			Vector2 vector2 = new Vector2(startPoint.X + width * (float)horizontalAlignment * slope, startPoint.Y + height * (float)verticalAlignment);
			Vector2 vector3 = new Vector2(vector2.X - width * (float)horizontalAlignment, vector2.Y);
			global::SharpDX.Direct2D1.PathGeometry pathGeometry = new global::SharpDX.Direct2D1.PathGeometry(base.RenderTarget.Factory);
			GeometrySink geometrySink = pathGeometry.Open();
			geometrySink.BeginFigure(startPoint, FigureBegin.Filled);
			geometrySink.AddLines(new Vector2[] { vector, vector2, vector3, startPoint });
			geometrySink.EndFigure(FigureEnd.Open);
			geometrySink.Close();
			AntialiasMode antialiasMode = base.RenderTarget.AntialiasMode;
			base.RenderTarget.AntialiasMode = AntialiasMode.PerPrimitive;
			global::SharpDX.Direct2D1.Brush brush2 = brush.ToDxBrush(base.RenderTarget);
			base.RenderTarget.FillGeometry(pathGeometry, brush2);
			if (borderEnable && !borderBrush.IsTransparent())
			{
				global::SharpDX.Direct2D1.Brush brush3 = borderBrush.ToDxBrush(base.RenderTarget);
				base.RenderTarget.DrawGeometry(pathGeometry, brush3, width = (float)borderWidth);
				brush3.Dispose();
			}
			brush2.Dispose();
			geometrySink.Dispose();
			pathGeometry.Dispose();
			base.RenderTarget.AntialiasMode = antialiasMode;
		}
		
		private void AddMarker(int barIndex, bool isBullish)
		{
			if (!this.MarkerEnabled)
			{
				return;
			}
			DDSonarlikeIcebergFinder.MarkerInfo markerInfo = new DDSonarlikeIcebergFinder.MarkerInfo(barIndex, isBullish);
			if (!this.dictMarkers.ContainsKey(barIndex))
			{
				this.dictMarkers.Add(barIndex, markerInfo);
				return;
			}
			this.dictMarkers[barIndex] = markerInfo;
		}
		
		private void DrawMarkers(ChartScale chartScale)
		{
			if (!this.MarkerEnabled)
			{
				return;
			}
			if (this.dictMarkers.Count == 0)
			{
				return;
			}
			for (int i = base.ChartBars.FromIndex; i <= base.ChartBars.ToIndex; i++)
			{
				if (this.dictMarkers.ContainsKey(i))
				{
					this.DrawOneMarker(chartScale, this.dictMarkers[i]);
				}
			}
		}
		
		private void DrawOneMarker(ChartScale chartScale, DDSonarlikeIcebergFinder.MarkerInfo markerInfo)
		{
			bool isBullish = markerInfo.IsBullish;
			int barIndex = markerInfo.BarIndex;
			string text = (isBullish ? this.MarkerStringBullish : this.MarkerStringBearish);
			text = this.FormatMarkerString(text);
			if (string.IsNullOrWhiteSpace(text))
			{
				return;
			}
			if (!isBullish && base.Highs[0].GetValueAt(barIndex).ApproxCompare(chartScale.MaxValue) >= 0)
			{
				return;
			}
			if (isBullish && base.Lows[0].GetValueAt(barIndex).ApproxCompare(chartScale.MinValue) <= 0)
			{
				return;
			}
			global::System.Windows.Media.Brush brush = (isBullish ? this.MarkerBrushBullish : this.MarkerBrushBearish);
			if (brush.IsTransparent())
			{
				return;
			}
			int num = (isBullish ? 1 : (-1));
			float num2 = (float)base.ChartControl.GetXByBarIndex(base.ChartBars, barIndex);
			float num3 = (float)(chartScale.GetYByValue((isBullish ? base.Lows[0] : base.Highs[0]).GetValueAt(barIndex)) + num * this.MarkerOffset);
			this.DrawText(text, this.MarkerFont, num2, num3, 0, num, brush, this.ScreenDPI, base.RenderTarget);
		}
		
		private void ComputeVolumes()
		{
			this.barShift = ((base.State == State.Historical || base.Calculate == Calculate.OnBarClose) ? 1 : 0);
			if (base.BarsInProgress == 0)
			{
				if (this.isUpDownTickMode)
				{
					this.tickVolumeBarIndex = base.CurrentBars[0] + 1;
				}
				else
				{
					if (base.CurrentBars[1] >= 0 && base.CurrentBars[0] >= 0 && this.dictPresentations.ContainsKey(this.tickVolumeBarIndex) && this.dictPresentations[this.tickVolumeBarIndex].GetTotalVol().ApproxCompare(base.Volumes[0].GetValueAt(this.tickVolumeBarIndex)) < 0)
					{
						if (this.tickVolumeCurrentBar1 == -1)
						{
							this.tickVolumeCurrentBar1 = base.CurrentBars[1] - 1;
						}
						int num = base.BarsArray[1].Count - 1 - this.tickVolumeCurrentBar1;
						if (num > 0)
						{
							for (int i = 0; i < num; i++)
							{
								this.tickVolumeCurrentBar1++;
								if (base.CurrentBars[1] == 0)
								{
									this.listStartBarIndex1.Add(i);
								}
								long ticks = base.BarsArray[1].GetTime(this.tickVolumeCurrentBar1).Ticks;
								if (ticks - base.BarsArray[0].GetTime(this.tickVolumeBarIndex).Ticks > 0L)
								{
									break;
								}
								double num2 = (double)base.BarsArray[1].GetVolume(this.tickVolumeCurrentBar1);
								double close = base.BarsArray[1].GetClose(this.tickVolumeCurrentBar1);
								int priceKey = this.GetPriceKey(close);
								this.queueCurrentBar1.Enqueue(this.tickVolumeCurrentBar1);
								if (close >= base.BarsArray[1].GetAsk(this.tickVolumeCurrentBar1))
								{
									this.lastTickIsBuy = true;
									if (this.UpdateAndCorrectVolume(num2, priceKey, close, ticks, this.tickVolumeCurrentBar1, true))
									{
										break;
									}
								}
								else if (close <= base.BarsArray[1].GetBid(this.tickVolumeCurrentBar1))
								{
									this.lastTickIsBuy = false;
									if (this.UpdateAndCorrectVolume(num2, priceKey, close, ticks, this.tickVolumeCurrentBar1, false))
									{
										break;
									}
								}
								else
								{
									double close2 = base.BarsArray[1].GetClose(this.tickVolumeCurrentBar1 - 1);
									bool flag = ((close.ApproxCompare(close2) == 0) ? this.lastTickIsBuy : (close.ApproxCompare(close2) > 0));
									this.lastTickIsBuy = flag;
									if (this.UpdateAndCorrectVolume(num2, priceKey, close, ticks, this.tickVolumeCurrentBar1, flag))
									{
										break;
									}
								}
							}
						}
					}
					this.tickVolumeBarIndex = base.CurrentBars[0] + this.barShift;
					if (!this.dictPresentations.ContainsKey(this.tickVolumeBarIndex))
					{
						this.dictPresentations.Add(this.tickVolumeBarIndex, new DDSonarlikeIcebergFinder.Presentation());
					}
					this.UpdateVolumes();
				}
			}
			else if (base.BarsInProgress == 1)
			{
				if (this.isUpDownTickMode)
				{
					this.DecideTickState();
					long volume = this.GetVolume(base.Volumes[1][0]);
					double num3 = base.Closes[1][0];
					if (!this.dictPresentations.ContainsKey(this.tickVolumeBarIndex))
					{
						this.dictPresentations.Add(this.tickVolumeBarIndex, new DDSonarlikeIcebergFinder.Presentation());
					}
					if (this.tickState > 0)
					{
						this.dictPresentations[this.tickVolumeBarIndex].AddBuy(this.GetPriceKey(num3), (double)volume);
					}
					if (this.tickState < 0)
					{
						this.dictPresentations[this.tickVolumeBarIndex].AddSell(this.GetPriceKey(num3), (double)volume);
					}
				}
				else
				{
					if (this.queueCurrentBar1.Count > 0 && (this.queueCurrentBar1.Dequeue() == base.CurrentBars[1] + this.barShift || this.listStartBarIndex1.Contains(base.CurrentBars[1])))
					{
						return;
					}
					if (this.barShift == 1 && (base.CurrentBars[1] == 0 || (this.listStartBarIndex1.Count > 0 && this.recalcSkippedBar)))
					{
						this.ComputeTickVolume(base.CurrentBars[1]);
						this.recalcSkippedBar = false;
					}
					if (base.CurrentBars[1] + this.barShift > this.tickVolumeCurrentBar1)
					{
						this.tickVolumeCurrentBar1 = base.CurrentBars[1] + this.barShift;
					}
					this.ComputeTickVolume(base.CurrentBars[1] + this.barShift);
				}
			}
			this.SyncData();
		}
		
		private void UpdateVolumes()
		{
			if (this.queuePresentationCell.Count == 0)
			{
				return;
			}
			int count = this.queuePresentationCell.Count;
			int i = 0;
			while (i < count)
			{
				DDSonarlikeIcebergFinder.PresentationCell presentationCell = this.queuePresentationCell.Peek();
				double num = (double)base.BarsArray[0].GetVolume(this.tickVolumeBarIndex) - this.dictPresentations[this.tickVolumeBarIndex].GetTotalVol();
				if (this.tickVolumeBarIndex <= 0)
				{
					goto IL_009D;
				}
				long ticks = base.BarsArray[0].GetTime(this.tickVolumeBarIndex - 1).Ticks;
				if (presentationCell.TimeTicks >= ticks)
				{
					goto IL_009D;
				}
				this.queuePresentationCell.Dequeue();
				IL_01B2:
				i++;
				continue;
				IL_009D:
				if (presentationCell.VolBuy > 0.0)
				{
					if (presentationCell.VolBuy.ApproxCompare(num) > 0)
					{
						presentationCell.VolBuy -= num;
						this.dictPresentations[this.tickVolumeBarIndex].AddBuy(presentationCell.Key, num);
						return;
					}
					this.dictPresentations[this.tickVolumeBarIndex].AddBuy(presentationCell.Key, presentationCell.VolBuy);
					this.queuePresentationCell.Dequeue();
					if (presentationCell.VolBuy.ApproxCompare(num) == 0)
					{
						return;
					}
					goto IL_01B2;
				}
				else
				{
					if (presentationCell.VolSell <= 0.0)
					{
						goto IL_01B2;
					}
					if (presentationCell.VolSell.ApproxCompare(num) > 0)
					{
						presentationCell.VolSell -= num;
						this.dictPresentations[this.tickVolumeBarIndex].AddSell(presentationCell.Key, num);
						return;
					}
					this.dictPresentations[this.tickVolumeBarIndex].AddSell(presentationCell.Key, presentationCell.VolSell);
					this.queuePresentationCell.Dequeue();
					if (presentationCell.VolSell.ApproxCompare(num) == 0)
					{
						return;
					}
					goto IL_01B2;
				}
			}
		}
		
		private bool UpdateAndCorrectVolume(double volume, int key, double price, long timeTicks, int currentBar1, bool isBuy)
		{
			if (isBuy)
			{
				this.dictPresentations[this.tickVolumeBarIndex].AddBuy(key, volume);
			}
			else
			{
				this.dictPresentations[this.tickVolumeBarIndex].AddSell(key, volume);
			}
			double num = this.dictPresentations[this.tickVolumeBarIndex].GetTotalVol() - base.Volumes[0].GetValueAt(this.tickVolumeBarIndex);
			if (num.ApproxCompare(0.0) >= 0)
			{
				if (num.ApproxCompare(0.0) > 0)
				{
					this.dictPresentations[this.tickVolumeBarIndex].AddVolume(isBuy, key, -num);
					this.EnqueueData(num, key, price, timeTicks, currentBar1, isBuy);
				}
				return true;
			}
			return false;
		}
		private void SyncData()
		{
		}
		
		private void ComputeTickVolume(int currentBar1)
		{
			try
			{
				long ticks = base.BarsArray[1].GetTime(currentBar1).Ticks;
				double num = (double)base.BarsArray[1].GetVolume(currentBar1);
				double close = base.BarsArray[1].GetClose(currentBar1);
				int priceKey = this.GetPriceKey(close);
				if (close.ApproxCompare(base.BarsArray[1].GetAsk(currentBar1)) >= 0)
				{
					this.AddTickVolume(num, priceKey, close, ticks, currentBar1, true);
					this.lastTickIsBuy = true;
				}
				else if (close.ApproxCompare(base.BarsArray[1].GetBid(currentBar1)) <= 0)
				{
					this.AddTickVolume(num, priceKey, close, ticks, currentBar1, false);
					this.lastTickIsBuy = false;
				}
				else
				{
					double close2 = base.BarsArray[1].GetClose(currentBar1 - 1);
					bool flag = ((close.ApproxCompare(close2) == 0) ? this.lastTickIsBuy : (close.ApproxCompare(close2) > 0));
					this.AddTickVolume(num, priceKey, close, ticks, currentBar1, flag);
					this.lastTickIsBuy = flag;
				}
			}
			catch (Exception ex)
			{
				this.PrintException(ex);
			}
		}
		
		private void AddTickVolume(double volume, int key, double price, long timeTicks, int currentBar1, bool isBuy)
		{
			long num = timeTicks - base.BarsArray[0].GetTime(this.tickVolumeBarIndex).Ticks;
			double num2 = (double)base.BarsArray[0].GetVolume(this.tickVolumeBarIndex) - this.dictPresentations[this.tickVolumeBarIndex].GetTotalVol();
			if (num2.ApproxCompare(0.0) <= 0 || num > 0L)
			{
				this.EnqueueData(volume, key, price, timeTicks, currentBar1, isBuy);
				return;
			}
			if (volume.ApproxCompare(num2) <= 0)
			{
				this.dictPresentations[this.tickVolumeBarIndex].AddVolume(isBuy, key, volume);
				return;
			}
			this.EnqueueData(volume - num2, key, price, timeTicks, currentBar1, isBuy);
			this.dictPresentations[this.tickVolumeBarIndex].AddVolume(isBuy, key, num2);
		}
		
		private void EnqueueData(double volume, int key, double price, long timeTicks, int currentBar, bool isBuy)
		{
			if (isBuy)
			{
				this.queuePresentationCell.Enqueue(new DDSonarlikeIcebergFinder.PresentationCell(volume, 0.0, key, price, timeTicks, currentBar));
				return;
			}
			this.queuePresentationCell.Enqueue(new DDSonarlikeIcebergFinder.PresentationCell(0.0, volume, key, price, timeTicks, currentBar));
		}
		private int GetPriceKey(double price)
		{
			return Convert.ToInt32(price / base.TickSize);
		}
		private double GetPriceByKey(int key)
		{
			return (double)key * base.TickSize;
		}
		
		private void DecideTickState()
		{
			if (base.CurrentBars[1] <= 0)
			{
				this.tickState = 0;
				return;
			}
			int num = base.Instrument.MasterInstrument.Compare(base.Closes[1][0], base.Closes[1][1]);
			if (num != 0)
			{
				this.tickState = num;
			}
		}
		
		private long GetVolume(double vol)
		{
			if (this.VolumeBase == DDSonarlikeIcebergFinder_VolumeBase.UpDownTick_UnitVolume)
			{
				return 1L;
			}
			return Convert.ToInt64(vol);
		}
		
		private bool IsDDRenkoOrKingRenkoBarType(BarsPeriodType periodType)
		{
			return periodType == (BarsPeriodType)12345 || periodType == (BarsPeriodType)678910;
		}
		
		private void ComputeUnit()
		{
			try
			{
				BarsPeriod barsPeriod = base.BarsArray[0].BarsPeriod;
				BarsPeriodType barsPeriodType = barsPeriod.BarsPeriodType;
				int value = barsPeriod.Value;
				bool flag = NinjaTrader.NinjaScript.Indicators.DimDim.DDSonarlikeIcebergFinder.barsPeriodIdentityGlobal == null && this.RangeUnit == DDSonarlikeIcebergFinder_Unit.DDATR;
				this.isDDRenkoOrKingRenkoBarType = this.IsDDRenkoOrKingRenkoBarType(barsPeriodType);
				bool flag2;
				if (NinjaTrader.NinjaScript.Indicators.DimDim.DDSonarlikeIcebergFinder.barsPeriodIdentityGlobal != null)
				{
					BarsPeriodType? barsPeriodType2 = NinjaTrader.NinjaScript.Indicators.DimDim.DDSonarlikeIcebergFinder.barsPeriodIdentityGlobal;
					BarsPeriodType barsPeriodType3 = barsPeriodType;
					flag2 = !((barsPeriodType2.GetValueOrDefault() == barsPeriodType3) & (barsPeriodType2 != null));
				}
				else
				{
					flag2 = false;
				}
				bool flag3 = flag2;
				bool flag4 = this.isDDRenkoOrKingRenkoBarType && NinjaTrader.NinjaScript.Indicators.DimDim.DDSonarlikeIcebergFinder.barsPeriodValueGlobal != 0 && NinjaTrader.NinjaScript.Indicators.DimDim.DDSonarlikeIcebergFinder.barsPeriodValueGlobal != value;
				if (flag || flag3 || flag4)
				{
					if (this.isDDRenkoOrKingRenkoBarType)
					{
						this.RangeUnit = DDSonarlikeIcebergFinder_Unit.Ticks;
						this.RangeFactorTicks = (double)(value * 2);
					}
					else
					{
						this.RangeUnit = DDSonarlikeIcebergFinder_Unit.DDATR;
					}
				}
				NinjaTrader.NinjaScript.Indicators.DimDim.DDSonarlikeIcebergFinder.barsPeriodIdentityGlobal = new BarsPeriodType?(barsPeriodType);
				NinjaTrader.NinjaScript.Indicators.DimDim.DDSonarlikeIcebergFinder.barsPeriodValueGlobal = value;
			}
			catch (Exception ex)
			{
				this.PrintException(ex);
			}
		}
		
		private void MoveDataFromListToList<T>(int key, T TData, SortedList<int, T> listActive, SortedList<int, T> listInactive)
		{
			if (!listInactive.ContainsKey(key))
			{
				listInactive.Add(key, TData);
			}
			else
			{
				listInactive[key] = TData;
			}
			listActive.Remove(key);
		}
		
		private void AddDataToList<T>(int key, T TData, SortedList<int, T> listActive)
		{
			if (!listActive.ContainsKey(key))
			{
				listActive.Add(key, TData);
				return;
			}
			listActive[key] = TData;
		}
		
		private void UpdateBarStatePaintBars(DDSonarlikeIcebergFinder.BarStateType barState, int barStart, int barEnd, bool isBullish)
		{
			if (!this.isCharting)
			{
				return;
			}
			if (!this.BarEnabled)
			{
				return;
			}
			if (barStart > barEnd)
			{
				return;
			}
			int num = (isBullish ? 1 : (-1));
			for (int i = barStart; i <= barEnd; i++)
			{
				int num2 = base.CurrentBars[0] - i;
				if (Math.Abs(this.seriesBarState[num2]) < (int)barState)
				{
					this.seriesBarState[num2] = num * (int)barState;
					this.PaintOneBar(true, this.seriesBarState[num2], i);
				}
			}
		}
		
		private void PaintOneBar(bool isToggleClickEvent, int barState, int barIndex)
		{
			if (!isToggleClickEvent && barState == 0)
			{
				return;
			}
			if (!this.isCharting || !this.BarEnabled || !this.SwitchedOn)
			{
				return;
			}
			bool flag = barState > 0;
			int num = base.CurrentBars[0] - barIndex;
			global::System.Windows.Media.Brush barBrush = this.GetBarBrush(this.seriesBarState[num], flag);
			int num2 = (isToggleClickEvent ? base.Closes[0].GetValueAt(barIndex).ApproxCompare(base.Opens[0].GetValueAt(barIndex)) : base.Closes[0][0].ApproxCompare(base.Opens[0][0]));
			int num3 = (flag ? 1 : (-1));
			if (this.BarOutlineEnabled && !barBrush.IsTransparent())
			{
				if (isToggleClickEvent)
				{
					base.CandleOutlineBrushes[num] = barBrush;
				}
				else
				{
					base.CandleOutlineBrush = barBrush;
				}
			}
			if (this.BarBiasBased)
			{
				if (barBrush.IsTransparent())
				{
					if (num3 * num2 < 0)
					{
						if (isToggleClickEvent)
						{
							base.BarBrushes[num] = Brushes.Transparent;
						}
						else
						{
							base.BarBrush = Brushes.Transparent;
						}
					}
				}
				else if (num2 != 0)
				{
					if (isToggleClickEvent)
					{
						base.BarBrushes[num] = ((num3 * num2 > 0) ? barBrush : Brushes.Transparent);
					}
					else
					{
						base.BarBrush = ((num3 * num2 > 0) ? barBrush : Brushes.Transparent);
					}
				}
			}
			else if (num2 != 0)
			{
				if (isToggleClickEvent)
				{
					base.BarBrushes[num] = barBrush;
				}
				else
				{
					base.BarBrush = barBrush;
				}
			}
			if (!isToggleClickEvent)
			{
				this.seriesBarState[num] = barState;
			}
		}
		
		private global::System.Windows.Media.Brush GetBarBrush(int barState, bool isBullish)
		{
			global::System.Windows.Media.Brush brush;
			if (Math.Abs(barState) == 2)
			{
				brush = (isBullish ? this.BarTriggerBullish : this.BarTriggerBearish);
			}
			else if (Math.Abs(barState) == 1)
			{
				brush = (isBullish ? this.BarRangeBullish : this.BarRangeBearish);
			}
			else
			{
				brush = (isBullish ? this.MarkerBrushBullish : this.MarkerBrushBearish);
			}
			return brush;
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
			string text = "DDSonarlikeIcebergFinder.marker." + (isBullish ? "bullish." : "bearish.") + base.CurrentBar.ToString();
			global::System.Windows.Media.Brush brush = (isBullish ? this.MarkerBrushBullish : this.MarkerBrushBearish);
			if (brush.IsTransparent())
			{
				return;
			}
			double num = (isBullish ? base.Low[0] : base.High[0]);
			string text2 = (isBullish ? this.MarkerStringBullish : this.MarkerStringBearish);
			text2 = this.FormatMarkerString(text2);
			if (string.IsNullOrWhiteSpace(text2))
			{
				return;
			}
			int num2 = Convert.ToInt32(this.ComputeTextSize(text2, this.MarkerFont, this.ScreenDPI).Height);
			int num3 = (isBullish ? (-1) : 1) * (this.MarkerOffset + num2 / 2);
			NinjaTrader.NinjaScript.DrawingTools.Draw.Text(this, text, base.IsAutoScale, text2, 0, num, num3, brush, this.MarkerFont, TextAlignment.Center, Brushes.Transparent, Brushes.Transparent, 0);
		}
		
				private void PrintException(Exception ex)
		{
			NinjaTrader.Code.Output.Process(ex.ToString(), PrintTo.OutputTab1);
		}
		
		private int GetDPI()
		{
			int dpi = 99;
			try
			{
				System.Windows.Window mainWindow = (System.Windows.Application.Current != null) ? System.Windows.Application.Current.MainWindow : null;
				if (mainWindow != null)
				{
					System.Windows.PresentationSource src = System.Windows.PresentationSource.FromVisual(mainWindow);
					if (src != null && src.CompositionTarget != null)
					{
						dpi = (int)(96.0 * src.CompositionTarget.TransformToDevice.M11);
					}
				}
			}
			catch { }
			if (dpi < 99) dpi = 99;
			if (dpi > 500) dpi = 500;
			return dpi;
		}
		
		private string FormatMarkerString(string input)
		{
			if (string.IsNullOrWhiteSpace(input))
			{
				return string.Empty;
			}
			string[] parts = input.Trim().Split(new[] { '+' }, StringSplitOptions.RemoveEmptyEntries);
			for (int i = 0; i < parts.Length; i++)
			{
				parts[i] = parts[i].Trim();
			}
			return string.Join("\n", parts).Trim();
		}

		private Size2F ComputeTextSize(string text, SimpleFont font, int dpi)
		{
			if (font == null)
			{
				return new Size2F(0f, 12f);
			}
			if (string.IsNullOrEmpty(text))
			{
				return new Size2F(0f, (float)(font.Size * 1.5));
			}
			string[] parts = text.Split('\n');
			int maxLen = 0;
			for (int i = 0; i < parts.Length; i++)
			{
				if (parts[i].Length > maxLen) maxLen = parts[i].Length;
			}
			float lineHeight = (float)(font.Size * 1.5);
			float width = (float)(maxLen * font.Size * 0.7);
			float height = lineHeight * Math.Max(1, parts.Length);
			return new Size2F(width, height);
		}
		
		private void DrawText(string text, SimpleFont font, float x, float y, int angle, int direction,
			global::System.Windows.Media.Brush wpfBrush, int dpi, global::SharpDX.Direct2D1.RenderTarget renderTarget)
		{
			if (renderTarget == null || font == null || string.IsNullOrEmpty(text) || wpfBrush == null)
				return;
			Size2F size = this.ComputeTextSize(text, font, dpi);
			float top;
			if (direction > 0)
				top = y;
			else if (direction < 0)
				top = y - size.Height;
			else
				top = y - size.Height / 2f;
			global::SharpDX.RectangleF rect = new global::SharpDX.RectangleF(x - size.Width / 2f, top, size.Width, size.Height);
			using (global::SharpDX.DirectWrite.Factory factory = new global::SharpDX.DirectWrite.Factory())
			using (global::SharpDX.DirectWrite.TextFormat tf = new global::SharpDX.DirectWrite.TextFormat(factory, font.Family.ToString(), (float)font.Size))
			{
				tf.TextAlignment = global::SharpDX.DirectWrite.TextAlignment.Center;
				tf.ParagraphAlignment = global::SharpDX.DirectWrite.ParagraphAlignment.Near;
				tf.WordWrapping = global::SharpDX.DirectWrite.WordWrapping.NoWrap;
				global::SharpDX.Direct2D1.Brush dxBrush = wpfBrush.ToDxBrush(renderTarget);
				renderTarget.DrawText(text, tf, rect, dxBrush);
				dxBrush.Dispose();
			}
		}

		
		private const int defaultMargin = 5;
		private const int defaultBorderWidth = 4;
		private const string toolTipSpace = "  ";
		private const int neutralRangePercentage = 40;
		private const int ReferencePricePeriod = 2;
		private const int ReferencePriceCloseWeight = 1;
		private const int SlowdownScan = 5;
		private const int WeakWeakSplit = 10;
		private static BarsPeriodType? barsPeriodIdentityGlobal;
		private static int barsPeriodValueGlobal;
		private const bool VolumeFilterEnabled = false;
		private const int VolumeFilterSizeMinimum = 1;
		private const int VolumeFilterSizeMaximum = 999;
		private const bool SignalFilterEnabled = false;
		private int VolumePeriod;
		private const bool TriggerGroupFilterEnabled = true;
		private const int TriggerGroupMaxBars = 10;
		private const int TriggerGroupPeriodBars = 10;
		private double OffsetMultiplierTrend;
		private bool RangeWithoutGroupEnabled;
		private bool isDDRenkoOrKingRenkoBarType;
		private bool isUpDownTickMode;
		private bool isMarkerCustomRenderingMethod;
		private bool isUnitATR;
		private List<double> listRawPositiveDelta;
		private List<double> listRawNegativeDelta;
		private bool originalDrawOnPricePanel;
		private Series<double> seriesRawBuyVolume;
		private Series<double> seriesRawSellVolume;
		private Series<double> volumeBuy;
		private Series<double> volumeSell;
		private Series<double> volumeBuyAvg;
		private Series<double> volumeSellAvg;
		private Series<double> thresholdPositiveStrong;
		private Series<double> thresholdPositiveModerate;
		private Series<double> thresholdNegativeModerate;
		private Series<double> thresholdNegativeStrong;
		private Series<double> seriesSignalState;
		private Series<double> seriesTrendVector;
		private Series<double> seriesTrailingStop;
		private Series<double> seriesDiffHighLow;
		private Series<int> seriesSignalTrend;
		private Series<int> seriesBarState;
		private DDSonarlikeIcebergFinder.SolarWaveInfo solarWaveInfo;
		private DDSonarlikeIcebergFinder.TriggerBarInfo triggerBarInfo;
		private DDSonarlikeIcebergFinder.RangeInfo lastRangeInfo;
		private SortedList<int, DDSonarlikeIcebergFinder.RangeInfo> listRangeInfoActive;
		private SortedList<int, DDSonarlikeIcebergFinder.RangeInfo> listRangeInfoInactive;
		private List<DDSonarlikeIcebergFinder.TriggerBarInfo> listTriggerBarInfo;
		private global::SharpDX.Direct2D1.GradientStop[] rangeActiveTopGradientStop;
		private global::SharpDX.Direct2D1.GradientStop[] rangeActiveBottomGradientStop;
		private global::SharpDX.Direct2D1.GradientStop[] rangeInactiveTopGradientStop;
		private global::SharpDX.Direct2D1.GradientStop[] rangeInactiveBottomGradientStop;
		private global::System.Windows.Media.Brush rangeActiveBearish;
		private global::System.Windows.Media.Brush rangeActiveBullish;
		private global::System.Windows.Media.Brush rangeInactiveBearish;
		private global::System.Windows.Media.Brush rangeInactiveBullish;
		private global::System.Windows.Media.Brush summaryRangeForceBarNegativeActive;
		private global::System.Windows.Media.Brush summaryRangeForceBarPositiveActive;
		private global::System.Windows.Media.Brush summaryRangeForceBarNegativeInactive;
		private global::System.Windows.Media.Brush summaryRangeForceBarPositiveInactive;
		private global::System.Windows.Media.Brush summaryRangeDeltaNegativeActive;
		private global::System.Windows.Media.Brush summaryRangeDeltaPositiveActive;
		private global::System.Windows.Media.Brush summaryRangeDeltaNeutralActive;
		private global::System.Windows.Media.Brush summaryRangeDeltaNegativeInactive;
		private global::System.Windows.Media.Brush summaryRangeDeltaPositiveInactive;
		private global::System.Windows.Media.Brush summaryRangeDeltaNeutralInactive;
		private const string nickname = "iceberg:exc";
		private const string prefix = "DDSonarlikeIcebergFinder";
		private const string indicatorName = "DDSonarlikeIceberg";
		private const string indicatorNameFull = "DDSonarlikeIceberg";
		private bool isCharting;
		private Dictionary<int, DDSonarlikeIcebergFinder.MarkerInfo> dictMarkers;
		private long volDelta;
		private long buyVol;
		private long sellVol;
		private double sumPositiveDelta;
		private double sumNegativeDelta;
		private int signalTrade;
		private int bullishBarIndex = 1;
		private int bearishBarIndex = -1;
		private double sumDiffHighLow;
		private int lastDiffBarIndex = -1;
		private float barDistance;
		private bool lastTickIsBuy;
		private bool recalcSkippedBar = true;
		private int barShift;
		private int tickState;
		private int tickVolumeBarIndex;
		private int tickVolumeCurrentBar1 = -1;
		private Queue<int> queueCurrentBar1;
		private Dictionary<int, DDSonarlikeIcebergFinder.Presentation> dictPresentations;
		private Queue<DDSonarlikeIcebergFinder.PresentationCell> queuePresentationCell;
		private List<int> listStartBarIndex1;
		private class MarkerInfo
		{
			public int BarIndex { get; set; }
			public bool IsBullish { get; set; }
			
			public MarkerInfo(int barIndex, bool isBullish)
			{
				this.BarIndex = barIndex;
				this.IsBullish = isBullish;
			}

		}
		private class TriggerBarInfo
		{
			public int BarStart { get; set; }
			public int BarEnd { get; set; }
			public bool IsBullish { get; set; }
			public double Highest { get; set; }
			public double Lowest { get; set; }
			public double TopPrice { get; set; }
			public double BottomPrice { get; set; }
			public List<int> ListBarIndex { get; set; }
			public double VolumeDelta { get; set; }
			public double VolumeBuy { get; set; }
			public double VolumeSell { get; set; }
			public double VolumeAvg { get; set; }
			public bool HasRange { get; set; }
			public double TotalVolume
			{
				get
				{
					return this.VolumeBuy + this.VolumeSell;
				}
			}
			public int Sign
			{
				get
				{
					if (!this.IsBullish)
					{
						return -1;
					}
					return 1;
				}
			}
			public bool IsGroup
			{
				get
				{
					return this.ListBarIndex.Count > 1;
				}
			}
			
			public TriggerBarInfo(int barStart, int barEnd, bool isBullish, List<int> listBarIndex, double volumeDelta, double volumeBuy, double volumeSell, double volumeAvg, double highest = -1.7976931348623157E+308, double lowest = 1.7976931348623157E+308, double topPrice = -1.7976931348623157E+308, double bottomPrice = 1.7976931348623157E+308)
			{
				this.BarStart = barStart;
				this.BarEnd = barEnd;
				this.IsBullish = isBullish;
				this.ListBarIndex = listBarIndex;
				this.TopPrice = topPrice;
				this.BottomPrice = bottomPrice;
				this.Highest = highest;
				this.Lowest = lowest;
				this.VolumeDelta = volumeDelta;
				this.VolumeBuy = volumeBuy;
				this.VolumeSell = volumeSell;
				this.VolumeAvg = (double)((int)Math.Abs(volumeAvg));
			}
			
			public void Update(bool isCreate, int barIndex, double high, double low, double topPrice, double bottomPrice, double volumeDelta, double volumeBuy, double volumeSell, double volumeAvg)
			{
				this.ListBarIndex.Add(barIndex);
				this.Highest = Math.Max(high, this.Highest);
				this.Lowest = Math.Min(low, this.Lowest);
				this.TopPrice = Math.Max(topPrice, this.TopPrice);
				this.BottomPrice = Math.Min(bottomPrice, this.BottomPrice);
				this.VolumeDelta += volumeDelta;
				this.VolumeBuy += volumeBuy;
				this.VolumeSell += volumeSell;
				this.VolumeAvg = (double)((int)Math.Abs(volumeAvg));
				if (isCreate)
				{
					this.BarStart = barIndex;
					return;
				}
				this.BarEnd = barIndex;
			}
			public bool SearchLimitReached(int limit, int currentBar)
			{
				return currentBar - this.BarEnd >= limit;
			}

		}
		private class RangeInfo
		{
			public DDSonarlikeIcebergFinder.TriggerBarInfo TriggerBar { get; set; }
			public int TriggerBarIndex { get; set; }
			public int BarStart { get; set; }
			public int BarEnd { get; set; }
			public double PriceTop { get; set; }
			public double PriceBottom { get; set; }
			public long VolumeBuy { get; set; }
			public long VolumeSell { get; set; }
			public bool IsBullish { get; set; }
			public List<DDSonarlikeIcebergFinder.TriggerBarInfo> TriggerBarCollection { get; set; }
			public long VolumeDelta
			{
				get
				{
					return this.VolumeBuy - this.VolumeSell;
				}
			}
			public long VolumeTotal
			{
				get
				{
					return this.VolumeBuy + this.VolumeSell;
				}
			}
			public int Sign
			{
				get
				{
					if (!this.IsBullish)
					{
						return -1;
					}
					return 1;
				}
			}
			public double PriceSignal
			{
				
				get
				{
					if (!this.IsBullish)
					{
						return this.PriceBottom;
					}
					return this.PriceTop;
				}
			}
			
			public RangeInfo(DDSonarlikeIcebergFinder.TriggerBarInfo triggerBar, int barStart, int barEnd, double priceTop, double priceBottom, bool isBullish)
			{
				this.TriggerBar = triggerBar;
				this.BarStart = barStart;
				this.BarEnd = barEnd;
				this.PriceTop = priceTop;
				this.PriceBottom = priceBottom;
				this.IsBullish = isBullish;
				this.TriggerBarCollection = new List<DDSonarlikeIcebergFinder.TriggerBarInfo>();
			}
			
			public int GetPriceState(double price)
			{
				if (((price - this.PriceSignal) * (double)this.Sign).ApproxCompare(0.0) >= 0)
				{
					return this.Sign;
				}
				return 0;
			}
			
			public void UpdateTriggerBarCollection(List<DDSonarlikeIcebergFinder.TriggerBarInfo> listTriggerBar, int currentBar)
			{
				if (listTriggerBar == null || listTriggerBar.Count == 0)
				{
					return;
				}
				for (int i = listTriggerBar.Count - 1; i >= 0; i--)
				{
					DDSonarlikeIcebergFinder.TriggerBarInfo triggerBarInfo = listTriggerBar[i];
					if (triggerBarInfo.IsBullish == this.TriggerBar.IsBullish)
					{
						this.TriggerBarCollection.Add(triggerBarInfo);
					}
				}
			}

		}
		private class SolarWaveInfo
		{
			public bool IsUptrend { get; set; }
			public double StopCurrentValue { get; set; }
			public int CountSlowdown { get; set; }
			public int SlowdownScan { get; set; }
			public int WeakWeakSplit { get; set; }
			public int NextWeakTrendBar { get; set; }
			public double OffsetBase { get; set; }
			public double OffsetLevel { get; set; }
			public Series<double> SeriesTrailingStop { get; set; }
			public Series<double> SeriesTrendVector { get; set; }
			public Series<int> SeriesSignalTrend { get; set; }
			
			public SolarWaveInfo(double offsetBase, double offsetLevel, int nextWeakTrendBar, int slowdownScan, int weakWeakSplit, Series<double> seriesTrailingStop, Series<double> seriesTrendVector, Series<int> seriesSignalTrend)
			{
				this.OffsetBase = Math.Max(offsetBase, offsetLevel);
				this.OffsetLevel = Math.Min(offsetBase, offsetLevel);
				this.NextWeakTrendBar = nextWeakTrendBar;
				this.SlowdownScan = slowdownScan;
				this.WeakWeakSplit = weakWeakSplit;
				this.SeriesTrailingStop = seriesTrailingStop;
				this.SeriesTrendVector = seriesTrendVector;
				this.SeriesSignalTrend = seriesSignalTrend;
			}

		}
		private class Presentation
		{
			public Dictionary<int, DDSonarlikeIcebergFinder.PresentationCell> DictCell { get; set; }
			public int POCKey { get; set; }
			public int POCBuyKey { get; set; }
			public int POCSellKey { get; set; }
			public int LowKey { get; set; }
			public int HighKey { get; set; }
			public double VolBuy { get; set; }
			public double VolSell { get; set; }
			public double VolDelta { get; set; }
			public double POCVol { get; set; }
			public double POCBuyVol { get; set; }
			public double POCSellVol { get; set; }
			
			internal Presentation()
			{
				this.DictCell = new Dictionary<int, DDSonarlikeIcebergFinder.PresentationCell>();
				this.VolBuy = (this.VolSell = (this.VolDelta = 0.0));
				this.POCKey = 0;
				this.POCBuyKey = 0;
				this.POCSellKey = 0;
				this.LowKey = int.MaxValue;
				this.HighKey = 0;
				this.POCVol = 0.0;
				this.POCBuyVol = 0.0;
				this.POCSellVol = 0.0;
			}
			
			internal DDSonarlikeIcebergFinder.PresentationCell GetValue(int key)
			{
				if (this.DictCell.ContainsKey(key))
				{
					return this.DictCell[key];
				}
				return new DDSonarlikeIcebergFinder.PresentationCell(0.0, 0.0, 0, 0.0, 0L, 0);
			}
			
			internal bool GetCellState(int key)
			{
				return this.DictCell.ContainsKey(key) && this.DictCell[key].CheckedVolume;
			}
			
			internal double GetTotalVol(int key)
			{
				return this.GetValue(key).VolBuy + this.GetValue(key).VolSell;
			}
			internal double GetTotalVol()
			{
				return this.VolBuy + this.VolSell;
			}
			internal double GetBuyVol(int key)
			{
				return this.GetValue(key).VolBuy;
			}
			internal double GetSellVol(int key)
			{
				return this.GetValue(key).VolSell;
			}
			
			internal void AddBuy(int key, double vol)
			{
				if (this.DictCell.ContainsKey(key))
				{
					this.DictCell[key].VolBuy += vol;
				}
				else
				{
					this.DictCell.Add(key, new DDSonarlikeIcebergFinder.PresentationCell(vol, 0.0, 0, 0.0, 0L, 0));
				}
				this.VolBuy += vol;
				this.VolDelta = this.VolBuy - this.VolSell;
				if (this.GetTotalVol(key) > this.POCVol)
				{
					this.POCKey = key;
					this.POCVol = this.GetTotalVol(key);
				}
				if (this.GetBuyVol(key) > this.POCBuyVol)
				{
					this.POCBuyKey = key;
					this.POCBuyVol = this.GetBuyVol(key);
				}
				this.LowKey = Math.Min(key, this.LowKey);
				this.HighKey = Math.Max(key, this.HighKey);
			}
			
			internal void AddSell(int key, double vol)
			{
				if (this.DictCell.ContainsKey(key))
				{
					this.DictCell[key].VolSell += vol;
				}
				else
				{
					this.DictCell.Add(key, new DDSonarlikeIcebergFinder.PresentationCell(0.0, vol, 0, 0.0, 0L, 0));
				}
				this.VolSell += vol;
				this.VolDelta = this.VolBuy - this.VolSell;
				if (this.GetTotalVol(key) > this.POCVol)
				{
					this.POCKey = key;
					this.POCVol = this.GetTotalVol(key);
				}
				if (this.GetSellVol(key) > this.POCSellVol)
				{
					this.POCSellKey = key;
					this.POCSellVol = this.GetSellVol(key);
				}
				this.LowKey = Math.Min(key, this.LowKey);
				this.HighKey = Math.Max(key, this.HighKey);
			}
			
			internal void AddVolume(bool isBuy, int key, double vol)
			{
				if (isBuy)
				{
					this.AddBuy(key, vol);
					return;
				}
				this.AddSell(key, vol);
			}
			internal void Clear()
			{
				this.DictCell.Clear();
			}
			

		}
		private class PresentationCell
		{
			public double VolBuy { get; set; }
			public double VolSell { get; set; }
			public double Price { get; set; }
			public bool CheckedVolume { get; set; }
			public int Key { get; set; }
			public long TimeTicks { get; set; }
			public long BarIndex { get; set; }
			
			internal PresentationCell(double volBuy, double volSell, int key = 0, double price = 0.0, long timeTicks = 0L, int barIndex = 0)
			{
				this.VolBuy = volBuy;
				this.VolSell = volSell;
				this.Key = key;
				this.Price = price;
				this.TimeTicks = timeTicks;
				this.BarIndex = (long)barIndex;
			}

		}
		
		public static class DD_BrushManager
		{
			public static global::System.Windows.Media.Brush CreateOpacityBrush(global::System.Windows.Media.Brush brush, int opacity)
			{
				if (brush == null) return global::System.Windows.Media.Brushes.Transparent;
				global::System.Windows.Media.Brush clone = brush.Clone();
				clone.Opacity = opacity / 100.0;
				clone.Freeze();
				return clone;
			}
		}
		
		private enum BarStateType
		{
			Range = 1,
			TriggerBar,
			Signal
		}

		public enum DDSonarlikeIcebergFinder_RenderingMethod
		{
			Builtin,
			Custom
		}
		public enum DDSonarlikeIcebergFinder_Position
		{
			Top,
			Bottom
		}
		public enum DDSonarlikeIcebergFinder_VolumeBase
		{

			BidAskPrice_RealVolume,
			UpDownTick_RealVolume,
			UpDownTick_UnitVolume
		}
		public enum DDSonarlikeIcebergFinder_Unit
		{
			Ticks,
			DDATR
		}
		
		public class DDSonarlikeIcebergFinder_Converter : IndicatorBaseConverter
		{
			public override PropertyDescriptorCollection GetProperties(ITypeDescriptorContext context, object component, Attribute[] attrs)
			{
				DDSonarlikeIcebergFinder DDSonarlikeIcebergFinder = component as DDSonarlikeIcebergFinder;
				PropertyDescriptorCollection propertyDescriptorCollection = (base.GetPropertiesSupported(context) ? base.GetProperties(context, component, attrs) : TypeDescriptor.GetProperties(component, attrs));
				if (DDSonarlikeIcebergFinder == null || propertyDescriptorCollection == null)
				{
					return propertyDescriptorCollection;
				}
				PropertyDescriptor propertyDescriptor = propertyDescriptorCollection["RangeATRPeriod"];
				PropertyDescriptor propertyDescriptor2 = propertyDescriptorCollection["RangeFactorATR"];
				PropertyDescriptor propertyDescriptor3 = propertyDescriptorCollection["RangeFactorTicks"];
				propertyDescriptorCollection.Remove(propertyDescriptor);
				propertyDescriptorCollection.Remove(propertyDescriptor2);
				propertyDescriptorCollection.Remove(propertyDescriptor3);
				if (DDSonarlikeIcebergFinder.RangeUnit == DimDim.DDSonarlikeIcebergFinder.DDSonarlikeIcebergFinder_Unit.DDATR)
				{
					propertyDescriptorCollection.Add(propertyDescriptor);
					propertyDescriptorCollection.Add(propertyDescriptor2);
				}
				else
				{
					propertyDescriptorCollection.Add(propertyDescriptor3);
				}
				return propertyDescriptorCollection;
			}
			public override bool GetPropertiesSupported(ITypeDescriptorContext context)
			{
				return true;
			}
		}
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private DimDim.DDSonarlikeIcebergFinder[] cacheDDSonarlikeIcebergFinder;
		public DimDim.DDSonarlikeIcebergFinder DDSonarlikeIcebergFinder(double volumeRatioStrong, int volumeDeltaPeriod, int triggerMinBars, double rangeFactorATR, double rangeFactorTicks, int rangeATRPeriod, int rangeFindingPeriod, int rangeMaxAge, int rangeMinAge, bool rangeFilterEnabled, int signalSplit)
		{
			return DDSonarlikeIcebergFinder(Input, volumeRatioStrong, volumeDeltaPeriod, triggerMinBars, rangeFactorATR, rangeFactorTicks, rangeATRPeriod, rangeFindingPeriod, rangeMaxAge, rangeMinAge, rangeFilterEnabled, signalSplit);
		}

		public DimDim.DDSonarlikeIcebergFinder DDSonarlikeIcebergFinder(ISeries<double> input, double volumeRatioStrong, int volumeDeltaPeriod, int triggerMinBars, double rangeFactorATR, double rangeFactorTicks, int rangeATRPeriod, int rangeFindingPeriod, int rangeMaxAge, int rangeMinAge, bool rangeFilterEnabled, int signalSplit)
		{
			if (cacheDDSonarlikeIcebergFinder != null)
				for (int idx = 0; idx < cacheDDSonarlikeIcebergFinder.Length; idx++)
					if (cacheDDSonarlikeIcebergFinder[idx] != null && cacheDDSonarlikeIcebergFinder[idx].VolumeRatioStrong == volumeRatioStrong && cacheDDSonarlikeIcebergFinder[idx].VolumeDeltaPeriod == volumeDeltaPeriod && cacheDDSonarlikeIcebergFinder[idx].TriggerMinBars == triggerMinBars && cacheDDSonarlikeIcebergFinder[idx].RangeFactorATR == rangeFactorATR && cacheDDSonarlikeIcebergFinder[idx].RangeFactorTicks == rangeFactorTicks && cacheDDSonarlikeIcebergFinder[idx].RangeATRPeriod == rangeATRPeriod && cacheDDSonarlikeIcebergFinder[idx].RangeFindingPeriod == rangeFindingPeriod && cacheDDSonarlikeIcebergFinder[idx].RangeMaxAge == rangeMaxAge && cacheDDSonarlikeIcebergFinder[idx].RangeMinAge == rangeMinAge && cacheDDSonarlikeIcebergFinder[idx].RangeFilterEnabled == rangeFilterEnabled && cacheDDSonarlikeIcebergFinder[idx].SignalSplit == signalSplit && cacheDDSonarlikeIcebergFinder[idx].EqualsInput(input))
						return cacheDDSonarlikeIcebergFinder[idx];
			return CacheIndicator<DimDim.DDSonarlikeIcebergFinder>(new DimDim.DDSonarlikeIcebergFinder(){ VolumeRatioStrong = volumeRatioStrong, VolumeDeltaPeriod = volumeDeltaPeriod, TriggerMinBars = triggerMinBars, RangeFactorATR = rangeFactorATR, RangeFactorTicks = rangeFactorTicks, RangeATRPeriod = rangeATRPeriod, RangeFindingPeriod = rangeFindingPeriod, RangeMaxAge = rangeMaxAge, RangeMinAge = rangeMinAge, RangeFilterEnabled = rangeFilterEnabled, SignalSplit = signalSplit }, input, ref cacheDDSonarlikeIcebergFinder);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.DimDim.DDSonarlikeIcebergFinder DDSonarlikeIcebergFinder(double volumeRatioStrong, int volumeDeltaPeriod, int triggerMinBars, double rangeFactorATR, double rangeFactorTicks, int rangeATRPeriod, int rangeFindingPeriod, int rangeMaxAge, int rangeMinAge, bool rangeFilterEnabled, int signalSplit)
		{
			return indicator.DDSonarlikeIcebergFinder(Input, volumeRatioStrong, volumeDeltaPeriod, triggerMinBars, rangeFactorATR, rangeFactorTicks, rangeATRPeriod, rangeFindingPeriod, rangeMaxAge, rangeMinAge, rangeFilterEnabled, signalSplit);
		}

		public Indicators.DimDim.DDSonarlikeIcebergFinder DDSonarlikeIcebergFinder(ISeries<double> input , double volumeRatioStrong, int volumeDeltaPeriod, int triggerMinBars, double rangeFactorATR, double rangeFactorTicks, int rangeATRPeriod, int rangeFindingPeriod, int rangeMaxAge, int rangeMinAge, bool rangeFilterEnabled, int signalSplit)
		{
			return indicator.DDSonarlikeIcebergFinder(input, volumeRatioStrong, volumeDeltaPeriod, triggerMinBars, rangeFactorATR, rangeFactorTicks, rangeATRPeriod, rangeFindingPeriod, rangeMaxAge, rangeMinAge, rangeFilterEnabled, signalSplit);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.DimDim.DDSonarlikeIcebergFinder DDSonarlikeIcebergFinder(double volumeRatioStrong, int volumeDeltaPeriod, int triggerMinBars, double rangeFactorATR, double rangeFactorTicks, int rangeATRPeriod, int rangeFindingPeriod, int rangeMaxAge, int rangeMinAge, bool rangeFilterEnabled, int signalSplit)
		{
			return indicator.DDSonarlikeIcebergFinder(Input, volumeRatioStrong, volumeDeltaPeriod, triggerMinBars, rangeFactorATR, rangeFactorTicks, rangeATRPeriod, rangeFindingPeriod, rangeMaxAge, rangeMinAge, rangeFilterEnabled, signalSplit);
		}

		public Indicators.DimDim.DDSonarlikeIcebergFinder DDSonarlikeIcebergFinder(ISeries<double> input , double volumeRatioStrong, int volumeDeltaPeriod, int triggerMinBars, double rangeFactorATR, double rangeFactorTicks, int rangeATRPeriod, int rangeFindingPeriod, int rangeMaxAge, int rangeMinAge, bool rangeFilterEnabled, int signalSplit)
		{
			return indicator.DDSonarlikeIcebergFinder(input, volumeRatioStrong, volumeDeltaPeriod, triggerMinBars, rangeFactorATR, rangeFactorTicks, rangeATRPeriod, rangeFindingPeriod, rangeMaxAge, rangeMinAge, rangeFilterEnabled, signalSplit);
		}
	}
}

#endregion
