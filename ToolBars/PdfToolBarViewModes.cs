using System;
using System.Windows.Controls.Primitives;

namespace Patagames.Pdf.Net.Controls.Wpf.ToolBars
{
	/// <summary>
	/// Provides a container for Windows toolbar objects with predefined functionality for changing view modes
	/// </summary>
	public class PdfToolBarViewModes : PdfToolBar
	{
		#region Overriding
		/// <summary>
		/// Create all buttons and add its into toolbar. Override this method to create custom buttons
		/// </summary>
		protected override void InitializeButtons()
		{
			var btn = CreateToggleButton("btnModeSingle",
				Properties.Resources.btnModeSingleText,
				Properties.Resources.btnModeSingleToolTipText,
				"modeSingle.png",
				btn_ModeSingleClick,
				16,16,
				ImageTextType.ImageOnly);
			this.Items.Add(btn);

			btn = CreateToggleButton("btnModeVertical",
				Properties.Resources.btnModeVerticalText,
				Properties.Resources.btnModeVerticalToolTipText,
				"modeVertical.png",
				btn_ModeVerticalClick,
				16,16,
				ImageTextType.ImageOnly);
			this.Items.Add(btn);

			btn = CreateToggleButton("btnModeHorizontal",
				Properties.Resources.btnModeHorizontalText,
				Properties.Resources.btnModeHorizontalToolTipText,
				"modeHorizontal.png",
				btn_ModeHorizontalClick,
				16,16,
				ImageTextType.ImageOnly);
			this.Items.Add(btn);

			btn = CreateToggleButton("btnModeTiles",
				Properties.Resources.btnModeTilesText,
				Properties.Resources.btnModeTilesToolTipText,
				"modeTiles.png",
				btn_ModeTilesClick,
				16,16,
				ImageTextType.ImageOnly);
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
				tsb.IsChecked = (PdfViewer.ViewMode == ViewModes.SinglePage);

			tsb = this.Items[1] as ToggleButton;
			if (tsb != null)
				tsb.IsChecked = (PdfViewer.ViewMode == ViewModes.Vertical);

			tsb = this.Items[2] as ToggleButton;
			if (tsb != null)
				tsb.IsChecked = (PdfViewer.ViewMode == ViewModes.Horizontal);

			tsb = this.Items[3] as ToggleButton;
			if (tsb != null)
				tsb.IsChecked = (PdfViewer.ViewMode == ViewModes.TilesVertical);

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
		private void btn_ModeSingleClick(object sender, System.EventArgs e)
		{
			OnModeSingleClick(this.Items[0] as ToggleButton);
		}
		private void btn_ModeVerticalClick(object sender, System.EventArgs e)
		{
			OnModeVerticalClick(this.Items[1] as ToggleButton);
		}
		private void btn_ModeHorizontalClick(object sender, System.EventArgs e)
		{
			OnModeHorizontalClick(this.Items[2] as ToggleButton);
		}
		private void btn_ModeTilesClick(object sender, System.EventArgs e)
		{
			OnModeTilesClick(this.Items[3] as ToggleButton);
		}
		#endregion

		#region Protected methods
		/// <summary>
		/// Occurs when the Single Page button is clicked
		/// </summary>
		/// <param name="item">The item that has been clicked</param>
		protected virtual void OnModeSingleClick(ToggleButton item)
		{
			PdfViewer.ViewMode = ViewModes.SinglePage;
		}

		/// <summary>
		/// Occurs when the Continuous Vertical button is clicked
		/// </summary>
		/// <param name="item">The item that has been clicked</param>
		protected virtual void OnModeVerticalClick(ToggleButton item)
		{
			PdfViewer.ViewMode = ViewModes.Vertical;
		}

		/// <summary>
		/// Occurs when the Continuous Horizontal button is clicked
		/// </summary>
		/// <param name="item">The item that has been clicked</param>
		protected virtual void OnModeHorizontalClick(ToggleButton item)
		{
			PdfViewer.ViewMode = ViewModes.Horizontal;
		}

		/// <summary>
		/// Occurs when the Continuous Facing button is clicked
		/// </summary>
		/// <param name="item">The item that has been clicked</param>
		protected virtual void OnModeTilesClick(ToggleButton item)
		{
			PdfViewer.ViewMode = ViewModes.TilesVertical;
		}

		#endregion

		#region Private methods
		private void UnsubscribePdfViewEvents(PdfViewer oldValue)
		{
			oldValue.AfterDocumentChanged -= PdfViewer_SomethingChanged;
			oldValue.DocumentLoaded -= PdfViewer_SomethingChanged;
			oldValue.DocumentClosed -= PdfViewer_SomethingChanged;
			oldValue.ViewModeChanged -= PdfViewer_SomethingChanged;
		}

		private void SubscribePdfViewEvents(PdfViewer newValue)
		{
			newValue.AfterDocumentChanged += PdfViewer_SomethingChanged;
			newValue.DocumentLoaded += PdfViewer_SomethingChanged;
			newValue.DocumentClosed += PdfViewer_SomethingChanged;
			newValue.ViewModeChanged += PdfViewer_SomethingChanged;
		}

		#endregion
	}
}
