using System;
using System.Reflection;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Patagames.Pdf.Net.Controls.Wpf
{
	internal class Helpers
	{
        static Helpers()
        {
            var flags = BindingFlags.NonPublic | BindingFlags.Static;
            var dpiProperty = typeof(SystemParameters).GetProperty("Dpi", flags);
            Dpi = (int)dpiProperty.GetValue(null, null);
        }

        #region DPIhandling
        /// <summary>
        /// Gets current logical DPI
        /// </summary>
        /// <remarks>
        /// This will require restart the app if DPI is changed
        /// but it is too much overhead to check it on each conversion
        /// </remarks>
        public static int Dpi { get; private set; }

        /// <summary>
        /// Convert WPF units (DIPs - device independent pixels) to physical pixels.
        /// </summary>
        /// <param name="units">Device independent pixels</param>
        /// <returns>Physical pixels</returns>
        /// <remarks>1 DIP = 1/96 DPI</remarks>
        public static int UnitsToPixels(double units)
        {
            return (int)(units / 96 * Dpi);
        }

        /// <summary>
        /// Convert physical pixels to WPF units (DIPs - device independent pixels)
        /// </summary>
        /// <param name="pixels">Physical pixels</param>
        /// <returns>Device independent pixels</returns>
        /// <remarks>1 DIP = 1/96 DPI</remarks>
        public static double PixelsToUnits(int pixels)
        {
            return (double)pixels * 96.0 / Dpi;
        }

        #endregion DPIhandling

        #region Colors, pens, brushes, Rects and Sizes, etc
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

		internal static RenderRect CreateRenderRect(double x, double y, double w, double h, bool isChecked)
		{
			if (w < 0)
				w = 0;
			if (h < 0)
				h = 0;
			return new RenderRect(x, y, w, h, isChecked);
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

		internal static double ThicknessHorizontal(Thickness pageMargin)
		{
			return pageMargin.Left + pageMargin.Right;
        }

		internal static double ThicknessVertical(Thickness pageMargin)
		{
			return pageMargin.Top + pageMargin.Bottom;
        }
        #endregion

        #region Render
        public static void DrawImageUnscaled(DrawingContext drawingContext, WriteableBitmap wpfBmp, double x, double y)
		{
			drawingContext.DrawImage(wpfBmp, new Rect(x, y, PixelsToUnits(wpfBmp.PixelWidth), PixelsToUnits(wpfBmp.PixelHeight)));
		}
        public static void FillRectangle(DrawingContext drawingContext, Brush brush, Rect rect)
		{
			drawingContext.DrawRectangle(brush, null, rect);
		}

        public static void DrawRectangle(DrawingContext drawingContext, Pen pen, Rect rect)
		{
			drawingContext.DrawRectangle(null, pen, rect);
		}

        /// <summary>
        /// Calculate pixel offset to prevent image blur.
        /// </summary>
        /// <param name="UI">UIElement in which the offset is calculated.</param>
        /// <returns>The offset point</returns>
        public static Point GetPixelOffset(UIElement UI)
        {
            Point pixelOffset = new Point();

            PresentationSource ps = PresentationSource.FromVisual(UI);
            if (ps != null)
            {
                Visual rootVisual = ps.RootVisual;

                // Transform (0,0) from this element up to pixels.
                pixelOffset = UI.TransformToAncestor(rootVisual).Transform(pixelOffset);
                pixelOffset = ApplyVisualTransform(pixelOffset, rootVisual, false);
                pixelOffset = ps.CompositionTarget.TransformToDevice.Transform(pixelOffset);

                // Round the origin to the nearest whole pixel.
                pixelOffset.X = Math.Round(pixelOffset.X);
                pixelOffset.Y = Math.Round(pixelOffset.Y);

                // Transform the whole-pixel back to this element.
                pixelOffset = ps.CompositionTarget.TransformFromDevice.Transform(pixelOffset);
                pixelOffset = ApplyVisualTransform(pixelOffset, rootVisual, true);
                var ttd = rootVisual.TransformToDescendant(UI);
                if(ttd!= null)
                    pixelOffset = ttd.Transform(pixelOffset);
            }

            return pixelOffset;
        }

        private static Point ApplyVisualTransform(Point point, Visual v, bool inverse)
        {
            bool success = true;
            return TryApplyVisualTransform(point, v, inverse, true, out success);
        }

        private static Point TryApplyVisualTransform(Point point, Visual v, bool inverse, bool throwOnError, out bool success)
        {
            success = true;
            if (v != null)
            {
                Matrix visualTransform = GetVisualTransform(v);
                if (inverse)
                {
                    if (!throwOnError && !visualTransform.HasInverse)
                    {
                        success = false;
                        return new Point(0, 0);
                    }
                    visualTransform.Invert();
                }
                point = visualTransform.Transform(point);
            }
            return point;
        }

        /// <summary>
        /// Gets the matrix that will convert a point from "above" the
        /// coordinate space of a visual into the the coordinate space
        /// "below" the visual.
        /// </summary>
        /// <param name="v">Visual</param>
        /// <returns>Matrix</returns>
        private static Matrix GetVisualTransform(Visual v)
        {
            if (v != null)
            {
                Matrix m = Matrix.Identity;

                Transform transform = VisualTreeHelper.GetTransform(v);
                if (transform != null)
                {
                    Matrix cm = transform.Value;
                    m = Matrix.Multiply(m, cm);
                }

                Vector offset = VisualTreeHelper.GetOffset(v);
                m.Translate(offset.X, offset.Y);

                return m;
            }

            return Matrix.Identity;
        }
        #endregion

        #region Int32 structures which are missing in WPF
        /// <summary>
        /// Represents Int32 size
        /// </summary>
        public struct Int32Size
		{
			public int Width;
			public int Height;

            public Int32Size(int width, int height)
            {
                Width = width;
                Height = height;
            }

            public override bool Equals(object obj)
            {
                if (!(obj is Int32Size))
                    return false;

                var pt = (Int32Size)obj;
                return Equals(pt);
            }

            public bool Equals(Int32Size obj)
            {
                if (!obj.Width.Equals(this.Width))
                    return false;
                if (!obj.Height.Equals(this.Height))
                    return false;

                return true;
            }

            public static bool operator ==(Int32Size left, Int32Size right)
            {
                return left.Equals(right);
            }

            public static bool operator !=(Int32Size left, Int32Size right)
            {
                return !(left == right);
            }

            public override int GetHashCode()
            {
                unchecked // Overflow is fine, just wrap
                {
                    int hash = 17;
                    // Suitable nullity checks etc, of course :)
                    hash = hash * 23 + Width.GetHashCode();
                    hash = hash * 23 + Height.GetHashCode();
                    return hash;
                }
            }
        }

        /// <summary>
        /// Represents Int32 point
        /// </summary>
        public struct Int32Point
        {
            public int X;
            public int Y;

            public Int32Point(int x, int y)
            {
                X = x;
                Y = y;
            }

            public override bool Equals(object obj)
            {
                if (!(obj is Int32Point))
                    return false;

                var pt = (Int32Point)obj;
                return Equals(pt);
            }

            public bool Equals(Int32Point obj)
            {
                if (!obj.X.Equals(this.X))
                    return false;
                if (!obj.Y.Equals(this.Y))
                    return false;

                return true;
            }

            public static bool operator ==(Int32Point left, Int32Point right)
            {
                return left.Equals(right);
            }

            public static bool operator !=(Int32Point left, Int32Point right)
            {
                return !(left == right);
            }

            public override int GetHashCode()
            {
                unchecked // Overflow is fine, just wrap
                {
                    int hash = 17;
                    // Suitable nullity checks etc, of course :)
                    hash = hash * 23 + X.GetHashCode();
                    hash = hash * 23 + Y.GetHashCode();
                    return hash;
                }
            }

        }
        #endregion

        #region Code Security
        public static void SecurityAssert()
        {
#if !DOTNET50
            new System.Drawing.Printing.PrintingPermission(System.Drawing.Printing.PrintingPermissionLevel.DefaultPrinting).Assert();
#endif
        }

        internal static void SecurityRevert()
        {
#if !DOTNET50
            System.Security.CodeAccessPermission.RevertAssert();
#endif
        }
        #endregion
    }
}
