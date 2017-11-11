using System;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

using Tektosyne;
using Tektosyne.Collections;
using Tektosyne.Windows;

using Hexkit.Global;
using Hexkit.Scenario;
using Hexkit.World;

namespace Hexkit.Editor.Dialog {
    #region Type Aliases

    using EntityClassDictionary = SortedListEx<String, EntityClass>;
    using EntityTemplateList = ListEx<EntityTemplate>;

    // entity stacks: entity class ID and entity template
    using EntityListItem = Tuple<String, EntityTemplate>;

    #endregion

    /// <summary>
    /// Shows a dialog allowing the user to change an <see cref="Area"/> for a single <see
    /// cref="World.Site"/>, or the default terrains for all sites.</summary>
    /// <remarks>
    /// Please refer to the "Change Site Contents" page of the "Editor Dialogs" section in the
    /// application help file for details on this dialog.</remarks>

    public partial class ChangeSite: Window {
        #region ChangeSite(Area, EntityCategory)

        /// <summary>
        /// Initializes a new instance of the <see cref="ChangeSite"/> class with the specified <see
        /// cref="Area"/> and <see cref="EntityCategory"/>.</summary>
        /// <param name="area"><para>
        /// The <see cref="Area"/> whose data to change.
        /// </para><para>-or-</para><para>
        /// A null reference to change the default terrain stack of the current <see
        /// cref="AreaSection"/>.</para></param>
        /// <param name="factions">
        /// An <see cref="IList"/> containing all faction identifiers in the current <see
        /// cref="FactionSection"/>, starting at index position one. Ignored if <paramref
        /// name="area"/> is a null reference.</param>
        /// <param name="category">
        /// A <see cref="EntityCategory"/> value indicating which entity stack to show initially.
        /// Ignored if <paramref name="area"/> is a null reference.</param>
        /// <exception cref="ArgumentException">
        /// <paramref name="area"/> is not a null reference and contains an empty <see
        /// cref="Area.Bounds"/> collection.</exception>
        /// <remarks><para>
        /// If <paramref name="area"/> is a null reference, <b>ChangeSite</b> may change the
        /// contents of the <see cref="AreaSection.Terrains"/> collection of the current <see
        /// cref="AreaSection"/>, i.e. the default terrain stack. Otherwise, the specified <paramref
        /// name="area"/> may be changed but the <b>AreaSection</b> will remain unchanged.
        /// </para><para>
        /// In either case, the value of the <see cref="DataChanged"/> property indicates whether
        /// any changes were made.</para></remarks>

