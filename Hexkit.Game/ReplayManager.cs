using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Windows;

using Tektosyne;
using Tektosyne.Geometry;
using Tektosyne.Windows;

using Hexkit.Global;
using Hexkit.Graphics;
using Hexkit.Options;
using Hexkit.World;
using Hexkit.World.Commands;
using Hexkit.World.Instructions;

namespace Hexkit.Game {
    #region Type Aliases

    // avoid confusion with System.Threading.ExecutionContext
    using ExecutionContext = Hexkit.World.Commands.ExecutionContext;

    #endregion

    /// <summary>
    /// Manages interactive replays for the current game session.</summary>
    /// <remarks><para>
    /// <b>ReplayManager</b> manages the interactive replay of history commands; that is, all
    /// functions that are accessible through the "Replay" menu of the Hexkit Game application.
    /// </para><para>
    /// An instance of <b>ReplayManager</b> always operates on the current <see cref="Session"/>
    /// instance. The actual replay is performed on a background thread. Clients may call <see
    /// cref="ReplayManager.RequestState"/> to communicate with an active replay thread.
    /// </para></remarks>

    public class ReplayManager: IDisposable {
        #region ReplayManager()

        /// <summary>
        /// Initializes a new instance of the <see cref="ReplayManager"/> class.</summary>

        internal ReplayManager() {
            Clear(false);
        }

        #endregion
        #region Private Fields

        // signal to pause or resume command replay
        private static ManualResetEvent _playSignal = new ManualResetEvent(true);

        // property backers
        private ReplayState _currentState;

        // original session data before replay
        private WorldState _originalWorldState;
        private PointI _originalSelected =  Site.InvalidLocation;
        private SessionState _originalState = SessionState.Closed;

        // current command replay data
        private Command _command;
        private int _commandIndex = -1;
        private bool _commandSkip;

        #endregion
        #region CurrentState

        /// <summary>
        /// Gets the current state of the <see cref="ReplayManager"/>.</summary>
        /// <value>
        /// A <see cref="ReplayState"/> value indicating the current state of the <see
        /// cref="ReplayManager"/>. The default is <see cref="ReplayState.Stop"/>.</value>
        /// <remarks><para>
        /// The <see cref="ReplayManager"/> updates <b>CurrentState</b> in response to <see
        /// cref="Start"/> and <see cref="RequestState"/> calls.
        /// </para><para>
        /// Any such update assigns the same value to the <see cref="RequestedState"/> property and
        /// invokes <see cref="MainWindow.OnReplayStateChanged"/> on the <see cref="MainWindow"/>.
        /// This call is automatically marshalled to the current <see cref="Application"/> thread if
        /// different from the one setting <b>CurrentState</b>.</para></remarks>

        public ReplayState CurrentState {
            [DebuggerStepThrough]
            get { return this._currentState; }
            private set {
                if (this._currentState != value) {
                    this._currentState = value;
                    RequestedState = value;

                    // notify main window of state change
                    if (Application.Current.CheckAccess())
                        MainWindow.Instance.OnReplayStateChanged(value);
                    else
                        AsyncAction.Invoke(() => MainWindow.Instance.OnReplayStateChanged(value));
                }
            }
        }

        #endregion
        #region IsComputerTurn

        /// <summary>
        /// Gets a value indicating whether the most recent computer player turn is being replayed.
        /// </summary>
        /// <value>
        /// <c>true</c> if the replay shows the most recent computer player turn; otherwise,
        /// <c>false</c>. The default is <c>false</c>.</value>
        /// <remarks>
        /// If <b>IsComputerTurn</b> is <c>true</c>, the next <see cref="Faction"/> will be
        /// activated automatically when the current replay stops.</remarks>

        public bool IsComputerTurn { get; private set; }

        #endregion
        #region RequestedState

        /// <summary>
        /// Gets the requested state of the <see cref="ReplayManager"/>.</summary>
        /// <value>
        /// A <see cref="ReplayState"/> value indicating the requested state of the <see
        /// cref="ReplayManager"/>. The default is <see cref="ReplayState.Stop"/>.</value>
        /// <remarks>
        /// <b>RequestedState</b> usually returns the same value as <see cref="CurrentState"/>, but
        /// may be set to a different value by <see cref="RequestState"/> when a requested <see
        /// cref="ReplayState"/> cannot be entered immediately.</remarks>

