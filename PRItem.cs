using Patagames.Pdf.Enums;
using System;

namespace Patagames.Pdf.Net.Controls.Wpf
{
	internal class PRItem : IDisposable
    {
		public ProgressiveStatus status;
        public PdfBitmap Bitmap;

        public PRItem(ProgressiveStatus status, Helpers.Int32Size canvasSize)
        {
            this.status = status;
            if (canvasSize.Width > 0 && canvasSize.Height > 0)
                Bitmap = new PdfBitmap(canvasSize.Width, canvasSize.Height, true);
        }

        public void Dispose()
        {
            if (Bitmap != null)
                Bitmap.Dispose();
            Bitmap = null;
        }
    }
}
