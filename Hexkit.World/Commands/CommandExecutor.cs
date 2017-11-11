using System;
using System.Collections.Generic;
using System.Diagnostics;

using Tektosyne;
using Tektosyne.Geometry;
using Hexkit.Scenario;

namespace Hexkit.World.Commands {

    /// <summary>
    /// Provides a framework for queuing and executing commands under the control of a human or
    /// computer player.</summary>
    /// <remarks><para>
    /// <b>CommandExecutor</b> simplifies the execution of game commands with helper methods that
    /// perform all required calls and transform their arguments into <see cref="Command"/>
    /// parameters where necessary.
    /// </para><para>
    /// Commands may also request the execution of additional commands by calling back to the <see
    /// cref="CommandExecutor.QueueCommand"/> method. All such commands are added to a command queue
    /// in the order in which they arrive. Command execution continues until the queue is empty.
    /// </para><para>
    /// Moreover, the processing of the command queue and the execution of individual commands are
    /// handled by two virtual methods which can be overridden to perform error handling and other
    /// client-specific actions.
    /// </para><note type="caution">
    /// Only use <b>CommandExecutor</b> methods when adding new commands to the command history! You
    /// must directly validate and execute <see cref="Command"/> objects when replaying commands
    /// that are already present in the command history.</note></remarks>

    public class CommandExecutor {
        #region Private Fields

        // property backers
        private readonly Queue<Command> _queuedCommands = new Queue<Command>();

        #endregion
        #region QueuedCommands

        /// <summary>
        /// Gets a list of all commands still queued for execution.</summary>
        /// <value>
        /// A <see cref="Queue{Command}"/> containing all <see cref="Command"/> objects that were
        /// queued for execution by <see cref="QueueCommand"/>, and have not yet been executed. The
        /// default is an empty collection.</value>
        /// <remarks><para>
        /// <b>QueuedCommands</b> never returns a null reference. This property never changes once
        /// the object has been constructed.
        /// </para><para>
        /// Please refer to <see cref="ProcessCommand"/> for details on the operation of the
        /// <b>QueuedCommands</b> collection.</para></remarks>

        protected Queue<Command> QueuedCommands {
            [DebuggerStepThrough]
            get { return this._queuedCommands; }
        }

        #endregion
        #region ExecuteCommand

        /// <summary>
        /// Executes the specified <see cref="Command"/> and adds it to the command history.
        /// </summary>
        /// <param name="worldState">
        /// The <see cref="WorldState"/> on which to execute the specified <paramref
        /// name="command"/>.</param>
        /// <param name="command">
        /// The <see cref="Command"/> to execute.</param>
        /// <param name="queued">
        /// <c>true</c> if <paramref name="command"/> was enqueued by the <see cref="QueueCommand"/>
        /// method; <c>false</c> if <paramref name="command"/> was directly supplied to the <see
        /// cref="ProcessCommand"/> method.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="worldState"/> or <paramref name="command"/> is a null reference.
        /// </exception>
        /// <exception cref="InvalidCommandException">
        /// The specified <paramref name="command"/> contains data that is invalid with respect to
        /// the specified <paramref name="worldState"/>.</exception>
        /// <remarks><para>
        /// <b>ExecuteCommand</b> executes the specified <paramref name="command"/> instance on the
        /// specified <paramref name="worldState"/>, and then adds the <paramref name="command"/> to
        /// the <see cref="WorldState.History"/> of the <paramref name="worldState"/>. The <paramref
        /// name="queued"/> parameter is ignored.
        /// </para><para>
        /// Derived classes may override <b>ExecuteCommand</b> to perform additional actions upon
        /// successful command execution, such as updating map views.</para></remarks>

        protected virtual void ExecuteCommand(
            WorldState worldState, Command command, bool queued) {

            if (worldState == null)
                ThrowHelper.ThrowArgumentNullException("worldState");
            if (command == null)
                ThrowHelper.ThrowArgumentNullException("command");

            command.Validate(worldState);
            command.Execute(new ExecutionContext(worldState, QueueCommand, null));
            worldState.History.AddCommand(command, worldState.CurrentTurn);
        }

