#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
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
	public class SimpleWave
	{
		private MarketWaveDirection direction;
		private DateTime startTime, endTime;
		private double highest, lowest;
		
		public SimpleWave()
		{ }
		
		public MarketWaveDirection Direction
		{
			get { return direction; }
			set { direction = value; }
		}
		
		public DateTime StartTime
		{
			get { return startTime; }
			set { startTime = value; }
		}
		
		public DateTime EndTime
		{
			get { return endTime; }
			set { endTime = value; }
		}
		
		public double Highest
		{
			get { return highest; }
			set { highest = value; }
		}
		
		public double Lowest
		{
			get { return lowest; }
			set { lowest = value; }
		}
		
		public override string ToString()
		{
			string str = direction.ToString() + ";";
			str += highest.ToString() + ";";
			str += lowest.ToString() + ";";
			str += startTime.ToString() + ";";
			str += endTime.ToString();
			return str;
		}
	}
	
	public class CombinatedWaves : Indicator
	{
		private double minPercent = 23.6;
		private Brush lineColor = Brushes.Black;
		private int lineWidth = 2;
		
		private List<SimpleWave> simpleWaves;
		private SimpleWave currentSimpleWave;
		
		private List<List<MarketWave>> waves;
		
		private int roundValue;
		private double minPercentValue;
		private int tagNumber = 0;
		
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Enter the description for your new custom Indicator here.";
				Name										= "Combinated waves";
				Calculate									= Calculate.OnBarClose;
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
				MaximumBarsLookBack							= MaximumBarsLookBack.Infinite;
			}
			else if (State == State.Configure)
			{
				string name = Instrument.FullName.Split(' ')[0];
				switch (name)
				{
					case "CL":
						roundValue = 2;
						break;
					case "GC":
						roundValue = 1;
	                    break;
	                case "6B":
	                    roundValue = 4;
						break;
	                case "6E":
	                    roundValue = 5;
						break;
				}
				
				simpleWaves = new List<SimpleWave>();
				currentSimpleWave = new SimpleWave();
				waves = new List<List<MarketWave>>();
			}
			else if (State == State.DataLoaded)
			{
				HistoricalAnalysis();
				DrawWaves();
			}
		}

		protected override void OnBarUpdate()
		{
			//Add your custom indicator logic here.
			
			double open, close, high, low;
			DateTime time;
			
			high = High.GetValueAt(High.Count - 1);
			low = Low.GetValueAt(Low.Count - 1);
			time = Time.GetValueAt(Time.Count - 1);
			MainLogic(high, low, time, true);
		}
		
		private void HistoricalAnalysis()
		{
			int index = GetRecentWorkDayFirstBarIndex();
			
			currentSimpleWave.Highest = High.GetValueAt(index);
			currentSimpleWave.Lowest = Low.GetValueAt(index);
			currentSimpleWave.StartTime = Time.GetValueAt(index);
			currentSimpleWave.EndTime = Time.GetValueAt(index);
			if (Close.GetValueAt(index) > Open.GetValueAt(index))
				currentSimpleWave.Direction = MarketWaveDirection.Up;
			else if (Close.GetValueAt(index) < Open.GetValueAt(index))
				currentSimpleWave.Direction = MarketWaveDirection.Down;
			CalculateMinPercentValue();
			
			double high, low;
			DateTime time;
			for (int i = index + 1; i < Time.Count; i++)
			{
				high = High.GetValueAt(i);
				low = Low.GetValueAt(i);
				time = Time.GetValueAt(i);
				MainLogic(high, low, time, false);
			}
		}
		
		private void MainLogic(double high, double low, DateTime time, bool realtime)
		{
			if (currentSimpleWave.Direction == MarketWaveDirection.Up)
			{
				if (high > currentSimpleWave.Highest)
				{
					currentSimpleWave.Highest = high;
					currentSimpleWave.EndTime = time;
					CalculateMinPercentValue();
					if (realtime)
						RedrawCurrentWave();
				}
				if (low <= minPercentValue)
				{
					simpleWaves.Add(currentSimpleWave);
					currentSimpleWave = new SimpleWave();
					currentSimpleWave.Direction = MarketWaveDirection.Down;
					currentSimpleWave.Highest = simpleWaves.Last().Highest;
					currentSimpleWave.Lowest = low;
					currentSimpleWave.StartTime = simpleWaves.Last().EndTime;
					currentSimpleWave.EndTime = time;
					CalculateMinPercentValue();
					if (realtime)
						DrawWave(currentSimpleWave);
				}
			}
			else if (currentSimpleWave.Direction == MarketWaveDirection.Down)
			{
				if (low < currentSimpleWave.Lowest)
				{
					currentSimpleWave.Lowest = low;
					currentSimpleWave.EndTime = time;
					CalculateMinPercentValue();
					if (realtime)
						RedrawCurrentWave();
				}
				if (high >= minPercentValue)
				{
					simpleWaves.Add(currentSimpleWave);
					currentSimpleWave = new SimpleWave();
					currentSimpleWave.Direction = MarketWaveDirection.Up;
					currentSimpleWave.Highest = high;
					currentSimpleWave.Lowest = simpleWaves.Last().Lowest;
					currentSimpleWave.StartTime = simpleWaves.Last().EndTime;
					currentSimpleWave.EndTime = time;
					CalculateMinPercentValue();
					if (realtime)
						DrawWave(currentSimpleWave);
				}
			}
		}
		