        public ReplayState RequestedState { get; private set; }

        #endregion
        #region Private Methods
        #region CheckStartState

        /// <summary>
        /// Checks that the session <see cref="Session.State"/> allows interactive replay to start.
        /// </summary>
        /// <exception cref="PropertyValueException">
        /// The current session <see cref="Session.State"/> is neither <see
        /// cref="SessionState.Closed"/>, <see cref="SessionState.Computer"/>, nor <see
        /// cref="SessionState.Human"/>.</exception>
        /// <remarks>
        /// <b>CheckStartState</b> does nothing on success.</remarks>

        private static void CheckStartState() {
            SessionState state = Session.State;

            if (state != SessionState.Closed &&
                state != SessionState.Computer &&
                state != SessionState.Human) {

                ThrowHelper.ThrowPropertyValueException("Session.State", state,
                    Tektosyne.Strings.PropertyNotInValues + String.Join(", ",
                        SessionState.Closed, SessionState.Computer, SessionState.Human));
            }
        }

        #endregion
        #region Clear

        /// <summary>
        /// Clears all replay data.</summary>
        /// <param name="disposing">
        /// <c>true</c> if <b>Clear</b> was called by <see cref="Dispose"/>; otherwise,
        /// <c>false</c>.</param>
        /// <remarks><para>
        /// <b>Clear</b> resets the <see cref="SessionExecutor.AbortSignal"/> and the <see
        /// cref="IsComputerTurn"/> flag, the command data used by <see cref="NextCommand"/>, and
        /// the original session data recorded by <see cref="Start"/>.
        /// </para><para>
        /// If <see cref="IsComputerTurn"/> was <c>true</c> and <paramref name="disposing"/> is
        /// <c>false</c>, <b>Clear</b> also invokes <see cref="MainWindow.ComputerEndTurn"/> on the
        /// <see cref="MainWindow"/>.</para></remarks>

        private void Clear(bool disposing) {
            _playSignal.Set();
            SessionExecutor.AbortSignal.Reset();

            this._command = null;
            this._commandIndex = -1;

            this._originalWorldState = null;
            this._originalSelected = Site.InvalidLocation;
            this._originalState = SessionState.Closed;

            bool wasComputerTurn = IsComputerTurn;
            IsComputerTurn = false;

            // disposing skips actions that require valid main window
            if (!disposing && wasComputerTurn)
                MainWindow.ComputerEndTurn();
        }

        #endregion
        #region NextCommand

        /// <summary>
        /// Replay the next history <see cref="Command"/>.</summary>
        /// <returns>
        /// <c>true</c> if interactive replay should continue; <c>false</c> if <see cref="Stop"/>
        /// should be called.</returns>
        /// <exception cref="InvalidCommandException">
        /// The current <see cref="Command"/> contains invalid data.</exception>
        /// <remarks><para>
        /// <b>NextCommand</b> fetches the next history command, validates and executes it, and
        /// increments the command counter as necessary. <b>NextCommand</b> returns <c>false</c> if
        /// the command history has been exhausted.
        /// </para><para>
        /// The <see cref="Command.Source"/> and <see cref="Command.Target"/> sites of each history
        /// command are scrolled into view on the default <see cref="Session.MapView"/> if the <see
        /// cref="Options.ReplayOptions.Scroll"/> property of the current <see
        /// cref="ApplicationOptions"/> instance is <c>true</c>.</para></remarks>

