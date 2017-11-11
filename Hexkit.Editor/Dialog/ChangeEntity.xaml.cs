using System;
using System.Collections;
using System.Collections.Generic;
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

using Hexkit.Global;
using Hexkit.Graphics;
using Hexkit.Scenario;

namespace Hexkit.Editor.Dialog {
    #region Type Aliases

    using NumericUpDown = System.Windows.Forms.NumericUpDown;
    using VariableModifierDictionary = SortedListEx<String, VariableModifier>;
    using VariableValueDictionary = SortedListEx<String, Int32>;

    // Image tab: image ID and image stack entry
    using ImageListItem = KeyValuePair<String, ImageStackEntry>;

    #endregion

    /// <summary>
    /// Shows a dialog allowing the user to change an <see cref="EntityClass"/>.</summary>
    /// <remarks>
    /// Please refer to the "Change Entity Class" page of the "Editor Dialogs" section in the
    /// application help file for details on this dialog.</remarks>

    public partial class ChangeEntity: Window {
        #region ChangeEntity(EntityClass)

        /// <summary>
        /// Initializes a new instance of the <see cref="ChangeEntity"/> class with the specified
        /// <see cref="EntityClass"/>.</summary>
        /// <param name="entity">
        /// The <see cref="EntityClass"/> whose data to change.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="entity"/> is a null reference.</exception>
        /// <exception cref="PropertyValueException">
        /// The current <see cref="ImageSection"/> contains an empty <see
        /// cref="ImageSection.Collection"/>.</exception>
        /// <remarks>
        /// The data of the specified <paramref name="entity"/> may be changed in the dialog, as
        /// indicated by the value of the <see cref="DataChanged"/> property.</remarks>

        public ChangeEntity(EntityClass entity) {
            if (entity == null)
                ThrowHelper.ThrowArgumentNullException("entity");

            // require non-empty Collection for simplicity
            if (MasterSection.Instance.Images.Collection.Count == 0)
                ThrowHelper.ThrowPropertyValueException(
                    "MasterSection.Instance.Images.Collection", Tektosyne.Strings.PropertyEmpty);

            this._entity = entity;

            InitializeComponent();
            Title += entity.Id;

            #region TextTab

            // show entity name and optional information
            NameBox.Text = entity.Name;
            DetailBox.Text = String.Join(Environment.NewLine, entity.Paragraphs);

            #endregion
            #region ImagesTab

            // Available Image list will be shown in separate window
            ((Grid) ImagesTab.Content).Children.Remove(AvailableImageList);

            // initialize stack control buttons
            AddImageButton.ShowSymbol(Symbols.BoxEmpty);
            RemoveImageButton.ShowSymbol(Symbols.BoxCrossed);
            MoveImageUpButton.ShowSymbol(Symbols.ArrowUp);
            MoveImageDownButton.ShowSymbol(Symbols.ArrowDown);
            FindImageButton.ShowSymbol(Symbols.ArrowRight);

            // show images defined by scenario
            RegularPolygon polygon = MasterSection.Instance.Areas.MapGrid.Element;
            AvailableImageList.Polygon = polygon;
            foreach (EntityImage image in MasterSection.Instance.Images.Collection.Values)
                AvailableImageList.Insert(new ImageListBoxItem(image));

            // show current image stack entries
            foreach (ImageStackEntry entry in entity.ImageStack) {
                string id = entry.Image.Key;
                var clone = new ImageStackEntry(entry); // clone for editing
                ImageList.Items.Add(new ImageListItem(id, clone));
            }

            // adjust column width of Image list view
            DependencyPropertyDescriptor.FromProperty(
                ListView.ActualWidthProperty, typeof(ListView))
                .AddValueChanged(ImageList, OnImageWidthChanged);

            // show preview of image stack
            StackPreview.Polygon = polygon;
            StackPreview.Show(entity.ImageStack);

            #endregion
            #region VariablesTab

            // create shallow copies of entity variables
            this._entityAttributes = new VariableValueDictionary(entity.Attributes);
            this._entityAttributeModifiers = new VariableModifierDictionary(entity.AttributeModifiers);
            this._entityCounters = new VariableValueDictionary(entity.Counters);
            this._entityResources = new VariableValueDictionary(entity.Resources);
            this._entityResourceModifiers = new VariableModifierDictionary(entity.ResourceModifiers);
            this._entityBuildResources = new VariableValueDictionary(entity.BuildResources);

            // register access key for combo box without label
            AccessKeyManager.Register("M", AddVariableModifierCombo);

            // add targets to Add Modifier combo box
            foreach (ModifierTarget target in VariableModifier.AllModifierTargets)
                AddVariableModifierCombo.Items.Add(target);

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
            #region AbilitiesTab

            // show only abilities & options for our entity category
            string categoryName = entity.Category.ToString();

            foreach (Control control in ((Grid) AbilitiesGroup.Content).Children)
                if (control.Name.EndsWith(categoryName, StringComparison.Ordinal))
                    control.Visibility = Visibility.Visible;

            foreach (Control control in ((Grid) OptionsGroup.Content).Children)
                if (!control.Name.EndsWith(categoryName, StringComparison.Ordinal))
                    control.Visibility = Visibility.Hidden;

            // show available Resource Transfer options
            foreach (ResourceTransferMode mode in Enum.GetValues(typeof(ResourceTransferMode)))
                TransferCombo.Items.Add(mode);

            BlocksAttackToggle.IsChecked = entity.BlocksAttack;
            TransferCombo.SelectedItem = entity.ResourceTransfer;

            // check which entity category we're editing
            TerrainClass terrainClass = entity as TerrainClass;
            UnitClass unitClass = entity as UnitClass;

            // scenario attributes & resources for various options
            IList<String> attributes = MasterSection.Instance.Variables.Attributes.Keys;
            IList<String> resources = MasterSection.Instance.Variables.Resources.Keys;

            if (terrainClass != null) {
                // show terrain abilities
                CaptureToggleTerrain.IsChecked = terrainClass.CanCapture;
                DestroyToggleTerrain.IsChecked = terrainClass.CanDestroy;
                BackgroundToggleTerrain.IsChecked = terrainClass.IsBackground;

                // show terrain options
                InitComboBox(DifficultyComboTerrain, attributes, terrainClass.DifficultyAttribute);
                InitComboBox(ElevationComboTerrain, attributes, terrainClass.ElevationAttribute);
            }
            else if (unitClass != null) {
                // show unit abilities
                CaptureToggleUnit.IsChecked = unitClass.CanCapture;
                DefendToggleUnit.IsChecked = unitClass.CanDefendOnly;
                HealingToggleUnit.IsChecked = unitClass.CanHeal;

                // add Ranged Attack values and select current value
                foreach (TargetMode mode in EntityClass.AllTargetModes)
                    RangedAttackComboUnit.Items.Add(mode);
                RangedAttackComboUnit.SelectedItem = unitClass.RangedAttack;

                // show other unit options
                InitComboBox(AttackRangeComboUnit, attributes, unitClass.AttackRangeAttribute);
                InitComboBox(MovementComboUnit, attributes, unitClass.MovementAttribute);
                InitComboBox(MoraleComboUnit, resources, unitClass.MoraleResource);
                InitComboBox(StrengthComboUnit, resources, unitClass.StrengthResource);
            }
            else {
                // other categories have no options
                OptionsInfo.Visibility = Visibility.Visible;
            }

            #endregion
            #region OtherTab

            // show modifier range
            ModifierRangeUpDown.Maximum = SimpleXml.MaxPointIValue;
            ModifierRangeUpDown.Value = entity.ModifierRange;

            // show entity valuation
            ValuationUpDown.Value = (decimal) entity.Valuation;

            // show map view display options
            VisibleToggle.IsChecked = entity.IsVisible;
            DropShadowToggle.IsChecked = entity.HasDropShadow;
            TranslucentToggle.IsChecked = entity.IsTranslucent;

            // disable drop shadows for background terrain
            if (terrainClass != null && terrainClass.IsBackground)
                DropShadowToggle.IsEnabled = false;

            #endregion

            // construction completed
            this._initialized = true;

            // Image tab: select first list item
            if (ImageList.Items.Count > 0)
                ImageList.SelectedIndex = 0;
            else {
                // disable image buttons if list empty
                RemoveImageButton.IsEnabled = false;
                MoveImageUpButton.IsEnabled = false;
                MoveImageDownButton.IsEnabled = false;
            }

            // Variables tab: show attributes by default
            AttributeToggle.IsChecked = true;
        }