//		private void ImpulseCorrectionAnalysis()
//		{
//			SimpleWave lastWave = simpleWaves[simpleWaves.Count - 1];
//			SimpleWave preLastWave = simpleWaves[simpleWaves.Count - 2];
//			if (lastWave.Direction == MarketWaveDirection.Up)
//			{
//				if (preLastWave.Highest < lastWave.Highest)
//				{
//					MarketWave impulse = new MarketWave(lastWave);
//					impulse.Type = MarketWaveType.Impulse;
//					impulse.StartIndex = simpleWaves[simpleWaves.Count - 1];
					
//					MarketWave correction = new MarketWave(preLastWave);
//					correction.Type = MarketWaveType.Correction;
//					correction.StartIndex = simpleWaves[simpleWaves.Count - 2];
					
//					waves.Add(new List<MarketWave> { correction, impulse });
//				}
//				else if (preLastWave.Highest > lastWave.Highest)
//				{
//					MarketWave correction = new MarketWave(lastWave);
//					correction.Type = MarketWaveType.Correction;
//					correction.StartIndex = simpleWaves[simpleWaves.Count - 1];
					
//					MarketWave impulse = new MarketWave(preLastWave);
//					impulse.Type = MarketWaveType.Impulse;
//					impulse.StartIndex = simpleWaves[simpleWaves.Count - 2];
					
//					waves.Add(new List<MarketWave> { impulse, correction });
//				}
//			}
//			else if (lastWave.Direction == MarketWaveDirection.Down)
//			{
//				if (preLastWave.Lowest > lastWave.Lowest)
//				{
//					MarketWave impulse = new MarketWave(lastWave);
//					impulse.Type = MarketWaveType.Impulse;
//					impulse.StartIndex = simpleWaves[simpleWaves.Count - 1];
					
//					MarketWave correction = new MarketWave(preLastWave);
//					correction.Type = MarketWaveType.Correction;
//					correction.StartIndex = simpleWaves[simpleWaves.Count - 2];
					
//					waves.Add(new List<MarketWave> { correction, impulse });
//				}
//				else if (preLastWave.Lowest < lastWave.Lowest)
//				{
//					MarketWave correction = new MarketWave(lastWave);
//					correction.Type = MarketWaveType.Correction;
//					impulse.StartIndex = simpleWaves[simpleWaves.Count - 2];
					
//					MarketWave impulse = new MarketWave(preLastWave);
//					impulse.Type = MarketWaveType.Impulse;
//					correction.StartIndex = simpleWaves[simpleWaves.Count - 1];
					
//					waves.Add(new List<MarketWave> { impulse, correction });
//				}
//			}
			
