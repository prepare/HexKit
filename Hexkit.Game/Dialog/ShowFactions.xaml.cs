using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

using Tektosyne;
using Tektosyne.Collections;
using Tektosyne.Geometry;

using Hexkit.Global;
using Hexkit.Graphics;
using Hexkit.Scenario;
using Hexkit.World;

// avoid confusion with System.Windows.Condition
using Condition = Hexkit.Scenario.Condition;

namespace Hexkit.Game.Dialog {
    #region Type Aliases

    using CategorizedValueDictionary = KeyValueList<String, CategorizedValue>;

    // ClassesTab: entity name and ability indicator
    using ClassListItem = Tuple<EntityClass, String>;

    // VariablesTab: variable class and formatted value
    using VariableListItem = Tuple<VariableClass, String>;

    // VariablesTab: modifier name and formatted value
    using ModifierListItem = Tuple<String, String>;

    #endregion

    /// <summary>
    /// Shows a dialog with information on all factions in the game.</summary>
    /// <remarks>
    /// Please refer to the "Faction Status" page of the "Game Dialogs" section in the application
    /// help file for details on this dialog.</remarks>

    public partial class ShowFactions: Window {
        #region ShowFactions(MapView, FactionClass)

        /// <summary>
        /// Initializes a new instance of the <see cref="ShowFactions"/> class with the specified
        /// <see cref="MapView"/> and initially selected <see cref="FactionClass"/>.</summary>
        /// <param name="mapView">
        /// The <see cref="MapView"/> whose <see cref="MapView.WorldState"/> contains all surviving
        /// factions.</param>
        /// <param name="factionClass">
        /// The <see cref="FactionClass"/> to select initially, or a null reference to select the
        /// first faction.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="mapView"/> is a null reference.</exception>

        public ShowFactions(MapView mapView, FactionClass factionClass) {
            if (mapView == null)
                ThrowHelper.ThrowArgumentNullException("mapView");

            // select first faction class if none specified
            if (factionClass == null)
                factionClass = MasterSection.Instance.Factions.Collection[0].Value;

            this._mapView = mapView;
            this._worldState = mapView.WorldState;
            this._factionClass = factionClass;
            this._dialogTitle = Title;

            // count all sites that are, or can be, owned
            foreach (Site site in this._worldState.Sites)
                if (site.CanCapture || site.Owner != null)
                    ++this._siteCount;

            InitializeComponent();

            // ClassesTab: adjust column widths of Entity Class list view
            DependencyPropertyDescriptor.FromProperty(
                ListView.ActualWidthProperty, typeof(ListView))
                .AddValueChanged(ClassList, OnClassWidthChanged);

            // VariablesTab: adjust column widths of Variable list view
            DependencyPropertyDescriptor.FromProperty(
                ListView.ActualWidthProperty, typeof(ListView))
                .AddValueChanged(VariableList, OnVariableWidthChanged);

            // ConditionsTab: adjust column widths of Condition list view
            DependencyPropertyDescriptor.FromProperty(
                ListView.ActualWidthProperty, typeof(ListView))
                .AddValueChanged(ConditionList, OnConditionWidthChanged);

            // show factions and select specified faction
            FactionCombo.ItemsSource = MasterSection.Instance.Factions.Collection.Values;
            FactionCombo.SelectedItem = factionClass;

            // default to unit classes
            UnitToggle.IsChecked = true;
        }

        #endregion
        #region Private Fields

        // construction parameters
        private readonly MapView _mapView;
        private readonly WorldState _worldState;

        // prefix for dialog title
        private readonly string _dialogTitle;

        // total number of sites that can be owned
        private readonly int _siteCount;

        // abbreviations for entity class abilities
        private readonly static string _abbrBuild = Global.Strings.AbbrevBuild;
        private readonly static string _abbrCapture = Global.Strings.AbbrevCapture;

        // currently selected faction
        private FactionClass _factionClass;
        private Faction _faction;
        private CategorizedValueDictionary _modifiers;

        #endregion
        #region Private Members
        #region CurrentCategory

