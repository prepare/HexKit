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
using Tektosyne.Windows;

using Hexkit.Global;
using Hexkit.Graphics;
using Hexkit.Scenario;
using Hexkit.World;
using Hexkit.World.Commands;

namespace Hexkit.Game.Dialog {
    #region Type Aliases

    // entity and formatted site
    using EntityListItem = Tuple<Entity, String>;

    #endregion

    /// <summary>
    /// Shows a dialog with all entities of a specified <see cref="Faction"/>, and optionally allows
    /// renaming, placing, and destroying entities.</summary>
    /// <remarks>
    /// Please refer to the "Faction Entities" page of the "Game Dialogs" section in the application
    /// help file for details on this dialog.</remarks>

    public partial class ShowEntities: Window {
        #region ShowEntities(MapView, Faction, EntityCategory, ShowEntitiesMode)

        /// <summary>
        /// Initializes a new instance of the <see cref="ShowEntities"/> class with the specified
        /// <see cref="MapView"/>, <see cref="Faction"/>, and initially selected <see
        /// cref="EntityCategory"/>.</summary>
        /// <param name="mapView">
        /// The <see cref="MapView"/> whose <see cref="MapView.WorldState"/> contains the specified
        /// <paramref name="faction"/>.</param>
        /// <param name="faction">
        /// The <see cref="Faction"/> whose possessions to show.</param>
        /// <param name="category">
        /// An <see cref="EntityCategory"/> value indicating which entities to show initially.
        /// Invalid values default to <see cref="EntityCategory.Unit"/>.</param>
        /// <param name="mode">
        /// A <see cref="ShowEntitiesMode"/> value indicating the initial display mode for the
        /// dialog.</param>
        /// <exception cref="ArgumentException">
        /// The <see cref="WorldState.Factions"/> collection of the <see cref="MapView.WorldState"/>
        /// associated with the specified <paramref name="mapView"/> does not contain the specified
        /// <paramref name="faction"/>.</exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="mapView"/> or <paramref name="faction"/> is a null reference.
        /// </exception>

        public ShowEntities(MapView mapView, Faction faction,
            EntityCategory category, ShowEntitiesMode mode) {

            if (mapView == null)
                ThrowHelper.ThrowArgumentNullException("mapView");
            if (faction == null)
                ThrowHelper.ThrowArgumentNullException("faction");

            if (!mapView.WorldState.Factions.Contains(faction))
                ThrowHelper.ThrowArgumentException(
                    "faction", Tektosyne.Strings.ArgumentNotInCollection);

            this._mapView = mapView;
            this._worldState = mapView.WorldState;
            this._faction = faction;
            this._mode = mode;

            InitializeComponent();
            Title += faction.Name;

            // adjust column width of Entity list view
            DependencyPropertyDescriptor.FromProperty(
                ListView.ActualWidthProperty, typeof(ListView))
                .AddValueChanged(EntityList, OnEntityWidthChanged);

            // hide command buttons unless requested
            if ((mode & ShowEntitiesMode.Command) == 0)
                CommandPanel.Visibility = Visibility.Collapsed;

            // disable Site filter if no site selected
            if (mapView.SelectedSite == Site.InvalidLocation)
                SiteToggle.IsEnabled = false;

            // check Site or Placed filter if desired
            if ((this._mode & ShowEntitiesMode.Site) != 0)
                SiteToggle.IsChecked = true;
            else if ((this._mode & ShowEntitiesMode.Unplaced) != 0)
                PlacedToggle.IsChecked = null;

            // construction completed
            this._initialized = true;

            // select initially displayed category
            if (category == EntityCategory.Upgrade)
                UpgradeToggle.IsChecked = true;
            else if (category == EntityCategory.Terrain)
                TerrainToggle.IsChecked = true;
            else
                UnitToggle.IsChecked = true;
        }

        #endregion
        #region Private Fields

        // construction arguments
        private readonly MapView _mapView;
        private readonly WorldState _worldState;
        private readonly Faction _faction;
        private readonly ShowEntitiesMode _mode;