//			while (true)
//			{
//				MarketWave impulse, correction;
//				if (waves.Last()[0].Type == MarketWaveType.Impulse)
//					impulse = waves.Last()[0];
//			}
//		}
		
		private void DrawWaves()
		{
			for (int i = 0; i < simpleWaves.Count; i++)
				DrawWave(simpleWaves[i]);
			DrawWave(currentSimpleWave);
		}
		
		private void DrawWave(SimpleWave wave)
		{
			tagNumber++;
			string tag = "ZigZag_line_" + tagNumber.ToString();
			DateTime startTime = wave.StartTime;
			DateTime endTime = wave.EndTime;
			DashStyleHelper helper = DashStyleHelper.Solid;
			if (wave.Direction == MarketWaveDirection.Up)
			{
				double startY = wave.Lowest;
				double endY = wave.Highest;
				Draw.Line(this, tag, false, startTime, startY,
					endTime, endY, lineColor, helper, lineWidth);
			}
			else if (wave.Direction == MarketWaveDirection.Down)
			{
				double startY = wave.Highest;
				double endY = wave.Lowest;
				Draw.Line(this, tag, false, startTime, startY,
					endTime, endY, lineColor, helper, lineWidth);
			}
		}
		
		private void RedrawCurrentWave()
		{
			string tag = "ZigZag_line_" + tagNumber.ToString();
			RemoveDrawObject(tag);
			tagNumber--;
			DrawWave(currentSimpleWave);
		}
		
		public DateTime GetRecentWorkDay()
		{
			DateTime dateTime = Time.GetValueAt(Time.Count - 1);
			if (dateTime.ToString("HH:mm:ss") == "00:00:00")
				return Time.GetValueAt(Time.Count - 2);
			return dateTime;
		}
		
		public DateTime GetPreviousWorkDay()
		{
			DateTime previousWorkDay;
			DateTime recentDate = GetRecentWorkDay();
			DayOfWeek dayOfWeek = recentDate.DayOfWeek;
			
			if (dayOfWeek == DayOfWeek.Monday)
				previousWorkDay = recentDate.AddDays(-3);
			else
				previousWorkDay = recentDate.AddDays(-1);
			
			return previousWorkDay;
		}
		
		private int GetRecentWorkDayFirstBarIndex()
		{
			DateTime dateTime = GetRecentWorkDay();
			
			int index, bars_ago = 0;
			while (true)
			{
				index = Time.Count - 1 - bars_ago;
				if (Time.GetValueAt(index).Date < dateTime.Date)
					break;
				bars_ago++;
			}
			index += 1;
			DateTime indexTime = Time.GetValueAt(index);
			
			if (indexTime.ToString("HH:mm:ss") == "00:00:00")
				return index + 1;
			return index;
		}
		
		public int GetPreviousWorkDayFirstBarIndex()
		{
			DateTime dateTime = GetPreviousWorkDay();
			
			int index, bars_ago = 0;
			while (true)
			{
				index = Time.Count - 1 - bars_ago;
				if (Time.GetValueAt(index).Date < dateTime.Date)
					break;
				bars_ago++;
			}
			index += 1;
			DateTime indexTime = Time.GetValueAt(index);
			
			if (indexTime.ToString("HH:mm:ss") == "00:00:00")
				return index + 1;
			return index;
		}
		
		private void CalculateMinPercentValue()
		{
			if (currentSimpleWave.Direction == MarketWaveDirection.Up)
			{
				minPercentValue = currentSimpleWave.Highest
				- Math.Round((currentSimpleWave.Highest - currentSimpleWave.Lowest)
				/ 100 * minPercent, roundValue);
			}
			else if (currentSimpleWave.Direction == MarketWaveDirection.Down)
			{
				minPercentValue = currentSimpleWave.Lowest
				+ Math.Round((currentSimpleWave.Highest - currentSimpleWave.Lowest)
				/ 100 * minPercent, roundValue);
			}
		}
		
		#region Properties
		[Display(Name = "Minimal percent", Order = 0)]
		public double MinPercent
		{
			get { return minPercent; }
			set { minPercent = value; }
		}
		
		[XmlIgnore]
		[Display(Name = "Line color", Order = 1)]
		public Brush LineColor
		{
			get { return lineColor; }
			set { lineColor = value; }
		}
		
		[Browsable(false)]
		public string LineColorSerialize
		{
			get { return Serialize.BrushToString(LineColor); }
			set { LineColor = Serialize.StringToBrush(value); }
		}
		
		[Display(Name = "Line width", Order = 2)]
		public int LineWidth
		{
			get { return lineWidth; }
			set { lineWidth = value; }
		}
		#endregion
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private CombinatedWaves[] cacheCombinatedWaves;
		public CombinatedWaves CombinatedWaves()
		{
			return CombinatedWaves(Input);
		}

		public CombinatedWaves CombinatedWaves(ISeries<double> input)
		{
			if (cacheCombinatedWaves != null)
				for (int idx = 0; idx < cacheCombinatedWaves.Length; idx++)
					if (cacheCombinatedWaves[idx] != null &&  cacheCombinatedWaves[idx].EqualsInput(input))
						return cacheCombinatedWaves[idx];
			return CacheIndicator<CombinatedWaves>(new CombinatedWaves(), input, ref cacheCombinatedWaves);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.CombinatedWaves CombinatedWaves()
		{
			return indicator.CombinatedWaves(Input);
		}

		public Indicators.CombinatedWaves CombinatedWaves(ISeries<double> input )
		{
			return indicator.CombinatedWaves(input);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.CombinatedWaves CombinatedWaves()
		{
			return indicator.CombinatedWaves(Input);
		}

		public Indicators.CombinatedWaves CombinatedWaves(ISeries<double> input )
		{
			return indicator.CombinatedWaves(input);
		}
	}
}

#endregion
