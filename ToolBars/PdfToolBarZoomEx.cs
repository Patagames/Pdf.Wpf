using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Patagames.Pdf.Net.Controls.Wpf.ToolBars
{
	/// <summary>
	/// Provides a container for Windows toolbar objects with predefined functionality for zooming
	/// </summary>
	public class PdfToolBarZoomEx : PdfToolBar
	{
		#region Private fields
		private int _trackBarWidth = 104;
		private int _trackBarHeight = 32;
		private int _currentZoomLevel = 0;
		private double[] _zoomLevel = { 8.33f, 12.5f, 25, 33.33f, 50, 66.67f, 75, 100, 125, 150, 200, 300, 400, 600, 800 };
		#endregion

		#region Public properties
		/// <summary>
		/// Gets or sets the array with zoom values for ComboBox or TrackBar
		/// </summary>
		public double[] ZoomLevel
		{
			get
			{
				return _zoomLevel;
			}
			set
			{
				if (value != null && value.Length > 0)
				{
					_zoomLevel = value;
					this.Items.Clear();
					InitializeButtons();
					UpdateButtons();
				}
			}
		}

		/// <summary>
		/// Calculate the current zoom
		/// </summary>
		public double Zoom
		{
			get
			{
				if (PdfViewer == null || PdfViewer.Document == null || PdfViewer.Document.Pages.Count == 0)
					return 0;
				var page = PdfViewer.Document.Pages.CurrentPage;
				switch (PdfViewer.SizeMode)
				{
					case SizeModes.Zoom:
						return PdfViewer.Zoom * 100;
					default:
						return PdfViewer.CalcActualRect(PdfViewer.CurrentIndex).Width * 100 / page.Width;
				}
			}
		}
		#endregion

		#region Constructor, Destructor, Initialisation

		private Button CreateZoomDropDown()
		{
			var btn = CreateButton(
				"btnDropDownZoomEx",
				Properties.Resources.btnZoomComboText,
				Properties.Resources.btnZoomComboToolTipText,
				null,
				ZoomLevel_ButtonClick,
				16, 16,
				ImageTextType.TextOnly);
			btn.MinWidth = 80;

			double x = 0;
			double y = 0;
			var fig = new PathFigure();
			fig.StartPoint = new Point(x, y);
			fig.Segments.Add(new LineSegment(new Point(x + 10.43, y), true));
			fig.Segments.Add(new LineSegment(new Point(x + 5.215, y + 6.099), true));
			fig.Segments.Add(new LineSegment(new Point(x, y), true));
			fig.IsClosed = true;

			var path = new Path();
			path.Margin = new Thickness(4);
			path.VerticalAlignment = VerticalAlignment.Center;
			path.HorizontalAlignment = HorizontalAlignment.Right;
			path.Width = 6;
			path.Fill = new SolidColorBrush(Color.FromArgb(255, 82, 125, 181));
			path.Stretch = System.Windows.Media.Stretch.Uniform;
			path.Data = new PathGeometry();
			(path.Data as PathGeometry).Figures.Add(fig);

			(btn.Content as StackPanel).Orientation = Orientation.Horizontal;
			(btn.Content as StackPanel).Children.Add(path);
			btn.ContextMenu = new ContextMenu();
			btn.ContextMenu.Opened += ZoomLevel_DropDownOpening;

			MenuItem item = null;
			for (int i = ZoomLevel.Length - 1; i >= 0; i--)
			{
				item = new MenuItem();
				item.Header = string.Format("{0:0.00}%", ZoomLevel[i]);
				item.Name = "btnZoomLevel_" + ZoomLevel[i].ToString().Replace(".","_");
				item.Tag = i;
				item.Click += ZoomLevel_Click;
				btn.ContextMenu.Items.Add(item);
			}
			btn.ContextMenu.Items.Add(new Separator());

			item = new MenuItem();
			item.Header = Properties.Resources.btnActualSizeText;
			//item = Properties.PdfToolStrip.btnActualSize16Image;
			item.Click += btn_ActualSizeClick;
			item.Name = "btnActualSizeEx";
			item.ToolTip = new ToolTip()
			{
				Content = Properties.Resources.btnActualSizeToolTipText
			};
			btn.ContextMenu.Items.Add(item);


			item = new MenuItem();
			item.Header = Properties.Resources.btnFitPageText;
			//item = Properties.PdfToolStrip.btnFitPage16Image;
			item.Click += btn_FitPageClick;
			item.Name = "btnFitPageEx";
			item.ToolTip = new ToolTip()
			{
				Content = Properties.Resources.btnFitPageToolTipText
			};
			btn.ContextMenu.Items.Add(item);


			item = new MenuItem();
			item.Header = Properties.Resources.btnFitWidthText;
			//item = Properties.PdfToolStrip.btnFitWidth16Image;
			item.Click += btn_FitWidthClick;
			item.Name = "btnFitWidthEx";
			item.ToolTip = new ToolTip()
			{
				Content = Properties.Resources.btnFitWidthToolTipText
			};
			btn.ContextMenu.Items.Add(item);


			item = new MenuItem();
			item.Header = Properties.Resources.btnFitHeightText;
			//item = Properties.PdfToolStrip.btnFitHeight16Image;
			item.Click += btn_FitHeightClick;
			item.Name = "btnFitHeightEx";
			item.ToolTip = new ToolTip()
			{
				Content = Properties.Resources.btnFitHeightToolTipText
			};
			btn.ContextMenu.Items.Add(item);

			return btn;
		}

		private Slider CreateTrackBar()
		{
			var btn = new Slider();
			btn.Name = "btnTrackBar";
			btn.Width = _trackBarWidth;
			btn.Height = _trackBarHeight;
			btn.TickPlacement = System.Windows.Controls.Primitives.TickPlacement.None;
			btn.Maximum = ZoomLevel.Length - 1;
			btn.Minimum = 0;
			btn.LargeChange = 1;
			btn.SmallChange = 1;
			btn.ValueChanged += TrackBar_ValueChanged;
			btn.Margin = new Thickness(0, 13, 0, 0);
			return btn;
		}

		#endregion

		#region Overriding
		/// <summary>
		/// Create all buttons and add its into toolbar. Override this method to create custom buttons
		/// </summary>
		protected override void InitializeButtons()
		{
			var btn = CreateZoomDropDown();
			this.Items.Add(btn);

			btn = CreateButton("btnZoomExOut",
				Properties.Resources.btnZoomOutText,
				Properties.Resources.btnZoomOutToolTipText,
				"zoomExOut.png",
				btn_ZoomExOutClick,
				16, 16, ImageTextType.ImageOnly);
			btn.Padding = new Thickness(0);
			this.Items.Add(btn);

			var sl = CreateTrackBar();
			this.Items.Add(sl);

			btn = CreateButton("btnZoomExIn",
				Properties.Resources.btnZoomInText,
				Properties.Resources.btnZoomInToolTipText,
				"zoomExIn.png",
				btn_ZoomExInClick,
				16, 16, ImageTextType.ImageOnly);
			btn.Padding = new Thickness(0);
			this.Items.Add(btn);
		}

		/// <summary>
		/// Called when the ToolBar's items need to change its states
		/// </summary>
		protected override void UpdateButtons()
		{
			var tsi = this.Items[0] as Button;
			if (tsi != null)
				tsi.IsEnabled = (PdfViewer != null) && (PdfViewer.Document != null);

			var sli = this.Items[2] as Slider;
			if (sli != null)
				sli.IsEnabled = (PdfViewer != null) && (PdfViewer.Document != null);

			tsi = this.Items[1] as Button;
			if (tsi != null)
				tsi.IsEnabled = (PdfViewer != null) && (PdfViewer.Document != null);

			tsi = this.Items[3] as Button;
			if (tsi != null)
				tsi.IsEnabled = (PdfViewer != null) && (PdfViewer.Document != null);

			if (PdfViewer == null || PdfViewer.Document == null)
				return;


			var zoom = Zoom;
			tsi = this.Items[0] as Button;
			if (tsi != null)
				((tsi.Content as StackPanel).Children[0] as TextBlock).Text = string.Format("{0:.00}%", zoom);

			CalcCurrentZoomLevel();

			var tstb = this.Items[2] as Slider;
			if (tstb == null)
				return;
			tstb.ValueChanged -= TrackBar_ValueChanged;
			tstb.Value = this.Orientation== Orientation.Vertical ? _currentZoomLevel * -1 : _currentZoomLevel;
			tstb.ValueChanged += TrackBar_ValueChanged;
		}

		/// <summary>
		/// Called when the current PdfViewer control associated with the ToolBar is changing.
		/// </summary>
		/// <param name="oldValue">PdfViewer control of which was associated with the ToolBar.</param>
		/// <param name="newValue">PdfViewer control of which will be associated with the ToolBar.</param>
		protected override void OnPdfViewerChanging(PdfViewer oldValue, PdfViewer newValue)
		{
			base.OnPdfViewerChanging(oldValue, newValue);
			if (oldValue != null)
				UnsubscribePdfViewEvents(oldValue);
			if (newValue != null)
				SubscribePdfViewEvents(newValue);
		}

		/// <summary>
		/// Raises the LayoutCompleted event.
		/// </summary>
		protected virtual void OnToolBarOrientationChanged()
		{
			if (this.Orientation == Orientation.Vertical)
			{
				var tstb = (this.Items[2] as Slider);
				if (tstb != null)
				{
					tstb.Orientation = Orientation.Vertical;
					tstb.Width = _trackBarHeight;
					tstb.Height = _trackBarWidth;

					tstb.Minimum = (ZoomLevel.Length - 1) * -1;
					tstb.Maximum = 0;
				}
			}
			else
			{
				var tstb = (this.Items[2] as Slider);
				if (tstb != null)
				{
					tstb.Orientation = Orientation.Horizontal;
					tstb.Width = _trackBarWidth;
					tstb.Height = _trackBarHeight;

					tstb.Maximum = (ZoomLevel.Length - 1);
					tstb.Minimum = 0;
				}
			}
			UpdateButtons();
		}

		/// <summary>
		/// Invoked whenever the effective value of any dependency property on this System.Windows.FrameworkElement has been updated. The specific dependency property that changed is reported in the arguments parameter. Overrides System.Windows.DependencyObject.OnPropertyChanged(System.Windows.DependencyPropertyChangedEventArgs).
		/// </summary>
		/// <param name="e"> The event data that describes the property that changed, as well as old and new values</param>
		protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
		{
			base.OnPropertyChanged(e);

			if (e.Property.Name == "Orientation")
				OnToolBarOrientationChanged();
		}
		#endregion

		#region Event handlers for PdfViewer
		private void PdfViewer_SomethingChanged(object sender, EventArgs e)
		{
			UpdateButtons();
		}
		#endregion

		#region Event handlers for buttons
		private void ZoomLevel_ButtonClick(object sender, EventArgs e)
		{
			(sender as Button).ContextMenu.IsEnabled = true;
			(sender as Button).ContextMenu.PlacementTarget = (sender as Button);
			(sender as Button).ContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
			(sender as Button).ContextMenu.IsOpen = true;
		}
		private void ZoomLevel_DropDownOpening(object sender, EventArgs e)
		{
			var btn = this.Items[0] as Button;
			if (btn == null)
				return;

			var cm = btn.ContextMenu;

			MenuItem tsmiFitHeight = cm.Items[cm.Items.Count - 1] as MenuItem;
			MenuItem tsmiFitWidth = cm.Items[cm.Items.Count - 2] as MenuItem;
			MenuItem tsmiFitSize = cm.Items[cm.Items.Count - 3] as MenuItem;
			MenuItem tsmiFitActual = cm.Items[cm.Items.Count - 4] as MenuItem;

			if (tsmiFitHeight != null)
				tsmiFitHeight.IsChecked = (PdfViewer.SizeMode == SizeModes.FitToHeight);

			if (tsmiFitWidth != null)
				tsmiFitWidth.IsChecked = (PdfViewer.SizeMode == SizeModes.FitToWidth);

			if (tsmiFitSize != null)
				tsmiFitSize.IsChecked = (PdfViewer.SizeMode == SizeModes.FitToSize);

			if (tsmiFitActual != null)
				tsmiFitActual.IsChecked = ((PdfViewer.SizeMode == SizeModes.Zoom) && (PdfViewer.Zoom >= 1 - 0.00004 && PdfViewer.Zoom <= 1 + 0.00004));
		}

		private void TrackBar_ValueChanged(object sender, EventArgs e)
		{
			OnTrackBarValueChanged(this.Items[2] as Slider);
		}

		private void ZoomLevel_Click(object sender, EventArgs e)
		{
			OnZoomLevelClick(sender as Button, ZoomLevel[(int)(sender as MenuItem).Tag]);
		}

		private void btn_ZoomExOutClick(object sender, System.EventArgs e)
		{
			OnZoomExOutClick(sender as Button);
		}
		private void btn_ZoomExInClick(object sender, System.EventArgs e)
		{
			OnZoomExInClick(sender as Button);
		}
		private void btn_ActualSizeClick(object sender, System.EventArgs e)
		{
			OnActualSizeClick(sender as MenuItem);
		}
		private void btn_FitPageClick(object sender, System.EventArgs e)
		{
			OnFitPageClick(sender as MenuItem);
		}
		private void btn_FitWidthClick(object sender, System.EventArgs e)
		{
			OnFitWidthClick(sender as MenuItem);
		}
		private void btn_FitHeightClick(object sender, System.EventArgs e)
		{
			OnFitHeightClick(sender as MenuItem);
		}
		#endregion

		#region Protected methods
		/// <summary>
		/// Sets specified zoom level for Pdf document
		/// </summary>
		/// <param name="zoomIndex">Index of the zoom in ZoomLevel</param>
		protected void SetZoom(int zoomIndex)
		{
			SetZoom(ZoomLevel[zoomIndex] / 100);
		}

		/// <summary>
		/// Sets specified zoom for Pdf document
		/// </summary>
		/// <param name="zoom">zoom value</param>
		protected virtual void SetZoom(double zoom)
		{
			UnsubscribePdfViewEvents(PdfViewer);
			PdfViewer.SizeMode = SizeModes.Zoom;
			PdfViewer.Zoom = (float)zoom;
			SubscribePdfViewEvents(PdfViewer);
			CalcCurrentZoomLevel();
		}

		/// <summary>
		/// Calculate zoom level for current <see cref="Zoom"/> and store it in internal field
		/// </summary>
		protected void CalcCurrentZoomLevel()
		{
			var zoom = Zoom;
			double min = double.MaxValue;
			_currentZoomLevel = 0;
			for (int i = 0; i < ZoomLevel.Length; i++)
			{
				double m = ZoomLevel[i] - zoom;
				if (m < 0) m = -m;
				if (min > m)
				{
					min = m;
					_currentZoomLevel = i;
				}
			}
		}

		/// <summary>
		/// Occurs when the Value property of a track bar changes, either by movement of the scroll box or by manipulation in code.
		/// </summary>
		/// <param name="item">The item that has been clicked</param>
		protected virtual void OnTrackBarValueChanged(Slider item)
		{
			int val = (int)item.Value;
			SetZoom( this.Orientation == Orientation.Vertical ? val * -1 : val);
			UpdateButtons();
		}

		/// <summary>
		/// Occurs when the any item with zoom level clicked in ZoomDropDown button
		/// </summary>
		/// <param name="item">The item that has been clicked</param>
		/// <param name="zoom">The zoom value of item that was clicked</param>
		protected virtual void OnZoomLevelClick(Button item, double zoom)
		{
			SetZoom(zoom / 100);
			UpdateButtons();
		}

		/// <summary>
		/// Occurs when the Zoom In button is clicked
		/// </summary>
		/// <param name="item">The item that has been clicked</param>
		protected virtual void OnZoomExInClick(Button item)
		{
			if (_currentZoomLevel < ZoomLevel.Length - 1)
			{
				_currentZoomLevel++;
				SetZoom(_currentZoomLevel);
				UpdateButtons();
			}
		}

		/// <summary>
		/// Occurs when the Zoom Out button is clicked
		/// </summary>
		/// <param name="item">The item that has been clicked</param>
		protected virtual void OnZoomExOutClick(Button item)
		{
			if (_currentZoomLevel > 0)
			{
				_currentZoomLevel--;
				SetZoom(_currentZoomLevel);
				UpdateButtons();
			}

		}

		/// <summary>
		/// Occurs when the Actual Size item is clicked
		/// </summary>
		/// <param name="item">The item that has been clicked</param>
		protected virtual void OnActualSizeClick(MenuItem item)
		{
			SetZoom(1.0f);
			UpdateButtons();
		}

		/// <summary>
		/// Occurs when the Fit To Page item is clicked
		/// </summary>
		/// <param name="item">The item that has been clicked</param>
		protected virtual void OnFitPageClick(MenuItem item)
		{
			PdfViewer.SizeMode = SizeModes.FitToSize;
		}

		/// <summary>
		/// Occurs when the Fit To Width item is clicked
		/// </summary>
		/// <param name="item">The item that has been clicked</param>
		protected virtual void OnFitWidthClick(MenuItem item)
		{
			PdfViewer.SizeMode = SizeModes.FitToWidth;
		}

		/// <summary>
		/// Occurs when the Fit To Height item is clicked
		/// </summary>
		/// <param name="item">The item that has been clicked</param>
		protected virtual void OnFitHeightClick(MenuItem item)
		{
			PdfViewer.SizeMode = SizeModes.FitToHeight;
		}
		#endregion

		#region Private methods
		private void UnsubscribePdfViewEvents(PdfViewer oldValue)
		{
			oldValue.DocumentLoaded -= PdfViewer_SomethingChanged;
			oldValue.DocumentClosed -= PdfViewer_SomethingChanged;
			oldValue.ViewModeChanged -= PdfViewer_SomethingChanged;
			oldValue.SizeModeChanged -= PdfViewer_SomethingChanged;
			oldValue.ZoomChanged -= PdfViewer_SomethingChanged;
			oldValue.CurrentPageChanged -= PdfViewer_SomethingChanged;
		}

		private void SubscribePdfViewEvents(PdfViewer newValue)
		{
			newValue.DocumentLoaded += PdfViewer_SomethingChanged;
			newValue.DocumentClosed += PdfViewer_SomethingChanged;
			newValue.ViewModeChanged += PdfViewer_SomethingChanged;
			newValue.SizeModeChanged += PdfViewer_SomethingChanged;
			newValue.ZoomChanged += PdfViewer_SomethingChanged;
			newValue.CurrentPageChanged += PdfViewer_SomethingChanged;
		}

		#endregion

	}
}