        #endregion
        #region ProcessCommand

        /// <summary>
        /// Enqueues the specified <see cref="Command"/> and executes all <see
        /// cref="QueuedCommands"/>.</summary>
        /// <param name="worldState">
        /// The <see cref="WorldState"/> on which to execute all <see cref="QueuedCommands"/>.
        /// </param>
        /// <param name="command">
        /// The <see cref="Command"/> to enqueue.</param>
        /// <returns>
        /// Always <c>true</c>. Any exceptions thrown by <see cref="ExecuteCommand"/> are propagated
        /// to the caller. Derived classes may change this behavior.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="worldState"/> or <paramref name="command"/> is a null reference.
        /// </exception>
        /// <exception cref="InvalidCommandException">
        /// The specified <paramref name="command"/> or another queued command contains data that is
        /// invalid with respect to the specified <paramref name="worldState"/>.</exception>
        /// <remarks><para>
        /// <b>ProcessCommand</b> performs the following actions:
        /// </para><list type="number"><item>
        /// Add the specified <paramref name="command"/> to <see cref="QueuedCommands"/>.
        /// </item><item>
        /// Remove the next <see cref="Command"/> from <see cref="QueuedCommands"/>.
        /// </item><item>
        /// Call <see cref="ExecuteCommand"/> with the specified <paramref name="worldState"/> and
        /// the dequeued command. This call may add new elements to <see cref="QueuedCommands"/>.
        /// </item><item>
        /// Return to step 2 until <see cref="QueuedCommands"/> is empty.
        /// </item></list><para>
        /// If any <see cref="ExecuteCommand"/> call throws an exception, <b>ProcessCommand</b>
        /// removes all remaining <see cref="QueuedCommands"/> before returning.</para></remarks>

        protected virtual bool ProcessCommand(WorldState worldState, Command command) {

            if (worldState == null)
                ThrowHelper.ThrowArgumentNullException("worldState");
            if (command == null)
                ThrowHelper.ThrowArgumentNullException("command");

            // start queue with specified command
            QueuedCommands.Enqueue(command);
            bool queued = false;

            try {
                // process all queued commands
                while (QueuedCommands.Count > 0) {
                    command = QueuedCommands.Dequeue();

                    // attempt to execute next command
                    ExecuteCommand(worldState, command, queued);

                    // were new commands enqueued?
                    queued = (QueuedCommands.Count > 0);
                }

                return true;
            }
            finally {
                // clear queue in case of error
                QueuedCommands.Clear();
            }
        }

        #endregion
        #region QueueCommand

        /// <summary>
        /// Adds the specified <see cref="Command"/> to the <see cref="QueuedCommands"/> collection.
        /// </summary>
        /// <param name="command">
        /// The <see cref="Command"/> to enqueue.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="command"/> is a null reference.</exception>
        /// <exception cref="InvalidCommandException">
        /// <paramref name="command"/> is a <see cref="BeginTurnCommand"/> or an <see
        /// cref="EndTurnCommand"/>.</exception>
        /// <remarks>
        /// <b>QueueCommand</b> conforms to the <see cref="QueueCommandCallback"/> delegate.
        /// </remarks>

        protected void QueueCommand(Command command) {
            if (command == null)
                ThrowHelper.ThrowArgumentNullException("command");

            if (command is BeginTurnCommand || command is EndTurnCommand)
                InvalidCommandException.ThrowNewWithFormat(
                    Global.Strings.CommandQueueInvalid, command.Name);

            QueuedCommands.Enqueue(command);
        }

        #endregion
        #region Command Methods
        #region ExecuteAttack

