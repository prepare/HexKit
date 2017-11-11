using System;
using System.Collections.Generic;
using System.Diagnostics;

using Tektosyne;
using Tektosyne.Collections;
using Hexkit.Scenario;

namespace Hexkit.World {
    #region Type Aliases

    using EntityClassList = KeyedList<String, EntityClass>;
    using EntityList = KeyedList<String, Entity>;

    #endregion

    /// <summary>
    /// Provides a weak reference to an <see cref="Entity"/>.</summary>
    /// <remarks><para>
    /// <b>EntityReference</b> encapsulates the unique identifier of an <see cref="Entity"/> object,
    /// together with a weak reference to the object itself, and a separate copy of its display name
    /// that remains available after the object has been garbage-collected.
    /// </para><para>
    /// Use <b>EntityReference</b> instances rather than direct references to identify entities
    /// across different deep copies of an underlying <see cref="WorldState"/>. Such copies are
    /// created by computer player algorithms and by interactive game replays. Weak references allow
    /// the garbage collector to delete <b>Entity</b> instances that belong to obsolete
    /// <b>WorldState</b> copies.</para></remarks>

    public struct EntityReference {
        #region EntityReference(Entity)

        /// <overloads>
        /// Initializes a new instance of the <see cref="EntityReference"/> class.</overloads>
        /// <summary>
        /// Initializes a new instance of the <see cref="EntityReference"/> class with the specified
        /// <see cref="Entity"/>.</summary>
        /// <param name="entity">
        /// The initial value for the <see cref="Value"/> property.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="entity"/> is a null reference.</exception>
        /// <remarks>
        /// The <see cref="Id"/> and <see cref="Name"/> properties are set to the <see
        /// cref="Entity.Id"/> and <see cref="Entity.Name"/> of the specified <paramref
        /// name="entity"/>, respectively.</remarks>

        public EntityReference(Entity entity) {
            if (entity == null)
                ThrowHelper.ThrowArgumentNullException("entity");

            this._id = entity.Id;
            this._name = entity.Name;
            this._reference = new WeakReference(entity, false);
        }

        #endregion
        #region EntityReference(String)

        /// <summary>
        /// Initializes a new instance of the <see cref="EntityReference"/> class with the specified
        /// identifier.</summary>
        /// <param name="id">
        /// The initial value for the <see cref="Id"/> property.</param>
        /// <exception cref="ArgumentNullOrEmptyException">
        /// <paramref name="id"/> is a null reference or an empty string.</exception>
        /// <remarks>
        /// The <see cref="Name"/> and <see cref="Value"/> properties are set to null references.
        /// </remarks>

        public EntityReference(string id) {
            if (String.IsNullOrEmpty(id))
                ThrowHelper.ThrowArgumentNullOrEmptyException("id");

            this._id = String.Intern(id);
            this._name = null;
            this._reference = null;
        }

        #endregion
        #region EntityReference(Tag)

        /// <summary>
        /// Initializes a new instance of the <see cref="EntityReference"/> class with invalid
        /// values.</summary>
        /// <param name="tag">
        /// A dummy parameter to identify this constructor.</param>
        /// <remarks>
        /// Please refer to <see cref="Invalid"/> for details.</remarks>

