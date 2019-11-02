using Patagames.Pdf.Enums;
using Patagames.Pdf.Net.EventArguments;
using Patagames.Pdf.Net.Exceptions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.ComponentModel;
using Patagames.Pdf.Net.Annotations;

namespace Patagames.Pdf.Net.Controls.Wpf
{
    /// <summary>
    /// Represents a pdf view control for displaying a Pdf document.
    /// </summary>	
    [LicenseProvider]
    public partial class PdfViewer : Control, IScrollInfo
    {
        #region Private fields
        private bool _preventStackOverflowBugWorkaround = false;
        private SelectInfo _selectInfo = new SelectInfo() { StartPage = -1 };
        private SortedDictionary<int, List<HighlightInfo>> _highlightedText = new SortedDictionary<int, List<HighlightInfo>>();
        private bool _mousePressed = false;
        private bool _mousePressedInLink = false;
        private bool _isShowSelection = false;
        private int _onstartPageIndex = 0;
        private Point _panToolInitialScrollPosition;
        private Point _panToolInitialMousePosition;

        private PdfForms _fillForms;
        private List<Rect> _selectedRectangles = new List<Rect>();
        private Pen _pageBorderColorPen = Helpers.CreatePen((Color)PageBorderColorProperty.DefaultMetadata.DefaultValue);
        private Pen _pageSeparatorColorPen = Helpers.CreatePen((Color)PageSeparatorColorProperty.DefaultMetadata.DefaultValue);
		private Pen _currentPageHighlightColorPen = Helpers.CreatePen((Color)CurrentPageHighlightColorProperty.DefaultMetadata.DefaultValue, 4);

        private RenderRect[] _renderRects;
        private int _startPage
        {
            get
            {
                if (Document == null)
                    return 0;
                switch (ViewMode)
                {
                    case ViewModes.SinglePage:
                        return Document.Pages.CurrentIndex;
                    case ViewModes.TilesLine:
                        return Document.Pages.CurrentIndex % TilesCount == 0 ? Document.Pages.CurrentIndex : Document.Pages.CurrentIndex - Document.Pages.CurrentIndex % TilesCount;
                    default:
                        return 0;
                }
            }
        }
        private int _endPage
        {
            get
            {
                if (Document == null)
                    return -1;
                switch (ViewMode)
                {
                    case ViewModes.SinglePage:
                        return Document.Pages.CurrentIndex;
                    case ViewModes.TilesLine:
                        return Math.Min(_startPage + TilesCount - 1, _renderRects != null ? _renderRects.Length - 1 : -1);
                    default:
                        return _renderRects != null ? _renderRects.Length - 1 : -1;
                }
            }
        }

        private Size _extent = new Size(0, 0);
        private Size _viewport = new Size(0, 0);
        private Point _autoScrollPosition = new Point(0, 0);
        private bool _isProgrammaticallyFocusSetted=false;

        private PRCollection _prPages = new PRCollection();
        private System.Windows.Threading.DispatcherTimer _invalidateTimer = null;

        WriteableBitmap _canvasWpfBitmap = null;
        private bool _loadedByViewer = true;

        private struct CaptureInfo
        {
            public PdfForms forms;
            public SynchronizationContext sync;
            public int color;
        }
        private CaptureInfo _externalDocCapture;

		private Point _scrollPoint;
		private bool _scrollPointSaved;

        private enum SmoothSelection { None, ByCharacter, ByLine }
        private SmoothSelection _smoothSelection;
        #endregion

        #region Events
        /// <summary>
        /// Occurs whenever the Document property is changed.
        /// </summary>
        public event EventHandler AfterDocumentChanged;

        /// <summary>
        /// Occurs immediately before the document property would be changed.
        /// </summary>
        public event EventHandler<DocumentClosingEventArgs> BeforeDocumentChanged;

        /// <summary>
        /// Occurs whenever the document loads.
        /// </summary>
        public event EventHandler DocumentLoaded;

        /// <summary>
        /// Occurs before the document unloads.
        /// </summary>
        public event EventHandler<DocumentClosingEventArgs> DocumentClosing;

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

        /// <summary>
        /// Occurs when the value of the <see cref="ShowLoadingIcon"/> property has changed.
        /// </summary>
        public event EventHandler ShowLoadingIconChanged;

        /// <summary>
        /// Occurs when the value of the <see cref="UseProgressiveRender"/> property has changed.
        /// </summary>
        public event EventHandler UseProgressiveRenderChanged;

        /// <summary>
        /// Occurs when the value of the <see cref="LoadingIconText"/> property has changed.
        /// </summary>
        public event EventHandler LoadingIconTextChanged;
		
		/// <summary>
		/// Occurs when the <see cref="FormsBlendMode"/> property has changed.
		/// </summary>
		public event EventHandler FormsBlendModeChanged;


		#endregion

		#region Event raises
		/// <summary>
		/// Raises the <see cref="AfterDocumentChanged"/> event.
		/// </summary>
		/// <param name="e">An System.EventArgs that contains the event data.</param>
		protected virtual void OnAfterDocumentChanged(EventArgs e)
        {
            if (AfterDocumentChanged != null)
                AfterDocumentChanged(this, e);
        }

        /// <summary>
        /// Raises the <see cref="BeforeDocumentChanged"/> event.
        /// </summary>
        /// <param name="e">An System.EventArgs that contains the event data.</param>
        /// <returns>True if changing should be canceled, False otherwise</returns>
        protected virtual bool OnBeforeDocumentChanged(DocumentClosingEventArgs e)
        {
            if (BeforeDocumentChanged != null)
                BeforeDocumentChanged(this, e);
            return e.Cancel;
        }

        /// <summary>
        /// Raises the <see cref="DocumentLoaded"/> event.
        /// </summary>
        /// <param name="e">An System.EventArgs that contains the event data.</param>
        protected virtual void OnDocumentLoaded(EventArgs e)
        {
            if (DocumentLoaded != null)
                DocumentLoaded(this, e);
        }

