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

    #endregion

    /// <summary>
    /// Shows a dialog allowing the user to change an <see cref="EntityTemplate"/>.</summary>
    /// <remarks>
    /// Please refer to the "Change Entity Template" page of the "Editor Dialogs" section in the
    /// application help file for details on this dialog.</remarks>

    public partial class ChangeTemplate: Window {
        #region ChangeTemplate(EntityTemplate, EntityClass)

        /// <summary>
        /// Initializes a new instance of the <see cref="ChangeTemplate"/> class with the specified
        /// <see cref="EntityTemplate"/>.</summary>
        /// <param name="template">
        /// The <see cref="EntityTemplate"/> whose data to change.</param>
        /// <param name="entityClass">
        /// The <see cref="EntityClass"/> on which the specified <paramref name="template"/> is
        /// based. Use <see cref="CanEdit"/> to obtain this argument.</param>
        /// <exception cref="ArgumentException">
        /// <paramref name="template"/> specifies an <see cref="EntityTemplate.EntityClass"/> that
        /// differs from the specified <paramref name="entityClass"/>.</exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="template"/> or <paramref name="entityClass"/> is a null reference.
        /// </exception>
        /// <remarks>
        /// The data of the specified <paramref name="template"/> may be changed in the dialog, as
        /// indicated by the value of the <see cref="DataChanged"/> property.</remarks>

        public ChangeTemplate(EntityTemplate template, EntityClass entityClass) {
            if (template == null)
                ThrowHelper.ThrowArgumentNullException("template");
            if (entityClass == null)
                ThrowHelper.ThrowArgumentNullException("entityClass");

            if (template.EntityClass != entityClass.Id)
                ThrowHelper.ThrowArgumentExceptionWithFormat(
                    "template", Tektosyne.Strings.ArgumentPropertyInvalid, "EntityClass");

            this._template = template;
            this._entityClass = entityClass;

            InitializeComponent();
            Title += template.EntityClass;

            #region TextTab

            // show instance and default name
            NameBox.Text = template.Name;
            DefaultNameBox.Text = entityClass.Name;

            // show visibility override
            VisibleToggle.IsChecked = template.IsVisible;

            #endregion
            #region FrameTab

            if (entityClass.ImageAnimation != AnimationMode.None) {
                // only show note that entity class is animated
                FrameSingleInfo.Visibility = Visibility.Collapsed;
                RandomFrameToggle.Visibility = Visibility.Collapsed;
                FrameList.Visibility = Visibility.Collapsed;
            }
            else if (entityClass.FrameCount < 2) {
                // only show note that entity class has single frame
                FrameAnimationInfo.Visibility = Visibility.Collapsed;
                RandomFrameToggle.Visibility = Visibility.Collapsed;
                FrameList.Visibility = Visibility.Collapsed;
            }
            else {
                // hide both notes and show frame controls instead
                FrameSingleInfo.Visibility = Visibility.Collapsed;
                FrameAnimationInfo.Visibility = Visibility.Collapsed;

                // prepare list box for catalog frames
                FrameList.FrameBitmap = MapViewManager.Instance.Catalog;
                FrameList.Polygon = MapViewManager.Instance.MapGrid.Element;

                // add catalog frame bounds to list box
                for (int i = 0; i < entityClass.FrameCount; i++) {
                    int index = entityClass.FrameIndex + i;
                    RectI bounds = MapViewManager.Instance.GetTileBounds(index);
                    FrameList.Items.Add(new ImageListBoxItem(bounds));
                }

                // initialize frame controls
                if (template.UseRandomFrame)
                    RandomFrameToggle.IsChecked = true;
                else
                    SelectFrameOffset();
            }

            #endregion
            #region VariablesTab

            // reference default variables of entity class
            this._defaultAttributes = entityClass.Attributes;
            this._defaultAttributeModifiers = entityClass.AttributeModifiers;
            this._defaultCounters = entityClass.Counters;
            this._defaultResources = entityClass.Resources;
            this._defaultResourceModifiers = entityClass.ResourceModifiers;

            // create shallow copies of entity template variables
            this._entityAttributes = new VariableValueDictionary(template.Attributes);
            this._entityAttributeModifiers = new VariableModifierDictionary(template.AttributeModifiers);
            this._entityCounters = new VariableValueDictionary(template.Counters);
            this._entityResources = new VariableValueDictionary(template.Resources);
            this._entityResourceModifiers = new VariableModifierDictionary(template.ResourceModifiers);

            // set range for variable value control
            VariableUpDown.Minimum = VariableClass.AbsoluteMinimum;
            VariableUpDown.Maximum = VariableClass.AbsoluteMaximum;

            // adjust column widths of Variable list view
            DependencyPropertyDescriptor.FromProperty(
                ListView.ActualWidthProperty, typeof(ListView))
                .AddValueChanged(VariableList, OnVariableWidthChanged);

            #endregion

            // construction completed
            this._initialized = true;

            // Variables tab: show attributes by default
            AttributeToggle.IsChecked = true;
        }

        #endregion
        #region Private Fields

        // construction parameters
        private readonly EntityTemplate _template;
        private readonly EntityClass _entityClass;

        // was construction completed?
        private readonly bool _initialized = false;

        // current basic & modifier variables
        private readonly VariableValueDictionary
            _entityAttributes, _entityCounters, _entityResources;
        private readonly VariableModifierDictionary
            _entityAttributeModifiers, _entityResourceModifiers;

        // default basic & modifier variables
        private readonly VariableValueDictionary
            _defaultAttributes, _defaultCounters, _defaultResources;
        private readonly VariableModifierDictionary
            _defaultAttributeModifiers, _defaultResourceModifiers;

        // selected current basic & modifier variables
        private VariableValueDictionary _currentVariables;
        private VariableModifierDictionary _currentVariableModifiers;

        // selected default basic & modifier variables
        private VariableValueDictionary _defaultVariables;
        private VariableModifierDictionary _defaultVariableModifiers;

        #endregion
        #region CanEdit

        /// <summary>
        /// Determines whether the specified <see cref="EntityTemplate"/> can be edited in the <see
        /// cref="ChangeTemplate"/> dialog.</summary>
        /// <param name="owner">
        /// The parent <see cref="Window"/> for any dialogs.</param>
        /// <param name="template">
        /// The <see cref="EntityTemplate"/> to examine.</param>
        /// <param name="entityClass">
        /// Returns the <see cref="EntityClass"/> on which the specified <paramref name="template"/>
        /// is based; or a null reference on failure.</param>
        /// <returns>
        /// <c>true</c> if the specified <paramref name="template"/> is a valid argument to the <see
        /// cref="ChangeTemplate"/> constructor; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="template"/> is a null reference.</exception>
        /// <remarks>
        /// <b>CanEdit</b> returns <c>true</c> if a valid <paramref name="entityClass"/> was found.
        /// Otherwise, <b>CanEdit</b> shows a dialog with the specified <paramref name="owner"/>,
        /// informing the user that the specified <paramref name="template"/> cannot be edited, and
        /// returns <c>false</c>.</remarks>

        public static bool CanEdit(Window owner,
            EntityTemplate template, out EntityClass entityClass) {

            if (template == null)
                ThrowHelper.ThrowArgumentNullException("template");

            entityClass = MasterSection.Instance.Entities.GetEntity(template);
            if (entityClass != null) return true;

            MessageBox.Show(owner,
                Global.Strings.DialogEntityClassMissing,
                Global.Strings.TitleEntityClassMissing,
                MessageBoxButton.OK, MessageBoxImage.Information);

            return false;
        }

        #endregion
        #region DataChanged

        /// <summary>
        /// Gets a value indicating whether any data was changed in the dialog.</summary>
        /// <value>
        /// <c>true</c> if the <see cref="EntityTemplate"/> supplied to the dialog constructor has
        /// been modified; otherwise, <c>false</c>.</value>
        /// <remarks>
        /// <b>DataChanged</b> returns <c>false</c> if no detectable changes were made. However, the
        /// original data may have been overwritten with a copy that is not detectably different,
        /// namely if the user clicked <b>OK</b> without making any changes.</remarks>

        public bool DataChanged { get; private set; }

        #endregion
        #region Private Methods
        #region CreateVariableItem(String)

        /// <overloads>
        /// Creates a new item for the "Variables" <see cref="ListView"/> on the <see
        /// cref="VariablesTab"/> page, containing the specified <see cref="VariableClass"/>
        /// identifier and associated value.</overloads>
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
        /// <paramref name="id"/> is not found in the currently selected default <see
        /// cref="VariableValueDictionary"/>.</remarks>

        private VariableListItem CreateVariableItem(string id) {
            if (this._defaultVariables == null)
                return null;

            // get default value if present
            int defaultValue;
            if (!this._defaultVariables.TryGetValue(id, out defaultValue))
                return null;

            // add current value if necessary
            int currentValue;
            if (!this._currentVariables.TryGetValue(id, out currentValue))
                currentValue = defaultValue;

            // format variable as basic value
            VariableListItem newItem = new VariableListItem(id,
                    VariableClass.FormatUnscaled(currentValue, false),
                    VariableClass.FormatUnscaled(defaultValue, false), null);

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
        /// identifier and associated modifier value for the specified <see cref="ModifierTarget"/>.
        /// </summary>
        /// <param name="id">
        /// The <see cref="VariableClass.Id"/> string of a <see cref="VariableClass"/>.</param>
        /// <param name="target">
        /// A <see cref="ModifierTarget"/> value indicating which modifier value to add.</param>
        /// <returns>
        /// The new <see cref="VariableListItem"/> if one was created and added to the "Variables"
        /// <see cref="ListView"/>; otherwise, a null reference.</returns>
        /// <remarks>
        /// <b>CreateVariableItem</b> immediately returns a null reference if the specified
        /// <paramref name="id"/> is not found in the currently selected default <see
        /// cref="VariableModifierDictionary"/>, or if the element that matches the specified
        /// <paramref name="id"/> does not define a modifier value for the specified <paramref
        /// name="target"/>.</remarks>

        private VariableListItem CreateVariableItem(string id, ModifierTarget target) {
            if (this._defaultVariableModifiers == null)
                return null;

            // get default modifier if present
            VariableModifier defaultModifier;
            if (!this._defaultVariableModifiers.TryGetValue(id, out defaultModifier))
                return null;

            // get value for specified target, if any
            int? defaultValue = defaultModifier.GetByTarget(target);
            if (defaultValue == null) return null;

            // add current modifier if necessary
            VariableModifier currentModifier;
            if (!this._currentVariableModifiers.TryGetValue(id, out currentModifier)) {
                currentModifier = (VariableModifier) defaultModifier.Clone();
                this._currentVariableModifiers.Add(id, currentModifier);
            }

            // format variable as modifier value
            int? currentValue = currentModifier.GetByTarget(target);
            VariableListItem newItem = new VariableListItem(id,
                    VariableClass.FormatUnscaled(currentValue, target),
                    VariableClass.FormatUnscaled(defaultValue.Value, true), target);

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
        #region SelectFrameOffset

        /// <summary>
        /// Selects the <see cref="ImageListBox"/> item on the <see cref="FrameTab"/> page indicated
        /// by the <see cref="EntityTemplate.FrameOffset"/> of the edited <see
        /// cref="EntityTemplate"/>.</summary>
        /// <remarks>
        /// <b>SelectFrameOffset</b> does nothing if the <see cref="ImageListBox"/> is empty, and
        /// otherwise restricts the <b>FrameOffset</b> to a legal item index.</remarks>

        private void SelectFrameOffset() {
            int count = FrameList.Items.Count;
            if (count > 0) {
                int offset = this._template.FrameOffset;
                FrameList.SelectAndShow(Math.Max(0, Math.Min(count, offset)));
            }
        }

        #endregion
        #region ResetVariables

        /// <summary>
        /// Resets all current values in the "Variables" <see cref="ListView"/> on the <see
        /// cref="VariablesTab"/> page to their default values.</summary>
        /// <remarks>
        /// <b>ResetVariables</b> copies the "Default Value" column to the "Value" column for all
        /// items in the "Variables" list view, and then reselects the currently selected item to
        /// update all other controls.</remarks>

        private void ResetVariables() {
            if (VariableList.Items.Count == 0) return;

            // reset all current values to their default values
            for (int i = 0; i < VariableList.Items.Count; i++) {
                VariableListItem item = (VariableListItem) VariableList.Items[i];
                item.Value = item.DefaultValue;
            }
            VariableList.Items.Refresh();

            // reselect current selection to update other controls
            int index = VariableList.SelectedIndex;
            VariableList.SelectedIndex = -1;
            VariableList.SelectAndShow(Math.Max(0, index));
        }

        #endregion
        #region UpdateVariables

        /// <summary>
        /// Updates the specified template variables with the basic values of the specified dialog
        /// variables, and sets the <see cref="DataChanged"/> flag if they differ.</summary>
        /// <param name="classValues">
        /// One of the dictionaries holding the default variable values of the <see
        /// cref="EntityClass"/> associated with the <see cref="EntityTemplate"/> being edited.
        /// </param>
        /// <param name="templateValues">
        /// One of the dictionaries holding the variable values stored in the <see
        /// cref="EntityTemplate"/> being edited.</param>
        /// <param name="dialogValues">
        /// One of the dictionaries holding the variable values entered by the user on the <see
        /// cref="VariablesTab"/> page.</param>
        /// <remarks>
        /// Before updating the specified <paramref name="templateValues"/>, <b>UpdateVariables</b>
        /// removes all key-and-value pairs from the specified <paramref name="dialogValues"/> that
        /// also appear in the specified <paramref name="classValues"/>.</remarks>

        private void UpdateVariables(VariableValueDictionary classValues,
            VariableValueDictionary templateValues, VariableValueDictionary dialogValues) {

            foreach (var pair in classValues)
                dialogValues.Remove(pair);

            if (!templateValues.Equals(dialogValues))
                DataChanged = true;

            templateValues.Clear();
            templateValues.AddRange(dialogValues);
        }

        #endregion
        #region UpdateVariableModifiers

        /// <summary>
        /// Updates the specified template variables with the modifier values of the specified
        /// dialog variables, and sets the <see cref="DataChanged"/> flag if they differ.</summary>
        /// <param name="classValues">
        /// One of the dictionaries holding the default variable values of the <see
        /// cref="EntityClass"/> associated with the <see cref="EntityTemplate"/> being edited.
        /// </param>
        /// <param name="templateValues">
        /// One of the dictionaries holding the variable values stored in the <see
        /// cref="EntityTemplate"/> being edited.</param>
        /// <param name="dialogValues">
        /// One of the dictionaries holding the variable values entered by the user on the <see
        /// cref="VariablesTab"/> page.</param>
        /// <remarks>
        /// Before updating the specified <paramref name="templateValues"/>, <b>UpdateVariables</b>
        /// removes all key-and-value pairs from the specified <paramref name="dialogValues"/> that
        /// also appear in the specified <paramref name="classValues"/>.</remarks>

        private void UpdateVariableModifiers(VariableModifierDictionary classValues,
            VariableModifierDictionary templateValues, VariableModifierDictionary dialogValues) {

            foreach (var pair in classValues)
                dialogValues.Remove(pair);

            if (!templateValues.Equals(dialogValues))
                DataChanged = true;

            templateValues.Clear();
            templateValues.AddRange(dialogValues);
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
        /// page of the <see cref="ChangeTemplate"/> dialog.</remarks>

        private void HelpExecuted(object sender, ExecutedRoutedEventArgs args) {
            args.Handled = true;

            // default to dialog help page
            string helpPage = "DlgChangeTemplate.html";

            // show help for specific tab page
            if (TextTab.IsSelected)
                helpPage = "DlgChangeTemplateText.html";
            else if (FrameTab.IsSelected)
                helpPage = "DlgChangeTemplateFrame.html";
            else if (VariablesTab.IsSelected)
                helpPage = "DlgChangeTemplateVars.html";

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
        /// cref="EntityTemplate"/> object supplied to the constructor, and sets the <see
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
                EditorUtility.ValidateVariables(this, this._entityResourceModifiers);

            if (!variablesValid) {
                args.Cancel = true;
                return;
            }

            // read name box into instance name property
            string name = NameBox.Text;
            if (this._template.Name != name) {
                this._template.Name = name;
                DataChanged = true;
            }

            // read visibility option into entity template
            bool? isVisible = VisibleToggle.IsChecked;
            if (this._template.IsVisible != isVisible) {
                this._template.IsVisible = isVisible;
                DataChanged = true;
            }

            // read frame controls into template offset & random flag
            if (this._entityClass.ImageAnimation == AnimationMode.None
                && this._entityClass.FrameCount > 1) {

                int offset = 0;
                bool random = false;
                
                if (RandomFrameToggle.IsChecked == true)
                    random = true;
                else
                    offset = Math.Max(0, FrameList.SelectedIndex);

                if (offset != this._template.FrameOffset ||
                    random != this._template.UseRandomFrame) {

                    DataChanged = true;
                    this._template.FrameOffset = offset;
                    this._template.UseRandomFrame = random;
                }
            }

            // read variable values into entity template collections
            UpdateVariables(this._entityClass.Attributes,
                this._template.Attributes, this._entityAttributes);

            UpdateVariableModifiers(this._entityClass.AttributeModifiers,
                this._template.AttributeModifiers, this._entityAttributeModifiers);

            UpdateVariables(this._entityClass.Counters,
                this._template.Counters, this._entityCounters);

            UpdateVariables(this._entityClass.Resources,
                this._template.Resources, this._entityResources);

            UpdateVariableModifiers(this._entityClass.ResourceModifiers,
                this._template.ResourceModifiers, this._entityResourceModifiers);
        }

        #endregion
        #region OnFrameActivate

        /// <summary>
        /// Handles the <see cref="Control.MouseDoubleClick"/> event for an <see
        /// cref="ImageListBoxItem"/> of the <see cref="ImageListBox"/> on the <see
        /// cref="FrameTab"/> page.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="MouseButtonEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnFrameActivate</b> selects the double-clicked item in the <see
        /// cref="ImageListBox"/>, and sets the <see cref="Window.DialogResult"/> of the <see
        /// cref="ChangeFrame"/> dialog to <c>true</c> to confirm all changes.</remarks>

        private void OnFrameActivate(object sender, MouseButtonEventArgs args) {
            args.Handled = true;

            // retrieve double-clicked item, if any
            var source = args.OriginalSource as DependencyObject;
            var item = ItemsControl.ContainerFromElement(FrameList, source) as ImageListBoxItem;

            if (item != null) {
                FrameList.SelectAndShow(item);
                DialogResult = true;
            }
        }

        #endregion
        #region OnFrameSelected

        /// <summary>
        /// Handles the <see cref="Selector.SelectionChanged"/> event for the <see
        /// cref="ImageListBox"/> on the <see cref="FrameTab"/> page.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="SelectionChangedEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnFrameSelected</b> unchecks the "Random Frame" check box if the current <see
        /// cref="Selector.SelectedIndex"/> is valid.</remarks>

        private void OnFrameSelected(object sender, SelectionChangedEventArgs args) {
            args.Handled = true;
            if (this._initialized && FrameList.SelectedIndex >= 0)
                RandomFrameToggle.IsChecked = false;
        }

        #endregion
        #region OnNameClear

        /// <summary>
        /// Handles the <see cref="ButtonBase.Click"/> event for the "Clear Instance Name" <see
        /// cref="Button"/> on the <see cref="TextTab"/> page.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="RoutedEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnNameClear</b> clears the contents of the "Instance Name" text box.</remarks>

        private void OnNameClear(object sender, RoutedEventArgs args) {
            args.Handled = true;
            NameBox.Clear();
        }

        #endregion
        #region OnRandomFrame

        /// <summary>
        /// Handles the <see cref="ButtonBase.Click"/> event for the "Random Frame" <see
        /// cref="CheckBox"/> on the <see cref="FrameTab"/> page.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="RoutedEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnRandomFrame</b> clears the selected item of the <see cref="ImageListBox"/> if the
        /// "Random Frame" option is checked, and otherwise resets an invalid item selection to the
        /// original <see cref="EntityTemplate.FrameOffset"/>.</remarks>

        private void OnRandomFrame(object sender, RoutedEventArgs args) {
            args.Handled = true;
            if (!this._initialized) return;

            if (RandomFrameToggle.IsChecked == true)
                FrameList.SelectedIndex = -1;
            else if (FrameList.SelectedIndex < 0)
                SelectFrameOffset();
        }

        #endregion
        #region OnResetCategory

        /// <summary>
        /// Handles the <see cref="ButtonBase.Click"/> event for the "Reset Category" <see
        /// cref="Button"/> on the <see cref="VariablesTab"/> page.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="RoutedEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnResetCategory</b> resets the current values of all variables in the selected
        /// category to their default values and updates all controls accordingly.</remarks>

        private void OnResetCategory(object sender, RoutedEventArgs args) {
            args.Handled = true;

            // reset current dictionary to its default values
            if (this._currentVariables != null)
                this._currentVariables.Clear();
            if (this._currentVariableModifiers != null)
                this._currentVariableModifiers.Clear();

            ResetVariables();
        }

        #endregion
        #region OnResetGlobal

        /// <summary>
        /// Handles the <see cref="ButtonBase.Click"/> event for the "Reset Globally" <see
        /// cref="Button"/> on the <see cref="VariablesTab"/> page.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="RoutedEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnResetGlobal</b> resets <em>all</em> current variable values of the edited <see
        /// cref="EntityTemplate"/> to their default values and updates all controls accordingly.
        /// </remarks>

        private void OnResetGlobal(object sender, RoutedEventArgs args) {
            args.Handled = true;

            // reset all dictionaries to their default values
            this._entityAttributes.Clear();
            this._entityAttributeModifiers.Clear();
            this._entityCounters.Clear();
            this._entityResources.Clear();
            this._entityResourceModifiers.Clear();

            ResetVariables();
        }

        #endregion
        #region OnResetVariable

        /// <summary>
        /// Handles the <see cref="ButtonBase.Click"/> event for the "Reset Variable" <see
        /// cref="Button"/> on the <see cref="VariablesTab"/> page.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="RoutedEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnResetValue</b> resets the current value of the first selected item in the
        /// "Variables" list view to its default value and updates all controls accordingly.
        /// </remarks>

        private void OnResetVariable(object sender, RoutedEventArgs args) {
            args.Handled = true;

            // retrieve selected variable, if any
            if (VariableList.SelectedIndex < 0) return;
            var item = (VariableListItem) VariableList.SelectedItem;

            // extract decimal default value and modifier flag
            bool isModifier;
            decimal defaultValue = VariableClass.ParseUnscaled(item.DefaultValue, out isModifier);
            Debug.Assert(item.Target.HasValue == isModifier);

            /*
             * Changing the numeric control also updates the Variables list view
             * and the backing collection, thanks to the OnVariableChanged event.
             */

            if (VariableUpDown.Value != defaultValue)
                VariableUpDown.Value = defaultValue;
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

            // switch category (default to Attribute)
            IList<String> variableKeys;
            if (AttributeToggle.IsChecked == true) {
                variableKeys = MasterSection.Instance.Variables.Attributes.Keys;
                this._currentVariables = this._entityAttributes;
                this._currentVariableModifiers = this._entityAttributeModifiers;
                this._defaultVariables = this._defaultAttributes;
                this._defaultVariableModifiers = this._defaultAttributeModifiers;
            }
            else if (CounterToggle.IsChecked == true) {
                variableKeys = MasterSection.Instance.Variables.Counters.Keys;
                this._currentVariables = this._entityCounters;
                this._currentVariableModifiers = null;
                this._defaultVariables = this._defaultCounters;
                this._defaultVariableModifiers = null;
            }
            else if (ResourceToggle.IsChecked == true) {
                variableKeys = MasterSection.Instance.Variables.Resources.Keys;
                this._currentVariables = this._entityResources;
                this._currentVariableModifiers = this._entityResourceModifiers;
                this._defaultVariables = this._defaultResources;
                this._defaultVariableModifiers = this._defaultResourceModifiers;
            }
            else {
                Debug.Fail("OnVariableCategory: No Category button checked.");
                variableKeys = MasterSection.Instance.Variables.Attributes.Keys;
                this._currentVariables = this._entityAttributes;
                this._currentVariableModifiers = this._entityAttributeModifiers;
                this._defaultVariables = this._defaultAttributes;
                this._defaultVariableModifiers = this._defaultAttributeModifiers;
            }

            // add template values if defined
            VariableList.Items.Clear();
            foreach (string id in variableKeys) {
                CreateVariableItem(id);
                if (this._entityAttributeModifiers != null)
                    foreach (ModifierTarget target in VariableModifier.AllModifierTargets)
                        CreateVariableItem(id, target);
            }

            // select first variable, if any, and update controls
            bool anyVariables = (VariableList.Items.Count > 0);
            if (anyVariables)
                VariableList.SelectedIndex = 0;
            else
                VariableUpDown.Value = 0m;

            ResetVariableButton.IsEnabled = anyVariables;
            ResetCategoryButton.IsEnabled = anyVariables;
            VariableUpDown.Enabled = anyVariables;
        }

        #endregion
        #region OnVariableChanged

        /// <summary>
        /// Handles the <see cref="NumericUpDown.ValueChanged"/> event for the "Value" <see
        /// cref="NumericUpDown"/> control on the <see cref="VariablesTab"/> page.</summary>
        /// <param name="sender">
        /// The <see cref="NumericUpDown"/> control sending the event.</param>
        /// <param name="args">
        /// An <see cref="EventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnVariableChanged</b> updates the "Value" column of the first selected item in the
        /// "Variables" list view, as well as the corresponding backing dictionary.</remarks>

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
        /// <b>OnVariableSelected</b> updates the "Value" control with the data of the first
        /// selected item in the "Variables" list view.</remarks>

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
        #region OnVariableWidthChanged

        /// <summary>
        /// Handles changes to the <see cref="FrameworkElement.ActualWidth"/> property of the
        /// "Variable" <see cref="ListView"/> on the <see cref="VariablesTab"/> page.</summary>
        /// <param name="sender">
        /// The <see cref="ListView"/> control sending the event.</param>
        /// <param name="args">
        /// An <see cref="EventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnVariableWidthChanged</b> resizes the first two columns of the "Variable" list view
        /// on the <see cref="VariablesTab"/> page to share the available list view width.</remarks>

        private void OnVariableWidthChanged(object sender, EventArgs args) {

            double width = (VariableList.ActualWidth -VariableDefaultColumn.ActualWidth - 28) / 2.0;
            if (width > 0) {
                VariableColumn.Width = width;
                VariableValueColumn.Width = width;
            }
        }

        #endregion
        #endregion
    }
}
