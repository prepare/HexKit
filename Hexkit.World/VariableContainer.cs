using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;

using Tektosyne;
using Tektosyne.Collections;
using Hexkit.Scenario;

namespace Hexkit.World {
    #region Type Aliases

    using VariableList = KeyedList<String, Variable>;
    using VariableValueDictionary = SortedListEx<String, Int32>;

    #endregion

    /// <summary>
    /// Provides a safe and efficient wrapper around a <see cref="Variable"/> collection.</summary>
    /// <remarks><para>
    /// <b>VariableContainer</b> provides safe access, lazy allocation, and contextual information
    /// for a wrapped <see cref="Variable"/> collection.
    /// </para><list type="bullet"><item>
    /// The contained <see cref="VariableContainer.Variables"/> are exposed through a read-only
    /// wrapper. Write access is possible only through two internal methods, <see
    /// cref="VariableContainer.SetValue"/> and <see cref="VariableContainer.ImportChanges"/>. This
    /// ensures that rule scripts use the appropriate HCL instructions to change faction or entity
    /// variables.
    /// </item><item>
    /// When a <b>VariableContainer</b> is created with an existing <b>Variable</b> collection, the
    /// container initially stores a reference to the existing collection. A new backing collection
    /// is allocated only when <b>SetValue</b> or <b>UpdateValues</b> attempts to modify a variable
    /// value. This lazy allocation is essential to reduce the number of copy operations when
    /// cloning world states.
    /// </item><item>
    /// <b>VariableContainer</b> stores the <see cref="VariableContainer.Category"/> and <see
    /// cref="VariableContainer.Purpose"/> for the contained variables. This information is
    /// available even if the container is empty, and incompatible variables are rejected.
    /// </item></list></remarks>

    public class VariableContainer: ICloneable {
        #region VariableContainer(VariableCategory, VariablePurpose)

        /// <overloads>
        /// Initializes a new instance of the <see cref="VariableContainer"/> class.</overloads>
        /// <summary>
        /// Initializes a new instance of the <see cref="VariableContainer"/> class with the
        /// specified category and purpose.</summary>
        /// <param name="category">
        /// The initial value for the <see cref="Category"/> property.</param>
        /// <param name="purpose">
        /// The initial value for the <see cref="Purpose"/> property.</param>
        /// <exception cref="InvalidEnumArgumentException">
        /// <paramref name="purpose"/> is not a valid bitwise combination of <see
        /// cref="VariablePurpose"/> values.</exception>
        /// <remarks>
        /// The <see cref="Variables"/> property is initialized to an empty read-only <see
        /// cref="VariableList"/> that is shared among all <see cref="VariableContainer"/>
        /// instances.</remarks>

        internal VariableContainer(VariableCategory category, VariablePurpose purpose) {

            if (!Variable.CheckPurpose(purpose))
                ThrowHelper.ThrowInvalidEnumArgumentException(
                    "purpose", (int) purpose, typeof(VariablePurpose));

            this._category = category;
            this._purpose = (purpose & ~VariablePurpose.Scenario);
            this._variables = VariableList.Empty;
        }

        #endregion
        #region VariableContainer(VariableCategory, VariablePurpose, VariableValueDictionary)

        /// <summary>
        /// Initializes a new instance of the <see cref="VariableContainer"/> class with the
        /// specified category, purpose, and initial <see cref="Int32"/> values.</summary>
        /// <param name="category">
        /// The initial value for the <see cref="Category"/> property.</param>
        /// <param name="purpose">
        /// The initial value for the <see cref="Purpose"/> property.</param>
        /// <param name="values">
        /// A <see cref="VariableValueDictionary"/> containing the initial values for the <see
        /// cref="Variables"/> collection.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="values"/> is a null reference.</exception>
        /// <exception cref="InvalidEnumArgumentException">
        /// <paramref name="purpose"/> is not a valid bitwise combination of <see
        /// cref="VariablePurpose"/> values.</exception>
        /// <remarks>
        /// The <see cref="Variables"/> property is initialized to the result of <see
        /// cref="Variable.CreateCollection"/> for the specified <paramref name="values"/>,
        /// <paramref name="category"/>, and <paramref name="purpose"/>.</remarks>