        /// <summary>
        /// Raises the <see cref="DocumentClosing"/> event.
        /// </summary>
        /// <param name="e">An System.EventArgs that contains the event data.</param>
        /// <returns>True if closing should be canceled, False otherwise</returns>
        protected virtual bool OnDocumentClosing(DocumentClosingEventArgs e)
        {
            if (DocumentClosing != null)
                DocumentClosing(this, e);
            return e.Cancel;
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
        /// <summary>
        /// Raises the <see cref="ShowLoadingIconChanged"/> event.
        /// </summary>
        /// <param name="e">An System.EventArgs that contains the event data.</param>
        protected virtual void OnShowLoadingIconChanged(EventArgs e)
        {
            if (ShowLoadingIconChanged != null)
                ShowLoadingIconChanged(this, e);
        }

        /// <summary>
        /// Raises the <see cref="UseProgressiveRenderChanged"/> event.
        /// </summary>
        /// <param name="e">An System.EventArgs that contains the event data.</param>
        protected virtual void OnUseProgressiveRenderChanged(EventArgs e)
        {
            if (UseProgressiveRenderChanged != null)
                UseProgressiveRenderChanged(this, e);
        }

        /// <summary>
        /// Raises the <see cref="LoadingIconTextChanged"/> event.
        /// </summary>
        /// <param name="e">An System.EventArgs that contains the event data.</param>
        protected virtual void OnLoadingIconTextChanged(EventArgs e)
        {
            if (LoadingIconTextChanged != null)
                LoadingIconTextChanged(this, e);
        }

		/// <summary>
		/// Raises the <see cref="FormsBlendModeChanged"/> event.
		/// </summary>
		/// <param name="e">An System.EventArgs that contains the event data.</param>
		protected virtual void OnFormsBlendModeChanged(EventArgs e)
		{
			if (FormsBlendModeChanged != null)
				FormsBlendModeChanged(this, e);
		}
        #endregion

        #region Dependency properties
        /// <summary>
        /// DependencyProperty as the backing store for <see cref="FormsBlendMode"/>
        /// </summary>
        public static readonly DependencyProperty FormsBlendModeProperty =
			DependencyProperty.Register("FormsBlendMode", typeof(BlendTypes), typeof(PdfViewer),
				new FrameworkPropertyMetadata(BlendTypes.FXDIB_BLEND_MULTIPLY,
					FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.Journal,
					(o, e) => { (o as PdfViewer).OnFormsBlendModeChanged(EventArgs.Empty); }));

		/// <summary>
		/// DependencyProperty as the backing store for <see cref="PdfViewer"/>
		/// </summary>
		/// <remarks>
		/// <note type="note">
		/// Please note
		/// <list type="bullet">
		/// <item>
		/// The OneWay binding would be disabled if you set the Document property with a document what is not from the binding source. 
		/// More explanations can be found <a href="http://stackoverflow.com/questions/1389038/why-does-data-binding-break-in-oneway-mode">here</a>
		/// </item>
		/// <item>The TwoWay binding does not allow you to set the Document property with the document what is not from a binding source.</item>
		/// </list>
		/// </note>
		/// </remarks>
		public static readonly DependencyProperty DocumentProperty =
            DependencyProperty.Register("Document", typeof(PdfDocument), typeof(PdfViewer),
                new PropertyMetadata(null, 
                    (o, e) =>
                    {
                        var viewer = o as PdfViewer;
                        var oldValue = e.OldValue as PdfDocument;
                        var newValue = e.NewValue as PdfDocument;

                        if (oldValue != newValue)
                        {
                            if (oldValue != null && viewer._loadedByViewer)
                            {
                                //we need to close the previous document if it was loaded by viewer
                                oldValue.Dispose();
                                //_document = null;
                                viewer.OnDocumentClosed(EventArgs.Empty);
                            }
                            else if (oldValue != null && !viewer._loadedByViewer)
                            {
                                oldValue.Pages.CurrentPageChanged -= viewer.Pages_CurrentPageChanged;
                                oldValue.Pages.PageInserted -= viewer.Pages_PageInserted;
                                oldValue.Pages.PageDeleted -= viewer.Pages_PageDeleted;
                                oldValue.Pages.ProgressiveRender -= viewer.Pages_ProgressiveRender;
                            }
                            viewer._extent = new Size(0, 0);
                            viewer._selectInfo = new SelectInfo() { StartPage = -1 };
                            viewer._highlightedText.Clear();
                            viewer._onstartPageIndex = 0;
                            viewer._renderRects = null;
                            viewer._loadedByViewer = false;
                            viewer.ReleaseFillForms(viewer._externalDocCapture);
                            //_document = value;
                            viewer.UpdateDocLayout();
                            if (newValue != null)
                            {
                                if (newValue.FormFill != viewer._fillForms)
                                    viewer._externalDocCapture = viewer.CaptureFillForms(newValue.FormFill);
                                newValue.Pages.CurrentPageChanged += viewer.Pages_CurrentPageChanged;
                                newValue.Pages.PageInserted += viewer.Pages_PageInserted;
                                newValue.Pages.PageDeleted += viewer.Pages_PageDeleted;
                                newValue.Pages.ProgressiveRender += viewer.Pages_ProgressiveRender;
                                viewer.SetCurrentPage(viewer._onstartPageIndex);
                                if (newValue.Pages.Count > 0)
                                    if (viewer._onstartPageIndex != 0)
                                        viewer.ScrollToPage(viewer._onstartPageIndex);
                                    else
                                        viewer._autoScrollPosition = new Point(0, 0);
                            }
                            viewer.OnAfterDocumentChanged(EventArgs.Empty);
                        }
                     },
                    (dobj, o) =>
                    {
                        var viewer = dobj as PdfViewer;
                        var oldValue = viewer.Document;
                        var newValue = o as PdfDocument;

                        if (oldValue != newValue)
                        {
                            if (viewer.OnBeforeDocumentChanged(new DocumentClosingEventArgs()))
                                return oldValue;

                            if (oldValue != null && viewer._loadedByViewer)
                            {
                                //we need to close the previous document if it was loaded by viewer
                                if (viewer.OnDocumentClosing(new DocumentClosingEventArgs()))
                                    return oldValue; //the closing was canceled;
                            }
                        }
                        return newValue;
                    }));

		/// <summary>
		/// DependencyProperty as the backing store for <see cref="PageBackColor"/>
		/// </summary>
		public static readonly DependencyProperty PageBackColorProperty =
			DependencyProperty.Register("PageBackColor", typeof(Color), typeof(PdfViewer),
				new FrameworkPropertyMetadata(Color.FromArgb(255, 255, 255, 255),
					FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.Journal,
					(o, e) => { (o as PdfViewer).OnPageBackColorChanged(EventArgs.Empty); }));

		/// <summary>
		/// DependencyProperty as the backing store for <see cref="PageMargin"/>
		/// </summary>
		public static readonly DependencyProperty PageMarginProperty =
			DependencyProperty.Register("PageMargin", typeof(Thickness), typeof(PdfViewer),
				new FrameworkPropertyMetadata(new Thickness(10),
					FrameworkPropertyMetadataOptions.Journal | FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure,
					(o, e) => {
						(o as PdfViewer).UpdateDocLayout();
						(o as PdfViewer).OnPageMarginChanged(EventArgs.Empty); }));

		/// <summary>
		/// DependencyProperty as the backing store for <see cref="Padding"/>
		/// </summary>
		public static readonly new DependencyProperty PaddingProperty =
			DependencyProperty.Register("Padding", typeof(Thickness), typeof(PdfViewer),
				new FrameworkPropertyMetadata(new Thickness(10),
					FrameworkPropertyMetadataOptions.Journal | FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure,
					(o, e) => {
						(o as PdfViewer).UpdateDocLayout();
					}));

		/// <summary>
		/// DependencyProperty as the backing store for <see cref="PageBorderColor"/>
		/// </summary>
		public static readonly DependencyProperty PageBorderColorProperty =
			DependencyProperty.Register("PageBorderColor", typeof(Color), typeof(PdfViewer),
				new FrameworkPropertyMetadata(Color.FromArgb(255, 0, 0, 0),
					FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.Journal,
					(o, e) => 
					{
						var viewer = (o as PdfViewer);
						viewer._pageBorderColorPen = Helpers.CreatePen((Color)e.NewValue);
						viewer.OnPageBorderColorChanged(EventArgs.Empty);
					}));

		/// <summary>
		/// DependencyProperty as the backing store for <see cref="SizeMode"/>
		/// </summary>
		public static readonly DependencyProperty SizeModeProperty =
			DependencyProperty.Register("SizeMode", typeof(SizeModes), typeof(PdfViewer),
				new FrameworkPropertyMetadata(SizeModes.FitToWidth,
					FrameworkPropertyMetadataOptions.Journal,
					(o, e) =>
					{
						var viewer = (o as PdfViewer);
						viewer.UpdateDocLayout();
						viewer.OnSizeModeChanged(EventArgs.Empty);
					}));

		/// <summary>
		/// DependencyProperty as the backing store for <see cref="TextSelectColor"/>
		/// </summary>
		public static readonly DependencyProperty TextSelectColorProperty =
			DependencyProperty.Register("TextSelectColor", typeof(Color), typeof(PdfViewer),
				new FrameworkPropertyMetadata(Color.FromArgb(70, 70, 130, 180),
					FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.Journal,
					(o, e) =>
					{
						var viewer = (o as PdfViewer);
						viewer.OnTextSelectColorChanged(EventArgs.Empty);
					}));

		/// <summary>
		/// DependencyProperty as the backing store for <see cref="FormHighlightColor"/>
		/// </summary>
		public static readonly DependencyProperty FormHighlightColorProperty =
			DependencyProperty.Register("FormHighlightColor", typeof(Color), typeof(PdfViewer),
				new FrameworkPropertyMetadata(Color.FromArgb(0, 255, 255, 255),
					FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.Journal,
					(o, e) =>
					{
						var viewer = (o as PdfViewer);
						if (viewer._fillForms != null)
							viewer._fillForms.SetHighlightColorEx(FormFieldTypes.FPDF_FORMFIELD_UNKNOWN, Helpers.ToArgb((Color)e.NewValue));
						if (viewer.Document != null && !viewer._loadedByViewer && viewer._externalDocCapture.forms != null)
							viewer._externalDocCapture.forms.SetHighlightColorEx(FormFieldTypes.FPDF_FORMFIELD_UNKNOWN, Helpers.ToArgb((Color)e.NewValue));
						viewer.OnFormHighlightColorChanged(EventArgs.Empty);
					}));

		/// <summary>
		/// DependencyProperty as the backing store for <see cref="Zoom"/>
		/// </summary>
		public static readonly DependencyProperty ZoomProperty =
			DependencyProperty.Register("Zoom", typeof(float), typeof(PdfViewer),
				new FrameworkPropertyMetadata(1.0f,
					FrameworkPropertyMetadataOptions.Journal | FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure,
					(o, e) =>
					{
                        var viewer = (o as PdfViewer);
                        if (viewer._preventStackOverflowBugWorkaround)
                            return;
                        viewer.UpdateDocLayout();
						viewer.OnZoomChanged(EventArgs.Empty);
					}));

        /// <summary>
        /// DependencyProperty as the backing store for <see cref="SelectedText"/>
        /// </summary>
        public static readonly DependencyProperty SelectedTextProperty =
            DependencyProperty.Register("SelectedText", typeof(string), typeof(PdfViewer),
                new FrameworkPropertyMetadata("",
                    FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.Journal));

		/// <summary>
		/// DependencyProperty as the backing store for <see cref="ViewMode"/>
		/// </summary>
		public static readonly DependencyProperty ViewModeProperty =
			DependencyProperty.Register("ViewMode", typeof(ViewModes), typeof(PdfViewer),
				new FrameworkPropertyMetadata(ViewModes.Vertical,
					FrameworkPropertyMetadataOptions.Journal,
					(o, e) =>
					{
						var viewer = (o as PdfViewer);
						viewer.UpdateDocLayout();
						viewer.OnViewModeChanged(EventArgs.Empty);
					}));

		/// <summary>
		/// DependencyProperty as the backing store for <see cref="PageSeparatorColor"/>
		/// </summary>
		public static readonly DependencyProperty PageSeparatorColorProperty =
			DependencyProperty.Register("PageSeparatorColor", typeof(Color), typeof(PdfViewer),
				new FrameworkPropertyMetadata(Color.FromArgb(255, 190, 190, 190),
					FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.Journal,
					(o, e) =>
					{
						var viewer = (o as PdfViewer);
						viewer._pageSeparatorColorPen = Helpers.CreatePen((Color)e.NewValue);
						viewer.OnPageSeparatorColorChanged(EventArgs.Empty);
					}));

		/// <summary>
		/// DependencyProperty as the backing store for <see cref="ShowPageSeparator"/>
		/// </summary>
		public static readonly DependencyProperty ShowPageSeparatorProperty =
			DependencyProperty.Register("ShowPageSeparator", typeof(bool), typeof(PdfViewer),
				new FrameworkPropertyMetadata(true,
					 FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.Journal,
					(o, e) =>
					{
						var viewer = (o as PdfViewer);
						viewer.OnShowPageSeparatorChanged(EventArgs.Empty);
					}));

		/// <summary>
		/// DependencyProperty as the backing store for <see cref="CurrentPageHighlightColor"/>
		/// </summary>
		public static readonly DependencyProperty CurrentPageHighlightColorProperty =
			DependencyProperty.Register("CurrentPageHighlightColor", typeof(Color), typeof(PdfViewer),
				new FrameworkPropertyMetadata(Color.FromArgb(170, 70, 130, 180),
					FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.Journal,
					(o, e) =>
					{
						var viewer = (o as PdfViewer);
						viewer._currentPageHighlightColorPen = Helpers.CreatePen((Color)e.NewValue, 4);
						viewer.OnCurrentPageHighlightColorChanged(EventArgs.Empty);
					}));

		/// <summary>
		/// DependencyProperty as the backing store for <see cref="ShowCurrentPageHighlight"/>
		/// </summary>
		public static readonly DependencyProperty ShowCurrentPageHighlightProperty =
			DependencyProperty.Register("ShowCurrentPageHighlight", typeof(bool), typeof(PdfViewer),
				new FrameworkPropertyMetadata(true,
					 FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.Journal,
					(o, e) =>
					{
						var viewer = (o as PdfViewer);
						viewer.OnShowCurrentPageHighlightChanged(EventArgs.Empty);
					}));

		/// <summary>
		/// DependencyProperty as the backing store for <see cref="PageVAlign"/>
		/// </summary>
		public static readonly DependencyProperty PageVAlignProperty =
			DependencyProperty.Register("PageVAlign", typeof(VerticalAlignment), typeof(PdfViewer),
				new FrameworkPropertyMetadata(VerticalAlignment.Center,
					 FrameworkPropertyMetadataOptions.Journal,
					(o, e) =>
					{
						var viewer = (o as PdfViewer);
						viewer.UpdateDocLayout();
						viewer.OnPageAlignChanged(EventArgs.Empty);
					}));

		/// <summary>
		/// DependencyProperty as the backing store for <see cref="PageHAlign"/>
		/// </summary>
		public static readonly DependencyProperty PageHAlignProperty =
			DependencyProperty.Register("PageHAlign", typeof(HorizontalAlignment), typeof(PdfViewer),
				new FrameworkPropertyMetadata(HorizontalAlignment.Center,
					 FrameworkPropertyMetadataOptions.Journal,
					(o, e) =>
					{
						var viewer = (o as PdfViewer);
						viewer.UpdateDocLayout();
						viewer.OnPageAlignChanged(EventArgs.Empty);
					}));

		/// <summary>
		/// DependencyProperty as the backing store for <see cref="RenderFlags"/>
		/// </summary>
		public static readonly DependencyProperty RenderFlagsProperty =
			DependencyProperty.Register("RenderFlags", typeof(RenderFlags), typeof(PdfViewer),
				new FrameworkPropertyMetadata(RenderFlags.FPDF_LCD_TEXT | RenderFlags.FPDF_NO_CATCH,
					 FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.Journal,
					(o, e) =>
					{
						var viewer = (o as PdfViewer);
						viewer.OnRenderFlagsChanged(EventArgs.Empty);
					}));

		/// <summary>
		/// DependencyProperty as the backing store for <see cref="TilesCount"/>
		/// </summary>
		public static readonly DependencyProperty TilesCountProperty =
			DependencyProperty.Register("TilesCount", typeof(int), typeof(PdfViewer),
				new FrameworkPropertyMetadata(2,
					 FrameworkPropertyMetadataOptions.Journal,
					(o, e) =>
					{
						var viewer = (o as PdfViewer);
						viewer.UpdateDocLayout();
						viewer.OnTilesCountChanged(EventArgs.Empty);
					}, 
					(v, o) => 
					{
						return (int)o < 2 ? 2 : o;
					}));

		/// <summary>
		/// DependencyProperty as the backing store for <see cref="MouseMode"/>
		/// </summary>
		public static readonly DependencyProperty MouseModeProperty =
			DependencyProperty.Register("MouseMode", typeof(MouseModes), typeof(PdfViewer),
				new FrameworkPropertyMetadata(MouseModes.Default,
					 FrameworkPropertyMetadataOptions.Journal,
					(o, e) =>
					{
						var viewer = (o as PdfViewer);
						viewer.OnMouseModeChanged(EventArgs.Empty);
					}));

		/// <summary>
		/// DependencyProperty as the backing store for <see cref="ShowLoadingIcon"/>
		/// </summary>
		public static readonly DependencyProperty ShowLoadingIconProperty =
			DependencyProperty.Register("ShowLoadingIcon", typeof(bool), typeof(PdfViewer),
				new FrameworkPropertyMetadata(true,
					 FrameworkPropertyMetadataOptions.Journal,
					(o, e) =>
					{
						var viewer = (o as PdfViewer);
						viewer.OnShowLoadingIconChanged(EventArgs.Empty);
					}));

		/// <summary>
		/// DependencyProperty as the backing store for <see cref="UseProgressiveRender"/>
		/// </summary>
		public static readonly DependencyProperty UseProgressiveRenderProperty =
			DependencyProperty.Register("UseProgressiveRender", typeof(bool), typeof(PdfViewer),
				new FrameworkPropertyMetadata(true,
					 FrameworkPropertyMetadataOptions.Journal,
					(o, e) =>
					{
						var viewer = (o as PdfViewer);
						viewer.UpdateDocLayout();
						viewer.OnUseProgressiveRenderChanged(EventArgs.Empty);
					}));

		/// <summary>
		/// DependencyProperty as the backing store for <see cref="LoadingIconText"/>
		/// </summary>
		public static readonly DependencyProperty LoadingIconTextProperty =
			DependencyProperty.Register("LoadingIconText", typeof(string), typeof(PdfViewer),
				new FrameworkPropertyMetadata("",
					 FrameworkPropertyMetadataOptions.Journal,
					(o, e) =>
					{
						var viewer = (o as PdfViewer);
						viewer.OnLoadingIconTextChanged(EventArgs.Empty);
					}));

		/// <summary>
		/// DependencyProperty as the backing store for <see cref="PageAutoDispose"/>
		/// </summary>
		public static readonly DependencyProperty PageAutoDisposeProperty =
			DependencyProperty.Register("PageAutoDispose", typeof(bool), typeof(PdfViewer),
				new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.Journal));

		/// <summary>
		/// DependencyProperty as the backing store for <see cref="OptimizedLoadThreshold"/>
		/// </summary>
		public static readonly DependencyProperty OptimizedLoadThresholdProperty =
			DependencyProperty.Register("OptimizedLoadThreshold", typeof(int), typeof(PdfViewer),
				new FrameworkPropertyMetadata(1000));
        #endregion

