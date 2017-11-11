using System;
using System.ComponentModel;
using System.Diagnostics;

using Tektosyne;
using Tektosyne.Collections;
using Tektosyne.Geometry;

using Hexkit.Global;
using Hexkit.Scenario;

namespace Hexkit.World {
    #region Type Aliases

    using CategorizedValueDictionary = KeyValueList<String, CategorizedValue>;
    using VariableModifierDictionary = SortedListEx<String, VariableModifier>;
    using VariableValueDictionary = SortedListEx<String, Int32>;
    using VariableList = KeyedList<String, Variable>;

    #endregion

    /// <summary>
    /// Represents a numerical variable associated with an <see cref="Entity"/> or <see
    /// cref="Faction"/>.</summary>
    /// <remarks><para>
    /// <b>Variable</b> represents the value of a specific type of attribute or resource associated
    /// with an entity or faction. <b>Variable</b> objects are "instances" of one of the <see
    /// cref="VariableClass"/> objects defined by the current <see cref="VariableSection"/>.
    /// </para><para>
    /// Attributes represents numerical properties that quantify the <em>performance</em> of an
    /// entity. Attributes are recalculated from scratch whenever the entity's situation changes.
    /// Their values never accumulate across turns.
    /// </para><para>
    /// Resources represent numerical properties that quantify the <em>possessions</em> of a faction
    /// or entity. Resources are recalculated either once per turn, or to reflect special
    /// transactions. Their values usually accumulate across turns.</para></remarks>

    public class Variable: ICloneable, IKeyedValue<String> {
        #region Variable(VariableClass, VariablePurpose, Int32, Boolean)

        /// <overloads>
        /// Initializes a new instance of the <see cref="Variable"/> class.</overloads>
        /// <summary>
        /// Initializes a new instance of the <see cref="Variable"/> class based on the specified
        /// variable class, and with the specified purpose and initial value.</summary>
        /// <param name="variableClass">
        /// The initial value for the <see cref="VariableClass"/> property.</param>
        /// <param name="purpose">
        /// The initial value for the <see cref="Purpose"/> property.</param>
        /// <param name="value">
        /// The initial value for the <see cref="Value"/> and <see cref="InitialValue"/> properties.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="variableClass"/> is a null reference.</exception>
        /// <exception cref="InvalidEnumArgumentException">
        /// <paramref name="purpose"/> is not a valid bitwise combination of <see
        /// cref="VariablePurpose"/> values.</exception>
        /// <remarks>
        /// The specified <paramref name="value"/> is restricted to the legal range for the new <see
        /// cref="Variable"/> instance, as defined by its <see cref="Minimum"/> and <see
        /// cref="Maximum"/> values while ignoring <see cref="IsLimitedResource"/>.</remarks>

        public Variable(VariableClass variableClass, VariablePurpose purpose, int value) {
            if (variableClass == null)
                ThrowHelper.ThrowArgumentNullException("variableClass");

            if (!CheckPurpose(purpose))
                ThrowHelper.ThrowInvalidEnumArgumentException(
                    "purpose", (int) purpose, typeof(VariablePurpose));

            this._variableClass = variableClass;
            this._purpose = purpose;

            // legal range for basic values
            int minimum = variableClass.Minimum;
            int maximum = variableClass.Maximum;

            // legal range for modifier values
            if ((purpose & VariablePurpose.Modifier) != 0) {
                minimum = VariableClass.AbsoluteMinimum;
                maximum = VariableClass.AbsoluteMaximum;
            }

            // restrict value to legal range
            if (value < minimum) value = minimum;
            if (value > maximum) value = maximum;

            InitialValue = value;
            Value = value;
        }

        #endregion
        #region Variable(String, VariableCategory, VariablePurpose, Int32)

        /// <summary>
        /// Initializes a new instance of the <see cref="Variable"/> class based on the <see
        /// cref="Scenario.VariableClass"/> with the specified identifier and category, and with the
        /// specified purpose and initial value.</summary>
        /// <param name="classId">
        /// The <see cref="Scenario.VariableClass.Id"/> string of the <see
        /// cref="Scenario.VariableClass"/> object that is the initial value for the <see
        /// cref="VariableClass"/> property.</param>
        /// <param name="category">
        /// The initial value for the <see cref="Category"/> property.</param>
        /// <param name="purpose">
        /// The initial value for the <see cref="Purpose"/> property.</param>
        /// <param name="value">
        /// The initial value for the <see cref="Value"/> and <see cref="InitialValue"/> properties.
        /// </param>
        /// <exception cref="ArgumentException">
        /// <see cref="ApplicationInfo.IsEditor"/> is <c>false</c>, and <paramref name="classId"/>
        /// does not match any <see cref="VariableClass"/> in the specified <paramref
        /// name="category"/> defined by the current <see cref="VariableSection"/>.</exception>
        /// <exception cref="ArgumentNullOrEmptyException">
        /// <paramref name="classId"/> is a null reference or an empty string.</exception>
        /// <exception cref="InvalidEnumArgumentException"><para>
        /// <paramref name="category"/> is not a valid <see cref="VariableCategory"/> value.
        /// </para><para>-or-</para><para>
        /// <paramref name="purpose"/> is not a valid bitwise combination of <see
        /// cref="VariablePurpose"/> values.</para></exception>
        /// <remarks><para>
        /// The specified <paramref name="value"/> is restricted to the legal range for the new <see
        /// cref="Variable"/> instance, as defined by its <see cref="Minimum"/> and <see
        /// cref="Maximum"/> values while ignoring <see cref="IsLimitedResource"/>.
        /// </para><para>
        /// If <see cref="ApplicationInfo.IsEditor"/> is <c>true</c> and the specified <paramref
        /// name="classId"/> was not found in the specified <paramref name="category"/>, a new <see
        /// cref="Scenario.VariableClass"/> object is created with the specified parameters.
        /// </para></remarks>

        public Variable(string classId, VariableCategory category, VariablePurpose purpose, int value):
            this(GetVariableClass(classId, category), purpose, value) { }

