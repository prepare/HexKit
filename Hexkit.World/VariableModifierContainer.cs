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
    using VariableModifierDictionary = SortedListEx<String, VariableModifier>;
    using VariableValueDictionary = SortedListEx<String, Int32>;

    #endregion

    /// <summary>
    /// Provides a safe and efficient wrapper around an array of <see cref="Variable"/> collections
    /// that correspond to <see cref="ModifierTarget"/> values.</summary>
    /// <remarks><para>
    /// <b>VariableModifierContainer</b> provides safe access, lazy allocation, and contextual
    /// information for a wrapped array of <see cref="Variable"/> collections.
    /// </para><list type="bullet"><item>
    /// The contained <see cref="Variable"/> collections are exposed only through the read-only
    /// wrappers returned by <see cref="VariableModifierContainer.GetVariables"/>. Write access is
    /// possible only through two internal methods, <see cref="VariableModifierContainer.SetValue"/>
    /// and <see cref="VariableModifierContainer.ImportChanges"/>. This ensures that rule scripts
    /// use the appropriate HCL instructions to change faction or entity variable modifiers.
    /// </item><item>
    /// When a <b>VariableModifierContainer</b> is created with an existing array of <see
    /// cref="Variable"/> collections, the container initially stores a reference to the existing
    /// array. A new array of backing collections is allocated only when <b>SetValue</b> or
    /// <b>UpdateValues</b> attempts to modify a variable value. This lazy allocation is essential
    /// to reduce the number of copy operations when cloning world states.
    /// </item><item>
    /// <b>VariableModifierContainer</b> stores the <see cref="VariableModifierContainer.Category"/>
    /// and <see cref="VariableModifierContainer.Purpose"/> for the contained variables. This
    /// information is available even if the container is empty, and incompatible variables are
    /// rejected.</item></list></remarks>

    public class VariableModifierContainer: ICloneable {
        #region VariableModifierContainer(VariableCategory, VariablePurpose)

        /// <overloads>
        /// Initializes a new instance of the <see cref="VariableModifierContainer"/> class.
        /// </overloads>
        /// <summary>
        /// Initializes a new instance of the <see cref="VariableModifierContainer"/> class with the
        /// specified category and purpose.</summary>
        /// <param name="category">
        /// The initial value for the <see cref="Category"/> property.</param>
        /// <param name="purpose">
        /// The initial value for the <see cref="Purpose"/> property.</param>
        /// <exception cref="InvalidEnumArgumentException">
        /// <paramref name="purpose"/> is not a valid bitwise combination of <see
        /// cref="VariablePurpose"/> values.</exception>
        /// <remarks>
        /// The array containing all <see cref="Variable"/> collections is initialized to the shared
        /// <see cref="EntityClassCache.EmptyModifierArray"/>.</remarks>

        internal VariableModifierContainer(VariableCategory category, VariablePurpose purpose) {

            if (!Variable.CheckPurpose(purpose))
                ThrowHelper.ThrowInvalidEnumArgumentException(
                    "purpose", (int) purpose, typeof(VariablePurpose));

            this._category = category;
            this._purpose = (purpose & ~VariablePurpose.Scenario);
            this._variableArray = EntityClassCache.EmptyModifierArray;
        }

        #endregion
        #region VariableModifierContainer(VariableCategory, VariablePurpose, VariableList[])

        /// <summary>
        /// Initializes a new instance of the <see cref="VariableModifierContainer"/> class with the
        /// specified category, purpose, and initial <see cref="Variable"/> values.</summary>
        /// <param name="category">
        /// The initial value for the <see cref="Category"/> property.</param>
        /// <param name="purpose">
        /// The initial value for the <see cref="Purpose"/> property.</param>
        /// <param name="variableArray">
        /// An <see cref="Array"/> containing the initial values for all <see cref="Variable"/>
        /// collections.</param>
        /// <exception cref="ArgumentException"><para>
        /// <paramref name="variableArray"/> contains collections that are not read-only.
        /// </para><para>-or-</para><para>
        /// <paramref name="variableArray"/> contains collections with elements whose <see
        /// cref="Variable.Category"/> is different from the specified <paramref name="category"/>.
        /// </para><para>-or-</para><para>
        /// <paramref name="variableArray"/> contains collections with elements whose <see
        /// cref="Variable.Purpose"/> is different from the specified <paramref name="purpose"/>.
        /// </para></exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="variableArray"/> is a null reference.</exception>
        /// <exception cref="IndexOutOfRangeException">
        /// <paramref name="variableArray"/> does not contain a <see cref="VariableList"/> at each
        /// index position that corresponds to a <see cref="ModifierTarget"/> value.</exception>
        /// <exception cref="InvalidEnumArgumentException">
        /// <paramref name="purpose"/> is not a valid bitwise combination of <see
        /// cref="VariablePurpose"/> values.</exception>
        /// <remarks>
        /// <see cref="VariableModifierContainer"/> delays copying of the specified <paramref
        /// name="variableArray"/> and its elements until <see cref="SetValue"/> attempts to change
        /// an element. It is the caller's responsibility to ensure that the specified array and
        /// collections never change.</remarks>

        internal VariableModifierContainer(VariableCategory category, VariablePurpose purpose,
            VariableList[] variableArray): this(category, purpose) {

            SetVariables(variableArray);
        }

        #endregion
        #region VariableModifierContainer(VariableModifierContainer)

        /// <summary>
        /// Initializes a new instance of the <see cref="VariableModifierContainer"/> class that is
        /// a deep copy of the specified instance.</summary>
        /// <param name="container">
        /// The <see cref="VariableModifierContainer"/> object whose properties should be copied to
        /// the new instance.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="container"/> is a null reference.</exception>
        /// <remarks>
        /// This constructor is called by <see cref="Clone"/> to perform a deep copy of the
        /// specified <paramref name="container"/>. Please refer to that method for further details.
        /// </remarks>

        private VariableModifierContainer(VariableModifierContainer container) {
            if (container == null)
                ThrowHelper.ThrowArgumentNullException("container");

            this._category = container._category;
            this._purpose = container._purpose;

            // create deep copy only for writable collections
            this._variableArray = (IsReadOnly(container._variableArray) ?
                container._variableArray : CopyModifierArray(container._variableArray));
        }

        #endregion
        #region Private Fields

        // property backers
        private readonly VariableCategory _category;
        private readonly VariablePurpose _purpose;
        private VariableList[] _variableArray;

        #endregion
        #region Public Properties
        #region Category

        /// <summary>
        /// Gets the category of all <see cref="Variable"/> values.</summary>
        /// <value>
        /// The value of the <see cref="Scenario.VariableClass.Category"/> property of the
        /// underlying <see cref="Variable.VariableClass"/> for any managed <see cref="Variable"/>.
        /// </value>
        /// <remarks>
        /// <b>Category</b> never changes once the object has been constructed.</remarks>

        public VariableCategory Category {
            [DebuggerStepThrough]
            get { return this._category; }
        }

        #endregion
        #region IsEmpty

        /// <summary>
        /// Gets a value indicating whether the <see cref="VariableModifierContainer"/> is empty.
        /// </summary>
        /// <value>
        /// <c>true</c> if <see cref="ICollection{T}.Count"/> is zero for every <see
        /// cref="VariableList"/> managed by the <see cref="VariableModifierContainer"/>; otherwise,
        /// <c>false</c>.</value>

        public bool IsEmpty {
            get {
                return (Self.Count == 0 && Owner.Count == 0 && 
                    Units.Count == 0 && UnitsRanged.Count == 0 &&
                    OwnerUnits.Count == 0 && OwnerUnitsRanged.Count == 0);
            }
        }

        #endregion
        #region IsModifier

        /// <summary>
        /// Gets a value indicating whether all <see cref="Variable"/> values are modifiers.
        /// </summary>
        /// <value>
        /// <c>true</c> if all managed <see cref="Variable"/> values represent <see
        /// cref="VariablePurpose.Modifier"/> values; <c>false</c> if they represent <see
        /// cref="VariablePurpose.Basic"/> values.</value>
        /// <remarks>
        /// <b>IsModifier</b> never changes once the object has been constructed.</remarks>

        public bool IsModifier {
            [DebuggerStepThrough]
            get { return ((Purpose & VariablePurpose.Modifier) != 0); }
        }

        #endregion
        #region Item[Int32, ModifierTarget]

        /// <overloads>
        /// Gets a specific <see cref="VariableList"/> element for a specified <see
        /// cref="ModifierTarget"/>.</overloads>
        /// <summary>
        /// Gets the <see cref="VariableList"/> element at the specified index for the specified
        /// <see cref="ModifierTarget"/>.</summary>
        /// <param name="index">
        /// The zero-based index of the <see cref="VariableList"/> element to get for the specified
        /// <paramref name="target"/>.</param>
        /// <param name="target">
        /// A <see cref="ModifierTarget"/> value indicating which <see cref="VariableList"/> to
        /// search.</param>
        /// <value>
        /// The element at the specified <paramref name="index"/> in the <see cref="VariableList"/> 
        /// for the specified <paramref name="target"/>.</value>
        /// <exception cref="ArgumentOutOfRangeException"><para>
        /// <paramref name="index"/> is less than zero.
        /// </para><para>-or-</para><para>
        /// <paramref name="index"/> is equal to or greater than the element count of the <see
        /// cref="VariableList"/> indicated by <paramref name="target"/>.
        /// </para><para>-or-</para><para>
        /// <paramref name="target"/> is not a valid <see cref="ModifierTarget"/> value.
        /// </para></exception>
        /// <remarks>
        /// This indexer has the same effect as the equivalent indexer of the <see
        /// cref="GetVariables"/> result for the specified <paramref name="target"/>.</remarks>

        public Variable this[int index, ModifierTarget target] {
            [DebuggerStepThrough]
            get { return GetVariables(target)[index]; }
        }

        #endregion
        #region Item[String, ModifierTarget]

        /// <summary>
        /// Gets the <see cref="VariableList"/> element associated with the specified identifier for
        /// the specified <see cref="ModifierTarget"/>, or a null reference if not found.</summary>
        /// <param name="id">
        /// The <see cref="Variable.Id"/> string whose <see cref="VariableList"/> element for the
        /// specified <paramref name="target"/> to get.</param>
        /// <param name="target">
        /// A <see cref="ModifierTarget"/> value indicating which <see cref="VariableList"/> to
        /// search.</param>
        /// <value>
        /// The element with the specified <paramref name="id"/> in the <see cref="VariableList"/> 
        /// for the specified <paramref name="target"/>, if found; otherwise, a null reference.
        /// </value>
        /// <exception cref="ArgumentNullOrEmptyException">
        /// <paramref name="id"/> is a null reference or an empty string.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="target"/> is not a valid <see cref="ModifierTarget"/> value.</exception>
        /// <remarks>
        /// This indexer has the same effect as the equivalent indexer of the <see
        /// cref="GetVariables"/> result for the specified <paramref name="target"/>, but returns a
        /// null reference instead of throwing a <see cref="KeyNotFoundException"/> if the specified
        /// <paramref name="id"/> is not found.</remarks>

        public Variable this[string id, ModifierTarget target] {
            get {
                if (String.IsNullOrEmpty(id))
                    ThrowHelper.ThrowArgumentNullOrEmptyException("id");

                Variable variable;
                GetVariables(target).TryGetValue(id, out variable);
                return variable;
            }
        }

        #endregion
        #region MaxCount

        /// <summary>
        /// Gets the maximum number of elements contained in any <see cref="VariableList"/>.
        /// </summary>
        /// <value>
        /// The maximum <see cref="ICollection{T}.Count"/> for any <see cref="VariableList"/>
        /// managed by the <see cref="VariableModifierContainer"/>.</value>

        public int MaxCount {
            get {
                return Fortran.Max(Self.Count, Owner.Count, 
                    Units.Count, UnitsRanged.Count,
                    OwnerUnits.Count, OwnerUnitsRanged.Count);
            }
        }

        #endregion
        #region Owner

        /// <summary>
        /// Gets the <see cref="Variable"/> collection for <see cref="ModifierTarget.Owner"/>
        /// modifiers managed by the <see cref="VariableModifierContainer"/>.</summary>
        /// <returns>
        /// The read-only <see cref="VariableList"/> that is returned by <see cref="GetVariables"/>
        /// for a <see cref="ModifierTarget"/> value of <see cref="ModifierTarget.Owner"/>.
        /// </returns>
        /// <remarks>
        /// <b>Owner</b> never returns a null reference. All keys are unique. Use the <see
        /// cref="SetValue"/> method to add or change a <see cref="Variable"/> value.</remarks>

        public VariableList Owner {
            [DebuggerStepThrough]
            get { return GetVariables(ModifierTarget.Owner); }
        }

        #endregion
        #region OwnerUnits

        /// <summary>
        /// Gets the <see cref="Variable"/> collection for <see cref="ModifierTarget.OwnerUnits"/>
        /// modifiers managed by the <see cref="VariableModifierContainer"/>.</summary>
        /// <returns>
        /// The read-only <see cref="VariableList"/> that is returned by <see cref="GetVariables"/>
        /// for a <see cref="ModifierTarget"/> value of <see cref="ModifierTarget.OwnerUnits"/>.
        /// </returns>
        /// <remarks>
        /// <b>OwnerUnits</b> never returns a null reference. All keys are unique. Use the <see
        /// cref="SetValue"/> method to add or change a <see cref="Variable"/> value.</remarks>

        public VariableList OwnerUnits {
            [DebuggerStepThrough]
            get { return GetVariables(ModifierTarget.OwnerUnits); }
        }

        #endregion
        #region OwnerUnitsRanged

        /// <summary>
        /// Gets the <see cref="Variable"/> collection for <see
        /// cref="ModifierTarget.OwnerUnitsRanged"/> modifiers managed by the <see
        /// cref="VariableModifierContainer"/>.</summary>
        /// <returns>
        /// The read-only <see cref="VariableList"/> that is returned by <see cref="GetVariables"/>
        /// for a <see cref="ModifierTarget"/> value of <see
        /// cref="ModifierTarget.OwnerUnitsRanged"/>.</returns>
        /// <remarks>
        /// <b>OwnerUnitsRanged</b> never returns a null reference. All keys are unique. Use the
        /// <see cref="SetValue"/> method to add or change a <see cref="Variable"/> value.</remarks>

        public VariableList OwnerUnitsRanged {
            [DebuggerStepThrough]
            get { return GetVariables(ModifierTarget.OwnerUnitsRanged); }
        }

        #endregion
        #region Purpose

        /// <summary>
        /// Gets the purpose of all <see cref="Variable"/> values.</summary>
        /// <value>
        /// A <see cref="VariablePurpose"/> value indicating the purpose of all managed <see
        /// cref="Variable"/> values.</value>
        /// <remarks><para>
        /// <b>Purpose</b> never changes once the object has been constructed. This property always
        /// contains one of the valid bitwise combinations defined for the <see
        /// cref="VariablePurpose"/> enumeration.
        /// </para><para>
        /// <b>Purpose</b> never contains the <see cref="VariablePurpose.Scenario"/> flag, even if
        /// it was supplied to the constructor. This flag is communicated to all initial elements of
        /// any <see cref="VariableList"/>, however.</para></remarks>

        public VariablePurpose Purpose {
            [DebuggerStepThrough]
            get { return this._purpose; }
        }

        #endregion
        #region Self

        /// <summary>
        /// Gets the <see cref="Variable"/> collection for <see cref="ModifierTarget.Self"/>
        /// modifiers managed by the <see cref="VariableModifierContainer"/>.</summary>
        /// <returns>
        /// The read-only <see cref="VariableList"/> that is returned by <see cref="GetVariables"/>
        /// for a <see cref="ModifierTarget"/> value of <see cref="ModifierTarget.Self"/>.</returns>
        /// <remarks>
        /// <b>Self</b> never returns a null reference. All keys are unique. Use the <see
        /// cref="SetValue"/> method to add or change a <see cref="Variable"/> value.</remarks>

        public VariableList Self {
            [DebuggerStepThrough]
            get { return GetVariables(ModifierTarget.Self); }
        }

        #endregion
        #region Units

        /// <summary>
        /// Gets the <see cref="Variable"/> collection for <see cref="ModifierTarget.Units"/>
        /// modifiers managed by the <see cref="VariableModifierContainer"/>.</summary>
        /// <returns>
        /// The read-only <see cref="VariableList"/> that is returned by <see cref="GetVariables"/>
        /// for a <see cref="ModifierTarget"/> value of <see cref="ModifierTarget.Units"/>.
        /// </returns>
        /// <remarks>
        /// <b>Units</b> never returns a null reference. All keys are unique. Use the <see
        /// cref="SetValue"/> method to add or change a <see cref="Variable"/> value.</remarks>

        public VariableList Units {
            [DebuggerStepThrough]
            get { return GetVariables(ModifierTarget.Units); }
        }

        #endregion
        #region UnitsRanged

        /// <summary>
        /// Gets the <see cref="Variable"/> collection for <see cref="ModifierTarget.UnitsRanged"/>
        /// modifiers managed by the <see cref="VariableModifierContainer"/>.</summary>
        /// <returns>
        /// The read-only <see cref="VariableList"/> that is returned by <see cref="GetVariables"/>
        /// for a <see cref="ModifierTarget"/> value of <see cref="ModifierTarget.UnitsRanged"/>.
        /// </returns>
        /// <remarks>
        /// <b>UnitsRanged</b> never returns a null reference. All keys are unique. Use the <see
        /// cref="SetValue"/> method to add or change a <see cref="Variable"/> value.</remarks>

        public VariableList UnitsRanged {
            [DebuggerStepThrough]
            get { return GetVariables(ModifierTarget.UnitsRanged); }
        }

        #endregion
        #endregion
        #region Private Methods
        #region CopyModifierArray

        /// <summary>
        /// Creates a deep copy of the specified array of <see cref="Variable"/> collections indexed
        /// by <see cref="ModifierTarget"/> values.</summary>
        /// <param name="variableArray">
        /// An <see cref="Array"/> containing one <see cref="VariableList"/> for each <see
        /// cref="ModifierTarget"/>.</param>
        /// <returns>
        /// A deep copy of the specified <paramref name="variableArray"/>.</returns>
        /// <exception cref="IndexOutOfRangeException">
        /// <paramref name="variableArray"/> does not contain a <see cref="VariableList"/> at each
        /// index position that corresponds to a <see cref="ModifierTarget"/> value.</exception>
        /// <remarks>
        /// <b>CopyModifierArray</b> returns a new <see cref="Array"/> containing deep copies of
        /// each <see cref="VariableList"/> in the specified <paramref name="variableArray"/>. Each
        /// copied collection is writable, even if the original collection was read-only.</remarks>

        private static VariableList[] CopyModifierArray(VariableList[] variableArray) {

            var newArray = new VariableList[VariableModifier.AllModifierTargets.Length];
            foreach (var target in VariableModifier.AllModifierTargets)
                newArray[(int) target] = variableArray[(int) target].Copy();

            return newArray;
        }

        #endregion
        #region IsReadOnly

        /// <summary>
        /// Gets a value indicating whether the specified array of <see cref="Variable"/>
        /// collections indexed by <see cref="ModifierTarget"/> values is read-only.</summary>
        /// <param name="variableArray">
        /// An <see cref="Array"/> containing one <see cref="VariableList"/> for each <see
        /// cref="ModifierTarget"/>.</param>
        /// <returns>
        /// <c>true</c> if all <see cref="VariableList"/> collections in the specified <paramref
        /// name="variableArray"/> are read-only; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentException">
        /// <paramref name="variableArray"/> contains at least one <see cref="VariableList"/> that
        /// is read-only, and at least one <see cref="VariableList"/> that is writable.</exception>
        /// <remarks>
        /// Any <see cref="VariableList"/> array that is managed by a <see
        /// cref="VariableModifierContainer"/> must contain either only read-only collections or
        /// only writable collections, but not both. For better performance, <b>IsReadOnly</b>
        /// verifies this condition in debug mode only.</remarks>

        private static bool IsReadOnly(VariableList[] variableArray) {
            bool isReadOnly = variableArray[0].IsReadOnly;
#if DEBUG
            foreach (var target in VariableModifier.AllModifierTargets)
                if (variableArray[(int) target].IsReadOnly != isReadOnly)
                    ThrowHelper.ThrowArgumentExceptionWithFormat("variableArray",
                        Tektosyne.Strings.ArgumentContainsDifferent, "IsReadOnly");
#endif
            return isReadOnly;
        }

        #endregion
        #endregion
        #region Internal Methods
        #region ExportChanges

        /// <summary>
        /// Exports any changes in any <see cref="Variable"/> collection, relative to the specified
        /// source dictionary, to the specified target dictionary.</summary>
        /// <param name="sourceDictionary">
        /// The <see cref="VariableModifierDictionary"/> to examine for changes.</param>
        /// <param name="targetDictionary">
        /// The <see cref="VariableModifierDictionary"/> that receives the changes.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="sourceDictionary"/> or <paramref name="targetDictionary"/> is a null
        /// reference.</exception>
        /// <remarks><para>
        /// <b>ExportChanges</b> compares all key-and-value pairs in the specified <paramref
        /// name="sourceDictionary"/> to the current contents of all <see cref="Variable"/>
        /// collections. Any key that exists with a different <see cref="ModifierTarget"/> value in
        /// the corresponding <see cref="VariableList"/> is added to the specified <paramref
        /// name="targetDictionary"/> with its current <see cref="VariableList"/> value.
        /// </para><para>
        /// Whenever a new <see cref="VariableModifier"/> is added to the <paramref
        /// name="targetDictionary"/> because of a change in one <see cref="ModifierTarget"/> value,
        /// all unchanged values of the same <see cref="VariableModifier"/> are copied from the
        /// <paramref name="sourceDictionary"/>.</para></remarks>

        internal void ExportChanges(VariableModifierDictionary sourceDictionary,
            VariableModifierDictionary targetDictionary) {

            if (sourceDictionary == null)
                ThrowHelper.ThrowArgumentNullException("sourceDictionary");
            if (targetDictionary == null)
                ThrowHelper.ThrowArgumentNullException("targetDictionary");

            Variable variable;
            VariableModifier modifier;

            foreach (var pair in sourceDictionary)
                foreach (var target in VariableModifier.AllModifierTargets) {
                    var variables = this._variableArray[(int) target];
                    if (variables.TryGetValue(pair.Key, out variable)) {

                        int value = pair.Value.GetByTarget(target).GetValueOrDefault();
                        if (variable.Value != value) {

                            // check if target collection defines identifier
                            if (!targetDictionary.TryGetValue(pair.Key, out modifier)) {
                                modifier = (VariableModifier) pair.Value.Clone();
                                targetDictionary.Add(pair.Key, modifier);
                            }

                            modifier.SetByTarget(target, value);
                        }
                    }
                }
        }

        #endregion
        #region ImportChanges

        /// <summary>
        /// Imports any changed values from the specified dictionary into the corresponding <see
        /// cref="Variable"/> collections.</summary>
        /// <param name="dictionary">
        /// A <see cref="VariableModifierDictionary"/> that maps <see cref="VariableClass.Id"/>
        /// strings of <see cref="VariableClass"/> objects to <see cref="Int32"/> values.</param>
        /// <returns>
        /// <c>true</c> if any <see cref="VariableList"/> was changed; otherwise, <c>false</c>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="dictionary"/> is a null reference.</exception>
        /// <remarks><para>
        /// <b>ImportChanges</b> sets all <see cref="VariableList"/> elements whose <see
        /// cref="Variable.VariableClass"/> identifier equals a <see cref="String"/> key in the
        /// specified <paramref name="dictionary"/> to the <see cref="Int32"/> value associated with
        /// the corresponding <see cref="ModifierTarget"/>, if different from the existing value.
        /// </para><para>
        /// <b>ImportChanges</b> does not add new elements for keys that are not found in the
        /// <b>Variables</b> collection. Such <paramref name="dictionary"/> entries are ignored.
        /// </para><para>
        /// All imported <paramref name="dictionary"/> values are restricted to the legal range for
        /// the changed <see cref="Variable"/>, as defined by its <see cref="Variable.Minimum"/> and
        /// <see cref="Variable.Maximum"/> values.</para></remarks>

        internal bool ImportChanges(VariableModifierDictionary dictionary) {
            if (dictionary == null)
                ThrowHelper.ThrowArgumentNullException("dictionary");

            bool changed = false;
            foreach (var pair in dictionary)
                foreach (var target in VariableModifier.AllModifierTargets) {
                    int value = pair.Value.GetByTarget(target).GetValueOrDefault();

                    // search variable class among existing values
                    var variables = this._variableArray[(int) target];
                    Variable variable;
                    if (variables.TryGetValue(pair.Key, out variable)) {

                        // perform lazy allocation if necessary
                        if (IsReadOnly(this._variableArray)) {

                            // skip variable if restricted value unchanged
                            value = variable.RestrictValue(value);
                            if (value == variable.Value) continue;

                            // make all collections writable
                            this._variableArray = CopyModifierArray(this._variableArray);

                            // reacquire collection & element
                            variables = this._variableArray[(int) target];
                            variable = variables[pair.Key];
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
        /// Sets the <see cref="VariableList"/> element with the specified <see
        /// cref="VariableClass"/> to the specified value.</summary>
        /// <param name="variableClass">
        /// The <see cref="VariableClass"/> whose instance value to set.</param>
        /// <param name="target">
        /// A <see cref="ModifierTarget"/> value indicating the <see cref="VariableList"/> to
        /// modify.</param>
        /// <param name="value">
        /// The <see cref="Int32"/> value to assign to an instance of the specified <paramref
        /// name="variableClass"/> in the <see cref="VariableList"/> indicated by <paramref
        /// name="target"/>.</param>
        /// <returns>
        /// <c>true</c> if a <see cref="VariableList"/> was changed; otherwise, <c>false</c>.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// <paramref name="variableClass"/> specifies a <see cref="VariableClass.Category"/> that
        /// is different from the <see cref="Category"/> of this <see
        /// cref="VariableModifierContainer"/>.</exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="variableClass"/> is a null reference.</exception>
        /// <exception cref="IndexOutOfRangeException">
        /// <paramref name="target"/> is not a valid <see cref="ModifierTarget"/> value.</exception>
        /// <remarks><para>
        /// <b>SetValue</b> sets the <see cref="VariableList"/> element that corresponds to the
        /// specified <paramref name="target"/> and whose <see cref="Variable.VariableClass"/>
        /// equals the specified <paramref name="variableClass"/> to the specified <paramref
        /// name="value"/>.
        /// </para><para>
        /// <b>SetValue</b> adds a new element with the specified <paramref name="variableClass"/>
        /// and <paramref name="value"/> to the <see cref="VariableList"/> for the specified
        /// <paramref name="target"/> if no existing <see cref="Variable"/> value is found.
        /// </para><para>
        /// The specified <paramref name="value"/> is restricted to the legal range for the new or
        /// changed element, as defined by its <see cref="Variable.Minimum"/> and <see
        /// cref="Variable.Maximum"/> values.</para></remarks>

        internal bool SetValue(VariableClass variableClass, ModifierTarget target, int value) {
            if (variableClass == null)
                ThrowHelper.ThrowArgumentNullException("variableClass");

            if (variableClass.Category != Category)
                ThrowHelper.ThrowArgumentExceptionWithFormat(
                    "variableClass", Tektosyne.Strings.ArgumentSpecifiesInvalid, "Category");

            // search variable class among existing values
            var variables = this._variableArray[(int) target];
            Variable variable;
            if (variables.TryGetValue(variableClass.Id, out variable)) {

                // perform lazy allocation if necessary
                if (IsReadOnly(this._variableArray)) {

                    // quit if restricted value unchanged
                    value = variable.RestrictValue(value);
                    if (value == variable.Value) return false;

                    // make all collections writable
                    this._variableArray = CopyModifierArray(this._variableArray);

                    // reacquire collection & element
                    variables = this._variableArray[(int) target];
                    variable = variables[variableClass.Id];
                }

                // set new value for existing variable
                return variable.SetValue(value);
            }

            // perform lazy allocation if necessary
            if (IsReadOnly(this._variableArray)) {
                this._variableArray = CopyModifierArray(this._variableArray);
                variables = this._variableArray[(int) target];
            }

            // variable class not found, add new value
            variables.Add(new Variable(variableClass, Purpose, value));
            return true;
        }

        #endregion
        #region SetVariables

        /// <summary>
        /// Sets the all <see cref="Variable"/> collections to the specified values.</summary>
        /// <param name="variableArray">
        /// An <see cref="Array"/> containing the new <see cref="Variable"/> collections.</param>
        /// <exception cref="ArgumentException"><para>
        /// <paramref name="variableArray"/> contains collections that are not read-only.
        /// </para><para>-or-</para><para>
        /// <paramref name="variableArray"/> contains collections with elements whose <see
        /// cref="Variable.Category"/> is different from the current <see cref="Category"/>.
        /// </para><para>-or-</para><para>
        /// <paramref name="variableArray"/> contains collections with elements whose <see
        /// cref="Variable.Purpose"/> is different from the current <see cref="Purpose"/>.
        /// </para></exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="variableArray"/> is a null reference.</exception>
        /// <exception cref="IndexOutOfRangeException">
        /// <paramref name="variableArray"/> does not contain a <see cref="VariableList"/> at each
        /// index position that corresponds to a <see cref="ModifierTarget"/> value.</exception>
        /// <remarks><para>
        /// <b>SetVariables</b> is called by the <see cref="VariableModifierContainer"/> constructor
        /// that takes a <see cref="VariableList"/> array. Call this method on an existing <see
        /// cref="VariableModifierContainer"/> to reset all its <see cref="Variable"/> collections.
        /// </para><para>
        /// <see cref="VariableModifierContainer"/> delays copying of the specified <paramref
        /// name="variableArray"/> and its collections until <see cref="SetValue"/> attempts to
        /// change an element. It is the caller's responsibility to ensure that the specified array
        /// and collections never change.</para></remarks>

        internal void SetVariables(VariableList[] variableArray) {
            if (variableArray == null)
                ThrowHelper.ThrowArgumentNullException("variableArray");

            if (!IsReadOnly(variableArray))
                ThrowHelper.ThrowArgumentException(
                    "variableArray", Tektosyne.Strings.ArgumentNotReadOnly);
            
            foreach (var target in VariableModifier.AllModifierTargets) {
                var variables = variableArray[(int) target];

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
            }

            this._variableArray = variableArray;
        }

        #endregion
        #endregion
        #region ContainsId

        /// <summary>
        /// Determines whether the <see cref="VariableList"/> for the specified <see
        /// cref="ModifierTarget"/> contains the specified identifier.</summary>
        /// <param name="id">
        /// The <see cref="Variable.Id"/> string to locate in the <see cref="VariableList"/> for the
        /// specified <paramref name="target"/>.</param>
        /// <param name="target">
        /// A <see cref="ModifierTarget"/> value indicating which <see cref="VariableList"/> to
        /// search.</param>
        /// <returns>
        /// <c>true</c> if <paramref name="id"/> is found in the <see cref="VariableList"/> for the
        /// specified <paramref name="target"/>; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullOrEmptyException">
        /// <paramref name="id"/> is a null reference or an empty string.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="target"/> is not a valid <see cref="ModifierTarget"/> value.</exception>
        /// <remarks>
        /// <b>ContainsId</b> returns the result of <see cref="VariableList.ContainsKey"/> for the
        /// specified <paramref name="id"/> and the <see cref="GetVariables"/> result for the
        /// specified <paramref name="target"/>.</remarks>

        public bool ContainsId(string id, ModifierTarget target) {
            if (String.IsNullOrEmpty(id))
                ThrowHelper.ThrowArgumentNullOrEmptyException("id");

            return GetVariables(target).ContainsKey(id);
        }

        #endregion
        #region GetValue

        /// <summary>
        /// Returns the <see cref="Variable.Value"/> of the <see cref="Variable"/> with the
        /// specified identifier and for the specified <see cref="ModifierTarget"/>, or zero if not
        /// found.</summary>
        /// <param name="id">
        /// The <see cref="Variable.Id"/> string to locate in the <see cref="VariableList"/> for the
        /// specified <paramref name="target"/>.</param>
        /// <param name="target">
        /// A <see cref="ModifierTarget"/> value indicating which <see cref="VariableList"/> to
        /// search.</param>
        /// <returns>
        /// The <see cref="Variable.Value"/> of the element with the specified <paramref name="id"/>
        /// in the <see cref="VariableList"/> for the specified <paramref name="target"/>, if found;
        /// otherwise, zero.</returns>
        /// <exception cref="ArgumentNullOrEmptyException">
        /// <paramref name="id"/> is a null reference or an empty string.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="target"/> is not a valid <see cref="ModifierTarget"/> value.</exception>
        /// <remarks>
        /// <b>GetValue</b> returns the <see cref="Variable.Value"/> of the result of <see
        /// cref="VariableList.TryGetValue"/> for the specified <paramref name="id"/> and the <see
        /// cref="GetVariables"/> result for the specified <paramref name="target"/> if successful,
        /// and zero if unsuccessful.</remarks>

        public int GetValue(string id, ModifierTarget target) {
            if (String.IsNullOrEmpty(id))
                ThrowHelper.ThrowArgumentNullOrEmptyException("id");

            Variable variable;
            return (GetVariables(target).TryGetValue(id, out variable) ? variable.Value : 0);
        }

        #endregion
        #region GetVariables

        /// <summary>
        /// Gets the <see cref="Variable"/> collection managed by the <see
        /// cref="VariableModifierContainer"/> for the specified <see cref="ModifierTarget"/>.
        /// </summary>
        /// <param name="target">
        /// A <see cref="ModifierTarget"/> value indicating which <see cref="VariableList"/> to
        /// return.</param>
        /// <returns>
        /// A read-only <see cref="VariableList"/> containing the <see cref="Variable"/> objects
        /// managed by the <see cref="VariableModifierContainer"/> for the specified <paramref
        /// name="target"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="target"/> is not a valid <see cref="ModifierTarget"/> value.</exception>
        /// <remarks>
        /// <b>GetVariables</b> never returns a null reference. All keys in the returned <see
        /// cref="VariableList"/> are unique. Use the <see cref="SetValue"/> method to add or change
        /// a <see cref="Variable"/> value.</remarks>

        public VariableList GetVariables(ModifierTarget target) {
            return this._variableArray[(int) target].AsReadOnly();
        }

        #endregion
        #region IndexOfId

        /// <summary>
        /// Returns the zero-based index of the first occurrence of the specified identifier in the
        /// <see cref="VariableList"/> for the specified <see cref="ModifierTarget"/>.</summary>
        /// <param name="id">
        /// The <see cref="Variable.Id"/> string to locate in the <see cref="VariableList"/> for the
        /// specified <paramref name="target"/>.</param>
        /// <param name="target">
        /// A <see cref="ModifierTarget"/> value indicating which <see cref="VariableList"/> to
        /// search.</param>
        /// <returns>
        /// The zero-based index of the first occurrence of <paramref name="id"/> in the <see
        /// cref="VariableList"/> for the specified <paramref name="target"/>, if found; otherwise,
        /// -1.</returns>
        /// <exception cref="ArgumentNullOrEmptyException">
        /// <paramref name="id"/> is a null reference or an empty string.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="target"/> is not a valid <see cref="ModifierTarget"/> value.</exception>
        /// <remarks>
        /// <b>IndexOfId</b> returns the result of <see cref="VariableList.IndexOfKey"/> for the
        /// specified <paramref name="id"/> and the <see cref="GetVariables"/> result for the
        /// specified <paramref name="target"/>.</remarks>

        public int IndexOfId(string id, ModifierTarget target) {
            if (String.IsNullOrEmpty(id))
                ThrowHelper.ThrowArgumentNullOrEmptyException("id");

            return GetVariables(target).IndexOfKey(id);
        }

        #endregion
        #region ToDictionary()

        /// <overloads>
        /// Creates a new dictionary from the elements in one or more <see cref="Variable"/>
        /// collections.</overloads>
        /// <summary>
        /// Creates a new <see cref="VariableModifierDictionary"/> from the elements in all <see
        /// cref="VariableList"/> collections.</summary>
        /// <returns>
        /// A <see cref="VariableModifierDictionary"/> that maps all <see cref="Variable.Id"/>
        /// strings in the <see cref="VariableList"/> for each <see cref="ModifierTarget"/> to the
        /// corresponding <see cref="VariableModifier"/> objects.</returns>
        /// <remarks>
        /// <b>ToDictionary</b> never returns a null reference, but it returns an empty dictionary
        /// if all <see cref="VariableList"/> collections are empty.</remarks>

        public VariableModifierDictionary ToDictionary() {
            return Variable.CreateDictionary(this._variableArray);
        }

        #endregion
        #region ToDictionary(ModifierTarget)

        /// <summary>
        /// Creates a new <see cref="VariableValueDictionary"/> from the elements in the specified
        /// <see cref="Variable"/> collection.</summary>
        /// <param name="target">
        /// A <see cref="ModifierTarget"/> value indicating which <see cref="VariableList"/> to
        /// convert.</param>
        /// <returns>
        /// A <see cref="VariableValueDictionary"/> that maps all <see cref="Variable.Id"/> strings
        /// in the <see cref="VariableList"/> for the specified <paramref name="target"/> to the
        /// corresponding <see cref="Variable"/> value.</returns>
        /// <remarks>
        /// <b>ToDictionary</b> never returns a null reference, but it returns an empty dictionary
        /// if the <see cref="VariableList"/> for the specified <paramref name="target"/> is empty.
        /// </remarks>

        public VariableValueDictionary ToDictionary(ModifierTarget target) {
            return Variable.CreateDictionary(GetVariables(target));
        }

        #endregion
        #region ICloneable Members

        /// <summary>
        /// Creates a new <see cref="VariableModifierContainer"/> object that is a deep copy of the
        /// current instance.</summary>
        /// <returns>
        /// A new <see cref="VariableModifierContainer"/> object that is a deep copy of the current
        /// instance.</returns>
        /// <remarks><para>
        /// <b>Clone</b> processes the properties of the current instance as follows:
        /// </para><list type="table"><listheader>
        /// <term>Property</term><description>Action</description>
        /// </listheader><item>
        /// <term>Any <see cref="VariableList"/></term>
        /// <description>Deep copy assigned to the new instance if <see cref="SetValue"/> has
        /// changed one or more elements; otherwise, value copied to the new instance.</description>
        /// </item><item>
        /// <term><see cref="Category"/><br /> <see cref="Purpose"/></term>
        /// <description>Values copied to the new instance.</description>
        /// </item><item>
        /// <term><see cref="IsModifier"/></term>
        /// <description>Value provided by <see cref="Purpose"/>.</description>
        /// </item></list></remarks>

        public object Clone() {
            return new VariableModifierContainer(this);
        }

        #endregion
    }
}
