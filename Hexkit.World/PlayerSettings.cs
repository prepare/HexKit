using System;
using System.Globalization;

namespace Hexkit.World {

    /// <summary>
    /// Contains player settings that are relevant to rule scripts.</summary>
    /// <remarks>
    /// <b>PlayerSettings</b> contains a subset of the player settings and algorithm options managed
    /// by the <b>Hexkit.Players</b> namespace. A rule script may retrieve the current
    /// <b>PlayerSettings</b> using the <see cref="Faction.PlayerSettings"/> property of the current
    /// <see cref="Faction"/>, and adjust its behavior accordingly.</remarks>

    public struct PlayerSettings: IEquatable<PlayerSettings> {
        #region PlayerSettings(Boolean, Boolean)

        /// <summary>
        /// Initializes a new instance of the <see cref="PlayerSettings"/> class with the specified
        /// property values.</summary>
        /// <param name="isComputer">
        /// The initial value for the <see cref="IsComputer"/> property.</param>
        /// <param name="useScripting">
        /// The initial value for the <see cref="UseScripting"/> property.</param>

        public PlayerSettings(bool isComputer, bool useScripting) {

            this._isComputer = isComputer;
            this._useScripting = useScripting;
        }

        #endregion
        #region Private Fields

        // property backers
        private readonly bool _isComputer, _useScripting;

        #endregion
        #region IsComputer

        /// <summary>
        /// Gets a value indicating whether the <see cref="Faction"/> is controlled by a computer
        /// player.</summary>
        /// <value>
        /// <c>true</c> if the <see cref="Faction"/> is controlled by a computer player;
        /// <c>false</c> if the <see cref="Faction"/> is controlled by a human player.</value>
        /// <remarks>
        /// <b>IsComputer</b> allows rule scripts to modify the behavior of the <see
        /// cref="Faction"/> when it is controlled by a computer player.</remarks>

        public bool IsComputer {
            get { return this._isComputer; }
        }

        #endregion
        #region UseScripting

        /// <summary>
        /// Gets a value indicating whether a computer-controlled <see cref="Faction"/> should use
        /// scripted behavior.</summary>
        /// <value>
        /// <c>true</c> if <see cref="IsComputer"/> is <c>true</c> and the <see cref="Faction"/>
        /// should use all scripted behavior defined by the rule script; otherwise, <c>false</c>.
        /// </value>
        /// <remarks>
        /// <b>UseScripting</b> allows rule scripts to disable all or some of the scripted behavior
        /// normally imposed on a computer-controlled <see cref="Faction"/>. This property always
        /// returns <c>false</c> if <see cref="IsComputer"/> is <c>false</c>.</remarks>

        public bool UseScripting {
            get { return this._useScripting && this._isComputer; }
        }

        #endregion
        #region GetHashCode

        /// <summary>
        /// Returns the hash code for this <see cref="PlayerSettings"/> instance.</summary>
        /// <returns>
        /// An <see cref="Int32"/> hash code.</returns>
        /// <remarks>
        /// <b>GetHashCode</b> returns one if <see cref="IsComputer"/> is <c>true</c>; otherwise,
        /// zero.</remarks>

        public override int GetHashCode() {
            return Convert.ToInt32(IsComputer);
        }

        #endregion
        #region ToString

        /// <summary>
        /// Returns a <see cref="String"/> that represents this <see cref="PlayerSettings"/>
        /// instance.</summary>
        /// <returns>
        /// A <see cref="String"/> containing the values of the <see cref="IsComputer"/> and <see
        /// cref="UseScripting"/> properties.</returns>

        public override string ToString() {
            return String.Format(CultureInfo.InvariantCulture,
                "{{IsComputer={0}, UseScripting={1}}}", IsComputer, UseScripting);
        }

        #endregion
        #region Public Operators
        #region operator==

        /// <summary>
        /// Determines whether two <see cref="PlayerSettings"/> instances have the same value.
        /// </summary>
        /// <param name="x">
        /// The first <see cref="PlayerSettings"/> instance to compare.</param>
        /// <param name="y">
        /// The second <see cref="PlayerSettings"/> instance to compare.</param>
        /// <returns>
        /// <c>true</c> if the value of <paramref name="x"/> is the same as the value of <paramref
        /// name="y"/>; otherwise, <c>false</c>.</returns>
        /// <remarks>
        /// This operator invokes the <see cref="Equals(PlayerSettings)"/> method to test the two
        /// <see cref="PlayerSettings"/> instances for value equality.</remarks>

