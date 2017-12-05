using System;
using System.Windows;

namespace Patagames.Pdf.Net.Controls.Wpf
{
	internal struct RenderRect
	{
		public bool IsChecked { get; set; }
		public double X { get; set; }
		public double Y { get; set; }
		public double Left { get; }
		public double Top { get; }
		public double Right { get; }
		public double Bottom { get; }
		public double Width { get; set; }
		public double Height { get; set; }

		public RenderRect(double x, double y, double width, double height, bool isChecked)
		{
			X = Left = x;
			Y = Top = y;
			Width = width;
			Height = height;
			Right = Left + Width;
			Bottom = Top + Height;
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
