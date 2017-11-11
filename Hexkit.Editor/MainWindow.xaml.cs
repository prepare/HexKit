using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Threading;

using Tektosyne;
using Tektosyne.Geometry;
using Tektosyne.IO;
using Tektosyne.Windows;

using Hexkit.Global;
using Hexkit.Graphics;
using Hexkit.Options;
using Hexkit.Players;
using Hexkit.Scenario;
using Hexkit.World;

namespace Hexkit.Editor {

    /// <summary>
    /// Provides the top-level window for the Hexkit Editor application.</summary>
    /// <remarks>
    /// Use the <see cref="MainWindow.Instance"/> property to retrieve the <see cref="MainWindow"/>
    /// instance that is associated with the current WPF <see cref="Application"/> instance.
    /// </remarks>

    public partial class MainWindow: Window {
        #region MainWindow()

        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindow"/> class.</summary>
        /// <remarks>
        /// This constructor creates global instances of the <see cref="MasterSection"/> and <see
        /// cref="PlayerManager"/> classes before initializing the new <see cref="MainWindow"/>.
        /// </remarks>

        public MainWindow() {
            InitializeComponent();
            if (DesignerProperties.GetIsInDesignMode(this))
                return;

            // create persistent scenario instance
            MasterSection.CreateInstance();

            // create persistent player manager
            PlayerManager.CreateInstance();

            // show default title
            Title = String.Format(ApplicationInfo.Culture, "{0} - {1}",
                ApplicationInfo.Title, Global.Strings.TitleScenarioUntitled);

            // show default status bar message
            StatusMessage.DefaultText = ApplicationInfo.Signature;

            // initialize section tab pages
            MasterTab.SectionContent = new MasterTabContent();
            ImagesTab.SectionContent = new ImagesTabContent();
            VariablesTab.SectionContent = new VariablesTabContent();
            EntitiesTab.SectionContent = new EntitiesTabContent();
            FactionsTab.SectionContent = new FactionsTabContent();
            AreasTab.SectionContent = new AreasTabContent();

            // initialize scenario and tab contents
            MasterTab.Clear();

            // show options initialization message, if any
            var options = ApplicationOptions.Instance;
            options.ShowInitMessage();

            // restore saved window bounds, if any
            options.View.LoadDesktop(this);

            // handle options changes and reflect current state
            options.OptionsChanged += OnOptionsChanged;
            OnOptionsChanged(this, EventArgs.Empty);

            // prepare main menu for selection change handling
            this._menuMessages = ApplicationUtility.InitializeMenu(MainMenu, OnHighlightChanged);
        }

        #endregion
        #region Private Fields

        // property backers
        private PointI _statusPosition = Site.InvalidLocation;

        // currently highlighted menu items
        private readonly List<MenuItem> _menuItems = new List<MenuItem>();

        // help links and status bar messages for menu items
        private readonly Dictionary<String, String> _menuMessages;

        // depth of current wait stack
        private uint _waitCalls;

        #endregion
        #region Public Properties
        #region Instance

        /// <summary>
        /// Gets the current instance of the <see cref="MainWindow"/> class.</summary>
        /// <value>
        /// The instance of the <see cref="MainWindow"/> class that is associated with the current
        /// WPF <see cref="Application"/> instance.</value>
        /// <remarks>
        /// <b>MainWindow</b> returns the value of the <see cref="Application.MainWindow"/> property
        /// of the current WPF <see cref="Application"/> instance, cast to type <see
        /// cref="MainWindow"/> for convenience.</remarks>

        public static MainWindow Instance {
            [DebuggerStepThrough]
            get { return (MainWindow) Application.Current.MainWindow; }
        }

        #endregion
        #region SelectedSection

        /// <summary>
        /// Gets or sets the <see cref="ScenarioSection"/> for the currently selected tab page.
        /// </summary>
        /// <value>
        /// A <see cref="ScenarioSection"/> value indicating the scenario section managed by the
        /// currently selected Hexkit Editor tab page.</value>
        /// <remarks>
        /// Setting <b>SelectedSection</b> changes the currently selected Hexkit Editor tab page to
        /// the one for the specified <see cref="ScenarioSection"/>.</remarks>

        public ScenarioSection SelectedSection {
            [DebuggerStepThrough]
            get { return (ScenarioSection) EditorTabControl.SelectedIndex; }
            [DebuggerStepThrough]
            set { EditorTabControl.SelectedIndex = (int) value; }
        }

        #endregion
        #region SelectedTabPage

        /// <summary>
        /// Gets or sets the currently selected tab page.</summary>
        /// <value>
        /// The <see cref="SectionTabItem"/> that represents the currently selected Hexkit Editor
        /// tab page.</value>
        /// <remarks>
        /// <b>SelectedTabPage</b> returns the <see cref="Selector.SelectedItem"/> of the Hexkit
        /// Editor tab control, cast to type <see cref="SectionTabItem"/> for convenience.</remarks>

        public SectionTabItem SelectedTabPage {
            [DebuggerStepThrough]
            get { return (SectionTabItem) EditorTabControl.SelectedItem; }
            [DebuggerStepThrough]
            set { EditorTabControl.SelectedItem = value; }
        }

        #endregion
        #region StatusPosition

        /// <summary>
        /// Gets or sets the coordinates shown in the "Position" panel of the hosted <see
        /// cref="StatusBar"/>.</summary>
        /// <value>
        /// The coordinates that are shown in the "Position" panel of the hosted <see
        /// cref="StatusBar"/>. The default is <see cref="Site.InvalidLocation"/>.</value>
        /// <remarks>
        /// <b>StatusPosition</b> clears the "Position" panel if either or both coordinates are
        /// negative.</remarks>

