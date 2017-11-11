using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

using Tektosyne;
using Hexkit.Global;

namespace Hexkit.Game.Dialog {

    /// <summary>
    /// Shows a dialog allowing the user to change an e-mail address.</summary>
    /// <remarks><para>
    /// Please refer to the "Change E-mail" page of the "Game Dialogs" section in the application
    /// help file for details on this dialog.
    /// </para><para>
    /// Unlike most other "Change" dialogs, <b>ChangeEmail</b> cannot change its construction
    /// argument directly because <see cref="String"/> objects are immutable in the .NET Framework.
    /// </para><para>
    /// The usual <b>DataChanged</b> property is therefore replaced with a <see
    /// cref="ChangeEmail.Address"/> property that allows clients to obtain the user input if the
    /// <see cref="Window.ShowDialog"/> method returns <c>true</c>.</para></remarks>

    public partial class ChangeEmail: Window {
        #region ChangeEmail(String, String)

        /// <summary>
        /// Initializes a new instance of the <see cref="ChangeEmail"/> class with the specified
        /// initial content and additional caption text.</summary>
        /// <param name="address">
        /// The initial value for the <see cref="Address"/> property.</param>
        /// <param name="caption">
        /// Additional text to display in the title bar of the dialog.</param>
        /// <exception cref="ArgumentNullOrEmptyException">
        /// <paramref name="caption"/> is a null reference or an empty string.</exception>

        public ChangeEmail(string address, string caption) {
            if (String.IsNullOrEmpty(caption))
                ThrowHelper.ThrowArgumentNullOrEmptyException("caption");

            InitializeComponent();
            Title += caption;

            // set focus on address
            AddressBox.Text = address;
            AddressBox.Focus();
        }

        #endregion
        #region Address

        /// <summary>
        /// Gets the e-mail address entered by the user.</summary>
        /// <value>
        /// The value of the <see cref="TextBox.Text"/> property of the <see cref="TextBox"/>
        /// control used to show and change the e-mail address supplied to the constructor.</value>
        /// <remarks>
        /// <b>Address</b> provides access to the text entered by the user when the <see
        /// cref="Window.ShowDialog"/> call has returned.</remarks>

        public string Address {
            [DebuggerStepThrough]
            get { return AddressBox.Text; }
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
        /// cref="ChangeEmail"/> dialog.</remarks>

        private void HelpExecuted(object sender, ExecutedRoutedEventArgs args) {
            args.Handled = true;
            ApplicationUtility.ShowHelp("DlgChangeEmail.html");
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
        #region OnClosing

        /// <summary>
        /// Raises and handles the <see cref="Window.Closing"/> event.</summary>
        /// <param name="args">
        /// A <see cref="CancelEventArgs"/> object containing event data.</param>
        /// <remarks><para>
        /// <b>OnClosing</b> raises the <see cref="Window.Closing"/> event by calling the base class
        /// implementation of <see cref="Window.OnClosing"/> with the specified <paramref
        /// name="args"/>.
        /// </para><para>
        /// If the event was not requested to <see cref="CancelEventArgs.Cancel"/>, <b>OnClosing</b>
        /// handles the <see cref="Window.Closing"/> event by verifying that <see cref="Address"/>
        /// contains either an empty string or a valid e-mail address. Otherwise, <b>OnClosing</b>
        /// requests the user to enter a valid e-mail address and cancels the event.
        /// </para></remarks>

        protected override void OnClosing(CancelEventArgs args) {
            base.OnClosing(args);
            if (args.Cancel) return;

            // do nothing if user cancelled or text box empty
            if (DialogResult != true || String.IsNullOrEmpty(Address))
                return;

            // check for valid e-mail address format
            if (!Address.IsValidEmail()) {
                MessageBox.Show(this,
                    Global.Strings.DialogMailInvalid, Global.Strings.TitleMailInvalid,
                    MessageBoxButton.OK, MessageBoxImage.Information);

                args.Cancel = true;
            }
        }

        #endregion
    }
}
