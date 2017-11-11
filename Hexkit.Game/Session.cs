using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Windows;
using System.Xml;
using System.Xml.Schema;

using Tektosyne;
using Tektosyne.Geometry;
using Tektosyne.IO;
using Tektosyne.Net;
using Tektosyne.Windows;
using Tektosyne.Xml;

using Hexkit.Global;
using Hexkit.Graphics;
using Hexkit.Players;
using Hexkit.Scenario;
using Hexkit.World;
using Hexkit.World.Commands;

namespace Hexkit.Game {

    /// <summary>
    /// Manages a game session in the Hexkit Game application.</summary>
    /// <remarks><para>
    /// <b>Session</b> manages all data associated with a Hexkit game session, and also controls XML
    /// serialization of Hexkit session descriptions. Session data includes the following items:
    /// </para><list type="bullet"><item>
    /// The scenario on which a game is based, represented by the current <see
    /// cref="MasterSection"/> instance.
    /// </item><item>
    /// The player and faction setup, represented by the current <see cref="PlayerManager"/>
    /// instance.
    /// </item><item>
    /// The current state of the game world, represented by the <see cref="Session.WorldState"/>
    /// property.
    /// </item><item>
    /// Facilities to respond to user input, such as the interactive <see cref="Session.Replay"/>
    /// manager.
    /// </item><item>
    /// Some auxiliary data about the current session state.
    /// </item></list><para>
    /// <b>Session</b> handles tasks such as starting and resuming games, processing input by the
    /// local human player, and dispatching control to the next active player.
    /// </para><para>
    /// Only a single instance of the <b>Session</b> class can be created at a time. Use <see
    /// cref="Session.Start"/> or <see cref="Session.Resume"/> to instantiate the class, <see
    /// cref="Session.Instance"/> to retrieve the current instance, and <see cref="Session.Close"/>
    /// or <see cref="Session.Dispose"/> to delete the instance.
    /// </para><para>
    /// <b>Session</b> is serialized to the XML element "session" defined in <see
    /// cref="FilePaths.ScenarioSchema"/>. This is the root node of a Hexkit session description.
    /// </para></remarks>

    public class Session: IDisposable, IXmlSerializable {
        #region Session()

        /// <summary>
        /// Initializes a new instance of the <see cref="Session"/> class.</summary>
        /// <remarks>
        /// This constructor is private because clients should use the <see cref="Start"/> and <see
        /// cref="Resume"/> methods to instantiate the <see cref="Session"/> class.</remarks>

        private Session() {

            // prepare player manager
            PlayerManager.CreateInstance();

            // allow factions to retrieve their player settings
            Faction.GetPlayerSettings = PlayerManager.Instance.GetPlayerSettings;
        }

        #endregion
        #region Private Fields

        // property backers
        private readonly SessionExecutor _executor = new SessionExecutor();
        private readonly ReplayManager _replayManager = new ReplayManager();
        private readonly TargetSelection _targetSelection = new TargetSelection();

        private string _localEmail, _scenarioHash, _scenarioPath;
        private static SessionState _state = SessionState.Invalid;
        private WorldState _worldState = new WorldState();

        #endregion
        #region Public Properties
        #region Executor

        /// <summary>
        /// Gets the command executor for the <see cref="Session"/>.</summary>
        /// <value>
        /// The <see cref="SessionExecutor"/> that manages command execution for the <see
        /// cref="Session"/>.</value>
        /// <remarks>
        /// <b>Executor</b> never returns a null reference. This property never changes once the
        /// object has been constructed.</remarks>

        public SessionExecutor Executor {
            [DebuggerStepThrough]
            get { return this._executor; }
        }

        #endregion
        #region GameWasOver

        /// <summary>
        /// Gets a value indicating whether the <see cref="Session"/> is based on a saved game that
        /// had already ended before it was saved.</summary>
        /// <value>
        /// <c>true</c> if the associated <see cref="WorldState"/> is based on a saved game whose
        /// <see cref="World.WorldState.GameOver"/> property was <c>true</c> before it had been
        /// saved; otherwise, <c>false</c>. The default is <c>false</c>.</value>
        /// <remarks>
        /// <b>GameWasOver</b> does not change once it has been set to <c>true</c>. This property
        /// set by the <see cref="Resume"/> method. Its value is unrelated to that of the <see
        /// cref="World.WorldState.GameOver"/> property of the current <see cref="WorldState"/>.
        /// </remarks>

        public bool GameWasOver { get; private set; }

        #endregion
        #region Instance

        /// <summary>
        /// Gets the current instance of the <see cref="Session"/> class.</summary>
        /// <value>
        /// The current instance of the <see cref="Session"/> class if one was successfully
        /// initialized and has not yet been disposed of; otherwise, a null reference. The default
        /// is a null reference.</value>
        /// <remarks>
        /// <b>Instance</b> is set by the <see cref="Start"/> and <see cref="Resume"/> methods, and
        /// cleared by the <see cref="Close"/> and <see cref="Dispose"/> methods.</remarks>

        public static Session Instance { get; private set; }

        #endregion
        #region LocalEmail

        /// <summary>
        /// Gets or sets the e-mail address of the local human player.</summary>
        /// <value>
        /// The e-mail address of the local human player. The default is an empty string, indicating
        /// "hotseat" mode.</value>
        /// <remarks><para>
        /// <b>LocalEmail</b> returns an empty string when set to a null reference.
        /// </para><para>
        /// This property is set automatically by <see cref="Start"/> and <see cref="Resume"/>, and
        /// whenever the user changes player data in the <see cref="Dialog.ChangePlayers"/> dialog.
        /// </para></remarks>

        public string LocalEmail {
            [DebuggerStepThrough]
            get { return this._localEmail ?? ""; }
            [DebuggerStepThrough]
            set { this._localEmail = value; }
        }

        #endregion
        #region MapView

