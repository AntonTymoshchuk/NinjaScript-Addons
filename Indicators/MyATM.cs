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
	public class MyATM : Indicator
	{
		private AccountNicknames accountNickname = AccountNicknames.AnatoliiTymoshchuk283327;
		private Account myAccount;
		
		private int currentQuantity = 0;
		private List<string> orderNames;
		private MarketPosition marketPosition;
		
		private double tpOffset = 20;
		private double slOffset = 20;
		
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Введите описание новой пользовательской Индикатор здесь.";
				Name										= "Мой ATM";
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
			}
			else if (State == State.Configure)
			{
				orderNames = new List<string>();
			}
			else if (State == State.DataLoaded)
			{
				myAccount = GetAccount();
				myAccount.OrderUpdate += OnOrderUpdate;
				myAccount.PositionUpdate += OnPositionUpdate;
				
				// Сделать взаимо-установку счета с OrderViewer.
				// Сделать опцию постановки S/L и T/P с OCO именем.
			}
			else if (State == State.Terminated)
			{
				try
				{
					myAccount.OrderUpdate -= OnOrderUpdate;
					myAccount.PositionUpdate -= OnPositionUpdate;
				}
				catch (NullReferenceException)
				{ }
			}
		}

		protected override void OnBarUpdate()
		{
			//Добавьте логику пользовательского indicator здесь.
		}
		
		private void OnOrderUpdate(object sender, OrderEventArgs e)
		{
			if (e.OrderState == OrderState.Filled || e.OrderState == OrderState.Cancelled)
			{
				string toCancelName = string.Empty;
				if (e.Order.Name.StartsWith("Target"))
				{
					int number = Convert.ToInt32(e.Order.Name.Remove(0, 6));
					toCancelName = "Stop" + number.ToString();
				}
				else if (e.Order.Name.StartsWith("Stop"))
				{
					int number = Convert.ToInt32(e.Order.Name.Remove(0, 4));
					toCancelName = "Target" + number.ToString();
				}
				
				if (toCancelName != string.Empty)
				{
					Order orderToCancel = GetOrderByName(toCancelName);
					if (orderToCancel != null)
						myAccount.Cancel(new [] { orderToCancel });
				}
			}
		}
		
		private void OnPositionUpdate(object sender, PositionEventArgs e)
		{
			Position myPosition = GetPosition();
			if (myPosition == null)
				CancelMyOrders();
			else
			{
				if (myPosition.MarketPosition != marketPosition)
					CancelMyOrders();
				
				marketPosition = myPosition.MarketPosition;
				double limitPrice = 0; double stopPrice = 0;
				int quantity = myPosition.Quantity - currentQuantity;
				OrderAction orderAction = OrderAction.Buy;
				int ordersNumber = GetMaxStopTargetNumber() + 1;
				string tpName = "Target" + ordersNumber.ToString();
				string slName = "Stop" + ordersNumber.ToString();
				currentQuantity = myPosition.Quantity;
				
				if (marketPosition == MarketPosition.Long)
				{
					limitPrice = myPosition.AveragePrice + tpOffset * TickSize;
					stopPrice = myPosition.AveragePrice - slOffset * TickSize;
					orderAction = OrderAction.Sell;
				}
				else if (marketPosition == MarketPosition.Short)
				{
					limitPrice = myPosition.AveragePrice - tpOffset * TickSize;
					stopPrice = myPosition.AveragePrice + slOffset * TickSize;
				}
				
				Order tpOrder = myAccount.CreateOrder(Instrument, orderAction,
				OrderType.Limit, OrderEntry.Automated, TimeInForce.Gtc, quantity,
				limitPrice, 0, "", tpName, DateTime.MaxValue, null);
				
				Order slOrder = myAccount.CreateOrder(Instrument, orderAction,
				OrderType.StopMarket, OrderEntry.Automated, TimeInForce.Gtc, quantity,
				0, stopPrice, "", slName, DateTime.MaxValue, null);
				
				myAccount.Submit(new [] { tpOrder, slOrder });
				orderNames.AddRange(new [] { tpName, slName });
			}
		}
		
		private void CancelMyOrders()
		{
			currentQuantity = 0;
			List<Order> ordersToCancel = new List<Order>();
			for (int i = 0; i < orderNames.Count; i++)
			{
				Order order = GetOrderByName(orderNames[i]);
				ordersToCancel.Add(order);
			}
			myAccount.Cancel(ordersToCancel);
			orderNames.Clear();
		}
		
		private int GetMaxStopTargetNumber()
		{
			List<Order> orders = GetOrdersOfInstrument();
			int maxNumber = 0;
			for (int i = 0; i < orders.Count; i++)
			{
				if (orders[i].Name.StartsWith("Target"))
				{
					int number = Convert.ToInt32(orders[i].Name.Remove(0, 6));
					if (number > maxNumber)
						maxNumber = number;
				}
				else if (orders[i].Name.StartsWith("Stop"))
				{
					int number = Convert.ToInt32(orders[i].Name.Remove(0, 4));
					if (number > maxNumber)
						maxNumber = number;
				}
			}
			return maxNumber;
		}
		
		private List<Order> GetOrdersOfInstrument()
		{
			List<Order> orders = new List<Order>();
			for (int i = 0; i < myAccount.Orders.Count; i++)
			{
				if (myAccount.Orders[i].Instrument == Instrument)
					orders.Add(myAccount.Orders[i]);
			}
			return orders;
		}
		
		private Order GetOrderByName(string orderName)
		{
			foreach (Order order in myAccount.Orders)
			{
				if (order.Name == orderName && order.Instrument == Instrument)
					return order;
			}
			return null;
		}
		
		private Position GetPosition()
		{
			for (int i = 0; i < myAccount.Positions.Count; i++)
			{
				if (myAccount.Positions[i].Instrument == Instrument)
					return myAccount.Positions[i];
			}
			return null;
		}
		
		private Account GetAccount()
		{
			string accountDisplayName = accountNickname.ToString();
			if (accountDisplayName == "AnatoliiTymoshchuk283327")
				accountDisplayName = "Anatolii Tymoshchuk!Mirus!283327";
			
			for (int i = 0; i < Account.All.Count; i++)
			{
				if (Account.All[i].DisplayName == accountDisplayName)
					return Account.All[i];
			}
			return null;
		}
		
		public OrderViewer GetOrderViewer()
		{
			for (int i = 0; i < ChartControl.Indicators.Count; i++)
			{
				if (ChartControl.Indicators[i].Name == "Визуализатор ордеров")
					return ChartControl.Indicators[i] as OrderViewer;
			}
			return null;
		}
		
		#region Properties
		[Display(Name = "Счет", GroupName = "1. Основные настройки", Order = 0)]
		public AccountNicknames AccountNickname
		{
			get { return accountNickname; }
			set { accountNickname = value; }
		}
		
		[Display(Name = "Смещение T/P", GroupName = "1. Основные настройки", Order = 1)]
		public double TpOffset
		{
			get { return tpOffset; }
			set { tpOffset = value; }
		}
		
		[Display(Name = "Смещение S/L", GroupName = "1. Основные настройки", Order = 2)]
		public double SlOffset
		{
			get { return slOffset; }
			set { slOffset = value; }
		}
		#endregion
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private MyATM[] cacheMyATM;
		public MyATM MyATM()
		{
			return MyATM(Input);
		}

		public MyATM MyATM(ISeries<double> input)
		{
			if (cacheMyATM != null)
				for (int idx = 0; idx < cacheMyATM.Length; idx++)
					if (cacheMyATM[idx] != null &&  cacheMyATM[idx].EqualsInput(input))
						return cacheMyATM[idx];
			return CacheIndicator<MyATM>(new MyATM(), input, ref cacheMyATM);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.MyATM MyATM()
		{
			return indicator.MyATM(Input);
		}

		public Indicators.MyATM MyATM(ISeries<double> input )
		{
			return indicator.MyATM(input);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.MyATM MyATM()
		{
			return indicator.MyATM(Input);
		}

		public Indicators.MyATM MyATM(ISeries<double> input )
		{
			return indicator.MyATM(input);
		}
	}
}

#endregion
