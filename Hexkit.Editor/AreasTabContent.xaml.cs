using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Threading;

using Tektosyne;
using Tektosyne.Geometry;
using Tektosyne.Win32Api;
using Tektosyne.Windows;

using Hexkit.Global;
using Hexkit.Graphics;
using Hexkit.Options;
using Hexkit.Scenario;
using Hexkit.World;

using SystemInformation = System.Windows.Forms.SystemInformation;
using FloodFill = Tektosyne.Graph.FloodFill<Tektosyne.Geometry.PointI>;

namespace Hexkit.Editor {

    /// <summary>
    /// Provides the section-specific content within the "Areas" tab page of the Hexkit Editor
    /// application.</summary>
    /// <remarks>
    /// Please refer to the "Areas Page" page of the "Editor Display" section in the application
    /// help file for details on this tab page.</remarks>

    public partial class AreasTabContent: UserControl, IEditorTabContent {
        #region AreasTabContent()

        /// <summary>
        /// Initializes a new instance of the <see cref="AreasTabContent"/> class.</summary>

        public AreasTabContent() {
            InitializeComponent();
            MapViewHost.Background = SystemColors.ControlDarkBrush;

            // initialize opacity slider for overlay image
            OpacitySlider.Value = ApplicationOptions.Instance.Editor.Overlay.Opacity;
            if (ApplicationOptions.Instance.Editor.Overlay.IsEmpty)
                OpacitySlider.Visibility = Visibility.Collapsed;

            // create timer to show mouse position
            this._idleTimer = new DispatcherTimer(TimeSpan.FromMilliseconds(100),
                DispatcherPriority.ApplicationIdle, OnIdleTimer, Dispatcher);

            // construction completed
            this._initialized = true;
        }

        #endregion
        #region Private Fields

        // timer for mouse position updates
        private readonly DispatcherTimer _idleTimer;

        // was construction completed?
        private readonly bool _initialized;

        // property backers
        private SectionTabItem _sectionTab;
        private WorldState _worldState;

        #region _ignoreEvents

        /// <summary>
        /// A value greater than zero if control input events should be ignored.</summary>
        /// <remarks><para>
        /// Changing the owner of a <see cref="Site"/> sets the <see cref="WorldChanged"/> flag.
        /// This is usually undesirable when the control is changed programmatically (e.g. during
        /// <see cref="Initialize"/>).
        /// </para><para>
        /// To selectively inhibit event processing, increase the <b>_ignoreEvents</b> counter
        /// before modifying control data, then decrease the counter again. When the counter reaches
        /// zero, normal control input event processing is resumed.</para></remarks>

        private uint _ignoreEvents;

        #endregion
        #region _selection

        /// <summary>
        /// The <see cref="Area"/> for the single selected <see cref="Site"/>, or a null reference
        /// if no <see cref="Site"/> is selected.</summary>
        /// <remarks>
        /// This <see cref="Area"/> always contains a single <see cref="Area.Bounds"/> element whose
        /// location equals the <see cref="MapView"/> selection and whose size is (1,1). The other
        /// data equals the owner and entity stacks of the selected <see cref="Site"/>.</remarks>

        private Area _selection;

        #endregion
        #endregion
        #region MapView

        /// <summary>
        /// Gets the default <see cref="Graphics.MapView"/> hosted by the "Areas" tab page.
        /// </summary>
        /// <value>
        /// The default <see cref="Graphics.MapView"/> hosted by the "Areas" tab page.</value>
        /// <remarks><para>
        /// <b>MapView</b> returns the element of the <see cref="MapViewManager.MapViews"/>
        /// collection of the current <see cref="MapViewManager"/> instance whose key is "default".
        /// </para><para>
        /// <b>MapView</b> returns a null reference if the current <b>MapViewManager</b> instance is
        /// a null reference, or if the default map view has not been created yet.</para></remarks>

        public static MapView MapView {
            [DebuggerStepThrough]
            get {
                MapViewManager manager = MapViewManager.Instance;
                if (manager == null) return null;

                MapView mapView;
                manager.MapViews.TryGetValue("default", out mapView);
                return mapView;
            }
        }

        #endregion
        #region ScenarioChanged

        /// <summary>
        /// Gets or sets a value indicating whether relevant scenario data has changed.</summary>
        /// <value>
        /// <c>true</c> if any section of the current scenario has changed that is relevant to the
        /// associated <see cref="WorldState"/>; otherwise, <c>false</c>. The default is
        /// <c>false</c>.</value>
        /// <remarks><para>
        /// <b>ScenarioChanged</b> should be set to <c>true</c> whenever any section of the current
        /// scenario has changed that might require a recreation of the associated <see
        /// cref="WorldState"/> and <see cref="MapView"/>.
        /// </para><para>
        /// If <b>ScenarioChanged</b> is <c>true</c>, the associated <see cref="WorldState"/> and
        /// <see cref="MapView"/> will be regenerated by the next call to <see
        /// cref="Synchronize()"/>.</para></remarks>

        public bool ScenarioChanged { get; set; }

        #endregion
        #region WorldChanged

