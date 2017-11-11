using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

using Tektosyne;
using Tektosyne.Collections;

using Hexkit.Global;
using Hexkit.Graphics;
using Hexkit.Scenario;
using Hexkit.World;

namespace Hexkit.Game {
    #region Type Aliases

    using VariableClassDictionary = SortedListEx<String, VariableClass>;
    using VariableList = KeyedList<String, Variable>;
    using VariableModifierDictionary = SortedListEx<String, VariableModifier>;
    using VariableValueDictionary = SortedListEx<String, Int32>;

    #endregion

    /// <summary>
    /// Provides a <see cref="ListView"/> control that shows <see cref="Entity"/> and <see
    /// cref="EntityClass"/> properties.</summary>
    /// <remarks><para>
    /// <b>PropertyListView</b> provides a <see cref="ListView"/> control showing all abilities,
    /// attributes, and resources of the specified <see cref="Entity"/> or <see
    /// cref="EntityClass"/>. Items are stored as <see cref="PropertyListItem"/> instances.
    /// </para><para>
    /// Clicking on a variable row will display a <see cref="Dialog.ShowVariables"/> dialog
    /// containing information on the selected variable.
    /// </para><para>
    /// Clicking on an abbreviated <see cref="Unit"/> ability row will display a message box with a
    /// brief description of all available abilities.</para></remarks>

    public partial class PropertyListView: ListView {
        #region PropertyListView()

        /// <summary>
        /// Initializes a new instance of the <see cref="PropertyListView"/> class.</summary>

        public PropertyListView() {
            InitializeComponent();
            ApplicationUtility.ApplyDefaultStyle(this);

            // hide Category column by default
            ((GridView) View).Columns.Remove(PropertyCategoryColumn);

            // adjust column width of Property list view
            DependencyPropertyDescriptor.FromProperty(
                ListView.ActualWidthProperty, typeof(ListView))
                .AddValueChanged(this, OnPropertyWidthChanged);
        }

        #endregion
        #region Private Fields

        // property backers
        private bool _isCategoryVisible;

        // abbreviations for unit abilities
        private readonly static string _abbrAttack = Global.Strings.AbbrevAttack;
        private readonly static string _abbrBuild = Global.Strings.AbbrevBuild;
        private readonly static string _abbrCapture = Global.Strings.AbbrevCapture;
        private readonly static string _abbrMove = Global.Strings.AbbrevMove;

        #endregion
        #region Private Methods
        #region CreateAbilityRow

        /// <summary>
        /// Adds one ability row with the specified name and value to the "Property" <see
        /// cref="ListView"/>.</summary>
        /// <param name="name">
        /// The <see cref="PropertyListItem.Name"/> of the ability.</param>
        /// <param name="value">
        /// The <see cref="Boolean"/> value of the ability.</param>
        /// <remarks>
        /// <b>CreateAbilityRow</b> creates and adds one <see cref="PropertyListItem"/> with the
        /// specified <paramref name="name"/> and <paramref name="value"/> columns. The <see
        /// cref="PropertyListItem.Category"/> is the localized string "Category", and the <see
        /// cref="PropertyListItem.Tag"/> is a null reference.</remarks>

        private void CreateAbilityRow(string name, bool value) {
            Items.Add(new PropertyListItem(
                Global.Strings.LabelAbility, name, value.ToString(), null));
        }

        #endregion
        #region CreateAbilityRows(Entity)

