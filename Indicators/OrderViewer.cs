#region Using declarations
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
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
	public enum AccountNicknames
	{
		AnatoliiTymoshchuk283327 = 1,
		Sim101 = 2,
		DEMO1678662 = 3,
		Playback101 = 4
	}
	
	public class OrderViewer : Indicator
	{
		private Grid domGrid;
		private QuantityUpDown quantityUpDown;
		private Button buyMktButton, sellMktButton;
		private Button reverseButton, closeButton;
		private Label positionQuantityLabel, positionAfpLabel, positionUplLabel;
		private Label askBidLabel, askBidVolumeLabel;
		private bool displayDom = true;
		private int domWidth = 202;
		
		private AccountNicknames accountNickname = AccountNicknames.AnatoliiTymoshchuk283327;
		private Account myAccount;
		private Brush accountBrush = Brushes.Red;
		private int accountFontSize = 20;
		
		private Brush limitOrderBrush = Brushes.Cyan;
		private Brush mitOrderBrush = Brushes.SpringGreen;
		private Brush stopMarketOrderBrush = Brushes.Pink;
		private Brush stopLimitOrderBrush = Brushes.Violet;
		private Brush takeProfitOrderBrush = Brushes.Lime;
		private Brush stopLossOrderBrush = Brushes.Red;
		private Brush positionBrush = Brushes.BurlyWood;
		private int lineWidth = 2;
		private bool fillWithWhiteBrush = true;
		
		private List<string> positionTags = new List<string>();
		private Dictionary<string, List<Order>> sameOrdersSaved;
		
		private MenuItem sellLimitItem, sellMitItem, buyStopMarketItem, buyStopLimitItem;
		private MenuItem buyLimitItem, buyMitItem, sellStopMarketItem, sellStopLimitItem;
		private MenuItem cancelOrderItem, increaseQuantityItem, decreaseQuantityItem;
		private double cmOpenPrice;
		private Order cmSelectedOrder;
		private Order orderToReplace = null;
		private int quantity = 1;
		
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Введите описание новой пользовательской Индикатор здесь.";
				Name										= "Визуализатор ордеров";
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
				sellLimitItem = new MenuItem();
				sellLimitItem.Click += SellLimitItem_Click;
				sellMitItem = new MenuItem();
				sellMitItem.Click += SellMitItem_Click;
				buyStopMarketItem = new MenuItem();
				buyStopMarketItem.Click += BuyStopMarketItem_Click;
				buyStopLimitItem = new MenuItem();
				buyStopLimitItem.Click += BuyStopLimitItem_Click;
				
				buyLimitItem = new MenuItem();
				buyLimitItem.Click += BuyLimitItem_Click;
				buyMitItem = new MenuItem();
				buyMitItem.Click += BuyMitItem_Click;
				sellStopMarketItem = new MenuItem();
				sellStopMarketItem.Click += SellStopMarketItem_Click;
				sellStopLimitItem = new MenuItem();
				sellStopLimitItem.Click += SellStopLimitItem_Click;
				
				cancelOrderItem = new MenuItem();
				cancelOrderItem.Click += CancelOrderItem_Click;
				increaseQuantityItem = new MenuItem();
				increaseQuantityItem.Click += IncreaseQuantityItem_Click;
				decreaseQuantityItem = new MenuItem();
				decreaseQuantityItem.Click += DecreaseQuantityItem_Click;
				
				sameOrdersSaved = new Dictionary<string, List<Order>>();
			}
			
			else if (State == State.DataLoaded)
			{
				myAccount = GetAccount();
				myAccount.OrderUpdate += OnOrderUpdate;
				myAccount.PositionUpdate += OnPositionUpdate;
			}
			
			else if (State == State.Historical)
			{
			    ChartControl.Dispatcher.InvokeAsync((() =>
			    {
			        if (UserControlCollection.Contains(domGrid))
			        	return;
					
			        domGrid = new Grid
			        {
				        HorizontalAlignment = HorizontalAlignment.Left,
				        VerticalAlignment = VerticalAlignment.Top,
			        };
			 
			        RowDefinition row1 = new RowDefinition();
			        domGrid.RowDefinitions.Add(row1);
					RowDefinition row2 = new RowDefinition();
			        domGrid.RowDefinitions.Add(row2);
					RowDefinition row3 = new RowDefinition();
			        domGrid.RowDefinitions.Add(row3);
					RowDefinition row4 = new RowDefinition();
			        domGrid.RowDefinitions.Add(row4);
					RowDefinition row5 = new RowDefinition();
			        domGrid.RowDefinitions.Add(row5);
					RowDefinition row6 = new RowDefinition();
			        domGrid.RowDefinitions.Add(row6);
					RowDefinition row7 = new RowDefinition();
			        domGrid.RowDefinitions.Add(row7);
					RowDefinition row8 = new RowDefinition();
			        domGrid.RowDefinitions.Add(row8);
					
					string fontFamilyName = ChartControl.Properties.LabelFont.Family.ToString() + " Bold";
			        Label accountLabel = new Label
			        {
				        Content = myAccount.DisplayName,
						FontFamily = new FontFamily(fontFamilyName),
						FontSize = accountFontSize,
						Foreground = accountBrush,
						Margin = new Thickness(0, 15, 0, -9)
			        };
					Grid.SetRow(accountLabel, 0);
					
					Label quantityLabel = new Label
					{
						Content = "Кол-во ордеров",
						Width = domWidth,
						HorizontalAlignment = HorizontalAlignment.Left
					};
					Grid.SetRow(quantityLabel, 1);
					if (accountNickname == AccountNicknames.Sim101 ||
						accountNickname == AccountNicknames.Playback101)
						quantityLabel.Margin = new Thickness(0, 15, 0, 0);
					
					quantityUpDown = new QuantityUpDown
					{
						Instrument = Instrument,
						Value = quantity,
						Minimum = 1,
						Margin = new Thickness(6, 0, 0, 3),
						Width = domWidth,
						HorizontalAlignment = HorizontalAlignment.Left
					};
					quantityUpDown.ValueChanged += OnQuantityUpDown_ValueChanged;
					Grid.SetRow(quantityUpDown, 2);
					
					Grid buttons1and2Grid = new Grid
					{
						Width = domWidth,
						Margin = new Thickness(6, 0, 0, 0),
						HorizontalAlignment = HorizontalAlignment.Left
					};
					Grid.SetRow(buttons1and2Grid, 3);
					ColumnDefinition column11 = new ColumnDefinition();
					buttons1and2Grid.ColumnDefinitions.Add(column11);
					ColumnDefinition column12 = new ColumnDefinition();
					buttons1and2Grid.ColumnDefinitions.Add(column12);
					
					buyMktButton = new Button
					{
						Content = "Купить",
						Background = Brushes.Green,
						Margin = new Thickness(0, 3, 3, 3),
						BorderBrush = Brushes.DarkGray,
						BorderThickness = new Thickness(1)
					};
					buyMktButton.Click += OnBuyMktButton_Click;
					Grid.SetColumn(buyMktButton, 0);
					buttons1and2Grid.Children.Add(buyMktButton);
					
					sellMktButton = new Button
					{
						Content = "Продать",
						Background = Brushes.Red,
						Margin = new Thickness(3, 3, 0, 3),
						BorderBrush = Brushes.DarkGray,
						BorderThickness = new Thickness(1)
					};
					sellMktButton.Click += OnSellMktButton_Click;
					Grid.SetColumn(sellMktButton, 1);
					buttons1and2Grid.Children.Add(sellMktButton);
					
					Grid buttons3and4Grid = new Grid
					{
						Width = domWidth,
						Margin = new Thickness(6, 0, 0, 0),
						HorizontalAlignment = HorizontalAlignment.Left
					};
					Grid.SetRow(buttons3and4Grid, 4);
					ColumnDefinition column21 = new ColumnDefinition();
					buttons3and4Grid.ColumnDefinitions.Add(column21);
					ColumnDefinition column22 = new ColumnDefinition();
					buttons3and4Grid.ColumnDefinitions.Add(column22);
					
					reverseButton = new Button
					{
						Content = "Разворот",
						Background = Brushes.Blue,
						Margin = new Thickness(0, 3, 3, 3),
						BorderBrush = Brushes.DarkGray,
						BorderThickness = new Thickness(1)
					};
					reverseButton.Click += OnReverseButton_Click;
					Grid.SetColumn(reverseButton, 0);
					buttons3and4Grid.Children.Add(reverseButton);
					
					closeButton = new Button
					{
						Content = "Закрыть",
						Background = Brushes.Blue,
						Margin = new Thickness(3, 3, 0, 3),
						BorderBrush = Brushes.DarkGray,
						BorderThickness = new Thickness(1)
					};
					closeButton.Click += OnCloseButton_Click;
					Grid.SetColumn(closeButton, 1);
					buttons3and4Grid.Children.Add(closeButton);
					
					Grid positionGrid = new Grid
					{
						Width = domWidth,
						Margin = new Thickness(6, 0, 0, 0),
						HorizontalAlignment = HorizontalAlignment.Left
					};
					Grid.SetRow(positionGrid, 5);
					ColumnDefinition column31 = new ColumnDefinition();
					positionGrid.ColumnDefinitions.Add(column31);
					ColumnDefinition column32 = new ColumnDefinition();
					positionGrid.ColumnDefinitions.Add(column32);
					
					positionQuantityLabel = new Label
					{
						Content = "Позиций нет",
						Background = Brushes.Silver,
						BorderBrush = Brushes.DarkGray,
						BorderThickness = new Thickness(1, 1, 0, 1),
						Margin = new Thickness(0, 3, 0, 0),
						HorizontalContentAlignment = HorizontalAlignment.Center
					};
					Grid.SetColumn(positionQuantityLabel, 0);
					positionGrid.Children.Add(positionQuantityLabel);
					
					positionAfpLabel = new Label
					{
						Content = "Вход",
						Background = Brushes.Silver,
						BorderBrush = Brushes.DarkGray,
						BorderThickness = new Thickness(1),
						Margin = new Thickness(0, 3, 0, 0),
						HorizontalContentAlignment = HorizontalAlignment.Center
					};
					Grid.SetColumn(positionAfpLabel, 1);
					positionGrid.Children.Add(positionAfpLabel);
					
					positionUplLabel = new Label
					{
						Content = "PnL",
						Background = Brushes.Black,
						BorderBrush = Brushes.DarkGray,
						Foreground = Brushes.LightGray,
						BorderThickness = new Thickness(1, 0, 1, 1),
						Margin = new Thickness(6, 0, 0, 0),
						HorizontalAlignment = HorizontalAlignment.Left,
						HorizontalContentAlignment = HorizontalAlignment.Center,
						Width = domWidth
					};
					Grid.SetRow(positionUplLabel, 6);
					
					Grid askBidGrid = new Grid
					{
						Width = domWidth,
						Margin = new Thickness(6, 0, 0, 0),
						HorizontalAlignment = HorizontalAlignment.Left
					};
					Grid.SetRow(askBidGrid, 7);
					ColumnDefinition column41 = new ColumnDefinition();
					askBidGrid.ColumnDefinitions.Add(column41);
					ColumnDefinition column42 = new ColumnDefinition();
					askBidGrid.ColumnDefinitions.Add(column42);
					
					askBidLabel = new Label
					{
						Background = Brushes.Gainsboro
					};
					Grid.SetColumn(askBidLabel, 0);
					askBidGrid.Children.Add(askBidLabel);
					
					askBidVolumeLabel = new Label
					{
						Background = Brushes.Gainsboro
					};
					Grid.SetColumn(askBidVolumeLabel, 1);
					askBidGrid.Children.Add(askBidVolumeLabel);
					
					if (accountNickname != AccountNicknames.Sim101 &&
						accountNickname != AccountNicknames.Playback101)
			        	domGrid.Children.Add(accountLabel);
					if (displayDom == true)
					{
						domGrid.Children.Add(quantityLabel);
						domGrid.Children.Add(quantityUpDown);
						domGrid.Children.Add(buttons1and2Grid);
						domGrid.Children.Add(buttons3and4Grid);
						domGrid.Children.Add(positionGrid);
						domGrid.Children.Add(positionUplLabel);
						domGrid.Children.Add(askBidGrid);
					}
			        
			        UserControlCollection.Add(domGrid);
			    }));
			}
			
			else if (State == State.Realtime)
			{
				DisplayAllOrders();
				DisplayAllPositions();
				UpdatePositionLabels();
				UpdateAskBidLabels();
				
				ChartControl.Dispatcher.InvokeAsync((() =>
				{
					ChartControl.ContextMenuOpening += OnContextMenuOpening;
					ChartControl.ContextMenuClosing += OnContextMenuClosing;
				}));
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
				
				if (ChartControl != null)
				{
					ChartControl.Dispatcher.InvokeAsync((() =>
					{
						ChartControl.ContextMenuOpening -= OnContextMenuOpening;
						ChartControl.ContextMenuClosing -= OnContextMenuClosing;
						
						if (quantityUpDown != null)
							quantityUpDown.ValueChanged -= OnQuantityUpDown_ValueChanged;
						if (buyMktButton != null)
							buyMktButton.Click -= OnBuyMktButton_Click;
						if (sellMktButton != null)
							sellMktButton.Click -= OnSellMktButton_Click;
						if (reverseButton != null)
							reverseButton.Click -= OnReverseButton_Click;
						if (closeButton != null)
							closeButton.Click -= OnCloseButton_Click;

				        if (domGrid != null)
							domGrid.Children.Clear();
					}));
				}
			}
		}
		
		private void OnQuantityUpDown_ValueChanged(object sender, RoutedEventArgs e)
		{
			quantity = quantityUpDown.Value;
		}
		
		private void OnBuyMktButton_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				Order order = myAccount.CreateOrder(Instrument, OrderAction.Buy,
				OrderType.Market, OrderEntry.Automated, TimeInForce.Gtc, quantity,
				0, 0, "", "", DateTime.MaxValue, null);
				myAccount.Submit(new [] { order });
			}
			catch (Exception exc)
			{
				Print(exc.Message + '\n' + exc.StackTrace + '\n');
			}
		}
		
		private void OnSellMktButton_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				Order order = myAccount.CreateOrder(Instrument, OrderAction.Sell,
				OrderType.Market, OrderEntry.Automated, TimeInForce.Gtc, quantity,
				0, 0, "", "", DateTime.MaxValue, null);
				myAccount.Submit(new [] { order });
			}
			catch (Exception exc)
			{
				Print(exc.Message + '\n' + exc.StackTrace + '\n');
			}
		}
		
		private void OnReverseButton_Click(object sender, RoutedEventArgs e)
		{
			Position myPosition = GetPosition();
			if (myPosition != null)
				myPosition.Reverse(TimeInForce.Gtc, DateTime.MaxValue);
		}
		
		private void OnCloseButton_Click(object sender, RoutedEventArgs e)
		{
			Position myPosition = GetPosition();
			if (myPosition != null)
				myPosition.Close();
			else
				myAccount.Cancel(GetOrdersOfInstrument());
		}
		
		#region Context menu events and methods
		private void OnContextMenuOpening(object sender, ContextMenuEventArgs e)
		{
			try
			{
				int precision = CalcPricePrecision();
				cmOpenPrice = ChartPanel.Scales[ScaleJustification].GetValueByYWpf(e.CursorTop);
				cmOpenPrice = Math.Round(cmOpenPrice, precision);
				string cmOpenPriceString = cmOpenPrice.ToString("F" + precision.ToString());
				
				object selectedObject = null;
				Type selectedObjectType = null;
				double orderPrice;
				string orderPriceString;
				
				for (int i = 0; i < ChartControl.ChartObjects.Count; i++)
				{
					if (ChartControl.ChartObjects[i].IsSelected == true)
					{
						selectedObject = ChartControl.ChartObjects[i];
						selectedObjectType = ChartControl.ChartObjects[i].GetType();
						break;
					}
				}
				
				if (selectedObject != null && selectedObjectType == typeof(ChartTraderLine))
				{
					ChartTraderLine selectedCtl = selectedObject as ChartTraderLine;
					string orderId = selectedCtl.Tag.Split('_')[1];
					Order order = GetOrderById(orderId);
					
					if (order != null)
					{
						cmSelectedOrder = order;
						orderPrice = GetOrderPrice(order);
						orderPriceString = orderPrice.ToString("F" + precision.ToString());
						
						ChartControl.Dispatcher.InvokeAsync((() =>
						{
							cancelOrderItem.Header = "Отменить ордер @ " + orderPriceString;
							ChartControl.ContextMenu.Items.Insert(0, cancelOrderItem);
							increaseQuantityItem.Header = "+ Кол-во ордера @ " + orderPriceString;
							ChartControl.ContextMenu.Items.Insert(1, increaseQuantityItem);
							if (order.Quantity == 1)
								decreaseQuantityItem.IsEnabled = false;
							decreaseQuantityItem.Header = "– Кол-во ордера @ " + orderPriceString;
							ChartControl.ContextMenu.Items.Insert(2, decreaseQuantityItem);
						}));
					}
				}
				else if (selectedObject == null)
				{
					double close = Close.GetValueAt(Close.Count - 1);
					string qStr = quantity.ToString();
					
					if (cmOpenPrice >= close)
					{
						ChartControl.Dispatcher.InvokeAsync((() =>
						{
							sellLimitItem.Header = "Продать Лимит " + qStr + " @ " + cmOpenPriceString;
							ChartControl.ContextMenu.Items.Insert(8, sellLimitItem);
							sellMitItem.Header = "Продать MIT " + qStr + " @ " + cmOpenPriceString;
							ChartControl.ContextMenu.Items.Insert(9, sellMitItem);
							buyStopMarketItem.Header = "Купить Стоп " + qStr + " @ " + cmOpenPriceString;
							ChartControl.ContextMenu.Items.Insert(10, buyStopMarketItem);
							buyStopLimitItem.Header = "Купить Стоп Лимит " + qStr + " @ " + cmOpenPriceString;
							ChartControl.ContextMenu.Items.Insert(11, buyStopLimitItem);
						}));
					}
					else if (cmOpenPrice < close)
					{
						ChartControl.Dispatcher.InvokeAsync((() =>
						{
							buyLimitItem.Header = "Купить Лимит " + qStr + " @ " + cmOpenPriceString;
							ChartControl.ContextMenu.Items.Insert(8, buyLimitItem);
							buyMitItem.Header = "Купить MIT " + qStr + " @ " + cmOpenPriceString;
							ChartControl.ContextMenu.Items.Insert(9, buyMitItem);
							sellStopMarketItem.Header = "Продать Стоп " + qStr + " @ " + cmOpenPriceString;
							ChartControl.ContextMenu.Items.Insert(10, sellStopMarketItem);
							sellStopLimitItem.Header = "Продать Стоп Лимит " + qStr + " @ " + cmOpenPriceString;
							ChartControl.ContextMenu.Items.Insert(11, sellStopLimitItem);
						}));
					}
					
					Order order = GetOrderByPrice(cmOpenPrice);
					if (order == null)
					{
						Point mouseDownPoint = ChartControl.MouseDownPoint;
						double chartScaleWidth = ChartPanel.Scales[ScaleJustification].Width;
						
						if (mouseDownPoint.X >= chartScaleWidth - 138 &&
							mouseDownPoint.X <= chartScaleWidth - 8.5)
						{
							double y = mouseDownPoint.Y;
							double priceAtY;
							for (double iy = 0; iy <= 7; iy += 0.5)
							{
								y = mouseDownPoint.Y + iy;
								priceAtY = ChartPanel.Scales[ScaleJustification].GetValueByYWpf(y);
								priceAtY = Math.Round(priceAtY, precision);
								order = GetOrderByPrice(priceAtY);
								if (order != null)
									break;
							}
							if (order == null)
							{
								for (double iy = 0; iy <= 6; iy += 0.5)
								{
									y = mouseDownPoint.Y - iy;
									priceAtY = ChartPanel.Scales[ScaleJustification].GetValueByYWpf(y);
									priceAtY = Math.Round(priceAtY, precision);
									order = GetOrderByPrice(priceAtY);
									if (order != null)
										break;
								}
							}
						}
					}
					if (order != null)
					{
						cmSelectedOrder = order;
						orderPrice = GetOrderPrice(order);
						orderPriceString = orderPrice.ToString("F" + precision.ToString());
						
						ChartControl.Dispatcher.InvokeAsync((() =>
						{
							cancelOrderItem.Header = "Отменить ордер @ " + orderPriceString;
							ChartControl.ContextMenu.Items.Insert(0, cancelOrderItem);
							increaseQuantityItem.Header = "+ Кол-во ордера @ " + orderPriceString;
							ChartControl.ContextMenu.Items.Insert(1, increaseQuantityItem);
							if (order.Quantity == 1)
								decreaseQuantityItem.IsEnabled = false;
							decreaseQuantityItem.Header = "– Кол-во ордера @ " + orderPriceString;
							ChartControl.ContextMenu.Items.Insert(2, decreaseQuantityItem);
						}));
					}
				}
			}
			catch (Exception exc)
			{
				Print(exc.Message + '\n' + exc.StackTrace + '\n');
			}
		}
		
		private void OnContextMenuClosing(object sender, ContextMenuEventArgs e)
		{
			try { RemoveMyMenuItems(); }
			catch (Exception exc)
			{
				Print(exc.Message + '\n' + exc.StackTrace + '\n');
			}
		}
		
		private void RemoveMyMenuItems()
		{
			try
			{
				ChartControl.Dispatcher.InvokeAsync((() =>
				{
					ChartControl.ContextMenu.Items.Remove(sellLimitItem);
					ChartControl.ContextMenu.Items.Remove(sellMitItem);
					ChartControl.ContextMenu.Items.Remove(buyStopMarketItem);
					ChartControl.ContextMenu.Items.Remove(buyStopLimitItem);
					
					ChartControl.ContextMenu.Items.Remove(buyLimitItem);
					ChartControl.ContextMenu.Items.Remove(buyMitItem);
					ChartControl.ContextMenu.Items.Remove(sellStopMarketItem);
					ChartControl.ContextMenu.Items.Remove(sellStopLimitItem);
					
					ChartControl.ContextMenu.Items.Remove(cancelOrderItem);
					ChartControl.ContextMenu.Items.Remove(increaseQuantityItem);
					ChartControl.ContextMenu.Items.Remove(decreaseQuantityItem);
					decreaseQuantityItem.IsEnabled = true;
				}));
			}
			catch (Exception exc)
			{
				Print(exc.Message + '\n' + exc.StackTrace + '\n');
			}
		}
		
		private void SellLimitItem_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				Order order = myAccount.CreateOrder(Instrument, OrderAction.Sell,
				OrderType.Limit, OrderEntry.Automated, TimeInForce.Gtc, quantity,
				cmOpenPrice, 0, "", "", DateTime.MaxValue, null);
				myAccount.Submit(new [] { order });
			}
			catch (Exception exc)
			{
				Print(exc.Message + '\n' + exc.StackTrace + '\n');
			}
		}
		
		private void SellMitItem_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				Order order = myAccount.CreateOrder(Instrument, OrderAction.Sell,
				OrderType.MIT, OrderEntry.Automated, TimeInForce.Gtc, quantity,
				cmOpenPrice, cmOpenPrice, "", "", DateTime.MaxValue, null);
				myAccount.Submit(new [] { order });
			}
			catch (Exception exc)
			{
				Print(exc.Message + '\n' + exc.StackTrace + '\n');
			}
		}
		
		private void BuyStopMarketItem_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				Order order = myAccount.CreateOrder(Instrument, OrderAction.Buy,
				OrderType.StopMarket, OrderEntry.Automated, TimeInForce.Gtc, quantity,
				0, cmOpenPrice, "", "", DateTime.MaxValue, null);
				myAccount.Submit(new [] { order });
			}
			catch (Exception exc)
			{
				Print(exc.Message + '\n' + exc.StackTrace + '\n');
			}
		}
		
		private void BuyStopLimitItem_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				Order order = myAccount.CreateOrder(Instrument, OrderAction.Buy,
				OrderType.StopLimit, OrderEntry.Automated, TimeInForce.Gtc, quantity,
				cmOpenPrice, cmOpenPrice, "", "", DateTime.MaxValue, null);
				myAccount.Submit(new [] { order });
			}
			catch (Exception exc)
			{
				Print(exc.Message + '\n' + exc.StackTrace + '\n');
			}
		}
		
		private void BuyLimitItem_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				Order order = myAccount.CreateOrder(Instrument, OrderAction.Buy,
				OrderType.Limit, OrderEntry.Automated, TimeInForce.Gtc, quantity,
				cmOpenPrice, 0, "", "", DateTime.MaxValue, null);
				myAccount.Submit(new [] { order });
			}
			catch (Exception exc)
			{
				Print(exc.Message + '\n' + exc.StackTrace + '\n');
			}
		}
		
		private void BuyMitItem_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				Order order = myAccount.CreateOrder(Instrument, OrderAction.Buy,
				OrderType.MIT, OrderEntry.Automated, TimeInForce.Gtc, quantity,
				cmOpenPrice, cmOpenPrice, "", "", DateTime.MaxValue, null);
				myAccount.Submit(new [] { order });
			}
			catch (Exception exc)
			{
				Print(exc.Message + '\n' + exc.StackTrace + '\n');
			}
		}
		
		private void SellStopMarketItem_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				Order order = myAccount.CreateOrder(Instrument, OrderAction.Sell,
				OrderType.StopMarket, OrderEntry.Automated, TimeInForce.Gtc, quantity,
				0, cmOpenPrice, "", "", DateTime.MaxValue, null);
				myAccount.Submit(new [] { order });
			}
			catch (Exception exc)
			{
				Print(exc.Message + '\n' + exc.StackTrace + '\n');
			}
		}
		
		private void SellStopLimitItem_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				Order order = myAccount.CreateOrder(Instrument, OrderAction.Sell,
				OrderType.StopLimit, OrderEntry.Automated, TimeInForce.Gtc, quantity,
				cmOpenPrice, cmOpenPrice, "", "", DateTime.MaxValue, null);
				myAccount.Submit(new [] { order });
			}
			catch (Exception exc)
			{
				Print(exc.Message + '\n' + exc.StackTrace + '\n');
			}
		}
		
		private void CancelOrderItem_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				myAccount.Cancel(new [] { cmSelectedOrder });
			}
			catch (Exception exc)
			{
				Print(exc.Message + '\n' + exc.StackTrace + '\n');
			}
		}
		
		private void IncreaseQuantityItem_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				cmSelectedOrder.QuantityChanged = cmSelectedOrder.Quantity + 1;
				myAccount.Change(new [] { cmSelectedOrder });
			}
			catch (Exception exc)
			{
				Print(exc.Message + '\n' + exc.StackTrace + '\n');
			}
		}
		
		private void DecreaseQuantityItem_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				cmSelectedOrder.QuantityChanged = cmSelectedOrder.Quantity - 1;
				myAccount.Change(new [] { cmSelectedOrder });
			}
			catch (Exception exc)
			{
				Print(exc.Message + '\n' + exc.StackTrace + '\n');
			}
		}
		#endregion

		protected override void OnBarUpdate()
		{
			//Добавьте логику пользовательского indicator здесь.
			
			if (State != State.Realtime)
				return;
			
			DisplayAllPositions();
			UpdatePositionLabels();
			UpdateAskBidLabels();
		}
		
		private void DisplayAllOrders()
		{
			for (int i = 0; i < myAccount.Orders.Count; i++)
				DrawOrderLine(myAccount.Orders[i]);
		}
		
		private void DisplayAllPositions()
		{
			List<string> closedPositionTags = new List<string>();
			int matchesCount;
			for (int t = 0; t < positionTags.Count; t++)
			{
				matchesCount = 0;
				for (int i = 0; i < myAccount.Positions.Count; i++)
				{
					if (positionTags[t] == CreatePositionLineTag(myAccount.Positions[i]))
						matchesCount += 1;
				}
				if (matchesCount == 0)
					closedPositionTags.Add(positionTags[t]);
			}
			for (int i = 0; i < closedPositionTags.Count; i++)
			{
				RemoveDrawObject(closedPositionTags[i]);
				positionTags.Remove(closedPositionTags[i]);
			}
			
			for (int i = 0; i < myAccount.Positions.Count; i++)
				DrawPositionLine(myAccount.Positions[i]);
		}
		
		private void OnOrderUpdate(object sender, OrderEventArgs e)
		{
			if (e.Order.Instrument != Instrument)
				return;
			
			string tag = CreateOrderLineTag(e.Order);
			
			if (e.Order.OrderState != OrderState.ChangePending &&
				e.Order.OrderState != OrderState.ChangeSubmitted &&
				e.Order.OrderState != OrderState.CancelPending &&
				e.Order.OrderState != OrderState.CancelSubmitted)
			{
				RemoveDrawObject(tag);
				DrawOrderLine(e.Order);
			}
			
			for (int i = 0; i < e.Order.Account.Orders.Count; i++)
			{
				if (e.Order.Account.Orders[i] == e.Order ||
					e.Order.Account.Orders[i].Instrument != Instrument)
					continue;
				if (e.Order.OrderType == e.Order.Account.Orders[i].OrderType &&
					e.Order.OrderAction == e.Order.Account.Orders[i].OrderAction &&
					(e.Order.Account.Orders[i].OrderState != OrderState.Cancelled &&
					e.Order.Account.Orders[i].OrderState != OrderState.Rejected &&
					e.Order.Account.Orders[i].OrderState != OrderState.Unknown &&
					e.Order.Account.Orders[i].OrderState != OrderState.Filled))
				{
					if ((e.Order.OrderType == OrderType.Limit &&
						e.Order.LimitPrice == e.Order.Account.Orders[i].LimitPrice) ||
						(e.Order.OrderType != OrderType.Limit && e.Order.OrderType != OrderType.Unknown &&
						e.Order.StopPrice == e.Order.Account.Orders[i].StopPrice))
					{
						DrawOrderLine(e.Order.Account.Orders[i]);
					}
				}
			}
		}
		
		private void OnPositionUpdate(object sender, PositionEventArgs e)
		{
			DisplayAllPositions();
			UpdatePositionLabels();
		}
		
		private void DrawOrderLine(Order order)
		{
			if (order.Instrument != Instrument)
				return;
			
			if (order.OrderState == OrderState.Cancelled ||
				order.OrderState == OrderState.Rejected ||
				order.OrderState == OrderState.Unknown ||
				order.OrderState == OrderState.Filled)
				return;
			
			double y = 0;
			Brush brush = Brushes.Transparent;
			string tag = CreateOrderLineTag(order);
			string text = string.Empty;
			
			string sameOrderTag, otherOrderTag;
			List<Order> sameOrders = new List<Order>();
			for (int i = 0; i < order.Account.Orders.Count; i++)
			{
				if (order.Account.Orders[i] == order ||
					order.Account.Orders[i].Instrument != Instrument)
					continue;
				if (order.OrderType == order.Account.Orders[i].OrderType &&
					order.OrderAction == order.Account.Orders[i].OrderAction &&
					(order.Account.Orders[i].OrderState != OrderState.Cancelled &&
					order.Account.Orders[i].OrderState != OrderState.Rejected &&
					order.Account.Orders[i].OrderState != OrderState.Unknown &&
					order.Account.Orders[i].OrderState != OrderState.Filled))
				{
					if ((order.OrderType == OrderType.Limit &&
						order.LimitPrice == order.Account.Orders[i].LimitPrice) ||
						(order.OrderType != OrderType.Limit && order.OrderType != OrderType.Unknown &&
						order.StopPrice == order.Account.Orders[i].StopPrice))
					{
						sameOrders.Add(order.Account.Orders[i]);
						sameOrderTag = CreateOrderLineTag(order.Account.Orders[i]);
						
						if (sameOrdersSaved.ContainsKey(tag) == true)
						{
							if (sameOrdersSaved[tag].Contains(order.Account.Orders[i]) == false)
								sameOrdersSaved[tag].Add(order.Account.Orders[i]);
						}
						else
						{
							sameOrdersSaved.Add(tag, new List<Order>());
							sameOrdersSaved[tag].Add(order.Account.Orders[i]);
						}
					}
				}
			}
			if (sameOrders.Count == 0)
				text += order.Quantity.ToString() + ' ';
			else
			{
				int groupQuantity = order.Quantity;
				for (int i = 0; i < sameOrders.Count; i++)
					groupQuantity += sameOrders[i].Quantity;
				text += groupQuantity.ToString() + "s ";
			}
			
			if (sameOrdersSaved.ContainsKey(tag) == true)
			{
				List<Order> noMoreSameOrders = new List<Order>();
				for (int i = 0; i < sameOrdersSaved[tag].Count; i++)
				{
					if (sameOrders.Contains(sameOrdersSaved[tag][i]) == false)
						noMoreSameOrders.Add(sameOrdersSaved[tag][i]);
				}
				for (int i = 0; i < noMoreSameOrders.Count; i++)
				{
					sameOrdersSaved[tag].Remove(noMoreSameOrders[i]);
					if (sameOrdersSaved[tag].Count == 0)
						sameOrdersSaved.Remove(tag);
					DrawOrderLine(noMoreSameOrders[i]);
				}
			}
			
			if (order.OrderAction == OrderAction.Buy ||
				order.OrderAction == OrderAction.BuyToCover)
				text += "Купить ";
			else if (order.OrderAction == OrderAction.Sell ||
				order.OrderAction == OrderAction.SellShort)
				text += "Продать ";
			
			switch (order.OrderType)
			{
				case OrderType.Limit:
					text += "LMT";
					y = order.LimitPrice;
					brush = limitOrderBrush;
					break;
				case OrderType.StopMarket:
					text += "STP";
					y = order.StopPrice;
					brush = stopMarketOrderBrush;
					break;
				case OrderType.StopLimit:
					text += "SLM";
					y = order.StopPrice;
					brush = stopLimitOrderBrush;
					break;
			}
			if (order.OrderType == OrderType.MIT || order.OrderType == OrderType.Market)
			{
				text += "MIT";
				y = order.StopPrice;
				brush = mitOrderBrush;
			}
			
			if (order.Name.StartsWith("Target"))
				brush = takeProfitOrderBrush;
			else if (order.Name.StartsWith("Stop"))
				brush = stopLossOrderBrush;
			
			Brush rectBrush = brush;
			if (fillWithWhiteBrush == true)
				rectBrush = Brushes.White;
			
			ChartTraderLine ctLine = Draw.ChartTraderLine(this, tag, true,
				y, text, false, brush, rectBrush, lineWidth, false, true);
			ctLine.Account = myAccount;
			ctLine.Order = order;
		}
		
		private string CreateOrderLineTag(Order order)
		{
			return "order_" + order.Id.ToString();
		}
		
		private void DrawPositionLine(Position myPosition)
		{
			if (myPosition.Instrument != Instrument)
				return;
			
			double y = myPosition.AveragePrice;
			string tag = CreatePositionLineTag(myPosition);
			if (positionTags.Contains(tag) == false)
				positionTags.Add(tag);
			string text = string.Empty;
		
			double unrealizedProfitLoss = myPosition.GetUnrealizedProfitLoss(PerformanceUnit.Currency);
			if (unrealizedProfitLoss < 0)
				text += '–';
			text += '$' + Math.Abs(unrealizedProfitLoss).ToString("F2") + ' ' + myPosition.Quantity.ToString();

			Brush rectBrush = positionBrush;
			if (fillWithWhiteBrush == true)
				rectBrush = Brushes.White;
			
			Draw.ChartTraderLine(this, tag, true, y, text, true,
				positionBrush, rectBrush, lineWidth, true, true);
		}
		
		private string CreatePositionLineTag(Position myPosition)
		{
			return "position_" + myPosition.MarketPosition.ToString()
				+ "_on_" + myPosition.AveragePrice.ToString();
		}
		
		private void UpdatePositionLabels()
		{
			ChartControl.Dispatcher.InvokeAsync((() =>
			{
				Position myPosition = GetPosition();
				if (myPosition == null)
				{
					positionQuantityLabel.Content = "Позиций нет";
					positionQuantityLabel.Background = Brushes.Silver;
					positionQuantityLabel.Foreground = Brushes.Black;
					positionAfpLabel.Content = "Вход";
					positionUplLabel.Content = "PnL";
					positionUplLabel.Foreground = Brushes.LightGray;
				}
				else
				{
					positionQuantityLabel.Content = myPosition.Quantity.ToString();
					if (myPosition.MarketPosition == MarketPosition.Long)
					{
						positionQuantityLabel.Background = Brushes.LimeGreen;
						positionQuantityLabel.Foreground = Brushes.Black;
					}
					else if (myPosition.MarketPosition == MarketPosition.Short)
					{
						positionQuantityLabel.Background = Brushes.Firebrick;
						positionQuantityLabel.Foreground = Brushes.White;
					}
					
					int precision = CalcPricePrecision();
					double averagePrice = Math.Round(myPosition.AveragePrice, precision);
					positionAfpLabel.Content = averagePrice.ToString("F" + precision.ToString());
					
					string text = string.Empty;
					double unrealizedProfitLoss = myPosition.GetUnrealizedProfitLoss(PerformanceUnit.Currency);
					if (unrealizedProfitLoss < 0)
						text += '–';
					text += Math.Abs(unrealizedProfitLoss).ToString("F2") + " $";
					positionUplLabel.Content = text;
					if (unrealizedProfitLoss <= 0)
						positionUplLabel.Foreground = Brushes.Red;
					else if (unrealizedProfitLoss > 0)
						positionUplLabel.Foreground = Brushes.LimeGreen;
				}
			}));
		}
		
		private void UpdateAskBidLabels()
		{
			int precision = CalcPricePrecision();
			
			double myAsk = Math.Round(GetCurrentAsk(), precision);
			string askStr = myAsk.ToString("F" + precision.ToString());
			
			double myBid = Math.Round(GetCurrentBid(), precision);
			string bidStr = myBid.ToString("F" + precision.ToString());
			
			ChartControl.Dispatcher.InvokeAsync((() =>
			{
				askBidLabel.Content = "A:  " + askStr + "\n\nB:  " + bidStr;
				askBidVolumeLabel.Content = GetCurrentAskVolume().ToString() +
					"\n\n" + GetCurrentBidVolume().ToString();
			}));
		}
		
		private int CalcPricePrecision()
		{
			string tickSizeStr = TickSize.ToString();
			if (tickSizeStr.Contains('-') == true)
				return Convert.ToInt32(tickSizeStr.Split('-')[1]);
			else
			{
				char[] separators = { ',', '.' };
				return tickSizeStr.Split(separators)[1].Length;
			}
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
		
		private Order GetOrderById(string orderId)
		{
			foreach (Order order in myAccount.Orders)
			{
				if (order.Id.ToString() == orderId)
					return order;
			}
			return null;
		}
		
		private Order GetOrderByPrice(double price)
		{
			foreach (Order order in myAccount.Orders)
			{
				if (order.OrderState == OrderState.Cancelled ||
					order.OrderState == OrderState.Rejected ||
					order.OrderState == OrderState.Unknown ||
					order.OrderState == OrderState.Filled)
					continue;
				if (order.LimitPrice == price || order.StopPrice == price)
					return order;
			}
			return null;
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
		
		private double GetOrderPrice(Order order)
		{
			switch (order.OrderType)
			{
				case OrderType.Limit:
					return order.LimitPrice;
					break;
				case OrderType.MIT:
					return order.StopPrice;
					break;
				case OrderType.StopMarket:
					return order.StopPrice;
					break;
				case OrderType.StopLimit:
					return order.StopPrice;
					break;
			}
			return 0;
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
		
		#region Properties
		[Display(Name = "Счет", GroupName = "1. Настройки счета", Order = 0)]
		public AccountNicknames AccountNickname
		{
			get { return accountNickname; }
			set { accountNickname = value; }
		}
		
		[XmlIgnore]
		[Display(Name = "Цвет счета", GroupName = "1. Настройки счета", Order = 1)]
		public Brush AccountBrush
		{
			get { return accountBrush; }
			set { accountBrush = value; }
		}
		
		[Browsable(false)]
		public string AccountBrushSerialize
		{
			get { return Serialize.BrushToString(AccountBrush); }
			set { AccountBrush = Serialize.StringToBrush(value); }
		}
		
		[Display(Name = "Размер шрифта", GroupName = "1. Настройки счета", Order = 2)]
		public int AccountFontSize
		{
			get { return accountFontSize; }
			set { accountFontSize = value; }
		}
		
		[Display(Name = "Кол-во ордеров", GroupName = "1. Настройки счета", Order = 3)]
		public int Quantity
		{
			get { return quantity; }
			set { quantity = value; }
		}
		
		[Display(Name = "Кнопки управления", GroupName = "1. Настройки счета", Order = 4)]
		public bool DisplayDom
		{
			get { return displayDom; }
			set { displayDom = value; }
		}
		
		[Display(Name = "Ширина панели", GroupName = "1. Настройки счета", Order = 5)]
		public int DomWidth
		{
			get { return domWidth; }
			set { domWidth = value; }
		}
		
		[XmlIgnore]
		[Display(Name = "Лимитные ордера", GroupName = "2. Настройки линий", Order = 0)]
		public Brush LimitOrderBrush
		{
			get { return limitOrderBrush; }
			set { limitOrderBrush = value; }
		}
		
		[Browsable(false)]
		public string LimitOrderBrushSerialize
		{
			get { return Serialize.BrushToString(LimitOrderBrush); }
			set { LimitOrderBrush = Serialize.StringToBrush(value); }
		}
		
		[XmlIgnore]
		[Display(Name = "MIT ордера", GroupName = "2. Настройки линий", Order = 1)]
		public Brush MitOrderBrush
		{
			get { return mitOrderBrush; }
			set { mitOrderBrush = value; }
		}
		
		[Browsable(false)]
		public string MitOrderBrushSerialize
		{
			get { return Serialize.BrushToString(MitOrderBrush); }
			set { MitOrderBrush = Serialize.StringToBrush(value); }
		}
		
		[XmlIgnore]
		[Display(Name = "Стоп лимит ордера", GroupName = "2. Настройки линий", Order = 2)]
		public Brush StopLimitOrderBrush
		{
			get { return stopLimitOrderBrush; }
			set { stopLimitOrderBrush = value; }
		}
		
		[Browsable(false)]
		public string StopLimitOrderBrushSerialize
		{
			get { return Serialize.BrushToString(StopLimitOrderBrush); }
			set { StopLimitOrderBrush = Serialize.StringToBrush(value); }
		}
		
		[XmlIgnore]
		[Display(Name = "Стоп маркет ордера", GroupName = "2. Настройки линий", Order = 3)]
		public Brush StopMarketOrderBrush
		{
			get { return stopMarketOrderBrush; }
			set { stopMarketOrderBrush = value; }
		}
		
		[Browsable(false)]
		public string StopMarketOrderBrushSerialize
		{
			get { return Serialize.BrushToString(StopMarketOrderBrush); }
			set { StopMarketOrderBrush = Serialize.StringToBrush(value); }
		}
		
		[XmlIgnore]
		[Display(Name = "Таргет прибыли", GroupName = "2. Настройки линий", Order = 4)]
		public Brush TakeProfitOrderBrush
		{
			get { return takeProfitOrderBrush; }
			set { takeProfitOrderBrush = value; }
		}
		
		[Browsable(false)]
		public string TakeProfitOrderBrushSerialize
		{
			get { return Serialize.BrushToString(TakeProfitOrderBrush); }
			set { TakeProfitOrderBrush = Serialize.StringToBrush(value); }
		}
		
		[XmlIgnore]
		[Display(Name = "Стоп лосс", GroupName = "2. Настройки линий", Order = 5)]
		public Brush StopLossOrderBrush
		{
			get { return stopLossOrderBrush; }
			set { stopLossOrderBrush = value; }
		}
		
		[Browsable(false)]
		public string StopLossOrderBrushSerialize
		{
			get { return Serialize.BrushToString(StopLossOrderBrush); }
			set { StopLossOrderBrush = Serialize.StringToBrush(value); }
		}
		
		[XmlIgnore]
		[Display(Name = "Позиции", GroupName = "2. Настройки линий", Order = 6)]
		public Brush PositionBrush
		{
			get { return positionBrush; }
			set { positionBrush = value; }
		}
		
		[Browsable(false)]
		public string PositionBrushSerialize
		{
			get { return Serialize.BrushToString(PositionBrush); }
			set { PositionBrush = Serialize.StringToBrush(value); }
		}
		
		[Display(Name = "Ширина линии", GroupName = "2. Настройки линий", Order = 7)]
		public int LineWidth
		{
			get { return lineWidth; }
			set { lineWidth = value; }
		}
		
		[Display(Name = "Поле белого цвета", GroupName = "2. Настройки линий", Order = 8)]
		public bool FillWithWhiteBrush
		{
			get { return fillWithWhiteBrush; }
			set { fillWithWhiteBrush = value; }
		}
		#endregion
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private OrderViewer[] cacheOrderViewer;
		public OrderViewer OrderViewer()
		{
			return OrderViewer(Input);
		}

		public OrderViewer OrderViewer(ISeries<double> input)
		{
			if (cacheOrderViewer != null)
				for (int idx = 0; idx < cacheOrderViewer.Length; idx++)
					if (cacheOrderViewer[idx] != null &&  cacheOrderViewer[idx].EqualsInput(input))
						return cacheOrderViewer[idx];
			return CacheIndicator<OrderViewer>(new OrderViewer(), input, ref cacheOrderViewer);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.OrderViewer OrderViewer()
		{
			return indicator.OrderViewer(Input);
		}

		public Indicators.OrderViewer OrderViewer(ISeries<double> input )
		{
			return indicator.OrderViewer(input);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.OrderViewer OrderViewer()
		{
			return indicator.OrderViewer(Input);
		}

		public Indicators.OrderViewer OrderViewer(ISeries<double> input )
		{
			return indicator.OrderViewer(input);
		}
	}
}

#endregion
