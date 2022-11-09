using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;

namespace Patagames.Pdf.Net.Controls.Wpf
{
	/// <summary>
	/// Represents the BookmarksViewer control for displaying bookmarks contained in PDF document.
	/// </summary>
	public class BookmarksViewer : TreeView
	{
		#region Dependency properties
		/// <summary>
		/// DependencyProperty as the backing store for <see cref="PdfViewer"/>
		/// </summary>
		public static readonly DependencyProperty PdfViewerProperty =
			DependencyProperty.Register("PdfViewer", typeof(PdfViewer), typeof(BookmarksViewer),
				new PropertyMetadata(null,
					(o, e) =>
					{
						var bookmarksViewer = o as BookmarksViewer;
						var oldValue = e.OldValue as PdfViewer;
						var newValue = e.NewValue as PdfViewer;

						if (oldValue != newValue)
							bookmarksViewer.OnPdfViewerChanging(oldValue, newValue);
					}));
		#endregion

		#region Public Properties
		/// <summary>
		/// Gets or sets PdfViewer control associated with this BookmarksViewer control
		/// </summary>
		/// <remarks>It's a dependency property. Please find more details here: <see cref="BookmarksViewer.PdfViewerProperty"/></remarks>
		public PdfViewer PdfViewer
		{
			get { return (PdfViewer)GetValue(PdfViewerProperty); }
			set { SetValue(PdfViewerProperty, value); }
		}
		#endregion

		#region Constructors and initialization
		/// <summary>
		/// Initializes a new instance of the <see cref="BookmarksViewer"/> class. 
		/// </summary>
		public BookmarksViewer()
		{
			string dataTemplateString =
					@"<HierarchicalDataTemplate ItemsSource=""{Binding Path=Childs}"">" +
						@"<TextBlock Text=""{Binding Title}"" />" +
					@"</HierarchicalDataTemplate>";
			ParserContext parserContext = new ParserContext();
			parserContext.XmlnsDictionary.Add("", "http://schemas.microsoft.com/winfx/2006/xaml/presentation");
			DataTemplate template = (DataTemplate)XamlReader.Parse(dataTemplateString, parserContext);
			ItemTemplate = template;
		}
		#endregion

		#region Overrides
		/// <summary>
		/// Raises the System.Windows.Controls.TreeView.SelectedItemChanged event when the 
		/// System.Windows.Controls.TreeView.SelectedItem property value changes.
		/// </summary>
		/// <param name="e">Provides the item that was previously selected and the item that is currently 
		/// selected for the System.Windows.Controls.TreeView.SelectedItemChanged event.</param>
		protected override void OnSelectedItemChanged(RoutedPropertyChangedEventArgs<object> e)
		{
			var bookmark = e.NewValue as PdfBookmark;
			if (bookmark == null)
				return;

			if (bookmark.Action != null)
				ProcessAction(bookmark.Action);
			else if (bookmark.Destination != null)
				ProcessDestination(bookmark.Destination);

			base.OnSelectedItemChanged(e);
		}
		#endregion

		#region Protected methods
		/// <summary>
		/// Called when the current PdfViewer control associated with the ToolStrip is changing.
		/// </summary>
		/// <param name="oldValue">PdfViewer control of which was associated with the ToolStrip.</param>
		/// <param name="newValue">PdfViewer control of which will be associated with the ToolStrip.</param>
		protected virtual void OnPdfViewerChanging(PdfViewer oldValue, PdfViewer newValue)
		{
			if (oldValue != null)
			{
				oldValue.AfterDocumentChanged -= pdfViewer_DocumentChanged;
				oldValue.DocumentClosed -= pdfViewer_DocumentClosed;
				oldValue.DocumentLoaded -= pdfViewer_DocumentLoaded;
			}
			if (newValue != null)
			{
				newValue.AfterDocumentChanged += pdfViewer_DocumentChanged;
				newValue.DocumentClosed += pdfViewer_DocumentClosed;
				newValue.DocumentLoaded += pdfViewer_DocumentLoaded;
			}

			RebuildTree();
		}

		/// <summary>
		/// Process the <see cref="PdfAction"/>.
		/// </summary>
		/// <param name="pdfAction">PdfAction to be performed.</param>
		protected virtual void ProcessAction(PdfAction pdfAction)
		{
			if (PdfViewer == null)
				return;
			PdfViewer.ProcessAction(pdfAction);
		}

		/// <summary>
		/// Process the <see cref="PdfDestination"/>.
		/// </summary>
		/// <param name="pdfDestination">PdfDestination to be performed.</param>
		protected virtual void ProcessDestination(PdfDestination pdfDestination)
		{
			if (PdfViewer == null)
				return;
			PdfViewer.ProcessDestination(pdfDestination); ;
		}
		#endregion

		#region Private event handlers
		private void pdfViewer_DocumentChanged(object sender, EventArgs e)
		{
			RebuildTree();
		}

		private void pdfViewer_DocumentLoaded(object sender, EventArgs e)
		{
			RebuildTree();
		}

		private void pdfViewer_DocumentClosed(object sender, EventArgs e)
		{
			RebuildTree();
		}
		#endregion

		#region Public methods
		/// <summary>
		/// Constructs the tree of bookmarks
		/// </summary>
		public void RebuildTree()
		{
			if (PdfViewer == null || PdfViewer.Document == null || PdfViewer.Document.Bookmarks == null)
				this.ItemsSource = null;
			else
			{
				this.ItemsSource = PdfViewer.Document.Bookmarks;
			}
		}
		#endregion
	}
}
