using System;
using System.ComponentModel;
using System.Diagnostics;

using Tektosyne;
using Hexkit.Global;

namespace Hexkit.Scenario {

    /// <summary>
    /// Defines a set of modifier values for a <see cref="VariableClass"/>.</summary>
    /// <remarks><para>
    /// <b>VariableModifier</b> corresponds to the complex XML type "variableModifier" defined in
    /// <see cref="FilePaths.ScenarioSchema"/>.
    /// </para><para>
    /// One <b>VariableModifier</b> instance aggregates all "variableModifier" elements that refer
    /// to the same <see cref="VariableClass"/> identifier. Each "variableModifier" element sets the
    /// properties that correspond to its "target" XML attribute.
    /// </para><para>
    /// The modifier values corresponding to each <see cref="ModifierTarget"/> are represented by
    /// <see cref="Nullable{Int32}"/> instances, rather than plain <see cref="Int32"/> values, and
    /// default to null references. This allows Hexkit Editor and display code to distinguish
    /// between absent values (i.e. null references) and present values that equal zero. Gameplay
    /// code should treat both values as a modifier that has no effect.
    /// </para><note type="implementnotes">
    /// Like all <b>Hexkit.Scenario</b> types, <b>VariableModifier</b> should be immutable outside
    /// of Hexkit Editor. However, any client may currently call the public method <see
    /// cref="VariableModifier.SetByTarget"/> to change property values. This is unfortunately
    /// necessary to allow creation of <see cref="VariableModifier"/> collections by
    /// <b>Hexkit.World</b> types without excessively verbous constructor calls.</note></remarks>

    public sealed class VariableModifier: ICloneable, IEquatable<VariableModifier> {
        #region VariableModifier()

        /// <overloads>
        /// Initializes a new instance of the <see cref="VariableModifier"/> class.</overloads>
        /// <summary>
        /// Initializes a new instance of the <see cref="VariableModifier"/> class with default
        /// properties.</summary>

        public VariableModifier() { }

        #endregion
        #region VariableModifier(VariableModifier)

        /// <summary>
        /// Initializes a new instance of the <see cref="VariableModifier"/> class with property
        /// values copied from the specified instance.</summary>
        /// <param name="modifier">
        /// The <see cref="VariableModifier"/> instance whose property values are copied to the new
        /// instance.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="modifier"/> is a null reference.</exception>
        /// <remarks>
        /// This constructor is called by <see cref="Clone"/> to perform a deep copy of the
        /// specified <paramref name="modifier"/>.</remarks>

        public VariableModifier(VariableModifier modifier) {

            this._self = modifier._self;
            this._owner = modifier._owner;
            this._units = modifier._units;
            this._unitsRanged = modifier._unitsRanged;
            this._ownerUnits = modifier._ownerUnits;
            this._ownerUnitsRanged = modifier._ownerUnitsRanged;
        }

        #endregion
        #region Private Fields

        // property backers
        private int? _self, _owner, _units, _unitsRanged, _ownerUnits, _ownerUnitsRanged;

        #endregion
        #region AllModifierTargets

        /// <summary>
        /// An <see cref="Array"/> comprising all values in <see cref="ModifierTarget"/>.</summary>
        /// <remarks>
        /// <b>AllModifierTargets</b> facilitates iterating through all values of the <see
        /// cref="ModifierTarget"/> enumeration. This field holds the result of
        /// <c>Enum.GetValues(typeof(ModifierTarget))</c>.</remarks>

        public static readonly ModifierTarget[] AllModifierTargets =
            (ModifierTarget[]) Enum.GetValues(typeof(ModifierTarget));

        #endregion
        #region IsEmpty

        /// <summary>
        /// Gets a value indicating whether all <see cref="VariableClass"/> modifiers are zero or
        /// undefined.</summary>
        /// <value>
        /// <c>true</c> if all <see cref="VariableClass"/> modifiers defined by the <see
        /// cref="VariableModifier"/> are zero or null references; otherwise, <c>false</c>.</value>
        /// <remarks>
        /// An empty <see cref="VariableModifier"/> has no effect and may be discarded.</remarks>

        public bool IsEmpty {
            get {
                return (Self.GetValueOrDefault() == 0
                    && Owner.GetValueOrDefault() == 0
                    && Units.GetValueOrDefault() == 0
                    && UnitsRanged.GetValueOrDefault() == 0
                    && OwnerUnits.GetValueOrDefault() == 0
                    && OwnerUnitsRanged.GetValueOrDefault() == 0);
            }
        }