        /// <summary>
        /// Executes an <see cref="AttackCommand"/> with the specified units and target location.
        /// </summary>
        /// <param name="worldState">
        /// The <see cref="WorldState"/> on which to execute the <see cref="AttackCommand"/>.
        /// </param>
        /// <param name="units">
        /// An <see cref="IList{T}"/> containing the <see cref="Unit"/> objects that perform the
        /// attack.</param>
        /// <param name="target">
        /// The coordinates of the <see cref="Site"/> that is the target of the <see
        /// cref="AttackCommand"/>.</param>
        /// <returns>
        /// <c>true</c> if the <see cref="AttackCommand"/> was executed successfully; otherwise,
        /// <c>false</c>.</returns>
        /// <exception cref="ArgumentException">
        /// <paramref name="units"/> contains a null reference.</exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="worldState"/> is a null reference.</exception>
        /// <exception cref="ArgumentNullOrEmptyException">
        /// <paramref name="units"/> is a null reference or an empty collection.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="target"/> specifies one or two negative coordinates.</exception>
        /// <remarks>
        /// <b>ExecuteAttack</b> calls <see cref="ProcessCommand"/> with an <see
        /// cref="AttackCommand"/> containing the specified <paramref name="target"/> and attacking
        /// <paramref name="units"/> that is executed by the <see cref="WorldState.ActiveFaction"/>
        /// in the specified <paramref name="worldState"/>.</remarks>

        public bool ExecuteAttack(WorldState worldState, IList<Entity> units, PointI target) {
            Command command = new AttackCommand(worldState.ActiveFaction, units, target);
            return ProcessCommand(worldState, command);
        }

        #endregion
        #region ExecuteAutomate

        /// <summary>
        /// Executes an <see cref="AutomateCommand"/> with the specified entities and internal text.
        /// </summary>
        /// <param name="worldState">
        /// The <see cref="WorldState"/> on which to execute the <see cref="AutomateCommand"/>.
        /// </param>
        /// <param name="entities">
        /// An <see cref="IList{T}"/> containing the <see cref="Entity"/> objects manipulated by the
        /// <see cref="AutomateCommand"/>.</param>
        /// <param name="text">
        /// The internal text associated with the <see cref="AutomateCommand"/>.</param>
        /// <returns>
        /// <c>true</c> if the <see cref="AutomateCommand"/> was executed successfully; otherwise,
        /// <c>false</c>.</returns>
        /// <exception cref="ArgumentException">
        /// <paramref name="entities"/> contains a null reference.</exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="worldState"/> is a null reference.</exception>
        /// <exception cref="ArgumentNullOrEmptyException"><para>
        /// <paramref name="entities"/> is a null reference or an empty collection.
        /// </para><para>-or-</para><para>
        /// <paramref name="text"/> is a null reference or an empty string.</para></exception>
        /// <remarks>
        /// <b>ExecuteAutomate</b> calls <see cref="ProcessCommand"/> with a <see
        /// cref="AutomateCommand"/> containing the specified <paramref name="entities"/> and
        /// <paramref name="text"/> that is executed by the <see cref="WorldState.ActiveFaction"/>
        /// in the specified <paramref name="worldState"/>.</remarks>

        public bool ExecuteAutomate(WorldState worldState, IList<Entity> entities, string text) {
            Command command = new AutomateCommand(worldState.ActiveFaction, entities, text);
            return ProcessCommand(worldState, command);
        }

        #endregion
        #region ExecuteBeginTurn

        /// <summary>
        /// Executes a <see cref="BeginTurnCommand"/>.</summary>
        /// <param name="worldState">
        /// The <see cref="WorldState"/> on which to execute the <see cref="BeginTurnCommand"/>.
        /// </param>
        /// <returns>
        /// <c>true</c> if the <see cref="BeginTurnCommand"/> was executed successfully; otherwise,
        /// <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="worldState"/> is a null reference.</exception>
        /// <remarks>
        /// <b>ExecuteBeginTurn</b> calls <see cref="ProcessCommand"/> with a <see
        /// cref="BeginTurnCommand"/> that is executed by the <see cref="WorldState.ActiveFaction"/>
        /// in the specified <paramref name="worldState"/>.</remarks>

        public bool ExecuteBeginTurn(WorldState worldState) {
            Command command = new BeginTurnCommand(worldState.ActiveFaction);
            return ProcessCommand(worldState, command);
        }

        #endregion
        #region ExecuteBuild