        /// <summary>
        /// Gets the default <see cref="Graphics.MapView"/> for the <see cref="Session"/>.</summary>
        /// <value>
        /// The default <see cref="Graphics.MapView"/> for the associated <see cref="WorldState"/>.
        /// </value>
        /// <remarks><para>
        /// <b>MapView</b> returns the element of the <see cref="MapViewManager.MapViews"/>
        /// collection of the current <see cref="MapViewManager"/> instance whose key is "default".
        /// </para><para>
        /// <b>MapView</b> returns a null reference if the current <see cref="MapViewManager"/>
        /// instance is a null reference, or if the default map view has not been created yet.
        /// </para></remarks>

        public static MapView MapView {
            [DebuggerStepThrough]
            get {
                MapViewManager manager = MapViewManager.Instance;
                if (manager == null) return null;

                MapView mapView;
                manager.MapViews.TryGetValue("default", out mapView);
                return mapView;
            }
        }

        #endregion
        #region Replay

        /// <summary>
        /// Gets the <see cref="ReplayManager"/> for the <see cref="Session"/>.</summary>
        /// <value>
        /// The <see cref="ReplayManager"/> that manages interactive command replay for the <see
        /// cref="Session"/>.</value>
        /// <remarks>
        /// <b>Replay</b> never returns a null reference. This property never changes once the
        /// object has been constructed.</remarks>

        public ReplayManager Replay {
            [DebuggerStepThrough]
            get { return this._replayManager; }
        }

        #endregion
        #region ScenarioHash

        /// <summary>
        /// Gets the scenario hash code last read from a session description file.</summary>
        /// <value>
        /// The <see cref="MasterSection.Hash"/> string that was stored in the XML session
        /// description file read by the last call to <see cref="Resume"/>. The default is an empty
        /// string.</value>
        /// <remarks>
        /// <b>ScenarioHash</b> never returns a null reference.</remarks>

        public string ScenarioHash {
            [DebuggerStepThrough]
            get { return this._scenarioHash ?? ""; }
        }

        #endregion
        #region ScenarioPath

        /// <summary>
        /// Gets the scenario file path last read from a session description file.</summary>
        /// <value>
        /// The <see cref="MasterSection.Path"/> string that was stored in the XML session
        /// description file read by the last call to <see cref="Resume"/>. The default is an empty
        /// string.</value>
        /// <remarks>
        /// <b>ScenarioPath</b> never returns a null reference.</remarks>

        public string ScenarioPath {
            [DebuggerStepThrough]
            get { return this._scenarioPath ?? ""; }
        }

        #endregion
        #region SelectedSite

        /// <summary>
        /// Gets the <see cref="Graphics.MapView.SelectedSite"/> of the default <see
        /// cref="MapView"/>.</summary>
        /// <value>
        /// The <see cref="Site"/> at the coordinates of the <see
        /// cref="Graphics.MapView.SelectedSite"/> in the default <see cref="MapView"/>, if any;
        /// otherwise, a null reference.</value>

        public Site SelectedSite {
            get {
                MapView mapView = MapView;
                return (mapView == null ? null : WorldState.GetSite(mapView.SelectedSite));
            }
        }

        #endregion
        #region State

        /// <summary>
        /// Gets or sets the current <see cref="SessionState"/>.</summary>
        /// <value>
        /// A <see cref="SessionState"/> value indicating the current state of the <see
        /// cref="Session"/> class. The default is <see cref="SessionState.Invalid"/>.</value>
        /// <remarks>
        /// Setting the <b>State</b> property raises the <see cref="StateChanged"/> event if any
        /// listeners are attached. The current <see cref="Instance"/> is transmitted as the sender.
        /// </remarks>

        public static SessionState State {
            [DebuggerStepThrough]
            get { return Session._state; }
            set {
                Session._state = value;

                // broadcast state change to listeners
                if (StateChanged != null)
                    StateChanged(Session.Instance, EventArgs.Empty);
            }
        }

        #endregion
        #region StateChanged

        /// <summary>
        /// Occurs when the session <see cref="State"/> is changed.</summary>
        /// <remarks>
        /// <b>StateChanged</b> is raised when the <see cref="State"/> property of the <see
        /// cref="Session"/> class changes. The current session <see cref="Instance"/> is
        /// transmitted as the sender.</remarks>

        public static event EventHandler StateChanged;

        #endregion
        #region TargetSelection

        /// <summary>
        /// Gets the <see cref="Game.TargetSelection"/> manager for the <see cref="Session"/>.
        /// </summary>
        /// <value>
        /// The <see cref="Game.TargetSelection"/> object that manages interactive target selection
        /// for the <see cref="Session"/>.</value>
        /// <remarks>
        /// <b>TargetSelection</b> never returns a null reference. This property never changes once
        /// the object has been constructed.</remarks>

        public TargetSelection TargetSelection {
            [DebuggerStepThrough]
            get { return this._targetSelection; }
        }

        #endregion
        #region WorldChanged

        /// <summary>
        /// Gets a value indicating whether the current <see cref="WorldState"/> holds unsaved
        /// changes.</summary>
        /// <value>
        /// <c>true</c> if any data of the associated <see cref="WorldState"/> has changed since the
        /// last successful <see cref="Start"/>, <see cref="Resume"/>, or <see cref="Save"/>
        /// operation; otherwise, <c>false</c>.</value>

        public bool WorldChanged { get; private set; }

        #endregion
        #region WorldState

        /// <summary>
        /// Gets or sets the current <see cref="World.WorldState"/> for the <see cref="Session"/>.
        /// </summary>
        /// <value>
        /// The <see cref="World.WorldState"/> that manages all game world data for the <see
        /// cref="Session"/>. The default is an empty <see cref="World.WorldState"/>.</value>
        /// <exception cref="ArgumentNullException">
        /// The property is set to a null reference.</exception>
        /// <remarks>
        /// <b>WorldState</b> never returns a null reference.</remarks>

        public WorldState WorldState {
            [DebuggerStepThrough]
            get { return this._worldState; }
            [DebuggerStepThrough]
            set {
                if (value == null)
                    ThrowHelper.ThrowArgumentNullException("value");

                this._worldState = value;
            }
        }

        #endregion
        #endregion
        #region Private Methods
        #region CheckGameOver

