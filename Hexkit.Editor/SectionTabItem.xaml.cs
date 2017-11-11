using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

using Tektosyne;
using Tektosyne.Collections;
using Tektosyne.IO;
using Tektosyne.Windows;

using Hexkit.Global;
using Hexkit.Scenario;

namespace Hexkit.Editor {

    /// <summary>
    /// Provides common functionality for all Hexkit Editor tab pages.</summary>
    /// <remarks><para>
    /// <b>SectionTabItem</b> provides the following features shared by all Hexkit Editor tab pages:
    /// </para><list type="bullet"><item>
    /// Controls to set the file path and toggle inlining, described on the "Section Location" page
    /// of the "Editor Display" section in the application help file.
    /// </item><item>
    /// Methods to clear, load, and save scenario section data.
    /// </item><item>
    /// A flag indicating whether the user has changed any data.
    /// </item></list><para>
    /// Any tab page content that is specific to the associated scenario section is contained within
    /// a hosted <see cref="UserControl"/> named "SectionControl". Any such content must implement
    /// the <see cref="IEditorTabContent"/> interface.</para></remarks>

    public partial class SectionTabItem: TabItem {
        #region SectionTabItem()

        /// <summary>
        /// Initializes a new instance of the <see cref="SectionTabItem"/> class.</summary>
        /// <remarks>
        /// Once the <see cref="SectionTabItem"/> control has been created, set the <see
        /// cref="SectionContent"/> property to the hosted <see cref="IEditorTabContent"/> control 
        /// before accessing any section-specific members.</remarks>

        public SectionTabItem() {
            InitializeComponent();
        }

        #endregion
        #region Private Fields

        // property backers
        private bool _dataChanged;

        // ignore Inline check box events?
        private bool _ignoreInlineEvents;

        // stored file path for scenario section
        private RootedPath _path = FilePaths.CreateCommonPath();

        #endregion
        #region DataChanged

        /// <summary>
        /// Gets or sets a value indicating whether the associated <see cref="Section"/> holds
        /// unsaved changes.</summary>
        /// <value>
        /// <c>true</c> if the <see cref="Section"/> managed by the tab page was changed since the
        /// last successful <see cref="Clear"/>, <see cref="Load"/>, or <see cref="Save"/>
        /// operation; otherweise, <c>false</c>.</value>
        /// <remarks><para>
        /// Setting <b>DataChanged</b> calls <see cref="MainWindow.OnSectionChanged"/> method and
        /// shows or hides the marker for unsaved changes, depending on the new property value.
        /// </para><para>
        /// When set to <c>true</c>, <b>DataChanged</b> also calls <see
        /// cref="MasterSection.ValidateSection"/> to re-validate the associated <see
        /// cref="Section"/>.</para></remarks>

        public bool DataChanged {
            [DebuggerStepThrough]
            get { return this._dataChanged; }
            set {
                this._dataChanged = value;

                // update menu and dependent data
                MainWindow.Instance.OnSectionChanged(Section);

                if (value) {
                    // show marker for unsaved changes
                    DataChangedLabel.Visibility = Visibility.Visible;

                    // re-validate section data
                    MasterSection.Instance.ValidateSection(Section);
                } else {
                    // hide marker for unsaved changes
                    DataChangedLabel.Visibility = Visibility.Hidden;
                }
            }
        }

        #endregion
        #region IsInlined

        /// <summary>
        /// Gets or sets a value indicating whether the associated <see cref="Section"/> should be
        /// inlined.</summary>
        /// <value>
        /// <c>true</c> if the <see cref="Section"/> managed by the tab page should be inlined in
        /// the current <see cref="MasterSection"/>; otherwise, <c>false</c>.</value>
        /// <remarks><para>
        /// <b>IsInlined</b> is meaningless on the "Master" tab page, i.e. if the associated <see
        /// cref="Section"/> equals <see cref="ScenarioSection.Master"/>.
        /// </para><para>
        /// Setting this property checks or unchecks the "Inline" check box without triggering its
        /// <see cref="ToggleButton.Checked"/> event handler.</para></remarks>