        #endregion
        #region Variable(Variable)

        /// <summary>
        /// Initializes a new instance of the <see cref="Variable"/> class that is a deep copy of
        /// the specified instance.</summary>
        /// <param name="variable">
        /// The <see cref="Variable"/> object whose properties should be copied to the new instance.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="variable"/> is a null reference.</exception>
        /// <remarks>
        /// This constructor is called by <see cref="Clone"/> to perform a deep copy of the
        /// specified <paramref name="variable"/>. Please refer to that method for further details.
        /// </remarks>

        private Variable(Variable variable) {
            if (variable == null)
                ThrowHelper.ThrowArgumentNullException("variable");

            this._variableClass = variable._variableClass;
            this._purpose = variable._purpose;

            InitialValue = variable.InitialValue;
            Value = variable.Value;
        }

        #endregion
        #region Private Fields

        // property backers
        private readonly VariableClass _variableClass;
        private readonly VariablePurpose _purpose;

        #endregion
        #region AllPurposes

        /// <summary>
        /// An <see cref="Array"/> comprising all values in <see cref="VariablePurpose"/>.
        /// </summary>
        /// <remarks>
        /// <b>AllPurposes</b> facilitates iterating through all values of the <see
        /// cref="VariablePurpose"/> enumeration. This field holds the result of
        /// <c>Enum.GetValues(typeof(VariablePurpose))</c>.</remarks>

        public static readonly VariablePurpose[] AllPurposes =
            (VariablePurpose[]) Enum.GetValues(typeof(VariablePurpose));

        #endregion
        #region Public Properties
        #region Category

        /// <summary>
        /// Gets the category of the <see cref="Variable"/>.</summary>
        /// <value>
        /// The value of the <see cref="Scenario.VariableClass.Category"/> property of the
        /// underlying <see cref="VariableClass"/>.</value>
        /// <remarks><para>
        /// <b>Category</b> never changes once the object has been constructed.
        /// </para><para>
        /// The value of this property determines the exact type of the underlying <see
        /// cref="VariableClass"/>: <see cref="AttributeClass"/> or <see cref="ResourceClass"/>.
        /// </para></remarks>

        public VariableCategory Category {
            [DebuggerStepThrough]
            get { return VariableClass.Category; }
        }

        #endregion
        #region Id

        /// <summary>
        /// Gets the identifier of the <see cref="Variable"/>.</summary>
        /// <value>
        /// The value of the <see cref="Scenario.VariableClass.Id"/> property of the underlying <see
        /// cref="VariableClass"/>.</value>
        /// <remarks><para>
        /// <b>Id</b> never returns a null reference or an empty string.
        /// </para><para>
        /// <b>Id</b> returns an internal identifier that is not intended for display in Hexkit
        /// Game.</para></remarks>

        public string Id {
            [DebuggerStepThrough]
            get { return VariableClass.Id; }
        }

        #endregion
        #region InitialValue

        /// <summary>
        /// Gets the initial value of the <see cref="Variable"/>.</summary>
        /// <value>
        /// An <see cref="Int32"/> value representing the initial value of the <see
        /// cref="Variable"/>.</value>
        /// <remarks><para>
        /// <b>InitialValue</b> is never smaller than <see cref="Minimum"/> or greater than <see
        /// cref="Maximum"/>. Use the method <see cref="SetInitialValue"/> to set this property.
        /// </para><para>
        /// <b>InitialValue</b> equals the <see cref="Value"/> property at the time of construction.
        /// This is the original scenario value associated with the <see cref="Variable"/> if <see
        /// cref="IsScenario"/> is <c>true</c>; otherwise, it is the first value that the rule
        /// script defines for the <b>Variable</b>.</para></remarks>

        public int InitialValue { get; private set; }

        #endregion
        #region IsBasic

        /// <summary>
        /// Gets a value indicating whether the <see cref="Variable"/> represents a basic value.
        /// </summary>
        /// <value>
        /// <c>true</c> if the <see cref="Variable"/> represents a <see
        /// cref="VariablePurpose.Basic"/> value; <c>false</c> if it represents a <see
        /// cref="VariablePurpose.Modifier"/> value.</value>
        /// <remarks>
        /// <b>IsBasic</b> never changes once the object has been constructed.</remarks>

        public bool IsBasic {
            [DebuggerStepThrough]
            get { return ((Purpose & VariablePurpose.Basic) != 0); }
        }

        #endregion
        #region IsDepletableResource

        /// <summary>
        /// Gets a value indicating whether the <see cref="Variable"/> represents an <see
        /// cref="Entity"/> resource that is considered depletable.</summary>
        /// <value><para>
        /// <c>true</c> if the following conditions are met; otherwise, <c>false</c>:
        /// </para><list type="bullet"><item>
        /// <see cref="Purpose"/> contains the <see cref="VariablePurpose.Entity"/> and <see
        /// cref="VariablePurpose.Basic"/> flags.
        /// </item><item>
        /// The underlying <see cref="VariableClass"/> is a <see cref="ResourceClass"/> whose <see
        /// cref="ResourceClass.IsDepletable"/> flag is <c>true</c>.</item></list></value>

        public bool IsDepletableResource {
            get {
                // only basic entity values are depletable
                const VariablePurpose entityBasic = (VariablePurpose.Entity | VariablePurpose.Basic);
                if ((Purpose & entityBasic) != entityBasic)
                    return false;

                // check for depletable resource class
                ResourceClass resourceClass = VariableClass as ResourceClass;
                return (resourceClass != null ? resourceClass.IsDepletable : false);
            }
        }

        #endregion
        #region IsLimitedResource

        /// <summary>
        /// Gets a value indicating whether the <see cref="Variable"/> represents an <see
        /// cref="Entity"/> resource that is limited by its <see cref="InitialValue"/>.</summary>
        /// <value><para>
        /// <c>true</c> if the following conditions are met; otherwise, <c>false</c>:
        /// </para><list type="bullet"><item>
        /// <see cref="Purpose"/> contains the <see cref="VariablePurpose.Entity"/> and <see
        /// cref="VariablePurpose.Basic"/> flags.
        /// </item><item>
        /// The underlying <see cref="VariableClass"/> is a <see cref="ResourceClass"/> whose <see
        /// cref="ResourceClass.IsLimited"/> flag is <c>true</c>.</item></list></value>

