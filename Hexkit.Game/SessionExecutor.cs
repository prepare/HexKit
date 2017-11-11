using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Threading;

using Tektosyne;
using Tektosyne.Geometry;
using Tektosyne.Windows;

using Hexkit.Global;
using Hexkit.Graphics;
using Hexkit.Options;
using Hexkit.Scenario;
using Hexkit.World;
using Hexkit.World.Commands;
using Hexkit.World.Instructions;

namespace Hexkit.Game {
    #region Type Aliases

    // avoid confusion with System.Threading.ExecutionContext
    using ExecutionContext = Hexkit.World.Commands.ExecutionContext;

    #endregion

    /// <summary>
    /// Manages command execution for the current game session.</summary>
    /// <remarks><para>
    /// <b>SessionExecutor</b> derives from the <see cref="CommandExecutor"/> class to simplify
    /// command execution for the current <see cref="Session"/>.
    /// </para><para>
    /// The <see cref="CommandExecutor.ExecuteCommand"/> and <see
    /// cref="CommandExecutor.ProcessCommand"/> methods are overridden to display error messages if
    /// command execution fails, and to show display events and update relevant <see
    /// cref="Session"/> data otherwise.
    /// </para><para>
    /// <b>SessionExecutor</b> also provides a number of static methods that perform the display
    /// actions requested by the various event instructions of the Hexkit Command Language.
    /// </para><note type="caution">
    /// All <see cref="SessionExecutor"/> methods except for <see
    /// cref="SessionExecutor.ShowMessageEvent"/> must run on a background thread to facilitate
    /// display events and user interruption. The latter is communicated by setting the <see
    /// cref="SessionExecutor.AbortSignal"/>.</note></remarks>

    public sealed class SessionExecutor: CommandExecutor {
        #region Private Fields

        // property backers
        private static ManualResetEvent _abortSignal = new ManualResetEvent(false);

        #endregion
        #region AbortSignal

        /// <summary>
        /// Gets a <see cref="ManualResetEvent"/> that signals <see cref="ExecuteCommand"/> to skip
        /// any further delays or display actions.</summary>
        /// <value>
        /// A <see cref="ManualResetEvent"/> that is signaled whenever <see cref="ExecuteCommand"/>
        /// should skip any further delays or display actions. The default state is nonsignaled.
        /// </value>
        /// <remarks>
        /// <b>AbortSignal</b> is signaled when a <see cref="UserAction"/> is requested during
        /// command execution or history replay, and reset to nonsignaled when the execution or
        /// replay has ended.</remarks>

        public static ManualResetEvent AbortSignal {
            [DebuggerStepThrough]
            get { return SessionExecutor._abortSignal; }
        }

        #endregion
        #region ExecuteCommand

        /// <summary>
        /// Executes the specified command and adds it to the command history.</summary>
        /// <param name="worldState">
        /// The <see cref="WorldState"/> on which the specified <paramref name="command"/> is
        /// executed.</param>
        /// <param name="command">
        /// The <see cref="Command"/> to execute.</param>
        /// <param name="queued">
        /// <c>true</c> if <paramref name="command"/> was enqueued by the <see
        /// cref="CommandExecutor.QueueCommand"/> method; <c>false</c> if <paramref name="command"/>
        /// was directly supplied to the <see cref="ProcessCommand"/> method.</param>
        /// <exception cref="ArgumentException">
        /// <paramref name="worldState"/> does not equal the <see cref="Session.WorldState"/> of the
        /// current <see cref="Session"/>.</exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="worldState"/> or <paramref name="command"/> is a null reference.
        /// </exception>
        /// <exception cref="InvalidCommandException">
        /// The specified <paramref name="command"/> contains data that is invalid with respect to
        /// the specified <paramref name="worldState"/>.</exception>
        /// <remarks><para>
        /// <b>ExecuteCommand</b> attempts to execute the specified <paramref name="command"/> by
        /// calling the base class implementation of <see cref="CommandExecutor.ExecuteCommand"/>.
        /// </para><para>
        /// On success, <b>ExecuteCommand</b> shows any events that were generated and sets the <see
        /// cref="Session.WorldChanged"/> flag of the current <see cref="Session"/>.
        /// </para><para>
        /// If <paramref name="queued"/> is <c>true</c>, <b>ExecuteCommand</b> inserts additional
        /// delays before and during command execution, and scrolls the default <see
        /// cref="Session.MapView"/> to bring the affected sites into view. These additional actions
        /// are skipped if the <see cref="AbortSignal"/> is set, however.</para></remarks>

