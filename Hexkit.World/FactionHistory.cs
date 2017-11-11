using System;
using System.Collections.Generic;
using System.Diagnostics;

using Tektosyne;
using Tektosyne.Collections;

using Hexkit.Global;
using Hexkit.Scenario;

namespace Hexkit.World {

    /// <summary>
    /// Records the history of a <see cref="Faction"/>.</summary>
    /// <remarks><para>
    /// <b>FactionHistory</b> associates the <see cref="Faction.Id"/> string of a <see
    /// cref="Faction"/> with its <see cref="Faction.FactionClass"/> and with the following
    /// important events in its history:
    /// </para><list type="bullet"><item>
    /// Zero or more <see cref="FactionEventType.Advance"/> events specify each turn ended by the
    /// <see cref="Faction"/> while it still participated in the game.
    /// </item><item>
    /// The <see cref="FactionEventType.Create"/> event specifies the turn during which the <see
    /// cref="Faction"/> was created. This is currently always the first game turn.
    /// </item><item>
    /// The <see cref="FactionEventType.Delete"/> event specifies the turn during which the <see
    /// cref="Faction"/> was deleted, if ever.
    /// </item><item>
    /// The <see cref="FactionEventType.Victory"/> event specifies the turn during which the <see
    /// cref="Faction"/> has won the game, if ever.
    /// </item></list><para>
    /// All events include the number of <see cref="Faction.Sites"/> and <see cref="Faction.Units"/>
    /// and the total <see cref="Faction.UnitStrength"/> which the <see cref="Faction"/> possessed
    /// at the time of the event. These numbers are zero for the <see
    /// cref="FactionEventType.Delete"/> event, obviously.
    /// </para><para>
    /// The <b>FactionHistory</b> persists even after the associated <see cref="Faction"/> has been
    /// deleted from the current <see cref="WorldState"/>. As such, it is useful for debugging and
    /// for statistics display.</para></remarks>

    public class FactionHistory: ICloneable, IKeyedValue<String> {
        #region FactionHistory(FactionHistory)

        /// <summary>
        /// Initializes a new instance of the <see cref="FactionHistory"/> class that is a shallow
        /// copy of the specified instance.</summary>
        /// <param name="history">
        /// The <see cref="FactionHistory"/> object whose property values should be copied to the
        /// new instance.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="history"/> is a null reference.</exception>
        /// <remarks>
        /// This "copy constructor" does not need to perform a deep copy as the properties of the
        /// <see cref="FactionHistory"/> class either contain immutable objects, or are to be
        /// treated as immutable in the case of the <see cref="Events"/> array.</remarks>

        private FactionHistory(FactionHistory history) {
            if (history == null)
                ThrowHelper.ThrowArgumentNullException("history");

            this._factionClass = history._factionClass;
            this._id = history._id;
            Events = history.Events;
        }

        #endregion
        #region FactionHistory(WorldState, Faction)

        /// <overloads>
        /// Initializes a new instance of the <see cref="FactionHistory"/> class.</overloads>
        /// <summary>
        /// Initializes a new instance of the <see cref="FactionHistory"/> class with the specified
        /// <see cref="WorldState"/> and <see cref="Faction"/>.</summary>
        /// <param name="worldState">
        /// The <see cref="WorldState"/> at the time when the specified <paramref name="faction"/>
        /// was created.</param>
        /// <param name="faction">
        /// The <see cref="Faction"/> providing the initial values for the <see
        /// cref="FactionClass"/> and <see cref="Id"/> properties.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="worldState"/> or <paramref name="faction"/> is a null reference.
        /// </exception>
        /// <remarks>
        /// The <see cref="Events"/> property is initialized to an empty <see cref="Array"/>.
        /// Clients must call <see cref="Create"/> once the <see cref="Faction"/> has been fully
        /// initialized to begin event recording.</remarks>

        public FactionHistory(WorldState worldState, Faction faction) {

            if (worldState == null)
                ThrowHelper.ThrowArgumentNullException("worldState");
            if (faction == null)
                ThrowHelper.ThrowArgumentNullException("faction");

            this._factionClass = faction.FactionClass;
            this._id = faction.Id;
            Events = new FactionEvent[] { };
        }

        #endregion
        #region Private Fields