        public PointI StatusPosition {
            [DebuggerStepThrough]
            get { return this._statusPosition; }
            set {
                this._statusPosition = value;

                if (value.X < 0 || value.Y < 0)
                    StatusPositionFormat.Text = "";
                else
                    StatusPositionFormat.Show(ApplicationInfo.Culture, value.X, value.Y);
            }
        }

        #endregion
        #endregion
        #region Public Methods
        #region AnyDataChanged

        /// <summary>
        /// Determines whether any tab page contains unsaved changes.</summary>
        /// <returns>
        /// <c>true</c> if the <see cref="SectionTabItem.DataChanged"/> property for any Hexkit
        /// Editor tab page returns <c>true</c>; otherwise, <c>false</c>.</returns>

        public bool AnyDataChanged() {
            bool changed = false;

            // query all tab pages for changes
            foreach (SectionTabItem page in EditorTabControl.Items)
                changed |= ((SectionTabItem) page).DataChanged;

            return changed;
        }

        #endregion
        #region BeginWait

        /// <summary>
        /// Shows the specified <see cref="StatusBar"/> message and enters wait mode.</summary>
        /// <param name="message">
        /// The message to show in the "Message" panel of the hosted <see cref="StatusBar"/>.
        /// </param>
        /// <remarks><para>
        /// <b>BeginWait</b> pushes the specified <paramref name="message"/> on the stack of the
        /// <see cref="StackTextBlock"/> that represents the "Message" panel, and sets the <see
        /// cref="Mouse.OverrideCursor"/> to the <see cref="Cursors.Wait"/> cursor.
        /// </para><para>
        /// Call <b>BeginWait</b> at the start of lengthy non-interactive operations, such as
        /// loading a scenario file, and call <see cref="EndWait"/> when the operation has finished.
        /// You may call <b>BeginWait</b> repeatedly, as long as there is a matching or greater
        /// number of <see cref="EndWait"/> calls.</para></remarks>

        public void BeginWait(string message) {

            // show hourglass cursor on first call
            if (++this._waitCalls == 1) {
                Mouse.OverrideCursor = Cursors.Wait;
                CommandManager.InvalidateRequerySuggested();
            }

            // show new message
            StatusMessage.Push(message);
            Dispatcher.DoEvents();
        }

        #endregion
        #region ClearData

        /// <summary>
        /// Asks for confirmation to clear the specified <see cref="ScenarioSection"/>.</summary>
        /// <param name="section">
        /// A <see cref="ScenarioSection"/> value indicating the scenario section whose data to
        /// clear.</param>
        /// <returns>
        /// <c>true</c> if the specified <paramref name="section"/> contains no unsaved changes or
        /// if the user agreed to discard such changes; otherwise, <c>false</c>.</returns>
        /// <remarks>
        /// <b>ClearData</b> asks for confirmation to lose unsaved changes in all sections, rather
        /// than just the current section, if the specified <paramref name="section"/> equals <see
        /// cref="ScenarioSection.Master"/> because clearing the Master section always clears all
        /// subsections as well.</remarks>

        public bool ClearData(ScenarioSection section) {
            MessageBoxResult result = MessageBoxResult.OK;

            if (section == ScenarioSection.Master) {
                // confirm clearing any changed sections
                if (AnyDataChanged())
                    result = MessageBox.Show(this, Global.Strings.DialogSectionUnsavedAll,
                        Global.Strings.TitleSectionUnsaved,
                        MessageBoxButton.OKCancel, MessageBoxImage.Question);
            } else {
                // confirm clearing this changed section
                if (GetTabPage(section).DataChanged)
                    result = MessageBox.Show(this, Global.Strings.DialogSectionUnsaved,
                        Global.Strings.TitleSectionUnsaved,
                        MessageBoxButton.OKCancel, MessageBoxImage.Question);
            }

            // return true if unchanged or confirmed
            return (result == MessageBoxResult.OK);
        }

        #endregion
        #region EndWait

        /// <summary>
        /// Shows the previous <see cref="StatusBar"/> message and leaves wait mode.</summary>
        /// <remarks><para>
        /// <b>EndWait</b> pops the current message from the stack of the <see
        /// cref="StackTextBlock"/> that represents the "Message" panel, clears the <see
        /// cref="Mouse.OverrideCursor"/>.
        /// </para><para>
        /// You must call <b>EndWait</b> at least once for every <see cref="BeginWait"/> call. Any
        /// additional <b>EndWait</b> calls are ignored.</para></remarks>

        public void EndWait() {

            // ignore excess calls
            if (this._waitCalls == 0) return;

            // restore previous message
            StatusMessage.Pop();

            // restore default cursor on last call
            if (--this._waitCalls == 0) {
                Mouse.OverrideCursor = null;
                CommandManager.InvalidateRequerySuggested();
            }
        }

        #endregion
        #region GetTabPage

        /// <summary>
        /// Gets the tab page for the specified <see cref="ScenarioSection"/>.</summary>
        /// <param name="section">
        /// A <see cref="ScenarioSection"/> value indicating the scenario section whose tab page to
        /// return.</param>
        /// <returns>
        /// The <see cref="SectionTabItem"/> that represents the Hexkit Editor tab page for the
        /// specified <paramref name="section"/>.</returns>
        /// <exception cref="InvalidEnumArgumentException">
        /// <paramref name="section"/> is not a valid <see cref="ScenarioSection"/> value.
        /// </exception>

