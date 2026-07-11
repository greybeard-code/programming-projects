#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.NinjaScript;
using NinjaTrader.Gui.Tools;
using NinjaTrader.NinjaScript.DrawingTools;
using SharpDX;
using SharpDX.Direct2D1;
using Brush = System.Windows.Media.Brush;
using SolidColorBrush = System.Windows.Media.SolidColorBrush;
using Color = System.Windows.Media.Color;
#endregion

namespace NinjaTrader.NinjaScript.Indicators
{
[CategoryOrder("Critical", 1000070)]
[CategoryOrder("Special", 1000060)]
[CategoryOrder("Alerts", 1000040)]
[CategoryOrder("Control Panel", 1000030)]
[CategoryOrder("Graphics", 1000020)]
[CategoryOrder("General", 1000010)]
[CategoryOrder("SL/TP Movement", 1000009)]
[CategoryOrder("Developer", 0)]
public class ninZaATRTradeShield : Indicator
{
	private class GeneralInfo
	{
		public bool IsLevelsInitialized { get; set; }

		public int InitCustomStopOrdersCount { get; set; }

		public int InitCustomLimitOrdersCount { get; set; }

		public int PrevOrderQuantity { get; set; }

		public double AtrStop1 { get; set; }

		public double AtrStop2 { get; set; }

		public double AtrStop3 { get; set; }

		public double AtrTarget1 { get; set; }

		public double AtrTarget2 { get; set; }

		public double AtrTarget3 { get; set; }

		public SortedList<int, StopLevelInfo> ListPlotInfo { get; set; }

		public GeneralInfo(int prevOrderQuantity)
		{
			IsLevelsInitialized = false;
			InitCustomStopOrdersCount = 0;
			InitCustomLimitOrdersCount = 0;
			PrevOrderQuantity = prevOrderQuantity;
			AtrStop1 = double.NaN;
			AtrStop2 = double.NaN;
			AtrStop3 = double.NaN;
			AtrTarget1 = double.NaN;
			AtrTarget2 = double.NaN;
			AtrTarget3 = double.NaN;
			ListPlotInfo = new SortedList<int, StopLevelInfo>();
		}

		}

	private class StopLevelInfo
	{
		public double Stop1 { get; set; }

		public double Stop2 { get; set; }

		public double Stop3 { get; set; }

		public double Target1 { get; set; }

		public double Target2 { get; set; }

		public double Target3 { get; set; }

		public StopLevelInfo(double stop1, double stop2, double stop3, double target1, double target2, double target3)
		{
			Stop1 = stop1;
			Stop2 = stop2;
			Stop3 = stop3;
			Target1 = target1;
			Target2 = target2;
			Target3 = target3;
		}

		}

	private class LevelInfo
	{
		public double StopPrice { get; set; }

		public double TargetPrice { get; set; }

		public double StopAtrInfoPrefix { get; set; }

		public double TargetAtrInfoPrefix { get; set; }

		public LevelInfo(double stopPrice, double targetPrice, double stopAtrInfoPrefix, double targetAtrInfoPrefix)
		{
			StopPrice = stopPrice;
			TargetPrice = targetPrice;
			StopAtrInfoPrefix = stopAtrInfoPrefix;
			TargetAtrInfoPrefix = targetAtrInfoPrefix;
		}

		}

	private class DraggablePanel : Grid
	{
		public Grid gridButtons;

		public Button hideFocus = new Button
		{
			MinWidth = 0.0,
			Width = 0.0,
			Height = 0.0
		};

		public Thumb drag;

		public Button btnMini;

		public Button buttonOnOff;

		public Button buttonTarget;

		public Button buttonStop;

		public Button btnMergeStop1;

		public Button btnMergeStop2;

		public Button btnMergeStop3;

		public List<Button> listMergeStopBtn;

		public Button btnMergeTarget1;

		public Button btnMergeTarget2;

		public Button btnMergeTarget3;

		public List<Button> listMergeTargetBtn;

		public TextPosition alignment;

		public DraggablePanel(bool minimized, int minimumButtonWidth, bool switchedOn, Brush switchActive, Brush switchInactive, bool stoplossEnabled, Brush stopActive, Brush stopInactive, bool targetEnabled, Brush targetActive, Brush targetInactive, byte sLMergedIndex, byte tPMergedIndex, double textExecutionSize, Brush textExecutionBrush, Brush dragBrush, TextPosition alignment, Thickness thickness)
		{
			this.alignment = alignment;
			SetPosition(thickness);
			drag = new Thumb
			{
				Width = 6.0,
				Cursor = Cursors.SizeAll
			};
			FrameworkElementFactory frameworkElementFactory = new FrameworkElementFactory(typeof(Border));
			frameworkElementFactory.SetValue(Border.BackgroundProperty, dragBrush);
			drag.Template = new ControlTemplate(typeof(Thumb))
			{
				VisualTree = frameworkElementFactory
			};
			drag.SetValue(Grid.ColumnProperty, 0);
			base.Children.Add(drag);
			base.Children.Add(hideFocus);
			hideFocus.SetValue(Grid.ColumnProperty, 1);
			btnMini = CreateButton("ATR-TradeShield", "mini", textExecutionSize, dragBrush, textExecutionBrush, this, minimumButtonWidth, 1);
			btnMini.Margin = new Thickness(1.0, 0.0, 0.0, 0.0);
			btnMini.ToolTip = "  Click to restore.";
			gridButtons = new Grid();
			base.Children.Add(gridButtons);
			gridButtons.RowDefinitions.Add(new RowDefinition());
			buttonOnOff = CreateButton("Info", "on-off", textExecutionSize, (!switchedOn) ? switchInactive : switchActive, textExecutionBrush, gridButtons, minimumButtonWidth, 0, 6);
			buttonOnOff.Margin = new Thickness(1.0, 0.0, 0.0, 0.0);
			gridButtons.RowDefinitions.Add(new RowDefinition());
			buttonStop = CreateButton("Stop", "stop", textExecutionSize, (!stoplossEnabled) ? stopInactive : stopActive, textExecutionBrush, gridButtons, minimumButtonWidth, 0, 3, gridButtons.RowDefinitions.Count - 1);
			buttonStop.Margin = new Thickness(1.0, 1.0, 0.0, 0.0);
			buttonTarget = CreateButton("Target", "target", textExecutionSize, (!targetEnabled) ? targetInactive : targetActive, textExecutionBrush, gridButtons, minimumButtonWidth + 1, 3, 3, gridButtons.RowDefinitions.Count - 1);
			buttonTarget.Margin = new Thickness(1.0, 1.0, 0.0, 0.0);
			int num = Math.Max(minimumButtonWidth, Convert.ToInt32(buttonTarget.Width));
			buttonTarget.Width = (buttonStop.Width = num);
			buttonOnOff.Width = num * 2;
			gridButtons.RowDefinitions.Add(new RowDefinition());
			int num3 = num / 3;
			btnMergeStop1 = CreateButton("1", "1", 11.0, (sLMergedIndex != 1) ? stopInactive : stopActive, textExecutionBrush, gridButtons, num3, 0, 0, 2);
			btnMergeStop2 = CreateButton("2", "2", 11.0, (sLMergedIndex != 2) ? stopInactive : stopActive, textExecutionBrush, gridButtons, num3, 1, 0, 2);
			btnMergeStop3 = CreateButton("3", "3", 11.0, (sLMergedIndex != 3) ? stopInactive : stopActive, textExecutionBrush, gridButtons, num3, 2, 0, 2);
			btnMergeTarget1 = CreateButton("1", "1", 11.0, (tPMergedIndex != 1) ? targetInactive : targetActive, textExecutionBrush, gridButtons, num3, 3, 0, 2);
			btnMergeTarget2 = CreateButton("2", "2", 11.0, (tPMergedIndex != 2) ? targetInactive : targetActive, textExecutionBrush, gridButtons, num3, 4, 0, 2);
			btnMergeTarget3 = CreateButton("3", "3", 11.0, (tPMergedIndex != 3) ? targetInactive : targetActive, textExecutionBrush, gridButtons, num3, 5, 0, 2);
			btnMergeStop1.Margin = (btnMergeStop2.Margin = (btnMergeStop3.Margin = (btnMergeTarget1.Margin = (btnMergeTarget2.Margin = (btnMergeTarget3.Margin = new Thickness(1.0, 1.0, 0.0, 0.0))))));
			Button button = btnMergeStop1;
			Button button2 = btnMergeStop2;
			Button button3 = btnMergeStop3;
			Button button4 = btnMergeTarget1;
			Button button5 = btnMergeTarget2;
			btnMergeTarget3.VerticalAlignment = VerticalAlignment.Top;
			button5.VerticalAlignment = VerticalAlignment.Top;
			button4.VerticalAlignment = VerticalAlignment.Top;
			button3.VerticalAlignment = VerticalAlignment.Top;
			button2.VerticalAlignment = VerticalAlignment.Top;
			button.VerticalAlignment = VerticalAlignment.Top;
			listMergeStopBtn = new List<Button>(3) { btnMergeStop1, btnMergeStop2, btnMergeStop3 };
			listMergeTargetBtn = new List<Button>(3) { btnMergeTarget1, btnMergeTarget2, btnMergeTarget3 };
			gridButtons.ColumnDefinitions.Add(new ColumnDefinition
			{
				Width = new GridLength(num3)
			});
			gridButtons.ColumnDefinitions.Add(new ColumnDefinition
			{
				Width = new GridLength(num3)
			});
			gridButtons.ColumnDefinitions.Add(new ColumnDefinition
			{
				Width = new GridLength(num3)
			});
			gridButtons.ColumnDefinitions.Add(new ColumnDefinition
			{
				Width = new GridLength(num3)
			});
			gridButtons.ColumnDefinitions.Add(new ColumnDefinition
			{
				Width = new GridLength(num3)
			});
			gridButtons.ColumnDefinitions.Add(new ColumnDefinition
			{
				Width = new GridLength(num3)
			});
			gridButtons.SetValue(Grid.ColumnProperty, 2);
			base.ColumnDefinitions.Add(new ColumnDefinition
			{
				Width = default(GridLength)
			});
			base.ColumnDefinitions.Add(new ColumnDefinition
			{
				Width = default(GridLength)
			});
			base.ColumnDefinitions.Add(new ColumnDefinition
			{
				Width = default(GridLength)
			});
			base.RowDefinitions.Add(new RowDefinition
			{
				Height = GridLength.Auto
			});
			SetState(minimized);
		}

