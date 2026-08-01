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

#endregion

//This namespace holds Drawing tools in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.DrawingTools
{
	/// <summary>
	/// Represents an interface that exposes information regarding a Horizontal LabeledLine IDrawingTool.
	/// </summary>
	[EditorBrowsable(EditorBrowsableState.Always)]
	public class ChartTraderLine : CedLabeledLine
	{
		private double linePrice;
		private Account myAccount;
		private Order myOrder;
		
		[XmlIgnore]
		[Browsable(false)]
		public Account Account
		{
			get { return myAccount; }
			set { myAccount = value; }
		}
		
		[XmlIgnore]
		[Browsable(false)]
		public Order Order
		{
			get { return myOrder; }
			set { myOrder = value; }
		}
		
		// override this, we only need operations on a single anchor
		public override IEnumerable<ChartAnchor> Anchors { get { return new[] { StartAnchor }; } }

		protected override void OnStateChange()
		{
			base.OnStateChange();
			if (State == State.SetDefaults)
			{
				EndAnchor.IsBrowsable				= false;
				LineType							= ChartLineType.HorizontalLine;
				Name								= "ChartTraderLine";
				StartAnchor.DisplayName				= Custom.Resource.NinjaScriptDrawingToolAnchor;
				StartAnchor.IsXPropertiesVisible	= false;
			}
		}
		
		public override Cursor GetCursor(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, Point point)
		{
			switch (DrawingState)
			{
				case DrawingState.Building:	return Cursors.Pen;
				case DrawingState.Moving:	return IsLocked ? Cursors.No : Cursors.Hand;
				case DrawingState.Editing:	return IsLocked ? Cursors.No : Cursors.Hand;
				default:					//return Cursors.Arrow;
					// draw move cursor if cursor is near line path anywhere
					Point startPoint = StartAnchor.GetPoint(chartControl, chartPanel, chartScale);

					// just go by single axis since we know the entire lines position
					if (Math.Abs(point.Y - startPoint.Y) <= cursorSensitivity)
						return Cursors.Arrow;
					else
						return null;
			}
		}
		
		public override void OnMouseDown(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, ChartAnchor dataPoint)
		{
			linePrice = StartAnchor.Price;
			base.OnMouseDown(chartControl, chartPanel, chartScale, dataPoint);
			
			Point mouseDownPoint = chartControl.MouseDownPoint;
			double linePriceYValue = chartScale.GetYByValueWpf(linePrice);
			
//			Print("X = " + (chartScale.Width - mouseDownPoint.X).ToString());
//			Print("Y = " + (linePriceYValue - mouseDownPoint.Y).ToString());
			
			if (mouseDownPoint.X >= chartScale.Width - 27 &&
				mouseDownPoint.X <= chartScale.Width - 8.5 &&
				mouseDownPoint.Y >= linePriceYValue - 7 &&
				mouseDownPoint.Y <= linePriceYValue + 6)
			{
				if (Account != null && Order != null)
					Account.Cancel(new [] { Order });
			}
		}
		
		public override void OnMouseMove(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, ChartAnchor dataPoint)
		{
			if (IsLocked && DrawingState != DrawingState.Building)
				return;

			IgnoresSnapping = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);

			if (DrawingState == DrawingState.Building)
			{
				// start anchor will not be editing here because we start building as soon as user clicks, which
				// plops down a start anchor right away
				if (EndAnchor.IsEditing)
					Anchor45(StartAnchor, dataPoint, chartControl, chartPanel, chartScale).CopyDataValues(EndAnchor);
			}
			else if (DrawingState == DrawingState.Editing && editingAnchor != null)
			{
				// horizontal line only needs Y value updated
				MasterInstrument masterInst = chartControl.Instrument.MasterInstrument;
				double roundPrice = Math.Round(dataPoint.Price, CalcPricePrecision(masterInst));
				editingAnchor.Price = roundPrice;
				EndAnchor.Price		= roundPrice;
				linePrice			= roundPrice;
			}
			else if (DrawingState == DrawingState.Moving)
			{
				// only move anchor values as needed depending on line type
				foreach (ChartAnchor anchor in Anchors)
					anchor.MoveAnchorPrice(InitialMouseDownAnchor, dataPoint, chartControl, chartPanel, chartScale, this);
			}
		}
		
		public override void OnMouseUp(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, ChartAnchor dataPoint)
		{
			base.OnMouseUp(chartControl, chartPanel, chartScale, dataPoint);
			
			if (Account != null && Order != null)
			{
				switch (Order.OrderType)
				{
					case OrderType.Limit:
						Order.LimitPriceChanged = linePrice;
						break;
					case OrderType.MIT:
						Order.LimitPriceChanged = linePrice;
						Order.StopPriceChanged = linePrice;
						break;
					case OrderType.StopMarket:
						Order.StopPriceChanged = linePrice;
						break;
					case OrderType.StopLimit:
						Order.LimitPriceChanged = linePrice;
						Order.StopPriceChanged = linePrice;
						break;
				}
				Account.Change(new [] { Order });
			}
		}
		
		private int CalcPricePrecision(MasterInstrument masterInst)
		{
			string tickSizeStr = masterInst.TickSize.ToString();
			if (tickSizeStr.Contains('-') == true)
				return Convert.ToInt32(tickSizeStr.Split('-')[1]);
			else
			{
				char[] separators = { ',', '.' };
				return tickSizeStr.Split(separators)[1].Length;
			}
		}
	}
	
	/// <summary>
	/// Represents an interface that exposes information regarding a LabeledLine IDrawingTool.
	/// </summary>
	[EditorBrowsable(EditorBrowsableState.Always)]
	public class CedLabeledLine : CedLine
	{
		private bool isPosition = false;
		
		[XmlIgnore]
		[Browsable(false)]
		public bool IsPosition
		{
			get { return isPosition; }
			set { isPosition = value; }
		}
		
		private bool appendPriceTime;
		private bool needsLayoutUpdate;
		private bool offScreenDXBrushNeedsUpdate;
		private bool backgroundDXBrushNeedsUpdate;
		private string lastText;
		private string displayText;
		private Brush offScreenMediaBrush;
		private Brush backgroundMediaBrush;
		private Brush foregroundMediaBrush;
		private SharpDX.Direct2D1.Brush offScreenDXBrush;
		private SharpDX.Direct2D1.Brush backgroundDXBrush;
		private SharpDX.DirectWrite.TextLayout cachedTextLayout;
		
		private List<string> blackForegroundBrushes = new List<string>() {
			Brushes.Transparent.ToString(), Brushes.White.ToString(), Brushes.WhiteSmoke.ToString(), Brushes.Gainsboro.ToString(), Brushes.LightGray.ToString(),
			Brushes.Silver.ToString(), Brushes.LightPink.ToString(), Brushes.Pink.ToString(), Brushes.LavenderBlush.ToString(), Brushes.HotPink.ToString(),
			Brushes.Orchid.ToString(), Brushes.Thistle.ToString(), Brushes.Plum.ToString(), Brushes.Violet.ToString(), Brushes.Magenta.ToString(), Brushes.Lavender.ToString(),
			Brushes.GhostWhite.ToString(), Brushes.LightSteelBlue.ToString(), Brushes.DodgerBlue.ToString(), Brushes.AliceBlue.ToString(),
			Brushes.LightSkyBlue.ToString(), Brushes.SkyBlue.ToString(), Brushes.DeepSkyBlue.ToString(), Brushes.LightBlue.ToString(),
			Brushes.PowderBlue.ToString(), Brushes.Azure.ToString(), Brushes.LightCyan.ToString(), Brushes.PaleTurquoise.ToString(), Brushes.Cyan.ToString(),
			Brushes.DarkTurquoise.ToString(), Brushes.MediumTurquoise.ToString(), Brushes.Turquoise.ToString(), Brushes.Aquamarine.ToString(),
			Brushes.MediumAquamarine.ToString(), Brushes.MediumSpringGreen.ToString(), Brushes.MintCream.ToString(),
			Brushes.SpringGreen.ToString(), Brushes.Honeydew.ToString(), Brushes.LightGreen.ToString(), Brushes.PaleGreen.ToString(),
			Brushes.DarkSeaGreen.ToString(), Brushes.LimeGreen.ToString(), Brushes.Lime.ToString(), Brushes.Chartreuse.ToString(), Brushes.LawnGreen.ToString(),
			Brushes.GreenYellow.ToString(), Brushes.YellowGreen.ToString(), Brushes.Beige.ToString(), Brushes.LightGoldenrodYellow.ToString(),
			Brushes.Ivory.ToString(), Brushes.LightYellow.ToString(), Brushes.Yellow.ToString(), Brushes.DarkKhaki.ToString(), Brushes.LemonChiffon.ToString(),
			Brushes.PaleGoldenrod.ToString(), Brushes.Khaki.ToString(), Brushes.Gold.ToString(), Brushes.Cornsilk.ToString(), Brushes.FloralWhite.ToString(),
			Brushes.OldLace.ToString(), Brushes.Wheat.ToString(), Brushes.Moccasin.ToString(), Brushes.Orange.ToString(), Brushes.PapayaWhip.ToString(),
			Brushes.BlanchedAlmond.ToString(), Brushes.NavajoWhite.ToString(), Brushes.AntiqueWhite.ToString(), Brushes.Tan.ToString(),
			Brushes.BurlyWood.ToString(), Brushes.Bisque.ToString(), Brushes.DarkOrange.ToString(), Brushes.Linen.ToString(), Brushes.PeachPuff.ToString(),
			Brushes.SandyBrown.ToString(), Brushes.SeaShell.ToString(), Brushes.Sienna.ToString(), Brushes.LightSalmon.ToString(), Brushes.Coral.ToString(),
			Brushes.DarkSalmon.ToString(), Brushes.MistyRose.ToString(), Brushes.Salmon.ToString(), Brushes.Snow.ToString()
		};
		
		private List<string> whiteForegroundBrushes = new List<string>() {
			Brushes.Black.ToString(), Brushes.DarkGray.ToString(), Brushes.Gray.ToString(), Brushes.DimGray.ToString(),
			Brushes.Crimson.ToString(), Brushes.PaleVioletRed.ToString(), Brushes.DeepPink.ToString(), Brushes.MediumVioletRed.ToString(),
			Brushes.DarkMagenta.ToString(), Brushes.Purple.ToString(), Brushes.MediumOrchid.ToString(), Brushes.DarkViolet.ToString(),
			Brushes.DarkOrchid.ToString(), Brushes.Indigo.ToString(), Brushes.BlueViolet.ToString(), Brushes.MediumPurple.ToString(),
			Brushes.MediumSlateBlue.ToString(), Brushes.SlateBlue.ToString(), Brushes.DarkSlateBlue.ToString(), Brushes.Blue.ToString(),
			Brushes.MediumBlue.ToString(), Brushes.MidnightBlue.ToString(), Brushes.DarkBlue.ToString(), Brushes.Navy.ToString(),
			Brushes.RoyalBlue.ToString(), Brushes.CornflowerBlue.ToString(), Brushes.LightSlateGray.ToString(), Brushes.SlateGray.ToString(),
			Brushes.SteelBlue.ToString(), Brushes.CadetBlue.ToString(), Brushes.DarkSlateGray.ToString(), Brushes.DarkCyan.ToString(), Brushes.Teal.ToString(),
			Brushes.LightSeaGreen.ToString(), Brushes.MediumSeaGreen.ToString(), Brushes.SeaGreen.ToString(), Brushes.ForestGreen.ToString(),
			Brushes.Green.ToString(), Brushes.DarkGreen.ToString(), Brushes.DarkOliveGreen.ToString(), Brushes.OliveDrab.ToString(), Brushes.Olive.ToString(),
			Brushes.Goldenrod.ToString(), Brushes.DarkGoldenrod.ToString(), Brushes.Peru.ToString(), Brushes.Chocolate.ToString(), Brushes.SaddleBrown.ToString(),
			Brushes.OrangeRed.ToString(), Brushes.Tomato.ToString(), Brushes.LightCoral.ToString(), Brushes.RosyBrown.ToString(), Brushes.IndianRed.ToString(),
			Brushes.Red.ToString(), Brushes.Brown.ToString(), Brushes.Firebrick.ToString(), Brushes.DarkRed.ToString(), Brushes.Maroon.ToString()
		};
		
		public enum TextMode
		{
			EndPointAtPriceScale,
			PriceScale,
			EndPoint
		}
		
		public enum RectSide
		{
			Top,
			Bottom,
			Left,
			Right,
			None
		}
		
		protected override void OnStateChange()
		{
			base.OnStateChange();
			
			if (State == State.SetDefaults)
			{
				Name						= "CedLabeledLine";
				OutlineStroke				= new Stroke(Brushes.Black, 1f);
				BackgroundBrush				= Brushes.White;
				OffScreenBrush				= Brushes.Red;
				DisplayText 				= String.Empty;
				AppendPriceTime				= true;
				Font						= null;
				AreaOpacity 				= 100;
				TextDisplayMode				= TextMode.EndPointAtPriceScale;
				HorizontalOffset			= 10;
				VerticalOffset				= -7;
				offScreenDXBrushNeedsUpdate = true;
				backgroundDXBrushNeedsUpdate = true;
			}
			else if (State == State.Terminated)
			{
				if (cachedTextLayout != null)
					cachedTextLayout.Dispose();
				cachedTextLayout = null;
			}
		}
		
		public override void OnRenderTargetChanged()
        {
			base.OnRenderTargetChanged();
			
			if (RenderTarget == null)
				return;
			
			if (offScreenDXBrush != null)
				offScreenDXBrush.Dispose();
			offScreenDXBrush = offScreenMediaBrush.ToDxBrush(RenderTarget);
			
			if (backgroundDXBrush != null)
				backgroundDXBrush.Dispose();
			backgroundDXBrush = backgroundMediaBrush.ToDxBrush(RenderTarget);
			backgroundDXBrush.Opacity = (float)AreaOpacity / 100f;
		}
		
		/* Steps:
		*	1. Project start/end points for rays and extended lines
		*	2. Find collitions with ChartPanel for TextBox coordinates
		*	3. Determine price to be appended 
		*	4. Create message
		*	5. Draw TextBox
		*/

		public override void OnRender(ChartControl chartControl, ChartScale chartScale)
		{
			base.OnRender(chartControl, chartScale);
			
			Stroke.RenderTarget 		= RenderTarget;
			OutlineStroke.RenderTarget	= RenderTarget;
						
			bool snap					= true;
			bool startsOnScreen			= true;
			bool priceOffScreen			= false;
			bool instrumentLoaded		= false;
			double priceToUse			= 0;
			string pricetime			= String.Empty;
			string TextToDisplay		= DisplayText;
			MasterInstrument masterInst = null;

			if (GetAttachedToChartBars().Bars != null)
			{
				masterInst = GetAttachedToChartBars().Bars.Instrument.MasterInstrument;
				instrumentLoaded = true;
			}
			else
				instrumentLoaded = false;

			Point	startPoint			= StartAnchor.GetPoint(chartControl, ChartPanel, chartScale);
			Point	endPoint			= EndAnchor.GetPoint(chartControl, ChartPanel, chartScale);
			
			double 	strokePixAdj		= ((double)(Stroke.Width % 2)).ApproxCompare(0) == 0 ? 0.5d : 0d;
			Vector	pixelAdjustVec		= new Vector(strokePixAdj, strokePixAdj);
			
			Point 	startAdj			= (LineType == ChartLineType.HorizontalLine ? new Point(ChartPanel.X, startPoint.Y) : new Point(startPoint.X, ChartPanel.Y)) + pixelAdjustVec;
			Point 	endAdj				= (LineType == ChartLineType.HorizontalLine ? new Point(ChartPanel.X + ChartPanel.W, startPoint.Y) : new Point(startPoint.X, ChartPanel.Y + ChartPanel.H)) + pixelAdjustVec;
			
			Vector 	distVec 			= Vector.Divide(Point.Subtract(endPoint, startPoint), 100);
			Vector 	scalVec				= (LineType == ChartLineType.ExtendedLine || LineType == ChartLineType.Ray || LineType == ChartLineType.HorizontalLine) ? Vector.Multiply(distVec, 10000) : Vector.Multiply(distVec, 100);
			Point 	extPoint			= Vector.Add(scalVec, startPoint);
				
			// Project extended line start point if it is off screen
			if (LineType == ChartLineType.ExtendedLine && TextDisplayMode != TextMode.EndPoint)
				startPoint 				= Point.Subtract(startPoint, scalVec);

			// Find collisions with ChartPanel bounds for PriceScale bound TextBox coordinates
			if (LineType == ChartLineType.HorizontalLine)
			{
				extPoint = endAdj;
				startPoint = startAdj;
			}
			else if (TextDisplayMode == TextMode.EndPoint)
			{
				extPoint = endPoint;
				snap 	 = false;
			}
			else
			{
				if (extPoint.X <= ChartPanel.X || extPoint.Y < ChartPanel.Y || extPoint.X > ChartPanel.X + ChartPanel.W || extPoint.Y > ChartPanel.Y + ChartPanel.H)
				{
					switch (LineIntersectsRect(startPoint, extPoint, new SharpDX.RectangleF(ChartPanel.X, ChartPanel.Y, ChartPanel.W, ChartPanel.H)))
					{
						case RectSide.Top:
							extPoint = FindIntersection(startPoint, extPoint, new Point(ChartPanel.X, ChartPanel.Y), new Point(ChartPanel.X + ChartPanel.W, ChartPanel.Y));
							break;
						case RectSide.Bottom:
							extPoint = FindIntersection(startPoint, extPoint, new Point(ChartPanel.X + ChartPanel.W, ChartPanel.Y + ChartPanel.H), new Point(ChartPanel.X, ChartPanel.Y + ChartPanel.H));
							break;
						case RectSide.Left:
							extPoint = FindIntersection(startPoint, extPoint, new Point(ChartPanel.X, ChartPanel.Y + ChartPanel.H), new Point(ChartPanel.X, ChartPanel.Y));
							break;
						case RectSide.Right:
							extPoint = FindIntersection(startPoint, extPoint, new Point(ChartPanel.X + ChartPanel.W, ChartPanel.Y), new Point(ChartPanel.X + ChartPanel.W, ChartPanel.Y + ChartPanel.H));
							break;
						default:
							return;
					}
				}
				
				if (startPoint.X <= ChartPanel.X || startPoint.Y < ChartPanel.Y || startPoint.X > ChartPanel.X + ChartPanel.W || startPoint.Y > ChartPanel.Y + ChartPanel.H)
				{
					switch (LineIntersectsRect(extPoint, startPoint, new SharpDX.RectangleF(ChartPanel.X, ChartPanel.Y, ChartPanel.W, ChartPanel.H)))
					{
						case RectSide.Top:
							startPoint = FindIntersection(extPoint, startPoint, new Point(ChartPanel.X, ChartPanel.Y), new Point(ChartPanel.X + ChartPanel.W, ChartPanel.Y));
							break;
						case RectSide.Bottom:
							startPoint = FindIntersection(extPoint, startPoint, new Point(ChartPanel.X + ChartPanel.W, ChartPanel.Y + ChartPanel.H), new Point(ChartPanel.X, ChartPanel.Y + ChartPanel.H));
							break;
						case RectSide.Left:
							startPoint = FindIntersection(extPoint, startPoint, new Point(ChartPanel.X, ChartPanel.Y + ChartPanel.H), new Point(ChartPanel.X, ChartPanel.Y));
							break;
						case RectSide.Right:
							startPoint = FindIntersection(extPoint, startPoint, new Point(ChartPanel.X + ChartPanel.W, ChartPanel.Y), new Point(ChartPanel.X + ChartPanel.W, ChartPanel.Y + ChartPanel.H));
							break;
						default:
							return;
					}
				}
				
				if (endPoint.X <= ChartPanel.X || endPoint.Y < ChartPanel.Y || endPoint.X > ChartPanel.X + ChartPanel.W || endPoint.Y > ChartPanel.Y + ChartPanel.H)
					priceOffScreen = true;
				
				if (endPoint.X == startPoint.X && startPoint.Y < endPoint.Y && priceOffScreen)
					extPoint.Y = ChartPanel.Y + ChartPanel.H;
			}
			
			// Scale coordinates by HorizontalOffset/VerticalOffset
			distVec 	= Point.Subtract(extPoint, startPoint);
			scalVec 	= Vector.Multiply(Vector.Divide(distVec, 100), HorizontalOffset);
			extPoint.X  -= HorizontalOffset;
			extPoint.Y 	-= VerticalOffset;

			// Get a Price or a Timestamp to append to the label
			switch (LineType)
			{
				case ChartLineType.HorizontalLine:
					priceToUse = StartAnchor.Price;
					break;
				default:
					priceToUse = priceOffScreen && TextDisplayMode == TextMode.PriceScale
							   ? chartScale.GetValueByY(endPoint.X >= startPoint.X
														? (float)FindIntersection(startPoint, endPoint, new Point(ChartPanel.W, ChartPanel.Y), new Point(ChartPanel.W, ChartPanel.Y + ChartPanel.H)).Y
						 								: (float)FindIntersection(startPoint, endPoint, new Point(ChartPanel.X, ChartPanel.Y), new Point(ChartPanel.X, ChartPanel.Y + ChartPanel.H)).Y)
							   : EndAnchor.Price;
					break;
			}
			
			// Round the price
			if (IsGlobalDrawingTool)
				pricetime = "Append Price/Time is not compatible with Global Drawing Objects";
			else if (!instrumentLoaded)
				pricetime = "Instrument Loading...";
			else
			{
				int precision = CalcPricePrecision(masterInst);
				double roundPrice = Math.Round(priceToUse, precision);
				pricetime = roundPrice.ToString("F" + precision.ToString());
			}
			
			// Check if we need to append price or time
			if (AppendPriceTime && DisplayText.Length > 0)
			{
				if (IsPosition == false)
					TextToDisplay = String.Format("{0} {1}  X", DisplayText, pricetime);
				else
					TextToDisplay = String.Format("{0} {1}", DisplayText, pricetime);
			}
			else if (AppendPriceTime)
				TextToDisplay = pricetime;
			
			// Use Label Font if one is not specified by template
			if (Font == null)
				Font = new NinjaTrader.Gui.Tools.SimpleFont(chartControl.Properties.LabelFont.Family.ToString(), 11);
			
			// Update DX Brushes
			if (offScreenDXBrushNeedsUpdate)
			{
				if (offScreenDXBrush != null)
					offScreenDXBrush.Dispose();
				offScreenDXBrush = offScreenMediaBrush.ToDxBrush(RenderTarget);
				offScreenDXBrushNeedsUpdate = false;
			}
			
			if (backgroundDXBrushNeedsUpdate)
			{
				if (backgroundDXBrush != null)
					backgroundDXBrush.Dispose();
				backgroundDXBrush = backgroundMediaBrush.ToDxBrush(RenderTarget);
				backgroundDXBrush.Opacity = (float)AreaOpacity / 100f;
				backgroundDXBrushNeedsUpdate = false;
			}
			
			SharpDX.Direct2D1.Brush txtBrush = null;
			string backgroundBrushStr = backgroundMediaBrush.ToString();
			
			if (blackForegroundBrushes.Contains(backgroundBrushStr))
				txtBrush = Brushes.Black.ToDxBrush(RenderTarget);
			else if (whiteForegroundBrushes.Contains(backgroundBrushStr))
				txtBrush = Brushes.White.ToDxBrush(RenderTarget);
			
			if (foregroundMediaBrush != null)
				txtBrush = foregroundMediaBrush.ToDxBrush(RenderTarget);
			
			// Draw TextBoxes
			switch (LineType)
			{
				case ChartLineType.HorizontalLine:
					DrawTextBox(snap, TextToDisplay, extPoint.X, extPoint.Y, txtBrush, backgroundDXBrush, OutlineStroke, 0);
					break;
				default:
					DrawTextBox(snap, TextToDisplay, extPoint.X, extPoint.Y, priceOffScreen && TextDisplayMode == TextMode.EndPointAtPriceScale ? offScreenDXBrush : txtBrush, backgroundDXBrush, OutlineStroke, 0);
					break;
			}
		}
		
		private void DrawTextBox(bool Snap, string displayText, double x, double y, SharpDX.Direct2D1.Brush txtBrush, SharpDX.Direct2D1.Brush bgBrush, Stroke stroke, float rotate)
		{
			const int padding = 4;
			
			// Text has changed, need to update cached TextLayout
			if (displayText != lastText)
				needsLayoutUpdate = true;
			lastText = displayText;
			
			// Update cachedTextLayout
			if (needsLayoutUpdate || cachedTextLayout == null)
			{
				SharpDX.DirectWrite.TextFormat textFormat = Font.ToDirectWriteTextFormat();
				cachedTextLayout = 	new SharpDX.DirectWrite.TextLayout(NinjaTrader.Core.Globals.DirectWriteFactory,
									displayText, textFormat, ChartPanel.X + ChartPanel.W,
									textFormat.FontSize);
				textFormat.Dispose();
				needsLayoutUpdate = false;
			}
			
			// Snap TextBox coordinates to ChartPanel when out of bounds
			if (Snap)
			{
				if (rotate == 1.5708f)
					y = Math.Max(ChartPanel.Y + cachedTextLayout.Metrics.Width + 2 * padding, y);
				else
				{
					y = Math.Min(ChartPanel.H + ChartPanel.Y - padding, y);
					y = Math.Max(ChartPanel.Y + cachedTextLayout.Metrics.Height + padding, y);
					x = Math.Max(ChartPanel.X + cachedTextLayout.Metrics.Width + 2 * padding, x);
				}
			}
			
			// Apply rotation
			RenderTarget.Transform = SharpDX.Matrix3x2.Rotation(rotate, new SharpDX.Vector2((float)x, (float)y));
			
			// Add padding to TextPlotPoint
			SharpDX.Vector2 TextPlotPoint = new System.Windows.Point(x - cachedTextLayout.Metrics.Width - padding * 2, y - cachedTextLayout.Metrics.Height).ToVector2();
			
			// Draw the TextBox
			if (displayText.Length > 0)
			{
	            SharpDX.RectangleF 					PLBoundRect		= new SharpDX.RectangleF((float)x - cachedTextLayout.Metrics.Width - padding * 3, (float)y - cachedTextLayout.Metrics.Height - padding / 2, cachedTextLayout.Metrics.Width + padding * 3, cachedTextLayout.Metrics.Height + padding);
				SharpDX.Direct2D1.RoundedRectangle 	PLRoundedRect 	= new SharpDX.Direct2D1.RoundedRectangle() { Rect = PLBoundRect, RadiusX = 0, RadiusY = 0 };
				RenderTarget.FillRoundedRectangle(PLRoundedRect, bgBrush);
				RenderTarget.DrawRoundedRectangle(PLRoundedRect, stroke.BrushDX, stroke.Width, stroke.StrokeStyle);
				
				// Draw the TextLayout
				RenderTarget.DrawTextLayout(TextPlotPoint, cachedTextLayout, txtBrush, SharpDX.Direct2D1.DrawTextOptions.NoSnap);
			}
			
			// Restore rotation
			RenderTarget.Transform = SharpDX.Matrix3x2.Identity;
		}
		
		private Point FindIntersection(Point p1, Point p2, Point p3, Point p4)
		{
			Point intersection = new Point();
			
		    // Get the segments' parameters.
		    double dx12 = p2.X - p1.X;
		    double dy12 = p2.Y - p1.Y;
		    double dx34 = p4.X - p3.X;
		    double dy34 = p4.Y - p3.Y;

		    // Solve for t1 and t2
		    double denominator = (dy12 * dx34 - dx12 * dy34);

		    double t1 = ((p1.X - p3.X) * dy34 + (p3.Y - p1.Y) * dx34) 
						/ denominator;
		    
			if (double.IsInfinity(t1))
		        intersection = new Point(double.NaN, double.NaN);

		    // Find the point of intersection.
		    intersection = new Point(Math.Max(p1.X + dx12 * t1, 0), p1.Y + dy12 * t1);
			return intersection;
		}
		
		private RectSide LineIntersectsRect(Point p1, Point p2, SharpDX.RectangleF r)
	    {

	        if (LineIntersectsLine(p1, p2, new Point(r.X, r.Y), new Point(r.X + r.Width, r.Y)) && p1.Y > r.Y)
				return RectSide.Top;
			if (LineIntersectsLine(p1, p2, new Point(r.X + r.Width, r.Y), new Point(r.X + r.Width, r.Y + r.Height)) && p1.X < r.X + r.Width)
				return RectSide.Right;
			if (LineIntersectsLine(p1, p2, new Point(r.X + r.Width, r.Y + r.Height), new Point(r.X, r.Y + r.Height)) && p1.Y < r.Y + r.Height)
				return RectSide.Bottom;
			if (LineIntersectsLine(p1, p2, new Point(r.X, r.Y + r.Height), new Point(r.X, r.Y)))
				return RectSide.Left;

			return RectSide.None;
		}

	    private bool LineIntersectsLine(Point l1p1, Point l1p2, Point l2p1, Point l2p2)
	    {
	        double q = (l1p1.Y - l2p1.Y) * (l2p2.X - l2p1.X) - (l1p1.X - l2p1.X) * (l2p2.Y - l2p1.Y);
	        double d = (l1p2.X - l1p1.X) * (l2p2.Y - l2p1.Y) - (l1p2.Y - l1p1.Y) * (l2p2.X - l2p1.X);

	        if( d == 0 )
	            return false;

	        double r = q / d;

	        q = (l1p1.Y - l2p1.Y) * (l1p2.X - l1p1.X) - (l1p1.X - l2p1.X) * (l1p2.Y - l1p1.Y);
	        double s = q / d;

	        if( r < 0 || r > 1 || s < 0 || s > 1 )
	            return false;

	        return true;
	    }
		
		private int CalcPricePrecision(MasterInstrument masterInst)
		{
			string tickSizeStr = masterInst.TickSize.ToString();
			if (tickSizeStr.Contains('-') == true)
				return Convert.ToInt32(tickSizeStr.Split('-')[1]);
			else
			{
				char[] separators = { ',', '.' };
				return tickSizeStr.Split(separators)[1].Length;
			}
		}
		
		#region Properties
		[Display(Name = "Text Horizontal Offset", Description = "Distance to offset from End Point", GroupName = "General", Order = 5)]
		[Range(0, 100)]
		public double HorizontalOffset
		{ get; set; }
		
		[Display(Name = "Text Vertical Offset", Description = "Distance from line", GroupName = "General", Order = 6)]
		[Range(-100, 100)]
		public double VerticalOffset
		{ get; set; }
		
		[ExcludeFromTemplate]
		[Display(Name = "Text", GroupName = "General", Order = 7)]
		[PropertyEditor("NinjaTrader.Gui.Tools.MultilineEditor")]
		public string DisplayText
		{
			get { return displayText; }
			set
			{
				if (displayText == value)
					return;
				displayText			= value;
				needsLayoutUpdate 	= true;
			}
		}
		
		[Display(Name = "Append Price/Time", GroupName = "General", Order = 8)]
		public bool AppendPriceTime
		{
			get { return appendPriceTime; }
			set
			{
				if (appendPriceTime == value)
					return;
				appendPriceTime			= value;
				needsLayoutUpdate		= true;
			}
		}
		
		[Display(Name = "Text Display Mode", GroupName = "General", Order = 10)]
		public TextMode TextDisplayMode
		{ get; set; }
		
		[Display(Name = "Font", GroupName = "General", Order = 11)]
		public Gui.Tools.SimpleFont Font
		{ get; set; }
		
		[XmlIgnore]
		[Display(GroupName = "General", Name = "Price Offscreen Text Color", Order = 12)]
		public Brush OffScreenBrush 
		{ 
			get { return offScreenMediaBrush; } 
			set
			{
				offScreenMediaBrush = value;
				offScreenDXBrushNeedsUpdate = true;
			}
		}
		
		[Browsable(false)]
		public string OffScreenBrushSerializable
		{
			get { return Serialize.BrushToString(OffScreenBrush); }
			set { OffScreenBrush = Serialize.StringToBrush(value); }
		}
		
		[Display(GroupName = "General", Name = "Text Box Outline", Order = 100)]
		public Stroke OutlineStroke { get; set; }
		
		[XmlIgnore]
		[Display(GroupName = "General", Name = "Text Box Background Color", Order = 101)]
		public Brush BackgroundBrush 
		{ 
			get { return backgroundMediaBrush; } 
			set
			{
				backgroundMediaBrush = value;
				backgroundDXBrushNeedsUpdate = true;
			}
		}
		
		[Browsable(false)]
		public string BackgroundBrushSerializable
		{
			get { return Serialize.BrushToString(BackgroundBrush); }
			set { BackgroundBrush = Serialize.StringToBrush(value); }
		}
		
		[Display(GroupName = "General", Name = "Text Box Background Opacity", Order = 102)]
		public int AreaOpacity { get; set; }
		
		[XmlIgnore]
		[Display(GroupName = "General", Name = "Text Box Foreground Color", Order = 103)]
		public Brush ForegroundBrush 
		{ 
			get { return foregroundMediaBrush; } 
			set { foregroundMediaBrush = value; }
		}
		
		[Browsable(false)]
		public string ForegroundBrushSerializable
		{
			get { return Serialize.BrushToString(ForegroundBrush); }
			set { ForegroundBrush = Serialize.StringToBrush(value); }
		}
		#endregion
	}
	
	#region NinjaScript Overloads
	public static partial class Draw
	{
		private static T DrawCedLabeledLineTypeCore<T>(NinjaScriptBase owner, bool isAutoScale, string tag,
										int startBarsAgo, DateTime startTime, double startY, int endBarsAgo, DateTime endTime, double endY, string displayText, bool isPosition,
										Brush brush, Brush bgBrush, DashStyleHelper dashStyle, int width, bool isGlobal, bool isLocked, string templateName) where T : CedLabeledLine
		{
			if (owner == null)
				throw new ArgumentException("owner");

			if (string.IsNullOrWhiteSpace(tag))
				throw new ArgumentException(@"tag cant be null or empty", "tag");

			if (isGlobal && tag[0] != GlobalDrawingToolManager.GlobalDrawingToolTagPrefix)
				tag = string.Format("{0}{1}", GlobalDrawingToolManager.GlobalDrawingToolTagPrefix, tag);

			T lineT = DrawingTool.GetByTagOrNew(owner, typeof(T), tag, templateName) as T;

			if (lineT == null)
				return null;

			if (lineT is ChartTraderLine)
			{
				if (startY.ApproxCompare(double.MinValue) == 0)
					throw new ArgumentException("missing horizontal line Y");
			}
			else if (startTime == Core.Globals.MinDate && endTime == Core.Globals.MinDate && startBarsAgo == int.MinValue && endBarsAgo == int.MinValue)
				throw new ArgumentException("bad start/end date/time");

			DrawingTool.SetDrawingToolCommonValues(lineT, tag, isAutoScale, owner, isGlobal);

			// don't nuke existing anchor refs on the instance
			ChartAnchor startAnchor;

			// check if it's one of the single anchor lines
			if (lineT is ChartTraderLine)
			{
				startAnchor = DrawingTool.CreateChartAnchor(owner, startBarsAgo, startTime, startY);
				startAnchor.CopyDataValues(lineT.StartAnchor);
			}
			else
			{
				startAnchor				= DrawingTool.CreateChartAnchor(owner, startBarsAgo, startTime, startY);
				ChartAnchor endAnchor	= DrawingTool.CreateChartAnchor(owner, endBarsAgo, endTime, endY);
				startAnchor.CopyDataValues(lineT.StartAnchor);
				endAnchor.CopyDataValues(lineT.EndAnchor);
			}

			if (brush != null)
				lineT.Stroke = new Stroke(brush, dashStyle, width) { RenderTarget = lineT.Stroke.RenderTarget };
			
			lineT.DisplayText = displayText;
			lineT.IsPosition = isPosition;
			lineT.BackgroundBrush = bgBrush;
			lineT.IsLocked = isLocked;
			if (lineT.IsPosition == true)
				lineT.ZOrder = int.MaxValue - 1;
			else if (lineT.IsPosition == false)
				lineT.ZOrder = int.MaxValue;
				
			lineT.SetState(State.Active);
			return lineT;
		}
		
		// chart trader line overloads
		private static ChartTraderLine ChartTraderLineCore(NinjaScriptBase owner, bool isAutoScale, string tag, double y,
			string displayText, bool isPosition, Brush brush, Brush bgBrush, DashStyleHelper dashStyle, int width, bool isLocked)
		{
			return DrawCedLabeledLineTypeCore<ChartTraderLine>(owner, isAutoScale, tag, 0, Core.Globals.MinDate, y, 0, Core.Globals.MinDate,
											y, displayText, isPosition, brush, bgBrush, dashStyle, width, false, isLocked, null);
		}

		public static ChartTraderLine ChartTraderLine(NinjaScriptBase owner, string tag, bool isAutoscale, double y,
			string displayText, bool isPosition, Brush brush, Brush bgBrush, int width, bool isLocked, bool drawOnPricePanel)
		{
			return DrawingTool.DrawToggledPricePanel(owner, drawOnPricePanel, () =>
				ChartTraderLineCore(owner, isAutoscale, tag, y, displayText, isPosition, brush, bgBrush, DashStyleHelper.Solid, width, isLocked));
		}
	}
	#endregion
	
	/// <summary>
	/// Represents an interface that exposes information regarding a Line IDrawingTool.
	/// </summary>
	public class CedLine : DrawingTool
	{
		// this line class takes care of all stock line types, so we use this to keep track
		// of what kind of line instances this is. Localization is not needed because it's not visible on ui
		protected enum ChartLineType
		{
			ArrowLine,
			ExtendedLine,
			HorizontalLine,
			Line,
			Ray,
			VerticalLine,
		}

		public override IEnumerable<ChartAnchor> Anchors { get { return new[] { StartAnchor, EndAnchor }; } }
		[Display(Order = 2)]
		public ChartAnchor	EndAnchor		{ get; set; }
		[Display(Order = 1)]
		public ChartAnchor StartAnchor		{ get; set; }

		[CLSCompliant(false)]
		protected		SharpDX.Direct2D1.PathGeometry		ArrowPathGeometry;
		protected	const	double							cursorSensitivity		= 15;
		protected		ChartAnchor							editingAnchor;

		[Browsable(false)]
		[XmlIgnore]
		protected ChartLineType LineType { get; set; }

		[Display(ResourceType = typeof(Custom.Resource), GroupName = "NinjaScriptGeneral", Name = "NinjaScriptDrawingToolLine", Order = 99)]
		public Stroke Stroke { get; set; }

		public override bool SupportsAlerts { get { return true; } }

		protected ChartAnchor Anchor45(ChartAnchor starAnchort, ChartAnchor endAnchor, ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale)
		{
			if (!Keyboard.IsKeyDown(Key.LeftShift) && !Keyboard.IsKeyDown(Key.RightShift))
				return endAnchor;

			Point	startPoint	= starAnchort.GetPoint(chartControl, chartPanel, chartScale);
			Point	endPoint	= endAnchor.GetPoint(chartControl, chartPanel, chartScale);

			double	diffX		= endPoint.X - startPoint.X;
			double	diffY		= endPoint.Y - startPoint.Y;

			double	length		= Math.Sqrt(diffX * diffX + diffY * diffY);

			double	angle		= Math.Atan2(diffY, diffX);

			double step			= Math.PI / 8;
			double targetAngle	= 0;

			if (angle > Math.PI - step || angle < -Math.PI + step)	targetAngle = Math.PI;
			else if (angle > Math.PI - step * 3)					targetAngle = Math.PI - step * 2;
			else if (angle > Math.PI - step * 5)					targetAngle = Math.PI - step * 4;
			else if (angle > Math.PI - step * 7)					targetAngle = Math.PI - step * 6;
			else if (angle < -Math.PI + step * 3)					targetAngle = -Math.PI + step * 2;
			else if (angle < -Math.PI + step * 5)					targetAngle = -Math.PI + step * 4;
			else if (angle < -Math.PI + step * 7)					targetAngle = -Math.PI + step * 6;

			Point		targetPoint = new Point(startPoint.X + Math.Cos(targetAngle) * length, startPoint.Y + Math.Sin(targetAngle) * length);
			ChartAnchor	ret			= new ChartAnchor();

			ret.UpdateFromPoint(targetPoint, chartControl, chartScale);

			if (startPoint.X == targetPoint.X)
			{
				ret.Time		= starAnchort.Time;
				ret.SlotIndex	=starAnchort.SlotIndex;
			}
			else if (startPoint.Y == targetPoint.Y)
				ret.Price = starAnchort.Price;

			return ret;
		}

		public override Cursor GetCursor(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, Point point)
		{
			switch (DrawingState)
			{
				case DrawingState.Building:	return Cursors.Pen;
				case DrawingState.Moving:	return IsLocked ? Cursors.No : Cursors.SizeAll;
				case DrawingState.Editing:
					if (IsLocked)
						return Cursors.No;
					if (LineType == ChartLineType.HorizontalLine || LineType == ChartLineType.VerticalLine)
						return Cursors.SizeAll;
					return editingAnchor == StartAnchor ? Cursors.SizeNESW : Cursors.SizeNWSE;
				default:
					// draw move cursor if cursor is near line path anywhere
					Point startPoint = StartAnchor.GetPoint(chartControl, chartPanel, chartScale);

					if (LineType == ChartLineType.HorizontalLine || LineType == ChartLineType.VerticalLine)
					{
						// just go by single axis since we know the entire lines position
						if (LineType == ChartLineType.VerticalLine && Math.Abs(point.X - startPoint.X) <= cursorSensitivity)
							return IsLocked ? Cursors.Arrow : Cursors.SizeAll;
						if (LineType == ChartLineType.HorizontalLine && Math.Abs(point.Y - startPoint.Y) <= cursorSensitivity)
							return IsLocked ? Cursors.Arrow : Cursors.SizeAll;
						return null;
					}

					ChartAnchor closest = GetClosestAnchor(chartControl, chartPanel, chartScale, cursorSensitivity, point);
					if (closest != null)
					{
						if (IsLocked)
							return Cursors.Arrow;
						return closest == StartAnchor ? Cursors.SizeNESW : Cursors.SizeNWSE;
					}

					Point	endPoint		= EndAnchor.GetPoint(chartControl, chartPanel, chartScale);
					Point	minPoint		= startPoint;
					Point	maxPoint		= endPoint;

					// if we're an extended or ray line, we want to use min & max points in vector used for hit testing
					if (LineType == ChartLineType.ExtendedLine)
					{
						// adjust vector to include min all the way to max points
						minPoint	= GetExtendedPoint(chartControl, chartPanel, chartScale, EndAnchor, StartAnchor);
						maxPoint	= GetExtendedPoint(chartControl, chartPanel, chartScale, StartAnchor, EndAnchor);
					}
					else if (LineType == ChartLineType.Ray)
						maxPoint	= GetExtendedPoint(chartControl, chartPanel, chartScale, StartAnchor, EndAnchor);

					Vector	totalVector	= maxPoint - minPoint;
					return MathHelper.IsPointAlongVector(point, minPoint, totalVector, cursorSensitivity) ?
						IsLocked ? Cursors.Arrow : Cursors.SizeAll : null;
			}
		}

		public override IEnumerable<AlertConditionItem> GetAlertConditionItems()
		{
			yield return new AlertConditionItem
			{
				Name					= Custom.Resource.NinjaScriptDrawingToolLine,
				ShouldOnlyDisplayName	= true
			};
		}

		public sealed override Point[] GetSelectionPoints(ChartControl chartControl, ChartScale chartScale)
		{
			ChartPanel	chartPanel	= chartControl.ChartPanels[chartScale.PanelIndex];
			Point		startPoint	= StartAnchor.GetPoint(chartControl, chartPanel, chartScale);
			Point		endPoint	= EndAnchor.GetPoint(chartControl, chartPanel, chartScale);

			int			totalWidth	= chartPanel.W + chartPanel.X;
			int			totalHeight	= chartPanel.Y + chartPanel.H;

			if (LineType == ChartLineType.VerticalLine)
				return new[] { new Point(startPoint.X, chartPanel.Y), new Point(startPoint.X, chartPanel.Y + ((totalHeight - chartPanel.Y) / 2d)), new Point(startPoint.X, totalHeight) };
			if (LineType == ChartLineType.HorizontalLine)
				return new[] { new Point(chartPanel.X, startPoint.Y), new Point(totalWidth / 2d, startPoint.Y), new Point(totalWidth, startPoint.Y) };

			//Vector strokeAdj = new Vector(Stroke.Width / 2, Stroke.Width / 2);
			Point midPoint = startPoint + ((endPoint - startPoint) / 2);
			return new[]{ startPoint, midPoint, endPoint };
		}

		public override bool IsAlertConditionTrue(AlertConditionItem conditionItem, Condition condition, ChartAlertValue[] values, ChartControl chartControl, ChartScale chartScale)
		{
			if (values.Length < 1)
				return false;
			ChartPanel chartPanel = chartControl.ChartPanels[PanelIndex];
			// h line and v line have much more simple alert handling
			if (LineType == ChartLineType.HorizontalLine)
			{
				double barVal	= values[0].Value;
				double lineVal	= conditionItem.Offset.Calculate(StartAnchor.Price, AttachedTo.Instrument);

				switch (condition)
				{
					case Condition.Equals:			return barVal.ApproxCompare(lineVal) == 0;
					case Condition.NotEqual:		return barVal.ApproxCompare(lineVal) != 0;
					case Condition.Greater:			return barVal > lineVal;
					case Condition.GreaterEqual:	return barVal >= lineVal;
					case Condition.Less:			return barVal < lineVal;
					case Condition.LessEqual:		return barVal <= lineVal;
					case Condition.CrossAbove:
					case Condition.CrossBelow:
						Predicate<ChartAlertValue> predicate = v =>
						{
							if (condition == Condition.CrossAbove)
								return v.Value > lineVal;
							return v.Value < lineVal;
						};
						return MathHelper.DidPredicateCross(values, predicate);
				}
				return false;
			}

			// get start / end points of what is absolutely shown for our vector
			Point lineStartPoint	= StartAnchor.GetPoint(chartControl, chartPanel, chartScale);
			Point lineEndPoint		= EndAnchor.GetPoint(chartControl, chartPanel, chartScale);

			if (LineType == ChartLineType.ExtendedLine || LineType == ChartLineType.Ray)
			{
				// need to adjust vector to rendered extensions
				Point maxPoint = GetExtendedPoint(chartControl, chartPanel, chartScale, StartAnchor, EndAnchor);
				if (LineType == ChartLineType.ExtendedLine)
				{
					Point minPoint = GetExtendedPoint(chartControl, chartPanel, chartScale,EndAnchor, StartAnchor);
					lineStartPoint = minPoint;
				}
				lineEndPoint = maxPoint;
			}

			double minLineX = double.MaxValue;
			double maxLineX = double.MinValue;

			foreach (Point point in new[]{lineStartPoint, lineEndPoint})
			{
				minLineX = Math.Min(minLineX, point.X);
				maxLineX = Math.Max(maxLineX, point.X);
			}

			// first thing, if our smallest x is greater than most recent bar, we have nothing to do yet.
			// do not try to check Y because lines could cross through stuff
			double firstBarX = values[0].ValueType == ChartAlertValueType.StaticValue ? minLineX : chartControl.GetXByTime(values[0].Time);
			double firstBarY = chartScale.GetYByValue(values[0].Value);

			// dont have to take extension into account as its already handled in min/max line x

			// bars completely passed our line
			if (maxLineX < firstBarX)
				return false;

			// bars not yet to our line
			if (minLineX > firstBarX)
				return false;

			// NOTE: normalize line points so the leftmost is passed first. Otherwise, our vector
			// math could end up having the line normal vector being backwards if user drew it backwards.
			// but we dont care the order of anchors, we want 'up' to mean 'up'!
			Point leftPoint		= lineStartPoint.X < lineEndPoint.X ? lineStartPoint : lineEndPoint;
			Point rightPoint	= lineEndPoint.X > lineStartPoint.X ? lineEndPoint : lineStartPoint;

			Point barPoint = new Point(firstBarX, firstBarY);
			// NOTE: 'left / right' is relative to if line was vertical. it can end up backwards too
			MathHelper.PointLineLocation pointLocation = MathHelper.GetPointLineLocation(leftPoint, rightPoint, barPoint);
			// for vertical things, think of a vertical line rotated 90 degrees to lay flat, where it's normal vector is 'up'
			switch (condition)
			{
				case Condition.Greater:			return pointLocation == MathHelper.PointLineLocation.LeftOrAbove;
				case Condition.GreaterEqual:	return pointLocation == MathHelper.PointLineLocation.LeftOrAbove || pointLocation == MathHelper.PointLineLocation.DirectlyOnLine;
				case Condition.Less:			return pointLocation == MathHelper.PointLineLocation.RightOrBelow;
				case Condition.LessEqual:		return pointLocation == MathHelper.PointLineLocation.RightOrBelow || pointLocation == MathHelper.PointLineLocation.DirectlyOnLine;
				case Condition.Equals:			return pointLocation == MathHelper.PointLineLocation.DirectlyOnLine;
				case Condition.NotEqual:		return pointLocation != MathHelper.PointLineLocation.DirectlyOnLine;
				case Condition.CrossAbove:
				case Condition.CrossBelow:
					Predicate<ChartAlertValue> predicate = v =>
					{
						double barX = chartControl.GetXByTime(v.Time);
						double barY = chartScale.GetYByValue(v.Value);
						Point stepBarPoint = new Point(barX, barY);
						MathHelper.PointLineLocation ptLocation = MathHelper.GetPointLineLocation(leftPoint, rightPoint, stepBarPoint);
						if (condition == Condition.CrossAbove)
							return ptLocation == MathHelper.PointLineLocation.LeftOrAbove;
						return ptLocation == MathHelper.PointLineLocation.RightOrBelow;
					};
					return MathHelper.DidPredicateCross(values, predicate);
			}

			return false;
		}

		public override bool IsVisibleOnChart(ChartControl chartControl, ChartScale chartScale, DateTime firstTimeOnChart, DateTime lastTimeOnChart)
		{
			if (DrawingState == DrawingState.Building)
				return true;

			DateTime	minTime = Core.Globals.MaxDate;
			DateTime	maxTime = Core.Globals.MinDate;

			if (LineType != ChartLineType.ExtendedLine && LineType != ChartLineType.Ray)
			{
				// make sure our 1 anchor is in time frame
				if (LineType == ChartLineType.VerticalLine)
					return StartAnchor.Time >= firstTimeOnChart && StartAnchor.Time <= lastTimeOnChart;

				// check at least one of our anchors is in horizontal time frame
				foreach (ChartAnchor anchor in Anchors)
				{
					if (anchor.Time < minTime)
						minTime = anchor.Time;
					if (anchor.Time > maxTime)
						maxTime = anchor.Time;
				}
			}
			else
			{
				// extended line, rays: here we'll get extended point and see if they're on scale
				ChartPanel	panel		= chartControl.ChartPanels[PanelIndex];
				Point		startPoint	= StartAnchor.GetPoint(chartControl, panel, chartScale);

				Point		minPoint	= startPoint;
				Point		maxPoint	= GetExtendedPoint(chartControl, panel, chartScale, StartAnchor, EndAnchor);

				if (LineType == ChartLineType.ExtendedLine)
					minPoint = GetExtendedPoint(chartControl, panel, chartScale, EndAnchor, StartAnchor);

				foreach (Point pt in new[] { minPoint, maxPoint })
				{
					DateTime time = chartControl.GetTimeByX((int) pt.X);
					if (time > maxTime)
						maxTime = time;
					if (time < minTime)
						minTime = time;
				}
			}

			// check offscreen vertically. make sure to check the line doesnt cut through the scale, so check both are out
			if (LineType == ChartLineType.HorizontalLine && (StartAnchor.Price < chartScale.MinValue || StartAnchor.Price > chartScale.MaxValue) && !IsAutoScale)
				return false; // horizontal line only has one anchor to whiff

			// hline extends, but otherwise try to check if line horizontally crosses through visible chart times in some way
			if (LineType != ChartLineType.HorizontalLine && (minTime > lastTimeOnChart || maxTime < firstTimeOnChart))
				return false;

			return true;
		}

		public override void OnCalculateMinMax()
		{
			MinValue = double.MaxValue;
			MaxValue = double.MinValue;

			if (!IsVisible)
				return;

			// make sure to set good min/max values on single click lines as well, in case anchor left in editing
			if (LineType == ChartLineType.HorizontalLine)
				MinValue = MaxValue = Anchors.First().Price;
			else if (LineType != ChartLineType.VerticalLine)
			{
				// return min/max values only if something has been actually drawn
				if (Anchors.Any(a => !a.IsEditing))
					foreach (ChartAnchor anchor in Anchors)
					{
						MinValue = Math.Min(anchor.Price, MinValue);
						MaxValue = Math.Max(anchor.Price, MaxValue);
					}
			}
		}

		public override void OnMouseDown(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, ChartAnchor dataPoint)
		{
			switch (DrawingState)
			{
				case DrawingState.Building:
					if (StartAnchor.IsEditing)
					{
						dataPoint.CopyDataValues(StartAnchor);
						StartAnchor.IsEditing = false;

						// these lines only need one anchor, so stop editing end anchor too
						if (LineType == ChartLineType.HorizontalLine || LineType == ChartLineType.VerticalLine)
							EndAnchor.IsEditing = false;

						// give end anchor something to start with so we dont try to render it with bad values right away
						dataPoint.CopyDataValues(EndAnchor);
					}
					else if (EndAnchor.IsEditing)
					{
						dataPoint.CopyDataValues(EndAnchor);
						EndAnchor.IsEditing = false;
					}

					// is initial building done (both anchors set)
					if (!StartAnchor.IsEditing && !EndAnchor.IsEditing)
					{
						DrawingState = DrawingState.Normal;
						IsSelected = false;
					}
					break;
				case DrawingState.Normal:
					Point point = dataPoint.GetPoint(chartControl, chartPanel, chartScale);
					// see if they clicked near a point to edit, if so start editing
					if (LineType == ChartLineType.HorizontalLine || LineType == ChartLineType.VerticalLine)
					{
						if (GetCursor(chartControl, chartPanel, chartScale, point) == null)
							IsSelected = false;
						else
						{
							// we dont care here, since we're moving just one anchor
							editingAnchor = StartAnchor;
						}
					}
					else
						editingAnchor = GetClosestAnchor(chartControl, chartPanel, chartScale, cursorSensitivity, point);

					if (editingAnchor != null)
					{
						editingAnchor.IsEditing = true;
						DrawingState = DrawingState.Editing;
					}
					else
					{
						if (GetCursor(chartControl, chartPanel, chartScale, point) != null)
							DrawingState = DrawingState.Moving;
						else
						// user whiffed.
							IsSelected = false;
					}
					break;
			}
		}

		public override void OnMouseMove(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, ChartAnchor dataPoint)
		{
			if (IsLocked && DrawingState != DrawingState.Building)
				return;

			IgnoresSnapping = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);

			if (DrawingState == DrawingState.Building)
			{
				// start anchor will not be editing here because we start building as soon as user clicks, which
				// plops down a start anchor right away
				if (EndAnchor.IsEditing)
					Anchor45(StartAnchor, dataPoint, chartControl, chartPanel, chartScale).CopyDataValues(EndAnchor);
			}
			else if (DrawingState == DrawingState.Editing && editingAnchor != null)
			{
				// if its a line with two anchors, update both x/y at once
				if (LineType != ChartLineType.HorizontalLine && LineType != ChartLineType.VerticalLine)
				{
					ChartAnchor startAnchor = editingAnchor == StartAnchor ? EndAnchor : StartAnchor;
					Anchor45(startAnchor, dataPoint, chartControl, chartPanel, chartScale).CopyDataValues(editingAnchor);
				}
				else if (LineType != ChartLineType.VerticalLine)
				{
					// horizontal line only needs Y value updated
					editingAnchor.Price = dataPoint.Price;
					EndAnchor.Price		= dataPoint.Price;
				}
				else
				{
					// vertical line only needs X value updated
					editingAnchor.Time		= dataPoint.Time;
					editingAnchor.SlotIndex	= dataPoint.SlotIndex;
				}
			}
			else if (DrawingState == DrawingState.Moving)
				foreach (ChartAnchor anchor in Anchors)
					// only move anchor values as needed depending on line type
					if (LineType == ChartLineType.HorizontalLine)
						anchor.MoveAnchorPrice(InitialMouseDownAnchor, dataPoint, chartControl, chartPanel, chartScale, this);
					else if (LineType == ChartLineType.VerticalLine)
						anchor.MoveAnchorTime(InitialMouseDownAnchor, dataPoint, chartControl, chartPanel, chartScale, this);
					else
						anchor.MoveAnchor(InitialMouseDownAnchor, dataPoint, chartControl, chartPanel, chartScale, this);
			//lastMouseMovePoint.Value, point, chartControl, chartScale);
		}

		public override void OnMouseUp(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, ChartAnchor dataPoint)
		{
			// simply end whatever moving
			if (DrawingState == DrawingState.Moving || DrawingState == DrawingState.Editing)
				DrawingState = DrawingState.Normal;
			if (editingAnchor != null)
				editingAnchor.IsEditing = false;
			editingAnchor = null;
		}

		public override void OnRender(ChartControl chartControl, ChartScale chartScale)
		{
			if (Stroke == null)
				return;

			Stroke.RenderTarget									= RenderTarget;

			SharpDX.Direct2D1.AntialiasMode	oldAntiAliasMode	= RenderTarget.AntialiasMode;
			RenderTarget.AntialiasMode							= SharpDX.Direct2D1.AntialiasMode.PerPrimitive;
			ChartPanel						panel				= chartControl.ChartPanels[chartScale.PanelIndex];
			Point							startPoint			= StartAnchor.GetPoint(chartControl, panel, chartScale);

			// align to full pixel to avoid unneeded aliasing
			double							strokePixAdj		= ((double)(Stroke.Width % 2)).ApproxCompare(0) == 0 ? 0.5d : 0d;
			Vector							pixelAdjustVec		= new Vector(strokePixAdj, strokePixAdj);

			if (LineType == ChartLineType.HorizontalLine || LineType == ChartLineType.VerticalLine)
			{
				// horizontal and vertical line only need single anchor (StartAnchor) to draw
				// so just go by panel bounds. Keep in mind the panel may not start at 0
				Point startAdj	= (LineType == ChartLineType.HorizontalLine ? new Point(panel.X, startPoint.Y) : new Point(startPoint.X, panel.Y)) + pixelAdjustVec;
				Point endAdj	= (LineType == ChartLineType.HorizontalLine ? new Point(panel.X + panel.W, startPoint.Y) : new Point(startPoint.X, panel.Y + panel.H)) + pixelAdjustVec;
				RenderTarget.DrawLine(startAdj.ToVector2(), endAdj.ToVector2(), Stroke.BrushDX, Stroke.Width, Stroke.StrokeStyle);
				return;
			}

			Point					endPoint			= EndAnchor.GetPoint(chartControl, panel, chartScale);

			// convert our start / end pixel points to directx 2d vectors
			Point					endPointAdjusted	= endPoint + pixelAdjustVec;
			SharpDX.Vector2			endVec				= endPointAdjusted.ToVector2();
			Point					startPointAdjusted	= startPoint + pixelAdjustVec;
			SharpDX.Vector2			startVec			= startPointAdjusted.ToVector2();
			SharpDX.Direct2D1.Brush	tmpBrush			= IsInHitTest ? chartControl.SelectionBrush : Stroke.BrushDX;

			// if a plain ol' line, then we're all done
			// if we're an arrow line, make sure to draw the actual line. for extended lines, only a single
			// line to extended points is drawn below, to avoid unneeded multiple DrawLine calls
			if (LineType == ChartLineType.Line)
			{
				RenderTarget.DrawLine(startVec, endVec, tmpBrush, Stroke.Width, Stroke.StrokeStyle);
				return;
			}
			// we have a line type with extensions (ray / extended line) or additional drawing needed
			// create a line vector to easily calculate total length
			Vector lineVector = endPoint - startPoint;
			lineVector.Normalize();

			if (LineType != ChartLineType.ArrowLine)
			{
				Point minPoint = startPointAdjusted;
				Point maxPoint = GetExtendedPoint(chartControl, panel, chartScale, StartAnchor, EndAnchor);//GetExtendedPoint(startPoint, endPoint); //
				if (LineType == ChartLineType.ExtendedLine)
					minPoint = GetExtendedPoint(chartControl, panel, chartScale, EndAnchor, StartAnchor);
				RenderTarget.DrawLine(minPoint.ToVector2(), maxPoint.ToVector2(), tmpBrush, Stroke.Width, Stroke.StrokeStyle);
			}
			else
			{
				// translate to the angle the line is pointing to simplify drawing the arrow rect
				// the ArrowPathGeometry is created with 0,0 as arrow point, so transform there as well
				// note rotation is against zero, not end vector
				RenderTarget.DrawLine(startVec, endVec, tmpBrush, Stroke.Width, Stroke.StrokeStyle);
				float				vectorAngle			= -(float)Math.Atan2(lineVector.X, lineVector.Y);

				// adjust end vector slightly to cover edges of line stroke
				Vector				adjustVector		= lineVector * 5;
				SharpDX.Vector2		arrowPointVec		= new SharpDX.Vector2((float)(endVec.X + adjustVector.X), (float)(endVec.Y + adjustVector.Y));
				// rotate and scale our arrow to stroke size, the geo is created as a fixed width of 10
				// make sure to rotate, then scale before translating so we end up in the right place
				SharpDX.Matrix3x2	transformMatrix2	= SharpDX.Matrix3x2.Rotation(vectorAngle, SharpDX.Vector2.Zero)
					* SharpDX.Matrix3x2.Scaling((float)Math.Max(1.0f, Stroke.Width *.45) + 0.25f) * SharpDX.Matrix3x2.Translation(arrowPointVec);
				if (ArrowPathGeometry == null)
				{

					// create our arrow directx geometry.
					// just make a static size we will scale when drawing
					// all relative to top of line
					// nudge up y slightly to cover up top of stroke (instead of using zero),
					// half the stroke will hide any overlap
					ArrowPathGeometry								= new SharpDX.Direct2D1.PathGeometry(Core.Globals.D2DFactory);
					SharpDX.Direct2D1.GeometrySink	geometrySink	= ArrowPathGeometry.Open();
					SharpDX.Vector2					top				= new SharpDX.Vector2(0, Stroke.Width * 0.5f);
					float							arrowWidth		= 6f;

					geometrySink.BeginFigure(top, SharpDX.Direct2D1.FigureBegin.Filled);
					geometrySink.AddLine(new SharpDX.Vector2(arrowWidth, -arrowWidth));
					geometrySink.AddLine(new SharpDX.Vector2(-arrowWidth, -arrowWidth));
					geometrySink.AddLine(top);// cap off figure
					geometrySink.EndFigure(SharpDX.Direct2D1.FigureEnd.Closed);
					geometrySink.Close();
				}

				RenderTarget.Transform = transformMatrix2;

				RenderTarget.FillGeometry(ArrowPathGeometry, tmpBrush);
				RenderTarget.Transform = SharpDX.Matrix3x2.Identity;
			}
			RenderTarget.AntialiasMode	= oldAntiAliasMode;
		}

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				LineType					= ChartLineType.Line;
				Name						= "CedLine";
				DrawingState				= DrawingState.Building;

				EndAnchor					= new ChartAnchor
				{
					IsEditing		= true,
					DrawingTool		= this,
					DisplayName		= Custom.Resource.NinjaScriptDrawingToolAnchorEnd,
					IsBrowsable		= true
				};

				StartAnchor			= new ChartAnchor
				{
					IsEditing		= true,
					DrawingTool		= this,
					DisplayName		= Custom.Resource.NinjaScriptDrawingToolAnchorStart,
					IsBrowsable		= true
				};

				// a normal line with both end points has two anchors
				Stroke						= new Stroke(Brushes.CornflowerBlue, 2f);
			}
			else if (State == State.Terminated)
			{
				// release any device resources
				Dispose();
			}
		}
	}
}
