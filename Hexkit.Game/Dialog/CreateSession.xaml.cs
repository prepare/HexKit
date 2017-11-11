using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

using Tektosyne;
using Tektosyne.Windows;

using Hexkit.Global;
using Hexkit.Graphics;
using Hexkit.Players;
using Hexkit.Scenario;
using Hexkit.World;
using Hexkit.World.Commands;

namespace Hexkit.Game.Dialog {
    #region Type Aliases

    // avoid confusion with System.Threading.ExecutionContext
    using ExecutionContext = Hexkit.World.Commands.ExecutionContext;

    #endregion

    /// <summary>
    /// Shows a dialog with status updates while a new <see cref="Session"/> is being initialized.
    /// </summary>
    /// <remarks><para>
    /// Please refer to the "Create Session" page of the "Game Dialogs" section in the application
    /// help file for details on this dialog.
    /// </para><para>
    /// <b>CreateSession</b> is a modal dialog that runs the actual task of initializing a new <see
    /// cref="Session"/> instance in a background thread. The former prevents the user from
    /// manipulating the application's GUI while a new game is starting, and the latter prevents
    /// Windows from marking the application as "frozen" due to a blocked message queue.
    /// </para><para>
    /// <b>CreateSession</b> contains a <b>Close</b> button that does nothing. It is apparently
    /// impossible to display a Windows Forms dialog with a border and title bar, but without a
    /// <b>Close</b> button.</para></remarks>

    public partial class CreateSession: Window {
        #region CreateSession(String, Boolean)

        /// <summary>
        /// Initializes a new instance of the <see cref="CreateSession"/> class.</summary>
        /// <param name="path">
        /// The absolute file path to the XML scenario or session description file used to initalize
        /// the new <see cref="Session"/> instance.</param>
        /// <param name="openScenario">
        /// <c>true</c> if <paramref name="path"/> is a scenario description file; <c>false</c> if
        /// <paramref name="path"/> is a session description file.</param>
        /// <exception cref="ArgumentException">
        /// <paramref name="path"/> is not an absolute file path.</exception>
        /// <exception cref="ArgumentNullOrEmptyException">
        /// <paramref name="path"/> is a null reference or an empty string.</exception>
        /// <exception cref="PropertyValueException">
        /// The current session <see cref="Session.Instance"/> is a null reference.</exception>
        /// <remarks>
        /// <b>CreateSession</b> reports success or failure with a <see cref="Window.DialogResult"/>
        /// of <c>true</c> or <c>false</c>, respectively. In the case of failure, the <see
        /// cref="SessionException"/> property may contain additional error information.</remarks>

        public CreateSession(string path, bool openScenario) {

            if (String.IsNullOrEmpty(path))
                ThrowHelper.ThrowArgumentNullOrEmptyException("path");
            if (!Path.IsPathRooted(path))
                ThrowHelper.ThrowArgumentException("path", Tektosyne.Strings.ArgumentNotRooted);

            if (Session.Instance == null)
                ThrowHelper.ThrowPropertyValueException(
                    "Session.Instance", Tektosyne.Strings.PropertyNull);

            this._path = path;
            this._openScenario = openScenario;

            InitializeComponent();
            Title = (openScenario ?
                Global.Strings.TitleScenarioStarting : Global.Strings.TitleGameResuming);
        }

        #endregion
        #region Private Fields

        // construction arguments
        private readonly string _path;
        private readonly bool _openScenario;

        // variables for background thread
        private Thread _thread;
        private TaskEvents _threadEvents;
        private bool _threadResult;

        // delegates for cross-thread invocation
        private delegate bool ConfirmCommandErrorCallback(InvalidCommandException e);
        private delegate bool ConfirmScenarioChangedCallback();

        #endregion
        #region SessionException

        /// <summary>
        /// The <see cref="Exception"/> that occurred while the <see cref="Session"/> was
        /// initialized.</summary>
        /// <value><para>
        /// The <see cref="Exception"/> that occurred while the current session <see
        /// cref="Session.Instance"/> was initialized.
        /// </para><para>-or-</para><para>
        /// A null reference if initialization was successfully completed. The default is a null
        /// reference.</para></value>

