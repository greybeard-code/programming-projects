#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Threading;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Core;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.Data;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.NinjaScript;
using NinjaTrader.Gui.Tools;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.DrawingTools;
using NinjaTrader.NinjaScript.Indicators;
using SharpDX;
using SharpDX.Direct2D1;
// Disambiguate 'Brush': both SharpDX.Direct2D1 and System.Windows.Media define it.
// All D2D1 concrete types (LinearGradientBrush, SolidColorBrush, etc.) are
// referenced by their fully-qualified SharpDX.Direct2D1.* names in drawing code,
// so these two aliases are the only ones needed.
using Brush   = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
#endregion

// ============================================================
//  KingPanaZilla indicator suite — combined single-file build
//  Contains: gbKingOrderBlock, gbPANAKanal, gbThunderZilla,
//             gbKingPanaZilla (composite signal indicator)
//
//  Install: copy this single file to the NinjaTrader 8 custom
//           indicators folder and compile.
//  See README.md for full documentation.
// ============================================================

// ==== Global-scope types (referenced by generated factory methods) ====
public enum gbThunderZillaMAType
{
	DEMA = 0,
	EMA = 1,
	HMA = 2,
	LinReg = 3,
	SMA = 4,
	TEMA = 5,
	TMA = 6,
	VWMA = 7,
	WMA = 8,
	WilderMA = 9,
	ZLEMA = 10
}

public enum gbThunderZilla_RenderingMethod
{
	Custom,
	Builtin
}

public class gbThunderZilla_SoundConverter : TypeConverter
{
	public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
	{
		if (context != null)
		{
			List<string> list = new List<string>();
			FileInfo[] files = new DirectoryInfo(Globals.InstallDir + "sounds").GetFiles("*.wav");
			foreach (FileInfo fileInfo in files)
			{
				list.Add(fileInfo.Name);
			}
			return new StandardValuesCollection(list);
		}
		return null;
	}

	public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
	{
		return true;
	}
}

namespace NinjaTrader.NinjaScript.Indicators.GreyBeard.KingPanaZilla
{

// ==== gbKingOrderBlock ===========================================

public enum gbKingOrderBlockTextPosition
{
	BottomLeft = 0,
	BottomRight = 1,
	Center = 2,
	TopLeft = 3,
	TopRight = 4
}

public enum gbKingOrderBlock_MarkerRenderingMethod
{
	Custom = 0,
	Builtin = 1
}

public enum gbKingOrderBlock_SwingPointRenderingMethod
{
	Custom = 0,
	Builtin = 1
}

public enum gbKingOrderBlock_SwingPointDisplayMode
{
	Smart = 0,
	All = 1,
	Disabled = 2
}

public enum gbKingOrderBlock_BosChochDisplayMode
{
	Smart = 0,
	All = 1,
	Disabled = 2
}

public class gbKingOrderBlock_SoundConverter : TypeConverter
{
	public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
	{
		if (context != null)
		{
			List<string> list = new List<string>();
			FileInfo[] files = new DirectoryInfo(NinjaTrader.Core.Globals.InstallDir + "sounds").GetFiles("*.wav");
			foreach (FileInfo fileInfo in files)
				list.Add(fileInfo.Name);
			return new StandardValuesCollection(list);
		}
		return null;
	}

	public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
	{
		return true;
	}
}

[CategoryOrder("Alerts", 1000040)]
[CategoryOrder("Special", 1000060)]
[CategoryOrder("Graphics", 1000020)]
[CategoryOrder("Critical", 1000070)]
[CategoryOrder("General", 1000010)]
[CategoryOrder("Gradient", 1000030)]
public class gbKingOrderBlock : Indicator
{
	private class MarkerInfo
	{
		public int BarIndex { get; set; }

		public bool IsBullish { get; set; }

		public SignalType SignalType { get; set; }

		public MarkerInfo(int barIndex, bool isBullish, SignalType signalType)
		{
			BarIndex = barIndex;
			IsBullish = isBullish;
			SignalType = signalType;
		}
	}

	public class SwingPoint : BackupExtension
	{
		public bool IsTop { get; set; }

		public double Price { get; set; }

		public int BarStart { get; set; }

		public int BarEnd { get; set; }

		[BackupProperties]
		public bool BackupIsBroken { get; set; }

		public bool IsBroken { get; set; }

		[BackupProperties]
		public bool PrevAllowDraw { get; set; }

		public bool AllowDraw { get; set; }

		[BackupProperties]
		public bool BackupIsHasOrderBlock { get; set; }

		public bool IsHasOrderBlock { get; set; }

		public SwingPoint(bool isTop, double price, int barStart, int barEnd, bool isBroken = false, bool isHasOrderBlock = false)
		{
			IsTop = isTop;
			Price = price;
			BarStart = barStart;
			BarEnd = barEnd;
			IsBroken = isBroken;
			IsHasOrderBlock = isHasOrderBlock;
		}
	}

	public enum BosChochType
	{
		Bos,
		Choch
	}

	public class BosChochInfo : BackupExtension
	{
		public bool IsTop { get; set; }

		public double Price { get; set; }

		public int BarStart { get; set; }

		public int BarEnd { get; set; }

		[BackupProperties]
		public bool BackupIsHasOrderBlock { get; set; }

		public bool IsHasOrderBlock { get; set; }

		public BosChochType BosChochType { get; set; }

		public BosChochInfo(bool isTop, double price, int barStart, BosChochType bosChochType, int barEnd = -1, bool isBroken = false, bool isHasOrderBlock = false)
		{
			IsTop = isTop;
			Price = price;
			BarStart = barStart;
			BosChochType = bosChochType;
			BarEnd = barEnd;
			IsHasOrderBlock = isHasOrderBlock;
		}
	}

	public class ZoneInfo : BackupExtension
	{
		public bool IsTop { get; set; }

		public int BarStart { get; set; }

		public int BarEnd { get; set; }

		public bool IsBroken { get; set; }

		public double PriceTop { get; set; }

		public double PriceBottom { get; set; }

		public int Sign
		{
			get
			{
				if (!IsTop)
				{
					return 1;
				}
				return -1;
			}
		}

		public ZoneInfo(bool isTop, bool isBroken = false, int barStart = -1, int barEnd = -1, double priceTop = -1.0, double priceBottom = -1.0)
		{
			IsTop = isTop;
			IsBroken = isBroken;
			BarStart = barStart;
			BarEnd = barEnd;
			PriceTop = priceTop;
			PriceBottom = priceBottom;
		}

		public ZoneInfo()
		{
		}
	}

	public class OrderBlockInfo : ZoneInfo
	{
		public double PriceBroken
		{
			get
			{
				if (!base.IsTop)
				{
					return base.PriceBottom;
				}
				return base.PriceTop;
			}
		}

		public double PriceSignal
		{
			get
			{
				if (!base.IsTop)
				{
					return base.PriceTop;
				}
				return base.PriceBottom;
			}
		}

		[BackupProperties]
		public int BackupCountReturnSignal { get; set; }

		public int CountReturnSignal { get; set; }

		public OrderBlockInfo(bool isTop, bool isBroken = false, int barStart = -1, int barEnd = -1, double priceTop = -1.0, double priceBottom = -1.0, int countReturnSignal = 0)
			: base(isTop, isBroken, barStart, barEnd, priceTop, priceBottom)
		{
			CountReturnSignal = countReturnSignal;
		}

		public OrderBlockInfo()
		{
		}
	}

	public class ImbalanceInfo : ZoneInfo
	{
		[BackupProperties]
		public bool BackupIsFixed { get; set; }

		public bool IsFixed { get; set; }

		[BackupProperties]
		public bool BackupIsBroken { get; set; }

		[BackupProperties]
		public int BackupBarEnd { get; set; }

		[BackupProperties]
		public double BackupPriceTop { get; set; }

		[BackupProperties]
		public double BackupPriceBottom { get; set; }

		public double PriceBroken
		{
			get
			{
				if (!base.IsTop)
				{
					return base.PriceTop;
				}
				return base.PriceBottom;
			}
		}

		public ImbalanceInfo(bool isTop, bool isBroken = false, int barStart = -1, int barEnd = -1, double priceTop = -1.0, double priceBottom = -1.0, bool isFixed = false, bool isActive = true)
			: base(isTop, isBroken, barStart, barEnd, priceTop, priceBottom)
		{
			IsFixed = isFixed;
		}

		public ImbalanceInfo()
		{
		}
	}

	public class MarubozuInfo
	{
		public double PriceTop { get; set; }

		public double PriceBottom { get; set; }

		public MarubozuInfo(double priceTop, double priceBottom)
		{
			PriceTop = priceTop;
			PriceBottom = priceBottom;
		}
	}

	private class ValueData
	{
		public double MinValue { get; set; }

		public double MaxValue { get; set; }

		public ValueData()
		{
			MinValue = (MaxValue = -1.0);
		}

		public void Add(double price)
		{
			if (price.ApproxCompare(0.0) > 0)
			{
				if (MinValue.ApproxCompare(0.0) >= 0 && MaxValue.ApproxCompare(0.0) >= 0)
				{
					MinValue = Math.Min(MinValue, price);
					MaxValue = Math.Max(MaxValue, price);
				}
				else
				{
					double minValue = (MaxValue = price);
					MinValue = minValue;
				}
			}
		}
	}

	[AttributeUsage(AttributeTargets.Property)]
	public class BackupPropertiesAttribute : Attribute
	{
	}

	public class BackupExtension
	{
		public void BackupProperties(Dictionary<Type, PropertyInfo[]> cachePropertyInfo)
		{
			BackupOrRevertProperties(isBackup: true, cachePropertyInfo);
		}

		public void RevertProperties(Dictionary<Type, PropertyInfo[]> cachePropertyInfo)
		{
			BackupOrRevertProperties(isBackup: false, cachePropertyInfo);
		}

		public void BackupOrRevertProperties(bool isBackup, Dictionary<Type, PropertyInfo[]> cachePropertyInfo)
		{
			if (!cachePropertyInfo.TryGetValue(GetType(), out var value))
			{
				value = GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);
				cachePropertyInfo[GetType()] = value;
			}
			else
			{
				value = cachePropertyInfo[GetType()];
			}
			PropertyInfo[] array = value;
			foreach (PropertyInfo propertyInfo in array)
			{
				if (!Attribute.IsDefined(propertyInfo, typeof(BackupPropertiesAttribute)))
				{
					continue;
				}
				PropertyInfo property = GetType().GetProperty(propertyInfo.Name.Replace("Backup", ""));
				if (property != null)
				{
					if (isBackup)
					{
						propertyInfo.SetValue(this, property.GetValue(this));
					}
					else
					{
						property.SetValue(this, propertyInfo.GetValue(this));
					}
				}
			}
		}
	}

	private enum SignalType
	{
		Return = 1,
		Breakout
	}

	private const string toolTipSpace = "  ";

	private float barOutlineWidth;

	private double orderBlockOffsetDifferenceDirection;

	private double orderBlockOffsetSameDirection;

	private bool isMarkerCustomRenderingMethod;

	private bool isSwingPointCustomRenderingMethod;

	private bool isSwingPointSmartDisplayMode;

	private bool isOnBarCloseMode;

	private bool isBosChochSmartDisplayMode;

	private bool flagNewImbalance;

	private bool flagNewOrderBlock;

	private bool flagNewMaker;

	private bool flagNewSwingPoint;

	private bool flagNewBosChochTop;

	private bool flagNewBosChochBottom;

	private SortedList<int, SwingPoint> listSwingsTop;

	private SortedList<int, SwingPoint> listSwingsBottom;

	private SortedList<int, BosChochInfo> listBosChochInfoTop;

	private SortedList<int, BosChochInfo> listBosChochInfoBottom;

	private SortedList<int, OrderBlockInfo> listOrderBlockActive;

	private SortedList<int, OrderBlockInfo> listOrderBlockInactive;

	private SortedList<int, ImbalanceInfo> listImbalanceActive;

	private SortedList<int, ImbalanceInfo> listImbalanceInactive;

	private SortedList<int, MarkerInfo> listMarkers;

	private SharpDX.Direct2D1.GradientStop[] imbalanceActiveTopGradientStop;

	private SharpDX.Direct2D1.GradientStop[] imbalanceActiveBottomGradientStop;

	private SharpDX.Direct2D1.GradientStop[] imbalanceInactiveTopGradientStop;

	private SharpDX.Direct2D1.GradientStop[] imbalanceInactiveBottomGradientStop;

	private SharpDX.Direct2D1.GradientStop[] orderBlockActiveTopGradientStop;

	private SharpDX.Direct2D1.GradientStop[] orderBlockActiveBottomGradientStop;

	private SharpDX.Direct2D1.GradientStop[] orderBlockInactiveTopGradientStop;

	private SharpDX.Direct2D1.GradientStop[] orderBlockInactiveBottomGradientStop;

	private System.Windows.Media.Brush imbalanceActiveTop;

	private System.Windows.Media.Brush imbalanceActiveBottom;

	private System.Windows.Media.Brush imbalanceInactiveTop;

	private System.Windows.Media.Brush imbalanceInactiveBottom;

	private System.Windows.Media.Brush orderBlockActiveTop;

	private System.Windows.Media.Brush orderBlockActiveBottom;

	private System.Windows.Media.Brush orderBlockInactiveTop;

	private System.Windows.Media.Brush orderBlockInactiveBottom;

	private System.Windows.Media.Brush chochTopTextColor;

	private System.Windows.Media.Brush chochBottomTextColor;

	private System.Windows.Media.Brush bosTopTextColor;

	private System.Windows.Media.Brush bosBottomTextColor;

	private SwingPoint lastSwingPoint;

	private SwingPoint lastSwingPointTop;

	private SwingPoint lastSwingPointBottom;

	private BosChochInfo lastBosChochInfo;

	private ImbalanceInfo lastImbalanceInfo;

	private Dictionary<int, OrderBlockInfo> dictOrderBlockInfosBroken;

	private Dictionary<int, ImbalanceInfo> dictImbalanceInfosBroken;

	private Dictionary<Type, PropertyInfo[]> cachePropertyInfo = new Dictionary<Type, PropertyInfo[]>();

	private Window alertWindow;
	private bool alertWindowClosed;

	private const string prefix = "gbKingOrderBlock";

	private const string indicatorName = "King Order Block";

	private bool isCharting;

	private bool isLastBarOnEachTick;

	private State prevState = State.Historical;

	private int signalState;

	private int signalZoneBullish;

	private int signalZoneBearish;

	private int prevReturnBullishBarIndex = -1;

	private int prevReturnBearishBarIndex = -1;

	private int returnBullishBarIndex = -1;

	private int returnBearishBarIndex = -1;

	private int fromIndex;

	private int toIndex;

	private DateTime nextAlert = DateTime.MinValue;

	private DateTime nextRearm = DateTime.MinValue;

	private string soundPath = string.Empty;

	private DispatcherTimer rearmTimer;

	[Display(Name = "Condition: Breakout", Order = 0, GroupName = "Alerts")]
	public bool ConditionBreakout { get; set; }

	[Display(Name = "Condition: Return", Order = 1, GroupName = "Alerts")]
	public bool ConditionReturn { get; set; }

	[Display(Name = "Popup: Enabled", Order = 2, GroupName = "Alerts")]
	public bool PopupEnabled { get; set; }

	[Display(Name = "Popup: Background Color", Order = 3, GroupName = "Alerts")]
	[XmlIgnore]
	public System.Windows.Media.Brush PopupBackgroundBrush { get; set; }

	[Browsable(false)]
	public string PopupBackgroundBrush_Serialize
	{
		get
		{
			return Serialize.BrushToString(PopupBackgroundBrush);
		}
		set
		{
			PopupBackgroundBrush = Serialize.StringToBrush(value);
		}
	}

	[Display(Name = "Popup: Background Opacity", Order = 4, GroupName = "Alerts")]
	[Range(0, 100)]
	public int PopupBackgroundOpacity { get; set; }

	[XmlIgnore]
	[Display(Name = "Popup: Text Color", Order = 5, GroupName = "Alerts")]
	public System.Windows.Media.Brush PopupTextBrush { get; set; }

	[Browsable(false)]
	public string PopupTextBrush_Serialize
	{
		get
		{
			return Serialize.BrushToString(PopupTextBrush);
		}
		set
		{
			PopupTextBrush = Serialize.StringToBrush(value);
		}
	}

	[Range(8, int.MaxValue)]
	[Display(Name = "Popup: Text Size", Order = 6, GroupName = "Alerts")]
	public int PopupTextSize { get; set; }

	[XmlIgnore]
	[Display(Name = "Popup: Button Color", Order = 7, GroupName = "Alerts")]
	public System.Windows.Media.Brush PopupButtonBrush { get; set; }

	[Browsable(false)]
	public string PopupButtonBrush_Serialize
	{
		get
		{
			return Serialize.BrushToString(PopupButtonBrush);
		}
		set
		{
			PopupButtonBrush = Serialize.StringToBrush(value);
		}
	}

	[Display(Name = "Sound: Enabled", Order = 10, GroupName = "Alerts")]
	public bool SoundEnabled { get; set; }

	[Display(Name = "Sound: Break Up", Order = 11, GroupName = "Alerts")]
	[TypeConverter(typeof(gbKingOrderBlock_SoundConverter))]
	public string SoundBreakUp { get; set; }

	[Display(Name = "Sound: Break Down", Order = 12, GroupName = "Alerts")]
	[TypeConverter(typeof(gbKingOrderBlock_SoundConverter))]
	public string SoundBreakDown { get; set; }

	[Display(Name = "Sound: Return Bullish", Order = 14, GroupName = "Alerts")]
	[TypeConverter(typeof(gbKingOrderBlock_SoundConverter))]
	public string SoundReturnBullish { get; set; }

	[TypeConverter(typeof(gbKingOrderBlock_SoundConverter))]
	[Display(Name = "Sound: Return Bearish", Order = 15, GroupName = "Alerts")]
	public string SoundReturnBearish { get; set; }

	[Display(Name = "Sound: Rearm Enabled", Order = 16, GroupName = "Alerts")]
	public bool SoundRearmEnabled { get; set; }

	[Display(Name = "Sound: Rearm Seconds ", Order = 17, GroupName = "Alerts")]
	[Range(0, int.MaxValue)]
	public int SoundRearmSeconds { get; set; }

	[Display(Name = "Email: Enabled", Order = 20, GroupName = "Alerts")]
	public bool EmailEnabled { get; set; }

	[Display(Name = "Email: Receiver", Order = 21, GroupName = "Alerts")]
	public string EmailReceiver { get; set; }

	[Display(Name = "Marker: Enabled", Order = 30, GroupName = "Alerts")]
	public bool MarkerEnabled { get; set; }

	[Display(Name = "Marker: Rendering Method", Order = 31, GroupName = "Alerts", Description = "\"Custom\" rendering provides a better appearance, and is therefore recommended in most cases.\nIf your computer is weak or slow, please switch to \"Builtin\" rendering.")]
	public gbKingOrderBlock_MarkerRenderingMethod MarkerRenderingMethod { get; set; }

	[XmlIgnore]
	[Display(Name = "Marker: Color Break Up", Order = 32, GroupName = "Alerts")]
	public System.Windows.Media.Brush MarkerBrushBreakUp { get; set; }

	[Browsable(false)]
	public string MarkerBrushBreakUp_Serialize
	{
		get
		{
			return Serialize.BrushToString(MarkerBrushBreakUp);
		}
		set
		{
			MarkerBrushBreakUp = Serialize.StringToBrush(value);
		}
	}

	[XmlIgnore]
	[Display(Name = "Marker: Color Break Down", Order = 33, GroupName = "Alerts")]
	public System.Windows.Media.Brush MarkerBrushBreakDown { get; set; }

	[Browsable(false)]
	public string MarkerBrushBreakDown_Serialize
	{
		get
		{
			return Serialize.BrushToString(MarkerBrushBreakDown);
		}
		set
		{
			MarkerBrushBreakDown = Serialize.StringToBrush(value);
		}
	}

	[Display(Name = "Marker: Color Return Bullish", Order = 34, GroupName = "Alerts")]
	[XmlIgnore]
	public System.Windows.Media.Brush MarkerBrushReturnBullish { get; set; }

	[Browsable(false)]
	public string MarkerBrushReturnBullish_Serialize
	{
		get
		{
			return Serialize.BrushToString(MarkerBrushReturnBullish);
		}
		set
		{
			MarkerBrushReturnBullish = Serialize.StringToBrush(value);
		}
	}

	[Display(Name = "Marker: Color Return Bearish", Order = 35, GroupName = "Alerts")]
	[XmlIgnore]
	public System.Windows.Media.Brush MarkerBrushReturnBearish { get; set; }

	[Browsable(false)]
	public string MarkerBrushReturnBearish_Serialize
	{
		get
		{
			return Serialize.BrushToString(MarkerBrushReturnBearish);
		}
		set
		{
			MarkerBrushReturnBearish = Serialize.StringToBrush(value);
		}
	}

	[Display(Name = "Marker: String Break Up", Order = 40, GroupName = "Alerts")]
	public string MarkerStringBreakUp { get; set; }

	[Display(Name = "Marker: String Break Down", Order = 41, GroupName = "Alerts")]
	public string MarkerStringBreakDown { get; set; }

	[Display(Name = "Marker: String Return Bullish", Order = 42, GroupName = "Alerts")]
	public string MarkerStringReturnBullish { get; set; }

	[Display(Name = "Marker: String Return Bearish", Order = 43, GroupName = "Alerts")]
	public string MarkerStringReturnBearish { get; set; }

	[Display(Name = "Marker: Font", Order = 45, GroupName = "Alerts")]
	public SimpleFont MarkerFont { get; set; }

	[Display(Name = "Marker: Offset", Order = 47, GroupName = "Alerts")]
	public int MarkerOffset { get; set; }

	[Range(0, int.MaxValue)]
	[Display(Name = "Alert Blocking (Seconds)", Order = 50, GroupName = "Alerts", Description = "The minimum interval between 2 consecutive alerts")]
	public int AlertBlockingSeconds { get; set; }

	[Display(Name = "Screen DPI", Order = 100, GroupName = "General")]
	[Range(99, 500)]
	public int ScreenDPI { get; set; }

	[Display(Name = "Gradient: Enabled", Order = 0, GroupName = "Graphics")]
	public bool GradientEnabled { get; set; }

	[Display(Name = "Imbalance Active: Top Start", Order = 2, GroupName = "Graphics")]
	[XmlIgnore]
	public System.Windows.Media.Brush ImbalanceActiveTopStart { get; set; }

	[Browsable(false)]
	public string ImbalanceActiveTopStart_Serialize
	{
		get
		{
			return Serialize.BrushToString(ImbalanceActiveTopStart);
		}
		set
		{
			ImbalanceActiveTopStart = Serialize.StringToBrush(value);
		}
	}

	[Display(Name = "Imbalance Active: Top End", Order = 4, GroupName = "Graphics")]
	[XmlIgnore]
	public System.Windows.Media.Brush ImbalanceActiveTopEnd { get; set; }

	[Browsable(false)]
	public string ImbalanceActiveTopEnd_Serialize
	{
		get
		{
			return Serialize.BrushToString(ImbalanceActiveTopEnd);
		}
		set
		{
			ImbalanceActiveTopEnd = Serialize.StringToBrush(value);
		}
	}

	[Display(Name = "Imbalance Active: Bottom Start", Order = 6, GroupName = "Graphics")]
	[XmlIgnore]
	public System.Windows.Media.Brush ImbalanceActiveBottomStart { get; set; }

	[Browsable(false)]
	public string ImbalanceActiveBottomStart_Serialize
	{
		get
		{
			return Serialize.BrushToString(ImbalanceActiveBottomStart);
		}
		set
		{
			ImbalanceActiveBottomStart = Serialize.StringToBrush(value);
		}
	}

	[Display(Name = "Imbalance Active: Bottom End", Order = 8, GroupName = "Graphics")]
	[XmlIgnore]
	public System.Windows.Media.Brush ImbalanceActiveBottomEnd { get; set; }

	[Browsable(false)]
	public string ImbalanceActiveBottomEnd_Serialize
	{
		get
		{
			return Serialize.BrushToString(ImbalanceActiveBottomEnd);
		}
		set
		{
			ImbalanceActiveBottomEnd = Serialize.StringToBrush(value);
		}
	}

	[Display(Name = "Imbalance Active: Opacity", Order = 10, GroupName = "Graphics")]
	[Range(0, 100)]
	public int ImbalanceActiveOpacity { get; set; }

	[Display(Name = "Imbalance Active: Border Top", Order = 12, GroupName = "Graphics")]
	public Stroke ImbalanceActiveBorderTop { get; set; }

	[Display(Name = "Imbalance Active: Border Bottom", Order = 14, GroupName = "Graphics")]
	public Stroke ImbalanceActiveBorderBottom { get; set; }

	[Display(Name = "Imbalance Inactive: Enabled", Order = 16, GroupName = "Graphics")]
	public bool ImbalanceInactiveEnabled { get; set; }

	[Display(Name = "Imbalance Inactive: Top Start", Order = 18, GroupName = "Graphics")]
	[XmlIgnore]
	public System.Windows.Media.Brush ImbalanceInactiveTopStart { get; set; }

	[Browsable(false)]
	public string ImbalanceInactiveTopStart_Serialize
	{
		get
		{
			return Serialize.BrushToString(ImbalanceInactiveTopStart);
		}
		set
		{
			ImbalanceInactiveTopStart = Serialize.StringToBrush(value);
		}
	}

	[Display(Name = "Imbalance Inactive: Top End", Order = 20, GroupName = "Graphics")]
	[XmlIgnore]
	public System.Windows.Media.Brush ImbalanceInactiveTopEnd { get; set; }

	[Browsable(false)]
	public string ImbalanceInactiveTopEnd_Serialize
	{
		get
		{
			return Serialize.BrushToString(ImbalanceInactiveTopEnd);
		}
		set
		{
			ImbalanceInactiveTopEnd = Serialize.StringToBrush(value);
		}
	}

	[XmlIgnore]
	[Display(Name = "Imbalance Inactive: Bottom Start", Order = 22, GroupName = "Graphics")]
	public System.Windows.Media.Brush ImbalanceInactiveBottomStart { get; set; }

	[Browsable(false)]
	public string ImbalanceInactiveBottomStart_Serialize
	{
		get
		{
			return Serialize.BrushToString(ImbalanceInactiveBottomStart);
		}
		set
		{
			ImbalanceInactiveBottomStart = Serialize.StringToBrush(value);
		}
	}

	[XmlIgnore]
	[Display(Name = "Imbalance Inactive: Bottom End", Order = 24, GroupName = "Graphics")]
	public System.Windows.Media.Brush ImbalanceInactiveBottomEnd { get; set; }

	[Browsable(false)]
	public string ImbalanceInactiveBottomEnd_Serialize
	{
		get
		{
			return Serialize.BrushToString(ImbalanceInactiveBottomEnd);
		}
		set
		{
			ImbalanceInactiveBottomEnd = Serialize.StringToBrush(value);
		}
	}

	[Range(0, 100)]
	[Display(Name = "Imbalance Inactive: Opacity", Order = 26, GroupName = "Graphics")]
	public int ImbalanceInactiveOpacity { get; set; }

	[Display(Name = "Imbalance Inactive: Border Top", Order = 28, GroupName = "Graphics")]
	public Stroke ImbalanceInactiveBorderTop { get; set; }

	[Display(Name = "Imbalance Inactive: Border Bottom", Order = 30, GroupName = "Graphics")]
	public Stroke ImbalanceInactiveBorderBottom { get; set; }

	[XmlIgnore]
	[Display(Name = "Order Block Active: Top Start", Order = 32, GroupName = "Graphics")]
	public System.Windows.Media.Brush OrderBlockActiveTopStart { get; set; }

	[Browsable(false)]
	public string OrderBlockActiveTopStart_Serialize
	{
		get
		{
			return Serialize.BrushToString(OrderBlockActiveTopStart);
		}
		set
		{
			OrderBlockActiveTopStart = Serialize.StringToBrush(value);
		}
	}

	[XmlIgnore]
	[Display(Name = "Order Block Active: Top End", Order = 34, GroupName = "Graphics")]
	public System.Windows.Media.Brush OrderBlockActiveTopEnd { get; set; }

	[Browsable(false)]
	public string OrderBlockActiveTopEnd_Serialize
	{
		get
		{
			return Serialize.BrushToString(OrderBlockActiveTopEnd);
		}
		set
		{
			OrderBlockActiveTopEnd = Serialize.StringToBrush(value);
		}
	}

	[Display(Name = "Order Block Active: Bottom Start", Order = 36, GroupName = "Graphics")]
	[XmlIgnore]
	public System.Windows.Media.Brush OrderBlockActiveBottomStart { get; set; }

	[Browsable(false)]
	public string OrderBlockActiveBottomStart_Serialize
	{
		get
		{
			return Serialize.BrushToString(OrderBlockActiveBottomStart);
		}
		set
		{
			OrderBlockActiveBottomStart = Serialize.StringToBrush(value);
		}
	}

	[XmlIgnore]
	[Display(Name = "Order Block Active: Bottom End", Order = 38, GroupName = "Graphics")]
	public System.Windows.Media.Brush OrderBlockActiveBottomEnd { get; set; }

	[Browsable(false)]
	public string OrderBlockActiveBottomEnd_Serialize
	{
		get
		{
			return Serialize.BrushToString(OrderBlockActiveBottomEnd);
		}
		set
		{
			OrderBlockActiveBottomEnd = Serialize.StringToBrush(value);
		}
	}

	[Display(Name = "Order Block Active: Opacity", Order = 40, GroupName = "Graphics")]
	[Range(0, 100)]
	public int OrderBlockActiveOpacity { get; set; }

	[Display(Name = "Order Block Active: Border Top", Order = 42, GroupName = "Graphics")]
	public Stroke OrderBlockActiveBorderTop { get; set; }

	[Display(Name = "Order Block Active: Border Bottom", Order = 44, GroupName = "Graphics")]
	public Stroke OrderBlockActiveBorderBottom { get; set; }

	[Display(Name = "Order Block Inactive: Enabled", Order = 46, GroupName = "Graphics")]
	public bool OrderBlockInactiveEnabled { get; set; }

	[Display(Name = "Order Block Inactive: Top Start", Order = 48, GroupName = "Graphics")]
	[XmlIgnore]
	public System.Windows.Media.Brush OrderBlockInactiveTopStart { get; set; }

	[Browsable(false)]
	public string OrderBlockInactiveTopStart_Serialize
	{
		get
		{
			return Serialize.BrushToString(OrderBlockInactiveTopStart);
		}
		set
		{
			OrderBlockInactiveTopStart = Serialize.StringToBrush(value);
		}
	}

	[XmlIgnore]
	[Display(Name = "Order Block Inactive: Top End", Order = 50, GroupName = "Graphics")]
	public System.Windows.Media.Brush OrderBlockInactiveTopEnd { get; set; }

	[Browsable(false)]
	public string OrderBlockInactiveTopEnd_Serialize
	{
		get
		{
			return Serialize.BrushToString(OrderBlockInactiveTopEnd);
		}
		set
		{
			OrderBlockInactiveTopEnd = Serialize.StringToBrush(value);
		}
	}

	[XmlIgnore]
	[Display(Name = "Order Block Inactive: Bottom Start", Order = 52, GroupName = "Graphics")]
	public System.Windows.Media.Brush OrderBlockInactiveBottomStart { get; set; }

	[Browsable(false)]
	public string OrderBlockInactiveBottomStart_Serialize
	{
		get
		{
			return Serialize.BrushToString(OrderBlockInactiveBottomStart);
		}
		set
		{
			OrderBlockInactiveBottomStart = Serialize.StringToBrush(value);
		}
	}

	[Display(Name = "Order Block Inactive: Bottom End", Order = 54, GroupName = "Graphics")]
	[XmlIgnore]
	public System.Windows.Media.Brush OrderBlockInactiveBottomEnd { get; set; }

	[Browsable(false)]
	public string OrderBlockInactiveBottomEnd_Serialize
	{
		get
		{
			return Serialize.BrushToString(OrderBlockInactiveBottomEnd);
		}
		set
		{
			OrderBlockInactiveBottomEnd = Serialize.StringToBrush(value);
		}
	}

	[Range(0, 100)]
	[Display(Name = "Order Block Inactive: Opacity", Order = 56, GroupName = "Graphics")]
	public int OrderBlockInactiveOpacity { get; set; }

	[Display(Name = "Order Block Inactive: Border Top", Order = 58, GroupName = "Graphics")]
	public Stroke OrderBlockInactiveBorderTop { get; set; }

	[Display(Name = "Order Block Inactive: Border Bottom", Order = 60, GroupName = "Graphics")]
	public Stroke OrderBlockInactiveBorderBottom { get; set; }

	[Display(Name = "Swing Point: Display Mode", Order = 62, GroupName = "Graphics")]
	public gbKingOrderBlock_SwingPointDisplayMode SwingPointDisplayMode { get; set; }

	[Display(Name = "Swing Point: Top", Order = 64, GroupName = "Graphics")]
	[XmlIgnore]
	public System.Windows.Media.Brush SwingPointTop { get; set; }