        public bool IsLimitedResource {
            get {
                // only basic entity values are limited
                const VariablePurpose entityBasic = (VariablePurpose.Entity | VariablePurpose.Basic);
                if ((Purpose & entityBasic) != entityBasic)
                    return false;

                // check for limited resource class
                ResourceClass resourceClass = VariableClass as ResourceClass;
                return (resourceClass != null ? resourceClass.IsLimited : false);
            }
        }

        #endregion
        #region IsModifier

        /// <summary>
        /// Gets a value indicating whether the <see cref="Variable"/> represents a modifier value.
        /// </summary>
        /// <value>
        /// <c>true</c> if the <see cref="Variable"/> represents a <see
        /// cref="VariablePurpose.Modifier"/> value; <c>false</c> if it represents a <see
        /// cref="VariablePurpose.Basic"/> value.</value>
        /// <remarks>
        /// <b>IsModifier</b> never changes once the object has been constructed.</remarks>

        public bool IsModifier {
            [DebuggerStepThrough]
            get { return ((Purpose & VariablePurpose.Modifier) != 0); }
        }

        #endregion
        #region IsResettingResource

        /// <summary>
        /// Gets a value indicating whether the <see cref="Variable"/> represents a <see
        /// cref="Faction"/> resource that should be reset to its <see cref="InitialValue"/> at the
        /// start of each turn.</summary>
        /// <value><para>
        /// <c>true</c> if the following conditions are met; otherwise, <c>false</c>:
        /// </para><list type="bullet"><item>
        /// <see cref="Purpose"/> contains the <see cref="VariablePurpose.Faction"/> and <see
        /// cref="VariablePurpose.Basic"/> flags.
        /// </item><item>
        /// The underlying <see cref="VariableClass"/> is a <see cref="ResourceClass"/> whose <see
        /// cref="ResourceClass.IsResetting"/> flag is <c>true</c>.</item></list></value>

        public bool IsResettingResource {
            get {
                // only basic faction values are limited
                const VariablePurpose factionBasic = (VariablePurpose.Faction | VariablePurpose.Basic);
                if ((Purpose & factionBasic) != factionBasic)
                    return false;

                // check for resetting resource class
                ResourceClass resourceClass = VariableClass as ResourceClass;
                return (resourceClass != null ? resourceClass.IsResetting : false);
            }
        }

        #endregion
        #region IsScenario

        /// <summary>
        /// Gets a value indicating whether the <see cref="Variable"/> is defined by the scenario.
        /// </summary>
        /// <value>
        /// <c>true</c> if the <see cref="Variable"/> represents a value that was defined by the
        /// <see cref="VariablePurpose.Scenario"/>; otherwise, <c>false</c>.</value>
        /// <remarks><para>
        /// <b>IsScenario</b> never changes once the object has been constructed.
        /// </para><para>
        /// If <b>IsScenario</b> is <c>true</c>, the creation of this <see cref="Variable"/> was
        /// requested by the current scenario, and <see cref="InitialValue"/> holds the original
        /// scenario value associated with the <b>Variable</b>.
        /// </para><para>
        /// If <b>IsScenario</b> is <c>false</c>, this <b>Variable</b> was created by the rule
        /// script. There is no counterpart in the current scenario, and <b>InitialValue</b> holds
        /// the first value that the rule script defines for the <b>Variable</b>.</para></remarks>

        public bool IsScenario {
            [DebuggerStepThrough]
            get { return ((Purpose & VariablePurpose.Scenario) != 0); }
        }

        #endregion
        #region Maximum

        /// <summary>
        /// Gets the maximum value for the <see cref="Variable"/>.</summary>
        /// <value><list type="bullet"><item>
        /// The constant value <see cref="Scenario.VariableClass.AbsoluteMaximum"/> if <see
        /// cref="IsModifier"/> is <c>true</c>.
        /// </item><item>
        /// Otherwise, the value of the <see cref="InitialValue"/> property if <see
        /// cref="IsLimitedResource"/> is <c>true</c>.
        /// </item><item>
        /// Otherwise, the value of the <see cref="Scenario.VariableClass.Maximum"/> property of the
        /// underlying <see cref="VariableClass"/>.</item></list></value>

        public int Maximum {
            get {
                return (IsModifier ? Scenario.VariableClass.AbsoluteMaximum :
                    (IsLimitedResource ? InitialValue : VariableClass.Maximum));
            }
        }

        #endregion
        #region Minimum

        /// <summary>
        /// Gets the minimum value for the <see cref="Variable"/>.</summary>
        /// <value><list type="bullet"><item>
        /// The constant value <see cref="Scenario.VariableClass.AbsoluteMinimum"/> if <see
        /// cref="IsModifier"/> is <c>true</c>.
        /// </item><item>
        /// Otherwise, the value of the <see cref="Scenario.VariableClass.Minimum"/> property of the
        /// underlying <see cref="VariableClass"/>.</item></list></value>

        public int Minimum {
            get {
                return (IsModifier ? Scenario.VariableClass.AbsoluteMinimum :
                    VariableClass.Minimum);
            }
        }

        #endregion
        #region Name

        /// <summary>
        /// Gets the display name of the <see cref="Variable"/>.</summary>
        /// <value>
        /// The value of the <see cref="Scenario.VariableClass.Name"/> property of the underlying
        /// <see cref="VariableClass"/>.</value>

        public string Name {
            [DebuggerStepThrough]
            get { return VariableClass.Name; }
        }

        #endregion
        #region Purpose

