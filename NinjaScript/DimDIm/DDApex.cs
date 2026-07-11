using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Core;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.Data;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.Tools;
using NinjaTrader.NinjaScript.DrawingTools;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;

namespace NinjaTrader.NinjaScript.Indicators.DimDim
{
	[CategoryOrder("General", 1000010)]
	[CategoryOrder("Graphics", 1000020)]
	[CategoryOrder("Alerts", 1000040)]
	[CategoryOrder("Toggle", 1000050)]
	[CategoryOrder("Gradient", 1000030)]
	[CategoryOrder("Signal Condition", 1000008)]
	[CategoryOrder("Special", 1000070)]
	[CategoryOrder("Windows", 1000060)]
	[CategoryOrder("Critical", 1000080)]
	[CategoryOrder("Parameters", 10)]
	[CategoryOrder("Developer", 0)]
	public class DDApexFlowZignal : Indicator
	{
		[Display(Name = "Marker: Enabled", Order = 60, GroupName = "Alerts")]
		public bool MarkerEnabled { get; set; }
		[Display(Name = "Marker: Rendering Method", Order = 62, GroupName = "Alerts", Description = "\"Custom\" rendering provides a better appearance, and is therefore recommended in most cases.\nIf your computer is weak or slow, please switch to \"Builtin\" rendering.")]
		public DDApexFlowZignal_RenderingMethod MarkerRenderingMethod { get; set; }
		[Display(Name = "Marker: Push Color Bullish", Order = 64, GroupName = "Alerts")]
		[XmlIgnore]
		public global::System.Windows.Media.Brush MarkerPushBrushBullish { get; set; }
		[Browsable(false)]
		public string MarkerPushBrushBullish_Serialize
		{
			get
			{
				return Serialize.BrushToString(this.MarkerPushBrushBullish);
			}
			set
			{
				this.MarkerPushBrushBullish = Serialize.StringToBrush(value);
			}
		}
		[Display(Name = "Marker: Push Color Bearish", Order = 66, GroupName = "Alerts")]
		[XmlIgnore]
		public global::System.Windows.Media.Brush MarkerPushBrushBearish { get; set; }
		[Browsable(false)]
		public string MarkerPushBrushBearish_Serialize
		{
			get
			{
				return Serialize.BrushToString(this.MarkerPushBrushBearish);
			}
			set
			{
				this.MarkerPushBrushBearish = Serialize.StringToBrush(value);
			}
		}
		[Display(Name = "Marker: Exhaustion Color Bullish", Order = 68, GroupName = "Alerts")]
		[XmlIgnore]
		public global::System.Windows.Media.Brush MarkerExhaustionBrushBullish { get; set; }
		[Browsable(false)]
		public string MarkerExhaustionBrushBullish_Serialize
		{
			get
			{
				return Serialize.BrushToString(this.MarkerExhaustionBrushBullish);
			}
			set
			{
				this.MarkerExhaustionBrushBullish = Serialize.StringToBrush(value);
			}
		}
		[XmlIgnore]
		[Display(Name = "Marker: Exhaustion Color Bearish", Order = 72, GroupName = "Alerts")]
		public global::System.Windows.Media.Brush MarkerExhaustionBrushBearish { get; set; }
		[Browsable(false)]
		public string MarkerExhaustionBrushBearish_Serialize
		{
			get
			{
				return Serialize.BrushToString(this.MarkerExhaustionBrushBearish);
			}
			set
			{
				this.MarkerExhaustionBrushBearish = Serialize.StringToBrush(value);
			}
		}
		[XmlIgnore]
		[Display(Name = "Marker: Absorption Color Bullish", Order = 74, GroupName = "Alerts")]
		public global::System.Windows.Media.Brush MarkerAbsorptionBrushBullish { get; set; }
		[Browsable(false)]
		public string MarkerAbsorptionBrushBullish_Serialize
		{
			get
			{
				return Serialize.BrushToString(this.MarkerAbsorptionBrushBullish);
			}
			set
			{
				this.MarkerAbsorptionBrushBullish = Serialize.StringToBrush(value);
			}
		}
		[XmlIgnore]
		[Display(Name = "Marker: Absorption Color Bearish", Order = 78, GroupName = "Alerts")]
		public global::System.Windows.Media.Brush MarkerAbsorptionBrushBearish { get; set; }
		[Browsable(false)]
		public string MarkerAbsorptionBrushBearish_Serialize
		{
			get
			{
				return Serialize.BrushToString(this.MarkerAbsorptionBrushBearish);
			}
			set
			{
				this.MarkerAbsorptionBrushBearish = Serialize.StringToBrush(value);
			}
		}
		[XmlIgnore]
		[Display(Name = "Marker: Ab-Push Color Bullish", Order = 82, GroupName = "Alerts")]
		public global::System.Windows.Media.Brush MarkerAbPushBrushBullish { get; set; }
		[Browsable(false)]
		public string MarkerAbPushBrushBullish_Serialize
		{
			get
			{
				return Serialize.BrushToString(this.MarkerAbPushBrushBullish);
			}
			set
			{
				this.MarkerAbPushBrushBullish = Serialize.StringToBrush(value);
			}
		}
		[Display(Name = "Marker: Ab-Push Color Bearish", Order = 86, GroupName = "Alerts")]
		[XmlIgnore]
		public global::System.Windows.Media.Brush MarkerAbPushBrushBearish { get; set; }
		[Browsable(false)]
		public string MarkerAbPushBrushBearish_Serialize
		{
			get
			{
				return Serialize.BrushToString(this.MarkerAbPushBrushBearish);
			}
			set
			{
				this.MarkerAbPushBrushBearish = Serialize.StringToBrush(value);
			}
		}
		[Display(Name = "Marker: Ex-Push Color Bullish", Order = 90, GroupName = "Alerts")]
		[XmlIgnore]
		public global::System.Windows.Media.Brush MarkerExPushBrushBullish { get; set; }
		[Browsable(false)]
		public string MarkerExPushBrushBullish_Serialize
		{
			get
			{
				return Serialize.BrushToString(this.MarkerExPushBrushBullish);
			}
			set
			{
				this.MarkerExPushBrushBullish = Serialize.StringToBrush(value);
			}
		}
		[XmlIgnore]
		[Display(Name = "Marker: Ex-Push Color Bearish", Order = 94, GroupName = "Alerts")]
		public global::System.Windows.Media.Brush MarkerExPushBrushBearish { get; set; }
		[Browsable(false)]
		public string MarkerExPushBrushBearish_Serialize
		{
			get
			{
				return Serialize.BrushToString(this.MarkerExPushBrushBearish);
			}
			set
			{
				this.MarkerExPushBrushBearish = Serialize.StringToBrush(value);
			}
		}
		[Display(Name = "Marker: Push String Bullish", Order = 100, GroupName = "Alerts")]
		public string MarkerPushStringBullish { get; set; }
		[Display(Name = "Marker: Push String Bearish", Order = 104, GroupName = "Alerts")]
		public string MarkerPushStringBearish { get; set; }
		[Display(Name = "Marker: Absorption String Bullish", Order = 108, GroupName = "Alerts")]
		public string MarkerAbsorptionStringBullish { get; set; }
		[Display(Name = "Marker: Absorption String Bearish", Order = 112, GroupName = "Alerts")]
		public string MarkerAbsorptionStringBearish { get; set; }
		[Display(Name = "Marker: Exhaustion String Bullish", Order = 116, GroupName = "Alerts")]
		public string MarkerExhaustionStringBullish { get; set; }
		[Display(Name = "Marker: Exhaustion String Bearish", Order = 120, GroupName = "Alerts")]
		public string MarkerExhaustionStringBearish { get; set; }
		[Display(Name = "Marker: Ab-Push String Bullish", Order = 124, GroupName = "Alerts")]
		public string MarkerAbPushStringBullish { get; set; }
		[Display(Name = "Marker: Ab-Push String Bearish", Order = 128, GroupName = "Alerts")]
		public string MarkerAbPushStringBearish { get; set; }
		[Display(Name = "Marker: Ex-Push String Bullish", Order = 132, GroupName = "Alerts")]
		public string MarkerExPushStringBullish { get; set; }
		[Display(Name = "Marker: Ex-Push String Bearish", Order = 136, GroupName = "Alerts")]
		public string MarkerExPushStringBearish { get; set; }
		[Display(Name = "Marker: Font", Order = 140, GroupName = "Alerts")]
		public SimpleFont MarkerFont { get; set; }
		[Display(Name = "Marker: Offset", Order = 144, GroupName = "Alerts")]
		public int MarkerOffset { get; set; }
		[Display(Name = "Alert Blocking (Seconds)", Order = 150, GroupName = "Alerts", Description = "The minimum interval between 2 consecutive alerts")]
		[Range(0, 2147483647)]
		public int AlertBlockingSeconds { get; set; }
		[Display(Name = "Website", Order = 0, GroupName = "Developer")]
		public string Website
		{
			get
			{
				return "DD.co";
			}
		}
		[Display(Name = "Update", Order = 10, GroupName = "Developer")]
		public new string Update
		{
			get
			{
				return "22 Jan 2026";
			}
		}
		[Display(Name = "Logo: Enabled", Order = 20, GroupName = "Developer")]
		public bool LogoEnabled { get; set; }
		[Display(Name = "Instruction: Enabled", Order = 30, GroupName = "Developer")]
		public bool InstructionEnabled { get; set; }
		[Display(Name = "Volume Average Area: Enabled", Order = 10, GroupName = "Graphics")]
		public bool VolumeAverageAreaEnabled { get; set; }
		[Display(Name = "Volume Average Area: Buy", Order = 14, GroupName = "Graphics")]
		[XmlIgnore]
		public global::System.Windows.Media.Brush VolumeAverageAreaBuy { get; set; }
		[Browsable(false)]
		public string VolumeAverageAreaBuy_Serialize
		{
			get
			{
				return Serialize.BrushToString(this.VolumeAverageAreaBuy);
			}
			set
			{
				this.VolumeAverageAreaBuy = Serialize.StringToBrush(value);
			}
		}
		[Display(Name = "Volume Average Area: Sell", Order = 18, GroupName = "Graphics")]
		[XmlIgnore]
		public global::System.Windows.Media.Brush VolumeAverageAreaSell { get; set; }
		[Browsable(false)]
		public string VolumeAverageAreaSell_Serialize
		{
			get
			{
				return Serialize.BrushToString(this.VolumeAverageAreaSell);
			}
			set
			{
				this.VolumeAverageAreaSell = Serialize.StringToBrush(value);
			}
		}
		[Range(1, 100)]
		[Display(Name = "Volume Average Area: Opacity", Order = 22, GroupName = "Graphics")]
		public int VolumeAverageAreaOpacity { get; set; }
		[Display(Name = "Presentation: Enabled", Order = 24, GroupName = "Graphics")]
		public bool PresentationEnabled { get; set; }
		[XmlIgnore]
		[Display(Name = "Theme: Buy Strong", Order = 26, GroupName = "Graphics")]
		public global::System.Windows.Media.Brush ThemeBuyStrong { get; set; }
		[Browsable(false)]
		public string ThemeBuyStrong_Serialize
		{
			get
			{
				return Serialize.BrushToString(this.ThemeBuyStrong);
			}
			set
			{
				this.ThemeBuyStrong = Serialize.StringToBrush(value);
			}
		}
		[XmlIgnore]
		[Display(Name = "Theme: Buy Weak", Order = 30, GroupName = "Graphics")]
		public global::System.Windows.Media.Brush ThemeBuyWeak { get; set; }
		[Browsable(false)]
		public string ThemeBuyWeak_Serialize
		{
			get
			{
				return Serialize.BrushToString(this.ThemeBuyWeak);
			}
			set
			{
				this.ThemeBuyWeak = Serialize.StringToBrush(value);
			}
		}
		[Display(Name = "Theme: Neutral", Order = 34, GroupName = "Graphics")]
		[XmlIgnore]
		public global::System.Windows.Media.Brush ThemeNeutral { get; set; }
		[Browsable(false)]
		public string ThemeNeutral_Serialize
		{
			get
			{
				return Serialize.BrushToString(this.ThemeNeutral);
			}
			set
			{
				this.ThemeNeutral = Serialize.StringToBrush(value);
			}
		}
		[XmlIgnore]
		[Display(Name = "Theme: Sell Weak", Order = 38, GroupName = "Graphics")]
		public global::System.Windows.Media.Brush ThemeSellWeak { get; set; }
		[Browsable(false)]
		public string ThemeSellWeak_Serialize
		{
			get
			{
				return Serialize.BrushToString(this.ThemeSellWeak);
			}
			set
			{
				this.ThemeSellWeak = Serialize.StringToBrush(value);
			}
		}
		[XmlIgnore]
		[Display(Name = "Theme: Sell Strong", Order = 42, GroupName = "Graphics")]
		public global::System.Windows.Media.Brush ThemeSellStrong { get; set; }
		[Browsable(false)]
		public string ThemeSellStrong_Serialize
		{
			get
			{
				return Serialize.BrushToString(this.ThemeSellStrong);
			}
			set
			{
				this.ThemeSellStrong = Serialize.StringToBrush(value);
			}
		}
		[Range(0, 100)]
		[Display(Name = "Theme: Opacity", Order = 46, GroupName = "Graphics")]
		public int ThemeOpacity { get; set; }
		[Display(Name = "Number: Enabled", Order = 50, GroupName = "Graphics")]
		public bool NumberEnabled { get; set; }
		[Display(Name = "Number: Color Default", Order = 54, GroupName = "Graphics")]
		[XmlIgnore]
		public global::System.Windows.Media.Brush NumberBrushDefault { get; set; }
		[Browsable(false)]
		public string NumberBrushDefault_Serialize
		{
			get
			{
				return Serialize.BrushToString(this.NumberBrushDefault);
			}
			set
			{
				this.NumberBrushDefault = Serialize.StringToBrush(value);
			}
		}
		[XmlIgnore]
		[Display(Name = "Number: Color Strong", Order = 58, GroupName = "Graphics")]
		public global::System.Windows.Media.Brush NumberBrushStrong { get; set; }
		[Browsable(false)]
		public string NumberBrushStrong_Serialize
		{
			get
			{
				return Serialize.BrushToString(this.NumberBrushStrong);
			}
			set
			{
				this.NumberBrushStrong = Serialize.StringToBrush(value);
			}
		}
		[Display(Name = "Number: Font Default", Order = 62, GroupName = "Graphics")]
		public SimpleFont NumberFontDefault { get; set; }
		[Display(Name = "Number: Font Strong", Order = 64, GroupName = "Graphics")]
		public SimpleFont NumberFontStrong { get; set; }
		[Display(Name = "Number: Margin", Order = 68, GroupName = "Graphics")]
		[Range(0, 2147483647)]
		public int NumberMargin { get; set; }
		[Display(Name = "POC Highlight : Enabled", Order = 72, GroupName = "Graphics")]
		public bool POCHighlightEnabled { get; set; }
		[Display(Name = "POC Highlight: Border Buy", Order = 74, GroupName = "Graphics")]
		public Stroke POCHighlightBorderBuyStroke { get; set; }
		[Display(Name = "POC Highlight: Border Neutral", Order = 78, GroupName = "Graphics")]
		public Stroke POCHighlightBorderNeutralStroke { get; set; }
		[Display(Name = "POC Highlight: Border Sell", Order = 82, GroupName = "Graphics")]
		public Stroke POCHighlightBorderSellStroke { get; set; }
		[Display(Name = "Strong Row Highlight: Color Buy", Order = 86, GroupName = "Graphics")]
		[XmlIgnore]
		public global::System.Windows.Media.Brush StrongRowHighlightBuyBrush { get; set; }
		[Browsable(false)]
		public string StrongRowHighlightBuyBrush_Serialize
		{
			get
			{
				return Serialize.BrushToString(this.StrongRowHighlightBuyBrush);
			}
			set
			{
				this.StrongRowHighlightBuyBrush = Serialize.StringToBrush(value);
			}
		}
		[Display(Name = "Strong Row Highlight: Color Neutral", Order = 90, GroupName = "Graphics")]
		[XmlIgnore]
		public global::System.Windows.Media.Brush StrongRowHighlightNeutralBrush { get; set; }
		[Browsable(false)]
		public string StrongRowHighlightNeutralBrush_Serialize
		{
			get
			{
				return Serialize.BrushToString(this.StrongRowHighlightNeutralBrush);
			}
			set
			{
				this.StrongRowHighlightNeutralBrush = Serialize.StringToBrush(value);
			}
		}
		[Display(Name = "Strong Row Highlight: Color Sell", Order = 94, GroupName = "Graphics")]
		[XmlIgnore]
		public global::System.Windows.Media.Brush StrongRowHighlightSellBrush { get; set; }
		[Browsable(false)]
		public string StrongRowHighlightSellBrush_Serialize
		{
			get
			{
				return Serialize.BrushToString(this.StrongRowHighlightSellBrush);
			}
			set
			{
				this.StrongRowHighlightSellBrush = Serialize.StringToBrush(value);
			}
		}
		[Display(Name = "Strong Row Highlight: Marker Enabled", Order = 96, GroupName = "Graphics")]
		public bool StrongRowHighlightMarkerEnabled { get; set; }
		[Display(Name = "Strong Row Highlight: Marker Radius", Order = 98, GroupName = "Graphics")]
		[Range(0, 2147483647)]
		public int StrongRowHighlightMarkerRadius { get; set; }
		[Display(Name = "Summary: Enabled", Order = 120, GroupName = "Graphics")]
		public bool SummaryEnabled { get; set; }
		[Display(Name = "Summary: Top", Order = 124, GroupName = "Graphics")]
		public bool SummaryTop { get; set; }
		[Display(Name = "Summary: Delta Positive", Order = 128, GroupName = "Graphics")]
		[XmlIgnore]
		public global::System.Windows.Media.Brush SummaryDeltaPositive { get; set; }
		[Browsable(false)]
		public string SummaryDeltaPositive_Serialize
		{
			get
			{
				return Serialize.BrushToString(this.SummaryDeltaPositive);
			}
			set
			{
				this.SummaryDeltaPositive = Serialize.StringToBrush(value);
			}
		}
		[XmlIgnore]
		[Display(Name = "Summary: Delta Zero", Order = 132, GroupName = "Graphics")]
		public global::System.Windows.Media.Brush SummaryDeltaZero { get; set; }
		[Browsable(false)]
		public string SummaryDeltaZero_Serialize
		{
			get
			{
				return Serialize.BrushToString(this.SummaryDeltaZero);
			}
			set
			{
				this.SummaryDeltaZero = Serialize.StringToBrush(value);
			}
		}
		[Display(Name = "Summary: Delta Negative", Order = 136, GroupName = "Graphics")]
		[XmlIgnore]
		public global::System.Windows.Media.Brush SummaryDeltaNegative { get; set; }
		[Browsable(false)]
		public string SummaryDeltaNegative_Serialize
		{
			get
			{
				return Serialize.BrushToString(this.SummaryDeltaNegative);
			}
			set
			{
				this.SummaryDeltaNegative = Serialize.StringToBrush(value);
			}
		}
		[Display(Name = "Summary: Number Only", Order = 140, GroupName = "Graphics")]
		public bool SummaryNumberOnly { get; set; }
		[Display(Name = "Summary: Number Font", Order = 144, GroupName = "Graphics")]
		public SimpleFont SummaryNumberFont { get; set; }
		[Range(0, 2147483647)]
		[Display(Name = "Summary: Margin", Order = 148, GroupName = "Graphics")]
		public int SummaryMargin { get; set; }
		[Display(Name = "Delta Chart: Enabled", Order = 152, GroupName = "Graphics")]
		public bool DeltaChartEnabled { get; set; }
		[Range(0, 100)]
		[Display(Name = "Delta Chart: Height (%)", Order = 156, GroupName = "Graphics")]
		public float DeltaChartHeightPercent { get; set; }
		[XmlIgnore]
		[Display(Name = "Delta Chart: Background Color", Order = 160, GroupName = "Graphics")]
		public global::System.Windows.Media.Brush DeltaChartBackground { get; set; }
		[Browsable(false)]
		public string DeltaChartBackgroundBrush_Serialize
		{
			get
			{
				return Serialize.BrushToString(this.DeltaChartBackground);
			}
			set
			{
				this.DeltaChartBackground = Serialize.StringToBrush(value);
			}
		}
		[Range(0, 100)]
		[Display(Name = "Delta Chart: Background Opacity", Order = 164, GroupName = "Graphics")]
		public int DeltaChartBackgroundOpacity { get; set; }
		[Display(Name = "Delta Chart: Axes Color", Order = 168, GroupName = "Graphics")]
		[XmlIgnore]
		public global::System.Windows.Media.Brush DeltaChartAxesBrush { get; set; }
		[Browsable(false)]
		public string DeltaChartAxesBrush_Serialize
		{
			get
			{
				return Serialize.BrushToString(this.DeltaChartAxesBrush);
			}
			set
			{
				this.DeltaChartAxesBrush = Serialize.StringToBrush(value);
			}
		}
		[Display(Name = "Delta Chart: Bar Positive", Order = 172, GroupName = "Graphics")]
		[XmlIgnore]
		public global::System.Windows.Media.Brush DeltaChartBarPositive { get; set; }
		[Browsable(false)]
		public string DeltaChartBarPositive_Serialize
		{
			get
			{
				return Serialize.BrushToString(this.DeltaChartBarPositive);
			}
			set
			{
				this.DeltaChartBarPositive = Serialize.StringToBrush(value);
			}
		}
		[XmlIgnore]
		[Display(Name = "Delta Chart: Bar Neutral", Order = 176, GroupName = "Graphics")]
		public global::System.Windows.Media.Brush DeltaChartBarNeutral { get; set; }
		[Browsable(false)]
		public string DeltaChartBarNeutral_Serialize
		{
			get
			{
				return Serialize.BrushToString(this.DeltaChartBarNeutral);
			}
			set
			{
				this.DeltaChartBarNeutral = Serialize.StringToBrush(value);
			}
		}
		[Display(Name = "Delta Chart: Bar Negative", Order = 180, GroupName = "Graphics")]
		[XmlIgnore]
		public global::System.Windows.Media.Brush DeltaChartBarNegative { get; set; }
		[Browsable(false)]
		public string DeltaChartBarNegative_Serialize
		{
			get
			{
				return Serialize.BrushToString(this.DeltaChartBarNegative);
			}
			set
			{
				this.DeltaChartBarNegative = Serialize.StringToBrush(value);
			}
		}
		[Range(0, 100)]
		[Display(Name = "Delta Chart: Bar Opacity", Order = 184, GroupName = "Graphics")]
		public int DeltaChartOpacity { get; set; }
		[Display(Name = "Delta Chart: Threshold Enabled", Order = 188, GroupName = "Graphics")]
		public bool DeltaChartPlotEnabled { get; set; }
		[Display(Name = "Delta Chart: Threshold Positive", Order = 192, GroupName = "Graphics")]
		public Stroke DeltaChartThresholdPositive { get; set; }
		[Display(Name = "Delta Chart: Threshold Negative", Order = 196, GroupName = "Graphics")]
		public Stroke DeltaChartThresholdNegative { get; set; }
		[Display(Name = "Volume Base", GroupName = "Parameters", Order = 10)]
		public DDApexFlowZignal_VolumeBase VolumeBase { get; set; }
		[Range(1, 2147483647)]
		[NinjaScriptProperty]
		[Display(Name = "Avg Volume Period", GroupName = "Parameters", Order = 42)]
		public int AvgVolPeriod { get; set; }
		[Range(1, 2147483647)]
		[NinjaScriptProperty]
		[Display(Name = "Avg Delta Period", GroupName = "Parameters", Order = 44)]
		public int AvgDeltaPeriod { get; set; }
		[Range(1, 2147483647)]
		[NinjaScriptProperty]
		[Display(Name = "Neutral Range (%)", GroupName = "Parameters", Order = 46)]
		public int NeutralRange { get; set; }
		[Range(1, 2147483647)]
		[NinjaScriptProperty]
		[Display(Name = "N: Absorption", GroupName = "Parameters", Order = 48)]
		public int AbsorptionN { get; set; }
		[Display(Name = "N: Exhaustion", GroupName = "Parameters", Order = 50)]
		[NinjaScriptProperty]
		[Range(1, 2147483647)]
		public int ExhaustionN { get; set; }
		[Display(Name = "N: Push", GroupName = "Parameters", Order = 52)]
		[Range(1, 2147483647)]
		[NinjaScriptProperty]
		public int PushN { get; set; }
		[Display(Name = "Push: Min Body (Ticks)", GroupName = "Parameters", Order = 54)]
		[Range(1, 2147483647)]
		[NinjaScriptProperty]
		public int MinBodyTicks { get; set; }
		[Display(Name = "Push Bullish: Max Upper Wick (%)", GroupName = "Parameters", Order = 56)]
		[NinjaScriptProperty]
		[Range(1, 100)]
		public int MaxWickPercentBullish { get; set; }
		[Range(1, 100)]
		[NinjaScriptProperty]
		[Display(Name = "Push Bullish: Min Body (%)", GroupName = "Parameters", Order = 58)]
		public int MinBodyPercentBullish { get; set; }
		[NinjaScriptProperty]
		[Range(1, 100)]
		[Display(Name = "Push Bearish: Max Lower Wick (%)", GroupName = "Parameters", Order = 60)]
		public int MaxWickPercentBearish { get; set; }
		[Display(Name = "Push Bearish: Min Body (%)", GroupName = "Parameters", Order = 62)]
		[Range(1, 100)]
		[NinjaScriptProperty]
		public int MinBodyPercentBearish { get; set; }
		[NinjaScriptProperty]
		[Range(1, 2147483647)]
		[Display(Name = "Signal: Ab + Push Period (Bars)", GroupName = "Parameters", Order = 64)]
		public int AbPushPeriod { get; set; }
		[Range(1, 2147483647)]
		[Display(Name = "Signal: Ex + Push Period (Bars)", GroupName = "Parameters", Order = 66)]
		[NinjaScriptProperty]
		public int ExPushPeriod { get; set; }
		[NinjaScriptProperty]
		[Display(Name = "Condition Absorption: Enabled", Order = 10, GroupName = "Signal Condition")]
		public bool SCAbsorptionEnabled { get; set; }
		[NinjaScriptProperty]
		[Display(Name = "Condition Absorption: Bullish", Order = 14, GroupName = "Signal Condition")]
		public bool SCAbsorptionBullish { get; set; }
		[Display(Name = "Condition Absorption: Bearish", Order = 18, GroupName = "Signal Condition")]
		[NinjaScriptProperty]
		public bool SCAbsorptionBearish { get; set; }
		[NinjaScriptProperty]
		[Display(Name = "Condition Exhaustion: Enabled", Order = 20, GroupName = "Signal Condition")]
		public bool SCExhaustionEnabled { get; set; }
		[NinjaScriptProperty]
		[Display(Name = "Condition Exhaustion: Bullish", Order = 24, GroupName = "Signal Condition")]
		public bool SCExhaustionBullish { get; set; }
		[Display(Name = "Condition Exhaustion: Bearish", Order = 28, GroupName = "Signal Condition")]
		[NinjaScriptProperty]
		public bool SCExhaustionBearish { get; set; }
		[Display(Name = "Condition Push: Enabled", Order = 30, GroupName = "Signal Condition")]
		[NinjaScriptProperty]
		public bool SCPushEnabled { get; set; }
		[Display(Name = "Condition Push: Bullish", Order = 34, GroupName = "Signal Condition")]
		[NinjaScriptProperty]
		public bool SCPushBullish { get; set; }
		[NinjaScriptProperty]
		[Display(Name = "Condition Push: Bearish", Order = 38, GroupName = "Signal Condition")]
		public bool SCPushBearish { get; set; }
		[Display(Name = "Condition Ab + Push: Enabled", Order = 40, GroupName = "Signal Condition")]
		[NinjaScriptProperty]
		public bool SCAbPushEnabled { get; set; }
		[NinjaScriptProperty]
		[Display(Name = "Condition Ab + Push: Bullish", Order = 44, GroupName = "Signal Condition")]
		public bool SCAbPushBullish { get; set; }
		[NinjaScriptProperty]
		[Display(Name = "Condition Ab + Push: Bearish", Order = 48, GroupName = "Signal Condition")]
		public bool SCAbPushBearish { get; set; }
		[NinjaScriptProperty]
		[Display(Name = "Condition Ex + Push: Enabled", Order = 50, GroupName = "Signal Condition")]
		public bool SCExPushEnabled { get; set; }
		[Display(Name = "Condition Ex + Push: Bullish", Order = 54, GroupName = "Signal Condition")]
		[NinjaScriptProperty]
		public bool SCExPushBullish { get; set; }
		[Display(Name = "Condition Ex + Push: Bearish", Order = 58, GroupName = "Signal Condition")]
		[NinjaScriptProperty]
		public bool SCExPushBearish { get; set; }
		[Browsable(false)]
		[DDApexFlowZignal.ConditionPropertiesAttribute(Key = 0, SignalType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.SignalType.Absorption, IsHeaderNote = true, HeaderLeft = "Strong Sell effort", HeaderRight = "Strong Buy effort ", IsOppositeDirection = true)]
		public bool ArsorptionHeader1 { get; set; }
		[NinjaScriptProperty]
		[Display(Name = "Absorption Bullish: Delta Negative", Order = 70, GroupName = "Signal Condition")]
		[DDApexFlowZignal.ConditionPropertiesAttribute(Key = 2, ConditionText = "Negative Delta", SignalType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.SignalType.Absorption, IsBuy = true, FuncType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.FuncType.ConditionDirection, DataType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.DataType.VolumeDelta, ConditionType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.ConditionType.Down)]
		public bool SCAbsorptionBullishDirectionDeltaEnabled { get; set; }
		[NinjaScriptProperty]
		[DDApexFlowZignal.ConditionPropertiesAttribute(Key = 4, ConditionText = "Negative Delta exceeds average threshold", SignalType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.SignalType.Absorption, IsBuy = true, FuncType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.FuncType.ConditionDeltaAverage, DataType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.DataType.VolumeNegativeDelta, ConditionType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.ConditionType.Above)]
		[Display(Name = "Absorption Bullish: Negative Delta Above Avg", Order = 72, GroupName = "Signal Condition")]
		public bool SCAbsorptionBullishDeltaNegativeAvgEnabled { get; set; }
		[NinjaScriptProperty]
		[Display(Name = "Absorption Bullish: Volume Sell Above Avg", Order = 74, GroupName = "Signal Condition")]
		[DDApexFlowZignal.ConditionPropertiesAttribute(Key = 6, ConditionText = "Sell Volume exceeds average threshold", SignalType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.SignalType.Absorption, IsBuy = true, FuncType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.FuncType.ConditionVolumeAvg, DataType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.DataType.VolumeSell, ConditionType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.ConditionType.Above)]
		public bool SCAbsorptionBullishVolumeAvgSellEnabled { get; set; }
		[Display(Name = "Absorption Bullish: Volume Sell Extremum", Order = 78, GroupName = "Signal Condition")]
		[NinjaScriptProperty]
		[DDApexFlowZignal.ConditionPropertiesAttribute(Key = 8, ConditionText = "Maximum Sell volume over N bars", SignalType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.SignalType.Absorption, IsBuy = true, FuncType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.FuncType.ConditionVolumeExtrema, DataType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.DataType.VolumeSell, ConditionType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.ConditionType.Max)]
		public bool SCAbsorptionBullishVolumeExtremaSellEnabled { get; set; }
		[DDApexFlowZignal.ConditionPropertiesAttribute(Key = 10, SignalType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.SignalType.Absorption, IsHeaderNote = true, HeaderLeft = "Insignificant Buy-side outcome", HeaderRight = "Insignificant Sell-side outcome", IsOppositeDirection = false, IsWeak = true)]
		[Browsable(false)]
		public string ArsorptionHeader2 { get; set; }
		[Display(Name = "Absorption Bullish: Bar Direction Up", Order = 82, GroupName = "Signal Condition")]
		[DDApexFlowZignal.ConditionPropertiesAttribute(Key = 12, ConditionText = "Bullish bar", SignalType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.SignalType.Absorption, IsBuy = true, FuncType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.FuncType.ConditionDirection, DataType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.DataType.Bar, ConditionType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.ConditionType.Up)]
		[NinjaScriptProperty]
		public bool SCAbsorptionBullishDirectionBarEnabled { get; set; }
		[DDApexFlowZignal.ConditionPropertiesAttribute(Key = 14, ConditionText = "Close price above neutral range", SignalType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.SignalType.Absorption, IsBuy = true, FuncType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.FuncType.ConditionNeutralRange, DataType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.DataType.Close, ConditionType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.ConditionType.Above)]
		[NinjaScriptProperty]
		[Display(Name = "Absorption Bullish: Close Above Neutral", Order = 86, GroupName = "Signal Condition")]
		public bool SCAbsorptionBullishNeutralRangeCloseEnabled { get; set; }
		[DDApexFlowZignal.ConditionPropertiesAttribute(Key = 16, ConditionText = "Body size is not maximum over N bars", SignalType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.SignalType.Absorption, IsBuy = true, FuncType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.FuncType.ConditionBodyOrSpreadNonExtreme, DataType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.DataType.Body, ConditionType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.ConditionType.Max)]
		[Display(Name = "Absorption Bullish: Body Non-Extreme", Order = 90, GroupName = "Signal Condition")]
		[NinjaScriptProperty]
		public bool SCAbsorptionBullishNonExtremeBodyEnabled { get; set; }
		[Display(Name = "Absorption Bullish: Spread Non-Extreme", Order = 94, GroupName = "Signal Condition")]
		[NinjaScriptProperty]
		[DDApexFlowZignal.ConditionPropertiesAttribute(Key = 18, ConditionText = "Range is not maximum over N bars", SignalType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.SignalType.Absorption, IsBuy = true, FuncType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.FuncType.ConditionBodyOrSpreadNonExtreme, DataType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.DataType.Spread, ConditionType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.ConditionType.Max)]
		public bool SCAbsorptionBullishNonExtremeSpreadEnabled { get; set; }
		[NinjaScriptProperty]
		[Display(Name = "Absorption Bearish: Delta Positive", Order = 98, GroupName = "Signal Condition")]
		[DDApexFlowZignal.ConditionPropertiesAttribute(Key = 2, ConditionText = "Positive Delta", SignalType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.SignalType.Absorption, IsBuy = false, FuncType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.FuncType.ConditionDirection, DataType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.DataType.VolumeDelta, ConditionType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.ConditionType.Up)]
		public bool SCAbsorptionBearishDirectionDeltaEnabled { get; set; }
		[NinjaScriptProperty]
		[Display(Name = "Absorption Bearish: Positive Delta Above Avg", Order = 100, GroupName = "Signal Condition")]
		[DDApexFlowZignal.ConditionPropertiesAttribute(Key = 4, ConditionText = "Positive Delta exceeds average threshold", SignalType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.SignalType.Absorption, IsBuy = false, FuncType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.FuncType.ConditionDeltaAverage, DataType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.DataType.VolumePostiveDelta, ConditionType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.ConditionType.Above)]
		public bool SCAbsorptionBearishDeltaPositiveAvgEnabled { get; set; }
		[DDApexFlowZignal.ConditionPropertiesAttribute(Key = 6, ConditionText = "Buy Volume exceeds average threshold", SignalType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.SignalType.Absorption, IsBuy = false, FuncType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.FuncType.ConditionVolumeAvg, DataType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.DataType.VolumeBuy, ConditionType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.ConditionType.Above)]
		[Display(Name = "Absorption Bearish: Volume Buy Above Avg", Order = 102, GroupName = "Signal Condition")]
		[NinjaScriptProperty]
		public bool SCAbsorptionBearishVolumeAvgBuyEnabled { get; set; }
		[NinjaScriptProperty]
		[Display(Name = "Absorption Bearish: Volume Buy Extremum", Order = 106, GroupName = "Signal Condition")]
		[DDApexFlowZignal.ConditionPropertiesAttribute(Key = 8, ConditionText = "Maximum Buy volume over N bars", SignalType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.SignalType.Absorption, IsBuy = false, FuncType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.FuncType.ConditionVolumeExtrema, DataType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.DataType.VolumeBuy, ConditionType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.ConditionType.Max)]
		public bool SCAbsorptionBearishVolumeExtremaBuyEnabled { get; set; }
		[Display(Name = "Absorption Bearish: Bar Direction Down", Order = 110, GroupName = "Signal Condition")]
		[DDApexFlowZignal.ConditionPropertiesAttribute(Key = 12, ConditionText = "Bearish bar", SignalType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.SignalType.Absorption, IsBuy = false, FuncType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.FuncType.ConditionDirection, DataType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.DataType.Bar, ConditionType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.ConditionType.Down)]
		[NinjaScriptProperty]
		public bool SCAbsorptionBearishDirectionBarEnabled { get; set; }
		[DDApexFlowZignal.ConditionPropertiesAttribute(Key = 14, ConditionText = "Close price below neutral range", SignalType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.SignalType.Absorption, IsBuy = false, FuncType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.FuncType.ConditionNeutralRange, DataType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.DataType.Close, ConditionType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.ConditionType.Below)]
		[NinjaScriptProperty]
		[Display(Name = "Absorption Bearish: Close Below Neutral", Order = 114, GroupName = "Signal Condition")]
		public bool SCAbsorptionBearishNeutralRangeCloseEnabled { get; set; }
		[Display(Name = "Absorption Bearish: Body Non-Extreme", Order = 118, GroupName = "Signal Condition")]
		[DDApexFlowZignal.ConditionPropertiesAttribute(Key = 16, ConditionText = "Body size is not maximum over N bars", SignalType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.SignalType.Absorption, IsBuy = false, FuncType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.FuncType.ConditionBodyOrSpreadNonExtreme, DataType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.DataType.Body, ConditionType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.ConditionType.Max)]
		[NinjaScriptProperty]
		public bool SCAbsorptionBearishNonExtremeBodyEnabled { get; set; }
		[Display(Name = "Absorption Bearish: Spread Non-Extreme", Order = 122, GroupName = "Signal Condition")]
		[DDApexFlowZignal.ConditionPropertiesAttribute(Key = 18, ConditionText = "Range is not maximum over N bars", SignalType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.SignalType.Absorption, IsBuy = false, FuncType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.FuncType.ConditionBodyOrSpreadNonExtreme, DataType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.DataType.Spread, ConditionType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.ConditionType.Max)]
		[NinjaScriptProperty]
		public bool SCAbsorptionBearishNonExtremeSpreadEnabled { get; set; }
		[DDApexFlowZignal.ConditionPropertiesAttribute(Key = 0, SignalType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.SignalType.Exhaustion, IsHeaderNote = true, HeaderLeft = "Sell momentum decreasing", HeaderRight = "Buy momentum decreasing", IsOppositeDirection = true, IsWeak = true)]
		[Browsable(false)]
		public bool ExhaustionHeader1 { get; set; }
		[Display(Name = "Exhaustion Bullish: Bar Decreasing", Order = 126, GroupName = "Signal Condition")]
		[DDApexFlowZignal.ConditionPropertiesAttribute(Key = 1, ConditionText = "N consecutive bearish bars", SignalType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.SignalType.Exhaustion, IsBuy = true, FuncType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.FuncType.ConditionConsecutiveBar, DataType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.DataType.Bar, ConditionType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.ConditionType.Decreasing)]
		[NinjaScriptProperty]
		public bool SCExhaustionBullishConsecutiveBarEnabled { get; set; }
		[DDApexFlowZignal.ConditionPropertiesAttribute(Key = 2, ConditionText = "Body decreasing over N bars", SignalType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.SignalType.Exhaustion, IsBuy = true, FuncType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.FuncType.ConditionConsecutiveBodyOrSpread, DataType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.DataType.Body, ConditionType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.ConditionType.Decreasing)]
		[NinjaScriptProperty]
		[Display(Name = "Exhaustion Bullish: Body Decreasing", Order = 130, GroupName = "Signal Condition")]
		public bool SCExhaustionBullishConsecutiveBodyEnabled { get; set; }
		[DDApexFlowZignal.ConditionPropertiesAttribute(Key = 3, ConditionText = "Range decreasing over N bars", SignalType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.SignalType.Exhaustion, IsBuy = true, FuncType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.FuncType.ConditionConsecutiveBodyOrSpread, DataType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.DataType.Spread, ConditionType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.ConditionType.Decreasing)]
		[NinjaScriptProperty]
		[Display(Name = "Exhaustion Bullish: Spread Decreasing", Order = 134, GroupName = "Signal Condition")]
		public bool SCExhaustionBullishConsecutiveSpreadEnabled { get; set; }
		[Display(Name = "Exhaustion Bullish: Volume Decreasing", Order = 138, GroupName = "Signal Condition")]
		[DDApexFlowZignal.ConditionPropertiesAttribute(Key = 4, ConditionText = "Total volume decreasing over N bars", SignalType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.SignalType.Exhaustion, IsBuy = true, FuncType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.FuncType.ConditionConsecutiveVolume, DataType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.DataType.Volume, ConditionType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.ConditionType.Decreasing)]
		[NinjaScriptProperty]
		public bool SCExhaustionBullishConsecutiveVolumeEnabled { get; set; }
		[DDApexFlowZignal.ConditionPropertiesAttribute(Key = 5, ConditionText = "Sell volume decreasing over N bars", SignalType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.SignalType.Exhaustion, IsBuy = true, FuncType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.FuncType.ConditionConsecutiveVolume, DataType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.DataType.VolumeSell, ConditionType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.ConditionType.Decreasing)]
		[NinjaScriptProperty]
		[Display(Name = "Exhaustion Bullish: Sell Volume Decreasing", Order = 142, GroupName = "Signal Condition")]
		public bool SCExhaustionBullishConsecutiveVolumeSellEnabled { get; set; }
		[Display(Name = "Exhaustion Bullish: Negative Delta Below Avg", Order = 146, GroupName = "Signal Condition")]
		[DDApexFlowZignal.ConditionPropertiesAttribute(Key = 6, ConditionText = "Negative Delta less than average threshold", SignalType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.SignalType.Exhaustion, IsBuy = true, FuncType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.FuncType.ConditionDeltaAverage, DataType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.DataType.VolumeNegativeDelta, ConditionType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.ConditionType.Below)]
		[NinjaScriptProperty]
		public bool SCExhaustionBullishDeltaNegativeAvgEnabled { get; set; }
		[Display(Name = "Exhaustion Bullish: Sell Volume Below Avg", Order = 150, GroupName = "Signal Condition")]
		[DDApexFlowZignal.ConditionPropertiesAttribute(Key = 7, ConditionText = "Sell volume less than average threshold", SignalType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.SignalType.Exhaustion, IsBuy = true, FuncType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.FuncType.ConditionVolumeAvg, DataType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.DataType.VolumeSell, ConditionType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.ConditionType.Below)]
		[NinjaScriptProperty]
		public bool SCExhaustionBullishVolumeAvgSellEnabled { get; set; }
		[DDApexFlowZignal.ConditionPropertiesAttribute(Key = 1, ConditionText = "N consecutive bullish bars ", SignalType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.SignalType.Exhaustion, IsBuy = false, FuncType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.FuncType.ConditionConsecutiveBar, DataType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.DataType.Bar, ConditionType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.ConditionType.Increasing)]
		[Display(Name = "Exhaustion Bearish: Bar Increasing", Order = 154, GroupName = "Signal Condition")]
		[NinjaScriptProperty]
		public bool SCExhaustionBearishConsecutiveBarEnabled { get; set; }
		[Display(Name = "Exhaustion Bearish: Body Decreasing", Order = 158, GroupName = "Signal Condition")]
		[DDApexFlowZignal.ConditionPropertiesAttribute(Key = 2, ConditionText = "Body decreasing over N bars", SignalType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.SignalType.Exhaustion, IsBuy = false, FuncType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.FuncType.ConditionConsecutiveBodyOrSpread, DataType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.DataType.Body, ConditionType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.ConditionType.Decreasing)]
		[NinjaScriptProperty]
		public bool SCExhaustionBearishConsecutiveBodyEnabled { get; set; }
		[Display(Name = "Exhaustion Bearish: Spread Decreasing", Order = 162, GroupName = "Signal Condition")]
		[DDApexFlowZignal.ConditionPropertiesAttribute(Key = 3, ConditionText = "Range decreasing over N bars", SignalType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.SignalType.Exhaustion, IsBuy = false, FuncType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.FuncType.ConditionConsecutiveBodyOrSpread, DataType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.DataType.Spread, ConditionType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.ConditionType.Decreasing)]
		[NinjaScriptProperty]
		public bool SCExhaustionBearishConsecutiveSpreadEnabled { get; set; }
		[DDApexFlowZignal.ConditionPropertiesAttribute(Key = 4, ConditionText = "Total volume decreasing over N bars", SignalType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.SignalType.Exhaustion, IsBuy = false, FuncType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.FuncType.ConditionConsecutiveVolume, DataType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.DataType.Volume, ConditionType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.ConditionType.Decreasing)]
		[Display(Name = "Exhaustion Bearish: Volume Decreasing", Order = 166, GroupName = "Signal Condition")]
		[NinjaScriptProperty]
		public bool SCExhaustionBearishConsecutiveVolumeEnabled { get; set; }
		[DDApexFlowZignal.ConditionPropertiesAttribute(Key = 5, ConditionText = "Buy volume decreasing over N bars", SignalType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.SignalType.Exhaustion, IsBuy = false, FuncType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.FuncType.ConditionConsecutiveVolume, DataType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.DataType.VolumeBuy, ConditionType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.ConditionType.Decreasing)]
		[Display(Name = "Exhaustion Bearish: Buy Volume Decreasing", Order = 170, GroupName = "Signal Condition")]
		[NinjaScriptProperty]
		public bool SCExhaustionBearishConsecutiveVolumeBuyEnabled { get; set; }
		[DDApexFlowZignal.ConditionPropertiesAttribute(Key = 6, ConditionText = "Positive Delta less than average threshold", SignalType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.SignalType.Exhaustion, IsBuy = false, FuncType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.FuncType.ConditionDeltaAverage, DataType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.DataType.VolumePostiveDelta, ConditionType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.ConditionType.Below)]
		[NinjaScriptProperty]
		[Display(Name = "Exhaustion Bearish: Positive Delta Below Avg", Order = 174, GroupName = "Signal Condition")]
		public bool SCExhaustionBearishDeltaPositiveAvgEnabled { get; set; }
		[NinjaScriptProperty]
		[Display(Name = "Exhaustion Bearish: Buy Volume Below Avg", Order = 178, GroupName = "Signal Condition")]
		[DDApexFlowZignal.ConditionPropertiesAttribute(Key = 7, ConditionText = "Buy volume less than average threshold", SignalType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.SignalType.Exhaustion, IsBuy = false, FuncType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.FuncType.ConditionVolumeAvg, DataType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.DataType.VolumeBuy, ConditionType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.ConditionType.Below)]
		public bool SCExhaustionBearishVolumeAvgBuyEnabled { get; set; }
		[DDApexFlowZignal.ConditionPropertiesAttribute(Key = 0, SignalType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.SignalType.Push, IsHeaderNote = true, HeaderLeft = "Buy-side participation & effort", HeaderRight = "Sell-side participation & effort", IsOppositeDirection = false)]
		[Browsable(false)]
		public bool PushHeader1 { get; set; }
		[DDApexFlowZignal.ConditionPropertiesAttribute(Key = 1, ConditionText = "Maximum Buy volume over N bars ", SignalType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.SignalType.Push, IsBuy = true, FuncType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.FuncType.ConditionVolumeExtrema, DataType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.DataType.VolumeBuy, ConditionType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.ConditionType.Max)]
		[Display(Name = "Push Bullish: Buy Volume Extremum", Order = 182, GroupName = "Signal Condition")]
		[NinjaScriptProperty]
		public bool SCPushBullishVolumeExtremaBuyEnabled { get; set; }
		[Display(Name = "Push Bullish: Delta Direction Positive", Order = 194, GroupName = "Signal Condition")]
		[DDApexFlowZignal.ConditionPropertiesAttribute(Key = 2, ConditionText = "Positive Delta", SignalType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.SignalType.Push, IsBuy = true, FuncType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.FuncType.ConditionDirection, DataType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.DataType.VolumeDelta, ConditionType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.ConditionType.Up)]
		[NinjaScriptProperty]
		public bool SCPushBullishDirectionDeltaEnabled { get; set; }
		[NinjaScriptProperty]
		[Display(Name = "Push Bullish: Positive Delta Above Avg", Order = 206, GroupName = "Signal Condition")]
		[DDApexFlowZignal.ConditionPropertiesAttribute(Key = 3, ConditionText = "Positive Delta above average threshold", SignalType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.SignalType.Push, IsBuy = true, FuncType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.FuncType.ConditionDeltaAverage, DataType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.DataType.VolumePostiveDelta, ConditionType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.ConditionType.Above)]
		public bool SCPushBullishDeltaPositiveAvgEnabled { get; set; }
		[DDApexFlowZignal.ConditionPropertiesAttribute(Key = 4, ConditionText = "Buy Volume above average threshold", SignalType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.SignalType.Push, IsBuy = true, FuncType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.FuncType.ConditionVolumeAvg, DataType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.DataType.VolumeBuy, ConditionType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.ConditionType.Above)]
		[NinjaScriptProperty]
		[Display(Name = "Push Bullish: Buy Volume Above Avg", Order = 210, GroupName = "Signal Condition")]
		public bool SCPushBullishVolumeAvgBuyEnabled { get; set; }
		[NinjaScriptProperty]
		[Display(Name = "Push Bullish: POC Direction Up", Order = 198, GroupName = "Signal Condition")]
		[DDApexFlowZignal.ConditionPropertiesAttribute(Key = 5, ConditionText = "Bullish POC bias", SignalType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.SignalType.Push, IsBuy = true, FuncType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.FuncType.ConditionDirection, DataType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.DataType.POC, ConditionType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.ConditionType.Up)]
		public bool SCPushBullishDirectionPOCEnabled { get; set; }
		[DDApexFlowZignal.ConditionPropertiesAttribute(Key = 6, ConditionText = "POC above neutral range", SignalType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.SignalType.Push, IsBuy = true, FuncType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.FuncType.ConditionNeutralRange, DataType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.DataType.POC, ConditionType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.ConditionType.Above)]
		[Display(Name = "Push Bullish: POC Above Neutral", Order = 214, GroupName = "Signal Condition")]
		[NinjaScriptProperty]
		public bool SCPushBullishNeutralRangePOCEnabled { get; set; }
		[Browsable(false)]
		[DDApexFlowZignal.ConditionPropertiesAttribute(Key = 7, SignalType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.SignalType.Push, IsHeaderNote = true, HeaderLeft = "Buy-side results clearly dominant", HeaderRight = "Sell-side results clearly dominant", IsOppositeDirection = false)]
		public bool PushHeader2 { get; set; }
		[Display(Name = "Push Bullish: Body Extremum", Order = 186, GroupName = "Signal Condition")]
		[DDApexFlowZignal.ConditionPropertiesAttribute(Key = 8, ConditionText = "Body size is maximum over N bars", SignalType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.SignalType.Push, IsBuy = true, FuncType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.FuncType.ConditionBodyOrSpreadExtrema, DataType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.DataType.Body, ConditionType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.ConditionType.Max)]
		[NinjaScriptProperty]
		public bool SCPushBullishBodyExtremaEnabled { get; set; }
		[Display(Name = "Push Bullish: Spread Extremum", Order = 190, GroupName = "Signal Condition")]
		[DDApexFlowZignal.ConditionPropertiesAttribute(Key = 9, ConditionText = "Range is maximum over N bars", SignalType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.SignalType.Push, IsBuy = true, FuncType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.FuncType.ConditionBodyOrSpreadExtrema, DataType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.DataType.Spread, ConditionType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.ConditionType.Max)]
		[NinjaScriptProperty]
		public bool SCPushBullishSpreadExtremaEnabled { get; set; }
		[Display(Name = "Push Bullish: Bar Direction Up", Order = 202, GroupName = "Signal Condition")]
		[DDApexFlowZignal.ConditionPropertiesAttribute(Key = 10, ConditionText = "Bullish bar", SignalType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.SignalType.Push, IsBuy = true, FuncType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.FuncType.ConditionDirection, DataType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.DataType.Bar, ConditionType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.ConditionType.Up)]
		[NinjaScriptProperty]
		public bool SCPushBullishDirectionBarEnabled { get; set; }
		[Display(Name = "Push Bullish: Close Above Neutral", Order = 218, GroupName = "Signal Condition")]
		[DDApexFlowZignal.ConditionPropertiesAttribute(Key = 11, ConditionText = "Close price above neutral range", SignalType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.SignalType.Push, IsBuy = true, FuncType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.FuncType.ConditionNeutralRange, DataType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.DataType.Close, ConditionType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.ConditionType.Above)]
		[NinjaScriptProperty]
		public bool SCPushBullishNeutralRangeCloseEnabled { get; set; }
		[Display(Name = "Push Bullish: Wick Smaller Than Body", Order = 222, GroupName = "Signal Condition")]
		[DDApexFlowZignal.ConditionPropertiesAttribute(Key = 12, ConditionText = "Upper Wick < [MaxWickPercentBullish]% of Range", SignalType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.SignalType.Push, IsBuy = true, FuncType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.FuncType.ConditionCompareRange, DataType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.DataType.Wick, ConditionType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.ConditionType.Smaller)]
		[NinjaScriptProperty]
		public bool SCPushBullishCompareRangeWickEnabled { get; set; }
		[Display(Name = "Push Bullish: Body Greater Than Range", Order = 226, GroupName = "Signal Condition")]
		[DDApexFlowZignal.ConditionPropertiesAttribute(Key = 13, ConditionText = "Body Size > [MinBodyPercentBullish]% of Range", SignalType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.SignalType.Push, IsBuy = true, FuncType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.FuncType.ConditionCompareRange, DataType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.DataType.Body, ConditionType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.ConditionType.Greater)]
		[NinjaScriptProperty]
		public bool SCPushBullishCompareRangeBodyEnabled { get; set; }
		[Display(Name = "Push Bullish: Body Greater Than Tick", Order = 230, GroupName = "Signal Condition")]
		[NinjaScriptProperty]
		public bool SCPushBullishCompareTickBodyEnabled { get; set; }
		[Display(Name = "Push Bearish: Sell Volume Extremum", Order = 234, GroupName = "Signal Condition")]
		[DDApexFlowZignal.ConditionPropertiesAttribute(Key = 1, ConditionText = "Maximum Sell volume over N bars", SignalType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.SignalType.Push, IsBuy = false, FuncType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.FuncType.ConditionVolumeExtrema, DataType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.DataType.VolumeSell, ConditionType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.ConditionType.Max)]
		[NinjaScriptProperty]
		public bool SCPushBearishVolumeExtremaSellEnabled { get; set; }
		[DDApexFlowZignal.ConditionPropertiesAttribute(Key = 2, ConditionText = "Negatie Delta", SignalType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.SignalType.Push, IsBuy = false, FuncType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.FuncType.ConditionDirection, DataType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.DataType.VolumeDelta, ConditionType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.ConditionType.Down)]
		[Display(Name = "Push Bearish: Delta Direction Negative", Order = 246, GroupName = "Signal Condition")]
		[NinjaScriptProperty]
		public bool SCPushBearishDirectionDeltaEnabled { get; set; }
		[DDApexFlowZignal.ConditionPropertiesAttribute(Key = 3, ConditionText = "Negative Delta above average threshold", SignalType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.SignalType.Push, IsBuy = false, FuncType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.FuncType.ConditionDeltaAverage, DataType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.DataType.VolumeNegativeDelta, ConditionType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.ConditionType.Above)]
		[NinjaScriptProperty]
		[Display(Name = "Push Bearish: Negative Delta Above Avg", Order = 258, GroupName = "Signal Condition")]
		public bool SCPushBearishDeltaNegativeAvgEnabled { get; set; }
		[Display(Name = "Push Bearish: Sell Volume Above Avg", Order = 262, GroupName = "Signal Condition")]
		[DDApexFlowZignal.ConditionPropertiesAttribute(Key = 4, ConditionText = "Sell Volume above average threshold", SignalType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.SignalType.Push, IsBuy = false, FuncType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.FuncType.ConditionVolumeAvg, DataType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.DataType.VolumeSell, ConditionType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.ConditionType.Above)]
		[NinjaScriptProperty]
		public bool SCPushBearishVolumeAvgSellEnabled { get; set; }
		[Display(Name = "Push Bearish: POC Bearish", Order = 250, GroupName = "Signal Condition")]
		[DDApexFlowZignal.ConditionPropertiesAttribute(Key = 5, ConditionText = "Bearish POC bias", SignalType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.SignalType.Push, IsBuy = false, FuncType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.FuncType.ConditionDirection, DataType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.DataType.POC, ConditionType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.ConditionType.Down)]
		[NinjaScriptProperty]
		public bool SCPushBearishDirectionPOCEnabled { get; set; }
		[Display(Name = "Push Bearish: POC Below Neutral", Order = 266, GroupName = "Signal Condition")]
		[DDApexFlowZignal.ConditionPropertiesAttribute(Key = 6, ConditionText = "POC below neutral range", SignalType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.SignalType.Push, IsBuy = false, FuncType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.FuncType.ConditionNeutralRange, DataType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.DataType.POC, ConditionType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.ConditionType.Below)]
		[NinjaScriptProperty]
		public bool SCPushBearishNeutralRangePOCEnabled { get; set; }
		[Display(Name = "Push Bearish: Body Extremum", Order = 238, GroupName = "Signal Condition")]
		[DDApexFlowZignal.ConditionPropertiesAttribute(Key = 8, ConditionText = "Body size is maximum over N bars", SignalType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.SignalType.Push, IsBuy = false, FuncType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.FuncType.ConditionBodyOrSpreadExtrema, DataType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.DataType.Body, ConditionType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.ConditionType.Max)]
		[NinjaScriptProperty]
		public bool SCPushBearishBodyExtremaEnabled { get; set; }
		[Display(Name = "Push Bearish: Spread Extremum", Order = 242, GroupName = "Signal Condition")]
		[DDApexFlowZignal.ConditionPropertiesAttribute(Key = 9, ConditionText = "Range is maximum over N bars", SignalType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.SignalType.Push, IsBuy = false, FuncType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.FuncType.ConditionBodyOrSpreadExtrema, DataType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.DataType.Spread, ConditionType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.ConditionType.Max)]
		[NinjaScriptProperty]
		public bool SCPushBearishSpreadExtremaEnabled { get; set; }
		[Display(Name = "Push Bearish: Bar Direction Down", Order = 254, GroupName = "Signal Condition")]
		[DDApexFlowZignal.ConditionPropertiesAttribute(Key = 10, ConditionText = "Bearish bar", SignalType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.SignalType.Push, IsBuy = false, FuncType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.FuncType.ConditionDirection, DataType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.DataType.Bar, ConditionType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.ConditionType.Down)]
		[NinjaScriptProperty]
		public bool SCPushBearishDirectionBarEnabled { get; set; }
		[Display(Name = "Push Bearish: Close Below Neutral", Order = 270, GroupName = "Signal Condition")]
		[DDApexFlowZignal.ConditionPropertiesAttribute(Key = 11, ConditionText = "Close price below neutral range", SignalType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.SignalType.Push, IsBuy = false, FuncType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.FuncType.ConditionNeutralRange, DataType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.DataType.Close, ConditionType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.ConditionType.Below)]
		[NinjaScriptProperty]
		public bool SCPushBearishNeutralRangeCloseEnabled { get; set; }
		[Display(Name = "Push Bearish: Wick Smaller Than Body", Order = 274, GroupName = "Signal Condition")]
		[NinjaScriptProperty]
		[DDApexFlowZignal.ConditionPropertiesAttribute(Key = 12, ConditionText = "Lower Wick < [MaxWickPercentBearish]% of Range", SignalType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.SignalType.Push, IsBuy = false, FuncType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.FuncType.ConditionCompareRange, DataType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.DataType.Wick, ConditionType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.ConditionType.Smaller)]
		public bool SCPushBearishCompareRangeWickEnabled { get; set; }
		[DDApexFlowZignal.ConditionPropertiesAttribute(Key = 13, ConditionText = "Body Size > [MinBodyPercentBearish]% of Range", SignalType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.SignalType.Push, IsBuy = false, FuncType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.FuncType.ConditionCompareRange, DataType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.DataType.Body, ConditionType = NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.ConditionType.Greater)]
		[NinjaScriptProperty]
		[Display(Name = "Push Bearish: Body Greater Than Range", Order = 278, GroupName = "Signal Condition")]
		public bool SCPushBearishCompareRangeBodyEnabled { get; set; }
		[Display(Name = "Push Bearish: Body Greater Than Tick", Order = 282, GroupName = "Signal Condition")]
		[NinjaScriptProperty]
		public bool SCPushBearishCompareTickBodyEnabled { get; set; }
		[Range(0.0, 9.223372036854776E+18)]
		[Display(Name = "Threshold: Strong Volume", Order = 14, GroupName = "General")]
		public long ThresholdStrongVolume { get; set; }
		[Range(0, 100)]
		[Display(Name = "Threshold: Strong Delta (%)", Order = 16, GroupName = "General")]
		public double ThresholdStrongDeltaPercentage { get; set; }
		[Range(0, 2147483647)]
		[Display(Name = "Peak Neighborhood (Bars)", GroupName = "General", Order = 18)]
		[NinjaScriptProperty]
		public int PeakNeighborhood { get; set; }
		[NinjaScriptProperty]
		[Range(1, 100)]
		[Display(Name = "Row Threshold: Multiplier", Order = 20, GroupName = "General")]
		public double RowThresholdMultiplier { get; set; }
		[Display(Name = "On State: Hide Bars", Order = 40, GroupName = "General")]
		public bool OnStateHideBars { get; set; }
		[Display(Name = "Off State: Hide Bars", Order = 50, GroupName = "General")]
		public bool OffStateHideBars { get; set; }
		[Range(99, 500)]
		[Display(Name = "Screen DPI", Order = 100, GroupName = "General")]
		public int ScreenDPI { get; set; }
		[Display(Name = "Enabled", Order = 0, GroupName = "Toggle")]
		public bool ToggleEnabled { get; set; }
		[XmlIgnore]
		[Display(Name = "Background: On", Order = 10, GroupName = "Toggle")]
		public global::System.Windows.Media.Brush ToggleBackBrushOn { get; set; }
		[Browsable(false)]
		public string ToggleBackBrushOnSerialize
		{
			get
			{
				return Serialize.BrushToString(this.ToggleBackBrushOn);
			}
			set
			{
				this.ToggleBackBrushOn = Serialize.StringToBrush(value);
			}
		}
		[XmlIgnore]
		[Display(Name = "Background: Off", Order = 11, GroupName = "Toggle")]
		public global::System.Windows.Media.Brush ToggleBackBrushOff { get; set; }
		[Browsable(false)]
		public string ToggleBackBrushOffSerialize
		{
			get
			{
				return Serialize.BrushToString(this.ToggleBackBrushOff);
			}
			set
			{
				this.ToggleBackBrushOff = Serialize.StringToBrush(value);
			}
		}
		[Display(Name = "Button #1: Text", Order = 20, GroupName = "Toggle")]
		public string Button1Text { get; set; }
		[XmlIgnore]
		[Display(Name = "Button: Text Color", Order = 24, GroupName = "Toggle")]
		public global::System.Windows.Media.Brush ButtonTextBrush { get; set; }
		[Browsable(false)]
		public string ButtonTextBrush_Serialize
		{
			get
			{
				return Serialize.BrushToString(this.ButtonTextBrush);
			}
			set
			{
				this.ButtonTextBrush = Serialize.StringToBrush(value);
			}
		}
		[Range(1, 2147483647)]
		[Display(Name = "Button: Text Size", Order = 26, GroupName = "Toggle")]
		public int ButtonTextSize { get; set; }
		[XmlIgnore]
		[Display(Name = "Drag Bar: Color", Order = 30, GroupName = "Toggle")]
		public global::System.Windows.Media.Brush ToggleDragBrush { get; set; }
		[Browsable(false)]
		public string ToggleDragBrushSerialize
		{
			get
			{
				return Serialize.BrushToString(this.ToggleDragBrush);
			}
			set
			{
				this.ToggleDragBrush = Serialize.StringToBrush(value);
			}
		}
		[Display(Name = "Position: Alignment", Order = 40, GroupName = "Toggle")]
		public DD_TextPosition TogglePositionAlignment
		{
			get
			{
				return this.togglePositionAlignment;
			}
			set
			{
				if (value == DD_TextPosition.TopLeft)
				{
					this.TogglePositionMarginTop = (this.TogglePositionMarginLeft = 5.0);
				}
				if (value == DD_TextPosition.TopRight)
				{
					this.TogglePositionMarginTop = (this.TogglePositionMarginRight = 5.0);
				}
				if (value == DD_TextPosition.BottomRight)
				{
					this.TogglePositionMarginBottom = (this.TogglePositionMarginRight = 5.0);
				}
				if (value == DD_TextPosition.BottomLeft)
				{
					this.TogglePositionMarginBottom = (this.TogglePositionMarginLeft = 5.0);
				}
				if (value == DD_TextPosition.Center)
				{
					this.TogglePositionMarginLeft = (this.TogglePositionMarginTop = (this.TogglePositionMarginRight = (this.TogglePositionMarginBottom = 5.0)));
				}
				this.togglePositionAlignment = value;
			}
		}
		[Display(Name = "Position: Margin Left", Order = 41, GroupName = "Toggle")]
		public double TogglePositionMarginLeft { get; set; }
		[Display(Name = "Position: Margin Top", Order = 42, GroupName = "Toggle")]
		public double TogglePositionMarginTop { get; set; }
		[Display(Name = "Position: Margin Right", Order = 43, GroupName = "Toggle")]
		public double TogglePositionMarginRight { get; set; }
		[Display(Name = "Position: Margin Bottom", Order = 44, GroupName = "Toggle")]
		public double TogglePositionMarginBottom { get; set; }
		[XmlIgnore]
		[Display(Name = "Main Window: Text Color", Order = 0, GroupName = "Windows")]
		public global::System.Windows.Media.Brush MainWindowTextColor { get; set; }
		[Browsable(false)]
		public string MainWindowTextColor_Serialize
		{
			get
			{
				return Serialize.BrushToString(this.MainWindowTextColor);
			}
			set
			{
				this.MainWindowTextColor = Serialize.StringToBrush(value);
			}
		}
		[Display(Name = "Main Window: Left", Order = 2, GroupName = "Windows")]
		public double MainWindowLeft { get; set; }
		[Display(Name = "Main Window: Top", Order = 4, GroupName = "Windows")]
		public double MainWindowTop { get; set; }
		[Display(Name = "Main Window: Width", Order = 6, GroupName = "Windows")]
		public double MainWindowWidth { get; set; }
		[Display(Name = "Main Window: Height", Order = 8, GroupName = "Windows")]
		public double MainWindowHeight { get; set; }
		[Display(Name = "Child Window: Background", Order = 10, GroupName = "Windows")]
		public string ChildWindowBackground { get; set; }
		[Display(Name = "Child Window: Width", Order = 14, GroupName = "Windows")]
		public double ChildWindowWidth { get; set; }
		[Display(Name = "Child Window: Height", Order = 18, GroupName = "Windows")]
		public double ChildWindowHeight { get; set; }
		[Display(Name = "Z Order", Order = 0, GroupName = "Special")]
		public int IndicatorZOrder { get; set; }
		[Display(Name = "User Note", Order = 10, GroupName = "Special")]
		public string UserNote { get; set; }
		[Display(Name = "Switched On", Order = 0, GroupName = "Critical")]
		public bool SwitchedOn { get; set; }
		[Display(Name = "On State: Bar Distance", Order = 10, GroupName = "Critical")]
		[Range(0.0, 3.4028234663852886E+38)]
		public float OnStateBarDistance { get; set; }
		[Display(Name = "On State: Bar Width", Order = 11, GroupName = "Critical")]
		[Range(0.0, 1.7976931348623157E+308)]
		public double OnStateBarWidth { get; set; }
		[Display(Name = "Off State: Bar Distance", Order = 20, GroupName = "Critical")]
		[Range(0.0, 3.4028234663852886E+38)]
		public float OffStateBarDistance { get; set; }
		[Display(Name = "Off State: Bar Width", Order = 21, GroupName = "Critical")]
		[Range(0.0, 1.7976931348623157E+308)]
		public double OffStateBarWidth { get; set; }
		[Browsable(false)]
		[XmlIgnore]
		public Series<double> VolumeBuy
		{
			get
			{
				return base.Values[0];
			}
		}
		[XmlIgnore]
		[Browsable(false)]
		public Series<double> VolumeSell
		{
			get
			{
				return base.Values[1];
			}
		}
		[XmlIgnore]
		[Browsable(false)]
		public Series<double> VolumeDelta
		{
			get
			{
				return base.Values[2];
			}
		}
		[XmlIgnore]
		[Browsable(false)]
		public Series<double> VolumeBuyAvg
		{
			get
			{
				return base.Values[3];
			}
		}
		[XmlIgnore]
		[Browsable(false)]
		public Series<double> VolumeSellAvg
		{
			get
			{
				return base.Values[4];
			}
		}
		[Browsable(false)]
		[XmlIgnore]
		public Series<double> DeltaPositiveAvg
		{
			get
			{
				return base.Values[5];
			}
		}
		[XmlIgnore]
		[Browsable(false)]
		public Series<double> DeltaNegativeAvg
		{
			get
			{
				return base.Values[6];
			}
		}
		[XmlIgnore]
		[Browsable(false)]
		public Series<double> POCBar
		{
			get
			{
				return base.Values[7];
			}
		}
		[Browsable(false)]
		[XmlIgnore]
		public Series<double> SignalTrade
		{
			get
			{
				return base.Values[8];
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
				return "ApexFlow Zignal by DD.co" + this.GetUserNote();
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
			string text2 = "instrument";
			string text3 = "period";
			if (text.Contains(text2) && base.Instruments[0] != null)
			{
				text = text.Replace(text2, base.Instruments[0].FullName);
			}
			if (text.Contains(text3) && base.BarsPeriods[0] != null)
			{
				text = text.Replace(text3, base.BarsPeriods[0].ToString());
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
					base.Name = "DDApexFlowZignal";
					base.Calculate = Calculate.OnBarClose;
					base.IsOverlay = true;
					base.DisplayInDataBox = true;
					base.DrawOnPricePanel = true;
					base.DrawHorizontalGridLines = true;
					base.DrawVerticalGridLines = true;
					base.PaintPriceMarkers = true;
					base.ScaleJustification = ScaleJustification.Right;
					base.IsSuspendedWhileInactive = false;
					this.MarkerEnabled = true;
					this.MarkerRenderingMethod = DDApexFlowZignal_RenderingMethod.Custom;
					this.MarkerPushBrushBullish = Brushes.DodgerBlue;
					this.MarkerPushBrushBearish = Brushes.HotPink;
					this.MarkerAbsorptionBrushBullish = Brushes.LimeGreen;
					this.MarkerAbsorptionBrushBearish = Brushes.DeepPink;
					this.MarkerExhaustionBrushBullish = Brushes.SkyBlue;
					this.MarkerExhaustionBrushBearish = Brushes.LightPink;
					this.MarkerAbPushBrushBullish = Brushes.Cyan;
					this.MarkerAbPushBrushBearish = Brushes.Yellow;
					this.MarkerExPushBrushBullish = Brushes.Cyan;
					this.MarkerExPushBrushBearish = Brushes.Yellow;
					this.MarkerPushStringBullish = "▲ + Push";
					this.MarkerPushStringBearish = "Push + ▼";
					this.MarkerAbsorptionStringBullish = "⬤ + Ab";
					this.MarkerAbsorptionStringBearish = "Ab + ⬤";
					this.MarkerExhaustionStringBullish = "● + Ex";
					this.MarkerExhaustionStringBearish = "Ex + ●";
					this.MarkerAbPushStringBullish = "\ud83e\udc31";
					this.MarkerAbPushStringBearish = "\ud83e\udc33";
					this.MarkerExPushStringBullish = "⇧";
					this.MarkerExPushStringBearish = "⇩";
					this.MarkerFont = new SimpleFont("Arial", 15);
					this.MarkerOffset = 10;
					this.AlertBlockingSeconds = 60;
					this.VolumeBase = DDApexFlowZignal_VolumeBase.BidAskPrice_RealVolume;
					this.PresentationStyleBar = DDApexFlowZignal_PresentationStyle.ProfileCombined;
					this.ThresholdStrongVolume = 50L;
					this.ThresholdStrongDeltaPercentage = 25.0;
					this.PeakNeighborhood = 4;
					this.RowThresholdMultiplier = 2.0;
					this.OnStateHideBars = false;
					this.OffStateHideBars = false;
					this.ScreenDPI = 99;
					this.VolumeAverageAreaEnabled = true;
					this.VolumeAverageAreaBuy = Brushes.LimeGreen;
					this.VolumeAverageAreaSell = Brushes.DeepPink;
					this.VolumeAverageAreaOpacity = 25;
					this.PresentationEnabled = true;
					this.ThemeBuyStrong = Brushes.BlueViolet;
					this.ThemeBuyWeak = Brushes.MediumSlateBlue;
					this.ThemeNeutral = Brushes.Gainsboro;
					this.ThemeSellWeak = Brushes.Orange;
					this.ThemeSellStrong = Brushes.Tomato;
					this.ThemeOpacity = 90;
					this.NumberEnabled = true;
					this.NumberBrushDefault = Brushes.White;
					this.NumberBrushStrong = Brushes.Indigo;
					this.NumberFontDefault = new SimpleFont("Arial", 12);
					this.NumberFontStrong = new SimpleFont("Arial", 16)
					{
						Bold = true
					};
					this.NumberMargin = 5;
					this.POCHighlightEnabled = true;
					this.POCHighlightBorderBuyStroke = new Stroke(Brushes.LimeGreen, 2f);
					this.POCHighlightBorderNeutralStroke = new Stroke(Brushes.Gainsboro, 2f);
					this.POCHighlightBorderSellStroke = new Stroke(Brushes.OrangeRed, 2f);
					this.SummaryEnabled = false;
					this.SummaryTop = false;
					this.SummaryDeltaPositive = Brushes.LimeGreen;
					this.SummaryDeltaZero = Brushes.DarkGray;
					this.SummaryDeltaNegative = Brushes.OrangeRed;
					this.SummaryNumberOnly = true;
					this.SummaryNumberFont = new SimpleFont("Arial", 16)
					{
						Bold = true
					};
					this.SummaryMargin = 8;
					this.StrongRowHighlightBuyBrush = Brushes.LimeGreen;
					this.StrongRowHighlightNeutralBrush = Brushes.Gainsboro;
					this.StrongRowHighlightSellBrush = Brushes.OrangeRed;
					this.StrongRowHighlightMarkerEnabled = true;
					this.StrongRowHighlightMarkerRadius = 4;
					this.DeltaChartEnabled = true;
					this.DeltaChartHeightPercent = 20f;
					this.DeltaChartAxesBrush = Brushes.LightGray;
					this.DeltaChartBackground = Brushes.Yellow;
					this.DeltaChartBackgroundOpacity = 4;
					this.DeltaChartBarPositive = Brushes.LimeGreen;
					this.DeltaChartBarNeutral = Brushes.Gainsboro;
					this.DeltaChartBarNegative = Brushes.HotPink;
					this.DeltaChartOpacity = 100;
					this.DeltaChartPlotEnabled = true;
					this.DeltaChartThresholdPositive = new Stroke(Brushes.Cyan, DashStyleHelper.Solid, 1f);
					this.DeltaChartThresholdNegative = new Stroke(Brushes.Yellow, DashStyleHelper.Solid, 1f);
					this.MaxWickPercentBullish = 15;
					this.MaxWickPercentBearish = 15;
					this.MinBodyPercentBullish = 60;
					this.MinBodyPercentBearish = 60;
					this.MinBodyTicks = 10;
					this.NeutralRange = 30;
					this.AvgVolPeriod = 10;
					this.AvgDeltaPeriod = 10;
					this.AbsorptionN = 3;
					this.ExhaustionN = 3;
					this.PushN = 7;
					this.AbPushPeriod = 5;
					this.ExPushPeriod = 5;
					this.SCAbsorptionEnabled = true;
					this.SCAbsorptionBullish = true;
					this.SCAbsorptionBullishDirectionDeltaEnabled = false;
					this.SCAbsorptionBullishDeltaNegativeAvgEnabled = true;
					this.SCAbsorptionBullishVolumeAvgSellEnabled = true;
					this.SCAbsorptionBullishVolumeExtremaSellEnabled = false;
					this.SCAbsorptionBullishDirectionBarEnabled = false;
					this.SCAbsorptionBullishNeutralRangeCloseEnabled = true;
					this.SCAbsorptionBullishNonExtremeBodyEnabled = true;
					this.SCAbsorptionBullishNonExtremeSpreadEnabled = true;
					this.SCAbsorptionBearish = true;
					this.SCAbsorptionBearishDirectionDeltaEnabled = false;
					this.SCAbsorptionBearishDeltaPositiveAvgEnabled = true;
					this.SCAbsorptionBearishVolumeAvgBuyEnabled = true;
					this.SCAbsorptionBearishVolumeExtremaBuyEnabled = false;
					this.SCAbsorptionBearishDirectionBarEnabled = false;
					this.SCAbsorptionBearishNeutralRangeCloseEnabled = true;
					this.SCAbsorptionBearishNonExtremeBodyEnabled = true;
					this.SCAbsorptionBearishNonExtremeSpreadEnabled = true;
					this.SCExhaustionEnabled = true;
					this.SCExhaustionBullish = true;
					this.SCExhaustionBullishConsecutiveBarEnabled = true;
					this.SCExhaustionBullishConsecutiveBodyEnabled = true;
					this.SCExhaustionBullishConsecutiveSpreadEnabled = true;
					this.SCExhaustionBullishConsecutiveVolumeEnabled = true;
					this.SCExhaustionBullishConsecutiveVolumeSellEnabled = true;
					this.SCExhaustionBullishDeltaNegativeAvgEnabled = true;
					this.SCExhaustionBullishVolumeAvgSellEnabled = false;
					this.SCExhaustionBearish = true;
					this.SCExhaustionBearishConsecutiveBarEnabled = true;
					this.SCExhaustionBearishConsecutiveBodyEnabled = true;
					this.SCExhaustionBearishConsecutiveSpreadEnabled = true;
					this.SCExhaustionBearishConsecutiveVolumeEnabled = true;
					this.SCExhaustionBearishConsecutiveVolumeBuyEnabled = true;
					this.SCExhaustionBearishDeltaPositiveAvgEnabled = true;
					this.SCExhaustionBearishVolumeAvgBuyEnabled = false;
					this.SCPushEnabled = true;
					this.SCPushBullish = true;
					this.SCPushBullishVolumeExtremaBuyEnabled = true;
					this.SCPushBullishDirectionDeltaEnabled = true;
					this.SCPushBullishDeltaPositiveAvgEnabled = false;
					this.SCPushBullishVolumeAvgBuyEnabled = true;
					this.SCPushBullishDirectionPOCEnabled = false;
					this.SCPushBullishNeutralRangePOCEnabled = false;
					this.SCPushBullishBodyExtremaEnabled = true;
					this.SCPushBullishSpreadExtremaEnabled = false;
					this.SCPushBullishDirectionBarEnabled = true;
					this.SCPushBullishNeutralRangeCloseEnabled = true;
					this.SCPushBullishCompareRangeWickEnabled = true;
					this.SCPushBullishCompareRangeBodyEnabled = false;
					this.SCPushBullishCompareTickBodyEnabled = true;
					this.SCPushBearish = true;
					this.SCPushBearishVolumeExtremaSellEnabled = true;
					this.SCPushBearishDirectionDeltaEnabled = true;
					this.SCPushBearishDeltaNegativeAvgEnabled = false;
					this.SCPushBearishVolumeAvgSellEnabled = true;
					this.SCPushBearishDirectionPOCEnabled = false;
					this.SCPushBearishNeutralRangePOCEnabled = false;
					this.SCPushBearishBodyExtremaEnabled = true;
					this.SCPushBearishSpreadExtremaEnabled = false;
					this.SCPushBearishDirectionBarEnabled = true;
					this.SCPushBearishNeutralRangeCloseEnabled = true;
					this.SCPushBearishCompareRangeWickEnabled = true;
					this.SCPushBearishCompareRangeBodyEnabled = false;
					this.SCPushBearishCompareTickBodyEnabled = true;
					this.SCAbPushEnabled = true;
					this.SCAbPushBullish = true;
					this.SCAbPushBearish = true;
					this.SCExPushEnabled = true;
					this.SCExPushBullish = true;
					this.SCExPushBearish = true;
					this.ToggleEnabled = true;
					this.ToggleBackBrushOn = Brushes.DodgerBlue;
					this.ToggleBackBrushOff = Brushes.Silver;
					this.Button1Text = "ApexFlow Zignal";
					this.ButtonTextBrush = Brushes.White;
					this.ButtonTextSize = 10;
					this.ToggleDragBrush = Brushes.LimeGreen;
					this.TogglePositionAlignment = DD_TextPosition.TopLeft;
					this.TogglePositionMarginLeft = 5.0;
					this.TogglePositionMarginTop = 5.0;
					this.TogglePositionMarginRight = 5.0;
					this.TogglePositionMarginBottom = 5.0;
					this.IndicatorZOrder = -1;
					this.UserNote = "instrument (period)";
					this.MainWindowTextColor = Brushes.Transparent;
					this.MainWindowLeft = (this.MainWindowTop = 30.0);
					this.MainWindowWidth = 670.0;
					this.MainWindowHeight = 390.0;
					this.ChildWindowWidth = 750.0;
					this.ChildWindowHeight = 670.0;
					this.ChildWindowBackground = string.Empty;
					this.SwitchedOn = true;
					this.OnStateBarDistance = 120f;
					this.OnStateBarWidth = 5.0;
					this.OffStateBarDistance = 10f;
					this.OffStateBarWidth = 3.0;
					base.AddPlot(new Stroke(Brushes.Transparent, DashStyleHelper.Solid, 1f), PlotStyle.Line, "Volume: Buy");
					base.AddPlot(new Stroke(Brushes.Transparent, DashStyleHelper.Solid, 1f), PlotStyle.Square, "Volume: Sell");
					base.AddPlot(new Stroke(Brushes.Transparent, DashStyleHelper.Solid, 1f), PlotStyle.Square, "Volume: Delta");
					base.AddPlot(new Stroke(Brushes.Transparent, DashStyleHelper.Solid, 1f), PlotStyle.Square, "Volume: Avg Buy");
					base.AddPlot(new Stroke(Brushes.Transparent, DashStyleHelper.Solid, 1f), PlotStyle.Square, "Volume: Avg Sell");
					base.AddPlot(new Stroke(Brushes.Transparent, DashStyleHelper.Solid, 1f), PlotStyle.Square, "Volume: Avg Delta Positive");
					base.AddPlot(new Stroke(Brushes.Transparent, DashStyleHelper.Solid, 1f), PlotStyle.Square, "Volume: Avg Delta Negative");
					base.AddPlot(new Stroke(Brushes.Transparent, DashStyleHelper.Solid, 1f), PlotStyle.Line, "POC Bar");
					base.AddPlot(new Stroke(Brushes.Transparent, DashStyleHelper.Solid, 1f), PlotStyle.Line, "Signal Trade");
				}
				else if (base.State == State.Configure)
				{
					this.dictPresentations = new Dictionary<int, DDApexFlowZignal.Presentation>();
					this.queuePresentationCell = new Queue<DDApexFlowZignal.PresentationCell>();
					this.queueCurrentBar1 = new Queue<int>();
					this.listStartBarIndex1 = new List<int>();
					this.dictPresentations.Add(0, new DDApexFlowZignal.Presentation());
					base.AddDataSeries(BarsPeriodType.Tick, 1);
					this.seriesSignalTrade = new Series<double>(this, MaximumBarsLookBack.Infinite);
					this.PresentationStyleBar = DDApexFlowZignal_PresentationStyle.ProfileCombined;
					this.heightRatio = this.DeltaChartHeightPercent / 100f;
					this.isUpDownTickMode = this.VolumeBase > DDApexFlowZignal_VolumeBase.BidAskPrice_RealVolume;
					this.hideBars = (this.SwitchedOn && this.OnStateHideBars) || (!this.SwitchedOn && this.OffStateHideBars);
					this.seriesBody = new Series<double>(this, MaximumBarsLookBack.Infinite);
					this.seriesRange = new Series<double>(this, MaximumBarsLookBack.Infinite);
					this.seriesVolumeTotal = new Series<double>(this, MaximumBarsLookBack.Infinite);
					this.seriesVolumeDelta = new Series<double>(this, MaximumBarsLookBack.Infinite);
					this.seriesVolumeBuy = new Series<double>(this, MaximumBarsLookBack.Infinite);
					this.seriesVolumeSell = new Series<double>(this, MaximumBarsLookBack.Infinite);
					this.seriesVolumeDeltaPositive = new Series<double>(this, MaximumBarsLookBack.Infinite);
					this.seriesVolumeDeltaNegative = new Series<double>(this, MaximumBarsLookBack.Infinite);
					this.neutralRange = (double)this.NeutralRange / 100.0;
					this.ConfigParameters();
					this.isMarkerCustomRenderingMethod = this.MarkerRenderingMethod == DDApexFlowZignal_RenderingMethod.Custom;
					if (this.isMarkerCustomRenderingMethod)
					{
						this.dictMarkers = new Dictionary<int, DDApexFlowZignal.MarkerInfo>();
					}
					this.dictTextLayoutStrong = new Dictionary<int, TextLayout>();
					this.dictTextLayoutDefault = new Dictionary<int, TextLayout>();
					this.listRawPositiveDelta = new List<double>();
					this.listRawNegativeDelta = new List<double>();
					this.AbsorptionWindowLeft = this.MainWindowLeft + this.MainWindowWidth;
					this.AbsorptionWindowTop = this.MainWindowTop + 100.0;
					this.AbsorptionWindowWidth = this.ChildWindowWidth;
					this.AbsorptionWindowHeight = this.ChildWindowHeight;
					this.ExhaustionWindowLeft = this.MainWindowLeft + this.MainWindowWidth + 100.0;
					this.ExhaustionWindowTop = this.MainWindowTop + 200.0;
					this.ExhaustionWindowWidth = this.ChildWindowWidth;
					this.ExhaustionWindowHeight = this.ChildWindowHeight;
					this.PushWindowLeft = this.MainWindowLeft + this.MainWindowWidth + 200.0;
					this.PushWindowTop = this.MainWindowTop + 300.0;
					this.PushWindowWidth = this.ChildWindowWidth;
					this.PushWindowHeight = this.ChildWindowHeight;
					this.dictAbsorptionCondition = new SortedList<int, DDApexFlowZignal.SignalConditionBody>();
					this.dictExhaustionCondition = new SortedList<int, DDApexFlowZignal.SignalConditionBody>();
					this.dictPushCondition = new SortedList<int, DDApexFlowZignal.SignalConditionBody>();
					this.properties = this.GetPropertiesWithAttribute<DDApexFlowZignal.ConditionPropertiesAttribute>();
					this.absorptionCondition = new DDApexFlowZignal.ConditionGroup(NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.SignalType.Absorption);
					this.exhaustionCondition = new DDApexFlowZignal.ConditionGroup(NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.SignalType.Exhaustion);
					this.pushCondition = new DDApexFlowZignal.ConditionGroup(NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.SignalType.Push);
					this.dictConditionGroup = new Dictionary<DDApexFlowZignal.SignalType, DDApexFlowZignal.ConditionGroup>();
					this.dictConditionGroup.Add(NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.SignalType.Absorption, this.absorptionCondition);
					this.dictConditionGroup.Add(NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.SignalType.Exhaustion, this.exhaustionCondition);
					this.dictConditionGroup.Add(NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.SignalType.Push, this.pushCondition);
					this.UpdateConditionGroup();
					this.barText = this.PresentationStyleBar == DDApexFlowZignal_PresentationStyle.Text;
					this.barTable = this.PresentationStyleBar == DDApexFlowZignal_PresentationStyle.Table;
					this.barProfileCombined = this.PresentationStyleBar == DDApexFlowZignal_PresentationStyle.ProfileCombined;
					this.barProfileDivided = this.PresentationStyleBar == DDApexFlowZignal_PresentationStyle.ProfileDivided;
					this.barProfileDelta = this.PresentationStyleBar == DDApexFlowZignal_PresentationStyle.ProfileDelta;
					this.textFormatDefault = new TextFormat(Globals.DirectWriteFactory, this.NumberFontDefault.FamilySerialize, global::SharpDX.DirectWrite.FontWeight.Normal, global::SharpDX.DirectWrite.FontStyle.Normal, (float)this.NumberFontDefault.Size);
					this.textFormatStrong = new TextFormat(Globals.DirectWriteFactory, this.NumberFontStrong.FamilySerialize, global::SharpDX.DirectWrite.FontWeight.Bold, global::SharpDX.DirectWrite.FontStyle.Normal, (float)this.NumberFontStrong.Size);
					this.avgBuyCloudColor = DD_BrushManager.CreateOpacityBrush(this.VolumeAverageAreaBuy, this.VolumeAverageAreaOpacity);
					this.avgSellCloudColor = DD_BrushManager.CreateOpacityBrush(this.VolumeAverageAreaSell, this.VolumeAverageAreaOpacity);
					this.themeBuyStrongColor = DD_BrushManager.CreateOpacityBrush(this.ThemeBuyStrong, this.ThemeOpacity);
					this.themeBuyWeakColor = DD_BrushManager.CreateOpacityBrush(this.ThemeBuyWeak, this.ThemeOpacity);
					this.themeNeutralColor = DD_BrushManager.CreateOpacityBrush(this.ThemeNeutral, this.ThemeOpacity);
					this.themeSellWeakColor = DD_BrushManager.CreateOpacityBrush(this.ThemeSellWeak, this.ThemeOpacity);
					this.themeSellStrongColor = DD_BrushManager.CreateOpacityBrush(this.ThemeSellStrong, this.ThemeOpacity);
					this.thresholdBuyColor = DD_BrushManager.CreateOpacityBrush(this.ThemeBuyStrong, this.ThemeOpacity / 2);
					this.thresholdSellColor = DD_BrushManager.CreateOpacityBrush(this.ThemeSellStrong, this.ThemeOpacity / 2);
					this.thresholdTotalColor = DD_BrushManager.CreateOpacityBrush(DD_BrushManager.CreateGradientBrush(this.ThemeBuyStrong, this.ThemeSellStrong, 0.5), this.ThemeOpacity / 2);
					this.peakBuyPointColor = DD_BrushManager.CreateOpacityBrush(this.StrongRowHighlightBuyBrush, this.ThemeOpacity);
					this.peakSellPointColor = DD_BrushManager.CreateOpacityBrush(this.StrongRowHighlightSellBrush, this.ThemeOpacity);
					this.deltaChartBarPositiveColor = DD_BrushManager.CreateOpacityBrush(this.DeltaChartBarPositive, this.DeltaChartOpacity);
					this.deltaChartBarNegativeColor = DD_BrushManager.CreateOpacityBrush(this.DeltaChartBarNegative, this.DeltaChartOpacity);
					this.deltaChartBarNeutralColor = DD_BrushManager.CreateOpacityBrush(this.DeltaChartBarNeutral, this.DeltaChartOpacity);
					this.deltaChartBackgroundColor = DD_BrushManager.CreateOpacityBrush(this.DeltaChartBackground, this.DeltaChartBackgroundOpacity);
				}
				else if (base.State == State.DataLoaded)
				{
					this.isDayWeakMonthYear = base.BarsArray[0].BarsPeriod.BarsPeriodType == BarsPeriodType.Day || base.BarsArray[0].BarsPeriod.BarsPeriodType == BarsPeriodType.Week || base.BarsArray[0].BarsPeriod.BarsPeriodType == BarsPeriodType.Month || base.BarsArray[0].BarsPeriod.BarsPeriodType == BarsPeriodType.Year;
				}
				else if (base.State == State.Historical)
				{
						bool flag = this.ChildWindowBackground.Length == 7 && this.ChildWindowBackground.All(new Func<char, bool>("#0123456789abcdefABCDEF".Contains<char>));
						if (string.IsNullOrWhiteSpace(this.ChildWindowBackground) || (!flag && !this.MainWindowTextColor.IsTransparent()))
						{
							this.MainWindowTextColor = Brushes.Transparent;
						}
						if (this.MainWindowTextColor.IsTransparent())
						{
							this.isLightTheme = DDResources_GlobalConstantAndFunction.IsLightTheme().GetValueOrDefault();
							this.MainWindowTextColor = (this.isLightTheme ? Brushes.Black : Brushes.LightGray);
							this.ChildWindowBackground = (this.isLightTheme ? "#FFFFFF" : "#1E1E1E");
						}
						else
						{
							this.isLightTheme = this.ChildWindowBackground == "#FFFFFF";
						}
						this.mainWindowBorderColor = (this.isLightTheme ? Brushes.Black : Brushes.Gray);
						NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.childWindowBackground = (global::System.Windows.Media.SolidColorBrush)new BrushConverter().ConvertFrom(this.ChildWindowBackground);
						NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.childWindowBackground.Freeze();
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
						if (this.isCharting)
						{
							base.ChartControl.Dispatcher.InvokeAsync(delegate
							{
								base.ChartPanel.SizeChanged += this.OnPanelSizeChanged;
								base.ChartControl.PreviewMouseDown += this.OnMouseDown;
								base.ChartControl.PreviewMouseMove += this.OnMouseMove;
								base.ChartControl.PreviewMouseLeftButtonUp += this.OnMouseLeftButtonUp;
								if (this.DeltaChartEnabled && this.draggableSeperator == null)
								{
									this.actualMarginY = base.ChartPanel.ActualHeight * (double)this.heightRatio - 5.0;
									Thickness thickness2 = new Thickness(-1.0, -1.0, -1.0, this.actualMarginY);
									this.draggableSeperator = new DDApexFlowZignal.DraggableSeperatorPanel(this.MainWindowTextColor, NinjaTrader.NinjaScript.DrawingTools.TextPosition.BottomLeft, thickness2, (float)base.ChartPanel.ActualWidth);
									this.draggableSeperator.drag.DragDelta += this.OnToggleDraggableSeperator;
									base.UserControlCollection.Add(this.draggableSeperator);
								}
							});
						}
				}
				else if (base.State == State.Terminated)
				{
					if (this.isCharting)
					{
						base.ChartControl.Dispatcher.InvokeAsync(delegate
						{
							base.ChartPanel.SizeChanged -= this.OnPanelSizeChanged;
							base.ChartControl.PreviewMouseDown -= this.OnMouseDown;
							base.ChartControl.PreviewMouseMove -= this.OnMouseMove;
							base.ChartControl.PreviewMouseLeftButtonUp -= this.OnMouseLeftButtonUp;
							if (this.canvasMouseInfo != null)
							{
								base.UserControlCollection.Remove(this.canvasMouseInfo);
								this.canvasMouseInfo.Children.Clear();
								this.canvasMouseInfo = null;
								this.borderMouseInfo = null;
								this.txtMouseInfo = null;
							}
							if (this.btnResetScale != null)
							{
								base.UserControlCollection.Remove(this.btnResetScale);
								this.btnResetScale = null;
							}
							if (this.draggableSeperator != null)
							{
								this.draggableSeperator.drag.DragDelta -= this.OnToggleDraggableSeperator;
								this.draggableSeperator = null;
							}
							if (this.mainWindowNT != null)
							{
								this.mainWindowNT.Close();
								this.mainWindowNT = null;
							}
							if (this.absorptionWindowNT != null)
							{
								this.absorptionWindowNT.Close();
								this.absorptionWindowNT = null;
							}
							if (this.exhaustionWindowNT != null)
							{
								this.exhaustionWindowNT.Close();
								this.exhaustionWindowNT = null;
							}
							if (this.pushWindowNT != null)
							{
								this.pushWindowNT.Close();
								this.pushWindowNT = null;
							}
						});
					}
					if (this.dictTextLayoutStrong != null && this.dictTextLayoutStrong.Count > 0)
					{
						foreach (TextLayout textLayout in this.dictTextLayoutStrong.Values)
						{
							textLayout.Dispose();
						}
					}
					if (this.textFormatDefault != null)
					{
						this.textFormatDefault.Dispose();
					}
					if (this.textFormatStrong != null)
					{
						this.textFormatStrong.Dispose();
					}
				}
			}
			catch (Exception ex)
			{
				this.PrintException(ex);
			}
		}
		private void OnPanelSizeChanged(object sender, SizeChangedEventArgs e)
		{
			if (this.isCharting)
			{
				base.ChartControl.Dispatcher.InvokeAsync(delegate
				{
					try
					{
						if (sender is ChartPanel && this.draggableSeperator != null)
						{
							this.actualMarginY = this.ChartPanel.ActualHeight * (double)this.heightRatio - 5.0;
							Thickness thickness = new Thickness(-1.0, -1.0, -1.0, this.actualMarginY);
							this.draggableSeperator.SetPosition(thickness);
							this.draggableSeperator.Width = this.ChartPanel.ActualWidth;
							if (this.btnResetScale != null)
							{
								this.btnResetScale.Margin = new Thickness(-1.0, -1.0, this.actualMarginX, this.actualMarginY - this.btnResetScale.ActualHeight);
							}
						}
					}
					catch (Exception ex)
					{
						this.PrintException(ex);
					}
				});
			}
		}
		private void ConfigParameters()
		{
			this.maxPeriod = this.AbsorptionN;
			this.maxPeriod = Math.Max(this.maxPeriod, this.ExhaustionN);
			this.maxPeriod = Math.Max(this.maxPeriod, this.PushN);
			this.maxPeriod = Math.Max(this.maxPeriod, this.AvgVolPeriod);
			this.maxPeriod = Math.Max(this.maxPeriod, this.AvgDeltaPeriod);
			this.maxWickBullishRatio = (double)this.MaxWickPercentBullish / 100.0;
			this.maxWickBearishRatio = (double)this.MaxWickPercentBearish / 100.0;
			this.minBodyBullishRatio = (double)this.MinBodyPercentBullish / 100.0;
			this.minBodyBearishRatio = (double)this.MinBodyPercentBearish / 100.0;
		}
		private void ClearConditionGroup()
		{
			if (this.dictConditionGroup.Count == 0)
			{
				return;
			}
			foreach (KeyValuePair<DDApexFlowZignal.SignalType, DDApexFlowZignal.ConditionGroup> keyValuePair in this.dictConditionGroup)
			{
				DDApexFlowZignal.ConditionGroup value = keyValuePair.Value;
				value.ListBuyConditionInfo.Clear();
				value.ListSellConditionInfo.Clear();
			}
		}
		private void UpdateConditionGroup()
		{
			this.ClearConditionGroup();
			this.ConfigParameters();
			foreach (PropertyInfo propertyInfo in this.properties)
			{
				DDApexFlowZignal.ConditionPropertiesAttribute customAttribute = propertyInfo.GetCustomAttribute<DDApexFlowZignal.ConditionPropertiesAttribute>();
				if (customAttribute != null && !customAttribute.IsHeaderNote)
				{
					bool isBuy = customAttribute.IsBuy;
					DDApexFlowZignal.SignalType signalType = customAttribute.SignalType;
					DDApexFlowZignal.FuncType funcType = customAttribute.FuncType;
					DDApexFlowZignal.DataType dataType = customAttribute.DataType;
					DDApexFlowZignal.ConditionType conditionType = customAttribute.ConditionType;
					object value = propertyInfo.GetValue(this);
					bool flag = value is bool && (bool)value;
					DDApexFlowZignal.ConditionGroup conditionGroup = this.dictConditionGroup[signalType];
					bool flag2;
					bool flag3;
					if (signalType == NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.SignalType.Absorption)
					{
						conditionGroup.IsBuyEnabled = this.SCAbsorptionEnabled && this.SCAbsorptionBullish;
						flag2 = conditionGroup.IsBuyEnabled || (this.SCAbPushEnabled && this.SCAbPushBullish);
						conditionGroup.IsSellEnabled = this.SCAbsorptionEnabled && this.SCAbsorptionBearish;
						flag3 = conditionGroup.IsSellEnabled || (this.SCAbPushEnabled && this.SCAbPushBearish);
					}
					else if (signalType == NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.SignalType.Exhaustion)
					{
						conditionGroup.IsBuyEnabled = this.SCExhaustionEnabled && this.SCExhaustionBullish;
						flag2 = conditionGroup.IsBuyEnabled || (this.SCExPushEnabled && this.SCExPushBullish);
						conditionGroup.IsSellEnabled = this.SCExhaustionEnabled && this.SCExhaustionBearish;
						flag3 = conditionGroup.IsSellEnabled || (this.SCExPushEnabled && this.SCExPushBearish);
					}
					else
					{
						conditionGroup.IsBuyEnabled = this.SCPushEnabled && this.SCPushBullish;
						flag2 = conditionGroup.IsBuyEnabled || (this.SCAbPushEnabled && this.SCAbPushBullish) || (this.SCExPushEnabled && this.SCExPushBullish);
						conditionGroup.IsSellEnabled = this.SCPushEnabled && this.SCPushBearish;
						flag3 = conditionGroup.IsSellEnabled || (this.SCAbPushEnabled && this.SCAbPushBearish) || (this.SCExPushEnabled && this.SCExPushBearish);
					}
					if (flag)
					{
						if (isBuy && flag2)
						{
							conditionGroup.ListBuyConditionInfo.Add(this.GetConditionInfo(true, funcType, dataType, conditionType, signalType));
						}
						else if (!isBuy && flag3)
						{
							conditionGroup.ListSellConditionInfo.Add(this.GetConditionInfo(false, funcType, dataType, conditionType, signalType));
						}
					}
				}
			}
		}
		protected override void OnBarUpdate()
		{
			try
			{
					if (base.BarsInProgress == 0 && base.State != State.Historical && this.isCharting)
					{
						base.ChartControl.Dispatcher.InvokeAsync(delegate
						{
							base.TriggerCustomEvent(delegate(object state)
							{
								this.retrieveBarIndex = this.RetrieveBarIndex();
							}, null);
						});
					}
					this.ComputeVolumes();
					if (!this.isDayWeakMonthYear && base.CurrentBars[1] >= 0)
					{
						if (base.BarsInProgress == 0)
						{
							this.maxVolume = Math.Max(this.maxVolume, base.Volumes[0][0]);
							int num = base.CurrentBars[0];
							if (this.hideBars)
							{
								if (num == 0)
								{
									base.BarBrush = (base.CandleOutlineBrush = Brushes.Transparent);
								}
								base.BarBrushes[-1] = (base.CandleOutlineBrushes[-1] = Brushes.Transparent);
							}
							if (num > 0)
							{
								this.POCBar[0] = ((this.dictPresentations[num].POCKey > 0) ? this.GetPriceByKey(this.dictPresentations[num].POCKey) : base.Closes[0][0]);
							}
							this.FindSignal(num);
						}
					}
			}
			catch (Exception ex)
			{
				this.PrintException(ex);
			}
		}
		private void FindSignal(int barIndex)
		{
			if (barIndex < this.maxPeriod)
			{
				return;
			}
			this.factorBottom = (this.factorTop = 0);
			int num = base.CurrentBars[0] - barIndex;
			if (this.lastAbsorptionBarIndex != -1 && barIndex - this.lastAbsorptionBarIndex > this.AbPushPeriod)
			{
				this.lastAbsorptionBarIndex = -1;
			}
			if (this.lastExhaustionBarIndex != -1 && barIndex - this.lastExhaustionBarIndex > this.ExPushPeriod)
			{
				this.lastExhaustionBarIndex = -1;
			}
			if (this.dictConditionGroup.Count > 0)
			{
				foreach (KeyValuePair<DDApexFlowZignal.SignalType, DDApexFlowZignal.ConditionGroup> keyValuePair in this.dictConditionGroup)
				{
					DDApexFlowZignal.SignalType key = keyValuePair.Key;
					DDApexFlowZignal.ConditionGroup value = keyValuePair.Value;
					int num2 = value.GetSignal(barIndex);
					if (num2 != 0)
					{
						bool flag = num2 > 0;
						if ((flag && value.IsBuyEnabled) || (!flag && value.IsSellEnabled))
						{
							if (this.isMarkerCustomRenderingMethod)
							{
								this.AddMarker(barIndex, flag, value.SignalType);
							}
							else
							{
								this.PrintMarker(num2 > 0, value.SignalType, barIndex);
							}
							if (num >= 0)
							{
								this.SignalTrade[num] = (double)num2;
							}
						}
						if (key == NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.SignalType.Exhaustion)
						{
							this.lastExhaustionBarIndex = barIndex;
						}
						else if (key == NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.SignalType.Absorption)
						{
							this.lastAbsorptionBarIndex = barIndex;
						}
						else
						{
							if (this.SCAbPushEnabled && ((flag && this.SCAbPushBullish) || (!flag && this.SCAbPushBearish)) && this.lastAbsorptionBarIndex != -1 && barIndex - this.lastAbsorptionBarIndex <= this.AbPushPeriod)
							{
								int num3 = base.CurrentBars[0] - this.lastAbsorptionBarIndex;
								if (num3 >= 0 && this.seriesSignalTrade[num3] != 0.0)
								{
									bool flag2 = this.seriesSignalTrade[num3] > 0.0;
									if (flag2 == flag)
									{
										if (this.isMarkerCustomRenderingMethod)
										{
											this.AddMarker(barIndex, flag, NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.SignalType.AbPush);
										}
										else
										{
											this.PrintMarker(flag2, NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.SignalType.AbPush, barIndex);
										}
										this.lastAbsorptionBarIndex = -1;
										num2 = (flag ? 1 : (-1)) * 4;
										if (num >= 0)
										{
											this.SignalTrade[num] = (double)num2;
										}
									}
								}
							}
							if (this.SCExPushEnabled && ((flag && this.SCExPushBullish) || (!flag && this.SCExPushBearish)) && this.lastExhaustionBarIndex != -1 && barIndex - this.lastExhaustionBarIndex <= this.ExPushPeriod)
							{
								int num4 = base.CurrentBars[0] - this.lastExhaustionBarIndex;
								if (num4 >= 0 && this.seriesSignalTrade[num4] != 0.0)
								{
									bool flag3 = this.seriesSignalTrade[num4] > 0.0;
									if (flag3 == flag)
									{
										if (this.isMarkerCustomRenderingMethod)
										{
											this.AddMarker(barIndex, flag, NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.SignalType.ExPush);
										}
										else
										{
											this.PrintMarker(flag3, NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.SignalType.ExPush, barIndex);
										}
										this.lastExhaustionBarIndex = -1;
										num2 = (flag ? 1 : (-1)) * 5;
										if (num >= 0)
										{
											this.SignalTrade[num] = (double)num2;
										}
									}
								}
							}
						}
						this.seriesSignalTrade[0] = (double)num2;
					}
				}
			}
		}
		private double ComputeSMA(ref double sum, List<double> inputList, int barsInProgress, int period, bool isCheck = false)
		{
			int count = inputList.Count;
			sum = sum + inputList[count - 1] - ((count > period) ? inputList[count - period - 1] : 0.0);
			return sum / (double)((count < period) ? count : period);
		}
		private int GetPeriodBySignalType(DDApexFlowZignal.SignalType signalType)
		{
			if (signalType == NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.SignalType.Absorption)
			{
				return this.AbsorptionN;
			}
			if (signalType == NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.SignalType.Exhaustion)
			{
				return this.ExhaustionN;
			}
			return this.PushN;
		}
		private DDApexFlowZignal.ConditionInfo GetConditionInfo(bool isBullish, DDApexFlowZignal.FuncType type, DDApexFlowZignal.DataType dataType, DDApexFlowZignal.ConditionType conditionType, DDApexFlowZignal.SignalType signalType)
		{
			int periodBySignalType = this.GetPeriodBySignalType(signalType);
			if (type == NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.FuncType.ConditionDirection)
			{
				return new DDApexFlowZignal.ConditionInfo(isBullish, new Func<DDApexFlowZignal.DataType, DDApexFlowZignal.ConditionType, int, int, bool, bool>(this.ConditionDirection), dataType, conditionType, 0);
			}
			if (type == NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.FuncType.ConditionNeutralRange)
			{
				return new DDApexFlowZignal.ConditionInfo(isBullish, new Func<DDApexFlowZignal.DataType, DDApexFlowZignal.ConditionType, int, int, bool, bool>(this.ConditionNeutralRange), dataType, conditionType, 0);
			}
			if (type == NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.FuncType.ConditionVolumeExtrema)
			{
				return new DDApexFlowZignal.ConditionInfo(isBullish, new Func<DDApexFlowZignal.DataType, DDApexFlowZignal.ConditionType, int, int, bool, bool>(this.ConditionVolumeExtrema), dataType, conditionType, periodBySignalType);
			}
			if (type == NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.FuncType.ConditionBodyOrSpreadExtrema)
			{
				return new DDApexFlowZignal.ConditionInfo(isBullish, new Func<DDApexFlowZignal.DataType, DDApexFlowZignal.ConditionType, int, int, bool, bool>(this.ConditionBodyOrSpreadExtrema), dataType, conditionType, periodBySignalType);
			}
			if (type == NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.FuncType.ConditionBodyOrSpreadNonExtreme)
			{
				return new DDApexFlowZignal.ConditionInfo(isBullish, new Func<DDApexFlowZignal.DataType, DDApexFlowZignal.ConditionType, int, int, bool, bool>(this.ConditionBodyOrSpreadNonExtreme), dataType, conditionType, periodBySignalType);
			}
			if (type == NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.FuncType.ConditionVolumeAvg)
			{
				return new DDApexFlowZignal.ConditionInfo(isBullish, new Func<DDApexFlowZignal.DataType, DDApexFlowZignal.ConditionType, int, int, bool, bool>(this.ConditionVolumeAvg), dataType, conditionType, this.AvgVolPeriod);
			}
			if (type == NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.FuncType.ConditionDeltaAverage)
			{
				return new DDApexFlowZignal.ConditionInfo(isBullish, new Func<DDApexFlowZignal.DataType, DDApexFlowZignal.ConditionType, int, int, bool, bool>(this.ConditionDeltaAvg), dataType, conditionType, this.AvgDeltaPeriod);
			}
			if (type == NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.FuncType.ConditionCompareRange)
			{
				return new DDApexFlowZignal.ConditionInfo(isBullish, new Func<DDApexFlowZignal.DataType, DDApexFlowZignal.ConditionType, int, int, bool, bool>(this.ConditionCompareRange), dataType, conditionType, 0);
			}
			if (type == NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.FuncType.ConditionCompareTick)
			{
				return new DDApexFlowZignal.ConditionInfo(isBullish, new Func<DDApexFlowZignal.DataType, DDApexFlowZignal.ConditionType, int, int, bool, bool>(this.ConditionCompareTick), dataType, conditionType, 0);
			}
			if (type == NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.FuncType.ConditionConsecutiveBar)
			{
				return new DDApexFlowZignal.ConditionInfo(isBullish, new Func<DDApexFlowZignal.DataType, DDApexFlowZignal.ConditionType, int, int, bool, bool>(this.ConditionConsecutiveBar), dataType, conditionType, periodBySignalType);
			}
			if (type == NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.FuncType.ConditionConsecutiveBodyOrSpread)
			{
				return new DDApexFlowZignal.ConditionInfo(isBullish, new Func<DDApexFlowZignal.DataType, DDApexFlowZignal.ConditionType, int, int, bool, bool>(this.ConditionConsecutiveBodyOrSpread), dataType, conditionType, periodBySignalType);
			}
			if (type == NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.FuncType.ConditionConsecutiveVolume)
			{
				return new DDApexFlowZignal.ConditionInfo(isBullish, new Func<DDApexFlowZignal.DataType, DDApexFlowZignal.ConditionType, int, int, bool, bool>(this.ConditionConsecutiveVolume), dataType, conditionType, periodBySignalType);
			}
			throw new NotSupportedException(string.Format("ConditionFuncType '{0}' is not supported", type));
		}
		private double GetValueSeries(PriceSeries series, int barIndex, bool isClickEvent)
		{
			if (!isClickEvent)
			{
				return series[base.CurrentBars[0] - barIndex];
			}
			return series.GetValueAt(barIndex);
		}
		private double GetValueSeries(VolumeSeries series, int barIndex, bool isClickEvent)
		{
			if (!isClickEvent)
			{
				return series[base.CurrentBars[0] - barIndex];
			}
			return series.GetValueAt(barIndex);
		}
		private double GetValueSeries(Series<double> series, int barIndex, bool isClickEvent)
		{
			if (!isClickEvent)
			{
				return series[base.CurrentBars[0] - barIndex];
			}
			return series.GetValueAt(barIndex);
		}
		private bool ConditionDirection(DDApexFlowZignal.DataType dataType, DDApexFlowZignal.ConditionType conditionType, int barIndex, int period = 0, bool isBullish = false)
		{
			if (dataType != NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.DataType.Bar && dataType != NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.DataType.VolumeDelta && dataType != NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.DataType.POC)
			{
				return false;
			}
			if (conditionType != NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.ConditionType.Up && conditionType != NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.ConditionType.Down)
			{
				return false;
			}
			double num;
			if (dataType == NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.DataType.Bar)
			{
				num = this.GetValueSeries(base.Closes[0], barIndex, this.isClickEvent) - this.GetValueSeries(base.Opens[0], barIndex, this.isClickEvent);
			}
			else
			{
				if (!this.dictPresentations.ContainsKey(barIndex))
				{
					return false;
				}
				if (dataType == NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.DataType.VolumeDelta)
				{
					num = this.dictPresentations[barIndex].VolDelta;
				}
				else
				{
					num = this.dictPresentations[barIndex].GetPOCDelta();
				}
			}
			if (num.ApproxCompare(0.0) == 0)
			{
				return false;
			}
			int num2 = ((conditionType == NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.ConditionType.Up) ? 1 : (-1));
			return num * (double)num2 > 0.0;
		}
		private bool ConditionNeutralRange(DDApexFlowZignal.DataType dataType, DDApexFlowZignal.ConditionType conditionType, int barIndex, int period = 0, bool isBullish = false)
		{
			if (dataType != NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.DataType.Close && dataType != NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.DataType.POC)
			{
				return false;
			}
			if (conditionType != NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.ConditionType.Above && conditionType != NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.ConditionType.Below)
			{
				return false;
			}
			int num = ((conditionType == NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.ConditionType.Above) ? 1 : (-1));
			double num2 = (this.GetValueSeries(base.Highs[0], barIndex, this.isClickEvent) - this.GetValueSeries(base.Lows[0], barIndex, this.isClickEvent)) * this.neutralRange;
			double num3 = this.GetValueSeries(base.Medians[0], barIndex, this.isClickEvent) + (double)num * num2 / 2.0;
			double num4;
			if (dataType == NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.DataType.Close)
			{
				num4 = this.GetValueSeries(base.Closes[0], barIndex, this.isClickEvent);
			}
			else
			{
				if (!this.dictPresentations.ContainsKey(barIndex))
				{
					return false;
				}
				num4 = this.GetPriceByKey(this.dictPresentations[barIndex].POCKey);
			}
			return ((num4 - num3) * (double)num).ApproxCompare(0.0) > 0;
		}
		private bool ConditionVolumeExtrema(DDApexFlowZignal.DataType dataType, DDApexFlowZignal.ConditionType conditionType, int barIndex, int period = 0, bool isBullish = false)
		{
			if (dataType != NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.DataType.Volume && dataType != NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.DataType.VolumeBuy && dataType != NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.DataType.VolumeSell)
			{
				return false;
			}
			if (conditionType != NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.ConditionType.Max && conditionType != NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.ConditionType.Min)
			{
				return false;
			}
			bool flag = conditionType == NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.ConditionType.Max;
			int num = (flag ? 1 : (-1));
			return this.FindBarVolume(barIndex, dataType, flag, !flag, period) == num;
		}
		private bool ConditionBodyOrSpreadExtrema(DDApexFlowZignal.DataType dataType, DDApexFlowZignal.ConditionType conditionType, int barIndex, int period = 0, bool isBullish = false)
		{
			if ((dataType != NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.DataType.Body && dataType != NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.DataType.Spread) || period == 0)
			{
				return false;
			}
			if (conditionType != NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.ConditionType.Max && conditionType != NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.ConditionType.Min)
			{
				return false;
			}
			bool flag = dataType == NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.DataType.Body;
			bool flag2 = conditionType == NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.ConditionType.Max;
			int num = (flag2 ? 1 : (-1));
			return this.FindBarBodyOrSpread(barIndex, flag, flag2, !flag2, period) == num;
		}
		private bool ConditionBodyOrSpreadNonExtreme(DDApexFlowZignal.DataType dataType, DDApexFlowZignal.ConditionType conditionType, int barIndex, int period = 0, bool isBullish = false)
		{
			return !this.ConditionBodyOrSpreadExtrema(dataType, conditionType, barIndex, period, false);
		}
		private bool ConditionVolumeAvg(DDApexFlowZignal.DataType dataType, DDApexFlowZignal.ConditionType conditionType, int barIndex, int period = 0, bool isBullish = false)
		{
			if (dataType != NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.DataType.Volume && dataType != NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.DataType.VolumeBuy && dataType != NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.DataType.VolumeSell)
			{
				return false;
			}
			if (conditionType != NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.ConditionType.Above && conditionType != NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.ConditionType.Below)
			{
				return false;
			}
			if (period == 0)
			{
				return false;
			}
			Series<double> series = ((dataType == NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.DataType.Volume) ? this.seriesVolumeTotal : ((dataType == NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.DataType.VolumeBuy) ? this.seriesVolumeBuy : this.seriesVolumeSell));
			double valueSeries = this.GetValueSeries(base.SMA(series, period).Value, barIndex, false);
			int num = ((conditionType == NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.ConditionType.Above) ? 1 : (-1));
			return ((this.GetValueSeries(series, barIndex, this.isClickEvent) - valueSeries) * (double)num).ApproxCompare(0.0) > 0;
		}
		private bool ConditionDeltaAvg(DDApexFlowZignal.DataType dataType, DDApexFlowZignal.ConditionType conditionType, int barIndex, int period = 0, bool isBullish = false)
		{
			if ((dataType != NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.DataType.VolumePostiveDelta && dataType != NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.DataType.VolumeNegativeDelta) || period == 0)
			{
				return false;
			}
			if (conditionType != NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.ConditionType.Above && conditionType != NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.ConditionType.Below)
			{
				return false;
			}
			double valueSeries = this.GetValueSeries(this.VolumeDelta, barIndex, this.isClickEvent);
			if (dataType == NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.DataType.VolumePostiveDelta && valueSeries <= 0.0)
			{
				return false;
			}
			if (dataType == NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.DataType.VolumeNegativeDelta && valueSeries >= 0.0)
			{
				return false;
			}
			double num = Math.Abs(valueSeries);
			double num2 = Math.Abs(this.GetValueSeries((dataType == NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.DataType.VolumePostiveDelta) ? this.seriesVolumeDeltaPositive : this.seriesVolumeDeltaNegative, barIndex, this.isClickEvent));
			int num3 = ((conditionType == NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.ConditionType.Above) ? 1 : (-1));
			return ((num - num2) * (double)num3).ApproxCompare(0.0) > 0;
		}
		private bool ConditionCompareRange(DDApexFlowZignal.DataType dataType, DDApexFlowZignal.ConditionType conditionType, int barIndex, int period = 0, bool isBullish = false)
		{
			if (dataType != NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.DataType.Wick && dataType != NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.DataType.Body)
			{
				return false;
			}
			if (conditionType != NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.ConditionType.Greater && conditionType != NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.ConditionType.Smaller)
			{
				return false;
			}
			double valueSeries = this.GetValueSeries(base.Opens[0], barIndex, this.isClickEvent);
			double valueSeries2 = this.GetValueSeries(base.Highs[0], barIndex, this.isClickEvent);
			double valueSeries3 = this.GetValueSeries(base.Lows[0], barIndex, this.isClickEvent);
			double valueSeries4 = this.GetValueSeries(base.Closes[0], barIndex, this.isClickEvent);
			double num = valueSeries2 - valueSeries3;
			int num2 = ((conditionType == NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.ConditionType.Greater) ? 1 : (-1));
			double num3;
			double num4;
			if (dataType == NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.DataType.Wick)
			{
				num3 = Math.Min(valueSeries2 - valueSeries4, valueSeries4 - valueSeries3);
				num4 = (isBullish ? this.maxWickBullishRatio : this.maxWickBearishRatio) * num;
			}
			else
			{
				num3 = Math.Abs(valueSeries4 - valueSeries);
				num4 = (isBullish ? this.minBodyBullishRatio : this.minBodyBearishRatio) * num;
			}
			return ((num3 - num4) * (double)num2).ApproxCompare(0.0) > 0;
		}
		private bool ConditionCompareTick(DDApexFlowZignal.DataType dataType, DDApexFlowZignal.ConditionType conditionType, int barIndex, int period = 0, bool isBullish = false)
		{
			if (dataType != NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.DataType.Body)
			{
				return false;
			}
			if (conditionType != NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.ConditionType.Greater && conditionType != NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.ConditionType.Smaller)
			{
				return false;
			}
			int num = ((conditionType == NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.ConditionType.Greater) ? 1 : (-1));
			return ((Math.Abs(this.GetValueSeries(base.Closes[0], barIndex, this.isClickEvent) - this.GetValueSeries(base.Opens[0], barIndex, this.isClickEvent)) - (double)this.MinBodyTicks * base.TickSize) * (double)num).ApproxCompare(0.0) > 0;
		}
		private bool ConditionConsecutiveBar(DDApexFlowZignal.DataType dataType, DDApexFlowZignal.ConditionType conditionType, int barIndex, int period = 0, bool isBullish = false)
		{
			if (dataType != NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.DataType.Bar || period == 0)
			{
				return false;
			}
			if (conditionType != NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.ConditionType.Increasing && conditionType != NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.ConditionType.Decreasing)
			{
				return false;
			}
			if (base.CurrentBars[0] < period - 1)
			{
				return false;
			}
			int num = ((conditionType == NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.ConditionType.Increasing) ? 1 : (-1));
			for (int i = barIndex - period + 1; i <= barIndex; i++)
			{
				if ((this.GetValueSeries(base.Closes[0], i, this.isClickEvent) - this.GetValueSeries(base.Opens[0], i, this.isClickEvent)) * (double)num < 0.0)
				{
					return false;
				}
			}
			return true;
		}
		private bool ConditionConsecutiveBodyOrSpread(DDApexFlowZignal.DataType dataType, DDApexFlowZignal.ConditionType conditionType, int barIndex, int period = 0, bool isBullish = false)
		{
			if ((dataType != NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.DataType.Body && dataType != NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.DataType.Spread) || period == 0)
			{
				return false;
			}
			if (conditionType != NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.ConditionType.Increasing && conditionType != NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.ConditionType.Decreasing)
			{
				return false;
			}
			if (base.CurrentBars[0] < period - 1)
			{
				return false;
			}
			PriceSeries priceSeries = ((dataType == NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.DataType.Body) ? base.Closes[0] : base.Highs[0]);
			PriceSeries priceSeries2 = ((dataType == NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.DataType.Body) ? base.Opens[0] : base.Lows[0]);
			int num = ((conditionType == NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.ConditionType.Increasing) ? 1 : (-1));
			for (int i = barIndex; i >= barIndex - period + 2; i--)
			{
				double valueSeries = this.GetValueSeries(priceSeries, i, this.isClickEvent);
				double valueSeries2 = this.GetValueSeries(priceSeries2, i, this.isClickEvent);
				double valueSeries3 = this.GetValueSeries(priceSeries, i - 1, this.isClickEvent);
				double valueSeries4 = this.GetValueSeries(priceSeries2, i - 1, this.isClickEvent);
				double num2 = Math.Abs(valueSeries - valueSeries2);
				double num3 = Math.Abs(valueSeries3 - valueSeries4);
				if (((num2 - num3) * (double)num).ApproxCompare(0.0) < 0)
				{
					return false;
				}
			}
			return true;
		}
		private bool ConditionConsecutiveVolume(DDApexFlowZignal.DataType dataType, DDApexFlowZignal.ConditionType conditionType, int barIndex, int period = 0, bool isBullish = false)
		{
			if ((dataType != NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.DataType.Volume && dataType != NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.DataType.VolumeBuy && dataType != NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.DataType.VolumeSell) || period == 0)
			{
				return false;
			}
			if (conditionType != NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.ConditionType.Increasing && conditionType != NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.ConditionType.Decreasing)
			{
				return false;
			}
			if (base.CurrentBars[0] < period - 1)
			{
				return false;
			}
			int num = ((conditionType == NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.ConditionType.Increasing) ? 1 : (-1));
			Series<double> series = ((dataType == NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.DataType.Volume) ? this.seriesVolumeTotal : ((dataType == NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.DataType.VolumeBuy) ? this.seriesVolumeBuy : this.seriesVolumeSell));
			for (int i = barIndex; i >= barIndex - period + 2; i--)
			{
				if (((this.GetValueSeries(series, i, this.isClickEvent) - this.GetValueSeries(series, i - 1, this.isClickEvent)) * (double)num).ApproxCompare(0.0) < 0)
				{
					return false;
				}
			}
			return true;
		}
		private int FindBarBodyOrSpread(int barIndex, bool isBody, bool isMaxEnabled, bool isMinEnabled, int period)
		{
			double valueSeries = this.GetValueSeries(isBody ? base.Closes[0] : base.Highs[0], barIndex, this.isClickEvent);
			double valueSeries2 = this.GetValueSeries(isBody ? base.Opens[0] : base.Lows[0], barIndex, this.isClickEvent);
			int num = (int)(Math.Abs(valueSeries - valueSeries2) / base.TickSize);
			for (int i = barIndex - 1; i > barIndex - period; i--)
			{
				double valueSeries3 = this.GetValueSeries(isBody ? base.Closes[0] : base.Highs[0], i, this.isClickEvent);
				double valueSeries4 = this.GetValueSeries(isBody ? base.Opens[0] : base.Lows[0], i, this.isClickEvent);
				int num2 = (int)(Math.Abs(valueSeries3 - valueSeries4) / base.TickSize);
				if (isMaxEnabled && num <= num2)
				{
					isMaxEnabled = false;
				}
				if (isMinEnabled && num >= num2)
				{
					isMinEnabled = false;
				}
				if (!isMaxEnabled && !isMinEnabled)
				{
					return 0;
				}
			}
			if (isMaxEnabled)
			{
				return 1;
			}
			if (!isMinEnabled)
			{
				return 0;
			}
			return -1;
		}
		private int FindBarVolume(int barIndex, DDApexFlowZignal.DataType dataType, bool isMaxEnabled, bool isMinEnabled, int period)
		{
			if (dataType == NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.DataType.Volume)
			{
				double valueSeries = this.GetValueSeries(base.Volumes[0], barIndex, this.isClickEvent);
				for (int i = barIndex - 1; i > barIndex - period; i--)
				{
					double valueSeries2 = this.GetValueSeries(base.Volumes[0], i, this.isClickEvent);
					if (isMaxEnabled && valueSeries.ApproxCompare(valueSeries2) <= 0)
					{
						isMaxEnabled = false;
					}
					if (isMinEnabled && valueSeries.ApproxCompare(valueSeries2) >= 0)
					{
						isMinEnabled = false;
					}
					if (!isMaxEnabled && !isMinEnabled)
					{
						return 0;
					}
				}
			}
			else if (dataType == NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.DataType.VolumeBuy || dataType == NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.DataType.VolumeSell)
			{
				bool flag = dataType == NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.DataType.VolumeBuy;
				double num = (flag ? this.GetValueSeries(this.seriesVolumeBuy, barIndex, this.isClickEvent) : this.GetValueSeries(this.seriesVolumeSell, barIndex, this.isClickEvent));
				for (int j = barIndex - 1; j > barIndex - period; j--)
				{
					double num2 = (flag ? this.GetValueSeries(this.seriesVolumeBuy, j, this.isClickEvent) : this.GetValueSeries(this.seriesVolumeSell, j, this.isClickEvent));
					if (isMaxEnabled && num.ApproxCompare(num2) <= 0)
					{
						isMaxEnabled = false;
					}
					if (isMinEnabled && num.ApproxCompare(num2) >= 0)
					{
						isMinEnabled = false;
					}
					if (!isMaxEnabled && !isMinEnabled)
					{
						return 0;
					}
				}
			}
			else
			{
				double num3 = this.GetValueSeries(this.seriesVolumeDelta, barIndex, this.isClickEvent);
				bool flag2 = dataType == NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.DataType.VolumePostiveDelta;
				if (flag2 ? (num3.ApproxCompare(0.0) <= 0) : (num3.ApproxCompare(0.0) >= 0))
				{
					return 0;
				}
				if (!flag2)
				{
					num3 = Math.Abs(num3);
				}
				for (int k = barIndex - 1; k > barIndex - period; k--)
				{
					double num4 = this.GetValueSeries(this.seriesVolumeDelta, k, this.isClickEvent);
					if (!(flag2 ? (num4.ApproxCompare(0.0) <= 0) : (num4.ApproxCompare(0.0) >= 0)))
					{
						if (!flag2)
						{
							num4 = Math.Abs(num4);
						}
						if (isMaxEnabled && num3.ApproxCompare(num4) <= 0)
						{
							isMaxEnabled = false;
						}
						if (isMinEnabled && num3.ApproxCompare(num4) >= 0)
						{
							isMinEnabled = false;
						}
						if (!isMaxEnabled && !isMinEnabled)
						{
							return 0;
						}
					}
				}
			}
			if (isMaxEnabled)
			{
				return 1;
			}
			if (!isMinEnabled)
			{
				return 0;
			}
			return -1;
		}
		protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
		{
			try
			{
				{
					base.OnRender(chartControl, chartScale);
					if (this.isCharting)
					{
						if (!base.IsInHitTest)
						{
							this.scaleOfChart = chartScale;
							if (base.ChartControl.Properties.EquidistantBarSpacing)
							{
								if (this.errorPrinted)
								{
									base.RemoveDrawObjects();
									this.errorPrinted = false;
								}
								this.barDistance = base.ChartControl.Properties.BarDistance;
								this.barWidth = base.ChartControl.BarWidth;
								if (this.SwitchedOn)
								{
									if (!this.OnStateBarWidth.Equals(this.barWidth))
									{
										this.OnStateBarWidth = this.barWidth;
									}
									if (!this.OnStateBarDistance.Equals(this.barDistance))
									{
										this.OnStateBarDistance = this.barDistance;
									}
								}
								else
								{
									if (!this.OffStateBarWidth.Equals(this.barWidth))
									{
										this.OffStateBarWidth = this.barWidth;
									}
									if (!this.OffStateBarDistance.Equals(this.barDistance))
									{
										this.OffStateBarDistance = this.barDistance;
									}
								}
								this.minY = (float)(base.ChartPanel.Y + base.ChartPanel.H);
								this.DrawDeltaChart(chartControl, chartScale);
								if (this.SwitchedOn)
								{
									double minValue = chartScale.MinValue;
									double maxValue = chartScale.MaxValue;
									this.cellWidth = this.barDistance - 20f;
									this.candleMargin = (this.hideBars ? 1f : ((float)this.barWidth + 5f));
									this.minKey = this.GetPriceKey(minValue);
									this.maxKey = this.GetPriceKey(maxValue);
									this.minPixelsBetween2Rows = Convert.ToInt32(Math.Ceiling((double)base.ChartPanel.H / 350.0));
									if (this.minPixelsBetween2Rows < 1)
									{
										this.minPixelsBetween2Rows = 1;
									}
									this.autoThickness = 1 + Math.Max(this.minPixelsBetween2Rows, this.GetPixelDistanceByKeys(this.minKey, this.minKey + 1));
									this.allowDrawText = (float)this.autoThickness >= this.GetTextLayout(0L, false).Metrics.Height - 6f;
									if (this.PresentationEnabled)
									{
										for (int i = base.ChartBars.FromIndex; i <= base.ChartBars.ToIndex; i++)
										{
											if (this.dictPresentations.ContainsKey(i))
											{
												this.PaintBarPresentation(i, this.dictPresentations[i], (double)this.barDistance);
											}
										}
									}
									if (this.isMarkerCustomRenderingMethod)
									{
										this.DrawMarkers(chartScale);
									}
								}
							}
							else
							{
								SimpleFont simpleFont = new SimpleFont("Arial", 15);
								string text = "ApexFlow Zignal only works with \"Equidistant Bar Spacing\" set to TRUE.";
								string text2 = "DDApexFlowZignal.error";
								NinjaTrader.NinjaScript.DrawingTools.Draw.TextFixed(this, text2, text, NinjaTrader.NinjaScript.DrawingTools.TextPosition.Center, Brushes.Red, simpleFont, Brushes.Transparent, Brushes.Transparent, 0);
								this.errorPrinted = true;
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				this.PrintException(ex);
			}
		}
		private void PaintPriceMarker(int x, int y, bool isLeft, string text, SimpleFont font, global::SharpDX.Direct2D1.Brush foreground, global::SharpDX.Direct2D1.Brush background, float margin)
		{
			float num = (float)font.Size / 1.5f;
			float num2 = (float)font.Size / 4f;
			float num3 = (float)font.Size / 2f;
			x += (int)margin;
			float num4 = this.deltaChartMaxTextSize.Width + 2f * num3 + ((margin == 0f) ? (2f * this.marginX) : 0f);
			RectangleF rectangleF;
			RectangleF rectangleF2;
			if (isLeft)
			{
				rectangleF = new RectangleF((float)x, (float)y - this.deltaChartMaxTextSize.Height / 2f, num4, this.deltaChartMaxTextSize.Height);
				rectangleF2 = new RectangleF((float)x, (float)y - this.deltaChartMaxTextSize.Height / 2f - num2, num4, this.deltaChartMaxTextSize.Height + 2f * num2);
			}
			else
			{
				rectangleF = new RectangleF((float)x, (float)y - this.deltaChartMaxTextSize.Height / 2f, num4, this.deltaChartMaxTextSize.Height);
				rectangleF2 = new RectangleF((float)x, (float)y - this.deltaChartMaxTextSize.Height / 2f - num2, num4, this.deltaChartMaxTextSize.Height + 2f * num2);
			}
			Vector2 vector;
			Vector2 vector2;
			Vector2 vector3;
			Vector2 vector4;
			Vector2 vector5;
			if (isLeft)
			{
				vector = new Vector2(rectangleF2.Left, rectangleF2.Top);
				vector2 = new Vector2(rectangleF2.Right, rectangleF2.Top);
				vector3 = new Vector2(rectangleF2.Right, rectangleF2.Bottom);
				vector4 = new Vector2(rectangleF2.Left, rectangleF2.Bottom);
				vector5 = new Vector2(rectangleF2.Left - num, (rectangleF2.Top + rectangleF2.Bottom) / 2f);
			}
			else
			{
				vector = new Vector2(rectangleF2.Left, rectangleF2.Top);
				vector2 = new Vector2(rectangleF2.Right, rectangleF2.Top);
				vector3 = new Vector2(rectangleF2.Right + num, (rectangleF2.Top + rectangleF2.Bottom) / 2f);
				vector4 = new Vector2(rectangleF2.Right, rectangleF2.Bottom);
				vector5 = new Vector2(rectangleF2.Left, rectangleF2.Bottom);
			}
			global::SharpDX.Direct2D1.PathGeometry pathGeometry = new global::SharpDX.Direct2D1.PathGeometry(base.RenderTarget.Factory);
			GeometrySink geometrySink = pathGeometry.Open();
			geometrySink.BeginFigure(vector, FigureBegin.Filled);
			geometrySink.AddLines(new Vector2[] { vector2, vector3, vector4, vector5, vector });
			geometrySink.EndFigure(FigureEnd.Open);
			geometrySink.Close();
			AntialiasMode antialiasMode = base.RenderTarget.AntialiasMode;
			base.RenderTarget.AntialiasMode = AntialiasMode.PerPrimitive;
			base.RenderTarget.FillGeometry(pathGeometry, background);
			base.RenderTarget.AntialiasMode = antialiasMode;
			TextFormat textFormat = font.ToDirectWriteTextFormat();
			textFormat.TextAlignment = global::SharpDX.DirectWrite.TextAlignment.Center;
			base.RenderTarget.DrawText(text, textFormat, rectangleF, foreground);
			geometrySink.Dispose();
			pathGeometry.Dispose();
			textFormat.Dispose();
		}
		private void DrawPlot(Vector2[] arrayPoints, Stroke stroke)
		{
			if (arrayPoints.Length < 2)
			{
				return;
			}
			global::SharpDX.Direct2D1.PathGeometry pathGeometry = new global::SharpDX.Direct2D1.PathGeometry(base.RenderTarget.Factory);
			GeometrySink geometrySink = pathGeometry.Open();
			geometrySink.BeginFigure(arrayPoints[0], FigureBegin.Filled);
			geometrySink.AddLines(arrayPoints);
			geometrySink.EndFigure(FigureEnd.Open);
			geometrySink.Close();
			AntialiasMode antialiasMode = base.RenderTarget.AntialiasMode;
			base.RenderTarget.AntialiasMode = AntialiasMode.PerPrimitive;
			global::SharpDX.Direct2D1.Brush brush = stroke.Brush.ToDxBrush(base.RenderTarget);
			base.RenderTarget.DrawGeometry(pathGeometry, brush, stroke.Width, stroke.StrokeStyle);
			brush.Dispose();
			geometrySink.Dispose();
			pathGeometry.Dispose();
			base.RenderTarget.AntialiasMode = antialiasMode;
		}
		private void DrawCloud(Vector2[] arrayPoints, global::System.Windows.Media.Brush brush)
		{
			int num = arrayPoints.Length;
			if (num < 2)
			{
				return;
			}
			Vector2 vector = arrayPoints[1];
			arrayPoints[0] = new Vector2(vector.X, this.deltaChartCenterY);
			Vector2 vector2 = arrayPoints[num - 2];
			arrayPoints[num - 1] = new Vector2(vector2.X, this.deltaChartCenterY);
			global::SharpDX.Direct2D1.PathGeometry pathGeometry = new global::SharpDX.Direct2D1.PathGeometry(base.RenderTarget.Factory);
			GeometrySink geometrySink = pathGeometry.Open();
			geometrySink.BeginFigure(arrayPoints[0], FigureBegin.Filled);
			geometrySink.AddLines(arrayPoints);
			geometrySink.EndFigure(FigureEnd.Open);
			geometrySink.Close();
			AntialiasMode antialiasMode = base.RenderTarget.AntialiasMode;
			base.RenderTarget.AntialiasMode = AntialiasMode.PerPrimitive;
			global::SharpDX.Direct2D1.Brush brush2 = brush.ToDxBrush(base.RenderTarget);
			base.RenderTarget.FillGeometry(pathGeometry, brush2);
			brush2.Dispose();
			geometrySink.Dispose();
			pathGeometry.Dispose();
			base.RenderTarget.AntialiasMode = antialiasMode;
		}
		private void DrawDeltaChart(ChartControl chartControl, ChartScale chartScale)
		{
			if (!this.DeltaChartEnabled)
			{
				return;
			}
			Size2F size2F = this.ComputeTextSize(this.FormatPriceMarker(this.maxVolume), this.labelTextFont, this.ScreenDPI);
			int fromIndex = base.ChartBars.FromIndex;
			int toIndex = base.ChartBars.ToIndex;
			float num = (float)base.ChartPanel.H * this.heightRatio;
			this.maxBarHeight = num / 2f - this.marginY;
			this.minY -= num;
			this.deltaChartCenterY = this.minY + num / 2f;
			float num2 = (float)base.ChartPanel.W - size2F.Width - 20f;
			this.actualMarginX = base.ChartPanel.ActualWidth - this.RealToWpf((double)num2);
			int num3 = Math.Min(base.CurrentBars[0], toIndex);
			int num4 = num3 - fromIndex + 1;
			for (int i = toIndex; i >= fromIndex; i--)
			{
				float num5 = (float)chartControl.GetXByBarIndex(base.ChartBars, i);
				num3 = i;
				if (num5 < num2)
				{
					break;
				}
				num4--;
			}
			if (!this.isManualScaling)
			{
				this.maxVol = double.MinValue;
				for (int j = fromIndex; j <= num3; j++)
				{
					if (j <= base.CurrentBars[0] && this.dictPresentations.ContainsKey(j))
					{
						DDApexFlowZignal.Presentation presentation = this.dictPresentations[j];
						this.maxVol = Math.Max(this.maxVol, Math.Max(Math.Abs(presentation.VolBuy), Math.Abs(presentation.VolSell)));
					}
				}
			}
			if (num4 <= 0)
			{
				return;
			}
			this.deltaChartMaxTextSize = this.ComputeTextSize(this.FormatPriceMarker(-this.maxVol), this.labelTextFont, this.ScreenDPI);
			Vector2[] array = new Vector2[num4];
			Vector2[] array2 = new Vector2[num4];
			Vector2[] array3 = new Vector2[num4 + 2];
			Vector2[] array4 = new Vector2[num4 + 2];
			this.yStartValueBar = this.deltaChartCenterY - (this.maxBarHeight + this.marginY / 2f);
			this.yEndValueBar = this.deltaChartCenterY + (this.maxBarHeight + this.marginY / 2f);
			Vector2 vector = new Vector2(num2, this.yStartValueBar);
			Vector2 vector2 = new Vector2(num2, this.yEndValueBar);
			base.RenderTarget.DrawLine(vector, vector2, Brushes.LightGray.ToDxBrush(base.RenderTarget), 1f);
			if (!this.deltaChartBackgroundColor.IsTransparent())
			{
				global::SharpDX.Direct2D1.Brush brush = this.deltaChartBackgroundColor.ToDxBrush(base.RenderTarget);
				RectangleF rectangleF = new RectangleF(vector.X, vector.Y, (float)(-(float)base.ChartPanel.W), vector2.Y - vector.Y);
				base.RenderTarget.FillRectangle(rectangleF, brush);
				brush.Dispose();
			}
			Vector2 vector3 = new Vector2((float)base.ChartPanel.X, this.deltaChartCenterY);
			Vector2 vector4 = new Vector2(num2, this.deltaChartCenterY);
			base.RenderTarget.DrawLine(vector3, vector4, DD_BrushManager.CreateOpacityBrush(this.DeltaChartAxesBrush, 50).ToDxBrush(base.RenderTarget), 1f, new Stroke(Brushes.Transparent, DashStyleHelper.Dot, 1f).StrokeStyle);
			this.DrawYValue(num2, this.maxVol, this.maxVol, this.DeltaChartAxesBrush, Brushes.Transparent, this.marginX);
			this.DrawYValue(num2, this.maxVol, 0.0, this.DeltaChartAxesBrush, Brushes.Transparent, this.marginX);
			this.DrawYValue(num2, this.maxVol, -this.maxVol, this.DeltaChartAxesBrush, Brushes.Transparent, this.marginX);
			if (this.dictPresentations.ContainsKey(num3))
			{
				double num6 = Math.Min(this.seriesVolumeDeltaPositive.GetValueAt(Math.Min(num3, base.CurrentBars[0])), this.maxVol);
				this.DrawYValue(num2, this.maxVol, num6, this.DeltaChartAxesBrush, this.DeltaChartThresholdPositive.Brush, 0f);
				double num7 = Math.Max(this.seriesVolumeDeltaNegative.GetValueAt(Math.Min(num3, base.CurrentBars[0])), -this.maxVol);
				this.DrawYValue(num2, this.maxVol, num7, this.DeltaChartAxesBrush, this.DeltaChartThresholdNegative.Brush, 0f);
				double volDelta = this.dictPresentations[num3].VolDelta;
				global::System.Windows.Media.Brush brush2 = ((volDelta > 0.0) ? this.DeltaChartBarPositive : this.DeltaChartBarNegative);
				this.DrawYValue(num2, this.maxVol, volDelta, this.DeltaChartAxesBrush, brush2, 0f);
			}
			int num8 = 0;
			float num9 = ((!this.SwitchedOn || this.barDistance < 30f) ? ((float)this.barWidth * 2f) : (this.barDistance * 0.5f));
			float num10 = num9 - num9 % 2f + 1f;
			float num11 = Math.Max(3f, num10);
			for (int k = fromIndex; k <= toIndex; k++)
			{
				if (this.dictPresentations.ContainsKey(k))
				{
					DDApexFlowZignal.Presentation presentation2 = this.dictPresentations[k];
					float num12 = (float)chartControl.GetXByBarIndex(base.ChartBars, k);
					if (num12 < num2)
					{
						if (this.DeltaChartPlotEnabled && num8 < num4)
						{
							float num13 = this.deltaChartCenterY - (float)(this.seriesVolumeDeltaPositive.GetValueAt(k) / this.maxVol * (double)this.maxBarHeight);
							array[num8] = new Vector2(num12, Math.Max(num13, this.minY));
							float num14 = this.deltaChartCenterY - (float)(this.seriesVolumeDeltaNegative.GetValueAt(k) / this.maxVol * (double)this.maxBarHeight);
							array2[num8] = new Vector2(num12, Math.Min(num14, (float)(base.ChartPanel.H - 2)));
							float num15 = this.deltaChartCenterY - (float)(this.VolumeBuyAvg.GetValueAt(k) / this.maxVol * (double)this.maxBarHeight);
							array3[num8 + 1] = new Vector2(num12, Math.Max(num15, this.minY));
							float num16 = this.deltaChartCenterY - (float)(-(float)this.VolumeSellAvg.GetValueAt(k) / this.maxVol * (double)this.maxBarHeight);
							array4[num8 + 1] = new Vector2(num12, Math.Min(num16, (float)(base.ChartPanel.H - 2)));
							num8++;
						}
						global::SharpDX.Direct2D1.Brush brush3 = ((presentation2.VolDelta == 0.0) ? this.deltaChartBarNeutralColor : ((presentation2.VolDelta > 0.0) ? this.deltaChartBarPositiveColor : this.deltaChartBarNegativeColor)).ToDxBrush(base.RenderTarget);
						if (presentation2.VolDelta != 0.0)
						{
							float num17 = (float)(((presentation2.VolDelta > 0.0) ? presentation2.VolBuy : presentation2.VolSell) / this.maxVol * (double)this.maxBarHeight);
							float num18 = Math.Min(this.maxBarHeight, num17);
							int num19 = ((presentation2.VolDelta > 0.0) ? (-1) : 1);
							RectangleF rectangleF2 = new RectangleF(num12 - (num11 - 1f) / 2f, this.deltaChartCenterY, num11, (float)num19 * num18);
							base.RenderTarget.DrawRectangle(rectangleF2, brush3, 1f);
							float num20 = this.deltaChartCenterY;
							float num21 = this.deltaChartCenterY + (float)((double)num19 * Math.Min(Math.Abs(presentation2.VolDelta / this.maxVol * (double)this.maxBarHeight), (double)this.maxBarHeight));
							Vector2 vector5 = new Vector2(num12, num20);
							Vector2 vector6 = new Vector2(num12, num21);
							base.RenderTarget.DrawLine(vector5, vector6, brush3, num11);
						}
						else
						{
							Vector2 vector7 = new Vector2(num12 - num10 / 2f, this.deltaChartCenterY);
							Vector2 vector8 = new Vector2(num12 + num10 / 2f, this.deltaChartCenterY);
							base.RenderTarget.DrawLine(vector7, vector8, brush3, num11);
						}
						brush3.Dispose();
					}
				}
			}
			if (this.DeltaChartPlotEnabled)
			{
				this.DrawPlot(array, this.DeltaChartThresholdPositive);
				this.DrawPlot(array2, this.DeltaChartThresholdNegative);
				this.DrawCloud(array3, this.avgBuyCloudColor);
				this.DrawCloud(array4, this.avgSellCloudColor);
			}
		}
		private void DrawYValue(float x, double maxAbsVolDelta, double volumeDelta, global::System.Windows.Media.Brush foreground, global::System.Windows.Media.Brush background, float marginX)
		{
			float num = this.deltaChartCenterY - (float)(volumeDelta / maxAbsVolDelta * (double)this.maxBarHeight);
			if (num < this.yStartValueBar || num > this.yEndValueBar)
			{
				return;
			}
			if (marginX > 0f)
			{
				Vector2 vector = new Vector2(x, num);
				Vector2 vector2 = new Vector2(x + marginX / 2f, num);
				global::SharpDX.Direct2D1.Brush brush = this.DeltaChartAxesBrush.ToDxBrush(base.RenderTarget);
				base.RenderTarget.DrawLine(vector, vector2, brush, 1f);
				brush.Dispose();
			}
			global::SharpDX.Direct2D1.Brush brush2 = foreground.ToDxBrush(base.RenderTarget);
			global::SharpDX.Direct2D1.Brush brush3 = background.ToDxBrush(base.RenderTarget);
			this.PaintPriceMarker((int)x, (int)num, true, this.FormatPriceMarker(volumeDelta), this.labelTextFont, brush2, brush3, marginX);
			brush2.Dispose();
			brush3.Dispose();
		}
		private TextLayout GetTextLayout(long number, bool isStrong)
		{
			int num = (int)number;
			Dictionary<int, TextLayout> dictionary = (isStrong ? this.dictTextLayoutStrong : this.dictTextLayoutDefault);
			TextFormat textFormat = (isStrong ? this.textFormatStrong : this.textFormatDefault);
			if (!dictionary.ContainsKey(num))
			{
				string text = num.ToString();
				TextLayout textLayout = new TextLayout(Globals.DirectWriteFactory, text, textFormat, 100f, 50f);
				dictionary[num] = textLayout;
			}
			return dictionary[num];
		}
		private void DrawOnePoint(float x, float y, global::System.Windows.Media.Brush brush)
		{
			if (brush.IsTransparent())
			{
				return;
			}
			AntialiasMode antialiasMode = base.RenderTarget.AntialiasMode;
			base.RenderTarget.AntialiasMode = AntialiasMode.PerPrimitive;
			Vector2 vector = new Vector2(x, y);
			global::SharpDX.Direct2D1.Ellipse ellipse = new global::SharpDX.Direct2D1.Ellipse(vector, (float)this.StrongRowHighlightMarkerRadius, (float)this.StrongRowHighlightMarkerRadius);
			global::SharpDX.Direct2D1.Brush brush2 = brush.ToDxBrush(base.RenderTarget);
			base.RenderTarget.FillEllipse(ellipse, brush2);
			brush2.Dispose();
			base.RenderTarget.AntialiasMode = antialiasMode;
		}
		private void PaintBarPresentation(int bar, DDApexFlowZignal.Presentation presentation, double barDistance)
		{
			if (presentation.POCKey == 0 || presentation.LowKey == 2147483647 || presentation.HighKey == 0)
			{
				return;
			}
			float num = (float)base.ChartControl.GetXByBarIndex(base.ChartBars, bar);
			float num2 = (this.barProfileCombined ? (this.cellWidth - 2f * this.candleMargin) : (this.cellWidth / 2f - this.candleMargin - (float)this.StrongRowHighlightMarkerRadius));
			float num8;
			if (barDistance >= 30.0)
			{
				double high = base.BarsArray[0].GetHigh(bar);
				double low = base.BarsArray[0].GetLow(bar);
				int priceKey = this.GetPriceKey(high);
				int priceKey2 = this.GetPriceKey(low);
				float num3 = float.MinValue;
				for (int i = priceKey2; i <= priceKey; i++)
				{
					DDApexFlowZignal.PresentationCell value = presentation.GetValue(i);
					long num4 = Convert.ToInt64(value.VolBuy);
					long num5 = Convert.ToInt64(value.VolSell);
					if (this.barProfileCombined)
					{
						long num6 = num4 + num5;
						bool flag = num6 >= 2L * this.ThresholdStrongVolume;
						TextLayout textLayout = this.GetTextLayout(num6, flag);
						num3 = Math.Max(num3, textLayout.Metrics.Width + (float)(2 * this.NumberMargin));
					}
					else
					{
						bool flag2 = num4 >= 2L * this.ThresholdStrongVolume;
						TextLayout textLayout2 = this.GetTextLayout(num4, flag2);
						num3 = Math.Max(num3, textLayout2.Metrics.Width + (float)(2 * this.NumberMargin));
						bool flag3 = num5 >= 2L * this.ThresholdStrongVolume;
						TextLayout textLayout3 = this.GetTextLayout(num5, flag3);
						num3 = Math.Max(num3, textLayout3.Metrics.Width + (float)(2 * this.NumberMargin));
					}
				}
				num2 = Math.Max(num2, num3);
				double num7;
				if (this.barProfileCombined)
				{
					num7 = presentation.POCVol;
				}
				else if (this.barProfileDelta)
				{
					num7 = presentation.POCDeltaVol;
				}
				else
				{
					num7 = Math.Max(presentation.POCBuyVol, presentation.POCSellVol);
				}
				for (int j = priceKey2; j <= priceKey; j++)
				{
					num8 = (float)this.scaleOfChart.GetYByValue((double)j * base.TickSize);
					long num9 = Convert.ToInt64(presentation.GetValue(j).VolBuy);
					long num10 = Convert.ToInt64(presentation.GetValue(j).VolSell);
					long num11 = Convert.ToInt64(presentation.GetValue(j).VolDelta);
					long num12 = num9 + num10;
					float num13;
					if (this.barTable)
					{
						num13 = num2;
					}
					else if (this.barText)
					{
						num13 = num3;
					}
					else if (this.barProfileDivided)
					{
						num13 = (float)Convert.ToInt32((double)((float)num10 * num2) / num7);
					}
					else if (this.barProfileDelta)
					{
						num13 = (float)Convert.ToInt32((double)((float)Math.Abs(num11) * num2) / num7);
					}
					else
					{
						num13 = (float)Convert.ToInt32((double)((float)(num9 + num10) * num2) / num7);
					}
					float num14;
					if (this.barTable)
					{
						num14 = num2;
					}
					else if (this.barText)
					{
						num14 = num3;
					}
					else if (this.barProfileDelta)
					{
						num14 = (float)Convert.ToInt32((double)((float)Math.Abs(num11) * num2) / num7);
					}
					else
					{
						num14 = (float)Convert.ToInt32((double)((float)num9 * num2) / num7);
					}
					if (this.StrongRowHighlightMarkerEnabled)
					{
						float num15 = num - this.candleMargin - num13 - (float)this.StrongRowHighlightMarkerRadius - 2f;
						float num16 = num + this.candleMargin + num14 + (float)this.StrongRowHighlightMarkerRadius + 2f;
						global::System.Windows.Media.Brush brush = ((num11 > 0L) ? this.peakBuyPointColor : this.peakSellPointColor);
						if (this.barProfileCombined)
						{
							if (num12 != 0L && (double)num12 >= presentation.AvgVolTotal * this.RowThresholdMultiplier)
							{
								this.DrawOnePoint(num15, num8, brush);
							}
						}
						else if (this.barProfileDelta)
						{
							if (num11 < 0L && (double)num11 < presentation.AvgDeltaNegative * this.RowThresholdMultiplier)
							{
								this.DrawOnePoint(num15, num8, brush);
							}
							else if (num11 > 0L && (double)num11 > presentation.AvgDeltaPositive * this.RowThresholdMultiplier)
							{
								this.DrawOnePoint(num16, num8, brush);
							}
						}
						else
						{
							if ((double)num9 > this.RowThresholdMultiplier * presentation.AvgBuy)
							{
								this.DrawOnePoint(num16, num8, this.peakBuyPointColor);
							}
							if ((double)num10 > this.RowThresholdMultiplier * presentation.AvgSell)
							{
								this.DrawOnePoint(num15, num8, this.peakSellPointColor);
							}
						}
					}
					this.PrintBars(priceKey2, priceKey, num9, num10, num11, num, num8, num2, Convert.ToInt64(num7), this.hideBars, false, this.allowDrawText);
				}
				this.scaleOfChart.GetYByValue(high);
				this.scaleOfChart.GetYByValue(low);
			}
			num8 = (float)this.scaleOfChart.GetYByValue((double)presentation.POCKey * base.TickSize);
			this.HighlightBarPOCAndPrintSummary(bar, presentation, num, num8, barDistance, num2);
		}
		private void PrintBars(int lowKey, int highKey, long buy, long sell, long delta, float x, float y, float maxBarWidth, long maxVol, bool useTinyWhitespace, bool isToday, bool allowDrawText)
		{
			bool flag = this.barText;
			bool flag2 = this.barTable;
			bool flag3 = this.barProfileCombined;
			bool flag4 = this.barProfileDivided;
			bool flag5 = this.barProfileDelta;
			bool flag6 = flag2 || flag;
			int deltaState = this.GetDeltaState(buy, sell);
			if (flag5)
			{
				if (delta == 0L)
				{
					return;
				}
				buy = ((delta > 0L) ? delta : 0L);
				sell = ((delta < 0L) ? delta : 0L);
			}
			float num = (useTinyWhitespace ? 1f : this.candleMargin);
			float num2;
			if (flag)
			{
				num2 = (float)this.NumberMargin + (useTinyWhitespace ? 1f : ((float)base.ChartControl.BarWidth));
			}
			else
			{
				num2 = (float)this.NumberMargin + num;
			}
			bool flag7 = false;
			bool flag8 = false;
			if (isToday)
			{
				bool flag9 = flag4 || flag3 || flag;
			}
			global::System.Windows.Media.Brush brush;
			global::System.Windows.Media.Brush brush2;
			if (deltaState >= 2)
			{
				if (flag4 || flag5)
				{
					brush = this.themeSellWeakColor;
					brush2 = this.themeBuyStrongColor;
					flag8 = true;
				}
				else
				{
					brush2 = (brush = this.themeBuyStrongColor);
					flag8 = (flag7 = true);
				}
			}
			else if (deltaState == 1)
			{
				if (flag4 || flag5)
				{
					brush = this.themeSellWeakColor;
					brush2 = this.themeBuyWeakColor;
				}
				else
				{
					brush2 = (brush = this.themeBuyWeakColor);
				}
			}
			else if (deltaState == 0)
			{
				if (flag4 || flag5)
				{
					brush = this.themeSellWeakColor;
					brush2 = this.themeBuyWeakColor;
				}
				else
				{
					brush2 = (brush = this.themeNeutralColor);
				}
			}
			else if (deltaState == -1)
			{
				if (flag4 || flag5)
				{
					brush = this.themeSellWeakColor;
					brush2 = this.themeBuyWeakColor;
				}
				else
				{
					brush2 = (brush = this.themeSellWeakColor);
				}
			}
			else if (flag4 || flag5)
			{
				brush = this.themeSellStrongColor;
				brush2 = this.themeBuyWeakColor;
				flag7 = true;
			}
			else
			{
				brush2 = (brush = this.themeSellStrongColor);
				flag8 = (flag7 = true);
			}
			if (flag)
			{
				Vector2 vector = new Vector2(x, (float)this.scaleOfChart.GetYByValue((double)lowKey * base.TickSize));
				Vector2 vector2 = new Vector2(x, (float)this.scaleOfChart.GetYByValue((double)highKey * base.TickSize));
				global::SharpDX.Direct2D1.Brush brush3 = base.ChartBars.Properties.ChartStyle.Stroke2.Brush.ToDxBrush(base.RenderTarget);
				base.RenderTarget.DrawLine(vector, vector2, brush3);
				brush3.Dispose();
			}
			bool flag10 = true;
			if (isToday && !this.SwitchedOn)
			{
				flag10 = this.ComputeTextSize("12345", this.NumberFontDefault, this.ScreenDPI).Height * 0.9f < (float)this.autoThickness;
			}
			if (!flag)
			{
				float num3;
				if (flag2)
				{
					num3 = maxBarWidth;
				}
				else if (flag4)
				{
					num3 = (float)Convert.ToInt32((float)sell * maxBarWidth / (float)maxVol);
				}
				else if (flag5)
				{
					num3 = (float)Convert.ToInt32((float)(-(float)sell) * maxBarWidth / (float)maxVol);
				}
				else
				{
					num3 = (float)Convert.ToInt32((float)(buy + sell) * maxBarWidth / (float)maxVol);
				}
				if (num3 > 0f)
				{
					Vector2 vector3 = new Vector2(x - num, y);
					Vector2 vector4 = new Vector2(vector3.X - num3, vector3.Y);
					global::SharpDX.Direct2D1.Brush brush4 = brush.ToDxBrush(base.RenderTarget);
					base.RenderTarget.DrawLine(vector3, vector4, brush4, (float)this.autoThickness);
					brush4.Dispose();
				}
			}
			if (flag10 && (flag || this.NumberEnabled) && allowDrawText)
			{
				global::System.Windows.Media.Brush brush5;
				if (flag)
				{
					brush5 = brush;
				}
				else
				{
					brush5 = (flag7 ? this.NumberBrushStrong : this.NumberBrushDefault);
				}
				if (!flag3)
				{
					if (flag6 || Math.Abs(sell) > 0L)
					{
						bool flag11 = false;
						if (!isToday)
						{
							flag11 = Math.Abs(sell) >= this.ThresholdStrongVolume;
						}
						TextLayout textLayout = this.GetTextLayout(sell, flag11);
						global::SharpDX.Direct2D1.Brush brush6 = brush5.ToDxBrush(base.RenderTarget);
						Vector2 vector5 = new Vector2(x - num2 - textLayout.Metrics.Width, y - textLayout.Metrics.Height / 2f);
						base.RenderTarget.DrawTextLayout(vector5, textLayout, brush6);
						brush6.Dispose();
					}
				}
				else
				{
					long num4 = buy + sell;
					if (num4 > 0L)
					{
						bool flag12 = false;
						if (!isToday)
						{
							flag12 = num4 >= 2L * this.ThresholdStrongVolume;
						}
						TextLayout textLayout2 = this.GetTextLayout(num4, flag12);
						global::SharpDX.Direct2D1.Brush brush7 = brush5.ToDxBrush(base.RenderTarget);
						Vector2 vector6 = new Vector2(x - num2 - textLayout2.Metrics.Width, y - textLayout2.Metrics.Height / 2f);
						base.RenderTarget.DrawTextLayout(vector6, textLayout2, brush7);
						brush7.Dispose();
					}
				}
			}
			if (!flag && !flag3)
			{
				float num3;
				if (flag2)
				{
					num3 = maxBarWidth;
				}
				else
				{
					num3 = (float)Convert.ToInt32((float)buy * maxBarWidth / (float)maxVol);
				}
				if (num3 > 0f)
				{
					Vector2 vector3 = new Vector2(x + num - 1f, y);
					Vector2 vector4 = new Vector2(vector3.X + num3, vector3.Y);
					global::SharpDX.Direct2D1.Brush brush8 = brush2.ToDxBrush(base.RenderTarget);
					base.RenderTarget.DrawLine(vector3, vector4, brush8, (float)this.autoThickness);
					brush8.Dispose();
				}
			}
			if (flag10 && allowDrawText && (flag || (this.NumberEnabled && !flag3)) && (flag6 || Math.Abs(buy) > 0L))
			{
				global::System.Windows.Media.Brush brush5;
				if (flag)
				{
					brush5 = brush2;
				}
				else
				{
					brush5 = (flag8 ? this.NumberBrushStrong : this.NumberBrushDefault);
				}
				bool flag13 = false;
				if (!isToday)
				{
					flag13 = buy >= this.ThresholdStrongVolume;
				}
				TextLayout textLayout3 = this.GetTextLayout(buy, flag13);
				global::SharpDX.Direct2D1.Brush brush9 = brush5.ToDxBrush(base.RenderTarget);
				Vector2 vector7 = new Vector2(x + num2 - 1f, y - textLayout3.Metrics.Height / 2f);
				base.RenderTarget.DrawTextLayout(vector7, textLayout3, brush9);
				brush9.Dispose();
			}
		}
		private void HighlightBarPOCAndPrintSummary(int bar, DDApexFlowZignal.Presentation presentation, float centerX, float centerY, double bardistance, float maxBarWidth)
		{
			float num = (float)this.autoThickness;
			float num2 = centerY - num / 2f;
			float num3;
			float num4;
			if (this.barProfileCombined)
			{
				num3 = this.cellWidth - 2f * this.candleMargin;
				num4 = centerX - this.candleMargin - num3;
			}
			else
			{
				num3 = (maxBarWidth + this.candleMargin) * 2f;
				num4 = centerX - num3 / 2f;
				num3 -= 1f;
			}
			RectangleF rectangleF = new RectangleF(num4, num2, num3, num);
			if (bardistance >= 30.0)
			{
				if (this.POCHighlightEnabled)
				{
					double deltaVol = presentation.GetDeltaVol(presentation.POCKey);
					Stroke stroke = ((deltaVol == 0.0) ? this.POCHighlightBorderNeutralStroke : ((deltaVol > 0.0) ? this.POCHighlightBorderBuyStroke : this.POCHighlightBorderSellStroke));
					global::SharpDX.Direct2D1.Brush brush = stroke.Brush.ToDxBrush(base.RenderTarget);
					base.RenderTarget.DrawRectangle(rectangleF, brush, stroke.Width, stroke.StrokeStyle);
					brush.Dispose();
				}
				if (this.SummaryEnabled)
				{
					int deltaState = this.GetDeltaState(Convert.ToInt64(presentation.VolBuy), Convert.ToInt64(presentation.VolSell));
					global::System.Windows.Media.Brush brush2 = this.SummaryDeltaZero;
					if (deltaState > 0)
					{
						brush2 = this.SummaryDeltaPositive;
					}
					else if (deltaState < 0)
					{
						brush2 = this.SummaryDeltaNegative;
					}
					long num5 = Convert.ToInt64(presentation.VolBuy - presentation.VolSell);
					string text = "0";
					if (num5 > 0L)
					{
						text = "+" + num5.ToString();
					}
					else if (num5 < 0L)
					{
						text = num5.ToString();
					}
					float num6;
					if (this.SummaryTop)
					{
						num6 = (float)this.scaleOfChart.GetYByValue(base.BarsArray[0].GetHigh(bar)) - (float)this.autoThickness / 2f - (float)this.SummaryMargin + 1f;
					}
					else
					{
						num6 = (float)this.scaleOfChart.GetYByValue(base.BarsArray[0].GetLow(bar)) + (float)this.autoThickness / 2f + (float)this.SummaryMargin - 1f;
					}
					if (this.barProfileCombined)
					{
						this.DrawText(text, this.SummaryNumberFont, centerX, num6, 0, this.SummaryTop ? (-1) : 1, brush2);
					}
					else
					{
						this.DrawText(text, this.SummaryNumberFont, rectangleF.Center.X, num6, 0, this.SummaryTop ? (-1) : 1, brush2);
					}
					if (!this.SummaryNumberOnly)
					{
						int num7 = 4;
						int num8 = 4;
						float height = this.ComputeTextSize(text, this.SummaryNumberFont, this.ScreenDPI).Height;
						float num9;
						if (this.SummaryTop)
						{
							num9 = num6 - height - (float)num7 - (float)num8;
						}
						else
						{
							num9 = num6 + height + (float)num7;
						}
						RectangleF rectangleF2 = new RectangleF(rectangleF.X, num9, rectangleF.Width, (float)num8);
						global::SharpDX.Direct2D1.Brush brush3 = brush2.ToDxBrush(base.RenderTarget);
						base.RenderTarget.DrawRectangle(rectangleF2, brush3);
						if (deltaState == 0)
						{
							base.RenderTarget.FillRectangle(rectangleF2, brush3);
						}
						else
						{
							RectangleF rectangleF3 = new RectangleF(rectangleF2.X, rectangleF2.Y, rectangleF2.Width, rectangleF2.Height);
							float num10 = (float)(presentation.VolBuy + presentation.VolSell);
							if (deltaState > 0)
							{
								rectangleF3.Width = rectangleF2.Width * (float)presentation.VolBuy / num10;
								rectangleF3.X += rectangleF2.Width - rectangleF3.Width;
							}
							else
							{
								rectangleF3.Width = rectangleF2.Width * (float)presentation.VolSell / num10;
							}
							base.RenderTarget.FillRectangle(rectangleF3, brush3);
						}
						brush3.Dispose();
					}
				}
			}
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
						this.dictPresentations.Add(this.tickVolumeBarIndex, new DDApexFlowZignal.Presentation());
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
						this.dictPresentations.Add(this.tickVolumeBarIndex, new DDApexFlowZignal.Presentation());
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
				DDApexFlowZignal.PresentationCell presentationCell = this.queuePresentationCell.Peek();
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
			if (base.BarsInProgress == 0)
			{
				int num = this.tickVolumeBarIndex - this.barShift;
				if (!this.dictPresentations.ContainsKey(num))
				{
					return;
				}
				DDApexFlowZignal.Presentation presentation = this.dictPresentations[num];
				if (presentation.VolDelta > 0.0)
				{
					this.listRawPositiveDelta.Add(presentation.VolDelta);
					this.seriesVolumeDeltaPositive[0] = this.ComputeSMA(ref this.sumPositiveDelta, this.listRawPositiveDelta, 0, this.AvgDeltaPeriod, false);
					if (num > 0)
					{
						this.seriesVolumeDeltaNegative[0] = this.seriesVolumeDeltaNegative[1];
					}
				}
				else if (presentation.VolDelta < 0.0)
				{
					this.listRawNegativeDelta.Add(presentation.VolDelta);
					this.seriesVolumeDeltaNegative[0] = this.ComputeSMA(ref this.sumNegativeDelta, this.listRawNegativeDelta, 0, this.AvgDeltaPeriod, false);
					if (num > 0)
					{
						this.seriesVolumeDeltaPositive[0] = this.seriesVolumeDeltaPositive[1];
					}
				}
				else if (num > 0)
				{
					this.seriesVolumeDeltaNegative[0] = this.seriesVolumeDeltaNegative[1];
					this.seriesVolumeDeltaPositive[0] = this.seriesVolumeDeltaPositive[1];
					this.seriesVolumeDeltaPositive[0] = this.seriesVolumeDeltaPositive[1];
					this.seriesVolumeDeltaNegative[0] = this.seriesVolumeDeltaNegative[1];
				}
				this.seriesBody[0] = Math.Abs(base.Closes[0][0] - base.Opens[0][0]);
				this.seriesRange[0] = base.Highs[0][0] - base.Lows[0][0];
				this.seriesVolumeTotal[0] = presentation.GetTotalVol();
				this.VolumeBuy[0] = (this.seriesVolumeBuy[0] = presentation.VolBuy);
				this.VolumeSell[0] = (this.seriesVolumeSell[0] = presentation.VolSell);
				this.VolumeDelta[0] = (this.seriesVolumeDelta[0] = presentation.VolDelta);
				this.VolumeBuyAvg[0] = this.ComputeMAValue(this.seriesVolumeBuy, DD_MAType.SMA, this.AvgVolPeriod);
				this.VolumeSellAvg[0] = this.ComputeMAValue(this.seriesVolumeSell, DD_MAType.SMA, this.AvgVolPeriod);
			}
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
			catch (Exception)
			{
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
				this.queuePresentationCell.Enqueue(new DDApexFlowZignal.PresentationCell(volume, 0.0, key, price, timeTicks, currentBar));
				return;
			}
			this.queuePresentationCell.Enqueue(new DDApexFlowZignal.PresentationCell(0.0, volume, key, price, timeTicks, currentBar));
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
			if (this.VolumeBase == DDApexFlowZignal_VolumeBase.UpDownTick_UnitVolume)
			{
				return 1L;
			}
			return Convert.ToInt64(vol);
		}
		private void AddMarker(int barIndex, bool isBullish, DDApexFlowZignal.SignalType signalType)
		{
			if (!this.MarkerEnabled)
			{
				return;
			}
			DDApexFlowZignal.SignalInfo signalInfo = new DDApexFlowZignal.SignalInfo(isBullish, signalType);
			if (!this.dictMarkers.ContainsKey(barIndex))
			{
				DDApexFlowZignal.MarkerInfo markerInfo = new DDApexFlowZignal.MarkerInfo(barIndex, isBullish, new List<DDApexFlowZignal.SignalInfo>());
				markerInfo.ListOfSignalInfo.Add(signalInfo);
				this.dictMarkers.Add(barIndex, markerInfo);
				return;
			}
			this.dictMarkers[barIndex].ListOfSignalInfo.Add(signalInfo);
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
		private void DrawOneMarker(ChartScale chartScale, DDApexFlowZignal.MarkerInfo markerInfo)
		{
			int count = markerInfo.ListOfSignalInfo.Count;
			if (count == 0)
			{
				return;
			}
			int barIndex = markerInfo.BarIndex;
			float num = (float)base.ChartControl.GetXByBarIndex(base.ChartBars, barIndex);
			float num2 = (float)(chartScale.GetYByValue(base.Highs[0].GetValueAt(barIndex)) - this.MarkerOffset);
			float num3 = (float)(chartScale.GetYByValue(base.Lows[0].GetValueAt(barIndex)) + this.MarkerOffset);
			int num4 = -1;
			int num5 = -1;
			for (int i = 0; i <= count - 1; i++)
			{
				DDApexFlowZignal.SignalInfo signalInfo = markerInfo.ListOfSignalInfo[i];
				string markerString = this.GetMarkerString(signalInfo.IsBullish, signalInfo.SignalType);
				string text = this.FormatMarkerString(markerString);
				if (string.IsNullOrWhiteSpace(text))
				{
					return;
				}
				bool isBullish = signalInfo.IsBullish;
				if (!isBullish && base.Highs[0].GetValueAt(barIndex).ApproxCompare(chartScale.MaxValue) >= 0)
				{
					return;
				}
				if (isBullish && base.Lows[0].GetValueAt(barIndex).ApproxCompare(chartScale.MinValue) <= 0)
				{
					return;
				}
				global::System.Windows.Media.Brush markerBrush = this.GetMarkerBrush(isBullish, signalInfo.SignalType);
				if (markerBrush.IsTransparent())
				{
					return;
				}
				int num6 = (isBullish ? 1 : (-1));
				float num7;
				if (isBullish)
				{
					num4++;
					num3 = (num7 = num3 + (float)num6 * ((float)this.MarkerOffset + this.MarkerFont.TextFormatHeight + 20f) * (float)num4);
				}
				else
				{
					num5++;
					num2 = (num7 = num2 + (float)num6 * ((float)this.MarkerOffset + this.MarkerFont.TextFormatHeight + 20f) * (float)num5);
				}
				this.DrawText(text, this.MarkerFont, num, num7, 0, num6, markerBrush, this.ScreenDPI, base.RenderTarget);
			}
		}
		public override void OnCalculateMinMax()
		{
			try
			{
				if (this.DeltaChartEnabled)
				{
					this.minPrice = double.MaxValue;
					this.maxPrice = double.MinValue;
					for (int i = base.ChartBars.FromIndex; i <= base.ChartBars.ToIndex; i++)
					{
						this.minPrice = Math.Min(this.minPrice, base.Lows[0].GetValueAt(i));
						this.maxPrice = Math.Max(this.maxPrice, base.Highs[0].GetValueAt(i));
					}
					double num = (base.ChartPanel.MaxValue - base.ChartPanel.MinValue) * (double)this.heightRatio;
					base.MaxValue = this.maxPrice;
					base.MinValue = this.minPrice - num;
				}
			}
			catch
			{
			}
		}
		private void SetupMouseInfoUI()
		{
			if (this.borderMouseInfo != null)
			{
				return;
			}
			this.txtMouseInfo = new TextBlock
			{
				Foreground = Brushes.White,
				FontSize = 11.0,
				Margin = new Thickness(5.0),
				TextAlignment = global::System.Windows.TextAlignment.Left
			};
			this.borderMouseInfo = new Border
			{
				Background = new global::System.Windows.Media.SolidColorBrush(global::System.Windows.Media.Color.FromArgb(200, 30, 30, 30)),
				BorderBrush = Brushes.Gray,
				BorderThickness = new Thickness(1.0),
				CornerRadius = new CornerRadius(3.0),
				Child = this.txtMouseInfo,
				Visibility = Visibility.Collapsed,
				IsHitTestVisible = false
			};
			this.canvasMouseInfo = new Canvas
			{
				IsHitTestVisible = false
			};
			this.canvasMouseInfo.Children.Add(this.borderMouseInfo);
			base.UserControlCollection.Add(this.canvasMouseInfo);
		}
		private void SetupResetButton()
		{
			if (!this.DeltaChartEnabled || this.btnResetScale != null)
			{
				return;
			}
			this.btnResetScale = new Button
			{
				Content = "F",
				Width = 20.0,
				Height = 20.0,
				MinWidth = 0.0,
				MinHeight = 0.0,
				FontSize = 14.0,
				Background = Brushes.Transparent,
				Foreground = this.MainWindowTextColor,
				Visibility = Visibility.Collapsed,
				VerticalAlignment = VerticalAlignment.Bottom,
				HorizontalAlignment = HorizontalAlignment.Right,
				Padding = new Thickness(0.0)
			};
			this.btnResetScale.Margin = new Thickness(0.0, 0.0, this.actualMarginX + 3.0, this.actualMarginY - this.btnResetScale.Height);
			this.btnResetScale.Click += delegate(object s, RoutedEventArgs e)
			{
				this.ResetScale();
			};
			base.UserControlCollection.Add(this.btnResetScale);
		}
		private void ResetScale()
		{
			this.isManualScaling = false;
			this.btnResetScale.Visibility = Visibility.Collapsed;
			this.isShowInfoActive = false;
			if (this.borderMouseInfo != null)
			{
				this.borderMouseInfo.Visibility = Visibility.Collapsed;
			}
			base.ChartControl.InvalidateVisual();
		}
		private void OnMouseDown(object sender, MouseButtonEventArgs e)
		{
			Action cachedAction1 = null;
			base.TriggerCustomEvent(delegate(object state)
			{
				try
				{
					if (this.isCharting)
					{
						Dispatcher dispatcher = this.ChartControl.Dispatcher;
						Action action;
						if ((action = cachedAction1) == null)
						{
							action = (cachedAction1 = delegate
							{
								global::System.Windows.Point position = e.GetPosition(this.ChartControl);
								if (e.ChangedButton == MouseButton.Left)
								{
									if (position.X > this.ChartPanel.ActualWidth - this.actualMarginX && position.Y > this.ChartPanel.ActualHeight - this.actualMarginY)
									{
										this.SetupResetButton();
										this.isScaling = true;
										this.lastMouseY = position.Y;
										this.ChartControl.CaptureMouse();
										return;
									}
								}
								else if (e.ChangedButton == MouseButton.Middle && position.X >= 0.0 && position.X <= this.ChartPanel.ActualWidth - this.actualMarginX && position.Y >= 0.0 && position.Y >= this.ChartPanel.ActualHeight - this.actualMarginY)
								{
									this.isShowInfoActive = !this.isShowInfoActive;
									if (this.isShowInfoActive)
									{
										this.SetupMouseInfoUI();
										this.UpdateMouseText(position);
									}
									if (this.borderMouseInfo != null)
									{
										this.borderMouseInfo.Visibility = (this.isShowInfoActive ? Visibility.Visible : Visibility.Collapsed);
									}
								}
							});
						}
						dispatcher.InvokeAsync(action);
					}
				}
				catch
				{
				}
			}, e);
		}
		private void OnMouseMove(object sender, MouseEventArgs e)
		{
			Action cachedAction1 = null;
			base.TriggerCustomEvent(delegate(object state)
			{
				try
				{
					if (this.isCharting)
					{
						Dispatcher dispatcher = this.ChartControl.Dispatcher;
						Action action;
						if ((action = cachedAction1) == null)
						{
							action = (cachedAction1 = delegate
							{
								global::System.Windows.Point position = e.GetPosition(this.ChartControl);
								if (this.isScaling)
								{
									double num = position.Y - this.lastMouseY;
									this.ApplyZoomLogic(num);
									this.lastMouseY = position.Y;
								}
								if (this.isShowInfoActive)
								{
									if (position.X < 0.0 || position.X > this.ChartPanel.ActualWidth - this.actualMarginX || position.Y < 0.0 || position.Y < this.ChartPanel.ActualHeight - this.actualMarginY)
									{
										this.isShowInfoActive = false;
										this.borderMouseInfo.Visibility = Visibility.Collapsed;
										return;
									}
									this.UpdateMouseText(position);
									Canvas.SetLeft(this.borderMouseInfo, position.X + 15.0);
									Canvas.SetTop(this.borderMouseInfo, position.Y + 15.0);
								}
							});
						}
						dispatcher.InvokeAsync(action);
					}
				}
				catch
				{
				}
			}, e);
		}
		private void UpdateMouseText(global::System.Windows.Point p)
		{
			this.retrieveBarIndex = this.RetrieveBarIndex();
			DDApexFlowZignal.Presentation presentation = this.dictPresentations[this.retrieveBarIndex];
			this.txtMouseInfo.Text = string.Format("Vol Delta: {0:F0}\nVol Buy: {1:F0}\nVol Sell: {2:F0}\nAvg Buy: {3:F0}\nAvg Sell: {4:F0}\nAvg Delta+: {5:F0}\nAvg Delta-: {6:F0}", new object[]
			{
				presentation.VolDelta,
				presentation.VolBuy,
				presentation.VolSell,
				this.VolumeBuyAvg.GetValueAt(this.retrieveBarIndex),
				this.VolumeSellAvg.GetValueAt(this.retrieveBarIndex),
				this.seriesVolumeDeltaPositive.GetValueAt(this.retrieveBarIndex),
				this.seriesVolumeDeltaNegative.GetValueAt(this.retrieveBarIndex)
			});
			Canvas.SetLeft(this.borderMouseInfo, p.X + 15.0);
			Canvas.SetTop(this.borderMouseInfo, p.Y + 15.0);
		}
		private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			base.TriggerCustomEvent(delegate(object state)
			{
				try
				{
					if (this.isCharting)
					{
						base.ChartControl.Dispatcher.InvokeAsync(delegate
						{
							if (this.isScaling)
							{
								this.isScaling = false;
								base.ChartControl.ReleaseMouseCapture();
							}
						});
					}
				}
				catch
				{
				}
			}, e);
		}
		private void ApplyZoomLogic(double deltaY)
		{
			this.isManualScaling = true;
			double num = this.maxVol * 2.0;
			double num2 = deltaY * 0.005;
			double num3 = num * num2;
			this.maxVol += num3;
			if (this.btnResetScale.Visibility != Visibility.Visible)
			{
				this.btnResetScale.Visibility = Visibility.Visible;
			}
			base.ChartControl.InvalidateVisual();
		}
		private int RetrieveBarIndex()
		{
			global::System.Windows.Point position = Mouse.GetPosition(base.ChartControl);
			global::System.Windows.Point point = new global::System.Windows.Point(position.X * (double)this.ScreenDPI / 100.0, position.Y);
			int num = Convert.ToInt32(point.X);
			DateTime timeByX = base.ChartControl.GetTimeByX(num);
			int bar = base.BarsArray[0].GetBar(timeByX);
			if (bar <= 0)
			{
				return 0;
			}
			int num2 = bar - 1;
			int xbyBarIndex = base.ChartControl.GetXByBarIndex(base.ChartBars, num2);
			int xbyBarIndex2 = base.ChartControl.GetXByBarIndex(base.ChartBars, bar);
			if (xbyBarIndex >= xbyBarIndex2)
			{
				return bar;
			}
			if (num < xbyBarIndex)
			{
				return num2;
			}
			if (num > xbyBarIndex2)
			{
				return bar;
			}
			int num3 = num - xbyBarIndex;
			int num4 = xbyBarIndex2 - num;
			if (num3 <= num4)
			{
				return num2;
			}
			return bar;
		}
		public double RealToWpf(double physicalPixel)
		{
			if (base.ChartPanel.W == 0)
			{
				return 0.0;
			}
			double num = base.ChartPanel.ActualWidth / (double)base.ChartPanel.W;
			return physicalPixel * num;
		}
		private void RegisterNumericValidation(TextBox textBox, int min = 1, int max = 100)
		{
			textBox.PreviewTextInput += delegate(object s, TextCompositionEventArgs e)
			{
				e.Handled = !Regex.IsMatch(e.Text, "^[0-9]+$");
			};
			textBox.PreviewKeyDown += delegate(object s, KeyEventArgs e)
			{
				if (e.Key == Key.Space)
				{
					e.Handled = true;
				}
			};
			textBox.TextChanged += delegate(object s, TextChangedEventArgs e)
			{
				if (string.IsNullOrEmpty(textBox.Text))
				{
					return;
				}
				int num;
				if (int.TryParse(textBox.Text, out num))
				{
					if (num < min)
					{
						textBox.Text = min.ToString();
					}
					else if (num > max)
					{
						textBox.Text = max.ToString();
					}
					textBox.SelectionStart = textBox.Text.Length;
				}
			};
		}
		public string GetIconOnly(string text)
		{
			if (string.IsNullOrEmpty(text))
			{
				return string.Empty;
			}
			return Regex.Replace(text, "[a-zA-Z\\s\\+]", "");
		}
		private PropertyInfo[] GetPropertiesWithAttribute<TAttr>() where TAttr : Attribute
		{
			return (from prop in base.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
				where Attribute.IsDefined(prop, typeof(TAttr))
				select prop).ToArray<PropertyInfo>();
		}
		public void CreateBodies(StackPanel stackPanel, DDApexFlowZignal.SignalType signalType)
		{
			try
			{
				SortedList<int, DDApexFlowZignal.SignalConditionBody> sortedList = ((signalType == NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.SignalType.Absorption) ? this.dictAbsorptionCondition : ((signalType == NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.SignalType.Exhaustion) ? this.dictExhaustionCondition : this.dictPushCondition));
				sortedList.Clear();
				foreach (PropertyInfo propertyInfo in this.properties)
				{
					DDApexFlowZignal.ConditionPropertiesAttribute customAttribute = propertyInfo.GetCustomAttribute<DDApexFlowZignal.ConditionPropertiesAttribute>();
					if (customAttribute != null && customAttribute.SignalType == signalType)
					{
						int key = customAttribute.Key;
						string conditionText = customAttribute.ConditionText;
						bool isBuy = customAttribute.IsBuy;
						bool isHeaderNote = customAttribute.IsHeaderNote;
						DDApexFlowZignal.FuncType funcType = customAttribute.FuncType;
						object value = propertyInfo.GetValue(this);
						bool flag = value is bool && (bool)value;
						if (isHeaderNote)
						{
							if (!sortedList.ContainsKey(key))
							{
								string headerLeft = customAttribute.HeaderLeft;
								string headerRight = customAttribute.HeaderRight;
								bool isOppositeDirection = customAttribute.IsOppositeDirection;
								bool isWeak = customAttribute.IsWeak;
								sortedList.Add(key, new DDApexFlowZignal.SignalConditionBody
								{
									IsHeaderNote = true,
									HeaderLeft = headerLeft,
									HeaderRight = headerRight,
									IsOppositeDirection = isOppositeDirection,
									IsWeak = isWeak
								});
							}
						}
						else if (sortedList.ContainsKey(key))
						{
							DDApexFlowZignal.SignalConditionBody signalConditionBody = sortedList[key];
							if (isBuy)
							{
								signalConditionBody.IsBuyEnabled = flag;
								signalConditionBody.ConditionBuyText = customAttribute.ConditionText;
							}
							else
							{
								signalConditionBody.IsSellEnabled = flag;
								signalConditionBody.ConditionSellText = customAttribute.ConditionText;
							}
						}
						else
						{
							DDApexFlowZignal.SignalConditionBody signalConditionBody2 = new DDApexFlowZignal.SignalConditionBody();
							if (isBuy)
							{
								signalConditionBody2.IsBuyEnabled = flag;
								signalConditionBody2.ConditionBuyText = customAttribute.ConditionText;
							}
							else
							{
								signalConditionBody2.IsSellEnabled = flag;
								signalConditionBody2.ConditionSellText = customAttribute.ConditionText;
							}
							sortedList.Add(key, signalConditionBody2);
						}
					}
				}
				int count = sortedList.Count;
				for (int j = 0; j < count; j++)
				{
					DDApexFlowZignal.SignalConditionBody signalConditionBody3 = sortedList.Values[j];
					if (signalConditionBody3.IsHeaderNote)
					{
						global::System.Windows.Media.Brush brush;
						global::System.Windows.Media.Brush brush2;
						if (!signalConditionBody3.IsWeak)
						{
							brush = (signalConditionBody3.IsOppositeDirection ? Brushes.HotPink : Brushes.LimeGreen);
							brush2 = (signalConditionBody3.IsOppositeDirection ? Brushes.LimeGreen : Brushes.HotPink);
						}
						else
						{
							brush = (signalConditionBody3.IsOppositeDirection ? Brushes.Pink : Brushes.DarkSeaGreen);
							brush2 = (signalConditionBody3.IsOppositeDirection ? Brushes.DarkSeaGreen : Brushes.Pink);
						}
						Border border = this.CreateBorderHeader(false, signalConditionBody3.HeaderLeft, brush, signalConditionBody3.HeaderRight, brush2, HorizontalAlignment.Left, "", "");
						stackPanel.Children.Add(border);
					}
					else
					{
						DDApexFlowZignal.SignalConditionBody signalConditionBody4 = this.CreateGridCheckBoxCondition(signalConditionBody3.ConditionBuyText, signalConditionBody3.IsBuyEnabled, signalConditionBody3.ConditionSellText, signalConditionBody3.IsSellEnabled);
						stackPanel.Children.Add(signalConditionBody4.Grid);
						signalConditionBody3.CheckBoxBuy = signalConditionBody4.CheckBoxBuy;
						signalConditionBody3.CheckBoxSell = signalConditionBody4.CheckBoxSell;
					}
				}
			}
			catch (Exception ex)
			{
				this.PrintException(ex);
			}
		}
		public void SetConditionParameters(DDApexFlowZignal.SignalType signalType)
		{
			try
			{
				SortedList<int, DDApexFlowZignal.SignalConditionBody> sortedList;
				if (signalType == NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.SignalType.Absorption)
				{
					sortedList = this.dictAbsorptionCondition;
					int num;
					if (this.txtAbsorptionN != null && int.TryParse(this.txtAbsorptionN.Text, out num) && num > 0)
					{
						this.AbsorptionN = num;
					}
				}
				else if (signalType == NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.SignalType.Exhaustion)
				{
					sortedList = this.dictExhaustionCondition;
					int num2;
					if (this.txtExhaustionN != null && int.TryParse(this.txtExhaustionN.Text, out num2) && num2 > 0)
					{
						this.ExhaustionN = num2;
					}
				}
				else
				{
					sortedList = this.dictPushCondition;
					int num3;
					if (this.txtPushN != null && int.TryParse(this.txtPushN.Text, out num3) && num3 > 0)
					{
						this.PushN = num3;
					}
					foreach (KeyValuePair<string, string> keyValuePair in this.tempParameterValueBuffer)
					{
						string key = keyValuePair.Key;
						string value = keyValuePair.Value;
						if (!string.IsNullOrEmpty(value))
						{
							this.SetPropertyValue(key, value);
						}
					}
					this.tempParameterValueBuffer.Clear();
				}
				foreach (PropertyInfo propertyInfo in this.properties)
				{
					DDApexFlowZignal.ConditionPropertiesAttribute customAttribute = propertyInfo.GetCustomAttribute<DDApexFlowZignal.ConditionPropertiesAttribute>();
					if (customAttribute != null && !customAttribute.IsHeaderNote && customAttribute.SignalType == signalType && sortedList.ContainsKey(customAttribute.Key))
					{
						DDApexFlowZignal.SignalConditionBody signalConditionBody = sortedList[customAttribute.Key];
						bool value2 = (customAttribute.IsBuy ? signalConditionBody.CheckBoxBuy.IsChecked : signalConditionBody.CheckBoxSell.IsChecked).Value;
						propertyInfo.SetValue(this, value2);
					}
				}
			}
			catch (Exception ex)
			{
				this.PrintException(ex);
			}
		}
		private void PrintMarker(bool isBullish, DDApexFlowZignal.SignalType signalType, int barIndex)
		{
			if (!this.isCharting)
			{
				return;
			}
			if (!this.MarkerEnabled)
			{
				return;
			}
			if (barIndex < base.BarsRequiredToPlot)
			{
				return;
			}
			string text = "DDApexFlowZignal.marker." + this.GetTagSuffix(isBullish, signalType) + barIndex.ToString();
			global::System.Windows.Media.Brush markerBrush = this.GetMarkerBrush(isBullish, signalType);
			double num = (isBullish ? this.GetValueSeries(base.Lows[0], barIndex, this.isClickEvent) : this.GetValueSeries(base.Highs[0], barIndex, this.isClickEvent));
			string text2 = this.GetMarkerString(isBullish, signalType);
			text2 = this.FormatMarkerString(text2);
			int num2 = Convert.ToInt32(this.ComputeTextSize(text2, this.MarkerFont, this.ScreenDPI).Height);
			int num4;
			if (!isBullish)
			{
				int num3 = this.factorTop + 1;
				this.factorTop = num3;
				num4 = num3;
			}
			else
			{
				int num3 = this.factorBottom - 1;
				this.factorBottom = num3;
				num4 = num3;
			}
			int num5 = num4 * (this.MarkerOffset + num2 / 2);
			NinjaTrader.NinjaScript.DrawingTools.Draw.Text(this, text, base.IsAutoScale, text2, base.CurrentBars[0] - barIndex, num, num5, markerBrush, this.MarkerFont, global::System.Windows.TextAlignment.Center, Brushes.Transparent, Brushes.Transparent, 0);
		}
		private string GetTagSuffix(bool isBullish, DDApexFlowZignal.SignalType signalType)
		{
			if (signalType == NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.SignalType.Push)
			{
				if (!isBullish)
				{
					return "push.bearish.";
				}
				return "push.bullish.";
			}
			else if (signalType == NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.SignalType.Absorption)
			{
				if (!isBullish)
				{
					return "absorption.bearish.";
				}
				return "absorption.bullish.";
			}
			else if (signalType == NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.SignalType.Exhaustion)
			{
				if (!isBullish)
				{
					return "exhaustion.bearish.";
				}
				return "exhaustion.bullish.";
			}
			else if (signalType == NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.SignalType.AbPush)
			{
				if (!isBullish)
				{
					return "abpush.bearish.";
				}
				return "abpush.bullish.";
			}
			else if (signalType == NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.SignalType.ExPush)
			{
				if (!isBullish)
				{
					return "expush.bearish.";
				}
				return "expush.bullish.";
			}
			else
			{
				if (!isBullish)
				{
					return "bearish.";
				}
				return "bullish.";
			}
		}
		private global::System.Windows.Media.Brush GetMarkerBrush(bool isBullish, DDApexFlowZignal.SignalType signalType)
		{
			if (signalType == NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.SignalType.Push)
			{
				if (!isBullish)
				{
					return this.MarkerPushBrushBearish;
				}
				return this.MarkerPushBrushBullish;
			}
			else if (signalType == NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.SignalType.Absorption)
			{
				if (!isBullish)
				{
					return this.MarkerAbsorptionBrushBearish;
				}
				return this.MarkerAbsorptionBrushBullish;
			}
			else if (signalType == NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.SignalType.Exhaustion)
			{
				if (!isBullish)
				{
					return this.MarkerExhaustionBrushBearish;
				}
				return this.MarkerExhaustionBrushBullish;
			}
			else if (signalType == NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.SignalType.AbPush)
			{
				if (!isBullish)
				{
					return this.MarkerAbPushBrushBearish;
				}
				return this.MarkerAbPushBrushBullish;
			}
			else if (signalType == NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.SignalType.ExPush)
			{
				if (!isBullish)
				{
					return this.MarkerExPushBrushBearish;
				}
				return this.MarkerExPushBrushBullish;
			}
			else
			{
				if (!isBullish)
				{
					return this.MarkerPushBrushBearish;
				}
				return this.MarkerPushBrushBullish;
			}
		}
		private string GetMarkerString(bool isBullish, DDApexFlowZignal.SignalType signalType)
		{
			if (signalType == NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.SignalType.Push)
			{
				if (!isBullish)
				{
					return this.MarkerPushStringBearish;
				}
				return this.MarkerPushStringBullish;
			}
			else if (signalType == NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.SignalType.Exhaustion)
			{
				if (!isBullish)
				{
					return this.MarkerExhaustionStringBearish;
				}
				return this.MarkerExhaustionStringBullish;
			}
			else if (signalType == NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.SignalType.Absorption)
			{
				if (!isBullish)
				{
					return this.MarkerAbsorptionStringBearish;
				}
				return this.MarkerAbsorptionStringBullish;
			}
			else if (signalType == NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.SignalType.AbPush)
			{
				if (!isBullish)
				{
					return this.MarkerAbPushStringBearish;
				}
				return this.MarkerAbPushStringBullish;
			}
			else
			{
				if (!isBullish)
				{
					return this.MarkerExPushStringBearish;
				}
				return this.MarkerExPushStringBullish;
			}
		}
		private int GetDeltaState(long buy, long sell)
		{
			if (buy == sell)
			{
				return 0;
			}
			if (buy > sell)
			{
				if (buy >= this.ThresholdStrongVolume && (double)(buy - sell) >= (double)buy * this.ThresholdStrongDeltaPercentage / 100.0)
				{
					return 2;
				}
				return 1;
			}
			else
			{
				if (sell >= this.ThresholdStrongVolume && (double)(sell - buy) >= (double)sell * this.ThresholdStrongDeltaPercentage / 100.0)
				{
					return -2;
				}
				return -1;
			}
		}
		private int GetPixelDistanceByKeys(int startKey, int endKey)
		{
			return Math.Abs(this.scaleOfChart.GetYByValue((double)startKey * base.TickSize) - this.scaleOfChart.GetYByValue((double)endKey * base.TickSize));
		}
		private void DrawText(string text, SimpleFont font, float x, float y, int horizontalAlignment, int verticalAlignment, global::System.Windows.Media.Brush brush)
		{
			TextFormat textFormat = font.ToDirectWriteTextFormat();
			int num = 5;
			Size2F size2F = this.ComputeTextSize(text, font, this.ScreenDPI);
			float num2 = size2F.Width + (float)(2 * num);
			float height = size2F.Height;
			float num3;
			if (horizontalAlignment > 0)
			{
				num3 = x;
				textFormat.TextAlignment = global::SharpDX.DirectWrite.TextAlignment.Leading;
			}
			else if (horizontalAlignment < 0)
			{
				num3 = x - num2;
				textFormat.TextAlignment = global::SharpDX.DirectWrite.TextAlignment.Trailing;
			}
			else
			{
				num3 = x - num2 / 2f;
				textFormat.TextAlignment = global::SharpDX.DirectWrite.TextAlignment.Center;
			}
			float num4;
			if (verticalAlignment > 0)
			{
				num4 = y;
			}
			else if (verticalAlignment < 0)
			{
				num4 = y - height;
			}
			else
			{
				num4 = y - height / 2f;
			}
			RectangleF rectangleF = new RectangleF(num3, num4, num2, height);
			global::SharpDX.Direct2D1.Brush brush2 = brush.ToDxBrush(base.RenderTarget);
			base.RenderTarget.DrawText(text, textFormat, rectangleF, brush2);
			brush2.Dispose();
		}
		private void PrintException(Exception exception)
		{
			string text = string.Concat(new string[]
			{
				"DDApexFlowZignal: ",
				exception.ToString(),
				" (",
				exception.StackTrace,
				")"
			});
			base.Print(text);
			NinjaScript.Log(text, LogLevel.Error);
		}
		private void OnBtnArsorptionSetting_Click(object sender, RoutedEventArgs e)
		{
			base.TriggerCustomEvent(delegate(object state)
			{
				this.BuildAbsorptionWindowNT();
			}, e);
		}
		private void OnBtnExhaustionSetting_Click(object sender, RoutedEventArgs e)
		{
			base.TriggerCustomEvent(delegate(object state)
			{
				this.BuildExhaustionWindowNT();
			}, e);
		}
		private void OnBtnPushSetting_Click(object sender, RoutedEventArgs e)
		{
			base.TriggerCustomEvent(delegate(object state)
			{
				this.BuildPushWindowNT();
			}, e);
		}
		private void OnWindowSizeChanged(object sender, EventArgs e)
		{
			Action cachedAction1 = null;
			base.TriggerCustomEvent(delegate(object state)
			{
				try
				{
					if (this.isCharting)
					{
						Dispatcher dispatcher = this.ChartControl.Dispatcher;
						Action action;
						if ((action = cachedAction1) == null)
						{
							action = (cachedAction1 = delegate
							{
								NTWindow ntwindow = sender as NTWindow;
								if (ntwindow == this.mainWindowNT)
								{
									this.MainWindowLeft = this.mainWindowNT.Left;
									this.MainWindowTop = this.mainWindowNT.Top;
									this.MainWindowWidth = this.mainWindowNT.Width;
									this.MainWindowHeight = this.mainWindowNT.Height;
									return;
								}
								if (ntwindow == this.absorptionWindowNT)
								{
									this.AbsorptionWindowLeft = this.absorptionWindowNT.Left;
									this.AbsorptionWindowTop = this.absorptionWindowNT.Top;
									this.AbsorptionWindowWidth = this.absorptionWindowNT.Width;
									this.AbsorptionWindowHeight = this.absorptionWindowNT.Height;
									return;
								}
								if (ntwindow == this.exhaustionWindowNT)
								{
									this.ExhaustionWindowLeft = this.exhaustionWindowNT.Left;
									this.ExhaustionWindowTop = this.exhaustionWindowNT.Top;
									this.ExhaustionWindowWidth = this.exhaustionWindowNT.Width;
									this.ExhaustionWindowHeight = this.exhaustionWindowNT.Height;
									return;
								}
								if (ntwindow == this.pushWindowNT)
								{
									this.PushWindowLeft = this.pushWindowNT.Left;
									this.PushWindowTop = this.pushWindowNT.Top;
									this.PushWindowWidth = this.pushWindowNT.Width;
									this.PushWindowHeight = this.pushWindowNT.Height;
								}
							});
						}
						dispatcher.InvokeAsync(action);
					}
				}
				catch (Exception ex)
				{
					this.PrintException(ex);
				}
			}, e);
		}
		private void OnWindowNT_Closing(object sender, CancelEventArgs e)
		{
			Action cachedAction1 = null;
			base.TriggerCustomEvent(delegate(object state)
			{
				if (this.isCharting)
				{
					Dispatcher dispatcher = this.ChartControl.Dispatcher;
					Action action;
					if ((action = cachedAction1) == null)
					{
						action = (cachedAction1 = delegate
						{
							NTWindow ntwindow = sender as NTWindow;
							if (ntwindow == this.mainWindowNT)
							{
								this.btnMainCancel.Click -= this.OnBtnCancel_Click;
								this.btnMainOK.Click -= this.OnBtnOK_Click;
								this.btnMainApply.Click -= this.OnBtnApply_Click;
								this.gridMainContent = null;
								this.btnMainApply = (this.btnMainOK = (this.btnMainCancel = null));
								if (this.mainWindowNT != null)
								{
									this.mainWindowNT.Closing -= this.OnWindowNT_Closing;
									this.mainWindowNT = null;
									return;
								}
							}
							else if (ntwindow == this.absorptionWindowNT)
							{
								this.btnAbsorptionCancel.Click -= this.OnBtnCancel_Click;
								this.btnAbsorptionOK.Click -= this.OnBtnOK_Click;
								this.btnAbsorptionApply.Click -= this.OnBtnApply_Click;
								this.gridAbsorptionContent = null;
								this.btnAbsorptionApply = (this.btnAbsorptionOK = (this.btnAbsorptionCancel = null));
								this.txtAbsorptionN = null;
								if (this.absorptionWindowNT != null)
								{
									this.absorptionWindowNT.Closing -= this.OnWindowNT_Closing;
									this.absorptionWindowNT = null;
									return;
								}
							}
							else if (ntwindow == this.exhaustionWindowNT)
							{
								this.btnExhaustionCancel.Click -= this.OnBtnCancel_Click;
								this.btnExhaustionOK.Click -= this.OnBtnOK_Click;
								this.btnExhaustionApply.Click -= this.OnBtnApply_Click;
								this.gridExhaustionContent = null;
								this.btnExhaustionApply = (this.btnExhaustionOK = (this.btnExhaustionCancel = null));
								this.txtExhaustionN = null;
								if (this.exhaustionWindowNT != null)
								{
									this.exhaustionWindowNT.Closing -= this.OnWindowNT_Closing;
									this.exhaustionWindowNT = null;
									return;
								}
							}
							else if (ntwindow == this.pushWindowNT)
							{
								this.btnPushCancel.Click -= this.OnBtnCancel_Click;
								this.btnPushOK.Click -= this.OnBtnOK_Click;
								this.btnPushApply.Click -= this.OnBtnApply_Click;
								this.gridPushContent = null;
								this.btnPushApply = (this.btnPushOK = (this.btnPushCancel = null));
								this.txtPushN = null;
								if (this.pushWindowNT != null)
								{
									this.pushWindowNT.Closing -= this.OnWindowNT_Closing;
									this.pushWindowNT = null;
								}
							}
						});
					}
					dispatcher.InvokeAsync(action);
				}
			}, e);
		}
		private void OnBtnCancel_Click(object sender, RoutedEventArgs e)
		{
			Action cachedAction1 = null;
			base.TriggerCustomEvent(delegate(object state)
			{
				if (this.isCharting)
				{
					Dispatcher dispatcher = this.ChartControl.Dispatcher;
					Action action;
					if ((action = cachedAction1) == null)
					{
						action = (cachedAction1 = delegate
						{
							Button button = sender as Button;
							if (button == this.btnMainCancel)
							{
								this.TerminateMainWindowNT(this.mainWindowNT);
								return;
							}
							if (button == this.btnAbsorptionCancel)
							{
								this.TerminateMainWindowNT(this.absorptionWindowNT);
								return;
							}
							if (button == this.btnExhaustionCancel)
							{
								this.TerminateMainWindowNT(this.exhaustionWindowNT);
								return;
							}
							if (button == this.btnPushCancel)
							{
								this.TerminateMainWindowNT(this.pushWindowNT);
							}
						});
					}
					dispatcher.InvokeAsync(action);
				}
			}, e);
		}
		private void OnBtnOK_Click(object sender, RoutedEventArgs e)
		{
			Action cachedAction1 = null;
			base.TriggerCustomEvent(delegate(object state)
			{
				try
				{
					if (this.isCharting)
					{
						Dispatcher dispatcher = this.ChartControl.Dispatcher;
						Action action;
						if ((action = cachedAction1) == null)
						{
							action = (cachedAction1 = delegate
							{
								this.isClickEvent = true;
								this.RemoveDrawObjects();
								this.dictMarkers.Clear();
								Button button = sender as Button;
								this.SetAllConditionParameters(button, true);
								this.UpdateConditionGroup();
								if (button == this.btnMainOK)
								{
									this.mainWindowNT.Close();
								}
								else if (button == this.btnAbsorptionOK)
								{
									this.absorptionWindowNT.Close();
								}
								else if (button == this.btnExhaustionOK)
								{
									this.exhaustionWindowNT.Close();
								}
								else if (button == this.btnPushOK)
								{
									this.pushWindowNT.Close();
								}
								int num = this.BarsArray[0].Count - 2;
								int num2 = this.CurrentBars[0];
								this.lastAbsorptionBarIndex = (this.lastExhaustionBarIndex = -1);
								for (int i = 0; i <= num; i++)
								{
									this.FindSignal(i);
								}
								this.isClickEvent = false;
								this.ChartControl.InvalidateVisual();
								this.ChartControl.InvalidateVisual();
							});
						}
						dispatcher.InvokeAsync(action);
					}
				}
				catch (Exception ex)
				{
					this.PrintException(ex);
				}
			}, e);
		}
		private void OnBtnApply_Click(object sender, RoutedEventArgs e)
		{
			Action cachedAction1 = null;
			base.TriggerCustomEvent(delegate(object state)
			{
				try
				{
					if (this.isCharting)
					{
						Dispatcher dispatcher = this.ChartControl.Dispatcher;
						Action action;
						if ((action = cachedAction1) == null)
						{
							action = (cachedAction1 = delegate
							{
								this.isClickEvent = true;
								if (!this.isMarkerCustomRenderingMethod)
								{
									this.RemoveDrawObjects();
								}
								else
								{
									this.dictMarkers.Clear();
								}
								Button button = sender as Button;
								try
								{
									this.SetAllConditionParameters(button, false);
									this.UpdateConditionGroup();
									int num = this.BarsArray[0].Count - 2;
									int num2 = this.CurrentBars[0];
									this.lastAbsorptionBarIndex = (this.lastExhaustionBarIndex = -1);
									for (int i = 0; i <= num; i++)
									{
										this.FindSignal(i);
									}
								}
								catch (Exception ex2)
								{
									this.PrintException(ex2);
								}
								finally
								{
									this.isClickEvent = false;
									this.ChartControl.InvalidateVisual();
								}
							});
						}
						dispatcher.InvokeAsync(action);
					}
				}
				catch (Exception ex)
				{
					this.PrintException(ex);
				}
			}, e);
		}
		private void SetAllConditionParameters(Button buttonClick, bool isOk)
		{
			Button button = (isOk ? this.btnMainOK : this.btnMainApply);
			Button button2 = (isOk ? this.btnAbsorptionOK : this.btnAbsorptionApply);
			Button button3 = (isOk ? this.btnExhaustionOK : this.btnExhaustionApply);
			Button button4 = (isOk ? this.btnPushOK : this.btnPushApply);
			if (buttonClick == button && this.mainWindowNT != null)
			{
				this.SCAbsorptionEnabled = this.bodyAbBar.CheckBoxEnabled.IsChecked.Value;
				this.SCAbsorptionBullish = this.bodyAbBar.IsBuyEnabled;
				this.SCAbsorptionBearish = this.bodyAbBar.IsSellEnabled;
				this.SCExhaustionEnabled = this.bodyExBar.CheckBoxEnabled.IsChecked.Value;
				this.SCExhaustionBullish = this.bodyExBar.IsBuyEnabled;
				this.SCExhaustionBearish = this.bodyExBar.IsSellEnabled;
				this.SCPushEnabled = this.bodyPushBar.CheckBoxEnabled.IsChecked.Value;
				this.SCPushBullish = this.bodyPushBar.IsBuyEnabled;
				this.SCPushBearish = this.bodyPushBar.IsSellEnabled;
				this.SCAbPushEnabled = this.bodyAbPush.CheckBoxEnabled.IsChecked.Value;
				this.SCAbPushBullish = this.bodyAbPush.IsBuyEnabled;
				this.SCAbPushBearish = this.bodyAbPush.IsSellEnabled;
				this.SCExPushEnabled = this.bodyExPush.CheckBoxEnabled.IsChecked.Value;
				this.SCExPushBullish = this.bodyExPush.IsBuyEnabled;
				this.SCExPushBearish = this.bodyExPush.IsSellEnabled;
				return;
			}
			if (buttonClick == button2 && this.absorptionWindowNT != null)
			{
				this.SetConditionParameters(NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.SignalType.Absorption);
			}
			if (buttonClick == button3 && this.exhaustionWindowNT != null)
			{
				this.SetConditionParameters(NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.SignalType.Exhaustion);
			}
			if (buttonClick == button4 && this.pushWindowNT != null)
			{
				this.SetConditionParameters(NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.SignalType.Push);
			}
		}
		private void TerminateMainWindowNT(NTWindow nTWindow)
		{
			try
			{
				if (this.isCharting)
				{
					base.ChartControl.Dispatcher.InvokeAsync(delegate
					{
						if (nTWindow != null)
						{
							nTWindow.Close();
							nTWindow.Closing -= this.OnWindowNT_Closing;
							nTWindow.SizeChanged -= new SizeChangedEventHandler(this.OnWindowSizeChanged);
							nTWindow.LocationChanged -= this.OnWindowSizeChanged;
							nTWindow = null;
						}
					});
				}
			}
			catch (Exception ex)
			{
				this.PrintException(ex);
			}
		}
		private void BuildPushWindowNT()
		{
			if (this.isCharting)
			{
				base.ChartControl.Dispatcher.InvokeAsync(delegate
				{
					try
					{
						if (this.pushWindowNT != null && this.pushWindowNT.IsLoaded)
						{
							if (this.pushWindowNT.WindowState == global::System.Windows.WindowState.Minimized)
							{
								this.pushWindowNT.WindowState = global::System.Windows.WindowState.Normal;
							}
							this.pushWindowNT.Activate();
						}
						else
						{
							if (this.pushWindowNT != null)
							{
								this.pushWindowNT.Closing -= this.OnWindowNT_Closing;
								this.pushWindowNT.SizeChanged -= new SizeChangedEventHandler(this.OnWindowSizeChanged);
								this.pushWindowNT.LocationChanged -= this.OnWindowSizeChanged;
							}
							this.pushWindowNT = new NTWindow
							{
								Caption = "Push Condition",
								Padding = new Thickness(0.0),
								MinWidth = 250.0,
								MinHeight = 200.0
							};
							this.gridPushContent = new Grid
							{
								Margin = new Thickness(10.0)
							};
							this.gridPushContent.RowDefinitions.Add(new RowDefinition
							{
								Height = new GridLength(1.0, GridUnitType.Star)
							});
							this.gridPushContent.RowDefinitions.Add(new RowDefinition
							{
								Height = default(GridLength)
							});
							StackPanel stackPanel = new StackPanel
							{
								Orientation = Orientation.Vertical
							};
							Border border = this.CreateBorderHeader(true, this.GetSignalContent(NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.SignalType.Push, true), null, this.GetSignalContent(NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.SignalType.Push, false), null, HorizontalAlignment.Center, NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.HeaderNotes.notePushLeft, NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.HeaderNotes.notePushRight);
							stackPanel.Children.Add(border);
							this.txtPushN = new TextBox();
							StackPanel stackPanel2 = this.CreateInputPanel(NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.SignalType.Push, this.PushN);
							stackPanel.Children.Add(stackPanel2);
							this.CreateBodies(stackPanel, NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.SignalType.Push);
							Border border2 = new Border
							{
								BorderBrush = (this.mainWindowBorderColor ?? this.MainWindowTextColor),
								BorderThickness = new Thickness(1.0, 0.0, 1.0, 1.0)
							};
							border2.Child = stackPanel;
							border2.SetValue(Grid.RowProperty, 0);
							this.gridPushContent.Children.Add(border2);
							StackPanel stackPanel3 = new StackPanel
							{
								Orientation = Orientation.Horizontal,
								HorizontalAlignment = HorizontalAlignment.Right,
								Margin = new Thickness(0.0, 18.0, 0.0, 0.0)
							};
							stackPanel3.SetValue(Grid.RowProperty, 1);
							this.btnPushCancel = new Button
							{
								Content = "Cancel",
								Height = 30.0,
								MinWidth = 100.0,
								MinHeight = 30.0,
								ToolTip = string.Empty,
								Cursor = Cursors.Hand,
								Margin = new Thickness(10.0, 0.0, 0.0, 0.0)
							};
							this.btnPushOK = new Button
							{
								Content = "OK",
								Height = 30.0,
								MinWidth = 100.0,
								MinHeight = 30.0,
								ToolTip = string.Empty,
								IsDefault = true,
								Cursor = Cursors.Hand,
								Margin = new Thickness(10.0, 0.0, 0.0, 0.0)
							};
							this.btnPushApply = new Button
							{
								Content = "Apply",
								Height = 30.0,
								MinWidth = 100.0,
								MinHeight = 30.0,
								ToolTip = string.Empty,
								IsDefault = true,
								Cursor = Cursors.Hand,
								Margin = new Thickness(10.0, 0.0, 0.0, 0.0)
							};
							stackPanel3.Children.Add(this.btnPushOK);
							stackPanel3.Children.Add(this.btnPushCancel);
							stackPanel3.Children.Add(this.btnPushApply);
							this.btnPushApply.Click += this.OnBtnApply_Click;
							this.btnPushCancel.Click += this.OnBtnCancel_Click;
							this.btnPushOK.Click += this.OnBtnOK_Click;
							this.gridPushContent.Children.Add(stackPanel3);
							this.pushWindowNT.Content = this.gridPushContent;
							this.pushWindowNT.Closing += this.OnWindowNT_Closing;
							this.pushWindowNT.SizeChanged += new SizeChangedEventHandler(this.OnWindowSizeChanged);
							this.pushWindowNT.LocationChanged += this.OnWindowSizeChanged;
							this.pushWindowNT.Opacity = 0.0;
							this.pushWindowNT.Show();
							this.pushWindowNT.Width = ((this.PushWindowWidth < 0.0) ? 200.0 : Math.Max(200.0, this.PushWindowWidth));
							this.pushWindowNT.Height = ((this.PushWindowHeight < 0.0) ? 300.0 : Math.Max(300.0, this.PushWindowHeight));
							this.pushWindowNT.Top = this.PushWindowTop;
							this.pushWindowNT.Left = this.PushWindowLeft;
							this.pushWindowNT.Opacity = 1.0;
						}
					}
					catch (Exception ex)
					{
						this.PrintException(ex);
					}
				});
			}
		}
		private StackPanel CreateInputPanel(DDApexFlowZignal.SignalType signalType, int n)
		{
			TextBox textBox = ((signalType == NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.SignalType.Absorption) ? this.txtAbsorptionN : ((signalType == NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.SignalType.Exhaustion) ? this.txtExhaustionN : this.txtPushN));
			StackPanel stackPanel = new StackPanel();
			stackPanel.Orientation = Orientation.Horizontal;
			stackPanel.Margin = new Thickness(5.0, 5.0, 0.0, 5.0);
			stackPanel.HorizontalAlignment = HorizontalAlignment.Center;
			Label label = new Label
			{
				Content = "N =",
				FontSize = this.fontSizeCondition,
				VerticalAlignment = VerticalAlignment.Center,
				Foreground = this.MainWindowTextColor,
				Padding = new Thickness(0.0),
				Margin = new Thickness(0.0, 0.0, 4.0, 0.0)
			};
			textBox.Text = n.ToString();
			textBox.Width = 80.0;
			textBox.VerticalAlignment = VerticalAlignment.Center;
			textBox.HorizontalAlignment = HorizontalAlignment.Center;
			textBox.VerticalContentAlignment = VerticalAlignment.Center;
			textBox.TextAlignment = global::System.Windows.TextAlignment.Center;
			textBox.ToolTip = "Number of bars (N) used for " + signalType.ToString() + " conditions";
			textBox.Margin = new Thickness(0.0, 0.0, 0.0, 0.0);
			this.RegisterNumericValidation(textBox, 1, int.MaxValue);
			stackPanel.Children.Add(label);
			stackPanel.Children.Add(textBox);
			return stackPanel;
		}
		private void BuildExhaustionWindowNT()
		{
			if (this.isCharting)
			{
				base.ChartControl.Dispatcher.InvokeAsync(delegate
				{
					try
					{
						if (this.exhaustionWindowNT != null && this.exhaustionWindowNT.IsLoaded)
						{
							if (this.exhaustionWindowNT.WindowState == global::System.Windows.WindowState.Minimized)
							{
								this.exhaustionWindowNT.WindowState = global::System.Windows.WindowState.Normal;
							}
							this.exhaustionWindowNT.Activate();
						}
						else
						{
							if (this.exhaustionWindowNT != null)
							{
								this.exhaustionWindowNT.Closing -= this.OnWindowNT_Closing;
								this.exhaustionWindowNT.SizeChanged -= new SizeChangedEventHandler(this.OnWindowSizeChanged);
								this.exhaustionWindowNT.LocationChanged -= this.OnWindowSizeChanged;
							}
							this.exhaustionWindowNT = new NTWindow
							{
								Caption = "Exhaustion Condition",
								Padding = new Thickness(0.0),
								MinWidth = 250.0,
								MinHeight = 200.0
							};
							this.gridExhaustionContent = new Grid
							{
								Margin = new Thickness(10.0)
							};
							this.gridExhaustionContent.RowDefinitions.Add(new RowDefinition
							{
								Height = new GridLength(1.0, GridUnitType.Star)
							});
							this.gridExhaustionContent.RowDefinitions.Add(new RowDefinition
							{
								Height = default(GridLength)
							});
							StackPanel stackPanel = new StackPanel
							{
								Orientation = Orientation.Vertical
							};
							Border border = this.CreateBorderHeader(true, this.GetSignalContent(NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.SignalType.Exhaustion, true), null, this.GetSignalContent(NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.SignalType.Exhaustion, false), null, HorizontalAlignment.Center, NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.HeaderNotes.noteExhaustionLeft, NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.HeaderNotes.noteExhaustionRight);
							stackPanel.Children.Add(border);
							this.txtExhaustionN = new TextBox();
							StackPanel stackPanel2 = this.CreateInputPanel(NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.SignalType.Exhaustion, this.ExhaustionN);
							stackPanel.Children.Add(stackPanel2);
							this.CreateBodies(stackPanel, NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.SignalType.Exhaustion);
							Border border2 = new Border
							{
								BorderBrush = (this.mainWindowBorderColor ?? this.MainWindowTextColor),
								BorderThickness = new Thickness(1.0, 0.0, 1.0, 1.0)
							};
							border2.Child = stackPanel;
							border2.SetValue(Grid.RowProperty, 0);
							this.gridExhaustionContent.Children.Add(border2);
							StackPanel stackPanel3 = new StackPanel
							{
								Orientation = Orientation.Horizontal,
								HorizontalAlignment = HorizontalAlignment.Right,
								Margin = new Thickness(0.0, 18.0, 0.0, 0.0)
							};
							stackPanel3.SetValue(Grid.RowProperty, 1);
							this.btnExhaustionCancel = new Button
							{
								Content = "Cancel",
								Height = 30.0,
								MinWidth = 100.0,
								MinHeight = 30.0,
								ToolTip = string.Empty,
								Cursor = Cursors.Hand,
								Margin = new Thickness(10.0, 0.0, 0.0, 0.0)
							};
							this.btnExhaustionOK = new Button
							{
								Content = "OK",
								Height = 30.0,
								MinWidth = 100.0,
								MinHeight = 30.0,
								ToolTip = string.Empty,
								IsDefault = true,
								Cursor = Cursors.Hand,
								Margin = new Thickness(10.0, 0.0, 0.0, 0.0)
							};
							this.btnExhaustionApply = new Button
							{
								Content = "Apply",
								Height = 30.0,
								MinWidth = 100.0,
								MinHeight = 30.0,
								ToolTip = string.Empty,
								IsDefault = true,
								Cursor = Cursors.Hand,
								Margin = new Thickness(10.0, 0.0, 0.0, 0.0)
							};
							stackPanel3.Children.Add(this.btnExhaustionOK);
							stackPanel3.Children.Add(this.btnExhaustionCancel);
							stackPanel3.Children.Add(this.btnExhaustionApply);
							this.btnExhaustionApply.Click += this.OnBtnApply_Click;
							this.btnExhaustionCancel.Click += this.OnBtnCancel_Click;
							this.btnExhaustionOK.Click += this.OnBtnOK_Click;
							this.gridExhaustionContent.Children.Add(stackPanel3);
							this.exhaustionWindowNT.Content = this.gridExhaustionContent;
							this.exhaustionWindowNT.Closing += this.OnWindowNT_Closing;
							this.exhaustionWindowNT.SizeChanged += new SizeChangedEventHandler(this.OnWindowSizeChanged);
							this.exhaustionWindowNT.LocationChanged += this.OnWindowSizeChanged;
							this.exhaustionWindowNT.Opacity = 0.0;
							this.exhaustionWindowNT.Show();
							this.exhaustionWindowNT.Width = ((this.ExhaustionWindowWidth < 0.0) ? 200.0 : Math.Max(200.0, this.ExhaustionWindowWidth));
							this.exhaustionWindowNT.Height = ((this.ExhaustionWindowHeight < 0.0) ? 300.0 : Math.Max(300.0, this.ExhaustionWindowHeight));
							this.exhaustionWindowNT.Top = this.ExhaustionWindowTop;
							this.exhaustionWindowNT.Left = this.ExhaustionWindowLeft;
							this.exhaustionWindowNT.Opacity = 1.0;
						}
					}
					catch (Exception ex)
					{
						this.PrintException(ex);
					}
				});
			}
		}
		private void BuildAbsorptionWindowNT()
		{
			if (this.isCharting)
			{
				base.ChartControl.Dispatcher.InvokeAsync(delegate
				{
					try
					{
						if (this.absorptionWindowNT != null && this.absorptionWindowNT.IsLoaded)
						{
							if (this.absorptionWindowNT.WindowState == global::System.Windows.WindowState.Minimized)
							{
								this.absorptionWindowNT.WindowState = global::System.Windows.WindowState.Normal;
							}
							this.absorptionWindowNT.Activate();
						}
						else
						{
							if (this.absorptionWindowNT != null)
							{
								this.absorptionWindowNT.Closing -= this.OnWindowNT_Closing;
								this.absorptionWindowNT.SizeChanged -= new SizeChangedEventHandler(this.OnWindowSizeChanged);
								this.absorptionWindowNT.LocationChanged -= this.OnWindowSizeChanged;
							}
							this.absorptionWindowNT = new NTWindow
							{
								Caption = "Arsorption Condition",
								Padding = new Thickness(0.0),
								MinWidth = 250.0,
								MinHeight = 200.0
							};
							this.gridAbsorptionContent = new Grid
							{
								Margin = new Thickness(10.0)
							};
							this.gridAbsorptionContent.RowDefinitions.Add(new RowDefinition
							{
								Height = new GridLength(1.0, GridUnitType.Star)
							});
							this.gridAbsorptionContent.RowDefinitions.Add(new RowDefinition
							{
								Height = default(GridLength)
							});
							StackPanel stackPanel = new StackPanel
							{
								Orientation = Orientation.Vertical
							};
							Border border = this.CreateBorderHeader(true, this.GetSignalContent(NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.SignalType.Absorption, true), null, this.GetSignalContent(NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.SignalType.Absorption, false), null, HorizontalAlignment.Center, NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.HeaderNotes.noteAbsorptionLeft, NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.HeaderNotes.noteAbsorptionRight);
							stackPanel.Children.Add(border);
							this.txtAbsorptionN = new TextBox();
							StackPanel stackPanel2 = this.CreateInputPanel(NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.SignalType.Absorption, this.AbsorptionN);
							stackPanel.Children.Add(stackPanel2);
							this.CreateBodies(stackPanel, NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.SignalType.Absorption);
							Border border2 = new Border
							{
								BorderBrush = (this.mainWindowBorderColor ?? this.MainWindowTextColor),
								BorderThickness = new Thickness(1.0, 0.0, 1.0, 1.0)
							};
							border2.Child = stackPanel;
							border2.SetValue(Grid.RowProperty, 0);
							this.gridAbsorptionContent.Children.Add(border2);
							StackPanel stackPanel3 = new StackPanel
							{
								Orientation = Orientation.Horizontal,
								HorizontalAlignment = HorizontalAlignment.Right,
								Margin = new Thickness(0.0, 18.0, 0.0, 0.0)
							};
							stackPanel3.SetValue(Grid.RowProperty, 1);
							this.btnAbsorptionCancel = new Button
							{
								Content = "Cancel",
								Height = 30.0,
								MinWidth = 100.0,
								MinHeight = 30.0,
								ToolTip = string.Empty,
								Cursor = Cursors.Hand,
								Margin = new Thickness(10.0, 0.0, 0.0, 0.0)
							};
							this.btnAbsorptionOK = new Button
							{
								Content = "OK",
								Height = 30.0,
								MinWidth = 100.0,
								MinHeight = 30.0,
								ToolTip = string.Empty,
								IsDefault = true,
								Cursor = Cursors.Hand,
								Margin = new Thickness(10.0, 0.0, 0.0, 0.0)
							};
							this.btnAbsorptionApply = new Button
							{
								Content = "Apply",
								Height = 30.0,
								MinWidth = 100.0,
								MinHeight = 30.0,
								ToolTip = string.Empty,
								IsDefault = true,
								Cursor = Cursors.Hand,
								Margin = new Thickness(10.0, 0.0, 0.0, 0.0)
							};
							stackPanel3.Children.Add(this.btnAbsorptionOK);
							stackPanel3.Children.Add(this.btnAbsorptionCancel);
							stackPanel3.Children.Add(this.btnAbsorptionApply);
							this.btnAbsorptionApply.Click += this.OnBtnApply_Click;
							this.btnAbsorptionCancel.Click += this.OnBtnCancel_Click;
							this.btnAbsorptionOK.Click += this.OnBtnOK_Click;
							this.gridAbsorptionContent.Children.Add(stackPanel3);
							this.absorptionWindowNT.Content = this.gridAbsorptionContent;
							this.absorptionWindowNT.Closing += this.OnWindowNT_Closing;
							this.absorptionWindowNT.SizeChanged += new SizeChangedEventHandler(this.OnWindowSizeChanged);
							this.absorptionWindowNT.LocationChanged += this.OnWindowSizeChanged;
							this.absorptionWindowNT.Opacity = 0.0;
							this.absorptionWindowNT.Show();
							this.absorptionWindowNT.Width = ((this.AbsorptionWindowWidth < 0.0) ? 200.0 : Math.Max(200.0, this.AbsorptionWindowWidth));
							this.absorptionWindowNT.Height = ((this.AbsorptionWindowHeight < 0.0) ? 300.0 : Math.Max(300.0, this.AbsorptionWindowHeight));
							this.absorptionWindowNT.Top = this.AbsorptionWindowTop;
							this.absorptionWindowNT.Left = this.AbsorptionWindowLeft;
							this.absorptionWindowNT.Opacity = 1.0;
						}
					}
					catch (Exception ex)
					{
						this.PrintException(ex);
					}
				});
			}
		}
		private void AddSeperator(StackPanel stackPanel)
		{
			Border border = new Border
			{
				BorderBrush = Brushes.LightSkyBlue,
				BorderThickness = new Thickness(0.0, 1.0, 0.0, 0.0),
				Margin = new Thickness(0.0, 0.0, 0.0, 5.0)
			};
			stackPanel.Children.Add(border);
		}
		private void BuildMainWindowNT()
		{
			if (this.isCharting)
			{
				base.ChartControl.Dispatcher.InvokeAsync(delegate
				{
					try
					{
						if (this.mainWindowNT != null && this.mainWindowNT.IsLoaded)
						{
							if (this.mainWindowNT.WindowState == global::System.Windows.WindowState.Minimized)
							{
								this.mainWindowNT.WindowState = global::System.Windows.WindowState.Normal;
							}
							this.mainWindowNT.Activate();
						}
						else
						{
							if (this.mainWindowNT != null)
							{
								this.mainWindowNT.Closing -= this.OnWindowNT_Closing;
								this.mainWindowNT.SizeChanged -= new SizeChangedEventHandler(this.OnWindowSizeChanged);
								this.mainWindowNT.LocationChanged -= this.OnWindowSizeChanged;
							}
							this.mainWindowNT = new NTWindow
							{
								Caption = "ApexFlow Zignal",
								Padding = new Thickness(0.0),
								MinWidth = 250.0,
								MinHeight = 200.0
							};
							this.gridMainContent = new Grid
							{
								Margin = new Thickness(10.0)
							};
							this.gridMainContent.RowDefinitions.Add(new RowDefinition
							{
								Height = new GridLength(1.0, GridUnitType.Star)
							});
							this.gridMainContent.RowDefinitions.Add(new RowDefinition
							{
								Height = default(GridLength)
							});
							StackPanel stackPanel = new StackPanel
							{
								Orientation = Orientation.Vertical,
								Margin = new Thickness(5.0)
							};
							Grid grid = this.CreateBorderHeader("", "Bullish Signal Bar", "Bearish Signal Bar", this.minSideBarWidth);
							stackPanel.Children.Add(grid);
							Grid grid2 = new Grid();
							grid2.ColumnDefinitions.Add(new ColumnDefinition
							{
								Width = new GridLength(1.0, GridUnitType.Auto)
							});
							grid2.ColumnDefinitions.Add(new ColumnDefinition
							{
								Width = new GridLength(1.0, GridUnitType.Star)
							});
							Border border = new Border
							{
								Width = (double)this.stackTypeSideBarWidth,
								Background = Brushes.LightSkyBlue,
								HorizontalAlignment = HorizontalAlignment.Left,
								Margin = new Thickness(0.0, 0.0, 0.0, 0.0),
								Child = new TextBlock
								{
									Text = "Type",
									Foreground = Brushes.Blue,
									FontSize = 18.0,
									HorizontalAlignment = HorizontalAlignment.Center,
									VerticalAlignment = VerticalAlignment.Center,
									LayoutTransform = new RotateTransform(-90.0)
								}
							};
							border.SetValue(Grid.ColumnProperty, 0);
							grid2.Children.Add(border);
							this.bodyAbBar = this.CreateGridCheckBox("Absorption Bar", this.SCAbsorptionEnabled, this.SCAbsorptionBullish, this.SCAbsorptionBearish, this.minSideBarWidth - this.stackTypeSideBarWidth - 20f, true, NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.SignalType.Absorption);
							this.bodyExBar = this.CreateGridCheckBox("Exhaustion Bar", this.SCExhaustionEnabled, this.SCExhaustionBullish, this.SCExhaustionBearish, this.minSideBarWidth - this.stackTypeSideBarWidth - 20f, true, NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.SignalType.Exhaustion);
							this.bodyPushBar = this.CreateGridCheckBox("Push Bar", this.SCPushEnabled, this.SCPushBullish, this.SCPushBearish, this.minSideBarWidth - this.stackTypeSideBarWidth - 20f, true, NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.SignalType.Push);
							StackPanel stackPanel2 = new StackPanel
							{
								Orientation = Orientation.Vertical
							};
							stackPanel2.Children.Add(this.bodyAbBar.Grid);
							stackPanel2.Children.Add(this.bodyExBar.Grid);
							stackPanel2.Children.Add(this.bodyPushBar.Grid);
							stackPanel2.SetValue(Grid.ColumnProperty, 1);
							grid2.Children.Add(stackPanel2);
							this.AddSeperator(stackPanel2);
							stackPanel.Children.Add(grid2);
							Grid grid3 = new Grid
							{
								Margin = new Thickness(0.0, 0.0, 0.0, 5.0)
							};
							grid3.ColumnDefinitions.Add(new ColumnDefinition
							{
								Width = new GridLength(1.0, GridUnitType.Auto)
							});
							grid3.ColumnDefinitions.Add(new ColumnDefinition
							{
								Width = new GridLength(1.0, GridUnitType.Star)
							});
							Border border2 = new Border
							{
								Width = (double)this.stackTypeSideBarWidth,
								Background = Brushes.LightSkyBlue,
								HorizontalAlignment = HorizontalAlignment.Left,
								Child = new TextBlock
								{
									Text = "Concept",
									FontSize = 18.0,
									Foreground = Brushes.Blue,
									HorizontalAlignment = HorizontalAlignment.Center,
									VerticalAlignment = VerticalAlignment.Center,
									LayoutTransform = new RotateTransform(-90.0)
								}
							};
							border2.SetValue(Grid.ColumnProperty, 0);
							grid3.Children.Add(border2);
							this.bodyAbPush = this.CreateGridCheckBox("Ab + Push Bar", this.SCAbPushEnabled, this.SCAbPushBullish, this.SCAbPushBearish, this.minSideBarWidth - this.stackTypeSideBarWidth - 20f, true, NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.SignalType.AbPush);
							this.bodyExPush = this.CreateGridCheckBox("Ex + Push Bar", this.SCExPushEnabled, this.SCExPushBullish, this.SCExPushBearish, this.minSideBarWidth - this.stackTypeSideBarWidth - 20f, true, NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.SignalType.ExPush);
							StackPanel stackPanel3 = new StackPanel
							{
								Orientation = Orientation.Vertical
							};
							stackPanel3.Children.Add(this.bodyAbPush.Grid);
							stackPanel3.Children.Add(this.bodyExPush.Grid);
							stackPanel3.SetValue(Grid.ColumnProperty, 1);
							grid3.Children.Add(stackPanel3);
							stackPanel.Children.Add(grid3);
							Border border3 = new Border
							{
								BorderBrush = (this.mainWindowBorderColor ?? this.MainWindowTextColor),
								BorderThickness = new Thickness(1.0)
							};
							border3.Child = stackPanel;
							border3.SetValue(Grid.RowProperty, 0);
							this.gridMainContent.Children.Add(border3);
							StackPanel stackPanel4 = new StackPanel
							{
								Orientation = Orientation.Horizontal,
								HorizontalAlignment = HorizontalAlignment.Right,
								Margin = new Thickness(0.0, 18.0, 0.0, 0.0)
							};
							stackPanel4.SetValue(Grid.RowProperty, 1);
							this.btnMainCancel = new Button
							{
								Content = "Cancel",
								Height = 30.0,
								MinWidth = 100.0,
								MinHeight = 30.0,
								ToolTip = string.Empty,
								Cursor = Cursors.Hand,
								Margin = new Thickness(10.0, 0.0, 0.0, 0.0)
							};
							this.btnMainOK = new Button
							{
								Content = "OK",
								Height = 30.0,
								MinWidth = 100.0,
								MinHeight = 30.0,
								ToolTip = string.Empty,
								IsDefault = true,
								Cursor = Cursors.Hand,
								Margin = new Thickness(10.0, 0.0, 0.0, 0.0)
							};
							this.btnMainApply = new Button
							{
								Content = "Apply",
								Height = 30.0,
								MinWidth = 100.0,
								MinHeight = 30.0,
								ToolTip = string.Empty,
								IsDefault = true,
								Cursor = Cursors.Hand,
								Margin = new Thickness(10.0, 0.0, 0.0, 0.0)
							};
							stackPanel4.Children.Add(this.btnMainOK);
							stackPanel4.Children.Add(this.btnMainCancel);
							stackPanel4.Children.Add(this.btnMainApply);
							this.btnMainApply.Click += this.OnBtnApply_Click;
							this.btnMainCancel.Click += this.OnBtnCancel_Click;
							this.btnMainOK.Click += this.OnBtnOK_Click;
							this.gridMainContent.Children.Add(stackPanel4);
							this.mainWindowNT.Content = this.gridMainContent;
							this.mainWindowNT.Closing += this.OnWindowNT_Closing;
							this.mainWindowNT.SizeChanged += new SizeChangedEventHandler(this.OnWindowSizeChanged);
							this.mainWindowNT.LocationChanged += this.OnWindowSizeChanged;
							this.mainWindowNT.Opacity = 0.0;
							this.mainWindowNT.Show();
							this.mainWindowNT.Width = ((this.MainWindowWidth < 0.0) ? 250.0 : Math.Max(250.0, this.MainWindowWidth));
							this.mainWindowNT.Height = ((this.MainWindowHeight < 0.0) ? 200.0 : Math.Max(200.0, this.MainWindowHeight));
							this.mainWindowNT.Top = this.MainWindowTop;
							this.mainWindowNT.Left = this.MainWindowLeft;
							this.mainWindowNT.Opacity = 1.0;
						}
					}
					catch (Exception ex)
					{
						this.PrintException(ex);
					}
				});
			}
		}
		private ValueTuple<object, int, int> GetPropertyInfo(string propName)
		{
			PropertyInfo property = base.GetType().GetProperty(propName);
			if (property != null)
			{
				RangeAttribute customAttribute = property.GetCustomAttribute<RangeAttribute>();
				if (customAttribute != null)
				{
					return new ValueTuple<object, int, int>(property.GetValue(this), Convert.ToInt32(customAttribute.Minimum), Convert.ToInt32(customAttribute.Maximum));
				}
			}
			return new ValueTuple<object, int, int>(null, 0, int.MaxValue);
		}
		private void SetPropertyValue(string propName, object value)
		{
			PropertyInfo property = base.GetType().GetProperty(propName);
			if (property != null && property.CanWrite)
			{
				object obj = Convert.ChangeType(value, property.PropertyType);
				property.SetValue(this, obj);
			}
		}
		private DDApexFlowZignal.SignalConditionBody CreateGridCheckBoxCondition(string textBuy, bool isBuyEnabled, string textSell, bool isSellEnabled)
		{
			Grid grid = new Grid();
			grid.ColumnDefinitions.Add(new ColumnDefinition
			{
				Width = new GridLength(1.0, GridUnitType.Star)
			});
			grid.ColumnDefinitions.Add(new ColumnDefinition
			{
				Width = new GridLength(1.0, GridUnitType.Star)
			});
			CheckBox checkBox = this.CreateFlexibleCheckBox(textBuy, isBuyEnabled);
			checkBox.SetValue(Grid.ColumnProperty, 0);
			CheckBox checkBox2 = this.CreateFlexibleCheckBox(textSell, isSellEnabled);
			checkBox2.SetValue(Grid.ColumnProperty, 1);
			grid.Children.Add(checkBox);
			grid.Children.Add(checkBox2);
			return new DDApexFlowZignal.SignalConditionBody(grid, isBuyEnabled, textBuy, checkBox, isSellEnabled, textSell, checkBox2);
		}
		private CheckBox CreateFlexibleCheckBox(string rawText, bool isEnabled)
		{
			CheckBox checkBox = new CheckBox
			{
				IsChecked = new bool?(isEnabled),
				FontSize = this.fontSizeCondition,
				VerticalAlignment = VerticalAlignment.Center,
				Margin = new Thickness(10.0, 5.0, 0.0, 5.0)
			};
			Match match = Regex.Match(rawText, "\\[(.*?)\\]");
			if (match.Success)
			{
				string propName = match.Groups[1].Value;
				if (!this.tempParameterValueBuffer.ContainsKey(propName))
				{
					this.tempParameterValueBuffer.Add(propName, "");
				}
				string[] array = rawText.Split(new string[] { match.Value }, StringSplitOptions.None);
				WrapPanel wrapPanel = new WrapPanel
				{
					VerticalAlignment = VerticalAlignment.Center
				};
				wrapPanel.Children.Add(new TextBlock
				{
					Text = array[0],
					VerticalAlignment = VerticalAlignment.Center
				});
				ValueTuple<object, int, int> propertyInfo = this.GetPropertyInfo(propName);
				TextBox input = new TextBox();
				object item = propertyInfo.Item1;
				input.Text = ((item != null) ? item.ToString() : null) ?? "";
				input.Width = 40.0;
				input.Margin = new Thickness(5.0, 0.0, 5.0, 0.0);
				input.VerticalContentAlignment = VerticalAlignment.Center;
				input.HorizontalContentAlignment = HorizontalAlignment.Center;
				this.RegisterNumericValidation(input, propertyInfo.Item2, propertyInfo.Item3);
				input.TextChanged += delegate(object s, TextChangedEventArgs e)
				{
					this.tempParameterValueBuffer[propName] = input.Text;
				};
				wrapPanel.Children.Add(input);
				if (array.Length > 1)
				{
					wrapPanel.Children.Add(new TextBlock
					{
						Text = array[1],
						VerticalAlignment = VerticalAlignment.Center
					});
				}
				checkBox.Content = wrapPanel;
			}
			else
			{
				checkBox.Content = rawText;
			}
			return checkBox;
		}
		private DDApexFlowZignal.Body CreateGridCheckBox(string text, bool isEnabled, bool isBuyEnabled, bool isSellEnabled, float minWidth, bool hasSet, DDApexFlowZignal.SignalType signalType)
		{
			Grid grid = new Grid();
			grid.ColumnDefinitions.Add(new ColumnDefinition
			{
				Width = default(GridLength)
			});
			grid.ColumnDefinitions.Add(new ColumnDefinition
			{
				Width = new GridLength(1.0, GridUnitType.Star)
			});
			grid.ColumnDefinitions.Add(new ColumnDefinition
			{
				Width = new GridLength(1.0, GridUnitType.Star)
			});
			if (hasSet)
			{
				grid.ColumnDefinitions.Add(new ColumnDefinition
				{
					Width = default(GridLength)
				});
			}
			CheckBox checkBoxMain = new CheckBox
			{
				Content = text,
				IsChecked = new bool?(isEnabled),
				Height = 40.0,
				MinWidth = 100.0,
				MinHeight = 40.0,
				Width = (double)minWidth,
				VerticalAlignment = VerticalAlignment.Center,
				HorizontalAlignment = HorizontalAlignment.Left,
				Margin = new Thickness(10.0, 0.0, 10.0, 0.0)
			};
			checkBoxMain.SetValue(Grid.ColumnProperty, 0);
			grid.Children.Add(checkBoxMain);
			DDApexFlowZignal.Body body = new DDApexFlowZignal.Body(grid, checkBoxMain, isBuyEnabled, null, isSellEnabled, null);
			bool flag = signalType == NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.SignalType.Absorption || signalType == NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.SignalType.Exhaustion || signalType == NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.SignalType.Push;
			global::System.Windows.Media.Brush buttonBuyEnableColor = (flag ? Brushes.DodgerBlue : Brushes.LimeGreen);
			global::System.Windows.Media.Brush buttonSellEnableColor = (flag ? Brushes.Orchid : Brushes.HotPink);
			Button toggleBuy = new Button
			{
				Content = this.GetSignalContent(signalType, true),
				VerticalAlignment = VerticalAlignment.Center,
				HorizontalAlignment = HorizontalAlignment.Stretch,
				Height = 40.0,
				MinWidth = 100.0,
				MinHeight = 40.0,
				Background = (body.IsBuyEnabled ? buttonBuyEnableColor : Brushes.Gray),
				Foreground = Brushes.White,
				Cursor = Cursors.Hand,
				Margin = new Thickness(0.0, 0.0, 5.0, 5.0),
				ToolTip = string.Empty
			};
			toggleBuy.Click += delegate(object sender, RoutedEventArgs e)
			{
				body.IsBuyEnabled = !body.IsBuyEnabled;
				toggleBuy.Background = (body.IsBuyEnabled ? buttonBuyEnableColor : Brushes.Gray);
				toggleBuy.Foreground = (body.IsBuyEnabled ? Brushes.White : Brushes.Silver);
				if (!checkBoxMain.IsChecked.Value && body.IsBuyEnabled)
				{
					checkBoxMain.IsChecked = new bool?(true);
				}
			};
			toggleBuy.SetValue(Grid.ColumnProperty, 1);
			Button toggleSell = new Button
			{
				Content = this.GetSignalContent(signalType, false),
				VerticalAlignment = VerticalAlignment.Center,
				HorizontalAlignment = HorizontalAlignment.Stretch,
				MinWidth = 100.0,
				MinHeight = 40.0,
				Background = (body.IsSellEnabled ? buttonSellEnableColor : Brushes.Gray),
				Foreground = Brushes.White,
				Cursor = Cursors.Hand,
				Margin = new Thickness(0.0, 0.0, 0.0, 5.0),
				ToolTip = string.Empty
			};
			toggleSell.Click += delegate(object sender, RoutedEventArgs e)
			{
				body.IsSellEnabled = !body.IsSellEnabled;
				toggleSell.Background = (body.IsSellEnabled ? buttonSellEnableColor : Brushes.Gray);
				toggleSell.Foreground = (body.IsSellEnabled ? Brushes.White : Brushes.Silver);
				if (!checkBoxMain.IsChecked.Value && body.IsSellEnabled)
				{
					checkBoxMain.IsChecked = new bool?(true);
				}
			};
			toggleSell.SetValue(Grid.ColumnProperty, 2);
			grid.Children.Add(toggleBuy);
			grid.Children.Add(toggleSell);
			if (hasSet)
			{
				Button button = this.CreateButtonSetting(signalType);
				button.SetValue(Grid.ColumnProperty, 3);
				grid.Children.Add(button);
			}
			body.ToggleBuy = toggleBuy;
			body.ToggleSell = toggleSell;
			return body;
		}
		private string GetSignalContent(DDApexFlowZignal.SignalType signalType, bool isBuy)
		{
			bool flag = signalType == NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.SignalType.Absorption || signalType == NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.SignalType.Exhaustion;
			if (signalType == NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.SignalType.Absorption || signalType == NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.SignalType.Exhaustion || signalType == NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.SignalType.Push)
			{
				isBuy = (flag ? (!isBuy) : isBuy);
				string text = (isBuy ? "Buy " : "Sell ");
				string text2 = signalType.ToString() + " Bar ";
				string text3 = "[" + this.GetMarkerString(isBuy, signalType) + "]";
				return text + text2 + text3;
			}
			string iconOnly = this.GetIconOnly(isBuy ? this.MarkerPushStringBullish : this.MarkerPushStringBearish);
			if (signalType == NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.SignalType.AbPush)
			{
				string iconOnly2 = this.GetIconOnly(isBuy ? this.MarkerAbPushStringBullish : this.MarkerAbPushStringBearish);
				string iconOnly3 = this.GetIconOnly(isBuy ? this.MarkerAbsorptionStringBullish : this.MarkerAbsorptionStringBearish);
				return string.Concat(new string[] { iconOnly2, " = ", iconOnly3, " + ", iconOnly });
			}
			string iconOnly4 = this.GetIconOnly(isBuy ? this.MarkerExPushStringBullish : this.MarkerExPushStringBearish);
			string iconOnly5 = this.GetIconOnly(isBuy ? this.MarkerExhaustionStringBullish : this.MarkerExhaustionStringBearish);
			return string.Concat(new string[] { iconOnly4, " = ", iconOnly5, " + ", iconOnly });
		}
		private Button CreateButtonSetting(DDApexFlowZignal.SignalType signalType)
		{
			Path path = new Path
			{
				Data = global::System.Windows.Media.Geometry.Parse("M19,19H5V5H12V3H5C3.89,3 3,3.89 3,5V19C3,20.1 3.89,21 5,21H19C20.1,21 21,20.1 21,19V12H19V19M14,3V5H17.59L7.76,14.83L9.17,16.24L19,6.41V10H21V3H14Z"),
				Fill = Brushes.White,
				Stretch = global::System.Windows.Media.Stretch.Uniform,
				SnapsToDevicePixels = true
			};
			Viewbox viewbox = new Viewbox
			{
				Child = path,
				Width = 14.0,
				Height = 14.0
			};
			Button button = new Button
			{
				Content = viewbox,
				Height = 40.0,
				MinHeight = 40.0,
				MinWidth = 32.0,
				Foreground = Brushes.White,
				BorderThickness = new Thickness(1.0),
				VerticalAlignment = VerticalAlignment.Center,
				HorizontalAlignment = HorizontalAlignment.Left,
				Margin = new Thickness(5.0, 0.0, 0.0, 5.0),
				Cursor = Cursors.Hand,
				Focusable = false
			};
			if (signalType == NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.SignalType.Absorption)
			{
				button.ToolTip = "Set Absorption Conditions";
				button.Click += this.OnBtnArsorptionSetting_Click;
				this.btnArsorptionSetting = button;
			}
			else if (signalType == NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.SignalType.Exhaustion)
			{
				button.ToolTip = "Set Exhaustion Conditions";
				button.Click += this.OnBtnExhaustionSetting_Click;
				this.btnExhaustionSetting = button;
			}
			else if (signalType == NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.SignalType.Push)
			{
				button.ToolTip = "Set Push Conditions";
				button.Click += this.OnBtnPushSetting_Click;
				this.btnPushSetting = button;
			}
			else
			{
				button.ToolTip = "Not Available";
				button.Background = Brushes.Transparent;
				button.BorderBrush = Brushes.DimGray;
				button.Opacity = 0.4;
				button.IsEnabled = false;
			}
			return button;
		}
		private Grid CreateBorderHeader(string text, string textLeft, string textRight, float width)
		{
			Grid grid = new Grid();
			grid.Margin = new Thickness(0.0, 0.0, 1.0, 5.0);
			grid.ColumnDefinitions.Add(new ColumnDefinition
			{
				Width = default(GridLength)
			});
			grid.ColumnDefinitions.Add(new ColumnDefinition
			{
				Width = new GridLength(100.0, GridUnitType.Star)
			});
			grid.ColumnDefinitions.Add(new ColumnDefinition
			{
				Width = new GridLength(100.0, GridUnitType.Star)
			});
			grid.ColumnDefinitions.Add(new ColumnDefinition
			{
				Width = new GridLength(40.0)
			});
			Label label = new Label
			{
				Content = text,
				FontSize = this.fontSizeHeader,
				Foreground = this.MainWindowTextColor,
				VerticalAlignment = VerticalAlignment.Center,
				HorizontalAlignment = HorizontalAlignment.Center,
				Width = (double)width
			};
			label.SetValue(Grid.ColumnProperty, 0);
			grid.Children.Add(label);
			Label label2 = new Label
			{
				Content = textLeft,
				FontSize = this.fontSizeHeader,
				Foreground = this.MainWindowTextColor,
				VerticalAlignment = VerticalAlignment.Center,
				HorizontalAlignment = HorizontalAlignment.Stretch,
				HorizontalContentAlignment = HorizontalAlignment.Center
			};
			label2.SetValue(Grid.ColumnProperty, 1);
			grid.Children.Add(label2);
			Label label3 = new Label
			{
				Content = textRight,
				FontSize = this.fontSizeHeader,
				Foreground = this.MainWindowTextColor,
				VerticalAlignment = VerticalAlignment.Center,
				HorizontalAlignment = HorizontalAlignment.Stretch,
				HorizontalContentAlignment = HorizontalAlignment.Center
			};
			label3.SetValue(Grid.ColumnProperty, 2);
			grid.Children.Add(label3);
			return grid;
		}
		private Border CreateBorderHeader(bool isMain, string textLeft, global::System.Windows.Media.Brush brushLeft, string textRight, global::System.Windows.Media.Brush brushRight, HorizontalAlignment horizontalContentAlignment, string noteHeaderLeft = "", string noteHeaderRight = "")
		{
			global::System.Windows.Media.Brush brush = new global::System.Windows.Media.SolidColorBrush((global::System.Windows.Media.Color)ColorConverter.ConvertFromString("#252526"));
			Grid grid = new Grid
			{
				Margin = new Thickness(5.0, 0.0, 0.0, 0.0)
			};
			if (isMain)
			{
				grid.Background = (this.isLightTheme ? Brushes.DarkGray : brush);
			}
			grid.ColumnDefinitions.Add(new ColumnDefinition
			{
				Width = new GridLength(1.0, GridUnitType.Star)
			});
			grid.ColumnDefinitions.Add(new ColumnDefinition
			{
				Width = new GridLength(1.0, GridUnitType.Star)
			});
			double num = (isMain ? this.fontSizeHeader : this.fontSizeSubHeader);
			StackPanel stackPanel = new StackPanel
			{
				VerticalAlignment = VerticalAlignment.Center
			};
			stackPanel.Children.Add(new TextBlock
			{
				Text = textLeft,
				TextAlignment = global::System.Windows.TextAlignment.Left,
				TextWrapping = TextWrapping.Wrap,
				FontWeight = (isMain ? FontWeights.Bold : FontWeights.Normal),
				Margin = new Thickness(0.0, 0.0, 0.0, (double)(isMain ? 3 : 0))
			});
			if (!noteHeaderLeft.IsNullOrEmpty())
			{
				stackPanel.Children.Add(new TextBlock
				{
					Text = noteHeaderLeft,
					FontStyle = FontStyles.Italic,
					FontSize = num - 2.0,
					Opacity = 0.8,
					TextWrapping = TextWrapping.Wrap,
					TextAlignment = global::System.Windows.TextAlignment.Left
				});
			}
			Label label = new Label
			{
				Content = stackPanel,
				FontSize = num,
				Foreground = (isMain ? this.MainWindowTextColor : brushLeft),
				HorizontalContentAlignment = HorizontalAlignment.Left,
				VerticalAlignment = VerticalAlignment.Top,
				HorizontalAlignment = HorizontalAlignment.Stretch
			};
			label.SetValue(Grid.ColumnProperty, 0);
			grid.Children.Add(label);
			StackPanel stackPanel2 = new StackPanel
			{
				VerticalAlignment = VerticalAlignment.Center
			};
			stackPanel2.Children.Add(new TextBlock
			{
				Text = textRight,
				TextAlignment = global::System.Windows.TextAlignment.Left,
				TextWrapping = TextWrapping.Wrap,
				FontWeight = (isMain ? FontWeights.Bold : FontWeights.Normal),
				Margin = new Thickness(0.0, 0.0, 0.0, (double)(isMain ? 3 : 0))
			});
			if (!noteHeaderRight.IsNullOrEmpty())
			{
				stackPanel2.Children.Add(new TextBlock
				{
					Text = noteHeaderRight,
					FontStyle = FontStyles.Italic,
					FontSize = num - 2.0,
					Opacity = 0.8,
					TextWrapping = TextWrapping.Wrap,
					TextAlignment = global::System.Windows.TextAlignment.Left
				});
			}
			Label label2 = new Label
			{
				Content = stackPanel2,
				FontSize = num,
				Foreground = (isMain ? this.MainWindowTextColor : brushRight),
				HorizontalContentAlignment = HorizontalAlignment.Left,
				VerticalAlignment = VerticalAlignment.Top,
				HorizontalAlignment = HorizontalAlignment.Stretch
			};
			label2.SetValue(Grid.ColumnProperty, 1);
			grid.Children.Add(label2);
			Border border = new Border
			{
				BorderBrush = (this.mainWindowBorderColor ?? this.MainWindowTextColor),
				BorderThickness = new Thickness(0.0, 1.0, 0.0, (double)(isMain ? 2 : 1)),
				Margin = new Thickness(0.0)
			};
			if (isMain)
			{
				border.Background = (this.isLightTheme ? Brushes.DarkGray : brush);
			}
			border.Child = grid;
			return border;
		}
		private void OnToggleDraggableSeperator(object sender, DragDeltaEventArgs e)
		{
			base.TriggerCustomEvent(delegate(object state)
			{
				try
				{
					if (this.isCharting)
					{
						base.ChartControl.Dispatcher.InvokeAsync(delegate
						{
							this.heightRatio = (float)this.draggableSeperator.Margin.Bottom / (float)base.ChartPanel.ActualHeight;
							this.draggableSeperator.Margin = new Thickness(-1.0, -1.0, -1.0, this.draggableSeperator.Margin.Bottom - 5.0);
							this.actualMarginY = this.draggableSeperator.Margin.Bottom;
							if (this.btnResetScale != null)
							{
								this.btnResetScale.Margin = new Thickness(-1.0, -1.0, this.actualMarginX, this.actualMarginY - this.btnResetScale.Height);
							}
							this.DeltaChartHeightPercent = this.heightRatio * 100f;
							base.ForceRefresh();
						});
					}
				}
				catch
				{
				}
			}, e);
		}
		private void MakeCandlesInvisible()
		{
			base.CandleOutlineBrushes[-1] = (base.BarBrushes[-1] = Brushes.Transparent);
			for (int i = 0; i <= base.CurrentBars[0]; i++)
			{
				base.CandleOutlineBrushes[i] = (base.BarBrushes[i] = Brushes.Transparent);
			}
		}
		private void MakeCandlesVisible()
		{
			base.CandleOutlineBrushes[-1] = base.ChartBars.Properties.ChartStyle.Stroke2.Brush;
			double open = base.BarsArray[0].GetOpen(base.BarsArray[0].Count - 1);
			double close = base.BarsArray[0].GetClose(base.BarsArray[0].Count - 1);
			if (close > open)
			{
				base.BarBrushes[-1] = base.ChartBars.Properties.ChartStyle.UpBrush;
			}
			if (close < open)
			{
				base.BarBrushes[-1] = base.ChartBars.Properties.ChartStyle.DownBrush;
			}
			for (int i = 0; i <= base.CurrentBars[0]; i++)
			{
				base.CandleOutlineBrushes[i] = base.ChartBars.Properties.ChartStyle.Stroke2.Brush;
				if (base.Closes[0][i] > base.Opens[0][i])
				{
					base.BarBrushes[i] = base.ChartBars.Properties.ChartStyle.UpBrush;
				}
				if (base.Closes[0][i] < base.Opens[0][i])
				{
					base.BarBrushes[i] = base.ChartBars.Properties.ChartStyle.DownBrush;
				}
			}
		}
		
				private int GetDPI()
		{
			int dpi = 99;
			try
			{
				global::System.Windows.Window mainWindow = (global::System.Windows.Application.Current != null) ? global::System.Windows.Application.Current.MainWindow : null;
				if (mainWindow != null)
				{
					global::System.Windows.PresentationSource src = global::System.Windows.PresentationSource.FromVisual(mainWindow);
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
		private SharpDX.Size2F ComputeTextSize(string text, SimpleFont font, int dpi)
		{
			if (font == null) return new SharpDX.Size2F(0f, 12f);
			if (string.IsNullOrEmpty(text)) return new SharpDX.Size2F(0f, (float)(font.Size * 1.5));
			string[] lines = text.Split('\n');
			int maxLen = 0;
			for (int i = 0; i < lines.Length; i++)
			{
				if (lines[i].Length > maxLen) maxLen = lines[i].Length;
			}
			float lineHeight = (float)(font.Size * 1.5);
			float width = (float)(maxLen * font.Size * 0.7);
			float height = lineHeight * Math.Max(1, lines.Length);
			return new SharpDX.Size2F(width, height);
		}
		private string FormatMarkerString(string text)
		{
			if (string.IsNullOrWhiteSpace(text)) return string.Empty;
			string[] parts = text.Trim().Split(new[] { '+' }, StringSplitOptions.RemoveEmptyEntries);
			for (int i = 0; i < parts.Length; i++) parts[i] = parts[i].Trim();
			return string.Join("\n", parts).Trim();
		}
		private double ComputeMAValue(ISeries<double> input, DD_MAType maType, int period)
		{
			if (base.CurrentBar < 1)
			{
				return input[0];
			}
			if (maType == DD_MAType.SMA)
			{
				double sum = 0.0;
				int count = Math.Min(period, base.CurrentBar + 1);
				for (int i = 0; i < count; i++)
				{
					sum += input[i];
				}
				return sum / count;
			}
			double m = 2.0 / (period + 1);
			return input[0] * m + input[1] * (1.0 - m);
		}
		private void DrawText(string text, SimpleFont font, float x, float y, int angle, int direction, global::System.Windows.Media.Brush wpfBrush, int dpi, SharpDX.Direct2D1.RenderTarget renderTarget)
		{
			if (renderTarget == null || font == null || string.IsNullOrEmpty(text) || wpfBrush == null || wpfBrush.IsTransparent())
				return;

			SharpDX.Size2F size = this.ComputeTextSize(text, font, dpi);
			float top;
			if (direction > 0) top = y;
			else if (direction < 0) top = y - size.Height;
			else top = y - size.Height / 2f;

			SharpDX.RectangleF rect = new SharpDX.RectangleF(x - size.Width / 2f, top, size.Width, size.Height);
			using (SharpDX.DirectWrite.Factory factory = new SharpDX.DirectWrite.Factory())
			using (SharpDX.DirectWrite.TextFormat textFormat = new SharpDX.DirectWrite.TextFormat(factory, font.Family.ToString(), (float)font.Size))
			{
				textFormat.TextAlignment = SharpDX.DirectWrite.TextAlignment.Center;
				textFormat.ParagraphAlignment = SharpDX.DirectWrite.ParagraphAlignment.Near;
				textFormat.WordWrapping = SharpDX.DirectWrite.WordWrapping.NoWrap;
				SharpDX.Direct2D1.Brush dxBrush = wpfBrush.ToDxBrush(renderTarget);
				renderTarget.DrawText(text, textFormat, rect, dxBrush);
				dxBrush.Dispose();
			}
		}
		
		private const byte marginValue = 10;
		private const double mainWindowMinWidth = 250.0;
		private const double mainWindowMinHeight = 200.0;
		private const double childWindowMinWidth = 200.0;
		private const double childWindowMinHeight = 300.0;
		private const byte buttonMinWidth = 100;
		private const byte buttonMinHeight = 30;
		private DD_TextPosition togglePositionAlignment;
		private const int defaultMargin = 5;
		private const string toolTipSpace = "  ";
		private const int btnSignalMinHeight = 40;
		private Series<double> seriesBody;
		private Series<double> seriesRange;
		private Series<double> seriesVolumeTotal;
		private Series<double> seriesVolumeDelta;
		private Series<double> seriesVolumeBuy;
		private Series<double> seriesVolumeSell;
		private Series<double> seriesVolumeDeltaPositive;
		private Series<double> seriesVolumeDeltaNegative;
		private TextFormat textFormatDefault;
		private TextFormat textFormatStrong;
		private Dictionary<int, TextLayout> dictTextLayoutStrong;
		private Dictionary<int, TextLayout> dictTextLayoutDefault;
		private List<double> listRawPositiveDelta;
		private List<double> listRawNegativeDelta;
		private SortedList<int, DDApexFlowZignal.SignalConditionBody> dictAbsorptionCondition;
		private SortedList<int, DDApexFlowZignal.SignalConditionBody> dictExhaustionCondition;
		private SortedList<int, DDApexFlowZignal.SignalConditionBody> dictPushCondition;
		private double sumPositiveDelta;
		private double sumNegativeDelta;
		private global::System.Windows.Media.Brush themeBuyStrongColor;
		private global::System.Windows.Media.Brush themeBuyWeakColor;
		private global::System.Windows.Media.Brush themeNeutralColor;
		private global::System.Windows.Media.Brush themeSellWeakColor;
		private global::System.Windows.Media.Brush themeSellStrongColor;
		private global::System.Windows.Media.Brush thresholdTotalColor;
		private global::System.Windows.Media.Brush thresholdBuyColor;
		private global::System.Windows.Media.Brush thresholdSellColor;
		private global::System.Windows.Media.Brush peakBuyPointColor;
		private global::System.Windows.Media.Brush peakSellPointColor;
		private global::System.Windows.Media.Brush deltaChartBarPositiveColor;
		private global::System.Windows.Media.Brush deltaChartBarNeutralColor;
		private global::System.Windows.Media.Brush deltaChartBarNegativeColor;
		private global::System.Windows.Media.Brush deltaChartBackgroundColor;
		private global::System.Windows.Media.Brush avgBuyCloudColor;
		private global::System.Windows.Media.Brush avgSellCloudColor;
		private SimpleFont labelTextFont = new SimpleFont("Arial", 10);
		private DDApexFlowZignal.ConditionGroup absorptionCondition;
		private DDApexFlowZignal.ConditionGroup exhaustionCondition;
		private DDApexFlowZignal.ConditionGroup pushCondition;
		private Dictionary<DDApexFlowZignal.SignalType, DDApexFlowZignal.ConditionGroup> dictConditionGroup;
		private PropertyInfo[] properties;
		private Series<double> seriesSignalTrade;
		private int maxPeriod;
		private bool isUpDownTickMode;
		private bool isMarkerCustomRenderingMethod;
		private DDApexFlowZignal_PresentationStyle PresentationStyleBar;
		private float heightRatio;
		private double maxWickBullishRatio;
		private double maxWickBearishRatio;
		private double minBodyBullishRatio;
		private double minBodyBearishRatio;
		private double neutralRange;
		private bool barText;
		private bool barTable;
		private bool barProfileCombined;
		private bool barProfileDivided;
		private bool barProfileDelta;
		private const string nickname = "apexflow:exc";
		private const string prefix = "DDApexFlowZignal";
		private const string indicatorName = "ApexFlow Zignal";
		private const string indicatorNameFull = "ApexFlow Zignal by DD.co";
		private const string receiverEmail = "receiver@example.com";
		private bool isCharting;
		private bool isLightTheme;
		private static global::System.Windows.Media.Brush childWindowBackground;
		private bool isDayWeakMonthYear;
		private double maxVolume = double.MinValue;
		private int lastExhaustionBarIndex = -1;
		private int lastAbsorptionBarIndex = -1;
		private const double maxNumberOfRows = 350.0;
		private ChartScale scaleOfChart;
		private int minKey;
		private int maxKey;
		private int minPixelsBetween2Rows;
		private int autoThickness;
		private float cellWidth;
		private float candleMargin;
		private bool errorPrinted;
		private bool hideBars;
		private float minY;
		private float deltaChartCenterY;
		private float barDistance;
		private double barWidth;
		private bool allowDrawText;
		private Size2F deltaChartMaxTextSize;
		private float marginY = 20f;
		private float marginX = 8f;
		private double actualMarginX;
		private double actualMarginY;
		private float maxBarHeight;
		private double maxVol = double.MinValue;
		private float yStartValueBar;
		private float yEndValueBar;
		private bool lastTickIsBuy;
		private bool recalcSkippedBar = true;
		private int tickState;
		private int barShift;
		private int tickVolumeBarIndex;
		private int tickVolumeCurrentBar1 = -1;
		private Queue<int> queueCurrentBar1;
		private Dictionary<int, DDApexFlowZignal.Presentation> dictPresentations;
		private Queue<DDApexFlowZignal.PresentationCell> queuePresentationCell;
		private List<int> listStartBarIndex1;
		private Dictionary<int, DDApexFlowZignal.MarkerInfo> dictMarkers;
		private double minPrice = double.MaxValue;
		private double maxPrice = double.MinValue;
		private Canvas canvasMouseInfo;
		private Border borderMouseInfo;
		private TextBlock txtMouseInfo;
		private bool isShowInfoActive;
		private Button btnResetScale;
		private bool isManualScaling;
		private double lastMouseY;
		private bool isScaling;
		private int retrieveBarIndex;
		private int factorBottom;
		private int factorTop;
		private DateTime nextAlert = DateTime.MinValue;
		private DateTime nextRearm = DateTime.MinValue;
		private string soundPath = string.Empty;
		private DispatcherTimer rearmTimer;
		private double AbsorptionWindowLeft;
		private double AbsorptionWindowTop;
		private double AbsorptionWindowWidth;
		private double AbsorptionWindowHeight;
		private double ExhaustionWindowLeft;
		private double ExhaustionWindowTop;
		private double ExhaustionWindowWidth;
		private double ExhaustionWindowHeight;
		private double PushWindowLeft;
		private double PushWindowTop;
		private double PushWindowWidth;
		private double PushWindowHeight;
		private bool isClickEvent;
		private NTWindow pushWindowNT;
		private Grid gridPushContent;
		private Button btnPushApply;
		private Button btnPushOK;
		private Button btnPushCancel;
		private Button btnPushSetting;
		private TextBox txtPushN;
		private NTWindow exhaustionWindowNT;
		private Grid gridExhaustionContent;
		private Button btnExhaustionApply;
		private Button btnExhaustionOK;
		private Button btnExhaustionCancel;
		private Button btnExhaustionSetting;
		private TextBox txtExhaustionN;
		private NTWindow absorptionWindowNT;
		private Grid gridAbsorptionContent;
		private Button btnAbsorptionOK;
		private Button btnAbsorptionApply;
		private Button btnAbsorptionCancel;
		private Button btnArsorptionSetting;
		private TextBox txtAbsorptionN;
		private NTWindow mainWindowNT;
		private global::System.Windows.Media.Brush mainWindowBorderColor;
		private DDApexFlowZignal.Body bodyAbBar;
		private DDApexFlowZignal.Body bodyExBar;
		private DDApexFlowZignal.Body bodyPushBar;
		private DDApexFlowZignal.Body bodyAbPush;
		private DDApexFlowZignal.Body bodyExPush;
		private Grid gridMainContent;
		private Button btnMainOK;
		private Button btnMainCancel;
		private Button btnMainApply;
		private float minSideBarWidth = 170f;
		private float stackTypeSideBarWidth = 32f;
		private Dictionary<string, string> tempParameterValueBuffer = new Dictionary<string, string>();
		private double fontSizeHeader = 16.0;
		private double fontSizeSubHeader = 14.0;
		private double fontSizeCondition = 12.0;
		private DDApexFlowZignal.DraggableSeperatorPanel draggableSeperator;
		private static class HeaderNotes
		{
			static HeaderNotes()
			{
				NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.HeaderNotes.noteAbsorptionLeft = string.Format("Strong {0} effort, but the outcome favors the {1} side and is insignificant.", "Sell", "Buy");
				NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.HeaderNotes.noteAbsorptionRight = string.Format("Strong {0} effort, but the outcome favors the {1} side and is insignificant.", "Buy", "Sell");
				NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.HeaderNotes.noteExhaustionLeft = string.Format("{0}-side effort persists but momentum fades and exhausts.", "Sell");
				NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.HeaderNotes.noteExhaustionRight = string.Format("{0}-side effort persists but momentum fades and exhausts.", "Buy");
				NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.HeaderNotes.notePushLeft = string.Format("Only {0} participate, showing clear strength.\n{1} effort is significant and results are clear.", "Buyers", "Buy");
				NinjaTrader.NinjaScript.Indicators.DimDim.DDApexFlowZignal.HeaderNotes.notePushRight = string.Format("Only {0} participate, showing clear strength.\n{1} effort is significant and results are clear.", "Sellers", "Sell");
			}
			public const string settingIconData = "M12,15.5A3.5,3.5 0 0,1 8.5,12A3.5,3.5 0 0,1 12,8.5A3.5,3.5 0 0,1 15.5,12A3.5,3.5 0 0,1 12,15.5M19.43,12.97C19.47,12.65 19.5,12.33 19.5,12C19.5,11.67 19.47,11.35 19.43,11.03L21.54,9.37C21.73,9.22 21.78,8.95 21.66,8.73L19.66,5.27C19.54,5.05 19.27,4.97 19.05,5.05L16.56,6.05C16.04,5.65 15.47,5.32 14.87,5.07L14.5,2.42C14.46,2.18 14.25,2 14,2H10C9.75,2 9.54,2.18 9.5,2.42L9.13,5.07C8.53,5.32 7.96,5.65 7.44,6.05L4.95,5.05C4.73,4.97 4.46,5.05 4.34,5.27L2.34,8.73C2.21,8.95 2.27,9.22 2.46,9.37L4.57,11.03C4.53,11.35 4.5,11.67 4.5,12C4.5,12.33 4.53,12.65 4.57,12.97L2.46,14.63C2.27,14.78 2.21,15.05 2.34,15.27L4.34,18.73C4.46,18.95 4.73,19.03 4.95,18.95L7.44,17.95C7.96,18.35 8.53,18.68 9.13,18.93L9.5,21.58C9.54,21.82 9.75,22 10,22H14C14.25,22 14.46,21.82 14.5,21.58L14.87,18.93C15.47,18.68 16.04,18.35 16.56,17.95L19.05,18.95C19.27,19.03 19.54,18.95 19.66,18.73L21.66,15.27C21.78,15.05 21.73,14.78 21.54,14.63L19.43,12.97Z";
			public const string openWindowIconData = "M19,19H5V5H12V3H5C3.89,3 3,3.89 3,5V19C3,20.1 3.89,21 5,21H19C20.1,21 21,20.1 21,19V12H19V19M14,3V5H17.59L7.76,14.83L9.17,16.24L19,6.41V10H21V3H14Z";
			private const string noteAbsorptionStringFormat = "Strong {0} effort, but the outcome favors the {1} side and is insignificant.";
			private const string noteExhaustionStringFormat = "{0}-side effort persists but momentum fades and exhausts.";
			private const string notePushStringFormat = "Only {0} participate, showing clear strength.\n{1} effort is significant and results are clear.";
			public static string noteAbsorptionLeft;
			public static string noteAbsorptionRight;
			public static string noteExhaustionLeft;
			public static string noteExhaustionRight;
			public static string notePushLeft;
			public static string notePushRight;
		}
		public enum ConditionType
		{
			Null,
			Above,
			Below,
			Max,
			Min,
			Up,
			Down,
			Smaller,
			Greater,
			Decreasing,
			Increasing,
			Tick
		}
		public enum DataType
		{
			Bar,
			Time,
			Body,
			Wick,
			Spread,
			Volume,
			VolumeBuy,
			VolumeSell,
			VolumeDelta,
			VolumePostiveDelta,
			VolumeNegativeDelta,
			POC,
			Close
		}
		public enum SignalType
		{
			Absorption = 1,
			Exhaustion,
			Push,
			AbPush,
			ExPush
		}
		
		public enum DDApexFlowZignal_PresentationStyle
		{
			Table,
			Text,
			ProfileDivided,
			ProfileCombined,
			ProfileDelta
		}
				
		public enum DDApexFlowZignal_RenderingMethod
		{
			Builtin,
			Custom
		}
		
		public enum DDApexFlowZignal_VolumeBase
		{
			BidAskPrice_RealVolume,
			UpDownTick_RealVolume,
			UpDownTick_UnitVolume
		}
		
		public class ConditionGroup
		{
			public DDApexFlowZignal.SignalType SignalType { get; set; }
			public bool IsBuyEnabled { get; set; }
			public List<DDApexFlowZignal.ConditionInfo> ListBuyConditionInfo { get; set; }
			public bool IsSellEnabled { get; set; }
			public List<DDApexFlowZignal.ConditionInfo> ListSellConditionInfo { get; set; }
			public ConditionGroup(DDApexFlowZignal.SignalType type)
			{
				this.SignalType = type;
				this.ListBuyConditionInfo = new List<DDApexFlowZignal.ConditionInfo>();
				this.ListSellConditionInfo = new List<DDApexFlowZignal.ConditionInfo>();
			}
			public int GetSignal(int currentBar)
			{
				if (this.ListBuyConditionInfo.Count == 0 && this.ListSellConditionInfo.Count == 0)
				{
					return 0;
				}
				int signalType = (int)this.SignalType;
				if (this.ListBuyConditionInfo.Count > 0)
				{
					bool flag = true;
					foreach (DDApexFlowZignal.ConditionInfo conditionInfo in this.ListBuyConditionInfo)
					{
						if (conditionInfo.ConditionFunc != null && !conditionInfo.ExcuteConditionFunc(currentBar))
						{
							flag = false;
							break;
						}
					}
					if (flag)
					{
						return signalType;
					}
				}
				if (this.ListSellConditionInfo.Count > 0)
				{
					foreach (DDApexFlowZignal.ConditionInfo conditionInfo2 in this.ListSellConditionInfo)
					{
						if (conditionInfo2.ConditionFunc != null && !conditionInfo2.ExcuteConditionFunc(currentBar))
						{
							return 0;
						}
					}
					return -signalType;
				}
				return 0;
			}
		}
		public class ConditionInfo
		{
			public bool IsBullish { get; set; }
			public DDApexFlowZignal.DataType DataType { get; set; }
			public DDApexFlowZignal.ConditionType ConditionType { get; set; }
			public int Period { get; set; }
			public Func<DDApexFlowZignal.DataType, DDApexFlowZignal.ConditionType, int, int, bool, bool> ConditionFunc { get; set; }
			public ConditionInfo(bool isBullish, Func<DDApexFlowZignal.DataType, DDApexFlowZignal.ConditionType, int, int, bool, bool> conditionFunc, DDApexFlowZignal.DataType dataType, DDApexFlowZignal.ConditionType conditionType, int period = 0)
			{
				this.IsBullish = isBullish;
				this.DataType = dataType;
				this.ConditionType = conditionType;
				this.Period = period;
				this.ConditionFunc = conditionFunc;
			}
			public bool ExcuteConditionFunc(int currentBar)
			{
				return this.ConditionFunc != null && this.ConditionFunc(this.DataType, this.ConditionType, currentBar, this.Period, this.IsBullish);
			}
		}
		public enum FuncType
		{
			ConditionDirection,
			ConditionNeutralRange,
			ConditionVolumeExtrema,
			ConditionBodyOrSpreadExtrema,
			ConditionBodyOrSpreadNonExtreme,
			ConditionVolumeAvg,
			ConditionDeltaAverage,
			ConditionCompareRange,
			ConditionCompareTick,
			ConditionConsecutiveBar,
			ConditionConsecutiveBodyOrSpread,
			ConditionConsecutiveVolume
		}
		public class Presentation
		{
			public int POCKey { get; set; }
			public int POCBuyKey { get; set; }
			public int POCSellKey { get; set; }
			public int LowKey { get; set; }
			public int HighKey { get; set; }
			public double VolBuy { get; set; }
			public double VolSell { get; set; }
			public double VolDelta { get; set; }
			public double VolDeltaMin { get; set; }
			public double VolDeltaMax { get; set; }
			public double POCVol { get; set; }
			public double POCBuyVol { get; set; }
			public double POCSellVol { get; set; }
			public double POCDeltaVol { get; set; }
			public double AvgBuy { get; set; }
			public double AvgSell { get; set; }
			public double AvgDeltaPositive { get; set; }
			public double AvgDeltaNegative { get; set; }
			public double AvgVolTotal { get; set; }
			internal Presentation()
			{
				this.DictCell = new Dictionary<int, DDApexFlowZignal.PresentationCell>();
				this.Clear();
			}
			internal void Clear()
			{
				this.DictCell.Clear();
				this.VolDeltaMin = 0.0;
				this.VolDeltaMax = 0.0;
				this.VolBuy = (this.VolSell = (this.VolDelta = 0.0));
				this.POCKey = (this.POCBuyKey = (this.POCSellKey = 0));
				this.LowKey = int.MaxValue;
				this.HighKey = 0;
				this.POCVol = (this.POCBuyVol = (this.POCSellVol = (this.POCDeltaVol = 0.0)));
				this.AvgBuy = (this.AvgSell = (this.AvgDeltaPositive = (this.AvgDeltaNegative = (this.AvgVolTotal = 0.0))));
				this.sumDeltaPositive = (this.sumDeltaNegative = 0.0);
				this.totalBuyKeys = (this.totalSellKeys = (this.positiveDeltaLevels = (this.negativeDeltaLevels = 0)));
			}
			internal DDApexFlowZignal.PresentationCell GetValue(int key)
			{
				if (!this.DictCell.ContainsKey(key))
				{
					return new DDApexFlowZignal.PresentationCell(0.0, 0.0, 0, 0.0, 0L, 0);
				}
				return this.DictCell[key];
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
			internal double GetDeltaVol(int key)
			{
				return this.GetValue(key).VolDelta;
			}
			internal double GetPOCDelta()
			{
				return this.GetValue(this.POCKey).VolDelta;
			}
			internal void AddBuy(int key, double vol)
			{
				bool flag = !this.DictCell.ContainsKey(key);
				double num = (flag ? 0.0 : this.DictCell[key].VolDelta);
				if (flag)
				{
					this.DictCell.Add(key, new DDApexFlowZignal.PresentationCell(vol, 0.0, 0, 0.0, 0L, 0));
					this.totalBuyKeys++;
					this.totalKeys++;
				}
				else
				{
					if (this.DictCell[key].VolBuy == 0.0)
					{
						this.totalBuyKeys++;
					}
					this.DictCell[key].VolBuy += vol;
					this.DictCell[key].VolDelta += vol;
				}
				double num2 = Math.Abs(this.DictCell[key].VolDelta);
				if (num2.ApproxCompare(this.POCDeltaVol) >= 0)
				{
					this.POCDeltaVol = num2;
				}
				this.VolBuy += vol;
				this.VolDelta = this.VolBuy - this.VolSell;
				this.VolDeltaMax = Math.Max(this.VolDelta, this.VolDeltaMax);
				this.VolDeltaMin = Math.Min(this.VolDelta, this.VolDeltaMin);
				double totalVol = this.GetTotalVol(key);
				if (totalVol > this.POCVol)
				{
					this.POCKey = key;
					this.POCVol = totalVol;
				}
				if (this.DictCell[key].VolBuy > this.POCBuyVol)
				{
					this.POCBuyKey = key;
					this.POCBuyVol = this.DictCell[key].VolBuy;
				}
				this.LowKey = Math.Min(key, this.LowKey);
				this.HighKey = Math.Max(key, this.HighKey);
				this.AvgBuy = this.VolBuy / (double)this.totalBuyKeys;
				this.AvgVolTotal = (this.VolBuy + this.VolSell) / (double)this.totalKeys;
				double volDelta = this.DictCell[key].VolDelta;
				if (flag)
				{
					if (volDelta > 0.0)
					{
						this.sumDeltaPositive += volDelta;
						this.positiveDeltaLevels++;
					}
					else if (volDelta < 0.0)
					{
						this.sumDeltaNegative += volDelta;
						this.negativeDeltaLevels++;
					}
				}
				else if (volDelta > 0.0)
				{
					if (num < 0.0)
					{
						this.sumDeltaNegative -= num;
						this.negativeDeltaLevels--;
						this.positiveDeltaLevels++;
					}
					else
					{
						if (num == 0.0)
						{
							this.positiveDeltaLevels++;
						}
						this.sumDeltaPositive -= num;
					}
					this.sumDeltaPositive += volDelta;
				}
				else if (volDelta < 0.0)
				{
					if (num > 0.0)
					{
						this.sumDeltaPositive -= num;
						this.positiveDeltaLevels--;
						this.negativeDeltaLevels++;
					}
					else
					{
						this.sumDeltaNegative -= num;
						if (num == 0.0)
						{
							this.negativeDeltaLevels++;
						}
					}
					this.sumDeltaNegative += volDelta;
				}
				else if (num < 0.0)
				{
					this.sumDeltaNegative -= num;
					this.negativeDeltaLevels--;
				}
				else if (num > 0.0)
				{
					this.sumDeltaPositive -= num;
					this.positiveDeltaLevels--;
				}
				this.AvgDeltaPositive = ((this.positiveDeltaLevels > 0) ? (this.sumDeltaPositive / (double)this.positiveDeltaLevels) : 0.0);
				this.AvgDeltaNegative = ((this.negativeDeltaLevels > 0) ? (this.sumDeltaNegative / (double)this.negativeDeltaLevels) : 0.0);
			}
			internal void AddSell(int key, double vol)
			{
				bool flag = !this.DictCell.ContainsKey(key);
				double num = (flag ? 0.0 : this.DictCell[key].VolDelta);
				if (flag)
				{
					this.DictCell.Add(key, new DDApexFlowZignal.PresentationCell(0.0, vol, 0, 0.0, 0L, 0));
					this.totalSellKeys++;
					this.totalKeys++;
				}
				else
				{
					if (this.DictCell[key].VolSell == 0.0)
					{
						this.totalSellKeys++;
					}
					this.DictCell[key].VolSell += vol;
					this.DictCell[key].VolDelta -= vol;
				}
				double num2 = Math.Abs(this.DictCell[key].VolDelta);
				if (num2.ApproxCompare(this.POCDeltaVol) >= 0)
				{
					this.POCDeltaVol = num2;
				}
				this.VolSell += vol;
				this.VolDelta = this.VolBuy - this.VolSell;
				this.VolDeltaMax = Math.Max(this.VolDelta, this.VolDeltaMax);
				this.VolDeltaMin = Math.Min(this.VolDelta, this.VolDeltaMin);
				double totalVol = this.GetTotalVol(key);
				if (totalVol > this.POCVol)
				{
					this.POCKey = key;
					this.POCVol = totalVol;
				}
				if (this.DictCell[key].VolSell > this.POCSellVol)
				{
					this.POCSellKey = key;
					this.POCSellVol = this.DictCell[key].VolSell;
				}
				this.LowKey = Math.Min(key, this.LowKey);
				this.HighKey = Math.Max(key, this.HighKey);
				this.AvgSell = this.VolSell / (double)this.totalSellKeys;
				this.AvgVolTotal = (this.VolBuy + this.VolSell) / (double)this.totalKeys;
				double volDelta = this.DictCell[key].VolDelta;
				if (flag)
				{
					if (volDelta > 0.0)
					{
						this.sumDeltaPositive += volDelta;
						this.positiveDeltaLevels++;
					}
					else if (volDelta < 0.0)
					{
						this.sumDeltaNegative += volDelta;
						this.negativeDeltaLevels++;
					}
				}
				else if (volDelta > 0.0)
				{
					if (num < 0.0)
					{
						this.sumDeltaNegative -= num;
						this.negativeDeltaLevels--;
						this.positiveDeltaLevels++;
					}
					else
					{
						this.sumDeltaPositive -= num;
						if (num == 0.0)
						{
							this.positiveDeltaLevels++;
						}
					}
					this.sumDeltaPositive += volDelta;
				}
				else if (volDelta < 0.0)
				{
					if (num > 0.0)
					{
						this.sumDeltaPositive -= num;
						this.positiveDeltaLevels--;
						this.negativeDeltaLevels++;
					}
					else
					{
						if (num == 0.0)
						{
							this.negativeDeltaLevels++;
						}
						this.sumDeltaNegative -= num;
					}
					this.sumDeltaNegative += volDelta;
				}
				else if (num < 0.0)
				{
					this.sumDeltaNegative -= num;
					this.negativeDeltaLevels--;
				}
				else if (num > 0.0)
				{
					this.sumDeltaPositive -= num;
					this.positiveDeltaLevels--;
				}
				this.AvgDeltaPositive = ((this.positiveDeltaLevels > 0) ? (this.sumDeltaPositive / (double)this.positiveDeltaLevels) : 0.0);
				this.AvgDeltaNegative = ((this.negativeDeltaLevels > 0) ? (this.sumDeltaNegative / (double)this.negativeDeltaLevels) : 0.0);
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
			public Dictionary<int, DDApexFlowZignal.PresentationCell> DictCell;
			private double sumDeltaPositive;
			private double sumDeltaNegative;
			private int totalBuyKeys;
			private int totalSellKeys;
			private int totalKeys;
			private int positiveDeltaLevels;
			private int negativeDeltaLevels;
		}
		public class PresentationCell
		{
			public double VolBuy { get; set; }
			public double VolSell { get; set; }
			public double VolDelta { get; set; }
			public double Price { get; set; }
			public int Key { get; set; }
			public long TimeTicks { get; set; }
			public long BarIndex { get; set; }
			internal PresentationCell(double volBuy, double volSell, int key = 0, double price = 0.0, long timeTicks = 0L, int barIndex = 0)
			{
				this.VolBuy = volBuy;
				this.VolSell = volSell;
				this.VolDelta = volBuy - volSell;
				this.Key = key;
				this.Price = price;
				this.TimeTicks = timeTicks;
				this.BarIndex = (long)barIndex;
			}
		}
		private class MarkerInfo
		{
			public int BarIndex { get; set; }
			public List<DDApexFlowZignal.SignalInfo> ListOfSignalInfo { get; set; }
			public MarkerInfo(int barIndex, bool isBullish, List<DDApexFlowZignal.SignalInfo> listOfSignalInfo)
			{
				this.BarIndex = barIndex;
				this.ListOfSignalInfo = listOfSignalInfo;
			}
		}
		private class SignalInfo
		{
			public bool IsBullish { get; set; }
			public DDApexFlowZignal.SignalType SignalType { get; set; }
			public SignalInfo(bool isBullish, DDApexFlowZignal.SignalType signalType)
			{
				this.IsBullish = isBullish;
				this.SignalType = signalType;
			}
		}
		[AttributeUsage(AttributeTargets.Property)]
		public class ConditionPropertiesAttribute : Attribute
		{
			public int Key { get; set; }
			public string ConditionText { get; set; }
			public DDApexFlowZignal.SignalType SignalType { get; set; }
			public bool IsBuy { get; set; }
			public DDApexFlowZignal.FuncType FuncType { get; set; }
			public DDApexFlowZignal.DataType DataType { get; set; }
			public DDApexFlowZignal.ConditionType ConditionType { get; set; }
			public bool IsHeaderNote { get; set; }
			public string HeaderLeft { get; set; }
			public string HeaderRight { get; set; }
			public bool IsOppositeDirection { get; set; }
			public bool IsWeak { get; set; }
		}
		private class SignalConditionBody
		{
			public Grid Grid { get; set; }
			public bool IsBuyEnabled { get; set; }
			public string ConditionBuyText { get; set; }
			public CheckBox CheckBoxBuy { get; set; }
			public bool IsSellEnabled { get; set; }
			public string ConditionSellText { get; set; }
			public CheckBox CheckBoxSell { get; set; }
			public bool IsHeaderNote { get; set; }
			public string HeaderLeft { get; set; }
			public string HeaderRight { get; set; }
			public bool IsOppositeDirection { get; set; }
			public bool IsWeak { get; set; }
			public SignalConditionBody()
			{
			}
			public SignalConditionBody(Grid grid, bool isBuyEnabled, string conditionBuyText, CheckBox checkBoxBuy, bool isSellEnabled, string conditionSellText, CheckBox checkBoxSell)
			{
				this.Grid = grid;
				this.IsBuyEnabled = isBuyEnabled;
				this.ConditionBuyText = conditionBuyText;
				this.CheckBoxBuy = checkBoxBuy;
				this.IsSellEnabled = isSellEnabled;
				this.ConditionSellText = conditionSellText;
				this.CheckBoxSell = checkBoxSell;
			}
		}
		private class Body
		{
			public Grid Grid { get; set; }
			public CheckBox CheckBoxEnabled { get; set; }
			public bool IsBuyEnabled { get; set; }
			public Button ToggleBuy { get; set; }
			public bool IsSellEnabled { get; set; }
			public Button ToggleSell { get; set; }
			public Body(Grid grid, CheckBox checkBoxEnabled, bool isBuyEnabled, Button toggleBuy, bool isSellEnabled, Button toggleSell)
			{
				this.Grid = grid;
				this.CheckBoxEnabled = checkBoxEnabled;
				this.IsBuyEnabled = isBuyEnabled;
				this.ToggleBuy = toggleBuy;
				this.IsSellEnabled = isSellEnabled;
				this.ToggleSell = toggleSell;
			}
		}
		private class DraggableSeperatorPanel : Grid
		{
			internal DraggableSeperatorPanel(global::System.Windows.Media.Brush dragBrush, NinjaTrader.NinjaScript.DrawingTools.TextPosition alignment, Thickness thickness, float width)
			{
				this.alignment = alignment;
				this.thickness = thickness;
				this.drag = new Thumb
				{
					Cursor = Cursors.SizeNS,
					HorizontalAlignment = HorizontalAlignment.Stretch,
					Height = 10.0,
					ToolTip = "Drag to resize the sub-panel."
				};
				this.drag.DragDelta += this.OnDragDelta;
				FrameworkElementFactory frameworkElementFactory = new FrameworkElementFactory(typeof(Border));
				frameworkElementFactory.SetValue(Border.BackgroundProperty, Brushes.Transparent);
				FrameworkElementFactory frameworkElementFactory2 = new FrameworkElementFactory(typeof(Border));
				frameworkElementFactory2.SetValue(Border.BackgroundProperty, DD_BrushManager.CreateOpacityBrush(dragBrush, 80));
				frameworkElementFactory2.SetValue(FrameworkElement.HeightProperty, 0.7);
				frameworkElementFactory.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Stretch);
				frameworkElementFactory.AppendChild(frameworkElementFactory2);
				this.drag.Template = new ControlTemplate(typeof(Thumb))
				{
					VisualTree = frameworkElementFactory
				};
				base.Width = (double)width;
				base.Height = this.drag.Height;
				this.SetPosition(thickness);
				base.Children.Add(this.drag);
			}
			private void OnDragDelta(object sender, DragDeltaEventArgs e)
			{
				try
				{
					double num4;
					double num3;
					double num2;
					double num = (num2 = (num3 = (num4 = 5.0)));
					if (base.HorizontalAlignment == HorizontalAlignment.Left)
					{
						num2 = base.Margin.Left + e.HorizontalChange;
					}
					else
					{
						num3 = base.Margin.Right - e.HorizontalChange;
					}
					if (base.VerticalAlignment == VerticalAlignment.Top)
					{
						num = base.Margin.Top + e.VerticalChange;
					}
					else
					{
						num4 = base.Margin.Bottom - e.VerticalChange;
					}
					num2 = Math.Max(0.0, num2);
					num = Math.Max(0.0, num);
					num3 = Math.Max(0.0, num3);
					num4 = Math.Max(0.0, num4);
					base.Margin = new Thickness(-1.0, -1.0, -1.0, num4);
				}
				catch
				{
				}
			}
			public void SetPosition(Thickness thickness)
			{
				if (this.alignment == NinjaTrader.NinjaScript.DrawingTools.TextPosition.Center)
				{
					base.HorizontalAlignment = HorizontalAlignment.Center;
					base.VerticalAlignment = VerticalAlignment.Center;
					return;
				}
				if (this.alignment == NinjaTrader.NinjaScript.DrawingTools.TextPosition.TopLeft || this.alignment == NinjaTrader.NinjaScript.DrawingTools.TextPosition.BottomLeft)
				{
					base.HorizontalAlignment = HorizontalAlignment.Left;
				}
				else
				{
					base.HorizontalAlignment = HorizontalAlignment.Right;
				}
				if (this.alignment == NinjaTrader.NinjaScript.DrawingTools.TextPosition.TopLeft || this.alignment == NinjaTrader.NinjaScript.DrawingTools.TextPosition.TopRight)
				{
					base.VerticalAlignment = VerticalAlignment.Top;
				}
				else
				{
					base.VerticalAlignment = VerticalAlignment.Bottom;
				}
				base.Margin = thickness;
			}
			internal Thumb drag;
			private NinjaTrader.NinjaScript.DrawingTools.TextPosition alignment;
			private Thickness thickness;
		}

		public enum DD_TextPosition { TopLeft, TopRight, BottomLeft, BottomRight, Center }

		private enum DD_MAType { EMA, SMA, DEMA, HMA, LinReg, TEMA, TMA, VWMA, WMA }

		private static class DD_BrushManager
		{
			public static global::System.Windows.Media.Brush CreateOpacityBrush(global::System.Windows.Media.Brush brush, int opacity)
			{
				if (brush == null) return global::System.Windows.Media.Brushes.Transparent;
				global::System.Windows.Media.Brush clone = brush.Clone();
				clone.Opacity = opacity / 100.0;
				clone.Freeze();
				return clone;
			}

			public static global::System.Windows.Media.Brush CreateGradientBrush(global::System.Windows.Media.Brush brushStart, global::System.Windows.Media.Brush brushEnd, double factor)
			{
				global::System.Windows.Media.SolidColorBrush start = brushStart as global::System.Windows.Media.SolidColorBrush;
				global::System.Windows.Media.SolidColorBrush end = brushEnd as global::System.Windows.Media.SolidColorBrush;
				if (start == null) return brushEnd ?? global::System.Windows.Media.Brushes.Transparent;
				if (end == null) return brushStart;
				if (factor < 0.0) factor = 0.0;
				else if (factor > 1.0) factor = 1.0;
				global::System.Windows.Media.Color color = global::System.Windows.Media.Color.FromArgb(
					(byte)(start.Color.A + (end.Color.A - start.Color.A) * factor),
					(byte)(start.Color.R + (end.Color.R - start.Color.R) * factor),
					(byte)(start.Color.G + (end.Color.G - start.Color.G) * factor),
					(byte)(start.Color.B + (end.Color.B - start.Color.B) * factor));
				global::System.Windows.Media.SolidColorBrush result = new global::System.Windows.Media.SolidColorBrush(color);
				result.Freeze();
				return result;
			}
		}

		private static class DDResources_GlobalConstantAndFunction
		{
			public static bool? IsLightTheme()
			{
				return IsLightTheme(GetSkinBrush("ChartControl.ChartBackground", global::System.Windows.Media.Brushes.White));
			}

			public static bool? IsLightTheme(global::System.Windows.Media.Brush backgroundBrush)
			{
				global::System.Windows.Media.Color? color = GetBrushColor(backgroundBrush);
				if (!color.HasValue) return null;
				return GetBrightness(color.Value) >= 128.0;
			}

			private static global::System.Windows.Media.Brush GetSkinBrush(string resourceName, global::System.Windows.Media.Brush fallback)
			{
				try
				{
					if (global::System.Windows.Application.Current != null)
					{
						object resource = global::System.Windows.Application.Current.TryFindResource(resourceName);
						global::System.Windows.Media.Brush brush = resource as global::System.Windows.Media.Brush;
						if (brush != null) return brush;
						if (resource is global::System.Windows.Media.Color)
							return new global::System.Windows.Media.SolidColorBrush((global::System.Windows.Media.Color)resource);
					}
				}
				catch
				{
				}
				return fallback;
			}

			private static global::System.Windows.Media.Color? GetBrushColor(global::System.Windows.Media.Brush brush)
			{
				global::System.Windows.Media.SolidColorBrush solidColorBrush = brush as global::System.Windows.Media.SolidColorBrush;
				if (solidColorBrush == null) return null;
				return solidColorBrush.Color;
			}

			private static double GetBrightness(global::System.Windows.Media.Color color)
			{
				return color.R * 0.299 + color.G * 0.587 + color.B * 0.114;
			}
		}
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private DimDim.DDApexFlowZignal[] cacheDDApexFlowZignal;
		public DimDim.DDApexFlowZignal DDApexFlowZignal(int avgVolPeriod, int avgDeltaPeriod, int neutralRange, int absorptionN, int exhaustionN, int pushN, int minBodyTicks, int maxWickPercentBullish, int minBodyPercentBullish, int maxWickPercentBearish, int minBodyPercentBearish, int abPushPeriod, int exPushPeriod, bool sCAbsorptionEnabled, bool sCAbsorptionBullish, bool sCAbsorptionBearish, bool sCExhaustionEnabled, bool sCExhaustionBullish, bool sCExhaustionBearish, bool sCPushEnabled, bool sCPushBullish, bool sCPushBearish, bool sCAbPushEnabled, bool sCAbPushBullish, bool sCAbPushBearish, bool sCExPushEnabled, bool sCExPushBullish, bool sCExPushBearish, bool sCAbsorptionBullishDirectionDeltaEnabled, bool sCAbsorptionBullishDeltaNegativeAvgEnabled, bool sCAbsorptionBullishVolumeAvgSellEnabled, bool sCAbsorptionBullishVolumeExtremaSellEnabled, bool sCAbsorptionBullishDirectionBarEnabled, bool sCAbsorptionBullishNeutralRangeCloseEnabled, bool sCAbsorptionBullishNonExtremeBodyEnabled, bool sCAbsorptionBullishNonExtremeSpreadEnabled, bool sCAbsorptionBearishDirectionDeltaEnabled, bool sCAbsorptionBearishDeltaPositiveAvgEnabled, bool sCAbsorptionBearishVolumeAvgBuyEnabled, bool sCAbsorptionBearishVolumeExtremaBuyEnabled, bool sCAbsorptionBearishDirectionBarEnabled, bool sCAbsorptionBearishNeutralRangeCloseEnabled, bool sCAbsorptionBearishNonExtremeBodyEnabled, bool sCAbsorptionBearishNonExtremeSpreadEnabled, bool sCExhaustionBullishConsecutiveBarEnabled, bool sCExhaustionBullishConsecutiveBodyEnabled, bool sCExhaustionBullishConsecutiveSpreadEnabled, bool sCExhaustionBullishConsecutiveVolumeEnabled, bool sCExhaustionBullishConsecutiveVolumeSellEnabled, bool sCExhaustionBullishDeltaNegativeAvgEnabled, bool sCExhaustionBullishVolumeAvgSellEnabled, bool sCExhaustionBearishConsecutiveBarEnabled, bool sCExhaustionBearishConsecutiveBodyEnabled, bool sCExhaustionBearishConsecutiveSpreadEnabled, bool sCExhaustionBearishConsecutiveVolumeEnabled, bool sCExhaustionBearishConsecutiveVolumeBuyEnabled, bool sCExhaustionBearishDeltaPositiveAvgEnabled, bool sCExhaustionBearishVolumeAvgBuyEnabled, bool sCPushBullishVolumeExtremaBuyEnabled, bool sCPushBullishDirectionDeltaEnabled, bool sCPushBullishDeltaPositiveAvgEnabled, bool sCPushBullishVolumeAvgBuyEnabled, bool sCPushBullishDirectionPOCEnabled, bool sCPushBullishNeutralRangePOCEnabled, bool sCPushBullishBodyExtremaEnabled, bool sCPushBullishSpreadExtremaEnabled, bool sCPushBullishDirectionBarEnabled, bool sCPushBullishNeutralRangeCloseEnabled, bool sCPushBullishCompareRangeWickEnabled, bool sCPushBullishCompareRangeBodyEnabled, bool sCPushBullishCompareTickBodyEnabled, bool sCPushBearishVolumeExtremaSellEnabled, bool sCPushBearishDirectionDeltaEnabled, bool sCPushBearishDeltaNegativeAvgEnabled, bool sCPushBearishVolumeAvgSellEnabled, bool sCPushBearishDirectionPOCEnabled, bool sCPushBearishNeutralRangePOCEnabled, bool sCPushBearishBodyExtremaEnabled, bool sCPushBearishSpreadExtremaEnabled, bool sCPushBearishDirectionBarEnabled, bool sCPushBearishNeutralRangeCloseEnabled, bool sCPushBearishCompareRangeWickEnabled, bool sCPushBearishCompareRangeBodyEnabled, bool sCPushBearishCompareTickBodyEnabled, int peakNeighborhood, double rowThresholdMultiplier)
		{
			return DDApexFlowZignal(Input, avgVolPeriod, avgDeltaPeriod, neutralRange, absorptionN, exhaustionN, pushN, minBodyTicks, maxWickPercentBullish, minBodyPercentBullish, maxWickPercentBearish, minBodyPercentBearish, abPushPeriod, exPushPeriod, sCAbsorptionEnabled, sCAbsorptionBullish, sCAbsorptionBearish, sCExhaustionEnabled, sCExhaustionBullish, sCExhaustionBearish, sCPushEnabled, sCPushBullish, sCPushBearish, sCAbPushEnabled, sCAbPushBullish, sCAbPushBearish, sCExPushEnabled, sCExPushBullish, sCExPushBearish, sCAbsorptionBullishDirectionDeltaEnabled, sCAbsorptionBullishDeltaNegativeAvgEnabled, sCAbsorptionBullishVolumeAvgSellEnabled, sCAbsorptionBullishVolumeExtremaSellEnabled, sCAbsorptionBullishDirectionBarEnabled, sCAbsorptionBullishNeutralRangeCloseEnabled, sCAbsorptionBullishNonExtremeBodyEnabled, sCAbsorptionBullishNonExtremeSpreadEnabled, sCAbsorptionBearishDirectionDeltaEnabled, sCAbsorptionBearishDeltaPositiveAvgEnabled, sCAbsorptionBearishVolumeAvgBuyEnabled, sCAbsorptionBearishVolumeExtremaBuyEnabled, sCAbsorptionBearishDirectionBarEnabled, sCAbsorptionBearishNeutralRangeCloseEnabled, sCAbsorptionBearishNonExtremeBodyEnabled, sCAbsorptionBearishNonExtremeSpreadEnabled, sCExhaustionBullishConsecutiveBarEnabled, sCExhaustionBullishConsecutiveBodyEnabled, sCExhaustionBullishConsecutiveSpreadEnabled, sCExhaustionBullishConsecutiveVolumeEnabled, sCExhaustionBullishConsecutiveVolumeSellEnabled, sCExhaustionBullishDeltaNegativeAvgEnabled, sCExhaustionBullishVolumeAvgSellEnabled, sCExhaustionBearishConsecutiveBarEnabled, sCExhaustionBearishConsecutiveBodyEnabled, sCExhaustionBearishConsecutiveSpreadEnabled, sCExhaustionBearishConsecutiveVolumeEnabled, sCExhaustionBearishConsecutiveVolumeBuyEnabled, sCExhaustionBearishDeltaPositiveAvgEnabled, sCExhaustionBearishVolumeAvgBuyEnabled, sCPushBullishVolumeExtremaBuyEnabled, sCPushBullishDirectionDeltaEnabled, sCPushBullishDeltaPositiveAvgEnabled, sCPushBullishVolumeAvgBuyEnabled, sCPushBullishDirectionPOCEnabled, sCPushBullishNeutralRangePOCEnabled, sCPushBullishBodyExtremaEnabled, sCPushBullishSpreadExtremaEnabled, sCPushBullishDirectionBarEnabled, sCPushBullishNeutralRangeCloseEnabled, sCPushBullishCompareRangeWickEnabled, sCPushBullishCompareRangeBodyEnabled, sCPushBullishCompareTickBodyEnabled, sCPushBearishVolumeExtremaSellEnabled, sCPushBearishDirectionDeltaEnabled, sCPushBearishDeltaNegativeAvgEnabled, sCPushBearishVolumeAvgSellEnabled, sCPushBearishDirectionPOCEnabled, sCPushBearishNeutralRangePOCEnabled, sCPushBearishBodyExtremaEnabled, sCPushBearishSpreadExtremaEnabled, sCPushBearishDirectionBarEnabled, sCPushBearishNeutralRangeCloseEnabled, sCPushBearishCompareRangeWickEnabled, sCPushBearishCompareRangeBodyEnabled, sCPushBearishCompareTickBodyEnabled, peakNeighborhood, rowThresholdMultiplier);
		}

		public DimDim.DDApexFlowZignal DDApexFlowZignal(ISeries<double> input, int avgVolPeriod, int avgDeltaPeriod, int neutralRange, int absorptionN, int exhaustionN, int pushN, int minBodyTicks, int maxWickPercentBullish, int minBodyPercentBullish, int maxWickPercentBearish, int minBodyPercentBearish, int abPushPeriod, int exPushPeriod, bool sCAbsorptionEnabled, bool sCAbsorptionBullish, bool sCAbsorptionBearish, bool sCExhaustionEnabled, bool sCExhaustionBullish, bool sCExhaustionBearish, bool sCPushEnabled, bool sCPushBullish, bool sCPushBearish, bool sCAbPushEnabled, bool sCAbPushBullish, bool sCAbPushBearish, bool sCExPushEnabled, bool sCExPushBullish, bool sCExPushBearish, bool sCAbsorptionBullishDirectionDeltaEnabled, bool sCAbsorptionBullishDeltaNegativeAvgEnabled, bool sCAbsorptionBullishVolumeAvgSellEnabled, bool sCAbsorptionBullishVolumeExtremaSellEnabled, bool sCAbsorptionBullishDirectionBarEnabled, bool sCAbsorptionBullishNeutralRangeCloseEnabled, bool sCAbsorptionBullishNonExtremeBodyEnabled, bool sCAbsorptionBullishNonExtremeSpreadEnabled, bool sCAbsorptionBearishDirectionDeltaEnabled, bool sCAbsorptionBearishDeltaPositiveAvgEnabled, bool sCAbsorptionBearishVolumeAvgBuyEnabled, bool sCAbsorptionBearishVolumeExtremaBuyEnabled, bool sCAbsorptionBearishDirectionBarEnabled, bool sCAbsorptionBearishNeutralRangeCloseEnabled, bool sCAbsorptionBearishNonExtremeBodyEnabled, bool sCAbsorptionBearishNonExtremeSpreadEnabled, bool sCExhaustionBullishConsecutiveBarEnabled, bool sCExhaustionBullishConsecutiveBodyEnabled, bool sCExhaustionBullishConsecutiveSpreadEnabled, bool sCExhaustionBullishConsecutiveVolumeEnabled, bool sCExhaustionBullishConsecutiveVolumeSellEnabled, bool sCExhaustionBullishDeltaNegativeAvgEnabled, bool sCExhaustionBullishVolumeAvgSellEnabled, bool sCExhaustionBearishConsecutiveBarEnabled, bool sCExhaustionBearishConsecutiveBodyEnabled, bool sCExhaustionBearishConsecutiveSpreadEnabled, bool sCExhaustionBearishConsecutiveVolumeEnabled, bool sCExhaustionBearishConsecutiveVolumeBuyEnabled, bool sCExhaustionBearishDeltaPositiveAvgEnabled, bool sCExhaustionBearishVolumeAvgBuyEnabled, bool sCPushBullishVolumeExtremaBuyEnabled, bool sCPushBullishDirectionDeltaEnabled, bool sCPushBullishDeltaPositiveAvgEnabled, bool sCPushBullishVolumeAvgBuyEnabled, bool sCPushBullishDirectionPOCEnabled, bool sCPushBullishNeutralRangePOCEnabled, bool sCPushBullishBodyExtremaEnabled, bool sCPushBullishSpreadExtremaEnabled, bool sCPushBullishDirectionBarEnabled, bool sCPushBullishNeutralRangeCloseEnabled, bool sCPushBullishCompareRangeWickEnabled, bool sCPushBullishCompareRangeBodyEnabled, bool sCPushBullishCompareTickBodyEnabled, bool sCPushBearishVolumeExtremaSellEnabled, bool sCPushBearishDirectionDeltaEnabled, bool sCPushBearishDeltaNegativeAvgEnabled, bool sCPushBearishVolumeAvgSellEnabled, bool sCPushBearishDirectionPOCEnabled, bool sCPushBearishNeutralRangePOCEnabled, bool sCPushBearishBodyExtremaEnabled, bool sCPushBearishSpreadExtremaEnabled, bool sCPushBearishDirectionBarEnabled, bool sCPushBearishNeutralRangeCloseEnabled, bool sCPushBearishCompareRangeWickEnabled, bool sCPushBearishCompareRangeBodyEnabled, bool sCPushBearishCompareTickBodyEnabled, int peakNeighborhood, double rowThresholdMultiplier)
		{
			if (cacheDDApexFlowZignal != null)
				for (int idx = 0; idx < cacheDDApexFlowZignal.Length; idx++)
					if (cacheDDApexFlowZignal[idx] != null && cacheDDApexFlowZignal[idx].AvgVolPeriod == avgVolPeriod && cacheDDApexFlowZignal[idx].AvgDeltaPeriod == avgDeltaPeriod && cacheDDApexFlowZignal[idx].NeutralRange == neutralRange && cacheDDApexFlowZignal[idx].AbsorptionN == absorptionN && cacheDDApexFlowZignal[idx].ExhaustionN == exhaustionN && cacheDDApexFlowZignal[idx].PushN == pushN && cacheDDApexFlowZignal[idx].MinBodyTicks == minBodyTicks && cacheDDApexFlowZignal[idx].MaxWickPercentBullish == maxWickPercentBullish && cacheDDApexFlowZignal[idx].MinBodyPercentBullish == minBodyPercentBullish && cacheDDApexFlowZignal[idx].MaxWickPercentBearish == maxWickPercentBearish && cacheDDApexFlowZignal[idx].MinBodyPercentBearish == minBodyPercentBearish && cacheDDApexFlowZignal[idx].AbPushPeriod == abPushPeriod && cacheDDApexFlowZignal[idx].ExPushPeriod == exPushPeriod && cacheDDApexFlowZignal[idx].SCAbsorptionEnabled == sCAbsorptionEnabled && cacheDDApexFlowZignal[idx].SCAbsorptionBullish == sCAbsorptionBullish && cacheDDApexFlowZignal[idx].SCAbsorptionBearish == sCAbsorptionBearish && cacheDDApexFlowZignal[idx].SCExhaustionEnabled == sCExhaustionEnabled && cacheDDApexFlowZignal[idx].SCExhaustionBullish == sCExhaustionBullish && cacheDDApexFlowZignal[idx].SCExhaustionBearish == sCExhaustionBearish && cacheDDApexFlowZignal[idx].SCPushEnabled == sCPushEnabled && cacheDDApexFlowZignal[idx].SCPushBullish == sCPushBullish && cacheDDApexFlowZignal[idx].SCPushBearish == sCPushBearish && cacheDDApexFlowZignal[idx].SCAbPushEnabled == sCAbPushEnabled && cacheDDApexFlowZignal[idx].SCAbPushBullish == sCAbPushBullish && cacheDDApexFlowZignal[idx].SCAbPushBearish == sCAbPushBearish && cacheDDApexFlowZignal[idx].SCExPushEnabled == sCExPushEnabled && cacheDDApexFlowZignal[idx].SCExPushBullish == sCExPushBullish && cacheDDApexFlowZignal[idx].SCExPushBearish == sCExPushBearish && cacheDDApexFlowZignal[idx].SCAbsorptionBullishDirectionDeltaEnabled == sCAbsorptionBullishDirectionDeltaEnabled && cacheDDApexFlowZignal[idx].SCAbsorptionBullishDeltaNegativeAvgEnabled == sCAbsorptionBullishDeltaNegativeAvgEnabled && cacheDDApexFlowZignal[idx].SCAbsorptionBullishVolumeAvgSellEnabled == sCAbsorptionBullishVolumeAvgSellEnabled && cacheDDApexFlowZignal[idx].SCAbsorptionBullishVolumeExtremaSellEnabled == sCAbsorptionBullishVolumeExtremaSellEnabled && cacheDDApexFlowZignal[idx].SCAbsorptionBullishDirectionBarEnabled == sCAbsorptionBullishDirectionBarEnabled && cacheDDApexFlowZignal[idx].SCAbsorptionBullishNeutralRangeCloseEnabled == sCAbsorptionBullishNeutralRangeCloseEnabled && cacheDDApexFlowZignal[idx].SCAbsorptionBullishNonExtremeBodyEnabled == sCAbsorptionBullishNonExtremeBodyEnabled && cacheDDApexFlowZignal[idx].SCAbsorptionBullishNonExtremeSpreadEnabled == sCAbsorptionBullishNonExtremeSpreadEnabled && cacheDDApexFlowZignal[idx].SCAbsorptionBearishDirectionDeltaEnabled == sCAbsorptionBearishDirectionDeltaEnabled && cacheDDApexFlowZignal[idx].SCAbsorptionBearishDeltaPositiveAvgEnabled == sCAbsorptionBearishDeltaPositiveAvgEnabled && cacheDDApexFlowZignal[idx].SCAbsorptionBearishVolumeAvgBuyEnabled == sCAbsorptionBearishVolumeAvgBuyEnabled && cacheDDApexFlowZignal[idx].SCAbsorptionBearishVolumeExtremaBuyEnabled == sCAbsorptionBearishVolumeExtremaBuyEnabled && cacheDDApexFlowZignal[idx].SCAbsorptionBearishDirectionBarEnabled == sCAbsorptionBearishDirectionBarEnabled && cacheDDApexFlowZignal[idx].SCAbsorptionBearishNeutralRangeCloseEnabled == sCAbsorptionBearishNeutralRangeCloseEnabled && cacheDDApexFlowZignal[idx].SCAbsorptionBearishNonExtremeBodyEnabled == sCAbsorptionBearishNonExtremeBodyEnabled && cacheDDApexFlowZignal[idx].SCAbsorptionBearishNonExtremeSpreadEnabled == sCAbsorptionBearishNonExtremeSpreadEnabled && cacheDDApexFlowZignal[idx].SCExhaustionBullishConsecutiveBarEnabled == sCExhaustionBullishConsecutiveBarEnabled && cacheDDApexFlowZignal[idx].SCExhaustionBullishConsecutiveBodyEnabled == sCExhaustionBullishConsecutiveBodyEnabled && cacheDDApexFlowZignal[idx].SCExhaustionBullishConsecutiveSpreadEnabled == sCExhaustionBullishConsecutiveSpreadEnabled && cacheDDApexFlowZignal[idx].SCExhaustionBullishConsecutiveVolumeEnabled == sCExhaustionBullishConsecutiveVolumeEnabled && cacheDDApexFlowZignal[idx].SCExhaustionBullishConsecutiveVolumeSellEnabled == sCExhaustionBullishConsecutiveVolumeSellEnabled && cacheDDApexFlowZignal[idx].SCExhaustionBullishDeltaNegativeAvgEnabled == sCExhaustionBullishDeltaNegativeAvgEnabled && cacheDDApexFlowZignal[idx].SCExhaustionBullishVolumeAvgSellEnabled == sCExhaustionBullishVolumeAvgSellEnabled && cacheDDApexFlowZignal[idx].SCExhaustionBearishConsecutiveBarEnabled == sCExhaustionBearishConsecutiveBarEnabled && cacheDDApexFlowZignal[idx].SCExhaustionBearishConsecutiveBodyEnabled == sCExhaustionBearishConsecutiveBodyEnabled && cacheDDApexFlowZignal[idx].SCExhaustionBearishConsecutiveSpreadEnabled == sCExhaustionBearishConsecutiveSpreadEnabled && cacheDDApexFlowZignal[idx].SCExhaustionBearishConsecutiveVolumeEnabled == sCExhaustionBearishConsecutiveVolumeEnabled && cacheDDApexFlowZignal[idx].SCExhaustionBearishConsecutiveVolumeBuyEnabled == sCExhaustionBearishConsecutiveVolumeBuyEnabled && cacheDDApexFlowZignal[idx].SCExhaustionBearishDeltaPositiveAvgEnabled == sCExhaustionBearishDeltaPositiveAvgEnabled && cacheDDApexFlowZignal[idx].SCExhaustionBearishVolumeAvgBuyEnabled == sCExhaustionBearishVolumeAvgBuyEnabled && cacheDDApexFlowZignal[idx].SCPushBullishVolumeExtremaBuyEnabled == sCPushBullishVolumeExtremaBuyEnabled && cacheDDApexFlowZignal[idx].SCPushBullishDirectionDeltaEnabled == sCPushBullishDirectionDeltaEnabled && cacheDDApexFlowZignal[idx].SCPushBullishDeltaPositiveAvgEnabled == sCPushBullishDeltaPositiveAvgEnabled && cacheDDApexFlowZignal[idx].SCPushBullishVolumeAvgBuyEnabled == sCPushBullishVolumeAvgBuyEnabled && cacheDDApexFlowZignal[idx].SCPushBullishDirectionPOCEnabled == sCPushBullishDirectionPOCEnabled && cacheDDApexFlowZignal[idx].SCPushBullishNeutralRangePOCEnabled == sCPushBullishNeutralRangePOCEnabled && cacheDDApexFlowZignal[idx].SCPushBullishBodyExtremaEnabled == sCPushBullishBodyExtremaEnabled && cacheDDApexFlowZignal[idx].SCPushBullishSpreadExtremaEnabled == sCPushBullishSpreadExtremaEnabled && cacheDDApexFlowZignal[idx].SCPushBullishDirectionBarEnabled == sCPushBullishDirectionBarEnabled && cacheDDApexFlowZignal[idx].SCPushBullishNeutralRangeCloseEnabled == sCPushBullishNeutralRangeCloseEnabled && cacheDDApexFlowZignal[idx].SCPushBullishCompareRangeWickEnabled == sCPushBullishCompareRangeWickEnabled && cacheDDApexFlowZignal[idx].SCPushBullishCompareRangeBodyEnabled == sCPushBullishCompareRangeBodyEnabled && cacheDDApexFlowZignal[idx].SCPushBullishCompareTickBodyEnabled == sCPushBullishCompareTickBodyEnabled && cacheDDApexFlowZignal[idx].SCPushBearishVolumeExtremaSellEnabled == sCPushBearishVolumeExtremaSellEnabled && cacheDDApexFlowZignal[idx].SCPushBearishDirectionDeltaEnabled == sCPushBearishDirectionDeltaEnabled && cacheDDApexFlowZignal[idx].SCPushBearishDeltaNegativeAvgEnabled == sCPushBearishDeltaNegativeAvgEnabled && cacheDDApexFlowZignal[idx].SCPushBearishVolumeAvgSellEnabled == sCPushBearishVolumeAvgSellEnabled && cacheDDApexFlowZignal[idx].SCPushBearishDirectionPOCEnabled == sCPushBearishDirectionPOCEnabled && cacheDDApexFlowZignal[idx].SCPushBearishNeutralRangePOCEnabled == sCPushBearishNeutralRangePOCEnabled && cacheDDApexFlowZignal[idx].SCPushBearishBodyExtremaEnabled == sCPushBearishBodyExtremaEnabled && cacheDDApexFlowZignal[idx].SCPushBearishSpreadExtremaEnabled == sCPushBearishSpreadExtremaEnabled && cacheDDApexFlowZignal[idx].SCPushBearishDirectionBarEnabled == sCPushBearishDirectionBarEnabled && cacheDDApexFlowZignal[idx].SCPushBearishNeutralRangeCloseEnabled == sCPushBearishNeutralRangeCloseEnabled && cacheDDApexFlowZignal[idx].SCPushBearishCompareRangeWickEnabled == sCPushBearishCompareRangeWickEnabled && cacheDDApexFlowZignal[idx].SCPushBearishCompareRangeBodyEnabled == sCPushBearishCompareRangeBodyEnabled && cacheDDApexFlowZignal[idx].SCPushBearishCompareTickBodyEnabled == sCPushBearishCompareTickBodyEnabled && cacheDDApexFlowZignal[idx].PeakNeighborhood == peakNeighborhood && cacheDDApexFlowZignal[idx].RowThresholdMultiplier == rowThresholdMultiplier && cacheDDApexFlowZignal[idx].EqualsInput(input))
						return cacheDDApexFlowZignal[idx];
			return CacheIndicator<DimDim.DDApexFlowZignal>(new DimDim.DDApexFlowZignal(){ AvgVolPeriod = avgVolPeriod, AvgDeltaPeriod = avgDeltaPeriod, NeutralRange = neutralRange, AbsorptionN = absorptionN, ExhaustionN = exhaustionN, PushN = pushN, MinBodyTicks = minBodyTicks, MaxWickPercentBullish = maxWickPercentBullish, MinBodyPercentBullish = minBodyPercentBullish, MaxWickPercentBearish = maxWickPercentBearish, MinBodyPercentBearish = minBodyPercentBearish, AbPushPeriod = abPushPeriod, ExPushPeriod = exPushPeriod, SCAbsorptionEnabled = sCAbsorptionEnabled, SCAbsorptionBullish = sCAbsorptionBullish, SCAbsorptionBearish = sCAbsorptionBearish, SCExhaustionEnabled = sCExhaustionEnabled, SCExhaustionBullish = sCExhaustionBullish, SCExhaustionBearish = sCExhaustionBearish, SCPushEnabled = sCPushEnabled, SCPushBullish = sCPushBullish, SCPushBearish = sCPushBearish, SCAbPushEnabled = sCAbPushEnabled, SCAbPushBullish = sCAbPushBullish, SCAbPushBearish = sCAbPushBearish, SCExPushEnabled = sCExPushEnabled, SCExPushBullish = sCExPushBullish, SCExPushBearish = sCExPushBearish, SCAbsorptionBullishDirectionDeltaEnabled = sCAbsorptionBullishDirectionDeltaEnabled, SCAbsorptionBullishDeltaNegativeAvgEnabled = sCAbsorptionBullishDeltaNegativeAvgEnabled, SCAbsorptionBullishVolumeAvgSellEnabled = sCAbsorptionBullishVolumeAvgSellEnabled, SCAbsorptionBullishVolumeExtremaSellEnabled = sCAbsorptionBullishVolumeExtremaSellEnabled, SCAbsorptionBullishDirectionBarEnabled = sCAbsorptionBullishDirectionBarEnabled, SCAbsorptionBullishNeutralRangeCloseEnabled = sCAbsorptionBullishNeutralRangeCloseEnabled, SCAbsorptionBullishNonExtremeBodyEnabled = sCAbsorptionBullishNonExtremeBodyEnabled, SCAbsorptionBullishNonExtremeSpreadEnabled = sCAbsorptionBullishNonExtremeSpreadEnabled, SCAbsorptionBearishDirectionDeltaEnabled = sCAbsorptionBearishDirectionDeltaEnabled, SCAbsorptionBearishDeltaPositiveAvgEnabled = sCAbsorptionBearishDeltaPositiveAvgEnabled, SCAbsorptionBearishVolumeAvgBuyEnabled = sCAbsorptionBearishVolumeAvgBuyEnabled, SCAbsorptionBearishVolumeExtremaBuyEnabled = sCAbsorptionBearishVolumeExtremaBuyEnabled, SCAbsorptionBearishDirectionBarEnabled = sCAbsorptionBearishDirectionBarEnabled, SCAbsorptionBearishNeutralRangeCloseEnabled = sCAbsorptionBearishNeutralRangeCloseEnabled, SCAbsorptionBearishNonExtremeBodyEnabled = sCAbsorptionBearishNonExtremeBodyEnabled, SCAbsorptionBearishNonExtremeSpreadEnabled = sCAbsorptionBearishNonExtremeSpreadEnabled, SCExhaustionBullishConsecutiveBarEnabled = sCExhaustionBullishConsecutiveBarEnabled, SCExhaustionBullishConsecutiveBodyEnabled = sCExhaustionBullishConsecutiveBodyEnabled, SCExhaustionBullishConsecutiveSpreadEnabled = sCExhaustionBullishConsecutiveSpreadEnabled, SCExhaustionBullishConsecutiveVolumeEnabled = sCExhaustionBullishConsecutiveVolumeEnabled, SCExhaustionBullishConsecutiveVolumeSellEnabled = sCExhaustionBullishConsecutiveVolumeSellEnabled, SCExhaustionBullishDeltaNegativeAvgEnabled = sCExhaustionBullishDeltaNegativeAvgEnabled, SCExhaustionBullishVolumeAvgSellEnabled = sCExhaustionBullishVolumeAvgSellEnabled, SCExhaustionBearishConsecutiveBarEnabled = sCExhaustionBearishConsecutiveBarEnabled, SCExhaustionBearishConsecutiveBodyEnabled = sCExhaustionBearishConsecutiveBodyEnabled, SCExhaustionBearishConsecutiveSpreadEnabled = sCExhaustionBearishConsecutiveSpreadEnabled, SCExhaustionBearishConsecutiveVolumeEnabled = sCExhaustionBearishConsecutiveVolumeEnabled, SCExhaustionBearishConsecutiveVolumeBuyEnabled = sCExhaustionBearishConsecutiveVolumeBuyEnabled, SCExhaustionBearishDeltaPositiveAvgEnabled = sCExhaustionBearishDeltaPositiveAvgEnabled, SCExhaustionBearishVolumeAvgBuyEnabled = sCExhaustionBearishVolumeAvgBuyEnabled, SCPushBullishVolumeExtremaBuyEnabled = sCPushBullishVolumeExtremaBuyEnabled, SCPushBullishDirectionDeltaEnabled = sCPushBullishDirectionDeltaEnabled, SCPushBullishDeltaPositiveAvgEnabled = sCPushBullishDeltaPositiveAvgEnabled, SCPushBullishVolumeAvgBuyEnabled = sCPushBullishVolumeAvgBuyEnabled, SCPushBullishDirectionPOCEnabled = sCPushBullishDirectionPOCEnabled, SCPushBullishNeutralRangePOCEnabled = sCPushBullishNeutralRangePOCEnabled, SCPushBullishBodyExtremaEnabled = sCPushBullishBodyExtremaEnabled, SCPushBullishSpreadExtremaEnabled = sCPushBullishSpreadExtremaEnabled, SCPushBullishDirectionBarEnabled = sCPushBullishDirectionBarEnabled, SCPushBullishNeutralRangeCloseEnabled = sCPushBullishNeutralRangeCloseEnabled, SCPushBullishCompareRangeWickEnabled = sCPushBullishCompareRangeWickEnabled, SCPushBullishCompareRangeBodyEnabled = sCPushBullishCompareRangeBodyEnabled, SCPushBullishCompareTickBodyEnabled = sCPushBullishCompareTickBodyEnabled, SCPushBearishVolumeExtremaSellEnabled = sCPushBearishVolumeExtremaSellEnabled, SCPushBearishDirectionDeltaEnabled = sCPushBearishDirectionDeltaEnabled, SCPushBearishDeltaNegativeAvgEnabled = sCPushBearishDeltaNegativeAvgEnabled, SCPushBearishVolumeAvgSellEnabled = sCPushBearishVolumeAvgSellEnabled, SCPushBearishDirectionPOCEnabled = sCPushBearishDirectionPOCEnabled, SCPushBearishNeutralRangePOCEnabled = sCPushBearishNeutralRangePOCEnabled, SCPushBearishBodyExtremaEnabled = sCPushBearishBodyExtremaEnabled, SCPushBearishSpreadExtremaEnabled = sCPushBearishSpreadExtremaEnabled, SCPushBearishDirectionBarEnabled = sCPushBearishDirectionBarEnabled, SCPushBearishNeutralRangeCloseEnabled = sCPushBearishNeutralRangeCloseEnabled, SCPushBearishCompareRangeWickEnabled = sCPushBearishCompareRangeWickEnabled, SCPushBearishCompareRangeBodyEnabled = sCPushBearishCompareRangeBodyEnabled, SCPushBearishCompareTickBodyEnabled = sCPushBearishCompareTickBodyEnabled, PeakNeighborhood = peakNeighborhood, RowThresholdMultiplier = rowThresholdMultiplier }, input, ref cacheDDApexFlowZignal);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.DimDim.DDApexFlowZignal DDApexFlowZignal(int avgVolPeriod, int avgDeltaPeriod, int neutralRange, int absorptionN, int exhaustionN, int pushN, int minBodyTicks, int maxWickPercentBullish, int minBodyPercentBullish, int maxWickPercentBearish, int minBodyPercentBearish, int abPushPeriod, int exPushPeriod, bool sCAbsorptionEnabled, bool sCAbsorptionBullish, bool sCAbsorptionBearish, bool sCExhaustionEnabled, bool sCExhaustionBullish, bool sCExhaustionBearish, bool sCPushEnabled, bool sCPushBullish, bool sCPushBearish, bool sCAbPushEnabled, bool sCAbPushBullish, bool sCAbPushBearish, bool sCExPushEnabled, bool sCExPushBullish, bool sCExPushBearish, bool sCAbsorptionBullishDirectionDeltaEnabled, bool sCAbsorptionBullishDeltaNegativeAvgEnabled, bool sCAbsorptionBullishVolumeAvgSellEnabled, bool sCAbsorptionBullishVolumeExtremaSellEnabled, bool sCAbsorptionBullishDirectionBarEnabled, bool sCAbsorptionBullishNeutralRangeCloseEnabled, bool sCAbsorptionBullishNonExtremeBodyEnabled, bool sCAbsorptionBullishNonExtremeSpreadEnabled, bool sCAbsorptionBearishDirectionDeltaEnabled, bool sCAbsorptionBearishDeltaPositiveAvgEnabled, bool sCAbsorptionBearishVolumeAvgBuyEnabled, bool sCAbsorptionBearishVolumeExtremaBuyEnabled, bool sCAbsorptionBearishDirectionBarEnabled, bool sCAbsorptionBearishNeutralRangeCloseEnabled, bool sCAbsorptionBearishNonExtremeBodyEnabled, bool sCAbsorptionBearishNonExtremeSpreadEnabled, bool sCExhaustionBullishConsecutiveBarEnabled, bool sCExhaustionBullishConsecutiveBodyEnabled, bool sCExhaustionBullishConsecutiveSpreadEnabled, bool sCExhaustionBullishConsecutiveVolumeEnabled, bool sCExhaustionBullishConsecutiveVolumeSellEnabled, bool sCExhaustionBullishDeltaNegativeAvgEnabled, bool sCExhaustionBullishVolumeAvgSellEnabled, bool sCExhaustionBearishConsecutiveBarEnabled, bool sCExhaustionBearishConsecutiveBodyEnabled, bool sCExhaustionBearishConsecutiveSpreadEnabled, bool sCExhaustionBearishConsecutiveVolumeEnabled, bool sCExhaustionBearishConsecutiveVolumeBuyEnabled, bool sCExhaustionBearishDeltaPositiveAvgEnabled, bool sCExhaustionBearishVolumeAvgBuyEnabled, bool sCPushBullishVolumeExtremaBuyEnabled, bool sCPushBullishDirectionDeltaEnabled, bool sCPushBullishDeltaPositiveAvgEnabled, bool sCPushBullishVolumeAvgBuyEnabled, bool sCPushBullishDirectionPOCEnabled, bool sCPushBullishNeutralRangePOCEnabled, bool sCPushBullishBodyExtremaEnabled, bool sCPushBullishSpreadExtremaEnabled, bool sCPushBullishDirectionBarEnabled, bool sCPushBullishNeutralRangeCloseEnabled, bool sCPushBullishCompareRangeWickEnabled, bool sCPushBullishCompareRangeBodyEnabled, bool sCPushBullishCompareTickBodyEnabled, bool sCPushBearishVolumeExtremaSellEnabled, bool sCPushBearishDirectionDeltaEnabled, bool sCPushBearishDeltaNegativeAvgEnabled, bool sCPushBearishVolumeAvgSellEnabled, bool sCPushBearishDirectionPOCEnabled, bool sCPushBearishNeutralRangePOCEnabled, bool sCPushBearishBodyExtremaEnabled, bool sCPushBearishSpreadExtremaEnabled, bool sCPushBearishDirectionBarEnabled, bool sCPushBearishNeutralRangeCloseEnabled, bool sCPushBearishCompareRangeWickEnabled, bool sCPushBearishCompareRangeBodyEnabled, bool sCPushBearishCompareTickBodyEnabled, int peakNeighborhood, double rowThresholdMultiplier)
		{
			return indicator.DDApexFlowZignal(Input, avgVolPeriod, avgDeltaPeriod, neutralRange, absorptionN, exhaustionN, pushN, minBodyTicks, maxWickPercentBullish, minBodyPercentBullish, maxWickPercentBearish, minBodyPercentBearish, abPushPeriod, exPushPeriod, sCAbsorptionEnabled, sCAbsorptionBullish, sCAbsorptionBearish, sCExhaustionEnabled, sCExhaustionBullish, sCExhaustionBearish, sCPushEnabled, sCPushBullish, sCPushBearish, sCAbPushEnabled, sCAbPushBullish, sCAbPushBearish, sCExPushEnabled, sCExPushBullish, sCExPushBearish, sCAbsorptionBullishDirectionDeltaEnabled, sCAbsorptionBullishDeltaNegativeAvgEnabled, sCAbsorptionBullishVolumeAvgSellEnabled, sCAbsorptionBullishVolumeExtremaSellEnabled, sCAbsorptionBullishDirectionBarEnabled, sCAbsorptionBullishNeutralRangeCloseEnabled, sCAbsorptionBullishNonExtremeBodyEnabled, sCAbsorptionBullishNonExtremeSpreadEnabled, sCAbsorptionBearishDirectionDeltaEnabled, sCAbsorptionBearishDeltaPositiveAvgEnabled, sCAbsorptionBearishVolumeAvgBuyEnabled, sCAbsorptionBearishVolumeExtremaBuyEnabled, sCAbsorptionBearishDirectionBarEnabled, sCAbsorptionBearishNeutralRangeCloseEnabled, sCAbsorptionBearishNonExtremeBodyEnabled, sCAbsorptionBearishNonExtremeSpreadEnabled, sCExhaustionBullishConsecutiveBarEnabled, sCExhaustionBullishConsecutiveBodyEnabled, sCExhaustionBullishConsecutiveSpreadEnabled, sCExhaustionBullishConsecutiveVolumeEnabled, sCExhaustionBullishConsecutiveVolumeSellEnabled, sCExhaustionBullishDeltaNegativeAvgEnabled, sCExhaustionBullishVolumeAvgSellEnabled, sCExhaustionBearishConsecutiveBarEnabled, sCExhaustionBearishConsecutiveBodyEnabled, sCExhaustionBearishConsecutiveSpreadEnabled, sCExhaustionBearishConsecutiveVolumeEnabled, sCExhaustionBearishConsecutiveVolumeBuyEnabled, sCExhaustionBearishDeltaPositiveAvgEnabled, sCExhaustionBearishVolumeAvgBuyEnabled, sCPushBullishVolumeExtremaBuyEnabled, sCPushBullishDirectionDeltaEnabled, sCPushBullishDeltaPositiveAvgEnabled, sCPushBullishVolumeAvgBuyEnabled, sCPushBullishDirectionPOCEnabled, sCPushBullishNeutralRangePOCEnabled, sCPushBullishBodyExtremaEnabled, sCPushBullishSpreadExtremaEnabled, sCPushBullishDirectionBarEnabled, sCPushBullishNeutralRangeCloseEnabled, sCPushBullishCompareRangeWickEnabled, sCPushBullishCompareRangeBodyEnabled, sCPushBullishCompareTickBodyEnabled, sCPushBearishVolumeExtremaSellEnabled, sCPushBearishDirectionDeltaEnabled, sCPushBearishDeltaNegativeAvgEnabled, sCPushBearishVolumeAvgSellEnabled, sCPushBearishDirectionPOCEnabled, sCPushBearishNeutralRangePOCEnabled, sCPushBearishBodyExtremaEnabled, sCPushBearishSpreadExtremaEnabled, sCPushBearishDirectionBarEnabled, sCPushBearishNeutralRangeCloseEnabled, sCPushBearishCompareRangeWickEnabled, sCPushBearishCompareRangeBodyEnabled, sCPushBearishCompareTickBodyEnabled, peakNeighborhood, rowThresholdMultiplier);
		}

		public Indicators.DimDim.DDApexFlowZignal DDApexFlowZignal(ISeries<double> input , int avgVolPeriod, int avgDeltaPeriod, int neutralRange, int absorptionN, int exhaustionN, int pushN, int minBodyTicks, int maxWickPercentBullish, int minBodyPercentBullish, int maxWickPercentBearish, int minBodyPercentBearish, int abPushPeriod, int exPushPeriod, bool sCAbsorptionEnabled, bool sCAbsorptionBullish, bool sCAbsorptionBearish, bool sCExhaustionEnabled, bool sCExhaustionBullish, bool sCExhaustionBearish, bool sCPushEnabled, bool sCPushBullish, bool sCPushBearish, bool sCAbPushEnabled, bool sCAbPushBullish, bool sCAbPushBearish, bool sCExPushEnabled, bool sCExPushBullish, bool sCExPushBearish, bool sCAbsorptionBullishDirectionDeltaEnabled, bool sCAbsorptionBullishDeltaNegativeAvgEnabled, bool sCAbsorptionBullishVolumeAvgSellEnabled, bool sCAbsorptionBullishVolumeExtremaSellEnabled, bool sCAbsorptionBullishDirectionBarEnabled, bool sCAbsorptionBullishNeutralRangeCloseEnabled, bool sCAbsorptionBullishNonExtremeBodyEnabled, bool sCAbsorptionBullishNonExtremeSpreadEnabled, bool sCAbsorptionBearishDirectionDeltaEnabled, bool sCAbsorptionBearishDeltaPositiveAvgEnabled, bool sCAbsorptionBearishVolumeAvgBuyEnabled, bool sCAbsorptionBearishVolumeExtremaBuyEnabled, bool sCAbsorptionBearishDirectionBarEnabled, bool sCAbsorptionBearishNeutralRangeCloseEnabled, bool sCAbsorptionBearishNonExtremeBodyEnabled, bool sCAbsorptionBearishNonExtremeSpreadEnabled, bool sCExhaustionBullishConsecutiveBarEnabled, bool sCExhaustionBullishConsecutiveBodyEnabled, bool sCExhaustionBullishConsecutiveSpreadEnabled, bool sCExhaustionBullishConsecutiveVolumeEnabled, bool sCExhaustionBullishConsecutiveVolumeSellEnabled, bool sCExhaustionBullishDeltaNegativeAvgEnabled, bool sCExhaustionBullishVolumeAvgSellEnabled, bool sCExhaustionBearishConsecutiveBarEnabled, bool sCExhaustionBearishConsecutiveBodyEnabled, bool sCExhaustionBearishConsecutiveSpreadEnabled, bool sCExhaustionBearishConsecutiveVolumeEnabled, bool sCExhaustionBearishConsecutiveVolumeBuyEnabled, bool sCExhaustionBearishDeltaPositiveAvgEnabled, bool sCExhaustionBearishVolumeAvgBuyEnabled, bool sCPushBullishVolumeExtremaBuyEnabled, bool sCPushBullishDirectionDeltaEnabled, bool sCPushBullishDeltaPositiveAvgEnabled, bool sCPushBullishVolumeAvgBuyEnabled, bool sCPushBullishDirectionPOCEnabled, bool sCPushBullishNeutralRangePOCEnabled, bool sCPushBullishBodyExtremaEnabled, bool sCPushBullishSpreadExtremaEnabled, bool sCPushBullishDirectionBarEnabled, bool sCPushBullishNeutralRangeCloseEnabled, bool sCPushBullishCompareRangeWickEnabled, bool sCPushBullishCompareRangeBodyEnabled, bool sCPushBullishCompareTickBodyEnabled, bool sCPushBearishVolumeExtremaSellEnabled, bool sCPushBearishDirectionDeltaEnabled, bool sCPushBearishDeltaNegativeAvgEnabled, bool sCPushBearishVolumeAvgSellEnabled, bool sCPushBearishDirectionPOCEnabled, bool sCPushBearishNeutralRangePOCEnabled, bool sCPushBearishBodyExtremaEnabled, bool sCPushBearishSpreadExtremaEnabled, bool sCPushBearishDirectionBarEnabled, bool sCPushBearishNeutralRangeCloseEnabled, bool sCPushBearishCompareRangeWickEnabled, bool sCPushBearishCompareRangeBodyEnabled, bool sCPushBearishCompareTickBodyEnabled, int peakNeighborhood, double rowThresholdMultiplier)
		{
			return indicator.DDApexFlowZignal(input, avgVolPeriod, avgDeltaPeriod, neutralRange, absorptionN, exhaustionN, pushN, minBodyTicks, maxWickPercentBullish, minBodyPercentBullish, maxWickPercentBearish, minBodyPercentBearish, abPushPeriod, exPushPeriod, sCAbsorptionEnabled, sCAbsorptionBullish, sCAbsorptionBearish, sCExhaustionEnabled, sCExhaustionBullish, sCExhaustionBearish, sCPushEnabled, sCPushBullish, sCPushBearish, sCAbPushEnabled, sCAbPushBullish, sCAbPushBearish, sCExPushEnabled, sCExPushBullish, sCExPushBearish, sCAbsorptionBullishDirectionDeltaEnabled, sCAbsorptionBullishDeltaNegativeAvgEnabled, sCAbsorptionBullishVolumeAvgSellEnabled, sCAbsorptionBullishVolumeExtremaSellEnabled, sCAbsorptionBullishDirectionBarEnabled, sCAbsorptionBullishNeutralRangeCloseEnabled, sCAbsorptionBullishNonExtremeBodyEnabled, sCAbsorptionBullishNonExtremeSpreadEnabled, sCAbsorptionBearishDirectionDeltaEnabled, sCAbsorptionBearishDeltaPositiveAvgEnabled, sCAbsorptionBearishVolumeAvgBuyEnabled, sCAbsorptionBearishVolumeExtremaBuyEnabled, sCAbsorptionBearishDirectionBarEnabled, sCAbsorptionBearishNeutralRangeCloseEnabled, sCAbsorptionBearishNonExtremeBodyEnabled, sCAbsorptionBearishNonExtremeSpreadEnabled, sCExhaustionBullishConsecutiveBarEnabled, sCExhaustionBullishConsecutiveBodyEnabled, sCExhaustionBullishConsecutiveSpreadEnabled, sCExhaustionBullishConsecutiveVolumeEnabled, sCExhaustionBullishConsecutiveVolumeSellEnabled, sCExhaustionBullishDeltaNegativeAvgEnabled, sCExhaustionBullishVolumeAvgSellEnabled, sCExhaustionBearishConsecutiveBarEnabled, sCExhaustionBearishConsecutiveBodyEnabled, sCExhaustionBearishConsecutiveSpreadEnabled, sCExhaustionBearishConsecutiveVolumeEnabled, sCExhaustionBearishConsecutiveVolumeBuyEnabled, sCExhaustionBearishDeltaPositiveAvgEnabled, sCExhaustionBearishVolumeAvgBuyEnabled, sCPushBullishVolumeExtremaBuyEnabled, sCPushBullishDirectionDeltaEnabled, sCPushBullishDeltaPositiveAvgEnabled, sCPushBullishVolumeAvgBuyEnabled, sCPushBullishDirectionPOCEnabled, sCPushBullishNeutralRangePOCEnabled, sCPushBullishBodyExtremaEnabled, sCPushBullishSpreadExtremaEnabled, sCPushBullishDirectionBarEnabled, sCPushBullishNeutralRangeCloseEnabled, sCPushBullishCompareRangeWickEnabled, sCPushBullishCompareRangeBodyEnabled, sCPushBullishCompareTickBodyEnabled, sCPushBearishVolumeExtremaSellEnabled, sCPushBearishDirectionDeltaEnabled, sCPushBearishDeltaNegativeAvgEnabled, sCPushBearishVolumeAvgSellEnabled, sCPushBearishDirectionPOCEnabled, sCPushBearishNeutralRangePOCEnabled, sCPushBearishBodyExtremaEnabled, sCPushBearishSpreadExtremaEnabled, sCPushBearishDirectionBarEnabled, sCPushBearishNeutralRangeCloseEnabled, sCPushBearishCompareRangeWickEnabled, sCPushBearishCompareRangeBodyEnabled, sCPushBearishCompareTickBodyEnabled, peakNeighborhood, rowThresholdMultiplier);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.DimDim.DDApexFlowZignal DDApexFlowZignal(int avgVolPeriod, int avgDeltaPeriod, int neutralRange, int absorptionN, int exhaustionN, int pushN, int minBodyTicks, int maxWickPercentBullish, int minBodyPercentBullish, int maxWickPercentBearish, int minBodyPercentBearish, int abPushPeriod, int exPushPeriod, bool sCAbsorptionEnabled, bool sCAbsorptionBullish, bool sCAbsorptionBearish, bool sCExhaustionEnabled, bool sCExhaustionBullish, bool sCExhaustionBearish, bool sCPushEnabled, bool sCPushBullish, bool sCPushBearish, bool sCAbPushEnabled, bool sCAbPushBullish, bool sCAbPushBearish, bool sCExPushEnabled, bool sCExPushBullish, bool sCExPushBearish, bool sCAbsorptionBullishDirectionDeltaEnabled, bool sCAbsorptionBullishDeltaNegativeAvgEnabled, bool sCAbsorptionBullishVolumeAvgSellEnabled, bool sCAbsorptionBullishVolumeExtremaSellEnabled, bool sCAbsorptionBullishDirectionBarEnabled, bool sCAbsorptionBullishNeutralRangeCloseEnabled, bool sCAbsorptionBullishNonExtremeBodyEnabled, bool sCAbsorptionBullishNonExtremeSpreadEnabled, bool sCAbsorptionBearishDirectionDeltaEnabled, bool sCAbsorptionBearishDeltaPositiveAvgEnabled, bool sCAbsorptionBearishVolumeAvgBuyEnabled, bool sCAbsorptionBearishVolumeExtremaBuyEnabled, bool sCAbsorptionBearishDirectionBarEnabled, bool sCAbsorptionBearishNeutralRangeCloseEnabled, bool sCAbsorptionBearishNonExtremeBodyEnabled, bool sCAbsorptionBearishNonExtremeSpreadEnabled, bool sCExhaustionBullishConsecutiveBarEnabled, bool sCExhaustionBullishConsecutiveBodyEnabled, bool sCExhaustionBullishConsecutiveSpreadEnabled, bool sCExhaustionBullishConsecutiveVolumeEnabled, bool sCExhaustionBullishConsecutiveVolumeSellEnabled, bool sCExhaustionBullishDeltaNegativeAvgEnabled, bool sCExhaustionBullishVolumeAvgSellEnabled, bool sCExhaustionBearishConsecutiveBarEnabled, bool sCExhaustionBearishConsecutiveBodyEnabled, bool sCExhaustionBearishConsecutiveSpreadEnabled, bool sCExhaustionBearishConsecutiveVolumeEnabled, bool sCExhaustionBearishConsecutiveVolumeBuyEnabled, bool sCExhaustionBearishDeltaPositiveAvgEnabled, bool sCExhaustionBearishVolumeAvgBuyEnabled, bool sCPushBullishVolumeExtremaBuyEnabled, bool sCPushBullishDirectionDeltaEnabled, bool sCPushBullishDeltaPositiveAvgEnabled, bool sCPushBullishVolumeAvgBuyEnabled, bool sCPushBullishDirectionPOCEnabled, bool sCPushBullishNeutralRangePOCEnabled, bool sCPushBullishBodyExtremaEnabled, bool sCPushBullishSpreadExtremaEnabled, bool sCPushBullishDirectionBarEnabled, bool sCPushBullishNeutralRangeCloseEnabled, bool sCPushBullishCompareRangeWickEnabled, bool sCPushBullishCompareRangeBodyEnabled, bool sCPushBullishCompareTickBodyEnabled, bool sCPushBearishVolumeExtremaSellEnabled, bool sCPushBearishDirectionDeltaEnabled, bool sCPushBearishDeltaNegativeAvgEnabled, bool sCPushBearishVolumeAvgSellEnabled, bool sCPushBearishDirectionPOCEnabled, bool sCPushBearishNeutralRangePOCEnabled, bool sCPushBearishBodyExtremaEnabled, bool sCPushBearishSpreadExtremaEnabled, bool sCPushBearishDirectionBarEnabled, bool sCPushBearishNeutralRangeCloseEnabled, bool sCPushBearishCompareRangeWickEnabled, bool sCPushBearishCompareRangeBodyEnabled, bool sCPushBearishCompareTickBodyEnabled, int peakNeighborhood, double rowThresholdMultiplier)
		{
			return indicator.DDApexFlowZignal(Input, avgVolPeriod, avgDeltaPeriod, neutralRange, absorptionN, exhaustionN, pushN, minBodyTicks, maxWickPercentBullish, minBodyPercentBullish, maxWickPercentBearish, minBodyPercentBearish, abPushPeriod, exPushPeriod, sCAbsorptionEnabled, sCAbsorptionBullish, sCAbsorptionBearish, sCExhaustionEnabled, sCExhaustionBullish, sCExhaustionBearish, sCPushEnabled, sCPushBullish, sCPushBearish, sCAbPushEnabled, sCAbPushBullish, sCAbPushBearish, sCExPushEnabled, sCExPushBullish, sCExPushBearish, sCAbsorptionBullishDirectionDeltaEnabled, sCAbsorptionBullishDeltaNegativeAvgEnabled, sCAbsorptionBullishVolumeAvgSellEnabled, sCAbsorptionBullishVolumeExtremaSellEnabled, sCAbsorptionBullishDirectionBarEnabled, sCAbsorptionBullishNeutralRangeCloseEnabled, sCAbsorptionBullishNonExtremeBodyEnabled, sCAbsorptionBullishNonExtremeSpreadEnabled, sCAbsorptionBearishDirectionDeltaEnabled, sCAbsorptionBearishDeltaPositiveAvgEnabled, sCAbsorptionBearishVolumeAvgBuyEnabled, sCAbsorptionBearishVolumeExtremaBuyEnabled, sCAbsorptionBearishDirectionBarEnabled, sCAbsorptionBearishNeutralRangeCloseEnabled, sCAbsorptionBearishNonExtremeBodyEnabled, sCAbsorptionBearishNonExtremeSpreadEnabled, sCExhaustionBullishConsecutiveBarEnabled, sCExhaustionBullishConsecutiveBodyEnabled, sCExhaustionBullishConsecutiveSpreadEnabled, sCExhaustionBullishConsecutiveVolumeEnabled, sCExhaustionBullishConsecutiveVolumeSellEnabled, sCExhaustionBullishDeltaNegativeAvgEnabled, sCExhaustionBullishVolumeAvgSellEnabled, sCExhaustionBearishConsecutiveBarEnabled, sCExhaustionBearishConsecutiveBodyEnabled, sCExhaustionBearishConsecutiveSpreadEnabled, sCExhaustionBearishConsecutiveVolumeEnabled, sCExhaustionBearishConsecutiveVolumeBuyEnabled, sCExhaustionBearishDeltaPositiveAvgEnabled, sCExhaustionBearishVolumeAvgBuyEnabled, sCPushBullishVolumeExtremaBuyEnabled, sCPushBullishDirectionDeltaEnabled, sCPushBullishDeltaPositiveAvgEnabled, sCPushBullishVolumeAvgBuyEnabled, sCPushBullishDirectionPOCEnabled, sCPushBullishNeutralRangePOCEnabled, sCPushBullishBodyExtremaEnabled, sCPushBullishSpreadExtremaEnabled, sCPushBullishDirectionBarEnabled, sCPushBullishNeutralRangeCloseEnabled, sCPushBullishCompareRangeWickEnabled, sCPushBullishCompareRangeBodyEnabled, sCPushBullishCompareTickBodyEnabled, sCPushBearishVolumeExtremaSellEnabled, sCPushBearishDirectionDeltaEnabled, sCPushBearishDeltaNegativeAvgEnabled, sCPushBearishVolumeAvgSellEnabled, sCPushBearishDirectionPOCEnabled, sCPushBearishNeutralRangePOCEnabled, sCPushBearishBodyExtremaEnabled, sCPushBearishSpreadExtremaEnabled, sCPushBearishDirectionBarEnabled, sCPushBearishNeutralRangeCloseEnabled, sCPushBearishCompareRangeWickEnabled, sCPushBearishCompareRangeBodyEnabled, sCPushBearishCompareTickBodyEnabled, peakNeighborhood, rowThresholdMultiplier);
		}

		public Indicators.DimDim.DDApexFlowZignal DDApexFlowZignal(ISeries<double> input , int avgVolPeriod, int avgDeltaPeriod, int neutralRange, int absorptionN, int exhaustionN, int pushN, int minBodyTicks, int maxWickPercentBullish, int minBodyPercentBullish, int maxWickPercentBearish, int minBodyPercentBearish, int abPushPeriod, int exPushPeriod, bool sCAbsorptionEnabled, bool sCAbsorptionBullish, bool sCAbsorptionBearish, bool sCExhaustionEnabled, bool sCExhaustionBullish, bool sCExhaustionBearish, bool sCPushEnabled, bool sCPushBullish, bool sCPushBearish, bool sCAbPushEnabled, bool sCAbPushBullish, bool sCAbPushBearish, bool sCExPushEnabled, bool sCExPushBullish, bool sCExPushBearish, bool sCAbsorptionBullishDirectionDeltaEnabled, bool sCAbsorptionBullishDeltaNegativeAvgEnabled, bool sCAbsorptionBullishVolumeAvgSellEnabled, bool sCAbsorptionBullishVolumeExtremaSellEnabled, bool sCAbsorptionBullishDirectionBarEnabled, bool sCAbsorptionBullishNeutralRangeCloseEnabled, bool sCAbsorptionBullishNonExtremeBodyEnabled, bool sCAbsorptionBullishNonExtremeSpreadEnabled, bool sCAbsorptionBearishDirectionDeltaEnabled, bool sCAbsorptionBearishDeltaPositiveAvgEnabled, bool sCAbsorptionBearishVolumeAvgBuyEnabled, bool sCAbsorptionBearishVolumeExtremaBuyEnabled, bool sCAbsorptionBearishDirectionBarEnabled, bool sCAbsorptionBearishNeutralRangeCloseEnabled, bool sCAbsorptionBearishNonExtremeBodyEnabled, bool sCAbsorptionBearishNonExtremeSpreadEnabled, bool sCExhaustionBullishConsecutiveBarEnabled, bool sCExhaustionBullishConsecutiveBodyEnabled, bool sCExhaustionBullishConsecutiveSpreadEnabled, bool sCExhaustionBullishConsecutiveVolumeEnabled, bool sCExhaustionBullishConsecutiveVolumeSellEnabled, bool sCExhaustionBullishDeltaNegativeAvgEnabled, bool sCExhaustionBullishVolumeAvgSellEnabled, bool sCExhaustionBearishConsecutiveBarEnabled, bool sCExhaustionBearishConsecutiveBodyEnabled, bool sCExhaustionBearishConsecutiveSpreadEnabled, bool sCExhaustionBearishConsecutiveVolumeEnabled, bool sCExhaustionBearishConsecutiveVolumeBuyEnabled, bool sCExhaustionBearishDeltaPositiveAvgEnabled, bool sCExhaustionBearishVolumeAvgBuyEnabled, bool sCPushBullishVolumeExtremaBuyEnabled, bool sCPushBullishDirectionDeltaEnabled, bool sCPushBullishDeltaPositiveAvgEnabled, bool sCPushBullishVolumeAvgBuyEnabled, bool sCPushBullishDirectionPOCEnabled, bool sCPushBullishNeutralRangePOCEnabled, bool sCPushBullishBodyExtremaEnabled, bool sCPushBullishSpreadExtremaEnabled, bool sCPushBullishDirectionBarEnabled, bool sCPushBullishNeutralRangeCloseEnabled, bool sCPushBullishCompareRangeWickEnabled, bool sCPushBullishCompareRangeBodyEnabled, bool sCPushBullishCompareTickBodyEnabled, bool sCPushBearishVolumeExtremaSellEnabled, bool sCPushBearishDirectionDeltaEnabled, bool sCPushBearishDeltaNegativeAvgEnabled, bool sCPushBearishVolumeAvgSellEnabled, bool sCPushBearishDirectionPOCEnabled, bool sCPushBearishNeutralRangePOCEnabled, bool sCPushBearishBodyExtremaEnabled, bool sCPushBearishSpreadExtremaEnabled, bool sCPushBearishDirectionBarEnabled, bool sCPushBearishNeutralRangeCloseEnabled, bool sCPushBearishCompareRangeWickEnabled, bool sCPushBearishCompareRangeBodyEnabled, bool sCPushBearishCompareTickBodyEnabled, int peakNeighborhood, double rowThresholdMultiplier)
		{
			return indicator.DDApexFlowZignal(input, avgVolPeriod, avgDeltaPeriod, neutralRange, absorptionN, exhaustionN, pushN, minBodyTicks, maxWickPercentBullish, minBodyPercentBullish, maxWickPercentBearish, minBodyPercentBearish, abPushPeriod, exPushPeriod, sCAbsorptionEnabled, sCAbsorptionBullish, sCAbsorptionBearish, sCExhaustionEnabled, sCExhaustionBullish, sCExhaustionBearish, sCPushEnabled, sCPushBullish, sCPushBearish, sCAbPushEnabled, sCAbPushBullish, sCAbPushBearish, sCExPushEnabled, sCExPushBullish, sCExPushBearish, sCAbsorptionBullishDirectionDeltaEnabled, sCAbsorptionBullishDeltaNegativeAvgEnabled, sCAbsorptionBullishVolumeAvgSellEnabled, sCAbsorptionBullishVolumeExtremaSellEnabled, sCAbsorptionBullishDirectionBarEnabled, sCAbsorptionBullishNeutralRangeCloseEnabled, sCAbsorptionBullishNonExtremeBodyEnabled, sCAbsorptionBullishNonExtremeSpreadEnabled, sCAbsorptionBearishDirectionDeltaEnabled, sCAbsorptionBearishDeltaPositiveAvgEnabled, sCAbsorptionBearishVolumeAvgBuyEnabled, sCAbsorptionBearishVolumeExtremaBuyEnabled, sCAbsorptionBearishDirectionBarEnabled, sCAbsorptionBearishNeutralRangeCloseEnabled, sCAbsorptionBearishNonExtremeBodyEnabled, sCAbsorptionBearishNonExtremeSpreadEnabled, sCExhaustionBullishConsecutiveBarEnabled, sCExhaustionBullishConsecutiveBodyEnabled, sCExhaustionBullishConsecutiveSpreadEnabled, sCExhaustionBullishConsecutiveVolumeEnabled, sCExhaustionBullishConsecutiveVolumeSellEnabled, sCExhaustionBullishDeltaNegativeAvgEnabled, sCExhaustionBullishVolumeAvgSellEnabled, sCExhaustionBearishConsecutiveBarEnabled, sCExhaustionBearishConsecutiveBodyEnabled, sCExhaustionBearishConsecutiveSpreadEnabled, sCExhaustionBearishConsecutiveVolumeEnabled, sCExhaustionBearishConsecutiveVolumeBuyEnabled, sCExhaustionBearishDeltaPositiveAvgEnabled, sCExhaustionBearishVolumeAvgBuyEnabled, sCPushBullishVolumeExtremaBuyEnabled, sCPushBullishDirectionDeltaEnabled, sCPushBullishDeltaPositiveAvgEnabled, sCPushBullishVolumeAvgBuyEnabled, sCPushBullishDirectionPOCEnabled, sCPushBullishNeutralRangePOCEnabled, sCPushBullishBodyExtremaEnabled, sCPushBullishSpreadExtremaEnabled, sCPushBullishDirectionBarEnabled, sCPushBullishNeutralRangeCloseEnabled, sCPushBullishCompareRangeWickEnabled, sCPushBullishCompareRangeBodyEnabled, sCPushBullishCompareTickBodyEnabled, sCPushBearishVolumeExtremaSellEnabled, sCPushBearishDirectionDeltaEnabled, sCPushBearishDeltaNegativeAvgEnabled, sCPushBearishVolumeAvgSellEnabled, sCPushBearishDirectionPOCEnabled, sCPushBearishNeutralRangePOCEnabled, sCPushBearishBodyExtremaEnabled, sCPushBearishSpreadExtremaEnabled, sCPushBearishDirectionBarEnabled, sCPushBearishNeutralRangeCloseEnabled, sCPushBearishCompareRangeWickEnabled, sCPushBearishCompareRangeBodyEnabled, sCPushBearishCompareTickBodyEnabled, peakNeighborhood, rowThresholdMultiplier);
		}
	}
}

#endregion