        public bool IsInlined {
            [DebuggerStepThrough]
            get { return (InlineToggle.IsChecked == true); }
            private set {
                this._ignoreInlineEvents = true;
                InlineToggle.IsChecked = value;
                this._ignoreInlineEvents = false;
            }
        }

        #endregion
        #region Path

        /// <summary>
        /// Gets or sets the file path for the associated <see cref="Section"/>.</summary>
        /// <value>
        /// The file path for the <see cref="Section"/> managed by tab page, relative to the current
        /// root folder for Hexkit user files. The default is an empty string.</value>
        /// <remarks><para>
        /// <b>Path</b> returns an empty string when set to a null reference. The value of this
        /// property persists even while <see cref="IsInlined"/> returns <c>true</c>. This allows
        /// the user to temporarily inline a subsection without losing its file path.
        /// </para><para>
        /// Setting this property also updates the "Path" text box, and automatically sets or clears
        /// <see cref="IsInlined"/> depending on whether the new <b>Path</b> is empty.
        /// </para></remarks>

        public string Path {
            get { return this._path.RelativePath; }
            private set {
                this._path = this._path.Change(value);
                OnPathChanged(this._path.RelativePath.Length == 0);
            }
        }

        #endregion
        #region Section

        /// <summary>
        /// Gets the <see cref="ScenarioSection"/> managed by the tab page.</summary>
        /// <value>
        /// The <see cref="IEditorTabContent.Section"/> value of the hosted <see
        /// cref="SectionContent"/>, indicating the Hexkit scenario section managed by the tab page.
        /// </value>

        public ScenarioSection Section {
            [DebuggerStepThrough]
            get { return SectionContent.Section; }
        }

        #endregion
        #region SectionContent

        /// <summary>
        /// Gets or sets the <see cref="IEditorTabContent"/> control hosted by the tab page.
        /// </summary>
        /// <value>
        /// The <see cref="IEditorTabContent"/> control hosted by the tab page.</value>
        /// <exception cref="ArgumentNullException">
        /// The property is set to a null reference.</exception>
        /// <exception cref="InvalidOperationException">
        /// The property is set more than once.</exception>
        /// <remarks>
        /// <b>SectionContent</b> must be set directly after the <see cref="SectionTabItem"/> has
        /// been created, and before any section-specific members are accessed.</remarks>

        public IEditorTabContent SectionContent {
            [DebuggerStepThrough]
            get { return (IEditorTabContent) SectionHost.Content; }
            set {
                if (value == null)
                    ThrowHelper.ThrowArgumentNullException("value");
                if (SectionHost.Content != null)
                    ThrowHelper.ThrowInvalidOperationException(Tektosyne.Strings.PropertySetOnce);

                SectionHost.Content = value;
                value.SectionTab = this;

                this._ignoreInlineEvents = true;

                // enable and check Inline option for all subsections
                InlineToggle.IsEnabled = (value.Section != ScenarioSection.Master);
                InlineToggle.IsChecked = (value.Section != ScenarioSection.Master);

                this._ignoreInlineEvents = false;
            }
        }

        #endregion
        #region Public Methods
        #region Clear

        /// <summary>
        /// Clears all <see cref="Section"/> data.</summary>
        /// <remarks>
        /// <b>Clear</b> sets the <see cref="Path"/> property to an empty string, invokes <see
        /// cref="MasterSection.Clear"/> on the <see cref="Section"/> managed by the tab page, and
        /// calls <see cref="Initialize"/> to re-initialize all controls with the data of the (now
        /// cleared) <see cref="Section"/>.</remarks>

        public void Clear() {
            Path = "";

            // clear scenario section data
            MasterSection.Instance.ClearSection(Section);

            // initialize tab controls
            Initialize();
        }

