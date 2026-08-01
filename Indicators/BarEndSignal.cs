#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.SuperDom;
using NinjaTrader.Gui.Tools;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

//This namespace holds Indicators in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Indicators
{
	public class BarEndSignal : Indicator
	{
		private Thread beepingThread;
		
		private DateTime currentBarTime;
		
		private bool pauseActivity = false;
		
		private bool signal1Played = false;		
		private bool signal1Using = false;
		private int signal1Delay;
		private int signal1Duration = 1000;
		private int signal1Frequency = 500;
		
		private bool signal2Played = false;
		private bool signal2Using = false;
		private int signal2Delay;
		private int signal2Duration = 1000;
		private int signal2Frequency = 500;
		
		private bool signal3Played = false;
		private bool signal3Using = false;
		private int signal3Delay;
		private int signal3Duration = 1000;
		private int signal3Frequency = 500;
		
		private bool signal4Played = false;
		private bool signal4Using = false;
		private int signal4Delay;
		private int signal4Duration = 1000;
		private int signal4Frequency = 500;
		
		private bool signal5Played = false;
		private bool signal5Using = false;
		private int signal5Delay;
		private int signal5Duration = 1000;
		private int signal5Frequency = 500;
		
		private bool signal6Played = false;
		private bool signal6Using = false;
		private int signal6Delay;
		private int signal6Duration = 1000;
		private int signal6Frequency = 500;
		
		private bool signal7Played = false;
		private bool signal7Using = false;
		private int signal7Delay;
		private int signal7Duration = 1000;
		private int signal7Frequency = 500;
		
		private bool signal8Played = false;
		private bool signal8Using = false;
		private int signal8Delay;
		private int signal8Duration = 1000;
		private int signal8Frequency = 500;
		
		private bool signal9Played = false;
		private bool signal9Using = false;
		private int signal9Delay;
		private int signal9Duration = 1000;
		private int signal9Frequency = 500;
		
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Enter the description for your new custom Indicator here.";
				Name										= "Сигнал закрытия свечи";
				Calculate									= Calculate.OnPriceChange;
				IsOverlay									= true;
				DisplayInDataBox							= true;
				DrawOnPricePanel							= true;
				DrawHorizontalGridLines						= true;
				DrawVerticalGridLines						= true;
				PaintPriceMarkers							= true;
				ScaleJustification							= NinjaTrader.Gui.Chart.ScaleJustification.Right;
				//Disable this property if your indicator requires custom values that cumulate with each new market data event. 
				//See Help Guide for additional information.
				IsSuspendedWhileInactive					= false;
				MaximumBarsLookBack                         = MaximumBarsLookBack.Infinite;
			}
			else if (State == State.Configure)
			{ }
			else if (State == State.Realtime)
			{
				currentBarTime = Time.GetValueAt(Time.Count - 1);
				beepingThread = new Thread(BeepingProc);
				beepingThread.IsBackground = true;
				beepingThread.Start(this);
			}
			else if (State == State.Terminated)
			{
				try
				{ beepingThread.Abort(); }
				catch (NullReferenceException)
				{ return; }
			}
		}
		
		private static void BeepingProc(Object obj)
		{
			BarEndSignal ind = obj as BarEndSignal;

			while (true)
			{
				if (ind.currentBarTime != ind.Bars.LastBarTime)
				{
					ind.currentBarTime = ind.Bars.LastBarTime;
					
					ind.signal1Played = false;
					ind.signal2Played = false;
					ind.signal3Played = false;
					
					ind.signal4Played = false;
					ind.signal5Played = false;
					ind.signal6Played = false;
					
					ind.signal7Played = false;
					ind.signal8Played = false;
					ind.signal9Played = false;
				}
				
				if (ind.currentBarTime == DateTime.MinValue)
					return;
				
				DateTime signal1Time = ind.currentBarTime.AddSeconds(-ind.signal1Delay);
				DateTime signal2Time = ind.currentBarTime.AddSeconds(-ind.signal2Delay);
				DateTime signal3Time = ind.currentBarTime.AddSeconds(-ind.signal3Delay);
				
				DateTime signal4Time = ind.currentBarTime.AddSeconds(-ind.signal4Delay);
				DateTime signal5Time = ind.currentBarTime.AddSeconds(-ind.signal5Delay);
				DateTime signal6Time = ind.currentBarTime.AddSeconds(-ind.signal6Delay);
				
				DateTime signal7Time = ind.currentBarTime.AddSeconds(-ind.signal7Delay);
				DateTime signal8Time = ind.currentBarTime.AddSeconds(-ind.signal8Delay);
				DateTime signal9Time = ind.currentBarTime.AddSeconds(-ind.signal9Delay);
				
				DateTime now = TrimMilliseconds(DateTime.Now);
				
				if (ind.signal1Using == true && ind.pauseActivity == false && ind.signal1Played == false && now == signal1Time)
				{
					Console.Beep(ind.signal1Frequency, ind.signal1Duration);
					ind.signal1Played = true;
				}
				
				if (ind.signal2Using == true && ind.pauseActivity == false && ind.signal2Played == false && now == signal2Time)
				{
					Console.Beep(ind.signal2Frequency, ind.signal2Duration);
					ind.signal2Played = true;
				}
				
				if (ind.signal3Using == true && ind.pauseActivity == false && ind.signal3Played == false && now == signal3Time)
				{
					Console.Beep(ind.signal3Frequency, ind.signal3Duration);
					ind.signal3Played = true;
				}
				
				if (ind.signal4Using == true && ind.pauseActivity == false && ind.signal4Played == false && now == signal4Time)
				{
					Console.Beep(ind.signal4Frequency, ind.signal4Duration);
					ind.signal4Played = true;
				}
				
				if (ind.signal5Using == true && ind.pauseActivity == false && ind.signal5Played == false && now == signal5Time)
				{
					Console.Beep(ind.signal5Frequency, ind.signal5Duration);
					ind.signal5Played = true;
				}
				
				if (ind.signal6Using == true && ind.pauseActivity == false && ind.signal6Played == false && now == signal6Time)
				{
					Console.Beep(ind.signal6Frequency, ind.signal6Duration);
					ind.signal6Played = true;
				}
				
				if (ind.signal7Using == true && ind.pauseActivity == false && ind.signal7Played == false && now == signal7Time)
				{
					Console.Beep(ind.signal7Frequency, ind.signal7Duration);
					ind.signal7Played = true;
				}
				
				if (ind.signal8Using == true && ind.pauseActivity == false && ind.signal8Played == false && now == signal8Time)
				{
					Console.Beep(ind.signal8Frequency, ind.signal8Duration);
					ind.signal8Played = true;
				}
				
				if (ind.signal9Using == true && ind.pauseActivity == false && ind.signal9Played == false && now == signal9Time)
				{
					Console.Beep(ind.signal9Frequency, ind.signal9Duration);
					ind.signal9Played = true;
				}
				
				Thread.Sleep(100);
			}
		}
		
		public static DateTime TrimMilliseconds(DateTime dt)
		{
		    return new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second, 0, dt.Kind);
		}

		protected override void OnBarUpdate()
		{
			//Add your custom indicator logic here.
		}
		
		#region Properties
		[Display(Name = "Приостановить работу", GroupName = "NinjaScriptParameters", Order = 0)]
		public bool PauseActivity
		{
			get { return pauseActivity; }
			set { pauseActivity = value; }
		}
		
		
		[Display(Name = "Использовать", GroupName = "Сигнал 1", Order = 0)]
		public bool Signal1Using
		{
			get { return signal1Using; }
			set { signal1Using = value; }
		}
		
		[Display(Name = "Время до закрытия свечи (сек)", GroupName = "Сигнал 1", Order = 1)]
		public int Signal1Delay
		{
			get { return signal1Delay; }
			set { signal1Delay = value; }
		}
		
		[Display(Name = "Длительность (мс)", GroupName = "Сигнал 1", Order = 2)]
		public int Signal1Duration
		{
			get { return signal1Duration; }
			set { signal1Duration = value; }
		}
		
		[Display(Name = "Частота (Гц)", GroupName = "Сигнал 1", Order = 3)]
		public int Signal1Frequency
		{
			get { return signal1Frequency; }
			set { signal1Frequency = value; }
		}
		
		
		[Display(Name = "Использовать", GroupName = "Сигнал 2", Order = 0)]
		public bool Signal2Using
		{
			get { return signal2Using; }
			set { signal2Using = value; }
		}
		
		[Display(Name = "Время до закрытия свечи (сек)", GroupName = "Сигнал 2", Order = 1)]
		public int Signal2Delay
		{
			get { return signal2Delay; }
			set { signal2Delay = value; }
		}
		
		[Display(Name = "Длительность (мс)", GroupName = "Сигнал 2", Order = 2)]
		public int Signal2Duration
		{
			get { return signal2Duration; }
			set { signal2Duration = value; }
		}
		
		[Display(Name = "Частота (Гц)", GroupName = "Сигнал 2", Order = 3)]
		public int Signal2Frequency
		{
			get { return signal2Frequency; }
			set { signal2Frequency = value; }
		}
		
		
		[Display(Name = "Использовать", GroupName = "Сигнал 3", Order = 0)]
		public bool Signal3Using
		{
			get { return signal3Using; }
			set { signal3Using = value; }
		}
		
		[Display(Name = "Время до закрытия свечи (сек)", GroupName = "Сигнал 3", Order = 1)]
		public int Signal3Delay
		{
			get { return signal3Delay; }
			set { signal3Delay = value; }
		}
		
		[Display(Name = "Длительность (мс)", GroupName = "Сигнал 3", Order = 2)]
		public int Signal3Duration
		{
			get { return signal3Duration; }
			set { signal3Duration = value; }
		}
		
		[Display(Name = "Частота (Гц)", GroupName = "Сигнал 3", Order = 3)]
		public int Signal3Frequency
		{
			get { return signal3Frequency; }
			set { signal3Frequency = value; }
		}
		
		
		[Display(Name = "Использовать", GroupName = "Сигнал 4", Order = 0)]
		public bool Signal4Using
		{
			get { return signal4Using; }
			set { signal4Using = value; }
		}
		
		[Display(Name = "Время до закрытия свечи (сек)", GroupName = "Сигнал 4", Order = 1)]
		public int Signal4Delay
		{
			get { return signal4Delay; }
			set { signal4Delay = value; }
		}
		
		[Display(Name = "Длительность (мс)", GroupName = "Сигнал 4", Order = 2)]
		public int Signal4Duration
		{
			get { return signal4Duration; }
			set { signal4Duration = value; }
		}
		
		[Display(Name = "Частота (Гц)", GroupName = "Сигнал 4", Order = 3)]
		public int Signal4Frequency
		{
			get { return signal4Frequency; }
			set { signal4Frequency = value; }
		}
		
		
		[Display(Name = "Использовать", GroupName = "Сигнал 5", Order = 0)]
		public bool Signal5Using
		{
			get { return signal5Using; }
			set { signal5Using = value; }
		}
		
		[Display(Name = "Время до закрытия свечи (сек)", GroupName = "Сигнал 5", Order = 1)]
		public int Signal5Delay
		{
			get { return signal5Delay; }
			set { signal5Delay = value; }
		}
		
		[Display(Name = "Длительность (мс)", GroupName = "Сигнал 5", Order = 2)]
		public int Signal5Duration
		{
			get { return signal5Duration; }
			set { signal5Duration = value; }
		}
		
		[Display(Name = "Частота (Гц)", GroupName = "Сигнал 5", Order = 3)]
		public int Signal5Frequency
		{
			get { return signal5Frequency; }
			set { signal5Frequency = value; }
		}
		
		
		[Display(Name = "Использовать", GroupName = "Сигнал 6", Order = 0)]
		public bool Signal6Using
		{
			get { return signal6Using; }
			set { signal6Using = value; }
		}
		
		[Display(Name = "Время до закрытия свечи (сек)", GroupName = "Сигнал 6", Order = 1)]
		public int Signal6Delay
		{
			get { return signal6Delay; }
			set { signal6Delay = value; }
		}
		
		[Display(Name = "Длительность (мс)", GroupName = "Сигнал 6", Order = 2)]
		public int Signal6Duration
		{
			get { return signal6Duration; }
			set { signal6Duration = value; }
		}
		
		[Display(Name = "Частота (Гц)", GroupName = "Сигнал 6", Order = 3)]
		public int Signal6Frequency
		{
			get { return signal6Frequency; }
			set { signal6Frequency = value; }
		}
		
		
		[Display(Name = "Использовать", GroupName = "Сигнал 7", Order = 0)]
		public bool Signal7Using
		{
			get { return signal7Using; }
			set { signal7Using = value; }
		}
		
		[Display(Name = "Время до закрытия свечи (сек)", GroupName = "Сигнал 7", Order = 1)]
		public int Signal7Delay
		{
			get { return signal7Delay; }
			set { signal7Delay = value; }
		}
		
		[Display(Name = "Длительность (мс)", GroupName = "Сигнал 7", Order = 2)]
		public int Signal7Duration
		{
			get { return signal7Duration; }
			set { signal7Duration = value; }
		}
		
		[Display(Name = "Частота (Гц)", GroupName = "Сигнал 7", Order = 3)]
		public int Signal7Frequency
		{
			get { return signal7Frequency; }
			set { signal7Frequency = value; }
		}
		
		
		[Display(Name = "Использовать", GroupName = "Сигнал 8", Order = 0)]
		public bool Signal8Using
		{
			get { return signal8Using; }
			set { signal8Using = value; }
		}
		
		[Display(Name = "Время до закрытия свечи (сек)", GroupName = "Сигнал 8", Order = 1)]
		public int Signal8Delay
		{
			get { return signal8Delay; }
			set { signal8Delay = value; }
		}
		
		[Display(Name = "Длительность (мс)", GroupName = "Сигнал 8", Order = 2)]
		public int Signal8Duration
		{
			get { return signal8Duration; }
			set { signal8Duration = value; }
		}
		
		[Display(Name = "Частота (Гц)", GroupName = "Сигнал 8", Order = 3)]
		public int Signal8Frequency
		{
			get { return signal8Frequency; }
			set { signal8Frequency = value; }
		}
		
		
		[Display(Name = "Использовать", GroupName = "Сигнал 9", Order = 0)]
		public bool Signal9Using
		{
			get { return signal9Using; }
			set { signal9Using = value; }
		}
		
		[Display(Name = "Время до закрытия свечи (сек)", GroupName = "Сигнал 9", Order = 1)]
		public int Signal9Delay
		{
			get { return signal9Delay; }
			set { signal9Delay = value; }
		}
		
		[Display(Name = "Длительность (мс)", GroupName = "Сигнал 9", Order = 2)]
		public int Signal9Duration
		{
			get { return signal9Duration; }
			set { signal9Duration = value; }
		}
		
		[Display(Name = "Частота (Гц)", GroupName = "Сигнал 9", Order = 3)]
		public int Signal9Frequency
		{
			get { return signal9Frequency; }
			set { signal9Frequency = value; }
		}
		#endregion
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private BarEndSignal[] cacheBarEndSignal;
		public BarEndSignal BarEndSignal()
		{
			return BarEndSignal(Input);
		}

		public BarEndSignal BarEndSignal(ISeries<double> input)
		{
			if (cacheBarEndSignal != null)
				for (int idx = 0; idx < cacheBarEndSignal.Length; idx++)
					if (cacheBarEndSignal[idx] != null &&  cacheBarEndSignal[idx].EqualsInput(input))
						return cacheBarEndSignal[idx];
			return CacheIndicator<BarEndSignal>(new BarEndSignal(), input, ref cacheBarEndSignal);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.BarEndSignal BarEndSignal()
		{
			return indicator.BarEndSignal(Input);
		}

		public Indicators.BarEndSignal BarEndSignal(ISeries<double> input )
		{
			return indicator.BarEndSignal(input);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.BarEndSignal BarEndSignal()
		{
			return indicator.BarEndSignal(Input);
		}

		public Indicators.BarEndSignal BarEndSignal(ISeries<double> input )
		{
			return indicator.BarEndSignal(input);
		}
	}
}

#endregion