        #endregion
        #region Private Fields

        // construction parameters
        private readonly EntityClass _entity;

        // was construction completed?
        private readonly bool _initialized;

        // ignore control input events?
        private bool _ignoreEvents;

        // separate window for Available Images list
        private Window _availableImageWindow;

        // current basic & modifier variables
        private readonly VariableValueDictionary
            _entityAttributes, _entityCounters, _entityResources,_entityBuildResources;
        private readonly VariableModifierDictionary
            _entityAttributeModifiers, _entityResourceModifiers;

        // selected current basic & modifier variables
        private VariableValueDictionary _currentVariables;
        private VariableModifierDictionary _currentVariableModifiers;

        #endregion
        #region DataChanged

        /// <summary>
        /// Gets a value indicating whether any data was changed in the dialog.</summary>
        /// <value>
        /// <c>true</c> if the <see cref="EntityClass"/> supplied to the dialog constructor has been
        /// modified; otherwise, <c>false</c>.</value>
        /// <remarks>
        /// <b>DataChanged</b> returns <c>false</c> if no detectable changes were made. However, the
        /// original data may have been overwritten with a copy that is not detectably different,
        /// namely if the user clicked <b>OK</b> without making any changes.</remarks>

        public bool DataChanged { get; private set; }

        #endregion
        #region Private Methods
        #region AddImage

        /// <summary>
        /// Adds a new item to the "Image Stack" <see cref="ListView"/> on the <see
        /// cref="ImagesTab"/> page, based on the specified <see cref="EntityImage"/>.</summary>
        /// <param name="image">
        /// The <see cref="EntityImage"/> to add.</param>
        /// <remarks>
        /// <b>AddImage</b> adds the specified <paramref name="image"/> to the "Image Stack" list
        /// view and sets the <see cref="DataChanged"/> flag.</remarks>

        private void AddImage(EntityImage image) {
            if (image == null) return;

            // create new image stack entry with selected image
            ImageStackEntry entry = new ImageStackEntry(image);

            // add and select new list view item
            var item = new ImageListItem(image.Id, entry);
            int index = ImageList.Items.Add(item);
            ImageList.SelectAndShow(index);

            // broadcast data changes
            UpdateImageStack();
            DataChanged = true;
        }

        #endregion
        #region AddVariable

        /// <summary>
        /// Adds a new basic value to the "Variable" <see cref="ListView"/> on the <see
        /// cref="VariablesTab"/> page, based on the specified <see cref="VariableClass"/>.
        /// </summary>
        /// <param name="variable">
        /// The <see cref="VariableClass"/> to add.</param>
        /// <remarks>
        /// <b>AddVariable</b> adds a basic value for the specified <paramref name="variable"/> to
        /// the "Variable" list view and sets the <see cref="DataChanged"/> flag. The variable
        /// category depends on the selected "Category" radio button.</remarks>

        private void AddVariable(VariableClass variable) {
            if (variable == null || this._currentVariables == null)
                return;

            // check if this entry already exists
            if (this._currentVariables.ContainsKey(variable.Id)) {

                // abort, can't add same item twice
                MessageBox.Show(this, Global.Strings.DialogVariableDuplicate,
                    Global.Strings.TitleVariableDuplicate,
                    MessageBoxButton.OK, MessageBoxImage.Information);

                return;
            }

            // create & select new variable with default value
            this._currentVariables.Add(variable.Id, 0);
            var item = CreateVariableItem(variable.Id);
            VariableList.SelectAndShow(item);

            // broadcast data changes
            UpdateVariablesTab(false);
            DataChanged = true;
        }

        #endregion
        #region AddVariableModifier

        /// <summary>
        /// Adds a new modifier value to the "Variable" <see cref="ListView"/> on the <see
        /// cref="VariablesTab"/> page, based on the specified <see cref="VariableClass"/>.
        /// </summary>
        /// <param name="variable">
        /// The <see cref="VariableClass"/> to add.</param>
        /// <param name="target">
        /// A <see cref="ModifierTarget"/> value indicating which modifier value to add.</param>
        /// <remarks>
        /// <b>AddVariableModifier</b> adds a modifier value for the specified <paramref
        /// name="variable"/> and <paramref name="target"/> to the "Variable" list view and sets the
        /// <see cref="DataChanged"/> flag. The variable category depends on the selected "Category"
        /// radio button.</remarks>

        private void AddVariableModifier(VariableClass variable, ModifierTarget target) {
            if (variable == null || this._currentVariableModifiers == null)
                return;

            // check if this entry already exists
            VariableModifier modifier;
            this._currentVariableModifiers.TryGetValue(variable.Id, out modifier);

            if (modifier == null) {
                // create new entry for variable identifier
                modifier = new VariableModifier();
                this._currentVariableModifiers.Add(variable.Id, modifier);
            }
            else if (modifier.GetByTarget(target) != null) {

                // abort, can't add same item twice
                MessageBox.Show(this, Global.Strings.DialogVariableDuplicate,
                    Global.Strings.TitleVariableDuplicate,
                    MessageBoxButton.OK, MessageBoxImage.Information);

                return;
            }

            // create & select new variable with default values
            modifier.SetByTarget(target, 0);
            var item = CreateVariableItem(variable.Id, target);
            VariableList.SelectAndShow(item);

            // broadcast data changes
            UpdateVariablesTab(false);
            DataChanged = true;
        }

        #endregion
        #region CreateVariableItem(String)

