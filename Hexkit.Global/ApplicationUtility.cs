using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;

using Tektosyne;
using Tektosyne.IO;
using Tektosyne.Net;
using Tektosyne.Windows;

namespace Hexkit.Global {

    /// <summary>
    /// Provides helper methods for the application.</summary>

    public static class ApplicationUtility {
        #region Private Fields

        // template for separator list view item
        private static ControlTemplate _separatorTemplate;

        // mutex to ensure single instance
        private static Mutex _mutex;

        #endregion
        #region AddSeparator

        /// <summary>
        /// Adds a <see cref="Separator"/> item to the specified <see cref="ListView"/>.</summary>
        /// <param name="listView">
        /// The <see cref="ListView"/> that receives the <see cref="Separator"/> item.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="listView"/> is a null reference.</exception>
        /// <remarks><para>
        /// <b>AddSeparator</b> adds an empty <see cref="ListViewItem"/> to the specified <paramref
        /// name="listView"/> whose <see cref="Control.Template"/> shows a <see cref="Separator"/>.
        /// </para><para>
        /// This explicit <see cref="Control.Template"/> overrides any implicit data templates of
        /// the specified <paramref name="listView"/>, allowing the display of <see
        /// cref="Separator"/> items in a multi-column <see cref="GridView"/>.</para></remarks>

        public static void AddSeparator(this ListView listView) {
            if (listView == null)
                ThrowHelper.ThrowArgumentNullException("listView");

            if (ApplicationUtility._separatorTemplate == null)
                ApplicationUtility._separatorTemplate = (ControlTemplate)
                    Application.Current.MainWindow.FindResource("listViewSeparator");

            var item = new ListViewItem();
            item.Template = ApplicationUtility._separatorTemplate;
            listView.Items.Add(item);
        }

        #endregion
        #region ApplyDefaultStyle

        /// <summary>
        /// Applies a default style to the specified <see cref="FrameworkElement"/>.</summary>
        /// <param name="element">
        /// The <see cref="FrameworkElement"/> whose display parameters to adjust.</param>
        /// <remarks><para>
        /// <b>ApplyDefaultStyle</b> does nothing if the specified <paramref name="element"/> is a
        /// null reference; otherwise, its <see cref="FrameworkElement.Margin"/> is set to a uniform
        /// <see cref="Thickness"/> of four device-independent pixels.
        /// </para><para>
        /// Call this method in the constructor of any custom <see cref="FrameworkElement"/> that
        /// does not use a specific <see cref="FrameworkElement.Style"/>.</para></remarks>

        public static void ApplyDefaultStyle(FrameworkElement element) {
            if (element == null) return;
            element.Margin = new Thickness(4);
        }

        #endregion
        #region CheckSingleInstance

        /// <summary>
        /// Checks that the user is running only a single instance of the application.</summary>
        /// <returns>
        /// <c>true</c> if the current user is not running another instance of the current
        /// application; otherwise, <c>false</c>.</returns>
        /// <remarks><para>
        /// <b>CheckSingleInstance</b> attempts to create a <see cref="Mutex"/> whose name is
        /// composed of the application <see cref="ApplicationInfo.Title"/> and the current Windows
        /// <see cref="Environment.UserName"/>, separated by a dash.
        /// </para><para>
        /// If this <see cref="Mutex"/> already exists, <b>CheckSingleInstance</b> notifies the user
        /// that another application instance is already running and returns <c>false</c>.
        /// Otherwise, <b>CheckSingleInstance</b> silently returns <c>true</c>.</para></remarks>

