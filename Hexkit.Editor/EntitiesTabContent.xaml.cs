using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

using Tektosyne;
using Tektosyne.Collections;
using Tektosyne.Geometry;
using Tektosyne.Windows;
using Hexkit.Scenario;

namespace Hexkit.Editor {
    #region Type Aliases

    using EntityClassDictionary = SortedListEx<String, EntityClass>;

    #endregion

    /// <summary>
    /// Provides the "Entities" tab page for the Hexkit Editor application.</summary>
    /// <remarks>
    /// Please refer to the "Entities Page" page of the "Editor Display" section in the application
    /// help file for details on this tab page.</remarks>

    public partial class EntitiesTabContent: UserControl, IEditorTabContent {
        #region EntitiesTabContent()

        /// <summary>
        /// Initializes a new instance of the <see cref="EntitiesTabContent"/> class.</summary>

        public EntitiesTabContent() {
            InitializeComponent();

            // adjust column widths of Entity list view
            DependencyPropertyDescriptor.FromProperty(
                ListView.ActualWidthProperty, typeof(ListView))
                .AddValueChanged(EntityList, OnEntityWidthChanged);
        }

        #endregion
        #region Private Fields

        // property backers
        private SectionTabItem _sectionTab;

        #endregion
        #region Private Members
        #region CurrentCategory

        /// <summary>
        /// Gets or sets the currently selected <see cref="EntityCategory"/>.</summary>
        /// <value>
        /// A <see cref="EntityCategory"/> value indicating the current selection in the "Category"
        /// <see cref="GroupBox"/>.</value>

        private EntityCategory CurrentCategory { get; set; }

        #endregion
        #region CurrentDefaultId

        /// <summary>
        /// Gets the default identifier for a new <see cref="EntityClass"/> of the <see
        /// cref="CurrentCategory"/>.</summary>
        /// <value>
        /// The default value for the <see cref="EntityClass.Id"/> string of a new <see
        /// cref="EntityClass"/> instance, given the <see cref="CurrentCategory"/>.</value>
        /// <exception cref="PropertyValueException">
        /// <see cref="CurrentCategory"/> is not a valid <see cref="EntityCategory"/> value.
        /// </exception>

        private string CurrentDefaultId {
            get {
                switch (CurrentCategory) {

                    case EntityCategory.Unit:    return "unit-id";
                    case EntityCategory.Terrain: return "terrain-id";
                    case EntityCategory.Effect:  return "effect-id";
                    case EntityCategory.Upgrade: return "upgrade-id";

                    default:
                        ThrowHelper.ThrowPropertyValueException(
                            "CurrentCategory", CurrentCategory, null);
                        return null;
                }
            }
        }

        #endregion
        #region CurrentEntities

        /// <summary>
        /// Gets the <see cref="EntitySection"/> collection for the <see cref="CurrentCategory"/>.
        /// </summary>
        /// <value>
        /// The result of <see cref="EntitySection.GetEntities"/> for the current <see
        /// cref="EntitySection"/> and the <see cref="CurrentCategory"/>.</value>

        private EntityClassDictionary CurrentEntities {
            get { return MasterSection.Instance.Entities.GetEntities(CurrentCategory); }
        }

        #endregion
        #region ChangeEntity

        /// <summary>
        /// Allows the user to change the specified <see cref="EntityClass"/>.</summary>
        /// <param name="entity">
        /// The <see cref="EntityClass"/> to change.</param>
        /// <remarks><para>
        /// <b>ChangeEntity</b> shows an error message if the <see cref="ImageSection"/> does not
        /// currently define any images. Otherwise, <b>ChangeEntity</b> displays a <see
        /// cref="Dialog.ChangeEntity"/> dialog for the specified <paramref name="entity"/>.
        /// </para><para>
        /// If the user made any changes, <b>ChangeEntity</b> propagates them to the <see
        /// cref="CurrentEntities"/> collection and sets the <see
        /// cref="SectionTabItem.DataChanged"/> flag.</para></remarks>