        #region Public properties (dependency)
        /// <summary>
        /// Gets or sets blend mode which is used in drawing of acro forms.
        /// </summary>
        /// <remarks>
        /// <para>Default value: <strong>FXDIB_BLEND_MULTIPLY</strong></para>
        /// </remarks>
        public BlendTypes FormsBlendMode
		{
			get { return (BlendTypes)GetValue(FormsBlendModeProperty); }
			set { SetValue(FormsBlendModeProperty, value); }
		}


		/// <summary>
		/// Gets or sets the PDF document associated with the current PdfViewer control.
		/// </summary>
		/// <remarks>It's a dependency property. Please find more details here: <see cref="PdfViewer.DocumentProperty"/></remarks>
		public PdfDocument Document
        {
            get { return (PdfDocument)GetValue(DocumentProperty); }
            set { SetValue(DocumentProperty, value); }
        }

		/// <summary>
		/// Gets or sets the background color for the control under PDF page.
		/// </summary>
		/// <remarks>It's a dependency property. Please find more details here: <see cref="PdfViewer.PageBackColorProperty"/></remarks>
		public Color PageBackColor
		{
			get { return (Color)GetValue(PageBackColorProperty); }
			set { SetValue(PageBackColorProperty, value); }
		}

		/// <summary>
		/// Specifies space between pages margins
		/// </summary>
		/// <remarks>It's a dependency property. Please find more details here: <see cref="PdfViewer.PageMarginProperty"/></remarks>
		public Thickness PageMargin
		{
			get { return (Thickness)GetValue(PageMarginProperty); }
			set { SetValue(PageMarginProperty, value); }
		}

		/// <summary>
		/// Gets or sets the padding inside a control.
		/// </summary>
		/// <remarks>It's a dependency property. Please find more details here: <see cref="PdfViewer.PaddingProperty"/></remarks>
		public new Thickness Padding
		{
			get { return (Thickness)GetValue(PaddingProperty); }
			set { SetValue(PaddingProperty, value); }
		}

		/// <summary>
		/// Gets or sets the border color of the page
		/// </summary>
		/// <remarks>It's a dependency property. Please find more details here: <see cref="PdfViewer.PageBorderColorProperty"/></remarks>
		public Color PageBorderColor
		{
			get { return (Color)GetValue(PageBorderColorProperty); }
			set { SetValue(PageBorderColorProperty, value); }
		}

		/// <summary>
		/// Control how the PdfViewer will handle  pages placement and control sizing
		/// </summary>
		/// <remarks>It's a dependency property. Please find more details here: <see cref="PdfViewer.SizeModeProperty"/></remarks>
		public SizeModes SizeMode
		{
			get { return (SizeModes)GetValue(SizeModeProperty); }
			set { SetValue(SizeModeProperty, value); }
		}

		/// <summary>
		/// Gets or sets the selection color of the control.
		/// </summary>
		/// <remarks>It's a dependency property. Please find more details here: <see cref="PdfViewer.TextSelectColorProperty"/></remarks>
		public Color TextSelectColor
		{
			get { return (Color)GetValue(TextSelectColorProperty); }
			set { SetValue(TextSelectColorProperty, value); }
		}

		/// <summary>
		/// Gets or set the highlight color of the form fields in the document.
		/// </summary>
		/// <remarks>It's a dependency property. Please find more details here: <see cref="PdfViewer.FormHighlightColorProperty"/></remarks>
		public Color FormHighlightColor
		{
			get { return (Color)GetValue(FormHighlightColorProperty); }
			set { SetValue(FormHighlightColorProperty, value); }
		}

		/// <summary>
		/// This property allows you to scale the PDF page. To take effect the <see cref="SizeMode"/> property should be Zoom
		/// </summary>
		/// <remarks>It's a dependency property. Please find more details here: <see cref="PdfViewer.ZoomProperty"/></remarks>
		public float Zoom
		{
			get { return (float)GetValue(ZoomProperty); }
			set { SetValue(ZoomProperty, value); }
		}

		/// <summary>
		/// Gets selected text from PdfView control
		/// </summary>
		/// <remarks>It's a dependency property. Please find more details here: <see cref="PdfViewer.SelectedTextProperty"/></remarks>
		public string SelectedText
		{
            get { return (string)GetValue(SelectedTextProperty); }
		}

		/// <summary>
		/// Control how the PdfViewer will display pages
		/// </summary>
		/// <remarks>It's a dependency property. Please find more details here: <see cref="PdfViewer.ViewModeProperty"/></remarks>
		public ViewModes ViewMode
		{
			get { return (ViewModes)GetValue(ViewModeProperty); }
			set { SetValue(ViewModeProperty, value); }
		}

		/// <summary>
		/// Gets or sets the page separator color.
		/// </summary>
		/// <remarks>It's a dependency property. Please find more details here: <see cref="PdfViewer.PageSeparatorColorProperty"/></remarks>
		public Color PageSeparatorColor
		{
			get { return (Color)GetValue(PageSeparatorColorProperty); }
			set { SetValue(PageSeparatorColorProperty, value); }
		}

		/// <summary>
		/// Determines whether the page separator is visible or hidden
		/// </summary>
		/// <remarks>It's a dependency property. Please find more details here: <see cref="PdfViewer.ShowPageSeparatorProperty"/></remarks>
		public bool ShowPageSeparator
		{
			get { return (bool)GetValue(ShowPageSeparatorProperty); }
			set { SetValue(ShowPageSeparatorProperty, value); }
		}

		/// <summary>
		/// Gets or sets the current page highlight color.
		/// </summary>
		/// <remarks>It's a dependency property. Please find more details here: <see cref="PdfViewer.CurrentPageHighlightColorProperty"/></remarks>
		public Color CurrentPageHighlightColor
		{
			get { return (Color)GetValue(CurrentPageHighlightColorProperty); }
			set { SetValue(CurrentPageHighlightColorProperty, value); }
		}

		/// <summary>
		/// Determines whether the current page's highlight is visible or hidden.
		/// </summary>
		/// <remarks>It's a dependency property. Please find more details here: <see cref="PdfViewer.ShowCurrentPageHighlightProperty"/></remarks>
		public bool ShowCurrentPageHighlight
		{
			get { return (bool)GetValue(ShowCurrentPageHighlightProperty); }
			set { SetValue(ShowCurrentPageHighlightProperty, value); }
		}

		/// <summary>
		/// Gets or sets the vertical alignment of page in the control.
		/// </summary>
		/// <remarks>It's a dependency property. Please find more details here: <see cref="PdfViewer.PageVAlignProperty"/></remarks>
		public VerticalAlignment PageVAlign
		{
			get { return (VerticalAlignment)GetValue(PageVAlignProperty); }
			set { SetValue(PageVAlignProperty, value); }
		}

		/// <summary>
		/// Gets or sets the horizontal alignment of page in the control.
		/// </summary>
		/// <remarks>It's a dependency property. Please find more details here: <see cref="PdfViewer.PageHAlignProperty"/></remarks>
		public HorizontalAlignment PageHAlign
		{
			get { return (HorizontalAlignment)GetValue(PageHAlignProperty); }
			set { SetValue(PageHAlignProperty, value); }
		}


		/// <summary>
		/// Gets or sets a RenderFlags. None for normal display, or combination of <see cref="RenderFlags"/>
		/// </summary>
		/// <remarks>It's a dependency property. Please find more details here: <see cref="PdfViewer.RenderFlagsProperty"/></remarks>
		public RenderFlags RenderFlags
		{
			get { return (RenderFlags)GetValue(RenderFlagsProperty); }
			set { SetValue(RenderFlagsProperty, value); }
		}

		/// <summary>
		/// Gets or sets visible page count for tiles view mode
		/// </summary>
		/// <remarks>It's a dependency property. Please find more details here: <see cref="PdfViewer.TilesCountProperty"/></remarks>
		public int TilesCount
		{
			get { return (int)GetValue(TilesCountProperty); }
			set { SetValue(TilesCountProperty, value); }
		}

		/// <summary>
		/// Gets or sets mouse mode for PdfViewer control
		/// </summary>
		/// <remarks>It's a dependency property. Please find more details here: <see cref="PdfViewer.MouseModeProperty"/></remarks>
		public MouseModes MouseMode
		{
			get { return (MouseModes)GetValue(MouseModeProperty); }
			set { SetValue(MouseModeProperty, value); }
		}

		/// <summary>
		/// Determines whether the page's loading icon should be shown
		/// </summary>
		/// <remarks>It's a dependency property. Please find more details here: <see cref="PdfViewer.ShowLoadingIconProperty"/></remarks>
		public bool ShowLoadingIcon
		{
			get { return (bool)GetValue(ShowLoadingIconProperty); }
			set { SetValue(ShowLoadingIconProperty, value); }
		}

		/// <summary>
		/// If true the progressive rendering is used for render page
		/// </summary>
		/// <remarks>It's a dependency property. Please find more details here: <see cref="PdfViewer.UseProgressiveRenderProperty"/></remarks>
		public bool UseProgressiveRender
		{
			get { return (bool)GetValue(UseProgressiveRenderProperty); }
			set { SetValue(UseProgressiveRenderProperty, value); }
		}

		/// <summary>
		/// Gets or sets loading icon text in progressive rendering mode
		/// </summary>
		/// <remarks>It's a dependency property. Please find more details here: <see cref="PdfViewer.LoadingIconTextProperty"/></remarks>
		public string LoadingIconText
		{
			get { return (string)GetValue(LoadingIconTextProperty); }
			set { SetValue(LoadingIconTextProperty, value); }
		}

		/// <summary>
		/// Gets or sets a value indicating whether the PdfViewer will dispose any pages placed outside of its visible boundaries.
		/// </summary>
		public bool PageAutoDispose
		{
			get { return (bool)GetValue(PageAutoDisposeProperty); }
			set { SetValue(PageAutoDisposeProperty, value); }
		}

		/// <summary>
		/// Gets or sets the threshold value in the number of pages after which the optimized document load is activated.
		/// </summary>
		/// <value>Default: 1000</value>
		public int OptimizedLoadThreshold
		{
			get { return (int)GetValue(OptimizedLoadThresholdProperty); }
			set { SetValue(OptimizedLoadThresholdProperty, value); }
		}
		#endregion

		#region Public Properties
		/// <summary>
		/// Gets the Forms object associated with the current PdfViewer control.
		/// </summary>
		/// <remarks>The FillForms object are used for the correct processing of forms within the PdfViewer control</remarks>
		public PdfForms FillForms { get { return _fillForms; } }

		/// <summary>
		/// Gets information about selected text in a PdfView control
		/// </summary>
		public SelectInfo SelectInfo { get { return NormalizeSelectionInfo(); } }

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
		[Obsolete("This property is ignored now", false)]
        [ReadOnly(true)]
        [Browsable(false)]
        public bool AllowSetDocument { get; set; }

		/// <summary>
		/// Gets information about highlighted text in a PdfView control
		/// </summary>
		public SortedDictionary<int, List<HighlightInfo>> HighlightedTextInfo { get { return _highlightedText; } }
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
			if (Document.Pages.Count == 0)
				return;
			if (index < 0 || index > Document.Pages.Count - 1)
				return;

			if (ViewMode == ViewModes.SinglePage || ViewMode == ViewModes.TilesLine)
			{
				if (index != CurrentIndex)
				{
					SetCurrentPage(index);
					_prPages.ReleaseCanvas();
				}
				InvalidateVisual();
			}
			else
			{
				var rect = renderRects(index);
				if (rect.Width == 0 || rect.Height == 0)
					return;
				SetVerticalOffset(rect.Y);
				SetHorizontalOffset(rect.X);
			}
		}

		/// <summary>
		/// Scrolls the control view to the specified character on the current page
		/// </summary>
		/// <param name="charIndex">Character index</param>
		public void ScrollToChar(int charIndex)
		{
			ScrollToChar(CurrentIndex, charIndex);
		}

		/// <summary>
		/// Scrolls the control view to the specified character on the specified page
		/// </summary>
		/// <param name="charIndex">Character index</param>
		/// <param name="pageIndex">Zero-based index of a page.</param>
		public void ScrollToChar(int pageIndex, int charIndex)
		{
			if (Document == null)
				return;
			if (Document.Pages.Count == 0)
				return;
			if (pageIndex < 0)
				return;
			var page = Document.Pages[pageIndex];
			int cnt = page.Text.CountChars;
			if (charIndex < 0)
				charIndex = 0;
			if (charIndex >= cnt)
				charIndex = cnt - 1;
			if (charIndex < 0)
				return;
			var ti = page.Text.GetTextInfo(charIndex, 1);
			if (ti.Rects == null || ti.Rects.Count == 0)
				return;

			ScrollToPage(pageIndex);
			var pt = PageToClient(pageIndex, new Point(ti.Rects[0].left, ti.Rects[0].top));
			var curPt = _autoScrollPosition;
			SetVerticalOffset(pt.Y - curPt.Y);
			SetHorizontalOffset(pt.X - curPt.X);
		}

