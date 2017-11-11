using System;
using System.Diagnostics;
using System.Reflection;
using System.Xml;

using Tektosyne;
using Tektosyne.Collections;
using Tektosyne.Xml;

using Hexkit.Global;
using Hexkit.World.Commands;

namespace Hexkit.World {
    #region Type Aliases

    using CommandList = ListEx<Command>;
    using EntityHistoryDictionary = DictionaryEx<String, EntityHistory>;
    using FactionHistoryDictionary = DictionaryEx<String, FactionHistory>;

    #endregion

    /// <summary>
    /// Represents the history of a Hexkit game world.</summary>
    /// <remarks><para>
    /// <b>History</b> records all commands that were executed since the start of a game. This
    /// enables the faithful recreation of a <see cref="WorldState"/> from a compact serialization
    /// format (saved games) while keeping track of any actions that were performed, for replay or
    /// evaluation purposes.
    /// </para><para>
    /// <b>History</b> also records the history of all <see cref="Entity"/> and <see
    /// cref="Faction"/> objects that were created since the start of a game, and retains their
    /// history even after they have been deleted from the current <see cref="WorldState"/>.
    /// </para><para>
    /// <b>History</b> is serialized to the XML element "history" defined in <see
    /// cref="FilePaths.SessionSchema"/>.</para></remarks>

    public class History: ICloneable, IXmlSerializable {
        #region History()

        /// <overloads>
        /// Initializes a new instance of the <see cref="History"/> class.</overloads>
        /// <summary>
        /// Initializes a new instance of the <see cref="History"/> class with default properties.
        /// </summary>

        internal History() {
            this._commands = new CommandList();
            this._entities = new EntityHistoryDictionary();
            this._factions = new FactionHistoryDictionary();
        }

        #endregion
        #region History(History)

        /// <summary>
        /// Initializes a new instance of the <see cref="History"/> class that is a deep copy of the
        /// specified instance.</summary>
        /// <param name="history">
        /// The <see cref="History"/> object whose properties should be copied to the new instance.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="history"/> is a null reference.</exception>
        /// <remarks>
        /// This constructor is called by <see cref="Clone"/> to perform a deep copy of the
        /// specified <paramref name="history"/>. Please refer to that method for further details.
        /// </remarks>

        private History(History history) {
            if (history == null)
                ThrowHelper.ThrowArgumentNullException("history");

            FullTurns = history.FullTurns;
            IsInitialized = history.IsInitialized;

            // copy constructor creates shallow copy
            this._commands = new CommandList(history._commands);

            // Copy calls Clone on each element
            this._entities = history._entities.Copy();
            this._factions = history._factions.Copy();
        }

        #endregion
        #region Private Fields

        // property backers
        private readonly CommandList _commands;
        private readonly EntityHistoryDictionary _entities;
        private readonly FactionHistoryDictionary _factions;

        #endregion
        #region Commands

        /// <summary>
        /// Gets a list of all commands executed so far.</summary>
        /// <value>
        /// A read-only <see cref="CommandList"/> containing all <see cref="Command"/> objects that
        /// have been executed so far. The default is an empty collection.</value>
        /// <remarks><para>
        /// <b>Commands</b> never returns a null reference, and its elements are never null
        /// references. Use the <see cref="AddCommand"/> and <see cref="AddCommands"/> methods to
        /// add new <see cref="Command"/> objects to the collection. Once added, elements cannot be
        /// removed from the collection.
        /// </para><para>
        /// The first command that was executed in the game recorded by the <see cref="History"/> is
        /// stored at index position zero while the most recent command is stored at the highest
        /// index position.</para></remarks>

        public CommandList Commands {
            [DebuggerStepThrough]
            get { return this._commands.AsReadOnly(); }
        }

        #endregion
        #region Entities

