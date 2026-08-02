using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Core;
using NinjaTrader.Custom;
using NinjaTrader.Data;
using NinjaTrader.Gui;
using NinjaTrader.Gui.NinjaScript;
using NinjaTrader.NinjaScript.DrawingTools;

namespace NinjaTrader.NinjaScript.Indicators
{
	[CategoryOrder("Ultimate Zones", 1)]
	[CategoryOrder("Small Arrow Settings", 4)]
	[CategoryOrder("Alerts", 2)]
	[CategoryOrder("Version", 0)]
	[CategoryOrder("Large Arrow Settings", 3)]
	public class UltimateAIProV3 : Indicator
	{
		private class LineLevel_Large
		{
			public bool trIsEnabled_Trail;

			public bool trIsEnabled_Bind;

			private UltimateAIProV3 s;

			public double level;

			public string tmpLineName;

			public string directionPrefix;

			public Stroke tmpLineStroke;

			public Stroke brokenLineStroke;

			public LineLevel_Large(UltimateAIProV3 s, bool isEnabled_Trail, bool isEnabled_Bind)
			{
				this.s = s;
				level = 0.0;
			}

			public void DestroyTemp(int shift)
			{
				string text = directionPrefix + "_" + s.Time[shift];
				if (s.glList_Tags.Contains(text))
				{
					s.glList_Tags.Remove(text);
					s.RemoveDrawObject(text);
				}
			}

			public void Trail(int shift, double _level, bool isDraw = true)
			{
				level = _level;
				if (!s.pmIsOutside && trIsEnabled_Trail && isDraw)
				{
					string text = directionPrefix + "_" + s.Time[shift];
					Line line = Draw.Line(s, text, shift, level, shift - 1, level, Brushes.Blue);
					line.Stroke = tmpLineStroke;
					s.glList_Tags.Add(text);
				}
			}

			public void Bind(int shift, double _level, bool isDraw = true)
			{
				level = _level;
				if (!s.pmIsOutside && trIsEnabled_Bind && isDraw)
				{
					string tag = directionPrefix + "_F_" + s.Time[shift];
					Line line = Draw.Line(s, tag, shift, level, shift - 1, level, Brushes.Blue);
					line.Stroke = brokenLineStroke;
						level = 0.0;
				}
			}
		}

		private class BuyLineLevel_Large : LineLevel_Large
		{
			public BuyLineLevel_Large(UltimateAIProV3 s, bool isEnabled_Trail, bool isEnabled_Bind)
				: base(s, isEnabled_Trail, isEnabled_Bind)
			{
				trIsEnabled_Trail = isEnabled_Trail;
				trIsEnabled_Bind = isEnabled_Bind;
				directionPrefix = "buyL";
				tmpLineName = directionPrefix + "_tmp";
				tmpLineStroke = s.buy_tmp_line_stroke;
				brokenLineStroke = s.buy_broken_line_stroke;
			}
		}

		private class SellLineLevel_Large : LineLevel_Large
		{
			public SellLineLevel_Large(UltimateAIProV3 s, bool isEnabled_Trail, bool isEnabled_Bind)
				: base(s, isEnabled_Trail, isEnabled_Bind)
			{
				trIsEnabled_Trail = isEnabled_Trail;
				trIsEnabled_Bind = isEnabled_Bind;
				directionPrefix = "sellL";
				tmpLineName = directionPrefix + "_tmp";
				tmpLineStroke = s.sell_tmp_line_stroke;
				brokenLineStroke = s.sell_broken_line_stroke;
			}
		}

		private class LineLevel_Small
		{
			private UltimateAIProV3 s;

			public bool trIsBind;

			public bool trIsEnabled_Trail;

			public bool trIsEnabled_Bind;

			public int trCurrentBar;

			public double level;

			public string tmpLineName;

			public string directionPrefix;

			public Stroke tmpLineStroke;

			public Stroke brokenLineStroke;

			public LineLevel_Small(UltimateAIProV3 s, int cb, bool isEnabled_Trail, bool isEnabled_Bind)
			{
				this.s = s;
				level = 0.0;
				trCurrentBar = cb;
			}

			public void DestroyTemp_Small(int shift)
			{
				string text = tmpLineName;
				s.RemoveDrawObject(text);
			}

			public void Trail(int shift, double _level)
			{
				level = _level;
				if (!s.pmIsOutside && trIsEnabled_Trail)
				{
					string tag = tmpLineName;
					Line line = Draw.Line(s, tag, shift, level, shift - 1, level, Brushes.Blue);
					line.Stroke = tmpLineStroke;
				}
			}

			public void Bind(int shift)
			{
				if (!s.pmIsOutside && trIsEnabled_Bind)
				{
					s.RemoveDrawObject(tmpLineName);
					string tag = tmpLineName + "_F_" + s.Time[shift];
					Line line = Draw.Line(s, tag, shift, level, shift - 1, level, Brushes.Blue);
					line.Stroke = brokenLineStroke;
						level = 0.0;
				}
			}
		}

		private class BuyLineLevel_Small : LineLevel_Small
		{
			public BuyLineLevel_Small(UltimateAIProV3 s, int cb, bool isEnabled_Trail, bool isEnabled_Bind)
				: base(s, cb, isEnabled_Trail, isEnabled_Bind)
			{
				trIsEnabled_Trail = isEnabled_Trail;
				trIsEnabled_Bind = isEnabled_Bind;
				directionPrefix = "buyS";
				trCurrentBar = cb;
				tmpLineName = directionPrefix + "_tmp" + trCurrentBar;
				tmpLineStroke = s.buy_tmp_line_stroke_small;
				brokenLineStroke = s.buy_broken_line_stroke_small;
			}
		}

		private class SellLineLevel_Small : LineLevel_Small
		{
			public SellLineLevel_Small(UltimateAIProV3 s, int cb, bool isEnabled_Trail, bool isEnabled_Bind)
				: base(s, cb, isEnabled_Trail, isEnabled_Bind)
			{
				trIsEnabled_Trail = isEnabled_Trail;
				trIsEnabled_Bind = isEnabled_Bind;
				directionPrefix = "sellS";
				trCurrentBar = cb;
				tmpLineName = directionPrefix + "_tmp" + trCurrentBar;
				tmpLineStroke = s.sell_tmp_line_stroke_small;
				brokenLineStroke = s.sell_broken_line_stroke_small;
			}
		}

		private const string par_HTF = "Ultimate Zones";

		private const string parBuffers = "Buffers";

		private const string param_title_breakout_settings = "Large Arrow Settings";

		private const string param_title_breakout_settings_small = "Small Arrow Settings";

		private const string parAlerts = "Alerts";

		private double percentamount;

		private double revAmount;

		private double atrreversal;

		private int atrlength;

		private int averagelength;

		private string FileAlert1;

		private Series<double> K;

		private Series<double> macd;

		private WiZigZagHighLowTOS1v0 EI;

		private UltimateAIProV3 glUltimateAIPro;

		private Series<double> macdup;

		private Series<double> macddown;

		private Series<double> macd1;

		private Series<double> macd2;

		private Series<double> buy;

		private Series<double> buysignal;

		private Series<double> sell;

		private Series<double> sellsignal;

		private Series<double> EISave;

		private Series<double> isConf;

		private Series<double> EIL;

		private Series<double> EIH;

		private Series<double> dir;

		private Series<double> signal;

		private Series<double> revLineTop;

		private Series<double> revLineBot;

		private List<string> glList_Tags;

		private int lbPot;

		private string glID;

		public Stroke buy_tmp_line_stroke;

		public Stroke buy_broken_line_stroke;

		public Stroke sell_tmp_line_stroke;

		public Stroke sell_broken_line_stroke;

		public Stroke buy_tmp_line_stroke_small;

		public Stroke buy_broken_line_stroke_small;

		public Stroke sell_tmp_line_stroke_small;

		public Stroke sell_broken_line_stroke_small;

		public Stroke htf_up_line_stroke;

		public Stroke htf_dn_line_stroke;

		public Stroke pmBuffer_topLine_Stroke_stroke;

		public Stroke pmBuffer_botLine_Stroke_stroke;

		public Stroke pmBuffer_upSignal_Stroke_stroke;

		public Stroke pmBuffer_Long_WithDot_Stroke_stroke;

		public Stroke pmBuffer_dnSignal_Stroke_stroke;

		public Stroke pmBuffer_Short_WithDot_Stroke_stroke;

		public Stroke pmBuffer_long_Stroke_stroke;

		public Stroke pmBuffer_short_Stroke_stroke;

		public Stroke pmBuffer_longDot_Stroke_stroke;

		public Stroke pmBuffer_shortDot_Stroke_stroke;

		private LineLevel_Large buyLine;

		private LineLevel_Large sellLine;

		private LineLevel_Small buyLine_Small;

		private LineLevel_Small sellLine_Small;

		private int glLastAlertBar_BUY;

		private int glLastAlertBar_SELL;

		private int glLastAlertBar_longDot;

		private int glLastAlertBar_shortDot;

		private int glLastAlertBar_longS;

		private int glLastAlertBar_shortS;

		private int glLastAlertBar_upSignal;

		private int glLastAlertBar_dnSignal;

		private int upSignal_plot;

		private int dnSignal_plot;

		private int Colorbars_plot;

		private int EnhancedLines_plot;

		private int longS_plot;

		private int shortS_plot;

		private int botLine_plot;

		private int topLine_plot;

		private int BUY_plot;

		private int SELL_plot;

		private int Dot_longS;

		private int Dot_shortS;

		private int botLine_plot_htf;

		private int topLine_plot_htf;

		public override string DisplayName => Name;

		[Display(Name = "Version", Description = "Version", Order = 1, GroupName = "Version")]
		[ReadOnly(true)]
		[XmlIgnore]
		public string Version { get; set; }

		[ReadOnly(true)]
		private bool pmIsOutside { get; set; }

		private int trendLength { get; set; }

		private int sequentialLength { get; set; }

		private int fastLength { get; set; }

		private int slowLength { get; set; }

		private int MACDLength { get; set; }

		private UltimateAIPro_AverageType averageType { get; set; }

		private int overbought { get; set; }

		private int oversold { get; set; }

		private int KPeriod { get; set; }

		private int DPeriod { get; set; }

		private UltimateAIPro_Price priceH { get; set; }

		private UltimateAIPro_Price priceL { get; set; }

		private UltimateAIPro_Price priceC { get; set; }

		private UltimateAIPro_AverageType avgType { get; set; }

		private UltimateAIPro_Method method { get; set; }

		[NinjaScriptProperty]
		[Display(ResourceType = typeof(Resource), Name = "Enable Ultimate Zones", Order = 1, GroupName = "Ultimate Zones")]
		public bool pmHTF_Enable { get; set; }

		[NinjaScriptProperty]
		[Display(ResourceType = typeof(Resource), Name = "Zone Type", Order = 2, GroupName = "Ultimate Zones")]
		public EnumHtfType_UltimateAIPro pmHTF_Type { get; set; }

		[Display(ResourceType = typeof(Resource), Name = "Zone Period", Order = 3, GroupName = "Ultimate Zones")]
		[NinjaScriptProperty]
		public int pmHTF_Period { get; set; }

		[NinjaScriptProperty]
		[Display(ResourceType = typeof(Resource), Name = "Zone Days To Load", Order = 4, GroupName = "Ultimate Zones")]
		public int pmHTF_DaysToLoad { get; set; }