	[Browsable(false)]
	public string SwingPointTop_Serialize
	{
		get
		{
			return Serialize.BrushToString(SwingPointTop);
		}
		set
		{
			SwingPointTop = Serialize.StringToBrush(value);
		}
	}

	[XmlIgnore]
	[Display(Name = "Swing Point: Bottom", Order = 66, GroupName = "Graphics")]
	public System.Windows.Media.Brush SwingPointBottom { get; set; }

	[Browsable(false)]
	public string SwingPointBottom_Serialize
	{
		get
		{
			return Serialize.BrushToString(SwingPointBottom);
		}
		set
		{
			SwingPointBottom = Serialize.StringToBrush(value);
		}
	}

	[Display(Name = "Swing Point: Rendering Method", Order = 68, GroupName = "Graphics", Description = "\"Custom\" rendering provides a better appearance, and is therefore recommended in most cases.\nIf your computer is weak or slow, please switch to \"Builtin\" rendering.")]
	public gbKingOrderBlock_SwingPointRenderingMethod SwingPointRenderingMethod { get; set; }

	[Display(Name = "Bos/Choch: Display Mode", Order = 69, GroupName = "Graphics")]
	public gbKingOrderBlock_BosChochDisplayMode BosChochDisplayMode { get; set; }

	[Display(Name = "Bos Top: Line Enabled", Order = 70, GroupName = "Graphics")]
	public bool BosTopLineEnabled { get; set; }

	[Display(Name = "Bos Top: Line Stroke", Order = 72, GroupName = "Graphics")]
	public Stroke BosTopLineStroke { get; set; }

	[Display(Name = "Bos Top: Text Enabled", Order = 74, GroupName = "Graphics")]
	public bool BosTopTextEnabled { get; set; }

	[Display(Name = "Bos Bottom: Line Enabled", Order = 76, GroupName = "Graphics")]
	public bool BosBottomLineEnabled { get; set; }

	[Display(Name = "Bos Bottom: Line Stroke", Order = 78, GroupName = "Graphics")]
	public Stroke BosBottomLineStroke { get; set; }

	[Display(Name = "Bos Bottom: Text Enabled", Order = 80, GroupName = "Graphics")]
	public bool BosBottomTextEnabled { get; set; }

	[Display(Name = "Choch Top: Line Enabled", Order = 82, GroupName = "Graphics")]
	public bool ChochTopLineEnabled { get; set; }

	[Display(Name = "Choch Top: Line Stroke", Order = 84, GroupName = "Graphics")]
	public Stroke ChochTopLineStroke { get; set; }

	[Display(Name = "Choch Top: Text Enabled", Order = 86, GroupName = "Graphics")]
	public bool ChochTopTextEnabled { get; set; }

	[Display(Name = "Choch Bottom: Line Enabled", Order = 88, GroupName = "Graphics")]
	public bool ChochBottomLineEnabled { get; set; }

	[Display(Name = "Choch Bottom: Line Stroke", Order = 90, GroupName = "Graphics")]
	public Stroke ChochBottomLineStroke { get; set; }

	[Display(Name = "Choch Bottom: Text Enabled", Order = 92, GroupName = "Graphics")]
	public bool ChochBottomTextEnabled { get; set; }

	[Display(Name = "Bos & Choch: Text Opacity", Order = 94, GroupName = "Graphics")]
	[Range(0, 100)]
	public int BosChochTextOpacity { get; set; }

	[Display(Name = "Bos & Choch: Text Font", Order = 96, GroupName = "Graphics")]
	public SimpleFont BosChochTextFont { get; set; }

	[Display(Name = "Bos & Choch: Text Offset", Order = 98, GroupName = "Graphics")]
	public int BosChochTextOffset { get; set; }

	[Range(1, int.MaxValue)]
	[Display(Name = "Swing Point: Neighborhood", Order = 0, GroupName = "Parameters")]
	[NinjaScriptProperty]
	public int SwingPointNeighborhood { get; set; }

	[NinjaScriptProperty]
	[Range(1, int.MaxValue)]
	[Display(Name = "Imbalance: Qualifying (Bars)", Order = 10, GroupName = "Parameters")]
	public int ImbalanceQualifying { get; set; }

	[NinjaScriptProperty]
	[Range(1, int.MaxValue)]
	[Display(Name = "Order Block: Finding Bos/Choch Period", Order = 22, GroupName = "Parameters")]
	public int OrderBlockFindingBosChochPeriod { get; set; }

	[NinjaScriptProperty]
	[Range(0, int.MaxValue)]
	[Display(Name = "Order Block: Age (Bars)", Order = 24, GroupName = "Parameters")]
	public int OrderBlockAge { get; set; }

	[Range(0, int.MaxValue)]
	[NinjaScriptProperty]
	[Display(Name = "Order Blocks: Same Direction Offset (Ticks)", Order = 26, GroupName = "Parameters")]
	public int OrderBlocksSameDirectionOffset { get; set; }

	[Display(Name = "Order Blocks: Difference Direction Offset (Ticks)", Order = 28, GroupName = "Parameters")]
	[NinjaScriptProperty]
	[Range(0, int.MaxValue)]
	public int OrderBlocksDifferenceDirectionOffset { get; set; }

	[Display(Name = "Signal Trade: Quantity Per Order Block", Order = 30, GroupName = "Parameters")]
	[Range(1, int.MaxValue)]
	[NinjaScriptProperty]
	public int SignalTradeQuantityPerOrderBlock { get; set; }

	[Range(1, int.MaxValue)]
	[Display(Name = "Signal Trade: Split (Bars)", Order = 40, GroupName = "Parameters")]
	[NinjaScriptProperty]
	public int SignalTradeSplitBars { get; set; }

	[Display(Name = "Z Order", Order = 0, GroupName = "Special")]
	public int IndicatorZOrder { get; set; }

	[Display(Name = "User Note", Order = 10, GroupName = "Special")]
	public string UserNote { get; set; }

	[Browsable(false)]
	[XmlIgnore]
	public Series<double> Signal_Trade => base.Values[0];

	[Browsable(false)]
	[XmlIgnore]
	public Series<double> Signal_State => base.Values[1];

	[XmlIgnore]
	[Browsable(false)]
	public Series<double> Signal_Zone_Bullish => base.Values[2];

	[XmlIgnore]
	[Browsable(false)]
	public Series<double> Signal_Zone_Bearish => base.Values[3];

	public override string DisplayName
	{
		get
		{
			if (base.Parent is MarketAnalyzerColumnBase)
			{
				return base.DisplayName;
			}
			return "King Order Block by GreyBeard" + GetUserNote();
		}
	}