        public SectionTabItem GetTabPage(ScenarioSection section) {

            int index = (int) section;
            if (index < 0 || index >= EditorTabControl.Items.Count)
                ThrowHelper.ThrowInvalidEnumArgumentException(
                    "section", index, typeof(ScenarioSection));

            return (SectionTabItem) EditorTabControl.Items[index];
        }

        #endregion
        #region OnSectionChanged

        /// <summary>
        /// Updates other tab pages to reflect data changes in the specified <see
        /// cref="ScenarioSection"/>.</summary>
        /// <param name="section">
        /// The <see cref="ScenarioSection"/> whose data has changed.</param>
        /// <remarks><para>
        /// <b>OnSectionChanged</b> marks subsections with unsaved changes on the "Master" tab page,
        /// and notifies the "Areas" tab page if any data required by the map view has changed.
        /// </para><para>
        /// Call this method whenever the user has changed any data on a Hexkit Editor tab page.
        /// </para></remarks>

        public void OnSectionChanged(ScenarioSection section) {

            // update Master tab if any subsection changed
            if (section != ScenarioSection.Master)
                ((MasterTabContent) MasterTab.SectionContent).UpdateSubsection(section);

            // update Areas tab if relevant subsection changed
            if (section == ScenarioSection.Images ||
                section == ScenarioSection.Entities ||
                section == ScenarioSection.Factions)
                ((AreasTabContent) AreasTab.SectionContent).ScenarioChanged = true;
        }

        #endregion
        #region UpdateTabPage

        /// <summary>
        /// Updates the tab page for the specified <see cref="ScenarioSection"/>, which must be a
        /// subsection, to reflect the current scenario.</summary>
        /// <param name="section">
        /// A <see cref="ScenarioSection"/> value indicating the scenario subsection whose tab page
        /// to update.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="section"/> equals <see cref="ScenarioSection.Master"/>.</exception>
        /// <remarks><para>
        /// If the <see cref="MasterSection.SectionPaths"/> element associated with the specified
        /// <paramref name="section"/> specifies a section file, <b>UpdateTabPage</b> loads the new
        /// data for the corresponding <see cref="ScenarioElement"/> object from that section file.
        /// </para><para>
        /// In any case, <b>UpdateTabPage</b> then initializes the Hexkit Editor tab page for the
        /// specified <paramref name="section"/> from the corresponding <see
        /// cref="ScenarioElement"/>.</para></remarks>

        public void UpdateTabPage(ScenarioSection section) {

            // Master section is not valid here
            if (section == ScenarioSection.Master)
                ThrowHelper.ThrowArgumentOutOfRangeExceptionWithFormat(
                    "section", section, Tektosyne.Strings.ArgumentEquals, ScenarioSection.Master);

            // retrieve file path defined for this section
            RootedPath path = MasterSection.Instance.SectionPaths.GetPath(section);

            // set associated file and initialize section tab
            GetTabPage(section).Load(path.IsEmpty ? "" : path.AbsolutePath);
        }

        #endregion
        #endregion
        #region Command Event Handlers
        #region CommandCanExecute

        /// <summary>
        /// Handles the <see cref="CommandBinding.CanExecute"/> event for any <see
        /// cref="RoutedUICommand"/> that is always available.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="CanExecuteRoutedEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>CommandCanExecute</b> sets the <see cref="CanExecuteRoutedEventArgs.CanExecute"/>
        /// property of the specified <paramref name="args"/> to <c>true</c> exactly if there are no
        /// pending <see cref="EndWait"/> calls.</remarks>

        private void CommandCanExecute(object sender, CanExecuteRoutedEventArgs args) {
            args.Handled = true;
            args.CanExecute = (this._waitCalls == 0);
        }

        #endregion
        #region FileCommandCanExecute

        /// <summary>
        /// Handles the <see cref="CommandBinding.CanExecute"/> event for any <see
        /// cref="RoutedUICommand"/> in the "File" <see cref="Menu"/> that requires a valid
        /// <see cref="SectionTabItem.Path"/>.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="CanExecuteRoutedEventArgs"/> object containing event data.</param>

        private void FileCommandCanExecute(object sender, CanExecuteRoutedEventArgs args) {
            args.Handled = true;

            ScenarioSection section = SelectedSection;
            if (section < 0)
                args.CanExecute = false;
            else {
                // enable command if file path is defined
                RootedPath path = MasterSection.Instance.SectionPaths.GetPath(section);
                args.CanExecute = !path.IsEmpty;
            }
        }

        #endregion
        #region File..Executed

        private void FileNewExecuted(object sender, ExecutedRoutedEventArgs args) {
            args.Handled = true;

            // clear active tab if user confirms
            if (ClearData(SelectedSection))
                SelectedTabPage.Clear();
        }

        private void FileOpenExecuted(object sender, ExecutedRoutedEventArgs args) {
            args.Handled = true;

            // ask user to select existing section file
            RootedPath path = FileDialogs.OpenSectionDialog(
                SelectedSection, SelectedTabPage.Path, false);

            // load into active tab if user confirms
            if (!path.IsEmpty && ClearData(SelectedSection))
                SelectedTabPage.Load(path.AbsolutePath);
        }