        // property backers
        private readonly FactionClass _factionClass;
        private readonly string _id;

        #endregion
        #region Public Properties
        #region FactionClass

        /// <summary>
        /// Gets the scenario class of the associated <see cref="Faction"/>.</summary>
        /// <value>
        /// The underlying <see cref="Faction.FactionClass"/> of the <see cref="Faction"/> whose
        /// history is recorded by the <see cref="FactionHistory"/>.</value>
        /// <remarks>
        /// <b>FactionClass</b> never returns a null reference. This property never changes once the
        /// object has been constructed.</remarks>

        public FactionClass FactionClass {
            [DebuggerStepThrough]
            get { return this._factionClass; }
        }

        #endregion
        #region Events

        /// <summary>
        /// Gets a list of all events that occurred to the <see cref="Faction"/>.</summary>
        /// <value>
        /// An <see cref="Array"/> containing all <see cref="FactionEvent"/> objects that affected
        /// the associated <see cref="Faction"/>. The default is an empty array.</value>
        /// <remarks><para>
        /// Call <see cref="Advance"/>, <see cref="Create"/>, <see cref="Delete"/>, or <see
        /// cref="Victory"/> to add a new element of the corresponding <see
        /// cref="FactionEventType"/> to the <b>Events</b> array.
        /// </para><para>
        /// The <see cref="FactionHistory"/> class never changes or removes existing <b>Events</b>,
        /// and neither should client code.</para></remarks>

        public FactionEvent[] Events { get; private set; }

        #endregion
        #region Id

        /// <summary>
        /// Gets the identifier of the associated <see cref="Faction"/>.</summary>
        /// <value>
        /// The <see cref="Faction.Id"/> string of the <see cref="Faction"/> whose history is
        /// recorded by the <see cref="FactionHistory"/>.</value>
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
        /// Gets a value indicating whether the associated <see cref="Faction"/> has been deleted.
        /// </summary>
        /// <value>
        /// <c>true</c> if the <see cref="Faction"/> whose history is recorded by the <see
        /// cref="FactionHistory"/> has been deleted; otherwise, <c>false</c>.</value>
        /// <remarks>
        /// <b>IsDeleted</b> returns <c>true</c> exactly if the <see cref="FactionEvent.EventType"/>
        /// of the last <see cref="Events"/> element equals <see cref="FactionEventType.Delete"/>.
        /// Once this has happened, no new <see cref="Events"/> may be added.</remarks>

        public bool IsDeleted {
            get {
                int count = Events.Length - 1;
                return (count >= 0 && Events[count].EventType == FactionEventType.Delete);
            }
        }

        #endregion
        #region LastTurn

        /// <summary>
        /// Gets the highest turn index in the <see cref="Events"/> collection.</summary>
        /// <value>
        /// The <see cref="FactionEvent.Turn"/> value of the last element in the <see
        /// cref="Events"/> collection, or -1 if the collection is empty.</value>

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
        /// Adds the specified <see cref="FactionEvent"/> to the <see cref="Events"/> collection.
        /// </summary>
        /// <param name="factionEvent">
        /// The <see cref="FactionEvent"/> to add to the <see cref="Events"/> collection.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="factionEvent"/> is a null reference.</exception>
        /// <exception cref="PropertyValueException">
        /// <see cref="IsDeleted"/> is already <c>true</c>.</exception>

        private void AddEvent(FactionEvent factionEvent) {
            if (factionEvent == null)
                ThrowHelper.ThrowArgumentNullException("factionEvent");
            if (IsDeleted)
                ThrowHelper.ThrowPropertyValueException("IsDeleted", Tektosyne.Strings.PropertyTrue);

            // copy elements to new and bigger array
            int count = Events.Length;
            FactionEvent[] array = new FactionEvent[count + 1];
            Events.CopyTo(array, 0);

            // add event to end of new array
            array[count] = factionEvent;
            Events = array;
        }

        #endregion
        #endregion
        #region Internal Methods
        #region Create

