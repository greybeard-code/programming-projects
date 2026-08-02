using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using System.Xml.Serialization;
using System;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.Core;
using NinjaTrader.Custom;
using NinjaTrader.Data;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui;
using NinjaTrader.NinjaScript;
using SharpDX.Direct2D1;
using SharpDX;
namespace UltimateSignalsNamespace
{
	public enum MaMode
	{
		SMA,
		EMA,
		HMA,
		WMA,
		Wilders
	}
	public enum PriceMode
	{
		Open,
		High,
		Low,
		Close,
		Median,
		Typical,
		Weighted
	}
	public enum Method
	{
		average,
		high_low
	}
	public abstract class IndicatorEngine
	{
		protected State State;
		protected NinjaScriptBase script;
		public string Name;
		public string Description;
		public bool DisplayInDataBox;
		public bool DrawOnPricePanel;
		public bool IsSuspendedWhileInactive;
		public bool IsOverlay;
		public bool PaintPriceMarkers;
		public Calculate Calculate;
		public MaximumBarsLookBack MaximumBarsLookBack;
		[CompilerGenerated]
		private List<Series<double>> tBM_;
		public ISeries<double> Input;
		protected ISeries<double> Open;
		protected ISeries<double> High;
		protected ISeries<double> Low;
		protected ISeries<double> Close;
		protected ISeries<double> Volume;
		public List<Series<double>> Values
		{
			[CompilerGenerated]
			get
			{
				return tBM_;
			}
			[CompilerGenerated]
			protected set
			{
				tBM_ = value;
			}
		}
		protected Series<double> Value
		{
			get
			{
				return Values[0];
			}
		}
		protected int Count
		{
			get
			{
				return Value.Count;
			}
		}
		public double this[int index]
		{
			get
			{
				return Value[index];
			}
			protected set
			{
				Value[index] = value;
			}
		}
		protected int CurrentBar
		{
			get
			{
				return script.CurrentBar;
			}
		}
		public double TickSize
		{
			get
			{
				return script.TickSize;
			}
		}
		public bool IsFirstTickOfBar
		{
			get
			{
				return script.IsFirstTickOfBar;
			}
		}
		public Bars[] BarsArray
		{
			get
			{
				return script.BarsArray;
			}
		}
		public IndicatorEngine(NinjaScriptBase scriptIn)
		{
			script = scriptIn;
			Input = scriptIn.Input;
			Open = scriptIn.Open;
			High = scriptIn.High;
			Low = scriptIn.Low;
			Close = scriptIn.Close;
			Volume = (ISeries<double>)(object)scriptIn.Volume;
			Values = new List<Series<double>>();
			SetState((State)1);
		}
		public void SetState(State stateIn)
		{
			State = stateIn;
			OnStateChange();
		}
		protected abstract void OnStateChange();
		protected void AddPlot(System.Windows.Media.Brush brush, string name)
		{
			Values.Add(new Series<double>(script));
		}
		protected void AddPlot(Stroke stroke, PlotStyle plotStyle, string name)
		{
			AddPlot(Brushes.Transparent, name);
		}
		public bool IsValidDataPoint(int barAgo)
		{
			return Value.IsValidDataPoint(barAgo);
		}
		public abstract void OnBarUpdate();
	}
	public class ATR : IndicatorEngine
	{
		[CompilerGenerated]
		private int rRM_;
		public int Period
		{
			[CompilerGenerated]
			get
			{
				return rRM_;
			}
			[CompilerGenerated]
			set
			{
				rRM_ = value;
			}
		}
		public ATR(NinjaScriptBase scriptIn)
			: base(scriptIn)
		{
		}
		protected override void OnStateChange()
		{
			if ((int)State == 1)
			{
				Description = Resource.NinjaScriptIndicatorDescriptionATR;
				Name = Resource.NinjaScriptIndicatorNameATR;
				IsSuspendedWhileInactive = true;
				Period = 14;
				AddPlot(Brushes.DarkCyan, Resource.NinjaScriptIndicatorNameATR);
			}
		}
		public override void OnBarUpdate()
		{
			double num = High[0];
			double num2 = Low[0];
			if (base.CurrentBar == 0)
			{
				base.Value[0] = num - num2;
				return;
			}
			double num3 = Close[1];
			double num4 = Math.Max(Math.Abs(num2 - num3), Math.Max(num - num2, Math.Abs(num - num3)));
			base.Value[0] = ((double)(Math.Min(base.CurrentBar + 1, Period) - 1) * base.Value[1] + num4) / (double)Math.Min(base.CurrentBar + 1, Period);
		}
	}
	public class SMA : IndicatorEngine
	{
		private double xRM_;
		private double xhM_;
		[CompilerGenerated]
		private int rRM_;
		[NinjaScriptProperty]
		[Display(Name = "Period", GroupName = "NinjaScriptParameters", Order = 0)]
		[Range(1, int.MaxValue)]
		public int Period
		{
			[CompilerGenerated]
			get
			{
				return rRM_;
			}
			[CompilerGenerated]
			set
			{
				rRM_ = value;
			}
		}
		public SMA(NinjaScriptBase scriptIn)
			: base(scriptIn)
		{
		}
		public static SMA create(NinjaScriptBase scriptIn, ISeries<double> inputIn, int periodIn)
		{
			SMA sMA = new SMA(scriptIn);
			sMA.Period = periodIn;
			sMA.Input = inputIn;
			sMA.SetState((State)2);
			sMA.SetState(scriptIn.State);
			return sMA;
		}
		protected override void OnStateChange()
		{
			if ((int)State == 1)
			{
				Description = Resource.NinjaScriptIndicatorDescriptionSMA;
				Name = Resource.NinjaScriptIndicatorNameSMA;
				IsOverlay = true;
				IsSuspendedWhileInactive = true;
				Period = 14;
				AddPlot(Brushes.Goldenrod, Resource.NinjaScriptIndicatorNameSMA);
			}
			else if ((int)State == 2)
			{
				xRM_ = 0.0;
				xhM_ = 0.0;
			}
		}
		public override void OnBarUpdate()
		{
			if (base.BarsArray[0].BarsType.IsRemoveLastBarSupported)
			{
				if (base.CurrentBar == 0)
				{
					base.Value[0] = Input[0];
					return;
				}
				double num = base.Value[1] * (double)Math.Min(base.CurrentBar, Period);
				if (base.CurrentBar >= Period)
				{
					base.Value[0] = (num + Input[0] - Input[Period]) / (double)Math.Min(base.CurrentBar, Period);
				}
				else
				{
					base.Value[0] = (num + Input[0]) / (double)(Math.Min(base.CurrentBar, Period) + 1);
				}
			}
			else
			{
				if (base.IsFirstTickOfBar)
				{
					xRM_ = xhM_;
				}
				xhM_ = xRM_ + Input[0] - ((base.CurrentBar >= Period) ? Input[Period] : 0.0);
				base.Value[0] = xhM_ / (double)((base.CurrentBar < Period) ? (base.CurrentBar + 1) : Period);
			}
		}
	}
	public class EMA : IndicatorEngine
	{
		private double rhM_;
		private double rxM_;
		[CompilerGenerated]
		private int rRM_;
		[Display(Name = "Period", GroupName = "NinjaScriptParameters", Order = 0)]
		[Range(1, int.MaxValue)]
		[NinjaScriptProperty]
		public int Period
		{
			[CompilerGenerated]
			get
			{
				return rRM_;
			}
			[CompilerGenerated]
			set
			{
				rRM_ = value;
			}
		}
		public EMA(NinjaScriptBase scriptIn)
			: base(scriptIn)
		{
		}
		public static EMA create(NinjaScriptBase scriptIn, ISeries<double> inputIn, int periodIn)
		{
			EMA eMA = new EMA(scriptIn);
			eMA.Period = periodIn;
			eMA.Input = inputIn;
			eMA.SetState((State)2);
			eMA.SetState(scriptIn.State);
			return eMA;
		}
		protected override void OnStateChange()
		{
			if ((int)State == 1)
			{
				Description = Resource.NinjaScriptIndicatorDescriptionEMA;
				Name = Resource.NinjaScriptIndicatorNameEMA;
				IsOverlay = true;
				IsSuspendedWhileInactive = true;
				Period = 14;
				AddPlot(Brushes.Goldenrod, Resource.NinjaScriptIndicatorNameEMA);
			}
			else if ((int)State == 2)
			{
				rhM_ = 2.0 / (double)(1 + Period);
				rxM_ = 1.0 - 2.0 / (double)(1 + Period);
			}
		}
		public override void OnBarUpdate()
		{
			base.Value[0] = ((base.CurrentBar == 0) ? Input[0] : (Input[0] * rhM_ + rxM_ * base.Value[1]));
		}
	}
	public class HMA : IndicatorEngine
	{
		private Series<double> sBM_;
		private WMA sRM_;
		private WMA shM_;
		private WMA sxM_;
		[CompilerGenerated]
		private int rRM_;
		[Display(Name = "Period", GroupName = "NinjaScriptParameters", Order = 0)]
		[NinjaScriptProperty]
		[Range(2, int.MaxValue)]
		public int Period
		{
			[CompilerGenerated]
			get
			{
				return rRM_;
			}
			[CompilerGenerated]
			set
			{
				rRM_ = value;
			}
		}
		public HMA(NinjaScriptBase scriptIn)
			: base(scriptIn)
		{
		}
		public static HMA create(NinjaScriptBase scriptIn, ISeries<double> inputIn, int periodIn)
		{
			HMA hMA = new HMA(scriptIn);
			hMA.Period = periodIn;
			hMA.Input = inputIn;
			hMA.SetState((State)2);
			hMA.SetState(scriptIn.State);
			return hMA;
		}
		protected override void OnStateChange()
		{
			if ((int)State == 1)
			{
				Description = Resource.NinjaScriptIndicatorDescriptionHMA;
				Name = Resource.NinjaScriptIndicatorNameHMA;
				IsSuspendedWhileInactive = true;
				Period = 14;
				IsOverlay = true;
				AddPlot(Brushes.Goldenrod, Resource.NinjaScriptIndicatorNameHMA);
			}
			else if ((int)State == 4)
			{
				sBM_ = new Series<double>(script);
				sRM_ = WMA.create(script, Input, Period / 2);
				shM_ = WMA.create(script, Input, Period);
				sxM_ = WMA.create(script, (ISeries<double>)(object)sBM_, (int)Math.Sqrt(Period));
			}
		}
		public override void OnBarUpdate()
		{
			sRM_.OnBarUpdate();
			shM_.OnBarUpdate();
			sBM_[0] = 2.0 * sRM_[0] - shM_[0];
			sxM_.OnBarUpdate();
			base.Value[0] = sxM_[0];
		}
	}
	public class WMA : IndicatorEngine
	{
		private int yxM_;
		private double xRM_;
		private double zBM_;
		private double xhM_;
		private double zRM_;
		[CompilerGenerated]
		private int rRM_;
		[Display(Name = "Period", GroupName = "NinjaScriptParameters", Order = 0)]
		[Range(1, int.MaxValue)]
		[NinjaScriptProperty]
		public int Period
		{
			[CompilerGenerated]
			get
			{
				return rRM_;
			}
			[CompilerGenerated]
			set
			{
				rRM_ = value;
			}
		}
		public WMA(NinjaScriptBase scriptIn)
			: base(scriptIn)
		{
		}
		public static WMA create(NinjaScriptBase scriptIn, ISeries<double> inputIn, int periodIn)
		{
			WMA wMA = new WMA(scriptIn);
			wMA.Period = periodIn;
			wMA.Input = inputIn;
			wMA.SetState((State)2);
			wMA.SetState(scriptIn.State);
			return wMA;
		}
		protected override void OnStateChange()
		{
			if ((int)State == 1)
			{
				Description = Resource.NinjaScriptIndicatorDescriptionWMA;
				Name = Resource.NinjaScriptIndicatorNameWMA;
				IsOverlay = true;
				IsSuspendedWhileInactive = true;
				Period = 14;
				AddPlot(Brushes.Goldenrod, Resource.NinjaScriptIndicatorNameWMA);
			}
			else if ((int)State == 2)
			{
				xRM_ = 0.0;
				zBM_ = 0.0;
				xhM_ = 0.0;
				zRM_ = 0.0;
			}
		}
		public override void OnBarUpdate()
		{
			if (base.BarsArray[0].BarsType.IsRemoveLastBarSupported)
			{
				if (base.CurrentBar == 0)
				{
					base.Value[0] = Input[0];
					return;
				}
				int num = Math.Min(Period - 1, base.CurrentBar);
				double num2 = 0.0;
				int num3 = 0;
				for (int num4 = num; num4 >= 0; num4--)
				{
					num2 += (double)(num4 + 1) * Input[num - num4];
					num3 += num4 + 1;
				}
				base.Value[0] = num2 / (double)num3;
			}
			else
			{
				if (base.IsFirstTickOfBar)
				{
					zBM_ = zRM_;
					xRM_ = xhM_;
					yxM_ = Math.Min(base.CurrentBar + 1, Period);
				}
				zRM_ = zBM_ - ((base.CurrentBar >= Period) ? xRM_ : 0.0) + (double)yxM_ * Input[0];
				xhM_ = xRM_ + Input[0] - ((base.CurrentBar >= Period) ? Input[Period] : 0.0);
				base.Value[0] = zRM_ / (0.5 * (double)yxM_ * (double)(yxM_ + 1));
			}
		}
	}
	public class TosWildersMa : IndicatorEngine
	{
		private double rhM_;
		private double rxM_;
		[CompilerGenerated]
		private int rRM_;
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "Period", GroupName = "NinjaScriptParameters", Order = 0)]
		public int Period
		{
			[CompilerGenerated]
			get
			{
				return rRM_;
			}
			[CompilerGenerated]
			set
			{
				rRM_ = value;
			}
		}
		public TosWildersMa(NinjaScriptBase scriptIn)
			: base(scriptIn)
		{
		}
		public static TosWildersMa create(NinjaScriptBase scriptIn, ISeries<double> inputIn, int periodIn)
		{
			TosWildersMa tosWildersMa = new TosWildersMa(scriptIn);
			tosWildersMa.Period = periodIn;
			tosWildersMa.Input = inputIn;
			tosWildersMa.SetState((State)2);
			tosWildersMa.SetState(scriptIn.State);
			return tosWildersMa;
		}
		protected override void OnStateChange()
		{
			if ((int)State == 1)
			{
				Name = "TOS Wilder MA";
				Description = Name;
				IsOverlay = true;
				IsSuspendedWhileInactive = true;
				Period = 14;
				AddPlot(Brushes.Goldenrod, Resource.NinjaScriptIndicatorNameEMA);
			}
			else if ((int)State == 2)
			{
				rhM_ = 1.0 / (double)Period;
				rxM_ = 1.0 - rhM_;
			}
		}
		public override void OnBarUpdate()
		{
			base.Value[0] = ((base.CurrentBar == 0) ? Input[0] : (Input[0] * rhM_ + rxM_ * base.Value[1]));
		}
	}
	public class MAX : IndicatorEngine
	{
		private int uRM_;
		private double uhM_;
		private double uxM_;
		private int vBM_;
		private int vRM_;
		[CompilerGenerated]
		private int rRM_;
		[Range(1, int.MaxValue)]
		[NinjaScriptProperty]
		[Display(Name = "Period", GroupName = "NinjaScriptParameters", Order = 0)]
		public int Period
		{
			[CompilerGenerated]
			get
			{
				return rRM_;
			}
			[CompilerGenerated]
			set
			{
				rRM_ = value;
			}
		}
		public MAX(NinjaScriptBase scriptIn)
			: base(scriptIn)
		{
		}
		public static MAX create(NinjaScriptBase scriptIn, ISeries<double> inputIn, int periodIn)
		{
			MAX mAX = new MAX(scriptIn);
			mAX.Input = inputIn;
			mAX.Period = periodIn;
			mAX.SetState((State)2);
			mAX.SetState(scriptIn.State);
			return mAX;
		}
		protected override void OnStateChange()
		{
			if ((int)State == 1)
			{
				Description = Resource.NinjaScriptIndicatorDescriptionMAX;
				Name = Resource.NinjaScriptIndicatorNameMAX;
				IsOverlay = true;
				IsSuspendedWhileInactive = true;
				Period = 14;
				AddPlot(Brushes.DarkCyan, Resource.NinjaScriptIndicatorNameMAX);
			}
			else if ((int)State == 2)
			{
				uRM_ = 0;
				uhM_ = 0.0;
				uxM_ = 0.0;
				vBM_ = 0;
				vRM_ = 0;
			}
		}
		public override void OnBarUpdate()
		{
			if (base.CurrentBar == 0)
			{
				uxM_ = Input[0];
				uhM_ = Input[0];
				vBM_ = 0;
				uRM_ = 0;
				vRM_ = 0;
				base.Value[0] = Input[0];
				return;
			}
			if (base.CurrentBar - vBM_ >= Period || base.CurrentBar < vRM_)
			{
				uxM_ = double.MinValue;
				for (int num = Math.Min(base.CurrentBar, Period - 1); num > 0; num--)
				{
					if (Input[num] >= uxM_)
					{
						uxM_ = Input[num];
						vBM_ = base.CurrentBar - num;
					}
				}
			}
			if (vRM_ != base.CurrentBar)
			{
				uhM_ = uxM_;
				uRM_ = vBM_;
				vRM_ = base.CurrentBar;
			}
			if (Input[0] >= uhM_)
			{
				uxM_ = Input[0];
				vBM_ = base.CurrentBar;
			}
			else
			{
				uxM_ = uhM_;
				vBM_ = uRM_;
			}
			base.Value[0] = uxM_;
		}
	}
	public class MIN : IndicatorEngine
	{
		private int uRM_;
		private double wxM_;
		private double xBM_;
		private int vBM_;
		private int vRM_;
		[CompilerGenerated]
		private int rRM_;
		[Range(1, int.MaxValue)]
		[Display(Name = "Period", GroupName = "NinjaScriptParameters", Order = 0)]
		[NinjaScriptProperty]
		public int Period
		{
			[CompilerGenerated]
			get
			{
				return rRM_;
			}
			[CompilerGenerated]
			set
			{
				rRM_ = value;
			}
		}
		public MIN(NinjaScriptBase scriptIn)
			: base(scriptIn)
		{
		}
		public static MIN create(NinjaScriptBase scriptIn, ISeries<double> inputIn, int periodIn)
		{
			MIN mIN = new MIN(scriptIn);
			mIN.Input = inputIn;
			mIN.Period = periodIn;
			mIN.SetState((State)2);
			mIN.SetState(scriptIn.State);
			return mIN;
		}
		protected override void OnStateChange()
		{
			if ((int)State == 1)
			{
				Description = Resource.NinjaScriptIndicatorDescriptionMIN;
				Name = Resource.NinjaScriptIndicatorNameMIN;
				IsOverlay = true;
				IsSuspendedWhileInactive = true;
				Period = 14;
				AddPlot(Brushes.DarkCyan, Resource.NinjaScriptIndicatorNameMIN);
			}
			else if ((int)State == 2)
			{
				uRM_ = 0;
				wxM_ = 0.0;
				xBM_ = 0.0;
				vBM_ = 0;
				vRM_ = 0;
			}
		}
		public override void OnBarUpdate()
		{
			if (base.CurrentBar == 0)
			{
				xBM_ = Input[0];
				wxM_ = Input[0];
				vBM_ = 0;
				uRM_ = 0;
				vRM_ = 0;
				base.Value[0] = Input[0];
				return;
			}
			if (base.CurrentBar - vBM_ >= Period || base.CurrentBar < vRM_)
			{
				xBM_ = double.MaxValue;
				for (int num = Math.Min(base.CurrentBar, Period - 1); num > 0; num--)
				{
					if (Input[num] <= xBM_)
					{
						xBM_ = Input[num];
						vBM_ = base.CurrentBar - num;
					}
				}
			}
			if (vRM_ != base.CurrentBar)
			{
				wxM_ = xBM_;
				uRM_ = vBM_;
				vRM_ = base.CurrentBar;
			}
			if (Input[0] <= wxM_)
			{
				xBM_ = Input[0];
				vBM_ = base.CurrentBar;
			}
			else
			{
				xBM_ = wxM_;
				vBM_ = uRM_;
			}
			base.Value[0] = xBM_;
		}
	}
	public class CheckNewBar
	{
		private NinjaScriptBase oRM_;
		private int ohM_;
		private int oxM_ = -1;
		[CompilerGenerated]
		private bool pBM_;
		public bool isNewBar
		{
			[CompilerGenerated]
			get
			{
				return pBM_;
			}
			[CompilerGenerated]
			private set
			{
				pBM_ = value;
			}
		}
		public CheckNewBar(NinjaScriptBase scriptIn, int xSeriesIn = 0)
		{
			oRM_ = scriptIn;
			ohM_ = xSeriesIn;
		}
		public bool check()
		{
			return check(oRM_.CurrentBars[ohM_]);
		}
		public bool check(int currentBar)
		{
			isNewBar = currentBar != oxM_;
			oxM_ = currentBar;
			return isNewBar;
		}
	}
	public class WaitForNextBar
	{
		private NinjaScriptBase oRM_;
		private int ohM_;
		private int oxM_;
		[CompilerGenerated]
		private bool pBM_;
		public bool isNewBar
		{
			[CompilerGenerated]
			get
			{
				return pBM_;
			}
			[CompilerGenerated]
			private set
			{
				pBM_ = value;
			}
		}
		public void activate()
		{
			oxM_ = oRM_.CurrentBars[ohM_];
		}
		public WaitForNextBar(NinjaScriptBase scriptIn, int xSeriesIn = 0)
		{
			oRM_ = scriptIn;
			ohM_ = xSeriesIn;
		}
		public bool check()
		{
			int num = oRM_.CurrentBars[ohM_];
			isNewBar = num != oxM_;
			return isNewBar;
		}
	}
	public class LineDraw
	{
		private Series<double> tRM_;
		private SharpDX.Direct2D1.Brush thM_;
		private float txM_;
		private System.Windows.Media.Brush uBM_;
		public LineDraw(Series<double> drawSeriesIn, float widthIn, System.Windows.Media.Brush refBrushIn)
		{
			tRM_ = drawSeriesIn;
			txM_ = widthIn;
			uBM_ = refBrushIn;
		}
		public void onRenderTargetChange(RenderTarget newRenderTarget)
		{
			if (newRenderTarget != null)
			{
				thM_ = DxExtensions.ToDxBrush(uBM_, newRenderTarget);
			}
		}
		public void render(RenderTarget renderTarget, ChartControl chartControl, ChartScale chartScale, ChartBars chartBars)
		{
			for (int i = chartBars.FromIndex; i <= chartBars.ToIndex; i++)
			{
				if (tRM_.IsValidDataPointAt(i) && i < chartBars.Count - 1 && tRM_.IsValidDataPointAt(i + 1))
				{
					int xByBarIndex = chartControl.GetXByBarIndex(chartBars, i);
					int xByBarIndex2 = chartControl.GetXByBarIndex(chartBars, i + 1);
					int yByValue = chartScale.GetYByValue(tRM_.GetValueAt(i));
					int yByValue2 = chartScale.GetYByValue(tRM_.GetValueAt(i));
					drawLine(renderTarget, xByBarIndex, yByValue, xByBarIndex2, yByValue2, thM_);
				}
			}
		}
		public void drawLine(RenderTarget renderTarget, int px, int py, int px2, int py2, SharpDX.Direct2D1.Brush brush)
		{
			Vector2 val = new Vector2((float)px, (float)py);
			Vector2 val2 = new Vector2((float)px2, (float)py2);
			renderTarget.DrawLine(val, val2, brush, txM_);
		}
	}
	public class TosArrowDraw
	{
		private Series<double> tRM_;
		private SharpDX.Direct2D1.Brush thM_;
		private int xxM_;
		private double yBM_;
		private System.Windows.Media.Brush uBM_;
		public TosArrowDraw(Series<double> drawSeriesIn, int dirIn, double scaleIn, System.Windows.Media.Brush refBrushIn)
		{
			tRM_ = drawSeriesIn;
			xxM_ = dirIn;
			yBM_ = scaleIn;
			uBM_ = refBrushIn;
		}
		public void setSeries(Series<double> drawSeriesIn)
		{
			tRM_ = drawSeriesIn;
		}
		public void onRenderTargetChange(RenderTarget newRenderTarget)
		{
			if (newRenderTarget != null)
			{
				thM_ = DxExtensions.ToDxBrush(uBM_, newRenderTarget);
			}
		}
		public void render(RenderTarget renderTarget, ChartControl chartControl, ChartScale chartScale, ChartBars chartBars)
		{
			for (int i = chartBars.FromIndex; i <= chartBars.ToIndex; i++)
			{
				if (tRM_.IsValidDataPointAt(i))
				{
					int xByBarIndex = chartControl.GetXByBarIndex(chartBars, i);
					int yByValue = chartScale.GetYByValue(tRM_.GetValueAt(i));
					drawArrow(renderTarget, xByBarIndex, yByValue, thM_);
				}
			}
		}
		public void drawArrow(RenderTarget renderTarget, int px, int py, SharpDX.Direct2D1.Brush brush)
		{
			SharpDX.Direct2D1.PathGeometry val = new SharpDX.Direct2D1.PathGeometry(Globals.D2DFactory);
			GeometrySink val2 = val.Open();
			double num = yBM_ * 0.4;
			double num2 = yBM_;
			double num3 = yBM_ * 0.4;
			double num4 = yBM_ * 0.1;
			int num5 = yRM_((double)py + (double)xxM_ * num);
			int num6 = yRM_((double)py + (double)xxM_ * num2);
			Vector2 val3 = new Vector2((float)px, (float)py);
			Vector2 val4 = new Vector2((float)yRM_((double)px - num3), (float)num5);
			Vector2 val5 = new Vector2((float)yRM_((double)px - num4), (float)num5);
			Vector2 val6 = new Vector2((float)yRM_((double)px - num4), (float)num6);
			Vector2 val7 = new Vector2((float)yRM_((double)px + num4), (float)num6);
			Vector2 val8 = new Vector2((float)yRM_((double)px + num4), (float)num5);
			Vector2 val9 = new Vector2((float)yRM_((double)px + num3), (float)num5);
			((SimplifiedGeometrySink)val2).BeginFigure(val3, FigureBegin.Filled);
			val2.AddLine(val4);
			val2.AddLine(val5);
			val2.AddLine(val6);
			val2.AddLine(val7);
			val2.AddLine(val8);
			val2.AddLine(val9);
			((SimplifiedGeometrySink)val2).EndFigure(FigureEnd.Open);
			((SimplifiedGeometrySink)val2).Close();
			renderTarget.FillGeometry(val, brush);
		}
		private int yRM_(double yhM_)
		{
			return (int)Math.Round(yhM_);
		}
	}
	public class TosStochastics : IndicatorEngine
	{
		private Series<double> den;
		private Series<double> fastK;
		private MIN min;
		private MAX max;
		private Series<double> nom;
		private IndicatorEngine smaFastK;
		private IndicatorEngine smaK;
		private ISeries<double> priceH;
		private ISeries<double> priceL;
		private ISeries<double> priceC;
		[NinjaScriptProperty]
		[Display(GroupName = "Parameters", Order = 10, Name = "overbought", Description = "overbought")]
		[Range(0, 100)]
		public double overbought
		{
			get;
			set;
		}
		[Range(0, 100)]
		[Display(GroupName = "Parameters", Order = 20, Name = "oversold", Description = "oversold")]
		[NinjaScriptProperty]
		public double oversold
		{
			get;
			set;
		}
		[NinjaScriptProperty]
		[Display(GroupName = "Parameters", Order = 30, Name = "KPeriod", Description = "KPeriod")]
		[Range(1, int.MaxValue)]
		public int PeriodK
		{
			get;
			set;
		}
		[Display(GroupName = "Parameters", Order = 40, Name = "DPeriod", Description = "DPeriod")]
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		public int PeriodD
		{
			get;
			set;
		}
		[Display(GroupName = "Parameters", Order = 50, Name = "Smooth", Description = "Smooth")]
		[Range(1, int.MaxValue)]
		[NinjaScriptProperty]
		public int Smooth
		{
			get;
			set;
		}
		[Display(GroupName = "Parameters", Order = 60, Name = "priceH", Description = "priceH")]
		public PriceMode priceHSelect
		{
			get;
			set;
		}
		[Display(GroupName = "Parameters", Order = 70, Name = "priceL", Description = "priceL")]
		public PriceMode priceLSelect
		{
			get;
			set;
		}
		[Display(GroupName = "Parameters", Order = 80, Name = "priceC", Description = "priceC")]
		[NinjaScriptProperty]
		public PriceMode priceCSelect
		{
			get;
			set;
		}
		[Display(GroupName = "Parameters", Order = 90, Name = "avgType", Description = "avgType")]
		[NinjaScriptProperty]
		public MaMode avgType
		{
			get;
			set;
		}
		[Browsable(false)]
		[XmlIgnore]
		public Series<double> D
		{
			get
			{
				return base.Values[0];
			}
		}
		[XmlIgnore]
		[Browsable(false)]
		public Series<double> K
		{
			get
			{
				return base.Values[1];
			}
		}
		public TosStochastics(NinjaScriptBase scriptIn)
			: base(scriptIn)
		{
		}
		protected override void OnStateChange()
		{
			if ((int)State == 1)
			{
				Name = "TOS Stochastic full";
				Description = Name;
				IsSuspendedWhileInactive = true;
				overbought = 80.0;
				oversold = 20.0;
				PeriodK = 10;
				PeriodD = 10;
				Smooth = 3;
				priceHSelect = PriceMode.High;
				priceLSelect = PriceMode.Low;
				priceCSelect = PriceMode.Close;
				avgType = MaMode.SMA;
				AddPlot(Brushes.DodgerBlue, Resource.StochasticsD);
				AddPlot(Brushes.Goldenrod, Resource.StochasticsK);
			}
			else if ((int)State == 4)
			{
				priceH = PriceSeriesPicker.Get(script, priceHSelect);
				priceL = PriceSeriesPicker.Get(script, priceLSelect);
				priceC = PriceSeriesPicker.Get(script, priceCSelect);
				den = new Series<double>(script);
				nom = new Series<double>(script);
				fastK = new Series<double>(script);
				min = MIN.create(script, priceL, PeriodK);
				max = MAX.create(script, priceH, PeriodK);
				smaFastK = MovingAverageFactory.Create(script, avgType, (ISeries<double>)(object)fastK, Smooth);
				smaK = MovingAverageFactory.Create(script, avgType, (ISeries<double>)(object)K, PeriodD);
			}
		}
		public override void OnBarUpdate()
		{
			min.OnBarUpdate();
			max.OnBarUpdate();
			double num = min[0];
			nom[0] = priceC[0] - num;
			den[0] = max[0] - num;
			if (MathExtentions.ApproxCompare(den[0], 0.0) == 0)
			{
				fastK[0] = ((base.CurrentBar == 0) ? 50.0 : fastK[1]);
			}
			else
			{
				fastK[0] = Math.Min(100.0, Math.Max(0.0, 100.0 * nom[0] / den[0]));
			}
			smaFastK.OnBarUpdate();
			K[0] = smaFastK[0];
			smaK.OnBarUpdate();
			D[0] = smaK[0];
		}
	}
	public class TosZigZagHighLow : IndicatorEngine
	{
		public Series<int> extremumDir;
		public int startIndex;
		[XmlIgnore]
		public ISeries<double> highSeries;
		[XmlIgnore]
		public ISeries<double> lowSeries;
		private ATR atr;
		private CheckNewBar checkNewBar;
		[NinjaScriptProperty]
		[Display(GroupName = "Parameters", Order = 10, Name = "price_h", Description = "price_h")]
		public PriceMode PPriceH
		{
			get;
			set;
		}
		[Display(GroupName = "Parameters", Order = 20, Name = "price_l", Description = "price_l")]
		[NinjaScriptProperty]
		public PriceMode PPriceL
		{
			get;
			set;
		}
		[NinjaScriptProperty]
		[Display(GroupName = "Parameters", Order = 30, Name = "percentage reversal", Description = "percentage reversal")]
		[Range(0, 100)]
		public double PPercentageReversal
		{
			get;
			set;
		}
		[Range(0.0, double.MaxValue)]
		[Display(GroupName = "Parameters", Order = 40, Name = "absolute reversal", Description = "absolute reversal")]
		[NinjaScriptProperty]
		public double PAbsoluteReversal
		{
			get;
			set;
		}
		[Range(0, int.MaxValue)]
		[Display(GroupName = "Parameters", Order = 50, Name = "atr length", Description = "atr length")]
		[NinjaScriptProperty]
		public int PAtrLength
		{
			get;
			set;
		}
		[Range(0.0, double.MaxValue)]
		[Display(GroupName = "Parameters", Order = 60, Name = "atr reversal", Description = "atr reversal")]
		[NinjaScriptProperty]
		public double PAtrReversal
		{
			get;
			set;
		}
		[Range(0, int.MaxValue)]
		[NinjaScriptProperty]
		[Display(GroupName = "Parameters", Order = 70, Name = "tick reversal", Description = "tick reversal")]
		public double PTickReversal
		{
			get;
			set;
		}
		public int xLastChangedBar
		{
			get;
			private set;
		}
		public TosZigZagHighLow(NinjaScriptBase scriptIn)
			: base(scriptIn)
		{
		}
		protected override void OnStateChange()
		{
			if (atr != null)
			{
				atr.SetState(State);
			}
			if ((int)State == 1)
			{
				Name = "TOS ZigZagHighLow";
				Description = Name;
				DisplayInDataBox = false;
				DrawOnPricePanel = false;
				IsSuspendedWhileInactive = false;
				IsOverlay = true;
				PaintPriceMarkers = false;
				PPriceH = PriceMode.High;
				PPriceL = PriceMode.Low;
				PPercentageReversal = 5.0;
				PAbsoluteReversal = 0.0;
				PAtrLength = 5;
				PAtrReversal = 1.5;
				PTickReversal = 0.0;
				AddPlot(Brushes.DodgerBlue, Resource.NinjaScriptIndicatorNameZigZag);
				DisplayInDataBox = false;
				PaintPriceMarkers = false;
				xLastChangedBar = 0;
			}
			else if ((int)State == 2)
			{
				MaximumBarsLookBack = (MaximumBarsLookBack)1;
				startIndex = int.MinValue;
			}
			else if ((int)State == 4)
			{
				highSeries = PriceSeriesPicker.Get(script, PPriceH);
				lowSeries = PriceSeriesPicker.Get(script, PPriceL);
				checkNewBar = new CheckNewBar(script);
				atr = new ATR(script);
				atr.Period = PAtrLength;
				atr.SetState((State)2);
				atr.SetState(State);
				extremumDir = new Series<int>(script, (MaximumBarsLookBack)1);
			}
		}
		public override void OnBarUpdate()
		{
			atr.OnBarUpdate();
			base.Value.Reset(0);
			extremumDir[0] = 0;
			if (checkNewBar.check())
			{
				updateBar(base.CurrentBar - 1);
			}
		}
		private void updateBar(int currentBar)
		{
			xLastChangedBar = currentBar;
			int num = base.CurrentBar - currentBar;
			if (currentBar < 1)
			{
				return;
			}
			base.Value.Reset(num);
			extremumDir[num] = 0;
			int num2 = 0;
			int num3 = -1;
			double num4 = 0.0;
			for (int i = num + 1; i <= currentBar; i++)
			{
				if (extremumDir[i] <= 0)
				{
					if (extremumDir[i] < 0)
					{
						num3 = base.CurrentBar - i;
						num4 = base.Value[i];
						num2 = -1;
						break;
					}
					continue;
				}
				num3 = base.CurrentBar - i;
				num4 = base.Value[i];
				num2 = 1;
				break;
			}
			if (num4 == 0.0)
			{
				num4 = Input[0];
			}
			double num5 = atr[num];
			double num6 = PAbsoluteReversal + PTickReversal * base.TickSize + num5 * PAtrReversal;
			double num7 = ((num2 < 0) ? num4 : (num4 * (1.0 - PPercentageReversal * 0.01) - num6));
			double num8 = ((num2 > 0) ? num4 : (num4 * (1.0 + PPercentageReversal * 0.01) + num6));
			bool flag = highSeries[num] > num8;
			bool flag2 = lowSeries[num] < num7;
			if (flag && flag2)
			{
				if (num2 > 0)
				{
					flag2 = false;
				}
				else
				{
					flag = false;
				}
			}
			if (flag)
			{
				base.Value[num] = highSeries[num];
				extremumDir[num] = 1;
				updateLastChangedBar(currentBar);
				if (num2 > 0 && num3 >= 0)
				{
					base.Value.Reset(base.CurrentBar - num3);
					extremumDir[base.CurrentBar - num3] = 0;
					updateLastChangedBar(num3);
				}
			}
			if (flag2)
			{
				base.Value[num] = lowSeries[num];
				extremumDir[num] = -1;
				updateLastChangedBar(currentBar);
				if (num2 < 0 && num3 >= 0)
				{
					base.Value.Reset(base.CurrentBar - num3);
					extremumDir[base.CurrentBar - num3] = 0;
					updateLastChangedBar(num3);
				}
			}
			if (startIndex == int.MinValue && base.Value.IsValidDataPoint(num))
			{
				startIndex = currentBar - (((int)Calculate == 0) ? 1 : 0);
			}
		}
		private void updateLastChangedBar(int xChangedBar)
		{
			if (xChangedBar < xLastChangedBar)
			{
				xLastChangedBar = xChangedBar;
			}
		}
		private bool IsPriceGreater(double a, double b)
		{
			return MathExtentions.ApproxCompare(a, b) > 0;
		}
	}
	public static class MovingAverageFactory
	{
		public static IndicatorEngine Create(
			NinjaScriptBase owner,
			MaMode mode,
			ISeries<double> input,
			int length)
		{
			switch (mode)
			{
			case MaMode.SMA:
				return SMA.create(owner, input, length);
			case MaMode.EMA:
				return EMA.create(owner, input, length);
			case MaMode.HMA:
				return HMA.create(owner, input, length);
			case MaMode.WMA:
				return WMA.create(owner, input, length);
			case MaMode.Wilders:
			default:
				return TosWildersMa.create(owner, input, length);
			}
		}
	}
	public static class PriceSeriesPicker
	{
		public static ISeries<double> Get(NinjaScriptBase owner, PriceMode mode)
		{
			return Get(owner, owner.BarsInProgress, mode);
		}
		public static ISeries<double> Get(NinjaScriptBase owner, int barsInProgress, PriceMode mode)
		{
			switch (mode)
			{
			case PriceMode.Open:
				return owner.Opens[barsInProgress];
			case PriceMode.High:
				return owner.Highs[barsInProgress];
			case PriceMode.Low:
				return owner.Lows[barsInProgress];
			case PriceMode.Median:
				return owner.Medians[barsInProgress];
			case PriceMode.Typical:
				return owner.Typicals[barsInProgress];
			case PriceMode.Weighted:
				return owner.Weighteds[barsInProgress];
			case PriceMode.Close:
			default:
				return owner.Closes[barsInProgress];
			}
		}
	}
}
