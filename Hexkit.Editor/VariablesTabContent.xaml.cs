using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

using Tektosyne;
using Tektosyne.Collections;
using Tektosyne.Windows;
using Hexkit.Scenario;

namespace Hexkit.Editor {
    #region Type Aliases

    using VariableClassDictionary = SortedListEx<String, VariableClass>;

    #endregion

    /// <summary>
    /// Provides the "Variables" tab page for the Hexkit Editor application.</summary>
    /// <remarks>
    /// Please refer to the "Variables Page" page of the "Editor Display" section in the application
    /// help file for details on this tab page.</remarks>

    public partial class VariablesTabContent: UserControl, IEditorTabContent {
        #region VariablesTabContent()

        /// <summary>
        /// Initializes a new instance of the <see cref="VariablesTabContent"/> class.</summary>

        public VariablesTabContent() {
            InitializeComponent();

            // adjust column widths of Variable list view
            DependencyPropertyDescriptor.FromProperty(
                ListView.ActualWidthProperty, typeof(ListView))
                .AddValueChanged(VariableList, OnVariableWidthChanged);
        }

        #endregion
        #region Private Fields

        // property backers
        private SectionTabItem _sectionTab;

        #endregion
        #region Private Members
        #region CurrentCategory

        /// <summary>
        /// Gets or sets the currently selected <see cref="VariableCategory"/>.</summary>
        /// <value>
        /// A <see cref="VariableCategory"/> value indicating the current selection in the
        /// "Category" <see cref="GroupBox"/>.</value>

        private VariableCategory CurrentCategory { get; set; }

        #endregion
        #region CurrentDefaultId

        /// <summary>
        /// Gets the default identifier for a new <see cref="VariableClass"/> of the <see
        /// cref="CurrentCategory"/>.</summary>
        /// <value>
        /// The default value for the <see cref="VariableClass.Id"/> string of a new <see
        /// cref="VariableClass"/> instance, given the <see cref="CurrentCategory"/>.</value>
        /// <exception cref="PropertyValueException">
        /// <see cref="CurrentCategory"/> is not a valid <see cref="VariableCategory"/> value.
        /// </exception>

        private string CurrentDefaultId {
            get {
                switch (CurrentCategory) {

                    case VariableCategory.Attribute: return "attribute-id";
                    case VariableCategory.Counter:   return "counter-id";
                    case VariableCategory.Resource:  return "resource-id";

                    default:
                        ThrowHelper.ThrowPropertyValueException(
                            "CurrentCategory", CurrentCategory, null);
                        return null;
                }
            }
        }

        #endregion
        #region CurrentVariables

        /// <summary>
        /// Gets the <see cref="VariableSection"/> collection for the <see cref="CurrentCategory"/>.
        /// </summary>
        /// <value>
        /// The result of <see cref="VariableSection.GetVariables"/> for the current <see
        /// cref="VariableSection"/> and the <see cref="CurrentCategory"/>.</value>

        private VariableClassDictionary CurrentVariables {
            get { return MasterSection.Instance.Variables.GetVariables(CurrentCategory); }
        }

        #endregion
        #region ChangeVariable

        /// <summary>
        /// Allows the user to change the specified <see cref="VariableClass"/>.</summary>
        /// <param name="variable">
        /// The <see cref="VariableClass"/> to change.</param>
        /// <remarks><para>
        /// <b>ChangeVariable</b> displays a <see cref="Dialog.ChangeVariable"/> dialog for the
        /// specified <paramref name="variable"/>.
        /// </para><para>
        /// If the user made any changes, <b>ChangeVariable</b> propagates them to the <see
        /// cref="CurrentVariables"/> collection and sets the <see
        /// cref="SectionTabItem.DataChanged"/> flag.</para></remarks>

        private void ChangeVariable(VariableClass variable) {
            if (variable == null) return;

            // show dialog and let user make changes
            var dialog = new Dialog.ChangeVariable(variable) { Owner = MainWindow.Instance };
            dialog.ShowDialog();

            // broadcast data changes, if any
            if (dialog.DataChanged) {
                VariableList.Items.Refresh();
                SectionTab.DataChanged = true;
            }
        }

        #endregion
        #region EnableListButtons

        /// <summary>
        /// Enables or disables the "Change ID", "Change Variable", and "Remove Variable" <see
        /// cref="Button"/> controls, depending on whether the "Variable" <see cref="ListView"/>
        /// contains any items.</summary>

        private void EnableListButtons() {
            bool anyVariables = (VariableList.Items.Count > 0);

            // enable or disable list control buttons
            ChangeIdButton.IsEnabled = anyVariables;
            ChangeVariableButton.IsEnabled = anyVariables;
            RemoveVariableButton.IsEnabled = anyVariables;
        }

