using System;

namespace Patagames.Pdf.Net.Controls.Wpf
{
	/// <summary>
	/// Provides data for the <see cref="E:Patagames.Pdf.Net.Controls.Wpf.PdfPrint.PagePrinted"/> event.
	/// </summary>
	public class PagePrintedEventArgs : EventArgs
	{
		/// <summary>
		/// Zero-based number of a page which has been printed.
		/// </summary>
		public int PageNumber { get; private set; }

		/// <summary>
		/// Indicates how many pages should be printed in current job.
		/// </summary>
		public int TotalToPrint { get; set; }
		
		/// <summary>
		/// Gets or sets a value indicating whether the printing job should be canceled.
		/// </summary>
		public bool Cancel { get; set; }

		/// <summary>
		/// Constructs new instance of PagePrintedEventArgs class
		/// </summary>
		/// <param name="PageNumber">Zero-based number of a page which has been printed.</param>
		/// <param name="TotalToPrint">Indicates how many pages should be printed in current job.</param>
		public PagePrintedEventArgs(int PageNumber, int TotalToPrint)
		{
			this.PageNumber = PageNumber;
			this.TotalToPrint = TotalToPrint;
			Cancel = false;
		}
	}
}
