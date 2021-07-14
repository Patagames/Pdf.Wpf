namespace Patagames.Pdf.Net.Controls.Wpf
{
	/// <summary>
	/// Specifies how the PdfViewer will process mouse events
	/// </summary>
	public enum MouseModes
	{
		/// <summary>
		/// By default. Select text, process links
		/// </summary>
		Default,

		/// <summary>
		/// Any processing is missing
		/// </summary>
		None,

		/// <summary>
		/// Select text only
		/// </summary>
		SelectTextTool,

		/// <summary>
		/// Move the page
		/// </summary>
		PanTool,
	}
}