	private string GetUserNote()
	{
		string text = UserNote.Trim();
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
				base.Name = "gbKingOrderBlock";
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
				ShowTransparentPlotsInDataBox = true;
				ConditionReturn = true;
				ConditionBreakout = false;
				PopupEnabled = false;
				PopupBackgroundBrush = Brushes.Gold;
				PopupBackgroundOpacity = 60;
				PopupTextBrush = Brushes.DarkSlateGray;
				PopupTextSize = 16;
				PopupButtonBrush = Brushes.Transparent;
				SoundEnabled = false;
				SoundBreakUp = "Alert1.wav";
				SoundBreakDown = "Alert2.wav";
				SoundReturnBullish = "Alert4.wav";
				SoundReturnBearish = "Alert3.wav";
				SoundRearmEnabled = true;
				SoundRearmSeconds = 5;
				EmailEnabled = false;
				EmailReceiver = "receiver@example.com";
				MarkerEnabled = true;
				MarkerRenderingMethod = gbKingOrderBlock_MarkerRenderingMethod.Custom;
				MarkerBrushBreakUp = Brushes.SpringGreen;
				MarkerBrushBreakDown = Brushes.HotPink;
				MarkerBrushReturnBullish = Brushes.Chartreuse;
				MarkerBrushReturnBearish = Brushes.DeepPink;
				MarkerStringBreakUp = "⇑ + Break";
				MarkerStringBreakDown = "Break + ⇓";
				MarkerStringReturnBullish = "▲ + OB";
				MarkerStringReturnBearish = "OB + ▼";
				MarkerFont = new SimpleFont("Arial", 20);
				MarkerOffset = 10;
				AlertBlockingSeconds = 60;
				ScreenDPI = 99;
				GradientEnabled = true;
				ImbalanceActiveTopStart = Brushes.DeepPink;
				ImbalanceActiveTopEnd = Brushes.Black;
				ImbalanceActiveBottomStart = Brushes.LightSeaGreen;
				ImbalanceActiveBottomEnd = Brushes.Black;
				ImbalanceActiveOpacity = 90;
				ImbalanceActiveBorderTop = new Stroke(Brushes.Transparent, DashStyleHelper.Dash, 2f, 90);
				ImbalanceActiveBorderBottom = new Stroke(Brushes.Transparent, DashStyleHelper.Dash, 2f, 90);
				ImbalanceInactiveEnabled = true;
				ImbalanceInactiveTopStart = Brushes.DeepPink;
				ImbalanceInactiveTopEnd = Brushes.Black;
				ImbalanceInactiveBottomStart = Brushes.LightSeaGreen;
				ImbalanceInactiveBottomEnd = Brushes.Black;
				ImbalanceInactiveOpacity = 50;
				ImbalanceInactiveBorderTop = new Stroke(Brushes.Transparent, DashStyleHelper.Dash, 2f, 50);
				ImbalanceInactiveBorderBottom = new Stroke(Brushes.Transparent, DashStyleHelper.Dash, 2f, 50);
				OrderBlockActiveTopStart = Brushes.PaleVioletRed;
				OrderBlockActiveTopEnd = Brushes.Black;
				OrderBlockActiveBottomStart = Brushes.LimeGreen;
				OrderBlockActiveBottomEnd = Brushes.Black;
				OrderBlockActiveOpacity = 90;
				OrderBlockActiveBorderTop = new Stroke(Brushes.Yellow, DashStyleHelper.Dash, 1f, 90);
				OrderBlockActiveBorderBottom = new Stroke(Brushes.DeepSkyBlue, DashStyleHelper.Dash, 1f, 90);
				OrderBlockInactiveEnabled = true;
				OrderBlockInactiveTopStart = Brushes.HotPink;
				OrderBlockInactiveTopEnd = Brushes.Black;
				OrderBlockInactiveBottomStart = Brushes.MediumSeaGreen;
				OrderBlockInactiveBottomEnd = Brushes.Black;
				OrderBlockInactiveOpacity = 50;
				OrderBlockInactiveBorderTop = new Stroke(Brushes.Yellow, DashStyleHelper.Dash, 1f, 50);
				OrderBlockInactiveBorderBottom = new Stroke(Brushes.DeepSkyBlue, DashStyleHelper.Dash, 1f, 50);
				SwingPointRenderingMethod = gbKingOrderBlock_SwingPointRenderingMethod.Custom;
				SwingPointDisplayMode = gbKingOrderBlock_SwingPointDisplayMode.Smart;
				SwingPointTop = Brushes.DodgerBlue;
				SwingPointBottom = Brushes.HotPink;
				BosChochDisplayMode = gbKingOrderBlock_BosChochDisplayMode.Smart;
				BosTopLineEnabled = true;
				BosTopLineStroke = new Stroke(Brushes.Green, DashStyleHelper.Dot, 2f);
				BosTopTextEnabled = false;
				BosBottomLineEnabled = true;
				BosBottomLineStroke = new Stroke(Brushes.HotPink, DashStyleHelper.Dot, 2f);
				BosBottomTextEnabled = false;
				ChochTopLineEnabled = true;
				ChochTopLineStroke = new Stroke(Brushes.LimeGreen, DashStyleHelper.Dot, 2f);
				ChochTopTextEnabled = false;
				ChochBottomLineEnabled = true;
				ChochBottomLineStroke = new Stroke(Brushes.DeepPink, DashStyleHelper.Dot, 2f);
				ChochBottomTextEnabled = false;
				BosChochTextFont = new SimpleFont("Arial", 15);
				BosChochTextOffset = 0;
				BosChochTextOpacity = 30;
				SwingPointNeighborhood = 3;
				ImbalanceQualifying = 2;
				OrderBlockFindingBosChochPeriod = 20;
				OrderBlocksSameDirectionOffset = 10;
				OrderBlocksDifferenceDirectionOffset = 10;
				OrderBlockAge = 500;
				SignalTradeQuantityPerOrderBlock = 3;
				SignalTradeSplitBars = 6;
				IndicatorZOrder = -10;
				UserNote = "instrument (period)";
				AddPlot(Brushes.Transparent, "Signal Trade");
				AddPlot(Brushes.Transparent, "Signal State");
				AddPlot(Brushes.Transparent, "Signal Zone: Bullish");
				AddPlot(Brushes.Transparent, "Signal Zone: Bearish");
			}
			else if (base.State == State.Configure)
			{
				orderBlockOffsetSameDirection = (double)OrderBlocksSameDirectionOffset * base.TickSize;
				orderBlockOffsetDifferenceDirection = (double)OrderBlocksDifferenceDirectionOffset * base.TickSize;
				listSwingsTop = new SortedList<int, SwingPoint>();
				listSwingsBottom = new SortedList<int, SwingPoint>();
				listBosChochInfoTop = new SortedList<int, BosChochInfo>();
				listBosChochInfoBottom = new SortedList<int, BosChochInfo>();
				listOrderBlockActive = new SortedList<int, OrderBlockInfo>();
				listOrderBlockInactive = new SortedList<int, OrderBlockInfo>();
				listImbalanceActive = new SortedList<int, ImbalanceInfo>();
				listImbalanceInactive = new SortedList<int, ImbalanceInfo>();
				dictImbalanceInfosBroken = new Dictionary<int, ImbalanceInfo>();
				dictOrderBlockInfosBroken = new Dictionary<int, OrderBlockInfo>();
				isMarkerCustomRenderingMethod = MarkerRenderingMethod == gbKingOrderBlock_MarkerRenderingMethod.Custom;
				if (isMarkerCustomRenderingMethod)
				{
					listMarkers = new SortedList<int, MarkerInfo>();
				}
				isSwingPointCustomRenderingMethod = SwingPointRenderingMethod == gbKingOrderBlock_SwingPointRenderingMethod.Custom;
				isSwingPointSmartDisplayMode = SwingPointDisplayMode == gbKingOrderBlock_SwingPointDisplayMode.Smart;
				isBosChochSmartDisplayMode = BosChochDisplayMode == gbKingOrderBlock_BosChochDisplayMode.Smart;
				bosTopTextColor = BosTopLineStroke.Brush;
				bosBottomTextColor = BosBottomLineStroke.Brush;
				chochTopTextColor = ChochTopLineStroke.Brush;
				chochBottomTextColor = ChochBottomLineStroke.Brush;
				if (GradientEnabled)
				{
					imbalanceActiveBottomGradientStop = CreateGradientStopArr(ImbalanceActiveBottomStart, ImbalanceActiveBottomEnd, ImbalanceActiveOpacity);
					imbalanceActiveTopGradientStop = CreateGradientStopArr(ImbalanceActiveTopStart, ImbalanceActiveTopEnd, ImbalanceActiveOpacity);
					imbalanceInactiveBottomGradientStop = CreateGradientStopArr(ImbalanceInactiveBottomStart, ImbalanceInactiveBottomEnd, ImbalanceInactiveOpacity);
					imbalanceInactiveTopGradientStop = CreateGradientStopArr(ImbalanceInactiveTopStart, ImbalanceInactiveTopEnd, ImbalanceInactiveOpacity);
					orderBlockActiveBottomGradientStop = CreateGradientStopArr(OrderBlockActiveBottomStart, OrderBlockActiveBottomEnd, OrderBlockActiveOpacity);
					orderBlockActiveTopGradientStop = CreateGradientStopArr(OrderBlockActiveTopStart, OrderBlockActiveTopEnd, OrderBlockActiveOpacity);
					orderBlockInactiveBottomGradientStop = CreateGradientStopArr(OrderBlockInactiveBottomStart, OrderBlockInactiveBottomEnd, OrderBlockInactiveOpacity);
					orderBlockInactiveTopGradientStop = CreateGradientStopArr(OrderBlockInactiveTopStart, OrderBlockInactiveTopEnd, OrderBlockInactiveOpacity);
				}
				else
				{
					imbalanceActiveTop = CreateOpacityBrush(ImbalanceActiveTopStart, ImbalanceActiveOpacity);
					imbalanceActiveBottom = CreateOpacityBrush(ImbalanceActiveBottomStart, ImbalanceActiveOpacity);
					imbalanceInactiveTop = CreateOpacityBrush(ImbalanceInactiveTopStart, ImbalanceInactiveOpacity);
					imbalanceInactiveBottom = CreateOpacityBrush(ImbalanceInactiveBottomStart, ImbalanceInactiveOpacity);
					orderBlockActiveTop = CreateOpacityBrush(OrderBlockActiveTopStart, OrderBlockActiveOpacity);
					orderBlockActiveBottom = CreateOpacityBrush(OrderBlockActiveBottomStart, OrderBlockActiveOpacity);
					orderBlockInactiveTop = CreateOpacityBrush(OrderBlockInactiveTopStart, OrderBlockInactiveOpacity);
					orderBlockInactiveBottom = CreateOpacityBrush(OrderBlockInactiveBottomStart, OrderBlockInactiveOpacity);
				}
				isOnBarCloseMode = base.Calculate == Calculate.OnBarClose;
			}
			else if (base.State == State.Historical)
			{
				if (ScreenDPI < 100 && isCharting)
				{
					base.ChartControl.Dispatcher.Invoke(() =>
					{
						try
						{
							ScreenDPI = (int)System.Windows.Media.VisualTreeHelper.GetDpi(base.ChartControl).PixelsPerInchX;
						}
						catch { ScreenDPI = 99; }
					});
				}
				if (IndicatorZOrder != 0)
				{
					SetZOrder(IndicatorZOrder);
				}
				isCharting = base.ChartControl != null;
				if (isCharting)
				{
					barOutlineWidth = base.ChartBars.Properties.ChartStyle.Stroke2.Width;
				}
				if (!isCharting)
				{
					return;
				}
				base.ChartControl.Dispatcher.InvokeAsync(delegate
				{
					if (rearmTimer == null)
					{
						rearmTimer = new DispatcherTimer();
						rearmTimer.Interval = TimeSpan.FromSeconds(1);
						rearmTimer.Tick += OnRearmTimerTick;
					}
				});
			}
			else
			{
				if (base.State != State.Terminated)
				{
					return;
				}
				if (isCharting)
				{
					base.ChartControl.Dispatcher.InvokeAsync(delegate
					{
						if (alertWindow != null)
						{
							alertWindow.Close();
							alertWindow = null;
						}
					});
				}
				if (rearmTimer != null)
				{
					rearmTimer.Stop();
					rearmTimer.Tick -= OnRearmTimerTick;
					rearmTimer = null;
				}
			}
		}
		catch (Exception exception)
		{
			Print(exception.ToString());
		}
	}

	private SharpDX.Direct2D1.GradientStop[] CreateGradientStopArr(System.Windows.Media.Brush brushStart, System.Windows.Media.Brush brushEnd, int opacity)
	{
		if (brushStart.IsTransparent() && brushEnd.IsTransparent())
		{
			return null;
		}
		System.Windows.Media.SolidColorBrush solidColorBrush = (System.Windows.Media.SolidColorBrush)CreateOpacityBrush(brushStart, opacity);
		Color4 color = new Color4((float)(int)solidColorBrush.Color.R / 255f, (float)(int)solidColorBrush.Color.G / 255f, (float)(int)solidColorBrush.Color.B / 255f, (float)(int)solidColorBrush.Color.A / 255f);
		System.Windows.Media.SolidColorBrush solidColorBrush2 = (System.Windows.Media.SolidColorBrush)CreateOpacityBrush(brushEnd, opacity);
		Color4 color2 = new Color4((float)(int)solidColorBrush2.Color.R / 255f, (float)(int)solidColorBrush2.Color.G / 255f, (float)(int)solidColorBrush2.Color.B / 255f, (float)(int)solidColorBrush2.Color.A / 255f);
		return new SharpDX.Direct2D1.GradientStop[3]
		{
			new SharpDX.Direct2D1.GradientStop
			{
				Position = -0.2f,
				Color = color2
			},
			new SharpDX.Direct2D1.GradientStop
			{
				Position = 0.5f,
				Color = color
			},
			new SharpDX.Direct2D1.GradientStop
			{
				Position = 1.2f,
				Color = color2
			}
		};
	}

	protected override void OnBarUpdate()
	{
		try
		{
			int num = (signalZoneBearish = 0);
			signalZoneBullish = 0;
			signalState = 0;
			if (!isOnBarCloseMode)
			{
				isLastBarOnEachTick = IsLastBarOnEachTick();
				CalculateOnEachTickMode();
			}
			FindSwingsByNeighborhood();
			FindBrokenSwingPointAndAddBosChochInfoToList(listSwingsTop, isTop: true);
			FindBrokenSwingPointAndAddBosChochInfoToList(listSwingsBottom, isTop: false);
			if (base.CurrentBar >= 3)
			{
				FindImbalanceAndAddToList();
				FindBrokenImbalance();
				num = FindBrokenOrderBlockAndSignal();
				FindOrderBlockAndAddToList();
				ComputeSignalState();
				Signal_Trade[0] = num;
				Signal_State[0] = signalState;
				Signal_Zone_Bullish[0] = signalZoneBullish;
				Signal_Zone_Bearish[0] = signalZoneBearish;
				if (base.IsFirstTickOfBar)
				{
					prevState = base.State;
				}
			}
		}
		catch (Exception exception)
		{
			Print(exception.ToString());
		}
	}

	private void ComputeSignalState()
	{
		if (listOrderBlockActive == null || listOrderBlockActive.Count == 0)
		{
			return;
		}
		bool flag = true;
		bool flag2 = true;
		bool flag3 = true;
		int num = listOrderBlockActive.Count - 1;
		while (num >= 0 && (flag || flag3 || flag2))
		{
			ZoneInfo zoneInfo = listOrderBlockActive.Values[num];
			if (zoneInfo != null)
			{
				if (flag)
				{
					if (zoneInfo.IsTop)
					{
						if (base.Close[0].ApproxCompare(zoneInfo.PriceBottom) >= 0)
						{
							signalState = -2;
							flag = false;
						}
						else if (base.High[0].ApproxCompare(zoneInfo.PriceBottom) >= 0)
						{
							signalState = -1;
							flag = false;
						}
					}
					else if (base.Close[0].ApproxCompare(zoneInfo.PriceTop) <= 0)
					{
						signalState = 2;
						flag = false;
					}
					else if (base.Low[0].ApproxCompare(zoneInfo.PriceTop) <= 0)
					{
						signalState = 1;
						flag = false;
					}
				}
				if (flag2 && !zoneInfo.IsTop)
				{
					signalZoneBullish = 1;
					flag2 = false;
				}
				if (flag3 && zoneInfo.IsTop)
				{
					signalZoneBearish = 1;
					flag3 = false;
				}
			}
			num--;
		}
	}

	private void CalculateOnEachTickMode()
	{
		if (base.State != State.Historical)
		{
			if (base.IsFirstTickOfBar)
			{
				FindLastValues();
				BackupPropertiesAndLastValues();
				ChangePropertiesListActive(listOrderBlockActive, isBackup: true);
				ResetFlagAndDictBroken();
			}
			RevertAllChanged();
		}
	}

	public void FindLastValues()
	{
		lastSwingPointTop = ((listSwingsTop.Count > 0) ? listSwingsTop.Last().Value : null);
		lastSwingPointBottom = ((listSwingsBottom.Count > 0) ? listSwingsBottom.Last().Value : null);
		if (flagNewSwingPoint)
		{
			KeyValuePair<int, SwingPoint>? lastElement = GetLastElement(listSwingsTop, listSwingsBottom);
			lastSwingPoint = ((!lastElement.HasValue) ? null : lastElement.Value.Value);
		}
		if (flagNewBosChochTop || flagNewBosChochBottom)
		{
			KeyValuePair<int, BosChochInfo>? lastElement2 = GetLastElement(listBosChochInfoTop, listBosChochInfoBottom);
			lastBosChochInfo = ((!lastElement2.HasValue) ? null : lastElement2.Value.Value);
		}
		if (flagNewImbalance)
		{
			KeyValuePair<int, ImbalanceInfo>? lastElement3 = GetLastElement(listImbalanceActive, listImbalanceInactive);
			lastImbalanceInfo = ((!lastElement3.HasValue) ? null : lastElement3.Value.Value);
		}
	}

	private bool IsLastBarOnEachTick()
	{
		if (!isOnBarCloseMode)
		{
			return base.CurrentBar == base.Bars.Count - 1;
		}
		return false;
	}

	public void ChangePropertiesListActive<TValue>(SortedList<int, TValue> listActive, bool isBackup)
	{
		if (listActive.Count == 0)
		{
			return;
		}
		string name = (isBackup ? "BackupProperties" : "RevertProperties");
		MethodInfo method = typeof(TValue).GetMethod(name);
		if (method == null)
		{
			return;
		}
		foreach (KeyValuePair<int, TValue> item in listActive)
		{
			method.Invoke(item.Value, new object[1] { cachePropertyInfo });
		}
	}

	public void BackupPropertiesAndLastValues()
	{
		if (lastSwingPointTop != null)
		{
			lastSwingPointTop.BackupProperties(cachePropertyInfo);
		}
		if (lastSwingPointBottom != null)
		{
			lastSwingPointBottom.BackupProperties(cachePropertyInfo);
		}
		if (lastSwingPoint != null)
		{
			lastSwingPoint.BackupProperties(cachePropertyInfo);
		}
		if (lastBosChochInfo != null)
		{
			lastBosChochInfo.BackupProperties(cachePropertyInfo);
		}
		if (lastImbalanceInfo != null)
		{
			lastImbalanceInfo.BackupProperties(cachePropertyInfo);
		}
		prevReturnBullishBarIndex = returnBullishBarIndex;
		prevReturnBearishBarIndex = returnBearishBarIndex;
	}

	public void RevertPropertiesAndLastValues()
	{
		if (lastSwingPointTop != null)
		{
			lastSwingPointTop.RevertProperties(cachePropertyInfo);
		}
		if (lastSwingPointBottom != null)
		{
			lastSwingPointBottom.RevertProperties(cachePropertyInfo);
		}
		if (lastSwingPoint != null)
		{
			lastSwingPoint.RevertProperties(cachePropertyInfo);
		}
		if (lastBosChochInfo != null)
		{
			lastBosChochInfo.RevertProperties(cachePropertyInfo);
		}
		if (lastImbalanceInfo != null)
		{
			lastImbalanceInfo.RevertProperties(cachePropertyInfo);
		}
		returnBullishBarIndex = prevReturnBullishBarIndex;
		returnBearishBarIndex = prevReturnBearishBarIndex;
	}

	public void ResetFlagAndDictBroken()
	{
		dictImbalanceInfosBroken.Clear();
		dictOrderBlockInfosBroken.Clear();
		flagNewBosChochBottom = false;
		flagNewBosChochTop = false;
		flagNewSwingPoint = false;
		flagNewMaker = false;
		flagNewOrderBlock = false;
		flagNewImbalance = false;
	}

	public void RevertAllChanged()
	{
		if (flagNewOrderBlock)
		{
			RemoveLastElementInList(listOrderBlockActive);
		}
		if (dictOrderBlockInfosBroken.Count > 0)
		{
			RevertListBroken(dictOrderBlockInfosBroken, listOrderBlockInactive, listOrderBlockActive);
		}
		if (flagNewImbalance)
		{
			RemoveLastElementInList(listImbalanceActive);
		}
		if (dictImbalanceInfosBroken.Count > 0)
		{
			RevertListBroken(dictImbalanceInfosBroken, listImbalanceInactive, listImbalanceActive);
		}
		if (flagNewBosChochTop)
		{
			RemoveLastElementInList(listBosChochInfoTop);
		}
		if (flagNewBosChochBottom)
		{
			RemoveLastElementInList(listBosChochInfoBottom);
		}
		if (flagNewSwingPoint)
		{
			SortedList<int, SwingPoint> listData = (lastSwingPoint.IsTop ? listSwingsTop : listSwingsBottom);
			RemoveLastElementInList(listData);
			RemoveSwingPointBuiltinMode(lastSwingPoint.IsTop, base.CurrentBar - SwingPointNeighborhood);
		}
		if (flagNewMaker)
		{
			RemoveMarker();
		}
		if (!base.IsFirstTickOfBar)
		{
			FindLastValues();
		}
		if (prevState == State.Realtime)
		{
			RevertPropertiesAndLastValues();
		}
		ResetFlagAndDictBroken();
		ChangePropertiesListActive(listOrderBlockActive, isBackup: false);
	}

	private void RemoveMarker()
	{
		if (isMarkerCustomRenderingMethod)
		{
			RemoveLastElementInList(listMarkers);
			return;
		}
		string tag = "gbKingOrderBlock.marker." + base.CurrentBar;
		if (base.DrawObjects[tag] != null)
		{
			RemoveDrawObject(tag);
		}
	}

	private KeyValuePair<int, TValue>? GetLastElement<TValue>(SortedList<int, TValue> list1, SortedList<int, TValue> list2)
	{
		KeyValuePair<int, TValue>? result = ((list1.Count > 0) ? new KeyValuePair<int, TValue>?(list1.Last()) : ((KeyValuePair<int, TValue>?)null));
		KeyValuePair<int, TValue>? result2 = ((list2.Count > 0) ? new KeyValuePair<int, TValue>?(list2.Last()) : ((KeyValuePair<int, TValue>?)null));
		if (!result.HasValue && !result2.HasValue)
		{
			return null;
		}
		if (result2.HasValue)
		{
			if (result.HasValue)
			{
				if (result.Value.Key <= result2.Value.Key)
				{
					return result2;
				}
				return result;
			}
			return result2;
		}
		return result;
	}

	private void RevertListBroken<TValue>(Dictionary<int, TValue> dictBroken, SortedList<int, TValue> sortedListInactive, SortedList<int, TValue> sortedListActive)
	{
		foreach (KeyValuePair<int, TValue> item in dictBroken)
		{
			int key = item.Key;
			TValue value = item.Value;
			MoveElementFromListToList(key, value, sortedListInactive, sortedListActive);
		}
	}

	private void FindBrokenImbalance()
	{
		if (listImbalanceActive.Count == 0)
		{
			return;
		}
		for (int i = 0; i < listImbalanceActive.Count; i++)
		{
			int key = listImbalanceActive.Keys[i];
			ImbalanceInfo imbalanceInfo = listImbalanceActive.Values[i];
			if (IsImbalanceBroken(imbalanceInfo))
			{
				imbalanceInfo.IsBroken = true;
				MoveElementFromListToList(key, imbalanceInfo, listImbalanceActive, listImbalanceInactive);
				if (isLastBarOnEachTick)
				{
					dictImbalanceInfosBroken.Add(key, imbalanceInfo);
				}
			}
			else
			{
				imbalanceInfo.BarEnd = base.CurrentBar;
			}
		}
	}

	private bool IsImbalanceBroken(ImbalanceInfo imbalanceInfo)
	{
		return ((((imbalanceInfo.IsTop ? base.High[0] : base.Low[0]) - imbalanceInfo.PriceBroken) * (double)imbalanceInfo.Sign).ApproxCompare(0.0) < 0 && imbalanceInfo.BarEnd != base.CurrentBar) | (base.CurrentBar - imbalanceInfo.BarStart > OrderBlockAge);
	}

	private void FindImbalanceAndAddToList()
	{
		ImbalanceInfo imbalanceInfo = GetImbalanceInfo();
		if (imbalanceInfo != null)
		{
			AddElementToList(imbalanceInfo.BarStart, imbalanceInfo, listImbalanceActive);
			lastImbalanceInfo = imbalanceInfo;
			if (isLastBarOnEachTick)
			{
				flagNewImbalance = true;
			}
		}
	}

	private ImbalanceInfo GetImbalanceInfo()
	{
		if (base.Close[0].ApproxCompare(base.Open[0]) == 0)
		{
			return null;
		}
		bool flag = base.Close[0].ApproxCompare(base.Open[0]) < 0;
		MarubozuInfo marubozuInfo = CheckMarubozuBar(0, flag);
		if (lastImbalanceInfo != null)
		{
			if (marubozuInfo == null)
			{
				lastImbalanceInfo.IsFixed = true;
				return null;
			}
			if (!lastImbalanceInfo.IsFixed && !lastImbalanceInfo.IsBroken && lastImbalanceInfo.IsTop == flag && lastImbalanceInfo.BarEnd == base.CurrentBar - 1)
			{
				if (flag)
				{
					lastImbalanceInfo.PriceBottom = base.Close[0];
				}
				else
				{
					lastImbalanceInfo.PriceTop = base.Close[0];
				}
				lastImbalanceInfo.BarEnd = base.CurrentBar;
				return null;
			}
		}
		double num = -2147483648.0;
		double num2 = 2147483647.0;
		int num3 = ImbalanceQualifying - 1;
		while (true)
		{
			if (num3 >= 0)
			{
				MarubozuInfo marubozuInfo2 = CheckMarubozuBar(num3, flag);
				if (marubozuInfo2 == null)
				{
					break;
				}
				num = Math.Max(num, marubozuInfo2.PriceTop);
				num2 = Math.Min(num2, marubozuInfo2.PriceBottom);
				num3--;
				continue;
			}
			int barStart = base.CurrentBar - ImbalanceQualifying + 1;
			return new ImbalanceInfo(flag, isBroken: false, barStart, base.CurrentBar, num, num2);
		}
		return null;
	}

	private MarubozuInfo CheckMarubozuBar(int barAgo, bool isTop)
	{
		double num = (isTop ? base.Open[barAgo] : base.Close[barAgo]);
		double num2 = (isTop ? base.Close[barAgo] : base.Open[barAgo]);
		if (num.ApproxCompare(num2) == 0)
		{
			return null;
		}
		if (num.ApproxCompare(base.High[barAgo]) == 0 && num2.ApproxCompare(base.Low[barAgo]) == 0)
		{
			return new MarubozuInfo(num, num2);
		}
		return null;
	}

	private int FindBrokenOrderBlockAndSignal()
	{
		if (listOrderBlockActive.Count == 0)
		{
			return 0;
		}
		int num = 0;
		for (int i = 0; i < listOrderBlockActive.Count; i++)
		{
			int key = listOrderBlockActive.Keys[i];
			OrderBlockInfo orderBlockInfo = listOrderBlockActive.Values[i];
			orderBlockInfo.BarEnd = base.CurrentBar - 1;
			bool flag = base.CurrentBar - orderBlockInfo.BarStart > OrderBlockAge;
			bool flag2 = !orderBlockInfo.IsTop;
			int num2 = 1;
			int num3 = 2;
			if (base.Closes[0][0].ApproxCompare(orderBlockInfo.PriceBroken) * orderBlockInfo.Sign < 0 || flag)
			{
				if (orderBlockInfo.CountReturnSignal == 0 && Math.Abs(num) != num2 && !flag)
				{
					num = -num3 * orderBlockInfo.Sign;
					if (ConditionBreakout)
					{
						if (isLastBarOnEachTick)
						{
							flagNewMaker = true;
						}
						if (isMarkerCustomRenderingMethod)
						{
							AddMarker(base.CurrentBar, !flag2, SignalType.Breakout);
						}
						else
						{
							PrintMarker(!flag2, SignalType.Breakout);
						}
						TriggerAlerts(!flag2, SignalType.Breakout);
					}
				}
				MoveElementFromListToList(key, orderBlockInfo, listOrderBlockActive, listOrderBlockInactive);
				if (isLastBarOnEachTick)
				{
					dictOrderBlockInfosBroken.Add(key, orderBlockInfo);
				}
				continue;
			}
			bool flag3 = ((!orderBlockInfo.IsTop) ? (returnBullishBarIndex == -1 || base.CurrentBar - returnBullishBarIndex > SignalTradeSplitBars) : (returnBearishBarIndex == -1 || base.CurrentBar - returnBearishBarIndex > SignalTradeSplitBars));
			bool flag4 = orderBlockInfo.CountReturnSignal >= SignalTradeQuantityPerOrderBlock;
			if (!((((flag2 ? base.Low[0] : base.High[0]) - orderBlockInfo.PriceSignal) * (double)orderBlockInfo.Sign).ApproxCompare(0.0) < 0 && IsReversalBar(-orderBlockInfo.Sign) && flag3) || flag4 || Math.Abs(num) == num2)
			{
				continue;
			}
			num = orderBlockInfo.Sign;
			if (num == num2)
			{
				returnBullishBarIndex = base.CurrentBar;
			}
			else
			{
				returnBearishBarIndex = base.CurrentBar;
			}
			orderBlockInfo.CountReturnSignal++;
			if (ConditionReturn)
			{
				if (isLastBarOnEachTick)
				{
					flagNewMaker = true;
				}
				if (isMarkerCustomRenderingMethod)
				{
					AddMarker(base.CurrentBar, flag2, SignalType.Return);
				}
				else
				{
					PrintMarker(flag2, SignalType.Return);
				}
				TriggerAlerts(flag2, SignalType.Return);
			}
		}
		return num;
	}

	private void FindOrderBlockAndAddToList()
	{
		if (lastImbalanceInfo == null || lastSwingPoint == null || lastBosChochInfo == null || lastSwingPoint.IsHasOrderBlock || base.CurrentBar - lastSwingPoint.BarEnd > OrderBlockFindingBosChochPeriod || lastSwingPoint.IsTop != lastImbalanceInfo.IsTop || lastSwingPoint.IsTop == lastBosChochInfo.IsTop || lastBosChochInfo.BarEnd < lastSwingPoint.BarEnd || lastImbalanceInfo.BarStart < lastSwingPoint.BarStart)
		{
			return;
		}
		int num = base.CurrentBar - lastSwingPoint.BarStart;
		double num2 = base.High[num];
		double num3 = base.Low[num];
		if (!IsOffsetOf2OrderBlockOk(num))
		{
			return;
		}
		double num4 = (lastSwingPoint.IsTop ? lastImbalanceInfo.PriceTop : lastImbalanceInfo.PriceBottom);
		double num5 = (lastSwingPoint.IsTop ? num2 : num3);
		if ((num4 - num5) * (double)lastImbalanceInfo.Sign < 0.0)
		{
			return;
		}
		OrderBlockInfo element = new OrderBlockInfo(lastSwingPoint.IsTop, isBroken: false, lastSwingPoint.BarStart, base.CurrentBar, num2, num3);
		AddElementToList(lastSwingPoint.BarEnd, element, listOrderBlockActive);
		lastSwingPoint.IsHasOrderBlock = true;
		lastBosChochInfo.IsHasOrderBlock = true;
		if (isLastBarOnEachTick)
		{
			flagNewOrderBlock = true;
		}
		SwingPoint swingPoint = (lastBosChochInfo.IsTop ? listSwingsTop : listSwingsBottom)[lastBosChochInfo.BarStart];
		if (isBosChochSmartDisplayMode)
		{
			swingPoint.AllowDraw = true;
			if (isSwingPointSmartDisplayMode)
			{
				DrawSwingPointBuiltinMode(swingPoint.IsTop, swingPoint.BarEnd, base.CurrentBar - swingPoint.BarEnd);
			}
		}
	}

	private bool IsOffsetOf2OrderBlockOk(int startBarAgo)
	{
		if (listOrderBlockActive.Count == 0)
		{
			return true;
		}
		OrderBlockInfo orderBlockInfo = null;
		OrderBlockInfo orderBlockInfo2 = null;
		for (int num = listOrderBlockActive.Count - 1; num >= 0; num--)
		{
			OrderBlockInfo orderBlockInfo3 = listOrderBlockActive.Values[num];
			if (orderBlockInfo != null && orderBlockInfo2 != null)
			{
				break;
			}
			if (orderBlockInfo3.IsTop && orderBlockInfo == null)
			{
				orderBlockInfo = orderBlockInfo3;
			}
			else if (!orderBlockInfo3.IsTop && orderBlockInfo2 == null)
			{
				orderBlockInfo2 = orderBlockInfo3;
			}
		}
		if (orderBlockInfo != null && (orderBlockInfo.PriceBottom - base.High[startBarAgo]).ApproxCompare(lastSwingPoint.IsTop ? orderBlockOffsetSameDirection : orderBlockOffsetDifferenceDirection) <= 0)
		{
			return false;
		}
		if (orderBlockInfo2 != null && (base.Low[startBarAgo] - orderBlockInfo2.PriceTop).ApproxCompare(lastSwingPoint.IsTop ? orderBlockOffsetDifferenceDirection : orderBlockOffsetSameDirection) <= 0)
		{
			return false;
		}
		return true;
	}

	private double GetPrice(bool isCheckingTop, int barIndex)
	{
		if (!isCheckingTop)
		{
			return base.Low.GetValueAt(barIndex);
		}
		return base.High.GetValueAt(barIndex);
	}

	private void FindSwingsByNeighborhood()
	{
		CheckSwingPoint(isCheckingTop: true, listSwingsTop);
		CheckSwingPoint(isCheckingTop: false, listSwingsBottom);
	}

	private void CheckSwingPoint(bool isCheckingTop, SortedList<int, SwingPoint> listSwings)
	{
		try
		{
			int key = base.CurrentBar - SwingPointNeighborhood;
			SwingPoint swingPoint = CheckSwingPoint(isCheckingTop);
			if (swingPoint != null)
			{
				if (!listSwings.ContainsKey(key))
				{
					listSwings.Add(key, swingPoint);
				}
				if (SwingPointDisplayMode == gbKingOrderBlock_SwingPointDisplayMode.All)
				{
					DrawSwingPointBuiltinMode(isCheckingTop, key, SwingPointNeighborhood);
				}
				lastSwingPoint = swingPoint;
				if (isLastBarOnEachTick)
				{
					flagNewSwingPoint = true;
				}
			}
		}
		catch (Exception exception)
		{
			Print(exception.ToString());
		}
	}

	private void DrawSwingPointBuiltinMode(bool isTop, int key, int swingPointIndex)
	{
		try
		{
			if (SwingPointDisplayMode == gbKingOrderBlock_SwingPointDisplayMode.Disabled || isSwingPointCustomRenderingMethod)
			{
				return;
			}
			System.Windows.Media.Brush brush = (isTop ? SwingPointTop : SwingPointBottom);
			if (!brush.IsTransparent())
			{
				string tag = "gbKingOrderBlock.dot.price." + (isTop ? "top." : "bottom.") + key;
				Dot dot = Draw.Dot(this, tag, isAutoScale: true, swingPointIndex, (isTop ? base.High : base.Low)[swingPointIndex], brush);
				if (dot != null)
				{
					dot.ZOrder = -100;
				}
			}
		}
		catch (Exception exception)
		{
			Print(exception.ToString());
		}
	}

	private void RemoveSwingPointBuiltinMode(bool isTop, int key)
	{
		try
		{
			if (SwingPointDisplayMode != gbKingOrderBlock_SwingPointDisplayMode.Disabled && !isSwingPointCustomRenderingMethod)
			{
				string tag = "gbKingOrderBlock.dot.price." + (isTop ? "top." : "bottom.") + key;
				if (base.DrawObjects[tag] != null)
				{
					RemoveDrawObject(tag);
				}
			}
		}
		catch (Exception exception)
		{
			Print(exception.ToString());
		}
	}

	private SwingPoint CheckSwingPoint(bool isCheckingTop)
	{
		SwingPoint swingPoint = CheckSwingPointType1(isCheckingTop);
		if (swingPoint != null)
		{
			return swingPoint;
		}
		swingPoint = CheckSwingPointType2(isCheckingTop);
		if (swingPoint != null)
		{
			return swingPoint;
		}
		swingPoint = CheckSwingPointType3(isCheckingTop);
		if (swingPoint != null)
		{
			return swingPoint;
		}
		return null;
	}

	private SwingPoint CheckSwingPointType1(bool isCheckingTop)
	{
		if (base.CurrentBar < 2 * SwingPointNeighborhood)
		{
			return null;
		}
		int barStart = base.CurrentBar - SwingPointNeighborhood;
		int num = base.CurrentBar - SwingPointNeighborhood;
		int num2 = (isCheckingTop ? 1 : (-1));
		double price = GetPrice(isCheckingTop, num);
		int num3 = base.CurrentBar - 2 * SwingPointNeighborhood;
		while (true)
		{
			if (num3 <= base.CurrentBar)
			{
				if (num3 != num)
				{
					int num4 = GetPrice(isCheckingTop, num3).ApproxCompare(price);
					if (num2 * num4 >= 0)
					{
						break;
					}
				}
				num3++;
				continue;
			}
			return new SwingPoint(isCheckingTop, price, barStart, num);
		}
		return null;
	}

	private SwingPoint CheckSwingPointType2(bool isCheckingTop)
	{
		if (base.CurrentBar < 3 * SwingPointNeighborhood)
		{
			return null;
		}
		int num = base.CurrentBar - SwingPointNeighborhood;
		int num2 = num - SwingPointNeighborhood;
		int num3 = (isCheckingTop ? 1 : (-1));
		int num4 = 1;
		double price = GetPrice(isCheckingTop, num);
		int num5 = num - 1;
		while (num5 >= num2 && GetPrice(isCheckingTop, num5).ApproxCompare(price) == 0)
		{
			num4++;
			num5--;
		}
		if (num4 < 2)
		{
			return null;
		}
		num2 = num - num4 + 1;
		int num6 = num2 - SwingPointNeighborhood;
		while (true)
		{
			if (num6 <= base.CurrentBar)
			{
				if (num6 < num2 || num6 > num)
				{
					int num7 = GetPrice(isCheckingTop, num6).ApproxCompare(price);
					if (num3 * num7 >= 0)
					{
						break;
					}
				}
				num6++;
				continue;
			}
			return new SwingPoint(isCheckingTop, price, num2, num);
		}
		return null;
	}

	private SwingPoint CheckSwingPointType3(bool isCheckingTop)
	{
		if (base.CurrentBar < 3 * SwingPointNeighborhood)
		{
			return null;
		}
		ValueData valueData = new ValueData();
		ValueData valueData2 = new ValueData();
		ValueData valueData3 = new ValueData();
		int num = base.CurrentBar - SwingPointNeighborhood;
		int num2 = num - SwingPointNeighborhood;
		int num3 = (isCheckingTop ? 1 : (-1));
		int num4 = 1;
		double price = GetPrice(isCheckingTop, num);
		int num5 = 0;
		int num6 = num - 1;
		while (true)
		{
			if (num6 >= num2)
			{
				int num7 = GetPrice(isCheckingTop, num6).ApproxCompare(price);
				if (num3 * num7 > 0)
				{
					break;
				}
				if (num7 == 0)
				{
					num5 = num6;
					num4++;
				}
				num6--;
				continue;
			}
			if (num4 < 2)
			{
				return null;
			}
			num2 = num5;
			int num8 = num2 - SwingPointNeighborhood;
			while (true)
			{
				if (num8 <= base.CurrentBar)
				{
					double price2 = GetPrice(isCheckingTop, num8);
					if (num8 >= num2 && num8 <= num)
					{
						valueData.Add(price2);
					}
					else
					{
						int num9 = price2.ApproxCompare(price);
						if (num3 * num9 >= 0)
						{
							break;
						}
						if (num8 < num2)
						{
							valueData2.Add(price2);
						}
						else
						{
							valueData3.Add(price2);
						}
					}
					num8++;
					continue;
				}
				if (valueData.MaxValue.ApproxCompare(valueData.MinValue) == 0)
				{
					return null;
				}
				double @double = (isCheckingTop ? Math.Max(valueData2.MinValue, valueData3.MinValue) : Math.Min(valueData2.MaxValue, valueData3.MaxValue));
				int num10 = (isCheckingTop ? valueData.MinValue : valueData.MaxValue).ApproxCompare(@double);
				if (num3 * num10 <= 0)
				{
					return null;
				}
				return new SwingPoint(isCheckingTop, price, num2, num);
			}
			return null;
		}
		return null;
	}

	private void FindBrokenSwingPointAndAddBosChochInfoToList(SortedList<int, SwingPoint> listSwingPoint, bool isTop)
	{
		try
		{
			if (listSwingPoint.Count <= 0)
			{
				return;
			}
			SwingPoint swingPoint = listSwingPoint.Values[listSwingPoint.Count - 1];
			if (swingPoint.IsBroken)
			{
				return;
			}
			int barEnd = swingPoint.BarEnd;
			double price = swingPoint.Price;
			double num = base.Open[0];
			double num2 = base.Close[0];
			double num3 = Math.Abs(num - num2);
			int barEnd2 = base.CurrentBar - 1;
			if (isTop)
			{
				if (price.ApproxCompare(num) > 0 && (!(num3 > 0.0) || ((num.ApproxCompare(price) < 0 || num2.ApproxCompare(price) > 0) && (num2.ApproxCompare(price) < 0 || num.ApproxCompare(price) > 0))))
				{
					return;
				}
				swingPoint.IsBroken = true;
				if (BosChochDisplayMode == gbKingOrderBlock_BosChochDisplayMode.All)
				{
					swingPoint.AllowDraw = true;
					if (isSwingPointSmartDisplayMode)
					{
						DrawSwingPointBuiltinMode(isTop, barEnd, base.CurrentBar - barEnd);
					}
				}
				BosChochInfo value;
				if (lastBosChochInfo == null)
				{
					value = new BosChochInfo(isTop, price, barEnd, BosChochType.Bos, barEnd2);
				}
				else
				{
					bool flag = lastBosChochInfo.BosChochType == BosChochType.Choch;
					bool isTop2 = lastBosChochInfo.IsTop;
					value = ((!flag && isTop2) ? new BosChochInfo(isTop, price, barEnd, BosChochType.Bos, barEnd2) : ((flag && isTop2) ? new BosChochInfo(isTop, price, barEnd, BosChochType.Bos, barEnd2) : ((flag || isTop2) ? new BosChochInfo(isTop, price, barEnd, BosChochType.Choch, barEnd2) : new BosChochInfo(isTop, price, barEnd, BosChochType.Choch, barEnd2))));
				}
				if (isLastBarOnEachTick)
				{
					flagNewBosChochTop = true;
				}
				listBosChochInfoTop.Add(barEnd, value);
				lastBosChochInfo = value;
			}
			else
			{
				if (price.ApproxCompare(num) < 0 && (!(num3 > 0.0) || ((num.ApproxCompare(price) < 0 || num2.ApproxCompare(price) > 0) && (num2.ApproxCompare(price) < 0 || num.ApproxCompare(price) > 0))))
				{
					return;
				}
				swingPoint.IsBroken = true;
				if (BosChochDisplayMode == gbKingOrderBlock_BosChochDisplayMode.All)
				{
					swingPoint.AllowDraw = true;
					if (isSwingPointSmartDisplayMode)
					{
						DrawSwingPointBuiltinMode(isTop, barEnd, base.CurrentBar - barEnd);
					}
				}
				BosChochInfo value2;
				if (lastBosChochInfo == null)
				{
					value2 = new BosChochInfo(isTop, price, barEnd, BosChochType.Bos, barEnd2);
				}
				else
				{
					bool flag2 = lastBosChochInfo.BosChochType == BosChochType.Choch;
					bool isTop3 = lastBosChochInfo.IsTop;
					value2 = ((!flag2 && isTop3) ? new BosChochInfo(isTop, price, barEnd, BosChochType.Choch, barEnd2) : ((flag2 && isTop3) ? new BosChochInfo(isTop, price, barEnd, BosChochType.Choch, barEnd2) : ((flag2 || isTop3) ? new BosChochInfo(isTop, price, barEnd, BosChochType.Bos, barEnd2) : new BosChochInfo(isTop, price, barEnd, BosChochType.Bos, barEnd2))));
				}
				if (isLastBarOnEachTick)
				{
					flagNewBosChochBottom = true;
				}
				listBosChochInfoBottom.Add(barEnd, value2);
				lastBosChochInfo = value2;
			}
		}
		catch (Exception exception)
		{
			Print(exception.ToString());
		}
	}

	protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
	{
		try
		{
			if (isCharting && !base.IsInHitTest)
			{
				base.OnRender(chartControl, chartScale);
				fromIndex = base.ChartBars.FromIndex;
				toIndex = base.ChartBars.ToIndex;
				DrawSwingPoints(chartScale, listSwingsTop, isTop: true);
				DrawSwingPoints(chartScale, listSwingsBottom, isTop: false);
				DrawBosChochLines(chartScale, listBosChochInfoTop);
				DrawBosChochLines(chartScale, listBosChochInfoBottom);
				DrawImbalances(chartScale, listImbalanceActive, isActive: true);
				DrawImbalances(chartScale, listImbalanceInactive, isActive: false);
				DrawOrderBlocks(chartScale, listOrderBlockActive, isActive: true);
				DrawOrderBlocks(chartScale, listOrderBlockInactive, isActive: false);
				DrawMarkers(chartScale);
			}
		}
		catch (Exception exception)
		{
			Print(exception.ToString());
		}
	}

	private void DrawImbalances(ChartScale chartScale, SortedList<int, ImbalanceInfo> listImbalance, bool isActive)
	{
		if (listImbalance.Count <= 0 || (!isActive && !ImbalanceInactiveEnabled))
		{
			return;
		}
		foreach (KeyValuePair<int, ImbalanceInfo> item in listImbalance)
		{
			ImbalanceInfo value = item.Value;
			if (value.BarStart <= toIndex && value.BarEnd >= fromIndex)
			{
				DrawOneBox(chartScale, value, isActive, isImbalance: true);
			}
		}
	}

	private void DrawOrderBlocks(ChartScale chartScale, SortedList<int, OrderBlockInfo> listOrderBlock, bool isActive)
	{
		if (listOrderBlock.Count <= 0 || (!isActive && !ImbalanceInactiveEnabled))
		{
			return;
		}
		foreach (KeyValuePair<int, OrderBlockInfo> item in listOrderBlock)
		{
			OrderBlockInfo value = item.Value;
			if (value.BarStart <= toIndex && value.BarEnd >= fromIndex)
			{
				DrawOneBox(chartScale, value, isActive, isImbalance: false);
			}
		}
	}

	private void DrawOneBox(ChartScale chartScale, ZoneInfo zoneInfo, bool isActive, bool isImbalance)
	{
		if (zoneInfo == null)
		{
			return;
		}
		int barStart = zoneInfo.BarStart;
		int barEnd = zoneInfo.BarEnd;
		if (barStart >= barEnd || zoneInfo.PriceBottom.ApproxCompare(base.ChartPanel.MaxValue) > 0 || zoneInfo.PriceTop.ApproxCompare(base.ChartPanel.MinValue) < 0)
		{
			return;
		}
		bool isTop = zoneInfo.IsTop;
		SharpDX.Direct2D1.GradientStop[] array = null;
		System.Windows.Media.Brush brush = null;
		if (GradientEnabled)
		{
			array = (isActive ? ((!isImbalance) ? (isTop ? orderBlockActiveTopGradientStop : orderBlockActiveBottomGradientStop) : (isTop ? imbalanceActiveTopGradientStop : imbalanceActiveBottomGradientStop)) : ((!isImbalance) ? (isTop ? orderBlockInactiveTopGradientStop : orderBlockInactiveBottomGradientStop) : (isTop ? imbalanceInactiveTopGradientStop : imbalanceInactiveBottomGradientStop)));
			if (array == null)
			{
				return;
			}
		}
		else
		{
			brush = ((!isActive) ? ((!isImbalance) ? (isTop ? orderBlockInactiveTop : orderBlockInactiveBottom) : (isTop ? imbalanceInactiveTop : imbalanceInactiveBottom)) : ((!isImbalance) ? (isTop ? orderBlockActiveTop : orderBlockActiveBottom) : (isTop ? imbalanceActiveTop : imbalanceActiveBottom)));
			if (brush.IsTransparent())
			{
				return;
			}
		}
		int num = ((barStart < fromIndex) ? (base.ChartPanel.X - 10) : base.ChartControl.GetXByBarIndex(base.ChartBars, barStart));
		int num2 = ((barEnd > toIndex) ? (base.ChartPanel.X + base.ChartPanel.W + 10) : base.ChartControl.GetXByBarIndex(base.ChartBars, barEnd));
		int yByValue = chartScale.GetYByValue(zoneInfo.PriceTop);
		int yByValue2 = chartScale.GetYByValue(zoneInfo.PriceBottom);
		int num3 = Math.Abs(num2 - num);
		int num4 = Math.Abs(yByValue - yByValue2);
		RectangleF rect = new RectangleF(num, yByValue, num3, num4);
		if (GradientEnabled)
		{
			SharpDX.Direct2D1.GradientStopCollection gradientStopCollection = new SharpDX.Direct2D1.GradientStopCollection(base.RenderTarget, array);
			LinearGradientBrushProperties linearGradientBrushProperties = new LinearGradientBrushProperties
			{
				StartPoint = rect.TopLeft,
				EndPoint = rect.BottomLeft
			};
			SharpDX.Direct2D1.LinearGradientBrush linearGradientBrush = new SharpDX.Direct2D1.LinearGradientBrush(base.RenderTarget, linearGradientBrushProperties, gradientStopCollection);
			base.RenderTarget.FillRectangle(rect, linearGradientBrush);
			linearGradientBrush.Dispose();
			gradientStopCollection.Dispose();
		}
		else
		{
			SharpDX.Direct2D1.Brush brush2 = brush.ToDxBrush(base.RenderTarget);
			base.RenderTarget.FillRectangle(rect, brush2);
		}
		Stroke stroke = (isActive ? ((!isImbalance) ? (isTop ? OrderBlockActiveBorderTop : OrderBlockActiveBorderBottom) : (isTop ? ImbalanceActiveBorderTop : ImbalanceActiveBorderBottom)) : ((!isImbalance) ? (isTop ? OrderBlockInactiveBorderTop : OrderBlockInactiveBorderBottom) : (isTop ? ImbalanceInactiveBorderTop : ImbalanceInactiveBorderBottom)));
		if (!stroke.Brush.IsTransparent())
		{
			SharpDX.Direct2D1.Brush brush3 = stroke.Brush.ToDxBrush(base.RenderTarget);
			StrokeStyle strokeStyle = stroke.StrokeStyle;
			float width = stroke.Width;
			base.RenderTarget.DrawRectangle(rect, brush3, width, strokeStyle);
		}
	}

	private void DrawBosChochLines(ChartScale chartScale, SortedList<int, BosChochInfo> listBosChochInfos)
	{
		if (BosChochDisplayMode == gbKingOrderBlock_BosChochDisplayMode.Disabled || listBosChochInfos.Count <= 0)
		{
			return;
		}
		foreach (KeyValuePair<int, BosChochInfo> listBosChochInfo in listBosChochInfos)
		{
			BosChochInfo value = listBosChochInfo.Value;
			if (value.BarStart <= toIndex && value.BarEnd >= fromIndex)
			{
				DrawOneBosChochLine(chartScale, value);
			}
		}
	}

	private void DrawOneBosChochLine(ChartScale chartScale, BosChochInfo bosChochInfo)
	{
		if (BosChochDisplayMode == gbKingOrderBlock_BosChochDisplayMode.Smart && !bosChochInfo.IsHasOrderBlock)
		{
			return;
		}
		int barStart = bosChochInfo.BarStart;
		bool isTop = bosChochInfo.IsTop;
		if (bosChochInfo == null)
		{
			return;
		}
		int barEnd = bosChochInfo.BarEnd;
		if (barStart >= barEnd)
		{
			return;
		}
		bool flag = bosChochInfo.BosChochType == BosChochType.Choch;
		double price = bosChochInfo.Price;
		if (isTop)
		{
			if (price.ApproxCompare(chartScale.MaxValue) >= 0 || (flag && !ChochTopLineEnabled) || (!flag && !BosTopLineEnabled))
			{
				return;
			}
		}
		else if (price.ApproxCompare(chartScale.MinValue) <= 0 || (flag && !ChochBottomLineEnabled) || (!flag && !BosBottomLineEnabled))
		{
			return;
		}
		Stroke stroke = ((!isTop) ? (flag ? ChochBottomLineStroke : BosBottomLineStroke) : (flag ? ChochTopLineStroke : BosTopLineStroke));
		System.Windows.Media.Brush brush = stroke.Brush;
		if (brush.IsTransparent())
		{
			return;
		}
		float width = stroke.Width;
		StrokeStyle strokeStyle = stroke.StrokeStyle;
		float num = base.ChartControl.GetXByBarIndex(base.ChartBars, barStart);
		float num2 = base.ChartControl.GetXByBarIndex(base.ChartBars, barEnd);
		float y = chartScale.GetYByValue(price);
		Vector2 point = new Vector2(num, y);
		Vector2 point2 = new Vector2(num2, y);
		AntialiasMode antialiasMode = base.RenderTarget.AntialiasMode;
		base.RenderTarget.AntialiasMode = AntialiasMode.PerPrimitive;
		base.RenderTarget.DrawLine(point, point2, brush.ToDxBrush(base.RenderTarget), width, strokeStyle);
		base.RenderTarget.AntialiasMode = antialiasMode;
		if (isTop)
		{
			if ((flag && !ChochTopTextEnabled) || (!flag && !BosTopTextEnabled))
			{
				return;
			}
		}
		else if ((flag && !ChochBottomTextEnabled) || (!flag && !BosBottomTextEnabled))
		{
			return;
		}
		string text = (flag ? "Choch" : "Bos");
		float x = (num + num2) / 2f;
		float y2 = chartScale.GetYByValue(price) + ((!isTop) ? 1 : (-1)) * BosChochTextOffset;
		if (!isTop)
		{
			if (flag)
			{
			}
		}
		else if (!flag)
		{
		}
		DrawTextDX(text, BosChochTextFont, x, y2, 0, (!isTop) ? 1 : (-1), brush, ScreenDPI, base.RenderTarget);
	}

	private void DrawSwingPoints(ChartScale chartScale, SortedList<int, SwingPoint> listSwingsPoint, bool isTop)
	{
		if (SwingPointDisplayMode == gbKingOrderBlock_SwingPointDisplayMode.Disabled || !isSwingPointCustomRenderingMethod || listSwingsPoint.Count <= 0)
		{
			return;
		}
		System.Windows.Media.Brush brush = (isTop ? SwingPointTop : SwingPointBottom);
		if (brush.IsTransparent())
		{
			return;
		}
		float barWidth = Math.Max(3f, (float)base.ChartControl.BarWidth - barOutlineWidth);
		for (int i = base.ChartBars.FromIndex; i <= base.ChartBars.ToIndex; i++)
		{
			if (listSwingsPoint.ContainsKey(i))
			{
				DrawOneSwingPoint(chartScale, listSwingsPoint[i], barWidth, brush);
			}
		}
	}

	private void DrawOneSwingPoint(ChartScale chartScale, SwingPoint swingPoint, float barWidth, System.Windows.Media.Brush brushSwingPoint)
	{
		try
		{
			if (!isSwingPointSmartDisplayMode || swingPoint.AllowDraw)
			{
				double price = swingPoint.Price;
				if (price.ApproxCompare(base.ChartPanel.MaxValue) <= 0 && price.ApproxCompare(base.ChartPanel.MinValue) >= 0)
				{
					int barEnd = swingPoint.BarEnd;
					float x = base.ChartControl.GetXByBarIndex(base.ChartBars, barEnd);
					float y = chartScale.GetYByValue(price);
					Vector2 center = new Vector2(x, y);
					float num = Math.Max(4.7f, barWidth);
					SharpDX.Direct2D1.Ellipse ellipse = new SharpDX.Direct2D1.Ellipse(center, num + 0.2f, num + 0.2f);
					SharpDX.Direct2D1.Ellipse ellipse2 = new SharpDX.Direct2D1.Ellipse(center, num, num);
					AntialiasMode antialiasMode = base.RenderTarget.AntialiasMode;
					base.RenderTarget.AntialiasMode = AntialiasMode.PerPrimitive;
					base.RenderTarget.DrawEllipse(ellipse, Brushes.Silver.ToDxBrush(base.RenderTarget));
					base.RenderTarget.FillEllipse(ellipse2, brushSwingPoint.ToDxBrush(base.RenderTarget));
					base.RenderTarget.AntialiasMode = antialiasMode;
				}
			}
		}
		catch (Exception exception)
		{
			Print(exception.ToString());
		}
	}

	private void AddMarker(int barIndex, bool isBullish, SignalType signalType)
	{
		if (MarkerEnabled)
		{
			MarkerInfo value = new MarkerInfo(barIndex, isBullish, signalType);
			if (!listMarkers.ContainsKey(barIndex))
			{
				listMarkers.Add(barIndex, value);
			}
			else
			{
				listMarkers[barIndex] = value;
			}
		}
	}

	private void DrawMarkers(ChartScale chartScale)
	{
		try
		{
			if (!MarkerEnabled || listMarkers == null || listMarkers.Count <= 0)
			{
				return;
			}
			for (int i = base.ChartBars.FromIndex; i <= Math.Min(base.CurrentBars[0], base.ChartBars.ToIndex); i++)
			{
				if (listMarkers.ContainsKey(i))
				{
					DrawOneMarker(chartScale, listMarkers[i]);
				}
			}
		}
		catch (Exception exception)
		{
			Print(exception.ToString());
		}
	}

	private void DrawOneMarker(ChartScale chartScale, MarkerInfo markerInfo)
	{
		try
		{
			string input = ((markerInfo.SignalType != SignalType.Breakout) ? (markerInfo.IsBullish ? MarkerStringReturnBullish : MarkerStringReturnBearish) : (markerInfo.IsBullish ? MarkerStringBreakUp : MarkerStringBreakDown));
			string text = input;
			if (string.IsNullOrWhiteSpace(text))
			{
				return;
			}
			bool isBullish = markerInfo.IsBullish;
			int barIndex = markerInfo.BarIndex;
			if ((isBullish || base.Highs[0].GetValueAt(barIndex).ApproxCompare(chartScale.MaxValue) < 0) && (!isBullish || base.Lows[0].GetValueAt(barIndex).ApproxCompare(chartScale.MinValue) > 0))
			{
				System.Windows.Media.Brush brush = ((markerInfo.SignalType != SignalType.Breakout) ? (isBullish ? MarkerBrushReturnBullish : MarkerBrushReturnBearish) : (isBullish ? MarkerBrushBreakUp : MarkerBrushBreakDown));
				if (!brush.IsTransparent())
				{
					int num = (isBullish ? 1 : (-1));
					float x = base.ChartControl.GetXByBarIndex(base.ChartBars, barIndex);
					float y = chartScale.GetYByValue((isBullish ? base.Lows[0] : base.Highs[0]).GetValueAt(barIndex)) + num * MarkerOffset;
					DrawTextDX(text, MarkerFont, x, y, 0, num, brush, ScreenDPI, base.RenderTarget);
				}
			}
		}
		catch (Exception exception)
		{
			Print(exception.ToString());
		}
	}

	private void PrintMarker(bool isBullish, SignalType signalType)
	{
		if (isCharting && MarkerEnabled && base.CurrentBars[0] >= base.BarsRequiredToPlot)
		{
			string tag = "gbKingOrderBlock.marker." + GetTagSuffix(isBullish, signalType) + base.CurrentBars[0];
			System.Windows.Media.Brush textBrush = ((signalType != SignalType.Breakout) ? (isBullish ? MarkerBrushReturnBullish : MarkerBrushReturnBearish) : (isBullish ? MarkerBrushBreakUp : MarkerBrushBreakDown));
			double y = (isBullish ? base.Lows[0][0] : base.Highs[0][0]);
			string input = ((signalType != SignalType.Breakout) ? (isBullish ? MarkerStringReturnBullish : MarkerStringReturnBearish) : (isBullish ? MarkerStringBreakUp : MarkerStringBreakDown));
			int num;
			using (var tf = MarkerFont.ToDirectWriteTextFormat())
			using (var tl = new SharpDX.DirectWrite.TextLayout(NinjaTrader.Core.Globals.DirectWriteFactory, input, tf, 400, 400))
				num = (int)tl.Metrics.Height;
			int yPixelOffset = ((!isBullish) ? 1 : (-1)) * (MarkerOffset + num / 2);
			Draw.Text(this, tag, base.IsAutoScale, input, 0, y, yPixelOffset, textBrush, MarkerFont, System.Windows.TextAlignment.Center, Brushes.Transparent, Brushes.Transparent, 0);
		}
	}

	private string GetTagSuffix(bool isBullish, SignalType signalType)
	{
		if (signalType == SignalType.Breakout)
		{
			if (!isBullish)
			{
				return "breakdown.";
			}
			return "breakup.";
		}
		if (!isBullish)
		{
			return "returnbearish.";
		}
		return "returnbullish.";
	}

	public override string FormatPriceMarker(double price)
	{
		return base.Instrument.MasterInstrument.FormatPrice(base.Instrument.MasterInstrument.RoundToTickSize(price));
	}

	private void MoveElementFromListToList<TValue>(int key, TValue element, SortedList<int, TValue> listActive, SortedList<int, TValue> listInactive)
	{
		if (!listInactive.ContainsKey(key))
		{
			listInactive.Add(key, element);
		}
		listActive.Remove(key);
	}

	private void AddElementToList<TValue>(int key, TValue element, SortedList<int, TValue> listActive)
	{
		if (!listActive.ContainsKey(key))
		{
			listActive.Add(key, element);
		}
		else
		{
			listActive[key] = element;
		}
	}

	private void RemoveLastElementInList<TValue>(SortedList<int, TValue> listData)
	{
		int count = listData.Count;
		if (count != 0)
		{
			listData.RemoveAt(count - 1);
		}
	}

	private bool IsReversalBar(int sign)
	{
		if (base.CurrentBar < 2)
		{
			return false;
		}
		if (sign * base.Closes[0][1].ApproxCompare(base.Closes[0][0]) > 0)
		{
			return sign * base.Closes[0][2].ApproxCompare(base.Closes[0][1]) < 0;
		}
		return false;
	}

	private void TriggerAlerts(bool isBullish, SignalType signalType)
	{
		if (base.State == State.Historical || (!PopupEnabled && !SoundEnabled && !EmailEnabled))
		{
			return;
		}
		DateTime now = DateTime.Now;
		if (now < nextAlert)
		{
			return;
		}
		string arg = now.ToString("HH:mm");
		string text = now.ToString("HH:mm:ss, dd MMM yyyy");
		nextAlert = now + TimeSpan.FromSeconds(AlertBlockingSeconds);
		string arg2 = $"{base.Instrument.FullName} ({base.BarsPeriod})";
		string arg3;
		string text2;
		if (signalType == SignalType.Breakout)
		{
			arg3 = (isBullish ? "BREAK UP" : "BREAK DOWN");
			text2 = "a";
		}
		else
		{
			arg3 = (isBullish ? "RETURN BULLISH" : "RETURN BEARISH");
			text2 = "a";
		}
		string text3 = "King Order Block" + $": {arg3} alert on {arg2} at {arg}";
		string popupMessage = string.Format("There has been " + text2 + " {0} signal.\n\nAlert chart: {1}.\nAlert time: {2}", arg3, arg2, text);
		string text4 = "\n_______________________\n\n";
		string text5 = popupMessage + text4 + "King Order Block by GreyBeard\nWebsite: http://greybeard.com";
		if (PopupEnabled && isCharting)
		{
			base.ChartControl.Dispatcher.InvokeAsync(delegate
			{
				if (alertWindow != null)
				{
					alertWindow.Close();
				}
				alertWindow = new Window
				{
					Title = "King Order Block",
					Width = 400,
					Height = 250,
					WindowStartupLocation = WindowStartupLocation.CenterScreen,
					Background = CreateOpacityBrush(PopupBackgroundBrush, PopupBackgroundOpacity),
					Content = new TextBlock
					{
						Text = popupMessage,
						Foreground = PopupTextBrush,
						FontSize = PopupTextSize,
						TextWrapping = TextWrapping.Wrap,
						Margin = new Thickness(15),
						VerticalAlignment = VerticalAlignment.Center
					}
				};
				alertWindowClosed = false;
				alertWindow.Closed += (s, args) => alertWindowClosed = true;
				alertWindow.Show();
			});
		}
		if (SoundEnabled)
		{
			string id = "alert @ " + text;
			soundPath = string.Concat(str2: (signalType != SignalType.Breakout) ? (isBullish ? SoundReturnBullish : SoundReturnBearish) : (isBullish ? SoundBreakUp : SoundBreakDown), str0: Globals.InstallDir, str1: "sounds\\");
			Alert(id, Priority.High, text3, soundPath, 0, Brushes.Red, Brushes.Yellow);
			if (SoundRearmEnabled && PopupEnabled && isCharting)
			{
				nextRearm = now + TimeSpan.FromSeconds(SoundRearmSeconds);
				rearmTimer.Start();
			}
		}
		if (EmailEnabled && EmailReceiver != "receiver@example.com")
		{
			SendMail(EmailReceiver, text3, text5);
		}
	}

	private void OnRearmTimerTick(object sender, EventArgs e)
	{
		TriggerCustomEvent(delegate
		{
			try
			{
				if (isCharting)
				{
					base.ChartControl.Dispatcher.InvokeAsync(delegate
					{
						if (alertWindow != null && alertWindowClosed)
						{
							rearmTimer.Stop();
						}
						else if (DateTime.Now >= nextRearm)
						{
							nextRearm = DateTime.Now + TimeSpan.FromSeconds(SoundRearmSeconds);
							PlaySound(soundPath);
						}
					});
				}
			}
			catch
			{
			}
		}, e);
	}

	private static System.Windows.Media.Brush CreateOpacityBrush(System.Windows.Media.Brush brush, int opacityPercent)
	{
		if (brush == null || brush.IsTransparent())
			return brush;
		var scb = (System.Windows.Media.SolidColorBrush)brush;
		byte alpha = (byte)(255 * opacityPercent / 100);
		var color = System.Windows.Media.Color.FromArgb(alpha, scb.Color.R, scb.Color.G, scb.Color.B);
		var result = new System.Windows.Media.SolidColorBrush(color);
		result.Freeze();
		return result;
	}

	private void DrawTextDX(string text, SimpleFont font, float x, float y, int xAlign, int ySign, System.Windows.Media.Brush brush, int screenDPI, SharpDX.Direct2D1.RenderTarget rt)
	{
		if (string.IsNullOrEmpty(text) || brush.IsTransparent())
			return;
		using (var textFormat = font.ToDirectWriteTextFormat())
		{
			textFormat.TextAlignment = SharpDX.DirectWrite.TextAlignment.Center;
			textFormat.ParagraphAlignment = SharpDX.DirectWrite.ParagraphAlignment.Near;
			using (var textLayout = new SharpDX.DirectWrite.TextLayout(NinjaTrader.Core.Globals.DirectWriteFactory, text, textFormat, 400, 400))
			{
				float textHeight = textLayout.Metrics.Height;
				float drawX = x - 200;
				float drawY = (ySign < 0) ? y - textHeight : y;
				using (var dxBrush = brush.ToDxBrush(rt))
				{
					rt.DrawTextLayout(new SharpDX.Vector2(drawX, drawY), textLayout, dxBrush);
				}
			}
		}
	}

} // class gbKingOrderBlock