		[Display(Name = "Zone Up Line Stroke", Order = 30, GroupName = "Ultimate Zones")]
		public Stroke pmHTF_UpLineStroke
		{
			get
			{
				return htf_up_line_stroke;
			}
			set
			{
				htf_up_line_stroke = value;
			}
		}

		[Display(Name = "Zone Dn Line Stroke", Order = 31, GroupName = "Ultimate Zones")]
		public Stroke pmHTF_DnLineStroke
		{
			get
			{
				return htf_dn_line_stroke;
			}
			set
			{
				htf_dn_line_stroke = value;
			}
		}

		[Display(ResourceType = typeof(Resource), Name = "Enable upSignal", Order = 1, GroupName = "Buffers")]
		[NinjaScriptProperty]
		public bool pmBuffer_upSignal_Enable { get; set; }

		[Display(Name = "upSignal Stroke", Order = 2, GroupName = "Buffers")]
		public Stroke pmBuffer_upSignal_Stroke
		{
			get
			{
				return pmBuffer_upSignal_Stroke_stroke;
			}
			set
			{
				pmBuffer_upSignal_Stroke_stroke = value;
			}
		}

		[NinjaScriptProperty]
		[Display(ResourceType = typeof(Resource), Name = "upSignal Offset", Order = 4, GroupName = "Buffers")]
		public int pmBuffer_upSignal_Offset { get; set; }

		[Display(ResourceType = typeof(Resource), Name = "Enable dnSignal", Order = 10, GroupName = "Buffers")]
		[NinjaScriptProperty]
		public bool pmBuffer_dnSignal_Enable { get; set; }

		[NinjaScriptProperty]
		[Display(ResourceType = typeof(Resource), Name = "upSignal Offset", Order = 11, GroupName = "Buffers")]
		public int pmBuffer_dnSignal_Offset { get; set; }

		[Display(Name = "dnSignal Stroke", Order = 12, GroupName = "Buffers")]
		public Stroke pmBuffer_dnSignal_Stroke
		{
			get
			{
				return pmBuffer_dnSignal_Stroke_stroke;
			}
			set
			{
				pmBuffer_dnSignal_Stroke_stroke = value;
			}
		}

		[Display(ResourceType = typeof(Resource), Name = "Enable long", Order = 21, GroupName = "Buffers")]
		[NinjaScriptProperty]
		public bool pmBuffer_long_Enable { get; set; }

		[Display(ResourceType = typeof(Resource), Name = "long Offset", Order = 22, GroupName = "Buffers")]
		[NinjaScriptProperty]
		public int pmBuffer_long_Offset { get; set; }

		[Display(Name = "long Stroke", Order = 23, GroupName = "Buffers")]
		public Stroke pmBuffer_long_Stroke
		{
			get
			{
				return pmBuffer_long_Stroke_stroke;
			}
			set
			{
				pmBuffer_long_Stroke_stroke = value;
			}
		}

		[Display(Name = "Long With Dot Stroke", Order = 24, GroupName = "Buffers")]
		public Stroke pmBuffer_Long_WithDot_Stroke
		{
			get
			{
				return pmBuffer_Long_WithDot_Stroke_stroke;
			}
			set
			{
				pmBuffer_Long_WithDot_Stroke_stroke = value;
			}
		}

		[Display(ResourceType = typeof(Resource), Name = "Enable short", Order = 40, GroupName = "Buffers")]
		[NinjaScriptProperty]
		public bool pmBuffer_short_Enable { get; set; }

		[NinjaScriptProperty]
		[Display(ResourceType = typeof(Resource), Name = "short Offset", Order = 41, GroupName = "Buffers")]
		public int pmBuffer_short_Offset { get; set; }

		[Display(Name = "short Stroke", Order = 12, GroupName = "Buffers")]
		public Stroke pmBuffer_short_Stroke
		{
			get
			{
				return pmBuffer_short_Stroke_stroke;
			}
			set
			{
				pmBuffer_short_Stroke_stroke = value;
			}
		}

		[Display(Name = "Short With Dot Stroke", Order = 44, GroupName = "Buffers")]
		public Stroke pmBuffer_Short_WithDot_Stroke
		{
			get
			{
				return pmBuffer_Short_WithDot_Stroke_stroke;
			}
			set
			{
				pmBuffer_Short_WithDot_Stroke_stroke = value;
			}
		}

		[NinjaScriptProperty]
		[Display(ResourceType = typeof(Resource), Name = "Enable longDot", Order = 51, GroupName = "Buffers")]
		public bool pmBuffer_longDot_Enable { get; set; }

		[Display(Name = "longDot Stroke", Order = 52, GroupName = "Buffers")]
		public Stroke pmBuffer_longDot_Stroke
		{
			get
			{
				return pmBuffer_longDot_Stroke_stroke;
			}
			set
			{
				pmBuffer_longDot_Stroke_stroke = value;
			}
		}

		[Display(ResourceType = typeof(Resource), Name = "Enable shortDot", Order = 53, GroupName = "Buffers")]
		[NinjaScriptProperty]
		public bool pmBuffer_shortDot_Enable { get; set; }

		[Display(Name = "shortDot Stroke", Order = 54, GroupName = "Buffers")]
		public Stroke pmBuffer_shortDot_Stroke
		{
			get
			{
				return pmBuffer_shortDot_Stroke_stroke;
			}
			set
			{
				pmBuffer_shortDot_Stroke_stroke = value;
			}
		}

		[NinjaScriptProperty]
		[Display(ResourceType = typeof(Resource), Name = "Enable botLine", Order = 60, GroupName = "Buffers")]
		public bool pmBuffer_botLine_Enable { get; set; }

		[Display(Name = "botLine Stroke", Order = 61, GroupName = "Buffers")]
		public Stroke pmBuffer_botLine_Stroke
		{
			get
			{
				return pmBuffer_botLine_Stroke_stroke;
			}
			set
			{
				pmBuffer_botLine_Stroke_stroke = value;
			}
		}

		[Display(ResourceType = typeof(Resource), Name = "Enable topLine", Order = 62, GroupName = "Buffers")]
		[NinjaScriptProperty]
		public bool pmBuffer_topLine_Enable { get; set; }

		[Display(Name = "topLine Stroke", Order = 63, GroupName = "Buffers")]
		public Stroke pmBuffer_topLine_Stroke
		{
			get
			{
				return pmBuffer_topLine_Stroke_stroke;
			}
			set
			{
				pmBuffer_topLine_Stroke_stroke = value;
			}
		}