        /// <summary>
        /// Gets the history of all entities created so far.</summary>
        /// <value>
        /// A read-only <see cref="EntityHistoryDictionary"/> mapping <see cref="Entity.Id"/>
        /// strings of <see cref="Entity"/> objects to the corresponding <see cref="EntityHistory"/>
        /// objects. The default is an empty collection.</value>
        /// <remarks>
        /// Call <see cref="AddEntity"/> to add a new <see cref="EntityHistory"/> object to the
        /// <b>Entities</b> collection. Once added, elements cannot be removed from the collection.
        /// </remarks>

        public EntityHistoryDictionary Entities {
            [DebuggerStepThrough]
            get { return this._entities.AsReadOnly(); }
        }

        #endregion
        #region Factions

        /// <summary>
        /// Gets the history of all factions created so far.</summary>
        /// <value>
        /// A read-only <see cref="FactionHistoryDictionary"/> mapping <see cref="Faction.Id"/>
        /// strings of <see cref="Faction"/> objects to the corresponding <see
        /// cref="FactionHistory"/> objects. The default is an empty collection.</value>
        /// <remarks>
        /// Call <see cref="AddFaction"/> to add a new <see cref="FactionHistory"/> object to the
        /// <b>Factions</b> collection. Once added, elements cannot be removed from the collection.
        /// </remarks>

        public FactionHistoryDictionary Factions {
            [DebuggerStepThrough]
            get { return this._factions.AsReadOnly(); }
        }

        #endregion
        #region FullTurns

        /// <summary>
        /// Gets the number of full turns completed in <see cref="Commands"/>.</summary>
        /// <value>
        /// The number of full turns that were completed by any <see cref="EndTurnCommand"/> stored
        /// in the <see cref="Commands"/> collection. The default is zero.</value>
        /// <remarks><para>
        /// <b>FullTurns</b> is increased automatically by the <see cref="AddCommand"/> and <see
        /// cref="AddCommands"/> methods.
        /// </para><para>
        /// When replaying the <see cref="Commands"/> of another <see cref="History"/> object, use
        /// the <see cref="CopyFullTurns"/> method to display a correct total turn count.
        /// </para></remarks>

        public int FullTurns { get; private set; }

        #endregion
        #region IsInitialized

        /// <summary>
        /// Gets or sets a value indicating whether the <see cref="History"/> object has been fully
        /// initialized.</summary>
        /// <value>
        /// <c>true</c> if all properties of the <see cref="History"/> object have been fully
        /// initialized; otherwise, <c>false</c>. The default is <c>false</c>.</value>
        /// <remarks><para>
        /// <b>IsInitialized</b> is set to <c>true</c> when the <see cref="Entities"/> and <see
        /// cref="Factions"/> collections have been initialized with all entity &amp; faction data
        /// required for the current scenario.
        /// </para><para>
        /// This always happens before the first <see cref="Commands"/> entry is executed. However,
        /// when replaying a saved game, the <see cref="Commands"/> collection is first filled with
        /// all recorded commands, and then all entity &amp; faction data is added.</para></remarks>

        public bool IsInitialized { get; internal set; }

        #endregion
        #region AddCommand

        /// <summary>
        /// Adds the specified command to the <see cref="Commands"/> collection.</summary>
        /// <param name="command">
        /// The <see cref="Command"/> to add to the <see cref="Commands"/> collection.</param>
        /// <param name="turn">
        /// The <see cref="WorldState.CurrentTurn"/> index after <paramref name="command"/> has been
        /// executed.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="command"/> is a null reference.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="turn"/> is less than <see cref="FullTurns"/> or greater than
        /// <b>FullTurns</b> plus one.</exception>
        /// <remarks>
        /// <b>AddCommand</b> adds the specified <paramref name="command"/> to the <see
        /// cref="Commands"/> collection, and sets the <see cref="FullTurns"/> property to the
        /// specified <paramref name="turn"/> index if the latter is greater.</remarks>

