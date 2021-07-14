using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media.Imaging;

namespace Patagames.Pdf.Net.Controls.Wpf.ToolBars
{
	/// <summary>
	/// Represents the base functionality for all PdfToolBars
	/// </summary>
	public class PdfToolBar : ToolBar
	{
		/// <summary>
		/// Specifies what to render (image or text) for toolbar item
		/// </summary>
		protected enum ImageTextType
		{
			/// <summary>
			/// Specifies that only an image is to be rendered for toolbar item.
			/// </summary>
			ImageOnly,

			/// <summary>
			///  Specifies that only text is to be rendered for toolbar item.
			/// </summary>
			TextOnly,

			/// <summary>
			/// Specifies that both an image and text are to be rendered.
			/// </summary>
			ImageBeforeText
		};

		#region Private fields
		private PdfViewer _pdfViewer = null;
		#endregion

		#region Public properties

		/// <summary>
		/// DependencyProperty as the backing store for PdfViewer
		/// </summary>
		public static readonly DependencyProperty PdfViewerProperty =
			DependencyProperty.Register("PdfViewer", typeof(PdfViewer), typeof(PdfToolBar), new PropertyMetadata(null, (o, e) => 
			{
                bool b = System.ComponentModel.DesignerProperties.GetIsInDesignMode(o as PdfToolBar);
                if (e.OldValue!= e.NewValue && !b)
					(o as PdfToolBar).OnPdfViewerChanging(e.OldValue as PdfViewer, e.NewValue as PdfViewer);
			}));

		/// <summary>
		/// Gets or sets PdfViewer control associated with this PdfToolBar control
		/// </summary>
		public PdfViewer PdfViewer
		{
			get { return (PdfViewer)GetValue(PdfViewerProperty); }
			set { SetValue(PdfViewerProperty, value); }
		}
		#endregion

		#region Constructors, destructors, initialisation
		/// <summary>
		/// Initialize the new instance of PdfToolBar class
		/// </summary>
		public PdfToolBar()
		{
			InitializeButtons();
			UpdateButtons();
		}
		#endregion

		#region Protected methods
		/// <summary>
		/// Create all buttons and add its into toolbar. Override this method to create custom buttons
		/// </summary>
		protected virtual void InitializeButtons()
		{

		}

		/// <summary>
		/// Called when the ToolBars's items need to change its states
		/// </summary>
		protected virtual void UpdateButtons()
		{

		}

		/// <summary>
		/// Called when the current PdfViewer control associated with the ToolBar is changing.
		/// </summary>
		/// <param name="oldValue">PdfViewer control of which was associated with the ToolBar.</param>
		/// <param name="newValue">PdfViewer control of which will be associated with the ToolBar.</param>
		protected virtual void OnPdfViewerChanging(PdfViewer oldValue, PdfViewer newValue)
		{
			_pdfViewer = newValue;
			UpdateButtons();
		}

        /// <summary>
        /// Create the Uri to the resource with the specified name.
        /// </summary>
        /// <param name="resName">Resource's name.</param>
        /// <returns>Uri to the resource.</returns>
        protected virtual Uri CreateUriToResource(string resName)
        {
            return new Uri("pack://application:,,,/Patagames.Pdf.Wpf;component/Resources/" + resName, UriKind.Absolute);
        }

        /// <summary>
        /// Create a new instance of Button class with the specified name that displays the specified text and image and that raises the Click event.
        /// </summary>
        /// <param name="name">The name of the Button.</param>
        /// <param name="text">The text to display on the Button.</param>
        /// <param name="toolTipText">Specify the text that appears as a ToolTip for a control.</param>
        /// <param name="imgRes">The image to display on the Button.</param>
        /// <param name="onClick">An event handler that raises the Click event.</param>
        /// <param name="imgWidth">Image width</param>
        /// <param name="imgHeight">Image height</param>
        /// <param name="imageTextType">Image and text layout</param>
        /// <returns>Newly created Button</returns>
        protected virtual Button CreateButton(string name, string text, string toolTipText, Uri imgRes, RoutedEventHandler onClick, int imgWidth=32, int imgHeight=32, ImageTextType imageTextType = ImageTextType.ImageBeforeText)
		{
			Button btn = new Button();
			btn.Name = name;
			btn.ToolTip = new ToolTip()
			{
				Content = toolTipText
			};
			var btncontent = new StackPanel();
			Image img = null;
			TextBlock txt = null;

			if (imgRes != null)
				img = new Image()
				{
					Source = new BitmapImage(imgRes),
					Stretch = System.Windows.Media.Stretch.Fill,
					Width = imgWidth,
					Height = imgHeight,
				};
			if (text != null)
				txt = new TextBlock()
				{
					Text = text,
					TextAlignment = TextAlignment.Center
				};
			btn.Content = btncontent;
			btn.Click += onClick;
			btn.Padding = new Thickness(7, 2, 7, 2);

			if(imageTextType== ImageTextType.ImageBeforeText || imageTextType== ImageTextType.ImageOnly)
				btncontent.Children.Add(img);
			if (imageTextType == ImageTextType.ImageBeforeText || imageTextType == ImageTextType.TextOnly)
				btncontent.Children.Add(txt);

			return btn;
		}

		/// <summary>
		/// Create a new instance of ToggleButon class with the specified name that displays the specified text and image and that raises the Click event.
		/// </summary>
		/// <param name="name">The name of the ToggleButton.</param>
		/// <param name="text">The text to display on the ToggleButton.</param>
		/// <param name="toolTipText">Specify the text that appears as a ToolTip for a control.</param>
		/// <param name="imgResName">The image name in resources to display on the ToggleButton.</param>
		/// <param name="onClick">An event handler that raises the Click event.</param>
		/// <param name="imgWidth">Image width</param>
		/// <param name="imgHeight">Image height</param>
		/// <param name="imageTextType">Image and text layout</param>
		/// <returns>Newly created ToggleButton</returns>
		protected virtual ToggleButton CreateToggleButton(string name, string text, string toolTipText, string imgResName, RoutedEventHandler onClick, int imgWidth = 32, int imgHeight = 32, ImageTextType imageTextType = ImageTextType.ImageBeforeText)
		{
			ToggleButton btn = new ToggleButton();
			btn.Name = name;
			btn.ToolTip = new ToolTip()
			{
				Content = toolTipText
			};
			var btncontent = new StackPanel();
			Image img = null;
			TextBlock txt = null;

			if (imgResName != null)
				img = new Image()
				{
					Source = new BitmapImage(new Uri("pack://application:,,,/Patagames.Pdf.Wpf;component/Resources/" + imgResName, UriKind.Absolute)),
					Stretch = System.Windows.Media.Stretch.Fill,
					Width = imgWidth,
					Height = imgHeight,
				};
			if (text != null)
				txt = new TextBlock()
				{
					Text = text,
					TextAlignment = TextAlignment.Center
				};

			btn.Content = btncontent;
			btn.Click += onClick;
			btn.Padding = new Thickness(7, 2, 7, 2);

			if (imageTextType == ImageTextType.ImageBeforeText || imageTextType == ImageTextType.ImageOnly)
				btncontent.Children.Add(img);
			if (imageTextType == ImageTextType.ImageBeforeText || imageTextType == ImageTextType.TextOnly)
				btncontent.Children.Add(txt);

			return btn;
		}

		#endregion
	}
}
