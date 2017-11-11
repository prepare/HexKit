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

using Hexkit.Global;
using Hexkit.Options;
using Hexkit.Scenario;

namespace Hexkit.Graphics {
    #region Type Aliases

    using VariableClassDictionary = SortedListEx<String, VariableClass>;

    #endregion

    /// <summary>
    /// Shows a dialog allowing the user to select a variable and display flags for site overlays.
    /// </summary>
    /// <remarks>
    /// Please refer to the "Show Variable" page of the "Game Dialogs" or "Editor Dialogs" section
    /// in the application help file for details on this dialog.</remarks>

    public partial class ShowVariable: Window {
        #region ShowVariable(VariableClass, VariableDisplay)

        /// <summary>
        /// Initializes a new instance of the <see cref="ShowVariable"/> class with the specified
        /// initially selected <see cref="VariableClass"/> and <see cref="VariableDisplay"/> flags.
        /// </summary>
        /// <param name="variable"><para>
        /// The <see cref="VariableClass"/> to select initially.
        /// </para><para>-or-</para><para>
        /// A null reference to select the first <see cref="AttributeClass"/>, <see
        /// cref="ResourceClass"/>, or <see cref="CounterClass"/>, in that order.</para></param>
        /// <param name="flags">
        /// A <see cref="VariableDisplay"/> value indicating which display flags to select
        /// initially.</param>
        /// <exception cref="ArgumentException">
        /// <paramref name="variable"/> is neither a null reference nor an element of the <see
        /// cref="VariableSection"/> collection that matches its <see
        /// cref="VariableClass.Category"/>.</exception>
        /// <exception cref="InvalidEnumArgumentException">
        /// <paramref name="variable"/> specifies an invalid <see cref="VariableClass.Category"/>.
        /// </exception>

        public ShowVariable(VariableClass variable, VariableDisplay flags) {
            InitializeComponent();

            if (variable != null) {
                VariableSection variables = MasterSection.Instance.Variables;
                var dictionary = variables.GetVariables(variable.Category);

                // specified variable must be part of its collection
                if (!dictionary.ContainsKey(variable.Id))
                    ThrowHelper.ThrowArgumentException(
                        "variable", Global.Strings.ArgumentNotNullOrVariable);
            }

            Variable = variable;
            VariableFlags = flags;

            // read specified display flags into check boxes
            if ((flags & VariableDisplay.Basic) != 0)
                BasicToggle.IsChecked = true;
            if ((flags & VariableDisplay.Modifier) != 0)
                ModifierToggle.IsChecked = true;
            if ((flags & VariableDisplay.Numbers) != 0)
                NumbersToggle.IsChecked = true;
            if ((flags & VariableDisplay.Shades) != 0)
                ShadesToggle.IsChecked = true;

            // adjust column width of Variable list view
            DependencyPropertyDescriptor.FromProperty(
                ListView.ActualWidthProperty, typeof(ListView))
                .AddValueChanged(VariableList, OnVariableWidthChanged);

            if (variable != null) {
                // select specified variable, if any
                switch (variable.Category) {

                    case VariableCategory.Attribute:
                        AttributeToggle.IsChecked = true;
                        break;

                    case VariableCategory.Counter:
                        CounterToggle.IsChecked = true;
                        break;

                    case VariableCategory.Resource:
                        ResourceToggle.IsChecked = true;
                        break;

                    default:
                        ThrowHelper.ThrowInvalidEnumArgumentException("variable.Category",
                            (int) variable.Category, typeof(VariableCategory));
                        break;
                }

                VariableList.SelectAndShow(variable);
            }
            else {
                // select category with defined variables
                if (MasterSection.Instance.Variables.Attributes.Count > 0)
                    AttributeToggle.IsChecked = true;
                else if (MasterSection.Instance.Variables.Resources.Count > 0)
                    ResourceToggle.IsChecked = true;
                else if (MasterSection.Instance.Variables.Counters.Count > 0)
                    CounterToggle.IsChecked = true;
                else
                    AttributeToggle.IsChecked = true;
            }
        }

        #endregion
        #region Variable

        /// <summary>
        /// Gets the <see cref="VariableClass"/> selected by the user. </summary>
        /// <value><para>
        /// The <see cref="VariableClass"/> selected by the user.
        /// </para><para>-or-</para><para>
        /// A null reference to indicate that no variable values should be shown.</para></value>
        /// <remarks>
        /// <b>Variable</b> returns a null reference if the current scenario does not define any
        /// variables.</remarks>

