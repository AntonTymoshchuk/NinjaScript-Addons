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
	public enum FractalDirection
	{
		Up = 0,
		Down = 1
	}
	
	public class FractalIndicator : Indicator
	{
		private bool historicalAnalysis = false;
		private int tagId = 1;
		
		private bool analyseHistoricalData = true;
		private int fontSize = 12;
		private Brush fractalTextColor = Brushes.Black;
		
		private bool isFractal = false;
		private FractalDirection fractalDirection;
		
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Enter the description for your new custom Indicator here.";
				Name										= "Fractal Indicator";
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
			}
		}

		protected override void OnBarUpdate()
		{
			//Add your custom indicator logic here.
			
			if (State < State.Realtime)
				return;
			
			if (historicalAnalysis == true)
			{
				for (int i = Time.Count - 5; i > 0; i--)
				{
					CheckFractal(i, i + 1, i + 2, i + 3);
					if (isFractal == true)
						DrawFractalInfo(i);
				}
				historicalAnalysis = false;
			}
			CheckFractal(0, 1, 2, 3);
			if (isFractal == true)
				DrawFractalInfo(0);
		}
		
		private void CheckFractal(int rb, int cb, int lb1, int lb2)
		{
			if (High[lb1] < High[cb] && High[lb2] < High[cb] && High[rb] < High[cb] && Open[rb] > Close[rb])
			{
				isFractal = true;
				fractalDirection = FractalDirection.Down;
				return;
			}
			if (Low[lb1] > Low[cb] && Low[lb2] > Low[cb] && Low[rb] > Low[cb] && Close[rb] > Open[rb])
			{
				isFractal = true;
				fractalDirection = FractalDirection.Up;
				return;
			}
			isFractal = false;
			
			if (analyseHistoricalData == false)
			{
				string tag = "fractal_info_" + tagId.ToString();
				RemoveDrawObject(tag);
			}
		}
		
		public void DrawFractalInfo(int rb)
		{
			string tag = "fractal_info_" + tagId.ToString();
			if (analyseHistoricalData == true)
				tagId += 1;
			string text = string.Empty;
			if (fractalDirection == FractalDirection.Up)
				text += "▲";
			else
				text += "▼";
			text += " Fractal " + fractalDirection.ToString();
			DateTime time = Time[rb];
			double price = Close[rb];
			SimpleFont font = new SimpleFont();
			font.Size = fontSize;
			Draw.Text(this, tag, true, text, time, price, 0, fractalTextColor,
				font, TextAlignment.Left, Brushes.Transparent, Brushes.Transparent, 0);
		}
		
		#region Properties
		[Display(Name = "Analyse historical data", Order = 0)]
		public bool AnalyseHistoricalData
		{
			get { return analyseHistoricalData; }
			set
			{
				analyseHistoricalData = value;
				historicalAnalysis = value;
			}
		}
		
		[Display(Name = "Font size", Order = 1)]
		public int FontSize
		{
			get { return fontSize; }
			set { fontSize = value; }
		}
		
		[XmlIgnore]
		[Display(Name = "Text color", Order = 2)]
		public Brush FractalTextColor
		{
			get { return fractalTextColor; }
			set { fractalTextColor = value; }
		}
		
		[Browsable(false)]
		public string FractalTextColorSerialize
		{
			get { return Serialize.BrushToString(FractalTextColor); }
			set { FractalTextColor = Serialize.StringToBrush(value); }
		}
		#endregion
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private FractalIndicator[] cacheFractalIndicator;
		public FractalIndicator FractalIndicator()
		{
			return FractalIndicator(Input);
		}

		public FractalIndicator FractalIndicator(ISeries<double> input)
		{
			if (cacheFractalIndicator != null)
				for (int idx = 0; idx < cacheFractalIndicator.Length; idx++)
					if (cacheFractalIndicator[idx] != null &&  cacheFractalIndicator[idx].EqualsInput(input))
						return cacheFractalIndicator[idx];
			return CacheIndicator<FractalIndicator>(new FractalIndicator(), input, ref cacheFractalIndicator);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.FractalIndicator FractalIndicator()
		{
			return indicator.FractalIndicator(Input);
		}

		public Indicators.FractalIndicator FractalIndicator(ISeries<double> input )
		{
			return indicator.FractalIndicator(input);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.FractalIndicator FractalIndicator()
		{
			return indicator.FractalIndicator(Input);
		}

		public Indicators.FractalIndicator FractalIndicator(ISeries<double> input )
		{
			return indicator.FractalIndicator(input);
		}
	}
}

#endregion