        internal VariableContainer(VariableCategory category, VariablePurpose purpose,
            VariableValueDictionary values): this(category, purpose) {

            if (values == null)
                ThrowHelper.ThrowArgumentNullException("values");

            // create writable collection from initial values
            this._variables = Variable.CreateCollection(values, category, purpose, false);
        }

        #endregion
        #region VariableContainer(VariableCategory, VariablePurpose, VariableList)

        /// <summary>
        /// Initializes a new instance of the <see cref="VariableContainer"/> class with the
        /// specified category, purpose, and initial <see cref="Variable"/> values.</summary>
        /// <param name="category">
        /// The initial value for the <see cref="Category"/> property.</param>
        /// <param name="purpose">
        /// The initial value for the <see cref="Purpose"/> property.</param>
        /// <param name="variables">
        /// The initial value for the <see cref="Variables"/> property.</param>
        /// <exception cref="ArgumentException"><para>
        /// <paramref name="variables"/> is not read-only.
        /// </para><para>-or-</para><para>
        /// <paramref name="variables"/> contains elements whose <see cref="Variable.Category"/> is
        /// different from the specified <paramref name="category"/>.
        /// </para><para>-or-</para><para>
        /// <paramref name="variables"/> contains elements whose <see cref="Variable.Purpose"/> is
        /// different from the specified <paramref name="purpose"/>.</para></exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="variables"/> is a null reference.</exception>
        /// <exception cref="InvalidEnumArgumentException">
        /// <paramref name="purpose"/> is not a valid bitwise combination of <see
        /// cref="VariablePurpose"/> values.</exception>
        /// <remarks>
        /// <see cref="VariableContainer"/> delays copying of the specified <paramref
        /// name="variables"/> until <see cref="SetValue"/> attempts to change an element. It is the
        /// caller's responsibility to ensure that the specified collection never changes.</remarks>

        internal VariableContainer(VariableCategory category, VariablePurpose purpose,
            VariableList variables): this(category, purpose) {

            SetVariables(variables);
        }

        #endregion
        #region VariableContainer(VariableContainer)

        /// <summary>
        /// Initializes a new instance of the <see cref="VariableContainer"/> class that is a deep
        /// copy of the specified instance.</summary>
        /// <param name="container">
        /// The <see cref="VariableContainer"/> object whose properties should be copied to the new
        /// instance.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="container"/> is a null reference.</exception>
        /// <remarks>
        /// This constructor is called by <see cref="Clone"/> to perform a deep copy of the
        /// specified <paramref name="container"/>. Please refer to that method for further details.
        /// </remarks>

        private VariableContainer(VariableContainer container) {
            if (container == null)
                ThrowHelper.ThrowArgumentNullException("container");

            this._category = container._category;
            this._purpose = container._purpose;

            // create deep copy only for writable collections
            this._variables = (container._variables.IsReadOnly ?
                container._variables : container._variables.Copy());
        }

        #endregion
        #region Private Fields

        // property backers
        private readonly VariableCategory _category;
        private readonly VariablePurpose _purpose;
        private VariableList _variables;

        #endregion
        #region Category

        /// <summary>
        /// Gets the category of all <see cref="Variable"/> values.</summary>
        /// <value>
        /// The value of the <see cref="Scenario.VariableClass.Category"/> property of the
        /// underlying <see cref="Variable.VariableClass"/> for any <see cref="Variables"/> element.
        /// </value>
        /// <remarks>
        /// <b>Category</b> never changes once the object has been constructed.</remarks>

        public VariableCategory Category {
            [DebuggerStepThrough]
            get { return this._category; }
        }

        #endregion
        #region Count