        /// <summary>
        /// Checks the <see cref="WorldState"/> for victory and defeat conditions.</summary>
        /// <returns>
        /// <c>true</c> if <see cref="State"/> is <see cref="SessionState.Closed"/>; otherwise,
        /// <c>false</c>.</returns>
        /// <remarks><para>
        /// <b>CheckGameOver</b> examines the <see cref="World.WorldState.GameOver"/> property of
        /// the associated <see cref="WorldState"/> to determine whether the game has ended.
        /// </para><para>
        /// If so, the <see cref="State"/> property is set to <see cref="SessionState.Closed"/>, a
        /// victory message is shown, and the current <see cref="WorldState"/> is saved and sent to
        /// all remote human players for examination.</para></remarks>

        private bool CheckGameOver() {
            if (!WorldState.GameOver) return false;

            State = SessionState.Closed;

            // select site associated with winning faction
            Faction faction = WorldState.WinningFaction;
            PointI factionSite = Finder.FindFactionSite(null, faction);
            MapView.CenterAndSelect(factionSite);

            /*
             * If GameWasOver is true, we just silently replayed a game that
             * was already finished, so we just show the victory dialog.
             *
             * If GameWasOver is false, we just interactively finished a game
             * on the local system, so we must now broadcast the situation to
             * all remote human players after showing the victory dialog.
             *
             * Saving the game stores a GameOver value of true, which in turn
             * sets GameWasOver to true on the recipients' systems, preventing
             * them from broadcasting the completed game again.
             */

            GameUtility.ShowWinner(faction);
            if (!GameWasOver) SendEmailGame(null);

            return true;
        }

        #endregion
        #region IsRemotePlayer

        /// <summary>
        /// Determines whether the specified <see cref="HumanPlayer"/> resides at a remote system.
        /// </summary>
        /// <param name="human">
        /// The <see cref="HumanPlayer"/> to examine.</param>
        /// <returns>
        /// <c>true</c> if the <see cref="HumanPlayer.Email"/> address of the specified <paramref
        /// name="human"/> player indicates a remote system; otherwise, <c>false</c>.</returns>
        /// <remarks>
        /// <b>IsRemotePlayer</b> returns <c>true</c> exactly if the <see cref="HumanPlayer.Email"/>
        /// address of the specified <paramref name="human"/> player is not an empty string and
        /// differs from the current <see cref="LocalEmail"/> address.</remarks>

        private bool IsRemotePlayer(HumanPlayer human) {
            return (human.Email.Length > 0 && human.Email != LocalEmail);
        }

        #endregion
        #region SelectFactionUnit

        /// <summary>
        /// Selects an active <see cref="Unit"/> on a <see cref="Site"/> associated with the
        /// specified <see cref="Faction"/>.</summary>
        /// <param name="faction">
        /// The <see cref="Faction"/> whose <see cref="Site"/> and <see cref="Unit"/> to select.
        /// </param>
        /// <remarks><para>
        /// <b>SelectFactionUnit</b> first calls <see cref="Finder.FindFactionSite"/> to obtain a
        /// <see cref="Site"/> associated with the specified <paramref name="faction"/>, and sets
        /// the <see cref="Graphics.MapView.SelectedSite"/> of the default <see cref="MapView"/> to
        /// that <see cref="Site"/> if different.
        /// </para><para>
        /// <b>SelectFactionUnit</b> then checks if the <see cref="Graphics.MapView.SelectedSite"/>
        /// is valid and contains a <see cref="Unit"/> that belongs to the specified <paramref
        /// name="faction"/> and that has valid targets for an <see cref="AttackCommand"/> or a <see
        /// cref="MoveCommand"/>.
        /// </para><para>
        /// If so, and the <see cref="MainWindow.SelectedEntity"/> does not equal such a <see
        /// cref="Unit"/>, <b>SelectFactionUnit</b> sets the <see cref="MainWindow.SelectedEntity"/>
        /// to the first such <see cref="Unit"/>.</para></remarks>

        private void SelectFactionUnit(Faction faction) {

            // select faction site if necessary
            PointI factionSite = Finder.FindFactionSite(WorldState, faction);
            if (factionSite != MapView.SelectedSite)
                MapView.CenterAndSelect(factionSite);

            // get currently selected entity
            string selectedEntityId = MainWindow.Instance.SelectedEntity;
            Entity activeUnit = null;

            // find first or matching active unit
            if (MapView.SelectedSite != Site.InvalidLocation) {
                Site selectedSite = WorldState.GetSite(MapView.SelectedSite);

                // prepare single-unit collection
                List<Entity> units = new List<Entity>(1) { null };

                for (int i = 0; i < selectedSite.Units.Count; i++) {
                    units[0] = selectedSite.Units[i];

                    // check for owned unit with valid targets
                    if (units[0].Owner == faction &&
                        (Finder.FindAttackTargets(WorldState, units).Count > 0
                        || Finder.FindMoveTargets(WorldState, units).Count > 0)) {

                        // quit if active unit already selected
                        if (selectedEntityId == units[0].Id)
                            return;

                        // remember first active unit
                        if (activeUnit == null) activeUnit = units[0];
                    }
                }
            }

            // select first active unit if no match found
            if (activeUnit != null)
                MainWindow.Instance.SelectedEntity = activeUnit.Id;
        }

        #endregion
        #region SendEmailGame

        /// <summary>
        /// Creates and sends a PBEM file to the active human player(s).</summary>
        /// <param name="recipient"><para>
        /// The <see cref="HumanPlayer"/> that is the recipient of the PBEM file.
        /// </para><para>-or-</para><para>
        /// A null reference to indicate that the PBEM file should be sent to all remote <see
        /// cref="PlayerManager.Humans"/>.</para></param>
        /// <returns>
        /// <c>true</c> if the PBEM file was created and dispatched successfully; otherwise,
        /// <c>false</c>.</returns>
        /// <remarks><para>
        /// <b>SendEmailGame</b> saves the current <see cref="WorldState"/> to the predefined <see
        /// cref="SessionFileType.Email"/> session file, and then creates an e-mail message with
        /// this file attached.
        /// </para><para>
        /// If the specified <paramref name="recipient"/> is valid, the message is sent only to that
        /// <see cref="HumanPlayer"/>, along with a notice indicating the active faction and player.
        /// </para><para>
        /// If <paramref name="recipient"/> is a null reference, the message is broadcast to all
        /// remote <see cref="PlayerManager.Humans"/>, along with a notice that the game has ended.
        /// If there are no remote human players, <b>SendEmailGame</b> immediately returns
        /// <c>true</c> without saving the game or sending a message.</para></remarks>

