using System;
using System.Collections.Generic;
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
using Hexkit.Players;
using Hexkit.Scenario;

namespace Hexkit.Editor.Dialog {
    #region Type Aliases

    using NumericUpDown = System.Windows.Forms.NumericUpDown;
    using PropertyGrid = System.Windows.Forms.PropertyGrid;
    using PropertySort = System.Windows.Forms.PropertySort;
    using PropertyValueChangedEventArgs = System.Windows.Forms.PropertyValueChangedEventArgs;

    using ConditionList = KeyedList<ConditionParameter, Hexkit.Scenario.Condition>;
    using EntityTemplateList = ListEx<EntityTemplate>;
    using IdentifierList = ListEx<String>;
    using VariableValueDictionary = SortedListEx<String, Int32>;

    // avoid confusion with System.Windows.Condition
    using Condition = Hexkit.Scenario.Condition;

    // Supply tab: resource ID and supply check box
    using SupplyListItem = Tuple<String, Boolean>;

    // Classes tab: entity class ID and build check box
    using ClassListItem = Tuple<String, Boolean>;

    // Entities tab: entity class ID and entity template
    using EntityListItem = Tuple<String, EntityTemplate>;

    #endregion

    /// <summary>
    /// Shows a dialog allowing the user to change a <see cref="FactionClass"/>.</summary>
    /// <remarks>
    /// Please refer to the "Change Faction" page of the "Editor Dialogs" section in the application
    /// help file for details on this dialog.</remarks>

    public partial class ChangeFaction: Window {
        #region ChangeFaction(FactionClass)

        /// <summary>
        /// Initializes a new instance of the <see cref="ChangeFaction"/> class with the specified
        /// <see cref="FactionClass"/>.</summary>
        /// <param name="faction">
        /// The <see cref="FactionClass"/> whose data to change.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="faction"/> is a null reference.</exception>
        /// <remarks>
        /// The data of the specified <paramref name="faction"/> may be changed in the dialog, as
        /// indicated by the value of the <see cref="DataChanged"/> property.</remarks>

        public ChangeFaction(FactionClass faction) {
            if (faction == null)
                ThrowHelper.ThrowArgumentNullException("faction");

            this._faction = faction;

            InitializeComponent();
            Title += faction.Id;

            #region TextTab

            // show faction name and optional information
            NameBox.Text = faction.Name;
            DetailBox.Text = String.Join(Environment.NewLine, faction.Paragraphs);

            #endregion
            #region VariablesTab

            // create shallow copies of faction variables
            this._factionCounters[0] = new VariableValueDictionary(faction.Counters);
            this._factionResources[0] = new VariableValueDictionary(faction.Resources);
            this._factionResources[1] = new VariableValueDictionary(faction.ResourceModifiers);

            // set range for variable value control
            VariableUpDown.Minimum = VariableClass.AbsoluteMinimum;
            VariableUpDown.Maximum = VariableClass.AbsoluteMaximum;

            // adjust column width of Variable list view
            DependencyPropertyDescriptor.FromProperty(
                ListView.ActualWidthProperty, typeof(ListView))
                .AddValueChanged(VariableList, OnVariableWidthChanged);

            // adjust column width of Available Variable list view
            DependencyPropertyDescriptor.FromProperty(
                ListView.ActualWidthProperty, typeof(ListView))
                .AddValueChanged(AvailableVariableList, OnAvailableVariableWidthChanged);

            #endregion
            #region SupplyTab

            // add scenario resources and check faction supply resources
            foreach (string id in MasterSection.Instance.Variables.Resources.Keys) {
                bool isChecked = faction.SupplyResources.Contains(id);
                SupplyListItem item = new SupplyListItem(id, isChecked);
                SupplyList.Items.Add(item);
            }

            // adjust column width of Supply list view
            DependencyPropertyDescriptor.FromProperty(
                ListView.ActualWidthProperty, typeof(ListView))
                .AddValueChanged(SupplyList, OnSupplyWidthChanged);

            // select first item if present
            if (SupplyList.Items.Count > 0)
                SupplyList.SelectedIndex = 0;

            #endregion
            #region ClassesTab

            // create copies of faction class identifiers
            this._buildableUnits = new IdentifierList(faction.BuildableUnits.Keys);
            this._buildableTerrains = new IdentifierList(faction.BuildableTerrains.Keys);
            this._buildableUpgrades = new IdentifierList(faction.BuildableUpgrades.Keys);

            // adjust column width of Class list view
            DependencyPropertyDescriptor.FromProperty(
                ListView.ActualWidthProperty, typeof(ListView))
                .AddValueChanged(ClassList, OnClassWidthChanged);

            #endregion
            #region EntitiesTab

            // create shallow copies of faction entities
            this._factionUnits = new EntityTemplateList(faction.Units);
            this._factionTerrains = new EntityTemplateList(faction.Terrains);
            this._factionUpgrades = new EntityTemplateList(faction.Upgrades);

            // adjust column width of Entity list view
            DependencyPropertyDescriptor.FromProperty(
                ListView.ActualWidthProperty, typeof(ListView))
                .AddValueChanged(EntityList, OnEntityWidthChanged);

            // adjust column width of Available Entity list view
            DependencyPropertyDescriptor.FromProperty(
                ListView.ActualWidthProperty, typeof(ListView))
                .AddValueChanged(AvailableEntityList, OnAvailableEntityWidthChanged);

            #endregion
            #region ConditionsTab

            // gather controls in "Victory Conditions" group box
            this._victoryConditions = new ConditionControls[] {
                new ConditionControls(ConditionParameter.Sites, VictorySitesToggle, VictorySitesUpDown),
                new ConditionControls(ConditionParameter.Units, VictoryUnitsToggle, VictoryUnitsUpDown),
                new ConditionControls(ConditionParameter.UnitStrength, VictoryUnitStrengthToggle, VictoryUnitStrengthUpDown),
                new ConditionControls(ConditionParameter.Turns, VictoryTurnsToggle, VictoryTurnsUpDown)
            };

            // gather controls in "Defeat Conditions" group box
            this._defeatConditions = new ConditionControls[] {
                new ConditionControls(ConditionParameter.Sites, DefeatSitesToggle, DefeatSitesUpDown),
                new ConditionControls(ConditionParameter.Units, DefeatUnitsToggle, DefeatUnitsUpDown),
                new ConditionControls(ConditionParameter.UnitStrength, DefeatUnitStrengthToggle, DefeatUnitStrengthUpDown),
                new ConditionControls(ConditionParameter.Turns, DefeatTurnsToggle, DefeatTurnsUpDown)
            };

            // read victory conditions into controls
            foreach (ConditionControls conditionControls in this._victoryConditions)
                conditionControls.Load(faction.VictoryConditions);

            // read defeat conditions into controls
            foreach (ConditionControls conditionControls in this._defeatConditions)
                conditionControls.Load(faction.DefeatConditions);

            #endregion
            #region PlayerTab

            // adjust display for Options grid
            OptionsGrid.PropertySort = PropertySort.Alphabetical;
            OptionsGrid.ToolbarVisible = false;

            // add "(none)" to Algorithm combo box
            AlgorithmCombo.Items.Add(Global.Strings.LabelNone);

            string algorithmId = null;
            int algorithmIndex = -1;

            // add all algorithms to Algorithm combo box
            foreach (Algorithm algorithm in PlayerManager.Instance.Algorithms) {
                int index = AlgorithmCombo.Items.Add(algorithm);
                if (algorithm.Id == faction.Computer.Algorithm) {
                    algorithmId = algorithm.Id;
                    algorithmIndex = index;
                }
            }

            if (!faction.Computer.IsValid || algorithmIndex < 0) {
                // show "(none)" in combo box
                AlgorithmCombo.SelectedIndex = 0;
            } else {
                // select algorithm in combo box
                AlgorithmCombo.SelectedIndex = algorithmIndex;

                // import faction settings
                this._factionOptions = AlgorithmOptions.Create(algorithmId);
                this._factionOptions.Load(faction.Computer.Options);

                // update options display
                OptionsGrid.SelectedObject = this._factionOptions;
            }

            #endregion

            // construction completed
            this._initialized = true;

            // Variables tab: show resources by default
            ResourceToggle.IsChecked = true;

            // Classes tab: show unit classes by default
            UnitClassToggle.IsChecked = true;

            // Entities tab: show units by default
            UnitToggle.IsChecked = true;
        }