        /// <summary>
        /// Gets the number of elements contained in the <see cref="Variables"/> collection.
        /// </summary>
        /// <value>
        /// The number of elements contained in the <see cref="Variables"/> collection.</value>
        /// <remarks>
        /// <b>Count</b> returns the value of the <see cref="ICollection{T}.Count"/> property of the
        /// <see cref="Variables"/> collection.</remarks>

        public int Count {
            [DebuggerStepThrough]
            get { return Variables.Count; }
        }

        #endregion
        #region IsModifier

        /// <summary>
        /// Gets a value indicating whether all <see cref="Variable"/> values are modifiers.
        /// </summary>
        /// <value>
        /// <c>true</c> if all elements in the <see cref="Variables"/> collection represent <see
        /// cref="VariablePurpose.Modifier"/> values; <c>false</c> if they represent <see
        /// cref="VariablePurpose.Basic"/> values.</value>
        /// <remarks>
        /// <b>IsModifier</b> never changes once the object has been constructed.</remarks>

        public bool IsModifier {
            [DebuggerStepThrough]
            get { return ((Purpose & VariablePurpose.Modifier) != 0); }
        }

        #endregion
        #region Item[Int32]

        /// <overloads>
        /// Gets a specific <see cref="Variables"/> element.</overloads>
        /// <summary>
        /// Gets the <see cref="Variables"/> element at the specified index.</summary>
        /// <param name="index">
        /// The zero-based index of the <see cref="Variables"/> element to get.</param>
        /// <value>
        /// The <see cref="Variables"/> element at the specified <paramref name="index"/>.</value>
        /// <exception cref="ArgumentOutOfRangeException"><para>
        /// <paramref name="index"/> is less than zero.
        /// </para><para>-or-</para><para>
        /// <paramref name="index"/> is equal to or greater than <see cref="Count"/>.
        /// </para></exception>
        /// <remarks>
        /// This indexer has the same effect as the equivalent indexer of the <see
        /// cref="Variables"/> collection.</remarks>

        public Variable this[int index] {
            [DebuggerStepThrough]
            get { return Variables[index]; }
        }

        #endregion
        #region Item[String]

        /// <summary>
        /// Gets the <see cref="Variables"/> element associated with the specified identifier, or a
        /// null reference if not found.</summary>
        /// <param name="id">
        /// The <see cref="Variable.Id"/> string whose <see cref="Variables"/> element to get.
        /// </param>
        /// <value>
        /// The <see cref="Variables"/> element with the specified <paramref name="id"/>, if found;
        /// otherwise, a null reference.</value>
        /// <exception cref="ArgumentNullOrEmptyException">
        /// <paramref name="id"/> is a null reference or an empty string.</exception>
        /// <remarks>
        /// This indexer has the same effect as the equivalent indexer of the <see
        /// cref="Variables"/> collection, but returns a null reference instead of throwing a <see
        /// cref="KeyNotFoundException"/> if the specified <paramref name="id"/> is not found.
        /// </remarks>

        public Variable this[string id] {
            get {
                if (String.IsNullOrEmpty(id))
                    ThrowHelper.ThrowArgumentNullOrEmptyException("id");

                Variable variable;
                Variables.TryGetValue(id, out variable);
                return variable;
            }
        }

        #endregion
        #region Purpose

        /// <summary>
        /// Gets the purpose of all <see cref="Variable"/> values.</summary>
        /// <value>
        /// A <see cref="VariablePurpose"/> value indicating the purpose of all elements in the <see
        /// cref="Variables"/> collection.</value>
        /// <remarks><para>
        /// <b>Purpose</b> never changes once the object has been constructed. This property always
        /// contains one of the valid bitwise combinations defined for the <see
        /// cref="VariablePurpose"/> enumeration.
        /// </para><para>
        /// <b>Purpose</b> never contains the <see cref="VariablePurpose.Scenario"/> flag, even if
        /// it was supplied to the constructor. This flag is communicated to all initial elements of
        /// the <see cref="Variables"/> collection, however.</para></remarks>

