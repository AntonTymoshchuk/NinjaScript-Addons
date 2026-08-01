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
using System.Net;
using System.Net.Sockets;
#endregion

//This namespace holds Indicators in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Indicators
{
	public class IndicatorDisplay : Indicator
	{
		private int port;
		
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Введите описание новой пользовательской Индикатор здесь.";
				Name										= "IndicatorDisplay";
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
			}
			else if (State == State.Configure)
			{
			}
			else if (State == State.DataLoaded)
			{
				myThread = new Thread(Method);
				myThread.IsBackground = true;
				myThread.Start();
			}
			else if (State == State.Terminated)
			{
				myThread.Abort();
				tcpListener.Stop();
			}
		}

		protected override void OnBarUpdate()
		{
			//Добавьте логику пользовательского indicator здесь.
		}
		
		private Thread myThread;
		private TcpListener tcpListener;
		
		private void Method()
		{
			try
			{
				tcpListener = new TcpListener(IPAddress.Loopback, port);
				tcpListener.Start();
				
				while (true)
				{
					TcpClient tcpClient = tcpListener.AcceptTcpClient();
					NetworkStream incomingStream = tcpClient.GetStream();
					string message = LocalNetworkManager.ReadNetworkStream(incomingStream);
					string[] columns = message.Split(';');
					
					if (message == "RemoveDrawObjects")
						RemoveDrawObjects();
					if (columns[0] == "Draw")
					{
						string lineTag = columns[1];
						DateTime startTime = Convert.ToDateTime(columns[2]);
						DateTime endTime = Convert.ToDateTime(columns[3]);
						double highest = Convert.ToDouble(columns[4]);
						double lowest = Convert.ToDouble(columns[5]);
						ChartLineDirection direction = (ChartLineDirection) Enum.Parse(typeof(ChartLineDirection), columns[6]);
						Brush brush = Serialize.StringToBrush(columns[7]);
						DashStyleHelper style = (DashStyleHelper) Enum.Parse(typeof(DashStyleHelper), columns[8]);
						int width = Convert.ToInt32(columns[9]);
						
						if (direction == ChartLineDirection.Up)
							Draw.Line(this, lineTag, false, startTime, lowest, endTime, highest, brush, style, width);
						else if (direction == ChartLineDirection.Down)
							Draw.Line(this, lineTag, false, startTime, highest, endTime, lowest, brush, style, width);
					}
					if (columns[0] == "Remove")
					{
						string lineTag = columns[1];
						RemoveDrawObject(lineTag);
					}
				}
			}
			catch (Exception exception)
			{
				if (exception.GetType() != typeof(ThreadAbortException))
					ReportException(exception, DateTime.Now);
			}
		}
		
		private void ReportException(Exception exception, DateTime time)
		{
			Print("Indicator exception at: " + Instrument.FullName + ", " + time.ToString());
			Print(exception.Message);
			Print(exception.StackTrace);
			Print(exception.Data);
			Print(string.Empty);
		}
		
		[Display(Name = "Порт", Order = 0)]
		public int Port
		{
			get { return port; }
			set { port = value; }
		}
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private IndicatorDisplay[] cacheIndicatorDisplay;
		public IndicatorDisplay IndicatorDisplay()
		{
			return IndicatorDisplay(Input);
		}

		public IndicatorDisplay IndicatorDisplay(ISeries<double> input)
		{
			if (cacheIndicatorDisplay != null)
				for (int idx = 0; idx < cacheIndicatorDisplay.Length; idx++)
					if (cacheIndicatorDisplay[idx] != null &&  cacheIndicatorDisplay[idx].EqualsInput(input))
						return cacheIndicatorDisplay[idx];
			return CacheIndicator<IndicatorDisplay>(new IndicatorDisplay(), input, ref cacheIndicatorDisplay);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.IndicatorDisplay IndicatorDisplay()
		{
			return indicator.IndicatorDisplay(Input);
		}

		public Indicators.IndicatorDisplay IndicatorDisplay(ISeries<double> input )
		{
			return indicator.IndicatorDisplay(input);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.IndicatorDisplay IndicatorDisplay()
		{
			return indicator.IndicatorDisplay(Input);
		}

		public Indicators.IndicatorDisplay IndicatorDisplay(ISeries<double> input )
		{
			return indicator.IndicatorDisplay(input);
		}
	}
}

#endregion
