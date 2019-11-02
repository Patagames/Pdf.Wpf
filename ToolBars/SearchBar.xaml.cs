using Patagames.Pdf.Enums;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace Patagames.Pdf.Net.Controls.Wpf.ToolBars
{
	internal partial class SearchBar : UserControl
	{
		#region Private fields
		private int _totalRecords = 0;
		private int _currentRecord = 0;
		private DispatcherTimer _onsearchTimer;
		private Color _borderColor;
		#endregion

		#region Public events and properties
		public event EventHandler CurrentRecordChanged = null;
		public event EventHandler NeedSearch = null;

		public string MenuItemMathCaseText { get { return Properties.Resources.menuItemMatchCase; } }
		public string MenuItemMatchWholeWordText { get { return Properties.Resources.menuItemMatchWholeWord; } }
		

		public Color BorderColor
		{
			get
			{
				return _borderColor;
			}
			set
			{
				_borderColor = value;
				pnlBorder.BorderBrush = new SolidColorBrush(value);
			}
		}
		public FindFlags FindFlags { get; set; }

		public int TotalRecords
		{
			get
			{
				return _totalRecords;
			}
			set
			{
				_totalRecords = value;
				if (_totalRecords < 0)
					_totalRecords = 0;
				if (_currentRecord > _totalRecords)
					_currentRecord = _totalRecords;
				if (_totalRecords == 0)
					lblInfo.Background = new SolidColorBrush(Colors.PaleVioletRed);
				else
					lblInfo.Background = new SolidColorBrush(Colors.Transparent);
				SetInfoText();
				EnableButton(picUp, _totalRecords > 0);
				EnableButton(picDown, _totalRecords > 0);

				if (_totalRecords > 0 && _currentRecord == 0)
					CurrentRecord = 1;
			}
		}
		public int CurrentRecord
		{
			get
			{
				return _currentRecord;
			}
			set
			{
				if (_currentRecord != value)
				{
					_currentRecord = value;
					SetInfoText();
					if (CurrentRecordChanged != null)
						CurrentRecordChanged(this, EventArgs.Empty);
				}
			}
		}

		private void SetInfoText()
		{
			lblInfo.Text = string.Format(Properties.Resources.searchLblnfo, CurrentRecord, TotalRecords);
		}

		public string SearchText
		{
			get
			{
				return tbSearch.Text;
			}
			set
			{
				if (SearchText != value)
					tbSearch.Text = value;
			}
		}

		public bool IsCheckedMatchCase { get {return (FindFlags & FindFlags.MatchCase) == FindFlags.MatchCase; } }
		public bool IsCheckedMatchWholeWord { get { return (FindFlags & FindFlags.MatchWholeWord) == FindFlags.MatchWholeWord; } }

		#endregion

		#region Constructors
		public SearchBar()
		{
			InitializeComponent();
			this.DataContext = this;
			pnlBorder.Background = new SolidColorBrush(BorderColor);
			EnableButton(picUp, false);
			EnableButton(picDown, false);
			lblInfo.Visibility = Visibility.Hidden;

			_onsearchTimer = new DispatcherTimer();
			_onsearchTimer.Interval = TimeSpan.FromMilliseconds(50);
			_onsearchTimer.Tick += _onsearchTimer_Tick;
		}
		#endregion

		#region Buttons and context menu reaction

		private void picDown_Click(object sender, RoutedEventArgs e)
		{
			if (CurrentRecord < TotalRecords)
				CurrentRecord++;
			else
				CurrentRecord = 1;

		}

		private void picUp_Click(object sender, RoutedEventArgs e)
		{
			if (CurrentRecord > 1)
				CurrentRecord--;
			else
				CurrentRecord = TotalRecords;

		}

		private void picMenu_Click(object sender, RoutedEventArgs e)
		{
			(sender as Button).ContextMenu.IsEnabled = true;
			(sender as Button).ContextMenu.PlacementTarget = (sender as Button);
			(sender as Button).ContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
			(sender as Button).ContextMenu.IsOpen = true;
		}


		private void searchMenuItem_Click(object sender, RoutedEventArgs e)
		{
			FindFlags flag = (FindFlags)FindFlags.Parse(FindFlags.GetType(), (sender as MenuItem).Tag.ToString());
			FindFlags ^= flag;
			OnSearch();
		}

		#endregion

		#region Text changed and timer
		private void tbSearch_TextChanged(object sender, TextChangedEventArgs e)
		{
			lblInfo.Visibility = (tbSearch.Text != "") ? Visibility.Visible : Visibility.Hidden;
			EnableButton(picUp, (tbSearch.Text != ""));
			EnableButton(picDown, (tbSearch.Text != ""));
			_onsearchTimer.Stop();
			_onsearchTimer.Start();
		}
        private void TbSearch_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.Enter) && (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)))
            {
                picUp_Click(null, null);
                e.Handled = true;
            }
            else if (Keyboard.IsKeyDown(Key.Enter))
            {
                picDown_Click(null, null);
                e.Handled = true;
            }
        }

        private void _onsearchTimer_Tick(object sender, EventArgs e)
		{
			_onsearchTimer.Stop();
			OnSearch();
		}
		#endregion

		#region Event handlers
		private void pnlHostTextBox_Click(object sender, EventArgs e)
		{
			tbSearch.Focus();
		}
		#endregion

		#region Private methods
		private void EnableButton(Button button, bool enabled)
		{
			switch (button.Name)
			{
				case "picUp":
					picUp.IsEnabled = enabled;
					break;
				case "picDown":
					picDown.IsEnabled = enabled;
					break;
			}
		}

		private void OnSearch()
		{
			if (NeedSearch != null)
				NeedSearch(this, EventArgs.Empty);
		}
        #endregion


    }
}