        public Exception SessionException { get; private set; }

        #endregion
        #region Private Methods
        #region ConfirmCommandError

        /// <summary>
        /// Asks the user to confirm the specified <see cref="InvalidCommandException"/>.</summary>
        /// <param name="exception">
        /// The <see cref="InvalidCommandException"/> to confirm.</param>
        /// <returns>
        /// <c>true</c> if the user confirms that the command replay should continue in spite of the
        /// specified <paramref name="exception"/>; otherwise, <c>false</c>.</returns>
        /// <remarks>
        /// <b>ConfirmCommandError</b> conforms to the <see cref="ConfirmCommandErrorCallback"/>
        /// delegate for cross-thread invocations.</remarks>

        private bool ConfirmCommandError(InvalidCommandException exception) {

            string message = String.Format(ApplicationInfo.Culture,
                Global.Strings.DialogCommandResume, MasterSection.Instance.Path);

            bool? result = MessageDialog.Show(this, message,
                Global.Strings.TitleCommandInvalid, exception,
                MessageBoxButton.OKCancel, Images.Error);

            return (result == true);
        }

        #endregion
        #region ConfirmScenarioChanged

        /// <summary>
        /// Asks the user to confirm a changed scenario file.</summary>
        /// <returns>
        /// <c>true</c> if the user confirms that the command replay should continue in spite of the
        /// changed scenario file; otherwise, <c>false</c>.</returns>
        /// <remarks>
        /// <b>ConfirmScenarioChanged</b> conforms to the <see
        /// cref="ConfirmScenarioChangedCallback"/> delegate for cross-thread invocations.</remarks>

        private bool ConfirmScenarioChanged() {

            MessageBoxResult result = MessageBox.Show(this,
                Global.Strings.DialogScenarioChanged, Global.Strings.TitleScenarioChanged,
                MessageBoxButton.OKCancel, MessageBoxImage.Warning, MessageBoxResult.Cancel);

            return (result == MessageBoxResult.OK);
        }

        #endregion
        #region CreateMapView

        /// <summary>
        /// Creates the default <see cref="Session.MapView"/> for the current <see
        /// cref="Session.WorldState"/>.</summary>
        /// <remarks><para>
        /// <b>CreateMapView</b> creates a new <see cref="MapView"/> object whose <see
        /// cref="MapView.Id"/> is the literal string "default". This matches the identifier by
        /// which the <see cref="Session.MapView"/> property of the <see cref="Session"/> class
        /// locates the default map view.
        /// </para><note type="implementnotes">
        /// Callers on a background thread must use <see cref="Dispatcher.Invoke"/> to invoke
        /// <b>CreateMapView</b> because the new <see cref="MapView"/> is automatically attached to
        /// the current <see cref="MainWindow"/> instance.</note></remarks>

        private void CreateMapView() {

            MapViewManager manager = MapViewManager.Instance;
            Debug.Assert(manager != null);

            WorldState world = Session.Instance.WorldState;
            Debug.Assert(world != null);

            // create default map view for current world state
            MainWindow window = MainWindow.Instance;
            MapView mapView = manager.CreateView("default", world,
                window.MapViewHost, window.OnMapMouseDown, window.OnMapMouseWheel);

            Debug.Assert(mapView == Session.MapView);

            // attach handler for automatic data view updates
            mapView.SelectedSiteChanged += window.OnSiteSelected;
        }

        #endregion
        #region CreateMapViewManager

        /// <summary>
        /// Creates a new <see cref="MapViewManager"/> instance based on the current scenario.
        /// </summary>
        /// <remarks><para>
        /// <b>CreateMapViewManager</b> creates a new <see cref="MapViewManager"/> instance. To save
        /// memory, <b>CreateMapViewManager</b> calls <see cref="ImageSection.Unload"/> on the
        /// current <see cref="ImageSection"/> once a new instance has been created.
        /// </para><para>
        /// <b>CreateMapViewManager</b> raises the <see cref="TaskEvents.TaskProgress"/> event twice
        /// with a value of one.
        /// </para><note type="implementnotes">
        /// The <see cref="MapViewManager"/> is created on the foreground thread for compatiblity
        /// with the default <see cref="MapView"/> that will be attached to the current <see
        /// cref="MainWindow"/> instance.</note></remarks>