        public static bool CheckSingleInstance() {

            // name mutex for application and user
            string title = ApplicationInfo.Title, user = Environment.UserName;
            string mutexName = String.Concat(title, " - ", user);

            /*
             * Attempt to get lock on global mutex. Note that the mutex
             * must be stored in a static field; otherwise, the garbage
             * collector would release it when the method exits!
             */

            bool firstInstance;
            ApplicationUtility._mutex = new Mutex(true, mutexName, out firstInstance);

            if (!firstInstance) {
                // lock failed: another instance exists
                string message = String.Format(ApplicationInfo.Culture,
                    Strings.DialogInstanceAnother, title, user);

                MessageBox.Show(message,
                    String.Format(ApplicationInfo.Culture, Strings.TitleInstanceAnother, title),
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }

            return firstInstance;
        }

        #endregion
        #region InitializeMenu

        /// <summary>
        /// Initializes the specified <see cref="Menu"/>.</summary>
        /// <param name="menu">
        /// The <see cref="Menu"/> whose items to process.</param>
        /// <param name="onHighlightChanged">
        /// An <see cref="EventHandler"/> to invoke when the <see cref="MenuItem.IsHighlighted"/>
        /// property of any <see cref="MenuItem"/> has changed.</param>
        /// <returns>
        /// A <see cref="Dictionary{String, String}"/> that maps the <see
        /// cref="FrameworkElement.Tag"/> text of each <see cref="MenuItem"/> to the corresponding
        /// <see cref="FrameworkElement.ToolTip"/> text.</returns>
        /// <remarks>
        /// <b>InitializeMenu</b> removes the <see cref="FrameworkElement.ToolTip"/> from each <see
        /// cref="MenuItem"/> after placing its text in the returned dictionary. Each <see
        /// cref="MenuItem"/> must have a unique <see cref="FrameworkElement.Tag"/> of type <see
        /// cref="String"/> to use as a key in this dictionary.</remarks>

        public static Dictionary<String, String> InitializeMenu(
            Menu menu, EventHandler onHighlightChanged) {

            if (menu == null)
                ThrowHelper.ThrowArgumentNullException("menu");
            if (onHighlightChanged == null)
                ThrowHelper.ThrowArgumentNullException("onHighlightChanged");

            // enqueue only items of type MenuItem (not Separator)
            var queue = new Queue<MenuItem>();
            Action<ItemCollection> enqueueItems = (items) => {
                for (int i = 0; i < items.Count; i++) {
                    MenuItem item = items[i] as MenuItem;
                    if (item != null) queue.Enqueue(item);
                }
            };

            var window = Application.Current.MainWindow;
            var tooltips = new Dictionary<String, String>();

            // traverse entire menu tree
            enqueueItems(menu.Items);
            while (queue.Count > 0) {
                MenuItem item = queue.Dequeue();
                enqueueItems(item.Items);

                /*
                 * HACK: The current WPF RoutedCommand architecture has a serious bug regarding
                 * menu items and input focus.
                 * 
                 * If we leave the CommandTarget of a MenuItem at its default value of null,
                 * the element with the keyboard focus will act as the actual target. However,
                 * if the focused element cannot receive keyboard input, any MenuItem associated
                 * with a RoutedCommand will be disabled until another element is selected!
                 * 
                 * Since our application commands never use the supplied CommandTarget anyway,
                 * we simply set this property to the current MainWindow to evade this issue.
                 */

                item.CommandTarget = window;

                // extract ToolTip text into separate dictionary
                tooltips.Add((string) item.Tag, item.ToolTip.ToString());
                item.ToolTip = null;

                // handle IsHighlighted changes for menu item
                DependencyPropertyDescriptor.FromProperty(
                    MenuItem.IsHighlightedProperty, typeof(MenuItem))
                    .AddValueChanged(item, onHighlightChanged);
            }

            return tooltips;
        }

        #endregion
        #region OnUnhandledException

        /// <summary>
        /// Handles the <see cref="Application.DispatcherUnhandledException"/> event.</summary>
        /// <param name="information">
        /// Information about the <see cref="Exception"/> and the current application state, or a
        /// null reference for no available information.</param>
        /// <param name="rulesFile">
        /// The file path to the current scenario rule script, or a null reference if no such file
        /// exists.</param>
        /// <param name="saveScenario">
        /// An optional <see cref="Action{String}"/> delegate that is invoked to save the current
        /// scenario data to an XML file.</param>
        /// <param name="saveSession">
        /// An optional <see cref="Action{String}"/> delegate that is invoked to save the current
        /// session data to an XML file.</param>
        /// <remarks><para>
        /// <b>OnUnhandledException</b> creates a text file describing the error which includes the
        /// specified <paramref name="information"/>. Additionally, up to two debug XML files
        /// containing scenario and session data are created by invoking any of the supplied <see
        /// cref="Action{String}"/> delegates that are not null references.
        /// </para><para>
        /// The user is also asked to e-mail all created files, plus the specified <paramref
        /// name="rulesFile"/> if it exists, to the e-mail address specified by the <see
        /// cref="ApplicationInfo.Email"/> property via Simple MAPI, or if this fails, to send basic
        /// error information to the same address using the Explorer "mailto" method.
        /// </para><para>
        /// Finally, <b>OnUnhandledException</b> terminates the application using <see
        /// cref="Environment.Exit"/> and returns an error code of -1.</para></remarks>

        public static void OnUnhandledException(string information, string rulesFile,
            Action<String> saveScenario, Action<String> saveSession) {

            // check for original distribution package
            bool original = ApplicationInfo.IsSigned;

            // determine error and debug file names
            string errorFile = FilePaths.CreateUserPath("FatalError.txt").AbsolutePath;
            string scenarioFile = FilePaths.GetScenarioFile(ScenarioFileType.Debug).AbsolutePath;
            string sessionFile = FilePaths.GetSessionFile(SessionFileType.Debug).AbsolutePath;

            // create an error e-mail?
            bool wantEmail = false;

            if (original) {
                // ask user to create e-mail with attachments
                string message = String.Format(ApplicationInfo.Culture,
                    Strings.DialogFatalErrorOriginal, errorFile, scenarioFile, sessionFile);

                wantEmail = ShowFatalError(message, null, true);
            }
            else {
                // just notify user that files are being saved
                string message = String.Format(ApplicationInfo.Culture,
                    Strings.DialogFatalErrorModified, errorFile, scenarioFile, sessionFile);

                ShowFatalError(message, null, false);
            }

            // create state information string
            StringBuilder info = new StringBuilder();
            info.AppendFormat("{0} ", ApplicationInfo.Signature);
            info.Append(Environment.NewLine);
            info.AppendFormat("Kynosarges Signature: {0}", original);
            info.Append(Environment.NewLine);
            info.AppendFormat("Public Key Token: {0}",
                StringUtility.Validate(ApplicationInfo.PublicKeyToken));
            info.Append(Environment.NewLine);
            info.AppendFormat("Home: \"{0}\" ", FilePaths.ApplicationFolder);
            info.Append(Environment.NewLine);
            info.Append(Environment.NewLine);
            info.AppendFormat("{0} ", Environment.OSVersion);
            info.Append(Environment.NewLine);
            info.AppendFormat("{0} ", WindowsUtility.GetMemoryStatus());
            info.Append(Environment.NewLine);
            info.Append(Environment.NewLine);

            // append additional information if specified
            if (!String.IsNullOrEmpty(information)) {
                info.AppendFormat("{0} ", information);
                info.Append(Environment.NewLine);
            }

            // create subject for e-mail message
            string subject = String.Concat(ApplicationInfo.Signature, " ", Strings.LabelError);

            // collection to hold file attachment data
            List<MapiAddress> files = new List<MapiAddress>();

            try {
                try {
                    // always create error information file
                    files.Add(new MapiAddress(Path.GetFileName(errorFile), errorFile));
                    using (StreamWriter writer = new StreamWriter(errorFile, false, Encoding.UTF8))
                        writer.WriteLine(info.ToString());

                    // attach rule script file if specified
                    RootedPath rulesPath = FilePaths.CreateCommonPath(rulesFile);
                    if (!rulesPath.IsEmpty && File.Exists(rulesPath.AbsolutePath))
                        files.Add(new MapiAddress(rulesPath.FileName, rulesPath.AbsolutePath));

                    // create scenario debug file if possible
                    if (saveScenario != null) {
                        saveScenario(scenarioFile);
                        files.Add(new MapiAddress(Path.GetFileName(scenarioFile), scenarioFile));
                    }

                    // create session debug file if possible
                    if (saveSession != null) {
                        saveSession(sessionFile);
                        files.Add(new MapiAddress(Path.GetFileName(sessionFile), sessionFile));
                    }
                }
                catch (Exception e) {

                    // only proceed if user wanted e-mail
                    if (wantEmail) {
                        // ask user to try simpler e-mail format
                        wantEmail = ShowFatalError(Strings.DialogFatalErrorSaveMail, e, true);
                        throw; // rethrow to try mailto:
                    }

                    // user declined e-mail, just show error
                    ShowFatalError(Strings.DialogFatalErrorSave, e, false);
                    return; // quit, nothing else to do
                }

                // quit now if user declined e-mail
                if (!wantEmail) return;

                try {
                    // address e-mail to application author
                    MapiAddress recipient = new MapiAddress(
                        ApplicationInfo.Company, ApplicationInfo.Email);

                    // try sending e-mail with attached files
                    MapiMail.SendMail(subject, "", new[] { recipient }, files.ToArray());
                }
                catch (Exception e) {
                    // quit if user cancelled MAPI session
                    MapiException me = e as MapiException;
                    if (me != null && me.Code == MapiException.Abort)
                        return;

                    // ask user to try simpler e-mail format
                    wantEmail = ShowFatalError(Strings.DialogFatalErrorMail, e, true);
                    throw; // rethrow to try mailto:
                }
            }
            catch {
                // quit now if user declined e-mail
                if (!wantEmail) return;

                // construct shell command to create e-mail
                string mailto = String.Format(CultureInfo.InvariantCulture,
                    "mailto:{0}?subject={1}&body={2}", ApplicationInfo.Email, subject, info);

                try {
                    // request default e-mail client
                    Process.Start(mailto);
                }
                catch (Exception e) {
                    ShowExplorerError(null, Strings.DialogExplorerMailError, e);
                }
            }
            finally {
                // quit application upon return
                Environment.Exit(-1);
            }
        }

        #endregion
        #region ShowExplorerError

        /// <summary>
        /// Shows a dialog indicating a Windows Explorer error.</summary>
        /// <param name="owner">
        /// The <see cref="Window.Owner"/> of the dialog.</param>
        /// <param name="message">
        /// The text to display in the primary message area.</param>
        /// <param name="e">
        /// An <see cref="Exception"/> object providing additional primary and/or secondary message
        /// text, or a null reference for no additional text.</param>
        /// <remarks><para>
        /// <b>ShowFatalError</b> shows a <see cref="MessageBox"/> if <paramref name="e"/> is a null
        /// reference, and a <see cref="MessageDialog"/> otherwise.
        /// </para><para>
        /// The dialog caption is always set to the localized string "Windows Explorer Error", and
        /// the dialog icon to the standard <see cref="MessageBoxImage.Error"/> icon.
        /// </para></remarks>

        internal static void ShowExplorerError(Window owner, string message, Exception e) {
            if (e != null) {
                MessageDialog.Show(owner, message, Strings.TitleExplorerError,
                    e, MessageBoxButton.OK, Images.Error);
            } else {
                MessageBox.Show(owner, message, Strings.TitleExplorerError,
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion
        #region ShowFatalError

        /// <summary>
        /// Shows a dialog indicating a fatal application error.</summary>
        /// <param name="message">
        /// The text to display in the primary message area.</param>
        /// <param name="e">
        /// An <see cref="Exception"/> object providing additional primary and/or secondary message
        /// text, or a null reference for no additional text.</param>
        /// <param name="allowCancel">
        /// <c>true</c> to display an "OK" button and a "Cancel" button at the bottom of the dialog;
        /// <c>false</c> to display an "OK" button only.</param>
        /// <returns>
        /// <c>true</c> if the user chose "OK"; <c>false</c> if <paramref name="allowCancel"/> is
        /// <c>true</c> and the user chose "Cancel".</returns>
        /// <remarks><para>
        /// <b>ShowFatalError</b> shows a <see cref="MessageBox"/> if <paramref name="e"/> is a null
        /// reference, and a <see cref="MessageDialog"/> otherwise.
        /// </para><para>
        /// The dialog caption is always set to the localized string "Fatal Error", the dialog icon
        /// to the standard <see cref="MessageBoxImage.Error"/> icon, and the <see
        /// cref="Window.Owner"/> of the dialog to a null reference.</para></remarks>

        internal static bool ShowFatalError(string message, Exception e, bool allowCancel) {
            var buttons = (allowCancel ? MessageBoxButton.OKCancel : MessageBoxButton.OK);

            if (e != null) {
                bool? result = MessageDialog.Show(null, message,
                    Strings.TitleFatalError, e, buttons, Images.Error);
                return (result == true);
            } else {
                MessageBoxResult result = MessageBox.Show(message,
                    Strings.TitleFatalError, buttons, MessageBoxImage.Error);
                return (result == MessageBoxResult.OK);
            }
        }

        #endregion
        #region ShowHelp

        /// <summary>
        /// Displays the contents of the application help file for the specified topic.</summary>
        /// <param name="topic">
        /// The topic or keyword to display help for.</param>
        /// <remarks>
        /// <b>ShowHelp</b> displays the contents of the localized HTML <see
        /// cref="FilePaths.HelpFile"/> for the specified <paramref name="topic"/>, which may be the
        /// file name of an embedded HTML page, or the table of contents if <paramref name="topic"/>
        /// is a null reference.</remarks>

        public static void ShowHelp(string topic) {
            System.Windows.Forms.Help.ShowHelp(null, FilePaths.HelpFile, topic);
        }

        #endregion
        #region StartFile

        /// <summary>
        /// Attempts to start the specified file using its associated application.</summary>
        /// <param name="owner">
        /// The parent <see cref="Window"/> of any dialog.</param>
        /// <param name="path">
        /// The path of the file to start, either absolute or relative to the <see
        /// cref="FilePaths.ApplicationFolder"/>.</param>
        /// <remarks>
        /// <b>StartFile</b> supplies the specified <paramref name="path"/> to Windows Explorer
        /// which should launch the associated application. An error message is shown on failure.
        /// </remarks>

        public static void StartFile(Window owner, string path) {
            try {
                Process.Start(Path.Combine(FilePaths.ApplicationFolder, path));
            }
            catch (Exception e) {
                string message = String.Format(ApplicationInfo.Culture,
                    Strings.DialogExplorerFileError, path);

                ShowExplorerError(owner, message, e);
            }
        }

        #endregion
    }
}
