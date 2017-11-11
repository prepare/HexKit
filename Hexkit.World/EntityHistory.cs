using System;
using System.Diagnostics;

using Tektosyne;
using Tektosyne.Collections;

using Hexkit.Global;
using Hexkit.Scenario;

namespace Hexkit.World {

    /// <summary>
    /// Records the history of an <see cref="Entity"/>.</summary>
    /// <remarks><para>
    /// <b>EntityHistory</b> associates the <see cref="Entity.Id"/> string of an <see
    /// cref="Entity"/> with its <see cref="Entity.EntityClass"/> and with the following important
    /// events in its history:
    /// </para><list type="bullet"><item>
    /// The <see cref="EntityEventType.Create"/> event specifies the turn during which the <see
    /// cref="Entity"/> was created.
    /// </item><item>
    /// The <see cref="EntityEventType.Delete"/> event specifies the turn during which the <see
    /// cref="Entity"/> was deleted, if ever; and the <see cref="Faction"/> that executed the
    /// command which caused the deletion.
    /// </item><item>
    /// Zero or more <see cref="EntityEventType.SetName"/> events specify each turn during which the
    /// <see cref="Entity"/> changed its <see cref="Entity.InstanceName"/>, and the new name itself.
    /// </item></list><para>
    /// The <b>EntityHistory</b> persists even after the associated <see cref="Entity"/> has been
    /// deleted from the current <see cref="WorldState"/>. As such, it is useful for debugging and
    /// for statistics display.</para></remarks>

    public class EntityHistory: ICloneable, IKeyedValue<String> {
        #region EntityHistory(EntityHistory)

        /// <summary>
        /// Initializes a new instance of the <see cref="EntityHistory"/> class that is a shallow
        /// copy of the specified instance.</summary>
        /// <param name="history">
        /// The <see cref="EntityHistory"/> object whose property values should be copied to the new
        /// instance.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="history"/> is a null reference.</exception>
        /// <remarks>
        /// This "copy constructor" does not need to perform a deep copy as the properties of the
        /// <see cref="EntityHistory"/> class either contain immutable objects, or are to be treated
        /// as immutable in the case of the <see cref="Events"/> array.</remarks>

        private EntityHistory(EntityHistory history) {
            if (history == null)
                ThrowHelper.ThrowArgumentNullException("history");

            this._entityClass = history._entityClass;
            this._id = history._id;
            Events = history.Events;
        }

        #endregion
        #region EntityHistory(WorldState, Entity)

        /// <overloads>
        /// Initializes a new instance of the <see cref="EntityHistory"/> class.</overloads>
        /// <summary>
        /// Initializes a new instance of the <see cref="EntityHistory"/> class with the specified
        /// <see cref="WorldState"/> and <see cref="Entity"/>.</summary>
        /// <param name="worldState">
        /// The <see cref="WorldState"/> at the time when the specified <paramref name="entity"/>
        /// was created.</param>
        /// <param name="entity">
        /// The <see cref="Entity"/> providing the initial values for the <see cref="EntityClass"/>
        /// and <see cref="Id"/> properties.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="worldState"/> or <paramref name="entity"/> is a null reference.
        /// </exception>
        /// <remarks>
        /// The <see cref="Events"/> property is initialized to an <see cref="Array"/> containing a
        /// single <see cref="EntityEventType.Create"/> event with the <see
        /// cref="WorldState.CurrentTurn"/> and <see cref="WorldState.ActiveFaction"/> identifier of
        /// the specified <paramref name="worldState"/>.</remarks>

        public EntityHistory(WorldState worldState, Entity entity) {

            if (worldState == null)
                ThrowHelper.ThrowArgumentNullException("worldState");
            if (entity == null)
                ThrowHelper.ThrowArgumentNullException("entity");

            this._entityClass = entity.EntityClass;
            this._id = entity.Id;

            Events = new EntityEvent[] {
                new EntityEvent(EntityEventType.Create,
                    worldState.CurrentTurn, worldState.ActiveFaction.Id)
            };
        }

        #endregion
        #region Private Fields

        // property backers
        private readonly EntityClass _entityClass;
        private readonly string _id;

        #endregion
        #region Public Properties
        #region EntityClass