        #endregion
        #region Initialize

        /// <summary>
        /// Initializes the controls of the tab page.</summary>
        /// <remarks>
        /// <b>Initialize</b> calls the <see cref="IEditorTabContent.Initialize"/> method of the
        /// hosted <see cref="SectionContent"/>, sets the <see cref="DataChanged"/> flag to
        /// <c>false</c>, and calls <see cref="MasterSection.ValidateSection"/> to re-validate the
        /// associated <see cref="Section"/>.</remarks>

        public void Initialize() {
            SectionContent.Initialize();
            DataChanged = false;
            MasterSection.Instance.ValidateSection(Section);
        }

        #endregion
        #region Load()

        /// <overloads>
        /// Loads all <see cref="Section"/> data from a file.</overloads>
        /// <summary>
        /// Loads all <see cref="Section"/> data from the current <see cref="Path"/>.</summary>
        /// <remarks><para>
        /// <b>Load</b> calls <see cref="MasterSection.LoadSection"/> to load the associated <see
        /// cref="Section"/> from the current <see cref="Path"/>, unless <see cref="IsInlined"/> is
        /// <c>true</c> or <see cref="Path"/> is an empty string.
        /// </para><para>
        /// On failure, <b>Load</b> displays an error message and clears the current <b>Path</b>.
        /// </para><para>
        /// In any case, <b>Load</b> invokes <see cref="Initialize"/> before returning, in order to
        /// re-initialize the controls of the tab page from the new contents of its associated <see
        /// cref="Section"/>.</para></remarks>

        public void Load() {

            // just initialize controls for empty path
            if (IsInlined || Path.Length == 0) {
                Initialize();
                return;
            }

            // ask user to wait
            MainWindow.Instance.BeginWait(Global.Strings.StatusSectionOpening);

            try {
                // attempt to load scenario section
                MasterSection.Instance.LoadSection(Section);
                Dispatcher.DoEvents();
            }
            catch (Exception e) {
                string message = String.Format(ApplicationInfo.Culture,
                    Global.Strings.DialogSectionLoadError, Path);

                MessageDialog.Show(MainWindow.Instance, message,
                    Global.Strings.TitleSectionError, e, MessageBoxButton.OK, Images.Error);

                Path = ""; // no valid path now
            }

            Initialize();
            MainWindow.Instance.EndWait();
        }

        #endregion
        #region Load(String)

        /// <summary>
        /// Loads all <see cref="Section"/> data from the specified path.</summary>
        /// <param name="path">
        /// The file path to load the associated <see cref="Section"/> from.</param>
        /// <remarks>
        /// <b>Load</b> sets the <see cref="Path"/> property to the specified <paramref
        /// name="path"/> and calls the parameterless <see cref="Load()"/> overload.</remarks>

        public void Load(string path) {
            Path = path; Load();
        }

        #endregion
        #region ProcessAllIdentifiers