        /// <summary>
        /// Gets the purpose of the <see cref="Variable"/>.</summary>
        /// <value>
        /// A <see cref="VariablePurpose"/> value indicating the purpose of the <see
        /// cref="Variable"/>.</value>
        /// <remarks>
        /// <b>Purpose</b> never changes once the object has been constructed. This property always
        /// contains one of the valid bitwise combinations defined for the <see
        /// cref="VariablePurpose"/> enumeration.</remarks>

        public VariablePurpose Purpose {
            [DebuggerStepThrough]
            get { return this._purpose; }
        }

        #endregion
        #region Value

        /// <summary>
        /// Gets the current value of the <see cref="Variable"/>.</summary>
        /// <value>
        /// An <see cref="Int32"/> value representing the current value of the <see
        /// cref="Variable"/>.</value>
        /// <remarks>
        /// <b>Value</b> is never smaller than <see cref="Minimum"/> or greater than <see
        /// cref="Maximum"/>. Use the method <see cref="SetValue"/> to set this property.</remarks>

        public int Value { get; private set; }

        #endregion
        #region VariableClass

        /// <summary>
        /// Gets the scenario class of the <see cref="Variable"/>.</summary>
        /// <value>
        /// The <see cref="Scenario.VariableClass"/> on which the <see cref="Variable"/> is based.
        /// </value>
        /// <remarks>
        /// <b>VariableClass</b> never returns a null reference. This property never changes once
        /// the object has been constructed.</remarks>

        public VariableClass VariableClass {
            [DebuggerStepThrough]
            get { return this._variableClass; }
        }

        #endregion
        #endregion
        #region CheckPurpose

        /// <summary>
        /// Checks that the specified <see cref="VariablePurpose"/> value is valid.</summary>
        /// <param name="purpose">
        /// The <see cref="VariablePurpose"/> value to examine.</param>
        /// <returns>
        /// <c>true</c> if the specified <paramref name="purpose"/> is a valid bitwise combination
        /// of <see cref="VariablePurpose"/> values; otherwise, <c>false</c>.</returns>

        public static bool CheckPurpose(VariablePurpose purpose) {
            switch (purpose & ~VariablePurpose.Scenario) {

                case (VariablePurpose.Basic | VariablePurpose.Entity):
                case (VariablePurpose.Basic | VariablePurpose.Faction):
                case (VariablePurpose.Modifier | VariablePurpose.Entity):
                case (VariablePurpose.Modifier | VariablePurpose.Faction):
                    return true;

                default:
                    return false;
            }
        }

        #endregion
        #region CreateCollection(CategorizedValueDictionary, ...)

        /// <summary>
        /// Creates a new <see cref="VariableList"/> from the entries of the specified <see
        /// cref="CategorizedValueDictionary"/>.</summary>
        /// <param name="dictionary">
        /// A <see cref="CategorizedValueDictionary"/> that maps <see
        /// cref="Scenario.VariableClass.Id"/> strings of <see cref="Scenario.VariableClass"/>
        /// objects to <see cref="CategorizedValue"/> objects.</param>
        /// <param name="category">
        /// The initial value for the <see cref="Category"/> properties of all newly created <see
        /// cref="Variable"/> objects.</param>
        /// <param name="purpose">
        /// The initial value for the <see cref="Purpose"/> properties of all newly created <see
        /// cref="Variable"/> objects.</param>
        /// <returns><para>
        /// A <see cref="VariableList"/> that contains one <see cref="Variable"/> element for each
        /// entry in the specified <paramref name="dictionary"/>. The collection ensures that all
        /// keys and elements are unique.
        /// </para><para>
        /// The <see cref="Value"/> property of each element is set to the <see
        /// cref="CategorizedValue.Total"/> component of the corresponding <paramref
        /// name="dictionary"/> entry.</para></returns>
        /// <exception cref="ArgumentException">
        /// <paramrefa name="dictionary"/> contains a <see cref="String"/> key that does not match
        /// any  of the <see cref="VariableClass"/> objects defined by the current <see
        /// cref="VariableSection"/>.</exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="dictionary"/> is a null reference.</exception>
        /// <exception cref="ArgumentNullOrEmptyException">
        /// <paramref name="dictionary"/> contains a <see cref="String"/> key that is a null
        /// reference or an empty string.</exception>
        /// <exception cref="InvalidEnumArgumentException">
        /// <paramref name="purpose"/> is not a valid bitwise combination of <see
        /// cref="VariablePurpose"/> values.</exception>
        /// <remarks><para>
        /// <b>CreateCollection</b> never returns a null reference, but it returns an empty
        /// collection if the specified <paramref name="dictionary"/> is empty.
        /// </para><para>
        /// All <see cref="CategorizedValue.Total"/> components in the specified <paramref
        /// name="dictionary"/> are restricted to the legal range for the corresponding <see
        /// cref="Variable"/>, as defined by its <see cref="Minimum"/> and <see cref="Maximum"/>
        /// values.</para></remarks>

        public static VariableList CreateCollection(CategorizedValueDictionary dictionary,
            VariableCategory category, VariablePurpose purpose) {

            if (dictionary == null)
                ThrowHelper.ThrowArgumentNullException("dictionary");

            var collection = new VariableList(dictionary.Count, true);

            for (int i = 0; i < dictionary.Count; i++) {
                string id = dictionary.GetKey(i);
                int value = dictionary.GetByIndex(i).Total;
                collection.Add(new Variable(id, category, purpose, value));
            }

            return collection;
        }

        #endregion
        #region CreateCollection(VariableValueDictionary, ...)

