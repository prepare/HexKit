using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

using Tektosyne;
using Tektosyne.Windows;

using Hexkit.Global;
using Hexkit.Scenario;

namespace Hexkit.Editor.Dialog {
    #region Type Aliases

    using NumericUpDown = System.Windows.Forms.NumericUpDown;

    #endregion

    /// <summary>
    /// Shows a dialog allowing the user to change a <see cref="VariableClass"/>.</summary>
    /// <remarks>
    /// Please refer to the "Change Variable" page of the "Editor Dialogs" section in the
    /// application help file for details on this dialog.</remarks>

    public partial class ChangeVariable: Window {
        #region ChangeVariable(VariableClass)

        /// <summary>
        /// Initializes a new instance of the <see cref="ChangeVariable"/> class with the specified
        /// <see cref="VariableClass"/>.</summary>
        /// <param name="variable">
        /// The <see cref="VariableClass"/> whose data to change.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="variable"/> is a null reference.</exception>
        /// <remarks>
        /// The data of the specified <paramref name="variable"/> may be changed in the dialog, as
        /// indicated by the value of the <see cref="DataChanged"/> property.</remarks>

        public ChangeVariable(VariableClass variable) {
            if (variable == null)
                ThrowHelper.ThrowArgumentNullException("variable");

            this._variable = variable;
            this._resource = variable as ResourceClass; // may be null

            InitializeComponent();
            Title = Title + variable.Id;

            // assign strings to text controls
            NameBox.Text = this._variable.Name;
            DetailBox.Text = String.Join(Environment.NewLine, this._variable.Paragraphs);

            // retrieve maximum variable range
            int min = VariableClass.AbsoluteMinimum;
            int max = VariableClass.AbsoluteMaximum;

            // most numeric controls use maximum range
            MinimumUpDown.Minimum = min; MinimumUpDown.Maximum = max;
            MaximumUpDown.Minimum = min; MaximumUpDown.Maximum = max;
            DefeatUpDown.Minimum = min; DefeatUpDown.Maximum = max;
            VictoryUpDown.Minimum = min; VictoryUpDown.Maximum = max;

            // assign checked values to range and scale controls
            MinimumUpDown.Value = Math.Max(min, Math.Min(max, this._variable.Minimum));
            MaximumUpDown.Value = Math.Max(min, Math.Min(max, this._variable.Maximum));
            ScaleUpDown.Value = Math.Max(1, Math.Min(100, this._variable.Scale));

            // disable Resource Options if not needed
            if (this._resource == null)
                ResourceOptions.IsEnabled = false;
            else {
                // assign resource flags to check boxes
                ResetToggle.IsChecked = this._resource.IsResetting;
                LimitToggle.IsChecked = this._resource.IsLimited;
                DepleteToggle.IsChecked = this._resource.IsDepletable;

                // assign Defeat value or disable control
                if (this._resource.Defeat != Int32.MinValue) {
                    DefeatToggle.IsChecked = true;
                    DefeatUpDown.Value = Math.Max(min, Math.Min(max, this._resource.Defeat));
                } else
                    DefeatUpDown.Enabled = false;

                // assign Victory value or disable control
                if (this._resource.Victory != Int32.MaxValue) {
                    VictoryToggle.IsChecked = true;
                    VictoryUpDown.Value = Math.Max(min, Math.Min(max, this._resource.Victory));
                } else
                    VictoryUpDown.Enabled = false;
            } 

            // construction completed
            this._initialized = true;
        }

        #endregion
        #region Private Fields

        // construction parameters
        private readonly VariableClass _variable;
        private readonly ResourceClass _resource;

        // was construction completed?
        private readonly bool _initialized = false;

        #endregion
        #region DataChanged

        /// <summary>
        /// Gets a value indicating whether any data was changed in the dialog.</summary>
        /// <value>
        /// <c>true</c> if any of the objects supplied to the constructor have been modified;
        /// otherwise, <c>false</c>.</value>
        /// <remarks>
        /// <b>DataChanged</b> returns <c>false</c> if the objects supplied to the constructor were
        /// not modified in any detectable way. However, the original data may have been overwritten
        /// with a copy that is not detectably different, namely if the user clicked <b>OK</b>
        /// without making any changes.</remarks>

        public bool DataChanged { get; private set; }

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
        /// cref="ChangeVariable"/> dialog.</remarks>