        /// <summary>
        /// Changes or deletes all occurrences of the specified identifier in the specified
        /// collection or in the entire scenario.</summary>
        /// <typeparam name="TValue">
        /// The type of all values in the specified <paramref name="collection"/>. The type of all
        /// keys is assumed to be <see cref="String"/>.</typeparam>
        /// <param name="collection">
        /// The <see cref="ICollection{T}"/> whose elements to process. This must be either an <see
        /// cref="IDictionary{TKey, TValue}"/> or an <see cref="IList{T}"/> holding <see
        /// cref="KeyValuePair{TKey, TValue}"/> elements.</param>
        /// <param name="oldId">
        /// The identifier to remove from <paramref name="collection"/> or from the current
        /// scenario.</param>
        /// <param name="newId"><para>
        /// The identifier to store with all values of <paramref name="oldId"/> in <paramref
        /// name="collection"/> or in the current scenario.
        /// </para><para>-or-</para><para>
        /// A null reference to delete all elements with <paramref name="oldId"/> from <paramref
        /// name="collection"/> or from the current scenario.</para></param>
        /// <returns>
        /// <c>true</c> if the user confirmed the change; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentException">
        /// <paramref name="collection"/> implements neither <see cref="IDictionary{TKey, TValue}"/>
        /// nor <see cref="IList{T}"/>.</exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="collection"/> is a null reference.</exception>
        /// <exception cref="ArgumentNullOrEmptyException">
        /// <paramref name="oldId"/> is a null reference or an empty string.</exception>
        /// <remarks><para>
        /// <b>ProcessAllIdentifiers</b> invokes <see
        /// cref="MasterSection.ProcessIdentifierBySection"/> to count all occurrences of the
        /// specified <paramref name="oldId"/> in the current scenario.
        /// </para><para>
        /// If any occurrences are found, <b>ProcessAllIdentifiers</b> asks the user if all of them
        /// should be deleted or changed to the specified <paramref name="newId"/>, or only those in
        /// the specified <paramref name="collection"/>, or if the entire operation should be
        /// cancelled.
        /// </para><para>
        /// If the user cancels, <b>ProcessAllIdentifiers</b> returns <c>false</c> without changing
        /// any data. Otherwise, the requested changes are performed using either
        /// <b>ProcessIdentifierBySection</b> or <see cref="CollectionsUtility.ChangeKey"/>. In the
        /// first case, the Hexkit Editor tab pages managing the changed scenario sections, if any,
        /// are also flagged as containing unsaved changes.</para></remarks>

        public static bool ProcessAllIdentifiers<TValue>(
            ICollection<KeyValuePair<String, TValue>> collection, string oldId, string newId) {

            if (collection == null)
                ThrowHelper.ThrowArgumentNullException("collection");
            if (String.IsNullOrEmpty(oldId))
                ThrowHelper.ThrowArgumentNullOrEmptyException("oldId");

            // check if required interface is available
            IDictionary<String, TValue> dictionary = collection as IDictionary<String, TValue>;
            IList<KeyValuePair<String, TValue>> list =
                collection as IList<KeyValuePair<String, TValue>>;

            if (dictionary == null && list == null)
                ThrowHelper.ThrowArgumentException("collection",
                    Tektosyne.Strings.ArgumentNotInTypes + "IDictionary, IList");

            // count all occurrences of old key
            MasterSection scenario = MasterSection.Instance;
            int[] found = scenario.ProcessIdentifierBySection(oldId, oldId);
            int totalFound = found[(int) ScenarioSection.Master];

            // ask to change those occurrences, if any
            MessageBoxResult result = MessageBoxResult.No;
            if (totalFound > 1) {

                string dialogText = (newId == null ?
                    Global.Strings.DialogIdentifierDelete :
                    Global.Strings.DialogIdentifierChange);

                result = MessageBox.Show(MainWindow.Instance,
                    String.Format(ApplicationInfo.Culture, dialogText, totalFound - 1),
                    Global.Strings.TitleIdentifierReferenced,
                    MessageBoxButton.YesNoCancel, MessageBoxImage.Question);

                // process identifiers throughout scenario
                if (result == MessageBoxResult.Yes) {
                    scenario.ProcessIdentifierBySection(oldId, newId);

                    for (int i = 0; i < found.Length; i++) {
                        if (found[i] == 0) continue;
                        ScenarioSection section = (ScenarioSection) i;
                        MainWindow.Instance.GetTabPage(section).DataChanged = true;
                    }
                }
            }

            // process identifiers in specified collection only
            if (result == MessageBoxResult.No) {
                if (dictionary != null)
                    CollectionsUtility.ProcessKey(dictionary, oldId, newId);
                else
                    CollectionsUtility.ProcessKey(list, oldId, newId);
            }

            // allow user to abort if a dialog came up
            return (result != MessageBoxResult.Cancel);
        }

        #endregion
        #region Save()