        /// <overloads>
        /// Adds rows for the abilities of the specified <see cref="Entity"/> or <see
        /// cref="EntityClass"/>.</overloads>
        /// <summary>
        /// Adds one row for all abilities of the specified <see cref="Entity"/>.</summary>
        /// <param name="entity">
        /// The <see cref="Entity"/> whose abilities to show.</param>
        /// <remarks><para>
        /// If the specified <paramref name="entity"/> is of category <see
        /// cref="EntityCategory.Terrain"/>, <b>CreateAbilityRows</b> adds one row indicating that
        /// its site can be captured if the <paramref name="entity"/> has this ability.
        /// </para><para>
        /// If <paramref name="entity"/> is of category <see cref="EntityCategory.Unit"/>,
        /// <b>CreateAbilityRows</b> adds one row whose <see cref="PropertyListItem.Value"/> column
        /// contains either an abbreviation or a dash for each ability flag defined by the <paramref
        /// name="entity"/>, depending on whether the it has the corresponding ability.
        /// </para><para>
        /// The <see cref="PropertyListItem.Tag"/> of each row holds the <see cref="EntityClass"/>
        /// of the specified <paramref name="entity"/>.</para></remarks>

        private void CreateAbilityRows(Entity entity) {
            string abilities = null;
            Unit unit = entity as Unit;
            Terrain terrain = entity as Terrain;

            if (unit != null) {
                abilities = String.Format(ApplicationInfo.Culture, "{0}/{1}/{2}/{3}",
                    (unit.CanAttack ? _abbrAttack : "–"),
                    (unit.CanMove ? _abbrMove : "–"),
                    (unit.CanBuild ? _abbrBuild : "–"),
                    (unit.CanCapture ? _abbrCapture : "–"));
            }
            else if (terrain != null) {
                if (terrain.CanCapture) {
                    abilities = (terrain.CanDestroy ?
                        Global.Strings.LabelCaptureDestroy :
                        Global.Strings.LabelCapturePassive);
                } else if (terrain.CanDestroy)
                    abilities = Global.Strings.LabelDestroyPassive;
            }

            if (abilities != null) {
                // insert separator if required
                if (Items.Count > 0) ApplicationUtility.AddSeparator(this);

                // add single row for any abilities
                Items.Add(new PropertyListItem(Global.Strings.LabelAbility,
                    Global.Strings.LabelAbilities, abilities, entity.EntityClass));
            }
        }

        #endregion
        #region CreateAbilityRows(EntityClass)

        /// <summary>
        /// Adds one row for each ability of the specified <see cref="EntityClass"/>.</summary>
        /// <param name="entityClass">
        /// The <see cref="EntityClass"/> whose abilities to show.</param>
        /// <remarks>
        /// <b>CreateAbilityRows</b> calls <see cref="CreateAbilityRow"/> for each ability of the
        /// specified <paramref name="entityClass"/>, if any.</remarks>

        private void CreateAbilityRows(EntityClass entityClass) {

            // effects & upgrades have no abilities
            if (entityClass.Category == EntityCategory.Effect ||
                entityClass.Category == EntityCategory.Upgrade)
                return;

            // insert separator if required
            if (Items.Count > 0) ApplicationUtility.AddSeparator(this);

            switch (entityClass.Category) {

                case EntityCategory.Unit:
                    UnitClass unitClass = (UnitClass) entityClass;
                    CreateAbilityRow(Global.Strings.LabelCapture, unitClass.CanCapture);
                    break;

                case EntityCategory.Terrain:
                    TerrainClass terrainClass = (TerrainClass) entityClass;
                    CreateAbilityRow(Global.Strings.LabelCapturePassive, terrainClass.CanCapture);
                    CreateAbilityRow(Global.Strings.LabelDestroyPassive, terrainClass.CanDestroy);
                    break;
            }
        }

        #endregion
        #region CreateVariableRows(Entity, VariableCategory)

        /// <overloads>
        /// Adds one row for each variable of the specified <see cref="Entity"/> or <see
        /// cref="EntityClass"/>.</overloads>
        /// <summary>
        /// Adds one row for each variable of the specified <see cref="Entity"/>.</summary>
        /// <param name="entity">
        /// The <see cref="Entity"/> whose variables to show.</param>
        /// <param name="category">
        /// The <see cref="EntityCategory"/> whose variables to show.</param>
        /// <exception cref="InvalidEnumArgumentException">
        /// <paramref name="category"/> is neither <see cref="VariableCategory.Attribute"/> nor <see
        /// cref="VariableCategory.Resource"/>.</exception>
        /// <remarks>
        /// <b>CreateVariableRows</b> adds one row for each basic value and modifier value defined
        /// by the specified <paramref name="entity"/>. The <see cref="PropertyListItem.Tag"/> of
        /// each row holds the corresponding <see cref="VariableClass"/>.</remarks>