        public static bool operator ==(PlayerSettings x, PlayerSettings y) {
            return x.Equals(y);
        }

        #endregion
        #region operator!=

        /// <summary>
        /// Determines whether two <see cref="PlayerSettings"/> instances have different values.
        /// </summary>
        /// <param name="x">
        /// The first <see cref="PlayerSettings"/> instance to compare.</param>
        /// <param name="y">
        /// The second <see cref="PlayerSettings"/> instance to compare.</param>
        /// <returns>
        /// <c>true</c> if the value of <paramref name="x"/> is different from the value of
        /// <paramref name="y"/>; otherwise, <c>false</c>.</returns>
        /// <remarks>
        /// This operator invokes the <see cref="Equals(PlayerSettings)"/> method to test the two
        /// <see cref="PlayerSettings"/> instances for value inequality.</remarks>

        public static bool operator !=(PlayerSettings x, PlayerSettings y) {
            return !x.Equals(y);
        }

        #endregion
        #endregion
        #region IEquatable Members
        #region Equals(Object)

        /// <overloads>
        /// Determines whether two <see cref="PlayerSettings"/> instances have the same value.
        /// </overloads>
        /// <summary>
        /// Determines whether this <see cref="PlayerSettings"/> instance and a specified object,
        /// which must be a <see cref="PlayerSettings"/> instance, have the same value.</summary>
        /// <param name="obj">
        /// An <see cref="Object"/> to compare to this <see cref="PlayerSettings"/> instance.
        /// </param>
        /// <returns>
        /// <c>true</c> if <paramref name="obj"/> is another <see cref="PlayerSettings"/> instance
        /// and its value is the same as this instance; otherwise, <c>false</c>.</returns>
        /// <remarks>
        /// If the specified <paramref name="obj"/> is another <see cref="PlayerSettings"/>
        /// instance, <b>Equals</b> invokes the strongly-typed <see cref="Equals(PlayerSettings)"/>
        /// overload to test the two instances for value equality.</remarks>

        public override bool Equals(object obj) {

            if (obj == null || !(obj is PlayerSettings))
                return false;

            return Equals((PlayerSettings) obj);
        }

        #endregion
        #region Equals(PlayerSettings)

        /// <summary>
        /// Determines whether this instance and a specified <see cref="PlayerSettings"/> instance
        /// have the same value.</summary>
        /// <param name="settings">
        /// Another <see cref="PlayerSettings"/> instance to compare to this instance.</param>
        /// <returns>
        /// <c>true</c> if the value of <paramref name="settings"/> is the same as this instance;
        /// otherwise, <c>false</c>.</returns>
        /// <remarks>
        /// <b>Equals</b> compares the values of the <see cref="IsComputer"/> and <see
        /// cref="UseScripting"/> properties of the two <see cref="PlayerSettings"/> instances to
        /// test for value equality.</remarks>

        public bool Equals(PlayerSettings settings) {

            return (IsComputer == settings.IsComputer &&
                UseScripting == settings.UseScripting);
        }

        #endregion
        #region Equals(PlayerSettings, PlayerSettings)

        /// <summary>
        /// Determines whether two specified <see cref="PlayerSettings"/> instances have the same
        /// value.</summary>
        /// <param name="x">
        /// The first <see cref="PlayerSettings"/> instance to compare.</param>
        /// <param name="y">
        /// The second <see cref="PlayerSettings"/> instance to compare.</param>
        /// <returns>
        /// <c>true</c> if the value of <paramref name="x"/> is the same as the value of <paramref
        /// name="y"/>; otherwise, <c>false</c>.</returns>
        /// <remarks>
        /// <b>Equals</b> invokes the non-static <see cref="Equals(PlayerSettings)"/> overload to
        /// test the two <see cref="PlayerSettings"/> instances for value equality.</remarks>

        public static bool Equals(PlayerSettings x, PlayerSettings y) {
            return x.Equals(y);
        }

        #endregion
        #endregion
    }
}