        /// <summary>
        /// Gets the scenario class of the associated <see cref="Entity"/>.</summary>
        /// <value>
        /// The underlying <see cref="Entity.EntityClass"/> of the <see cref="Entity"/> whose
        /// history is recorded by the <see cref="EntityHistory"/>.</value>
        /// <remarks>
        /// <b>EntityClass</b> never returns a null reference. This property never changes once the
        /// object has been constructed.</remarks>

        public EntityClass EntityClass {
            [DebuggerStepThrough]
            get { return this._entityClass; }
        }

        #endregion
        #region Events

        /// <summary>
        /// Gets a list of all events that occurred to the <see cref="Entity"/>.</summary>
        /// <value>
        /// An <see cref="Array"/> containing all <see cref="EntityEvent"/> objects that affected
        /// the associated <see cref="Entity"/>. The default is an array containing a single <see
        /// cref="EntityEventType.Create"/> event.</value>
        /// <remarks><para>
        /// Call <see cref="Delete"/> or <see cref="SetName"/> to add a new element of the
        /// corresponding <see cref="EntityEventType"/> to the <b>Events</b> array.
        /// </para><para>
        /// The <see cref="EntityHistory"/> class never changes or removes existing <b>Events</b>,
        /// and neither should client code.</para></remarks>

        public EntityEvent[] Events { get; private set; }

        #endregion
        #region Id

        /// <summary>
        /// Gets the identifier of the associated <see cref="Entity"/>.</summary>
        /// <value>
        /// The <see cref="Entity.Id"/> string of the <see cref="Entity"/> whose history is recorded
        /// by the <see cref="EntityHistory"/>.</value>
        /// <remarks>
        /// <b>Id</b> never returns a null reference or an empty string. This property never changes
        /// once the object has been constructed.</remarks>

        public string Id {
            [DebuggerStepThrough]
            get { return this._id; }
        }

        #endregion
        #region IsDeleted

        /// <summary>
        /// Gets a value indicating whether the associated <see cref="Entity"/> has been deleted.
        /// </summary>
        /// <value>
        /// <c>true</c> if the <see cref="Entity"/> whose history is recorded by the <see
        /// cref="EntityHistory"/> has been deleted; otherwise, <c>false</c>.</value>
        /// <remarks>
        /// <b>IsDeleted</b> returns <c>true</c> exactly if the <see cref="EntityEvent.EventType"/>
        /// of the last <see cref="Events"/> element equals <see cref="EntityEventType.Delete"/>.
        /// Once this has happened, no new <see cref="Events"/> may be added.</remarks>

        public bool IsDeleted {
            get {
                int count = Events.Length - 1;
                return (count >= 0 && Events[count].EventType == EntityEventType.Delete);
            }
        }

        #endregion
        #region LastName

        /// <summary>
        /// Gets the most recent display name of the associated <see cref="Entity"/>.</summary>
        /// <value>
        /// The most recent <see cref="Entity.Name"/> of the <see cref="Entity"/> whose history is
        /// recorded by the <see cref="EntityHistory"/>.</value>
        /// <remarks><para>
        /// <b>LastName</b> returns the <see cref="EntityEvent.Value"/> of the last <see
        /// cref="EntityEventType.SetName"/> event found in the <see cref="Events"/> collection.
        /// </para><para>
        /// <b>LastName</b> returns the <see cref="Hexkit.Scenario.EntityClass.Name"/> of the
        /// underlying <see cref="EntityClass"/> if no such event was found, or if its <see
        /// cref="EntityEvent.Value"/> field contains a null reference or an empty string.
        /// </para></remarks>

        public string LastName {
            get {
                for (int i = Events.Length - 1; i >= 0; i--)
                    if (Events[i].EventType == EntityEventType.SetName)
                        return StringUtility.Validate((string) Events[i].Value, EntityClass.Name);

                return EntityClass.Name;
            }
        }

        #endregion
        #region LastTurn

        /// <summary>
        /// Gets the highest turn index in the <see cref="Events"/> collection.</summary>
        /// <value>
        /// The <see cref="EntityEvent.Turn"/> value of the last element in the <see cref="Events"/>
        /// collection, or -1 if the collection is empty.</value>

        public int LastTurn {
            get {
                int count = Events.Length - 1;
                return (count >= 0 ? Events[count].Turn : -1);
            }
        }

