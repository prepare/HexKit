using System;
using System.Windows.Controls;

using Hexkit.Scenario;

namespace Hexkit.Editor.Dialog {

    /// <summary>
    /// Provides the <see cref="ContentControl.Content"/> of a <see cref="ListViewItem"/> in the
    /// "Variable" <see cref="ListView"/> controls of several Hexkit Editor dialogs.</summary>
    /// <remarks>
    /// <b>VariableListItem</b> provides the column data for all <see cref="ItemsControl.Items"/> in
    /// the "Variable" list views of the <see cref="ChangeEntity"/>, <see cref="ChangeFaction"/>,
    /// and <see cref="ChangeTemplate"/> dialogs.</remarks>

    public class VariableListItem {
        #region VariableListItem(String, String, ModifierTarget)

        /// <overloads>
        /// Initializes a new instance of the <see cref="VariableListItem"/> class.</overloads>
        /// <summary>
        /// Initializes a new instance of the <see cref="VariableListItem"/> class with the
        /// specified identifier, current value, and <see cref="ModifierTarget"/>.</summary>
        /// <param name="id">
        /// The initial value for the <see cref="Id"/> property.</param>
        /// <param name="value">
        /// The initial value for the <see cref="Value"/> property.</param>
        /// <param name="target">
        /// The initial value for the <see cref="Target"/> property.</param>

        public VariableListItem(string id, string value, ModifierTarget? target) {

            Id = id;
            Value = value;
            Target = target;
        }

        #endregion
        #region VariableListItem(String, String, String, ModifierTarget)

        /// <summary>
        /// Initializes a new instance of the <see cref="VariableListItem"/> class with the
        /// specified identifier, current and default value, and <see cref="ModifierTarget"/>.
        /// </summary>
        /// <param name="id">
        /// The initial value for the <see cref="Id"/> property.</param>
        /// <param name="value">
        /// The initial value for the <see cref="Value"/> property.</param>
        /// <param name="defaultValue">
        /// The initial value for the <see cref="DefaultValue"/> property.</param>
        /// <param name="target">
        /// The initial value for the <see cref="Target"/> property.</param>

        public VariableListItem(string id, string value,
            string defaultValue, ModifierTarget? target) {

            Id = id;
            Value = value;
            DefaultValue = defaultValue;
            Target = target;
        }

        #endregion
        #region DefaultValue

        /// <summary>
        /// Gets or sets the default value for the associated <see cref="VariableClass"/>.</summary>
        /// <value>
        /// A <see cref="String"/> representation of the default value for the associated <see
        /// cref="VariableClass"/>. The default is a null reference.</value>
        /// <remarks>
        /// <b>DefaultValue</b> is used only by the <see cref="ChangeTemplate"/> dialog and holds
        /// the default value defined by the underlying <see cref="EntityClass"/> for the associated
        /// <see cref="VariableClass"/>.</remarks>

        public string DefaultValue { get; set; }

        #endregion
        #region Id

        /// <summary>
        /// Gets the identifier of the <see cref="VariableClass"/> represented by the <see
        /// cref="VariableListItem"/>.</summary>
        /// <value>
        /// The <see cref="VariableClass.Id"/> string of the <see cref="VariableClass"/> represented
        /// by the <see cref="VariableListItem"/>.</value>

        public string Id { get; private set; }

        #endregion
        #region Target

        /// <summary>
        /// Gets the <see cref="ModifierTarget"/> for the associated <see cref="VariableClass"/>.
        /// </summary>
        /// <value><para>
        /// A <see cref="ModifierTarget"/> value indicating the modifier value defined by <see
        /// cref="Value"/> and <see cref="DefaultValue"/>.
        /// </para><para>-or-</para><para>
        /// A null reference if <see cref="Value"/> and <see cref="DefaultValue"/> define a basic
        /// value for the associated <see cref="VariableClass"/>.</para></value>

        public ModifierTarget? Target { get; private set; }

        #endregion
        #region Value

        /// <summary>
        /// Gets or sets the current value for the associated <see cref="VariableClass"/>.</summary>
        /// <value>
        /// A <see cref="String"/> representation of the current value for the associated <see
        /// cref="VariableClass"/>. The default is a null reference.</value>

        public string Value { get; set; }

        #endregion
    }
}