        private void CreateVariableRows(Entity entity, VariableCategory category) {

            string categoryLabel = "";
            VariableClassDictionary variables = null;
            VariableList basics = null;
            VariableModifierContainer modifiers = null;

            // retrieve variable collections for category
            if (category == VariableCategory.Attribute) {
                categoryLabel = Global.Strings.LabelAttribute;
                variables = MasterSection.Instance.Variables.Attributes;
                basics = entity.Attributes.Variables;
                modifiers = entity.AttributeModifiers;
            }
            else if (category == VariableCategory.Resource) {
                categoryLabel = Global.Strings.LabelResource;
                variables = MasterSection.Instance.Variables.Resources;
                basics = entity.Resources.Variables;
                modifiers = entity.ResourceModifiers;
            }
            else ThrowHelper.ThrowInvalidEnumArgumentException(
                  "category", (int) category, typeof(VariableCategory));

            Unit unit = entity as Unit;
            int firstIndex = Items.Count;

            // insert separator before first attribute?
            bool addSeparator = (firstIndex > 0);

            // add modifier row, preceded by separator if required
            Action<VariableClass, Int32, ModifierTarget, Int32>
                addRow = (variable, value, target, range) => {

                if (addSeparator) {
                    ApplicationUtility.AddSeparator(this);
                    addSeparator = false;
                    ++firstIndex;
                }

                string column = variable.Format(value, target, range);
                Items.Add(new PropertyListItem(categoryLabel, variable.Name, column, variable));
            };

            // process all variables defined by the scenario
            foreach (var pair in variables) {
                string id = pair.Key;

                // format basic value and self-modifier, if present
                string basicValue = FormatVariable(basics, id);
                string modSelf = FormatVariable(modifiers.Self, id);

                string column = (basicValue == null ? modSelf :
                    (modSelf == null ? basicValue : String.Format(
                        ApplicationInfo.Culture, "{0} {1}", basicValue, modSelf)));

                if (column != null) {
                    // insert separator if required
                    if (addSeparator) {
                        ApplicationUtility.AddSeparator(this);
                        addSeparator = false;
                        ++firstIndex;
                    }

                    var item = new PropertyListItem(categoryLabel, pair.Value.Name, column, pair.Value);

                    // show color bar for partially depleted resources
                    Variable resource;
                    if (basics.TryGetValue(id, out resource) && resource.IsDepletableResource)
                        item.Background = MediaObjects.GetBrush(MediaObjects.DangerFadeBrushes,
                            resource.Value, resource.Minimum, resource.Maximum);

                    // show unit strength resource as first entry
                    if (unit != null && unit.UnitClass.StrengthResource == id)
                        Items.Insert(firstIndex, item);
                    else
                        Items.Add(item);
                }

                // determine which additional modifiers are present
                Variable modOwner, modUnits, modUnitsRanged, modOwnerUnits, modOwnerUnitsRanged;
                modifiers.Units.TryGetValue(id, out modUnits);
                modifiers.UnitsRanged.TryGetValue(id, out modUnitsRanged);
                modifiers.OwnerUnits.TryGetValue(id, out modOwnerUnits);
                modifiers.OwnerUnitsRanged.TryGetValue(id, out modOwnerUnitsRanged);

                int range = entity.EntityClass.ModifierRange;
                if (modifiers.Owner.TryGetValue(id, out modOwner))
                    addRow(pair.Value, modOwner.Value, ModifierTarget.Owner, range);

                // always show Units & UnitsRanged modifiers
                if (modifiers.Units.TryGetValue(id, out modUnits))
                    addRow(pair.Value, modUnits.Value, ModifierTarget.Units, range);
                if (modifiers.UnitsRanged.TryGetValue(id, out modUnitsRanged))
                    addRow(pair.Value, modUnitsRanged.Value, ModifierTarget.UnitsRanged, range);

                // show OwnerUnits modifier only if different from Units modifier
                if (modifiers.OwnerUnits.TryGetValue(id, out modOwnerUnits)) {
                    if (modUnits == null || modUnits.Value != modOwnerUnits.Value)
                        addRow(pair.Value, modOwnerUnits.Value, ModifierTarget.OwnerUnits, range);
                } else if (modUnits != null && modUnits.Value != 0)
                    addRow(pair.Value, 0, ModifierTarget.OwnerUnits, range);

                // show OwnerUnitsRanged modifier only if different from UnitsRanged modifier
                if (modifiers.OwnerUnitsRanged.TryGetValue(id, out modOwnerUnitsRanged)) {
                    if (modUnitsRanged == null || modUnitsRanged.Value != modOwnerUnitsRanged.Value)
                        addRow(pair.Value, modOwnerUnitsRanged.Value, ModifierTarget.OwnerUnitsRanged, range);
                } else if (modUnitsRanged != null && modUnitsRanged.Value != 0)
                    addRow(pair.Value, 0, ModifierTarget.OwnerUnitsRanged, range);
            }
        }