        public ChangeSite(Area area, IList factions, EntityCategory category) {

            if (area != null && area.Bounds.Count == 0)
                ThrowHelper.ThrowArgumentExceptionWithFormat(
                    "area", Tektosyne.Strings.ArgumentContainsEmpty, "Bounds");

            this._area = area;
            InitializeComponent();

            if (area == null) {
                // hide Other tab when editing default contents
                OtherTab.Visibility = Visibility.Collapsed;
            } else {
                // show caption for editing site contents
                Title = Global.Strings.TitleContentsChange + Site.Format(area.Bounds[0].Location);

                // set initial tab selection as specified
                if (category == EntityCategory.Terrain)
                    TerrainTab.IsSelected = true;
                else
                    OtherTab.IsSelected = true;
            }

            #region TerrainTab

            // initialize stack control buttons
            AddTerrainButton.ShowSymbol(Symbols.BoxEmpty);
            ChangeTerrainButton.ShowSymbol(Symbols.Pencil);
            RemoveTerrainButton.ShowSymbol(Symbols.BoxCrossed);
            MoveTerrainUpButton.ShowSymbol(Symbols.ArrowUp);
            MoveTerrainDownButton.ShowSymbol(Symbols.ArrowDown);

            // initialize preview panel for stack images
            TerrainPreview.Polygon = MasterSection.Instance.Areas.MapGrid.Element;

            // create shallow copy of default or local terrain stack
            var terrainClasses = MasterSection.Instance.Entities.Terrains;
            this._terrains = new EntityTemplateList(area == null ?
                MasterSection.Instance.Areas.Terrains : area.Terrains);

            // add "no background" entry to combo box
            if (area != null)
                BackgroundCombo.Items.Add(Global.Strings.LabelTerrainBackgroundNone);

            // add scenario terrains to appropriate controls
            foreach (var pair in terrainClasses) {
                TerrainClass terrain = (TerrainClass) pair.Value;

                // add background terrains to combo box
                if (terrain != null && terrain.IsBackground) {
                    BackgroundCombo.Items.Add(pair.Key);
                    continue;
                }

                // add other terrains to foreground list
                AvailableTerrainList.Items.Add(pair.Key);
            }

            // client must provide at least one background terrain
            Debug.Assert(BackgroundCombo.Items.Count > 0,
                "Client failed to provide background terrains.");

            // select first background terrain entry
            BackgroundCombo.Tag = null;
            BackgroundCombo.SelectedIndex = 0;

            // show terrain stack in appropriate controls
            foreach (EntityTemplate template in this._terrains) {
                EntityClass terrain;
                terrainClasses.TryGetValue(template.EntityClass, out terrain);

                // select background terrain in combo box
                if (terrain != null && ((TerrainClass) terrain).IsBackground) {
                    BackgroundCombo.Tag = template;
                    BackgroundCombo.SelectedItem = template.EntityClass;
                    continue;
                }

                // add other terrains to editable list
                CreateEntityItem(TerrainList, template);
            }

            // create entity template for background terrain
            if (area == null && BackgroundCombo.Tag == null) {
                EntityTemplate template = new EntityTemplate(EntityCategory.Terrain);
                template.EntityClass = (string) BackgroundCombo.SelectedItem;
                BackgroundCombo.Tag = template;
            }

            // determine "Change Terrain" button and change marker
            BackgroundChanged.Visibility = Visibility.Hidden;
            ChangeBackgroundButton.IsEnabled = false;

            if (BackgroundCombo.Tag != null) {
                ChangeBackgroundButton.IsEnabled = true;
                if (((EntityTemplate) BackgroundCombo.Tag).IsModified)
                    BackgroundChanged.Visibility = Visibility.Visible;
            }

            // select first terrain class, if any
            bool anyClasses = (AvailableTerrainList.Items.Count > 0);
            if (anyClasses) AvailableTerrainList.SelectedIndex = 0;

            // select first terrain template, if any
            bool anyTerrains = (TerrainList.Items.Count > 0);
            if (anyTerrains) TerrainList.SelectedIndex = 0;

            // enable or disable terrain list controls
            AddTerrainButton.IsEnabled = anyClasses;
            ChangeTerrainButton.IsEnabled = anyTerrains;
            RemoveTerrainButton.IsEnabled = anyTerrains;
            MoveTerrainDownButton.IsEnabled = anyTerrains;
            MoveTerrainUpButton.IsEnabled = anyTerrains;

            // adjust column width of Terrain list view
            DependencyPropertyDescriptor.FromProperty(
                ListView.ActualWidthProperty, typeof(ListView))
                .AddValueChanged(TerrainList, OnTerrainWidthChanged);

            // adjust column width of Available Terrain list view
            DependencyPropertyDescriptor.FromProperty(
                ListView.ActualWidthProperty, typeof(ListView))
                .AddValueChanged(AvailableTerrainList, OnAvailableTerrainWidthChanged);

            // create terrain stack preview
            UpdatePreview(EntityCategory.Terrain);

            #endregion
            #region OtherTab

            if (area != null) {
                // initialize stack control buttons
                AddEntityButton.ShowSymbol(Symbols.BoxEmpty);
                ChangeEntityButton.ShowSymbol(Symbols.Pencil);
                RemoveEntityButton.ShowSymbol(Symbols.BoxCrossed);
                MoveEntityUpButton.ShowSymbol(Symbols.ArrowUp);
                MoveEntityDownButton.ShowSymbol(Symbols.ArrowDown);

                // initialize preview panel for stack images
                EntityPreview.Polygon = MasterSection.Instance.Areas.MapGrid.Element;

                // create shallow copies of non-terrain stacks
                this._units = new EntityTemplateList(area.Units);
                this._effects = new EntityTemplateList(area.Effects);

                // add "same as site owner" entry to combo box
                UnitOwnerCombo.Items.Add(Global.Strings.LabelUnitOwnerNone);

                // add all faction identifiers to combo box
                for (int i = 1; i < factions.Count; i++)
                    UnitOwnerCombo.Items.Add(factions[i]);

                // select entry indicated by area's unit owner
                this._unitOwner = area.UnitOwner;
                if (this._unitOwner.Length == 0) {

                    // show "same as site owner" if unowned
                    UnitOwnerCombo.SelectedIndex = 0;
                } else {
                    // add unit owner ID if not among factions
                    if (!UnitOwnerCombo.Items.Contains(this._unitOwner))
                        UnitOwnerCombo.Items.Add(this._unitOwner);

                    UnitOwnerCombo.SelectedItem = this._unitOwner;
                }

                // adjust column width of Entity list view
                DependencyPropertyDescriptor.FromProperty(
                    ListView.ActualWidthProperty, typeof(ListView))
                    .AddValueChanged(EntityList, OnEntityWidthChanged);

                // adjust column width of Available Entity list view
                DependencyPropertyDescriptor.FromProperty(
                    ListView.ActualWidthProperty, typeof(ListView))
                    .AddValueChanged(AvailableEntityList, OnAvailableEntityWidthChanged);
            }

            #endregion

            // construction completed
            this._initialized = true;

            // Other tab: select specified entity stack
            if (area != null) {
                if (category == EntityCategory.Effect)
                    EffectToggle.IsChecked = true;
                else
                    UnitToggle.IsChecked = true;
            }
        }