		[Display(Name = "Buy Line Buffer Ticks", Order = 10, GroupName = "Large Arrow Settings")]
		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		public int high_buffer_ticks { get; set; }

		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name = "Sell Line Buffer Ticks", Order = 20, GroupName = "Large Arrow Settings")]
		public int low_buffer_ticks { get; set; }

		[Display(Name = "Buy Line Stroke", Order = 30, GroupName = "Large Arrow Settings")]
		public Stroke HighTmpLineStroke
		{
			get
			{
				return buy_tmp_line_stroke;
			}
			set
			{
				buy_tmp_line_stroke = value;
			}
		}

		[Display(Name = "Buy Broken Line Stroke", Order = 40, GroupName = "Large Arrow Settings")]
		public Stroke HighBrokenLineStroke
		{
			get
			{
				return buy_broken_line_stroke;
			}
			set
			{
				buy_broken_line_stroke = value;
			}
		}

		[Display(Name = "Sell Line Stroke", Order = 50, GroupName = "Large Arrow Settings")]
		public Stroke LowTmpLineStroke
		{
			get
			{
				return sell_tmp_line_stroke;
			}
			set
			{
				sell_tmp_line_stroke = value;
			}
		}

		[Display(Name = "Sell Broken Line Stroke", Order = 60, GroupName = "Large Arrow Settings")]
		public Stroke LowBrokenLineStroke
		{
			get
			{
				return sell_broken_line_stroke;
			}
			set
			{
				sell_broken_line_stroke = value;
			}
		}

		[Display(Name = "Buy Line Buffer Ticks", Order = 10, GroupName = "Small Arrow Settings")]
		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		public int high_buffer_ticks_small { get; set; }

		[Range(0, int.MaxValue)]
		[Display(Name = "Sell Line Buffer Ticks", Order = 20, GroupName = "Small Arrow Settings")]
		[NinjaScriptProperty]
		public int low_buffer_ticks_small { get; set; }

		[Display(Name = "Buy Line Stroke", Order = 30, GroupName = "Small Arrow Settings")]
		public Stroke HighTmpLineStroke_small
		{
			get
			{
				return buy_tmp_line_stroke_small;
			}
			set
			{
				buy_tmp_line_stroke_small = value;
			}
		}

		[Display(Name = "Buy Broken Line Stroke", Order = 40, GroupName = "Small Arrow Settings")]
		public Stroke HighBrokenLineStroke_small
		{
			get
			{
				return buy_broken_line_stroke_small;
			}
			set
			{
				buy_broken_line_stroke_small = value;
			}
		}

		[Display(Name = "Sell Line Stroke", Order = 50, GroupName = "Small Arrow Settings")]
		public Stroke LowTmpLineStroke_small
		{
			get
			{
				return sell_tmp_line_stroke_small;
			}
			set
			{
				sell_tmp_line_stroke_small = value;
			}
		}

		[Display(Name = "Sell Broken Line Stroke", Order = 60, GroupName = "Small Arrow Settings")]
		public Stroke LowBrokenLineStroke_small
		{
			get
			{
				return sell_broken_line_stroke_small;
			}
			set
			{
				sell_broken_line_stroke_small = value;
			}
		}

		[Display(Name = "upSignal", Description = "", Order = 1, GroupName = "Alerts")]
		[NinjaScriptProperty]
		public bool pmAlerts_upSignal_Enable { get; set; }

		[PropertyEditor("NinjaTrader.Gui.Tools.FilePathPicker", Filter = "WAV Files (*.wav)|*.wav")]
		[NinjaScriptProperty]
		[Display(Name = "upSignal Sound", Order = 2, GroupName = "Alerts")]
		public string pmAlerts_upSignal_File { get; set; }

		[Display(Name = "dnSignal", Description = "", Order = 5, GroupName = "Alerts")]
		[NinjaScriptProperty]
		public bool pmAlerts_dnSignal_Enable { get; set; }

		[PropertyEditor("NinjaTrader.Gui.Tools.FilePathPicker", Filter = "WAV Files (*.wav)|*.wav")]
		[NinjaScriptProperty]
		[Display(Name = "dnSignal Sound", Order = 6, GroupName = "Alerts")]
		public string pmAlerts_dnSignal_File { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "long", Description = "", Order = 10, GroupName = "Alerts")]
		public bool pmAlerts_long_Enable { get; set; }

		[PropertyEditor("NinjaTrader.Gui.Tools.FilePathPicker", Filter = "WAV Files (*.wav)|*.wav")]
		[Display(Name = "long Sound", Order = 11, GroupName = "Alerts")]
		[NinjaScriptProperty]
		public string pmAlerts_long_File { get; set; }

		[Display(Name = "short", Description = "", Order = 15, GroupName = "Alerts")]
		[NinjaScriptProperty]
		public bool pmAlerts_short_Enable { get; set; }

		[Display(Name = "short Sound", Order = 16, GroupName = "Alerts")]
		[PropertyEditor("NinjaTrader.Gui.Tools.FilePathPicker", Filter = "WAV Files (*.wav)|*.wav")]
		[NinjaScriptProperty]
		public string pmAlerts_short_File { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "BUY", Description = "", Order = 20, GroupName = "Alerts")]
		public bool pmAlerts_BUY_Enable { get; set; }

		[PropertyEditor("NinjaTrader.Gui.Tools.FilePathPicker", Filter = "WAV Files (*.wav)|*.wav")]
		[Display(Name = "BUY Sound", Order = 21, GroupName = "Alerts")]
		[NinjaScriptProperty]
		public string pmAlerts_BUY_File { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "SELL", Description = "", Order = 25, GroupName = "Alerts")]
		public bool pmAlerts_SELL_Enable { get; set; }

		[NinjaScriptProperty]
		[PropertyEditor("NinjaTrader.Gui.Tools.FilePathPicker", Filter = "WAV Files (*.wav)|*.wav")]
		[Display(Name = "SELL Sound", Order = 26, GroupName = "Alerts")]
		public string pmAlerts_SELL_File { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "longDot", Description = "", Order = 20, GroupName = "Alerts")]
		public bool pmAlerts_longDot_Enable { get; set; }

		[PropertyEditor("NinjaTrader.Gui.Tools.FilePathPicker", Filter = "WAV Files (*.wav)|*.wav")]
		[NinjaScriptProperty]
		[Display(Name = "longDot Sound", Order = 21, GroupName = "Alerts")]
		public string pmAlerts_longDot_File { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "shortDot", Description = "", Order = 25, GroupName = "Alerts")]
		public bool pmAlerts_shortDot_Enable { get; set; }

		[PropertyEditor("NinjaTrader.Gui.Tools.FilePathPicker", Filter = "WAV Files (*.wav)|*.wav")]
		[NinjaScriptProperty]
		[Display(Name = "shortDot Sound", Order = 26, GroupName = "Alerts")]
		public string pmAlerts_shortDot_File { get; set; }

		[XmlIgnore]
		[Browsable(false)]
		public Series<double> upSignal => Values[upSignal_plot];

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> dnSignal => Values[dnSignal_plot];

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> Colorbars => Values[Colorbars_plot];

		[XmlIgnore]
		[Browsable(false)]
		public Series<double> EnhancedLines => Values[EnhancedLines_plot];

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> longS => Values[longS_plot];

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> shortS => Values[shortS_plot];

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> botLine => Values[botLine_plot];

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> topLine => Values[topLine_plot];

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> BUY => Values[BUY_plot];

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> SELL => Values[SELL_plot];

		[XmlIgnore]
		[Browsable(false)]
		public Series<double> glBuffer_Dot_longS => Values[Dot_longS];

		[XmlIgnore]
		[Browsable(false)]
		public Series<double> glBuffer_Dot_shortS => Values[Dot_shortS];

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> botLine_htf => Values[botLine_plot_htf];

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> topLine_htf => Values[topLine_plot_htf];

		#region Lifecycle
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Name = "UltimateAIProV3";
				Version = Name;
				Description = "";
				IsOverlay = true;
				IsSuspendedWhileInactive = true;
				Calculate = Calculate.OnEachTick;
				MaximumBarsLookBack = (MaximumBarsLookBack)1;
				pmIsOutside = false;
				percentamount = 0.01;
				revAmount = 0.05;
				atrreversal = 2.0;
				atrlength = 5;
				averagelength = 5;
				trendLength = 5;
				sequentialLength = 3;
				fastLength = 5;
				slowLength = 26;
				MACDLength = 9;
				averageType = UltimateAIPro_AverageType.EXPONENTIAL;
				overbought = 80;
				oversold = 20;
				KPeriod = 10;
				DPeriod = 10;
				priceH = UltimateAIPro_Price.High;
				priceL = UltimateAIPro_Price.Low;
				priceC = UltimateAIPro_Price.Close;
				avgType = UltimateAIPro_AverageType.SIMPLE;
				pmAlerts_BUY_Enable = true;
				pmAlerts_SELL_Enable = true;
				pmAlerts_dnSignal_Enable = true;
				pmAlerts_upSignal_Enable = true;
				pmAlerts_longDot_Enable = true;
				pmAlerts_shortDot_Enable = true;
				pmAlerts_long_Enable = true;
				pmAlerts_short_Enable = true;
				pmAlerts_BUY_File = Path.Combine(Globals.InstallDir, "sounds", "Alert2.wav");
				pmAlerts_SELL_File = Path.Combine(Globals.InstallDir, "sounds", "Alert2.wav");
				pmAlerts_dnSignal_File = Path.Combine(Globals.InstallDir, "sounds", "Alert2.wav");
				pmAlerts_upSignal_File = Path.Combine(Globals.InstallDir, "sounds", "Alert2.wav");
				pmAlerts_longDot_File = Path.Combine(Globals.InstallDir, "sounds", "Alert2.wav");
				pmAlerts_shortDot_File = Path.Combine(Globals.InstallDir, "sounds", "Alert2.wav");
				pmAlerts_long_File = Path.Combine(Globals.InstallDir, "sounds", "Alert2.wav");
				pmAlerts_short_File = Path.Combine(Globals.InstallDir, "sounds", "Alert2.wav");
				pmBuffer_upSignal_Enable = true;
				pmBuffer_dnSignal_Enable = true;
				pmBuffer_long_Enable = true;
				pmBuffer_longDot_Enable = true;
				pmBuffer_short_Enable = true;
				pmBuffer_shortDot_Enable = true;
				pmBuffer_botLine_Enable = true;
				pmBuffer_topLine_Enable = true;
				pmBuffer_upSignal_Offset = 4;
				pmBuffer_dnSignal_Offset = 4;
				pmBuffer_long_Offset = 6;
				pmBuffer_short_Offset = 6;
				method = UltimateAIPro_Method.Average;
				pmHTF_Enable = true;
				pmHTF_DaysToLoad = 1;
				pmHTF_Type = EnumHtfType_UltimateAIPro.Minute;
				pmHTF_Period = 15;
				high_buffer_ticks = 1;
				low_buffer_ticks = 1;
				buy_tmp_line_stroke = new Stroke(Brushes.LimeGreen);
				buy_broken_line_stroke = new Stroke(Brushes.Magenta);
				sell_tmp_line_stroke = new Stroke(Brushes.Red);
				sell_broken_line_stroke = new Stroke(Brushes.Magenta);
				high_buffer_ticks_small = 1;
				low_buffer_ticks_small = 1;
				buy_tmp_line_stroke_small = new Stroke(Brushes.Red);
				buy_broken_line_stroke_small = new Stroke(Brushes.Magenta);
				sell_tmp_line_stroke_small = new Stroke(Brushes.LimeGreen);
				sell_broken_line_stroke_small = new Stroke(Brushes.Magenta);
				htf_up_line_stroke = new Stroke(Brushes.Aqua);
				htf_dn_line_stroke = new Stroke(Brushes.RoyalBlue);
				pmBuffer_upSignal_Stroke_stroke = new Stroke(Brushes.LimeGreen, DashStyleHelper.Solid, 3f);
				pmBuffer_Long_WithDot_Stroke_stroke = new Stroke(Brushes.Magenta, DashStyleHelper.Solid, 3f);
				pmBuffer_dnSignal_Stroke_stroke = new Stroke(Brushes.Red, DashStyleHelper.Solid, 3f);
				pmBuffer_Short_WithDot_Stroke_stroke = new Stroke(Brushes.Magenta, DashStyleHelper.Solid, 3f);
				pmBuffer_long_Stroke_stroke = new Stroke(Brushes.LimeGreen, DashStyleHelper.Solid, 8f);
				pmBuffer_short_Stroke_stroke = new Stroke(Brushes.Red, DashStyleHelper.Solid, 8f);
				pmBuffer_botLine_Stroke_stroke = new Stroke(Brushes.LightGreen, DashStyleHelper.Solid, 1f);
				pmBuffer_topLine_Stroke_stroke = new Stroke(Brushes.LightPink, DashStyleHelper.Solid, 1f);
				pmBuffer_longDot_Stroke_stroke = new Stroke(Brushes.LimeGreen, DashStyleHelper.Solid, 3f);
				pmBuffer_shortDot_Stroke_stroke = new Stroke(Brushes.Red, DashStyleHelper.Solid, 3f);
				upSignal_plot = Plots.Length;
				AddPlot(pmBuffer_upSignal_Stroke_stroke, PlotStyle.TriangleUp, "upSignal");
				dnSignal_plot = Plots.Length;
				AddPlot(pmBuffer_dnSignal_Stroke_stroke, PlotStyle.Dot, "dnSignal");
				Colorbars_plot = Plots.Length;
				AddPlot(new Stroke(Brushes.Transparent, DashStyleHelper.Solid, 1f), PlotStyle.TriangleDown, "Colorbars");
				EnhancedLines_plot = Plots.Length;
				AddPlot(new Stroke(Brushes.Transparent, DashStyleHelper.Solid, 1f), PlotStyle.TriangleDown, "EnhancedLines");
				longS_plot = Plots.Length;
				AddPlot(pmBuffer_long_Stroke_stroke, PlotStyle.TriangleUp, "long");
				shortS_plot = Plots.Length;
				AddPlot(pmBuffer_short_Stroke_stroke, PlotStyle.Dot, "short");
				botLine_plot = Plots.Length;
				AddPlot(pmBuffer_botLine_Stroke_stroke, PlotStyle.TriangleDown, "botLine");
				topLine_plot = Plots.Length;
				AddPlot(pmBuffer_topLine_Stroke_stroke, PlotStyle.TriangleDown, "topLine");
				BUY_plot = Plots.Length;
				AddPlot(new Stroke(Brushes.Transparent, DashStyleHelper.Solid, 8f), PlotStyle.TriangleUp, "BUY");
				SELL_plot = Plots.Length;
				AddPlot(new Stroke(Brushes.Transparent, DashStyleHelper.Solid, 8f), PlotStyle.Dot, "SELL");
				Dot_longS = Plots.Length;
				AddPlot(pmBuffer_longDot_Stroke_stroke, PlotStyle.Hash, "long Dot");
				Dot_shortS = Plots.Length;
				AddPlot(pmBuffer_shortDot_Stroke_stroke, PlotStyle.Hash, "short Dot");
				botLine_plot_htf = Plots.Length;
				AddPlot(new Stroke(Brushes.Transparent, DashStyleHelper.Solid, 1f), PlotStyle.Hash, "botLine HTF");
				topLine_plot_htf = Plots.Length;
				AddPlot(new Stroke(Brushes.Transparent, DashStyleHelper.Solid, 1f), PlotStyle.Hash, "topLine HTF");
			}
			else if (State == State.Configure)
			{
				if (pmHTF_Enable)
				{
					AddDataSeries((BarsPeriodType)pmHTF_Type, pmHTF_Period);
				}
			}
			else if (State == State.DataLoaded)
			{
				if (!pmBuffer_upSignal_Enable)
				{
					((Stroke)Plots[upSignal_plot]).Brush = Brushes.Transparent;
				}
				else
				{
					((Stroke)Plots[upSignal_plot]).Brush = pmBuffer_upSignal_Stroke_stroke.Brush;
				}
				if (!pmBuffer_dnSignal_Enable)
				{
					((Stroke)Plots[dnSignal_plot]).Brush = Brushes.Transparent;
				}
				else
				{
					((Stroke)Plots[dnSignal_plot]).Brush = pmBuffer_dnSignal_Stroke_stroke.Brush;
				}
				if (!pmBuffer_long_Enable)
				{
					((Stroke)Plots[longS_plot]).Brush = Brushes.Transparent;
				}
				else
				{
					((Stroke)Plots[longS_plot]).Brush = pmBuffer_long_Stroke_stroke.Brush;
				}
				if (!pmBuffer_short_Enable)
				{
					((Stroke)Plots[shortS_plot]).Brush = Brushes.Transparent;
				}
				else
				{
					((Stroke)Plots[shortS_plot]).Brush = pmBuffer_short_Stroke_stroke.Brush;
				}
				if (!pmBuffer_longDot_Enable)
				{
					((Stroke)Plots[Dot_longS]).Brush = Brushes.Transparent;
				}
				else
				{
					((Stroke)Plots[Dot_longS]).Brush = pmBuffer_longDot_Stroke_stroke.Brush;
				}
				if (!pmBuffer_shortDot_Enable)
				{
					((Stroke)Plots[Dot_shortS]).Brush = Brushes.Transparent;
				}
				else
				{
					((Stroke)Plots[Dot_shortS]).Brush = pmBuffer_shortDot_Stroke_stroke.Brush;
				}
				if (!pmBuffer_botLine_Enable)
				{
					((Stroke)Plots[botLine_plot]).Brush = Brushes.Transparent;
				}
				else
				{
					((Stroke)Plots[botLine_plot]).Brush = pmBuffer_botLine_Stroke_stroke.Brush;
				}
				if (!pmBuffer_topLine_Enable)
				{
					((Stroke)Plots[topLine_plot]).Brush = Brushes.Transparent;
				}
				else
				{
					((Stroke)Plots[topLine_plot]).Brush = pmBuffer_topLine_Stroke_stroke.Brush;
				}
				Random random = new Random();
				int num = random.Next(5, 21);
				StringBuilder stringBuilder = new StringBuilder(num);
				for (int i = 0; i < num; i++)
				{
					stringBuilder.Append("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789"[random.Next("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789".Length)]);
				}
				glID = stringBuilder.ToString();
				macdup = new Series<double>(this, MaximumBarsLookBack.Infinite);
				macddown = new Series<double>(this, MaximumBarsLookBack.Infinite);
				macd1 = new Series<double>(this, MaximumBarsLookBack.Infinite);
				macd2 = new Series<double>(this, MaximumBarsLookBack.Infinite);
				buy = new Series<double>(this, MaximumBarsLookBack.Infinite);
				buysignal = new Series<double>(this, MaximumBarsLookBack.Infinite);
				sell = new Series<double>(this, MaximumBarsLookBack.Infinite);
				sellsignal = new Series<double>(this, MaximumBarsLookBack.Infinite);
				EISave = new Series<double>(this, MaximumBarsLookBack.Infinite);
				isConf = new Series<double>(this, MaximumBarsLookBack.Infinite);
				EIL = new Series<double>(this, MaximumBarsLookBack.Infinite);
				EIH = new Series<double>(this, MaximumBarsLookBack.Infinite);
				dir = new Series<double>(this, MaximumBarsLookBack.Infinite);
				signal = new Series<double>(this, MaximumBarsLookBack.Infinite);
				revLineTop = new Series<double>(this, MaximumBarsLookBack.Infinite);
				revLineBot = new Series<double>(this, MaximumBarsLookBack.Infinite);
				macd = new Series<double>(this, MaximumBarsLookBack.Infinite);
				glList_Tags = new List<string>();
				K = StochasticFullTOS1v0(overbought, oversold, KPeriod, DPeriod, (StochasticFullTOS1v0_Price)priceH, (StochasticFullTOS1v0_Price)priceL, (StochasticFullTOS1v0_Price)priceC, 3, (StochasticFullTOS1v0_AverageType)avgType).FullKSeries;
				EI = WiZigZagHighLowTOS1v0((WiZigZagHighLowTOS1v0_PriceTypeHL)priceH, (WiZigZagHighLowTOS1v0_PriceTypeHL)priceL, percentamount, revAmount, atrlength, atrreversal, 0);
				buyLine = new BuyLineLevel_Large(this, pmBuffer_longDot_Enable, pmBuffer_long_Enable);
				sellLine = new SellLineLevel_Large(this, pmBuffer_shortDot_Enable, pmBuffer_short_Enable);
			}
			else if (State == State.Historical)
			{
				Print((object)("Historical:" + DateTime.Now.ToString("yyyy.MM.dd HH.mm.ss.ffff")));
			}
			else if (State == State.Realtime)
			{
				Print((object)("Realtime:" + DateTime.Now.ToString("yyyy.MM.dd HH.mm.ss.ffff")));
			}
		}

		#endregion

		#region BarUpdate
		protected override void OnBarUpdate()
		{
			if (CurrentBars[0] <= trendLength || (pmHTF_Enable && CurrentBars[1] < 0))
			{
				return;
			}
			ActionHtfLines();
			if (BarsInProgress == 1)
			{
				return;
			}
			if (State == State.Historical && BarsInProgress == 0 && Time[0].Date != Time[1].Date)
			{
				Print((object)(pmIsOutside + " => " + DateTime.Now.ToString("yyyy.MM.dd HH.mm.ss.ffff") + " => New Day: " + Time[0].Date.ToString() + ", " + ((object)BarsArray[0]).ToString() + ", " + ((!pmHTF_Enable) ? "x" : CurrentBars[1].ToString()) + ", " + ((!pmHTF_Enable) ? "x" : ((object)BarsArray[1]).ToString())));
			}
			if (lbPot == 0)
			{
				lbPot = CurrentBar;
			}
			int num = MRO((Func<bool>)(() => EI.ZZDot[0] > 0.0), 2, CurrentBar);
			if (num >= 0)
			{
				lbPot = num + 1;
				int num2 = Math.Min(lbPot, CurrentBar);
				for (int num3 = num2; num3 >= 0; num3--)
				{
					ResetPlot(macdup, num3);
					ResetPlot(macddown, num3);
					ResetPlot(macd1, num3);
					ResetPlot(macd2, num3);
					ResetPlot(buy, num3);
					ResetPlot(buysignal, num3);
					ResetPlot(sell, num3);
					ResetPlot(sellsignal, num3);
					ResetPlot(EISave, num3);
					ResetPlot(isConf, num3);
					ResetPlot(EIL, num3);
					ResetPlot(EIH, num3);
					ResetPlot(dir, num3);
					ResetPlot(signal, num3);
					ResetPlot(revLineTop, num3);
					ResetPlot(revLineBot, num3);
					ResetPlot(upSignal, num3);
					ResetPlot(dnSignal, num3);
					ResetPlot(Colorbars, num3);
					ResetPlot(EnhancedLines, num3);
					ResetPlotAndObj(glBuffer_Dot_longS, num3, buyLine);
					ResetPlotAndObj(glBuffer_Dot_shortS, num3, sellLine);
					ResetPlot(botLine, num3);
					ResetPlot(topLine, num3);
					ResetPlot(BUY, num3);
					ResetPlot(SELL, num3);
				}
				for (int num4 = num2; num4 >= 0; num4--)
				{
					Update(num4);
				}
				if (!pmIsOutside)
				{
					LargeArrowsIdentification_1();
					LargeArrowsIdentification_0();
					SmallArrowsIdentification();
				}
			}
		}

		private void SmallArrowsIdentification()
		{
			int num = CurrentBars[0];
			int num2 = MRO((Func<bool>)(() => upSignal[0] > 0.0), 1, CurrentBars[0]);
			if (num2 > 0)
			{
				int cb = num - num2;
				buyLine_Small = null;
				buyLine_Small = new BuyLineLevel_Small(this, cb, pmBuffer_upSignal_Enable, pmBuffer_upSignal_Enable);
				int num3 = num2;
				while (num3 >= 0 && !buyLine_Small.trIsBind)
				{
					double num4 = Lows[0][num3];
					double level = buyLine_Small.level;
					double level2 = num4 - (double)high_buffer_ticks_small * TickSize;
					bool flag = level > 0.0 && num4 <= level;
					if (num3 > 0 && (level <= 0.0 || !flag))
					{
						buyLine_Small.Trail(num3, level2);
					}
					else if (flag)
					{
						buyLine_Small.Bind(num3 + 1);
						break;
					}
					num3--;
				}
			}
			int num5 = MRO((Func<bool>)(() => dnSignal[0] > 0.0), 1, CurrentBar);
			if (num5 <= 0)
			{
				return;
			}
			int cb2 = num - num5;
			sellLine_Small = null;
			sellLine_Small = new SellLineLevel_Small(this, cb2, pmBuffer_dnSignal_Enable, pmBuffer_dnSignal_Enable);
			int num6 = num5;
			while (num6 >= 0 && !sellLine_Small.trIsBind)
			{
				double num7 = Highs[0][num6];
				double level3 = sellLine_Small.level;
				double level4 = num7 + (double)low_buffer_ticks_small * TickSize;
				bool flag2 = level3 > 0.0 && num7 >= level3;
				if (num6 > 0 && (level3 <= 0.0 || !flag2))
				{
					sellLine_Small.Trail(num6, level4);
				}
				else if (flag2)
				{
					sellLine_Small.Bind(num6 + 1);
					break;
				}
				num6--;
			}
		}

		private void LargeArrowsIdentification_1()
		{
			int num = 0;
			double num2 = Highs[0][0];
			double num3 = Lows[0][0];
			double num4 = Highs[0][1];
			double num5 = Lows[0][1];
			double num6 = glBuffer_Dot_longS[1];
			double num7 = glBuffer_Dot_shortS[1];
			bool flag = glBuffer_Dot_longS.IsValidDataPoint(1);
			bool flag2 = glBuffer_Dot_shortS.IsValidDataPoint(1);
			_ = longS[0];
			_ = shortS[0];
			bool flag3 = longS.IsValidDataPoint(0);
			bool flag4 = shortS.IsValidDataPoint(0);
			if (flag)
			{
				if (flag3)
				{
					ResetPlotAndObj(glBuffer_Dot_longS, num + 1, buyLine);
				}
				else
				{
					double num8 = num4 + (double)high_buffer_ticks * TickSize;
					if (num2 >= num8)
					{
						longS[num] = num6;
						ResetPlotAndObj(glBuffer_Dot_longS, num + 1, buyLine);
						buyLine.Bind(num + 1, num8);
						if (pmAlerts_long_Enable && pmBuffer_long_Enable && State != State.Historical && glLastAlertBar_longS < CurrentBar - num)
						{
							Logger(num + ", " + glLastAlertBar_longS + ", " + pmAlerts_long_File.ToString(), debug: true, 920, "LargeArrowsIdentification_1");
							glLastAlertBar_longS = CurrentBar - num;
							PlaySound(pmAlerts_long_File);
						}
					}
				}
			}
			if (!flag2)
			{
				return;
			}
			if (flag4)
			{
				ResetPlotAndObj(glBuffer_Dot_shortS, num + 1, sellLine);
				return;
			}
			double num9 = num5 - (double)low_buffer_ticks * TickSize;
			if (num3 <= num9)
			{
				shortS[num] = num7;
				ResetPlotAndObj(glBuffer_Dot_shortS, num + 1, sellLine);
				sellLine.Bind(num + 1, num9);
				if (pmAlerts_short_Enable && pmBuffer_short_Enable && State != State.Historical && glLastAlertBar_shortS < CurrentBar - num)
				{
					Logger(num + ", " + glLastAlertBar_shortS + ", " + pmAlerts_short_File.ToString(), debug: true, 960, "LargeArrowsIdentification_1");
					glLastAlertBar_shortS = CurrentBar - num;
					PlaySound(pmAlerts_short_File);
				}
			}
		}

		private void LargeArrowsIdentification_0()
		{
			int num = 0;
			double num2 = Highs[0][0];
			double num3 = Lows[0][0];
			double num4 = Highs[0][1];
			double num5 = Lows[0][1];
			double num6 = glBuffer_Dot_longS[0];
			double num7 = glBuffer_Dot_shortS[0];
			bool flag = glBuffer_Dot_longS.IsValidDataPoint(0);
			bool flag2 = glBuffer_Dot_shortS.IsValidDataPoint(0);
			bool flag3 = longS.IsValidDataPoint(0);
			bool flag4 = shortS.IsValidDataPoint(0);
			bool flag5 = upSignal.IsValidDataPoint(0);
			bool flag6 = dnSignal.IsValidDataPoint(0);
			if (flag3)
			{
				longS[num] = num3 - (double)pmBuffer_long_Offset * TickSize;
				if (pmBuffer_long_Enable)
				{
					if (flag5)
					{
						PlotBrushes[longS_plot][num] = pmBuffer_Long_WithDot_Stroke_stroke.Brush;
					}
					else
					{
						PlotBrushes[longS_plot][num] = pmBuffer_long_Stroke_stroke.Brush;
					}
				}
			}
			if (flag)
			{
				if (flag3)
				{
					longS[num] = num3 - (double)pmBuffer_long_Offset * TickSize;
					ResetPlotAndObj(glBuffer_Dot_longS, num, buyLine);
				}
				else
				{
					double num8 = num4 + (double)high_buffer_ticks * TickSize;
					if (num2 >= num8)
					{
						longS[num] = num6;
						if (pmBuffer_long_Enable)
						{
							if (flag5)
							{
								PlotBrushes[longS_plot][num] = pmBuffer_Long_WithDot_Stroke_stroke.Brush;
							}
							else
							{
								PlotBrushes[longS_plot][num] = pmBuffer_long_Stroke_stroke.Brush;
							}
						}
						ResetPlotAndObj(glBuffer_Dot_longS, num, buyLine);
						buyLine.Bind(num + 1, num8, isDraw: false);
						if (pmAlerts_long_Enable && pmBuffer_long_Enable && State != State.Historical && glLastAlertBar_longS < CurrentBar - num)
						{
							Logger(num + ", " + glLastAlertBar_longS + ", " + pmAlerts_long_File.ToString(), debug: true, 1048, "LargeArrowsIdentification_0");
							glLastAlertBar_longS = CurrentBar - num;
							PlaySound(pmAlerts_long_File);
						}
					}
				}
			}
			if (flag4)
			{
				shortS[num] = num2 + (double)pmBuffer_long_Offset * TickSize;
				if (pmBuffer_long_Enable)
				{
					if (flag6)
					{
						PlotBrushes[shortS_plot][num] = pmBuffer_Short_WithDot_Stroke_stroke.Brush;
					}
					else
					{
						PlotBrushes[shortS_plot][num] = pmBuffer_short_Stroke_stroke.Brush;
					}
				}
			}
			if (!flag2)
			{
				return;
			}
			if (flag4)
			{
				shortS[num] = num2 + (double)pmBuffer_long_Offset * TickSize;
				ResetPlotAndObj(glBuffer_Dot_shortS, num + 1, sellLine);
				return;
			}
			double num9 = num5 - (double)low_buffer_ticks * TickSize;
			if (!(num3 <= num9))
			{
				return;
			}
			shortS[num] = num7;
			if (pmBuffer_long_Enable)
			{
				if (flag6)
				{
					PlotBrushes[shortS_plot][num] = pmBuffer_Short_WithDot_Stroke_stroke.Brush;
				}
				else
				{
					PlotBrushes[shortS_plot][num] = pmBuffer_short_Stroke_stroke.Brush;
				}
			}
			ResetPlotAndObj(glBuffer_Dot_shortS, num, sellLine);
			sellLine.Bind(num + 1, num9, isDraw: false);
			if (pmAlerts_short_Enable && pmBuffer_short_Enable && State != State.Historical && glLastAlertBar_shortS < CurrentBar - num)
			{
				Logger(num + ", " + glLastAlertBar_shortS + ", " + pmAlerts_short_File.ToString(), debug: true, 1131, "LargeArrowsIdentification_0");
				glLastAlertBar_shortS = CurrentBar - num;
				PlaySound(pmAlerts_short_File);
			}
		}

		private void ActionHtfLines()
		{
			if (pmHTF_Enable && !pmIsOutside && BarsInProgress == 1)
			{
				glUltimateAIPro = UltimateAIProV3(BarsArray[1], pmHTF_Enable: false, pmHTF_Type, pmHTF_Period, pmHTF_DaysToLoad, pmBuffer_upSignal_Enable: true, 0, pmBuffer_dnSignal_Enable: true, 0, pmBuffer_long_Enable: true, 0, pmBuffer_short_Enable: true, 0, pmBuffer_longDot_Enable: true, pmBuffer_shortDot_Enable: true, pmBuffer_botLine_Enable: true, pmBuffer_topLine_Enable: true, high_buffer_ticks, low_buffer_ticks, high_buffer_ticks_small, low_buffer_ticks_small, pmAlerts_upSignal_Enable: false, "", pmAlerts_dnSignal_Enable: false, "", pmAlerts_long_Enable: false, "", pmAlerts_short_Enable: false, "", pmAlerts_BUY_Enable: false, "", pmAlerts_SELL_Enable: false, "", pmAlerts_longDot_Enable: false, "", pmAlerts_shortDot_Enable: false, "");
				glUltimateAIPro.pmIsOutside = true;
				int num = CurrentBars[0];
				int num2 = 0;
				double num3 = 0.0;
				int num4 = 0;
				double num5 = 0.0;
				DateTime dateTime = Times[0][0].Date.AddDays(-pmHTF_DaysToLoad);
				for (int i = 0; i < CurrentBars[1]; i++)
				{
					DateTime dateTime2 = Times[1][i];
					int bar = BarsArray[0].GetBar(dateTime2);
					int val = num - bar;
					val = Math.Max(val, 0);
					DateTime dateTime3 = Times[0][val];
					bool flag = glUltimateAIPro.topLine.IsValidDataPoint(i);
					string text = "HL#TOP#" + dateTime3.ToBinary();
					double num6 = glUltimateAIPro.topLine[i];
					if (flag)
					{
						if (num3 != num6)
						{
							num2++;
							num3 = num6;
						}
						topLine_htf[val] = num6;
						if (dateTime3 >= dateTime)
						{
							HorizontalLine horizontalLine = Draw.HorizontalLine(this, text, num6, Brushes.Aqua);
							horizontalLine.Stroke = htf_up_line_stroke;
						}
					}
					else if (topLine_htf.IsValidDataPoint(val))
					{
						topLine_htf.Reset(val);
						RemoveDrawObject(text);
					}
					bool flag2 = glUltimateAIPro.botLine.IsValidDataPoint(i);
					string text2 = "HL#BOT#" + dateTime3.ToBinary();
					double num7 = glUltimateAIPro.botLine[i];
					if (flag2)
					{
						if (num5 != num7)
						{
							num4++;
							num5 = num7;
						}
						botLine_htf[val] = num7;
						if (dateTime3 >= dateTime)
						{
							HorizontalLine horizontalLine2 = Draw.HorizontalLine(this, text2, num7, Brushes.RoyalBlue);
							horizontalLine2.Stroke = htf_dn_line_stroke;
						}
					}
					else if (botLine_htf.IsValidDataPoint(val))
					{
						botLine_htf.Reset(val);
						RemoveDrawObject(text2);
					}
					if (num4 > 2 && num2 > 2)
					{
						break;
					}
				}
			}
			if (pmHTF_Enable)
			{
				DateTime dateTime4 = Times[0][0];
				DateTime dateTime5 = Times[0][1];
				if (dateTime4.Date != dateTime5.Date)
				{
					ResetLineObjects(dateTime4.Date);
				}
			}
		}

		private void ResetLineObjects(DateTime date)
		{
			DateTime dateTime = date.AddDays(-pmHTF_DaysToLoad);
			int num = 0;
			List<IDrawingTool>.Enumerator enumerator = DrawObjects.ToList().GetEnumerator();
			while (enumerator.MoveNext())
			{
				IDrawingTool current = enumerator.Current;
				if (!(current is HorizontalLine) || !current.IsAttachedToNinjaScript || !current.Tag.StartsWith("HL#"))
				{
					continue;
				}
				string tag = current.Tag;
				string[] array = tag.Split('#');
				if (array.Length == 3)
				{
					DateTime dateTime2 = DateTime.FromBinary(Convert.ToInt64(array[2]));
					if (!(dateTime2 > dateTime))
					{
						num++;
						RemoveDrawObject(tag);
					}
				}
			}
			if (num > 0)
			{
				ForceRefresh();
			}
		}

		private void Update(int c)
		{
			int num = c + 1;
			int num2 = c + 2;
			if (CurrentBar <= c + trendLength)
			{
				return;
			}
			macd[c] = MovingAverage(Close, averageType, fastLength)[c] - MovingAverage(Close, averageType, slowLength)[c];
			macd1[c] = ((macd[c] >= macd[num]) ? 1 : 0);
			macdup[c] = ((SUM(macd1, sequentialLength)[c] == (double)sequentialLength && !(macd[c] < macd[c + trendLength])) ? 1 : 0);
			macd2[c] = ((macd[c] < macd[num]) ? 1 : 0);
			macddown[c] = ((SUM(macd2, sequentialLength)[c] == (double)sequentialLength && !(macd[c] >= macd[c + trendLength])) ? 1 : 0);
			if (macddown[num] != 0.0 && macd[c] > macd[num] && K[c] < (double)overbought)
			{
				upSignal[c] = Low[c] - (double)pmBuffer_upSignal_Offset * TickSize;
				if (pmAlerts_upSignal_Enable && pmBuffer_upSignal_Enable && State != State.Historical && c == 0 && glLastAlertBar_upSignal < CurrentBar - c)
				{
					Logger(c + ", " + glLastAlertBar_dnSignal + ", " + pmAlerts_upSignal_File.ToString(), debug: true, 1476, "Update");
					glLastAlertBar_upSignal = CurrentBar - c;
					PlaySound(pmAlerts_upSignal_File);
				}
			}
			if (macdup[num] != 0.0 && macd[c] < macd[num] && K[c] > (double)oversold)
			{
				dnSignal[c] = High[c] + (double)pmBuffer_dnSignal_Offset * TickSize;
				if (pmAlerts_dnSignal_Enable && pmBuffer_dnSignal_Enable && State != State.Historical && c == 0 && glLastAlertBar_dnSignal < CurrentBar - c)
				{
					Logger(c + ", " + glLastAlertBar_dnSignal + ", " + pmAlerts_dnSignal_File.ToString(), debug: true, 1499, "Update");
					glLastAlertBar_dnSignal = CurrentBar - c;
					PlaySound(pmAlerts_dnSignal_File);
				}
			}
			ISeries<double> close = Close;
			EMA eMA = EMA(close, 8);
			EMA eMA2 = EMA(close, 14);
			EMA eMA3 = EMA(close, 21);
			buy[c] = ((eMA[c] > eMA2[c] && eMA2[c] > eMA3[c] && !(Low[c] <= eMA[c])) ? 1 : 0);
			bool flag = eMA[c] <= eMA2[c];
			bool flag2 = buy[num] == 0.0 && buy[c] != 0.0;
			buysignal[c] = ((flag2 && !flag) ? 1.0 : ((buysignal[num] != 1.0 || !flag) ? buysignal[num] : 0.0));
			if (buysignal[num] == 0.0)
			{
				_ = buysignal[c];
			}
			if (buysignal[num] != 0.0)
			{
				_ = buysignal[c];
			}
			sell[c] = ((eMA[c] < eMA2[c] && eMA2[c] < eMA3[c] && !(High[c] >= eMA[c])) ? 1 : 0);
			bool flag3 = eMA[c] >= eMA2[c];
			bool flag4 = sell[num] == 0.0 && sell[c] != 0.0;
			sellsignal[c] = ((flag4 && !flag3) ? 1.0 : ((sellsignal[num] != 1.0 || !flag3) ? sellsignal[num] : 0.0));
			if (sellsignal[num] == 0.0)
			{
				_ = sellsignal[c];
			}
			Colorbars[c] = ((buysignal[c] == 1.0) ? 1 : ((sellsignal[c] == 1.0) ? 2 : ((buysignal[c] == 0.0 || sellsignal[c] == 0.0) ? 3 : 0)));
			double num3 = EI.ZZ[c];
			double num4 = ((Close[c] * percentamount / 100.0 > Math.Max((revAmount < atrreversal * ATR(atrlength)[c]) ? 1 : 0, revAmount)) ? (Close[c] * percentamount / 100.0) : ((revAmount < atrreversal * ATR(atrlength)[c]) ? (atrreversal * ATR(atrlength)[c]) : revAmount));
			EISave[c] = ((num3 != 0.0) ? EI[c] : EISave[num]);
			double num5 = ((EISave[c] == Price(priceH)[c]) ? Price(priceH)[c] : Price(priceL)[c]) - EISave[num];
			bool flag5 = num5 >= 0.0;
			isConf[c] = ((Math.Abs(num5) >= num4 || (num3 == 0.0 && isConf[num] != 0.0)) ? 1 : 0);
			int num6 = (flag5 ? 1 : 0);
			if (num6 <= 1 && EI[c] != 0.0)
			{
				EnhancedLines[c] = EI[c];
			}
			EIL[c] = ((num3 == 0.0 || flag5) ? EIL[num] : Price(priceL)[c]);
			EIH[c] = ((num3 == 0.0 || !flag5) ? EIH[num] : Price(priceH)[c]);
			dir[c] = ((EIL[c] != EIL[num] || (Price(priceL)[c] == EIL[num] && Price(priceL)[c] == EISave[c])) ? 1.0 : ((EIH[c] != EIH[num] || (Price(priceH)[c] == EIH[num] && Price(priceH)[c] == EISave[c])) ? (-1.0) : dir[num]));
			signal[c] = ((dir[c] > 0.0 && !(Price(priceL)[c] <= EIL[c])) ? ((signal[num] <= 0.0) ? 1.0 : signal[num]) : ((!(dir[c] < 0.0) || Price(priceH)[c] >= EIH[c]) ? signal[num] : ((signal[num] >= 0.0) ? (-1.0) : signal[num])));
			bool flag6 = true;
			bool flag7 = signal[c] > 0.0 && signal[num] <= 0.0;
			bool flag8 = flag6 && signal[c] < 0.0 && signal[num] >= 0.0;
			int num7 = CurrentBar - 10;
			bool flag9 = longS.IsValidDataPoint(Math.Max(c - 1, 0));
			bool flag10 = longS.IsValidDataPoint(c);
			bool flag11 = shortS.IsValidDataPoint(Math.Max(c - 1, 0));
			bool flag12 = shortS.IsValidDataPoint(c);
			if (num7 > 0 && flag7 && Colorbars[c] != 3.0 && !flag9 && !flag10)
			{
				glBuffer_Dot_longS[c] = Low[c] - (double)pmBuffer_long_Offset * TickSize;
				if (c == 0)
				{
					double level = Highs[0][c + 1] + (double)high_buffer_ticks * TickSize;
					buyLine.Trail(c + 1, level, isDraw: false);
				}
				else
				{
					double level2 = Highs[0][c] + (double)high_buffer_ticks * TickSize;
					buyLine.Trail(c, level2);
				}
				if (pmAlerts_longDot_Enable && pmBuffer_longDot_Enable && State != State.Historical && c == 0 && glLastAlertBar_longDot < CurrentBar - c)
				{
					Logger(c + ", " + glLastAlertBar_longDot + ", " + pmAlerts_longDot_File.ToString(), debug: true, 1777, "Update");
					glLastAlertBar_longDot = CurrentBar - c;
					PlaySound(pmAlerts_longDot_File);
				}
			}
			if (num7 > 0 && flag8 && Colorbars[c] != 3.0 && !flag11 && !flag12)
			{
				glBuffer_Dot_shortS[c] = High[c] + (double)pmBuffer_short_Offset * TickSize;
				if (c == 0)
				{
					double level3 = Lows[0][c + 1] - (double)low_buffer_ticks * TickSize;
					sellLine.Trail(c + 1, level3, isDraw: false);
				}
				else
				{
					double level4 = Lows[0][c] - (double)low_buffer_ticks * TickSize;
					sellLine.Trail(c, level4);
				}
				if (pmAlerts_shortDot_Enable && pmBuffer_shortDot_Enable && State != State.Historical && c == 0 && glLastAlertBar_shortDot < CurrentBar - c)
				{
					Logger(c + ", " + glLastAlertBar_shortDot + ", " + pmAlerts_shortDot_File.ToString(), debug: true, 1818, "Update");
					glLastAlertBar_shortDot = CurrentBar - c;
					PlaySound(pmAlerts_shortDot_File);
				}
			}
			if (num7 != 0 && flag8)
			{
				revLineBot[c] = 0.0;
				revLineTop[c] = High[num];
			}
			else if (num7 != 0 && flag7)
			{
				revLineTop[c] = 0.0;
				revLineBot[c] = Low[num];
			}
			else if (revLineBot[num] != 0.0 && (Colorbars[num2] == 2.0 || Colorbars[num] == 2.0))
			{
				revLineBot[c] = revLineBot[num];
				revLineTop[c] = 0.0;
			}
			else if (revLineTop[num] != 0.0 && (Colorbars[num2] == 1.0 || Colorbars[num] == 1.0))
			{
				revLineTop[c] = revLineTop[num];
				revLineBot[c] = 0.0;
			}
			else
			{
				revLineTop[c] = 0.0;
				revLineBot[c] = 0.0;
			}
			if (revLineBot[c] != 0.0)
			{
				botLine[num] = revLineBot[c];
			}
			if (revLineTop[c] != 0.0)
			{
				topLine[num] = revLineTop[c];
			}
			if (num7 != 0 && flag7 && Colorbars[c] == 3.0 && !glBuffer_Dot_longS.IsValidDataPoint(c) && !flag10)
			{
				glBuffer_Dot_longS[c] = Low[c] - (double)pmBuffer_long_Offset * TickSize;
				if (c == 0)
				{
					double level5 = Highs[0][c + 1] + (double)high_buffer_ticks * TickSize;
					buyLine.Trail(c + 1, level5, isDraw: false);
				}
				else
				{
					double level6 = Highs[0][c] + (double)high_buffer_ticks * TickSize;
					buyLine.Trail(c, level6);
				}
				if (pmAlerts_longDot_Enable && pmBuffer_longDot_Enable && State != State.Historical && c == 0 && glLastAlertBar_longDot < CurrentBar - c)
				{
					Logger(c + ", " + glLastAlertBar_longDot + ", " + pmAlerts_longDot_File.ToString(), debug: true, 1925, "Update");
					glLastAlertBar_longDot = CurrentBar - c;
					PlaySound(pmAlerts_longDot_File);
				}
			}
			if (num7 != 0 && flag8 && Colorbars[c] == 3.0 && !glBuffer_Dot_shortS.IsValidDataPoint(c) && !flag12)
			{
				glBuffer_Dot_shortS[c] = High[c] + (double)pmBuffer_short_Offset * TickSize;
				if (c == 0)
				{
					double level7 = Lows[0][c + 1] - (double)low_buffer_ticks * TickSize;
					sellLine.Trail(c + 1, level7, isDraw: false);
				}
				else
				{
					double level8 = Lows[0][c] - (double)low_buffer_ticks * TickSize;
					sellLine.Trail(c, level8);
				}
				if (pmAlerts_shortDot_Enable && pmBuffer_shortDot_Enable && State != State.Historical && c == 0 && glLastAlertBar_shortDot < CurrentBar - c)
				{
					Logger(c + ", " + glLastAlertBar_shortDot + ", " + pmAlerts_shortDot_File.ToString(), debug: true, 1999, "Update");
					glLastAlertBar_shortDot = CurrentBar - c;
					PlaySound(pmAlerts_shortDot_File);
				}
			}
			bool flag13 = glBuffer_Dot_longS.IsValidDataPoint(c);
			bool flag14 = upSignal.IsValidDataPoint(c);
			if (pmBuffer_longDot_Enable && flag13)
			{
				if (flag14)
				{
					PlotBrushes[Dot_longS][c] = pmBuffer_Long_WithDot_Stroke_stroke.Brush;
				}
				else
				{
					PlotBrushes[Dot_longS][c] = pmBuffer_longDot_Stroke_stroke.Brush;
				}
			}
			bool flag15 = glBuffer_Dot_shortS.IsValidDataPoint(c);
			bool flag16 = dnSignal.IsValidDataPoint(c);
			if (pmBuffer_shortDot_Enable && flag15)
			{
				if (flag16)
				{
					PlotBrushes[Dot_shortS][c] = pmBuffer_Short_WithDot_Stroke_stroke.Brush;
				}
				else
				{
					PlotBrushes[Dot_shortS][c] = pmBuffer_shortDot_Stroke_stroke.Brush;
				}
			}
		}

		private void ResetPlot(Series<double> series, int index = 0)
		{
			series[index] = 0.0;
			series.Reset(index);
		}

		private void ResetPlotAndObj(Series<double> series, int index, LineLevel_Large inst)
		{
			series[index] = 0.0;
			series.Reset(index);
			inst.DestroyTemp(index);
		}

		private void ResetPlot(Series<int> series, int index = 0)
		{
			series[index] = 0;
			series.Reset(index);
		}

		private void ResetPlot(Series<bool> series, int index = 0)
		{
			series[index] = false;
			series.Reset(index);
		}

		private ISeries<double> Price(UltimateAIPro_Price averageType, int input = 0)
		{
			switch (averageType)
			{
			default:
				return Closes[input];
			case UltimateAIPro_Price.OHLC4:
				return Weighteds[input];
			case UltimateAIPro_Price.HLC3:
				return Typicals[input];
			case UltimateAIPro_Price.HL2:
				return Medians[input];
			case UltimateAIPro_Price.Close:
				return Closes[input];
			case UltimateAIPro_Price.Open:
				return Opens[input];
			case UltimateAIPro_Price.Low:
				return Lows[input];
			case UltimateAIPro_Price.High:
				return Highs[input];
			}
		}

		private ISeries<double> MovingAverage(ISeries<double> input, UltimateAIPro_AverageType averageType, int length)
		{
			switch (averageType)
			{
			default:
				return SMA(Input, length);
			case UltimateAIPro_AverageType.HULL:
				return HMA(input, length);
			case UltimateAIPro_AverageType.WILDERS:
				return WilderMA1v0(input, length);
			case UltimateAIPro_AverageType.WEIGHTED:
				return WMA(input, length);
			case UltimateAIPro_AverageType.EXPONENTIAL:
				return EMA(input, length);
			case UltimateAIPro_AverageType.SIMPLE:
				return SMA(input, length);
			}
		}

		public void Logger(string msg, bool debug = true, [CallerLineNumber] int callerLineNumber = 0, [CallerMemberName] string caller = null)
		{
			string text = "";
			string name = ((object)this).GetType().Name;
			string text2 = ((name == Name) ? name : (name + " :: " + ((object)this).ToString()));
			string text3 = ((Instrument == null) ? "NI" : Instrument.FullName);
			string text4 = ((object)State).ToString();
			int managedThreadId = Thread.CurrentThread.ManagedThreadId;
			string text5 = "(L:" + Close[0] + ")";
			text = ((!debug) ? $"{text2} Logger: {text3} {DateTime.Now.ToString()} {msg}" : string.Format("{0} Logger: {1} {2} {3} ({4}:{5}) {6}", text2 + ":" + glID + ":" + managedThreadId, text3, text4, text5 + " " + DateTime.Now.ToString(), caller, callerLineNumber, msg));
			if (State != State.Historical)
			{
				NinjaScript.Log(text, (LogLevel)1);
			}
			Print((object)text);
		}
		#endregion

	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private UltimateAIProV3[] cacheUltimateAIProV3;
		public UltimateAIProV3 UltimateAIProV3(bool pmHTF_Enable, EnumHtfType_UltimateAIPro pmHTF_Type, int pmHTF_Period, int pmHTF_DaysToLoad, bool pmBuffer_upSignal_Enable, int pmBuffer_upSignal_Offset, bool pmBuffer_dnSignal_Enable, int pmBuffer_dnSignal_Offset, bool pmBuffer_long_Enable, int pmBuffer_long_Offset, bool pmBuffer_short_Enable, int pmBuffer_short_Offset, bool pmBuffer_longDot_Enable, bool pmBuffer_shortDot_Enable, bool pmBuffer_botLine_Enable, bool pmBuffer_topLine_Enable, int high_buffer_ticks, int low_buffer_ticks, int high_buffer_ticks_small, int low_buffer_ticks_small, bool pmAlerts_upSignal_Enable, string pmAlerts_upSignal_File, bool pmAlerts_dnSignal_Enable, string pmAlerts_dnSignal_File, bool pmAlerts_long_Enable, string pmAlerts_long_File, bool pmAlerts_short_Enable, string pmAlerts_short_File, bool pmAlerts_BUY_Enable, string pmAlerts_BUY_File, bool pmAlerts_SELL_Enable, string pmAlerts_SELL_File, bool pmAlerts_longDot_Enable, string pmAlerts_longDot_File, bool pmAlerts_shortDot_Enable, string pmAlerts_shortDot_File)
		{
			return UltimateAIProV3(Input, pmHTF_Enable, pmHTF_Type, pmHTF_Period, pmHTF_DaysToLoad, pmBuffer_upSignal_Enable, pmBuffer_upSignal_Offset, pmBuffer_dnSignal_Enable, pmBuffer_dnSignal_Offset, pmBuffer_long_Enable, pmBuffer_long_Offset, pmBuffer_short_Enable, pmBuffer_short_Offset, pmBuffer_longDot_Enable, pmBuffer_shortDot_Enable, pmBuffer_botLine_Enable, pmBuffer_topLine_Enable, high_buffer_ticks, low_buffer_ticks, high_buffer_ticks_small, low_buffer_ticks_small, pmAlerts_upSignal_Enable, pmAlerts_upSignal_File, pmAlerts_dnSignal_Enable, pmAlerts_dnSignal_File, pmAlerts_long_Enable, pmAlerts_long_File, pmAlerts_short_Enable, pmAlerts_short_File, pmAlerts_BUY_Enable, pmAlerts_BUY_File, pmAlerts_SELL_Enable, pmAlerts_SELL_File, pmAlerts_longDot_Enable, pmAlerts_longDot_File, pmAlerts_shortDot_Enable, pmAlerts_shortDot_File);
		}

		public UltimateAIProV3 UltimateAIProV3(ISeries<double> input, bool pmHTF_Enable, EnumHtfType_UltimateAIPro pmHTF_Type, int pmHTF_Period, int pmHTF_DaysToLoad, bool pmBuffer_upSignal_Enable, int pmBuffer_upSignal_Offset, bool pmBuffer_dnSignal_Enable, int pmBuffer_dnSignal_Offset, bool pmBuffer_long_Enable, int pmBuffer_long_Offset, bool pmBuffer_short_Enable, int pmBuffer_short_Offset, bool pmBuffer_longDot_Enable, bool pmBuffer_shortDot_Enable, bool pmBuffer_botLine_Enable, bool pmBuffer_topLine_Enable, int high_buffer_ticks, int low_buffer_ticks, int high_buffer_ticks_small, int low_buffer_ticks_small, bool pmAlerts_upSignal_Enable, string pmAlerts_upSignal_File, bool pmAlerts_dnSignal_Enable, string pmAlerts_dnSignal_File, bool pmAlerts_long_Enable, string pmAlerts_long_File, bool pmAlerts_short_Enable, string pmAlerts_short_File, bool pmAlerts_BUY_Enable, string pmAlerts_BUY_File, bool pmAlerts_SELL_Enable, string pmAlerts_SELL_File, bool pmAlerts_longDot_Enable, string pmAlerts_longDot_File, bool pmAlerts_shortDot_Enable, string pmAlerts_shortDot_File)
		{
			if (cacheUltimateAIProV3 != null)
				for (int idx = 0; idx < cacheUltimateAIProV3.Length; idx++)
					if (cacheUltimateAIProV3[idx] != null && cacheUltimateAIProV3[idx].pmHTF_Enable == pmHTF_Enable && cacheUltimateAIProV3[idx].pmHTF_Type == pmHTF_Type && cacheUltimateAIProV3[idx].pmHTF_Period == pmHTF_Period && cacheUltimateAIProV3[idx].pmHTF_DaysToLoad == pmHTF_DaysToLoad && cacheUltimateAIProV3[idx].pmBuffer_upSignal_Enable == pmBuffer_upSignal_Enable && cacheUltimateAIProV3[idx].pmBuffer_upSignal_Offset == pmBuffer_upSignal_Offset && cacheUltimateAIProV3[idx].pmBuffer_dnSignal_Enable == pmBuffer_dnSignal_Enable && cacheUltimateAIProV3[idx].pmBuffer_dnSignal_Offset == pmBuffer_dnSignal_Offset && cacheUltimateAIProV3[idx].pmBuffer_long_Enable == pmBuffer_long_Enable && cacheUltimateAIProV3[idx].pmBuffer_long_Offset == pmBuffer_long_Offset && cacheUltimateAIProV3[idx].pmBuffer_short_Enable == pmBuffer_short_Enable && cacheUltimateAIProV3[idx].pmBuffer_short_Offset == pmBuffer_short_Offset && cacheUltimateAIProV3[idx].pmBuffer_longDot_Enable == pmBuffer_longDot_Enable && cacheUltimateAIProV3[idx].pmBuffer_shortDot_Enable == pmBuffer_shortDot_Enable && cacheUltimateAIProV3[idx].pmBuffer_botLine_Enable == pmBuffer_botLine_Enable && cacheUltimateAIProV3[idx].pmBuffer_topLine_Enable == pmBuffer_topLine_Enable && cacheUltimateAIProV3[idx].high_buffer_ticks == high_buffer_ticks && cacheUltimateAIProV3[idx].low_buffer_ticks == low_buffer_ticks && cacheUltimateAIProV3[idx].high_buffer_ticks_small == high_buffer_ticks_small && cacheUltimateAIProV3[idx].low_buffer_ticks_small == low_buffer_ticks_small && cacheUltimateAIProV3[idx].pmAlerts_upSignal_Enable == pmAlerts_upSignal_Enable && cacheUltimateAIProV3[idx].pmAlerts_upSignal_File == pmAlerts_upSignal_File && cacheUltimateAIProV3[idx].pmAlerts_dnSignal_Enable == pmAlerts_dnSignal_Enable && cacheUltimateAIProV3[idx].pmAlerts_dnSignal_File == pmAlerts_dnSignal_File && cacheUltimateAIProV3[idx].pmAlerts_long_Enable == pmAlerts_long_Enable && cacheUltimateAIProV3[idx].pmAlerts_long_File == pmAlerts_long_File && cacheUltimateAIProV3[idx].pmAlerts_short_Enable == pmAlerts_short_Enable && cacheUltimateAIProV3[idx].pmAlerts_short_File == pmAlerts_short_File && cacheUltimateAIProV3[idx].pmAlerts_BUY_Enable == pmAlerts_BUY_Enable && cacheUltimateAIProV3[idx].pmAlerts_BUY_File == pmAlerts_BUY_File && cacheUltimateAIProV3[idx].pmAlerts_SELL_Enable == pmAlerts_SELL_Enable && cacheUltimateAIProV3[idx].pmAlerts_SELL_File == pmAlerts_SELL_File && cacheUltimateAIProV3[idx].pmAlerts_longDot_Enable == pmAlerts_longDot_Enable && cacheUltimateAIProV3[idx].pmAlerts_longDot_File == pmAlerts_longDot_File && cacheUltimateAIProV3[idx].pmAlerts_shortDot_Enable == pmAlerts_shortDot_Enable && cacheUltimateAIProV3[idx].pmAlerts_shortDot_File == pmAlerts_shortDot_File && cacheUltimateAIProV3[idx].EqualsInput(input))
						return cacheUltimateAIProV3[idx];
			return CacheIndicator<UltimateAIProV3>(new UltimateAIProV3(){ pmHTF_Enable = pmHTF_Enable, pmHTF_Type = pmHTF_Type, pmHTF_Period = pmHTF_Period, pmHTF_DaysToLoad = pmHTF_DaysToLoad, pmBuffer_upSignal_Enable = pmBuffer_upSignal_Enable, pmBuffer_upSignal_Offset = pmBuffer_upSignal_Offset, pmBuffer_dnSignal_Enable = pmBuffer_dnSignal_Enable, pmBuffer_dnSignal_Offset = pmBuffer_dnSignal_Offset, pmBuffer_long_Enable = pmBuffer_long_Enable, pmBuffer_long_Offset = pmBuffer_long_Offset, pmBuffer_short_Enable = pmBuffer_short_Enable, pmBuffer_short_Offset = pmBuffer_short_Offset, pmBuffer_longDot_Enable = pmBuffer_longDot_Enable, pmBuffer_shortDot_Enable = pmBuffer_shortDot_Enable, pmBuffer_botLine_Enable = pmBuffer_botLine_Enable, pmBuffer_topLine_Enable = pmBuffer_topLine_Enable, high_buffer_ticks = high_buffer_ticks, low_buffer_ticks = low_buffer_ticks, high_buffer_ticks_small = high_buffer_ticks_small, low_buffer_ticks_small = low_buffer_ticks_small, pmAlerts_upSignal_Enable = pmAlerts_upSignal_Enable, pmAlerts_upSignal_File = pmAlerts_upSignal_File, pmAlerts_dnSignal_Enable = pmAlerts_dnSignal_Enable, pmAlerts_dnSignal_File = pmAlerts_dnSignal_File, pmAlerts_long_Enable = pmAlerts_long_Enable, pmAlerts_long_File = pmAlerts_long_File, pmAlerts_short_Enable = pmAlerts_short_Enable, pmAlerts_short_File = pmAlerts_short_File, pmAlerts_BUY_Enable = pmAlerts_BUY_Enable, pmAlerts_BUY_File = pmAlerts_BUY_File, pmAlerts_SELL_Enable = pmAlerts_SELL_Enable, pmAlerts_SELL_File = pmAlerts_SELL_File, pmAlerts_longDot_Enable = pmAlerts_longDot_Enable, pmAlerts_longDot_File = pmAlerts_longDot_File, pmAlerts_shortDot_Enable = pmAlerts_shortDot_Enable, pmAlerts_shortDot_File = pmAlerts_shortDot_File }, input, ref cacheUltimateAIProV3);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.UltimateAIProV3 UltimateAIProV3(bool pmHTF_Enable, EnumHtfType_UltimateAIPro pmHTF_Type, int pmHTF_Period, int pmHTF_DaysToLoad, bool pmBuffer_upSignal_Enable, int pmBuffer_upSignal_Offset, bool pmBuffer_dnSignal_Enable, int pmBuffer_dnSignal_Offset, bool pmBuffer_long_Enable, int pmBuffer_long_Offset, bool pmBuffer_short_Enable, int pmBuffer_short_Offset, bool pmBuffer_longDot_Enable, bool pmBuffer_shortDot_Enable, bool pmBuffer_botLine_Enable, bool pmBuffer_topLine_Enable, int high_buffer_ticks, int low_buffer_ticks, int high_buffer_ticks_small, int low_buffer_ticks_small, bool pmAlerts_upSignal_Enable, string pmAlerts_upSignal_File, bool pmAlerts_dnSignal_Enable, string pmAlerts_dnSignal_File, bool pmAlerts_long_Enable, string pmAlerts_long_File, bool pmAlerts_short_Enable, string pmAlerts_short_File, bool pmAlerts_BUY_Enable, string pmAlerts_BUY_File, bool pmAlerts_SELL_Enable, string pmAlerts_SELL_File, bool pmAlerts_longDot_Enable, string pmAlerts_longDot_File, bool pmAlerts_shortDot_Enable, string pmAlerts_shortDot_File)
		{
			return indicator.UltimateAIProV3(Input, pmHTF_Enable, pmHTF_Type, pmHTF_Period, pmHTF_DaysToLoad, pmBuffer_upSignal_Enable, pmBuffer_upSignal_Offset, pmBuffer_dnSignal_Enable, pmBuffer_dnSignal_Offset, pmBuffer_long_Enable, pmBuffer_long_Offset, pmBuffer_short_Enable, pmBuffer_short_Offset, pmBuffer_longDot_Enable, pmBuffer_shortDot_Enable, pmBuffer_botLine_Enable, pmBuffer_topLine_Enable, high_buffer_ticks, low_buffer_ticks, high_buffer_ticks_small, low_buffer_ticks_small, pmAlerts_upSignal_Enable, pmAlerts_upSignal_File, pmAlerts_dnSignal_Enable, pmAlerts_dnSignal_File, pmAlerts_long_Enable, pmAlerts_long_File, pmAlerts_short_Enable, pmAlerts_short_File, pmAlerts_BUY_Enable, pmAlerts_BUY_File, pmAlerts_SELL_Enable, pmAlerts_SELL_File, pmAlerts_longDot_Enable, pmAlerts_longDot_File, pmAlerts_shortDot_Enable, pmAlerts_shortDot_File);
		}

		public Indicators.UltimateAIProV3 UltimateAIProV3(ISeries<double> input , bool pmHTF_Enable, EnumHtfType_UltimateAIPro pmHTF_Type, int pmHTF_Period, int pmHTF_DaysToLoad, bool pmBuffer_upSignal_Enable, int pmBuffer_upSignal_Offset, bool pmBuffer_dnSignal_Enable, int pmBuffer_dnSignal_Offset, bool pmBuffer_long_Enable, int pmBuffer_long_Offset, bool pmBuffer_short_Enable, int pmBuffer_short_Offset, bool pmBuffer_longDot_Enable, bool pmBuffer_shortDot_Enable, bool pmBuffer_botLine_Enable, bool pmBuffer_topLine_Enable, int high_buffer_ticks, int low_buffer_ticks, int high_buffer_ticks_small, int low_buffer_ticks_small, bool pmAlerts_upSignal_Enable, string pmAlerts_upSignal_File, bool pmAlerts_dnSignal_Enable, string pmAlerts_dnSignal_File, bool pmAlerts_long_Enable, string pmAlerts_long_File, bool pmAlerts_short_Enable, string pmAlerts_short_File, bool pmAlerts_BUY_Enable, string pmAlerts_BUY_File, bool pmAlerts_SELL_Enable, string pmAlerts_SELL_File, bool pmAlerts_longDot_Enable, string pmAlerts_longDot_File, bool pmAlerts_shortDot_Enable, string pmAlerts_shortDot_File)
		{
			return indicator.UltimateAIProV3(input, pmHTF_Enable, pmHTF_Type, pmHTF_Period, pmHTF_DaysToLoad, pmBuffer_upSignal_Enable, pmBuffer_upSignal_Offset, pmBuffer_dnSignal_Enable, pmBuffer_dnSignal_Offset, pmBuffer_long_Enable, pmBuffer_long_Offset, pmBuffer_short_Enable, pmBuffer_short_Offset, pmBuffer_longDot_Enable, pmBuffer_shortDot_Enable, pmBuffer_botLine_Enable, pmBuffer_topLine_Enable, high_buffer_ticks, low_buffer_ticks, high_buffer_ticks_small, low_buffer_ticks_small, pmAlerts_upSignal_Enable, pmAlerts_upSignal_File, pmAlerts_dnSignal_Enable, pmAlerts_dnSignal_File, pmAlerts_long_Enable, pmAlerts_long_File, pmAlerts_short_Enable, pmAlerts_short_File, pmAlerts_BUY_Enable, pmAlerts_BUY_File, pmAlerts_SELL_Enable, pmAlerts_SELL_File, pmAlerts_longDot_Enable, pmAlerts_longDot_File, pmAlerts_shortDot_Enable, pmAlerts_shortDot_File);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.UltimateAIProV3 UltimateAIProV3(bool pmHTF_Enable, EnumHtfType_UltimateAIPro pmHTF_Type, int pmHTF_Period, int pmHTF_DaysToLoad, bool pmBuffer_upSignal_Enable, int pmBuffer_upSignal_Offset, bool pmBuffer_dnSignal_Enable, int pmBuffer_dnSignal_Offset, bool pmBuffer_long_Enable, int pmBuffer_long_Offset, bool pmBuffer_short_Enable, int pmBuffer_short_Offset, bool pmBuffer_longDot_Enable, bool pmBuffer_shortDot_Enable, bool pmBuffer_botLine_Enable, bool pmBuffer_topLine_Enable, int high_buffer_ticks, int low_buffer_ticks, int high_buffer_ticks_small, int low_buffer_ticks_small, bool pmAlerts_upSignal_Enable, string pmAlerts_upSignal_File, bool pmAlerts_dnSignal_Enable, string pmAlerts_dnSignal_File, bool pmAlerts_long_Enable, string pmAlerts_long_File, bool pmAlerts_short_Enable, string pmAlerts_short_File, bool pmAlerts_BUY_Enable, string pmAlerts_BUY_File, bool pmAlerts_SELL_Enable, string pmAlerts_SELL_File, bool pmAlerts_longDot_Enable, string pmAlerts_longDot_File, bool pmAlerts_shortDot_Enable, string pmAlerts_shortDot_File)
		{
			return indicator.UltimateAIProV3(Input, pmHTF_Enable, pmHTF_Type, pmHTF_Period, pmHTF_DaysToLoad, pmBuffer_upSignal_Enable, pmBuffer_upSignal_Offset, pmBuffer_dnSignal_Enable, pmBuffer_dnSignal_Offset, pmBuffer_long_Enable, pmBuffer_long_Offset, pmBuffer_short_Enable, pmBuffer_short_Offset, pmBuffer_longDot_Enable, pmBuffer_shortDot_Enable, pmBuffer_botLine_Enable, pmBuffer_topLine_Enable, high_buffer_ticks, low_buffer_ticks, high_buffer_ticks_small, low_buffer_ticks_small, pmAlerts_upSignal_Enable, pmAlerts_upSignal_File, pmAlerts_dnSignal_Enable, pmAlerts_dnSignal_File, pmAlerts_long_Enable, pmAlerts_long_File, pmAlerts_short_Enable, pmAlerts_short_File, pmAlerts_BUY_Enable, pmAlerts_BUY_File, pmAlerts_SELL_Enable, pmAlerts_SELL_File, pmAlerts_longDot_Enable, pmAlerts_longDot_File, pmAlerts_shortDot_Enable, pmAlerts_shortDot_File);
		}

		public Indicators.UltimateAIProV3 UltimateAIProV3(ISeries<double> input , bool pmHTF_Enable, EnumHtfType_UltimateAIPro pmHTF_Type, int pmHTF_Period, int pmHTF_DaysToLoad, bool pmBuffer_upSignal_Enable, int pmBuffer_upSignal_Offset, bool pmBuffer_dnSignal_Enable, int pmBuffer_dnSignal_Offset, bool pmBuffer_long_Enable, int pmBuffer_long_Offset, bool pmBuffer_short_Enable, int pmBuffer_short_Offset, bool pmBuffer_longDot_Enable, bool pmBuffer_shortDot_Enable, bool pmBuffer_botLine_Enable, bool pmBuffer_topLine_Enable, int high_buffer_ticks, int low_buffer_ticks, int high_buffer_ticks_small, int low_buffer_ticks_small, bool pmAlerts_upSignal_Enable, string pmAlerts_upSignal_File, bool pmAlerts_dnSignal_Enable, string pmAlerts_dnSignal_File, bool pmAlerts_long_Enable, string pmAlerts_long_File, bool pmAlerts_short_Enable, string pmAlerts_short_File, bool pmAlerts_BUY_Enable, string pmAlerts_BUY_File, bool pmAlerts_SELL_Enable, string pmAlerts_SELL_File, bool pmAlerts_longDot_Enable, string pmAlerts_longDot_File, bool pmAlerts_shortDot_Enable, string pmAlerts_shortDot_File)
		{
			return indicator.UltimateAIProV3(input, pmHTF_Enable, pmHTF_Type, pmHTF_Period, pmHTF_DaysToLoad, pmBuffer_upSignal_Enable, pmBuffer_upSignal_Offset, pmBuffer_dnSignal_Enable, pmBuffer_dnSignal_Offset, pmBuffer_long_Enable, pmBuffer_long_Offset, pmBuffer_short_Enable, pmBuffer_short_Offset, pmBuffer_longDot_Enable, pmBuffer_shortDot_Enable, pmBuffer_botLine_Enable, pmBuffer_topLine_Enable, high_buffer_ticks, low_buffer_ticks, high_buffer_ticks_small, low_buffer_ticks_small, pmAlerts_upSignal_Enable, pmAlerts_upSignal_File, pmAlerts_dnSignal_Enable, pmAlerts_dnSignal_File, pmAlerts_long_Enable, pmAlerts_long_File, pmAlerts_short_Enable, pmAlerts_short_File, pmAlerts_BUY_Enable, pmAlerts_BUY_File, pmAlerts_SELL_Enable, pmAlerts_SELL_File, pmAlerts_longDot_Enable, pmAlerts_longDot_File, pmAlerts_shortDot_Enable, pmAlerts_shortDot_File);
		}
	}
}

#endregion