        private bool SendEmailGame(HumanPlayer recipient) {

            // subject shows scenario title & turn count
            string subject = String.Format(ApplicationInfo.Culture, Global.Strings.InfoTitleTurn,
                MasterSection.Instance.Title, WorldState.CurrentTurn + 1);

            // text shows note on attachment
            string text = Global.Strings.InfoPlayerMail;

            // determine recipient(s)
            List<MapiAddress> recips = new List<MapiAddress>();
            if (recipient == null) {

                // all remote human players are recipients
                foreach (HumanPlayer human in PlayerManager.Instance.Humans)
                    if (IsRemotePlayer(human))
                        recips.Add(new MapiAddress(human.Name, human.Email));

                // succeed immediately if no recipients
                if (recips.Count == 0) return true;

                // add "Game Over" notice
                text += Global.Strings.InfoPlayerMailFinal;
            }
            else {
                // use specified recipient only
                recips.Add(new MapiAddress(recipient.Name, recipient.Email));

                // add "Active Faction/Player" notice
                text += String.Format(ApplicationInfo.Culture, Global.Strings.InfoPlayerMailPlay,
                    WorldState.ActiveFaction.Name, recipient.Name);
            }

            // attempt to create PBEM file
            string path = FilePaths.GetSessionFile(SessionFileType.Email).AbsolutePath;
            if (!Save(ref path, false)) return false;

            // add file as an attachment
            MapiAddress[] files = { new MapiAddress(Path.GetFileName(path), path) };

            try {
                // try sending PBEM file to remote player(s)
                MapiMail.SendMail(subject, text, recips.ToArray(), files);
            }
            catch (Exception e) {
                // quit if user cancelled MAPI session
                MapiException me = e as MapiException;
                if (me != null && me.Code == MapiException.Abort)
                    return false;

                // report exception message to the user
                MessageDialog.Show(MainWindow.Instance,
                    Global.Strings.DialogMailError, Global.Strings.TitleMailError,
                    e, MessageBoxButton.OK, Images.Error);

                return false;
            }

            return true;
        }

        #endregion
        #endregion
        #region Public Methods
        #region ChangePlayers

        /// <summary>
        /// Shows the "Player Setup" dialog for an active <see cref="Session"/>.</summary>
        /// <remarks><para>
        /// <b>ChangePlayers</b> shows the <see cref="Dialog.ChangePlayers"/> dialog. If the user
        /// made any changes, <b>ChangePlayers</b> sets the  <see cref="WorldChanged"/> flag and
        /// recalculates the <see cref="LocalEmail"/> property to reflect the changes.
        /// </para><para>
        /// If the user changed the player controlling the active faction, <b>ChangePlayers</b> asks
        /// if the faction should switch players immediately. If the user confirms and <see
        /// cref="Dispatch"/> transfers control to a remote human player, <see cref="Close"/> will
        /// be called without confirmation.</para></remarks>

        public void ChangePlayers() {

            //  get active faction's current player
            PlayerManager players = PlayerManager.Instance;
            Player oldPlayer = players.GetPlayer(WorldState.ActiveFaction);

            // show "Player Setup" dialog
            var dialog = new Dialog.ChangePlayers() { Owner = MainWindow.Instance };
            dialog.ShowDialog();
            if (!dialog.DataChanged) return;

            // session data has changed
            WorldChanged = true;

            // update LocalEmail with possible changes
            HumanPlayer human = players.GetLocalHuman(WorldState, true);
            LocalEmail = human.Email;

            // check if active faction's player changed
            Player newPlayer = players.GetPlayer(WorldState.ActiveFaction);
            if (oldPlayer != newPlayer) {

                string message = String.Format(ApplicationInfo.Culture,
                    Global.Strings.DialogPlayerChanged, newPlayer.Name, oldPlayer.Name);

                string caption = String.Format(ApplicationInfo.Culture,
                    Global.Strings.TitlePlayerChanged, WorldState.ActiveFaction.Name);

                // ask user to switch players immediately
                MessageBoxResult result = MessageBox.Show(MainWindow.Instance,
                    message, caption, MessageBoxButton.YesNo, MessageBoxImage.Question);

                // dispatch to new player if confirmed
                if (result == MessageBoxResult.Yes) {
                    UserAction.Clear();

                    // stop computer player if still active
                    if (State == SessionState.Computer)
                        players.ComputerThread.Stop();

                    Dispatch();
                }
            }
        }

        #endregion
        #region Close

        /// <summary>
        /// Closes the current <see cref="Session"/> after optionally asking for user confirmation.
        /// </summary>
        /// <param name="confirm">
        /// <c>true</c> to ask the user for confirmation if the current <see cref="Session"/>
        /// contains unsaved changes; otherwise, <c>false</c>.</param>
        /// <returns>
        /// <c>true</c> if the current <see cref="Session"/> was closed; otherwise, <c>false</c>.
        /// </returns>
        /// <remarks><para>
        /// <b>Close</b> returns <c>true</c> if there is no current <see cref="Session"/>; if
        /// <paramref name="confirm"/> is <c>false</c>; if the associated <see cref="WorldState"/>
        /// did not contain any unsaved changes; or if the user agreed to lose these changes.
        /// </para><para>
        /// On success, and if there was a current <see cref="Session"/>, <b>Close</b> calls <see
        /// cref="Dispose"/> which resets the <see cref="Instance"/> property to a null reference,
        /// allowing the creation of another instance of the <see cref="Session"/> class.
        /// </para><para>
        /// A return value of <c>false</c> indicates that <paramref name="confirm"/> is <c>true</c>
        /// and there were unsaved changes which the user declined to lose. The current <see
        /// cref="Instance"/> will remain valid in this case.</para></remarks>

