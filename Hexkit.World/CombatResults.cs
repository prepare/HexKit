using System;
using System.Diagnostics;
using System.Globalization;

using Tektosyne;
using Hexkit.World.Commands;

namespace Hexkit.World {

    /// <summary>
    /// Provides the results of an <see cref="AttackCommand"/>.</summary>
    /// <remarks>
    /// <b>CombatResults</b> holds the sums of the original and remaining <see
    /// cref="Unit.Strength"/> values of all units participating in an <see cref="AttackCommand"/>,
    /// as well as the (actual or estimated) losses expressed both as absolute <see
    /// cref="Unit.Strength"/> values and as percentages of the original values.</remarks>

    public struct CombatResults: IEquatable<CombatResults> {
        #region Private Fields

        // property backers
        private int _attackerLosses, _attackerStrength, _defenderLosses, _defenderStrength;

        #endregion
        #region AttackerLosses

        /// <summary>
        /// Gets or sets the total <see cref="Unit.Strength"/> losses of all attacking units.
        /// </summary>
        /// <value>
        /// The sum of the losses to the <see cref="Unit.Strength"/> values of all <see
        /// cref="Unit"/> objects participating in the attack.</value>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The property is set to a negative value.</exception>
        /// <remarks>
        /// When set to a value greater than <see cref="AttackerStrength"/>, <b>AttackerLosses</b>
        /// returns <see cref="AttackerStrength"/> instead.</remarks>

        public int AttackerLosses {
            [DebuggerStepThrough]
            get { return this._attackerLosses; }
            set {
                if (value < 0)
                    ThrowHelper.ThrowArgumentOutOfRangeException(
                        "value", value, Tektosyne.Strings.ArgumentNegative);

                if (value > AttackerStrength)
                    value = AttackerStrength;

                this._attackerLosses = value;
            }
        }

        #endregion
        #region AttackerPercent

        /// <summary>
        /// Gets the relative <see cref="Unit.Strength"/> losses of all attacking units.</summary>
        /// <value>
        /// A value from 0 to 100, obtained by dividing <see cref="AttackerLosses"/> by <see
        /// cref="AttackerStrength"/> and multiplying the result by 100.</value>
        /// <remarks>
        /// <b>AttackerPercent</b> returns zero if <see cref="AttackerStrength"/> is zero.</remarks>

        public int AttackerPercent {
            get {
                return (AttackerStrength == 0 ? 0 :
                    Fortran.NInt(AttackerLosses * 100.0 / AttackerStrength));
            }
        }

        #endregion
        #region AttackerRemainder

        /// <summary>
        /// Gets or sets the total remaining <see cref="Unit.Strength"/> of all attacking units.
        /// </summary>
        /// <value>
        /// The sum of the remaining <see cref="Unit.Strength"/> values of all <see cref="Unit"/>
        /// objects participating in the attack.</value>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The property is set to a value that is greater than <see cref="AttackerStrength"/>.
        /// </exception>
        /// <remarks><para>
        /// <b>AttackerRemainder</b> never returns a negative value, or a value greater than <see
        /// cref="AttackerStrength"/>.
        /// </para><para>
        /// This property is not backed by a separate variable, but by the difference between <see
        /// cref="AttackerStrength"/> and <see cref="AttackerLosses"/>. Setting this property
        /// actually changes <see cref="AttackerLosses"/>.</para></remarks>

        public int AttackerRemainder {
            get { return AttackerStrength - AttackerLosses; }
            set { AttackerLosses = AttackerStrength - value; }
        }

        #endregion
        #region AttackerStrength

        /// <summary>
        /// Gets or sets the total original <see cref="Unit.Strength"/> of all attacking units.
        /// </summary>
        /// <value>
        /// The sum of the original <see cref="Unit.Strength"/> values of all <see cref="Unit"/>
        /// objects participating in the attack.</value>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The property is set to a value that is less than <see cref="AttackerLosses"/>.
        /// </exception>
        /// <remarks>
        /// <b>AttackerStrength</b> never returns a negative value.</remarks>

        public int AttackerStrength {
            [DebuggerStepThrough]
            get { return this._attackerStrength; }
            set {
                if (value < AttackerLosses)
                    ThrowHelper.ThrowArgumentOutOfRangeExceptionWithFormat(
                        "value", value, Tektosyne.Strings.ArgumentLessValue, AttackerLosses);

                this._attackerStrength = value;
            }
        }

        #endregion
        #region DefenderLosses

