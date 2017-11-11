using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml;

using Tektosyne;
using Tektosyne.Collections;
using Tektosyne.Xml;

using Hexkit.Global;
using Hexkit.Scenario;
using Hexkit.World;

namespace Hexkit.Players {
    #region Type Aliases

    using AlgorithmList = KeyedList<String, Algorithm>;
    using ComputerPlayerList = KeyedList<String, ComputerPlayer>;
    using FactionList = KeyedList<String, Faction>;
    using HumanPlayerList = KeyedList<String, HumanPlayer>;

    #endregion

    /// <summary>
    /// Manages player data for a Hexkit game session.</summary>
    /// <remarks><para>
    /// <b>PlayerManager</b> manages all human and computer players of a Hexkit Game session,
    /// including the computer player algorithms. A <see cref="PlayerManager"/> is always associated
    /// with a game session, and its data is stored as part of a Hexkit session description.
    /// </para><para>
    /// Only a single instance of the <b>PlayerManager</b> class can be created at a time. Use <see
    /// cref="PlayerManager.CreateInstance"/> to instantiate the class, <see
    /// cref="PlayerManager.Instance"/> to retrieve the current instance, and <see
    /// cref="PlayerManager.Dispose"/> to delete the instance.
    /// </para><para>
    /// <b>PlayerManager</b> is serialized to the XML element "players" defined in <see
    /// cref="FilePaths.SessionSchema"/>.</para></remarks>

    public class PlayerManager: IDisposable, IXmlSerializable {
        #region PlayerManager()

        /// <summary>
        /// Initializes a new instance of the <see cref="PlayerManager"/> class.</summary>
        /// <remarks><para>
        /// This constructor is private because clients should use the <see cref="CreateInstance"/>
        /// method to instantiate the <see cref="PlayerManager"/> class.
        /// </para><para>
        /// The new instance is not immediately usable. Clients must subsequently invoke either <see
        /// cref="Initialize"/> or <see cref="ReadXml"/> and <see cref="ValidatePlayers"/> on the
        /// new instance to acquire the data of the current <see cref="WorldState"/>.
        /// </para></remarks>

        private PlayerManager() {

            // add one instance of each algorithm
            this._algorithms.Add(new Seeker());
        }

        #endregion
        #region Private Fields

        // property backers
        private readonly ComputerThread _computerThread = new ComputerThread();
        private readonly AlgorithmList _algorithms = new AlgorithmList(true);
        private readonly ComputerPlayerList _computers = new ComputerPlayerList(true);
        private readonly HumanPlayerList _humans = new HumanPlayerList(true);

        private readonly Dictionary<String, Player>
            _factionsToPlayers = new Dictionary<String, Player>();

        #endregion
        #region Algorithms

        /// <summary>
        /// Gets a list of all computer player algorithms.</summary>
        /// <value>
        /// A read-only <see cref="AlgorithmList"/> containing all available <see cref="Algorithm"/>
        /// implementations.</value>
        /// <remarks>
        /// <b>Algorithms</b> never returns a null reference, and its elements are never null
        /// references. The <see cref="Algorithm.Id"/> strings of all elements are unique.</remarks>

        public AlgorithmList Algorithms {
            [DebuggerStepThrough]
            get { return this._algorithms.AsReadOnly(); }
        }

        #endregion
        #region Computers

        /// <summary>
        /// Gets a list of all computer players.</summary>
        /// <value>
        /// A <see cref="ComputerPlayerList"/> defining all computer players. The default is
        /// described in <see cref="Initialize"/>.</value>
        /// <remarks>
        /// <b>Computers</b> never returns a null reference, and its elements are never null
        /// references. The <see cref="Player.DefaultName"/> strings of all elements are unique.
        /// </remarks>

        public ComputerPlayerList Computers {
            [DebuggerStepThrough]
            get { return this._computers; }
        }

        #endregion
        #region ComputerThread

        /// <summary>
        /// Gets the <see cref="Players.ComputerThread"/> that runs computer players for the <see
        /// cref="PlayerManager"/>.</summary>
        /// <value>
        /// The <see cref="Players.ComputerThread"/> that runs computer players for the <see
        /// cref="PlayerManager"/>.</value>
        /// <remarks>
        /// <b>ComputerThread</b> never returns a null reference. This property never changes once
        /// the object has been constructed.</remarks>