        public VariablePurpose Purpose {
            [DebuggerStepThrough]
            get { return this._purpose; }
        }

        #endregion
        #region Variables

        /// <summary>
        /// The <see cref="Variable"/> list managed by the <see cref="VariableContainer"/>.
        /// </summary>
        /// <value>
        /// A read-only <see cref="VariableList"/> containing the <see cref="Variable"/> objects
        /// managed by the <see cref="VariableContainer"/>.</value>
        /// <remarks>
        /// <b>Variables</b> never returns a null reference. All keys are unique. Use the <see
        /// cref="SetValue"/> method to add or change a <see cref="Variable"/> value.</remarks>

        public VariableList Variables {
            [DebuggerStepThrough]
            get { return this._variables.AsReadOnly(); }
        }

        #endregion
        #region Internal Methods
        #region ExportChanges

        /// <summary>
        /// Exports any changes in the <see cref="Variables"/> collection, relative to the specified
        /// source dictionary, to the specified target dictionary.</summary>
        /// <param name="sourceDictionary">
        /// The <see cref="VariableValueDictionary"/> to examine for changes.</param>
        /// <param name="targetDictionary">
        /// The <see cref="VariableValueDictionary"/> that receives the changes.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="sourceDictionary"/> or <paramref name="targetDictionary"/> is a null
        /// reference.</exception>
        /// <remarks>
        /// <b>ExportChanges</b> compares all key-and-value pairs in the specified <paramref
        /// name="sourceDictionary"/> to the current contents of the <see cref="Variables"/>
        /// collection. Any key that exists with a different value in the <see cref="Variables"/>
        /// collection is added to the specified <paramref name="targetDictionary"/> with its
        /// current <see cref="Variables"/> value.</remarks>

        internal void ExportChanges(VariableValueDictionary sourceDictionary,
            VariableValueDictionary targetDictionary) {

            if (sourceDictionary == null)
                ThrowHelper.ThrowArgumentNullException("sourceDictionary");
            if (targetDictionary == null)
                ThrowHelper.ThrowArgumentNullException("targetDictionary");

            Variable variable;
            foreach (var pair in sourceDictionary)
                if (Variables.TryGetValue(pair.Key, out variable) && variable.Value != pair.Value)
                    targetDictionary[pair.Key] = variable.Value;
        }

        #endregion
        #region ImportChanges

        /// <summary>
        /// Imports any changed values from the specified dictionary into the <see
        /// cref="Variables"/> collection.</summary>
        /// <param name="dictionary">
        /// A <see cref="VariableValueDictionary"/> that maps <see cref="VariableClass.Id"/> strings
        /// of <see cref="VariableClass"/> objects to <see cref="Int32"/> values.</param>
        /// <returns>
        /// <c>true</c> if the <see cref="Variables"/> collection was changed; otherwise,
        /// <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="dictionary"/> is a null reference.</exception>
        /// <remarks><para>
        /// <b>ImportChanges</b> sets all <see cref="Variables"/> elements whose <see
        /// cref="Variable.VariableClass"/> identifier equals a <see cref="String"/> key in the
        /// specified <paramref name="dictionary"/> to the associated <see cref="Int32"/> value, if
        /// different from the existing value.
        /// </para><para>
        /// <b>ImportChanges</b> does not add new elements for keys that are not found in the
        /// <b>Variables</b> collection. Such <paramref name="dictionary"/> entries are ignored.
        /// </para><para>
        /// All imported <paramref name="dictionary"/> values are restricted to the legal range for
        /// the changed <b>Variables</b> element, as defined by its <see cref="Variable.Minimum"/>
        /// and <see cref="Variable.Maximum"/> values.</para></remarks>