        protected override void ExecuteCommand(
            WorldState worldState, Command command, bool queued) {

            if (worldState == null)
                ThrowHelper.ThrowArgumentNullException("worldState");
            if (command == null)
                ThrowHelper.ThrowArgumentNullException("command");

            if (worldState != Session.Instance.WorldState)
                ThrowHelper.ThrowArgumentExceptionWithFormat("worldState",
                    Tektosyne.Strings.ArgumentNotEquals, "Session.Instance.WorldState");

            // get current delay for interactive replay
            int delay = ApplicationOptions.Instance.Game.Replay.Delay;

            // add delay between queued commands
            if (queued) AbortSignal.WaitOne(2 * delay, false);

            // validate & show command
            command.Validate(worldState);
            ShowCommand(command);

            PointI source = command.Source.Location;
            PointI target = command.Target.Location;
            MapView mapView = Session.MapView;

            if (queued && !AbortSignal.WaitOne(0, false)) {
                bool sourceValid = Finder.MapGrid.Contains(source);

                AsyncAction.Invoke(delegate {
                    // scroll command sites into view
                    mapView.ScrollIntoView(source, target);

                    // highlight source site if valid
                    if (sourceValid) {
                        mapView.SelectedSite = source;

                        // select first entity if specified
                        EntityReference[] entities = command.Entities;
                        if (entities != null && entities.Length > 0)
                            MainWindow.Instance.SelectedEntity = entities[0].Id;
                    }
                });

                // show valid source site for a while
                if (sourceValid)
                    AbortSignal.WaitOne(delay, false);
            }

            // execute command and add to history
            command.Execute(new ExecutionContext(worldState, QueueCommand, ShowCommandEvent));
            worldState.History.AddCommand(command, worldState.CurrentTurn);

            AsyncAction.Invoke(delegate {
                // move to target if valid, else update source
                if (Finder.MapGrid.Contains(target))
                    mapView.SelectedSite = target;
                else
                    MainWindow.Instance.UpdateSelection();

                // ensure command effects are shown
                mapView.Redraw();
            });

            // world state has changed
            Session.Instance.SetWorldChanged();
        }

        #endregion
        #region ProcessCommand

        /// <summary>
        /// Enqueues the specified command and executes all <see
        /// cref="CommandExecutor.QueuedCommands"/>.</summary>
        /// <param name="worldState">
        /// The <see cref="WorldState"/> on which all <see cref="CommandExecutor.QueuedCommands"/>
        /// are executed.</param>
        /// <param name="command">
        /// The <see cref="Command"/> to enqueue.</param>
        /// <returns>
        /// <c>true</c> if all <see cref="CommandExecutor.QueuedCommands"/> were successfully
        /// executed; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="worldState"/> or <paramref name="command"/> is a null reference.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// <b>ProcessCommand</b> was called on the <see cref="DispatcherObject.Dispatcher"/> thread
        /// of the current <see cref="Application"/> instance, rather than on a background thread.
        /// </exception>
        /// <remarks><para>
        /// <b>ProcessCommand</b> updates the <see cref="StatusBar"/> message and returns the result
        /// of the base class implementation of <see cref="CommandExecutor.ProcessCommand"/>, which
        /// is always <c>true</c>. If that call throws an <see cref="InvalidCommandException"/>,
        /// <b>ProcessCommand</b> displays its text and immediately returns <c>false</c>.
        /// </para><para>
        /// If the current session <see cref="Session.State"/> equals <see
        /// cref="SessionState.Human"/> or <see cref="SessionState.Selection"/>, it is changed to
        /// <see cref="SessionState.Command"/> while <b>ProcessCommand</b> executes, and then reset
        /// to <see cref="SessionState.Human"/>.</para></remarks>