        public ComputerThread ComputerThread {
            [DebuggerStepThrough]
            get { return this._computerThread; }
        }

        #endregion
        #region Humans

        /// <summary>
        /// Gets a list of all human players.</summary>
        /// <value>
        /// A <see cref="HumanPlayerList"/> defining all human players. The default is described in
        /// <see cref="Initialize"/>.</value>
        /// <remarks>
        /// <b>Humans</b> never returns a null reference, and its elements are never null
        /// references. The <see cref="Player.DefaultName"/> strings of all elements are unique.
        /// </remarks>

        public HumanPlayerList Humans {
            [DebuggerStepThrough]
            get { return this._humans; }
        }

        #endregion
        #region Instance

        /// <summary>
        /// Gets the current instance of the <see cref="PlayerManager"/> class.</summary>
        /// <value>
        /// The current instance of the <see cref="PlayerManager"/> class if one was successfully
        /// initialized; otherwise, a null reference. The default is a null reference.</value>
        /// <remarks>
        /// <b>Instance</b> is set by the <see cref="CreateInstance"/> method and cleared by the
        /// <see cref="Dispose"/> method.</remarks>

        public static PlayerManager Instance { get; private set; }

        #endregion
        #region CreateInstance

        /// <summary>
        /// Initializes a new <see cref="Instance"/> of the <see cref="PlayerManager"/> class.
        /// </summary>
        /// <exception cref="PropertyValueException">
        /// <see cref="Instance"/> is not a null reference.</exception>
        /// <remarks><para>
        /// <b>CreateInstance</b> sets the <see cref="Instance"/> property to a newly created
        /// instance of the <see cref="PlayerManager"/> class that has been initialized with default
        /// properties.
        /// </para><para>
        /// The new instance is not immediately usable. Clients must subsequently invoke either <see
        /// cref="Initialize"/> or <see cref="ReadXml"/> and <see cref="ValidatePlayers"/> on the
        /// new instance to acquire the data of the current <see cref="WorldState"/>.
        /// </para><para>
        /// Only a single instance of the <b>PlayerManager</b> class can be created at a time.
        /// Calling the <see cref="Dispose"/> method on the current <b>Instance</b> clears this
        /// property and allows the creation of another <b>PlayerManager</b> instance.
        /// </para></remarks>

        public static void CreateInstance() {

            // check for existing instance
            if (Instance != null)
                ThrowHelper.ThrowPropertyValueException(
                    "Instance", Tektosyne.Strings.PropertyNotNull);

            // set singleton reference
            Instance = new PlayerManager();
        }

        #endregion
        #region GetGameMode

        /// <summary>
        /// Gets the <see cref="GameMode"/> implied by the participating human players.</summary>
        /// <returns>
        /// A <see cref="GameMode"/> value that reflects the contents of the <see cref="Humans"/>
        /// collection.</returns>
        /// <remarks><para>
        /// <b>GetGameMode</b> examines all elements in <see cref="Humans"/> whose <see
        /// cref="Player.Factions"/> collection is not empty, and returns one of the following <see
        /// cref="GameMode"/> values:
        /// </para><list type="bullet"><item>
        /// <see cref="GameMode.Single"/> if there is at most one such element.
        /// </item><item>
        /// <see cref="GameMode.Email"/> if there are at least two such elements, and their <see
        /// cref="HumanPlayer.Email"/> addresses are not empty strings.
        /// </item><item>
        /// <see cref="GameMode.Hotseat"/> otherwise.</item></list></remarks>

        public GameMode GetGameMode() {
            int total = 0, empty = 0, valid = 0;

            // count players that control factions
            for (int i = 0; i < Humans.Count; i++) {
                HumanPlayer human = Humans[i];

                if (human.Factions.Count > 0) {
                    ++total;

                    if (human.Email.Length == 0) ++empty;
                    else ++valid;
                }
            }

            // enforced by "Player Setup" dialog
            Debug.Assert(empty == 0 || valid == 0);

            // ignore e-mail in single-player mode
            if (total <= 1) return GameMode.Single;

            // valid addresses imply PBEM mode
            return (valid > 0 ? GameMode.Email : GameMode.Hotseat);
        }