        internal bool ImportChanges(VariableValueDictionary dictionary) {
            if (dictionary == null)
                ThrowHelper.ThrowArgumentNullException("dictionary");

            bool changed = false;
            foreach (var pair in dictionary) {
                int value = pair.Value;

                // search variable class among existing values
                Variable variable;
                if (Variables.TryGetValue(pair.Key, out variable)) {

                    // perform lazy allocation if necessary
                    if (this._variables.IsReadOnly) {

                        // skip variable if restricted value unchanged
                        value = variable.RestrictValue(value);
                        if (value == variable.Value) continue;

                        this._variables = this._variables.Copy();

                        // reacquire element for variable class
                        variable = this._variables[pair.Key];
                    }

                    // set new value for existing variable
                    changed = variable.SetValue(value);
                }
            }

            return changed;
        }

        #endregion
        #region SetValue

        /// <summary>
        /// Sets the <see cref="Variable"/> element with the specified <see cref="VariableClass"/>
        /// to the specified value.</summary>
        /// <param name="variableClass">
        /// The <see cref="VariableClass"/> whose instance value to set.</param>
        /// <param name="value">
        /// The <see cref="Int32"/> value to assign to an instance of the specified <paramref
        /// name="variableClass"/> in the <see cref="Variables"/> collection.</param>
        /// <param name="initial">
        /// <c>true</c> to set the <see cref="Variable.InitialValue"/> of the indicated <see
        /// cref="Variable"/> to the specified <paramref name="value"/>; <c>false</c> to set its
        /// current <see cref="Variable.Value"/>.</param>
        /// <returns>
        /// <c>true</c> if the <see cref="Variables"/> collection was changed; otherwise,
        /// <c>false</c>.</returns>
        /// <exception cref="ArgumentException">
        /// <paramref name="variableClass"/> specifies a <see cref="VariableClass.Category"/> that
        /// is different from the <see cref="Category"/> of this <see cref="VariableContainer"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="variableClass"/> is a null reference.</exception>
        /// <remarks><para>
        /// <b>SetValue</b> sets the <see cref="Variables"/> element whose <see
        /// cref="Variable.VariableClass"/> equals the specified <paramref name="variableClass"/> to
        /// the specified <paramref name="value"/>. <b>SetValue</b> sets either the current <see
        /// cref="Variable.Value"/> or <see cref="Variable.InitialValue"/>, as determined by the
        /// specified <paramref name="initial"/> flag.
        /// </para><para>
        /// <b>SetValue</b> adds a new element with the specified <paramref name="variableClass"/>
        /// and <paramref name="value"/> to the <b>Variables</b> collection if no existing <see
        /// cref="Variable"/> value is found.
        /// </para><para>
        /// The specified <paramref name="value"/> is restricted to the legal range for the new or
        /// changed element, as defined by its <see cref="Variable.Minimum"/> and <see
        /// cref="Variable.Maximum"/> values, but ignoring its current <see
        /// cref="Variable.InitialValue"/> if <paramref name="initial"/> is <c>true</c>.
        /// </para><para>
        /// <b>SetValue</b> may also reset the current <see cref="Variable.Value"/> of the indicated
        /// <see cref="Variable"/> if <paramref name="initial"/> is <c>true</c>, as described in
        /// <see cref="Variable.SetInitialValue"/>.</para></remarks>

        internal bool SetValue(VariableClass variableClass, int value, bool initial) {
            if (variableClass == null)
                ThrowHelper.ThrowArgumentNullException("variableClass");

            if (variableClass.Category != Category)
                ThrowHelper.ThrowArgumentExceptionWithFormat(
                    "variableClass", Tektosyne.Strings.ArgumentSpecifiesInvalid, "Category");

            // search variable class among existing values
            Variable variable;
            if (Variables.TryGetValue(variableClass.Id, out variable)) {

                // perform lazy allocation if necessary
                if (this._variables.IsReadOnly) {

                    // quit if restricted value unchanged
                    value = (initial ? variable.RestrictInitialValue(value) :
                        variable.RestrictValue(value));
                    if (value == variable.Value) return false;

                    this._variables = this._variables.Copy();

                    // reacquire element for variable class
                    variable = this._variables[variableClass.Id];
                }

                // set new value for existing variable
                return (initial ? variable.SetInitialValue(value) : variable.SetValue(value));
            }

            // perform lazy allocation if necessary
            if (this._variables.IsReadOnly)
                this._variables = this._variables.Copy();

            // variable class not found, add new value
            this._variables.Add(new Variable(variableClass, Purpose, value));
            return true;
        }