        /// <summary>
        /// Gets or sets the total <see cref="Unit.Strength"/> losses of all defending units.
        /// </summary>
        /// <value>
        /// The sum of the losses to the <see cref="Unit.Strength"/> values of all <see
        /// cref="Unit"/> objects participating in the defense.</value>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The property is set to a negative value.</exception>
        /// <remarks>
        /// When set to a value greater than <see cref="DefenderStrength"/>, <b>DefenderLosses</b>
        /// returns <see cref="DefenderStrength"/> instead.</remarks>

        public int DefenderLosses {
            [DebuggerStepThrough]
            get { return this._defenderLosses; }
            set {
                if (value < 0)
                    ThrowHelper.ThrowArgumentOutOfRangeException(
                        "value", value, Tektosyne.Strings.ArgumentNegative);

                if (value > DefenderStrength)
                    value = DefenderStrength;

                this._defenderLosses = value;
            }
        }

        #endregion
        #region DefenderPercent

        /// <summary>
        /// Gets the relative <see cref="Unit.Strength"/> losses of all defending units.</summary>
        /// <value>
        /// A value from 0 to 100, obtained by dividing <see cref="DefenderLosses"/> by <see
        /// cref="DefenderStrength"/> and multiplying the result by 100.</value>
        /// <remarks>
        /// <b>DefenderPercent</b> returns zero if <see cref="DefenderStrength"/> is zero.</remarks>

        public int DefenderPercent {
            get {
                return (DefenderStrength == 0 ? 0 :
                    Fortran.NInt(DefenderLosses * 100.0 / DefenderStrength));
            }
        }

        #endregion
        #region DefenderRemainder

        /// <summary>
        /// Gets or sets the total remaining <see cref="Unit.Strength"/> of all defending units.
        /// </summary>
        /// <value>
        /// The sum of the remaining <see cref="Unit.Strength"/> values of all <see cref="Unit"/>
        /// objects participating in the defense.</value>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The property is set to a value that is greater than <see cref="DefenderStrength"/>.
        /// </exception>
        /// <remarks><para>
        /// <b>DefenderRemainder</b> never returns a negative value, or a value greater than <see
        /// cref="DefenderStrength"/>.
        /// </para><para>
        /// This property is not backed by a separate variable, but by the difference between <see
        /// cref="DefenderStrength"/> and <see cref="DefenderLosses"/>. Setting this property
        /// actually changes <see cref="DefenderLosses"/>.</para></remarks>

        public int DefenderRemainder {
            get { return DefenderStrength - DefenderLosses; }
            set { DefenderLosses = DefenderStrength - value; }
        }

        #endregion
        #region DefenderStrength

        /// <summary>
        /// Gets or sets the total original <see cref="Unit.Strength"/> of all defending units.
        /// </summary>
        /// <value>
        /// The sum of the original <see cref="Unit.Strength"/> values of all <see cref="Unit"/>
        /// objects participating in the defense.</value>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The property is set to a value that is less than <see cref="DefenderLosses"/>.
        /// </exception>
        /// <remarks>
        /// <b>DefenderStrength</b> never returns a negative value.</remarks>

        public int DefenderStrength {
            [DebuggerStepThrough]
            get { return this._defenderStrength; }
            set {
                if (value < DefenderLosses)
                    ThrowHelper.ThrowArgumentOutOfRangeExceptionWithFormat(
                        "value", value, Tektosyne.Strings.ArgumentLessValue, DefenderLosses);

                this._defenderStrength = value;
            }
        }

        #endregion
        #region GetHashCode

        /// <summary>
        /// Returns the hash code for this <see cref="CombatResults"/> instance.</summary>
        /// <returns>
        /// An <see cref="Int32"/> hash code.</returns>
        /// <remarks>
        /// <b>GetHashCode</b> returns the value of the <see cref="DefenderLosses"/> property.
        /// </remarks>

        public override int GetHashCode() {
            return DefenderLosses;
        }

        #endregion
        #region ToString

        /// <summary>
        /// Returns a <see cref="String"/> that represents this <see cref="CombatResults"/>
        /// instance.</summary>
        /// <returns>
        /// A <see cref="String"/> containing the values of the <see cref="AttackerStrength"/>, <see
        /// cref="AttackerLosses"/>, <see cref="DefenderStrength"/>, and <see
        /// cref="DefenderLosses"/> properties.</returns>

        public override string ToString() {
            return String.Format(CultureInfo.InvariantCulture,
                "{{AttackerStrength={0}, AttackerLosses={1}, DefenderStrength={2}, DefenderLosses={3}}}",
                AttackerStrength, AttackerLosses, DefenderStrength, DefenderLosses);
        }

        #endregion
        #region Public Operators
        #region operator==