        #endregion
        #region Private Fields

        // construction parameters
        private readonly Area _area;

        // was construction completed?
        private readonly bool _initialized = false;

        #region TerrainTab

        /// <summary>
        /// The site's current <see cref="Area.Terrains"/> stack or the scenario's default <see
        /// cref="AreaSection.Terrains"/> stack.</summary>

        private readonly EntityTemplateList _terrains;

        #endregion
        #region OtherTab

        // selected entity category and corresponding stack & classes
        private EntityCategory _currentCategory;
        private EntityTemplateList _currentEntities;
        private EntityClassDictionary _currentClasses;

        /// <summary>
        /// The site's current <see cref="Area.Effects"/> stack.</summary>

        private readonly EntityTemplateList _effects;

        /// <summary>
        /// The site's current <see cref="Area.Units"/> stack.</summary>

        private readonly EntityTemplateList _units;

        /// <summary><para>
        /// The current <see cref="FactionClass.Id"/> string of the <see cref="FactionClass"/> that
        /// owns all of the site's <see cref="Area.Units"/>.
        /// </para><para>-or-</para><para>
        /// An empty string if all <see cref="Area.Units"/> belong to the site's current <see
        /// cref="Area.Owner"/>.</para></summary>

        private string _unitOwner;

        #endregion
        #endregion
        #region DataChanged

        /// <summary>
        /// Gets a value indicating whether any data was changed in the dialog.</summary>
        /// <value>
        /// <c>true</c> if either the <see cref="Area"/> supplied to the constructor or the current
        /// <see cref="AreaSection"/> have been modified; otherwise, <c>false</c>.</value>
        /// <remarks>
        /// <b>DataChanged</b> returns <c>false</c> if no detectable changes were made. However, the
        /// original data may have been overwritten with a copy that is not detectably different,
        /// namely if the user clicked <b>OK</b> without making any changes.</remarks>

        public bool DataChanged { get; private set; }

        #endregion
        #region Private Methods
        #region CreateEntityItem

        /// <summary>
        /// Creates a new item for the "Terrain Stack" or "Entity Stack" <see cref="ListView"/>, 
        /// containing the specified <see cref="EntityTemplate"/>.</summary>
        /// <param name="listView">
        /// The <see cref="ListView"/> that receives the specified <paramref name="template"/>.
        /// </param>
        /// <param name="template">
        /// The <see cref="EntityTemplate"/> to add to the specified <paramref name="listView"/>.
        /// </param>
        /// <returns>
        /// The newly created <see cref="EntityListItem"/> for the specified <paramref
        /// name="template"/> that was added to the "Entity" <see cref="ListView"/>.</returns>
        /// <remarks>
        /// The <see cref="EntityListItem.Item2"/> component of the returned <see
        /// cref="EntityListItem"/> contains the specified <paramref name="template"/>.</remarks>

        private static EntityListItem CreateEntityItem(
            ListView listView, EntityTemplate template) {

            EntityListItem item = new EntityListItem(GetEntityText(template), template);
            listView.Items.Add(item);
            return item;
        }

        #endregion
        #region GetEntityText

