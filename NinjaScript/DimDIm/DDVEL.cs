using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
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
namespace NinjaTrader.NinjaScript.Indicators.DimDim
{
	[CategoryOrder("Toggle", 1000050)]
	[CategoryOrder("Gradient", 1000030)]
	[CategoryOrder("Special", 1000060)]
	[CategoryOrder("Critical", 1000070)]
	[CategoryOrder("Developer", 0)]
	[CategoryOrder("Graphics", 1000020)]
	[CategoryOrder("Alerts", 1000040)]
	[CategoryOrder("General", 1000010)]
	public class DDVEL : Indicator
	{
		[Display(Name = "Condition: Trend Start", Order = 0, GroupName = "Alerts")]
		public bool ConditionTrendStart
		{
			[CompilerGenerated]
			get
			{
				return this._conditionTrendStart;
			}
			[CompilerGenerated]
			set
			{
				this._conditionTrendStart = value;
			}
		}
		[Display(Name = "Condition: Reversal", Order = 2, GroupName = "Alerts")]
		public bool ConditionReversal
		{
			
			get
			{
				return this._conditionReversal;
			}
			set
			{
				this._conditionReversal = value;
			}
		}
		[Display(Name = "Condition: Early", Order = 4, GroupName = "Alerts")]
		public bool ConditionEarly
		{
			get
			{
				return this._conditionEarly;
			}
			set
			{
				this._conditionEarly = value;
			}
		}
		[Display(Name = "Condition: Deep", Order = 6, GroupName = "Alerts")]
		public bool ConditionDeep
		{
			get
			{
				return this._conditionDeep;
			}
			set
			{
				this._conditionDeep = value;
			}
		}
		[Display(Name = "Marker: Enabled", Order = 50, GroupName = "Alerts")]
		public bool MarkerEnabled
		{
			get
			{
				return this._markerEnabled;
			}
			set
			{
				this._markerEnabled = value;
			}
		}
		[Display(Name = "Marker: Rendering Method", Order = 52, GroupName = "Alerts", Description = "\"Custom\" rendering provides a better appearance, and is therefore recommended in most cases.\nIf your computer is weak or slow, please switch to \"Builtin\" rendering.")]
		public DDVEL_RenderingMethod MarkerRenderingMethod
		{
			get
			{
				return this._markerRenderingMethod;
			}
			set
			{
				this._markerRenderingMethod = value;
			}
		}
		[XmlIgnore]
		[Display(Name = "Marker: Color Bullish", Order = 54, GroupName = "Alerts")]
		public Brush MarkerBrushBullish
		{
			get
			{
				return this._markerBrushBullish;
			}
			set
			{
				this._markerBrushBullish = value;
			}
		}
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
		[Display(Name = "Marker: Color Bearish", Order = 55, GroupName = "Alerts")]
		public Brush MarkerBrushBearish
		{
			get
			{
				return this._markerBrushBearish;
			}
			set
			{
				this._markerBrushBearish = value;
			}
		}
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
		[Display(Name = "Marker: String Uptrend Start", Order = 56, GroupName = "Alerts")]
		public string MarkerStringUptrendStart
		{
			get
			{
				return this._markerStringUptrendStart;
			}
			set
			{
				this._markerStringUptrendStart = value;
			}
		}
		[Display(Name = "Marker: String Downtrend Start", Order = 57, GroupName = "Alerts")]
		public string MarkerStringDowntrendStart
		{
			get
			{
				return this._markerStringDowntrendStart;
			}
			set
			{
				this._markerStringDowntrendStart = value;
			}
		}
		[Display(Name = "Marker: String Reversal Bullish", Order = 58, GroupName = "Alerts")]
		public string MarkerStringReversalBullish
		{
			get
			{
				return this._markerStringReversalBullish;
			}
			set
			{
				this._markerStringReversalBullish = value;
			}
		}
		[Display(Name = "Marker: String Reversal Bearish", Order = 60, GroupName = "Alerts")]
		public string MarkerStringReversalBearish
		{
			get
			{
				return this._markerStringReversalBearish;
			}
			set
			{
				this._markerStringReversalBearish = value;
			}
		}
		[Display(Name = "Marker: String Early Bullish", Order = 62, GroupName = "Alerts")]
		public string MarkerStringEarlyBullish
		{
			get
			{
				return this._markerStringEarlyBullish;
			}
			set
			{
				this._markerStringEarlyBullish = value;
			}
		}
		[Display(Name = "Marker: String Early Bearish", Order = 64, GroupName = "Alerts")]
		public string MarkerStringEarlyBearish
		{
			get
			{
				return this._markerStringEarlyBearish;
			}
			set
			{
				this._markerStringEarlyBearish = value;
			}
		}
		[Display(Name = "Marker: String Deep Bullish", Order = 66, GroupName = "Alerts")]
		public string MarkerStringDeepBullish
		{
			get
			{
				return this._markerStringDeepBullish;
			}
			set
			{
				this._markerStringDeepBullish = value;
			}
		}
		[Display(Name = "Marker: String Deep Bearish", Order = 68, GroupName = "Alerts")]
		public string MarkerStringDeepBearish
		{
			get
			{
				return this._markerStringDeepBearish;
			}
			set
			{
				this._markerStringDeepBearish = value;
			}
		}
		[Display(Name = "Marker: Font", Order = 70, GroupName = "Alerts")]
		public SimpleFont MarkerFont
		{
			get
			{
				return this._markerFont;
			}
			set
			{
				this._markerFont = value;
			}
		}
		[Display(Name = "Marker: Offset", Order = 72, GroupName = "Alerts")]
		public int MarkerOffset
		{
			get
			{
				return this._markerOffset;
			}
			set
			{
				this._markerOffset = value;
			}
		}
		[Display(Name = "Alert Blocking (Seconds)", Order = 90, GroupName = "Alerts", Description = "The minimum interval between 2 consecutive alerts")]
		[Range(0, 2147483647)]
		public int AlertBlockingSeconds
		{
			get
			{
				return this._alertBlockingSeconds;
			}
			set
			{
				this._alertBlockingSeconds = value;
			}
		}
		[Display(Name = "Logo: Enabled", Order = 20, GroupName = "Developer")]
		public bool LogoEnabled
		{
			get
			{
				return this._logoEnabled;
			}
			set
			{
				this._logoEnabled = value;
			}
		}
		[Display(Name = "Instruction: Enabled", Order = 30, GroupName = "Developer")]
		public bool InstructionEnabled
		{
			get
			{
				return this._instructionEnabled;
			}
			set
			{
				this._instructionEnabled = value;
			}
		}
		[Display(Name = "Screen DPI", Order = 100, GroupName = "General")]
		[Range(99, 500)]
		public int ScreenDPI
		{
			get
			{
				return this._screenDpi;
			}
			set
			{
				this._screenDpi = value;
			}
		}
		[Display(Name = "Plot: Enabled", Order = 0, GroupName = "Graphics")]
		public bool PlotEnabled
		{
			get
			{
				return this._plotEnabled;
			}
			set
			{
				this._plotEnabled = value;
			}
		}
		[Display(Name = "Plot: Uptrend", Order = 1, GroupName = "Graphics")]
		[XmlIgnore]
		public Brush PlotUptrend
		{
			get
			{
				return this._plotUptrend;
			}
			set
			{
				this._plotUptrend = value;
			}
		}
		[Browsable(false)]
		public string PlotUptrend_Serialize
		{
			get
			{
				return Serialize.BrushToString(this.PlotUptrend);
			}
			set
			{
				this.PlotUptrend = Serialize.StringToBrush(value);
			}
		}
		[Display(Name = "Plot: Downtrend", Order = 2, GroupName = "Graphics")]
		[XmlIgnore]
		public Brush PlotDowntrend
		{
			get
			{
				return this._plotDowntrend;
			}
			set
			{
				this._plotDowntrend = value;
			}
		}
		[Browsable(false)]
		public string PlotDowntrend_Serialize
		{
			get
			{
				return Serialize.BrushToString(this.PlotDowntrend);
			}
			set
			{
				this.PlotDowntrend = Serialize.StringToBrush(value);
			}
		}
		[Display(Name = "Cloud: Enabled", Order = 50, GroupName = "Graphics")]
		public bool CloudEnabled
		{
			get
			{
				return this._cloudEnabled;
			}
			set
			{
				this._cloudEnabled = value;
			}
		}
		[Display(Name = "Cloud: Upper Near", Order = 52, GroupName = "Graphics")]
		[XmlIgnore]
		public Brush CloudUpperNear
		{
			get
			{
				return this._cloudUpperNear;
			}
			set
			{
				this._cloudUpperNear = value;
			}
		}
		[Browsable(false)]
		public string CloudUpperNear_Serialize
		{
			get
			{
				return Serialize.BrushToString(this.CloudUpperNear);
			}
			set
			{
				this.CloudUpperNear = Serialize.StringToBrush(value);
			}
		}
		[Display(Name = "Cloud: Upper Far", Order = 53, GroupName = "Graphics")]
		[XmlIgnore]
		public Brush CloudUpperFar
		{
			get
			{
				return this._cloudUpperFar;
			}
			set
			{
				this._cloudUpperFar = value;
			}
		}
		[Browsable(false)]
		public string CloudUpperFar_Serialize
		{
			get
			{
				return Serialize.BrushToString(this.CloudUpperFar);
			}
			set
			{
				this.CloudUpperFar = Serialize.StringToBrush(value);
			}
		}
		[Display(Name = "Cloud: Lower Near", Order = 54, GroupName = "Graphics")]
		[XmlIgnore]
		public Brush CloudLowerNear
		{
			get
			{
				return this._cloudLowerNear;
			}
			set
			{
				this._cloudLowerNear = value;
			}
		}
		[Browsable(false)]
		public string CloudLowerNear_Serialize
		{
			get
			{
				return Serialize.BrushToString(this.CloudLowerNear);
			}
			set
			{
				this.CloudLowerNear = Serialize.StringToBrush(value);
			}
		}
		[XmlIgnore]
		[Display(Name = "Cloud: Lower Far", Order = 55, GroupName = "Graphics")]
		public Brush CloudLowerFar
		{
			get
			{
				return this._cloudLowerFar;
			}
			set
			{
				this._cloudLowerFar = value;
			}
		}
		[Browsable(false)]
		public string CloudLowerFar_Serialize
		{
			get
			{
				return Serialize.BrushToString(this.CloudLowerFar);
			}
			set
			{
				this.CloudLowerFar = Serialize.StringToBrush(value);
			}
		}
		[Display(Name = "Cloud: Opacity", Order = 56, GroupName = "Graphics")]
		[Range(0, 100)]
		public int CloudOpacity
		{
			get
			{
				return this._cloudOpacity;
			}
			set
			{
				this._cloudOpacity = value;
			}
		}
		[Range(2, 50)]
		[Display(Name = "Cloud: Quantity Per Side", Order = 57, GroupName = "Graphics")]
		public int CloudQuantityPerSide
		{
			get
			{
				return this._cloudQuantityPerSide;
			}
			set
			{
				this._cloudQuantityPerSide = value;
			}
		}
		[Display(Name = "Cloud: Offset (StdDev)", Order = 58, GroupName = "Graphics")]
		[Range(0.01, 1.7976931348623157E+308)]
		public double CloudOffset
		{
			get
			{
				return this._cloudOffset;
			}
			set
			{
				this._cloudOffset = value;
			}
		}
		[Display(Name = "Info: Delta Positive", Order = 74, GroupName = "Graphics")]
		[XmlIgnore]
		public Brush InfoDeltaPositive
		{
			get
			{
				return this._infoDeltaPositive;
			}
			set
			{
				this._infoDeltaPositive = value;
			}
		}
		[Browsable(false)]
		public string InfoDeltaPositive_Serialize
		{
			get
			{
				return Serialize.BrushToString(this.InfoDeltaPositive);
			}
			set
			{
				this.InfoDeltaPositive = Serialize.StringToBrush(value);
			}
		}
		[XmlIgnore]
		[Display(Name = "Info: Delta Negative", Order = 76, GroupName = "Graphics")]
		public Brush InfoDeltaNegative
		{
			get
			{
				return this._infoDeltaNegative;
			}
			set
			{
				this._infoDeltaNegative = value;
			}
		}
		[Browsable(false)]
		public string InfoDeltaNegative_Serialize
		{
			get
			{
				return Serialize.BrushToString(this.InfoDeltaNegative);
			}
			set
			{
				this.InfoDeltaNegative = Serialize.StringToBrush(value);
			}
		}
		[Display(Name = "Info: Delta Zero", Order = 78, GroupName = "Graphics")]
		[XmlIgnore]
		public Brush InfoDeltaZero
		{
			get
			{
				return this._infoDeltaZero;
			}
			set
			{
				this._infoDeltaZero = value;
			}
		}
		[Browsable(false)]
		public string InfoDeltaZero_Serialize
		{
			get
			{
				return Serialize.BrushToString(this.InfoDeltaZero);
			}
			set
			{
				this.InfoDeltaZero = Serialize.StringToBrush(value);
			}
		}
		[Range(0, 100)]
		[Display(Name = "Info: Delta Opacity", Order = 80, GroupName = "Graphics")]
		public int InfoDeltaOpacity
		{
			get
			{
				return this._infoDeltaOpacity;
			}
			set
			{
				this._infoDeltaOpacity = value;
			}
		}
		[Display(Name = "Info: Number Font", Order = 82, GroupName = "Graphics")]
		public SimpleFont InfoNumberFont
		{
			get
			{
				return this._infoNumberFont;
			}
			set
			{
				this._infoNumberFont = value;
			}
		}
		[Display(Name = "Info: Display Period", Order = 84, GroupName = "Graphics")]
		[Range(1, 2147483647)]
		public int InfoDisplayPeriod
		{
			get
			{
				return this._infoDisplayPeriod;
			}
			set
			{
				this._infoDisplayPeriod = value;
			}
		}
		[Range(0, 2147483647)]
		[Display(Name = "Info: Margin", Order = 86, GroupName = "Graphics")]
		public int InfoMargin
		{
			get
			{
				return this._infoMargin;
			}
			set
			{
				this._infoMargin = value;
			}
		}
		[Display(Name = "Swing Point: Enabled", Order = 100, GroupName = "Graphics")]
		public bool SwingPointEnabled
		{
			get
			{
				return this._swingPointEnabled;
			}
			set
			{
				this._swingPointEnabled = value;
			}
		}
		[XmlIgnore]
		[Display(Name = "Swing Point: Top", Order = 101, GroupName = "Graphics")]
		public Brush SwingPointTop
		{
			get
			{
				return this._swingPointTop;
			}
			set
			{
				this._swingPointTop = value;
			}
		}
		[Browsable(false)]
		public string SwingPointTop_Serialize
		{
			get
			{
				return Serialize.BrushToString(this.SwingPointTop);
			}
			set
			{
				this.SwingPointTop = Serialize.StringToBrush(value);
			}
		}
		[XmlIgnore]
		[Display(Name = "Swing Point: Bottom", Order = 102, GroupName = "Graphics")]
		public Brush SwingPointBottom
		{
			get
			{
				return this._swingPointBottom;
			}
			set
			{
				this._swingPointBottom = value;
			}
		}
		[Browsable(false)]
		public string SwingPointBottom_Serialize
		{
			get
			{
				return Serialize.BrushToString(this.SwingPointBottom);
			}
			set
			{
				this.SwingPointBottom = Serialize.StringToBrush(value);
			}
		}
		[Display(Name = "Line: Enabled", Order = 130, GroupName = "Graphics")]
		public bool LineEnabled
		{
			get
			{
				return this._lineEnabled;
			}
			set
			{
				this._lineEnabled = value;
			}
		}
		[XmlIgnore]
		[Display(Name = "Line: Color Up", Order = 131, GroupName = "Graphics")]
		public Brush LineColorUp
		{
			get
			{
				return this._lineColorUp;
			}
			set
			{
				this._lineColorUp = value;
			}
		}
		[Browsable(false)]
		public string LineColorUp_Serialize
		{
			get
			{
				return Serialize.BrushToString(this.LineColorUp);
			}
			set
			{
				this.LineColorUp = Serialize.StringToBrush(value);
			}
		}
		[XmlIgnore]
		[Display(Name = "Line: Color Down", Order = 132, GroupName = "Graphics")]
		public Brush LineColorDown
		{
			get
			{
				return this._lineColorDown;
			}
			set
			{
				this._lineColorDown = value;
			}
		}
		[Browsable(false)]
		public string LineColorDown_Serialize
		{
			get
			{
				return Serialize.BrushToString(this.LineColorDown);
			}
			set
			{
				this.LineColorDown = Serialize.StringToBrush(value);
			}
		}
		[Display(Name = "Line: Opacity", Order = 133, GroupName = "Graphics")]
		[Range(0, 100)]
		public int LineOpacity
		{
			get
			{
				return this._lineOpacity;
			}
			set
			{
				this._lineOpacity = value;
			}
		}
		[Display(Name = "Line: Style Finished", Order = 134, GroupName = "Graphics")]
		public Stroke LineStyleFinished
		{
			get
			{
				return this._lineStyleFinished;
			}
			set
			{
				this._lineStyleFinished = value;
			}
		}
		[Display(Name = "Line: Style Developing", Order = 135, GroupName = "Graphics")]
		public Stroke LineStyleDeveloping
		{
			get
			{
				return this._lineStyleDeveloping;
			}
			set
			{
				this._lineStyleDeveloping = value;
			}
		}
		[Display(Name = "Smart Line Thickness: Enabled", Order = 150, GroupName = "Graphics")]
		public bool SmartLineThicknessEnabled
		{
			get
			{
				return this._smartLineThicknessEnabled;
			}
			set
			{
				this._smartLineThicknessEnabled = value;
			}
		}
		[Display(Name = "Smart Line Thickness: Step", Order = 152, GroupName = "Graphics")]
		[Range(0.0, 1.7976931348623157E+308)]
		public double SmartLineThicknessStep
		{
			get
			{
				return this._smartLineThicknessStep;
			}
			set
			{
				this._smartLineThicknessStep = value;
			}
		}
		[Range(1.0, 1.7976931348623157E+308)]
		[Display(Name = "Smart Line Thickness: Thinnest", Order = 154, GroupName = "Graphics")]
		public double SmartLineThicknessThinnest
		{
			get
			{
				return this._smartLineThicknessThinnest;
			}
			set
			{
				this._smartLineThicknessThinnest = value;
			}
		}
		[Display(Name = "Smart Line Thickness: Thickest", Order = 156, GroupName = "Graphics")]
		[Range(1.0, 1.7976931348623157E+308)]
		public double SmartLineThicknessThickest
		{
			get
			{
				return this._smartLineThicknessThickest;
			}
			set
			{
				this._smartLineThicknessThickest = value;
			}
		}
		[NinjaScriptProperty]
		[Display(Name = "MA: Type", Order = 0, GroupName = "Parameters")]
		public ninZa_MAType MAType
		{
			get
			{
				return this._maType;
			}
			set
			{
				this._maType = value;
			}
		}
		[NinjaScriptProperty]
		[Range(1, 2147483647)]
		[Display(Name = "MA: Period", Order = 2, GroupName = "Parameters")]
		public int MAPeriod
		{
			get
			{
				return this._maPeriod;
			}
			set
			{
				this._maPeriod = value;
			}
		}
		[NinjaScriptProperty]
		[Display(Name = "MA: Smoothing Enabled", Order = 4, GroupName = "Parameters")]
		public bool MASmoothingEnabled
		{
			get
			{
				return this._maSmoothingEnabled;
			}
			set
			{
				this._maSmoothingEnabled = value;
			}
		}
		[Range(1, 2147483647)]
		[NinjaScriptProperty]
		[Display(Name = "MA: Smoothing Period", Order = 6, GroupName = "Parameters")]
		public int MASmoothingPeriod
		{
			get
			{
				return this._maSmoothingPeriod;
			}
			set
			{
				this._maSmoothingPeriod = value;
			}
		}
		[NinjaScriptProperty]
		[Display(Name = "MA: Offset (StdDev)", Order = 8, GroupName = "Parameters")]
		[Range(0.0, 1.7976931348623157E+308)]
		public double MAOffset
		{
			get
			{
				return this._maOffset;
			}
			set
			{
				this._maOffset = value;
			}
		}
		[Display(Name = "Signal Reversal: Filter Enabled", Order = 10, GroupName = "Parameters")]
		[NinjaScriptProperty]
		public bool SignalReversalFilterEnabled
		{
			get
			{
				return this._signalReversalFilterEnabled;
			}
			set
			{
				this._signalReversalFilterEnabled = value;
			}
		}
		[Display(Name = "Signal Early & Deep: Calculating Period", Order = 30, GroupName = "Parameters")]
		[NinjaScriptProperty]
		[Range(1, 2147483647)]
		public int SignalEarlyDeepCalculatingPeriod
		{
			get
			{
				return this._signalEarlyDeepCalculatingPeriod;
			}
			set
			{
				this._signalEarlyDeepCalculatingPeriod = value;
			}
		}
		[Display(Name = "Signal: Finding Period", Order = 40, GroupName = "Parameters")]
		[Range(1, 2147483647)]
		[NinjaScriptProperty]
		public int SignalFindingPeriod
		{
			get
			{
				return this._signalFindingPeriod;
			}
			set
			{
				this._signalFindingPeriod = value;
			}
		}
		[Range(0, 2147483647)]
		[Display(Name = "Signal Split (Bars)", Order = 60, GroupName = "Parameters")]
		[NinjaScriptProperty]
		public int SignalSplit
		{
			get
			{
				return this._signalSplit;
			}
			set
			{
				this._signalSplit = value;
			}
		}
		[Range(0, 2147483647)]
		[NinjaScriptProperty]
		[Display(Name = "Threshold: Overbought", Order = 90, GroupName = "Parameters")]
		public int ThresholdOverbought
		{
			get
			{
				return this._thresholdOverbought;
			}
			set
			{
				this._thresholdOverbought = value;
			}
		}
		[NinjaScriptProperty]
		[Display(Name = "Threshold: Oversold", Order = 92, GroupName = "Parameters")]
		[Range(0, 2147483647)]
		public int ThresholdOversold
		{
			get
			{
				return this._thresholdOversold;
			}
			set
			{
				this._thresholdOversold = value;
			}
		}
		[Display(Name = "Enabled", Order = 0, GroupName = "Toggle")]
		public bool ToggleEnabled
		{
			get
			{
				return this._toggleEnabled;
			}
			set
			{
				this._toggleEnabled = value;
			}
		}
		[XmlIgnore]
		[Display(Name = "Background: On", Order = 10, GroupName = "Toggle")]
		public Brush ToggleBackBrushOn
		{
			get
			{
				return this._toggleBackBrushOn;
			}
			set
			{
				this._toggleBackBrushOn = value;
			}
		}
		[Browsable(false)]
		public string ToggleBackBrushOn_Serialize
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
		[Display(Name = "Background: Off", Order = 11, GroupName = "Toggle")]
		[XmlIgnore]
		public Brush ToggleBackBrushOff
		{
			get
			{
				return this._toggleBackBrushOff;
			}
			set
			{
				this._toggleBackBrushOff = value;
			}
		}
		[Browsable(false)]
		public string ToggleBackBrushOff_Serialize
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
		public string Button1Text
		{
			get
			{
				return this._button1Text;
			}
			set
			{
				this._button1Text = value;
			}
		}
		[Display(Name = "Button #2: Text", Order = 22, GroupName = "Toggle")]
		public string Button2Text
		{
			get
			{
				return this._button2Text;
			}
			set
			{
				this._button2Text = value;
			}
		}
		[XmlIgnore]
		[Display(Name = "Button: Text Color", Order = 24, GroupName = "Toggle")]
		public Brush ButtonTextBrush
		{
			get
			{
				return this._buttonTextBrush;
			}
			set
			{
				this._buttonTextBrush = value;
			}
		}
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
		public int ButtonTextSize
		{
			get
			{
				return this._buttonTextSize;
			}
			set
			{
				this._buttonTextSize = value;
			}
		}
		[Display(Name = "Drag Bar: Color", Order = 30, GroupName = "Toggle")]
		[XmlIgnore]
		public Brush ToggleDragBrush
		{
			get
			{
				return this._toggleDragBrush;
			}
			set
			{
				this._toggleDragBrush = value;
			}
		}
		[Browsable(false)]
		public string ToggleDragBrush_Serialize
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
		public ninZa_TextPosition TogglePositionAlignment
		{
			get
			{
				return this._togglePositionAlignment;
			}
			set
			{
				if (value == ninZa_TextPosition.TopLeft)
				{
					double num = 5.0;
					double num2 = (double)5f;
					this.TogglePositionMarginLeft = num;
					this.TogglePositionMarginTop = num2;
				}
				if (value == ninZa_TextPosition.TopRight)
				{
					double num3 = 5.0;
					double num2 = (double)5f;
					this.TogglePositionMarginRight = num3;
					this.TogglePositionMarginTop = num2;
				}
				if (value == ninZa_TextPosition.BottomRight)
				{
					double num4 = 5.0;
					double num2 = (double)5f;
					this.TogglePositionMarginRight = num4;
					this.TogglePositionMarginBottom = num2;
				}
				if (value == ninZa_TextPosition.BottomLeft)
				{
					double num5 = 5.0;
					double num2 = (double)5f;
					this.TogglePositionMarginLeft = num5;
					this.TogglePositionMarginBottom = num2;
				}
				if (value == ninZa_TextPosition.Center)
				{
					this.TogglePositionMarginBottom = 5.0;
					this.TogglePositionMarginRight = (double)5f;
					double num6 = (double)5f;
					double num2 = (double)5f;
					this.TogglePositionMarginTop = num6;
					this.TogglePositionMarginLeft = num2;
				}
				this._togglePositionAlignment = value;
			}
		}
		[Display(Name = "Position: Margin Left", Order = 41, GroupName = "Toggle")]
		public double TogglePositionMarginLeft
		{
			get
			{
				return this._togglePositionMarginLeft;
			}
			set
			{
				this._togglePositionMarginLeft = value;
			}
		}
		[Display(Name = "Position: Margin Top", Order = 42, GroupName = "Toggle")]
		public double TogglePositionMarginTop
		{
			get
			{
				return this._togglePositionMarginTop;
			}
			set
			{
				this._togglePositionMarginTop = value;
			}
		}
		[Display(Name = "Position: Margin Right", Order = 43, GroupName = "Toggle")]
		public double TogglePositionMarginRight
		{
			get
			{
				return this._togglePositionMarginRight;
			}
			set
			{
				this._togglePositionMarginRight = value;
			}
		}
		[Display(Name = "Position: Margin Bottom", Order = 44, GroupName = "Toggle")]
		public double TogglePositionMarginBottom
		{
			get
			{
				return this._togglePositionMarginBottom;
			}
			set
			{
				this._togglePositionMarginBottom = value;
			}
		}
		[Display(Name = "Z Order", Order = 0, GroupName = "Special")]
		public int IndicatorZOrder
		{
			get
			{
				return this._indicatorZOrder;
			}
			set
			{
				this._indicatorZOrder = value;
			}
		}
		[Display(Name = "User Note", Order = 10, GroupName = "Special")]
		public string UserNote
		{
			get
			{
				return this._userNote;
			}
			set
			{
				this._userNote = value;
			}
		}
		[Display(Name = "Switched On", Order = 0, GroupName = "Critical")]
		public bool SwitchedOn
		{
			get
			{
				return this._switchedOn;
			}
			set
			{
				this._switchedOn = value;
			}
		}
		[Display(Name = "Info Enabled", Order = 10, GroupName = "Critical")]
		public bool InfoEnabled
		{
			get
			{
				return this._infoEnabled;
			}
			set
			{
				this._infoEnabled = value;
			}
		}
		[Browsable(false)]
		[XmlIgnore]
		public Series<double> MA
		{
			get
			{
				return base.Values[0];
			}
		}
		[XmlIgnore]
		[Browsable(false)]
		public Series<double> VolumeDelta
		{
			get
			{
				return base.Values[1];
			}
		}
		[XmlIgnore]
		[Browsable(false)]
		public Series<double> Signal_Trend
		{
			get
			{
				return base.Values[2];
			}
		}
		[Browsable(false)]
		[XmlIgnore]
		public Series<double> Signal_Trade
		{
			get
			{
				return base.Values[3];
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
				return "V-Elementra" + this.FormatDisplayNameSuffix();
			}
		}
		private string FormatDisplayNameSuffix()
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
					base.Name = "DDVEL";
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
					base.AddPlot(new Stroke(Brushes.Goldenrod, DashStyleHelper.Dot, 1f), PlotStyle.Line, "MA");
					base.AddPlot(Brushes.Transparent, "Volume Delta");
					base.AddPlot(Brushes.Transparent, "Signal: Trend");
					base.AddPlot(Brushes.Transparent, "Signal: Trade");
					this.ConditionTrendStart = true;
					this.ConditionReversal = true;
					this.ConditionEarly = true;
					this.ConditionDeep = true;
					this.MarkerEnabled = true;
					this.MarkerRenderingMethod = DDVEL_RenderingMethod.Builtin;
					this.MarkerBrushBullish = Brushes.Cyan;
					this.MarkerBrushBearish = Brushes.Coral;
					this.MarkerStringUptrendStart = "●";
					this.MarkerStringDowntrendStart = "●";
					this.MarkerStringReversalBullish = "▲ + R";
					this.MarkerStringReversalBearish = "R + ▼";
					this.MarkerStringEarlyBullish = ".";
					this.MarkerStringEarlyBearish = ".";
					this.MarkerStringDeepBullish = ".";
					this.MarkerStringDeepBearish = ".";
					this.MarkerFont = new SimpleFont("Arial", 20);
					this.MarkerOffset = 10;
					this.AlertBlockingSeconds = 60;
					this.LogoEnabled = false;
					this.InstructionEnabled = false;
					this.ScreenDPI = 100;
					this.PlotEnabled = true;
					this.PlotUptrend = Brushes.DodgerBlue;
					this.PlotDowntrend = Brushes.HotPink;
					this.CloudEnabled = false;
					this.CloudUpperNear = Brushes.Black;
					this.CloudUpperFar = Brushes.HotPink;
					this.CloudLowerNear = Brushes.Black;
					this.CloudLowerFar = Brushes.LightGreen;
					this.CloudOpacity = 80;
					this.CloudQuantityPerSide = 17;
					this.CloudOffset = 0.072;
					this.InfoDeltaPositive = Brushes.Cyan;
					this.InfoDeltaNegative = Brushes.DarkOrange;
					this.InfoDeltaZero = Brushes.DarkGray;
					this.InfoDeltaOpacity = 100;
					this.InfoNumberFont = new SimpleFont("Arial", 13)
					{
						Bold = true
					};
					this.InfoDisplayPeriod = 5;
					this.InfoMargin = 8;
					this.SwingPointEnabled = false;
					this.SwingPointTop = Brushes.DodgerBlue;
					this.SwingPointBottom = Brushes.HotPink;
					this.LineEnabled = true;
					this.LineColorUp = Brushes.DodgerBlue;
					this.LineColorDown = Brushes.Coral;
					this.LineOpacity = 100;
					this.LineStyleFinished = new Stroke(Brushes.Transparent, DashStyleHelper.Solid, 4f);
					this.LineStyleDeveloping = new Stroke(Brushes.Transparent, DashStyleHelper.Dot, 3f);
					this.SmartLineThicknessEnabled = true;
					this.SmartLineThicknessStep = 1E-05;
					this.SmartLineThicknessThinnest = 1.0;
					this.SmartLineThicknessThickest = 10.0;
					this.MAType = ninZa_MAType.EMA;
					this.MAPeriod = 89;
					this.MASmoothingEnabled = true;
					this.MASmoothingPeriod = 14;
					this.MAOffset = 1.2;
					this.SignalReversalFilterEnabled = true;
					this.SignalEarlyDeepCalculatingPeriod = 10;
					this.SignalFindingPeriod = 5;
					this.SignalSplit = 7;
					this.ThresholdOverbought = 70;
					this.ThresholdOversold = 30;
					this.ToggleEnabled = true;
					this.ToggleBackBrushOn = Brushes.DodgerBlue;
					this.ToggleBackBrushOff = Brushes.Silver;
					this.Button1Text = "V-Elementra";
					this.Button2Text = "Info";
					this.ButtonTextBrush = Brushes.White;
					this.ButtonTextSize = 10;
					this.ToggleDragBrush = Brushes.LimeGreen;
					this.TogglePositionAlignment = ninZa_TextPosition.TopLeft;
					this.TogglePositionMarginLeft = 5.0;
					this.TogglePositionMarginTop = 5.0;
					this.TogglePositionMarginRight = 5.0;
					this.TogglePositionMarginBottom = 5.0;
					this.SwitchedOn = true;
					this.InfoEnabled = true;
					this.IndicatorZOrder = -100;
					this.UserNote = "instrument (period)";
				}
				else if (base.State == State.Configure)
				{
					this._useCustomMarkerRendering = this.MarkerRenderingMethod == DDVEL_RenderingMethod.Custom;
					this._useSecondaryVolumeSeries = false;
					base.AddDataSeries(BarsPeriodType.Tick, 1);
					this._completedTrends = new SortedList<int, TrendSegment>();
					this._activeTrends = new SortedList<int, TrendSegment>();
					this._trendReversalFlags = new SortedList<int, bool>();
					this._stochRawSeries = new Series<double>(this, MaximumBarsLookBack.Infinite);
					this._markersByBar = new Dictionary<int, SignalEvent>();
					this._maUpper = new Series<double>(this, MaximumBarsLookBack.Infinite);
					this._maCenter = new Series<double>(this, MaximumBarsLookBack.Infinite);
					this._maLower = new Series<double>(this, MaximumBarsLookBack.Infinite);
					this._reversalFilterLocked = new Dictionary<int, bool>();
					this._infoBrushPositive = ninZa_BrushManager.CreateOpacityBrush(this.InfoDeltaPositive, this.InfoDeltaOpacity);
					this._infoBrushZero = ninZa_BrushManager.CreateOpacityBrush(this.InfoDeltaZero, this.InfoDeltaOpacity);
					this._infoBrushNegative = ninZa_BrushManager.CreateOpacityBrush(this.InfoDeltaNegative, this.InfoDeltaOpacity);
					this._earlySignalRaw = new Series<double>(this, MaximumBarsLookBack.Infinite);
					this._deepSignalRaw = new Series<double>(this, MaximumBarsLookBack.Infinite);
					this._cloudSeriesCount = this.CloudQuantityPerSide + 1;
					if (this.CloudEnabled && this.CloudOpacity > 0)
					{
						this._cloudUpperSeries = new List<Series<double>>();
						this._cloudLowerSeries = new List<Series<double>>();
						for (int i = 0; i < this._cloudSeriesCount; i++)
						{
							this._cloudUpperSeries.Add(new Series<double>(this, MaximumBarsLookBack.Infinite));
							this._cloudLowerSeries.Add(new Series<double>(this, MaximumBarsLookBack.Infinite));
						}
					}
					this._atrSeries = base.ninZaATR(100);
					this._swingPoints = new List<SwingPoint>();
					this._lineBrushUp = ninZa_BrushManager.CreateOpacityBrush(this.LineColorUp, this.LineOpacity);
					this._lineBrushDown = ninZa_BrushManager.CreateOpacityBrush(this.LineColorDown, this.LineOpacity);
				}
				else if (base.State == State.DataLoaded)
				{
					this._isLongTimeframeBars = base.BarsArray[0].BarsPeriod.BarsPeriodType == BarsPeriodType.Day || base.BarsArray[0].BarsPeriod.BarsPeriodType == BarsPeriodType.Week || base.BarsArray[0].BarsPeriod.BarsPeriodType == BarsPeriodType.Month || base.BarsArray[0].BarsPeriod.BarsPeriodType == BarsPeriodType.Year;
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
						this._isLive = base.ChartControl != null;
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
					if (!this._isLongTimeframeBars)
					{
						if (base.BarsInProgress == 0)
						{
							this._volumeBarKey = base.CurrentBars[0] + 1;
						}
						else if (base.BarsInProgress == 1)
						{
							double num = base.Closes[1][0];
							if (base.BarsArray[1].IsFirstBarOfSession)
							{
								if (base.CurrentBars[0] >= 0)
								{
									if (!base.BarsArray[0].IsFirstBarOfSession)
									{
										int num2 = base.CurrentBars[0];
									}
									else
									{
										int num3 = base.CurrentBars[0];
									}
								}
								this._sessionVolume = new BarVolumeState();
								this._sessionLow = (this._sessionHigh = num);
							}
							if (num.ApproxCompare(this._sessionLow) < 0)
							{
								this._sessionLow = num;
							}
							if (num.ApproxCompare(this._sessionHigh) > 0)
							{
								this._sessionHigh = num;
							}
							if (this._useSecondaryVolumeSeries)
							{
								this.UpdateTickDirection();
								long num4 = this.VolumeToLong(base.Volumes[1][0]);
								double num5 = base.Closes[1][0];
								if (!this._barVolumeByIndex.ContainsKey(this._volumeBarKey))
								{
									this._barVolumeByIndex.Add(this._volumeBarKey, new BarVolumeState());
								}
								if (this._tickDirection > 0)
								{
									this._barVolumeByIndex[this._volumeBarKey].AddAskVolume(this.PriceToTickIndex(num5), (double)num4);
									this._sessionVolume.AddAskVolume(this.PriceToTickIndex(num5), (double)num4);
								}
								if (this._tickDirection < 0)
								{
									this._barVolumeByIndex[this._volumeBarKey].AddBidVolume(this.PriceToTickIndex(num5), (double)num4);
									this._sessionVolume.AddBidVolume(this.PriceToTickIndex(num5), (double)num4);
								}
							}
						}
						if (base.BarsInProgress == 0)
						{
							this.UpdateMaAndCloud();
							if (this.ConditionDeep || this.ConditionEarly)
							{
								this._stochRange = base.Input[0] - base.MIN(base.Low, this.SignalEarlyDeepCalculatingPeriod)[0];
								this._stochFullRange = base.MAX(base.High, this.SignalEarlyDeepCalculatingPeriod)[0] - base.MIN(base.Low, this.SignalEarlyDeepCalculatingPeriod)[0];
								if (this._stochFullRange.ApproxCompare(0.0) == 0)
								{
									this._stochRawSeries[0] = ((base.CurrentBar == 0) ? 50.0 : this._stochRawSeries[1]);
								}
								else
								{
									this._stochRawSeries[0] = Math.Max(0.0, Math.Min(100.0, 100.0 * this._stochRange / this._stochFullRange));
								}
								this._earlySignalRaw[0] = Math.Min(100.0, Math.Max(0.0, this.ComputeMAValue(this._stochRawSeries, ninZa_MAType.EMA, 5)));
								this._deepSignalRaw[0] = Math.Min(100.0, Math.Max(0.0, this.ComputeMAValue(this._earlySignalRaw, ninZa_MAType.EMA, 7)));
								if (base.CurrentBar == 0)
								{
									this._stochCrossUp = this._earlySignalRaw[0] >= this._deepSignalRaw[0];
								}
								if (this._earlySignalRaw[0].ApproxCompare((double)this.ThresholdOverbought) >= 0)
								{
									this._stochZone = 1;
								}
								else if (this._earlySignalRaw[0].ApproxCompare((double)this.ThresholdOversold) <= 0)
								{
									this._stochZone = -1;
								}
								else
								{
									this._stochZone = 0;
								}
							}
							int num6 = base.CurrentBars[0];
							if (num6 != 0)
							{
								if (num6 == 1)
								{
									this._trendIsBullish = this.MA[0].ApproxCompare(this.MA[1]) > 0;
								}
								else
								{
									int num7 = 0;
									if (this._trendIsBullish && this.MA[0].ApproxCompare(this.MA[1]) < 0)
									{
										this._trendIsBullish = false;
										if (this.ConditionTrendStart)
										{
											SignalEvent u = new SignalEvent(this._trendIsBullish, num6, SignalKind.TrendStart);
											if (this._useCustomMarkerRendering)
											{
												this.RegisterMarker(u);
											}
											else
											{
												this.DrawBuiltinMarker(u);
											}
										}
										num7 = -1;
									}
									else if (!this._trendIsBullish && this.MA[0].ApproxCompare(this.MA[1]) > 0)
									{
										this._trendIsBullish = true;
										if (this.ConditionTrendStart)
										{
											SignalEvent u2 = new SignalEvent(this._trendIsBullish, num6, SignalKind.TrendStart);
											if (this._useCustomMarkerRendering)
											{
												this.RegisterMarker(u2);
											}
											else
											{
												this.DrawBuiltinMarker(u2);
											}
										}
										num7 = 1;
									}
									this.Signal_Trend[0] = (double)(this._trendIsBullish ? 1 : (-1));
									if (this._isLive && this.PlotEnabled)
									{
										if (this.Signal_Trend[0].ApproxCompare(this.Signal_Trend[1]) != 0)
										{
											base.PlotBrushes[0][0] = base.PlotBrushes[0][1];
										}
										else
										{
											Brush brush = (this._trendIsBullish ? this.PlotUptrend : this.PlotDowntrend);
											if (!brush.IsTransparent())
											{
												base.PlotBrushes[0][0] = brush;
											}
										}
									}
									double num8 = base.Lows[0][0];
									double num9 = base.Closes[0][0];
									double num10 = base.Opens[0][0];
									double num11 = base.Highs[0][0];
									if (this._barVolumeByIndex.ContainsKey(num6))
									{
										this.VolumeDelta[0] = this._barVolumeByIndex[num6].VolumeDelta;
									}
									if (this.SignalReversalFilterEnabled)
									{
										if (base.High[0].ApproxCompare(this._maCenter[0]) > 0 && num8.ApproxCompare(this._maCenter[0]) < 0)
										{
											this._pendingReversalDirection = null;
											for (int i = this._activeTrends.Count - 1; i >= 0; i--)
											{
												KeyValuePair<int, TrendSegment> keyValuePair = this._activeTrends.ElementAt(i);
												this.ClearReversalFlagsFromSegment(keyValuePair.Value, keyValuePair.Key);
											}
											bool flag = false;
											if (this._completedTrends.Count > 0 && num6 <= this._completedTrends[this._completedTrends.Keys.Max()].GetEndBarIndex() + this.InfoDisplayPeriod && ((this._completedTrends[this._completedTrends.Keys.Max()].GetReversalDirection().Value && num8.ApproxCompare(this._maLower[0]) < 0) || (!this._completedTrends[this._completedTrends.Keys.Max()].GetReversalDirection().Value && num11.ApproxCompare(this._maUpper[0]) > 0)))
											{
												flag = true;
											}
											this._activeTrends.Add(num6, new TrendSegment(num6, flag, -1, null));
										}
										bool flag2 = num9.ApproxCompare(num10) > 0;
										bool flag3 = num11.ApproxCompare(this._maUpper[0]) > 0;
										bool flag4 = this._barVolumeByIndex.ContainsKey(num6) && this._barVolumeByIndex[num6].VolumeDelta > 0.0;
										bool flag5 = num9.ApproxCompare(num10) < 0;
										bool flag6 = num8.ApproxCompare(this._maLower[0]) < 0;
										bool flag7 = this._barVolumeByIndex.ContainsKey(num6) && this._barVolumeByIndex[num6].VolumeDelta < 0.0;
										for (int j = this._activeTrends.Count - 1; j >= 0; j--)
										{
											TrendSegment value = this._activeTrends.ElementAt(j).Value;
											if (flag2 && flag3 && flag4)
											{
												value.SetSignalBarIndex(num6);
												value.SetReversalDirection(new bool?(false));
											}
											else if (flag5 && flag6 && flag7)
											{
												value.SetSignalBarIndex(num6);
												value.SetReversalDirection(new bool?(true));
											}
										}
										this.UpdateReversalFilter();
										int num12 = num6 - 1;
										bool flag8 = this._barVolumeByIndex.ContainsKey(num6) && Math.Abs(this._barVolumeByIndex[num6].VolumeDelta) > 1.0;
										bool flag9 = base.Closes[0][1].ApproxCompare(base.Opens[0][1]) > 0 && num9.ApproxCompare(num10) < 0;
										bool flag10 = this._barVolumeByIndex.ContainsKey(num12) && this._barVolumeByIndex[num12].VolumeDelta > 0.0 && this._barVolumeByIndex.ContainsKey(num6) && this._barVolumeByIndex[num6].VolumeDelta < 0.0;
										bool flag11 = num11.ApproxCompare(base.Highs[0][1]) > 0;
										bool flag12 = num11.ApproxCompare(this._maUpper[0]) < 0;
										bool flag13 = base.Closes[0][1].ApproxCompare(base.Opens[0][1]) < 0 && num9.ApproxCompare(num10) > 0;
										bool flag14 = this._barVolumeByIndex.ContainsKey(num12) && this._barVolumeByIndex[num12].VolumeDelta < 0.0 && this._barVolumeByIndex.ContainsKey(num6) && this._barVolumeByIndex[num6].VolumeDelta > 0.0;
										bool flag15 = num8.ApproxCompare(base.Lows[0][1]) < 0;
										bool flag16 = num8.ApproxCompare(this._maLower[0]) > 0;
										for (int k = this._activeTrends.Count - 1; k >= 0; k--)
										{
											KeyValuePair<int, TrendSegment> keyValuePair2 = this._activeTrends.ElementAt(k);
											TrendSegment value2 = keyValuePair2.Value;
											int key = keyValuePair2.Key;
											int num13 = value2.GetSignalBarIndex();
											if (num6 > num13 && num13 != -1)
											{
												if (this._trendReversalFlags.ContainsKey(num6))
												{
													this._activeTrends.Remove(key);
												}
												else if (value2.GetReversalDirection() != null && (value2.GetReversalDirection().Value || !flag12) && (!value2.GetReversalDirection().Value || !flag16))
												{
													if (value2.GetReversalDirection() != null && !value2.GetReversalDirection().Value && flag9 && flag10 && flag11 && flag8 && this.IsSignalPivot(false))
													{
														value2.SetEndBarIndex(num6);
														this._completedTrends.Add(key, value2);
														this._activeTrends.Remove(key);
														if (!this._trendReversalFlags.ContainsKey(num6))
														{
															this._trendReversalFlags.Add(num6, true);
														}
														if (this.ConditionReversal)
														{
															SignalEvent u3 = new SignalEvent(false, num6, SignalKind.Reversal);
															if (this._useCustomMarkerRendering)
															{
																this.RegisterMarker(u3);
															}
															else
															{
																this.DrawBuiltinMarker(u3);
															}
														}
														num7 = -2;
													}
													else if (value2.GetReversalDirection() != null && value2.GetReversalDirection().Value && flag13 && flag14 && flag15 && flag8 && this.IsSignalPivot(true))
													{
														value2.SetEndBarIndex(num6);
														this._completedTrends.Add(key, value2);
														this._activeTrends.Remove(key);
														if (!this._trendReversalFlags.ContainsKey(num6))
														{
															this._trendReversalFlags.Add(num6, true);
														}
														if (this.ConditionReversal)
														{
															SignalEvent u4 = new SignalEvent(true, num6, SignalKind.Reversal);
															if (this._useCustomMarkerRendering)
															{
																this.RegisterMarker(u4);
															}
															else
															{
																this.DrawBuiltinMarker(u4);
															}
														}
														num7 = 2;
													}
												}
												else
												{
													this.ClearReversalFlagsFromSegment(value2, key);
												}
											}
										}
									}
									else
									{
										this.UpdateReversalFilter();
										bool flag17 = num9.ApproxCompare(num10) > 0;
										bool flag18 = num11.ApproxCompare(this._maUpper[0]) > 0;
										bool flag19 = this._barVolumeByIndex.ContainsKey(num6) && this._barVolumeByIndex[num6].VolumeDelta > 0.0;
										bool flag20 = num9.ApproxCompare(num10) < 0;
										bool flag21 = num8.ApproxCompare(this._maLower[0]) < 0;
										bool flag22 = this._barVolumeByIndex.ContainsKey(num6) && this._barVolumeByIndex[num6].VolumeDelta < 0.0;
										if (flag17 && flag18 && flag19)
										{
											this._activeTrends.Add(num6, new TrendSegment(-1, false, num6, new bool?(false)));
										}
										else if (flag20 && flag21 && flag22)
										{
											this._activeTrends.Add(num6, new TrendSegment(-1, false, num6, new bool?(true)));
										}
										int num14 = num6 - 1;
										bool flag23 = this._barVolumeByIndex.ContainsKey(num6) && Math.Abs(this._barVolumeByIndex[num6].VolumeDelta) > 1.0;
										bool flag24 = base.Closes[0][1].ApproxCompare(base.Opens[0][1]) > 0 && num9.ApproxCompare(num10) < 0;
										bool flag25 = this._barVolumeByIndex.ContainsKey(num14) && this._barVolumeByIndex[num14].VolumeDelta > 0.0 && this._barVolumeByIndex.ContainsKey(num6) && this._barVolumeByIndex[num6].VolumeDelta < 0.0;
										bool flag26 = num11.ApproxCompare(base.Highs[0][1]) > 0;
										bool flag27 = num11.ApproxCompare(this._maUpper[0]) < 0;
										bool flag28 = base.Closes[0][1].ApproxCompare(base.Opens[0][1]) < 0 && num9.ApproxCompare(num10) > 0;
										bool flag29 = this._barVolumeByIndex.ContainsKey(num14) && this._barVolumeByIndex[num14].VolumeDelta < 0.0 && this._barVolumeByIndex.ContainsKey(num6) && this._barVolumeByIndex[num6].VolumeDelta > 0.0;
										bool flag30 = num8.ApproxCompare(base.Lows[0][1]) < 0;
										bool flag31 = num8.ApproxCompare(this._maLower[0]) > 0;
										for (int l = this._activeTrends.Count - 1; l >= 0; l--)
										{
											KeyValuePair<int, TrendSegment> keyValuePair3 = this._activeTrends.ElementAt(l);
											TrendSegment value3 = keyValuePair3.Value;
											int key2 = keyValuePair3.Key;
											int num15 = value3.GetSignalBarIndex();
											if (num6 > num15)
											{
												if (this._trendReversalFlags.ContainsKey(num6))
												{
													this._activeTrends.Remove(key2);
												}
												else if (value3.GetReversalDirection() != null && (value3.GetReversalDirection().Value || !flag27) && (!value3.GetReversalDirection().Value || !flag31))
												{
													if (value3.GetReversalDirection() != null && !value3.GetReversalDirection().Value && flag24 && flag25 && flag26 && flag23 && this.IsSignalPivot(false))
													{
														value3.SetEndBarIndex(num6);
														this._completedTrends.Add(key2, value3);
														this._activeTrends.Remove(key2);
														if (!this._trendReversalFlags.ContainsKey(num6))
														{
															this._trendReversalFlags.Add(num6, true);
														}
														if (this.ConditionReversal)
														{
															SignalEvent u5 = new SignalEvent(false, num6, SignalKind.Reversal);
															if (this._useCustomMarkerRendering)
															{
																this.RegisterMarker(u5);
															}
															else
															{
																this.DrawBuiltinMarker(u5);
															}
														}
														num7 = -2;
													}
													else if (value3.GetReversalDirection() != null && value3.GetReversalDirection().Value && flag28 && flag29 && flag30 && flag23 && this.IsSignalPivot(true))
													{
														value3.SetEndBarIndex(num6);
														this._completedTrends.Add(key2, value3);
														this._activeTrends.Remove(key2);
														if (!this._trendReversalFlags.ContainsKey(num6))
														{
															this._trendReversalFlags.Add(num6, true);
														}
														if (this.ConditionReversal)
														{
															SignalEvent u6 = new SignalEvent(true, num6, SignalKind.Reversal);
															if (this._useCustomMarkerRendering)
															{
																this.RegisterMarker(u6);
															}
															else
															{
																this.DrawBuiltinMarker(u6);
															}
														}
														num7 = 2;
													}
												}
												else
												{
													this._activeTrends.Remove(key2);
												}
											}
										}
									}
									if (this.ConditionDeep || this.ConditionEarly)
									{
										bool flag32 = num8.ApproxCompare(this._maLower[0]) > 0 && num11.ApproxCompare(this._maUpper[0]) < 0;
										if (!this._stochCrossUp && this._earlySignalRaw[0].ApproxCompare(this._deepSignalRaw[0]) > 0)
										{
											this._stochCrossUp = true;
											bool flag33 = num9.ApproxCompare(num10) > 0;
											bool flag34 = num8.ApproxCompare(this._maCenter[0]) > 0;
											if (flag32 && this._trendIsBullish && (this._stochZone != 0 || this._earlySignalRaw[1].ApproxCompare((double)this.ThresholdOversold) <= 0) && this._deepSignalRaw[0].ApproxCompare((double)this.ThresholdOversold) <= 0 && flag33 && this.IsSignalPivot(true) && base.CurrentBar - this._lastCloudBar > this.SignalSplit)
											{
												if (flag34)
												{
													if (this.ConditionEarly)
													{
														SignalEvent u7 = new SignalEvent(true, base.CurrentBars[0], SignalKind.Early);
														if (this._useCustomMarkerRendering)
														{
															this.RegisterMarker(u7);
														}
														else
														{
															this.DrawBuiltinMarker(u7);
														}
													}
													num7 = 3;
												}
												else
												{
													if (this.ConditionDeep)
													{
														SignalEvent u8 = new SignalEvent(true, base.CurrentBars[0], SignalKind.Deep);
														if (this._useCustomMarkerRendering)
														{
															this.RegisterMarker(u8);
														}
														else
														{
															this.DrawBuiltinMarker(u8);
														}
													}
													num7 = 4;
												}
												this._lastCloudBar = base.CurrentBar;
											}
										}
										else if (this._stochCrossUp && this._earlySignalRaw[0].ApproxCompare(this._deepSignalRaw[0]) < 0)
										{
											this._stochCrossUp = false;
											bool flag35 = num9.ApproxCompare(num10) < 0;
											bool flag36 = num11.ApproxCompare(this._maCenter[0]) < 0;
											if (flag32 && !this._trendIsBullish && (this._stochZone != 0 || this._earlySignalRaw[1].ApproxCompare((double)this.ThresholdOverbought) >= 0) && this._deepSignalRaw[0].ApproxCompare((double)this.ThresholdOverbought) >= 0 && flag35 && this.IsSignalPivot(false) && base.CurrentBar - this._lastCloudBar > this.SignalSplit)
											{
												if (flag36)
												{
													if (this.ConditionEarly)
													{
														SignalEvent u9 = new SignalEvent(false, base.CurrentBars[0], SignalKind.Early);
														if (this._useCustomMarkerRendering)
														{
															this.RegisterMarker(u9);
														}
														else
														{
															this.DrawBuiltinMarker(u9);
														}
													}
													num7 = -3;
												}
												else
												{
													if (this.ConditionDeep)
													{
														SignalEvent u10 = new SignalEvent(false, base.CurrentBars[0], SignalKind.Deep);
														if (this._useCustomMarkerRendering)
														{
															this.RegisterMarker(u10);
														}
														else
														{
															this.DrawBuiltinMarker(u10);
														}
													}
													num7 = -4;
												}
												this._lastCloudBar = base.CurrentBar;
											}
										}
									}
									this.Signal_Trade[0] = (double)num7;
									if (this._isLive && this.CloudEnabled && this.CloudOpacity > 0 && num6 >= base.BarsArray[0].Count - 2)
									{
										for (int m = 1; m < this._cloudSeriesCount; m++)
										{
											double num16 = (double)(this._cloudSeriesCount - m) / (double)this._cloudSeriesCount - 0.15000000596046448;
											string text = "DDVEL.cloud.upper." + m.ToString();
											Brush brush2 = ninZa_BrushManager.CreateGradientBrush(this.CloudUpperFar, this.CloudUpperNear, num16);
											NinjaTrader.NinjaScript.DrawingTools.Region region = NinjaTrader.NinjaScript.DrawingTools.Draw.Region(this, text, num6, 0, this._cloudUpperSeries[m - 1], this._cloudUpperSeries[m], null, brush2, this.CloudOpacity, 0);
											text = "DDVEL.cloud.lower." + m.ToString();
											Brush brush3 = ninZa_BrushManager.CreateGradientBrush(this.CloudLowerFar, this.CloudLowerNear, num16);
											NinjaTrader.NinjaScript.DrawingTools.Region region2 = NinjaTrader.NinjaScript.DrawingTools.Draw.Region(this, text, num6, 0, this._cloudLowerSeries[m - 1], this._cloudLowerSeries[m], null, brush3, this.CloudOpacity, 0);
											if (!this.SwitchedOn)
											{
												NinjaScript ninjaScript = region;
												region2.IsVisible = false;
												ninjaScript.IsVisible = false;
											}
										}
									}
									double num17 = 5.0 * this._atrSeries[0];
									if (this._swingState == 0)
									{
										if (num11.ApproxCompare(this._maUpper[0]) > 0)
										{
											this._swingLowPrice = base.High[0];
											this._swingCandidateBar = base.CurrentBar;
											SwingPoint swingPoint = new SwingPoint(this._swingCandidateBar, this._swingLowPrice, true, true);
											this.CommitSwingPoint(swingPoint);
											this._swingState = 1;
											this._swingVolumeSum = this._swingLowPrice - num17;
										}
										else if (num8.ApproxCompare(this._maLower[0]) < 0)
										{
											this._swingHighPrice = base.Low[0];
											this._swingDirection = base.CurrentBar;
											SwingPoint swingPoint2 = new SwingPoint(this._swingDirection, this._swingHighPrice, false, true);
											this.CommitSwingPoint(swingPoint2);
											this._swingState = -1;
											this._swingVolumeSum = this._swingHighPrice + num17;
										}
									}
									else if (this._swingState > 0)
									{
										bool flag37 = false;
										if (num11.ApproxCompare(this._swingLowPrice) > 0 && num11.ApproxCompare(this._maUpper[0]) > 0)
										{
											flag37 = true;
											this._swingLowPrice = num11;
											this._swingCandidateBar = base.CurrentBar;
											this.UpdateDevelopingSwing(this._swingCandidateBar, this._swingLowPrice);
											this._swingVolumeSum = Math.Max(this._swingVolumeSum, this._swingLowPrice - num17);
										}
										if (!flag37 && num8.ApproxCompare(this._swingVolumeSum) <= 0 && base.CurrentBar > this._swingCandidateBar && num8.ApproxCompare(this._maLower[0]) < 0)
										{
											this._swingHighPrice = num8;
											this._swingDirection = base.CurrentBar;
											SwingPoint swingPoint3 = new SwingPoint(this._swingDirection, this._swingHighPrice, false, true);
											this.CommitSwingPoint(swingPoint3);
											this._swingState = -1;
											this._swingVolumeSum = Math.Min(this._swingHighPrice + num17, this._swingPoints[1].Price);
										}
									}
									else
									{
										bool flag38 = false;
										if (num8.ApproxCompare(this._swingHighPrice) < 0 && num8.ApproxCompare(this._maLower[0]) < 0)
										{
											flag38 = true;
											this._swingHighPrice = num8;
											this._swingDirection = base.CurrentBar;
											this.UpdateDevelopingSwing(this._swingDirection, this._swingHighPrice);
											this._swingVolumeSum = Math.Min(this._swingVolumeSum, this._swingHighPrice + num17);
										}
										if (!flag38 && num11.ApproxCompare(this._swingVolumeSum) >= 0 && base.CurrentBar > this._swingDirection && num11.ApproxCompare(this._maUpper[0]) > 0)
										{
											this._swingLowPrice = num11;
											this._swingCandidateBar = base.CurrentBar;
											SwingPoint swingPoint4 = new SwingPoint(this._swingCandidateBar, this._swingLowPrice, true, true);
											this.CommitSwingPoint(swingPoint4);
											this._swingState = 1;
											this._swingVolumeSum = Math.Max(this._swingLowPrice - num17, this._swingPoints[1].Price);
										}
									}
									if (this._swingPoints.Count >= 2 && this._swingPoints[0].IsCandidate && this._swingPoints[0].BarIndex == num6)
									{
										for (int n = this._swingPoints[1].BarIndex; n <= this._swingPoints[0].BarIndex; n++)
										{
											this._swingPoints[0].SumVolPerLine += base.Volumes[0][num6 - n];
										}
									}
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
		private bool IsSignalPivot(bool isLow)
		{
			if (base.CurrentBars[0] <= this.SignalFindingPeriod)
			{
				return false;
			}
			double num = (isLow ? base.Lows[0][1] : base.Highs[0][1]);
			int num2 = (isLow ? (-1) : 1);
			for (int i = 2; i <= this.SignalFindingPeriod; i++)
			{
				if (num.ApproxCompare(isLow ? base.Lows[0][i] : base.Highs[0][i]) * num2 < 0)
				{
					return false;
				}
			}
			return true;
		}
		private void ClearReversalFlagsFromSegment(TrendSegment segment, int endBarIndex)
		{
			for (int i = segment.GetStartBarIndex(); i <= base.CurrentBars[0]; i++)
			{
				if (this._reversalFilterLocked.ContainsKey(i) && !this._reversalFilterLocked[i] && this._barVolumeByIndex.ContainsKey(i))
				{
					this._barVolumeByIndex[i].ReversalDirection = null;
				}
			}
			this._activeTrends.Remove(endBarIndex);
		}
		private void UpdateReversalFilter()
		{
			double num = base.Lows[0][0];
			double num2 = base.Closes[0][0];
			double num3 = base.Highs[0][0];
			int num4 = base.CurrentBars[0];
			if (this.SignalReversalFilterEnabled)
			{
				bool flag = false;
				if (this._activeTrends.Count > 0)
				{
					if (num4 > this._activeTrends.Keys.Max())
					{
						int num5 = this._activeTrends.Keys.Max();
						if (this._activeTrends[num5].GetReversalDirection() == null)
						{
							if (this._completedTrends.Count > 0)
							{
								TrendSegment u = this._completedTrends[this._completedTrends.Keys.Max()];
								bool value = u.GetReversalDirection().Value;
								if (num4 <= u.GetEndBarIndex() + this.InfoDisplayPeriod && (!value || num.ApproxCompare(this._maLower[0]) <= 0) && (value || num3.ApproxCompare(this._maUpper[0]) >= 0))
								{
									flag = true;
								}
								else
								{
									this._pendingReversalDirection = null;
								}
							}
							else if (num.ApproxCompare(this._maLower[0]) > 0 && num3.ApproxCompare(this._maUpper[0]) < 0)
							{
								this._pendingReversalDirection = null;
							}
							if (num.ApproxCompare(this._maLower[0]) < 0)
							{
								this._pendingReversalDirection = new bool?(false);
							}
							else if (num3.ApproxCompare(this._maUpper[0]) > 0)
							{
								this._pendingReversalDirection = new bool?(true);
							}
						}
						else if (this._activeTrends[num5].GetReversalDirection().Value)
						{
							if (num.ApproxCompare(this._maLower[0]) < 0)
							{
								this._pendingReversalDirection = new bool?(false);
							}
							if (this._barVolumeByIndex.ContainsKey(num5) && base.Lows[0].GetValueAt(num5).ApproxCompare(this._maLower.GetValueAt(num5)) < 0)
							{
								this._barVolumeByIndex[num5].ReversalDirection = new bool?(false);
								if (!this._reversalFilterLocked.ContainsKey(num5))
								{
									this._reversalFilterLocked.Add(num5, this._activeTrends[num5].IsFilterLocked());
								}
								else
								{
									this._reversalFilterLocked[num5] = this._activeTrends[num5].IsFilterLocked();
								}
							}
						}
						else if (!this._activeTrends[num5].GetReversalDirection().Value)
						{
							if (num3.ApproxCompare(this._maUpper[0]) > 0)
							{
								this._pendingReversalDirection = new bool?(true);
							}
							if (this._barVolumeByIndex.ContainsKey(num5) && base.Highs[0].GetValueAt(num5).ApproxCompare(this._maUpper.GetValueAt(num5)) > 0)
							{
								this._barVolumeByIndex[num5].ReversalDirection = new bool?(true);
								if (!this._reversalFilterLocked.ContainsKey(num5))
								{
									this._reversalFilterLocked.Add(num5, this._activeTrends[num5].IsFilterLocked());
								}
								else
								{
									this._reversalFilterLocked[num5] = this._activeTrends[num5].IsFilterLocked();
								}
							}
						}
					}
				}
				else if (this._completedTrends.Count > 0)
				{
					bool value2 = this._completedTrends[this._completedTrends.Keys.Max()].GetReversalDirection().Value;
					if (num4 <= this._completedTrends[this._completedTrends.Keys.Max()].GetEndBarIndex() + this.InfoDisplayPeriod && ((value2 && num.ApproxCompare(this._maLower[0]) < 0) || (!value2 && num3.ApproxCompare(this._maUpper[0]) > 0)))
					{
						flag = true;
					}
					else
					{
						this._pendingReversalDirection = null;
					}
				}
				else
				{
					this._pendingReversalDirection = null;
				}
				if (this._pendingReversalDirection != null && this._barVolumeByIndex.ContainsKey(num4))
				{
					this._barVolumeByIndex[num4].ReversalDirection = this._pendingReversalDirection;
					this._reversalFilterLocked.Add(num4, flag);
					return;
				}
			}
			else
			{
				if (this._pendingReversalDirection == null || this._pendingReversalDirection.Value)
				{
					if (this._reversalWatch == null && num3.ApproxCompare(this._maUpper[0]) >= 0)
					{
						this._reversalWatch = new ReversalWatch(num4);
					}
					if (this._reversalWatch != null)
					{
						if (this._completedTrends.Count > 0 && this._completedTrends[this._completedTrends.Keys.Max()].GetEndBarIndex() >= this._reversalWatch.GetStartBarIndex() && !this._reversalWatch.IsConfirmed())
						{
							this._reversalWatch.SetConfirmed(true);
						}
						if (num3.ApproxCompare(this._maUpper[0]) >= 0)
						{
							this._pendingReversalDirection = new bool?(true);
							if (this._completedTrends.Count > 0 && num4 == this._completedTrends[this._completedTrends.Keys.Max()].GetEndBarIndex() + this.InfoDisplayPeriod + 1)
							{
								this._reversalWatch = new ReversalWatch(num4);
							}
						}
						else
						{
							if (!this._reversalWatch.IsConfirmed())
							{
								for (int i = this._reversalWatch.GetStartBarIndex(); i <= num4; i++)
								{
									if (this._barVolumeByIndex.ContainsKey(i))
									{
										this._barVolumeByIndex[i].ReversalDirection = null;
									}
								}
							}
							this._pendingReversalDirection = null;
							this._reversalWatch = null;
						}
					}
				}
				if (this._pendingReversalDirection == null || !this._pendingReversalDirection.Value)
				{
					if (this._reversalWatch == null && num.ApproxCompare(this._maLower[0]) <= 0)
					{
						this._reversalWatch = new ReversalWatch(num4);
					}
					if (this._reversalWatch != null)
					{
						if (this._completedTrends.Count > 0 && this._completedTrends[this._completedTrends.Keys.Max()].GetEndBarIndex() >= this._reversalWatch.GetStartBarIndex() && !this._reversalWatch.IsConfirmed())
						{
							this._reversalWatch.SetConfirmed(true);
						}
						if (num.ApproxCompare(this._maLower[0]) <= 0)
						{
							this._pendingReversalDirection = new bool?(false);
							if (this._completedTrends.Count > 0 && num4 == this._completedTrends[this._completedTrends.Keys.Max()].GetEndBarIndex() + this.InfoDisplayPeriod + 1)
							{
								this._reversalWatch = new ReversalWatch(num4);
							}
						}
						else
						{
							if (!this._reversalWatch.IsConfirmed())
							{
								for (int j = this._reversalWatch.GetStartBarIndex(); j <= num4; j++)
								{
									if (this._barVolumeByIndex.ContainsKey(j))
									{
										this._barVolumeByIndex[j].ReversalDirection = null;
									}
								}
							}
							this._pendingReversalDirection = null;
							this._reversalWatch = null;
						}
					}
				}
				if (this._pendingReversalDirection != null && this._barVolumeByIndex.ContainsKey(num4) && this._barVolumeByIndex[num4].ReversalDirection == null)
				{
					this._barVolumeByIndex[num4].ReversalDirection = new bool?(this._pendingReversalDirection.Value);
				}
			}
		}
		protected override void OnMarketData(MarketDataEventArgs e)
		{
			try
			{
					if (!this._useSecondaryVolumeSeries)
					{
						if (base.Bars.IsTickReplay)
						{
							if (e.MarketDataType == MarketDataType.Last)
							{
								this._lastAsk = e.Ask;
								this._lastBid = e.Bid;
								this._lastPrice = e.Price;
								double num = (double)e.Volume / 2.0;
								if (!this._barVolumeByIndex.ContainsKey(this._volumeBarKey))
								{
									this._barVolumeByIndex.Add(this._volumeBarKey, new BarVolumeState());
								}
								if (this._lastAsk > 0.0 && this._lastPrice.ApproxCompare(this._lastAsk) >= 0)
								{
									this._barVolumeByIndex[this._volumeBarKey].AddAskVolume(this.PriceToTickIndex(this._lastPrice), num);
									this._sessionVolume.AddAskVolume(this.PriceToTickIndex(this._lastPrice), num);
								}
								if (this._lastBid > 0.0 && this._lastPrice.ApproxCompare(this._lastBid) <= 0)
								{
									this._barVolumeByIndex[this._volumeBarKey].AddBidVolume(this.PriceToTickIndex(this._lastPrice), num);
									this._sessionVolume.AddBidVolume(this.PriceToTickIndex(this._lastPrice), num);
								}
							}
						}
						else if (e.MarketDataType == MarketDataType.Ask)
						{
							this._lastAsk = e.Price;
						}
						else if (e.MarketDataType == MarketDataType.Bid)
						{
							this._lastBid = e.Price;
						}
						else if (e.MarketDataType == MarketDataType.Last)
						{
							this._lastPrice = e.Price;
							double num2 = (double)e.Volume / 2.0;
							if (!this._barVolumeByIndex.ContainsKey(this._volumeBarKey))
							{
								this._barVolumeByIndex.Add(this._volumeBarKey, new BarVolumeState());
							}
							if (this._lastAsk > 0.0 && this._lastPrice.ApproxCompare(this._lastAsk) >= 0)
							{
								this._barVolumeByIndex[this._volumeBarKey].AddAskVolume(this.PriceToTickIndex(this._lastPrice), num2);
								this._sessionVolume.AddAskVolume(this.PriceToTickIndex(this._lastPrice), num2);
							}
							if (this._lastBid > 0.0 && this._lastPrice.ApproxCompare(this._lastBid) <= 0)
							{
								this._barVolumeByIndex[this._volumeBarKey].AddBidVolume(this.PriceToTickIndex(this._lastPrice), num2);
								this._sessionVolume.AddBidVolume(this.PriceToTickIndex(this._lastPrice), num2);
							}
						}
					}
			}
			catch (Exception ex)
			{
				this.PrintException(ex);
			}
		}
		private void RegisterMarker(SignalEvent signalEvent)
		{
			if (!this.MarkerEnabled)
			{
				return;
			}
			int num = signalEvent.GetBarIndex();
			if (!this._markersByBar.ContainsKey(num))
			{
				this._markersByBar.Add(num, signalEvent);
				return;
			}
			this._markersByBar[num] = signalEvent;
		}
		private void UpdateMaAndCloud()
		{
			double num = base.StdDev(base.Input, this.MAPeriod)[0];
			double num2 = this.ComputeMAValue(base.Input, this.MAType, this.MAPeriod);
			double num3;
			if (this.MASmoothingEnabled)
			{
				this._stochRawSeries[0] = num2;
				num3 = this.ComputeMAValue(this._stochRawSeries, ninZa_MAType.EMA, this.MASmoothingPeriod);
			}
			else
			{
				num3 = num2;
			}
			double num4 = base.Instrument.MasterInstrument.RoundToTickSize(num3 + this.MAOffset * num);
			double num5 = base.Instrument.MasterInstrument.RoundToTickSize(num3 - this.MAOffset * num);
			this._maUpper[0] = num4;
			this.MA[0] = (this._maCenter[0] = base.Instrument.MasterInstrument.RoundToTickSize(num3));
			this._maLower[0] = num5;
			if (this.CloudEnabled && this.CloudOpacity > 0)
			{
				double num6 = this.CloudOffset * num;
				for (int i = 0; i < this._cloudSeriesCount; i++)
				{
					this._cloudUpperSeries[i][0] = num4 + (double)i * num6;
					this._cloudLowerSeries[i][0] = num5 - (double)i * num6;
				}
			}
		}
		protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
		{
			try
			{
				if (this._isLive && this.SwitchedOn && !base.IsInHitTest)
				{
					base.OnRender(chartControl, chartScale);
					this.RenderSwingLines(chartScale);
					this._barDistance = chartControl.Properties.BarDistance - 10f;
					this.UpdateInfoPanelLayout(chartScale);
					if (!this.InfoEnabled || base.ChartControl.Properties.BarDistance < 4f)
					{
						this._infoYOffset = float.NaN;
					}
					for (int i = base.ChartBars.FromIndex; i <= Math.Min(base.CurrentBars[0], base.ChartBars.ToIndex); i++)
					{
						if (this.InfoEnabled && this._barVolumeByIndex.Count > 0 && this._barVolumeByIndex.ContainsKey(i))
						{
							this.RenderBarVolumeInfo(chartScale, i, this._barVolumeByIndex[i], (float)chartControl.GetXByBarIndex(base.ChartBars, i));
						}
						if (this.MarkerEnabled && this._useCustomMarkerRendering && this._markersByBar.Count > 0 && this._markersByBar.ContainsKey(i))
						{
							this.RenderCustomMarker(chartScale, this._markersByBar[i]);
						}
					}
					bool flag = float.IsNaN(this._infoYOffset) != float.IsNaN(this._infoYOffsetPrev);
					if (this.MarkerEnabled && !this._useCustomMarkerRendering && base.DrawObjects != null && base.DrawObjects.Count > 0 && flag)
					{
						foreach (IDrawingTool drawingTool in base.DrawObjects)
						{
							if (drawingTool.Tag.Contains("DDVEL") && drawingTool.ToString() == "NinjaTrader.NinjaScript.DrawingTools.Text" && (drawingTool.Tag.Contains("reversal.bull.") || drawingTool.Tag.Contains("reversal.bear.")))
							{
								NinjaTrader.NinjaScript.DrawingTools.Text text = (NinjaTrader.NinjaScript.DrawingTools.Text)drawingTool;
								int num = Convert.ToInt32(text.Anchor.SlotIndex);
								if (this._barVolumeByIndex.Count > 0 && this._barVolumeByIndex.ContainsKey(num) && this._barVolumeByIndex[num].ReversalDirection != null)
								{
									if (!this._markerTextYOffsetCache.ContainsKey(text))
									{
										this._markerTextYOffsetCache[text] = text.YPixelOffset;
									}
									int num2 = this._markerTextYOffsetCache[text];
									text.YPixelOffset = (float.IsNaN(this._infoYOffset) ? num2 : (num2 + (drawingTool.Tag.Contains("reversal.bull.") ? (-1) : 1) * 32));
								}
							}
						}
						this._infoYOffsetPrev = this._infoYOffset;
					}
				}
			}
			catch (Exception ex)
			{
				this.PrintException(ex);
			}
		}
		private void RenderSwingLines(ChartScale chartScale)
		{
			if (this._swingPoints == null || this._swingPoints.Count < 1)
			{
				return;
			}
			int fromIndex = base.ChartBars.FromIndex;
			int num = Math.Min(base.ChartBars.ToIndex, base.CurrentBars[0]);
			if (this._swingPoints.Count != 1)
			{
				int num2 = 0;
				int num3 = this._swingPoints.Count - 1;
				for (int i = 0; i < this._swingPoints.Count - 1; i++)
				{
					if (this._swingPoints[i].BarIndex > num && this._swingPoints[i + 1].BarIndex <= num)
					{
						num2 = i;
					}
					if (this._swingPoints[i].BarIndex >= fromIndex && this._swingPoints[i + 1].BarIndex < fromIndex)
					{
						num3 = i + 1;
						this.DrawSwingSegments(num2, num3, chartScale);
						this.DrawSwingPointEllipses(num2, num3, chartScale);
						return;
					}
				}
				this.DrawSwingSegments(num2, num3, chartScale);
				this.DrawSwingPointEllipses(num2, num3, chartScale);
				return;
			}
			if (this._swingPoints[0].BarIndex >= fromIndex && this._swingPoints[0].BarIndex <= num)
			{
				this.DrawSwingPointEllipses(0, 0, chartScale);
				return;
			}
		}
		private void DrawSwingSegments(int fromIndex, int toIndex, ChartScale chartScale)
		{
			if (!this.LineEnabled)
			{
				return;
			}
			if (this._swingPoints != null && this._swingPoints.Count >= 2)
			{
				SwingPoint swingPoint = this._swingPoints[fromIndex];
				for (int i = fromIndex + 1; i <= toIndex; i++)
				{
					SwingPoint swingPoint2 = this._swingPoints[i];
					Vector2 vector = this.SwingPointToVector(chartScale, swingPoint);
					Vector2 vector2 = this.SwingPointToVector(chartScale, swingPoint2);
					Stroke stroke = (swingPoint.IsCandidate ? this.LineStyleDeveloping : this.LineStyleFinished);
					double num;
					if (!this.SmartLineThicknessEnabled)
					{
						num = (double)stroke.Width;
					}
					else
					{
						num = Math.Max(this.SmartLineThicknessThinnest, Math.Min(swingPoint.SumVolPerLine * this.SmartLineThicknessStep, this.SmartLineThicknessThickest));
					}
					SharpDX.Direct2D1.Brush brush = (swingPoint.IsTop ? this._lineBrushUp : this._lineBrushDown).ToDxBrush(base.RenderTarget);
					SharpDX.Direct2D1.AntialiasMode antialiasMode = base.RenderTarget.AntialiasMode;
					base.RenderTarget.AntialiasMode = 0;
					base.RenderTarget.DrawLine(vector, vector2, brush, (float)num, stroke.StrokeStyle);
					base.RenderTarget.AntialiasMode = antialiasMode;
					brush.Dispose();
					swingPoint = swingPoint2;
				}
				return;
			}
		}
		private void DrawSwingPointEllipses(int fromIndex, int toIndex, ChartScale chartScale)
		{
			if (!this.SwingPointEnabled)
			{
				return;
			}
			if (this._swingPoints != null && this._swingPoints.Count >= 1)
			{
				float num = (float)Math.Max(base.ChartControl.BarWidth, 5.0);
				for (int i = fromIndex; i <= toIndex; i++)
				{
					SwingPoint swingPoint = this._swingPoints[i];
					if (swingPoint.Price.ApproxCompare(chartScale.MinValue) >= 0 && swingPoint.Price.ApproxCompare(chartScale.MaxValue) <= 0)
					{
						SharpDX.Direct2D1.Brush brush = (swingPoint.IsTop ? this.SwingPointTop : this.SwingPointBottom).ToDxBrush(base.RenderTarget);
						Vector2 vector = this.SwingPointToVector(chartScale, swingPoint);
						SharpDX.Direct2D1.Ellipse ellipse = new SharpDX.Direct2D1.Ellipse(vector, num, num);
						SharpDX.Direct2D1.AntialiasMode antialiasMode = base.RenderTarget.AntialiasMode;
						base.RenderTarget.AntialiasMode = 0;
						base.RenderTarget.FillEllipse(ellipse, brush);
						brush.Dispose();
						base.RenderTarget.AntialiasMode = antialiasMode;
					}
				}
				return;
			}
		}
		private Vector2 SwingPointToVector(ChartScale chartScale, SwingPoint swingPoint)
		{
			return new Vector2((float)base.ChartControl.GetXByBarIndex(base.ChartBars, swingPoint.BarIndex), (float)chartScale.GetYByValue(swingPoint.Price));
		}
		private void UpdateInfoPanelLayout(ChartScale chartScale)
		{
			double minValue = chartScale.MinValue;
			this._infoBaseTick = this.PriceToTickIndex(minValue);
			this._infoRowCount = Convert.ToInt32(Math.Ceiling((double)base.ChartPanel.H / 350.0));
			if (this._infoRowCount < 1)
			{
				this._infoRowCount = 1;
			}
			this._infoColumns = 1 + Math.Max(this._infoRowCount, this.GetPixelHeightForTicks(chartScale, this._infoBaseTick, this._infoBaseTick + 1));
		}
		private int GetPixelHeightForTicks(ChartScale chartScale, int tickLow, int tickHigh)
		{
			return Math.Abs(chartScale.GetYByValue((double)tickLow * base.TickSize) - chartScale.GetYByValue((double)tickHigh * base.TickSize));
		}
		private void RenderBarVolumeInfo(ChartScale chartScale, int barIndex, BarVolumeState barVolume, float x)
		{
			if (barVolume.ReversalDirection != null && base.ChartControl.Properties.BarDistance >= 4f)
			{
				int num = this.CompareVolumeStrength(Convert.ToInt64(barVolume.TotalAskVolume), Convert.ToInt64(barVolume.TotalBidVolume));
				Brush brush;
				if (num > 0)
				{
					brush = this._infoBrushPositive;
				}
				else if (num < 0)
				{
					brush = this._infoBrushNegative;
				}
				else
				{
					brush = this._infoBrushZero;
				}
				double volumeDelta = barVolume.VolumeDelta;
				string text = volumeDelta.ToString("N0");
				if (volumeDelta > 0.0)
				{
					text = "+" + text;
				}
				bool value = barVolume.ReversalDirection.Value;
				int num2 = 4;
				float num3;
				if (value)
				{
					num3 = (float)chartScale.GetYByValue(base.BarsArray[0].GetHigh(barIndex)) - (float)this._infoColumns / 2f - (float)this.InfoMargin + 1f - (float)num2;
				}
				else
				{
					num3 = (float)chartScale.GetYByValue(base.BarsArray[0].GetLow(barIndex)) + (float)this._infoColumns / 2f + (float)this.InfoMargin - 1f;
				}
				RectangleF rectangleF = new RectangleF(x - this._barDistance / 2f, num3, this._barDistance - 1f, (float)num2);
				SharpDX.Direct2D1.Brush brush2 = brush.ToDxBrush(base.RenderTarget);
				base.RenderTarget.DrawRectangle(rectangleF, brush2);
				if (num == 0)
				{
					base.RenderTarget.FillRectangle(rectangleF, brush2);
				}
				else
				{
					RectangleF rectangleF2 = new RectangleF(rectangleF.X, rectangleF.Y, rectangleF.Width, rectangleF.Height);
					float num4 = (float)(barVolume.TotalAskVolume + barVolume.TotalBidVolume);
					if (num > 0)
					{
						rectangleF2.Width = rectangleF.Width * (float)barVolume.TotalAskVolume / num4;
						rectangleF2.X += rectangleF.Width - rectangleF2.Width;
					}
					else
					{
						rectangleF2.Width = rectangleF.Width * (float)barVolume.TotalBidVolume / num4;
					}
					base.RenderTarget.FillRectangle(rectangleF2, brush2);
				}
				brush2.Dispose();
				int num5 = 8;
				float num6;
				if (value)
				{
					num6 = num3 - (float)num5;
				}
				else
				{
					num6 = num3 + (float)num2 + (float)num5;
				}
				int num7 = (value ? (-1) : 1);
				this.DrawText(text, this.InfoNumberFont, rectangleF.Center.X, num6, 0, num7, brush, this.ScreenDPI, base.RenderTarget);
				float height = this.ComputeTextSize(text, this.InfoNumberFont, this.ScreenDPI).Height;
				this._infoYOffset = num6 + (float)num7 * (height - 8f);
				return;
			}
		}
		private void RenderCustomMarker(ChartScale chartScale, SignalEvent signalEvent)
		{
			bool flag = signalEvent.IsBullish();
			SignalKind signalKind = signalEvent.GetSignalKind();
			bool flag2 = signalKind == SignalKind.TrendStart;
			bool flag3 = signalKind == SignalKind.Reversal;
			string text;
			if (flag2)
			{
				text = (flag ? this.MarkerStringUptrendStart : this.MarkerStringDowntrendStart);
			}
			else if (flag3)
			{
				text = (flag ? this.MarkerStringReversalBullish : this.MarkerStringReversalBearish);
			}
			else if (signalKind == SignalKind.Early)
			{
				text = (flag ? this.MarkerStringEarlyBullish : this.MarkerStringEarlyBearish);
			}
			else
			{
				text = (flag ? this.MarkerStringDeepBullish : this.MarkerStringDeepBearish);
			}
			text = this.FormatMarkerString(text);
			if (string.IsNullOrWhiteSpace(text))
			{
				return;
			}
			int num = signalEvent.GetBarIndex();
			if (!flag && base.Highs[0].GetValueAt(num).ApproxCompare(chartScale.MaxValue) >= 0)
			{
				return;
			}
			if (flag && base.Lows[0].GetValueAt(num).ApproxCompare(chartScale.MinValue) <= 0)
			{
				return;
			}
			Brush brush = (flag ? this.MarkerBrushBullish : this.MarkerBrushBearish);
			if (brush.IsTransparent())
			{
				return;
			}
			int num2 = (flag ? 1 : (-1));
			float num3 = (float)base.ChartControl.GetXByBarIndex(base.ChartBars, num);
			float num4;
			if (flag3 && !float.IsNaN(this._infoYOffset) && this._barVolumeByIndex.ContainsKey(num) && this._barVolumeByIndex[num].ReversalDirection != null)
			{
				num4 = this._infoYOffset + (float)(num2 * this.MarkerOffset);
			}
			else if (flag2)
			{
				num4 = (float)chartScale.GetYByValue(this.MA.GetValueAt(num));
			}
			else
			{
				num4 = (float)(chartScale.GetYByValue((flag ? base.Lows[0] : base.Highs[0]).GetValueAt(num)) + num2 * this.MarkerOffset);
			}
			this.DrawText(text, this.MarkerFont, num3, num4, 0, flag2 ? 0 : num2, brush, this.ScreenDPI, base.RenderTarget);
		}
		private void UpdateDevelopingSwing(int barIndex, double price)
		{
			if (this._swingPoints == null || this._swingPoints.Count < 1)
			{
				return;
			}
			if (!this._swingPoints[0].IsCandidate)
			{
				return;
			}
			this._swingPoints[0].Price = price;
			this._swingPoints[0].BarIndex = barIndex;
			if (this._swingPoints.Count > 1)
			{
				this._swingPoints[0].Length = Convert.ToInt32((this._swingPoints[0].Price - this._swingPoints[1].Price) / base.TickSize);
			}
		}
		private void CommitSwingPoint(SwingPoint swingPoint)
		{
			if (this._swingPoints == null)
			{
				return;
			}
			if (this._swingPoints.Count == 0)
			{
				swingPoint.Length = 0;
				this._swingPoints.Add(swingPoint);
				return;
			}
			this._swingPoints[0].IsCandidate = false;
			this._swingPoints.Insert(0, swingPoint);
			this._swingPoints[0].Length = Convert.ToInt32((this._swingPoints[0].Price - this._swingPoints[1].Price) / base.TickSize);
		}
		private int CompareVolumeStrength(long buyVolume, long sellVolume)
		{
			if (buyVolume == sellVolume)
			{
				return 0;
			}
			if (buyVolume > sellVolume)
			{
				if (buyVolume >= 50L && (double)(buyVolume - sellVolume) >= (double)buyVolume * 25.0 / 100.0)
				{
					return 2;
				}
				return 1;
			}
			else
			{
				if (sellVolume >= 50L && (double)(sellVolume - buyVolume) >= (double)sellVolume * 25.0 / 100.0)
				{
					return -2;
				}
				return -1;
			}
		}
		private void UpdateTickDirection()
		{
			if (base.CurrentBars[1] <= 0)
			{
				this._tickDirection = 0;
				return;
			}
			int num = base.Instrument.MasterInstrument.Compare(base.Closes[1][0], base.Closes[1][1]);
			if (num != 0)
			{
				this._tickDirection = num;
			}
		}
		private long VolumeToLong(double volume)
		{
			return Convert.ToInt64(volume);
		}
		private int PriceToTickIndex(double price)
		{
			return Convert.ToInt32(price / base.TickSize);
		}
		private void DrawBuiltinMarker(SignalEvent signalEvent)
		{
			if (!this._isLive || !this.MarkerEnabled)
			{
				return;
			}
			if (base.CurrentBar < base.BarsRequiredToPlot)
			{
				return;
			}
			SignalKind signalKind = signalEvent.GetSignalKind();
			bool flag = signalKind == SignalKind.TrendStart;
			bool flag2 = signalKind == SignalKind.Reversal;
			bool flag3 = signalKind == SignalKind.Early;
			bool flag4 = signalEvent.IsBullish();
			string text;
			if (flag)
			{
				text = "DDVEL.marker." + (flag4 ? "trend.bull." : "trend.bear.") + base.CurrentBar.ToString();
			}
			else if (flag2)
			{
				text = "DDVEL.marker." + (flag4 ? "reversal.bull." : "reversal.bear.") + base.CurrentBar.ToString();
			}
			else if (flag3)
			{
				text = "DDVEL.marker." + (flag4 ? "early.bull." : "early.bear.") + base.CurrentBar.ToString();
			}
			else
			{
				text = "DDVEL.marker." + (flag4 ? "deep.bull." : "deep.bear.") + base.CurrentBar.ToString();
			}
			Brush brush = (flag4 ? this.MarkerBrushBullish : this.MarkerBrushBearish);
			if (brush.IsTransparent())
			{
				return;
			}
			double num = (flag ? this.MA[0] : (flag4 ? base.Lows[0][0] : base.Highs[0][0]));
			string text2;
			if (flag)
			{
				text2 = (flag4 ? this.MarkerStringUptrendStart : this.MarkerStringDowntrendStart);
			}
			else if (flag2)
			{
				text2 = (flag4 ? this.MarkerStringReversalBullish : this.MarkerStringReversalBearish);
			}
			else if (signalKind == SignalKind.Early)
			{
				text2 = (flag4 ? this.MarkerStringEarlyBullish : this.MarkerStringEarlyBearish);
			}
			else
			{
				text2 = (flag4 ? this.MarkerStringDeepBullish : this.MarkerStringDeepBearish);
			}
			text2 = this.FormatMarkerString(text2);
			int num2 = Convert.ToInt32(this.ComputeTextSize(text2, this.MarkerFont, this.ScreenDPI).Height);
			int num3 = (flag4 ? (-1) : 1);
			int num4 = num3 * (this.MarkerOffset + num2 / 2);
			NinjaTrader.NinjaScript.DrawingTools.Text text3 = NinjaTrader.NinjaScript.DrawingTools.Draw.Text(this, text, base.IsAutoScale, text2, 0, num, flag ? 0 : num4, brush, this.MarkerFont, TextAlignment.Center, Brushes.Transparent, Brushes.Transparent, 0);
			if (!this._markerTextYOffsetCache.ContainsKey(text3))
			{
				this._markerTextYOffsetCache[text3] = text3.YPixelOffset;
			}
			if (flag2 && this._barVolumeByIndex.Count > 0 && this._barVolumeByIndex.ContainsKey(base.CurrentBar) && this._barVolumeByIndex[base.CurrentBar].ReversalDirection != null && !float.IsNaN(this._infoYOffset))
			{
				text3.YPixelOffset += num3 * 32;
			}
			if (!this.SwitchedOn)
			{
				text3.IsVisible = false;
			}
		}
		private void PrintException(Exception exception)
		{
			string text = "DDVEL: " + exception.ToString() + " (" + exception.StackTrace + ")";
			Print((object)text);
			Log(text, NinjaTrader.Cbi.LogLevel.Error);
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
			catch
			{
			}
			if (dpi < 99)
			{
				dpi = 99;
			}
			if (dpi > 500)
			{
				dpi = 500;
			}
			return dpi;
		}
		private string FormatMarkerString(string text)
		{
			if (string.IsNullOrWhiteSpace(text))
			{
				return string.Empty;
			}
			string[] parts = text.Trim().Split(new[] { '+' }, StringSplitOptions.RemoveEmptyEntries);
			for (int i = 0; i < parts.Length; i++)
			{
				parts[i] = parts[i].Trim();
			}
			return string.Join("\n", parts).Trim();
		}
		private SharpDX.Size2F ComputeTextSize(string text, SimpleFont font, int dpi)
		{
			if (font == null)
			{
				return new SharpDX.Size2F(0f, 12f);
			}
			if (string.IsNullOrEmpty(text))
			{
				return new SharpDX.Size2F(0f, 0f);
			}
			string[] lines = text.Split('\n');
			int maxLen = 0;
			for (int i = 0; i < lines.Length; i++)
			{
				if (lines[i].Length > maxLen)
				{
					maxLen = lines[i].Length;
				}
			}
			float lineHeight = (float)(font.Size * 1.5);
			float width = (float)(maxLen * font.Size * 0.7);
			float height = lineHeight * Math.Max(1, lines.Length);
			return new SharpDX.Size2F(width, height);
		}
		private void DrawText(string text, SimpleFont font, float x, float y, int angle, int direction, Brush wpfBrush, int dpi, SharpDX.Direct2D1.RenderTarget renderTarget)
		{
			if (renderTarget == null || font == null || string.IsNullOrEmpty(text) || wpfBrush == null || wpfBrush.IsTransparent())
			{
				return;
			}
			SharpDX.Size2F size = this.ComputeTextSize(text, font, dpi);
			float top;
			if (direction > 0)
			{
				top = y;
			}
			else if (direction < 0)
			{
				top = y - size.Height;
			}
			else
			{
				top = y - size.Height / 2f;
			}
			SharpDX.RectangleF rect = new SharpDX.RectangleF(x - size.Width / 2f, top, size.Width, size.Height);
			using (SharpDX.DirectWrite.TextFormat textFormat = new SharpDX.DirectWrite.TextFormat(Globals.DirectWriteFactory, font.Family.ToString(), (float)font.Size))
			{
				textFormat.TextAlignment = SharpDX.DirectWrite.TextAlignment.Center;
				textFormat.ParagraphAlignment = SharpDX.DirectWrite.ParagraphAlignment.Near;
				textFormat.WordWrapping = SharpDX.DirectWrite.WordWrapping.NoWrap;
				SharpDX.Direct2D1.Brush dxBrush = wpfBrush.ToDxBrush(renderTarget);
				renderTarget.DrawText(text, textFormat, rect, dxBrush);
				dxBrush.Dispose();
			}
		}
		private double ComputeMAValue(ISeries<double> input, ninZa_MAType maType, int period)
		{
			if (base.CurrentBar < 0 || period <= 0)
			{
				return input[0];
			}
			if (maType == ninZa_MAType.SMA)
			{
				return SMA(input, period)[0];
			}
			return EMA(input, period)[0];
		}
		private bool _conditionTrendStart;
		private bool _conditionReversal;
		private bool _conditionEarly;
		private bool _conditionDeep;
		private bool _markerEnabled;
		private DDVEL_RenderingMethod _markerRenderingMethod;
		private Brush _markerBrushBullish;
		private Brush _markerBrushBearish;
		private string _markerStringUptrendStart;
		private string _markerStringDowntrendStart;
		private string _markerStringReversalBullish;
		private string _markerStringReversalBearish;
		private string _markerStringEarlyBullish;
		private string _markerStringEarlyBearish;
		private string _markerStringDeepBullish;
		private string _markerStringDeepBearish;
		private SimpleFont _markerFont;
		private int _markerOffset;
		private int _alertBlockingSeconds;
		private bool _logoEnabled;
		private bool _instructionEnabled;
		private int _screenDpi;
		private bool _plotEnabled;
		private Brush _plotUptrend;
		private Brush _plotDowntrend;
		private bool _cloudEnabled;
		private Brush _cloudUpperNear;
		private Brush _cloudUpperFar;
		private Brush _cloudLowerNear;
		private Brush _cloudLowerFar;
		private int _cloudOpacity;
		private int _cloudQuantityPerSide;
		private double _cloudOffset;
		private Brush _infoDeltaPositive;
		private Brush _infoDeltaNegative;
		private Brush _infoDeltaZero;
		private int _infoDeltaOpacity;
		private SimpleFont _infoNumberFont;
		private int _infoDisplayPeriod;
		private int _infoMargin;
		private bool _swingPointEnabled;
		private Brush _swingPointTop;
		private Brush _swingPointBottom;
		private bool _lineEnabled;
		private Brush _lineColorUp;
		private Brush _lineColorDown;
		private int _lineOpacity;
		private Stroke _lineStyleFinished;
		private Stroke _lineStyleDeveloping;
		private bool _smartLineThicknessEnabled;
		private double _smartLineThicknessStep;
		private double _smartLineThicknessThinnest;
		private double _smartLineThicknessThickest;
		private ninZa_MAType _maType;
		private int _maPeriod;
		private bool _maSmoothingEnabled;
		private int _maSmoothingPeriod;
		private double _maOffset;
		private bool _signalReversalFilterEnabled;
		private int _signalEarlyDeepCalculatingPeriod;
		private int _signalFindingPeriod;
		private int _signalSplit;
		private int _thresholdOverbought;
		private int _thresholdOversold;
		private bool _toggleEnabled;
		private Brush _toggleBackBrushOn;
		private Brush _toggleBackBrushOff;
		private string _button1Text;
		private string _button2Text;
		private Brush _buttonTextBrush;
		private int _buttonTextSize;
		private Brush _toggleDragBrush;
		private ninZa_TextPosition _togglePositionAlignment;
		private double _togglePositionMarginLeft;
		private double _togglePositionMarginTop;
		private double _togglePositionMarginRight;
		private double _togglePositionMarginBottom;
		private int _indicatorZOrder;
		private string _userNote;
		private bool _switchedOn;
		private bool _infoEnabled;
		private bool _useCustomMarkerRendering;
		private Series<double> _stochRawSeries;
		private bool _useSecondaryVolumeSeries;
		private int _tickDirection;
		private int _volumeBarKey;
		private Dictionary<int, BarVolumeState> _barVolumeByIndex = new Dictionary<int, BarVolumeState>();
		private BarVolumeState _sessionVolume = new BarVolumeState();
		private double _sessionLow;
		private double _sessionHigh;
		private bool _isLongTimeframeBars;
		private Series<double> _maUpper;
		private Series<double> _maCenter;
		private Series<double> _maLower;
		private Dictionary<int, bool> _reversalFilterLocked;
		private bool? _pendingReversalDirection;
		private Dictionary<NinjaTrader.NinjaScript.DrawingTools.Text, int> _markerTextYOffsetCache = new Dictionary<NinjaTrader.NinjaScript.DrawingTools.Text, int>();
		private bool _trendIsBullish;
		private Brush _infoBrushPositive;
		private Brush _infoBrushNegative;
		private Brush _infoBrushZero;
		private double _stochRange;
		private double _stochFullRange;
		private bool _stochCrossUp;
		private int _stochZone;
		private Series<double> _earlySignalRaw;
		private Series<double> _deepSignalRaw;
		private int _lastCloudBar = -1;
		private List<Series<double>> _cloudUpperSeries;
		private List<Series<double>> _cloudLowerSeries;
		private int _cloudSeriesCount;
		private ISeries<double> _atrSeries;
		private double _swingVolumeSum;
		private List<SwingPoint> _swingPoints;
		private int _swingDirection;
		private int _swingCandidateBar;
		private double _swingHighPrice;
		private double _swingLowPrice;
		private int _swingState;
		private Brush _lineBrushUp;
		private Brush _lineBrushDown;
		private bool _isLive;
		private Dictionary<int, SignalEvent> _markersByBar;
		private SortedList<int, TrendSegment> _activeTrends;
		private SortedList<int, TrendSegment> _completedTrends;
		private SortedList<int, bool> _trendReversalFlags;
		private ReversalWatch _reversalWatch;
		private double _lastBid;
		private double _lastAsk;
		private double _lastPrice;
		private int _infoBaseTick;
		private int _infoRowCount;
		private int _infoColumns;
		private float _barDistance;
		private float _infoYOffset = float.NaN;
		private float _infoYOffsetPrev = float.NaN;
		private sealed class PriceLevelVolume
		{
			internal PriceLevelVolume(double askVolume, double bidVolume)
			{
				this.AskVolume = askVolume;
				this.BidVolume = bidVolume;
			}
			internal double AskVolume;
			internal double BidVolume;
		}
		private sealed class TrendSegment
		{
			public TrendSegment(int startBarIndex = -1, bool isFilterLocked = false, int signalBarIndex = -1, bool? reversalDirection = null)
			{
				this.SetStartBarIndex(startBarIndex);
				this.SetFilterLocked(isFilterLocked);
				this.SetSignalBarIndex(signalBarIndex);
				this.SetEndBarIndex(-1);
				this.SetReversalDirection(reversalDirection);
			}
			public int GetStartBarIndex() { return this._startBarIndex; }
			public void SetStartBarIndex(int startBarIndex) { this._startBarIndex = startBarIndex; }
			public bool IsFilterLocked() { return this._isFilterLocked; }
			public void SetFilterLocked(bool isFilterLocked) { this._isFilterLocked = isFilterLocked; }
			public int GetSignalBarIndex() { return this._signalBarIndex; }
			public void SetSignalBarIndex(int signalBarIndex) { this._signalBarIndex = signalBarIndex; }
			public int GetEndBarIndex() { return this._endBarIndex; }
			public void SetEndBarIndex(int endBarIndex) { this._endBarIndex = endBarIndex; }
			public bool? GetReversalDirection() { return this._reversalDirection; }
			public void SetReversalDirection(bool? reversalDirection) { this._reversalDirection = reversalDirection; }
			private int _startBarIndex;
			private bool _isFilterLocked;
			private int _signalBarIndex;
			private int _endBarIndex;
			private bool? _reversalDirection;
		}
		private sealed class SignalEvent
		{
			public SignalEvent(bool isBullish, int barIndex, SignalKind signalKind)
			{
				this.SetBullish(isBullish);
				this.SetBarIndex(barIndex);
				this.SetSignalKind(signalKind);
			}
			public bool IsBullish() { return this._isBullish; }
			public void SetBullish(bool isBullish) { this._isBullish = isBullish; }
			public int GetBarIndex() { return this._barIndex; }
			public void SetBarIndex(int barIndex) { this._barIndex = barIndex; }
			public SignalKind GetSignalKind() { return this._signalKind; }
			public void SetSignalKind(SignalKind signalKind) { this._signalKind = signalKind; }
			private bool _isBullish;
			private int _barIndex;
			private SignalKind _signalKind;
		}
		private sealed class ReversalWatch
		{
			public ReversalWatch(int startBarIndex)
			{
				this.SetStartBarIndex(startBarIndex);
				this.SetConfirmed(false);
			}
			public int GetStartBarIndex() { return this._startBarIndex; }
			public void SetStartBarIndex(int startBarIndex) { this._startBarIndex = startBarIndex; }
			public bool IsConfirmed() { return this._isConfirmed; }
			public void SetConfirmed(bool isConfirmed) { this._isConfirmed = isConfirmed; }
			private int _startBarIndex;
			private bool _isConfirmed;
		}
		private enum SignalKind
		{
			TrendStart = 0,
			Reversal = 1,
			Early = 2,
			Deep = 3
		}
		
		public enum DDVEL_RenderingMethod
		{
			Builtin,
			Custom
		}
		
		private sealed class BarVolumeState
		{
			internal BarVolumeState()
			{
				this._volumeByTick = new Dictionary<int, PriceLevelVolume>();
				this.TotalAskVolume = 0.0;
				this.TotalBidVolume = 0.0;
				this.VolumeDelta = 0.0;
				this.MaxVolumeTickIndex = 0;
				this.MaxAskTickIndex = 0;
				this.MaxBidTickIndex = 0;
				this.MinTickIndex = int.MaxValue;
				this.MaxTickIndex = 0;
				this.MaxTotalVolume = 0.0;
				this.MaxAskVolume = 0.0;
				this.MaxBidVolume = 0.0;
				this.ReversalDirection = null;
			}
			internal PriceLevelVolume GetPriceLevel(int tickIndex)
			{
				if (this._volumeByTick.ContainsKey(tickIndex))
					return this._volumeByTick[tickIndex];
				return new PriceLevelVolume(0.0, 0.0);
			}
			internal double GetTotalVolumeAt(int tickIndex) { return this.GetPriceLevel(tickIndex).AskVolume + this.GetPriceLevel(tickIndex).BidVolume; }
			internal double GetAskVolumeAt(int tickIndex) { return this.GetPriceLevel(tickIndex).AskVolume; }
			internal double GetBidVolumeAt(int tickIndex) { return this.GetPriceLevel(tickIndex).BidVolume; }
			internal void AddAskVolume(int tickIndex, double volume)
			{
				if (this._volumeByTick.ContainsKey(tickIndex))
					this._volumeByTick[tickIndex].AskVolume += volume;
				else
					this._volumeByTick.Add(tickIndex, new PriceLevelVolume(volume, 0.0));
				this.TotalAskVolume += volume;
				this.VolumeDelta = this.TotalAskVolume - this.TotalBidVolume;
				if (this.GetTotalVolumeAt(tickIndex) > this.MaxTotalVolume)
				{
					this.MaxVolumeTickIndex = tickIndex;
					this.MaxTotalVolume = this.GetTotalVolumeAt(tickIndex);
				}
				if (this.GetAskVolumeAt(tickIndex) > this.MaxAskVolume)
				{
					this.MaxAskTickIndex = tickIndex;
					this.MaxAskVolume = this.GetAskVolumeAt(tickIndex);
				}
				this.MinTickIndex = Math.Min(tickIndex, this.MinTickIndex);
				this.MaxTickIndex = Math.Max(tickIndex, this.MaxTickIndex);
			}
			internal void AddBidVolume(int tickIndex, double volume)
			{
				if (this._volumeByTick.ContainsKey(tickIndex))
					this._volumeByTick[tickIndex].BidVolume += volume;
				else
					this._volumeByTick.Add(tickIndex, new PriceLevelVolume(0.0, volume));
				this.TotalBidVolume += volume;
				this.VolumeDelta = this.TotalAskVolume - this.TotalBidVolume;
				if (this.GetTotalVolumeAt(tickIndex) > this.MaxTotalVolume)
				{
					this.MaxVolumeTickIndex = tickIndex;
					this.MaxTotalVolume = this.GetTotalVolumeAt(tickIndex);
				}
				if (this.GetBidVolumeAt(tickIndex) > this.MaxBidVolume)
				{
					this.MaxBidTickIndex = tickIndex;
					this.MaxBidVolume = this.GetBidVolumeAt(tickIndex);
				}
				this.MinTickIndex = Math.Min(tickIndex, this.MinTickIndex);
				this.MaxTickIndex = Math.Max(tickIndex, this.MaxTickIndex);
			}
			internal void Clear() { this._volumeByTick.Clear(); }
			private Dictionary<int, PriceLevelVolume> _volumeByTick;
			internal int MaxVolumeTickIndex;
			internal int MaxAskTickIndex;
			internal int MaxBidTickIndex;
			internal int MinTickIndex;
			internal int MaxTickIndex;
			internal bool? ReversalDirection;
			internal double TotalAskVolume;
			internal double TotalBidVolume;
			internal double VolumeDelta;
			internal double MaxTotalVolume;
			internal double MaxAskVolume;
			internal double MaxBidVolume;
		}
		public class SwingPoint
		{
			public SwingPoint(int barIndex, double price, bool isHigh, bool isCandidate)
			{
				this.BarIndex = barIndex;
				this.Price = price;
				this.IsTop = isHigh;
				this.IsCandidate = isCandidate;
				this.SumVolPerLine = 0.0;
			}
			public int BarIndex
			{
				get
				{
					return this._barIndex;
				}
				set
				{
					this._barIndex = value;
				}
			}
			public double Price
			{
				get
				{
					return this._price;
				}
				set
				{
					this._price = value;
				}
			}
			public int Length
			{
				get
				{
					return this._length;
				}
				set
				{
					this._length = value;
				}
			}
			public bool IsTop
			{
				get
				{
					return this._isTop;
				}
				set
				{
					this._isTop = value;
				}
			}
			public bool IsCandidate
			{
				get
				{
					return this._isCandidate;
				}
				set
				{
					this._isCandidate = value;
				}
			}
			public double SumVolPerLine
			{
				get
				{
					return this._sumVolPerLine;
				}
				set
				{
					this._sumVolPerLine = value;
				}
			}
			private int _barIndex;
			private double _price;
			private int _length;
			private bool _isTop;
			private bool _isCandidate;
			private double _sumVolPerLine;
		}
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private DimDim.DDVEL[] cacheDDVEL;
		public DimDim.DDVEL DDVEL(ninZa_MAType mAType, int mAPeriod, bool mASmoothingEnabled, int mASmoothingPeriod, double mAOffset, bool signalReversalFilterEnabled, int signalEarlyDeepCalculatingPeriod, int signalFindingPeriod, int signalSplit, int thresholdOverbought, int thresholdOversold)
		{
			return DDVEL(Input, mAType, mAPeriod, mASmoothingEnabled, mASmoothingPeriod, mAOffset, signalReversalFilterEnabled, signalEarlyDeepCalculatingPeriod, signalFindingPeriod, signalSplit, thresholdOverbought, thresholdOversold);
		}

		public DimDim.DDVEL DDVEL(ISeries<double> input, ninZa_MAType mAType, int mAPeriod, bool mASmoothingEnabled, int mASmoothingPeriod, double mAOffset, bool signalReversalFilterEnabled, int signalEarlyDeepCalculatingPeriod, int signalFindingPeriod, int signalSplit, int thresholdOverbought, int thresholdOversold)
		{
			if (cacheDDVEL != null)
				for (int idx = 0; idx < cacheDDVEL.Length; idx++)
					if (cacheDDVEL[idx] != null && cacheDDVEL[idx].MAType == mAType && cacheDDVEL[idx].MAPeriod == mAPeriod && cacheDDVEL[idx].MASmoothingEnabled == mASmoothingEnabled && cacheDDVEL[idx].MASmoothingPeriod == mASmoothingPeriod && cacheDDVEL[idx].MAOffset == mAOffset && cacheDDVEL[idx].SignalReversalFilterEnabled == signalReversalFilterEnabled && cacheDDVEL[idx].SignalEarlyDeepCalculatingPeriod == signalEarlyDeepCalculatingPeriod && cacheDDVEL[idx].SignalFindingPeriod == signalFindingPeriod && cacheDDVEL[idx].SignalSplit == signalSplit && cacheDDVEL[idx].ThresholdOverbought == thresholdOverbought && cacheDDVEL[idx].ThresholdOversold == thresholdOversold && cacheDDVEL[idx].EqualsInput(input))
						return cacheDDVEL[idx];
			return CacheIndicator<DimDim.DDVEL>(new DimDim.DDVEL(){ MAType = mAType, MAPeriod = mAPeriod, MASmoothingEnabled = mASmoothingEnabled, MASmoothingPeriod = mASmoothingPeriod, MAOffset = mAOffset, SignalReversalFilterEnabled = signalReversalFilterEnabled, SignalEarlyDeepCalculatingPeriod = signalEarlyDeepCalculatingPeriod, SignalFindingPeriod = signalFindingPeriod, SignalSplit = signalSplit, ThresholdOverbought = thresholdOverbought, ThresholdOversold = thresholdOversold }, input, ref cacheDDVEL);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.DimDim.DDVEL DDVEL(ninZa_MAType mAType, int mAPeriod, bool mASmoothingEnabled, int mASmoothingPeriod, double mAOffset, bool signalReversalFilterEnabled, int signalEarlyDeepCalculatingPeriod, int signalFindingPeriod, int signalSplit, int thresholdOverbought, int thresholdOversold)
		{
			return indicator.DDVEL(Input, mAType, mAPeriod, mASmoothingEnabled, mASmoothingPeriod, mAOffset, signalReversalFilterEnabled, signalEarlyDeepCalculatingPeriod, signalFindingPeriod, signalSplit, thresholdOverbought, thresholdOversold);
		}

		public Indicators.DimDim.DDVEL DDVEL(ISeries<double> input , ninZa_MAType mAType, int mAPeriod, bool mASmoothingEnabled, int mASmoothingPeriod, double mAOffset, bool signalReversalFilterEnabled, int signalEarlyDeepCalculatingPeriod, int signalFindingPeriod, int signalSplit, int thresholdOverbought, int thresholdOversold)
		{
			return indicator.DDVEL(input, mAType, mAPeriod, mASmoothingEnabled, mASmoothingPeriod, mAOffset, signalReversalFilterEnabled, signalEarlyDeepCalculatingPeriod, signalFindingPeriod, signalSplit, thresholdOverbought, thresholdOversold);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.DimDim.DDVEL DDVEL(ninZa_MAType mAType, int mAPeriod, bool mASmoothingEnabled, int mASmoothingPeriod, double mAOffset, bool signalReversalFilterEnabled, int signalEarlyDeepCalculatingPeriod, int signalFindingPeriod, int signalSplit, int thresholdOverbought, int thresholdOversold)
		{
			return indicator.DDVEL(Input, mAType, mAPeriod, mASmoothingEnabled, mASmoothingPeriod, mAOffset, signalReversalFilterEnabled, signalEarlyDeepCalculatingPeriod, signalFindingPeriod, signalSplit, thresholdOverbought, thresholdOversold);
		}

		public Indicators.DimDim.DDVEL DDVEL(ISeries<double> input , ninZa_MAType mAType, int mAPeriod, bool mASmoothingEnabled, int mASmoothingPeriod, double mAOffset, bool signalReversalFilterEnabled, int signalEarlyDeepCalculatingPeriod, int signalFindingPeriod, int signalSplit, int thresholdOverbought, int thresholdOversold)
		{
			return indicator.DDVEL(input, mAType, mAPeriod, mASmoothingEnabled, mASmoothingPeriod, mAOffset, signalReversalFilterEnabled, signalEarlyDeepCalculatingPeriod, signalFindingPeriod, signalSplit, thresholdOverbought, thresholdOversold);
		}
	}
}

#endregion
