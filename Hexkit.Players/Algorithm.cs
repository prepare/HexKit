using System;
using System.Diagnostics;
using System.Xml;

using Tektosyne;
using Tektosyne.Collections;
using Tektosyne.Windows;

using Hexkit.Global;
using Hexkit.World;
using Hexkit.World.Commands;

namespace Hexkit.Players {
    #region Type Aliases

    using FactionStateList = KeyedList<String, FactionState>;

    #endregion

    /// <summary>
    /// Provides common functionality for all computer player algorithms.</summary>
    /// <remarks><para>
    /// <b>Algorithm</b> provides common functionality for computer player algorithms. This includes
    /// interfacing with clients, options and state management, and XML serialization.
    /// </para><para>
    /// <b>Algorithm</b> does not implement any part of the actual computer player algorithm.
    /// Derived classes should override the abstract <see cref="Algorithm.FindBestCommands"/> method
    /// for that purpose.
    /// </para><para>
    /// <b>Algorithm</b> is serialized to the XML element "algorithm" in a Hexkit session
    /// description. The value of the "id" attribute equals that of the <see cref="Algorithm.Id"/>
    /// property in a derived class.</para></remarks>

    public abstract class Algorithm: XmlSerializable, IKeyedValue<String> {
        #region Private Fields

        // property backers
        private volatile WorldState _bestWorld;
        private readonly CommandExecutor _executor = new CommandExecutor();
        private readonly FactionStateList _factionStates = new FactionStateList();

        #endregion
        #region Protected Members
        #region Executor

        /// <summary>
        /// Gets the <see cref="CommandExecutor"/> that executes game commands for the <see
        /// cref="Algorithm"/>.</summary>
        /// <value>
        /// The <see cref="CommandExecutor"/> that the <see cref="Algorithm"/> will use to create
        /// and execute <see cref="Command"/> objects.</value>
        /// <remarks>
        /// <b>Executor</b> never returns a null reference. This property never changes once the
        /// object has been constructed.</remarks>

        protected CommandExecutor Executor {
            [DebuggerStepThrough]
            get { return this._executor; }
        }

        #endregion
        #region FactionStates

        /// <summary>
        /// Gets a list of <see cref="FactionState"/> data for all factions controlled by the <see
        /// cref="Algorithm"/>.</summary>
        /// <value>
        /// A <see cref="FactionStateList"/> containing all <see cref="FactionState"/> data for the
        /// <see cref="Algorithm"/>.</value>
        /// <remarks>
        /// <b>FactionStates</b> never returns a null reference, but it returns an empty collection
        /// if no state data has been stored.</remarks>

        protected FactionStateList FactionStates {
            [DebuggerStepThrough]
            get { return this._factionStates; }
        }

        #endregion
        #region FindBestCommands

        /// <summary>
        /// Performs the actual computer player calculations.</summary>
        /// <param name="worldState">
        /// The <see cref="WorldState"/> whose active faction should issue commands.</param>
        /// <param name="options">
        /// An <see cref="AlgorithmOptions"/> object containing optional settings for the <see
        /// cref="Algorithm"/>.</param>
        /// <param name="events">
        /// An optional <see cref="TaskEvents"/> object used for progress display.</param>
        /// <remarks><para>
        /// <see cref="Algorithm"/> does not implement <b>FindBestCommands</b>. Derived classes must
        /// override this method to perform the actual computer calculations in response to a call
        /// to <see cref="FindBestWorld"/>.
        /// </para><para>
        /// The specified <paramref name="worldState"/> is guaranteed not to be a null reference and
        /// may be modified as desired. <b>FindBestCommands</b> should issue commands for the active
        /// faction and add them to the command <see cref="History"/>.
        /// </para><para>
        /// However, no <see cref="BeginTurnCommand"/> or <see cref="EndTurnCommand"/> should be
        /// generated for the active faction, as these commands were or will be issued by the
        /// calling methods.</para></remarks>

        protected abstract void FindBestCommands(WorldState worldState,
            AlgorithmOptions options, TaskEvents events);

        #endregion
        #endregion
        #region BestWorld

        /// <summary>
        /// Gets the best possible end-of-turn <see cref="WorldState"/> found by the last successful
        /// computer player calculations.</summary>
        /// <value><para>
        /// The <see cref="WorldState"/> found by the last successful call to <see
        /// cref="FindBestWorld"/>.
        /// </para><para>-or-</para><para>
        /// A null reference if <b>FindBestWorld</b> raised an exception, or if the method has not
        /// been called yet.</para></value>
        /// <remarks>
        /// <b>BestWorld</b> is backed by a <c>volatile</c> field to ensure proper communication
        /// with clients when <see cref="FindBestWorld"/> is run on a background thread.</remarks>