        /// <summary>
        /// Gets a value indicating whether the current <see cref="WorldState"/> holds user changes.
        /// </summary>
        /// <value>
        /// <c>true</c> if any data of the associated <see cref="WorldState"/> has changed compared
        /// to the current <see cref="AreaSection"/>; otherwise, <c>false</c>. The default is
        /// <c>false</c>.</value>
        /// <remarks>
        /// Please refer to <see cref="WorldState"/> for details.</remarks>

        public bool WorldChanged { get; private set; }

        #endregion
        #region WorldState

        /// <summary>
        /// Gets the <see cref="World.WorldState"/> shown on the "Areas" tab page.</summary>
        /// <value>
        /// The <see cref="World.WorldState"/> whose data is shown in the hosted <see
        /// cref="MapView"/>.</value>
        /// <remarks><para>
        /// The data of the current <see cref="AreaSection"/> is presented to the user as a map view
        /// display. However, map views are based on game world data, not directly on scenario data.
        /// </para><para>
        /// Therefore, the scenario data is translated into an equivalent <see
        /// cref="World.WorldState"/> object before creating the <see cref="MapView"/>, and the
        /// <b>WorldState</b> property allows other classes to access this object.
        /// </para><para>
        /// When the user has made changes to the current <b>WorldState</b>, its data is translated
        /// back to the underlying <b>AreaSection</b>. The <see cref="WorldChanged"/> property
        /// indicates whether such a re-translation is necessary.
        /// </para><para>
        /// <b>WorldState</b> always invokes <see cref="Synchronize()"/> to ensure that the data of
        /// the returned object is up to date. For this reason, consecutive property accesses may
        /// return different object references.</para></remarks>

        public WorldState WorldState {
            get {
                Synchronize();
                return this._worldState;
            }
        }

        #endregion
        #region Private Methods
        #region AddScenarioFactions

        /// <summary>
        /// Adds all factions in the current scenario to the "Owner" <see cref="ComboBox"/>.
        /// </summary>
        /// <remarks><para>
        /// <b>AddScenarioFactions</b> adds the <see cref="FactionClass.Id"/> strings of all <see
        /// cref="FactionClass"/> objects in the current scenario to the "Owner" combo box.
        /// </para><para>
        /// These identifiers are collected in three different locations:
        /// </para><list type="number"><item>
        /// All elements in the <see cref="FactionSection.Collection"/> of the current <see
        /// cref="FactionSection"/>.
        /// </item><item>
        /// Any <see cref="Area.Owner"/> identifiers of <see cref="Area"/> objects that were not
        /// encountered in step 1.
        /// </item><item>
        /// Any <see cref="Area.UnitOwner"/> identifiers of <b>Area</b> objects that were not
        /// encountered in steps 1 and 2.</item></list></remarks>

        private void AddScenarioFactions() {
            var items = OwnerCombo.Items;

            // add all scenario factions to combo box
            FactionSection factions = MasterSection.Instance.Factions;
            foreach (string id in factions.Collection.Keys)
                items.Add(id);

            // add any additional area owners
            AreaSection areas = MasterSection.Instance.Areas;
            foreach (Area area in areas.Collection) {
                string id = area.Owner;
                if (id.Length > 0 && !items.Contains(id))
                    items.Add(id);
            }

            // add any additional unit owners
            foreach (Area area in areas.Collection) {
                string id = area.UnitOwner;
                if (id.Length > 0 && id != area.Owner && !items.Contains(id))
                    items.Add(id);
            }
        }

        #endregion
        #region AnyBackgroundTerrain

        /// <summary>
        /// Determines whether the current scenario contains at least one <see cref="TerrainClass"/>
        /// with at least one background image.</summary>
        /// <returns>
        /// <c>true</c> if <see cref="TerrainClass.IsBackground"/> is <c>true</c> for at least one
        /// <see cref="TerrainClass"/> defined by the current <see cref="EntitySection"/>;
        /// otherwise, <c>false</c>.</returns>
        /// <remarks>
        /// <b>AnyBackgroundTerrain</b> immediately returns <c>false</c> if either the <see
        /// cref="ImageSection.Collection"/> of the current <see cref="ImageSection"/> or the <see
        /// cref="EntitySection.Terrains"/> collection of the current <see cref="EntitySection"/> is
        /// empty.</remarks>

        private static bool AnyBackgroundTerrain() {
            MasterSection scenario = MasterSection.Instance;

            // fail immediately if no images or no terrains
            if (scenario.Images.Collection.Count == 0 ||
                scenario.Entities.Terrains.Count == 0)
                return false;

            // succeed if any terrain is a background terrain
            foreach (TerrainClass terrain in scenario.Entities.Terrains.Values)
                if (terrain != null && terrain.IsBackground)
                    return true;

            // no background terrain
            return false;
        }

        #endregion
        #region ClearSelection

        /// <summary>
        /// Clears all data and GUI state based on the current <see cref="MapView"/> selection.
        /// </summary>
        /// <remarks>
        /// <b>ClearSelection</b> sets <see cref="_selection"/> to a null reference and resets all
        /// site editing controls to their default values. The <see
        /// cref="Graphics.MapView.SelectedSite"/> itself remains unchanged.</remarks>

