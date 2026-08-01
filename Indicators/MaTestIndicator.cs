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
	public class MaTestIndicator : Indicator
	{
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Enter the description for your new custom Indicator here.";
				Name										= "MaTestIndicator";
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
			else if (State == State.Historical)
			{
				Analysis();
			}
		}

		protected override void OnBarUpdate()
		{
			//Add your custom indicator logic here.
		}
		
		private void Analysis()
		{
			int yes = 0, no = 0;
			for (int i = 100; i < Time.Count; i++)
			{
				if (Close.GetValueAt(i) > Open.GetValueAt(i) && Close.GetValueAt(i - 1) < Open.GetValueAt(i - 1) && GetMADirection(i) == true)
				{
					if (Close.GetValueAt(i + 1) > Open.GetValueAt(i + 1))
						yes += 1;
					else
						no += 1;
				}
			}
			Print(yes.ToString());
			Print(no.ToString());
		}
		
		private bool GetMADirection(int index)
		{
			int n = 0;
			double p;
			for (int i = 0; i < 4; i++)
			{
				if (EMA(89)[Time.Count - index] > EMA(89)[Time.Count - (index + 1)])
					n += 1;
			}
			if (n == 4)
				return true;
			return false;
		}
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private MaTestIndicator[] cacheMaTestIndicator;
		public MaTestIndicator MaTestIndicator()
		{
			return MaTestIndicator(Input);
		}

		public MaTestIndicator MaTestIndicator(ISeries<double> input)
		{
			if (cacheMaTestIndicator != null)
				for (int idx = 0; idx < cacheMaTestIndicator.Length; idx++)
					if (cacheMaTestIndicator[idx] != null &&  cacheMaTestIndicator[idx].EqualsInput(input))
						return cacheMaTestIndicator[idx];
			return CacheIndicator<MaTestIndicator>(new MaTestIndicator(), input, ref cacheMaTestIndicator);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.MaTestIndicator MaTestIndicator()
		{
			return indicator.MaTestIndicator(Input);
		}

		public Indicators.MaTestIndicator MaTestIndicator(ISeries<double> input )
		{
			return indicator.MaTestIndicator(input);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.MaTestIndicator MaTestIndicator()
		{
			return indicator.MaTestIndicator(Input);
		}

		public Indicators.MaTestIndicator MaTestIndicator(ISeries<double> input )
		{
			return indicator.MaTestIndicator(input);
		}
	}
}

#endregion
