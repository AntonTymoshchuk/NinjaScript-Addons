#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using System.Xml.Linq;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.SuperDom;
using NinjaTrader.Gui.Tools;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.Gui.Tools;
#endregion

//This namespace holds Add ons in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.AddOns
{
	public class StrategyStatistics : NinjaTrader.NinjaScript.AddOnBase
	{
		private NTMenuItem strategyStatisticsMenuItem;
		private NTMenuItem existingMenuItemInControlCenter;
		
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Введите описание новой пользовательской Добавить здесь.";
				Name										= "Статистика торговой системы";
			}
			else if (State == State.Configure)
			{
			}
		}
		
		protected override void OnWindowCreated(Window window)
		{
			ControlCenter controlCenter = window as ControlCenter;
			if (controlCenter == null)
				return;
			
			existingMenuItemInControlCenter = controlCenter.FindFirst("ControlCenterMenuItemNew") as NTMenuItem;
			if (existingMenuItemInControlCenter == null)
				return;
			
			strategyStatisticsMenuItem = new NTMenuItem();
			strategyStatisticsMenuItem.Header = "Статистика торговой системы";
			strategyStatisticsMenuItem.Style = Application.Current.TryFindResource("MainMenuItem") as Style;
			
			existingMenuItemInControlCenter.Items.Add(strategyStatisticsMenuItem);
			strategyStatisticsMenuItem.Click += OnMenuItemClick;
		}
		
		protected override void OnWindowDestroyed(Window window)
		{
			if (strategyStatisticsMenuItem != null && window is ControlCenter)
			{
				if (existingMenuItemInControlCenter != null &&
					existingMenuItemInControlCenter.Items.Contains(strategyStatisticsMenuItem))
					existingMenuItemInControlCenter.Items.Remove(strategyStatisticsMenuItem);
				
				strategyStatisticsMenuItem.Click -= OnMenuItemClick;
				strategyStatisticsMenuItem = null;
			}
		}
		
		private void OnMenuItemClick(object sender, RoutedEventArgs e)
		{
			Core.Globals.RandomDispatcher.BeginInvoke(new Action(() => new StrategyStatisticsWindow().Show()));
		}
	}
	
	public class StrategyStatisticsWindowFactory : INTTabFactory
	{
		public NTWindow CreateParentWindow()
		{
			return new StrategyStatisticsWindow();
		}
		
		public NTTabPage CreateTabPage(string typeName, bool isTrue)
		{
			return new StrategyStatisticsTab();
		}
	}
	
	public class StrategyStatisticsWindow : NTWindow, IWorkspacePersistence
	{
		public StrategyStatisticsWindow()
		{
			Caption = "Статистика торговой системы";
			Width = 1200;
			Height = 750;
			
			TabControl tabControl = new TabControl();
			TabControlManager.SetIsMovable(tabControl, true);
			TabControlManager.SetCanAddTabs(tabControl, true);
			TabControlManager.SetCanRemoveTabs(tabControl, true);
			
			TabControlManager.SetFactory(tabControl, new StrategyStatisticsWindowFactory());
			Content = tabControl;
			
			tabControl.AddNTTabPage(new StrategyStatisticsTab());
			
			Loaded += (o, e) =>
			{
				if (WorkspaceOptions == null)
					WorkspaceOptions = new WorkspaceOptions("StrategyStatistics-" + Guid.NewGuid().ToString("N"), this);
			};
		}
		
		public void Restore(XDocument document, XElement element)
		{
			if (MainTabControl != null)
				MainTabControl.RestoreFromXElement(element);
		}
		
		public void Save(XDocument document, XElement element)
		{
			if (MainTabControl != null)
				MainTabControl.SaveToXElement(element);
		}
		
		public WorkspaceOptions WorkspaceOptions { get; set; }
	}
	
	public class StrategyStatisticsTab : NTTabPage, NinjaTrader.Gui.Tools.IInstrumentProvider, NinjaTrader.Gui.Tools.IIntervalProvider
	{
		private DependencyObject pageContent;
		private CheckBox wednesdayA16SessionCheckBox;
		private CheckBox wednesdayA18SessionCheckBox;
		
		#region Parameters
		private DateTime beginDate;
		private DateTime endDate;
		private string directoryPath;
		private List<string> excludedDnS;
		
		private bool mondayESession;
		private double mondayEDailyTP;
		private double mondayEDailySL;
		private bool mondayEContinue;
		private bool mondayASession;
		private double mondayADailyTP;
		private double mondayADailySL;
		
		private bool tuesdayESession;
		private double tuesdayEDailyTP;
		private double tuesdayEDailySL;
		private bool tuesdayEContinue;
		private bool tuesdayASession;
		private double tuesdayADailyTP;
		private double tuesdayADailySL;
		
		private bool wednesdayESession;
		private double wednesdayEDailyTP;
		private double wednesdayEDailySL;
		private bool wednesdayEContinue;
		private bool wednesdayA16Session;
		private bool wednesdayA18Session;
		private double wednesdayADailyTP;
		private double wednesdayADailySL;
		
		private bool thursdayESession;
		private double thursdayEDailyTP;
		private double thursdayEDailySL;
		private bool thursdayEContinue;
		private bool thursdayASession;
		private double thursdayADailyTP;
		private double thursdayADailySL;
		
		private bool fridayESession;
		private double fridayEDailyTP;
		private double fridayEDailySL;
		private bool fridayEContinue;
		private bool fridayASession;
		private double fridayADailyTP;
		private double fridayADailySL;
		#endregion
		
		private List<StatisticsDay> statisticsDays;
		private StatisticsContainer statisticsContainer;
		private Thread calculationThread;
		
		public StrategyStatisticsTab()
		{
			statisticsDays = new List<StatisticsDay>();
			Content = LoadXAML();
			TabName = "Новая вкладка";
		}
		
		private DependencyObject LoadXAML()
		{
			try
			{
				using (Stream assemblyResourceStream = GetManifestResourceStream("AddOns.StrategyStatisticsTab.xaml"))
				{
					if (assemblyResourceStream == null)
						return null;
					StreamReader streamReader = new StreamReader(assemblyResourceStream);
					Page page = System.Windows.Markup.XamlReader.Load(streamReader.BaseStream) as Page;
					if (page == null)
						return null;
					
					pageContent = page.Content as DependencyObject;
					wednesdayA16SessionCheckBox = LogicalTreeHelper.FindLogicalNode(pageContent, "WednesdayA16SessionCheckBox") as CheckBox;
					wednesdayA18SessionCheckBox = LogicalTreeHelper.FindLogicalNode(pageContent, "WednesdayA18SessionCheckBox") as CheckBox;
					wednesdayA16SessionCheckBox.Checked += OnCheckBoxChecked;
					wednesdayA18SessionCheckBox.Checked += OnCheckBoxChecked;
					Button calculateButton = LogicalTreeHelper.FindLogicalNode(pageContent, "CalculateButton") as Button;
					Button directoryButton = LogicalTreeHelper.FindLogicalNode(pageContent, "DirectoryButton") as Button;
					calculateButton.Click += OnCalculateButtonClick;
					directoryButton.Click += OnDirectoryButtonClick;
					ApplyTextBoxEventHandlers();
					
					return pageContent;
				}
			}
			catch { return null; }
		}
		
		private void ApplyTextBoxEventHandlers()
		{
			List<string> p1s = new List<string> { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday" };
			List<string> p2s = new List<string> { "E", "A" };
			List<string> p3s = new List<string> { "TP", "SL" };
			
			foreach (string p1 in p1s)
			{
				foreach (string p2 in p2s)
				{
					foreach (string p3 in p3s)
					{
						string name = p1 + p2 + "Daily" + p3 + "TextBox";
						TextBox textBox = LogicalTreeHelper.FindLogicalNode(pageContent, name) as TextBox;
						textBox.PreviewTextInput += OnTextBoxPreviewTextInput;
					}
				}
			}
		}
		
		private void OnCheckBoxChecked(object sender, RoutedEventArgs e)
		{
			CheckBox checkBox = sender as CheckBox;
			if (checkBox == wednesdayA16SessionCheckBox && wednesdayA16SessionCheckBox.IsChecked == true)
				wednesdayA18SessionCheckBox.IsChecked = false;
			if (checkBox == wednesdayA18SessionCheckBox && wednesdayA18SessionCheckBox.IsChecked == true)
				wednesdayA16SessionCheckBox.IsChecked = false;
		}
		
		private void OnTextBoxPreviewTextInput(object sender, TextCompositionEventArgs e)
		{
			e.Handled = !(int.TryParse(e.Text, out int result));
		}
		
		private void OnCalculateButtonClick(object sender, RoutedEventArgs e)
		{
			if (CheckParameters() == true)
			{
				calculationThread = new Thread(CalculationMethod);
				calculationThread.IsBackground = true;
				calculationThread.Start();
			}
		}
		
		private void OnDirectoryButtonClick(object sender, RoutedEventArgs e)
		{
			TextBox directoryPathTextBox = LogicalTreeHelper.FindLogicalNode(pageContent, "DirectoryPathTextBox") as TextBox;
			System.Windows.Forms.FolderBrowserDialog folderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog();
			folderBrowserDialog.RootFolder = Environment.SpecialFolder.UserProfile;
			folderBrowserDialog.SelectedPath = directoryPathTextBox.Text;
			folderBrowserDialog.ShowNewFolderButton = false;
			folderBrowserDialog.ShowDialog();
			directoryPathTextBox.Text = folderBrowserDialog.SelectedPath;
		}
		
		private void CalculationMethod()
		{
			EnableCalculateButton(false);
			ClearOutputControls();
			FillStatisticsDaysList();
			ExcludeDaysAndSessions();
			GenerateColumnNames();
			for (int i = 0; i < statisticsDays.Count; i++)
			{
				statisticsDays[i].CalculateEuropeanSession();
				statisticsDays[i].CalculateAmericanSession();
				ReportProcessStatus(i + 1);
			}
			DisplayTotalResult();
			DisplayAdditionalInfo();
			EnableCalculateButton(true);
		}
		
		private bool CheckParameters()
		{
			try
			{
				beginDate = Convert.ToDateTime((LogicalTreeHelper.FindLogicalNode(pageContent, "BeginDateTextBox") as TextBox).Text);
			}
			catch
			{
				MessageBox.Show("Ошибка при вводе даты начала", "Дата начала");
				return false;
			}
			try
			{
				endDate = Convert.ToDateTime((LogicalTreeHelper.FindLogicalNode(pageContent, "EndDateTextBox") as TextBox).Text);
			}
			catch
			{
				MessageBox.Show("Ошибка при вводе даты конца", "Дата конца");
				return false;
			}
			
			directoryPath = (LogicalTreeHelper.FindLogicalNode(pageContent, "DirectoryPathTextBox") as TextBox).Text;
			if (Directory.Exists(directoryPath) == false)
			{
				MessageBox.Show("Не существует папки по указанному пути", "Путь к папке");
				return false;
			}
			
			string excludedDnSstring = (LogicalTreeHelper.FindLogicalNode(pageContent, "ExcludedDnSTextBox") as TextBox).Text;
			excludedDnS = excludedDnSstring.Split(new string[] { "; " }, StringSplitOptions.RemoveEmptyEntries).ToList();
			foreach (string excludedItem in excludedDnS)
			{
				if (DateTime.TryParse(excludedItem.Split(' ')[0], out DateTime result) == false)
				{
					MessageBox.Show(string.Format("Ошибка при вводе даты: {0}", excludedItem), "Исключённые дни и сессии");
					return false;
				}
			}
			
			mondayESession = Convert.ToBoolean((LogicalTreeHelper.FindLogicalNode(pageContent, "MondayESessionCheckBox") as CheckBox).IsChecked);
			mondayEDailyTP = Convert.ToDouble((LogicalTreeHelper.FindLogicalNode(pageContent, "MondayEDailyTPTextBox") as TextBox).Text);
			mondayEDailySL = Convert.ToDouble((LogicalTreeHelper.FindLogicalNode(pageContent, "MondayEDailySLTextBox") as TextBox).Text);
			mondayEContinue = Convert.ToBoolean((LogicalTreeHelper.FindLogicalNode(pageContent, "MondayEContinueCheckBox") as CheckBox).IsChecked);
			mondayASession = Convert.ToBoolean((LogicalTreeHelper.FindLogicalNode(pageContent, "MondayASessionCheckBox") as CheckBox).IsChecked);
			mondayADailyTP = Convert.ToDouble((LogicalTreeHelper.FindLogicalNode(pageContent, "MondayADailyTPTextBox") as TextBox).Text);
			mondayADailySL = Convert.ToDouble((LogicalTreeHelper.FindLogicalNode(pageContent, "MondayADailySLTextBox") as TextBox).Text);
			
			tuesdayESession = Convert.ToBoolean((LogicalTreeHelper.FindLogicalNode(pageContent, "TuesdayESessionCheckBox") as CheckBox).IsChecked);
			tuesdayEDailyTP = Convert.ToDouble((LogicalTreeHelper.FindLogicalNode(pageContent, "TuesdayEDailyTPTextBox") as TextBox).Text);
			tuesdayEDailySL = Convert.ToDouble((LogicalTreeHelper.FindLogicalNode(pageContent, "TuesdayEDailySLTextBox") as TextBox).Text);
			tuesdayEContinue = Convert.ToBoolean((LogicalTreeHelper.FindLogicalNode(pageContent, "TuesdayEContinueCheckBox") as CheckBox).IsChecked);
			tuesdayASession = Convert.ToBoolean((LogicalTreeHelper.FindLogicalNode(pageContent, "TuesdayASessionCheckBox") as CheckBox).IsChecked);
			tuesdayADailyTP = Convert.ToDouble((LogicalTreeHelper.FindLogicalNode(pageContent, "TuesdayADailyTPTextBox") as TextBox).Text);
			tuesdayADailySL = Convert.ToDouble((LogicalTreeHelper.FindLogicalNode(pageContent, "TuesdayADailySLTextBox") as TextBox).Text);
			
			wednesdayESession = Convert.ToBoolean((LogicalTreeHelper.FindLogicalNode(pageContent, "WednesdayESessionCheckBox") as CheckBox).IsChecked);
			wednesdayEDailyTP = Convert.ToDouble((LogicalTreeHelper.FindLogicalNode(pageContent, "WednesdayEDailyTPTextBox") as TextBox).Text);
			wednesdayEDailySL = Convert.ToDouble((LogicalTreeHelper.FindLogicalNode(pageContent, "WednesdayEDailySLTextBox") as TextBox).Text);
			wednesdayEContinue = Convert.ToBoolean((LogicalTreeHelper.FindLogicalNode(pageContent, "WednesdayEContinueCheckBox") as CheckBox).IsChecked);
			wednesdayA16Session = Convert.ToBoolean((LogicalTreeHelper.FindLogicalNode(pageContent, "WednesdayA16SessionCheckBox") as CheckBox).IsChecked);
			wednesdayA18Session = Convert.ToBoolean((LogicalTreeHelper.FindLogicalNode(pageContent, "WednesdayA18SessionCheckBox") as CheckBox).IsChecked);
			wednesdayADailyTP = Convert.ToDouble((LogicalTreeHelper.FindLogicalNode(pageContent, "WednesdayADailyTPTextBox") as TextBox).Text);
			wednesdayADailySL = Convert.ToDouble((LogicalTreeHelper.FindLogicalNode(pageContent, "WednesdayADailySLTextBox") as TextBox).Text);
			
			thursdayESession = Convert.ToBoolean((LogicalTreeHelper.FindLogicalNode(pageContent, "ThursdayESessionCheckBox") as CheckBox).IsChecked);
			thursdayEDailyTP = Convert.ToDouble((LogicalTreeHelper.FindLogicalNode(pageContent, "ThursdayEDailyTPTextBox") as TextBox).Text);
			thursdayEDailySL = Convert.ToDouble((LogicalTreeHelper.FindLogicalNode(pageContent, "ThursdayEDailySLTextBox") as TextBox).Text);
			thursdayEContinue = Convert.ToBoolean((LogicalTreeHelper.FindLogicalNode(pageContent, "ThursdayEContinueCheckBox") as CheckBox).IsChecked);
			thursdayASession = Convert.ToBoolean((LogicalTreeHelper.FindLogicalNode(pageContent, "ThursdayASessionCheckBox") as CheckBox).IsChecked);
			thursdayADailyTP = Convert.ToDouble((LogicalTreeHelper.FindLogicalNode(pageContent, "ThursdayADailyTPTextBox") as TextBox).Text);
			thursdayADailySL = Convert.ToDouble((LogicalTreeHelper.FindLogicalNode(pageContent, "ThursdayADailySLTextBox") as TextBox).Text);
			
			fridayESession = Convert.ToBoolean((LogicalTreeHelper.FindLogicalNode(pageContent, "FridayESessionCheckBox") as CheckBox).IsChecked);
			fridayEDailyTP = Convert.ToDouble((LogicalTreeHelper.FindLogicalNode(pageContent, "FridayEDailyTPTextBox") as TextBox).Text);
			fridayEDailySL = Convert.ToDouble((LogicalTreeHelper.FindLogicalNode(pageContent, "FridayEDailySLTextBox") as TextBox).Text);
			fridayEContinue = Convert.ToBoolean((LogicalTreeHelper.FindLogicalNode(pageContent, "FridayEContinueCheckBox") as CheckBox).IsChecked);
			fridayASession = Convert.ToBoolean((LogicalTreeHelper.FindLogicalNode(pageContent, "FridayASessionCheckBox") as CheckBox).IsChecked);
			fridayADailyTP = Convert.ToDouble((LogicalTreeHelper.FindLogicalNode(pageContent, "FridayADailyTPTextBox") as TextBox).Text);
			fridayADailySL = Convert.ToDouble((LogicalTreeHelper.FindLogicalNode(pageContent, "FridayADailySLTextBox") as TextBox).Text);
			return true;
		}
		
		private void FillStatisticsDaysList()
		{
			DateTime tempDate = beginDate;
			statisticsContainer = new StatisticsContainer();
			statisticsDays.Clear();
			
			while (tempDate <= endDate)
			{
				if (Directory.Exists(Path.Combine(directoryPath, tempDate.ToString("dd.MM.yyyy"))))
				{
					if (tempDate.DayOfWeek == DayOfWeek.Monday)
					{
						statisticsDays.Add(new StatisticsDay(tempDate, mondayESession, mondayEDailyTP, mondayEDailySL,
							mondayEContinue, mondayASession, false, mondayADailyTP, mondayADailySL,
							directoryPath, statisticsContainer, Dispatcher, pageContent));
					}
					else if (tempDate.DayOfWeek == DayOfWeek.Tuesday)
					{
						statisticsDays.Add(new StatisticsDay(tempDate, tuesdayESession, tuesdayEDailyTP, tuesdayEDailySL,
							tuesdayEContinue, tuesdayASession, false, tuesdayADailyTP, tuesdayADailySL,
							directoryPath, statisticsContainer, Dispatcher, pageContent));
					}
					else if (tempDate.DayOfWeek == DayOfWeek.Wednesday)
					{
						statisticsDays.Add(new StatisticsDay(tempDate, wednesdayESession, wednesdayEDailyTP, wednesdayEDailySL,
							wednesdayEContinue, wednesdayA16Session, wednesdayA18Session, wednesdayADailyTP, wednesdayADailySL,
							directoryPath, statisticsContainer, Dispatcher, pageContent));
					}
					else if (tempDate.DayOfWeek == DayOfWeek.Thursday)
					{
						statisticsDays.Add(new StatisticsDay(tempDate, thursdayESession, thursdayEDailyTP, thursdayEDailySL,
							thursdayEContinue, thursdayASession, false, thursdayADailyTP, thursdayADailySL,
							directoryPath, statisticsContainer, Dispatcher, pageContent));
					}
					else if (tempDate.DayOfWeek == DayOfWeek.Friday)
					{
						statisticsDays.Add(new StatisticsDay(tempDate, fridayESession, fridayEDailyTP, fridayEDailySL,
							fridayEContinue, fridayASession, false, fridayADailyTP, fridayADailySL,
							directoryPath, statisticsContainer, Dispatcher, pageContent));
					}
				}
				tempDate = tempDate.AddDays(1);
			}
		}
		
		private void ExcludeDaysAndSessions()
		{
			foreach (string excludedItem in excludedDnS)
			{
				string[] subItems = excludedItem.Split(' ');
				DateTime excludedDate = DateTime.Parse(subItems[0]);
				foreach (StatisticsDay statisticsDay in statisticsDays)
				{
					if (statisticsDay.Date == excludedDate)
					{
						if (subItems.Length == 1)
							statisticsDay.ExcludeDay();
						else if (subItems[1].ToUpper() == "E")
							statisticsDay.ExcludeEuropeanSession();
						else if (subItems[1].ToUpper() == "A")
							statisticsDay.ExcludeAmericanSession();
						break;
					}
				}
			}
			List<StatisticsDay> excludedDays = new List<StatisticsDay>();
			foreach (StatisticsDay statisticDay in statisticsDays)
			{
				if (statisticDay.CheckAnySession() == false)
					excludedDays.Add(statisticDay);
			}
			foreach (StatisticsDay excludedDay in excludedDays)
				statisticsDays.Remove(excludedDay);
		}
		
		private void EnableCalculateButton(bool isEnabled)
		{
			Dispatcher.Invoke(() =>
			{
				Button calculateButton = LogicalTreeHelper.FindLogicalNode(pageContent, "CalculateButton") as Button;
				calculateButton.IsEnabled = isEnabled;
			});
		}
		
		private void ClearOutputControls()
		{
			Dispatcher.Invoke(() =>
			{
				TextBlock processTextBlock = LogicalTreeHelper.FindLogicalNode(pageContent, "ProcessTextBlock") as TextBlock;
				TextBlock resultTextBlock = LogicalTreeHelper.FindLogicalNode(pageContent, "ResultTextBlock") as TextBlock;
				TextBlock additionalTextBlock = LogicalTreeHelper.FindLogicalNode(pageContent, "AdditionalTextBlock") as TextBlock;
				
				processTextBlock.Text = string.Empty;
				resultTextBlock.Text = string.Empty;
				additionalTextBlock.Text = string.Empty;
				
				Grid outputGrid = LogicalTreeHelper.FindLogicalNode(pageContent, "OutputGrid") as Grid;
				outputGrid.Children.Clear();
				outputGrid.RowDefinitions.Clear();
			});
		}
		
		private void GenerateColumnNames()
		{
			Dispatcher.Invoke(() =>
			{
				Grid outputGrid = LogicalTreeHelper.FindLogicalNode(pageContent, "OutputGrid") as Grid;
				outputGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
				
				TextBox column0TextBox = ControlGenerator.GenerateTextBox("Дата", 0, 0, Brushes.Transparent, true);
				TextBox column1TextBox = ControlGenerator.GenerateTextBox("День недели", 1, 0, Brushes.Transparent, true);
				TextBox column2TextBox = ControlGenerator.GenerateTextBox("Сессия", 2, 0, Brushes.Transparent, true);
				TextBox column3TextBox = ControlGenerator.GenerateTextBox("Результат", 3, 0, Brushes.Transparent, true);
				TextBox column4TextBox = ControlGenerator.GenerateTextBox("Время", 4, 0, Brushes.Transparent, true);
				TextBox column5TextBox = ControlGenerator.GenerateTextBox("Всего", 5, 0, Brushes.Transparent, true);
				
				column0TextBox.BorderThickness = new Thickness(1, 1, 0, 1);
				column1TextBox.BorderThickness = new Thickness(1, 1, 0, 1);
				column2TextBox.BorderThickness = new Thickness(1, 1, 0, 1);
				column3TextBox.BorderThickness = new Thickness(1, 1, 0, 1);
				column4TextBox.BorderThickness = new Thickness(1, 1, 0, 1);
				column5TextBox.BorderThickness = new Thickness(1, 1, 1, 1);
				
				outputGrid.Children.Add(column0TextBox);
				outputGrid.Children.Add(column1TextBox);
				outputGrid.Children.Add(column2TextBox);
				outputGrid.Children.Add(column3TextBox);
				outputGrid.Children.Add(column4TextBox);
				outputGrid.Children.Add(column5TextBox);
			});
		}
		
		private void ReportProcessStatus(int days)
		{
			Dispatcher.Invoke(() =>
			{
				TextBlock processTextBlock = LogicalTreeHelper.FindLogicalNode(pageContent, "ProcessTextBlock") as TextBlock;
				int count = statisticsDays.Count;
				double percent = days * 100 / count;
				percent = Math.Round(percent, 2, MidpointRounding.AwayFromZero);
				if (percent < 100)
					processTextBlock.Text = string.Format("Процесс ... {0}%", percent);
				else if (percent == 100)
					processTextBlock.Text = "Завершено.";
			});
		}
		
		private void DisplayTotalResult()
		{
			Dispatcher.Invoke(() =>
			{
				TextBlock resultTextBlock = LogicalTreeHelper.FindLogicalNode(pageContent, "ResultTextBlock") as TextBlock;
				resultTextBlock.Text = string.Format("Результат: {0}$.", statisticsContainer.Total.ToString("F2"));
				if (statisticsContainer.Total > 0)
					resultTextBlock.Foreground = Brushes.ForestGreen;
				else if (statisticsContainer.Total <= 0)
					resultTextBlock.Foreground = Brushes.Red;
			});
		}
		
		private void DisplayAdditionalInfo()
		{
			Dispatcher.Invoke(() =>
			{
				TextBlock additionalTextBlock = LogicalTreeHelper.FindLogicalNode(pageContent, "AdditionalTextBlock") as TextBlock;
				additionalTextBlock.Text = string.Format("Пройдено {0} рабочих дней.", statisticsDays.Count);
			});
		}
		
		public Cbi.Instrument Instrument { get; set; }
		public NinjaTrader.Data.BarsPeriod BarsPeriod { get; set; }
		
		protected override string GetHeaderPart(string variable)
		{
			return variable;
		}
		
		protected override void Restore(XElement element)
		{ }
		
		protected override void Save(XElement element)
		{ }
	}
	
	public class StatisticsDay
	{
		private const string from09to16 = "09-00-00 – 15-55-00.csv";
		private const string from09to23 = "09-00-00 – 22-55-00.csv";
		private const string from16to23 = "16-00-00 – 22-55-00.csv";
		private const string from18to23 = "18-00-00 – 22-55-00.csv";
		
		private DateTime date;
		private bool europeanSession;
		private double europeanDailyTP, europeanDailySL;
		private bool europeanContinue;
		private bool american16Session;
		private bool american18Session;
		private double americanDailyTP, americanDailySL;
		private string directoryPath;
		private StatisticsContainer statisticsContainer;
		private Dispatcher dispatcher;
		private DependencyObject pageContent;
		
		public DateTime Date
		{
			get { return date; }
		}
		
		private string europeanFilePath, americanFilePath;
		
		public StatisticsDay(DateTime date, bool europeanSession, double europeanDailyTP,
			double europeanDailySL, bool europeanContinue, bool american16Session,
			bool american18Session, double americanDailyTP, double americanDailySL,
			string directoryPath, StatisticsContainer statisticsContainer,
			Dispatcher dispatcher, DependencyObject pageContent)
		{
			this.date = date;
			this.europeanSession = europeanSession;
			this.europeanDailyTP = europeanDailyTP;
			this.europeanDailySL = europeanDailySL;
			this.europeanContinue = europeanContinue;
			this.american16Session = american16Session;
			this.american18Session = american18Session;
			this.americanDailyTP = americanDailyTP;
			this.americanDailySL = americanDailySL;
			this.directoryPath = Path.Combine(directoryPath, date.ToString("dd.MM.yyyy"));
			this.statisticsContainer = statisticsContainer;
			this.pageContent = pageContent;
			this.dispatcher = dispatcher;
			GetSessionFilesPath();
		}
		
		private void GetSessionFilesPath()
		{
			string tempPath;
			if (europeanContinue == false)
			{
				tempPath = Path.Combine(directoryPath, from09to16);
				if (File.Exists(tempPath))
					europeanFilePath = tempPath;
			}
			if (europeanContinue == true)
			{
				tempPath = Path.Combine(directoryPath, from09to23);
				if (File.Exists(tempPath))
					europeanFilePath = tempPath;
				else
				{
					tempPath = Path.Combine(directoryPath, from09to16);
					if (File.Exists(tempPath))
						europeanFilePath = tempPath;
				}
			}
			if (american16Session == true)
			{
				tempPath = Path.Combine(directoryPath, from16to23);
				if (File.Exists(tempPath))
					americanFilePath = tempPath;
			}
			if (american18Session == true)
			{
				tempPath = Path.Combine(directoryPath, from18to23);
				if (File.Exists(tempPath))
					americanFilePath = tempPath;
			}
		}
		
		public void ExcludeDay()
		{
			europeanSession = false;
			american16Session = false;
			american18Session = false;
		}
		
		public void ExcludeEuropeanSession()
		{
			europeanSession = false;
		}
		
		public void ExcludeAmericanSession()
		{
			american16Session = false;
			american18Session = false;
		}
		
		public bool CheckAnySession()
		{
			if (europeanSession == false && american16Session == false && american18Session == false)
				return false;
			return true;
		}
		
		private bool CheckAmericanSession()
		{
			if (american16Session == true || american18Session == true)
				return true;
			return false;
		}
		
		public void CalculateEuropeanSession()
		{
			if (europeanSession == true)
			{
				double unrealizedPnL, realizedPnL, resultPnL = 0;
				TimeSpan tempTime = TimeSpan.Zero;
				string sessionPeriod = "09:00 – 15:55";
				
				List<string> csvStrings = File.ReadAllLines(europeanFilePath).ToList();
				csvStrings.RemoveAt(0);
				
				foreach (string csvString in csvStrings)
				{
					string[] items = csvString.Split(';');
					unrealizedPnL = Convert.ToDouble(items[0]);
					realizedPnL = Convert.ToDouble(items[1]);
					tempTime = TimeSpan.Parse(items[2]);
					
					if (unrealizedPnL >= europeanDailyTP - 20)
					{
						resultPnL = europeanDailyTP;
						break;
					}
					if (unrealizedPnL <= europeanDailySL * -1)
					{
						resultPnL = europeanDailySL * -1;
						break;
					}
					if (csvString == csvStrings.Last())
						resultPnL = realizedPnL;
					if (europeanContinue == true && tempTime >= TimeSpan.Parse("16:00:00"))
					{
						if (sessionPeriod == "09:00 – 15:55")
							sessionPeriod = "09:00 – 22:55";
						if (CheckAmericanSession())
							ExcludeAmericanSession();
					}
				}
				
				double total = statisticsContainer.Total + resultPnL;
				statisticsContainer.Total = Math.Round(total, 2, MidpointRounding.AwayFromZero);
				AddStatisticsRow(SessionOrigin.European, sessionPeriod, resultPnL, tempTime);
			}
		}
		
		public void CalculateAmericanSession()
		{
			if (CheckAmericanSession() == true)
			{
				double unrealizedPnL, realizedPnL, resultPnL = 0;
				TimeSpan tempTime = TimeSpan.Zero;
				string sessionPeriod = "16:00 – 22:55";
				if (american18Session == true)
					sessionPeriod = "18:00 – 22:55";
				
				List<string> csvStrings = File.ReadAllLines(americanFilePath).ToList();
				csvStrings.RemoveAt(0);
				
				foreach (string csvString in csvStrings)
				{
					string[] items = csvString.Split(';');
					unrealizedPnL = Convert.ToDouble(items[0]);
					realizedPnL = Convert.ToDouble(items[1]);
					tempTime = TimeSpan.Parse(items[2]);
					
					if (unrealizedPnL >= americanDailyTP - 20)
					{
						resultPnL = americanDailyTP;
						break;
					}
					if (unrealizedPnL <= americanDailySL * -1)
					{
						resultPnL = americanDailySL * -1;
						break;
					}
					if (csvString == csvStrings.Last())
						resultPnL = realizedPnL;
				}
				
				double total = statisticsContainer.Total + resultPnL;
				statisticsContainer.Total = Math.Round(total, 2, MidpointRounding.AwayFromZero);
				AddStatisticsRow(SessionOrigin.American, sessionPeriod, resultPnL, tempTime);
			}
		}
		
		private void AddStatisticsRow(SessionOrigin sessionOrigin, string sessionPeriod, double resultPnL, TimeSpan resultTime)
		{
			dispatcher.Invoke(() =>
			{
				Grid outputGrid = LogicalTreeHelper.FindLogicalNode(pageContent, "OutputGrid") as Grid;
				outputGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
				int row = outputGrid.RowDefinitions.Count - 1;
				
				string dateString = date.ToString("dd.MM.yyyy");
				string dayOfWeekName = GetDayOfWeekName(date.DayOfWeek);
				string resultTimeString = resultTime.ToString();
				string totalString = statisticsContainer.Total.ToString("F2");
				Brush dayOfWeekBrush = GetDayOfWeekBrush(date.DayOfWeek);
				
				TextBox dateTextBox = ControlGenerator.GenerateTextBox(dateString, 0, row, dayOfWeekBrush);
				TextBox dayOfWeekTextBox = ControlGenerator.GenerateTextBox(dayOfWeekName, 1, row, dayOfWeekBrush);
				TextBox sessionPeriodTextBox = ControlGenerator.GenerateTextBox(sessionPeriod, 2, row, dayOfWeekBrush);
				TextBox resultTextBox = ControlGenerator.GenerateTextBox(resultPnL.ToString("F2"), 3, row, dayOfWeekBrush);
				TextBox resultTimeTextBox = ControlGenerator.GenerateTextBox(resultTimeString, 4, row, dayOfWeekBrush);
				TextBox totalTextBox = ControlGenerator.GenerateTextBox(totalString, 5, row, dayOfWeekBrush);
				totalTextBox.BorderThickness = new Thickness(1, 0, 1, 1);
				
				if (sessionOrigin == SessionOrigin.European && CheckAmericanSession() == true)
				{
					dateTextBox.BorderThickness = new Thickness(1, 0, 0, 0);
					dayOfWeekTextBox.BorderThickness = new Thickness(1, 0, 0, 0);
				}
				if (sessionOrigin == SessionOrigin.American && europeanSession == true)
				{
					dateTextBox.Focusable = false;
					dateTextBox.Text = string.Empty;
					dayOfWeekTextBox.Focusable = false;
					dayOfWeekTextBox.Text = string.Empty;
				}
				
				if (resultPnL > 0)
					resultTextBox.Foreground = Brushes.ForestGreen;
				else if (resultPnL <= 0)
					resultTextBox.Foreground = Brushes.Red;
				if (statisticsContainer.Total > 0)
					totalTextBox.Foreground = Brushes.ForestGreen;
				else if (statisticsContainer.Total <= 0)
					totalTextBox.Foreground = Brushes.Red;
				
				outputGrid.Children.Add(dateTextBox);
				outputGrid.Children.Add(dayOfWeekTextBox);
				outputGrid.Children.Add(sessionPeriodTextBox);
				outputGrid.Children.Add(resultTextBox);
				outputGrid.Children.Add(resultTimeTextBox);
				outputGrid.Children.Add(totalTextBox);
			});
		}
		
		private string GetDayOfWeekName(DayOfWeek dayOfWeek)
		{
			switch (dayOfWeek)
			{
				case DayOfWeek.Monday:
					return "Понедельник";
				case DayOfWeek.Tuesday:
					return "Вторник";
				case DayOfWeek.Wednesday:
					return "Среда";
				case DayOfWeek.Thursday:
					return "Четверг";
				case DayOfWeek.Friday:
					return "Пятница";
			}
			return string.Empty;
		}
		
		private Brush GetDayOfWeekBrush(DayOfWeek dayOfWeek)
		{
			switch (dayOfWeek)
			{
				case DayOfWeek.Monday:
					return Brushes.White;
				case DayOfWeek.Tuesday:
					return Brushes.Azure;
				case DayOfWeek.Wednesday:
					return Brushes.SeaShell;
				case DayOfWeek.Thursday:
					return Brushes.Honeydew;
				case DayOfWeek.Friday:
					return Brushes.Ivory;
			}
			return Brushes.Transparent;
		}
	}
	
	public enum SessionOrigin
	{
		European = 0,
		American = 1
	}
	
	public class StatisticsContainer
	{
		private double total;
		
		public double Total
		{
			get { return total; }
			set { total = value; }
		}
		
		public StatisticsContainer()
		{
			this.total = 0;
		}
	}
	
	public static class ControlGenerator
	{
		public static TextBox GenerateTextBox(string text, int column, int row, Brush brush, bool bold = false)
		{
			TextBox textBox = new TextBox
			{
				Background = brush,
				IsReadOnly = true,
				FontSize = 12,
				Text = text
			};
			if (bold == true)
				textBox.FontWeight = FontWeights.Bold;
			textBox.BorderThickness = new Thickness(1, 0, 0, 1);
			Grid.SetColumn(textBox, column);
			Grid.SetRow(textBox, row);
			return textBox;
		}
	}
}