        /// <summary>
        /// Adds a <see cref="FactionEventType.Create"/> event to the <see cref="Events"/>
        /// collection.</summary>
        /// <param name="worldState">
        /// The current <see cref="WorldState"/> at the time of the event.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="worldState"/> is a null reference.</exception>
        /// <exception cref="KeyNotFoundException">
        /// The <see cref="WorldState.Factions"/> collection of the specified <paramref
        /// name="worldState"/> does not contain the <see cref="Id"/> of the associated <see
        /// cref="Faction"/>.</exception>
        /// <exception cref="PropertyValueException"><para>
        /// <see cref="Events"/> already contains one or more elements.
        /// </para><para>-or-</para><para>
        /// <see cref="IsDeleted"/> is already <c>true</c>.</para></exception>
        /// <remarks>
        /// <b>Create</b> creates a new <see cref="FactionEventType.Create"/> event with the <see
        /// cref="WorldState.CurrentTurn"/> and the current data of the associated <see
        /// cref="Faction"/> in the <paramref name="worldState"/>.</remarks>

        internal void Create(WorldState worldState) {
            if (worldState == null)
                ThrowHelper.ThrowArgumentNullException("worldState");
            if (Events.Length > 0)
                ThrowHelper.ThrowPropertyValueException(
                    "Events", Tektosyne.Strings.PropertyNotEmpty);

            Faction faction = worldState.Factions[Id];
            AddEvent(new FactionEvent(FactionEventType.Create, worldState.CurrentTurn,
                faction.Sites.Count, faction.Units.Count, faction.UnitStrength));
        }

        #endregion
        #region Advance

        /// <summary>
        /// Adds a <see cref="FactionEventType.Advance"/> event to the <see cref="Events"/>
        /// collection.</summary>
        /// <param name="worldState">
        /// The current <see cref="WorldState"/> at the time of the event.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="worldState"/> is a null reference.</exception>
        /// <exception cref="KeyNotFoundException">
        /// The <see cref="WorldState.Factions"/> collection of the specified <paramref
        /// name="worldState"/> does not contain the <see cref="Id"/> of the associated <see
        /// cref="Faction"/>.</exception>
        /// <exception cref="PropertyValueException">
        /// <see cref="IsDeleted"/> is already <c>true</c>.</exception>
        /// <remarks>
        /// <b>Advance</b> creates a new <see cref="FactionEventType.Advance"/> event with the <see
        /// cref="WorldState.CurrentTurn"/> and the current data of the associated <see
        /// cref="Faction"/> in the <paramref name="worldState"/>.</remarks>

        internal void Advance(WorldState worldState) {
            if (worldState == null)
                ThrowHelper.ThrowArgumentNullException("worldState");

            Faction faction = worldState.Factions[Id];
            AddEvent(new FactionEvent(FactionEventType.Advance, worldState.CurrentTurn,
                faction.Sites.Count, faction.Units.Count, faction.UnitStrength));
        }

        #endregion
        #region Delete

        /// <summary>
        /// Adds a <see cref="FactionEventType.Delete"/> event to the <see cref="Events"/>
        /// collection.</summary>
        /// <param name="worldState">
        /// The current <see cref="WorldState"/> at the time of the event.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="worldState"/> is a null reference.</exception>
        /// <exception cref="PropertyValueException">
        /// <see cref="IsDeleted"/> is already <c>true</c>.</exception>
        /// <remarks>
        /// <b>Delete</b> creates a new <see cref="FactionEventType.Delete"/> event with the <see
        /// cref="WorldState.CurrentTurn"/> of the specified <paramref name="worldState"/>.
        /// </remarks>

        internal void Delete(WorldState worldState) {
            if (worldState == null)
                ThrowHelper.ThrowArgumentNullException("worldState");

            AddEvent(new FactionEvent(FactionEventType.Delete, worldState.CurrentTurn));
        }

        #endregion
        #region Victory

        /// <summary>
        /// Adds a <see cref="FactionEventType.Victory"/> event to the <see cref="Events"/>
        /// collection.</summary>
        /// <param name="worldState">
        /// The current <see cref="WorldState"/> at the time of the event.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="worldState"/> is a null reference.</exception>
        /// <exception cref="KeyNotFoundException">
        /// The <see cref="WorldState.Factions"/> collection of the specified <paramref
        /// name="worldState"/> does not contain the <see cref="Id"/> of the associated <see
        /// cref="Faction"/>.</exception>
        /// <exception cref="PropertyValueException">
        /// <see cref="IsDeleted"/> is already <c>true</c>.</exception>
        /// <remarks>
        /// <b>Victory</b> creates a new <see cref="FactionEventType.Victory"/> event with the <see
        /// cref="WorldState.CurrentTurn"/> and the current data of the associated <see
        /// cref="Faction"/> in the <paramref name="worldState"/>.</remarks>

