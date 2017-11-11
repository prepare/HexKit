using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

using Tektosyne;
using Tektosyne.Geometry;

using Hexkit.Global;
using Hexkit.Graphics;
using Hexkit.Scenario;
using Hexkit.World;

namespace Hexkit.Game.Dialog {
    #region Type Aliases

    // entity class and build marker
    using ClassListItem = Tuple<EntityClass, String>;

    // site owner name, formatted and actual place site
    using SiteListItem = Tuple<String, String, PointI>;

    #endregion

    /// <summary>
    /// Shows a dialog with all placement sites for the entity classes available to a specific <see
    /// cref="Faction"/>.</summary>
    /// <remarks>
    /// Please refer to the "Placement Sites" page of the "Game Dialogs" section in the application
    /// help file for details on this dialog.</remarks>

    public partial class ShowPlacements: Window, IDisposable {
        #region ShowPlacements(EntityClass)

        /// <summary>
        /// Initializes a new instance of the <see cref="ShowPlacements"/> class with the specified
        /// <see cref="WorldState"/>, <see cref="Faction"/>, and initially selected <see
        /// cref="EntityCategory"/>.</summary>
        /// <param name="worldState">
        /// The <see cref="WorldState"/> containing the specified <paramref name="faction"/>.
        /// </param>
        /// <param name="faction">
        /// The <see cref="Faction"/> whose available entity classes to show.</param>
        /// <param name="category">
        /// An <see cref="EntityCategory"/> value indicating which entity classes to show initially.
        /// Invalid values default to <see cref="EntityCategory.Unit"/>.</param>
        /// <exception cref="ArgumentException">
        /// The <see cref="WorldState.Factions"/> collection of the specified <paramref
        /// name="worldState"/> does not contain the specified <paramref name="faction"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="worldState"/> or <paramref name="faction"/> is a null reference.
        /// </exception>

        public ShowPlacements(WorldState worldState, Faction faction, EntityCategory category) {

            if (worldState == null)
                ThrowHelper.ThrowArgumentNullException("worldState");
            if (faction == null)
                ThrowHelper.ThrowArgumentNullException("faction");

            if (!worldState.Factions.Contains(faction))
                ThrowHelper.ThrowArgumentException(
                    "faction", Tektosyne.Strings.ArgumentNotInCollection);

            this._worldState = worldState;
            this._faction = faction;
            this._category = category;

            InitializeComponent();
            Title += faction.Name;

            // create map view with default properties
            this._mapView = MapViewManager.Instance.CreateView(
                "showPlacements", worldState, MapViewHost, OnMapMouseDown, null);

            // prepare to highlight home sites
            this._mapView.SelectedRegion = Finder.MapGrid.CreateArray<Boolean>();

            // retrieve placement targets for all available entity classes
            this._unitTargets = Finder.FindAllPlaceTargets(worldState, faction, EntityCategory.Unit);
            this._terrainTargets = Finder.FindAllPlaceTargets(worldState, faction, EntityCategory.Terrain);

            // adjust column width of Entity Class list view
            DependencyPropertyDescriptor.FromProperty(
                ListView.ActualWidthProperty, typeof(ListView))
                .AddValueChanged(ClassList, OnClassWidthChanged);

            // adjust column width of Site list view
            DependencyPropertyDescriptor.FromProperty(
                ListView.ActualWidthProperty, typeof(ListView))
                .AddValueChanged(SiteList, OnSiteWidthChanged);
        }

        #endregion
        #region Private Fields

        // string used to mark ListView columns as checked
        private const string CheckMark = Global.Symbols.CheckMarkText;

        // string used to indicate that a site is unowned
        private static readonly string OwnerNone = Global.Strings.LabelOwnerNone;

        // construction arguments
        private readonly WorldState _worldState;
        private readonly Faction _faction;
        private readonly EntityCategory _category;

        // map view hosted by this dialog
        private readonly MapView _mapView;

        // was initially selected site shown?
        private bool _initialSiteShown;

        // placement targets for current & all categories
        private SortedList<String, IList<PointI>> _classTargets;
        private readonly SortedList<String, IList<PointI>> _unitTargets, _terrainTargets;

        #endregion
        #region Event Handlers
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
        /// cref="ShowPlacements"/> dialog.</remarks>

        private void HelpExecuted(object sender, ExecutedRoutedEventArgs args) {
            args.Handled = true;
            ApplicationUtility.ShowHelp("DlgShowPlacements.html");
        }

        #endregion
        #region OnCategoryChecked