        private void ClearSelection() {
            this._selection = null;

            // null selection requires Modify mode
            ModifyToggle.IsChecked = true;
            ReplaceToggle.IsEnabled = FillToggle.IsEnabled = false;

            // clear site owner in combo box
            OwnerCombo.SelectedIndex = 0;

            // disable controls for selected site
            SiteGroup.IsEnabled = false;
            SiteGroup.Header = Site.FormatLabel(Site.InvalidLocation);
        }

        #endregion
        #region ConfirmCustomEntities

        /// <summary>
        /// Checks if the specified map edge movements would result in the deletion of any
        /// customized entity stacks, and asks the user to confirm if so.</summary>
        /// <param name="left">
        /// The number of columns by which to move the left map edge.</param>
        /// <param name="top">
        /// The number or rows by which to move the top map edge.</param>
        /// <param name="right">
        /// The number of columns by which to move the right map edge.</param>
        /// <param name="bottom">
        /// The number of rows by which to move the bottom map edge.</param>
        /// <returns>
        /// <c>true</c> if the specified edge movements would not result in the deletion of entity
        /// stacks with customized entities, or if the user confirmed such deletion; otherwise,
        /// <c>false</c>.</returns>

        private bool ConfirmCustomEntities(int left, int top, int right, int bottom) {

            // retrieve current map dimensions
            int width = this._worldState.Sites.GetLength(0);
            int height = this._worldState.Sites.GetLength(1);

            // ignore added rows and columns
            if (left < 0) left = 0;
            if (top < 0) top = 0;
            if (right > 0) right = 0;
            if (bottom > 0) bottom = 0;

            // compute first deleted index
            right += width;
            bottom += height;

            int count = 0;

            // count custom stacks in deleted rows
            for (int x = 0; x < width; x++) {
                for (int y = 0; y < top; y++)
                    count += CountCustomEntities(x, y);
                for (int y = bottom; y < height; y++)
                    count += CountCustomEntities(x, y);
            }

            // count custom stacks in deleted columns
            for (int y = top; y < bottom; y++) {
                for (int x = 0; x < left; x++)
                    count += CountCustomEntities(x, y);
                for (int x = right; x < width; x++)
                    count += CountCustomEntities(x, y);
            }

            // no custom stacks found
            if (count == 0) return true;

            // ask user to confirm deletion
            MessageBoxResult result = MessageBox.Show(MainWindow.Instance,
                String.Format(ApplicationInfo.Culture, Global.Strings.DialogSiteDelete, count),
                Global.Strings.TitleSiteDelete, MessageBoxButton.YesNo, MessageBoxImage.Question);

            return (result == MessageBoxResult.Yes);
        }

        #endregion
        #region CreateMapView

        /// <summary>
        /// Creates the hosted <see cref="MapView"/>.</summary>
        /// <remarks>
        /// <b>CreateMapView</b> (re-)creates the associated <see cref="WorldState"/> if none exists
        /// or if the <see cref="ScenarioChanged"/> flag is set, and then (re-)creates the global
        /// <see cref="MapViewManager"/> and the hosted <see cref="MapView"/>.</remarks>

        private void CreateMapView() {

            if (ScenarioChanged || this._worldState == null) {
                ScenarioChanged = false;

                // (re)create world state from scenario
                this._worldState = new WorldState();
                this._worldState.Initialize(null);
            }

            // (re)create map view manager for current scenario
            if (MapViewManager.Instance != null)
                MapViewManager.Instance.Dispose();

            MapViewManager.CreateInstance(Dispatcher);

            // create mouse event handlers
            MouseButtonEventHandler onMouseDown = OnMapMouseDown;
            MouseWheelEventHandler onMouseWheel = MainWindow.Instance.OnMapMouseWheel;

            // create map view from world state
            MapViewManager.Instance.CreateView("default",
                this._worldState, MapViewHost, onMouseDown, onMouseWheel);
        }

        #endregion
        #region CountCustomEntities

        /// <summary>
        /// Counts the <see cref="World.WorldState.Sites"/> with customized entities at the
        /// specified coordinates.</summary>
        /// <param name="x">
        /// The x-coordinate of the <see cref="Site"/> to examine.</param>
        /// <param name="y">
        /// The y-coordinate of the <see cref="Site"/> to examine.</param>
        /// <returns>
        /// One if the <see cref="Site"/> at (<paramref name="x"/>,<paramref name="x"/>) contains
        /// any entities other than the default terrain stack; otherwise, zero.</returns>
        /// <remarks>
        /// <b>CountCustomEntities</b> returns zero if the specified <paramref name="x"/> and
        /// <paramref name="y"/> coordinates indicate an invalid map location.</remarks>

        private int CountCustomEntities(int x, int y) {

            if (x < 0 || x >= this._worldState.Sites.GetLength(0) ||
                y < 0 || y >= this._worldState.Sites.GetLength(1))
                return 0;

            Site site = this._worldState.Sites[x, y];
            return (site.HasCustomEntities() ? 1 : 0);
        }

        #endregion
        #region SetSelection