        private void FileRevertExecuted(object sender, ExecutedRoutedEventArgs args) {
            args.Handled = true;

            // quit if there is no associated file
            if (MasterSection.Instance.SectionPaths.GetPath(SelectedSection).IsEmpty)
                return;

            // revert to old file if user confirms
            if (ClearData(SelectedSection))
                SelectedTabPage.Load();
        }

        private void FileSaveExecuted(object sender, ExecutedRoutedEventArgs args) {
            args.Handled = true;
            SelectedTabPage.Save();
        }

        private void FileSaveAsExecuted(object sender, ExecutedRoutedEventArgs args) {
            args.Handled = true;

            // ask user to enter or select a section file
            RootedPath path = FileDialogs.SaveSectionDialog(SelectedSection, SelectedTabPage.Path);

            // save active tab to specified file
            if (!path.IsEmpty)
                SelectedTabPage.Save(path.AbsolutePath);
        }

        private void FileSaveAllExecuted(object sender, ExecutedRoutedEventArgs args) {
            args.Handled = true;
            foreach (SectionTabItem item in EditorTabControl.Items)
                item.Save();
        }

        private void FileExitExecuted(object sender, ExecutedRoutedEventArgs args) {
            args.Handled = true;
            Close();
        }

        #endregion
        #region Edit...Executed

        private void EditMasterExecuted(object sender, ExecutedRoutedEventArgs args) {
            args.Handled = true;
            EditorTabControl.SelectedIndex = (int) ScenarioSection.Master;
        }

        private void EditImagesExecuted(object sender, ExecutedRoutedEventArgs args) {
            args.Handled = true;
            EditorTabControl.SelectedIndex = (int) ScenarioSection.Images;
        }

        private void EditVariablesExecuted(object sender, ExecutedRoutedEventArgs args) {
            args.Handled = true;
            EditorTabControl.SelectedIndex = (int) ScenarioSection.Variables;
        }

        private void EditEntitiesExecuted(object sender, ExecutedRoutedEventArgs args) {
            args.Handled = true;
            EditorTabControl.SelectedIndex = (int) ScenarioSection.Entities;
        }

        private void EditFactionsExecuted(object sender, ExecutedRoutedEventArgs args) {
            args.Handled = true;
            EditorTabControl.SelectedIndex = (int) ScenarioSection.Factions;
        }

        private void EditAreasExecuted(object sender, ExecutedRoutedEventArgs args) {
            args.Handled = true;
            EditorTabControl.SelectedIndex = (int) ScenarioSection.Areas;
        }

        #endregion
        #region ViewCommandCanExecute

        /// <summary>
        /// Handles the <see cref="CommandBinding.CanExecute"/> event for any <see
        /// cref="RoutedUICommand"/> in the "View" <see cref="Menu"/> that operates on the default
        /// <see cref="AreasTabContent.MapView"/> on the "Areas" tab page.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="CanExecuteRoutedEventArgs"/> object containing event data.</param>

        private void ViewCommandCanExecute(object sender, CanExecuteRoutedEventArgs args) {
            args.Handled = true;

            // enable map view commands if Areas tab active
            args.CanExecute = (this._waitCalls == 0 && SelectedSection == ScenarioSection.Areas);

            // enable Animation only if supported by map view manager
            if (args.CanExecute && args.Command == Resources["viewAnimationCommand"]) {
                var manager = MapViewManager.Instance;
                args.CanExecute = (manager != null && manager.Animation);
            }
        }

        #endregion
        #region View...Executed (MapView)

        private void ViewAnimationExecuted(object sender, ExecutedRoutedEventArgs args) {
            args.Handled = true;
            MapView mapView = AreasTabContent.MapView;
            if (mapView != null)
                mapView.Animation = !mapView.Animation;
        }

        private void ViewShowFlagsExecuted(object sender, ExecutedRoutedEventArgs args) {
            args.Handled = true;
            MapView mapView = AreasTabContent.MapView;
            if (mapView != null)
                mapView.ShowFlags = !mapView.ShowFlags;
        }

        private void ViewShowGridExecuted(object sender, ExecutedRoutedEventArgs args) {
            args.Handled = true;
            MapView mapView = AreasTabContent.MapView;
            if (mapView != null)
                mapView.ShowGrid = !mapView.ShowGrid;
        }

        private void ViewShowOwnerExecuted(object sender, ExecutedRoutedEventArgs args) {
            args.Handled = true;
            MapView mapView = AreasTabContent.MapView;
            if (mapView != null)
                mapView.ShowOwner = !mapView.ShowOwner;
        }

        private void ViewShowGaugesExecuted(object sender, ExecutedRoutedEventArgs args) {
            args.Handled = true;
            MapView mapView = AreasTabContent.MapView;
            if (mapView == null) return;

            // display "Show Gauges" dialog
            var dialog = new ShowGauges(mapView.GaugeResource, mapView.GaugeResourceFlags);
            dialog.Owner = this;
            if (dialog.ShowDialog() == true)
                mapView.ShowGauges(dialog.Resource, dialog.ResourceFlags);
        }

        private void ViewShowVariableExecuted(object sender, ExecutedRoutedEventArgs args) {
            args.Handled = true;
            MapView mapView = AreasTabContent.MapView;
            if (mapView == null) return;

            // display "Show Variable" dialog
            var dialog = new ShowVariable(mapView.ShownVariable, mapView.ShownVariableFlags);
            dialog.Owner = this;
            if (dialog.ShowDialog() == true)
                mapView.ShowVariable(dialog.Variable, dialog.VariableFlags);
        }

