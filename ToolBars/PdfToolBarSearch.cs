using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace Patagames.Pdf.Net.Controls.Wpf.ToolBars
{
	/// <summary>
	/// Provides a container for Windows toolbar objects with predefined functionality for searching
	/// </summary>
	public class PdfToolBarSearch : PdfToolBar
	{
		#region Private fields
		PdfSearch search = null;
		List<PdfSearch.FoundText> _foundText = new List<PdfSearch.FoundText>();
		List<PdfSearch.FoundText> _forHighlight = new List<PdfSearch.FoundText>();
		object _syncFoundText = new object();
		DispatcherTimer _foundTextTimer;
		delegate void DelegateType1();
		#endregion

		#region Public Properties
		/// <summary>
		/// Gets or sets the color of the found text.
		/// </summary>
		public Color HighlightColor { get; set; }

		/// <summary>
		/// Gets or sets search text
		/// </summary>
		public string SearchText
		{
			get
			{
				return (this.Items[0] as SearchBar).SearchText;
			}
			set
			{
				(this.Items[0] as SearchBar).SearchText = value;
			}
		}
		#endregion

		#region Constructors
		/// <summary>
		/// Initialize the new instance of PdfToolBarSearch class
		/// </summary>
		public PdfToolBarSearch()
		{
			_foundTextTimer = new DispatcherTimer();
			_foundTextTimer.Interval = TimeSpan.FromMilliseconds(50);
			_foundTextTimer.Tick += _foundTextTimer_Tick;
			HighlightColor = Color.FromArgb(90, 255, 255, 0);
		}
		#endregion

		#region Overriding
		/// <summary>
		/// Create all buttons and add its into toolbar. Override this method to create custom buttons
		/// </summary>
		protected override void InitializeButtons()
		{
			var btn = new SearchBar();
			btn.Name = "btnSearchBar";
			btn.CurrentRecordChanged += SearchBar_CurrentRecordChanged;
			btn.NeedSearch += SearchBar_NeedSearch;
			this.Items.Add(btn);
		}

		/// <summary>
		/// Called when the ToolBar's items need to change its states
		/// </summary>
		protected override void UpdateButtons()
		{
			var tsi = this.Items[0] as SearchBar;
			if (tsi != null)
			{
				tsi.IsEnabled = (PdfViewer != null) && (PdfViewer.Document != null);
				tsi.TotalRecords = 0;
				tsi.SearchText = "";
			}

			if (PdfViewer == null || PdfViewer.Document == null)
				return;
		}

		/// <summary>
		/// Called when the current PdfViewer control associated with the ToolBar is changing.
		/// </summary>
		/// <param name="oldValue">PdfViewer control of which was associated with the ToolBar.</param>
		/// <param name="newValue">PdfViewer control of which will be associated with the ToolBar.</param>
		protected override void OnPdfViewerChanging(PdfViewer oldValue, PdfViewer newValue)
		{
			base.OnPdfViewerChanging(oldValue, newValue);
			if (oldValue != null)
				UnsubscribePdfViewEvents(oldValue);
			if (newValue != null)
				SubscribePdfViewEvents(newValue);

			if (oldValue != null && oldValue.Document != null && search == null)
				PdfViewer_DocumentClosed(this, EventArgs.Empty);
			if (newValue != null && newValue.Document != null && search == null)
				PdfViewer_DocumentLoaded(this, EventArgs.Empty);


		}

		#endregion

		#region Event handlers for PdfViewer
		private void PdfViewer_DocumentClosing(object sender, EventArguments.DocumentClosingEventArgs e)
		{
			e.Cancel = false;
			StopSearch();
		}

		private void PdfViewer_DocumentClosed(object sender, EventArgs e)
		{
			UpdateButtons();
			search.FoundTextAdded -= Search_FoundTextAdded;
			search.SearchCompleted -= Search_SearchCompleted;
			StopSearch();
		}

		private void PdfViewer_DocumentLoaded(object sender, EventArgs e)
		{
			UpdateButtons();
			search = new PdfSearch(PdfViewer.Document);
			search.FoundTextAdded += Search_FoundTextAdded;
			search.SearchCompleted += Search_SearchCompleted;
		}

		private void Search_SearchCompleted(object sender, System.ComponentModel.RunWorkerCompletedEventArgs e)
		{
			_foundTextTimer.Stop();
			_foundTextTimer_Tick(_foundTextTimer, EventArgs.Empty);
		}

		int _sleepCnt = 0;
		private void Search_FoundTextAdded(object sender, EventArguments.FoundTextAddedEventArgs e)
		{
			lock (_syncFoundText)
			{
				_foundText.Add(e.FoundText);
				_forHighlight.Add(e.FoundText);
			}
			//Give a chance to GUI thread to process the found records.
			_sleepCnt++;
			if (_sleepCnt >= 100)
			{
				_sleepCnt = 0;
				System.Threading.Thread.Sleep(10);
			}
		}

		#endregion

		#region Event handlers for buttons
		private void SearchBar_NeedSearch(object sender, EventArgs e)
		{
			var item = this.Items[0] as SearchBar;
			OnNeedSearch(item, item.SearchText);
		}

		private void SearchBar_CurrentRecordChanged(object sender, EventArgs e)
		{
			var item = this.Items[0] as SearchBar;
			OnCurrentRecordChanged(item, item.CurrentRecord);
		}

		#endregion

		#region Protected methods
		private void OnCurrentRecordChanged(SearchBar item, int currentRecord)
		{
			ScrollToRecord(currentRecord);
		}

		private void OnNeedSearch(SearchBar item, string searchText)
		{
			var tssb = item as SearchBar;
			if (tssb == null)
				return;
			StartSearch(tssb, searchText);
		}

		#endregion

		#region Private methods
		private void UnsubscribePdfViewEvents(PdfViewer oldValue)
		{
			oldValue.DocumentLoaded -= PdfViewer_DocumentLoaded;
			oldValue.DocumentClosing -= PdfViewer_DocumentClosing;
			oldValue.DocumentClosed -= PdfViewer_DocumentClosed;
		}

		private void SubscribePdfViewEvents(PdfViewer newValue)
		{
			newValue.DocumentLoaded += PdfViewer_DocumentLoaded;
			newValue.DocumentClosing += PdfViewer_DocumentClosing;
			newValue.DocumentClosed += PdfViewer_DocumentClosed;
		}

		private void StartSearch(SearchBar sb, string searchText)
		{
			StopSearch();
			if (searchText == "")
				return;
			search.Start(searchText, sb.FindFlags);
			_foundTextTimer.Start();
		}

		private void StopSearch()
		{
			_foundTextTimer.Stop();
			search.End();
			while (search.IsBusy)
				DoEvents();
			_foundText.Clear();
			_forHighlight.Clear();
			PdfViewer.RemoveHighlightFromText();
		}

		private void ScrollToRecord(int currentRecord)
		{
			PdfSearch.FoundText ft;
			lock (_syncFoundText)
			{
				if (currentRecord < 1 || currentRecord > _foundText.Count)
					return;
				ft = _foundText[currentRecord - 1];
			}

			PdfViewer.CurrentIndex = ft.PageIndex;
			PdfViewer.ScrollToPage(ft.PageIndex);
			PdfViewer.ScrollToChar(ft.CharIndex);
		}

		private void _foundTextTimer_Tick(object sender, EventArgs e)
		{
			var tssb = this.Items[0] as SearchBar;
			if (tssb == null)
				return;
			lock (_syncFoundText)
			{
				tssb.TotalRecords = _foundText.Count;
				foreach (var ft in _forHighlight)
					PdfViewer.HighlightText(ft.PageIndex, ft.CharIndex, ft.CharsCount, HighlightColor);
				_forHighlight.Clear();
			}
		}

		private static void DoEvents()
		{
			DelegateType1 method = DoEventsInternal;
			Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, method);
			//Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(delegate { }));
		}

		private static void DoEventsInternal()
		{

		}
		#endregion
	}
}