        /// <summary>
        /// Selects the specified map location in the <see cref="MapView"/>, replacing either the
        /// old or the new selection data, depending on the current editing mode.</summary>
        /// <param name="location">
        /// The coordinates of the <see cref="Site"/> to select in the <see cref="MapView"/>.
        /// </param>
        /// <param name="center">
        /// <c>true</c> to center the <see cref="MapView"/> on the specified <paramref
        /// name="location"/>; otherwise, <c>false</c>.</param>
        /// <remarks><para>
        /// <b>SetSelection</b> always sets the coordinates of the current selection to the
        /// specified <paramref name="location"/>.
        /// </para><para>
        /// In "Modify" mode, or if the previous selection was invalid, <b>SetSelection</b> also
        /// replaces all other data of the current selection with the data of the specified
        /// <paramref name="location"/>. The <see cref="Site"/> at that <paramref name="location"/>
        /// remains unchanged.
        /// </para><para>
        /// In "Replace" mode, <b>SetSelection</b> overwrites the data at the specified <paramref
        /// name="location"/> with that of the current selection, and sets the <see
        /// cref="SectionTabItem.DataChanged"/> and <see cref="WorldChanged"/> flags.
        /// </para><para>
        /// In "Fill" mode, <b>SetSelection</b> also overwrites the data of all contiguous sites
        /// that contain the same data as the specified <paramref name="location"/>.
        /// </para><para>
        /// The specified <paramref name="location"/> may be invalid, in which case the current
        /// selection is cleared and all site editing controls are reset. <b>SetSelection</b>
        /// returns <c>false</c> in this case; otherwise, <c>true</c>.</para></remarks>

        private void SetSelection(PointI location, bool center) {

            // prevent control input events
            ++this._ignoreEvents;

            // highlight site in map panel
            MapView.SelectedSite = location;

            // read back to check validity
            location = MapView.SelectedSite;

            // clear data if selection invalid
            if (location == Site.InvalidLocation) {
                ClearSelection();
                goto finished;
            }

            // center map view on selection if desired
            if (center) MapView.CenterOn(location);

            // enable controls for selected site
            SiteGroup.IsEnabled = true;
            SiteGroup.Header = Site.FormatLabel(location);

            // valid selection allows Replace and Fill modes
            ReplaceToggle.IsEnabled = FillToggle.IsEnabled = true;

            /*
             * 1. Modify mode overwrites selection with site
             * 2. Replace mode overwrites site with selection
             * 3. Fill mode overwrites area with selection
             */

            if (ModifyToggle.IsChecked == true) {
                this._selection = this._worldState.CreateArea(location);

                // show site owner in combo box
                string owner = this._selection.Owner;
                if (owner.Length == 0)
                    OwnerCombo.SelectedIndex = 0;
                else
                    OwnerCombo.SelectedItem = owner;
            }
            else if (this._selection != null && ReplaceToggle.IsChecked == true) {

                // move selection to new site and replace its data
                this._selection.Bounds[0] = new RectI(location.X, location.Y, 1, 1);
                this._worldState.CreateSite(this._selection);

                // broadcast data changes
                WorldChanged = true;
                SectionTab.DataChanged = true;
                MapView.Redraw();
            }
            else if (this._selection != null && FillToggle.IsChecked == true) {

                Cursor oldCursor = MainWindow.Instance.Cursor;
                MainWindow.Instance.Cursor = Cursors.Wait;

                // check for Area equality with specified location
                Area area = this._worldState.CreateArea(location);
                Debug.Assert(area != null);
                Predicate<PointI> match = (p => area.Equals(this._worldState.CreateArea(p)));

                // find contiguous sites with equal data
                FloodFill floodFill = new FloodFill(MapView.MapGrid);
                if (floodFill.FindMatching(match, location)) {

                    // copy selection data to all sites within fill area
                    foreach (PointI fillSite in floodFill.Nodes) {
                        this._selection.Bounds[0] = new RectI(fillSite.X, fillSite.Y, 1, 1);
                        this._worldState.CreateSite(this._selection);
                    }
                }

                // move selection to new site and replace its data
                this._selection.Bounds[0] = new RectI(location.X, location.Y, 1, 1);
                this._worldState.CreateSite(this._selection);

                // broadcast data changes
                WorldChanged = true;
                SectionTab.DataChanged = true;
                MapView.Redraw();

                MainWindow.Instance.Cursor = oldCursor;
            }

        finished:
            // allow control input events
            --this._ignoreEvents;
        }

        #endregion
        #region Synchronize

        /// <summary>
        /// Synchronizes the <see cref="AreaSection"/> with the <see cref="WorldState"/>.</summary>
        /// <param name="left">
        /// The number of columns to add or remove at the left map edge.</param>
        /// <param name="top">
        /// The number of rows to add or remove at the top map edge.</param>
        /// <param name="right">
        /// The number of columns to add or remove at the right map edge.</param>
        /// <param name="bottom">
        /// The number of rows to add or remove at the bottom map edge.</param>
        /// <remarks>
        /// The four parameters of this <b>Synchronize</b> overload are supplied to the <see
        /// cref="World.WorldState.CreateAreaSection"/> method. Please refer to the public <see
        /// cref="Synchronize()"/> overload for further details.</remarks>