        private void CreateMapViewManager() {
            this._threadEvents.OnTaskMessage(this, Global.Strings.StatusMapViewCreating);

            // create map view manager
            Dispatcher.Invoke(() => MapViewManager.CreateInstance(Dispatcher));
            this._threadEvents.OnTaskProgress(this, 1);

            // unload image file bitmaps
            MasterSection.Instance.Images.Unload();
            this._threadEvents.OnTaskProgress(this, 1);
        }

        #endregion
        #region CreateScenario

        /// <summary>
        /// Creates a new <see cref="MasterSection"/> instance from the specified scenario
        /// description file.</summary>
        /// <param name="path">
        /// The file path to the XML scenario description to open.</param>
        /// <exception cref="ArgumentNullOrEmptyException">
        /// <paramref name="path"/> is a null reference or an empty string.</exception>
        /// <remarks>
        /// <b>CreateScenario</b> raises the <see cref="TaskEvents.TaskProgress"/> event four times
        /// with a value of one.</remarks>

        private void CreateScenario(string path) {
            if (String.IsNullOrEmpty(path))
                ThrowHelper.ThrowArgumentNullOrEmptyException("path");

            this._threadEvents.OnTaskMessage(this, Global.Strings.StatusScenarioReading);

            // create scenario instance
            MasterSection.CreateInstance();
            this._threadEvents.OnTaskProgress(this, 1);

            // load specified XML scenario
            MasterSection.Instance.Load(path);
            this._threadEvents.OnTaskProgress(this, 1);

            // read media files from disk
            MasterSection.Instance.Images.Load();
            this._threadEvents.OnTaskProgress(this, 1);

            // load associated rule script
            MasterSection.Instance.Rules.Load();
            this._threadEvents.OnTaskProgress(this, 1);
        }

        #endregion
        #region OpenScenario

        /// <summary>
        /// Initializes the current <see cref="Session"/> instance from the scenario description
        /// file supplied to the constructor.</summary>
        /// <returns>
        /// <c>true</c> if the current session <see cref="Session.Instance"/> was successfully
        /// initialized; otherwise, <c>false</c>.</returns>

        private bool OpenScenario() {

            Session session = Session.Instance;
            Debug.Assert(session != null);

            PlayerManager players = PlayerManager.Instance;
            Debug.Assert(players != null);

            // create scenario from file
            CreateScenario(this._path);

            // initialize world state from scenario
            session.WorldState.Initialize(this._threadEvents);

            // initialize rule script with complete world state
            var factory = (IRulesFactory) MasterSection.Instance.Rules.Factory;
            factory.Initialize(session.WorldState);

            // intialize player data from world state
            players.Initialize(session.WorldState.Factions);

            // find local e-mail address
            HumanPlayer human = players.GetLocalHuman(session.WorldState, true);
            session.LocalEmail = human.Email;

            // create default map view
            CreateMapViewManager();
            Dispatcher.Invoke(CreateMapView);

            return true;
        }

        #endregion
        #region OpenSession

        /// <summary>
        /// Initializes the current <see cref="Session"/> instance from the session description file
        /// supplied to the constructor.</summary>
        /// <returns>
        /// <c>true</c> if the current session <see cref="Session.Instance"/> was successfully
        /// initialized; otherwise, <c>false</c>.</returns>