        /// <summary>
        /// Executes a <see cref="BuildCommand"/> with the specified entity class.</summary>
        /// <param name="worldState">
        /// The <see cref="WorldState"/> on which to execute the <see cref="BuildCommand"/>.
        /// </param>
        /// <param name="classId">
        /// The <see cref="EntityClass.Id"/> string of the <see cref="EntityClass"/> to build.
        /// </param>
        /// <param name="count">
        /// A non-negative <see cref="Int32"/> value indicating the number of <see cref="Entity"/>
        /// objects to build.</param>
        /// <returns>
        /// <c>true</c> if the <see cref="BuildCommand"/> was executed successfully; otherwise,
        /// <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="worldState"/> is a null reference.</exception>
        /// <exception cref="ArgumentNullOrEmptyException">
        /// <paramref name="classId"/> is a null reference or an empty string.</exception>
        /// <remarks>
        /// <b>ExecuteBuild</b> calls <see cref="ProcessCommand"/> with a <see cref="BuildCommand"/>
        /// containing <paramref name="count"/> times the specified <paramref name="classId"/>
        /// string. The command is executed by the <see cref="WorldState.ActiveFaction"/> in the
        /// specified <paramref name="worldState"/>.</remarks>

        public bool ExecuteBuild(WorldState worldState, string classId, int count) {

            // multiply entity class identifiers
            string[] classIds = new string[count];
            for (int i = 0; i < count; i++)
                classIds[i] = classId;

            Command command = new BuildCommand(worldState.ActiveFaction, classIds);
            return ProcessCommand(worldState, command);
        }

        #endregion
        #region ExecuteBuildPlace

        /// <summary>
        /// Executes a <see cref="BuildCommand"/> with the specified entity class, and then a <see
        /// cref="PlaceCommand"/> with the newly created entities.</summary>
        /// <param name="worldState">
        /// The <see cref="WorldState"/> on which to execute the <see cref="BuildCommand"/> and <see
        /// cref="PlaceCommand"/>.</param>
        /// <param name="classId">
        /// The <see cref="EntityClass.Id"/> string of the <see cref="EntityClass"/> to build.
        /// </param>
        /// <param name="count">
        /// A non-negative <see cref="Int32"/> value indicating the number of <see cref="Entity"/>
        /// objects to build and place.</param>
        /// <param name="target">
        /// The coordinates of the <see cref="Site"/> that is the target of the <see
        /// cref="PlaceCommand"/>.</param>
        /// <returns>
        /// <c>true</c> if the <see cref="BuildCommand"/> and <see cref="PlaceCommand"/> were
        /// executed successfully; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="worldState"/> is a null reference.</exception>
        /// <exception cref="ArgumentNullOrEmptyException">
        /// <paramref name="classId"/> is a null reference or an empty string.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="target"/> specifies one or two negative coordinates.</exception>
        /// <remarks><para>
        /// <b>ExecuteBuildPlace</b> calls <see cref="ProcessCommand"/> twice: first with a <see
        /// cref="BuildCommand"/> containing <paramref name="count"/> times the specified <paramref
        /// name="classId"/> string, and then with a <see cref="PlaceCommand"/> containing the
        /// specified <paramref name="target"/> and the newly created <see
        /// cref="Command.Entities"/>.
        /// </para><para>
        /// Both commands are executed by the <see cref="WorldState.ActiveFaction"/> in the
        /// specified <paramref name="worldState"/>.</para></remarks>

        public bool ExecuteBuildPlace(WorldState worldState,
            string classId, int count, PointI target) {

            // multiply entity class identifiers
            string[] classIds = new string[count];
            for (int i = 0; i < count; i++)
                classIds[i] = classId;

            // build entities of specified class
            Faction faction = worldState.ActiveFaction;
            Command build = new BuildCommand(faction, classIds);
            if (!ProcessCommand(worldState, build))
                return false;

            // place newly built entities on specified site
            IList<Entity> entities = EntityReference.GetEntities(build.Entities);
            Command place = new PlaceCommand(faction, entities, target);
            return ProcessCommand(worldState, place);
        }

        #endregion
        #region ExecuteDestroy