        #endregion
        #region CreateVariableRows(EntityClass, VariableCategory, Boolean)

        /// <summary>
        /// Adds one row for each variable of the specified <see cref="EntityClass"/>.</summary>
        /// <param name="entityClass">
        /// The <see cref="EntityClass"/> whose variables to show.</param>
        /// <param name="category">
        /// The <see cref="VariableCategory"/> whose variables to show.</param>
        /// <param name="showBuildResources">
        /// <c>true</c> to show <see cref="EntityClass.BuildResources"/>, <c>false</c> to show <see
        /// cref="EntityClass.Resources"/>. This argument is ignored if <paramref name="category"/>
        /// does not equal <see cref="VariableCategory.Resource"/>.</param>
        /// <exception cref="InvalidEnumArgumentException">
        /// <paramref name="category"/> is neither <see cref="VariableCategory.Attribute"/> nor <see
        /// cref="VariableCategory.Resource"/>.</exception>
        /// <remarks>
        /// <b>CreateVariableRows</b> adds one row for each initial value and modifier value defined
        /// by the specified <paramref name="entityClass"/>. The <see cref="PropertyListItem.Tag"/>
        /// of each row holds the corresponding <see cref="VariableClass"/>.</remarks>

        private void CreateVariableRows(EntityClass entityClass,
            VariableCategory category, bool showBuildResources) {

            string categoryLabel = "";
            VariableClassDictionary variables = null;
            VariableValueDictionary basics = null;
            VariableModifierDictionary modifiers = null;

            // retrieve name and variable collections for category
            if (category == VariableCategory.Attribute) {
                categoryLabel = Global.Strings.LabelAttribute;
                variables = MasterSection.Instance.Variables.Attributes;
                basics = entityClass.Attributes;
                modifiers = entityClass.AttributeModifiers;
            }
            else if (category == VariableCategory.Resource) {
                variables = MasterSection.Instance.Variables.Resources;

                if (showBuildResources) {
                    categoryLabel = Global.Strings.LabelBuildCost;
                    basics = entityClass.BuildResources;
                } else {
                    categoryLabel = Global.Strings.LabelResource;
                    basics = entityClass.Resources;
                    modifiers = entityClass.ResourceModifiers;
                }
            } else ThrowHelper.ThrowInvalidEnumArgumentException(
                  "category", (int) category, typeof(VariableCategory));

            // insert separator before first property?
            bool addSeparator = (Items.Count > 0);

            // add variable row, preceded by separator if required
            Action<VariableClass, String> addRow = (variable, value) => {
                if (addSeparator) {
                    ApplicationUtility.AddSeparator(this);
                    addSeparator = false;
                }

                Items.Add(new PropertyListItem(categoryLabel, variable.Name, value, variable));
            };

            // process all variables defined by the scenario
            foreach (var pair in variables) {
                string id = pair.Key;
                string basicValue = null, modSelf = null;

                // get basic value if defined
                if (basics.ContainsKey(id))
                    basicValue = pair.Value.Format(basics[id], false);

                // get set of modifiers and self-modifier value
                VariableModifier modifier = null;
                if (modifiers != null && modifiers.TryGetValue(id, out modifier) && modifier.Self != null)
                    modSelf = pair.Value.Format(modifier.Self.Value, true);

                string column = (basicValue == null ? modSelf :
                    (modSelf == null ? basicValue : String.Format(
                        ApplicationInfo.Culture, "{0} {1}", basicValue, modSelf)));

                if (column != null) addRow(pair.Value, column);
                if (modifier == null) continue;

                // add one row for each additional modifier value
                foreach (ModifierTarget target in VariableModifier.AllModifierTargets) {
                    if (target == ModifierTarget.Self) continue;

                    int? value = modifier.GetByTarget(target);
                    if (value != null) {
                        column = pair.Value.Format(value, target, entityClass.ModifierRange);
                        addRow(pair.Value, column);
                    }
                }
            }
        }