        private bool OpenSession() {

            Session session = Session.Instance;
            Debug.Assert(session != null);

            PlayerManager players = PlayerManager.Instance;
            Debug.Assert(players != null);

            // load session file
            this._threadEvents.OnTaskMessage(this, Global.Strings.StatusGameReading);
            session.LoadDirect(this._path);

            // create referenced scenario
            CreateScenario(session.ScenarioPath);

            // compare saved and actual hash code
            if (session.ScenarioHash != MasterSection.Instance.Hash) {

                // ask user to confirm changed scenario
                object result = Dispatcher.Invoke(
                    new ConfirmScenarioChangedCallback(ConfirmScenarioChanged));

                if ((bool) result == false)
                    return false;
            }

            // initialize world state from scenario
            session.WorldState.Initialize(this._threadEvents);

            // initialize rule script with complete world state
            ((IRulesFactory) MasterSection.Instance.Rules.Factory).Initialize(session.WorldState);

            // validate player data in session file
            players.ValidatePlayers(session.WorldState.Factions);

            // try replaying command history
            if (!ReplayHistory()) return false;

            // find local e-mail address
            HumanPlayer human = players.GetLocalHuman(session.WorldState, false);
            session.LocalEmail = human.Email;

            // create default map view
            CreateMapViewManager();
            Dispatcher.Invoke(CreateMapView);

            return true;
        }

        #endregion
        #region ReplayHistory

        /// <summary>
        /// Replays all commands in the current <see cref="WorldState.History"/>.</summary>
        /// <returns>
        /// <c>true</c> if all history commands were successfully replayed; otherwise, <c>false</c>.
        /// </returns>
        /// <remarks><para>
        /// <b>ReplayHistory</b> is called by <see cref="OpenSession"/> to complete the restoration
        /// of a saved game.
        /// </para><para>
        /// <b>ReplayHistory</b> shows a dialog when an exception occurs during command replay, and
        /// returns <c>false</c> if the user cancels the replay. No exceptions are propagated to the
        /// caller.
        /// </para><para>
        /// <b>ReplayHistory</b> raises the <see cref="TaskEvents.TaskProgress"/> event three times
        /// with an increment of one.</para></remarks>

        private bool ReplayHistory() {

            this._threadEvents.Timer.Restart();
            this._threadEvents.OnTaskMessage(this, Global.Strings.StatusReplayCommands);

            WorldState world = Session.Instance.WorldState;
            IList<Command> commands = world.History.Commands;
            ExecutionContext context = new ExecutionContext(world, null, null);

            // compute thresholds for progress counter
            int progressIncrement = (commands.Count - 1) / 3;
            int progress = progressIncrement / 2;
            string progressMessage = Global.Strings.StatusReplayCommandsCount;

            for (int i = 0; i < commands.Count; i++) {

                // advance progress counter
                if (i >= progress) {
                    this._threadEvents.OnTaskProgress(this, 1);
                    progress += progressIncrement;
                }

                // show running count every 0.5 seconds
                if (this._threadEvents.RestartTimer(500L))
                    this._threadEvents.OnTaskMessage(this, progressMessage, i + 1, commands.Count);

                try {
                    // validate & execute command
                    Command command = commands[i];
                    command.Validate(world);
                    command.Execute(context);
                }
                catch (InvalidCommandException e) {

                    // ask user to confirm erroneous command
                    object result = Dispatcher.Invoke(
                        new ConfirmCommandErrorCallback(ConfirmCommandError), e);

                    if ((bool) result == false)
                        return false;
                }
            }

            return true;
        }

        #endregion
        #region ThreadMain

        /// <summary>
        /// Initializes the current <see cref="Session"/> instance in a background thread.</summary>
        /// <remarks><para>
        /// <b>ThreadMain</b> is started by <see cref="OnContentRendered"/> and raises the <see
        /// cref="TaskEvents.TaskComplete"/> event when the operation has completed.
        /// </para><para>
        /// <b>ThreadMain</b> catches any exception that occurs, stores it in the <see
        /// cref="SessionException"/> property unless it is a <see cref="ThreadAbortException"/>,
        /// and returns <c>false</c> in that case.
        /// </para><para>
        /// Otherwise, <b>ThreadMain</b> returns either <c>true</c> or <c>false</c>, depending on
        /// whether the scenario or session description file was successfully opened.
        /// </para></remarks>

