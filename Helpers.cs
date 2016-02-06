using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Patagames.Pdf.Net.Controls.Wpf
{
	class Helpers
	{
        static Helpers()
        {
            // This will require restart the app if DPI is changed
            // but it is too much overhead to check it on each conversion
            var flags = BindingFlags.NonPublic | BindingFlags.Static;
            var dpiProperty = typeof(SystemParameters).GetProperty("Dpi", flags);

            Dpi = (int)dpiProperty.GetValue(null, null);
        }

        #region DPIhandling

        internal static int Dpi { get; private set; }

        internal static int PointsToPixels(double points)
        {
            return (int)(points * Dpi / 72.0);
        }

        internal static double PixelsToPoints(int pixels)
        {
            return pixels * 72.0 / Dpi;
        }

        #endregion DPIhandling

        #region Colors, pens, brushes, Rects and Sizes
        private static Color _emptyColor = Color.FromArgb(0, 0, 0, 0);
		public static Color ColorEmpty { get { return _emptyColor; } }

		internal static Pen CreatePen(Brush brush, double thick = 1.0)
		{
			return new Pen(brush, thick);
		}

		internal static Pen CreatePen(Color color, double thick=1.0)
		{
			return CreatePen(CreateBrush(color), thick);
		}

		internal static Brush CreateBrush(Color color)
		{
			return new SolidColorBrush(color);
        }

		internal static int ToArgb(Color color)
		{
			return (color.A << 24) | (color.R << 16) | (color.G << 8) | color.B;
		}

		internal static Size CreateSize(double nw, double nh)
		{
			if (nw < 0)
				nw = 0;
			if (nh < 0)
				nh = 0;
			return new Size(nw, nh);
		}

		internal static Rect CreateRect(double x, double y, double w, double h)
		{
			if (w < 0)
				w = 0;
			if (h < 0)
				h = 0;
			return new Rect(x, y, w, h);
		}

		internal static Rect CreateRect(Point location, Size size)
		{
			if (size.Width < 0)
				size.Width = 0;
			if (size.Height < 0)
				size.Height = 0;
			return new Rect(location, size);
		}
		#endregion

		#region Render
		internal static void DrawImageUnscaled(DrawingContext drawingContext, PdfBitmap bmp, double x, double y)
		{
			var isrc = BitmapSource.Create(bmp.Width, bmp.Height, Dpi, Dpi, PixelFormats.Bgra32, null, bmp.Buffer, bmp.Stride * bmp.Height, bmp.Stride);
            drawingContext.DrawImage(isrc, new Rect(x, y, PixelsToPoints(bmp.Width), PixelsToPoints(bmp.Height)));
		}

		internal static void FillRectangle(DrawingContext drawingContext, Brush brush, Rect rect)
		{
			drawingContext.DrawRectangle(brush, null, rect);
		}

		internal static void DrawRectangle(DrawingContext drawingContext, Pen pen, Rect rect)
		{
			drawingContext.DrawRectangle(null, pen, rect);
		}

        #endregion

    }
}