        #endregion
        #region GetLocalHuman

        /// <summary>
        /// Finds the <see cref="Humans"/> element that most likely represents the local human
        /// player.</summary>
        /// <param name="worldState">
        /// The <see cref="WorldState"/> containing the sequence of surviving <see
        /// cref="WorldState.Factions"/>.</param>
        /// <param name="findUser">
        /// <c>true</c> to first look for a <see cref="HumanPlayer"/> whose <see
        /// cref="Player.Name"/> equals the current Windows <see cref="Environment.UserName"/>;
        /// <c>false</c> to skip this step.</param>
        /// <returns>
        /// The <see cref="Humans"/> element that most likely represents the human player at the
        /// local machine.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="worldState"/> is a null reference.</exception>
        /// <exception cref="PropertyValueException">
        /// <see cref="Humans"/> is an empty collection.</exception>
        /// <remarks><para>
        /// <b>GetLocalHuman</b> uses several heuristics to find the <see cref="Humans"/> element
        /// that is most likely to represent the local human player:
        /// </para><list type="number"><item>
        /// If <paramref name="findUser"/> is <c>true</c>, return the first <see cref="Humans"/>
        /// element whose <see cref="Player.Name"/> matches the current <see
        /// cref="Environment.UserName"/>.
        /// </item><item>
        /// Failing that, find the first <see cref="WorldState.Factions"/> element in the specified
        /// <paramref name="worldState"/>, starting at <see cref="WorldState.ActiveFactionIndex"/>,
        /// that is controlled by a <see cref="HumanPlayer"/> object, and return that object.
        /// </item><item>
        /// Failing that, return the first element in the <see cref="Humans"/> collection.
        /// </item></list><para>
        /// Note that the local player is free to use a name different from his current <see
        /// cref="Environment.UserName"/>, and also that a remote player is free to adopt the name
        /// of the local current user.</para></remarks>

        public HumanPlayer GetLocalHuman(WorldState worldState, bool findUser) {
            if (worldState == null)
                ThrowHelper.ThrowArgumentNullException("worldState");
            if (Humans.Count == 0)
                ThrowHelper.ThrowPropertyValueException("Humans", Tektosyne.Strings.PropertyEmpty);

            if (findUser) {
                string userName = Environment.UserName;

                // try to find current user among humans
                for (int i = 0; i < Humans.Count; i++) {
                    HumanPlayer human = Humans[i];
                    Debug.Assert(human != null);
                    if (human.Name == userName)
                        return human;
                }
            }

            // try to find next human to become active
            var factions = worldState.Factions;
            for (int i = 0; i < factions.Count; i++) {
                int index = (worldState.ActiveFactionIndex + i) % factions.Count;
                HumanPlayer human = GetPlayer(factions[index]) as HumanPlayer;
                if (human != null) return human;
            }

            // default to first human player
            Debug.Assert(Humans[0] != null);
            return Humans[0];
        }

        #endregion
        #region GetPlayer

        /// <summary>
        /// Returns the <see cref="Player"/> that controls the specified <see cref="Faction"/>.
        /// </summary>
        /// <param name="faction">
        /// The <see cref="Faction"/> whose controlling player to find.</param>
        /// <returns>
        /// The <see cref="Player"/> whose <see cref="Player.Factions"/> collection contains the
        /// specified <paramref name="faction"/>.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="faction"/> is a null reference.</exception>
        /// <remarks>
        /// <b>GetPlayer</b> returns a null reference if the specified <paramref name="faction"/> is
        /// not associated with any <see cref="Player"/>.</remarks>

        public Player GetPlayer(Faction faction) {
            if (faction == null)
                ThrowHelper.ThrowArgumentNullException("faction");

            Player player;
            this._factionsToPlayers.TryGetValue(faction.Id, out player);
            Debug.Assert(player == null || player.Factions.Contains(faction.Id));
            return player;
        }

