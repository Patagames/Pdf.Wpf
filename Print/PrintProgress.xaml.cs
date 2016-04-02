using System;
using System.Windows;

namespace Patagames.Pdf.Net.Controls.Wpf.ToolBars
{
	/// <summary>
	/// Interaction logic for PrintProgress.xaml
	/// </summary>
	internal partial class PrintProgress : Window
	{
		delegate void DelegateType1();

		public event EventHandler NeedStopPrinting;

		public PrintProgress()
		{
			InitializeComponent();

			this.Closing += PrintProgress_Closing;
		}

		private void PrintProgress_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			e.Cancel = true;
			DelegateType1 method = ShowMessageBoxOnClose;
			Dispatcher.BeginInvoke(method);
			//Dispatcher.BeginInvoke(new Action(() =>
			//{
			//	if (MessageBox.Show(Properties.Resources.txtPromptPrintAbort, Properties.Resources.InfoHeader, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
			//		Dispatcher.BeginInvoke(new Action(() => { if (NeedStopPrinting != null) NeedStopPrinting(this, EventArgs.Empty); }));
			//}));
		}

		private void ShowMessageBoxOnClose()
		{
			DelegateType1 method = SendCancelationEvent;
			if (MessageBox.Show(Properties.Resources.txtPromptPrintAbort, Properties.Resources.InfoHeader, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
				Dispatcher.BeginInvoke(method);
		}

		private void SendCancelationEvent()
		{
			if (NeedStopPrinting != null)
				NeedStopPrinting(this, EventArgs.Empty);
		}

		internal void CloseWithoutPrompt()
		{
			this.Closing -= PrintProgress_Closing;
			this.Owner = null;
			Close();
		}

		internal void SetText(int pageNumber, int count)
		{
			textBlock.Text = string.Format(Properties.Resources.txtPrinting, pageNumber, count);
		}

		internal void SetText(string txt)
		{
			textBlock.Text = txt;
		}

		private void button_Click(object sender, RoutedEventArgs e)
		{
			Close();
		}
	}
}