        /// <summary>
        /// Creates a new <see cref="VariableList"/> from the entries of the specified <see
        /// cref="VariableValueDictionary"/>.</summary>
        /// <param name="dictionary">
        /// A <see cref="VariableValueDictionary"/> that maps <see
        /// cref="Scenario.VariableClass.Id"/> strings of <see cref="Scenario.VariableClass"/>
        /// objects to <see cref="Int32"/> values.</param>
        /// <param name="category">
        /// The initial value for the <see cref="Category"/> properties of all newly created <see
        /// cref="Variable"/> objects.</param>
        /// <param name="purpose">
        /// The initial value for the <see cref="Purpose"/> properties of all newly created <see
        /// cref="Variable"/> objects.</param>
        /// <param name="readOnly">
        /// <c>true</c> to return a read-only <see cref="VariableList"/>; <c>false</c> to return a
        /// writable <see cref="VariableList"/>.</param>
        /// <returns>
        /// A <see cref="VariableList"/> that contains one <see cref="Variable"/> element for each
        /// entry in the specified <paramref name="dictionary"/>. The collection ensures that all
        /// keys and elements are unique.</returns>
        /// <exception cref="ArgumentException">
        /// <paramrefa name="dictionary"/> contains a <see cref="String"/> key that does not match
        /// any of the <see cref="VariableClass"/> objects defined by the current scenario <see
        /// cref="VariableSection"/>.</exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="dictionary"/> is a null reference.</exception>
        /// <exception cref="ArgumentNullOrEmptyException">
        /// <paramref name="dictionary"/> contains a <see cref="String"/> key that is a null
        /// reference or an empty string.</exception>
        /// <exception cref="InvalidEnumArgumentException">
        /// <paramref name="purpose"/> is not a valid bitwise combination of <see
        /// cref="VariablePurpose"/> values.</exception>
        /// <remarks><para>
        /// <b>CreateCollection</b> never returns a null reference, but it returns an empty
        /// collection if the specified <paramref name="dictionary"/> is empty.
        /// </para><para>
        /// All <see cref="Int32"/> values in the specified <paramref name="dictionary"/> are
        /// restricted to the legal range for the corresponding <see cref="Variable"/>, as defined
        /// by its <see cref="Minimum"/> and <see cref="Maximum"/> values.</para></remarks>

        public static VariableList CreateCollection(VariableValueDictionary dictionary,
            VariableCategory category, VariablePurpose purpose, bool readOnly) {

            if (dictionary == null)
                ThrowHelper.ThrowArgumentNullException("dictionary");

            var collection = new VariableList(dictionary.Count, true);

            for (int i = 0; i < dictionary.Count; i++) {
                string id = dictionary.GetKey(i);
                int value = dictionary.GetByIndex(i);
                collection.Add(new Variable(id, category, purpose, value));
            }

            return (readOnly ? collection.AsReadOnly() : collection);
        }

        #endregion
        #region CreateCollections(VariableModifierDictionary, ...)

        /// <summary>
        /// Creates a new <see cref="VariableList"/> array from the entries of the specified <see
        /// cref="VariableModifierDictionary"/>.</summary>
        /// <param name="dictionary">
        /// A <see cref="VariableModifierDictionary"/> that maps <see
        /// cref="Scenario.VariableClass.Id"/> strings of <see cref="Scenario.VariableClass"/>
        /// objects to <see cref="VariableModifier"/> objects.</param>
        /// <param name="category">
        /// The initial value for the <see cref="Category"/> properties of all newly created <see
        /// cref="Variable"/> objects.</param>
        /// <param name="purpose">
        /// The initial value for the <see cref="Purpose"/> properties of all newly created <see
        /// cref="Variable"/> objects.</param>
        /// <param name="readOnly">
        /// <c>true</c> to return read-only <see cref="VariableList"/> collections; <c>false</c> to
        /// return writable <see cref="VariableList"/> collections.</param>
        /// <returns>
        /// An <see cref="Array"/> containing one <see cref="VariableList"/> for each <see
        /// cref="ModifierTarget"/>. Each collection contains one <see cref="Variable"/> element for
        /// the corresponding modifier of each entry in the specified <paramref name="dictionary"/>.
        /// The collections ensure that all keys and elements are unique.</returns>
        /// <exception cref="ArgumentException">
        /// <paramref name="dictionary"/> contains a <see cref="String"/> key that does not match
        /// any of the <see cref="VariableClass"/> objects defined by the current scenario <see
        /// cref="VariableSection"/>.</exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="dictionary"/> is a null reference.</exception>
        /// <exception cref="ArgumentNullOrEmptyException">
        /// <paramref name="dictionary"/> contains a <see cref="String"/> key that is a null
        /// reference or an empty string.</exception>
        /// <exception cref="InvalidEnumArgumentException">
        /// <paramref name="purpose"/> is not a valid bitwise combination of <see
        /// cref="VariablePurpose"/> values.</exception>
        /// <remarks><para>
        /// <b>CreateCollections</b> never returns a null reference, but it returns an array of
        /// empty collections if the specified <paramref name="dictionary"/> is empty.
        /// </para><para>
        /// All <see cref="Int32"/> values in the specified <paramref name="dictionary"/> are
        /// restricted to the legal range for the corresponding <see cref="Variable"/>, as defined
        /// by its <see cref="Minimum"/> and <see cref="Maximum"/> values.</para></remarks>

        public static VariableList[] CreateCollections(VariableModifierDictionary dictionary,
            VariableCategory category, VariablePurpose purpose, bool readOnly) {

            if (dictionary == null)
                ThrowHelper.ThrowArgumentNullException("dictionary");

            // create variable collections for all possible targets
            var collections = new VariableList[VariableModifier.AllModifierTargets.Length];
            for (int i = 0; i < collections.Length; i++)
                collections[i] = new VariableList(0, true);

            // iterate over all defined modifier sets
            for (int i = 0; i < dictionary.Count; i++) {
                string id = dictionary.GetKey(i);
                var modifier = dictionary.GetByIndex(i);

                // create one variable per target of current modifier
                foreach (var target in VariableModifier.AllModifierTargets) {
                    int value = modifier.GetByTarget(target).GetValueOrDefault();
                    if (value != 0) {
                        var variable = new Variable(id, category, purpose, value);
                        collections[(int) target].Add(variable);
                    }
                }
            }

            // make all collections read-only if desired
            if (readOnly)
                for (int i = 0; i < collections.Length; i++)
                    collections[i] = collections[i].AsReadOnly();

            return collections;
        }

        #endregion
        #region CreateDictionary(VariableList)