        #endregion
        #region GetPlayerSettings

        /// <summary>
        /// Retrieves the current <see cref="PlayerSettings"/> for the specified <see
        /// cref="Faction"/>.</summary>
        /// <param name="faction">
        /// The <see cref="Faction"/> whose <see cref="PlayerSettings"/> to retrieve.</param>
        /// <returns>
        /// The current <see cref="PlayerSettings"/> for the <see cref="Faction"/>.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="faction"/> is a null reference.</exception>
        /// <remarks>
        /// <b>GetPlayerSettings</b> conforms to the <see cref="GetPlayerSettingsCallback"/>
        /// delegate and backs the <see cref="Faction.GetPlayerSettings"/> property of the <see
        /// cref="Faction"/> class.</remarks>

        public PlayerSettings GetPlayerSettings(Faction faction) {
            if (faction == null)
                ThrowHelper.ThrowArgumentNullException("faction");

            ComputerPlayer computer = GetPlayer(faction) as ComputerPlayer;
            bool isComputer = (computer != null);
            bool useScripting = (isComputer ? computer.Options.UseScripting : false);

            return new PlayerSettings(isComputer, useScripting);
        }

        #endregion
        #region Initialize

        /// <summary>
        /// Initializes the <see cref="PlayerManager"/> with the specified factions.</summary>
        /// <param name="factions">
        /// A <see cref="FactionList"/> containing all <see cref="Faction"/> objects to associate
        /// with players.</param>
        /// <exception cref="ArgumentNullOrEmptyException">
        /// <paramref name="factions"/> is a null reference or an empty collection.</exception>
        /// <remarks><para>
        /// For each element in the specified <paramref name="factions"/> collection,
        /// <b>Initialize</b> adds one <see cref="HumanPlayer"/> to the <see cref="Humans"/>
        /// collection and one <see cref="ComputerPlayer"/> to the <see cref="Computers"/>
        /// collection.
        /// </para><para>
        /// Any <paramref name="factions"/> element for which the current scenario defines valid
        /// <see cref="FactionClass.Computer"/> data is assigned to the <see cref="Computers"/>
        /// element at the same index position, which is initialized with that faction's data.
        /// </para><para>
        /// The first <paramref name="factions"/> element without valid <see
        /// cref="FactionClass.Computer"/> data is assigned to the first <see cref="Humans"/>
        /// element, whose <see cref="Player.Name"/> is set to the current Windows <see
        /// cref="Environment.UserName"/>. Any remaining <paramref name="factions"/> elements are
        /// assigned to the <see cref="Computers"/> element at the same index position.
        /// </para><para>
        /// Clients should call <b>Initialize</b> after creating an initial <see cref="WorldState"/>
        /// based on the current scenario.</para></remarks>

        public void Initialize(FactionList factions) {
            if (factions == null || factions.Count == 0)
                ThrowHelper.ThrowArgumentNullOrEmptyException("factions");

            // create human & computer players for all factions
            for (int i = 0; i < factions.Count; i++) {
                Humans.Add(new HumanPlayer(i));
                Computers.Add(new ComputerPlayer(i));

                // map faction to i-th computer player by default
                Faction faction = factions[i];
                Player player = Computers[i];

                // check for valid customized computer settings
                CustomComputer customComputer = faction.FactionClass.Computer;
                bool isValid = customComputer.IsValid &&
                    Algorithms.ContainsKey(customComputer.Algorithm);

                if (isValid) {
                    // create computer player with desired algorithm and options
                    ComputerPlayer computer = (ComputerPlayer) player;
                    computer.Options = AlgorithmOptions.Create(customComputer.Algorithm);
                    computer.Options.Load(customComputer.Options);
                }

                // map faction to first human player if still empty
                if (!customComputer.IsValid && Humans[0].Factions.Count == 0)
                    player = Humans[0];

                // establish assignment in both directions
                player.WritableFactions.Add(faction.Id);
                this._factionsToPlayers[faction.Id] = player;
            }

            // first human player defaults to current user
            Humans[0].Name = Environment.UserName;
        }

        #endregion
        #region SetPlayer