// ==== gbPANAKanal ===============================================
[CategoryOrder("Critical", 1000070)]
[CategoryOrder("Developer", 0)]
[CategoryOrder("Special", 1000060)]
[CategoryOrder("Graphics", 1000020)]
[CategoryOrder("General", 1000010)]
[CategoryOrder("Alerts", 1000040)]
[CategoryOrder("Gradient", 1000030)]
public class gbPANAKanal : Indicator
{
	private struct MarkerInfo
	{
		public int BarIndex { get; set; }

		public bool IsBullish { get; set; }

		public SignalInfo SignalInfo { get; set; }

		}

	private class LineInfo
	{
		public bool IsTop { get; set; }

		public double Price { get; set; }

		public int BarStart { get; set; }

		public int BarEnd { get; set; }

		public bool IsBroken { get; set; }

		}

	private struct TraillingStopAndFibonacciInfo
	{
		public double TraillingStop { get; set; }

		public double Fibonacci1 { get; set; }

		public double Fibonacci2 { get; set; }

		}

	private enum SignalInfo
	{
		Trend,
		Break,
		Pullback
	}

	private Series<double> seriesDiffHighLow;

	private Series<double> seriesTrueRange;

	private Series<double> seriesWildMA;

	private Series<double> seriesUp;

	private Series<double> seriesDown;

	private Series<double> seriesFibonacci1;

	private Series<double> seriesFibonacci2;

	private Series<double> seriesSK;

	private Dictionary<int, LineInfo> dictLineInfo;

	private Dictionary<int, MarkerInfo> dictMarkers;

	private bool isCustomMarkerRenderingMethod;

	private bool isOnBarCloseMode;

	private bool isUptrend;

	private Window alertWindow;

	private const string prefix = "gbPANAKanal";

	private const string indicatorName = "PANA Kanal";

	private bool isCharting;

	private const double swingArmFibonacciLevel1 = 0.618;

	private const double swingArmFibonacciLevel2 = 0.786;

	private int signalStateKeltner;

	private int prevSignalStateKeltner;

	private bool hasPbSignal;

	private bool prevHasPbSignal;

	private int pbSignalIndex;

	private int prevPbSignalIndex;

	private bool isResetConditionPb;

	private bool hasBreakSignal;

	private bool prevHasBreakSignal;

	private int breakUpSignalIndex;

	private int breakDownSignalIndex;

	private int prevBreakSignalIndex;

	private bool isCheckPriceInZone = true;

	private bool isPriceInZone;

	private bool prevIsPriceInZone;

	private bool isResetConditionBreak;

	private int crossUpIndex;

	private int crossDownIndex;

	private bool hasBreakSignalOnEachTick;

	private bool hasPbSignalOnEachTick;

	private bool isLineBrokenByCandleUp;

	private bool isLineBrokenByCandleDown;

	private bool isBreakSignalSplitOk;

	private int signalTrend1;

	private double extremum1;

	private SignalInfo signalInfo;

	private string tagRegion1 = string.Empty;

	private string tagRegion2 = string.Empty;

	private DateTime nextAlert = DateTime.MinValue;

	private DateTime nextRearm = DateTime.MinValue;

	private string soundPath = string.Empty;

	private DispatcherTimer rearmTimer;


	[Display(Name = "Condition: Trend Start", Order = 0, GroupName = "Alerts")]
	public bool ConditionTrendStart { get; set; }

	[Display(Name = "Condition: Trend Pullback", Order = 1, GroupName = "Alerts")]
	public bool ConditionTrendPullback { get; set; }

	[Display(Name = "Condition: Break", Order = 2, GroupName = "Alerts")]
	public bool ConditionBreak { get; set; }

	[Display(Name = "Popup: Enabled", Order = 3, GroupName = "Alerts")]
	public bool PopupEnabled { get; set; }

	[XmlIgnore]
	[Display(Name = "Popup: Background Color", Order = 4, GroupName = "Alerts")]
	public Brush PopupBackgroundBrush { get; set; }

	[Browsable(false)]
	public string PopupBackgroundBrush_Serialize
	{
		get
		{
			return Serialize.BrushToString(PopupBackgroundBrush);
		}
		set
		{
			PopupBackgroundBrush = Serialize.StringToBrush(value);
		}
	}

	[Display(Name = "Popup: Background Opacity", Order = 5, GroupName = "Alerts")]
	public int PopupBackgroundOpacity { get; set; }

	[XmlIgnore]
	[Display(Name = "Popup: Text Color", Order = 6, GroupName = "Alerts")]
	public Brush PopupTextBrush { get; set; }

	[Browsable(false)]
	public string PopupTextBrush_Serialize
	{
		get
		{
			return Serialize.BrushToString(PopupTextBrush);
		}
		set
		{
			PopupTextBrush = Serialize.StringToBrush(value);
		}
	}

	[Display(Name = "Popup: Text Size", Order = 7, GroupName = "Alerts")]
	public int PopupTextSize { get; set; }

	[Display(Name = "Popup: Button Color", Order = 8, GroupName = "Alerts")]
	[XmlIgnore]
	public Brush PopupButtonBrush { get; set; }

	[Browsable(false)]
	public string PopupButtonBrush_Serialize
	{
		get
		{
			return Serialize.BrushToString(PopupButtonBrush);
		}
		set
		{
			PopupButtonBrush = Serialize.StringToBrush(value);
		}
	}

	[Display(Name = "Sound: Enabled", Order = 10, GroupName = "Alerts")]
	public bool SoundEnabled { get; set; }

	[TypeConverter(typeof(gbPANAKanal_SoundConverter))]
	[Display(Name = "Sound: Uptrend Start", Order = 11, GroupName = "Alerts")]
	public string SoundUptrendStart { get; set; }

	[Display(Name = "Sound: Downtrend Start", Order = 12, GroupName = "Alerts")]
	[TypeConverter(typeof(gbPANAKanal_SoundConverter))]
	public string SoundDowntrendStart { get; set; }

	[TypeConverter(typeof(gbPANAKanal_SoundConverter))]
	[Display(Name = "Sound: Uptrend Pullback", Order = 13, GroupName = "Alerts")]
	public string SoundUptrendPullback { get; set; }

	[Display(Name = "Sound: Downtrend Pullback", Order = 14, GroupName = "Alerts")]
	[TypeConverter(typeof(gbPANAKanal_SoundConverter))]
	public string SoundDowntrendPullback { get; set; }

	[TypeConverter(typeof(gbPANAKanal_SoundConverter))]
	[Display(Name = "Sound: Break Up", Order = 15, GroupName = "Alerts")]
	public string SoundBreakUp { get; set; }

	[Display(Name = "Sound: Break Down", Order = 16, GroupName = "Alerts")]
	[TypeConverter(typeof(gbPANAKanal_SoundConverter))]
	public string SoundBreakDown { get; set; }

	[Display(Name = "Sound: Rearm Enabled", Order = 17, GroupName = "Alerts")]
	public bool SoundRearmEnabled { get; set; }

	[Display(Name = "Sound: Rearm Seconds ", Order = 18, GroupName = "Alerts")]
	public int SoundRearmSeconds { get; set; }

	[Display(Name = "Email: Enabled", Order = 20, GroupName = "Alerts")]
	public bool EmailEnabled { get; set; }

	[Display(Name = "Email: Receiver", Order = 21, GroupName = "Alerts")]
	public string EmailReceiver { get; set; }

	[Display(Name = "Marker: Enabled", Order = 30, GroupName = "Alerts")]
	public bool MarkerEnabled { get; set; }

	[Display(Name = "Marker: Rendering Method", Order = 31, GroupName = "Alerts", Description = "\"Custom\" rendering provides a better appearance, and is therefore recommended in most cases.\nIf your computer is weak or slow, please switch to \"Builtin\" rendering.")]
	public gbPANAKanal_MarkerRenderingMethod MarkerRenderingMethod { get; set; }

	[XmlIgnore]
	[Display(Name = "Marker: Color Bullish", Order = 32, GroupName = "Alerts")]
	public Brush MarkerBrushBullish { get; set; }

	[Browsable(false)]
	public string MarkerBrushBullish_Serialize
	{
		get
		{
			return Serialize.BrushToString(MarkerBrushBullish);
		}
		set
		{
			MarkerBrushBullish = Serialize.StringToBrush(value);
		}
	}

	[Display(Name = "Marker: Color Bearish", Order = 33, GroupName = "Alerts")]
	[XmlIgnore]
	public Brush MarkerBrushBearish { get; set; }

	[Browsable(false)]
	public string MarkerBrushBearish_Serialize
	{
		get
		{
			return Serialize.BrushToString(MarkerBrushBearish);
		}
		set
		{
			MarkerBrushBearish = Serialize.StringToBrush(value);
		}
	}

	[Display(Name = "Marker: String Uptrend Start", Order = 34, GroupName = "Alerts")]
	public string MarkerStringUptrendStart { get; set; }

	[Display(Name = "Marker: String Downtrend Start", Order = 35, GroupName = "Alerts")]
	public string MarkerStringDowntrendStart { get; set; }

	[Display(Name = "Marker: String Uptrend Pullback", Order = 36, GroupName = "Alerts")]
	public string MarkerStringUptrendPullback { get; set; }

	[Display(Name = "Marker: String Downtrend Pullback", Order = 37, GroupName = "Alerts")]
	public string MarkerStringDowntrendPullback { get; set; }

	[Display(Name = "Marker: String Break Up", Order = 38, GroupName = "Alerts")]
	public string MarkerStringBreakUp { get; set; }

	[Display(Name = "Marker: String Break Down", Order = 39, GroupName = "Alerts")]
	public string MarkerStringBreakDown { get; set; }

	[Display(Name = "Marker: Font", Order = 40, GroupName = "Alerts")]
	public SimpleFont MarkerFont { get; set; }

	[Display(Name = "Marker: Offset", Order = 41, GroupName = "Alerts")]
	public int MarkerOffset { get; set; }
	[Display(Name = "Alert Blocking (Seconds)", Order = 50, GroupName = "Alerts", Description = "The minimum interval between 2 consecutive alerts")]
	public int AlertBlockingSeconds { get; set; }

	[Display(Name = "Telegram:", Order = 0, GroupName = "Developer")]
	public string Website => "https://t.me/val1312q";

	[Display(Name = "Update", Order = 10, GroupName = "Developer")]
	public new string Update => "05 Oct 2024";

	[Display(Name = "Screen DPI", Order = 100, GroupName = "General")]
	public int ScreenDPI { get; set; }

	[Display(Name = "Plot: Enabled", Order = 0, GroupName = "Graphics")]
	public bool PlotEnabled { get; set; }

	[XmlIgnore]
	[Display(Name = "Plot: Uptrend", Order = 3, GroupName = "Graphics")]
	public Brush PlotUptrend { get; set; }

	[Browsable(false)]
	public string PlotUptrend_Serialize
	{
		get
		{
			return Serialize.BrushToString(PlotUptrend);
		}
		set
		{
			PlotUptrend = Serialize.StringToBrush(value);
		}
	}

	[XmlIgnore]
	[Display(Name = "Plot: Downtrend", Order = 4, GroupName = "Graphics")]
	public Brush PlotDowntrend { get; set; }

	[Browsable(false)]
	public string PlotDowntrend_Serialize
	{
		get
		{
			return Serialize.BrushToString(PlotDowntrend);
		}
		set
		{
			PlotDowntrend = Serialize.StringToBrush(value);
		}
	}

	[Display(Name = "Bar: Enabled", Order = 10, GroupName = "Graphics")]
	public bool BarEnabled { get; set; }

	[XmlIgnore]
	[Display(Name = "Bar: Uptrend", Order = 20, GroupName = "Graphics")]
	public Brush BarUptrend { get; set; }

	[Browsable(false)]
	public string BarUptrend_Serialize
	{
		get
		{
			return Serialize.BrushToString(BarUptrend);
		}
		set
		{
			BarUptrend = Serialize.StringToBrush(value);
		}
	}

	[Display(Name = "Bar: Downtrend", Order = 21, GroupName = "Graphics")]
	[XmlIgnore]
	public Brush BarDowntrend { get; set; }

	[Browsable(false)]
	public string BarDowntrend_Serialize
	{
		get
		{
			return Serialize.BrushToString(BarDowntrend);
		}
		set
		{
			BarDowntrend = Serialize.StringToBrush(value);
		}
	}

	[Display(Name = "Bar: Outline Enabled", Order = 70, GroupName = "Graphics")]
	public bool BarOutlineEnabled { get; set; }

	[Display(Name = "Bar: Bias Based", Order = 71, GroupName = "Graphics")]
	public bool BarBiasBased { get; set; }

	[Display(Name = "Region: Enabled", Order = 80, GroupName = "Graphics")]
	public bool RegionEnabled { get; set; }

	[XmlIgnore]
	[Display(Name = "Region: Uptrend", Order = 82, GroupName = "Graphics")]
	public Brush RegionUptrend { get; set; }

	[Browsable(false)]
	public string RegionUptrend_Serialize
	{
		get
		{
			return Serialize.BrushToString(RegionUptrend);
		}
		set
		{
			RegionUptrend = Serialize.StringToBrush(value);
		}
	}

	[Display(Name = "Region: Downtrend", Order = 84, GroupName = "Graphics")]
	[XmlIgnore]
	public Brush RegionDowntrend { get; set; }

	[Browsable(false)]
	public string RegionDowntrend_Serialize
	{
		get
		{
			return Serialize.BrushToString(RegionDowntrend);
		}
		set
		{
			RegionDowntrend = Serialize.StringToBrush(value);
		}
	}

	[Display(Name = "Region: Opacity #1", Order = 86, GroupName = "Graphics")]
	public int RegionOpacity1 { get; set; }
	[Display(Name = "Region: Opacity #2", Order = 88, GroupName = "Graphics")]
	public int RegionOpacity2 { get; set; }

	[Display(Name = "Line: Active Top", Order = 90, GroupName = "Graphics")]
	public Stroke LineActiveTop { get; set; }

	[Display(Name = "Line: Active Bottom", Order = 92, GroupName = "Graphics")]
	public Stroke LineActiveBottom { get; set; }

	[Display(Name = "Line: Inactive Enabled", Order = 94, GroupName = "Graphics")]
	public bool LineInactiveEnabled { get; set; }

	[Display(Name = "Line: Inactive Top", Order = 96, GroupName = "Graphics")]
	public Stroke LineInactiveTop { get; set; }

	[Display(Name = "Line: Inactive Bottom", Order = 98, GroupName = "Graphics")]
	public Stroke LineInactiveBottom { get; set; }

	[Display(Name = "Period", Order = 2, GroupName = "Parameters")]
	[NinjaScriptProperty]
	public int Period { get; set; }
	[Display(Name = "Factor", Order = 4, GroupName = "Parameters")]
	[NinjaScriptProperty]
	public double Factor { get; set; }
	[NinjaScriptProperty]
	[Display(Name = "Middle Period", Order = 6, GroupName = "Parameters")]
	public int MiddlePeriod { get; set; }
	[Display(Name = "Signal Break Split (Bars)", Order = 8, GroupName = "Parameters")]
	[NinjaScriptProperty]
	public int SignalBreakSplit { get; set; }
	[Display(Name = "Signal Pullback Finding Period", Order = 10, GroupName = "Parameters")]
	[NinjaScriptProperty]
	public int SignalPullbackFindingPeriod { get; set; }

	[Display(Name = "Z Order", Order = 0, GroupName = "Special")]
	public int IndicatorZOrder { get; set; }

	[Display(Name = "User Note", Order = 10, GroupName = "Special")]
	public string UserNote { get; set; }

	[XmlIgnore]
	[Browsable(false)]
	public Series<double> Extremum => Values[0];

	[Browsable(false)]
	[XmlIgnore]
	public Series<double> Middle => Values[1];

	[XmlIgnore]
	[Browsable(false)]
	public Series<double> TrailingStop => Values[2];

	[Browsable(false)]
	[XmlIgnore]
	public Series<double> Signal_Trend => Values[3];

	[XmlIgnore]
	[Browsable(false)]
	public Series<double> Signal_Trade => Values[4];

	[XmlIgnore]
	public override string DisplayName
	{
		get
		{
			if (!(Parent is MarketAnalyzerColumnBase))
			{
				return "PANA Kanal by GreyBeard" + GetUserNote();
			}
			return base.DisplayName;
		}
	}

	private string GetUserNote()
	{
		string text = UserNote.Trim();
		if (!string.IsNullOrWhiteSpace(text))
		{
			text = text.ToLower();
			string text2 = "instrument";
			string text3 = "period";
			if (text.Contains(text2) && Instruments[0] != null)
			{
				text = text.Replace(text2, Instruments[0].FullName);
			}
			if (text.Contains(text3) && BarsPeriods[0] != null)
			{
				text = text.Replace(text3, ((object)BarsPeriods[0]).ToString());
			}
			return " (" + text + ")";
		}
		return string.Empty;
	}

	protected override void OnStateChange()
	{
		switch (State)
		{
		case State.SetDefaults:
			Name = "gbPANAKanal";
			Description = "PANA Kanal";
			Calculate = Calculate.OnBarClose;
			IsOverlay = true;
			DisplayInDataBox = true;
			DrawOnPricePanel = true;
			IsSuspendedWhileInactive = false;
			IsAutoScale = true;
			BarsRequiredToPlot = 0;
			ShowTransparentPlotsInDataBox = true;

			// Parameters
			Period = 20;
			Factor = 4.0;
			MiddlePeriod = 14;
			SignalBreakSplit = 20;
			SignalPullbackFindingPeriod = 10;

			// General
			ScreenDPI = 100;

			// Graphics
			PlotEnabled = true;
			PlotUptrend = Brushes.DodgerBlue;             // #FF1E90FF
			PlotDowntrend = Brushes.Magenta;               // #FFFF00FF
			BarEnabled = true;
			BarUptrend = Brushes.DodgerBlue;               // #FF1E90FF
			BarDowntrend = Brushes.Plum;                   // #FFDDA0DD
			BarOutlineEnabled = true;
			BarBiasBased = true;
			RegionEnabled = true;
			RegionUptrend = Brushes.LimeGreen;             // #FF32CD32
			RegionDowntrend = Brushes.HotPink;             // #FFFF69B4
			RegionOpacity1 = 30;
			RegionOpacity2 = 50;
			LineActiveTop = new Stroke(Brushes.LimeGreen, DashStyleHelper.Solid, 5);       // #FF32CD32
			LineActiveBottom = new Stroke(Brushes.Crimson, DashStyleHelper.Solid, 5);      // #FFDC143C
			LineInactiveEnabled = true;
			LineInactiveTop = new Stroke(Brushes.LimeGreen, DashStyleHelper.Solid, 5, 40);
			LineInactiveBottom = new Stroke(Brushes.Crimson, DashStyleHelper.Solid, 5, 40);

			// Alerts
			ConditionTrendStart = true;
			ConditionTrendPullback = true;
			ConditionBreak = true;
			PopupEnabled = false;
			PopupBackgroundBrush = Brushes.Gold;           // #FFFFD700
			PopupBackgroundOpacity = 60;
			PopupTextBrush = Brushes.DarkSlateGray;        // #FF2F4F4F
			PopupTextSize = 16;
			PopupButtonBrush = Brushes.Transparent;        // #00FFFFFF
			SoundEnabled = false;
			SoundUptrendStart = "Alert4.wav";
			SoundDowntrendStart = "Alert3.wav";
			SoundUptrendPullback = "Alert4.wav";
			SoundDowntrendPullback = "Alert3.wav";
			SoundBreakUp = "Alert1.wav";
			SoundBreakDown = "Alert2.wav";
			SoundRearmEnabled = true;
			SoundRearmSeconds = 5;
			EmailEnabled = false;
			EmailReceiver = "receiver@example.com";
			MarkerEnabled = true;
			MarkerRenderingMethod = gbPANAKanal_MarkerRenderingMethod.Custom;
			MarkerBrushBullish = Brushes.LimeGreen;        // #FF32CD32
			MarkerBrushBearish = Brushes.OrangeRed;        // #FFFF4500
			MarkerStringUptrendStart = "\u25b2 + Trend";
			MarkerStringDowntrendStart = "Trend + \u25bc";
			MarkerStringUptrendPullback = "\ud83e\udc31 + Pb";
			MarkerStringDowntrendPullback = "Pb + \ud83e\udc33";
			MarkerStringBreakUp = "\ud83e\udc75 + Break";
			MarkerStringBreakDown = "Break + \ud83e\udc76";
			MarkerFont = new SimpleFont("Arial", 20);
			MarkerOffset = 10;
			AlertBlockingSeconds = 60;

			// Special
			IndicatorZOrder = 0;
			UserNote = "instrument (period)";

			// Plots
			AddPlot(new Stroke(Brushes.Yellow, 1), PlotStyle.Dot, "Extremum");                              // #FFFFFF00
			AddPlot(new Stroke(Brushes.Goldenrod, DashStyleHelper.Dot, 1), PlotStyle.Dot, "Middle");         // #FFDAA520
			AddPlot(new Stroke(Brushes.Goldenrod, DashStyleHelper.Solid, 3), PlotStyle.Line, "Trailing Stop"); // #FFDAA520
			AddPlot(new Stroke(Brushes.Transparent, 1), PlotStyle.Line, "Signal: Trend");
			AddPlot(new Stroke(Brushes.Transparent, 1), PlotStyle.Line, "Signal: Trade");
			break;

		case State.DataLoaded:
			seriesDiffHighLow = new Series<double>(this);
			seriesTrueRange = new Series<double>(this);
			seriesWildMA = new Series<double>(this);
			seriesUp = new Series<double>(this);
			seriesDown = new Series<double>(this);
			seriesFibonacci1 = new Series<double>(this, MaximumBarsLookBack.Infinite);
			seriesFibonacci2 = new Series<double>(this, MaximumBarsLookBack.Infinite);
			seriesSK = new Series<double>(this);
			dictLineInfo = new Dictionary<int, LineInfo>();
			dictMarkers = new Dictionary<int, MarkerInfo>();
			isCustomMarkerRenderingMethod = MarkerRenderingMethod == gbPANAKanal_MarkerRenderingMethod.Custom;
			isOnBarCloseMode = Calculate == Calculate.OnBarClose;
			rearmTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
			rearmTimer.Tick += OnRearmTimerTick;
			break;

		case State.Historical:
			isCharting = ChartControl != null;
			break;

		case State.Terminated:
			if (isCharting)
			{
				if (alertWindow != null)
					ChartControl.Dispatcher.InvokeAsync(delegate { alertWindow.Close(); });
			}
			if (rearmTimer != null)
			{
				rearmTimer.Stop();
				rearmTimer = null;
			}
			break;
		}
	}