        /// <summary>
        /// Determines whether two <see cref="CombatResults"/> instances have the same value.
        /// </summary>
        /// <param name="x">
        /// The first <see cref="CombatResults"/> instance to compare.</param>
        /// <param name="y">
        /// The second <see cref="CombatResults"/> instance to compare.</param>
        /// <returns>
        /// <c>true</c> if the value of <paramref name="x"/> is the same as the value of <paramref
        /// name="y"/>; otherwise, <c>false</c>.</returns>
        /// <remarks>
        /// This operator invokes the <see cref="Equals(CombatResults)"/> method to test the two
        /// <see cref="CombatResults"/> instances for value equality.</remarks>

        public static bool operator ==(CombatResults x, CombatResults y) {
            return x.Equals(y);
        }

        #endregion
        #region operator!=

        /// <summary>
        /// Determines whether two <see cref="CombatResults"/> instances have different values.
        /// </summary>
        /// <param name="x">
        /// The first <see cref="CombatResults"/> instance to compare.</param>
        /// <param name="y">
        /// The second <see cref="CombatResults"/> instance to compare.</param>
        /// <returns>
        /// <c>true</c> if the value of <paramref name="x"/> is different from the value of
        /// <paramref name="y"/>; otherwise, <c>false</c>.</returns>
        /// <remarks>
        /// This operator invokes the <see cref="Equals(CombatResults)"/> method to test the two
        /// <see cref="CombatResults"/> instances for value inequality.</remarks>

        public static bool operator !=(CombatResults x, CombatResults y) {
            return !x.Equals(y);
        }

        #endregion
        #endregion
        #region IEquatable Members
        #region Equals(Object)

        /// <overloads>
        /// Determines whether two <see cref="CombatResults"/> instances have the same value.
        /// </overloads>
        /// <summary>
        /// Determines whether this <see cref="CombatResults"/> instance and a specified object,
        /// which must be a <see cref="CombatResults"/> instance, have the same value.</summary>
        /// <param name="obj">
        /// An <see cref="Object"/> to compare to this <see cref="CombatResults"/> instance.</param>
        /// <returns>
        /// <c>true</c> if <paramref name="obj"/> is another <see cref="CombatResults"/> instance
        /// and its value is the same as this instance; otherwise, <c>false</c>.</returns>
        /// <remarks>
        /// If the specified <paramref name="obj"/> is another <see cref="CombatResults"/> instance,
        /// <b>Equals</b> invokes the strongly-typed <see cref="Equals(CombatResults)"/> overload to
        /// test the two instances for value equality.</remarks>

        public override bool Equals(object obj) {

            if (obj == null || !(obj is CombatResults))
                return false;

            return Equals((CombatResults) obj);
        }

        #endregion
        #region Equals(CombatResults)

        /// <summary>
        /// Determines whether this instance and a specified <see cref="CombatResults"/> instance
        /// have the same value.</summary>
        /// <param name="results">
        /// Another <see cref="CombatResults"/> instance to compare to this instance.</param>
        /// <returns>
        /// <c>true</c> if the value of <paramref name="results"/> is the same as this instance;
        /// otherwise, <c>false</c>.</returns>
        /// <remarks>
        /// <b>Equals</b> compares the values of the <see cref="AttackerStrength"/>, <see
        /// cref="AttackerLosses"/>, <see cref="DefenderStrength"/>, and <see
        /// cref="DefenderLosses"/> properties of the two <see cref="CombatResults"/> instances to
        /// test for value equality.</remarks>

        public bool Equals(CombatResults results) {

            return (AttackerStrength == results.AttackerStrength &&
                AttackerLosses == results.AttackerLosses &&
                DefenderStrength == results.DefenderStrength &&
                DefenderLosses == results.DefenderLosses);
        }

        #endregion
        #region Equals(CombatResults, CombatResults)

        /// <summary>
        /// Determines whether two specified <see cref="CombatResults"/> instances have the same
        /// value.</summary>
        /// <param name="x">
        /// The first <see cref="CombatResults"/> instance to compare.</param>
        /// <param name="y">
        /// The second <see cref="CombatResults"/> instance to compare.</param>
        /// <returns>
        /// <c>true</c> if the value of <paramref name="x"/> is the same as the value of <paramref
        /// name="y"/>; otherwise, <c>false</c>.</returns>
        /// <remarks>
        /// <b>Equals</b> invokes the non-static <see cref="Equals(CombatResults)"/> overload to
        /// test the two <see cref="CombatResults"/> instances for value equality.</remarks>

        public static bool Equals(CombatResults x, CombatResults y) {
            return x.Equals(y);
        }

        #endregion
        #endregion
    }
}
