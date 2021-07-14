using Patagames.Pdf.Enums;
using System;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Printing;
using System.IO;
using System.Windows.Controls;

namespace Patagames.Pdf.Net.Controls.Wpf
{
	internal class PdfDocumentPaginator : DocumentPaginator, IDocumentPaginatorSource
	{
		private PdfDocument _doc = null;
		private DocumentPage _prevPage = null;
		private MemoryStream _mem = null;
		private bool _isValidPageCount = true;
		private int _pageCount = 0;
		private PageRange _pageRange;

		public event EventHandler<PagePrintedEventArgs> PagePrinted = null;

		public PdfDocumentPaginator(PdfDocument document, PageRange pageRange)
		{
			_doc = document;
			_pageCount = pageRange.PageTo - pageRange.PageFrom+1;
			_pageRange = pageRange;
		}


		#region Implements IDocumentPaginatorSource

		public DocumentPaginator DocumentPaginator
		{
			get
			{
				return this;
			}
		}

		#endregion

		#region Implements DocumentPaginator
		public override bool IsPageCountValid
		{
			get
			{
				return _isValidPageCount;
			}
		}

		public override int PageCount
		{
			get
			{
				return _pageCount;
			}
		}

		public override Size PageSize { get; set; }

		public override IDocumentPaginatorSource Source
		{
			get
			{
				return this;
			}
		}

		public PrintTicket PrinterTicket { get; set; }

		public override DocumentPage GetPage(int pageNumber)
		{
			pageNumber = pageNumber + _pageRange.PageFrom - 1;

			if (_prevPage != null)
				_prevPage.Dispose();

			double w = _doc.Pages[pageNumber].Width;
			double h = _doc.Pages[pageNumber].Height;
			if (PageRotation(_doc.Pages[pageNumber]) == PageRotate.Rotate270
				|| PageRotation(_doc.Pages[pageNumber]) == PageRotate.Rotate90)
			{
				var t = w; w = h; h = t;
				PrinterTicket.PageOrientation = PageOrientation.ReverseLandscape;
			}

			var visual = new DrawingVisual();
			var page = new DocumentPage(visual);
			page.PageDestroyed += Page_PageDestroyed;
			RenderPage(pageNumber, visual);
			_prevPage = page;


			if (PagePrinted != null)
			{
				var args = new PagePrintedEventArgs(pageNumber - _pageRange.PageFrom + 1, _pageCount);
				PagePrinted(this, args);
				if (args.Cancel)
					_pageCount = 0;
			}

			return page;
		}

		#endregion

		#region Private members

		private PageRotate PageRotation(PdfPage pdfPage)
		{
			int rot = pdfPage.Rotation - pdfPage.OriginalRotation;
			if (rot < 0)
				rot = 4 + rot;
			return (PageRotate)rot;
		}

		private Size GetRenderSize(Size pageSize, Size fitSize)
		{
			double w, h;
			w = pageSize.Width;
			h = pageSize.Height;

			double nh = fitSize.Height;
			double nw = w * nh / h;
			if (nw > fitSize.Width)
			{
				nw = fitSize.Width;
				nh = h * nw / w;
			}
			return new Size(nw, nh);
		}

		private void Page_PageDestroyed(object sender, EventArgs e)
		{
			if (_mem != null)
			{
				_mem.Close();
			}
		}

		private void RenderPage(int pageNumber, DrawingVisual visual)
		{
			int dpiX = PrinterTicket.PageResolution.X ?? 96;
			int dpiY = PrinterTicket.PageResolution.Y ?? 96;

			//Calculate the size of the printable area in inches
			//The printable area represents a DIPs (Device independed points) (DIPs = pixels/(DPI/96) )
			var fitSize = new Size()
			{
				Width = PageSize.Width / 96.0,
				Height = PageSize.Height / 96.0
			};

			//Get page's size in inches
			//The page size represents a points (1pt = 1/72 inch)
			var pdfSize = new Size()
			{
				Width = _doc.Pages[pageNumber].Width / 72.0f,
				Height = _doc.Pages[pageNumber].Height / 72.0f
			};

			//If page was rotated in original file, then we need to "rotate the paper in printer". 
			//For that just swap the width and height of the paper.
			if (_doc.Pages[pageNumber].OriginalRotation == PageRotate.Rotate270
				|| _doc.Pages[pageNumber].OriginalRotation == PageRotate.Rotate90)
				fitSize = new Size(fitSize.Height, fitSize.Width);

			//Calculate the render size (in inches) fitted to the paper's size. 
			var rSize = GetRenderSize(pdfSize, fitSize);

			int pixelWidth = (int)(rSize.Width * dpiX);
			int pixelHeight =(int)(rSize.Height * dpiY);

			using (PdfBitmap bmp = new PdfBitmap(pixelWidth, pixelHeight, true))
			{
				//Render to PdfBitmap using page's Render method with FPDF_PRINTING flag.
				_doc.Pages[pageNumber].RenderEx(
					bmp,
					0,
					0,
					pixelWidth,
					pixelHeight,
					PageRotate.Normal,
					RenderFlags.FPDF_PRINTING | RenderFlags.FPDF_ANNOT);


				//Rotates the PdfBitmap image depending on the orientation of the page
				PdfBitmap b2 = null;
				if (PageRotation(_doc.Pages[pageNumber]) == PageRotate.Rotate270)
					b2 = bmp.SwapXY(false, true);
				else if (PageRotation(_doc.Pages[pageNumber]) == PageRotate.Rotate180)
					b2 = bmp.FlipXY(true, true);
				else if (PageRotation(_doc.Pages[pageNumber]) == PageRotate.Rotate90)
					b2 = bmp.SwapXY(true, false);

				int stride = b2 == null ? bmp.Stride : b2.Stride;
				int width = b2 == null ? bmp.Width : b2.Width;
				int height = b2 == null ? bmp.Height : b2.Height;
				IntPtr buffer = b2 == null ? bmp.Buffer : b2.Buffer;
				var imgsrc = CreateImageSource(b2 ?? bmp);
				if (b2 != null)
					b2.Dispose();

				var dc = visual.RenderOpen();
				dc.DrawImage(imgsrc, new Rect(0, 0, imgsrc.PixelWidth / (dpiX / 96.0), imgsrc.Height / (dpiY / 90.0)));
				dc.Close();
				imgsrc = null;
			}
		}

		private BitmapSource CreateImageSource(PdfBitmap bmp)
		{
			_mem = new MemoryStream();
			bmp.Image.Save(_mem, System.Drawing.Imaging.ImageFormat.Png);

			BitmapImage bi = new BitmapImage();
			bi.BeginInit();
			bi.CacheOption = BitmapCacheOption.None;
			bi.StreamSource = _mem;
			bi.EndInit();
			bi.Freeze();
			//BitmapSource prgbaSource = new FormatConvertedBitmap(bi, PixelFormats.Pbgra32, null, 0);
			//WriteableBitmap bmp = new WriteableBitmap(prgbaSource);
			//int w = bmp.PixelWidth;
			//int h = bmp.PixelHeight;
			//int[] pixelData = new int[w * h];
			////int widthInBytes = 4 * w;
			//int widthInBytes = bmp.PixelWidth * (bmp.Format.BitsPerPixel / 8); //equals 4*w
			//bmp.CopyPixels(pixelData, widthInBytes, 0);
			//bmp.WritePixels(new Int32Rect(0, 0, w, h), pixelData, widthInBytes, 0);
			//bi = null;
			return bi;
		}


		#endregion

	}
}
