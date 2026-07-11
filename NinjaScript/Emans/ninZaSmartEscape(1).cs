#region Using declarations
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
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
using NinjaTrader.Gui.NinjaScript.AtmStrategy;
using NinjaTrader.Gui.Tools;
using NinjaTrader.NinjaScript.DrawingTools;
using SharpDX;
using SharpDX.Direct2D1;
using Brush = System.Windows.Media.Brush;
using SolidColorBrush = System.Windows.Media.SolidColorBrush;
using Color = System.Windows.Media.Color;
using Point = System.Windows.Point;
#endregion

namespace NinjaTrader.NinjaScript.Indicators
{
[TypeConverter("NinjaTrader.NinjaScript.Indicators.ninZaSmartEscape_Converter")]
[CategoryOrder("Critical", 1000060)]
[CategoryOrder("Special", 1000050)]
[CategoryOrder("Control Panel", 1000040)]
[CategoryOrder("Graphics", 1000030)]
[CategoryOrder("Pending Order", 1000020)]
[CategoryOrder("General", 1000010)]
[CategoryOrder("Developer", 0)]
public class ninZaSmartEscape : Indicator
{
	private class DraggablePanel : Grid
	{
		public Grid gridBtns;

		public Button btnMini;

		public Button btnExitConditionEnabled;

		public Thumb drag;

		public Button btnLong;

		public Button btnLongAny;

		public Button btnLongUp;

		public Button btnLongDown;

		public Button btnShort;

		public Button btnShortAny;

		public Button btnShortUp;

		public Button btnShortDown;

		public QuantityUpDown selectorOffsetLongAny;

		public QuantityUpDown selectorOffsetLongUp;

		public QuantityUpDown selectorOffsetLongDown;

		public QuantityUpDown selectorOffsetShortAny;

		public QuantityUpDown selectorOffsetShortUp;

		public QuantityUpDown selectorOffsetShortDown;

		public ComboBox accountSelector;

		public TextPosition alignment;