        /// <summary>
        /// Returns the <see cref="EntityListItem.Item1"/> component of the <see
        /// cref="EntityListItem"/> for the specified <see cref="EntityTemplate"/>.</summary>
        /// <param name="template">
        /// The <see cref="EntityTemplate"/> to display.</param>
        /// <returns>
        /// The <see cref="EntityTemplate.EntityClass"/> of the specified <paramref
        /// name="template"/>, with a prepended asterisk (*) if its <see
        /// cref="EntityTemplate.IsModified"/> flag is <c>true</c>.</returns>

        private static string GetEntityText(EntityTemplate template) {
            string text = template.EntityClass;
            if (template.IsModified) text = "* " + text;
            return text;
        }

        #endregion
        #region OnOtherTabChanged

        /// <summary>
        /// Updates the <see cref="OtherTab"/> page in response to data changes.</summary>
        /// <remarks>
        /// <b>OnOtherTabChanged</b> reloads the current entity stack from the current contents of
        /// the <see cref="OtherTab"/> page, updates any related controls, and sets the <see
        /// cref="DataChanged"/> flag.</remarks>

        private void OnOtherTabChanged() {

            // reload entity stack from list view
            this._currentEntities.Clear();
            foreach (EntityListItem item in EntityList.Items)
                this._currentEntities.Add(item.Item2);

            // enable or disable entity list controls
            bool anyEntities = (EntityList.Items.Count > 0);
            ChangeEntityButton.IsEnabled = anyEntities;
            RemoveEntityButton.IsEnabled = anyEntities;
            MoveEntityDownButton.IsEnabled = anyEntities;
            MoveEntityUpButton.IsEnabled = anyEntities;

            // update preview and broadcast changes
            UpdatePreview(this._currentCategory);
            DataChanged = true;
        }

        #endregion
        #region OnTerrainTabChanged

        /// <summary>
        /// Updates the <see cref="TerrainTab"/> page in response to data changes.</summary>
        /// <remarks>
        /// <b>OnTerrainTabChanged</b> reloads the current terrain stack from the current contents
        /// of the <see cref="TerrainTab"/> page, updates any related controls, and sets the <see
        /// cref="DataChanged"/> flag.</remarks>

        private void OnTerrainTabChanged() {

            // reload terrain stack
            this._terrains.Clear();
            BackgroundChanged.Visibility = Visibility.Hidden;

            // add background terrain if available
            EntityTemplate template = (EntityTemplate) BackgroundCombo.Tag;
            if (template != null) {
                this._terrains.Add(template);
                if (template.IsModified) BackgroundChanged.Visibility = Visibility.Visible;
            }

            // add foreground terrain list
            foreach (EntityListItem item in TerrainList.Items)
                this._terrains.Add(item.Item2);

            // enable or disable terrain list controls
            bool anyTerrains = (TerrainList.Items.Count > 0);
            ChangeTerrainButton.IsEnabled = anyTerrains;
            RemoveTerrainButton.IsEnabled = anyTerrains;
            MoveTerrainDownButton.IsEnabled = anyTerrains;
            MoveTerrainUpButton.IsEnabled = anyTerrains;

            // update preview and broadcast changes
            UpdatePreview(EntityCategory.Terrain);
            DataChanged = true;
        }

        #endregion
        #region UpdatePreview

        /// <summary>
        /// Updates the "Stack Preview" panel for the specified <see cref="EntityCategory"/>.
        /// </summary>
        /// <param name="category">
        /// An <see cref="EntityCategory"/> value indicating the entity stack to preview.</param>
        /// <exception cref="InvalidEnumArgumentException">
        /// <paramref name="category"/> is <see cref="EntityCategory.Upgrade"/> or an invalid <see
        /// cref="EntityCategory"/> value.</exception>
        /// <remarks>
        /// <b>UpdatePreview</b> shows overlaid images for all entities if <paramref
        /// name="category"/> equals <b>Effect</b> or <b>Terrain</b>, and only the image for the
        /// topmost entity if <paramref name="category"/> equals <b>Unit</b>.</remarks>

