#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
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
using NinjaTrader.NinjaScript.Strategies.Рабочие_стратегии;
#endregion

//This namespace holds Indicators in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Indicators
{
	public class ProfitGraph : Indicator
	{
		private double totalPnL = 0;
		private double biggestProfit = 0;
		private DateTime biggestProfitTime;
		private double biggestLoss = 0;
		private DateTime biggestLossTime;
		private double dailyTargetProfit = 0;
		private double dailyStopLoss = 0;
		
		private Grid myGrid;
		private Label biggestProfitLabel;
		private Label biggestLossLabel;
		
		private string csvFileFullName = string.Empty;
		private bool contentsAreSaved = false;
		private List<string> contents;
		
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Введите описание новой пользовательской Индикатор здесь.";
				Name										= "График прибыли";
				Calculate									= Calculate.OnPriceChange;
				IsOverlay									= false;
				DisplayInDataBox							= true;
				DrawOnPricePanel							= true;
				DrawHorizontalGridLines						= true;
				DrawVerticalGridLines						= true;
				PaintPriceMarkers							= true;
				ScaleJustification							= NinjaTrader.Gui.Chart.ScaleJustification.Right;
				//Disable this property if your indicator requires custom values that cumulate with each new market data event. 
				//See Help Guide for additional information.
				IsSuspendedWhileInactive					= false;
				
				AddLine(new Stroke(Brushes.Black, DashStyleHelper.Dash, 1), 0, "Линия 0");
				AddPlot(new Stroke(Brushes.Gray, 1), PlotStyle.Line, "Фикс. прибыль");
				AddPlot(new Stroke(Brushes.ForestGreen, 2), PlotStyle.Line, "Кривая прибыли");
				AddPlot(new Stroke(Brushes.Red, 2), PlotStyle.Line, "Кривая просадки");
			}
			else if (State == State.Configure)
			{
			}
			else if (State == State.DataLoaded)
			{
				contents = new List<string>();
				contents.Add("Не фиксированная прибыль;Фиксированная прибыль;Время");
			}
			else if (State == State.Historical)
			{
				if (ChartControl == null)
					return;
				
				ChartControl.Dispatcher.InvokeAsync((() =>
			    {
					myGrid = new Grid
					{
						HorizontalAlignment = HorizontalAlignment.Right,
				        VerticalAlignment = VerticalAlignment.Bottom
					};
					myGrid.RowDefinitions.Add(new RowDefinition());
					myGrid.RowDefinitions.Add(new RowDefinition());
					
					biggestProfitLabel = new Label
					{
						FontSize = 14,
						Foreground = Brushes.Green,
						Content = "Max прибыль: 0"
					};
					biggestLossLabel = new Label
					{
						FontSize = 14,
						Foreground = Brushes.Red,
						Content = "Max просадка: 0"
					};
					Grid.SetRow(biggestProfitLabel, 0);
					Grid.SetRow(biggestLossLabel, 1);
					
					myGrid.Children.Add(biggestProfitLabel);
					myGrid.Children.Add(biggestLossLabel);
					
					UserControlCollection.Add(myGrid);
				}));
			}
			else if (State == State.Terminated)
			{
				if (ChartControl == null)
					return;
				
				ChartControl.Dispatcher.InvokeAsync((() =>
				{
					if (myGrid != null)
					{
						myGrid.Children.Remove(biggestProfitLabel);
						myGrid.Children.Remove(biggestLossLabel);
					}
				}));
			}
		}

		protected override void OnBarUpdate()
		{
			//Добавьте логику пользовательского indicator здесь.
			
			if (State == State.Realtime)
			{
				SessionAdmin sessionAdmin;
				try { sessionAdmin = StrategyManager.GetSessionAdmin(ChartControl); }
				catch { sessionAdmin = null; }
				if (sessionAdmin == null)
				{
					WriteContentsToCsvFile();
					return;
				}
				if (csvFileFullName == string.Empty)
					csvFileFullName = GetCsvFileFullName(sessionAdmin);
				
				if (dailyTargetProfit != sessionAdmin.DailyTargetProfit)
				{
					dailyTargetProfit = sessionAdmin.DailyTargetProfit;
					Draw.HorizontalLine(this, "Daily_target_profit", dailyTargetProfit,
						Brushes.ForestGreen, DashStyleHelper.Dash, 2, false);
				}
				if (dailyStopLoss != sessionAdmin.DailyStopLoss)
				{
					dailyStopLoss = sessionAdmin.DailyStopLoss;
					Draw.HorizontalLine(this, "Daily_stop_loss", dailyStopLoss * -1,
						Brushes.DeepPink, DashStyleHelper.Dash, 1, false);
				}
				
				Position position = sessionAdmin.GetInstrumentPosition();
				if (position != null)
				{
					double unrealizedPnL = position.GetUnrealizedProfitLoss(PerformanceUnit.Currency);
					totalPnL = Math.Round(sessionAdmin.RealizedPnL + unrealizedPnL, 2, MidpointRounding.AwayFromZero);
				}
				
				Values[0][0] = sessionAdmin.RealizedPnL;
				if (totalPnL > 0 && totalPnL > Values[1][0])
					Values[1][0] = totalPnL;
				if (totalPnL > biggestProfit)
				{
					biggestProfit = totalPnL;
					biggestProfitTime = Time[0];
					Draw.Dot(this, "Biggest_profit", true, Time[0], biggestProfit, Brushes.ForestGreen, false);
					SetLabelContent(biggestProfitLabel, "Max прибыль:", biggestProfit, biggestProfitTime);
				}
				if (totalPnL <= 0 && totalPnL <= Values[2][0])
					Values[2][0] = totalPnL;
				if (totalPnL < biggestLoss)
				{
					biggestLoss = totalPnL;
					biggestLossTime = Time[0];
					Draw.Dot(this, "Biggest_loss", true, Time[0], biggestLoss, Brushes.Red, false);
					SetLabelContent(biggestLossLabel, "Max просадка:", biggestLoss, biggestLossTime);
				}
			}
		}
		
		private void SetLabelContent(Label label, string text, double value, DateTime time)
		{
			if (ChartControl == null)
				return;			
			string timeStr = time.ToString("HH:mm:ss");
			string fullText = string.Format("{0} {1} Время: {2}", text, value, timeStr);
			ChartControl.Dispatcher.InvokeAsync((() => { label.Content = fullText; }));
		}
		
		private string GetCsvFileFullName(SessionAdmin sessionAdmin)
		{
			string directoryPath, fullName;
			string startTimeString = sessionAdmin.StartTimeString.Replace(':', '-');
			string endTimeString = sessionAdmin.EndTimeString.Replace(':', '-');
			string name = string.Format("{0} – {1}.csv", startTimeString, endTimeString);
			string myDocuments = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
			directoryPath = Path.Combine(myDocuments, "NinjaTrader 8", "График прибыли", Instrument.FullName, Time[0].ToString("dd.MM.yyyy"));
			fullName = Path.Combine(directoryPath, name);
			if (Directory.Exists(directoryPath) == false)
				Directory.CreateDirectory(directoryPath);
			return fullName;
		}
		
		private void WriteContentsToCsvFile()
		{
			if (contentsAreSaved == true)
				return;
			if (csvFileFullName != string.Empty)
				contentsAreSaved = true;
			else
				return;
			
			for (int i = 0; i < Time.Count; i++)
			{
				if (Values[1].GetValueAt(i) != 0)
					AddNewContent(Values[1].GetValueAt(i), Values[0].GetValueAt(i), Time.GetValueAt(i));
				if (Values[2].GetValueAt(i) != 0)
					AddNewContent(Values[2].GetValueAt(i), Values[0].GetValueAt(i), Time.GetValueAt(i));
			}
			File.WriteAllLines(csvFileFullName, contents);
		}
		
		private void AddNewContent(double tempTotalPnL, double realizedPnL, DateTime time)
		{
			string timeStr = time.ToString("HH:mm:ss");
			string content = string.Format("{0};{1};{2}", tempTotalPnL, realizedPnL, timeStr);
			if (content.Split(';')[0] != contents.Last().Split(';')[0] ||
				content.Split(';')[1] != contents.Last().Split(';')[1])
				contents.Add(content);
		}
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private ProfitGraph[] cacheProfitGraph;
		public ProfitGraph ProfitGraph()
		{
			return ProfitGraph(Input);
		}

		public ProfitGraph ProfitGraph(ISeries<double> input)
		{
			if (cacheProfitGraph != null)
				for (int idx = 0; idx < cacheProfitGraph.Length; idx++)
					if (cacheProfitGraph[idx] != null &&  cacheProfitGraph[idx].EqualsInput(input))
						return cacheProfitGraph[idx];
			return CacheIndicator<ProfitGraph>(new ProfitGraph(), input, ref cacheProfitGraph);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.ProfitGraph ProfitGraph()
		{
			return indicator.ProfitGraph(Input);
		}

		public Indicators.ProfitGraph ProfitGraph(ISeries<double> input )
		{
			return indicator.ProfitGraph(input);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.ProfitGraph ProfitGraph()
		{
			return indicator.ProfitGraph(Input);
		}

		public Indicators.ProfitGraph ProfitGraph(ISeries<double> input )
		{
			return indicator.ProfitGraph(input);
		}
	}
}

#endregion