        private void ChangeEntity(EntityClass entity) {
            if (entity == null) return;

            // abort if there are no images
            if (MasterSection.Instance.Images.Collection.Count == 0) {
                MessageBox.Show(MainWindow.Instance,
                    Global.Strings.DialogImageNone, Global.Strings.TitleImageNone,
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // show dialog and let user make changes
            var dialog = new Dialog.ChangeEntity(entity) { Owner = MainWindow.Instance };
            dialog.ShowDialog();

            // broadcast data changes, if any
            if (dialog.DataChanged) {
                EntityList.Items.Refresh();
                UpdatePreview();
                SectionTab.DataChanged = true;
            }
        }

        #endregion
        #region EnableListButtons

        /// <summary>
        /// Enables or disables the "Change ID", "Change Entity", "Remove Entity", and "Change
        /// Placement Sites" <see cref="Button"/> controls, depending on the checked "Category" and
        /// on whether the "Entity" <see cref="ListView"/> contains any items.</summary>

        private void EnableListButtons() {

            // enable or disable list control buttons
            bool anyEntities = (EntityList.Items.Count > 0);

            ChangeIdButton.IsEnabled = anyEntities;
            ChangeEntityButton.IsEnabled = anyEntities;
            RemoveEntityButton.IsEnabled = anyEntities;

            // button "Change Placement Sites" requires units or terrains
            ChangePlacementsButton.IsEnabled = (anyEntities && 
                (CurrentCategory == EntityCategory.Unit ||
                CurrentCategory == EntityCategory.Terrain));
        }

        #endregion
        #endregion
        #region UpdatePreview

        /// <summary>
        /// Shows a preview of the selected <see cref="EntityClass"/>.</summary>
        /// <remarks>
        /// <b>UpdatePreview</b> updates the "Image Preview" control with the current <see
        /// cref="PolygonGrid.Element"/> shape and the data of the first selected item in the
        /// "Entity" list view.</remarks>

        public void UpdatePreview() {

            // update polygon shape if necessary
            EntityPreview.Polygon = MasterSection.Instance.Areas.MapGrid.Element;

            // show image stack of selected entity, if any
            EntityClass entity = EntityList.SelectedItem as EntityClass;
            if (entity == null)
                EntityPreview.Clear();
            else
                EntityPreview.Show(entity.ImageStack);
        }

        #endregion
        #region Event Handlers
        #region OnCategoryChecked

        /// <summary>
        /// Handles the <see cref="ToggleButton.Checked"/> event for the "Category" <see
        /// cref="RadioButton"/> controls.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// An <see cref="EventArgs"/> or <see cref="RoutedEventArgs"/> object containing event
        /// data.</param>
        /// <remarks>
        /// <b>OnCategoryChecked</b> updates the <see cref="CurrentCategory"/> with the <see
        /// cref="EntityCategory"/> value that corresponds to the selected radio button, and then
        /// reloads the "Entity" list view with corresponding <see cref="CurrentEntities"/>.
        /// </remarks>

        private void OnCategoryChecked(object sender, EventArgs args) {
            RoutedEventArgs routedArgs = args as RoutedEventArgs;
            if (routedArgs != null) {
                routedArgs.Handled = true;
                sender = routedArgs.Source;
            }

            // change category if sent by radio button
            if (sender == UnitToggle)
                CurrentCategory = EntityCategory.Unit;
            else if (sender == TerrainToggle)
                CurrentCategory = EntityCategory.Terrain;
            else if (sender == EffectToggle)
                CurrentCategory = EntityCategory.Effect;
            else if (sender == UpgradeToggle)
                CurrentCategory = EntityCategory.Upgrade;

            // update entity list and buttons
            EntityList.ItemsSource = CurrentEntities.Values;
            EnableListButtons();

            // select first item by default
            if (EntityList.Items.Count > 0)
                EntityList.SelectedIndex = 0;
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
        /// <b>OnEntityActivate</b> calls <see cref="ChangeEntity"/> with the double-clicked item in
        /// the "Entity" list view.</remarks>

        private void OnEntityActivate(object sender, MouseButtonEventArgs args) {
            args.Handled = true;

            // retrieve double-clicked item, if any
            var source = args.OriginalSource as DependencyObject;
            var item = ItemsControl.ContainerFromElement(EntityList, source) as ListViewItem;
            if (item != null) ChangeEntity(item.Content as EntityClass);
        }

        #endregion
        #region OnEntityAdd

        /// <summary>
        /// Handles the <see cref="ButtonBase.Click"/> event for the "Add Entity" <see
        /// cref="Button"/>.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="RoutedEventArgs"/> object containing event data.</param>
        /// <remarks><para>
        /// <b>OnEntityAdd</b> shows an error message if the <see cref="ImageSection"/> does not
        /// currently define any images. Otherwise, <b>OnEntityAdd</b> displays a <see
        /// cref="Dialog.ChangeIdentifier"/> dialog, followed by a <see cref="Dialog.ChangeEntity"/>
        /// dialog, allowing the user to define a new entity. The new entity copies the properties
        /// of the first selected item in the "Entity" list view, if any; otherwise, it is created
        /// with default properties.
        /// </para><para>
        /// If the user confirmed both dialogs, <b>OnEntityAdd</b> adds the new entity to the
        /// "Entity" list view and to the <see cref="CurrentEntities"/> collection, and sets the
        /// <see cref="SectionTabItem.DataChanged"/> flag.</para></remarks>

        private void OnEntityAdd(object sender, RoutedEventArgs args) {
            args.Handled = true;

            // abort if there are no images
            if (MasterSection.Instance.Images.Collection.Count == 0) {
                MessageBox.Show(MainWindow.Instance,
                    Global.Strings.DialogImageNone, Global.Strings.TitleImageNone,
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // ask user for new entity ID
            var entities = CurrentEntities;
            var dialog = new Dialog.ChangeIdentifier(CurrentDefaultId,
                Global.Strings.TitleEntityIdEnter, entities.ContainsKey, false);
            dialog.Owner = MainWindow.Instance;
            if (dialog.ShowDialog() != true) return;

            // retrieve new entity ID
            string id = String.Intern(dialog.Identifier);

            // create new entity based on selected entity, if any
            EntityClass entity, selection = EntityList.SelectedItem as EntityClass;
            if (selection == null)
                entity = EntityClass.Create(id, CurrentCategory);
            else {
                entity = (EntityClass) selection.Clone();
                entity.Id = id;
            }

            // let user make changes to new entity
            var entityDialog = new Dialog.ChangeEntity(entity) { Owner = MainWindow.Instance };
            if (entityDialog.ShowDialog() != true) return;

            // add entity to section table
            entities.Add(id, entity);

            // update list view and select new item
            EntityList.Items.Refresh();
            EntityList.SelectAndShow(entity);

            // broadcast data changes
            EnableListButtons();
            SectionTab.DataChanged = true;
        }

        #endregion
        #region OnEntityChange

        /// <summary>
        /// Handles the <see cref="ButtonBase.Click"/> event for the "Change Entity" <see
        /// cref="Button"/>.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="RoutedEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnEntityChange</b> calls <see cref="ChangeEntity"/> with the first selected item in
        /// the "Entity" list view.</remarks>

        private void OnEntityChange(object sender, RoutedEventArgs args) {
            args.Handled = true;
            ChangeEntity(EntityList.SelectedItem as EntityClass);
        }

        #endregion
        #region OnEntityId

        /// <summary>
        /// Handles the <see cref="ButtonBase.Click"/> event for the "Change ID" <see
        /// cref="Button"/>.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="RoutedEventArgs"/> object containing event data.</param>
        /// <remarks><para>
        /// <b>OnEntityId</b> displays a <see cref="Dialog.ChangeIdentifier"/> dialog for the first
        /// selected item in the "Entity" list view.
        /// </para><para>
        /// If the user made any changes, <b>OnEntityId</b> propagates them to the <see
        /// cref="CurrentEntities"/> collection and sets the <see
        /// cref="SectionTabItem.DataChanged"/> flag.</para></remarks>

        private void OnEntityId(object sender, RoutedEventArgs args) {
            args.Handled = true;

            // retrieve selected entity, if any
            EntityClass entity = EntityList.SelectedItem as EntityClass;
            if (entity == null) return;

            // let user enter new entity ID
            var entities = CurrentEntities;
            var dialog = new Dialog.ChangeIdentifier(entity.Id,
                Global.Strings.TitleEntityIdChange, entities.ContainsKey, true);
            dialog.Owner = MainWindow.Instance;
            if (dialog.ShowDialog() != true) return;

            // retrieve new faction ID
            string id = String.Intern(dialog.Identifier);

            // change existing ID references
            if (!SectionTabItem.ProcessAllIdentifiers(entities, entity.Id, id))
                return;

            // broadcast data changes
            EntityList.Items.Refresh();
            SectionTab.DataChanged = true;
        }

        #endregion
        #region OnEntityRemove

        /// <summary>
        /// Handles the <see cref="ButtonBase.Click"/> event for the "Remove Entity" <see
        /// cref="Button"/>.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="RoutedEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnEntityRemove</b> removes the first selected item in the "Entity" list view from
        /// that list view and from the <see cref="CurrentEntities"/> collection, and sets the <see
        /// cref="SectionTabItem.DataChanged"/> flag.</remarks>

        private void OnEntityRemove(object sender, RoutedEventArgs args) {
            args.Handled = true;

            // retrieve selected faction, if any
            int index = EntityList.SelectedIndex;
            if (index < 0) return;
            EntityClass entity = (EntityClass) EntityList.Items[index];

            // delete existing ID references
            var entities = CurrentEntities;
            if (!SectionTabItem.ProcessAllIdentifiers(entities, entity.Id, null))
                return;

            // select item in the same position
            EntityList.Items.Refresh();
            if (EntityList.Items.Count > 0)
                EntityList.SelectAndShow(Math.Min(EntityList.Items.Count - 1, index));

            // broadcast data changes
            EnableListButtons();
            SectionTab.DataChanged = true;
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

        private void OnEntitySelected(object sender, SelectionChangedEventArgs args) {
            args.Handled = true;
            UpdatePreview();
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
        /// <b>OnEntityWidthChanged</b> resizes both columns of the "Entity" list view so that each
        /// occupies the same share of the current list view width.</remarks>

        private void OnEntityWidthChanged(object sender, EventArgs args) {

            double width = (EntityList.ActualWidth - 28) / 2.0;
            if (width > 0) {
                EntityIdColumn.Width = width;
                EntityNameColumn.Width = width;
            }
        }

        #endregion
        #region OnPlacementsChange

        /// <summary>
        /// Handles the <see cref="ButtonBase.Click"/> event for the "Change Placement Sites" <see
        /// cref="Button"/>.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="RoutedEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnPlacementsChange</b> shows the <see cref="Dialog.ChangePlacements"/> dialog for the
        /// first selected item in the "Entity" list view, and sets the <see
        /// cref="SectionTabItem.DataChanged"/> flag of the "Entities" and/or "Areas" tab page if
        /// the user made any changes to the data of the current <see cref="EntitySection"/> and/or
        /// <see cref="AreaSection"/>, respectively.</remarks>

        private void OnPlacementsChange(object sender, RoutedEventArgs args) {
            args.Handled = true;

            // retrieve selected entity class, if any
            EntityClass entity = EntityList.SelectedItem as EntityClass;
            if (entity == null) return;

            // show "Change Placement Sites" dialog
            using (var dialog = new Dialog.ChangePlacements(entity)) {
                dialog.Owner = MainWindow.Instance;
                dialog.ShowDialog();

                // broadcast data changes
                if (dialog.EntitiesChanged)
                    SectionTab.DataChanged = true;
                if (dialog.AreasChanged)
                    MainWindow.Instance.AreasTab.DataChanged = true;
            }
        }

        #endregion
        #endregion
        #region IEditorTabContent Members
        #region Section

        /// <summary>
        /// Gets the <see cref="ScenarioSection"/> managed by the tab page.</summary>
        /// <value>
        /// The constant value <see cref="ScenarioSection.Entities"/>, indicating the Hexkit
        /// scenario section managed by the "Entities" tab page.</value>

        public ScenarioSection Section {
            get { return ScenarioSection.Entities; }
        }

        #endregion
        #region SectionTab

        /// <summary>
        /// Gets or sets the <see cref="SectionTabItem"/> for the tab page.</summary>
        /// <value>
        /// The <see cref="SectionTabItem"/> that contains the <see cref="EntitiesTabContent"/>
        /// control, i.e. the "Entities" tab page of the Hexkit Editor application.</value>
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
        /// <b>Initialize</b> initializes all controls that are specific to the "Entities" tab
        /// page.</remarks>

        public void Initialize() {

            // set preview panel to current polygon shape
            EntityPreview.Polygon = MasterSection.Instance.Areas.MapGrid.Element;

            // show units by default
            if (UnitToggle.IsChecked != true)
                UnitToggle.IsChecked = true;
            else {
                // force control update if Unit already checked
                OnCategoryChecked(UnitToggle, EventArgs.Empty);
            }
        }

        #endregion
        #endregion
    }
}
