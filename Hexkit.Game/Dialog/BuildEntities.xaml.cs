using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

using Tektosyne.Collections;
using Tektosyne.Geometry;
using Tektosyne.Windows;

using Hexkit.Global;
using Hexkit.Graphics;
using Hexkit.Scenario;
using Hexkit.World;
using Hexkit.World.Commands;

namespace Hexkit.Game.Dialog {
    #region Type Aliases

    using InstanceCountDictionary = SortedListEx<String, Int32>;
    using VariableValueDictionary = SortedListEx<String, Int32>;

    #endregion

    /// <summary>
    /// Shows a dialog allowing the <see cref="WorldState.ActiveFaction"/> to build new entities.
    /// </summary>
    /// <remarks>
    /// Please refer to the "Build Entities" page of the "Game Dialogs" section in the application
    /// help file for details on this dialog.</remarks>

    public partial class BuildEntities: Window {
        #region BuildEntities(MapView, Faction, EntityCategory, BuildEntitiesMode)

        /// <summary>
        /// Initializes a new instance of the <see cref="BuildEntities"/> class.</summary>
        /// <param name="category">
        /// An <see cref="EntityCategory"/> value indicating which entity classes to show initially.
        /// Invalid values default to <see cref="EntityCategory.Unit"/>.</param>
        /// <remarks>
        /// <b>BuildEntities</b> may override the specified <paramref name="category"/>. If any <see
        /// cref="EntityClass"/> allows placement on a valid <see cref="MapView.SelectedSite"/>,
        /// <b>BuildEntities</b> selects the category of that <see cref="EntityClass"/> and checks
        /// the "Site" filter.</remarks>