        private bool NextCommand() {

            Debug.Assert(this._originalWorldState != null);
            Debug.Assert(this._commandIndex >= 0);
            Debug.Assert(!this._commandSkip);

            // retrieve original command history
            IList<Command> commands = this._originalWorldState.History.Commands;

            // stop replay if history exhausted
            if (this._commandIndex >= commands.Count)
                return false;

            MapView mapView = Session.MapView;

            // retrieve next command in history
            if (this._command == null) {
                this._command = commands[this._commandIndex];

                // validate & show command
                this._command.Validate(Session.Instance.WorldState);
                SessionExecutor.ShowCommand(this._command);

                // scroll sites into view if desired
                PointI source = this._command.Source.Location;
                if (ApplicationOptions.Instance.Game.Replay.Scroll) {
                    PointI target = this._command.Target.Location;
                    AsyncAction.Invoke(() => mapView.ScrollIntoView(source, target));
                }

                // highlight valid source site
                if (Finder.MapGrid.Contains(source)) {
                    AsyncAction.Invoke(() => mapView.SelectedSite = source);

                    // select first entity if specified
                    EntityReference[] entities = this._command.Entities;
                    if (entities != null && entities.Length > 0)
                       AsyncAction.Invoke(() => MainWindow.Instance.SelectedEntity = entities[0].Id);

                    return true; // show source for a while
                }
            }

            // attempt to execute command
            this._command.Execute(new ExecutionContext(
                Session.Instance.WorldState, null, SessionExecutor.ShowCommandEvent));

            // check if command cleared by reentrant call
            if (this._command != null) {

                // add some delay after commands with message events
                this._commandSkip = this._command.Program.Exists(x => x is MessageInstruction);

                // highlight active faction for Begin/EndTurn, otherwise command target
                if (this._command is BeginTurnCommand || this._command is EndTurnCommand)
                    ShowFaction(this._command.Faction.Value);
                else
                    ShowSite(this._command.Target.Location);

                // ensure command effects are shown
                AsyncAction.Invoke(mapView.Redraw);
            }

            // prepare for next command
            this._command = null;
            ++this._commandIndex;

            return true;
        }

        #endregion
        #region PlayCommands

        /// <summary>
        /// Replays all commands, starting at the current replay index in the command history.
        /// </summary>
        /// <remarks><para>
        /// <b>PlayCommands</b> immediately calls <see cref="Stop"/> if <see cref="CurrentState"/>
        /// does not equal <see cref="ReplayState.Play"/>, or if there is no valid replay data.
        /// </para><para>
        /// Otherwise, <b>PlayCommands</b> blocks if <see cref="ReplayState.Pause"/> was requested;
        /// skips one faction ahead if <see cref="ReplayState.Skip"/> was requested; and stops the
        /// replay if <see cref="ReplayState.Stop"/> was requested.
        /// </para><para>
        /// Otherwise, <b>PlayCommands</b> calls <see cref="NextCommand"/> to replay the current
        /// history command. On failure, <b>PlayCommands</b> calls <see cref="Stop"/> and shows an
        /// error message if the call failed due to an <see cref="InvalidCommandException"/>.
        /// </para><para>
        /// Otherwise, <b>PlayCommands</b> blocks on the <see cref="SessionExecutor.AbortSignal"/>
        /// for the current <see cref="ReplayOptions.Delay"/>, and repeats the entire process until
        /// a <see cref="Stop"/> condition occurs or the command history is exhausted.
        /// </para></remarks>

        private void PlayCommands() {
            while (true) {

                // sanity check for valid state & command
                if (CurrentState != ReplayState.Play || this._commandIndex < 0)
                    break;

                // block in Pause state if requested
                if (RequestedState == ReplayState.Pause) {
                    CurrentState = ReplayState.Pause;
                    _playSignal.WaitOne();
                }

                // process other state change requests
                switch (RequestedState) {

                    case ReplayState.Play:
                        // change state back to Play after Pause
                        if (CurrentState == ReplayState.Pause)
                            CurrentState = ReplayState.Play;
                        break;

                    case ReplayState.Skip:
                        // change state to Skip, then to Play or Stop
                        int faction = Session.Instance.WorldState.ActiveFactionIndex + 1;
                        Skip(-1, faction);
                        if (CurrentState == ReplayState.Stop) return;
                        break;

                    case ReplayState.Stop:
                        Stop(); // change state to Stop
                        return;
                }

                // ensure commands are replayed normally
                SessionExecutor.AbortSignal.Reset();

                // skip one tick if desired
                if (this._commandSkip)
                    this._commandSkip = false;
                else {
                    try {
                        if (!NextCommand()) break;
                    }
                    catch (InvalidCommandException e) {
                        ShowCommandError(e);
                        break;
                    }
                }

                // wait for replay delay or abort signal
                int delay = ApplicationOptions.Instance.Game.Replay.Delay;
                SessionExecutor.AbortSignal.WaitOne(delay);
            }

            Stop();
        }