        /// <overloads>
        /// Creates a new item for the "Variables" <see cref="ListView"/> on the <see
        /// cref="VariablesTab"/> page, containing the specified <see cref="VariableClass"/>
        /// identifier and associated basic or modifier value.</overloads>
        /// <summary>
        /// Creates a new item for the "Variables" <see cref="ListView"/> on the <see
        /// cref="VariablesTab"/> page, containing the specified <see cref="VariableClass"/>
        /// identifier and associated basic value.</summary>
        /// <param name="id">
        /// The <see cref="VariableClass.Id"/> string of a <see cref="VariableClass"/>.</param>
        /// <returns>
        /// The new <see cref="VariableListItem"/> if one was created and added to the "Variables"
        /// <see cref="ListView"/>; otherwise, a null reference.</returns>
        /// <remarks>
        /// <b>CreateVariableItem</b> immediately returns a null reference if the specified
        /// <paramref name="id"/> is not found in currently selected <see
        /// cref="VariableValueDictionary"/>.</remarks>

        private VariableListItem CreateVariableItem(string id) {
            if (this._currentVariables == null)
                return null;

            // get current value if present
            int value;
            if (!this._currentVariables.TryGetValue(id, out value))
                return null;

            // format variable as basic value
            string formatValue = VariableClass.FormatUnscaled(value, false);

            // store ID and basic value
            var newItem = new VariableListItem(id, formatValue, null);

            ItemCollection items = VariableList.Items;
            for (int i = 0; i < items.Count; i++) {
                VariableListItem item = (VariableListItem) items[i];

                // sort alphabetically, with basic values before modifiers
                if (item.Id == newItem.Id || String.CompareOrdinal(item.Id, newItem.Id) > 0) {
                    items.Insert(i, newItem);
                    return newItem;
                }
            }

            // append to end of list
            items.Add(newItem);
            return newItem;
        }

        #endregion
        #region CreateVariableItem(String, ModifierTarget)

        /// <summary>
        /// Creates a new item for the "Variables" <see cref="ListView"/> on the <see
        /// cref="VariablesTab"/> page, containing the specified <see cref="VariableClass"/>
        /// identifier and associated modifier value.</summary>
        /// <param name="id">
        /// The <see cref="VariableClass.Id"/> string of a <see cref="VariableClass"/>.</param>
        /// <param name="target">
        /// A <see cref="ModifierTarget"/> value indicating which modifier value to add.</param>
        /// <returns>
        /// The new <see cref="VariableListItem"/> if one was created and added to the "Variables"
        /// <see cref="ListView"/>; otherwise, a null reference.</returns>
        /// <remarks>
        /// <b>CreateVariableItem</b> immediately returns a null reference if the specified
        /// <paramref name="id"/> is not found in the currently selected <see
        /// cref="VariableModifierDictionary"/>, or if the element that matches the specified
        /// <paramref name="id"/> does not define a modifier value for the specified <paramref
        /// name="target"/>.</remarks>

        private VariableListItem CreateVariableItem(string id, ModifierTarget target) {
            if (this._currentVariableModifiers == null)
                return null;

            // get current modifier if present
            VariableModifier modifier;
            if (!this._currentVariableModifiers.TryGetValue(id, out modifier))
                return null;

            // get value for specified target, if any
            int? value = modifier.GetByTarget(target);
            if (value == null) return null;

            // format variable as modifier value
            string formatValue = VariableClass.FormatUnscaled(value, target);

            // store ID and value with modifier target
            var newItem = new VariableListItem(id, formatValue, target);

            ItemCollection items = VariableList.Items;
            for (int i = 0; i < items.Count; i++) {
                VariableListItem item = (VariableListItem) items[i];

                // sort alphabetically, with basic values before modifiers
                if (String.CompareOrdinal(item.Id, newItem.Id) > 0) {
                    items.Insert(i, newItem);
                    return newItem;
                }
            }

            // append to end of list
            items.Add(newItem);
            return newItem;
        }

        #endregion
        #region FindImage

        /// <summary>
        /// Finds and selects the scenario <see cref="EntityImage"/> that corresponds to the
        /// selected <see cref="ImageStackEntry"/> on the <see cref="ImagesTab"/> page.</summary>
        /// <returns><para>
        /// The <see cref="ImageStackEntry"/> associated with the first selected item in the "Image
        /// Stack" <see cref="ListView"/>.
        /// </para><para>-or-</para><para>
        /// An empty <see cref="ImageStackEntry"/> if no item is selected.</para></returns>
        /// <remarks>
        /// <b>FindImage</b> selects the item in the hosted <see cref="ImageListBox"/> that
        /// corresponds to the first selected item in the "Image Stack" list view, or shows a
        /// message box if there is no corresponding scenario <see cref="EntityImage"/>.</remarks>

        private ImageStackEntry FindImage() {

            // retrieve selected image stack entry, if any
            object item = ImageList.SelectedItem;
            if (item == null) return new ImageStackEntry();
            ImageStackEntry entry = ((ImageListItem) item).Value;

            // select entry's image, if available
            for (int i = 0; i < AvailableImageList.Items.Count; i++) {
                var imageItem = (ImageListBoxItem) AvailableImageList.Items[i];

                if (entry.Image.Value == imageItem.Content as EntityImage) {
                    AvailableImageList.SelectedIndex = i;
                    AvailableImageList.ScrollIntoView(imageItem);
                    return entry;
                }
            }

            MessageBox.Show(MainWindow.Instance,
                Global.Strings.DialogImageMissing, Global.Strings.TitleImageMissing, 
                MessageBoxButton.OK, MessageBoxImage.Information);

            return entry;
        }

        #endregion
        #region GetSelectedItem

        /// <summary>
        /// Gets the <see cref="String"/> representation of the <see cref="Selector.SelectedItem"/>
        /// of the specified <see cref="ComboBox"/>, if any.</summary>
        /// <param name="comboBox">
        /// The <see cref="ComboBox"/> to examine.</param>
        /// <returns>
        /// The <see cref="String"/> representation of the <see cref="Selector.SelectedItem"/> of
        /// the specified <paramref name="comboBox"/>, if its index is greater than zero; otherwise,
        /// a null reference.</returns>

        private static string GetSelectedItem(ComboBox comboBox) {
            int index = comboBox.SelectedIndex;
            return (index <= 0 ? null : comboBox.Items[index].ToString());
        }

        #endregion
        #region InitComboBox

        /// <summary>
        /// Initializes the specified <see cref="ComboBox"/> with the specified <see
        /// cref="ItemsControl.Items"/> and <see cref="Selector.SelectedItem"/>.</summary>
        /// <param name="comboBox">
        /// The <see cref="ComboBox"/> to initialize.</param>
        /// <param name="items">
        /// The initial <see cref="ItemsControl.Items"/> for the specified <paramref
        /// name="comboBox"/>.</param>
        /// <param name="selectedItem">
        /// The initial <see cref="Selector.SelectedItem"/> for the specified <paramref
        /// name="comboBox"/>.</param>
        /// <remarks>
        /// <b>InitComboBox</b> places a first item labelled "(none)" before the specified <paramref
        /// name="items"/>, and selects that first item if the specified <paramref
        /// name="selectedItem"/> is a null reference or an empty string.</remarks>

