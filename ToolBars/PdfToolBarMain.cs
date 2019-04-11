using Microsoft.Win32;
using Patagames.Pdf.Net.EventArguments;
using Patagames.Pdf.Net.Exceptions;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace Patagames.Pdf.Net.Controls.Wpf.ToolBars
{
	/// <summary>
	/// Provides a container for Windows toolbar objects with predefined functionality for opening and printing
	/// </summary>
	public class PdfToolBarMain : PdfToolBar
	{
		#region Private fields
		delegate void ShowPrintDialogDelegate(System.Windows.Forms.PrintDialog dlg);
		#endregion

		#region Public events
		/// <summary>
		/// Occurs when the loaded document protected by password. Application should return the password through Value property
		/// </summary>1
		public event EventHandler<EventArgs<string>> PasswordRequired = null;

		/// <summary>
		/// Occurs after an instance of PdfPrintDocument class is created and before printing is started.
		/// </summary>
		/// <remarks>
		/// You can use this event to get access to PdfPrintDialog which is used in printing routine.
		/// For example, the printing routine shows the standard dialog with printing progress. 
		/// If you want to suppress it you can write in event handler the following:
		/// <code>
		/// private void ToolbarMain1_PdfPrintDocumentCreated(object sender, EventArgs&lt;PdfPrintDocument&gt; e)
		/// {
		///		e.Value.PrintController = new StandardPrintController();
		/// }
		/// </code>
		/// <note type="note">
		/// Because the PdfPrintDocumentis derived from standard System.Drawing.Printing.PrintDocument class you need to add the reference to System.Drawin assembly into your project.
		/// </note>
		/// </remarks>
		public event EventHandler<EventArgs<PdfPrintDocument>> PdfPrintDocumentCreated = null;
		#endregion

		#region Public properties
		/// <summary>
		/// DependencyProperty as the backing store for PdfViewer
		/// </summary>
		public static readonly DependencyProperty WindowProperty =
			DependencyProperty.Register("Window", typeof(Window), typeof(PdfToolBarMain), new PropertyMetadata(null));

		/// <summary>
		/// Sets the window there the Toolbar is hosted. If not specified then the PrintDialog will not be shown during printing.
		/// </summary>
		public Window Window
		{
			get { return (Window)GetValue(WindowProperty); }
			set { SetValue(WindowProperty, value); }
		}

		#endregion

		#region Overriding
		/// <summary>
		/// Create all buttons and add its into toolbar. Override this method to create custom buttons
		/// </summary>
		protected override void InitializeButtons()
		{
			var btn = CreateButton("btnOpenDoc",
				Properties.Resources.btnOpenText,
				Properties.Resources.btnOpenToolTipText,
                CreateUriToResource("docOpen.png"),
				btn_OpenDocClick);
			this.Items.Add(btn);

			btn = CreateButton("btnPrintDoc",
				Properties.Resources.btnPrintText,
				Properties.Resources.btnPrintToolTipText,
                CreateUriToResource("docPrint.png"),
				btn_PrintDocClick);
			this.Items.Add(btn);
		}

		/// <summary>
		/// Called when the ToolBar's items need to change its states
		/// </summary>
		protected override void UpdateButtons()
		{
			var tsi = this.Items[0] as Button;
			if (tsi != null)
				tsi.IsEnabled = (PdfViewer != null);

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

			if (oldValue != null && oldValue.Document != null)
				PdfViewer_DocumentClosed(this, EventArgs.Empty);
			if (newValue != null && newValue.Document != null)
				PdfViewer_DocumentLoaded(this, EventArgs.Empty);
		}
		#endregion

		#region Protected methods
		/// <summary>
		/// Occurs when the Open button is clicked
		/// </summary>
		/// <param name="item">The item that has been clicked</param>
		protected virtual void OnOpenClick(Button item)
		{
			var dlg = new OpenFileDialog();
			dlg.Multiselect = false;
			dlg.Filter = Properties.Resources.OpenDialogFilter;
			if (dlg.ShowDialog() == true)
			{
				try
				{
					PdfViewer.LoadDocument(dlg.FileName);
				}
				catch (InvalidPasswordException)
				{
					string password = OnPasswordRequired();
					try
					{
						PdfViewer.LoadDocument(dlg.FileName, password);
					}
					catch (Exception ex)
					{
						MessageBox.Show(ex.Message, Properties.Resources.ErrorHeader, MessageBoxButton.OK, MessageBoxImage.Error);
					}
				}
			}
		}

        /// <summary>
        /// Occurs when the Loaded document protected by password. Application should return the password
        /// </summary>
        /// <returns>Password to the document must be returned.</returns>
        protected virtual string OnPasswordRequired()
		{
			var args = new EventArgs<string>(null);
			if (PasswordRequired != null)
				PasswordRequired(this, args);
			return args.Value;
		}

		/// <summary>
		/// Occurs when the Print button is clicked
		/// </summary>
		/// <param name="item">The item that has been clicked</param>
		protected virtual void OnPrintClick(Button item)
		{
			if (PdfViewer.Document.FormFill != null)
				PdfViewer.Document.FormFill.ForceToKillFocus();

			//Show standard print dialog
			var printDoc = new PdfPrintDocument(PdfViewer.Document);
			var dlg = new System.Windows.Forms.PrintDialog();
			dlg.AllowCurrentPage = true;
			dlg.AllowSomePages = true;
			dlg.UseEXDialog = true;
			dlg.Document = printDoc;
			OnPdfPrinDocumentCreaded(new EventArgs<PdfPrintDocument>(printDoc));
			ShowPrintDialogDelegate showprintdialog = ShowPrintDialog;
			Dispatcher.BeginInvoke(showprintdialog, dlg);
        }

		/// <summary>
		/// Occurs after an instance of PdfPrintDocument class is created and before printing is started.
		/// </summary>
        /// <param name="e">Event arguments</param>
        protected virtual void OnPdfPrinDocumentCreaded(EventArgs<PdfPrintDocument> e)
		{
			if (PdfPrintDocumentCreated != null)
				PdfPrintDocumentCreated(this, e);
		}
		#endregion

		#region Event handlers for PdfViewer.document and PdfPrintDocument
		private void PdfViewer_DocumentClosed(object sender, EventArgs e)
		{
			UpdateButtons();
		}

		private void PdfViewer_DocumentLoaded(object sender, EventArgs e)
		{
			UpdateButtons();
		}
		#endregion

		#region Event handlers for buttons
		private void btn_OpenDocClick(object sender, EventArgs e)
		{
			OnOpenClick(this.Items[0] as Button);
		}
		private void btn_PrintDocClick(object sender, EventArgs e)
		{
			OnPrintClick(this.Items[1] as Button);
		}
		#endregion

		#region Private methods
		private void UnsubscribePdfViewEvents(PdfViewer oldValue)
		{
			oldValue.AfterDocumentChanged -= PdfViewer_DocumentLoaded;
			oldValue.DocumentClosing -= PdfViewer_DocumentClosed;
			oldValue.DocumentLoaded -= PdfViewer_DocumentLoaded;
			oldValue.DocumentClosed -= PdfViewer_DocumentClosed;
		}

		private void SubscribePdfViewEvents(PdfViewer newValue)
		{
			newValue.AfterDocumentChanged += PdfViewer_DocumentLoaded;
			newValue.DocumentClosing += PdfViewer_DocumentClosed;
			newValue.DocumentLoaded += PdfViewer_DocumentLoaded;
			newValue.DocumentClosed += PdfViewer_DocumentClosed;
		}

		private static void ShowPrintDialog(System.Windows.Forms.PrintDialog dlg)
		{
			if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
			{
				try
				{
					dlg.Document.Print();
				}
				catch (Win32Exception)
				{
					//Printing was canceled
				}
			}
		}

		#endregion
	}
}