        public static bool Close(bool confirm) {

            // succeed if no session active
            if (State == SessionState.Invalid)
                return true;

            Session session = Session.Instance;
            if (confirm && session.WorldChanged) {

                // ask for confirmation if changes unsaved
                MessageBoxResult result = MessageBox.Show(MainWindow.Instance,
                    Global.Strings.DialogGameUnsaved, Global.Strings.TitleGameUnsaved,
                    MessageBoxButton.OKCancel, MessageBoxImage.Question, MessageBoxResult.Cancel);

                if (result == MessageBoxResult.Cancel)
                    return false;
            }

            // clean up and succeed
            session.Dispose();
            return true;
        }

        #endregion
        #region Create

        /// <summary>
        /// Creates a new <see cref="Session"/> based on the specified scenario XML file.</summary>
        /// <param name="path">
        /// An optional path to the scenario XML file to open.</param>
        /// <remarks><para>
        /// <b>Create</b> performs the following actions:
        /// </para><list type="number"><item>
        /// Call <see cref="Close"/> to close the current <see cref="Session"/>, asking the user for
        /// confirmation if the <see cref="WorldState"/> contains unsaved data.
        /// </item><item>
        /// Call <see cref="Start"/> with the specified <paramref name="path"/>, asking the user for
        /// a scenario XML file to open if <paramref name="path"/> is not a valid string.
        /// </item><item>
        /// Show an <see cref="Dialog.AboutScenario"/> dialog and a <see
        /// cref="Dialog.ChangePlayers"/> dialog for the new <see cref="Session"/>.
        /// </item><item>
        /// Call <see cref="Dispatch"/> on the new <see cref="Instance"/> and then immediately <see
        /// cref="Close"/> if the <see cref="Session"/> does not continue on the local system.
        /// </item></list></remarks>

        public static void Create(string path) {

            // ask user to close session
            if (!Close(true)) return;

            // attempt to create game session
            if (!Start(path)) return;

            // show "About Scenario" dialog
            Window dialog = new Dialog.AboutScenario() { Owner = MainWindow.Instance };
            dialog.ShowDialog();

            /*
             * Technically we should mark the session as changed if the user changed player
             * assignments or properties, but we assume that the user doesn't want to play
             * anyway if he quits after this dialog without doing anything else.
             */

            // show "Player Setup" dialog
            dialog = new Dialog.ChangePlayers() { Owner = MainWindow.Instance };
            dialog.ShowDialog();

            // dispatch game to first player
            Instance.Dispatch();
        }

        #endregion
        #region Dispatch

        /// <summary>
        /// Dispatches the game to the player controlling the <see
        /// cref="World.WorldState.ActiveFaction"/>.</summary>
        /// <exception cref="InvalidOperationException">
        /// <see cref="PlayerManager.GetGameMode"/> returned an invalid <see cref="GameMode"/>
        /// value.</exception>
        /// <remarks><para>
        /// <b>Dispatch</b> performs the following actions:
        /// </para><list type="number"><item>
        /// Clear the <see cref="HumanAction.SkippedUnits"/> collection of the <see
        /// cref="HumanAction"/> class.
        /// </item><item>
        /// Center the <see cref="MapView"/> on the result of <see cref="Finder.FindFactionSite"/>
        /// for the <see cref="World.WorldState.ActiveFaction"/>.
        /// </item><item>
        /// If the <see cref="World.WorldState.GameOver"/> property of the current <see
        /// cref="WorldState"/> is <c>true</c>, set <see cref="State"/> to <see
        /// cref="SessionState.Closed"/>, and notify any remote human players.
        /// </item><item>
        /// If the <see cref="World.WorldState.ActiveFaction"/> is controlled by a computer player,
        /// start the <see cref="PlayerManager.ComputerThread"/>.
        /// </item><item>
        /// If the <see cref="World.WorldState.ActiveFaction"/> is controlled by a local human
        /// player, issue a <see cref="BeginTurnCommand"/> and check <b>GameOver</b> again to see if
        /// the faction just won the game.
        /// </item><item>
        /// If the <see cref="World.WorldState.ActiveFaction"/> is controlled by a remote human
        /// player, dispatch a PBEM message and call <see cref="Close"/> without confirmation.
        /// </item></list><para>
        /// <b>Dispatch</b> also calls <see cref="Close"/> without confirmation if an error occurs.
        /// </para></remarks>

        public void Dispatch() {

            // restart unit cycle each turn
            HumanAction.SkippedUnits.Clear();

            // select site associated with active faction
            Faction activeFaction = WorldState.ActiveFaction;
            PointI factionSite = Finder.FindFactionSite(null, activeFaction);
            MapView.CenterAndSelect(factionSite);

            if (CheckGameOver()) return;

            // get player controlling active faction
            PlayerManager players = PlayerManager.Instance;
            Player player = players.GetPlayer(activeFaction);

            // pass game to computer player
            ComputerPlayer computer = player as ComputerPlayer;
            if (computer != null) {
                State = SessionState.Computer;
                var args = new ComputerThreadArgs(
                    Application.Current.Dispatcher, WorldState, computer.Options);

                // attach task event handlers
                MainWindow window = MainWindow.Instance;
                args.Events.TaskComplete += window.OnComputerComplete;
                args.Events.TaskException += window.OnComputerException;
                args.Events.TaskMessage += window.OnComputerMessage;

                // start background thread
                players.ComputerThread.Start(args);
                return;
            }

            // pass game to human player
            HumanPlayer human = (HumanPlayer) player;

            // dispatch as determined by game mode
            switch (players.GetGameMode()) {

                case GameMode.Email: {
                    // guess next player's location
                    bool remote = IsRemotePlayer(human);
                    string dialogText = (remote ?
                        Global.Strings.DialogPlayerRemote :
                        Global.Strings.DialogPlayerLocal);

                    // ask user about player's real location
                    string message = String.Format(ApplicationInfo.Culture,
                        dialogText, human.Name, human.Email);

                    MessageBoxResult result = MessageBox.Show(MainWindow.Instance, message,
                        activeFaction.Name, MessageBoxButton.YesNo, MessageBoxImage.Information);

                    // invert assumption if requested
                    if (result == MessageBoxResult.No) {
                        if (remote) {
                            // user says this address is local
                            LocalEmail = human.Email;
                            remote = false;
                        } else
                            remote = true;
                    }

                    // pass game to remote human player
                    if (remote) {
                        // attempt to create & send PBEM file
                        while (!SendEmailGame(human)) {

                            // ask user for another attempt on failure
                            result = MessageBox.Show(MainWindow.Instance,
                                Global.Strings.DialogPlayerRetry, Global.Strings.TitleMailError,
                                MessageBoxButton.OKCancel, MessageBoxImage.Question);

                            if (result == MessageBoxResult.Cancel) break;
                        }

                        Close(false);
                        break;
                    }

                    // pass game to local human player
                    goto case GameMode.Hotseat;
                }

                case GameMode.Hotseat: {
                    // show "hot seat" message for local player
                    string message = String.Format(ApplicationInfo.Culture,
                        Global.Strings.DialogPlayerStart, human.Name);

                    MessageBox.Show(MainWindow.Instance, message, activeFaction.Name,
                        MessageBoxButton.OK, MessageBoxImage.Information);

                    goto case GameMode.Single;
                }

                case GameMode.Single:
                    State = SessionState.Human;

                    Action postAction = delegate {
                        // select one of the faction's active units
                        SelectFactionUnit(activeFaction);

                        // check if game was just won
                        CheckGameOver();
                    };

                    // check if we need a BeginTurn command
                    if (WorldState.History.HaveBeginTurn())
                        postAction();
                    else
                        AsyncAction.BeginRun(delegate {
                            // issue BeginTurn command on background thread
                            if (!Executor.ExecuteBeginTurn(WorldState))
                                postAction = () => Close(false);

                            AsyncAction.BeginInvoke(delegate {
                                postAction(); AsyncAction.EndRun();
                            });
                        });
                    break;

                default:
                    ThrowHelper.ThrowInvalidOperationExceptionWithFormat(
                        Tektosyne.Strings.MethodInvalidValue, "GetGameMode");
                    Close(false);
                    break;
            }
        }