        #endregion
        #endregion
        #region Private Methods
        #region AddEvent

        /// <summary>
        /// Adds the specified <see cref="EntityEvent"/> to the <see cref="Events"/> collection.
        /// </summary>
        /// <param name="entityEvent">
        /// The <see cref="EntityEvent"/> to add to the <see cref="Events"/> collection.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="entityEvent"/> is a null reference.</exception>
        /// <exception cref="PropertyValueException">
        /// <see cref="IsDeleted"/> is already <c>true</c>.</exception>

        private void AddEvent(EntityEvent entityEvent) {
            if (entityEvent == null)
                ThrowHelper.ThrowArgumentNullException("entityEvent");
            if (IsDeleted)
                ThrowHelper.ThrowPropertyValueException("IsDeleted", Tektosyne.Strings.PropertyTrue);

            // copy elements to new and bigger array
            int count = Events.Length;
            EntityEvent[] array = new EntityEvent[count + 1];
            Events.CopyTo(array, 0);

            // add event to end of new array
            array[count] = entityEvent;
            Events = array;
        }

        #endregion
        #endregion
        #region Internal Methods
        #region Delete

        /// <summary>
        /// Adds a <see cref="EntityEventType.Delete"/> event to the <see cref="Events"/> collection.
        /// </summary>
        /// <param name="worldState">
        /// The current <see cref="WorldState"/> at the time of the event.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="worldState"/> is a null reference.</exception>
        /// <exception cref="PropertyValueException">
        /// <see cref="IsDeleted"/> is already <c>true</c>.</exception>
        /// <remarks>
        /// <b>Delete</b> creates a new <see cref="EntityEventType.Delete"/> event with the <see
        /// cref="WorldState.CurrentTurn"/> and <see cref="WorldState.ActiveFaction"/> identifier
        /// of the specified <paramref name="worldState"/>.</remarks>

        internal void Delete(WorldState worldState) {
            if (worldState == null)
                ThrowHelper.ThrowArgumentNullException("worldState");

            AddEvent(new EntityEvent(EntityEventType.Delete,
                worldState.CurrentTurn, worldState.ActiveFaction.Id));
        }

        #endregion
        #region SetClass

        /// <summary>
        /// Adds a <see cref="EntityEventType.SetClass"/> event to the <see cref="Events"/>
        /// collection.</summary>
        /// <param name="worldState">
        /// The current <see cref="WorldState"/> at the time of the event.</param>
        /// <param name="id">
        /// The identifier of the new <see cref="Entity.EntityClass"/> of the associated <see
        /// cref="Entity"/>.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="worldState"/> is a null reference.</exception>
        /// <exception cref="ArgumentNullOrEmptyException">
        /// <paramref name="id"/> is and empty string or a null reference.</exception>
        /// <exception cref="PropertyValueException">
        /// <see cref="IsDeleted"/> is already <c>true</c>.</exception>
        /// <remarks>
        /// <b>SetClass</b> creates a new <see cref="EntityEventType.SetClass"/> event with the <see
        /// cref="WorldState.CurrentTurn"/> of the specified <paramref name="worldState"/> and with 
        /// the specified <paramref name="id"/>.</remarks>

        internal void SetClass(WorldState worldState, string id) {
            if (worldState == null)
                ThrowHelper.ThrowArgumentNullException("worldState");
            if (String.IsNullOrEmpty(id))
                ThrowHelper.ThrowArgumentNullOrEmptyException("id");

            AddEvent(new EntityEvent(EntityEventType.SetClass, worldState.CurrentTurn, id));
        }

        #endregion
        #region SetName

        /// <summary>
        /// Adds a <see cref="EntityEventType.SetName"/> event to the <see cref="Events"/>
        /// collection.</summary>
        /// <param name="worldState">
        /// The current <see cref="WorldState"/> at the time of the event.</param>
        /// <param name="name">
        /// The new <see cref="Entity.InstanceName"/> of the associated <see cref="Entity"/>, which
        /// may be a null reference.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="worldState"/> is a null reference.</exception>
        /// <exception cref="PropertyValueException">
        /// <see cref="IsDeleted"/> is already <c>true</c>.</exception>
        /// <remarks>
        /// <b>SetName</b> creates a new <see cref="EntityEventType.SetName"/> event with the <see
        /// cref="WorldState.CurrentTurn"/> of the specified <paramref name="worldState"/> and with 
        /// the specified <paramref name="name"/>, if any.</remarks>