        /// <summary>
        /// Changes the <see cref="Player"/> that controls the specified <see cref="Faction"/>.
        /// </summary>
        /// <param name="faction">
        /// The <see cref="Faction"/> whose controlling <see cref="Player"/> to change.</param>
        /// <param name="player">
        /// The <see cref="Player"/> that should control the specified <paramref name="faction"/>.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="faction"/> or <paramref name="player"/> is a null reference.</exception>
        /// <remarks><para>
        /// <b>SetPlayer</b> removes the specified <paramref name="faction"/> from its current
        /// controlling <see cref="Player"/>, if any, and assigns it to the specified <paramref
        /// name="player"/>.</para></remarks>

        public void SetPlayer(Faction faction, Player player) {
            if (faction == null)
                ThrowHelper.ThrowArgumentNullException("faction");
            if (player == null)
                ThrowHelper.ThrowArgumentNullException("player");

            Player oldPlayer = GetPlayer(faction);
            if (oldPlayer != null)
                oldPlayer.WritableFactions.Remove(faction.Id);

            // establish assignment in both directions
            player.WritableFactions.Add(faction.Id);
            this._factionsToPlayers[faction.Id] = player;
        }

        #endregion
        #region ValidatePlayer

        /// <summary>
        /// Validates the specified <see cref="Player"/> against the specified factions.</summary>
        /// <param name="player">
        /// The <see cref="Player"/> to validate.</param>
        /// <param name="factions">
        /// A <see cref="FactionList"/> containing the <see cref="Faction"/> objects to validate
        /// against.</param>
        /// <exception cref="XmlException">
        /// Validation failed.</exception>
        /// <remarks>
        /// Please refer to <see cref="ValidatePlayers"/> for a description of this method.
        /// </remarks>

        private void ValidatePlayer(Player player, FactionList factions) {

            // set references for all faction IDs
            for (int i = 0; i < player.Factions.Count; i++) {
                string id = player.Factions[i];

                // check for nonexistent faction
                Faction faction;
                if (!factions.TryGetValue(id, out faction))
                    ThrowHelper.ThrowXmlExceptionWithFormat(Global.Strings.XmlControlsInvalid, id);

                // check if controlled by another player
                if (GetPlayer(faction) != null)
                    ThrowHelper.ThrowXmlExceptionWithFormat(Global.Strings.XmlControlsDuplicate, id);

                // cache player assignment
                this._factionsToPlayers[id] = player;
            }
        }

        #endregion
        #region ValidatePlayers

        /// <summary>
        /// Validates all players against the specified factions.</summary>
        /// <param name="factions">
        /// A <see cref="FactionList"/> containing the <see cref="Faction"/> objects to validate
        /// against.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="factions"/> is a null reference.</exception>
        /// <exception cref="XmlException">
        /// Validation failed.</exception>
        /// <remarks><para>
        /// <b>ValidatePlayers</b> checks the <see cref="Player.Factions"/> collections of all <see
        /// cref="Player"/> objects in the <see cref="Humans"/> and <see cref="Computers"/>
        /// collections for nonexistent and duplicate identifiers of elements in the specified
        /// <paramref name="factions"/> collection.
        /// </para><para>
        /// Clients should call <b>ValidatePlayers</b> after restoring the data of this <see
        /// cref="PlayerManager"/> object and its associated <see cref="WorldState"/> from an XML
        /// session description.</para></remarks>

        public void ValidatePlayers(FactionList factions) {
            if (factions == null)
                ThrowHelper.ThrowArgumentNullException("factions");

            foreach (Player player in Humans)
                ValidatePlayer(player, factions);

            foreach (Player player in Computers)
                ValidatePlayer(player, factions);
        }

        #endregion
        #region IDisposable Members

        /// <summary>
        /// Releases all resources used by the <see cref="PlayerManager"/> object.</summary>
        /// <remarks><para>
        /// <b>Dispose</b> resets the <see cref="Instance"/> property to a null reference, allowing
        /// the creation of another instance of the <see cref="PlayerManager"/> class.
        /// </para><para>
        /// <b>Dispose</b> also disposes of the <see cref="ComputerThread"/> object, thereby
        /// stopping any ongoing background calculations.</para></remarks>

