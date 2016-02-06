using Patagames.Pdf.Enums;
using System.Windows;

namespace Patagames.Pdf.Net.Controls.Wpf
{
	internal class PRItem
	{
		public PdfBitmap bmp;
		public ProgressiveRenderingStatuses status;
		public int waitTime;
		public long prevTicks;
		public int width;
		public int height;
	}
}