        #endregion
        #region LoadDirect

        /// <summary>
        /// Loads all data of the <see cref="Session"/> object from the specified XML file which is
        /// validated against <see cref="FilePaths.SessionSchema"/>.</summary>
        /// <param name="file">
        /// The XML session description file from which to load the data for this <see
        /// cref="Session"/> object. An extension of ".gz" indicates GZip compression.</param>
        /// <exception cref="DetailException">
        /// An <see cref="XmlException"/> or <see cref="XmlSchemaException"/> occurred while loading
        /// <paramref name="file"/>. All such exceptions will be converted to a
        /// <b>DetailException</b> whose <see cref="DetailException.Detail"/> property contains
        /// technical details provided by the XML parser.</exception>

        internal void LoadDirect(string file) {
            Stream stream = null;

            try {
                // create stream for uncompressed data
                stream = new FileStream(file, FileMode.Open);

                // create stream with GZip decompression
                if (file.EndsWith(".gz", StringComparison.OrdinalIgnoreCase))
                    stream = new GZipStream(stream, CompressionMode.Decompress);

                // validate against Session schema
                XmlReaderSettings settings =
                    XmlUtility.CreateReaderSettings(FilePaths.SessionSchema);

                // read Session data from specified file
                using (XmlReader reader = XmlReader.Create(stream, settings))
                    ReadXml(reader);
            }
            catch (XmlSchemaException e) {
                ThrowHelper.ThrowDetailException(Global.Strings.XmlErrorSchema, e);
            }
            catch (XmlException e) {
                ThrowHelper.ThrowDetailException(Global.Strings.XmlError, e);
            }
            finally {
                if (stream != null) stream.Close();
            }
        }

        #endregion
        #region Open

        /// <summary>
        /// Opens a saved <see cref="Session"/> based on the specified session XML file.</summary>
        /// <param name="path">
        /// An optional path to the session XML file to open.</param>
        /// <remarks><para>
        /// <b>Open</b> performs the following actions:
        /// </para><list type="number"><item>
        /// Call <see cref="Close"/> to close the current <see cref="Session"/>, asking the user for
        /// confirmation if the <see cref="WorldState"/> contains unsaved data.
        /// </item><item>
        /// Call <see cref="Resume"/> with the specified <paramref name="path"/>, asking the user
        /// for a session XML file to open if <paramref name="path"/> is not a valid string.
        /// </item><item>
        /// Call <see cref="Dispatch"/> on the new <see cref="Instance"/>.</item></list></remarks>

        public static void Open(string path) {
            if (Close(true) && Resume(path))
                Instance.Dispatch();
        }

        #endregion
        #region Resume

        /// <summary>
        /// Initializes a new <see cref="Instance"/> of the <see cref="Session"/> class based on a
        /// session description file.</summary>
        /// <param name="file">
        /// An optional file path to an XML session description.</param>
        /// <returns>
        /// <c>true</c> if the new <see cref="Instance"/> was successfully initialized; otherwise,
        /// <c>false</c>.</returns>
        /// <exception cref="PropertyValueException">
        /// <see cref="Instance"/> is not a null reference.</exception>
        /// <remarks><para>
        /// <b>Resume</b> lets the user browse for a session file if the specified <paramref
        /// name="file"/> is a null reference or an empty string. If the user cancels this dialog,
        /// <b>Resume</b> returns without creating a new <see cref="Instance"/> of the <see
        /// cref="Session"/> class.
        /// </para><para>
        /// <b>Resume</b> shows a <see cref="Dialog.CreateSession"/> dialog while the new
        /// <b>Instance</b> is being created. Any errors that occur are shown in a message box.
        /// <b>Resume</b> does not set the <b>Instance</b> property in this case, but exceptions are
        /// never propagated to the caller.</para></remarks>