        public WorldState BestWorld {
            [DebuggerStepThrough]
            get { return this._bestWorld; }
        }

        #endregion
        #region Id

        /// <summary>
        /// Gets the identifier of the <see cref="Algorithm"/>.</summary>
        /// <value>
        /// The identifier of the <see cref="Algorithm"/>. This is the <see
        /// cref="System.Reflection.MemberInfo.Name"/> of the actual concrete <see cref="Type"/> of
        /// this object.</value>
        /// <remarks>
        /// <b>Id</b> returns a unique internal identifier that is used for XML serialization and
        /// key indexing in the <see cref="PlayerManager.Algorithms"/> collection.</remarks>

        public string Id {
            [DebuggerStepThrough]
            get { return GetType().Name; }
        }

        #endregion
        #region Name

        /// <summary>
        /// Gets the display name of the <see cref="Algorithm"/>.</summary>
        /// <value>
        /// The display name of the <see cref="Algorithm"/>. The default is the value of the <see
        /// cref="Id"/> property.</value>
        /// <remarks><para>
        /// <b>Name</b> returns the display name that should be used to represent the <see
        /// cref="Algorithm"/> within Hexkit Editor and Hexkit Game.
        /// </para><para>
        /// Derived classes may override <b>Name</b> if the value of the <see cref="Id"/> property
        /// is inadequate for display to the user. When overriding <b>Name</b>, you should never
        /// return a null reference or an empty string.</para></remarks>

        public virtual string Name {
            [DebuggerStepThrough]
            get { return Id; }
        }

        #endregion
        #region ClearBestWorld

        /// <summary>
        /// Sets the <see cref="BestWorld"/> property to a null reference.</summary>
        /// <remarks>
        /// <see cref="FindBestWorld"/> automatically sets the <see cref="BestWorld"/> property to a
        /// null reference if an error occurred, but clients may find it useful to explicitly clear
        /// this property at other times.</remarks>

        public void ClearBestWorld() {
            this._bestWorld = null;
        }

        #endregion
        #region CreateFactionState

        /// <summary>
        /// Creates a new instance of the class that should be used for <see cref="FactionStates"/>
        /// elements.</summary>
        /// <param name="faction">
        /// The initial value for the <see cref="FactionState.Faction"/> property of the new
        /// instance.</param>
        /// <returns>
        /// A new instance of the <see cref="FactionState"/> class.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="faction"/> is a null reference or an empty string.</exception>
        /// <remarks><para>
        /// <b>CreateFactionState</b> is called by <see cref="GetFactionState"/> to create new
        /// elements for the <see cref="FactionStates"/> collection.
        /// </para><para>
        /// Derived classes that require persistent state data should override this method to
        /// instantiate an algorithm-specific class derived from <see cref="FactionState"/>.
        /// </para></remarks>

        public virtual FactionState CreateFactionState(string faction) {
            return new FactionState(faction);
        }

        #endregion
        #region FindBestWorld

        /// <summary>
        /// Finds the best possible end-of-turn <see cref="WorldState"/> for the active faction in
        /// the specified <see cref="WorldState"/>.</summary>
        /// <param name="worldState">
        /// The <see cref="WorldState"/> that represents the current situation.</param>
        /// <param name="options">
        /// An <see cref="AlgorithmOptions"/> object containing optional settings for the <see
        /// cref="Algorithm"/>.</param>
        /// <param name="events">
        /// An optional <see cref="TaskEvents"/> object used for progress display.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="worldState"/> or <paramref name="options"/> is a null reference.
        /// </exception>
        /// <exception cref="Exception">
        /// An error occurred. <see cref="BestWorld"/> will be set to a null reference.</exception>
        /// <remarks><para>
        /// <b>FindBestWorld</b> sets the <see cref="BestWorld"/> property to a deep copy of the
        /// specified <paramref name="worldState"/> that was modified to represent the situation at
        /// the end of the active faction's turn.
        /// </para><para>
        /// All commands that were issued to create the new situation will be added to the <see
        /// cref="History"/> associated with <b>BestWorld</b>. No <see cref="EndTurnCommand"/> is
        /// generated; that is, the active faction remains active.
        /// </para><para>
        /// <b>FindBestWorld</b> issues a <see cref="BeginTurnCommand"/> if necessary. Then, if <see
        /// cref="WorldState.GameOver"/> is still <c>false</c>, <b>FindBestWorld</b> calls <see
        /// cref="FindBestCommands"/> with a deep copy of <paramref name="worldState"/> and with the
        /// specified <paramref name="options"/>. Derived classes must override
        /// <b>FindBestCommands</b> to perform the actual calculations.
        /// </para><para>
        /// The specified <paramref name="worldState"/> always remains unchanged. <b>BestWorld</b>
        /// is set to a null reference if an exception was raised during the calculation.
        /// </para></remarks>

