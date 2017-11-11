using System;
using System.Windows;
using System.Windows.Input;

using Hexkit.Global;

namespace Hexkit.Game.Dialog {

    /// <summary>
    /// Shows a dialog with basic program information.</summary>
    /// <remarks>
    /// Please refer to the "Show Event" page of the "Game Dialogs" section in the application help
    /// file for details on this dialog.</remarks>

    public partial class ShowEvent: Window {
        #region ShowEvent()

        /// <summary>
        /// Initializes a new instance of the <see cref="ShowEvent"/> class.</summary>
        /// <remarks>
        /// Clients should use the static <see cref="Show"/> method to display event messages.
        /// </remarks>

        public ShowEvent() {
            InitializeComponent();
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
        /// cref="ShowEvent"/> dialog.</remarks>

        private void HelpExecuted(object sender, ExecutedRoutedEventArgs args) {
            args.Handled = true;
            ApplicationUtility.ShowHelp("DlgShowEvent.html");
        }

        #endregion
        #region Show

        /// <summary>
        /// Displays a modal <see cref="ShowEvent"/> dialog with the specified owner, event message,
        /// and event caption.</summary>
        /// <param name="owner">
        /// The initial value for the <see cref="Window.Owner"/> property of the dialog.</param>
        /// <param name="message">
        /// The event message to display in the dialog.</param>
        /// <param name="caption">
        /// The event caption to display in the dialog.</param>
        /// <remarks>
        /// <b>Show</b> displays a modal <see cref="ShowEvent"/> dialog with the specified
        /// parameters. The dialog is centered on the screen if the specified <paramref
        /// name="owner"/> is a null reference.</remarks>

        public static void Show(Window owner, string message, string caption) {

            var dialog = new ShowEvent();
            dialog.EventCaption.Text = caption;
            dialog.EventMessage.Text = message;

            // center on screen if no owner specified
            if (owner == null)
                dialog.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            else
                dialog.Owner = owner;

            dialog.ShowDialog();
        }

        #endregion
    }
}
