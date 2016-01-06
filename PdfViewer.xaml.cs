using Patagames.Pdf.Enums;
using Patagames.Pdf.Net.EventArguments;
using Patagames.Pdf.Net.Exceptions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace Patagames.Pdf.Net.Controls.Wpf
{
	/// <summary>
	/// Represents a pdf view control for displaying an Pdf document.
	/// </summary>	
	public partial class PdfViewer : UserControl, IScrollInfo
	{
		#region Private fields
		private SelectInfo _selectInfo = new SelectInfo() { StartPage = -1 };
		private SortedDictionary<int, List<HighlightInfo>> _highlightedText = new SortedDictionary<int, List<HighlightInfo>>();
		private bool _mousePressed = false;
		private bool _mousePressedInLink = false;
		private int _onstartPageIndex = 0;
		private Point _panToolInitialScrollPosition;
		private Point _panToolInitialMousePosition;

		private PdfForms _fillForms;
		private List<Rect> _selectedRectangles = new List<Rect>();
		private Pen _pageBorderColorPen;
		private Brush _selectColorBrush;
		private Pen _pageSeparatorColorPen;
		private Pen _currentPageHighlightColorPen;

		private PdfDocument _document;
		private SizeModes _sizeMode = SizeModes.FitToWidth;
		private Color _formHighlightColor;
		private Color _pageBackColor;
		private Color _pageBorderColor;
		private Color _textSelectColor;
		private Thickness _pageMargin;
		private float _zoom;
		private ViewModes _viewMode;
		private Color _pageSeparatorColor;
		private bool _showPageSeparator;
		private Color _currentPageHighlightColor;
		private bool _showCurrentPageHighlight;
		private VerticalAlignment _pageVAlign;

		private HorizontalAlignment _pageHAlign;
		private RenderFlags _renderFlags = RenderFlags.FPDF_LCD_TEXT | RenderFlags.FPDF_NO_CATCH;
		private int _tilesCount;
		private MouseModes _mouseMode;


		private Rect[] _renderRects;
		private int _startPage { get { return ViewMode == ViewModes.SinglePage ? Document.Pages.CurrentIndex : 0; } }
		private int _endPage { get { return ViewMode == ViewModes.SinglePage ? Document.Pages.CurrentIndex : (_renderRects != null ? _renderRects.Length - 1 : -1); } }

		private Size _extent = new Size(0, 0);
		private Size _viewport = new Size(0, 0);
		private Point _autoScrollPosition = new Point(0, 0);
		private bool _isProgrammaticallyFocusSetted=false;

		PdfPage _invalidatePage = null;
		FS_RECTF _invalidateRect;
		#endregion

		#region Events
		/// <summary>
		/// Occurs whenever the document loads.
		/// </summary>
		public event EventHandler DocumentLoaded;

		/// <summary>
		/// Occurs whenever the document unloads.
		/// </summary>
		public event EventHandler DocumentClosed;

		/// <summary>
		/// Occurs when the <see cref="SizeMode"/> property has changed.
		/// </summary>
		public event EventHandler SizeModeChanged;

		/// <summary>
		/// Event raised when the value of the <see cref="PageBackColor"/> property is changed on Control..
		/// </summary>
		public event EventHandler PageBackColorChanged;

		/// <summary>
		/// Occurs when the <see cref="PageMargin"/> property has changed.
		/// </summary>
		public event EventHandler PageMarginChanged;

		/// <summary>
		/// Event raised when the value of the <see cref="PageBorderColor"/> property is changed on Control.
		/// </summary>
		public event EventHandler PageBorderColorChanged;

		/// <summary>
		/// Event raised when the value of the <see cref="TextSelectColor"/> property is changed on Control.
		/// </summary>
		public event EventHandler TextSelectColorChanged;

		/// <summary>
		/// Event raised when the value of the <see cref="FormHighlightColor"/> property is changed on Control.
		/// </summary>
		public event EventHandler FormHighlightColorChanged;

		/// <summary>
		/// Occurs when the <see cref="Zoom"/> property has changed.
		/// </summary>
		public event EventHandler ZoomChanged;

		/// <summary>
		/// Occurs when the current selection has changed.
		/// </summary>
		public event EventHandler SelectionChanged;

		/// <summary>
		/// Occurs when the <see cref="ViewMode"/> property has changed.
		/// </summary>
		public event EventHandler ViewModeChanged;

		/// <summary>
		/// Occurs when the <see cref="PageSeparatorColor"/> property has changed.
		/// </summary>
		public event EventHandler PageSeparatorColorChanged;

		/// <summary>
		/// Occurs when the <see cref="ShowPageSeparator"/> property has changed.
		/// </summary>
		public event EventHandler ShowPageSeparatorChanged;

		/// <summary>
		/// Occurs when the <see cref="CurrentPage"/> or <see cref="CurrentIndex"/> property has changed.
		/// </summary>
		public event EventHandler CurrentPageChanged;

		/// <summary>
		/// Occurs when the <see cref="CurrentPageHighlightColor"/> property has changed.
		/// </summary>
		public event EventHandler CurrentPageHighlightColorChanged;

		/// <summary>
		/// Occurs when the <see cref="ShowCurrentPageHighlight"/> property has changed.
		/// </summary>
		public event EventHandler ShowCurrentPageHighlightChanged;

		/// <summary>
		/// Occurs when the value of the <see cref="PageVAlign"/> or <see cref="PageHAlign"/> property has changed.
		/// </summary>
		public event EventHandler PageAlignChanged;

		/// <summary>
		/// Occurs before PdfLink or WebLink on the page was clicked.
		/// </summary>
		public event EventHandler<PdfBeforeLinkClickedEventArgs> BeforeLinkClicked;

		/// <summary>
		/// Occurs after PdfLink or WebLink on the page was clicked.
		/// </summary>
		public event EventHandler<PdfAfterLinkClickedEventArgs> AfterLinkClicked;

		/// <summary>
		/// Occurs when the value of the <see cref="RenderFlags"/> property has changed.
		/// </summary>
		public event EventHandler RenderFlagsChanged;

		/// <summary>
		/// Occurs when the value of the <see cref="TilesCount"/> property has changed.
		/// </summary>
		public event EventHandler TilesCountChanged;

		/// <summary>
		/// Occurs when the text highlighting changed
		/// </summary>
		public event EventHandler HighlightedTextChanged;

		/// <summary>
		/// Occurs when the value of the <see cref="MouseModes"/> property has changed.
		/// </summary>
		public event EventHandler MouseModeChanged;

		#endregion

		#region Event raises
		/// <summary>
		/// Raises the <see cref="DocumentLoaded"/> event.
		/// </summary>
		/// <param name="e">An System.EventArgs that contains the event data.</param>
		protected virtual void OnDocumentLoaded(EventArgs e)
		{
			if (Document != null && Document.FormFill != null)
				Document.FormFill.SetHighlightColorEx(FormFieldTypes.FPDF_FORMFIELD_UNKNOWN, Helpers.ToArgb(_formHighlightColor));

			if (DocumentLoaded != null)
				DocumentLoaded(this, e);
		}

		/// <summary>
		/// Raises the <see cref="DocumentClosed"/> event.
		/// </summary>
		/// <param name="e">An System.EventArgs that contains the event data.</param>
		protected virtual void OnDocumentClosed(EventArgs e)
		{
			if (DocumentClosed != null)
				DocumentClosed(this, e);
		}

		/// <summary>
		/// Raises the <see cref="SizeModeChanged"/> event.
		/// </summary>
		/// <param name="e">An System.EventArgs that contains the event data.</param>
		protected virtual void OnSizeModeChanged(EventArgs e)
		{
			if (SizeModeChanged != null)
				SizeModeChanged(this, e);
		}

		/// <summary>
		/// Raises the <see cref="PageBackColorChanged"/> event.
		/// </summary>
		/// <param name="e">An System.EventArgs that contains the event data.</param>
		protected virtual void OnPageBackColorChanged(EventArgs e)
		{
			if (PageBackColorChanged != null)
				PageBackColorChanged(this, e);
		}

		/// <summary>
		/// Raises the <see cref="PageMarginChanged"/> event.
		/// </summary>
		/// <param name="e">An System.EventArgs that contains the event data.</param>
		protected virtual void OnPageMarginChanged(EventArgs e)
		{
			if (PageMarginChanged != null)
				PageMarginChanged(this, e);
		}

		/// <summary>
		/// Raises the <see cref="PageBorderColorChanged"/> event.
		/// </summary>
		/// <param name="e">An System.EventArgs that contains the event data.</param>
		protected virtual void OnPageBorderColorChanged(EventArgs e)
		{
			if (PageBorderColorChanged != null)
				PageBorderColorChanged(this, e);
		}

		/// <summary>
		/// Raises the <see cref="TextSelectColorChanged"/> event.
		/// </summary>
		/// <param name="e">An System.EventArgs that contains the event data.</param>
		protected virtual void OnTextSelectColorChanged(EventArgs e)
		{
			if (TextSelectColorChanged != null)
				TextSelectColorChanged(this, e);
		}

		/// <summary>
		/// Raises the <see cref="FormHighlightColorChanged"/> event.
		/// </summary>
		/// <param name="e">An System.EventArgs that contains the event data.</param>
		protected virtual void OnFormHighlightColorChanged(EventArgs e)
		{
			if (FormHighlightColorChanged != null)
				FormHighlightColorChanged(this, e);
		}

		/// <summary>
		/// Raises the <see cref="ZoomChanged"/> event.
		/// </summary>
		/// <param name="e">An System.EventArgs that contains the event data.</param>
		protected virtual void OnZoomChanged(EventArgs e)
		{
			if (ZoomChanged != null)
				ZoomChanged(this, e);
		}

		/// <summary>
		/// Raises the <see cref="SelectionChanged"/> event.
		/// </summary>
		/// <param name="e">An System.EventArgs that contains the event data.</param>
		protected virtual void OnSelectionChanged(EventArgs e)
		{
			if (SelectionChanged != null)
				SelectionChanged(this, e);
		}

		/// <summary>
		/// Raises the <see cref="ViewModeChanged"/> event.
		/// </summary>
		/// <param name="e">An System.EventArgs that contains the event data.</param>
		protected virtual void OnViewModeChanged(EventArgs e)
		{
			if (ViewModeChanged != null)
				ViewModeChanged(this, e);
		}

		/// <summary>
		/// Raises the <see cref="PageSeparatorColorChanged"/> event.
		/// </summary>
		/// <param name="e">An System.EventArgs that contains the event data.</param>
		protected virtual void OnPageSeparatorColorChanged(EventArgs e)
		{
			if (PageSeparatorColorChanged != null)
				PageSeparatorColorChanged(this, e);
		}

		/// <summary>
		/// Raises the <see cref="ShowPageSeparatorChanged"/> event.
		/// </summary>
		/// <param name="e">An System.EventArgs that contains the event data.</param>
		protected virtual void OnShowPageSeparatorChanged(EventArgs e)
		{
			if (ShowPageSeparatorChanged != null)
				ShowPageSeparatorChanged(this, e);
		}

		/// <summary>
		/// Raises the <see cref="CurrentPageChanged"/> event.
		/// </summary>
		/// <param name="e">An System.EventArgs that contains the event data.</param>
		protected virtual void OnCurrentPageChanged(EventArgs e)
		{
			if (CurrentPageChanged != null)
				CurrentPageChanged(this, e);
		}

		/// <summary>
		/// Raises the <see cref="CurrentPageHighlightColorChanged"/> event.
		/// </summary>
		/// <param name="e">An System.EventArgs that contains the event data.</param>
		protected virtual void OnCurrentPageHighlightColorChanged(EventArgs e)
		{
			if (CurrentPageHighlightColorChanged != null)
				CurrentPageHighlightColorChanged(this, e);
		}

		/// <summary>
		/// Raises the <see cref="ShowCurrentPageHighlightChanged"/> event.
		/// </summary>
		/// <param name="e">An System.EventArgs that contains the event data.</param>
		protected virtual void OnShowCurrentPageHighlightChanged(EventArgs e)
		{
			if (ShowCurrentPageHighlightChanged != null)
				ShowCurrentPageHighlightChanged(this, e);
		}

		/// <summary>
		/// Raises the <see cref="PageAlignChanged"/> event.
		/// </summary>
		/// <param name="e">An System.EventArgs that contains the event data.</param>
		protected virtual void OnPageAlignChanged(EventArgs e)
		{
			if (PageAlignChanged != null)
				PageAlignChanged(this, e);
		}

		/// <summary>
		/// Raises the <see cref="BeforeLinkClicked"/> event.
		/// </summary>
		/// <param name="e">An PdfBeforeLinkClickedEventArgs that contains the event data.</param>
		protected virtual void OnBeforeLinkClicked(PdfBeforeLinkClickedEventArgs e)
		{
			if (BeforeLinkClicked != null)
				BeforeLinkClicked(this, e);
		}

		/// <summary>
		/// Raises the <see cref="AfterLinkClicked"/> event.
		/// </summary>
		/// <param name="e">An PdfAfterLinkClickedEventArgs that contains the event data.</param>
		protected virtual void OnAfterLinkClicked(PdfAfterLinkClickedEventArgs e)
		{
			if (AfterLinkClicked != null)
				AfterLinkClicked(this, e);
		}

		/// <summary>
		/// Raises the <see cref="RenderFlagsChanged"/> event.
		/// </summary>
		/// <param name="e">An System.EventArgs that contains the event data.</param>
		protected virtual void OnRenderFlagsChanged(EventArgs e)
		{
			if (RenderFlagsChanged != null)
				RenderFlagsChanged(this, e);
		}

		/// <summary>
		/// Raises the <see cref="TilesCountChanged"/> event.
		/// </summary>
		/// <param name="e">An System.EventArgs that contains the event data.</param>
		protected virtual void OnTilesCountChanged(EventArgs e)
		{
			if (TilesCountChanged != null)
				TilesCountChanged(this, e);
		}

		/// <summary>
		/// Raises the <see cref="HighlightedTextChanged"/> event.
		/// </summary>
		/// <param name="e">An System.EventArgs that contains the event data.</param>
		protected virtual void OnHighlightedTextChanged(EventArgs e)
		{
			if (HighlightedTextChanged != null)
				HighlightedTextChanged(this, e);
		}
		
		/// <summary>
		/// Raises the <see cref="MouseModeChanged"/> event.
		/// </summary>
		/// <param name="e">An System.EventArgs that contains the event data.</param>
		protected virtual void OnMouseModeChanged(EventArgs e)
		{
			if (MouseModeChanged != null)
				MouseModeChanged(this, e);
		}
		#endregion

		#region Public properties
		/// <summary>
		/// Gets or sets the Forms object associated with the current PdfViewer control.
		/// </summary>
		/// <remarks>The FillForms object are used for the correct processing of forms within the PdfViewer control</remarks>
		public PdfForms FillForms { get { return _fillForms; } }

		/// <summary>
		/// Gets or sets the PDF document associated with the current PdfViewer control.
		/// </summary>
		public PdfDocument Document
		{
			get
			{
				return _document;
			}
			set
			{
				if (!System.ComponentModel.DesignerProperties.GetIsInDesignMode(this) && !AllowSetDocument && value != null)
					throw new ArgumentException(Patagames.Pdf.Net.Controls.Wpf.Properties.Resources.err0001, "AllowSetDocument");
				if (_document != value)
				{
					Pdfium.FPDF_ShowSplash(true);
					CloseDocument();
					_document = value;
					UpdateDocLayout();
					if (_document != null)
					{
						_document.Pages.CurrentPageChanged += Pages_CurrentPageChanged;
						_document.Pages.PageInserted += Pages_PageInserted;
						_document.Pages.PageDeleted += Pages_PageDeleted;
						SetCurrentPage(_onstartPageIndex);
						ScrollToPage(_onstartPageIndex);
						OnDocumentLoaded(EventArgs.Empty);
					}
				}
			}
		}

		/// <summary>
		/// Gets or sets the background color for the control under PDF page.
		/// </summary>
		public Color PageBackColor
		{
			get
			{
				return _pageBackColor;
			}
			set
			{
				if (_pageBackColor != value)
				{
					_pageBackColor = value;
					InvalidateVisual();
					OnPageBackColorChanged(EventArgs.Empty);
				}

			}
		}

		/// <summary>
		/// Specifies space between pages margins
		/// </summary>
		public Thickness PageMargin
		{
			get
			{
				return _pageMargin;
			}
			set
			{
				if (_pageMargin != value)
				{
					_pageMargin = value;
					UpdateDocLayout();
					OnPageMarginChanged(EventArgs.Empty);
				}
			}
		}

		/// <summary>
		/// Gets or sets the border color of the page
		/// </summary>
		public Color PageBorderColor
		{
			get
			{
				return _pageBorderColor;
			}
			set
			{
				if (_pageBorderColor != value)
				{
					_pageBorderColor = value;
					_pageBorderColorPen = Helpers.CreatePen(_pageBorderColor);
					InvalidateVisual();
					OnPageBorderColorChanged(EventArgs.Empty);
				}
			}
		}

		/// <summary>
		/// Control how the PdfViewer will handle  pages placement and control sizing
		/// </summary>
		public SizeModes SizeMode
		{
			get
			{
				return _sizeMode;
			}
			set
			{
				if (_sizeMode != value)
				{
					_sizeMode = value;
					UpdateDocLayout();
					OnSizeModeChanged(EventArgs.Empty);
				}
			}
		}

		/// <summary>
		/// Gets or sets the selection color of the control.
		/// </summary>
		public Color TextSelectColor
		{
			get
			{
				return _textSelectColor;
			}
			set
			{
				if (_textSelectColor != value)
				{
					_textSelectColor = value;
					_selectColorBrush = Helpers.CreateBrush(_textSelectColor);
					InvalidateVisual();
					OnTextSelectColorChanged(EventArgs.Empty);
				}

			}
		}

		/// <summary>
		/// Gets or set the highlight color of the form fields in the document.
		/// </summary>
		public Color FormHighlightColor
		{
			get
			{
				return _formHighlightColor;
			}
			set
			{
				if (_formHighlightColor != value)
				{
					_formHighlightColor = value;
					if (Document != null && Document.FormFill != null)
						Document.FormFill.SetHighlightColorEx(FormFieldTypes.FPDF_FORMFIELD_UNKNOWN, Helpers.ToArgb(_formHighlightColor));
					InvalidateVisual();
					OnFormHighlightColorChanged(EventArgs.Empty);
				}
			}
		}

		/// <summary>
		/// This property allows you to scale the PDF page. To take effect the <see cref="SizeMode"/> property should be Zoom
		/// </summary>
		public float Zoom
		{
			get
			{
				return _zoom;
			}
			set
			{
				if (_zoom != value)
				{
					_zoom = value;
					UpdateDocLayout();
					OnZoomChanged(EventArgs.Empty);
				}
			}
		}

		/// <summary>
		/// Gets selected text from PdfView control
		/// </summary>
		public string SelectedText
		{
			get
			{
				if (Document == null)
					return "";

				var selTmp = NormalizeSelectionInfo();

				if (selTmp.StartPage < 0 || selTmp.StartIndex < 0)
					return "";

				string ret = "";
				for (int i = selTmp.StartPage; i <= selTmp.EndPage; i++)
				{
					if (ret != "")
						ret += "\r\n";

					int s = 0;
					if (i == selTmp.StartPage)
						s = selTmp.StartIndex;

					int len = Document.Pages[i].Text.CountChars;
					if (i == selTmp.EndPage)
						len = (selTmp.EndIndex + 1) - s;

					ret += Document.Pages[i].Text.GetText(s, len);
				}
				return ret;
			}
		}

		/// <summary>
		/// Gets information about selected text in a PdfView control
		/// </summary>
		public SelectInfo SelectInfo { get { return NormalizeSelectionInfo(); } }

		/// <summary>
		/// Control how the PdfViewer will display pages
		/// </summary>
		public ViewModes ViewMode
		{
			get
			{
				return _viewMode;
			}
			set
			{
				if (_viewMode != value)
				{
					_viewMode = value;
					UpdateDocLayout();
					if(_renderRects!= null)
						MakeVisible(null, _renderRects[CurrentIndex]);
					OnViewModeChanged(EventArgs.Empty);
				}
			}
		}

		/// <summary>
		/// Gets or sets the page separator color.
		/// </summary>
		public Color PageSeparatorColor
		{
			get
			{
				return _pageSeparatorColor;
			}
			set
			{
				if (_pageSeparatorColor != value)
				{
					_pageSeparatorColor = value;
					_pageSeparatorColorPen = Helpers.CreatePen(_pageSeparatorColor);
					InvalidateVisual();
					OnPageSeparatorColorChanged(EventArgs.Empty);
				}
			}
		}

		/// <summary>
		/// Determines whether the page separator is visible or hidden
		/// </summary>
		public bool ShowPageSeparator
		{
			get
			{
				return _showPageSeparator;
			}
			set
			{
				if (_showPageSeparator != value)
				{
					_showPageSeparator = value;
					InvalidateVisual();
					OnShowPageSeparatorChanged(EventArgs.Empty);
				}
			}
		}

		/// <summary>
		/// Gets or sets the current page highlight color.
		/// </summary>
		public Color CurrentPageHighlightColor
		{
			get
			{
				return _currentPageHighlightColor;
			}
			set
			{
				if (_currentPageHighlightColor != value)
				{
					_currentPageHighlightColor = value;
					_currentPageHighlightColorPen = Helpers.CreatePen(_currentPageHighlightColor, 4);
					InvalidateVisual();
					OnCurrentPageHighlightColorChanged(EventArgs.Empty);
				}
			}
		}

		/// <summary>
		/// Determines whether the current page's highlight is visible or hidden.
		/// </summary>
		public bool ShowCurrentPageHighlight
		{
			get
			{
				return _showCurrentPageHighlight;
			}
			set
			{
				if (_showCurrentPageHighlight != value)
				{
					_showCurrentPageHighlight = value;
					InvalidateVisual();
					OnShowCurrentPageHighlightChanged(EventArgs.Empty);
				}
			}
		}

		/// <summary>
		/// Gets or sets the current index of a page in PdfPageCollection
		/// </summary>
		public int CurrentIndex
		{
			get
			{
				if (Document == null)
					return -1;
				return Document.Pages.CurrentIndex;
			}
			set
			{
				if (Document == null)
					return;
				Document.Pages.CurrentIndex = value;
			}
		}

		/// <summary>
		/// Gets the current PdfPage item by <see cref="CurrentIndex "/>
		/// </summary>
		public PdfPage CurrentPage { get { return Document.Pages.CurrentPage; } }

		/// <summary>
		/// Gets or sets a value indicating whether the control can accept PDF document through Document property.
		/// </summary>
		public bool AllowSetDocument { get; set; }

		/// <summary>
		/// Gets or sets the vertical alignment of page in the control.
		/// </summary>
		public VerticalAlignment PageVAlign
		{
			get
			{
				return _pageVAlign;
			}
			set
			{
				if (_pageVAlign != value)
				{
					_pageVAlign = value;
					UpdateDocLayout();
					OnPageAlignChanged(EventArgs.Empty);
				}
			}
		}

		/// <summary>
		/// Gets or sets the horizontal alignment of page in the control.
		/// </summary>
		public HorizontalAlignment PageHAlign
		{
			get
			{
				return _pageHAlign;
			}
			set
			{
				if (_pageHAlign != value)
				{
					_pageHAlign = value;
					UpdateDocLayout();
					OnPageAlignChanged(EventArgs.Empty);
				}
			}
		}


		/// <summary>
		/// Gets or sets a RenderFlags. None for normal display, or combination of <see cref="RenderFlags"/>
		/// </summary>
		public RenderFlags RenderFlags
		{
			get
			{
				return _renderFlags;
			}
			set
			{
				if (_renderFlags != value)
				{
					_renderFlags = value;
					InvalidateVisual();
					OnRenderFlagsChanged(EventArgs.Empty);
				}
			}
		}

		/// <summary>
		/// Gets or sets visible page count for tiles view mode
		/// </summary>
		public int TilesCount
		{
			get
			{
				return _tilesCount;
			}
			set
			{
				int tmp = value < 2 ? 2 : value;
				if (_tilesCount != tmp)
				{
					_tilesCount = tmp;
					UpdateDocLayout();
					OnTilesCountChanged(EventArgs.Empty);
				}
			}
		}

		/// <summary>
		/// Gets information about highlighted text in a PdfView control
		/// </summary>
		public SortedDictionary<int, List<HighlightInfo>> HighlightedTextInfo { get { return _highlightedText; } }

		/// <summary>
		/// Gets or sets mouse mode for PdfViewer control
		/// </summary>
		public MouseModes MouseMode
		{
			get
			{
				return _mouseMode;
			}
			set
			{
				if (_mouseMode != value)
				{
					_mouseMode = value;
					OnMouseModeChanged(EventArgs.Empty);
				}
			}
		}
		#endregion

		#region Public methods
		/// <summary>
		/// Scrolls the control view to the specified page.
		/// </summary>
		/// <param name="index">Zero-based index of a page.</param>
		public void ScrollToPage(int index)
		{
			if (Document == null)
				return;
			if (ViewMode == ViewModes.SinglePage)
			{
				SetCurrentPage(index);
				InvalidateVisual();
			}
			else
			{
				var rect = renderRects(index);
				if (rect.Width == 0 || rect.Height == 0)
					return;
				//_autoScrollPosition = new Point(rect.X, rect.Y);
				SetVerticalOffset(rect.Y);
				SetHorizontalOffset(rect.X);
			}
		}

		/// <summary>
		/// Rotates the specified page to the specified angle.
		/// </summary>
		/// <param name="pageIndex">Zero-based index of a page for rotation.</param>
		/// <param name="angle">The angle which must be turned page</param>
		/// <remarks>The PDF page rotates clockwise. See <see cref="PageRotate"/> for details.</remarks>
		public void RotatePage(int pageIndex, PageRotate angle)
		{
			if (Document == null)
				return;
			Document.Pages[pageIndex].Rotation = angle;
			UpdateDocLayout();

		}

		/// <summary>
		/// Selects the text contained in specified pages.
		/// </summary>
		/// <param name="SelInfo"><see cref="SelectInfo"/> structure that describe text selection parameters.</param>
		public void SelectText(SelectInfo SelInfo)
		{
			SelectText(SelInfo.StartPage, SelInfo.StartIndex, SelInfo.EndPage, SelInfo.EndIndex);
		}

		/// <summary>
		/// Selects the text contained in specified pages.
		/// </summary>
		/// <param name="startPage">Zero-based index of a starting page.</param>
		/// <param name="startIndex">Zero-based char index on a startPage.</param>
		/// <param name="endPage">Zero-based index of a ending page.</param>
		/// <param name="endIndex">Zero-based char index on a endPage.</param>
		public void SelectText(int startPage, int startIndex, int endPage, int endIndex)
		{
			if (Document == null)
				return;

			if (startPage > Document.Pages.Count - 1)
				startPage = Document.Pages.Count - 1;
			if (startPage < 0)
				startPage = 0;

			if (endPage > Document.Pages.Count - 1)
				endPage = Document.Pages.Count - 1;
			if (endPage < 0)
				endPage = 0;

			int startCnt = Document.Pages[startPage].Text.CountChars;
			int endCnt = Document.Pages[endPage].Text.CountChars;

			if (startIndex > startCnt - 1)
				startIndex = startCnt - 1;
			if (startIndex < 0)
				startIndex = 0;

			if (endIndex > endCnt - 1)
				endIndex = endCnt - 1;
			if (endIndex < 0)
				endIndex = 0;

			_selectInfo = new SelectInfo()
			{
				StartPage = startPage,
				StartIndex = startIndex,
				EndPage = endPage,
				EndIndex = endIndex
			};
			InvalidateVisual();
			OnSelectionChanged(EventArgs.Empty);
		}

		/// <summary>
		/// Clear text selection
		/// </summary>
		public void DeselectText()
		{
			_selectInfo = new SelectInfo()
			{
				StartPage = -1,
			};
			InvalidateVisual();
			OnSelectionChanged(EventArgs.Empty);
		}

		/// <summary>
		/// Determines if the specified point is contained within Pdf page.
		/// </summary>
		/// <param name="pt">The System.Drawing.Point to test.</param>
		/// <returns>
		/// This method returns the zero based page index if the point represented by pt is contained within this page; otherwise -1.
		/// </returns>
		public int PointInPage(Point pt)
		{
			for (int i = _startPage; i <= _endPage; i++)
			{
				//Actual coordinates of the page with the scroll
				Rect actualRect = CalcActualRect(i);
				if (actualRect.Contains(pt))
					return i;
			}
			return -1;
		}

		/// <summary>
		/// Computes the location of the specified client point into page coordinates.
		/// </summary>
		/// <param name="pageIndex">Page index. Can be obtained by <see cref="PointInPage"/> method.</param>
		/// <param name="pt">The client coordinate Point to convert. </param>
		/// <returns>A Point that represents the converted Point, pt, in page coordinates.</returns>
		/// <exception cref="IndexOutOfRangeException">The page index is out of range</exception>
		/// <remarks>Permitted range of pages depends on the current view type and on some other parameters in the control.</remarks>
		public Point ClientToPage(int pageIndex, Point pt)
		{
			if (pageIndex < _startPage || pageIndex > _endPage)
				throw new IndexOutOfRangeException(Patagames.Pdf.Net.Controls.Wpf.Properties.Resources.err0002);
			var page = Document.Pages[pageIndex];
			var ar = CalcActualRect(pageIndex);
			double pX, pY;
			page.DeviceToPageEx((int)ar.X, (int)ar.Y, (int)ar.Width, (int)ar.Height, PageRotation(page), (int)pt.X, (int)pt.Y, out pX, out pY);
			return new Point(pX, pY);
		}

		/// <summary>
		/// Computes the location of the specified page point into client coordinates.
		/// </summary>
		/// <param name="pageIndex">Page index. Can be obtained by <see cref="PointInPage"/> method.</param>
		/// <param name="pt">The page coordinate Point to convert. </param>
		/// <returns>A Point that represents the converted Point, pt, in client coordinates.</returns>
		/// <exception cref="IndexOutOfRangeException">The page index is out of range</exception>
		/// <remarks>Permitted range of pages depends on the current view type and on some other parameters in the control.</remarks>
		public Point PageToClient(int pageIndex, Point pt)
		{
			if (pageIndex < _startPage || pageIndex > _endPage)
				throw new IndexOutOfRangeException(Patagames.Pdf.Net.Controls.Wpf.Properties.Resources.err0002);
			var page = Document.Pages[pageIndex];
			var ar = CalcActualRect(pageIndex);
			int dX, dY;
			page.PageToDeviceEx((int)ar.X, (int)ar.Y, (int)ar.Width, (int)ar.Height, PageRotation(page), (float)pt.X, (float)pt.Y, out dX, out dY);
			return new Point(dX, dY);
		}


		/// <summary>
		/// Highlight text on the page
		/// </summary>
		/// <param name="pageIndex">Zero-based index of the page</param>
		/// <param name="highlightInfo">Sets the options for highlighting text</param>
		public void HighlightText(int pageIndex, HighlightInfo highlightInfo)
		{
			HighlightText(pageIndex, highlightInfo.CharIndex, highlightInfo.CharsCount, highlightInfo.Color);
		}

		/// <summary>
		/// Highlight text on the page
		/// </summary>
		/// <param name="pageIndex">Zero-based index of the page</param>
		/// <param name="charIndex">Zero-based char index on the page.</param>
		/// <param name="charsCount">The number of highlighted characters on the page or -1 for highlight text from charIndex to end of the page.</param>
		/// <param name="color">Highlight color</param>
		public void HighlightText(int pageIndex, int charIndex, int charsCount, Color color)
		{
			//normalize all user input
			if (pageIndex < 0)
				pageIndex = 0;
			if (pageIndex > Document.Pages.Count - 1)
				pageIndex = Document.Pages.Count - 1;

			int charsCnt = Document.Pages[pageIndex].Text.CountChars;
			if (charIndex < 0)
				charIndex = 0;
			if (charIndex > charsCnt - 1)
				charIndex = charsCnt - 1;
			if (charsCount < 0)
				charsCount = charsCnt - charIndex;
			if (charIndex + charsCount > charsCnt)
				charsCount = charsCnt - 1 - charIndex;
			if (charsCount <= 0)
				return;

			var newEntry = new HighlightInfo() { CharIndex = charIndex, CharsCount = charsCount, Color = color };

			if (!_highlightedText.ContainsKey(pageIndex))
			{
				if (color != Helpers.ColorEmpty)
				{
					_highlightedText.Add(pageIndex, new List<HighlightInfo>());
					_highlightedText[pageIndex].Add(newEntry);
				}
			}
			else
			{
				var entries = _highlightedText[pageIndex];
				//Analize exists entries and remove overlapped and trancate intersecting entries
				for (int i = entries.Count - 1; i >= 0; i--)
				{
					List<HighlightInfo> calcEntries;
					if (CalcIntersectEntries(entries[i], newEntry, out calcEntries))
					{
						if (calcEntries.Count == 0)
							entries.RemoveAt(i);
						else
							for (int j = 0; j < calcEntries.Count; j++)
								if (j == 0)
									entries[i] = calcEntries[j];
								else
									entries.Insert(i, calcEntries[j]);
					}
				}
				if (color != Helpers.ColorEmpty)
					entries.Add(newEntry);
			}

			InvalidateVisual();
			OnHighlightedTextChanged(EventArgs.Empty);
		}

		/// <summary>
		/// Removes highlight from the text
		/// </summary>
		public void RemoveHighlightFromText()
		{
			_highlightedText.Clear();
			InvalidateVisual();
		}

		/// <summary>
		/// Removes highlight from the text
		/// </summary>
		/// <param name="pageIndex">Zero-based index of the page</param>
		/// <param name="charIndex">Zero-based char index on the page.</param>
		/// <param name="charsCount">The number of highlighted characters on the page or -1 for highlight text from charIndex to end of the page.</param>
		public void RemoveHighlightFromText(int pageIndex, int charIndex, int charsCount)
		{
			HighlightText(pageIndex, charIndex, charsCount, Helpers.ColorEmpty);
		}

		/// <summary>
		/// Highlight selected text on the page by specified color
		/// </summary>
		/// <param name="color">Highlight color</param>
		public void HilightSelectedText(Color color)
		{
			var selInfo = SelectInfo;
			if (selInfo.StartPage < 0 || selInfo.StartIndex < 0)
				return;

			for (int i = selInfo.StartPage; i <= selInfo.EndPage; i++)
			{
				int start = (i == selInfo.StartPage ? selInfo.StartIndex : 0);
				int len = (i == selInfo.EndPage ? (selInfo.EndIndex + 1) - start : -1);
				HighlightText(i, start, len, color);
			}
		}

		/// <summary>
		/// Removes highlight from selected text
		/// </summary>
		public void RemoveHilightFromSelectedText()
		{
			HilightSelectedText(Helpers.ColorEmpty);
		}

		/// <summary>
		/// Ensures that all sizes and positions of pages of a PdfViewer control are properly updated for layout.
		/// </summary>
		public void UpdateDocLayout()
		{
			double w = ActualWidth;
			double h = ActualHeight;
			if (w > 0 && h > 0)
				MeasureOverride(new Size(w, h));
			InvalidateVisual();
		}

		/// <summary>
		/// Calculates the actual rectangle of the specified page in client coordinates
		/// </summary>
		/// <param name="index">Zero-based page index</param>
		/// <returns>Calculated rectangle</returns>
		public Rect CalcActualRect(int index)
		{
			var rect = renderRects(index);
			rect.X += _autoScrollPosition.X;
			rect.Y += _autoScrollPosition.Y;
			return rect;
		}
		#endregion

		#region Load and Close document
		/// <summary>
		/// Open and load a PDF document from a file.
		/// </summary>
		/// <param name="path">Path to the PDF file (including extension)</param>
		/// <param name="password">A string used as the password for PDF file. If no password needed, empty or NULL can be used.</param>
		/// <exception cref="UnknownErrorException">unknown error</exception>
		/// <exception cref="PdfFileNotFoundException">file not found or could not be opened</exception>
		/// <exception cref="BadFormatException">file not in PDF format or corrupted</exception>
		/// <exception cref="InvalidPasswordException">password required or incorrect password</exception>
		/// <exception cref="UnsupportedSecuritySchemeException">unsupported security scheme</exception>
		/// <exception cref="PdfiumException">Error occured in PDFium. See ErrorCode for detail</exception>
		/// <exception cref="Exceptions.NoLicenseException">This exception thrown only in trial mode if document cannot be opened due to a license restrictions"</exception>
		/// <remarks>
		/// <note type="note">
		/// With the trial version the documents which size is smaller than 1024 Kb, or greater than 10 Mb can be loaded without any restrictions. For other documents the allowed ranges is 1.5 - 2 Mb; 2.5 - 3 Mb; 3.5 - 4 Mb; 4.5 - 5 Mb and so on.
		/// </note> 
		/// </remarks>
		public void LoadDocument(string path, string password = null)
		{
			try {
				Pdfium.FPDF_ShowSplash(true);
				CloseDocument();
				_document = PdfDocument.Load(path, _fillForms, password);
				UpdateDocLayout();
				_document.Pages.CurrentPageChanged += Pages_CurrentPageChanged;
				_document.Pages.PageInserted += Pages_PageInserted;
				_document.Pages.PageDeleted += Pages_PageDeleted;
				SetCurrentPage(_onstartPageIndex);
				ScrollToPage(_onstartPageIndex);
				OnDocumentLoaded(EventArgs.Empty);
			}
			catch (NoLicenseException ex)
			{
				MessageBox.Show(ex.Message, Properties.Resources.InfoHeader, MessageBoxButton.OK, MessageBoxImage.Information);
			}
		}

		/// <summary>
		/// Loads the PDF document from the specified stream.
		/// </summary>
		/// <param name="stream">The stream containing the PDF document to load. The stream must support seeking.</param>
		/// <param name="password">A string used as the password for PDF file. If no password needed, empty or NULL can be used.</param>
		/// <exception cref="UnknownErrorException">unknown error</exception>
		/// <exception cref="PdfFileNotFoundException">file not found or could not be opened</exception>
		/// <exception cref="BadFormatException">file not in PDF format or corrupted</exception>
		/// <exception cref="InvalidPasswordException">password required or incorrect password</exception>
		/// <exception cref="UnsupportedSecuritySchemeException">unsupported security scheme</exception>
		/// <exception cref="PdfiumException">Error occured in PDFium. See ErrorCode for detail</exception>
		/// <exception cref="Exceptions.NoLicenseException">This exception thrown only in trial mode if document cannot be opened due to a license restrictions"</exception>
		/// <remarks>
		/// <note type="note">
		/// <para>The application should maintain the stream resources being valid until the PDF document close.</para>
		/// <para>With the trial version the documents which size is smaller than 1024 Kb, or greater than 10 Mb can be loaded without any restrictions. For other documents the allowed ranges is 1.5 - 2 Mb; 2.5 - 3 Mb; 3.5 - 4 Mb; 4.5 - 5 Mb and so on.</para>
		/// </note> 
		/// </remarks>
		public void LoadDocument(Stream stream, string password = null)
		{
			try {
				Pdfium.FPDF_ShowSplash(true);
				CloseDocument();
				_document = PdfDocument.Load(stream, _fillForms, password);
				UpdateDocLayout();
				_document.Pages.CurrentPageChanged += Pages_CurrentPageChanged;
				_document.Pages.PageInserted += Pages_PageInserted;
				_document.Pages.PageDeleted += Pages_PageDeleted;
				SetCurrentPage(_onstartPageIndex);
				ScrollToPage(_onstartPageIndex);
				OnDocumentLoaded(EventArgs.Empty);
			}
			catch (NoLicenseException ex)
			{
				MessageBox.Show(ex.Message, Properties.Resources.InfoHeader, MessageBoxButton.OK, MessageBoxImage.Information);
			}
		}

		/// <summary>
		/// Loads the PDF document from the specified byte array.
		/// </summary>
		/// <param name="pdf">The byte array containing the PDF document to load.</param>
		/// <param name="password">A string used as the password for PDF file. If no password needed, empty or NULL can be used.</param>
		/// <exception cref="UnknownErrorException">unknown error</exception>
		/// <exception cref="PdfFileNotFoundException">file not found or could not be opened</exception>
		/// <exception cref="BadFormatException">file not in PDF format or corrupted</exception>
		/// <exception cref="InvalidPasswordException">password required or incorrect password</exception>
		/// <exception cref="UnsupportedSecuritySchemeException">unsupported security scheme</exception>
		/// <exception cref="PdfiumException">Error occured in PDFium. See ErrorCode for detail</exception>
		/// <exception cref="Exceptions.NoLicenseException">This exception thrown only in trial mode if document cannot be opened due to a license restrictions"</exception>
		/// <remarks>
		/// <note type="note">
		/// <para>The application should maintain the byte array being valid until the PDF document close.</para>
		/// <para>With the trial version the documents which size is smaller than 1024 Kb, or greater than 10 Mb can be loaded without any restrictions. For other documents the allowed ranges is 1.5 - 2 Mb; 2.5 - 3 Mb; 3.5 - 4 Mb; 4.5 - 5 Mb and so on.</para>
		/// </note> 
		/// </remarks>
		public void LoadDocument(byte[] pdf, string password = null)
		{
			try {
				Pdfium.FPDF_ShowSplash(true);
				CloseDocument();
				_document = PdfDocument.Load(pdf, _fillForms, password);
				UpdateDocLayout();
				_document.Pages.CurrentPageChanged += Pages_CurrentPageChanged;
				_document.Pages.PageInserted += Pages_PageInserted;
				_document.Pages.PageDeleted += Pages_PageDeleted;
				SetCurrentPage(_onstartPageIndex);
				ScrollToPage(_onstartPageIndex);
				OnDocumentLoaded(EventArgs.Empty);
			}
			catch (NoLicenseException ex)
			{
				MessageBox.Show(ex.Message, Properties.Resources.InfoHeader, MessageBoxButton.OK, MessageBoxImage.Information);
			}
		}

		/// <summary>
		/// Close a loaded PDF document.
		/// </summary>
		public void CloseDocument()
		{
			if (_document != null)
			{
				DeselectText();
				_document.Dispose();
				_document = null;
				OnDocumentClosed(EventArgs.Empty);
			}
			_renderRects = null;
			_document = null;
			_onstartPageIndex = 0;
			if (ScrollOwner != null)
				ScrollOwner.InvalidateScrollInfo();
			InvalidateVisual();

		}
		#endregion

		#region Constructors and initialization
		/// <summary>
		/// Initializes a new instance of the PdfViewer class.
		/// </summary>
		public PdfViewer()
		{
			Background = SystemColors.ControlDarkBrush;
			PageBackColor = Color.FromArgb(255, 255, 255, 255);
			PageBorderColor = Color.FromArgb(255, 0, 0, 0);
			FormHighlightColor = Color.FromArgb(0, 255, 255, 255);
			TextSelectColor = Color.FromArgb(70, 70, 130, 180);
			Zoom = 1;
			PageMargin = new Thickness(10);
			ViewMode = ViewModes.Vertical;
			ShowPageSeparator = true;
			PageSeparatorColor = Color.FromArgb(255, 190, 190, 190);
			CurrentPageHighlightColor = Color.FromArgb(170, 70, 130, 180);
			ShowCurrentPageHighlight = true;
			PageVAlign = VerticalAlignment.Center;
			PageHAlign = HorizontalAlignment.Center;
			RenderFlags = RenderFlags.FPDF_ANNOT;
			TilesCount = 2;

			InitializeComponent();

			_fillForms = new PdfForms();
			_fillForms.SynchronizingObject = new DispatcherISyncInvoke(Dispatcher);
			_fillForms.SetHighlightColorEx(FormFieldTypes.FPDF_FORMFIELD_UNKNOWN, Helpers.ToArgb(_formHighlightColor));
			_fillForms.AppBeep += FormsAppBeep;
			_fillForms.DoGotoAction += FormsDoGotoAction;
			_fillForms.DoNamedAction += FormsDoNamedAction;
			_fillForms.GotoPage += FormsGotoPage;
			_fillForms.Invalidate += FormsInvalidate;
			_fillForms.OutputSelectedRect += FormsOutputSelectedRect;
			_fillForms.SetCursor += FormsSetCursor;
			_fillForms.FocusChanged += FormsFocusChanged;
		}
		#endregion

		#region Overrides

		/// <summary>
		/// Gets the number of visual child elements within this element.
		/// </summary>
		protected override int VisualChildrenCount
		{
			get
			{
				return 0;
			}
		}

		/// <summary>
		/// Called to remeasure a control.
		/// </summary>
		/// <param name="availableSize">The maximum size that the method can return.</param>
		/// <returns>The size of the control, up to the maximum specified by availableSize.</returns>
		/// <remarks>The default control measurement only measures the first visual child.</remarks>
		protected override Size MeasureOverride(Size availableSize)
		{
			if (Document != null)
			{
				Size size = CalcPages();

				if (size != _extent)
				{
					_extent = size;
					if (ScrollOwner != null)
						ScrollOwner.InvalidateScrollInfo();
				}

				if (availableSize != _viewport)
				{
					_viewport = availableSize;
					if (ScrollOwner != null)
						ScrollOwner.InvalidateScrollInfo();
				}
			}

			if (double.IsInfinity(availableSize.Width)
				|| double.IsInfinity(availableSize.Height)
				)
			{
				return base.MeasureOverride(availableSize);
			}
			return availableSize;
		}

		/// <summary>
		/// Called to arrange and size the content of a Control object.
		/// </summary>
		/// <param name="finalSize">The computed size that is used to arrange the content.</param>
		/// <returns>The size of the control.</returns>
		/// <remarks>The default control arrangement arranges only the first visual child. No transforms are applied.</remarks>
		protected override Size ArrangeOverride(Size finalSize)
		{
			MeasureOverride(finalSize);
			return finalSize;
		}

		/// <summary>
		/// When overridden in a derived class, participates in rendering operations that are directed by the layout system. 
		/// The rendering instructions for this element are not used directly when this method is invoked, and are instead 
		/// preserved for later asynchronous use by layout and drawing.
		/// </summary>
		/// <param name="drawingContext">The drawing instructions for a specific element. This context is provided to the layout system. </param>
		/// <remarks>
		/// The OnRender method can be overridden to add further graphical elements (not previously defined in a logical tree) to a rendered element, such as effects or adorners. A DrawingContext object is passed as an argument, which provides methods for drawing shapes, text, images or videos.
		/// </remarks>
		protected override void OnRender(DrawingContext drawingContext)
		{
			Helpers.FillRectangle(drawingContext, Background, ClientRect);

			if (Document != null && _renderRects != null)
			{
				//Normalize info about text selection
				SelectInfo selTmp = NormalizeSelectionInfo();

				//For store coordinates of pages separators
				var separator = new List<Point>();

				//starting draw pages in vertical or horizontal modes
				for (int i = _startPage; i <= _endPage; i++)
				{
					//Actual coordinates of the page with the scroll
					Rect actualRect = CalcActualRect(i);
					if (!actualRect.IntersectsWith(ClientRect))
					{
						Document.Pages[i].Dispose();
						continue; //Page is invisible. Skip it
					}

					//Draw page
					DrawPage(drawingContext, Document.Pages[i], actualRect);
					//Draw fillforms selection
					DrawFillFormsSelection(drawingContext);
					//Draw text highlight
					if (_highlightedText.ContainsKey(i))
						DrawTextHighlight(drawingContext, _highlightedText[i], i);
					//Draw text selectionn
					DrawTextSelection(drawingContext, selTmp, i);
					//Draw current page highlight
					DrawCurrentPageHighlight(drawingContext, i, actualRect);
					//Calc coordinates for page separator
					CalcPageSeparator(actualRect, i, ref separator);
				}

				//Draw pages separators
				DrawPageSeparators(drawingContext, ref separator);

				_selectedRectangles.Clear();
			}
		}

		/// <summary>
		/// Raises the MouseDoubleClick event.
		/// </summary>
		/// <param name="e">A System.Windows.Forms.MouseButtonEventArgs that contains the event data.</param>
		protected override void OnMouseDoubleClick(MouseButtonEventArgs e)
		{
			if (e.LeftButton == MouseButtonState.Pressed)
			{
				if (Document != null)
				{
					Point page_point;
					var loc = e.GetPosition(this);
					int idx = DeviceToPage(loc.X, loc.Y, out page_point);
					if (idx >= 0)
					{
						switch (MouseMode)
						{
							case MouseModes.Default:
							case MouseModes.SelectTextTool:
								ProcessMouseDoubleClickForSelectTextTool(page_point, idx);
								break;
						}
					}
				}
			}

			base.OnMouseDoubleClick(e);
		}


		/// <summary>
		/// Raises the System.Windows.Forms.Control.MouseDown event.
		/// </summary>
		/// <param name="e">A System.Windows.Forms.MouseEventArgs that contains the event data.</param>
		protected override void OnMouseDown(MouseButtonEventArgs e)
		{
			if (e.LeftButton == MouseButtonState.Pressed)
			{
				if (Document != null)
				{
					Point page_point;
					var loc = e.GetPosition(this);
					int idx = DeviceToPage(loc.X, loc.Y, out page_point);
					if (idx >= 0)
					{
						Document.Pages[idx].OnLButtonDown(0, (float)page_point.X, (float)page_point.Y);
						SetCurrentPage(idx);

						_mousePressed = true;

						switch (MouseMode)
						{
							case MouseModes.Default:
								ProcessMouseDownDefaultTool(page_point, idx);
								ProcessMouseDownForSelectTextTool(page_point, idx);
								break;
							case MouseModes.SelectTextTool:
								ProcessMouseDownForSelectTextTool(page_point, idx);
								break;
							case MouseModes.PanTool:
								ProcessMouseDownPanTool(loc);
								break;

						}
						InvalidateVisual();
					}
				}
			}

			base.OnMouseDown(e);
		}

		/// <summary>
		/// Raises the System.Windows.Forms.Control.MouseMove event.
		/// </summary>
		/// <param name="e">A System.Windows.Forms.MouseEventArgs that contains the event data.</param>
		protected override void OnMouseMove(MouseEventArgs e)
		{
			if (Document != null)
			{
				Point page_point;
				var loc = e.GetPosition(this);
				int idx = DeviceToPage(loc.X, loc.Y, out page_point);

				if (idx >= 0)
				{
					int ei = Document.Pages[idx].Text.GetCharIndexAtPos((float)page_point.X, (float)page_point.Y, 10.0f, 10.0f);

					if (!Document.Pages[idx].OnMouseMove(0, (float)page_point.X, (float)page_point.Y))
					{
						if (ei >= 0 && (MouseMode == MouseModes.SelectTextTool || MouseMode == MouseModes.Default))
							Mouse.OverrideCursor = Cursors.IBeam;
						else
							Mouse.OverrideCursor = null;
					}

					switch (MouseMode)
					{
						case MouseModes.Default:
							ProcessMouseMoveForDefaultTool(page_point, idx);
							ProcessMouseMoveForSelectTextTool(idx, ei);
							break;
						case MouseModes.SelectTextTool:
							ProcessMouseMoveForSelectTextTool(idx, ei);
							break;
						case MouseModes.PanTool:
							ProcessMouseMoveForPanTool(loc);
							break;
					}
				}
			}

			base.OnMouseMove(e);
		}

		/// <summary>
		/// Raises the System.Windows.Forms.Control.MouseUp event.
		/// </summary>
		/// <param name="e">A System.Windows.Forms.MouseEventArgs that contains the event data.</param>
		protected override void OnMouseUp(MouseButtonEventArgs e)
		{
			if (!IsFocused)
			{
				_isProgrammaticallyFocusSetted = true;
				Keyboard.Focus(this);
			}

			_mousePressed = false;
			if (Document != null)
			{
				if (_selectInfo.StartPage >= 0)
					OnSelectionChanged(EventArgs.Empty);

				Point page_point;
				var loc = e.GetPosition(this);
				int idx = DeviceToPage(loc.X, loc.Y, out page_point);
				if (idx >= 0)
				{
					Document.Pages[idx].OnLButtonUp(0, (float)page_point.X, (float)page_point.Y);

					switch (MouseMode)
					{
						case MouseModes.Default:
							PriocessMouseUpForDefaultTool(page_point, idx);
							break;
						case MouseModes.PanTool:
							ProcessMouseUpPanTool(loc);
							break;
					}
				}
			}

			base.OnMouseUp(e);
		}

		/// <summary>
		/// Invoked when an unhandled System.Windows.Input.Keyboard.PreviewKeyDown attached
		/// event reaches an element in its route that is derived from this class. Implement
		///  this method to add class handling for this event.
		/// </summary>
		/// <param name="e">The System.Windows.Input.KeyEventArgs that contains the event data.</param>
		protected override void OnPreviewKeyDown(KeyEventArgs e)
		{
			if (Document != null)
			{
				var formsKey = (FWL_VKEYCODE)KeyInterop.VirtualKeyFromKey(e.Key);

				KeyboardModifiers mod = (KeyboardModifiers)0;

				if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
					mod |= KeyboardModifiers.ControlKey;
				if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
					mod |= KeyboardModifiers.ShiftKey;
				if (Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt))
					mod |= KeyboardModifiers.AltKey;

				if(Document.Pages.CurrentPage.OnKeyDown(formsKey, mod))
					e.Handled = true;
			}
			base.OnPreviewKeyDown(e);
		}

		/// <summary>
		/// Raises the System.Windows.Forms.Control.KeyUp event.
		/// </summary>
		/// <param name="e">A System.Windows.Forms.KeyEventArgs that contains the event data.</param>
		protected override void OnKeyUp(KeyEventArgs e)
		{
			if (Document != null)
			{
				KeyboardModifiers mod = (KeyboardModifiers)0;
				if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
					mod |= KeyboardModifiers.ControlKey;
				if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
					mod |= KeyboardModifiers.ShiftKey;
				if (Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt))
					mod |= KeyboardModifiers.AltKey;
				Document.Pages.CurrentPage.OnKeyUp((FWL_VKEYCODE)e.Key, mod);
			}
			base.OnKeyUp(e);
		}

		#endregion

		#region protected drawing functuions
		/// <summary>
		/// Draws page background
		/// </summary>
		/// <param name="bmp"><see cref="PdfBitmap"/> object</param>
		/// <param name="width">Actual width of the page</param>
		/// <param name="height">Actual height of the page</param>
		/// <remarks>
		/// Full page rendering is performed in the following order:
		/// <list type="bullet">
		/// <item><see cref="DrawPageBackColor"/></item>
		/// <item><see cref="DrawPage"/></item>
		/// <item><see cref="DrawFillFormsSelection"/></item>
		/// <item><see cref="DrawTextHighlight"/></item>
		/// <item><see cref="DrawTextSelection"/></item>
		/// <item><see cref="DrawCurrentPageHighlight"/></item>
		/// <item><see cref="DrawPageSeparators"/></item>
		/// </list>
		/// </remarks>
		protected virtual void DrawPageBackColor(PdfBitmap bmp, int width, int height)
		{
			bmp.FillRectEx(0, 0, width, height, Helpers.ToArgb(PageBackColor));
		}

		/// <summary>
		/// Draws page content and fillforms
		/// </summary>
		/// <param name="drawingContext">The drawing instructions for a specific element. This context is provided to the layout system.</param>
		/// <param name="page">Page to be drawn</param>
		/// <param name="actualRect">Page bounds in control coordinates</param>
		/// <remarks>
		/// Full page rendering is performed in the following order:
		/// <list type="bullet">
		/// <item><see cref="DrawPageBackColor"/></item>
		/// <item><see cref="DrawPage"/></item>
		/// <item><see cref="DrawFillFormsSelection"/></item>
		/// <item><see cref="DrawTextHighlight"/></item>
		/// <item><see cref="DrawTextSelection"/></item>
		/// <item><see cref="DrawCurrentPageHighlight"/></item>
		/// <item><see cref="DrawPageSeparators"/></item>
		/// </list>
		/// </remarks>
		protected virtual void DrawPage(DrawingContext drawingContext, PdfPage page, Rect actualRect)
		{
			if (actualRect.Width <= 0 || actualRect.Height <= 0)
				return;
			using (PdfBitmap bmp = new PdfBitmap((int)actualRect.Width, (int)actualRect.Height, true))
			{
				//Draw background to bitmap
				DrawPageBackColor(bmp, (int)actualRect.Width, (int)actualRect.Height);

				//Draw page content to bitmap
				page.RenderEx(bmp, 0, 0, (int)actualRect.Width, (int)actualRect.Height, PageRotation(page), RenderFlags);

				if (_invalidatePage != null && _invalidatePage == page)
				{
					int pt1X, pt2X, pt1Y, pt2Y;
					page.PageToDeviceEx(0, 0, (int)actualRect.Width, (int)actualRect.Height, PageRotation(page), _invalidateRect.left, _invalidateRect.top, out pt1X, out pt1Y);
					page.PageToDeviceEx(0, 0, (int)actualRect.Width, (int)actualRect.Height, PageRotation(page), _invalidateRect.right, _invalidateRect.bottom, out pt2X, out pt2Y);
					bmp.FillRectEx(pt1X, pt1Y, pt2X - pt1X, pt2Y - pt1Y, Helpers.ToArgb(PageBackColor));
				}

				//Draw fillforms to bitmap
				page.RenderForms(bmp, 0, 0, (int)actualRect.Width, (int)actualRect.Height, PageRotation(page), RenderFlags);

				//Draw bitmap to drawing surface
				Helpers.DrawImageUnscaled(drawingContext, bmp, actualRect.X, actualRect.Y);

				//Draw page border
				Helpers.DrawRectangle(drawingContext, _pageBorderColorPen, actualRect);
			}
		}

		/// <summary>
		/// Draws highlights inside a forms
		/// </summary>
		/// <param name="drawingContext">The drawing instructions for a specific element. This context is provided to the layout system.</param>
		/// <remarks>
		/// Full page rendering is performed in the following order:
		/// <list type="bullet">
		/// <item><see cref="DrawPageBackColor"/></item>
		/// <item><see cref="DrawPage"/></item>
		/// <item><see cref="DrawFillFormsSelection"/></item>
		/// <item><see cref="DrawTextHighlight"/></item>
		/// <item><see cref="DrawTextSelection"/></item>
		/// <item><see cref="DrawCurrentPageHighlight"/></item>
		/// <item><see cref="DrawPageSeparators"/></item>
		/// </list>
		/// </remarks>
		protected virtual void DrawFillFormsSelection(DrawingContext drawingContext)
		{
			foreach (var selectRc in _selectedRectangles)
				Helpers.FillRectangle(drawingContext, _selectColorBrush, selectRc);
		}

		/// <summary>
		/// Draws text highlights
		/// </summary>
		/// <param name="drawingContext">The drawing instructions for a specific element. This context is provided to the layout system.</param>
		/// <param name="entries">Highlights info.</param>
		/// <param name="pageIndex">Page index to be drawn</param>
		/// <remarks>
		/// Full page rendering is performed in the following order:
		/// <list type="bullet">
		/// <item><see cref="DrawPageBackColor"/></item>
		/// <item><see cref="DrawPage"/></item>
		/// <item><see cref="DrawFillFormsSelection"/></item>
		/// <item><see cref="DrawTextHighlight"/></item>
		/// <item><see cref="DrawTextSelection"/></item>
		/// <item><see cref="DrawCurrentPageHighlight"/></item>
		/// <item><see cref="DrawPageSeparators"/></item>
		/// </list>
		/// </remarks>
		protected virtual void DrawTextHighlight(DrawingContext drawingContext, List<HighlightInfo> entries, int pageIndex)
		{
			foreach (var e in entries)
			{
				var textInfo = Document.Pages[pageIndex].Text.GetTextInfo(e.CharIndex, e.CharsCount);
				foreach (var rc in textInfo.Rects)
				{
					var pt1 = PageToDevice(rc.left, rc.top, pageIndex);
					var pt2 = PageToDevice(rc.right, rc.bottom, pageIndex);
					double x = pt1.X < pt2.X ? pt1.X : pt2.X;
					double y = pt1.Y < pt2.Y ? pt1.Y : pt2.Y;
					double w = pt1.X > pt2.X ? pt1.X - pt2.X : pt2.X - pt1.X;
					double h = pt1.Y > pt2.Y ? pt1.Y - pt2.Y : pt2.Y - pt1.Y;
					Helpers.FillRectangle(drawingContext, e.Brush, Helpers.CreateRect(x, y, w, h));
				}
			}
		}

		/// <summary>
		/// Draws text selection
		/// </summary>
		/// <param name="drawingContext">The drawing instructions for a specific element. This context is provided to the layout system.</param>
		/// <param name="selInfo">Selection info</param>
		/// <param name="pageIndex">Page index to be drawn</param>
		/// <remarks>
		/// Full page rendering is performed in the following order:
		/// <list type="bullet">
		/// <item><see cref="DrawPageBackColor"/></item>
		/// <item><see cref="DrawPage"/></item>
		/// <item><see cref="DrawFillFormsSelection"/></item>
		/// <item><see cref="DrawTextHighlight"/></item>
		/// <item><see cref="DrawTextSelection"/></item>
		/// <item><see cref="DrawCurrentPageHighlight"/></item>
		/// <item><see cref="DrawPageSeparators"/></item>
		/// </list>
		/// </remarks>
		protected virtual void DrawTextSelection(DrawingContext drawingContext, SelectInfo selInfo, int pageIndex)
		{
			if (selInfo.StartPage < 0)
				return;

			if (pageIndex >= selInfo.StartPage && pageIndex <= selInfo.EndPage)
			{
				int s = 0;
				if (pageIndex == selInfo.StartPage)
					s = selInfo.StartIndex;

				int len = Document.Pages[pageIndex].Text.CountChars;
				if (pageIndex == selInfo.EndPage)
					len = (selInfo.EndIndex + 1) - s;

				var ti = Document.Pages[pageIndex].Text.GetTextInfo(s, len);
				foreach (var rc in ti.Rects)
				{
					var pt1 = PageToDevice(rc.left, rc.top, pageIndex);
					var pt2 = PageToDevice(rc.right, rc.bottom, pageIndex);

					double x = pt1.X < pt2.X ? pt1.X : pt2.X;
					double y = pt1.Y < pt2.Y ? pt1.Y : pt2.Y;
					double w = pt1.X > pt2.X ? pt1.X - pt2.X : pt2.X - pt1.X;
					double h = pt1.Y > pt2.Y ? pt1.Y - pt2.Y : pt2.Y - pt1.Y;

					Helpers.FillRectangle(drawingContext, _selectColorBrush, Helpers.CreateRect(x, y, w, h));
				}
			}
		}

		/// <summary>
		/// Draws current page highlight
		/// </summary>
		/// <param name="drawingContext">The drawing instructions for a specific element. This context is provided to the layout system.</param>
		/// <param name="pageIndex">Page index to be drawn</param>
		/// <param name="actualRect">Page bounds in control coordinates</param>
		/// <remarks>
		/// Full page rendering is performed in the following order:
		/// <list type="bullet">
		/// <item><see cref="DrawPageBackColor"/></item>
		/// <item><see cref="DrawPage"/></item>
		/// <item><see cref="DrawFillFormsSelection"/></item>
		/// <item><see cref="DrawTextHighlight"/></item>
		/// <item><see cref="DrawTextSelection"/></item>
		/// <item><see cref="DrawCurrentPageHighlight"/></item>
		/// <item><see cref="DrawPageSeparators"/></item>
		/// </list>
		/// </remarks>
		protected virtual void DrawCurrentPageHighlight(DrawingContext drawingContext, int pageIndex, Rect actualRect)
		{
			if (ShowCurrentPageHighlight && pageIndex == Document.Pages.CurrentIndex)
			{
				actualRect.Inflate(0, 0);
				Helpers.DrawRectangle(drawingContext, _currentPageHighlightColorPen, actualRect);
			}
		}

		/// <summary>
		/// Draws pages separatoes.
		/// </summary>
		/// <param name="drawingContext">The drawing instructions for a specific element. This context is provided to the layout system.</param>
		/// <param name="separator">List of pair of points what represents separator</param>
		/// <remarks>
		/// Full page rendering is performed in the following order:
		/// <list type="bullet">
		/// <item><see cref="DrawPageBackColor"/></item>
		/// <item><see cref="DrawPage"/></item>
		/// <item><see cref="DrawFillFormsSelection"/></item>
		/// <item><see cref="DrawTextHighlight"/></item>
		/// <item><see cref="DrawTextSelection"/></item>
		/// <item><see cref="DrawCurrentPageHighlight"/></item>
		/// <item><see cref="DrawPageSeparators"/></item>
		/// </list>
		/// </remarks>
		protected virtual void DrawPageSeparators(DrawingContext drawingContext, ref List<Point> separator)
		{
			for (int sep = 0; sep < separator.Count; sep += 2)
				drawingContext.DrawLine(_pageSeparatorColorPen, separator[sep], separator[sep + 1]);
		}

		#endregion

		#region Private methods
		private void ProcessLinkClicked(PdfLink pdfLink, PdfWebLink webLink)
		{
			var args = new PdfBeforeLinkClickedEventArgs(webLink, pdfLink);
			OnBeforeLinkClicked(args);
			if (args.Cancel)
				return;
			if (pdfLink != null && pdfLink.Destination != null)
				ProcessDestination(pdfLink.Destination);
			else if (pdfLink != null && pdfLink.Action != null)
				ProcessAction(pdfLink.Action);
			else if (webLink != null)
				Process.Start(webLink.Url);
			OnAfterLinkClicked(new PdfAfterLinkClickedEventArgs(webLink, pdfLink));

		}

		private void ProcessDestination(PdfDestination pdfDestination)
		{
			ScrollToPage(pdfDestination.PageIndex);
			InvalidateVisual();
		}

		private void ProcessAction(PdfAction pdfAction)
		{
			if (pdfAction.ActionType == ActionTypes.Uri)
				Process.Start(pdfAction.ActionUrl);
			else if (pdfAction.Destination != null)
				ProcessDestination(pdfAction.Destination);
		}

		private int CalcCurrentPage()
		{
			int idx = -1;
			double maxArea = 0;
			for (int i = _startPage; i <= _endPage; i++)
			{
				var page = Document.Pages[i];

				var rect = renderRects(i);
				rect.X += _autoScrollPosition.X;
				rect.Y += _autoScrollPosition.Y;
				if (!rect.IntersectsWith(ClientRect))
					continue;

				rect.Intersect(ClientRect);

				double area = rect.Width * rect.Height;
				if (maxArea < area)
				{
					maxArea = area;
					idx = i;
				}
			}
			return idx;
		}

		private void CalcPageSeparator(Rect actualRect, int pageIndex, ref List<Point> separator)
		{
			if (!ShowPageSeparator || pageIndex == _endPage || ViewMode == ViewModes.SinglePage)
				return;
			switch (ViewMode)
			{
				case ViewModes.Vertical:
					separator.Add(new Point(actualRect.X, actualRect.Bottom + PageMargin.Bottom));
					separator.Add(new Point(actualRect.Right, actualRect.Bottom + PageMargin.Bottom));
					break;
				case ViewModes.Horizontal:
					separator.Add(new Point(actualRect.Right + PageMargin.Right, actualRect.Top));
					separator.Add(new Point(actualRect.Right + PageMargin.Right, actualRect.Bottom));
					break;
				case ViewModes.TilesVertical:
					if ((pageIndex + 1) % TilesCount != 0)
					{
						//vertical
						separator.Add(new Point(actualRect.Right + PageMargin.Right, actualRect.Top));
						separator.Add(new Point(actualRect.Right + PageMargin.Right, actualRect.Bottom));
					}
					if (pageIndex <= _endPage - TilesCount)
					{
						//horizontal
						separator.Add(new Point(actualRect.X, actualRect.Bottom + PageMargin.Bottom));
						separator.Add(new Point(actualRect.Right, actualRect.Bottom + PageMargin.Bottom));
					}
					break;
			}
		}


		private Rect GetRenderRect(int index)
		{
			Size size = GetRenderSize(index);
			Point location = GetRenderLocation(size);
			return Helpers.CreateRect(location, size);
		}

		private Point GetRenderLocation(Size size)
		{
			var clientSize = ClientRect;
			double xcenter = (clientSize.Width - size.Width) / 2;
			double ycenter = (clientSize.Height - size.Height) / 2;
			double xright = clientSize.Width - size.Width;
			double ybottom = clientSize.Height - size.Height;

			if (xcenter < 0)
				xcenter = 0;
			if (ycenter < 0)
				ycenter = 0;

			double x = xcenter;
			double y = ycenter;

			switch (PageVAlign)
			{
				case VerticalAlignment.Bottom: y = ybottom; break;
				case VerticalAlignment.Top: y = 0; break;
			}

			switch (PageHAlign)
			{
				case HorizontalAlignment.Left: x = 0; break;
				case HorizontalAlignment.Right: x = xright; break;
			}
			return new Point(x, y);
		}

		private Size GetRenderSize(int index)
		{
			double w, h;
			Pdfium.FPDF_GetPageSizeByIndex(Document.Handle, index, out w, out h);

			var clientSize = ClientRect;
			double nw = clientSize.Width;
			double nh = h * nw / w;

			switch (SizeMode)
			{
				case SizeModes.FitToHeight:
					nh = clientSize.Height;
					nw = w * nh / h;
					break;
				case SizeModes.FitToSize:
					nh = clientSize.Height;
					nw = w * nh / h;
					if (nw > clientSize.Width)
					{
						nw = clientSize.Width;
						nh = h * nw / w;
					}
					break;
				case SizeModes.Zoom:
					nw = w * Zoom;
					nh = h * Zoom;
					break;
			}

			return Helpers.CreateSize(nw, nh);
		}

		private int DeviceToPage(double x, double y, out Point pagePoint)
		{
			for (int i = _startPage; i <= _endPage; i++)
			{
				var rect = _renderRects[i];
				rect.X += _autoScrollPosition.X;
				rect.Y += _autoScrollPosition.Y;
				if (!rect.Contains(x, y))
					continue;

				double px, py;
				Document.Pages[i].DeviceToPageEx(
					(int)rect.X, (int)rect.Y,
					(int)rect.Width, (int)rect.Height,
					PageRotation(Document.Pages[i]), (int)x, (int)y,
					out px, out py);
				pagePoint = new Point(px, py);
				return i;
			}
			pagePoint = new Point(0, 0);
			return -1;

		}

		private Point PageToDevice(double x, double y, int pageIndex)
		{
			var rect = renderRects(pageIndex);
			rect.X += _autoScrollPosition.X;
			rect.Y += _autoScrollPosition.Y;

			int dx, dy;
			Document.Pages[pageIndex].PageToDeviceEx(
					(int)rect.X, (int)rect.Y,
					(int)rect.Width, (int)rect.Height,
					PageRotation(Document.Pages[pageIndex]),
					(float)x, (float)y,
					out dx, out dy);
			return new Point(dx, dy);
		}

		private PageRotate PageRotation(PdfPage pdfPage)
		{
			int rot = pdfPage.Rotation - pdfPage.OriginalRotation;
			if (rot < 0)
				rot = 4 + rot;
			return (PageRotate)rot;
		}

		private SelectInfo NormalizeSelectionInfo()
		{
			var selTmp = _selectInfo;
			if (selTmp.StartPage >= 0 && selTmp.EndPage >= 0)
			{
				if (selTmp.StartPage > selTmp.EndPage)
				{
					selTmp = new SelectInfo()
					{
						StartPage = selTmp.EndPage,
						EndPage = selTmp.StartPage,
						StartIndex = selTmp.EndIndex,
						EndIndex = selTmp.StartIndex
					};
				}
				else if ((selTmp.StartPage == selTmp.EndPage) && (selTmp.StartIndex > selTmp.EndIndex))
				{
					selTmp = new SelectInfo()
					{
						StartPage = selTmp.StartPage,
						EndPage = selTmp.EndPage,
						StartIndex = selTmp.EndIndex,
						EndIndex = selTmp.StartIndex
					};
				}
			}
			return selTmp;
		}

		private Size CalcVertical()
		{
			_renderRects = new Rect[Document.Pages.Count];
			double y = 0;
			double width = 0;
			for (int i = 0; i < _renderRects.Length; i++)
			{
				var rrect = GetRenderRect(i);
				_renderRects[i] = Helpers.CreateRect(
					rrect.X + PageMargin.Left,
					y + PageMargin.Top,
					rrect.Width - PageMargin.Left - PageMargin.Right,
					rrect.Height - PageMargin.Top - PageMargin.Bottom);
				y += rrect.Height;
				if (width < rrect.Width)
					width = rrect.Width;
			}
			return Helpers.CreateSize(width, y);
		}

		private Size CalcTilesVertical()
		{
			_renderRects = new Rect[Document.Pages.Count];
			double maxX = 0;
			double maxY = 0;
			for (int i = 0; i < _renderRects.Length; i += TilesCount)
			{
				double x = 0;
				double y = maxY;
				for (int j = i; j < i + TilesCount; j++)
				{
					if (j >= _renderRects.Length)
						break;
					var rrect = GetRenderRect(j);
					rrect.Width = rrect.Width / TilesCount;
					rrect.Height = rrect.Height / TilesCount;

					_renderRects[j] = Helpers.CreateRect(
						x + PageMargin.Left + (j == i ? rrect.X : 0),
						y + PageMargin.Top,
						rrect.Width - PageMargin.Left - PageMargin.Right,
						rrect.Height - PageMargin.Top - PageMargin.Bottom);
					x += rrect.Width + (j == i ? rrect.X : 0);

					if (maxY < _renderRects[j].Y + _renderRects[j].Height + PageMargin.Bottom)
						maxY = _renderRects[j].Y + _renderRects[j].Height + PageMargin.Bottom;
					if (maxX < _renderRects[j].X + _renderRects[j].Width + PageMargin.Right)
						maxX = _renderRects[j].X + _renderRects[j].Width + PageMargin.Right;
				}
			}
			return Helpers.CreateSize(maxX, maxY);
		}

		private Size CalcTilesVerticalNoChangeSize()
		{
			_renderRects = new Rect[Document.Pages.Count];
			double maxX = 0;
			double maxY = 0;
			for (int i = 0; i < _renderRects.Length; i += TilesCount)
			{
				double x = 0;
				double y = maxY;
				for (int j = i; j < i + TilesCount; j++)
				{
					if (j >= _renderRects.Length)
						break;
					var rrect = GetRenderRect(j);

					_renderRects[j] = Helpers.CreateRect(
						x + PageMargin.Left,
						y + PageMargin.Top,
						rrect.Width - PageMargin.Left - PageMargin.Right,
						rrect.Height - PageMargin.Top - PageMargin.Bottom);
					x += rrect.Width;

					if (maxY < _renderRects[j].Y + _renderRects[j].Height + PageMargin.Bottom)
						maxY = _renderRects[j].Y + _renderRects[j].Height + PageMargin.Bottom;
					if (maxX < _renderRects[j].X + _renderRects[j].Width + PageMargin.Right)
						maxX = _renderRects[j].X + _renderRects[j].Width + PageMargin.Right;
				}
			}
			return Helpers.CreateSize(maxX, maxY);
		}

		private Size CalcHorizontal()
		{
			_renderRects = new Rect[Document.Pages.Count];
			double height = 0;
			double x = 0;
			for (int i = 0; i < _renderRects.Length; i++)
			{
				var rrect = GetRenderRect(i);
				_renderRects[i] = Helpers.CreateRect(
					x + PageMargin.Left,
					rrect.Y + PageMargin.Top,
					rrect.Width - PageMargin.Left - PageMargin.Right,
					rrect.Height - PageMargin.Top - PageMargin.Bottom);
				x += rrect.Width;
				if (height < rrect.Height)
					height = rrect.Height;
			}
			return Helpers.CreateSize(x, height);
		}

		private Size CalcSingle()
		{
			_renderRects = new Rect[Document.Pages.Count];
			Size ret = Helpers.CreateSize(0, 0);
			for (int i = 0; i < _renderRects.Length; i++)
			{
				var rrect = GetRenderRect(i);
				_renderRects[i] = Helpers.CreateRect(
					rrect.X + PageMargin.Left,
					rrect.Y + PageMargin.Top,
					rrect.Width - PageMargin.Left - PageMargin.Right,
					rrect.Height - PageMargin.Top - PageMargin.Bottom);
				if (i == Document.Pages.CurrentIndex)
					ret = Helpers.CreateSize(rrect.Width, rrect.Height);
			}
			return ret;
		}

		private Rect renderRects(int index)
		{
			if (_renderRects != null)
				return _renderRects[index];
			else
				return new Rect(0, 0, 0, 0);
		}

		private void SetCurrentPage(int index)
		{
			try
			{
				Document.Pages.CurrentPageChanged -= Pages_CurrentPageChanged;
				if (Document.Pages.CurrentIndex != index)
				{
					Document.Pages.CurrentIndex = index;
					OnCurrentPageChanged(EventArgs.Empty);
				}
			}
			finally
			{
				Document.Pages.CurrentPageChanged += Pages_CurrentPageChanged;
			}
		}

		private bool CalcIntersectEntries(HighlightInfo existEntry, HighlightInfo addingEntry, out List<HighlightInfo> calcEntries)
		{
			calcEntries = new List<HighlightInfo>();
			int eStart = existEntry.CharIndex;
			int eEnd = existEntry.CharIndex + existEntry.CharsCount - 1;
			int aStart = addingEntry.CharIndex;
			int aEnd = addingEntry.CharIndex + addingEntry.CharsCount - 1;

			if (eStart < aStart && eEnd >= aStart && eEnd <= aEnd)
			{
				calcEntries.Add(new HighlightInfo()
				{
					CharIndex = eStart,
					CharsCount = aStart - eStart,
					Color = existEntry.Color
				});
				return true;
			}
			else if (eStart >= aStart && eStart <= aEnd && eEnd > aEnd)
			{
				calcEntries.Add(new HighlightInfo()
				{
					CharIndex = aEnd + 1,
					CharsCount = eEnd - aEnd,
					Color = existEntry.Color
				});
				return true;
			}
			else if (eStart >= aStart && eEnd <= aEnd)
				return true;
			else if (eStart < aStart && eEnd > aEnd)
			{
				calcEntries.Add(new HighlightInfo()
				{
					CharIndex = eStart,
					CharsCount = aStart - eStart,
					Color = existEntry.Color
				});
				calcEntries.Add(new HighlightInfo()
				{
					CharIndex = aEnd + 1,
					CharsCount = eEnd - aEnd,
					Color = existEntry.Color
				});
				return true;
			}
			//no intersection
			return false;
		}

		private void OnScroll()
		{
			if (Document != null)
			{
				int idx = CalcCurrentPage();
				if (idx >= 0)
				{
					SetCurrentPage(idx);
					InvalidateVisual();
				}
			}
		}

		private Size CalcPages()
		{
			switch (ViewMode)
			{
				case ViewModes.Vertical: return CalcVertical();
				case ViewModes.Horizontal: return CalcHorizontal();
				case ViewModes.TilesVertical: return CalcTilesVertical();
				default: return CalcSingle();
			}
		}

		private bool GetWord(PdfText text, int ci, out int si, out int ei)
		{
			si = ei = ci;
			if (text == null)
				return false;

			if (ci < 0)
				return false;

			for (int i = ci - 1; i >= 0; i--)
			{
				var c = text.GetCharacter(i);

				if (
					char.IsSeparator(c) || char.IsPunctuation(c) || char.IsControl(c) ||
					char.IsWhiteSpace(c) || c == '\r' || c == '\n'
					)
					break;
				si = i;
			}

			int last = text.CountChars;
			for (int i = ci + 1; i < last; i++)
			{
				var c = text.GetCharacter(i);

				if (
					char.IsSeparator(c) || char.IsPunctuation(c) || char.IsControl(c) ||
					char.IsWhiteSpace(c) || c == '\r' || c == '\n'
					)
					break;
				ei = i;
			}
			return true;
		}
		#endregion

		#region FillForms event raises
		/// <summary>
		/// Called by the engine when it is required to redraw the page
		/// </summary>
		/// <param name="e">An <see cref="InvalidatePageEventArgs"/> that contains the event data.</param>
		protected virtual void OnFormsInvalidate(InvalidatePageEventArgs e)
		{
            InvalidateVisual();
		}

		/// <summary>
		/// Called by the engine when it is required to execute GoTo operation
		/// </summary>
		/// <param name="e">An EventArgs that contains the event data.</param>
		protected virtual void OnFormsGotoPage(EventArgs<int> e)
		{
			if (Document == null)
				return;
			SetCurrentPage(e.Value);
			ScrollToPage(e.Value);
		}

		/// <summary>
		/// Called by the engine when it is required to execute a named action
		/// </summary>
		/// <param name="e">An EventArgs that contains the event data.</param>
		protected virtual void OnFormsDoNamedAction(EventArgs<string> e)
		{
			if (Document == null)
				return;
			var dest = Document.NamedDestinations.GetByName(e.Value);
			if (dest != null)
			{
				SetCurrentPage(dest.PageIndex);
				ScrollToPage(dest.PageIndex);
			}
		}

		/// <summary>
		/// Called by the engine when it is required to execute a GoTo action
		/// </summary>
		/// <param name="e">An <see cref="DoGotoActionEventArgs"/> that contains the event data.</param>
		protected virtual void OnFormsDoGotoAction(DoGotoActionEventArgs e)
		{
			if (Document == null)
				_onstartPageIndex = e.PageIndex;
			else
			{
				SetCurrentPage(e.PageIndex);
				ScrollToPage(e.PageIndex);
			}
		}

		/// <summary>
		/// Called by the engine when it is required to play the sound
		/// </summary>
		/// <param name="e">An EventArgs that contains the event data.</param>
		protected virtual void OnFormsAppBeep(EventArgs<BeepTypes> e)
		{
			switch (e.Value)
			{
				case BeepTypes.Default: System.Media.SystemSounds.Beep.Play(); break;
				case BeepTypes.Error: System.Media.SystemSounds.Asterisk.Play(); break;
				case BeepTypes.Question: System.Media.SystemSounds.Question.Play(); break;
				case BeepTypes.Warning: System.Media.SystemSounds.Exclamation.Play(); break;
				case BeepTypes.Status: System.Media.SystemSounds.Beep.Play(); break;
				default: System.Media.SystemSounds.Beep.Play(); break;
			}

		}

		/// <summary>
		/// Called by the engine when it is required to draw selected regions in FillForms
		/// </summary>
		/// <param name="e">An <see cref="InvalidatePageEventArgs"/> that contains the event data.</param>
		protected virtual void OnFormsOutputSelectedRect(InvalidatePageEventArgs e)
		{
			if (Document == null)
				return;
			var idx = Document.Pages.GetPageIndex(e.Page);
			var pt1 = PageToDevice(e.Rect.left, e.Rect.top, idx);
			var pt2 = PageToDevice(e.Rect.right, e.Rect.bottom, idx);
			_selectedRectangles.Add(Helpers.CreateRect(pt1.X, pt1.Y, pt2.X - pt1.X, pt2.Y - pt1.Y));
			InvalidateVisual();
		}

		/// <summary>
		/// Called by the engine when it is required to change the cursor
		/// </summary>
		/// <param name="e">An <see cref="SetCursorEventArgs"/> that contains the event data.</param>
		protected virtual void OnFormsSetCursor(SetCursorEventArgs e)
		{
			switch (e.Cursor)
			{
				case CursorTypes.Hand: Mouse.OverrideCursor = Cursors.Hand; break;
				case CursorTypes.HBeam: Mouse.OverrideCursor = Cursors.IBeam; break;
				case CursorTypes.VBeam: Mouse.OverrideCursor = Cursors.IBeam; break;
				case CursorTypes.NESW: Mouse.OverrideCursor = Cursors.SizeNESW; break;
				case CursorTypes.NWSE: Mouse.OverrideCursor = Cursors.SizeNWSE; break;
				default: Mouse.OverrideCursor = null; break;
			}
		}
		#endregion

		#region FillForms event handlers
		private void FormsInvalidate(object sender, InvalidatePageEventArgs e)
		{
			if (_invalidatePage == null)
			{
				_invalidatePage = e.Page;
				_invalidateRect = new FS_RECTF() { left = e.Rect.left + 3.0f, right = e.Rect.right - 3.0f, top = e.Rect.top + 3.0f, bottom = e.Rect.bottom - 3.0f };
			}
			OnFormsInvalidate(e);
		}

		private void FormsFocusChanged(object sender, FocusChangedEventArgs e)
		{
			_invalidatePage = null;
		}

		private void FormsGotoPage(object sender, EventArgs<int> e)
		{
			OnFormsGotoPage(e);
        }

		private void FormsDoNamedAction(object sender, EventArgs<string> e)
		{
			OnFormsDoNamedAction(e);
		}

		private void FormsDoGotoAction(object sender, DoGotoActionEventArgs e)
		{
			OnFormsDoGotoAction(e);
		}

		private void FormsAppBeep(object sender, EventArgs<BeepTypes> e)
		{
			OnFormsAppBeep(e);
        }

		private void FormsOutputSelectedRect(object sender, InvalidatePageEventArgs e)
		{
			OnFormsOutputSelectedRect(e);
		}

		private void FormsSetCursor(object sender, SetCursorEventArgs e)
		{
			OnFormsSetCursor(e);
		}
		#endregion

		#region Miscellaneous event handlers
		void Pages_CurrentPageChanged(object sender, EventArgs e)
		{
			OnCurrentPageChanged(EventArgs.Empty);
			InvalidateVisual();
		}

		void Pages_PageInserted(object sender, PageCollectionChangedEventArgs e)
		{
			UpdateDocLayout();
		}

		void Pages_PageDeleted(object sender, PageCollectionChangedEventArgs e)
		{
			UpdateDocLayout();

		}
		#endregion

		#region Helpers
		private Rect ClientRect
		{
			get
			{
				//return VisualTreeHelper.GetDescendantBounds(pdfViewer);
				return new Rect(0, 0, ViewportWidth, ViewportHeight);
			}
		}
		#endregion

		#region IScrollInfo implementation

		#region properties
		/// <summary>
		/// Gets or sets a value that indicates whether scrolling on the vertical axis is possible.
		/// </summary>
		/// <value>true if scrolling is possible; otherwise, false. This property has no default value.</value>
		public bool CanVerticallyScroll { get; set; }

		/// <summary>
		/// Gets or sets a value that indicates whether scrolling on the horizontal axis is possible.
		/// </summary>
		/// <value>true if scrolling is possible; otherwise, false. This property has no default value.</value>
		public bool CanHorizontallyScroll { get; set; }

		/// <summary>
		/// Gets or sets a ScrollViewer element that controls scrolling behavior.
		/// </summary>
		/// <value>A ScrollViewer element that controls scrolling behavior. This property has no default value.</value>
		/// <remarks>Logical scrolling enables scrolling to the next element in the logical tree. Physical scrolling, in contrast, scrolls content by a defined measurable increment in a specified direction. If you require physical scrolling instead of logical scrolling, wrap the host Panel element in a ScrollViewer and set the value of its CanContentScroll property to false.</remarks>
		public ScrollViewer ScrollOwner { get; set; }

		/// <summary>
		/// Gets the horizontal size of the extent.
		/// </summary>
		/// <value>A Double that represents, in device independent pixels, the horizontal size of the extent. This property has no default value.</value>
		public double ExtentWidth
		{
			get
			{
				return _extent.Width;
			}
		}

		/// <summary>
		/// Gets the vertical size of the extent.
		/// </summary>
		/// <value>A Double that represents, in device independent pixels, the vertical size of the extent. This property has no default value.</value>
		public double ExtentHeight
		{
			get
			{
				return _extent.Height;
			}
		}

		/// <summary>
		/// Gets the horizontal size of the viewport for this content.
		/// </summary>
		/// <value>A Double that represents, in device independent pixels, the vertical size of the viewport for this content. This property has no default value.</value>
		public double ViewportWidth
		{
			get
			{
				return _viewport.Width;
			}
		}

		/// <summary>
		/// Gets the vertical size of the viewport for this content.
		/// </summary>
		/// <value>A Double that represents, in device independent pixels, the vertical size of the viewport for this content. This property has no default value.</value>
		public double ViewportHeight
		{
			get
			{
				return _viewport.Height;
			}
		}

		/// <summary>
		/// Gets the horizontal offset of the scrolled content.
		/// </summary>
		/// <value>A Double that represents, in device independent pixels, the horizontal offset. This property has no default value.</value>
		/// <remarks>Valid values are between zero and the <see cref="ExtentWidth"/> minus the <see cref="ViewportWidth"/>.</remarks>
		public double HorizontalOffset
		{
			get
			{
				return -_autoScrollPosition.X;
			}
		}

		/// <summary>
		/// Gets the vertical offset of the scrolled content.
		/// </summary>
		/// <value>A Double that represents, in device independent pixels, the vertical offset of the scrolled content. Valid values are between zero and the ExtentHeight minus the ViewportHeight. This property has no default value.</value>
		public double VerticalOffset
		{
			get
			{
				return -_autoScrollPosition.Y;
			}
		}

		#endregion

		#region methods
		/// <summary>
		/// Sets the amount of vertical offset.
		/// </summary>
		/// <param name="offset">The degree to which content is vertically offset from the containing viewport.</param>
		public void SetVerticalOffset(double offset)
		{
			if (offset < 0 || _viewport.Height >= _extent.Height)
			{
				offset = 0;
			}
			else
			{
				if (offset + _viewport.Height >= _extent.Height)
				{
					offset = _extent.Height - _viewport.Height;
				}
			}

			_autoScrollPosition.Y = -offset;

			if (ScrollOwner != null)
				ScrollOwner.InvalidateScrollInfo();

			OnScroll();
		}

		/// <summary>
		/// Sets the amount of horizontal offset.
		/// </summary>
		/// <param name="offset">The degree to which content is horizontally offset from the containing viewport.</param>
		public void SetHorizontalOffset(double offset)
		{
			if (offset < 0 || _viewport.Width >= _extent.Width)
			{
				offset = 0;
			}
			else
			{
				if (offset + _viewport.Width >= _extent.Width)
				{
					offset = _extent.Width - _viewport.Width;
				}
			}

			_autoScrollPosition.X = -offset;

			if (ScrollOwner != null)
				ScrollOwner.InvalidateScrollInfo();

			OnScroll();
		}

		/// <summary>
		/// Forces content to scroll until the coordinate space of a Visual object is visible.
		/// </summary>
		/// <param name="visual">A Visual that becomes visible.</param>
		/// <param name="rectangle">A bounding rectangle that identifies the coordinate space to make visible.</param>
		/// <returns>A Rect that is visible.</returns>
		/// <remarks>In most cases, the returned rectangle is a transformed version of the input rectangle. In some cases, such as when the input rectangle cannot fit entirely within the viewport, the return value may be smaller.</remarks>
		public Rect MakeVisible(Visual visual, Rect rectangle)
		{
			if(_isProgrammaticallyFocusSetted)
			{
				_isProgrammaticallyFocusSetted = false;
				return rectangle;
            }
			SetHorizontalOffset(rectangle.X - _autoScrollPosition.X);
			SetVerticalOffset(rectangle.Y - _autoScrollPosition.Y);
			return new Rect(_autoScrollPosition.X, _autoScrollPosition.Y, ViewportWidth, ViewportHeight);
		}

		/// <summary>
		/// Scrolls up within content by one logical unit.
		/// </summary>
		public void LineUp()
		{
			SetVerticalOffset(this.VerticalOffset - _viewport.Height / 10);
		}

		/// <summary>
		/// Scrolls down within content by one logical unit.
		/// </summary>
		public void LineDown()
		{
			SetVerticalOffset(this.VerticalOffset + _viewport.Height / 10);
		}

		/// <summary>
		/// Scrolls left within content by one logical unit.
		/// </summary>
		public void LineLeft()
		{
			SetHorizontalOffset(this.HorizontalOffset - _viewport.Width / 10);
		}

		/// <summary>
		/// Scrolls right within content by one logical unit.
		/// </summary>
		public void LineRight()
		{
			SetHorizontalOffset(this.HorizontalOffset + _viewport.Width / 10);
		}

		/// <summary>
		/// Scrolls up within content by one page.
		/// </summary>
		public void PageUp()
		{
			double childHeight = (_viewport.Height * 1);// / this.InternalChildren.Count;
			SetVerticalOffset(this.VerticalOffset - childHeight);
		}

		/// <summary>
		/// Scrolls down within content by one page.
		/// </summary>
		public void PageDown()
		{
			double childHeight = (_viewport.Height * 1);// / this.InternalChildren.Count;
			SetVerticalOffset(this.VerticalOffset + childHeight);
		}

		/// <summary>
		/// Scrolls left within content by one page.
		/// </summary>
		public void PageLeft()
		{
			double childWidth = (_viewport.Width * 1);// / this.InternalChildren.Count;
			SetHorizontalOffset(this.HorizontalOffset - childWidth);
		}

		/// <summary>
		/// Scrolls right within content by one page.
		/// </summary>
		public void PageRight()
		{
			double childWidth = (_viewport.Width * 1);// / this.InternalChildren.Count;
			SetHorizontalOffset(this.HorizontalOffset + childWidth);
		}

		/// <summary>
		/// Scrolls up within content after a user clicks the wheel button on a mouse.
		/// </summary>
		public void MouseWheelUp()
		{
			SetVerticalOffset(this.VerticalOffset - _viewport.Height / 10 * 3);
		}

		/// <summary>
		/// Scrolls down within content after a user clicks the wheel button on a mouse.
		/// </summary>
		public void MouseWheelDown()
		{
			SetVerticalOffset(this.VerticalOffset + _viewport.Height / 10 * 3);
		}

		/// <summary>
		/// Scrolls left within content after a user clicks the wheel button on a mouse.
		/// </summary>
		public void MouseWheelLeft()
		{
			SetHorizontalOffset(this.HorizontalOffset - _viewport.Width / 10 * 3);
		}

		/// <summary>
		/// Scrolls right within content after a user clicks the wheel button on a mouse.
		/// </summary>
		public void MouseWheelRight()
		{
			SetHorizontalOffset(this.HorizontalOffset + _viewport.Height / 10 * 3);
		}


		#endregion

		#endregion


		#region Select tool
		private void ProcessMouseDoubleClickForSelectTextTool(Point page_point, int page_index)
		{
			var page = Document.Pages[page_index];
			int si, ei;
			int ci = page.Text.GetCharIndexAtPos((float)page_point.X, (float)page_point.Y, 10.0f, 10.0f);
			if (GetWord(page.Text, ci, out si, out ei))
			{
				_selectInfo = new SelectInfo()
				{
					StartPage = page_index,
					EndPage = page_index,
					StartIndex = si,
					EndIndex = ei
				};
				if (_selectInfo.StartPage >= 0)
					OnSelectionChanged(EventArgs.Empty);
				InvalidateVisual();
			}
		}

		private void ProcessMouseDownForSelectTextTool(Point page_point, int page_index)
		{
			_selectInfo = new SelectInfo()
			{
				StartPage = page_index,
				EndPage = page_index,
                StartIndex = Document.Pages[page_index].Text.GetCharIndexAtPos((float)page_point.X, (float)page_point.Y, 10.0f, 10.0f),
				EndIndex = Document.Pages[page_index].Text.GetCharIndexAtPos((float)page_point.X, (float)page_point.Y, 10.0f, 10.0f)
			};
			if (_selectInfo.StartPage >= 0)
				OnSelectionChanged(EventArgs.Empty);
		}

		private void ProcessMouseMoveForSelectTextTool(int page_index, int character_index)
		{
			if (_mousePressed)
			{
				if (character_index >= 0)
				{
					_selectInfo = new SelectInfo()
					{
						StartPage = _selectInfo.StartPage,
						EndPage = page_index,
						EndIndex = character_index,
						StartIndex = _selectInfo.StartIndex
					};
				}
				InvalidateVisual();
			}
		}
		#endregion

		#region Default tool
		private void ProcessMouseDownDefaultTool(Point page_point, int page_index)
		{
			var pdfLink = Document.Pages[page_index].Links.GetLinkAtPoint((float)page_point.X, (float)page_point.Y);
			var webLink = Document.Pages[page_index].Text.WebLinks.GetWebLinkAtPoint((float)page_point.X, (float)page_point.Y);
			if (webLink != null || pdfLink != null)
				_mousePressedInLink = true;
			else
				_mousePressedInLink = false;
		}

		private void ProcessMouseMoveForDefaultTool(Point page_point, int page_index)
		{
			var pdfLink = Document.Pages[page_index].Links.GetLinkAtPoint((float)page_point.X, (float)page_point.Y);
			var webLink = Document.Pages[page_index].Text.WebLinks.GetWebLinkAtPoint((float)page_point.X, (float)page_point.Y);
			if (webLink != null || pdfLink != null)
				Mouse.OverrideCursor = Cursors.Hand;

		}

		private void PriocessMouseUpForDefaultTool(Point page_point, int page_index)
		{
			if (_mousePressedInLink)
			{
				var pdfLink = Document.Pages[page_index].Links.GetLinkAtPoint((float)page_point.X, (float)page_point.Y);
				var webLink = Document.Pages[page_index].Text.WebLinks.GetWebLinkAtPoint((float)page_point.X, (float)page_point.Y);
				if (webLink != null || pdfLink != null)
					ProcessLinkClicked(pdfLink, webLink);
			}
		}
		#endregion

		#region Pan tool
		private void ProcessMouseDownPanTool(Point mouse_point)
		{
			_panToolInitialScrollPosition = _autoScrollPosition;
			_panToolInitialMousePosition = mouse_point;
			CaptureMouse();
		}

		private void ProcessMouseMoveForPanTool(Point mouse_point)
		{
			if (!_mousePressed)
				return;
			var yOffs = mouse_point.Y - _panToolInitialMousePosition.Y;
			var xOffs = mouse_point.X - _panToolInitialMousePosition.X;
			//_autoScrollPosition = new Point(-_panToolInitialScrollPosition.X - xOffs, -_panToolInitialScrollPosition.Y - yOffs);
			SetVerticalOffset(-_panToolInitialScrollPosition.Y - yOffs);
			SetHorizontalOffset(-_panToolInitialScrollPosition.X - xOffs);
			//Scrol
		}

		private void ProcessMouseUpPanTool(Point mouse_point)
		{
			ReleaseMouseCapture();
		}
		#endregion
	}
}
