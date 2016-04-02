using System;
using System.Windows.Controls;

namespace Patagames.Pdf.Net.Controls.Wpf.ToolBars
{
	/// <summary>
	/// Provides a container for Windows toolbar objects with predefined functionality for zooming
	/// </summary>
	public class PdfToolBarZoom : PdfToolBarZoomEx
	{
		#region Constructor, Destructor, Initialisation
		private ComboBox CreateZoomCombo()
		{
			var btn = new ComboBox();
			btn.Name = "btnComboBox";
			btn.ToolTip = new ToolTip()
			{
				Content = Properties.Resources.btnZoomComboToolTipText
			};
			btn.KeyDown += ComboBox_KeyDown;
			btn.SelectionChanged += ComboBox_SelectionChanged;
			btn.Width = 70;
			for (int i = 0; i < ZoomLevel.Length; i++)
				btn.Items.Add(string.Format("{0:.00}%", ZoomLevel[i]));
			return btn;
		}

		#endregion

		#region Overriding
		/// <summary>
		/// Create all buttons and add its into toolbar. Override this method to create custom buttons
		/// </summary>
		protected override void InitializeButtons()
		{
			var btn = CreateButton("btnZoomOut",
				Properties.Resources.btnZoomOutText,
				Properties.Resources.btnZoomOutToolTipText,
				"zoomOut.png",
				btn_ZoomOutClick);
			this.Items.Add(btn);

			var combo = CreateZoomCombo();
			this.Items.Add(combo);

			btn = CreateButton("btnZoomIn",
				Properties.Resources.btnZoomInText,
				Properties.Resources.btnZoomInToolTipText,
				"zoomIn.png",
				btn_ZoomInClick);
			this.Items.Add(btn);

		}

		/// <summary>
		/// Called when the ToolBar's items need to change its states
		/// </summary>
		protected override void UpdateButtons()
		{
			var combo = this.Items[1] as ComboBox;
			if (combo != null)
				combo.IsEnabled = (PdfViewer != null) && (PdfViewer.Document != null);

			var tsi = this.Items[0] as Button;
			if (tsi != null)
				tsi.IsEnabled = (PdfViewer != null) && (PdfViewer.Document != null);

			tsi = this.Items[2] as Button;
			if (tsi != null)
				tsi.IsEnabled = (PdfViewer != null) && (PdfViewer.Document != null);

			if (PdfViewer == null || PdfViewer.Document == null)
				return;

			CalcCurrentZoomLevel();

			if (combo != null)
				combo.Text = string.Format("{0:.00}%", Zoom);

		}
		#endregion

		#region Event handlers for buttons
		private void btn_ZoomOutClick(object sender, System.EventArgs e)
		{
			OnZoomExOutClick(this.Items[0] as Button);
		}
		private void btn_ZoomInClick(object sender, System.EventArgs e)
		{
			OnZoomExInClick(this.Items[2] as Button);
		}

		private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if ((sender as ComboBox).SelectedIndex < 0)
				return;
			OnComboBoxSelectionChanged(sender as ComboBox, (sender as ComboBox).SelectedIndex);
		}


		private void ComboBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
		{
			OnComboBoxKeyDown(this.Items[1] as ComboBox, e);
		}
		#endregion

		#region Protected methods
		/// <summary>
		/// Occurs the the selected index changed
		/// </summary>
		/// <param name="item">ComboBox</param>
		/// <param name="selectedIndex">Selected index</param>
		protected virtual void OnComboBoxSelectionChanged(ComboBox item, int selectedIndex)
		{
			SetZoom(selectedIndex);
		}

		/// <summary>
		/// Occurs when a key is pressed and held down while the ComboBox has focus.
		/// </summary>
		/// <param name="item">ComboBox</param>
		/// <param name="e">Key event args</param>
		protected virtual void OnComboBoxKeyDown(ComboBox item, System.Windows.Input.KeyEventArgs e)
		{
			if (item == null)
				return;
			if (e.Key == System.Windows.Input.Key.Enter)
			{
				double zoom = 0;
				string text = item.Text.Replace("%", "").Replace(" ", "");
				var t = text;
				if (!double.TryParse(t, out zoom))
				{
					t = text.Replace(".", ",");
					if (!double.TryParse(t, out zoom))
					{
						t = text.Replace(",", ".");
						if (!double.TryParse(t, out zoom))
						{
							return;
						}
					}
				}
				if (zoom < ZoomLevel[0])
					zoom = ZoomLevel[0];
				else if (zoom > ZoomLevel[ZoomLevel.Length - 1])
					zoom = ZoomLevel[ZoomLevel.Length - 1];
				SetZoom(zoom / 100.0f);
				item.Text = string.Format("{0:.00}%", zoom);
			}
		}
		#endregion

	}
}