        /// <summary>
        /// Executes a <see cref="DestroyCommand"/> with the specified entities.</summary>
        /// <param name="worldState">
        /// The <see cref="WorldState"/> on which to execute the <see cref="DestroyCommand"/>.
        /// </param>
        /// <param name="entities">
        /// An <see cref="IList{T}"/> containing the <see cref="Entity"/> objects to delete.</param>
        /// <returns>
        /// <c>true</c> if the <see cref="DestroyCommand"/> was executed successfully; otherwise,
        /// <c>false</c>.</returns>
        /// <exception cref="ArgumentException">
        /// <paramref name="entities"/> contains a null reference.</exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="worldState"/> is a null reference.</exception>
        /// <exception cref="ArgumentNullOrEmptyException">
        /// <paramref name="entities"/> is a null reference or an empty collection.</exception>
        /// <remarks>
        /// <b>ExecuteDestroy</b> calls <see cref="ProcessCommand"/> with a <see
        /// cref="DestroyCommand"/> containing the specified <paramref name="entities"/> that is
        /// executed by the <see cref="WorldState.ActiveFaction"/> in the specified <paramref
        /// name="worldState"/>.</remarks>

        public bool ExecuteDestroy(WorldState worldState, IList<Entity> entities) {
            Command destroy = new DestroyCommand(worldState.ActiveFaction, entities);
            return ProcessCommand(worldState, destroy);
        }

        #endregion
        #region ExecuteEndTurn

        /// <summary>
        /// Executes an <see cref="EndTurnCommand"/>.</summary>
        /// <param name="worldState">
        /// The <see cref="WorldState"/> on which to execute the <see cref="EndTurnCommand"/>.
        /// </param>
        /// <returns>
        /// <c>true</c> if the <see cref="EndTurnCommand"/> was executed successfully; otherwise,
        /// <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="worldState"/> is a null reference.</exception>
        /// <remarks>
        /// <b>ExecuteEndTurn</b> calls <see cref="ProcessCommand"/> with an <see
        /// cref="EndTurnCommand"/> that is executed by the <see cref="WorldState.ActiveFaction"/>
        /// in the specified <paramref name="worldState"/>.</remarks>

        public bool ExecuteEndTurn(WorldState worldState) {
            Command command = new EndTurnCommand(worldState.ActiveFaction);
            return ProcessCommand(worldState, command);
        }

        #endregion
        #region ExecuteMove

        /// <summary>
        /// Executes a <see cref="MoveCommand"/> with the specified units and target location.
        /// </summary>
        /// <param name="worldState">
        /// The <see cref="WorldState"/> on which to execute the <see cref="MoveCommand"/>.</param>
        /// <param name="units">
        /// An <see cref="IList{T}"/> containing the <see cref="Unit"/> objects to move.</param>
        /// <param name="target">
        /// The coordinates of the <see cref="Site"/> that is the target of the <see
        /// cref="MoveCommand"/>.</param>
        /// <returns>
        /// <c>true</c> if the <see cref="MoveCommand"/> was executed successfully; otherwise,
        /// <c>false</c>.</returns>
        /// <exception cref="ArgumentException">
        /// <paramref name="units"/> contains a null reference.</exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="worldState"/> is a null reference.</exception>
        /// <exception cref="ArgumentNullOrEmptyException">
        /// <paramref name="units"/> is a null reference or an empty collection.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="target"/> specifies one or two negative coordinates.</exception>
        /// <remarks>
        /// <b>ExecuteMove</b> calls <see cref="ProcessCommand"/> with a <see cref="MoveCommand"/>
        /// containing the specified <paramref name="target"/> and moving <paramref name="units"/>
        /// that is executed by the <see cref="WorldState.ActiveFaction"/> in the specified
        /// <paramref name="worldState"/>.</remarks>

        public bool ExecuteMove(WorldState worldState, IList<Entity> units, PointI target) {
            Command command = new MoveCommand(worldState.ActiveFaction, units, target);
            return ProcessCommand(worldState, command);
        }

        #endregion
        #region ExecutePlace