        public static bool Resume(string file) {
            if (Instance != null)
                ThrowHelper.ThrowPropertyValueException(
                    "Instance", Tektosyne.Strings.PropertyNotNull);

            RootedPath path;
            if (String.IsNullOrEmpty(file)) {
                // ask user to select a session file
                path = FileDialogs.OpenSessionDialog();
            } else {
                // prepend user-specific session folder
                path = FilePaths.GetSessionFile(file);
            }

            if (path.IsEmpty) return false;
            file = path.AbsolutePath;

            // set singleton reference
            Instance = new Session();

            // create session with status display
            var dialog = new Dialog.CreateSession(file, false) { Owner = MainWindow.Instance };
            if (dialog.ShowDialog() == true)
                return true;

            // show exception, if any
            if (dialog.SessionException != null) {
                string message = String.Format(ApplicationInfo.Culture,
                    Global.Strings.DialogSessionOpenError, file);

                MessageDialog.Show(MainWindow.Instance, message, Global.Strings.TitleSessionError,
                    dialog.SessionException, MessageBoxButton.OK, Images.Error);
            }

            // clear singleton reference
            Instance.Dispose();
            return false;
        }

        #endregion
        #region Save

        /// <summary>
        /// Saves the <see cref="Session"/> to the specified file, interacting with the user as
        /// required.</summary>
        /// <param name="file"><para>
        /// An optional file path to an XML session description. An extension of ".gz" indicates
        /// GZip compression.
        /// </para><para>
        /// On return, this parameter holds the absolute file path to which the data of this <see
        /// cref="Session"/> object was saved, if <b>Save</b> returns <c>true</c>; otherwise, its
        /// value is undefined.</para></param>
        /// <param name="showDialog">
        /// <c>true</c> if a "Save File" dialog should be shown to let the user change the specified
        /// <paramref name="file"/>; otherwise, <c>false</c>.</param>
        /// <returns>
        /// <c>true</c> if the <see cref="Session"/> was saved successfully; otherwise,
        /// <c>false</c>.</returns>
        /// <remarks><para>
        /// If <paramref name="showDialog"/> is <c>true</c>, <b>Save</b> shows a "Save File" dialog
        /// to let the user change the specified <paramref name="file"/> . If the user cancels this
        /// dialog, or if <paramref name="showDialog"/> is <c>false</c> and <paramref name="file"/>
        /// is a null reference or an empty string, <b>Save</b> immediately returns <c>false</c>.
        /// </para><para>
        /// Any errors that occur during serialization are displayed to the user, but exceptions are
        /// never propagated to the caller. <b>Save</b> returns silently in case of success.
        /// </para></remarks>

        public bool Save(ref string file, bool showDialog) {

            // prepend user-specific session folder
            RootedPath path = FilePaths.GetSessionFile(file);

            // ask user to select a session file
            if (showDialog)
                path = FileDialogs.SaveSessionDialog(path.RelativePath);

            // return absolute path, if any
            if (path.IsEmpty) return false;
            file = path.AbsolutePath;

            // ask user to wait
            MainWindow.Instance.BeginWait(Global.Strings.StatusGameSaving);

            try {
                // save & clear change flag
                SaveDirect(file);
                WorldChanged = false;
                return true;
            }
            catch (Exception e) {
                string message = String.Format(ApplicationInfo.Culture,
                    Global.Strings.DialogSessionSaveError, file);

                MessageDialog.Show(MainWindow.Instance, message,
                    Global.Strings.TitleSessionError, e, MessageBoxButton.OK, Images.Error);

                return false;
            }
            finally {
                MainWindow.Instance.EndWait();
            }
        }

        #endregion
        #region SaveDirect

        /// <summary>
        /// Saves all <see cref="Session"/> data to the specified XML file which will conform to
        /// <see cref="FilePaths.SessionSchema"/>.</summary>
        /// <param name="file">
        /// The XML session description file to which to save the current data of this <see
        /// cref="Session"/> object. An extension of ".gz" indicates GZip compression.</param>
        /// <exception cref="ArgumentNullOrEmptyException">
        /// <paramref name="file"/> is a null reference or an empty string.</exception>
        /// <remarks><para>
        /// <b>SaveDirect</b> creates a monolithic XML document containing all serializable data of
        /// this <see cref="Session"/> object, as defined by <see cref="FilePaths.SessionSchema"/>.
        /// Any existing file at the location specified by <paramref name="file"/> will be
        /// overwritten.
        /// </para><para>
        /// <see cref="ApplicationUtility.OnUnhandledException"/> may call <b>SaveDirect</b> to
        /// write the current session data to an XML file.</para></remarks>

        internal void SaveDirect(string file) {
            if (String.IsNullOrEmpty(file))
                ThrowHelper.ThrowArgumentNullOrEmptyException("file");

            Stream stream = null;

            try {
                // create stream for uncompressed data
                stream = new FileStream(file, FileMode.Create);

                // create stream with GZip compression
                if (file.EndsWith(".gz", StringComparison.OrdinalIgnoreCase))
                    stream = new GZipStream(stream, CompressionMode.Compress);

                // write Session data to specified file
                XmlWriterSettings settings = XmlUtility.CreateWriterSettings();
                using (XmlWriter writer = XmlWriter.Create(stream, settings))
                    WriteXml(writer);
            }
            finally {
                if (stream != null) stream.Close();
            }
        }

        #endregion
        #region SetWorldChanged

        /// <summary>
        /// Sets the <see cref="WorldChanged"/> property to <c>true</c>.</summary>
        /// <remarks>
        /// <b>SetWorldChanged</b> should be called to indicate that the associated <see
        /// cref="WorldState"/> contains unsaved changes.</remarks>

        public void SetWorldChanged() {
            WorldChanged = true;
        }

        #endregion
        #region Start

        /// <summary>
        /// Initializes a new <see cref="Instance"/> of the <see cref="Session"/> class based on a
        /// scenario description file.</summary>
        /// <param name="file">
        /// An optional file path to an XML scenario description.</param>
        /// <returns>
        /// <c>true</c> if the new <see cref="Instance"/> was successfully initialized; otherwise,
        /// <c>false</c>.</returns>
        /// <exception cref="PropertyValueException">
        /// <see cref="Instance"/> is not a null reference.</exception>
        /// <remarks><para>
        /// <b>Start</b> lets the user browse for a scenario file if the specified <paramref
        /// name="file"/> is a null reference or an empty string. If the user cancels this dialog,
        /// <b>Start</b> returns without creating a new <see cref="Instance"/> of the <see
        /// cref="Session"/> class.
        /// </para><para>
        /// <b>Start</b> shows a <see cref="Dialog.CreateSession"/> dialog while the new
        /// <b>Instance</b> is being created. Any errors that occur are shown in a message box.
        /// <b>Start</b> does not set the <b>Instance</b> property in this case, but exceptions are
        /// never propagated to the caller.</para></remarks>