        #endregion
        #region ShowCommandError

        /// <summary>
        /// Shows an error message for the specified <see cref="InvalidCommandException"/>.
        /// </summary>
        /// <param name="exception">
        /// An <see cref="InvalidCommandException"/> that occurred during command replay.</param>
        /// <remarks>
        /// <b>ShowError</b> shows a <see cref="MessageDialog"/> with the specified <paramref
        /// name="exception"/> and a note that replay has been stopped.</remarks>

        private static void ShowCommandError(InvalidCommandException exception) {

            AsyncAction.Invoke(() => MessageDialog.Show(MainWindow.Instance,
                Global.Strings.DialogReplayError, Global.Strings.TitleReplayError,
                exception, MessageBoxButton.OK, Images.Error));
        }

        #endregion
        #region ShowFaction

        /// <summary>
        /// Shows a <see cref="Site"/> associated with the specified <see cref="Faction"/>.
        /// </summary>
        /// <param name="faction">
        /// The <see cref="Faction"/> for which to show a <see cref="Site"/>.</param>
        /// <remarks>
        /// <b>ShowFaction</b> updates the "Turn/Faction" display, and may also update the default
        /// <see cref="Session.MapView"/> to show the <see cref="Site"/> returned by <see
        /// cref="Finder.FindFactionSite"/> for the specified <paramref name="faction"/>, depending
        /// on the settings of the current <see cref="ApplicationOptions"/> instance.</remarks>

        private void ShowFaction(Faction faction) {

            // find site associated with faction
            PointI factionSite = Finder.FindFactionSite(null, faction);

            // scroll site into view if desired
            if (ApplicationOptions.Instance.Game.Replay.Scroll)
                AsyncAction.Invoke(() => Session.MapView.ScrollIntoView(factionSite));

            // highlight site in any case
            ShowSite(factionSite);

            // update Turn/Faction display
            AsyncAction.Invoke(() => MainWindow.Instance.UpdateTurnFaction());
        }

        #endregion
        #region ShowSite

        /// <summary>
        /// Selects the specified <see cref="Site"/>.</summary>
        /// <param name="site">
        /// The coordinates of the <see cref="Site"/> to select.</param>
        /// <remarks>
        /// <b>ShowSite</b> selects the specified <paramref name="site"/> in the default <see
        /// cref="Session.MapView"/> if the coordinates are valid, and otherwise updates the
        /// "Selected Site" display to reflect possible changes in the contents of the current <see
        /// cref="MapView.SelectedSite"/>.</remarks>

        private void ShowSite(PointI site) {

            // highlight valid site for a while
            if (Finder.MapGrid.Contains(site)) {
                AsyncAction.Invoke(() => Session.MapView.SelectedSite = site);
                this._commandSkip = true;
                return;
            }

            /*
             * Since we did not set SelectedSite we must refresh the
             * Selection panel in case its SelectedSite was affected
             * by whatever event caused the call to ShowSite.
             */

            AsyncAction.Invoke(() => MainWindow.Instance.UpdateSelection());
        }

        #endregion
        #region Skip

        /// <summary>
        /// Skips ahead to the specified full turn and active faction indices.</summary>
        /// <param name="turn">
        /// The index of the full turn at which to resume interactive replay.</param>
        /// <param name="faction">
        /// The index of the faction whose activation during the specified <paramref name="turn"/>
        /// should resume interactive replay.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="faction"/> is less than zero.</exception>
        /// <exception cref="PropertyValueException">
        /// The current session <see cref="Session.State"/> is not <see
        /// cref="SessionState.Replay"/>.</exception>
        /// <remarks><para>
        /// <b>Skip</b> performs one of the following actions, depending on the specified <paramref
        /// name="turn"/> index:
        /// </para><list type="table"><listheader>
        /// <term><paramref name="turn"/></term><description>Action</description>
        /// </listheader><item>
        /// <term>Less than zero</term>
        /// <description>Silently execute history commands until the next <see
        /// cref="EndTurnCommand"/> was executed.</description>
        /// </item><item>
        /// <term>Less than or equal to the index of the currently replayed turn</term>
        /// <description>Do nothing.</description>
        /// </item><item>
        /// <term>Less than the maximum turn index in the current game</term>
        /// <description>Silently execute history commands until the index of the currently replayed
        /// turn equals <paramref name="turn"/>.</description>
        /// </item><item>
        /// <term>Greater than the maximum turn index in the current game</term>
        /// <description>Call <see cref="Stop"/>.</description>
        /// </item></list><para>
        /// If the specified <paramref name="faction"/> index is less than the number of surviving
        /// <see cref="WorldState.Factions"/> when interactive replay would normally resume,
        /// <b>Skip</b> continues to silently execute history commands until the faction with the
        /// specified index has been activated. Otherwise, the <paramref name="faction"/> parameter
        /// is ignored.
        /// </para><para>
        /// <b>Skip</b> also sets the <see cref="CurrentState"/> to <see cref="ReplayState.Skip"/>
        /// during execution, then back to <see cref="ReplayState.Play"/> when finished. <b>Skip</b>
        /// calls <see cref="Stop"/> instead if an error occurred, or if the command history is
        /// already exhausted.</para></remarks>