        /// <overloads>
        /// Creates a new <see cref="Variable"/> dictionary from the elements in the specified <see
        /// cref="Variable"/> collection(s).</overloads>
        /// <summary>
        /// Creates a new <see cref="VariableValueDictionary"/> from the elements in the specified
        /// <see cref="Variable"/> collection.</summary>
        /// <param name="collection">
        /// A <see cref="VariableList"/> whose elements contain the data for the new <see
        /// cref="VariableValueDictionary"/>.</param>
        /// <returns>
        /// A <see cref="VariableValueDictionary"/> that maps all <see cref="Id"/> strings in the
        /// specified <paramref name="collection"/> to the corresponding <see cref="Value"/>
        /// components.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="collection"/> is a null reference.</exception>
        /// <remarks>
        /// <b>CreateDictionary</b> never returns a null reference, but it returns an empty
        /// dictionary if the specified <paramref name="collection"/> is empty.</remarks>

        public static VariableValueDictionary CreateDictionary(VariableList collection) {
            if (collection == null)
                ThrowHelper.ThrowArgumentNullException("collection");

            var dictionary = new VariableValueDictionary(collection.Count);

            for (int i = 0; i < collection.Count; i++) {
                Variable variable = collection[i];
                dictionary.Add(variable.Id, variable.Value);
            }

            return dictionary;
        }

        #endregion
        #region CreateDictionary(VariableList[])

        /// <summary>
        /// Creates a new <see cref="VariableModifierDictionary"/> from the elements in the
        /// specified array of <see cref="Variable"/> collections.</summary>
        /// <param name="collections">
        /// An <see cref="Array"/> containing one <see cref="VariableList"/> for each <see
        /// cref="ModifierTarget"/> whose elements contain the data for the new <see
        /// cref="VariableModifierDictionary"/>.</param>
        /// <returns>
        /// A <see cref="VariableModifierDictionary"/> that maps all <see cref="Id"/> strings in all
        /// specified <paramref name="collections"/> to the corresponding <see
        /// cref="VariableModifier"/> objects.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="collections"/> is a null reference.</exception>
        /// <remarks>
        /// <b>CreateDictionary</b> never returns a null reference, but it returns an empty
        /// dictionary if all specified <paramref name="collections"/> are empty.</remarks>

        public static VariableModifierDictionary CreateDictionary(VariableList[] collections) {
            if (collections == null)
                ThrowHelper.ThrowArgumentNullException("collections");

            var dictionary = new VariableModifierDictionary();

            foreach (var target in VariableModifier.AllModifierTargets) {
                var collection = collections[(int) target];

                for (int i = 0; i < collection.Count; i++) {
                    Variable variable = collection[i];

                    VariableModifier modifier;
                    if (!dictionary.TryGetValue(variable.Id, out modifier)) {
                        modifier = new VariableModifier();
                        dictionary.Add(variable.Id, modifier);
                    }

                    modifier.SetByTarget(target, variable.Value);
                }
            }

            return dictionary;
        }

        #endregion
        #region FromLocation

        /// <summary>
        /// Converts the specified map coordinates to a <see cref="Variable"/> value.</summary>
        /// <param name="location">
        /// A <see cref="PointI"/> value containing the map coordinates to encode.</param>
        /// <returns>
        /// An <see cref="Int32"/> value ranging from <see
        /// cref="Scenario.VariableClass.AbsoluteMinimum"/> to <see
        /// cref="Scenario.VariableClass.AbsoluteMaximum"/> that encodes the specified <paramref
        /// name="location"/>.</returns>
        /// <exception cref="ArgumentException">
        /// <paramref name="location"/> contains coordinates with different signs.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="location"/> contains a coordinate whose absolute value is greater than
        /// <see cref="SimpleXml.MaxSizeIValue"/>.</exception>
        /// <remarks><para>
        /// <b>FromLocation</b> adds the <see cref="PointI.X"/> component of the specified <paramref
        /// name="location"/> to the product of the <see cref="PointI.Y"/> component and the <see
        /// cref="SimpleXml.MaxSizeIValue"/>.
        /// </para><para>
        /// <b>FromLocation</b> and <see cref="ToLocation"/> allow storing the map coordinates of a
        /// <see cref="Site"/> within a single <see cref="Variable"/> value. The encoding also 
        /// supports two negative coordinates, but not mixing positive and negative coordinates.
        /// </para></remarks>

        public static int FromLocation(PointI location) {
            const int max = SimpleXml.MaxSizeIValue;

            if (location.X < -max || location.X > max || location.Y < -max || location.Y > max)
                ThrowHelper.ThrowArgumentOutOfRangeExceptionWithFormat(
                    "location", location, Tektosyne.Strings.ArgumentLessOrGreater, -max, max);

            if (location.X * location.Y < 0)
                ThrowHelper.ThrowArgumentException(
                    "location", Tektosyne.Strings.ArgumentConflictSign);

            return location.X + location.Y * max;
        }

        #endregion
        #region GetRelativeMagnitude

        /// <summary>
        /// Computes the relative magnitude of the current value compared to a specified normal
        /// value.</summary>
        /// <param name="normal">
        /// A value between <see cref="Minimum"/> and <see cref="Maximum"/> that defines a relative
        /// magnitude of one.</param>
        /// <returns><para>
        /// 1.0 if <see cref="Value"/> equals <paramref name="normal"/>.
        /// </para><para>
        /// 0.0 if <paramref name="normal"/> equals <see cref="Minimum"/>.
        /// </para><para>
        /// <see cref="Value"/> minus <b>Minimum</b>, divided by <paramref name="normal"/> minus
        /// <b>Minimum</b>, otherwise.</para></returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="normal"/> is less than <see cref="Minimum"/> or greater than <see
        /// cref="Maximum"/>.</exception>
        /// <exception cref="PropertyValueException">
        /// <see cref="IsModifier"/> is <c>true</c>.</exception>