	protected override void OnBarUpdate()
	{
		ComputeKeltner();
		if (IsFirstTickOfBar)
		{
			prevSignalStateKeltner = signalStateKeltner;
		}
		signalStateKeltner = GetStateKeltner(prevSignalStateKeltner, Middle[0]);
		if (CurrentBar != 0)
		{
			double num = High[0];
			double num2 = High[1];
			double num3 = Low[0];
			double num4 = Low[1];
			double num5 = Close[0];
			double num6 = Close[1];
			seriesDiffHighLow[0] = num - num3;
			double val = Math.Min(seriesDiffHighLow[0], 1.5 * SMA(seriesDiffHighLow, Period)[0]);
			double val2 = ((MathExtentions.ApproxCompare(num3, num2) > 0) ? (num - num6 - 0.5 * (num3 - num2)) : (num - num6));
			double val3 = ((MathExtentions.ApproxCompare(num, num4) < 0) ? (num6 - num3 - 0.5 * (num4 - num)) : (num6 - num3));
			seriesTrueRange[0] = Math.Max(val, Math.Max(val2, val3));
			seriesWildMA[0] = seriesWildMA[1] + (seriesTrueRange[0] - seriesWildMA[1]) / (double)Period;
			double num7 = Factor * seriesWildMA[0];
			double num8 = num5 - num7;
			double num9 = num5 + num7;
			double num10 = seriesUp[1];
			double num11 = seriesDown[1];
			seriesUp[0] = ((MathExtentions.ApproxCompare(num6, num10) <= 0) ? num8 : Math.Max(num8, num10));
			seriesDown[0] = ((MathExtentions.ApproxCompare(num6, num11) >= 0) ? num9 : Math.Min(num9, num11));
			if (IsFirstTickOfBar)
			{
				hasPbSignalOnEachTick = false;
				hasBreakSignalOnEachTick = false;
				isPriceInZone = !isResetConditionBreak && isCheckPriceInZone;
				isBreakSignalSplitOk = !hasBreakSignal || CurrentBar - (((!isUptrend) ? breakDownSignalIndex : breakUpSignalIndex) - 1) >= SignalBreakSplit;
				if (!hasPbSignal && isResetConditionPb)
				{
					hasPbSignal = true;
					isResetConditionPb = false;
				}
				signalTrend1 = Convert.ToInt32(Signal_Trend[1]);
				extremum1 = Extremum[1];
			}
			int num12 = 0;
			TraillingStopAndFibonacciInfo traillingStopAndFibonacciInfo = default(TraillingStopAndFibonacciInfo);
			double num13;
			if (!isUptrend)
			{
				if (isOnBarCloseMode || signalTrend1 != 1)
				{
					if (MathExtentions.ApproxCompare(num5, num11) <= 0)
					{
						num13 = Math.Min(extremum1, num3);
						traillingStopAndFibonacciInfo = ComputeTraillingStopAndFibonacci(isUptrend, num13);
						num12 = CheckBreakoutAndPullbackSignal(traillingStopAndFibonacciInfo.Fibonacci1);
						CheckLineBrokenAndAddNewLine();
					}
					else
					{
						isUptrend = true;
						num12 = 1;
						prevIsPriceInZone = isCheckPriceInZone;
						isCheckPriceInZone = true;
						isResetConditionBreak = false;
						prevHasPbSignal = hasPbSignal;
						hasPbSignal = false;
						prevPbSignalIndex = pbSignalIndex;
						pbSignalIndex = 0;
						prevHasBreakSignal = hasBreakSignal;
						hasBreakSignal = false;
						breakUpSignalIndex = CurrentBar;
						crossUpIndex = CurrentBar;
						isLineBrokenByCandleUp = false;
						num13 = num;
						traillingStopAndFibonacciInfo = ComputeTraillingStopAndFibonacci(isUptrend, num13);
						if (!isOnBarCloseMode)
						{
							if (hasBreakSignalOnEachTick)
							{
								if (ConditionBreak)
								{
									RemoveMarker();
									RemovePopupAndSound();
								}
								breakDownSignalIndex = prevBreakSignalIndex;
								hasBreakSignalOnEachTick = false;
							}
							if (hasPbSignalOnEachTick)
							{
								if (ConditionTrendPullback)
								{
									RemoveMarker();
									RemovePopupAndSound();
								}
								isResetConditionPb = false;
								hasPbSignalOnEachTick = false;
							}
						}
						CheckLineBrokenAndAddNewLine(isTrendSwitched: true);
					}
				}
				else if (MathExtentions.ApproxCompare(num5, num10) < 0)
				{
					AddNewLine();
					num13 = num3;
					traillingStopAndFibonacciInfo = ComputeTraillingStopAndFibonacci(isUptrend, num13);
				}
				else
				{
					isUptrend = true;
					hasPbSignal = prevHasPbSignal;
					pbSignalIndex = prevPbSignalIndex;
					hasBreakSignal = prevHasBreakSignal;
					isCheckPriceInZone = prevIsPriceInZone;
					num13 = Math.Max(extremum1, num);
					traillingStopAndFibonacciInfo = ComputeTraillingStopAndFibonacci(isUptrend, num13);
					if (ConditionTrendStart)
					{
						RemoveMarker();
						RemovePopupAndSound();
					}
					num12 = CheckBreakoutAndPullbackSignal(traillingStopAndFibonacciInfo.Fibonacci1);
					if (dictLineInfo.ContainsKey(CurrentBar))
					{
						dictLineInfo.Remove(CurrentBar);
						CheckLineBrokenAndAddNewLine();
					}
				}
			}
			else if (isOnBarCloseMode || signalTrend1 != -1)
			{
				if (MathExtentions.ApproxCompare(num5, num10) >= 0)
				{
					num13 = Math.Max(extremum1, num);
					traillingStopAndFibonacciInfo = ComputeTraillingStopAndFibonacci(isUptrend, num13);
					num12 = CheckBreakoutAndPullbackSignal(traillingStopAndFibonacciInfo.Fibonacci1);
					CheckLineBrokenAndAddNewLine();
				}
				else
				{
					isUptrend = false;
					num12 = -1;
					prevIsPriceInZone = isCheckPriceInZone;
					isCheckPriceInZone = true;
					isResetConditionBreak = false;
					prevHasPbSignal = hasPbSignal;
					hasPbSignal = false;
					prevPbSignalIndex = pbSignalIndex;
					pbSignalIndex = 0;
					prevHasBreakSignal = hasBreakSignal;
					hasBreakSignal = false;
					breakDownSignalIndex = CurrentBar;
					crossDownIndex = CurrentBar;
					isLineBrokenByCandleDown = false;
					num13 = num3;
					traillingStopAndFibonacciInfo = ComputeTraillingStopAndFibonacci(isUptrend, num13);
					if (!isOnBarCloseMode)
					{
						if (hasBreakSignalOnEachTick)
						{
							if (ConditionBreak)
							{
								RemoveMarker();
								RemovePopupAndSound();
							}
							breakUpSignalIndex = prevBreakSignalIndex;
							hasBreakSignalOnEachTick = false;
						}
						if (hasPbSignalOnEachTick)
						{
							if (ConditionTrendPullback)
							{
								RemoveMarker();
								RemovePopupAndSound();
							}
							isResetConditionPb = false;
							hasPbSignalOnEachTick = false;
						}
					}
					CheckLineBrokenAndAddNewLine(isTrendSwitched: true);
				}
			}
			else if (MathExtentions.ApproxCompare(num5, num11) > 0)
			{
				AddNewLine();
				num13 = num;
				traillingStopAndFibonacciInfo = ComputeTraillingStopAndFibonacci(isUptrend, num13);
			}
			else
			{
				isUptrend = false;
				hasPbSignal = prevHasPbSignal;
				pbSignalIndex = prevPbSignalIndex;
				hasBreakSignal = prevHasBreakSignal;
				isCheckPriceInZone = prevIsPriceInZone;
				num13 = Math.Min(extremum1, num3);
				traillingStopAndFibonacciInfo = ComputeTraillingStopAndFibonacci(isUptrend, num13);
				if (ConditionTrendStart)
				{
					RemoveMarker();
					RemovePopupAndSound();
				}
				num12 = CheckBreakoutAndPullbackSignal(traillingStopAndFibonacciInfo.Fibonacci1);
				if (dictLineInfo.ContainsKey(CurrentBar))
				{
					dictLineInfo.Remove(CurrentBar);
					CheckLineBrokenAndAddNewLine();
				}
			}
			Extremum[0] = num13;
			TrailingStop[0] = traillingStopAndFibonacciInfo.TraillingStop;
			seriesFibonacci1[0] = traillingStopAndFibonacciInfo.Fibonacci1;
			seriesFibonacci2[0] = traillingStopAndFibonacciInfo.Fibonacci2;
			Signal_Trend[0] = (isUptrend ? 1 : (-1));
			Signal_Trade[0] = num12;
			if (ConditionTrendStart && Signal_Trend[0] * (double)signalTrend1 < 0.0)
			{
				signalInfo = SignalInfo.Trend;
				if (!isCustomMarkerRenderingMethod)
				{
					PrintMarker(isUptrend, signalInfo);
				}
				else
				{
					AddMarker(CurrentBar, isUptrend, signalInfo);
				}
				TriggerAlerts(isUptrend, signalInfo);
			}
			if (isCharting)
			{
				Brush brush = ((!isUptrend) ? PlotDowntrend : PlotUptrend);
				if (!BrushExtensions.IsTransparent(brush))
				{
					if (MathExtentions.ApproxCompare(Signal_Trend[0], (double)signalTrend1) == 0)
					{
						if (PlotEnabled)
						{
							BrushSeries obj = PlotBrushes[0];
							Brush brush2 = (PlotBrushes[2][0] = brush);
							obj[0] = brush2;
						}
					}
					else
					{
						BrushSeries obj2 = PlotBrushes[0];
						Brush brush2 = (PlotBrushes[2][0] = Brushes.Transparent);
						obj2[0] = brush2;
					}
					if (PlotEnabled)
					{
						PlotBrushes[1][0] = brush;
					}
				}
			}
			PaintBar(isUptrend);
			if (!isCharting || !RegionEnabled)
			{
				return;
			}
			Brush brush4 = ((!isUptrend) ? RegionDowntrend : RegionUptrend);
			if (BrushExtensions.IsTransparent(brush4))
			{
				return;
			}
			if (!((!isUptrend) ? (crossDownIndex == CurrentBar) : (crossUpIndex == CurrentBar)))
			{
				int barsAgo = CurrentBar - ((!isUptrend) ? crossDownIndex : crossUpIndex);
				if (RegionOpacity1 > 0)
				{
					FillRegion(seriesFibonacci1, seriesFibonacci2, brush4, barsAgo, 1, updateTag: true, RegionOpacity1);
				}
				if (RegionOpacity2 > 0)
				{
					FillRegion(seriesFibonacci2, TrailingStop, brush4, barsAgo, 2, updateTag: true, RegionOpacity2);
				}
			}
			else if (!isOnBarCloseMode)
			{
				brush4 = ((!isUptrend) ? RegionUptrend : RegionDowntrend);
				int barsAgo2 = CurrentBar - ((!isUptrend) ? crossUpIndex : crossDownIndex);
				if (RegionOpacity1 > 0)
				{
					FillRegion(seriesFibonacci1, seriesFibonacci2, brush4, barsAgo2, 1, updateTag: false, RegionOpacity1);
				}
				if (RegionOpacity2 > 0)
				{
					FillRegion(seriesFibonacci2, TrailingStop, brush4, barsAgo2, 2, updateTag: false, RegionOpacity2);
				}
			}
		}
		else
		{
			seriesWildMA[0] = 0.0;
			seriesTrueRange[0] = 0.0;
			Series<double> obj3 = seriesUp;
			seriesDown[0] = 0.0;
			obj3[0] = 0.0;
			isUptrend = false;
			Signal_Trend[0] = (isUptrend ? 1 : (-1));
			AddNewLine();
		}
	}

	private void FillRegion(Series<double> series1, Series<double> series2, Brush regionBrush, int barsAgo, int tagIndex, bool updateTag, int regionOpacity)
	{
		if (updateTag)
		{
			if (tagIndex == 1)
			{
				tagRegion1 = "gbPANAKanal.cloud1." + ((!isUptrend) ? crossDownIndex : crossUpIndex);
			}
			if (tagIndex == 2)
			{
				tagRegion2 = "gbPANAKanal.cloud2." + ((!isUptrend) ? crossDownIndex : crossUpIndex);
			}
		}
		Draw.Region(this, (tagIndex != 1) ? tagRegion2 : tagRegion1, barsAgo, (!updateTag) ? 1 : 0, series1, series2, Brushes.Transparent, regionBrush, regionOpacity);
	}

	private TraillingStopAndFibonacciInfo ComputeTraillingStopAndFibonacci(bool isUptrend, double extremum)
	{
		TraillingStopAndFibonacciInfo result = new TraillingStopAndFibonacciInfo
		{
			TraillingStop = ((!isUptrend) ? seriesDown[0] : seriesUp[0])
		};
		double num = result.TraillingStop - extremum;
		result.Fibonacci1 = extremum + num * swingArmFibonacciLevel1;
		result.Fibonacci2 = extremum + num * swingArmFibonacciLevel2;
		return result;
	}

	private int CheckBreakoutAndPullbackSignal(double fibonacci1)
	{
		if (!isPriceInZone)
		{
			if (!isUptrend || MathExtentions.ApproxCompare(Close[0], fibonacci1) >= 0)
			{
				if (isUptrend || MathExtentions.ApproxCompare(Close[0], fibonacci1) <= 0)
				{
					isCheckPriceInZone = false;
				}
				else
				{
					isCheckPriceInZone = true;
				}
			}
			else
			{
				isCheckPriceInZone = true;
			}
		}
		int result = 0;
		if (!hasPbSignal)
		{
			if (IsFirstTickOfBar)
			{
				pbSignalIndex++;
			}
			if (pbSignalIndex < SignalPullbackFindingPeriod)
			{
				double num = Close[0];
				double num2 = Close[1];
				double num3 = Open[0];
				double num4 = Open[1];
				int num5 = MathExtentions.ApproxCompare(num, num3);
				int num6 = MathExtentions.ApproxCompare(num2, num4);
				if (!((isUptrend ? (num6 < 0 && num5 > 0) : (num6 > 0 && num5 < 0)) & ((isUptrend && MathExtentions.ApproxCompare(Low[0], fibonacci1) <= 0) || (!isUptrend && MathExtentions.ApproxCompare(High[0], fibonacci1) >= 0))))
				{
					if (hasPbSignalOnEachTick)
					{
						if (ConditionTrendPullback)
						{
							RemoveMarker();
						}
						isResetConditionPb = false;
						hasPbSignalOnEachTick = false;
					}
				}
				else
				{
					if (hasBreakSignalOnEachTick)
					{
						if (ConditionBreak)
						{
							RemoveMarker();
						}
						if (!isUptrend)
						{
							breakDownSignalIndex = prevBreakSignalIndex;
						}
						else
						{
							breakUpSignalIndex = prevBreakSignalIndex;
						}
						hasBreakSignalOnEachTick = false;
					}
					result = ((!isUptrend) ? (-3) : 3);
					if (!hasPbSignalOnEachTick)
					{
						if (ConditionTrendPullback)
						{
							signalInfo = SignalInfo.Pullback;
							if (!isCustomMarkerRenderingMethod)
							{
								PrintMarker(isUptrend, signalInfo);
							}
							else
							{
								AddMarker(CurrentBar, isUptrend, signalInfo);
							}
							TriggerAlerts(isUptrend, signalInfo);
						}
						isResetConditionPb = true;
						hasPbSignalOnEachTick = true;
					}
				}
			}
		}
		if (!isResetConditionPb)
		{
			if (!isPriceInZone || !isBreakSignalSplitOk || !((!isUptrend) ? (signalStateKeltner - prevSignalStateKeltner < 0) : (signalStateKeltner - prevSignalStateKeltner > 0)))
			{
				isResetConditionBreak = false;
				if (hasBreakSignalOnEachTick)
				{
					if (ConditionBreak)
					{
						RemoveMarker();
					}
					if (!isUptrend)
					{
						breakDownSignalIndex = prevBreakSignalIndex;
					}
					else
					{
						breakUpSignalIndex = prevBreakSignalIndex;
					}
					hasBreakSignal = false;
					hasBreakSignalOnEachTick = false;
				}
			}
			else
			{
				result = ((!isUptrend) ? (-2) : 2);
				if (!hasBreakSignalOnEachTick)
				{
					if (!isUptrend)
					{
						prevBreakSignalIndex = breakDownSignalIndex;
						breakDownSignalIndex = CurrentBar;
					}
					else
					{
						prevBreakSignalIndex = breakUpSignalIndex;
						breakUpSignalIndex = CurrentBar;
					}
					if (ConditionBreak)
					{
						signalInfo = SignalInfo.Break;
						if (!isCustomMarkerRenderingMethod)
						{
							PrintMarker(isUptrend, signalInfo);
						}
						else
						{
							AddMarker(CurrentBar, isUptrend, signalInfo);
						}
						TriggerAlerts(isUptrend, signalInfo);
					}
					hasBreakSignal = true;
					isResetConditionBreak = true;
					hasBreakSignalOnEachTick = true;
				}
			}
			return result;
		}
		return result;
	}

	private void CheckLineBrokenAndAddNewLine(bool isTrendSwitched = false)
	{
		if (dictLineInfo.Count <= 0)
		{
			return;
		}
		LineInfo lineInfo = dictLineInfo.Values.Last();
		bool isTop = lineInfo.IsTop;
		if (lineInfo.IsBroken)
		{
			if (!isTop || isLineBrokenByCandleUp)
			{
				if (!isTop && !isLineBrokenByCandleDown)
				{
					lineInfo.IsBroken = false;
				}
			}
			else
			{
				lineInfo.IsBroken = false;
			}
		}
		if (!lineInfo.IsBroken)
		{
			if (!isTrendSwitched)
			{
				bool flag;
				if (flag = ((!isTop) ? (MathExtentions.ApproxCompare(Low[0], lineInfo.Price) <= 0) : (MathExtentions.ApproxCompare(High[0], lineInfo.Price) >= 0)))
				{
					lineInfo.IsBroken = true;
					if (!isTop)
					{
						isLineBrokenByCandleDown = true;
					}
					else
					{
						isLineBrokenByCandleUp = true;
					}
				}
				lineInfo.BarEnd = CurrentBar - (flag ? 1 : 0);
			}
			else
			{
				lineInfo.IsBroken = true;
				lineInfo.BarEnd = CurrentBar - 1;
			}
		}
		if (isTrendSwitched)
		{
			AddNewLine();
		}
	}

	private void RemoveMarker()
	{
		if (MarkerEnabled)
		{
			if (!isCustomMarkerRenderingMethod)
			{
				string text = "gbPANAKanal.marker." + CurrentBar;
				RemoveDrawObject(text);
			}
			else if (dictMarkers.ContainsKey(CurrentBar))
			{
				dictMarkers.Remove(CurrentBar);
			}
		}
	}

	private void RemovePopupAndSound()
	{
		if (!PopupEnabled && !SoundEnabled)
		{
			return;
		}
		TriggerCustomEvent((Action<object>)delegate
		{
			if (isCharting)
			{
				ChartControl.Dispatcher.InvokeAsync(delegate
				{
					if (PopupEnabled && alertWindow != null)
					{
						alertWindow.Close();
					}
					if (SoundEnabled && alertWindow != null && !alertWindow.IsVisible)
					{
						rearmTimer.Stop();
					}
				});
			}
		}, (object)null);
	}

	private void AddNewLine()
	{
		double num = 0.0;
		if (CurrentBar != 0)
		{
			num = Math.Abs(TrailingStop[1] - seriesFibonacci1[1]);
		}
		LineInfo value = new LineInfo
		{
			Price = ((!isUptrend) ? (Low[0] - num) : (High[0] + num)),
			BarStart = CurrentBar,
			BarEnd = CurrentBar,
			IsTop = isUptrend,
			IsBroken = false
		};
		if (dictLineInfo.ContainsKey(CurrentBar))
		{
			dictLineInfo[CurrentBar] = value;
		}
		else
		{
			dictLineInfo.Add(CurrentBar, value);
		}
	}

	private void ComputeKeltner()
	{
		seriesSK[0] = EMA(Input, MiddlePeriod)[0];
		double num = EMA(seriesSK, 2)[0];
		Middle[0] = Instrument.MasterInstrument.RoundToTickSize(num);
	}

	protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
	{
		if (ChartControl != null && !IsInHitTest)
		{
			DrawLines(chartScale);
			DrawMarkers(chartScale);
			base.OnRender(chartControl, chartScale);
		}
	}

	private void DrawLines(ChartScale chartScale)
	{
		if (dictLineInfo.Count <= 0)
		{
			return;
		}
		if (!LineInactiveEnabled)
		{
			LineInfo value = dictLineInfo.ElementAt(dictLineInfo.Count - 1).Value;
			if (!value.IsBroken)
			{
				DrawOneLine(chartScale, value);
			}
		}
		else
		{
			for (int num = dictLineInfo.Count - 1; num >= 0; num--)
			{
				DrawOneLine(chartScale, dictLineInfo.ElementAt(num).Value);
			}
		}
	}

	private void DrawOneLine(ChartScale chartScale, LineInfo lineInfo)
	{
		if (lineInfo == null)
		{
			return;
		}
		int barStart = lineInfo.BarStart;
		bool isTop = lineInfo.IsTop;
		bool isBroken = lineInfo.IsBroken;
		int barEnd = lineInfo.BarEnd;
		int fromIndex = ChartBars.FromIndex;
		int toIndex = ChartBars.ToIndex;
		if (barStart >= barEnd || barStart > toIndex || barEnd < fromIndex)
		{
			return;
		}
		double price = lineInfo.Price;
		if (!isTop)
		{
			if (MathExtentions.ApproxCompare(price, chartScale.MinValue) <= 0)
			{
				return;
			}
		}
		else if (MathExtentions.ApproxCompare(price, chartScale.MaxValue) >= 0)
		{
			return;
		}
		Stroke val = (isBroken ? ((!isTop) ? LineInactiveBottom : LineInactiveTop) : ((!isTop) ? LineActiveBottom : LineActiveTop));
		Brush brush = val.Brush;
		if (!BrushExtensions.IsTransparent(brush))
		{
			float width = val.Width;
			SharpDX.Direct2D1.StrokeStyle strokeStyle = val.StrokeStyle;
			float num = ChartControl.GetXByBarIndex(ChartBars, barStart);
			float num2 = ChartControl.GetXByBarIndex(ChartBars, barEnd);
			float num3 = chartScale.GetYByValue(price);
			Vector2 val2 = new Vector2(num, num3);
			Vector2 val3 = new Vector2(num2, num3);
			RenderTarget.AntialiasMode = (SharpDX.Direct2D1.AntialiasMode)0;
			RenderTarget.DrawLine(val2, val3, DxExtensions.ToDxBrush(brush, RenderTarget), width, strokeStyle);
		}
	}

	private void AddMarker(int barIndex, bool isBullish, SignalInfo signalInfo)
	{
		if (MarkerEnabled)
		{
			MarkerInfo value = new MarkerInfo
			{
				BarIndex = barIndex,
				IsBullish = isBullish,
				SignalInfo = signalInfo
			};
			if (dictMarkers.ContainsKey(barIndex))
			{
				dictMarkers[barIndex] = value;
			}
			else
			{
				dictMarkers.Add(barIndex, value);
			}
		}
	}

	private void DrawMarkers(ChartScale chartScale)
	{
		if (!MarkerEnabled || !isCustomMarkerRenderingMethod || dictMarkers == null || dictMarkers.Count <= 0)
		{
			return;
		}
		for (int i = ChartBars.FromIndex; i <= Math.Min(CurrentBar, ChartBars.ToIndex); i++)
		{
			if (dictMarkers.ContainsKey(i))
			{
				DrawOneMarker(chartScale, dictMarkers[i]);
			}
		}
	}

	private void DrawOneMarker(ChartScale chartScale, MarkerInfo markerInfo)
	{
		bool isBullish = markerInfo.IsBullish;
		SignalInfo signalInfo = markerInfo.SignalInfo;
		int barIndex = markerInfo.BarIndex;
		if ((!isBullish && MathExtentions.ApproxCompare(High.GetValueAt(barIndex), chartScale.MaxValue) >= 0) || (isBullish && MathExtentions.ApproxCompare(Low.GetValueAt(barIndex), chartScale.MinValue) <= 0))
		{
			return;
		}
		Brush brush = ((!isBullish) ? MarkerBrushBearish : MarkerBrushBullish);
		if (BrushExtensions.IsTransparent(brush))
		{
			return;
		}
		string text = ((signalInfo == SignalInfo.Trend) ? ((!isBullish) ? MarkerStringDowntrendStart : MarkerStringUptrendStart) : ((signalInfo == SignalInfo.Break) ? ((!isBullish) ? MarkerStringBreakDown : MarkerStringBreakUp) : ((!isBullish) ? MarkerStringDowntrendPullback : MarkerStringUptrendPullback)));
		if (!string.IsNullOrWhiteSpace(text))
		{
			text = text.Replace(" + ", "\n");
			int num = (isBullish ? 1 : (-1));
			float num2 = ChartControl.GetXByBarIndex(ChartBars, barIndex);
			double num3;
			if (signalInfo != SignalInfo.Trend)
			{
				num3 = chartScale.GetYByValue(((!isBullish) ? High : Low).GetValueAt(barIndex)) + num * MarkerOffset;
			}
			else
			{
				double num4 = Math.Min(Low.GetValueAt(barIndex), TrailingStop.GetValueAt(barIndex));
				double num5 = Math.Max(High.GetValueAt(barIndex), TrailingStop.GetValueAt(barIndex));
				num3 = chartScale.GetYByValue((!isBullish) ? num5 : num4) + num * MarkerOffset;
			}
			using (var textFormat = MarkerFont.ToDirectWriteTextFormat())
			{
				textFormat.TextAlignment = SharpDX.DirectWrite.TextAlignment.Center;
				using (var textLayout = new SharpDX.DirectWrite.TextLayout(NinjaTrader.Core.Globals.DirectWriteFactory, text, textFormat, 200, 200))
				{
					float textHeight = textLayout.Metrics.Height;
					float x = num2 - 100;
					float y = (num < 0) ? (float)num3 - textHeight : (float)num3;
					using (var dxBrush = DxExtensions.ToDxBrush(brush, RenderTarget))
					{
						RenderTarget.DrawTextLayout(new SharpDX.Vector2(x, y), textLayout, dxBrush);
					}
				}
			}
		}
	}

	private void PrintMarker(bool isBullish, SignalInfo signalInfo)
	{
		if (isCharting && MarkerEnabled && CurrentBar >= BarsRequiredToPlot)
		{
			string tag = "gbPANAKanal.marker." + CurrentBar;
			Brush textBrush = ((!isBullish) ? MarkerBrushBearish : MarkerBrushBullish);
			double y = ((signalInfo != SignalInfo.Trend) ? ((!isBullish) ? High[0] : Low[0]) : ((!isBullish) ? Math.Max(High[0], TrailingStop[0]) : Math.Min(Low[0], TrailingStop[0])));
			string text = ((signalInfo == SignalInfo.Trend) ? ((!isBullish) ? MarkerStringDowntrendStart : MarkerStringUptrendStart) : ((signalInfo == SignalInfo.Break) ? ((!isBullish) ? MarkerStringBreakDown : MarkerStringBreakUp) : ((!isBullish) ? MarkerStringDowntrendPullback : MarkerStringUptrendPullback)));
			text = text.Replace(" + ", "\n");
			int num = (int)(MarkerFont.Size * 1.4 * text.Split('\n').Length);
			int yPixelOffset = ((!isBullish) ? 1 : (-1)) * (MarkerOffset + num / 2);
			Draw.Text(this, tag, IsAutoScale, text, 0, y, yPixelOffset, textBrush, MarkerFont, System.Windows.TextAlignment.Center, Brushes.Transparent, Brushes.Transparent, 0);
		}
	}

	private int GetStateKeltner(int prevState, double middleBandValue)
	{
		int num = MathExtentions.ApproxCompare(Close[0], middleBandValue);
		if (num <= 0)
		{
			if (num != 0)
			{
				return -1;
			}
			if (prevState == 1)
			{
				return 1;
			}
			return -1;
		}
		return 1;
	}

	private void PaintBar(bool isUptrend, bool isToggleClickEvent = false, int barIndex = 0)
	{
		if (!isCharting || !BarEnabled)
		{
			return;
		}
		Brush brush = ((!isUptrend) ? BarDowntrend : BarUptrend);
		int num = ((!isToggleClickEvent) ? MathExtentions.ApproxCompare(Close[0], Open[0]) : MathExtentions.ApproxCompare(Close.GetValueAt(barIndex), Open.GetValueAt(barIndex)));
		int num2 = (isUptrend ? 1 : (-1));
		int num3 = CurrentBar - barIndex;
		if (BarOutlineEnabled && !BrushExtensions.IsTransparent(brush))
		{
			if (!isToggleClickEvent)
			{
				CandleOutlineBrush = brush;
			}
			else
			{
				CandleOutlineBrushes[num3] = brush;
			}
		}
		if (!BarBiasBased)
		{
			if (num != 0)
			{
				if (!isToggleClickEvent)
				{
					BarBrush = brush;
				}
				else
				{
					BarBrushes[num3] = brush;
				}
			}
		}
		else if (!BrushExtensions.IsTransparent(brush))
		{
			if (num != 0)
			{
				if (!isToggleClickEvent)
				{
					BarBrush = ((num2 * num <= 0) ? Brushes.Transparent : brush);
				}
				else
				{
					BarBrushes[num3] = ((num2 * num <= 0) ? Brushes.Transparent : brush);
				}
			}
		}
		else if (num2 * num < 0)
		{
			if (!isToggleClickEvent)
			{
				BarBrush = Brushes.Transparent;
			}
			else
			{
				BarBrushes[num3] = Brushes.Transparent;
			}
		}
	}

	public override string FormatPriceMarker(double price)
	{
		return Instrument.MasterInstrument.FormatPrice(Instrument.MasterInstrument.RoundToTickSize(price), true);
	}

	private void TriggerAlerts(bool isUptrend, SignalInfo signalInfo)
	{
		if (State == State.Historical || (!PopupEnabled && !SoundEnabled && !EmailEnabled))
		{
			return;
		}
		DateTime now = DateTime.Now;
		if (now < nextAlert)
		{
			return;
		}
		string text = now.ToString("HH:mm");
		string text2 = now.ToString("HH:mm:ss, dd MMM yyyy");
		nextAlert = now + TimeSpan.FromSeconds(AlertBlockingSeconds);
		string text3 = $"{Instrument.FullName} ({BarsPeriod})";
		bool flag = signalInfo == SignalInfo.Trend;
		bool flag2 = signalInfo == SignalInfo.Break;
		string arg = (flag ? "TREND-START" : (flag2 ? ((!isUptrend) ? "BREAK-DOWN" : ": BREAK-UP") : "TREND-PULLBACK"));
		string text4 = string.Format("{0}: {1} alert on {2} at ", "PANA Kanal", arg, text3) + text;
		string popupMessage;
		if (!flag)
		{
			if (!flag2)
			{
				popupMessage = string.Format("The {0} has had a PULLBACK.", (!isUptrend) ? "downtrend" : "uptrend");
			}
			else
			{
				popupMessage = string.Format("The market has broken {0} the {1}.", (!isUptrend) ? "BELOW" : "ABOVE", (!isUptrend) ? "Lower" : "Upper");
			}
		}
		else
		{
			popupMessage = string.Format("The market has entered {0}.", (!isUptrend) ? "a DOWNTREND" : "an UPTREND");
		}
		popupMessage = $"{popupMessage}\n\nAlert chart: {text3}\nAlert time: {text2}";
		string text5 = "\n_______________________\n\n";
		string text6 = popupMessage + text5 + "PANA Kanal by GreyBeard\nWebsite: http://greybeard.com";
		if (PopupEnabled && isCharting)
		{
			ChartControl.Dispatcher.InvokeAsync(delegate
			{
				if (alertWindow != null)
					alertWindow.Close();
				alertWindow = new Window
				{
					Title = "PANA Kanal by GreyBeard",
					Width = 400, Height = 250,
					WindowStartupLocation = WindowStartupLocation.CenterScreen,
					Background = PopupBackgroundBrush,
					Opacity = PopupBackgroundOpacity / 100.0,
					Topmost = true,
					Content = new TextBlock
					{
						Text = popupMessage,
						Foreground = PopupTextBrush,
						FontSize = PopupTextSize,
						TextWrapping = TextWrapping.Wrap,
						Margin = new Thickness(15)
					}
				};
				alertWindow.Show();
			});
		}
		if (SoundEnabled)
		{
			string text7 = "alert @ " + text2;
			soundPath = string.Concat(str2: flag ? ((!isUptrend) ? SoundDowntrendStart : SoundUptrendStart) : (flag2 ? ((!isUptrend) ? SoundBreakDown : SoundBreakUp) : ((!isUptrend) ? SoundDowntrendPullback : SoundUptrendPullback)), str0: Globals.InstallDir, str1: "sounds\\");
			Alert(text7, (Priority)0, text4, soundPath, 0, Brushes.Red, Brushes.Yellow);
			if (SoundRearmEnabled && PopupEnabled && isCharting)
			{
				nextRearm = now + TimeSpan.FromSeconds(SoundRearmSeconds);
				rearmTimer.Start();
			}
		}
		if (EmailEnabled && EmailReceiver != "receiver@example.com")
		{
			SendMail(EmailReceiver, text4, text6);
		}
	}

	private void OnRearmTimerTick(object sender, EventArgs e)
	{
		TriggerCustomEvent((Action<object>)delegate
		{
			if (isCharting)
			{
				ChartControl.Dispatcher.InvokeAsync(delegate
				{
					if (alertWindow != null && !alertWindow.IsVisible)
					{
						rearmTimer.Stop();
					}
					else
					{
						if (DateTime.Now >= nextRearm)
						{
							nextRearm = DateTime.Now + TimeSpan.FromSeconds(SoundRearmSeconds);
							PlaySound(soundPath);
						}
					}
				});
			}
		}, (object)e);
	}


}


public enum gbPANAKanal_MarkerRenderingMethod
{
	Custom,
	Builtin
}

public class gbPANAKanal_SoundConverter : TypeConverter
{
	public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
	{
		if (context != null)
		{
			List<string> list = new List<string>();
			FileInfo[] files = new DirectoryInfo(Globals.InstallDir + "sounds").GetFiles("*.wav");
			foreach (FileInfo fileInfo in files)
			{
				list.Add(fileInfo.Name);
			}
			return new StandardValuesCollection(list);
		}
		return null;
	}

	public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
	{
		return true;
	}
}