        protected override bool ProcessCommand(WorldState worldState, Command command) {

            if (Application.Current.Dispatcher.CheckAccess())
                ThrowHelper.ThrowInvalidOperationException(Tektosyne.Strings.ThreadForeground);

            AsyncAction.Invoke(delegate {
                // switch human player to Command state
                if (Session.State == SessionState.Human || Session.State == SessionState.Selection)
                    Session.State = SessionState.Command;

                // show command execution message
                MainWindow.Instance.StatusMessage.Push(Global.Strings.StatusCommandExecuting);
            });

            try {
                return base.ProcessCommand(worldState, command);
            }
            catch (InvalidCommandException e) {
                AsyncAction.Invoke(delegate {
                    Mouse.OverrideCursor = null;

                    // notify user of command error
                    MessageDialog.Show(MainWindow.Instance,
                        Global.Strings.DialogCommandInvalid, Global.Strings.TitleCommandInvalid,
                        e, MessageBoxButton.OK, Images.Error);
                });

                return false;
            }
            finally {
                AsyncAction.Invoke(delegate {
                    MainWindow.Instance.StatusMessage.Pop();

                    // revert human player to default state
                    if (Session.State == SessionState.Command)
                        Session.State = SessionState.Human;
                });
            }
        }

        #endregion
        #region SelectEntityEvent

        /// <summary>
        /// Handles the entity selection event represented by the specified <see
        /// cref="Instruction"/>.</summary>
        /// <param name="instruction">
        /// The <see cref="SelectEntityInstruction"/> that represents the entity selection event to
        /// handle.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="instruction"/> is a null reference.</exception>
        /// <remarks><para>
        /// <b>SelectEntityEvent</b> selects the <see cref="Entity"/> indicated by the specified
        /// <paramref name="instruction"/> in the default <see cref="Session.MapView"/> and in the
        /// data view.
        /// </para><para>
        /// <b>SelectEntityEvent</b> does nothing if the specified <paramref name="instruction"/>
        /// indicates no <see cref="Entity"/>, or one that is unplaced.</para></remarks>

        public static void SelectEntityEvent(SelectEntityInstruction instruction) {
            if (instruction == null)
                ThrowHelper.ThrowArgumentNullException("instruction");

            // get entity identifier, if any
            string id = instruction.Id;
            if (String.IsNullOrEmpty(id)) return;

            // get placed entity, if any
            Entity entity = Session.Instance.WorldState.Entities[id];
            if (entity == null || entity.Site == null)
                return;

            // select site and entity
            AsyncAction.Invoke(delegate {
                Session.MapView.SelectedSite = entity.Site.Location;
                MainWindow.Instance.SelectedEntity = entity.Id;
            });
        }

        #endregion
        #region ShowCommand

        /// <summary>
        /// Shows the data of the specified <see cref="Command"/>.</summary>
        /// <param name="command">
        /// The <see cref="Command"/> whose data to show.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="command"/> is a null reference.</exception>
        /// <exception cref="PropertyValueException">
        /// The current session <see cref="Session.State"/> is <see cref="SessionState.Invalid"/>.
        /// </exception>
        /// <remarks>
        /// <b>ShowCommand</b> invokes <see cref="Object.ToString"/> on the specified <paramref
        /// name="command"/> and replaces the contents of the event view with the resulting string.
        /// </remarks>

        public static void ShowCommand(Command command) {
            if (command == null)
                ThrowHelper.ThrowArgumentNullException("command");

            if (Session.State == SessionState.Invalid)
                ThrowHelper.ThrowPropertyValueExceptionWithFormat("Session.State",
                    Session.State, Tektosyne.Strings.PropertyIsValue, SessionState.Invalid);

            AsyncAction.Invoke(delegate {
                // replace message text with command text plus line break
                MainWindow.Instance.EventMessage.Text = command.ToString();
                MainWindow.Instance.EventMessage.AppendText(Environment.NewLine);
            });
        }

