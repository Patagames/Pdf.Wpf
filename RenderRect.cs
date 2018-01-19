using System;
using System.Windows;

namespace Patagames.Pdf.Net.Controls.Wpf
{
    internal struct RenderRect
    {
        public bool IsChecked { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double Left { get { return X; } }
		public double Top { get { return Y; } }
		public double Right { get { return X + Width; } }
		public double Bottom { get { return Y + Height; } }
		public double Width { get; set; }
		public double Height { get; set; }

		public RenderRect(double x, double y, double width, double height, bool isChecked)
		{
			X = x;
			Y = y;
			Width = width;
			Height = height;
			IsChecked = isChecked;
		}

		internal bool Contains(double x, double y)
		{
			return new Rect(X, Y, Width, Height).Contains(x, y);
		}

		internal Rect Rect()
		{
			return new Rect(X, Y, Width, Height);
		}
	}
}
