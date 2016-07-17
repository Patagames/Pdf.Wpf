using Microsoft.Win32;
using Patagames.Pdf.Net.EventArguments;
using Patagames.Pdf.Net.Exceptions;
using System;
using System.Windows;
using System.Windows.Controls;
using System.ComponentModel;
using System.Windows.Threading;

namespace Patagames.Pdf.Net.Controls.Wpf.ToolBars
{
	/// <summary>
	/// Provides a container for Windows toolbar objects with predefined functionality for opening and printing
	/// </summary>
	public class PdfToolBarMain : PdfToolBar
	{
		#region Private fields
		PdfPrint _print = null;
		PrintProgress _printPogress = null;
		bool _cancelationPending = false;
		delegate void DelegateType1();
		delegate void DelegateType2(PagePrintedEventArgs e);
		#endregion

		#region Public events
		/// <summary>
		/// Occurs when the loaded document protected by password. Application should return the password through Value property
		/// </summary>1
		public event EventHandler<EventArgs<string>> PasswordRequired = null;

		/// <summary>
		/// Occurs when the print operation has been started.
		/// </summary>
		public event EventHandler PrintStarted = null;

		/// <summary>
		/// Occurs when the print operation has completed or has been canceled.
		/// </summary>
		public event EventHandler PrintCompleted = null;

		/// <summary>
		/// Occurs when the document's page gas been printed.
		/// </summary>
		public event EventHandler<PagePrintedEventArgs> PagePrinted = null;

		/// <summary>
		/// Occurs when the cancelation has been requested.
		/// </summary>
		public event EventHandler CancelationPending = null;

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
				"docOpen.png",
				btn_OpenDocClick);
			this.Items.Add(btn);

			btn = CreateButton("btnPrintDoc",
				Properties.Resources.btnPrintText,
				Properties.Resources.btnPrintToolTipText,
				"docPrint.png",
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

			if (oldValue != null && oldValue.Document != null && _print == null)
				PdfViewer_DocumentClosed(this, EventArgs.Empty);
			if (newValue != null && newValue.Document != null && _print == null)
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
		/// <returns></returns>
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
			item.IsEnabled = false;
			StartPrint();
		}

		/// <summary>
		/// Occurs when the print operation has been started.
		/// </summary>
		protected virtual void OnPrintStarted()
		{
			DelegateType1 method = OnPrintStartedRoutine;
			Dispatcher.BeginInvoke(method);
			//Dispatcher.BeginInvoke(new Action(()=>OnPrintStartedRoutine()));
		}

		/// <summary>
		/// Occurs when the document's page gas been printed.
		/// </summary>
		/// <param name="e">An <see cref="PagePrintedEventArgs"/> that contains the event data.</param>
		protected virtual void OnPagePrinted(PagePrintedEventArgs e)
		{
			DelegateType2 method = OnPagePrintedRoutine;
			Dispatcher.BeginInvoke(method, e);
			//Dispatcher.BeginInvoke(new Action(() => OnPagePrintedRoutine(e)));
		}

		/// <summary>
		/// Occurs when the print operation has completed or has been canceled.
		/// </summary>
		protected virtual void OnPrintCompleted()
		{
			DelegateType1 method = OnPrintCompletedRoutine;
			Dispatcher.BeginInvoke(method);
			//Dispatcher.BeginInvoke(new Action(() => OnPrintCompletedRoutine()));
		}


		/// <summary>
		/// Occurs when the print operation has completed or has been canceled.
		/// </summary>
		protected virtual void OnCancelationPending()
		{
			DelegateType1 method = OnCancelationPendingRoutine;
			Dispatcher.BeginInvoke(method);
			//Dispatcher.BeginInvoke(new Action(() => OnCancelationPendingRoutine()));
		}
		#endregion

		#region Event handlers for PdfViewer.document and PdfPrint

		private void PdfViewer_DocumentClosing(object sender, EventArguments.DocumentClosingEventArgs e)
		{
			e.Cancel = false;
			StopPrint();
		}

		private void PdfViewer_DocumentClosed(object sender, EventArgs e)
		{
			UpdateButtons();
			StopPrint();
			_print.PagePrinted -= Print_PagePrinted;
			_print.PrintCompleted -= Print_PrintCompleted;
			_print.PrintStarted -= Print_PrintStarted;
			_print.CancelationPending -= Print_CancelationPending;
		}

		private void PdfViewer_DocumentLoaded(object sender, EventArgs e)
		{
			UpdateButtons();
			_print = new PdfPrint(PdfViewer.Document);
			_print.PagePrinted += Print_PagePrinted;
			_print.PrintCompleted += Print_PrintCompleted;
			_print.PrintStarted += Print_PrintStarted;
			_print.CancelationPending += Print_CancelationPending;
		}

		private void Print_PrintCompleted(object sender, EventArgs e)
		{
			OnPrintCompleted();
		}

		private void Print_PrintStarted(object sender, EventArgs e)
		{
			OnPrintStarted();
		}

		private void Print_PagePrinted(object sender, PagePrintedEventArgs e)
		{
			OnPagePrinted(e);
		}

		private void Print_CancelationPending(object sender, EventArgs e)
		{
			OnCancelationPending();
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
			oldValue.DocumentClosing -= PdfViewer_DocumentClosed;
			oldValue.DocumentLoaded -= PdfViewer_DocumentLoaded;
			oldValue.DocumentClosed -= PdfViewer_DocumentClosed;
		}

		private void SubscribePdfViewEvents(PdfViewer newValue)
		{
			newValue.DocumentClosing += PdfViewer_DocumentClosed;
			newValue.DocumentLoaded += PdfViewer_DocumentLoaded;
			newValue.DocumentClosed += PdfViewer_DocumentClosed;
		}

		private static void DoEvents()
		{
			DelegateType1 method = DoEventsInternal;
			Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, method);
			//Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(delegate { }));
		}

		private static void DoEventsInternal()
		{

		}

		#endregion

		#region Printing routines
		private void StartPrint()
		{
			StopPrint();
			_cancelationPending = false;
			_print.ShowDialog(Window);
		}

		private void StopPrint()
		{
			if (_print == null)
				return;
			_print.End();
			while (_print.IsBusy)
				DoEvents();

			var tsi = this.Items[1] as Button;
			if (tsi != null)
				tsi.IsEnabled = true;
		}


		private void OnPrintStartedRoutine()
		{
			if (PrintStarted != null)
				PrintStarted(this, EventArgs.Empty);

			_printPogress = new PrintProgress();
			_printPogress.NeedStopPrinting += (s, e) => StopPrint();
			_printPogress.Owner = Window;
			OnPagePrintedRoutine(new PagePrintedEventArgs(0, PdfViewer.Document.Pages.Count));
			_printPogress.Show();
		}


		private void OnPrintCompletedRoutine()
		{
			if (PrintCompleted != null)
				PrintCompleted(this, EventArgs.Empty);

			if (_printPogress != null)
			{
				_printPogress.CloseWithoutPrompt();
				_printPogress = null;
			}
		}

		private void OnPagePrintedRoutine(PagePrintedEventArgs e)
		{
			if (PagePrinted != null)
				PagePrinted(this, e);

			if (_printPogress != null && !_cancelationPending)
				_printPogress.SetText(e.PageNumber + 1, e.TotalToPrint);
		}

		private void OnCancelationPendingRoutine()
		{
			if (CancelationPending != null)
				CancelationPending(this, EventArgs.Empty);

			_cancelationPending = true;
			if (_printPogress != null)
				_printPogress.SetText(Properties.Resources.txtPrintingStop);
		}
		#endregion
	}
}