        private void Synchronize(int left, int top, int right, int bottom) {

            // prevent control input events
            ++this._ignoreEvents;

            // store current scroll position and selection
            Point scroll = new Point();
            PointI select = Site.InvalidLocation;

            if (MapView != null) {
                scroll = MapView.ScrollPosition;
                select = MapView.SelectedSite;
            }

            // ask user to wait and queue actions
            MainWindow.Instance.BeginWait(Global.Strings.StatusAreasSynchronize);

            // initialize site owner combo box
            OwnerCombo.Items.Clear();
            OwnerCombo.Items.Add(Global.Strings.LabelOwnerNone);

            // clear all selection data
            ClearSelection();

            // recreate Areas section from world state
            if (WorldChanged) {
                if (this._worldState != null)
                    MasterSection.Instance.Areas =
                        this._worldState.CreateAreaSection(left, top, right, bottom);

                WorldChanged = false;
            }

            CreateMapView();

            // add all factions found in the scenario
            AddScenarioFactions();

            // restore scroll position, if any
            if (scroll != new Point())
                MapView.ScrollPosition = scroll;

            // restore map view selection, if any
            if (select != Site.InvalidLocation)
                SetSelection(select, false);

            // allow control input events
            --this._ignoreEvents;

            MainWindow.Instance.EndWait();
        }

        #endregion
        #endregion
        #region ProcessMousePosition

        /// <summary>
        /// Processes the current position of the mouse cursor.</summary>
        /// <remarks><para>
        /// <b>ProcessMousePosition</b> shows the coordinates of the <see cref="Site"/> under the
        /// mouse cursor in the "Position" field of the <see cref="StatusBar"/>.
        /// </para><para>
        /// <b>ProcessMousePosition</b> also autoscrolls the hosted <see cref="MapView"/> if the
        /// <see cref="MainWindow"/> is maximized, and the mouse cursor is at an edge of the current
        /// <see cref="SystemInformation.VirtualScreen"/>.</para></remarks>

        public static void ProcessMousePosition() {

            // quit if no map view exists
            MapView mapView = MapView;
            if (mapView == null) return;

            // autoscroll map view if cursor is on window edges
            if (MainWindow.Instance.WindowState == WindowState.Maximized) {
                PointI cursor;
                User.GetCursorPos(out cursor);
                var screen = SystemInformation.VirtualScreen;

                if (cursor.X <= 0)
                    mapView.ScrollStep(ScrollDirection.Left);
                else if (cursor.X >= screen.Width - 1)
                    mapView.ScrollStep(ScrollDirection.Right);

                if (cursor.Y <= 0)
                    mapView.ScrollStep(ScrollDirection.Up);
                else if (cursor.Y >= screen.Height - 1)
                    mapView.ScrollStep(ScrollDirection.Down);
            }

            // show site under cursor in status bar
            MainWindow.Instance.StatusPosition =
                (mapView.IsMouseOver ? mapView.MouseToSite() : Site.InvalidLocation);
        }

        #endregion
        #region Synchronize

        /// <summary>
        /// Synchronizes the <see cref="AreaSection"/> with the <see cref="WorldState"/>.</summary>
        /// <remarks><para>
        /// <b>Synchronize</b> reloads all data of the current scenario managed by other tab pages,
        /// recreates the <see cref="AreaSection"/> from the current <see cref="WorldState"/> if the
        /// <see cref="WorldChanged"/> flag is set, and then recreates the associated
        /// <b>WorldState</b> from all current scenario sections if the <see
        /// cref="ScenarioChanged"/> flag is set.
        /// </para><para>
        /// When <b>Synchronize</b> has returned, the data shown on the "Areas" tab page reflects
        /// the current scenario data, regardless of which updates took place.
        /// </para><para>
        /// <b>Synchronize</b> does nothing if neither the <b>WorldChanged</b> nor the
        /// <b>ScenarioChanged</b> flag is set, as the above condition is already true.
        /// </para></remarks>

        public void Synchronize() {
            if (WorldChanged || ScenarioChanged)
                Synchronize(0, 0, 0, 0);
        }

        #endregion
        #region Event Handlers
        #region OnChangeDefault

        /// <summary>
        /// Handles the <see cref="ButtonBase.Click"/> event for the "Change Default Contents" <see
        /// cref="Button"/>.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="RoutedEventArgs"/> object containing event data.</param>
        /// <remarks><para>
        /// <b>OnChangeDefault</b> shows an error message if the current scenario does not define
        /// any background terrains.
        /// </para><para>
        /// Otherwise, <b>OnChangeDefault</b> displays a <see cref="Dialog.ChangeSite"/> dialog for
        /// the default terrain stack of the current <see cref="AreaSection"/>, and sets the <see
        /// cref="SectionTabItem.DataChanged"/> and <see cref="ScenarioChanged"/> flags and calls
        /// <see cref="Synchronize()"/> if the user made any changes.</para></remarks>