// ==== gbThunderZilla ============================================
[CategoryOrder("General", 1000010)]
[CategoryOrder("Special", 1000060)]
[CategoryOrder("Graphics", 1000020)]
[CategoryOrder("Gradient", 1000030)]
[CategoryOrder("Alerts", 1000040)]
[CategoryOrder("Developer", 0)]
[CategoryOrder("Critical", 1000070)]
public class gbThunderZilla : Indicator
{
	private enum SignalTradeInfo
	{
		NoSignal = 0,
		UptrendStart = 1,
		DowntrendStart = -1,
		DowntrendSlowdown = 2,
		UptrendSlowdown = -2,
		UptrendPullback = 3,
		DowntrendPullback = -3,
		MoveStopUp = 4,
		MoveStopDown = -4
	}

	private struct MarkerInfo
	{
		public int BarIndex { get; set; }

		public bool IsBullish { get; set; }

		public SignalInfo SignalInfo { get; set; }

		}

	private class CountInfo
	{
		public byte CountSW { get; set; }

		public byte CountSM { get; set; }

		public byte Total => (byte)(CountSW + CountSM);

		}

	private enum SignalInfo
	{
		Trend,
		Slowdown,
		Pullback,
		MoveStop
	}

	private const double offsetMultiplierTrend = 30.0;

	private const gbThunderZillaMAType sumoFastMA1Type = gbThunderZillaMAType.EMA;

	private const int sumoFastMA1Period = 14;

	private const bool sumoFastMA1SmoothingEnabled = false;

	private const gbThunderZillaMAType sumoFastMA1SmoothingMethod = gbThunderZillaMAType.SMA;

	private const int sumoFastMA1SmoothingPeriod = 5;

	private const gbThunderZillaMAType sumoFastMA2Type = gbThunderZillaMAType.EMA;

	private const int sumoFastMA2Period = 30;

	private const bool sumoFastMA2SmoothingEnabled = false;

	private const gbThunderZillaMAType sumoFastMA2SmoothingMethod = gbThunderZillaMAType.SMA;

	private const int sumoFastMA2SmoothingPeriod = 10;

	private const gbThunderZillaMAType sumoFastMA3Type = gbThunderZillaMAType.EMA;

	private const int sumoFastMA3Period = 45;

	private const bool sumoFastMA3SmoothingEnabled = false;

	private const gbThunderZillaMAType sumoFastMA3SmoothingMethod = gbThunderZillaMAType.SMA;

	private const int sumoFastMA3SmoothingPeriod = 15;

	private const int sumoSignalSplitFirst = 15;

	private const int sumoSignalSplitSecond = 30;

	private const int oBOSMFIPeriod = 14;

	private const int oBOSMFIThresholdHigh = 70;

	private const int oBOSMFIThresholdLow = 30;

	private const int oBOSRSIPeriod = 14;

	private const int oBOSRSISmooth = 3;

	private const int oBOSRSIThresholdHigh = 70;

	private const int oBOSRSIThresholdLow = 30;

	private const int oBOSStochPeriodD = 7;

	private const int oBOSStochPeriodK = 14;

	private const gbThunderZillaMAType oBOSStochSmoothingMethod = gbThunderZillaMAType.SMA;

	private const int oBOSStochSmoothingPeriod = 3;

	private const int oBOSStochThresholdHigh = 70;

	private const int oBOSStochThresholdLow = 30;

	private const int oBOSSafeReversalPeriod = 3;

	private const string toolTipSpace = "  ";

	private string tag;

	private Dictionary<int, MarkerInfo> dictMarkers;

	private Series<double> seriesSWTrendVector;

	private Series<double> seriesSumoMax;

	private Series<double> seriesSumoMin;

	private Series<double> seriesSumoFair;

	private Series<int> seriesOBOSSignalState;

	private Series<double> seriesMinMax;

	private Brush cloudMixBrush;

	private bool swIsUptrend;

	private bool sumoIsUptrend;

	private SignalTradeInfo trendState;

	private Window alertWindow;

	private const string prefix = "gbThunderZilla";

	private const string indicatorName = "ThunderZilla";

	private bool isCharting;

	private bool isCustomRenderingMethod;

	private CountInfo countInfo;

	private bool isTrendOrSwTrendChanged;

	private int sumoSignalTrade;

	private int obosSignalTrade;

	private int crossIndex;

	private double countSignalPerFlat;

	private double countSignalPerTrend;

	private int sumoNextBar = -1;

	private int sumoCountSignalBars;

	private int obosOverlapExitBarIndex = -1;

	private int obosLastSignalState = -1;

	private DateTime nextAlert = DateTime.MinValue;

	private DateTime nextRearm = DateTime.MinValue;

	private string soundPath = string.Empty;

	private DispatcherTimer rearmTimer;

	[Display(Name = "Condition: Trend Start", Order = 0, GroupName = "Alerts")]
	public bool ConditionTrendStart { get; set; }

	[Display(Name = "Condition: Trend Slowdown", Order = 1, GroupName = "Alerts")]
	public bool ConditionSlowdown { get; set; }

	[Display(Name = "Condition: Trend Pullback", Order = 2, GroupName = "Alerts")]
	public bool ConditionTrendPullback { get; set; }

	[Display(Name = "Condition: Move Stop", Order = 3, GroupName = "Alerts")]
	public bool ConditionMoveStop { get; set; }

	[Display(Name = "Popup: Enabled", Order = 10, GroupName = "Alerts")]
	public bool PopupEnabled { get; set; }

	[XmlIgnore]
	[Display(Name = "Popup: Background Color", Order = 11, GroupName = "Alerts")]
	public Brush PopupBackgroundBrush { get; set; }

	[Browsable(false)]
	public string PopupBackgroundBrush_Serialize
	{
		get
		{
			return Serialize.BrushToString(PopupBackgroundBrush);
		}
		set
		{
			PopupBackgroundBrush = Serialize.StringToBrush(value);
		}
	}
	[Display(Name = "Popup: Background Opacity", Order = 12, GroupName = "Alerts")]
	public int PopupBackgroundOpacity { get; set; }

	[Display(Name = "Popup: Text Color", Order = 13, GroupName = "Alerts")]
	[XmlIgnore]
	public Brush PopupTextBrush { get; set; }

	[Browsable(false)]
	public string PopupTextBrush_Serialize
	{
		get
		{
			return Serialize.BrushToString(PopupTextBrush);
		}
		set
		{
			PopupTextBrush = Serialize.StringToBrush(value);
		}
	}
	[Display(Name = "Popup: Text Size", Order = 14, GroupName = "Alerts")]
	public int PopupTextSize { get; set; }

	[XmlIgnore]
	[Display(Name = "Popup: Button Color", Order = 15, GroupName = "Alerts")]
	public Brush PopupButtonBrush { get; set; }

	[Browsable(false)]
	public string PopupButtonBrush_Serialize
	{
		get
		{
			return Serialize.BrushToString(PopupButtonBrush);
		}
		set
		{
			PopupButtonBrush = Serialize.StringToBrush(value);
		}
	}

	[Display(Name = "Sound: Enabled", Order = 20, GroupName = "Alerts")]
	public bool SoundEnabled { get; set; }

	[TypeConverter(typeof(gbThunderZilla_SoundConverter))]
	[Display(Name = "Sound: Uptrend Start", Order = 21, GroupName = "Alerts")]
	public string SoundUptrendStart { get; set; }

	[Display(Name = "Sound: Uptrend Slowdown", Order = 22, GroupName = "Alerts")]
	[TypeConverter(typeof(gbThunderZilla_SoundConverter))]
	public string SoundUptrendSlowdown { get; set; }

	[TypeConverter(typeof(gbThunderZilla_SoundConverter))]
	[Display(Name = "Sound: Uptrend Pullback", Order = 23, GroupName = "Alerts")]
	public string SoundUptrendPullback { get; set; }

	[Display(Name = "Sound: Downtrend Start", Order = 24, GroupName = "Alerts")]
	[TypeConverter(typeof(gbThunderZilla_SoundConverter))]
	public string SoundDowntrendStart { get; set; }

	[Display(Name = "Sound: Downtrend Slowdown", Order = 25, GroupName = "Alerts")]
	[TypeConverter(typeof(gbThunderZilla_SoundConverter))]
	public string SoundDowntrendSlowdown { get; set; }

	[Display(Name = "Sound: Dowtrend Pullback", Order = 26, GroupName = "Alerts")]
	[TypeConverter(typeof(gbThunderZilla_SoundConverter))]
	public string SoundDowntrendPullback { get; set; }

	[Display(Name = "Sound: Move Stop Up", Order = 27, GroupName = "Alerts")]
	[TypeConverter(typeof(gbThunderZilla_SoundConverter))]
	public string SoundMoveStopUp { get; set; }

	[Display(Name = "Sound: Move Stop Down", Order = 28, GroupName = "Alerts")]
	[TypeConverter(typeof(gbThunderZilla_SoundConverter))]
	public string SoundMoveStopDown { get; set; }

	[Display(Name = "Sound: Rearm Enabled", Order = 29, GroupName = "Alerts")]
	public bool SoundRearmEnabled { get; set; }

	[Display(Name = "Sound: Rearm Seconds ", Order = 30, GroupName = "Alerts")]
	public int SoundRearmSeconds { get; set; }

	[Display(Name = "Email: Enabled", Order = 31, GroupName = "Alerts")]
	public bool EmailEnabled { get; set; }

	[Display(Name = "Email: Receiver", Order = 32, GroupName = "Alerts")]
	public string EmailReceiver { get; set; }

	[Display(Name = "Marker: Enabled", Order = 40, GroupName = "Alerts")]
	public bool MarkerEnabled { get; set; }

	[Display(Name = "Marker: Rendering Method", Order = 41, GroupName = "Alerts", Description = "\"Custom\" rendering provides a better appearance, and is therefore recommended in most cases.\nIf your computer is weak or slow, please switch to \"Builtin\" rendering.")]
	public gbThunderZilla_RenderingMethod MarkerRenderingMethod { get; set; }

	[Display(Name = "Marker: Color Bullish", Order = 42, GroupName = "Alerts")]
	[XmlIgnore]
	public Brush MarkerBrushBullish { get; set; }

	[Browsable(false)]
	public string MarkerBrushBullish_Serialize
	{
		get
		{
			return Serialize.BrushToString(MarkerBrushBullish);
		}
		set
		{
			MarkerBrushBullish = Serialize.StringToBrush(value);
		}
	}

	[Display(Name = "Marker: Color Bearish", Order = 43, GroupName = "Alerts")]
	[XmlIgnore]
	public Brush MarkerBrushBearish { get; set; }

	[Browsable(false)]
	public string MarkerBrushBearish_Serialize
	{
		get
		{
			return Serialize.BrushToString(MarkerBrushBearish);
		}
		set
		{
			MarkerBrushBearish = Serialize.StringToBrush(value);
		}
	}

	[Display(Name = "Marker: String Uptrend Start", Order = 44, GroupName = "Alerts")]
	public string MarkerStringUptrendStart { get; set; }

	[Display(Name = "Marker: String Uptrend Slowdown", Order = 45, GroupName = "Alerts")]
	public string MarkerStringUptrendSlowdown { get; set; }

	[Display(Name = "Marker: String Uptrend Pullback", Order = 46, GroupName = "Alerts")]
	public string MarkerStringUptrendPullback { get; set; }

	[Display(Name = "Marker: String Downtrend Start", Order = 47, GroupName = "Alerts")]
	public string MarkerStringDowntrendStart { get; set; }

	[Display(Name = "Marker: String Downtrend Slowdown", Order = 48, GroupName = "Alerts")]
	public string MarkerStringDowntrendSlowndown { get; set; }

	[Display(Name = "Marker: String Downtrend Pullback", Order = 49, GroupName = "Alerts")]
	public string MarkerStringDowntrendPullback { get; set; }

	[Display(Name = "Marker: String Move Stop Up", Order = 50, GroupName = "Alerts")]
	public string MarkerStringMoveStopUp { get; set; }

	[Display(Name = "Marker: String Move Stop Down", Order = 51, GroupName = "Alerts")]
	public string MarkerStringMoveStopDown { get; set; }

	[Display(Name = "Marker: Font", Order = 60, GroupName = "Alerts")]
	public SimpleFont MarkerFont { get; set; }

	[Display(Name = "Marker: Offset", Order = 61, GroupName = "Alerts")]
	public int MarkerOffset { get; set; }

	[Display(Name = "Alert Blocking (Seconds)", Order = 70, GroupName = "Alerts", Description = "The minimum interval between 2 consecutive alerts")]
	public int AlertBlockingSeconds { get; set; }

	[Display(Name = "Website", Order = 0, GroupName = "Developer")]
	public string Website => "greybeard.com";

	[Display(Name = "Update", Order = 10, GroupName = "Developer")]
	public string Update => "11 Aug 2025";

	[Display(Name = "Screen DPI", Order = 100, GroupName = "General")]
	public int ScreenDPI { get; set; }

	[Display(Name = "Plot Trend: Enabled", Order = 0, GroupName = "Graphics")]
	public bool PlotSlowEnabled { get; set; }

	[XmlIgnore]
	[Display(Name = "Plot Trend: Uptrend", Order = 1, GroupName = "Graphics")]
	public Brush PlotTrendUptrend { get; set; }

	[Browsable(false)]
	public string PlotTrendUptrend_Serialize
	{
		get
		{
			return Serialize.BrushToString(PlotTrendUptrend);
		}
		set
		{
			PlotTrendUptrend = Serialize.StringToBrush(value);
		}
	}

	[Display(Name = "Plot Trend: Downtrend", Order = 2, GroupName = "Graphics")]
	[XmlIgnore]
	public Brush PlotTrendDowntrend { get; set; }

	[Browsable(false)]
	public string PlotTrendDowntrend_Serialize
	{
		get
		{
			return Serialize.BrushToString(PlotTrendDowntrend);
		}
		set
		{
			PlotTrendDowntrend = Serialize.StringToBrush(value);
		}
	}

	[Display(Name = "Plot Trend: Neutral", Order = 3, GroupName = "Graphics")]
	[XmlIgnore]
	public Brush PlotTrendNeutral { get; set; }

	[Browsable(false)]
	public string PlotTrendNeutral_Serialize
	{
		get
		{
			return Serialize.BrushToString(PlotTrendNeutral);
		}
		set
		{
			PlotTrendNeutral = Serialize.StringToBrush(value);
		}
	}

	[Display(Name = "Plot Stop: Enabled", Order = 10, GroupName = "Graphics")]
	public bool PlotStopEnabled { get; set; }

	[XmlIgnore]
	[Display(Name = "Plot Stop: Uptrend", Order = 11, GroupName = "Graphics")]
	public Brush PlotStopUptrend { get; set; }

	[Browsable(false)]
	public string PlotStopUptrend_Serialize
	{
		get
		{
			return Serialize.BrushToString(PlotStopUptrend);
		}
		set
		{
			PlotStopUptrend = Serialize.StringToBrush(value);
		}
	}

	[Display(Name = "Plot Stop: Downtrend", Order = 12, GroupName = "Graphics")]
	[XmlIgnore]
	public Brush PlotStopDowntrend { get; set; }

	[Browsable(false)]
	public string PlotStopDowntrend_Serialize
	{
		get
		{
			return Serialize.BrushToString(PlotStopDowntrend);
		}
		set
		{
			PlotStopDowntrend = Serialize.StringToBrush(value);
		}
	}

	[Display(Name = "Bar: Enabled", Order = 20, GroupName = "Graphics")]
	public bool BarEnabled { get; set; }

	[XmlIgnore]
	[Display(Name = "Bar: Uptrend", Order = 21, GroupName = "Graphics")]
	public Brush BarUptrend { get; set; }

	[Browsable(false)]
	public string BarUptrend_Serialize
	{
		get
		{
			return Serialize.BrushToString(BarUptrend);
		}
		set
		{
			BarUptrend = Serialize.StringToBrush(value);
		}
	}

	[XmlIgnore]
	[Display(Name = "Bar: Downtrend", Order = 22, GroupName = "Graphics")]
	public Brush BarDowntrend { get; set; }

	[Browsable(false)]
	public string BarDowntrend_Serialize
	{
		get
		{
			return Serialize.BrushToString(BarDowntrend);
		}
		set
		{
			BarDowntrend = Serialize.StringToBrush(value);
		}
	}

	[XmlIgnore]
	[Display(Name = "Bar: Neutral", Order = 23, GroupName = "Graphics")]
	public Brush BarNeutral { get; set; }

	[Browsable(false)]
	public string BarNoTrend_Serialize
	{
		get
		{
			return Serialize.BrushToString(BarNeutral);
		}
		set
		{
			BarNeutral = Serialize.StringToBrush(value);
		}
	}

	[Display(Name = "Bar: Outline Enabled", Order = 70, GroupName = "Graphics")]
	public bool BarOutlineEnabled { get; set; }

	[Display(Name = "Bar: Bias Based", Order = 71, GroupName = "Graphics")]
	public bool BarBiasBased { get; set; }

	[Display(Name = "Cloud: Enabled", Order = 90, GroupName = "Graphics")]
	public bool CloudEnabled { get; set; }

	[XmlIgnore]
	[Display(Name = "Cloud: Uptrend", Order = 91, GroupName = "Graphics")]
	public Brush CloudUptrend { get; set; }

	[Browsable(false)]
	public string CloudUptrend_Serialize
	{
		get
		{
			return Serialize.BrushToString(CloudUptrend);
		}
		set
		{
			CloudUptrend = Serialize.StringToBrush(value);
		}
	}

	[XmlIgnore]
	[Display(Name = "Cloud: Downtrend", Order = 92, GroupName = "Graphics")]
	public Brush CloudDowntrend { get; set; }

	[Browsable(false)]
	public string CloudDowntrend_Serialize
	{
		get
		{
			return Serialize.BrushToString(CloudDowntrend);
		}
		set
		{
			CloudDowntrend = Serialize.StringToBrush(value);
		}
	}

	[XmlIgnore]
	[Display(Name = "Cloud: Neutral", Order = 93, GroupName = "Graphics")]
	public Brush CloudNeutral { get; set; }