        private void Skip(int turn, int faction) {

            if (Session.State != SessionState.Replay)
                ThrowHelper.ThrowPropertyValueExceptionWithFormat("Session.State",
                    Session.State, Tektosyne.Strings.PropertyNotValue, SessionState.Replay);

            if (faction < 0)
                ThrowHelper.ThrowArgumentOutOfRangeException(
                    "faction", faction, Tektosyne.Strings.ArgumentNegative);

            WorldState world = Session.Instance.WorldState;

            // reset faction to zero if already dead
            if (faction >= world.Factions.Count)
                faction = 0;

            // do nothing if specified turn & faction reached
            if ((turn >= 0 && turn < world.CurrentTurn) ||
                (turn == world.CurrentTurn && world.ActiveFactionIndex >= faction))
                return;

            // stop immediately if unreachable turn specified
            if (turn > this._originalWorldState.CurrentTurn) {
                Stop();
                return;
            }

            // enter Skip state
            CurrentState = ReplayState.Skip;

            // clear pending command, if any
            this._command = null;

            // suspend interactive replay
            AsyncAction.Invoke(delegate {
                MainWindow.Instance.BeginWait(Global.Strings.StatusReplayCommands);
                MainWindow.Instance.StatusMessage.Push();
            });

            TaskEvents events = new TaskEvents(Application.Current.Dispatcher);
            events.TaskMessage += ((sender, args) =>
                MainWindow.Instance.StatusMessage.Text = args.Value);

            try {
                // skip forward to specified turn & faction
                bool resume = SilentReplay(turn, faction, events);

                if (resume) {
                    // show active faction's home site
                    ShowFaction(world.ActiveFaction);

                    // update map view if replay visible
                    if (world == Session.MapView.WorldState)
                        AsyncAction.Invoke(Session.MapView.Redraw);

                    // enter Play state
                    CurrentState = ReplayState.Play;
                    return;
                }
            }
            finally {
                AsyncAction.Invoke(delegate {
                    MainWindow.Instance.StatusMessage.Pop();
                    MainWindow.Instance.EndWait();
                });
            }

            Stop(); // end of history or error
        }

        #endregion
        #region SilentReplay

        /// <summary>
        /// Silently replays all commands in the original command history up to the specified full
        /// turn and active faction indices.</summary>
        /// <param name="turn">
        /// The index of the full turn where replay should stop.</param>
        /// <param name="faction">
        /// The index of the faction whose activation during the specified <paramref name="turn"/>
        /// should stop the replay.</param>
        /// <param name="events">
        /// An optional <see cref="TaskEvents"/> object used for progress display.</param>
        /// <returns>
        /// <c>true</c> if all commands in the specified range were successfully replayed, and if
        /// more commands remain in the original command history; otherwise, <c>false</c>.</returns>
        /// <remarks><para>
        /// <b>SilentReplay</b> is called by <see cref="Skip"/> to skip over part of an ongoing
        /// interactive replay.
        /// </para><para>
        /// <b>SilentReplay</b> shows a dialog and returns <c>false</c> when an <see
        /// cref="InvalidCommandException"/> occurs during command replay.</para></remarks>