		/// <summary>
		/// Scrolls the control view to the specified point on the specified page
		/// </summary>
		/// <param name="pageIndex">Zero-based index of a page.</param>
		/// <param name="pagePoint">Point on the page in the page's coordinate system</param>
		public void ScrollToPoint(int pageIndex, Point pagePoint)
		{
			if (Document == null)
				return;
			int count = Document.Pages.Count;
			if (count == 0)
				return;
			if (pageIndex < 0 || pageIndex > count - 1)
				return;

			ScrollToPage(pageIndex);
			var pt = PageToClient(pageIndex, pagePoint);
			var curPt = _autoScrollPosition;
			SetVerticalOffset(pt.Y - curPt.Y);
			SetHorizontalOffset(pt.X - curPt.X);
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
			_isShowSelection = true;
			InvalidateVisual();
            GenerateSelectedTextProperty();
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
            GenerateSelectedTextProperty();
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
			page.DeviceToPage((int)ar.X, (int)ar.Y, (int)ar.Width, (int)ar.Height, PageRotation(page), (int)pt.X, (int)pt.Y, out pX, out pY);
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
			page.PageToDevice((int)ar.X, (int)ar.Y, (int)ar.Width, (int)ar.Height, PageRotation(page), (float)pt.X, (float)pt.Y, out dX, out dY);
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
            HighlightText(pageIndex, charIndex, charsCount, color, new FS_RECTF());
        }