        public void Dispose() {

            // guaranteed by CreateInstance
            Debug.Assert(Instance == this);

            // clear singleton reference
            Instance = null;

            // dispose of thread manager
            if (this._computerThread != null)
                this._computerThread.Dispose();
        }

        #endregion
        #region IXmlSerializable Members
        #region ConstXmlName

        /// <summary>
        /// The name of the XML element associated with the <see cref="PlayerManager"/> class.
        /// </summary>
        /// <remarks>
        /// <b>ConstXmlName</b> holds the value "players", indicating the XML element in <see
        /// cref="FilePaths.SessionSchema"/> whose data is managed by the <see
        /// cref="PlayerManager"/> class.</remarks>

        public const string ConstXmlName = "players";

        #endregion
        #region XmlName

        /// <summary>
        /// Gets the name of the XML element associated with the <see cref="PlayerManager"/> object.
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
        /// Reads XML data into the <see cref="PlayerManager"/> object using the specified <see
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
        /// <b>ReadXml</b> replaces the data of this <see cref="PlayerManager"/> object with any
        /// matching data read from the specified <paramref name="reader"/>. Any instance data that
        /// <paramref name="reader"/> fails to supply is left unchanged.
        /// </para><para>
        /// The current node of the specified <paramref name="reader"/> must be either an element
        /// start tag named <see cref="XmlName"/>, or a node from which such a start tag can be
        /// reached by a single call to <see cref="XmlReader.MoveToContent"/>. The provided XML data
        /// is assumed to conform to <see cref="FilePaths.SessionSchema"/>.</para></remarks>

        public void ReadXml(XmlReader reader) {

            XmlUtility.MoveToStartElement(reader, XmlName);
            if (reader.IsEmptyElement) return;

            while (reader.Read() && reader.IsStartElement()) {
                switch (reader.Name) {

                    case HumanPlayer.ConstXmlName:
                        HumanPlayer human = new HumanPlayer(Humans.Count);
                        human.ReadXml(reader);
                        Humans.Add(human);
                        break;

                    case ComputerPlayer.ConstXmlName:
                        ComputerPlayer computer = new ComputerPlayer(Computers.Count);
                        computer.ReadXml(reader);
                        Computers.Add(computer);
                        break;

                    case Algorithm.ConstXmlName:
                        string id = reader["id"];
                        if (!String.IsNullOrEmpty(id)) {
                            Algorithm algorithm;

                            // get algorithm with matching identifier
                            if (!Algorithms.TryGetValue(id, out algorithm))
                                ThrowHelper.ThrowXmlExceptionWithFormat(
                                    Global.Strings.XmlAlgorithmInvalid, Algorithm.ConstXmlName, id);

                            algorithm.ReadXml(reader);
                        }
                        break;

                    default:
                        // skip to end tag of unknown element
                        XmlUtility.MoveToEndElement(reader);
                        break;
                }
            }
        }

        #endregion
        #region WriteXml

        /// <summary>
        /// Writes the current data of the <see cref="PlayerManager"/> object to the specified <see
        /// cref="XmlWriter"/>.</summary>
        /// <param name="writer">
        /// The <see cref="XmlWriter"/> to which to write.</param>
        /// <remarks>
        /// <b>WriteXml</b> writes all data of this <see cref="PlayerManager"/> object for which
        /// <see cref="FilePaths.SessionSchema"/> defines an XML representation to the specified
        /// <paramref name="writer"/>. The resulting data stream is an XML fragment comprising an
        /// XML element named <see cref="XmlName"/> which conforms to the corresponding element of
        /// <b>SessionSchema</b>.</remarks>

        public void WriteXml(XmlWriter writer) {
            writer.WriteStartElement(XmlName);

            foreach (HumanPlayer human in Humans)
                human.WriteXml(writer);

            foreach (ComputerPlayer computer in Computers)
                computer.WriteXml(writer);

            foreach (Algorithm algorithm in Algorithms)
                algorithm.WriteXml(writer);

            writer.WriteEndElement();
        }

        #endregion
        #endregion
    }
}