		public void SetState(bool minimized)
		{
			if (!minimized)
			{
				gridButtons.Visibility = Visibility.Visible;
				btnMini.Visibility = Visibility.Collapsed;
				double height = (drag.Height = buttonOnOff.Height * 3.0);
				base.Height = height;
			}
			else
			{
				gridButtons.Visibility = Visibility.Collapsed;
				btnMini.Visibility = Visibility.Visible;
				double height = (drag.Height = buttonOnOff.Height);
				base.Height = height;
			}
			base.ColumnDefinitions[1].Width = new GridLength((!minimized) ? 0.0 : btnMini.Width);
			drag.ToolTip = "  Drag to anywhere; double click to " + ((!minimized) ? "minimize" : "restore") + ".";
		}

		public void SetButtonBackground(Button button, Brush brush)
		{
			if (button != null)
			{
				Brush background = (button.BorderBrush = brush);
				button.Background = background;
			}
		}

		public Button CreateButton(string text, string tag, double textSize, Brush back, Brush fore, Grid grid, int buttonWidth, int column = 0, int columnSpan = 0, int row = 0)
		{
			Button button = new Button
			{
				Content = text,
				MinWidth = 0.0
			};
			button.Foreground = fore;
			Brush background = (button.BorderBrush = back);
			button.Background = background;
			button.HorizontalAlignment = HorizontalAlignment.Left;
			button.FontSize = textSize;
			button.Cursor = Cursors.Hand;
			button.Tag = tag;
			button.ToolTip = null;
			button.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
			Size size = button.DesiredSize;
			button.Width = Math.Max(Convert.ToInt32(size.Width + 3.0 * button.Padding.Left), buttonWidth);
			button.Height = Convert.ToInt32(size.Height + 3.0 * button.Padding.Top);
			if (row > 0)
			{
				button.SetValue(Grid.RowProperty, row);
			}
			if (column > 0)
			{
				button.SetValue(Grid.ColumnProperty, column);
			}
			if (columnSpan > 0)
			{
				button.SetValue(Grid.ColumnSpanProperty, columnSpan);
			}
			grid?.Children.Add(button);
			return button;
		}

		private void SetPosition(Thickness thickness)
		{
			if (alignment != TextPosition.Center)
			{
				if (alignment == TextPosition.TopLeft || alignment == TextPosition.BottomLeft)
				{
					base.HorizontalAlignment = HorizontalAlignment.Left;
				}
				else
				{
					base.HorizontalAlignment = HorizontalAlignment.Right;
				}
				if (alignment == TextPosition.TopLeft || alignment == TextPosition.TopRight)
				{
					base.VerticalAlignment = VerticalAlignment.Top;
				}
				else
				{
					base.VerticalAlignment = VerticalAlignment.Bottom;
				}
				base.Margin = thickness;
			}
			else
			{
				base.HorizontalAlignment = HorizontalAlignment.Center;
				base.VerticalAlignment = VerticalAlignment.Center;
			}
		}

		}

	private TextPosition controlPanelPositionAlignment;

	private const int defaultMargin = 5;

	private const string toolTipSpace = "  ";

	private double atrTarget1InitValue;

	private double atrTarget2InitValue;

	private double atrTarget3InitValue;

	private bool isValidTab;

	private TabControl tabControl;

	private bool isUnitATR;

	private double positionPrice = double.NaN;

	private bool? isLong;

	private double levelWidthPercent;

	private string atrInfoSuffix;

	private Brush chartBackground;

	private List<double> listAtrStopPrice;

	private List<double> listAtrTargetPrice;

	private List<Order> listCustomStopOrders;

	private int currentCustomStopOrdersCount;

	private List<Order> listCustomLimitOrders;

	private int currentCustomLimitOrdersCount;

	private double atrValue = double.NaN;

	private int historicalLastBar;

	private Timer timerOrdersUpdate;

	private bool isTimerStopped = true;

	private const string nickname = "atrtradeshield:exc";

	private Window alertWindow;

	private const string prefix = "ninZaATRTradeShield";

	private const string indicatorName = "ATR-TradeShield";

	private const string indicatorNameFull = "ATR-TradeShield by ninZa.co";

	private bool isCharting;

	private static Dictionary<Instrument, GeneralInfo> dictInstrumentGeneralInfo;

	private List<LevelInfo> listLevelInfo;

	private int countTimer;

	private ChartTrader chartTrader;

	private Account activeAccount;

	private bool chartTraderAvailable;

	private BindingFlags flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

	private AccountSelector chartTraderAccountSelector;

	private DraggablePanel controlPanel;

	[Display(Name = "Website", Order = 0, GroupName = "Developer")]
	public string Website => "ninZa.co";

	[Display(Name = "Update", Order = 10, GroupName = "Developer")]
	public string Update => "20 Jun 2025";

	[Display(Name = "Logo: Enabled", Order = 20, GroupName = "Developer")]
	public bool LogoEnabled { get; set; }

	[Display(Name = "Instruction: Enabled", Order = 30, GroupName = "Developer")]
	public bool InstructionEnabled { get; set; }

	[Display(Name = "Auto Scale", Order = 0, GroupName = "General")]
	public bool AutoScale { get; set; }

	[Display(Name = "Screen DPI", Order = 100, GroupName = "General")]
	public int ScreenDPI { get; set; }

	[Display(Name = "Level: Width (%)", Order = 10, GroupName = "Graphics")]
	public double LevelWidth { get; set; }

	[Display(Name = "Level Stop: Style", Order = 12, GroupName = "Graphics")]
	public Stroke LevelStopStyle { get; set; }

	[Display(Name = "Level Target: Style", Order = 14, GroupName = "Graphics")]
	public Stroke LevelTargetStyle { get; set; }

	[Display(Name = "Info: Enabled", Order = 20, GroupName = "Graphics")]
	public bool InfoEnabled { get; set; }

	[Display(Name = "Info: Font", Order = 22, GroupName = "Graphics")]
	public SimpleFont InfoFont { get; set; }

	[Display(Name = "Info: Margin", Order = 24, GroupName = "Graphics")]
	public int InfoMargin { get; set; }

	[Display(Name = "Smart Mode: Enabled", Order = 0, GroupName = "Parameters")]
	[NinjaScriptProperty]
	public bool SmartModeEnabled { get; set; }