        #endregion
        #region ShowCommandEvent

        /// <summary>
        /// Handles the command event represented by the specified <see cref="Instruction"/>.
        /// </summary>
        /// <param name="instruction">
        /// The <see cref="Instruction"/> that represents the command event to handle.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="instruction"/> is a null reference.</exception>
        /// <exception cref="PropertyValueException">
        /// The current session <see cref="Session.State"/> is <see cref="SessionState.Invalid"/>.
        /// </exception>
        /// <remarks><para>
        /// <b>ShowCommandEvent</b> takes the following actions, depending on the exact type of the
        /// specified <paramref name="instruction"/>:
        /// </para><list type="table"><listheader>
        /// <term>Type</term><description>Action</description>
        /// </listheader><item>
        /// <term><see cref="SelectEntityInstruction"/></term>
        /// <description>Call <see cref="SelectEntityEvent"/>.</description>
        /// </item><item>
        /// <term><see cref="ImageInstruction"/></term>
        /// <description>Call <see cref="ShowImageEvent"/>.</description>
        /// </item><item>
        /// <term><see cref="MessageInstruction"/></term>
        /// <description>Call <see cref="ShowMessageEvent"/>.</description>
        /// </item><item>
        /// <term>Other</term><description>Do nothing.</description>
        /// </item></list></remarks>

        public static void ShowCommandEvent(Instruction instruction) {
            if (instruction == null)
                ThrowHelper.ThrowArgumentNullException("instruction");

            if (Session.State == SessionState.Invalid)
                ThrowHelper.ThrowPropertyValueExceptionWithFormat("Session.State",
                    Session.State, Tektosyne.Strings.PropertyIsValue, SessionState.Invalid);

            MessageInstruction message = instruction as MessageInstruction;
            if (message != null) {
                AsyncAction.Invoke(() =>
                    ShowMessageEvent(message, MainWindow.Instance.EventMessage, true));
                return;
            }

            SelectEntityInstruction selectEntity = instruction as SelectEntityInstruction;
            if (selectEntity != null) {
                SelectEntityEvent(selectEntity);
                return;
            }

            ImageInstruction image = instruction as ImageInstruction;
            if (image != null) {
                ShowImageEvent(image);
                return;
            }
        }

        #endregion
        #region ShowImageEvent

        /// <summary>
        /// Handles the image event represented by the specified <see cref="Instruction"/>.
        /// </summary>
        /// <param name="instruction">
        /// The <see cref="ImageInstruction"/> that represents the image event to handle.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="instruction"/> is a null reference.</exception>
        /// <remarks><para>
        /// <b>ShowImageEvent</b> shows the image event represented by the specified <paramref
        /// name="instruction"/> on the default <see cref="Session.MapView"/>.
        /// </para><para>
        /// If the <see cref="ImageInstruction.Delay"/> of the specified <paramref
        /// name="instruction"/> is zero or negative, <b>ShowImageEvent</b> substitutes a default
        /// delay of 250 msec. The specified or default delay is then adjusted by the current <see
        /// cref="ReplayOptions.Speed"/> setting, as follows:
        /// </para><list type="table"><listheader>
        /// <term>Speed</term><description>Delay</description>
        /// </listheader><item>
        /// <term><see cref="ReplaySpeed.Slow"/></term><description>Doubled</description>
        /// </item><item>
        /// <term><see cref="ReplaySpeed.Medium"/></term><description>Unchanged</description>
        /// </item><item>
        /// <term><see cref="ReplaySpeed.Fast"/></term><description>Halved</description>
        /// </item><item>
        /// <term><see cref="ReplaySpeed.Turbo"/></term><description>Skipped</description>
        /// </item></list><para>
        /// <b>ShowImageEvent</b> returns immediately without showing the specified <paramref
        /// name="instruction"/> if the current <see cref="ReplayOptions.Speed"/> setting is <see
        /// cref="ReplaySpeed.Turbo"/>.</para></remarks>