        /// <summary>
        /// Gets the <see cref="EntityCategory"/> that corresponds to the currently checked
        /// "Category" <see cref="RadioButton"/> on the <see cref="ClassesTab"/> page.</summary>
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
                else
                    return EntityCategory.Unit;
            }
        }

        #endregion
        #region CreateConditionRows

        /// <summary>
        /// Adds one <see cref="ConditionListItem"/> for each condition that applies to the selected
        /// <see cref="Faction"/> to the "Condition" <see cref="ListView"/> on the <see
        /// cref="ConditionsTab"/> page.</summary>
        /// <remarks>
        /// <b>CreateConditionRows</b> adds one row for each possible <see
        /// cref="ConditionParameter"/>, plus one row for each resource defined in the current <see
        /// cref="VariableSection"/> that appears in the <see cref="Faction.Resources"/> collection
        /// of the selected <see cref="Faction"/>, if any.</remarks>

        private void CreateConditionRows() {

            var defeatConditions = this._faction.FactionClass.DefeatConditions;
            var victoryConditions = this._faction.FactionClass.VictoryConditions;

            // process all non-resource conditions
            foreach (ConditionParameter parameter in FactionClass.AllConditionParameters) {

                // get current value for this faction
                int currentValue = this._faction.GetConditionValue(this._worldState, parameter);
                string current = currentValue.ToString("N0", ApplicationInfo.Culture);

                // get individual defeat & victory thresholds
                string defeat = "—", victory = "—";
                Condition condition;
                if (defeatConditions.TryGetValue(parameter, out condition))
                    defeat = condition.Threshold.ToString("N0", ApplicationInfo.Culture);
                if (victoryConditions.TryGetValue(parameter, out condition))
                    victory = condition.Threshold.ToString("N0", ApplicationInfo.Culture);

                var item = new ConditionListItem(parameter) {
                    Current = current, Defeat = defeat, Victory = victory
                };

                ConditionList.Items.Add(item);
            }

            ConditionList.Items.Add(new ConditionListItem());

            // process all resources defined by scenario
            foreach (var pair in MasterSection.Instance.Variables.Resources) {

                // skip resources not owned by faction
                if (!this._faction.Resources.Variables.ContainsKey(pair.Key))
                    continue;

                // get current value for this faction
                int currentValue = this._faction.Resources.Variables[pair.Key].Value;
                string current = currentValue.ToString("N0", ApplicationInfo.Culture);

                // get global defeat & victory thresholds
                ResourceClass resource = (ResourceClass) pair.Value;
                string defeat = "—", victory = "—";
                if (resource.Defeat > Int32.MinValue)
                    defeat = resource.Defeat.ToString("N0", ApplicationInfo.Culture);
                if (resource.Victory < Int32.MaxValue)
                    victory = resource.Victory.ToString("N0", ApplicationInfo.Culture);

                var item = new ConditionListItem(pair.Value) {
                    Current = current, Defeat = defeat, Victory = victory
                };

                ConditionList.Items.Add(item);
            }
        }

        #endregion
        #region CreateModifierRows

        /// <summary>
        /// Adds one <see cref="ModifierListItem"/> for each <see cref="CategorizedValue"/> property
        /// to the "Modifier" <see cref="ListView"/> on the <see cref="VariablesTab"/> page.
        /// </summary>
        /// <remarks>
        /// <b>CreateModifierRows</b> adds one row for each of the four entity categories, another
        /// row for the "Other" property, one blank row, and a final row for the "Total" property.
        /// The "Value" column of each non-blank row contains an em dash (—).</remarks>

        private void CreateModifierRows() {
            string dash = "—"; // em dash

            ModifierList.Items.Add(new ModifierListItem(Global.Strings.LabelUnits, dash));
            ModifierList.Items.Add(new ModifierListItem(Global.Strings.LabelTerrains, dash));
            ModifierList.Items.Add(new ModifierListItem(Global.Strings.LabelEffects, dash));
            ModifierList.Items.Add(new ModifierListItem(Global.Strings.LabelUpgrades, dash));
            ModifierList.Items.Add(new ModifierListItem(Global.Strings.LabelOther, dash));
            ModifierList.Items.Add(new ModifierListItem("", ""));
            ModifierList.Items.Add(new ModifierListItem(Global.Strings.LabelTotal, dash));
        }

        #endregion
        #region CreateResourceRows

        /// <summary>
        /// Adds one <see cref="VariableListItem"/> for each resource of the selected <see
        /// cref="Faction"/> to the "Resource" <see cref="ListView"/> on the <see
        /// cref="VariablesTab"/> page.</summary>
        /// <remarks>
        /// <b>CreateResourceRows</b> adds one row for each resource defined in the current <see
        /// cref="VariableSection"/> that appears in either the <see cref="Faction.Resources"/> or
        /// the <see cref="Faction.ResourceModifiers"/> collection of the selected <see
        /// cref="Faction"/>.</remarks>

        private void CreateResourceRows() {

            var basics = this._faction.Resources.Variables;
            var modifiers = this._faction.ResourceModifiers.Variables;

            // process all resources defined by the scenario
            foreach (var pair in MasterSection.Instance.Variables.Resources) {

                // determine which resources are present
                bool hasBasic = basics.ContainsKey(pair.Key);
                bool hasModifier = modifiers.ContainsKey(pair.Key);

                // create string containing all available values
                string values = null;
                if (hasBasic) {
                    if (hasModifier) {
                        values = String.Format(ApplicationInfo.Culture, "{0} {1}",
                            pair.Value.Format(basics[pair.Key].Value, false),
                            pair.Value.Format(modifiers[pair.Key].Value, true));
                    } else
                        values = pair.Value.Format(basics[pair.Key].Value, false);
                }
                else if (hasModifier)
                    values = pair.Value.Format(modifiers[pair.Key].Value, true);

                // add row only if resource values are defined
                if (values != null)
                    VariableList.Items.Add(new VariableListItem(pair.Value, values));
            }
        }

        #endregion
        #region FormatModifier

        /// <summary>
        /// Formats the specified value as a modifier for the specified <see cref="VariableClass"/>.
        /// </summary>
        /// <param name="variable">
        /// The <see cref="VariableClass"/> that defines the <paramref name="value"/>.</param>
        /// <param name="value">
        /// The instance value of the specified <paramref name="variable"/> to format.</param>
        /// <returns>
        /// An em dash (—) if <paramref name="value"/> is zero; otherwise, the result of <see
        /// cref="VariableClass.Format"/> for the specified <paramref name="variable"/> and
        /// <paramref name="value"/>.</returns>

        private static string FormatModifier(VariableClass variable, int value) {
            return (value == 0 ? "—" : variable.Format(value, true));
        }

        #endregion
        #region UpdateClasses

        /// <summary>
        /// Updates the "Entity Class" <see cref="ListView"/> on the <see cref="ClassesTab"/> page
        /// with the current dialog selections.</summary>
        /// <remarks>
        /// <b>UpdateClasses</b> updates the the "Entity Class" list view and the "Show Placement
        /// Sites" button to reflect the selected <see cref="FactionClass"/> and the selected
        /// "Category" radio button.</remarks>

        private void UpdateClasses() {

            // clear entity classes list view
            ClassList.Items.Clear();
            ShowPlaceButton.IsEnabled = false;

            // quit if no faction instance available
            if (this._faction == null) return;

            // get available classes in selected category
            EntityCategory category = CurrentCategory;
            var availableClasses = this._faction.GetAvailableClasses(category);

            // quit if no available classes
            if (availableClasses == null) return;

            // get buildable classes in selected category
            var buildableClasses = this._faction.GetBuildableClasses(category);

            // show this faction's available classes
            foreach (EntityClass entityClass in availableClasses) {
                bool canBuild = buildableClasses.ContainsKey(entityClass.Id);
                bool canCapture = false;

                UnitClass unitClass = entityClass as UnitClass;
                TerrainClass terrainClass = entityClass as TerrainClass;

                // determine abilities specific to subclasses
                if (unitClass != null)
                    canCapture = unitClass.CanCapture;
                else if (terrainClass != null)
                    canCapture = terrainClass.CanCapture;

                // create ability marker string
                string abilities = String.Format(ApplicationInfo.Culture, "{0}/{1}",
                    (canBuild ? _abbrBuild : "–"),
                    (canCapture ? _abbrCapture : "–"));

                ClassList.Items.Add(new ClassListItem(entityClass, abilities));
            }

            // select first available class by default
            if (ClassList.Items.Count > 0) {
                ClassList.SelectedIndex = 0;

                // enable Show Placements button
                ShowPlaceButton.IsEnabled = true;
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
        /// page of the <see cref="ShowFactions"/> dialog.</remarks>

        private void HelpExecuted(object sender, ExecutedRoutedEventArgs args) {
            args.Handled = true;

            // default to dialog help page
            string helpPage = "DlgShowFactions.html";

            // show help for specific tab page
            if (GeneralTab.IsSelected)
                helpPage = "DlgShowFactionsGeneral.html";
            else if (AssetsTab.IsSelected)
                helpPage = "DlgShowFactionsAssets.html";
            else if (ClassesTab.IsSelected)
                helpPage = "DlgShowFactionsClasses.html";
            else if (VariablesTab.IsSelected)
                helpPage = "DlgShowFactionsVars.html";
            else if (ConditionsTab.IsSelected)
                helpPage = "DlgShowFactionsConditions.html";

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
        /// <remarks><para>
        /// <b>OnAccept</b> calls <see cref="Window.Close"/> on this dialog, and also <see
        /// cref="Window.Activate"/> on its <see cref="Window.Owner"/> if that <see cref="Window"/>
        /// is the <see cref="MainWindow"/>.
        /// </para><para>
        /// This event handler is necessary because the <see cref="Window.Hide"/> and <see
        /// cref="Window.Show"/> calls potentially performed by <see cref="OnShowEntities"/> prevent
        /// the "OK" button from closing the dialog by setting the <see cref="Window.DialogResult"/>
        /// property, and also deactivate the <see cref="MainWindow"/>.</para></remarks>

        private void OnAccept(object sender, RoutedEventArgs args) {
            args.Handled = true;
            Close();

            if (Owner == MainWindow.Instance)
                Owner.Activate();
        }

        #endregion
        #region OnCategoryChecked

        /// <summary>
        /// Handles the <see cref="ToggleButton.Checked"/> event for the "Category" <see
        /// cref="RadioButton"/> controls on the <see cref="ClassesTab"/> page.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="RoutedEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnCategoryChecked</b> updates the "Entity Class" list view to reflect the selected
        /// "Category" radio button.</remarks>

        private void OnCategoryChecked(object sender, RoutedEventArgs args) {
            args.Handled = true;
            UpdateClasses();
        }

        #endregion
        #region OnConditionActivate

        /// <summary>
        /// Handles the <see cref="Control.MouseDoubleClick"/> event for a <see
        /// cref="ListViewItem"/> of the "Condition" <see cref="ListView"/> on the <see
        /// cref="ConditionsTab"/> page.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="MouseButtonEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnConditionActivate</b> displays a <see cref="ShowVariables"/> dialog containing
        /// information on the double-clicked item in the "Conditions" list view if it indicates a
        /// <see cref="VariableClass"/>.</remarks>

        private void OnConditionActivate(object sender, MouseButtonEventArgs args) {
            args.Handled = true;

            // retrieve double-clicked item, if any
            var source = args.OriginalSource as DependencyObject;
            var listItem = ItemsControl.ContainerFromElement(VariableList, source) as ListViewItem;
            if (listItem == null) return;

            // check if parameter is a variable class
            ConditionListItem item = (ConditionListItem) listItem.Content;
            VariableClass variable = item.Parameter as VariableClass;
            if (variable == null) return;

            // show info dialog for variable class
            var dialog = new ShowVariables(variable) { Owner = this };
            dialog.ShowDialog();
        }

        #endregion
        #region OnClassActivate

        /// <summary>
        /// Handles the <see cref="Control.MouseDoubleClick"/> event for a <see
        /// cref="ListViewItem"/> of the "Entity Class" <see cref="ListView"/> on the <see
        /// cref="ClassesTab"/> page.</summary>
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
        #region OnFactionSelected

        /// <summary>
        /// Handles the <see cref="Selector.SelectionChanged"/> event for the "Faction" <see
        /// cref="ComboBox"/>.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="SelectionChangedEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnFactionSelected</b> updates all dialog controls to reflect the selected item in the
        /// "Faction" combo box.</remarks>

        private void OnFactionSelected(object sender, SelectionChangedEventArgs args) {

            // retrieve selected faction, if any
            this._factionClass = FactionCombo.SelectedItem as FactionClass;

            // update dialog caption and color label
            if (this._factionClass == null) {
                Title = this._dialogTitle + Global.Strings.LabelNone;
                FactionColorInfo.Background = Background;
            } else {
                Title = this._dialogTitle + this._factionClass.Name;
                FactionColorInfo.Background = new SolidColorBrush(this._factionClass.Color);
            }

            // get surviving faction instance, if any
            this._worldState.Factions.TryGetValue(this._factionClass.Id, out this._faction);
            bool haveInstance = (this._faction != null);

            // show label indicating surviving or deleted faction
            if (haveInstance) {
                FactionLiveInfo.Visibility = Visibility.Visible;
                FactionDeadInfo.Visibility = Visibility.Collapsed;
            } else {
                FactionLiveInfo.Visibility = Visibility.Collapsed;
                FactionDeadInfo.Visibility = Visibility.Visible;
            }

            // GeneralTab: show informational text for faction
            if (this._factionClass == null)
                FactionInfo.Clear();
            else 
                FactionInfo.Text = String.Join(Environment.NewLine, this._factionClass.Paragraphs);

            // ClassesTab: update Entity Class list view
            UpdateClasses();

            #region AssetsTab

            PointI home = Site.InvalidLocation;
            Site homeSite = null;

            // get current home site, if any
            if (this._factionClass != null) {
                home = this._factionClass.HomeSite;
                homeSite = this._worldState.GetSite(home);
            }

            // update Home Site controls
            HomeSiteInfo.Text = Site.Format(home);
            SelectHomeButton.IsEnabled = (homeSite != null);

            HomeOwnerInfo.Text = (homeSite == null ? "" :
                (homeSite.Owner == null ? Global.Strings.LabelOwnerNone :
                    (homeSite.Owner == this._faction ?
                        Global.Strings.LabelOwnerSame : homeSite.Owner.Name)));

            // should we enable the Show Upgrades button?
            bool haveUpgrades = (MasterSection.Instance.Entities.Upgrades.Count > 0);

            // enable buttons if faction instance available
            ShowUnitsButton.IsEnabled = haveInstance;
            ShowUpgradesButton.IsEnabled = (haveInstance && haveUpgrades);
            ShowTerrainsButton.IsEnabled = haveInstance;

            if (haveInstance) {
                // show faction's share of available sites
                int owned = this._faction.Sites.Count;
                OwnedInfo.Text = owned.ToString("N0", ApplicationInfo.Culture);
                UnownedInfo.Text = (this._siteCount - owned).ToString("N0", ApplicationInfo.Culture);
                double conquest = (this._siteCount > 0 ? owned / (double) this._siteCount : 0.0);
                ConquestInfo.Text = conquest.ToString("P1", ApplicationInfo.Culture);

                // show count of all faction possessions
                UnitsInfo.Text = this._faction.Units.Count.ToString("N0", ApplicationInfo.Culture);
                TerrainsInfo.Text = this._faction.Terrains.Count.ToString("N0", ApplicationInfo.Culture);
                UpgradesInfo.Text = this._faction.Upgrades.Count.ToString("N0", ApplicationInfo.Culture); ;
            } else {
                // faction owns none of the available sites
                OwnedInfo.Text = "—";
                UnownedInfo.Text = this._siteCount.ToString("N0", ApplicationInfo.Culture);
                ConquestInfo.Text = "—";

                // faction owns no possesssions
                UnitsInfo.Text = "—";
                TerrainsInfo.Text = "—";
                UpgradesInfo.Text = "—";
            }

            #endregion
            #region VariablesTab

            // clear faction list views
            VariableList.Items.Clear();
            ModifierList.Items.Clear();
            this._modifiers = null;

            // show resources of selected faction, if any
            if (this._faction != null) {
                CreateResourceRows();
                CreateModifierRows();

                // compute modifiers for resetting and accumulating resources
                this._modifiers = this._faction.ComputeResourceModifiers(true);
                this._modifiers.AddRange(this._faction.ComputeResourceModifiers(false));

                // select first resource by default
                if (VariableList.Items.Count > 0)
                    VariableList.SelectedIndex = 0;
            }

            #endregion
            #region ConditionsTab

            // clear Condition list view
            ConditionList.Items.Clear();

            // show conditions of selected faction, if any
            if (this._faction != null) {
                CreateConditionRows();

                // select first condition by default
                if (ConditionList.Items.Count > 0)
                    ConditionList.SelectedIndex = 0;
            }

            #endregion
        }

        #endregion
        #region OnSelectHome

        /// <summary>
        /// Handles the <see cref="ButtonBase.Click"/> event for the "Select Site" <see
        /// cref="Button"/> on the <see cref="AssetsTab"/> page.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="RoutedEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnSelectHome</b> centers and selects the <see cref="Faction.HomeSite"/> of the
        /// selected <see cref="Faction"/>, if any, on the the associated <see cref="MapView"/>.
        /// </remarks>

        private void OnSelectHome(object sender, RoutedEventArgs args) {
            args.Handled = true;

            if (this._factionClass != null)
                this._mapView.CenterAndSelect(this._factionClass.HomeSite);
        }

        #endregion
        #region OnShowEntities

        /// <summary>
        /// Handles the <see cref="ButtonBase.Click"/> event for the "Show Units", "Show Terrains",
        /// and "Show Upgrades" <see cref="Button"/> controls on the <see cref="AssetsTab"/> page.
        /// </summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="RoutedEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnShowEntities</b> shows a <see cref="ShowEntities"/> dialog with the selected <see
        /// cref="Faction"/> and an initial <see cref="EntityCategory"/> that corresponds to the
        /// clicked button. Any parent dialogs are temporarily hidden so that the default <see
        /// cref="MapView"/> is not obscured.</remarks>

        private void OnShowEntities(object sender, RoutedEventArgs args) {
            args.Handled = true;
            if (this._faction == null) return;

            // determine initial category (default is Unit)
            EntityCategory category = EntityCategory.Unit;
            if (args.Source == ShowTerrainsButton)
                category = EntityCategory.Terrain;
            else if (args.Source == ShowUpgradesButton)
                category = EntityCategory.Upgrade;

            // hide all intermediate dialogs
            var owners = new Stack<Window>();
            Window owner = this;
            while (owner != null && owner != MainWindow.Instance) {
                owner.Hide();
                owners.Push(owner);
                owner = owner.Owner;
            }

            // show read-only dialog with desired initial category
            var dialog = new ShowEntities(this._mapView, this._faction, category, 0);
            dialog.Owner = this;
            dialog.ShowDialog();

            // show all intermediate dialogs
            while (owners.Count > 0) {
                owner = owners.Pop();
                owner.Show();
            }
        }

        #endregion
        #region OnShowPlacements

        /// <summary>
        /// Handles the <see cref="ButtonBase.Click"/> event for the "Show Placement Sites" <see
        /// cref="Button"/> on the <see cref="ClassesTab"/> page.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="RoutedEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnShowPlacements</b> shows a <see cref="ShowPlacements"/> dialog for the selected
        /// <see cref="Faction"/> and <see cref="EntityCategory"/>.</remarks>

        private void OnShowPlacements(object sender, RoutedEventArgs args) {
            args.Handled = true;
            if (this._faction == null) return;

            using (var dialog = new ShowPlacements(
                this._worldState, this._faction, CurrentCategory)) {
                dialog.Owner = this;
                dialog.ShowDialog();
            }
        }

        #endregion
        #region OnVariableActivate

        /// <summary>
        /// Handles the <see cref="Control.MouseDoubleClick"/> event for a <see
        /// cref="ListViewItem"/> of the "Variable" <see cref="ListView"/> on the <see
        /// cref="VariablesTab"/> page.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="MouseButtonEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnVariableActivate</b> displays a <see cref="ShowVariables"/> dialog containing
        /// information on the double-clicked item in the "Variables" list view.</remarks>

        private void OnVariableActivate(object sender, MouseButtonEventArgs args) {
            args.Handled = true;

            // retrieve double-clicked item, if any
            var source = args.OriginalSource as DependencyObject;
            var listItem = ItemsControl.ContainerFromElement(VariableList, source) as ListViewItem;
            if (listItem == null) return;

            // show info dialog for variable class
            VariableListItem item = (VariableListItem) listItem.Content;
            var dialog = new ShowVariables(item.Item1) { Owner = this };
            dialog.ShowDialog();
        }

        #endregion
        #region OnVariableSelected

        /// <summary>
        /// Handles the <see cref="Selector.SelectionChanged"/> event for the "Variable" <see
        /// cref="ListView"/> on the <see cref="VariablesTab"/> page.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="SelectionChangedEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnVariableSelected</b> updates the "Modifier" list view to reflect the selected item
        /// in the "Variable" list view.</remarks>

        private void OnVariableSelected(object sender, SelectionChangedEventArgs args) {

            // retrieve selected variable class, if any
            VariableClass variable = null;
            int index = VariableList.SelectedIndex;
            if (index >= 0) {
                var item = (VariableListItem) VariableList.Items[index];
                variable = item.Item1;
            }

            // initialize all counters to dashes
            string[] values = new string[7] { "—", "—", "—", "—", "—", "", "—" };
            CategorizedValue value;

            // show categorized modifiers to selected variable, if any
            if (variable != null && this._modifiers.TryGetValue(variable.Id, out value)) {
                values[0] = FormatModifier(variable, value.Unit);
                values[1] = FormatModifier(variable, value.Terrain);
                values[2] = FormatModifier(variable, value.Effect);
                values[3] = FormatModifier(variable, value.Upgrade);
                values[4] = FormatModifier(variable, value.Other);
                values[6] = variable.Format(value.Total, true);
            }

            // copy formatted values to Modifier list view
            for (int i = 0; i < values.Length; i++) {
                ModifierListItem item = (ModifierListItem) ModifierList.Items[i];
                ModifierList.Items[i] = new ModifierListItem(item.Item1, values[i]);
            }
        }

        #endregion
        #region On...WidthChanged

        /// <summary>
        /// Handles changes to the <see cref="FrameworkElement.ActualWidth"/> property of the
        /// "Entity Class" <see cref="ListView"/> on the <see cref="ClassesTab"/> page.</summary>
        /// <param name="sender">
        /// The <see cref="ListView"/> control sending the event.</param>
        /// <param name="args">
        /// An <see cref="EventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnClassWidthChanged</b> resizes the first column of the "Entity Class" list view to
        /// the available list view width.</remarks>

        private void OnClassWidthChanged(object sender, EventArgs args) {

            double width = ClassList.ActualWidth - ClassAbilityColumn.ActualWidth - 28;
            if (width > 0) ClassColumn.Width = width;
        }

        /// <summary>
        /// Handles changes to the <see cref="FrameworkElement.ActualWidth"/> property of the
        /// "Condition" <see cref="ListView"/> on the <see cref="ConditionsTab"/> page.</summary>
        /// <param name="sender">
        /// The <see cref="ListView"/> control sending the event.</param>
        /// <param name="args">
        /// An <see cref="EventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnConditionWidthChanged</b> resizes the first column of the "Condition" list view to
        /// the available list view width.</remarks>

        private void OnConditionWidthChanged(object sender, EventArgs args) {

            double width = ConditionList.ActualWidth - ConditionDefeatColumn.ActualWidth -
                ConditionCurrentColumn.ActualWidth - ConditionVictoryColumn.ActualWidth - 28;

            if (width > 0) ConditionColumn.Width = width;
        }

        /// <summary>
        /// Handles changes to the <see cref="FrameworkElement.ActualWidth"/> property of the
        /// "Variable" <see cref="ListView"/> on the <see cref="VariablesTab"/> page.</summary>
        /// <param name="sender">
        /// The <see cref="ListView"/> control sending the event.</param>
        /// <param name="args">
        /// An <see cref="EventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnVariableWidthChanged</b> resizes the first column of the "Variable" list view to
        /// the available list view width.</remarks>

        private void OnVariableWidthChanged(object sender, EventArgs args) {

            double width = VariableList.ActualWidth - VariableValueColumn.ActualWidth - 28;
            if (width > 0) VariableColumn.Width = width;
        }

        #endregion
        #endregion
        #region Class ConditionListItem

        /// <summary>
        /// Provides the data for a <see cref="ListViewItem"/> in the "Condition" <see
        /// cref="ListView"/> on the <see cref="ConditionsTab"/> page.</summary>

        private class ConditionListItem {
            #region ConditionListItem()

            /// <overloads>
            /// Initializes a new instance of the <see cref="ConditionListItem"/> class.</overloads>
            /// <summary>
            /// Initializes a new instance of the <see cref="ConditionListItem"/> class with default
            /// values.</summary>
            /// <remarks>
            /// The <see cref="Parameter"/> and <see cref="ParameterText"/> properties remain null
            /// references. Use this constructor for empty separator rows.</remarks>

            public ConditionListItem() { }

            #endregion
            #region ConditionListItem(ConditionParameter)

            /// <summary>
            /// Initializes a new instance of the <see cref="ConditionListItem"/> class with the
            /// specified <see cref="ConditionParameter"/>.</summary>
            /// <param name="parameter">
            /// The initial value for the <see cref="Parameter"/> and <see cref="ParameterText"/>
            /// properties.</param>
            /// <exception cref="InvalidEnumArgumentException">
            /// <paramref name="parameter"/> is not a valid <see cref="ConditionParameter"/> value.
            /// </exception>

            public ConditionListItem(ConditionParameter parameter) {
                Parameter = parameter;
                ParameterText = Condition.GetParameterString(parameter);
            }

            #endregion
            #region ConditionListItem(VariableClass)

            /// <summary>
            /// Initializes a new instance of the <see cref="ConditionListItem"/> class with the
            /// specified <see cref="VariableClass"/>.</summary>
            /// <param name="variable">
            /// The initial value for the <see cref="Parameter"/> and <see cref="ParameterText"/>
            /// properties.</param>
            /// <exception cref="ArgumentNullException">
            /// <paramref name="variable"/> is a null reference.</exception>

            public ConditionListItem(VariableClass variable) {
                if (variable == null)
                    ThrowHelper.ThrowArgumentNullException("variable");

                Parameter = variable;
                ParameterText = variable.Name;
            }

            #endregion
            #region Parameter

            /// <summary>
            /// Gets the <see cref="ConditionParameter"/> value or <see cref="VariableClass"/>
            /// object that is associated with the <see cref="ConditionListItem"/>.</summary>
            /// <value>
            /// The <see cref="ConditionParameter"/> value or <see cref="VariableClass"/> object
            /// supplied to the constructor, if any; otherwise, a null reference.</value>

            public object Parameter { get; private set; }

            #endregion
            #region ParameterText

            /// <summary>
            /// Gets a <see cref="String"/> that represents the associated <see cref="Parameter"/>.
            /// </summary>
            /// <value><para>
            /// The <see cref="Condition.GetParameterString"/> result for a <see cref="Parameter"/>
            /// that is a <see cref="ConditionParameter"/> value.
            /// </para><para>-or-</para><para>
            /// The <see cref="VariableClass.Name"/> of a <see cref="Parameter"/> that is a <see
            /// cref="VariableClass"/> object.
            /// </para><para>-or-</para><para>
            /// A null reference if <see cref="Parameter"/> is a null reference.</para></value>

            public string ParameterText { get; private set; }

            #endregion

            public string Current { get; set; }
            public string Defeat { get; set; }
            public string Victory { get; set; }
        }

        #endregion
    }
}