		public DraggablePanel(bool minimized, int minimumButtonWidth, Brush exitConditionEnabledBackground, Brush exitConditionEnableTextColor, Brush exitConditionLongEnabledBackground, Brush exitConditionShortEnabledBackground, Brush executionLongAny, Brush executionLongUp, Brush executionLongDown, Brush executionShortAny, Brush executionShortUp, Brush executionShortDown, double textExecutionSize, Brush textExecutionBrush, double textSettingSize, Brush dragBrush, TextPosition alignment, Thickness thickness)
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
			btnMini = CreateButton("Smart Escape", string.Empty, textExecutionSize, dragBrush, exitConditionEnableTextColor, minimumButtonWidth, "  Click to restore.");
			btnMini.Margin = new Thickness(1.0, 0.0, 0.0, 0.0);
			btnMini.SetValue(Grid.ColumnProperty, 1);
			base.Children.Add(btnMini);
			gridBtns = new Grid();
			base.Children.Add(gridBtns);
			gridBtns.RowDefinitions.Add(new RowDefinition());
			btnExitConditionEnabled = CreateButton("Smart Escape", string.Empty, textExecutionSize, exitConditionEnabledBackground, exitConditionEnableTextColor, -1, null, 0, 4);
			btnExitConditionEnabled.HorizontalAlignment = HorizontalAlignment.Stretch;
			btnExitConditionEnabled.Margin = new Thickness(1.0, 0.0, 0.0, 0.0);
			btnExitConditionEnabled.Cursor = Cursors.Hand;
			gridBtns.Children.Add(btnExitConditionEnabled);
			gridBtns.RowDefinitions.Add(new RowDefinition());
			btnLong = CreateButton("LONG", "long", textExecutionSize, exitConditionLongEnabledBackground, textExecutionBrush, minimumButtonWidth, null, 0, 0, 1, 2);
			gridBtns.Children.Add(btnLong);
			btnLongAny = CreateButton("Any", "any", textExecutionSize, executionLongAny, textExecutionBrush, minimumButtonWidth, null, 1, 0, 1);
			gridBtns.Children.Add(btnLongAny);
			btnLong.Height = btnLongAny.Height * 2.0 - 5.0;
			btnLongUp = CreateButton("Up", "up", textExecutionSize, executionLongUp, textExecutionBrush, minimumButtonWidth, null, 2, 0, 1);
			gridBtns.Children.Add(btnLongUp);
			btnLongDown = CreateButton("Down", "down", textExecutionSize, executionLongDown, textExecutionBrush, minimumButtonWidth, null, 3, 0, 1);
			gridBtns.Children.Add(btnLongDown);
			gridBtns.RowDefinitions.Add(new RowDefinition());
			QuantityUpDown val = new QuantityUpDown();
			((FrameworkElement)val).HorizontalAlignment = HorizontalAlignment.Stretch;
			((FrameworkElement)val).VerticalAlignment = VerticalAlignment.Top;
			((FrameworkElement)val).Margin = new Thickness(1.0, 1.0, 0.0, 0.0);
			((Control)val).FontSize = textSettingSize;
			val.IsPopupEnabled = false;
			((FrameworkElement)val).Cursor = Cursors.Arrow;
			selectorOffsetLongAny = val;
			((DependencyObject)(object)selectorOffsetLongAny).SetValue(Grid.RowProperty, (object)2);
			((DependencyObject)(object)selectorOffsetLongAny).SetValue(Grid.ColumnProperty, (object)1);
			gridBtns.Children.Add((UIElement)(object)selectorOffsetLongAny);
			QuantityUpDown val2 = new QuantityUpDown();
			((FrameworkElement)val2).HorizontalAlignment = HorizontalAlignment.Stretch;
			((FrameworkElement)val2).VerticalAlignment = VerticalAlignment.Top;
			((FrameworkElement)val2).Margin = new Thickness(1.0, 1.0, 0.0, 0.0);
			((Control)val2).FontSize = textSettingSize;
			val2.IsPopupEnabled = false;
			((FrameworkElement)val2).Cursor = Cursors.Arrow;
			selectorOffsetLongUp = val2;
			((DependencyObject)(object)selectorOffsetLongUp).SetValue(Grid.RowProperty, (object)2);
			((DependencyObject)(object)selectorOffsetLongUp).SetValue(Grid.ColumnProperty, (object)2);
			gridBtns.Children.Add((UIElement)(object)selectorOffsetLongUp);
			QuantityUpDown val3 = new QuantityUpDown();
			((FrameworkElement)val3).HorizontalAlignment = HorizontalAlignment.Stretch;
			((FrameworkElement)val3).VerticalAlignment = VerticalAlignment.Top;
			((FrameworkElement)val3).Margin = new Thickness(1.0, 1.0, 0.0, 0.0);
			((Control)val3).FontSize = textSettingSize;
			val3.IsPopupEnabled = false;
			((FrameworkElement)val3).Cursor = Cursors.Arrow;
			selectorOffsetLongDown = val3;
			((DependencyObject)(object)selectorOffsetLongDown).SetValue(Grid.RowProperty, (object)2);
			((DependencyObject)(object)selectorOffsetLongDown).SetValue(Grid.ColumnProperty, (object)3);
			gridBtns.Children.Add((UIElement)(object)selectorOffsetLongDown);
			gridBtns.RowDefinitions.Add(new RowDefinition());
			btnShort = CreateButton("SHORT", "short", textExecutionSize, exitConditionShortEnabledBackground, textExecutionBrush, minimumButtonWidth, null, 0, 0, 3, 2);
			gridBtns.Children.Add(btnShort);
			btnShortAny = CreateButton("Any", "any", textExecutionSize, executionShortAny, textExecutionBrush, minimumButtonWidth, null, 1, 0, 3);
			gridBtns.Children.Add(btnShortAny);
			btnShort.Height = btnShort.Height * 2.0 - 5.0;
			btnShortUp = CreateButton("Up", "up", textExecutionSize, executionShortUp, textExecutionBrush, minimumButtonWidth, null, 2, 0, 3);
			gridBtns.Children.Add(btnShortUp);
			btnShortDown = CreateButton("Down", "down", textExecutionSize, executionShortDown, textExecutionBrush, minimumButtonWidth, null, 3, 0, 3);
			gridBtns.Children.Add(btnShortDown);
			int num = ((btnShort == null) ? minimumButtonWidth : Math.Max(minimumButtonWidth, Convert.ToInt32(btnShort.Width)));
			int num2 = ((btnShortDown == null) ? minimumButtonWidth : Math.Max(minimumButtonWidth, Convert.ToInt32(btnShortDown.Width)));
			btnLong.Width = (btnShort.Width = num);
			btnLongUp.Width = (btnLongDown.Width = (btnLongAny.Width = num2));
			btnShortUp.Width = (btnShortDown.Width = (btnShortAny.Width = num2));
			gridBtns.RowDefinitions.Add(new RowDefinition());
			QuantityUpDown val4 = new QuantityUpDown();
			((FrameworkElement)val4).HorizontalAlignment = HorizontalAlignment.Stretch;
			((FrameworkElement)val4).VerticalAlignment = VerticalAlignment.Top;
			((FrameworkElement)val4).Margin = new Thickness(1.0, 1.0, 0.0, 0.0);
			((Control)val4).FontSize = textSettingSize;
			val4.IsPopupEnabled = false;
			((FrameworkElement)val4).Cursor = Cursors.Arrow;
			selectorOffsetShortAny = val4;
			((DependencyObject)(object)selectorOffsetShortAny).SetValue(Grid.RowProperty, (object)4);
			((DependencyObject)(object)selectorOffsetShortAny).SetValue(Grid.ColumnProperty, (object)1);
			gridBtns.Children.Add((UIElement)(object)selectorOffsetShortAny);
			QuantityUpDown val5 = new QuantityUpDown();
			((FrameworkElement)val5).HorizontalAlignment = HorizontalAlignment.Stretch;
			((FrameworkElement)val5).VerticalAlignment = VerticalAlignment.Top;
			((FrameworkElement)val5).Margin = new Thickness(1.0, 1.0, 0.0, 0.0);
			((Control)val5).FontSize = textSettingSize;
			val5.IsPopupEnabled = false;
			((FrameworkElement)val5).Cursor = Cursors.Arrow;
			selectorOffsetShortUp = val5;
			((DependencyObject)(object)selectorOffsetShortUp).SetValue(Grid.RowProperty, (object)4);
			((DependencyObject)(object)selectorOffsetShortUp).SetValue(Grid.ColumnProperty, (object)2);
			gridBtns.Children.Add((UIElement)(object)selectorOffsetShortUp);
			QuantityUpDown val6 = new QuantityUpDown();
			((FrameworkElement)val6).HorizontalAlignment = HorizontalAlignment.Stretch;
			((FrameworkElement)val6).VerticalAlignment = VerticalAlignment.Top;
			((FrameworkElement)val6).Margin = new Thickness(1.0, 1.0, 0.0, 0.0);
			((Control)val6).FontSize = textSettingSize;
			val6.IsPopupEnabled = false;
			((FrameworkElement)val6).Cursor = Cursors.Arrow;
			selectorOffsetShortDown = val6;
			((DependencyObject)(object)selectorOffsetShortDown).SetValue(Grid.RowProperty, (object)4);
			((DependencyObject)(object)selectorOffsetShortDown).SetValue(Grid.ColumnProperty, (object)3);
			gridBtns.Children.Add((UIElement)(object)selectorOffsetShortDown);
			gridBtns.RowDefinitions.Add(new RowDefinition());
			accountSelector = new ComboBox
			{
				ToolTip = null,
				Margin = new Thickness(1.0, 1.0, 0.0, 0.0),
				VerticalAlignment = VerticalAlignment.Stretch,
				VerticalContentAlignment = VerticalAlignment.Center,
				HorizontalContentAlignment = HorizontalAlignment.Center,
				FontSize = textSettingSize,
				DisplayMemberPath = "DisplayName",
				Cursor = Cursors.Arrow
			};
			accountSelector.SetValue(Grid.RowProperty, 5);
			accountSelector.SetValue(Grid.ColumnSpanProperty, 4);
			gridBtns.Children.Add(accountSelector);
			gridBtns.ColumnDefinitions.Add(new ColumnDefinition
			{
				Width = new GridLength(num)
			});
			gridBtns.ColumnDefinitions.Add(new ColumnDefinition
			{
				Width = new GridLength(num2)
			});
			gridBtns.ColumnDefinitions.Add(new ColumnDefinition
			{
				Width = new GridLength(num2)
			});
			gridBtns.ColumnDefinitions.Add(new ColumnDefinition
			{
				Width = new GridLength(num2)
			});
			gridBtns.SetValue(Grid.ColumnProperty, 2);
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
				gridBtns.Visibility = Visibility.Visible;
				btnMini.Visibility = Visibility.Collapsed;
			}
			else
			{
				gridBtns.Visibility = Visibility.Collapsed;
				btnMini.Visibility = Visibility.Visible;
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

		public Button CreateButton(string text, string tag, double textSize, Brush backgroundBrush, Brush foregroundBrush, int buttonWidth, string toolTip = null, int column = 0, int columnSpan = 0, int row = 0, int rowSpan = 0)
		{
			Button button = new Button
			{
				Content = text,
				MinWidth = 0.0,
				Foreground = foregroundBrush,
				Background = backgroundBrush,
				BorderBrush = backgroundBrush,
				HorizontalAlignment = HorizontalAlignment.Left,
				FontSize = textSize,
				Margin = new Thickness(1.0, 1.0, 0.0, 0.0),
				Cursor = Cursors.Hand,
				Tag = tag,
				ToolTip = toolTip,
				Focusable = false
			};
			if (buttonWidth > 0)
			{
				button.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
				Size size = button.DesiredSize;
				button.Width = Math.Max(Convert.ToInt32(size.Width + 3.0 * button.Padding.Left), buttonWidth);
				button.Height = Convert.ToInt32(size.Height + 3.0 * button.Padding.Top);
			}
			if (row > 0)
			{
				button.SetValue(Grid.RowProperty, row);
			}
			if (rowSpan > 0)
			{
				button.SetValue(Grid.RowSpanProperty, rowSpan);
			}
			if (column > 0)
			{
				button.SetValue(Grid.ColumnProperty, column);
			}
			if (columnSpan > 0)
			{
				button.SetValue(Grid.ColumnSpanProperty, columnSpan);
			}
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

	private const byte altKey = 18;

	private double currentMarketPrice;

	private int quantityPosition = -1;

	private bool exitCondExecuting;

	private const string nickname = "se:exc";

	private bool isUnlicensed = false;

	private System.Windows.Window alertWindow;

	private const string prefix = "ninZaSmartEscape";

	private const string indicatorName = "Smart Escape";

	private const string indicatorNameFull = "Smart Escape by ninZa.co";

	private bool isCharting;

	private bool isValidTab;

	private TabControl tabControl;

	private Brush chartBackground;

	private bool isUptrend;

	private ChartScale chartScale;

	private ninZaSmartEscape_AltState prevAltState;

	private ninZaSmartEscape_AltState currentAltState;

	private DispatcherTimer keyWatchTimer;

	private bool checkedKeysStatesFirstTime;

	private ChartTrader chartTrader;

	private bool chartTraderAvailable;

	private Account activeAccount;

	private BindingFlags flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

	private FieldInfo field;

	private MenuItem menuItemOco;

	private TifSelector selectorTif;

	private AccountSelector selectorAccount;

	private AtmStrategySelector chartTraderAtmSelector;

	private DraggablePanel controlPanel;


	[Display(Name = "Website", Order = 0, GroupName = "Developer")]
	public string Website => "ninZa.co";

	[Display(Name = "Update", Order = 10, GroupName = "Developer")]
	public string Update => "20 Aug 2025";

	[Display(Name = "Logo: Enabled", Order = 20, GroupName = "Developer")]
	public bool LogoEnabled { get; set; }

	[Display(Name = "Instruction: Enabled", Order = 30, GroupName = "Developer")]
	public bool InstructionEnabled { get; set; }

	[Display(Name = "Exit Condition: Enabled", Order = 0, GroupName = "General")]
	public bool ExitConditionEnabled { get; set; }

	[Display(Name = "Exit Condition: Active 'Til Canceled", Description = "Unexecuted EOB orders (due to unsatisfied conditions) remain active through bars,\nuntil they are either executed or manually canceled.", Order = 2, GroupName = "General")]
	public bool ExitConditionActiveTilCanceled { get; set; }

	[Display(Name = "Exit Condition Long: Enabled", Order = 10, GroupName = "General")]
	public bool ExitConditionLongEnabled { get; set; }

	[Display(Name = "Exit Condition Long: Bar Direction Any", Order = 12, GroupName = "General")]
	public bool ExitConditionLongAny { get; set; }

	[Display(Name = "Exit Condition Long: # Of Bars Any", Order = 14, GroupName = "General")]
	public int ExitConditionLongAnyNumberOfBars { get; set; }

	[Display(Name = "Exit Condition Long: Bar Direction Up", Order = 16, GroupName = "General")]
	public bool ExitConditionLongUp { get; set; }

	[Display(Name = "Exit Condition Long: # Of Bars Up", Order = 18, GroupName = "General")]
	public int ExitConditionLongUpNumberOfBars { get; set; }

	[Display(Name = "Exit Condition Long: Bar Direction Down", Order = 20, GroupName = "General")]
	public bool ExitConditionLongDown { get; set; }

	[Display(Name = "Exit Condition Long: # Of Bars Down", Order = 22, GroupName = "General")]
	public int ExitConditionLongDownNumberOfBars { get; set; }

	[Display(Name = "Exit Condition Short: Enabled", Order = 30, GroupName = "General")]
	public bool ExitConditionShortEnabled { get; set; }

	[Display(Name = "Exit Condition Short: Bar Direction Any", Order = 32, GroupName = "General")]
	public bool ExitConditionShortAny { get; set; }

	[Display(Name = "Exit Condition Short: # Of Bars Any", Order = 34, GroupName = "General")]
	public int ExitConditionShortAnyNumberOfBars { get; set; }

	[Display(Name = "Exit Condition Short: Bar Direction Up", Order = 36, GroupName = "General")]
	public bool ExitConditionShortUp { get; set; }

	[Display(Name = "Exit Condition Short: # Of Bars Up", Order = 38, GroupName = "General")]
	public int ExitConditionShortUpNumberOfBars { get; set; }

	[Display(Name = "Exit Condition Short: Bar Direction Down", Order = 40, GroupName = "General")]
	public bool ExitConditionShortDown { get; set; }

	[Display(Name = "Exit Condition Short: # Of Bars Down", Order = 42, GroupName = "General")]
	public int ExitConditionShortDownNumberOfBars { get; set; }

	[Display(Name = "Pending Order: Behind Or At Market", Order = 50, GroupName = "General")]
	public ninZaSmartEscape_OrderTypeBehindOrAtMarket OrderTypeBehindOrAtMarket { get; set; }

	[Display(Name = "Pending Order: In Front Of Market", Order = 52, GroupName = "General")]
	public ninZaSmartEscape_OrderTypeInFrontOfMarket OrderTypeInFrontOfMarket { get; set; }

	[Display(Name = "Pending Order: SLM Offset", Order = 54, GroupName = "General")]
	public int OrderSlmOffset { get; set; }

	[Display(Name = "Screen DPI", Order = 100, GroupName = "General")]
	public int ScreenDPI { get; set; }

	[Display(Name = "Info: Enabled", Order = 90, GroupName = "Graphics")]
	public bool InfoEnabled { get; set; }

	[Display(Name = "Info: Position", Order = 92, GroupName = "Graphics")]
	public TextPosition InfoPosition { get; set; }

	[Display(Name = "Info: Margin X", Order = 93, GroupName = "Graphics")]
	public int InfoMarginX { get; set; }

	[Display(Name = "Info: Margin Y", Order = 94, GroupName = "Graphics")]
	public int InfoMarginY { get; set; }

	[Display(Name = "Info: Color", Order = 95, GroupName = "Graphics")]
	[XmlIgnore]
	public Brush InfoBrush { get; set; }

	[Browsable(false)]
	public string InfoBrush_Serialize
	{
		get
		{
			return Serialize.BrushToString(InfoBrush);
		}
		set
		{
			InfoBrush = Serialize.StringToBrush(value);
		}
	}

	[Display(Name = "Info: Font", Order = 96, GroupName = "Graphics")]
	public SimpleFont InfoFont { get; set; }

	[Display(Name = "Minimized", Order = 0, GroupName = "Control Panel")]
	public bool CpMinimized { get; set; }

	[XmlIgnore]
	[Display(Name = "Button Exit: Condition Active", Order = 10, GroupName = "Control Panel")]
	public Brush CpButtonExitConditionActive { get; set; }

	[Browsable(false)]
	public string CpButtonExitConditionActive_Serialize
	{
		get
		{
			return Serialize.BrushToString(CpButtonExitConditionActive);
		}
		set
		{
			CpButtonExitConditionActive = Serialize.StringToBrush(value);
		}
	}

	[XmlIgnore]
	[Display(Name = "Button Exit: Condition Inactive", Order = 12, GroupName = "Control Panel")]
	public Brush CpButtonExitConditionInactive { get; set; }

	[Browsable(false)]
	public string CpButtonExitConditionInactive_Serialize
	{
		get
		{
			return Serialize.BrushToString(CpButtonExitConditionInactive);
		}
		set
		{
			CpButtonExitConditionInactive = Serialize.StringToBrush(value);
		}
	}

	[XmlIgnore]
	[Display(Name = "Button Exit Long: Condition Active", Order = 14, GroupName = "Control Panel")]
	public Brush CpButtonExitLongConditionActive { get; set; }

	[Browsable(false)]
	public string CpButtonExitLongConditionActive_Serialize
	{
		get
		{
			return Serialize.BrushToString(CpButtonExitLongConditionActive);
		}
		set
		{
			CpButtonExitLongConditionActive = Serialize.StringToBrush(value);
		}
	}

	[Display(Name = "Button Exit Long: Condition Inactive", Order = 16, GroupName = "Control Panel")]
	[XmlIgnore]
	public Brush CpButtonExitLongConditionInactive { get; set; }

	[Browsable(false)]
	public string CpButtonExitLongConditionInactive_Serialize
	{
		get
		{
			return Serialize.BrushToString(CpButtonExitLongConditionInactive);
		}
		set
		{
			CpButtonExitLongConditionInactive = Serialize.StringToBrush(value);
		}
	}

	[XmlIgnore]
	[Display(Name = "Button Exit Short: Condition Active", Order = 18, GroupName = "Control Panel")]
	public Brush CpButtonExitShortConditionActive { get; set; }

	[Browsable(false)]
	public string CpButtonExitShortConditionActive_Serialize
	{
		get
		{
			return Serialize.BrushToString(CpButtonExitShortConditionActive);
		}
		set
		{
			CpButtonExitShortConditionActive = Serialize.StringToBrush(value);
		}
	}

	[XmlIgnore]
	[Display(Name = "Button Exit Short: Condition Inactive", Order = 20, GroupName = "Control Panel")]
	public Brush CpButtonExitShortConditionInactive { get; set; }

	[Browsable(false)]
	public string CpButtonExitShortConditionInactive_Serialize
	{
		get
		{
			return Serialize.BrushToString(CpButtonExitShortConditionInactive);
		}
		set
		{
			CpButtonExitShortConditionInactive = Serialize.StringToBrush(value);
		}
	}

	[Display(Name = "Button Long: Any Active", Order = 30, GroupName = "Control Panel")]
	[XmlIgnore]
	public Brush CpButtonLongAnyActive { get; set; }

	[Browsable(false)]
	public string CpButtonLongAnyActive_Serialize
	{
		get
		{
			return Serialize.BrushToString(CpButtonLongAnyActive);
		}
		set
		{
			CpButtonLongAnyActive = Serialize.StringToBrush(value);
		}
	}

	[XmlIgnore]
	[Display(Name = "Button Long: Any Inactive", Order = 32, GroupName = "Control Panel")]
	public Brush CpButtonLongAnyInactive { get; set; }

	[Browsable(false)]
	public string CpButtonLongAnyInactive_Serialize
	{
		get
		{
			return Serialize.BrushToString(CpButtonLongAnyInactive);
		}
		set
		{
			CpButtonLongAnyInactive = Serialize.StringToBrush(value);
		}
	}

	[Display(Name = "Button Long: Up Active", Order = 34, GroupName = "Control Panel")]
	[XmlIgnore]
	public Brush CpButtonLongUpActive { get; set; }

	[Browsable(false)]
	public string CpButtonLongUpActive_Serialize
	{
		get
		{
			return Serialize.BrushToString(CpButtonLongUpActive);
		}
		set
		{
			CpButtonLongUpActive = Serialize.StringToBrush(value);
		}
	}

	[Display(Name = "Button Long: Up Inactive", Order = 36, GroupName = "Control Panel")]
	[XmlIgnore]
	public Brush CpButtonLongUpInactive { get; set; }

	[Browsable(false)]
	public string CpButtonLongUpInactive_Serialize
	{
		get
		{
			return Serialize.BrushToString(CpButtonLongUpInactive);
		}
		set
		{
			CpButtonLongUpInactive = Serialize.StringToBrush(value);
		}
	}

	[Display(Name = "Button Long: Down Active", Order = 38, GroupName = "Control Panel")]
	[XmlIgnore]
	public Brush CpButtonLongDownActive { get; set; }

	[Browsable(false)]
	public string CpButtonLongDownActive_Serialize
	{
		get
		{
			return Serialize.BrushToString(CpButtonLongDownActive);
		}
		set
		{
			CpButtonLongDownActive = Serialize.StringToBrush(value);
		}
	}

	[Display(Name = "Button Long: Down Inactive", Order = 40, GroupName = "Control Panel")]
	[XmlIgnore]
	public Brush CpButtonLongDownInactive { get; set; }

	[Browsable(false)]
	public string CpButtonLongDownInactive_Serialize
	{
		get
		{
			return Serialize.BrushToString(CpButtonLongDownInactive);
		}
		set
		{
			CpButtonLongDownInactive = Serialize.StringToBrush(value);
		}
	}

	[XmlIgnore]
	[Display(Name = "Button Short: Any Active", Order = 30, GroupName = "Control Panel")]
	public Brush CpButtonShortAnyActive { get; set; }

	[Browsable(false)]
	public string CpButtonShortAnyActive_Serialize
	{
		get
		{
			return Serialize.BrushToString(CpButtonShortAnyActive);
		}
		set
		{
			CpButtonShortAnyActive = Serialize.StringToBrush(value);
		}
	}

	[XmlIgnore]
	[Display(Name = "Button Short: Any Inactive", Order = 32, GroupName = "Control Panel")]
	public Brush CpButtonShortAnyInactive { get; set; }

	[Browsable(false)]
	public string CpButtonShortAnyInactive_Serialize
	{
		get
		{
			return Serialize.BrushToString(CpButtonShortAnyInactive);
		}
		set
		{
			CpButtonShortAnyInactive = Serialize.StringToBrush(value);
		}
	}

	[XmlIgnore]
	[Display(Name = "Button Short: Up Active", Order = 34, GroupName = "Control Panel")]
	public Brush CpButtonShortUpActive { get; set; }

	[Browsable(false)]
	public string CpButtonShortUpActive_Serialize
	{
		get
		{
			return Serialize.BrushToString(CpButtonShortUpActive);
		}
		set
		{
			CpButtonShortUpActive = Serialize.StringToBrush(value);
		}
	}

	[XmlIgnore]
	[Display(Name = "Button Short: Up Inactive", Order = 36, GroupName = "Control Panel")]
	public Brush CpButtonShortUpInactive { get; set; }

	[Browsable(false)]
	public string CpButtonShortUpInactive_Serialize
	{
		get
		{
			return Serialize.BrushToString(CpButtonShortUpInactive);
		}
		set
		{
			CpButtonShortUpInactive = Serialize.StringToBrush(value);
		}
	}

	[Display(Name = "Button Short: Down Active", Order = 38, GroupName = "Control Panel")]
	[XmlIgnore]
	public Brush CpButtonShortDownActive { get; set; }

	[Browsable(false)]
	public string CpButtonShortDownActive_Serialize
	{
		get
		{
			return Serialize.BrushToString(CpButtonShortDownActive);
		}
		set
		{
			CpButtonShortDownActive = Serialize.StringToBrush(value);
		}
	}

	[XmlIgnore]
	[Display(Name = "Button Short: Down Inactive", Order = 40, GroupName = "Control Panel")]
	public Brush CpButtonShortDownInactive { get; set; }

	[Browsable(false)]
	public string CpButtonShortDownInactive_Serialize
	{
		get
		{
			return Serialize.BrushToString(CpButtonShortDownInactive);
		}
		set
		{
			CpButtonShortDownInactive = Serialize.StringToBrush(value);
		}
	}

	[Display(Name = "Title: Text Color", Order = 14, GroupName = "Control Panel")]
	[XmlIgnore]
	public Brush CpTitleTextBrush { get; set; }

	[Browsable(false)]
	public string CpTitleTextBrush_Serialize
	{
		get
		{
			return Serialize.BrushToString(CpTitleTextBrush);
		}
		set
		{
			CpTitleTextBrush = Serialize.StringToBrush(value);
		}
	}

	[Display(Name = "Button: Width", Order = 46, GroupName = "Control Panel")]
	public int CpMinimumButtonWidth { get; set; }

	[Display(Name = "Text: Execution Size", Order = 54, GroupName = "Control Panel")]
	public int CpTextExecutionSize { get; set; }

	[Display(Name = "Text: Execution Color", Order = 56, GroupName = "Control Panel")]
	[XmlIgnore]
	public Brush CpTextExecutionBrush { get; set; }

	[Browsable(false)]
	public string CpTextExecutionBrush_Serialize
	{
		get
		{
			return Serialize.BrushToString(CpTextExecutionBrush);
		}
		set
		{
			CpTextExecutionBrush = Serialize.StringToBrush(value);
		}
	}

	[Display(Name = "Text: Setting Size", Order = 62, GroupName = "Control Panel")]
	public int CpTextSettingSize { get; set; }

	[XmlIgnore]
	[Display(Name = "Drag Bar: Color", Order = 70, GroupName = "Control Panel")]
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

	[Display(Name = "Position: Alignment", Order = 80, GroupName = "Control Panel")]
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

	[Display(Name = "Position: Margin Left", Order = 82, GroupName = "Control Panel")]
	public double CpPositionMarginLeft { get; set; }

	[Display(Name = "Position: Margin Top", Order = 84, GroupName = "Control Panel")]
	public double CpPositionMarginTop { get; set; }

	[Display(Name = "Position: Margin Right", Order = 86, GroupName = "Control Panel")]
	public double CpPositionMarginRight { get; set; }

	[Display(Name = "Position: Margin Bottom", Order = 88, GroupName = "Control Panel")]
	public double CpPositionMarginBottom { get; set; }

	[Display(Name = "Z Order", Order = 0, GroupName = "Special")]
	public int IndicatorZOrder { get; set; }

	[Display(Name = "User Note", Order = 10, GroupName = "Special")]
	public string UserNote { get; set; }

	[Display(Name = "OcoId", Order = 0, GroupName = "Critical")]
	public string OcoId { get; set; }

	[Display(Name = "Last Bar Index Any", Order = 10, GroupName = "Critical")]
	public int LastBarIndexAny { get; set; }

	[Display(Name = "Last Bar Index Up", Order = 12, GroupName = "Critical")]
	public int LastBarIndexUp { get; set; }

	[Display(Name = "Last Bar Index Down", Order = 14, GroupName = "Critical")]
	public int LastBarIndexDown { get; set; }

	[Display(Name = "Active Account", Order = 20, GroupName = "Critical")]
	public string ActiveAccountName { get; set; }

	[Display(Name = "Pending: Buy", Order = 10, GroupName = "Pending Order")]
	[XmlIgnore]
	public Brush PendingBuy { get; set; }

	[Browsable(false)]
	public string PendingBuy_Serialize
	{
		get
		{
			return Serialize.BrushToString(PendingBuy);
		}
		set
		{
			PendingBuy = Serialize.StringToBrush(value);
		}
	}

	[XmlIgnore]
	[Display(Name = "Pending: Sell", Order = 11, GroupName = "Pending Order")]
	public Brush PendingSell { get; set; }

	[Browsable(false)]
	public string PendingSell_Serialize
	{
		get
		{
			return Serialize.BrushToString(PendingSell);
		}
		set
		{
			PendingSell = Serialize.StringToBrush(value);
		}
	}

	[Display(Name = "Order Status: Line Enabled", Order = 30, GroupName = "Pending Order")]
	public bool OrderStatusLineEnabled { get; set; }

	[Display(Name = "Order Status: Line Style", Order = 31, GroupName = "Pending Order")]
	public Stroke OrderStatusLineStroke { get; set; }

	[Display(Name = "Order Status: Price Enabled", Order = 32, GroupName = "Pending Order")]
	public bool OrderStatusPriceEnabled { get; set; }

	[Display(Name = "Order Status: Order Type Enabled", Order = 32, GroupName = "Pending Order")]
	public bool OrderStatusOrderTypeEnabled { get; set; }

	[Display(Name = "Order Status: Text Font", Order = 34, GroupName = "Pending Order")]
	public SimpleFont OrderStatusTextFont { get; set; }

	public override string DisplayName
	{
		get
		{
			if (!(Parent is MarketAnalyzerColumnBase))
			{
				return "Smart Escape by ninZa.co" + GetUserNote();
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
								if (tabControl != null)
								{
									tabControl.SelectionChanged -= OnTabSwitch;
									tabControl = null;
								}
								((UIElement)(object)ChartPanel).MouseMove -= OnMouseMove;
								((UIElement)(object)ChartPanel).MouseLeave -= OnMouseLeave;
								((UIElement)(object)ChartPanel).MouseEnter -= OnMouseEnter;
								((UIElement)(object)ChartPanel).MouseDown -= OnMouseDown;
								((UIElement)(object)ChartPanel).KeyDown -= OnKeyDown;
								((UIElement)(object)ChartPanel).KeyUp -= OnKeyUp;
								if (chartTrader != null)
								{
									chartTrader = null;
								}
								if (selectorAccount != null)
								{
									if (activeAccount != null)
									{
										activeAccount.OrderUpdate -= ActiveAccount_OrderUpdate;
										activeAccount.PositionUpdate -= ActiveAccount_PositionUpdate;
										activeAccount = null;
									}
									selectorAccount = null;
								}
								if (selectorTif != null)
								{
									selectorTif = null;
								}
								if (menuItemOco != null)
								{
									menuItemOco = null;
								}
								if (chartTraderAtmSelector != null)
								{
									chartTraderAtmSelector = null;
								}
								if (alertWindow != null)
								{
									((Window)(object)alertWindow).Close();
									alertWindow = null;
								}
								if (controlPanel != null)
								{
									controlPanel.accountSelector.SelectionChanged -= OnAccountSelectorSelectionChanged;
									controlPanel.btnExitConditionEnabled.Click -= OnButtonExitConditionEnabledClick;
									controlPanel.drag.DragDelta -= OnControlPanelDragDelta;
									controlPanel.btnMini.Click -= OnControlPanelBtnMiniClick;
									controlPanel.drag.MouseDoubleClick -= OnControlPanelDragDoubleClick;
									controlPanel.btnLong.Click -= OnBtnBarDirectionLongClick;
									controlPanel.btnLongAny.Click -= OnBtnBarDirectionLongClick;
									controlPanel.btnLongUp.Click -= OnBtnBarDirectionLongClick;
									controlPanel.btnLongDown.Click -= OnBtnBarDirectionLongClick;
									controlPanel.btnShort.Click -= OnBtnBarDirectionShortClick;
									controlPanel.btnShortAny.Click -= OnBtnBarDirectionShortClick;
									controlPanel.btnShortUp.Click -= OnBtnBarDirectionShortClick;
									controlPanel.btnShortDown.Click -= OnBtnBarDirectionShortClick;
									controlPanel.selectorOffsetLongAny.ValueChanged -= OnOffsetLongAnyChange;
									controlPanel.selectorOffsetLongUp.ValueChanged -= OnOffsetLongUpChange;
									controlPanel.selectorOffsetLongDown.ValueChanged -= OnOffsetLongDownChange;
									controlPanel.selectorOffsetShortAny.ValueChanged -= OnOffsetShortAnyChange;
									controlPanel.selectorOffsetShortUp.ValueChanged -= OnOffsetShortUpChange;
									controlPanel.selectorOffsetShortDown.ValueChanged -= OnOffsetShortDownChange;
									controlPanel = null;
								}
							});
						}
						if (keyWatchTimer != null)
						{
							keyWatchTimer.Stop();
							keyWatchTimer.Tick -= OnKeyWatchTimerTick;
							keyWatchTimer = null;
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
						if (!isCharting)
						{
							return;
						}
						ChartControl.Dispatcher.InvokeAsync(delegate
						{
							if (chartTrader == null)
							{
								ref ChartTrader reference = ref chartTrader;
								DependencyObject dependencyObject = Extensions.FindFirst((DependencyObject)Window.GetWindow(((FrameworkElement)(object)ChartControl).Parent), "ChartWindowChartTraderControl");
								reference = (ChartTrader)(object)((dependencyObject is ChartTrader) ? dependencyObject : null);
								chartTraderAvailable = chartTrader != null;
								if (chartTraderAvailable)
								{
									Type type = ((object)chartTrader).GetType();
									field = type.GetField("cbxTif", flags);
									if (field != null)
									{
										ref TifSelector reference2 = ref selectorTif;
										object value = field.GetValue(chartTrader);
										reference2 = (TifSelector)((value is TifSelector) ? value : null);
									}
									field = type.GetField("mnuOcoOrderItem", flags);
									if (field != null)
									{
										menuItemOco = field.GetValue(chartTrader) as MenuItem;
									}
									field = type.GetField("cbxStrategySelector", flags);
									if (field != null)
									{
										ref AtmStrategySelector reference3 = ref chartTraderAtmSelector;
										object value2 = field.GetValue(chartTrader);
										reference3 = (AtmStrategySelector)((value2 is AtmStrategySelector) ? value2 : null);
									}
									field = type.GetField("cbxAccounts", flags);
									if (field != null)
									{
										ref AccountSelector reference4 = ref selectorAccount;
										object value3 = field.GetValue(chartTrader);
										reference4 = (AccountSelector)((value3 is AccountSelector) ? value3 : null);
										if (selectorAccount != null)
										{
											if (!string.IsNullOrWhiteSpace(ActiveAccountName))
											{
												Account accountByName = GetAccountByName(ActiveAccountName);
												if (accountByName != null)
												{
													if (((ItemsControl)(object)selectorAccount).Items.Contains(accountByName))
													{
														activeAccount = accountByName;
													}
													if (selectorAccount.SelectedAccount != activeAccount)
													{
														selectorAccount.SelectedAccount = activeAccount;
													}
												}
											}
											else
											{
												activeAccount = selectorAccount.SelectedAccount;
											}
										}
									}
									SaveActiveAccountName();
								}
							}
							if (controlPanel == null)
							{
								Thickness thickness = new Thickness(CpPositionMarginLeft, CpPositionMarginTop, CpPositionMarginRight, CpPositionMarginBottom);
								Brush executionLongAny = ((!ExitConditionLongAny) ? CpButtonLongAnyInactive : CpButtonLongAnyActive);
								Brush executionLongUp = ((!ExitConditionLongUp) ? CpButtonLongUpInactive : CpButtonLongUpActive);
								Brush executionLongDown = ((!ExitConditionLongDown) ? CpButtonLongDownInactive : CpButtonLongDownActive);
								Brush executionShortAny = ((!ExitConditionShortAny) ? CpButtonShortAnyInactive : CpButtonShortAnyActive);
								Brush executionShortUp = ((!ExitConditionShortUp) ? CpButtonShortUpInactive : CpButtonShortUpActive);
								Brush executionShortDown = ((!ExitConditionShortDown) ? CpButtonShortDownInactive : CpButtonShortDownActive);
								Brush exitConditionEnabledBackground = ((!ExitConditionEnabled) ? CpButtonExitConditionInactive : CpButtonExitConditionActive);
								Brush exitConditionLongEnabledBackground = ((!ExitConditionLongEnabled) ? CpButtonExitLongConditionInactive : CpButtonExitLongConditionActive);
								Brush exitConditionShortEnabledBackground = ((!ExitConditionShortEnabled) ? CpButtonExitShortConditionInactive : CpButtonExitShortConditionActive);
								controlPanel = new DraggablePanel(CpMinimized, CpMinimumButtonWidth, exitConditionEnabledBackground, CpTitleTextBrush, exitConditionLongEnabledBackground, exitConditionShortEnabledBackground, executionLongAny, executionLongUp, executionLongDown, executionShortAny, executionShortUp, executionShortDown, CpTextExecutionSize, CpTextExecutionBrush, CpTextSettingSize, CpDragBrush, CpPositionAlignment, thickness);
								controlPanel.gridBtns.Background = chartBackground;
								if (selectorAccount != null)
								{
									IEnumerator<Account> enumerator = Account.All.GetEnumerator();
									while (enumerator.MoveNext())
									{
										Account current = enumerator.Current;
										if (((ItemsControl)(object)selectorAccount).Items.Contains(current))
										{
											controlPanel.accountSelector.Items.Add(current);
										}
									}
								}
								if (activeAccount != null)
								{
									activeAccount.OrderUpdate += ActiveAccount_OrderUpdate;
									activeAccount.PositionUpdate += ActiveAccount_PositionUpdate;
									controlPanel.accountSelector.SelectedItem = activeAccount;
								}
								controlPanel.selectorOffsetLongAny.Value = ExitConditionLongAnyNumberOfBars;
								controlPanel.selectorOffsetLongUp.Value = ExitConditionLongUpNumberOfBars;
								controlPanel.selectorOffsetLongDown.Value = ExitConditionLongDownNumberOfBars;
								controlPanel.selectorOffsetShortAny.Value = ExitConditionShortAnyNumberOfBars;
								controlPanel.selectorOffsetShortUp.Value = ExitConditionShortUpNumberOfBars;
								controlPanel.selectorOffsetShortDown.Value = ExitConditionShortDownNumberOfBars;
								controlPanel.drag.DragDelta += OnControlPanelDragDelta;
								controlPanel.btnMini.Click += OnControlPanelBtnMiniClick;
								controlPanel.drag.MouseDoubleClick += OnControlPanelDragDoubleClick;
								controlPanel.btnExitConditionEnabled.Click += OnButtonExitConditionEnabledClick;
								controlPanel.btnLong.Click += OnBtnBarDirectionLongClick;
								controlPanel.btnLongAny.Click += OnBtnBarDirectionLongClick;
								controlPanel.btnLongUp.Click += OnBtnBarDirectionLongClick;
								controlPanel.btnLongDown.Click += OnBtnBarDirectionLongClick;
								controlPanel.btnShort.Click += OnBtnBarDirectionShortClick;
								controlPanel.btnShortAny.Click += OnBtnBarDirectionShortClick;
								controlPanel.btnShortUp.Click += OnBtnBarDirectionShortClick;
								controlPanel.btnShortDown.Click += OnBtnBarDirectionShortClick;
								controlPanel.selectorOffsetLongAny.ValueChanged += OnOffsetLongAnyChange;
								controlPanel.selectorOffsetLongUp.ValueChanged += OnOffsetLongUpChange;
								controlPanel.selectorOffsetLongDown.ValueChanged += OnOffsetLongDownChange;
								controlPanel.selectorOffsetShortAny.ValueChanged += OnOffsetShortAnyChange;
								controlPanel.selectorOffsetShortUp.ValueChanged += OnOffsetShortUpChange;
								controlPanel.selectorOffsetShortDown.ValueChanged += OnOffsetShortDownChange;
								controlPanel.accountSelector.SelectionChanged += OnAccountSelectorSelectionChanged;
								UserControlCollection.Add(controlPanel);
							}
							if (tabControl == null)
							{
								ref TabControl reference5 = ref tabControl;
								Window window = Window.GetWindow(((FrameworkElement)(object)ChartControl).Parent);
								reference5 = ((NTWindow)((window is Chart) ? window : null)).MainTabControl;
								tabControl.SelectionChanged += OnTabSwitch;
								ref bool reference6 = ref isValidTab;
								object content = (tabControl.SelectedItem as TabItem).Content;
								reference6 = ((ChartTab)((content is ChartTab) ? content : null)).ChartControl == ChartControl;
							}
							((UIElement)(object)ChartPanel).MouseMove += OnMouseMove;
							((UIElement)(object)ChartPanel).MouseLeave += OnMouseLeave;
							((UIElement)(object)ChartPanel).MouseEnter += OnMouseEnter;
							((UIElement)(object)ChartPanel).MouseDown += OnMouseDown;
							((UIElement)(object)ChartPanel).KeyDown += OnKeyDown;
							((UIElement)(object)ChartPanel).KeyUp += OnKeyUp;
							if (ExitConditionEnabled && (ExitConditionLongEnabled || ExitConditionShortEnabled))
							{
								Position position = GetPosition();
								if (position != null && (int)position.MarketPosition != 1)
								{
									quantityPosition = position.Quantity;
									if (isCharting)
									{
										ChartControl.Dispatcher.InvokeAsync(delegate
										{
											OcoId = GetEffOcoId();
										});
									}
									if (LastBarIndexAny < 0 && ExitConditionLongAny)
									{
										LastBarIndexAny = BarsArray[0].Count - 1;
									}
								}
							}
						});
					}
				}
				else
				{
					isUnlicensed = false;
				}
			}
			else
			{
				Calculate = (Calculate)2;
				DeactivateModifierKeysStates();
				keyWatchTimer = new DispatcherTimer();
				keyWatchTimer.Interval = TimeSpan.FromMilliseconds(100.0);
				keyWatchTimer.Tick += OnKeyWatchTimerTick;
			}
		}
		else
		{
			Description = string.Empty;
			Name = "ninZaSmartEscape";
			Calculate = (Calculate)2;
			IsOverlay = true;
			DisplayInDataBox = true;
			DrawOnPricePanel = true;
			DrawHorizontalGridLines = true;
			DrawVerticalGridLines = true;
			PaintPriceMarkers = true;
			ScaleJustification = (ScaleJustification)1;
			IsSuspendedWhileInactive = false;
			BarsRequiredToPlot = 0;
			LogoEnabled = true;
			InstructionEnabled = true;
			ExitConditionEnabled = false;
			ExitConditionActiveTilCanceled = true;
			ExitConditionLongEnabled = true;
			ExitConditionLongAny = true;
			ExitConditionLongAnyNumberOfBars = 10;
			ExitConditionLongUp = true;
			ExitConditionLongUpNumberOfBars = 10;
			ExitConditionLongDown = true;
			ExitConditionLongDownNumberOfBars = 10;
			ExitConditionShortEnabled = true;
			ExitConditionShortAny = true;
			ExitConditionShortAnyNumberOfBars = 10;
			ExitConditionShortUp = true;
			ExitConditionShortUpNumberOfBars = 10;
			ExitConditionShortDown = true;
			ExitConditionShortDownNumberOfBars = 10;
			OrderTypeInFrontOfMarket = ninZaSmartEscape_OrderTypeInFrontOfMarket.STP;
			OrderSlmOffset = 0;
			ScreenDPI = 99;
			InfoEnabled = true;
			InfoPosition = TextPosition.TopLeft;
			InfoMarginX = 10;
			InfoMarginY = 20;
			InfoBrush = Brushes.Orange;
			InfoFont = new SimpleFont("Arial", 24);
			CpMinimized = false;
			CpButtonExitConditionActive = Brushes.LimeGreen;
			CpButtonExitConditionInactive = Brushes.Gray;
			CpButtonExitLongConditionActive = Brushes.DodgerBlue;
			CpButtonExitLongConditionInactive = Brushes.LightSkyBlue;
			CpButtonLongAnyActive = Brushes.DarkOrange;
			CpButtonLongAnyInactive = Brushes.NavajoWhite;
			CpButtonLongUpActive = Brushes.DodgerBlue;
			CpButtonLongUpInactive = Brushes.PaleTurquoise;
			CpButtonLongDownActive = Brushes.HotPink;
			CpButtonLongDownInactive = Brushes.Thistle;
			CpButtonExitShortConditionActive = Brushes.HotPink;
			CpButtonExitShortConditionInactive = Brushes.Thistle;
			CpButtonShortAnyActive = Brushes.DarkOrange;
			CpButtonShortAnyInactive = Brushes.NavajoWhite;
			CpButtonShortUpActive = Brushes.DodgerBlue;
			CpButtonShortUpInactive = Brushes.PaleTurquoise;
			CpButtonShortDownActive = Brushes.HotPink;
			CpButtonShortDownInactive = Brushes.Thistle;
			CpTitleTextBrush = Brushes.White;
			CpMinimumButtonWidth = 50;
			CpTextExecutionSize = 13;
			CpTextExecutionBrush = Brushes.White;
			CpTextSettingSize = 13;
			CpDragBrush = Brushes.LimeGreen;
			CpPositionAlignment = TextPosition.TopLeft;
			CpPositionMarginLeft = 5.0;
			CpPositionMarginTop = 5.0;
			CpPositionMarginRight = 5.0;
			CpPositionMarginBottom = 5.0;
			PendingBuy = Brushes.DodgerBlue;
			PendingSell = Brushes.HotPink;
			OrderStatusLineEnabled = true;
			OrderStatusLineStroke = new Stroke(Brushes.Transparent, (DashStyleHelper)4, 2f);
			OrderStatusPriceEnabled = true;
			OrderStatusTextFont = new SimpleFont("Arial", 18);
			OrderStatusOrderTypeEnabled = true;
			IndicatorZOrder = 0;
			UserNote = "instrument (period)";
			OcoId = null;
			LastBarIndexDown = -1;
			LastBarIndexUp = -1;
			LastBarIndexAny = -1;
			ActiveAccountName = string.Empty;
		}
	}

	private void SaveActiveAccountName()
	{
		if (activeAccount != null && activeAccount.Name != ActiveAccountName)
		{
			ActiveAccountName = activeAccount.Name;
		}
	}

	private Account GetAccountByName(string accountName)
	{
		IEnumerator<Account> enumerator = Account.All.GetEnumerator();
		while (enumerator.MoveNext())
		{
			Account current = enumerator.Current;
			if (current.DisplayName == accountName)
			{
				return current;
			}
		}
		return null;
	}

	protected override void OnBarUpdate()
	{
		if (State == State.DataLoaded || CurrentBar == 0)
		{
			return;
		}
		currentMarketPrice = Close[0];
		double num = Close[1];
		if (!IsFirstTickOfBar)
		{
			return;
		}
		if (!ExitConditionEnabled || (!ExitConditionLongEnabled && !ExitConditionShortEnabled))
		{
			return;
		}
		bool flag = false;
		int num2 = MathExtentions.ApproxCompare(num, Open[1]);
		bool flag2 = num2 == 0;
		if (CurrentBar != 1)
		{
			if (!isUptrend || (num2 >= 0 && (!flag2 || MathExtentions.ApproxCompare(num, Close[2]) >= 0)))
			{
				if (!isUptrend && (num2 > 0 || (flag2 && MathExtentions.ApproxCompare(num, Close[2]) > 0)))
				{
					flag = true;
					isUptrend = true;
				}
			}
			else
			{
				flag = true;
				isUptrend = false;
			}
			Position position = GetPosition();
			if (position == null)
			{
				return;
			}
			MarketPosition marketPosition = position.MarketPosition;
			if ((int)marketPosition == 2)
			{
				return;
			}
			if ((int)marketPosition != 0)
			{
				if (!ExitConditionShortAny && !ExitConditionShortUp && !ExitConditionShortDown)
				{
					return;
				}
				if (ExitConditionShortUp)
				{
					if (flag)
					{
						LastBarIndexUp = -1;
					}
					if (isUptrend && LastBarIndexUp < 0)
					{
						LastBarIndexUp = CurrentBar;
					}
				}
				if (ExitConditionShortDown)
				{
					if (flag)
					{
						LastBarIndexDown = -1;
					}
					if (!isUptrend && LastBarIndexDown < 0)
					{
						LastBarIndexDown = CurrentBar;
					}
				}
				bool flag3 = false;
				if (ExitConditionShortAny && LastBarIndexAny >= 0)
				{
					flag3 = CurrentBar - LastBarIndexAny >= ExitConditionShortAnyNumberOfBars;
				}
				if (!flag3 && ExitConditionShortUp && LastBarIndexUp >= 0)
				{
					flag3 = CurrentBar - LastBarIndexUp >= ExitConditionShortUpNumberOfBars - 1;
				}
				if (!flag3 && ExitConditionShortDown && LastBarIndexDown >= 0)
				{
					flag3 = CurrentBar - LastBarIndexDown >= ExitConditionShortDownNumberOfBars - 1;
				}
				if (!flag3)
				{
					return;
				}
				if (isCharting)
				{
					ChartControl.Dispatcher.InvokeAsync(delegate
					{
						position.Close((string)null);
					});
				}
				OcoId = null;
				LastBarIndexDown = -1;
				LastBarIndexUp = -1;
				LastBarIndexAny = -1;
			}
			else
			{
				if (!ExitConditionLongAny && !ExitConditionLongUp && !ExitConditionLongDown)
				{
					return;
				}
				if (ExitConditionLongUp)
				{
					if (flag)
					{
						LastBarIndexUp = -1;
					}
					if (isUptrend && LastBarIndexUp < 0)
					{
						LastBarIndexUp = CurrentBar;
					}
				}
				if (ExitConditionLongDown)
				{
					if (flag)
					{
						LastBarIndexDown = -1;
					}
					if (!isUptrend && LastBarIndexDown < 0)
					{
						LastBarIndexDown = CurrentBar;
					}
				}
				bool flag4 = false;
				if (ExitConditionLongAny && LastBarIndexAny >= 0)
				{
					flag4 = CurrentBar - LastBarIndexAny >= ExitConditionLongAnyNumberOfBars;
				}
				if (!flag4 && ExitConditionLongUp && LastBarIndexUp >= 0)
				{
					flag4 = CurrentBar - LastBarIndexUp >= ExitConditionLongUpNumberOfBars - 1;
				}
				if (!flag4 && ExitConditionLongDown && LastBarIndexDown >= 0)
				{
					flag4 = CurrentBar - LastBarIndexDown >= ExitConditionLongDownNumberOfBars - 1;
				}
				if (!flag4)
				{
					return;
				}
				if (isCharting)
				{
					ChartControl.Dispatcher.InvokeAsync(delegate
					{
						position.Close((string)null);
					});
				}
				OcoId = null;
				LastBarIndexDown = -1;
				LastBarIndexUp = -1;
				LastBarIndexAny = -1;
			}
		}
		else
		{
			isUptrend = MathExtentions.ApproxCompare(Close[0], num) > 0;
		}
	}

	private Position GetPosition()
	{
		if (activeAccount != null)
		{
			Collection<Position> positions = activeAccount.Positions;
			if (positions == null || positions.Count <= 0)
			{
				return null;
			}
			IEnumerator<Position> enumerator = positions.GetEnumerator();
			while (enumerator.MoveNext())
			{
				Position current = enumerator.Current;
				if (((object)current.Instrument).Equals((object)Instrument))
				{
					return current;
				}
			}
			return null;
		}
		return null;
	}

	protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
	{
		if (isCharting)
		{
			if (!checkedKeysStatesFirstTime)
			{
				UpdateModifierKeysStates();
				checkedKeysStatesFirstTime = true;
			}
			base.OnRender(chartControl, chartScale);
			if (!IsInHitTest)
			{
				this.chartScale = chartScale;
				PrintOrderStatus();
			}
		}
	}

	private void PrintOrderStatus()
	{
		if (!((UIElement)(object)ChartPanel).IsMouseOver || currentAltState == ninZaSmartEscape_AltState.Null || currentAltState != ninZaSmartEscape_AltState.OnlyALT || (!OrderStatusLineEnabled && !OrderStatusPriceEnabled && !OrderStatusOrderTypeEnabled && !InfoEnabled))
		{
			return;
		}
		Position position = GetPosition();
		if (position == null)
		{
			return;
		}
		DrawInfo();
		double effMousePrice = GetEffMousePrice();
		MarketPosition marketPosition = position.MarketPosition;
		if ((int)marketPosition != 2)
		{
			bool flag;
			string text = ((!(flag = marketPosition == MarketPosition.Short)) ? "▼" : "▲");
			if (OrderStatusOrderTypeEnabled)
			{
				string empty = string.Empty;
				int num = MathExtentions.ApproxCompare(effMousePrice, currentMarketPrice);
				empty = ((!flag) ? ((num < 0) ? (empty + OrderTypeInFrontOfMarket) : (empty + OrderTypeBehindOrAtMarket)) : ((num > 0) ? (empty + OrderTypeInFrontOfMarket) : (empty + OrderTypeBehindOrAtMarket)));
				text = ((chartTrader == null) ? (text + " " + empty) : (text + " " + empty + " " + ((chartTrader.Quantity != 0) ? chartTrader.Quantity.ToString() : string.Empty)));
			}
			if (OrderStatusPriceEnabled)
			{
				text = text + ((!OrderStatusOrderTypeEnabled) ? " " : " @ ") + FormatPriceMarker(effMousePrice);
			}
			float num2 = (float)OrderStatusTextFont.Size / 2f;
			float num3 = ChartPanel.X + ChartPanel.W;
			float num4 = chartScale.GetYByValue(effMousePrice);
			Brush brush = ((!flag) ? PendingSell : PendingBuy);
			DrawTextOnChart(text, OrderStatusTextFont, num3, num4, -1, 0, brush, ScreenDPI, RenderTarget);
			if (OrderStatusLineEnabled)
			{
				float width = (float)ComputeTextSize(text, OrderStatusTextFont, ScreenDPI).Width;
				Vector2 val = new Vector2((float)ChartPanel.X, num4);
				Vector2 val2 = new Vector2(num3 - num2 - width - num2 / 2f - 1f, num4);
				RenderTarget.DrawLine(val, val2, DxExtensions.ToDxBrush(brush, RenderTarget), OrderStatusLineStroke.Width, OrderStatusLineStroke.StrokeStyle);
			}
		}
	}

	private void DrawInfo()
	{
		if (ChartControl == null || chartTrader == null || !InfoEnabled)
		{
			return;
		}
		float num;
		float num2;
		int num3;
		int num4;
		if (InfoPosition != TextPosition.TopLeft)
		{
			if (InfoPosition != TextPosition.TopRight)
			{
				if (InfoPosition != TextPosition.BottomLeft)
				{
					if (InfoPosition != TextPosition.BottomRight)
					{
						num = ChartPanel.X + ChartPanel.W / 2 + InfoMarginX;
						num2 = ChartPanel.Y + ChartPanel.H / 2 + InfoMarginY;
						num3 = 0;
						num4 = 0;
					}
					else
					{
						num = ChartPanel.X + ChartPanel.W - InfoMarginX;
						num2 = ChartPanel.Y + ChartPanel.H - InfoMarginY;
						num3 = -1;
						num4 = -1;
					}
				}
				else
				{
					num = ChartPanel.X + InfoMarginX;
					num2 = ChartPanel.Y + ChartPanel.H - InfoMarginY;
					num3 = 1;
					num4 = -1;
				}
			}
			else
			{
				num = ChartPanel.X + ChartPanel.W - InfoMarginX;
				num2 = ChartPanel.Y + InfoMarginY;
				num3 = -1;
				num4 = 1;
			}
		}
		else
		{
			num = ChartPanel.X + InfoMarginX;
			num2 = ChartPanel.Y + InfoMarginY;
			num3 = 1;
			num4 = 1;
		}
		string text = "  |  ";
		string text2 = ((activeAccount != null) ? activeAccount.DisplayName : "None");
		string text3 = ((chartTrader.AtmStrategy != null) ? ((NinjaScript)chartTrader.AtmStrategy).DisplayName : "None");
		string text4 = text2 + text + text3;
		DrawTextOnChart(text4, InfoFont, num, num2, num3, num4, InfoBrush, ScreenDPI, RenderTarget);
	}

	private void OnMouseMove(object sender, MouseEventArgs e)
	{
		TriggerCustomEvent((Action<object>)delegate
		{
			UpdateModifierKeysStates();
			if (isCharting)
			{
				ChartControl.Dispatcher.InvokeAsync(delegate
				{
					if (currentAltState == ninZaSmartEscape_AltState.OnlyALT)
					{
						ChartControl.InvalidateVisual();
					}
				});
			}
		}, (object)e);
	}

	private void OnMouseLeave(object sender, MouseEventArgs e)
	{
		TriggerCustomEvent((Action<object>)delegate
		{
			if (currentAltState == ninZaSmartEscape_AltState.OnlyALT && isCharting)
			{
				ChartControl.Dispatcher.InvokeAsync(delegate
				{
					Keyboard.Focus((IInputElement)ChartPanel);
				});
			}
			DeactivateModifierKeysStates();
			if (keyWatchTimer.IsEnabled)
			{
				keyWatchTimer.Stop();
			}
			if (isCharting)
			{
				ChartControl.Dispatcher.InvokeAsync(delegate
				{
					ChartControl.InvalidateVisual();
				});
			}
		}, (object)e);
	}

	private void OnMouseEnter(object sender, MouseEventArgs e)
	{
		TriggerCustomEvent((Action<object>)delegate
		{
			if (!keyWatchTimer.IsEnabled)
			{
				keyWatchTimer.Start();
			}
		}, (object)e);
	}

	private void OnMouseDown(object sender, MouseButtonEventArgs e)
	{
		TriggerCustomEvent((Action<object>)delegate
		{
			if (keyWatchTimer.IsEnabled)
			{
				keyWatchTimer.Stop();
			}
			UpdateModifierKeysStates();
			if (currentAltState != ninZaSmartEscape_AltState.Null && currentAltState == ninZaSmartEscape_AltState.OnlyALT && e.ChangedButton == MouseButton.Left)
			{
				Position position = GetPosition();
				if (position != null)
				{
					MarketPosition marketPosition = position.MarketPosition;
					if ((int)marketPosition != 2)
					{
						bool flag = (int)marketPosition == 1;
						double effMousePrice = GetEffMousePrice();
						OrderType orderType;
						Point point = CalculateOrderTypeAndPrices(flag, effMousePrice, out orderType);
						double x = point.X;
						double y = point.Y;
						SubmitOrder((OrderAction)((!flag) ? 2 : 0), orderType, x, y, OcoId);
					}
				}
			}
		}, (object)e);
	}

	[DllImport("user32", CharSet = CharSet.Ansi, SetLastError = true)]
	private static extern int GetAsyncKeyState(int vKey);

	private void UpdateModifierKeysStates()
	{
		if (GetAsyncKeyState(Convert.ToInt32((byte)18)) == 0)
		{
			currentAltState = ninZaSmartEscape_AltState.None;
		}
		else
		{
			currentAltState = ninZaSmartEscape_AltState.OnlyALT;
		}
	}

	private void DeactivateModifierKeysStates()
	{
		currentAltState = ninZaSmartEscape_AltState.Null;
		prevAltState = ninZaSmartEscape_AltState.Null;
	}

	private void OnKeyDown(object sender, KeyEventArgs e)
	{
		TriggerCustomEvent((Action<object>)delegate
		{
			if (GetPosition() != null)
			{
				UpdateModifierKeysStates();
				if (currentAltState != ninZaSmartEscape_AltState.Null && currentAltState != ninZaSmartEscape_AltState.None && isCharting)
				{
					ChartControl.Dispatcher.InvokeAsync(delegate
					{
						ChartControl.InvalidateVisual();
					});
				}
			}
		}, (object)e);
	}

	private void OnKeyUp(object sender, KeyEventArgs e)
	{
		TriggerCustomEvent((Action<object>)delegate
		{
			Key key = ((e.Key != Key.System) ? e.Key : e.SystemKey);
			if ((key == Key.LeftAlt || key == Key.RightAlt) && isCharting)
			{
				ChartControl.Dispatcher.InvokeAsync(delegate
				{
					Keyboard.Focus((IInputElement)ChartPanel);
				});
			}
			UpdateModifierKeysStates();
			if (isCharting)
			{
				ChartControl.Dispatcher.InvokeAsync(delegate
				{
					ChartControl.InvalidateVisual();
				});
			}
		}, (object)e);
	}

	private void OnKeyWatchTimerTick(object sender, EventArgs e)
	{
		TriggerCustomEvent((Action<object>)delegate
		{
			UpdateModifierKeysStates();
			if (prevAltState != ninZaSmartEscape_AltState.Null)
			{
				if (prevAltState != currentAltState && isCharting)
				{
					ChartControl.Dispatcher.InvokeAsync(delegate
					{
						ChartControl.InvalidateVisual();
					});
				}
			}
			else if (isCharting)
			{
				ChartControl.Dispatcher.InvokeAsync(delegate
				{
					ChartControl.InvalidateVisual();
				});
			}
			prevAltState = currentAltState;
		}, (object)e);
	}

	private Point GetDPIAdjustedMousePosition()
	{
		Point position = Mouse.GetPosition((IInputElement)ChartControl);
		return new Point(position.X * (double)ScreenDPI / 100.0, position.Y * (double)ScreenDPI / 100.0);
	}

	private double GetEffMousePrice()
	{
		float y = DxExtensions.ToVector2(GetDPIAdjustedMousePosition()).Y;
		return Instrument.MasterInstrument.RoundToTickSize(chartScale.GetValueByY(y));
	}

	private void SubmitOrder(OrderAction orderAction, OrderType orderType, double limit, double stop, object ocoId)
	{
		if (!isCharting)
		{
			return;
		}
		ChartControl.Dispatcher.InvokeAsync(delegate
		{
			if (chartTraderAvailable && selectorTif != null)
			{
				DateTime dateTime = (((int)selectorTif.SelectedTif != 3) ? DateTime.MaxValue : selectorTif.GtdDate);
				object[] parameters = new object[13]
				{
					activeAccount,
					Instrument,
					orderAction,
					orderType,
					selectorTif.SelectedTif,
					dateTime,
					chartTrader.Quantity,
					limit,
					stop,
					ocoId,
					null,
					chartTrader.AtmStrategy,
					true
				};
				MethodInfo method = ((object)chartTrader).GetType().GetMethod("SubmitOrder", flags);
				if (method != null)
				{
					method.Invoke(chartTrader, parameters);
				}
			}
		});
	}

	private string ReformatOcoId(string input)
	{
		string[] array = input.Split('-');
		string text = string.Empty;
		string[] array2 = array;
		foreach (string text2 in array2)
		{
			text += text2;
		}
		return text;
	}

	private Point CalculateOrderTypeAndPrices(bool isBuy, double price, out OrderType orderType)
	{
		Point result = default(Point);
		int num = (isBuy ? 1 : (-1));
		double num2 = (double)OrderSlmOffset * TickSize;
		int num3 = MathExtentions.ApproxCompare(price, currentMarketPrice);
		if (!isBuy)
		{
			if (num3 >= 0)
			{
				orderType = (OrderType)(int)GetOrderType(OrderTypeBehindOrAtMarket.ToString());
			}
			else
			{
				orderType = (OrderType)(int)GetOrderType(OrderTypeInFrontOfMarket.ToString());
			}
		}
		else if (num3 <= 0)
		{
			orderType = (OrderType)(int)GetOrderType(OrderTypeBehindOrAtMarket.ToString());
		}
		else
		{
			orderType = (OrderType)(int)GetOrderType(OrderTypeInFrontOfMarket.ToString());
		}
		double num4 = 0.0;
		double x = 0.0;
		if (orderType != OrderType.Limit)
		{
			if ((int)orderType != 2)
			{
				if ((int)orderType != 3)
				{
					if ((int)orderType == 4)
					{
						num4 = price;
					}
				}
				else
				{
					num4 = price;
					x = num4 + (double)num * num2;
				}
			}
			else
			{
				num4 = price;
			}
		}
		else
		{
			x = price;
		}
		result.X = x;
		result.Y = num4;
		return result;
	}

	private OrderType GetOrderType(string input)
	{
		return (OrderType)(input switch
		{
			"STP" => 4, 
			"MIT" => 2, 
			"SLM" => 3, 
			_ => 0, 
		});
	}

	private void OnAccountSelectorSelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		TriggerCustomEvent((Action<object>)delegate
		{
			if (isCharting)
			{
				ChartControl.Dispatcher.InvokeAsync(delegate
				{
					ResetID();
					if (activeAccount != null)
					{
						activeAccount.OrderUpdate -= ActiveAccount_OrderUpdate;
						activeAccount.PositionUpdate -= ActiveAccount_PositionUpdate;
					}
					ref Account reference = ref activeAccount;
					object selectedItem = controlPanel.accountSelector.SelectedItem;
					reference = (Account)((selectedItem is Account) ? selectedItem : null);
					if (activeAccount != null)
					{
						activeAccount.OrderUpdate += ActiveAccount_OrderUpdate;
						activeAccount.PositionUpdate += ActiveAccount_PositionUpdate;
						quantityPosition = -1;
						LastBarIndexDown = -1;
						LastBarIndexUp = -1;
						LastBarIndexAny = -1;
						Position position = GetPosition();
						if (position != null && (int)position.MarketPosition != 1)
						{
							LastBarIndexAny = CurrentBar;
							SaveActiveAccountName();
						}
					}
				});
			}
		}, (object)e);
	}

	private void SetButtonState(Button button, bool buttonActived, bool isLongButton)
	{
		if (!buttonActived)
		{
			return;
		}
		if (!isLongButton)
		{
			if (!ExitConditionShortEnabled)
			{
				ExitConditionShortEnabled = true;
				controlPanel.SetButtonBackground(button, CpButtonExitShortConditionActive);
			}
		}
		else if (!ExitConditionLongEnabled)
		{
			ExitConditionLongEnabled = true;
			controlPanel.SetButtonBackground(button, CpButtonExitLongConditionActive);
		}
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

	private void OnButtonExitConditionEnabledClick(object sender, RoutedEventArgs e)
	{
		TriggerCustomEvent((Action<object>)delegate(object state)
		{
			if (isCharting)
			{
				ChartControl.Dispatcher.InvokeAsync(delegate
				{
					ExitConditionEnabled = !ExitConditionEnabled;
					controlPanel.SetButtonBackground(controlPanel.btnExitConditionEnabled, (!ExitConditionEnabled) ? CpButtonExitConditionInactive : CpButtonExitConditionActive);
					if (!ExitConditionEnabled)
					{
						ninZaSmartEscape obj = this;
						ninZaSmartEscape obj2 = this;
						ninZaSmartEscape obj3 = this;
						ExitConditionLongDown = false;
						obj3.ExitConditionLongUp = false;
						obj2.ExitConditionLongAny = false;
						obj.ExitConditionLongEnabled = false;
						controlPanel.SetButtonBackground(controlPanel.btnLong, CpButtonExitLongConditionInactive);
						controlPanel.SetButtonBackground(controlPanel.btnLongAny, CpButtonLongAnyInactive);
						controlPanel.SetButtonBackground(controlPanel.btnLongUp, CpButtonLongUpInactive);
						controlPanel.SetButtonBackground(controlPanel.btnLongDown, CpButtonLongDownInactive);
						ninZaSmartEscape obj4 = this;
						ninZaSmartEscape obj5 = this;
						ninZaSmartEscape obj6 = this;
						ExitConditionShortDown = false;
						obj6.ExitConditionShortUp = false;
						obj5.ExitConditionShortAny = false;
						obj4.ExitConditionShortEnabled = false;
						controlPanel.SetButtonBackground(controlPanel.btnShort, CpButtonExitShortConditionInactive);
						controlPanel.SetButtonBackground(controlPanel.btnShortAny, CpButtonShortAnyInactive);
						controlPanel.SetButtonBackground(controlPanel.btnShortUp, CpButtonShortUpInactive);
						controlPanel.SetButtonBackground(controlPanel.btnShortDown, CpButtonShortDownInactive);
						ninZaSmartEscape obj7 = this;
						ninZaSmartEscape obj8 = this;
						LastBarIndexDown = -1;
						obj8.LastBarIndexUp = -1;
						obj7.LastBarIndexAny = -1;
					}
					else
					{
						Position position = GetPosition();
						if (position != null)
						{
							MarketPosition marketPosition = position.MarketPosition;
							if ((int)marketPosition != 2)
							{
								if ((int)marketPosition != 0)
								{
									if ((ExitConditionShortAny || ExitConditionShortUp || ExitConditionShortDown) && ExitConditionShortAny)
									{
										LastBarIndexAny = (int)state;
									}
								}
								else if ((ExitConditionLongAny || ExitConditionLongUp || ExitConditionLongDown) && ExitConditionLongAny)
								{
									LastBarIndexAny = (int)state;
								}
							}
						}
					}
				});
			}
		}, (object)CurrentBar);
	}

	private void OnBtnBarDirectionLongClick(object sender, RoutedEventArgs e)
	{
		TriggerCustomEvent((Action<object>)delegate(object state)
		{
			if (isCharting)
			{
				ChartControl.Dispatcher.InvokeAsync(delegate
				{
					Button button = sender as Button;
					if (button != controlPanel.btnLong)
					{
						if (button != controlPanel.btnLongAny)
						{
							if (button != controlPanel.btnLongUp)
							{
								ExitConditionLongDown = !ExitConditionLongDown;
								controlPanel.SetButtonBackground(button, (!ExitConditionLongDown) ? CpButtonLongDownInactive : CpButtonLongDownActive);
								SetButtonState(controlPanel.btnLong, ExitConditionLongDown, isLongButton: true);
								if (ExitConditionEnabled)
								{
									Position position = GetPosition();
									if (position != null && (int)position.MarketPosition == 0 && !ExitConditionLongDown)
									{
										LastBarIndexDown = -1;
									}
								}
							}
							else
							{
								ExitConditionLongUp = !ExitConditionLongUp;
								controlPanel.SetButtonBackground(button, (!ExitConditionLongUp) ? CpButtonLongUpInactive : CpButtonLongUpActive);
								SetButtonState(controlPanel.btnLong, ExitConditionLongUp, isLongButton: true);
								if (ExitConditionEnabled)
								{
									Position position2 = GetPosition();
									if (position2 != null && (int)position2.MarketPosition == 0 && !ExitConditionLongUp)
									{
										LastBarIndexUp = -1;
									}
								}
							}
						}
						else
						{
							ExitConditionLongAny = !ExitConditionLongAny;
							controlPanel.SetButtonBackground(button, (!ExitConditionLongAny) ? CpButtonLongAnyInactive : CpButtonLongAnyActive);
							SetButtonState(controlPanel.btnLong, ExitConditionLongAny, isLongButton: true);
							if (ExitConditionEnabled)
							{
								Position position3 = GetPosition();
								if (position3 != null && (int)position3.MarketPosition == 0)
								{
									if (!ExitConditionLongAny)
									{
										LastBarIndexAny = -1;
									}
									else
									{
										LastBarIndexAny = (int)state;
									}
								}
							}
						}
					}
					else
					{
						ExitConditionLongEnabled = !ExitConditionLongEnabled;
						controlPanel.SetButtonBackground(button, (!ExitConditionLongEnabled) ? CpButtonExitLongConditionInactive : CpButtonExitLongConditionActive);
						if (!ExitConditionLongEnabled)
						{
							ninZaSmartEscape obj = this;
							ninZaSmartEscape obj2 = this;
							ExitConditionLongDown = false;
							obj2.ExitConditionLongUp = false;
							obj.ExitConditionLongAny = false;
							controlPanel.SetButtonBackground(controlPanel.btnLongAny, CpButtonLongAnyInactive);
							controlPanel.SetButtonBackground(controlPanel.btnLongUp, CpButtonLongUpInactive);
							controlPanel.SetButtonBackground(controlPanel.btnLongDown, CpButtonLongDownInactive);
							if (ExitConditionEnabled)
							{
								Position position4 = GetPosition();
								if (position4 != null && (int)position4.MarketPosition == 0)
								{
									ninZaSmartEscape obj3 = this;
									ninZaSmartEscape obj4 = this;
									LastBarIndexDown = -1;
									obj4.LastBarIndexUp = -1;
									obj3.LastBarIndexAny = -1;
								}
							}
						}
					}
					if (button != controlPanel.btnLong && ExitConditionLongEnabled && !ExitConditionLongAny && !ExitConditionLongUp && !ExitConditionLongDown)
					{
						ExitConditionLongEnabled = false;
						controlPanel.SetButtonBackground(controlPanel.btnLong, CpButtonExitLongConditionInactive);
					}
				});
			}
		}, (object)CurrentBar);
	}

	private void OnBtnBarDirectionShortClick(object sender, RoutedEventArgs e)
	{
		TriggerCustomEvent((Action<object>)delegate(object state)
		{
			if (isCharting)
			{
				ChartControl.Dispatcher.InvokeAsync(delegate
				{
					Button button = sender as Button;
					if (button != controlPanel.btnShort)
					{
						if (button != controlPanel.btnShortAny)
						{
							if (button != controlPanel.btnShortUp)
							{
								ExitConditionShortDown = !ExitConditionShortDown;
								controlPanel.SetButtonBackground(button, (!ExitConditionShortDown) ? CpButtonShortDownInactive : CpButtonShortDownActive);
								SetButtonState(controlPanel.btnShort, ExitConditionShortDown, isLongButton: false);
								if (ExitConditionEnabled)
								{
									Position position = GetPosition();
									if (position != null && (int)position.MarketPosition == 0 && !ExitConditionShortDown)
									{
										LastBarIndexDown = -1;
									}
								}
							}
							else
							{
								ExitConditionShortUp = !ExitConditionShortUp;
								controlPanel.SetButtonBackground(button, (!ExitConditionShortUp) ? CpButtonShortUpInactive : CpButtonShortUpActive);
								SetButtonState(controlPanel.btnShort, ExitConditionShortUp, isLongButton: false);
								if (ExitConditionEnabled)
								{
									Position position2 = GetPosition();
									if (position2 != null && (int)position2.MarketPosition == 0 && !ExitConditionShortUp)
									{
										LastBarIndexUp = -1;
									}
								}
							}
						}
						else
						{
							ExitConditionShortAny = !ExitConditionShortAny;
							controlPanel.SetButtonBackground(button, (!ExitConditionShortAny) ? CpButtonShortAnyInactive : CpButtonShortAnyActive);
							SetButtonState(controlPanel.btnShort, ExitConditionShortAny, isLongButton: false);
							if (ExitConditionEnabled)
							{
								Position position3 = GetPosition();
								if (position3 != null && (int)position3.MarketPosition == 0)
								{
									if (!ExitConditionShortAny)
									{
										LastBarIndexAny = -1;
									}
									else
									{
										LastBarIndexAny = (int)state;
									}
								}
							}
						}
					}
					else
					{
						ExitConditionShortEnabled = !ExitConditionShortEnabled;
						controlPanel.SetButtonBackground(button, (!ExitConditionShortEnabled) ? CpButtonExitShortConditionInactive : CpButtonExitShortConditionActive);
						if (!ExitConditionShortEnabled)
						{
							ninZaSmartEscape obj = this;
							ninZaSmartEscape obj2 = this;
							ExitConditionShortDown = false;
							obj2.ExitConditionShortUp = false;
							obj.ExitConditionShortAny = false;
							controlPanel.SetButtonBackground(controlPanel.btnShortAny, CpButtonShortAnyInactive);
							controlPanel.SetButtonBackground(controlPanel.btnShortUp, CpButtonShortUpInactive);
							controlPanel.SetButtonBackground(controlPanel.btnShortDown, CpButtonShortDownInactive);
							if (ExitConditionEnabled)
							{
								Position position4 = GetPosition();
								if (position4 != null && (int)position4.MarketPosition == 0)
								{
									ninZaSmartEscape obj3 = this;
									ninZaSmartEscape obj4 = this;
									LastBarIndexDown = -1;
									obj4.LastBarIndexUp = -1;
									obj3.LastBarIndexAny = -1;
								}
							}
						}
					}
					if (button != controlPanel.btnShort && ExitConditionShortEnabled && !ExitConditionShortAny && !ExitConditionShortUp && !ExitConditionShortDown)
					{
						ExitConditionShortEnabled = false;
						controlPanel.SetButtonBackground(controlPanel.btnShort, CpButtonExitShortConditionInactive);
					}
				});
			}
		}, (object)CurrentBar);
	}

	private void OnOffsetLongAnyChange(object sender, RoutedEventArgs e)
	{
		TriggerCustomEvent((Action<object>)delegate(object state)
		{
			if (isCharting)
			{
				ChartControl.Dispatcher.InvokeAsync(delegate
				{
					ExitConditionLongAnyNumberOfBars = controlPanel.selectorOffsetLongAny.Value;
					if (ExitConditionEnabled && ExitConditionLongAny)
					{
						Position position = GetPosition();
						if (position != null && (int)position.MarketPosition == 0)
						{
							LastBarIndexAny = (int)state;
						}
					}
				});
			}
		}, (object)CurrentBar);
	}

	private void OnOffsetLongUpChange(object sender, RoutedEventArgs e)
	{
		TriggerCustomEvent((Action<object>)delegate
		{
			if (isCharting)
			{
				ChartControl.Dispatcher.InvokeAsync(delegate
				{
					ExitConditionLongUpNumberOfBars = controlPanel.selectorOffsetLongUp.Value;
					if (ExitConditionEnabled && ExitConditionLongUp)
					{
						Position position = GetPosition();
						if (position != null && (int)position.MarketPosition == 0)
						{
							LastBarIndexUp = -1;
						}
					}
				});
			}
		}, (object)e);
	}

	private void OnOffsetLongDownChange(object sender, RoutedEventArgs e)
	{
		TriggerCustomEvent((Action<object>)delegate
		{
			if (isCharting)
			{
				ChartControl.Dispatcher.InvokeAsync(delegate
				{
					ExitConditionLongDownNumberOfBars = controlPanel.selectorOffsetLongDown.Value;
					if (ExitConditionEnabled && ExitConditionLongDown)
					{
						Position position = GetPosition();
						if (position != null && (int)position.MarketPosition == 0)
						{
							LastBarIndexDown = -1;
						}
					}
				});
			}
		}, (object)e);
	}

	private void OnOffsetShortAnyChange(object sender, RoutedEventArgs e)
	{
		TriggerCustomEvent((Action<object>)delegate(object state)
		{
			if (isCharting)
			{
				ChartControl.Dispatcher.InvokeAsync(delegate
				{
					ExitConditionShortAnyNumberOfBars = controlPanel.selectorOffsetShortAny.Value;
					if (ExitConditionEnabled && ExitConditionShortAny)
					{
						Position position = GetPosition();
						if (position != null && (int)position.MarketPosition == 0)
						{
							LastBarIndexAny = (int)state;
						}
					}
				});
			}
		}, (object)CurrentBar);
	}

	private void OnOffsetShortUpChange(object sender, RoutedEventArgs e)
	{
		TriggerCustomEvent((Action<object>)delegate
		{
			if (isCharting)
			{
				ChartControl.Dispatcher.InvokeAsync(delegate
				{
					ExitConditionShortUpNumberOfBars = controlPanel.selectorOffsetShortUp.Value;
					if (ExitConditionEnabled && ExitConditionShortUp)
					{
						Position position = GetPosition();
						if (position != null && (int)position.MarketPosition == 0)
						{
							LastBarIndexUp = -1;
						}
					}
				});
			}
		}, (object)e);
	}

	private void OnOffsetShortDownChange(object sender, RoutedEventArgs e)
	{
		TriggerCustomEvent((Action<object>)delegate
		{
			if (isCharting)
			{
				ChartControl.Dispatcher.InvokeAsync(delegate
				{
					ExitConditionShortDownNumberOfBars = controlPanel.selectorOffsetShortDown.Value;
					if (ExitConditionEnabled && ExitConditionShortDown)
					{
						Position position = GetPosition();
						if (position != null && (int)position.MarketPosition == 0)
						{
							LastBarIndexDown = -1;
						}
					}
				});
			}
		}, (object)e);
	}

	private void OnChartTraderAccountChange(object sender, SelectionChangedEventArgs e)
	{
		TriggerCustomEvent((Action<object>)delegate(object state)
		{
			if (isValidTab)
			{
				if (isCharting)
				{
					ChartControl.Dispatcher.InvokeAsync(delegate
					{
						ResetID();
					});
				}
				if (activeAccount != null)
				{
					activeAccount.OrderUpdate -= ActiveAccount_OrderUpdate;
					activeAccount.PositionUpdate -= ActiveAccount_PositionUpdate;
				}
				activeAccount = selectorAccount.SelectedAccount;
				if (activeAccount != null)
				{
					activeAccount.OrderUpdate += ActiveAccount_OrderUpdate;
					activeAccount.PositionUpdate += ActiveAccount_PositionUpdate;
					quantityPosition = -1;
					LastBarIndexDown = -1;
					LastBarIndexUp = -1;
					LastBarIndexAny = -1;
					Position position = GetPosition();
					if (position != null && (int)position.MarketPosition != 1)
					{
						LastBarIndexAny = (int)state;
					}
				}
			}
		}, (object)CurrentBar);
	}

	private void ActiveAccount_OrderUpdate(object sender, OrderEventArgs e)
	{
		TriggerCustomEvent((Action<object>)delegate(object state)
		{
			Order order = ((OrderEventArgs)((state is OrderEventArgs) ? state : null)).Order;
			if (!string.IsNullOrEmpty(order.Oco) && order.Oco == OcoId)
			{
				OrderState orderState = order.OrderState;
				if ((int)orderState == 2 || (int)orderState == 1)
				{
					ResetID();
					OcoId = GetEffOcoId();
				}
			}
		}, (object)e);
	}

	private void ActiveAccount_PositionUpdate(object sender, PositionEventArgs e)
	{
		if (!ExitConditionEnabled)
		{
			return;
		}
		bool isNotEnabledLong = !ExitConditionLongAny && !ExitConditionLongUp && !ExitConditionLongDown;
		bool isNotEnabledShort = !ExitConditionShortAny && !ExitConditionShortUp && !ExitConditionShortDown;
		Position position = GetPosition();
		TriggerCustomEvent((Action<object>)delegate(object state)
		{
			if (position == null)
			{
				if (!(isNotEnabledLong && isNotEnabledShort))
				{
					Collection<Order> orders = activeAccount.Orders;
					if (orders != null || orders.Count > 0)
					{
						List<Order> list = new List<Order>();
						IEnumerator<Order> enumerator = orders.GetEnumerator();
						while (enumerator.MoveNext())
						{
							Order current = enumerator.Current;
							string oco = current.Oco;
							if (!string.IsNullOrEmpty(oco) && ((object)current.Instrument).Equals((object)Instrument) && (int)current.OrderEntry == 1)
							{
								OrderState orderState = current.OrderState;
								if (((current.IsLimit && (int)orderState == 10) || (current.IsStopMarket && (int)orderState == 0)) && oco == OcoId)
								{
									list.Add(current);
								}
							}
						}
						if (list.Count() > 0)
						{
							activeAccount.CancelOrdersByOcoID((IEnumerable<Order>)list, (string)null);
						}
					}
					if (!ExitConditionActiveTilCanceled)
					{
						ExitConditionEnabled = false;
						if (isCharting)
						{
							ChartControl.Dispatcher.InvokeAsync(delegate
							{
								controlPanel.SetButtonBackground(controlPanel.btnExitConditionEnabled, CpButtonExitConditionInactive);
							});
						}
					}
					ResetID();
					exitCondExecuting = false;
					ninZaSmartEscape obj = this;
					ninZaSmartEscape obj2 = this;
					LastBarIndexDown = -1;
					obj2.LastBarIndexUp = -1;
					obj.LastBarIndexAny = -1;
					quantityPosition = -1;
				}
			}
			else
			{
				MarketPosition marketPosition = position.MarketPosition;
				bool flag;
				if ((int)marketPosition != 2 && !((flag = (int)marketPosition == 0) && isNotEnabledLong) && !(!flag && isNotEnabledShort))
				{
					if (quantityPosition >= 0)
					{
						if (position.Quantity > quantityPosition && !exitCondExecuting)
						{
							if (!flag)
							{
								if (ExitConditionShortAny)
								{
									LastBarIndexAny = (int)state;
								}
								if (ExitConditionShortUp)
								{
									LastBarIndexUp = -1;
								}
								if (ExitConditionShortDown)
								{
									LastBarIndexDown = -1;
								}
							}
							else
							{
								if (ExitConditionLongAny)
								{
									LastBarIndexAny = (int)state;
								}
								if (ExitConditionLongUp)
								{
									LastBarIndexUp = -1;
								}
								if (ExitConditionLongDown)
								{
									LastBarIndexDown = -1;
								}
							}
							exitCondExecuting = true;
						}
						quantityPosition = position.Quantity;
					}
					else
					{
						OcoId = GetEffOcoId();
						if (!flag)
						{
							if (ExitConditionShortAny)
							{
								LastBarIndexAny = (int)state;
							}
							if (ExitConditionShortUp)
							{
								LastBarIndexUp = -1;
							}
							if (ExitConditionShortDown)
							{
								LastBarIndexDown = -1;
							}
						}
						else
						{
							if (ExitConditionLongAny)
							{
								LastBarIndexAny = (int)state;
							}
							if (ExitConditionLongUp)
							{
								LastBarIndexUp = -1;
							}
							if (ExitConditionLongDown)
							{
								LastBarIndexDown = -1;
							}
						}
						quantityPosition = position.Quantity;
					}
				}
			}
		}, (object)CurrentBar);
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
					if (!num && isValidTab && selectorAccount.SelectedAccount != activeAccount)
					{
						selectorAccount.SelectedAccount = activeAccount;
					}
				});
			}
		}, (object)e);
	}

	public override string FormatPriceMarker(double price)
	{
		return Instrument.MasterInstrument.FormatPrice(Instrument.MasterInstrument.RoundToTickSize(price), true);
	}

	private void ResetID()
	{
		if (menuItemOco == null || !isCharting)
		{
			return;
		}
		ChartControl.Dispatcher.InvokeAsync(delegate
		{
			if (menuItemOco.IsEnabled)
			{
				menuItemOco.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent, menuItemOco));
				menuItemOco.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent, menuItemOco));
				ChartControl.InvalidateVisual();
			}
		});
	}

	private string GetEffOcoId()
	{
		if (chartTrader == null || menuItemOco == null)
		{
			return string.Empty;
		}
		FieldInfo fieldInfo = ((object)chartTrader).GetType().GetField("OcoId", flags);
		if (!(fieldInfo == null))
		{
			return ReformatOcoId(((Guid)fieldInfo.GetValue(chartTrader)/*cast due to .constrained prefix*/).ToString());
		}
		return string.Empty;
	}

	private void CreateInstructionContent()
	{
	}

	private void OnInstructionClose(object sender, RoutedEventArgs e)
	{
		TriggerCustomEvent((Action<object>)delegate
		{
			InstructionEnabled = false;
		}, (object)e);
	}

	private System.Windows.Size ComputeTextSize(string text, SimpleFont font, int dpi)
	{
		if (string.IsNullOrEmpty(text)) return new System.Windows.Size(0, 0);
		int lines = 1;
		int maxLen = 0;
		int curLen = 0;
		foreach (char c in text)
		{
			if (c == '\n') { lines++; if (curLen > maxLen) maxLen = curLen; curLen = 0; }
			else curLen++;
		}
		if (curLen > maxLen) maxLen = curLen;
		return new System.Windows.Size(font.Size * maxLen * 0.6, font.Size * 1.4 * lines);
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

	public enum ninZaSmartEscape_AltState
	{
		Null,
		None,
		OnlyALT
	}

	public enum ninZaSmartEscape_OrderTypeBehindOrAtMarket
	{
		LMT,
		MIT
	}

	public enum ninZaSmartEscape_OrderTypeInFrontOfMarket
	{
		SLM,
		STP
	}

	public class ninZaSmartEscape_Converter : NinjaTrader.NinjaScript.IndicatorBaseConverter
	{
		public override System.ComponentModel.PropertyDescriptorCollection GetProperties(System.ComponentModel.ITypeDescriptorContext context, object component, Attribute[] attrs)
		{
			ninZaSmartEscape obj = component as ninZaSmartEscape;
			System.ComponentModel.PropertyDescriptorCollection propertyDescriptorCollection = ((!base.GetPropertiesSupported(context)) ? System.ComponentModel.TypeDescriptor.GetProperties(component, attrs) : base.GetProperties(context, component, attrs));
			if (obj == null || propertyDescriptorCollection == null)
			{
				return propertyDescriptorCollection;
			}
			System.ComponentModel.PropertyDescriptor value = propertyDescriptorCollection["LastBarIndexAny"];
			System.ComponentModel.PropertyDescriptor value2 = propertyDescriptorCollection["LastBarIndexUp"];
			System.ComponentModel.PropertyDescriptor value3 = propertyDescriptorCollection["LastBarIndexDown"];
			propertyDescriptorCollection.Remove(value);
			propertyDescriptorCollection.Remove(value2);
			propertyDescriptorCollection.Remove(value3);
			return propertyDescriptorCollection;
		}

		public override bool GetPropertiesSupported(System.ComponentModel.ITypeDescriptorContext context)
		{
			return true;
		}
	}

	public class ninZaSmartEscape_SoundConverter : System.ComponentModel.TypeConverter
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
}