        #endregion
        #region FormatVariable

        /// <summary>
        /// Returns a <see cref="String"/> that represents the <see cref="Variable"/> with the
        /// specified identifier in the specified collection.</summary>
        /// <param name="variables">
        /// The <see cref="VariableList"/> to search.</param>
        /// <param name="id">
        /// The <see cref="Variable.Id"/> string of the <see cref="Variable"/> instance to
        /// format.</param>
        /// <returns><para>
        /// The result of <see cref="Variable.ToString"/> for the <paramref name="variables"/>
        /// element with the specified <paramref name="id"/>.
        /// </para><para>-or-</para><para>
        /// A null reference if no such element is found.</para></returns>

        private static string FormatVariable(VariableList variables, string id) {
            int index = variables.IndexOfKey(id);
            return (index < 0 ? null : variables[index].ToString());
        }

        #endregion
        #endregion
        #region IsCategoryVisible

        /// <summary>
        /// Gets or sets a value indicating whether the "Category" column is visible.</summary>
        /// <value>
        /// <c>true</c> if the "Category" column is visible; otherwise, <c>false</c>. The default is
        /// <c>false</c>.</value>
        /// <remarks>
        /// Changing the value of <b>IsCategoryVisible</b> automatically resizes the "Property"
        /// column to fill the available space.</remarks>

        public bool IsCategoryVisible {
            [DebuggerStepThrough]
            get { return this._isCategoryVisible; }
            set {
                this._isCategoryVisible = value;
                var columns = ((GridView) View).Columns;

                if (value) {
                    if (!columns.Contains(PropertyCategoryColumn))
                        columns.Insert(0, PropertyCategoryColumn);
                } else {
                    int index = columns.IndexOf(PropertyCategoryColumn);
                    if (index >= 0) columns.RemoveAt(index);
                }

                OnPropertyWidthChanged(this, EventArgs.Empty);
            }
        }

        #endregion
        #region OnItemActivate

        /// <summary>
        /// Handles the <see cref="Control.MouseDoubleClick"/> event for a <see
        /// cref="ListViewItem"/> of the <see cref="PropertyListView"/>.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="MouseButtonEventArgs"/> object containing event data.</param>
        /// <remarks><para>
        /// <b>OnItemActivate</b> should be called by the actual event handler defined for each
        /// concrete instance of <see cref="PropertyListView"/>.
        /// </para><para>
        /// If the current <see cref="MasterSection"/> instance is not a null reference,
        /// <b>OnItemActivate</b> shows a <see cref="Dialog.ShowVariables"/> dialog containing
        /// information on the double-clicked item, if any.</para></remarks>