        private static void InitComboBox(ComboBox comboBox,
            IList<string> items, string selectedItem) {

            // first item indicates no selection
            comboBox.Items.Add(Global.Strings.LabelNone);

            // add remaining items, if any
            foreach (string item in items)
                comboBox.Items.Add(item);

            // selected indicated item, if possible
            if (comboBox.Items.Count == 1 || String.IsNullOrEmpty(selectedItem))
                comboBox.SelectedIndex = 0;
            else
                comboBox.SelectedItem = selectedItem;
        }

        #endregion
        #region ShowAvailableImageWindow

        /// <summary>
        /// Shows the "Available Images" <see cref="ImageListBox"/> in a separate <see
        /// cref="Window"/>.</summary>
        /// <remarks>
        /// <b>ShowAvailableImageWindow</b> creates a new <see cref="Window"/> for the "Available
        /// Images" list box if necessary, and always shows the <see cref="Window"/>.</remarks>

        private void ShowAvailableImageWindow() {
            if (this._availableImageWindow == null) {

                var window = new Window() { Owner = this };
                window.Title = Global.Strings.TitleAvailableImages;
                window.ResizeMode = ResizeMode.CanResize;
                window.ShowActivated = false;
                window.ShowInTaskbar = false;
                window.WindowStyle = WindowStyle.ToolWindow;

                // ensure we notice if user closes window
                window.Closed += delegate {
                    this._availableImageWindow.Content = null;
                    this._availableImageWindow = null;
                };

                // set DialogResult on Enter/Escape
                window.KeyDown += (sender, args) => {
                    switch (args.Key) {

                        case Key.Enter:
                            args.Handled = true;
                            DialogResult = true;
                            break;

                        case Key.Escape:
                            args.Handled = true;
                            DialogResult = false;
                            break;
                    }
                };

                // show window to right of dialog
                window.Left = Left + ActualWidth;
                window.Top = Top;
                window.Width = Math.Min(ActualWidth,
                    SystemParameters.VirtualScreenWidth - window.Left);
                window.Height = ActualHeight;

                // show Available Images list in window
                AvailableImageList.Visibility = Visibility.Visible;
                window.Content = AvailableImageList;
                this._availableImageWindow = window;
            }

            this._availableImageWindow.Show();
        }

        #endregion
        #region UpdateImageStack

        /// <summary>
        /// Updates all <see cref="EntityClass.ImageStack"/> data and controls on the <see
        /// cref="ImagesTab"/> page.</summary>

        private void UpdateImageStack() {

            // show updated image stack
            var imageStack = new List<ImageStackEntry>(ImageList.Items.Count);
            foreach (ImageListItem item in ImageList.Items)
                imageStack.Add(item.Value);
            StackPreview.Show(imageStack);

            // disable image buttons if list empty
            bool anyImages = (ImageList.Items.Count > 0);
            RemoveImageButton.IsEnabled = anyImages;
            MoveImageUpButton.IsEnabled = anyImages;
            MoveImageDownButton.IsEnabled = anyImages;
        }

        #endregion
        #region UpdateImageUsage

        /// <summary>
        /// Updates all "Feature Use" controls on the <see cref="ImagesTab"/> page to reflect the
        /// specified <see cref="ImageStackEntry"/>.</summary>
        /// <param name="entry">
        /// The <see cref="ImageStackEntry"/> whose data to show.</param>

