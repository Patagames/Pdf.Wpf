using Patagames.Pdf.Enums;
using System;
using System.Windows.Controls;

namespace Patagames.Pdf.Net.Controls.Wpf.ToolBars
{
	/// <summary>
	/// Provides a container for Windows toolbar objects with predefined functionality for pages rotation
	/// </summary>
	public class PdfToolBarRotate : PdfToolBar
	{
		#region Overriding
		/// <summary>
		/// Create all buttons and add its into toolbar. Override this method to create custom buttons
		/// </summary>
		protected override void InitializeButtons()
		{
			var btn = CreateButton("btnRotateLeft",
				Properties.Resources.btnRotateLeftText,
				Properties.Resources.btnRotateLeftToolTipText,
                CreateUriToResource("rotateLeft.png"),
				btn_RotateLeftClick);
			this.Items.Add(btn);

			btn = CreateButton("btnRotateRight",
				Properties.Resources.btnRotateRightText,
				Properties.Resources.btnRotateRightToolTipText,
                CreateUriToResource("rotateRight.png"),
				btn_RotateRightClick);
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
		private void btn_RotateLeftClick(object sender, System.EventArgs e)
		{
			OnRotateLeftClick(this.Items[0] as Button);
		}
		private void btn_RotateRightClick(object sender, System.EventArgs e)
		{
			OnRotateRightClick(this.Items[1] as Button);
		}
		#endregion

		#region Protected methods
		/// <summary>
		/// Occurs when the Rotate Left button is clicked
		/// </summary>
		/// <param name="item">The item that has been clicked</param>
		protected virtual void OnRotateLeftClick(Button item)
		{
			var ang = PdfViewer.Document.Pages.CurrentPage.Rotation;
			if (ang > PageRotate.Normal)
				ang--;
			else
				ang = PageRotate.Rotate270;
			PdfViewer.RotatePage(PdfViewer.CurrentIndex, ang);
		}

		/// <summary>
		/// Occurs when the Rotate Right button is clicked
		/// </summary>
		/// <param name="item">The item that has been clicked</param>
		protected virtual void OnRotateRightClick(Button item)
		{
			var ang = PdfViewer.Document.Pages.CurrentPage.Rotation;
			if (ang < PageRotate.Rotate270)
				ang++;
			else
				ang = PageRotate.Normal;
			PdfViewer.RotatePage(PdfViewer.CurrentIndex, ang);
		}

		#endregion

		#region Private methods
		private void UnsubscribePdfViewEvents(PdfViewer oldValue)
		{
			oldValue.AfterDocumentChanged -= PdfViewer_SomethingChanged;
			oldValue.DocumentLoaded -= PdfViewer_SomethingChanged;
			oldValue.DocumentClosed -= PdfViewer_SomethingChanged;
		}

		private void SubscribePdfViewEvents(PdfViewer newValue)
		{
			newValue.AfterDocumentChanged += PdfViewer_SomethingChanged;
			newValue.DocumentLoaded += PdfViewer_SomethingChanged;
			newValue.DocumentClosed += PdfViewer_SomethingChanged;
		}

		#endregion
	}
}
