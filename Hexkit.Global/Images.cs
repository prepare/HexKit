using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

using Tektosyne.Windows;

namespace Hexkit.Global {

    /// <summary>
    /// Provides access to images stored as embedded resources.</summary>

    public static class Images {
        #region Application

        /// <summary>
        /// The application icon in <see cref="BitmapImage"/> format.</summary>
        /// <remarks>
        /// <b>Application</b> should contain the same icon that is embedded in the application
        /// executable and used by Windows Explorer to represent the application.</remarks>

        public readonly static BitmapImage Application = new BitmapImage(
            new Uri("pack://application:,,,/Hexkit.Global;Component/Hexkit.png"));

        #endregion
        #region Error

        /// <summary>
        /// The standard <see cref="MessageBoxImage.Error"/> icon in <see cref="BitmapSource"/>
        /// format.</summary>
        /// <remarks>
        /// <b>Error</b> caches the bitmap created by <see cref="WindowsUtility.GetSystemBitmap"/>
        /// for use with WPF <see cref="Image"/> elements.</remarks>

        public readonly static BitmapSource Error =
            WindowsUtility.GetSystemBitmap(MessageBoxImage.Error);

        #endregion
        #region Information

        /// <summary>
        /// The standard <see cref="MessageBoxImage.Information"/> icon in <see cref="BitmapSource"/>
        /// format.</summary>
        /// <remarks>
        /// <b>Information</b> caches the bitmap created by <see cref="WindowsUtility.GetSystemBitmap"/>
        /// for use with WPF <see cref="Image"/> elements.</remarks>

        public readonly static BitmapSource Information =
            WindowsUtility.GetSystemBitmap(MessageBoxImage.Information);

        #endregion
        #region Invalid

        /// <summary>
        /// An icon indicating an invalid state or operation, in <see cref="BitmapImage"/> format.
        /// </summary>
        /// <remarks>
        /// <b>Invalid</b> contains a <see cref="BitmapImage"/> of a red circle with a slash,
        /// intended as a symbol for an invalid state or operation.</remarks>

        public readonly static BitmapImage Invalid = new BitmapImage(
            new Uri("pack://application:,,,/Hexkit.Global;Component/Invalid.png"));

        #endregion
        #region Warning

        /// <summary>
        /// The standard <see cref="MessageBoxImage.Warning"/> icon in <see cref="BitmapSource"/>
        /// format.</summary>
        /// <remarks>
        /// <b>Warning</b> caches the bitmap created by <see cref="WindowsUtility.GetSystemBitmap"/>
        /// for use with WPF <see cref="Image"/> elements.</remarks>

        public readonly static BitmapSource Warning =
            WindowsUtility.GetSystemBitmap(MessageBoxImage.Warning);

        #endregion
    }
}