        #endregion
        #region Private Fields

        // construction parameters
        private readonly FactionClass _faction;

        // was construction completed?
        private readonly bool _initialized = false;

        #region VariablesTab

        // current variables (basic & modifier values)
        private VariableValueDictionary[] _currentVariables;

        /// <summary>
        /// An array containing the faction's current <see cref="FactionClass.Counters"/> in the
        /// first index position.</summary>

        private readonly VariableValueDictionary[]
            _factionCounters = new VariableValueDictionary[2];

        /// <summary>
        /// An array containing the faction's current <see cref="FactionClass.Resources"/> and <see
        /// cref="FactionClass.ResourceModifiers"/>.</summary>

        private readonly VariableValueDictionary[]
            _factionResources = new VariableValueDictionary[2];

        #endregion
        #region ClassesTab

        // current entity classes
        private IdentifierList _currentClasses;

        /// <summary>
        /// A list containing the keys of the faction's current <see
        /// cref="FactionClass.BuildableTerrains"/>.</summary>

        private readonly IdentifierList _buildableTerrains;

        /// <summary>
        /// A list containing the keys of the faction's current <see
        /// cref="FactionClass.BuildableUnits"/>.</summary>

        private readonly IdentifierList _buildableUnits;

        /// <summary>
        /// A list containing the keys of the faction's current <see
        /// cref="FactionClass.BuildableUpgrades"/>.</summary>

        private readonly IdentifierList _buildableUpgrades;

        #endregion
        #region EntitiesTab

        // current entities
        private EntityTemplateList _currentEntities;

        /// <summary>
        /// A list containing the faction's current <see cref="FactionClass.Terrains"/>.</summary>

        private readonly EntityTemplateList _factionTerrains;

        /// <summary>
        /// A list containing the faction's current <see cref="FactionClass.Units"/>.</summary>

        private readonly EntityTemplateList _factionUnits;

        /// <summary>
        /// A list containing the faction's current <see cref="FactionClass.Upgrades"/>.</summary>

        private readonly EntityTemplateList _factionUpgrades;

        #endregion
        #region ConditionsTab

        /// <summary>
        /// An array of <see cref="ConditionControls"/> for the "Victory Conditions" <see
        /// cref="GroupBox"/>.</summary>

        private readonly ConditionControls[] _victoryConditions;

        /// <summary>
        /// An array of <see cref="ConditionControls"/> for the "Defeat Conditions" <see
        /// cref="GroupBox"/>.</summary>

        private readonly ConditionControls[] _defeatConditions;

        #endregion
        #region PlayerTab

        /// <summary>
        /// The faction's current customized <see cref="AlgorithmOptions"/>.</summary>

        private AlgorithmOptions _factionOptions;

        #endregion
        #endregion
        #region DataChanged

        /// <summary>
        /// Gets a value indicating whether any data was changed in the dialog.</summary>
        /// <value>
        /// <c>true</c> if the <see cref="FactionClass"/> supplied to the constructor have been
        /// modified; otherwise, <c>false</c>.</value>
        /// <remarks>
        /// <b>DataChanged</b> returns <c>false</c> if no detectable changes were made. However, the
        /// original data may have been overwritten with a copy that is not detectably different,
        /// namely if the user clicked <b>OK</b> without making any changes.</remarks>

        public bool DataChanged { get; private set; }

        #endregion
        #region Private Methods
        #region AddEntity

        /// <summary>
        /// Adds a new item to the "Entity" <see cref="ListView"/> on the <see cref="EntitiesTab"/>
        /// page, based on the specified <see cref="EntityClass"/>.</summary>
        /// <param name="entity">
        /// The <see cref="EntityClass"/> to add.</param>
        /// <remarks>
        /// <b>AddEntity</b> adds the specified <paramref name="entity"/> to the "Entity" list view
        /// and sets the <see cref="DataChanged"/> flag.</remarks>

        private void AddEntity(EntityClass entity) {
            if (entity == null) return;

            // create new entity template with selected ID
            EntityTemplate template = new EntityTemplate(entity.Category);
            template.EntityClass = entity.Id;

            // create & select associated list view item
            EntityListItem item = CreateEntityItem(template);
            EntityList.SelectAndShow(item);

            // broadcast data changes
            UpdateEntitiesTab();
            DataChanged = true;
        }

        #endregion
        #region AddVariable

