using System;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;

namespace Hexkit.Global {

    /// <summary>
    /// Shows a dialog with basic program information.</summary>
    /// <remarks>
    /// Please refer to the "About Hexkit" page of the "Game Dialogs" or "Editor Dialogs" section in
    /// the application help file for details on this dialog.</remarks>

    public partial class AboutDialog: Window {
        #region AboutDialog()

        /// <summary>
        /// Initializes a new instance of the <see cref="AboutDialog"/> class.</summary>
        /// <remarks>
        /// This constructor is parameterless because <see cref="AboutDialog"/> retrieves all
        /// required data from static members of various <b>Global</b> classes.</remarks>

        public AboutDialog() {
            InitializeComponent();

            // hide warning if assemblies are unmodified
            if (ApplicationInfo.IsSigned)
                ModifiedNote.Visibility = Visibility.Collapsed;
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
        /// cref="AboutDialog"/>.</remarks>

        private void HelpExecuted(object sender, ExecutedRoutedEventArgs args) {
            args.Handled = true;
            ApplicationUtility.ShowHelp("DlgAbout.html");
        }

        #endregion
        #region OnLinkAuthor

        /// <summary>
        /// Handles the <see cref="Hyperlink.Click"/> event for the author <see cref="Hyperlink"/>.
        /// </summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="RoutedEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnLinkAuthor</b> supplies a "mailto:" URL to Windows Explorer that contains the
        /// author's e-mail address and the application signature. Explorer should respond by
        /// launching the default e-mail client and letting the user write an e-mail.</remarks>

        private void OnLinkAuthor(object sender, RoutedEventArgs args) {
            args.Handled = true;

            // construct shell command to e-mail comments
            string mailto = String.Format(
                CultureInfo.InvariantCulture, "mailto:{0}?subject={1} Comments",
                ApplicationInfo.Email, ApplicationInfo.Signature);

            try {
                // request default e-mail client
                Process.Start(mailto);
            }
            catch (Exception e) {
                ApplicationUtility.ShowExplorerError(this, Strings.DialogExplorerMailError, e);
            }
        }

        #endregion
        #region OnLinkWebsite

        /// <summary>
        /// Handles the <see cref="Hyperlink.Click"/> event for the website <see cref="Hyperlink"/>.
        /// </summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="RoutedEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnLinkWebsite</b> supplies the URL of the application website to Windows Explorer
        /// which should respond by launching the default web browser and showing that URL.
        /// </remarks>

        private void OnLinkWebsite(object sender, RoutedEventArgs args) {
            args.Handled = true;
            try {
                // request default web browser
                Process.Start(ApplicationInfo.Website);
            }
            catch (Exception e) {
                ApplicationUtility.ShowExplorerError(this, Strings.DialogExplorerWebError, e);
            }
        }

        #endregion
    }
}