        private void ViewCenterSiteExecuted(object sender, ExecutedRoutedEventArgs args) {
            args.Handled = true;
            MapView mapView = AreasTabContent.MapView;
            if (mapView != null)
                mapView.CenterOn(mapView.SelectedSite);
        }

        #endregion
        #region ViewZoom...Executed

        private void ViewZoomStdExecuted(object sender, RoutedEventArgs args) {
            args.Handled = true;
            MapView mapView = AreasTabContent.MapView;

            // set standard map scale of 100%
            if (mapView != null && mapView.Scale != 100) {
                BeginWait(Global.Strings.StatusZoomChanging);
                mapView.Scale = 100;
                EndWait();
            }
        }

        private void ViewZoomInExecuted(object sender, RoutedEventArgs args) {
            args.Handled = true;
            MapView mapView = AreasTabContent.MapView;
            if (mapView == null) return;

            // determine index of active map scale
            IList<Int32> scales = ApplicationOptions.Instance.View.MapScales;
            int index = scales.IndexOf(mapView.Scale);

            // set next higher map scale if possible
            if (index >= 0 && index < scales.Count - 1) {
                BeginWait(Global.Strings.StatusZoomChanging);
                mapView.Scale = scales[index + 1];
                EndWait();
            }
        }

        private void ViewZoomOutExecuted(object sender, RoutedEventArgs args) {
            args.Handled = true;
            MapView mapView = AreasTabContent.MapView;
            if (mapView == null) return;

            // determine index of active map scale
            IList<Int32> scales = ApplicationOptions.Instance.View.MapScales;
            int index = scales.IndexOf(mapView.Scale);

            // set next lower map scale if possible
            if (index > 0) {
                BeginWait(Global.Strings.StatusZoomChanging);
                mapView.Scale = scales[index - 1];
                EndWait();
            }
        }

        #endregion
        #region ViewTheme...Executed

        private void ViewThemeSystemExecuted(object sender, ExecutedRoutedEventArgs args) {
            args.Handled = true;
            ApplicationOptions.Instance.View.Theme = DefaultTheme.System;
        }

        private void ViewThemeClassicExecuted(object sender, ExecutedRoutedEventArgs args) {
            args.Handled = true;
            ApplicationOptions.Instance.View.Theme = DefaultTheme.Classic;
        }

        private void ViewThemeLunaExecuted(object sender, ExecutedRoutedEventArgs args) {
            args.Handled = true;
            ApplicationOptions.Instance.View.Theme = DefaultTheme.Luna;
        }

        private void ViewThemeLunaHomesteadExecuted(object sender, ExecutedRoutedEventArgs args) {
            args.Handled = true;
            ApplicationOptions.Instance.View.Theme = DefaultTheme.LunaHomestead;
        }

        private void ViewThemeLunaMetallicExecuted(object sender, ExecutedRoutedEventArgs args) {
            args.Handled = true;
            ApplicationOptions.Instance.View.Theme = DefaultTheme.LunaMetallic;
        }

        private void ViewThemeRoyaleExecuted(object sender, ExecutedRoutedEventArgs args) {
            args.Handled = true;
            ApplicationOptions.Instance.View.Theme = DefaultTheme.Royale;
        }

        private void ViewThemeAeroExecuted(object sender, ExecutedRoutedEventArgs args) {
            args.Handled = true;
            ApplicationOptions.Instance.View.Theme = DefaultTheme.Aero;
        }

        private void ViewThemeAero2Executed(object sender, ExecutedRoutedEventArgs args) {
            args.Handled = true;
            ApplicationOptions.Instance.View.Theme = DefaultTheme.Aero2;
        }

        private void ViewThemeAeroLiteExecuted(object sender, ExecutedRoutedEventArgs args) {
            args.Handled = true;
            ApplicationOptions.Instance.View.Theme = DefaultTheme.AeroLite;
        }

        #endregion
        #region View...Executed (Other)

        private void ViewBitmapGridExecuted(object sender, ExecutedRoutedEventArgs args) {
            args.Handled = true;
            ApplicationOptions.Instance.View.BitmapGrid = !ApplicationOptions.Instance.View.BitmapGrid;
            if (MapViewManager.Instance != null)
                MapViewManager.Instance.RedrawAll();
        }

        private void ViewOpaqueImagesExecuted(object sender, ExecutedRoutedEventArgs args) {
            args.Handled = true;
            ApplicationOptions.Instance.View.OpaqueImages = !ApplicationOptions.Instance.View.OpaqueImages;
            if (MapViewManager.Instance != null)
                MapViewManager.Instance.RedrawAll();
        }

        private void ViewStaticArrowsExecuted(object sender, ExecutedRoutedEventArgs args) {
            args.Handled = true;
            ApplicationOptions.Instance.View.StaticArrows = !ApplicationOptions.Instance.View.StaticArrows;
            if (MapViewManager.Instance != null)
                MapViewManager.Instance.UpdateAllArrows();
        }

        private void ViewStaticMarkerExecuted(object sender, ExecutedRoutedEventArgs args) {
            args.Handled = true;
            ApplicationOptions.Instance.View.StaticMarker = !ApplicationOptions.Instance.View.StaticMarker;
            if (MapViewManager.Instance != null)
                MapViewManager.Instance.UpdateAllMarkers();
        }

        private void ViewSaveWindowExecuted(object sender, ExecutedRoutedEventArgs args) {
            args.Handled = true;
            ApplicationOptions.Instance.View.SaveDesktop(this);
        }