        internal void SetName(WorldState worldState, string name) {
            if (worldState == null)
                ThrowHelper.ThrowArgumentNullException("worldState");

            AddEvent(new EntityEvent(EntityEventType.SetName, worldState.CurrentTurn, name));
        }

        #endregion
        #endregion
        #region ICloneable Members

        /// <summary>
        /// Creates a new <see cref="EntityHistory"/> object that is a shallow copy of the current
        /// instance.</summary>
        /// <returns>
        /// A new <see cref="EntityHistory"/> object that is a shallow copy of the current instance.
        /// </returns>
        /// <remarks>
        /// <b>Clone</b> invokes the "copy constructor", <see cref="EntityHistory(EntityHistory)"/>,
        /// to create a shallow copy of the current instance.</remarks>

        public object Clone() {
            return new EntityHistory(this);
        }

        #endregion
        #region IKeyedValue Members

        /// <summary>
        /// Gets the identifier of the associated <see cref="Entity"/>.</summary>
        /// <value>
        /// The value of the <see cref="Id"/> property.</value>

        string IKeyedValue<String>.Key {
            [DebuggerStepThrough]
            get { return Id; }
        }

        #endregion
        #region Class EntityEvent

        /// <summary>
        /// Describes an event that occurred in the course of an <see cref="EntityHistory"/>.
        /// </summary>
        /// <remarks>
        /// <b>EntityEvent</b> is a simple data container that associates the index of a game turn
        /// with an <see cref="EntityEventType"/> indicating some event that affected the <see
        /// cref="Entity"/> during that turn, and optionally another <see cref="Object"/> that was
        /// also involved.</remarks>

        public class EntityEvent {
            #region EntityEvent(EntityEventType, Int32)

            /// <overloads>
            /// Initializes a new instance of the <see cref="EntityEvent"/> class.</overloads>
            /// <summary>
            /// Initializes a new instance of the <see cref="EntityEvent"/> class with the
            /// specified event type and turn index.</summary>
            /// <param name="type">
            /// The initial value for the <see cref="EventType"/> field.</param>
            /// <param name="turn">
            /// The initial value for the <see cref="Turn"/> field.</param>
            /// <exception cref="ArgumentOutOfRangeException">
            /// <paramref name="turn"/> is less than zero.</exception>
            /// <remarks>
            /// The <see cref="Value"/> field is set to a null reference.</remarks>

            internal EntityEvent(EntityEventType type, int turn): this(type, turn, null) { }

            #endregion
            #region EntityEvent(EntityEventType, Int32, Object)

            /// <summary>
            /// Initializes a new instance of the <see cref="EntityEvent"/> class with the
            /// specified event type, turn index, and associated <see cref="Object"/>.</summary>
            /// <param name="type">
            /// The initial value for the <see cref="EventType"/> field.</param>
            /// <param name="turn">
            /// The initial value for the <see cref="Turn"/> field.</param>
            /// <param name="value">
            /// The initial value for the <see cref="Value"/> field. This argument may be a null
            /// reference.</param>
            /// <exception cref="ArgumentOutOfRangeException">
            /// <paramref name="turn"/> is less than zero.</exception>

            internal EntityEvent(EntityEventType type, int turn, object value) {
                if (turn < 0)
                    ThrowHelper.ThrowArgumentOutOfRangeException(
                        "turn", turn, Tektosyne.Strings.ArgumentNegative);

                this._eventType = type;
                this._turn = turn;
                this._value = value;
            }

            #endregion
            #region Private Fields

            // property backers
            private readonly EntityEventType _eventType;
            private readonly int _turn;
            private readonly object _value;

            #endregion
            #region EventType

            /// <summary>
            /// The event type of the <see cref="EntityEvent"/>.</summary>
            /// <remarks>
            /// The value of the <see cref="EventType"/> field determines the contents of the
            /// <see cref="Value"/> field. Please refer to <see cref="EntityEventType"/> for
            /// details.</remarks>

            public EntityEventType EventType {
                [DebuggerStepThrough]
                get { return this._eventType; }
            }