        #endregion
        #region Owner

        /// <summary>
        /// Gets or sets the <see cref="VariableClass"/> modifier that affects the owner of the
        /// defining entity.</summary>
        /// <value>
        /// The <see cref="VariableClass"/> modifier affecting the faction that owns the entity that
        /// defines the <see cref="VariableModifier"/>. The default is a null reference.</value>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The property is set to a value that is less than <see
        /// cref="VariableClass.AbsoluteMinimum"/> or greater than <see
        /// cref="VariableClass.AbsoluteMaximum"/>.</exception>
        /// <exception cref="InvalidOperationException">
        /// The property is set, and <see cref="ApplicationInfo.IsEditor"/> is <c>false</c>.
        /// </exception>
        /// <remarks>
        /// <b>Owner</b> holds the value of the XML element whose "target" attribute equals <see
        /// cref="ModifierTarget.Owner"/>. This modifier has no effect for <see
        /// cref="AttributeClass"/> variables, or if the defining entity is unowned.</remarks>

        public int? Owner {
            [DebuggerStepThrough]
            get { return this._owner; }
            [DebuggerStepThrough]
            set {
                ApplicationInfo.CheckEditor();
                VariableClass.CheckAbsoluteRange(value);
                this._owner = value;
            }
        }

        #endregion
        #region OwnerUnits

        /// <summary>
        /// Gets or sets the <see cref="VariableClass"/> modifier that affects local units with the
        /// same owner as the defining entity.</summary>
        /// <value>
        /// The <see cref="VariableClass"/> modifier affecting local units with the same owner as
        /// the entity that defines the <see cref="VariableModifier"/>. The default is a null
        /// reference.</value>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The property is set to a value that is less than <see
        /// cref="VariableClass.AbsoluteMinimum"/> or greater than <see
        /// cref="VariableClass.AbsoluteMaximum"/>.</exception>
        /// <exception cref="InvalidOperationException">
        /// The property is set, and <see cref="ApplicationInfo.IsEditor"/> is <c>false</c>.
        /// </exception>
        /// <remarks><para>
        /// <b>OwnerUnits</b> holds the value of the XML element whose "target" attribute equals
        /// <see cref="ModifierTarget.OwnerUnits"/>. This modifier has no effect if the defining
        /// entity is unowned.
        /// </para><para>
        /// The local map site is the <see cref="FactionClass.HomeSite"/> of the owning faction if
        /// the defining entity is an <see cref="UpgradeClass"/> instance; otherwise,
        /// <b>OwnerUnits</b> has no effect if the defining entity is unplaced.
        /// </para><para>
        /// For placed units, the <b>OwnerUnits</b> modifier affects the defining entity itself and
        /// aggregates with the <see cref="Self"/> modifier, if any.</para></remarks>

        public int? OwnerUnits {
            [DebuggerStepThrough]
            get { return this._ownerUnits; }
            [DebuggerStepThrough]
            set {
                ApplicationInfo.CheckEditor();
                VariableClass.CheckAbsoluteRange(value);
                this._ownerUnits = value;
            }
        }

        #endregion
        #region OwnerUnitsRanged

        /// <summary>
        /// Gets or sets the <see cref="VariableClass"/> modifier that affects units within modifier
        /// range and with the same owner as the defining entity.</summary>
        /// <value>
        /// The <see cref="VariableClass"/> modifier affecting units within <see
        /// cref="EntityClass.ModifierRange"/> and with the same owner as the entity that defines
        /// the <see cref="VariableModifier"/>. The default is a null reference.</value>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The property is set to a value that is less than <see
        /// cref="VariableClass.AbsoluteMinimum"/> or greater than <see
        /// cref="VariableClass.AbsoluteMaximum"/>.</exception>
        /// <exception cref="InvalidOperationException">
        /// The property is set, and <see cref="ApplicationInfo.IsEditor"/> is <c>false</c>.
        /// </exception>
        /// <remarks><para>
        /// <b>OwnerUnitsRanged</b> holds the value of the XML element whose "target" attribute
        /// equals <see cref="ModifierTarget.OwnerUnitsRanged"/>. This modifier has no effect if the
        /// defining entity is unowned.
        /// </para><para>
        /// The <see cref="EntityClass.ModifierRange"/> extends from the local map site of the
        /// defining entity, as described for the <see cref="OwnerUnits"/> modifier.
        /// </para></remarks>

