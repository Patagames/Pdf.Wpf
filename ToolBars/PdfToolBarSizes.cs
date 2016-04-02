using System;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace Patagames.Pdf.Net.Controls.Wpf.ToolBars
{
	/// <summary>
	/// Provides a container for Windows toolbar objects with predefined functionality for changing pages size mode
	/// </summary>
	public class PdfToolBarSizes : PdfToolBar
	{
		#region Overriding
		/// <summary>
		/// Create all buttons and add its into toolbar. Override this method to create custom buttons
		/// </summary>
		protected override void InitializeButtons()
		{
			var btn = CreateToggleButton("btnActualSize",
				Properties.Resources.btnActualSizeText,
				Properties.Resources.btnActualSizeToolTipText,
				"viewActualSize.PNG",
				btn_ActualSizeClick);
			this.Items.Add(btn);

			btn = CreateToggleButton("btnFitPage",
				Properties.Resources.btnFitPageText,
				Properties.Resources.btnFitPageToolTipText,
				"viewFitPage.PNG",
				btn_FitPageClick);
			this.Items.Add(btn);

			btn = CreateToggleButton("btnFitWidth",
				Properties.Resources.btnFitWidthText,
				Properties.Resources.btnFitWidthToolTipText,
				"viewFitWidth.PNG",
				btn_FitWidthClick);
			this.Items.Add(btn);

			btn = CreateToggleButton("btnFitHeight",
				Properties.Resources.btnFitHeightText,
				Properties.Resources.btnFitHeightToolTipText,
				"viewFitHeight.PNG",
				btn_FitHeightClick);
			this.Items.Add(btn);
		}

		/// <summary>
		/// Called when the ToolBar's items need to change its states
		/// </summary>
		protected override void UpdateButtons()
		{
			var tsi = this.Items[0] as ToggleButton;
			if (tsi != null)
				tsi.IsEnabled = (PdfViewer != null) && (PdfViewer.Document != null);

			tsi = this.Items[1] as ToggleButton;
			if (tsi != null)
				tsi.IsEnabled = (PdfViewer != null) && (PdfViewer.Document != null);

			tsi = this.Items[2] as ToggleButton;
			if (tsi != null)
				tsi.IsEnabled = (PdfViewer != null) && (PdfViewer.Document != null);

			tsi = this.Items[3] as ToggleButton;
			if (tsi != null)
				tsi.IsEnabled = (PdfViewer != null) && (PdfViewer.Document != null);

			if (PdfViewer == null || PdfViewer.Document == null)
				return;

			var tsb = this.Items[0] as ToggleButton;
			if (tsb != null)
				tsb.IsChecked = ((PdfViewer.SizeMode == SizeModes.Zoom) && (PdfViewer.Zoom >= 1 - 0.00004 && PdfViewer.Zoom <= 1 + 0.00004));

			tsb = this.Items[1] as ToggleButton;
			if (tsb != null)
				tsb.IsChecked = (PdfViewer.SizeMode == SizeModes.FitToSize);

			tsb = this.Items[2] as ToggleButton;
			if (tsb != null)
				tsb.IsChecked = (PdfViewer.SizeMode == SizeModes.FitToWidth);

			tsb = this.Items[3] as ToggleButton;
			if (tsb != null)
				tsb.IsChecked = (PdfViewer.SizeMode == SizeModes.FitToHeight);

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

		#endregion

		#region Event handlers for PdfViewer
		private void PdfViewer_SomethingChanged(object sender, EventArgs e)
		{
			UpdateButtons();
		}
		#endregion

		#region Event handlers for buttons
		private void btn_ActualSizeClick(object sender, System.EventArgs e)
		{
			OnActualSizeClick(this.Items[0] as ToggleButton);
		}
		private void btn_FitPageClick(object sender, System.EventArgs e)
		{
			OnFitPageClick(this.Items[1] as ToggleButton);
		}
		private void btn_FitWidthClick(object sender, System.EventArgs e)
		{
			OnFitWidthClick(this.Items[2] as ToggleButton);
		}
		private void btn_FitHeightClick(object sender, System.EventArgs e)
		{
			OnFitHeightClick(this.Items[3] as ToggleButton);
		}
		#endregion

		#region Protected methods
		/// <summary>
		/// Occurs when the Actual Size button is clicked
		/// </summary>
		/// <param name="item">The item that has been clicked</param>
		protected virtual void OnActualSizeClick(ToggleButton item)
		{
			UnsubscribePdfViewEvents(PdfViewer);
			PdfViewer.SizeMode = SizeModes.Zoom;
			PdfViewer.Zoom = 1;
			SubscribePdfViewEvents(PdfViewer);
			UpdateButtons();
		}

		/// <summary>
		/// Occurs when the Fit Page button is clicked
		/// </summary>
		/// <param name="item">The item that has been clicked</param>
		protected virtual void OnFitPageClick(ToggleButton item)
		{
			PdfViewer.SizeMode = SizeModes.FitToSize;
		}

		/// <summary>
		/// Occurs when the Fit Width button is clicked
		/// </summary>
		/// <param name="item">The item that has been clicked</param>
		protected virtual void OnFitWidthClick(ToggleButton item)
		{
			PdfViewer.SizeMode = SizeModes.FitToWidth;
		}

		/// <summary>
		/// Occurs when the Fit Height button is clicked
		/// </summary>
		/// <param name="item">The item that has been clicked</param>
		protected virtual void OnFitHeightClick(ToggleButton item)
		{
			PdfViewer.SizeMode = SizeModes.FitToHeight;
		}

		#endregion

		#region Private methods
		private void UnsubscribePdfViewEvents(PdfViewer oldValue)
		{
			oldValue.DocumentLoaded -= PdfViewer_SomethingChanged;
			oldValue.DocumentClosed -= PdfViewer_SomethingChanged;
			oldValue.SizeModeChanged -= PdfViewer_SomethingChanged;
			oldValue.ZoomChanged -= PdfViewer_SomethingChanged;
		}

		private void SubscribePdfViewEvents(PdfViewer newValue)
		{
			newValue.DocumentLoaded += PdfViewer_SomethingChanged;
			newValue.DocumentClosed += PdfViewer_SomethingChanged;
			newValue.SizeModeChanged += PdfViewer_SomethingChanged;
			newValue.ZoomChanged += PdfViewer_SomethingChanged;
		}

		#endregion
	}

}
