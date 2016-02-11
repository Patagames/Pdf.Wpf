using Patagames.Pdf.Enums;

using System.Windows.Media.Imaging;

namespace Patagames.Pdf.Net.Controls.Wpf
{
	internal class PRItem
	{
		public PdfBitmap bmp;
		public WriteableBitmap wpfBmp;
		public ProgressiveRenderingStatuses status;
		public int waitTime;
		public long prevTicks;
		public int width;
		public int height;
	}
}
