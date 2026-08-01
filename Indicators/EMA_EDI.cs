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
namespace NinjaTrader.NinjaScript.Indicators.Метод_Тимощука
{
	public class EMA_EDI : Indicator
	{
		private double cA = 0;
		private double lcA = 0;
		
		private int emaPeriod = 21;
		
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"EMA Equilibrium Deviation Index (EDI)";
				Name										= "EMA EDI";
				Calculate									= Calculate.OnBarClose;
				IsOverlay									= false;
				DisplayInDataBox							= true;
				DrawOnPricePanel							= true;
				DrawHorizontalGridLines						= true;
				DrawVerticalGridLines						= true;
				PaintPriceMarkers							= true;
				ScaleJustification							= NinjaTrader.Gui.Chart.ScaleJustification.Right;
				//Disable this property if your indicator requires custom values that cumulate with each new market data event. 
				//See Help Guide for additional information.
				IsSuspendedWhileInactive					= true;
				
				AddLine(new Stroke(Brushes.Black, DashStyleHelper.Dash, 1), 0, "Equilibrium");
				AddPlot(new Stroke(Brushes.RoyalBlue, 2), PlotStyle.Line, "EDI");
			}
			else if (State == State.Configure)
			{
			}
		}

		protected override void OnBarUpdate()
		{
			if (CurrentBar > 1)
			{
				double v = Math.Abs(EMA(emaPeriod)[0] - EMA(emaPeriod)[1]);
				double v1 = Math.Abs(EMA(emaPeriod)[1] - EMA(emaPeriod)[2]);
				double a = v - v1;
				CumulateA(a);
				if (cA < 0)
					cA = 0;
				Values[0][0] = cA;
			}
		}
		
		private void CumulateA(double a)
		{
			if (cA == 0 && lcA == 0)
			{
				cA = a;
				lcA = a;
				return;
			}
			if (lcA < 0 && a > 0)
			{
				cA += a;
				lcA = a;
				return;
			}
			if (lcA > 0 && a < 0)
			{
				cA += a;
				lcA = a;
				return;
			}
			if (lcA > 0 && a > 0)
			{
				lcA += a;
				cA += lcA;
				return;
			}
			if (lcA < 0 && a < 0)
			{
				lcA += a;
				cA += lcA;
				return;
			}
		}
		
		#region Properties
		[Display(Name = "EMA period")]
		public int EmaPeriod
		{
			get { return emaPeriod; }
			set { emaPeriod = value; }
		}
		#endregion
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private Метод_Тимощука.EMA_EDI[] cacheEMA_EDI;
		public Метод_Тимощука.EMA_EDI EMA_EDI()
		{
			return EMA_EDI(Input);
		}

		public Метод_Тимощука.EMA_EDI EMA_EDI(ISeries<double> input)
		{
			if (cacheEMA_EDI != null)
				for (int idx = 0; idx < cacheEMA_EDI.Length; idx++)
					if (cacheEMA_EDI[idx] != null &&  cacheEMA_EDI[idx].EqualsInput(input))
						return cacheEMA_EDI[idx];
			return CacheIndicator<Метод_Тимощука.EMA_EDI>(new Метод_Тимощука.EMA_EDI(), input, ref cacheEMA_EDI);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.Метод_Тимощука.EMA_EDI EMA_EDI()
		{
			return indicator.EMA_EDI(Input);
		}

		public Indicators.Метод_Тимощука.EMA_EDI EMA_EDI(ISeries<double> input )
		{
			return indicator.EMA_EDI(input);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.Метод_Тимощука.EMA_EDI EMA_EDI()
		{
			return indicator.EMA_EDI(Input);
		}

		public Indicators.Метод_Тимощука.EMA_EDI EMA_EDI(ISeries<double> input )
		{
			return indicator.EMA_EDI(input);
		}
	}
}

#endregion