        #endregion
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
        /// cref="VariableCategory"/> value that corresponds to the selected radio button, and then
        /// reloads the "Variable" list view with corresponding <see cref="CurrentVariables"/>.
        /// </remarks>

        private void OnCategoryChecked(object sender, EventArgs args) {
            RoutedEventArgs routedArgs = args as RoutedEventArgs;
            if (routedArgs != null) {
                routedArgs.Handled = true;
                sender = routedArgs.Source;
            }

            // change category if sent by radio button
            if (sender == AttributeToggle)
                CurrentCategory = VariableCategory.Attribute;
            else if (sender == CounterToggle)
                CurrentCategory = VariableCategory.Counter;
            else if (sender == ResourceToggle)
                CurrentCategory = VariableCategory.Resource;

            // update variable list and buttons
            VariableList.ItemsSource = CurrentVariables.Values;
            EnableListButtons();

            // select first item by default
            if (VariableList.Items.Count > 0)
                VariableList.SelectedIndex = 0;
        }

        #endregion
        #region OnVariableActivate

        /// <summary>
        /// Handles the <see cref="Control.MouseDoubleClick"/> event for a <see
        /// cref="ListViewItem"/> of the "Variable" <see cref="ListView"/>.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="MouseButtonEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnVariableActivate</b> calls <see cref="ChangeVariable"/> with the double-clicked
        /// item in the "Variable" list view.</remarks>

        private void OnVariableActivate(object sender, MouseButtonEventArgs args) {
            args.Handled = true;

            // retrieve double-clicked item, if any
            var source = args.OriginalSource as DependencyObject;
            var item = ItemsControl.ContainerFromElement(VariableList, source) as ListViewItem;
            if (item != null) ChangeVariable(item.Content as VariableClass);
        }

        #endregion
        #region OnVariableAdd

        /// <summary>
        /// Handles the <see cref="ButtonBase.Click"/> event for the "Add Variable" <see
        /// cref="Button"/>.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="RoutedEventArgs"/> object containing event data.</param>
        /// <remarks><para>
        /// <b>OnVariableAdd</b> displays a <see cref="Dialog.ChangeIdentifier"/> dialog, followed
        /// by a <see cref="Dialog.ChangeVariable"/> dialog, allowing the user to define a new
        /// variable. The new variable copies the properties of the first selected item in the
        /// "Variable" list view, if any; otherwise, it is created with default properties.
        /// </para><para>
        /// If the user confirmed both dialogs, <b>OnVariableAdd</b> adds the new variable to the
        /// "Variable" list view and to the <see cref="CurrentVariables"/> collection, and sets the
        /// <see cref="SectionTabItem.DataChanged"/> flag.</para></remarks>

        private void OnVariableAdd(object sender, RoutedEventArgs args) {
            args.Handled = true;

            // ask user for new variable ID
            var variables = CurrentVariables;
            var dialog = new Dialog.ChangeIdentifier(CurrentDefaultId,
                Global.Strings.TitleVariableIdEnter, variables.ContainsKey, false);
            dialog.Owner = MainWindow.Instance;
            if (dialog.ShowDialog() != true) return;

            // retrieve new variable ID
            string id = String.Intern(dialog.Identifier);

            // create new variable based on selected variable, if any
            VariableClass variable, selection = VariableList.SelectedItem as VariableClass;
            if (selection == null)
                variable = VariableClass.Create(id, CurrentCategory);
            else {
                variable = (VariableClass) selection.Clone();
                variable.Id = id;
            }

            // let user make changes to new variable
            var variableDialog = new Dialog.ChangeVariable(variable) { Owner = MainWindow.Instance };
            if (variableDialog.ShowDialog() != true) return;

            // add variable to section table
            variables.Add(id, variable);

            // update list view and select new item
            VariableList.Items.Refresh();
            VariableList.SelectAndShow(variable);

            // broadcast data changes
            EnableListButtons();
            SectionTab.DataChanged = true;
        }

        #endregion
        #region OnVariableChange

        /// <summary>
        /// Handles the <see cref="ButtonBase.Click"/> event for the "Change Variable" <see
        /// cref="Button"/>.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="RoutedEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnVariableChange</b> calls <see cref="ChangeVariable"/> with the first selected item
        /// in the "Variable" list view.</remarks>

        private void OnVariableChange(object sender, RoutedEventArgs args) {
            args.Handled = true;
            ChangeVariable(VariableList.SelectedItem as VariableClass);
        }

        #endregion
        #region OnVariableId

