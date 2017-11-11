using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

using Tektosyne;
using Hexkit.Global;

namespace Hexkit.Game.Dialog {
    #region Type Aliases

    using NumericUpDown = System.Windows.Forms.NumericUpDown;

    #endregion

    /// <summary>
    /// Shows a dialog allowing the user to change a turn index.</summary>
    /// <remarks>
    /// Please refer to the "Change Turn" page of the "Game Dialogs" section in the application help
    /// file for details on this dialog.</remarks>

    public partial class ChangeTurn: Window {
        #region ChangeTurn(Int32, String)

        /// <summary>
        /// Initializes a new instance of the <see cref="ChangeTurn"/> class with the specified
        /// initial turn index and caption text.</summary>
        /// <param name="turn">
        /// The initial value for the <see cref="Turn"/> property. This is also the maximum turn
        /// index that will be accepted.</param>
        /// <param name="caption">
        /// The text to display in the title bar of the dialog.</param>
        /// <exception cref="ArgumentNullOrEmptyException">
        /// <paramref name="caption"/> is a null reference or an empty string.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="turn"/> is less than zero.</exception>

        public ChangeTurn(int turn, string caption) {
            if (turn < 0)
                ThrowHelper.ThrowArgumentOutOfRangeException(
                    "turn", turn, Tektosyne.Strings.ArgumentNegative);

            if (String.IsNullOrEmpty(caption))
                ThrowHelper.ThrowArgumentNullOrEmptyException("caption");

            InitializeComponent();
            Title = caption;

            // set focus on turn index
            TurnUpDown.Maximum = turn + 1;
            TurnUpDown.Value = turn + 1;
            TurnUpDown.Select();
        }

        #endregion
        #region Turn

        /// <summary>
        /// Gets the turn index entered by the user.</summary>
        /// <value>
        /// One less than the <see cref="NumericUpDown.Value"/> of the <see cref="NumericUpDown"/>
        /// control used to show and change the turn index supplied to the constructor.</value>
        /// <remarks><para>
        /// <b>Turn</b> provides access to the turn index entered by the user when the <see
        /// cref="Window.ShowDialog()"/> call has returned.
        /// </para><para>
        /// The value of this property is never negative, and always less than or equal to the turn
        /// index supplied to the constructor.</para></remarks>

        public int Turn {
            get { return (int) TurnUpDown.Value - 1; }
        }

        #endregion
        #region HelpExecuted

        /// <summary>
        /// Handles the <see cref="CommandBinding.Executed"/> event for the <see
        /// cref="ApplicationCommands.Help"/> command.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// An <see cref="ExecutedRoutedEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>HelpExecuted</b> opens the application help file on the help page for the <see
        /// cref="ChangeTurn"/> dialog.</remarks>

        private void HelpExecuted(object sender, ExecutedRoutedEventArgs args) {
            args.Handled = true;
            ApplicationUtility.ShowHelp("DlgChangeTurn.html");
        }

        #endregion
        #region OnAccept

        /// <summary>
        /// Handles the <see cref="ButtonBase.Click"/> event for the "OK" <see cref="Button"/>.
        /// </summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="RoutedEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnAccept</b> sets the <see cref="Window.DialogResult"/> property to <c>true</c>. This
        /// also triggers the <see cref="Window.Closing"/> event.</remarks>

        private void OnAccept(object sender, RoutedEventArgs args) {
            args.Handled = true;
            DialogResult = true;
        }

        #endregion
    }
}