        private void OnChangeDefault(object sender, RoutedEventArgs args) {
            args.Handled = true;

            // abort if there are no background terrains
            if (!AnyBackgroundTerrain()) {
                MessageBox.Show(MainWindow.Instance,
                    Global.Strings.DialogBackgroundNone, Global.Strings.TitleAreaInvalid,
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // adopt pending user changes
            if (WorldChanged) Synchronize();

            // show dialog and let user make changes
            var dialog = new Dialog.ChangeSite(null, null, EntityCategory.Terrain);
            dialog.Owner = MainWindow.Instance;
            dialog.ShowDialog();

            // broadcast data changes, if any
            if (dialog.DataChanged) {
                SectionTab.DataChanged = true;
                ScenarioChanged = true;
                Synchronize();
            }
        }

        #endregion
        #region OnChangeGeometry

        /// <summary>
        /// Handles the <see cref="ButtonBase.Click"/> event for the "Change Map Geometry" <see
        /// cref="Button"/>.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="RoutedEventArgs"/> object containing event data.</param>
        /// <remarks><para>
        /// <b>OnChangeGeometry</b> displays a <see cref="Dialog.ChangeGeometry"/> dialog and sets
        /// the <see cref="SectionTabItem.DataChanged"/> and <see cref="ScenarioChanged"/> flags if
        /// the user made any changes. <b>OnChangeGeometry</b> also the <see cref="WorldChanged"/>
        /// flag if the new map size differs from the that of the <see cref="AreaSection.MapGrid"/>
        /// associated with the current <see cref="AreaSection"/>.
        /// </para><para>
        /// <b>OnChangeGeometry</b> also stores the new <see cref="AreaSection.MapGrid"/>, calls
        /// <see cref="ImagesTabContent.UpdateGeometry"/> to notify the "Images" tab page of the new
        /// polygon geometry, and calls <see cref="Synchronize"/> to update the hosted <see
        /// cref="MapView"/> accordingly.</para></remarks>

        private void OnChangeGeometry(object sender, RoutedEventArgs args) {
            args.Handled = true;

            PolygonGrid newMapGrid;
            bool dataChanged = false, edgeMoved = false;
            int left = 0, top = 0, right = 0, bottom = 0;

            // show "Change Map Geometry" dialog
            var dialog = new Dialog.ChangeGeometry() { Owner = MainWindow.Instance };
            if (dialog.ShowDialog() != true) return;

            // retrieve desired structural changes
            dataChanged = dialog.DataChanged;
            newMapGrid = dialog.MapGrid;

            // retrieve desired edge movements
            edgeMoved = dialog.EdgeMoved;
            left = dialog.MoveLeft; right = dialog.MoveRight;
            top = dialog.MoveTop; bottom = dialog.MoveBottom;

            // confirm deletion of custom stacks, if any
            if (edgeMoved && !ConfirmCustomEntities(left, top, right, bottom))
                edgeMoved = false;

            if (edgeMoved || dataChanged) {
                // store new map grid
                MasterSection.Instance.Areas.MapGrid = newMapGrid;

                // broadcast data changes
                SectionTab.DataChanged = true;
                ScenarioChanged = true;

                // notify Images tab page of new geometry
                if (dataChanged) {
                    var imagesContent = MainWindow.Instance.ImagesTab.SectionContent;
                    ((ImagesTabContent) imagesContent).UpdateGeometry();
                }

                // recreate Areas section
                if (edgeMoved) WorldChanged = true;
                Synchronize(left, top, right, bottom);
            }
        }

        #endregion
        #region OnChangeOverlay

        /// <summary>
        /// Handles the <see cref="ButtonBase.Click"/> event for the "Overlay" and "Editor Overlay"
        /// <see cref="Button"/> controls.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="RoutedEventArgs"/> object containing event data.</param>
        /// <remarks><para>
        /// <b>OnChangeOverlay</b> displays a <see cref="Dialog.ChangeOverlay"/> dialog for the <see
        /// cref="OverlayImage"/> indicated by the specified <paramref name="sender"/>.
        /// </para><para>
        /// If the user changed the current <see cref="AreaSection"/>, <b>OnChangeOverlay</b> also
        /// sets the <see cref="SectionTabItem.DataChanged"/> flag.
        /// </para><para>
        /// Otherwise, if the user changed the current <see cref="EditorOptions"/>,
        /// <b>OnChangeOverlay</b> shows the "Opacity" slider exactly if there is valid <see
        /// cref="OverlayImage"/>.</para></remarks>

        private void OnChangeOverlay(object sender, RoutedEventArgs args) {
            args.Handled = true;
            bool editing = (sender == EditorOverlayButton);

            // show "Change Overlay" dialog
            var dialog = new Dialog.ChangeOverlay(editing) { Owner = MainWindow.Instance };
            if (dialog.ShowDialog() != true || !dialog.DataChanged)
                return;

            // broadcast data changes
            if (editing) {
                ApplicationOptions.Instance.Save();
                bool isEmpty = ApplicationOptions.Instance.Editor.Overlay.IsEmpty;
                OpacitySlider.Visibility = (isEmpty ? Visibility.Collapsed : Visibility.Visible);
            } else
                SectionTab.DataChanged = true;
        }

        #endregion
        #region OnChangeSite

        /// <summary>
        /// Handles the <see cref="ButtonBase.Click"/> event for the "Change Site Contents" <see
        /// cref="Button"/>.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="RoutedEventArgs"/> or <see cref="EventArgs{Boolean}"/> object containing
        /// event data.</param>
        /// <remarks><para>
        /// <b>OnChangeSite</b> shows an error message if the current scenario does not define any
        /// background terrains.
        /// </para><para>
        /// Otherwise, <b>OnChangeSite</b> displays a <see cref="Dialog.ChangeSite"/> dialog for the
        /// selected <see cref="Site"/>, and sets the <see cref="SectionTabItem.DataChanged"/> and
        /// <see cref="WorldChanged"/> flags if the user made any changes.
        /// </para><para>
        /// If <paramref name="args"/> is an <see cref="EventArgs{Boolean}"/> object whose <see
        /// cref="EventArgs{T}.Value"/> is <c>true</c>, the <see cref="Dialog.ChangeSite"/> dialog
        /// will default to the first <see cref="EntityCategory"/> whose site stack is not empty,
        /// rather than to <see cref="EntityCategory.Terrain"/>.</para></remarks>

        private void OnChangeSite(object sender, EventArgs args) {
            RoutedEventArgs routedArgs = args as RoutedEventArgs;
            if (routedArgs != null) routedArgs.Handled = true;

            // abort if no site selected
            if (this._selection == null) return;

            // abort if there are no background terrains
            if (!AnyBackgroundTerrain()) {
                MessageBox.Show(MainWindow.Instance,
                    Global.Strings.DialogBackgroundNone, Global.Strings.TitleAreaInvalid,
                    MessageBoxButton.OK, MessageBoxImage.Information);

                return;
            }

            // determine initial tab page for dialog
            EntityCategory category = EntityCategory.Terrain;
            EventArgs<Boolean> booleanArgs = args as EventArgs<Boolean>;

            // select first non-empty category, if any
            if (booleanArgs != null && booleanArgs.Value) {
                if (this._selection.Units.Count > 0)
                    category = EntityCategory.Unit;
                else if (this._selection.Effects.Count > 0)
                    category = EntityCategory.Effect;
                else
                    category = EntityCategory.Unit;
            }

            // show "Change Site Contents" dialog
            var dialog = new Dialog.ChangeSite(this._selection, OwnerCombo.Items, category);
            dialog.Owner = MainWindow.Instance;
            dialog.ShowDialog();

            // broadcast data changes, if any
            if (dialog.DataChanged) {
                this._worldState.CreateSite(this._selection);
                WorldChanged = true;
                SectionTab.DataChanged = true;
                MapView.Redraw();
            }
        }

        #endregion
        #region OnIdleTimer

        /// <summary>
        /// Handles the <see cref="DispatcherTimer.Tick"/> event for the <see
        /// cref="DispatcherPriority.ApplicationIdle"/> timer.</summary>
        /// <param name="sender">
        /// The <see cref="DispatcherTimer"/> object sending the event.</param>
        /// <param name="args">
        /// An <see cref="EventArgs"/> object containing event data.</param>
        /// <remarks><para>
        /// If the "Areas" tab page is active, <b>OnIdle</b> invokes <see
        /// cref="ProcessMousePosition"/> to display the coordinates of the <see cref="Site"/> under
        /// the mouse cursor, and to auto-scroll the default <see cref="MapView"/> if appropriate.
        /// </para><para>
        /// Otherwise, <b>OnIdle</b> does nothing.</para></remarks>

        private void OnIdleTimer(object sender, EventArgs args) {
            if (SectionTab.IsSelected)
                ProcessMousePosition();
        }

        #endregion
        #region OnMapMouseDown

        /// <summary>
        /// Handles the <see cref="UIElement.MouseDown"/> event for the hosted <see
        /// cref="MapView"/>.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="MouseButtonEventArgs"/> object containing event data.</param>
        /// <remarks><para>
        /// <b>OnMapMouseDown</b> performs the following actions, depending on the specified
        /// <paramref name="args"/>:
        /// </para><list type="bullet"><item>
        /// On <see cref="MouseButton.Left"/> clicks, change the <see
        /// cref="Graphics.MapView.SelectedSite"/> to the clicked site, if valid; otherwise, to <see
        /// cref="Site.InvalidLocation"/>.
        /// </item><item>
        /// On double <see cref="MouseButton.Left"/> clicks, display the "Terrain" tab page of a
        /// <see cref="Dialog.ChangeSite"/> dialog.
        /// </item><item>
        /// On <see cref="MouseButton.Right"/> clicks, display the "Other" tab page of a <see
        /// cref="Dialog.ChangeSite"/> dialog.
        /// </item><item>
        /// On <see cref="MouseButton.Middle"/> clicks, forward to <see
        /// cref="MainWindow.OnMapMouseDown"/>.</item></list></remarks>

        private void OnMapMouseDown(object sender, MouseButtonEventArgs args) {
            args.Handled = true;
            if (MapView == null) return;

            switch (args.ChangedButton) {
                case MouseButton.Left:
                case MouseButton.Right:

                    // select site clicked on, if any
                    PointI site = MapView.MouseToSite(args);
                    SetSelection(site, false);

                    if (args.ChangedButton == MouseButton.Right) {
                        // change non-terrain stacks on right-click
                        OnChangeSite(ChangeSiteButton, new EventArgs<Boolean>(true));
                    }
                    else if (args.ClickCount >= 2) {
                        // change terrain stack on left double-click
                        OnChangeSite(ChangeSiteButton, new EventArgs<Boolean>(false));
                    }
                    break;

                case MouseButton.Middle:
                    // let main window handle zooming
                    MainWindow.Instance.OnMapMouseDown(sender, args);
                    break;
            }
        }

        #endregion
        #region OnOpacityChanged

        /// <summary>
        /// Handles the <see cref="RangeBase.ValueChanged"/> event for the "Opacity" <see
        /// cref="Slider"/>.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="RoutedPropertyChangedEventArgs{Double}"/> object containing event data.
        /// </param>
        /// <remarks>
        /// <b>OnOpacityChanged</b> updates the current <see cref="EditorOptions"/> and the hosted
        /// <see cref="MapView"/> with the new <see cref="RangeBase.Value"/>.</remarks>

        private void OnOpacityChanged(object sender, RoutedPropertyChangedEventArgs<Double> args) {
            args.Handled = true;

            if (this._initialized) {
                ApplicationOptions.Instance.Editor.Overlay.Opacity = OpacitySlider.Value;
                ApplicationOptions.Instance.Save();
                MapView.ShowOverlay(MainWindow.Instance, true);
            }
        }

        #endregion
        #region OnOwnerSelected

        /// <summary>
        /// Handles the <see cref="Selector.SelectionChanged"/> event for the "Owner" <see
        /// cref="ComboBox"/>.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="SelectionChangedEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnOwnerSelected</b> sets the selected item in the "Owner" combo box as the new owner
        /// of the selected <see cref="Site"/>, and sets the <see cref="SectionTabItem.DataChanged"/>
        /// and <see cref="WorldChanged"/> flags.</remarks>

        private void OnOwnerSelected(object sender, SelectionChangedEventArgs args) {
            args.Handled = true;
            if (this._ignoreEvents > 0) return;

            // abort if no site selected
            if (this._selection == null) return;

            // default to no owner (index 0)
            string id = "";

            // retrieve selected owner faction, if any
            if (OwnerCombo.SelectedIndex > 0) {
                id = (string) OwnerCombo.SelectedItem;
                if (id == null) return;
            }

            /*
             * If the new Owner is different from UnitOwner, we do nothing.
             * 
             * An empty UnitOwner will keep following the new Owner, 
             * and a valid UnitOwner will remain valid regardless.
             * 
             * But if the new Owner equals UnitOwner, we must adjust UnitOwner.
             * 
             * If the new Owner is empty and any Units are present, the UnitOwner
             * can no longer keep following Owner and must be set to the old Owner.
             * 
             * If the new Owner is valid and the same as UnitOwner, we change
             * UnitOwner to follow the new Owner. Same if no Units are present.
             */

            if (id == this._selection.UnitOwner) {
                if (id.Length > 0 || this._selection.Units.Count == 0)
                    this._selection.UnitOwner = "";
                else
                    this._selection.UnitOwner = this._selection.Owner;
            }

            // set new owner for selected site
            this._selection.Owner = id;

            // broadcast data changes
            this._worldState.CreateSite(this._selection);
            WorldChanged = true;
            SectionTab.DataChanged = true;
            MapView.Redraw();
        }

        #endregion
        #endregion
        #region IEditorTabContent Members
        #region Section

        /// <summary>
        /// Gets the <see cref="ScenarioSection"/> managed by the tab page.</summary>
        /// <value>
        /// The constant value <see cref="ScenarioSection.Areas"/>, indicating the Hexkit scenario
        /// section managed by the "Areas" tab page.</value>

        public ScenarioSection Section {
            get { return ScenarioSection.Areas; }
        }

        #endregion
        #region SectionTab

        /// <summary>
        /// Gets or sets the <see cref="SectionTabItem"/> for the tab page.</summary>
        /// <value>
        /// The <see cref="SectionTabItem"/> that contains the <see cref="AreasTabContent"/>
        /// control, i.e. the "Areas" tab page of the Hexkit Editor application.</value>
        /// <exception cref="ArgumentNullException">
        /// The property is set to a null reference.</exception>
        /// <exception cref="InvalidOperationException">
        /// The property is set more than once.</exception>

        public SectionTabItem SectionTab {
            [DebuggerStepThrough]
            get { return this._sectionTab; }
            set {
                if (value == null)
                    ThrowHelper.ThrowArgumentNullException("value");
                if (this._sectionTab != null)
                    ThrowHelper.ThrowInvalidOperationException(Tektosyne.Strings.PropertySetOnce);

                this._sectionTab = value;
            }
        }

        #endregion
        #region Initialize

        /// <summary>
        /// Initializes the section-specific controls of the tab page.</summary>
        /// <remarks>
        /// <b>Initialize</b> initializes all controls that are specific to the "Areas" tab page,
        /// and also calls <see cref="ImagesTabContent.UpdateGeometry"/> to notify the "Images" tab
        /// page of the current polygon geometry.</remarks>

        public void Initialize() {

            // prevent control input events
            ++this._ignoreEvents;

            // create default map view if necessary
            if (MapView == null) CreateMapView();
            
            // update hosted controls
            WorldChanged = false;
            ScenarioChanged = true;
            Synchronize();

            // notify Images tab page of map geometry
            var imagesContent = MainWindow.Instance.ImagesTab.SectionContent;
            ((ImagesTabContent) imagesContent).UpdateGeometry();

            // allow control input events
            --this._ignoreEvents;
        }

        #endregion
        #endregion
    }
}