        private void HelpExecuted(object sender, ExecutedRoutedEventArgs args) {
            args.Handled = true;
            ApplicationUtility.ShowHelp("DlgChangeVariable.html");
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
        /// cref="VariableClass"/> supplied to the constructor.</para></remarks>

        protected override void OnClosing(CancelEventArgs args) {
            base.OnClosing(args);
            if (args.Cancel) return;

            // user cancelled dialog, ignore changes
            if (DialogResult != true) {
                DataChanged = false;
                return;
            }

            // read name box into name property
            this._variable.Name = NameBox.Text;

            // read numeric controls into range and scale properties
            this._variable.Minimum = (int) MinimumUpDown.Value;
            this._variable.Maximum = (int) MaximumUpDown.Value;
            this._variable.Scale = (int) ScaleUpDown.Value;

            // read resource-specific settings
            if (this._resource != null) {
                this._resource.IsResetting = (ResetToggle.IsChecked == true);
                this._resource.IsLimited = (LimitToggle.IsChecked == true);
                this._resource.IsDepletable = (DepleteToggle.IsChecked == true);

                // read numeric control or use default value
                this._resource.Defeat = (DefeatToggle.IsChecked == true ?
                    (int) DefeatUpDown.Value : Int32.MinValue);

                // read numeric control or use default value
                this._resource.Victory = (VictoryToggle.IsChecked == true ?
                    (int) VictoryUpDown.Value : Int32.MaxValue);
            }

            // read detail paragraphs into string collection
            this._variable.Paragraphs.Clear();
            this._variable.Paragraphs.AddRange(DetailBox.Text.Split(
                new string[] { Environment.NewLine }, StringSplitOptions.None));
        }

        #endregion
        #region OnOptionChanged

        /// <summary>
        /// Handles the <see cref="ButtonBase.Click"/> event for all <see cref="CheckBox"/>
        /// controls.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// An <see cref="RoutedEventArgs"/> object containing event data.</param>
        /// <remarks><para>
        /// If the specified <paramref name="sender"/> is the "Defeat" or "Victory" check box,
        /// <b>OnOptionChanged</b> enables or disables the <see cref="NumericUpDown"/> control
        /// associated with it, depending on its <see cref="ToggleButton.IsChecked"/> state, and
        /// then calls <see cref="OnValueChanged"/> with that <see cref="NumericUpDown"/> control to
        /// set the <see cref="DataChanged"/> flag if necessary.
        /// </para><para>
        /// Otherwise, <b>OnOptionChanged</b> sets the <see cref="DataChanged"/> flag if the check
        /// state of the specified <paramref name="sender"/> differs from the corresponding value of
        /// the <see cref="VariableClass"/> being edited.</para></remarks>

        private void OnOptionChanged(object sender, RoutedEventArgs args) {
            args.Handled = true;
            if (!this._initialized) return;

            CheckBox checkBox = (CheckBox) sender;
            bool isChecked = (checkBox.IsChecked == true);

            if (checkBox == DefeatToggle) {
                DefeatUpDown.Enabled = isChecked;
                OnValueChanged(DefeatUpDown, EventArgs.Empty);
            }
            else if (checkBox == VictoryToggle) {
                VictoryUpDown.Enabled = isChecked;
                OnValueChanged(VictoryUpDown, EventArgs.Empty);
            }
            else if (this._resource != null) {
                if (checkBox == ResetToggle)
                    DataChanged |= (this._resource.IsResetting != isChecked);
                else if (checkBox == LimitToggle)
                    DataChanged |= (this._resource.IsLimited != isChecked);
                else if (checkBox == DepleteToggle)
                    DataChanged |= (this._resource.IsDepletable != isChecked);
            }
        }

        #endregion
        #region OnTextChanged

        /// <summary>
        /// Handles the <see cref="TextBoxBase.TextChanged"/> event for the "Variable Name" and
        /// "Informational Text" <see cref="TextBox"/> controls.</summary>
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
        #region OnValueChanged

        /// <summary>
        /// Handles the <see cref="NumericUpDown.ValueChanged"/> event for all <see
        /// cref="NumericUpDown"/> controls.</summary>
        /// <param name="sender">
        /// The <see cref="NumericUpDown"/> control sending the event.</param>
        /// <param name="args">
        /// An <see cref="EventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnValueChanged</b> sets the <see cref="DataChanged"/> flag if the <see
        /// cref="NumericUpDown.Value"/> of the specified <paramref name="sender"/> differs from the
        /// corresponding value of the <see cref="VariableClass"/> being edited.</remarks>

        private void OnValueChanged(object sender, EventArgs args) {
            if (!this._initialized) return;

            decimal value = ((NumericUpDownEx) sender).Value;
            bool changed = false;

            // determine if sender contains changed data
            if (sender == MinimumUpDown.HostedControl)
                changed = (this._variable.Minimum != value);
            else if (sender == MaximumUpDown.HostedControl)
                changed = (this._variable.Maximum != value);
            else if (sender == ScaleUpDown.HostedControl)
                changed = (this._variable.Scale != value);
            else if (this._resource != null) {
                if (sender == DefeatUpDown.HostedControl) {
                    changed = (this._resource.Defeat !=
                        (DefeatToggle.IsChecked == true ? value : Int32.MinValue));
                } else if (sender == VictoryUpDown.HostedControl) {
                    changed = (this._resource.Victory !=
                        (VictoryToggle.IsChecked == true ? value : Int32.MaxValue));
                }
            }

            // set DataChanged flag accordingly
            if (changed) DataChanged = true;
        }

        #endregion
    }
}