        #endregion
        #region Help...Executed

        private void HelpExecuted(object sender, ExecutedRoutedEventArgs args) {
            args.Handled = true;
            string page = null;

            // show help for current menu item, if any
            int index = this._menuItems.Count - 1;
            if (index >= 0)
                page = (string) this._menuItems[index].Tag;

            ApplicationUtility.ShowHelp(page);
        }

        private void HelpTabPageExecuted(object sender, ExecutedRoutedEventArgs args) {
            args.Handled = true;
            string page = null;

            // show help for active tab page
            switch (SelectedSection) {
                case ScenarioSection.Master:    page = "EditorTabMaster.html"; break;
                case ScenarioSection.Images:    page = "EditorTabImages.html"; break;
                case ScenarioSection.Variables: page = "EditorTabVariables.html"; break;
                case ScenarioSection.Entities:  page = "EditorTabEntities.html"; break;
                case ScenarioSection.Factions:  page = "EditorTabFactions.html"; break;
                case ScenarioSection.Areas:     page = "EditorTabAreas.html"; break;
            }

            ApplicationUtility.ShowHelp(page);
        }

        private void HelpReadMeExecuted(object sender, ExecutedRoutedEventArgs args) {
            args.Handled = true;
            ApplicationUtility.StartFile(this, "ReadMe.html");
        }

        private void HelpWhatsNewExecuted(object sender, ExecutedRoutedEventArgs args) {
            args.Handled = true;
            ApplicationUtility.StartFile(this, "WhatsNew.html");
        }

        private void HelpAboutExecuted(object sender, ExecutedRoutedEventArgs args) {
            args.Handled = true;
            var dialog = new AboutDialog() { Owner = this };
            dialog.ShowDialog();
        }

        #endregion
        #endregion
        #region Event Handlers
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
        /// handles the <see cref="Window.Closing"/> event by asking the user to confirm discarding
        /// all changes if any section was changed since the last load or save operation. The event
        /// is cancelled if the user declines the question.</para></remarks>

        protected override void OnClosing(CancelEventArgs args) {
            base.OnClosing(args);
            if (args.Cancel) return;

            // ask user for confirmation if any section changed
            if (AnyDataChanged()) {
                MessageBoxResult result = MessageBox.Show(this,
                    Global.Strings.DialogSectionUnsavedAll, Global.Strings.TitleSectionUnsaved,
                    MessageBoxButton.OKCancel, MessageBoxImage.Question);

                // clean up if user confirms, else cancel
                if (result == MessageBoxResult.OK)
                    MasterSection.Instance.Dispose();
                else
                    args.Cancel = true;
            }
        }

        #endregion
        #region OnContentRendered

        /// <summary>
        /// Raises and handles the <see cref="Window.ContentRendered"/> event.</summary>
        /// <param name="args">
        /// An <see cref="EventArgs"/> object containing event data.</param>
        /// <remarks><para>
        /// <b>OnContentRendered</b> raises the <see cref="Window.ContentRendered"/> event by
        /// calling the base class implementation of <see cref="Window.OnContentRendered"/> with the
        /// specified <paramref name="args"/>.
        /// </para><para>
        /// If any command line arguments were supplied to the <see cref="Application"/>,
        /// <b>OnContentRendered</b> then handles the event by calling <see
        /// cref="FilePaths.FindArgumentFile"/> on the first command line argument.
        /// </para><para>
        /// If <see cref="FilePaths.FindArgumentFile"/> recognizes a scenario XML file, that file is
        /// opened as usual. Otherwise, an error message is shown to the user. All other command
        /// line arguments are ignored.</para></remarks>