        public void FindBestWorld(WorldState worldState,
            AlgorithmOptions options, TaskEvents events) {

            if (worldState == null)
                ThrowHelper.ThrowArgumentNullException("worldState");
            if (options == null)
                ThrowHelper.ThrowArgumentNullException("options");

            // default to a null reference
            this._bestWorld = null;

            // create deep copy of world state
            WorldState clone = (WorldState) worldState.Clone();

            // issue BeginTurn command if necessary
            if (!clone.History.HaveBeginTurn())
                Executor.ExecuteBeginTurn(clone);

            // check if game was just won
            if (!clone.GameOver) {

                // compute best end-of-turn world state
                FindBestCommands(clone, options, events);
            }

            // store result on success
            this._bestWorld = clone;
        }

        #endregion
        #region GetFactionState

        /// <summary>
        /// Gets the <see cref="FactionStates"/> element for the active faction in the specified
        /// <see cref="WorldState"/>, given the specified maximum age.</summary>
        /// <param name="worldState">
        /// The <see cref="WorldState"/> whose active faction should be controlled by the <see
        /// cref="Algorithm"/>.</param>
        /// <param name="maxAge"><para>
        /// A positive <see cref="Int32"/> value indicating the maximum difference between the <see
        /// cref="WorldState.CurrentTurn"/> of the specified <paramref name="worldState"/>, and the
        /// <see cref="FactionState.Turn"/> of the <see cref="FactionStates"/> element for the
        /// active faction.
        /// </para><para>-or-</para><para>
        /// Zero to indicate that the <b>Turn</b> of the <b>FactionStates</b> element should not be
        /// considered.</para></param>
        /// <returns>
        /// The <see cref="FactionStates"/> element whose <see cref="FactionState.Faction"/> matches
        /// that of the active faction in the specified <paramref name="worldState"/>.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="worldState"/> is a null reference.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="maxAge"/> is less than zero.</exception>
        /// <remarks><para>
        /// <b>GetFactionState</b> sets the <see cref="FactionState.Turn"/> property of any returned
        /// <see cref="FactionState"/> to the <see cref="WorldState.CurrentTurn"/> of the specified
        /// <paramref name="worldState"/>.
        /// </para><para>
        /// If <b>GetFactionState</b> finds a matching <see cref="FactionStates"/> element whose
        /// <b>Turn</b> property is greater than <b>CurrentTurn</b> or less than <b>CurrentTurn</b>
        /// minus <paramref name="maxAge"/>, the element is considered invalid and replaced with a
        /// new instance, created by the <see cref="CreateFactionState"/> method.
        /// </para><para>
        /// When a faction is continuously controlled by the same <see cref="Algorithm"/>, the age
        /// of its corresponding <b>FactionStates</b> element is always one turn whenever <see
        /// cref="FindBestWorld"/> begins execution.
        /// </para><para>
        /// The age may be greater than one turn if either the faction's controlling player or its
        /// associated <b>Algorithm</b> has temporarily changed. The <paramref name="maxAge"/>
        /// parameter allows the <b>Algorithm</b> to decide whether to accept or reject such
        /// outdated <b>FactionState</b> instances.</para></remarks>

        public FactionState GetFactionState(WorldState worldState, int maxAge) {
            if (worldState == null)
                ThrowHelper.ThrowArgumentNullException("worldState");
            if (maxAge < 0)
                ThrowHelper.ThrowArgumentOutOfRangeException(
                    "maxAge", maxAge, Tektosyne.Strings.ArgumentNegative);

            // get state data for active faction
            string id = worldState.ActiveFaction.Id;
            FactionState factionState;
            FactionStates.TryGetValue(id, out factionState);

            // check for invalid or excessive age
            if (factionState != null && maxAge > 0) {
                int age = worldState.CurrentTurn - factionState.Turn;

                // delete entry if invalid
                if (age < 0 || age > maxAge) {
                    FactionStates.Remove(factionState);
                    factionState = null;
                }
            }

            // create new entry if required
            if (factionState == null) {
                factionState = CreateFactionState(id);
                FactionStates.Add(factionState);
            }

            // update state data's age
            factionState.Turn = worldState.CurrentTurn;

            return factionState;
        }

        #endregion
        #region ToString

