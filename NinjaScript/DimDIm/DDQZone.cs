using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Drawing;
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
using ScottPlot;
using ScottPlot.Plottable;
using ScottPlot.Renderable;
using ScottPlot.WPF;
using SharpDX;
using SharpDX.Direct2D1;
namespace NinjaTrader.NinjaScript.Indicators.DimDim
{
	[CategoryOrder("Alerts", 1000060)]
	[CategoryOrder("Critical", 1000090)]
	[CategoryOrder("Special", 1000080)]
	[TypeConverter("NinjaTrader.NinjaScript.Indicators.DimDim.DDQuantZone_Converter")]
	[CategoryOrder("Graphics", 1000020)]
	[CategoryOrder("General", 1000010)]
	[CategoryOrder("Developer", 0)]
	[CategoryOrder("Toggle", 1000070)]
	[CategoryOrder("Windows", 1000040)]
	[CategoryOrder("Control Panel", 1000030)]
	[CategoryOrder("Gradient", 1000050)]
	public class DDQuantZone : Indicator
	{
		public enum DD_MAType
		{
			EMA,
			SMA,
			DEMA,
			HMA,
			LinReg,
			TEMA,
			TMA,
			VWMA,
			WMA,
			WilderMA,
			ZLEMA
		}

		public enum DD_TextPosition { TopLeft, TopRight, BottomLeft, BottomRight, Center }

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
			public static string GetDefaultDocumentsPath()
			{
				return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
			}

			public static T FindVisualParent<T>(global::System.Windows.DependencyObject child, T defaultValue) where T : global::System.Windows.DependencyObject
			{
				global::System.Windows.DependencyObject current = child;
				while (current != null)
				{
					T match = current as T;
					if (match != null) return match;
					current = global::System.Windows.Media.VisualTreeHelper.GetParent(current);
				}
				return defaultValue;
			}

			public static bool? IsLightTheme()
			{
				return IsLightTheme(GetSkinBrush("ChartControl.ChartBackground", global::System.Windows.Media.Brushes.White));
			}

			public static bool? IsLightTheme(global::System.Windows.Media.Brush backgroundBrush)
			{
				global::System.Windows.Media.Color? color = GetBrushColor(backgroundBrush);
				if (!color.HasValue)
					return null;

				return GetBrightness(color.Value) >= 128.0;
			}

			public static global::System.Windows.Media.Brush GetChartBackgroundBrush(ChartControl chartControl)
			{
				global::System.Windows.Media.Brush brush = null;
				try
				{
					brush = chartControl != null && chartControl.Properties != null ? chartControl.Properties.ChartBackground : null;
				}
				catch
				{
					brush = null;
				}
				return brush ?? GetSkinBrush("ChartControl.ChartBackground", global::System.Windows.Media.Brushes.White);
			}

			public static global::System.Windows.Media.Brush GetChartTextBrush(ChartControl chartControl, global::System.Windows.Media.Brush backgroundBrush)
			{
				global::System.Windows.Media.Brush brush = null;
				try
				{
					brush = chartControl != null && chartControl.Properties != null ? chartControl.Properties.ChartText : null;
				}
				catch
				{
					brush = null;
				}
				brush = brush ?? GetSkinBrush("ChartControl.ChartText", null);

				if (IsReadableOn(brush, backgroundBrush))
					return brush;

				return IsLightTheme(backgroundBrush).GetValueOrDefault(true) ? global::System.Windows.Media.Brushes.Black : global::System.Windows.Media.Brushes.LightGray;
			}

			public static global::System.Windows.Media.Brush EnsureReadableTextBrush(global::System.Windows.Media.Brush textBrush, ChartControl chartControl, global::System.Windows.Media.Brush backgroundBrush)
			{
				if (IsReadableOn(textBrush, backgroundBrush))
					return textBrush;

				return GetChartTextBrush(chartControl, backgroundBrush);
			}

			public static string BrushToHex(global::System.Windows.Media.Brush brush, string fallback)
			{
				global::System.Windows.Media.Color? color = GetBrushColor(brush);
				if (!color.HasValue)
					return fallback;

				global::System.Windows.Media.Color value = color.Value;
				return value.A == byte.MaxValue
					? string.Format("#{0:X2}{1:X2}{2:X2}", value.R, value.G, value.B)
					: string.Format("#{0:X2}{1:X2}{2:X2}{3:X2}", value.A, value.R, value.G, value.B);
			}

			public static global::System.Windows.Size MeasureControlSize(global::System.Windows.FrameworkElement control, string text)
			{
				return ComputeControlTextSize(control, text);
			}

			public static global::System.Windows.Size ComputeControlTextSize(global::System.Windows.FrameworkElement control, string text)
			{
				if (control == null)
					return new global::System.Windows.Size(0.0, 0.0);

				object content = null;
				global::System.Windows.Controls.ContentControl contentControl = control as global::System.Windows.Controls.ContentControl;
				if (contentControl != null)
					content = contentControl.Content;
				else
				{
					global::System.Windows.Controls.TextBlock textBlock = control as global::System.Windows.Controls.TextBlock;
					if (textBlock != null)
						content = textBlock.Text;
				}

				string measuredText = string.IsNullOrEmpty(text) ? Convert.ToString(content) : text;
				global::System.Windows.Controls.Control wpfControl = control as global::System.Windows.Controls.Control;
				global::System.Windows.Controls.TextBlock sourceTextBlock = control as global::System.Windows.Controls.TextBlock;
				global::System.Windows.Controls.TextBlock measuringBlock = new global::System.Windows.Controls.TextBlock
				{
					Text = measuredText ?? string.Empty,
					FontFamily = wpfControl != null ? wpfControl.FontFamily : (sourceTextBlock != null ? sourceTextBlock.FontFamily : global::System.Windows.SystemFonts.MessageFontFamily),
					FontSize = wpfControl != null ? wpfControl.FontSize : (sourceTextBlock != null ? sourceTextBlock.FontSize : global::System.Windows.SystemFonts.MessageFontSize),
					FontStretch = wpfControl != null ? wpfControl.FontStretch : (sourceTextBlock != null ? sourceTextBlock.FontStretch : global::System.Windows.FontStretches.Normal),
					FontStyle = wpfControl != null ? wpfControl.FontStyle : (sourceTextBlock != null ? sourceTextBlock.FontStyle : global::System.Windows.FontStyles.Normal),
					FontWeight = wpfControl != null ? wpfControl.FontWeight : (sourceTextBlock != null ? sourceTextBlock.FontWeight : global::System.Windows.FontWeights.Normal)
				};
				measuringBlock.Measure(new global::System.Windows.Size(double.PositiveInfinity, double.PositiveInfinity));
				global::System.Windows.Size desiredSize = measuringBlock.DesiredSize;
				global::System.Windows.Thickness padding = wpfControl != null ? wpfControl.Padding : new global::System.Windows.Thickness();
				return new global::System.Windows.Size(Math.Ceiling(desiredSize.Width + padding.Left + padding.Right), Math.Ceiling(desiredSize.Height + padding.Top + padding.Bottom));
			}

			private static global::System.Windows.Media.Brush GetSkinBrush(string resourceName, global::System.Windows.Media.Brush fallback)
			{
				try
				{
					if (global::System.Windows.Application.Current != null)
					{
						object resource = global::System.Windows.Application.Current.TryFindResource(resourceName);
						global::System.Windows.Media.Brush brush = resource as global::System.Windows.Media.Brush;
						if (brush != null)
							return brush;

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
				if (solidColorBrush == null)
					return null;

				return solidColorBrush.Color;
			}

			private static double GetBrightness(global::System.Windows.Media.Color color)
			{
				return color.R * 0.299 + color.G * 0.587 + color.B * 0.114;
			}

			private static bool IsReadableOn(global::System.Windows.Media.Brush textBrush, global::System.Windows.Media.Brush backgroundBrush)
			{
				global::System.Windows.Media.Color? textColor = GetBrushColor(textBrush);
				global::System.Windows.Media.Color? backgroundColor = GetBrushColor(backgroundBrush);
				if (!textColor.HasValue || !backgroundColor.HasValue || textColor.Value.A == 0 || backgroundColor.Value.A == 0)
					return false;

				return Math.Abs(GetBrightness(textColor.Value) - GetBrightness(backgroundColor.Value)) >= 96.0;
			}
		}

		[Display(Name = "Popup: Enabled", Order = 0, GroupName = "Alerts")]
		public bool PopupEnabled { get; set; }
		[Display(Name = "Popup: Background Color", Order = 2, GroupName = "Alerts")]
		[XmlIgnore]
		public global::System.Windows.Media.Brush PopupBackgroundBrush { get; set; }
		[Browsable(false)]
		public string PopupBackgroundBrushSerialize
		{
			get
			{
				return Serialize.BrushToString(this.PopupBackgroundBrush);
			}
			set
			{
				this.PopupBackgroundBrush = Serialize.StringToBrush(value);
			}
		}
		[Display(Name = "Popup: Background Opacity", Order = 4, GroupName = "Alerts")]
		[Range(0, 100)]
		public int PopupBackgroundOpacity { get; set; }
		[Display(Name = "Popup: Text Color", Order = 6, GroupName = "Alerts")]
		[XmlIgnore]
		public global::System.Windows.Media.Brush PopupTextBrush { get; set; }
		[Browsable(false)]
		public string PopupTextBrushSerialize
		{
			get
			{
				return Serialize.BrushToString(this.PopupTextBrush);
			}
			set
			{
				this.PopupTextBrush = Serialize.StringToBrush(value);
			}
		}
		[Range(8, 2147483647)]
		[Display(Name = "Popup: Text Size", Order = 8, GroupName = "Alerts")]
		public int PopupTextSize { get; set; }
		[XmlIgnore]
		[Display(Name = "Popup: Button Color", Order = 10, GroupName = "Alerts")]
		public global::System.Windows.Media.Brush PopupButtonBrush { get; set; }
		[Browsable(false)]
		public string PopupButtonBrushSerialize
		{
			get
			{
				return Serialize.BrushToString(this.PopupButtonBrush);
			}
			set
			{
				this.PopupButtonBrush = Serialize.StringToBrush(value);
			}
		}
		[Display(Name = "Sound: Enabled", Order = 12, GroupName = "Alerts")]
		public bool SoundEnabled { get; set; }
		[Display(Name = "Sound: Bullish", Order = 14, GroupName = "Alerts")]
		[TypeConverter(typeof(DDQuantZone))]
		public string SoundBullish { get; set; }
		[TypeConverter(typeof(DDQuantZone))]
		[Display(Name = "Sound: Bearish", Order = 16, GroupName = "Alerts")]
		public string SoundBearish { get; set; }
		[Display(Name = "Sound: Rearm Enabled", Order = 18, GroupName = "Alerts")]
		public bool SoundRearmEnabled { get; set; }
		[Display(Name = "Sound: Rearm Seconds ", Order = 20, GroupName = "Alerts")]
		[Range(0, 2147483647)]
		public int SoundRearmSeconds { get; set; }
		[Display(Name = "Email: Enabled", Order = 22, GroupName = "Alerts")]
		public bool EmailEnabled { get; set; }
		[Display(Name = "Email: Receiver", Order = 24, GroupName = "Alerts")]
		public string EmailReceiver { get; set; }
		[Display(Name = "Marker: Enabled", Order = 26, GroupName = "Alerts")]
		public bool MarkerEnabled { get; set; }
		[Display(Name = "Marker: Rendering Method", Order = 28, GroupName = "Alerts", Description = "\"Custom\" rendering provides a better appearance, and is therefore recommended in most cases.\nIf your computer is weak or slow, please switch to \"Builtin\" rendering.")]
		public DDQuantZone_MarkerRenderingMethod MarkerRenderingMethod { get; set; }
		[XmlIgnore]
		[Display(Name = "Marker: Color Bullish", Order = 30, GroupName = "Alerts")]
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
		[XmlIgnore]
		[Display(Name = "Marker: Color Bearish", Order = 32, GroupName = "Alerts")]
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
		[Display(Name = "Marker: String Bullish", Order = 34, GroupName = "Alerts")]
		public string MarkerStringBullish { get; set; }
		[Display(Name = "Marker: String Bearish", Order = 36, GroupName = "Alerts")]
		public string MarkerStringBearish { get; set; }
		[Display(Name = "Marker: Font", Order = 38, GroupName = "Alerts")]
		public SimpleFont MarkerFont { get; set; }
		[Display(Name = "Marker: Offset", Order = 40, GroupName = "Alerts")]
		public int MarkerOffset { get; set; }
		[Range(0, 2147483647)]
		[Display(Name = "Alert Blocking (Seconds)", Order = 42, GroupName = "Alerts", Description = "The minimum interval between 2 consecutive alerts")]
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
				return "08 Apr 2026";
			}
		}
		[Display(Name = "Logo: Enabled", Order = 20, GroupName = "Developer")]
		public bool LogoEnabled { get; set; }
		[Display(Name = "Instruction: Enabled", Order = 30, GroupName = "Developer")]
		public bool InstructionEnabled { get; set; }
		[Display(Name = "Screen DPI", Order = 100, GroupName = "General")]
		[Range(99, 500)]
		public int ScreenDPI { get; set; }
		[Display(Name = "Plot: Enabled", Order = 0, GroupName = "Graphics")]
		public bool PlotEnabled { get; set; }
		[Display(Name = "Plot Bullish", Order = 2, GroupName = "Graphics")]
		[XmlIgnore]
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
		[Display(Name = "Plot Bearish", Order = 4, GroupName = "Graphics")]
		[XmlIgnore]
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
		[Display(Name = "Swing Point: Enabled", Order = 6, GroupName = "Graphics")]
		public bool SwingPointEnabled { get; set; }
		[XmlIgnore]
		[Display(Name = "Swing Point: Top", Order = 8, GroupName = "Graphics")]
		public global::System.Windows.Media.Brush SwingPointTop { get; set; }
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
		[Display(Name = "Swing Point: Bottom", Order = 10, GroupName = "Graphics")]
		[XmlIgnore]
		public global::System.Windows.Media.Brush SwingPointBottom { get; set; }
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
		[Display(Name = "Line: Enabled", Order = 12, GroupName = "Graphics")]
		public bool LineEnabled { get; set; }
		[Display(Name = "Line: Bullish", Order = 14, GroupName = "Graphics")]
		public Stroke LineBullish { get; set; }
		[Display(Name = "Line: Bearish", Order = 16, GroupName = "Graphics")]
		public Stroke LineBearish { get; set; }
		[Display(Name = "Level: Enabled", Order = 18, GroupName = "Graphics")]
		public bool LevelEnabled { get; set; }
		[Display(Name = "Zone: Display Mode", Order = 20, GroupName = "Graphics")]
		public DDQuantZone_DisplayMode ZoneDisplayMode { get; set; }
		[XmlIgnore]
		[Display(Name = "Zone: Bullish #1 Start", Order = 22, GroupName = "Graphics")]
		public global::System.Windows.Media.Brush ZoneBullish1Start { get; set; }
		[Browsable(false)]
		public string ZoneBullish1Start_Serialize
		{
			get
			{
				return Serialize.BrushToString(this.ZoneBullish1Start);
			}
			set
			{
				this.ZoneBullish1Start = Serialize.StringToBrush(value);
			}
		}
		[Display(Name = "Zone: Bullish #1 End", Order = 24, GroupName = "Graphics")]
		[XmlIgnore]
		public global::System.Windows.Media.Brush ZoneBullish1End { get; set; }
		[Browsable(false)]
		public string ZoneBullish1End_Serialize
		{
			get
			{
				return Serialize.BrushToString(this.ZoneBullish1End);
			}
			set
			{
				this.ZoneBullish1End = Serialize.StringToBrush(value);
			}
		}
		[XmlIgnore]
		[Display(Name = "Zone: Bullish #2 Start", Order = 26, GroupName = "Graphics")]
		public global::System.Windows.Media.Brush ZoneBullish2Start { get; set; }
		[Browsable(false)]
		public string ZoneBullish2Start_Serialize
		{
			get
			{
				return Serialize.BrushToString(this.ZoneBullish2Start);
			}
			set
			{
				this.ZoneBullish2Start = Serialize.StringToBrush(value);
			}
		}
		[XmlIgnore]
		[Display(Name = "Zone: Bullish #2 End", Order = 28, GroupName = "Graphics")]
		public global::System.Windows.Media.Brush ZoneBullish2End { get; set; }
		[Browsable(false)]
		public string ZoneBullish2End_Serialize
		{
			get
			{
				return Serialize.BrushToString(this.ZoneBullish2End);
			}
			set
			{
				this.ZoneBullish2End = Serialize.StringToBrush(value);
			}
		}
		[XmlIgnore]
		[Display(Name = "Zone: Bullish #3 Start", Order = 30, GroupName = "Graphics")]
		public global::System.Windows.Media.Brush ZoneBullish3Start { get; set; }
		[Browsable(false)]
		public string ZoneBullish3Start_Serialize
		{
			get
			{
				return Serialize.BrushToString(this.ZoneBullish3Start);
			}
			set
			{
				this.ZoneBullish3Start = Serialize.StringToBrush(value);
			}
		}
		[Display(Name = "Zone: Bullish #3 End", Order = 32, GroupName = "Graphics")]
		[XmlIgnore]
		public global::System.Windows.Media.Brush ZoneBullish3End { get; set; }
		[Browsable(false)]
		public string ZoneBullish3End_Serialize
		{
			get
			{
				return Serialize.BrushToString(this.ZoneBullish3End);
			}
			set
			{
				this.ZoneBullish3End = Serialize.StringToBrush(value);
			}
		}
		[Display(Name = "Zone: Bullish #4 Start", Order = 34, GroupName = "Graphics")]
		[XmlIgnore]
		public global::System.Windows.Media.Brush ZoneBullish4Start { get; set; }
		[Browsable(false)]
		public string ZoneBullish4Start_Serialize
		{
			get
			{
				return Serialize.BrushToString(this.ZoneBullish4Start);
			}
			set
			{
				this.ZoneBullish4Start = Serialize.StringToBrush(value);
			}
		}
		[Display(Name = "Zone: Bullish #4 End", Order = 34, GroupName = "Graphics")]
		[XmlIgnore]
		public global::System.Windows.Media.Brush ZoneBullish4End { get; set; }
		[Browsable(false)]
		public string ZoneBullish4End_Serialize
		{
			get
			{
				return Serialize.BrushToString(this.ZoneBullish4End);
			}
			set
			{
				this.ZoneBullish4End = Serialize.StringToBrush(value);
			}
		}
		[Display(Name = "Zone: Bearish #1 Start", Order = 38, GroupName = "Graphics")]
		[XmlIgnore]
		public global::System.Windows.Media.Brush ZoneBearish1Start { get; set; }
		[Browsable(false)]
		public string ZoneBearish1Start_Serialize
		{
			get
			{
				return Serialize.BrushToString(this.ZoneBearish1Start);
			}
			set
			{
				this.ZoneBearish1Start = Serialize.StringToBrush(value);
			}
		}
		[Display(Name = "Zone: Bearish #1 End", Order = 40, GroupName = "Graphics")]
		[XmlIgnore]
		public global::System.Windows.Media.Brush ZoneBearish1End { get; set; }
		[Browsable(false)]
		public string ZoneBearish1End_Serialize
		{
			get
			{
				return Serialize.BrushToString(this.ZoneBearish1End);
			}
			set
			{
				this.ZoneBearish1End = Serialize.StringToBrush(value);
			}
		}
		[Display(Name = "Zone: Bearish #2 Start", Order = 42, GroupName = "Graphics")]
		[XmlIgnore]
		public global::System.Windows.Media.Brush ZoneBearish2Start { get; set; }
		[Browsable(false)]
		public string ZoneBearish2Start_Serialize
		{
			get
			{
				return Serialize.BrushToString(this.ZoneBearish2Start);
			}
			set
			{
				this.ZoneBearish2Start = Serialize.StringToBrush(value);
			}
		}
		[XmlIgnore]
		[Display(Name = "Zone: Bearish #2 End", Order = 44, GroupName = "Graphics")]
		public global::System.Windows.Media.Brush ZoneBearish2End { get; set; }
		[Browsable(false)]
		public string ZoneBearish2End_Serialize
		{
			get
			{
				return Serialize.BrushToString(this.ZoneBearish2End);
			}
			set
			{
				this.ZoneBearish2End = Serialize.StringToBrush(value);
			}
		}
		[XmlIgnore]
		[Display(Name = "Zone: Bearish #3 Start", Order = 46, GroupName = "Graphics")]
		public global::System.Windows.Media.Brush ZoneBearish3Start { get; set; }
		[Browsable(false)]
		public string ZoneBearish3Start_Serialize
		{
			get
			{
				return Serialize.BrushToString(this.ZoneBearish3Start);
			}
			set
			{
				this.ZoneBearish3Start = Serialize.StringToBrush(value);
			}
		}
		[XmlIgnore]
		[Display(Name = "Zone: Bearish #3 End", Order = 48, GroupName = "Graphics")]
		public global::System.Windows.Media.Brush ZoneBearish3End { get; set; }
		[Browsable(false)]
		public string ZoneBearish3End_Serialize
		{
			get
			{
				return Serialize.BrushToString(this.ZoneBearish3End);
			}
			set
			{
				this.ZoneBearish3End = Serialize.StringToBrush(value);
			}
		}
		[XmlIgnore]
		[Display(Name = "Zone: Bearish #4 Start", Order = 50, GroupName = "Graphics")]
		public global::System.Windows.Media.Brush ZoneBearish4Start { get; set; }
		[Browsable(false)]
		public string ZoneBearish4Start_Serialize
		{
			get
			{
				return Serialize.BrushToString(this.ZoneBearish4Start);
			}
			set
			{
				this.ZoneBearish4Start = Serialize.StringToBrush(value);
			}
		}
		[XmlIgnore]
		[Display(Name = "Zone: Bearish #4 End", Order = 52, GroupName = "Graphics")]
		public global::System.Windows.Media.Brush ZoneBearish4End { get; set; }
		[Browsable(false)]
		public string ZoneBearish4End_Serialize
		{
			get
			{
				return Serialize.BrushToString(this.ZoneBearish4End);
			}
			set
			{
				this.ZoneBearish4End = Serialize.StringToBrush(value);
			}
		}
		[Range(0, 100)]
		[Display(Name = "Zone: Opacity", Order = 54, GroupName = "Graphics")]
		public int ZoneOpacity { get; set; }
		[Display(Name = "Minimized", Order = 0, GroupName = "Control Panel")]
		public bool CpMinimized { get; set; }
		[XmlIgnore]
		[Display(Name = "Title: Color", Order = 10, GroupName = "Control Panel")]
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
		[Display(Name = "Button Width", Order = 20, GroupName = "Control Panel")]
		public int CpButtonWidth { get; set; }
		[Range(1, 2147483647)]
		[Display(Name = "Text: Execution Size", Order = 30, GroupName = "Control Panel")]
		public int CpTextExecutionSize { get; set; }
		[XmlIgnore]
		[Display(Name = "Text: Execution Color", Order = 32, GroupName = "Control Panel")]
		public global::System.Windows.Media.Brush CpTextExecutionBrush { get; set; }
		[Browsable(false)]
		public string CpTextExecutionBrush_Serialize
		{
			get
			{
				return Serialize.BrushToString(this.CpTextExecutionBrush);
			}
			set
			{
				this.CpTextExecutionBrush = Serialize.StringToBrush(value);
			}
		}
		[Display(Name = "Text: Quantity Size", Order = 40, GroupName = "Control Panel")]
		[Range(1, 2147483647)]
		public int CpTextQuantitySize { get; set; }
		[Display(Name = "Text: Quantity Color", Order = 42, GroupName = "Control Panel")]
		[XmlIgnore]
		public global::System.Windows.Media.Brush CpTextQuantityBrush { get; set; }
		[Browsable(false)]
		public string CpTextQuantityBrush_Serialize
		{
			get
			{
				return Serialize.BrushToString(this.CpTextQuantityBrush);
			}
			set
			{
				this.CpTextQuantityBrush = Serialize.StringToBrush(value);
			}
		}
		[Range(1, 2147483647)]
		[Display(Name = "Text: Setting Size", Order = 44, GroupName = "Control Panel")]
		public int CpTextSettingSize { get; set; }
		[Display(Name = "Drag Bar Color", Order = 50, GroupName = "Control Panel")]
		[XmlIgnore]
		public global::System.Windows.Media.Brush CpDragBrush { get; set; }
		[Browsable(false)]
		public string CpDragBrush_Serialize
		{
			get
			{
				return Serialize.BrushToString(this.CpDragBrush);
			}
			set
			{
				this.CpDragBrush = Serialize.StringToBrush(value);
			}
		}
		[Display(Name = "Position: Alignment", Order = 60, GroupName = "Control Panel")]
		public NinjaTrader.NinjaScript.DrawingTools.TextPosition CpPositionAlignment
		{
			get
			{
				return this.controlPanelPositionAlignment;
			}
			set
			{
				if (value == NinjaTrader.NinjaScript.DrawingTools.TextPosition.TopLeft)
				{
					double num = 5.0;
					double num2 = (double)5f;
					this.CpPositionMarginLeft = num;
					this.CpPositionMarginTop = num2;
				}
				if (value == NinjaTrader.NinjaScript.DrawingTools.TextPosition.TopRight)
				{
					double num3 = 5.0;
					double num2 = (double)5f;
					this.CpPositionMarginRight = num3;
					this.CpPositionMarginTop = num2;
				}
				if (value == NinjaTrader.NinjaScript.DrawingTools.TextPosition.BottomRight)
				{
					double num4 = 5.0;
					double num2 = (double)5f;
					this.CpPositionMarginRight = num4;
					this.CpPositionMarginBottom = num2;
				}
				if (value == NinjaTrader.NinjaScript.DrawingTools.TextPosition.BottomLeft)
				{
					double num5 = 5.0;
					double num2 = (double)5f;
					this.CpPositionMarginLeft = num5;
					this.CpPositionMarginBottom = num2;
				}
				if (value == NinjaTrader.NinjaScript.DrawingTools.TextPosition.Center)
				{
					double num6 = 5.0;
					double num7 = (double)5f;
					this.CpPositionMarginBottom = num6;
					double num8 = num7;
					double num9 = (double)5f;
					this.CpPositionMarginRight = num8;
					double num10 = num9;
					double num2 = (double)5f;
					this.CpPositionMarginTop = num10;
					this.CpPositionMarginLeft = num2;
				}
				this.controlPanelPositionAlignment = value;
			}
		}
		[Display(Name = "Position: Margin Left", Order = 62, GroupName = "Control Panel")]
		public double CpPositionMarginLeft { get; set; }
		[Display(Name = "Position: Margin Top", Order = 64, GroupName = "Control Panel")]
		public double CpPositionMarginTop { get; set; }
		[Display(Name = "Position: Margin Right", Order = 66, GroupName = "Control Panel")]
		public double CpPositionMarginRight { get; set; }
		[Display(Name = "Position: Margin Bottom", Order = 68, GroupName = "Control Panel")]
		public double CpPositionMarginBottom { get; set; }
		[XmlIgnore]
		[Display(Name = "Indicator Window: Text Color", Order = 0, GroupName = "Windows")]
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
		[Display(Name = "Indicator Window: Left", Order = 10, GroupName = "Windows")]
		public double MainWindowLeft { get; set; }
		[Display(Name = "Indicator Window: Top", Order = 12, GroupName = "Windows")]
		public double MainWindowTop { get; set; }
		[Display(Name = "Indicator Window: Width", Order = 14, GroupName = "Windows")]
		public double MainWindowWidth { get; set; }
		[Display(Name = "Indicator Window: Height", Order = 16, GroupName = "Windows")]
		public double MainWindowHeight { get; set; }
		[Display(Name = "Info Window: Left", Order = 20, GroupName = "Windows")]
		public double InfoWindowLeft { get; set; }
		[Display(Name = "Info Window: Top", Order = 22, GroupName = "Windows")]
		public double InfoWindowTop { get; set; }
		[Display(Name = "Info Window: Width", Order = 24, GroupName = "Windows")]
		public double InfoWindowWidth { get; set; }
		[Display(Name = "Info Window: Height", Order = 26, GroupName = "Windows")]
		public double InfoWindowHeight { get; set; }
		[Display(Name = "Window: Background", Order = 28, GroupName = "Windows")]
		public string WindowBackground { get; set; }
		[Display(Name = "Default: Fast MA Type", Order = 0, GroupName = "Parameters")]
		
		public DD_MAType DefaultFastMAType { get; set; }
		[Display(Name = "Default: Fast Period", Order = 2, GroupName = "Parameters")]
		[NinjaScriptProperty]
		[Range(1, 2147483647)]
		public int DefaultFastPeriod { get; set; }
		