        private bool SilentReplay(int turn, int faction, TaskEvents events) {

            string progressMessage = Global.Strings.StatusReplayCommandsCount;
            WorldState worldState = Session.Instance.WorldState;
            ExecutionContext context = new ExecutionContext(worldState, null, null);
            IList<Command> commands = this._originalWorldState.History.Commands;

            /*
             * We only replay up to the next-to-last command because
             * there is nothing left to resume after the last command,
             * and the caller will invoke Stop anyway at this point.
             */

            while (this._commandIndex < commands.Count - 1) {

                // show running count every 0.5 seconds
                if (events != null && events.RestartTimer(500L))
                    events.OnTaskMessage(this, progressMessage,
                        this._commandIndex + 1, commands.Count);

                // fetch next command and increment counter
                Command command = commands[this._commandIndex++];

                try {
                    // attempt to validate & execute command
                    command.Validate(worldState);
                    command.Execute(context);
                }
                catch (InvalidCommandException e) {
                    ShowCommandError(e);
                    return false;
                }

                // check if specified turn reached
                if (command is EndTurnCommand && worldState.CurrentTurn >= turn) {

                    // succeed if specified faction dead or active
                    if (faction >= worldState.Factions.Count ||
                        worldState.ActiveFactionIndex >= faction)
                        return true;
                }
            }

            return false; // no commands left
        }

        #endregion
        #region Stop

        /// <summary>
        /// Stops a replay in progress.</summary>
        /// <remarks><para>
        /// <b>Stop</b> halts the interactive command replay, restores the original <see
        /// cref="Session.State"/> and <see cref="Session.WorldState"/> properties of the current
        /// <see cref="Session"/>, and clears all replay data.
        /// </para><para>
        /// <b>Stop</b> does nothing if the current session <b>State</b> does not equal <see
        /// cref="SessionState.Replay"/>.</para></remarks>

        private void Stop() {
            AsyncAction.Invoke(delegate {

                // do nothing if already stopped
                if (Session.State != SessionState.Replay)
                    return;

                // enter Stop state
                CurrentState = ReplayState.Stop;

                // restore original world state
                Session.Instance.WorldState = this._originalWorldState;
                Session.MapView.WorldState = Session.Instance.WorldState;

                // restore original site selection, if any
                if (this._originalSelected != Site.InvalidLocation)
                    Session.MapView.CenterAndSelect(this._originalSelected);

                // restore original session state
                Session.State = this._originalState;
                MainWindow.Instance.StatusMessage.Pop();

                // clear last replay event message
                MainWindow.Instance.EventMessage.Clear();

                Clear(false); // clear all replay data
            });
        }

        #endregion
        #endregion
        #region RequestState

        /// <summary>
        /// Requests that the <see cref="CurrentState"/> changes to the specified <see
        /// cref="ReplayState"/>.</summary>
        /// <param name="state">
        /// The requested new value for the <see cref="CurrentState"/> property.</param>
        /// <remarks><para>
        /// <b>RequestState</b> does nothing if the specified <paramref name="state"/> already
        /// equals <see cref="CurrentState"/> or <see cref="RequestedState"/>.
        /// </para><para>
        /// <b>RequestState</b> also does nothing if <see cref="CurrentState"/> equals <see
        /// cref="ReplayState.Skip"/> or <see cref="ReplayState.Stop"/>. Skipping ahead prevents all
        /// input, and clients must call <see cref="Start"/> to start replays.
        /// </para><para>
        /// Otherwise, <b>RequestState</b> sets the <see cref="RequestedState"/> to the specified
        /// <paramref name="state"/>, and also sets the <see cref="SessionExecutor.AbortSignal"/> to
        /// speed up <see cref="Command"/> replay until the background thread gets around to check
        /// the new <see cref="RequestedState"/>.</para></remarks>

        public void RequestState(ReplayState state) {

            // quit if state already current or requested
            if (state == CurrentState || state == RequestedState)
                return;

            // quit if currently skipping or stopped
            if (CurrentState == ReplayState.Skip || CurrentState == ReplayState.Stop)
                return;

            // pause current replay if requested
            if (CurrentState == ReplayState.Play && state == ReplayState.Pause)
                _playSignal.Reset();
            else
                _playSignal.Set();

            // let background thread enter requested state
            RequestedState = state;
            SessionExecutor.AbortSignal.Set();
        }

