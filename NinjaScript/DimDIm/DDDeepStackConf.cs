using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using System.Xml.Serialization;
using Microsoft.CSharp.RuntimeBinder;
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
	public class DDNumericUpDown : System.Windows.Controls.UserControl
	{
		public class NUDSettingInfo
		{
			public System.Collections.Generic.List<int> Quick { get; set; } = new System.Collections.Generic.List<int>();
			public System.Collections.Generic.List<int> Increment { get; set; } = new System.Collections.Generic.List<int>();
		}

		public System.Windows.Controls.TextBox NUDTextBox { get; private set; }
		public NUDSettingInfo SettingInfo { get; set; }
		public double Minimum { get; set; } = 1;
		public double Maximum { get; set; } = int.MaxValue;

		private double _value;
		private bool _suppressTextSync;
		public double Value
		{
			get { return _value; }
			set
			{
				double clamped = Clamp(value);
				_value = clamped;
				if (NUDTextBox != null)
				{
					string s = clamped.ToString(System.Globalization.CultureInfo.InvariantCulture);
					if (NUDTextBox.Text != s)
					{
						_suppressTextSync = true;
						NUDTextBox.Text = s;
						_suppressTextSync = false;
					}
				}
			}
		}

		private double Clamp(double v)
		{
			if (v < Minimum) return Minimum;
			if (v > Maximum) return Maximum;
			return v;
		}

		public DDNumericUpDown()
		{
			MinWidth = 30;
			MinHeight = 18;
			Focusable = false;
			IsEnabledChanged += (s, e) => { Opacity = ((bool)e.NewValue) ? 1.0 : 0.4; };
			var grid = new System.Windows.Controls.Grid();
			grid.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = new System.Windows.GridLength(1.0, System.Windows.GridUnitType.Star) });
			grid.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = System.Windows.GridLength.Auto });

			NUDTextBox = new System.Windows.Controls.TextBox
			{
				VerticalContentAlignment = System.Windows.VerticalAlignment.Center,
				HorizontalContentAlignment = System.Windows.HorizontalAlignment.Center,
				BorderThickness = new System.Windows.Thickness(0),
				Background = System.Windows.Media.Brushes.Transparent,
				Padding = new System.Windows.Thickness(0),
				Text = ((int)Minimum).ToString(System.Globalization.CultureInfo.InvariantCulture),
				IsTabStop = true,
				Focusable = true
			};
			_value = Minimum;
			NUDTextBox.SetValue(System.Windows.Controls.Grid.ColumnProperty, 0);

			NUDTextBox.TextChanged += (s, e) =>
			{
				if (_suppressTextSync) return;
				double v;
				if (double.TryParse(NUDTextBox.Text, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out v))
					_value = Clamp(v);
			};
			NUDTextBox.LostFocus += (s, e) =>
			{
				_suppressTextSync = true;
				NUDTextBox.Text = _value.ToString(System.Globalization.CultureInfo.InvariantCulture);
				_suppressTextSync = false;
			};
			NUDTextBox.PreviewTextInput += (s, e) =>
			{
				if (!IsAllowedTextInput(e.Text)) e.Handled = true;
			};
			NUDTextBox.PreviewMouseDown += (s, e) =>
			{
				if (!NUDTextBox.IsKeyboardFocusWithin)
				{
					NUDTextBox.Focus();
					System.Windows.Input.Keyboard.Focus(NUDTextBox);
					NUDTextBox.SelectAll();
					e.Handled = true;
				}
			};
			NUDTextBox.KeyDown += (s, e) => { e.Handled = true; };
			NUDTextBox.TextInput += (s, e) => { e.Handled = true; };

			var btnPanel = new System.Windows.Controls.StackPanel { Orientation = System.Windows.Controls.Orientation.Vertical, VerticalAlignment = System.Windows.VerticalAlignment.Center };
			btnPanel.SetValue(System.Windows.Controls.Grid.ColumnProperty, 1);
			var btnUp = new System.Windows.Controls.Button
			{
				Content = "▲", MinWidth = 0, MinHeight = 0, FontSize = 6,
				Padding = new System.Windows.Thickness(2, 0, 2, 0),
				BorderThickness = new System.Windows.Thickness(0),
				Background = System.Windows.Media.Brushes.Transparent,
				Cursor = System.Windows.Input.Cursors.Hand,
				Focusable = false
			};
			var btnDown = new System.Windows.Controls.Button
			{
				Content = "▼", MinWidth = 0, MinHeight = 0, FontSize = 6,
				Padding = new System.Windows.Thickness(2, 0, 2, 0),
				BorderThickness = new System.Windows.Thickness(0),
				Background = System.Windows.Media.Brushes.Transparent,
				Cursor = System.Windows.Input.Cursors.Hand,
				Focusable = false
			};
			btnUp.Click += (s, e) => { Value = _value + GetStep(); };
			btnDown.Click += (s, e) => { Value = _value - GetStep(); };
			btnPanel.Children.Add(btnUp);
			btnPanel.Children.Add(btnDown);

			grid.Children.Add(NUDTextBox);
			grid.Children.Add(btnPanel);
			Content = grid;
		}

		private static bool IsAllowedTextInput(string s)
		{
			if (string.IsNullOrEmpty(s)) return true;
			for (int i = 0; i < s.Length; i++)
			{
				char c = s[i];
				if (!(char.IsDigit(c) || c == '-' || c == '.' || c == ',')) return false;
			}
			return true;
		}

		private double GetStep()
		{
			if (SettingInfo != null && SettingInfo.Increment != null && SettingInfo.Increment.Count > 0)
				return SettingInfo.Increment[0];
			return 1.0;
		}

		public void SaveNUDSettings() { }
	}

	public class DDSearchBar : System.Windows.Controls.UserControl
	{
		public event System.Windows.Controls.TextChangedEventHandler TextChanged;
		public string FindContent { get { return _textBox != null ? _textBox.Text : string.Empty; } }

		private readonly System.Windows.Controls.TextBox _textBox;

		public DDSearchBar(System.Windows.Media.Brush foreground, System.Windows.Media.Brush background, int row, int column, string placeholder)
		{
			_textBox = new System.Windows.Controls.TextBox
			{
				Foreground = foreground ?? System.Windows.Media.Brushes.Black,
				Background = background ?? System.Windows.Media.Brushes.Transparent,
				ToolTip = placeholder
			};
			_textBox.TextChanged += (s, e) =>
			{
				var handler = TextChanged;
				if (handler != null) handler(this, e);
			};
			SetValue(System.Windows.Controls.Grid.RowProperty, row);
			SetValue(System.Windows.Controls.Grid.ColumnProperty, column);
			Content = _textBox;
		}
	}

	public class ShowYesNoMessageWindow
	{
		public System.Windows.Window YesNoMessageWindow;
		private readonly System.Windows.Media.Brush _backgroundBrush;

		public ShowYesNoMessageWindow(System.Windows.Media.Brush backgroundBrush)
		{
			_backgroundBrush = backgroundBrush;
		}

		public void ShowMessageBoxOKButtonOnly(System.Windows.Window owner, string message, System.Windows.Media.Brush textBrush, string caption = "", string buttonYesText = "OK")
		{
			CloseExisting();
			var win = new System.Windows.Window
			{
				Owner = owner,
				Title = caption ?? string.Empty,
				Width = 420,
				Height = 200,
				Background = _backgroundBrush ?? System.Windows.Media.Brushes.White,
				WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner,
				ResizeMode = System.Windows.ResizeMode.NoResize,
				ShowInTaskbar = false,
				Topmost = true
			};
			var grid = new System.Windows.Controls.Grid();
			grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = new System.Windows.GridLength(1.0, System.Windows.GridUnitType.Star) });
			grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = System.Windows.GridLength.Auto });
			var tb = new System.Windows.Controls.TextBlock
			{
				Text = message ?? string.Empty,
				Foreground = textBrush ?? System.Windows.Media.Brushes.Black,
				Margin = new System.Windows.Thickness(20),
				TextWrapping = System.Windows.TextWrapping.Wrap,
				VerticalAlignment = System.Windows.VerticalAlignment.Center,
				HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
				TextAlignment = System.Windows.TextAlignment.Center
			};
			tb.SetValue(System.Windows.Controls.Grid.RowProperty, 0);
			var btn = new System.Windows.Controls.Button
			{
				Content = string.IsNullOrEmpty(buttonYesText) ? "OK" : buttonYesText,
				Width = 80,
				Margin = new System.Windows.Thickness(10),
				HorizontalAlignment = System.Windows.HorizontalAlignment.Center
			};
			btn.SetValue(System.Windows.Controls.Grid.RowProperty, 1);
			btn.Click += (s, e) => win.Close();
			grid.Children.Add(tb);
			grid.Children.Add(btn);
			win.Content = grid;
			YesNoMessageWindow = win;
			win.Show();
		}

		private void CloseExisting()
		{
			if (YesNoMessageWindow != null)
			{
				try { YesNoMessageWindow.Close(); } catch { }
				YesNoMessageWindow = null;
			}
		}
	}

	public static class DD_ImageCreator
	{
		public static System.Windows.Controls.Image CreateImageFromCode(string base64Png)
		{
			try
			{
				byte[] bytes = System.Convert.FromBase64String(base64Png);
				var bmp = new System.Windows.Media.Imaging.BitmapImage();
				using (var ms = new System.IO.MemoryStream(bytes))
				{
					bmp.BeginInit();
					bmp.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
					bmp.StreamSource = ms;
					bmp.EndInit();
					bmp.Freeze();
				}
				return new System.Windows.Controls.Image { Source = bmp };
			}
			catch
			{
				return new System.Windows.Controls.Image();
			}
		}
	}

	[CategoryOrder("Alerts", 1000030)]
	[CategoryOrder("Graphics", 1000020)]
	[CategoryOrder("Control Panel", 1000040)]
	[CategoryOrder("Windows", 1000050)]
	[CategoryOrder("General", 1000010)]
	[TypeConverter(typeof(DDDeepStackConfluence_Converter))]
	[CategoryOrder("Critical", 1000070)]
	[CategoryOrder("Special", 1000060)]
	[CategoryOrder("Developer", 0)]
	public class DDDeepStackConfluence : Indicator
	{
		[Display(Name = "Condition: Sync Signal Start", Order = 0, GroupName = "Alerts")]
		public bool ConditionSyncSignalStart { get; set; }
		[Display(Name = "Condition: Sync Signal Continuing", Order = 1, GroupName = "Alerts")]
		public bool ConditionSyncSignalContinuing { get; set; }
		[Display(Name = "Condition: Sync Signal End", Order = 2, GroupName = "Alerts")]
		public bool ConditionSyncSignalEnd { get; set; }
		[Display(Name = "Marker: Enabled", Order = 30, GroupName = "Alerts")]
		public bool MarkerEnabled { get; set; }
		[Display(Name = "Marker: Rendering Method", Order = 31, GroupName = "Alerts", Description = "\"Custom\" rendering provides a better appearance, and is therefore recommended in most cases.\nIf your computer is weak or slow, please switch to \"Builtin\" rendering.")]
		public DDDeepStackConfluence_RenderingMethod MarkerRenderingMethod { get; set; }
		[XmlIgnore]
		[Display(Name = "Marker: Color Bullish", Order = 32, GroupName = "Alerts")]
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
		[Display(Name = "Marker: Color Bearish", Order = 33, GroupName = "Alerts")]
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
		[Display(Name = "Marker: String Sync Signal Bullish Start", Order = 36, GroupName = "Alerts")]
		public string MarkerStringSyncSignalBullishStart { get; set; }
		[Display(Name = "Marker: String Sync Signal Bearish Start", Order = 37, GroupName = "Alerts")]
		public string MarkerStringSyncSignalBearishStart { get; set; }
		[Display(Name = "Marker: String Sync Signal Bullish Continuing", Order = 38, GroupName = "Alerts")]
		public string MarkerStringSyncSignalBullishContinuing { get; set; }
		[Display(Name = "Marker: String Sync Signal Bearish Continuing", Order = 39, GroupName = "Alerts")]
		public string MarkerStringSyncSignalBearishContinuing { get; set; }
		[Display(Name = "Marker: String Sync Signal Bullish End", Order = 40, GroupName = "Alerts")]
		public string MarkerStringSyncSignalBullishEnd { get; set; }
		[Display(Name = "Marker: String Sync Signal Bearish End", Order = 41, GroupName = "Alerts")]
		public string MarkerStringSyncSignalBearishEnd { get; set; }
		[Display(Name = "Marker: Font", Order = 42, GroupName = "Alerts")]
		public SimpleFont MarkerFont { get; set; }
		[Display(Name = "Marker: Offset", Order = 43, GroupName = "Alerts")]
		public int MarkerOffset { get; set; }
		[Range(99, 500)]
		[Display(Name = "Screen DPI", Order = 100, GroupName = "General")]
		public int ScreenDPI { get; set; }
		[XmlIgnore]
		[Display(Name = "Plot: Bullish", Order = 0, GroupName = "Graphics")]
		public global::System.Windows.Media.Brush PlotBullish { get; set; }
		[Browsable(false)]
		public string PlotBullish_Serialize
		{
			get
			{
				return Serialize.BrushToString(this.PlotBullish);
			}
			set
			{
				this.PlotBullish = Serialize.StringToBrush(value);
			}
		}
		[XmlIgnore]
		[Display(Name = "Plot: Bearish", Order = 2, GroupName = "Graphics")]
		public global::System.Windows.Media.Brush PlotBearish { get; set; }
		[Browsable(false)]
		public string PlotBearish_Serialize
		{
			get
			{
				return Serialize.BrushToString(this.PlotBearish);
			}
			set
			{
				this.PlotBearish = Serialize.StringToBrush(value);
			}
		}
		[XmlIgnore]
		[Display(Name = "Plot: Neutral", Order = 4, GroupName = "Graphics")]
		public global::System.Windows.Media.Brush PlotNeutral { get; set; }
		[Browsable(false)]
		public string PlotNeutral_Serialize
		{
			get
			{
				return Serialize.BrushToString(this.PlotNeutral);
			}
			set
			{
				this.PlotNeutral = Serialize.StringToBrush(value);
			}
		}
		[Range(1, 2147483647)]
		[Display(Name = "Plot: Width", Order = 5, GroupName = "Graphics")]
		public int PlotWidth { get; set; }
		[Display(Name = "Bar: Enabled", Order = 10, GroupName = "Graphics")]
		public bool BarEnabled { get; set; }
		[Display(Name = "Bar: Bullish", Order = 12, GroupName = "Graphics")]
		[XmlIgnore]
		public global::System.Windows.Media.Brush BarBullish { get; set; }
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
		[Display(Name = "Bar: Bearish", Order = 14, GroupName = "Graphics")]
		[XmlIgnore]
		public global::System.Windows.Media.Brush BarBearish { get; set; }
		[Browsable(false)]
		public string Bearish_Serialize
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
		[Display(Name = "Bar: Outline Enabled", Order = 16, GroupName = "Graphics")]
		public bool BarOutlineEnabled { get; set; }
		[Display(Name = "Bar: Bias Based", Order = 18, GroupName = "Graphics")]
		public bool BarBiasBased { get; set; }
		[Display(Name = "Background: Enabled", Order = 30, GroupName = "Graphics")]
		public bool BackgroundEnabled { get; set; }
		[XmlIgnore]
		[Display(Name = "Background: Bullish", Order = 32, GroupName = "Graphics")]
		public global::System.Windows.Media.Brush BackgroundBullish { get; set; }
		[Browsable(false)]
		public string BackgroundOverlapOB_Serialize
		{
			get
			{
				return Serialize.BrushToString(this.BackgroundBullish);
			}
			set
			{
				this.BackgroundBullish = Serialize.StringToBrush(value);
			}
		}
		[XmlIgnore]
		[Display(Name = "Background: Bearish", Order = 34, GroupName = "Graphics")]
		public global::System.Windows.Media.Brush BackgroundBearish { get; set; }
		[Browsable(false)]
		public string BackgroundOverlapOS_Serialize
		{
			get
			{
				return Serialize.BrushToString(this.BackgroundBearish);
			}
			set
			{
				this.BackgroundBearish = Serialize.StringToBrush(value);
			}
		}
		[Display(Name = "Background: Opacity", Order = 36, GroupName = "Graphics")]
		[Range(0, 100)]
		public int BackgroundOpacity { get; set; }
		[Display(Name = "Ribbon: Enabled", Order = 40, GroupName = "Graphics")]
		public bool RibbonEnabled { get; set; }
		[Display(Name = "Ribbon: Position", Order = 42, GroupName = "Graphics")]
		public DDDeepStackConfluence_RibbonPosition RibbonPosition { get; set; }
		[XmlIgnore]
		[Display(Name = "Ribbon: Bullish", Order = 44, GroupName = "Graphics")]
		public global::System.Windows.Media.Brush RibbonBullish { get; set; }
		[Browsable(false)]
		public string RibbonBullish_Serialize
		{
			get
			{
				return Serialize.BrushToString(this.RibbonBullish);
			}
			set
			{
				this.RibbonBullish = Serialize.StringToBrush(value);
			}
		}
		[Display(Name = "Ribbon: Bearish", Order = 46, GroupName = "Graphics")]
		[XmlIgnore]
		public global::System.Windows.Media.Brush RibbonBearish { get; set; }
		[Browsable(false)]
		public string RibbonBearish_Serialize
		{
			get
			{
				return Serialize.BrushToString(this.RibbonBearish);
			}
			set
			{
				this.RibbonBearish = Serialize.StringToBrush(value);
			}
		}
		[XmlIgnore]
		[Display(Name = "Ribbon: Neutral", Order = 48, GroupName = "Graphics")]
		public global::System.Windows.Media.Brush RibbonNeutral { get; set; }
		[Browsable(false)]
		public string RibbonNeutral_Serialize
		{
			get
			{
				return Serialize.BrushToString(this.RibbonNeutral);
			}
			set
			{
				this.RibbonNeutral = Serialize.StringToBrush(value);
			}
		}
		[Display(Name = "Ribbon: Opacity", Order = 50, GroupName = "Graphics")]
		[Range(0, 100)]
		public int RibbonOpacity { get; set; }
		[Display(Name = "Ribbon: Height", Order = 52, GroupName = "Graphics")]
		[Range(1, 2147483647)]
		public int RibbonHeight { get; set; }
		[Range(1, 2147483647)]
		[Display(Name = "Ribbon: Distance", Order = 54, GroupName = "Graphics")]
		public int RibbonDistance { get; set; }
		[Range(0, 2147483647)]
		[Display(Name = "Ribbon: Margin", Order = 56, GroupName = "Graphics")]
		public int RibbonMargin { get; set; }
		[Display(Name = "Font", Order = 94, GroupName = "Graphics")]
		public SimpleFont Font { get; set; }
		[Display(Name = "Data Series #1: Type", Order = 0, GroupName = "Parameters")]
		public string DataSeries1Type
		{
			[MethodImpl(MethodImplOptions.NoInlining)]
			get
			{
				string text = base.BarsPeriod.BarsPeriodType.ToString();
				if (this.IsNullBarsPeriods())
				{
					return text;
				}
				if (this.IsDDRenkoOrKingRenkoBarType())
				{
					foreach (string text2 in NinjaTrader.NinjaScript.Indicators.DimDim.DDDeepStackConfluence.dictRenkoInfo.Keys)
					{
						if (text == text2)
						{
							return NinjaTrader.NinjaScript.Indicators.DimDim.DDDeepStackConfluence.dictRenkoInfo[text2];
						}
					}
					return string.Empty;
				}
				return text;
			}
		}
		[Display(Name = "Data Series #1: Value", Order = 2, GroupName = "Parameters")]
		public string DataSeries1Value
		{
			[MethodImpl(MethodImplOptions.NoInlining)]
			get
			{
				BarsPeriodType barsPeriodType = base.BarsPeriod.BarsPeriodType;
				if (this.IsNullBarsPeriods())
				{
					return barsPeriodType.ToString();
				}
				if (this.IsHeikenAshiType())
				{
					return string.Format("{0}{1}", base.BarsPeriod.BaseBarsPeriodValue, base.BarsPeriod.BaseBarsPeriodType);
				}
				bool flag = this.IsDDRenkoOrKingRenkoBarType();
				string text = base.BarsPeriod.Value.ToString();
				if (!flag)
				{
					return text;
				}
				return string.Format("{0}/{1}", text, base.BarsPeriod.Value2);
			}
		}
		[Display(Name = "Data Series #2: Type", Order = 4, GroupName = "Parameters")]
		[RefreshProperties(RefreshProperties.All)]
		[PropertyEditor("NinjaTrader.Gui.Tools.StringStandardValuesEditorKey")]
		[TypeConverter(typeof(DDDeepStackConfluence_TypeDataSeriesConverter))]
		public DDDeepStackConfluence_DataSeriesType DataSeries2Type { get; set; }
		[NinjaScriptProperty]
		[Range(1, 2147483647)]
		[Display(Name = "Data Series #2: Value", Order = 6, GroupName = "Parameters")]
		public int DataSeries2Value
		{
			get
			{
				return this._DataSeries2Value;
			}
			set
			{
				this._DataSeries2Value = value;
			}
		}
		[Range(1, 2147483647)]
		[NinjaScriptProperty]
		[Display(Name = "Data Series #2: Value 1", Order = 8, GroupName = "Parameters")]
		public int DataSeries2Value1
		{
			get
			{
				return this._DataSeries2Value1;
			}
			set
			{
				this._DataSeries2Value1 = value;
			}
		}
		[Range(1, 2147483647)]
		[NinjaScriptProperty]
		[Display(Name = "Data Series #2: Value 2", Order = 10, GroupName = "Parameters")]
		public int DataSeries2Value2
		{
			get
			{
				return this._DataSeries2Value2;
			}
			set
			{
				this._DataSeries2Value2 = value;
			}
		}
		[PropertyEditor("NinjaTrader.Gui.Tools.StringStandardValuesEditorKey")]
		[TypeConverter(typeof(DDDeepStackConfluence_TypeDataSeriesConverter))]
		[RefreshProperties(RefreshProperties.All)]
		[Display(Name = "Data Series #3: Type", Order = 14, GroupName = "Parameters")]
		public DDDeepStackConfluence_DataSeriesType DataSeries3Type { get; set; }
		[NinjaScriptProperty]
		[Range(1, 2147483647)]
		[Display(Name = "Data Series #3: Value", Order = 16, GroupName = "Parameters")]
		public int DataSeries3Value
		{
			get
			{
				return this._DataSeries3Value;
			}
			set
			{
				this._DataSeries3Value = value;
			}
		}
		[Range(1, 2147483647)]
		[Display(Name = "Data Series #3: Value 1", Order = 18, GroupName = "Parameters")]
		[NinjaScriptProperty]
		public int DataSeries3Value1
		{
			get
			{
				return this._DataSeries3Value1;
			}
			set
			{
				this._DataSeries3Value1 = value;
			}
		}
		[Display(Name = "Data Series #3: Value 2", Order = 20, GroupName = "Parameters")]
		[Range(1, 2147483647)]
		[NinjaScriptProperty]
		public int DataSeries3Value2
		{
			get
			{
				return this._DataSeries3Value2;
			}
			set
			{
				this._DataSeries3Value2 = value;
			}
		}
		[PropertyEditor("NinjaTrader.Gui.Tools.StringStandardValuesEditorKey")]
		[Display(Name = "Data Series #4: Type", Order = 24, GroupName = "Parameters")]
		[TypeConverter(typeof(DDDeepStackConfluence_TypeDataSeriesConverter))]
		[RefreshProperties(RefreshProperties.All)]
		public DDDeepStackConfluence_DataSeriesType DataSeries4Type { get; set; }
		[NinjaScriptProperty]
		[Display(Name = "Data Series #4: Value", Order = 26, GroupName = "Parameters")]
		[Range(1, 2147483647)]
		public int DataSeries4Value
		{
			get
			{
				return this._DataSeries4Value;
			}
			set
			{
				this._DataSeries4Value = value;
			}
		}
		[NinjaScriptProperty]
		[Display(Name = "Data Series #4: Value 1", Order = 28, GroupName = "Parameters")]
		[Range(1, 2147483647)]
		public int DataSeries4Value1
		{
			get
			{
				return this._DataSeries4Value1;
			}
			set
			{
				this._DataSeries4Value1 = value;
			}
		}
		[NinjaScriptProperty]
		[Display(Name = "Data Series #4: Value 2", Order = 30, GroupName = "Parameters")]
		[Range(1, 2147483647)]
		public int DataSeries4Value2
		{
			get
			{
				return this._DataSeries4Value2;
			}
			set
			{
				this._DataSeries4Value2 = value;
			}
		}
		[RefreshProperties(RefreshProperties.All)]
		[TypeConverter(typeof(DDDeepStackConfluence_TypeDataSeriesConverter))]
		[Display(Name = "Data Series #5: Type", Order = 34, GroupName = "Parameters")]
		[PropertyEditor("NinjaTrader.Gui.Tools.StringStandardValuesEditorKey")]
		public DDDeepStackConfluence_DataSeriesType DataSeries5Type { get; set; }
		[Display(Name = "Data Series #5: Value", Order = 36, GroupName = "Parameters")]
		[Range(1, 2147483647)]
		[NinjaScriptProperty]
		public int DataSeries5Value
		{
			get
			{
				return this._DataSeries5Value;
			}
			set
			{
				this._DataSeries5Value = value;
			}
		}
		[Display(Name = "Data Series #5: Value 1", Order = 38, GroupName = "Parameters")]
		[Range(1, 2147483647)]
		[NinjaScriptProperty]
		public int DataSeries5Value1
		{
			get
			{
				return this._DataSeries5Value1;
			}
			set
			{
				this._DataSeries5Value1 = value;
			}
		}
		[Display(Name = "Data Series #5: Value 2", Order = 40, GroupName = "Parameters")]
		[Range(1, 2147483647)]
		[NinjaScriptProperty]
		public int DataSeries5Value2
		{
			get
			{
				return this._DataSeries5Value2;
			}
			set
			{
				this._DataSeries5Value2 = value;
			}
		}
		[NinjaScriptProperty]
		[Display(Name = "Data Series Behind: Enabled", Order = 50, GroupName = "Parameters")]
		[RefreshProperties(RefreshProperties.All)]
		public bool DataSeriesBehindEnabled { get; set; }
		[Display(Name = "Data Series Behind: Type", Order = 52, GroupName = "Parameters")]
		[TypeConverter(typeof(DDDeepStackConfluence_TypeDataSeriesBehindConverter))]
		[PropertyEditor("NinjaTrader.Gui.Tools.StringStandardValuesEditorKey")]
		public DDDeepStackConfluence_DataSeriesType DataSeriesBehindType { get; set; }
		[Display(Name = "Data Series Behind: Value", Order = 54, GroupName = "Parameters")]
		[NinjaScriptProperty]
		[Range(1, 2147483647)]
		public int DataSeriesBehindValue { get; set; }
		[Display(Name = "Minimized", Order = 0, GroupName = "Control Panel")]
		public bool CpMinimized { get; set; }
		[Display(Name = "Title: Color", Order = 10, GroupName = "Control Panel")]
		[XmlIgnore]
		public global::System.Windows.Media.Brush CpTitleColor { get; set; }
		[Browsable(false)]
		public string CpTitleColor_Serialize
		{
			get
			{
				return Serialize.BrushToString(this.CpTitleColor);
			}
			set
			{
				this.CpTitleColor = Serialize.StringToBrush(value);
			}
		}
		[Display(Name = "Title: Text Color", Order = 12, GroupName = "Control Panel")]
		[XmlIgnore]
		public global::System.Windows.Media.Brush CpTitleTextBrush { get; set; }
		[Browsable(false)]
		public string CpTitleTextBrush_Serialize
		{
			get
			{
				return Serialize.BrushToString(this.CpTitleTextBrush);
			}
			set
			{
				this.CpTitleTextBrush = Serialize.StringToBrush(value);
			}
		}
		[Display(Name = "Label: Text Color", Order = 23, GroupName = "Control Panel")]
		[XmlIgnore]
		public global::System.Windows.Media.Brush CpLabelTextBrush { get; set; }
		[Browsable(false)]
		public string CpLabelTextBrush_Serialize
		{
			get
			{
				return Serialize.BrushToString(this.CpLabelTextBrush);
			}
			set
			{
				this.CpLabelTextBrush = Serialize.StringToBrush(value);
			}
		}
		[Display(Name = "Text: Size", Order = 30, GroupName = "Control Panel")]
		[Range(1, 2147483647)]
		public int CpTextSize { get; set; }
		[Display(Name = "Drag Bar: Color", Order = 40, GroupName = "Control Panel")]
		[XmlIgnore]
		public global::System.Windows.Media.Brush CpDragBarBrush { get; set; }
		[Browsable(false)]
		public string CpDragBarBrush_Serialize
		{
			get
			{
				return Serialize.BrushToString(this.CpDragBarBrush);
			}
			set
			{
				this.CpDragBarBrush = Serialize.StringToBrush(value);
			}
		}
		[Display(Name = "Position: Alignment", Order = 50, GroupName = "Control Panel")]
		public NinjaTrader.NinjaScript.DrawingTools.TextPosition CpPositionAlignment
		{
			get
			{
				return this.cpPanelPositionAlignment;
			}
			[MethodImpl(MethodImplOptions.NoInlining)]
			set
			{
				if (value == NinjaTrader.NinjaScript.DrawingTools.TextPosition.TopLeft)
				{
					this.CpPositionMarginTop = (this.CpPositionMarginLeft = 5.0);
				}
				if (value == NinjaTrader.NinjaScript.DrawingTools.TextPosition.TopRight)
				{
					this.CpPositionMarginTop = (this.CpPositionMarginRight = 5.0);
				}
				if (value == NinjaTrader.NinjaScript.DrawingTools.TextPosition.BottomRight)
				{
					this.CpPositionMarginBottom = (this.CpPositionMarginRight = 5.0);
				}
				if (value == NinjaTrader.NinjaScript.DrawingTools.TextPosition.BottomLeft)
				{
					this.CpPositionMarginBottom = (this.CpPositionMarginLeft = 5.0);
				}
				if (value == NinjaTrader.NinjaScript.DrawingTools.TextPosition.Center)
				{
					this.CpPositionMarginLeft = (this.CpPositionMarginTop = (this.CpPositionMarginRight = (this.CpPositionMarginBottom = 5.0)));
				}
				this.cpPanelPositionAlignment = value;
			}
		}
		[Display(Name = "Position: Margin Left", Order = 60, GroupName = "Control Panel")]
		public double CpPositionMarginLeft { get; set; }
		[Display(Name = "Position: Margin Top", Order = 61, GroupName = "Control Panel")]
		public double CpPositionMarginTop { get; set; }
		[Display(Name = "Position: Margin Right", Order = 62, GroupName = "Control Panel")]
		public double CpPositionMarginRight { get; set; }
		[Display(Name = "Position: Margin Bottom", Order = 63, GroupName = "Control Panel")]
		public double CpPositionMarginBottom { get; set; }
		[Display(Name = "Main Window: Text Color", Order = 0, GroupName = "Windows")]
		[XmlIgnore]
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
		[Display(Name = "Main Window: Left", Order = 20, GroupName = "Windows")]
		public double MainWindowLeft { get; set; }
		[Display(Name = "Main Window: Top", Order = 22, GroupName = "Windows")]
		public double MainWindowTop { get; set; }
		[Display(Name = "Main Window: Width", Order = 24, GroupName = "Windows")]
		public double MainWindowWidth { get; set; }
		[Display(Name = "Main Window: Height", Order = 26, GroupName = "Windows")]
		public double MainWindowHeight { get; set; }
		[Display(Name = "Child Window: Background", Order = 30, GroupName = "Windows")]
		public string ChildWindowBackground { get; set; }
		[Display(Name = "Z Order", Order = 0, GroupName = "Special")]
		public int IndicatorZOrder { get; set; }
		[Display(Name = "User Note", Order = 10, GroupName = "Special")]
		public string UserNote { get; set; }
		[Display(Name = "Documents Path", Order = 10, GroupName = "Critical")]
		public string DocumentsPath { get; set; }
		[Display(Name = "Indicators Config JSON", Order = 20, GroupName = "Critical")]
		public string IndicatorsConfigJSON { get; set; }
		[Display(Name = "Indicators Params JSON", Order = 30, GroupName = "Critical")]
		public string IndicatorsParamsJSON { get; set; }
		[Display(Name = "Namespace", Order = 20, GroupName = "Critical")]
		public string Namespace { get; set; }
		[Display(Name = "Input Series Index", Order = 40, GroupName = "Critical")]
		public int InputSeriesIndex { get; set; }
		[Display(Name = "Plot Index", Order = 50, GroupName = "Critical")]
		public int PlotIndex { get; set; }
		[Display(Name = "Value: Bullish", Order = 60, GroupName = "Critical")]
		public double ValueBullish { get; set; }
		[Display(Name = "Value: Bearish", Order = 62, GroupName = "Critical")]
		public double ValueBearish { get; set; }
		[Display(Name = "Operator: Bullish", Order = 64, GroupName = "Critical")]
		public DDDeepStackConfluence_Operators OperatorBullish { get; set; }
		[Display(Name = "Operator: Bearish", Order = 66, GroupName = "Critical")]
		public DDDeepStackConfluence_Operators OperatorBearish { get; set; }
		[Browsable(false)]
		[XmlIgnore]
		public Series<double> Signal_State
		{
			get
			{
				return base.Values[0];
			}
		}
		[Browsable(false)]
		[XmlIgnore]
		public Series<double> Signal_Trade
		{
			get
			{
				return base.Values[1];
			}
		}
		public override string DisplayName
		{
			[MethodImpl(MethodImplOptions.NoInlining)]
			get
			{
				if (base.Parent is MarketAnalyzerColumnBase)
				{
					return base.DisplayName;
				}
				return "DeepStack Confluence by DD.co" + this.GetUserNote();
			}
		}
		[MethodImpl(MethodImplOptions.NoInlining)]
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
		[MethodImpl(MethodImplOptions.NoInlining)]
		protected override void OnStateChange()
		{
			try
			{
				if (base.State == State.SetDefaults)
				{
					base.Description = string.Empty;
					base.Name = "DDDeepStackConfluence";
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
					this.ConditionSyncSignalStart = true;
					this.ConditionSyncSignalContinuing = false;
					this.ConditionSyncSignalEnd = true;
					this.MarkerEnabled = true;
					this.MarkerRenderingMethod = DDDeepStackConfluence_RenderingMethod.Custom;
					this.MarkerBrushBullish = Brushes.DodgerBlue;
					this.MarkerBrushBearish = Brushes.HotPink;
					this.MarkerStringSyncSignalBullishStart = "▲ + DSC";
					this.MarkerStringSyncSignalBearishStart = "DSC + ▼";
					this.MarkerStringSyncSignalBullishContinuing = "•";
					this.MarkerStringSyncSignalBearishContinuing = "•";
					this.MarkerStringSyncSignalBullishEnd = "⯀";
					this.MarkerStringSyncSignalBearishEnd = "⯀";
					this.MarkerFont = new SimpleFont("Arial", 20);
					this.MarkerOffset = 10;
					this.ScreenDPI = 99;
					this.PlotBullish = Brushes.LimeGreen;
					this.PlotBearish = Brushes.HotPink;
					this.PlotNeutral = Brushes.Gray;
					this.PlotWidth = 5;
					this.BarEnabled = true;
					this.BarBullish = Brushes.DodgerBlue;
					this.BarBearish = Brushes.DeepPink;
					this.BarOutlineEnabled = true;
					this.BarBiasBased = true;
					this.BackgroundEnabled = true;
					this.BackgroundBullish = Brushes.DeepSkyBlue;
					this.BackgroundBearish = Brushes.DeepPink;
					this.BackgroundOpacity = 30;
					this.RibbonEnabled = true;
					this.RibbonPosition = DDDeepStackConfluence_RibbonPosition.Bottom;
					this.RibbonBullish = Brushes.LimeGreen;
					this.RibbonBearish = Brushes.HotPink;
					this.RibbonNeutral = Brushes.Gray;
					this.RibbonOpacity = 80;
					this.RibbonHeight = 10;
					this.RibbonDistance = 5;
					this.RibbonMargin = 4;
					this.Font = new SimpleFont("Arial", 10);
					this.DataSeries2Type = DDDeepStackConfluence_DataSeriesType.Minute;
					this.DataSeries2Value = 2;
					this.DataSeries2Value1 = 3;
					this.DataSeries2Value2 = 1;
					this.DataSeries3Type = DDDeepStackConfluence_DataSeriesType.Minute;
					this.DataSeries3Value = 3;
					this.DataSeries3Value1 = 6;
					this.DataSeries3Value2 = 2;
					this.DataSeries4Type = DDDeepStackConfluence_DataSeriesType.Minute;
					this.DataSeries4Value = 4;
					this.DataSeries4Value1 = 8;
					this.DataSeries4Value2 = 4;
					this.DataSeries5Type = DDDeepStackConfluence_DataSeriesType.Minute;
					this.DataSeries5Value = 5;
					this.DataSeries5Value1 = 12;
					this.DataSeries5Value2 = 4;
					this.DataSeriesBehindEnabled = false;
					this.DataSeriesBehindType = DDDeepStackConfluence_DataSeriesType.Tick;
					this.DataSeriesBehindValue = 1;
					this.CpMinimized = false;
					this.CpTitleColor = Brushes.LimeGreen;
					this.CpTitleTextBrush = Brushes.White;
					this.CpLabelTextBrush = Brushes.White;
					this.CpDragBarBrush = Brushes.LimeGreen;
					this.CpTextSize = 13;
					this.CpPositionAlignment = NinjaTrader.NinjaScript.DrawingTools.TextPosition.TopLeft;
					this.CpPositionMarginLeft = 5.0;
					this.CpPositionMarginTop = 5.0;
					this.CpPositionMarginRight = 5.0;
					this.CpPositionMarginBottom = 5.0;
					this.MainWindowTextColor = Brushes.Transparent;
					this.MainWindowLeft = (this.MainWindowTop = 0.0);
					this.MainWindowWidth = 630.0;
					this.MainWindowHeight = 620.0;
					this.ChildWindowBackground = string.Empty;
					this.IndicatorZOrder = 0;
					this.UserNote = "instrument (period)";
					this.DocumentsPath = string.Empty;
					this.IndicatorsConfigJSON = string.Empty;
					this.IndicatorsParamsJSON = string.Empty;
					this.Namespace = string.Empty;
					this.InputSeriesIndex = 0;
					this.PlotIndex = 0;
					this.ValueBullish = 1.0;
					this.ValueBearish = -1.0;
					this.OperatorBullish = DDDeepStackConfluence_Operators.Equal;
					this.OperatorBearish = DDDeepStackConfluence_Operators.Equal;
					base.AddPlot(Brushes.Transparent, "Signal: State");
					base.AddPlot(Brushes.Transparent, "Signal: Trade");
				}
				else if (base.State == State.Configure)
				{
					if (!string.IsNullOrWhiteSpace(this.DocumentsPath))
					{
						if (!Directory.Exists(this.DocumentsPath))
						{
							this.DocumentsPath = string.Empty;
						}
						else
						{
							if (!this.DocumentsPath.EndsWith("\\"))
							{
								this.DocumentsPath += "\\";
							}
							if (!File.Exists(this.DocumentsPath + "NinjaTrader.Custom.dll"))
							{
								this.DocumentsPath = string.Empty;
							}
						}
					}
					if (string.IsNullOrWhiteSpace(this.DocumentsPath))
					{
						this.DocumentsPath = Path.Combine(DDResources_GlobalConstantAndFunction.GetDefaultDocumentsPath(), "NinjaTrader 8\\bin\\Custom\\");
					}
					if (!this.IsNullBarsPeriods())
					{
						Type typeFromHandle = typeof(DDDeepStackConfluence);
						string text = ((typeFromHandle == null) ? string.Empty : typeFromHandle.Name);
						if (!string.IsNullOrWhiteSpace(text))
						{
							this.listIndicatorExcluded.Insert(0, text);
						}
						this.dictMethodInfo = new Dictionary<string, MethodInfo>();
						this.sortedListIndicatorItem = new SortedList<string, DDDeepStackConfluence.IndicatorItem>();
						this.GetAllMethods();
						this.listTimeframeStr = new List<string>();
						this.timeframeInfoArr = new int[4][];
						bool flag = this.IsDDRenkoOrKingRenkoBarType();
						bool flag2 = this.IsHeikenAshiType();
						string text2 = (flag ? this.DataSeries1Type[0].ToString() : (flag2 ? "HA" : this.DataSeries1Value));
						string text3 = (flag ? this.DataSeries1Value : (flag2 ? (this.DataSeries1Value[0].ToString() + this.DataSeries1Value[1].ToString().ToLower()) : ((this.DataSeries1Type == "Month" || this.DataSeries1Type == "Day" || this.DataSeries1Type == "Week" || this.DataSeries1Type == "Year") ? this.DataSeries1Type[0].ToString() : this.DataSeries1Type[0].ToString().ToLower())));
						string text4 = text2 + text3;
						this.listTimeframeStr.Add(text4);
						if (this.DataSeries2Type == DDDeepStackConfluence_DataSeriesType.Disabled)
						{
							this.DataSeries2Type = DDDeepStackConfluence_DataSeriesType.Minute;
						}
						if (this.FilterTimeframe(this.DataSeries2Type, this.DataSeries2Value, this.DataSeries2Value1, this.DataSeries2Value2))
						{
							BarsPeriod barsPeriod = this.ComputeBarsPeriod(this.DataSeries2Type, this.DataSeries2Value, this.DataSeries2Value1, this.DataSeries2Value2);
							this.DD_AddDataSeries(barsPeriod);
						}
						if (this.FilterTimeframe(this.DataSeries3Type, this.DataSeries3Value, this.DataSeries3Value1, this.DataSeries3Value2))
						{
							BarsPeriod barsPeriod2 = this.ComputeBarsPeriod(this.DataSeries3Type, this.DataSeries3Value, this.DataSeries3Value1, this.DataSeries3Value2);
							this.DD_AddDataSeries(barsPeriod2);
						}
						if (this.FilterTimeframe(this.DataSeries4Type, this.DataSeries4Value, this.DataSeries4Value1, this.DataSeries4Value2))
						{
							BarsPeriod barsPeriod3 = this.ComputeBarsPeriod(this.DataSeries4Type, this.DataSeries4Value, this.DataSeries4Value1, this.DataSeries4Value2);
							this.DD_AddDataSeries(barsPeriod3);
						}
						if (this.FilterTimeframe(this.DataSeries5Type, this.DataSeries5Value, this.DataSeries5Value1, this.DataSeries5Value2))
						{
							BarsPeriod barsPeriod4 = this.ComputeBarsPeriod(this.DataSeries5Type, this.DataSeries5Value, this.DataSeries5Value1, this.DataSeries5Value2);
							this.DD_AddDataSeries(barsPeriod4);
						}
						this.timeframeInfoArr[0] = new int[]
						{
							(int)this.DataSeries2Type,
							this.DataSeries2Value,
							this.DataSeries2Value1,
							this.DataSeries2Value2
						};
						this.timeframeInfoArr[1] = new int[]
						{
							(int)this.DataSeries3Type,
							this.DataSeries3Value,
							this.DataSeries3Value1,
							this.DataSeries3Value2
						};
						this.timeframeInfoArr[2] = new int[]
						{
							(int)this.DataSeries4Type,
							this.DataSeries4Value,
							this.DataSeries4Value1,
							this.DataSeries4Value2
						};
						this.timeframeInfoArr[3] = new int[]
						{
							(int)this.DataSeries5Type,
							this.DataSeries5Value,
							this.DataSeries5Value1,
							this.DataSeries5Value2
						};
						this.isPricePanel = base.Panel <= 0;
						base.IsAutoScale = !this.RibbonEnabled || !this.isPricePanel;
						if (!string.IsNullOrWhiteSpace(this.IndicatorsConfigJSON) && !string.IsNullOrWhiteSpace(this.IndicatorsParamsJSON))
						{
							JavaScriptSerializer javaScriptSerializer = new JavaScriptSerializer();
							this.effListIndicatorConfig = javaScriptSerializer.Deserialize<List<DDDeepStackConfluence.IndicatorConfig>>(this.IndicatorsConfigJSON);
							List<string> list = javaScriptSerializer.Deserialize<List<string>>(this.IndicatorsParamsJSON);
							this.effListOfListParamInfo = new List<List<DDDeepStackConfluence.ParamInfo>>();
							int num = this.effListIndicatorConfig.Count - 1;
							bool flag3 = false;
							for (int i = num; i >= 0; i--)
							{
								if (this.sortedListIndicatorItem.ContainsKey(this.effListIndicatorConfig[i].Name))
								{
									string text5 = list[i];
									List<DDDeepStackConfluence.ParamInfo> list2 = javaScriptSerializer.Deserialize<List<DDDeepStackConfluence.ParamInfo>>(text5);
									if (i == num)
									{
										this.effListOfListParamInfo.Add(list2);
									}
									else
									{
										this.effListOfListParamInfo.Insert(0, list2);
									}
								}
								else
								{
									if (!flag3)
									{
										flag3 = true;
									}
									this.effListIndicatorConfig.RemoveAt(i);
								}
							}
							this.countIndicator = this.effListIndicatorConfig.Count;
							if (this.countIndicator > 0)
							{
								this.lblTextArr = new string[this.countIndicator];
								this.indicatorBaseArr = new IndicatorBase[this.countIndicator][];
								this.cacheIndicatorBaseArr = new IndicatorBase[this.countIndicator][][];
								this.dictPlotValue0Arr = new Dictionary<int, double>[this.countIndicator];
								this.dictPlotOrDataSeriesArr = new Dictionary<int, List<DDDeepStackConfluence.PlotOrDataSeriesInfo>>[this.countIndicator];
								this.signalStateArr = new int[this.countIndicator];
								this.countPlot = 2;
								this.maxLabelWidth = 0;
								for (int j = 0; j < this.countIndicator; j++)
								{
									DDDeepStackConfluence.IndicatorConfig indicatorConfig = this.effListIndicatorConfig[j];
									string text6 = indicatorConfig.DisplayName;
									if (text6.Contains(" by"))
									{
										text6 = text6.Substring(0, text6.IndexOf(" by"));
									}
									if (text6.Contains("("))
									{
										text6 = text6.Substring(0, text6.IndexOf("("));
									}
									string[] array = indicatorConfig.TimeframeConfigStr.Split(new char[] { '&' }, StringSplitOptions.RemoveEmptyEntries);
									int num2 = array.Length;
									this.dictPlotValue0Arr[j] = new Dictionary<int, double>();
									List<int> list3 = new List<int>();
									string text7 = string.Empty;
									int num3 = 0;
									for (int k = 0; k < this.countDataSeries; k++)
									{
										string text8 = this.listTimeframeStr[k];
										for (int l = 0; l < num2; l++)
										{
											string text9 = array[l].Trim();
											if (!(text8 != text9))
											{
												list3.Add(k);
												this.dictPlotValue0Arr[j].Add(k, double.MinValue);
												text7 = ((num3 == 0) ? (text9 ?? "") : (text7 + "&" + text9));
												text6 += ((num3 == 0) ? (" [" + text9) : (" | " + text9));
												num3++;
												break;
											}
										}
									}
									if (num3 > 0)
									{
										text6 += "]";
									}
									else
									{
										string text10 = this.listTimeframeStr[0];
										text6 = text6 + " [" + text10 + "]";
										text7 = text10 ?? "";
										list3.Add(0);
										this.dictPlotValue0Arr[j].Add(0, double.MinValue);
									}
									this.lblTextArr[j] = text6;
									indicatorConfig.TimeframeConfigStr = text7;
									indicatorConfig.ListBarInProgressSelected = list3;
									int count = list3.Count;
									this.cacheIndicatorBaseArr[j] = new IndicatorBase[count][];
									this.indicatorBaseArr[j] = new IndicatorBase[count];
									Size2F size2F = this.ComputeTextSize(text6, this.Font, this.ScreenDPI);
									this.maxLabelWidth = Convert.ToInt32(Math.Max((float)this.maxLabelWidth, size2F.Width + 22f));
									this.maxLabelHeight = Convert.ToInt32(Math.Max((float)this.maxLabelHeight, size2F.Height));
									base.AddPlot(new Stroke(Brushes.Transparent, (float)this.PlotWidth), PlotStyle.Dot, string.Format("Signal: #{0}", j + 1));
									this.countPlot++;
								}
								base.ChartControl.Properties.BarMarginRight = Math.Max(this.maxLabelWidth, base.ChartControl.Properties.BarMarginRight);
								if (flag3)
								{
									List<string> list4 = new List<string>();
									for (int m = 0; m < this.countIndicator; m++)
									{
										string text11 = javaScriptSerializer.Serialize(this.effListOfListParamInfo[m]);
										list4.Add(text11);
									}
									this.IndicatorsConfigJSON = javaScriptSerializer.Serialize(this.effListIndicatorConfig);
									this.IndicatorsParamsJSON = javaScriptSerializer.Serialize(list4);
								}
								if (this.DataSeriesBehindEnabled)
								{
									BarsPeriod barsPeriod5 = this.ComputeBarsPeriod(this.DataSeriesBehindType, this.DataSeriesBehindValue, 0, 0);
									base.AddDataSeries(barsPeriod5);
								}
							}
							else
							{
								this.IndicatorsConfigJSON = (this.IndicatorsParamsJSON = string.Empty);
							}
							if (!this.isPricePanel)
							{
								this.MarkerRenderingMethod = DDDeepStackConfluence_RenderingMethod.Builtin;
							}
							if (this.RibbonEnabled && this.isPricePanel)
							{
								this.sortedListSignalInfo = new SortedList<int, int[]>();
							}
							this.isCustomRenderingMethod = this.MarkerRenderingMethod == DDDeepStackConfluence_RenderingMethod.Custom;
							if (this.isCustomRenderingMethod)
							{
								this.dictMarkers = new SortedList<int, DDDeepStackConfluence.MarkerInfo>();
							}
							this.isRibbonPositionBottom = this.RibbonPosition == DDDeepStackConfluence_RibbonPosition.Bottom;
							this.isOnBarCloseMode = base.Calculate == Calculate.OnBarClose;
							this.backgroundBullish = DD_BrushManager.CreateOpacityBrush(this.BackgroundBullish, this.BackgroundOpacity);
							this.backgroundBearish = DD_BrushManager.CreateOpacityBrush(this.BackgroundBearish, this.BackgroundOpacity);
							this.ribbonNeutral = DD_BrushManager.CreateOpacityBrush(this.RibbonNeutral, this.RibbonOpacity);
							this.ribbonBullish = DD_BrushManager.CreateOpacityBrush(this.RibbonBullish, this.RibbonOpacity);
							this.ribbonBearish = DD_BrushManager.CreateOpacityBrush(this.RibbonBearish, this.RibbonOpacity);
						}
					}
				}
				else if (base.State == State.DataLoaded)
				{
						this.LoadIndicator();
				}
				else if (base.State == State.Historical)
				{
						bool flag4 = this.ChildWindowBackground.Length == 7 && this.ChildWindowBackground.All(new Func<char, bool>("#0123456789abcdefABCDEF".Contains<char>));
						if (string.IsNullOrWhiteSpace(this.ChildWindowBackground) || (!flag4 && !this.MainWindowTextColor.IsTransparent()))
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
						NinjaTrader.NinjaScript.Indicators.DimDim.DDDeepStackConfluence.childWindowBackground = (global::System.Windows.Media.SolidColorBrush)new BrushConverter().ConvertFrom(this.ChildWindowBackground);
						NinjaTrader.NinjaScript.Indicators.DimDim.DDDeepStackConfluence.childWindowBackground.Freeze();
						if (this.ScreenDPI < 100)
						{
							this.ScreenDPI = this.GetDPI();
						}
						if (this.IndicatorZOrder != 0)
						{
							base.SetZOrder(this.IndicatorZOrder);
						}
						this.isCharting = base.ChartControl != null;
						if (this.isCharting)
						{
							base.ChartControl.Dispatcher.InvokeAsync(delegate
							{
								this.chartWindow = Window.GetWindow(base.ChartControl.Parent) as Chart;
								if (this.dragablePanel == null)
								{
									Thickness thickness = new Thickness(this.CpPositionMarginLeft, this.CpPositionMarginTop, this.CpPositionMarginRight, this.CpPositionMarginBottom);
									this.dragablePanel = new DDDeepStackConfluence.DragablePanel("DeepStack Confluence", this.timeframeInfoArr, this.CpTitleColor, this.CpTitleTextBrush, this.CpDragBarBrush, this.CpTextSize, this.CpPositionAlignment, thickness, this.CpMinimized, this.MainWindowTextColor, NinjaTrader.NinjaScript.Indicators.DimDim.DDDeepStackConfluence.childWindowBackground);
									ComboBox[] comboBoxArr = this.dragablePanel.comboBoxArr;
									DDNumericUpDown[] nudValueArr = this.dragablePanel.nudValueArr;
									int num4 = comboBoxArr.Length;
									for (int n = 0; n < num4; n++)
									{
										comboBoxArr[n].SelectionChanged += this.OnCmbTimeframe_SelectionChanged;
										DDNumericUpDown DDNumericUpDown = nudValueArr[n * 2];
										DDNumericUpDown DDNumericUpDown2 = nudValueArr[n * 2 + 1];
										DDNumericUpDown.NUDTextBox.TextChanged += this.OnNUDTextBox_TextChanged;
										DDNumericUpDown2.NUDTextBox.TextChanged += this.OnNUDTextBox_TextChanged;
									}
									this.dragablePanel.drag.DragDelta += this.OnCpDragDelta;
									this.dragablePanel.btnMini.Click += this.OnBtnMiniClick;
									this.dragablePanel.drag.MouseDoubleClick += this.OnBtnDragDoubleClick;
									this.dragablePanel.btnTitle.Click += this.OnBtnTitle_Click;
									base.UserControlCollection.Add(this.dragablePanel);
								}
								if (this.yesNoMessageWindow == null)
								{
									this.yesNoMessageWindow = new ShowYesNoMessageWindow(NinjaTrader.NinjaScript.Indicators.DimDim.DDDeepStackConfluence.childWindowBackground);
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
							if (this.mainWindowNT != null)
							{
								this.mainWindowNT.Close();
								this.mainWindowNT = null;
							}
							if (this.dragablePanel != null)
							{
								if (this.dragablePanel.settingWindow != null)
								{
									this.dragablePanel.settingWindow.Close();
									this.dragablePanel.settingWindow = null;
								}
								this.dragablePanel.drag.DragDelta -= this.OnCpDragDelta;
								this.dragablePanel.btnMini.Click -= this.OnBtnMiniClick;
								this.dragablePanel.drag.MouseDoubleClick -= this.OnBtnDragDoubleClick;
								this.dragablePanel.btnTitle.Click -= this.OnBtnTitle_Click;
								base.UserControlCollection.Remove(this.dragablePanel);
								this.dragablePanel = null;
								base.ChartControl.InvalidateVisual();
							}
							if (this.yesNoMessageWindow != null)
							{
								if (this.yesNoMessageWindow.YesNoMessageWindow != null)
								{
									this.yesNoMessageWindow.YesNoMessageWindow.Close();
									this.yesNoMessageWindow.YesNoMessageWindow = null;
								}
								this.yesNoMessageWindow = null;
							}
						});
					}
				}
			}
			catch (Exception ex)
			{
				this.PrintException(ex);
			}
		}
		[MethodImpl(MethodImplOptions.NoInlining)]
		private bool FilterTimeframe(DDDeepStackConfluence_DataSeriesType dataSeriesType, int dataSeriesValue, int dataSeriesValue1, int dataSeriesValue2)
		{
			if (dataSeriesType == DDDeepStackConfluence_DataSeriesType.Disabled)
			{
				return false;
			}
			string timeframeStr = this.GetTimeframeStr(dataSeriesType, dataSeriesValue, dataSeriesValue1, dataSeriesValue2);
			if (!string.IsNullOrWhiteSpace(timeframeStr) && !this.listTimeframeStr.Contains(timeframeStr))
			{
				this.listTimeframeStr.Add(timeframeStr);
				return true;
			}
			return false;
		}
		[MethodImpl(MethodImplOptions.NoInlining)]
		private bool IsNullBarsPeriods()
		{
			if (base.BarsPeriods != null && base.BarsPeriods.Length != 0)
			{
				return base.BarsPeriods.Any((BarsPeriod x) => x == null);
			}
			return true;
		}
		[MethodImpl(MethodImplOptions.NoInlining)]
		private bool IsDDRenkoOrKingRenkoBarType()
		{
			BarsPeriodType barsPeriodType = base.BarsPeriod.BarsPeriodType;
			foreach (string text in NinjaTrader.NinjaScript.Indicators.DimDim.DDDeepStackConfluence.dictRenkoInfo.Keys)
			{
				if (barsPeriodType == (BarsPeriodType)int.Parse(text))
				{
					return true;
				}
			}
			return false;
		}
		[MethodImpl(MethodImplOptions.NoInlining)]
		private bool IsHeikenAshiType()
		{
			return base.BarsPeriod.BarsPeriodType == BarsPeriodType.HeikenAshi;
		}
		[MethodImpl(MethodImplOptions.NoInlining)]
		private void DD_AddDataSeries(BarsPeriod barsPeriod)
		{
			if (barsPeriod == null)
			{
				return;
			}
			if (string.IsNullOrWhiteSpace(barsPeriod.ToString()))
			{
				return;
			}
			base.AddDataSeries(barsPeriod);
			this.countDataSeries++;
		}
		[MethodImpl(MethodImplOptions.NoInlining)]
		private BarsPeriod ComputeBarsPeriod(DDDeepStackConfluence_DataSeriesType dataSeriesType, int value, int value1, int value2)
		{
			BarsPeriod barsPeriod2;
			try
			{
				BarsPeriod barsPeriod;
				if (dataSeriesType == DDDeepStackConfluence_DataSeriesType.ninZaRenko || dataSeriesType == DDDeepStackConfluence_DataSeriesType.KingRenko)
				{
					bool flag = dataSeriesType == DDDeepStackConfluence_DataSeriesType.ninZaRenko;
					barsPeriod = new BarsPeriod
					{
						BarsPeriodType = (flag ? ((BarsPeriodType)12345) : ((BarsPeriodType)678910)),
						Value = value1,
						Value2 = value2
					};
				}
				else
				{
					BarsPeriodType barsPeriodType;
					if (dataSeriesType == DDDeepStackConfluence_DataSeriesType.Tick)
					{
						barsPeriodType = BarsPeriodType.Tick;
					}
					else if (dataSeriesType == DDDeepStackConfluence_DataSeriesType.Volume)
					{
						barsPeriodType = BarsPeriodType.Volume;
					}
					else if (dataSeriesType == DDDeepStackConfluence_DataSeriesType.Range)
					{
						barsPeriodType = BarsPeriodType.Range;
					}
					else if (dataSeriesType == DDDeepStackConfluence_DataSeriesType.Second)
					{
						barsPeriodType = BarsPeriodType.Second;
					}
					else if (dataSeriesType == DDDeepStackConfluence_DataSeriesType.Minute)
					{
						barsPeriodType = BarsPeriodType.Minute;
					}
					else if (dataSeriesType == DDDeepStackConfluence_DataSeriesType.Day)
					{
						barsPeriodType = BarsPeriodType.Day;
					}
					else if (dataSeriesType == DDDeepStackConfluence_DataSeriesType.Week)
					{
						barsPeriodType = BarsPeriodType.Week;
					}
					else if (dataSeriesType == DDDeepStackConfluence_DataSeriesType.Month)
					{
						barsPeriodType = BarsPeriodType.Month;
					}
					else
					{
						barsPeriodType = BarsPeriodType.Year;
					}
					barsPeriod = new BarsPeriod
					{
						BarsPeriodType = barsPeriodType,
						Value = value
					};
				}
				barsPeriod2 = barsPeriod;
			}
			catch (Exception ex)
			{
				this.PrintException(ex);
				barsPeriod2 = null;
			}
			return barsPeriod2;
		}
		[MethodImpl(MethodImplOptions.NoInlining)]
		private string GetTimeframeStr(DDDeepStackConfluence_DataSeriesType dataSeriesType, int value, int value1, int value2)
		{
			string text3;
			try
			{
				string text;
				if (dataSeriesType == DDDeepStackConfluence_DataSeriesType.ninZaRenko || dataSeriesType == DDDeepStackConfluence_DataSeriesType.KingRenko)
				{
					bool flag = dataSeriesType == DDDeepStackConfluence_DataSeriesType.ninZaRenko;
					text = string.Format("{0}{1}/{2}", flag ? "n" : "K", value1, value2);
				}
				else
				{
					bool flag2 = dataSeriesType == DDDeepStackConfluence_DataSeriesType.Tick || dataSeriesType == DDDeepStackConfluence_DataSeriesType.Volume || dataSeriesType == DDDeepStackConfluence_DataSeriesType.Range || dataSeriesType == DDDeepStackConfluence_DataSeriesType.Second || dataSeriesType == DDDeepStackConfluence_DataSeriesType.Minute;
					string text2 = dataSeriesType.ToString()[0].ToString();
					text = string.Format("{0}{1}", value, flag2 ? text2.ToLower() : text2);
				}
				text3 = text;
			}
			catch (Exception ex)
			{
				this.PrintException(ex);
				text3 = string.Empty;
			}
			return text3;
		}
		[MethodImpl(MethodImplOptions.NoInlining)]
		private void GetAllMethods()
		{
			try
			{
				this.dictMethodInfo.Clear();
				this.sortedListIndicatorItem.Clear();
				if (File.Exists(this.DocumentsPath + "NinjaTrader.Custom.dll"))
				{
					string text = typeof(Indicator).Namespace + ".Indicator";
					Assembly assembly = Assembly.LoadFrom(this.DocumentsPath + "NinjaTrader.Custom.dll");
					object obj = ((assembly == null) ? null : assembly.CreateInstance(text));
					if (obj != null)
					{
						Type type = obj.GetType();
						MethodInfo[] methods = type.GetMethods();
						if (methods != null && methods.Length != 0)
						{
							foreach (MethodInfo methodInfo in methods)
							{
								if (!(methodInfo == null) && !(methodInfo.ReturnType == null) && !this.listIndicatorExcluded.Contains(methodInfo.ReturnType.Name))
								{
									Type baseType = methodInfo.ReturnType.BaseType;
									if (baseType != null && baseType.Name == type.Name)
									{
										ParameterInfo[] parameters = methodInfo.GetParameters();
										if (parameters != null)
										{
											try
											{
												if (parameters.Length != 0 && parameters[0].ParameterType == typeof(ISeries<double>))
												{
													string name = methodInfo.Name;
													if (!this.dictMethodInfo.ContainsKey(name))
													{
														this.dictMethodInfo.Add(name, methodInfo);
													}
													DDDeepStackConfluence.IndicatorItem indicatorItem = new DDDeepStackConfluence.IndicatorItem(name, this.MainWindowTextColor);
													if (!this.sortedListIndicatorItem.ContainsKey(name))
													{
														this.sortedListIndicatorItem.Add(name, indicatorItem);
													}
												}
											}
											catch
											{
											}
										}
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
		[MethodImpl(MethodImplOptions.NoInlining)]
		private void LoadIndicator()
		{
			try
			{
				if (this.countIndicator == 0 || string.IsNullOrWhiteSpace(this.IndicatorsParamsJSON) || string.IsNullOrWhiteSpace(this.IndicatorsConfigJSON))
				{
					this.isNullIndicators = true;
				}
				else
				{
					string[] files = Directory.GetFiles(this.DocumentsPath, "*.dll", SearchOption.TopDirectoryOnly);
					for (int i = 0; i < this.countIndicator; i++)
					{
						DDDeepStackConfluence.IndicatorConfig indicatorConfig = this.effListIndicatorConfig[i];
						Type type = null;
						foreach (string text in files)
						{
							try
							{
								Assembly assembly = Assembly.LoadFrom(text);
								if (!(assembly == null))
								{
									type = Type.GetType(indicatorConfig.Namespace + indicatorConfig.Name + ", " + assembly.FullName);
									if (type != null)
									{
										break;
									}
								}
							}
							catch
							{
							}
						}
						if (type == null)
						{
							return;
						}
						List<int> listBarInProgressSelected = indicatorConfig.ListBarInProgressSelected;
						int count = listBarInProgressSelected.Count;
						List<DDDeepStackConfluence.ParamInfo> list = this.effListOfListParamInfo[i];
						PriceSeries[] priceSeries = this.GetPriceSeries(int.Parse(list[0].Value));
						for (int k = 0; k < count; k++)
						{
							IndicatorBase indicatorBase = (IndicatorBase)Activator.CreateInstance(type);
							if (indicatorBase == null)
							{
								return;
							}
							string text2 = indicatorBase.DisplayName;
							if (text2.Contains(" by DD.co") || text2.Contains(" by RenkoKings.com") || text2.Contains(" by HelloWin.io"))
							{
								text2 = text2.Substring(0, text2.IndexOf(" by"));
							}
							else if (text2.Contains("("))
							{
								text2 = text2.Substring(0, text2.IndexOf("("));
							}
							PropertyInfo[] properties = indicatorBase.GetType().GetProperties();
							int count2 = list.Count;
							for (int l = 0; l < count2; l++)
							{
								string name = list[l].Name;
								foreach (PropertyInfo propertyInfo in properties)
								{
									if (name == propertyInfo.Name)
									{
										Type propertyType = propertyInfo.PropertyType;
										string value = list[l].Value;
										if (propertyType.IsEnum)
										{
											Type underlyingType = Enum.GetUnderlyingType(propertyType);
											object obj = Convert.ChangeType(value, underlyingType);
											propertyInfo.SetValue(indicatorBase, obj);
										}
										else if (propertyType == typeof(string))
										{
											propertyInfo.SetValue(indicatorBase, value);
										}
										else if (propertyType == typeof(decimal))
										{
											propertyInfo.SetValue(indicatorBase, decimal.Parse(value));
										}
										else if (propertyType == typeof(double))
										{
											propertyInfo.SetValue(indicatorBase, double.Parse(value));
										}
										else if (propertyType == typeof(float))
										{
											propertyInfo.SetValue(indicatorBase, float.Parse(value));
										}
										else if (propertyType == typeof(bool))
										{
											propertyInfo.SetValue(indicatorBase, bool.Parse(value));
										}
										else if (propertyType == typeof(char))
										{
											propertyInfo.SetValue(indicatorBase, char.Parse(value));
										}
										else if (propertyType == typeof(ulong))
										{
											propertyInfo.SetValue(indicatorBase, ulong.Parse(value));
										}
										else if (propertyType == typeof(long))
										{
											propertyInfo.SetValue(indicatorBase, long.Parse(value));
										}
										else if (propertyType == typeof(uint))
										{
											propertyInfo.SetValue(indicatorBase, uint.Parse(value));
										}
										else if (propertyType == typeof(int))
										{
											propertyInfo.SetValue(indicatorBase, int.Parse(value));
										}
										else if (propertyType == typeof(ushort))
										{
											propertyInfo.SetValue(indicatorBase, ushort.Parse(value));
										}
										else if (propertyType == typeof(short))
										{
											propertyInfo.SetValue(indicatorBase, short.Parse(value));
										}
										else if (propertyType == typeof(sbyte))
										{
											propertyInfo.SetValue(indicatorBase, sbyte.Parse(value));
										}
										else if (propertyType == typeof(byte))
										{
											propertyInfo.SetValue(indicatorBase, byte.Parse(value));
										}
									}
								}
							}
							int num = listBarInProgressSelected[k];
							indicatorBase.Calculate = base.Calculate;
							this.indicatorBaseArr[i][k] = base.CacheIndicator<IndicatorBase>(indicatorBase, priceSeries[num], ref this.cacheIndicatorBaseArr[i][k]);
						}
					}
					this.CollectAllPlotsAndDataSeries();
					this.isNullIndicators = false;
				}
			}
			catch
			{
				this.ShowErrorMessageBox();
			}
		}
		[MethodImpl(MethodImplOptions.NoInlining)]
		private PriceSeries[] GetPriceSeries(int indexPrice)
		{
			Type underlyingType = Enum.GetUnderlyingType(typeof(PriceType));
			object obj = Convert.ChangeType(indexPrice, underlyingType);
			PriceType priceType = (PriceType)obj;
			PriceSeries[] array;
			if (priceType == PriceType.Close)
			{
				array = base.Closes;
			}
			else if (priceType == PriceType.High)
			{
				array = base.Highs;
			}
			else if (priceType == PriceType.Low)
			{
				array = base.Lows;
			}
			else if (priceType == PriceType.Median)
			{
				array = base.Medians;
			}
			else if (priceType == PriceType.Open)
			{
				array = base.Opens;
			}
			else if (priceType == PriceType.Typical)
			{
				array = base.Typicals;
			}
			else
			{
				array = base.Weighteds;
			}
			return array;
		}
		[MethodImpl(MethodImplOptions.NoInlining)]
		private void ShowErrorMessageBox()
		{
			try
			{
				if (this.isCharting)
				{
					base.ChartControl.Dispatcher.InvokeAsync(delegate
					{
						if (this.yesNoMessageWindow == null)
						{
							this.yesNoMessageWindow = new ShowYesNoMessageWindow(NinjaTrader.NinjaScript.Indicators.DimDim.DDDeepStackConfluence.childWindowBackground);
						}
						this.yesNoMessageWindow.ShowMessageBoxOKButtonOnly(this.mainWindowNT, "An error occurred while loading the indicator.\nPlease reload the chart (press F5) and try a different indicator.", this.MainWindowTextColor, "DeepStack Confluence by DD.co", "OK");
						this.yesNoMessageWindow.YesNoMessageWindow.Closing += this.OnYesNoMessageWindow_Closing;
					});
				}
				this.isNullIndicators = true;
			}
			catch (Exception ex)
			{
				this.PrintException(ex);
			}
		}
		[MethodImpl(MethodImplOptions.NoInlining)]
		private void OnYesNoMessageWindow_Closing(object sender, CancelEventArgs e)
		{
			try
			{
				if (this.yesNoMessageWindow.YesNoMessageWindow != null)
				{
					this.yesNoMessageWindow.YesNoMessageWindow.Closing -= this.OnYesNoMessageWindow_Closing;
					if (this.yesNoMessageWindow.YesNoMessageWindow.Owner != null)
					{
						this.yesNoMessageWindow.YesNoMessageWindow.Owner.Activate();
					}
					this.yesNoMessageWindow.YesNoMessageWindow = null;
				}
			}
			catch (Exception ex)
			{
				this.PrintException(ex);
			}
		}
		[MethodImpl(MethodImplOptions.NoInlining)]
		private void CollectAllPlotsAndDataSeries()
		{
			try
			{
				for (int i = 0; i < this.countIndicator; i++)
				{
					this.dictPlotOrDataSeriesArr[i] = new Dictionary<int, List<DDDeepStackConfluence.PlotOrDataSeriesInfo>>();
					List<int> listBarInProgressSelected = this.effListIndicatorConfig[i].ListBarInProgressSelected;
					int num = this.indicatorBaseArr[i].Length;
					for (int j = 0; j < num; j++)
					{
						List<DDDeepStackConfluence.PlotOrDataSeriesInfo> list = new List<DDDeepStackConfluence.PlotOrDataSeriesInfo>();
						IndicatorBase indicatorBase = this.indicatorBaseArr[i][j];
						Plot[] plots = indicatorBase.Plots;
						if (plots != null && plots.Length > 0)
						{
							for (int k = 0; k < plots.Length; k++)
							{
								DDDeepStackConfluence.PlotOrDataSeriesInfo plotOrDataSeriesInfo = new DDDeepStackConfluence.PlotOrDataSeriesInfo(plots[k].Name, indicatorBase.Values[k]);
								list.Add(plotOrDataSeriesInfo);
							}
						}
						PropertyInfo[] properties = indicatorBase.GetType().GetProperties();
						if (properties != null && properties.Length != 0)
						{
							PropertyInfo[] array = properties;
							int l = 0;
							while (l < array.Length)
							{
								PropertyInfo propertyInfo = array[l];
								Series<double> series = null;
								try
								{
									series = propertyInfo.GetValue(indicatorBase) as Series<double>;
								}
								catch
								{
									goto IL_0274;
								}
								goto IL_00E1;
								IL_0274:
								l++;
								continue;
								IL_00E1:
								bool flag = false;
								object[] customAttributes = propertyInfo.GetCustomAttributes(false);
								string text = propertyInfo.Name.Trim();
								if (text == "Value" || text == "Values" || series == null || !series.ToString().Contains("NinjaTrader.NinjaScript.Series"))
								{
									goto IL_0274;
								}
								flag = true;
								Dictionary<string, object> dictionary;
								if (customAttributes != null)
								{
									dictionary = customAttributes.ToDictionary((object a) => a.GetType().Name, (object a) => a);
								}
								else
								{
									dictionary = null;
								}
								Dictionary<string, object> dictionary2 = dictionary;
								if (dictionary2 == null && dictionary2.Count <= 0)
								{
									goto IL_0274;
								}
								bool flag2 = false;
								bool flag3 = false;
								foreach (string text2 in dictionary2.Keys.ToList<string>())
								{
									object obj = dictionary2[text2];
									if (obj.GetType() == typeof(BrowsableAttribute) && !((BrowsableAttribute)obj).Browsable)
									{
										flag2 = true;
									}
									if (obj.GetType() == typeof(XmlIgnoreAttribute))
									{
										flag3 = true;
									}
								}
								if (!flag || !flag2 || !flag3)
								{
									goto IL_0274;
								}
								bool flag4 = false;
								Series<double>[] values = indicatorBase.Values;
								for (int m = 0; m < values.Length; m++)
								{
									if (values[m] == series)
									{
										flag4 = true;
										break;
									}
								}
								if (!flag4)
								{
									list.Add(new DDDeepStackConfluence.PlotOrDataSeriesInfo(text, series));
									goto IL_0274;
								}
								goto IL_0274;
							}
							int num2 = listBarInProgressSelected[j];
							this.dictPlotOrDataSeriesArr[i].Add(num2, list);
						}
					}
				}
			}
			catch (Exception ex)
			{
				this.PrintException(ex);
			}
		}
		[MethodImpl(MethodImplOptions.NoInlining)]
		protected override void OnBarUpdate()
		{
			try
			{
					if (!this.isNullIndicators)
					{
						if (!this.DataSeriesBehindEnabled || base.BarsInProgress != this.countDataSeries)
						{
							if (this.isNeedCheckDataSeries)
							{
								bool flag = true;
								for (int i = 0; i < base.BarsArray.Length; i++)
								{
									if (base.CurrentBars[i] < 0)
									{
										return;
									}
								}
								if (flag)
								{
									this.isNeedCheckDataSeries = false;
								}
							}
							if (base.CurrentBars[0] == 0)
							{
								this.Signal_Trade[0] = 0.0;
								this.Signal_State[0] = 0.0;
							}
							else
							{
								for (int j = 2; j < this.countPlot; j++)
								{
									base.Values[j][0] = (double)(this.countPlot - j);
								}
								int signalOverlap = this.GetSignalOverlap(0, base.BarsInProgress);
								bool flag2 = signalOverlap != 0;
								if (!this.isPricePanel)
								{
									base.PlotBrushes[2][0] = ((signalOverlap != 0) ? ((signalOverlap > 0) ? this.PlotBullish : this.PlotBearish) : this.PlotNeutral);
								}
								this.signalStateArr[0] = signalOverlap;
								for (int k = 1; k < this.countIndicator; k++)
								{
									int signalOverlap2 = this.GetSignalOverlap(k, base.BarsInProgress);
									if (!this.isPricePanel)
									{
										base.PlotBrushes[k + 2][0] = ((signalOverlap2 != 0) ? ((signalOverlap2 > 0) ? this.PlotBullish : this.PlotBearish) : this.PlotNeutral);
									}
									this.signalStateArr[k] = signalOverlap2;
									if (flag2 && signalOverlap != signalOverlap2)
									{
										flag2 = false;
									}
								}
								if (this.RibbonEnabled && this.isPricePanel)
								{
									int[] array = new int[this.signalStateArr.Length];
									Array.Copy(this.signalStateArr, array, this.signalStateArr.Length);
									int num = base.CurrentBars[0];
									if (this.sortedListSignalInfo.ContainsKey(num))
									{
										this.sortedListSignalInfo[num] = array;
									}
									else
									{
										this.sortedListSignalInfo.Add(num, array);
									}
								}
								int num2 = (flag2 ? signalOverlap : 0);
								int num3 = 0;
								this.Signal_State[0] = (double)num2;
								int num4 = Convert.ToInt32(this.Signal_State[1]);
								if (flag2)
								{
									bool flag3 = num2 > 0;
									if (this.ConditionSyncSignalStart && ((num2 == 1 && num4 != 1) || (num2 == -1 && num4 != -1)))
									{
										num3 = num2;
										if (this.isCustomRenderingMethod)
										{
											this.AddMarker(base.CurrentBars[0], flag3, NinjaTrader.NinjaScript.Indicators.DimDim.DDDeepStackConfluence.MarkerType.Start);
										}
										else
										{
											this.PrintMarker(flag3, NinjaTrader.NinjaScript.Indicators.DimDim.DDDeepStackConfluence.MarkerType.Start);
										}
										this.markerIndexStart = base.CurrentBars[0];
									}
									if (this.ConditionSyncSignalContinuing && num2 == num4)
									{
										if (this.isCustomRenderingMethod)
										{
											this.AddMarker(base.CurrentBars[0], flag3, NinjaTrader.NinjaScript.Indicators.DimDim.DDDeepStackConfluence.MarkerType.Continuing);
										}
										else
										{
											this.PrintMarker(flag3, NinjaTrader.NinjaScript.Indicators.DimDim.DDDeepStackConfluence.MarkerType.Continuing);
										}
									}
									if (this.isCharting && this.BackgroundEnabled && this.BackgroundOpacity > 0)
									{
										global::System.Windows.Media.Brush brush = (flag3 ? this.backgroundBullish : this.backgroundBearish);
										if (!brush.IsTransparent())
										{
											base.BackBrushAll = brush;
										}
									}
									this.PaintBar(flag3);
								}
								else
								{
									if (this.MarkerEnabled && base.CurrentBars[0] == this.markerIndexStart)
									{
										if (this.isCustomRenderingMethod)
										{
											if (this.dictMarkers.ContainsKey(this.markerIndexStart))
											{
												this.dictMarkers.Remove(this.markerIndexStart);
											}
										}
										else
										{
											base.RemoveDrawObject(this.tag);
										}
									}
									if (this.BackgroundEnabled)
									{
										base.BackBrushAll = Brushes.Transparent;
									}
									if (this.BarEnabled)
									{
										double num5 = base.Closes[0][0];
										double num6 = base.Opens[0][0];
										if (num5.ApproxCompare(num6) > 0)
										{
											base.BarBrushes[0] = (base.CandleOutlineBrush = base.ChartBars.Properties.ChartStyle.UpBrush);
										}
										else if (num5.ApproxCompare(num6) < 0)
										{
											base.BarBrushes[0] = (base.CandleOutlineBrush = base.ChartBars.Properties.ChartStyle.DownBrush);
										}
									}
								}
								if (num4 != 0 && num2 != num4)
								{
									bool flag4 = num4 == 1;
									num3 = (flag4 ? 2 : (-2));
									if (this.ConditionSyncSignalEnd)
									{
										if (this.isCustomRenderingMethod)
										{
											this.AddMarker(base.CurrentBars[0], flag4, NinjaTrader.NinjaScript.Indicators.DimDim.DDDeepStackConfluence.MarkerType.End);
										}
										else
										{
											this.PrintMarker(flag4, NinjaTrader.NinjaScript.Indicators.DimDim.DDDeepStackConfluence.MarkerType.End);
										}
									}
								}
								this.Signal_Trade[0] = (double)num3;
							}
						}
					}
			}
			catch (Exception ex)
			{
				this.PrintException(ex);
			}
		}
		[MethodImpl(MethodImplOptions.NoInlining)]
		private int GetSignalOverlap(int index, int barsInProgress)
		{
			DDDeepStackConfluence.IndicatorConfig indicatorConfig = this.effListIndicatorConfig[index];
			Dictionary<int, double> dictionary = this.dictPlotValue0Arr[index];
			if (indicatorConfig.ListBarInProgressSelected.Contains(barsInProgress))
			{
				List<DDDeepStackConfluence.PlotOrDataSeriesInfo> list = this.dictPlotOrDataSeriesArr[index][barsInProgress];
				if (list.Count <= 0)
				{
					return 0;
				}
				double num = list[indicatorConfig.PlotIndex].PlotOrDataSeries[0];
				dictionary[barsInProgress] = num;
			}
			DDDeepStackConfluence_Operators operatorBullish = (DDDeepStackConfluence_Operators)indicatorConfig.OperatorBullish;
			DDDeepStackConfluence_Operators operatorBearish = (DDDeepStackConfluence_Operators)indicatorConfig.OperatorBearish;
			double valueBullish = indicatorConfig.ValueBullish;
			double valueBearish = indicatorConfig.ValueBearish;
			double value = dictionary.ElementAt(0).Value;
			int num2 = (this.IsMatchOperator(operatorBullish, value, valueBullish) ? 1 : (this.IsMatchOperator(operatorBearish, value, valueBearish) ? (-1) : 0));
			bool flag = num2 != 0;
			if (flag)
			{
				int count = dictionary.Count;
				for (int i = 1; i < count; i++)
				{
					double value2 = dictionary.ElementAt(i).Value;
					int num3 = (this.IsMatchOperator(operatorBullish, value2, valueBullish) ? 1 : (this.IsMatchOperator(operatorBearish, value2, valueBearish) ? (-1) : 0));
					if (num2 != num3)
					{
						flag = false;
						break;
					}
				}
			}
			if (!flag)
			{
				return 0;
			}
			return num2;
		}
		[MethodImpl(MethodImplOptions.NoInlining)]
		private bool IsMatchOperator(DDDeepStackConfluence_Operators op, double signal, double valueCompare)
		{
			int num = signal.ApproxCompare(valueCompare);
			if (op == DDDeepStackConfluence_Operators.Greater)
			{
				return num > 0;
			}
			if (op == DDDeepStackConfluence_Operators.Smaller)
			{
				return num < 0;
			}
			if (op == DDDeepStackConfluence_Operators.GreaterOrEqual)
			{
				return num >= 0;
			}
			if (op == DDDeepStackConfluence_Operators.SmallerOrEqual)
			{
				return num <= 0;
			}
			if (op == DDDeepStackConfluence_Operators.Equal)
			{
				return num == 0;
			}
			return op == DDDeepStackConfluence_Operators.Unequal && num != 0;
		}
		[MethodImpl(MethodImplOptions.NoInlining)]
		private void AddMarker(int barIndex, bool isBullish, DDDeepStackConfluence.MarkerType markerType)
		{
			if (!this.MarkerEnabled)
			{
				return;
			}
			DDDeepStackConfluence.MarkerInfo markerInfo = new DDDeepStackConfluence.MarkerInfo
			{
				BarIndex = barIndex,
				IsBullish = isBullish,
				MarkerType = markerType
			};
			if (this.dictMarkers.ContainsKey(barIndex))
			{
				this.dictMarkers[barIndex] = markerInfo;
				return;
			}
			this.dictMarkers.Add(barIndex, markerInfo);
		}
		[MethodImpl(MethodImplOptions.NoInlining)]
		public override void OnCalculateMinMax()
		{
			base.MaxValue = (double)(this.countIndicator + 1);
			base.MinValue = 0.0;
		}
		[MethodImpl(MethodImplOptions.NoInlining)]
		protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
		{
			try
			{
				if (this.isCharting)
				{
					if (!base.IsInHitTest)
					{
						if (!this.isNullIndicators)
						{
							base.OnRender(chartControl, chartScale);
							this.DrawMarkers(chartScale);
							if (this.RibbonEnabled && this.isPricePanel)
							{
								int fromIndex = base.ChartBars.FromIndex;
								int num = Math.Min(base.CurrentBars[0], base.ChartBars.ToIndex);
								for (int i = 1; i <= this.countIndicator; i++)
								{
									int num2 = (this.isRibbonPositionBottom ? (this.countIndicator - i) : (i - 1));
									for (int j = fromIndex; j <= num; j++)
									{
										if (this.sortedListSignalInfo.ContainsKey(j))
										{
											int num3 = this.sortedListSignalInfo[j][num2];
											if (this.RibbonOpacity > 0)
											{
												this.PrintRibbonCell(i, num3, j);
											}
										}
									}
									if (this.RibbonOpacity > 0)
									{
										this.PaintOneLabelRibbon(i, this.signalStateArr[num2], this.lblTextArr[num2]);
									}
								}
								if (this.needReloadChart)
								{
									int num4 = base.ChartPanel.X + base.ChartPanel.W - 5;
									this.yReload = (this.isRibbonPositionBottom ? this.yReload : (this.yReload + 5f));
									this.DrawText("You have to reload the chart (press F5) for the new setting to take effect.", new SimpleFont("Arial", 18), (float)num4, this.yReload, -1, -1, Brushes.HotPink, this.ScreenDPI, base.RenderTarget);
								}
							}
							else if (!this.isPricePanel)
							{
								for (int k = 1; k <= this.countIndicator; k++)
								{
									int num5 = this.countIndicator - k;
									this.PaintLabel((double)k, this.signalStateArr[num5], this.lblTextArr[num5], chartScale);
								}
								if (this.needReloadChart)
								{
									int num6 = base.ChartPanel.X + base.ChartPanel.W - 5;
									int num7 = base.ChartPanel.Y + base.ChartPanel.H - 5;
									this.DrawText("You have to reload the chart (press F5) for the new setting to take effect.", new SimpleFont("Arial", 18), (float)num6, (float)num7, -1, -1, Brushes.HotPink, this.ScreenDPI, base.RenderTarget);
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
		[MethodImpl(MethodImplOptions.NoInlining)]
		private void PaintOneLabelRibbon(int plotIndex, int signal, string labelText)
		{
			try
			{
				if (this.maxLabelWidth * this.RibbonHeight != 0)
				{
					if (!string.IsNullOrWhiteSpace(labelText))
					{
						global::System.Windows.Media.Brush brush = ((signal == 0) ? this.ribbonNeutral : ((signal > 0) ? this.ribbonBullish : this.ribbonBearish));
						if (!brush.IsTransparent())
						{
							int num = Math.Min(base.ChartControl.GetXByBarIndex(base.ChartBars, base.BarsArray[0].Count - 1), base.ChartPanel.X + base.ChartPanel.W - this.maxLabelWidth);
							int num2 = base.ChartPanel.Y + (this.isRibbonPositionBottom ? base.ChartPanel.H : 0);
							int num3;
							if (this.isRibbonPositionBottom)
							{
								num3 = num2 - this.RibbonMargin - (plotIndex - 1) * this.RibbonDistance - plotIndex * this.RibbonHeight;
							}
							else
							{
								num3 = num2 + this.RibbonMargin + (plotIndex - 1) * (this.RibbonDistance + this.RibbonHeight);
							}
							this.yReload = (float)num3;
							this.DrawText(labelText, this.Font, (float)num + (float)this.maxLabelWidth / 2f, (float)num3 + (float)this.RibbonHeight / 2f, 0, 0, brush, this.ScreenDPI, base.RenderTarget);
						}
					}
				}
			}
			catch (Exception ex)
			{
				this.PrintException(ex);
			}
		}
		[MethodImpl(MethodImplOptions.NoInlining)]
		private void PaintLabel(double plotIndex, int signal, string labelText, ChartScale chartScale)
		{
			try
			{
				if (this.maxLabelWidth * this.maxLabelWidth != 0)
				{
					if (!string.IsNullOrWhiteSpace(labelText))
					{
						global::System.Windows.Media.Brush brush = ((signal == 0) ? this.PlotNeutral : ((signal > 0) ? this.PlotBullish : this.PlotBearish));
						if (!brush.IsTransparent())
						{
							int num = Math.Min(base.ChartControl.GetXByBarIndex(base.ChartBars, base.BarsArray[0].Count - 1), base.ChartPanel.X + base.ChartPanel.W - this.maxLabelWidth);
							int num2 = Convert.ToInt32((float)chartScale.GetYByValue(plotIndex) - (float)this.maxLabelHeight / 2f);
							this.DrawText(labelText, this.Font, (float)num + (float)this.maxLabelWidth / 2f, (float)num2 + (float)this.maxLabelHeight / 2f, 0, 0, brush, this.ScreenDPI, base.RenderTarget);
						}
					}
				}
			}
			catch (Exception ex)
			{
				this.PrintException(ex);
			}
		}
		[MethodImpl(MethodImplOptions.NoInlining)]
		private void PrintRibbonCell(int plotIndex, int signal, int barIndex)
		{
			global::System.Windows.Media.Brush brush = ((signal == 0) ? this.ribbonNeutral : ((signal > 0) ? this.ribbonBullish : this.ribbonBearish));
			if (brush.IsTransparent())
			{
				return;
			}
			float xbyBarIndex = (float)base.ChartControl.GetXByBarIndex(base.ChartBars, barIndex);
			float barDistance = base.ChartControl.Properties.BarDistance;
			float num = xbyBarIndex - barDistance / 2f;
			int num2 = base.ChartPanel.Y + (this.isRibbonPositionBottom ? base.ChartPanel.H : 0);
			float num3;
			if (this.isRibbonPositionBottom)
			{
				num3 = (float)(num2 - this.RibbonMargin - (plotIndex - 1) * this.RibbonDistance - plotIndex * this.RibbonHeight);
			}
			else
			{
				num3 = (float)(num2 + this.RibbonMargin + (plotIndex - 1) * (this.RibbonDistance + this.RibbonHeight));
			}
			float num4 = barDistance + 0.5f;
			float num5 = (float)this.RibbonHeight;
			RectangleF rectangleF = new RectangleF(num, num3, num4, num5);
			global::SharpDX.Direct2D1.Brush brush2 = brush.ToDxBrush(base.RenderTarget);
			base.RenderTarget.FillRectangle(rectangleF, brush2);
			brush2.Dispose();
		}
		[MethodImpl(MethodImplOptions.NoInlining)]
		private void DrawMarkers(ChartScale chartScale)
		{
			try
			{
				if (this.MarkerEnabled)
				{
					if (this.isCustomRenderingMethod)
					{
						if (this.dictMarkers != null && this.dictMarkers.Count > 0)
						{
							for (int i = base.ChartBars.FromIndex; i <= Math.Min(base.CurrentBars[0], base.ChartBars.ToIndex); i++)
							{
								if (this.dictMarkers.ContainsKey(i))
								{
									this.DrawOneMarker(chartScale, this.dictMarkers[i]);
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
		
		private void PrintException(Exception exception)
		{
			string text = "DDDeepStackConf: " + exception.ToString() + " (" + exception.StackTrace + ")";
			Print((object)text);
			Log(text, NinjaTrader.Cbi.LogLevel.Error);
		}

		private DDDeepStackConfluence DD { get { return this; } }

		private SharpDX.Size2F ComputeTextSize(string text, SimpleFont font, int dpi)
		{
			if (font == null) return new SharpDX.Size2F(0f, 12f);
			if (string.IsNullOrEmpty(text)) return new SharpDX.Size2F(0f, 0f);
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

		private int ComputeTextHeight(string text, SimpleFont font)
		{
			return (int)Math.Ceiling(ComputeTextSize(text, font, this.ScreenDPI).Height);
		}

		private string FormatMarkerString(string text)
		{
			if (string.IsNullOrWhiteSpace(text)) return string.Empty;
			string[] parts = text.Trim().Split(new[] { '+' }, StringSplitOptions.RemoveEmptyEntries);
			for (int i = 0; i < parts.Length; i++) parts[i] = parts[i].Trim();
			return string.Join("\n", parts).Trim();
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

		public override string FormatPriceMarker(double price)
		{
			return Instrument.MasterInstrument.FormatPrice(Instrument.MasterInstrument.RoundToTickSize(price), true);
		}
		
		
		[MethodImpl(MethodImplOptions.NoInlining)]
		private void DrawOneMarker(ChartScale chartScale, DDDeepStackConfluence.MarkerInfo markerInfo)
		{
			try
			{
				bool isBullish = markerInfo.IsBullish;
				DDDeepStackConfluence.MarkerType markerType = markerInfo.MarkerType;
				int barIndex = markerInfo.BarIndex;
				if (isBullish || base.Highs[0].GetValueAt(barIndex).ApproxCompare(chartScale.MaxValue) < 0)
				{
					if (!isBullish || base.Lows[0].GetValueAt(barIndex).ApproxCompare(chartScale.MinValue) > 0)
					{
						global::System.Windows.Media.Brush brush = (isBullish ? this.MarkerBrushBullish : this.MarkerBrushBearish);
						if (!brush.IsTransparent())
						{
							string text;
							if (markerType == NinjaTrader.NinjaScript.Indicators.DimDim.DDDeepStackConfluence.MarkerType.Start)
							{
								text = (isBullish ? this.MarkerStringSyncSignalBullishStart : this.MarkerStringSyncSignalBearishStart);
							}
							else if (markerType == NinjaTrader.NinjaScript.Indicators.DimDim.DDDeepStackConfluence.MarkerType.Continuing)
							{
								text = (isBullish ? this.MarkerStringSyncSignalBullishContinuing : this.MarkerStringSyncSignalBearishContinuing);
							}
							else
							{
								text = (isBullish ? this.MarkerStringSyncSignalBullishEnd : this.MarkerStringSyncSignalBearishEnd);
							}
							if (!string.IsNullOrWhiteSpace(text))
							{
								text = this.FormatMarkerString(text);
								float num = (float)base.ChartControl.GetXByBarIndex(base.ChartBars, barIndex);
								bool flag = markerType == NinjaTrader.NinjaScript.Indicators.DimDim.DDDeepStackConfluence.MarkerType.End;
								int num2 = (flag ? (isBullish ? (-1) : 1) : (isBullish ? 1 : (-1)));
								double valueAt = (flag ? (isBullish ? base.Highs[0] : base.Lows[0]) : (isBullish ? base.Lows[0] : base.Highs[0])).GetValueAt(barIndex);
								float num3 = (float)(chartScale.GetYByValue(valueAt) + num2 * this.MarkerOffset);
								this.DrawText(text, this.MarkerFont, num, num3, 0, num2, brush, this.ScreenDPI, base.RenderTarget);
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
		[MethodImpl(MethodImplOptions.NoInlining)]
		private void OnBtnTitle_Click(object sender, RoutedEventArgs e)
		{
			base.TriggerCustomEvent(delegate(object state)
			{
				this.BuildMainWindowNT();
			}, e);
		}
		[MethodImpl(MethodImplOptions.NoInlining)]
		private void OnCpDragDelta(object sender, DragDeltaEventArgs e)
		{
			Action cachedAction = null;
			base.TriggerCustomEvent(delegate(object state)
			{
				try
				{
					if (this.isCharting)
					{
						Dispatcher dispatcher = this.ChartControl.Dispatcher;
						Action action;
						if ((action = cachedAction) == null)
						{
							action = (cachedAction = delegate
							{
								if (this.dragablePanel.alignment == NinjaTrader.NinjaScript.DrawingTools.TextPosition.Center)
								{
									this.dragablePanel.Margin = new Thickness(5.0);
									return;
								}
								double num4;
								double num3;
								double num2;
								double num = (num2 = (num3 = (num4 = 5.0)));
								if (this.dragablePanel.HorizontalAlignment == HorizontalAlignment.Left)
								{
									num2 = this.dragablePanel.Margin.Left + e.HorizontalChange;
								}
								else
								{
									num3 = this.dragablePanel.Margin.Right - e.HorizontalChange;
								}
								if (this.dragablePanel.VerticalAlignment == VerticalAlignment.Top)
								{
									num = this.dragablePanel.Margin.Top + e.VerticalChange;
								}
								else
								{
									num4 = this.dragablePanel.Margin.Bottom - e.VerticalChange;
								}
								num2 = Math.Max(0.0, num2);
								num = Math.Max(0.0, num);
								num3 = Math.Max(0.0, num3);
								num4 = Math.Max(0.0, num4);
								this.dragablePanel.Margin = new Thickness(num2, num, num3, num4);
								this.CpPositionMarginLeft = this.dragablePanel.Margin.Left;
								this.CpPositionMarginTop = this.dragablePanel.Margin.Top;
								this.CpPositionMarginRight = this.dragablePanel.Margin.Right;
								this.CpPositionMarginBottom = this.dragablePanel.Margin.Bottom;
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
		[MethodImpl(MethodImplOptions.NoInlining)]
		private void OnBtnDragDoubleClick(object sender, MouseButtonEventArgs e)
		{
			base.TriggerCustomEvent(delegate(object state)
			{
				try
				{
					if (this.isCharting)
					{
						base.ChartControl.Dispatcher.InvokeAsync(delegate
						{
							this.CpMinimized = !this.CpMinimized;
							this.dragablePanel.Minimized(this.CpMinimized);
							this.dragablePanel.drag.ToolTip = "  Drag to anywhere; double click to {0}." + (this.CpMinimized ? "restore" : "minimize");
						});
					}
				}
				catch (Exception ex)
				{
					this.PrintException(ex);
				}
			}, e);
		}
		[MethodImpl(MethodImplOptions.NoInlining)]
		private void OnBtnMiniClick(object sender, RoutedEventArgs e)
		{
			base.TriggerCustomEvent(delegate(object state)
			{
				try
				{
					if (this.isCharting)
					{
						base.ChartControl.Dispatcher.InvokeAsync(delegate
						{
							this.CpMinimized = false;
							this.dragablePanel.Minimized(this.CpMinimized);
							this.dragablePanel.drag.ToolTip = "  Drag to anywhere; double click to {0}." + (this.CpMinimized ? "restore" : "minimize");
						});
					}
				}
				catch (Exception ex)
				{
					this.PrintException(ex);
				}
			}, e);
		}
		[MethodImpl(MethodImplOptions.NoInlining)]
		private void OnNUDTextBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			Action cachedAction = null;
			base.TriggerCustomEvent(delegate(object state)
			{
				try
				{
					if (this.isCharting)
					{
						Dispatcher dispatcher = this.ChartControl.Dispatcher;
						Action action;
						if ((action = cachedAction) == null)
						{
							action = (cachedAction = delegate
							{
								DDNumericUpDown DDNumericUpDown = DDResources_GlobalConstantAndFunction.FindVisualParent<DDNumericUpDown>(sender as TextBox, null);
								if (DDNumericUpDown == null)
								{
									return;
								}
								int num = (int)DDNumericUpDown.Tag;
								int num2 = (int)DDNumericUpDown.Value;
								if (num == 0 || num == 1)
								{
									this.ChangeDataSeriesValue(num, 2, this.DataSeries2Type, num2, ref this._DataSeries2Value, ref this._DataSeries2Value1, ref this._DataSeries2Value2);
								}
								else if (num == 2 || num == 3)
								{
									this.ChangeDataSeriesValue(num, 3, this.DataSeries3Type, num2, ref this._DataSeries3Value, ref this._DataSeries3Value1, ref this._DataSeries3Value2);
								}
								else if (num == 4 || num == 5)
								{
									this.ChangeDataSeriesValue(num, 4, this.DataSeries4Type, num2, ref this._DataSeries4Value, ref this._DataSeries4Value1, ref this._DataSeries4Value2);
								}
								else
								{
									this.ChangeDataSeriesValue(num, 5, this.DataSeries5Type, num2, ref this._DataSeries5Value, ref this._DataSeries5Value1, ref this._DataSeries5Value2);
								}
								List<string> oldTimeframeList = new List<string>(this.listTimeframeStr);
								this.listTimeframeStr.RemoveRange(1, this.listTimeframeStr.Count - 1);
								this.FilterTimeframe(this.DataSeries2Type, this.DataSeries2Value, this.DataSeries2Value1, this.DataSeries2Value2);
								this.FilterTimeframe(this.DataSeries3Type, this.DataSeries3Value, this.DataSeries3Value1, this.DataSeries3Value2);
								this.FilterTimeframe(this.DataSeries4Type, this.DataSeries4Value, this.DataSeries4Value1, this.DataSeries4Value2);
								this.FilterTimeframe(this.DataSeries5Type, this.DataSeries5Value, this.DataSeries5Value1, this.DataSeries5Value2);
								this.RemapIndicatorTimeframes(oldTimeframeList);
								this.needReloadChart = true;
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
		[MethodImpl(MethodImplOptions.NoInlining)]
		private void ChangeDataSeriesValue(int tag, int index, DDDeepStackConfluence_DataSeriesType dataSeriesType, int dataSeriesAssignValue, ref int dataSeriesValue, ref int dataSeriesValue1, ref int dataSeriesValue2)
		{
			if (tag % 2 == 0)
			{
				if (dataSeriesType == DDDeepStackConfluence_DataSeriesType.ninZaRenko || dataSeriesType == DDDeepStackConfluence_DataSeriesType.KingRenko)
				{
					dataSeriesValue1 = dataSeriesAssignValue;
					return;
				}
				if (dataSeriesType != DDDeepStackConfluence_DataSeriesType.Disabled)
				{
					dataSeriesValue = dataSeriesAssignValue;
					return;
				}
			}
			else
			{
				dataSeriesValue2 = dataSeriesAssignValue;
			}
		}

		private void RemapIndicatorTimeframes(List<string> oldList)
		{
			if (this.effListIndicatorConfig == null || this.effListIndicatorConfig.Count == 0) return;
			if (oldList == null) return;
			var newSet = new HashSet<string>(this.listTimeframeStr);
			var oldSet = new HashSet<string>(oldList);
			var gone = oldList.Where(s => !string.IsNullOrEmpty(s) && !newSet.Contains(s)).Distinct().ToList();
			var added = this.listTimeframeStr.Where(s => !string.IsNullOrEmpty(s) && !oldSet.Contains(s)).Distinct().ToList();
			if (gone.Count != 1 || added.Count != 1) return;
			string oldStr = gone[0];
			string newStr = added[0];
			bool changed = false;
			foreach (var cfg in this.effListIndicatorConfig)
			{
				if (cfg == null || string.IsNullOrWhiteSpace(cfg.TimeframeConfigStr)) continue;
				string[] tokens = cfg.TimeframeConfigStr.Split(new[] { '&' }, StringSplitOptions.RemoveEmptyEntries);
				bool localChanged = false;
				for (int i = 0; i < tokens.Length; i++)
				{
					if (tokens[i].Trim() == oldStr)
					{
						tokens[i] = newStr;
						localChanged = true;
					}
				}
				if (localChanged)
				{
					cfg.TimeframeConfigStr = string.Join("&", tokens);
					changed = true;
				}
			}
			if (changed)
			{
				try { this.IndicatorsConfigJSON = new JavaScriptSerializer().Serialize(this.effListIndicatorConfig); }
				catch (Exception ex) { this.PrintException(ex); }
			}
		}
		[MethodImpl(MethodImplOptions.NoInlining)]
		private void OnCmbTimeframe_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			Action cachedAction = null;
			base.TriggerCustomEvent(delegate(object state)
			{
				try
				{
					if (this.isCharting)
					{
						Dispatcher dispatcher = this.ChartControl.Dispatcher;
						Action action;
						if ((action = cachedAction) == null)
						{
							action = (cachedAction = delegate
							{
								ComboBox comboBox = sender as ComboBox;
								if (comboBox == null)
								{
									return;
								}
								int num = (int)comboBox.Tag;
								DDDeepStackConfluence_DataSeriesType DDDeepStackConfluence_DataSeriesType = (DDDeepStackConfluence_DataSeriesType)comboBox.SelectedValue;
								bool flag = DDDeepStackConfluence_DataSeriesType == DDDeepStackConfluence_DataSeriesType.Disabled;
								DDNumericUpDown DDNumericUpDown = this.dragablePanel.nudValueArr[num * 2];
								DDNumericUpDown DDNumericUpDown2 = this.dragablePanel.nudValueArr[num * 2 + 1];
								int dataSeries2Value;
								int dataSeries2Value2;
								int dataSeries2Value3;
								if (num == 0)
								{
									if (flag)
									{
										comboBox.SelectedValue = this.DataSeries2Type;
										return;
									}
									this.DataSeries2Type = DDDeepStackConfluence_DataSeriesType;
									dataSeries2Value = this.DataSeries2Value1;
									dataSeries2Value2 = this.DataSeries2Value2;
									dataSeries2Value3 = this.DataSeries2Value;
								}
								else if (num == 1)
								{
									this.DataSeries3Type = DDDeepStackConfluence_DataSeriesType;
									dataSeries2Value = this.DataSeries3Value1;
									dataSeries2Value2 = this.DataSeries3Value2;
									dataSeries2Value3 = this.DataSeries3Value;
								}
								else if (num == 2)
								{
									this.DataSeries4Type = DDDeepStackConfluence_DataSeriesType;
									dataSeries2Value = this.DataSeries4Value1;
									dataSeries2Value2 = this.DataSeries4Value2;
									dataSeries2Value3 = this.DataSeries4Value;
								}
								else
								{
									this.DataSeries5Type = DDDeepStackConfluence_DataSeriesType;
									dataSeries2Value = this.DataSeries5Value1;
									dataSeries2Value2 = this.DataSeries5Value2;
									dataSeries2Value3 = this.DataSeries5Value;
								}
								if (flag)
								{
									DDNumericUpDown.IsEnabled = (DDNumericUpDown2.IsEnabled = false);
								}
								else
								{
									DDNumericUpDown.IsEnabled = (DDNumericUpDown2.IsEnabled = true);
									if (DDDeepStackConfluence_DataSeriesType == DDDeepStackConfluence_DataSeriesType.ninZaRenko || DDDeepStackConfluence_DataSeriesType == DDDeepStackConfluence_DataSeriesType.KingRenko)
									{
										DDNumericUpDown.Value = (double)dataSeries2Value;
										DDNumericUpDown2.Value = (double)dataSeries2Value2;
										DDNumericUpDown2.Visibility = Visibility.Visible;
										DDNumericUpDown.SetValue(Grid.ColumnSpanProperty, 1);
									}
									else
									{
										DDNumericUpDown.Value = (double)dataSeries2Value3;
										DDNumericUpDown.SetValue(Grid.ColumnSpanProperty, 2);
										DDNumericUpDown2.Visibility = Visibility.Hidden;
									}
								}
								List<string> oldTimeframeList = new List<string>(this.listTimeframeStr);
								this.listTimeframeStr.RemoveRange(1, this.listTimeframeStr.Count - 1);
								this.FilterTimeframe(this.DataSeries2Type, this.DataSeries2Value, this.DataSeries2Value1, this.DataSeries2Value2);
								this.FilterTimeframe(this.DataSeries3Type, this.DataSeries3Value, this.DataSeries3Value1, this.DataSeries3Value2);
								this.FilterTimeframe(this.DataSeries4Type, this.DataSeries4Value, this.DataSeries4Value1, this.DataSeries4Value2);
								this.FilterTimeframe(this.DataSeries5Type, this.DataSeries5Value, this.DataSeries5Value1, this.DataSeries5Value2);
								this.RemapIndicatorTimeframes(oldTimeframeList);
								this.needReloadChart = true;
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
		[MethodImpl(MethodImplOptions.NoInlining)]
		private void OnMainWindowChanged(object sender, EventArgs e)
		{
			base.TriggerCustomEvent(delegate(object state)
			{
				try
				{
					if (this.isCharting)
					{
						base.ChartControl.Dispatcher.InvokeAsync(delegate
						{
							this.MainWindowLeft = this.mainWindowNT.Left;
							this.MainWindowTop = this.mainWindowNT.Top;
							this.MainWindowWidth = this.mainWindowNT.Width;
							this.MainWindowHeight = this.mainWindowNT.Height;
						});
					}
				}
				catch (Exception ex)
				{
					this.PrintException(ex);
				}
			}, e);
		}
		[MethodImpl(MethodImplOptions.NoInlining)]
		private void OnMainWindowNT_Closing(object sender, CancelEventArgs e)
		{
			base.TriggerCustomEvent(delegate(object state)
			{
				if (this.isCharting)
				{
					base.ChartControl.Dispatcher.InvokeAsync(delegate
					{
						this.btnCancel.Click -= this.OnBtnCancle_Click;
						this.btnOK.Click -= this.OnBtnOK_Click;
						this.indicatorsListPage.DDSearchBarControl.TextChanged -= this.OnTbSearch_TextChanged;
						this.indicatorsListPage.listBoxIndicatorsResult.SelectionChanged -= this.OnListIndicatorsResult_SelectionChanged;
						this.indicatorsListPage.listBoxIndicatorsResult.MouseDoubleClick -= this.OnListIndicatorsResult_MouseDoubleClick;
						this.indicatorsListPage.listBoxIndicatorsMain.SelectionChanged -= this.OnListIndicatorsMain_SelectionChanged;
						this.indicatorsListPage.listBoxIndicatorsMain.MouseDoubleClick -= this.OnListIndicatorsMain_MouseDoubleClick;
						this.indicatorsListPage.listBoxIndicatorsConfig.SelectionChanged -= this.OnListIndicatorsConfigured_SelectionChanged;
						this.indicatorsListPage.lblAdd.MouseLeftButtonUp -= this.OnLblAdd_MouseLeftButtonUp;
						this.indicatorsListPage.lblRemove.MouseLeftButtonUp -= this.OnLblRemove_MouseLeftButtonUp;
						this.indicatorsListPage.lblUp.MouseLeftButtonUp -= this.OnLblUp_MouseLeftButtonUp;
						this.indicatorsListPage.lblDown.MouseLeftButtonUp -= this.OnLblDown_MouseLeftButtonUp;
						((INotifyCollectionChanged)this.indicatorsListPage.listBoxIndicatorsConfig.Items).CollectionChanged -= this.OnListIndicatorsConfig_CollectionChanged;
						this.indicatorsListPage = null;
						this.indicatorPropsPage = null;
						this.gridContent = null;
						this.btnOK = null;
						this.btnCancel = null;
						if (this.mainWindowNT != null)
						{
							this.mainWindowNT.Closing -= this.OnMainWindowNT_Closing;
							this.mainWindowNT = null;
						}
					});
				}
			}, e);
		}
		[MethodImpl(MethodImplOptions.NoInlining)]
		private void OnBtnCancle_Click(object sender, RoutedEventArgs e)
		{
			base.TriggerCustomEvent(delegate(object state)
			{
				this.TerminateMainWindowNT();
			}, e);
		}
		[MethodImpl(MethodImplOptions.NoInlining)]
		private void OnBtnOK_Click(object sender, RoutedEventArgs e)
		{
			base.TriggerCustomEvent(delegate(object state)
			{
				try
				{
					if (this.isCharting)
					{
						base.ChartControl.Dispatcher.InvokeAsync(delegate
						{
							if (this.yesNoMessageWindow.YesNoMessageWindow != null)
							{
								this.yesNoMessageWindow.YesNoMessageWindow.Close();
								this.yesNoMessageWindow.YesNoMessageWindow = null;
							}
							List<DDDeepStackConfluence.IndicatorConfig> listIndicatorConfig = this.indicatorPropsPage.ListIndicatorConfig;
							List<List<DDDeepStackConfluence.ParamInfo>> listOfListParamInfo = this.indicatorPropsPage.ListOfListParamInfo;
							int count = listIndicatorConfig.Count;
							int count2 = listOfListParamInfo.Count;
							bool flag = count != this.countIndicator || count2 != this.countIndicator;
							bool flag2 = false;
							if (this.indicatorPropsPage.IsParamChanged || flag || this.isIndicatorsConfigChanged)
							{
								JavaScriptSerializer javaScriptSerializer = new JavaScriptSerializer();
								string text = string.Empty;
								string text2 = string.Empty;
								if (count > 0 && count2 > 0)
								{
									List<string> list = new List<string>();
									for (int i = 0; i < count; i++)
									{
										string text3 = javaScriptSerializer.Serialize(listOfListParamInfo[i]);
										list.Add(text3);
									}
									text = javaScriptSerializer.Serialize(listIndicatorConfig);
									text2 = javaScriptSerializer.Serialize(list);
								}
								this.IndicatorsConfigJSON = text;
								this.IndicatorsParamsJSON = text2;
								flag2 = true;
							}
							if (flag2)
							{
								this.ShowRefreshChartMessage();
							}
							this.TerminateMainWindowNT();
						});
					}
				}
				catch (Exception ex)
				{
					this.PrintException(ex);
				}
			}, e);
		}
		[MethodImpl(MethodImplOptions.NoInlining)]
		private void OnListIndicatorsMain_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			Action cachedAction = null;
			base.TriggerCustomEvent(delegate(object state)
			{
				try
				{
					if (this.isCharting)
					{
						Dispatcher dispatcher = this.ChartControl.Dispatcher;
						Action action;
						if ((action = cachedAction) == null)
						{
							action = (cachedAction = delegate
							{
								ListBox listBox = sender as ListBox;
								if (listBox == null || listBox.SelectedItems == null || listBox.SelectedItems.Count <= 0)
								{
									return;
								}
								if (listBox.SelectedIndex < 0)
								{
									return;
								}
								Label lblRemove = this.indicatorsListPage.lblRemove;
								Label lblAdd = this.indicatorsListPage.lblAdd;
								lblAdd.Foreground = this.MainWindowTextColor;
								lblAdd.IsEnabled = true;
								lblAdd.Cursor = Cursors.Hand;
								lblRemove.Foreground = Brushes.Gray;
								lblRemove.IsEnabled = false;
								lblRemove.Cursor = Cursors.No;
								DDDeepStackConfluence.IndicatorItem indicatorItem = listBox.SelectedItem as DDDeepStackConfluence.IndicatorItem;
								if (!this.sortedListIndicatorItem.ContainsKey(indicatorItem.DisplayName))
								{
									return;
								}
								this.indicatorPropsPage.RenderIndicatorPropsPage(indicatorItem, false, false, -1);
								this.indicatorsListPage.listBoxIndicatorsConfig.SelectedIndex = -1;
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
		[MethodImpl(MethodImplOptions.NoInlining)]
		private void OnListIndicatorsResult_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			Action cachedAction = null;
			base.TriggerCustomEvent(delegate(object state)
			{
				try
				{
					if (this.isCharting)
					{
						Dispatcher dispatcher = this.ChartControl.Dispatcher;
						Action action;
						if ((action = cachedAction) == null)
						{
							action = (cachedAction = delegate
							{
								ListBox listBox = sender as ListBox;
								if (listBox == null || listBox.SelectedItems == null || listBox.SelectedItems.Count <= 0)
								{
									return;
								}
								DDDeepStackConfluence.IndicatorItem indicatorItem = listBox.SelectedItem as DDDeepStackConfluence.IndicatorItem;
								foreach (object obj in ((IEnumerable)this.indicatorsListPage.listBoxIndicatorsMain.Items))
								{
									DDDeepStackConfluence.IndicatorItem indicatorItem2 = (DDDeepStackConfluence.IndicatorItem)obj;
									if (indicatorItem2.DisplayName == indicatorItem.DisplayName)
									{
										this.indicatorsListPage.listBoxIndicatorsMain.SelectedItem = indicatorItem2;
										break;
									}
								}
							});
						}
						dispatcher.InvokeAsync(action);
					}
				}
				catch (Exception ex)
				{
					this.DD.PrintException(ex);
				}
			}, e);
		}
		[MethodImpl(MethodImplOptions.NoInlining)]
		private void OnListIndicatorsMain_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			Action cachedAction = null;
			base.TriggerCustomEvent(delegate(object state)
			{
				try
				{
					if (this.isCharting)
					{
						Dispatcher dispatcher = this.ChartControl.Dispatcher;
						Action action;
						if ((action = cachedAction) == null)
						{
							action = (cachedAction = delegate
							{
								ListBox listBox = sender as ListBox;
								if (listBox == null || listBox.SelectedItems == null || listBox.SelectedItems.Count <= 0)
								{
									return;
								}
								DDDeepStackConfluence.IndicatorItem indicatorItem = listBox.SelectedItem as DDDeepStackConfluence.IndicatorItem;
								if (this.sortedListIndicatorItem.ContainsKey(indicatorItem.DisplayName))
								{
									DDDeepStackConfluence.IndicatorItem indicatorItem2 = new DDDeepStackConfluence.IndicatorItem(indicatorItem.DisplayName, this.MainWindowTextColor);
									this.indicatorsListPage.listBoxIndicatorsConfig.Items.Add(indicatorItem2);
									listBox.SelectedIndex = -1;
									this.isAddedToListBoxConfig = true;
									this.indicatorsListPage.listBoxIndicatorsConfig.SelectedIndex = this.indicatorsListPage.listBoxIndicatorsConfig.Items.Count - 1;
								}
							});
						}
						dispatcher.InvokeAsync(action);
					}
				}
				catch (Exception ex)
				{
					this.DD.PrintException(ex);
				}
			}, e);
		}
		private void OnListIndicatorsResult_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			this.OnListIndicatorsMain_MouseDoubleClick(sender, e);
		}
		[MethodImpl(MethodImplOptions.NoInlining)]
		private void OnListIndicatorsConfigured_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			Action cachedAction = null;
			base.TriggerCustomEvent(delegate(object state)
			{
				try
				{
					if (this.isCharting)
					{
						Dispatcher dispatcher = this.ChartControl.Dispatcher;
						Action action;
						if ((action = cachedAction) == null)
						{
							action = (cachedAction = delegate
							{
								ListBox listBox = sender as ListBox;
								if (listBox == null || listBox.SelectedItems == null || listBox.Items.Count <= 0)
								{
									return;
								}
								int selectedIndex = listBox.SelectedIndex;
								if (selectedIndex < 0)
								{
									return;
								}
								Label lblAdd = this.indicatorsListPage.lblAdd;
								lblAdd.Foreground = Brushes.Gray;
								lblAdd.IsEnabled = false;
								lblAdd.Cursor = Cursors.No;
								Label lblRemove = this.indicatorsListPage.lblRemove;
								lblRemove.Foreground = this.MainWindowTextColor;
								lblRemove.IsEnabled = true;
								lblRemove.Cursor = Cursors.Hand;
								DDDeepStackConfluence.IndicatorItem indicatorItem = listBox.SelectedItem as DDDeepStackConfluence.IndicatorItem;
								if (this.sortedListIndicatorItem.ContainsKey(indicatorItem.IndicatorName))
								{
									this.indicatorPropsPage.RenderIndicatorPropsPage(indicatorItem, true, this.isAddedToListBoxConfig, selectedIndex);
									this.isAddedToListBoxConfig = false;
									this.indicatorsListPage.listBoxIndicatorsMain.SelectedIndex = (this.indicatorsListPage.listBoxIndicatorsResult.SelectedIndex = -1);
								}
							});
						}
						dispatcher.InvokeAsync(action);
					}
				}
				catch (Exception ex)
				{
					this.DD.PrintException(ex);
				}
			}, e);
		}
		[MethodImpl(MethodImplOptions.NoInlining)]
		private void OnTbSearch_TextChanged(object sender, TextChangedEventArgs e)
		{
			base.TriggerCustomEvent(delegate(object state)
			{
				try
				{
					if (this.isCharting)
					{
						base.ChartControl.Dispatcher.InvokeAsync(delegate
						{
							string findContent = this.indicatorsListPage.DDSearchBarControl.FindContent;
							if (findContent == "Start typing to search")
							{
								return;
							}
							this.indicatorsListPage.FindIndicatorByName(findContent);
							this.indicatorsListPage.listBoxIndicatorsMain.ScrollIntoView(this.indicatorsListPage.listBoxIndicatorsMain.SelectedItem);
						});
					}
				}
				catch (Exception ex)
				{
					this.DD.PrintException(ex);
				}
			}, e);
		}
		[MethodImpl(MethodImplOptions.NoInlining)]
		private void OnLblAdd_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			base.TriggerCustomEvent(delegate(object state)
			{
				try
				{
					if (this.isCharting)
					{
						base.ChartControl.Dispatcher.InvokeAsync(delegate
						{
							ListBox listBoxIndicatorsMain = this.indicatorsListPage.listBoxIndicatorsMain;
							if (listBoxIndicatorsMain == null || listBoxIndicatorsMain.SelectedItems == null || listBoxIndicatorsMain.SelectedItems.Count <= 0 || listBoxIndicatorsMain.SelectedIndex < 0)
							{
								return;
							}
							DDDeepStackConfluence.IndicatorItem indicatorItem = listBoxIndicatorsMain.SelectedItem as DDDeepStackConfluence.IndicatorItem;
							if (this.sortedListIndicatorItem.ContainsKey(indicatorItem.DisplayName))
							{
								DDDeepStackConfluence.IndicatorItem indicatorItem2 = new DDDeepStackConfluence.IndicatorItem(indicatorItem.DisplayName, this.MainWindowTextColor);
								this.indicatorsListPage.listBoxIndicatorsConfig.Items.Add(indicatorItem2);
								listBoxIndicatorsMain.SelectedIndex = -1;
								this.isAddedToListBoxConfig = true;
								this.indicatorsListPage.listBoxIndicatorsConfig.SelectedIndex = this.indicatorsListPage.listBoxIndicatorsConfig.Items.Count - 1;
								this.isIndicatorsConfigChanged = true;
							}
						});
					}
				}
				catch (Exception ex)
				{
					this.DD.PrintException(ex);
				}
			}, e);
		}
		[MethodImpl(MethodImplOptions.NoInlining)]
		private void OnLblRemove_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			Action cachedAction = null;
			base.TriggerCustomEvent(delegate(object state)
			{
				try
				{
					if (this.isCharting)
					{
						Dispatcher dispatcher = this.ChartControl.Dispatcher;
						Action action;
						if ((action = cachedAction) == null)
						{
							action = (cachedAction = delegate
							{
								ListBox listBoxIndicatorsConfig = this.indicatorsListPage.listBoxIndicatorsConfig;
								if (listBoxIndicatorsConfig == null || listBoxIndicatorsConfig.SelectedItems == null || listBoxIndicatorsConfig.SelectedItems.Count <= 0 || listBoxIndicatorsConfig.SelectedIndex < 0)
								{
									return;
								}
								int selectedIndex = listBoxIndicatorsConfig.SelectedIndex;
								listBoxIndicatorsConfig.Items.RemoveAt(selectedIndex);
								this.indicatorPropsPage.ListIndicatorConfig.RemoveAt(selectedIndex);
								this.indicatorPropsPage.ListOfListParamInfo.RemoveAt(selectedIndex);
								int count = listBoxIndicatorsConfig.Items.Count;
								int num = ((selectedIndex == count) ? (count - 1) : selectedIndex);
								listBoxIndicatorsConfig.SelectedIndex = num;
								if (num < 0)
								{
									this.indicatorPropsPage.StackPanelIndicatorProps.Children.Clear();
									Label label = sender as Label;
									label.Foreground = Brushes.Gray;
									label.IsEnabled = false;
									label.Cursor = Cursors.No;
								}
								this.isIndicatorsConfigChanged = true;
							});
						}
						dispatcher.InvokeAsync(action);
					}
				}
				catch (Exception ex)
				{
					this.DD.PrintException(ex);
				}
			}, e);
		}
		[MethodImpl(MethodImplOptions.NoInlining)]
		private void OnLblDown_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			base.TriggerCustomEvent(delegate(object state)
			{
				try
				{
					if (this.isCharting)
					{
						base.ChartControl.Dispatcher.InvokeAsync(delegate
						{
							ListBox listBoxIndicatorsConfig = this.indicatorsListPage.listBoxIndicatorsConfig;
							if (listBoxIndicatorsConfig == null || listBoxIndicatorsConfig.SelectedItems == null || listBoxIndicatorsConfig.SelectedItems.Count <= 0)
							{
								return;
							}
							int selectedIndex = listBoxIndicatorsConfig.SelectedIndex;
							if (selectedIndex >= listBoxIndicatorsConfig.Items.Count - 1)
							{
								return;
							}
							int num = selectedIndex + 1;
							DDDeepStackConfluence.IndicatorItem indicatorItem = listBoxIndicatorsConfig.SelectedItem as DDDeepStackConfluence.IndicatorItem;
							if (selectedIndex == listBoxIndicatorsConfig.Items.Count - 2)
							{
								listBoxIndicatorsConfig.Items.RemoveAt(selectedIndex);
								listBoxIndicatorsConfig.Items.Add(indicatorItem);
								listBoxIndicatorsConfig.SelectedIndex = listBoxIndicatorsConfig.Items.Count - 1;
								DDDeepStackConfluence.IndicatorConfig indicatorConfig = this.indicatorPropsPage.ListIndicatorConfig[selectedIndex];
								List<DDDeepStackConfluence.ParamInfo> list = this.indicatorPropsPage.ListOfListParamInfo[selectedIndex];
								this.indicatorPropsPage.ListIndicatorConfig.RemoveAt(selectedIndex);
								this.indicatorPropsPage.ListIndicatorConfig.Add(indicatorConfig);
								this.indicatorPropsPage.ListOfListParamInfo.RemoveAt(selectedIndex);
								this.indicatorPropsPage.ListOfListParamInfo.Add(list);
							}
							else
							{
								this.MoveIndicatorItem(listBoxIndicatorsConfig, selectedIndex, num);
							}
							this.isIndicatorsConfigChanged = true;
						});
					}
				}
				catch (Exception ex)
				{
					this.DD.PrintException(ex);
				}
			}, e);
		}
		[MethodImpl(MethodImplOptions.NoInlining)]
		private void OnLblUp_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			base.TriggerCustomEvent(delegate(object state)
			{
				try
				{
					if (this.isCharting)
					{
						base.ChartControl.Dispatcher.InvokeAsync(delegate
						{
							ListBox listBoxIndicatorsConfig = this.indicatorsListPage.listBoxIndicatorsConfig;
							if (listBoxIndicatorsConfig == null || listBoxIndicatorsConfig.SelectedItems == null || listBoxIndicatorsConfig.SelectedItems.Count <= 0)
							{
								return;
							}
							int selectedIndex = listBoxIndicatorsConfig.SelectedIndex;
							if (selectedIndex <= 0)
							{
								return;
							}
							int num = selectedIndex - 1;
							this.MoveIndicatorItem(listBoxIndicatorsConfig, selectedIndex, num);
							this.isIndicatorsConfigChanged = true;
						});
					}
				}
				catch (Exception ex)
				{
					this.DD.PrintException(ex);
				}
			}, e);
		}
		[MethodImpl(MethodImplOptions.NoInlining)]
		private void OnListIndicatorsConfig_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			base.TriggerCustomEvent(delegate(object state)
			{
				try
				{
					if (this.isCharting)
					{
						base.ChartControl.Dispatcher.InvokeAsync(delegate
						{
							bool flag = this.indicatorsListPage.listBoxIndicatorsConfig.Items.Count > 1;
							this.indicatorsListPage.lblUp.IsEnabled = flag;
							this.indicatorsListPage.lblDown.IsEnabled = flag;
							this.indicatorsListPage.lblUp.Foreground = (flag ? this.MainWindowTextColor : Brushes.Gray);
							this.indicatorsListPage.lblUp.Cursor = (flag ? Cursors.Hand : Cursors.No);
							this.indicatorsListPage.lblDown.Foreground = (flag ? this.MainWindowTextColor : Brushes.Gray);
							this.indicatorsListPage.lblDown.Cursor = (flag ? Cursors.Hand : Cursors.No);
						});
					}
				}
				catch
				{
				}
			}, e);
		}
		[MethodImpl(MethodImplOptions.NoInlining)]
		private void MoveIndicatorItem(ListBox listBox, int selectedIndex, int insertIndex)
		{
			DDDeepStackConfluence.IndicatorItem indicatorItem = listBox.SelectedItem as DDDeepStackConfluence.IndicatorItem;
			listBox.Items.RemoveAt(selectedIndex);
			listBox.Items.Insert(insertIndex, indicatorItem);
			listBox.SelectedIndex = insertIndex;
			DDDeepStackConfluence.IndicatorConfig indicatorConfig = this.indicatorPropsPage.ListIndicatorConfig[selectedIndex];
			List<DDDeepStackConfluence.ParamInfo> list = this.indicatorPropsPage.ListOfListParamInfo[selectedIndex];
			this.indicatorPropsPage.ListIndicatorConfig.RemoveAt(selectedIndex);
			this.indicatorPropsPage.ListIndicatorConfig.Insert(insertIndex, indicatorConfig);
			this.indicatorPropsPage.ListOfListParamInfo.RemoveAt(selectedIndex);
			this.indicatorPropsPage.ListOfListParamInfo.Insert(insertIndex, list);
		}
		[MethodImpl(MethodImplOptions.NoInlining)]
		private void ShowRefreshChartMessage()
		{
			if (this.isCharting)
			{
				base.ChartControl.Dispatcher.InvokeAsync(delegate
				{
					this.yesNoMessageWindow.ShowMessageBoxOKButtonOnly(this.chartWindow, "You have to reload the chart (press F5) for the new setting to take effect.", this.MainWindowTextColor, "DeepStack Confluence by DD.co", "OK");
					this.yesNoMessageWindow.YesNoMessageWindow.Closing += this.OnYesNoMessageWindow_Closing;
				});
			}
		}
		[MethodImpl(MethodImplOptions.NoInlining)]
		private void TerminateMainWindowNT()
		{
			try
			{
				if (this.isCharting)
				{
					base.ChartControl.Dispatcher.InvokeAsync(delegate
					{
						if (this.mainWindowNT != null)
						{
							this.mainWindowNT.Close();
							this.mainWindowNT.Closing -= this.OnMainWindowNT_Closing;
							this.mainWindowNT.SizeChanged -= new SizeChangedEventHandler(this.OnMainWindowChanged);
							this.mainWindowNT.LocationChanged -= this.OnMainWindowChanged;
							this.mainWindowNT = null;
						}
					});
				}
			}
			catch (Exception ex)
			{
				this.DD.PrintException(ex);
			}
		}
		[MethodImpl(MethodImplOptions.NoInlining)]
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
								this.mainWindowNT.Closing -= this.OnMainWindowNT_Closing;
								this.mainWindowNT.SizeChanged -= new SizeChangedEventHandler(this.OnMainWindowChanged);
								this.mainWindowNT.LocationChanged -= this.OnMainWindowChanged;
							}
							Window window = Window.GetWindow(base.ChartControl.Parent);
							this.mainWindowNT = new NTWindow
							{
								Caption = "DeepStack Confluence",
								Padding = new Thickness(0.0),
								MinWidth = 350.0,
								MinHeight = 200.0,
								Owner = window
							};
							this.gridContent = new Grid
							{
								Margin = new Thickness(10.0)
							};
							this.gridContent.RowDefinitions.Add(new RowDefinition
							{
								Height = new GridLength(1.0, GridUnitType.Star)
							});
							this.gridContent.RowDefinitions.Add(new RowDefinition
							{
								Height = default(GridLength)
							});
							this.gridContent.ColumnDefinitions.Add(new ColumnDefinition
							{
								Width = new GridLength(1.2, GridUnitType.Star),
								MinWidth = 100.0
							});
							this.gridContent.ColumnDefinitions.Add(new ColumnDefinition
							{
								Width = new GridLength(8.0)
							});
							this.gridContent.ColumnDefinitions.Add(new ColumnDefinition
							{
								Width = new GridLength(1.8, GridUnitType.Star),
								MinWidth = 100.0
							});
							this.GetAllMethods();
							this.indicatorsListPage = new DDDeepStackConfluence.IndicatorsListPage(this.sortedListIndicatorItem, this.isLightTheme, this.MainWindowTextColor, this.mainWindowBorderColor);
							this.indicatorsListPage.SetValue(Grid.ColumnProperty, 0);
							this.indicatorsListPage.DDSearchBarControl.TextChanged += this.OnTbSearch_TextChanged;
							this.indicatorsListPage.listBoxIndicatorsResult.SelectionChanged += this.OnListIndicatorsResult_SelectionChanged;
							this.indicatorsListPage.listBoxIndicatorsResult.MouseDoubleClick += this.OnListIndicatorsResult_MouseDoubleClick;
							this.indicatorsListPage.listBoxIndicatorsMain.SelectionChanged += this.OnListIndicatorsMain_SelectionChanged;
							this.indicatorsListPage.listBoxIndicatorsMain.MouseDoubleClick += this.OnListIndicatorsMain_MouseDoubleClick;
							this.indicatorsListPage.listBoxIndicatorsConfig.SelectionChanged += this.OnListIndicatorsConfigured_SelectionChanged;
							this.indicatorsListPage.lblAdd.MouseLeftButtonUp += this.OnLblAdd_MouseLeftButtonUp;
							this.indicatorsListPage.lblRemove.MouseLeftButtonUp += this.OnLblRemove_MouseLeftButtonUp;
							this.indicatorsListPage.lblUp.MouseLeftButtonUp += this.OnLblUp_MouseLeftButtonUp;
							this.indicatorsListPage.lblDown.MouseLeftButtonUp += this.OnLblDown_MouseLeftButtonUp;
							((INotifyCollectionChanged)this.indicatorsListPage.listBoxIndicatorsConfig.Items).CollectionChanged += this.OnListIndicatorsConfig_CollectionChanged;
							GridSplitter gridSplitter = new GridSplitter
							{
								Background = Brushes.Transparent,
								HorizontalAlignment = HorizontalAlignment.Stretch
							};
							gridSplitter.SetValue(Grid.ColumnProperty, 1);
							List<string> list = new List<string>();
							foreach (object obj in Enum.GetValues(typeof(DDDeepStackConfluence_Operators)))
							{
								DDDeepStackConfluence_Operators DDDeepStackConfluence_Operators = (DDDeepStackConfluence_Operators)obj;
								if (DDDeepStackConfluence_Operators == DDDeepStackConfluence_Operators.Greater)
								{
									list.Add(">");
								}
								else if (DDDeepStackConfluence_Operators == DDDeepStackConfluence_Operators.Smaller)
								{
									list.Add("<");
								}
								else if (DDDeepStackConfluence_Operators == DDDeepStackConfluence_Operators.GreaterOrEqual)
								{
									list.Add(">=");
								}
								else if (DDDeepStackConfluence_Operators == DDDeepStackConfluence_Operators.SmallerOrEqual)
								{
									list.Add("<=");
								}
								else if (DDDeepStackConfluence_Operators == DDDeepStackConfluence_Operators.Equal)
								{
									list.Add("=");
								}
								else
								{
									list.Add("!=");
								}
							}
							this.indicatorPropsPage = new DDDeepStackConfluence.IndicatorPropertiesPage(this.dictMethodInfo, this.listTimeframeStr, list, this.DocumentsPath, base.Instruments[0].FullName, base.BarsPeriods[0].ToString(), this.isLightTheme, this.MainWindowTextColor, this.mainWindowBorderColor);
							this.indicatorPropsPage.SetValue(Grid.ColumnProperty, 2);
							this.indicatorPropsPage.ListOfListParamInfo = new List<List<DDDeepStackConfluence.ParamInfo>>();
							if (string.IsNullOrWhiteSpace(this.IndicatorsConfigJSON) && string.IsNullOrWhiteSpace(this.IndicatorsParamsJSON))
							{
								this.indicatorPropsPage.ListIndicatorConfig = new List<DDDeepStackConfluence.IndicatorConfig>();
							}
							else
							{
								this.indicatorPropsPage.ListIndicatorConfig = this.effListIndicatorConfig;
								this.indicatorPropsPage.ListOfListParamInfo = this.effListOfListParamInfo;
								int count = this.indicatorPropsPage.ListIndicatorConfig.Count;
								if (count > 0)
								{
									for (int i = 0; i < count; i++)
									{
										DDDeepStackConfluence.IndicatorConfig indicatorConfig = this.indicatorPropsPage.ListIndicatorConfig[i];
										if (this.sortedListIndicatorItem.ContainsKey(indicatorConfig.Name))
										{
											DDDeepStackConfluence.IndicatorItem indicatorItem = new DDDeepStackConfluence.IndicatorItem(indicatorConfig.Name, this.MainWindowTextColor);
											string text = indicatorConfig.DisplayName;
											if (text.Contains("instrument"))
											{
												text = text.Replace("instrument", base.Instruments[0].FullName);
											}
											if (text.Contains("period"))
											{
												text = text.Replace("period", base.BarsPeriods[0].ToString());
											}
											indicatorItem.DisplayName = text;
											this.indicatorsListPage.listBoxIndicatorsConfig.Items.Add(indicatorItem);
										}
									}
									this.indicatorsListPage.listBoxIndicatorsConfig.SelectedIndex = 0;
								}
							}
							this.gridContent.Children.Add(this.indicatorsListPage);
							this.gridContent.Children.Add(gridSplitter);
							this.gridContent.Children.Add(this.indicatorPropsPage);
							StackPanel stackPanel = new StackPanel
							{
								Orientation = Orientation.Horizontal,
								HorizontalAlignment = HorizontalAlignment.Right,
								Margin = new Thickness(0.0, 18.0, 0.0, 0.0)
							};
							stackPanel.SetValue(Grid.ColumnSpanProperty, 3);
							stackPanel.SetValue(Grid.RowProperty, 1);
							this.btnCancel = new Button
							{
								Content = "Cancel",
								Height = 30.0,
								MinWidth = 100.0,
								MinHeight = 30.0,
								ToolTip = string.Empty,
								Cursor = Cursors.Hand
							};
							this.btnOK = new Button
							{
								Content = "OK",
								Height = 30.0,
								MinWidth = 100.0,
								MinHeight = 30.0,
								ToolTip = string.Empty,
								Cursor = Cursors.Hand
							};
							stackPanel.Children.Add(this.btnOK);
							stackPanel.Children.Add(this.btnCancel);
							this.btnCancel.Click += this.OnBtnCancle_Click;
							this.btnOK.Click += this.OnBtnOK_Click;
							this.gridContent.Children.Add(stackPanel);
							this.mainWindowNT.Content = this.gridContent;
							this.mainWindowNT.Closing += this.OnMainWindowNT_Closing;
							this.mainWindowNT.SizeChanged += new SizeChangedEventHandler(this.OnMainWindowChanged);
							this.mainWindowNT.LocationChanged += this.OnMainWindowChanged;
							bool flag = this.MainWindowTop == 0.0 && this.MainWindowLeft == 0.0;
							if (flag)
							{
								this.mainWindowNT.WindowStartupLocation = WindowStartupLocation.CenterOwner;
							}
							this.mainWindowNT.Opacity = 0.0;
							this.mainWindowNT.Show();
							if (!flag)
							{
								this.mainWindowNT.Top = this.MainWindowTop;
								this.mainWindowNT.Left = this.MainWindowLeft;
							}
							this.mainWindowNT.Width = ((this.MainWindowWidth < 0.0) ? 350.0 : Math.Max(350.0, this.MainWindowWidth));
							this.mainWindowNT.Height = ((this.MainWindowHeight < 0.0) ? 200.0 : Math.Max(200.0, this.MainWindowHeight));
							this.mainWindowNT.Opacity = 1.0;
							this.isIndicatorsConfigChanged = false;
						}
					}
					catch (Exception ex)
					{
						this.DD.PrintException(ex);
					}
				});
			}
		}
		[MethodImpl(MethodImplOptions.NoInlining)]
		private static void NumericTextBox_TextChangedHandler(TextBox textBox, ref string oldText, bool includedMinus = false, bool includedDot = false)
		{
			if (includedMinus && textBox.Text == '-'.ToString())
			{
				return;
			}
			if (includedDot && textBox.Text == ".")
			{
				textBox.Text = "0.";
				return;
			}
			if (textBox.Text.Contains(","))
			{
				textBox.Text = textBox.Text.Replace(",", string.Empty);
			}
			if (oldText == null)
			{
				oldText = textBox.Text.Trim();
			}
			string text = string.Empty;
			string text2 = "0123456789" + (includedMinus ? '-'.ToString() : string.Empty) + (includedDot ? "." : string.Empty);
			for (int i = 0; i < oldText.Length; i++)
			{
				if (text2.Contains(oldText[i]))
				{
					text += oldText[i].ToString();
				}
			}
			for (int j = 0; j < text.Length; j++)
			{
				if (text.Count((char x) => x == "."[0]) < 1)
				{
					break;
				}
				int num = text.IndexOf(".");
				text = text.Substring(0, num + 1) + text.Substring(num + 1).Replace(".", string.Empty);
			}
			for (int k = 0; k < text.Length; k++)
			{
				if (text.Count((char x) => x == '-') < 1)
				{
					break;
				}
				int num2 = text.IndexOf('-');
				if (num2 == 0)
				{
					text = text.Substring(0, num2 + 1) + text.Substring(num2 + 1).Replace('-'.ToString(), string.Empty);
				}
				else
				{
					text = text.Replace('-'.ToString(), string.Empty);
				}
			}
			oldText = text;
			if ((!includedDot && textBox.Text.Contains(".")) || (!includedMinus && textBox.Text.Contains('-')))
			{
				textBox.Text = oldText.Trim();
				textBox.Select(textBox.Text.Length, 0);
				return;
			}
			bool flag = textBox.Text.Split(new char[] { "."[0] }).Length - 1 > 1;
			bool flag2 = textBox.Text.Split(new char[] { '-' }).Length - 1 > 1;
			if (flag || flag2)
			{
				textBox.Text = oldText;
			}
			double naN = double.NaN;
			if (!double.TryParse(textBox.Text, out naN) || double.IsNaN(naN))
			{
				textBox.Text = oldText;
			}
			textBox.Text = textBox.Text.Trim();
			textBox.Select(textBox.Text.Length, 0);
		}
		private static void NumericTextBox_PreviewKeyDownHandler(TextBox textBox, ref string oldText)
		{
			oldText = textBox.Text.Trim();
		}
		[MethodImpl(MethodImplOptions.NoInlining)]
		private static void NumericTextBox_LostFocusHandler(TextBox textBox, int minValue = 1)
		{
			double naN = double.NaN;
			if (!double.TryParse(textBox.Text, out naN) || double.IsNaN(naN))
			{
				textBox.Text = minValue.ToString();
				return;
			}
			textBox.Text = naN.ToString();
		}
		[MethodImpl(MethodImplOptions.NoInlining)]
		private void PrintMarker(bool isBullish, DDDeepStackConfluence.MarkerType markerType)
		{
			if (!this.isCharting)
			{
				return;
			}
			if (!this.MarkerEnabled)
			{
				return;
			}
			if (base.CurrentBars[0] < base.BarsRequiredToPlot)
			{
				return;
			}
			string text = (isBullish ? "bullish" : "bearish");
			string text2;
			if (markerType == NinjaTrader.NinjaScript.Indicators.DimDim.DDDeepStackConfluence.MarkerType.Start)
			{
				text2 = (isBullish ? this.MarkerStringSyncSignalBullishStart : this.MarkerStringSyncSignalBearishStart);
				this.tag = string.Format("{0}.marker.start.{1}.{2}", "DDDeepStackConfluence", text, base.CurrentBars[0]);
			}
			else if (markerType == NinjaTrader.NinjaScript.Indicators.DimDim.DDDeepStackConfluence.MarkerType.Continuing)
			{
				text2 = (isBullish ? this.MarkerStringSyncSignalBullishContinuing : this.MarkerStringSyncSignalBearishContinuing);
				this.tag = string.Format("{0}.marker.continuing.{1}.{2}", "DDDeepStackConfluence", text, base.CurrentBars[0]);
			}
			else
			{
				text2 = (isBullish ? this.MarkerStringSyncSignalBullishEnd : this.MarkerStringSyncSignalBearishEnd);
				this.tag = string.Format("{0}.marker.end.{1}.{2}", "DDDeepStackConfluence", text, base.CurrentBars[0]);
			}
			if (string.IsNullOrWhiteSpace(text2))
			{
				return;
			}
			text2 = this.DD.FormatMarkerString(text2);
			global::System.Windows.Media.Brush brush = (isBullish ? this.MarkerBrushBullish : this.MarkerBrushBearish);
			double num = base.Lows[0][0];
			double num2 = base.Highs[0][0];
			bool flag = markerType == NinjaTrader.NinjaScript.Indicators.DimDim.DDDeepStackConfluence.MarkerType.End;
			double num3 = (flag ? (isBullish ? num2 : num) : (isBullish ? num : num2));
			int num4 = Convert.ToInt32(this.DD.ComputeTextSize(text2, this.MarkerFont, this.ScreenDPI).Height);
			int num5 = (flag ? (isBullish ? 1 : (-1)) : (isBullish ? (-1) : 1)) * (this.MarkerOffset + num4 / 2);
			NinjaTrader.NinjaScript.DrawingTools.Draw.Text(this, this.tag, base.IsAutoScale, text2, 0, num3, num5, brush, this.MarkerFont, TextAlignment.Center, Brushes.Transparent, Brushes.Transparent, 0);
		}
		[MethodImpl(MethodImplOptions.NoInlining)]
		private void PaintBar(bool isUptrend)
		{
			if (!this.isCharting)
			{
				return;
			}
			if (!this.BarEnabled)
			{
				return;
			}
			global::System.Windows.Media.Brush brush = (isUptrend ? this.BarBullish : this.BarBearish);
			int num = base.Closes[0][0].ApproxCompare(base.Opens[0][0]);
			int num2 = (isUptrend ? 1 : (-1));
			if (this.BarOutlineEnabled && !brush.IsTransparent())
			{
				base.CandleOutlineBrush = brush;
			}
			if (this.BarBiasBased)
			{
				if (brush.IsTransparent() && num2 * num < 0)
				{
					base.BarBrush = Brushes.Transparent;
					return;
				}
				if (num != 0)
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

		static DDDeepStackConfluence()
		{
			NinjaTrader.NinjaScript.Indicators.DimDim.DDDeepStackConfluence.dictRenkoInfo = new Dictionary<string, string>
			{
				{ "12345", "n" },
				{ "678910", "K" },
				{ "11121314", "zrtv" },
				{ "15161718", "zr" },
				{ "19202122", "zv" }
			};
		}
		private const double mainWindowMinWidth = 350.0;
		private const double mainWindowMinHeight = 200.0;
		private const byte marginValue = 10;
		private const byte buttonMinWidth = 100;
		private const byte buttonMinHeight = 30;
		private const byte parameterControlMinWidth = 50;
		private const byte paramControlMinHeight = 20;
		private const byte splitPanelMinWidth = 100;
		private const byte btnFontSize = 12;
		private const string expandIcon = "\n\n⮚ ";
		private const string dotChar = ".";
		private const char minusChar = '-';
		private const string textToolTipDrag = "Drag to anywhere; double click to {0}.";
		private const string searchTemplatePlaceHolder = "Start typing to search";
		private const string msgInputParameter_StringFormat = "Please input the value for parameter{0} below:";
		private const string msgInputReload = "You have to reload the chart (press F5) for the new setting to take effect.";
		private const string msgNotificationIndicatorCannotLoad = "An error occurred while loading the indicator.\nPlease reload the chart (press F5) and try a different indicator.";
		private const int DDRenkoCode = 12345;
		private const int kingRenkoCode = 678910;
		private const int zonixRTVCode = 11121314;
		private const int zonixRenkoCode = 15161718;
		private const int zonixVectorCode = 19202122;
		private static Dictionary<string, string> dictRenkoInfo;
		private const string textTooltipBtnTitle = "Click to choose an indicator.";
		private const string customDll = "NinjaTrader.Custom.dll";
		private const string ntCustomPath = "NinjaTrader 8\\bin\\Custom\\";
		private const string nudSettingsPath = "\\NinjaTrader 8\\DD.co\\DDDeepStackConfluence";
		private const string userNote = "instrument (period)";
		private const byte padding = 22;
		public const string dataSeries3Type = "DataSeries3Type";
		public const string dataSeries3Value = "DataSeries3Value";
		public const string dataSeries3Value1 = "DataSeries3Value1";
		public const string dataSeries3Value2 = "DataSeries3Value2";
		public const string dataSeries4Type = "DataSeries4Type";
		public const string dataSeries4Value = "DataSeries4Value";
		public const string dataSeries4Value1 = "DataSeries4Value1";
		public const string dataSeries4Value2 = "DataSeries4Value2";
		public const string dataSeries5Type = "DataSeries5Type";
		public const string dataSeries5Value = "DataSeries5Value";
		public const string dataSeries5Value1 = "DataSeries5Value1";
		public const string dataSeries5Value2 = "DataSeries5Value2";
		public const string dataSeriesBehindType = "DataSeriesBehindType";
		public const string dataSeriesBehindValue = "DataSeriesBehindValue";
		public const string disabled = "Disabled";
		public const string tick = "Tick";
		public const string volume = "Volume";
		public const string range = "Range";
		public const string second = "Second";
		public const string minute = "Minute";
		public const string day = "Day";
		public const string week = "Week";
		public const string month = "Month";
		public const string year = "Year";
		public const string DDRenko = "DDRenko";
		public const string kingRenko = "KingRenko$";
		private const string greaterOperator = ">";
		private const string smallerOperator = "<";
		private const string smallerOrEqualOperator = "<=";
		private const string greaterOrEqualOperator = ">=";
		private const string equalOperator = "=";
		private const string unequalOperator = "!=";
		private const string between = "btw.";
		private const int defaultValueBullish = 1;
		private const int defaultValueBearish = -1;
		private const DDDeepStackConfluence_Operators defaultOperator = DDDeepStackConfluence_Operators.Equal;
		private int _DataSeries2Value;
		private int _DataSeries2Value1;
		private int _DataSeries2Value2;
		private int _DataSeries3Value;
		private int _DataSeries3Value1;
		private int _DataSeries3Value2;
		private int _DataSeries4Value;
		private int _DataSeries4Value1;
		private int _DataSeries4Value2;
		private int _DataSeries5Value;
		private int _DataSeries5Value1;
		private int _DataSeries5Value2;
		private NinjaTrader.NinjaScript.DrawingTools.TextPosition cpPanelPositionAlignment;
		private const int defaultMargin = 5;
		private const string toolTipSpace = "  ";
		private string tag;
		private List<string> listIndicatorExcluded = new List<string>
		{
			"DDEOBExit", "DDEOBOrdering", "DDGlobalZlert", "DDBracketOrdering", "DDMultiTimeframeFusion", "HelloWin_CaptainOptimusStrong", "HelloWin_CaptainOptimusStrong_v2", "HelloWin", "HelloWin_InfinityAlgoEngine", "DDMultiInstrumentSynergy",
			"DDResources", "DDATR", "DDBarStatus", "DDBidAskDisplay", "DDHelperMFI", "DDHelperRSI", "DDHelperSMMA", "DDHelperStochastic", "DDTickDataMicroscope"
		};
		private IndicatorBase[][] indicatorBaseArr;
		private IndicatorBase[][][] cacheIndicatorBaseArr;
		private Dictionary<int, double>[] dictPlotValue0Arr;
		private int[] signalStateArr;
		private string[] lblTextArr;
		private List<string> listTimeframeStr;
		private Dictionary<string, MethodInfo> dictMethodInfo;
		private SortedList<string, DDDeepStackConfluence.IndicatorItem> sortedListIndicatorItem;
		private Dictionary<int, List<DDDeepStackConfluence.PlotOrDataSeriesInfo>>[] dictPlotOrDataSeriesArr;
		private SortedList<int, DDDeepStackConfluence.MarkerInfo> dictMarkers;
		private List<DDDeepStackConfluence.IndicatorConfig> effListIndicatorConfig;
		private List<List<DDDeepStackConfluence.ParamInfo>> effListOfListParamInfo;
		private NTWindow mainWindowNT;
		private ShowYesNoMessageWindow yesNoMessageWindow;
		private Chart chartWindow;
		private global::System.Windows.Media.Brush backgroundBullish;
		private global::System.Windows.Media.Brush backgroundBearish;
		private global::System.Windows.Media.Brush ribbonNeutral;
		private global::System.Windows.Media.Brush ribbonBullish;
		private global::System.Windows.Media.Brush ribbonBearish;
		private bool isOnBarCloseMode;
		private bool isRibbonPositionBottom;
		private SortedList<int, int[]> sortedListSignalInfo;
		private bool needReloadChart;
		private int maxLabelWidth;
		private int maxLabelHeight;
		private int[][] timeframeInfoArr;
		private const string nickname = "deepstack:exc";
		private const string prefix = "DDDeepStackConfluence";
		private const string indicatorName = "DeepStack Confluence";
		private const string indicatorNameFull = "DeepStack Confluence by DD.co";
		private const string receiverEmail = "receiver@example.com";
		private bool isCharting;
		private bool isLightTheme;
		private bool isPricePanel;
		private static global::System.Windows.Media.Brush childWindowBackground;
		private int countDataSeries = 1;
		private int countIndicator;
		private int countPlot = 2;
		private bool isCustomRenderingMethod;
		private bool isNeedCheckDataSeries = true;
		private bool isNullIndicators = true;
		private int markerIndexStart;
		private float yReload;
		private DDDeepStackConfluence.DragablePanel dragablePanel;
		private bool isAddedToListBoxConfig;
		private DDDeepStackConfluence.IndicatorsListPage indicatorsListPage;
		private DDDeepStackConfluence.IndicatorPropertiesPage indicatorPropsPage;
		private Grid gridContent;
		private Button btnOK;
		private Button btnCancel;
		private global::System.Windows.Media.Brush mainWindowBorderColor;
		private bool isIndicatorsConfigChanged;
		private struct MarkerInfo
		{
			public int BarIndex { get; set; }
			public bool IsBullish { get; set; }
			public DDDeepStackConfluence.MarkerType MarkerType { get; set; }

		}
		private enum MarkerType
		{
			Start,
			Continuing,
			End
		}
		private class DragablePanel : Grid
		{
			public global::System.Windows.Media.Brush MainWindowTextColor { get; set; }
			public global::System.Windows.Media.Brush ChildWindowBackground { get; set; }
			[MethodImpl(MethodImplOptions.NoInlining)]
			public DragablePanel(string indicatorName, int[][] timeframeInfoArr, global::System.Windows.Media.Brush titleBackground, global::System.Windows.Media.Brush titleTextColor, global::System.Windows.Media.Brush dragBackground, int textSize, NinjaTrader.NinjaScript.DrawingTools.TextPosition alignment, Thickness thickness, bool minimized, global::System.Windows.Media.Brush mainWindowTextColor, global::System.Windows.Media.Brush childWindowBackground)
			{
				this.MainWindowTextColor = mainWindowTextColor;
				this.ChildWindowBackground = childWindowBackground;
				this.alignment = alignment;
				this.SetPosition(thickness);
				this.drag = new Thumb
				{
					Cursor = Cursors.SizeAll,
					Width = 6.0,
					ToolTip = "  Drag to anywhere; double click to {0}." + (minimized ? "restore" : "minimize")
				};
				FrameworkElementFactory frameworkElementFactory = new FrameworkElementFactory(typeof(Border));
				frameworkElementFactory.SetValue(Border.BackgroundProperty, dragBackground);
				this.drag.Template = new ControlTemplate(typeof(Thumb))
				{
					VisualTree = frameworkElementFactory
				};
				this.drag.SetValue(Grid.ColumnProperty, 0);
				this.btnMini = new Button
				{
					Content = indicatorName,
					MinWidth = 0.0,
					Background = dragBackground,
					BorderBrush = dragBackground,
					Foreground = titleTextColor,
					HorizontalAlignment = HorizontalAlignment.Left,
					VerticalAlignment = VerticalAlignment.Center,
					Margin = new Thickness(1.0, 0.0, 0.0, 0.0),
					FontSize = (double)textSize,
					Tag = 99,
					ToolTip = "  Click to restore",
					Cursor = Cursors.Hand
				};
				Size size = DDResources_GlobalConstantAndFunction.MeasureControlSize(this.btnMini, "");
				this.btnMini.Width = size.Width;
				this.btnMini.Height = size.Height;
				this.btnMini.SetValue(Grid.ColumnProperty, 1);
				this.gridContent = new Grid();
				this.gridContent.RowDefinitions.Add(new RowDefinition());
				this.gridContent.RowDefinitions.Add(new RowDefinition());
				this.gridContent.SetValue(Grid.ColumnProperty, 2);
				this.btnTitle = new Button
				{
					MinWidth = 0.0,
					Content = indicatorName,
					Foreground = titleTextColor,
					Background = titleBackground,
					BorderBrush = titleBackground,
					HorizontalAlignment = HorizontalAlignment.Stretch,
					FontSize = (double)textSize,
					Margin = new Thickness(1.0, 0.0, 0.0, 1.0),
					ToolTip = "  Click to choose an indicator.",
					Cursor = Cursors.Hand
				};
				this.btnTitle.SetValue(Grid.ColumnSpanProperty, 8);
				this.gridContent.Children.Add(this.btnTitle);
				this.gridContent.RowDefinitions.Add(new RowDefinition());
				this.gridContent.RowDefinitions.Add(new RowDefinition());
				Thickness thickness2 = new Thickness(1.0, 1.0, 0.0, 0.0);
				int num = timeframeInfoArr.Length;
				this.comboBoxArr = new ComboBox[num];
				this.nudValueArr = new DDNumericUpDown[num * 2];
				for (int i = 0; i < num; i++)
				{
					DDDeepStackConfluence_DataSeriesType DDDeepStackConfluence_DataSeriesType = (DDDeepStackConfluence_DataSeriesType)timeframeInfoArr[i][0];
					int num2 = timeframeInfoArr[i][1];
					int num3 = timeframeInfoArr[i][2];
					int num4 = timeframeInfoArr[i][3];
					int num5 = i * 2;
					ComboBox comboBox = this.CreateComboBox((double)textSize, i, num5, 1, 2);
					comboBox.SelectedValue = DDDeepStackConfluence_DataSeriesType;
					this.gridContent.Children.Add(comboBox);
					this.comboBoxArr[i] = comboBox;
					DDNumericUpDown DDNumericUpDown = this.CreateNumericUpDown(thickness2, (double)textSize, num5, 2, 0);
					DDNumericUpDown DDNumericUpDown2 = this.CreateNumericUpDown(thickness2, (double)textSize, num5 + 1, 2, 0);
					this.gridContent.Children.Add(DDNumericUpDown);
					this.gridContent.Children.Add(DDNumericUpDown2);
					this.nudValueArr[num5] = DDNumericUpDown;
					this.nudValueArr[num5 + 1] = DDNumericUpDown2;
					if (DDDeepStackConfluence_DataSeriesType == DDDeepStackConfluence_DataSeriesType.Disabled)
					{
						DDNumericUpDown.IsEnabled = (DDNumericUpDown2.IsEnabled = false);
						DDNumericUpDown.Value = (double)num2;
						DDNumericUpDown.SetValue(Grid.ColumnSpanProperty, 2);
						DDNumericUpDown2.Visibility = Visibility.Hidden;
					}
					else if (DDDeepStackConfluence_DataSeriesType == DDDeepStackConfluence_DataSeriesType.ninZaRenko || DDDeepStackConfluence_DataSeriesType == DDDeepStackConfluence_DataSeriesType.KingRenko)
					{
						DDNumericUpDown.IsEnabled = (DDNumericUpDown2.IsEnabled = true);
						DDNumericUpDown.Value = (double)num3;
						DDNumericUpDown2.Value = (double)num4;
						DDNumericUpDown2.Visibility = Visibility.Visible;
						DDNumericUpDown.SetValue(Grid.ColumnSpanProperty, 1);
					}
					else
					{
						DDNumericUpDown.IsEnabled = (DDNumericUpDown2.IsEnabled = true);
						DDNumericUpDown.Value = (double)num2;
						DDNumericUpDown.SetValue(Grid.ColumnSpanProperty, 2);
						DDNumericUpDown2.Visibility = Visibility.Hidden;
					}
				}
				this.gridContent.ColumnDefinitions.Add(new ColumnDefinition
				{
					Width = default(GridLength)
				});
				this.gridContent.ColumnDefinitions.Add(new ColumnDefinition
				{
					Width = default(GridLength)
				});
				this.gridContent.ColumnDefinitions.Add(new ColumnDefinition
				{
					Width = default(GridLength)
				});
				this.gridContent.ColumnDefinitions.Add(new ColumnDefinition
				{
					Width = default(GridLength)
				});
				this.gridContent.ColumnDefinitions.Add(new ColumnDefinition
				{
					Width = default(GridLength)
				});
				this.gridContent.ColumnDefinitions.Add(new ColumnDefinition
				{
					Width = default(GridLength)
				});
				this.gridContent.ColumnDefinitions.Add(new ColumnDefinition
				{
					Width = default(GridLength)
				});
				this.gridContent.ColumnDefinitions.Add(new ColumnDefinition
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
				base.ColumnDefinitions.Add(new ColumnDefinition
				{
					Width = default(GridLength)
				});
				base.Children.Add(this.drag);
				base.Children.Add(this.btnMini);
				base.Children.Add(this.gridContent);
				this.Minimized(minimized);
			}
			[MethodImpl(MethodImplOptions.NoInlining)]
			private ComboBox CreateComboBox(double textSettingSize, int tag, int column = 0, int row = 0, int columnSpan = 0)
			{
				ComboBox comboBox = new ComboBox
				{
					Background = Brushes.Gray,
					BorderBrush = Brushes.Gray,
					Foreground = Brushes.White,
					Tag = tag,
					HorizontalAlignment = HorizontalAlignment.Stretch,
					VerticalAlignment = VerticalAlignment.Stretch,
					FontSize = textSettingSize,
					Margin = new Thickness(1.0, 0.0, 0.0, 0.0),
					ToolTip = string.Empty,
					Focusable = false,
					ItemsSource = Enum.GetValues(typeof(DDDeepStackConfluence_DataSeriesType)),
					Cursor = Cursors.Arrow
				};
				if (column > 0)
				{
					comboBox.SetValue(Grid.ColumnProperty, column);
				}
				if (row > 0)
				{
					comboBox.SetValue(Grid.RowProperty, row);
				}
				if (columnSpan > 0)
				{
					comboBox.SetValue(Grid.ColumnSpanProperty, columnSpan);
				}
				return comboBox;
			}
			[MethodImpl(MethodImplOptions.NoInlining)]
			private DDNumericUpDown CreateNumericUpDown(Thickness margin, double textExecutionSize, int column = 0, int row = 0, int columnSpan = 0)
			{
				DDNumericUpDown nud = new DDNumericUpDown
				{
					Background = Brushes.Gray,
					Foreground = Brushes.White,
					BorderBrush = Brushes.Gray,
					HorizontalAlignment = HorizontalAlignment.Stretch,
					VerticalAlignment = VerticalAlignment.Stretch,
					FontSize = textExecutionSize,
					Margin = margin,
					Tag = column,
					Cursor = Cursors.Arrow
				};
				if (column > 0) nud.SetValue(Grid.ColumnProperty, column);
				if (row > 0) nud.SetValue(Grid.RowProperty, row);
				if (columnSpan > 0) nud.SetValue(Grid.ColumnSpanProperty, columnSpan);
				return nud;
			}
			[MethodImpl(MethodImplOptions.NoInlining)]
			private void CreateNewSettingWindow(DDNumericUpDown upDownControl, DDDeepStackConfluence.DragablePanel.GridPopup gridPopup)
			{
				if (this.settingWindow == null)
				{
					Window window = Window.GetWindow(DDResources_GlobalConstantAndFunction.FindVisualParent<ChartControl>(upDownControl, null).Parent);
					this.settingWindow = new Window
					{
						Width = 500.0,
						Height = 250.0,
						Owner = window,
						Topmost = true,
						WindowStartupLocation = WindowStartupLocation.CenterOwner,
						Background = this.ChildWindowBackground
					};
					this.settingWindow.Closing += delegate(object s, CancelEventArgs e)
					{
						this.settingWindow = null;
					};
					DDDeepStackConfluence.DragablePanel.GridSettingContent gridSettingContent = new DDDeepStackConfluence.DragablePanel.GridSettingContent(this.settingWindow, upDownControl, this.ChildWindowBackground, this.MainWindowTextColor)
					{
						Margin = new Thickness(20.0)
					};
					DDDeepStackConfluence.DragablePanel.GridSettingContent gridSettingContent3 = gridSettingContent;
					gridSettingContent3.ButtonOKClicked = (RoutedEventHandler)Delegate.Combine(gridSettingContent3.ButtonOKClicked, new RoutedEventHandler(delegate(object s, RoutedEventArgs e)
					{
						gridSettingContent.SaveSettings(upDownControl);
						this.settingWindow.Close();
						gridPopup.LoadSettings(upDownControl.SettingInfo);
					}));
					DDDeepStackConfluence.DragablePanel.GridSettingContent gridSettingContent2 = gridSettingContent;
					gridSettingContent2.ButtonApplyClicked = (RoutedEventHandler)Delegate.Combine(gridSettingContent2.ButtonApplyClicked, new RoutedEventHandler(delegate(object s, RoutedEventArgs e)
					{
						gridSettingContent.SaveSettings(upDownControl);
						gridPopup.LoadSettings(upDownControl.SettingInfo);
					}));
					this.settingWindow.Content = gridSettingContent;
				}
			}
			[MethodImpl(MethodImplOptions.NoInlining)]
			public void Minimized(bool isMinimized)
			{
				if (isMinimized)
				{
					this.gridContent.Visibility = Visibility.Collapsed;
					this.btnMini.Visibility = Visibility.Visible;
					return;
				}
				this.gridContent.Visibility = Visibility.Visible;
				this.btnMini.Visibility = Visibility.Collapsed;
			}
			[MethodImpl(MethodImplOptions.NoInlining)]
			private void SetPosition(Thickness thickness)
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
			private Grid gridContent;
			public Thumb drag;
			public Button btnMini;
			public Button btnTitle;
			public NinjaTrader.NinjaScript.DrawingTools.TextPosition alignment;
			public Window settingWindow;
			public ComboBox[] comboBoxArr;
			public DDNumericUpDown[] nudValueArr;
			private class GridSettingContent : Grid
			{
				private sealed class GridSettingContentClosure
				{
					public Window settingWindow;
					public GridSettingContent capturedThis;
				}
				public ObservableCollection<int> ListQuickNumbers { get; } = new ObservableCollection<int>();
				public ObservableCollection<string> ListIncrementNumbers { get; } = new ObservableCollection<string>();
				[MethodImpl(MethodImplOptions.NoInlining)]
				public GridSettingContent(Window settingWindow, DDNumericUpDown upDownControl, global::System.Windows.Media.Brush background, global::System.Windows.Media.Brush textColor)
				{
					DDDeepStackConfluence.DragablePanel.GridSettingContent.GridSettingContentClosure closure = new DDDeepStackConfluence.DragablePanel.GridSettingContent.GridSettingContentClosure();
					closure.settingWindow = settingWindow;
					closure.capturedThis = this;
					base.ColumnDefinitions.Add(new ColumnDefinition());
					base.ColumnDefinitions.Add(new ColumnDefinition());
					base.RowDefinitions.Add(new RowDefinition
					{
						Height = default(GridLength)
					});
					base.RowDefinitions.Add(new RowDefinition
					{
						Height = new GridLength(1.0, GridUnitType.Star)
					});
					base.RowDefinitions.Add(new RowDefinition
					{
						Height = default(GridLength)
					});
					this.listQuick.ItemsSource = this.ListQuickNumbers;
					this.listIncrement.ItemsSource = this.ListIncrementNumbers;
					this.listQuick.BorderBrush = textColor;
					this.listIncrement.BorderBrush = textColor;
					foreach (int num in upDownControl.SettingInfo.Quick)
					{
						this.ListQuickNumbers.Add(num);
					}
					foreach (int num2 in upDownControl.SettingInfo.Increment)
					{
						this.ListIncrementNumbers.Add("+" + num2.ToString());
					}
					this.listQuick.SetValue(Grid.RowProperty, 1);
					this.listIncrement.SetValue(Grid.RowProperty, 1);
					this.listIncrement.SetValue(Grid.ColumnProperty, 1);
					base.Children.Add(this.listQuick);
					base.Children.Add(this.listIncrement);
					TextBox textBoxLeft = new TextBox
					{
						BorderThickness = new Thickness(1.0, 1.0, 1.0, 0.0),
						BorderBrush = textColor,
						Foreground = textColor
					};
					Button button = new Button
					{
						MinWidth = 0.0,
						Width = 20.0,
						Height = 20.0,
						BorderThickness = new Thickness(0.0),
						BorderBrush = Brushes.Transparent,
						Cursor = Cursors.Hand,
						Padding = new Thickness(2.0),
						Background = Brushes.LightGray,
						Content = DD_ImageCreator.CreateImageFromCode("iVBORw0KGgoAAAANSUhEUgAAADAAAAAwCAYAAABXAvmHAAAACXBIWXMAAC4jAAAuIwF4pT92AAAD50lEQVR4nNVaTUgVURi90ovox/AnCN20MWxZlGatjITcFRSVge5SEsLM2r6FEJgVFYqh7SyKQqH2Rq5KsKhlkZs2SUEl2c+i4nUOc8fGeXfem+/Oe2/mHTjcee/Ovfec+blz5/smlclkVFRsu6m2ojgAtoCNYANYBW7Wu3wDl8AF8C04Bz5936c+Rh07ZdsQomtQnAI7wSawIsfuWzRprB3sAzPoYx7lHfAezHyx0SE2gEHrUQyAPeBGm0E1aLhZcwj9jqO8BiMfJJ2ENoAB1qLoB9MqmnAT2N95sAfjDKK8DiO/wzQMZQCdbkfxENxpLTEcaOQy2IExj8PEu3wN8hpAR4dRTKr/N2RY8FL4qbc3gPWCtjxQLzB2F0w8zrVjTgPo4DSKW+AaweB/waP+gfWBmBb0xQM2jXZn0NftoJ0CDaBhN4rxkIN5MWM6avwPfc5g85CgL5qdQLsKtJ8w7WA0gAZHUIwJBvIi13RoNVUCY9D0CSYe+SuyDOgblte85LIpNqhlEtp2+2/sVQb0VMnZprKE4sKCmh5A417vFOs/A+dU8afKKNilHI1X3D9WDOgnbDoGUVKkofUuzsIif3jPwAVwUzyaRKDGi8p5cjsG4KgWRXeMoqTohuZLOAuf3TPQoQq/vikmqJWaR10DnTGKsQU1j6b0y0hT3Gos0ETtPAOtKvfLSFJBza00sC9uJRHQQgONAZV/wCfgV2Gnz/PUSc92NXhQmddtO/hnQ0DDY/nW4lKgvxEUI9J2eimetZADGmig2lCxWGjxUaCX4nzy1vmqqmjA9PT9VXxZYpg0VVqHVZICGvgOrvP9vz4GLflg0rRMA5xlan0VdbxxknIf6JvYf/0TSzTAcJ9pJppCQ5tp9JmebUxCzqLYL+zPnUZNWKABxirbDZWsk7yAu2CwNWiq5EPzpEWfQXhDkXy49BWw01JijgZmlXPUym09RM2zKYa4dZS4OW5FQsxTu/scYIi73AxQ88oC6T44pMrnreyHcjQ7BvhuicuIobv+OFUJMEHN3PAuJa6CDOYmPTLBlUN2XIiZEZ1cGI5DlQCDbkyI8C/mbign75XU6Nwr5WhcwSoDjDkyM4LNlyp58dFl8IQ/9ZS1nGb0l5kRbE6p5ESomTTpMqWcjO8DjMPDRK+yS3DUWNblQq8pN0AEvtAwIwITfFxLU0xtpqW4XhK3CfoheOTtUkwEGzIzomRJPprlGYyS5COY3Y+W5CP0C/UeJU+zSgV78RosTJqV0Dc210rFSnS74BKh8IluQnc4zOSCKsynBl5QeHE/NXChBxhgfF6F/9jDBE4Qpf/Yw4UecJSM83Obf43SFH5B5VNuAAAAAElFTkSuQmCC")
					};
					button.SetValue(Grid.ColumnProperty, 1);
					Button button2 = new Button
					{
						MinWidth = 0.0,
						Width = 20.0,
						Height = 20.0,
						BorderThickness = new Thickness(0.0),
						BorderBrush = Brushes.Transparent,
						Cursor = Cursors.Hand,
						Padding = new Thickness(2.0),
						Background = Brushes.LightGray,
						Content = DD_ImageCreator.CreateImageFromCode("iVBORw0KGgoAAAANSUhEUgAAADAAAAAwCAYAAABXAvmHAAAACXBIWXMAAC4jAAAuIwF4pT92AAAErklEQVR4nMVaS4gVRxS9FwdBnIiOCIa4UzHr0UBmQLKa0ZW/hTpoVupi3CjGZBVXI0I0i4SESDLZRdGV342flQgq6Lj2M+6ERMRRyIRAIHTO6ep+vumpqldd1Z05cPu9ma66dU51vapbt7ovyzJJhuqHuH4G+xS2AbYONgDrL0rMwmZg07CnsAewO5Jlv6c23RddU3UFrvtgY7Ah/sdTeqAwCtsKOwLL4OM+Pi/AzkPM2xga9QWY3j4OOwT7IKbR0hNsuLBT8DuJz2/rPpVwAaqLcT0K+1rSiNtAf8eEnaJ6Ep/fQcg/IRXDBKjy0V+EbYxlGAgK+Qa2G23uhYjpXhV6C1DdhutvsGXJ9MLBjppC2/sh4rqvoF+AKsf5Tz3LtQN22CVwOAwRk65CbmKG/C8tEKsD8vsZXMQlwi7ADJuzLRKrA85WZ8HpFURcq96cL0B1Pa7nYIscDl/DOH8PwtY0RPIl7LGY9WSV5f6inJPqYPWHPVeAmSq5sLimyYfChSjLZlB2qXCMioymcZdbsF3w+Rd8crG7AfvEUo6cLqLMcPcUW30CXCFdU+WjnGyWvcv/Mg3uwLersJFI8rdhO+Dr78InO2a0+P8mS/mNBccz8wWYFfaEoyEOmy0d8iXYsOr2SBEkub1D/r3Pd/C5Bd+eiH04ncD9c+WK3f0EvhL30Lmf944NcSLs5N/7nCnipG2Wu+T4pZiVuxBgxt5BT4OD+ZjnsLE3WEeEn7zhw9+Xb9U3IQeElk+AUWW/pwJnm8s5SXevhYgIIb8kb0vkIw+f/oLzD6WAMU/hEiM5uXgRoeRDhyI5Q4DqajEbkRDEimiavOScwZ1PYLP4NyNV1BVBNE0+rwXbTAHDNSqVqCNCWiBfYpgCPo6oKBIqwoc08sQGClgbWVkkRIQL6eSJdRSwMsGBSIyIZsgTKynAN/+HIlxEc+SJ/oXYaTUKCmDSaSDRT+95vkRaAFjFLAW8kTQB4eRLNCfiDQW8gK2PdFCffIlmRExTAOPurRGVQ8MD93qQLuIpBdwTk3GrgzqxjTQQxbpwjwLu0o2Ex0MxgVlKFOsCOd/tQ+U/UJnp7qGWyIukh+I2PCD3ch1gJqKXgNSQuGkR5NzZE58XprjdqzLzNjsbiCpDRewUcxDi2pXNFpwLAWYT/au4f8yPnfvhcPIlQkQwZTPlETBZJhm6Q4nTsANiz0wM5Rt/W2YiLrbxizBJBteQ/lOseSHmWVQnxAipgvmZm7g/Mic3lBaY2UWoLs/bsueEiInuU5xqMPc9bI/YUxrMlN1CA9XUYkoowLpX4KuaWrRl5YipgmMHcwUw56g6VhS0DSXmLJ8USaemkrujhU9fcpfg0NlbPXqaH05n2fP8ZMT0ri1DzQZsGbMUrBF/Z/wL2287crLvB5iHVx2XhT/gILjijtvOBgj3hoYnIppHFzzocJ0VtA32/HjcERNhRDAzzUO+JraedcDF6nNwuOIr1HtLSQeqnJW4dA82w60n+IMeQ9vPehUM2xPTkSpniLYOuktwpmnhoJswDk9DCIdTE68adIPEW37VoIRp4IvilYDQlz2snsQcFv7PL3t0ms8b/DG3BXzd5j9AAhJkmvjHQAAAAABJRU5ErkJggg==")
					};
					button2.SetValue(Grid.ColumnProperty, 2);
					button.Click += delegate(object s, RoutedEventArgs e)
					{
						if (string.IsNullOrWhiteSpace(textBoxLeft.Text))
						{
							return;
						}
						try
						{
							int num4 = int.Parse(textBoxLeft.Text.Trim());
							if (!closure.capturedThis.ListQuickNumbers.Contains(num4))
							{
								closure.capturedThis.ListQuickNumbers.Add(num4);
							}
							closure.capturedThis.SortListBox(closure.capturedThis.listQuick);
						}
						catch
						{
						}
					};
					button2.Click += delegate(object s, RoutedEventArgs e)
					{
						if (closure.capturedThis.listQuick.SelectedItems == null || closure.capturedThis.listQuick.SelectedItems.Count <= 0)
						{
							return;
						}
						closure.capturedThis.ListQuickNumbers.Remove(int.Parse(closure.capturedThis.listQuick.SelectedItem.ToString()));
					};
					Grid grid = new Grid
					{
						Margin = new Thickness(0.0, 0.0, 5.0, 0.0)
					};
					grid.ColumnDefinitions.Add(new ColumnDefinition
					{
						Width = new GridLength(1.0, GridUnitType.Star)
					});
					grid.ColumnDefinitions.Add(new ColumnDefinition
					{
						Width = default(GridLength)
					});
					grid.ColumnDefinitions.Add(new ColumnDefinition
					{
						Width = default(GridLength)
					});
					grid.Children.Add(textBoxLeft);
					grid.Children.Add(button);
					grid.Children.Add(button2);
					base.Children.Add(grid);
					TextBox textBoxRight = new TextBox
					{
						BorderThickness = new Thickness(1.0, 1.0, 1.0, 0.0),
						BorderBrush = textColor,
						Foreground = textColor
					};
					Button button3 = new Button
					{
						MinWidth = 0.0,
						Width = 20.0,
						Height = 20.0,
						BorderThickness = new Thickness(0.0),
						BorderBrush = Brushes.Transparent,
						Cursor = Cursors.Hand,
						Padding = new Thickness(2.0),
						Background = Brushes.LightGray,
						Content = DD_ImageCreator.CreateImageFromCode("iVBORw0KGgoAAAANSUhEUgAAADAAAAAwCAYAAABXAvmHAAAACXBIWXMAAC4jAAAuIwF4pT92AAAD50lEQVR4nNVaTUgVURi90ovox/AnCN20MWxZlGatjITcFRSVge5SEsLM2r6FEJgVFYqh7SyKQqH2Rq5KsKhlkZs2SUEl2c+i4nUOc8fGeXfem+/Oe2/mHTjcee/Ovfec+blz5/smlclkVFRsu6m2ojgAtoCNYANYBW7Wu3wDl8AF8C04Bz5936c+Rh07ZdsQomtQnAI7wSawIsfuWzRprB3sAzPoYx7lHfAezHyx0SE2gEHrUQyAPeBGm0E1aLhZcwj9jqO8BiMfJJ2ENoAB1qLoB9MqmnAT2N95sAfjDKK8DiO/wzQMZQCdbkfxENxpLTEcaOQy2IExj8PEu3wN8hpAR4dRTKr/N2RY8FL4qbc3gPWCtjxQLzB2F0w8zrVjTgPo4DSKW+AaweB/waP+gfWBmBb0xQM2jXZn0NftoJ0CDaBhN4rxkIN5MWM6avwPfc5g85CgL5qdQLsKtJ8w7WA0gAZHUIwJBvIi13RoNVUCY9D0CSYe+SuyDOgblte85LIpNqhlEtp2+2/sVQb0VMnZprKE4sKCmh5A417vFOs/A+dU8afKKNilHI1X3D9WDOgnbDoGUVKkofUuzsIif3jPwAVwUzyaRKDGi8p5cjsG4KgWRXeMoqTohuZLOAuf3TPQoQq/vikmqJWaR10DnTGKsQU1j6b0y0hT3Gos0ETtPAOtKvfLSFJBza00sC9uJRHQQgONAZV/wCfgV2Gnz/PUSc92NXhQmddtO/hnQ0DDY/nW4lKgvxEUI9J2eimetZADGmig2lCxWGjxUaCX4nzy1vmqqmjA9PT9VXxZYpg0VVqHVZICGvgOrvP9vz4GLflg0rRMA5xlan0VdbxxknIf6JvYf/0TSzTAcJ9pJppCQ5tp9JmebUxCzqLYL+zPnUZNWKABxirbDZWsk7yAu2CwNWiq5EPzpEWfQXhDkXy49BWw01JijgZmlXPUym09RM2zKYa4dZS4OW5FQsxTu/scYIi73AxQ88oC6T44pMrnreyHcjQ7BvhuicuIobv+OFUJMEHN3PAuJa6CDOYmPTLBlUN2XIiZEZ1cGI5DlQCDbkyI8C/mbign75XU6Nwr5WhcwSoDjDkyM4LNlyp58dFl8IQ/9ZS1nGb0l5kRbE6p5ESomTTpMqWcjO8DjMPDRK+yS3DUWNblQq8pN0AEvtAwIwITfFxLU0xtpqW4XhK3CfoheOTtUkwEGzIzomRJPprlGYyS5COY3Y+W5CP0C/UeJU+zSgV78RosTJqV0Dc210rFSnS74BKh8IluQnc4zOSCKsynBl5QeHE/NXChBxhgfF6F/9jDBE4Qpf/Yw4UecJSM83Obf43SFH5B5VNuAAAAAElFTkSuQmCC")
					};
					button3.SetValue(Grid.ColumnProperty, 1);
					Button button4 = new Button
					{
						MinWidth = 0.0,
						Width = 20.0,
						Height = 20.0,
						BorderThickness = new Thickness(0.0),
						BorderBrush = Brushes.Transparent,
						Cursor = Cursors.Hand,
						Padding = new Thickness(2.0),
						Background = Brushes.LightGray,
						Content = DD_ImageCreator.CreateImageFromCode("iVBORw0KGgoAAAANSUhEUgAAADAAAAAwCAYAAABXAvmHAAAACXBIWXMAAC4jAAAuIwF4pT92AAAErklEQVR4nMVaS4gVRxS9FwdBnIiOCIa4UzHr0UBmQLKa0ZW/hTpoVupi3CjGZBVXI0I0i4SESDLZRdGV342flQgq6Lj2M+6ERMRRyIRAIHTO6ep+vumpqldd1Z05cPu9ma66dU51vapbt7ovyzJJhuqHuH4G+xS2AbYONgDrL0rMwmZg07CnsAewO5Jlv6c23RddU3UFrvtgY7Ah/sdTeqAwCtsKOwLL4OM+Pi/AzkPM2xga9QWY3j4OOwT7IKbR0hNsuLBT8DuJz2/rPpVwAaqLcT0K+1rSiNtAf8eEnaJ6Ep/fQcg/IRXDBKjy0V+EbYxlGAgK+Qa2G23uhYjpXhV6C1DdhutvsGXJ9MLBjppC2/sh4rqvoF+AKsf5Tz3LtQN22CVwOAwRk65CbmKG/C8tEKsD8vsZXMQlwi7ADJuzLRKrA85WZ8HpFURcq96cL0B1Pa7nYIscDl/DOH8PwtY0RPIl7LGY9WSV5f6inJPqYPWHPVeAmSq5sLimyYfChSjLZlB2qXCMioymcZdbsF3w+Rd8crG7AfvEUo6cLqLMcPcUW30CXCFdU+WjnGyWvcv/Mg3uwLersJFI8rdhO+Dr78InO2a0+P8mS/mNBccz8wWYFfaEoyEOmy0d8iXYsOr2SBEkub1D/r3Pd/C5Bd+eiH04ncD9c+WK3f0EvhL30Lmf944NcSLs5N/7nCnipG2Wu+T4pZiVuxBgxt5BT4OD+ZjnsLE3WEeEn7zhw9+Xb9U3IQeElk+AUWW/pwJnm8s5SXevhYgIIb8kb0vkIw+f/oLzD6WAMU/hEiM5uXgRoeRDhyI5Q4DqajEbkRDEimiavOScwZ1PYLP4NyNV1BVBNE0+rwXbTAHDNSqVqCNCWiBfYpgCPo6oKBIqwoc08sQGClgbWVkkRIQL6eSJdRSwMsGBSIyIZsgTKynAN/+HIlxEc+SJ/oXYaTUKCmDSaSDRT+95vkRaAFjFLAW8kTQB4eRLNCfiDQW8gK2PdFCffIlmRExTAOPurRGVQ8MD93qQLuIpBdwTk3GrgzqxjTQQxbpwjwLu0o2Ex0MxgVlKFOsCOd/tQ+U/UJnp7qGWyIukh+I2PCD3ch1gJqKXgNSQuGkR5NzZE58XprjdqzLzNjsbiCpDRewUcxDi2pXNFpwLAWYT/au4f8yPnfvhcPIlQkQwZTPlETBZJhm6Q4nTsANiz0wM5Rt/W2YiLrbxizBJBteQ/lOseSHmWVQnxAipgvmZm7g/Mic3lBaY2UWoLs/bsueEiInuU5xqMPc9bI/YUxrMlN1CA9XUYkoowLpX4KuaWrRl5YipgmMHcwUw56g6VhS0DSXmLJ8USaemkrujhU9fcpfg0NlbPXqaH05n2fP8ZMT0ri1DzQZsGbMUrBF/Z/wL2287crLvB5iHVx2XhT/gILjijtvOBgj3hoYnIppHFzzocJ0VtA32/HjcERNhRDAzzUO+JraedcDF6nNwuOIr1HtLSQeqnJW4dA82w60n+IMeQ9vPehUM2xPTkSpniLYOuktwpmnhoJswDk9DCIdTE68adIPEW37VoIRp4IvilYDQlz2snsQcFv7PL3t0ms8b/DG3BXzd5j9AAhJkmvjHQAAAAABJRU5ErkJggg==")
					};
					button4.SetValue(Grid.ColumnProperty, 2);
					button3.Click += delegate(object s, RoutedEventArgs e)
					{
						if (string.IsNullOrWhiteSpace(textBoxRight.Text))
						{
							return;
						}
						try
						{
							string text = "+" + int.Parse(textBoxRight.Text.Trim()).ToString();
							if (!closure.capturedThis.ListIncrementNumbers.Contains(text))
							{
								closure.capturedThis.ListIncrementNumbers.Add(text);
							}
							closure.capturedThis.SortListBox(closure.capturedThis.listIncrement);
						}
						catch
						{
						}
					};
					button4.Click += delegate(object s, RoutedEventArgs e)
					{
						if (closure.capturedThis.listIncrement.SelectedItems == null || closure.capturedThis.listIncrement.SelectedItems.Count <= 0)
						{
							return;
						}
						closure.capturedThis.ListIncrementNumbers.Remove(closure.capturedThis.listIncrement.SelectedItem.ToString());
					};
					Grid grid2 = new Grid
					{
						Margin = new Thickness(5.0, 0.0, 0.0, grid.Margin.Bottom)
					};
					grid2.SetValue(Grid.ColumnProperty, 1);
					grid2.ColumnDefinitions.Add(new ColumnDefinition
					{
						Width = new GridLength(1.0, GridUnitType.Star)
					});
					grid2.ColumnDefinitions.Add(new ColumnDefinition
					{
						Width = default(GridLength)
					});
					grid2.ColumnDefinitions.Add(new ColumnDefinition
					{
						Width = default(GridLength)
					});
					grid2.Children.Add(textBoxRight);
					grid2.Children.Add(button3);
					grid2.Children.Add(button4);
					base.Children.Add(grid2);
					Button button5 = new Button
					{
						Content = "Cancel"
					};
					int num3 = (int)DDResources_GlobalConstantAndFunction.MeasureControlSize(button5, "").Width;
					button5.Width = (double)num3;
					Button button6 = new Button
					{
						Content = "OK",
						Width = (double)num3
					};
					Button button7 = new Button
					{
						Content = "Apply",
						Width = (double)num3
					};
					button5.Click += delegate(object s, RoutedEventArgs e)
					{
						if (closure.capturedThis.ButtonCancelClicked != null)
						{
							closure.capturedThis.ButtonCancelClicked(s, e);
						}
						closure.settingWindow.Close();
					};
					button6.Click += delegate(object s, RoutedEventArgs e)
					{
						if (closure.capturedThis.ButtonOKClicked != null)
						{
							closure.capturedThis.ButtonOKClicked(s, e);
						}
					};
					button7.Click += delegate(object s, RoutedEventArgs e)
					{
						if (closure.capturedThis.ButtonApplyClicked != null)
						{
							closure.capturedThis.ButtonApplyClicked(s, e);
						}
					};
					WrapPanel wrapPanel = new WrapPanel
					{
						HorizontalAlignment = HorizontalAlignment.Right
					};
					wrapPanel.SetValue(Grid.RowProperty, 2);
					wrapPanel.SetValue(Grid.ColumnSpanProperty, 2);
					wrapPanel.Children.Add(button6);
					wrapPanel.Children.Add(button5);
					wrapPanel.Children.Add(button7);
					base.Children.Add(wrapPanel);
				}
				[MethodImpl(MethodImplOptions.NoInlining)]
				private void SortListBox(ListBox listBox)
				{
					((ListCollectionView)CollectionViewSource.GetDefaultView(listBox.ItemsSource)).CustomSort = Comparer<object>.Create(delegate(object a, object b)
					{
						string text = ((a != null) ? a.ToString() : null);
						string text2 = ((b != null) ? b.ToString() : null);
						int num;
						bool flag = int.TryParse(text, NumberStyles.Any, CultureInfo.CurrentCulture, out num);
						int num2;
						bool flag2 = int.TryParse(text2, NumberStyles.Any, CultureInfo.CurrentCulture, out num2);
						if (flag && flag2)
						{
							return num.CompareTo(num2);
						}
						if (flag)
						{
							return -1;
						}
						if (flag2)
						{
							return 1;
						}
						return string.Compare(text, text2, StringComparison.CurrentCultureIgnoreCase);
					});
				}
				[MethodImpl(MethodImplOptions.NoInlining)]
				public void SaveSettings(DDNumericUpDown upDownControl)
				{
					DDNumericUpDown.NUDSettingInfo nudsettingInfo = new DDNumericUpDown.NUDSettingInfo();
					nudsettingInfo.Quick = new List<int>();
					nudsettingInfo.Increment = new List<int>();
					foreach (object obj in ((IEnumerable)this.listQuick.Items))
					{
						nudsettingInfo.Quick.Add(int.Parse(obj.ToString()));
					}
					foreach (object obj2 in ((IEnumerable)this.listIncrement.Items))
					{
						nudsettingInfo.Increment.Add(int.Parse(obj2.ToString()));
					}
					upDownControl.SettingInfo = nudsettingInfo;
					upDownControl.SaveNUDSettings();
				}
				public RoutedEventHandler ButtonOKClicked;
				public RoutedEventHandler ButtonCancelClicked;
				public RoutedEventHandler ButtonApplyClicked;
				private ListBox listQuick = new ListBox
				{
					Margin = new Thickness(0.0, 0.0, 5.0, 10.0)
				};
				private ListBox listIncrement = new ListBox
				{
					Margin = new Thickness(5.0, 0.0, 0.0, 10.0)
				};
			}
			private class GridPopup : Grid
			{
				[MethodImpl(MethodImplOptions.NoInlining)]
				public GridPopup(DDNumericUpDown upDownControl)
				{
					base.Background = Brushes.LightYellow;
					Border border = new Border
					{
						BorderBrush = Brushes.Black,
						BorderThickness = new Thickness(1.0)
					};
					border.SetValue(Grid.RowSpanProperty, 2);
					border.SetValue(Grid.ColumnSpanProperty, 4);
					base.Children.Add(border);
					Border border2 = new Border
					{
						BorderBrush = Brushes.Black,
						BorderThickness = new Thickness(1.0, 0.0, 0.0, 0.0),
						Margin = new Thickness(10.0, 5.0, 10.0, 5.0)
					};
					border2.SetValue(Grid.ColumnProperty, 2);
					base.Children.Add(border2);
					base.ColumnDefinitions.Add(new ColumnDefinition());
					base.ColumnDefinitions.Add(new ColumnDefinition());
					base.ColumnDefinitions.Add(new ColumnDefinition());
					base.ColumnDefinitions.Add(new ColumnDefinition());
					base.RowDefinitions.Add(new RowDefinition());
					base.RowDefinitions.Add(new RowDefinition());
					DDNumericUpDown.NUDSettingInfo settingInfo = upDownControl.SettingInfo;
					this.stackPanel1.SetValue(Grid.ColumnProperty, 0);
					this.stackPanel2.SetValue(Grid.ColumnProperty, 1);
					this.stackPanel3.SetValue(Grid.ColumnProperty, 2);
					this.stackPanel4.SetValue(Grid.ColumnProperty, 3);
					this.stackPanel3.Margin = new Thickness(2.0 * border2.Margin.Left, this.stackPanel1.Margin.Top, this.stackPanel1.Margin.Right, this.stackPanel1.Margin.Bottom);
					if (settingInfo != null)
					{
						this.LoadSettings(settingInfo);
					}
					WrapPanel wrapPanel = new WrapPanel
					{
						HorizontalAlignment = HorizontalAlignment.Right
					};
					wrapPanel.SetValue(Grid.RowProperty, 1);
					wrapPanel.SetValue(Grid.ColumnSpanProperty, 4);
					Label label = new Label
					{
						Content = "custom",
						HorizontalContentAlignment = HorizontalAlignment.Center,
						Cursor = Cursors.Hand
					};
					label.Width = DDResources_GlobalConstantAndFunction.MeasureControlSize(label, "").Width + 10.0;
					label.MouseEnter += this.OnLabel_MouseEnter;
					label.MouseLeave += this.OnLabel_MouseLeave;
					Label label2 = new Label
					{
						Content = "clear",
						HorizontalContentAlignment = HorizontalAlignment.Center,
						Cursor = Cursors.Hand
					};
					label2.Width = DDResources_GlobalConstantAndFunction.MeasureControlSize(label2, "").Width + 10.0;
					label2.MouseEnter += this.OnLabel_MouseEnter;
					label2.MouseLeave += this.OnLabel_MouseLeave;
					Label label3 = new Label
					{
						Content = "close",
						HorizontalContentAlignment = HorizontalAlignment.Center,
						Cursor = Cursors.Hand
					};
					label3.Width = DDResources_GlobalConstantAndFunction.MeasureControlSize(label3, "").Width + 10.0;
					label3.MouseEnter += this.OnLabel_MouseEnter;
					label3.MouseLeave += this.OnLabel_MouseLeave;
					label.MouseLeftButtonUp += delegate(object s, MouseButtonEventArgs e)
					{
						if (this.LabelCustomClicked != null)
						{
							this.LabelCustomClicked(s, e);
						}
						this.ClosePopup();
					};
					label2.MouseLeftButtonUp += delegate(object s, MouseButtonEventArgs e)
					{
						if (this.LabelClearClicked != null)
						{
							this.LabelClearClicked(s, e);
						}
					};
					label3.MouseLeftButtonUp += delegate(object s, MouseButtonEventArgs e)
					{
						if (this.LabelCloseClicked != null)
						{
							this.LabelCloseClicked(s, e);
						}
						this.ClosePopup();
					};
					wrapPanel.Children.Add(label);
					wrapPanel.Children.Add(label2);
					wrapPanel.Children.Add(label3);
					base.Children.Add(this.stackPanel1);
					base.Children.Add(this.stackPanel2);
					base.Children.Add(this.stackPanel3);
					base.Children.Add(this.stackPanel4);
					base.Children.Add(wrapPanel);
				}
				[MethodImpl(MethodImplOptions.NoInlining)]
				public void LoadSettings(DDNumericUpDown.NUDSettingInfo settingInfo)
				{
					this.stackPanel1.Children.Clear();
					this.stackPanel2.Children.Clear();
					this.stackPanel3.Children.Clear();
					this.stackPanel4.Children.Clear();
					int num = -1;
					int num2 = -1;
					int num3 = -1;
					int num4 = -1;
					Thickness thickness = new Thickness(3.0, 1.0, 3.0, 1.0);
					Thickness thickness2 = new Thickness(3.0, 1.0, 3.0, 1.0);
					for (int i = 0; i < settingInfo.Quick.Count; i++)
					{
						string text = settingInfo.Quick[i].ToString();
						if (i % 2 == 0)
						{
							Label label = new Label
							{
								Content = text,
								Tag = text,
								HorizontalContentAlignment = HorizontalAlignment.Center,
								Cursor = Cursors.Hand,
								Padding = thickness,
								Margin = thickness2
							};
							num = (int)Math.Max((double)num, DDResources_GlobalConstantAndFunction.MeasureControlSize(label, "").Width + 3.0 * label.Padding.Left);
							label.Width = (double)num;
							label.MouseEnter += this.OnLabel_MouseEnter;
							label.MouseLeave += this.OnLabel_MouseLeave;
							label.MouseLeftButtonUp += delegate(object s, MouseButtonEventArgs e)
							{
								if (this.QuickAndIncrementClicked != null)
								{
									this.QuickAndIncrementClicked(s, e);
								}
								this.ClosePopup();
							};
							this.stackPanel1.Children.Add(label);
						}
						else
						{
							Label label2 = new Label
							{
								Content = text,
								Tag = text,
								HorizontalContentAlignment = HorizontalAlignment.Center,
								Cursor = Cursors.Hand,
								Padding = thickness,
								Margin = thickness2
							};
							num2 = (int)Math.Max((double)num2, DDResources_GlobalConstantAndFunction.MeasureControlSize(label2, "").Width + 3.0 * label2.Padding.Left);
							label2.Width = (double)num2;
							label2.MouseEnter += this.OnLabel_MouseEnter;
							label2.MouseLeave += this.OnLabel_MouseLeave;
							label2.MouseLeftButtonUp += delegate(object s, MouseButtonEventArgs e)
							{
								if (this.QuickAndIncrementClicked != null)
								{
									this.QuickAndIncrementClicked(s, e);
								}
								this.ClosePopup();
							};
							this.stackPanel2.Children.Add(label2);
						}
					}
					for (int j = 0; j < settingInfo.Increment.Count; j++)
					{
						string text2 = "+" + settingInfo.Increment[j].ToString();
						if (j % 2 == 0)
						{
							Label label3 = new Label
							{
								Content = text2,
								Tag = text2,
								HorizontalContentAlignment = HorizontalAlignment.Center,
								Cursor = Cursors.Hand,
								Padding = thickness,
								Margin = thickness2
							};
							num3 = (int)Math.Max((double)num3, DDResources_GlobalConstantAndFunction.MeasureControlSize(label3, "").Width + 3.0 * label3.Padding.Left);
							label3.Width = (double)num3;
							label3.MouseEnter += this.OnLabel_MouseEnter;
							label3.MouseLeave += this.OnLabel_MouseLeave;
							label3.MouseLeftButtonUp += delegate(object s, MouseButtonEventArgs e)
							{
								if (this.QuickAndIncrementClicked != null)
								{
									this.QuickAndIncrementClicked(s, e);
								}
								this.ClosePopup();
							};
							this.stackPanel3.Children.Add(label3);
						}
						else
						{
							Label label4 = new Label
							{
								Content = text2,
								Tag = text2,
								HorizontalContentAlignment = HorizontalAlignment.Center,
								Cursor = Cursors.Hand,
								Padding = thickness,
								Margin = thickness2
							};
							num4 = (int)Math.Max((double)num4, DDResources_GlobalConstantAndFunction.MeasureControlSize(label4, "").Width + 3.0 * label4.Padding.Left);
							label4.Width = (double)num4;
							label4.MouseEnter += this.OnLabel_MouseEnter;
							label4.MouseLeave += this.OnLabel_MouseLeave;
							label4.MouseLeftButtonUp += delegate(object s, MouseButtonEventArgs e)
							{
								if (this.QuickAndIncrementClicked != null)
								{
									this.QuickAndIncrementClicked(s, e);
								}
								this.ClosePopup();
							};
							this.stackPanel4.Children.Add(label4);
						}
					}
				}
				[MethodImpl(MethodImplOptions.NoInlining)]
				private void ClosePopup()
				{
					Popup popup = base.Parent as Popup;
					if (popup != null)
					{
						popup.IsOpen = false;
					}
				}
				[MethodImpl(MethodImplOptions.NoInlining)]
				private void OnLabel_MouseLeave(object sender, MouseEventArgs e)
				{
					Label label = sender as Label;
					if (label == null)
					{
						return;
					}
					label.FontWeight = FontWeights.Normal;
					label.FontStyle = FontStyles.Normal;
				}
				[MethodImpl(MethodImplOptions.NoInlining)]
				private void OnLabel_MouseEnter(object sender, MouseEventArgs e)
				{
					Label label = sender as Label;
					if (label == null)
					{
						return;
					}
					label.FontWeight = FontWeights.Bold;
					label.FontStyle = FontStyles.Italic;
				}
				public MouseButtonEventHandler LabelCustomClicked;
				public MouseButtonEventHandler LabelClearClicked;
				public MouseButtonEventHandler LabelCloseClicked;
				public MouseButtonEventHandler QuickAndIncrementClicked;
				private StackPanel stackPanel1 = new StackPanel();
				private StackPanel stackPanel2 = new StackPanel();
				private StackPanel stackPanel3 = new StackPanel();
				private StackPanel stackPanel4 = new StackPanel();
			}
		}
		private class ParamInfo
		{
			public string Name { get; set; }
			public string Value { get; set; }
		}
		private class IndicatorConfig
		{
			public string Name { get; set; }
			public string DisplayName { get; set; }
			public string Namespace { get; set; }
			public int PlotIndex { get; set; }
			public double ValueBullish { get; set; }
			public double ValueBearish { get; set; }
			public int OperatorBullish { get; set; }
			public int OperatorBearish { get; set; }
			public string TimeframeConfigStr { get; set; }
			public List<int> ListBarInProgressSelected { get; set; }
		}
		private class IndicatorItem : Grid
		{
			public string DisplayName
			{
				[MethodImpl(MethodImplOptions.NoInlining)]
				get
				{
					if (this.tblIndicatorName != null)
					{
						return this.tblIndicatorName.Text;
					}
					return string.Empty;
				}
				set
				{
					this.tblIndicatorName.Text = value;
				}
			}
			public string IndicatorName { get; set; }
			public global::System.Windows.Media.Brush Foreground
			{
				get
				{
					return this.tblIndicatorName.Foreground;
				}
				[MethodImpl(MethodImplOptions.NoInlining)]
				set
				{
					if (value != null)
					{
						this.tblIndicatorName.Foreground = value;
					}
				}
			}
			[MethodImpl(MethodImplOptions.NoInlining)]
			public IndicatorItem(string indicatorName, global::System.Windows.Media.Brush foreground)
			{
				this.tblIndicatorName = new TextBlock
				{
					VerticalAlignment = VerticalAlignment.Center,
					TextTrimming = TextTrimming.CharacterEllipsis
				};
				this.DisplayName = indicatorName;
				this.IndicatorName = indicatorName;
				base.Margin = new Thickness(-1.0, 0.0, -5.0, 0.0);
				this.Foreground = foreground;
				base.Children.Add(this.tblIndicatorName);
			}
			private TextBlock tblIndicatorName;
		}
		private class IndicatorsListPage : UserControl
		{
			[MethodImpl(MethodImplOptions.NoInlining)]
			public IndicatorsListPage(SortedList<string, DDDeepStackConfluence.IndicatorItem> sortedListIndicatorItem, bool isLightTheme, global::System.Windows.Media.Brush foreground, global::System.Windows.Media.Brush borderBrush = null)
			{
				base.Foreground = foreground;
				base.BorderBrush = borderBrush;
				this.sortedListIndicatorItems = sortedListIndicatorItem;
				Label label = new Label
				{
					Content = "Indicators",
					Background = (isLightTheme ? Brushes.White : Brushes.Black),
					Foreground = foreground,
					HorizontalAlignment = HorizontalAlignment.Stretch,
					VerticalAlignment = VerticalAlignment.Center,
					BorderBrush = (borderBrush ?? foreground),
					BorderThickness = new Thickness(0.0, 0.0, 0.0, 1.0)
				};
				this.DDSearchBarControl = new DDSearchBar(foreground, null, 0, 0, "Start typing to search")
				{
					Margin = new Thickness(5.0)
				};
				this.DDSearchBarControl.SetValue(Grid.RowProperty, 1);
				this.DDSearchBarControl.FontSize = 12.0;
				Thickness thickness = new Thickness(0.0, 2.0, 0.0, 0.0);
				Thickness thickness2 = new Thickness(0.0);
				this.listBoxIndicatorsMain = new ListBox
				{
					Foreground = foreground,
					Margin = thickness,
					BorderThickness = thickness2
				};
				this.listBoxIndicatorsMain.SetValue(ScrollViewer.HorizontalScrollBarVisibilityProperty, ScrollBarVisibility.Disabled);
				this.listBoxIndicatorsMain.SetValue(Control.HorizontalContentAlignmentProperty, HorizontalAlignment.Stretch);
				this.listBoxIndicatorsMain.SetValue(Grid.RowProperty, 2);
				ScrollViewer.SetVerticalScrollBarVisibility(this.listBoxIndicatorsMain, ScrollBarVisibility.Auto);
				this.listBoxIndicatorsMain.ItemsSource = this.sortedListIndicatorItems.Values;
				this.listBoxIndicatorsResult = new ListBox
				{
					Foreground = foreground,
					Margin = thickness,
					BorderThickness = thickness2
				};
				this.listBoxIndicatorsResult.SetValue(ScrollViewer.HorizontalScrollBarVisibilityProperty, ScrollBarVisibility.Disabled);
				this.listBoxIndicatorsResult.SetValue(Control.HorizontalContentAlignmentProperty, HorizontalAlignment.Stretch);
				this.listBoxIndicatorsResult.SetValue(Grid.RowProperty, 2);
				ScrollViewer.SetVerticalScrollBarVisibility(this.listBoxIndicatorsResult, ScrollBarVisibility.Auto);
				this.listBoxIndicatorsResult.Visibility = Visibility.Hidden;
				Grid grid = new Grid();
				grid.ColumnDefinitions.Add(new ColumnDefinition());
				grid.RowDefinitions.Add(new RowDefinition
				{
					Height = default(GridLength)
				});
				grid.RowDefinitions.Add(new RowDefinition
				{
					Height = default(GridLength)
				});
				grid.RowDefinitions.Add(new RowDefinition
				{
					Height = new GridLength(2.0, GridUnitType.Star)
				});
				grid.Children.Add(label);
				grid.Children.Add(this.DDSearchBarControl);
				grid.Children.Add(this.listBoxIndicatorsMain);
				grid.Children.Add(this.listBoxIndicatorsResult);
				UserControl userControl = new UserControl
				{
					BorderBrush = (borderBrush ?? foreground),
					BorderThickness = new Thickness(1.0),
					Margin = new Thickness(0.0, 0.0, 0.0, 8.0)
				};
				userControl.Content = grid;
				Label label2 = new Label
				{
					Content = "Configured",
					Background = (isLightTheme ? Brushes.White : Brushes.Black),
					Foreground = foreground,
					HorizontalAlignment = HorizontalAlignment.Stretch,
					VerticalAlignment = VerticalAlignment.Center,
					BorderBrush = (borderBrush ?? foreground),
					BorderThickness = new Thickness(1.0, 1.0, 1.0, 0.0)
				};
				label2.SetValue(Grid.RowProperty, 0);
				this.listBoxIndicatorsConfig = new ListBox
				{
					Foreground = foreground,
					BorderBrush = (borderBrush ?? foreground),
					BorderThickness = new Thickness(1.0, 1.0, 1.0, 0.0)
				};
				this.listBoxIndicatorsConfig.SetValue(ScrollViewer.HorizontalScrollBarVisibilityProperty, ScrollBarVisibility.Disabled);
				this.listBoxIndicatorsConfig.SetValue(Control.HorizontalContentAlignmentProperty, HorizontalAlignment.Stretch);
				this.listBoxIndicatorsConfig.SetValue(Grid.RowProperty, 1);
				ScrollViewer.SetVerticalScrollBarVisibility(this.listBoxIndicatorsConfig, ScrollBarVisibility.Auto);
				Thickness thickness3 = new Thickness(3.0, 5.0, 3.0, 5.0);
				WrapPanel wrapPanel = new WrapPanel
				{
					HorizontalAlignment = HorizontalAlignment.Right,
					Margin = new Thickness(0.0, 0.0, 2.0, 0.0)
				};
				this.lblAdd = new Label
				{
					Content = "add",
					Foreground = Brushes.Gray,
					FontStyle = FontStyles.Italic,
					IsEnabled = false,
					Padding = thickness3
				};
				this.lblAdd.Width = DDResources_GlobalConstantAndFunction.MeasureControlSize(this.lblAdd, "").Width + this.lblAdd.Padding.Left;
				wrapPanel.Children.Add(this.lblAdd);
				this.lblRemove = new Label
				{
					Content = "remove",
					Foreground = Brushes.Gray,
					FontStyle = FontStyles.Italic,
					IsEnabled = false,
					Padding = thickness3
				};
				this.lblRemove.Width = DDResources_GlobalConstantAndFunction.MeasureControlSize(this.lblRemove, "").Width + 2.0 * this.lblRemove.Padding.Left;
				wrapPanel.Children.Add(this.lblRemove);
				this.lblUp = new Label
				{
					Content = "up",
					Foreground = Brushes.Gray,
					FontStyle = FontStyles.Italic,
					Cursor = Cursors.Hand,
					IsEnabled = false,
					Padding = thickness3
				};
				this.lblUp.Width = DDResources_GlobalConstantAndFunction.MeasureControlSize(this.lblUp, "").Width + this.lblUp.Padding.Left;
				wrapPanel.Children.Add(this.lblUp);
				this.lblDown = new Label
				{
					Content = "down",
					Foreground = Brushes.Gray,
					FontStyle = FontStyles.Italic,
					Cursor = Cursors.Hand,
					IsEnabled = false,
					Padding = thickness3
				};
				this.lblDown.Width = DDResources_GlobalConstantAndFunction.MeasureControlSize(this.lblDown, "").Width + this.lblDown.Padding.Left;
				wrapPanel.Children.Add(this.lblDown);
				this.lblAdd.MouseEnter += this.Label_MouseEnter_TextBold;
				this.lblAdd.MouseLeave += this.Label_MouseLeave_TextRegular;
				this.lblRemove.MouseEnter += this.Label_MouseEnter_TextBold;
				this.lblRemove.MouseLeave += this.Label_MouseLeave_TextRegular;
				this.lblUp.MouseEnter += this.Label_MouseEnter_TextBold;
				this.lblUp.MouseLeave += this.Label_MouseLeave_TextRegular;
				this.lblDown.MouseEnter += this.Label_MouseEnter_TextBold;
				this.lblDown.MouseLeave += this.Label_MouseLeave_TextRegular;
				Border border = new Border
				{
					BorderBrush = (borderBrush ?? foreground),
					BorderThickness = new Thickness(1.0, 0.0, 1.0, 1.0)
				};
				border.Child = wrapPanel;
				border.SetValue(Grid.RowProperty, 2);
				Grid grid2 = new Grid();
				grid2.ColumnDefinitions.Add(new ColumnDefinition());
				grid2.RowDefinitions.Add(new RowDefinition
				{
					Height = default(GridLength)
				});
				grid2.RowDefinitions.Add(new RowDefinition
				{
					Height = new GridLength(2.0, GridUnitType.Star)
				});
				grid2.RowDefinitions.Add(new RowDefinition
				{
					Height = default(GridLength)
				});
				grid2.Children.Add(label2);
				grid2.Children.Add(this.listBoxIndicatorsConfig);
				grid2.Children.Add(border);
				grid2.SetValue(Grid.RowProperty, 1);
				base.Content = new Grid
				{
					ColumnDefinitions = 
					{
						new ColumnDefinition()
					},
					RowDefinitions = 
					{
						new RowDefinition
						{
							Height = new GridLength(2.0, GridUnitType.Star)
						},
						new RowDefinition
						{
							Height = new GridLength(1.0, GridUnitType.Star)
						}
					},
					Children = { userControl, grid2 }
				};
			}
			[MethodImpl(MethodImplOptions.NoInlining)]
			private void Label_MouseLeave_TextRegular(object sender, MouseEventArgs e)
			{
				Label lbl = sender as Label;
				if (lbl == null)
				{
					return;
				}
				lbl.Dispatcher.InvokeAsync(delegate
				{
					lbl.FontWeight = FontWeights.Regular;
				});
			}
			[MethodImpl(MethodImplOptions.NoInlining)]
			private void Label_MouseEnter_TextBold(object sender, MouseEventArgs e)
			{
				Label lbl = sender as Label;
				if (lbl == null)
				{
					return;
				}
				lbl.Dispatcher.InvokeAsync(delegate
				{
					lbl.FontWeight = FontWeights.DemiBold;
				});
			}
			[MethodImpl(MethodImplOptions.NoInlining)]
			public void FindIndicatorByName(string nameSearch = null)
			{
				if (string.IsNullOrWhiteSpace(nameSearch))
				{
					this.listBoxIndicatorsResult.Visibility = Visibility.Hidden;
					this.listBoxIndicatorsMain.Visibility = Visibility.Visible;
					return;
				}
				this.listBoxIndicatorsResult.Items.Clear();
				this.listBoxIndicatorsMain.Visibility = Visibility.Hidden;
				this.listBoxIndicatorsResult.Visibility = Visibility.Visible;
				foreach (string text in this.sortedListIndicatorItems.Keys)
				{
					if (text.ToUpper().Contains(nameSearch.ToUpper()))
					{
						this.listBoxIndicatorsResult.Items.Add(new DDDeepStackConfluence.IndicatorItem(text, base.Foreground));
					}
				}
			}
			public ListBox listBoxIndicatorsMain;
			public ListBox listBoxIndicatorsResult;
			public ListBox listBoxIndicatorsConfig;
			public Label lblAdd;
			public Label lblRemove;
			public Label lblUp;
			public Label lblDown;
			private SortedList<string, DDDeepStackConfluence.IndicatorItem> sortedListIndicatorItems;
			public DDSearchBar DDSearchBarControl;
		}
		private class IndicatorPropertiesPage : Grid
		{
			private sealed class IndicatorPropsClosure
			{
				public IndicatorPropertiesPage capturedThis;
				public DDDeepStackConfluence.IndicatorConfig indicatorConfig;
				public ComboBox cmbTimeframe;
			}
			private sealed class IndicatorPropsParamClosure
			{
				public IndicatorPropsClosure closure;
				public DDDeepStackConfluence.ParamInfo paramInfo;
			}
			public global::System.Windows.Media.Brush Foreground { get; set; }
			public string DocumentsPath { get; set; }
			private Dictionary<string, MethodInfo> DictMethodInfo { get; set; }
			private string Instrument { get; set; }
			private string BarsPeriod { get; set; }
			private List<string> ListOperator { get; set; }
			public List<List<DDDeepStackConfluence.ParamInfo>> ListOfListParamInfo { get; set; }
			public List<DDDeepStackConfluence.IndicatorConfig> ListIndicatorConfig { get; set; }
			public bool IsParamChanged { get; set; }
			public StackPanel StackPanelIndicatorProps { get; set; }
			private List<string> ListTimeframeStr { get; set; }
			[MethodImpl(MethodImplOptions.NoInlining)]
			public IndicatorPropertiesPage(Dictionary<string, MethodInfo> dictMethodInfo, List<string> listTimeframeStr, List<string> listOperator, string documentsPath, string instrument, string barsPeriod, bool isLightTheme, global::System.Windows.Media.Brush foreground, global::System.Windows.Media.Brush borderBrush = null)
			{
				this.DocumentsPath = documentsPath;
				this.Instrument = instrument;
				this.BarsPeriod = barsPeriod;
				this.ListOperator = listOperator;
				this.ListTimeframeStr = listTimeframeStr;
				this.Foreground = foreground;
				this.DictMethodInfo = dictMethodInfo;
				Label label = new Label
				{
					Content = "Properties",
					Background = (isLightTheme ? Brushes.White : Brushes.Black),
					Foreground = foreground,
					HorizontalAlignment = HorizontalAlignment.Stretch,
					VerticalAlignment = VerticalAlignment.Center,
					BorderBrush = (borderBrush ?? foreground),
					BorderThickness = new Thickness(1.0)
				};
				label.SetValue(Grid.RowProperty, 0);
				base.Children.Add(label);
				this.StackPanelIndicatorProps = new StackPanel
				{
					Margin = new Thickness(0.0, 5.0, 0.0, 5.0)
				};
				ScrollViewer scrollViewer = new ScrollViewer
				{
					VerticalScrollBarVisibility = ScrollBarVisibility.Auto
				};
				scrollViewer.Content = this.StackPanelIndicatorProps;
				scrollViewer.SetValue(Grid.RowProperty, 1);
				base.Children.Add(scrollViewer);
				Border border = new Border
				{
					BorderBrush = (borderBrush ?? foreground),
					BorderThickness = new Thickness(1.0)
				};
				border.SetValue(Grid.RowSpanProperty, 2);
				base.RowDefinitions.Add(new RowDefinition
				{
					Height = default(GridLength)
				});
				base.RowDefinitions.Add(new RowDefinition
				{
					Height = new GridLength(1.0, GridUnitType.Star)
				});
				base.Children.Add(border);
			}
			[MethodImpl(MethodImplOptions.NoInlining)]
			public void RenderIndicatorPropsPage(DDDeepStackConfluence.IndicatorItem indicatorItem, bool propsPageEnabled = false, bool addedToConfigListBox = false, int indicatorItemIndex = -1)
			{
				DDDeepStackConfluence.IndicatorPropertiesPage.IndicatorPropsClosure closure = new DDDeepStackConfluence.IndicatorPropertiesPage.IndicatorPropsClosure();
				closure.capturedThis = this;
				try
				{
					if (this.StackPanelIndicatorProps != null)
					{
						this.StackPanelIndicatorProps.Children.Clear();
					}
					string text = (propsPageEnabled ? indicatorItem.IndicatorName : indicatorItem.DisplayName);
					if (this.DictMethodInfo.ContainsKey(text))
					{
						MethodInfo methodInfo = this.DictMethodInfo[text];
						if (!(methodInfo == null))
						{
							if (!(methodInfo.ReturnType == null))
							{
								string fullName = methodInfo.ReturnType.FullName;
								if (!string.IsNullOrWhiteSpace(fullName))
								{
									Type type = null;
									foreach (string text2 in Directory.GetFiles(this.DocumentsPath, "*.dll", SearchOption.TopDirectoryOnly))
									{
										try
										{
											Assembly assembly = Assembly.LoadFrom(text2);
											if (!(assembly == null))
											{
												type = Type.GetType(string.Format("{0}, {1}", fullName, assembly.FullName));
												if (type != null)
												{
													break;
												}
											}
										}
										catch
										{
										}
									}
									if (!(type == null))
									{
										IndicatorBase indicatorBase = (IndicatorBase)Activator.CreateInstance(type);
										if (indicatorBase != null)
										{
											Type type2 = indicatorBase.GetType();
											if (!(type2 == null))
											{
												List<DDDeepStackConfluence.ParamInfo> list = null;
												closure.indicatorConfig = null;
												if (propsPageEnabled || addedToConfigListBox)
												{
													string text3 = indicatorBase.DisplayName;
													if (!text3.Contains("instrument"))
													{
														if (text3.Contains("("))
														{
															int num = text3.IndexOf("(");
															text3 = text3.Insert(num + 1, "instrument (period),");
														}
														else
														{
															text3 += " (instrument (period))";
														}
													}
													if (propsPageEnabled)
													{
														if (addedToConfigListBox)
														{
															list = new List<DDDeepStackConfluence.ParamInfo>();
															closure.indicatorConfig = new DDDeepStackConfluence.IndicatorConfig
															{
																Name = text,
																DisplayName = text3,
																Namespace = type2.Namespace + ".",
																TimeframeConfigStr = (this.ListTimeframeStr[0] ?? "")
															};
														}
														else
														{
															closure.indicatorConfig = this.ListIndicatorConfig[indicatorItemIndex];
															list = this.ListOfListParamInfo[indicatorItemIndex];
														}
													}
													if (text3.Contains("instrument"))
													{
														text3 = text3.Replace("instrument", this.Instrument);
													}
													if (text3.Contains("period"))
													{
														text3 = text3.Replace("period", this.BarsPeriod);
													}
													indicatorItem.DisplayName = text3;
												}
												PropertyInfo[] properties = type2.GetProperties();
												if (properties != null)
												{
													ParameterInfo[] parameters = methodInfo.GetParameters();
													if (parameters != null)
													{
														global::System.Windows.Media.Brush brush = (propsPageEnabled ? this.Foreground : Brushes.Gray);
														for (int j = 0; j < parameters.Length; j++)
														{
															try
															{
																ParameterInfo parameterInfo = parameters[j];
																if (parameterInfo != null)
																{
																	string text4 = parameterInfo.Name.ToLower();
																	foreach (PropertyInfo propertyInfo in properties)
																	{
																		try
																		{
																			if (!(propertyInfo == null))
																			{
																				string name = propertyInfo.Name;
																				if (name.ToLower() == text4)
																				{
																					DDDeepStackConfluence.IndicatorPropertiesPage.IndicatorPropsParamClosure paramClosure = new DDDeepStackConfluence.IndicatorPropertiesPage.IndicatorPropsParamClosure();
																					paramClosure.closure = closure;
																					Grid grid = new Grid
																					{
																						Margin = new Thickness(6.0, 0.0, 5.0, 3.0)
																					};
																					grid.ColumnDefinitions.Add(new ColumnDefinition
																					{
																						Width = new GridLength(1.0, GridUnitType.Star),
																						MinWidth = 50.0
																					});
																					grid.ColumnDefinitions.Add(new ColumnDefinition
																					{
																						Width = new GridLength(1.0, GridUnitType.Star),
																						MinWidth = 50.0
																					});
																					TextBlock textBlock = new TextBlock
																					{
																						Foreground = brush,
																						VerticalAlignment = VerticalAlignment.Center,
																						HorizontalAlignment = HorizontalAlignment.Left,
																						TextTrimming = TextTrimming.CharacterEllipsis,
																						Margin = new Thickness(0.0, 0.0, 10.0, 0.0)
																					};
																					textBlock.SetValue(Grid.ColumnProperty, 0);
																					string text5 = name;
																					foreach (Attribute attribute in propertyInfo.GetCustomAttributes())
																					{
																						if (attribute != null && attribute is DisplayAttribute)
																						{
																							DisplayAttribute displayAttribute = attribute as DisplayAttribute;
																							if (displayAttribute != null)
																							{
																								text5 = displayAttribute.Name;
																								break;
																							}
																						}
																					}
																					textBlock.Text = text5;
																					grid.Children.Add(textBlock);
																					paramClosure.paramInfo = null;
																					if (propsPageEnabled)
																					{
																						DDDeepStackConfluence.IndicatorPropertiesPage.IndicatorPropsParamClosure paramClosureRef = paramClosure;
																						DDDeepStackConfluence.ParamInfo paramInfo;
																						if (!addedToConfigListBox)
																						{
																							paramInfo = list[j];
																						}
																						else
																						{
																							(paramInfo = new DDDeepStackConfluence.ParamInfo()).Name = name;
																						}
																						paramClosureRef.paramInfo = paramInfo;
																					}
																					Type parameterType = parameterInfo.ParameterType;
																					if (!(parameterType == null))
																					{
																						bool flag = parameterType == typeof(ISeries<double>);
																						if (parameterType.IsEnum || flag)
																						{
																							ComboBox comboBox = new ComboBox
																							{
																								Foreground = brush,
																								MinHeight = 20.0,
																								VerticalContentAlignment = VerticalAlignment.Center,
																								ToolTip = string.Empty
																							};
																							comboBox.SetValue(Grid.ColumnProperty, 1);
																							if (propsPageEnabled)
																							{
																								comboBox.SelectionChanged += delegate(object sender, SelectionChangedEventArgs e)
																								{
																									ComboBox comboBox3 = sender as ComboBox;
																									paramClosure.paramInfo.Value = comboBox3.SelectedIndex.ToString();
																									paramClosure.closure.capturedThis.IsParamChanged = true;
																								};
																							}
																							comboBox.ItemsSource = Enum.GetValues(flag ? typeof(PriceType) : parameterType);
																							int num2;
																							if (propsPageEnabled && !addedToConfigListBox)
																							{
																								num2 = this.GetEnumIndex(parameterType, paramClosure.paramInfo.Value);
																							}
																							else
																							{
																								num2 = this.GetEnumIndex(parameterType, (flag ? 0 : ((int)propertyInfo.GetValue(indicatorBase))).ToString());
																							}
																							if (num2 >= 0)
																							{
																								comboBox.SelectedIndex = num2;
																							}
																							grid.Children.Add(comboBox);
																						}
																						else if (parameterType == typeof(bool))
																						{
																							CheckBox checkBox = new CheckBox
																							{
																								Foreground = brush
																							};
																							checkBox.SetValue(Grid.ColumnProperty, 1);
																							grid.Children.Add(checkBox);
																							if (propsPageEnabled && !addedToConfigListBox)
																							{
																								checkBox.IsChecked = new bool?(bool.Parse(paramClosure.paramInfo.Value));
																							}
																							else
																							{
																								string text6 = propertyInfo.GetValue(indicatorBase).ToString();
																								checkBox.IsChecked = new bool?(bool.Parse(text6));
																								if (addedToConfigListBox)
																								{
																									paramClosure.paramInfo.Value = text6;
																								}
																							}
																							if (propsPageEnabled)
																							{
																								checkBox.Checked += delegate(object sender, RoutedEventArgs e)
																								{
																									paramClosure.paramInfo.Value = "true";
																									paramClosure.closure.capturedThis.IsParamChanged = true;
																								};
																								checkBox.Unchecked += delegate(object sender, RoutedEventArgs e)
																								{
																									paramClosure.paramInfo.Value = "false";
																									paramClosure.closure.capturedThis.IsParamChanged = true;
																								};
																							}
																						}
																						else
																						{
																							TextBox tbPropValue = new TextBox
																							{
																								VerticalContentAlignment = VerticalAlignment.Center,
																								Foreground = brush,
																								MinHeight = 20.0
																							};
																							tbPropValue.SetValue(Grid.ColumnProperty, 1);
																							if (propsPageEnabled)
																							{
																								string oldText = null;
																								bool conditionIncludeDot = parameterType == typeof(double) || parameterType == typeof(float) || parameterType == typeof(long);
																								tbPropValue.TextChanged += delegate(object sender, TextChangedEventArgs e)
																								{
																									TextBox txtBox = sender as TextBox;
																									tbPropValue.Dispatcher.InvokeAsync(delegate
																									{
																										NinjaTrader.NinjaScript.Indicators.DimDim.DDDeepStackConfluence.NumericTextBox_TextChangedHandler(txtBox, ref oldText, true, conditionIncludeDot);
																									});
																									paramClosure.paramInfo.Value = (string.IsNullOrWhiteSpace(txtBox.Text) ? paramClosure.paramInfo.Value : txtBox.Text);
																									paramClosure.closure.capturedThis.IsParamChanged = true;
																								};
																								Action cachedKeyDownAction = null;
																								tbPropValue.PreviewKeyDown += delegate(object sender, KeyEventArgs e)
																								{
																									Dispatcher dispatcher = tbPropValue.Dispatcher;
																									Action action;
																									if ((action = cachedKeyDownAction) == null)
																									{
																										action = (cachedKeyDownAction = delegate
																										{
																											NinjaTrader.NinjaScript.Indicators.DimDim.DDDeepStackConfluence.NumericTextBox_PreviewKeyDownHandler(tbPropValue, ref oldText);
																										});
																									}
																									dispatcher.InvokeAsync(action);
																								};
																								Action cachedLostFocusAction = null;
																								tbPropValue.LostFocus += delegate(object sender, RoutedEventArgs e)
																								{
																									Dispatcher dispatcher2 = tbPropValue.Dispatcher;
																									Action action2;
																									if ((action2 = cachedLostFocusAction) == null)
																									{
																										action2 = (cachedLostFocusAction = delegate
																										{
																											NinjaTrader.NinjaScript.Indicators.DimDim.DDDeepStackConfluence.NumericTextBox_LostFocusHandler(tbPropValue, 0);
																										});
																									}
																									dispatcher2.InvokeAsync(action2);
																								};
																							}
																							if (propsPageEnabled && !addedToConfigListBox)
																							{
																								tbPropValue.Text = paramClosure.paramInfo.Value;
																							}
																							else
																							{
																								tbPropValue.Text = propertyInfo.GetValue(indicatorBase).ToString();
																							}
																							grid.Children.Add(tbPropValue);
																						}
																						this.StackPanelIndicatorProps.Children.Add(grid);
																						if (addedToConfigListBox)
																						{
																							list.Add(paramClosure.paramInfo);
																						}
																						break;
																					}
																				}
																			}
																		}
																		catch
																		{
																		}
																	}
																}
															}
															catch
															{
															}
														}
														if (addedToConfigListBox)
														{
															this.ListOfListParamInfo.Add(list);
														}
														Grid grid2 = new Grid
														{
															Margin = new Thickness(6.0, 0.0, 5.0, 3.0)
														};
														grid2.ColumnDefinitions.Add(new ColumnDefinition
														{
															Width = new GridLength(1.0, GridUnitType.Star),
															MinWidth = 50.0
														});
														grid2.ColumnDefinitions.Add(new ColumnDefinition
														{
															Width = new GridLength(1.0, GridUnitType.Star),
															MinWidth = 50.0
														});
														TextBlock textBlock2 = new TextBlock
														{
															Text = "Timeframe",
															Foreground = brush,
															VerticalAlignment = VerticalAlignment.Center,
															HorizontalAlignment = HorizontalAlignment.Left,
															TextTrimming = TextTrimming.CharacterEllipsis
														};
														textBlock2.SetValue(Grid.ColumnProperty, 0);
														closure.cmbTimeframe = new ComboBox
														{
															Foreground = brush,
															MinHeight = 20.0,
															ToolTip = string.Empty
														};
														closure.cmbTimeframe.SetValue(Grid.ColumnProperty, 1);
														if (propsPageEnabled)
														{
															closure.cmbTimeframe.MouseLeftButtonUp += delegate(object sender, MouseButtonEventArgs e)
															{
																closure.cmbTimeframe.IsDropDownOpen = !closure.cmbTimeframe.IsDropDownOpen;
															};
															string[] array3 = closure.indicatorConfig.TimeframeConfigStr.Split(new char[] { '&' }, StringSplitOptions.RemoveEmptyEntries);
															string text7 = string.Empty;
															int num3 = 0;
															foreach (string text8 in this.ListTimeframeStr)
															{
																CheckBox checkBox2 = new CheckBox
																{
																	Content = text8,
																	Foreground = brush,
																	HorizontalAlignment = HorizontalAlignment.Stretch,
																	HorizontalContentAlignment = HorizontalAlignment.Stretch,
																	IsHitTestVisible = true
																};
																foreach (string text9 in array3)
																{
																	if (!(text8 != text9.Trim()))
																	{
																		text7 = ((num3 == 0) ? text8 : (text7 + " - " + text8));
																		num3++;
																		checkBox2.IsChecked = new bool?(true);
																		break;
																	}
																}
																ComboBoxItem comboBoxItem = new ComboBoxItem
																{
																	Content = checkBox2,
																	HorizontalAlignment = HorizontalAlignment.Stretch,
																	HorizontalContentAlignment = HorizontalAlignment.Stretch,
																	Padding = new Thickness(10.0, 0.0, 0.0, 0.0)
																};
																closure.cmbTimeframe.Items.Add(comboBoxItem);
															}
															Button button = new Button
															{
																Content = "OK",
																Padding = new Thickness(0.0)
															};
															ComboBoxItem comboBoxItem2 = new ComboBoxItem
															{
																Content = button,
																Cursor = Cursors.Hand,
																ToolTip = string.Empty
															};
															closure.cmbTimeframe.Items.Add(comboBoxItem2);
															TextBlock tblItemDisplay = new TextBlock
															{
																Text = text7,
																TextTrimming = TextTrimming.CharacterEllipsis
															};
															ComboBoxItem cmbItemDisplay = new ComboBoxItem
															{
																Content = tblItemDisplay,
																Visibility = Visibility.Collapsed
															};
															closure.cmbTimeframe.Items.Add(cmbItemDisplay);
															closure.cmbTimeframe.SelectedIndex = closure.cmbTimeframe.Items.Count - 1;
															closure.cmbTimeframe.DropDownClosed += delegate(object s, EventArgs e)
															{
																ValueTuple<string, string> selectedTimeframe = closure.capturedThis.GetSelectedTimeframe(closure.cmbTimeframe.Items);
																closure.indicatorConfig.TimeframeConfigStr = selectedTimeframe.Item2;
																closure.cmbTimeframe.SelectedItem = cmbItemDisplay;
																if (tblItemDisplay.Text != selectedTimeframe.Item1)
																{
																	tblItemDisplay.Text = selectedTimeframe.Item1;
																	closure.capturedThis.IsParamChanged = true;
																}
															};
															button.Click += delegate(object sender, RoutedEventArgs e)
															{
																ValueTuple<string, string> selectedTimeframe2 = closure.capturedThis.GetSelectedTimeframe(closure.cmbTimeframe.Items);
																closure.indicatorConfig.TimeframeConfigStr = selectedTimeframe2.Item2;
																closure.cmbTimeframe.IsDropDownOpen = false;
																if (tblItemDisplay.Text != selectedTimeframe2.Item1)
																{
																	tblItemDisplay.Text = selectedTimeframe2.Item1;
																	closure.capturedThis.IsParamChanged = true;
																}
															};
															closure.cmbTimeframe.SelectionChanged += delegate(object s, SelectionChangedEventArgs e)
															{
																ValueTuple<string, string> selectedTimeframe3 = closure.capturedThis.GetSelectedTimeframe(closure.cmbTimeframe.Items);
																closure.indicatorConfig.TimeframeConfigStr = selectedTimeframe3.Item2;
																closure.cmbTimeframe.SelectedItem = cmbItemDisplay;
																if (tblItemDisplay.Text != selectedTimeframe3.Item1)
																{
																	tblItemDisplay.Text = selectedTimeframe3.Item1;
																	closure.capturedThis.IsParamChanged = true;
																}
															};
														}
														else
														{
															closure.cmbTimeframe.Items.Add(this.ListTimeframeStr[0]);
															closure.cmbTimeframe.SelectedIndex = 0;
														}
														grid2.Children.Add(textBlock2);
														grid2.Children.Add(closure.cmbTimeframe);
														this.StackPanelIndicatorProps.Children.Add(grid2);
														Grid grid3 = new Grid
														{
															Margin = new Thickness(6.0, 0.0, 5.0, 0.0)
														};
														grid3.ColumnDefinitions.Add(new ColumnDefinition
														{
															Width = new GridLength(1.0, GridUnitType.Star),
															MinWidth = 50.0
														});
														grid3.ColumnDefinitions.Add(new ColumnDefinition
														{
															Width = new GridLength(1.0, GridUnitType.Star),
															MinWidth = 50.0
														});
														TextBlock textBlock3 = new TextBlock
														{
															Text = "Plot",
															Foreground = brush,
															VerticalAlignment = VerticalAlignment.Center,
															HorizontalAlignment = HorizontalAlignment.Left,
															TextTrimming = TextTrimming.CharacterEllipsis
														};
														textBlock3.SetValue(Grid.ColumnProperty, 0);
														Plot[] plots = indicatorBase.Plots;
														if (plots != null && plots.Length != 0)
														{
															int num4 = plots.Length;
															string[] array4 = new string[num4];
															for (int k = 0; k < num4; k++)
															{
																array4[k] = plots[k].Name;
															}
															ComboBox comboBox2 = new ComboBox
															{
																Foreground = brush,
																MinHeight = 20.0,
																VerticalContentAlignment = VerticalAlignment.Center,
																ToolTip = string.Empty,
																ItemsSource = array4
															};
															comboBox2.SetValue(Grid.ColumnProperty, 1);
															if (propsPageEnabled)
															{
																comboBox2.SelectionChanged += delegate(object sender, SelectionChangedEventArgs e)
																{
																	ComboBox comboBox4 = sender as ComboBox;
																	closure.indicatorConfig.PlotIndex = comboBox4.SelectedIndex;
																	closure.capturedThis.IsParamChanged = true;
																};
															}
															if (propsPageEnabled && !addedToConfigListBox)
															{
																comboBox2.SelectedIndex = closure.indicatorConfig.PlotIndex;
															}
															else
															{
																comboBox2.SelectedIndex = 0;
															}
															grid3.Children.Add(textBlock3);
															grid3.Children.Add(comboBox2);
															this.StackPanelIndicatorProps.Children.Add(grid3);
															Grid grid4 = this.CreateConditionGridControl(null, closure.indicatorConfig, addedToConfigListBox, propsPageEnabled, true);
															this.StackPanelIndicatorProps.IsEnabled = propsPageEnabled;
															Grid grid5 = this.CreateConditionGridControl(grid4, closure.indicatorConfig, addedToConfigListBox, propsPageEnabled, false);
															if (num4 == 0)
															{
																grid3.Visibility = (grid4.Visibility = (grid5.Visibility = Visibility.Collapsed));
															}
															this.StackPanelIndicatorProps.Children.Add(grid4);
															if (addedToConfigListBox)
															{
																this.ListIndicatorConfig.Add(closure.indicatorConfig);
															}
															this.IsParamChanged = false;
														}
													}
												}
											}
										}
									}
								}
							}
						}
					}
				}
				catch
				{
				}
			}
			[MethodImpl(MethodImplOptions.NoInlining)]
			private ValueTuple<string, string> GetSelectedTimeframe(ItemCollection itemCollection)
			{
				string text = string.Empty;
				string text2 = string.Empty;
				int num = 0;
				foreach (object obj in ((IEnumerable)itemCollection))
				{
					CheckBox checkBox = (obj as ComboBoxItem).Content as CheckBox;
					if (checkBox != null && checkBox.IsChecked.GetValueOrDefault())
					{
						string text3 = checkBox.Content.ToString();
						string text4 = text3 ?? "";
						text = ((num > 0) ? (text + " - " + text3) : text3);
						text2 = ((num > 0) ? (text2 + "&" + text4) : text4);
						num++;
					}
				}
				if (num == 0)
				{
					text2 = this.ListTimeframeStr[0] ?? "";
					CheckBox checkBox2 = (itemCollection.GetItemAt(0) as ComboBoxItem).Content as CheckBox;
					if (checkBox2 != null)
					{
						text = checkBox2.Content.ToString();
						checkBox2.IsChecked = new bool?(true);
					}
				}
				return new ValueTuple<string, string>(text, text2);
			}
			[MethodImpl(MethodImplOptions.NoInlining)]
			private Grid CreateConditionGridControl(Grid grid, DDDeepStackConfluence.IndicatorConfig indicatorConfig, bool isAddedToListBoxConfig, bool isPropsPageEnabled, bool isBullish)
			{
				Grid grid2;
				if (grid == null)
				{
					this.rowIndex = 0;
					grid2 = new Grid
					{
						Margin = new Thickness(6.0, 3.0, 5.0, 0.0)
					};
					grid2.ColumnDefinitions.Add(new ColumnDefinition
					{
						Width = new GridLength(1.0, GridUnitType.Star),
						MinWidth = 50.0
					});
					grid2.ColumnDefinitions.Add(new ColumnDefinition
					{
						Width = new GridLength(1.0, GridUnitType.Star),
						MinWidth = 50.0
					});
				}
				else
				{
					grid2 = grid;
					this.rowIndex++;
				}
				grid2.RowDefinitions.Add(new RowDefinition
				{
					MinHeight = 20.0
				});
				Thickness thickness = new Thickness(0.0, (double)((this.rowIndex == 0) ? 0 : 3), 0.0, 0.0);
				TextBlock textBlock = new TextBlock
				{
					Text = "Value: " + (isBullish ? "Bullish" : "Bearish"),
					Foreground = (isPropsPageEnabled ? this.Foreground : Brushes.Gray),
					VerticalAlignment = VerticalAlignment.Center,
					HorizontalAlignment = HorizontalAlignment.Left,
					TextTrimming = TextTrimming.CharacterEllipsis,
					Margin = thickness
				};
				textBlock.SetValue(Grid.ColumnProperty, 0);
				textBlock.SetValue(Grid.RowProperty, this.rowIndex);
				Grid grid3 = new Grid();
				grid3.ColumnDefinitions.Add(new ColumnDefinition
				{
					Width = default(GridLength),
					MinWidth = 0.0
				});
				grid3.ColumnDefinitions.Add(new ColumnDefinition
				{
					Width = new GridLength(1.0, GridUnitType.Star),
					MinWidth = 50.0
				});
				grid3.SetValue(Grid.ColumnProperty, 1);
				grid3.SetValue(Grid.RowProperty, this.rowIndex);
				ComboBox comboBox = new ComboBox
				{
					Foreground = (isPropsPageEnabled ? this.Foreground : Brushes.Gray),
					FontWeight = FontWeights.Bold,
					VerticalContentAlignment = VerticalAlignment.Center,
					ToolTip = string.Empty,
					ItemsSource = this.ListOperator,
					MinWidth = 40.0,
					MinHeight = 20.0,
					Margin = new Thickness(0.0, (double)((this.rowIndex == 0) ? 0 : 3), 3.0, 0.0)
				};
				comboBox.SetValue(Grid.ColumnProperty, 0);
				TextBox tbPropValue = new TextBox
				{
					VerticalContentAlignment = VerticalAlignment.Center,
					Foreground = (isPropsPageEnabled ? this.Foreground : Brushes.Gray),
					MinWidth = 0.0,
					MinHeight = 20.0,
					Margin = thickness
				};
				tbPropValue.SetValue(Grid.ColumnProperty, 1);
				if (isPropsPageEnabled)
				{
					string oldText = null;
					tbPropValue.TextChanged += delegate(object sender, TextChangedEventArgs e)
					{
						TextBox txtBox = sender as TextBox;
						tbPropValue.Dispatcher.InvokeAsync(delegate
						{
							NinjaTrader.NinjaScript.Indicators.DimDim.DDDeepStackConfluence.NumericTextBox_TextChangedHandler(txtBox, ref oldText, true, true);
						});
						double num = (isBullish ? indicatorConfig.ValueBullish : indicatorConfig.ValueBearish);
						double num2 = (double.TryParse(txtBox.Text, out num2) ? num2 : num);
						if (isBullish)
						{
							indicatorConfig.ValueBullish = num2;
						}
						else
						{
							indicatorConfig.ValueBearish = num2;
						}
						this.IsParamChanged = true;
					};
					Action cachedPrimaryAction = null;
					tbPropValue.PreviewKeyDown += delegate(object sender, KeyEventArgs e)
					{
						Dispatcher dispatcher = tbPropValue.Dispatcher;
						Action action;
						if ((action = cachedPrimaryAction) == null)
						{
							action = (cachedPrimaryAction = delegate
							{
								NinjaTrader.NinjaScript.Indicators.DimDim.DDDeepStackConfluence.NumericTextBox_PreviewKeyDownHandler(tbPropValue, ref oldText);
							});
						}
						dispatcher.InvokeAsync(action);
					};
					Action cachedSecondaryAction = null;
					tbPropValue.LostFocus += delegate(object sender, RoutedEventArgs e)
					{
						Dispatcher dispatcher2 = tbPropValue.Dispatcher;
						Action action2;
						if ((action2 = cachedSecondaryAction) == null)
						{
							action2 = (cachedSecondaryAction = delegate
							{
								NinjaTrader.NinjaScript.Indicators.DimDim.DDDeepStackConfluence.NumericTextBox_LostFocusHandler(tbPropValue, 0);
							});
						}
						dispatcher2.InvokeAsync(action2);
					};
				}
				if (isPropsPageEnabled && !isAddedToListBoxConfig)
				{
					tbPropValue.Text = (isBullish ? indicatorConfig.ValueBullish : indicatorConfig.ValueBearish).ToString();
				}
				else
				{
					tbPropValue.Text = (isBullish ? 1 : (-1)).ToString();
				}
				if (isPropsPageEnabled)
				{
					comboBox.SelectionChanged += delegate(object sender, SelectionChangedEventArgs e)
					{
						int selectedIndex = (sender as ComboBox).SelectedIndex;
						if (isBullish)
						{
							indicatorConfig.OperatorBullish = selectedIndex;
						}
						else
						{
							indicatorConfig.OperatorBearish = selectedIndex;
						}
						this.IsParamChanged = true;
					};
				}
				if (isPropsPageEnabled && !isAddedToListBoxConfig)
				{
					comboBox.SelectedIndex = (isBullish ? indicatorConfig.OperatorBullish : indicatorConfig.OperatorBearish);
				}
				else
				{
					comboBox.SelectedIndex = 4;
				}
				grid3.Children.Add(comboBox);
				grid3.Children.Add(tbPropValue);
				grid2.Children.Add(textBlock);
				grid2.Children.Add(grid3);
				return grid2;
			}
			[MethodImpl(MethodImplOptions.NoInlining)]
			private int GetEnumIndex(Type inputType, string inputValue)
			{
				Type type;
				Array array;
				if (inputType == typeof(ISeries<double>))
				{
					type = Enum.GetUnderlyingType(typeof(PriceType));
					array = Enum.GetValues(typeof(PriceType));
				}
				else
				{
					type = Enum.GetUnderlyingType(inputType);
					array = Enum.GetValues(inputType);
				}
				for (int i = 0; i < array.Length; i++)
				{
					object obj = Convert.ChangeType(array.GetValue(i), type);
					if (inputValue == obj.ToString())
					{
						return i;
					}
				}
				return -1;
			}
			private int rowIndex;
		}
		public class PlotOrDataSeriesInfo
		{
			public string PlotName { get; set; }
			public Series<double> PlotOrDataSeries { get; set; }
			[MethodImpl(MethodImplOptions.NoInlining)]
			public PlotOrDataSeriesInfo(string plotName, Series<double> plotOrDataSeries)
			{
				this.PlotName = plotName;
				this.PlotOrDataSeries = plotOrDataSeries;
			}
		}
	}
	
	public class DDDeepStackConfluence_Converter : IndicatorBaseConverter
	{
		
		public override PropertyDescriptorCollection GetProperties(ITypeDescriptorContext context, object component, Attribute[] attrs)
		{
			DDDeepStackConfluence DDDeepStackConfluence = component as DDDeepStackConfluence;
			PropertyDescriptorCollection propertyDescriptorCollection = (base.GetPropertiesSupported(context) ? base.GetProperties(context, component, attrs) : TypeDescriptor.GetProperties(component, attrs));
			if (DDDeepStackConfluence == null || propertyDescriptorCollection == null)
			{
				return propertyDescriptorCollection;
			}
			PropertyDescriptor propertyDescriptor = propertyDescriptorCollection["IndicatorsConfigJSON"];
			PropertyDescriptor propertyDescriptor2 = propertyDescriptorCollection["IndicatorsParamsJSON"];
			PropertyDescriptor propertyDescriptor3 = propertyDescriptorCollection["Namespace"];
			PropertyDescriptor propertyDescriptor4 = propertyDescriptorCollection["IndicatorName"];
			PropertyDescriptor propertyDescriptor5 = propertyDescriptorCollection["InputSeriesIndex"];
			PropertyDescriptor propertyDescriptor6 = propertyDescriptorCollection["IndicatorSetting"];
			PropertyDescriptor propertyDescriptor7 = propertyDescriptorCollection["PlotIndex"];
			PropertyDescriptor propertyDescriptor8 = propertyDescriptorCollection["ValueBullish"];
			PropertyDescriptor propertyDescriptor9 = propertyDescriptorCollection["ValueBearish"];
			PropertyDescriptor propertyDescriptor10 = propertyDescriptorCollection["OperatorBullish"];
			PropertyDescriptor propertyDescriptor11 = propertyDescriptorCollection["OperatorBearish"];
			PropertyDescriptor propertyDescriptor12 = propertyDescriptorCollection["DataSeries2Value"];
			PropertyDescriptor propertyDescriptor13 = propertyDescriptorCollection["DataSeries2Value1"];
			PropertyDescriptor propertyDescriptor14 = propertyDescriptorCollection["DataSeries2Value2"];
			PropertyDescriptor propertyDescriptor15 = propertyDescriptorCollection["DataSeries3Value"];
			PropertyDescriptor propertyDescriptor16 = propertyDescriptorCollection["DataSeries3Value1"];
			PropertyDescriptor propertyDescriptor17 = propertyDescriptorCollection["DataSeries3Value2"];
			PropertyDescriptor propertyDescriptor18 = propertyDescriptorCollection["DataSeries4Value"];
			PropertyDescriptor propertyDescriptor19 = propertyDescriptorCollection["DataSeries4Value1"];
			PropertyDescriptor propertyDescriptor20 = propertyDescriptorCollection["DataSeries4Value2"];
			PropertyDescriptor propertyDescriptor21 = propertyDescriptorCollection["DataSeries5Value"];
			PropertyDescriptor propertyDescriptor22 = propertyDescriptorCollection["DataSeries5Value1"];
			PropertyDescriptor propertyDescriptor23 = propertyDescriptorCollection["DataSeries5Value2"];
			PropertyDescriptor propertyDescriptor24 = propertyDescriptorCollection["DataSeriesBehindEnabled"];
			PropertyDescriptor propertyDescriptor25 = propertyDescriptorCollection["DataSeriesBehindType"];
			PropertyDescriptor propertyDescriptor26 = propertyDescriptorCollection["DataSeriesBehindValue"];
			propertyDescriptorCollection.Remove(propertyDescriptor);
			propertyDescriptorCollection.Remove(propertyDescriptor2);
			propertyDescriptorCollection.Remove(propertyDescriptor3);
			propertyDescriptorCollection.Remove(propertyDescriptor4);
			propertyDescriptorCollection.Remove(propertyDescriptor5);
			propertyDescriptorCollection.Remove(propertyDescriptor6);
			propertyDescriptorCollection.Remove(propertyDescriptor7);
			propertyDescriptorCollection.Remove(propertyDescriptor8);
			propertyDescriptorCollection.Remove(propertyDescriptor9);
			propertyDescriptorCollection.Remove(propertyDescriptor10);
			propertyDescriptorCollection.Remove(propertyDescriptor11);
			propertyDescriptorCollection.Remove(propertyDescriptor13);
			propertyDescriptorCollection.Remove(propertyDescriptor14);
			propertyDescriptorCollection.Remove(propertyDescriptor15);
			propertyDescriptorCollection.Remove(propertyDescriptor16);
			propertyDescriptorCollection.Remove(propertyDescriptor17);
			DDDeepStackConfluence_DataSeriesType dataSeries3Type = DDDeepStackConfluence.DataSeries3Type;
			if (dataSeries3Type == DDDeepStackConfluence_DataSeriesType.Disabled)
			{
				propertyDescriptor15 = new DDDeepStackConfluence_ReadOnlyDescriptor(propertyDescriptor15, false);
			}
			propertyDescriptorCollection.Add(propertyDescriptor15);
			propertyDescriptorCollection.Remove(propertyDescriptor18);
			propertyDescriptorCollection.Remove(propertyDescriptor19);
			propertyDescriptorCollection.Remove(propertyDescriptor20);
			DDDeepStackConfluence_DataSeriesType dataSeries4Type = DDDeepStackConfluence.DataSeries4Type;
			if (dataSeries4Type == DDDeepStackConfluence_DataSeriesType.Disabled)
			{
				propertyDescriptor18 = new DDDeepStackConfluence_ReadOnlyDescriptor(propertyDescriptor18, false);
			}
			propertyDescriptorCollection.Add(propertyDescriptor18);
			propertyDescriptorCollection.Remove(propertyDescriptor21);
			propertyDescriptorCollection.Remove(propertyDescriptor22);
			propertyDescriptorCollection.Remove(propertyDescriptor23);
			DDDeepStackConfluence_DataSeriesType dataSeries5Type = DDDeepStackConfluence.DataSeries5Type;
			if (dataSeries5Type == DDDeepStackConfluence_DataSeriesType.Disabled)
			{
				propertyDescriptor21 = new DDDeepStackConfluence_ReadOnlyDescriptor(propertyDescriptor21, false);
			}
			propertyDescriptorCollection.Add(propertyDescriptor21);
			propertyDescriptorCollection.Remove(propertyDescriptor25);
			propertyDescriptorCollection.Remove(propertyDescriptor26);
			bool flag = (bool)propertyDescriptor24.GetValue(DDDeepStackConfluence);
			if (!flag)
			{
				propertyDescriptor25 = new DDDeepStackConfluence_ReadOnlyDescriptor(propertyDescriptor25, flag);
				propertyDescriptor26 = new DDDeepStackConfluence_ReadOnlyDescriptor(propertyDescriptor26, flag);
			}
			propertyDescriptorCollection.Add(propertyDescriptor25);
			propertyDescriptorCollection.Add(propertyDescriptor26);
			if (DDDeepStackConfluence.DataSeries2Type == DDDeepStackConfluence_DataSeriesType.ninZaRenko || DDDeepStackConfluence.DataSeries2Type == DDDeepStackConfluence_DataSeriesType.KingRenko)
			{
				propertyDescriptorCollection.Remove(propertyDescriptor12);
				propertyDescriptorCollection.Add(propertyDescriptor13);
				propertyDescriptorCollection.Add(propertyDescriptor14);
			}
			if (dataSeries3Type == DDDeepStackConfluence_DataSeriesType.ninZaRenko || dataSeries3Type == DDDeepStackConfluence_DataSeriesType.KingRenko)
			{
				propertyDescriptorCollection.Remove(propertyDescriptor15);
				if (dataSeries3Type == DDDeepStackConfluence_DataSeriesType.Disabled)
				{
					propertyDescriptor16 = new DDDeepStackConfluence_ReadOnlyDescriptor(propertyDescriptor16, false);
					propertyDescriptor17 = new DDDeepStackConfluence_ReadOnlyDescriptor(propertyDescriptor17, false);
				}
				propertyDescriptorCollection.Add(propertyDescriptor16);
				propertyDescriptorCollection.Add(propertyDescriptor17);
			}
			if (dataSeries4Type == DDDeepStackConfluence_DataSeriesType.ninZaRenko || dataSeries4Type == DDDeepStackConfluence_DataSeriesType.KingRenko)
			{
				propertyDescriptorCollection.Remove(propertyDescriptor18);
				if (dataSeries4Type == DDDeepStackConfluence_DataSeriesType.Disabled)
				{
					propertyDescriptor19 = new DDDeepStackConfluence_ReadOnlyDescriptor(propertyDescriptor19, false);
					propertyDescriptor20 = new DDDeepStackConfluence_ReadOnlyDescriptor(propertyDescriptor20, false);
				}
				propertyDescriptorCollection.Add(propertyDescriptor19);
				propertyDescriptorCollection.Add(propertyDescriptor20);
			}
			if (dataSeries5Type == DDDeepStackConfluence_DataSeriesType.ninZaRenko || dataSeries5Type == DDDeepStackConfluence_DataSeriesType.KingRenko)
			{
				propertyDescriptorCollection.Remove(propertyDescriptor21);
				if (dataSeries5Type == DDDeepStackConfluence_DataSeriesType.Disabled)
				{
					propertyDescriptor22 = new DDDeepStackConfluence_ReadOnlyDescriptor(propertyDescriptor22, false);
					propertyDescriptor23 = new DDDeepStackConfluence_ReadOnlyDescriptor(propertyDescriptor23, false);
				}
				propertyDescriptorCollection.Add(propertyDescriptor22);
				propertyDescriptorCollection.Add(propertyDescriptor23);
			}
			return propertyDescriptorCollection;
		}
		public override bool GetPropertiesSupported(ITypeDescriptorContext context)
		{
			return true;
		}
	}
	
	public class DDDeepStackConfluence_ReadOnlyDescriptor : PropertyDescriptor
	{
		
		public DDDeepStackConfluence_ReadOnlyDescriptor(PropertyDescriptor propertyDescriptor, bool isReadOnly)
			: base(propertyDescriptor.Name, propertyDescriptor.Attributes.OfType<Attribute>().ToArray<Attribute>())
		{
			this.property = propertyDescriptor;
			this.isReadOnly = isReadOnly;
		}
		
		public override object GetValue(object component)
		{
			DDDeepStackConfluence DDDeepStackConfluence = component as DDDeepStackConfluence;
			if (DDDeepStackConfluence == null)
			{
				return null;
			}
			string name = this.property.Name;
			if (name != null)
			{
				switch (name.Length)
				{
				case 16:
					switch (name[10])
					{
					case '3':
						if (name == "DataSeries3Value")
						{
							return DDDeepStackConfluence.DataSeries3Value;
						}
						break;
					case '4':
						if (name == "DataSeries4Value")
						{
							return DDDeepStackConfluence.DataSeries4Value;
						}
						break;
					case '5':
						if (name == "DataSeries5Value")
						{
							return DDDeepStackConfluence.DataSeries5Value;
						}
						break;
					}
					break;
				case 17:
					switch (name[10])
					{
					case '3':
						if (name == "DataSeries3Value1")
						{
							return DDDeepStackConfluence.DataSeries3Value1;
						}
						if (name == "DataSeries3Value2")
						{
							return DDDeepStackConfluence.DataSeries3Value2;
						}
						break;
					case '4':
						if (name == "DataSeries4Value1")
						{
							return DDDeepStackConfluence.DataSeries4Value1;
						}
						if (name == "DataSeries4Value2")
						{
							return DDDeepStackConfluence.DataSeries4Value2;
						}
						break;
					case '5':
						if (name == "DataSeries5Value1")
						{
							return DDDeepStackConfluence.DataSeries5Value1;
						}
						if (name == "DataSeries5Value2")
						{
							return DDDeepStackConfluence.DataSeries5Value2;
						}
						break;
					}
					break;
				case 20:
					if (name == "DataSeriesBehindType")
					{
						return DDDeepStackConfluence.DataSeriesBehindType;
					}
					break;
				case 21:
					if (name == "DataSeriesBehindValue")
					{
						return DDDeepStackConfluence.DataSeriesBehindValue;
					}
					break;
				}
			}
			return null;
		}
		
		public override void SetValue(object component, object value)
		{
			DDDeepStackConfluence DDDeepStackConfluence = component as DDDeepStackConfluence;
			if (DDDeepStackConfluence == null)
			{
				return;
			}
			string name = this.property.Name;
			if (name != null)
			{
				switch (name.Length)
				{
				case 16:
					switch (name[10])
					{
					case '3':
						if (!(name == "DataSeries3Value"))
						{
							return;
						}
						DDDeepStackConfluence.DataSeries3Value = (int)value;
						return;
					case '4':
						if (!(name == "DataSeries4Value"))
						{
							return;
						}
						DDDeepStackConfluence.DataSeries4Value = (int)value;
						return;
					case '5':
						if (!(name == "DataSeries5Value"))
						{
							return;
						}
						DDDeepStackConfluence.DataSeries5Value = (int)value;
						return;
					default:
						return;
					}
					break;
				case 17:
					switch (name[10])
					{
					case '3':
						if (name == "DataSeries3Value1")
						{
							DDDeepStackConfluence.DataSeries3Value1 = (int)value;
							return;
						}
						if (!(name == "DataSeries3Value2"))
						{
							return;
						}
						DDDeepStackConfluence.DataSeries3Value2 = (int)value;
						return;
					case '4':
						if (name == "DataSeries4Value1")
						{
							DDDeepStackConfluence.DataSeries4Value1 = (int)value;
							return;
						}
						if (!(name == "DataSeries4Value2"))
						{
							return;
						}
						DDDeepStackConfluence.DataSeries4Value2 = (int)value;
						return;
					case '5':
						if (name == "DataSeries5Value1")
						{
							DDDeepStackConfluence.DataSeries5Value1 = (int)value;
							return;
						}
						if (!(name == "DataSeries5Value2"))
						{
							return;
						}
						DDDeepStackConfluence.DataSeries5Value2 = (int)value;
						return;
					default:
						return;
					}
					break;
				case 18:
				case 19:
					break;
				case 20:
					if (!(name == "DataSeriesBehindType"))
					{
						return;
					}
					DDDeepStackConfluence.DataSeriesBehindType = (DDDeepStackConfluence_DataSeriesType)value;
					return;
				case 21:
					if (!(name == "DataSeriesBehindValue"))
					{
						return;
					}
					DDDeepStackConfluence.DataSeriesBehindValue = (int)value;
					break;
				default:
					return;
				}
			}
		}
		public override bool IsReadOnly
		{
			get
			{
				return !this.isReadOnly;
			}
		}
		public override bool CanResetValue(object component)
		{
			return true;
		}
		public override Type ComponentType
		{
			get
			{
				return typeof(DDDeepStackConfluence);
			}
		}
		public override Type PropertyType
		{
			get
			{
				return typeof(int);
			}
		}
		public override void ResetValue(object component)
		{
		}
		public override bool ShouldSerializeValue(object component)
		{
			return true;
		}

		private PropertyDescriptor property;
		private bool isReadOnly;
	}
	
	public class DDDeepStackConfluence_TypeDataSeriesBehindConverter : TypeConverter
	{
		public override TypeConverter.StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
		{
			return new TypeConverter.StandardValuesCollection(new List<string> { "Tick", "Volume", "Range", "Second", "Minute" });
		}
		public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
		{
			string text = value.ToString();
			if (text == "Tick")
			{
				return DDDeepStackConfluence_DataSeriesType.Tick;
			}
			if (text == "Volume")
			{
				return DDDeepStackConfluence_DataSeriesType.Volume;
			}
			if (text == "Range")
			{
				return DDDeepStackConfluence_DataSeriesType.Range;
			}
			if (text == "Second")
			{
				return DDDeepStackConfluence_DataSeriesType.Second;
			}
			if (text == "Minute")
			{
				return DDDeepStackConfluence_DataSeriesType.Minute;
			}
			return DDDeepStackConfluence_DataSeriesType.Tick;
		}
		public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
		{
			DDDeepStackConfluence_DataSeriesType DDDeepStackConfluence_DataSeriesType = (DDDeepStackConfluence_DataSeriesType)Enum.Parse(typeof(DDDeepStackConfluence_DataSeriesType), value.ToString());
			if (DDDeepStackConfluence_DataSeriesType == DDDeepStackConfluence_DataSeriesType.Tick)
			{
				return "Tick";
			}
			if (DDDeepStackConfluence_DataSeriesType == DDDeepStackConfluence_DataSeriesType.Volume)
			{
				return "Volume";
			}
			if (DDDeepStackConfluence_DataSeriesType == DDDeepStackConfluence_DataSeriesType.Range)
			{
				return "Range";
			}
			if (DDDeepStackConfluence_DataSeriesType == DDDeepStackConfluence_DataSeriesType.Second)
			{
				return "Second";
			}
			if (DDDeepStackConfluence_DataSeriesType == DDDeepStackConfluence_DataSeriesType.Minute)
			{
				return "Minute";
			}
			return string.Empty;
		}
		public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
		{
			return true;
		}
		public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
		{
			return true;
		}
		public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
		{
			return true;
		}
		public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
		{
			return true;
		}
	}
	
		public class DDDeepStackConfluence_TypeDataSeriesConverter : TypeConverter
	{
		public override TypeConverter.StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
		{
			return new TypeConverter.StandardValuesCollection(new List<string>
			{
				"Disabled", "Tick", "Volume", "Range", "Second", "Minute", "Day", "Week", "Month", "Year",
				"DDRenko", "KingRenko$"
			});
		}
		public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
		{
			string text = value.ToString();
			if (text == "Disabled")
			{
				return DDDeepStackConfluence_DataSeriesType.Disabled;
			}
			if (text == "Tick")
			{
				return DDDeepStackConfluence_DataSeriesType.Tick;
			}
			if (text == "Volume")
			{
				return DDDeepStackConfluence_DataSeriesType.Volume;
			}
			if (text == "Range")
			{
				return DDDeepStackConfluence_DataSeriesType.Range;
			}
			if (text == "Second")
			{
				return DDDeepStackConfluence_DataSeriesType.Second;
			}
			if (text == "Minute")
			{
				return DDDeepStackConfluence_DataSeriesType.Minute;
			}
			if (text == "Day")
			{
				return DDDeepStackConfluence_DataSeriesType.Day;
			}
			if (text == "Week")
			{
				return DDDeepStackConfluence_DataSeriesType.Week;
			}
			if (text == "Month")
			{
				return DDDeepStackConfluence_DataSeriesType.Month;
			}
			if (text == "Year")
			{
				return DDDeepStackConfluence_DataSeriesType.Year;
			}
			if (text == "DDRenko")
			{
				return DDDeepStackConfluence_DataSeriesType.ninZaRenko;
			}
			if (text == "KingRenko$")
			{
				return DDDeepStackConfluence_DataSeriesType.KingRenko;
			}
			return DDDeepStackConfluence_DataSeriesType.Minute;
		}
		public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
		{
			DDDeepStackConfluence_DataSeriesType DDDeepStackConfluence_DataSeriesType = (DDDeepStackConfluence_DataSeriesType)Enum.Parse(typeof(DDDeepStackConfluence_DataSeriesType), value.ToString());
			if (DDDeepStackConfluence_DataSeriesType == DDDeepStackConfluence_DataSeriesType.Disabled)
			{
				return "Disabled";
			}
			if (DDDeepStackConfluence_DataSeriesType == DDDeepStackConfluence_DataSeriesType.Tick)
			{
				return "Tick";
			}
			if (DDDeepStackConfluence_DataSeriesType == DDDeepStackConfluence_DataSeriesType.Volume)
			{
				return "Volume";
			}
			if (DDDeepStackConfluence_DataSeriesType == DDDeepStackConfluence_DataSeriesType.Range)
			{
				return "Range";
			}
			if (DDDeepStackConfluence_DataSeriesType == DDDeepStackConfluence_DataSeriesType.Second)
			{
				return "Second";
			}
			if (DDDeepStackConfluence_DataSeriesType == DDDeepStackConfluence_DataSeriesType.Minute)
			{
				return "Minute";
			}
			if (DDDeepStackConfluence_DataSeriesType == DDDeepStackConfluence_DataSeriesType.Day)
			{
				return "Day";
			}
			if (DDDeepStackConfluence_DataSeriesType == DDDeepStackConfluence_DataSeriesType.Week)
			{
				return "Week";
			}
			if (DDDeepStackConfluence_DataSeriesType == DDDeepStackConfluence_DataSeriesType.Month)
			{
				return "Month";
			}
			if (DDDeepStackConfluence_DataSeriesType == DDDeepStackConfluence_DataSeriesType.Year)
			{
				return "Year";
			}
			if (DDDeepStackConfluence_DataSeriesType == DDDeepStackConfluence_DataSeriesType.ninZaRenko)
			{
				return "DDRenko";
			}
			if (DDDeepStackConfluence_DataSeriesType == DDDeepStackConfluence_DataSeriesType.KingRenko)
			{
				return "KingRenko$";
			}
			return string.Empty;
		}
		public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
		{
			return true;
		}
		public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
		{
			return true;
		}
		public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
		{
			return true;
		}
		public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
		{
			return true;
		}
	}
	
	public enum DDDeepStackConfluence_DataSeriesType
	{
		Disabled,
		Tick,
		Volume,
		Range,
		Second,
		Minute,
		Day,
		Week,
		Month,
		Year,
		ninZaRenko,
		KingRenko
	}
	
	public enum DDDeepStackConfluence_RenderingMethod
	{
		Custom,
		Builtin
	}
	public enum DDDeepStackConfluence_RibbonPosition
	{
		Top,
		Bottom
	}
	public enum DDDeepStackConfluence_Operators
	{
		Greater,
		Smaller,
		SmallerOrEqual,
		GreaterOrEqual,
		Equal,
		Unequal
	}
	
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private DimDim.DDDeepStackConfluence[] cacheDDDeepStackConfluence;
		public DimDim.DDDeepStackConfluence DDDeepStackConfluence(int dataSeries2Value, int dataSeries2Value1, int dataSeries2Value2, int dataSeries3Value, int dataSeries3Value1, int dataSeries3Value2, int dataSeries4Value, int dataSeries4Value1, int dataSeries4Value2, int dataSeries5Value, int dataSeries5Value1, int dataSeries5Value2, bool dataSeriesBehindEnabled, int dataSeriesBehindValue)
		{
			return DDDeepStackConfluence(Input, dataSeries2Value, dataSeries2Value1, dataSeries2Value2, dataSeries3Value, dataSeries3Value1, dataSeries3Value2, dataSeries4Value, dataSeries4Value1, dataSeries4Value2, dataSeries5Value, dataSeries5Value1, dataSeries5Value2, dataSeriesBehindEnabled, dataSeriesBehindValue);
		}

		public DimDim.DDDeepStackConfluence DDDeepStackConfluence(ISeries<double> input, int dataSeries2Value, int dataSeries2Value1, int dataSeries2Value2, int dataSeries3Value, int dataSeries3Value1, int dataSeries3Value2, int dataSeries4Value, int dataSeries4Value1, int dataSeries4Value2, int dataSeries5Value, int dataSeries5Value1, int dataSeries5Value2, bool dataSeriesBehindEnabled, int dataSeriesBehindValue)
		{
			if (cacheDDDeepStackConfluence != null)
				for (int idx = 0; idx < cacheDDDeepStackConfluence.Length; idx++)
					if (cacheDDDeepStackConfluence[idx] != null && cacheDDDeepStackConfluence[idx].DataSeries2Value == dataSeries2Value && cacheDDDeepStackConfluence[idx].DataSeries2Value1 == dataSeries2Value1 && cacheDDDeepStackConfluence[idx].DataSeries2Value2 == dataSeries2Value2 && cacheDDDeepStackConfluence[idx].DataSeries3Value == dataSeries3Value && cacheDDDeepStackConfluence[idx].DataSeries3Value1 == dataSeries3Value1 && cacheDDDeepStackConfluence[idx].DataSeries3Value2 == dataSeries3Value2 && cacheDDDeepStackConfluence[idx].DataSeries4Value == dataSeries4Value && cacheDDDeepStackConfluence[idx].DataSeries4Value1 == dataSeries4Value1 && cacheDDDeepStackConfluence[idx].DataSeries4Value2 == dataSeries4Value2 && cacheDDDeepStackConfluence[idx].DataSeries5Value == dataSeries5Value && cacheDDDeepStackConfluence[idx].DataSeries5Value1 == dataSeries5Value1 && cacheDDDeepStackConfluence[idx].DataSeries5Value2 == dataSeries5Value2 && cacheDDDeepStackConfluence[idx].DataSeriesBehindEnabled == dataSeriesBehindEnabled && cacheDDDeepStackConfluence[idx].DataSeriesBehindValue == dataSeriesBehindValue && cacheDDDeepStackConfluence[idx].EqualsInput(input))
						return cacheDDDeepStackConfluence[idx];
			return CacheIndicator<DimDim.DDDeepStackConfluence>(new DimDim.DDDeepStackConfluence(){ DataSeries2Value = dataSeries2Value, DataSeries2Value1 = dataSeries2Value1, DataSeries2Value2 = dataSeries2Value2, DataSeries3Value = dataSeries3Value, DataSeries3Value1 = dataSeries3Value1, DataSeries3Value2 = dataSeries3Value2, DataSeries4Value = dataSeries4Value, DataSeries4Value1 = dataSeries4Value1, DataSeries4Value2 = dataSeries4Value2, DataSeries5Value = dataSeries5Value, DataSeries5Value1 = dataSeries5Value1, DataSeries5Value2 = dataSeries5Value2, DataSeriesBehindEnabled = dataSeriesBehindEnabled, DataSeriesBehindValue = dataSeriesBehindValue }, input, ref cacheDDDeepStackConfluence);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.DimDim.DDDeepStackConfluence DDDeepStackConfluence(int dataSeries2Value, int dataSeries2Value1, int dataSeries2Value2, int dataSeries3Value, int dataSeries3Value1, int dataSeries3Value2, int dataSeries4Value, int dataSeries4Value1, int dataSeries4Value2, int dataSeries5Value, int dataSeries5Value1, int dataSeries5Value2, bool dataSeriesBehindEnabled, int dataSeriesBehindValue)
		{
			return indicator.DDDeepStackConfluence(Input, dataSeries2Value, dataSeries2Value1, dataSeries2Value2, dataSeries3Value, dataSeries3Value1, dataSeries3Value2, dataSeries4Value, dataSeries4Value1, dataSeries4Value2, dataSeries5Value, dataSeries5Value1, dataSeries5Value2, dataSeriesBehindEnabled, dataSeriesBehindValue);
		}

		public Indicators.DimDim.DDDeepStackConfluence DDDeepStackConfluence(ISeries<double> input , int dataSeries2Value, int dataSeries2Value1, int dataSeries2Value2, int dataSeries3Value, int dataSeries3Value1, int dataSeries3Value2, int dataSeries4Value, int dataSeries4Value1, int dataSeries4Value2, int dataSeries5Value, int dataSeries5Value1, int dataSeries5Value2, bool dataSeriesBehindEnabled, int dataSeriesBehindValue)
		{
			return indicator.DDDeepStackConfluence(input, dataSeries2Value, dataSeries2Value1, dataSeries2Value2, dataSeries3Value, dataSeries3Value1, dataSeries3Value2, dataSeries4Value, dataSeries4Value1, dataSeries4Value2, dataSeries5Value, dataSeries5Value1, dataSeries5Value2, dataSeriesBehindEnabled, dataSeriesBehindValue);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.DimDim.DDDeepStackConfluence DDDeepStackConfluence(int dataSeries2Value, int dataSeries2Value1, int dataSeries2Value2, int dataSeries3Value, int dataSeries3Value1, int dataSeries3Value2, int dataSeries4Value, int dataSeries4Value1, int dataSeries4Value2, int dataSeries5Value, int dataSeries5Value1, int dataSeries5Value2, bool dataSeriesBehindEnabled, int dataSeriesBehindValue)
		{
			return indicator.DDDeepStackConfluence(Input, dataSeries2Value, dataSeries2Value1, dataSeries2Value2, dataSeries3Value, dataSeries3Value1, dataSeries3Value2, dataSeries4Value, dataSeries4Value1, dataSeries4Value2, dataSeries5Value, dataSeries5Value1, dataSeries5Value2, dataSeriesBehindEnabled, dataSeriesBehindValue);
		}

		public Indicators.DimDim.DDDeepStackConfluence DDDeepStackConfluence(ISeries<double> input , int dataSeries2Value, int dataSeries2Value1, int dataSeries2Value2, int dataSeries3Value, int dataSeries3Value1, int dataSeries3Value2, int dataSeries4Value, int dataSeries4Value1, int dataSeries4Value2, int dataSeries5Value, int dataSeries5Value1, int dataSeries5Value2, bool dataSeriesBehindEnabled, int dataSeriesBehindValue)
		{
			return indicator.DDDeepStackConfluence(input, dataSeries2Value, dataSeries2Value1, dataSeries2Value2, dataSeries3Value, dataSeries3Value1, dataSeries3Value2, dataSeries4Value, dataSeries4Value1, dataSeries4Value2, dataSeries5Value, dataSeries5Value1, dataSeries5Value2, dataSeriesBehindEnabled, dataSeriesBehindValue);
		}
	}
}

#endregion