        // was construction completed?
        private readonly bool _initialized = false;

        // place targets for entity classes of current category
        private SortedList<String, IList<PointI>> _classTargets;

        #endregion
        #region PlacingEntity

        /// <summary>
        /// Gets the <see cref="Entity"/> for which to select a placement site.</summary>
        /// <value><para>
        /// The <see cref="Entity"/> for which to select a placement site.
        /// </para><para>-or-</para><para>
        /// A null reference to indicate that no <see cref="Entity"/> is awaiting placement. The
        /// default is a null reference.</para></value>
        /// <remarks>
        /// <b>PlacingEntity</b> is set to the currently selected <see cref="Entity"/> when the user
        /// clicks the "Place on Map" button. The dialog is then closed automatically. Clients
        /// should read this property to determine whether placement is requested, and should let
        /// the user select a placement site if so.</remarks>

        public Entity PlacingEntity { get; private set; }

        #endregion
        #region Private Members
        #region CurrentCategory

        /// <summary>
        /// Gets the <see cref="EntityCategory"/> that corresponds to the currently checked
        /// "Category" <see cref="RadioButton"/>.</summary>
        /// <value>
        /// The <see cref="EntityCategory"/> value that corresponds to the currently checked
        /// "Category" <see cref="RadioButton"/>. The default is <see cref="EntityCategory.Unit"/>.
        /// </value>

        private EntityCategory CurrentCategory {
            get {
                if (UnitToggle.IsChecked == true)
                    return EntityCategory.Unit;
                else if (TerrainToggle.IsChecked == true)
                    return EntityCategory.Terrain;
                else if (UpgradeToggle.IsChecked == true)
                    return EntityCategory.Upgrade;
                else {
                    Debug.Fail("ShowEntities.CurrentCategory: No Category button checked.");
                    return EntityCategory.Unit;
                }
            }
        }

        #endregion
        #region SelectedEntity

        /// <summary>
        /// Gets the <see cref="Entity"/> that corresponds to the selected item in the "Entity" <see
        /// cref="ListView"/>.</summary>
        /// <value>
        /// The <see cref="Entity"/> that corresponds to the selected item in the "Entity" <see
        /// cref="ListView"/>, if any; otherwise, a null reference.</value>

        private Entity SelectedEntity {
            get {
                int index = EntityList.SelectedIndex;
                if (index < 0) return null;
                return ((EntityListItem) EntityList.Items[index]).Item1;
            }
        }

        #endregion
        #region UpdateEntities

        /// <summary>
        /// Updates the "Entity" <see cref="ListView"/> with the current "Category" and "Filter"
        /// choices.</summary>
        /// <param name="select">
        /// <c>true</c> to select the previously selected item, if any, or else the first item in
        /// the "Entity" <see cref="ListView"/>; <c>false</c> to not select any item.</param>