        public int? OwnerUnitsRanged {
            [DebuggerStepThrough]
            get { return this._ownerUnitsRanged; }
            [DebuggerStepThrough]
            set {
                ApplicationInfo.CheckEditor();
                VariableClass.CheckAbsoluteRange(value);
                this._ownerUnitsRanged = value;
            }
        }

        #endregion
        #region Self

        /// <summary>
        /// Gets or sets the <see cref="VariableClass"/> modifier that affects the defining entity
        /// itself.</summary>
        /// <value>
        /// The <see cref="VariableClass"/> modifier affecting the entity that defines the <see
        /// cref="VariableModifier"/>. The default is a null reference.</value>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The property is set to a value that is less than <see
        /// cref="VariableClass.AbsoluteMinimum"/> or greater than <see
        /// cref="VariableClass.AbsoluteMaximum"/>.</exception>
        /// <exception cref="InvalidOperationException">
        /// The property is set, and <see cref="ApplicationInfo.IsEditor"/> is <c>false</c>.
        /// </exception>
        /// <remarks>
        /// <b>Self</b> holds the value of the XML element whose "target" attribute equals <see
        /// cref="ModifierTarget.Self"/>. If the defining entity is a placed unit, <b>Self</b>
        /// aggregates with the <see cref="OwnerUnits"/> modifier, if any.</remarks>

        public int? Self {
            [DebuggerStepThrough]
            get { return this._self; }
            [DebuggerStepThrough]
            set {
                ApplicationInfo.CheckEditor();
                VariableClass.CheckAbsoluteRange(value);
                this._self = value;
            }
        }

        #endregion
        #region Units

        /// <summary>
        /// Gets or sets the <see cref="VariableClass"/> modifier that affects any local units.
        /// </summary>
        /// <value>
        /// The <see cref="VariableClass"/> modifier affecting local units, relative to the entity
        /// that defines the <see cref="VariableModifier"/>. The default is a null reference.
        /// </value>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The property is set to a value that is less than <see
        /// cref="VariableClass.AbsoluteMinimum"/> or greater than <see
        /// cref="VariableClass.AbsoluteMaximum"/>.</exception>
        /// <exception cref="InvalidOperationException">
        /// The property is set, and <see cref="ApplicationInfo.IsEditor"/> is <c>false</c>.
        /// </exception>
        /// <remarks><para>
        /// <b>Units</b> holds the value of the XML element whose "target" attribute equals <see
        /// cref="ModifierTarget.Units"/>.
        /// </para><para>
        /// The local map site is the <see cref="FactionClass.HomeSite"/> of the owning faction if
        /// the defining entity is an <see cref="UpgradeClass"/> instance; otherwise, <b>Units</b>
        /// has no effect if the defining entity is unplaced.
        /// </para><para>
        /// If the defining entity is owned, <b>Units</b> only affects units with a different owner.
        /// The <see cref="OwnerUnits"/> modifier applies to local units with the same owner.
        /// </para></remarks>

        public int? Units {
            [DebuggerStepThrough]
            get { return this._units; }
            [DebuggerStepThrough]
            set {
                ApplicationInfo.CheckEditor();
                VariableClass.CheckAbsoluteRange(value);
                this._units = value;
            }
        }

        #endregion
        #region UnitsRanged

        /// <summary>
        /// Gets or sets the <see cref="VariableClass"/> modifier that affects any units within
        /// modifier range.</summary>
        /// <value>
        /// The <see cref="VariableClass"/> modifier affecting units within <see
        /// cref="EntityClass.ModifierRange"/> of the entity that defines the <see
        /// cref="VariableModifier"/>. The default is a null reference.</value>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The property is set to a value that is less than <see
        /// cref="VariableClass.AbsoluteMinimum"/> or greater than <see
        /// cref="VariableClass.AbsoluteMaximum"/>.</exception>
        /// <exception cref="InvalidOperationException">
        /// The property is set, and <see cref="ApplicationInfo.IsEditor"/> is <c>false</c>.
        /// </exception>
        /// <remarks><para>
        /// <b>UnitsRanged</b> holds the value of the XML element whose "target" attribute equals
        /// <see cref="ModifierTarget.UnitsRanged"/>.
        /// </para><para>
        /// The <see cref="EntityClass.ModifierRange"/> extends from the local map site of the
        /// defining entity, as described for the <see cref="Units"/> modifier.
        /// </para><para>
        /// If the defining entity is owned, <b>UnitsRanged</b> only affects units with a different
        /// owner. The <see cref="OwnerUnitsRanged"/> modifier applies to units within modifier
        /// range that have the same owner.</para></remarks>

