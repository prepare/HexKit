using System;
using System.Windows.Controls;
using System.Windows.Media;

using Tektosyne;

namespace Hexkit.Global {

    /// <summary>
    /// Provides mnemonic names for symbolic characters.</summary>
    /// <remarks>
    /// <b>Symbols</b> defines single-character strings intended for display with the Microsoft
    /// Wingdings font, unless otherwise noted.</remarks>

    public static class Symbols {
        #region Arrow Characters

        /// <summary>An arrow pointing down.</summary>
        public const string ArrowDown = "\x00F2";

        /// <summary>An arrow pointing left.</summary>
        public const string ArrowLeft = "\x00EF";

        /// <summary>An arrow pointing left and down.</summary>
        public const string ArrowLeftDown = "\x00F7";

        /// <summary>An arrow pointing left and up.</summary>
        public const string ArrowLeftUp = "\x00F5";

        /// <summary>An arrow pointing right.</summary>
        public const string ArrowRight = "\x00F0";

        /// <summary>An arrow pointing right and down.</summary>
        public const string ArrowRightDown = "\x00F8";

        /// <summary>An arrow pointing right and up.</summary>
        public const string ArrowRightUp = "\x00F6";

        /// <summary>An arrow pointing up.</summary>
        public const string ArrowUp = "\x00F1";

        #endregion
        #region Symbol Characters

        /// <summary>An empty box.</summary>
        public const string BoxEmpty   = "\x006F";

        /// <summary>An empty box with a diagonal cross.</summary>
        public const string BoxCrossed = "\x0078";

        /// <summary>A check mark.</summary>
        public const string CheckMark  = "\x00FC";

        /// <summary>A pencil.</summary>
        public const string Pencil = "\x0021";

        #endregion
        #region CheckMarkText

        /// <summary>
        /// A check mark equivalent for text fonts.</summary>
        /// <remarks>
        /// Use <b>CheckMarkText</b> instead of <see cref="CheckMark"/> when typesetting with a
        /// normal text font instead of the Wingdings font.</remarks>

        public const string CheckMarkText = "x";

        #endregion
        #region ShowSymbol

        /// <summary>
        /// Shows the specified symbolic character in the specified <see cref="ContentControl"/>.
        /// </summary>
        /// <param name="control">
        /// The <see cref="ContentControl"/> to manipulate.</param>
        /// <param name="symbol">
        /// The symbolic character to show in the specified <paramref name="control"/>.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="control"/> is a null reference.</exception>
        /// <exception cref="ArgumentNullOrEmptyException">
        /// <paramref name="symbol"/> is a null reference or an empty string.</exception>
        /// <remarks>
        /// <b>ShowSymbol</b> stores the specified <paramref name="symbol"/> as the new <see
        /// cref="ContentControl.Content"/> of the specified <paramref name="control"/>, sets its
        /// <see cref="Control.FontFamily"/> to Wingdings, and enlarges its <see
        /// cref="Control.FontSize"/> to compensate for the smaller Wingdings characters. </remarks>

        public static void ShowSymbol(this ContentControl control, string symbol) {
            if (control == null)
                ThrowHelper.ThrowArgumentNullException("control");
            if (String.IsNullOrEmpty(symbol))
                ThrowHelper.ThrowArgumentNullOrEmptyException("symbol");

            control.Content = symbol;
            control.FontFamily = new FontFamily("Wingdings");
            control.FontSize *= 1.3;
        }

        #endregion
    }
}