        /// <summary>
        /// Handles the <see cref="ToggleButton.Checked"/> event for the "Category" <see
        /// cref="RadioButton"/> controls.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="RoutedEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnCategoryChecked</b> updates the "Entity Class" list view to reflect the selected
        /// "Category" radio button, and then selects the first item in that list view, if any.
        /// </remarks>

        private void OnCategoryChecked(object sender, RoutedEventArgs args) {
            args.Handled = true;

            // switch category (default to Unit)
            EntityCategory category = EntityCategory.Unit;
            if (UnitToggle.IsChecked == true) {
                category = EntityCategory.Unit;
                this._classTargets = this._unitTargets;
            }
            else if (TerrainToggle.IsChecked == true) {
                category = EntityCategory.Terrain;
                this._classTargets = this._terrainTargets;
            }
            else {
                Debug.Fail("ShowPlacements.OnCategoryChecked: No Category button checked.");
                category = EntityCategory.Unit;
                this._classTargets = this._unitTargets;
            }

            // get all buildable classes of current category
            var buildableClasses = this._faction.GetBuildableClasses(category);
            EntitySection entities = MasterSection.Instance.Entities;

            ClassList.Items.Clear();
            foreach (var pair in this._classTargets) {

                // get entity class for current target, if any
                EntityClass entityClass = entities.GetEntity(pair.Key);
                if (entityClass == null) continue;

                // check if specified faction can build entity class
                bool canBuild = buildableClasses.ContainsKey(entityClass.Id);
                string check = (canBuild ? CheckMark : "");

                ClassList.Items.Add(new ClassListItem(entityClass, check));
            }

            // select first entity class, if any
            if (ClassList.Items.Count > 0)
                ClassList.SelectedIndex = 0;
        }

        #endregion
        #region OnClassActivate

        /// <summary>
        /// Handles the <see cref="Control.MouseDoubleClick"/> event for a <see
        /// cref="ListViewItem"/> of the "Entity Class" <see cref="ListView"/>.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="MouseButtonEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnClassActivate</b> displays a <see cref="ShowClasses"/> dialog containing
        /// information on the double-clicked item in the "Entity Class" list view.</remarks>

        private void OnClassActivate(object sender, MouseButtonEventArgs args) {
            args.Handled = true;

            // retrieve double-clicked item, if any
            var source = args.OriginalSource as DependencyObject;
            var listItem = ItemsControl.ContainerFromElement(ClassList, source) as ListViewItem;
            if (listItem == null) return;

            // show info dialog for entity class
            ClassListItem item = (ClassListItem) listItem.Content;
            var dialog = new ShowClasses(item.Item1) { Owner = this };
            dialog.ShowDialog();
        }

        #endregion
        #region OnClassSelected

        /// <summary>
        /// Handles the <see cref="Selector.SelectionChanged"/> event for the "Entity Class" <see
        /// cref="ListView"/>.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="SelectionChangedEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnClassSelected</b> updates the "Site" list view to reflect the selected item in the
        /// "Entity Class" list view, and then selects the first item in that list view, if any.
        /// </remarks>

        private void OnClassSelected(object sender, SelectionChangedEventArgs args) {
            args.Handled = true;

            // clear Site list view & map view
            SiteList.ItemsSource = null;
            Array.Clear(this._mapView.SelectedRegion, 0, this._mapView.SelectedRegion.Length);
            this._mapView.Redraw();

            // retrieve selected entity class, if any
            int index = ClassList.SelectedIndex;
            if (index < 0) return;
            ClassListItem item = (ClassListItem) ClassList.Items[index];
            EntityClass entityClass = item.Item1;

            // retrieve placement sites, if any
            var targets = this._classTargets[entityClass.Id];
            List<SiteListItem> items = new List<SiteListItem>(targets.Count);

            // add sites to list view & selected region
            foreach (PointI target in targets) {
                this._mapView.SelectedRegion[target.X, target.Y] = true;

                // get site at specified coordinates
                Site site = this._worldState.GetSite(target);

                // get owner of this site, if any
                string owner = (site.Owner == null ?
                    ShowPlacements.OwnerNone : site.Owner.ToString());

                items.Add(new SiteListItem(site.ToString(), owner, target));
            }

            // redraw highlighted region
            this._mapView.Redraw();

            // use ItemsSource for sorting & virtualization
            items.Sort((x, y) => String.CompareOrdinal(x.Item1, y.Item1));
            SiteList.ItemsSource = items;

            // select first site, if any
            if (SiteList.Items.Count > 0)
                SiteList.SelectedIndex = 0;
        }

        #endregion
        #region OnClassWidthChanged