        public void AddCommand(Command command, int turn) {
            if (command == null)
                ThrowHelper.ThrowArgumentNullException("command");

            if (turn < FullTurns || turn > FullTurns + 1)
                ThrowHelper.ThrowArgumentOutOfRangeExceptionWithFormat("turn", turn,
                    Tektosyne.Strings.ArgumentLessOrGreater, "FullTurns", "FullTurns + 1");

            // add command to command history
            this._commands.Add(command);

            // adjust turn count if necessary
            if (turn > FullTurns) {
                Debug.Assert(command is EndTurnCommand);
                FullTurns = turn;
            }
        }

        #endregion
        #region AddCommands

        /// <summary>
        /// Adds the <see cref="Commands"/> of the specified <see cref="History"/> to this instance.
        /// </summary>
        /// <param name="history">
        /// The <see cref="History"/> object whose commands to append to this instance.</param>
        /// <returns>
        /// <c>true</c> if the properties of the current <see cref="History"/> were changed;
        /// otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="history"/> is a null reference.</exception>
        /// <exception cref="ArgumentException"><para>
        /// <paramref name="history"/> contains a smaller <see cref="FullTurns"/> index, or a <see
        /// cref="Commands"/> collection with fewer elements, than this instance.
        /// </para><para>-or-</para><para>
        /// <paramref name="history"/> contains the same number of <b>Commands</b> elements but a
        /// greater <b>FullTurns</b> index than this instance.</para></exception>
        /// <remarks><para>
        /// <b>AddCommands</b> does nothing and returns <c>false</c> if the specified <paramref
        /// name="history"/> contains the same <see cref="FullTurns"/> index and the same number of
        /// <see cref="Commands"/> as this instance.
        /// </para><para>
        /// Otherwise, <b>AddCommands</b> sets the <b>FullTurns</b> index to that of the specified
        /// <paramref name="history"/>, adds all <b>Commands</b> elements that <paramref
        /// name="history"/> contains in excess of the current collection, and returns <c>true</c>.
        /// </para></remarks>

        public bool AddCommands(History history) {
            if (history == null)
                ThrowHelper.ThrowArgumentNullException("history");

            if (history.FullTurns < FullTurns)
                ThrowHelper.ThrowArgumentExceptionWithFormat(
                    "history", Tektosyne.Strings.ArgumentPropertyConflict, "FullTurns");

            if (history.Commands.Count < Commands.Count)
                ThrowHelper.ThrowArgumentExceptionWithFormat(
                    "history", Tektosyne.Strings.ArgumentPropertyConflict, "Commands");

            // do nothing if same number of commands
            if (history.Commands.Count == Commands.Count) {

                // sanity check for unchanged turn count
                if (history.FullTurns > FullTurns)
                    ThrowHelper.ThrowArgumentExceptionWithFormat(
                        "history", Tektosyne.Strings.ArgumentPropertyConflict, "FullTurns");

                return false; // history data unchanged
            }

            // copy new turn count
            FullTurns = history.FullTurns;

            // add new commands to collection
            for (int i = Commands.Count; i < history.Commands.Count; i++)
                this._commands.Add(history.Commands[i]);

            return true; // history data has changed
        }

        #endregion
        #region AddEntity

        /// <summary>
        /// Adds a new element for the specified <see cref="Entity"/> to the <see cref="Entities"/>
        /// collection.</summary>
        /// <param name="worldState">
        /// The <see cref="WorldState"/> at the time when the specified <paramref name="entity"/>
        /// was created.</param>
        /// <param name="entity">
        /// The <see cref="Entity"/> whose <see cref="Entity.Id"/> and <see cref="EntityHistory"/>
        /// to add to the <see cref="Entities"/> collection.</param>
        /// <exception cref="ArgumentException">
        /// <see cref="Entities"/> already contains an element for the specified <paramref
        /// name="entity"/>.</exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="worldState"/> or <paramref name="entity"/> is a null reference.
        /// </exception>

        public void AddEntity(WorldState worldState, Entity entity) {

            if (worldState == null)
                ThrowHelper.ThrowArgumentNullException("worldState");
            if (entity == null)
                ThrowHelper.ThrowArgumentNullException("entity");

            this._entities.Add(entity.Id, new EntityHistory(worldState, entity));
        }