        /// <overloads>
        /// Saves all <see cref="Section"/> to a file.</overloads>
        /// <summary>
        /// Saves all <see cref="Section"/> data to the current <see cref="Path"/>.</summary>
        /// <remarks><para>
        /// <b>Save</b> calls <see cref="MasterSection.SaveSection"/> to save the contents of the
        /// associated <see cref="Section"/> to the current <see cref="Path"/>, unless <see
        /// cref="IsInlined"/> is <c>true</c> or <see cref="Path"/> is an empty string.
        /// </para><para>
        /// On success, <b>Save</b> sets the <see cref="DataChanged"/> property to <c>false</c>; on
        /// failure, an error message is shown. If the associated <see cref="Section"/> equals <see
        /// cref="ScenarioSection.Master"/>, <b>Save</b> also clears the <see cref="DataChanged"/>
        /// property of all subsections whose <see cref="IsInlined"/> flag is <c>true</c>.
        /// </para><para>
        /// If the associated <see cref="Section"/> equals <see cref="ScenarioSection.Areas"/>,
        /// <b>Save</b> invokes <see cref="AreasTabContent.Synchronize"/> on the hosted <see
        /// cref="AreasTabContent"/> control before saving. This ensures that the saved <see
        /// cref="AreaSection"/> includes any recent changes to the hosted map view.
        /// </para></remarks>

        public void Save() {
            if (IsInlined || Path.Length == 0) return;

            // ensure that Inline is unchecked
            InlineToggle.IsChecked = false;

            // synchronize map view with Areas section if necessary
            if (Section == ScenarioSection.Areas)
                ((AreasTabContent) SectionContent).Synchronize();

            // ask user to wait
            MainWindow.Instance.BeginWait(Global.Strings.StatusSectionSaving);

            try {
                // attempt to save scenario section
                MasterSection.Instance.SaveSection(Section);
                DataChanged = false;

                // clear DataChanged flag of all inlined subsections
                if (Section == ScenarioSection.Master)
                    foreach (ScenarioSection section in SectionUtility.Subsections) {
                        var sectionPage = MainWindow.Instance.GetTabPage(section);
                        if (sectionPage.IsInlined) sectionPage.DataChanged = false;
                    }
            }
            catch (Exception e) {
                string message = String.Format(ApplicationInfo.Culture,
                    Global.Strings.DialogSectionSaveError, Path);

                MessageDialog.Show(MainWindow.Instance, message,
                    Global.Strings.TitleSectionError, e, MessageBoxButton.OK, Images.Error);
            }

            MainWindow.Instance.EndWait();
        }

        #endregion
        #region Save(String)

        /// <summary>
        /// Saves all <see cref="Section"/> data to the specified path.</summary>
        /// <param name="path">
        /// The file path to save the associated <see cref="Section"/> to.</param>
        /// <remarks>
        /// <b>Save</b> sets the <see cref="Path"/> property to the specified <paramref
        /// name="path"/> and calls the parameterless <see cref="Save()"/> overload.</remarks>

        public void Save(string path) {
            Path = path; Save();
        }

        #endregion
        #endregion
        #region Event Handlers
        #region OnPathBrowse

        /// <summary>
        /// Handles the <see cref="ButtonBase.Click"/> event for the "Browse…" <see cref="Button"/>.
        /// </summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// An <see cref="EventArgs"/> or <see cref="RoutedEventArgs"/> object containing event
        /// data.</param>
        /// <remarks><para>
        /// <b>OnPathBrowse</b> automatically calls <see cref="Load"/> if the user specifies an
        /// existing file, and <see cref="Save"/> if the user specifies a non-existent file.
        /// </para><para>
        /// <b>OnPathBrowse</b> does nothing if the user cancels the dialog, or enters an empty path
        /// or the current <see cref="Path"/>.</para></remarks>