        public VariableClass Variable { get; private set; }

        #endregion
        #region VariableFlags

        /// <summary>
        /// Gets the <see cref="VariableDisplay"/> flags selected by the user for the current <see
        /// cref="Variable"/>.</summary>
        /// <value>
        /// A <see cref="VariableDisplay"/> value containing the display flags selected by the user.
        /// </value>
        /// <remarks>
        /// <b>VariableFlags</b> may return zero to indicate that no <see cref="Variable"/> values
        /// should be shown. In that case, <b>Variable</b> returns a null reference.</remarks>

        public VariableDisplay VariableFlags { get; private set; }

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
        /// cref="ShowVariable"/> dialog.</remarks>

        private void HelpExecuted(object sender, ExecutedRoutedEventArgs args) {
            args.Handled = true;
            ApplicationUtility.ShowHelp("DlgShowVariable.html");
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
        #region OnCategoryChecked

        /// <summary>
        /// Handles the <see cref="ToggleButton.Checked"/> event for all "Category" <see
        /// cref="RadioButton"/> controls.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="RoutedEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnCategoryChecked</b> updates the "Variables" list view to reflect the checked radio
        /// button.</remarks>

        private void OnCategoryChecked(object sender, RoutedEventArgs args) {
            args.Handled = true;
            VariableClassDictionary variables = null;

            if (AttributeToggle.IsChecked == true)
                variables = MasterSection.Instance.Variables.Attributes;
            else if (CounterToggle.IsChecked == true)
                variables = MasterSection.Instance.Variables.Counters;
            else if (ResourceToggle.IsChecked == true)
                variables = MasterSection.Instance.Variables.Resources;
            else {
                Debug.Fail("ShowVariable.OnCategoryChecked: No Category button checked.");
                variables = MasterSection.Instance.Variables.Attributes;
            }

            // show variables of selected category
            VariableList.Items.Clear();
            foreach (VariableClass variable in variables.Values)
                VariableList.Items.Add(variable);

            // select first list entry, if any
            if (VariableList.Items.Count > 0)
                VariableList.SelectedIndex = 0;
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
        /// handles the <see cref="Window.Closing"/> as follows:
        /// </para><para>
        /// If the <see cref="Window.DialogResult"/> is not <c>true</c>, indicating that the user
        /// cancelled the dialog and wants to discard all changes, <b>OnClosing</b> quits
        /// immediately.
        /// </para><para>
        /// Otherwise, <b>OnClosing</b> reads the control contents of this dialog into the <see
        /// cref="Variable"/> and <see cref="VariableFlags"/> properties.</para></remarks>

        protected override void OnClosing(CancelEventArgs args) {
            base.OnClosing(args);
            if (args.Cancel) return;

            // user cancelled dialog, ignore changes
            if (DialogResult != true) return;

            // read selected variable into Variable property
            Variable = VariableList.SelectedItem as VariableClass;

            // read selected display mode into VariableFlags property
            VariableFlags = 0;
            if (BasicToggle.IsChecked == true)
                VariableFlags |= VariableDisplay.Basic;
            if (ModifierToggle.IsChecked == true)
                VariableFlags |= VariableDisplay.Modifier;
            if (NumbersToggle.IsChecked == true)
                VariableFlags |= VariableDisplay.Numbers;
            if (ShadesToggle.IsChecked == true)
                VariableFlags |= VariableDisplay.Shades;

            // clear both properties if no valid display mode selected
            if ((VariableFlags & VariableDisplay.BasicAndModifier) == 0 ||
                (VariableFlags & VariableDisplay.NumbersAndShades) == 0 ||
                ((VariableFlags & VariableDisplay.Basic) == 0 &&
                Variable.Category == VariableCategory.Counter)) {

                Variable = null;
                VariableFlags = 0;
            }
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
        /// <b>OnVariableWidthChanged</b> resizes the column of the "Variable" list view to the
        /// current list view width.</remarks>

        private void OnVariableWidthChanged(object sender, EventArgs args) {
            double width = VariableList.ActualWidth - 28;
            if (width > 0) VariableColumn.Width = width;
        }

        #endregion
    }
}
