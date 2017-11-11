using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

using Tektosyne.Windows;

using Hexkit.Global;
using Hexkit.Options;
using Hexkit.Scenario;

namespace Hexkit.Graphics {

    /// <summary>
    /// Shows a dialog allowing the user to select a resource and display flags for depletion
    /// gauges.</summary>
    /// <remarks>
    /// Please refer to the "Show Gauges" page of the "Game Dialogs" or "Editor Dialogs" section in
    /// the application help file for details on this dialog.</remarks>

    public partial class ShowGauges: Window {
        #region ShowGauges(String, GaugeDisplay)

        /// <summary>
        /// Initializes a new instance of the <see cref="ShowGauges"/> class with the specified
        /// initially selected <see cref="ResourceClass"/> and <see cref="GaugeDisplay"/> flags.
        /// </summary>
        /// <param name="resource"><para>
        /// The identifier of the <see cref="ResourceClass"/> to select initially. Possible values
        /// include the pseudo-resources <see cref="ResourceClass.StandardMorale"/> and <see
        /// cref="ResourceClass.StandardStrength"/>.
        /// </para><para>-or-</para><para>
        /// A null reference to select the <see cref="ResourceClass.StandardStrength"/>
        /// pseudo-resource.</para></param>
        /// <param name="flags">
        /// A <see cref="GaugeDisplay"/> value indicating which display flags to select initially.
        /// </param>

        public ShowGauges(string resource, GaugeDisplay flags) {
            InitializeComponent();

            Resource = resource;
            ResourceFlags = flags;

            // read specified display flags into check boxes
            NeverToggle.IsChecked = String.IsNullOrEmpty(resource);
            AlwaysToggle.IsChecked = ((flags & GaugeDisplay.Always) != 0);
            StackToggle.IsChecked = ((flags & GaugeDisplay.Stack) != 0);

            // adjust column width of Resource list view
            DependencyPropertyDescriptor.FromProperty(
                ListView.ActualWidthProperty, typeof(ListView))
                .AddValueChanged(VariableList, OnVariableWidthChanged);

            // show standard unit resources
            VariableList.Items.Add(ResourceClass.StandardStrength);
            VariableList.Items.Add(ResourceClass.StandardMorale);
            VariableList.AddSeparator();

            // show all scenario resources
            foreach (VariableClass variable in MasterSection.Instance.Variables.Resources.Values)
                VariableList.Items.Add(variable);

            // select specified resource, if any
            if (resource != null)
                foreach (object item in VariableList.Items) {
                    VariableClass variable = item as VariableClass;
                    if (variable != null && variable.Id == resource) {
                        VariableList.SelectAndShow(variable);
                        break;
                    }
                }

            // select standard strength by default
            if (VariableList.SelectedItems.Count == 0)
                VariableList.SelectAndShow(0);
        }

        #endregion
        #region Resource

        /// <summary>
        /// Gets the identifier of the <see cref="ResourceClass"/> selected by the user.</summary>
        /// <value><para>
        /// The identifier of the <see cref="ResourceClass"/> selected by the user. Possible values
        /// include the pseudo-resources <see cref="ResourceClass.StandardMorale"/> and <see
        /// cref="ResourceClass.StandardStrength"/>.
        /// </para><para>-or-</para><para>
        /// A null reference to indicate that no resource gauges should be shown.</para></value>

        public string Resource { get; private set; }

        #endregion
        #region ResourceFlags

        /// <summary>
        /// Gets the <see cref="GaugeDisplay"/> flags selected by the user for the current <see
        /// cref="Resource"/>.</summary>
        /// <value>
        /// A <see cref="GaugeDisplay"/> value containing the display flags selected by the user.
        /// </value>

        public GaugeDisplay ResourceFlags { get; private set; }

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
        /// <b>HelpExecuted</b> opens the application help file on the help page for the <see
        /// cref="ShowGauges"/> dialog.</remarks>

        private void HelpExecuted(object sender, ExecutedRoutedEventArgs args) {
            args.Handled = true;
            ApplicationUtility.ShowHelp("DlgShowGauges.html");
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
        /// handles the <see cref="Window.Closing"/> as follows:
        /// </para><para>
        /// If the <see cref="Window.DialogResult"/> is not <c>true</c>, indicating that the user
        /// cancelled the dialog and wants to discard all changes, <b>OnClosing</b> quits
        /// immediately.
        /// </para><para>
        /// Otherwise, <b>OnClosing</b> reads the control contents of this dialog into the <see
        /// cref="Resource"/> and <see cref="ResourceFlags"/> properties.</para></remarks>

        protected override void OnClosing(CancelEventArgs args) {
            base.OnClosing(args);
            if (args.Cancel) return;

            // user cancelled dialog, ignore changes
            if (DialogResult != true) return;

            // read selected resource into Resource property
            Resource = null;
            if (NeverToggle.IsChecked == false) {
                var variable = VariableList.SelectedItem as VariableClass;
                if (variable != null) Resource = variable.Id;
            }

            // read selected display mode into ResourceFlags property
            ResourceFlags = 0;
            if (AlwaysToggle.IsChecked == true)
                ResourceFlags |= GaugeDisplay.Always;
            if (StackToggle.IsChecked == true)
                ResourceFlags |= GaugeDisplay.Stack;
        }

        #endregion
        #region OnVariableWidthChanged

        /// <summary>
        /// Handles changes to the <see cref="FrameworkElement.ActualWidth"/> property of the
        /// "Resource" <see cref="ListView"/>.</summary>
        /// <param name="sender">
        /// The <see cref="ListView"/> control sending the event.</param>
        /// <param name="args">
        /// An <see cref="EventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnVariableWidthChanged</b> resizes the column of the "Resource" list view to the
        /// current list view width.</remarks>

        private void OnVariableWidthChanged(object sender, EventArgs args) {
            double width = VariableList.ActualWidth - 28;
            if (width > 0) VariableColumn.Width = width;
        }

        #endregion
        #endregion
    }
}