        private void OnPathBrowse(object sender, EventArgs args) {
            RoutedEventArgs routedArgs = args as RoutedEventArgs;
            if (routedArgs != null) routedArgs.Handled = true;

            // ask user to enter an existing or new file
            RootedPath path = FileDialogs.OpenSectionDialog(Section, Path, true);

            // quit if user cancelled or entered old path
            if (path == null || path.IsEmpty || path.Equals(this._path))
                return;

            // save to or load from new path
            if (!File.Exists(path.AbsolutePath))
                Save(path.AbsolutePath);
            else {
                // ask for confirmation to load file
                if (MainWindow.Instance.ClearData(Section))
                    Load(path.AbsolutePath);
            }
        }

        #endregion
        #region OnPathChanged

        /// <summary>
        /// Update data and controls to reflect the current <see cref="Path"/> and the specified
        /// value for the <see cref="IsInlined"/> property.</summary>
        /// <param name="isInlined">
        /// The new value for the <see cref="IsInlined"/> property.</param>
        /// <remarks><para>
        /// <b>OnPathChanged</b> first sets the "Path" text box to the current <see cref="Path"/>,
        /// and the <see cref="IsInlined"/> property to the specified value.
        /// </para><para>
        /// <b>OnPathChanged</b> then enables or disables the "Path" text box, and stores either an
        /// empty string or the value of the <see cref="Path"/> property as the current <see
        /// cref="Section"/> path, depending on the new <see cref="IsInlined"/> value.
        /// </para><para>
        /// <b>OnPathChanged</b> finally sets the <see cref="DataChanged"/> flag for the "Master"
        /// tab page and calls <see cref="MainWindow.OnSectionChanged"/>.</para></remarks>

        private void OnPathChanged(bool isInlined) {

            // update Path text box
            string path = Path;
            PathBox.Text = path;

            // check Inline and enable Path text box
            if (Section != ScenarioSection.Master) {
                IsInlined = isInlined;
                PathBox.IsEnabled = !isInlined;
                if (isInlined) path = "";
            }

            // store new scenario section path
            MasterSection.Instance.SectionPaths.SetPath(Section, path);

            // update dependent section data
            MainWindow.Instance.MasterTab.DataChanged = true;
            MainWindow.Instance.OnSectionChanged(Section);
        }

        #endregion
        #region OnPathInline

        /// <summary>
        /// Handles the <see cref="ButtonBase.Click"/> event for the "Inline" <see
        /// cref="CheckBox"/>.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="RoutedEventArgs"/> object containing event data.</param>
        /// <remarks><para>
        /// <b>OnPathInline</b> prompts the user to browse for a valid file path if <see
        /// cref="IsInlined"/> is <c>false</c> while <see cref="Path"/> returns an empty string, and
        /// resets <see cref="IsInlined"/> to <c>true</c> if no valid path is selected.
        /// </para><para>
        /// Otherwise, <b>OnPathInline</b> calls <see cref="OnPathChanged"/> to update all data and
        /// controls with the new <see cref="IsInlined"/> and current <see cref="Path"/> values.
        /// </para><para>
        /// <b>OnPathInline</b> does nothing if the associated <see cref="Section"/> equals <see
        /// cref="ScenarioSection.Master"/> since the Master section is never inlined.
        /// </para></remarks>

        private void OnPathInline(object sender, RoutedEventArgs args) {
            args.Handled = true;

            // Master section is never inlined
            if (this._ignoreInlineEvents || Section == ScenarioSection.Master)
                return;

            /*
             * Require valid file path if Inline is unchecked.
             * 
             * OnPathBrowse triggers OnPathChanged on success,
             * so we don't need to make this call explicitly.
             */

            if (!IsInlined && Path.Length == 0) {
                OnPathBrowse(this, EventArgs.Empty);

                // recheck Inline option if no path selected
                if (Path.Length == 0) IsInlined = true;
                return;
            }

            // broadcast data changes
            OnPathChanged(IsInlined);
        }

        #endregion
        #endregion
    }
}