        /// <summary>
        /// Adds a new item to the "Variable" <see cref="ListView"/> on the <see
        /// cref="VariablesTab"/> page, based on the specified <see cref="VariableClass"/>.
        /// </summary>
        /// <param name="variable">
        /// The <see cref="VariableClass"/> to add.</param>
        /// <param name="isModifier">
        /// <c>true</c> to add a modifier value for the specified <paramref name="variable"/>;
        /// <c>false</c> to add a basic value.</param>
        /// <remarks>
        /// <b>AddVariable</b> adds the specified <paramref name="variable"/> to the "Variable" list
        /// view and sets the <see cref="DataChanged"/> flag. The variable category depends on the
        /// selected "Category" radio button and the specified <paramref name="isModifier"/> flag.
        /// </remarks>

        private void AddVariable(VariableClass variable, bool isModifier) {
            if (variable == null) return;

            // determine desired target collection
            var variables = this._currentVariables[isModifier ? 1 : 0];
            if (variables == null) return;

            // check if this entry already exists
            if (variables.ContainsKey(variable.Id)) {

                // abort, can't add same item twice
                MessageBox.Show(this, Global.Strings.DialogVariableDuplicate,
                    Global.Strings.TitleVariableDuplicate,
                    MessageBoxButton.OK, MessageBoxImage.Information);

                return;
            }

            // create & select new variable with default value
            variables.Add(variable.Id, 0);
            VariableListItem item = CreateVariableItem(variable.Id, isModifier);
            VariableList.SelectAndShow(item);

            // broadcast data changes
            UpdateVariablesTab(false);
            DataChanged = true;
        }

        #endregion
        #region ChangeEntity

        /// <summary>
        /// Allows the user to change the <see cref="EntityListItem"/> at the specified index in the
        /// "Entity" <see cref="ListView"/> on the <see cref="EntitiesTab"/> page.</summary>
        /// <param name="index">
        /// The index of the <see cref="EntityListItem"/> to change.</param>
        /// <remarks>
        /// <b>ChangeEntity</b> shows the <see cref="ChangeTemplate"/> dialog with the item at the
        /// specified <paramref name="index"/> in the "Entity" list view, and sets the <see
        /// cref="DataChanged"/> flag if the user made any changes.</remarks>

        private void ChangeEntity(int index) {
            if (index < 0) return;
            EntityListItem item = (EntityListItem) EntityList.Items[index];
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
                item = new EntityListItem(GetEntityText(template), template);
                EntityList.Items[index] = item;
                EntityList.SelectAndShow(index);

                UpdateEntitiesTab();
                DataChanged = true;
            }
        }

        #endregion
        #region CreateEntityItem

        /// <summary>
        /// Creates a new item for the "Entity" <see cref="ListView"/> on the <see
        /// cref="EntitiesTab"/> page, containing the specified <see cref="EntityTemplate"/>.
        /// </summary>
        /// <param name="template">
        /// The <see cref="EntityTemplate"/> to add to the <see cref="ListView"/>.</param>
        /// <returns>
        /// The newly created <see cref="EntityListItem"/> for the specified <paramref
        /// name="template"/> that was added to the "Entity" <see cref="ListView"/>.</returns>
        /// <remarks><para>
        /// <b>CreateEntityItem</b> inserts the new <see cref="EntityListItem"/> in alphabetical
        /// order among the existing list view items.
        /// </para><para>
        /// The <see cref="EntityListItem.Item2"/> component of the returned <see
        /// cref="EntityListItem"/> contains the specified <paramref name="template"/>.
        /// </para></remarks>

        private EntityListItem CreateEntityItem(EntityTemplate template) {
            EntityListItem item = new EntityListItem(GetEntityText(template), template);

            // sort entities alphabetically
            for (int i = EntityList.Items.Count - 1; i >= 0; i--) {
                EntityListItem cursor = (EntityListItem) EntityList.Items[i];
                if (item.Item1.CompareOrdinal(cursor.Item1) > 0) {
                    EntityList.Items.Insert(i + 1, item);
                    return item;
                }
            }

            EntityList.Items.Insert(0, item);
            return item;
        }

        #endregion
        #region CreateVariableItem

        /// <summary>
        /// Creates a new item for the "Variables" <see cref="ListView"/> on the <see
        /// cref="VariablesTab"/> page, containing the specified <see cref="VariableClass"/>
        /// identifier and associated basic or modifier value.</summary>
        /// <param name="id">
        /// The <see cref="VariableClass.Id"/> string of a <see cref="VariableClass"/>.</param>
        /// <param name="isModifier">
        /// <c>true</c> to format the <see cref="VariableClass"/> value associated with <paramref
        /// name="id"/> as a modifier value; <c>false</c> to format as a basic value. </param>
        /// <returns>
        /// The new <see cref="VariableListItem"/> if one was created and added to the "Variables"
        /// <see cref="ListView"/>; otherwise, a null reference.</returns>
        /// <remarks>
        /// <b>CreateVariableItem</b> immediately returns a null reference if the specified
        /// <paramref name="id"/> is not found in the currently selected <see
        /// cref="VariableValueDictionary"/> for the specified <paramref name="isModifier"/> flag.
        /// </remarks>

