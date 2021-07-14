using System;
using System.Windows.Controls;

namespace Patagames.Pdf.Net.Controls.Wpf.ToolBars
{
	/// <summary>
	/// Provides a container for Windows toolbar objects with predefined functionality for working with pages
	/// </summary>
	public class PdfToolBarPages : PdfToolBar
	{
		#region Constructor, Destructor, Initialisation
		private TextBox CreateTextBox()
		{
			var tb = new TextBox();
			tb.Name = "btnPageNumber";
			tb.Width = 70;
			tb.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
			tb.KeyDown += btnPageNumber_KeyDown;
			return tb;
		}
		#endregion

		#region Overriding
		/// <summary>
		/// Create all buttons and add its into toolbar. Override this method to create custom buttons
		/// </summary>
		protected override void InitializeButtons()
		{
			var btn = CreateButton("btnFirstPage",
				Properties.Resources.btnFirstPageText,
				Properties.Resources.btnFirstPageToolTipText,
                CreateUriToResource("toBegin.png"),
				btn_FirstPageClick,
				16,16, ImageTextType.ImageOnly);
			this.Items.Add(btn);

			btn = CreateButton("btnPreviousPage",
				Properties.Resources.btnPreviousPageText,
				Properties.Resources.btnPreviousPageToolTipText,
                CreateUriToResource("toLeft.png"),
				btn_PreviousPageClick,
				16, 16, ImageTextType.ImageOnly);
			this.Items.Add(btn);

			TextBox tb = CreateTextBox();
			this.Items.Add(tb);

			btn = CreateButton("btnNextPage",
				Properties.Resources.btnNextPageText,
				Properties.Resources.btnNextPageToolTipText,
                CreateUriToResource("toRight.png"),
				btn_NextPageClick,
				16, 16, ImageTextType.ImageOnly);
			this.Items.Add(btn);

			btn = CreateButton("btnLastPage",
				Properties.Resources.btnLastPageText,
				Properties.Resources.btnLastPageToolTipText,
                CreateUriToResource("toEnd.png"),
				btn_LastPageClick,
				16, 16, ImageTextType.ImageOnly);
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

			tsi = this.Items[3] as Button;
			if (tsi != null)
				tsi.IsEnabled = (PdfViewer != null) && (PdfViewer.Document != null);

			tsi = this.Items[4] as Button;
			if (tsi != null)
				tsi.IsEnabled = (PdfViewer != null) && (PdfViewer.Document != null);

			var tbi = this.Items[2] as TextBox;
			if (tbi == null)
				return;
			tbi.IsEnabled = (PdfViewer != null) && (PdfViewer.Document != null);


			if (PdfViewer == null || PdfViewer.Document == null)
				tbi.Text = "";
			else
				tbi.Text = string.Format("{0} / {1}", PdfViewer.Document.Pages.CurrentIndex + 1, PdfViewer.Document.Pages.Count);
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
		private void btnPageNumber_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
		{
			OnPageNumberKeyDown(this.Items[2] as TextBox, e);
		}

		private void btn_FirstPageClick(object sender, EventArgs e)
		{
			OnToBeginClick(this.Items[0] as Button);
		}

		private void btn_PreviousPageClick(object sender, System.EventArgs e)
		{
			OnToLeftClick(this.Items[1] as Button);
		}
		private void btn_NextPageClick(object sender, System.EventArgs e)
		{
			OnToRightClick(this.Items[3] as Button);
		}
		private void btn_LastPageClick(object sender, System.EventArgs e)
		{
			OnToEndClick(this.Items[4] as Button);
		}
		#endregion

		#region Protected methods
		/// <summary>
		/// Occurs when a key is pressed and held down while the PageNumber textbox has focus.
		/// </summary>
		/// <param name="item">PageNumber item</param>
		/// <param name="e">Key event args</param>
		protected virtual void OnPageNumberKeyDown(TextBox item, System.Windows.Input.KeyEventArgs e)
		{
			if (item == null)
				return;
			if (e.Key == System.Windows.Input.Key.Enter)
			{
				int pn = 0;
				string text = item.Text;
				char[] chs = { ' ', '/', '\\' };
				int i = text.LastIndexOfAny(chs);
				if (i > 0)
					text = text.Substring(0, i - 1);

				if (!int.TryParse(text, out pn))
					return;
				if (pn < 1)
					pn = 1;
				else if (pn > PdfViewer.Document.Pages.Count)
					pn = PdfViewer.Document.Pages.Count;

				PdfViewer.ScrollToPage(pn - 1);
				PdfViewer.CurrentIndex = pn - 1;
				item.Text = string.Format("{0} / {1}", pn, PdfViewer.Document.Pages.Count);
			}
		}

		/// <summary>
		/// Occurs when the First Page button is clicked
		/// </summary>
		/// <param name="item">The item that has been clicked</param>
		protected virtual void OnToBeginClick(Button item)
		{
			PdfViewer.ScrollToPage(0);
			PdfViewer.CurrentIndex = 0;
		}


		/// <summary>
		/// Occurs when the Previous Page button is clicked
		/// </summary>
		/// <param name="item">The item that has been clicked</param>
		protected virtual void OnToLeftClick(Button item)
		{
			int ci = PdfViewer.CurrentIndex;
			if (ci > 0)
				ci--;
			PdfViewer.ScrollToPage(ci);
			PdfViewer.CurrentIndex = ci;
		}

		/// <summary>
		/// Occurs when the Next Page button is clicked
		/// </summary>
		/// <param name="item">The item that has been clicked</param>
		protected virtual void OnToRightClick(Button item)
		{
			int ci = PdfViewer.CurrentIndex;
			if (ci < PdfViewer.Document.Pages.Count - 1)
				ci++;
			PdfViewer.ScrollToPage(ci);
			PdfViewer.CurrentIndex = ci;
		}

		/// <summary>
		/// Occurs when the Last Page button is clicked
		/// </summary>
		/// <param name="item">The item that has been clicked</param>
		protected virtual void OnToEndClick(Button item)
		{
			int ci = PdfViewer.Document.Pages.Count - 1;
			PdfViewer.ScrollToPage(ci);
			PdfViewer.CurrentIndex = ci;
		}

		#endregion

		#region Private methods
		private void UnsubscribePdfViewEvents(PdfViewer oldValue)
		{
			if (oldValue.Document != null)
			{
				oldValue.Document.Pages.PageInserted -= PdfViewer_SomethingChanged;
				oldValue.Document.Pages.PageDeleted -= PdfViewer_SomethingChanged;
			}
			oldValue.BeforeDocumentChanged -= Subscribe_BeforeDocumentChanged;
			oldValue.AfterDocumentChanged -= Subscribe_AfterDocumentChanged;
			oldValue.AfterDocumentChanged -= PdfViewer_SomethingChanged;
			oldValue.DocumentLoaded -= PdfViewer_SomethingChanged;
			oldValue.DocumentClosed -= PdfViewer_SomethingChanged;
			oldValue.CurrentPageChanged -= PdfViewer_SomethingChanged;
		}

		private void SubscribePdfViewEvents(PdfViewer newValue)
		{
			if (newValue.Document != null)
			{
				newValue.Document.Pages.PageInserted -= PdfViewer_SomethingChanged;
				newValue.Document.Pages.PageDeleted -= PdfViewer_SomethingChanged;
			}
			newValue.BeforeDocumentChanged += Subscribe_BeforeDocumentChanged;
			newValue.AfterDocumentChanged += Subscribe_AfterDocumentChanged;
			newValue.AfterDocumentChanged += PdfViewer_SomethingChanged;
			newValue.DocumentLoaded += PdfViewer_SomethingChanged;
			newValue.DocumentClosed += PdfViewer_SomethingChanged;
			newValue.CurrentPageChanged += PdfViewer_SomethingChanged;
		}
		private void Subscribe_AfterDocumentChanged(object sender, EventArgs e)
		{
			if (PdfViewer.Document != null)
			{
				PdfViewer.Document.Pages.PageInserted += PdfViewer_SomethingChanged;
				PdfViewer.Document.Pages.PageDeleted += PdfViewer_SomethingChanged;
			}
		}

		private void Subscribe_BeforeDocumentChanged(object sender, EventArguments.DocumentClosingEventArgs e)
		{
			if (PdfViewer.Document != null)
			{
				PdfViewer.Document.Pages.PageInserted -= PdfViewer_SomethingChanged;
				PdfViewer.Document.Pages.PageDeleted -= PdfViewer_SomethingChanged;
			}
		}
		#endregion
	}
}