        #endregion
        #region AddFaction

        /// <summary>
        /// Adds a new element for the specified <see cref="Faction"/> to the <see cref="Factions"/>
        /// collection.</summary>
        /// <param name="worldState">
        /// The <see cref="WorldState"/> at the time when the specified <paramref name="faction"/>
        /// was created.</param>
        /// <param name="faction">
        /// The <see cref="Faction"/> whose <see cref="Faction.Id"/> and <see
        /// cref="FactionHistory"/> to add to the <see cref="Factions"/> collection.</param>
        /// <exception cref="ArgumentException">
        /// <see cref="Factions"/> already contains an element for the specified <paramref
        /// name="faction"/>.</exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="worldState"/> or <paramref name="faction"/> is a null reference.
        /// </exception>

        public void AddFaction(WorldState worldState, Faction faction) {

            if (worldState == null)
                ThrowHelper.ThrowArgumentNullException("worldState");
            if (faction == null)
                ThrowHelper.ThrowArgumentNullException("faction");

            this._factions.Add(faction.Id, new FactionHistory(worldState, faction));
        }

        #endregion
        #region CopyFullTurns

        /// <summary>
        /// Copies the value of the <see cref="FullTurns"/> property from the specified <see
        /// cref="History"/>.</summary>
        /// <param name="history">
        /// The <see cref="History"/> object whose <see cref="FullTurns"/> property value to copy to
        /// this instance.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="history"/> is a null reference.</exception>

        public void CopyFullTurns(History history) {
            if (history == null)
                ThrowHelper.ThrowArgumentNullException("history");

            FullTurns = history.FullTurns;
        }

        #endregion
        #region HaveBeginTurn

        /// <summary>
        /// Determines whether the <see cref="Commands"/> collection contains a <see
        /// cref="BeginTurnCommand"/> without a subsequent <see cref="EndTurnCommand"/>.</summary>
        /// <returns>
        /// <c>true</c> if the <see cref="Commands"/> collection contains a <see
        /// cref="BeginTurnCommand"/> without a subsequent <see cref="EndTurnCommand"/>; otherwise,
        /// <c>false</c>.</returns>

        public bool HaveBeginTurn() {

            for (int i = Commands.Count - 1; i >= 0; i--) {
                Command command = Commands[i];

                if (command is EndTurnCommand)
                    return false;

                if (command is BeginTurnCommand)
                    return true;
            }

            return false;
        }

        #endregion
        #region ICloneable Members

        /// <summary>
        /// Creates a new <see cref="History"/> object that is a deep copy of the current instance.
        /// </summary>
        /// <returns>
        /// A new <see cref="History"/> object that is a deep copy of the current instance.
        /// </returns>
        /// <remarks><para>
        /// <b>Clone</b> processes the properties of the current instance as follows:
        /// </para><list type="table"><listheader>
        /// <term>Property</term><description>Action</description>
        /// </listheader><item>
        /// <term><see cref="FullTurns"/><br/><see cref="IsInitialized"/></term>
        /// <description>Values copied to the new instance.</description>
        /// </item><item>
        /// <term><see cref="Commands"/></term>
        /// <description>Shallow copy assigned to the new instance.</description>
        /// </item><item>
        /// <term><see cref="Entities"/><br/><see cref="Factions"/></term>
        /// <description>Deep copies assigned to the new instance.</description>
        /// </item></list></remarks>

        public object Clone() {
            return new History(this);
        }

        #endregion
        #region IXmlSerializable Members
        #region ConstXmlName

        /// <summary>
        /// The name of the XML element associated with the <see cref="History"/> class.</summary>
        /// <remarks>
        /// <b>ConstXmlName</b> holds the value "history", indicating the XML element in <see
        /// cref="FilePaths.SessionSchema"/> whose data is managed by the <see cref="History"/>
        /// class.</remarks>