	[Browsable(false)]
	public string CloudNeutral_Serialize
	{
		get
		{
			return Serialize.BrushToString(CloudNeutral);
		}
		set
		{
			CloudNeutral = Serialize.StringToBrush(value);
		}
	}
	[Display(Name = "Cloud: Opacity", Order = 94, GroupName = "Graphics")]
	public int CloudOpacity { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "Trend: MA Type", Order = 0, GroupName = "Parameters")]
	public gbThunderZillaMAType TrendMAType { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "Trend: Period", Order = 1, GroupName = "Parameters")]
	public int TrendPeriod { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "Trend: Smoothing Enabled", Order = 2, GroupName = "Parameters")]
	public bool TrendSmoothingEnabled { get; set; }

	[Display(Name = "Trend: Smoothing Method", Order = 3, GroupName = "Parameters")]
	[NinjaScriptProperty]
	public gbThunderZillaMAType TrendSmoothingMethod { get; set; }
	[Display(Name = "Trend: Smoothing Period", Order = 4, GroupName = "Parameters")]
	[NinjaScriptProperty]
	public int TrendSmoothingPeriod { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "Stop: Offset Multiplier (Ticks)", Order = 10, GroupName = "Parameters")]
	public double StopOffsetMultiplierStop { get; set; }

	[Display(Name = "Signal: Quantity Per Flat", Order = 20, GroupName = "Parameters")]
	[NinjaScriptProperty]
	public int SignalQuantityPerFlat { get; set; }

	[Display(Name = "Signal: Quantity Per Trend", Order = 22, GroupName = "Parameters")]
	[NinjaScriptProperty]
	public int SignalQuantityPerTrend { get; set; }

	[Display(Name = "Z Order", Order = 0, GroupName = "Special")]
	public int IndicatorZOrder { get; set; }

	[Display(Name = "User Note", Order = 10, GroupName = "Special")]
	public string UserNote { get; set; }

	[Browsable(false)]
	[XmlIgnore]
	public Series<double> Trend => Values[0];

	[XmlIgnore]
	[Browsable(false)]
	public Series<double> Stop => Values[1];

	[XmlIgnore]
	[Browsable(false)]
	public Series<double> Signal_Trend => Values[2];

	[Browsable(false)]
	[XmlIgnore]
	public Series<double> Signal_Trade => Values[3];

	public override string DisplayName
	{
		get
		{
			if (!(Parent is MarketAnalyzerColumnBase))
			{
				return "ThunderZilla by GreyBeard" + GetUserNote();
			}
			return base.DisplayName;
		}
	}

	private string GetUserNote()
	{
		string text = UserNote.Trim();
		if (!string.IsNullOrWhiteSpace(text))
		{
			text = text.ToLower();
			string text2 = "instrument";
			string text3 = "period";
			if (text.Contains(text2) && Instruments[0] != null)
			{
				text = text.Replace(text2, Instruments[0].FullName);
			}
			if (text.Contains(text3) && BarsPeriods[0] != null)
			{
				text = text.Replace(text3, ((object)BarsPeriods[0]).ToString());
			}
			return " (" + text + ")";
		}
		return string.Empty;
	}

	private double GetMA(ISeries<double> input, gbThunderZillaMAType maType, int period)
	{
		switch (maType)
		{
			case gbThunderZillaMAType.EMA: return EMA(input, period)[0];
			case gbThunderZillaMAType.SMA: return SMA(input, period)[0];
			case gbThunderZillaMAType.WMA: return WMA(input, period)[0];
			case gbThunderZillaMAType.HMA: return HMA(input, period)[0];
			case gbThunderZillaMAType.DEMA: return DEMA(input, period)[0];
			case gbThunderZillaMAType.TEMA: return TEMA(input, period)[0];
			case gbThunderZillaMAType.TMA: return TMA(input, period)[0];
			case gbThunderZillaMAType.LinReg: return LinReg(input, period)[0];
			case gbThunderZillaMAType.VWMA: return VWMA(input, period)[0];
			case gbThunderZillaMAType.WilderMA: return EMA(input, 2 * period - 1)[0];
			case gbThunderZillaMAType.ZLEMA: return ZLEMA(input, period)[0];
			default: return SMA(input, period)[0];
		}
	}

	private double GetMASmoothed(ISeries<double> input, gbThunderZillaMAType maType, int period, bool smoothEnabled, gbThunderZillaMAType smoothMethod, int smoothPeriod)
	{
		double val = GetMA(input, maType, period);
		return val;
	}

	private static Brush CreateAverageBrush(Brush a, Brush b)
	{
		var ca = ((SolidColorBrush)a).Color;
		var cb = ((SolidColorBrush)b).Color;
		var avg = System.Windows.Media.Color.FromArgb(255, (byte)((ca.R + cb.R) / 2), (byte)((ca.G + cb.G) / 2), (byte)((ca.B + cb.B) / 2));
		var brush = new SolidColorBrush(avg);
		brush.Freeze();
		return brush;
	}

	private string FormatMarkerString(string text)
	{
		return text.Replace(" + ", "\n");
	}

	private int ComputeTextHeight(string text, SimpleFont font)
	{
		int lines = 1;
		foreach (char c in text)
			if (c == '\n') lines++;
		return (int)(font.Size * 1.4 * lines);
	}

	private void DrawTextOnChart(string text, SimpleFont font, float x, float y, int xOffset, int direction, Brush wpfBrush, int dpi, SharpDX.Direct2D1.RenderTarget rt)
	{
		if (rt == null || string.IsNullOrEmpty(text)) return;

		using (var dxBrush = wpfBrush.ToDxBrush(rt))
		{
			using (var format = font.ToDirectWriteTextFormat())
			{
				format.TextAlignment = SharpDX.DirectWrite.TextAlignment.Center;
				using (var layout = new SharpDX.DirectWrite.TextLayout(NinjaTrader.Core.Globals.DirectWriteFactory, text, format, 200f, 200f))
				{
					float textH = layout.Metrics.Height;
					float drawX = x - 100f + xOffset;
					// direction > 0 = bullish (below bar): text starts at anchor going down
					// direction < 0 = bearish (above bar): text ends at anchor
					float drawY = (direction < 0) ? y - textH : y;

					rt.DrawTextLayout(new SharpDX.Vector2(drawX, drawY), layout, dxBrush);
				}
			}
		}
	}

	protected override void OnStateChange()
	{
		switch (State)
		{
			case State.SetDefaults:
				Description = string.Empty;
				Name = "gbThunderZilla";
				Calculate = Calculate.OnBarClose;
				IsOverlay = true;
				DisplayInDataBox = true;
				DrawOnPricePanel = true;
				DrawHorizontalGridLines = true;
				DrawVerticalGridLines = true;
				PaintPriceMarkers = true;
				ScaleJustification = ScaleJustification.Right;
				IsSuspendedWhileInactive = false;
				BarsRequiredToPlot = 0;
				ShowTransparentPlotsInDataBox = true;
				ConditionTrendStart = false;
				ConditionSlowdown = true;
				ConditionTrendPullback = true;
				ConditionMoveStop = true;
				PopupEnabled = false;
				PopupBackgroundBrush = Brushes.Gold;
				PopupBackgroundOpacity = 60;
				PopupTextBrush = Brushes.DarkSlateGray;
				PopupTextSize = 16;
				PopupButtonBrush = Brushes.Transparent;
				SoundEnabled = false;
				SoundUptrendStart = "Alert4.wav";
				SoundUptrendSlowdown = "Alert1.wav";
				SoundUptrendPullback = "Alert4.wav";
				SoundDowntrendStart = "Alert3.wav";
				SoundDowntrendSlowdown = "Alert2.wav";
				SoundDowntrendPullback = "Alert3.wav";
				SoundMoveStopUp = "Alert1.wav";
				SoundMoveStopDown = "Alert2.wav";
				SoundRearmEnabled = true;
				SoundRearmSeconds = 5;
				EmailEnabled = false;
				EmailReceiver = "receiver@example.com";
				MarkerEnabled = true;
				MarkerRenderingMethod = gbThunderZilla_RenderingMethod.Custom;
				MarkerBrushBullish = Brushes.DodgerBlue;
				MarkerBrushBearish = Brushes.HotPink;
				MarkerStringUptrendStart = "\u25b2 + Trend";
				MarkerStringUptrendSlowdown = "\u2b18";
				MarkerStringUptrendPullback = "\u2b06 + Buy";
				MarkerStringDowntrendStart = "Trend + \u25bc";
				MarkerStringDowntrendSlowndown = "\u2b19";
				MarkerStringDowntrendPullback = "Sell + \u2b07";
				MarkerStringMoveStopUp = "\u235f";
				MarkerStringMoveStopDown = "\u235f";
				MarkerFont = new SimpleFont("Arial", 20);
				MarkerOffset = 10;
				AlertBlockingSeconds = 60;
				ScreenDPI = 100;
				PlotSlowEnabled = true;
				PlotTrendUptrend = Brushes.DodgerBlue;
				PlotTrendDowntrend = Brushes.HotPink;
				PlotTrendNeutral = Brushes.Gold;
				PlotStopEnabled = true;
				PlotStopUptrend = Brushes.DodgerBlue;
				PlotStopDowntrend = Brushes.HotPink;
				BarEnabled = true;
				BarUptrend = Brushes.LimeGreen;
				BarDowntrend = Brushes.Red;
				BarNeutral = Brushes.Goldenrod;
				BarOutlineEnabled = true;
				BarBiasBased = true;
				CloudEnabled = true;
				CloudUptrend = Brushes.DodgerBlue;
				CloudDowntrend = Brushes.HotPink;
				CloudNeutral = Brushes.DarkGoldenrod;
				CloudOpacity = 30;
				TrendMAType = gbThunderZillaMAType.SMA;
				TrendPeriod = 100;
				TrendSmoothingEnabled = false;
				TrendSmoothingMethod = gbThunderZillaMAType.EMA;
				TrendSmoothingPeriod = 10;
				StopOffsetMultiplierStop = 60.0;
				SignalQuantityPerFlat = 2;
				SignalQuantityPerTrend = 999;
				IndicatorZOrder = 0;
				UserNote = "instrument (period)";
				AddPlot(new Stroke(Brushes.Gold, DashStyleHelper.Solid, 5f), PlotStyle.Line, "Trend");
				AddPlot(new Stroke(Brushes.Goldenrod, DashStyleHelper.Dot, 2f), PlotStyle.Line, "Stop");
				AddPlot(new Stroke(Brushes.Transparent, 1f), PlotStyle.Line, "Signal: Trend");
				AddPlot(new Stroke(Brushes.Transparent, 1f), PlotStyle.Line, "Signal: Trade");
				break;

			case State.Configure:
				seriesMinMax = new Series<double>(this, MaximumBarsLookBack.Infinite);
				seriesSWTrendVector = new Series<double>(this, MaximumBarsLookBack.TwoHundredFiftySix);
				seriesSumoMax = new Series<double>(this, MaximumBarsLookBack.TwoHundredFiftySix);
				seriesSumoMin = new Series<double>(this, MaximumBarsLookBack.TwoHundredFiftySix);
				seriesSumoFair = new Series<double>(this, MaximumBarsLookBack.TwoHundredFiftySix);
				seriesOBOSSignalState = new Series<int>(this, MaximumBarsLookBack.TwoHundredFiftySix);
				rearmTimer = new DispatcherTimer();
				rearmTimer.Interval = TimeSpan.FromSeconds(1);
				rearmTimer.Tick += OnRearmTimerTick;
				cloudMixBrush = CreateAverageBrush(CloudUptrend, CloudDowntrend);
				isCustomRenderingMethod = MarkerRenderingMethod == gbThunderZilla_RenderingMethod.Custom;
				if (isCustomRenderingMethod)
				{
					dictMarkers = new Dictionary<int, MarkerInfo>();
				}
				break;

			case State.DataLoaded:
				if (ScreenDPI < 100)
				{
					try
					{
						ScreenDPI = (int)(96 * System.Windows.PresentationSource.FromVisual(ChartControl)?.CompositionTarget?.TransformToDevice.M11 ?? 1.0);
					}
					catch
					{
						ScreenDPI = 96;
					}
				}
				if (IndicatorZOrder != 0)
				{
					SetZOrder(IndicatorZOrder);
				}
				isCharting = ChartControl != null;
				break;

			case State.Historical:
				break;

			case State.Terminated:
				if (isCharting)
				{
					ChartControl.Dispatcher.InvokeAsync(delegate
					{
						if (alertWindow != null)
						{
							alertWindow.Close();
							alertWindow = null;
						}
					});
				}
				if (rearmTimer != null)
				{
					rearmTimer.Stop();
					rearmTimer.Tick -= OnRearmTimerTick;
					rearmTimer = null;
				}
				break;
		}
	}

	protected override void OnBarUpdate()
	{
		double num = Open[0];
		double num2 = Close[0];
		double num3 = High[0];
		double num4 = Low[0];
		isTrendOrSwTrendChanged = false;
		ComputeSolarWindRK(num, num2);
		ComputeSumoPullback(num, num3, num4, num2);
		ComputeMultiOscOBOSOverlap();
		if (CurrentBar == 0)
		{
			return;
		}
		SignalTradeInfo signalTradeInfo = SignalTradeInfo.UptrendStart;
		SignalTradeInfo signalTradeInfo2 = SignalTradeInfo.DowntrendStart;
		SignalTradeInfo signalTradeInfo3 = SignalTradeInfo.NoSignal;
		if (CurrentBar != 1)
		{
			SignalTradeInfo signalTradeInfo4 = signalTradeInfo3;
			if (trendState != signalTradeInfo3)
			{
				if (trendState != signalTradeInfo)
				{
					if (!swIsUptrend || !sumoIsUptrend)
					{
						if (!swIsUptrend || sumoIsUptrend)
						{
							if (!swIsUptrend && sumoIsUptrend)
							{
								countInfo.CountSM = 0;
							}
						}
						else
						{
							countInfo.CountSW = 0;
						}
					}
					else
					{
						trendState = signalTradeInfo;
						countInfo.CountSW = 1;
						countInfo.CountSM = 1;
						isTrendOrSwTrendChanged = true;
						crossIndex = CurrentBar;
					}
					if (countInfo.Total == 0)
					{
						trendState = signalTradeInfo3;
						countInfo.CountSW = 0;
						countInfo.CountSM = 0;
						crossIndex = CurrentBar;
					}
				}
				else
				{
					if (swIsUptrend || sumoIsUptrend)
					{
						if (!swIsUptrend || sumoIsUptrend)
						{
							if (!swIsUptrend && sumoIsUptrend)
							{
								countInfo.CountSW = 0;
							}
						}
						else
						{
							countInfo.CountSM = 0;
						}
					}
					else
					{
						trendState = signalTradeInfo2;
						countInfo.CountSW = 1;
						countInfo.CountSM = 1;
						isTrendOrSwTrendChanged = true;
						crossIndex = CurrentBar;
					}
					if (countInfo.Total == 0)
					{
						trendState = signalTradeInfo3;
						countInfo.CountSW = 0;
						countInfo.CountSM = 0;
						crossIndex = CurrentBar;
					}
				}
			}
			else if (!swIsUptrend || !sumoIsUptrend)
			{
				if (!swIsUptrend && !sumoIsUptrend)
				{
					trendState = signalTradeInfo2;
					countInfo.CountSW = 1;
					countInfo.CountSM = 1;
					isTrendOrSwTrendChanged = true;
					crossIndex = CurrentBar;
				}
			}
			else
			{
				trendState = signalTradeInfo;
				countInfo.CountSW = 1;
				countInfo.CountSM = 1;
				isTrendOrSwTrendChanged = true;
				crossIndex = CurrentBar;
			}
			Signal_Trend[0] = (double)trendState;
			if (Signal_Trend[0] != Signal_Trend[1] && Math.Abs(Signal_Trend[0]) == 1.0)
			{
				countSignalPerTrend = 0.0;
			}
			double num5 = Instrument.MasterInstrument.RoundToTickSize(Trend[0]);
			double num6 = Instrument.MasterInstrument.RoundToTickSize(Trend[1]);
			double num7 = Instrument.MasterInstrument.RoundToTickSize(Stop[0]);
			double num8 = Instrument.MasterInstrument.RoundToTickSize(Stop[1]);
			if (MathExtentions.ApproxCompare(num7, num8) != 0)
			{
				countSignalPerFlat = 0.0;
			}
			int barIndex = CurrentBars[0];
			int num9 = Convert.ToInt32(Signal_Trend[1]);
			if (trendState != SignalTradeInfo.NoSignal)
			{
				bool flag = trendState == signalTradeInfo;
				if (trendState == (SignalTradeInfo)num9)
				{
					int num10 = (flag ? 1 : (-1));
					bool flag2 = MathExtentions.ApproxCompare(num2, num) * num10 > 0 && MathExtentions.ApproxCompare(Close[1], Open[1]) * num10 < 0;
					if (!(countSignalPerTrend >= (double)SignalQuantityPerTrend) && countSignalPerFlat < (double)SignalQuantityPerFlat && flag2)
					{
						if (sumoSignalTrade == 0)
						{
							double num11 = Instrument.MasterInstrument.RoundToTickSize(seriesSWTrendVector[0]);
							double num12 = Instrument.MasterInstrument.RoundToTickSize(seriesSumoFair[0]);
							bool flag3 = MathExtentions.ApproxCompare(num3, num12) >= 0 && MathExtentions.ApproxCompare(num4, num12) <= 0;
							bool flag4 = MathExtentions.ApproxCompare(num3, num5) >= 0 && MathExtentions.ApproxCompare(num4, num5) <= 0;
							bool flag5 = MathExtentions.ApproxCompare(num3, num11) >= 0 && MathExtentions.ApproxCompare(num4, num11) <= 0;
							if ((flag3 && flag5) || (flag3 && flag4))
							{
								if (ConditionTrendPullback)
								{
									if (!isCustomRenderingMethod)
									{
										PrintMarker(SignalInfo.Pullback, flag);
									}
									else
									{
										AddMarker(barIndex, flag, SignalInfo.Pullback);
									}
									TriggerAlerts(SignalInfo.Pullback, flag);
								}
								countSignalPerTrend += 1.0;
								countSignalPerFlat += 1.0;
								signalTradeInfo4 = ((!flag) ? SignalTradeInfo.DowntrendPullback : SignalTradeInfo.UptrendPullback);
							}
						}
						else
						{
							if (ConditionTrendPullback && ((sumoSignalTrade == 1 && flag) || (sumoSignalTrade == -1 && !flag)))
							{
								if (!isCustomRenderingMethod)
								{
									PrintMarker(SignalInfo.Pullback, flag);
								}
								else
								{
									AddMarker(barIndex, flag, SignalInfo.Pullback);
								}
								TriggerAlerts(SignalInfo.Pullback, flag);
							}
							countSignalPerTrend += 1.0;
							countSignalPerFlat += 1.0;
							signalTradeInfo4 = ((!flag) ? SignalTradeInfo.DowntrendPullback : SignalTradeInfo.UptrendPullback);
						}
					}
					if (swIsUptrend == flag && MathExtentions.ApproxCompare(num7, num5) * num10 > 0 && MathExtentions.ApproxCompare(num8, num6) * num10 <= 0)
					{
						if (ConditionMoveStop)
						{
							if (!isCustomRenderingMethod)
							{
								PrintMarker(SignalInfo.MoveStop, flag);
							}
							else
							{
								AddMarker(barIndex, flag, SignalInfo.MoveStop);
							}
							TriggerAlerts(SignalInfo.MoveStop, flag);
						}
						signalTradeInfo4 = ((!flag) ? SignalTradeInfo.MoveStopDown : SignalTradeInfo.MoveStopUp);
					}
				}
				else
				{
					if (ConditionTrendStart)
					{
						if (!isCustomRenderingMethod)
						{
							PrintMarker(SignalInfo.Trend, flag);
						}
						else
						{
							AddMarker(barIndex, trendState > SignalTradeInfo.NoSignal, SignalInfo.Trend);
						}
						TriggerAlerts(SignalInfo.Trend, flag);
					}
					countSignalPerFlat = 0.0;
					signalTradeInfo4 = ((!flag) ? signalTradeInfo2 : signalTradeInfo);
				}
			}
			if (obosSignalTrade != 0)
			{
				if (ConditionSlowdown)
				{
					if (!isCustomRenderingMethod)
					{
						PrintMarker(SignalInfo.Slowdown, obosSignalTrade > 0);
					}
					else
					{
						AddMarker(barIndex, obosSignalTrade > 0, SignalInfo.Slowdown);
					}
					TriggerAlerts(SignalInfo.Slowdown, obosSignalTrade > 0);
				}
				signalTradeInfo4 = ((obosSignalTrade != 1) ? SignalTradeInfo.UptrendSlowdown : SignalTradeInfo.DowntrendSlowdown);
			}
			Signal_Trade[0] = (double)signalTradeInfo4;
			if (isCharting)
			{
				if (PlotSlowEnabled)
				{
					Brush brush = ((trendState == SignalTradeInfo.NoSignal) ? PlotTrendNeutral : ((trendState <= SignalTradeInfo.NoSignal) ? PlotTrendDowntrend : PlotTrendUptrend));
					if (!BrushExtensions.IsTransparent(brush))
					{
						PlotBrushes[0][0] = brush;
					}
				}
				if (PlotStopEnabled)
				{
					Brush brush2 = ((trendState == SignalTradeInfo.NoSignal) ? Brushes.Transparent : ((trendState == signalTradeInfo && swIsUptrend) ? PlotStopUptrend : ((trendState == signalTradeInfo2 && !swIsUptrend) ? PlotStopDowntrend : Brushes.Transparent)));
					if (!isTrendOrSwTrendChanged)
					{
						PlotBrushes[1][0] = brush2;
					}
					else if ((int)Plots[1].PlotStyle == 6 || (int)Plots[1].PlotStyle == 7)
					{
						PlotBrushes[1][0] = Brushes.Transparent;
					}
					else
					{
						PlotBrushes[1][0] = brush2;
					}
				}
			}
			PaintBar(trendState);
			if (!isCharting || !CloudEnabled || CloudOpacity <= 0)
			{
				return;
			}
			double num13 = 0.4 * Math.Abs(num - num2);
			double num14 = num - num5;
			bool flag6 = MathExtentions.ApproxCompare(num, num5) >= 0 && MathExtentions.ApproxCompare(num2, num5) >= 0;
			bool flag7 = MathExtentions.ApproxCompare(num, num5) < 0 && MathExtentions.ApproxCompare(num2, num5) < 0;
			double num15 = Math.Max(num, num2);
			double num16 = Math.Min(num, num2);
			double num17 = ((trendState == SignalTradeInfo.NoSignal) ? ((MathExtentions.ApproxCompare(num15, num5) <= 0) ? num16 : num15) : (flag6 ? num15 : (flag7 ? num16 : ((MathExtentions.ApproxCompare(num14, num13) <= 0) ? num16 : num15))));
			seriesMinMax[0] = num17;
			bool flag8;
			Brush brush3 = ((flag8 = crossIndex == CurrentBar) ? cloudMixBrush : ((trendState == signalTradeInfo) ? CloudUptrend : ((trendState != signalTradeInfo2) ? CloudNeutral : CloudDowntrend)));
			if (!BrushExtensions.IsTransparent(brush3))
			{
				int startBarsAgo = (flag8 ? 1 : (CurrentBar - crossIndex));
				tag = "gbThunderZilla.cloud." + ((!flag8) ? crossIndex : (CurrentBar - 1));
				Draw.Region(this, tag, startBarsAgo, 0, Trend, seriesMinMax, null, brush3, CloudOpacity);
			}
		}
		else
		{
			trendState = ((swIsUptrend && sumoIsUptrend) ? signalTradeInfo : ((!swIsUptrend && !sumoIsUptrend) ? signalTradeInfo2 : signalTradeInfo3));
			Series<double> signal_Trend = Signal_Trend;
			double num18 = (Signal_Trade[0] = (double)trendState);
			signal_Trend[0] = num18;
			countInfo = new CountInfo();
			if (trendState != signalTradeInfo3)
			{
				countInfo.CountSW = 1;
				countInfo.CountSM = 1;
				isTrendOrSwTrendChanged = true;
				crossIndex = CurrentBar;
			}
			else
			{
				countInfo.CountSW = 0;
				countInfo.CountSM = 0;
			}
		}
	}

	private void ComputeSolarWindRK(double open, double close)
	{
		double num = Instrument.MasterInstrument.RoundToTickSize(StopOffsetMultiplierStop * TickSize);
		double num2 = Instrument.MasterInstrument.RoundToTickSize(close - num);
		double num3 = Instrument.MasterInstrument.RoundToTickSize(close + num);
		double num4 = Instrument.MasterInstrument.RoundToTickSize(30.0 * TickSize);
		double num5 = close - num4;
		double num6 = close + num4;
		if (CurrentBar != 0)
		{
			double num7 = seriesSWTrendVector[1];
			double num8 = Stop[1];
			if (!swIsUptrend)
			{
				if (MathExtentions.ApproxCompare(close, num8) <= 0)
				{
					if (MathExtentions.ApproxCompare(num6, num7) >= 0)
					{
						seriesSWTrendVector[0] = num7;
					}
					else
					{
						seriesSWTrendVector[0] = num6;
					}
					Stop[0] = Math.Min(num3, num8);
					return;
				}
				swIsUptrend = true;
				isTrendOrSwTrendChanged = true;
				if (MathExtentions.ApproxCompare(num5, num7) <= 0)
				{
					seriesSWTrendVector[0] = num7;
				}
				else
				{
					seriesSWTrendVector[0] = num5;
				}
				Stop[0] = num2;
			}
			else if (MathExtentions.ApproxCompare(close, num8) >= 0)
			{
				if (MathExtentions.ApproxCompare(num5, num7) <= 0)
				{
					seriesSWTrendVector[0] = num7;
				}
				else
				{
					seriesSWTrendVector[0] = num5;
				}
				Stop[0] = Math.Max(num2, num8);
			}
			else
			{
				swIsUptrend = false;
				isTrendOrSwTrendChanged = true;
				if (MathExtentions.ApproxCompare(num6, num7) >= 0)
				{
					seriesSWTrendVector[0] = num7;
				}
				else
				{
					seriesSWTrendVector[0] = num6;
				}
				Stop[0] = num3;
			}
		}
		else
		{
			swIsUptrend = MathExtentions.ApproxCompare(close, open) > 0;
			if (!swIsUptrend)
			{
				seriesSWTrendVector[0] = num6;
				Stop[0] = num3;
			}
			else
			{
				seriesSWTrendVector[0] = num5;
				Stop[0] = num2;
			}
		}
	}

	private void ComputeSumoPullback(double open0, double high0, double low0, double close0)
	{
		if (CurrentBar == 0)
		{
			return;
		}
		sumoSignalTrade = 0;
		double num = GetMASmoothed(Input, TrendMAType, TrendPeriod, TrendSmoothingEnabled, TrendSmoothingMethod, TrendSmoothingPeriod);
		double num2 = EMA(Input, 14)[0];
		double num3 = EMA(Input, 30)[0];
		double num4 = EMA(Input, 45)[0];
		double num5 = Math.Max(Math.Max(num, num2), Math.Max(num3, num4));
		double num6 = Math.Min(Math.Min(num, num2), Math.Min(num3, num4));
		if (MathExtentions.ApproxCompare(close0, open0) <= 0 || MathExtentions.ApproxCompare(Close[1], Open[1]) >= 0)
		{
			if (MathExtentions.ApproxCompare(close0, open0) < 0 && MathExtentions.ApproxCompare(Close[1], Open[1]) > 0 && MathExtentions.ApproxCompare(num6, low0) > 0 && MathExtentions.ApproxCompare(num5, high0) < 0 && MathExtentions.ApproxCompare(num, num5) == 0)
			{
				if (sumoCountSignalBars != 0 || sumoNextBar >= 0)
				{
					if (sumoCountSignalBars != 1)
					{
						if (sumoCountSignalBars == 2 && CurrentBar > sumoNextBar)
						{
							sumoSignalTrade = -1;
						}
					}
					else if (CurrentBar >= sumoNextBar)
					{
						sumoSignalTrade = -1;
						sumoCountSignalBars++;
						sumoNextBar = CurrentBar + 30;
					}
				}
				else
				{
					sumoSignalTrade = -1;
					sumoCountSignalBars++;
					sumoNextBar = CurrentBar + 15;
				}
			}
		}
		else if (MathExtentions.ApproxCompare(num6, low0) > 0 && MathExtentions.ApproxCompare(num5, high0) < 0 && MathExtentions.ApproxCompare(num, num6) == 0)
		{
			if (sumoCountSignalBars != 0 || sumoNextBar >= 0)
			{
				if (sumoCountSignalBars != 1)
				{
					if (sumoCountSignalBars == 2 && CurrentBar > sumoNextBar)
					{
						sumoSignalTrade = 1;
					}
				}
				else if (CurrentBar >= sumoNextBar)
				{
					sumoSignalTrade = 1;
					sumoCountSignalBars++;
					sumoNextBar = CurrentBar + 30;
				}
			}
			else
			{
				sumoSignalTrade = 1;
				sumoCountSignalBars++;
				sumoNextBar = CurrentBar + 15;
			}
		}
		if (CurrentBar > sumoNextBar && sumoCountSignalBars != 0)
		{
			sumoCountSignalBars = 0;
			sumoNextBar = -1;
		}
		Trend[0] = num;
		seriesSumoMax[0] = num5;
		seriesSumoMin[0] = num6;
		double num7 = Trend[0];
		double num8 = Trend[1];
		if (CurrentBar == 1)
		{
			sumoIsUptrend = MathExtentions.ApproxCompare(num5, num6) > 0;
		}
		if (!sumoIsUptrend || MathExtentions.ApproxCompare(seriesSumoMin[0], num7) >= 0 || (MathExtentions.ApproxCompare(seriesSumoMax[0], num7) != 0 && MathExtentions.ApproxCompare(seriesSumoMin[1], num8) != 0))
		{
			if (!sumoIsUptrend && MathExtentions.ApproxCompare(seriesSumoMax[0], num7) > 0 && (MathExtentions.ApproxCompare(seriesSumoMin[0], num7) == 0 || MathExtentions.ApproxCompare(seriesSumoMax[1], num8) == 0))
			{
				sumoIsUptrend = true;
			}
		}
		else
		{
			sumoIsUptrend = false;
		}
		seriesSumoFair[0] = (num + num2 + num3 + num4) * 0.25;
	}

	private void ComputeMultiOscOBOSOverlap()
	{
		if (CurrentBar == 0)
		{
			return;
		}
		double oscValue = MFI(Input, 14)[0];
		double oscValue2 = RSI(Input, 14, 3)[0];
		double oscValue3 = Stochastics(Input, 14, 7, 3).D[0];
		int stateSimple = GetStateSimple(oscValue, 70.0, 30.0);
		int stateSimple2 = GetStateSimple(oscValue2, 70.0, 30.0);
		int stateSimple3 = GetStateSimple(oscValue3, 70.0, 30.0);
		int num = 0;
		if (stateSimple <= 0 || stateSimple2 <= 0 || stateSimple3 <= 0)
		{
			if (stateSimple < 0 && stateSimple2 < 0 && stateSimple3 < 0)
			{
				num = -1;
			}
		}
		else
		{
			num = 1;
		}
		seriesOBOSSignalState[0] = num;
		if (seriesOBOSSignalState[0] == 0 && seriesOBOSSignalState[1] != 0)
		{
			obosOverlapExitBarIndex = CurrentBar;
			obosLastSignalState = Convert.ToInt32(seriesOBOSSignalState[1]);
		}
		obosSignalTrade = 0;
		if (obosOverlapExitBarIndex > 0 && num == 0 && CurrentBar - obosOverlapExitBarIndex < 3)
		{
			if (obosLastSignalState > 0 && MathExtentions.ApproxCompare(Close[0], Close[1]) < 0)
			{
				obosOverlapExitBarIndex = -1;
				obosSignalTrade = -1;
			}
			if (obosLastSignalState < 0 && MathExtentions.ApproxCompare(Close[0], Close[1]) > 0)
			{
				obosOverlapExitBarIndex = -1;
				obosSignalTrade = 1;
			}
		}
	}

	private int GetStateSimple(double oscValue, double thresholdHigh, double thresholdLow)
	{
		if (MathExtentions.ApproxCompare(oscValue, thresholdHigh) < 0)
		{
			if (MathExtentions.ApproxCompare(oscValue, thresholdLow) > 0)
			{
				return 0;
			}
			return -1;
		}
		return 1;
	}

	protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
	{
		if (isCharting && ChartControl != null && !IsInHitTest)
		{
			if (MarkerRenderingMethod == gbThunderZilla_RenderingMethod.Custom)
			{
				DrawMarkers(chartScale);
			}
			base.OnRender(chartControl, chartScale);
		}
	}

	private void AddMarker(int barIndex, bool isBullish, SignalInfo signalInfo)
	{
		if (MarkerEnabled)
		{
			MarkerInfo value = new MarkerInfo
			{
				BarIndex = barIndex,
				IsBullish = isBullish,
				SignalInfo = signalInfo
			};
			if (dictMarkers.ContainsKey(barIndex))
			{
				dictMarkers[barIndex] = value;
			}
			else
			{
				dictMarkers.Add(barIndex, value);
			}
		}
	}

	private void DrawMarkers(ChartScale chartScale)
	{
		if (!MarkerEnabled || dictMarkers == null || dictMarkers.Count <= 0)
		{
			return;
		}
		for (int i = ChartBars.FromIndex; i <= Math.Min(CurrentBars[0], ChartBars.ToIndex); i++)
		{
			if (dictMarkers.ContainsKey(i))
			{
				DrawAMarker(chartScale, dictMarkers[i]);
			}
		}
	}

	private void DrawAMarker(ChartScale chartScale, MarkerInfo markerInfo)
	{
		bool isBullish = markerInfo.IsBullish;
		SignalInfo signalInfo = markerInfo.SignalInfo;
		string text = ((signalInfo == SignalInfo.Trend) ? ((!isBullish) ? MarkerStringDowntrendStart : MarkerStringUptrendStart) : ((signalInfo == SignalInfo.Slowdown) ? ((!isBullish) ? MarkerStringDowntrendSlowndown : MarkerStringUptrendSlowdown) : ((signalInfo == SignalInfo.Pullback) ? ((!isBullish) ? MarkerStringDowntrendPullback : MarkerStringUptrendPullback) : ((!isBullish) ? MarkerStringMoveStopDown : MarkerStringMoveStopUp))));
		if (string.IsNullOrWhiteSpace(text) || (!isBullish && MathExtentions.ApproxCompare(High.GetValueAt(markerInfo.BarIndex), chartScale.MaxValue) >= 0) || (isBullish && MathExtentions.ApproxCompare(Low.GetValueAt(markerInfo.BarIndex), chartScale.MinValue) <= 0))
		{
			return;
		}
		Brush brush = ((!isBullish) ? MarkerBrushBearish : MarkerBrushBullish);
		if (!BrushExtensions.IsTransparent(brush))
		{
			text = FormatMarkerString(text);
			int num = (isBullish ? 1 : (-1));
			float num2 = ChartControl.GetXByBarIndex(ChartBars, markerInfo.BarIndex);
			object obj;
			if (signalInfo == SignalInfo.Trend || signalInfo == SignalInfo.MoveStop)
			{
				ISeries<double> trend = Trend;
				obj = trend;
			}
			else
			{
				obj = ((!isBullish) ? High : Low);
			}
			float num3 = chartScale.GetYByValue(((ISeries<double>)obj).GetValueAt(markerInfo.BarIndex)) + num * MarkerOffset;
			DrawTextOnChart(text, MarkerFont, num2, num3, 0, num, brush, ScreenDPI, RenderTarget);
		}
	}

	private void PrintMarker(SignalInfo signalInfo, bool isBullish)
	{
		if (isCharting && MarkerEnabled && CurrentBar >= BarsRequiredToPlot)
		{
			string text = ((signalInfo == SignalInfo.Trend) ? "trend" : ((signalInfo == SignalInfo.Slowdown) ? "slowdown" : ((signalInfo == SignalInfo.Pullback) ? "pullback" : "move-stop")));
			tag = "gbThunderZilla.marker." + text + "." + CurrentBar;
			Brush textBrush = ((!isBullish) ? MarkerBrushBearish : MarkerBrushBullish);
			double y = ((signalInfo != SignalInfo.MoveStop && signalInfo != SignalInfo.Trend) ? ((!isBullish) ? High[0] : Low[0]) : Trend[0]);
			string text2 = ((signalInfo == SignalInfo.Trend) ? ((!isBullish) ? MarkerStringDowntrendStart : MarkerStringUptrendStart) : ((signalInfo == SignalInfo.Slowdown) ? ((!isBullish) ? MarkerStringDowntrendSlowndown : MarkerStringUptrendSlowdown) : ((signalInfo == SignalInfo.Pullback) ? ((!isBullish) ? MarkerStringDowntrendPullback : MarkerStringUptrendPullback) : ((!isBullish) ? MarkerStringMoveStopDown : MarkerStringMoveStopUp))));
			text2 = FormatMarkerString(text2);
			int num = ComputeTextHeight(text2, MarkerFont);
			int yPixelOffset = ((!isBullish) ? 1 : (-1)) * (MarkerOffset + num / 2);
			Draw.Text(this, tag, IsAutoScale, text2, 0, y, yPixelOffset, textBrush, MarkerFont, TextAlignment.Center, Brushes.Transparent, Brushes.Transparent, 0);
		}
	}

	private void PaintBar(SignalTradeInfo trendState, bool isToggleClickEvent = false, int barIndex = 0)
	{
		if (!isCharting || !BarEnabled)
		{
			return;
		}
		Brush brush = ((trendState == SignalTradeInfo.NoSignal) ? BarNeutral : ((trendState != SignalTradeInfo.UptrendStart) ? BarDowntrend : BarUptrend));
		int num = ((!isToggleClickEvent) ? MathExtentions.ApproxCompare(Close[0], Open[0]) : MathExtentions.ApproxCompare(Close.GetValueAt(barIndex), Open.GetValueAt(barIndex)));
		int num2 = ((trendState != SignalTradeInfo.NoSignal) ? ((trendState == SignalTradeInfo.UptrendStart) ? 1 : (-1)) : 0);
		int num3 = CurrentBar - barIndex;
		if (BarOutlineEnabled && !BrushExtensions.IsTransparent(brush))
		{
			if (!isToggleClickEvent)
			{
				CandleOutlineBrush = brush;
			}
			else
			{
				CandleOutlineBrushes[num3] = brush;
			}
		}
		if (!BarBiasBased)
		{
			if (num != 0)
			{
				if (!isToggleClickEvent)
				{
					BarBrush = brush;
				}
				else
				{
					BarBrushes[num3] = brush;
				}
			}
		}
		else if (!BrushExtensions.IsTransparent(brush))
		{
			if (num != 0)
			{
				if (!isToggleClickEvent)
				{
					BarBrush = ((num2 * num <= 0) ? Brushes.Transparent : brush);
				}
				else
				{
					BarBrushes[num3] = ((num2 * num <= 0) ? Brushes.Transparent : brush);
				}
			}
		}
		else if (num2 * num < 0)
		{
			if (!isToggleClickEvent)
			{
				BarBrush = Brushes.Transparent;
			}
			else
			{
				BarBrushes[num3] = Brushes.Transparent;
			}
		}
	}

	public override string FormatPriceMarker(double price)
	{
		return Instrument.MasterInstrument.FormatPrice(Instrument.MasterInstrument.RoundToTickSize(price), true);
	}

	private void TriggerAlerts(SignalInfo signalInfo, bool isBullish)
	{
		if (State == State.Historical || (!PopupEnabled && !SoundEnabled && !EmailEnabled))
		{
			return;
		}
		DateTime now = DateTime.Now;
		if (now < nextAlert)
		{
			return;
		}
		string text = now.ToString("HH:mm");
		string text2 = now.ToString("HH:mm:ss, dd MMM yyyy");
		nextAlert = now + TimeSpan.FromSeconds(AlertBlockingSeconds);
		string text3 = Instrument.FullName + " (" + ((object)BarsPeriod)?.ToString() + ")";
		bool flag = signalInfo == SignalInfo.Trend;
		bool flag2 = signalInfo == SignalInfo.Slowdown;
		bool flag3 = signalInfo == SignalInfo.Pullback;
		string arg = (flag ? "TREND-START" : (flag2 ? "TREND-SLOWDOWN" : (flag3 ? "TREND-PULLBACK" : "MOVE-STOP")));
		string text4 = string.Format("{0}: {1} alert on {2} at ", "ThunderZilla", arg, text3) + text;
		string text5 = (flag ? string.Format("The market has entered {0}.", (!isBullish) ? "a DOWNTREND" : "an UPTREND") : (flag2 ? string.Format("The {0} has slowed down.", (!isBullish) ? "DOWNTREND" : "UPTREND") : (flag3 ? string.Format("The {0} has had a PULLBACK.", (!isBullish) ? "downtrend" : "uptrend") : string.Format("The {0} has had a MOVE STOP.", (!isBullish) ? "downtrend" : "uptrend"))));
		string popupMessage = text5 + "\n\nAlert chart: " + text3 + "\nAlert time: " + text2;
		string text6 = "\n_______________________\n\n";
		string text7 = popupMessage + text6 + "ThunderZilla by GreyBeard\nWebsite: https://greybeard.com";
		if (PopupEnabled && isCharting)
		{
			ChartControl.Dispatcher.InvokeAsync(delegate
			{
				if (alertWindow != null)
				{
					alertWindow.Close();
				}
				alertWindow = new Window
				{
					Title = "ThunderZilla by GreyBeard",
					Width = 400,
					Height = 300,
					WindowStartupLocation = WindowStartupLocation.CenterOwner,
					Background = PopupBackgroundBrush,
					Opacity = PopupBackgroundOpacity / 100.0,
					Topmost = true,
					Content = new TextBlock
					{
						Text = popupMessage,
						Foreground = PopupTextBrush,
						FontSize = PopupTextSize,
						TextWrapping = TextWrapping.Wrap,
						Margin = new Thickness(15)
					}
				};
				alertWindow.Show();
			});
		}
		if (SoundEnabled)
		{
			string text8 = "alert @ " + text2;
			soundPath = string.Concat(str2: flag ? ((!isBullish) ? SoundDowntrendStart : SoundUptrendStart) : (flag2 ? ((!isBullish) ? SoundUptrendSlowdown : SoundDowntrendSlowdown) : (flag3 ? ((!isBullish) ? SoundDowntrendPullback : SoundUptrendPullback) : ((!isBullish) ? SoundMoveStopDown : SoundMoveStopUp))), str0: Globals.InstallDir, str1: "sounds\\");
			Alert(text8, Priority.Low, text4, soundPath, 0, Brushes.Red, Brushes.Yellow);
			if (SoundRearmEnabled && PopupEnabled && isCharting)
			{
				nextRearm = now + TimeSpan.FromSeconds(SoundRearmSeconds);
				rearmTimer.Start();
			}
		}
		if (EmailEnabled && EmailReceiver != "receiver@example.com")
		{
			SendMail(EmailReceiver, text4, text7);
		}
	}

	private void OnRearmTimerTick(object sender, EventArgs e)
	{
		TriggerCustomEvent((Action<object>)delegate
		{
			if (isCharting)
			{
				ChartControl.Dispatcher.InvokeAsync(delegate
				{
					if (alertWindow != null && !alertWindow.IsVisible)
					{
						rearmTimer.Stop();
					}
					else
					{
						if (DateTime.Now >= nextRearm)
						{
							nextRearm = DateTime.Now + TimeSpan.FromSeconds(SoundRearmSeconds);
							PlaySound(soundPath);
						}
					}
				});
			}
		}, (object)e);
	}

}

// ==== gbKingPanaZilla (composite signal indicator) ==============
[CategoryOrder("General",                    1000010)]
[CategoryOrder("KingOrderBlock Parameters",  1000020)]
[CategoryOrder("PANAKanal Parameters",       1000030)]
[CategoryOrder("ThunderZilla Parameters",    1000040)]
[CategoryOrder("Visuals",                    1000050)]
[CategoryOrder("Display",                    1000055)]
[CategoryOrder("Logging",                    1000060)]
public class gbKingPanaZilla : Indicator
{
	// ---- child indicator references -------------------------
	// Cache arrays let us call CacheIndicator<T> directly, bypassing the
	// named factory methods (gbKingOrderBlock(...) etc.). This prevents NT8's
	// compiler from injecting duplicate factory declarations into this file's
	// generated-code section when all four files are compiled together.
	private GreyBeard.KingPanaZilla.gbKingOrderBlock   _king;
	private GreyBeard.KingPanaZilla.gbPANAKanal        _pana;
	private GreyBeard.KingPanaZilla.gbThunderZilla     _thunder;
	private GreyBeard.KingPanaZilla.gbKingOrderBlock[] _cacheKing;
	private GreyBeard.KingPanaZilla.gbPANAKanal[]      _cachePana;
	private GreyBeard.KingPanaZilla.gbThunderZilla[]   _cacheThunder;

	// ---- chart panel index (saved at DataLoaded for Terminated cleanup) --
	private int _chartPanelIndex = -1;

	// ---- CSV logging ----------------------------------------
	private StreamWriter _logWriter;

	// ---- signal output series (Values[0..2]) ----------------
	// +1 = buy signal, -1 = sell signal, 0 = no signal
	[Browsable(false)]
	[XmlIgnore]
	public NinjaTrader.NinjaScript.Series<double> PanaZillia_Trade => Values[0];

	[Browsable(false)]
	[XmlIgnore]
	public NinjaTrader.NinjaScript.Series<double> KingZilla_Trade  => Values[1];

	[Browsable(false)]
	[XmlIgnore]
	public NinjaTrader.NinjaScript.Series<double> KingPana_Trade   => Values[2];