        private VariableListItem CreateVariableItem(string id, bool isModifier) {

            // get variable dictionary, if any
            var variables = this._currentVariables[isModifier ? 1 : 0];
            if (variables == null) return null;

            // get index of identifier if present
            if (variables == null) return null;
            int index = variables.IndexOfKey(id);
            if (index < 0) return null;

            // format variable as basic or modifier value
            int value = variables.GetByIndex(index);
            string formatValue = VariableClass.FormatUnscaled(value, isModifier);

            // store ID and modifier target, if any
            ModifierTarget? target = null;
            if (isModifier) target = ModifierTarget.Self;
            var newItem = new VariableListItem(id, formatValue, target);

            ItemCollection items = VariableList.Items;
            for (int i = 0; i < items.Count; i++) {
                VariableListItem item = (VariableListItem) items[i];

                // sort alphabetically, with basic values before modifiers
                if ((item.Id == newItem.Id && !isModifier) ||
                    String.CompareOrdinal(item.Id, newItem.Id) > 0) {
                    items.Insert(i, newItem);
                    return newItem;
                }
            }

            // append to end of list
            items.Add(newItem);
            return newItem;
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
        #region UpdateEntitiesTab

        /// <summary>
        /// Updates all controls on the <see cref="EntitiesTab"/> page.</summary>
        /// <remarks>
        /// <b>UpdateEntitiesTab</b> reloads the list of faction entities from the current contents
        /// of the <see cref="EntitiesTab"/> page, and enables or disables any related controls to
        /// reflect the current list data.</remarks>

        private void UpdateEntitiesTab() {

            // reload entity list from list view
            this._currentEntities.Clear();
            foreach (EntityListItem item in EntityList.Items)
                this._currentEntities.Add(item.Item2);

            // enable or disable list manipulation buttons
            AddEntityButton.IsEnabled = (AvailableEntityList.Items.Count > 0);
            RemoveEntityButton.IsEnabled = (EntityList.Items.Count > 0);
        }

        #endregion
        #region UpdateVariablesTab

        /// <summary>
        /// Updates all controls on the <see cref="VariablesTab"/> page.</summary>
        /// <param name="select">
        /// <c>true</c> to select the first item in each <see cref="ListView"/>; <c>false</c> to
        /// leave any existing selections unchanged.</param>
        /// <remarks>
        /// <b>UpdateVariablesTab</b> enables or disables all controls on the <see
        /// cref="VariablesTab"/> page to reflect the current list view data. This method must be
        /// called whenever the contents of a list view change.</remarks>

        private void UpdateVariablesTab(bool select) {

            // update controls for available variables
            if (AvailableVariableList.Items.Count > 0) {
                if (select) AvailableVariableList.SelectedIndex = 0;
                AddVariableBasicButton.IsEnabled = true;
                AddVariableModifyButton.IsEnabled = (this._currentVariables[1] != null);
            } else {
                AddVariableBasicButton.IsEnabled = false;
                AddVariableModifyButton.IsEnabled = false;
            }

            // update controls for faction variables
            if (VariableList.Items.Count > 0) {
                if (select) VariableList.SelectedIndex = 0;
                RemoveVariableButton.IsEnabled = true;
                VariableUpDown.Enabled = true;
            } else {
                VariableList.SelectedIndex = -1;
                RemoveVariableButton.IsEnabled = false;
                VariableUpDown.Enabled = false;
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
        /// page of the <see cref="ChangeFaction"/> dialog.</remarks>

        private void HelpExecuted(object sender, ExecutedRoutedEventArgs args) {
            args.Handled = true;

            // default to dialog help page
            string helpPage = "DlgChangeFaction.html";

            // show help for specific tab page
            if (TextTab.IsSelected)
                helpPage = "DlgChangeFactionText.html";
            else if (VariablesTab.IsSelected)
                helpPage = "DlgChangeFactionVars.html";
            else if (SupplyTab.IsSelected)
                helpPage = "DlgChangeFactionSupply.html";
            else if (ClassesTab.IsSelected)
                helpPage = "DlgChangeFactionClasses.html";
            else if (EntitiesTab.IsSelected)
                helpPage = "DlgChangeFactionEntities.html";
            else if (ConditionsTab.IsSelected)
                helpPage = "DlgChangeFactionConditions.html";
            else if (PlayerTab.IsSelected)
                helpPage = "DlgChangeFactionPlayer.html";

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
        #region OnAlgorithmSelected

        /// <summary>
        /// Handles the <see cref="Selector.SelectionChanged"/> event for the "Algorithm" <see
        /// cref="ComboBox"/> on the <see cref="PlayerTab"/> page.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="SelectionChangedEventArgs"/> object containing event data.</param>
        /// <remarks><para>
        /// <b>OnAlgorithmSelected</b> creates a new <see cref="AlgorithmOptions"/> object for the
        /// selected item in the "Algorithm" combo box, or clears the current object if no item is
        /// selected. The "Options" property grid is updated accordingly.
        /// </para><para>
        /// <b>OnAlgorithmSelected</b> sets the <see cref="DataChanged"/> flag if the new algorithm
        /// differs from the <see cref="FactionClass.Computer"/> settings of the <see
        /// cref="FactionClass"/> being edited.</para></remarks>

        private void OnAlgorithmSelected(object sender, SelectionChangedEventArgs args) {
            args.Handled = true;
            if (!this._initialized) return;

            // check if any algorithm selected
            if (AlgorithmCombo.SelectedIndex <= 0)
                this._factionOptions = null;
            else {
                // create default options for selected algorithm
                Algorithm algorithm = (Algorithm) AlgorithmCombo.SelectedItem;
                this._factionOptions = AlgorithmOptions.Create(algorithm.Id);
            }

            // update options display
            OptionsGrid.SelectedObject = this._factionOptions;

            // broadcast data changes, if any
            CustomComputer computer = this._faction.Computer;
            if ((this._factionOptions == null && computer.IsValid) ||
                (this._factionOptions != null && computer.Algorithm != this._factionOptions.Algorithm.Id))
                DataChanged = true;
        }

        #endregion
        #region OnClassCategory

        /// <summary>
        /// Handles the <see cref="ToggleButton.Checked"/> event for the "Category" <see
        /// cref="RadioButton"/> controls on the <see cref="ClassesTab"/> page.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="RoutedEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnClassCategory</b> sets the current <see cref="EntityCategory"/> to the value
        /// indicated by the selected "Category" radio button, and updates all controls on the <see
        /// cref="ClassesTab"/> page to reflect the current category.</remarks>

        private void OnClassCategory(object sender, RoutedEventArgs args) {
            args.Handled = true;
            if (!this._initialized) return;

            // switch category (default to Unit)
            IList<String> entityKeys;
            if (UnitClassToggle.IsChecked == true) {
                entityKeys = MasterSection.Instance.Entities.Units.Keys;
                this._currentClasses = this._buildableUnits;
            }
            else if (TerrainClassToggle.IsChecked == true) {
                entityKeys = MasterSection.Instance.Entities.Terrains.Keys;
                this._currentClasses = this._buildableTerrains;
            }
            else if (UpgradeClassToggle.IsChecked == true) {
                entityKeys = MasterSection.Instance.Entities.Upgrades.Keys;
                this._currentClasses = this._buildableUpgrades;
            }
            else {
                Debug.Fail("ChangeFaction.OnClassCategory: No Category button checked.");
                entityKeys = MasterSection.Instance.Entities.Units.Keys;
                this._currentClasses = this._buildableUnits;
            }

            // add entity classes and check buildable classes
            ClassList.Items.Clear();
            foreach (string id in entityKeys) {
                bool isChecked = this._currentClasses.Contains(id);
                ClassList.Items.Add(new ClassListItem(id, isChecked));
            }

            // select first entity class if present
            if (ClassList.Items.Count > 0)
                ClassList.SelectedIndex = 0;
        }

        #endregion
        #region OnClassChanged

        /// <summary>
        /// Handles the <see cref="ButtonBase.Click"/> event for the "Entity Classes" <see
        /// cref="ListView"/> on the <see cref="ClassesTab"/> page.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="RoutedEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnClassChanged</b> updates the "Entity Classes" list view and sets the <see
        /// cref="DataChanged"/> flag if any changes were made.</remarks>

        private void OnClassChanged(object sender, RoutedEventArgs args) {
            args.Handled = true;
            if (!this._initialized) return;

            // retrieve checked entity class, if any
            var listItem = TreeHelper.FindParentListItem(args.Source);
            if (listItem == null) return;
            ClassListItem item = (ClassListItem) listItem.Content;
            int index = ClassList.Items.IndexOf(item);

            // get old and new buildable state
            bool oldState = this._currentClasses.Contains(item.Item1);
            bool newState = !item.Item2;

            // add or remove entity class as desired
            if (oldState != newState) {
                if (newState)
                    this._currentClasses.Add(item.Item1);
                else
                    this._currentClasses.Remove(item.Item1);

                // broadcast data changes
                item = new ClassListItem(item.Item1, newState);
                ClassList.Items[index] = item;
                ClassList.SelectAndShow(index);

                DataChanged = true;
            }
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
        /// cref="FactionClass"/> object supplied to the constructor, and sets the <see
        /// cref="DataChanged"/> flag is any object properties were changed.</para></remarks>

        protected override void OnClosing(CancelEventArgs args) {
            base.OnClosing(args);
            if (args.Cancel) return;

            // user cancelled dialog, ignore changes
            if (DialogResult != true) {
                DataChanged = false;
                return;
            }

            // check variable values for errors
            bool variablesValid =
                EditorUtility.ValidateVariables(this, this._factionCounters[0]) &&
                EditorUtility.ValidateVariables(this, this._factionResources[0]) &&
                EditorUtility.ValidateVariables(this, this._factionResources[1]);

            if (!variablesValid) {
                args.Cancel = true;
                return;
            }

            #region TextTab

            // read name box into name property
            this._faction.Name = NameBox.Text;

            // read detail paragraphs into string collection
            this._faction.Paragraphs.Clear();
            this._faction.Paragraphs.AddRange(DetailBox.Text.Split(
                new string[] { Environment.NewLine }, StringSplitOptions.None));

            #endregion
            #region VariablesTab

            // read variable values into faction collections
            this._faction.Counters.Clear();
            foreach (var pair in this._factionCounters[0])
                this._faction.Counters.Add(pair.Key, pair.Value);

            this._faction.Resources.Clear();
            foreach (var pair in this._factionResources[0])
                this._faction.Resources.Add(pair.Key, pair.Value);

            this._faction.ResourceModifiers.Clear();
            foreach (var pair in this._factionResources[1])
                this._faction.ResourceModifiers.Add(pair.Key, pair.Value);

            #endregion
            #region SupplyTab

            // read supply resources into string collection
            this._faction.SupplyResources.Clear();
            foreach (SupplyListItem item in SupplyList.Items)
                if (item.Item2) this._faction.SupplyResources.Add(item.Item1);

            #endregion
            #region ClassesTab

            // read buildable entity classes into collections
            // (null values will be corrected by validation)
            this._faction.BuildableUnits.Clear();
            foreach (string id in this._buildableUnits)
                this._faction.BuildableUnits.Add(id, null);

            this._faction.BuildableTerrains.Clear();
            foreach (string id in this._buildableTerrains)
                this._faction.BuildableTerrains.Add(id, null);

            this._faction.BuildableUpgrades.Clear();
            foreach (string id in this._buildableUpgrades)
                this._faction.BuildableUpgrades.Add(id, null);

            #endregion
            #region EntitiesTab

            // read owned entities into collections
            this._faction.Units.Clear();
            this._faction.Units.AddRange(this._factionUnits);

            this._faction.Terrains.Clear();
            this._faction.Terrains.AddRange(this._factionTerrains);

            this._faction.Upgrades.Clear();
            this._faction.Upgrades.AddRange(this._factionUpgrades);

            #endregion
            #region ConditionsTab

            // read victory conditions into VictoryConditions property
            var conditions = new ConditionList();
            foreach (ConditionControls conditionControls in this._victoryConditions)
                conditionControls.Save(conditions);

            if (!conditions.Equals(this._faction.VictoryConditions)) {
                DataChanged = true;
                this._faction.VictoryConditions.Clear();
                this._faction.VictoryConditions.AddRange(conditions);
            }

            // read defeat conditions into DefeatConditions property
            conditions.Clear();
            foreach (ConditionControls conditionControls in this._defeatConditions)
                conditionControls.Save(conditions);

            if (!conditions.Equals(this._faction.DefeatConditions)) {
                DataChanged = true;
                this._faction.DefeatConditions.Clear();
                this._faction.DefeatConditions.AddRange(conditions);
            }

            #endregion
            #region PlayerTab

            // read computer settings into Computer property
            if (this._factionOptions == null)
                this._faction.Computer.Clear();
            else {
                this._faction.Computer.Algorithm = this._factionOptions.Algorithm.Id;
                this._factionOptions.Save(this._faction.Computer.Options);
            }

            #endregion
        }

        #endregion
        #region OnAvailableEntityActivate

        /// <summary>
        /// Handles the <see cref="Control.MouseDoubleClick"/> event for a <see
        /// cref="ListViewItem"/> of the "Available Entities" <see cref="ListView"/> on the <see
        /// cref="EntitiesTab"/> page.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="MouseButtonEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnAvailableEntityActivate</b> calls <see cref="AddEntity"/> with the double-clicked
        /// item in the "Available Entities" list view.</remarks>

        private void OnAvailableEntityActivate(object sender, MouseButtonEventArgs args) {
            args.Handled = true;

            // retrieve double-clicked item, if any
            var source = args.OriginalSource as DependencyObject;
            var item = ItemsControl.ContainerFromElement(AvailableEntityList, source) as ListViewItem;
            if (item != null) AddEntity(item.Content as EntityClass);
        }

        #endregion
        #region OnEntityActivate

        /// <summary>
        /// Handles the <see cref="Control.MouseDoubleClick"/> event for a <see
        /// cref="ListViewItem"/> of the "Entities" <see cref="ListView"/> on the <see
        /// cref="EntitiesTab"/> page.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="MouseButtonEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnEntityActivate</b> calls <see cref="ChangeEntity"/> with the double-clicked item in
        /// the "Entities" list view.</remarks>

        private void OnEntityActivate(object sender, MouseButtonEventArgs args) {
            args.Handled = true;

            // retrieve double-clicked item, if any
            var source = args.OriginalSource as DependencyObject;
            var item = ItemsControl.ContainerFromElement(EntityList, source) as ListViewItem;
            if (item != null) ChangeEntity(EntityList.Items.IndexOf(item.Content));
        }

        #endregion
        #region OnEntityAdd

        /// <summary>
        /// Handles the <see cref="ButtonBase.Click"/> event for the "Add Entity" <see
        /// cref="Button"/> on the <see cref="EntitiesTab"/> page.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="RoutedEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnEntityAdd</b> calls <see cref="AddEntity"/> with the first selected item in the
        /// "Available Entities" list view.</remarks>

        private void OnEntityAdd(object sender, RoutedEventArgs args) {
            args.Handled = true;

            // retrieve selected entity class, if any
            EntityClass entity = AvailableEntityList.SelectedItem as EntityClass;
            AddEntity(entity);
        }

        #endregion
        #region OnEntityCategory

        /// <summary>
        /// Handles the <see cref="ToggleButton.Checked"/> event for the "Category" <see
        /// cref="RadioButton"/> controls on the <see cref="EntitiesTab"/> page.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="RoutedEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnEntityCategory</b> sets the current <see cref="EntityCategory"/> to the value 
        /// indicated by the selected "Category" radio button, and updates all controls on the <see
        /// cref="EntitiesTab"/> page to reflect the current category.</remarks>

        private void OnEntityCategory(object sender, RoutedEventArgs args) {
            args.Handled = true;
            if (!this._initialized) return;

            // switch category (default to Unit)
            IList<EntityClass> entities;
            if (UnitToggle.IsChecked == true) {
                entities = MasterSection.Instance.Entities.Units.Values;
                this._currentEntities = this._factionUnits;
            }
            else if (TerrainToggle.IsChecked == true) {
                entities = MasterSection.Instance.Entities.Terrains.Values;
                this._currentEntities = this._factionTerrains;
            }
            else if (UpgradeToggle.IsChecked == true) {
                entities = MasterSection.Instance.Entities.Upgrades.Values;
                this._currentEntities = this._factionUpgrades;
            }
            else {
                Debug.Fail("ChangeFaction.OnEntityCategory: No Category button checked.");
                entities = MasterSection.Instance.Entities.Units.Values;
                this._currentEntities = this._factionUnits;
            }

            // show available scenario entity classes
            AvailableEntityList.ItemsSource = entities;

            // add entity templates to faction list
            EntityList.Items.Clear();
            foreach (EntityTemplate template in this._currentEntities)
                CreateEntityItem(template);

            // select first entity class if present
            if (AvailableEntityList.Items.Count > 0) {
                AvailableEntityList.SelectedIndex = 0;
                AddEntityButton.IsEnabled = true;
            } else
                AddEntityButton.IsEnabled = false;

            // select first entity template if present
            if (EntityList.Items.Count > 0) {
                EntityList.SelectedIndex = 0;
                RemoveEntityButton.IsEnabled = true;
            } else {
                EntityList.SelectedIndex = -1;
                ChangeEntityButton.IsEnabled = false;
                RemoveEntityButton.IsEnabled = false;
            }
        }

        #endregion
        #region OnEntityChange

        /// <summary>
        /// Handles the <see cref="ButtonBase.Click"/> event for the "Change Entity" <see
        /// cref="Button"/> on the <see cref="EntitiesTab"/> page.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="RoutedEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnEntityChange</b> calls <see cref="ChangeEntity"/> with the first selected item in
        /// the "Entity" list view.</remarks>

        private void OnEntityChange(object sender, RoutedEventArgs args) {
            args.Handled = true;
            ChangeEntity(EntityList.SelectedIndex);
        }

        #endregion
        #region OnEntityRemove

        /// <summary>
        /// Handles the <see cref="ButtonBase.Click"/> event for the "Remove Entity" <see
        /// cref="Button"/> on the <see cref="EntitiesTab"/> page.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="RoutedEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnEntityRemove</b> removes the first selected item from the "Entity" list view and
        /// sets the <see cref="DataChanged"/> flag.</remarks>

        private void OnEntityRemove(object sender, RoutedEventArgs args) {
            args.Handled = true;

            // retrieve selected entity, if any
            int index = EntityList.SelectedIndex;
            if (index < 0) return;

            // remove entity from list view
            EntityList.Items.RemoveAt(index);

            // select item in the same position
            if (EntityList.Items.Count > 0)
                EntityList.SelectAndShow(Math.Min(EntityList.Items.Count - 1, index));

            // broadcast data changes
            UpdateEntitiesTab();
            DataChanged = true;
        }

        #endregion
        #region OnEntitySelected

        /// <summary>
        /// Handles the <see cref="Selector.SelectionChanged"/> event for the "Entities" <see
        /// cref="ListView"/> on the <see cref="EntitiesTab"/> page.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="SelectionChangedEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnEntitySelected</b> enables or disable the "Change Entity" button, depending on
        /// whether an item is selected in the "Variables" list view.</remarks>

        private void OnEntitySelected(object sender, SelectionChangedEventArgs args) {
            args.Handled = true;
            ChangeEntityButton.IsEnabled = (EntityList.SelectedItems.Count > 0);
        }

        #endregion
        #region OnOptionsChanged

        /// <summary>
        /// Handles the <see cref="PropertyGrid.PropertyValueChanged"/> event for the "Options" <see
        /// cref="PropertyGrid"/> on the <see cref="PlayerTab"/> page.</summary>
        /// <param name="sender">
        /// The <see cref="PropertyGrid"/> control sending the event.</param>
        /// <param name="args">
        /// A <see cref="PropertyValueChangedEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnOptionsChanged</b> sets the property of the current <see cref="AlgorithmOptions"/>
        /// object that is indicated by the specified <paramref name="args"/> to the indicated
        /// value, and sets the <see cref="DataChanged"/> flag.</remarks>

        private void OnOptionsChanged(object sender, PropertyValueChangedEventArgs args) {
            if (!this._initialized) return;

            // acquire new property value
            args.ChangedItem.PropertyDescriptor.SetValue(
                this._factionOptions, args.ChangedItem.Value);

            // broadcast data changes
            DataChanged = true;
        }

        #endregion
        #region OnSupplyChanged

        /// <summary>
        /// Handles the <see cref="ButtonBase.Click"/> event for the "Supply Resources" <see
        /// cref="ListView"/> on the <see cref="SupplyTab"/> page.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="RoutedEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnSupplyChanged</b> updates the "Supply Resources" list view and sets the <see
        /// cref="DataChanged"/> flag if any changes were made.</remarks>

        private void OnSupplyChanged(object sender, RoutedEventArgs args) {
            args.Handled = true;
            if (!this._initialized) return;

            // retrieve checked supply resource, if any
            var listItem = TreeHelper.FindParentListItem(args.Source);
            if (listItem == null) return;
            SupplyListItem item = (SupplyListItem) listItem.Content;
            int index = SupplyList.Items.IndexOf(item);

            // get old and new supply state
            bool oldState = (this._faction.SupplyResources.Contains(item.Item1));
            bool newState = !item.Item2;

            // broadcast data changes, if any
            if (oldState != newState) {
                item = new SupplyListItem(item.Item1, newState);
                SupplyList.Items[index] = item;
                SupplyList.SelectAndShow(index);

                DataChanged = true;
            }
        }

        #endregion
        #region OnTextChanged

        /// <summary>
        /// Handles the <see cref="TextBoxBase.TextChanged"/> event for the "Faction Name" and
        /// "Informational Text" <see cref="TextBox"/> controls on the <see cref="TextTab"/> page.
        /// </summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="TextChangedEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnTextChanged</b> sets the <see cref="DataChanged"/> flag if the <see
        /// cref="TextChangedEventArgs.Changes"/> collection of the specified <paramref
        /// name="args"/> is not empty.</remarks>

        private void OnTextChanged(object sender, TextChangedEventArgs args) {
            args.Handled = true;
            if (this._initialized && args.Changes.Count > 0)
                DataChanged = true;
        }

        #endregion
        #region OnAvailableVariableActivate

        /// <summary>
        /// Handles the <see cref="Control.MouseDoubleClick"/> event for a <see
        /// cref="ListViewItem"/> of the "Available Variables" <see cref="ListView"/> on the <see
        /// cref="VariablesTab"/> page.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="MouseButtonEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnAvailableVariableActivate</b> calls <see cref="AddVariable"/> with the
        /// double-clicked item in the "Available Variables" list view, added as a basic value.
        /// </remarks>

        private void OnAvailableVariableActivate(object sender, MouseButtonEventArgs args) {
            args.Handled = true;

            // retrieve double-clicked item, if any
            var source = args.OriginalSource as DependencyObject;
            var item = ItemsControl.ContainerFromElement(AvailableVariableList, source) as ListViewItem;
            if (item != null) AddVariable(item.Content as VariableClass, false);
        }

        #endregion
        #region OnVariableAdd

        /// <summary>
        /// Handles the <see cref="ButtonBase.Click"/> event for the "Add Basic Value" and "Add
        /// Modifier" <see cref="Button"/> controls on the <see cref="VariablesTab"/> page.
        /// </summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="RoutedEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnVariableAdd</b> calls <see cref="AddVariable"/> with the first selected item in the
        /// "Available Variables" list view. Either a basic value or a modifier value is added,
        /// depending on which button was clicked.</remarks>

        private void OnVariableAdd(object sender, RoutedEventArgs args) {
            args.Handled = true;

            // retrieve selected variable entry, if any
            VariableClass variable = AvailableVariableList.SelectedItem as VariableClass;
            bool isModifier = (args.Source == AddVariableModifyButton);
            AddVariable(variable, isModifier);
        }

        #endregion
        #region OnVariableCategory

        /// <summary>
        /// Handles the <see cref="ToggleButton.Checked"/> event for the "Category" <see
        /// cref="RadioButton"/> controls on the <see cref="VariablesTab"/> page.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="RoutedEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnVariableCategory</b> sets the current variable category and value collection the to
        /// values indicated by the selected "Category" radio button, and updates all controls on
        /// the <see cref="VariablesTab"/> page to reflect the current category.</remarks>

        private void OnVariableCategory(object sender, RoutedEventArgs args) {
            args.Handled = true;
            if (!this._initialized) return;

            // switch category (default to Resource)
            IList<VariableClass> variables;
            if (CounterToggle.IsChecked == true) {
                variables = MasterSection.Instance.Variables.Counters.Values;
                this._currentVariables = this._factionCounters;
            }
            else if (ResourceToggle.IsChecked == true) {
                variables = MasterSection.Instance.Variables.Resources.Values;
                this._currentVariables = this._factionResources;
            }
            else {
                Debug.Fail("ChangeFaction.OnVariableCategory: No Category button checked.");
                variables = MasterSection.Instance.Variables.Resources.Values;
                this._currentVariables = this._factionResources;
            }

            // show available scenario variables
            AvailableVariableList.ItemsSource = variables;

            // add faction values if defined
            VariableList.Items.Clear();
            foreach (VariableClass variable in variables) {
                CreateVariableItem(variable.Id, false);
                CreateVariableItem(variable.Id, true);
            }

            // enable or disable controls
            UpdateVariablesTab(true);
        }

        #endregion
        #region OnVariableChanged

        /// <summary>
        /// Handles the <see cref="NumericUpDown.ValueChanged"/> event for the "Variable Value" <see
        /// cref="NumericUpDown"/> control on the <see cref="VariablesTab"/> page.</summary>
        /// <param name="sender">
        /// The <see cref="NumericUpDown"/> control sending the event.</param>
        /// <param name="args">
        /// An <see cref="EventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnVariableChanged</b> updates the "Value" column of the first selected item in the
        /// "Variables" list view and sets the <see cref="DataChanged"/> flag.</remarks>

        private void OnVariableChanged(object sender, EventArgs args) {
            if (!this._initialized) return;

            // retrieve selected variable entry, if any
            int index = VariableList.SelectedIndex;
            if (index < 0) return;
            var item = (VariableListItem) VariableList.Items[index];

            // extract decimal value and modifier flag
            bool isModifier;
            decimal oldValue = VariableClass.ParseUnscaled(item.Value, out isModifier);

            // check if variable value has actually changed
            if (VariableUpDown.Value == oldValue) return;
            int value = (int) VariableUpDown.Value;

            // update variable value in collection
            var variables = this._currentVariables[isModifier ? 1 : 0];
            variables[item.Id] = value;

            // update variable value in list view
            item.Value = VariableClass.FormatUnscaled(value, isModifier);
            VariableList.Items.Refresh();
            VariableList.SelectAndShow(index);

            DataChanged = true;
        }

        #endregion
        #region OnVariableRemove

        /// <summary>
        /// Handles the <see cref="ButtonBase.Click"/> event for the "Remove Variable" <see
        /// cref="Button"/> on the <see cref="VariablesTab"/> page.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="RoutedEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnVariableRemove</b> removes the first selected item from the "Variables" list view
        /// and sets the <see cref="DataChanged"/> flag.</remarks>

        private void OnVariableRemove(object sender, RoutedEventArgs args) {
            args.Handled = true;

            // retrieve selected variable entry, if any
            int index = VariableList.SelectedIndex;
            if (index < 0) return;
            var item = (VariableListItem) VariableList.Items[index];

            // remove variable from list view and collection
            VariableList.Items.RemoveAt(index);
            var variables = this._currentVariables[item.Target == null ? 0 : 1];
            variables.Remove(item.Id);

            // select item in the same position
            if (VariableList.Items.Count > 0)
                VariableList.SelectAndShow(Math.Min(VariableList.Items.Count - 1, index));

            // broadcast data changes
            UpdateVariablesTab(false);
            DataChanged = true;
        }

        #endregion
        #region OnVariableSelected

        /// <summary>
        /// Handles the <see cref="Selector.SelectionChanged"/> event for the "Variables" <see
        /// cref="ListView"/> on the <see cref="VariablesTab"/> page.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="SelectionChangedEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnVariableSelected</b> updates the "Variable Value" control with the data of the
        /// first selected item in the "Variables" list view.</remarks>

        private void OnVariableSelected(object sender, SelectionChangedEventArgs args) {
            args.Handled = true;
            if (!this._initialized) return;

            // retrieve selected variable entry, if any
            int index = VariableList.SelectedIndex;
            if (index < 0)
                VariableUpDown.Value = 0m;
            else {
                // update numeric control with variable value
                var item = (VariableListItem) VariableList.Items[index];
                VariableUpDown.Value = VariableClass.ParseUnscaled(item.Value);
            }
        }

        #endregion
        #region On...WidthChanged

        /// <summary>
        /// Handles changes to the <see cref="FrameworkElement.ActualWidth"/> property of any <see
        /// cref="ListView"/> on any tab page.</summary>
        /// <param name="sender">
        /// The <see cref="ListView"/> control sending the event.</param>
        /// <param name="args">
        /// An <see cref="EventArgs"/> object containing event data.</param>
        /// <remarks>
        /// The <b>On...WidthChanged</b> methods resize the columns of all list views to the current
        /// or available list view width.</remarks>

        private void OnClassWidthChanged(object sender, EventArgs args) {
            double width = ClassList.ActualWidth - 28;
            if (width > 0) ClassColumn.Width = width;
        }

        private void OnSupplyWidthChanged(object sender, EventArgs args) {
            double width = SupplyList.ActualWidth - 28;
            if (width > 0) SupplyColumn.Width = width;
        }

        private void OnEntityWidthChanged(object sender, EventArgs args) {
            double width = EntityList.ActualWidth - 28;
            if (width > 0) EntityColumn.Width = width;
        }

        private void OnAvailableEntityWidthChanged(object sender, EventArgs args) {
            double width = AvailableEntityList.ActualWidth - 28;
            if (width > 0) AvailableEntityColumn.Width = width;
        }

        private void OnVariableWidthChanged(object sender, EventArgs args) {
            double width = VariableList.ActualWidth - VariableValueColumn.ActualWidth - 28;
            if (width > 0) VariableColumn.Width = width;
        }

        private void OnAvailableVariableWidthChanged(object sender, EventArgs args) {
            double width = AvailableVariableList.ActualWidth - 28;
            if (width > 0) AvailableVariableColumn.Width = width;
        }

        #endregion
        #endregion
        #region Class ConditionControls

        /// <summary>
        /// Provides editing controls for a specific <see cref="Condition"/> on the <see
        /// cref="ConditionsTab"/> page.</summary>
        /// <remarks>
        /// <b>ConditionControls</b> manages the <see cref="CheckBox"/> and <see
        /// cref="NumericUpDownEx"/> controls used to edit a <see cref="Condition"/>, and also
        /// provides methods to convert between control states and <see cref="Condition"/> data.
        /// </remarks>

        private class ConditionControls {
            #region ConditionsControls(...)

            /// <summary>
            /// Initializes a new instance of the <see cref="ConditionControls"/> class.</summary>
            /// <param name="parameter">
            /// The initial value for the <see cref="Parameter"/> property.</param>
            /// <param name="checkBox">
            /// The associated <see cref="CheckBox"/> control.</param>
            /// <param name="numericUpDown">
            /// The <see cref="NumericUpDownHost"/> control hosting the associated <see
            /// cref="NumericUpDownEx"/> control.</param>

            public ConditionControls(ConditionParameter parameter,
                CheckBox checkBox, NumericUpDownHost numericUpDown) {

                this._parameter = parameter;
                this._checkBox = checkBox;
                this._numericUpDown = numericUpDown;
            }

            #endregion
            #region Private Fields

            // construction arguments
            private readonly ConditionParameter _parameter;
            private readonly CheckBox _checkBox;
            private readonly NumericUpDownHost _numericUpDown;

            #endregion
            #region Parameter

            /// <summary>
            /// Gets the associated <see cref="ConditionParameter"/>.</summary>
            /// <value>
            /// The <see cref="ConditionParameter"/> edited by the <see cref="ConditionControls"/>.
            /// </value>

            public ConditionParameter Parameter {
                [DebuggerStepThrough]
                get { return this._parameter; }
            }

            #endregion
            #region Load

            /// <summary>
            /// Loads matching data from the specified collection into the <see
            /// cref="ConditionControls"/>.</summary>
            /// <param name="conditions">
            /// The <see cref="ConditionList"/> to search for a matching <see cref="Condition"/>.
            /// </param>

            public void Load(ConditionList conditions) {
                Condition condition;
                if (conditions.TryGetValue(Parameter, out condition)) {
                    this._checkBox.IsChecked = true;
                    this._numericUpDown.Value = condition.Threshold;
                }
            }

            #endregion
            #region Save

            /// <summary>
            /// Saves the data of the <see cref="ConditionControls"/> to the specified collection.
            /// </summary>
            /// <param name="conditions">
            /// The <see cref="ConditionList"/> to receive a new <see cref="Condition"/> with the
            /// associated data.</param>
            /// <remarks>
            /// <b>Save</b> does not check for existing <paramref name="conditions"/> elements with
            /// the same <see cref="Parameter"/>.</remarks>

            public void Save(ConditionList conditions) {
                if (this._checkBox.IsChecked == true) {
                    Condition condition = new Condition(Parameter, (int) this._numericUpDown.Value);
                    conditions.Add(condition);
                }
            }

            #endregion
        }

        #endregion
    }
}