        /// <summary>
        /// Handles changes to the <see cref="FrameworkElement.ActualWidth"/> property of the
        /// "Entity Class" <see cref="ListView"/>.</summary>
        /// <param name="sender">
        /// The <see cref="ListView"/> control sending the event.</param>
        /// <param name="args">
        /// An <see cref="EventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnClassWidthChanged</b> resizes the first column of the "Entity Class" list view to
        /// the available list view width.</remarks>

        private void OnClassWidthChanged(object sender, EventArgs args) {

            double width = ClassList.ActualWidth - ClassBuildColumn.ActualWidth - 28;
            if (width > 0) ClassColumn.Width = width;
        }

        #endregion
        #region OnContentRendered

        /// <summary>
        /// Raises and handles the <see cref="Window.ContentRendered"/> event.</summary>
        /// <param name="args">
        /// An <see cref="EventArgs"/> object containing event data.</param>
        /// <remarks><para>
        /// <b>OnContentRendered</b> raises the <see cref="Window.ContentRendered"/> event by
        /// calling the base class implementation of <see cref="Window.OnContentRendered"/>.
        /// </para><para>
        /// <b>OnContentRendered</b> then handles the <see cref="Window.ContentRendered"/> event by
        /// checking the "Category" radio button indicated by the <see cref="EntityCategory"/> value
        /// supplied to the constructor.</para></remarks>

        protected override void OnContentRendered(EventArgs args) {
            base.OnContentRendered(args);

            // select initial catgory
            if (this._category == EntityCategory.Terrain)
                TerrainToggle.IsChecked = true;
            else
                UnitToggle.IsChecked = true;
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
        /// <b>OnMapMouseDown</b> changes the selected <see cref="Site"/> on left button clicks and
        /// selects the corresponding item in the "Site" list view if it exists.
        /// </para><para>
        /// Left clicks outside the map area deselect the currently selected <see cref="Site"/> and
        /// the selected item in the "Site" list view instead.</para></remarks>

        private void OnMapMouseDown(object sender, MouseButtonEventArgs args) {
            args.Handled = true;

            // left mouse button moves selection
            if (args.ChangedButton == MouseButton.Left) {

                // select site clicked on, if any
                PointI site = this._mapView.MouseToSite(args);
                this._mapView.SelectedSite = site;

                // clear list view selection
                SiteList.SelectedIndex = -1;
                if (site == Site.InvalidLocation) return;

                // select corresponding item in Site list view
                for (int i = 0; i < SiteList.Items.Count; i++) {
                    SiteListItem item = (SiteListItem) SiteList.Items[i];

                    if (item.Item3 == site) {
                        SiteList.SelectedIndex = i;
                        SiteList.ScrollIntoView(item);
                        break;
                    }
                }
            }
        }

        #endregion
        #region OnSiteSelected

        /// <summary>
        /// Handles the <see cref="Selector.SelectionChanged"/> event for the "Site" <see
        /// cref="ListView"/>.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="SelectionChangedEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnSiteSelected</b> sets the <see cref="MapView.SelectedSite"/> of the hosted <see
        /// cref="MapView"/> to the selected item in the "Site" list view, if any.</remarks>

        private void OnSiteSelected(object sender, SelectionChangedEventArgs args) {
            args.Handled = true;

            // retrieve selected site, if any
            int index = SiteList.SelectedIndex;
            if (index < 0) return;
            SiteListItem item = (SiteListItem) SiteList.Items[index];

            // select site in map view
            if (this._initialSiteShown)
                this._mapView.SelectedSite = item.Item3;
            else {
                this._initialSiteShown = true;
                this._mapView.CenterAndSelect(item.Item3);
            }
        }

        #endregion
        #region OnSiteWidthChanged

        /// <summary>
        /// Handles changes to the <see cref="FrameworkElement.ActualWidth"/> property of the "Site"
        /// <see cref="ListView"/>.</summary>
        /// <param name="sender">
        /// The <see cref="ListView"/> control sending the event.</param>
        /// <param name="args">
        /// An <see cref="EventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnSiteWidthChanged</b> resizes the second column of the "Site" list view to the
        /// available list view width.</remarks>

        private void OnSiteWidthChanged(object sender, EventArgs args) {

            double width = SiteList.ActualWidth - SiteLocationColumn.ActualWidth - 28;
            if (width > 0) SiteOwnerColumn.Width = width;
        }

        #endregion
        #endregion
        #region IDisposable Members

        /// <summary>
        /// Releases any resources used by the <see cref="ShowPlacements"/> dialog.</summary>
        /// <remarks>
        /// <b>Dispose</b> closes the <see cref="MapView"/> hosted by this dialog.</remarks>

        public void Dispose() {
            if (this._mapView != null)
                MapViewManager.Instance.CloseView(this._mapView.Id);
        }

        #endregion
    }
}