        public const string ConstXmlName = "history";

        #endregion
        #region XmlName

        /// <summary>
        /// Gets the name of the XML element associated with the <see cref="History"/> object.
        /// </summary>
        /// <value>
        /// The value of the constant field <see cref="ConstXmlName"/>.</value>
        /// <remarks>
        /// <b>XmlName</b> specifies the name of the XML element defined in <see
        /// cref="FilePaths.SessionSchema"/> that is expected by <see cref="ReadXml"/> and created
        /// by <see cref="WriteXml"/>.</remarks>

        public string XmlName {
            [DebuggerStepThrough]
            get { return ConstXmlName; }
        }

        #endregion
        #region ReadXml

        /// <summary>
        /// Reads XML data into the <see cref="History"/> object using the specified <see
        /// cref="XmlReader"/>.</summary>
        /// <param name="reader">
        /// The <see cref="XmlReader"/> from which to read.</param>
        /// <exception cref="XmlException">
        /// An error occurred while parsing the XML data provided by <paramref name="reader"/>.
        /// </exception>
        /// <exception cref="System.Xml.Schema.XmlSchemaException">
        /// <paramref name="reader"/> is an <see cref="XmlValidatingReader"/>, and the XML data did
        /// not conform to <see cref="FilePaths.SessionSchema"/>.</exception>
        /// <remarks><para>
        /// <b>ReadXml</b> replaces the data of this <see cref="History"/> object with any matching
        /// data read from the specified <paramref name="reader"/>. Any instance data that <paramref
        /// name="reader"/> fails to supply is left unchanged.
        /// </para><para>
        /// The current node of the specified <paramref name="reader"/> must be either an element
        /// start tag named <see cref="XmlName"/>, or a node from which such a start tag can be
        /// reached by a single call to <see cref="XmlReader.MoveToContent"/>. The provided XML data
        /// is assumed to conform to <see cref="FilePaths.SessionSchema"/>.</para></remarks>

        public void ReadXml(XmlReader reader) {

            XmlUtility.MoveToStartElement(reader, XmlName);
            if (reader.IsEmptyElement) return;

            Assembly assembly = Assembly.GetExecutingAssembly();
            string prefix = "Hexkit.World.Commands.";
            string suffix = "Command";
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;

            while (reader.Read() && reader.IsStartElement()) {

                if (reader.Name == "fullTurns") {
                    // read number of completed full turns
                    FullTurns = XmlConvert.ToInt32(reader.ReadString());
                }
                else {
                    // attempt to instantiate command
                    Command command = (Command) assembly.CreateInstance(
                        prefix + reader.Name + suffix, false, flags, null, null, null, null);

                    if (command != null) {
                        // read command and add to history
                        command.ReadXml(reader);
                        this._commands.Add(command);
                    }
                    else {
                        // skip to end tag of unknown element
                        XmlUtility.MoveToEndElement(reader);
                    }
                }
            }
        }

        #endregion
        #region WriteXml

        /// <summary>
        /// Writes the current data of the <see cref="History"/> object to the specified <see
        /// cref="XmlWriter"/>.</summary>
        /// <param name="writer">
        /// The <see cref="XmlWriter"/> to which to write.</param>
        /// <remarks>
        /// <b>WriteXml</b> writes all data of this <see cref="History"/> object for which <see
        /// cref="FilePaths.SessionSchema"/> defines an XML representation to the specified
        /// <paramref name="writer"/>. The resulting data stream is an XML fragment comprising an
        /// XML element named <see cref="XmlName"/> which conforms to the corresponding element of
        /// <b>SessionSchema</b>.</remarks>

        public void WriteXml(XmlWriter writer) {

            writer.WriteStartElement(XmlName);
            writer.WriteElementString("fullTurns", XmlConvert.ToString(FullTurns));

            foreach (Command command in Commands)
                command.WriteXml(writer);

            writer.WriteEndElement();
        }

        #endregion
        #endregion
    }
}