        public static bool Start(string file) {
            if (Instance != null)
                ThrowHelper.ThrowPropertyValueException(
                    "Instance", Tektosyne.Strings.PropertyNotNull);

            RootedPath path;
            if (String.IsNullOrEmpty(file)) {
                // ask user to select a scenario file
                path = FileDialogs.OpenScenarioDialog();
            } else {
                // prepend user-specific scenario folder
                path = FilePaths.GetScenarioFile(file);
            }

            if (path.IsEmpty) return false;
            file = path.AbsolutePath;

            // set singleton reference
            Instance = new Session();

            // create session with status display
            var dialog = new Dialog.CreateSession(file, true) { Owner = MainWindow.Instance };
            if (dialog.ShowDialog() == true)
                return true;

            // show exception if one occurred
            if (dialog.SessionException != null) {
                string message = String.Format(ApplicationInfo.Culture,
                    Global.Strings.DialogScenarioOpenError, file);

                MessageDialog.Show(MainWindow.Instance, message, Global.Strings.TitleScenarioError,
                    dialog.SessionException, MessageBoxButton.OK, Images.Error);
            }

            // clear singleton reference
            Instance.Dispose();
            return false;
        }

        #endregion
        #endregion
        #region IDisposable Members

        /// <summary>
        /// Releases all resources used by the <see cref="Session"/> object.</summary>
        /// <remarks><para>
        /// <b>Dispose</b> disposes of the current <see cref="WorldState"/>, <see cref="Replay"/>,
        /// <see cref="PlayerManager"/>, <see cref="MapViewManager"/>, and <see
        /// cref="MasterSection"/> instances.
        /// </para><para>
        /// <b>Dispose</b> also resets the <see cref="Instance"/> property to a null reference,
        /// allowing the creation of another instance of the <see cref="Session"/> class, and resets
        /// the <see cref="State"/> property to <see cref="SessionState.Invalid"/>.</para></remarks>

        public void Dispose() {

            // guaranteed by Resume/Start
            Debug.Assert(Instance == this);

            // clear singleton reference
            Instance = null;
            State = SessionState.Invalid;

            // dispose of replay manager
            if (this._replayManager != null)
                this._replayManager.Dispose();

            // clear player settings delegate
            Faction.GetPlayerSettings = null;

            // dispose of player manager
            if (PlayerManager.Instance != null)
                PlayerManager.Instance.Dispose();

            // dispose of map view manager
            if (MapViewManager.Instance != null)
                MapViewManager.Instance.Dispose();

            // clear cached entity class data
            EntityClassCache.Clear();

            // dispose of scenario instance
            if (MasterSection.Instance != null)
                MasterSection.Instance.Dispose();
        }

        #endregion
        #region IXmlSerializable Members
        #region ConstXmlName

        /// <summary>
        /// The name of the XML element associated with the <see cref="Session"/> class.</summary>
        /// <remarks>
        /// <b>ConstXmlName</b> holds the value "session", indicating the XML element defined in
        /// <see cref="FilePaths.SessionSchema"/> whose data is managed by the <see
        /// cref="Session"/> class. This element is the root node of a Hexkit session description.
        /// </remarks>

        public const string ConstXmlName = "session";

        #endregion
        #region XmlName

        /// <summary>
        /// Gets the name of the XML element associated with the <see cref="Session"/> object.
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
        /// Reads XML data into the <see cref="Session"/> object using the specified <see
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
        /// <b>ReadXml</b> replaces the data of this <see cref="Session"/> object with any matching
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

            while (reader.Read() && reader.IsStartElement()) {
                switch (reader.Name) {

                    case "scenario":
                        // store scenario hash code and file path
                        this._scenarioHash = reader["hash"];

                        // allow for scenario file moving in scenario tree
                        RootedPath path = FilePaths.SearchScenarioTree(reader["href"]);
                        Debug.Assert(!path.IsEmpty);

                        this._scenarioPath = path.AbsolutePath;
                        break;

                    case PlayerManager.ConstXmlName:
                        // read player setup
                        PlayerManager.Instance.ReadXml(reader);
                        break;

                    case History.ConstXmlName:
                        // read command history
                        WorldState.History.ReadXml(reader);
                        break;

                    case "gameOver":
                        // read flag indicating end of game
                        if (XmlConvert.ToBoolean(reader.ReadString()))
                            GameWasOver = true;
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
        /// Writes the current data of the <see cref="Session"/> object to the specified <see
        /// cref="XmlWriter"/>.</summary>
        /// <param name="writer">
        /// The <see cref="XmlWriter"/> to which to write.</param>
        /// <remarks>
        /// The XML data sent to <paramref name="writer"/> comprises an XML document declaration; a
        /// root element named <see cref="XmlName"/> with a default XML namespace of <see
        /// cref="FilePaths.SessionNamespace"/>; and all serializable data stored in this
        /// <b>Session</b> object.</remarks>

        public void WriteXml(XmlWriter writer) {

            // write XML declaration and session namespace
            writer.WriteStartDocument(true);
            writer.WriteStartElement(XmlName, FilePaths.SessionNamespace);

            // write application info element
            ApplicationInfo.WriteXml(writer);

            // write scenario hash code and file path
            writer.WriteStartElement("scenario");
            writer.WriteAttributeString("hash", MasterSection.Instance.Hash);
            writer.WriteAttributeString("href", MasterSection.Instance.Path.RelativePath);
            writer.WriteEndElement();

            // write player setup
            PlayerManager.Instance.WriteXml(writer);

            // write command history
            WorldState.History.WriteXml(writer);

            // write flag indicating end of game
            if (WorldState.GameOver)
                writer.WriteElementString("gameOver", XmlConvert.ToString(WorldState.GameOver));

            // close root node and document
            writer.WriteEndElement();
            writer.WriteEndDocument();
        }

        #endregion
        #endregion
    }
}
