using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

using Tektosyne;
using Tektosyne.Windows;

using Hexkit.Global;
using Hexkit.Scenario;

namespace Hexkit.Game.Dialog {

    /// <summary>
    /// Shows a dialog with information on all variables in the current scenario.</summary>
    /// <remarks>
    /// Please refer to the "Variables" page of the "Game Dialogs" section in the application help
    /// file for details on this dialog.</remarks>

    public partial class ShowVariables: Window {
        #region ShowVariables(VariableClass)

        /// <summary>
        /// Initializes a new instance of the <see cref="ShowVariables"/> class with the specified
        /// initially selected <see cref="VariableClass"/>.</summary>
        /// <param name="variable"><para>
        /// The <see cref="VariableClass"/> to select initially.
        /// </para><para>-or-</para><para>
        /// A null reference to select the first <see cref="AttributeClass"/> or <see
        /// cref="ResourceClass"/>, in that order.</para></param>
        /// <exception cref="ArgumentException">
        /// <paramref name="variable"/> is neither a null reference nor an element of the <see
        /// cref="VariableSection"/> collection that matches its <see
        /// cref="VariableClass.Category"/>.</exception>

        public ShowVariables(VariableClass variable) {

            if (variable != null) {
                var dictionary =  MasterSection.Instance.Variables.GetVariables(variable.Category);

                // specified variable must be part of its collection
                if (!dictionary.ContainsKey(variable.Id))
                    ThrowHelper.ThrowArgumentException(
                        "variable", Global.Strings.ArgumentNotNullOrVariable);

                this._variable = variable;
            }

            InitializeComponent();

            // adjust column width of Variable list view
            DependencyPropertyDescriptor.FromProperty(
                ListView.ActualWidthProperty, typeof(ListView))
                .AddValueChanged(VariableList, OnVariableWidthChanged);
        }

        #endregion
        #region Private Fields

        // construction parameters
        private readonly VariableClass _variable = null;

        #endregion
        #region Private Methods
        #region SelectVariable

        /// <summary>
        /// Checks the "Category" <see cref="RadioButton"/> that corresponds to the specified <see
        /// cref="VariableClass"/>, which is also selected in the "Variable" <see cref="ListView"/>.
        /// </summary>
        /// <param name="variable">
        /// The <see cref="VariableClass"/> to select.</param>
        /// <exception cref="InvalidEnumArgumentException">
        /// <paramref name="variable"/> specifies an invalid <see cref="VariableClass.Category"/>
        /// value.</exception>
        /// <remarks>
        /// Checking a "Category" radio button automatically shows the corresponding <see
        /// cref="VariableClass"/> objects in the "Variable" list view, via <see
        /// cref="OnCategoryChecked"/>.</remarks>

        private void SelectVariable(VariableClass variable) {
            switch (variable.Category) {

                case VariableCategory.Attribute:
                    AttributeToggle.IsChecked = true;
                    break;

                case VariableCategory.Resource:
                    ResourceToggle.IsChecked = true;
                    break;

                default:
                    ThrowHelper.ThrowInvalidEnumArgumentException("variable.Category",
                        (int) variable.Category, typeof(VariableCategory));
                    break;
            }

            // list view was initialized by checking radio button
            VariableList.SelectAndShow(variable);
        }

        #endregion
        #endregion
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
        /// cref="ShowVariables"/> dialog.</remarks>