        public static void ShowImageEvent(ImageInstruction instruction) {
            if (instruction == null)
                ThrowHelper.ThrowArgumentNullException("instruction");

            // skip image events at Turbo speed
            ReplaySpeed speed = ApplicationOptions.Instance.Game.Replay.Speed;
            if (speed == ReplaySpeed.Turbo) return;

            // get event parameters
            EntityClass entityClass = instruction.EntityClass;
            PointI[] sites = instruction.Sites.ToArray();
            int delay = instruction.Delay;

            // default delay is 250 msec
            if (delay <= 0) delay = 250;

            // adjust delay for current replay speed
            switch (speed) {
                case ReplaySpeed.Slow: delay *= 2; break;
                case ReplaySpeed.Fast: delay /= 2; break;
            }

            // move or show image on default map view
            bool move = (instruction is MoveImageInstruction);
            Session.MapView.ShowImage(entityClass, sites, move, delay, AbortSignal);
        }

        #endregion
        #region ShowMessageEvent

        /// <summary>
        /// Handles the message event represented by the specified <see cref="Instruction"/>.
        /// </summary>
        /// <param name="instruction">
        /// The <see cref="MessageInstruction"/> that represents the message event to handle.
        /// </param>
        /// <param name="textBox">
        /// The <see cref="TextBoxBase"/> control to which to append the message.</param>
        /// <param name="showDialog">
        /// <c>true</c> to display a dialog if requested by the specified <paramref
        /// name="instruction"/>; otherwise, <c>false</c>.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="instruction"/> or <paramref name="textBox"/> is a null reference.
        /// </exception>
        /// <remarks><para>
        /// <b>ShowMessageEvent</b> creates a text message from the data of the specified <paramref
        /// name="instruction"/> and appends it to the specified <paramref name="textBox"/> control.
        /// </para><para>
        /// If <paramref name="instruction"/> is of type <see cref="ShowMessageDialogInstruction"/>
        /// and <paramref name="showDialog"/> is <c>true</c>, <b>ShowMessageEvent</b> also shows the
        /// message text in a <see cref="Dialog.ShowEvent"/> dialog.</para></remarks>

        public static void ShowMessageEvent(MessageInstruction instruction,
            TextBoxBase textBox, bool showDialog) {

            if (instruction == null)
                ThrowHelper.ThrowArgumentNullException("instruction");
            if (textBox == null)
                ThrowHelper.ThrowArgumentNullException("textBox");

            // show message in a modal dialog?
            bool dialog = (instruction is ShowMessageDialogInstruction);

            // get mandatory summary text
            string summary = StringUtility.Validate(
                instruction.Text, Global.Strings.LabelEventUnknown);

            // get details text (mandatory for dialog)
            string details = instruction.Details;
            if (dialog)
                details = StringUtility.Validate(details, Global.Strings.InfoEventUnknown);

            // get optional faction and entity names
            FactionClass faction = instruction.Faction;
            string[] names = instruction.Names.ToArray();

            // create caption from faction and summary
            StringBuilder caption = new StringBuilder();
            if (faction != null) {
                caption.Append(faction.Name);
                caption.Append(": ");
            }
            caption.Append(summary);

            // create message from details and names
            StringBuilder message = new StringBuilder();
            if (details != null) message.Append(details);

            // append specified names to message
            if (names != null)
                foreach (string name in names) {
                    if (message.Length > 0) message.Append(Environment.NewLine);
                    message.Append('\t');
                    message.Append(name);
                }

            // append message to event view text
            textBox.AppendText("– "); // en dash
            textBox.AppendText(caption.ToString());
            textBox.AppendText(Environment.NewLine);

            if (message.Length > 0) {
                textBox.AppendText(message.ToString());
                textBox.AppendText(Environment.NewLine);
            }

            // show message as dialog if desired
            if (dialog && showDialog) {

                // temporarily restore normal cursor if in wait mode
                var cursor = Mouse.OverrideCursor;
                Mouse.OverrideCursor = null;
                Dialog.ShowEvent.Show(MainWindow.Instance, message.ToString(), caption.ToString());
                Mouse.OverrideCursor = cursor;
            }
        }

        #endregion
    }
}