        public double GetRelativeMagnitude(int normal) {
            if (IsModifier)
                ThrowHelper.ThrowPropertyValueException(
                    "IsModifier", Tektosyne.Strings.PropertyTrue);

            int minimum = Minimum; // buffer for speed
            Debug.Assert(Value >= minimum);

            if (normal < minimum)
                ThrowHelper.ThrowArgumentOutOfRangeExceptionWithFormat(
                    "normal", normal, Tektosyne.Strings.ArgumentLessValue, minimum);

            if (normal > Maximum)
                ThrowHelper.ThrowArgumentOutOfRangeExceptionWithFormat(
                    "normal", normal, Tektosyne.Strings.ArgumentGreaterValue, Maximum);

            if (normal == Value) return 1.0;

            if (Value == minimum || normal == minimum)
                return 0.0;

            return (Value - minimum) / (double) (normal - minimum);
        }

        #endregion
        #region GetVariableClass

        /// <summary>
        /// Returns the <see cref="VariableClass"/> with the specified identifier and category.
        /// </summary>
        /// <param name="classId">
        /// The <see cref="Scenario.VariableClass.Id"/> string of the <see
        /// cref="Scenario.VariableClass"/> object that is the initial value for the <see
        /// cref="VariableClass"/> property.</param>
        /// <param name="category">
        /// The initial value for the <see cref="Category"/> property.</param>
        /// <returns>
        /// The <see cref="VariableClass"/> with the specified <paramref name="classId"/> and
        /// <paramref name="category"/>.</returns>
        /// <exception cref="ArgumentException">
        /// <see cref="ApplicationInfo.IsEditor"/> is <c>false</c>, and <paramref name="classId"/>
        /// does not match any <see cref="VariableClass"/> in the specified <paramref
        /// name="category"/> defined by the current <see cref="VariableSection"/>.</exception>
        /// <exception cref="ArgumentNullOrEmptyException">
        /// <paramref name="classId"/> is a null reference or an empty string.</exception>
        /// <exception cref="InvalidEnumArgumentException">
        /// <paramref name="category"/> is not a valid <see cref="VariableCategory"/> value.
        /// </exception>
        /// <remarks>
        /// If <see cref="ApplicationInfo.IsEditor"/> is <c>true</c> and the specified <paramref
        /// name="classId"/> was not found in the specified <paramref name="category"/>, 
        /// <b>GetVariableClass</b> returns a newly created <see cref="Scenario.VariableClass"/>
        /// object with the specified parameters.</remarks>

        private static VariableClass GetVariableClass(string classId, VariableCategory category) {

            VariableClass variableClass =
                MasterSection.Instance.Variables.GetVariable(classId, category);

            if (variableClass == null)
                ThrowHelper.ThrowArgumentExceptionWithFormat(
                    "classId", Tektosyne.Strings.ArgumentSpecifiesInvalid, "VariableClass");

            return variableClass;
        }

        #endregion
        #region RestrictInitialValue

        /// <summary>
        /// Restricts the specified value to the legal range for an <see cref="InitialValue"/> of
        /// the <see cref="Variable"/>.</summary>
        /// <param name="value">
        /// The <see cref="Int32"/> value to restrict.</param>
        /// <returns><list type="bullet"><item>
        /// <see cref="Minimum"/> if <paramref name="value"/> is less than <see cref="Minimum"/>.
        /// </item><item>
        /// <see cref="Maximum"/> if <paramref name="value"/> is greater than <see cref="Maximum"/>
        /// while ignoring the current <see cref="InitialValue"/>.
        /// </item><item>
        /// <paramref name="value"/> otherwise.</item></list></returns>

        internal int RestrictInitialValue(int value) {

            // determine minimum as usual
            int minimum = Minimum;
            if (value < minimum) return minimum;

            // determine maximum without InitialValue
            int maximum = (IsModifier ?
                Scenario.VariableClass.AbsoluteMaximum : VariableClass.Maximum);
            if (value > maximum) return maximum;

            return value;
        }

        #endregion
        #region RestrictValue

        /// <summary>
        /// Restricts the specified value to the legal range for the <see cref="Variable"/>.
        /// </summary>
        /// <param name="value">
        /// The <see cref="Int32"/> value to restrict.</param>
        /// <returns><list type="bullet"><item>
        /// <see cref="Minimum"/> if <paramref name="value"/> is less than <see cref="Minimum"/>.
        /// </item><item>
        /// <see cref="Maximum"/> if <paramref name="value"/> is greater than <see cref="Maximum"/>.
        /// </item><item>
        /// <paramref name="value"/> otherwise.</item></list></returns>

        public int RestrictValue(int value) {

            int minimum = Minimum;
            if (value < minimum) return minimum;

            int maximum = Maximum;
            if (value > maximum) return maximum;

            return value;
        }

        #endregion
        #region SetInitialValue

        /// <summary>
        /// Sets the <see cref="InitialValue"/> property to the specified value.</summary>
        /// <param name="value">
        /// The new value for <see cref="InitialValue"/> property.</param>
        /// <returns>
        /// <c>true</c> if the <see cref="InitialValue"/> property was changed; otherwise,
        /// <c>false</c>.</returns>
        /// <remarks><para>
        /// <b>SetInitialValue</b> restricts the specified <paramref name="value"/> to the legal
        /// range for the <see cref="Variable"/>, as defined by <see cref="Minimum"/> and <see
        /// cref="Maximum"/> but ignoring the current <see cref="InitialValue"/>, before attempting
        /// to set the <see cref="InitialValue"/> property.
        /// </para><para>
        /// If <see cref="InitialValue"/> was changed and <see cref="IsBasic"/> is <c>true</c>,
        /// <b>SetInitialValue</b> also sets the <see cref="Value"/> property to the specified
        /// <paramref name="value"/> if either of the following conditions holds:
        /// </para><para><list type="bullet"><item>
        /// <see cref="Category"/> equals <see cref="VariableCategory.Attribute"/>, indicating a
        /// basic attribute value that defaults to its <see cref="InitialValue"/> before modifiers
        /// apply. Clients must reapply attribute modifiers after <b>SetInitialValue</b> returns.
        /// </item><item>
        /// <see cref="IsLimitedResource"/> is <c>true</c>, indicating a basic resource value that
        /// is limited by its <see cref="InitialValue"/>, and the current <see cref="Value"/> is
        /// greater than the specified <paramref name="value"/>.</item></list></para></remarks>