        private void UpdateImageUsage(ImageStackEntry entry) {

            // prevent control input events
            this._ignoreEvents = true;

            // show connection flag
            if (entry.IsImageUnconnected) {
                UnconnectedToggle.IsEnabled = false;
                UnconnectedToggle.IsChecked = true;
            } else {
                UnconnectedToggle.IsEnabled = true;
                UnconnectedToggle.IsChecked = entry.UseUnconnected;
            }

            // show scaling flag
            if (entry.IsImageUnscaled) {
                UnscaledToggle.IsEnabled = false;
                UnscaledToggle.IsChecked = true;
            } else {
                UnscaledToggle.IsEnabled = true;
                UnscaledToggle.IsChecked = entry.UseUnscaled;
            }

            // show selected frame and animation options
            EntityImage image = entry.Image.Value;
            int count = image.Frames.Count;
            int frames = (entry.SingleFrame < 0 ? count : 1);

            bool isAnimated = (frames >= 2 && image.Animation != AnimationMode.None);
            string animation = (isAnimated ?
                image.Animation + " / " + image.Sequence :
                AnimationMode.None.ToString());

            FrameInfo.Text = String.Format(ApplicationInfo.Culture,
                Global.Strings.InfoEntityFrame, frames, count, animation);

            // show or hide frame selection buttons
            OneFrameButton.IsEnabled = (count >= 2);
            AllFrameButton.IsEnabled = (count >= 2 && frames == 1);

            // allow control input events
            this._ignoreEvents = false;
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
                AddVariableModifierCombo.IsEnabled = (this._currentVariableModifiers != null);
            } else {
                AddVariableBasicButton.IsEnabled = false;
                AddVariableModifierCombo.IsEnabled = false;
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
        /// page of the <see cref="ChangeEntity"/> dialog.</remarks>

        private void HelpExecuted(object sender, ExecutedRoutedEventArgs args) {
            args.Handled = true;

            // default to dialog help page
            string helpPage = "DlgChangeEntity.html";

            // show help for specific tab page
            if (TextTab.IsSelected)
                helpPage = "DlgChangeEntityText.html";
            else if (ImagesTab.IsSelected)
                helpPage = "DlgChangeEntityImages.html";
            else if (VariablesTab.IsSelected)
                helpPage = "DlgChangeEntityVars.html";
            else if (AbilitiesTab.IsSelected)
                helpPage = "DlgChangeEntityAbilities.html";
            else if (OtherTab.IsSelected)
                helpPage = "DlgChangeEntityOther.html";

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
        /// cref="EntityClass"/> object supplied to the constructor, and sets the <see
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
                EditorUtility.ValidateVariables(this, this._entityAttributes) &&
                EditorUtility.ValidateVariables(this, this._entityAttributeModifiers) &&
                EditorUtility.ValidateVariables(this, this._entityCounters) &&
                EditorUtility.ValidateVariables(this, this._entityResources) &&
                EditorUtility.ValidateVariables(this, this._entityResourceModifiers) &&
                EditorUtility.ValidateVariables(this, this._entityBuildResources);

            if (!variablesValid) {
                args.Cancel = true;
                return;
            }

            #region TextTab

            // read name box into name property
            this._entity.Name = NameBox.Text;

            // read detail paragraphs into string collection
            this._entity.Paragraphs.Clear();
            this._entity.Paragraphs.AddRange(DetailBox.Text.Split(
                new string[] { Environment.NewLine }, StringSplitOptions.None));

            #endregion
            #region ImagesTab

            // read list view into image stack
            this._entity.ImageStack.Clear();
            foreach (ImageListItem item in ImageList.Items)
                this._entity.ImageStack.Add(item.Value);

            #endregion
            #region VariablesTab

            // read variable values into faction collections
            this._entity.Attributes.Clear();
            foreach (var pair in this._entityAttributes)
                this._entity.Attributes.Add(pair.Key, pair.Value);

            this._entity.AttributeModifiers.Clear();
            foreach (var pair in this._entityAttributeModifiers)
                this._entity.AttributeModifiers.Add(pair.Key, pair.Value);

            this._entity.Counters.Clear();
            foreach (var pair in this._entityCounters)
                this._entity.Counters.Add(pair.Key, pair.Value);

            this._entity.Resources.Clear();
            foreach (var pair in this._entityResources)
                this._entity.Resources.Add(pair.Key, pair.Value);

            this._entity.ResourceModifiers.Clear();
            foreach (var pair in this._entityResourceModifiers)
                this._entity.ResourceModifiers.Add(pair.Key, pair.Value);

            this._entity.BuildResources.Clear();
            foreach (var pair in this._entityBuildResources)
                this._entity.BuildResources.Add(pair.Key, pair.Value);

            #endregion
            #region AbilitiesTab

            // acquire common abilities
            this._entity.BlocksAttack = (BlocksAttackToggle.IsChecked == true);
            this._entity.ResourceTransfer = (ResourceTransferMode) TransferCombo.SelectedItem;

            UnitClass unitClass = this._entity as UnitClass;
            TerrainClass terrainClass = this._entity as TerrainClass;

            if (terrainClass != null) {
                // acquire Terrain abilities
                terrainClass.CanCapture = (CaptureToggleTerrain.IsChecked == true);
                terrainClass.CanDestroy = (DestroyToggleTerrain.IsChecked == true);

                // acquire Terrain options
                terrainClass.IsBackground = (BackgroundToggleTerrain.IsChecked == true);
                terrainClass.DifficultyAttribute = GetSelectedItem(DifficultyComboTerrain);
                terrainClass.ElevationAttribute = GetSelectedItem(ElevationComboTerrain);
            }
            else if (unitClass != null) {
                // acquire Unit abilities
                unitClass.CanCapture = (CaptureToggleUnit.IsChecked == true);
                unitClass.CanDefendOnly = (DefendToggleUnit.IsChecked == true);
                unitClass.CanHeal = (HealingToggleUnit.IsChecked == true);

                // acquire Unit options
                unitClass.RangedAttack = (TargetMode) RangedAttackComboUnit.SelectedItem;
                unitClass.AttackRangeAttribute = GetSelectedItem(AttackRangeComboUnit);
                unitClass.MovementAttribute = GetSelectedItem(MovementComboUnit);
                unitClass.MoraleResource = GetSelectedItem(MoraleComboUnit);
                unitClass.StrengthResource = GetSelectedItem(StrengthComboUnit);
            }

            #endregion
            #region OtherTab

            // read numeric options
            this._entity.ModifierRange = (int) ModifierRangeUpDown.Value;
            this._entity.Valuation = (double) ValuationUpDown.Value;

            // read map view display options
            this._entity.IsVisible = (VisibleToggle.IsChecked == true);
            this._entity.HasDropShadow = (DropShadowToggle.IsChecked == true);
            this._entity.IsTranslucent = (TranslucentToggle.IsChecked == true);

            #endregion
        }

        #endregion
        #region OnFrameAll

        /// <summary>
        /// Handles the <see cref="ButtonBase.Click"/> event for the "Use All" <see cref="Button"/>
        /// on the <see cref="ImagesTab"/> page.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="RoutedEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnFrameAll</b> sets the <see cref="ImageStackEntry.SingleFrame"/> value of the first
        /// selected item in the "Image Stack" list view to -1, and sets the <see
        /// cref="DataChanged"/> flag if the value has changed.</remarks>

        private void OnFrameAll(object sender, RoutedEventArgs args) {
            args.Handled = true;

            // retrieve selected image stack entry, if any
            int index = ImageList.SelectedIndex;
            if (index < 0) return;
            ImageListItem item = (ImageListItem) ImageList.Items[index];
            ImageStackEntry entry = item.Value;

            // update image stack if frame changed
            if (entry.SingleFrame >= 0) {
                entry.SingleFrame = -1;
                ImageList.SelectAndShow(index);

                // broadcast data changes
                UpdateImageUsage(entry);
                UpdateImageStack();
                DataChanged = true;
            }
        }

        #endregion
        #region OnFrameOne

        /// <summary>
        /// Handles the <see cref="ButtonBase.Click"/> event for the "Use One" <see cref="Button"/>
        /// on the <see cref="ImagesTab"/> page.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="RoutedEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnFrameOne</b> shows a <see cref="ChangeFrame"/> dialog, allowing the user to change
        /// the <see cref="ImageStackEntry.SingleFrame"/> property of the first selected item in the
        /// "Image Stack" list view, and sets the <see cref="DataChanged"/> flag if the value has
        /// changed.</remarks>

        private void OnFrameOne(object sender, RoutedEventArgs args) {
            args.Handled = true;

            // retrieve selected image stack entry, if any
            int index = ImageList.SelectedIndex;
            if (index < 0) return;
            ImageListItem item = (ImageListItem) ImageList.Items[index];
            ImageStackEntry entry = item.Value;

            // retrieve associated image, if any
            EntityImage image = entry.Image.Value;
            if (image == null || image.Frames.Count < 2)
                return;

            // allow user to select new single frame
            var dialog = new ChangeFrame(entry) { Owner = this };
            dialog.ShowDialog();

            // update image stack if frame changed
            if (dialog.DataChanged) {
                ImageList.SelectAndShow(index);

                // broadcast data changes
                UpdateImageUsage(entry);
                UpdateImageStack();
                DataChanged = true;
            }
        }

        #endregion
        #region OnAvailableImageActivate

        /// <summary>
        /// Handles the <see cref="Control.MouseDoubleClick"/> event for an <see
        /// cref="ImageListBoxItem"/> of the <see cref="ImageListBox"/> on the <see
        /// cref="ImagesTab"/> page.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="MouseButtonEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnAvailableImageActivate</b> calls <see cref="AddImage"/> with the double-clicked
        /// item in the <see cref="ImageListBox"/>.</remarks>

        private void OnAvailableImageActivate(object sender, MouseButtonEventArgs args) {
            args.Handled = true;

            // retrieve double-clicked item, if any
            var source = args.OriginalSource as DependencyObject;
            var item = ItemsControl.ContainerFromElement(AvailableImageList, source) as ImageListBoxItem;
            if (item != null) AddImage(item.Content as EntityImage);
        }

        #endregion
        #region OnColorChanged

        /// <summary>
        /// Handles the <see cref="NumericUpDown.ValueChanged"/> event for the "Red", "Green", and
        /// "Blue" <see cref="NumericUpDown"/> controls on the <see cref="ImagesTab"/> page.
        /// </summary>
        /// <param name="sender">
        /// The <see cref="NumericUpDown"/> control sending the event.</param>
        /// <param name="args">
        /// An <see cref="EventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnColorChanged</b> updates the <see cref="ImageStackEntry.ColorShift"/> of the first
        /// selected item in the "Image Stack" list view, and sets the <see cref="DataChanged"/>
        /// flag if the value has changed.</remarks>

        private void OnColorChanged(object sender, EventArgs args) {
            if (this._ignoreEvents) return;

            // retrieve selected image stack entry, if any
            int index = ImageList.SelectedIndex;
            if (index < 0) return;
            ImageListItem item = (ImageListItem) ImageList.Items[index];
            ImageStackEntry entry = item.Value;

            // retrieve selected color shift
            ColorVector colorShift = new ColorVector(
                (short) RedUpDown.Value, (short) GreenUpDown.Value, (short) BlueUpDown.Value);

            // update image stack if color shift changed
            if (entry.ColorShift != colorShift) {
                entry.ColorShift = colorShift;
                ImageList.SelectAndShow(index);

                // broadcast data changes
                UpdateImageStack();
                DataChanged = true;
            }
        }

        #endregion
        #region OnImageAdd

        /// <summary>
        /// Handles the <see cref="ButtonBase.Click"/> event for the "Add Image" <see
        /// cref="Button"/> on the <see cref="ImagesTab"/> page.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="RoutedEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnImageAdd</b> adds the selected item in the hosted <see cref="ImageListBox"/> to the
        /// "Image Stack" list view and sets the <see cref="DataChanged"/> flag.</remarks>

        private void OnImageAdd(object sender, RoutedEventArgs args) {
            args.Handled = true;

            // retrieve selected scenario image, if any
            var item = AvailableImageList.SelectedItem as ImageListBoxItem;
            if (item != null) AddImage(item.Content as EntityImage);
        }

        #endregion
        #region OnImageDown

        /// <summary>
        /// Handles the <see cref="ButtonBase.Click"/> event for the "Move Image Down" <see
        /// cref="Button"/> on the <see cref="ImagesTab"/> page.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="RoutedEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnImageDown</b> swaps the first selected item in the "Image Stack" list view with its
        /// lower neighbor and sets the <see cref="DataChanged"/> flag.</remarks>

        private void OnImageDown(object sender, RoutedEventArgs args) {
            args.Handled = true;

            // retrieve first selected item, if any
            if (ImageList.SelectedItems.Count == 0 || ImageList.Items.Count < 2)
                return;

            // move item down and re-select it
            object item = ImageList.SelectedItem;
            int index = CollectionsUtility.MoveItemUntyped(ImageList.Items, item, +1);
            ImageList.SelectAndShow(Math.Min(index, ImageList.Items.Count - 1));

            // broadcast data changes
            UpdateImageStack();
            DataChanged = true;
        }

        #endregion
        #region OnImageFind

        /// <summary>
        /// Handles the <see cref="ButtonBase.Click"/> event for the "Find Image" <see
        /// cref="Button"/> on the <see cref="ImagesTab"/> page.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="RoutedEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnImageFind</b> calls <see cref="FindImage"/> to select the item in the <see
        /// cref="ImageListBox"/> that corresponds to the first selected item in the "Image Stack"
        /// list view.</remarks>

        private void OnImageFind(object sender, RoutedEventArgs args) {
            args.Handled = true;
            FindImage();
        }

        #endregion
        #region OnImageRemove

        /// <summary>
        /// Handles the <see cref="ButtonBase.Click"/> event for the "Remove Image" <see
        /// cref="Button"/> on the <see cref="ImagesTab"/> page.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="RoutedEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnImageRemove</b> removes the first selected item from the "Image Stack" list view
        /// and sets the <see cref="DataChanged"/>.</remarks>

        private void OnImageRemove(object sender, RoutedEventArgs args) {
            args.Handled = true;

            // retrieve selected image stack entry, if any
            int index = ImageList.SelectedIndex;
            if (index < 0) return;

            // remove entry from list view
            ImageList.Items.RemoveAt(index);

            // select item in the same position, or nothing (-1)
            ImageList.SelectAndShow(Math.Min(index, ImageList.Items.Count - 1));

            // broadcast data changes
            UpdateImageStack();
            DataChanged = true;
        }

        #endregion
        #region OnImageSelected

        /// <summary>
        /// Handles the <see cref="Selector.SelectionChanged"/> event for the "Image Stack" <see
        /// cref="ListView"/> on the <see cref="ImagesTab"/> page.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="SelectionChangedEventArgs"/> object containing event data.</param>
        /// <remarks><para>
        /// <b>OnImageSelected</b> calls <see cref="FindImage"/> to select the item in the hosted
        /// <see cref="ImageListBox"/> that corresponds to the first selected item in the "Image
        /// Stack" list view.
        /// </para><para>
        /// <b>OnImageSelected</b> also updates the "Positioning", "Color Shift", and "Feature Use"
        /// controls to reflect the data of the selected item.</para></remarks>

        private void OnImageSelected(object sender, SelectionChangedEventArgs args) {
            args.Handled = true;
            if (this._ignoreEvents) return;

            // retrieve selected image stack entry, if any
            ImageStackEntry entry = FindImage();
            if (entry.Image.Value == null) return;

            // prevent control input events
            this._ignoreEvents = true;

            // show pixel offset of selected image
            XOffsetUpDown.Value = entry.Offset.X;
            YOffsetUpDown.Value = entry.Offset.Y;

            // show mirror options of selected image
            XMirrorToggle.IsChecked = (entry.ScalingVector.X < 0);
            YMirrorToggle.IsChecked = (entry.ScalingVector.Y < 0);

            // show color shift of selected image
            RedUpDown.Value = entry.ColorShift.R;
            GreenUpDown.Value = entry.ColorShift.G;
            BlueUpDown.Value = entry.ColorShift.B;

            // update feature use controls
            UpdateImageUsage(entry);

            // allow control input events
            this._ignoreEvents = false;
        }

        #endregion
        #region OnImageUp

        /// <summary>
        /// Handles the <see cref="ButtonBase.Click"/> event for the "Move Image Up" <see
        /// cref="Button"/> on the <see cref="ImagesTab"/> page.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="RoutedEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnImageUp</b> swaps the first selected item in the "Image Stack" list view with its
        /// upper neighbor and sets the <see cref="DataChanged"/> flag.</remarks>

        private void OnImageUp(object sender, RoutedEventArgs args) {
            args.Handled = true;

            // retrieve first selected item, if any
            if (ImageList.SelectedItems.Count == 0 || ImageList.Items.Count < 2)
                return;

            // move item up and re-select it
            object item = ImageList.SelectedItem;
            int index = CollectionsUtility.MoveItemUntyped(ImageList.Items, item, -1);
            ImageList.SelectAndShow(Math.Max(0, index));

            // broadcast data changes
            UpdateImageStack();
            DataChanged = true;
        }

        #endregion
        #region OnMirrorChanged

        /// <summary>
        /// Handles the <see cref="ButtonBase.Click"/> event for the "X-Mirror" and "Y-Mirror" <see
        /// cref="CheckBox"/> controls on the <see cref="ImagesTab"/> page.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="RoutedEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnMirrorChanged</b> updates the <see cref="ImageStackEntry.ScalingVector"/> of the
        /// first selected item in the "Image Stack" list view, and sets <see cref="DataChanged"/>
        /// flag if the value has changed.</remarks>

        private void OnMirrorChanged(object sender, RoutedEventArgs args) {
            args.Handled = true;
            if (this._ignoreEvents) return;

            // retrieve selected image stack entry, if any
            int index = ImageList.SelectedIndex;
            if (index < 0) return;
            ImageListItem item = (ImageListItem) ImageList.Items[index];
            ImageStackEntry entry = item.Value;

            // replace indicated scaling vector component
            PointI vector = entry.ScalingVector;
            if (args.Source == XMirrorToggle)
                vector = new PointI((XMirrorToggle.IsChecked == true ? -1 : +1), vector.Y);
            else if (args.Source == YMirrorToggle)
                vector = new PointI(vector.X, (YMirrorToggle.IsChecked == true ? -1 : +1));

            // normalize scaling vector for comparison
            vector = ImageStackEntry.NormalizeScalingVector(vector);

            // update image stack if scaling vector changed
            if (entry.ScalingVector != vector) {
                entry.ScalingVector = vector;
                ImageList.SelectAndShow(index);

                // broadcast data changes
                UpdateImageStack();
                DataChanged = true;
            }
        }

        #endregion
        #region OnOffsetChanged

        /// <summary>
        /// Handles the <see cref="NumericUpDown.ValueChanged"/> event for the "X-Offset" and
        /// "Y-Offset" <see cref="NumericUpDown"/> controls on the <see cref="ImagesTab"/> page.
        /// </summary>
        /// <param name="sender">
        /// The <see cref="NumericUpDown"/> control sending the event.</param>
        /// <param name="args">
        /// An <see cref="EventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnOffsetChanged</b> updates the <see cref="ImageStackEntry.Offset"/> of the first
        /// selected item in the "Image Stack" list view, and sets the <see cref="DataChanged"/>
        /// flag if the value has changed.</remarks>

        private void OnOffsetChanged(object sender, EventArgs args) {
            if (this._ignoreEvents) return;

            // retrieve selected image stack entry, if any
            int index = ImageList.SelectedIndex;
            if (index < 0) return;
            ImageListItem item = (ImageListItem) ImageList.Items[index];
            ImageStackEntry entry = item.Value;

            // replace indicated offset coordinate
            PointI offset = entry.Offset;
            if (sender == XOffsetUpDown.HostedControl)
                offset = new PointI((int) XOffsetUpDown.Value, offset.Y);
            else if (sender == YOffsetUpDown.HostedControl)
                offset = new PointI(offset.X, (int) YOffsetUpDown.Value);

            // update image stack if offset changed
            if (entry.Offset != offset) {
                entry.Offset = offset;
                ImageList.SelectAndShow(index);

                // broadcast data changes
                UpdateImageStack();
                DataChanged = true;
            }
        }

        #endregion
        #region OnTabSelected

        /// <summary>
        /// Handles the <see cref="Selector.SelectionChanged"/> event for the main <see
        /// cref="TabControl"/> of the <see cref="ChangeEntity"/> dialog.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="SelectionChangedEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnTabSelected</b> shows a separate <see cref="Window"/> for the "Available Images"
        /// list box if the <see cref="ImagesTab"/> page is selected, and hides that <see
        /// cref="Window"/> otherwise.</remarks>

        private void OnTabSelected(object sender, SelectionChangedEventArgs args) {

            // ignore unhandled events from nested controls
            if (args.OriginalSource != DialogTabControl) return;
            args.Handled = true;

            if (ImagesTab.IsSelected) {
                ShowAvailableImageWindow();
                AvailableImageList.ScrollIntoView(AvailableImageList.SelectedItem);
            }
            else if (this._availableImageWindow != null)
                this._availableImageWindow.Hide();
        }

        #endregion
        #region OnTextChanged

        /// <summary>
        /// Handles the <see cref="TextBoxBase.TextChanged"/> event for the "Class Name" and
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
        #region OnUsageChanged

        /// <summary>
        /// Handles the <see cref="ButtonBase.Click"/> event for the "Unconnected" and "Unscaled"
        /// <see cref="CheckBox"/> controls on the <see cref="ImagesTab"/> page.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="RoutedEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnUsageChanged</b> updates the <see cref="ImageStackEntry.UseUnconnected"/> or <see
        /// cref="ImageStackEntry.UseUnscaled"/> property, depending on the specified <paramref
        /// name="sender"/>, of the first selected item in the "Image Stack" list view, and sets the
        /// <see cref="DataChanged"/> flag if the value has changed.</remarks>

        private void OnUsageChanged(object sender, RoutedEventArgs args) {
            args.Handled = true;
            if (this._ignoreEvents) return;

            // retrieve selected image stack entry, if any
            int index = ImageList.SelectedIndex;
            if (index < 0) return;
            ImageListItem item = (ImageListItem) ImageList.Items[index];
            ImageStackEntry entry = item.Value;

            // update specified feature usage flag
            bool oldValue = false, newValue = false;
            if (args.Source == UnconnectedToggle) {
                oldValue = entry.UseUnconnected;
                entry.UseUnconnected = newValue = (UnconnectedToggle.IsChecked == true);
            } else if (args.Source == UnscaledToggle) {
                oldValue = entry.UseUnscaled;
                entry.UseUnscaled = newValue = (UnscaledToggle.IsChecked == true);
            }

            // update image stack if scaling vector changed
            if (oldValue != newValue) {
                ImageList.SelectAndShow(index);

                // broadcast data changes
                UpdateImageUsage(entry);
                UpdateImageStack();
                DataChanged = true;
            }
        }

        #endregion
        #region OnUserInput

        /// <summary>
        /// Handles user input events for all controls on the <see cref="AbilitiesTab"/> and <see
        /// cref="OtherTab"/> pages.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// An <see cref="EventArgs"/> object containing event data.</param>

        private void OnUserInput(object sender, EventArgs args) {
            RoutedEventArgs routedArgs = args as RoutedEventArgs;
            if (routedArgs != null) routedArgs.Handled = true;
            if (this._initialized) DataChanged = true;
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
            if (item != null) AddVariable(item.Content as VariableClass);
        }

        #endregion
        #region OnVariableAdd

        /// <summary>
        /// Handles the <see cref="ButtonBase.Click"/> event for the "Add Basic Value" <see
        /// cref="Button"/> on the <see cref="VariablesTab"/> page.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="RoutedEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnVariableAdd</b> calls <see cref="AddVariable"/> with the first selected item in the
        /// "Available Variables" list view.</remarks>

        private void OnVariableAdd(object sender, RoutedEventArgs args) {
            args.Handled = true;

            // retrieve selected variable entry, if any
            VariableClass variable = AvailableVariableList.SelectedItem as VariableClass;
            AddVariable(variable);
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
            if (AttributeToggle.IsChecked == true) {
                variables = MasterSection.Instance.Variables.Attributes.Values;
                this._currentVariables = this._entityAttributes;
                this._currentVariableModifiers = this._entityAttributeModifiers;
            }
            else if (CounterToggle.IsChecked == true) {
                variables = MasterSection.Instance.Variables.Counters.Values;
                this._currentVariables = this._entityCounters;
                this._currentVariableModifiers = null;
            }
            else if (ResourceToggle.IsChecked == true) {
                variables = MasterSection.Instance.Variables.Resources.Values;
                this._currentVariables = this._entityResources;
                this._currentVariableModifiers = this._entityResourceModifiers;
            }
            else if (BuildResourceToggle.IsChecked == true) {
                variables = MasterSection.Instance.Variables.Resources.Values;
                this._currentVariables = this._entityBuildResources;
                this._currentVariableModifiers = null;
            }
            else {
                Debug.Fail("OnVariableCategory: No Category button checked.");
                variables = MasterSection.Instance.Variables.Attributes.Values;
                this._currentVariables = this._entityAttributes;
                this._currentVariableModifiers = this._entityAttributeModifiers;
            }

            // show available scenario variables
            AvailableVariableList.ItemsSource = variables;

            // add faction values if defined
            VariableList.Items.Clear();
            foreach (VariableClass variable in variables) {
                CreateVariableItem(variable.Id);

                if (this._entityAttributeModifiers != null)
                    foreach (ModifierTarget target in VariableModifier.AllModifierTargets)
                        CreateVariableItem(variable.Id, target);
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
            decimal oldValue;
            if (item.Target == null)
                oldValue = VariableClass.ParseUnscaled(item.Value);
            else {
                ModifierTarget target;
                if (!VariableClass.TryParseUnscaled(item.Value, out oldValue, out target))
                    Debug.Fail("OnVariableChanged: TryParseUnscaled failed.");
                Debug.Assert(target == item.Target.Value);
            }

            // check if variable value has actually changed
            if (VariableUpDown.Value == oldValue) return;
            int value = (int) VariableUpDown.Value;

            // update variable value in collection
            string formatValue;
            if (item.Target == null) {
                this._currentVariables[item.Id] = value;
                formatValue = VariableClass.FormatUnscaled(value, false);
            } else {
                this._currentVariableModifiers[item.Id].SetByTarget(item.Target.Value, value);
                formatValue = VariableClass.FormatUnscaled(value, item.Target.Value);
            }

            // update variable value in list view
            item.Value = formatValue;
            VariableList.Items.Refresh();
            VariableList.SelectAndShow(index);

            // broadcast data changes
            DataChanged = true;
        }

        #endregion
        #region OnVariableModifier

        /// <summary>
        /// Handles the <see cref="Selector.SelectionChanged"/> event for the "Add Modifier" <see
        /// cref="ComboBox"/> on the <see cref="VariablesTab"/> page.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="SelectionChangedEventArgs"/> object containing event data.</param>
        /// <remarks><para>
        /// <b>OnVariableModifier</b> calls <see cref="AddVariableModifier"/> with the first
        /// selected item in the "Available Variables" list view and with the selected <see
        /// cref="ModifierTarget"/> in the "Add Modifier" combo box, if any.
        /// </para><para>
        /// <b>OnVariableModifier</b> always reselects the first item the "Add Modifier" combo box 
        /// before returning. This item shows the control's name and access key.</para></remarks>

        private void OnVariableModifier(object sender, SelectionChangedEventArgs args) {
            args.Handled = true;

            // retrieve selected modifier target, if any
            object selection = AddVariableModifierCombo.SelectedItem;
            if (selection is ModifierTarget) {

                // retrieve selected variable entry, if any
                VariableClass variable = AvailableVariableList.SelectedItem as VariableClass;
                AddVariableModifier(variable, (ModifierTarget) selection);
            }

            AddVariableModifierCombo.SelectedIndex = 0;
        }

        #endregion
        #region OnVariableModifierAccess

        /// <summary>
        /// Handles the attached <b>AccessKeyPressed</b> event for the current <see
        /// cref="AccessKeyManager"/>.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// An <see cref="AccessKeyPressedEventArgs"/> object containing event data.</param>
        /// <remarks><para>
        /// <b>OnVariableModifierAccess</b> sets the <see cref="ComboBox.IsDropDownOpen"/> flag of
        /// the "Add Modifier" combo box to <c>true</c> if the pressed access <see
        /// cref="AccessKeyPressedEventArgs.Key"/> equals "M".
        /// </para><para>
        /// This allows the user to scroll through the available choices in the combo box without
        /// triggering the <see cref="OnVariableModifier"/> event handler until a selection is
        /// confirmed by pressing the Enter key.</para></remarks>

        private void OnVariableModifierAccess(object sender, AccessKeyPressedEventArgs args) {
            args.Handled = true;
            if (args.Key == "M")
                AddVariableModifierCombo.IsDropDownOpen = true;
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
            if (item.Target == null)
                this._currentVariables.Remove(item.Id);
            else {
                int modifierIndex = this._currentVariableModifiers.IndexOfKey(item.Id);
                var modifier = this._currentVariableModifiers.GetByIndex(modifierIndex);

                // remove target value and check for remaining targets
                modifier.SetByTarget(item.Target.Value, null);
                if (modifier.IsEmpty)
                    this._currentVariableModifiers.RemoveAt(modifierIndex);
            }

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
            decimal value = 0m;
            int index = VariableList.SelectedIndex;

            if (index >= 0) {
                // update numeric control with variable value
                VariableListItem item = (VariableListItem) VariableList.Items[index];
                if (item.Target == null)
                    value = VariableClass.ParseUnscaled(item.Value);
                else {
                    ModifierTarget target;
                    if (!VariableClass.TryParseUnscaled(item.Value, out value, out target))
                        Debug.Fail("OnVariableSelected: TryParseUnscaled failed.");
                    Debug.Assert(target == item.Target.Value);
                }
            }

            VariableUpDown.Value = value;
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

        private void OnImageWidthChanged(object sender, EventArgs args) {
            double width = ImageList.ActualWidth - 28;
            if (width > 0) ImageColumn.Width = width;
        }

        private void OnVariableWidthChanged(object sender, EventArgs args) {
            double width = (VariableList.ActualWidth - 28) / 2.0;
            if (width > 0) {
                VariableColumn.Width = width;
                VariableValueColumn.Width = width;
            }
        }

        private void OnAvailableVariableWidthChanged(object sender, EventArgs args) {
            double width = AvailableVariableList.ActualWidth - 28;
            if (width > 0) AvailableVariableColumn.Width = width;
        }

        #endregion
        #endregion
    }
}
