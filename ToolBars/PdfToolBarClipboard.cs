using System;
using System.Windows;
using System.Windows.Controls;

namespace Patagames.Pdf.Net.Controls.Wpf.ToolBars
{
	/// <summary>
	/// Provides a container for Windows toolbar objects with predefined functionality for working with clipboard
	/// </summary>
	public class PdfToolBarClipboard : PdfToolBar
	{
		#region Overriding
		/// <summary>
		/// Create all buttons and add its into toolbar. Override this method to create custom buttons
		/// </summary>
		protected override void InitializeButtons()
		{
			var btn = CreateButton("btnSelectAll",
				Properties.Resources.btnSelectAllText,
				Properties.Resources.btnSelectAllToolTipText,
                CreateUriToResource("selectAll.png"),
				btn_SelectAllClick);
			this.Items.Add(btn);

			btn = CreateButton("btnCopy",
				Properties.Resources.btnCopyText,
				Properties.Resources.btnCopyToolTipText,
                CreateUriToResource("textCopy.png"),
				btn_CopyClick);
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

			tsi = this.Items[1] as Button;
			if (tsi != null)
				tsi.IsEnabled = (PdfViewer != null) && (PdfViewer.Document != null);

			if (PdfViewer == null || PdfViewer.Document == null)
				return;

			var tsb = this.Items[1] as Button;
			if (tsb != null)
				tsb.IsEnabled = PdfViewer.SelectedText.Length > 0;


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
		private void btn_SelectAllClick(object sender, System.EventArgs e)
		{
			OnSelectAllClick(this.Items[0] as Button);
		}
		private void btn_CopyClick(object sender, System.EventArgs e)
		{
			OnCopyClick(this.Items[1] as Button);
		}
		#endregion

		#region Protected methods
		/// <summary>
		/// Occurs when the Select All button is clicked
		/// </summary>
		/// <param name="item">The item that has been clicked</param>
		protected virtual void OnSelectAllClick(Button item)
		{
			PdfViewer.SelectText(0, 0, PdfViewer.Document.Pages.Count - 1, PdfViewer.Document.Pages[PdfViewer.Document.Pages.Count - 1].Text.CountChars);
		}

		/// <summary>
		/// Occurs when the Copy button is clicked
		/// </summary>
		/// <param name="item">The item that has been clicked</param>
		protected virtual void OnCopyClick(Button item)
		{
			Clipboard.SetText(PdfViewer.SelectedText);
		}

		#endregion

		#region Private methods
		private void UnsubscribePdfViewEvents(PdfViewer oldValue)
		{
			oldValue.AfterDocumentChanged -= PdfViewer_SomethingChanged;
			oldValue.DocumentLoaded -= PdfViewer_SomethingChanged;
			oldValue.DocumentClosed -= PdfViewer_SomethingChanged;
			oldValue.SelectionChanged -= PdfViewer_SomethingChanged;
		}

		private void SubscribePdfViewEvents(PdfViewer newValue)
		{
			newValue.AfterDocumentChanged += PdfViewer_SomethingChanged;
			newValue.DocumentLoaded += PdfViewer_SomethingChanged;
			newValue.DocumentClosed += PdfViewer_SomethingChanged;
			newValue.SelectionChanged += PdfViewer_SomethingChanged;
		}

		#endregion

	}
}
