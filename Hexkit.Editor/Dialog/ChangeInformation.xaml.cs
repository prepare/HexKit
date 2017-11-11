using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

using Tektosyne;
using Hexkit.Global;
using Hexkit.Scenario;

namespace Hexkit.Editor.Dialog {

    /// <summary>
    /// Shows a dialog allowing the user to change an <see cref="Information"/> block.</summary>
    /// <remarks>
    /// Please refer to the "Change Information" page of the "Editor Dialogs" section in the
    /// application help file for details on this dialog.</remarks>

    public partial class ChangeInformation: Window {
        #region ChangeInformation(Information, String)

        /// <summary>
        /// Initializes a new instance of the <see cref="ChangeInformation"/> class with the
        /// specified <see cref="Information"/> block and dialog caption.</summary>
        /// <param name="info">
        /// The <see cref="Information"/> block whose data to change.</param>
        /// <param name="caption">
        /// The text to display in the title bar of the dialog.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="info"/> is a null reference.</exception>
        /// <exception cref="ArgumentNullOrEmptyException">
        /// <paramref name="caption"/> is a null reference or an empty string.</exception>
        /// <remarks>
        /// The data of the specified <paramref name="info"/> may be changed in the dialog, as
        /// indicated by the value of the <see cref="DataChanged"/> property.</remarks>

        public ChangeInformation(Information info, string caption) {
            if (info == null)
                ThrowHelper.ThrowArgumentNullException("info");
            if (String.IsNullOrEmpty(caption))
                ThrowHelper.ThrowArgumentNullOrEmptyException("caption");

            this._info = info;
            InitializeComponent();
            Title = caption;

            // initialize text boxes with property values
            AuthorBox.Text = info.Author;
            VersionBox.Text = info.Version;
            LegalBox.Text = info.Legal;
            DetailBox.Text = String.Join(Environment.NewLine, info.Paragraphs);

            // construction completed
            this._initialized = true;
        }

        #endregion
        #region Private Fields

        // construction parameters
        private readonly Information _info;

        // was construction completed?
        private readonly bool _initialized = false;

        #endregion
        #region DataChanged

        /// <summary>
        /// Gets a value indicating whether any data was changed in the dialog.</summary>
        /// <value>
        /// <c>true</c> if any of the objects supplied to the constructor have been modified;
        /// otherwise, <c>false</c>.</value>
        /// <remarks>
        /// <b>DataChanged</b> returns <c>false</c> if the objects supplied to the constructor were
        /// not modified in any detectable way. However, the original data may have been overwritten
        /// with a copy that is not detectably different, namely if the user clicked <b>OK</b>
        /// without making any changes.</remarks>

        public bool DataChanged { get; private set; }

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
        /// cref="ChangeInformation"/> dialog.</remarks>

        private void HelpExecuted(object sender, ExecutedRoutedEventArgs args) {
            args.Handled = true;
            ApplicationUtility.ShowHelp("DlgChangeInformation.html");
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
        /// handles the <see cref="Window.Closing"/> event by clearing the <see cref="DataChanged"/>
        /// flag if the <see cref="Window.DialogResult"/> is not <c>true</c>, indicating that the
        /// user cancelled the dialog and wants to discard all changes.
        /// </para><para>
        /// Otherwise, <b>OnClosing</b> reads the control contents of this dialog into the <see
        /// cref="Information"/> object supplied to the constructor.</para></remarks>

        protected override void OnClosing(CancelEventArgs args) {
            base.OnClosing(args);
            if (args.Cancel) return;

            // user cancelled dialog, ignore changes
            if (DialogResult != true) {
                DataChanged = false;
                return;
            }

            // read text boxes into text properties
            this._info.Author = AuthorBox.Text;
            this._info.Version = VersionBox.Text;
            this._info.Legal = LegalBox.Text;

            // read detail paragraphs into string collection
            this._info.Paragraphs.Clear();
            this._info.Paragraphs.AddRange(DetailBox.Text.Split(
                new string[] { Environment.NewLine }, StringSplitOptions.None));
        }

        #endregion
        #region OnTextChanged

        /// <summary>
        /// Handles the <see cref="TextBoxBase.TextChanged"/> event for the "Author", "Version",
        /// "Copyright", and "Informational Text" <see cref="TextBox"/> controls.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="TextChangedEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnTextChanged</b> sets the <see cref="DataChanged"/> flag if the <see
        /// cref="TextChangedEventArgs.Changes"/> collection of the specified <paramref
        /// name="args"/> is not empty.</remarks>

        private void OnTextChanged(object sender, TextChangedEventArgs args) {
            args.Handled = true;
            if (this._initialized && args.Changes.Count > 0)
                DataChanged = true;
        }

        #endregion
    }
}