        public int? UnitsRanged {
            [DebuggerStepThrough]
            get { return this._unitsRanged; }
            [DebuggerStepThrough]
            set {
                ApplicationInfo.CheckEditor();
                VariableClass.CheckAbsoluteRange(value);
                this._unitsRanged = value;
            }
        }

        #endregion
        #region FormatTarget

        /// <summary>
        /// Returns a localized <see cref="String"/> that represents the specified <see
        /// cref="ModifierTarget"/>.</summary>
        /// <param name="target">
        /// The <see cref="ModifierTarget"/> value to format.</param>
        /// <returns>
        /// A localized <see cref="String"/> representation of the specified <paramref
        /// name="target"/>.</returns>
        /// <remarks>
        /// Use <b>FormatTarget</b> to display <see cref="ModifierTarget"/> values in Hexkit Game.
        /// </remarks>

        public static string FormatTarget(ModifierTarget target) {
            switch (target) {

                case ModifierTarget.Self:
                    return Global.Strings.LabelSelf;

                case ModifierTarget.Owner:
                    return Global.Strings.LabelOwner;

                case ModifierTarget.Units:
                case ModifierTarget.UnitsRanged:
                    return Global.Strings.LabelUnits;

                case ModifierTarget.OwnerUnits:
                case ModifierTarget.OwnerUnitsRanged:
                    return Global.Strings.LabelUnitsOwner;

                default:
                    return target.ToString();
            }
        }

        #endregion
        #region GetByTarget

        /// <summary>
        /// Gets the <see cref="VariableClass"/> modifier for the specified <see
        /// cref="ModifierTarget"/>.</summary>
        /// <param name="target">
        /// A <see cref="ModifierTarget"/> value indicating which <see cref="VariableClass"/>
        /// modifier to get.</param>
        /// <returns>
        /// The value of the <see cref="VariableModifier"/> property that corresponds to the
        /// specified <paramref name="target"/>.</returns>
        /// <exception cref="InvalidEnumArgumentException">
        /// <paramref name="target"/> is not a valid <see cref="ModifierTarget"/> value.</exception>

        public int? GetByTarget(ModifierTarget target) {
            switch (target) {

                case ModifierTarget.Self:             return Self;
                case ModifierTarget.Owner:            return Owner;
                case ModifierTarget.Units:            return Units;
                case ModifierTarget.UnitsRanged:      return UnitsRanged;
                case ModifierTarget.OwnerUnits:       return OwnerUnits;
                case ModifierTarget.OwnerUnitsRanged: return OwnerUnitsRanged;

                default:
                    ThrowHelper.ThrowInvalidEnumArgumentException(
                        "target", (int) target, typeof(ModifierTarget));
                    return 0;
            }
        }

        #endregion
        #region GetHashCode

        /// <summary>
        /// Returns the hash code for this <see cref="VariableModifier"/> instance.</summary>
        /// <returns>
        /// An <see cref="Int32"/> hash code.</returns>
        /// <remarks>
        /// <b>GetHashCode</b> returns the value of the <see cref="Self"/> property, or zero if that
        /// value is a null reference.</remarks>

        public override int GetHashCode() {
            return (Self.HasValue ? Self.Value : 0);
        }

        #endregion
        #region SetByTarget

        /// <summary>
        /// Sets the <see cref="VariableClass"/> modifier for the specified <see
        /// cref="ModifierTarget"/>.</summary>
        /// <param name="target">
        /// A <see cref="ModifierTarget"/> value indicating which <see cref="VariableClass"/>
        /// modifier to set.</param>
        /// <param name="value">
        /// The new value for the <see cref="VariableModifier"/> property that corresponds to the
        /// specified <paramref name="target"/>.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="value"/> is less than <see cref="VariableClass.AbsoluteMinimum"/> or
        /// greater than <see cref="VariableClass.AbsoluteMaximum"/>.</exception>
        /// <exception cref="InvalidEnumArgumentException">
        /// <paramref name="target"/> is not a valid <see cref="ModifierTarget"/> value.</exception>