        internal void Victory(WorldState worldState) {
            if (worldState == null)
                ThrowHelper.ThrowArgumentNullException("worldState");

            Faction faction = worldState.Factions[Id];
            AddEvent(new FactionEvent(FactionEventType.Victory, worldState.CurrentTurn,
                faction.Sites.Count, faction.Units.Count, faction.UnitStrength));
        }

        #endregion
        #endregion
        #region ICloneable Members

        /// <summary>
        /// Creates a new <see cref="FactionHistory"/> object that is a shallow copy of the current
        /// instance.</summary>
        /// <returns>
        /// A new <see cref="FactionHistory"/> object that is a shallow copy of the current
        /// instance.</returns>
        /// <remarks>
        /// <b>Clone</b> invokes the "copy constructor", <see
        /// cref="FactionHistory(FactionHistory)"/>, to create a shallow copy of the current
        /// instance.</remarks>

        public object Clone() {
            return new FactionHistory(this);
        }

        #endregion
        #region IKeyedValue Members

        /// <summary>
        /// Gets the identifier of the associated <see cref="Faction"/>.</summary>
        /// <value>
        /// The value of the <see cref="Id"/> property.</value>

        string IKeyedValue<String>.Key {
            [DebuggerStepThrough]
            get { return Id; }
        }

        #endregion
        #region Class FactionEvent

        /// <summary>
        /// Describes an event that occurred in the course of a <see cref="FactionHistory"/>.
        /// </summary>
        /// <remarks>
        /// <b>FactionEvent</b> is a simple data container that associates the index of a game turn
        /// with an <see cref="FactionEventType"/> indicating some event that affected the <see
        /// cref="Faction"/> during that turn, and also with the number of its <see
        /// cref="Faction.Sites"/> and <see cref="Faction.Units"/> and its total <see
        /// cref="Faction.UnitStrength"/> at the time of the event.</remarks>

        public class FactionEvent {
            #region FactionEvent(FactionEventType, Int32)

            /// <overloads>
            /// Initializes a new instance of the <see cref="FactionEvent"/> class.</overloads>
            /// <summary>
            /// Initializes a new instance of the <see cref="FactionEvent"/> class with the
            /// specified event type and turn index.</summary>
            /// <param name="type">
            /// The initial value for the <see cref="EventType"/> field.</param>
            /// <param name="turn">
            /// The initial value for the <see cref="Turn"/> field.</param>
            /// <exception cref="ArgumentOutOfRangeException">
            /// <paramref name="turn"/> is less than zero.</exception>
            /// <remarks>
            /// The <see cref="Sites"/>, <see cref="Units"/>, and <see cref="UnitStrength"/> fields
            /// are set to zero.</remarks>

            internal FactionEvent(FactionEventType type, int turn):
                this(type, turn, 0, 0, 0) { }

            #endregion
            #region FactionEvent(FactionEventType, Int32, Int32, Int32, Int32)

            /// <summary>
            /// Initializes a new instance of the <see cref="FactionEvent"/> class with the
            /// specified event type, turn index, and number of owned sites and units.</summary>
            /// <param name="type">
            /// The initial value for the <see cref="EventType"/> field.</param>
            /// <param name="turn">
            /// The initial value for the <see cref="Turn"/> field.</param>
            /// <param name="sites">
            /// The initial value for the <see cref="Sites"/> field.</param>
            /// <param name="units">
            /// The initial value for the <see cref="Units"/> field.</param>
            /// <param name="unitStrength">
            /// The initial value for the <see cref="UnitStrength"/> field.</param>
            /// <exception cref="ArgumentOutOfRangeException">
            /// <paramref name="turn"/>, <paramref name="sites"/>, <paramref name="units"/>, or
            /// <paramref name="unitStrength"/> is less than zero.</exception>

