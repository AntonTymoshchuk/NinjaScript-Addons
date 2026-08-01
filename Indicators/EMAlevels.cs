#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Net.Sockets;
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
	public class EMAlevels : Indicator
	{
		private int agent4HPort = 7770;
		
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Введите описание новой пользовательской Индикатор здесь.";
				Name										= "EMAlevels";
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
		}

		protected override void OnBarUpdate()
		{
			//Добавьте логику пользовательского indicator здесь.
			
			if (State != State.Realtime)
				return;
			
			string[] todaysEmaValues = AskAgent(agent4HPort, "GET TODAYS EMA VALUES").Split('\n');
			List<DateTime> dateTimes = new List<DateTime>();
			double emaValue; DateTime endTime;
			
			foreach (string todaysEmaValue in todaysEmaValues)
				dateTimes.Add(Convert.ToDateTime(todaysEmaValue.Split(';')[0]));
			
			for (int i = 0; i < todaysEmaValues.Length; i++)
			{
				emaValue = Convert.ToDouble(todaysEmaValues[i].Split(';')[1]);
				if (i == todaysEmaValues.Length - 1)
					endTime = Time[0];
				else
					endTime = dateTimes[i + 1];
				Draw.Line(this, string.Format("EMAlevel_{0}", i), false, dateTimes[i],
					emaValue, endTime, emaValue, Brushes.Blue, DashStyleHelper.Solid, 2);
			}
		}
		
		public string AskAgent(int port, string query)
		{
			TcpClient tcpClient = new TcpClient();
			tcpClient.Connect(IPAddress.Loopback, port);
			NetworkStream networkStream = tcpClient.GetStream();
			WriteNetworkStream(networkStream, query);
			string response = ReadNetworkStream(networkStream);
			networkStream.Close();
			tcpClient.Close();
			return response;
		}
		
		public void WriteNetworkStream(NetworkStream networkStream, string text)
		{
			byte[] bytes = Encoding.Unicode.GetBytes(text);
			networkStream.Write(bytes, 0, bytes.Length);
		}
		
		public string ReadNetworkStream(NetworkStream networkStream)
		{
			byte[] buffer = new byte[1024];
			int bytesRead = networkStream.Read(buffer, 0, buffer.Length);
			return Encoding.Unicode.GetString(buffer, 0, bytesRead);
		}
		
		#region Properties
		[Display(Name = "Порт связи с Agent 4H", GroupName = "Настройки", Order = 0)]
		public int Agent4HPort
		{
			get { return agent4HPort; }
			set { agent4HPort = value; }
		}
		#endregion
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private EMAlevels[] cacheEMAlevels;
		public EMAlevels EMAlevels()
		{
			return EMAlevels(Input);
		}

		public EMAlevels EMAlevels(ISeries<double> input)
		{
			if (cacheEMAlevels != null)
				for (int idx = 0; idx < cacheEMAlevels.Length; idx++)
					if (cacheEMAlevels[idx] != null &&  cacheEMAlevels[idx].EqualsInput(input))
						return cacheEMAlevels[idx];
			return CacheIndicator<EMAlevels>(new EMAlevels(), input, ref cacheEMAlevels);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.EMAlevels EMAlevels()
		{
			return indicator.EMAlevels(Input);
		}

		public Indicators.EMAlevels EMAlevels(ISeries<double> input )
		{
			return indicator.EMAlevels(input);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.EMAlevels EMAlevels()
		{
			return indicator.EMAlevels(Input);
		}

		public Indicators.EMAlevels EMAlevels(ISeries<double> input )
		{
			return indicator.EMAlevels(input);
		}
	}
}

#endregion