            #endregion
            #region Turn

            /// <summary>
            /// The zero-based index of the game turn during which the <see cref="EntityEvent"/>
            /// occurred.</summary>
            /// <remarks>
            /// <b>Turn</b> is never less than zero.</remarks>

            public int Turn {
                [DebuggerStepThrough]
                get { return this._turn; }
            }

            #endregion
            #region TurnText

            /// <summary>
            /// Gets a <see cref="String"/> that represents the associated <see cref="Turn"/>.
            /// </summary>
            /// <value>
            /// The string representation of the value of <see cref="Turn"/> plus one.</value>
            /// <remarks>
            /// <b>TurnText</b> converts <see cref="Turn"/> into a one-based index for display to
            /// the user.</remarks>

            public string TurnText {
                get { return (Turn + 1).ToString(ApplicationInfo.Culture); }
            }

            #endregion
            #region Value

            /// <summary>
            /// The <see cref="Object"/> associated with the <see cref="EntityEvent"/>.
            /// </summary>
            /// <remarks>
            /// <b>Value</b> may contain a null reference if the current <see cref="EventType"/> is
            /// not associated with another <see cref="Object"/>.</remarks>

            public object Value {
                [DebuggerStepThrough]
                get { return this._value; }
            }

            #endregion
            #region ValueText

            /// <summary>
            /// Gets a <see cref="String"/> that represents the associated <see cref="Value"/>.
            /// </summary>
            /// <value>
            /// An em dash (—) if <see cref="Value"/> is a null reference, or its string
            /// representation is a null reference or an empty string; otherwise, the
            /// culture-invariant string representation of <see cref="Value"/>.</value>

            public string ValueText {
                get { return StringUtility.Validate(Value, "—"); /* em dash */ }
            }

            #endregion
        }

        #endregion
        #region Enum EntityEventType

        /// <summary>
        /// Specifies the type of a <see cref="EntityEvent"/>.</summary>
        /// <remarks>
        /// <b>EntityEventType</b> specifies the possible values for the <see
        /// cref="EntityEvent.EventType"/> field of the <see cref="EntityEvent"/> class.</remarks>

        public enum EntityEventType: byte {
            #region Create

            /// <summary>
            /// Specifies that the <see cref="Entity"/> was created.</summary>
            /// <remarks><para>
            /// An <see cref="EntityHistory"/> always contains a single <b>Create</b> event, and it
            /// is always the first element in the <see cref="EntityHistory.Events"/> collection.
            /// </para><para>
            /// The <b>Create</b> event uses the <see cref="EntityEvent.Value"/> field to store the
            /// identifier of the <see cref="WorldState.ActiveFaction"/> at the time of the event.
            /// </para></remarks>

            Create,

            #endregion
            #region Delete

            /// <summary>
            /// Specifies that the <see cref="Entity"/> was deleted.</summary>
            /// <remarks><para>
            /// An <see cref="EntityHistory"/> contains at most a single <b>Delete</b> event, and it
            /// is always the last element in the <see cref="EntityHistory.Events"/> collection.
            /// </para><para>
            /// The <b>Delete</b> event uses the <see cref="EntityEvent.Value"/> field to store the
            /// identifier of the <see cref="WorldState.ActiveFaction"/> at the time of the event.
            /// </para></remarks>

            Delete,

            #endregion
            #region SetClass

            /// <summary>
            /// Specifies that the <see cref="Entity.EntityClass"/> of the <see cref="Entity"/> has
            /// changed.</summary>
            /// <remarks>
            /// The <b>SetClass</b> event uses the <see cref="EntityEvent.Value"/> field to store
            /// the identifier of the new <see cref="Entity.EntityClass"/> of the <see
            /// cref="Entity"/>.</remarks>

            SetClass,

            #endregion
            #region SetName

            /// <summary>
            /// Specifies that the <see cref="Entity.Name"/> of the <see cref="Entity"/> has
            /// changed.</summary>
            /// <remarks>
            /// The <b>SetName</b> event uses the <see cref="EntityEvent.Value"/> field to store the
            /// new <see cref="Entity.InstanceName"/> of the <see cref="Entity"/>, which may be a
            /// null reference.</remarks>

            SetName

            #endregion
        }

        #endregion
    }
}