        /// <summary>
        /// Returns a <see cref="String"/> that represents the <see cref="Algorithm"/>.</summary>
        /// <returns>
        /// The value of the <see cref="Name"/> property.</returns>

        public override string ToString() {
            return Name;
        }

        #endregion
        #region IKeyedValue Members

        /// <summary>
        /// Gets the identifier of the <see cref="Algorithm"/>.</summary>
        /// <value>
        /// The value of the <see cref="Id"/> property.</value>

        string IKeyedValue<String>.Key {
            [DebuggerStepThrough]
            get { return Id; }
        }

        #endregion
        #region XmlSerializable Members
        #region ConstXmlName

        /// <summary>
        /// The name of the XML element associated with the <see cref="Algorithm"/> class.</summary>
        /// <remarks>
        /// <b>ConstXmlName</b> holds the value "algorithm", indicating the XML element in <see
        /// cref="FilePaths.SessionSchema"/> whose data is managed by the <see
        /// cref="Algorithm"/> class.</remarks>

        public const string ConstXmlName = "algorithm";

        #endregion
        #region XmlName

        /// <summary>
        /// Gets the name of the XML element associated with the <see cref="Algorithm"/> object.
        /// </summary>
        /// <value>
        /// The name of the XML element to which the data of this <see cref="Algorithm"/> object is
        /// serialized. The default is the value of the constant field <b>ConstXmlName</b>.</value>
        /// <remarks><para>
        /// <b>XmlName</b> specifies the name of the XML element defined in <see
        /// cref="FilePaths.SessionSchema"/> that is expected by <see
        /// cref="XmlSerializable.ReadXml"/> and created by <see cref="XmlSerializable.WriteXml"/>.
        /// </para><para>
        /// <see cref="Algorithm"/> overrides this property so that derived classes do not have to
        /// provide their own <b>ConstXmlName</b> fields.</para></remarks>

        internal override string XmlName {
            [DebuggerStepThrough]
            get { return ConstXmlName; }
        }

        #endregion
        #region ReadXmlElements

        /// <summary>
        /// Reads XML element data into the <see cref="Algorithm"/> object using the specified <see
        /// cref="XmlReader"/>.</summary>
        /// <param name="reader">
        /// The <see cref="XmlReader"/> from which to read.</param>
        /// <returns>
        /// <c>true</c> if the current node of the specified <paramref name="reader"/> contained any
        /// matching data; otherwise, <c>false</c>.</returns>
        /// <exception cref="XmlException">
        /// An error occurred while parsing the XML data provided by <paramref name="reader"/>.
        /// </exception>
        /// <exception cref="System.Xml.Schema.XmlSchemaException">
        /// <paramref name="reader"/> is an <see cref="XmlValidatingReader"/>, and the XML data did
        /// not conform to <see cref="FilePaths.SessionSchema"/>.</exception>
        /// <remarks>
        /// Please refer to class <see cref="XmlSerializable"/> for a complete description of this
        /// method.</remarks>

        protected override bool ReadXmlElements(XmlReader reader) {
            switch (reader.Name) {

                case FactionState.ConstXmlName: {
                    string id = reader["faction"];
                    if (id != null) {

                        // read faction state if desired
                        FactionState state = CreateFactionState(id);
                        if (state != null) {
                            state.ReadXml(reader);
                            FactionStates.Add(state);
                        }
                    }
                    return true;
                }

                default: return false;
            }
        }

        #endregion
        #region WriteXmlAttributes

        /// <summary>
        /// Writes all current data of the <see cref="Algorithm"/> object that is serialized to XML
        /// attributes to the specified <see cref="XmlWriter"/>.</summary>
        /// <param name="writer">
        /// The <see cref="XmlWriter"/> to which to write.</param>
        /// <remarks>
        /// Please refer to class <see cref="XmlSerializable"/> for a complete description of this
        /// method.</remarks>

        protected override void WriteXmlAttributes(XmlWriter writer) {
            writer.WriteAttributeString("id", Id);
        }

        #endregion
        #region WriteXmlElements

        /// <summary>
        /// Writes all current data of the <see cref="Algorithm"/> object that is serialized to
        /// nested XML elements to the specified <see cref="XmlWriter"/>.</summary>
        /// <param name="writer">
        /// The <see cref="XmlWriter"/> to which to write.</param>
        /// <remarks>
        /// Please refer to class <see cref="XmlSerializable"/> for a complete description of this
        /// method.</remarks>

        protected override void WriteXmlElements(XmlWriter writer) {

            foreach (FactionState state in FactionStates)
                state.WriteXml(writer);
        }

        #endregion
        #endregion
    }
}