        /// <summary>
        /// Highlight text on the page
        /// </summary>
        /// <param name="pageIndex">Zero-based index of the page</param>
        /// <param name="charIndex">Zero-based char index on the page.</param>
        /// <param name="charsCount">The number of highlighted characters on the page or -1 for highlight text from charIndex to end of the page.</param>
        /// <param name="color">Highlight color</param>
        /// <param name="inflate">A delta values for each edge of the rectangles of the highlighted text.</param>
        public void HighlightText(int pageIndex, int charIndex, int charsCount, Color color, FS_RECTF inflate)
		{
			//normalize all user input
			if (pageIndex < 0)
				pageIndex = 0;
			if (pageIndex > Document.Pages.Count - 1)
				pageIndex = Document.Pages.Count - 1;

            IntPtr ph = Pdfium.FPDF_LoadPage(Document.Handle, pageIndex);
            IntPtr th = Pdfium.FPDFText_LoadPage(ph);
            int charsCnt = Pdfium.FPDFText_CountChars(th);
            Pdfium.FPDFText_ClosePage(th);
            Pdfium.FPDF_ClosePage(ph);

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

			var newEntry = new HighlightInfo() { CharIndex = charIndex, CharsCount = charsCount, Color = color, Inflate = inflate };

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
        [Obsolete("This method is obsolete. Please use HighlightSelectedText instead", false)]
        public void HilightSelectedText(Color color)
        {
            HighlightSelectedText(color);
        }

        /// <summary>
        /// Highlight selected text on the page by specified color
        /// </summary>
        /// <param name="color">Highlight color</param>
        public void HighlightSelectedText(Color color)
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
        [Obsolete("This method is obsolete. Please use RemoveHighlightFromSelectedText instead", false)]
        public void RemoveHilightFromSelectedText()
        {
            RemoveHighlightFromSelectedText();
        }

        /// <summary>
        /// Removes highlight from selected text
        /// </summary>
        public void RemoveHighlightFromSelectedText()
		{
			HighlightSelectedText(Helpers.ColorEmpty);
		}

		/// <summary>
		/// Ensures that all sizes and positions of pages of a PdfViewer control are properly updated for layout.
		/// </summary>
		public void UpdateDocLayout()
		{
			_prPages.ReleaseCanvas(); //something changed. Release canvas
			_viewport = new Size(ActualWidth, ActualHeight);
			if (Document == null || Document.Pages.Count <= 0)
			{
				_renderRects = null;
				InvalidateVisual();
				return;
			}

			SaveScrollPoint();
			_renderRects = new RenderRect[Document.Pages.Count];
			CalcPages();
			RestoreScrollPoint();
			InvalidateVisual();
		}

		/// <summary>
		/// Clear internal render buffer for rerender pages in Progressive mode
		/// </summary>
		public void ClearRenderBuffer()
		{
			_prPages.ReleaseCanvas();
		}

		/// <summary>
		/// Calculates the actual rectangle of the specified page in client coordinates
		/// </summary>
		/// <param name="index">Zero-based page index</param>
		/// <returns>Calculated rectangle</returns>
		public Rect CalcActualRect(int index)
		{
			if (_renderRects == null)
				return default(Rect);

			var rect = renderRects(index);
			rect.X += _autoScrollPosition.X;
			rect.Y += _autoScrollPosition.Y;
			return rect;
		}

        /// <summary>
        /// Get a collection of rectangles that represent the selected text on a specified page.
        /// </summary>
        /// <param name="pageIndex">Zero-based index of a page.</param>
        /// <returns>A collection of rectangles or an empty collection if the page does not contain selected text.</returns>
        /// <remarks>The rectangles are given in the user control coordinate system.</remarks>
        public List<Int32Rect> GetSelectedRects(int pageIndex)
        {
            return GetSelectedRects(pageIndex, SelectInfo);
        }

        /// <summary>
        /// Get a collection of rectangles that represent the selected text on a specific page and in accordance with the specified <see cref="SelectInfo"/> structure.
        /// </summary>
        /// <param name="pageIndex">Zero-based index of a page.</param>
        /// <param name="selInfo">A<see cref="SelectInfo"/> structure that represent the selected text.</param>
        /// <returns>A collection of rectangles or an empty collection if the page does not contain selected text.</returns>
        /// <remarks>The rectangles are given in the user control coordinate system.</remarks>
        public List<Int32Rect> GetSelectedRects(int pageIndex, SelectInfo selInfo)
        {
            if (pageIndex >= selInfo.StartPage && pageIndex <= selInfo.EndPage)
            {
                int cnt = Document.Pages[pageIndex].Text.CountChars;
                int s = 0;
                if (pageIndex == selInfo.StartPage)
                    s = selInfo.StartIndex;

                int len = cnt;
                if (pageIndex == selInfo.EndPage)
                    len = (selInfo.EndIndex + 1) - s;

                int s2 = s + len;
                int len2 = cnt - s2;

                var ti = GetRectsFromTextInfoWithoutSpaceCharacter(pageIndex, s, len);
                var tiBefore = _smoothSelection == SmoothSelection.ByLine && s > 0 ? GetRectsFromTextInfoWithoutSpaceCharacter(pageIndex, 0, s) : null;
                var tiAfter = _smoothSelection == SmoothSelection.ByLine && s2 < cnt && len2 > 0 ? GetRectsFromTextInfoWithoutSpaceCharacter(pageIndex, s2, len2) : null;
                return NormalizeRects(ti, pageIndex, tiBefore, tiAfter);
            }
            else
                return new List<Int32Rect>();
        }

        /// <summary>
        /// Get a collection of rectangles that represent the highlighted text on a specific page and in accordance with the specified <see cref="HighlightInfo"/> structure.
        /// </summary>
        /// <param name="pageIndex">Zero-based index of a page.</param>
        /// <param name="selInfo">A <see cref="HighlightInfo"/> structure that represent the highlighted text.</param>
        /// <returns>A collection of rectangles or an empty collection if the page does not contain highlighted text.</returns>
        /// <remarks>The rectangles are given in the user control coordinate system.</remarks>
        public List<Int32Rect> GetHighlightedRects(int pageIndex, HighlightInfo selInfo)
        {
            int cnt = Document.Pages[pageIndex].Text.CountChars;
            int s = selInfo.CharIndex;
            int len = selInfo.CharsCount;

            int s2 = s + len;
            int len2 = cnt - s2;

            var ti = GetRectsFromTextInfoWithoutSpaceCharacter(pageIndex, s, len);
            if (selInfo.Inflate == default(FS_RECTF))
            {
                var tiBefore = _smoothSelection == SmoothSelection.ByLine && s > 0 ? GetRectsFromTextInfoWithoutSpaceCharacter(pageIndex, 0, s) : null;
                var tiAfter = _smoothSelection == SmoothSelection.ByLine && s2 < cnt && len2 > 0 ? GetRectsFromTextInfoWithoutSpaceCharacter(pageIndex, s2, len2) : null;
                return NormalizeRects(ti, pageIndex, tiBefore, tiAfter, selInfo.Inflate);
            }
            else
                return NormalizeRects(ti, pageIndex, null, null, selInfo.Inflate);
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
            PdfDocument doc = null;
            try
            {
				CloseDocument();
                if (Document != null)
                    return; //closing or changing was canceled
                Document = doc = PdfDocument.Load(path, _fillForms, password);
                if (Document == null)
                    return; //Can't set Document due to TwoWay binding mode or closing or changing was canceled
                _loadedByViewer = true;
				OnDocumentLoaded(EventArgs.Empty);
			}
			catch (NoLicenseException ex)
			{
				MessageBox.Show(ex.Message, Properties.Resources.InfoHeader, MessageBoxButton.OK, MessageBoxImage.Information);
			}
            finally
            {
                if (Document == null && doc != null)
                    doc.Dispose();
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
            PdfDocument doc = null;
            try
            {
				CloseDocument();
                if (Document != null)
                    return; //closing or changing was canceled
                Document = doc = PdfDocument.Load(stream, _fillForms, password);
                if (Document == null)
                    return; //Can't set Document due to TwoWay binding mode or closing or changing was canceled
                _loadedByViewer = true;
				OnDocumentLoaded(EventArgs.Empty);
			}
			catch (NoLicenseException ex)
			{
				MessageBox.Show(ex.Message, Properties.Resources.InfoHeader, MessageBoxButton.OK, MessageBoxImage.Information);
			}
            finally
            {
                if (Document == null && doc != null)
                    doc.Dispose();
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
            PdfDocument doc = null;
            try
            {
				CloseDocument();
                if (Document != null)
                    return; //closing or changing was canceled
                Document = doc = PdfDocument.Load(pdf, _fillForms, password);
                if (Document == null)
                    return; //Can't set Document due to TwoWay binding mode or closing or changing was canceled
                _loadedByViewer = true;
				OnDocumentLoaded(EventArgs.Empty);
			}
			catch (NoLicenseException ex)
			{
				MessageBox.Show(ex.Message, Properties.Resources.InfoHeader, MessageBoxButton.OK, MessageBoxImage.Information);
			}
            finally
            {
                if (Document == null && doc != null)
                    doc.Dispose();
            }
        }

		/// <summary>
		/// Close a loaded PDF document.
		/// </summary>
		public void CloseDocument()
		{
			Document = null;
		}

		#endregion

		#region Constructors and initialization
		/// <summary>
		/// Initializes a new instance of the PdfViewer class.
		/// </summary>
		public PdfViewer()
		{
            PdfCommon.DesignTimeActivation();
            LoadingIconText = Properties.Resources.LoadingText;
			Background = SystemColors.ControlDarkBrush;
            _smoothSelection = SmoothSelection.ByLine;
            _prPages.PaintBackground += (s, e) => DrawPageBackColor(_prPages.CanvasBitmap, e.Value.X, e.Value.Y, e.Value.Width, e.Value.Height);
            _fillForms = new PdfForms();
			CaptureFillForms(_fillForms);
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
		/// Raises the System.Windows.FrameworkElement.SizeChanged event, using the specified information as part of the eventual event data.
		/// </summary>
		/// <param name="sizeInfo">Details of the old and new size involved in the change.</param>
		protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
		{
			UpdateDocLayout();
			base.OnRenderSizeChanged(sizeInfo);
		}


		/// <summary>
		/// Invoked when an unhandled System.Windows.Input.Mouse.MouseLeave attached event
		/// is raised on this element. Implement this method to add class handling for this
		/// event.
		/// </summary>
		/// <param name="e">The System.Windows.Input.MouseEventArgs that contains the event data.</param>
		protected override void OnMouseLeave(MouseEventArgs e)
		{
			Mouse.OverrideCursor = null;
			base.OnMouseLeave(e);
		}

        /// <summary>
        /// When overridden in a derived class, participates in rendering operations that are directed by the layout system. 
        /// The rendering instructions for this element are not used directly when this method is invoked, and are instead 
        /// preserved for later asynchronous use by layout and drawing.
        /// </summary>
        /// <param name="drawingContext">The drawing instructions for a specific element. This context is provided to the layout system. </param>
        /// <remarks>
        /// The OnRender method can be overridden to add further graphical elements (not previously defined in a logical tree) to a rendered element, such as effects or adorners. A DrawingContext object is passed as an argument, which provides methods for drawing shapes, text, images or videos.
        /// Full page rendering is performed in the following order:
        /// <list type="bullet">
        /// <item><see cref="DrawPageBackColor"/></item>
        /// <item><see cref="RegenerateAnnots"/></item>
        /// <item><see cref="DrawPage"/></item>
        /// <item><see cref="DrawFillForms"/></item>
        /// <item><see cref="DrawFillFormsSelection(PdfBitmap, List{Rect})"/></item>
        /// <item><see cref="DrawTextHighlight(PdfBitmap, List{HighlightInfo}, int)"/></item>
        /// <item><see cref="DrawTextSelection(PdfBitmap, SelectInfo, int)"/></item>
        /// <item><see cref="DrawRenderedPagesToDevice"/></item>
        /// <item><see cref="DrawPageSeparators"/></item>
        /// <item><see cref="DrawLoadingIcon"/> if page is still drawing</item>
        /// <item><see cref="DrawPageBorder"/></item>
        /// <item><see cref="DrawFillFormsSelection(DrawingContext, List{Rect})"/></item>
        /// <item><see cref="DrawTextHighlight(DrawingContext, List{HighlightInfo}, int)"/></item>
        /// <item><see cref="DrawTextSelection(DrawingContext, SelectInfo, int)"/></item>
        /// <item><see cref="DrawCurrentPageHighlight"/></item>
        /// </list>
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

				int cw = Helpers.UnitsToPixels(ClientRect.Width);
				int ch = Helpers.UnitsToPixels(ClientRect.Height);
				if (cw <= 0 || ch <= 0)
					return;

				//Initialize the Canvas bitmap
				_prPages.InitCanvas(new Helpers.Int32Size(cw, ch));
				bool allPagesAreRendered = true;
                bool[] stillLoading = new bool[_endPage + 1];

                PdfBitmap formsBitmap = null;
				//Drawing PART 1. Page content into canvas and some other things
				for (int i = _startPage; i <= _endPage; i++)
				{
					//Actual coordinates of the page with the scroll
					Rect actualRect = CalcActualRect(i);
					if (!actualRect.IntersectsWith(ClientRect))
					{
                        if (PageAutoDispose && CanDisposePage(i))
                            Document.Pages[i].Dispose();  //do not dispose the page if it contains highlighted  or selected text. fix for #052325
                        continue; //Page is invisible. Skip it
					}

					if (!_renderRects[i].IsChecked)
					{
						SaveScrollPoint();
						CalcPages();
						RestoreScrollPoint();
						actualRect = CalcActualRect(i);
					}

                    //Recreate annotations which have no AP stream.
                    RegenerateAnnots(i);
                    if (_prPages.CanvasBitmap == null)
						_prPages.InitCanvas(new Helpers.Int32Size(cw, ch)); //The canvas was dropped due to the execution of scripts on the page while it loading inside RegenerateAnnots.

                    //Draw page
                    bool isPageDrawn = DrawPage(_prPages.CanvasBitmap, Document.Pages[i], actualRect);
					allPagesAreRendered &= isPageDrawn;
                    stillLoading[i] = !isPageDrawn;

                    //Create new bitmap for fillforms if need
                    if (formsBitmap == null)
                        formsBitmap = new PdfBitmap(_prPages.CanvasSize.Width, _prPages.CanvasSize.Height, true);
                    //Copy image of rendered page from canvas bitmap to fillforms bitmap
                    int ax = Helpers.UnitsToPixels(actualRect.X);
                    int ay = Helpers.UnitsToPixels(actualRect.Y);
                    int aw = Helpers.UnitsToPixels(actualRect.Width);
                    int ah = Helpers.UnitsToPixels(actualRect.Height);
                    Pdfium.FPDFBitmap_CompositeBitmap(formsBitmap.Handle, ax, ay, aw, ah, _prPages.CanvasBitmap.Handle, ax, ay, BlendTypes.FXDIB_BLEND_NORMAL);

                    if (isPageDrawn)
					{
						//Draw fillForms to newly create bitmap
						DrawFillForms(formsBitmap, Document.Pages[i], actualRect);
						//Draw fillform selection
						DrawFillFormsSelection(formsBitmap, _selectedRectangles);
						//Draw text highlight
						if (_highlightedText.ContainsKey(i))
							DrawTextHighlight(formsBitmap, _highlightedText[i], i);
						//Draw text selection
						DrawTextSelection(formsBitmap, selTmp, i);
					}
					
					//Calc coordinates for page separator
					CalcPageSeparator(actualRect, i, ref separator);
				}

				//Draw canvas
				DrawRenderedPagesToDevice(drawingContext, _prPages.CanvasBitmap, formsBitmap, _prPages.CanvasSize.Width, _prPages.CanvasSize.Height);
				if (formsBitmap != null)
					formsBitmap.Dispose();

				//Draw pages separators
				DrawPageSeparators(drawingContext, ref separator);

				//Drawing PART 2.
				for (int i = _startPage; i <= _endPage; i++)
				{
					//Actual coordinates of the page with the scroll
					Rect actualRect = CalcActualRect(i);
					if (!actualRect.IntersectsWith(ClientRect))
						continue; //Page is invisible. Skip it

                    if (ShowLoadingIcon && stillLoading[i]) //draw loading icons
                        DrawLoadingIcon(drawingContext, Document.Pages[i], actualRect);
                    //Draw page border
                    DrawPageBorder(drawingContext, actualRect);
					//Draw fillforms selection
					DrawFillFormsSelection(drawingContext, _selectedRectangles);
					//Draw text highlight
					if (_highlightedText.ContainsKey(i))
						DrawTextHighlight(drawingContext, _highlightedText[i], i);
					//Draw text selectionn
					DrawTextSelection(drawingContext, selTmp, i);
					//Draw current page highlight
					DrawCurrentPageHighlight(drawingContext, i, actualRect);
				}

				if (!allPagesAreRendered)
					StartInvalidateTimer();
				else if ((RenderFlags & (RenderFlags.FPDF_THUMBNAIL | RenderFlags.FPDF_HQTHUMBNAIL)) != 0)
					_prPages.ReleaseCanvas();
				else if (!UseProgressiveRender)
					_prPages.ReleaseCanvas();

				_selectedRectangles.Clear();
			}
		}

        /// <summary>
        /// Create an appearance stream for annotations which do not have this one.
        /// </summary>
        /// <param name="pageIndex">Page index with which annotations are associated.</param>
        protected virtual void RegenerateAnnots(int pageIndex)
        {
            var pageHandle = Document.Pages[pageIndex].Handle;
            if (!Pdfium.IsFullAPI)
                return;
            var noapp = Pdfium.FPDFTOOLS_GetAnnotsWithoutAP(pageHandle);
            if (noapp != null && noapp.Length > 0)
            {
                var annots = Document.Pages[pageIndex].Annots;
                int cnt = annots.Count;
                foreach (int idx in noapp)
                {
                    if (idx < 0 || idx >= cnt)
                        continue;
                    var annot = annots[idx];
                    if (annot is PdfMarkupAnnotation)
                        (annot as PdfMarkupAnnotation).RegenerateAppearances();
                }
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
                        if (MouseMode != MouseModes.PanTool && MouseMode != MouseModes.None)
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
                    var cursor = CursorTypes.Arrow;
                    switch (MouseMode)
					{
						case MouseModes.Default:
                            cursor = ProcessMouseMoveForDefaultTool(page_point, idx);
                            var cursor2 = ProcessMouseMoveForSelectTextTool(page_point, idx);
                            if(cursor2!= CursorTypes.Arrow && _mousePressed && !_mousePressedInLink)
                                cursor = cursor2;
                            else
                                cursor = cursor == CursorTypes.Arrow ? cursor2 : cursor;
                            break;
						case MouseModes.SelectTextTool:
                            cursor = ProcessMouseMoveForSelectTextTool(page_point, idx);
							break;
						case MouseModes.PanTool:
							cursor = ProcessMouseMoveForPanTool(loc);
							break;
					}
                    InternalSetCursor(cursor);
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
                    GenerateSelectedTextProperty();

                Point page_point;
				var loc = e.GetPosition(this);
				int idx = DeviceToPage(loc.X, loc.Y, out page_point);
				if (idx >= 0)
				{
                    if (MouseMode != MouseModes.PanTool && MouseMode != MouseModes.None)
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
        /// <param name="bitmap">The drawing surface.</param>
        /// <param name="x">Actual X position of the page</param>
        /// <param name="y">Actual Y position of the page</param>
        /// <param name="width">Actual width of the page</param>
        /// <param name="height">Actual height of the page</param>
        /// <remarks>
        /// Please see the remarks section of <see cref="OnRender"/> for getting more info about page rendering order.
        /// </remarks>
        protected virtual void DrawPageBackColor(PdfBitmap bitmap, int x, int y, int width, int height)
		{
            bitmap.FillRectEx(x, y, width, height, Helpers.ToArgb(PageBackColor));
		}

        /// <summary>
        /// Draws page content and fillforms
        /// </summary>
        /// <param name="bitmap">The drawing surface.</param>
        /// <param name="page">Page to be drawn</param>
        /// <param name="actualRect">Page bounds in control coordinates</param>
        /// <remarks>
        /// Please see the remarks section of <see cref="OnRender"/> for getting more info about page rendering order.
        /// </remarks>
        /// <returns>true if page was rendered; false if any error is occurred or page is still rendering.</returns>
        protected virtual bool DrawPage(PdfBitmap bitmap, PdfPage page, Rect actualRect)
		{
			if (actualRect.Width <= 0 || actualRect.Height <= 0)
				return true;
			int width = Helpers.UnitsToPixels(actualRect.Width);
			int height = Helpers.UnitsToPixels(actualRect.Height);
			if (width <= 0 || height <= 0)
				return true;

			var pageRect = new Int32Rect(
				Helpers.UnitsToPixels(actualRect.X), 
				Helpers.UnitsToPixels(actualRect.Y), width, height);
			return _prPages.RenderPage(page, pageRect, PageRotation(page), RenderFlags, UseProgressiveRender);
		}

        /// <summary>
        /// Draw fill forms
        /// </summary>
		/// <param name="bitmap">The drawing surface.</param>
        /// <param name="page">Page to be drawn</param>
        /// <param name="actualRect">Page bounds in control coordinates</param>
        /// <remarks>
        /// Please see the remarks section of <see cref="OnRender"/> for getting more info about page rendering order.
        /// </remarks>
        protected virtual void DrawFillForms(PdfBitmap bitmap, PdfPage page, Rect actualRect)
		{
			int x = Helpers.UnitsToPixels(actualRect.X);
			int y = Helpers.UnitsToPixels(actualRect.Y);
			int width = Helpers.UnitsToPixels(actualRect.Width);
			int height = Helpers.UnitsToPixels(actualRect.Height);

			//Draw fillforms to bitmap
			page.RenderForms(bitmap, x, y, width, height, PageRotation(page), RenderFlags);
		}


        /// <summary>
        /// Draws highlights inside a forms
        /// </summary>
        /// <param name="bitmap">The drawing surface.</param>
        /// <param name="selectedRectangles">A collection of rectangles to be drawn</param>
        /// <remarks>
        /// Please see the remarks section of <see cref="OnRender"/> for getting more info about page rendering order.
        /// </remarks>
        protected virtual void DrawFillFormsSelection(PdfBitmap bitmap, List<Rect> selectedRectangles)
		{
			if (selectedRectangles == null)
				return;
			foreach (var selectRc in selectedRectangles)
			{
				int x = Helpers.UnitsToPixels(selectRc.X);
				int y = Helpers.UnitsToPixels(selectRc.Y);
				int w = Helpers.UnitsToPixels(selectRc.Width);
				int h = Helpers.UnitsToPixels(selectRc.Height);

				bitmap.FillRectEx(x, y, w, h, Helpers.ToArgb(TextSelectColor), FormsBlendMode);
			}
		}

        /// <summary>
        /// Draws text highlights
        /// </summary>
        /// <param name="bitmap">The drawing surface.</param>
        /// <param name="entries">Highlights info.</param>
        /// <param name="pageIndex">Page index to be drawn</param>
        /// <remarks>
        /// Please see the remarks section of <see cref="OnRender"/> for getting more info about page rendering order.
        /// </remarks>
        protected virtual void DrawTextHighlight(PdfBitmap bitmap, List<HighlightInfo> entries, int pageIndex)
		{
			if (entries == null)
				return;

			foreach (var e in entries)
			{
                var rects = GetHighlightedRects(pageIndex, e);
                foreach (var r in rects)
                    bitmap.FillRectEx(r.X, r.Y, r.Width, r.Height, Helpers.ToArgb(e.Color), FormsBlendMode);
			}
		}

        /// <summary>
        /// Draws text selection
        /// </summary>
        /// <param name="bitmap">The drawing surface.</param>
        /// <param name="selInfo">Selection info</param>
        /// <param name="pageIndex">Page index to be drawn</param>
        /// <remarks>
        /// Please see the remarks section of <see cref="OnRender"/> for getting more info about page rendering order.
        /// </remarks>
        protected virtual void DrawTextSelection(PdfBitmap bitmap, SelectInfo selInfo, int pageIndex)
		{
			if (selInfo.StartPage < 0 || !_isShowSelection)
				return;
			if (pageIndex >= selInfo.StartPage && pageIndex <= selInfo.EndPage)
			{
                var rects = GetSelectedRects(pageIndex, selInfo);
                foreach (var r in rects)
                    bitmap.FillRectEx(r.X, r.Y, r.Width, r.Height, Helpers.ToArgb(TextSelectColor), FormsBlendMode);
			}
		}

        /// <summary>
        /// Draw loading icon
        /// </summary>
        /// <param name="drawingContext">Drawing context</param>
        /// <param name="page">Page to be drawn</param>
        /// <param name="actualRect">Page bounds in control coordinates</param>
        /// <remarks>
        /// Please see the remarks section of <see cref="OnRender"/> for getting more info about page rendering order.
        /// </remarks>
        protected virtual void DrawLoadingIcon(DrawingContext drawingContext, PdfPage page, Rect actualRect)
		{
			Typeface tf = new Typeface("Tahoma");
            var ft = new FormattedText(
                LoadingIconText,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                tf, 14, Brushes.Black
#if DOTNET462 || DOTNET47 || DOTNET471 || DOTNET472 || DOTNET48
                , Helpers.Dpi / 96
#endif
                );
			ft.MaxTextWidth = actualRect.Width;
			ft.MaxTextHeight = actualRect.Height;
			ft.TextAlignment = TextAlignment.Left;

			double x = (actualRect.Width - ft.Width) / 2 + actualRect.X;
			if (x < actualRect.X)
				x = actualRect.X;
			double y = (actualRect.Height - ft.Height) / 2 + actualRect.Y;
			if (y < actualRect.Y)
				y = actualRect.Y;
			drawingContext.DrawText(ft, new Point(x, y));
		}

        /// <summary>
        /// Draws page's border
        /// </summary>
        /// <param name="drawingContext">The drawing context</param>
        /// <param name="BBox">Page's bounding box</param>
        /// <remarks>
        /// Please see the remarks section of <see cref="OnRender"/> for getting more info about page rendering order.
        /// </remarks>
        protected virtual void DrawPageBorder(DrawingContext drawingContext, Rect BBox)
		{
			//Draw page border
			Helpers.DrawRectangle(drawingContext, _pageBorderColorPen, BBox);
		}

        /// <summary>
        /// Left for backward compatibility. Actually the fillforms selection is drawn in <see cref="DrawFillFormsSelection(PdfBitmap, List{Rect})"/> method.
        /// </summary>
        /// <param name="drawingContext">The drawing instructions for a specific element. This context is provided to the layout system.</param>
        /// <param name="selectedRectangles">A collection of rectangles to be drawn</param>
        /// <remarks>
        /// Please see the remarks section of <see cref="OnRender"/> for getting more info about page rendering order.
        /// </remarks>
        protected virtual void DrawFillFormsSelection(DrawingContext drawingContext, List<Rect> selectedRectangles)
		{

		}

        /// <summary>
        /// Left for backward compatibility. Actually the text highlight is drawn in <see cref="DrawTextHighlight(PdfBitmap, List{HighlightInfo}, int)"/> method.
        /// </summary>
        /// <param name="drawingContext">The drawing instructions for a specific element. This context is provided to the layout system.</param>
        /// <param name="entries">Highlights info.</param>
        /// <param name="pageIndex">Page index to be drawn</param>
        /// <remarks>
        /// Please see the remarks section of <see cref="OnRender"/> for getting more info about page rendering order.
        /// </remarks>
        protected virtual void DrawTextHighlight(DrawingContext drawingContext, List<HighlightInfo> entries, int pageIndex)
		{

		}

        /// <summary>
        /// Left for backward compatibility. Actually the text selection is drawn in <see cref="DrawTextSelection(PdfBitmap, SelectInfo, int)"/> method.
        /// </summary>
        /// <param name="drawingContext">The drawing instructions for a specific element. This context is provided to the layout system.</param>
        /// <param name="selInfo">Selection info</param>
        /// <param name="pageIndex">Page index to be drawn</param>
        /// <remarks>
        /// Please see the remarks section of <see cref="OnRender"/> for getting more info about page rendering order.
        /// </remarks>
        protected virtual void DrawTextSelection(DrawingContext drawingContext, SelectInfo selInfo, int pageIndex)
		{

		}

        /// <summary>
        /// Draws current page highlight
        /// </summary>
        /// <param name="drawingContext">The drawing instructions for a specific element. This context is provided to the layout system.</param>
        /// <param name="pageIndex">Page index to be drawn</param>
        /// <param name="actualRect">Page bounds in control coordinates</param>
        /// <remarks>
        /// Please see the remarks section of <see cref="OnRender"/> for getting more info about page rendering order.
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
        /// Combine two buffers (rendered pages and forms) and draw them to graphics
        /// </summary>
        /// <param name="drawingContext">The drawing instructions for a specific element. This context is provided to the layout system.</param>
        /// <param name="canvasBitmap">Bitmap with rendered pages</param>
        /// <param name="formsBitmap">Bitmap with rendered forms</param>
        /// <param name="canvasWidth">Width of buffer</param>
        /// <param name="canvasHeight">Height of buffer</param>
        /// <remarks>
        /// This method should combine bitmaps with alpha blending and draw result to graphics surface.
        /// Please see the remarks section of <see cref="OnRender"/> for getting more info about page rendering order.
        /// </remarks>
        protected virtual void DrawRenderedPagesToDevice(DrawingContext drawingContext, PdfBitmap canvasBitmap, PdfBitmap formsBitmap, int canvasWidth, int canvasHeight)
		{
			//Convert PdfBitmap into Wpf WriteableBitmap
			int canvasStride = _prPages.CanvasBitmap.Stride;
			if (_canvasWpfBitmap == null || _canvasWpfBitmap.PixelWidth != canvasWidth || _canvasWpfBitmap.PixelHeight != canvasHeight)
				_canvasWpfBitmap = new WriteableBitmap(canvasWidth, canvasHeight, Helpers.Dpi, Helpers.Dpi, PixelFormats.Bgra32, null);


			if (formsBitmap == null)
				_canvasWpfBitmap.WritePixels(new Int32Rect(0, 0, canvasWidth, canvasHeight), canvasBitmap.Buffer, canvasStride * canvasHeight, canvasStride, 0, 0);
			else
				_canvasWpfBitmap.WritePixels(new Int32Rect(0, 0, canvasWidth, canvasHeight), formsBitmap.Buffer, canvasStride * canvasHeight, canvasStride, 0, 0);

            var _pixelOffset = Helpers.GetPixelOffset(this);
            Helpers.DrawImageUnscaled(drawingContext, _canvasWpfBitmap, _pixelOffset.X, _pixelOffset.Y);
		}

        /// <summary>
        /// Draws pages separatoes.
        /// </summary>
        /// <param name="drawingContext">The drawing instructions for a specific element. This context is provided to the layout system.</param>
        /// <param name="separator">List of pair of points what represents separator</param>
        /// <remarks>
        /// Please see the remarks section of <see cref="OnRender"/> for getting more info about page rendering order.
        /// </remarks>
        protected virtual void DrawPageSeparators(DrawingContext drawingContext, ref List<Point> separator)
		{
			if (separator == null || !ShowPageSeparator)
				return;

			for (int sep = 0; sep < separator.Count; sep += 2)
				drawingContext.DrawLine(_pageSeparatorColorPen, separator[sep], separator[sep + 1]);
		}
#endregion

#region Other protected methods
        /// <summary>
        /// Sets the cursor that is displayed when the mouse pointer is over the control.
        /// </summary>
        /// <param name="cursor">A <see cref="CursorTypes"/> that represents the cursor to display when the mouse pointer is over the control.</param>
        /// <remarks>You can override this method to change the logic of cursor setting in the control.</remarks>
        protected virtual void InternalSetCursor(CursorTypes cursor)
        {
            switch (cursor)
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

#region Private methods
        private bool CanDisposePage(int i)
        {
            if (_highlightedText.ContainsKey(i))
                return false;
            if (_selectInfo.StartPage < 0 || _selectInfo.EndPage < 0 || _selectInfo.StartIndex < 0 || _selectInfo.EndIndex < 0)
                return true;
            if (_selectInfo.StartPage >= i && _selectInfo.EndPage <= i)
                return false;

            return true;
        }

        private void SaveScrollPoint()
		{
			_scrollPointSaved = false;
			if (_renderRects != null)
			{
				_scrollPoint = ClientToPage(CurrentIndex, new Point(0, 0));
				_scrollPointSaved = true;
			}
		}

		private void RestoreScrollPoint()
		{
			if (_scrollPointSaved)
			{
				int idx = CurrentIndex;
				ScrollToPoint(CurrentIndex, _scrollPoint);
				CurrentIndex = idx;
			}
		}

		private void GenerateSelectedTextProperty()
        {
            string ret = "";
            if (Document != null)
            {
                var selTmp = NormalizeSelectionInfo();

                if (selTmp.StartPage >= 0 && selTmp.StartIndex >= 0)
                {
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
                }
            }
            SetValue(SelectedTextProperty, ret);
            OnSelectionChanged(EventArgs.Empty);
        }

        private CaptureInfo CaptureFillForms(PdfForms fillForms)
		{
			var ret = new CaptureInfo();
			if (fillForms == null)
				return ret;

			ret.forms = fillForms;
			ret.sync = fillForms.SynchronizationContext;

			fillForms.SynchronizationContext = SynchronizationContext.Current;
			ret.color = fillForms.SetHighlightColorEx(FormFieldTypes.FPDF_FORMFIELD_UNKNOWN, Helpers.ToArgb(FormHighlightColor));
			fillForms.AppBeep += FormsAppBeep;
			fillForms.DoGotoAction += FormsDoGotoAction;
			fillForms.DoNamedAction += FormsDoNamedAction;
			fillForms.GotoPage += FormsGotoPage;
			fillForms.Invalidate += FormsInvalidate;
			fillForms.OutputSelectedRect += FormsOutputSelectedRect;
			fillForms.SetCursor += FormsSetCursor;
			return ret;
		}

		private void ReleaseFillForms(CaptureInfo captureInfo)
		{
			if (captureInfo.forms == null)
				return;
			captureInfo.forms.AppBeep -= FormsAppBeep;
			captureInfo.forms.DoGotoAction -= FormsDoGotoAction;
			captureInfo.forms.DoNamedAction -= FormsDoNamedAction;
			captureInfo.forms.GotoPage -= FormsGotoPage;
			captureInfo.forms.Invalidate -= FormsInvalidate;
			captureInfo.forms.OutputSelectedRect -= FormsOutputSelectedRect;
			captureInfo.forms.SetCursor -= FormsSetCursor;
			captureInfo.forms.SynchronizationContext = captureInfo.sync;
			captureInfo.forms.SetHighlightColorEx(FormFieldTypes.FPDF_FORMFIELD_UNKNOWN, captureInfo.color);
		}

		private void CalcAndSetCurrentPage()
		{
			if (Document != null)
			{
				int idx = CalcCurrentPage();
				if (idx >= 0)
				{
					SetCurrentPage(idx);
				}
				InvalidateVisual();
			}
		}

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
                case ViewModes.TilesLine:
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

		private bool GetRenderRectEx(ref Rect rrect, int processedPage)
		{
            if (_renderRects.Length < OptimizedLoadThreshold || processedPage == 0)
            {
                rrect = GetRenderRect(processedPage);
                return true;
            }
            else if (_renderRects[processedPage].IsChecked)
            {
                rrect = new Rect(_renderRects[processedPage].X, _renderRects[processedPage].Y, _renderRects[processedPage].Width, _renderRects[processedPage].Height);
                return true;
            }
            else
                return false;
		}

		private Rect GetRenderRect(int index)
		{
			Size size = GetRenderSize(index);
			Point location = GetRenderLocation(size);
			return Helpers.CreateRect(location, size);
		}

		private Point GetRenderLocation(Size size)
		{
			Size clientSize = ClientRect.Size;
			if(ScrollOwner!= null)
			{
				double cw = ScrollOwner.ActualWidth - System.Windows.Forms.SystemInformation.VerticalScrollBarWidth;
				double ch = ScrollOwner.ActualHeight - System.Windows.Forms.SystemInformation.HorizontalScrollBarHeight;
				clientSize = new Size(
					cw < 0.000001 ? 0.000001 : cw,
					ch < 0.000001 ? 0.000001 : ch
					);
			}

			double xleft = 0 + Padding.Left;
			double ytop = 0 + Padding.Top;
			double xcenter = (clientSize.Width - Helpers.ThicknessHorizontal(Padding) - size.Width) / 2 + Padding.Left;
			double ycenter = (clientSize.Height - Helpers.ThicknessVertical(Padding) - size.Height) / 2 + Padding.Top;
			double xright = clientSize.Width - Helpers.ThicknessHorizontal(Padding) - size.Width + Padding.Left;
			double ybottom = clientSize.Height - Helpers.ThicknessVertical(Padding) - size.Height + Padding.Top;

			if (xcenter < Padding.Left)
				xcenter = Padding.Left;
			if (ycenter < Padding.Top)
				ycenter = Padding.Top;

			if (xright < Padding.Left)
				xright = Padding.Left;
            if (ybottom < Padding.Top)
                ybottom = Padding.Top;

            double x = xcenter;
			double y = ycenter;

			switch (PageVAlign)
			{
				case VerticalAlignment.Bottom: y = ybottom; break;
				case VerticalAlignment.Top: y = ytop; break;
			}

			switch (PageHAlign)
			{
				case HorizontalAlignment.Left: x = xleft; break;
				case HorizontalAlignment.Right: x = xright; break;
			}
			return new Point(x, y);
		}

		private Size GetRenderSize(int index)
		{
			Size clientSize = ClientRect.Size;
			if (ScrollOwner != null)
			{
				double cw = ScrollOwner.ActualWidth - System.Windows.Forms.SystemInformation.VerticalScrollBarWidth;
				double ch = ScrollOwner.ActualHeight - System.Windows.Forms.SystemInformation.HorizontalScrollBarHeight;
				clientSize = new Size(
					cw < 0.000001 ? 0.000001 : cw,
					ch < 0.000001 ? 0.000001 : ch
					);
			}

			double w, h;
			Pdfium.FPDF_GetPageSizeByIndex(Document.Handle, index, out w, out h);

            //converts PDF points which is 1/72 inch to WPF DIPs (Device Independed Pixels) which is 96 units per inch.
            w = w / 72.0 * 96;
            h = h / 72.0 * 96;

            double nw = clientSize.Width;
			double nh = h * nw / w;

            Size ret;
            switch (ViewMode)
            {
                case ViewModes.TilesLine:
                case ViewModes.TilesVertical:
                    ret =  CalcAppropriateSize(w, h, clientSize.Width / TilesCount - Helpers.ThicknessHorizontal(Padding), clientSize.Height - Helpers.ThicknessVertical(Padding));
                    break;
                case ViewModes.TilesHorizontal:
                    ret= CalcAppropriateSize(w, h, clientSize.Width - Helpers.ThicknessHorizontal(Padding), clientSize.Height / TilesCount - Helpers.ThicknessVertical(Padding));
                    break;
                default:
                    ret = CalcAppropriateSize(w, h, clientSize.Width - Helpers.ThicknessHorizontal(Padding), clientSize.Height - Helpers.ThicknessVertical(Padding));
                    break;
            }

            if (SizeMode != SizeModes.Zoom)
            {
                try
                {
                    _preventStackOverflowBugWorkaround = true;
                    Zoom = (float)(w / ret.Width);
                }
                finally
                {
                    _preventStackOverflowBugWorkaround = false;
                }
            }
            return ret;
        }

		private Size CalcAppropriateSize(double w, double h, double fitWidth, double fitHeight)
		{
			if (fitWidth < 0)
				fitWidth = 0;
			if (fitHeight < 0)
				fitHeight = 0;

			double nw = fitWidth;
			double nh = h * nw / w;

			switch (SizeMode)
			{
				case SizeModes.FitToHeight:
					nh = fitHeight;
					nw = w * nh / h;
					break;
				case SizeModes.FitToSize:
					nh = fitHeight;
					nw = w * nh / h;
					if (nw > fitWidth)
					{
						nw = fitWidth;
						nh = h * nw / w;
					}
					break;
				case SizeModes.Zoom:
					nw = w * Zoom;
					nh = h * Zoom;
					break;
			}
			return new Size(nw, nh);
		}

        private void AlignVertical(int from = 0, int to = -1)
        {
            if (to == -1)
                to = _renderRects.Length;

            if (_renderRects[to - 1].Bottom + Padding.Bottom < ClientRect.Size.Height)
            {
                var loc = GetRenderLocation(new Size(0, _renderRects[to - 1].Bottom - Padding.Bottom));
                var offset = loc.Y - _renderRects[from].Y;
                for (int i = from; i < to; i++)
                    _renderRects[i].Y += offset;
            }
        }

        private void AlignHorizontal(int from = 0, int to = -1)
        {
            if (to == -1)
                to = _renderRects.Length;

            if (_renderRects[to - 1].Right + Padding.Right < ClientRect.Size.Width)
            {
                var loc = GetRenderLocation(new Size(_renderRects[to - 1].Right - Padding.Right, 0));
                var offset = loc.X - _renderRects[from].X;
                for (int i = from; i < to; i++)
                    _renderRects[i].X += offset;
            }
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
				Document.Pages[i].DeviceToPage(
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

		private Helpers.Int32Point PageToDevice(double x, double y, int pageIndex)
		{
			var rect = renderRects(pageIndex);
			rect.X += _autoScrollPosition.X;
			rect.Y += _autoScrollPosition.Y;

			int dx, dy;
			Document.Pages[pageIndex].PageToDevice(
					(int)rect.X, (int)rect.Y,
					(int)rect.Width, (int)rect.Height,
					PageRotation(Document.Pages[pageIndex]),
					(float)x, (float)y,
					out dx, out dy);
			return new Helpers.Int32Point(dx, dy);
		}

        private Int32Rect PageToDeviceRect(FS_RECTF rc, int pageIndex)
        {
            var pt1 = PageToDevice(rc.left, rc.top, pageIndex);
            var pt2 = PageToDevice(rc.right, rc.bottom, pageIndex);
            int x = Helpers.UnitsToPixels(pt1.X < pt2.X ? pt1.X : pt2.X);
            int y = Helpers.UnitsToPixels(pt1.Y < pt2.Y ? pt1.Y : pt2.Y);
            int w = Helpers.UnitsToPixels(pt1.X > pt2.X ? pt1.X - pt2.X : pt2.X - pt1.X);
            int h = Helpers.UnitsToPixels(pt1.Y > pt2.Y ? pt1.Y - pt2.Y : pt2.Y - pt1.Y);
            return new Int32Rect(x, y, w, h);
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

        private List<Int32Rect> NormalizeRects(IEnumerable<FS_RECTF> rects, int pageIndex, IEnumerable<FS_RECTF> rectsBefore, IEnumerable<FS_RECTF> rectsAfter)
        {
            return NormalizeRects(rects, pageIndex, rectsBefore, rectsAfter, new FS_RECTF());
        }

        private List<Int32Rect> NormalizeRects(IEnumerable<FS_RECTF> rects, int pageIndex, IEnumerable<FS_RECTF> rectsBefore, IEnumerable<FS_RECTF> rectsAfter, FS_RECTF inflate)
        {
            List<Int32Rect> rows = new List<Int32Rect>();

            if (_smoothSelection == SmoothSelection.None)
            {
                foreach (var rc in rects)
                    rows.Add(PageToDeviceRect(rc, pageIndex));
                return rows;
            }
            
            int left, right, top, bottom;
            left = top = int.MaxValue;
            right = bottom = int.MinValue;
            float lowestBottom = float.NaN;
            float highestTop = float.NaN;

            foreach (var rc in rects)
            {
                rc.Inflate(inflate);

                float h = (highestTop - lowestBottom);
                //check if new row is required
                if (float.IsNaN(lowestBottom))
                {
                    lowestBottom = rc.bottom;
                    highestTop = rc.top;
                }
                else if (rc.top < lowestBottom + h / 2 || rc.bottom > highestTop - h / 2)
                {
                    rows.Add(new Int32Rect(left, top, right - left, bottom - top));
                    lowestBottom = rc.bottom;
                    highestTop = rc.top;
                    left = top = int.MaxValue;
                    right = bottom = int.MinValue;
                }
                else
                {
                    if (lowestBottom > rc.bottom)
                        lowestBottom = rc.bottom;
                    if (highestTop < rc.top)
                        highestTop = rc.top;
                }

                //calc client coordinates
                Int32Rect deviceRect = PageToDeviceRect(rc, pageIndex);

                //concatenate previous and current rectangle
                if (left > deviceRect.X)
                    left = deviceRect.X;
                if (right < deviceRect.X + deviceRect.Width)
                    right = deviceRect.X + deviceRect.Width;
                if (top > deviceRect.Y)
                    top = deviceRect.Y;
                if (bottom < deviceRect.Y + deviceRect.Height)
                    bottom = deviceRect.Y + deviceRect.Height;
            }
            rows.Add(new Int32Rect(left, top, right - left, bottom - top));

            if (_smoothSelection == SmoothSelection.ByLine && rectsBefore != null)
                PadRectagles(pageIndex, rectsBefore, rows, true);

            if (_smoothSelection == SmoothSelection.ByLine && rectsAfter != null)
                PadRectagles(pageIndex, rectsAfter, rows, false);

            return rows;
        }

        private void PadRectagles(int pageIndex, IEnumerable<FS_RECTF> padRects, List<Int32Rect> rows, bool isLeft)
        {
            var rTmp = NormalizeRects(padRects, pageIndex, null, null);
            if (rTmp.Count > 0)
            {
                var rc = rTmp[isLeft ? rTmp.Count - 1 : 0];
                if (!(rc.Y + rc.Height < rows[isLeft ? 0 : rows.Count - 1].Y || rc.Y > rows[isLeft ? 0 : rows.Count - 1].Y + rows[isLeft ? 0 : rows.Count - 1].Height))
                {
                    var l = rows[isLeft ? 0 : rows.Count - 1].X;
                    var r = rows[isLeft ? 0 : rows.Count - 1].X + rows[isLeft ? 0 : rows.Count - 1].Width;
                    var t = Math.Min(rc.Y, rows[isLeft ? 0 : rows.Count - 1].Y);
                    var b = Math.Max(rc.Y + rc.Height, rows[isLeft ? 0 : rows.Count - 1].Y + rows[isLeft ? 0 : rows.Count - 1].Height);
                    rows[isLeft ? 0 : rows.Count - 1] = new Int32Rect(l, t, r - l, b - t);
                }
            }
        }

        private Size CalcVertical()
		{
			double width = 0;
			Rect rrect = Rect.Empty;
			for (int i = 0; i < _renderRects.Length; i++)
			{
				bool isChecked = GetRenderRectEx(ref rrect, i);
				again: _renderRects[i] = Helpers.CreateRenderRect(
					rrect.X,
                    i > 0 ? _renderRects[i - 1].Bottom + Helpers.ThicknessVertical(PageMargin) : Padding.Top,
                    rrect.Width,
					rrect.Height,
					isChecked);
                if (!isChecked && CalcActualRect(i).IntersectsWith(ClientRect))
                {
                    isChecked = true;
                    rrect = GetRenderRect(i);
                    goto again;
                }
                if (width < _renderRects[i].Right)
                    width = _renderRects[i].Right;
            }
            AlignVertical();
			return Helpers.CreateSize(width + Padding.Right, _renderRects[_renderRects.Length - 1].Bottom + Padding.Bottom);
		}

        private Size CalcTilesVertical()
        {
            Rect rrect = Rect.Empty;
            double height = 0;
            double width = 0;
            for (int i = 0; i < _renderRects.Length; i += TilesCount)
            {
                int j;
                int lowestPage = i; //Index of the page with lowest bottom edge in the row
                for (j = i; j < i + TilesCount && j < _renderRects.Length; j++)
                {
                    bool isChecked = GetRenderRectEx(ref rrect, j);
                    again: _renderRects[j] = new RenderRect(
                        (j - i) % TilesCount != 0 ? _renderRects[j - 1].Right + Helpers.ThicknessHorizontal(PageMargin) : Padding.Left,
                        i > 0 ? height + Helpers.ThicknessVertical(PageMargin) : Padding.Top,
                        rrect.Width,
                        rrect.Height,
                        isChecked);
                    if (!isChecked && CalcActualRect(j).IntersectsWith(ClientRect))
                    {
                        isChecked = true;
                        rrect = GetRenderRect(j);
                        goto again;
                    }
                    if (_renderRects[lowestPage].Bottom < _renderRects[j].Bottom)
                        lowestPage = j;
                }
                AlignHorizontal(i, j);

                height = _renderRects[lowestPage].Bottom;
                if (width < _renderRects[j - 1].Right)
                    width = _renderRects[j - 1].Right;
            }
            AlignVertical();
            return new Size(width + Padding.Right, height + Padding.Bottom);
        }

        private Size CalcTilesHorizontal()
        {
            Rect rrect = Rect.Empty;
            double height = 0;
            double width = 0;
            for (int i = 0; i < _renderRects.Length; i += TilesCount)
            {
                int j;
                int rightestPage = i; //Index of the page with maximum right edge in the column
                for (j = i; j < i + TilesCount && j < _renderRects.Length; j++)
                {
                    bool isChecked = GetRenderRectEx(ref rrect, j);
                    again: _renderRects[j] = new RenderRect(
                        i > 0 ? width + Helpers.ThicknessHorizontal(PageMargin) : Padding.Left,
                        (j - i) % TilesCount != 0 ? _renderRects[j - 1].Bottom + Helpers.ThicknessVertical(PageMargin) : Padding.Top,
                        rrect.Width,
                        rrect.Height,
                        isChecked);
                    if (!isChecked && CalcActualRect(j).IntersectsWith(ClientRect))
                    {
                        isChecked = true;
                        rrect = GetRenderRect(j);
                        goto again;
                    }
                    if (_renderRects[rightestPage].Right < _renderRects[j].Right)
                        rightestPage = j;
                }
                AlignVertical(i, j);

                width = _renderRects[rightestPage].Right;
                if (height < _renderRects[j - 1].Bottom)
                    height = _renderRects[j - 1].Bottom;
            }
            AlignHorizontal();
            return new Size(width + Padding.Right, height + Padding.Bottom);
        }

        private Size CalcHorizontal()
		{
			double height = 0;
			Rect rrect = Rect.Empty;
			for (int i = 0; i < _renderRects.Length; i++)
			{
				bool isChecked = GetRenderRectEx(ref rrect, i);
				again: _renderRects[i] = Helpers.CreateRenderRect(
                    i > 0 ? _renderRects[i - 1].Right + Helpers.ThicknessHorizontal(PageMargin) : Padding.Left,
                    rrect.Y,
					rrect.Width,
					rrect.Height,
					isChecked);
                if (!isChecked && CalcActualRect(i).IntersectsWith(ClientRect))
                {
                    isChecked = true;
                    rrect = GetRenderRect(i);
                    goto again;
                }
                if (height < _renderRects[i].Bottom)
                    height = _renderRects[i].Bottom;
            }
            AlignHorizontal();
			return Helpers.CreateSize(_renderRects[_renderRects.Length - 1].Right + Padding.Right, height + Padding.Bottom);
		}

		private Size CalcSingle()
		{
			Size ret = Helpers.CreateSize(0, 0);
			Rect rrect = Rect.Empty;
			for (int i = 0; i < _renderRects.Length; i++)
			{
                bool isChecked = true;
                if (i == CurrentIndex)
                    rrect = GetRenderRect(i);
                else
                    isChecked = GetRenderRectEx(ref rrect, i);
				_renderRects[i] = Helpers.CreateRenderRect(
					rrect.X,
					rrect.Y,
					rrect.Width,
					rrect.Height,
					isChecked);
				if (i == Document.Pages.CurrentIndex)
					ret = Helpers.CreateSize(rrect.Width + Helpers.ThicknessHorizontal(Padding), rrect.Height + Helpers.ThicknessVertical(Padding));
			}
			return ret;
		}

        private Size CalcTilesLine()
        {
            Size ret = new Size(0, 0);
            Rect rrect = Rect.Empty;
            for (int i = 0; i < _renderRects.Length; i++)
            {
                bool isChecked = true;
                if (i >= _startPage && i <= _endPage)
                    rrect = GetRenderRect(i);
                else
                    isChecked = GetRenderRectEx(ref rrect, i);
                _renderRects[i] = new RenderRect(
                    i % TilesCount == 0 ? Padding.Left : _renderRects[i - 1].Right + Helpers.ThicknessHorizontal(PageMargin),
                    rrect.Y,
                    rrect.Width,
                    rrect.Height,
                    isChecked);

                if (i % TilesCount == TilesCount - 1 || i == _renderRects.Length - 1)
                {
                    AlignHorizontal(i - i % TilesCount, i + 1);
                    ret = new Size(_renderRects[i].Right + Padding.Right, _renderRects[IdxWithLowestBottom(i - i % TilesCount, i)].Bottom + Padding.Bottom);
                }
            }
            return ret;
        }

        private Rect renderRects(int index)
		{
			if (_renderRects != null)
				return _renderRects[index].Rect();
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
                    int prevStart = _startPage;
                    int prevEnd = _endPage;
                    Document.Pages.CurrentIndex = index;
					OnCurrentPageChanged(EventArgs.Empty);

                    if (ViewMode == ViewModes.SinglePage || ViewMode == ViewModes.TilesLine)
                        UpdateScrollBars(new Size(_renderRects[_endPage].Right + Padding.Right, _renderRects[IdxWithLowestBottom(_startPage, _endPage)].Bottom + Padding.Bottom));
                    if ((ViewMode == ViewModes.SinglePage || ViewMode == ViewModes.TilesLine) && _startPage != prevStart)
                        for (int i = prevStart; i <= prevEnd; i++)
                            if (PageAutoDispose && CanDisposePage(i))
                                Document.Pages[i].Dispose();
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

		private void CalcPages()
		{
			Size size;

			switch (ViewMode)
			{
				case ViewModes.Vertical:
					size = CalcVertical();
					break;
				case ViewModes.Horizontal:
					size = CalcHorizontal();
					break;
				case ViewModes.TilesVertical:
					size = CalcTilesVertical();
					break;
                case ViewModes.TilesHorizontal:
                    size = CalcTilesHorizontal();
                    break;
                case ViewModes.TilesLine:
                    size = CalcTilesLine();
                    break;
                default:
					size = CalcSingle();
					break;
			}

            UpdateScrollBars(size);

            //We should do this because we recalculate ZOOM in GetRenderRect which is called fromm Calc* methods
            if (SizeMode != SizeModes.Zoom)
                OnZoomChanged(EventArgs.Empty);
        }

        private void UpdateScrollBars(Size size)
        {
            if (size.Width != 0 && size.Height != 0)
            {
                _extent = size;
                _viewport = new Size(ActualWidth, ActualHeight);
                if (ScrollOwner != null)
                    ScrollOwner.InvalidateScrollInfo();
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

		private void StartInvalidateTimer()
		{
			if (_invalidateTimer != null)
				return;

			_invalidateTimer = new System.Windows.Threading.DispatcherTimer();
			_invalidateTimer.Interval = TimeSpan.FromMilliseconds(10);
			_invalidateTimer.Tick += (s, a) =>
			{
				if (!_prPages.IsNeedContinuePaint)
				{
					_invalidateTimer.Stop();
					_invalidateTimer = null;
				}
				InvalidateVisual();
			};
			_invalidateTimer.Start();
		}

        private int IdxWithLowestBottom(int from, int to)
        {
            int idx = from;
            for (int i = from + 1; i <= to; i++)
            {
                if (_renderRects[idx].Bottom < _renderRects[i].Bottom)
                    idx = i;
            }
            return idx;
        }

        private IEnumerable<FS_RECTF> GetRectsFromTextInfoWithoutSpaceCharacter(int pageIndex, int s, int len)
        {
            var tmpRet = new List<IEnumerable<FS_RECTF>>();
            int curStart = -1;
            int curLen = 0;
            for (int i = s; i < s + len; i++)
            {
                if (Document.Pages[pageIndex].Text.GetCharacter(i) == ' ')
                {
                    if (curStart >= 0)
                    {
                        tmpRet.Add(Document.Pages[pageIndex].Text.GetTextInfo(curStart, curLen).Rects);
                        curStart = -1;
                        curLen = 0;
                    }
                    continue;
                }
                if (curStart == -1)
                {
                    curStart = i;
                    curLen = 1;
                }
                else
                    curLen++;
            }
            if (curStart >= 0)
                tmpRet.Add(Document.Pages[pageIndex].Text.GetTextInfo(curStart, curLen).Rects);

            var ret = new List<FS_RECTF>();
            foreach (var t in tmpRet)
                ret.AddRange(t);

            return ret;
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
            InternalSetCursor(e.Cursor);
        }
#endregion

#region FillForms event handlers
        private void FormsInvalidate(object sender, InvalidatePageEventArgs e)
		{
			OnFormsInvalidate(e);
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
		private void Pages_ProgressiveRender(object sender, ProgressiveRenderEventArgs e)
		{
			e.NeedPause = _prPages.IsNeedPause(sender as PdfPage);
		}

		void Pages_CurrentPageChanged(object sender, EventArgs e)
		{
            if (ViewMode == ViewModes.SinglePage || ViewMode == ViewModes.TilesLine)
            {
                _prPages.ReleaseCanvas();
                UpdateScrollBars(new Size(_renderRects[_endPage].Right + Padding.Right, _renderRects[IdxWithLowestBottom(_startPage, _endPage)].Bottom + Padding.Bottom));
            }
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

			var prev = _autoScrollPosition.Y;
			_autoScrollPosition.Y = -offset;
			CalcAndSetCurrentPage();
			if (prev != _autoScrollPosition.Y)
				_prPages.ReleaseCanvas();

			if (ScrollOwner != null)
				ScrollOwner.InvalidateScrollInfo();
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

			var prev = _autoScrollPosition.X;
			_autoScrollPosition.X = -offset;
			CalcAndSetCurrentPage();
			if (prev != _autoScrollPosition.X)
				_prPages.ReleaseCanvas();

			if (ScrollOwner != null)
				ScrollOwner.InvalidateScrollInfo();
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
			if (ScrollOwner.ComputedVerticalScrollBarVisibility == Visibility.Visible)
				SetVerticalOffset(this.VerticalOffset - _viewport.Height / 10 * 1);
			else if (ScrollOwner.ComputedHorizontalScrollBarVisibility == Visibility.Visible)
				SetHorizontalOffset(this.HorizontalOffset - _viewport.Width / 10 * 1);
			else
				SetVerticalOffset(this.VerticalOffset - _viewport.Height / 10 * 1);
		}

		/// <summary>
		/// Scrolls down within content after a user clicks the wheel button on a mouse.
		/// </summary>
		public void MouseWheelDown()
		{
			if (ScrollOwner.ComputedVerticalScrollBarVisibility== Visibility.Visible)
				SetVerticalOffset(this.VerticalOffset + _viewport.Height / 10 * 1);
			else if (ScrollOwner.ComputedHorizontalScrollBarVisibility == Visibility.Visible)
				SetHorizontalOffset(this.HorizontalOffset + _viewport.Width / 10 * 1);
			else
				SetVerticalOffset(this.VerticalOffset + _viewport.Height / 10 * 1);
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
				_isShowSelection = true;
				if (_selectInfo.StartPage >= 0)
                    GenerateSelectedTextProperty();
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
				EndIndex = -1// Document.Pages[page_index].Text.GetCharIndexAtPos((float)page_point.X, (float)page_point.Y, 10.0f, 10.0f)
			};
			_isShowSelection = false;
			if (_selectInfo.StartPage >= 0)
                GenerateSelectedTextProperty();
        }

		private CursorTypes ProcessMouseMoveForSelectTextTool(Point page_point, int page_index)
		{
            int character_index = Document.Pages[page_index].Text.GetCharIndexAtPos((float)page_point.X, (float)page_point.Y, 10.0f, 10.0f);
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
                    _mousePressedInLink = false;
                    _isShowSelection = true;
				}
				InvalidateVisual();
			}

            if (!Document.Pages[page_index].OnMouseMove(0, (float)page_point.X, (float)page_point.Y))
                if (character_index >= 0)
                    return CursorTypes.VBeam;
            var formFieldType = Document.FormFill != null ? Document.Pages[page_index].GetFormFieldAtPoint((float)page_point.X, (float)page_point.Y) : FormFieldTypes.FPDF_FORMFIELD_NOFIELDS;
            switch (formFieldType)
            {
                case FormFieldTypes.FPDF_FORMFIELD_CHECKBOX:
                case FormFieldTypes.FPDF_FORMFIELD_COMBOBOX:
                case FormFieldTypes.FPDF_FORMFIELD_PUSHBUTTON:
                case FormFieldTypes.FPDF_FORMFIELD_RADIOBUTTON:
                case FormFieldTypes.FPDF_FORMFIELD_LISTBOX: return CursorTypes.Hand;
                case FormFieldTypes.FPDF_FORMFIELD_TEXTFIELD: return CursorTypes.VBeam;
            }
            return CursorTypes.Arrow;
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

		private CursorTypes ProcessMouseMoveForDefaultTool(Point page_point, int page_index)
		{
			var pdfLink = Document.Pages[page_index].Links.GetLinkAtPoint((float)page_point.X, (float)page_point.Y);
			var webLink = Document.Pages[page_index].Text.WebLinks.GetWebLinkAtPoint((float)page_point.X, (float)page_point.Y);
            if (webLink != null || pdfLink != null)
                return CursorTypes.Hand;
            return CursorTypes.Arrow;
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

		private CursorTypes ProcessMouseMoveForPanTool(Point mouse_point)
		{
			if (!_mousePressed)
				return CursorTypes.Arrow;
			var yOffs = mouse_point.Y - _panToolInitialMousePosition.Y;
			var xOffs = mouse_point.X - _panToolInitialMousePosition.X;
			SetVerticalOffset(-_panToolInitialScrollPosition.Y - yOffs);
			SetHorizontalOffset(-_panToolInitialScrollPosition.X - xOffs);
            return CursorTypes.Arrow;
		}

		private void ProcessMouseUpPanTool(Point mouse_point)
		{
			ReleaseMouseCapture();
		}
#endregion
	}
}