        #endregion
        #region Start(WorldState, Boolean)

        /// <overloads>
        /// Starts an interactive replay.</overloads>
        /// <summary>
        /// Replays all commands present in the specified <see cref="WorldState"/> but missing from
        /// the current <see cref="WorldState"/>.</summary>
        /// <param name="worldState">
        /// The <see cref="WorldState"/> containing the new commands to show in an interactive
        /// replay.</param>
        /// <param name="isComputer">
        /// The new value for the <see cref="IsComputerTurn"/> property while the replay is active.
        /// </param>
        /// <exception cref="ArgumentException">
        /// One of the conditions described in <see cref="History.AddCommands"/>.</exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="worldState"/> is a null reference.</exception>
        /// <exception cref="PropertyValueException">
        /// The current session <see cref="Session.State"/> is neither <see
        /// cref="SessionState.Closed"/>, <see cref="SessionState.Computer"/>, nor <see
        /// cref="SessionState.Human"/>.</exception>
        /// <remarks><para>
        /// <b>Start</b> sets the session <see cref="Session.State"/> to <see
        /// cref="SessionState.Replay"/> and <see cref="CurrentState"/> to <see
        /// cref="ReplayState.Play"/>, and begins an interactive replay of all history commands
        /// stored in the specified <paramref name="worldState"/> that are not present in the <see
        /// cref="WorldState.History"/> of the current session's <see cref="Session.WorldState"/>.
        /// </para><para>
        /// When the replay ends or is aborted, the current session's <see
        /// cref="Session.WorldState"/> will be set to the specified <paramref name="worldState"/>,
        /// and its previous value will be discarded. The session's original <see
        /// cref="Graphics.MapView.SelectedSite"/> is likewise discarded and replaced with <see
        /// cref="Site.InvalidLocation"/>.
        /// </para><para>
        /// The specified <paramref name="worldState"/> may contain the same number of commands as
        /// that of the current <see cref="Session"/>. In this case, <b>Start</b> briefly highlights
        /// the home site of the active faction but does nothing else.</para></remarks>

        public void Start(WorldState worldState, bool isComputer) {
            CheckStartState();

            // store computer turn flag
            IsComputerTurn = isComputer;

            // store current history command count
            History history = Session.Instance.WorldState.History;
            int index = history.Commands.Count;

            // append new history commands, if any
            history.AddCommands(worldState.History);

            // set restore point to new world state
            this._originalWorldState = worldState;

            // save current session state
            this._originalState = Session.State;

            // ignore current site selection
            this._originalSelected = Site.InvalidLocation;

            // start replay with first new command
            Debug.Assert(index <= history.Commands.Count);
            this._commandIndex = index;

            // switch session to Replay state
            Session.State = SessionState.Replay;
            MainWindow.Instance.StatusMessage.Push(Global.Strings.StatusReplay);

            // show active faction's home site
            ShowFaction(Session.Instance.WorldState.ActiveFaction);

            // enter Play state
            CurrentState = ReplayState.Play;
            AsyncAction.Run(PlayCommands);
        }

        #endregion
        #region Start(Int32, Int32)

        /// <summary>
        /// Replays commands starting at the specified full turn and active faction indices.
        /// </summary>
        /// <param name="turn">
        /// The index of the full turn at which to start interactive replay.</param>
        /// <param name="faction">
        /// The index of the faction whose activation during the specified <paramref name="turn"/>
        /// should start interactive replay.</param>
        /// <exception cref="ArgumentOutOfRangeException"><para>
        /// <paramref name="turn"/> is greater than the <see cref="WorldState.CurrentTurn"/> of the
        /// current <see cref="Session.WorldState"/>.
        /// </para><para>-or-</para><para>
        /// <paramref name="faction"/> is less than zero.</para></exception>
        /// <exception cref="PropertyValueException">
        /// The current session <see cref="Session.State"/> is neither <see
        /// cref="SessionState.Closed"/>, <see cref="SessionState.Computer"/>, nor <see
        /// cref="SessionState.Human"/>.</exception>
        /// <remarks><para>
        /// <b>Start</b> sets the session <see cref="Session.State"/> to <see
        /// cref="SessionState.Replay"/> and <see cref="CurrentState"/> to <see
        /// cref="ReplayState.Play"/>, skips ahead to the specified <paramref name="turn"/> and
        /// <paramref name="faction"/> indices, and then begins an interactive replay of all
        /// remaining commands stored in the session's <see cref="WorldState.History"/>.
        /// </para><para>
        /// <b>Start</b> shows an informational message and returns immediately if there are no
        /// commands to replay.
        /// </para><para>
        /// If the specified <paramref name="turn"/> is negative, <b>Start</b> shows a <see
        /// cref="Dialog.ChangeTurn"/> dialog, allowing the user to enter the turn at which to start
        /// interactive replay.
        /// </para><para>
        /// If the specified <paramref name="faction"/> is greater than the number of surviving
        /// factions at any point while skipping ahead, interactive replay will begin with the first
        /// active faction during the specified <paramref name="turn"/>.</para></remarks>