		[Display(Name = "Default: Slow MA Type", Order = 4, GroupName = "Parameters")]
		public DD_MAType DefaultSlowMAType { get; set; }
		[Display(Name = "Default: Slow Period", Order = 6, GroupName = "Parameters")]
		[NinjaScriptProperty]
		[Range(1, 2147483647)]
		public int DefaultSlowPeriod { get; set; }
		[Display(Name = "Mode", Order = 10, GroupName = "Parameters")]
		[RefreshProperties(RefreshProperties.All)]
		public DDQuantZone_Mode Mode { get; set; }
		[Display(Name = "Adverse Excursion", Order = 12, GroupName = "Parameters")]
		[NinjaScriptProperty]
		[Range(1, 100)]
		public int PAdverseExcursion { get; set; }
		[NinjaScriptProperty]
		[Display(Name = "Max Adverse Excursion", Order = 14, GroupName = "Parameters")]
		[Range(1, 100)]
		public int PMaxAdverseExcursion { get; set; }
		[Display(Name = "Max Favorable Excursion", Order = 16, GroupName = "Parameters")]
		[NinjaScriptProperty]
		[Range(1, 100)]
		public int PMaxFavorableExcursion { get; set; }
		[Display(Name = "Favorable Excursion", Order = 18, GroupName = "Parameters")]
		[NinjaScriptProperty]
		[Range(1, 100)]
		public int PFavorableExcursion { get; set; }
		[Display(Name = "Adverse Excursion", Order = 20, GroupName = "Parameters")]
		[PropertyEditor("NinjaTrader.Gui.Tools.StringStandardValuesEditorKey")]
		[RefreshProperties(RefreshProperties.All)]
		//[TypeConverter(typeof(DDQuantZone_AdverseExcursionConverter))]
		public DDQuantZone_AdverseExcursion GAdverseExcursion { get; set; }
		//[TypeConverter(typeof(DDQuantZone_AdverseExcursionConverter))]
		[RefreshProperties(RefreshProperties.All)]
		[PropertyEditor("NinjaTrader.Gui.Tools.StringStandardValuesEditorKey")]
		[Display(Name = "Max Adverse Excursion", Order = 22, GroupName = "Parameters")]
		public DDQuantZone_AdverseExcursion GMaxAdverseExcursion { get; set; }
		//[TypeConverter(typeof(DDQuantZone_FavorableExcursionConverter))]
		[RefreshProperties(RefreshProperties.All)]
		[Display(Name = "Max Favorable Excursion", Order = 24, GroupName = "Parameters")]
		[PropertyEditor("NinjaTrader.Gui.Tools.StringStandardValuesEditorKey")]
		public DDQuantZone_FavorableExcursion GMaxFavorableExcursion { get; set; }
		[Display(Name = "Favorable Excursion", Order = 26, GroupName = "Parameters")]
		[PropertyEditor("NinjaTrader.Gui.Tools.StringStandardValuesEditorKey")]
		[RefreshProperties(RefreshProperties.All)]
		//[TypeConverter(typeof(DDQuantZone_FavorableExcursionConverter))]
		public DDQuantZone_FavorableExcursion GFavorableExcursion { get; set; }
		[Range(100, 400)]
		[NinjaScriptProperty]
		[Display(Name = "Sample: Skip", Order = 28, GroupName = "Parameters")]
		public int SampleSkip { get; set; }
		[Display(Name = "Sample: Max", Order = 30, GroupName = "Parameters")]
		[NinjaScriptProperty]
		[Range(100, 10000)]
		public int SampleMax { get; set; }
		[NinjaScriptProperty]
		[Range(5, 100)]
		[Display(Name = "Sample: Range (Ticks)", Order = 32, GroupName = "Parameters")]
		public int SampleRange { get; set; }
		[Display(Name = "Sample: Steps", Order = 34, GroupName = "Parameters")]
		[NinjaScriptProperty]
		[Range(10, 50)]
		public int SampleSteps { get; set; }
		[NinjaScriptProperty]
		[Range(0, 2147483647)]
		[Display(Name = "Probability Zone Line (Bars)", Order = 40, GroupName = "Parameters")]
		public int ProbabilityZoneLine { get; set; }
		[Display(Name = "Enabled", Order = 0, GroupName = "Toggle")]
		public bool ToggleEnabled { get; set; }
		[XmlIgnore]
		[Display(Name = "Background: On", Order = 10, GroupName = "Toggle")]
		public global::System.Windows.Media.Brush ToggleBackBrushOn { get; set; }
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
		[Display(Name = "Background: Off", Order = 12, GroupName = "Toggle")]
		[XmlIgnore]
		public global::System.Windows.Media.Brush ToggleBackBrushOff { get; set; }
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
		[Display(Name = "Text: String", Order = 20, GroupName = "Toggle")]
		public string ToggleTextString { get; set; }
		[XmlIgnore]
		[Display(Name = "Text: Color", Order = 22, GroupName = "Toggle")]
		public global::System.Windows.Media.Brush ToggleTextBrush { get; set; }
		[Browsable(false)]
		public string ToggleTextBrush_Serialize
		{
			get
			{
				return Serialize.BrushToString(this.ToggleTextBrush);
			}
			set
			{
				this.ToggleTextBrush = Serialize.StringToBrush(value);
			}
		}
		[Display(Name = "Text: Size", Order = 24, GroupName = "Toggle")]
		[Range(1, 2147483647)]
		public int ToggleTextSize { get; set; }
		[XmlIgnore]
		[Display(Name = "Drag Bar: Color", Order = 30, GroupName = "Toggle")]
		public global::System.Windows.Media.Brush ToggleDragBrush { get; set; }
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
					double num = 5.0;
					double num2 = (double)5f;
					this.TogglePositionMarginLeft = num;
					this.TogglePositionMarginTop = num2;
				}
				if (value == DD_TextPosition.TopRight)
				{
					double num3 = 5.0;
					double num2 = (double)5f;
					this.TogglePositionMarginRight = num3;
					this.TogglePositionMarginTop = num2;
				}
				if (value == DD_TextPosition.BottomRight)
				{
					double num4 = 5.0;
					double num2 = (double)5f;
					this.TogglePositionMarginRight = num4;
					this.TogglePositionMarginBottom = num2;
				}
				if (value == DD_TextPosition.BottomLeft)
				{
					double num5 = 5.0;
					double num2 = (double)5f;
					this.TogglePositionMarginLeft = num5;
					this.TogglePositionMarginBottom = num2;
				}
				if (value == DD_TextPosition.Center)
				{
					double num6 = 5.0;
					double num7 = (double)5f;
					this.TogglePositionMarginBottom = num6;
					double num8 = num7;
					double num9 = (double)5f;
					this.TogglePositionMarginRight = num8;
					double num10 = num9;
					double num2 = (double)5f;
					this.TogglePositionMarginTop = num10;
					this.TogglePositionMarginLeft = num2;
				}
				this.togglePositionAlignment = value;
			}
		}
		[Display(Name = "Position: Margin Left", Order = 42, GroupName = "Toggle")]
		public double TogglePositionMarginLeft { get; set; }
		[Display(Name = "Position: Margin Top", Order = 44, GroupName = "Toggle")]
		public double TogglePositionMarginTop { get; set; }
		[Display(Name = "Position: Margin Right", Order = 46, GroupName = "Toggle")]
		public double TogglePositionMarginRight { get; set; }
		[Display(Name = "Position: Margin Bottom", Order = 48, GroupName = "Toggle")]
		public double TogglePositionMarginBottom { get; set; }
		[Display(Name = "Z Order", Order = 0, GroupName = "Special")]
		public int IndicatorZOrder { get; set; }
		[Display(Name = "User Note", Order = 10, GroupName = "Special")]
		public string UserNote { get; set; }
		[Display(Name = "Switched On: All", Order = 0, GroupName = "Critical")]
		public bool SwitchedOnAll { get; set; }
		[Display(Name = "Switched On: Auto-fit", Order = 4, GroupName = "Critical")]
		public bool SwitchedOnAutofit { get; set; }
		[Display(Name = "Documents Path", Order = 10, GroupName = "Critical")]
		public string DocumentsPath { get; set; }
		[Display(Name = "Namespace", Order = 20, GroupName = "Critical")]
		public string Namespace { get; set; }
		[Display(Name = "Indicator: Name", Order = 30, GroupName = "Critical")]
		public string IndicatorName { get; set; }
		[Display(Name = "Indicator: Setting", Order = 32, GroupName = "Critical")]
		public string IndicatorSetting { get; set; }
		[Display(Name = "Input Series Index", Order = 40, GroupName = "Critical")]
		public int InputSeriesIndex { get; set; }
		[Display(Name = "Plot Index", Order = 50, GroupName = "Critical")]
		public int PlotIndex { get; set; }
		[Display(Name = "Value: Bullish", Order = 60, GroupName = "Critical")]
		public double ValueBullish { get; set; }
		[Display(Name = "Value: Bearish", Order = 62, GroupName = "Critical")]
		public double ValueBearish { get; set; }
		[Display(Name = "Operator: Bullish", Order = 64, GroupName = "Critical")]
		public DDQuantZone_Operators OperatorBullish { get; set; }
		[Display(Name = "Operator: Bearish", Order = 66, GroupName = "Critical")]
		public DDQuantZone_Operators OperatorBearish { get; set; }
		[Browsable(false)]
		[XmlIgnore]
		public Series<double> MaxFavorableExcursion
		{
			get
			{
				return base.Values[0];
			}
		}
		[XmlIgnore]
		[Browsable(false)]
		public Series<double> FavorableExcursion
		{
			get
			{
				return base.Values[1];
			}
		}
		[XmlIgnore]
		[Browsable(false)]
		public Series<double> Signal_State
		{
			get
			{
				return base.Values[2];
			}
		}
		[XmlIgnore]
		[Browsable(false)]
		public Series<double> Signal_Zone
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
				return "DDQuantZone";
			}
		}
		protected override void OnStateChange()
		{
			try
			{
				if (base.State == State.SetDefaults)
				{
					base.Description = string.Empty;
					base.Name = "DDQuantZone";
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
					base.IsAutoScale = true;
					this.PopupEnabled = false;
					this.PopupBackgroundBrush = global::System.Windows.Media.Brushes.DodgerBlue;
					this.PopupBackgroundOpacity = 10;
					this.PopupTextBrush = global::System.Windows.Media.Brushes.Black;
					this.PopupTextSize = 16;
					this.PopupButtonBrush = global::System.Windows.Media.Brushes.Transparent;
					this.SoundEnabled = false;
					this.SoundBullish = "Alert4.wav";
					this.SoundBearish = "Alert3.wav";
					this.SoundRearmEnabled = true;
					this.SoundRearmSeconds = 5;
					this.EmailEnabled = false;
					this.EmailReceiver = "receiver@example.com";
					this.MarkerEnabled = true;
					this.MarkerRenderingMethod = DDQuantZone_MarkerRenderingMethod.Custom;
					this.MarkerBrushBullish = global::System.Windows.Media.Brushes.DodgerBlue;
					this.MarkerBrushBearish = global::System.Windows.Media.Brushes.HotPink;
					this.MarkerStringBullish = "❄";
					this.MarkerStringBearish = "❄";
					this.MarkerFont = new SimpleFont("Arial", 15);
					this.MarkerOffset = 10;
					this.AlertBlockingSeconds = 60;
					this.LogoEnabled = true;
					this.InstructionEnabled = true;
					this.ScreenDPI = 99;
					this.PlotEnabled = true;
					this.PlotBullish = global::System.Windows.Media.Brushes.DodgerBlue;
					this.PlotBearish = global::System.Windows.Media.Brushes.HotPink;
					this.SwingPointEnabled = true;
					this.SwingPointTop = global::System.Windows.Media.Brushes.HotPink;
					this.SwingPointBottom = global::System.Windows.Media.Brushes.DodgerBlue;
					this.LineEnabled = false;
					this.LineBullish = new Stroke(global::System.Windows.Media.Brushes.DodgerBlue, DashStyleHelper.Solid, 1f);
					this.LineBearish = new Stroke(global::System.Windows.Media.Brushes.HotPink, DashStyleHelper.Solid, 1f);
					this.LevelEnabled = true;
					this.ZoneDisplayMode = DDQuantZone_DisplayMode.Gradient;
					this.ZoneBullish1Start = global::System.Windows.Media.Brushes.DeepSkyBlue;
					this.ZoneBullish1End = global::System.Windows.Media.Brushes.DodgerBlue;
					this.ZoneBullish2Start = global::System.Windows.Media.Brushes.Cyan;
					this.ZoneBullish2End = global::System.Windows.Media.Brushes.DeepSkyBlue;
					this.ZoneBullish3Start = global::System.Windows.Media.Brushes.SkyBlue;
					this.ZoneBullish3End = global::System.Windows.Media.Brushes.Cyan;
					this.ZoneBullish4Start = global::System.Windows.Media.Brushes.PaleTurquoise;
					this.ZoneBullish4End = global::System.Windows.Media.Brushes.SkyBlue;
					this.ZoneBearish1Start = global::System.Windows.Media.Brushes.MediumVioletRed;
					this.ZoneBearish1End = global::System.Windows.Media.Brushes.DeepPink;
					this.ZoneBearish2Start = global::System.Windows.Media.Brushes.Magenta;
					this.ZoneBearish2End = global::System.Windows.Media.Brushes.MediumVioletRed;
					this.ZoneBearish3Start = global::System.Windows.Media.Brushes.Violet;
					this.ZoneBearish3End = global::System.Windows.Media.Brushes.Magenta;
					this.ZoneBearish4Start = global::System.Windows.Media.Brushes.Plum;
					this.ZoneBearish4End = global::System.Windows.Media.Brushes.Violet;
					this.ZoneOpacity = 30;
					this.DefaultFastMAType = DD_MAType.SMA;
					this.DefaultFastPeriod = 10;
					this.DefaultSlowMAType = DD_MAType.SMA;
					this.DefaultSlowPeriod = 20;
					this.Mode = DDQuantZone_Mode.Percentile;
					this.PAdverseExcursion = 90;
					this.PMaxAdverseExcursion = 96;
					this.PMaxFavorableExcursion = 80;
					this.PFavorableExcursion = 60;
					this.GAdverseExcursion = DDQuantZone_AdverseExcursion.L84;
					this.GMaxAdverseExcursion = DDQuantZone_AdverseExcursion.L97;
					this.GMaxFavorableExcursion = DDQuantZone_FavorableExcursion.T84;
					this.GFavorableExcursion = DDQuantZone_FavorableExcursion.T50;
					this.SampleSkip = 200;
					this.SampleMax = 5000;
					this.SampleRange = 20;
					this.SampleSteps = 50;
					this.ProbabilityZoneLine = 200;
					this.ToggleEnabled = true;
					this.ToggleBackBrushOn = global::System.Windows.Media.Brushes.DodgerBlue;
					this.ToggleBackBrushOff = global::System.Windows.Media.Brushes.Silver;
					this.ToggleTextString = "QuantZone";
					this.ToggleTextBrush = global::System.Windows.Media.Brushes.White;
					this.ToggleTextSize = 10;
					this.ToggleDragBrush = global::System.Windows.Media.Brushes.LimeGreen;
					this.TogglePositionAlignment = DD_TextPosition.TopLeft;
					this.TogglePositionMarginLeft = 5.0;
					this.TogglePositionMarginTop = 5.0;
					this.TogglePositionMarginRight = 5.0;
					this.TogglePositionMarginBottom = 5.0;
					this.CpMinimized = false;
					this.CpTitleColor = global::System.Windows.Media.Brushes.LimeGreen;
					this.CpTitleTextBrush = global::System.Windows.Media.Brushes.White;
					this.CpButtonWidth = 80;
					this.CpTextExecutionSize = 13;
					this.CpTextExecutionBrush = global::System.Windows.Media.Brushes.White;
					this.CpTextQuantitySize = 11;
					this.CpTextQuantityBrush = global::System.Windows.Media.Brushes.Black;
					this.CpTextSettingSize = 13;
					this.CpDragBrush = global::System.Windows.Media.Brushes.LimeGreen;
					this.CpPositionAlignment = NinjaTrader.NinjaScript.DrawingTools.TextPosition.TopLeft;
					this.CpPositionMarginLeft = 5.0;
					this.CpPositionMarginTop = 35.0;
					this.CpPositionMarginRight = 5.0;
					this.CpPositionMarginBottom = 5.0;
					this.MainWindowTextColor = global::System.Windows.Media.Brushes.Transparent;
					double num = 30.0;
					double num2 = (double)30f;
					this.MainWindowHeight = num;
					double num3 = num2;
					double num4 = (double)30f;
					this.MainWindowWidth = num3;
					double num5 = num4;
					double num6 = (double)30f;
					this.MainWindowTop = num5;
					this.MainWindowLeft = num6;
					double num7 = 30.0;
					num6 = (double)30f;
					this.InfoWindowTop = num7;
					this.InfoWindowLeft = num6;
					this.InfoWindowWidth = 900.0;
					this.InfoWindowHeight = 560.0;
					this.WindowBackground = string.Empty;
					this.SwitchedOnAll = true;
					this.SwitchedOnAutofit = true;
					this.DocumentsPath = string.Empty;
					this.Namespace = string.Empty;
					this.IndicatorName = string.Empty;
					this.IndicatorSetting = string.Empty;
					this.InputSeriesIndex = 0;
					this.PlotIndex = 0;
					this.ValueBullish = 1.0;
					this.ValueBearish = -1.0;
					this.OperatorBullish = DDQuantZone_Operators.Equal;
					this.OperatorBearish = DDQuantZone_Operators.Equal;
					this.IndicatorZOrder = -100;
					this.UserNote = "instrument (period)";
					base.AddPlot(new Stroke(global::System.Windows.Media.Brushes.Goldenrod, DashStyleHelper.Solid, 2f), PlotStyle.TriangleRight, "Max Favorable Excursion");
					base.AddPlot(new Stroke(global::System.Windows.Media.Brushes.Goldenrod, DashStyleHelper.Solid, 1f), PlotStyle.Square, "Favorable Excursion");
					base.AddPlot(global::System.Windows.Media.Brushes.Transparent, "Signal: State");
					base.AddPlot(global::System.Windows.Media.Brushes.Transparent, "Signal: Zone");
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
						this.DocumentsPath = global::System.IO.Path.Combine(DDResources_GlobalConstantAndFunction.GetDefaultDocumentsPath(), "NinjaTrader 8\\bin\\Custom\\");
					}
					Type typeFromHandle = typeof(DDQuantZone);
					string text = ((typeFromHandle == null) ? string.Empty : typeFromHandle.Name);
					if (!string.IsNullOrWhiteSpace(text))
					{
						this.listIndicatorExcluded.Insert(0, text);
					}
					this.dictMethodInfo = new Dictionary<string, MethodInfo>();
					this.sortedListIndicatorItem = new SortedList<string, DDQuantZone.IndicatorItem>();
					this.isCustomMarkerRenderingMethod = this.MarkerRenderingMethod == DDQuantZone_MarkerRenderingMethod.Custom;
					if (this.isCustomMarkerRenderingMethod)
					{
						this.dictMarkerInfo = new Dictionary<int, bool>();
					}
					this.GetAllMethods();
					int num8 = Math.Min(this.PAdverseExcursion, this.PMaxAdverseExcursion);
					int num9 = Math.Max(this.PAdverseExcursion, this.PMaxAdverseExcursion);
					this.PAdverseExcursion = num8;
					this.PMaxAdverseExcursion = num9;
					int num10 = Math.Max(this.PMaxFavorableExcursion, this.PFavorableExcursion);
					int num11 = Math.Min(this.PMaxFavorableExcursion, this.PFavorableExcursion);
					this.PMaxFavorableExcursion = num10;
					this.PFavorableExcursion = num11;
					DDQuantZone_AdverseExcursion gadverseExcursion = this.GAdverseExcursion;
					int gmaxAdverseExcursion = (int)this.GMaxAdverseExcursion;
					int num12 = Math.Min((int)gadverseExcursion, gmaxAdverseExcursion);
					int num13 = Math.Max((int)gadverseExcursion, gmaxAdverseExcursion);
					this.GAdverseExcursion = (DDQuantZone_AdverseExcursion)num12;
					this.GMaxAdverseExcursion = (DDQuantZone_AdverseExcursion)num13;
					DDQuantZone_FavorableExcursion gmaxFavorableExcursion = this.GMaxFavorableExcursion;
					int gfavorableExcursion = (int)this.GFavorableExcursion;
					int num14 = Math.Min((int)gmaxFavorableExcursion, gfavorableExcursion);
					int num15 = Math.Max((int)gmaxFavorableExcursion, gfavorableExcursion);
					this.GMaxFavorableExcursion = (DDQuantZone_FavorableExcursion)num14;
					this.GFavorableExcursion = (DDQuantZone_FavorableExcursion)num15;
					this.needReloadChart = false;
					this.indicatorBase = new IndicatorBase();
					this.listPlotOrDataSeries = new List<DDQuantZone.PlotOrDataSeriesInfo>();
					this.listSwingPoint = new List<DDQuantZone.SwingPoint>();
					this.isZoneDisable = this.ZoneDisplayMode == DDQuantZone_DisplayMode.Disabled;
					this.listLineInfo = new List<DDQuantZone.LineInfo>();
					if (!this.isZoneDisable || this.LevelEnabled)
					{
						this.sortedListZoneInfoActive = new SortedList<int, DDQuantZone.ZoneInfo>();
						this.sortedListZoneInfoInactive = new SortedList<int, DDQuantZone.ZoneInfo>();
					}
					this.listMarketMetric = new List<DDQuantZone.MarketMetric>();
					this.dictPlotInfo = new Dictionary<int, DDQuantZone.PlotInfo>();
					this.mmMin = new DDQuantZone.MarketMetric("Min");
					this.listMarketMetric.Add(this.mmMin);
					this.mmMax = new DDQuantZone.MarketMetric("Max");
					this.listMarketMetric.Add(this.mmMax);
					this.mmTotalSample = new DDQuantZone.MarketMetric("Total Sample");
					this.listMarketMetric.Add(this.mmTotalSample);
					string text2 = string.Empty;
					string text3 = string.Empty;
					string text4 = string.Empty;
					string text5 = "MFE ";
					string text6 = "FE ";
					this.isPercentMode = this.Mode == DDQuantZone_Mode.Percentile;
					if (this.isPercentMode)
					{
						text2 = "Mean Probability";
						text3 = "AE " + this.PAdverseExcursion.ToString();
						text4 = "MAE " + this.PMaxAdverseExcursion.ToString();
						text5 += this.PMaxFavorableExcursion.ToString();
						text6 += this.PFavorableExcursion.ToString();
					}
					else
					{
						text2 = "Expected Value E(X)";
						text3 += "Level 84.2% => E(X) + 1S";
						text4 += "Level 97.8% => E(X) + 2S";
						text5 += NinjaTrader.NinjaScript.Indicators.DimDim.DDQuantZone.strFavorableExcursionArr[num14];
						text6 += NinjaTrader.NinjaScript.Indicators.DimDim.DDQuantZone.strFavorableExcursionArr[num15];
						this.mmStdDeviation = new DDQuantZone.MarketMetric("Std Deviation");
						this.listMarketMetric.Add(this.mmStdDeviation);
					}
					this.mmAverage = new DDQuantZone.MarketMetric(text2);
					this.listMarketMetric.Add(this.mmAverage);
					this.mmLevel1 = new DDQuantZone.MarketMetric(text3);
					this.listMarketMetric.Add(this.mmLevel1);
					this.mmLevel2 = new DDQuantZone.MarketMetric(text4);
					this.listMarketMetric.Add(this.mmLevel2);
					if (!this.isPercentMode)
					{
						this.mmLevel3 = new DDQuantZone.MarketMetric("Level 99.2% => E(X) + 3S");
						this.listMarketMetric.Add(this.mmLevel3);
					}
					this.mmMaxFavorableExcursion = new DDQuantZone.MarketMetric(text5);
					this.listMarketMetric.Add(this.mmMaxFavorableExcursion);
					this.mmFavorableExcursion = new DDQuantZone.MarketMetric(text6);
					this.listMarketMetric.Add(this.mmFavorableExcursion);
					this.mmAdverseExcursionPanel = new DDQuantZone.MarketMetric(string.Empty);
					this.mmMaxAdverseExcursionPanel = new DDQuantZone.MarketMetric(string.Empty);
					this.mmMaxFavorableExcursionPanel = new DDQuantZone.MarketMetric(string.Empty);
					this.mmFavorableExcursionPanel = new DDQuantZone.MarketMetric(string.Empty);
					if (!this.isZoneDisable)
					{
						if (this.ZoneDisplayMode == DDQuantZone_DisplayMode.Gradient)
						{
							this.zoneBullishGradientStop1 = this.CreateGradientStopArr(this.ZoneBullish1Start, this.ZoneBullish1End, this.ZoneOpacity);
							this.zoneBearishGradientStop1 = this.CreateGradientStopArr(this.ZoneBearish1Start, this.ZoneBearish1End, this.ZoneOpacity);
							this.zoneBullishGradientStop2 = this.CreateGradientStopArr(this.ZoneBullish2Start, this.ZoneBullish2End, this.ZoneOpacity);
							this.zoneBearishGradientStop2 = this.CreateGradientStopArr(this.ZoneBearish2Start, this.ZoneBearish2End, this.ZoneOpacity);
							this.zoneBullishGradientStop3 = this.CreateGradientStopArr(this.ZoneBullish3Start, this.ZoneBullish3End, this.ZoneOpacity);
							this.zoneBearishGradientStop3 = this.CreateGradientStopArr(this.ZoneBearish3Start, this.ZoneBearish3End, this.ZoneOpacity);
							this.zoneBullishGradientStop4 = this.CreateGradientStopArr(this.ZoneBullish4Start, this.ZoneBullish4End, this.ZoneOpacity);
							this.zoneBearishGradientStop4 = this.CreateGradientStopArr(this.ZoneBearish4Start, this.ZoneBearish4End, this.ZoneOpacity);
						}
						else
						{
							this.zoneBullish1 = DD_BrushManager.CreateOpacityBrush(this.ZoneBullish1Start, this.ZoneOpacity);
							this.zoneBearish1 = DD_BrushManager.CreateOpacityBrush(this.ZoneBearish1Start, this.ZoneOpacity);
							this.zoneBullish2 = DD_BrushManager.CreateOpacityBrush(this.ZoneBullish2Start, this.ZoneOpacity);
							this.zoneBearish2 = DD_BrushManager.CreateOpacityBrush(this.ZoneBearish2Start, this.ZoneOpacity);
							this.zoneBullish3 = DD_BrushManager.CreateOpacityBrush(this.ZoneBullish3Start, this.ZoneOpacity);
							this.zoneBearish3 = DD_BrushManager.CreateOpacityBrush(this.ZoneBearish3Start, this.ZoneOpacity);
							this.zoneBullish4 = DD_BrushManager.CreateOpacityBrush(this.ZoneBullish4Start, this.ZoneOpacity);
							this.zoneBearish4 = DD_BrushManager.CreateOpacityBrush(this.ZoneBearish4Start, this.ZoneOpacity);
						}
					}
				}
				else if (base.State == State.DataLoaded)
				{
					this.LoadIndicator();
					this.listSampleInfo = new List<DDQuantZone.SampleInfo>();
					for (int i = 0; i < this.SampleSteps; i++)
					{
						int num16 = ((i == 0) ? 0 : (i * this.SampleRange + 1));
						int num17 = (i + 1) * this.SampleRange;
						DDQuantZone.SampleInfo sampleInfo = new DDQuantZone.SampleInfo
						{
							Index = i,
							From = num16,
							To = num17,
							SampleRange = num16.ToString() + " - " + num17.ToString()
						};
						this.listSampleInfo.Add(sampleInfo);
					}
				}
				else if (base.State == State.Historical)
				{
						bool flag = this.WindowBackground.Length == 7 && this.WindowBackground.All(new Func<char, bool>("#0123456789abcdefABCDEF".Contains<char>));
						if (string.IsNullOrWhiteSpace(this.WindowBackground) || (!flag && !this.MainWindowTextColor.IsTransparent()))
						{
							this.MainWindowTextColor = global::System.Windows.Media.Brushes.Transparent;
						}
						if (this.MainWindowTextColor.IsTransparent())
						{
							this.isLightTheme = DDResources_GlobalConstantAndFunction.IsLightTheme().GetValueOrDefault();
							this.MainWindowTextColor = (this.isLightTheme ? global::System.Windows.Media.Brushes.Black : global::System.Windows.Media.Brushes.LightGray);
							this.WindowBackground = (this.isLightTheme ? "#FFFFFF" : "#1E1E1E");
						}
						else
						{
							this.isLightTheme = this.WindowBackground == "#FFFFFF";
						}
						this.mainWindowBorderColor = (this.isLightTheme ? global::System.Windows.Media.Brushes.Black : global::System.Windows.Media.Brushes.Gray);
						NinjaTrader.NinjaScript.Indicators.DimDim.DDQuantZone.windowBackground = (global::System.Windows.Media.SolidColorBrush)new BrushConverter().ConvertFrom(this.WindowBackground);
						NinjaTrader.NinjaScript.Indicators.DimDim.DDQuantZone.windowBackground.Freeze();
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
								this.barOutlineWidth = base.ChartBars.Properties.ChartStyle.Stroke2.Width;
								if (this.controlPanel == null)
								{
									Thickness thickness2 = new Thickness(this.CpPositionMarginLeft, this.CpPositionMarginTop, this.CpPositionMarginRight, this.CpPositionMarginBottom);
									string text7 = (string.IsNullOrEmpty(this.IndicatorName) ? "Select an indicator..." : this.IndicatorName);
									this.controlPanel = new DDQuantZone.DraggablePanel(text7, this.mmAdverseExcursionPanel, this.mmMaxAdverseExcursionPanel, this.mmMaxFavorableExcursionPanel, this.mmFavorableExcursionPanel, this.CpMinimized, this.CpButtonWidth, this.CpTitleColor, this.CpTitleTextBrush, (double)this.CpTextExecutionSize, this.CpTextExecutionBrush, (double)this.CpTextSettingSize, this.CpDragBrush, this.CpPositionAlignment, thickness2);
									this.controlPanel.nudAdverseExcursion.Value = (double)this.PAdverseExcursion;
									this.controlPanel.nudMaxAdverseExcursion.Value = (double)this.PMaxAdverseExcursion;
									this.controlPanel.nudMaxFavorableExcursion.Value = (double)this.PMaxFavorableExcursion;
									this.controlPanel.nudFavorableExcursion.Value = (double)this.PFavorableExcursion;
									this.controlPanel.cbAdverseExcursion.SelectedIndex = (int)this.GAdverseExcursion;
									this.controlPanel.cbMaxAdverseExcursion.SelectedIndex = (int)this.GMaxAdverseExcursion;
									this.controlPanel.cbMaxFavorableExcursion.SelectedIndex = (int)this.GMaxFavorableExcursion;
									this.controlPanel.cbFavorableExcursion.SelectedIndex = (int)this.GFavorableExcursion;
									this.controlPanel.cbMode.SelectedIndex = (int)this.Mode;
									if (this.Mode == DDQuantZone_Mode.Gaussian)
									{
										UIElement nudAdverseExcursion = this.controlPanel.nudAdverseExcursion;
										this.controlPanel.nudMaxAdverseExcursion.Visibility = Visibility.Collapsed;
										nudAdverseExcursion.Visibility = Visibility.Collapsed;
										UIElement nudMaxFavorableExcursion = this.controlPanel.nudMaxFavorableExcursion;
										this.controlPanel.nudFavorableExcursion.Visibility = Visibility.Collapsed;
										nudMaxFavorableExcursion.Visibility = Visibility.Collapsed;
									}
									else
									{
										UIElement cbAdverseExcursion = this.controlPanel.cbAdverseExcursion;
										this.controlPanel.cbMaxAdverseExcursion.Visibility = Visibility.Collapsed;
										cbAdverseExcursion.Visibility = Visibility.Collapsed;
										UIElement cbMaxFavorableExcursion = this.controlPanel.cbMaxFavorableExcursion;
										this.controlPanel.cbFavorableExcursion.Visibility = Visibility.Collapsed;
										cbMaxFavorableExcursion.Visibility = Visibility.Collapsed;
									}
									this.controlPanel.btnTitle.Click += this.OnBtnTitleClick;
									this.controlPanel.btnInfo.Click += this.OnBtnInfoClick;
									this.controlPanel.drag.DragDelta += this.OnControlPanelDragDelta;
									this.controlPanel.btnMini.Click += this.OnControlPanelBtnMiniClick;
									this.controlPanel.drag.MouseDoubleClick += this.OnControlPanelDragDoubleClick;
									this.controlPanel.cbMode.SelectionChanged += this.OnCbModeChange;
									this.controlPanel.nudAdverseExcursion.NUDTextBox.TextChanged += new TextChangedEventHandler(this.OnNudAdverseExcursionChange);
									this.controlPanel.nudMaxAdverseExcursion.NUDTextBox.TextChanged += new TextChangedEventHandler(this.OnNudMaxAdverseExcursionChange);
									this.controlPanel.nudMaxFavorableExcursion.NUDTextBox.TextChanged += new TextChangedEventHandler(this.OnNudMaxFavorableExcursionChange);
									this.controlPanel.nudFavorableExcursion.NUDTextBox.TextChanged += new TextChangedEventHandler(this.OnNudFavorableExcursionChange);
									this.controlPanel.cbAdverseExcursion.SelectionChanged += this.OnCbAdverseExcursionChange;
									this.controlPanel.cbMaxAdverseExcursion.SelectionChanged += this.OnCbMaxAdverseExcursionChange;
									this.controlPanel.cbMaxFavorableExcursion.SelectionChanged += this.OnCbMaxFavorableExcursionChange;
									this.controlPanel.cbFavorableExcursion.SelectionChanged += this.OnCbFavorableExcursionChange;
									base.UserControlCollection.Add(this.controlPanel);
								}
								if (this.yesNoMessageWindow == null)
								{
									this.yesNoMessageWindow = new ShowYesNoMessageWindow(NinjaTrader.NinjaScript.Indicators.DimDim.DDQuantZone.windowBackground);
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
							if (this.windowIndicator != null)
							{
								this.windowIndicator.Close();
								this.windowIndicator = null;
							}
							if (this.windowInfo != null)
							{
								this.windowInfo.Close();
								this.windowInfo = null;
							}
							if (this.controlPanel != null)
							{
								this.controlPanel.btnTitle.Click -= this.OnBtnTitleClick;
								this.controlPanel.btnInfo.Click -= this.OnBtnInfoClick;
								this.controlPanel.drag.DragDelta -= this.OnControlPanelDragDelta;
								this.controlPanel.btnMini.Click -= this.OnControlPanelBtnMiniClick;
								this.controlPanel.drag.MouseDoubleClick -= this.OnControlPanelDragDoubleClick;
								this.controlPanel.nudAdverseExcursion.NUDTextBox.TextChanged -= new TextChangedEventHandler(this.OnNudAdverseExcursionChange);
								this.controlPanel.nudMaxAdverseExcursion.NUDTextBox.TextChanged -= new TextChangedEventHandler(this.OnNudMaxAdverseExcursionChange);
								this.controlPanel.nudMaxFavorableExcursion.NUDTextBox.TextChanged -= new TextChangedEventHandler(this.OnNudMaxFavorableExcursionChange);
								this.controlPanel.nudFavorableExcursion.NUDTextBox.TextChanged -= new TextChangedEventHandler(this.OnNudFavorableExcursionChange);
								this.controlPanel.cbMode.SelectionChanged -= this.OnCbModeChange;
								this.controlPanel.cbAdverseExcursion.SelectionChanged -= this.OnCbAdverseExcursionChange;
								this.controlPanel.cbMaxAdverseExcursion.SelectionChanged -= this.OnCbMaxAdverseExcursionChange;
								this.controlPanel.cbMaxFavorableExcursion.SelectionChanged -= this.OnCbMaxFavorableExcursionChange;
								this.controlPanel.cbFavorableExcursion.SelectionChanged -= this.OnCbFavorableExcursionChange;
								this.controlPanel = null;
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
							if (this.scottPlotHelpWindow != null)
							{
								this.scottPlotHelpWindow.Close();
								this.scottPlotHelpWindow = null;
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
					Position = 0f,
					Color = color
				},
				new global::SharpDX.Direct2D1.GradientStop
				{
					Position = 1f,
					Color = color2
				}
			};
		}
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
								if (!(methodInfo == null))
								{
									Type returnType = methodInfo.ReturnType;
									if (!(returnType == null) && !this.listIndicatorExcluded.Contains(returnType.Name))
									{
										Type baseType = returnType.BaseType;
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
														DDQuantZone.IndicatorItem indicatorItem = new DDQuantZone.IndicatorItem(name, this.MainWindowTextColor);
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
			}
			catch (Exception ex)
			{
				this.PrintException(ex);
			}
		}
		private void LoadIndicator()
		{
			try
			{
				if (!string.IsNullOrWhiteSpace(this.IndicatorName) && !string.IsNullOrWhiteSpace(this.IndicatorSetting))
				{
					Type type = null;
					foreach (string text in Directory.GetFiles(this.DocumentsPath, "*.dll", SearchOption.TopDirectoryOnly))
					{
						try
						{
							Assembly assembly = Assembly.LoadFrom(text);
							if (!(assembly == null))
							{
								type = Type.GetType(string.Format("{0}{1}, {2}", this.Namespace, this.IndicatorName, assembly.FullName));
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
						List<DDQuantZone.ParamInfo> list = new JavaScriptSerializer().Deserialize<List<DDQuantZone.ParamInfo>>(this.IndicatorSetting);
						PropertyInfo[] properties = indicatorBase.GetType().GetProperties();
						for (int j = 0; j < list.Count; j++)
						{
							string name = list[j].Name;
							foreach (PropertyInfo propertyInfo in properties)
							{
								if (name == propertyInfo.Name)
								{
									Type propertyType = propertyInfo.PropertyType;
									string value = list[j].Value;
									if (propertyType.IsEnum)
									{
										Type underlyingType = Enum.GetUnderlyingType(propertyType);
										object obj = Convert.ChangeType(value, underlyingType);
										propertyInfo.SetValue(indicatorBase, Enum.ToObject(propertyType, obj));
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
						PriceType priceType = (PriceType)this.InputSeriesIndex;
						ISeries<double> series;
						if (priceType == PriceType.Close)
						{
							series = base.Close;
						}
						else if (priceType == PriceType.High)
						{
							series = base.High;
						}
						else if (priceType == PriceType.Low)
						{
							series = base.Low;
						}
						else if (priceType == PriceType.Median)
						{
							series = base.Median;
						}
						else if (priceType == PriceType.Open)
						{
							series = base.Open;
						}
						else if (priceType == PriceType.Typical)
						{
							series = base.Typical;
						}
						else
						{
							series = base.Weighted;
						}
						this.indicatorBase = base.CacheIndicator<IndicatorBase>(indicatorBase, series, ref this.cacheIndicatorBaseArr);
						this.CollectAllPlotsAndDataSeries();
						this.isNullIndicators = false;
					}
				}
				else
				{
					this.isNullIndicators = true;
				}
			}
			catch
			{
				this.ShowErrorMessageBox();
			}
		}
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
							this.yesNoMessageWindow = new ShowYesNoMessageWindow(NinjaTrader.NinjaScript.Indicators.DimDim.DDQuantZone.windowBackground);
						}
						this.yesNoMessageWindow.ShowMessageBoxOKButtonOnly(this.windowIndicator, "An error occurred while loading the indicator.\nPlease reload the chart (press F5) and try a different indicator.", this.MainWindowTextColor, "QuantZone by DD.com", "OK");
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
		private void CollectAllPlotsAndDataSeries()
		{
			try
			{
				if (this.indicatorBase.Plots.Length != 0)
				{
					NinjaTrader.Gui.Plot[] plots = this.indicatorBase.Plots;
					if (plots != null && plots.Length != 0)
					{
						for (int i = 0; i < plots.Length; i++)
						{
							DDQuantZone.PlotOrDataSeriesInfo plotOrDataSeriesInfo = new DDQuantZone.PlotOrDataSeriesInfo(plots[i].Name, this.indicatorBase.Values[i]);
							this.listPlotOrDataSeries.Add(plotOrDataSeriesInfo);
						}
					}
				}
				PropertyInfo[] properties = this.indicatorBase.GetType().GetProperties();
				if (properties != null && properties.Length != 0)
				{
					PropertyInfo[] array = properties;
					int j = 0;
					while (j < array.Length)
					{
						PropertyInfo propertyInfo = array[j];
						Series<double> series = null;
						try
						{
							series = propertyInfo.GetValue(this.indicatorBase) as Series<double>;
						}
						catch
						{
							goto IL_0247;
						}
						goto IL_00A5;
						IL_0247:
						j++;
						continue;
						IL_00A5:
						bool flag = false;
						object[] customAttributes = propertyInfo.GetCustomAttributes(false);
						string text = propertyInfo.Name.Trim();
						if (text == "Value" || text == "Values")
						{
							break;
						}
						if (series == null || !series.ToString().Contains("NinjaTrader.NinjaScript.Series"))
						{
							goto IL_0247;
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
							goto IL_0247;
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
						if (flag && flag2 && flag3)
						{
							bool flag4 = false;
							Series<double>[] values = this.indicatorBase.Values;
							for (int k = 0; k < values.Length; k++)
							{
								if (values[k] == series)
								{
									flag4 = true;
									break;
								}
							}
							if (!flag4)
							{
								DDQuantZone.PlotOrDataSeriesInfo plotOrDataSeriesInfo2 = new DDQuantZone.PlotOrDataSeriesInfo(text, series);
								this.listPlotOrDataSeries.Add(plotOrDataSeriesInfo2);
							}
						}
						goto IL_0247;
					}
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
					double num = base.High[0];
					double num2 = base.Low[0];
					if (this.isNullIndicators)
					{
						double num3 = this.ComputeMAValue(base.Close, this.DefaultFastMAType, this.DefaultFastPeriod);
						double num4 = this.ComputeMAValue(base.Close, this.DefaultSlowMAType, this.DefaultSlowPeriod);
						if (base.CurrentBar == 0)
						{
							this.isUptrend = num3.ApproxCompare(num4) > 0;
							this.Signal_State[0] = 0.0;
							this.Signal_Zone[0] = 0.0;
							return;
						}
						if (this.isUptrend)
						{
							if (base.High[1].ApproxCompare(this.highest) > 0)
							{
								this.highest = base.High[1];
								this.idxHighest = base.CurrentBar - 1;
							}
							if (num3.ApproxCompare(num4) < 0)
							{
								this.isUptrend = false;
								this.isTrendChange = true;
								this.countSkip++;
							}
						}
						else
						{
							if (base.Low[1].ApproxCompare(this.lowest) < 0)
							{
								this.lowest = base.Low[1];
								this.idxLowest = base.CurrentBar - 1;
							}
							if (num3.ApproxCompare(num4) > 0)
							{
								this.isUptrend = true;
								this.isTrendChange = true;
								this.countSkip++;
							}
						}
					}
					else
					{
						double num5 = this.listPlotOrDataSeries[this.PlotIndex].PlotOrDataSeries[0];
						int num6 = (this.IsMatchOperator(this.OperatorBullish, num5, this.ValueBullish) ? 1 : (this.IsMatchOperator(this.OperatorBearish, num5, this.ValueBearish) ? (-1) : 0));
						if (!this.isInitOK && num6 == 0)
						{
							this.Signal_State[0] = 0.0;
							this.Signal_Zone[0] = 0.0;
							return;
						}
						if (!this.isInitOK)
						{
							this.isUptrend = num6 > 0;
							this.highest = num;
							this.lowest = num2;
							this.idxLowest = (this.idxHighest = base.CurrentBar);
							this.isInitOK = true;
							this.Signal_State[0] = 0.0;
							this.Signal_Zone[0] = 0.0;
							return;
						}
						if (this.isUptrend)
						{
							if (base.High[1].ApproxCompare(this.highest) > 0)
							{
								this.highest = base.High[1];
								this.idxHighest = base.CurrentBar - 1;
							}
							if (num6 < 0)
							{
								this.isTrendChange = true;
								this.isUptrend = false;
								this.countSkip++;
							}
						}
						else
						{
							if (base.Low[1].ApproxCompare(this.lowest) < 0)
							{
								this.lowest = base.Low[1];
								this.idxLowest = base.CurrentBar - 1;
							}
							if (num6 > 0)
							{
								this.isTrendChange = true;
								this.isUptrend = true;
								this.countSkip++;
							}
						}
					}
					if (this.isTrendChange)
					{
						int num7 = (this.isUptrend ? this.idxLowest : this.idxHighest);
						double num8 = (this.isUptrend ? this.lowest : this.highest);
						this.listSwingPoint.Add(new DDQuantZone.SwingPoint(!this.isUptrend, num8, base.CurrentBar, num7));
						this.highest = num;
						this.lowest = num2;
						this.idxHighest = (this.idxLowest = base.CurrentBar);
						this.isTrendChange = false;
						int num9 = this.listSwingPoint.Count - 1;
						int num10 = num9 - 1;
						if (num9 >= this.SampleMax)
						{
							DDQuantZone.SwingPoint swingPoint = this.listSwingPoint[0];
							bool isTop = swingPoint.IsTop;
							int sampleIndex = swingPoint.SampleIndex;
							double sampleDistance = swingPoint.SampleDistance;
							DDQuantZone.SampleInfo sampleInfo = this.listSampleInfo[sampleIndex];
							if (isTop)
							{
								DDQuantZone.MarketMetric marketMetric = this.mmTotalSample;
								double num11 = marketMetric.Bearish;
								marketMetric.Bearish = num11 - 1.0;
								DDQuantZone.SampleInfo sampleInfo2 = sampleInfo;
								int num12 = sampleInfo2.SampleDecreases - 1;
								sampleInfo2.SampleDecreases = num12;
								int num13 = num12;
								sampleInfo.SampleDecreases = Math.Max(num13, 0);
								if (sampleDistance.ApproxCompare(this.mmMin.Bearish) == 0)
								{
									this.mmMin.Bearish = this.nextMinDown;
								}
								if (sampleDistance.ApproxCompare(this.mmMax.Bearish) == 0)
								{
									this.mmMax.Bearish = this.nextMaxDown;
								}
							}
							else
							{
								DDQuantZone.MarketMetric marketMetric2 = this.mmTotalSample;
								double num11 = marketMetric2.Bullish;
								marketMetric2.Bullish = num11 - 1.0;
								DDQuantZone.SampleInfo sampleInfo3 = sampleInfo;
								int num12 = sampleInfo3.SampleIncrease - 1;
								sampleInfo3.SampleIncrease = num12;
								int num14 = num12;
								sampleInfo.SampleIncrease = Math.Max(num14, 0);
								if (sampleDistance.ApproxCompare(this.mmMin.Bullish) == 0)
								{
									this.mmMin.Bullish = this.nextMinUp;
								}
								if (sampleDistance.ApproxCompare(this.mmMax.Bullish) == 0)
								{
									this.mmMax.Bullish = this.nextMaxUp;
								}
							}
							this.listSwingPoint.RemoveAt(0);
							num10 = this.listSwingPoint.Count - 2;
						}
						if (num9 < 1)
						{
							this.Signal_State[0] = 0.0;
							this.Signal_Zone[0] = 0.0;
							return;
						}
						DDQuantZone.SwingPoint swingPoint2 = this.listSwingPoint[num10];
						double num15 = Math.Abs(swingPoint2.Price - num8) / base.TickSize;
						int num16 = Math.Min((int)(num15 / (double)this.SampleRange), this.SampleSteps - 1);
						swingPoint2.SampleIndex = num16;
						swingPoint2.SampleDistance = num15;
						DDQuantZone.SampleInfo sampleInfo4 = this.listSampleInfo[num16];
						if (this.isCharting && this.lineChartTab != null)
						{
							base.ChartControl.Dispatcher.InvokeAsync(delegate
							{
								this.lineChartTab.RefreshData(this.listSampleInfo, this.SampleRange, this.LineBullish.Brush, this.LineBearish.Brush);
							});
						}
						if (this.isUptrend)
						{
							DDQuantZone.MarketMetric marketMetric3 = this.mmTotalSample;
							double num11 = marketMetric3.Bearish;
							marketMetric3.Bearish = num11 + 1.0;
							DDQuantZone.SampleInfo sampleInfo5 = sampleInfo4;
							int num12 = sampleInfo5.SampleDecreases;
							sampleInfo5.SampleDecreases = num12 + 1;
							if (this.isBearishFirst)
							{
								if (num15.ApproxCompare(this.mmMin.Bearish) < 0)
								{
									this.nextMinDown = this.mmMin.Bearish;
									this.mmMin.Bearish = (double)this.RoundByFraction(num15);
								}
								if (num15.ApproxCompare(this.mmMax.Bearish) > 0)
								{
									this.nextMaxDown = this.mmMax.Bearish;
									this.mmMax.Bearish = (double)this.RoundByFraction(num15);
								}
							}
							else
							{
								this.mmMin.Bearish = (this.nextMinDown = (this.mmMax.Bearish = (this.nextMaxDown = (double)this.RoundByFraction(num15))));
								this.isBearishFirst = true;
							}
						}
						else
						{
							DDQuantZone.MarketMetric marketMetric4 = this.mmTotalSample;
							double num11 = marketMetric4.Bullish;
							marketMetric4.Bullish = num11 + 1.0;
							DDQuantZone.SampleInfo sampleInfo6 = sampleInfo4;
							int num12 = sampleInfo6.SampleIncrease;
							sampleInfo6.SampleIncrease = num12 + 1;
							if (this.isBullishFirst)
							{
								if (num15.ApproxCompare(this.mmMin.Bullish) < 0)
								{
									this.nextMinUp = this.mmMin.Bullish;
									this.mmMin.Bullish = (double)this.RoundByFraction(num15);
								}
								if (num15.ApproxCompare(this.mmMax.Bullish) > 0)
								{
									this.nextMaxUp = this.mmMax.Bullish;
									this.mmMax.Bullish = (double)this.RoundByFraction(num15);
								}
							}
							else
							{
								this.mmMin.Bullish = (this.nextMinUp = (this.mmMax.Bullish = (this.nextMaxUp = (double)this.RoundByFraction(num15))));
								this.isBullishFirst = true;
							}
						}
						double num17 = (this.isUptrend ? this.mmTotalSample.Bearish : this.mmTotalSample.Bullish);
						this.UpdateAndCalculateSamples(this.isUptrend, num17);
						if (this.countSkip < this.SampleSkip)
						{
							return;
						}
						if (this.isUptrend)
						{
							this.mmAdverseExcursionPanel.Bullish = (this.isPercentMode ? this.mmLevel1.Bullish : (this.mmAverage.Bullish + (double)this.GAdverseExcursion * this.mmStdDeviation.Bullish));
							this.mmMaxAdverseExcursionPanel.Bullish = (this.isPercentMode ? this.mmLevel2.Bullish : (this.mmAverage.Bullish + (double)this.GMaxAdverseExcursion * this.mmStdDeviation.Bullish));
						}
						else
						{
							this.mmAdverseExcursionPanel.Bullish = (this.isPercentMode ? this.mmLevel1.Bearish : (this.mmAverage.Bearish + (double)this.GAdverseExcursion * this.mmStdDeviation.Bearish));
							this.mmMaxAdverseExcursionPanel.Bullish = (this.isPercentMode ? this.mmLevel2.Bearish : (this.mmAverage.Bearish + (double)this.GMaxAdverseExcursion * this.mmStdDeviation.Bearish));
						}
						this.CreateZone(this.isUptrend, num7);
						if (this.isCharting && base.State == State.Realtime)
						{
							global::System.Windows.Media.Brush zoneBrush = (this.isUptrend ? this.ZoneBearish1End : this.ZoneBullish1End);
							base.ChartControl.Dispatcher.InvokeAsync(delegate
							{
								this.controlPanel.SetBackgroundLabel(zoneBrush);
							});
						}
					}
					else if (!this.isZoneDisable || this.LevelEnabled)
					{
						for (int i = this.sortedListZoneInfoActive.Count - 1; i >= 0; i--)
						{
							KeyValuePair<int, DDQuantZone.ZoneInfo> keyValuePair = this.sortedListZoneInfoActive.ElementAt(i);
							DDQuantZone.ZoneInfo value = keyValuePair.Value;
							int key = keyValuePair.Key;
							double num18 = (value.IsTop ? value.PriceTop : value.PriceBottom);
							value.LineBarEnd = base.CurrentBar;
							bool flag = false;
							if (num.ApproxCompare(num18) >= 0 && num2.ApproxCompare(num18) < 0)
							{
								flag = true;
							}
							else if (base.CurrentBar - value.BarEnd >= this.ProbabilityZoneLine)
							{
								flag = true;
							}
							if (flag)
							{
								if (!this.sortedListZoneInfoInactive.ContainsKey(key))
								{
									this.sortedListZoneInfoInactive.Add(key, value);
								}
								this.sortedListZoneInfoActive.Remove(key);
							}
						}
					}
					if (this.zoneInfo == null)
					{
						this.Signal_State[0] = 0.0;
						this.Signal_Zone[0] = 0.0;
					}
					else
					{
						this.zoneInfo.BarEnd = base.CurrentBar;
						bool isTop2 = this.zoneInfo.IsTop;
						double priceTop = this.zoneInfo.PriceTop;
						double priceBottom = this.zoneInfo.PriceBottom;
						int num19 = (isTop2 ? 1 : (-1));
						double num20 = (isTop2 ? num : num2);
						int num21 = 0;
						int num22 = 0;
						if (this.isDrawBox)
						{
							double num23 = (isTop2 ? priceTop : priceBottom);
							double num24 = (isTop2 ? priceBottom : priceTop);
							if (num20.ApproxCompare(num23) * num19 > 0)
							{
								num21 = (isTop2 ? 4 : (-2));
							}
							else if (num20.ApproxCompare(num24) * num19 < 0)
							{
								num21 = (isTop2 ? (-4) : 2);
							}
							else
							{
								num21 = (isTop2 ? 3 : 1);
							}
							num22 = (isTop2 ? (-1) : 1);
							double num25 = this.MaxFavorableExcursion[1];
							double num26 = this.FavorableExcursion[1];
							if (!this.isMFEBroken)
							{
								double num27 = this.mmMaxFavorableExcursionPanel.Bullish * base.TickSize;
								double num28;
								if ((isTop2 ? (num - num25) : (num25 - num2)).ApproxCompare(num27) <= 0)
								{
									num28 = num25;
								}
								else
								{
									num28 = (isTop2 ? (num - num27) : (num2 + num27));
								}
								this.MaxFavorableExcursion[0] = num28;
								if (this.PlotEnabled)
								{
									base.PlotBrushes[0][0] = (isTop2 ? this.PlotBearish : this.PlotBullish);
								}
								if (isTop2 && (num28.ApproxCompare(priceTop) >= 0 || num2.ApproxCompare(num28) <= 0))
								{
									this.isMFEBroken = true;
								}
								else if (!isTop2 && (num28.ApproxCompare(priceBottom) <= 0 || num.ApproxCompare(num28) >= 0))
								{
									this.isMFEBroken = true;
								}
							}
							if (!this.isFEBroken)
							{
								double num29 = this.mmFavorableExcursionPanel.Bullish * base.TickSize;
								double num30;
								if ((isTop2 ? (num - num26) : (num26 - num2)).ApproxCompare(num29) <= 0)
								{
									num30 = num26;
								}
								else
								{
									num30 = (isTop2 ? (num - num29) : (num2 + num29));
								}
								this.FavorableExcursion[0] = num30;
								if (this.PlotEnabled)
								{
									base.PlotBrushes[1][0] = (isTop2 ? this.PlotBearish : this.PlotBullish);
								}
								if (isTop2 && (num30.ApproxCompare(priceTop) >= 0 || num2.ApproxCompare(num30) <= 0))
								{
									this.isFEBroken = true;
								}
								else if (!isTop2 && (num30.ApproxCompare(priceBottom) <= 0 || num.ApproxCompare(num30) >= 0))
								{
									this.isFEBroken = true;
								}
							}
						}
						else if (num20.ApproxCompare(isTop2 ? priceBottom : priceTop) * num19 >= 0)
						{
							this.isDrawBox = true;
							this.PrintMarker(!isTop2);
							num21 = (isTop2 ? 3 : 1);
							num22 = (isTop2 ? (-1) : 1);
							double bullish = this.mmMaxFavorableExcursionPanel.Bullish;
							double bullish2 = this.mmFavorableExcursionPanel.Bullish;
							this.MaxFavorableExcursion[0] = num20 - (double)num19 * bullish * base.TickSize;
							this.FavorableExcursion[0] = num20 - (double)num19 * bullish2 * base.TickSize;
							global::System.Windows.Media.Brush brush = (isTop2 ? this.PlotBearish : this.PlotBullish);
							if (this.PlotEnabled)
							{
								base.PlotBrushes[0][0] = (base.PlotBrushes[1][0] = brush);
							}
							DDQuantZone.PlotInfo plotInfo = new DDQuantZone.PlotInfo(isTop2, string.Format(" MFE ({0} ticks)", bullish), string.Format(" FE ({0} ticks)", bullish2));
							if (!this.dictPlotInfo.ContainsKey(base.CurrentBar))
							{
								this.dictPlotInfo.Add(base.CurrentBar, plotInfo);
							}
							else
							{
								this.dictPlotInfo[base.CurrentBar] = plotInfo;
							}
						}
						this.Signal_State[0] = (double)num21;
						this.Signal_Zone[0] = (double)num22;
						if (base.CurrentBar == base.BarsArray[0].Count - 2 && this.isCharting)
						{
							base.ChartControl.Dispatcher.InvokeAsync(delegate
							{
								this.controlPanel.SetBackgroundLabel(this.isUptrend ? this.ZoneBearish4End : this.ZoneBullish4End);
								base.ChartControl.InvalidateVisual();
							});
						}
					}
			}
			catch (Exception ex)
			{
				this.PrintException(ex);
			}
		}
		private void UpdateAndCalculateSamples(bool isUptrend, double totalSample)
		{
			if (this.countSkip < this.SampleSkip)
			{
				return;
			}
			if (this.isPercentMode)
			{
				bool flag = false;
				bool flag2 = false;
				bool flag3 = false;
				bool flag4 = false;
				bool flag5 = false;
				double num = 0.0;
				using (List<DDQuantZone.SampleInfo>.Enumerator enumerator = this.listSampleInfo.GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						DDQuantZone.SampleInfo sampleInfo = enumerator.Current;
						int num2 = (isUptrend ? sampleInfo.SampleDecreases : sampleInfo.SampleIncrease);
						if (num2 >= 1)
						{
							double num3 = (double)num2 / totalSample * 100.0;
							if (isUptrend)
							{
								sampleInfo.ProbabilityDecreases = num3;
							}
							else
							{
								sampleInfo.ProbabilityIncreases = num3;
							}
							num += num3;
							if (!flag && num.ApproxCompare((double)this.PAdverseExcursion) >= 0)
							{
								int to = sampleInfo.To;
								if (isUptrend)
								{
									this.mmLevel1.Bearish = (double)to;
								}
								else
								{
									this.mmLevel1.Bullish = (double)to;
								}
								flag = true;
							}
							if (!flag2 && num.ApproxCompare((double)this.PMaxAdverseExcursion) >= 0)
							{
								int to2 = sampleInfo.To;
								if (isUptrend)
								{
									this.mmLevel2.Bearish = (double)to2;
								}
								else
								{
									this.mmLevel2.Bullish = (double)to2;
								}
								flag2 = true;
							}
							if (!flag3 && num.ApproxCompare(50.0) >= 0)
							{
								int to3 = sampleInfo.To;
								if (isUptrend)
								{
									this.mmAverage.Bearish = (double)to3;
								}
								else
								{
									this.mmAverage.Bullish = (double)to3;
								}
								flag3 = true;
							}
							if (!flag4 && (100.0 - num).ApproxCompare((double)this.PMaxFavorableExcursion) <= 0)
							{
								int to4 = sampleInfo.To;
								if (isUptrend)
								{
									this.mmMaxFavorableExcursion.Bearish = (double)to4;
								}
								else
								{
									this.mmMaxFavorableExcursion.Bullish = (double)to4;
								}
								this.mmMaxFavorableExcursionPanel.Bullish = (double)to4;
								flag4 = true;
							}
							if (!flag5 && (100.0 - num).ApproxCompare((double)this.PFavorableExcursion) <= 0)
							{
								int to5 = sampleInfo.To;
								if (isUptrend)
								{
									this.mmFavorableExcursion.Bearish = (double)to5;
								}
								else
								{
									this.mmFavorableExcursion.Bullish = (double)to5;
								}
								this.mmFavorableExcursionPanel.Bullish = (double)to5;
								flag5 = true;
							}
						}
					}
					return;
				}
			}
			double num4 = 0.0;
			double num5 = 0.0;
			foreach (DDQuantZone.SampleInfo sampleInfo2 in this.listSampleInfo)
			{
				int num6 = (isUptrend ? sampleInfo2.SampleDecreases : sampleInfo2.SampleIncrease);
				if (num6 >= 1)
				{
					double num7 = (double)num6 / totalSample;
					num4 += (double)sampleInfo2.To * num7;
					num7 *= 100.0;
					if (isUptrend)
					{
						sampleInfo2.ProbabilityDecreases = num7;
					}
					else
					{
						sampleInfo2.ProbabilityIncreases = num7;
					}
				}
			}
			foreach (DDQuantZone.SampleInfo sampleInfo3 in this.listSampleInfo)
			{
				int num8 = (isUptrend ? sampleInfo3.SampleDecreases : sampleInfo3.SampleIncrease);
				if (num8 >= 1)
				{
					double num9 = (double)num8 / totalSample;
					double num10 = (double)sampleInfo3.To - num4;
					num5 += Math.Pow(num10, 2.0) * num9;
				}
			}
			num5 = (double)this.RoundByFraction(Math.Sqrt(num5));
			num4 = (double)this.RoundByFraction(num4);
			double num11;
			if (this.GMaxFavorableExcursion == DDQuantZone_FavorableExcursion.T84)
			{
				num11 = num4 - num5;
			}
			else if (this.GMaxFavorableExcursion == DDQuantZone_FavorableExcursion.T15)
			{
				num11 = num4 + num5;
			}
			else
			{
				num11 = num4;
			}
			double num12;
			if (this.GFavorableExcursion == DDQuantZone_FavorableExcursion.T84)
			{
				num12 = num4 - num5;
			}
			else if (this.GFavorableExcursion == DDQuantZone_FavorableExcursion.T15)
			{
				num12 = num4 + num5;
			}
			else
			{
				num12 = num4;
			}
			this.mmMaxFavorableExcursionPanel.Bullish = num11;
			this.mmFavorableExcursionPanel.Bullish = num12;
			if (isUptrend)
			{
				this.mmStdDeviation.Bearish = num5;
				this.mmAverage.Bearish = num4;
				this.mmLevel1.Bearish = num4 + num5;
				this.mmLevel2.Bearish = num4 + 2.0 * num5;
				this.mmLevel3.Bearish = num4 + 3.0 * num5;
				this.mmMaxFavorableExcursion.Bearish = num11;
				this.mmFavorableExcursion.Bearish = num12;
				return;
			}
			this.mmStdDeviation.Bullish = num5;
			this.mmAverage.Bullish = num4;
			this.mmLevel1.Bullish = num4 + num5;
			this.mmLevel2.Bullish = num4 + 2.0 * num5;
			this.mmLevel3.Bullish = num4 + 3.0 * num5;
			this.mmMaxFavorableExcursion.Bullish = num11;
			this.mmFavorableExcursion.Bullish = num12;
		}
		private void CreateZone(bool isUptrend, int barEnd)
		{
			if (this.zoneInfo != null)
			{
				if (this.isDrawBox)
				{
					this.zoneInfo.LineBarEnd = base.CurrentBar;
					if ((!this.isZoneDisable || this.LevelEnabled) && !this.sortedListZoneInfoActive.ContainsKey(base.CurrentBar))
					{
						this.sortedListZoneInfoActive.Add(base.CurrentBar, this.zoneInfo);
					}
				}
				else
				{
					bool isTop = this.zoneInfo.IsTop;
					this.listLineInfo.Add(new DDQuantZone.LineInfo(isTop, isTop ? this.zoneInfo.PriceTop : this.zoneInfo.PriceBottom, this.zoneInfo.BarStart, this.zoneInfo.BarEnd));
				}
			}
			this.isDrawBox = false;
			this.isMFEBroken = false;
			this.isFEBroken = false;
			this.isStartPlotOK = false;
			int num = base.CurrentBar - barEnd;
			double obj = (isUptrend ? base.Low[num] : base.High[num]);
			int num2 = (isUptrend ? 1 : (-1));
			double num3;
			double num4;
			if (this.isPercentMode)
			{
				num3 = (isUptrend ? this.mmLevel1.Bullish : this.mmLevel1.Bearish);
				num4 = (isUptrend ? this.mmLevel2.Bullish : this.mmLevel2.Bearish);
			}
			else
			{
				double obj2 = (isUptrend ? this.mmAverage.Bullish : this.mmAverage.Bearish);
				double num5 = (isUptrend ? this.mmStdDeviation.Bullish : this.mmStdDeviation.Bearish);
				double obj3 = obj2;
				num3 = obj3 + (double)this.GAdverseExcursion * num5;
				num4 = obj3 + (double)this.GMaxAdverseExcursion * num5;
			}
			double obj4 = obj;
			double num6 = obj4 + (double)num2 * num3 * base.TickSize;
			double num7 = obj4 + (double)num2 * num4 * base.TickSize;
			double num8 = (isUptrend ? num7 : num6);
			double num9 = (isUptrend ? num6 : num7);
			string text = string.Format(" AE ({0} ticks)", num3);
			string text2 = string.Format(" MAE ({0} ticks)", num4);
			this.zoneInfo = new DDQuantZone.ZoneInfo(isUptrend, num8, num9, base.CurrentBar, base.CurrentBar, text, text2);
		}
		private bool IsMatchOperator(DDQuantZone_Operators opInput, double signal, double valueCompare)
		{
			int num = signal.ApproxCompare(valueCompare);
			return (opInput == DDQuantZone_Operators.Greater && num > 0) || (opInput == DDQuantZone_Operators.Smaller && num < 0) || (opInput == DDQuantZone_Operators.GreaterOrEqual && num >= 0) || (opInput == DDQuantZone_Operators.SmallerOrEqual && num <= 0) || (opInput == DDQuantZone_Operators.Equal && num == 0) || (opInput == DDQuantZone_Operators.Unequal && num != 0);
		}
		private int RoundByFraction(double value)
		{
			return (int)(value + (value % 1.0 > 0.5 ? 1 : 0));
		}
		public override void OnCalculateMinMax()
		{
			base.TriggerCustomEvent(delegate(object state)
			{
				try
				{
					if (!this.SwitchedOnAutofit)
					{
						for (int i = base.ChartBars.FromIndex; i <= base.ChartBars.ToIndex; i++)
						{
							this.maxValue = Math.Min(this.maxValue, base.Low.GetValueAt(i));
							this.minValue = Math.Max(this.minValue, base.High.GetValueAt(i));
						}
					}
					base.MaxValue = this.maxValue;
					base.MinValue = this.minValue;
					this.maxValue = double.MinValue;
					this.minValue = double.MaxValue;
				}
				catch
				{
				}
			}, null);
		}
		protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
		{
			try
			{
				if (this.isCharting)
				{
					if (this.needReloadChart)
					{
						int num = base.ChartPanel.X + base.ChartPanel.W - 5;
						int num2 = base.ChartPanel.Y + base.ChartPanel.H - 5;
						this.DrawText("You have to reload the chart (press F5) for the new setting to take effect.", new SimpleFont("Arial", 18), (float)num, (float)num2, -1, -1, global::System.Windows.Media.Brushes.Goldenrod, this.ScreenDPI, base.RenderTarget);
					}
					if (this.SwitchedOnAll)
					{
						base.OnRender(chartControl, chartScale);
						if (!base.IsInHitTest)
						{
							this.isScaleMinOK = false;
							this.isScaleMaxOK = false;
							this.DrawSwingPoints(chartScale);
							this.DrawMarkers(chartScale);
							if (!this.isZoneDisable || this.LevelEnabled)
							{
								this.DrawOneBox(chartScale, this.zoneInfo, true);
							}
							this.DrawBoxes(chartScale, this.sortedListZoneInfoActive);
							this.DrawBoxes(chartScale, this.sortedListZoneInfoInactive);
							this.DrawLines(chartScale);
							if (this.dictPlotInfo.Count > 0)
							{
								for (int i = base.ChartBars.FromIndex; i <= base.ChartBars.ToIndex; i++)
								{
									if (!this.isScaleMinOK)
									{
										this.minValue = base.Low.GetValueAt(i);
									}
									if (!this.isScaleMaxOK)
									{
										this.maxValue = base.High.GetValueAt(i);
									}
									if (this.dictPlotInfo.ContainsKey(i))
									{
										DDQuantZone.PlotInfo plotInfo = this.dictPlotInfo[i];
										double valueAt = this.MaxFavorableExcursion.GetValueAt(i);
										double valueAt2 = this.FavorableExcursion.GetValueAt(i);
										float num3 = (float)base.ChartControl.GetXByBarIndex(base.ChartBars, i - 1);
										float num4 = (float)chartScale.GetYByValue(valueAt);
										float num5 = (float)chartScale.GetYByValue(valueAt2);
										SimpleFont simpleFont = new SimpleFont("Arial", 12);
										global::System.Windows.Media.Brush brush = (plotInfo.IsTop ? this.LineBearish.Brush : this.LineBullish.Brush);
										this.DrawText(plotInfo.MaxFavorableExcursion, simpleFont, num3, num4, -1, 0, brush, this.ScreenDPI, base.RenderTarget);
										this.DrawText(plotInfo.FavorableExcursion, simpleFont, num3, num5, -1, 0, brush, this.ScreenDPI, base.RenderTarget);
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
		private void DrawMarkers(ChartScale chartScale)
		{
			try
			{
				if (this.MarkerEnabled)
				{
					if (this.isCustomMarkerRenderingMethod)
					{
						if (this.dictMarkerInfo != null && this.dictMarkerInfo.Count > 0)
						{
							double num = chartScale.MaxValue;
							double num2 = chartScale.MinValue;
							for (int i = base.ChartBars.FromIndex; i <= Math.Min(base.CurrentBar, base.ChartBars.ToIndex); i++)
							{
								if (this.dictMarkerInfo.ContainsKey(i))
								{
									bool flag = this.dictMarkerInfo[i];
									int num3 = i;
									if (!flag && base.High.GetValueAt(num3).ApproxCompare(num) >= 0)
									{
										break;
									}
									if (flag && base.Low.GetValueAt(num3).ApproxCompare(num2) <= 0)
									{
										break;
									}
									global::System.Windows.Media.Brush brush = (flag ? this.MarkerBrushBullish : this.MarkerBrushBearish);
									if (brush.IsTransparent())
									{
										break;
									}
									string text = (flag ? this.MarkerStringBullish : this.MarkerStringBearish);
									if (string.IsNullOrWhiteSpace(text))
									{
										break;
									}
									text = this.FormatMarkerString(text);
									int num4 = (flag ? (-1) : 1);
									float num5 = (float)base.ChartControl.GetXByBarIndex(base.ChartBars, num3);
									double num6 = (double)(chartScale.GetYByValue((flag ? base.High : base.Low).GetValueAt(num3)) + num4 * this.MarkerOffset);
									this.DrawText(text, this.MarkerFont, num5, (float)num6, 0, num4, brush, this.ScreenDPI, base.RenderTarget);
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
		private void DrawSwingPoints(ChartScale chartScale)
		{
			if (!this.SwingPointEnabled)
			{
				return;
			}
			if (this.listSwingPoint != null && this.listSwingPoint.Count >= 1)
			{
				for (int i = 0; i < this.listSwingPoint.Count; i++)
				{
					DDQuantZone.SwingPoint swingPoint = this.listSwingPoint[i];
					if (swingPoint.BarStart <= base.ChartBars.ToIndex && swingPoint.BarEnd >= base.ChartBars.FromIndex)
					{
						this.DrawOneSwingPoint(chartScale, swingPoint);
					}
				}
				return;
			}
		}
		private void DrawOneSwingPoint(ChartScale chartScale, DDQuantZone.SwingPoint swingPoint)
		{
			int barEnd = swingPoint.BarEnd;
			bool isTop;
			global::System.Windows.Media.Brush brush = ((isTop = swingPoint.IsTop) ? this.SwingPointTop : this.SwingPointBottom);
			if (brush.IsTransparent())
			{
				return;
			}
			float num = Math.Max(3f, (float)base.ChartControl.BarWidth - this.barOutlineWidth);
			double price = swingPoint.Price;
			if (price.ApproxCompare(base.ChartPanel.MaxValue) <= 0 && price.ApproxCompare(base.ChartPanel.MinValue) >= 0)
			{
				float num2 = (float)base.ChartControl.GetXByBarIndex(base.ChartBars, barEnd);
				float num3 = (float)chartScale.GetYByValue(isTop ? base.High.GetValueAt(barEnd) : base.Low.GetValueAt(barEnd));
				Vector2 vector = new Vector2(num2, num3);
				float num4 = Math.Max(4.7f, num);
				global::SharpDX.Direct2D1.Ellipse ellipse = new global::SharpDX.Direct2D1.Ellipse(vector, num4 + 0.2f, num4 + 0.2f);
				global::SharpDX.Direct2D1.Ellipse ellipse2 = new global::SharpDX.Direct2D1.Ellipse(vector, num4, num4);
				AntialiasMode antialiasMode = base.RenderTarget.AntialiasMode;
				base.RenderTarget.AntialiasMode = AntialiasMode.PerPrimitive;
				global::SharpDX.Direct2D1.Brush brush2 = global::System.Windows.Media.Brushes.Silver.ToDxBrush(base.RenderTarget);
				global::SharpDX.Direct2D1.Brush brush3 = brush.ToDxBrush(base.RenderTarget);
				base.RenderTarget.DrawEllipse(ellipse, brush2);
				base.RenderTarget.FillEllipse(ellipse2, brush3);
				brush2.Dispose();
				brush3.Dispose();
				base.RenderTarget.AntialiasMode = antialiasMode;
				return;
			}
		}
		private void DrawBoxes(ChartScale chartScale, SortedList<int, DDQuantZone.ZoneInfo> sortedListZoneInfo)
		{
			if (this.isZoneDisable && !this.LevelEnabled)
			{
				return;
			}
			if (sortedListZoneInfo == null)
			{
				return;
			}
			int count = sortedListZoneInfo.Count;
			if (count <= 0)
			{
				return;
			}
			for (int i = count - 1; i >= 0; i--)
			{
				this.DrawOneBox(chartScale, sortedListZoneInfo.Values[i], false);
			}
		}
		private void DrawOneBox(ChartScale chartScale, DDQuantZone.ZoneInfo zoneInfo, bool isCurrentBox = false)
		{
			if (zoneInfo == null)
			{
				return;
			}
			int barStart = zoneInfo.BarStart;
			int barEnd = zoneInfo.BarEnd;
			int num = zoneInfo.LineBarEnd;
			num = ((num < 0) ? barEnd : num);
			if (barStart >= barEnd)
			{
				return;
			}
			if (barStart > base.ChartBars.ToIndex || num < base.ChartBars.FromIndex)
			{
				return;
			}
			bool isTop = zoneInfo.IsTop;
			double priceTop = zoneInfo.PriceTop;
			double priceBottom = zoneInfo.PriceBottom;
			if (this.SwitchedOnAutofit)
			{
				if (isTop && priceTop.ApproxCompare(this.maxValue) > 0)
				{
					this.maxValue = priceTop;
					if (!this.isScaleMaxOK)
					{
						this.isScaleMaxOK = true;
					}
				}
				else if (!isTop && priceBottom.ApproxCompare(this.minValue) < 0)
				{
					this.minValue = priceBottom;
					if (!this.isScaleMinOK)
					{
						this.isScaleMinOK = true;
					}
				}
			}
			else if (priceBottom.ApproxCompare(base.ChartPanel.MaxValue) > 0 || priceTop.ApproxCompare(base.ChartPanel.MinValue) < 0)
			{
				return;
			}
			float num2 = (float)base.ChartControl.GetXByBarIndex(base.ChartBars, barStart);
			float num3 = (float)base.ChartControl.GetXByBarIndex(base.ChartBars, barEnd);
			float num4 = (float)base.ChartControl.GetXByBarIndex(base.ChartBars, num);
			float num5 = (float)chartScale.GetYByValue(priceTop);
			float num6 = (float)chartScale.GetYByValue(priceBottom);
			float num7 = (num5 - num6) / 4f;
			float num8 = num5 - num7;
			float num9 = num5 - 2f * num7;
			float num10 = num5 - 3f * num7;
			if (this.LevelEnabled)
			{
				Stroke stroke = (isTop ? this.LineBearish : this.LineBullish);
				global::System.Windows.Media.Brush brush = stroke.Brush;
				if (!brush.IsTransparent())
				{
					float width = stroke.Width;
					StrokeStyle strokeStyle = stroke.StrokeStyle;
					global::SharpDX.Direct2D1.Brush brush2 = brush.ToDxBrush(base.RenderTarget);
					Vector2 vector = new Vector2(num2, num5);
					Vector2 vector2 = new Vector2(num3, num5);
					base.RenderTarget.DrawLine(vector, vector2, brush2, isTop ? 4f : width, strokeStyle);
					Vector2 vector3 = new Vector2(num2, num8);
					Vector2 vector4 = new Vector2(num3, num8);
					base.RenderTarget.DrawLine(vector3, vector4, brush2, width, strokeStyle);
					Vector2 vector5 = new Vector2(num2, num9);
					Vector2 vector6 = new Vector2(num3, num9);
					base.RenderTarget.DrawLine(vector5, vector6, brush2, width, strokeStyle);
					Vector2 vector7 = new Vector2(num2, num10);
					Vector2 vector8 = new Vector2(num3, num10);
					base.RenderTarget.DrawLine(vector7, vector8, brush2, width, strokeStyle);
					Vector2 vector9 = new Vector2(num2, num6);
					Vector2 vector10 = new Vector2(num3, num6);
					base.RenderTarget.DrawLine(vector9, vector10, brush2, isTop ? width : 4f, strokeStyle);
					Vector2 vector11 = new Vector2(num3, isTop ? num5 : num6);
					Vector2 vector12 = new Vector2(num4, isTop ? num5 : num6);
					Stroke stroke2 = new Stroke(brush, DashStyleHelper.DashDot, 1f);
					base.RenderTarget.DrawLine(vector11, vector12, brush2, width, stroke2.StrokeStyle);
					brush2.Dispose();
				}
			}
			if (!this.isZoneDisable)
			{
				global::SharpDX.Direct2D1.GradientStop[] array = null;
				global::SharpDX.Direct2D1.GradientStop[] array2 = null;
				global::SharpDX.Direct2D1.GradientStop[] array3 = null;
				global::SharpDX.Direct2D1.GradientStop[] array4 = null;
				global::System.Windows.Media.Brush brush3 = null;
				global::System.Windows.Media.Brush brush4 = null;
				global::System.Windows.Media.Brush brush5 = null;
				global::System.Windows.Media.Brush brush6 = null;
				if (this.ZoneDisplayMode == DDQuantZone_DisplayMode.Gradient)
				{
					array = (isTop ? this.zoneBearishGradientStop1 : this.zoneBullishGradientStop4);
					array2 = (isTop ? this.zoneBearishGradientStop2 : this.zoneBullishGradientStop3);
					array3 = (isTop ? this.zoneBearishGradientStop3 : this.zoneBullishGradientStop2);
					array4 = (isTop ? this.zoneBearishGradientStop4 : this.zoneBullishGradientStop1);
				}
				else
				{
					brush3 = (isTop ? this.zoneBearish1 : this.zoneBullish1);
					brush4 = (isTop ? this.zoneBearish2 : this.zoneBullish2);
					brush5 = (isTop ? this.zoneBearish3 : this.zoneBullish3);
					brush6 = (isTop ? this.zoneBearish4 : this.zoneBullish4);
				}
				float num11 = num3 - num2;
				float num12 = Math.Abs(num6 - num5) / 4f;
				global::SharpDX.RectangleF rectangleF = new global::SharpDX.RectangleF(num2, num5, num11, num12);
				global::SharpDX.RectangleF rectangleF2 = new global::SharpDX.RectangleF(num2, num8, num11, num12);
				global::SharpDX.RectangleF rectangleF3 = new global::SharpDX.RectangleF(num2, num9, num11, num12);
				global::SharpDX.RectangleF rectangleF4 = new global::SharpDX.RectangleF(num2, num10, num11, num12);
				if (this.ZoneDisplayMode == DDQuantZone_DisplayMode.Gradient)
				{
					global::SharpDX.Direct2D1.GradientStopCollection gradientStopCollection = new global::SharpDX.Direct2D1.GradientStopCollection(base.RenderTarget, array);
					Vector2 vector13 = (isTop ? rectangleF.BottomLeft : rectangleF.TopLeft);
					Vector2 vector14 = (isTop ? rectangleF.TopLeft : rectangleF.BottomLeft);
					LinearGradientBrushProperties linearGradientBrushProperties = new LinearGradientBrushProperties
					{
						StartPoint = vector13,
						EndPoint = vector14
					};
					global::SharpDX.Direct2D1.LinearGradientBrush linearGradientBrush = new global::SharpDX.Direct2D1.LinearGradientBrush(base.RenderTarget, linearGradientBrushProperties, gradientStopCollection);
					base.RenderTarget.FillRectangle(rectangleF, linearGradientBrush);
					linearGradientBrush.Dispose();
					gradientStopCollection.Dispose();
					global::SharpDX.Direct2D1.GradientStopCollection gradientStopCollection2 = new global::SharpDX.Direct2D1.GradientStopCollection(base.RenderTarget, array2);
					Vector2 vector15 = (isTop ? rectangleF2.BottomLeft : rectangleF2.TopLeft);
					Vector2 vector16 = (isTop ? rectangleF2.TopLeft : rectangleF2.BottomLeft);
					LinearGradientBrushProperties linearGradientBrushProperties2 = new LinearGradientBrushProperties
					{
						StartPoint = vector15,
						EndPoint = vector16
					};
					global::SharpDX.Direct2D1.LinearGradientBrush linearGradientBrush2 = new global::SharpDX.Direct2D1.LinearGradientBrush(base.RenderTarget, linearGradientBrushProperties2, gradientStopCollection2);
					base.RenderTarget.FillRectangle(rectangleF2, linearGradientBrush2);
					linearGradientBrush2.Dispose();
					gradientStopCollection2.Dispose();
					global::SharpDX.Direct2D1.GradientStopCollection gradientStopCollection3 = new global::SharpDX.Direct2D1.GradientStopCollection(base.RenderTarget, array3);
					Vector2 vector17 = (isTop ? rectangleF3.BottomLeft : rectangleF3.TopLeft);
					Vector2 vector18 = (isTop ? rectangleF3.TopLeft : rectangleF3.BottomLeft);
					LinearGradientBrushProperties linearGradientBrushProperties3 = new LinearGradientBrushProperties
					{
						StartPoint = vector17,
						EndPoint = vector18
					};
					global::SharpDX.Direct2D1.LinearGradientBrush linearGradientBrush3 = new global::SharpDX.Direct2D1.LinearGradientBrush(base.RenderTarget, linearGradientBrushProperties3, gradientStopCollection3);
					base.RenderTarget.FillRectangle(rectangleF3, linearGradientBrush3);
					linearGradientBrush3.Dispose();
					gradientStopCollection3.Dispose();
					global::SharpDX.Direct2D1.GradientStopCollection gradientStopCollection4 = new global::SharpDX.Direct2D1.GradientStopCollection(base.RenderTarget, array4);
					Vector2 vector19 = (isTop ? rectangleF4.BottomLeft : rectangleF4.TopLeft);
					Vector2 vector20 = (isTop ? rectangleF4.TopLeft : rectangleF4.BottomLeft);
					LinearGradientBrushProperties linearGradientBrushProperties4 = new LinearGradientBrushProperties
					{
						StartPoint = vector19,
						EndPoint = vector20
					};
					global::SharpDX.Direct2D1.LinearGradientBrush linearGradientBrush4 = new global::SharpDX.Direct2D1.LinearGradientBrush(base.RenderTarget, linearGradientBrushProperties4, gradientStopCollection4);
					base.RenderTarget.FillRectangle(rectangleF4, linearGradientBrush4);
					linearGradientBrush4.Dispose();
					gradientStopCollection4.Dispose();
				}
				else
				{
					global::SharpDX.Direct2D1.Brush brush7 = brush3.ToDxBrush(base.RenderTarget);
					base.RenderTarget.FillRectangle(rectangleF, brush7);
					brush7.Dispose();
					global::SharpDX.Direct2D1.Brush brush8 = brush4.ToDxBrush(base.RenderTarget);
					base.RenderTarget.FillRectangle(rectangleF2, brush8);
					brush8.Dispose();
					global::SharpDX.Direct2D1.Brush brush9 = brush5.ToDxBrush(base.RenderTarget);
					base.RenderTarget.FillRectangle(rectangleF3, brush9);
					brush9.Dispose();
					global::SharpDX.Direct2D1.Brush brush10 = brush6.ToDxBrush(base.RenderTarget);
					base.RenderTarget.FillRectangle(rectangleF4, brush10);
					brush10.Dispose();
				}
			}
			SimpleFont simpleFont = new SimpleFont("Arial", 12);
			string text = (isTop ? zoneInfo.MAEStr : zoneInfo.AEStr);
			string text2 = (isTop ? zoneInfo.AEStr : zoneInfo.MAEStr);
			global::System.Windows.Media.Brush brush11 = (isTop ? global::System.Windows.Media.Brushes.HotPink : global::System.Windows.Media.Brushes.DodgerBlue);
			this.DrawText(text, simpleFont, num3, num5, 1, isTop ? (-1) : 0, brush11, this.ScreenDPI, base.RenderTarget);
			this.DrawText(text2, simpleFont, num3, num6, 1, (!isTop) ? 1 : 0, brush11, this.ScreenDPI, base.RenderTarget);
			if (!isCurrentBox)
			{
				return;
			}
			string text3;
			if (!this.isPercentMode)
			{
				if ((text3 = NinjaTrader.NinjaScript.Indicators.DimDim.DDQuantZone.strAdverseExcursionArr[(int)this.GAdverseExcursion]) != null)
				{
					goto IL_079B;
				}
			}
			else if ((text3 = this.PAdverseExcursion.ToString()) != null)
			{
				goto IL_079B;
			}
			string text4 = null;
			goto IL_07A0;
			IL_079B:
			text4 = text3.ToString();
			IL_07A0:
			string text5 = text4 + "%";
			string text6;
			if (!this.isPercentMode)
			{
				if ((text6 = NinjaTrader.NinjaScript.Indicators.DimDim.DDQuantZone.strAdverseExcursionArr[(int)this.GMaxAdverseExcursion]) != null)
				{
					goto IL_07D7;
				}
			}
			else if ((text6 = this.PMaxAdverseExcursion.ToString()) != null)
			{
				goto IL_07D7;
			}
			string text7 = null;
			goto IL_07DC;
			IL_07D7:
			text7 = text6.ToString();
			IL_07DC:
			string text8 = text7 + "%";
			this.DrawText(isTop ? text8 : text5, simpleFont, num2, num5, -1, 0, brush11, this.ScreenDPI, base.RenderTarget);
			this.DrawText(isTop ? text5 : text8, simpleFont, num2, num6, -1, 0, brush11, this.ScreenDPI, base.RenderTarget);
		}
		private void DrawLines(ChartScale chartScale)
		{
			if (!this.LineEnabled)
			{
				return;
			}
			if (this.listLineInfo == null)
			{
				return;
			}
			int count = this.listLineInfo.Count;
			if (count <= 0)
			{
				return;
			}
			for (int i = 0; i < count; i++)
			{
				DDQuantZone.LineInfo lineInfo = this.listLineInfo[i];
				int barStart = lineInfo.BarStart;
				int barEnd = lineInfo.BarEnd;
				if (barStart != barEnd && barStart <= base.ChartBars.ToIndex && barEnd >= base.ChartBars.FromIndex)
				{
					double price = lineInfo.Price;
					bool isTop = lineInfo.IsTop;
					if (this.SwitchedOnAutofit)
					{
						if (isTop && price.ApproxCompare(this.maxValue) > 0)
						{
							this.maxValue = price;
							if (!this.isScaleMaxOK)
							{
								this.isScaleMaxOK = true;
							}
						}
						else if (!isTop && price.ApproxCompare(this.minValue) < 0)
						{
							this.minValue = price;
							if (!this.isScaleMinOK)
							{
								this.isScaleMinOK = true;
							}
						}
					}
					else if (price.ApproxCompare(chartScale.MaxValue) > 0 || price.ApproxCompare(chartScale.MinValue) < 0)
					{
						goto IL_01BB;
					}
					Stroke stroke = (isTop ? this.LineBearish : this.LineBullish);
					global::System.Windows.Media.Brush brush = stroke.Brush;
					if (!brush.IsTransparent())
					{
						float num = (float)base.ChartControl.GetXByBarIndex(base.ChartBars, barStart);
						float num2 = (float)base.ChartControl.GetXByBarIndex(base.ChartBars, barEnd);
						float num3 = (float)chartScale.GetYByValue(price);
						Vector2 vector = new Vector2(num, num3);
						Vector2 vector2 = new Vector2(num2, num3);
						float width = stroke.Width;
						StrokeStyle strokeStyle = stroke.StrokeStyle;
						global::SharpDX.Direct2D1.Brush brush2 = brush.ToDxBrush(base.RenderTarget);
						base.RenderTarget.DrawLine(vector, vector2, brush2, width, strokeStyle);
						brush2.Dispose();
					}
				}
				IL_01BB:;
			}
		}

		private void OnBtnTitleClick(object sender, RoutedEventArgs e)
		{
			base.TriggerCustomEvent(delegate(object state)
			{
				this.BuildWindowIndicator();
			}, e);
		}
		private void OnBtnInfoClick(object sender, RoutedEventArgs e)
		{
			base.TriggerCustomEvent(delegate(object state)
			{
				this.BuildWindowInfo();
			}, e);
		}
		private void OnControlPanelDragDelta(object sender, DragDeltaEventArgs e)
		{
			Action cachedDragAction = null;
			base.TriggerCustomEvent(delegate(object state)
			{
				try
				{
					if (this.isCharting)
					{
						Dispatcher dispatcher = this.ChartControl.Dispatcher;
						Action action;
						if ((action = cachedDragAction) == null)
						{
							action = (cachedDragAction = delegate
							{
								if (this.controlPanel.alignment == NinjaTrader.NinjaScript.DrawingTools.TextPosition.Center)
								{
									this.controlPanel.Margin = new Thickness(5.0);
									return;
								}
								double num = 5.0;
								double num2 = (double)5f;
								double num3 = (double)5f;
								double num4 = num;
								if (this.controlPanel.HorizontalAlignment == global::System.Windows.HorizontalAlignment.Left)
								{
									num4 = this.controlPanel.Margin.Left + e.HorizontalChange;
								}
								else
								{
									num3 = this.controlPanel.Margin.Right - e.HorizontalChange;
								}
								double num5;
								if (this.controlPanel.VerticalAlignment == global::System.Windows.VerticalAlignment.Top)
								{
									num5 = this.controlPanel.Margin.Top + e.VerticalChange;
								}
								else
								{
									num5 = this.controlPanel.Margin.Top + e.VerticalChange;
								}
								num4 = Math.Max(0.0, num4);
								num5 = Math.Max(0.0, num5);
								num3 = Math.Max(0.0, num3);
								num2 = Math.Max(0.0, num2);
								this.controlPanel.Margin = new Thickness(num4, num5, num3, num2);
								this.CpPositionMarginLeft = this.controlPanel.Margin.Left;
								this.CpPositionMarginTop = this.controlPanel.Margin.Top;
								this.CpPositionMarginRight = this.controlPanel.Margin.Right;
								this.CpPositionMarginBottom = this.controlPanel.Margin.Bottom;
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
		private void OnControlPanelDragDoubleClick(object sender, MouseButtonEventArgs e)
		{
			base.TriggerCustomEvent(delegate(object state)
			{
				try
				{
					if (this.isCharting)
					{
						base.ChartControl.Dispatcher.InvokeAsync(delegate
						{
							this.SetControlPanelState();
						});
					}
				}
				catch (Exception ex)
				{
					this.PrintException(ex);
				}
			}, e);
		}
		private void SetControlPanelState()
		{
			this.CpMinimized = !this.CpMinimized;
			this.controlPanel.SetState(this.CpMinimized);
		}
		private void OnControlPanelBtnMiniClick(object sender, RoutedEventArgs e)
		{
			base.TriggerCustomEvent(delegate(object state)
			{
				try
				{
					if (this.isCharting)
					{
						base.ChartControl.Dispatcher.InvokeAsync(delegate
						{
							this.SetControlPanelState();
						});
					}
				}
				catch (Exception ex)
				{
					this.PrintException(ex);
				}
			}, e);
		}
		private void OnCbModeChange(object sender, SelectionChangedEventArgs e)
		{
			base.TriggerCustomEvent(delegate(object state)
			{
				try
				{
					if (this.isCharting)
					{
						base.ChartControl.Dispatcher.InvokeAsync(delegate
						{
							this.Mode = (DDQuantZone_Mode)this.controlPanel.cbMode.SelectedIndex;
							this.needReloadChart = true;
							if (this.Mode == DDQuantZone_Mode.Gaussian)
							{
								UIElement nudAdverseExcursion = this.controlPanel.nudAdverseExcursion;
								this.controlPanel.nudMaxAdverseExcursion.Visibility = Visibility.Collapsed;
								nudAdverseExcursion.Visibility = Visibility.Collapsed;
								UIElement cbAdverseExcursion = this.controlPanel.cbAdverseExcursion;
								this.controlPanel.cbMaxAdverseExcursion.Visibility = Visibility.Visible;
								cbAdverseExcursion.Visibility = Visibility.Visible;
								UIElement nudMaxFavorableExcursion = this.controlPanel.nudMaxFavorableExcursion;
								this.controlPanel.nudFavorableExcursion.Visibility = Visibility.Collapsed;
								nudMaxFavorableExcursion.Visibility = Visibility.Collapsed;
								UIElement cbMaxFavorableExcursion = this.controlPanel.cbMaxFavorableExcursion;
								this.controlPanel.cbFavorableExcursion.Visibility = Visibility.Visible;
								cbMaxFavorableExcursion.Visibility = Visibility.Visible;
								return;
							}
							UIElement cbAdverseExcursion2 = this.controlPanel.cbAdverseExcursion;
							this.controlPanel.cbMaxAdverseExcursion.Visibility = Visibility.Collapsed;
							cbAdverseExcursion2.Visibility = Visibility.Collapsed;
							UIElement nudAdverseExcursion2 = this.controlPanel.nudAdverseExcursion;
							this.controlPanel.nudMaxAdverseExcursion.Visibility = Visibility.Visible;
							nudAdverseExcursion2.Visibility = Visibility.Visible;
							UIElement cbMaxFavorableExcursion2 = this.controlPanel.cbMaxFavorableExcursion;
							this.controlPanel.cbFavorableExcursion.Visibility = Visibility.Collapsed;
							cbMaxFavorableExcursion2.Visibility = Visibility.Collapsed;
							UIElement nudMaxFavorableExcursion2 = this.controlPanel.nudMaxFavorableExcursion;
							this.controlPanel.nudFavorableExcursion.Visibility = Visibility.Visible;
							nudMaxFavorableExcursion2.Visibility = Visibility.Visible;
						});
					}
				}
				catch (Exception ex)
				{
					this.PrintException(ex);
				}
			}, e);
		}
		private void OnNudAdverseExcursionChange(object sender, RoutedEventArgs e)
		{
			base.TriggerCustomEvent(delegate(object state)
			{
				try
				{
					int num = (int)this.controlPanel.nudAdverseExcursion.Value;
					if (num > this.PMaxAdverseExcursion)
					{
						this.controlPanel.nudAdverseExcursion.Value = (double)this.PMaxAdverseExcursion;
					}
					else
					{
						this.needReloadChart = true;
						this.PAdverseExcursion = num;
					}
				}
				catch (Exception ex)
				{
					this.PrintException(ex);
				}
			}, e);
		}
		private void OnNudMaxAdverseExcursionChange(object sender, RoutedEventArgs e)
		{
			base.TriggerCustomEvent(delegate(object state)
			{
				try
				{
					int num = (int)this.controlPanel.nudMaxAdverseExcursion.Value;
					if (num < this.PAdverseExcursion)
					{
						this.controlPanel.nudMaxAdverseExcursion.Value = (double)this.PAdverseExcursion;
					}
					else
					{
						this.needReloadChart = true;
						this.PMaxAdverseExcursion = num;
					}
				}
				catch (Exception ex)
				{
					this.PrintException(ex);
				}
			}, e);
		}
		private void OnNudMaxFavorableExcursionChange(object sender, RoutedEventArgs e)
		{
			base.TriggerCustomEvent(delegate(object state)
			{
				try
				{
					int num = (int)this.controlPanel.nudMaxFavorableExcursion.Value;
					if (num < this.PFavorableExcursion)
					{
						this.controlPanel.nudMaxFavorableExcursion.Value = (double)this.PFavorableExcursion;
					}
					else
					{
						this.needReloadChart = true;
						this.PMaxFavorableExcursion = num;
					}
				}
				catch (Exception ex)
				{
					this.PrintException(ex);
				}
			}, e);
		}
		private void OnNudFavorableExcursionChange(object sender, RoutedEventArgs e)
		{
			base.TriggerCustomEvent(delegate(object state)
			{
				try
				{
					int num = (int)this.controlPanel.nudFavorableExcursion.Value;
					if (num > this.PMaxFavorableExcursion)
					{
						this.controlPanel.nudFavorableExcursion.Value = (double)this.PMaxFavorableExcursion;
					}
					else
					{
						this.needReloadChart = true;
						this.PFavorableExcursion = num;
					}
				}
				catch (Exception ex)
				{
					this.PrintException(ex);
				}
			}, e);
		}
		private void OnCbAdverseExcursionChange(object sender, SelectionChangedEventArgs e)
		{
			base.TriggerCustomEvent(delegate(object state)
			{
				try
				{
					double num = double.Parse(this.controlPanel.cbAdverseExcursion.SelectedItem.ToString());
					double num2 = double.Parse(NinjaTrader.NinjaScript.Indicators.DimDim.DDQuantZone.strAdverseExcursionArr[(int)this.GMaxAdverseExcursion]);
					if (num.ApproxCompare(num2) > 0)
					{
						this.controlPanel.cbAdverseExcursion.SelectedIndex = (int)this.GAdverseExcursion;
					}
					else
					{
						this.needReloadChart = true;
						this.GAdverseExcursion = (DDQuantZone_AdverseExcursion)this.controlPanel.cbAdverseExcursion.SelectedIndex;
					}
				}
				catch (Exception ex)
				{
					this.PrintException(ex);
				}
			}, e);
		}
		private void OnCbMaxAdverseExcursionChange(object sender, SelectionChangedEventArgs e)
		{
			base.TriggerCustomEvent(delegate(object state)
			{
				try
				{
					double num = double.Parse(this.controlPanel.cbMaxAdverseExcursion.SelectedItem.ToString());
					double num2 = double.Parse(NinjaTrader.NinjaScript.Indicators.DimDim.DDQuantZone.strAdverseExcursionArr[(int)this.GAdverseExcursion]);
					if (num.ApproxCompare(num2) < 0)
					{
						this.controlPanel.cbMaxAdverseExcursion.SelectedIndex = (int)this.GMaxAdverseExcursion;
					}
					else
					{
						this.needReloadChart = true;
						this.GMaxAdverseExcursion = (DDQuantZone_AdverseExcursion)this.controlPanel.cbMaxAdverseExcursion.SelectedIndex;
					}
				}
				catch (Exception ex)
				{
					this.PrintException(ex);
				}
			}, e);
		}
		private void OnCbMaxFavorableExcursionChange(object sender, SelectionChangedEventArgs e)
		{
			base.TriggerCustomEvent(delegate(object state)
			{
				try
				{
					double num = double.Parse(this.controlPanel.cbMaxFavorableExcursion.SelectedItem.ToString());
					double num2 = double.Parse(NinjaTrader.NinjaScript.Indicators.DimDim.DDQuantZone.strFavorableExcursionArr[(int)this.GFavorableExcursion]);
					if (num.ApproxCompare(num2) < 0)
					{
						this.controlPanel.cbMaxFavorableExcursion.SelectedIndex = (int)this.GMaxFavorableExcursion;
					}
					else
					{
						this.needReloadChart = true;
						this.GMaxFavorableExcursion = (DDQuantZone_FavorableExcursion)this.controlPanel.cbMaxFavorableExcursion.SelectedIndex;
					}
				}
				catch (Exception ex)
				{
					this.PrintException(ex);
				}
			}, e);
		}
		private void OnCbFavorableExcursionChange(object sender, SelectionChangedEventArgs e)
		{
			base.TriggerCustomEvent(delegate(object state)
			{
				try
				{
					double num = double.Parse(this.controlPanel.cbFavorableExcursion.SelectedItem.ToString());
					double num2 = double.Parse(NinjaTrader.NinjaScript.Indicators.DimDim.DDQuantZone.strFavorableExcursionArr[(int)this.GMaxFavorableExcursion]);
					if (num.ApproxCompare(num2) > 0)
					{
						this.controlPanel.cbFavorableExcursion.SelectedIndex = (int)this.GFavorableExcursion;
					}
					else
					{
						this.needReloadChart = true;
						this.GFavorableExcursion = (DDQuantZone_FavorableExcursion)this.controlPanel.cbFavorableExcursion.SelectedIndex;
					}
				}
				catch (Exception ex)
				{
					this.PrintException(ex);
				}
			}, e);
		}
		private void BuildWindowIndicator()
		{
			if (this.isCharting)
			{
				base.ChartControl.Dispatcher.InvokeAsync(delegate
				{
					try
					{
						if (this.windowIndicator != null && this.windowIndicator.IsLoaded)
						{
							if (this.windowIndicator.WindowState == global::System.Windows.WindowState.Minimized)
							{
								this.windowIndicator.WindowState = global::System.Windows.WindowState.Normal;
							}
							this.windowIndicator.Activate();
						}
						else
						{
							if (this.windowIndicator != null)
							{
								this.windowIndicator.Closing -= this.OnMainWindowNT_Closing;
								this.windowIndicator.SizeChanged -= new SizeChangedEventHandler(this.OnMainWindowChanged);
								this.windowIndicator.LocationChanged -= this.OnMainWindowChanged;
							}
							this.windowIndicator = new NTWindow
							{
								Caption = "QuantZone",
								Padding = new Thickness(0.0),
								MinWidth = 350.0,
								MinHeight = 200.0
							};
							this.gridIndicator = new Grid
							{
								Margin = new Thickness(10.0)
							};
							this.gridIndicator.RowDefinitions.Add(new RowDefinition
							{
								Height = new GridLength(1.0, GridUnitType.Star)
							});
							this.gridIndicator.RowDefinitions.Add(new RowDefinition
							{
								Height = default(GridLength)
							});
							this.gridIndicator.ColumnDefinitions.Add(new ColumnDefinition
							{
								Width = new GridLength(1.0, GridUnitType.Star),
								MinWidth = 100.0
							});
							this.gridIndicator.ColumnDefinitions.Add(new ColumnDefinition
							{
								Width = new GridLength(7.0)
							});
							this.gridIndicator.ColumnDefinitions.Add(new ColumnDefinition
							{
								Width = new GridLength(1.0, GridUnitType.Star),
								MinWidth = 100.0
							});
							this.GetAllMethods();
							this.indicatorListPage = new DDQuantZone.IndicatorsListPage(this.sortedListIndicatorItem, this.isLightTheme, this.MainWindowTextColor, this.mainWindowBorderColor);
							this.indicatorListPage.SetValue(Grid.ColumnProperty, 0);
							GridSplitter gridSplitter = new GridSplitter
							{
								Background = global::System.Windows.Media.Brushes.Transparent,
								HorizontalAlignment = global::System.Windows.HorizontalAlignment.Stretch
							};
							gridSplitter.SetValue(Grid.ColumnProperty, 1);
							List<string> list = new List<string>();
							foreach (object obj in Enum.GetValues(typeof(DDQuantZone_Operators)))
							{
								DDQuantZone_Operators DDQuantZone_Operators = (DDQuantZone_Operators)obj;
								if (DDQuantZone_Operators == DDQuantZone_Operators.Greater)
								{
									list.Add(">");
								}
								else if (DDQuantZone_Operators == DDQuantZone_Operators.Smaller)
								{
									list.Add("<");
								}
								else if (DDQuantZone_Operators == DDQuantZone_Operators.GreaterOrEqual)
								{
									list.Add(">=");
								}
								else if (DDQuantZone_Operators == DDQuantZone_Operators.SmallerOrEqual)
								{
									list.Add("<=");
								}
								else if (DDQuantZone_Operators == DDQuantZone_Operators.Equal)
								{
									list.Add("=");
								}
								else
								{
									list.Add("!=");
								}
							}
							this.indicatorPropsPage = new DDQuantZone.IndicatorPropertiesPage(this.dictMethodInfo, list, this.DocumentsPath, this.isLightTheme, this.MainWindowTextColor, this.mainWindowBorderColor);
							this.indicatorPropsPage.SetValue(Grid.ColumnProperty, 2);
							this.gridIndicator.Children.Add(this.indicatorListPage);
							this.gridIndicator.Children.Add(gridSplitter);
							this.gridIndicator.Children.Add(this.indicatorPropsPage);
							StackPanel stackPanel = new StackPanel
							{
								Orientation = global::System.Windows.Controls.Orientation.Horizontal,
								HorizontalAlignment = global::System.Windows.HorizontalAlignment.Right,
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
							this.indicatorListPage.DDSearchBar.TextChanged += this.OnDDSearchBar_TextChanged;
							this.indicatorListPage.lbxIndicatorResult.SelectionChanged += this.OnListIndicatorsResult_SelectionChanged;
							this.indicatorListPage.lbxIndicatorMain.SelectionChanged += this.OnListIndicatorsMain_SelectionChanged;
							this.indicatorListPage.DDSearchBar.Focus();
							this.gridIndicator.Children.Add(stackPanel);
							this.windowIndicator.Content = this.gridIndicator;
							if (this.isNullIndicators)
							{
								this.indicatorPropsPage.RenderPropsPageDefault((int)this.DefaultFastMAType, this.DefaultFastPeriod, (int)this.DefaultSlowMAType, this.DefaultSlowPeriod);
								this.indicatorPropsPage.checkBoxDefault.IsChecked = new bool?(true);
							}
							else
							{
								if (this.sortedListIndicatorItem.ContainsKey(this.IndicatorName))
								{
									int num = this.sortedListIndicatorItem.IndexOfKey(this.IndicatorName);
									this.indicatorListPage.lbxIndicatorMain.SelectedIndex = num;
									this.indicatorListPage.lbxIndicatorMain.ScrollIntoView(this.indicatorListPage.lbxIndicatorMain.SelectedItem);
								}
								this.indicatorPropsPage.checkBoxDefault.IsChecked = new bool?(false);
							}
							this.indicatorPropsPage.checkBoxDefault.Checked += delegate(object sender, RoutedEventArgs e)
							{
								this.indicatorPropsPage.RenderPropsPageDefault((int)this.DefaultFastMAType, this.DefaultFastPeriod, (int)this.DefaultSlowMAType, this.DefaultSlowPeriod);
							};
							this.indicatorPropsPage.checkBoxDefault.Unchecked += delegate(object sender, RoutedEventArgs e)
							{
								this.indicatorListPage.lbxIndicatorMain.ScrollIntoView(this.indicatorListPage.lbxIndicatorMain.SelectedItem);
							};
							this.windowIndicator.Closing += this.OnMainWindowNT_Closing;
							this.windowIndicator.SizeChanged += new SizeChangedEventHandler(this.OnMainWindowChanged);
							this.windowIndicator.LocationChanged += this.OnMainWindowChanged;
							this.windowIndicator.Show();
							this.windowIndicator.Top = this.MainWindowTop;
							this.windowIndicator.Left = this.MainWindowLeft;
						}
					}
					catch (Exception ex)
					{
						this.PrintException(ex);
					}
				});
			}
		}
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
							this.MainWindowLeft = this.windowIndicator.Left;
							this.MainWindowTop = this.windowIndicator.Top;
							this.MainWindowWidth = this.windowIndicator.Width;
							this.MainWindowHeight = this.windowIndicator.Height;
						});
					}
				}
				catch
				{
				}
			}, e);
		}
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
						this.indicatorListPage.DDSearchBar.TextChanged -= this.OnDDSearchBar_TextChanged;
						this.indicatorListPage.lbxIndicatorResult.SelectionChanged -= this.OnListIndicatorsResult_SelectionChanged;
						this.indicatorListPage.lbxIndicatorMain.SelectionChanged -= this.OnListIndicatorsMain_SelectionChanged;
						this.windowIndicator.Closing -= this.OnMainWindowNT_Closing;
						this.windowIndicator = null;
					});
				}
			}, e);
		}
		private void OnBtnCancle_Click(object sender, RoutedEventArgs e)
		{
			base.TriggerCustomEvent(delegate(object state)
			{
				try
				{
					if (this.isCharting)
					{
						base.ChartControl.Dispatcher.InvokeAsync(delegate
						{
							this.TerminateMainWindowNT();
						});
					}
				}
				catch
				{
				}
			}, e);
		}
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
							if (this.indicatorPropsPage.ListProps == null)
							{
								return;
							}
							if (this.indicatorPropsPage.ListProps.Count <= 0)
							{
								return;
							}
							if (this.yesNoMessageWindow.YesNoMessageWindow != null)
							{
								this.yesNoMessageWindow.YesNoMessageWindow.Close();
								this.yesNoMessageWindow.YesNoMessageWindow = null;
							}
							int num = 0;
							string text = string.Empty;
							int count = this.indicatorPropsPage.ListProps.Count;
							if (this.indicatorPropsPage.checkBoxDefault.IsChecked.Value)
							{
								DD_MAType DD_MAType = this.DefaultFastMAType;
								DD_MAType DD_MAType2 = this.DefaultSlowMAType;
								int num2 = this.DefaultFastPeriod;
								int num3 = this.DefaultSlowPeriod;
								for (int i = 0; i < count; i++)
								{
									DDQuantZone.PropertyItemInfo propertyItemInfo = this.indicatorPropsPage.ListProps[i];
									Control control = propertyItemInfo.Control;
									Type dataType = propertyItemInfo.DataType;
									if (control is ComboBox)
									{
										DD_MAType DD_MAType3 = (DD_MAType)(control as ComboBox).SelectedItem;
										if (i == 0)
										{
											DD_MAType = DD_MAType3;
										}
										else
										{
											DD_MAType2 = DD_MAType3;
										}
									}
									else
									{
										string text2 = (control as TextBox).Text;
										if (string.IsNullOrWhiteSpace(text2))
										{
											text = text + "\n\n⮚ " + propertyItemInfo.DisplayName;
											num++;
										}
										else
										{
											int num4 = Convert.ToInt32(text2);
											if (i == 1)
											{
												num2 = num4;
											}
											else
											{
												num3 = num4;
											}
										}
									}
								}
								if (num > 0)
								{
									this.yesNoMessageWindow.ShowMessageBoxOKButtonOnly(this.windowIndicator, string.Format("Please input the value for parameter{0} below:", (num > 1) ? "s" : string.Empty) + text, this.MainWindowTextColor, "QuantZone by DD.com", "OK");
									return;
								}
								this.TerminateMainWindowNT();
								bool flag = DD_MAType != this.DefaultFastMAType;
								bool flag2 = num2 != this.DefaultFastPeriod;
								bool flag3 = DD_MAType2 != this.DefaultSlowMAType;
								bool flag4 = num3 != this.DefaultSlowPeriod;
								if (flag || flag3 || flag2 || flag4 || !string.IsNullOrWhiteSpace(this.IndicatorName) || !string.IsNullOrWhiteSpace(this.IndicatorSetting))
								{
									this.DefaultFastMAType = DD_MAType;
									this.DefaultFastPeriod = num2;
									this.DefaultSlowMAType = DD_MAType2;
									this.DefaultSlowPeriod = num3;
									this.ShowRefreshChartMessage();
								}
								this.IndicatorName = string.Empty;
								this.IndicatorSetting = string.Empty;
								return;
							}
							else
							{
								if (this.indicatorListPage.lbxIndicatorMain.SelectedIndex < 0)
								{
									this.TerminateMainWindowNT();
								}
								PropertyInfo[] properties = this.indicatorPropsPage.SelectedIndicatorBase.GetType().GetProperties();
								List<DDQuantZone.ParamInfo> list = new List<DDQuantZone.ParamInfo>();
								int num5 = 0;
								for (int j = 0; j < count - 3; j++)
								{
									DDQuantZone.PropertyItemInfo propertyItemInfo2 = this.indicatorPropsPage.ListProps[j];
									string name = propertyItemInfo2.Name;
									PropertyInfo[] array = properties;
									for (int k = 0; k < array.Length; k++)
									{
										string name2 = array[k].Name;
										if (name2 == name)
										{
											Control control2 = propertyItemInfo2.Control;
											Type dataType2 = propertyItemInfo2.DataType;
											if (control2 is ComboBox)
											{
												object selectedItem = (control2 as ComboBox).SelectedItem;
												bool flag5 = dataType2 == typeof(ISeries<double>);
												if (dataType2.IsEnum || flag5)
												{
													Type underlyingType = Enum.GetUnderlyingType(flag5 ? typeof(PriceType) : dataType2);
													object obj = Convert.ChangeType(selectedItem, underlyingType);
													if (flag5)
													{
														num5 = Convert.ToInt32(obj);
													}
													list.Add(new DDQuantZone.ParamInfo
													{
														Name = name2,
														Value = obj.ToString()
													});
												}
											}
											else if (control2 is CheckBox)
											{
												bool? isChecked = (control2 as CheckBox).IsChecked;
												list.Add(new DDQuantZone.ParamInfo
												{
													Name = name2,
													Value = isChecked.ToString()
												});
											}
											else
											{
												string text3 = (control2 as TextBox).Text;
												if (string.IsNullOrWhiteSpace(text3))
												{
													text = text + "\n\n⮚ " + propertyItemInfo2.DisplayName;
													num++;
												}
												else
												{
													list.Add(new DDQuantZone.ParamInfo
													{
														Name = name2,
														Value = text3
													});
												}
											}
										}
									}
								}
								DDQuantZone.PropertyItemInfo propertyItemInfo3 = this.indicatorPropsPage.ListProps[count - 2];
								TextBox textBox = propertyItemInfo3.Control as TextBox;
								int selectedIndex = (propertyItemInfo3.OperatorControl as ComboBox).SelectedIndex;
								string text4 = textBox.Text;
								double num6;
								if (string.IsNullOrWhiteSpace(text4))
								{
									num6 = 0.0;
									text = text + "\n\n⮚ " + propertyItemInfo3.Name;
									num++;
								}
								else
								{
									num6 = Convert.ToDouble(text4);
								}
								DDQuantZone.PropertyItemInfo propertyItemInfo4 = this.indicatorPropsPage.ListProps[count - 1];
								TextBox textBox2 = propertyItemInfo4.Control as TextBox;
								int selectedIndex2 = (propertyItemInfo4.OperatorControl as ComboBox).SelectedIndex;
								string text5 = textBox2.Text;
								double num7;
								if (string.IsNullOrWhiteSpace(text5))
								{
									num7 = 0.0;
									text = text + "\n\n⮚ " + propertyItemInfo4.Name;
									num++;
								}
								else
								{
									num7 = Convert.ToDouble(text5);
								}
								if (num > 0)
								{
									this.yesNoMessageWindow.ShowMessageBoxOKButtonOnly(this.windowIndicator, string.Format("Please input the value for parameter{0} below:", (num > 1) ? "s" : string.Empty) + text, this.MainWindowTextColor, "QuantZone by DD.com", "OK");
									return;
								}
								string text6 = new JavaScriptSerializer().Serialize(list);
								string text7 = (this.indicatorListPage.lbxIndicatorMain.SelectedItem as DDQuantZone.IndicatorItem).IndicatorName;
								int selectedIndex3 = (this.indicatorPropsPage.ListProps[count - 3].Control as ComboBox).SelectedIndex;
								bool flag6 = text7 != this.IndicatorName;
								bool flag7 = this.IndicatorSetting != text6;
								bool flag8 = selectedIndex3 != this.PlotIndex;
								bool flag9 = num6 != this.ValueBullish;
								bool flag10 = num7 != this.ValueBearish;
								bool flag11 = selectedIndex != (int)this.OperatorBullish;
								bool flag12 = selectedIndex2 != (int)this.OperatorBearish;
								this.TerminateMainWindowNT();
								if (string.IsNullOrWhiteSpace(this.IndicatorName) || flag6 || flag7 || flag8 || flag9 || flag10 || flag11 || flag12)
								{
									this.IndicatorName = text7;
									this.IndicatorSetting = text6;
									this.InputSeriesIndex = num5;
									this.PlotIndex = selectedIndex3;
									this.ValueBullish = num6;
									this.ValueBearish = num7;
									this.OperatorBullish = (DDQuantZone_Operators)selectedIndex;
									this.OperatorBearish = (DDQuantZone_Operators)selectedIndex2;
									this.Namespace = this.indicatorPropsPage.SelectedIndicatorBase.GetType().Namespace + ".";
									this.controlPanel.btnTitle.Content = (this.controlPanel.btnMini.Content = text7);
									this.ShowRefreshChartMessage();
								}
								return;
							}
						});
					}
				}
				catch (Exception ex)
				{
					this.PrintException(ex);
				}
			}, e);
		}
		private void OnListIndicatorsMain_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			Action cachedSelectionAction = null;
			base.TriggerCustomEvent(delegate(object state)
			{
				if (this.isCharting)
				{
					Dispatcher dispatcher = this.ChartControl.Dispatcher;
					Action action;
					if ((action = cachedSelectionAction) == null)
					{
						action = (cachedSelectionAction = delegate
						{
							ListBox listBox = sender as ListBox;
							if (listBox == null)
							{
								return;
							}
							if (listBox.SelectedItems == null)
							{
								return;
							}
							if (listBox.SelectedItems.Count <= 0)
							{
								return;
							}
							DDQuantZone.IndicatorItem indicatorItem = listBox.SelectedItem as DDQuantZone.IndicatorItem;
							if (!this.sortedListIndicatorItem.ContainsKey(indicatorItem.IndicatorName))
							{
								return;
							}
							this.indicatorPropsPage.checkBoxDefault.IsChecked = new bool?(false);
							this.indicatorPropsPage.RenderPropsPage(indicatorItem, this.IndicatorName, this.IndicatorSetting, this.PlotIndex, this.ValueBullish, this.ValueBearish, this.OperatorBullish, this.OperatorBearish);
						});
					}
					dispatcher.InvokeAsync(action);
				}
			}, e);
		}
		private void OnListIndicatorsResult_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (this.indicatorListPage.lbxIndicatorResult.SelectedItem == null)
			{
				return;
			}
			foreach (object obj in ((IEnumerable)this.indicatorListPage.lbxIndicatorMain.Items))
			{
				if (((DDQuantZone.IndicatorItem)obj).IndicatorName == ((DDQuantZone.IndicatorItem)this.indicatorListPage.lbxIndicatorResult.SelectedItem).IndicatorName)
				{
					this.indicatorListPage.lbxIndicatorMain.SelectedItem = obj;
					break;
				}
			}
		}
		private void OnDDSearchBar_TextChanged(object sender, TextChangedEventArgs e)
		{
			base.TriggerCustomEvent(delegate(object state)
			{
				if (this.isCharting)
				{
					base.ChartControl.Dispatcher.InvokeAsync(delegate
					{
						string findContent = this.indicatorListPage.DDSearchBar.FindContent;
						if (findContent == "Start typing to search")
						{
							return;
						}
						this.indicatorListPage.FindIndicatorByName(findContent);
						this.indicatorListPage.lbxIndicatorMain.ScrollIntoView(this.indicatorListPage.lbxIndicatorMain.SelectedItem);
					});
				}
			}, e);
		}
		private void ShowRefreshChartMessage()
		{
			this.yesNoMessageWindow.ShowMessageBoxOKButtonOnly(this.chartWindow, "You have to reload the chart (press F5) for the new setting to take effect.", this.MainWindowTextColor, "QuantZone by DD.com", "OK");
			this.yesNoMessageWindow.YesNoMessageWindow.Closing += this.OnYesNoMessageWindow_Closing;
		}
		private void TerminateMainWindowNT()
		{
			if (this.windowIndicator != null)
			{
				this.windowIndicator.Close();
				this.windowIndicator.Closing -= this.OnMainWindowNT_Closing;
				this.windowIndicator.SizeChanged -= new SizeChangedEventHandler(this.OnMainWindowChanged);
				this.windowIndicator.LocationChanged -= this.OnMainWindowChanged;
				this.windowIndicator = null;
			}
		}
		private void BuildWindowInfo()
		{
			try
			{
				if (this.windowInfo != null && this.windowInfo.IsLoaded)
				{
					if (this.windowInfo.WindowState == global::System.Windows.WindowState.Minimized)
					{
						this.windowInfo.WindowState = global::System.Windows.WindowState.Normal;
					}
					this.windowInfo.Activate();
				}
				else
				{
					if (this.windowInfo != null)
					{
						this.windowInfo.Closing -= this.OnWindowInfo_Closing;
						this.windowInfo.SizeChanged -= new SizeChangedEventHandler(this.OnWindowInfoChanged);
						this.windowInfo.LocationChanged -= this.OnWindowInfoChanged;
					}
					this.windowInfo = new NTWindow
					{
						Caption = "QuantZone",
						Padding = new Thickness(0.0),
						MinWidth = 445.0,
						MinHeight = 445.0
					};
					Thickness thickness = new Thickness(10.0);
					DDQuantZone.InfoTab infoTab = new DDQuantZone.InfoTab(this.listSampleInfo, this.listMarketMetric, this.isLightTheme, this.MainWindowTextColor, this.mainWindowBorderColor);
					infoTab.Margin = thickness;
					TabControl tabControl = new TabControl();
					tabControl.Items.Add(new TabItem
					{
						Content = infoTab,
						Header = " Detail"
					});
					this.lineChartTab = new DDQuantZone.LineChartTab(this.isLightTheme)
					{
						Margin = thickness
					};
					this.lineChartTab.RefreshData(this.listSampleInfo, this.SampleRange, this.LineBullish.Brush, this.LineBearish.Brush);
					DDQuantZone.LineChartTab lineChartTab = this.lineChartTab;
					lineChartTab.HelpMenuItemClicked = (RoutedEventHandler)Delegate.Combine(lineChartTab.HelpMenuItemClicked, new RoutedEventHandler(delegate(object sender, RoutedEventArgs e)
					{
						base.TriggerCustomEvent(delegate(object state)
						{
							try
							{
								if (this.isCharting)
								{
									base.ChartControl.Dispatcher.InvokeAsync(delegate
									{
										if (this.scottPlotHelpWindow != null)
										{
											this.scottPlotHelpWindow.Close();
											this.scottPlotHelpWindow = null;
										}
										this.scottPlotHelpWindow = new HelpWindow();
										this.scottPlotHelpWindow.Show();
									});
								}
							}
							catch
							{
							}
						}, e);
					}));
					Label label = new Label
					{
						Content = "FREQUENCY DISTRIBUTION",
						Foreground = (this.isLightTheme ? global::System.Windows.Media.Brushes.Black : global::System.Windows.Media.Brushes.White),
						HorizontalContentAlignment = global::System.Windows.HorizontalAlignment.Center,
						VerticalContentAlignment = global::System.Windows.VerticalAlignment.Center,
						FontSize = 18.0,
						FontWeight = FontWeights.Bold
					};
					label.SetValue(Grid.RowProperty, 0);
					Grid grid = new Grid();
					grid.RowDefinitions.Add(new RowDefinition
					{
						Height = new GridLength(1.0, GridUnitType.Star)
					});
					grid.RowDefinitions.Add(new RowDefinition
					{
						Height = new GridLength(1.0, GridUnitType.Star)
					});
					grid.RowDefinitions.Add(new RowDefinition
					{
						Height = new GridLength(9.0, GridUnitType.Star)
					});
					this.lineChartTab.SetValue(Grid.RowProperty, 2);
					grid.Children.Add(this.lineChartTab);
					grid.Children.Add(label);
					Grid grid2 = new Grid();
					grid2.ColumnDefinitions.Add(new ColumnDefinition());
					grid2.ColumnDefinitions.Add(new ColumnDefinition());
					grid2.ColumnDefinitions.Add(new ColumnDefinition());
					grid2.ColumnDefinitions.Add(new ColumnDefinition());
					Label label2 = new Label
					{
						Height = 2.0,
						Width = 80.0,
						Background = this.LineBullish.Brush,
						HorizontalContentAlignment = global::System.Windows.HorizontalAlignment.Center,
						VerticalContentAlignment = global::System.Windows.VerticalAlignment.Center,
						HorizontalAlignment = global::System.Windows.HorizontalAlignment.Stretch,
						VerticalAlignment = global::System.Windows.VerticalAlignment.Stretch
					};
					label2.SetValue(Grid.ColumnProperty, 0);
					grid2.Children.Add(label2);
					TextBlock textBlock = new TextBlock
					{
						Text = "Bullish",
						FontSize = 13.0,
						Foreground = (this.isLightTheme ? global::System.Windows.Media.Brushes.Black : global::System.Windows.Media.Brushes.White),
						VerticalAlignment = global::System.Windows.VerticalAlignment.Center,
						HorizontalAlignment = global::System.Windows.HorizontalAlignment.Left,
						Margin = new Thickness(10.0, 0.0, 0.0, 0.0),
						FontWeight = FontWeights.Bold,
						TextTrimming = TextTrimming.CharacterEllipsis
					};
					textBlock.SetValue(Grid.ColumnProperty, 1);
					grid2.Children.Add(textBlock);
					Label label3 = new Label
					{
						Height = 2.0,
						Width = 80.0,
						Background = this.LineBearish.Brush,
						HorizontalContentAlignment = global::System.Windows.HorizontalAlignment.Center,
						VerticalContentAlignment = global::System.Windows.VerticalAlignment.Center,
						HorizontalAlignment = global::System.Windows.HorizontalAlignment.Stretch,
						VerticalAlignment = global::System.Windows.VerticalAlignment.Stretch
					};
					label3.SetValue(Grid.ColumnProperty, 2);
					grid2.Children.Add(label3);
					TextBlock textBlock2 = new TextBlock
					{
						Text = "Bearish",
						FontSize = 13.0,
						Foreground = (this.isLightTheme ? global::System.Windows.Media.Brushes.Black : global::System.Windows.Media.Brushes.White),
						VerticalAlignment = global::System.Windows.VerticalAlignment.Center,
						HorizontalAlignment = global::System.Windows.HorizontalAlignment.Left,
						Margin = new Thickness(10.0, 0.0, 0.0, 0.0),
						FontWeight = FontWeights.Bold,
						TextTrimming = TextTrimming.CharacterEllipsis
					};
					textBlock2.SetValue(Grid.ColumnProperty, 3);
					grid2.Children.Add(textBlock2);
					Border border = new Border
					{
						Height = 30.0,
						BorderBrush = global::System.Windows.Media.Brushes.Gray,
						CornerRadius = new CornerRadius(3.0),
						BorderThickness = new Thickness(1.0),
						HorizontalAlignment = global::System.Windows.HorizontalAlignment.Stretch,
						VerticalAlignment = global::System.Windows.VerticalAlignment.Stretch,
						Margin = new Thickness(100.0, 0.0, 100.0, 0.0)
					};
					border.SetValue(Grid.RowProperty, 1);
					border.Child = grid2;
					grid.Children.Add(border);
					tabControl.Items.Add(new TabItem
					{
						Content = grid,
						Header = "Graph"
					});
					this.btnClose = new Button
					{
						Content = "Close",
						Height = 30.0,
						MinWidth = 100.0,
						MinHeight = 30.0,
						HorizontalAlignment = global::System.Windows.HorizontalAlignment.Right,
						ToolTip = string.Empty,
						Cursor = Cursors.Hand
					};
					this.btnClose.SetValue(Grid.RowProperty, 1);
					this.btnClose.Click += this.OnBtnClose_Click;
					Grid grid3 = new Grid
					{
						Margin = new Thickness(10.0)
					};
					grid3.RowDefinitions.Add(new RowDefinition
					{
						Height = new GridLength(1.0, GridUnitType.Star)
					});
					grid3.RowDefinitions.Add(new RowDefinition
					{
						Height = default(GridLength)
					});
					grid3.Children.Add(tabControl);
					grid3.Children.Add(this.btnClose);
					this.windowInfo.Content = grid3;
					this.windowInfo.Closing += this.OnWindowInfo_Closing;
					this.windowInfo.SizeChanged += new SizeChangedEventHandler(this.OnWindowInfoChanged);
					this.windowInfo.LocationChanged += this.OnWindowInfoChanged;
					this.windowInfo.Opacity = 0.0;
					this.windowInfo.Show();
					this.windowInfo.Width = ((this.InfoWindowWidth < 0.0) ? 445.0 : Math.Max(445.0, this.InfoWindowWidth));
					this.windowInfo.Height = ((this.InfoWindowHeight < 0.0) ? 445.0 : Math.Max(445.0, this.InfoWindowHeight));
					if (this.InfoWindowTop > 0.0)
					{
						this.windowInfo.Top = this.InfoWindowTop;
					}
					if (this.InfoWindowLeft > 0.0)
					{
						this.windowInfo.Left = this.InfoWindowLeft;
					}
					this.windowInfo.Opacity = 1.0;
				}
			}
			catch (Exception ex)
			{
				this.PrintException(ex);
			}
		}
		private static void SetScootPlotStyle(WpfPlot scottPlot, bool isLightTheme = true)
		{
			if (isLightTheme)
			{
				return;
			}
			global::System.Drawing.Color color = global::System.Drawing.Color.FromArgb(255, 20, 20, 20);
			global::System.Drawing.Color color2 = global::System.Drawing.Color.FromArgb(255, 30, 30, 30);
			global::System.Drawing.Color color3 = global::System.Drawing.Color.FromArgb(255, 40, 40, 40);
			global::System.Drawing.Color lightGray = global::System.Drawing.Color.LightGray;
			scottPlot.Plot.Style(new global::System.Drawing.Color?(color), new global::System.Drawing.Color?(color2), new global::System.Drawing.Color?(color3), new global::System.Drawing.Color?(lightGray), new global::System.Drawing.Color?(lightGray), new global::System.Drawing.Color?(lightGray));
		}
		private static string CustomTickFormatterXAxis(double position)
		{
			string text = position.ToString("0.000000");
			if (!(text.Substring(text.IndexOf('.') + 1) == "000000"))
			{
				return string.Empty;
			}
			return position.ToString("0");
		}
		private static readonly Func<double, string> CustomTickFormatterXAxisDelegate = new Func<double, string>(CustomTickFormatterXAxis);
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
		private static void NumericTextBox_LostFocusHandler(TextBox textBox, int minValue = 1)
		{
			double naN = double.NaN;
			if (double.TryParse(textBox.Text, out naN) && !double.IsNaN(naN))
			{
				textBox.Text = naN.ToString();
				return;
			}
			textBox.Text = minValue.ToString();
		}
		private void OnWindowInfoChanged(object sender, EventArgs e)
		{
			base.TriggerCustomEvent(delegate(object state)
			{
				try
				{
					if (this.isCharting)
					{
						base.ChartControl.Dispatcher.InvokeAsync(delegate
						{
							this.InfoWindowLeft = this.windowInfo.Left;
							this.InfoWindowTop = this.windowInfo.Top;
							this.InfoWindowWidth = this.windowInfo.Width;
							this.InfoWindowHeight = this.windowInfo.Height;
						});
					}
				}
				catch
				{
				}
			}, e);
		}
		private void OnWindowInfo_Closing(object sender, CancelEventArgs e)
		{
			base.TriggerCustomEvent(delegate(object state)
			{
				if (this.isCharting)
				{
					base.ChartControl.Dispatcher.InvokeAsync(delegate
					{
						this.btnClose.Click -= this.OnBtnClose_Click;
						this.windowInfo.Closing -= this.OnWindowInfo_Closing;
						this.windowInfo = null;
					});
				}
			}, e);
		}
		private void OnBtnClose_Click(object sender, RoutedEventArgs e)
		{
			base.TriggerCustomEvent(delegate(object state)
			{
				try
				{
					if (this.isCharting)
					{
						base.ChartControl.Dispatcher.InvokeAsync(delegate
						{
							this.TerminateMainWindowInfo();
						});
					}
				}
				catch
				{
				}
			}, e);
		}
		private void TerminateMainWindowInfo()
		{
			if (this.windowInfo != null)
			{
				this.windowInfo.Close();
				this.windowInfo.Closing -= this.OnWindowInfo_Closing;
				this.windowInfo.SizeChanged -= new SizeChangedEventHandler(this.OnWindowInfoChanged);
				this.windowInfo.LocationChanged -= this.OnWindowInfoChanged;
				this.windowInfo = null;
			}
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
			if (this.isCustomMarkerRenderingMethod)
			{
				if (!this.dictMarkerInfo.ContainsKey(base.CurrentBar))
				{
					this.dictMarkerInfo.Add(base.CurrentBar, isBullish);
					return;
				}
			}
			else
			{
				string text = "DDQuantZone.marker." + base.CurrentBar.ToString();
				global::System.Windows.Media.Brush brush = (isBullish ? this.MarkerBrushBullish : this.MarkerBrushBearish);
				double num = (isBullish ? base.High[0] : base.Low[0]);
				string text2 = (isBullish ? this.MarkerStringBullish : this.MarkerStringBearish);
				text2 = this.FormatMarkerString(text2);
				int num2 = Convert.ToInt32(this.ComputeTextSize(text2, this.MarkerFont, this.ScreenDPI).Height);
				int num3 = (isBullish ? 1 : (-1)) * (this.MarkerOffset + num2 / 2);
				NinjaTrader.NinjaScript.DrawingTools.Text text3 = NinjaTrader.NinjaScript.DrawingTools.Draw.Text(this, text, base.IsAutoScale, text2, 0, num, num3, brush, this.MarkerFont, global::System.Windows.TextAlignment.Center, global::System.Windows.Media.Brushes.Transparent, global::System.Windows.Media.Brushes.Transparent, 0);
				if (!this.SwitchedOnAll)
				{
					text3.IsVisible = false;
				}
			}
		}
		public override string FormatPriceMarker(double price)
		{
			return base.Instrument.MasterInstrument.FormatPrice(base.Instrument.MasterInstrument.RoundToTickSize(price), true);
		}
		public static string[] strAdverseExcursionArr = new string[] { "50", "84.2", "97.8", "99.2" };
		public static string[] strFavorableExcursionArr = new string[] { "84.2", "50", "15.6" };
		public static string[] strModeArr = new string[] { "PERCENTILE", "GAUSSIAN" };
		public const string openWindowIconData = "M19,19H5V5H12V3H5C3.89,3 3,3.89 3,5V19C3,20.1 3.89,21 5,21H19C20.1,21 21,20.1 21,19V12H19V19M14,3V5H17.59L7.76,14.83L9.17,16.24L19,6.41V10H21V3H14Z";
		private const byte marginValue = 10;
		private const int windowInfoMinWidth = 445;
		private const int windowInfoMinHeight = 445;
		private const byte buttonMinWidth = 100;
		private const byte buttonMinHeight = 30;
		private const byte paramControlMinWidth = 50;
		private const byte paramControlMinHeight = 20;
		private const byte splitPanelMinWidth = 100;
		private const byte btnFontSize = 12;
		private const string expandIcon = "\n\n⮚ ";
		private const string dotChar = ".";
		private const char minusChar = '-';
		private const string textToolTipDrag = "Drag to anywhere; double click to {0}.";
		private const string searchTemplatePlaceHolder = "Start typing to search";
		private const string msgInputParameter_StringFormat = "Please input the value for parameter{0} below:";
		private const string contentTitleDefault = "Select an indicator...";
		private const string msgInputReload = "You have to reload the chart (press F5) for the new setting to take effect.";
		private const string msgNotificationIndicatorCannotLoad = "An error occurred while loading the indicator.\nPlease reload the chart (press F5) and try a different indicator.";
		private const string textToolTipEnabledAutofit = "Auto-fit is enabled";
		private const string textToolTipDisabledAutofit = "Auto-fit is disabled";
		private const string textTooltipBtnTitle = "Click to choose an indicator.";
		private const string customDll = "NinjaTrader.Custom.dll";
		private const string ntCustomPath = "NinjaTrader 8\\bin\\Custom\\";
		private const byte padding = 22;
		private const string greaterOp = ">";
		private const string smallerOp = "<";
		private const string smallerOrEqualOp = "<=";
		private const string greaterOrEqualOp = ">=";
		private const string equalOp = "=";
		private const string unequalOp = "!=";
		private const int defaultValBullish = 1;
		private const int defaultValBearish = -1;
		private const DDQuantZone_Operators defaultOperator = DDQuantZone_Operators.Equal;
		private NinjaTrader.NinjaScript.DrawingTools.TextPosition controlPanelPositionAlignment;
		private DD_TextPosition togglePositionAlignment;
		private const int defaultMargin = 5;
		private const string toolTipSpace = "  ";
		private List<string> listIndicatorExcluded = new List<string>
		{
			"DDEOBExit", "DDEOBOrdering", "DDGlobalZlert", "DDBracketOrdering", "DDMultiTimeframeFusion", "DDMultiTimeframeFusion_v2", "HelloWin_CaptainOptimusStrong", "HelloWin_CaptainOptimusStrong_v2", "HelloWin", "HelloWin_InfinityAlgoEngine",
			"DDMultiInstrumentSynergy", "DDResources", "DDATR", "DDBarStatus", "DDBidAskDisplay", "DDHelperMFI", "DDHelperRSI", "DDHelperSMMA", "DDHelperStochastic", "DDTickDataMicroscope"
		};
		private IndicatorBase indicatorBase;
		private IndicatorBase[] cacheIndicatorBaseArr;
		private Dictionary<string, MethodInfo> dictMethodInfo;
		private SortedList<string, DDQuantZone.IndicatorItem> sortedListIndicatorItem;
		private List<DDQuantZone.PlotOrDataSeriesInfo> listPlotOrDataSeries;
		private NTWindow windowIndicator;
		private NTWindow windowInfo;
		private ShowYesNoMessageWindow yesNoMessageWindow;
		private Chart chartWindow;
		private HelpWindow scottPlotHelpWindow;
		private List<DDQuantZone.SampleInfo> listSampleInfo;
		private bool isPercentMode;
		private SortedList<int, DDQuantZone.ZoneInfo> sortedListZoneInfoActive;
		private SortedList<int, DDQuantZone.ZoneInfo> sortedListZoneInfoInactive;
		private List<DDQuantZone.LineInfo> listLineInfo;
		private global::SharpDX.Direct2D1.GradientStop[] zoneBullishGradientStop1;
		private global::SharpDX.Direct2D1.GradientStop[] zoneBearishGradientStop1;
		private global::SharpDX.Direct2D1.GradientStop[] zoneBullishGradientStop2;
		private global::SharpDX.Direct2D1.GradientStop[] zoneBearishGradientStop2;
		private global::SharpDX.Direct2D1.GradientStop[] zoneBullishGradientStop3;
		private global::SharpDX.Direct2D1.GradientStop[] zoneBearishGradientStop3;
		private global::SharpDX.Direct2D1.GradientStop[] zoneBullishGradientStop4;
		private global::SharpDX.Direct2D1.GradientStop[] zoneBearishGradientStop4;
		private global::System.Windows.Media.Brush zoneBullish1;
		private global::System.Windows.Media.Brush zoneBearish1;
		private global::System.Windows.Media.Brush zoneBullish2;
		private global::System.Windows.Media.Brush zoneBearish2;
		private global::System.Windows.Media.Brush zoneBullish3;
		private global::System.Windows.Media.Brush zoneBearish3;
		private global::System.Windows.Media.Brush zoneBullish4;
		private global::System.Windows.Media.Brush zoneBearish4;
		private DDQuantZone.MarketMetric mmMin;
		private DDQuantZone.MarketMetric mmMax;
		private DDQuantZone.MarketMetric mmTotalSample;
		private DDQuantZone.MarketMetric mmAverage;
		private DDQuantZone.MarketMetric mmStdDeviation;
		private DDQuantZone.MarketMetric mmLevel1;
		private DDQuantZone.MarketMetric mmLevel2;
		private DDQuantZone.MarketMetric mmLevel3;
		private DDQuantZone.MarketMetric mmMaxFavorableExcursion;
		private DDQuantZone.MarketMetric mmFavorableExcursion;
		private List<DDQuantZone.MarketMetric> listMarketMetric;
		private DDQuantZone.MarketMetric mmAdverseExcursionPanel;
		private DDQuantZone.MarketMetric mmMaxAdverseExcursionPanel;
		private DDQuantZone.MarketMetric mmMaxFavorableExcursionPanel;
		private DDQuantZone.MarketMetric mmFavorableExcursionPanel;
		private double maxValue = double.MinValue;
		private double minValue = double.MaxValue;
		private Dictionary<int, DDQuantZone.PlotInfo> dictPlotInfo;
		private bool needReloadChart;
		private Dictionary<int, bool> dictMarkerInfo;
		private bool isCustomMarkerRenderingMethod;
		private bool isZoneDisable;
		private const string nickname = "quantzone:exc";
		private bool isUptrend;
		private const string prefix = "DDQuantZone";
		private const string indicatorName = "QuantZone";
		private const string indicatorNameFull = "QuantZone by DD.com";
		private const string receiverEmail = "receiver@example.com";
		private bool isCharting;
		private bool isLightTheme;
		private static global::System.Windows.Media.Brush windowBackground;
		private float barOutlineWidth;
		private bool isNullIndicators;
		private bool isInitOK;
		private int idxHighest;
		private int idxLowest = -1;
		private double highest = double.MinValue;
		private double lowest = double.MaxValue;
		private bool isTrendChange;
		private List<DDQuantZone.SwingPoint> listSwingPoint;
		private DDQuantZone.ZoneInfo zoneInfo;
		private double nextMinUp;
		private double nextMaxUp;
		private double nextMinDown;
		private double nextMaxDown;
		private int countSkip;
		private bool isDrawBox;
		private bool isBullishFirst;
		private bool isBearishFirst;
		private bool isStartPlotOK;
		private bool isMFEBroken;
		private bool isFEBroken;
		private bool isScaleMaxOK;
		private bool isScaleMinOK;
		private DDQuantZone.DraggablePanel controlPanel;
		private DDQuantZone.IndicatorsListPage indicatorListPage;
		private DDQuantZone.IndicatorPropertiesPage indicatorPropsPage;
		private Grid gridIndicator;
		private Button btnOK;
		private Button btnCancel;
		private global::System.Windows.Media.Brush mainWindowBorderColor;
		private Button btnClose;
		private DDQuantZone.LineChartTab lineChartTab;
		private class ZoneInfo
		{
			public bool IsTop { get; set; }
			public int BarStart { get; set; }
			public int BarEnd { get; set; }
			public double PriceTop { get; }
			public double PriceBottom { get; }
			public int LineBarEnd { get; set; } = -1;
			public string AEStr { get; set; }
			public string MAEStr { get; set; }
			public ZoneInfo(bool isTop, double priceTop, double priceBottom, int barStart, int barEnd, string aeStr, string maeStr)
			{
				this.IsTop = isTop;
				this.PriceTop = priceTop;
				this.PriceBottom = priceBottom;
				this.BarStart = barStart;
				this.BarEnd = barEnd;
				this.AEStr = aeStr;
				this.MAEStr = maeStr;
			}
		}
		public class LineInfo
		{
			public bool IsTop { get; set; }
			public int BarStart { get; set; }
			public int BarEnd { get; set; }
			public double Price { get; set; }
			public LineInfo(bool isTop, double price, int barStart, int barEnd)
			{
				this.IsTop = isTop;
				this.Price = price;
				this.BarStart = barStart;
				this.BarEnd = barEnd;
			}
		}
		public class PlotInfo
		{
			public bool IsTop { get; set; }
			public string MaxFavorableExcursion { get; set; }
			public string FavorableExcursion { get; set; }
			public PlotInfo(bool isTop, string maxFavorableExcursion, string favorableExcursion)
			{
				this.IsTop = isTop;
				this.MaxFavorableExcursion = maxFavorableExcursion;
				this.FavorableExcursion = favorableExcursion;
			}
		}
		private class SwingPoint
		{
			public bool IsTop { get; set; }
			public double Price { get; set; }
			public int BarStart { get; set; }
			public int BarEnd { get; set; }
			public int SampleIndex { get; set; }
			public double SampleDistance { get; set; }
			public SwingPoint(bool isTop, double price, int barStart, int barEnd)
			{
				this.IsTop = isTop;
				this.Price = price;
				this.BarStart = barStart;
				this.BarEnd = barEnd;
			}
		}
		public class SampleInfo : INotifyPropertyChanged
		{
			public int Index { get; set; }
			public int To { get; set; }
			public int From { get; set; }
			public string SampleRange { get; set; }
			public int SampleIncrease
			{
				get
				{
					return this._sampleIncrease;
				}
				set
				{
					if (this._sampleIncrease == value)
					{
						return;
					}
					this._sampleIncrease = value;
					this.OnPropertyChanged("SampleIncrease");
				}
			}
			public double ProbabilityIncreases
			{
				get
				{
					return Math.Round(this._probabilityIncreases, 2);
				}
				set
				{
					if (this._probabilityIncreases == value)
					{
						return;
					}
					this._probabilityIncreases = value;
					this.OnPropertyChanged("ProbabilityIncreases");
				}
			}
			public int SampleDecreases
			{
				get
				{
					return this._sampleDecreases;
				}
				set
				{
					if (this._sampleDecreases == value)
					{
						return;
					}
					this._sampleDecreases = value;
					this.OnPropertyChanged("SampleDecreases");
				}
			}
			public double ProbabilityDecreases
			{
				get
				{
					return Math.Round(this._probabilityDecreases, 2);
				}
				set
				{
					if (this._probabilityDecreases == value)
					{
						return;
					}
					this._probabilityDecreases = value;
					this.OnPropertyChanged("ProbabilityDecreases");
				}
			}
			public event PropertyChangedEventHandler PropertyChanged;
			protected void OnPropertyChanged([CallerMemberName] string propName = null)
			{
				PropertyChangedEventHandler propertyChanged = this.PropertyChanged;
				if (propertyChanged == null)
				{
					return;
				}
				propertyChanged(this, new PropertyChangedEventArgs(propName));
			}
			private int _sampleIncrease;
			private double _probabilityIncreases;
			private int _sampleDecreases;
			private double _probabilityDecreases;
		}
		public class MarketMetric : INotifyPropertyChanged
		{
			public string Label { get; set; }
			public double Bullish
			{
				get
				{
					return this._bullish;
				}
				set
				{
					if (this._bullish != value)
					{
						this._bullish = value;
						this.OnPropertyChanged("Bullish");
					}
				}
			}
			public double Bearish
			{
				get
				{
					return this._bearish;
				}
				set
				{
					if (this._bearish != value)
					{
						this._bearish = value;
						this.OnPropertyChanged("Bearish");
					}
				}
			}
			public MarketMetric(string label)
			{
				this.Label = label;
			}
			public event PropertyChangedEventHandler PropertyChanged;
			protected void OnPropertyChanged([CallerMemberName] string name = null)
			{
				PropertyChangedEventHandler propertyChanged = this.PropertyChanged;
				if (propertyChanged == null)
				{
					return;
				}
				propertyChanged(this, new PropertyChangedEventArgs(name));
			}
			private double _bullish;
			private double _bearish;
		}
		private class DraggablePanel : Grid
		{
			public DraggablePanel(string indicatorName, DDQuantZone.MarketMetric mmAdverseExcursion, DDQuantZone.MarketMetric mmMaxAdverseExcursion, DDQuantZone.MarketMetric mmMaxFavorableExcursion, DDQuantZone.MarketMetric mmFavorableExcursion, bool minimized, int minBtnWidth, global::System.Windows.Media.Brush titleBackground, global::System.Windows.Media.Brush titleTextColor, double textExecutionSize, global::System.Windows.Media.Brush textExecutionBrush, double textSettingSize, global::System.Windows.Media.Brush dragBrush, NinjaTrader.NinjaScript.DrawingTools.TextPosition alignment, Thickness thickness)
			{
				this.alignment = alignment;
				this.SetPosition(thickness);
				this.drag = new Thumb
				{
					Width = 6.0,
					Cursor = Cursors.SizeAll
				};
				FrameworkElementFactory frameworkElementFactory = new FrameworkElementFactory(typeof(Border));
				frameworkElementFactory.SetValue(Border.BackgroundProperty, dragBrush);
				this.drag.Template = new ControlTemplate(typeof(Thumb))
				{
					VisualTree = frameworkElementFactory
				};
				this.drag.SetValue(Grid.ColumnProperty, 0);
				base.Children.Add(this.drag);
				this.btnMini = this.CreateButton(string.IsNullOrEmpty(indicatorName) ? "Minimized" : indicatorName, string.Empty, textExecutionSize, dragBrush, textExecutionBrush, minBtnWidth, "  Click to restore.", 0, 0, 0);
				this.btnMini.SetValue(Grid.ColumnProperty, 1);
				base.Children.Add(this.btnMini);
				this.gridContent = new Grid();
				base.Children.Add(this.gridContent);
				this.gridContent.RowDefinitions.Add(new RowDefinition());
				this.btnTitle = this.CreateButton(string.IsNullOrEmpty(indicatorName) ? "Select an indicator..." : indicatorName, string.Empty, textExecutionSize, titleBackground, titleTextColor, -1, null, 0, 0, 0);
				this.btnTitle.HorizontalAlignment = global::System.Windows.HorizontalAlignment.Stretch;
				this.btnTitle.Cursor = Cursors.Hand;
				global::System.Windows.Shapes.Path path = new global::System.Windows.Shapes.Path
				{
					Data = global::System.Windows.Media.Geometry.Parse("M19,19H5V5H12V3H5C3.89,3 3,3.89 3,5V19C3,20.1 3.89,21 5,21H19C20.1,21 21,20.1 21,19V12H19V19M14,3V5H17.59L7.76,14.83L9.17,16.24L19,6.41V10H21V3H14Z"),
					Height = 15.0,
					Fill = global::System.Windows.Media.Brushes.White,
					Stretch = global::System.Windows.Media.Stretch.Uniform,
					SnapsToDevicePixels = true
				};
				this.btnInfo = this.CreateButton(string.Empty, string.Empty, textExecutionSize, titleBackground, titleTextColor, -1, null, 1, 0, 0);
				this.btnInfo.HorizontalAlignment = global::System.Windows.HorizontalAlignment.Stretch;
				this.btnInfo.Cursor = Cursors.Hand;
				this.btnInfo.Content = path;
				Grid grid = new Grid();
				grid.ColumnDefinitions.Add(new ColumnDefinition
				{
					Width = new GridLength(1.0, GridUnitType.Star)
				});
				grid.ColumnDefinitions.Add(new ColumnDefinition
				{
					Width = default(GridLength)
				});
				grid.SetValue(Grid.RowProperty, 0);
				grid.SetValue(Grid.ColumnSpanProperty, 4);
				grid.Children.Add(this.btnTitle);
				grid.Children.Add(this.btnInfo);
				this.gridContent.Children.Add(grid);
				Thickness thickness2 = new Thickness(1.0, 1.0, 0.0, 0.0);
				Label label = this.CreateLabel("Mode: ", global::System.Windows.Media.Brushes.DimGray, thickness2, textSettingSize, 0, 1, 0);
				label.HorizontalContentAlignment = global::System.Windows.HorizontalAlignment.Center;
				label.VerticalContentAlignment = global::System.Windows.VerticalAlignment.Center;
				this.gridContent.Children.Add(label);
				this.gridContent.RowDefinitions.Add(new RowDefinition());
				this.cbMode = new ComboBox
				{
					Background = global::System.Windows.Media.Brushes.Gray,
					Foreground = global::System.Windows.Media.Brushes.White,
					BorderBrush = global::System.Windows.Media.Brushes.Gray,
					Margin = new Thickness(1.0, 1.0, 0.0, 0.0),
					HorizontalContentAlignment = global::System.Windows.HorizontalAlignment.Center,
					VerticalContentAlignment = global::System.Windows.VerticalAlignment.Center,
					HorizontalAlignment = global::System.Windows.HorizontalAlignment.Stretch,
					VerticalAlignment = global::System.Windows.VerticalAlignment.Stretch,
					FontSize = textSettingSize,
					ToolTip = "  Mode",
					Cursor = Cursors.Arrow,
					Focusable = false,
					ItemsSource = NinjaTrader.NinjaScript.Indicators.DimDim.DDQuantZone.strModeArr
				};
				this.cbMode.SetValue(Grid.ColumnProperty, 1);
				this.cbMode.SetValue(Grid.ColumnSpanProperty, 2);
				this.cbMode.SetValue(Grid.RowProperty, 1);
				this.gridContent.Children.Add(this.cbMode);
				this.gridContent.RowDefinitions.Add(new RowDefinition());
				this.gridContent.RowDefinitions.Add(new RowDefinition());
				Label label2 = this.CreateLabel("Probability: ", global::System.Windows.Media.Brushes.DimGray, thickness2, textSettingSize, 0, 2, 0);
				this.gridContent.Children.Add(label2);
				Label label3 = this.CreateLabel("%", global::System.Windows.Media.Brushes.DimGray, thickness2, textSettingSize, 1, 2, 0);
				label3.HorizontalContentAlignment = global::System.Windows.HorizontalAlignment.Center;
				label3.VerticalContentAlignment = global::System.Windows.VerticalAlignment.Center;
				this.gridContent.Children.Add(label3);
				Label label4 = this.CreateLabel("Tick", global::System.Windows.Media.Brushes.DimGray, thickness2, textSettingSize, 2, 2, 0);
				label4.HorizontalContentAlignment = global::System.Windows.HorizontalAlignment.Center;
				label4.VerticalContentAlignment = global::System.Windows.VerticalAlignment.Center;
				this.gridContent.Children.Add(label4);
				this.gridContent.RowDefinitions.Add(new RowDefinition());
				this.gridContent.RowDefinitions.Add(new RowDefinition());
				this.lbAdverseExcursion = this.CreateLabel("AE", global::System.Windows.Media.Brushes.DimGray, thickness2, textSettingSize, 0, 3, 0);
				this.gridContent.Children.Add(this.lbAdverseExcursion);
				this.lbMaxAdverseExcursion = this.CreateLabel("MAE", global::System.Windows.Media.Brushes.DimGray, thickness2, textSettingSize, 0, 4, 0);
				this.gridContent.Children.Add(this.lbMaxAdverseExcursion);
				this.cbAdverseExcursion = this.CreateComboBox(textSettingSize, 1, 3, 0);
				this.cbAdverseExcursion.ItemsSource = NinjaTrader.NinjaScript.Indicators.DimDim.DDQuantZone.strAdverseExcursionArr;
				this.gridContent.Children.Add(this.cbAdverseExcursion);
				this.cbMaxAdverseExcursion = this.CreateComboBox(textSettingSize, 1, 4, 0);
				this.cbMaxAdverseExcursion.ItemsSource = NinjaTrader.NinjaScript.Indicators.DimDim.DDQuantZone.strAdverseExcursionArr;
				this.gridContent.Children.Add(this.cbMaxAdverseExcursion);
				this.nudAdverseExcursion = this.CreateNumericUpDown(thickness2, textExecutionSize, 1, 3, 0);
				this.gridContent.Children.Add(this.nudAdverseExcursion);
				this.nudMaxAdverseExcursion = this.CreateNumericUpDown(thickness2, textExecutionSize, 1, 4, 0);
				this.gridContent.Children.Add(this.nudMaxAdverseExcursion);
				Label label5 = this.CreateLabel(mmAdverseExcursion.Bullish.ToString(), new global::System.Windows.Media.SolidColorBrush(global::System.Windows.Media.Color.FromRgb(64, 63, 69)), thickness2, textSettingSize, 2, 3, 0);
				label5.HorizontalContentAlignment = global::System.Windows.HorizontalAlignment.Center;
				label5.VerticalContentAlignment = global::System.Windows.VerticalAlignment.Center;
				label5.DataContext = mmAdverseExcursion;
				label5.SetBinding(ContentControl.ContentProperty, new Binding("Bullish")
				{
					Mode = BindingMode.TwoWay
				});
				this.gridContent.Children.Add(label5);
				Label label6 = this.CreateLabel(mmMaxAdverseExcursion.Bullish.ToString(), new global::System.Windows.Media.SolidColorBrush(global::System.Windows.Media.Color.FromRgb(64, 63, 69)), thickness2, textSettingSize, 2, 4, 0);
				label6.HorizontalContentAlignment = global::System.Windows.HorizontalAlignment.Center;
				label6.VerticalContentAlignment = global::System.Windows.VerticalAlignment.Center;
				label6.SetBinding(ContentControl.ContentProperty, new Binding("Bullish")
				{
					Mode = BindingMode.TwoWay
				});
				label6.DataContext = mmMaxAdverseExcursion;
				this.gridContent.Children.Add(label6);
				this.gridContent.RowDefinitions.Add(new RowDefinition());
				this.gridContent.RowDefinitions.Add(new RowDefinition());
				this.lbMaxFavorableExcursion = this.CreateLabel("MFE", global::System.Windows.Media.Brushes.DimGray, thickness2, textSettingSize, 0, 5, 0);
				this.gridContent.Children.Add(this.lbMaxFavorableExcursion);
				this.lbFavorableExcursion = this.CreateLabel("FE", global::System.Windows.Media.Brushes.DimGray, thickness2, textSettingSize, 0, 6, 0);
				this.gridContent.Children.Add(this.lbFavorableExcursion);
				this.cbMaxFavorableExcursion = this.CreateComboBox(textSettingSize, 1, 5, 0);
				this.cbMaxFavorableExcursion.ItemsSource = NinjaTrader.NinjaScript.Indicators.DimDim.DDQuantZone.strFavorableExcursionArr;
				this.gridContent.Children.Add(this.cbMaxFavorableExcursion);
				this.cbFavorableExcursion = this.CreateComboBox(textSettingSize, 1, 6, 0);
				this.cbFavorableExcursion.ItemsSource = NinjaTrader.NinjaScript.Indicators.DimDim.DDQuantZone.strFavorableExcursionArr;
				this.gridContent.Children.Add(this.cbFavorableExcursion);
				this.nudMaxFavorableExcursion = this.CreateNumericUpDown(thickness2, textExecutionSize, 1, 5, 0);
				this.gridContent.Children.Add(this.nudMaxFavorableExcursion);
				this.nudFavorableExcursion = this.CreateNumericUpDown(thickness2, textExecutionSize, 1, 6, 0);
				this.gridContent.Children.Add(this.nudFavorableExcursion);
				Label label7 = this.CreateLabel(mmMaxFavorableExcursion.Bullish.ToString(), new global::System.Windows.Media.SolidColorBrush(global::System.Windows.Media.Color.FromRgb(64, 63, 69)), thickness2, textSettingSize, 0, 0, 0);
				label7.HorizontalContentAlignment = global::System.Windows.HorizontalAlignment.Center;
				label7.VerticalContentAlignment = global::System.Windows.VerticalAlignment.Center;
				label7.DataContext = mmMaxFavorableExcursion;
				label7.SetBinding(ContentControl.ContentProperty, new Binding("Bullish")
				{
					Mode = BindingMode.TwoWay
				});
				label7.SetValue(Grid.RowProperty, 5);
				label7.SetValue(Grid.ColumnProperty, 2);
				this.gridContent.Children.Add(label7);
				Label label8 = this.CreateLabel(mmFavorableExcursion.Bullish.ToString(), new global::System.Windows.Media.SolidColorBrush(global::System.Windows.Media.Color.FromRgb(64, 63, 69)), thickness2, textSettingSize, 0, 0, 0);
				label8.HorizontalContentAlignment = global::System.Windows.HorizontalAlignment.Center;
				label8.VerticalContentAlignment = global::System.Windows.VerticalAlignment.Center;
				label8.DataContext = mmFavorableExcursion;
				label8.SetBinding(ContentControl.ContentProperty, new Binding("Bullish")
				{
					Mode = BindingMode.TwoWay
				});
				label8.SetValue(Grid.RowProperty, 6);
				label8.SetValue(Grid.ColumnProperty, 2);
				this.gridContent.Children.Add(label8);
				this.gridContent.ColumnDefinitions.Add(new ColumnDefinition
				{
					Width = default(GridLength)
				});
				this.gridContent.ColumnDefinitions.Add(new ColumnDefinition
				{
					Width = new GridLength((double)minBtnWidth)
				});
				this.gridContent.ColumnDefinitions.Add(new ColumnDefinition
				{
					Width = new GridLength((double)minBtnWidth)
				});
				this.gridContent.SetValue(Grid.ColumnProperty, 2);
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
				this.SetState(minimized);
			}
			public void SetState(bool minimized)
			{
				if (minimized)
				{
					this.gridContent.Visibility = Visibility.Collapsed;
					this.btnMini.Visibility = Visibility.Visible;
				}
				else
				{
					this.gridContent.Visibility = Visibility.Visible;
					this.btnMini.Visibility = Visibility.Collapsed;
				}
				base.ColumnDefinitions[1].Width = new GridLength(minimized ? this.btnMini.Width : 0.0);
				this.drag.ToolTip = "  Drag to anywhere; double click to " + (minimized ? "restore" : "minimize") + ".";
			}
			public void SetBackgroundLabel(global::System.Windows.Media.Brush brush)
			{
				Control control = this.lbAdverseExcursion;
				this.lbMaxAdverseExcursion.Background = brush;
				control.Background = brush;
				Control control2 = this.lbMaxFavorableExcursion;
				this.lbFavorableExcursion.Background = brush;
				control2.Background = brush;
			}
			private Label CreateLabel(string content, global::System.Windows.Media.Brush background, Thickness margin, double textSettingSize, int column = 0, int row = 0, int columnSpan = 0)
			{
				Label label = new Label
				{
					Content = content,
					Background = background,
					Foreground = global::System.Windows.Media.Brushes.White,
					HorizontalAlignment = global::System.Windows.HorizontalAlignment.Stretch,
					VerticalAlignment = global::System.Windows.VerticalAlignment.Stretch,
					Margin = margin,
					FontSize = textSettingSize,
					ToolTip = null
				};
				if (column > 0)
				{
					label.SetValue(Grid.ColumnProperty, column);
				}
				if (row > 0)
				{
					label.SetValue(Grid.RowProperty, row);
				}
				if (columnSpan > 0)
				{
					label.SetValue(Grid.ColumnSpanProperty, columnSpan);
				}
				return label;
			}
			private DDNumericUpDown CreateNumericUpDown(Thickness margin, double textExecutionSize, int column = 0, int row = 0, int columnSpan = 0)
			{
				DDNumericUpDown nud = new DDNumericUpDown
				{
					Background = global::System.Windows.Media.Brushes.Gray,
					Foreground = global::System.Windows.Media.Brushes.White,
					BorderBrush = global::System.Windows.Media.Brushes.Gray,
					HorizontalAlignment = global::System.Windows.HorizontalAlignment.Stretch,
					VerticalAlignment = global::System.Windows.VerticalAlignment.Stretch,
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
			private ComboBox CreateComboBox(double textSettingSize, int column = 0, int row = 0, int columnSpan = 0)
			{
				ComboBox comboBox = new ComboBox
				{
					Background = global::System.Windows.Media.Brushes.Gray,
					BorderBrush = global::System.Windows.Media.Brushes.Gray,
					Foreground = global::System.Windows.Media.Brushes.White,
					VerticalContentAlignment = global::System.Windows.VerticalAlignment.Center,
					HorizontalAlignment = global::System.Windows.HorizontalAlignment.Stretch,
					VerticalAlignment = global::System.Windows.VerticalAlignment.Stretch,
					FontSize = textSettingSize,
					Margin = new Thickness(1.0, 1.0, 0.0, 0.0),
					ToolTip = string.Empty,
					Focusable = false,
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
			private Button CreateButton(string text, string tag, double textSize, global::System.Windows.Media.Brush backgroundBrush, global::System.Windows.Media.Brush foregroundBrush, int buttonWidth, string toolTip = null, int column = 0, int columnSpan = 0, int row = 0)
			{
				Button button = new Button
				{
					Content = text,
					MinWidth = 0.0,
					Foreground = foregroundBrush,
					Background = backgroundBrush,
					BorderBrush = backgroundBrush,
					HorizontalAlignment = global::System.Windows.HorizontalAlignment.Left,
					FontSize = textSize,
					Margin = new Thickness(1.0, 0.0, 0.0, 0.0),
					Cursor = Cursors.Hand,
					Tag = tag,
					ToolTip = toolTip,
					Focusable = false
				};
				if (buttonWidth > 0)
				{
					global::System.Windows.Size size = DDResources_GlobalConstantAndFunction.ComputeControlTextSize(button, "");
					button.Width = (double)Math.Max(Convert.ToInt32(size.Width + 3.0 * button.Padding.Left), buttonWidth);
					button.Height = (double)Convert.ToInt32(size.Height + 3.0 * button.Padding.Top);
				}
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
				return button;
			}
			private void SetPosition(Thickness thickness)
			{
				if (this.alignment == NinjaTrader.NinjaScript.DrawingTools.TextPosition.Center)
				{
					base.HorizontalAlignment = global::System.Windows.HorizontalAlignment.Center;
					base.VerticalAlignment = global::System.Windows.VerticalAlignment.Center;
					return;
				}
				if (this.alignment != NinjaTrader.NinjaScript.DrawingTools.TextPosition.TopLeft && this.alignment != NinjaTrader.NinjaScript.DrawingTools.TextPosition.BottomLeft)
				{
					base.HorizontalAlignment = global::System.Windows.HorizontalAlignment.Right;
				}
				else
				{
					base.HorizontalAlignment = global::System.Windows.HorizontalAlignment.Left;
				}
				if (this.alignment != NinjaTrader.NinjaScript.DrawingTools.TextPosition.TopLeft)
				{
					if (this.alignment != NinjaTrader.NinjaScript.DrawingTools.TextPosition.TopRight)
					{
						base.VerticalAlignment = global::System.Windows.VerticalAlignment.Bottom;
						goto IL_005D;
					}
				}
				base.VerticalAlignment = global::System.Windows.VerticalAlignment.Top;
				IL_005D:
				base.Margin = thickness;
			}
			public Grid gridContent;
			public Button btnMini;
			public Button btnTitle;
			public Button btnInfo;
			public Thumb drag;
			public ComboBox cbMode;
			public Label lbAdverseExcursion;
			public Label lbMaxAdverseExcursion;
			public Label lbMaxFavorableExcursion;
			public Label lbFavorableExcursion;
			public DDNumericUpDown nudAdverseExcursion;
			public DDNumericUpDown nudMaxAdverseExcursion;
			public DDNumericUpDown nudMaxFavorableExcursion;
			public DDNumericUpDown nudFavorableExcursion;
			public ComboBox cbAdverseExcursion;
			public ComboBox cbMaxAdverseExcursion;
			public ComboBox cbMaxFavorableExcursion;
			public ComboBox cbFavorableExcursion;
			public NinjaTrader.NinjaScript.DrawingTools.TextPosition alignment;
		}
		private class ParamInfo
		{
			public string Name { get; set; }
			public string Value { get; set; }
		}
		private struct PropertyItemInfo
		{
			public string DisplayName { get; set; }
			public string Name { get; set; }
			public Control Control { get; set; }
			public Control OperatorControl { get; set; }
			public Type DataType { get; set; }
		}
		private class IndicatorItem : Grid
		{
			public string IndicatorName
			{
				get
				{
					return this.tblIndicatorName.Text;
				}
				set
				{
					this.tblIndicatorName.Text = value;
				}
			}
			public global::System.Windows.Media.Brush Foreground
			{
				get
				{
					return this.tblIndicatorName.Foreground;
				}
				set
				{
					if (value != null)
					{
						this.tblIndicatorName.Foreground = value;
					}
				}
			}
			public FontWeight FontWeight
			{
				set
				{
					this.tblIndicatorName.FontWeight = value;
				}
			}
			public IndicatorItem(string indicatorName, global::System.Windows.Media.Brush foreground)
			{
				this.tblIndicatorName = new TextBlock
				{
					VerticalAlignment = global::System.Windows.VerticalAlignment.Center,
					TextTrimming = TextTrimming.CharacterEllipsis
				};
				this.IndicatorName = indicatorName;
				base.Margin = new Thickness(-5.0, 0.0, -5.0, 0.0);
				this.Foreground = foreground;
				base.Children.Add(this.tblIndicatorName);
			}
			private TextBlock tblIndicatorName;
		}
		private class IndicatorsListPage : UserControl
		{
			public IndicatorsListPage(SortedList<string, DDQuantZone.IndicatorItem> sortedListIndicatorItem, bool isLightTheme, global::System.Windows.Media.Brush foreground, global::System.Windows.Media.Brush borderBrush = null)
			{
				base.Foreground = foreground;
				base.BorderBrush = borderBrush;
				this.sortedListIndicatorItem = sortedListIndicatorItem;
				Label label = new Label
				{
					Content = "Indicators",
					Background = (isLightTheme ? global::System.Windows.Media.Brushes.White : global::System.Windows.Media.Brushes.Black),
					Foreground = foreground,
					HorizontalAlignment = global::System.Windows.HorizontalAlignment.Stretch,
					VerticalAlignment = global::System.Windows.VerticalAlignment.Center,
					BorderBrush = (borderBrush ?? foreground),
					BorderThickness = new Thickness(0.0, 0.0, 0.0, 1.0)
				};
				this.DDSearchBar = new DDSearchBar(foreground, null, 0, 0, "Start typing to search")
				{
					Margin = new Thickness(5.0)
				};
				this.DDSearchBar.SetValue(Grid.RowProperty, 1);
				this.DDSearchBar.FontSize = 12.0;
				this.lbxIndicatorMain = new ListBox
				{
					Foreground = foreground,
					Margin = new Thickness(2.0),
					BorderThickness = new Thickness(0.0)
				};
				this.lbxIndicatorMain.SetValue(ScrollViewer.HorizontalScrollBarVisibilityProperty, ScrollBarVisibility.Disabled);
				this.lbxIndicatorMain.SetValue(Control.HorizontalContentAlignmentProperty, global::System.Windows.HorizontalAlignment.Stretch);
				this.lbxIndicatorMain.SetValue(Grid.RowProperty, 2);
				ScrollViewer.SetVerticalScrollBarVisibility(this.lbxIndicatorMain, ScrollBarVisibility.Auto);
				foreach (string text in this.sortedListIndicatorItem.Keys)
				{
					this.lbxIndicatorMain.Items.Add(this.sortedListIndicatorItem[text]);
				}
				this.lbxIndicatorResult = new ListBox
				{
					Foreground = foreground,
					Margin = new Thickness(2.0),
					BorderThickness = new Thickness(0.0)
				};
				this.lbxIndicatorResult.SetValue(ScrollViewer.HorizontalScrollBarVisibilityProperty, ScrollBarVisibility.Disabled);
				this.lbxIndicatorResult.SetValue(Control.HorizontalContentAlignmentProperty, global::System.Windows.HorizontalAlignment.Stretch);
				this.lbxIndicatorResult.SetValue(Grid.RowProperty, 2);
				ScrollViewer.SetVerticalScrollBarVisibility(this.lbxIndicatorResult, ScrollBarVisibility.Auto);
				this.lbxIndicatorResult.Visibility = Visibility.Hidden;
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
							Height = default(GridLength)
						},
						new RowDefinition
						{
							Height = default(GridLength)
						},
						new RowDefinition
						{
							Height = new GridLength(1.0, GridUnitType.Star)
						}
					},
					Children = { label, this.DDSearchBar, this.lbxIndicatorMain, this.lbxIndicatorResult }
				};
				base.BorderBrush = borderBrush ?? foreground;
				base.BorderThickness = new Thickness(1.0);
			}
			public void FindIndicatorByName(string searchString = null)
			{
				if (!string.IsNullOrWhiteSpace(searchString) && !string.IsNullOrEmpty(searchString))
				{
					this.lbxIndicatorResult.Items.Clear();
					this.lbxIndicatorMain.Visibility = Visibility.Hidden;
					this.lbxIndicatorResult.Visibility = Visibility.Visible;
					foreach (string text in this.sortedListIndicatorItem.Keys)
					{
						if (text.ToUpper().Contains(searchString.ToUpper()))
						{
							this.lbxIndicatorResult.Items.Add(new DDQuantZone.IndicatorItem(text, base.Foreground));
						}
					}
					return;
				}
				this.lbxIndicatorResult.Visibility = Visibility.Hidden;
				this.lbxIndicatorMain.Visibility = Visibility.Visible;
			}
			public ListBox lbxIndicatorMain;
			public ListBox lbxIndicatorResult;
			public DDSearchBar DDSearchBar;
			public Label lblAdd;
			public Label lblRemove;
			private SortedList<string, DDQuantZone.IndicatorItem> sortedListIndicatorItem;
		}
		private class IndicatorPropertiesPage : Grid
		{
			public global::System.Windows.Media.Brush Foreground { get; set; }
			public string DocumentsPath { get; set; }
			public List<DDQuantZone.PropertyItemInfo> ListProps { get; set; }
			public IndicatorBase SelectedIndicatorBase { get; private set; }
			private Dictionary<string, MethodInfo> DictMethodInfo { get; set; }
			private List<string> ListOperator { get; set; }
			public IndicatorPropertiesPage(Dictionary<string, MethodInfo> methodInfoDict, List<string> listOperator, string documentsPath, bool isLightTheme, global::System.Windows.Media.Brush foreground, global::System.Windows.Media.Brush borderBrush = null)
			{
				this.DictMethodInfo = methodInfoDict;
				this.DocumentsPath = documentsPath;
				this.ListProps = new List<DDQuantZone.PropertyItemInfo>();
				this.ListOperator = listOperator;
				Grid grid = new Grid
				{
					Background = (isLightTheme ? global::System.Windows.Media.Brushes.White : global::System.Windows.Media.Brushes.Black)
				};
				grid.ColumnDefinitions.Add(new ColumnDefinition
				{
					Width = new GridLength(3.0, GridUnitType.Star)
				});
				grid.ColumnDefinitions.Add(new ColumnDefinition
				{
					Width = new GridLength(1.0, GridUnitType.Star)
				});
				Label label = new Label
				{
					Content = "Properties",
					Background = (isLightTheme ? global::System.Windows.Media.Brushes.White : global::System.Windows.Media.Brushes.Black),
					Foreground = foreground,
					HorizontalAlignment = global::System.Windows.HorizontalAlignment.Stretch,
					VerticalAlignment = global::System.Windows.VerticalAlignment.Center
				};
				label.SetValue(Grid.ColumnProperty, 0);
				grid.Children.Add(label);
				this.checkBoxDefault = new CheckBox
				{
					Content = "Default",
					Margin = new Thickness(3.0, 3.0, 5.0, 3.0),
					HorizontalAlignment = global::System.Windows.HorizontalAlignment.Stretch,
					VerticalAlignment = global::System.Windows.VerticalAlignment.Center,
					Cursor = Cursors.Hand
				};
				this.checkBoxDefault.SetValue(Grid.ColumnProperty, 1);
				grid.Children.Add(this.checkBoxDefault);
				Border border = new Border
				{
					BorderBrush = (borderBrush ?? foreground),
					BorderThickness = new Thickness(1.0)
				};
				border.Child = grid;
				base.Children.Add(border);
				this.stackPanel = new StackPanel
				{
					Margin = new Thickness(0.0, 5.0, 0.0, 5.0)
				};
				ScrollViewer scrollViewer = new ScrollViewer
				{
					VerticalScrollBarVisibility = ScrollBarVisibility.Auto
				};
				scrollViewer.Content = this.stackPanel;
				scrollViewer.SetValue(Grid.RowProperty, 1);
				base.Children.Add(scrollViewer);
				Border border2 = new Border
				{
					BorderBrush = (borderBrush ?? foreground),
					BorderThickness = new Thickness(1.0)
				};
				border2.SetValue(Grid.RowSpanProperty, 2);
				base.RowDefinitions.Add(new RowDefinition
				{
					Height = default(GridLength)
				});
				base.RowDefinitions.Add(new RowDefinition
				{
					Height = new GridLength(1.0, GridUnitType.Star)
				});
				base.Children.Add(border2);
				this.Foreground = foreground;
			}
			public void RenderPropsPage(DDQuantZone.IndicatorItem indicatorItemSelected, string inputedIndicatorName, string inputedPropsSettings, int plotIndex, double valueBullish, double valueBearish, DDQuantZone_Operators operatorBullish, DDQuantZone_Operators operatorBearish)
			{
				try
				{
					if (this.stackPanel != null)
					{
						this.stackPanel.Children.Clear();
					}
					this.ListProps.Clear();
					string indicatorName = indicatorItemSelected.IndicatorName;
					if (this.DictMethodInfo.ContainsKey(indicatorName))
					{
						MethodInfo methodInfo = this.DictMethodInfo[indicatorName];
						if (!(methodInfo == null))
						{
							ParameterInfo[] parameters = methodInfo.GetParameters();
							if (parameters != null)
							{
								bool flag = string.IsNullOrEmpty(inputedPropsSettings) || string.IsNullOrEmpty(inputedIndicatorName);
								JavaScriptSerializer javaScriptSerializer = new JavaScriptSerializer();
								List<DDQuantZone.ParamInfo> list = (flag ? null : javaScriptSerializer.Deserialize<List<DDQuantZone.ParamInfo>>(inputedPropsSettings));
								if (list != null && list.Count != parameters.Length)
								{
									list = null;
								}
								bool flag2 = list != null && indicatorName == inputedIndicatorName;
								if (!(methodInfo.ReturnType == null))
								{
									string fullName = methodInfo.ReturnType.FullName;
									if (!string.IsNullOrWhiteSpace(fullName))
									{
										Type type = null;
										foreach (string text in Directory.GetFiles(this.DocumentsPath, "*.dll", SearchOption.TopDirectoryOnly))
										{
											try
											{
												Assembly assembly = Assembly.LoadFrom(text);
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
										this.SelectedIndicatorBase = ((type != null) ? ((IndicatorBase)Activator.CreateInstance(type)) : null);
										if (this.SelectedIndicatorBase != null)
										{
											Type type2 = this.SelectedIndicatorBase.GetType();
											if (!(type2 == null))
											{
												PropertyInfo[] properties = type2.GetProperties();
												if (properties != null)
												{
													indicatorItemSelected.FontWeight = (flag2 ? FontWeights.DemiBold : FontWeights.Normal);
													for (int j = 0; j < parameters.Length; j++)
													{
														try
														{
															ParameterInfo parameterInfo = parameters[j];
															if (parameterInfo != null)
															{
																string text2 = parameterInfo.Name.ToLower();
																foreach (PropertyInfo propertyInfo in properties)
																{
																	try
																	{
																		if (!(propertyInfo == null))
																		{
																			string name = propertyInfo.Name;
																			if (name.ToLower() == text2)
																			{
																				Grid grid = new Grid
																				{
																					Margin = new Thickness(10.0, 0.0, 5.0, 3.0)
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
																					Foreground = this.Foreground,
																					VerticalAlignment = global::System.Windows.VerticalAlignment.Center,
																					HorizontalAlignment = global::System.Windows.HorizontalAlignment.Left,
																					TextTrimming = TextTrimming.CharacterEllipsis,
																					Margin = new Thickness(0.0, 0.0, 10.0, 0.0)
																				};
																				textBlock.SetValue(Grid.ColumnProperty, 0);
																				string text3 = name;
																				foreach (Attribute attribute in propertyInfo.GetCustomAttributes())
																				{
																					if (attribute != null && attribute is DisplayAttribute)
																					{
																						DisplayAttribute displayAttribute = attribute as DisplayAttribute;
																						if (displayAttribute != null)
																						{
																							text3 = displayAttribute.Name;
																							break;
																						}
																					}
																				}
																				textBlock.Text = text3;
																				grid.Children.Add(textBlock);
																				Type parameterType = parameterInfo.ParameterType;
																				if (!(parameterType == null))
																				{
																					Control control;
																					if (!parameterType.IsEnum && !(parameterType == typeof(ISeries<double>)))
																					{
																						if (parameterType == typeof(bool))
																						{
																							CheckBox checkBox = new CheckBox
																							{
																								Foreground = this.Foreground
																							};
																							checkBox.SetValue(Grid.ColumnProperty, 1);
																							grid.Children.Add(checkBox);
																							checkBox.IsChecked = new bool?(flag2 ? bool.Parse(list[j].Value) : ((bool)propertyInfo.GetValue(this.SelectedIndicatorBase)));
																							control = checkBox;
																						}
																						else
																						{
																							TextBox tbPropValue = new TextBox
																							{
																								Text = (flag2 ? list[j].Value : propertyInfo.GetValue(this.SelectedIndicatorBase).ToString()),
																								VerticalContentAlignment = global::System.Windows.VerticalAlignment.Center,
																								Foreground = this.Foreground,
																								MinHeight = 20.0
																							};
																							string oldText = null;
																							bool conditionIncludeDot = parameterType == typeof(double) || parameterType == typeof(float) || parameterType == typeof(long);
																							Action textChangedAction = null;
																							tbPropValue.TextChanged += delegate(object sender, TextChangedEventArgs e)
																							{
																								Dispatcher dispatcher = tbPropValue.Dispatcher;
																								Action action;
																								if ((action = textChangedAction) == null)
																								{
																									action = (textChangedAction = delegate
																									{
																										NinjaTrader.NinjaScript.Indicators.DimDim.DDQuantZone.NumericTextBox_TextChangedHandler(tbPropValue, ref oldText, true, conditionIncludeDot);
																									});
																								}
																								dispatcher.InvokeAsync(action);
																							};
																							Action previewKeyDownAction = null;
																							tbPropValue.PreviewKeyDown += delegate(object sender, KeyEventArgs e)
																							{
																								Dispatcher dispatcher2 = tbPropValue.Dispatcher;
																								Action action2;
																								if ((action2 = previewKeyDownAction) == null)
																								{
																									action2 = (previewKeyDownAction = delegate
																									{
																										NinjaTrader.NinjaScript.Indicators.DimDim.DDQuantZone.NumericTextBox_PreviewKeyDownHandler(tbPropValue, ref oldText);
																									});
																								}
																								dispatcher2.InvokeAsync(action2);
																							};
																							Action lostFocusAction = null;
																							tbPropValue.LostFocus += delegate(object sender, RoutedEventArgs e)
																							{
																								Dispatcher dispatcher3 = tbPropValue.Dispatcher;
																								Action action3;
																								if ((action3 = lostFocusAction) == null)
																								{
																									action3 = (lostFocusAction = delegate
																									{
																										NinjaTrader.NinjaScript.Indicators.DimDim.DDQuantZone.NumericTextBox_LostFocusHandler(tbPropValue, 0);
																									});
																								}
																								dispatcher3.InvokeAsync(action3);
																							};
																							tbPropValue.SetValue(Grid.ColumnProperty, 1);
																							grid.Children.Add(tbPropValue);
																							control = tbPropValue;
																						}
																					}
																					else
																					{
																						ComboBox comboBox = new ComboBox
																						{
																							Foreground = this.Foreground,
																							MinHeight = 20.0,
																							VerticalContentAlignment = global::System.Windows.VerticalAlignment.Center,
																							ToolTip = string.Empty
																						};
																						bool flag3 = parameterType == typeof(ISeries<double>);
																						comboBox.ItemsSource = Enum.GetValues(flag3 ? typeof(PriceType) : parameterType);
																						int num;
																						if (flag2)
																						{
																							if (list[j] == null)
																							{
																								goto IL_06E1;
																							}
																							num = this.GetEnumIndex(parameterType, list[j].Value);
																						}
																						else
																						{
																							num = this.GetEnumIndex(parameterType, (flag3 ? 0 : ((int)propertyInfo.GetValue(this.SelectedIndicatorBase))).ToString());
																						}
																						if (num >= 0)
																						{
																							comboBox.SelectedIndex = num;
																						}
																						comboBox.SetValue(Grid.ColumnProperty, 1);
																						grid.Children.Add(comboBox);
																						control = comboBox;
																					}
																					this.stackPanel.Children.Add(grid);
																					this.ListProps.Add(new DDQuantZone.PropertyItemInfo
																					{
																						DisplayName = text3,
																						Name = name,
																						DataType = parameterType,
																						Control = control
																					});
																					break;
																				}
																			}
																		}
																	}
																	catch
																	{
																	}
																	IL_06E1:;
																}
															}
														}
														catch
														{
														}
													}
													Grid grid2 = new Grid
													{
														Margin = new Thickness(10.0, 0.0, 5.0, 0.0)
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
														Text = "Plot",
														Foreground = this.Foreground,
														VerticalAlignment = global::System.Windows.VerticalAlignment.Center,
														HorizontalAlignment = global::System.Windows.HorizontalAlignment.Left,
														TextTrimming = TextTrimming.CharacterEllipsis
													};
													textBlock2.SetValue(Grid.ColumnProperty, 0);
													NinjaTrader.Gui.Plot[] plots = this.SelectedIndicatorBase.Plots;
													if (plots != null && plots.Length != 0)
													{
														int num2 = plots.Length;
														string[] array2 = new string[num2];
														for (int k = 0; k < num2; k++)
														{
															array2[k] = plots[k].Name;
														}
														ComboBox comboBox2 = new ComboBox
														{
															Foreground = this.Foreground,
															MinHeight = 20.0,
															VerticalContentAlignment = global::System.Windows.VerticalAlignment.Center,
															ToolTip = string.Empty,
															ItemsSource = array2,
															SelectedIndex = (flag2 ? plotIndex : 0)
														};
														comboBox2.SetValue(Grid.ColumnProperty, 1);
														grid2.Children.Add(textBlock2);
														grid2.Children.Add(comboBox2);
														this.ListProps.Add(new DDQuantZone.PropertyItemInfo
														{
															DisplayName = textBlock2.Text,
															Name = "Plots",
															DataType = typeof(byte),
															Control = comboBox2
														});
														this.stackPanel.Children.Add(grid2);
														Grid grid3 = this.CreateConditionGridControl(null, flag2 ? valueBullish : 1.0, "Value: Bullish", (int)(flag2 ? operatorBullish : DDQuantZone_Operators.Equal));
														this.stackPanel.Children.Add(grid3);
														Grid grid4 = this.CreateConditionGridControl(grid3, flag2 ? valueBearish : (-1.0), "Value: Bearish", (int)(flag2 ? operatorBearish : DDQuantZone_Operators.Equal));
														if (num2 == 0)
														{
															UIElement uielement = grid2;
															UIElement uielement2 = grid3;
															grid4.Visibility = Visibility.Collapsed;
															uielement2.Visibility = Visibility.Collapsed;
															uielement.Visibility = Visibility.Collapsed;
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
			public void RenderPropsPageDefault(int fastMATypeSelected, int fastMAPeriod, int slowMATypeSelected, int slowMAPeriod)
			{
				try
				{
					if (this.stackPanel != null)
					{
						this.stackPanel.Children.Clear();
					}
					this.ListProps.Clear();
					Grid grid = this.CreateGridPropItem("Fast: MA Type", fastMATypeSelected, true);
					Grid grid2 = this.CreateGridPropItem("Fast: Period", fastMAPeriod, false);
					Grid grid3 = this.CreateGridPropItem("Slow: MA Type", slowMATypeSelected, true);
					Grid grid4 = this.CreateGridPropItem("Slow: Period", slowMAPeriod, false);
					this.stackPanel.Children.Add(grid);
					this.stackPanel.Children.Add(grid2);
					this.stackPanel.Children.Add(grid3);
					this.stackPanel.Children.Add(grid4);
				}
				catch
				{
				}
			}
			private Grid CreateGridPropItem(string text, int value, bool isComboBox)
			{
				Grid grid = new Grid
				{
					Margin = new Thickness(10.0, 0.0, 5.0, 3.0)
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
					Text = text,
					Foreground = this.Foreground,
					VerticalAlignment = global::System.Windows.VerticalAlignment.Center,
					HorizontalAlignment = global::System.Windows.HorizontalAlignment.Left,
					TextTrimming = TextTrimming.CharacterEllipsis,
					Margin = new Thickness(0.0, 0.0, 10.0, 0.0)
				};
				textBlock.SetValue(Grid.ColumnProperty, 0);
				grid.Children.Add(textBlock);
				if (isComboBox)
				{
					ComboBox comboBox = new ComboBox
					{
						Foreground = this.Foreground,
						MinHeight = 20.0,
						VerticalContentAlignment = global::System.Windows.VerticalAlignment.Center,
						ItemsSource = Enum.GetValues(typeof(DD_MAType)),
						SelectedIndex = value,
						ToolTip = string.Empty
					};
					comboBox.SetValue(Grid.ColumnProperty, 1);
					grid.Children.Add(comboBox);
					this.ListProps.Add(new DDQuantZone.PropertyItemInfo
					{
						DisplayName = text,
						Name = string.Empty,
						DataType = typeof(Enum),
						Control = comboBox
					});
				}
				else
				{
					TextBox tbPropValue = new TextBox
					{
						Text = value.ToString(),
						VerticalContentAlignment = global::System.Windows.VerticalAlignment.Center,
						Foreground = this.Foreground,
						MinHeight = 20.0
					};
					string oldText = null;
					Action textChangedAction = null;
					tbPropValue.TextChanged += delegate(object sender, TextChangedEventArgs e)
					{
						Dispatcher dispatcher = tbPropValue.Dispatcher;
						Action action;
						if ((action = textChangedAction) == null)
						{
							action = (textChangedAction = delegate
							{
								NinjaTrader.NinjaScript.Indicators.DimDim.DDQuantZone.NumericTextBox_TextChangedHandler(tbPropValue, ref oldText, true, false);
							});
						}
						dispatcher.InvokeAsync(action);
					};
					Action previewKeyDownAction = null;
					tbPropValue.PreviewKeyDown += delegate(object sender, KeyEventArgs e)
					{
						Dispatcher dispatcher2 = tbPropValue.Dispatcher;
						Action action2;
						if ((action2 = previewKeyDownAction) == null)
						{
							action2 = (previewKeyDownAction = delegate
							{
								NinjaTrader.NinjaScript.Indicators.DimDim.DDQuantZone.NumericTextBox_PreviewKeyDownHandler(tbPropValue, ref oldText);
							});
						}
						dispatcher2.InvokeAsync(action2);
					};
					Action lostFocusAction = null;
					tbPropValue.LostFocus += delegate(object sender, RoutedEventArgs e)
					{
						Dispatcher dispatcher3 = tbPropValue.Dispatcher;
						Action action3;
						if ((action3 = lostFocusAction) == null)
						{
							action3 = (lostFocusAction = delegate
							{
								NinjaTrader.NinjaScript.Indicators.DimDim.DDQuantZone.NumericTextBox_LostFocusHandler(tbPropValue, 0);
							});
						}
						dispatcher3.InvokeAsync(action3);
					};
					tbPropValue.SetValue(Grid.ColumnProperty, 1);
					grid.Children.Add(tbPropValue);
					this.ListProps.Add(new DDQuantZone.PropertyItemInfo
					{
						DisplayName = text,
						Name = string.Empty,
						DataType = typeof(int),
						Control = tbPropValue
					});
				}
				return grid;
			}
			private Grid CreateConditionGridControl(Grid grid, double value, string textName, int indexOperator)
			{
				Grid grid2;
				if (grid == null)
				{
					this.rowIndex = 0;
					grid2 = new Grid
					{
						Margin = new Thickness(10.0, 3.0, 5.0, 0.0)
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
					Text = textName,
					Foreground = this.Foreground,
					VerticalAlignment = global::System.Windows.VerticalAlignment.Center,
					HorizontalAlignment = global::System.Windows.HorizontalAlignment.Left,
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
					Foreground = this.Foreground,
					FontWeight = FontWeights.Bold,
					VerticalContentAlignment = global::System.Windows.VerticalAlignment.Center,
					ToolTip = string.Empty,
					ItemsSource = this.ListOperator,
					SelectedIndex = indexOperator,
					MinWidth = 40.0,
					MinHeight = 20.0,
					Margin = new Thickness(0.0, (double)((this.rowIndex == 0) ? 0 : 3), 3.0, 0.0)
				};
				comboBox.SetValue(Grid.ColumnProperty, 0);
				TextBox tbPropertyValue = new TextBox
				{
					Text = value.ToString(),
					VerticalContentAlignment = global::System.Windows.VerticalAlignment.Center,
					Foreground = this.Foreground,
					MinWidth = 0.0,
					MinHeight = 20.0,
					Margin = thickness
				};
				tbPropertyValue.SetValue(Grid.ColumnProperty, 1);
				string oldValue = null;
				Action textChangedAction = null;
				tbPropertyValue.TextChanged += delegate(object sender, TextChangedEventArgs e)
				{
					Dispatcher dispatcher = tbPropertyValue.Dispatcher;
					Action action;
					if ((action = textChangedAction) == null)
					{
						action = (textChangedAction = delegate
						{
							NinjaTrader.NinjaScript.Indicators.DimDim.DDQuantZone.NumericTextBox_TextChangedHandler(tbPropertyValue, ref oldValue, true, true);
						});
					}
					dispatcher.InvokeAsync(action);
				};
				Action previewKeyDownAction = null;
				tbPropertyValue.PreviewKeyDown += delegate(object sender, KeyEventArgs e)
				{
					Dispatcher dispatcher2 = tbPropertyValue.Dispatcher;
					Action action2;
					if ((action2 = previewKeyDownAction) == null)
					{
						action2 = (previewKeyDownAction = delegate
						{
							NinjaTrader.NinjaScript.Indicators.DimDim.DDQuantZone.NumericTextBox_PreviewKeyDownHandler(tbPropertyValue, ref oldValue);
						});
					}
					dispatcher2.InvokeAsync(action2);
				};
				Action lostFocusAction = null;
				tbPropertyValue.LostFocus += delegate(object sender, RoutedEventArgs e)
				{
					Dispatcher dispatcher3 = tbPropertyValue.Dispatcher;
					Action action3;
					if ((action3 = lostFocusAction) == null)
					{
						action3 = (lostFocusAction = delegate
						{
							NinjaTrader.NinjaScript.Indicators.DimDim.DDQuantZone.NumericTextBox_LostFocusHandler(tbPropertyValue, 0);
						});
					}
					dispatcher3.InvokeAsync(action3);
				};
				grid3.Children.Add(comboBox);
				grid3.Children.Add(tbPropertyValue);
				grid2.Children.Add(textBlock);
				grid2.Children.Add(grid3);
				this.ListProps.Add(new DDQuantZone.PropertyItemInfo
				{
					DisplayName = textBlock.Text,
					Name = textName,
					DataType = typeof(double),
					Control = tbPropertyValue,
					OperatorControl = comboBox
				});
				return grid2;
			}
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
			public StackPanel stackPanel;
			public CheckBox checkBoxDefault;
			private int rowIndex;
		}
		public class PlotOrDataSeriesInfo
		{
			public string PlotName { get; set; }
			public Series<double> PlotOrDataSeries { get; set; }
			public PlotOrDataSeriesInfo(string plotName, Series<double> plotOrDataSeries)
			{
				this.PlotName = plotName;
				this.PlotOrDataSeries = plotOrDataSeries;
			}
		}
		private class InfoTab : Grid
		{
			internal InfoTab(List<DDQuantZone.SampleInfo> listSampleInfo, List<DDQuantZone.MarketMetric> listMarketMetricItem, bool isLightTheme, global::System.Windows.Media.Brush foreground, global::System.Windows.Media.Brush borderBrush = null)
			{
				DataGrid dataGrid = new DataGrid
				{
					ItemsSource = listSampleInfo,
					AutoGenerateColumns = false,
					HeadersVisibility = DataGridHeadersVisibility.Column,
					GridLinesVisibility = DataGridGridLinesVisibility.All,
					Foreground = foreground,
					Background = global::System.Windows.Media.Brushes.Transparent,
					RowBackground = global::System.Windows.Media.Brushes.Transparent,
					AlternatingRowBackground = global::System.Windows.Media.Brushes.Transparent,
					BorderBrush = global::System.Windows.Media.Brushes.Gray,
					BorderThickness = new Thickness(1.0),
					HorizontalAlignment = global::System.Windows.HorizontalAlignment.Stretch,
					VerticalAlignment = global::System.Windows.VerticalAlignment.Stretch,
					FontSize = 13.0,
					CanUserAddRows = false,
					CanUserResizeRows = false,
					CanUserReorderColumns = false,
					CanUserSortColumns = false,
					IsReadOnly = true,
					SelectionMode = DataGridSelectionMode.Single,
					SelectionUnit = DataGridSelectionUnit.FullRow
				};
				dataGrid.SetValue(Grid.ColumnProperty, 0);
				global::System.Windows.Style style = new global::System.Windows.Style(typeof(DataGridCell));
				style.Setters.Add(new Setter(Control.BorderBrushProperty, global::System.Windows.Media.Brushes.Transparent));
				style.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(0.0)));
				style.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(4.0)));
				style.Setters.Add(new Setter(TextBlock.TextAlignmentProperty, global::System.Windows.TextAlignment.Center));
				dataGrid.GridLinesVisibility = DataGridGridLinesVisibility.None;
				dataGrid.Columns.Add(new DataGridTextColumn
				{
					Header = "Sample Range",
					Binding = new Binding("SampleRange"),
					Width = new DataGridLength(110.0),
					CellStyle = style
				});
				dataGrid.Columns.Add(new DataGridTextColumn
				{
					Header = "Sample (+)",
					Binding = new Binding("SampleIncrease"),
					Width = new DataGridLength(85.0),
					CellStyle = style
				});
				dataGrid.Columns.Add(new DataGridTextColumn
				{
					Header = "Probability (+)",
					Binding = new Binding("ProbabilityIncreases"),
					Width = new DataGridLength(120.0),
					CellStyle = style
				});
				dataGrid.Columns.Add(new DataGridTextColumn
				{
					Header = "Sample (−)",
					Binding = new Binding("SampleDecreases"),
					Width = new DataGridLength(85.0),
					CellStyle = style
				});
				dataGrid.Columns.Add(new DataGridTextColumn
				{
					Header = "Probability (−)",
					Binding = new Binding("ProbabilityDecreases"),
					Width = new DataGridLength(120.0),
					CellStyle = style
				});
				dataGrid.ColumnHeaderStyle = new global::System.Windows.Style(typeof(DataGridColumnHeader))
				{
					Setters = 
					{
						new Setter(Control.BackgroundProperty, isLightTheme ? global::System.Windows.Media.Brushes.White : global::System.Windows.Media.Brushes.Black),
						new Setter(Control.ForegroundProperty, foreground),
						new Setter(Control.FontWeightProperty, FontWeights.SemiBold),
						new Setter(Control.BorderBrushProperty, new global::System.Windows.Media.SolidColorBrush(global::System.Windows.Media.Color.FromRgb(102, 102, 102))),
						new Setter(Control.BorderThicknessProperty, new Thickness(0.0, 0.0, 1.0, 1.0)),
						new Setter(Control.HorizontalContentAlignmentProperty, global::System.Windows.HorizontalAlignment.Center)
					}
				};
				GridSplitter gridSplitter = new GridSplitter
				{
					Background = global::System.Windows.Media.Brushes.Transparent,
					HorizontalAlignment = global::System.Windows.HorizontalAlignment.Stretch
				};
				gridSplitter.SetValue(Grid.ColumnProperty, 1);
				StackPanel stackPanel = new StackPanel
				{
					Orientation = global::System.Windows.Controls.Orientation.Vertical
				};
				TextBlock textBlock = new TextBlock
				{
					Text = "DETAIL",
					FontSize = 13.0,
					Foreground = foreground,
					FontWeight = FontWeights.Bold,
					HorizontalAlignment = global::System.Windows.HorizontalAlignment.Center,
					VerticalAlignment = global::System.Windows.VerticalAlignment.Center,
					Padding = new Thickness(10.0, 5.0, 10.0, 5.0)
				};
				Border border = new Border
				{
					Background = (isLightTheme ? global::System.Windows.Media.Brushes.White : global::System.Windows.Media.Brushes.Black),
					BorderBrush = (borderBrush ?? foreground),
					BorderThickness = new Thickness(0.0, 1.0, 0.0, 1.0),
					Margin = new Thickness(0.0, 0.0, 0.0, 5.0)
				};
				border.Child = textBlock;
				stackPanel.Children.Add(border);
				Grid grid = new Grid();
				grid.ColumnDefinitions.Add(new ColumnDefinition
				{
					Width = new GridLength(2.0, GridUnitType.Star)
				});
				grid.ColumnDefinitions.Add(new ColumnDefinition
				{
					Width = new GridLength(1.0, GridUnitType.Star)
				});
				grid.ColumnDefinitions.Add(new ColumnDefinition
				{
					Width = new GridLength(1.0, GridUnitType.Star)
				});
				TextBlock textBlock2 = new TextBlock
				{
					Text = "Bullish",
					Background = global::System.Windows.Media.Brushes.LimeGreen,
					Foreground = foreground,
					HorizontalAlignment = global::System.Windows.HorizontalAlignment.Center,
					VerticalAlignment = global::System.Windows.VerticalAlignment.Center,
					FontSize = 13.0,
					Margin = new Thickness(0.0, 0.0, 0.0, 10.0),
					Padding = new Thickness(10.0, 5.0, 10.0, 5.0)
				};
				textBlock2.SetValue(Grid.ColumnProperty, 1);
				TextBlock textBlock3 = new TextBlock
				{
					Text = "Bearish",
					Background = global::System.Windows.Media.Brushes.HotPink,
					Foreground = foreground,
					HorizontalAlignment = global::System.Windows.HorizontalAlignment.Center,
					VerticalAlignment = global::System.Windows.VerticalAlignment.Center,
					FontSize = 13.0,
					Margin = new Thickness(0.0, 0.0, 0.0, 10.0),
					Padding = new Thickness(7.0, 5.0, 7.0, 5.0)
				};
				textBlock3.SetValue(Grid.ColumnProperty, 2);
				grid.Children.Add(textBlock2);
				grid.Children.Add(textBlock3);
				stackPanel.Children.Add(grid);
				foreach (DDQuantZone.MarketMetric marketMetric in listMarketMetricItem)
				{
					Grid grid2 = this.CreateItemDetail(marketMetric, foreground);
					stackPanel.Children.Add(grid2);
				}
				Border border2 = new Border
				{
					BorderBrush = (borderBrush ?? foreground),
					BorderThickness = new Thickness(1.0, 0.0, 1.0, 1.0)
				};
				border2.Child = stackPanel;
				border2.SetValue(Grid.ColumnProperty, 2);
				base.ColumnDefinitions.Add(new ColumnDefinition
				{
					Width = new GridLength(4.0, GridUnitType.Star),
					MinWidth = 100.0
				});
				base.ColumnDefinitions.Add(new ColumnDefinition
				{
					Width = new GridLength(7.0)
				});
				base.ColumnDefinitions.Add(new ColumnDefinition
				{
					Width = new GridLength(2.0, GridUnitType.Star),
					MinWidth = 100.0
				});
				base.Children.Add(dataGrid);
				base.Children.Add(gridSplitter);
				base.Children.Add(border2);
			}
			private Grid CreateItemDetail(DDQuantZone.MarketMetric marketMetricItem, global::System.Windows.Media.Brush foreground)
			{
				Grid grid = new Grid();
				grid.ColumnDefinitions.Add(new ColumnDefinition
				{
					Width = new GridLength(2.0, GridUnitType.Star)
				});
				grid.ColumnDefinitions.Add(new ColumnDefinition
				{
					Width = new GridLength(1.0, GridUnitType.Star)
				});
				grid.ColumnDefinitions.Add(new ColumnDefinition
				{
					Width = new GridLength(1.0, GridUnitType.Star)
				});
				grid.DataContext = marketMetricItem;
				TextBlock textBlock = new TextBlock
				{
					Text = marketMetricItem.Label,
					Foreground = foreground,
					FontSize = 13.0,
					VerticalAlignment = global::System.Windows.VerticalAlignment.Center,
					HorizontalAlignment = global::System.Windows.HorizontalAlignment.Left,
					Margin = new Thickness(10.0, 0.0, 0.0, 10.0),
					TextTrimming = TextTrimming.CharacterEllipsis
				};
				textBlock.SetValue(Grid.ColumnProperty, 0);
				TextBlock textBlock2 = new TextBlock
				{
					Text = marketMetricItem.Bullish.ToString(),
					Foreground = foreground,
					FontSize = 13.0,
					VerticalAlignment = global::System.Windows.VerticalAlignment.Center,
					HorizontalAlignment = global::System.Windows.HorizontalAlignment.Center,
					Margin = new Thickness(10.0, 0.0, 0.0, 0.0),
					TextTrimming = TextTrimming.CharacterEllipsis
				};
				textBlock2.SetValue(Grid.ColumnProperty, 1);
				textBlock2.SetBinding(TextBlock.TextProperty, new Binding("Bullish")
				{
					Mode = BindingMode.TwoWay
				});
				TextBlock textBlock3 = new TextBlock
				{
					Text = marketMetricItem.Bearish.ToString(),
					Foreground = foreground,
					FontSize = 13.0,
					VerticalAlignment = global::System.Windows.VerticalAlignment.Center,
					HorizontalAlignment = global::System.Windows.HorizontalAlignment.Center,
					Margin = new Thickness(10.0, 0.0, 0.0, 0.0),
					TextTrimming = TextTrimming.CharacterEllipsis
				};
				textBlock3.SetValue(Grid.ColumnProperty, 2);
				textBlock3.SetBinding(TextBlock.TextProperty, new Binding("Bearish")
				{
					Mode = BindingMode.TwoWay
				});
				grid.Children.Add(textBlock);
				grid.Children.Add(textBlock2);
				grid.Children.Add(textBlock3);
				return grid;
			}
		}
		private class LineChartTab : Grid
		{
			public bool IsLightTheme { get; set; }
			public LineChartTab(bool isLightTheme)
			{
				this.IsLightTheme = isLightTheme;
				MenuItem menuItem = new MenuItem
				{
					Header = "Zoom to Fit Data"
				};
				menuItem.Click += delegate(object sender, RoutedEventArgs e)
				{
					if (this.hLineBullish != null)
					{
						this.hLineBullish.IsVisible = false;
					}
					if (this.vLineBullish != null)
					{
						this.vLineBullish.IsVisible = false;
					}
					if (this.hLineBearish != null)
					{
						this.hLineBearish.IsVisible = false;
					}
					if (this.vLineBearish != null)
					{
						this.vLineBearish.IsVisible = false;
					}
					this.lineChartObject.Plot.AxisAuto(null, null);
					this.lineChartObject.Render(false);
				};
				MenuItem menuItem2 = new MenuItem
				{
					Header = "Help"
				};
				menuItem2.Click += delegate(object sender, RoutedEventArgs e)
				{
					if (this.HelpMenuItemClicked != null)
					{
						this.HelpMenuItemClicked(this, e);
					}
				};
				this.rightClickMenu = new ContextMenu();
				this.rightClickMenu.Items.Add(menuItem);
				this.rightClickMenu.Items.Add(menuItem2);
				this.CreateNewLineChartObject();
			}
			private void CreateNewLineChartObject()
			{
				base.Children.Clear();
				this.lineChartObject = new WpfPlot();
				NinjaTrader.NinjaScript.Indicators.DimDim.DDQuantZone.SetScootPlotStyle(this.lineChartObject, this.IsLightTheme);
				global::System.Drawing.Color transparent = global::System.Drawing.Color.Transparent;
				global::System.Drawing.Color dodgerBlue = global::System.Drawing.Color.DodgerBlue;
				this.hLineBullish = this.lineChartObject.Plot.AddHorizontalLine(0.0, new global::System.Drawing.Color?(transparent), 1f, LineStyle.Solid, null);
				this.vLineBullish = this.lineChartObject.Plot.AddVerticalLine(0.0, new global::System.Drawing.Color?(transparent), 1f, LineStyle.Solid, null);
				global::ScottPlot.Plottable.AxisLine axisLine = this.hLineBullish;
				axisLine.PositionFormatter = NinjaTrader.NinjaScript.Indicators.DimDim.DDQuantZone.CustomTickFormatterXAxisDelegate;
				global::ScottPlot.Plottable.AxisLine axisLine2 = this.vLineBullish;
				axisLine2.PositionFormatter = NinjaTrader.NinjaScript.Indicators.DimDim.DDQuantZone.CustomTickFormatterXAxisDelegate;
				global::ScottPlot.Plottable.AxisLine axisLine3 = this.hLineBullish;
				this.vLineBullish.PositionLabel = true;
				axisLine3.PositionLabel = true;
				this.hLineBullish.PositionLabelBackground = dodgerBlue;
				this.vLineBullish.PositionLabelBackground = dodgerBlue;
				global::ScottPlot.Plottable.AxisLine axisLine4 = this.hLineBullish;
				this.vLineBullish.IsVisible = false;
				axisLine4.IsVisible = false;
				global::System.Drawing.Color hotPink = global::System.Drawing.Color.HotPink;
				this.hLineBearish = this.lineChartObject.Plot.AddHorizontalLine(0.0, new global::System.Drawing.Color?(transparent), 1f, LineStyle.Solid, null);
				this.vLineBearish = this.lineChartObject.Plot.AddVerticalLine(0.0, new global::System.Drawing.Color?(transparent), 1f, LineStyle.Solid, null);
				global::ScottPlot.Plottable.AxisLine axisLine5 = this.hLineBearish;
				axisLine5.PositionFormatter = NinjaTrader.NinjaScript.Indicators.DimDim.DDQuantZone.CustomTickFormatterXAxisDelegate;
				global::ScottPlot.Plottable.AxisLine axisLine6 = this.vLineBearish;
				axisLine6.PositionFormatter = NinjaTrader.NinjaScript.Indicators.DimDim.DDQuantZone.CustomTickFormatterXAxisDelegate;
				global::ScottPlot.Plottable.AxisLine axisLine7 = this.hLineBearish;
				this.vLineBearish.PositionLabel = true;
				axisLine7.PositionLabel = true;
				this.hLineBearish.PositionLabelBackground = hotPink;
				this.vLineBearish.PositionLabelBackground = hotPink;
				global::ScottPlot.Plottable.AxisLine axisLine8 = this.hLineBearish;
				this.vLineBearish.IsVisible = false;
				axisLine8.IsVisible = false;
				this.lastHighlightedIdxBullish = -1;
				this.lastHighlightedIdxBearish = -1;
				this.highlightedBullish = this.lineChartObject.Plot.AddPoint(0.0, 0.0, null, 5f, MarkerShape.filledCircle, null);
				this.highlightedBullish.MarkerShape = MarkerShape.openCircle;
				this.highlightedBullish.Color = dodgerBlue;
				this.highlightedBullish.MarkerSize = 10.0;
				this.highlightedBullish.IsVisible = false;
				this.highlightedBearish = this.lineChartObject.Plot.AddPoint(0.0, 0.0, null, 5f, MarkerShape.filledCircle, null);
				this.highlightedBearish.MarkerShape = MarkerShape.openCircle;
				this.highlightedBearish.Color = hotPink;
				this.highlightedBearish.MarkerSize = 10.0;
				this.highlightedBearish.IsVisible = false;
				this.lineChartObject.MouseMove += this.OnLineChartObjectMouseMove;
				this.lineChartObject.PreviewMouseDown += delegate(object sender, MouseButtonEventArgs e)
				{
					if (e.ClickCount > 1)
					{
						e.Handled = true;
					}
					if (e.MiddleButton == MouseButtonState.Pressed)
					{
						e.Handled = true;
						this.lineChartObject.Plot.AxisAuto(null, null);
						this.OnLineChartObjectMouseMove(sender, e);
					}
				};
				this.lineChartObject.RightClicked += delegate(object sender, EventArgs e)
				{
					this.rightClickMenu.IsOpen = true;
				};
				this.lineChartObject.Plot.XAxis.MinimumTickSpacing(1.0);
				Axis xaxis = this.lineChartObject.Plot.XAxis;
				xaxis.TickLabelFormat(NinjaTrader.NinjaScript.Indicators.DimDim.DDQuantZone.CustomTickFormatterXAxisDelegate);
				this.lineChartObject.Plot.XLabel("Sample Range");
				this.lineChartObject.Plot.YLabel("Frequency");
				base.Children.Add(this.lineChartObject);
			}
			private static double ReadTupleField(object tupleObj, string fieldName)
			{
				return (double)tupleObj.GetType().GetField(fieldName).GetValue(tupleObj);
			}
			private static int ReadTupleFieldInt(object tupleObj, string fieldName)
			{
				return (int)tupleObj.GetType().GetField(fieldName).GetValue(tupleObj);
			}
			private void OnLineChartObjectMouseMove(object sender, MouseEventArgs e)
			{
				if (this.linePlotBullish != null && this.linePlotBearish != null)
				{
					dynamic chartDyn = this.lineChartObject;
					object mouseCoords = chartDyn.GetMouseCoordinates();
					double mouseX = ReadTupleField(mouseCoords, "Item1");
					double mouseY = ReadTupleField(mouseCoords, "Item2");
					double num = this.lineChartObject.Plot.XAxis.Dims.PxPerUnit / this.lineChartObject.Plot.YAxis.Dims.PxPerUnit;
					AxisLimits axisLimits = this.lineChartObject.Plot.GetAxisLimits(0, 0);
					bool flag = mouseX > axisLimits.XMin && mouseX < axisLimits.XMax;
					bool flag2 = mouseY > axisLimits.YMin && mouseY < axisLimits.YMax;
					this.hLineBullish.IsVisible = (this.vLineBullish.IsVisible = (this.highlightedBullish.IsVisible = (this.highlightedBearish.IsVisible = flag && flag2)));
					this.hLineBearish.IsVisible = (this.vLineBearish.IsVisible = flag && flag2);
					dynamic bullishDyn = this.linePlotBullish;
					object nearestBullish = bullishDyn.GetPointNearest(mouseX, mouseY, num);
					double item = ReadTupleField(nearestBullish, "Item1");
					double item2 = ReadTupleField(nearestBullish, "Item2");
					int item3 = ReadTupleFieldInt(nearestBullish, "Item3");
					if (this.lastHighlightedIdxBullish != item3)
					{
						this.highlightedBullish.X = item;
						this.highlightedBullish.Y = item2;
						this.highlightedBullish.IsVisible = true;
						this.lastHighlightedIdxBullish = item3;
						this.hLineBullish.Y = item2;
						this.vLineBullish.X = item;
					}
					dynamic bearishDyn = this.linePlotBearish;
					object nearestBearish = bearishDyn.GetPointNearest(mouseX, mouseY, num);
					double item4 = ReadTupleField(nearestBearish, "Item1");
					double item5 = ReadTupleField(nearestBearish, "Item2");
					int item6 = ReadTupleFieldInt(nearestBearish, "Item3");
					if (this.lastHighlightedIdxBearish != item6)
					{
						this.highlightedBearish.X = item4;
						this.highlightedBearish.Y = item5;
						this.highlightedBearish.IsVisible = true;
						this.lastHighlightedIdxBearish = item6;
						this.hLineBearish.Y = item5;
						this.vLineBearish.X = item4;
					}
					this.lineChartObject.Render(false);
					return;
				}
			}
			public void RefreshData(List<DDQuantZone.SampleInfo> listSampleInfo, int sampleRange, global::System.Windows.Media.Brush brushBullish, global::System.Windows.Media.Brush brushBearish)
			{
				this.CreateNewLineChartObject();
				if (listSampleInfo != null && listSampleInfo.Count > 0)
				{
					int count = listSampleInfo.Count;
					double[] array = new double[count];
					double[] array2 = new double[count];
					double[] array3 = new double[count];
					for (int i = 0; i < count; i++)
					{
						DDQuantZone.SampleInfo sampleInfo = listSampleInfo[i];
						array[i] = (double)sampleInfo.SampleIncrease;
						array2[i] = (double)sampleInfo.SampleDecreases;
						array3[i] = (double)sampleInfo.To;
					}
					global::System.Windows.Media.SolidColorBrush solidColorBrush = (global::System.Windows.Media.SolidColorBrush)brushBullish;
					global::System.Windows.Media.SolidColorBrush solidColorBrush2 = (global::System.Windows.Media.SolidColorBrush)brushBearish;
					global::System.Drawing.Color color = global::System.Drawing.Color.FromArgb((int)solidColorBrush.Color.A, (int)solidColorBrush.Color.R, (int)solidColorBrush.Color.G, (int)solidColorBrush.Color.B);
					global::System.Drawing.Color color2 = global::System.Drawing.Color.FromArgb((int)solidColorBrush2.Color.A, (int)solidColorBrush2.Color.R, (int)solidColorBrush2.Color.G, (int)solidColorBrush2.Color.B);
					this.lineChartObject.Plot.XAxis.ManualTickSpacing((double)sampleRange);
					this.linePlotBullish = this.lineChartObject.Plot.AddScatter(array3, array, new global::System.Drawing.Color?(color), 1f, 5f, MarkerShape.filledCircle, LineStyle.Solid, null);
					this.linePlotBearish = this.lineChartObject.Plot.AddScatter(array3, array2, new global::System.Drawing.Color?(color2), 1f, 5f, MarkerShape.filledCircle, LineStyle.Solid, null);
					this.lineChartObject.Plot.Legend(true, Alignment.LowerRight);
					this.lineChartObject.Refresh(false);
					return;
				}
			}
			public RoutedEventHandler HelpMenuItemClicked;
			private WpfPlot lineChartObject;
			private ContextMenu rightClickMenu;
			private HLine hLineBullish;
			private HLine hLineBearish;
			private VLine vLineBullish;
			private VLine vLineBearish;
			private ScatterPlot linePlotBullish;
			private ScatterPlot linePlotBearish;
			private int lastHighlightedIdxBullish = -1;
			private int lastHighlightedIdxBearish = -1;
			private MarkerPlot highlightedBullish;
			private MarkerPlot highlightedBearish;
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
		private void PrintException(Exception exception)
		{
			string text = "DDQuantZone: " + exception.ToString() + " (" + exception.StackTrace + ")";
			Print((object)text);
			Log(text, NinjaTrader.Cbi.LogLevel.Error);
		}
		private string FormatMarkerString(string text)
		{
			if (string.IsNullOrWhiteSpace(text)) return string.Empty;
			string[] parts = text.Trim().Split(new[] { '+' }, StringSplitOptions.RemoveEmptyEntries);
			for (int i = 0; i < parts.Length; i++) parts[i] = parts[i].Trim();
			return string.Join("\n", parts).Trim();
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
	}

	public enum DDQuantZone_AdverseExcursion
	{
		L50,
		L84,
		L97,
		L99
	}
	public enum DDQuantZone_DisplayMode
	{
		Gradient,
		Normal,
		Disabled
	}
	public enum DDQuantZone_Mode
	{
		Percentile,
		Gaussian
	}
	
	public enum DDQuantZone_MarkerRenderingMethod
	{
		Custom,
		Builtin
	}
	public enum DDQuantZone_FavorableExcursion
	{
		T84,
		T50,
		T15
	}
	
	public enum DDQuantZone_Operators
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
		private DimDim.DDQuantZone[] cacheDDQuantZone;
		public DimDim.DDQuantZone DDQuantZone(int defaultFastPeriod, int defaultSlowPeriod, int pAdverseExcursion, int pMaxAdverseExcursion, int pMaxFavorableExcursion, int pFavorableExcursion, int sampleSkip, int sampleMax, int sampleRange, int sampleSteps, int probabilityZoneLine)
		{
			return DDQuantZone(Input, defaultFastPeriod, defaultSlowPeriod, pAdverseExcursion, pMaxAdverseExcursion, pMaxFavorableExcursion, pFavorableExcursion, sampleSkip, sampleMax, sampleRange, sampleSteps, probabilityZoneLine);
		}

		public DimDim.DDQuantZone DDQuantZone(ISeries<double> input, int defaultFastPeriod, int defaultSlowPeriod, int pAdverseExcursion, int pMaxAdverseExcursion, int pMaxFavorableExcursion, int pFavorableExcursion, int sampleSkip, int sampleMax, int sampleRange, int sampleSteps, int probabilityZoneLine)
		{
			if (cacheDDQuantZone != null)
				for (int idx = 0; idx < cacheDDQuantZone.Length; idx++)
					if (cacheDDQuantZone[idx] != null && cacheDDQuantZone[idx].DefaultFastPeriod == defaultFastPeriod && cacheDDQuantZone[idx].DefaultSlowPeriod == defaultSlowPeriod && cacheDDQuantZone[idx].PAdverseExcursion == pAdverseExcursion && cacheDDQuantZone[idx].PMaxAdverseExcursion == pMaxAdverseExcursion && cacheDDQuantZone[idx].PMaxFavorableExcursion == pMaxFavorableExcursion && cacheDDQuantZone[idx].PFavorableExcursion == pFavorableExcursion && cacheDDQuantZone[idx].SampleSkip == sampleSkip && cacheDDQuantZone[idx].SampleMax == sampleMax && cacheDDQuantZone[idx].SampleRange == sampleRange && cacheDDQuantZone[idx].SampleSteps == sampleSteps && cacheDDQuantZone[idx].ProbabilityZoneLine == probabilityZoneLine && cacheDDQuantZone[idx].EqualsInput(input))
						return cacheDDQuantZone[idx];
			return CacheIndicator<DimDim.DDQuantZone>(new DimDim.DDQuantZone(){ DefaultFastPeriod = defaultFastPeriod, DefaultSlowPeriod = defaultSlowPeriod, PAdverseExcursion = pAdverseExcursion, PMaxAdverseExcursion = pMaxAdverseExcursion, PMaxFavorableExcursion = pMaxFavorableExcursion, PFavorableExcursion = pFavorableExcursion, SampleSkip = sampleSkip, SampleMax = sampleMax, SampleRange = sampleRange, SampleSteps = sampleSteps, ProbabilityZoneLine = probabilityZoneLine }, input, ref cacheDDQuantZone);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.DimDim.DDQuantZone DDQuantZone(int defaultFastPeriod, int defaultSlowPeriod, int pAdverseExcursion, int pMaxAdverseExcursion, int pMaxFavorableExcursion, int pFavorableExcursion, int sampleSkip, int sampleMax, int sampleRange, int sampleSteps, int probabilityZoneLine)
		{
			return indicator.DDQuantZone(Input, defaultFastPeriod, defaultSlowPeriod, pAdverseExcursion, pMaxAdverseExcursion, pMaxFavorableExcursion, pFavorableExcursion, sampleSkip, sampleMax, sampleRange, sampleSteps, probabilityZoneLine);
		}

		public Indicators.DimDim.DDQuantZone DDQuantZone(ISeries<double> input , int defaultFastPeriod, int defaultSlowPeriod, int pAdverseExcursion, int pMaxAdverseExcursion, int pMaxFavorableExcursion, int pFavorableExcursion, int sampleSkip, int sampleMax, int sampleRange, int sampleSteps, int probabilityZoneLine)
		{
			return indicator.DDQuantZone(input, defaultFastPeriod, defaultSlowPeriod, pAdverseExcursion, pMaxAdverseExcursion, pMaxFavorableExcursion, pFavorableExcursion, sampleSkip, sampleMax, sampleRange, sampleSteps, probabilityZoneLine);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.DimDim.DDQuantZone DDQuantZone(int defaultFastPeriod, int defaultSlowPeriod, int pAdverseExcursion, int pMaxAdverseExcursion, int pMaxFavorableExcursion, int pFavorableExcursion, int sampleSkip, int sampleMax, int sampleRange, int sampleSteps, int probabilityZoneLine)
		{
			return indicator.DDQuantZone(Input, defaultFastPeriod, defaultSlowPeriod, pAdverseExcursion, pMaxAdverseExcursion, pMaxFavorableExcursion, pFavorableExcursion, sampleSkip, sampleMax, sampleRange, sampleSteps, probabilityZoneLine);
		}

		public Indicators.DimDim.DDQuantZone DDQuantZone(ISeries<double> input , int defaultFastPeriod, int defaultSlowPeriod, int pAdverseExcursion, int pMaxAdverseExcursion, int pMaxFavorableExcursion, int pFavorableExcursion, int sampleSkip, int sampleMax, int sampleRange, int sampleSteps, int probabilityZoneLine)
		{
			return indicator.DDQuantZone(input, defaultFastPeriod, defaultSlowPeriod, pAdverseExcursion, pMaxAdverseExcursion, pMaxFavorableExcursion, pFavorableExcursion, sampleSkip, sampleMax, sampleRange, sampleSteps, probabilityZoneLine);
		}
	}
}

#endregion
