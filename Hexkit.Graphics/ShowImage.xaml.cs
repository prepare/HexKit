using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace Hexkit.Graphics {

    /// <summary>
    /// Shows a WPF <see cref="ImageSource"/> in a modeless dialog.</summary>

    public partial class ShowImage: Window {
        #region ShowImage(ImageSource)

        /// <summary>
        /// Initializes a new instance of the <see cref="ShowImage"/> class.</summary>
        /// <param name="image">
        /// The initial value for the <see cref="Image"/> property.</param>

        public ShowImage(ImageSource image) {
            InitializeComponent();
            Image = image;
        }

        #endregion
        #region Image

        /// <summary>
        /// Gets or sets the image shown by the <see cref="ShowImage"/> dialog.</summary>
        /// <value>
        /// The <see cref="ImageSource"/> for the <see cref="ShowImage"/> dialog.</value>

        public ImageSource Image {
            get { return ImageHost.Source as ImageSource; }
            set { ImageHost.Source = value; }
        }

        #endregion
        #region OnClose

        /// <summary>
        /// Handles the <see cref="ButtonBase.Click"/> event for the "Close" <see cref="Button"/>.
        /// </summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="RoutedEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnClose</b> calls <see cref="Window.Close"/> to close the modeless <see
        /// cref="ShowImage"/> dialog.</remarks>

        private void OnClose(object sender, RoutedEventArgs args) {
            args.Handled = true;
            Close();
        }

        #endregion
    }
}
