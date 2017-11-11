using System;
using System.Diagnostics;

using Tektosyne;

namespace Hexkit.World {

    /// <summary>
    /// Provides a weak reference to a <see cref="Faction"/>.</summary>
    /// <remarks><para>
    /// <b>FactionReference</b> encapsulates the unique identifier of a <see cref="Faction"/>
    /// object, together with a weak reference to the object itself, and a separate copy of its
    /// display name that remains available after the object has been garbage-collected.
    /// </para><para>
    /// Use <b>FactionReference</b> instances rather than direct references to identify factions
    /// across different deep copies of an underlying <see cref="WorldState"/>. Such copies are
    /// created by computer player algorithms and by interactive game replays. Weak references allow
    /// the garbage collector to delete <b>Faction</b> instances that belong to obsolete
    /// <b>WorldState</b> copies.</para></remarks>

    public struct FactionReference {
        #region FactionReference(Faction)

        /// <overloads>
        /// Initializes a new instance of the <see cref="FactionReference"/> class.</overloads>
        /// <summary>
        /// Initializes a new instance of the <see cref="FactionReference"/> class with the
        /// specified <see cref="Faction"/>.</summary>
        /// <param name="faction">
        /// The initial value for the <see cref="Value"/> property.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="faction"/> is a null reference.</exception>
        /// <remarks>
        /// The <see cref="Id"/> and <see cref="Name"/> properties are set to the <see
        /// cref="Faction.Id"/> and <see cref="Faction.Name"/> of the specified <paramref
        /// name="faction"/>, respectively.</remarks>

        public FactionReference(Faction faction) {
            if (faction == null)
                ThrowHelper.ThrowArgumentNullException("faction");

            this._id = faction.Id;
            this._name = faction.Name;
            this._reference = new WeakReference(faction, false);
        }

        #endregion
        #region FactionReference(String)

        /// <summary>
        /// Initializes a new instance of the <see cref="FactionReference"/> class with the
        /// specified identifier.</summary>
        /// <param name="id">
        /// The initial value for the <see cref="Id"/> property.</param>
        /// <exception cref="ArgumentNullOrEmptyException">
        /// <paramref name="id"/> is a null reference or an empty string.</exception>
        /// <remarks>
        /// The <see cref="Name"/> and <see cref="Value"/> properties are set to null references.
        /// </remarks>

        public FactionReference(string id) {
            if (String.IsNullOrEmpty(id))
                ThrowHelper.ThrowArgumentNullOrEmptyException("id");

            this._id = String.Intern(id);
            this._name = null;
            this._reference = null;
        }

        #endregion
        #region FactionReference(Tag)

        /// <summary>
        /// Initializes a new instance of the <see cref="FactionReference"/> class with invalid
        /// values.</summary>
        /// <param name="tag">
        /// A dummy parameter to identify this constructor.</param>
        /// <remarks>
        /// Please refer to <see cref="Invalid"/> for details.</remarks>

        private FactionReference(Tag tag) {
            this._id = null;
            this._name = Global.Strings.LabelFactionUnknown;
            this._reference = null;
        }

        #endregion
        #region Private Fields

        // dummy enumeration for private constructor
        private enum Tag { Default }

        // property backers
        private readonly string _id;
        private string _name;
        private WeakReference _reference;

        #endregion
        #region Invalid

        /// <summary>
        /// Represents an invalid <see cref="FactionReference"/>.</summary>
        /// <remarks>
        /// <b>Invalid</b> holds a <see cref="FactionReference"/> whose <see cref="Id"/> and <see
        /// cref="Value"/> are null references, and whose <see cref="Name"/> is the localized string
        /// "Invalid Faction".</remarks>

        public static readonly FactionReference Invalid = new FactionReference(Tag.Default);

        #endregion
        #region Id

        /// <summary>
        /// Gets the identifier of the wrapped <see cref="Faction"/>.</summary>
        /// <value>
        /// The <see cref="Faction.Id"/> string of the wrapped <see cref="Value"/>. The default is a
        /// null reference.</value>
        /// <remarks>
        /// This property never changes once the instance has been constructed.</remarks>

        public string Id {
            [DebuggerStepThrough]
            get { return this._id; }
        }

        #endregion
        #region Name

        /// <summary>
        /// Gets the display name of the wrapped <see cref="Faction"/>.</summary>
        /// <value>
        /// The <see cref="Faction.Name"/> string of the wrapped <see cref="Value"/>. The default is
        /// the value of the <see cref="Id"/> property.</value>
        /// <remarks><para>
        /// <b>Name</b> is set automatically by setting the <see cref="Value"/> property.
        /// </para><para>
        /// This property is backed by a separate <see cref="String"/> to ensure that the display
        /// name remains available even after the wrapped <see cref="Value"/> has been
        /// garbage-collected.</para></remarks>

        public string Name {
            [DebuggerStepThrough]
            get { return StringUtility.Validate(this._name, this._id); }
        }

        #endregion
        #region Value

        /// <summary>
        /// Gets or sets the wrapped <see cref="Faction"/>.</summary>
        /// <value>
        /// The <see cref="Faction"/> wrapped by this <see cref="FactionReference"/> instance. The
        /// default is a null reference.</value>
        /// <exception cref="ArgumentException">
        /// The property is set, and <see cref="Id"/> does not equal the <see cref="Faction.Id"/>
        /// property of the new value.</exception>
        /// <exception cref="ArgumentNullException">
        /// The property is set to a null reference.</exception>
        /// <remarks><para>
        /// <b>Value</b> returns a null reference if the wrapped <see cref="Faction"/> has been
        /// garbage-collected.
        /// </para><para>
        /// Setting this property also sets the <see cref="Name"/> property to the <see
        /// cref="Faction.Name"/> string of the new value.</para></remarks>

        public Faction Value {
            [DebuggerStepThrough]
            get {
                if (this._reference == null)
                    return null;

                // Target is null after garbage collection
                return (Faction) this._reference.Target;
            }
            set {
                if (value == null)
                    ThrowHelper.ThrowArgumentNullException("value");

                if (value.Id != Id)
                    ThrowHelper.ThrowArgumentExceptionWithFormat(
                        "value", Tektosyne.Strings.ArgumentPropertyInvalid, "Id");

                this._name = value.Name;
                this._reference = new WeakReference(value, false);
            }
        }

        #endregion
        #region ToString

        /// <summary>
        /// Returns a <see cref="String"/> that represents the <see cref="FactionReference"/>
        /// object.</summary>
        /// <returns>
        /// The value of the <see cref="Name"/> property, if it is not a null reference or an empty
        /// string; otherwise, the literal string "(invalid)".</returns>

        public override string ToString() {
            return StringUtility.Validate(Name, "(invalid)");
        }

        #endregion
    }
}
