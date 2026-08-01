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
using System.Windows.Forms;
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
	public enum TradeActionType
	{
		Выставить = 0,
		Редактировать = 1,
		Отменить = 2
	}
	
	public class IndicatorTrading : Indicator
	{
		private TradeActionType tradeActionType = TradeActionType.Выставить;
		private string accountDisplayName;
		
		private OrderAction orderAction;
		private OrderType orderType;
		private OrderEntry orderEntry;
		private TimeInForce timeInForce;
		private int quantity1;
		private double limitPrice1;
		private double stopPrice1;
		private string oco;
		private string name;
		
		private long changeId;
		private int quantity2;
		private double limitPrice2;
		private double stopPrice2;
		
		private long cancelId;
		
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Введите описание новой пользовательской Индикатор здесь.";
				Name										= "Торговый индикатор";
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
			else if (State == State.Realtime)
			{
				try
				{
					Account myAccount = GetAccount(accountDisplayName);
					Order myOrder;
					switch (tradeActionType)
					{
						case TradeActionType.Выставить:
							myOrder = myAccount.CreateOrder(Instrument,
							orderAction, orderType, orderEntry, timeInForce, quantity1,
							limitPrice1, stopPrice1, oco, name, DateTime.MaxValue, null);
							myAccount.Submit(new [] { myOrder });
							break;
						case TradeActionType.Редактировать:
							myOrder = GetOrder(myAccount, changeId);
							myOrder.QuantityChanged = quantity2;
							myOrder.LimitPriceChanged = limitPrice2;
							myOrder.StopPriceChanged = stopPrice2;
							myAccount.Change(new [] { myOrder });
							break;
						case TradeActionType.Отменить:
							myOrder = GetOrder(myAccount, cancelId);
							myAccount.Cancel(new [] { myOrder });
							break;
					}
				}
				catch (Exception e)
				{
					string message = e.Message + '\n' + e.StackTrace +
						'\n' + e.Source + '\n' + e.TargetSite + '\n' + e.Data;
					MessageBoxButtons buttons = MessageBoxButtons.OK;
					System.Windows.Forms.MessageBox.Show(message, "Error", buttons);
				}
			}
		}
		
		public Account GetAccount(string displayName)
		{
			for (int i = 0; i < Account.All.Count; i++)
			{
				if (Account.All[i].DisplayName == displayName)
					return Account.All[i];
			}
			return null;
		}
		
		public Order GetOrder(Account myAccount, long orderId)
		{
			for (int i = 0; i < myAccount.Orders.Count; i++)
			{
				if (myAccount.Orders[i].Id == orderId)
					return myAccount.Orders[i];
			}
			return null;
		}

		protected override void OnBarUpdate()
		{
			//Добавьте логику пользовательского indicator здесь.
		}
		
		#region Properties
		[Display(Name = "Действие с ордером", GroupName = "1. Основное", Order = 0)]
		public TradeActionType TradeActionTypeProp
		{
			get { return tradeActionType; }
			set { tradeActionType = value; }
		}
		
		[Display(Name = "Название аккаунта", GroupName = "1. Основное", Order = 1)]
		public string AccountDisplayNameProp
		{
			get { return accountDisplayName; }
			set { accountDisplayName = value; }
		}
		
		[Display(Name = "Action", GroupName = "2. Выставить ордер", Order = 0)]
		public OrderAction OrderActionProp
		{
			get { return orderAction; }
			set { orderAction = value; }
		}
		
		[Display(Name = "Type", GroupName = "2. Выставить ордер", Order = 1)]
		public OrderType OrderTypeProp
		{
			get { return orderType; }
			set { orderType = value; }
		}
		
		[Display(Name = "Entry", GroupName = "2. Выставить ордер", Order = 2)]
		public OrderEntry OrderEntryProp
		{
			get { return orderEntry; }
			set { orderEntry = value; }
		}
		
		[Display(Name = "Time in force", GroupName = "2. Выставить ордер", Order = 3)]
		public TimeInForce TimeInForceProp
		{
			get { return timeInForce; }
			set { timeInForce = value; }
		}
		
		[Display(Name = "Количество", GroupName = "2. Выставить ордер", Order = 4)]
		public int Quantity1Prop
		{
			get { return quantity1; }
			set { quantity1 = value; }
		}
		
		[Display(Name = "Цена лимит", GroupName = "2. Выставить ордер", Order = 5)]
		public double LimitPrice1Prop
		{
			get { return limitPrice1; }
			set { limitPrice1 = value; }
		}
		
		[Display(Name = "Цена стоп", GroupName = "2. Выставить ордер", Order = 6)]
		public double StopPrice1Prop
		{
			get { return stopPrice1; }
			set { stopPrice1 = value; }
		}
		
		[Display(Name = "OCO", GroupName = "2. Выставить ордер", Order = 7)]
		public string OcoProp
		{
			get { return oco; }
			set { oco = value; }
		}
		
		[Display(Name = "Имя ордера", GroupName = "2. Выставить ордер", Order = 8)]
		public string NameProp
		{
			get { return name; }
			set { name = value; }
		}
		
		[Display(Name = "Id ордера", GroupName = "3. Редактировать ордер", Order = 0)]
		public long ChangeIdProp
		{
			get { return changeId; }
			set { changeId = value; }
		}
		
		[Display(Name = "Количество", GroupName = "3. Редактировать ордер", Order = 1)]
		public int Quantity2Prop
		{
			get { return quantity2; }
			set { quantity2 = value; }
		}
		
		[Display(Name = "Цена лимит", GroupName = "3. Редактировать ордер", Order = 2)]
		public double LimitPrice2Prop
		{
			get { return limitPrice2; }
			set { limitPrice2 = value; }
		}
		
		[Display(Name = "Цена стоп", GroupName = "3. Редактировать ордер", Order = 3)]
		public double StopPrice2Prop
		{
			get { return stopPrice2; }
			set { stopPrice2 = value; }
		}
		
		[Display(Name = "Id ордера", GroupName = "4. Отменить ордер", Order = 0)]
		public long CancelIdProp
		{
			get { return cancelId; }
			set { cancelId = value; }
		}
		#endregion
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private IndicatorTrading[] cacheIndicatorTrading;
		public IndicatorTrading IndicatorTrading()
		{
			return IndicatorTrading(Input);
		}

		public IndicatorTrading IndicatorTrading(ISeries<double> input)
		{
			if (cacheIndicatorTrading != null)
				for (int idx = 0; idx < cacheIndicatorTrading.Length; idx++)
					if (cacheIndicatorTrading[idx] != null &&  cacheIndicatorTrading[idx].EqualsInput(input))
						return cacheIndicatorTrading[idx];
			return CacheIndicator<IndicatorTrading>(new IndicatorTrading(), input, ref cacheIndicatorTrading);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.IndicatorTrading IndicatorTrading()
		{
			return indicator.IndicatorTrading(Input);
		}

		public Indicators.IndicatorTrading IndicatorTrading(ISeries<double> input )
		{
			return indicator.IndicatorTrading(input);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.IndicatorTrading IndicatorTrading()
		{
			return indicator.IndicatorTrading(Input);
		}

		public Indicators.IndicatorTrading IndicatorTrading(ISeries<double> input )
		{
			return indicator.IndicatorTrading(input);
		}
	}
}

#endregion