        private void ThreadMain() {
            try {
                // attempt to open scenario or session file
                if (this._openScenario ? OpenScenario() : OpenSession())
                    this._threadResult = true;
            }
            catch (Exception e) {
                // broadcast unexpected exception
                if (!(e is ThreadAbortException))
                    SessionException = e;
            }
            finally {
                // notify client that we're done
                this._threadEvents.OnTaskComplete(this);
            }
        }

        #endregion
        #endregion
        #region HelpExecuted

        /// <summary>
        /// Handles the <see cref="CommandBinding.Executed"/> event for the <see
        /// cref="ApplicationCommands.Help"/> command.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// An <see cref="ExecutedRoutedEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>HelpExecuted</b> opens the application help file on the help page for the <see
        /// cref="CreateSession"/> dialog.</remarks>

        private void HelpExecuted(object sender, ExecutedRoutedEventArgs args) {
            args.Handled = true;
            ApplicationUtility.ShowHelp("DlgCreateSession.html");
        }

        #endregion
        #region OnClosing

        /// <summary>
        /// Raises and handles the <see cref="Window.Closing"/> event.</summary>
        /// <param name="args">
        /// A <see cref="CancelEventArgs"/> object containing event data.</param>
        /// <remarks><para>
        /// <b>OnClosing</b> raises the <see cref="Window.Closing"/> event by calling the base class
        /// implementation of <see cref="Window.OnClosing"/> with the specified <paramref
        /// name="args"/>.
        /// </para><para>
        /// If the event was not already requested to <see cref="CancelEventArgs.Cancel"/>,
        /// <b>OnClosing</b> now cancels the event if the background thread is still running.
        /// </para><para>
        /// Otherwise, <b>OnClosing</b> handles the <see cref="Window.Closing"/> event by setting
        /// the <see cref="Window.DialogResult"/> to the result of the background thread.
        /// </para></remarks>

        protected override void OnClosing(CancelEventArgs args) {
            base.OnClosing(args);
            if (args.Cancel) return;

            if (this._thread != null && this._thread.IsAlive)
                args.Cancel = true;
            else {
                DialogResult = this._threadResult;
                MainWindow.Instance.StatusMessage.Pop();
            }
        }

        #endregion
        #region OnContentRendered

        /// <summary>
        /// Raises and handles the <see cref="Window.ContentRendered"/> event.</summary>
        /// <param name="args">
        /// An <see cref="EventArgs"/> object containing event data.</param>
        /// <remarks><para>
        /// <b>OnContentRendered</b> raises the <see cref="Window.ContentRendered"/> event by
        /// calling the base class implementation of <see cref="Window.OnContentRendered"/>.
        /// </para><para>
        /// <b>OnContentRendered</b> then handles the <see cref="Window.ContentRendered"/> event by
        /// starting a background thread that attempts to open the XML scenario or session
        /// description file supplied to the constructor. The background thread will raise the <see
        /// cref="TaskEvents.TaskComplete"/> event when the operation has completed.
        /// </para></remarks>

        protected override void OnContentRendered(EventArgs args) {
            base.OnContentRendered(args);

            // show activity message on status bar
            MainWindow.Instance.StatusMessage.Push(this._openScenario ?
                Global.Strings.StatusScenarioStarting : Global.Strings.StatusGameOpening);

            // set progress bar range
            StatusProgress.Minimum = 0;
            StatusProgress.Maximum = (this._openScenario ? 13 : 16);

            // attach task events to dialog methods
            this._threadEvents = new TaskEvents(Dispatcher);
            this._threadEvents.TaskMessage += ((sender, e) => StatusInfo.Text = e.Value);
            this._threadEvents.TaskProgress += ((sender, e) => StatusProgress.Value += e.Value);
            this._threadEvents.TaskComplete += ((sender, e) =>
                { this._thread.Join(500); this._thread = null; Close(); });

            // create and start background thread
            this._thread = new Thread(new ThreadStart(ThreadMain));
            this._thread.IsBackground = true;
            this._thread.Name = "CreateSession";
            this._thread.Start();
        }

        #endregion
    }
}