        public void SetByTarget(ModifierTarget target, int? value) {
            VariableClass.CheckAbsoluteRange(value);
            switch (target) {

                case ModifierTarget.Self:             this._self = value; break;
                case ModifierTarget.Owner:            this._owner = value; break;
                case ModifierTarget.Units:            this._units = value; break;
                case ModifierTarget.UnitsRanged:      this._unitsRanged = value; break;
                case ModifierTarget.OwnerUnits:       this._ownerUnits = value; break;
                case ModifierTarget.OwnerUnitsRanged: this._ownerUnitsRanged = value; break;

                default:
                    ThrowHelper.ThrowInvalidEnumArgumentException(
                        "target", (int) target, typeof(ModifierTarget));
                    break;
            }
        }

        #endregion
        #region ICloneable Members

        /// <summary>
        /// Creates a new <see cref="VariableModifier"/> object that is a deep copy of the current
        /// instance.</summary>
        /// <returns>
        /// A new <see cref="VariableModifier"/> object that is a deep copy of the current instance.
        /// </returns>
        /// <remarks>
        /// <b>Clone</b> calls the <see cref="VariableModifier(VariableModifier)"/> copy constructor
        /// with this <see cref="VariableModifier"/> object.</remarks>

        public object Clone() {
            return new VariableModifier(this);
        }

        #endregion
        #region IEquatable Members
        #region Equals(Object)

        /// <overloads>
        /// Determines whether two <see cref="VariableModifier"/> instances have the same value.
        /// </overloads>
        /// <summary>
        /// Determines whether this <see cref="VariableModifier"/> instance and a specified object,
        /// which must be a <see cref="VariableModifier"/>, have the same value.</summary>
        /// <param name="obj">
        /// An <see cref="Object"/> to compare to this <see cref="VariableModifier"/> instance.
        /// </param>
        /// <returns>
        /// <c>true</c> if <paramref name="obj"/> is another <see cref="VariableModifier"/> instance
        /// and its value is the same as this instance; otherwise, <c>false</c>.</returns>
        /// <remarks>
        /// If the specified <paramref name="obj"/> is another <see cref="VariableModifier"/>
        /// instance, <b>Equals</b> invokes the strongly-typed <see
        /// cref="Equals(VariableModifier)"/> overload to test the two instances for value equality.
        /// </remarks>

        public override bool Equals(object obj) {

            var modifier = obj as VariableModifier;
            if (Object.ReferenceEquals(modifier, null))
                return false;

            return Equals(modifier);
        }

        #endregion
        #region Equals(VariableModifier)

        /// <summary>
        /// Determines whether this instance and a specified <see cref="VariableModifier"/> have the
        /// same value.</summary>
        /// <param name="modifier">
        /// A <see cref="VariableModifier"/> to compare to this instance.</param>
        /// <returns>
        /// <c>true</c> if the value of <paramref name="modifier"/> is the same as this instance;
        /// otherwise, <c>false</c>.</returns>
        /// <remarks>
        /// <b>Equals</b> compares all <see cref="VariableClass"/> modifiers of the two <see
        /// cref="VariableModifier"/> instances to test for value equality.</remarks>

        public bool Equals(VariableModifier modifier) {
            if (Object.ReferenceEquals(modifier, null))
                return false;

            return (Self == modifier.Self
                && Owner == modifier.Owner
                && Units == modifier.Units
                && UnitsRanged == modifier.UnitsRanged
                && OwnerUnits == modifier.OwnerUnits
                && OwnerUnitsRanged == modifier.OwnerUnitsRanged);
        }

        #endregion
        #region Equals(VariableModifier, VariableModifier)

        /// <summary>
        /// Determines whether two specified <see cref="VariableModifier"/> instances have the same
        /// value.</summary>
        /// <param name="x">
        /// The first <see cref="VariableModifier"/> to compare.</param>
        /// <param name="y">
        /// The second <see cref="VariableModifier"/> to compare.</param>
        /// <returns>
        /// <c>true</c> if the value of <paramref name="x"/> is the same as the value of <paramref
        /// name="y"/>; otherwise, <c>false</c>.</returns>
        /// <remarks>
        /// <b>Equals</b> invokes the <see cref="Equals(VariableModifier)"/> instance method to test
        /// the two <see cref="VariableModifier"/> instances for value equality.</remarks>

        public static bool Equals(VariableModifier x, VariableModifier y) {

            if (Object.ReferenceEquals(x, null))
                return Object.ReferenceEquals(y, null);

            if (Object.ReferenceEquals(y, null))
                return false;

            return x.Equals(y);
        }

        #endregion
        #endregion
    }
}