        public BuildEntities(EntityCategory category) {
            this._category = category;

            this._worldState = Session.Instance.WorldState;
            this._faction = this._worldState.ActiveFaction;
            this._selectedSite = Session.MapView.SelectedSite;

            InitializeComponent();
            Title += this._faction.Name;

            // adjust column width of Entity Class list view
            DependencyPropertyDescriptor.FromProperty(
                ListView.ActualWidthProperty, typeof(ListView))
                .AddValueChanged(ClassList, OnClassWidthChanged);

            // adjust column width of Resource list view
            DependencyPropertyDescriptor.FromProperty(
                ListView.ActualWidthProperty, typeof(ListView))
                .AddValueChanged(ResourceList, OnResourceWidthChanged);

            // add all faction resources to Resource list view
            foreach (Variable resource in this._faction.Resources.Variables)
                ResourceList.Items.Add(new BuildListItem((ResourceClass) resource.VariableClass));

            // disable Site filter if no site selected
            if (this._selectedSite == Site.InvalidLocation)
                SiteToggle.IsEnabled = false;
            else {
                var categories = new EntityCategory[] {
                    EntityCategory.Unit, EntityCategory.Terrain, EntityCategory.Upgrade };

                // search for selected site among all place targets
                foreach (EntityCategory searchCategory in categories) {
                    var classTargets = Finder.FindAllPlaceTargets(
                        this._worldState, this._faction, searchCategory);

                    foreach (IList<PointI> targets in classTargets.Values)
                        if (targets.Contains(this._selectedSite)) {

                            // check Site filter and select category
                            SiteToggle.IsChecked = true;
                            category = searchCategory;
                            goto selectCategory;
                        }
                }
            }

        selectCategory:
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
        private readonly EntityCategory _category;

        // current game world data
        private readonly WorldState _worldState;
        private readonly Faction _faction;
        private readonly PointI _selectedSite;

        // was construction completed?
        private readonly bool _initialized = false;

        // place targets for entity classes of current category
        private SortedList<String, IList<PointI>> _classTargets;

        #endregion
        #region BuildingClass

        /// <summary>
        /// Gets the <see cref="EntityClass"/> to build and for which to select a placement site.
        /// </summary>
        /// <value><para>
        /// The <see cref="EntityClass"/> to build and for which to select a placement site.
        /// </para><para>-or-</para><para>
        /// A null reference to indicate that no <see cref="EntityClass"/> is awaiting building and
        /// placement. The default is a null reference.</para></value>
        /// <remarks>
        /// <b>BuildingClass</b> is set to the selected item in the "Entity Class" list view when
        /// the user clicks the "Build &amp; Place" button. The dialog is then closed automatically.
        /// Clients should read this property to determine whether placement is requested, and
        /// should let the user select a placement site if so.</remarks>

        public EntityClass BuildingClass { get; private set; }

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
                    Debug.Fail("BuildEntities.CurrentCategory: No Category button checked.");
                    return EntityCategory.Unit;
                }
            }
        }

        #endregion
        #region ShowClassCounts

        /// <summary>
        /// Shows the number of current and buildable <see cref="Entity"/> objects based on each
        /// <see cref="EntityClass"/> in the "Entity Class" <see cref="ListView"/>.</summary>
        /// <remarks><para>
        /// <b>ShowClassCounts</b> updates the "Entity Class" list view based on the entities and
        /// resources of the <see cref="WorldState.ActiveFaction"/>.
        /// </para><para>
        /// For each <see cref="EntityClass"/> row, the "Current" column shows the matching number
        /// of entities owned by the faction, and the "Build" column shows the value returned by
        /// <see cref="Faction.GetBuildCount"/> for the current <see cref="EntityClass"/>.
        /// </para></remarks>

        private void ShowClassCounts() {

            Faction faction = this._faction;
            WorldState world = this._worldState;
            var entities = faction.GetEntities(this._category);

            int count;
            var instanceCounts = new InstanceCountDictionary();

            // count number of instances of each entity class
            foreach (Entity entity in entities) {
                string id = entity.EntityClass.Id;
                instanceCounts.TryGetValue(id, out count);
                instanceCounts[id] = count + 1;
            }

            foreach (BuildListItem item in ClassList.Items) {

                // show number of owned entities in "Current" column
                instanceCounts.TryGetValue(item.EntityClass.Id, out count);
                item.CurrentCount = count;

                // show number of buildable entities in "Build" column
                item.BuildCount = faction.GetBuildCount(world, item.EntityClass);
            }

            ClassList.Items.Refresh();
        }

        #endregion
        #region ShowResources

        /// <summary>
        /// Shows the current faction resources and those required to build the specified <see
        /// cref="EntityClass"/> in the "Resource" <see cref="ListView"/>.</summary>
        /// <param name="entityClass">
        /// The <see cref="EntityClass"/> whose resource counts to show. This argument may be a null
        /// reference.</param>
        /// <remarks><para>
        /// <b>ShowResources</b> updates the "Resource" list view with the current resources of the
        /// <see cref="WorldState.ActiveFaction"/>, and those required to build the specified
        /// <paramref name="entityClass"/>, if valid.
        /// </para><para>
        /// For each resource row, the "Current" column shows the matching value in the faction's
        /// <see cref="Faction.Resources"/> collection, and the "Build" column shows the matching
        /// value, if any, in the collection returned by <see cref="Faction.GetBuildResources"/> for
        /// the specified <paramref name="entityClass"/>.</para></remarks>

        private void ShowResources(EntityClass entityClass) {

            // retrieve required resources for entity class
            Faction faction = this._faction;
            VariableValueDictionary buildResources = (entityClass == null ? null :
                faction.GetBuildResources(this._worldState, entityClass));

            foreach (BuildListItem item in ResourceList.Items) {

                // show available resources in "Current" column
                Variable current = faction.Resources[item.ResourceClass.Id];
                if (current == null) {
                    item.CurrentCount = 0;
                    item.CurrentText = "—"; // em dash
                } else {
                    item.CurrentCount = current.Value;
                    item.CurrentText = current.ToString();
                }

                // show required resources in "Build" column
                int index = (buildResources == null ? -1 :
                    buildResources.IndexOfKey(item.ResourceClass.Id));

                if (index < 0) {
                    item.BuildCount = 0;
                    item.BuildText = "—"; // em dash
                } else {
                    int required = buildResources.GetByIndex(index);
                    item.BuildCount = required;
                    item.BuildText = item.ResourceClass.Format(required, false);
                }
            }

            ResourceList.Items.Refresh();
        }

        #endregion
        #region UpdateClasses

        /// <summary>
        /// Updates the "Entity Class" <see cref="ListView"/> with the current "Category" and
        /// "Filter" choices.</summary>
        /// <param name="select">
        /// <c>true</c> to select the previously selected item, if any, or else the first item in
        /// the "Entity Class" <see cref="ListView"/>; <c>false</c> to not select any item.</param>

        private void UpdateClasses(bool select) {

            // remember selected item, if any
            BuildListItem selectedItem = null;
            if (select) {
                int index = ClassList.SelectedIndex;
                if (index >= 0) selectedItem = (BuildListItem) ClassList.Items[index];
            }

            // clear Entity Class list view
            ClassList.Items.Clear();

            // get all buildable classes for current category
            var classes = this._faction.GetBuildableClasses(CurrentCategory);

            // quit if no buildable classes
            if (classes == null || classes.Count == 0)
                return;

            // get current filter settings
            bool? showSite = SiteToggle.IsChecked;
            bool? showPlace = PlaceToggle.IsChecked;

            foreach (EntityClass entityClass in classes.Values) {

                // get all place targets for entity class
                IList<PointI> targets;
                this._classTargets.TryGetValue(entityClass.Id, out targets);

                // Site filter: skip classes without this place target
                if (showSite == true &&
                    (targets == null || !targets.Contains(this._selectedSite)))
                    continue;

                // Can Place filter: skip classes with or without place targets
                if ((showPlace == true && targets == null) ||
                    (showPlace == false && targets != null))
                    continue;

                ClassList.Items.Add(new BuildListItem(entityClass));
            }

            // show current & build counts
            ShowClassCounts();

            // select remembered or first item, if any
            if (select && ClassList.Items.Count > 0) {

                if (selectedItem != null) {
                    for (int i = 0; i < ClassList.Items.Count; i++) {
                        var item = (BuildListItem) ClassList.Items[i];

                        if (item.EntityClass == selectedItem.EntityClass) {
                            ClassList.SelectedIndex = i;
                            ClassList.ScrollIntoView(item);
                            break;
                        }
                    }
                }

                if (ClassList.SelectedIndex < 0)
                    ClassList.SelectedIndex = 0;
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
        /// cref="BuildEntities"/> dialog.</remarks>

        private void HelpExecuted(object sender, ExecutedRoutedEventArgs args) {
            args.Handled = true;
            ApplicationUtility.ShowHelp("DlgBuildEntities.html");
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
        /// "Category" radio button.</remarks>

        private void OnCategoryChecked(object sender, RoutedEventArgs args) {
            args.Handled = true;
            if (!this._initialized) return;

            // determine place targets for all entity classes
            this._classTargets = Finder.FindAllPlaceTargets(
                this._worldState, this._faction, CurrentCategory);

            UpdateClasses(true);
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
            BuildListItem item = (BuildListItem) listItem.Content;
            var dialog = new ShowClasses(item.EntityClass) { Owner = this };
            dialog.ShowDialog();
        }

        #endregion
        #region OnClassBuildOnly

        /// <summary>
        /// Handles the <see cref="ButtonBase.Click"/> event for the "Build Only" <see
        /// cref="Button"/>.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="RoutedEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnClassBuildOnly</b> issues a <see cref="BuildCommand"/> for the selected item in the
        /// "Entity Class" list view, if any, and updates all dialog controls to reflect the changed
        /// entity and resource counts.</remarks>

        private void OnClassBuildOnly(object sender, RoutedEventArgs args) {
            args.Handled = true;

            // retrieve selected item, if any
            int index = ClassList.SelectedIndex;
            BuildListItem item = (index < 0 ? null : (BuildListItem) ClassList.Items[index]);

            // quit if nothing to build
            if (item == null || item.BuildCount <= 0)
                return;

            // issue Build command
            Debug.Assert(item.EntityClass != null);
            AsyncAction.BeginRun(delegate {
                Session.Instance.Executor.ExecuteBuild(this._worldState, item.EntityClass.Id, 1);

                AsyncAction.BeginInvoke(delegate {
                    // show new entity and build counts
                    ShowClassCounts();

                    // reselect item to update dialog
                    ClassList.SelectedIndex = -1;
                    ClassList.SelectAndShow(index);

                    AsyncAction.EndRun();
                });
            });
        }

        #endregion
        #region OnClassBuildPlace

        /// <summary>
        /// Handles the <see cref="ButtonBase.Click"/> event for the "Build &amp; Place" <see
        /// cref="Button"/>.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="RoutedEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnClassBuildPlace</b> sets the <see cref="BuildingClass"/> property to the selected
        /// item in the "Entity Class" list view, if any, and then closes the dialog with a <see
        /// cref="Window.DialogResult"/> of <c>true</c>. The caller should allow the user to select
        /// a placement site for the new <see cref="BuildingClass"/> instance.</remarks>

        private void OnClassBuildPlace(object sender, RoutedEventArgs args) {
            args.Handled = true;

            // retrieve selected item, if any
            int index = ClassList.SelectedIndex;
            BuildListItem item = (index < 0 ? null : (BuildListItem) ClassList.Items[index]);

            // quit if nothing to build
            if (item == null || item.BuildCount <= 0)
                return;

            // quit if entity cannot be placed
            Debug.Assert(item.EntityClass != null);
            if (!this._classTargets.ContainsKey(item.EntityClass.Id))
                return;

            // communicate entity class to caller
            BuildingClass = item.EntityClass;

            // close dialog automatically
            DialogResult = true;
            Close();
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
        /// <b>OnClassSelected</b> updates all dialog controls to reflect the selected item in the
        /// "Entity Class" list view.</remarks>

        private void OnClassSelected(object sender, SelectionChangedEventArgs args) {
            args.Handled = true;
            if (!this._initialized) return;

            // retrieve selected item, if any
            int index = ClassList.SelectedIndex;
            BuildListItem item = (index < 0 ? null : (BuildListItem) ClassList.Items[index]);

            // show resources for selected entity class
            ShowResources(item == null ? null : item.EntityClass);

            // check if we can build the selected class
            bool canBuild = (item != null && item.BuildCount > 0);

            // enable Build & Place command for placeable entities
            Debug.Assert(!canBuild || item.EntityClass != null);
            BuildPlaceButton.IsEnabled = (canBuild &&
                this._classTargets.ContainsKey(item.EntityClass.Id));

            // enable Build Only command for all entities
            BuildOnlyButton.IsEnabled = canBuild;
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

            double width = ClassList.ActualWidth -
                ClassCurrentColumn.ActualWidth - ClassBuildColumn.ActualWidth - 28;

            if (width > 0) ClassColumn.Width = width;
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

            // checking Site automatically adjusts Can Place filter
            if (args.Source == SiteToggle &&
                SiteToggle.IsChecked == true && PlaceToggle.IsChecked == false) {

                // changing Can Place re-fires this event
                PlaceToggle.IsChecked = null;
                return;
            }

            if (this._initialized) UpdateClasses(true);
        }

        #endregion
        #region OnResourceActivate

        /// <summary>
        /// Handles the <see cref="Control.MouseDoubleClick"/> event for a <see
        /// cref="ListViewItem"/> of the hosted <see cref="PropertyListView"/>.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="MouseButtonEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnResourceActivate</b> displays a <see cref="ShowVariables"/> dialog containing
        /// information on the selected item in the "Resource" list view.</remarks>

        private void OnResourceActivate(object sender, MouseButtonEventArgs args) {
            args.Handled = true;

            // retrieve double-clicked item, if any
            var source = args.OriginalSource as DependencyObject;
            var listItem = ItemsControl.ContainerFromElement(ClassList, source) as ListViewItem;
            if (listItem == null) return;

            // show info dialog for resource class
            BuildListItem item = (BuildListItem) listItem.Content;
            var dialog = new Dialog.ShowVariables(item.ResourceClass) { Owner = this };
            dialog.ShowDialog();
        }

        #endregion
        #region OnResourceWidthChanged

        /// <summary>
        /// Handles changes to the <see cref="FrameworkElement.ActualWidth"/> property of the
        /// "Resource" <see cref="ListView"/>.</summary>
        /// <param name="sender">
        /// The <see cref="ListView"/> control sending the event.</param>
        /// <param name="args">
        /// An <see cref="EventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnResourceWidthChanged</b> resizes the first column of the "Resource" list view to
        /// the available list view width.</remarks>

        private void OnResourceWidthChanged(object sender, EventArgs args) {

            double width = ResourceList.ActualWidth -
                ResourceCurrentColumn.ActualWidth - ResourceBuildColumn.ActualWidth - 28;

            if (width > 0) ResourceColumn.Width = width;
        }

        #endregion
        #endregion
    }
}