            internal FactionEvent(FactionEventType type, int turn,
                int sites, int units, int unitStrength) {

                if (turn < 0)
                    ThrowHelper.ThrowArgumentOutOfRangeException(
                        "turn", turn, Tektosyne.Strings.ArgumentNegative);

                if (sites < 0)
                    ThrowHelper.ThrowArgumentOutOfRangeException(
                        "sites", sites, Tektosyne.Strings.ArgumentNegative);

                if (units < 0)
                    ThrowHelper.ThrowArgumentOutOfRangeException(
                        "units", units, Tektosyne.Strings.ArgumentNegative);

                if (unitStrength < 0)
                    ThrowHelper.ThrowArgumentOutOfRangeException(
                        "unitStrength", unitStrength, Tektosyne.Strings.ArgumentNegative);

                this._eventType = type;
                this._turn = turn;
                this._sites = sites;
                this._units = units;
                this._unitStrength = unitStrength;
            }

            #endregion
            #region Private Fields

            // property backers
            private readonly FactionEventType _eventType;
            private readonly int _turn, _sites, _units, _unitStrength;

            #endregion
            #region EventType

            /// <summary>
            /// The event type of the <see cref="FactionEvent"/>.</summary>

            public FactionEventType EventType {
                [DebuggerStepThrough]
                get { return this._eventType; }
            }

            #endregion
            #region Turn

            /// <summary>
            /// The zero-based index of the game turn during which the <see cref="FactionEvent"/>
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
            #region Sites

            /// <summary>
            /// The number of <see cref="Faction.Sites"/> that belonged to the associated <see
            /// cref="Faction"/> when the <see cref="FactionEvent"/> occurred.</summary>
            /// <remarks>
            /// <b>Sites</b> defaults to zero, and is never less than zero.</remarks>

            public int Sites {
                [DebuggerStepThrough]
                get { return this._sites; }
            }

            #endregion
            #region Units

            /// <summary>
            /// The number of <see cref="Faction.Units"/> that belonged to the associated <see
            /// cref="Faction"/> when the <see cref="FactionEvent"/> occurred.</summary>
            /// <remarks>
            /// <b>Units</b> defaults to zero, and is never less than zero.</remarks>

            public int Units {
                [DebuggerStepThrough]
                get { return this._units; }
            }

            #endregion
            #region UnitStrength

            /// <summary>
            /// The total <see cref="Faction.UnitStrength"/> of the associated <see cref="Faction"/>
            /// when the <see cref="FactionEvent"/> occurred.</summary>
            /// <remarks>
            /// <b>UnitStrength</b> defaults to zero, and is never less than zero.</remarks>

            public int UnitStrength {
                [DebuggerStepThrough]
                get { return this._unitStrength; }
            }

            #endregion
        }

        #endregion
        #region Enum FactionEventType

        /// <summary>
        /// Specifies the type of a <see cref="FactionEvent"/>.</summary>
        /// <remarks>
        /// <b>FactionEventType</b> specifies the possible values for the <see
        /// cref="FactionEvent.EventType"/> field of the <see cref="FactionEvent"/> class.</remarks>

        public enum FactionEventType: byte {
            #region Advance

            /// <summary>
            /// Specifies that the <see cref="Faction"/> has ended a game turn.</summary>

            Advance,

            #endregion
            #region Create

            /// <summary>
            /// Specifies that the <see cref="Faction"/> was created.</summary>
            /// <remarks>
            /// A <see cref="FactionHistory"/> always contains a single <b>Create</b> event, and it
            /// is always the first element in the <see cref="FactionHistory.Events"/> collection.
            /// </remarks>

            Create,

            #endregion
            #region Delete

            /// <summary>
            /// Specifies that the <see cref="Faction"/> was deleted.</summary>
            /// <remarks>
            /// A <see cref="FactionHistory"/> contains at most a single <b>Delete</b> event, and
            /// it is always the last element in the <see cref="FactionHistory.Events"/> collection.
            /// </remarks>

            Delete,

            #endregion
            #region Victory

            /// <summary>
            /// Specifies that the <see cref="Faction"/> was victorious.</summary>
            /// <remarks>
            /// A <see cref="FactionHistory"/> contains at most a single <b>Victory</b> event, and
            /// it is always the last element in the <see cref="FactionHistory.Events"/> collection.
            /// </remarks>

            Victory

            #endregion
        }

        #endregion
    }
}