        private EntityReference(Tag tag) {
            this._id = null;
            this._name = Global.Strings.LabelEntityUnknown;
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
        /// Represents an invalid <see cref="EntityReference"/>.</summary>
        /// <remarks>
        /// <b>Invalid</b> holds an <see cref="EntityReference"/> whose <see cref="Id"/> and <see
        /// cref="Value"/> are null references, and whose <see cref="Name"/> is the localized string
        /// "Unknown Entity".</remarks>

        public static readonly EntityReference Invalid = new EntityReference(Tag.Default);

        #endregion
        #region Id

        /// <summary>
        /// Gets the identifier of the wrapped <see cref="Entity"/>.</summary>
        /// <value>
        /// The <see cref="Entity.Id"/> string of the wrapped <see cref="Value"/>. The default is a
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
        /// Gets the display name of the wrapped <see cref="Entity"/>.</summary>
        /// <value>
        /// The <see cref="Entity.Name"/> string of the wrapped <see cref="Value"/>. The default is
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
        /// Gets or sets the wrapped <see cref="Entity"/>.</summary>
        /// <value>
        /// The <see cref="Entity"/> wrapped by the <see cref="EntityReference"/>. The default is a
        /// null reference.</value>
        /// <exception cref="ArgumentException">
        /// The property is set, and <see cref="Id"/> does not equal the <see cref="Entity.Id"/>
        /// property of the new value.</exception>
        /// <exception cref="ArgumentNullException">
        /// The property is set to a null reference.</exception>
        /// <remarks><para>
        /// <b>Value</b> returns a null reference if the wrapped <see cref="Entity"/> has been
        /// garbage-collected.
        /// </para><para>
        /// Setting this property also sets the <see cref="Name"/> property to the <see
        /// cref="Entity.Name"/> string of the new value.</para></remarks>

        public Entity Value {
            [DebuggerStepThrough]
            get {
                if (this._reference == null)
                    return null;

                // Target is null after garbage collection
                return (Entity) this._reference.Target;
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
        #region Public Methods
        #region CreateArray(IEntityList)

        /// <overloads>
        /// Creates an array of <see cref="EntityReference"/> objects.</overloads>
        /// <summary>
        /// Creates an <see cref="EntityReference"/> array from the specified <see cref="Entity"/>
        /// collection.</summary>
        /// <param name="entities">
        /// An <see cref="IList{T}"/> containing the <see cref="Entity"/> objects to be wrapped by
        /// the new <see cref="EntityReference"/> instances. This argument may be a null reference.
        /// </param>
        /// <returns>
        /// An <see cref="Array"/> containing the <see cref="EntityReference"/> instances created
        /// from the specified <paramref name="entities"/>.</returns>
        /// <remarks><para>
        /// <b>CreateArray</b> returns a null reference if the specified <paramref name="entities"/>
        /// collection is a null reference.
        /// </para><para>
        /// Otherwise, every <see cref="EntityReference"/> in the returned array is created from the
        /// <paramref name="entities"/> element at the same index position using the appropriate
        /// constructor.</para></remarks>

        public static EntityReference[] CreateArray(IList<Entity> entities) {
            if (entities == null) return null;

            EntityReference[] array = new EntityReference[entities.Count];

            for (int i = 0; i < entities.Count; i++)
                array[i] = new EntityReference(entities[i]);

            return array;
        }

        #endregion
        #region CreateArray(IStringList)

        /// <summary>
        /// Creates an <see cref="EntityReference"/> array from the specified identifier collection.
        /// </summary>
        /// <param name="identifiers">
        /// An <see cref="IList{T}"/> containing the <see cref="Id"/> strings for the new <see
        /// cref="EntityReference"/> instances. This argument may be a null reference.</param>
        /// <returns>
        /// An <see cref="Array"/> containing the <see cref="EntityReference"/> instances created
        /// from the specified <paramref name="identifiers"/>.</returns>
        /// <remarks><para>
        /// <b>CreateArray</b> returns a null reference if the specified <paramref
        /// name="identifiers"/> collection is a null reference.
        /// </para><para>
        /// Otherwise, every <see cref="EntityReference"/> in the returned array is created from the
        /// <paramref name="identifiers"/> element at the same index position using the appropriate
        /// constructor.</para></remarks>

        public static EntityReference[] CreateArray(IList<String> identifiers) {
            if (identifiers == null) return null;

            EntityReference[] array = new EntityReference[identifiers.Count];

            for (int i = 0; i < identifiers.Count; i++)
                array[i] = new EntityReference(identifiers[i]);

            return array;
        }

        #endregion
        #region GetEntities

        /// <summary>
        /// Returns a list of all entities in the specified <see cref="EntityReference"/> array.
        /// </summary>
        /// <param name="array">
        /// An <see cref="Array"/> of <see cref="EntityReference"/> instances whose <see
        /// cref="Value"/> components to extract. This argument may be a null reference.</param>
        /// <returns>
        /// A <see cref="EntityList"/> containing all valid <see cref="Value"/> components of any
        /// element in the specified <paramref name="array"/>.</returns>
        /// <remarks><para>
        /// <b>GetEntities</b> returns a null reference if the specified <paramref name="array"/> is
        /// a null reference.
        /// </para><para>
        /// Otherwise, every <see cref="Value"/> component found in the specified <paramref
        /// name="array"/> that is not a null reference is added to the returned collection.
        /// <b>GetEntities</b> returns an empty collection if all <b>Value</b> components are null
        /// references.</para></remarks>

        public static EntityList GetEntities(EntityReference[] array) {
            if (array == null) return null;
            var entities = new EntityList(array.Length);

            foreach (EntityReference entity in array)
                if (entity.Value != null)
                    entities.Add(entity.Value);

            return entities;
        }

        #endregion
        #region GetEntityClasses

        /// <summary>
        /// Returns a list of the entity classes of all entities in the specified <see
        /// cref="EntityReference"/> array.</summary>
        /// <param name="array">
        /// An <see cref="Array"/> of <see cref="EntityReference"/> instances whose <see
        /// cref="Value"/> components to process. This argument may be a null reference.</param>
        /// <returns>
        /// A <see cref="EntityClassList"/> containing the <see cref="Entity.EntityClass"/> values
        /// of all valid <see cref="Value"/> components of any element in the specified <paramref
        /// name="array"/>.</returns>
        /// <remarks><para>
        /// <b>GetEntityClasses</b> returns a null reference if the specified <paramref
        /// name="array"/> is a null reference.
        /// </para><para>
        /// Otherwise, the <see cref="Entity.EntityClass"/> value of every <see cref="Value"/>
        /// component found in the specified <paramref name="array"/> that is not a null reference
        /// is added to the returned collection.
        /// </para><para>
        /// <b>GetEntityClasses</b> returns an empty collection if all <b>Value</b> components are
        /// null references.</para></remarks>

        public static EntityClassList GetEntityClasses(EntityReference[] array) {
            if (array == null) return null;
            var entityClasses = new EntityClassList(array.Length);

            foreach (EntityReference entity in array)
                if (entity.Value != null)
                    entityClasses.Add(entity.Value.EntityClass);

            return entityClasses;
        }

        #endregion
        #region GetIdentifiers

        /// <summary>
        /// Returns a list of all identifiers in the specified <see cref="EntityReference"/> array.
        /// </summary>
        /// <param name="array">
        /// An <see cref="Array"/> of <see cref="EntityReference"/> instances whose <see cref="Id"/>
        /// components to extract. This argument may be a null reference.</param>
        /// <returns>
        /// An <see cref="Array"/> containing the <see cref="Id"/> components of all elements in the
        /// specified <paramref name="array"/>.</returns>
        /// <remarks><para>
        /// <b>GetIdentifiers</b> returns a null reference if the specified <paramref name="array"/>
        /// is a null reference.
        /// </para><para>
        /// Otherwise, every <see cref="Id"/> component found in the specified <paramref
        /// name="array"/> is added to the returned <see cref="Array"/>, including any null
        /// references.</para></remarks>

        public static string[] GetIdentifiers(EntityReference[] array) {
            if (array == null) return null;

            int count = array.Length;
            string[] identifiers = new string[count];

            for (int i = 0; i < count; i++)
                identifiers[i] = array[i].Id;

            return identifiers;
        }

        #endregion
        #region GetNames

        /// <summary>
        /// Returns a list of all names in the specified <see cref="EntityReference"/> array.
        /// </summary>
        /// <param name="array">
        /// An <see cref="Array"/> of <see cref="EntityReference"/> instances whose <see
        /// cref="Name"/> components to extract. This argument may be a null reference.</param>
        /// <returns>
        /// An <see cref="Array"/> containing the <see cref="Name"/> components of all elements in
        /// the specified <paramref name="array"/>.</returns>
        /// <remarks><para>
        /// <b>GetNames</b> returns a null reference if the specified <paramref name="array"/> is a
        /// null reference.
        /// </para><para>
        /// Otherwise, every <see cref="Name"/> component found in the specified <paramref
        /// name="array"/> is added to the returned <see cref="Array"/>, including any null
        /// references.</para></remarks>

        public static string[] GetNames(EntityReference[] array) {
            if (array == null) return null;

            int count = array.Length;
            string[] names = new string[count];

            for (int i = 0; i < count; i++)
                names[i] = array[i].Name;

            return names;
        }

        #endregion
        #region ToString

        /// <summary>
        /// Returns a <see cref="String"/> that represents the <see cref="EntityReference"/>.
        /// </summary>
        /// <returns>
        /// The value of the <see cref="Name"/> property, if it is not a null reference or an empty
        /// string; otherwise, the literal string "(invalid)".</returns>

        public override string ToString() {
            return StringUtility.Validate(Name, "(invalid)");
        }

        #endregion
        #endregion
    }
}