        private void UpdatePreview(EntityCategory category) {
            switch (category) {

                case EntityCategory.Terrain:
                    // show entire stack for terrains
                    TerrainPreview.Show(this._terrains);
                    break;

                case EntityCategory.Unit:
                    // show only topmost entity for units
                    int index = this._units.Count - 1;
                    if (index < 0)
                        EntityPreview.Clear();
                    else
                        EntityPreview.Show(this._units[index]);
                    break;

                case EntityCategory.Effect:
                    // show entire stack for effects
                    EntityPreview.Show(this._effects);
                    break;

                default:
                    ThrowHelper.ThrowInvalidEnumArgumentException(
                        "category", (int) category, typeof(EntityCategory));
                    break;
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
        /// <b>HelpExecuted</b> opens the application help file on the help page for the current tab
        /// page of the <see cref="ChangeSite"/> dialog.</remarks>

        private void HelpExecuted(object sender, ExecutedRoutedEventArgs args) {
            args.Handled = true;

            // default to dialog help page
            string helpPage = "DlgChangeSite.html";

            // show help for specific tab page
            if (TerrainTab.IsSelected)
                helpPage = "DlgChangeSiteTerrain.html";
            else if (OtherTab.IsSelected)
                helpPage = "DlgChangeSiteOther.html";

            ApplicationUtility.ShowHelp(helpPage);
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
        #region OnBackgroundChange

        /// <summary>
        /// Handles the <see cref="ButtonBase.Click"/> event for the "Change Terrain" <see
        /// cref="Button"/> on the <see cref="TerrainTab"/> page.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="RoutedEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnBackgroundChange</b> shows the <see cref="Dialog.ChangeTemplate"/> dialog with the
        /// selected item in the "Terrain Class" combo box, if any, and calls <see
        /// cref="OnTerrainTabChanged"/> if the user made any changes.</remarks>

        private void OnBackgroundChange(object sender, RoutedEventArgs args) {
            args.Handled = true;
            if (!this._initialized) return;

            // retrieve background terrain, if any
            EntityTemplate template = (EntityTemplate) BackgroundCombo.Tag;
            if (template == null) return;

            // attempt to find underlying entity class
            EntityClass entityClass;
            if (!ChangeTemplate.CanEdit(this, template, out entityClass))
                return;

            // show Change Template dialog
            var dialog = new ChangeTemplate(template, entityClass) { Owner = this };
            dialog.ShowDialog();

            // broadcast data changes
            if (dialog.DataChanged) OnTerrainTabChanged();
        }

        #endregion
        #region OnBackgroundSelected

        /// <summary>
        /// Handles the <see cref="Selector.SelectionChanged"/> event for the "Terrain Class" <see
        /// cref="ComboBox"/> on the <see cref="TerrainTab"/> page.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="SelectionChangedEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnBackgroundSelected</b> enables or disables the "Change Terrain" button, depending
        /// on whether a valid background terrain is selected in the "Terrain Class" combo box, and
        /// calls <see cref="OnTerrainTabChanged"/>.</remarks>

        private void OnBackgroundSelected(object sender, SelectionChangedEventArgs args) {
            args.Handled = true;
            if (!this._initialized) return;

            // is concrete background terrain selected?
            int index = BackgroundCombo.SelectedIndex;
            bool selected = (this._area == null || index > 0);

            // create entity template for background terrain
            if (selected) {
                EntityTemplate template = new EntityTemplate(EntityCategory.Terrain);
                template.EntityClass = (string) BackgroundCombo.Items[index];
                BackgroundCombo.Tag = template;
                ChangeBackgroundButton.IsEnabled = true;
            } else {
                BackgroundCombo.Tag = null;
                ChangeBackgroundButton.IsEnabled = false;
            }

            // broadcast data changes
            OnTerrainTabChanged();
        }

        #endregion
        #region OnCategoryChecked

        /// <summary>
        /// Handles the <see cref="ToggleButton.Checked"/> event for the "Category" <see
        /// cref="RadioButton"/> controls on the <see cref="OtherTab"/> page.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="RoutedEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnCategoryChecked</b> sets the current <see cref="EntityCategory"/> to the value
        /// indicated by the selected "Category" radio button, and updates all controls on the <see
        /// cref="OtherTab"/> page to reflect the current category.</remarks>

        private void OnCategoryChecked(object sender, RoutedEventArgs args) {
            args.Handled = true;
            if (!this._initialized || this._area == null)
                return;

            // switch category (default to Unit)
            if (EffectToggle.IsChecked == true) {
                this._currentCategory = EntityCategory.Effect;
                this._currentClasses = MasterSection.Instance.Entities.Effects;
                this._currentEntities = this._effects;
            }
            else {
                this._currentCategory = EntityCategory.Unit;
                this._currentClasses = MasterSection.Instance.Entities.Units;
                this._currentEntities = this._units;
            }

            // show all available entity classes
            AvailableEntityList.ItemsSource = this._currentClasses.Keys;

            // add current entity site stack
            EntityList.Items.Clear();
            foreach (EntityTemplate template in this._currentEntities)
                CreateEntityItem(EntityList, template);

            // select first entity class, if any
            bool anyClasses = (AvailableEntityList.Items.Count > 0);
            if (anyClasses) AvailableEntityList.SelectedIndex = 0;

            // select first entity template, if any
            bool anyEntities = (EntityList.Items.Count > 0);
            if (anyEntities) EntityList.SelectedIndex = 0;

            // enable or disable entity list controls
            AddEntityButton.IsEnabled = anyClasses;
            ChangeEntityButton.IsEnabled = anyEntities;
            RemoveEntityButton.IsEnabled = anyEntities;
            MoveEntityDownButton.IsEnabled = anyEntities;
            MoveEntityUpButton.IsEnabled = anyEntities;

            // create entity stack preview
            UpdatePreview(this._currentCategory);
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
        /// cref="Area"/> object supplied to the constructor, if valid, or into the current <see
        /// cref="AreaSection"/> otherwise, and sets the <see cref="DataChanged"/> flag is any
        /// object properties were changed.</para></remarks>

        protected override void OnClosing(CancelEventArgs args) {
            base.OnClosing(args);
            if (args.Cancel) return;

            // user cancelled dialog, ignore changes
            if (DialogResult != true) {
                DataChanged = false;
                return;
            }

            if (this._area == null) {
                // read default terrain stack into Areas section
                MasterSection.Instance.Areas.Terrains.Clear();
                MasterSection.Instance.Areas.Terrains.AddRange(this._terrains);
                return;
            }

            // check for unowned units on unowned site
            if (this._area.Owner.Length == 0 &&
                this._units.Count > 0 && this._unitOwner.Length == 0) {

                MessageBox.Show(this, Global.Strings.DialogSiteUnowned,
                    Global.Strings.TitleSiteUnowned,
                    MessageBoxButton.OK, MessageBoxImage.Information);

                args.Cancel = true;
                return;
            }

            // read all entity stacks into supplied Area
            this._area.Terrains.Clear();
            this._area.Units.Clear();
            this._area.Effects.Clear();

            this._area.Terrains.AddRange(this._terrains);
            this._area.Units.AddRange(this._units);
            this._area.Effects.AddRange(this._effects);

            // store new unit owner ID
            this._area.UnitOwner = this._unitOwner;
        }

        #endregion
        #region OnEntityAdd

        /// <summary>
        /// Handles the <see cref="ButtonBase.Click"/> event for the "Add Entity" <see
        /// cref="Button"/> and the <see cref="Control.MouseDoubleClick"/> event for the "Available
        /// Entities" <see cref="ListView"/> on any tab page.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="RoutedEventArgs"/> or <see cref="MouseButtonEventArgs"/> object containing
        /// event data.</param>
        /// <remarks>
        /// <b>OnEntityAdd</b> adds either the first selected item or the double-clicked item in the
        /// "Available Classes" list view, depending on the exact type of the specified <paramref
        /// name="args"/>, to the "Entity Stack" list view and sets the <see cref="DataChanged"/>
        /// flag.</remarks>

        private void OnEntityAdd(object sender, RoutedEventArgs args) {
            args.Handled = true;
            ListView stackList, classList;
            EntityCategory category;
            Action onDataChanged;

            if (TerrainTab.IsSelected) {
                stackList = TerrainList;
                classList = AvailableTerrainList;
                category = EntityCategory.Terrain;
                onDataChanged = OnTerrainTabChanged;
            } else {
                stackList = EntityList;
                classList = AvailableEntityList;
                category = this._currentCategory;
                onDataChanged = OnOtherTabChanged;
            }

            // determine which event occurred
            string classItem = null;
            if (args.RoutedEvent == ListViewItem.MouseDoubleClickEvent) {
                // retrieve double-clicked item, if any
                var source = args.OriginalSource as DependencyObject;
                var listItem = ItemsControl.ContainerFromElement(classList, source) as ListViewItem;
                if (listItem != null) classItem = listItem.Content as string;
            } else {
                // retrieve selected entity class, if any
                classItem = classList.SelectedItem as String;
            }
            if (classItem == null) return;

            // create new entity template with selected class ID
            EntityTemplate template = new EntityTemplate(category);
            template.EntityClass = classItem;

            // create and select associated list view item
            EntityListItem item = CreateEntityItem(stackList, template);
            stackList.SelectAndShow(item);

            // broadcast data changes
            onDataChanged();
        }

        #endregion
        #region OnEntityChange

        /// <summary>
        /// Handles the <see cref="ButtonBase.Click"/> event for the "Change Entity" <see
        /// cref="Button"/> and the <see cref="Control.MouseDoubleClick"/> event for the "Entity
        /// Stack" <see cref="ListView"/> on any tab page.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="RoutedEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnEntityChange</b> shows the <see cref="Dialog.ChangeTemplate"/> dialog with either
        /// the first selected item or the double-clicked item in the "Entity Stack" list view,
        /// depending on the exact type of the specified <paramref name="args"/>, and sets the <see
        /// cref="DataChanged"/> flag if the user made any changes.</remarks>

        private void OnEntityChange(object sender, RoutedEventArgs args) {
            args.Handled = true;
            ListView stackList;
            Action onDataChanged;

            if (TerrainTab.IsSelected) {
                stackList = TerrainList;
                onDataChanged = OnTerrainTabChanged;
            } else {
                stackList = EntityList;
                onDataChanged = OnOtherTabChanged;
            }

            // determine which event occurred
            int index = -1;
            if (args.RoutedEvent == ListViewItem.MouseDoubleClickEvent) {
                // retrieve double-clicked entity, if any
                var source = args.OriginalSource as DependencyObject;
                var listItem = ItemsControl.ContainerFromElement(stackList, source) as ListViewItem;
                if (listItem != null) index = stackList.Items.IndexOf(listItem.Content);
            } else {
                // retrieve selected entity, if any
                index = stackList.SelectedIndex;
            }
            if (index < 0) return;

            EntityListItem item = (EntityListItem) stackList.Items[index];
            EntityTemplate template = item.Item2;

            // attempt to find underlying entity class
            EntityClass entityClass;
            if (!ChangeTemplate.CanEdit(this, template, out entityClass))
                return;

            // show Change Template dialog
            var dialog = new ChangeTemplate(template, entityClass) { Owner = this };
            dialog.ShowDialog();

            // broadcast data changes
            if (dialog.DataChanged) {
                item = new EntityListItem(GetEntityText(template), item.Item2);
                stackList.Items[index] = item;
                stackList.SelectAndShow(index);

                onDataChanged();
            }
        }

        #endregion
        #region OnEntityDown

        /// <summary>
        /// Handles the <see cref="ButtonBase.Click"/> event for the "Move Entity Down" <see
        /// cref="Button"/> on any tab page.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="RoutedEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnEntityDown</b> swaps the first selected item in the "Entity Stack" list view with
        /// its lower neighbor and sets the <see cref="DataChanged"/> flag.</remarks>

        private void OnEntityDown(object sender, RoutedEventArgs args) {
            args.Handled = true;
            ListView stackList;
            Action onDataChanged;

            if (args.Source == MoveTerrainDownButton) {
                stackList = TerrainList;
                onDataChanged = OnTerrainTabChanged;
            } else {
                stackList = EntityList;
                onDataChanged = OnOtherTabChanged;
            }

            // retrieve first selected item, if any
            if (stackList.SelectedItems.Count == 0 || stackList.Items.Count < 2)
                return;

            // move item down and re-select it
            object item = stackList.SelectedItem;
            int index = CollectionsUtility.MoveItemUntyped(stackList.Items, item, +1);
            stackList.SelectAndShow(Math.Min(index, stackList.Items.Count - 1));

            onDataChanged();
        }

        #endregion
        #region OnEntityRemove

        /// <summary>
        /// Handles the <see cref="ButtonBase.Click"/> event for the "Remove Entity" <see
        /// cref="Button"/> on any tab page.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="RoutedEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnEntityRemove</b> removes the first selected item from the "Entity Stack" list view
        /// and sets the <see cref="DataChanged"/> flag.</remarks>

        private void OnEntityRemove(object sender, RoutedEventArgs args) {
            args.Handled = true;
            ListView stackList;
            Action onDataChanged;

            if (args.Source == RemoveTerrainButton) {
                stackList = TerrainList;
                onDataChanged = OnTerrainTabChanged;
            } else {
                stackList = EntityList;
                onDataChanged = OnOtherTabChanged;
            }

            // retrieve selected entity, if any
            int index = stackList.SelectedIndex;
            if (index < 0) return;

            // remove entity from list view
            stackList.Items.RemoveAt(index);

            // select item in the same position, or nothing (-1)
            stackList.SelectAndShow(Math.Min(index, stackList.Items.Count - 1));

            onDataChanged();
        }

        #endregion
        #region OnEntityUp

        /// <summary>
        /// Handles the <see cref="ButtonBase.Click"/> event for the "Move Entity Up" <see
        /// cref="Button"/> on any tab page.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="RoutedEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnEntityDown</b> swaps the first selected item in the "Entity Stack" list view with
        /// its upper neighbor and sets the <see cref="DataChanged"/> flag.</remarks>

        private void OnEntityUp(object sender, RoutedEventArgs args) {
            args.Handled = true;
            ListView stackList;
            Action onDataChanged;

            if (args.Source == MoveTerrainUpButton) {
                stackList = TerrainList;
                onDataChanged = OnTerrainTabChanged;
            } else {
                stackList = EntityList;
                onDataChanged = OnOtherTabChanged;
            }

            // retrieve first selected item, if any
            if (stackList.SelectedItems.Count == 0 || stackList.Items.Count < 2)
                return;

            // move item up and re-select it
            object item = stackList.SelectedItem;
            int index = CollectionsUtility.MoveItemUntyped(stackList.Items, item, -1);
            stackList.SelectAndShow(Math.Max(0, index));

            onDataChanged();
        }

        #endregion
        #region OnUnitOwnerSelected

        /// <summary>
        /// Handles the <see cref="Selector.SelectionChanged"/> event for the "Unit Owner" <see
        /// cref="ComboBox"/> on the <see cref="OtherTab"/> page.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="SelectionChangedEventArgs"/> object containing event data.</param>
        /// <remarks><para>
        /// <b>OnUnitOwnerSelected</b> sets the current unit owner ID to an empty string if the
        /// first (or no) entry is selected in the "Unit Owner" combo box; otherwise, to the 
        /// currently selected entry.
        /// </para><para>
        /// If the new value differs from the <see cref="Area.UnitOwner"/> of the <see cref="Area"/>
        /// supplied to the constructor, <b>OnUnitOwnerSelected</b> also sets the <see
        /// cref="DataChanged"/> flag.</para></remarks>

        private void OnUnitOwnerSelected(object sender, SelectionChangedEventArgs args) {
            args.Handled = true;
            if (!this._initialized || this._area == null)
                return;

            // store selected unit owner, if any
            int index = UnitOwnerCombo.SelectedIndex;
            this._unitOwner = (index <= 0 ? "" : (string) UnitOwnerCombo.SelectedItem);

            // broadcast data changes, if any
            if (this._unitOwner != this._area.UnitOwner)
                DataChanged = true;
        }

        #endregion
        #region On...WidthChanged

        /// <summary>
        /// Handles changes to the <see cref="FrameworkElement.ActualWidth"/> property of any <see
        /// cref="ListView"/> on either tab page.</summary>
        /// <param name="sender">
        /// The <see cref="ListView"/> control sending the event.</param>
        /// <param name="args">
        /// An <see cref="EventArgs"/> object containing event data.</param>
        /// <remarks>
        /// The <b>On...WidthChanged</b> methods resize the columns of all list views to the current
        /// list view width.</remarks>

        private void OnTerrainWidthChanged(object sender, EventArgs args) {
            double width = TerrainList.ActualWidth - 28;
            if (width > 0) TerrainColumn.Width = width;
        }

        private void OnAvailableTerrainWidthChanged(object sender, EventArgs args) {
            double width = AvailableTerrainList.ActualWidth - 28;
            if (width > 0) AvailableTerrainColumn.Width = width;
        }

        private void OnEntityWidthChanged(object sender, EventArgs args) {
            double width = EntityList.ActualWidth - 28;
            if (width > 0) EntityColumn.Width = width;
        }

        private void OnAvailableEntityWidthChanged(object sender, EventArgs args) {
            double width = AvailableEntityList.ActualWidth - 28;
            if (width > 0) AvailableEntityColumn.Width = width;
        }

        #endregion
        #endregion
    }
}