        #endregion
        #region SetVariables

        /// <summary>
        /// Sets the <see cref="Variables"/> property to the specified value.</summary>
        /// <param name="variables">
        /// The new value for the <see cref="Variables"/> property.</param>
        /// <exception cref="ArgumentException"><para>
        /// <paramref name="variables"/> is not read-only.
        /// </para><para>-or-</para><para>
        /// <paramref name="variables"/> contains elements whose <see cref="Variable.Category"/> is
        /// different from the current <see cref="Category"/>.
        /// </para><para>-or-</para><para>
        /// <paramref name="variables"/> contains elements whose <see cref="Variable.Purpose"/> is
        /// different from the current <see cref="Purpose"/>.</para></exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="variables"/> is a null reference.</exception>
        /// <remarks><para>
        /// <b>SetVariables</b> is called by the <see cref="VariableContainer"/> constructor that
        /// takes a <see cref="VariableList"/> argument. Call this method on an existing <see
        /// cref="VariableContainer"/> to reset its entire <see cref="Variables"/> collection.
        /// </para><para>
        /// <see cref="VariableContainer"/> delays copying of the specified <paramref
        /// name="variables"/> until <see cref="SetValue"/> attempts to change an element. It is the
        /// caller's responsibility to ensure that the specified collection never changes.
        /// </para></remarks>

        internal void SetVariables(VariableList variables) {
            if (variables == null)
                ThrowHelper.ThrowArgumentNullException("variables");

            if (!variables.IsReadOnly)
                ThrowHelper.ThrowArgumentException(
                    "variables", Tektosyne.Strings.ArgumentNotReadOnly);

            // check initial values for consistency
            for (int i = 0; i < variables.Count; i++) {
                Variable variable = variables[i];

                if (variable.Category != Category)
                    ThrowHelper.ThrowArgumentExceptionWithFormat(
                        "variables", Tektosyne.Strings.ArgumentContainsDifferent, "Category");

                if ((variable.Purpose & ~VariablePurpose.Scenario) != Purpose)
                    ThrowHelper.ThrowArgumentExceptionWithFormat(
                        "variables", Tektosyne.Strings.ArgumentContainsDifferent, "Purpose");
            }

            this._variables = variables;
        }

        #endregion
        #endregion
        #region ContainsId

        /// <summary>
        /// Determines whether the <see cref="Variables"/> collection contains the specified
        /// identifier.</summary>
        /// <param name="id">
        /// The <see cref="Variable.Id"/> string to locate in the <see cref="Variables"/>
        /// collection.</param>
        /// <returns>
        /// <c>true</c> if <paramref name="id"/> is found in the <see cref="Variables"/> collection;
        /// otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullOrEmptyException">
        /// <paramref name="id"/> is a null reference or an empty string.</exception>
        /// <remarks>
        /// <b>ContainsId</b> returns the result of <see cref="VariableList.ContainsKey"/> for the
        /// specified <paramref name="id"/> and the <see cref="Variables"/> collection.</remarks>

        public bool ContainsId(string id) {
            if (String.IsNullOrEmpty(id))
                ThrowHelper.ThrowArgumentNullOrEmptyException("id");

            return Variables.ContainsKey(id);
        }

        #endregion
        #region GetValue