        private void UpdateEntities(bool select) {

            // remember selected item, if any
            EntityListItem selectedItem = null;
            if (select) {
                int index = EntityList.SelectedIndex;
                if (index >= 0) selectedItem = (EntityListItem) EntityList.Items[index];
            }

            // clear Entity & Property list views
            EntityList.ItemsSource = null;
            PropertyList.ShowEntity(null);

            // get current general filter settings
            bool? showSite = SiteToggle.IsChecked;
            bool? showPlaced = PlacedToggle.IsChecked;

            // get category-specific filter settings
            EntityCategory category = CurrentCategory;
            bool? showCategory = null;
            switch (category) {

                case EntityCategory.Unit:
                    showCategory = MobileToggle.IsChecked;
                    break;

                case EntityCategory.Terrain:
                    showCategory = CaptureToggle.IsChecked;
                    break;
            }

            // get currently selected site
            PointI selected = this._mapView.SelectedSite;

            // get all owned faction entities for current category
            var entities = this._faction.GetEntities(category);
            var list = new List<EntityListItem>(entities.Count);

            foreach (Entity entity in entities) {

                // Site filter: skip entities on other sites
                if (showSite == true && (entity.Site == null || entity.Site.Location != selected))
                    continue;

                // Placed filter: skip placed or unplaced entities
                if ((showPlaced == true && !entity.IsPlaced) ||
                    (showPlaced == false && entity.IsPlaced))
                    continue;

                // get value that matches category-specific filter
                bool showEntity = false;
                switch (category) {

                    case EntityCategory.Unit:
                        showEntity = ((Unit) entity).IsMobile;
                        break;

                    case EntityCategory.Terrain:
                        showEntity = ((Terrain) entity).CanCapture;
                        break;
                }

                // skip entities that don't match category-specific filter
                if ((showCategory == true && !showEntity) ||
                    (showCategory == false && showEntity))
                    continue;

                // show entity name with current site
                string site = (entity.Site == null ? "—" : entity.Site.ToString());
                list.Add(new EntityListItem(entity, site));
            }

            // use ItemsSource for sorting & virtualization
            list.Sort((x, y) => StringUtility.CompareNatural(x.Item1.ToString(), y.Item1.ToString()));
            EntityList.ItemsSource = list;

            // select remembered or first item, if any
            if (select && EntityList.Items.Count > 0) {

                if (selectedItem != null)
                    EntityList.SelectAndShow(selectedItem);

                if (EntityList.SelectedIndex < 0)
                    EntityList.SelectedIndex = 0;
            }
        }

        #endregion
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
        /// cref="ShowEntities"/> dialog.</remarks>