        internal bool SetInitialValue(int value) {

            // quit if restricted value unchanged
            value = RestrictInitialValue(value);
            if (value == InitialValue) return false;

            InitialValue = value;

            // reset current value if necessary
            if (IsBasic &&
                (Category == VariableCategory.Attribute || (IsLimitedResource && Value > value)))
                Value = value;

            return true;
        }

        #endregion
        #region SetValue

        /// <summary>
        /// Sets the <see cref="Value"/> property to the specified value.</summary>
        /// <param name="value">
        /// The new value for the <see cref="Value"/> property.</param>
        /// <returns>
        /// <c>true</c> if the <see cref="Value"/> property was changed; otherwise, <c>false</c>.
        /// </returns>
        /// <remarks>
        /// <b>SetValue</b> restricts the specified <paramref name="value"/> to the legal range for
        /// the <see cref="Variable"/>, as defined by <see cref="Minimum"/> and <see
        /// cref="Maximum"/>, before attempting to set the <see cref="Value"/> property.</remarks>

        internal bool SetValue(int value) {

            // quit if restricted value unchanged
            value = RestrictValue(value);
            if (value == Value) return false;

            Value = value;
            return true;
        }

        #endregion
        #region ToLocation

        /// <summary>
        /// Converts the specified <see cref="Variable"/> value to map coordinates.</summary>
        /// <param name="value">
        /// An <see cref="Int32"/> value containing encoded map coordinates.</param>
        /// <returns>
        /// A <see cref="PointI"/> value that represents the map coordinates encoded in the
        /// specified <paramref name="value"/>. Both coordinates are either positive or negative, 
        /// depending on the sign of <paramref name="value"/>, and their absolute values do not
        /// exceed <see cref="SimpleXml.MaxSizeIValue"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="value"/> is less than <see
        /// cref="Scenario.VariableClass.AbsoluteMinimum"/> or greater than <see
        /// cref="Scenario.VariableClass.AbsoluteMaximum"/>.</exception>
        /// <remarks><para>
        /// <b>ToLocation</b> sets the <see cref="PointI.Y"/> component of the returned <see
        /// cref="PointI"/> value to the specified <paramref name="value"/> divided by <see
        /// cref="SimpleXml.MaxSizeIValue"/>, and the <see cref="PointI.X"/> component to the
        /// remainder of that division.
        /// </para><para>
        /// <see cref="FromLocation"/> and <b>ToLocation</b> allow storing the map coordinates of a
        /// <see cref="Site"/> within a single <see cref="Variable"/> value. The encoding also 
        /// supports two negative coordinates, but not mixing positive and negative coordinates.
        /// </para></remarks>

        public static PointI ToLocation(int value) {

            if (value < VariableClass.AbsoluteMinimum || value > VariableClass.AbsoluteMaximum)
                ThrowHelper.ThrowArgumentOutOfRangeExceptionWithFormat(
                    "value", value, Tektosyne.Strings.ArgumentLessOrGreater,
                        -VariableClass.AbsoluteMinimum, VariableClass.AbsoluteMaximum);

            int y = value / SimpleXml.MaxSizeIValue;
            int x = value - y * SimpleXml.MaxSizeIValue;

            return new PointI(x, y);
        }

        #endregion
        #region ToString

        /// <summary>
        /// Returns a <see cref="String"/> that represents the <see cref="Variable"/>.</summary>
        /// <returns>
        /// A <see cref="String"/> that represents the current <see cref="Value"/>, divided by the
        /// <see cref="Scenario.VariableClass.Scale"/> of the underlying <see
        /// cref="VariableClass"/>.</returns>
        /// <remarks>
        /// <b>ToString</b> calls <see cref="Scenario.VariableClass.Format"/> on the underlying <see
        /// cref="VariableClass"/> to obtain a string representation of the current <see
        /// cref="Value"/>.</remarks>

        public override string ToString() {
            return VariableClass.Format(Value, IsModifier);
        }

        #endregion
        #region ICloneable Members

        /// <summary>
        /// Creates a new <see cref="Variable"/> object that is a deep copy of the current instance.
        /// </summary>
        /// <returns>
        /// A new <see cref="Variable"/> object that is a deep copy of the current instance.
        /// </returns>
        /// <remarks><para>
        /// <b>Clone</b> processes the properties of the current instance as follows:
        /// </para><list type="table"><listheader>
        /// <term>Property</term><description>Action</description>
        /// </listheader><item>
        /// <term><see cref="InitialValue"/><br/> <see cref="Purpose"/><br/> <see
        /// cref="Value"/><br/> <see cref="VariableClass"/></term>
        /// <description>Values copied to the new instance.</description>
        /// </item><item>
        /// <term><see cref="Category"/><br/> <see cref="Id"/><br/> <see cref="Name"/></term>
        /// <description>Values provided by <see cref="VariableClass"/>.</description>
        /// </item><item>
        /// <term><see cref="IsDepletableResource"/><br/> <see cref="IsLimitedResource"/><br/> <see
        /// cref="IsModifier"/><br/> <see cref="IsResettingResource"/><br/> <see
        /// cref="IsScenario"/><br/> <see cref="Maximum"/><br/> <see cref="Minimum"/></term>
        /// <description>Values provided by <see cref="Purpose"/> and other properties.
        /// </description></item></list></remarks>

        public object Clone() {
            return new Variable(this);
        }

        #endregion
        #region IKeyedValue Members

        /// <summary>
        /// Gets the identifier of the <see cref="Variable"/>.</summary>
        /// <value>
        /// The value of the <see cref="Id"/> property.</value>

        string IKeyedValue<String>.Key {
            [DebuggerStepThrough]
            get { return Id; }
        }

        #endregion
    }
}
