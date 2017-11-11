using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Xml;

using Tektosyne;
using Hexkit.Global;

namespace Hexkit.Editor.Dialog {

    /// <summary>
    /// Shows a dialog allowing the user to change a unique internal identifier.</summary>
    /// <remarks><para>
    /// Please refer to the "Change Identifier" page of the "Editor Dialogs" section in the
    /// application help file for details on this dialog.
    /// </para><para>
    /// Unlike most other "Change" dialogs, <b>ChangeIdentifier</b> cannot change its construction
    /// argument directly because <see cref="String"/> objects are immutable in the .NET Framework.
    /// </para><para>
    /// The usual <b>DataChanged</b> property is therefore replaced with a <see
    /// cref="ChangeIdentifier.Identifier"/> property that allows clients to obtain the user input
    /// if the <see cref="Window.ShowDialog"/> method returns <c>true</c>.</para></remarks>

    public partial class ChangeIdentifier: Window {
        #region ChangeIdentifier(...)

        /// <summary>
        /// Initializes a new instance of the <see cref="ChangeIdentifier"/> class with the
        /// specified identifier and dialog caption.</summary>
        /// <param name="identifier">
        /// The initial value for the <see cref="Identifier"/> property.</param>
        /// <param name="caption">
        /// The text to display in the title bar of the dialog.</param>
        /// <param name="isDuplicate">
        /// The <see cref="Func{String, Boolean}"/> that determines whether the new <see
        /// cref="Identifier"/> is a duplicate of an existing identifier.</param>
        /// <param name="requireChange">
        /// <c>true</c> to silently return a <see cref="Window.DialogResult"/> of <c>false</c> if
        /// the user clicks OK without changing the specified <paramref name="identifier"/>;
        /// otherwise, <c>false</c>.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="isDuplicate"/> is a null reference.</exception>
        /// <exception cref="ArgumentNullOrEmptyException">
        /// <paramref name="identifier"/> or <paramref name="caption"/> is a null reference or an
        /// empty string.</exception>
        /// <remarks><para>
        /// <b>ChangeIdentifier</b> only returns a <see cref="Window.DialogResult"/> of <c>true</c>
        /// if the value of the <see cref="Identifier"/> property meets to the following conditions:
        /// </para><list type="bullet"><item>
        /// <see cref="Identifier"/> is not a null reference or an empty string.
        /// </item><item>
        /// <paramref name="requireChange"/> is <c>false</c> or <see cref="Identifier"/> does not
        /// equal the specified initial <paramref name="identifier"/>.
        /// </item><item>
        /// <see cref="Identifier"/> is a valid XML NCName.
        /// </item><item>
        /// <paramref name="isDuplicate"/> returns <c>false</c> for <see cref="Identifier"/>.
        /// </item></list></remarks>

        public ChangeIdentifier(string identifier, string caption,
            Func<String, Boolean> isDuplicate, bool requireChange) {

            if (String.IsNullOrEmpty(identifier))
                ThrowHelper.ThrowArgumentNullException("identifier");
            if (String.IsNullOrEmpty(caption))
                ThrowHelper.ThrowArgumentNullOrEmptyException("caption");
            if (isDuplicate == null)
                ThrowHelper.ThrowArgumentNullOrEmptyException("isDuplicate");

            this._identifier = identifier;
            this._isDuplicate = isDuplicate;
            this._requireChange = requireChange;

            InitializeComponent();
            Title = caption;

            // set focus on identifier
            IdentifierBox.Text = identifier;
            IdentifierBox.SelectAll();
            IdentifierBox.Focus();
        }

        #endregion
        #region Private Fields

        // construction parameters
        private readonly string _identifier;
        private readonly Func<String, Boolean> _isDuplicate;
        private readonly bool _requireChange;

        #endregion
        #region Identifier

        /// <summary>
        /// Gets the identifier entered by the user.</summary>
        /// <value>
        /// The value of the <see cref="TextBox.Text"/> property of the <see cref="TextBox"/>
        /// control used to show and change the identifier supplied to the constructor.</value>
        /// <remarks>
        /// <b>Identifier</b> provides access to the text entered by the user when the <see
        /// cref="Window.ShowDialog"/> call has returned.</remarks>

        public string Identifier {
            [DebuggerStepThrough]
            get { return IdentifierBox.Text; }
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
        /// cref="ChangeIdentifier"/> dialog.</remarks>

        private void HelpExecuted(object sender, ExecutedRoutedEventArgs args) {
            args.Handled = true;
            ApplicationUtility.ShowHelp("DlgChangeIdentifier.html");
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
        /// handles the <see cref="Window.Closing"/> event by validating the <see
        /// cref="Identifier"/> property, as follows:
        /// </para><list type="bullet"><item>
        /// If <see cref="Identifier"/> is a null reference or an empty string, <b>OnClosing</b>
        /// sets the <see cref="Window.DialogResult"/> to <c>false</c>.
        /// </item><item>
        /// If <see cref="Identifier"/> is unchanged but the caller required a change,
        /// <b>OnClosing</b> sets the <see cref="Window.DialogResult"/> to <c>false</c>.
        /// </item><item>
        /// If <see cref="Identifier"/> is not a valid XML NCName, <b>OnClosing</b> shows an error
        /// message and cancels the <see cref="Window.Closing"/> event.
        /// </item><item>
        /// If the <see cref="Func{String, Boolean}"/> method supplied to the constructor to test
        /// for duplicates returns <c>true</c> for <see cref="Identifier"/>, <b>OnClosing</b> shows
        /// an error message and cancels the <see cref="Window.Closing"/> event.
        /// </item></list></remarks>

        protected override void OnClosing(CancelEventArgs args) {
            base.OnClosing(args);
            if (args.Cancel) return;

            // interpret empty or unchanged input as cancellation
            if (String.IsNullOrEmpty(Identifier) ||
                (this._requireChange && Identifier == this._identifier))
                DialogResult = false;

            // quit immediately if user cancelled
            if (DialogResult != true)
                return;

            // malformed identifiers are not allowed
            if (!XmlReader.IsNameToken(Identifier) || Identifier.IndexOf(':') >= 0) {
                MessageBox.Show(this,
                    Global.Strings.DialogIdentifierMalformed,
                    Global.Strings.TitleIdentifierInvalid,
                    MessageBoxButton.OK, MessageBoxImage.Information);

                args.Cancel = true;
                return;
            }

            // duplicate identifiers are not allowed
            if (this._isDuplicate(Identifier)) {
                MessageBox.Show(this,
                    Global.Strings.DialogIdentifierDuplicate,
                    Global.Strings.TitleIdentifierInvalid,
                    MessageBoxButton.OK, MessageBoxImage.Information);

                args.Cancel = true;
                return;
            }
        }

        #endregion
    }
}