        private void HelpExecuted(object sender, ExecutedRoutedEventArgs args) {
            args.Handled = true;
            ApplicationUtility.ShowHelp("DlgShowEntities.html");
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
        /// <b>OnCategoryChecked</b> shows or hides the "Filter" check boxes and updates the
        /// "Entity" list view to reflect the selected "Category" radio button.</remarks>

        private void OnCategoryChecked(object sender, RoutedEventArgs args) {
            args.Handled = true;
            if (!this._initialized) return;

            EntityCategory category = CurrentCategory;

            // determine place targets for all entity classes
            if ((this._mode & ShowEntitiesMode.Command) != 0)
                this._classTargets = Finder.FindAllPlaceTargets(
                    this._worldState, this._faction, category);

            // uncheck Placed filter for upgrades since they are never placed
            if (category == EntityCategory.Upgrade && PlacedToggle.IsChecked == true)
                PlacedToggle.IsChecked = null;

            // show or hide category-specific filters
            MobileToggle.Visibility = (category == EntityCategory.Unit ?
                Visibility.Visible : Visibility.Collapsed);

            CaptureToggle.Visibility = (category == EntityCategory.Terrain ?
                Visibility.Visible : Visibility.Collapsed);

            UpdateEntities(true);
        }

        #endregion
        #region OnEntityActivate

        /// <summary>
        /// Handles the <see cref="Control.MouseDoubleClick"/> event for a <see
        /// cref="ListViewItem"/> of the "Entity" <see cref="ListView"/>.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="MouseButtonEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnEntityActivate</b> displays a <see cref="ShowClasses"/> dialog containing
        /// information on the double-clicked item in the "Entity" list view.</remarks>

        private void OnEntityActivate(object sender, MouseButtonEventArgs args) {
            args.Handled = true;

            // retrieve double-clicked item, if any
            var source = args.OriginalSource as DependencyObject;
            var listItem = ItemsControl.ContainerFromElement(EntityList, source) as ListViewItem;
            if (listItem == null) return;

            // show info dialog for entity class
            EntityListItem item = (EntityListItem) listItem.Content;
            var dialog = new ShowClasses(item.Item1) { Owner = this };
            dialog.ShowDialog();
        }

        #endregion
        #region OnEntityDestroy

        /// <summary>
        /// Handles the <see cref="ButtonBase.Click"/> event for the "Destroy" <see cref="Button"/>.
        /// </summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="RoutedEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnEntityDestroy</b> asks for confirmation and then issues a <see
        /// cref="DestroyCommand"/> for the selected item in the "Entity" list view, if any.
        /// </remarks>

        private void OnEntityDestroy(object sender, RoutedEventArgs args) {
            args.Handled = true;

            // retrieve selected entity, if any
            int index = EntityList.SelectedIndex;
            if (index < 0) return;
            Entity entity = ((EntityListItem) EntityList.Items[index]).Item1;
            if (!entity.CanDestroy) return;

            // ask user to confirm destruction
            string caption = String.Format(ApplicationInfo.Culture,
                Global.Strings.TitleEntityDestroy, entity.Name);

            MessageBoxResult result = MessageBox.Show(this,
                Global.Strings.DialogEntityDestroy, caption,
                MessageBoxButton.OKCancel, MessageBoxImage.Question, MessageBoxResult.Cancel);

            if (result == MessageBoxResult.Cancel) return;

            // issue Destroy command
            AsyncAction.BeginRun(delegate {
                Session.Instance.Executor.ExecuteDestroy(
                    this._worldState, new List<Entity>(1) { entity });

                // refresh list view and re-select index
                AsyncAction.BeginInvoke(delegate {
                    UpdateEntities(false);
                    EntityList.SelectAndShow(Math.Max(index, EntityList.Items.Count - 1));
                    AsyncAction.EndRun();
                });
            });
        }

        #endregion
        #region OnEntityPlace

        /// <summary>
        /// Handles the <see cref="ButtonBase.Click"/> event for the "Place on Map" <see
        /// cref="Button"/>.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="RoutedEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnEntityPlace</b> sets the <see cref="PlacingEntity"/> property to the selected item
        /// in the "Entity" list view, and then closes the dialog with a <see
        /// cref="Window.DialogResult"/> of <c>true</c>. The caller should allow the user to select
        /// a placement site for the <see cref="PlacingEntity"/>.</remarks>

        private void OnEntityPlace(object sender, RoutedEventArgs args) {
            args.Handled = true;

            // check that selected entity can be placed
            Entity entity = SelectedEntity;
            if (entity == null || entity.IsPlaced || this._classTargets == null
                || !this._classTargets.ContainsKey(entity.EntityClass.Id))
                return;

            // communicate entity to caller
            PlacingEntity = entity;

            // close dialog automatically
            DialogResult = true;
            Close();
        }

        #endregion
        #region OnEntityRename

        /// <summary>
        /// Handles the <see cref="ButtonBase.Click"/> event for the "Rename" <see cref="Button"/>.
        /// </summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="RoutedEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnEntityRename</b> shows the <see cref="RenameEntity"/> dialog for the selected item
        /// in the "Entity" list view, if any.</remarks>

        private void OnEntityRename(object sender, RoutedEventArgs args) {
            args.Handled = true;

            // retrieve selected entity, if any
            Entity entity = SelectedEntity;
            if (entity == null) return;

            // show "Rename Entity" dialog
            var dialog = new RenameEntity(this._worldState, entity) { Owner = this };
            if (dialog.ShowDialog() != true)
                return;

            UpdateEntities(false);

            // re-select renamed entity
            for (int i = 0; i < EntityList.Items.Count; i++) {
                var item = (EntityListItem) EntityList.Items[i];

                if (item.Item1 == entity) {
                    EntityList.SelectedIndex = i;
                    EntityList.ScrollIntoView(item);
                    return;
                }
            }

            Debug.Fail("ShowEntities.OnEntityRename: Renamed entity not found");
        }

        #endregion
        #region OnEntitySelected

        /// <summary>
        /// Handles the <see cref="Selector.SelectionChanged"/> event for the "Entity" <see
        /// cref="ListView"/>.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="SelectionChangedEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnEntitySelected</b> updates all dialog controls to reflect the selected item in
        /// the "Entity" list view.</remarks>

        private void OnEntitySelected(object sender, SelectionChangedEventArgs args) {
            args.Handled = true;
            if (!this._initialized) return;

            // check if dialog shows command buttons
            bool allowCommands = ((this._mode & ShowEntitiesMode.Command) != 0);

            // show statistics for selected entity
            Entity entity = SelectedEntity;
            PropertyList.ShowEntity(entity);

            // disable all buttons if no selection
            if (entity == null) {
                SelectSiteButton.IsEnabled = false;
                if (allowCommands) {
                    RenameButton.IsEnabled = false;
                    DestroyButton.IsEnabled = false;
                    PlaceButton.IsEnabled = false;
                }
                return;
            }

            // allow selecting site of placed entity
            SelectSiteButton.IsEnabled = entity.IsPlaced;

            // quit if no command buttons available
            if (!allowCommands) return;

            // enable Rename command for all entities
            RenameButton.IsEnabled = true;

            // enable Place command for placeable entities
            PlaceButton.IsEnabled = (!entity.IsPlaced && this._classTargets != null
                && this._classTargets.ContainsKey(entity.EntityClass.Id));

            // enable Destroy command for destructible entities
            DestroyButton.IsEnabled = entity.CanDestroy;
        }

        #endregion
        #region OnEntityWidthChanged

        /// <summary>
        /// Handles changes to the <see cref="FrameworkElement.ActualWidth"/> property of the
        /// "Entity" <see cref="ListView"/>.</summary>
        /// <param name="sender">
        /// The <see cref="ListView"/> control sending the event.</param>
        /// <param name="args">
        /// An <see cref="EventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnEntityWidthChanged</b> resizes the first column of the "Entity" list view to the
        /// available list view width.</remarks>

        private void OnEntityWidthChanged(object sender, EventArgs args) {

            double width = EntityList.ActualWidth - EntitySiteColumn.ActualWidth - 28;
            if (width > 0) EntityColumn.Width = width;
        }

        #endregion
        #region OnFilterChanged

        /// <summary>
        /// Handles the <see cref="ButtonBase.Click"/> event for the "Filter" <see cref="CheckBox"/>
        /// controls.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="RoutedEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnFilterChanged</b> sets the "Placed" filter to indeterminate if it is currently
        /// unchecked and the "Site" filter is checked. Otherwise, <b>OnFilterChanged</b> updates
        /// the "Entity Class" list view to reflect the selected "Filter" options.</remarks>

        private void OnFilterChanged(object sender, RoutedEventArgs args) {
            args.Handled = true;
            if (!this._initialized) return;

            // checking Site automatically adjusts Placed filter
            if (args.Source == SiteToggle &&
                SiteToggle.IsChecked == true && PlacedToggle.IsChecked == false) {

                // changing Placed re-fires this event
                PlacedToggle.IsChecked = null;
                return;
            }
            
            UpdateEntities(true);
        }

        #endregion
        #region OnSelectSite

        /// <summary>
        /// Handles the <see cref="ButtonBase.Click"/> event for the "Select Site" <see
        /// cref="Button"/>.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="RoutedEventArgs"/> object containing event data.</param>
        /// <remarks><para>
        /// <b>OnSelectSite</b> centers and selects the <see cref="Entity.Site"/>, if any, of the
        /// selected item in the "Entity" list view on the associated map view.
        /// </para><para>
        /// If a new site was selected, <b>OnSelectSite</b> also enables and checks the "Site"
        /// filter, and updates the "Entity" list view accordingly.</para></remarks>

        private void OnSelectSite(object sender, RoutedEventArgs args) {
            args.Handled = true;

            // retrieve selected entity, if any
            Entity entity = SelectedEntity;
            if (entity == null) return;

            // center on & select site containing entity
            this._mapView.CenterAndSelect(entity.Site.Location);

            // enable & check Site filter
            SiteToggle.IsEnabled = true;
            if (SiteToggle.IsChecked == true) {

                // adjust Placed filter if unchecked
                if (PlacedToggle.IsChecked == false)
                    PlacedToggle.IsChecked = null;
                else
                    UpdateEntities(true);
            } else
                SiteToggle.IsChecked = true;
        }

        #endregion
        #endregion
    }
}