        /// <summary>
        /// Executes a <see cref="PlaceCommand"/> with the specified entities and target location.
        /// </summary>
        /// <param name="worldState">
        /// The <see cref="WorldState"/> on which to excecute the <see cref="PlaceCommand"/>.
        /// </param>
        /// <param name="entities">
        /// An <see cref="IList{T}"/> containing the <see cref="Entity"/> objects to place.</param>
        /// <param name="target">
        /// The coordinates of the <see cref="Site"/> that is the target of the <see
        /// cref="PlaceCommand"/>.</param>
        /// <returns>
        /// <c>true</c> if the <see cref="PlaceCommand"/> was executed successfully; otherwise,
        /// <c>false</c>.</returns>
        /// <exception cref="ArgumentException">
        /// <paramref name="entities"/> contains a null reference.</exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="worldState"/> is a null reference.</exception>
        /// <exception cref="ArgumentNullOrEmptyException">
        /// <paramref name="entities"/> is a null reference or an empty collection.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="target"/> specifies one or two negative coordinates.</exception>
        /// <remarks>
        /// <b>ExecutePlace</b> calls <see cref="ProcessCommand"/> with a <see cref="PlaceCommand"/>
        /// containing the specified <paramref name="target"/> and unplaced <paramref
        /// name="entities"/> that is executed by the <see cref="WorldState.ActiveFaction"/> in the
        /// specified <paramref name="worldState"/>.</remarks>

        public bool ExecutePlace(WorldState worldState, IList<Entity> entities, PointI target) {
            Command command = new PlaceCommand(worldState.ActiveFaction, entities, target);
            return ProcessCommand(worldState, command);
        }

        #endregion
        #region ExecuteRename

        /// <summary>
        /// Executes a <see cref="RenameCommand"/> the specified <see cref="Entity"/> objects and
        /// entity name.</summary>
        /// <param name="worldState">
        /// The <see cref="WorldState"/> on which to execute the <see cref="RenameCommand"/>.
        /// </param>
        /// <param name="entities">
        /// An <see cref="IList{T}"/> containing the <see cref="Entity"/> objects to rename.</param>
        /// <param name="name">
        /// The new value for the <see cref="Entity.Name"/> property of all <paramref
        /// name="entities"/>. This argument may be a null reference or an empty string.</param>
        /// <returns>
        /// <c>true</c> if the <see cref="RenameCommand"/> was executed successfully; otherwise,
        /// <c>false</c>.</returns>
        /// <exception cref="ArgumentException">
        /// <paramref name="entities"/> contains a null reference.</exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="worldState"/> is a null reference.</exception>
        /// <exception cref="ArgumentNullOrEmptyException">
        /// <paramref name="entities"/> is a null reference or an empty collection.</exception>
        /// <remarks>
        /// <b>ExecuteRename</b> calls <see cref="ProcessCommand"/> with a <see
        /// cref="RenameCommand"/> containing the specified <paramref name="entities"/> and entity
        /// <paramref name="name"/> that is executed by the <see cref="WorldState.ActiveFaction"/>
        /// in the specified <paramref name="worldState"/>.</remarks>

        public bool ExecuteRename(WorldState worldState, IList<Entity> entities, string name) {
            Command command = new RenameCommand(worldState.ActiveFaction, entities, name);
            return ProcessCommand(worldState, command);
        }

        #endregion
        #region ExecuteResign

        /// <summary>
        /// Executes a <see cref="ResignCommand"/>.</summary>
        /// <param name="worldState">
        /// The <see cref="WorldState"/> on which to execute the <see cref="ResignCommand"/>.
        /// </param>
        /// <returns>
        /// <c>true</c> if the <see cref="ResignCommand"/> was executed successfully; otherwise,
        /// <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="worldState"/> is a null reference.</exception>
        /// <remarks>
        /// <b>ExecuteResign</b> calls <see cref="ProcessCommand"/> with a <see
        /// cref="ResignCommand"/> that is executed by the <see cref="WorldState.ActiveFaction"/> in
        /// the specified <paramref name="worldState"/>.</remarks>

        public bool ExecuteResign(WorldState worldState) {
            Command command = new ResignCommand(worldState.ActiveFaction);
            return ProcessCommand(worldState, command);
        }

        #endregion
        #endregion
    }
}