	// =========================================================
	protected override void OnStateChange()
	{
		switch (State)
		{
		case State.SetDefaults:
			Description              = "Composite signal indicator — combines gbKingOrderBlock, gbPANAKanal, and gbThunderZilla into three cross-system trade signals (+1 buy / -1 sell).";
			Name                     = "gbKingPanaZilla";
			Calculate                = Calculate.OnBarClose;
			IsOverlay                = true;
			DisplayInDataBox         = true;
			DrawOnPricePanel         = true;
			PaintPriceMarkers        = false;
			ScaleJustification       = ScaleJustification.Right;
			IsSuspendedWhileInactive = false;
			BarsRequiredToPlot       = 0;
			ShowTransparentPlotsInDataBox = true;

			// ---- KingOrderBlock defaults --------------------
			King_SwingPointNeighborhood              = 5;
			King_ImbalanceQualifying                 = 3;
			King_OrderBlockFindingBosChochPeriod     = 50;
			King_OrderBlockAge                       = 500;
			King_OrderBlocksSameDirectionOffset      = 10;
			King_OrderBlocksDifferenceDirectionOffset= 10;
			King_SignalTradeQuantityPerOrderBlock     = 3;
			King_SignalTradeSplitBars                 = 6;

			// ---- PANAKanal defaults -------------------------
			Pana_Period                              = 20;
			Pana_Factor                              = 4.0;
			Pana_MiddlePeriod                        = 14;
			Pana_SignalBreakSplit                     = 20;
			Pana_SignalPullbackFindingPeriod          = 10;

			// ---- ThunderZilla defaults ----------------------
			Thunder_TrendMAType                      = gbThunderZillaMAType.SMA;
			Thunder_TrendPeriod                      = 100;
			Thunder_TrendSmoothingEnabled            = false;
			Thunder_TrendSmoothingMethod             = gbThunderZillaMAType.EMA;
			Thunder_TrendSmoothingPeriod             = 10;
			Thunder_StopOffsetMultiplierStop         = 60.0;
			Thunder_SignalQuantityPerFlat             = 2;
			Thunder_SignalQuantityPerTrend            = 999;

			// ---- Visual defaults ----------------------------
			PanaZilliaBrush = Brushes.Cyan;
			KingZillaBrush  = Brushes.DodgerBlue;
			KingPanaBrush   = Brushes.LimeGreen;
			ArrowOffset     = 3;

			// ---- Display defaults ---------------------------
			ShowKingOrderBlock = true;
			ShowPANAKanal      = true;
			ShowThunderZilla   = true;

			// ---- Logging defaults ---------------------------
			LogEnabled = false;
			break;

		case State.Configure:
			// Transparent plots — +1/−1/0 readable from DataBox and other scripts.
			// Visual arrows are drawn in OnBarUpdate via Draw.ArrowUp/Down.
			AddPlot(new Stroke(Brushes.Transparent, 1), PlotStyle.Dot, "PanaZillia Trade");
			AddPlot(new Stroke(Brushes.Transparent, 1), PlotStyle.Dot, "KingZilla Trade");
			AddPlot(new Stroke(Brushes.Transparent, 1), PlotStyle.Dot, "KingPana Trade");
			break;

		case State.DataLoaded:
			// Factory methods (CacheIndicator) add child indicators to NinjaScripts so
			// their OnBarUpdate runs automatically. AddChartIndicator is Strategy-only;
			// add the three child indicators directly to the chart if visual rendering
			// of their zones/stops is also needed.
			if (LogEnabled)
			{
				string logPath = Path.Combine(
					Globals.UserDataDir,
					"gbKPZlog_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".csv");
				_logWriter = new StreamWriter(logPath, append: false, encoding: Encoding.UTF8);
				_logWriter.WriteLine("DateTime,Instrument,Price,PanaZillia_Trade,KingZilla_Trade,KingPana_Trade");
				_logWriter.Flush();
			}

			_king = CacheIndicator<GreyBeard.KingPanaZilla.gbKingOrderBlock>(
				new GreyBeard.KingPanaZilla.gbKingOrderBlock
				{
					SwingPointNeighborhood               = King_SwingPointNeighborhood,
					ImbalanceQualifying                  = King_ImbalanceQualifying,
					OrderBlockFindingBosChochPeriod      = King_OrderBlockFindingBosChochPeriod,
					OrderBlockAge                        = King_OrderBlockAge,
					OrderBlocksSameDirectionOffset       = King_OrderBlocksSameDirectionOffset,
					OrderBlocksDifferenceDirectionOffset = King_OrderBlocksDifferenceDirectionOffset,
					SignalTradeQuantityPerOrderBlock      = King_SignalTradeQuantityPerOrderBlock,
					SignalTradeSplitBars                 = King_SignalTradeSplitBars
				},
				Input, ref _cacheKing);

			_pana = CacheIndicator<GreyBeard.KingPanaZilla.gbPANAKanal>(
				new GreyBeard.KingPanaZilla.gbPANAKanal
				{
					Period                      = Pana_Period,
					Factor                      = Pana_Factor,
					MiddlePeriod                = Pana_MiddlePeriod,
					SignalBreakSplit             = Pana_SignalBreakSplit,
					SignalPullbackFindingPeriod  = Pana_SignalPullbackFindingPeriod
				},
				Input, ref _cachePana);

			_thunder = CacheIndicator<GreyBeard.KingPanaZilla.gbThunderZilla>(
				new GreyBeard.KingPanaZilla.gbThunderZilla
				{
					TrendMAType               = Thunder_TrendMAType,
					TrendPeriod               = Thunder_TrendPeriod,
					TrendSmoothingEnabled     = Thunder_TrendSmoothingEnabled,
					TrendSmoothingMethod      = Thunder_TrendSmoothingMethod,
					TrendSmoothingPeriod      = Thunder_TrendSmoothingPeriod,
					StopOffsetMultiplierStop  = Thunder_StopOffsetMultiplierStop,
					SignalQuantityPerFlat      = Thunder_SignalQuantityPerFlat,
					SignalQuantityPerTrend     = Thunder_SignalQuantityPerTrend
				},
				Input, ref _cacheThunder);

			// Add child indicators to the chart panel so their OnRender runs and their
			// visual drawings (zones, channels, clouds) appear on screen.
			// CacheIndicator wires them into the data pipeline; this wires them into
			// the rendering pipeline of the same panel gbKingPanaZilla lives on.
			if (ChartControl != null && ChartPanel != null)
			{
				_chartPanelIndex = ChartPanel.PanelIndex;
				var king    = _king;
				var pana    = _pana;
				var thunder = _thunder;
				int pidx    = _chartPanelIndex;

				ChartControl.Dispatcher.InvokeAsync(() =>
				{
					if (pidx >= ChartControl.ChartPanels.Count) return;
					var panel = ChartControl.ChartPanels[pidx];
					if (ShowKingOrderBlock && king    != null && !panel.NinjaScripts.Contains(king))
						panel.NinjaScripts.Add(king);
					if (ShowPANAKanal      && pana    != null && !panel.NinjaScripts.Contains(pana))
						panel.NinjaScripts.Add(pana);
					if (ShowThunderZilla   && thunder != null && !panel.NinjaScripts.Contains(thunder))
						panel.NinjaScripts.Add(thunder);
				});
			}
			break;

		case State.Terminated:
			// Remove child indicators from the chart rendering pipeline.
			// Capture refs before nulling; the lambda closes over the local copies.
			if (ChartControl != null && _chartPanelIndex >= 0)
			{
				var kingRef    = _king;
				var panaRef    = _pana;
				var thunderRef = _thunder;
				int pidx       = _chartPanelIndex;

				ChartControl.Dispatcher.InvokeAsync(() =>
				{
					if (pidx >= ChartControl.ChartPanels.Count) return;
					var panel = ChartControl.ChartPanels[pidx];
					if (kingRef    != null) panel.NinjaScripts.Remove(kingRef);
					if (panaRef    != null) panel.NinjaScripts.Remove(panaRef);
					if (thunderRef != null) panel.NinjaScripts.Remove(thunderRef);
				});
			}
			_chartPanelIndex = -1;
			_king         = null;
			_pana         = null;
			_thunder      = null;
			_cacheKing    = null;
			_cachePana    = null;
			_cacheThunder = null;
			if (_logWriter != null)
			{
				_logWriter.Flush();
				_logWriter.Dispose();
				_logWriter = null;
			}
			break;
		}
	}

	// =========================================================
	protected override void OnBarUpdate()
	{
		if (_pana == null || _thunder == null || _king == null)
			return;

		// Reset all signal plots each bar
		Values[0][0] = Values[1][0] = Values[2][0] = 0;

		double pkSig  = _pana.Signal_Trade[0];
		double tzSig  = _thunder.Signal_Trade[0];
		double koSig  = _king.Signal_Trade[0];
		double offset = ArrowOffset * TickSize;

		// ---- PanaZillia Trade ----
		if (pkSig >= 2 && tzSig >= 3)
		{
			Values[0][0] = 1;
			Draw.ArrowUp(this, "KPZ_PZ_" + CurrentBar, false,
				0, Low[0] - offset, PanaZilliaBrush);
		}
		else if (pkSig <= -2 && tzSig <= -3)
		{
			Values[0][0] = -1;
			Draw.ArrowDown(this, "KPZ_PZ_" + CurrentBar, false,
				0, High[0] + offset, PanaZilliaBrush);
		}

		// ---- KingZilla Trade ----
		if (tzSig >= 3 && koSig >= 1)
		{
			Values[1][0] = 1;
			Draw.ArrowUp(this, "KPZ_KZ_" + CurrentBar, false,
				0, Low[0] - offset, KingZillaBrush);
		}
		else if (tzSig <= -3 && koSig <= -1)
		{
			Values[1][0] = -1;
			Draw.ArrowDown(this, "KPZ_KZ_" + CurrentBar, false,
				0, High[0] + offset, KingZillaBrush);
		}

		// ---- KingPana Trade ----
		if (pkSig >= 2 && koSig >= 1)
		{
			Values[2][0] = 1;
			Draw.ArrowUp(this, "KPZ_KP_" + CurrentBar, false,
				0, Low[0] - offset, KingPanaBrush);
		}
		else if (pkSig <= -2 && koSig <= -1)
		{
			Values[2][0] = -1;
			Draw.ArrowDown(this, "KPZ_KP_" + CurrentBar, false,
				0, High[0] + offset, KingPanaBrush);
		}

		// ---- CSV log (any signal fires) ---------------------
		if (_logWriter != null && (Values[0][0] != 0 || Values[1][0] != 0 || Values[2][0] != 0))
		{
			_logWriter.WriteLine(string.Format("{0},\"{1}\",{2},{3},{4},{5}",
				Time[0].ToString("yyyy-MM-dd HH:mm:ss"),
				Instrument.FullName,
				Close[0].ToString(System.Globalization.CultureInfo.InvariantCulture),
				(int)Values[0][0],
				(int)Values[1][0],
				(int)Values[2][0]));
			_logWriter.Flush();
		}
	}

	// =========================================================
	#region Properties

	// ---- KingOrderBlock parameters --------------------------
	[Display(Name = "Swing Point: Neighborhood", Order = 0, GroupName = "KingOrderBlock Parameters")]
	[Range(1, int.MaxValue)]
	public int King_SwingPointNeighborhood { get; set; }

	[Display(Name = "Imbalance: Qualifying (Bars)", Order = 10, GroupName = "KingOrderBlock Parameters")]
	[Range(1, int.MaxValue)]
	public int King_ImbalanceQualifying { get; set; }

	[Display(Name = "Order Block: Finding BOS/CHoCH Period", Order = 20, GroupName = "KingOrderBlock Parameters")]
	[Range(1, int.MaxValue)]
	public int King_OrderBlockFindingBosChochPeriod { get; set; }

	[Display(Name = "Order Block: Age (Bars)", Order = 30, GroupName = "KingOrderBlock Parameters")]
	[Range(0, int.MaxValue)]
	public int King_OrderBlockAge { get; set; }

	[Display(Name = "Order Blocks: Same Direction Offset (Ticks)", Order = 40, GroupName = "KingOrderBlock Parameters")]
	[Range(0, int.MaxValue)]
	public int King_OrderBlocksSameDirectionOffset { get; set; }

	[Display(Name = "Order Blocks: Diff Direction Offset (Ticks)", Order = 50, GroupName = "KingOrderBlock Parameters")]
	[Range(0, int.MaxValue)]
	public int King_OrderBlocksDifferenceDirectionOffset { get; set; }

	[Display(Name = "Signal Trade: Quantity Per OB", Order = 60, GroupName = "KingOrderBlock Parameters")]
	[Range(1, int.MaxValue)]
	public int King_SignalTradeQuantityPerOrderBlock { get; set; }

	[Display(Name = "Signal Trade: Split (Bars)", Order = 70, GroupName = "KingOrderBlock Parameters")]
	[Range(1, int.MaxValue)]
	public int King_SignalTradeSplitBars { get; set; }

	// ---- PANAKanal parameters --------------------------------
	[Display(Name = "Period", Order = 0, GroupName = "PANAKanal Parameters")]
	[Range(1, int.MaxValue)]
	public int Pana_Period { get; set; }

	[Display(Name = "Factor", Order = 10, GroupName = "PANAKanal Parameters")]
	[Range(0.01, double.MaxValue)]
	public double Pana_Factor { get; set; }

	[Display(Name = "Middle Period", Order = 20, GroupName = "PANAKanal Parameters")]
	[Range(1, int.MaxValue)]
	public int Pana_MiddlePeriod { get; set; }

	[Display(Name = "Signal Break Split (Bars)", Order = 30, GroupName = "PANAKanal Parameters")]
	[Range(1, int.MaxValue)]
	public int Pana_SignalBreakSplit { get; set; }

	[Display(Name = "Signal Pullback Finding Period", Order = 40, GroupName = "PANAKanal Parameters")]
	[Range(1, int.MaxValue)]
	public int Pana_SignalPullbackFindingPeriod { get; set; }

	// ---- ThunderZilla parameters ----------------------------
	[Display(Name = "Trend: MA Type", Order = 0, GroupName = "ThunderZilla Parameters")]
	public gbThunderZillaMAType Thunder_TrendMAType { get; set; }

	[Display(Name = "Trend: Period", Order = 10, GroupName = "ThunderZilla Parameters")]
	[Range(1, int.MaxValue)]
	public int Thunder_TrendPeriod { get; set; }

	[Display(Name = "Trend: Smoothing Enabled", Order = 20, GroupName = "ThunderZilla Parameters")]
	public bool Thunder_TrendSmoothingEnabled { get; set; }

	[Display(Name = "Trend: Smoothing Method", Order = 30, GroupName = "ThunderZilla Parameters")]
	public gbThunderZillaMAType Thunder_TrendSmoothingMethod { get; set; }

	[Display(Name = "Trend: Smoothing Period", Order = 40, GroupName = "ThunderZilla Parameters")]
	[Range(1, int.MaxValue)]
	public int Thunder_TrendSmoothingPeriod { get; set; }

	[Display(Name = "Stop: Offset Multiplier (Ticks)", Order = 50, GroupName = "ThunderZilla Parameters")]
	[Range(0.0, double.MaxValue)]
	public double Thunder_StopOffsetMultiplierStop { get; set; }

	[Display(Name = "Signal: Quantity Per Flat", Order = 60, GroupName = "ThunderZilla Parameters")]
	[Range(1, int.MaxValue)]
	public int Thunder_SignalQuantityPerFlat { get; set; }

	[Display(Name = "Signal: Quantity Per Trend", Order = 70, GroupName = "ThunderZilla Parameters")]
	[Range(1, int.MaxValue)]
	public int Thunder_SignalQuantityPerTrend { get; set; }

	// ---- Visual parameters -----------------------------------
	[Display(Name = "PanaZillia Color", Order = 0, GroupName = "Visuals")]
	[XmlIgnore]
	public Brush PanaZilliaBrush { get; set; }
	[Browsable(false)]
	public string PanaZilliaBrushSerialize
	{
		get { return Serialize.BrushToString(PanaZilliaBrush); }
		set { PanaZilliaBrush = Serialize.StringToBrush(value); }
	}

	[Display(Name = "KingZilla Color", Order = 1, GroupName = "Visuals")]
	[XmlIgnore]
	public Brush KingZillaBrush { get; set; }
	[Browsable(false)]
	public string KingZillaBrushSerialize
	{
		get { return Serialize.BrushToString(KingZillaBrush); }
		set { KingZillaBrush = Serialize.StringToBrush(value); }
	}

	[Display(Name = "KingPana Color", Order = 2, GroupName = "Visuals")]
	[XmlIgnore]
	public Brush KingPanaBrush { get; set; }
	[Browsable(false)]
	public string KingPanaBrushSerialize
	{
		get { return Serialize.BrushToString(KingPanaBrush); }
		set { KingPanaBrush = Serialize.StringToBrush(value); }
	}

	[Display(Name = "Arrow Offset (Ticks)", Order = 3, GroupName = "Visuals")]
	[Range(0, int.MaxValue)]
	public int ArrowOffset { get; set; }

	// ---- Display properties ---------------------------------
	[Display(Name = "Show KingOrderBlock", Order = 0, GroupName = "Display",
		Description = "Display gbKingOrderBlock order block zones, BOS/CHoCH levels, and imbalance zones on the chart.")]
	public bool ShowKingOrderBlock { get; set; }

	[Display(Name = "Show PANAKanal", Order = 1, GroupName = "Display",
		Description = "Display gbPANAKanal channel bands, trend bar colouring, and Fibonacci pullback zones on the chart.")]
	public bool ShowPANAKanal { get; set; }

	[Display(Name = "Show ThunderZilla", Order = 2, GroupName = "Display",
		Description = "Display gbThunderZilla SolarWind/Sumo cloud, trend MA, and trailing stop on the chart.")]
	public bool ShowThunderZilla { get; set; }

	// ---- Logging properties ---------------------------------
	[Display(Name = "Enabled", Order = 0, GroupName = "Logging",
		Description = "Write a CSV signal log to the NinjaTrader user data folder. "
		            + "File is named gbKPZlog_YYYYMMDD_HHmmss.csv and created when the indicator loads. "
		            + "One row is written per bar on which at least one trade signal fires.")]
	public bool LogEnabled { get; set; }

	#endregion

} // class gbKingPanaZilla


} // namespace NinjaTrader.NinjaScript.Indicators.GreyBeard.KingPanaZilla

#region NinjaScript generated code. Neither change nor remove.

// Consolidated generated code for the entire KingPanaZilla suite.
// All four indicators' factory methods live here so NT8's compiler
// finds a complete section and never appends a duplicate block.
// The child indicator files (gbKingOrderBlock, gbPANAKanal,
// gbThunderZilla) carry no generated-code region of their own.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		// ---- gbKingOrderBlock ----------------------------------
		private GreyBeard.KingPanaZilla.gbKingOrderBlock[] cachegbKingOrderBlock;
		public GreyBeard.KingPanaZilla.gbKingOrderBlock gbKingOrderBlock(int swingPointNeighborhood, int imbalanceQualifying, int orderBlockFindingBosChochPeriod, int orderBlockAge, int orderBlocksSameDirectionOffset, int orderBlocksDifferenceDirectionOffset, int signalTradeQuantityPerOrderBlock, int signalTradeSplitBars)
		{
			return gbKingOrderBlock(Input, swingPointNeighborhood, imbalanceQualifying, orderBlockFindingBosChochPeriod, orderBlockAge, orderBlocksSameDirectionOffset, orderBlocksDifferenceDirectionOffset, signalTradeQuantityPerOrderBlock, signalTradeSplitBars);
		}
		public GreyBeard.KingPanaZilla.gbKingOrderBlock gbKingOrderBlock(ISeries<double> input, int swingPointNeighborhood, int imbalanceQualifying, int orderBlockFindingBosChochPeriod, int orderBlockAge, int orderBlocksSameDirectionOffset, int orderBlocksDifferenceDirectionOffset, int signalTradeQuantityPerOrderBlock, int signalTradeSplitBars)
		{
			if (cachegbKingOrderBlock != null)
				for (int idx = 0; idx < cachegbKingOrderBlock.Length; idx++)
					if (cachegbKingOrderBlock[idx] != null && cachegbKingOrderBlock[idx].SwingPointNeighborhood == swingPointNeighborhood && cachegbKingOrderBlock[idx].ImbalanceQualifying == imbalanceQualifying && cachegbKingOrderBlock[idx].OrderBlockFindingBosChochPeriod == orderBlockFindingBosChochPeriod && cachegbKingOrderBlock[idx].OrderBlockAge == orderBlockAge && cachegbKingOrderBlock[idx].OrderBlocksSameDirectionOffset == orderBlocksSameDirectionOffset && cachegbKingOrderBlock[idx].OrderBlocksDifferenceDirectionOffset == orderBlocksDifferenceDirectionOffset && cachegbKingOrderBlock[idx].SignalTradeQuantityPerOrderBlock == signalTradeQuantityPerOrderBlock && cachegbKingOrderBlock[idx].SignalTradeSplitBars == signalTradeSplitBars && cachegbKingOrderBlock[idx].EqualsInput(input))
						return cachegbKingOrderBlock[idx];
			return CacheIndicator<GreyBeard.KingPanaZilla.gbKingOrderBlock>(new GreyBeard.KingPanaZilla.gbKingOrderBlock(){ SwingPointNeighborhood = swingPointNeighborhood, ImbalanceQualifying = imbalanceQualifying, OrderBlockFindingBosChochPeriod = orderBlockFindingBosChochPeriod, OrderBlockAge = orderBlockAge, OrderBlocksSameDirectionOffset = orderBlocksSameDirectionOffset, OrderBlocksDifferenceDirectionOffset = orderBlocksDifferenceDirectionOffset, SignalTradeQuantityPerOrderBlock = signalTradeQuantityPerOrderBlock, SignalTradeSplitBars = signalTradeSplitBars }, input, ref cachegbKingOrderBlock);
		}

		// ---- gbPANAKanal ---------------------------------------
		private GreyBeard.KingPanaZilla.gbPANAKanal[] cachegbPANAKanal;
		public GreyBeard.KingPanaZilla.gbPANAKanal gbPANAKanal(int period, double factor, int middlePeriod, int signalBreakSplit, int signalPullbackFindingPeriod)
		{
			return gbPANAKanal(Input, period, factor, middlePeriod, signalBreakSplit, signalPullbackFindingPeriod);
		}
		public GreyBeard.KingPanaZilla.gbPANAKanal gbPANAKanal(ISeries<double> input, int period, double factor, int middlePeriod, int signalBreakSplit, int signalPullbackFindingPeriod)
		{
			if (cachegbPANAKanal != null)
				for (int idx = 0; idx < cachegbPANAKanal.Length; idx++)
					if (cachegbPANAKanal[idx].Period == period && cachegbPANAKanal[idx].Factor == factor && cachegbPANAKanal[idx].MiddlePeriod == middlePeriod && cachegbPANAKanal[idx].SignalBreakSplit == signalBreakSplit && cachegbPANAKanal[idx].SignalPullbackFindingPeriod == signalPullbackFindingPeriod && cachegbPANAKanal[idx].EqualsInput(input))
						return cachegbPANAKanal[idx];
			return CacheIndicator<GreyBeard.KingPanaZilla.gbPANAKanal>(new GreyBeard.KingPanaZilla.gbPANAKanal(){ Period = period, Factor = factor, MiddlePeriod = middlePeriod, SignalBreakSplit = signalBreakSplit, SignalPullbackFindingPeriod = signalPullbackFindingPeriod }, input, ref cachegbPANAKanal);
		}

		// ---- gbThunderZilla ------------------------------------
		private GreyBeard.KingPanaZilla.gbThunderZilla[] cachegbThunderZilla;
		public GreyBeard.KingPanaZilla.gbThunderZilla gbThunderZilla(global::gbThunderZillaMAType trendMAType, int trendPeriod, bool trendSmoothingEnabled, global::gbThunderZillaMAType trendSmoothingMethod, int trendSmoothingPeriod, double stopOffsetMultiplierStop, int signalQuantityPerFlat, int signalQuantityPerTrend)
		{
			return gbThunderZilla(Input, trendMAType, trendPeriod, trendSmoothingEnabled, trendSmoothingMethod, trendSmoothingPeriod, stopOffsetMultiplierStop, signalQuantityPerFlat, signalQuantityPerTrend);
		}
		public GreyBeard.KingPanaZilla.gbThunderZilla gbThunderZilla(ISeries<double> input, global::gbThunderZillaMAType trendMAType, int trendPeriod, bool trendSmoothingEnabled, global::gbThunderZillaMAType trendSmoothingMethod, int trendSmoothingPeriod, double stopOffsetMultiplierStop, int signalQuantityPerFlat, int signalQuantityPerTrend)
		{
			if (cachegbThunderZilla != null)
				for (int idx = 0; idx < cachegbThunderZilla.Length; idx++)
					if (cachegbThunderZilla[idx] != null && cachegbThunderZilla[idx].TrendMAType == trendMAType && cachegbThunderZilla[idx].TrendPeriod == trendPeriod && cachegbThunderZilla[idx].TrendSmoothingEnabled == trendSmoothingEnabled && cachegbThunderZilla[idx].TrendSmoothingMethod == trendSmoothingMethod && cachegbThunderZilla[idx].TrendSmoothingPeriod == trendSmoothingPeriod && cachegbThunderZilla[idx].StopOffsetMultiplierStop == stopOffsetMultiplierStop && cachegbThunderZilla[idx].SignalQuantityPerFlat == signalQuantityPerFlat && cachegbThunderZilla[idx].SignalQuantityPerTrend == signalQuantityPerTrend && cachegbThunderZilla[idx].EqualsInput(input))
						return cachegbThunderZilla[idx];
			return CacheIndicator<GreyBeard.KingPanaZilla.gbThunderZilla>(new GreyBeard.KingPanaZilla.gbThunderZilla(){ TrendMAType = trendMAType, TrendPeriod = trendPeriod, TrendSmoothingEnabled = trendSmoothingEnabled, TrendSmoothingMethod = trendSmoothingMethod, TrendSmoothingPeriod = trendSmoothingPeriod, StopOffsetMultiplierStop = stopOffsetMultiplierStop, SignalQuantityPerFlat = signalQuantityPerFlat, SignalQuantityPerTrend = signalQuantityPerTrend }, input, ref cachegbThunderZilla);
		}

		// ---- gbKingPanaZilla -----------------------------------
		private GreyBeard.KingPanaZilla.gbKingPanaZilla[] cachegbKingPanaZilla;
		public GreyBeard.KingPanaZilla.gbKingPanaZilla gbKingPanaZilla()
		{
			return gbKingPanaZilla(Input);
		}
		public GreyBeard.KingPanaZilla.gbKingPanaZilla gbKingPanaZilla(ISeries<double> input)
		{
			if (cachegbKingPanaZilla != null)
				for (int idx = 0; idx < cachegbKingPanaZilla.Length; idx++)
					if (cachegbKingPanaZilla[idx] != null && cachegbKingPanaZilla[idx].EqualsInput(input))
						return cachegbKingPanaZilla[idx];
			return CacheIndicator<GreyBeard.KingPanaZilla.gbKingPanaZilla>(new GreyBeard.KingPanaZilla.gbKingPanaZilla(){}, input, ref cachegbKingPanaZilla);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		// ---- gbKingOrderBlock ----------------------------------
		public Indicators.GreyBeard.KingPanaZilla.gbKingOrderBlock gbKingOrderBlock(int swingPointNeighborhood, int imbalanceQualifying, int orderBlockFindingBosChochPeriod, int orderBlockAge, int orderBlocksSameDirectionOffset, int orderBlocksDifferenceDirectionOffset, int signalTradeQuantityPerOrderBlock, int signalTradeSplitBars)
		{
			return indicator.gbKingOrderBlock(Input, swingPointNeighborhood, imbalanceQualifying, orderBlockFindingBosChochPeriod, orderBlockAge, orderBlocksSameDirectionOffset, orderBlocksDifferenceDirectionOffset, signalTradeQuantityPerOrderBlock, signalTradeSplitBars);
		}
		public Indicators.GreyBeard.KingPanaZilla.gbKingOrderBlock gbKingOrderBlock(ISeries<double> input, int swingPointNeighborhood, int imbalanceQualifying, int orderBlockFindingBosChochPeriod, int orderBlockAge, int orderBlocksSameDirectionOffset, int orderBlocksDifferenceDirectionOffset, int signalTradeQuantityPerOrderBlock, int signalTradeSplitBars)
		{
			return indicator.gbKingOrderBlock(input, swingPointNeighborhood, imbalanceQualifying, orderBlockFindingBosChochPeriod, orderBlockAge, orderBlocksSameDirectionOffset, orderBlocksDifferenceDirectionOffset, signalTradeQuantityPerOrderBlock, signalTradeSplitBars);
		}

		// ---- gbPANAKanal ---------------------------------------
		public Indicators.GreyBeard.KingPanaZilla.gbPANAKanal gbPANAKanal(int period, double factor, int middlePeriod, int signalBreakSplit, int signalPullbackFindingPeriod)
		{
			return indicator.gbPANAKanal(Input, period, factor, middlePeriod, signalBreakSplit, signalPullbackFindingPeriod);
		}
		public Indicators.GreyBeard.KingPanaZilla.gbPANAKanal gbPANAKanal(ISeries<double> input, int period, double factor, int middlePeriod, int signalBreakSplit, int signalPullbackFindingPeriod)
		{
			return indicator.gbPANAKanal(input, period, factor, middlePeriod, signalBreakSplit, signalPullbackFindingPeriod);
		}

		// ---- gbThunderZilla ------------------------------------
		public NinjaTrader.NinjaScript.Indicators.GreyBeard.KingPanaZilla.gbThunderZilla gbThunderZilla(global::gbThunderZillaMAType trendMAType, int trendPeriod, bool trendSmoothingEnabled, global::gbThunderZillaMAType trendSmoothingMethod, int trendSmoothingPeriod, double stopOffsetMultiplierStop, int signalQuantityPerFlat, int signalQuantityPerTrend)
		{
			return indicator.gbThunderZilla(Input, trendMAType, trendPeriod, trendSmoothingEnabled, trendSmoothingMethod, trendSmoothingPeriod, stopOffsetMultiplierStop, signalQuantityPerFlat, signalQuantityPerTrend);
		}
		public NinjaTrader.NinjaScript.Indicators.GreyBeard.KingPanaZilla.gbThunderZilla gbThunderZilla(ISeries<double> input, global::gbThunderZillaMAType trendMAType, int trendPeriod, bool trendSmoothingEnabled, global::gbThunderZillaMAType trendSmoothingMethod, int trendSmoothingPeriod, double stopOffsetMultiplierStop, int signalQuantityPerFlat, int signalQuantityPerTrend)
		{
			return indicator.gbThunderZilla(input, trendMAType, trendPeriod, trendSmoothingEnabled, trendSmoothingMethod, trendSmoothingPeriod, stopOffsetMultiplierStop, signalQuantityPerFlat, signalQuantityPerTrend);
		}

		// ---- gbKingPanaZilla -----------------------------------
		public Indicators.GreyBeard.KingPanaZilla.gbKingPanaZilla gbKingPanaZilla()
		{
			return indicator.gbKingPanaZilla(Input);
		}
		public Indicators.GreyBeard.KingPanaZilla.gbKingPanaZilla gbKingPanaZilla(ISeries<double> input)
		{
			return indicator.gbKingPanaZilla(input);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		// ---- gbKingOrderBlock ----------------------------------
		public Indicators.GreyBeard.KingPanaZilla.gbKingOrderBlock gbKingOrderBlock(int swingPointNeighborhood, int imbalanceQualifying, int orderBlockFindingBosChochPeriod, int orderBlockAge, int orderBlocksSameDirectionOffset, int orderBlocksDifferenceDirectionOffset, int signalTradeQuantityPerOrderBlock, int signalTradeSplitBars)
		{
			return indicator.gbKingOrderBlock(Input, swingPointNeighborhood, imbalanceQualifying, orderBlockFindingBosChochPeriod, orderBlockAge, orderBlocksSameDirectionOffset, orderBlocksDifferenceDirectionOffset, signalTradeQuantityPerOrderBlock, signalTradeSplitBars);
		}
		public Indicators.GreyBeard.KingPanaZilla.gbKingOrderBlock gbKingOrderBlock(ISeries<double> input, int swingPointNeighborhood, int imbalanceQualifying, int orderBlockFindingBosChochPeriod, int orderBlockAge, int orderBlocksSameDirectionOffset, int orderBlocksDifferenceDirectionOffset, int signalTradeQuantityPerOrderBlock, int signalTradeSplitBars)
		{
			return indicator.gbKingOrderBlock(input, swingPointNeighborhood, imbalanceQualifying, orderBlockFindingBosChochPeriod, orderBlockAge, orderBlocksSameDirectionOffset, orderBlocksDifferenceDirectionOffset, signalTradeQuantityPerOrderBlock, signalTradeSplitBars);
		}

		// ---- gbPANAKanal ---------------------------------------
		public Indicators.GreyBeard.KingPanaZilla.gbPANAKanal gbPANAKanal(int period, double factor, int middlePeriod, int signalBreakSplit, int signalPullbackFindingPeriod)
		{
			return indicator.gbPANAKanal(Input, period, factor, middlePeriod, signalBreakSplit, signalPullbackFindingPeriod);
		}
		public Indicators.GreyBeard.KingPanaZilla.gbPANAKanal gbPANAKanal(ISeries<double> input, int period, double factor, int middlePeriod, int signalBreakSplit, int signalPullbackFindingPeriod)
		{
			return indicator.gbPANAKanal(input, period, factor, middlePeriod, signalBreakSplit, signalPullbackFindingPeriod);
		}

		// ---- gbThunderZilla ------------------------------------
		public NinjaTrader.NinjaScript.Indicators.GreyBeard.KingPanaZilla.gbThunderZilla gbThunderZilla(global::gbThunderZillaMAType trendMAType, int trendPeriod, bool trendSmoothingEnabled, global::gbThunderZillaMAType trendSmoothingMethod, int trendSmoothingPeriod, double stopOffsetMultiplierStop, int signalQuantityPerFlat, int signalQuantityPerTrend)
		{
			return indicator.gbThunderZilla(Input, trendMAType, trendPeriod, trendSmoothingEnabled, trendSmoothingMethod, trendSmoothingPeriod, stopOffsetMultiplierStop, signalQuantityPerFlat, signalQuantityPerTrend);
		}
		public NinjaTrader.NinjaScript.Indicators.GreyBeard.KingPanaZilla.gbThunderZilla gbThunderZilla(ISeries<double> input, global::gbThunderZillaMAType trendMAType, int trendPeriod, bool trendSmoothingEnabled, global::gbThunderZillaMAType trendSmoothingMethod, int trendSmoothingPeriod, double stopOffsetMultiplierStop, int signalQuantityPerFlat, int signalQuantityPerTrend)
		{
			return indicator.gbThunderZilla(input, trendMAType, trendPeriod, trendSmoothingEnabled, trendSmoothingMethod, trendSmoothingPeriod, stopOffsetMultiplierStop, signalQuantityPerFlat, signalQuantityPerTrend);
		}

		// ---- gbKingPanaZilla -----------------------------------
		public Indicators.GreyBeard.KingPanaZilla.gbKingPanaZilla gbKingPanaZilla()
		{
			return indicator.gbKingPanaZilla(Input);
		}
		public Indicators.GreyBeard.KingPanaZilla.gbKingPanaZilla gbKingPanaZilla(ISeries<double> input)
		{
			return indicator.gbKingPanaZilla(input);
		}
	}
}

#endregion