        protected override void OnContentRendered(EventArgs args) {
            base.OnContentRendered(args);

            // return immediately if command line empty
            string[] commandLine = Environment.GetCommandLineArgs();
            if (commandLine == null || commandLine.Length < 2)
                return;

            // dialog for invalid file type
            string dialog = Global.Strings.DialogFileInvalid;

            // attempt to process first argument
            string path = commandLine[1];
            switch (FilePaths.FindArgumentFile(ref path)) {

                case ArgumentFileType.Scenario:
                    if (ClearData(ScenarioSection.Master))
                        MasterTab.Load(path);
                    break;

                case ArgumentFileType.Session:
                    dialog = Global.Strings.DialogFileGame;
                    goto default;

                case ArgumentFileType.Missing:
                    dialog = Global.Strings.DialogFileMissing;
                    goto default;

                default:
                    MessageBox.Show(this,
                        String.Format(ApplicationInfo.Culture, dialog, path),
                        Global.Strings.TitleCommandLineError,
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    break;
            }
        }

        #endregion
        #region OnDrop

        /// <summary>
        /// Raises and handles the <see cref="UIElement.Drop"/> event.</summary>
        /// <param name="args">
        /// A <see cref="DragEventArgs"/> object containing event data.</param>
        /// <remarks><para>
        /// <b>OnDrop</b> raises the <see cref="UIElement.Drop"/> event by calling the base class
        /// implementation of <see cref="UIElement.OnDrop"/> with the specified <paramref
        /// name="args"/>.
        /// </para><para>
        /// <b>OnDrop</b> then handles the <see cref="UIElement.Drop"/> event by calling <see
        /// cref="FilePaths.FindArgumentFile"/> with the first <see cref="IDataObject"/> element of
        /// format <see cref="DataFormats.FileDrop"/>, if any.
        /// </para><para>
        /// If <see cref="FilePaths.FindArgumentFile"/> recognizes a scenario XML file, that file is
        /// opened as usual. Otherwise, an error message is displayed. All other drap &amp; drop
        /// arguments are ignored.</para></remarks>

        protected override void OnDrop(DragEventArgs args) {
            base.OnDrop(args);
            args.Handled = true;

            // we accept only Windows shell file drags
            IDataObject data = args.Data;
            if (!data.GetDataPresent(DataFormats.FileDrop)) return;

            // retrieve file(s) dragged & dropped on this form
            string[] files = data.GetData(DataFormats.FileDrop) as string[];

            // sanity check for empty array
            if (files == null || files.Length < 1) return;

            // retrieve first file argument
            string path = files[0];

            // make sure this window is active
            Show(); Activate();

            // dialog for invalid file type
            string dialog = Global.Strings.DialogFileInvalid;

            // attempt to process this argument
            switch (FilePaths.FindArgumentFile(ref path)) {

                case ArgumentFileType.Scenario:
                    if (ClearData(ScenarioSection.Master))
                        MasterTab.Load(path);
                    break;

                case ArgumentFileType.Session:
                    dialog = Global.Strings.DialogFileGame;
                    goto default;

                case ArgumentFileType.Missing:
                    dialog = Global.Strings.DialogFileMissing;
                    goto default;

                default:
                    MessageBox.Show(this,
                        String.Format(ApplicationInfo.Culture, dialog, path),
                        Global.Strings.TitleDragDropError,
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    break;
            }
        }

        #endregion
        #region OnHighlightChanged

        /// <summary>
        /// Handles changes to the <see cref="MenuItem.IsHighlighted"/> property of any <see
        /// cref="MenuItem"/> in the main <see cref="Menu"/>.</summary>
        /// <param name="sender">
        /// The <see cref="MenuItem"/> control sending the event.</param>
        /// <param name="args">
        /// An <see cref="EventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnHighlightChanged</b> shows the original <see cref="FrameworkElement.ToolTip"/> text
        /// of the specified <paramref name="sender"/> as the new <see cref="StatusBar"/> message if
        /// its <see cref="MenuItem.IsHighlighted"/> flag is <c>true</c>, and otherwise restores the
        /// previous message.</remarks>

        private void OnHighlightChanged(object sender, EventArgs args) {
            MenuItem item = sender as MenuItem;
            if (item == null) return;

            // check if menu item was already highlighted
            int index = this._menuItems.IndexOf(item);

            if (index < 0 && item.IsHighlighted) {
                // add newly highlighted item to list
                if (this._menuItems.Count == 0) StatusMessage.Push();
                this._menuItems.Add(item);
            }
            else if (index >= 0 && !item.IsHighlighted) {
                // remove no longer highlighted item from list
                this._menuItems.RemoveAt(index);
                if (this._menuItems.Count == 0) StatusMessage.Pop();
            }

            // show message for most recent menu item, if any
            index = this._menuItems.Count - 1;
            if (index >= 0) {
                string tag = (string) this._menuItems[index].Tag;
                StatusMessage.Text = this._menuMessages[tag];
            }
        }

        #endregion
        #region OnKeyDown

        /// <summary>
        /// Raises and handles the <see cref="UIElement.KeyDown"/> event.</summary>
        /// <param name="args">
        /// A <see cref="KeyEventArgs"/> object containing event data.</param>
        /// <remarks><para>
        /// <b>OnKeyDown</b> raises the <see cref="UIElement.KeyDown"/> event by calling the base
        /// class implementation of <see cref="UIElement.OnKeyDown"/> with the specified <paramref
        /// name="args"/>.
        /// </para><para>
        /// <b>OnKeyDown</b> then handles the <b>KeyDown</b> event by invoking the <see
        /// cref="RoutedUICommand"/> that corresponds to the unmodified <see
        /// cref="KeyEventArgs.Key"/> of the specified <paramref name="args"/>, if any.
        /// </para></remarks>

        protected override void OnKeyDown(KeyEventArgs args) {
            base.OnKeyDown(args);

            // only handle unmodified keys
            if (Keyboard.Modifiers != ModifierKeys.None)
                return;

            // check if key invokes any command
            RoutedUICommand command = null;
            switch (args.Key) {

                case Key.C:
                    command = (RoutedUICommand) Resources["viewCenterSiteCommand"];
                    break;

                case Key.X:
                    command = (RoutedUICommand) Resources["viewZoomOutCommand"];
                    break;

                case Key.Z:
                    command = (RoutedUICommand) Resources["viewZoomInCommand"];
                    break;
            }

            // execute command if found
            if (command != null) {
                args.Handled = true;
                command.Execute(null, this);
            }
        }

        #endregion
        #region OnMapMouseDown

        /// <summary>
        /// Handles the <see cref="UIElement.MouseDown"/> event for the default <see
        /// cref="AreasTabContent.MapView"/> on the "Areas" tab page.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="MouseButtonEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnMapMouseDown</b> sets the standard zoom level on middle or wheel button clicks. Any
        /// other button clicks must be handled by the "Areas" tab page.</remarks>

        internal void OnMapMouseDown(object sender, MouseButtonEventArgs args) {
            args.Handled = true;
            if (this._waitCalls != 0) return;

            // mouse wheel button sets standard zoom
            if (args.ChangedButton == MouseButton.Middle)
                ViewZoomStdExecuted(this, args);
        }

        #endregion
        #region OnMapMouseWheel

        /// <summary>
        /// Handles the <see cref="UIElement.MouseWheel"/> event for the default <see
        /// cref="AreasTabContent.MapView"/> on the "Areas" tab page.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="MouseWheelEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnMapMouseWheel</b> zooms in on forward wheel rotation, and zooms out on backward
        /// wheel rotation. Clicking the wheel triggers <see cref="UIElement.MouseDown"/>.</remarks>

        public void OnMapMouseWheel(object sender, MouseWheelEventArgs args) {
            args.Handled = true;
            if (this._waitCalls != 0) return;

            // change zoom level if map view exists
            if (args.Delta > 0)
                ViewZoomInExecuted(this, args);
            else if (args.Delta < 0)
                ViewZoomOutExecuted(this, args);
        }

        #endregion
        #region OnOptionsChanged

        /// <summary>
        /// Handles the <see cref="ApplicationOptions.OptionsChanged"/> event for the current <see
        /// cref="ApplicationOptions"/>.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> sending the event.</param>
        /// <param name="args">
        /// An <see cref="EventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnOptionsChanged</b> updates the main <see cref="Menu"/> to reflect the current user
        /// settings.</remarks>

        private void OnOptionsChanged(object sender, EventArgs args) {

            // retrieve current settings for default map view
            ApplicationOptions options = ApplicationOptions.Instance;
            MapViewOptions mapView = options.View.LoadMapView("default");

            // check/uncheck boolean map view options
            MenuViewAnimation.IsChecked = mapView.Animation;
            MenuViewShowFlags.IsChecked = mapView.ShowFlags;
            MenuViewShowGrid.IsChecked = mapView.ShowGrid;
            MenuViewShowOwner.IsChecked = mapView.ShowOwner;

            // determine index of active display scale
            IList<Int32> scales = options.View.MapScales;
            int index = scales.IndexOf(mapView.Scale);

            // enable/disable and check/uncheck Zoom options
            MenuViewZoomStd.IsChecked = (mapView.Scale == 100);
            MenuViewZoomIn.IsEnabled = (index >= 0 && index < scales.Count - 1);
            MenuViewZoomOut.IsEnabled = (index > 0);

            // check/uncheck boolean performance options
            MenuViewBitmapGrid.IsChecked = options.View.BitmapGrid;
            MenuViewOpaqueImages.IsChecked = options.View.OpaqueImages;
            MenuViewStaticArrows.IsChecked = options.View.StaticArrows;
            MenuViewStaticMarker.IsChecked = options.View.StaticMarker;

            // check/uncheck display theme options
            DefaultTheme theme = options.View.Theme;
            MenuViewThemeSystem.IsChecked = (theme == DefaultTheme.System);
            MenuViewThemeClassic.IsChecked = (theme == DefaultTheme.Classic);
            MenuViewThemeLuna.IsChecked = (theme == DefaultTheme.Luna);
            MenuViewThemeLunaHomestead.IsChecked = (theme == DefaultTheme.LunaHomestead);
            MenuViewThemeLunaMetallic.IsChecked = (theme == DefaultTheme.LunaMetallic);
            MenuViewThemeRoyale.IsChecked = (theme == DefaultTheme.Royale);
            MenuViewThemeAero.IsChecked = (theme == DefaultTheme.Aero);
            MenuViewThemeAero2.IsChecked = (theme == DefaultTheme.Aero2);
            MenuViewThemeAeroLite.IsChecked = (theme == DefaultTheme.AeroLite);
        }

        #endregion
        #region OnTabSelected

        /// <summary>
        /// Handles the <see cref="Selector.SelectionChanged"/> event for the <see
        /// cref="TabControl"/> hosted by the main form.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="SelectionChangedEventArgs"/> object containing event data.</param>
        /// <remarks><para>
        /// <b>OnTabSelected</b> checks the "Edit" <see cref="MenuItem"/> for the current tab page.
        /// </para><para>
        /// If the "Areas" tab page has become active, <b>OnTabSelected</b> also calls its <see
        /// cref="AreasTabContent.Synchronize"/> method; otherwise, <b>OnTabSelected</b> clears the
        /// <see cref="StatusPosition"/> display.
        /// </para><para>
        /// If the "Entities" tab page has become active, <b>OnTabSelected</b> also calls its <see
        /// cref="EntitiesTabContent.UpdatePreview"/> method.</para></remarks>

        private void OnTabSelected(object sender, SelectionChangedEventArgs args) {

            // ignore unhandled events from nested controls
            if (args.OriginalSource != EditorTabControl) return;
            args.Handled = true;

            // retrieve active section
            ScenarioSection section = SelectedSection;
            if (section < 0) return;
            CommandManager.InvalidateRequerySuggested();

            // check active Edit option, uncheck all others
            var editItems = MenuEdit.Items;
            for (int i = 0; i < editItems.Count; i++)
                ((MenuItem) editItems[i]).IsChecked = (i == (int) section);

            // check if we are entering Areas tab
            bool isAreas = (section == ScenarioSection.Areas);
            MenuViewZoom.IsEnabled = isAreas;

            if (isAreas) {
                // synchronize map view on Areas tab
                ((AreasTabContent) AreasTab.SectionContent).Synchronize();
            } else {
                StatusPosition = Site.InvalidLocation;

                // update image preview on Entities tab
                if (section == ScenarioSection.Entities)
                    ((EntitiesTabContent) EntitiesTab.SectionContent).UpdatePreview();
            }
        }

        #endregion
        #endregion
    }
}