        /// <summary>
        /// Handles the <see cref="ButtonBase.Click"/> event for the "Change ID" <see
        /// cref="Button"/>.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="RoutedEventArgs"/> object containing event data.</param>
        /// <remarks><para>
        /// <b>OnVariableId</b> displays a <see cref="Dialog.ChangeIdentifier"/> dialog for the
        /// first selected item in the "Variable" list view.
        /// </para><para>
        /// If the user made any changes, <b>OnVariableId</b> propagates them to the <see
        /// cref="CurrentVariables"/> collection and sets the <see
        /// cref="SectionTabItem.DataChanged"/> flag.</para></remarks>

        private void OnVariableId(object sender, RoutedEventArgs args) {
            args.Handled = true;

            // retrieve selected variable, if any
            VariableClass variable = VariableList.SelectedItem as VariableClass;
            if (variable == null) return;

            // let user enter new variable ID
            var variables = CurrentVariables;
            var dialog = new Dialog.ChangeIdentifier(variable.Id,
                Global.Strings.TitleVariableIdChange, variables.ContainsKey, true);
            dialog.Owner = MainWindow.Instance;
            if (dialog.ShowDialog() != true) return;

            // retrieve new faction ID
            string id = String.Intern(dialog.Identifier);

            // change existing ID references
            if (!SectionTabItem.ProcessAllIdentifiers(variables, variable.Id, id))
                return;

            // broadcast data changes
            VariableList.Items.Refresh();
            SectionTab.DataChanged = true;
        }

        #endregion
        #region OnVariableRemove

        /// <summary>
        /// Handles the <see cref="ButtonBase.Click"/> event for the "Remove Variable" <see
        /// cref="Button"/>.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="RoutedEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnVariableRemove</b> removes the first selected item in the "Variable" list view from
        /// that list view and from the <see cref="CurrentVariables"/> collection, and sets the <see
        /// cref="SectionTabItem.DataChanged"/> flag.</remarks>

        private void OnVariableRemove(object sender, RoutedEventArgs args) {
            args.Handled = true;

            // retrieve selected faction, if any
            int index = VariableList.SelectedIndex;
            if (index < 0) return;
            VariableClass variable = (VariableClass) VariableList.Items[index];

            // delete existing ID references
            var variables = CurrentVariables;
            if (!SectionTabItem.ProcessAllIdentifiers(variables, variable.Id, null))
                return;

            // select item in the same position
            VariableList.Items.Refresh();
            if (VariableList.Items.Count > 0)
                VariableList.SelectAndShow(Math.Min(VariableList.Items.Count - 1, index));

            // broadcast data changes
            EnableListButtons();
            SectionTab.DataChanged = true;
        }

        #endregion
        #region OnVariableWidthChanged

        /// <summary>
        /// Handles changes to the <see cref="FrameworkElement.ActualWidth"/> property of the
        /// "Variable" <see cref="ListView"/>.</summary>
        /// <param name="sender">
        /// The <see cref="ListView"/> control sending the event.</param>
        /// <param name="args">
        /// An <see cref="EventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnVariableWidthChanged</b> resizes both columns of the "Variable" list view so that
        /// each occupies the same share of the current list view width.</remarks>

        private void OnVariableWidthChanged(object sender, EventArgs args) {

            double width = (VariableList.ActualWidth - 28) / 2.0;
            if (width > 0) {
                VariableIdColumn.Width = width;
                VariableNameColumn.Width = width;
            }
        }

        #endregion
        #endregion
        #region IEditorTabContent Members
        #region Section

        /// <summary>
        /// Gets the <see cref="ScenarioSection"/> managed by the tab page.</summary>
        /// <value>
        /// The constant value <see cref="ScenarioSection.Variables"/>, indicating the Hexkit
        /// scenario section managed by the "Variables" tab page.</value>

        public ScenarioSection Section {
            get { return ScenarioSection.Variables; }
        }

        #endregion
        #region SectionTab

        /// <summary>
        /// Gets or sets the <see cref="SectionTabItem"/> for the tab page.</summary>
        /// <value>
        /// The <see cref="SectionTabItem"/> that contains the <see cref="VariablesTabContent"/>
        /// control, i.e. the "Variables" tab page of the Hexkit Editor application.</value>
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
        /// <b>Initialize</b> initializes all controls that are specific to the "Variables" tab
        /// page.</remarks>

        public void Initialize() {

            // show attributes by default
            if (AttributeToggle.IsChecked != true)
                AttributeToggle.IsChecked = true;
            else {
                // force control update if Attribute already checked
                OnCategoryChecked(AttributeToggle, EventArgs.Empty);
            }
        }

        #endregion
        #endregion
    }
}