        public void OnItemActivate(object sender, MouseButtonEventArgs args) {
            args.Handled = true;

            // quit if no current scenario instance
            if (MasterSection.Instance == null) return;

            // retrieve double-clicked item, if any
            var source = args.OriginalSource as DependencyObject;
            var listItem = ItemsControl.ContainerFromElement(this, source) as ListViewItem;
            if (listItem == null) return;

            // retrieve supported content, if any
            var item = listItem.Content as PropertyListItem;
            if (item == null) return;

            // show info dialog for unit abilities
            if (item.Tag is UnitClass) {
                MessageBox.Show(Window.GetWindow(this),
                    Global.Strings.DialogUnitAbilites, Global.Strings.TitleAbilities,
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // show info dialog for this variable
            VariableClass variable = item.Tag as VariableClass;
            if (variable != null) {
                var dialog = new Dialog.ShowVariables(variable);
                dialog.Owner = Window.GetWindow(this);
                dialog.ShowDialog();
            }
        }

        #endregion
        #region OnPropertyWidthChanged

        /// <summary>
        /// Handles changes to the <see cref="FrameworkElement.ActualWidth"/> property of the <see
        /// cref="PropertyListView"/>.</summary>
        /// <param name="sender">
        /// The <see cref="ListView"/> control sending the event.</param>
        /// <param name="args">
        /// An <see cref="EventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnPropertyWidthChanged</b> resizes the "Property" and "Value" columns of the <see
        /// cref="PropertyListView"/> to share the available list view width.</remarks>

        private void OnPropertyWidthChanged(object sender, EventArgs args) {

            double width = ActualWidth - 28;
            if (IsCategoryVisible) width -= PropertyCategoryColumn.ActualWidth;

            if (width > 0) {
                width /= 2.0;
                PropertyColumn.Width = width;
                PropertyValueColumn.Width = width;
            }
        }

        #endregion
        #region ShowEntity

        /// <summary>
        /// Shows the properties of the specified <see cref="Entity"/>.</summary>
        /// <param name="entity">
        /// The <see cref="Entity"/> whose properties to show.</param>
        /// <remarks><para>
        /// <b>ShowEntity</b> merely clears the contents of the <see cref="PropertyListView"/> if
        /// the specified <paramref name="entity"/> or the current <see cref="MasterSection"/>
        /// instance is a null reference.
        /// </para><para>
        /// Otherwise, <b>ShowEntity</b> adds one row for each ability, resource, and attribute (in
        /// that order) defined by the specified <paramref name="entity"/>.</para></remarks>

        public void ShowEntity(Entity entity) {
            Items.Clear();

            // just clear display if nothing to show
            if (MasterSection.Instance == null || entity == null)
                return;

            // show entity abilities
            CreateAbilityRows(entity);

            // show entity variables
            CreateVariableRows(entity, VariableCategory.Resource);
            CreateVariableRows(entity, VariableCategory.Attribute);
        }

        #endregion
        #region ShowEntityClass

        /// <summary>
        /// Shows the properties of the specified <see cref="EntityClass"/>.</summary>
        /// <param name="entityClass">
        /// The <see cref="EntityClass"/> whose properties to show.</param>
        /// <remarks><para>
        /// <b>ShowEntityClass</b> merely clears the contents of the <see cref="PropertyListView"/>
        /// if the specified <paramref name="entityClass"/> or the current <see
        /// cref="MasterSection"/> instance is a null reference.
        /// </para><para>
        /// Otherwise, <b>ShowEntityClass</b> adds one row for each attribute, instance resource,
        /// build resource, and ability (in that order) defined by the specified <paramref
        /// name="entityClass"/>.</para></remarks>

        public void ShowEntityClass(EntityClass entityClass) {
            Items.Clear();

            // just clear display if nothing to show
            if (MasterSection.Instance == null || entityClass == null)
                return;

            // show entity class variables
            CreateVariableRows(entityClass, VariableCategory.Attribute, false);
            CreateVariableRows(entityClass, VariableCategory.Resource, false);
            CreateVariableRows(entityClass, VariableCategory.Resource, true);

            // show entity class abilities
            CreateAbilityRows(entityClass);
        }

        #endregion
    }
}