        /// <summary>
        /// Returns the <see cref="Variable.Value"/> of the <see cref="Variables"/> element with the
        /// specified identifier, or zero if not found.</summary>
        /// <param name="id">
        /// The <see cref="Variable.Id"/> string to locate in the <see cref="Variables"/>
        /// collection.</param>
        /// <returns>
        /// The <see cref="Variable.Value"/> of the <see cref="Variables"/> element with the
        /// specified <paramref name="id"/>, if found; otherwise, zero.</returns>
        /// <exception cref="ArgumentNullOrEmptyException">
        /// <paramref name="id"/> is a null reference or an empty string.</exception>
        /// <remarks>
        /// <b>GetValue</b> returns the <see cref="Variable.Value"/> of the result of <see
        /// cref="VariableList.TryGetValue"/> for the specified <paramref name="id"/> and the <see
        /// cref="Variables"/> collection if successful, and zero if unsuccessful.</remarks>

        public int GetValue(string id) {
            if (String.IsNullOrEmpty(id))
                ThrowHelper.ThrowArgumentNullOrEmptyException("id");

            Variable variable;
            return (Variables.TryGetValue(id, out variable) ? variable.Value : 0);
        }

        #endregion
        #region IndexOfId

        /// <summary>
        /// Returns the zero-based index of the first occurrence of the specified identifier in the
        /// <see cref="Variables"/> collection.</summary>
        /// <param name="id">
        /// The <see cref="Variable.Id"/> string to locate in the <see cref="Variables"/>
        /// collection.</param>
        /// <returns>
        /// The zero-based index of the first occurrence of <paramref name="id"/> in the <see
        /// cref="Variables"/> collection, if found; otherwise, -1.</returns>
        /// <exception cref="ArgumentNullOrEmptyException">
        /// <paramref name="id"/> is a null reference or an empty string.</exception>
        /// <remarks>
        /// <b>IndexOfId</b> returns the result of <see cref="VariableList.IndexOfKey"/> for the
        /// specified <paramref name="id"/> and the <see cref="Variables"/> collection.</remarks>

        public int IndexOfId(string id) {
            if (String.IsNullOrEmpty(id))
                ThrowHelper.ThrowArgumentNullOrEmptyException("id");

            return Variables.IndexOfKey(id);
        }

        #endregion
        #region ToDictionary

        /// <summary>
        /// Creates a new dictionary from the elements in the <see cref="Variables"/> collection.
        /// </summary>
        /// <returns>
        /// A <see cref="VariableValueDictionary"/> that maps all <see cref="Variable.Id"/> strings
        /// in the <see cref="Variables"/> collections to the corresponding <see
        /// cref="Variable.Value"/> components.</returns>
        /// <remarks>
        /// <b>ToDictionary</b> never returns a null reference, but it returns an empty dictionary
        /// if the <see cref="Variables"/> collection is empty.</remarks>

        public VariableValueDictionary ToDictionary() {
            return Variable.CreateDictionary(Variables);
        }

        #endregion
        #region ICloneable Members

        /// <summary>
        /// Creates a new <see cref="VariableContainer"/> object that is a deep copy of the current
        /// instance.</summary>
        /// <returns>
        /// A new <see cref="VariableContainer"/> object that is a deep copy of the current
        /// instance.</returns>
        /// <remarks><para>
        /// <b>Clone</b> processes the properties of the current instance as follows:
        /// </para><list type="table"><listheader>
        /// <term>Property</term><description>Action</description>
        /// </listheader><item>
        /// <term><see cref="Variables"/></term>
        /// <description>Deep copy assigned to the new instance if <see cref="SetValue"/> has
        /// changed one or more elements; otherwise, value copied to the new instance.</description>
        /// </item><item>
        /// <term><see cref="Category"/><br /> <see cref="Purpose"/></term>
        /// <description>Values copied to the new instance.</description>
        /// </item><item>
        /// <term><see cref="Count"/></term>
        /// <description>Value provided by <see cref="Variables"/>.</description>
        /// </item><item>
        /// <term><see cref="IsModifier"/></term>
        /// <description>Value provided by <see cref="Purpose"/>.</description>
        /// </item></list></remarks>

        public object Clone() {
            return new VariableContainer(this);
        }

        #endregion
    }
}
