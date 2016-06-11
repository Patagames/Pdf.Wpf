using System;
using System.Threading;
using System.Windows;
using System.Windows.Interop;

namespace Patagames.Pdf.Net.Controls.Wpf
{
	/// <summary>
	/// Provides methods to print the entire document asynchronously.
	/// </summary>
	public class PdfPrint
	{
		#region Private fields and properties
		private PdfDocument _document;

		private object _syncIsBusy = new object();
		private bool _isBusy = false;

		private object _syncIsEnd = new object();
		private bool _isEnd = false;
		private bool IsEnd
		{
			get
			{
				lock(_syncIsEnd)
				{
					return _isEnd;
				}
			}
			set
			{
				lock (_syncIsEnd)
				{
					_isEnd = value;
				}
			}
		}
		#endregion

		#region Public events
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
		/// Gets a value that indicates whether the printing has running.
		/// </summary>
		public bool IsBusy
		{
			get
			{
				lock(_syncIsBusy)
				{
					return _isBusy;
				}
			}
			set
			{
				lock(_syncIsBusy)
				{
					_isBusy = value;
				}
			}
		}
		#endregion

		#region Event raises
		/// <summary>
		/// Raises the <see cref="PrintStarted"/> event.
		/// </summary>
		protected virtual void OnPrintStarted()
		{
			if (PrintStarted != null)
				PrintStarted(this, EventArgs.Empty);
		}

		/// <summary>
		/// Raises the <see cref="PrintCompleted"/> event.
		/// </summary>
		protected virtual void OnPrintCompleted()
		{
			if (PrintCompleted != null)
				PrintCompleted(this, EventArgs.Empty);
		}

		/// <summary>
		/// Raises the <see cref="PagePrinted"/> event.
		/// </summary>
		/// <param name="e">An <see cref="PagePrintedEventArgs"/> that contains the event data.</param>
		protected virtual void OnPagePrinted(PagePrintedEventArgs e)
		{
			if (PagePrinted != null)
				PagePrinted(this, e);
		}

		/// <summary>
		/// Raises the <see cref="CancelationPending"/> event.
		/// </summary>
		protected virtual void OnCancelationPending()
		{
			if (CancelationPending != null)
				CancelationPending(this, EventArgs.Empty);
		}
		#endregion

		#region Constructor, destructor and initialization
		/// <summary>
		/// Initializes a new instance of the PdfPrint class.
		/// </summary>
		/// <param name="document">The instance of <see cref="PdfDocument"/> class</param>
		public PdfPrint(PdfDocument document)
		{
			_document = document;
		}
		#endregion

		#region Public methods
		/// <summary>
		/// Shows PrintDialog and starts asynchronous execution of a printing.
		/// </summary>
		/// <param name="window">Parent window for PrintDialog. If Null then PrintDialog not shown.</param>
		/// <remarks>This method does not breaks the previous operation before execute a new printing.</remarks>
		public void ShowDialog(Window window)
		{
			IntPtr hwnd = IntPtr.Zero;
			if (window != null)
			{
				WindowInteropHelper helper = new WindowInteropHelper(window);
				hwnd = helper.Handle;
			}

			var thread = new System.Threading.Thread(ThreadProc);
			thread.SetApartmentState(ApartmentState.STA);
			IsBusy = true;
			IsEnd = false;
			thread.Start(hwnd);
		}

		/// <summary>
		/// Starts asynchronous execution of a printing.
		/// </summary>
		/// <remarks>This method does not breaks the previous operation before execute a new printing.</remarks>
		public void StartPrintAsync()
		{
			ShowDialog(null);
		}

		/// <summary>
		/// Requests cancellation of a printing.
		/// </summary>
		public void End()
		{
			if(IsBusy)
				OnCancelationPending();
			IsEnd = true;
		}
		#endregion

		#region Private event handlers and methods
		private void ThreadProc(object param)
		{
			IntPtr hwnd = (IntPtr)param;
			var dlg = new ThreadSafePrintDialog();
			dlg.MinPage = 1;
			dlg.MaxPage = (uint)_document.Pages.Count;
			dlg.PageRange = new System.Windows.Controls.PageRange((int)dlg.MinPage, (int)dlg.MaxPage);
			dlg.UserPageRangeEnabled = true;

			if (hwnd==IntPtr.Zero || dlg.ShowDialog(hwnd) == true)
			{
				OnPrintStarted();
				var printTicket = dlg.PrintTicket;
				double printableAreaWidth = dlg.PrintableAreaWidth;
				double printableAreaHeight = dlg.PrintableAreaHeight;

				var paginator = new PdfDocumentPaginator(_document, dlg.PageRange);
				paginator.PagePrinted += Paginator_PagePrinted;
				paginator.PrinterTicket = printTicket;
				paginator.PageSize = new Size(printableAreaWidth, printableAreaHeight);
				dlg.PrintDocument(paginator, _document.Title);
				OnPrintCompleted();
			}
			IsBusy = false;
		}

		private void Paginator_PagePrinted(object sender, PagePrintedEventArgs e)
		{
			OnPagePrinted(e);
			if (IsEnd)
				e.Cancel = true;
		}
		#endregion
	}
}