        public void Start(int turn, int faction) {
            CheckStartState();
            Session session = Session.Instance;
            WorldState world = session.WorldState;

            if (turn > world.CurrentTurn)
                ThrowHelper.ThrowArgumentOutOfRangeExceptionWithFormat(
                    "turn", turn, Tektosyne.Strings.ArgumentGreaterValue, "CurrentTurn");

            if (faction < 0)
                ThrowHelper.ThrowArgumentOutOfRangeException(
                    "faction", faction, Tektosyne.Strings.ArgumentNegative);

            // show message and quit if no commands to replay
            if (world.History.Commands.Count == 0) {
                MessageBox.Show(MainWindow.Instance,
                    Global.Strings.DialogReplayNone, Global.Strings.TitleReplay,
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (turn < 0) {
                // ask user to specify starting turn
                var dialog = new Dialog.ChangeTurn(
                    world.CurrentTurn, Global.Strings.TitleReplayFromTurn);
                dialog.Owner = MainWindow.Instance;
                if (dialog.ShowDialog() != true) return;

                // get turn entered by user
                turn = dialog.Turn;
                Debug.Assert(turn >= 0);
                Debug.Assert(turn <= world.CurrentTurn);
            }

            // save current session data
            this._originalWorldState = session.WorldState;
            this._originalSelected = Session.MapView.SelectedSite;
            this._originalState = Session.State;

            // switch session to Replay state
            Session.State = SessionState.Replay;

            // show replay control message...
            MainWindow.Instance.StatusMessage.Push(Global.Strings.StatusReplay);

            // ...but wait for new world state
            MainWindow.Instance.BeginWait(Global.Strings.StatusReplayCommands);
            MainWindow.Instance.StatusMessage.Push();

            TaskEvents events = new TaskEvents(Application.Current.Dispatcher);
            events.TaskMessage += ((sender, args) =>
                MainWindow.Instance.StatusMessage.Text = args.Value);

            try {
                // create world state from scenario
                WorldState worldState = new WorldState();
                worldState.Initialize(events);

                // copy original turn count
                worldState.History.CopyFullTurns(this._originalWorldState.History);
                session.WorldState = worldState;
            }
            finally {
                MainWindow.Instance.StatusMessage.Pop();
                MainWindow.Instance.EndWait();
            }

            AsyncAction.Run(delegate {

                // skip to specified turn & faction
                this._commandIndex = 0;
                if (turn > 0 || faction > 0)
                    Skip(turn, faction);

                // set default map view to new world state
                AsyncAction.Invoke(() => Session.MapView.WorldState = session.WorldState);

                // show active faction's home if not skipped
                if (this._commandIndex == 0)
                    ShowFaction(session.WorldState.ActiveFaction);

                // enter Play state
                CurrentState = ReplayState.Play;
                PlayCommands();
            });
        }

        #endregion
        #region IDisposable Members

        /// <summary>
        /// Releases all resources used by the <see cref="ReplayManager"/> object.</summary>
        /// <remarks>
        /// <b>Dispose</b> sets <see cref="CurrentState"/> to <see cref="ReplayState.Stop"/> and
        /// clears all replay data.</remarks>

        public void Dispose() {
            CurrentState = ReplayState.Stop;
            Clear(true); // clear all replay data
        }

        #endregion
    }
}