        private void HelpExecuted(object sender, ExecutedRoutedEventArgs args) {
            args.Handled = true;
            ApplicationUtility.ShowHelp("DlgShowVariables.html");
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
        /// <b>OnCategoryChecked</b> updates the "Variable" list view to reflect the selected radio
        /// button and selects the first item, if any, which automatically updates all other dialog
        /// controls.</remarks>

        private void OnCategoryChecked(object sender, RoutedEventArgs args) {
            args.Handled = true;
            IList<VariableClass> variables = null;

            if (AttributeToggle.IsChecked == true)
                variables = MasterSection.Instance.Variables.Attributes.Values;
            else if (ResourceToggle.IsChecked == true)
                variables = MasterSection.Instance.Variables.Resources.Values;
            else
                Debug.Fail("ShowVariables.OnCategoryChecked: No Category button checked.");

            // show classes of selected category
            VariableList.ItemsSource = variables;

            // select first list entry, if any
            if (VariableList.Items.Count > 0)
                VariableList.SelectedIndex = 0;
            else {
                // inform user why list is empty
                VariableInfo.Text = Global.Strings.InfoVariableCategoryEmpty;
            }
        }

        #endregion
        #region OnContentRendered

        /// <summary>
        /// Raises and handles the <see cref="Window.ContentRendered"/> event.</summary>
        /// <param name="args">
        /// An <see cref="EventArgs"/> object containing event data.</param>
        /// <remarks><para>
        /// <b>OnContentRendered</b> raises the <see cref="Window.ContentRendered"/> event by
        /// calling the base class implementation of <see cref="Window.OnContentRendered"/>.
        /// </para><para>
        /// <b>OnContentRendered</b> then handles the <see cref="Window.ContentRendered"/> event by
        /// selecting either the first <see cref="AttributeClass"/> or <see cref="ResourceClass"/>
        /// in the "Variable" list view, or the <see cref="VariableClass"/> that was supplied to the
        /// constructor, if any.</para></remarks>

        protected override void OnContentRendered(EventArgs args) {
            base.OnContentRendered(args);

            if (this._variable == null) {
                // select first attribute or resource
                if (MasterSection.Instance.Variables.Attributes.Count > 0)
                    AttributeToggle.IsChecked = true;
                else if (MasterSection.Instance.Variables.Resources.Count > 0)
                    ResourceToggle.IsChecked = true;
                else
                    AttributeToggle.IsChecked = true;
            } else {
                // select specified variable
                SelectVariable(this._variable);
            }
        }

        #endregion
        #region OnVariableSelected

        /// <summary>
        /// Handles the <see cref="Selector.SelectionChanged"/> event for the "Variable" <see
        /// cref="ListView"/>.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="SelectionChangedEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnVariableSelected</b> updates all dialog controls to reflect the selected item in
        /// the "Variable" list view.</remarks>

        private void OnVariableSelected(object sender, SelectionChangedEventArgs args) {
            args.Handled = true;

            // clear data display
            VariableInfo.Clear();
            MinimumInfo.Clear();
            MaximumInfo.Clear();
            StepSizeInfo.Clear();

            // disable resource display
            ResourceGroup.IsEnabled = false;
            DefeatInfo.Text = "—";  // em dash
            VictoryInfo.Text = "—"; // em dash
            ResetToggle.IsChecked = false;
            LimitToggle.IsChecked = false;

            // retrieve selected item, if any
            VariableClass variable = VariableList.SelectedItem as VariableClass;
            if (variable == null) return;

            // show target range and step size
            MinimumInfo.Text = variable.Format(variable.Minimum, false);
            MaximumInfo.Text = variable.Format(variable.Maximum, false);
            StepSizeInfo.Text = variable.Format(1, false);

            // show additional data for resources
            ResourceClass resource = variable as ResourceClass;
            if (resource != null) {
                ResourceGroup.IsEnabled = true;

                ResetToggle.IsChecked = resource.IsResetting;
                LimitToggle.IsChecked = resource.IsLimited;

                if (resource.Defeat != Int32.MinValue)
                    DefeatInfo.Text = resource.Format(resource.Defeat, false);

                if (resource.Victory != Int32.MaxValue)
                    VictoryInfo.Text = resource.Format(resource.Victory, false);
            }

            // show associated informational text
            VariableInfo.Text = String.Join(Environment.NewLine, variable.Paragraphs);
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
        /// <b>OnVariableWidthChanged</b> resizes the only column of the "Variable" list view to the
        /// current list view width.</remarks>

        private void OnVariableWidthChanged(object sender, EventArgs args) {

            double width = VariableList.ActualWidth - 28;
            if (width > 0) VariableColumn.Width = width;
        }

        #endregion
    }
}