	[Display(Name = "Unit", Order = 2, GroupName = "Parameters")]
	public ninZaATRTradeShield_Unit Unit { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "Period", Order = 4, GroupName = "Parameters")]
	public int Period { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "Level #1: Stop Multiplier", Order = 10, GroupName = "Parameters")]
	public double Level1StopMultiplier { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "Level #1: Target Multiplier", Order = 12, GroupName = "Parameters")]
	public double Level1TargetMultiplier { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "Level #2: Stop Multiplier", Order = 20, GroupName = "Parameters")]
	public double Level2StopMultiplier { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "Level #2: Target Multiplier", Order = 22, GroupName = "Parameters")]
	public double Level2TargetMultiplier { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "Level #3: Stop Multiplier", Order = 30, GroupName = "Parameters")]
	public double Level3StopMultiplier { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "Level #3: Target Multiplier", Order = 32, GroupName = "Parameters")]
	public double Level3TargetMultiplier { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "Stoploss: Enabled", Order = 40, GroupName = "SL/TP Movement")]
	public bool StoplossMovementEnabled { get; set; }

	[Display(Name = "Stop #1", Order = 42, GroupName = "SL/TP Movement")]
	public ninZaATRTradeShield_StoplossMovement StopOrder1 { get; set; }

	[Display(Name = "Stop #2", Order = 44, GroupName = "SL/TP Movement")]
	public ninZaATRTradeShield_StoplossMovement StopOrder2 { get; set; }

	[Display(Name = "Stop #3", Order = 46, GroupName = "SL/TP Movement")]
	public ninZaATRTradeShield_StoplossMovement StopOrder3 { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "Target: Enabled", Order = 60, GroupName = "SL/TP Movement")]
	public bool TargetMovementEnabled { get; set; }

	[Display(Name = "Target #1", Order = 62, GroupName = "SL/TP Movement")]
	public ninZaATRTradeShield_TargetMovement TargetOrder1 { get; set; }

	[Display(Name = "Target #2", Order = 64, GroupName = "SL/TP Movement")]
	public ninZaATRTradeShield_TargetMovement TargetOrder2 { get; set; }

	[Display(Name = "Target #3", Order = 66, GroupName = "SL/TP Movement")]
	public ninZaATRTradeShield_TargetMovement TargetOrder3 { get; set; }

	[Display(Name = "Minimized", Order = 0, GroupName = "Control Panel")]
	public bool CpMinimized { get; set; }

	[Display(Name = "Button: Width", Order = 10, GroupName = "Control Panel")]
	public int CpMinimumButtonWidth { get; set; }

	[XmlIgnore]
	[Display(Name = "Button Switch: Active", Order = 11, GroupName = "Control Panel")]
	public Brush CpButtonSwitchActive { get; set; }

	[Browsable(false)]
	public string CpButtonSwitchActive_Serialize
	{
		get
		{
			return Serialize.BrushToString(CpButtonSwitchActive);
		}
		set
		{
			CpButtonSwitchActive = Serialize.StringToBrush(value);
		}
	}

	[Display(Name = "Button Switch: Inactive", Order = 12, GroupName = "Control Panel")]
	[XmlIgnore]
	public Brush CpButtonSwitchInactive { get; set; }

	[Browsable(false)]
	public string CpButtonSwitchInactive_Serialize
	{
		get
		{
			return Serialize.BrushToString(CpButtonSwitchInactive);
		}
		set
		{
			CpButtonSwitchInactive = Serialize.StringToBrush(value);
		}
	}

	[XmlIgnore]
	[Display(Name = "Button Stop: Active", Order = 15, GroupName = "Control Panel")]
	public Brush CpButtonStopActive { get; set; }

	[Browsable(false)]
	public string CpButtonStopActive_Serialize
	{
		get
		{
			return Serialize.BrushToString(CpButtonStopActive);
		}
		set
		{
			CpButtonStopActive = Serialize.StringToBrush(value);
		}
	}

	[XmlIgnore]
	[Display(Name = "Button Stop: Inactive", Order = 16, GroupName = "Control Panel")]
	public Brush CpButtonStopInactive { get; set; }

	[Browsable(false)]
	public string CpButtonStopInactive_Serialize
	{
		get
		{
			return Serialize.BrushToString(CpButtonStopInactive);
		}
		set
		{
			CpButtonStopInactive = Serialize.StringToBrush(value);
		}
	}

	[XmlIgnore]
	[Display(Name = "Button Target: Active", Order = 17, GroupName = "Control Panel")]
	public Brush CpButtonTargetActive { get; set; }

	[Browsable(false)]
	public string CpButtonTargetActive_Serialize
	{
		get
		{
			return Serialize.BrushToString(CpButtonTargetActive);
		}
		set
		{
			CpButtonTargetActive = Serialize.StringToBrush(value);
		}
	}

	[XmlIgnore]
	[Display(Name = "Button Target: Inactive", Order = 18, GroupName = "Control Panel")]
	public Brush CpButtonTargetInactive { get; set; }

	[Browsable(false)]
	public string CpButtonTargetInactive_Serialize
	{
		get
		{
			return Serialize.BrushToString(CpButtonTargetInactive);
		}
		set
		{
			CpButtonTargetInactive = Serialize.StringToBrush(value);
		}
	}

	[Display(Name = "Text: Size", Order = 30, GroupName = "Control Panel")]
	public int CpTextSize { get; set; }

	[XmlIgnore]
	[Display(Name = "Text: Color", Order = 31, GroupName = "Control Panel")]
	public Brush CpTextBrush { get; set; }

	[Browsable(false)]
	public string CpTextBrush_Serialize
	{
		get
		{
			return Serialize.BrushToString(CpTextBrush);
		}
		set
		{
			CpTextBrush = Serialize.StringToBrush(value);
		}
	}

	[XmlIgnore]
	[Display(Name = "Drag Bar: Color", Order = 40, GroupName = "Control Panel")]
	public Brush CpDragBrush { get; set; }

	[Browsable(false)]
	public string CpDragBrush_Serialize
	{
		get
		{
			return Serialize.BrushToString(CpDragBrush);
		}
		set
		{
			CpDragBrush = Serialize.StringToBrush(value);
		}
	}

	[Display(Name = "Position: Alignment", Order = 50, GroupName = "Control Panel")]
	public TextPosition CpPositionAlignment
	{
		get
		{
			return controlPanelPositionAlignment;
		}
		set
		{
			if (value == TextPosition.TopLeft)
			{
				double cpPositionMarginTop = 5.0;
				CpPositionMarginLeft = 5.0;
				CpPositionMarginTop = cpPositionMarginTop;
			}
			if (value == TextPosition.TopRight)
			{
				double cpPositionMarginTop = 5.0;
				CpPositionMarginRight = 5.0;
				CpPositionMarginTop = cpPositionMarginTop;
			}
			if (value == TextPosition.BottomRight)
			{
				double cpPositionMarginTop = 5.0;
				CpPositionMarginRight = 5.0;
				CpPositionMarginBottom = cpPositionMarginTop;
			}
			if (value == TextPosition.BottomLeft)
			{
				double cpPositionMarginTop = 5.0;
				CpPositionMarginLeft = 5.0;
				CpPositionMarginBottom = cpPositionMarginTop;
			}
			if (value == TextPosition.Center)
			{
				double cpPositionMarginRight = 5.0;
				CpPositionMarginBottom = 5.0;
				double cpPositionMarginTop2 = 5.0;
				CpPositionMarginRight = cpPositionMarginRight;
				double cpPositionMarginTop = 5.0;
				CpPositionMarginTop = cpPositionMarginTop2;
				CpPositionMarginLeft = cpPositionMarginTop;
			}
			controlPanelPositionAlignment = value;
		}
	}

	[Display(Name = "Position: Margin Left", Order = 51, GroupName = "Control Panel")]
	public double CpPositionMarginLeft { get; set; }

	[Display(Name = "Position: Margin Top", Order = 52, GroupName = "Control Panel")]
	public double CpPositionMarginTop { get; set; }

	[Display(Name = "Position: Margin Right", Order = 53, GroupName = "Control Panel")]
	public double CpPositionMarginRight { get; set; }

	[Display(Name = "Position: Margin Bottom", Order = 54, GroupName = "Control Panel")]
	public double CpPositionMarginBottom { get; set; }

	[Display(Name = "Z Order", Order = 0, GroupName = "Special")]
	public int IndicatorZOrder { get; set; }

	[Display(Name = "User Note", Order = 10, GroupName = "Special")]
	public string UserNote { get; set; }

	[Display(Name = "Switched On", Order = 0, GroupName = "Critical")]
	public bool SwitchedOn { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "SL Merged Index", Order = 10, GroupName = "Critical")]
	public byte SLMergedIndex { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "TP Merged Index", Order = 20, GroupName = "Critical")]
	public byte TPMergedIndex { get; set; }

	[Browsable(false)]
	[XmlIgnore]
	public Series<double> Stop1 => Values[0];

	[Browsable(false)]
	[XmlIgnore]
	public Series<double> Stop2 => Values[1];

	[Browsable(false)]
	[XmlIgnore]
	public Series<double> Stop3 => Values[2];

	[Browsable(false)]
	[XmlIgnore]
	public Series<double> Target1 => Values[3];

	[XmlIgnore]
	[Browsable(false)]
	public Series<double> Target2 => Values[4];

	[XmlIgnore]
	[Browsable(false)]
	public Series<double> Target3 => Values[5];

	public override string DisplayName
	{
		get
		{
			if (!(Parent is MarketAnalyzerColumnBase))
			{
				return "ATR-TradeShield by ninZa.co" + GetUserNote();
			}
			return base.DisplayName;
		}
	}

	private string GetUserNote()
	{
		string text = (UserNote ?? string.Empty).Trim();
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
		if (State != State.SetDefaults)
		{
			if (State != State.Configure)
			{
				if (State != State.DataLoaded)
				{
					if (State != State.Historical)
					{
						if (State != State.Terminated)
						{
							return;
						}
						if (isCharting)
						{
							ChartControl.Dispatcher.InvokeAsync(delegate
							{
								if (chartTrader != null)
								{
									chartTrader = null;
								}
								if (chartTraderAccountSelector != null)
								{
									((Selector)(object)chartTraderAccountSelector).SelectionChanged -= OnChartTraderAccountChange;
									if (chartTraderAccountSelector.SelectedAccount != null)
									{
										chartTraderAccountSelector.SelectedAccount.PositionUpdate -= OnChartTraderPositionUpdate;
									}
									chartTraderAccountSelector = null;
								}
								if (controlPanel != null)
								{
									controlPanel.drag.DragDelta -= OnControlPanelDragDelta;
									controlPanel.btnMini.Click -= OnControlPanelBtnMiniClick;
									controlPanel.drag.MouseDoubleClick -= OnControlPanelDragDoubleClick;
									if (controlPanel.buttonStop != null)
									{
										controlPanel.buttonStop.Click -= OnButtonStopTargetClick;
									}
									if (controlPanel.buttonTarget != null)
									{
										controlPanel.buttonTarget.Click -= OnButtonStopTargetClick;
									}
									if (controlPanel.buttonOnOff != null)
									{
										controlPanel.buttonOnOff.Click -= OnButtonOnOffClick;
									}
									controlPanel = null;
								}
								if (alertWindow != null)
								{
									alertWindow.Close();
									alertWindow = null;
								}
							});
						}
						if (timerOrdersUpdate != null)
						{
							timerOrdersUpdate.Stop();
							timerOrdersUpdate.Elapsed -= OnTimerElapsed;
							timerOrdersUpdate.Dispose();
							timerOrdersUpdate = null;
						}
					}
					else
					{
						if (ScreenDPI < 100)
						{
							ScreenDPI = 96;
						}
						if (IndicatorZOrder != 0)
						{
							SetZOrder(IndicatorZOrder);
						}
						isCharting = ChartControl != null;
						if (isCharting)
						{
							chartBackground = ChartControl.Properties.ChartBackground;
						}
						if (isCharting)
						{
							ChartControl.Dispatcher.InvokeAsync(delegate
							{
								if (tabControl == null)
								{
									ref TabControl reference = ref tabControl;
									Window window = Window.GetWindow(((FrameworkElement)(object)ChartControl).Parent);
									reference = ((NTWindow)((window is Chart) ? window : null)).MainTabControl;
									tabControl.SelectionChanged += OnTabSwitch;
									ref bool reference2 = ref isValidTab;
									object content = (tabControl.SelectedItem as TabItem).Content;
									reference2 = ((ChartTab)((content is ChartTab) ? content : null)).ChartControl == ChartControl;
								}
								if (chartTrader == null)
								{
									ref ChartTrader reference3 = ref chartTrader;
									DependencyObject dependencyObject = Extensions.FindFirst((DependencyObject)Window.GetWindow(((FrameworkElement)(object)ChartControl).Parent), "ChartWindowChartTraderControl");
									reference3 = (ChartTrader)(object)((dependencyObject is ChartTrader) ? dependencyObject : null);
									chartTraderAvailable = chartTrader != null;
									if (chartTraderAvailable)
									{
										FieldInfo field = ((object)chartTrader).GetType().GetField("cbxAccounts", flags);
										if (field != null)
										{
											ref AccountSelector reference4 = ref chartTraderAccountSelector;
											object value = field.GetValue(chartTrader);
											reference4 = (AccountSelector)((value is AccountSelector) ? value : null);
											if (chartTraderAccountSelector != null)
											{
												((Selector)(object)chartTraderAccountSelector).SelectionChanged += OnChartTraderAccountChange;
												activeAccount = chartTraderAccountSelector.SelectedAccount;
												if (chartTraderAccountSelector.SelectedAccount != null)
												{
													chartTraderAccountSelector.SelectedAccount.PositionUpdate += OnChartTraderPositionUpdate;
													// Bootstrap: if a position is already open when the indicator is added,
													// no PositionUpdate event will fire — manually pick up the existing state.
													if (activeAccount.Positions != null)
													{
														for (int pi = 0; pi < activeAccount.Positions.Count; pi++)
														{
															Position p = activeAccount.Positions[pi];
															if (p.Instrument == Instrument && p.MarketPosition != MarketPosition.Flat && p.Quantity > 0)
															{
																if (!dictInstrumentGeneralInfo.ContainsKey(Instrument))
																	dictInstrumentGeneralInfo.Add(Instrument, new GeneralInfo(p.Quantity));
																GeneralInfo gi = dictInstrumentGeneralInfo[Instrument];
																gi.PrevOrderQuantity = p.Quantity;
																gi.InitCustomStopOrdersCount = 0;
																gi.InitCustomLimitOrdersCount = 0;
																positionPrice = p.AveragePrice;
																isLong = p.MarketPosition == MarketPosition.Long;
																isTimerStopped = false;
																if (timerOrdersUpdate != null && !timerOrdersUpdate.Enabled)
																{
																	timerOrdersUpdate.Enabled = true;
																	timerOrdersUpdate.Start();
																}
																break;
															}
														}
													}
												}
											}
										}
									}
								}
								if (controlPanel == null)
								{
									controlPanel = new DraggablePanel(thickness: new Thickness(CpPositionMarginLeft, CpPositionMarginTop, CpPositionMarginRight, CpPositionMarginBottom), minimized: CpMinimized, minimumButtonWidth: CpMinimumButtonWidth, switchedOn: SwitchedOn, switchActive: CpButtonSwitchActive, switchInactive: CpButtonSwitchInactive, stoplossEnabled: StoplossMovementEnabled, stopActive: CpButtonStopActive, stopInactive: CpButtonStopInactive, targetEnabled: TargetMovementEnabled, targetActive: CpButtonTargetActive, targetInactive: CpButtonTargetInactive, sLMergedIndex: SLMergedIndex, tPMergedIndex: TPMergedIndex, textExecutionSize: CpTextSize, textExecutionBrush: CpTextBrush, dragBrush: CpDragBrush, alignment: CpPositionAlignment);
									controlPanel.gridButtons.Background = chartBackground;
									List<Button>.Enumerator enumerator = controlPanel.listMergeStopBtn.GetEnumerator();
									while (enumerator.MoveNext())
									{
										enumerator.Current.Click += OnButtonMergeStopClick;
									}
									enumerator = controlPanel.listMergeTargetBtn.GetEnumerator();
									while (enumerator.MoveNext())
									{
										enumerator.Current.Click += OnButtonMergeTargetClick;
									}
									controlPanel.drag.DragDelta += OnControlPanelDragDelta;
									controlPanel.btnMini.Click += OnControlPanelBtnMiniClick;
									controlPanel.drag.MouseDoubleClick += OnControlPanelDragDoubleClick;
									if (controlPanel.buttonStop != null)
									{
										controlPanel.buttonStop.Click += OnButtonStopTargetClick;
									}
									if (controlPanel.buttonTarget != null)
									{
										controlPanel.buttonTarget.Click += OnButtonStopTargetClick;
									}
									if (controlPanel.buttonOnOff != null)
									{
										controlPanel.buttonOnOff.Click += OnButtonOnOffClick;
									}
									UserControlCollection.Add(controlPanel);
								}
								// Logo and instruction panel creation removed (license-coupled)
							});
						}
						timerOrdersUpdate = new Timer(500.0);
						timerOrdersUpdate.Elapsed += OnTimerElapsed;
					}
				}
				else
				{
					if (Bars != null)
					{
						historicalLastBar = Bars.Count;
					}
				}
			}
			else
			{
				isUnitATR = Unit == ninZaATRTradeShield_Unit.ATR;
				listLevelInfo = new List<LevelInfo>();
				levelWidthPercent = LevelWidth / 100.0;
				atrInfoSuffix = ((Unit != ninZaATRTradeShield_Unit.ATR) ? " ninZaATR" : " ATR");
				if (dictInstrumentGeneralInfo == null)
				{
					dictInstrumentGeneralInfo = new Dictionary<Instrument, GeneralInfo>();
				}
			}
		}
		else
		{
			Description = string.Empty;
			Name = "ninZaATRTradeShield";
			Calculate = Calculate.OnEachTick;
			IsOverlay = true;
			DisplayInDataBox = true;
			DrawOnPricePanel = true;
			DrawHorizontalGridLines = true;
			DrawVerticalGridLines = true;
			PaintPriceMarkers = true;
			ScaleJustification = ScaleJustification.Right;
			IsSuspendedWhileInactive = false;
			BarsRequiredToPlot = 0;
			AddPlot(Brushes.Transparent, "Stop #1");
			AddPlot(Brushes.Transparent, "Stop #2");
			AddPlot(Brushes.Transparent, "Stop #3");
			AddPlot(Brushes.Transparent, "Target #1");
			AddPlot(Brushes.Transparent, "Target #2");
			AddPlot(Brushes.Transparent, "Target #3");
			LogoEnabled = true;
			InstructionEnabled = true;
			AutoScale = true;
			ScreenDPI = 99;
			LevelWidth = 27.0;
			LevelStopStyle = new Stroke(Brushes.HotPink, (DashStyleHelper)0, 1f, 100);
			LevelTargetStyle = new Stroke(Brushes.LimeGreen, (DashStyleHelper)0, 1f, 100);
			InfoEnabled = true;
			InfoFont = new SimpleFont("Arial", 12);
			InfoMargin = 5;
			SmartModeEnabled = true;
			Unit = ninZaATRTradeShield_Unit.ATR;
			Period = 5;
			Level1StopMultiplier = 2.0;
			Level1TargetMultiplier = 3.0;
			Level2StopMultiplier = 3.0;
			Level2TargetMultiplier = 4.0;
			Level3StopMultiplier = 4.0;
			Level3TargetMultiplier = 5.0;
			StoplossMovementEnabled = true;
			StopOrder1 = ninZaATRTradeShield_StoplossMovement.StopPlot1;
			StopOrder2 = ninZaATRTradeShield_StoplossMovement.StopPlot2;
			StopOrder3 = ninZaATRTradeShield_StoplossMovement.StopPlot3;
			TargetMovementEnabled = true;
			TargetOrder1 = ninZaATRTradeShield_TargetMovement.TargetPlot1;
			TargetOrder2 = ninZaATRTradeShield_TargetMovement.TargetPlot2;
			TargetOrder3 = ninZaATRTradeShield_TargetMovement.TargetPlot3;
			SwitchedOn = true;
			SLMergedIndex = 0;
			TPMergedIndex = 0;
			CpMinimized = false;
			CpMinimumButtonWidth = 100;
			CpButtonSwitchActive = Brushes.LimeGreen;
			CpButtonSwitchInactive = Brushes.Silver;
			CpButtonStopActive = Brushes.HotPink;
			CpButtonStopInactive = Brushes.Thistle;
			CpButtonTargetActive = Brushes.DodgerBlue;
			CpButtonTargetInactive = Brushes.LightSkyBlue;
			CpTextSize = 13;
			CpTextBrush = Brushes.White;
			CpDragBrush = Brushes.LimeGreen;
			CpPositionAlignment = TextPosition.TopLeft;
			CpPositionMarginLeft = 5.0;
			CpPositionMarginTop = 5.0;
			CpPositionMarginRight = 5.0;
			CpPositionMarginBottom = 5.0;
			IndicatorZOrder = -100;
			UserNote = "instrument (period)";
		}
	}

	protected override void OnBarUpdate()
	{
		if (!isTimerStopped || !dictInstrumentGeneralInfo.ContainsKey(Instrument))
		{
			return;
		}
		TriggerCustomEvent((Action<object>)delegate
		{
			bool flag = State == State.Historical && CurrentBar == Bars.Count - ((Calculate != Calculate.OnBarClose) ? 1 : 2);
			if (double.IsNaN(positionPrice))
			{
				GetPositionPrice();
			}
			if (!double.IsNaN(positionPrice) && flag)
			{
				ComputeLevel();
				MoveAndMergeStopTarget();
			}
			ComputeLevel();
			if (!dictInstrumentGeneralInfo.ContainsKey(Instrument)) { return; }
			GeneralInfo generalInfo = dictInstrumentGeneralInfo[Instrument];
			SortedList<int, StopLevelInfo> listPlotInfo = dictInstrumentGeneralInfo[Instrument].ListPlotInfo;
			if (CurrentBar - historicalLastBar < 0)
			{
				if (listPlotInfo.ContainsKey(CurrentBar))
				{
					Stop1[0] = listPlotInfo[CurrentBar].Stop1;
					Stop2[0] = listPlotInfo[CurrentBar].Stop2;
					Stop3[0] = listPlotInfo[CurrentBar].Stop3;
					Target1[0] = listPlotInfo[CurrentBar].Target1;
					Target2[0] = listPlotInfo[CurrentBar].Target2;
					Target3[0] = listPlotInfo[CurrentBar].Target3;
				}
			}
			else if (listLevelInfo.Count > 0)
			{
				StopLevelInfo value = new StopLevelInfo(generalInfo.AtrStop1, generalInfo.AtrStop2, generalInfo.AtrStop3, generalInfo.AtrTarget1, generalInfo.AtrTarget2, generalInfo.AtrTarget3);
				if (listPlotInfo.ContainsKey(CurrentBar))
				{
					listPlotInfo[CurrentBar] = value;
				}
				else
				{
					listPlotInfo.Add(CurrentBar, value);
				}
				Stop1[0] = generalInfo.AtrStop1;
				Stop2[0] = generalInfo.AtrStop2;
				Stop3[0] = generalInfo.AtrStop3;
				Target1[0] = generalInfo.AtrTarget1;
				Target2[0] = generalInfo.AtrTarget2;
				Target3[0] = generalInfo.AtrTarget3;
			}
			if (State == State.Realtime)
			{
				MoveAndMergeStopTarget();
			}
		}, null);
	}

	private void MoveAndMergeStopTarget()
	{
		if (double.IsNaN(positionPrice) || !isLong.HasValue || !dictInstrumentGeneralInfo.ContainsKey(Instrument) || (!StoplossMovementEnabled && !TargetMovementEnabled) || activeAccount == null || activeAccount.Orders == null || activeAccount.Orders.Count <= 0)
		{
			return;
		}
		listCustomStopOrders = new List<Order>();
		listCustomLimitOrders = new List<Order>();
		for (int num = activeAccount.Orders.Count - 1; num >= 0; num--)
		{
			Order val = activeAccount.Orders[num];
			if (val.Instrument == Instruments[0])
			{
				bool isStop = val.OrderType == OrderType.StopMarket || val.OrderType == OrderType.StopLimit;
				bool isLimit = val.OrderType == OrderType.Limit;
				bool stateOk = val.OrderState == OrderState.Accepted || val.OrderState == OrderState.Working || val.OrderState == OrderState.Submitted || val.OrderState == OrderState.ChangePending || val.OrderState == OrderState.ChangeSubmitted || val.OrderState == OrderState.PartFilled || val.OrderState == OrderState.Initialized;
				if (StoplossMovementEnabled && isStop && stateOk)
				{
					listCustomStopOrders.Add(val);
				}
				if (TargetMovementEnabled && isLimit && stateOk)
				{
					listCustomLimitOrders.Add(val);
				}
			}
		}
		GeneralInfo generalInfo = dictInstrumentGeneralInfo[Instrument];
		if (generalInfo.InitCustomStopOrdersCount == 0)
		{
			generalInfo.InitCustomStopOrdersCount = listCustomStopOrders.Count;
		}
		if (generalInfo.InitCustomLimitOrdersCount == 0)
		{
			generalInfo.InitCustomLimitOrdersCount = listCustomLimitOrders.Count;
		}
		currentCustomStopOrdersCount = listCustomStopOrders.Count;
		currentCustomLimitOrdersCount = listCustomLimitOrders.Count;
		if (StoplossMovementEnabled && listCustomStopOrders != null && listCustomStopOrders.Count > 0)
		{
			if (SLMergedIndex != 0)
			{
				double num2 = ((SLMergedIndex == 1) ? generalInfo.AtrStop1 : ((SLMergedIndex != 2) ? generalInfo.AtrStop3 : generalInfo.AtrStop2));
				for (int i = 0; i < listCustomStopOrders.Count; i++)
				{
					Order val2 = listCustomStopOrders[i];
					bool flag = MathExtentions.ApproxCompare(val2.StopPriceChanged, num2) != 0;
					if (!double.IsNaN(num2) && flag && (val2.OrderState == OrderState.Accepted || val2.OrderState == OrderState.Working))
					{
						val2.StopPriceChanged = num2;
						activeAccount.Change((IEnumerable<Order>)(object)new Order[1] { val2 });
					}
				}
			}
			else
			{
				double item = ((StopOrder1 == ninZaATRTradeShield_StoplossMovement.StopPlot1) ? generalInfo.AtrStop1 : ((StopOrder1 == ninZaATRTradeShield_StoplossMovement.StopPlot2) ? generalInfo.AtrStop2 : generalInfo.AtrStop3));
				double item2 = ((StopOrder2 == ninZaATRTradeShield_StoplossMovement.StopPlot1) ? generalInfo.AtrStop1 : ((StopOrder2 == ninZaATRTradeShield_StoplossMovement.StopPlot2) ? generalInfo.AtrStop2 : generalInfo.AtrStop3));
				if (generalInfo.InitCustomStopOrdersCount != 3)
				{
					if (generalInfo.InitCustomStopOrdersCount != 2)
					{
						if (generalInfo.InitCustomStopOrdersCount == 1 && currentCustomStopOrdersCount == 1)
						{
							listAtrStopPrice = new List<double> { item };
						}
					}
					else if (currentCustomStopOrdersCount != 2)
					{
						if (currentCustomStopOrdersCount == 1)
						{
							listAtrStopPrice = new List<double> { item2 };
						}
					}
					else
					{
						listAtrStopPrice = new List<double> { item2, item };
					}
				}
				else
				{
					double item3 = ((StopOrder3 == ninZaATRTradeShield_StoplossMovement.StopPlot1) ? generalInfo.AtrStop1 : ((StopOrder3 == ninZaATRTradeShield_StoplossMovement.StopPlot2) ? generalInfo.AtrStop2 : generalInfo.AtrStop3));
					if (currentCustomStopOrdersCount != 3)
					{
						if (currentCustomStopOrdersCount != 2)
						{
							if (currentCustomStopOrdersCount == 1)
							{
								listAtrStopPrice = new List<double> { item3 };
							}
						}
						else
						{
							listAtrStopPrice = new List<double> { item3, item2 };
						}
					}
					else
					{
						listAtrStopPrice = new List<double> { item3, item2, item };
					}
				}
				for (int j = 0; j < listCustomStopOrders.Count; j++)
				{
					Order val3 = listCustomStopOrders[j];
					double num3 = listAtrStopPrice[j];
					bool flag2 = MathExtentions.ApproxCompare(val3.StopPriceChanged, num3) != 0;
					if (!double.IsNaN(num3) && flag2 && (val3.OrderState == OrderState.Accepted || val3.OrderState == OrderState.Working))
					{
						val3.StopPriceChanged = num3;
						activeAccount.Change((IEnumerable<Order>)(object)new Order[1] { val3 });
					}
				}
			}
		}
		if (!TargetMovementEnabled || listCustomLimitOrders == null || listCustomLimitOrders.Count <= 0)
		{
			return;
		}
		if (TPMergedIndex != 0)
		{
			double num4 = ((TPMergedIndex == 1) ? generalInfo.AtrTarget1 : ((TPMergedIndex != 2) ? generalInfo.AtrTarget3 : generalInfo.AtrTarget2));
			for (int k = 0; k < listCustomLimitOrders.Count; k++)
			{
				Order val4 = listCustomLimitOrders[k];
				bool flag3 = MathExtentions.ApproxCompare(val4.LimitPriceChanged, num4) != 0;
				if (!double.IsNaN(num4) && flag3 && val4.OrderState == OrderState.Working)
				{
					val4.LimitPriceChanged = num4;
					activeAccount.Change((IEnumerable<Order>)(object)new Order[1] { val4 });
				}
			}
			return;
		}
		double item4 = ((TargetOrder1 == ninZaATRTradeShield_TargetMovement.TargetPlot1) ? generalInfo.AtrTarget1 : ((TargetOrder1 == ninZaATRTradeShield_TargetMovement.TargetPlot2) ? generalInfo.AtrTarget2 : generalInfo.AtrTarget3));
		double item5 = ((TargetOrder2 == ninZaATRTradeShield_TargetMovement.TargetPlot1) ? generalInfo.AtrTarget1 : ((TargetOrder2 == ninZaATRTradeShield_TargetMovement.TargetPlot2) ? generalInfo.AtrTarget2 : generalInfo.AtrTarget3));
		if (generalInfo.InitCustomLimitOrdersCount != 3)
		{
			if (generalInfo.InitCustomLimitOrdersCount != 2)
			{
				if (generalInfo.InitCustomLimitOrdersCount == 1 && currentCustomLimitOrdersCount == 1)
				{
					listAtrTargetPrice = new List<double> { item4 };
				}
			}
			else if (currentCustomLimitOrdersCount != 2)
			{
				if (currentCustomLimitOrdersCount == 1)
				{
					listAtrTargetPrice = new List<double> { item5 };
				}
			}
			else
			{
				listAtrTargetPrice = new List<double> { item5, item4 };
			}
		}
		else
		{
			double item6 = ((TargetOrder3 == ninZaATRTradeShield_TargetMovement.TargetPlot1) ? generalInfo.AtrTarget1 : ((TargetOrder3 == ninZaATRTradeShield_TargetMovement.TargetPlot2) ? generalInfo.AtrTarget2 : generalInfo.AtrTarget3));
			if (currentCustomLimitOrdersCount != 3)
			{
				if (currentCustomLimitOrdersCount != 2)
				{
					if (currentCustomLimitOrdersCount == 1)
					{
						listAtrTargetPrice = new List<double> { item6 };
					}
				}
				else
				{
					listAtrTargetPrice = new List<double> { item6, item5 };
				}
			}
			else
			{
				listAtrTargetPrice = new List<double> { item6, item5, item4 };
			}
		}
		for (int l = 0; l < listCustomLimitOrders.Count; l++)
		{
			Order val5 = listCustomLimitOrders[l];
			double num5 = listAtrTargetPrice[l];
			bool flag4 = MathExtentions.ApproxCompare(val5.LimitPriceChanged, num5) != 0;
			if (!double.IsNaN(num5) && flag4 && val5.OrderState == OrderState.Working)
			{
				val5.LimitPriceChanged = num5;
				activeAccount.Change((IEnumerable<Order>)(object)new Order[1] { val5 });
			}
		}
	}

	public override void OnCalculateMinMax()
	{
		if (!AutoScale)
		{
			return;
		}
		if (!isLong.HasValue)
		{
			double num = double.MaxValue;
			double num2 = double.MinValue;
			for (int i = ChartBars.FromIndex; i <= ChartBars.ToIndex; i++)
			{
				num = Math.Min(num, Low.GetValueAt(i));
				num2 = Math.Max(num2, High.GetValueAt(i));
			}
			MinValue = num;
			MaxValue = num2;
			return;
		}
		GeneralInfo generalInfo = dictInstrumentGeneralInfo[Instrument];
		double num3;
		double num4;
		if (!isLong.Value)
		{
			num3 = Math.Max(generalInfo.AtrStop1, Math.Max(generalInfo.AtrStop2, generalInfo.AtrStop3));
			num4 = Math.Min(generalInfo.AtrTarget1, Math.Min(generalInfo.AtrTarget2, generalInfo.AtrTarget3));
		}
		else
		{
			num3 = Math.Max(generalInfo.AtrTarget1, Math.Max(generalInfo.AtrTarget2, generalInfo.AtrTarget3));
			num4 = Math.Min(generalInfo.AtrStop1, Math.Min(generalInfo.AtrStop2, generalInfo.AtrStop3));
		}
		double num5 = Math.Min(num4, MinValue);
		if (num5 == 0.0)
		{
			num5 = num4;
		}
		if (!double.IsNaN(num5) && !double.IsNaN(num3))
		{
			MinValue = num5;
			MaxValue = Math.Max(num3, MaxValue);
		}
	}

	protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
	{
		if (isCharting && SwitchedOn)
		{
			base.OnRender(chartControl, chartScale);
			TriggerCustomEvent((Action<object>)delegate
			{
				DrawLevelsAndInfo(chartControl, chartScale);
			}, null);
		}
	}

	private void DrawLevelsAndInfo(ChartControl chartControl, ChartScale chartScale)
	{
		if (!double.IsNaN(positionPrice) && isLong.HasValue && listLevelInfo.Count != 0)
		{
			List<LevelInfo>.Enumerator enumerator = listLevelInfo.GetEnumerator();
			while (enumerator.MoveNext())
			{
				LevelInfo current = enumerator.Current;
				DrawOneLevelAndInfo(chartControl, chartScale, current);
			}
		}
	}

	private void DrawOneLevelAndInfo(ChartControl chartControl, ChartScale chartScale, LevelInfo levelInfo)
	{
		int canvasRight = chartControl.CanvasRight;
		int num = canvasRight - Convert.ToInt32(levelWidthPercent * (double)canvasRight);
		int num2 = num - InfoMargin;
		Brush brush = LevelStopStyle.Brush;
		if (!BrushExtensions.IsTransparent(brush))
		{
			double stopPrice = levelInfo.StopPrice;
			int yByValue = chartScale.GetYByValue(stopPrice);
			SharpDX.Direct2D1.Brush val = DxExtensions.ToDxBrush(brush, RenderTarget);
			double num3 = ComputeTextSize(FormatPriceMarker(stopPrice), InfoFont, ScreenDPI).Width;
			RenderTarget.DrawLine(new Vector2((float)num, (float)yByValue), new Vector2((float)(int)((double)canvasRight - num3 - (double)InfoMargin), (float)yByValue), val, LevelStopStyle.Width, LevelStopStyle.StrokeStyle);
			((DisposeBase)val).Dispose();
			if (InfoEnabled)
			{
				DrawTextOnChart(FormatPriceMarker(stopPrice), InfoFont, (float)(int)((double)canvasRight - num3), (float)yByValue, 1, 0, brush, ScreenDPI, RenderTarget);
				DrawTextOnChart(levelInfo.StopAtrInfoPrefix + atrInfoSuffix, InfoFont, (float)num2, (float)yByValue, -1, 0, brush, ScreenDPI, RenderTarget);
			}
		}
		Brush brush2 = LevelTargetStyle.Brush;
		if (!BrushExtensions.IsTransparent(brush2))
		{
			double targetPrice = levelInfo.TargetPrice;
			int yByValue2 = chartScale.GetYByValue(targetPrice);
			SharpDX.Direct2D1.Brush val2 = DxExtensions.ToDxBrush(brush2, RenderTarget);
			double num4 = ComputeTextSize(FormatPriceMarker(targetPrice), InfoFont, ScreenDPI).Width;
			RenderTarget.DrawLine(new Vector2((float)num, (float)yByValue2), new Vector2((float)(int)((double)canvasRight - num4 - (double)InfoMargin), (float)yByValue2), val2, LevelTargetStyle.Width, LevelTargetStyle.StrokeStyle);
			((DisposeBase)val2).Dispose();
			if (InfoEnabled)
			{
				DrawTextOnChart(FormatPriceMarker(targetPrice), InfoFont, (float)(int)((double)canvasRight - num4), (float)yByValue2, 1, 0, brush2, ScreenDPI, RenderTarget);
				DrawTextOnChart(levelInfo.TargetAtrInfoPrefix + atrInfoSuffix, InfoFont, (float)num2, (float)yByValue2, -1, 0, brush2, ScreenDPI, RenderTarget);
			}
		}
	}

	private void OnTimerElapsed(object sender, ElapsedEventArgs e)
	{
		TriggerCustomEvent((Action<object>)delegate
		{
			if (isCharting)
			{
				ChartControl.Dispatcher.InvokeAsync(delegate
				{
					if (isLong.HasValue)
					{
						GeneralInfo generalInfo = dictInstrumentGeneralInfo[Instrument];
						generalInfo.InitCustomLimitOrdersCount = 0;
						generalInfo.InitCustomStopOrdersCount = 0;
						MoveAndMergeStopTarget();
						countTimer++;
						if (countTimer >= 2)
						{
							isTimerStopped = true;
							countTimer = 0;
							timerOrdersUpdate.Stop();
							timerOrdersUpdate.Enabled = false;
						}
						ChartControl.InvalidateVisual();
						ChartControl.InvalidateVisual();
					}
				});
			}
		}, null);
	}

	public override string FormatPriceMarker(double price)
	{
		return Instrument.MasterInstrument.FormatPrice(Instrument.MasterInstrument.RoundToTickSize(price), true);
	}

	private double R2TS(double price)
	{
		return Instrument.MasterInstrument.RoundToTickSize(price);
	}

	private void GetPositionPrice()
	{
		if (!double.IsNaN(positionPrice))
		{
			return;
		}
		if (State == State.Realtime)
		{
			positionPrice = double.NaN;
		}
		isLong = null;
		if (activeAccount == null || activeAccount.Positions == null || activeAccount.Positions.Count() <= 0)
		{
			return;
		}
		int num = activeAccount.Positions.Count - 1;
		Position val;
		while (true)
		{
			if (num < 0)
			{
				return;
			}
			val = activeAccount.Positions[num];
			if (val.Instrument == Instrument)
			{
				break;
			}
			num--;
		}
		positionPrice = val.AveragePrice;
		isLong = (int)val.MarketPosition == 0;
	}

	private void ComputeLevel()
	{
		if (!dictInstrumentGeneralInfo.ContainsKey(Instrument))
		{
			return;
		}
		if (!SmartModeEnabled)
		{
			if (double.IsNaN(atrValue))
			{
				atrValue = ATR(Period).Values[0].GetValueAt(CurrentBar);
			}
		}
		else if (State == State.Realtime || (State == State.Historical && CurrentBar == Bars.Count - (((int)Calculate != 0) ? 1 : 2)))
		{
			atrValue = ATR(Period)[0];
		}
		GeneralInfo generalInfo = dictInstrumentGeneralInfo[Instrument];
		if (!isLong.HasValue || double.IsNaN(positionPrice))
		{
			if (!SmartModeEnabled)
			{
				atrValue = double.NaN;
				generalInfo.AtrStop1 = double.NaN;
			}
			listLevelInfo.Clear();
			currentCustomStopOrdersCount = 0;
			currentCustomLimitOrdersCount = 0;
			return;
		}
		int num = (isLong.Value ? 1 : (-1));
		if (!SmartModeEnabled && (SmartModeEnabled || !double.IsNaN(generalInfo.AtrStop1)))
		{
			return;
		}
		if (SmartModeEnabled)
		{
			if (generalInfo.IsLevelsInitialized)
			{
				listLevelInfo.Clear();
				double num2 = R2TS(Close[0] - (double)num * Level1StopMultiplier * atrValue);
				if (MathExtentions.ApproxCompare(num2, generalInfo.AtrStop1) * num > 0 || double.IsNaN(generalInfo.AtrStop1))
				{
					generalInfo.AtrStop1 = num2;
				}
				listLevelInfo.Add(new LevelInfo(generalInfo.AtrStop1, (atrTarget1InitValue == 0.0) ? generalInfo.AtrTarget1 : atrTarget1InitValue, Level1StopMultiplier, Level1TargetMultiplier));
				double num3 = R2TS(Close[0] - (double)num * Level2StopMultiplier * atrValue);
				if (MathExtentions.ApproxCompare(num3, generalInfo.AtrStop2) * num > 0 || double.IsNaN(generalInfo.AtrStop2))
				{
					generalInfo.AtrStop2 = num3;
				}
				listLevelInfo.Add(new LevelInfo(generalInfo.AtrStop2, (atrTarget2InitValue == 0.0) ? generalInfo.AtrTarget2 : atrTarget2InitValue, Level2StopMultiplier, Level2TargetMultiplier));
				double num4 = R2TS(Close[0] - (double)num * Level3StopMultiplier * atrValue);
				if (MathExtentions.ApproxCompare(num4, generalInfo.AtrStop3) * num > 0 || double.IsNaN(generalInfo.AtrStop3))
				{
					generalInfo.AtrStop3 = num4;
				}
				listLevelInfo.Add(new LevelInfo(generalInfo.AtrStop3, (atrTarget3InitValue == 0.0) ? generalInfo.AtrTarget3 : atrTarget3InitValue, Level3StopMultiplier, Level3TargetMultiplier));
			}
			else
			{
				generalInfo.AtrStop1 = R2TS(Close[0] - (double)num * Level1StopMultiplier * atrValue);
				generalInfo.AtrTarget1 = R2TS(Close[0] + (double)num * Level1TargetMultiplier * atrValue);
				generalInfo.AtrStop2 = R2TS(Close[0] - (double)num * Level2StopMultiplier * atrValue);
				generalInfo.AtrTarget2 = R2TS(Close[0] + (double)num * Level2TargetMultiplier * atrValue);
				generalInfo.AtrStop3 = R2TS(Close[0] - (double)num * Level3StopMultiplier * atrValue);
				generalInfo.AtrTarget3 = R2TS(Close[0] + (double)num * Level3TargetMultiplier * atrValue);
				listLevelInfo.Clear();
				listLevelInfo.Add(new LevelInfo(generalInfo.AtrStop1, generalInfo.AtrTarget1, Level1StopMultiplier, Level1TargetMultiplier));
				listLevelInfo.Add(new LevelInfo(generalInfo.AtrStop2, generalInfo.AtrTarget2, Level2StopMultiplier, Level2TargetMultiplier));
				listLevelInfo.Add(new LevelInfo(generalInfo.AtrStop3, generalInfo.AtrTarget3, Level3StopMultiplier, Level3TargetMultiplier));
				if (!double.IsNaN(atrValue))
				{
					generalInfo.IsLevelsInitialized = true;
				}
			}
		}
		else
		{
			generalInfo.AtrStop1 = R2TS(Close[0] - (double)num * Level1StopMultiplier * atrValue);
			generalInfo.AtrTarget1 = R2TS(atrTarget1InitValue = Close[0] + (double)num * Level1TargetMultiplier * atrValue);
			generalInfo.AtrStop2 = R2TS(Close[0] - (double)num * Level2StopMultiplier * atrValue);
			generalInfo.AtrTarget2 = R2TS(atrTarget2InitValue = Close[0] + (double)num * Level2TargetMultiplier * atrValue);
			generalInfo.AtrStop3 = R2TS(Close[0] - (double)num * Level3StopMultiplier * atrValue);
			generalInfo.AtrTarget3 = R2TS(atrTarget3InitValue = Close[0] + (double)num * Level3TargetMultiplier * atrValue);
			listLevelInfo.Clear();
			listLevelInfo.Add(new LevelInfo(generalInfo.AtrStop1, generalInfo.AtrTarget1, Level1StopMultiplier, Level1TargetMultiplier));
			listLevelInfo.Add(new LevelInfo(generalInfo.AtrStop2, generalInfo.AtrTarget2, Level2StopMultiplier, Level2TargetMultiplier));
			listLevelInfo.Add(new LevelInfo(generalInfo.AtrStop3, generalInfo.AtrTarget3, Level3StopMultiplier, Level3TargetMultiplier));
		}
	}

	private void ResetPlots()
	{
		SortedList<int, StopLevelInfo> listPlotInfo = dictInstrumentGeneralInfo[Instrument].ListPlotInfo;
		if (listPlotInfo != null && listPlotInfo.Count > 0)
		{
			IEnumerator<int> enumerator = listPlotInfo.Keys.GetEnumerator();
			while (enumerator.MoveNext())
			{
				int current = enumerator.Current;
				Stop1.Reset(CurrentBar - current);
				Stop2.Reset(CurrentBar - current);
				Stop3.Reset(CurrentBar - current);
				Target1.Reset(CurrentBar - current);
				Target2.Reset(CurrentBar - current);
				Target3.Reset(CurrentBar - current);
			}
			listPlotInfo.Clear();
		}
	}

	private void OnChartTraderAccountChange(object sender, SelectionChangedEventArgs e)
	{
		TriggerCustomEvent((Action<object>)delegate
		{
			if (isCharting)
			{
				ChartControl.Dispatcher.InvokeAsync(delegate
				{
					if (isValidTab)
					{
						listLevelInfo.Clear();
						positionPrice = double.NaN;
						isLong = null;
						ResetPlots();
						activeAccount = chartTraderAccountSelector.SelectedAccount;
						if (activeAccount != null && activeAccount.Positions != null && activeAccount.Positions.Count > 0)
						{
							GetPositionPrice();
							ComputeLevel();
						}
						ChartControl.InvalidateVisual();
						ChartControl.InvalidateVisual();
					}
				});
			}
		}, (object)e);
	}

	private void OnChartTraderPositionUpdate(object sender, PositionEventArgs e)
	{
		TriggerCustomEvent((Action<object>)delegate
		{
			if (isCharting)
			{
				ChartControl.Dispatcher.InvokeAsync(delegate
				{
					if ((int)e.Operation != 2)
					{
						if ((int)e.Operation != 0)
						{
							GeneralInfo generalInfo = dictInstrumentGeneralInfo[Instrument];
							if (e.Position.Quantity >= generalInfo.PrevOrderQuantity && e.Position.Instrument == Instrument)
							{
								generalInfo.InitCustomStopOrdersCount = 0;
								generalInfo.InitCustomLimitOrdersCount = 0;
								generalInfo.PrevOrderQuantity = e.Position.Quantity;
								isTimerStopped = false;
								positionPrice = e.Position.AveragePrice;
								isLong = (int)e.Position.MarketPosition == 0;
								if (!timerOrdersUpdate.Enabled)
								{
									timerOrdersUpdate.Enabled = true;
									timerOrdersUpdate.Start();
								}
								else
								{
									countTimer = 0;
								}
							}
						}
						else if (e.Position.Instrument == Instrument)
						{
							if (!dictInstrumentGeneralInfo.ContainsKey(Instrument))
							{
								dictInstrumentGeneralInfo.Add(Instrument, new GeneralInfo(e.Position.Quantity));
							}
							GeneralInfo generalInfo2 = dictInstrumentGeneralInfo[Instrument];
							generalInfo2.PrevOrderQuantity = e.Position.Quantity;
							generalInfo2.InitCustomStopOrdersCount = 0;
							generalInfo2.InitCustomLimitOrdersCount = 0;
							isTimerStopped = false;
							GetPositionPrice();
							ComputeLevel();
							positionPrice = e.Position.AveragePrice;
							isLong = (int)e.Position.MarketPosition == 0;
							if (!timerOrdersUpdate.Enabled)
							{
								timerOrdersUpdate.Enabled = true;
								timerOrdersUpdate.Start();
							}
							else
							{
								countTimer = 0;
							}
						}
					}
					else if (e.Position.Instrument == Instrument)
					{
						listLevelInfo.Clear();
						positionPrice = double.NaN;
						isLong = null;
						ResetPlots();
						if (dictInstrumentGeneralInfo.ContainsKey(Instrument))
						{
							dictInstrumentGeneralInfo[Instrument] = new GeneralInfo(-1);
						}
						if (timerOrdersUpdate.Enabled)
						{
							countTimer = 0;
							timerOrdersUpdate.Stop();
							timerOrdersUpdate.Enabled = false;
						}
					}
					ChartControl.InvalidateVisual();
					ChartControl.InvalidateVisual();
				});
			}
		}, (object)e);
	}

	private void OnTabSwitch(object sender, SelectionChangedEventArgs e)
	{
		TriggerCustomEvent((Action<object>)delegate
		{
			if (isCharting)
			{
				ChartControl.Dispatcher.InvokeAsync(delegate
				{
					bool num = isValidTab;
					ref bool reference = ref isValidTab;
					object content = (tabControl.SelectedItem as TabItem).Content;
					reference = ((ChartTab)((content is ChartTab) ? content : null)).ChartControl == ChartControl;
					if (!num && isValidTab && chartTraderAccountSelector.SelectedAccount != activeAccount)
					{
						chartTraderAccountSelector.SelectedAccount = activeAccount;
					}
				});
			}
		}, (object)e);
	}

	private void OnControlPanelDragDelta(object sender, DragDeltaEventArgs e)
	{
		TriggerCustomEvent((Action<object>)delegate
		{
			if (isCharting)
			{
				ChartControl.Dispatcher.InvokeAsync(delegate
				{
					if (controlPanel.alignment != TextPosition.Center)
					{
						double val = 5.0;
						double val2 = 5.0;
						double num = 5.0;
						double val3 = 5.0;
						if (controlPanel.HorizontalAlignment != HorizontalAlignment.Left)
						{
							val2 = controlPanel.Margin.Right - e.HorizontalChange;
						}
						else
						{
							val3 = controlPanel.Margin.Left + e.HorizontalChange;
						}
						num = ((controlPanel.VerticalAlignment == VerticalAlignment.Top) ? (controlPanel.Margin.Top + e.VerticalChange) : (controlPanel.Margin.Top + e.VerticalChange));
						val3 = Math.Max(0.0, val3);
						num = Math.Max(0.0, num);
						val2 = Math.Max(0.0, val2);
						val = Math.Max(0.0, val);
						controlPanel.Margin = new Thickness(val3, num, val2, val);
						CpPositionMarginLeft = controlPanel.Margin.Left;
						CpPositionMarginTop = controlPanel.Margin.Top;
						CpPositionMarginRight = controlPanel.Margin.Right;
						CpPositionMarginBottom = controlPanel.Margin.Bottom;
					}
					else
					{
						controlPanel.Margin = new Thickness(5.0);
					}
				});
			}
		}, (object)e);
	}

	private void OnControlPanelDragDoubleClick(object sender, MouseButtonEventArgs e)
	{
		TriggerCustomEvent((Action<object>)delegate
		{
			if (isCharting)
			{
				ChartControl.Dispatcher.InvokeAsync(delegate
				{
					SetControlPanelState();
					if (sender != null)
					{
					}
				});
			}
		}, (object)e);
	}

	private void SetControlPanelState()
	{
		CpMinimized = !CpMinimized;
		controlPanel.SetState(CpMinimized);
	}

	private void OnControlPanelBtnMiniClick(object sender, RoutedEventArgs e)
	{
		TriggerCustomEvent((Action<object>)delegate
		{
			if (isCharting)
			{
				ChartControl.Dispatcher.InvokeAsync(delegate
				{
					SetControlPanelState();
				});
			}
		}, (object)e);
	}

	private void OnButtonOnOffClick(object sender, RoutedEventArgs e)
	{
		TriggerCustomEvent((Action<object>)delegate
		{
			if (isCharting)
			{
				ChartControl.Dispatcher.InvokeAsync(delegate
				{
					controlPanel.hideFocus.Focus();
					SwitchedOn = !SwitchedOn;
					controlPanel.SetButtonBackground(controlPanel.buttonOnOff, (!SwitchedOn) ? CpButtonSwitchInactive : CpButtonSwitchActive);
					ChartControl.InvalidateVisual();
					ChartControl.InvalidateVisual();
				});
			}
		}, (object)e);
	}

	private void OnButtonStopTargetClick(object sender, RoutedEventArgs e)
	{
		TriggerCustomEvent((Action<object>)delegate
		{
			if (isCharting)
			{
				ChartControl.Dispatcher.InvokeAsync(delegate
				{
					controlPanel.hideFocus.Focus();
					if (sender is Button button)
					{
						if (button != controlPanel.buttonStop)
						{
							if (button == controlPanel.buttonTarget)
							{
								TargetMovementEnabled = !TargetMovementEnabled;
							}
						}
						else
						{
							StoplossMovementEnabled = !StoplossMovementEnabled;
						}
						MoveAndMergeStopTarget();
						controlPanel.SetButtonBackground(controlPanel.buttonStop, (!StoplossMovementEnabled) ? CpButtonStopInactive : CpButtonStopActive);
						controlPanel.SetButtonBackground(controlPanel.buttonTarget, (!TargetMovementEnabled) ? CpButtonTargetInactive : CpButtonTargetActive);
						ChartControl.InvalidateVisual();
						ChartControl.InvalidateVisual();
					}
				});
			}
		}, (object)e);
	}

	private void OnButtonMergeTargetClick(object sender, RoutedEventArgs e)
	{
		TriggerCustomEvent((Action<object>)delegate
		{
			if (isCharting)
			{
				ChartControl.Dispatcher.InvokeAsync(delegate
				{
					controlPanel.hideFocus.Focus();
					byte b = byte.Parse((sender as Button).Tag.ToString());
					TPMergedIndex = (byte)((TPMergedIndex == 0 || TPMergedIndex != b) ? b : 0);
					MoveAndMergeStopTarget();
					controlPanel.SetButtonBackground(controlPanel.btnMergeTarget1, (TPMergedIndex != 1) ? CpButtonTargetInactive : CpButtonTargetActive);
					controlPanel.SetButtonBackground(controlPanel.btnMergeTarget2, (TPMergedIndex != 2) ? CpButtonTargetInactive : CpButtonTargetActive);
					controlPanel.SetButtonBackground(controlPanel.btnMergeTarget3, (TPMergedIndex != 3) ? CpButtonTargetInactive : CpButtonTargetActive);
					ChartControl.InvalidateVisual();
					ChartControl.InvalidateVisual();
				});
			}
		}, (object)e);
	}

	private void OnButtonMergeStopClick(object sender, RoutedEventArgs e)
	{
		TriggerCustomEvent((Action<object>)delegate
		{
			if (isCharting)
			{
				ChartControl.Dispatcher.InvokeAsync(delegate
				{
					controlPanel.hideFocus.Focus();
					byte b = byte.Parse((sender as Button).Tag.ToString());
					SLMergedIndex = (byte)((SLMergedIndex == 0 || SLMergedIndex != b) ? b : 0);
					MoveAndMergeStopTarget();
					controlPanel.SetButtonBackground(controlPanel.btnMergeStop1, (SLMergedIndex != 1) ? CpButtonStopInactive : CpButtonStopActive);
					controlPanel.SetButtonBackground(controlPanel.btnMergeStop2, (SLMergedIndex != 2) ? CpButtonStopInactive : CpButtonStopActive);
					controlPanel.SetButtonBackground(controlPanel.btnMergeStop3, (SLMergedIndex != 3) ? CpButtonStopInactive : CpButtonStopActive);
					ChartControl.InvalidateVisual();
					ChartControl.InvalidateVisual();
				});
			}
		}, (object)e);
	}

	// CreateInstructionContent / OnInstructionClose removed (license panel stripped)

	private int ComputeTextHeight(string text, SimpleFont font)
	{
		int lines = 1;
		foreach (char c in text)
			if (c == '\n') lines++;
		return (int)(font.Size * 1.4 * lines);
	}

	private Size ComputeTextSize(string text, SimpleFont font, int dpi)
	{
		if (string.IsNullOrEmpty(text)) return new Size(0, 0);
		int lines = 1;
		int maxLen = 0;
		int cur = 0;
		foreach (char c in text)
		{
			if (c == '\n') { lines++; if (cur > maxLen) maxLen = cur; cur = 0; }
			else cur++;
		}
		if (cur > maxLen) maxLen = cur;
		return new Size(font.Size * maxLen * 0.6, font.Size * 1.4 * lines);
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
					float drawY = (direction < 0) ? y - textH : y;
					rt.DrawTextLayout(new SharpDX.Vector2(drawX, drawY), layout, dxBrush);
				}
			}
		}
	}

}

public enum ninZaATRTradeShield_StoplossMovement
{
	StopPlot1,
	StopPlot2,
	StopPlot3
}

public enum ninZaATRTradeShield_TargetMovement
{
	TargetPlot1,
	TargetPlot2,
	TargetPlot3
}

public enum ninZaATRTradeShield_Unit
{
	ninZaATR,
	ATR
}

public class ninZaATRTradeShield_SoundConverter : System.ComponentModel.TypeConverter
{
	public override System.ComponentModel.TypeConverter.StandardValuesCollection GetStandardValues(System.ComponentModel.ITypeDescriptorContext context)
	{
		if (context != null)
		{
			System.Collections.Generic.List<string> list = new System.Collections.Generic.List<string>();
			System.IO.FileInfo[] files = new System.IO.DirectoryInfo(NinjaTrader.Core.Globals.InstallDir + "sounds").GetFiles("*.wav");
			foreach (System.IO.FileInfo fileInfo in files)
			{
				list.Add(fileInfo.Name);
			}
			return new System.ComponentModel.TypeConverter.StandardValuesCollection(list);
		}
		return null;
	}

	public override bool GetStandardValuesSupported(System.ComponentModel.ITypeDescriptorContext context)
	{
		return true;
	}
}

}
