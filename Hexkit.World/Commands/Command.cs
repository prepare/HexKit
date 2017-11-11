using System;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Xml;

using Tektosyne;
using Tektosyne.Collections;
using Tektosyne.Geometry;
using Tektosyne.Xml;

using Hexkit.Global;
using Hexkit.Scenario;
using Hexkit.World.Instructions;

namespace Hexkit.World.Commands {

    using InstructionList = ListEx<Instruction>;

    /// <summary>
    /// Represents a game command.</summary>
    /// <remarks><para>
    /// <b>Command</b> provides the basic functionality for managing commands issued by factions.
    /// Derived classes should override <see cref="Command.Validate"/>, <see
    /// cref="Command.Execute"/>, any required properties, and the protected XML input/output
    /// methods to implement the semantics of specific commands.
    /// </para><para>
    /// <b>Command</b> corresponds to the complex XML type "command" defined in <see
    /// cref="FilePaths.SessionSchema"/>.</para></remarks>

    public abstract class Command: XmlSerializable {
        #region Command()

        /// <overloads>
        /// Initializes a new instance of the <see cref="Command"/> class.</overloads>
        /// <summary>
        /// Initializes a new instance of the <see cref="Command"/> class with default properties.
        /// </summary>

        protected Command() {
            Source = SiteReference.Invalid;
            Target = SiteReference.Invalid;
        }

        #endregion
        #region Command(Faction)

        /// <summary>
        /// Initializes a new instance of the <see cref="Command"/> class with the specified <see
        /// cref="Faction"/>.</summary>
        /// <param name="faction">
        /// The initial value for the <see cref="Faction"/> property.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="faction"/> is a null reference.</exception>

        protected Command(Faction faction): this() {
            if (faction == null)
                ThrowHelper.ThrowArgumentNullException("faction");

            this._faction = new FactionReference(faction);
        }

        #endregion
        #region Private Fields

        // property backers
        private readonly static ExecutionCounters _counters = new ExecutionCounters();
        private readonly InstructionList _program = new InstructionList();

        private FactionReference _faction;
        private bool _hasProgram;
        private int _turn = -1;

        #endregion
        #region Public Properties
        #region Context

        /// <summary>
        /// Gets the context in which the <see cref="Command"/> is executed.</summary>
        /// <value>
        /// The <see cref="ExecutionContext"/> passed to <see cref="Execute"/>. The default is a
        /// null reference.</value>
        /// <remarks><para>
        /// <b>Context</b> always returns a null reference except during the execution of <see
        /// cref="GenerateProgram"/>, when it is temporarily set to the <see
        /// cref="ExecutionContext"/> passed to <see cref="Execute"/>.
        /// </para><para>
        /// Rule script methods that receive a <see cref="Command"/> argument may use its
        /// <b>Context</b> to access the <see cref="ExecutionContext.WorldState"/> on which the
        /// command is executed, and the <see cref="ExecutionContext.QueueCommand"/> method to queue
        /// additional commands for subsequent execution.
        /// </para><para>
        /// <b>Context</b> always returns a valid <see cref="ExecutionContext"/> in this case since
        /// all such methods are called by <b>GenerateProgram</b>.</para></remarks>

        public ExecutionContext Context { get; private set; }

        #endregion
        #region Counters

        /// <summary>
        /// Gets the counters for executed commands and instructions.</summary>
        /// <value>
        /// The <see cref="ExecutionCounters"/> keeping track of all commands and instructions that
        /// were executed since the Hexkit Game application was started.</value>
        /// <remarks><para>
        /// <b>Counters</b> never returns a null reference. This property never changes once the
        /// type has been initialized.
        /// </para><para>
        /// The various <see cref="ExecutionCounters"/> are incremented by <see cref="Execute"/> and
        /// <see cref="ExecuteInstruction"/>, as appropriate, whenever a new HCL program is
        /// generated.</para></remarks>

        public static ExecutionCounters Counters {
            [DebuggerStepThrough]
            get { return Command._counters; }
        }

        #endregion
        #region Entities

        /// <summary>
        /// Gets a list of all entities affected by the <see cref="Command"/>.</summary>
        /// <value>
        /// An <see cref="Array"/> of <see cref="EntityReference"/> instances wrapping all affected
        /// <see cref="Entity"/> objects.</value>
        /// <remarks><para>
        /// <b>Entities</b> returns a null reference if the command does not affect any entities.
        /// </para><para>
        /// This is always the case for the <see cref="Command"/> implementation of <b>Entities</b>.
        /// Derived classes define a valid collection. When defined, all <b>Entities</b> elements
        /// must have the same <see cref="Entity.Category"/>.</para></remarks>

        public EntityReference[] Entities { get; protected set; }

        #endregion
        #region Faction

        /// <summary>
        /// Gets the <see cref="World.Faction"/> that executes the <see cref="Command"/>.</summary>
        /// <value>
        /// A <see cref="FactionReference"/> instance wrapping the <see cref="World.Faction"/>
        /// that executes the <see cref="Command"/>.</value>
        /// <remarks><para>
        /// The <see cref="FactionReference.Id"/> component may hold a null reference before
        /// initialization has completed, but never holds a null reference or an empty string
        /// afterwards.
        /// </para><para>
        /// The <see cref="FactionReference.Value"/> component may hold a null reference before <see
        /// cref="Validate"/> has succeeded but never holds a null reference afterwards, until the
        /// <see cref="World.Faction"/> object has been garbage-collected.
        /// </para><para>
        /// The <b>Id</b> component holds the value of the "faction" XML attribute.</para></remarks>

        public FactionReference Faction {
            [DebuggerStepThrough]
            get { return this._faction; }
        }

        #endregion
        #region HasProgram

        /// <summary>
        /// Gets a value indicating whether a valid HCL <see cref="Program"/> has been generated or
        /// deserialized for the <see cref="Command"/>.</summary>
        /// <value>
        /// <c>true</c> if the <see cref="Program"/> property already contains all HCL instructions
        /// for the <see cref="Command"/>; otherwise, <c>false</c>. The default is <c>false</c>.
        /// </value>
        /// <remarks><para>
        /// <see cref="GenerateProgram"/> and <see cref="ReadXmlElements"/> set <b>HasProgram</b> to
        /// <c>true</c>. The property never changes once it has been set.
        /// </para><note type="implementnotes">
        /// Some commands may generate an empty HCL program. In this case, <see cref="Program"/>
        /// does not contain any instructions even though <b>HasProgram</b> is <c>true</c>.
        /// </note></remarks>

        public bool HasProgram {
            [DebuggerStepThrough]
            get { return this._hasProgram; }
        }

        #endregion
        #region Name

        /// <summary>
        /// Gets the display name of the <see cref="Command"/>.</summary>
        /// <value>
        /// The display name of the <see cref="Command"/>. The default is the value of the <see
        /// cref="XmlName"/> property.</value>
        /// <exception cref="NotImplementedException">
        /// The property was accessed on an abstract base class.</exception>
        /// <remarks><para>
        /// <b>Name</b> returns the name that should be used to represent the <see cref="Command"/>
        /// within Hexkit Game.
        /// </para><para>
        /// When overridden in a derived class, <b>Name</b> should return a constant string that is
        /// neither empty nor a null reference.</para></remarks>

        public virtual string Name {
            [DebuggerStepThrough]
            get { return XmlName; }
        }

        #endregion
        #region Program

        /// <summary>
        /// Gets a list of all instructions that constitute the HCL program for the <see
        /// cref="Command"/>.</summary>
        /// <value>
        /// A read-only <see cref="InstructionList"/> containing the <see cref="Instruction"/>
        /// sequence that constitutes the HCL program for the <see cref="Command"/>. The default is
        /// an empty collection.</value>
        /// <remarks>
        /// <b>Program</b> never returns a null reference, and its collection elements are never
        /// null references.</remarks>

        public InstructionList Program {
            [DebuggerStepThrough]
            get { return this._program.AsReadOnly(); }
        }

        #endregion
        #region Source

        /// <summary>
        /// Gets the source <see cref="Site"/> for the <see cref="Command"/>.</summary>
        /// <value>
        /// A <see cref="SiteReference"/> instance wrapping the source <see cref="Site"/>.</value>
        /// <remarks><para>
        /// <b>Source</b> returns <see cref="SiteReference.Invalid"/> if the command does not define
        /// a source site.
        /// </para><para>
        /// This is always the case for the <see cref="Command"/> implementation of <b>Source</b>.
        /// Derived classes may define a valid <see cref="Site"/>.</para></remarks>

        public SiteReference Source { get; protected set; }

        #endregion
        #region Target

        /// <summary>
        /// Gets the target <see cref="Site"/> for the <see cref="Command"/>.</summary>
        /// <value>
        /// A <see cref="SiteReference"/> instance wrapping the target <see cref="Site"/>.</value>
        /// <remarks><para>
        /// <b>Target</b> returns <see cref="SiteReference.Invalid"/> if the command does not define
        /// a target site.
        /// </para><para>
        /// This is always the case for the <see cref="Command"/> implementation of <b>Target</b>.
        /// Derived classes may define a valid <see cref="Site"/>.</para></remarks>

        public SiteReference Target { get; protected set; }

        #endregion
        #region Turn

        /// <summary>
        /// Gets the index of the game turn when the <see cref="Command"/> was issued.</summary>
        /// <value>
        /// The zero-based index of the full game turn during which this <see cref="Command"/> was
        /// executed.</value>
        /// <remarks><para>
        /// <b>Turn</b> may hold a negative value until <see cref="Validate"/> has succeeded but
        /// never holds a negative value afterwards.
        /// </para><para>
        /// This property holds the value of the "turn" XML attribute.</para></remarks>

        public int Turn {
            [DebuggerStepThrough]
            get { return this._turn; }
        }

        #endregion
        #endregion
        #region Execute

        /// <summary>
        /// Executes the <see cref="Command"/> within the specified <see cref="ExecutionContext"/>.
        /// </summary>
        /// <param name="context">
        /// The <see cref="ExecutionContext"/> for the <see cref="Command"/>.</param>
        /// <exception cref="ArgumentException">
        /// <see cref="HasProgram"/> is <c>false</c>, and <paramref name="context"/> does not
        /// contain a valid <see cref="ExecutionContext.QueueCommand"/> method.</exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="context"/> is a null reference.</exception>
        /// <exception cref="InvalidCommandException">
        /// This <see cref="Command"/>, or another that was enqueued for subsequent execution,
        /// contains data that is invalid with respect to the specified <paramref name="context"/>.
        /// </exception>
        /// <exception cref="PropertyValueException">
        /// <see cref="Context"/> is not a null reference.</exception>
        /// <remarks><para>
        /// <b>Execute</b> first performs the following initializations for the entire <see
        /// cref="ExecutionContext.WorldState"/> if <see cref="History.IsInitialized"/> is still
        /// <c>false</c> for the associated <see cref="WorldState.History"/>:
        /// </para><list type="number"><item>
        /// Start a new <see cref="EntityHistory"/> for all <see cref="WorldState.Entities"/>.
        /// </item><item>
        /// Start a new <see cref="FactionHistory"/> for all <see cref="WorldState.Factions"/>.
        /// </item><item>
        /// Set <see cref="History.IsInitialized"/> to <c>true</c>.
        /// </item></list><para>
        /// If <see cref="HasProgram"/> is still <c>false</c>, <b>Execute</b> then generates a new
        /// HCL <see cref="Program"/> for the <see cref="Command"/>, as follows:
        /// </para><list type="number"><item>
        /// Set <see cref="Context"/> to the specified <paramref name="context"/>.
        /// </item><item>
        /// Call <see cref="GenerateProgram"/> to generate and execute the HCL <b>Program</b>.
        /// </item><item>
        /// Set <see cref="HasProgram"/> to <c>true</c>.
        /// </item><item>
        /// Reset <see cref="Context"/> to a null reference.
        /// </item></list><para>
        /// Otherwise, <b>Execute</b> executes the existing HCL program by invoking <see
        /// cref="Instruction.Execute"/> on all <b>Program</b> instructions. These calls may change
        /// the data of the specified <paramref name="context"/>.
        /// </para><para>
        /// Clients must call <see cref="Validate"/> with the <see cref="WorldState"/> of the
        /// specified <paramref name="context"/> before calling <b>Execute</b>, in order to set all
        /// required property values. This step is optional if the HCL program has already been
        /// generated.</para></remarks>

        public void Execute(ExecutionContext context) {
            if (context == null)
                ThrowHelper.ThrowArgumentNullException("context");
            if (Context != null)
                ThrowHelper.ThrowPropertyValueException(
                    "Context", Tektosyne.Strings.PropertyNotNull);

            // complete history initialization
            WorldState world = context.WorldState;
            if (!world.History.IsInitialized) {

                // start history for all entities
                foreach (Entity entity in world.Entities.Values) {
                    world.History.AddEntity(world, entity);
                    if (entity.InstanceName != null)
                        world.History.Entities[entity.Id].SetName(world, entity.InstanceName);
                }

                // start history for all factions
                foreach (Faction faction in world.Factions)
                    world.History.AddFaction(world, faction);

                world.History.IsInitialized = true;
            }

            if (!HasProgram) {
                if (context.QueueCommand == null)
                    ThrowHelper.ThrowArgumentExceptionWithFormat(
                        "context", Tektosyne.Strings.ArgumentPropertyInvalid, "QueueCommand");

                ++Counters.Commands;

                // generate & execute program
                Context = context;
                GenerateProgram();

                this._program.TrimExcess();
                this._hasProgram = true;
                Context = null;
            }
            else {
                // execute optimized HCL instructions
                for (int i = 0; i < Program.Count; i++) {
                    Instruction instruction = Program[i];

                    if (instruction.Category == InstructionCategory.Event) {
                        // show event if desired
                        if (context.ShowEvent != null)
                            context.ShowEvent(instruction);
                    } else {
                        // ineffectual instructions were removed
                        bool result = instruction.Execute(context.WorldState);
                        Debug.Assert(result, instruction.Name + ": Instruction has no effect.");
                    }
                }
            }
        }

        #endregion
        #region ExecuteInstruction

        /// <summary>
        /// Executes the specified <see cref="Instruction"/> within the current execution <see
        /// cref="Context"/>.</summary>
        /// <param name="instruction">
        /// The <see cref="Instruction"/> to execute.</param>
        /// <returns>
        /// <c>true</c> if execution of the specified <paramref name="instruction"/> has changed the
        /// current <see cref="ExecutionContext.WorldState"/>; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="instruction"/> is a null reference.</exception>
        /// <exception cref="InvalidCommandException">
        /// <paramref name="instruction"/> contains invalid data.</exception>
        /// <exception cref="PropertyValueException">
        /// <see cref="Context"/> is a null reference.</exception>
        /// <remarks><para>
        /// If the specified <paramref name="instruction"/> is an event instruction,
        /// <b>ExecuteInstruction</b> passes it to the <see cref="ExecutionContext.ShowEvent"/>
        /// method if one is defined, and adds it to the HCL <see cref="Program"/>.
        /// </para><para>
        /// Otherwise, <b>ExecuteInstruction</b> invokes <see cref="Instruction.Execute"/> on the
        /// <paramref name="instruction"/>, and adds it to the HCL <b>Program</b> only if the method
        /// succeeds.
        /// </para><para>
        /// In this last case only, <b>ExecuteInstruction</b> returns <c>true</c>; otherwise,
        /// <c>false</c>.</para></remarks>

        protected bool ExecuteInstruction(Instruction instruction) {
            if (instruction == null)
                ThrowHelper.ThrowArgumentNullException("instruction");
            if (Context == null)
                ThrowHelper.ThrowPropertyValueException("Context", Tektosyne.Strings.PropertyNull);

            ++Counters.Instructions;

            if (instruction.Category == InstructionCategory.Event) {
                if (Context.ShowEvent != null)
                    Context.ShowEvent(instruction);

                this._program.Add(instruction);
                ++Counters.EventInstructions;
                return false;
            }
            else if (instruction.Execute(Context.WorldState)) {
                this._program.Add(instruction);
                return true;
            }
            else {
                ++Counters.IneffectualInstructions;
                return false;
            }
        }

        #endregion
        #region GenerateProgram

        /// <summary>
        /// Generates and executes the HCL <see cref="Program"/> for the <see cref="Command"/>.
        /// </summary>
        /// <exception cref="InvalidCommandException">
        /// The <see cref="Command"/> contains data that is invalid with respect to the current <see
        /// cref="Context"/>.</exception>
        /// <exception cref="PropertyValueException">
        /// <see cref="Context"/> is a null reference.</exception>
        /// <remarks>
        /// <see cref="Command"/> does not implement <b>GenerateProgram</b>. Derived classes must
        /// override this method to call the various <b>Command</b> methods that will generate and
        /// execute the desired HCL <see cref="Program"/>.</remarks>

        protected abstract void GenerateProgram();

        #endregion
        #region InlineCommand

        /// <summary>
        /// Executes another <see cref="Command"/> within the current execution <see
        /// cref="Context"/>, and appends its HCL program to the current <see cref="Program"/>.
        /// </summary>
        /// <param name="command">
        /// The <see cref="Command"/> to execute.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="command"/> is a null reference.</exception>
        /// <exception cref="InvalidCommandException"><para>
        /// <paramref name="command"/> is a <see cref="BeginTurnCommand"/> or an <see
        /// cref="EndTurnCommand"/>.
        /// </para><para>-or-</para><para>
        /// <paramref name="command"/> contains data that is invalid with respect to the current
        /// <see cref="Context"/>.</para></exception>
        /// <exception cref="PropertyValueException">
        /// <see cref="Context"/> is a null reference.</exception>
        /// <remarks><para>
        /// <b>InlineCommand</b> validates and executes the specified <paramref name="command"/>
        /// with the current <see cref="Context"/>. The corresponding property of the specified
        /// <paramref name="command"/> is ignored.
        /// </para><para>
        /// When execution has finished, <b>InlineCommand</b> appends the entire HCL <see
        /// cref="Program"/> generated by the specified <paramref name="command"/> to the
        /// <b>Program</b> of the current <see cref="Command"/>.
        /// </para><para>
        /// Use <b>InlineCommand</b> to combine the effects of two game commands within the HCL
        /// program of a single command.</para></remarks>

        public void InlineCommand(Command command) {
            if (command == null)
                ThrowHelper.ThrowArgumentNullException("command");

            if (command is BeginTurnCommand || command is EndTurnCommand)
                InvalidCommandException.ThrowNewWithFormat(
                    Global.Strings.CommandInlineInvalid, command.Name);

            if (Context == null)
                ThrowHelper.ThrowPropertyValueException("Context", Tektosyne.Strings.PropertyNull);

            // execute command in current context
            command.Validate(Context.WorldState);
            command.Execute(Context);

            // append instructions to current program
            int count = command._program.Count;
            for (int i = 0; i < count; i++)
                this._program.Add(command._program[i]);

            // clear new program, just to be sure
            command._program.Clear();
        }

        #endregion
        #region ToString

        /// <summary>
        /// Returns a <see cref="String"/> that represents the <see cref="Command"/>.</summary>
        /// <returns>
        /// The value of the <see cref="Name"/> property, followed by the <b>Location</b> of the
        /// <see cref="Source"/> site, followed by the <b>Location</b> of the <see cref="Target"/>
        /// site. Locations that equal <see cref="Site.InvalidLocation"/> are omitted.</returns>

        public override string ToString() {
            StringBuilder sb = new StringBuilder();

            // append command name
            sb.Append(Name);

            // append source site if available
            if (Source.Location != Site.InvalidLocation) {
                sb.Append(' ');
                sb.Append(Source.ToString());
            }

            // append target site if available
            if (Target.Location != Site.InvalidLocation) {

                if (Source.Location != Site.InvalidLocation)
                    sb.Append(" to ");
                else
                    sb.Append(' ');

                sb.Append(Target.ToString());
            }

            return sb.ToString();
        }

        #endregion
        #region Validate

        /// <summary>
        /// Validates the current data of the <see cref="Command"/> against the specified <see
        /// cref="WorldState"/>.</summary>
        /// <param name="worldState">
        /// The <see cref="WorldState"/> to validate the <see cref="Command"/> against.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="worldState"/> is a null reference.</exception>
        /// <exception cref="InvalidCommandException">
        /// The value of the <see cref="Faction"/> property is invalid.</exception>
        /// <remarks><para>
        /// <b>Validate</b> sets the <see cref="Turn"/> property and the <b>Value</b> component of
        /// the <see cref="Faction"/> property if validation is successful. These properties hold 
        /// undefined values on failure.
        /// </para><para>
        /// Derived classes should validate and set any additional property values that are required
        /// by the actual type of the <see cref="Command"/> object. The specified <paramref
        /// name="worldState"/> should never be changed, however.</para></remarks>

        public virtual void Validate(WorldState worldState) {
            if (worldState == null)
                ThrowHelper.ThrowArgumentNullException("worldState");

            /*
             * Possible optimization: Do not re-acquire Faction.Value
             * if not a null reference. The value was set by the public
             * constructor in this case and should be correct.
             */

            if (String.IsNullOrEmpty(Faction.Id))
                InvalidCommandException.ThrowNewWithFormat(Global.Strings.CommandFactionNone, Name);

            Faction faction;
            if (!worldState.Factions.TryGetValue(Faction.Id, out faction))
                InvalidCommandException.ThrowNewWithFormat(
                    Global.Strings.CommandFactionInvalid, Name, Faction.Id);

            // specified turn must equal current turn
            if (Turn >= 0 && worldState.CurrentTurn != Turn)
                InvalidCommandException.ThrowNewWithFormat(
                    Global.Strings.CommandTurnInvalid, Name, Turn, worldState.CurrentTurn);

            // store executing faction and turn
            this._faction.Value = faction;
            this._turn = worldState.CurrentTurn;
        }

        #endregion
        #region Instruction Methods
        #region AdvanceFaction

        /// <summary>
        /// Executes an <see cref="AdvanceFactionInstruction"/>.</summary>
        /// <exception cref="PropertyValueException">
        /// <see cref="Context"/> is a null reference.</exception>

        internal void AdvanceFaction() {
            ExecuteInstruction(new AdvanceFactionInstruction());
        }

        #endregion
        #region CreateEntity

        /// <summary>
        /// Executes a <see cref="CreateEntityInstruction"/> with the specified <see
        /// cref="EntityClass"/> identifier.</summary>
        /// <param name="classId">
        /// The <see cref="EntityClass.Id"/> string of the <see cref="EntityClass"/> to instantiate.
        /// </param>
        /// <returns>
        /// The newly created <see cref="Entity"/> object.</returns>
        /// <exception cref="InvalidCommandException">
        /// <paramref name="classId"/> is not a valid entity class identifier.</exception>
        /// <exception cref="PropertyValueException">
        /// <see cref="Context"/> is a null reference.</exception>

        public Entity CreateEntity(string classId) {

            var instruction = new CreateEntityInstruction(classId);
            instruction.Results = new InstructionResults();
            ExecuteInstruction(instruction);

            // retrieve newly created entity
            Entity entity = instruction.Results.Entity;
            instruction.Results = null;

            return entity;
        }

        #endregion
        #region CreateFaction

        /// <summary>
        /// Executes a <see cref="CreateFactionInstruction"/> with the specified <see
        /// cref="World.Faction"/> identifier.</summary>
        /// <param name="factionId">
        /// The <see cref="World.Faction.Id"/> string of the created <see cref="World.Faction"/>.
        /// </param>
        /// <exception cref="InvalidCommandException">
        /// <paramref name="factionId"/> is not a valid faction identifier.</exception>
        /// <exception cref="PropertyValueException">
        /// <see cref="Context"/> is a null reference.</exception>

        internal void CreateFaction(string factionId) {
            ExecuteInstruction(new CreateFactionInstruction(factionId));
        }

        #endregion
        #region DeleteEntity

        /// <summary>
        /// Executes a <see cref="DeleteEntityInstruction"/> with the specified <see
        /// cref="Entity"/> identifier.</summary>
        /// <param name="entityId">
        /// The <see cref="Entity.Id"/> string of the <see cref="Entity"/> to delete.</param>
        /// <exception cref="InvalidCommandException">
        /// <paramref name="entityId"/> is not a valid entity identifier.</exception>
        /// <exception cref="PropertyValueException">
        /// <see cref="Context"/> is a null reference.</exception>
        /// <remarks>
        /// <b>DeleteEntity</b> calls <see cref="Entity.OnOwnerChanged"/> and <see
        /// cref="Entity.OnSiteChanged"/> on the indicated <see cref="Entity"/> if instruction
        /// execution has changed the current <see cref="ExecutionContext.WorldState"/>.</remarks>

        public void DeleteEntity(string entityId) {

            var instruction = new DeleteEntityInstruction(entityId);
            instruction.Results = new InstructionResults();

            if (ExecuteInstruction(instruction)) {
                Entity entity = instruction.Results.Entity;

                // notify entity of owner and site change
                entity.OnOwnerChanged(this, instruction.Results.Faction);
                entity.OnSiteChanged(this, instruction.Results.Site);
            }

            instruction.Results = null;
        }

        #endregion
        #region DeleteFaction

        /// <summary>
        /// Executes a <see cref="DeleteFactionInstruction"/> with the specified <see
        /// cref="World.Faction"/> identifier.</summary>
        /// <param name="factionId">
        /// The <see cref="World.Faction.Id"/> string of the <see cref="World.Faction"/> to delete.
        /// </param>
        /// <exception cref="InvalidCommandException">
        /// <paramref name="factionId"/> is not a valid faction identifier.</exception>
        /// <exception cref="PropertyValueException">
        /// <see cref="Context"/> is a null reference.</exception>

        public void DeleteFaction(string factionId) {
            ExecuteInstruction(new DeleteFactionInstruction(factionId));
        }

        #endregion
        #region MoveEntityToTop

        /// <summary>
        /// Executes a <see cref="MoveEntityToTopInstruction"/> with the specified
        /// <see cref="Entity"/> identifier.</summary>
        /// <param name="entityId">
        /// The <see cref="Entity.Id"/> string of the <see cref="Entity"/> to manipulate.</param>
        /// <exception cref="InvalidCommandException">
        /// <paramref name="entityId"/> is not a valid entity identifier.</exception>
        /// <exception cref="PropertyValueException">
        /// <see cref="Context"/> is a null reference.</exception>
        /// <remarks>
        /// <b>MoveEntityToTop</b> does nothing if the specified <paramref name="entityId"/>
        /// indicates an unplaced <see cref="Entity"/>.</remarks>

        public void MoveEntityToTop(string entityId) {
            ExecuteInstruction(new MoveEntityToTopInstruction(entityId));
        }

        #endregion
        #region MoveImage

        /// <summary>
        /// Executes a <see cref="MoveImageInstruction"/> with the specified <see
        /// cref="EntityClass"/> identifier, map locations, and display delay.</summary>
        /// <param name="classId">
        /// The <see cref="EntityClass.Id"/> string of the <see cref="EntityClass"/> whose image to
        /// move.</param>
        /// <param name="sites">
        /// An <see cref="Array"/> containing the coordinates of each <see cref="Site"/> to show the
        /// image on.</param>
        /// <param name="delay">
        /// The duration, in milliseconds, for which the image is shown on each <see cref="Site"/>.
        /// If this argument is non-positive, a default value of 250 msec is assumed.</param>
        /// <exception cref="PropertyValueException">
        /// <see cref="Context"/> is a null reference.</exception>
        /// <remarks>
        /// <b>MoveImage</b> does nothing if the specified <paramref name="classId"/> is not a valid
        /// entity class identifier; or if the specified <paramref name="sites"/> array is a null
        /// reference or an empty array, or contains any invalid map locations.</remarks>

        public void MoveImage(string classId, PointI[] sites, int delay) {
            ExecuteInstruction(new MoveImageInstruction(classId, sites, delay));
        }

        #endregion
        #region SelectEntity

        /// <summary>
        /// Executes a <see cref="SelectEntityInstruction"/> with the specified <see
        /// cref="Entity"/> identifier.</summary>
        /// <param name="entityId">
        /// The <see cref="Entity.Id"/> string of the <see cref="Entity"/> to select.</param>
        /// <exception cref="PropertyValueException">
        /// <see cref="Context"/> is a null reference.</exception>
        /// <remarks>
        /// <b>SelectEntity</b> does nothing if the specified <paramref name="entityId"/> is not a
        /// valid entity identifier, or specifies an unplaced <see cref="Entity"/>.</remarks>

        public void SelectEntity(string entityId) {
            ExecuteInstruction(new SelectEntityInstruction(entityId));
        }

        #endregion
        #region SetEntityClass

        /// <summary>
        /// Executes a <see cref="SetEntityClassInstruction"/> with the specified <see
        /// cref="Entity"/> and <see cref="EntityClass"/> identifiers.</summary>
        /// <param name="entityId">
        /// The <see cref="Entity.Id"/> string of the <see cref="Entity"/> to manipulate.</param>
        /// <param name="classId">
        /// The <see cref="EntityClass.Id"/> string of the new value for the <see
        /// cref="Entity.EntityClass"/> property of the specified <see cref="Entity"/>.</param>
        /// <exception cref="InvalidCommandException"><para>
        /// <paramref name="entityId"/> is not a valid entity identifier.
        /// </para><para>-or-</para><para>
        /// <paramref name="classId"/> is a non-empty string that is not a valid entity class
        /// identifier.
        /// </para><para>-or-</para><para>
        /// <paramref name="classId"/> does not match the <see cref="Entity.Category"/> of the
        /// indicated <see cref="Entity"/>.</para></exception>
        /// <exception cref="PropertyValueException">
        /// <see cref="Context"/> is a null reference.</exception>
        /// <remarks>
        /// <b>SetEntityClass</b> calls <see cref="Entity.OnEntityClassChanged"/> on the indicated
        /// <see cref="Entity"/> if instruction execution has changed the current <see
        /// cref="ExecutionContext.WorldState"/>.</remarks>

        public void SetEntityClass(string entityId, string classId) {

            var instruction = new SetEntityClassInstruction(entityId, classId);
            instruction.Results = new InstructionResults();

            if (ExecuteInstruction(instruction)) {
                Entity entity = instruction.Results.Entity;

                // notify entity of entity class change
                entity.OnEntityClassChanged(this, instruction.Results.EntityClass);
            }

            instruction.Results = null;
        }

        #endregion
        #region SetEntityDisplayClass

        /// <summary>
        /// Executes a <see cref="SetEntityDisplayClassInstruction"/> with the specified <see
        /// cref="Entity"/> and <see cref="EntityClass"/> identifiers.</summary>
        /// <param name="entityId">
        /// The <see cref="Entity.Id"/> string of the <see cref="Entity"/> to manipulate.</param>
        /// <param name="classId">
        /// The <see cref="EntityClass.Id"/> string of the new value for the <see
        /// cref="Entity.DisplayClass"/> property of the specified <see cref="Entity"/>.</param>
        /// <exception cref="InvalidCommandException"><para>
        /// <paramref name="entityId"/> is not a valid entity identifier.
        /// </para><para>-or-</para><para>
        /// <paramref name="classId"/> is a non-empty string that is not a valid entity class
        /// identifier.</para></exception>
        /// <exception cref="PropertyValueException">
        /// <see cref="Context"/> is a null reference.</exception>
        /// <remarks>
        /// Specify a null reference or an empty string for <paramref name="classId"/> to set the
        /// <see cref="Entity.DisplayClass"/> property of the indicated <see cref="Entity"/> to a
        /// null reference.</remarks>

        public void SetEntityDisplayClass(string entityId, string classId) {
            ExecuteInstruction(new SetEntityDisplayClassInstruction(entityId, classId));
        }

        #endregion
        #region SetEntityFrameOffset

        /// <summary>
        /// Executes a <see cref="SetEntityFrameOffsetInstruction"/> with the specified <see
        /// cref="Entity"/> identifier and frame index offset.</summary>
        /// <param name="entityId">
        /// The <see cref="Entity.Id"/> string of the <see cref="Entity"/> to manipulate.</param>
        /// <param name="frameOffset">
        /// The new value for the <see cref="Entity.FrameOffset"/> property of the specified <see
        /// cref="Entity"/>.</param>
        /// <exception cref="InvalidCommandException"><para>
        /// <paramref name="entityId"/> is not a valid entity identifier.
        /// </para><para>-or-</para><para>
        /// <paramref name="frameOffset"/> is less than zero.</para></exception>
        /// <exception cref="PropertyValueException">
        /// <see cref="Context"/> is a null reference.</exception>

        public void SetEntityFrameOffset(string entityId, int frameOffset) {
            ExecuteInstruction(new SetEntityFrameOffsetInstruction(entityId, frameOffset));
        }

        #endregion
        #region SetEntityName

        /// <summary>
        /// Executes a <see cref="SetEntityNameInstruction"/> with the specified <see
        /// cref="Entity"/> identifier and display name.</summary>
        /// <param name="entityId">
        /// The <see cref="Entity.Id"/> string of the <see cref="Entity"/> to manipulate.</param>
        /// <param name="name">
        /// The new value for the <see cref="Entity.Name"/> property of the indicated <see
        /// cref="Entity"/>.</param>
        /// <exception cref="InvalidCommandException">
        /// <paramref name="entityId"/> is not a valid entity identifier.</exception>
        /// <exception cref="PropertyValueException">
        /// <see cref="Context"/> is a null reference.</exception>
        /// <remarks>
        /// Specify a null reference or an empty string for <paramref name="name"/> to reset the
        /// <see cref="Entity.Name"/> property of the indicated <see cref="Entity"/> to its default
        /// value, i.e. the value of the <see cref="Entity.DefaultName"/> property.</remarks>

        public void SetEntityName(string entityId, string name) {
            ExecuteInstruction(new SetEntityNameInstruction(entityId, name));
        }

        #endregion
        #region SetEntityOwner

        /// <summary>
        /// Executes a <see cref="SetEntityOwnerInstruction"/> with the specified <see
        /// cref="Entity"/> and <see cref="World.Faction"/> identifiers.</summary>
        /// <param name="entityId">
        /// The <see cref="Entity.Id"/> string of the <see cref="Entity"/> to manipulate.</param>
        /// <param name="factionId">
        /// The <see cref="World.Faction.Id"/> string of the new value for the <see
        /// cref="Entity.Owner"/> property of the indicated <see cref="Entity"/>.</param>
        /// <exception cref="InvalidCommandException"><para>
        /// <paramref name="entityId"/> is not a valid entity identifier.
        /// </para><para>-or-</para><para>
        /// <paramref name="factionId"/> is a non-empty string that is not a valid faction
        /// identifier.
        /// </para><para>-or-</para><para>
        /// <paramref name="factionId"/> violates an invariant of the indicated <see
        /// cref="Entity"/>.</para></exception>
        /// <exception cref="PropertyValueException">
        /// <see cref="Context"/> is a null reference.</exception>
        /// <remarks><para>
        /// <b>SetEntityOwner</b> calls <see cref="Entity.OnOwnerChanged"/> on the indicated <see
        /// cref="Entity"/> if instruction execution has changed the current <see
        /// cref="ExecutionContext.WorldState"/>.
        /// </para><para>
        /// Specify a null reference or an empty string for <paramref name="factionId"/> to set the
        /// <see cref="Entity.Owner"/> property of the indicated <see cref="Entity"/> to a null
        /// reference.</para></remarks>

        public void SetEntityOwner(string entityId, string factionId) {

            var instruction = new SetEntityOwnerInstruction(entityId, factionId);
            instruction.Results = new InstructionResults();

            if (ExecuteInstruction(instruction)) {
                Entity entity = instruction.Results.Entity;

                // notify entity of owner change
                entity.OnOwnerChanged(this, instruction.Results.Faction);
            }

            instruction.Results = null;
        }

        #endregion
        #region SetEntitySite

        /// <summary>
        /// Executes a <see cref="SetEntitySiteInstruction"/> with the specified <see
        /// cref="Entity"/> identifier and map location.</summary>
        /// <param name="entityId">
        /// The <see cref="Entity.Id"/> string of the <see cref="Entity"/> to manipulate.</param>
        /// <param name="location">
        /// The <see cref="Site.Location"/> of the new value for the <see cref="Entity.Site"/>
        /// property of the specified <see cref="Entity"/>.</param>
        /// <exception cref="InvalidCommandException"><para>
        /// <paramref name="entityId"/> is not a valid entity identifier.
        /// </para><para>-or-</para><para>
        /// <paramref name="location"/> violates an invariant of the indicated <see cref="Entity"/>.
        /// </para></exception>
        /// <exception cref="PropertyValueException">
        /// <see cref="Context"/> is a null reference.</exception>
        /// <remarks><para>
        /// <b>SetEntitySite</b> calls <see cref="Entity.OnSiteChanged"/> on the indicated <see
        /// cref="Entity"/> if instruction execution has changed the current <see
        /// cref="ExecutionContext.WorldState"/>.
        /// </para><para>
        /// Specify an invalid <paramref name="location"/> to set the <see cref="Entity.Site"/>
        /// property of the indicated <see cref="Entity"/> to a null reference.</para></remarks>

        public void SetEntitySite(string entityId, PointI location) {

            var instruction = new SetEntitySiteInstruction(entityId, location);
            instruction.Results = new InstructionResults();

            if (ExecuteInstruction(instruction)) {
                Entity entity = instruction.Results.Entity;

                // notify entity of site change
                entity.OnSiteChanged(this, instruction.Results.Site);
            }

            instruction.Results = null;
        }

        #endregion
        #region SetEntityUniqueName

        /// <summary>
        /// Executes a <see cref="SetEntityUniqueNameInstruction"/> with the specified <see
        /// cref="Entity"/> and <see cref="World.Faction"/> identifiers.</summary>
        /// <param name="entityId">
        /// The <see cref="Entity.Id"/> string of the <see cref="Entity"/> to manipulate.</param>
        /// <param name="factionId">
        /// The <see cref="World.Faction.Id"/> string of the <see cref="World.Faction"/> within
        /// whose possessions the new <see cref="Entity.Name"/> should be unique.</param>
        /// <exception cref="InvalidCommandException"><para>
        /// <paramref name="entityId"/> is not a valid entity identifier.
        /// </para><para>-or-</para><para>
        /// <paramref name="factionId"/> is not a valid faction identifier.</para></exception>
        /// <exception cref="PropertyValueException">
        /// <see cref="Context"/> is a null reference.</exception>
        /// <remarks>
        /// <b>SetEntityUniqueName</b> always generates a new <see cref="Entity.Name"/> with each
        /// invocation. Take care not to call this method repeatedly, unless the indicated <see
        /// cref="Entity"/> has changed owners.</remarks>

        public void SetEntityUniqueName(string entityId, string factionId) {
            ExecuteInstruction(new SetEntityUniqueNameInstruction(entityId, factionId));
        }

        #endregion
        #region SetEntityVariable

        /// <summary>
        /// Executes a <see cref="SetEntityVariableInstruction"/> with the specified <see
        /// cref="Entity"/> and <see cref="Variable"/> identifiers and basic value.</summary>
        /// <param name="entityId">
        /// The <see cref="Entity.Id"/> string of the <see cref="Entity"/> to manipulate.</param>
        /// <param name="variableId">
        /// The <see cref="VariableClass.Id"/> string of the <see cref="VariableClass"/> whose
        /// instance to add or change in the matching <see cref="VariableContainer"/> of the
        /// indicated <see cref="Entity"/>.</param>
        /// <param name="value">
        /// The new basic value to associate with the indicated <see cref="VariableClass"/>.</param>
        /// <exception cref="InvalidCommandException"><para>
        /// <paramref name="entityId"/> is not a valid entity identifier.
        /// </para><para>-or-</para><para>
        /// <paramref name="variableId"/> is not a valid variable identifier.</para></exception>
        /// <exception cref="PropertyValueException">
        /// <see cref="Context"/> is a null reference.</exception>
        /// <remarks><para>
        /// <b>SetEntityVariable</b> calls <see cref="Entity.OnVariableChanged"/> on the indicated
        /// <see cref="Entity"/> if instruction execution has changed the current <see
        /// cref="ExecutionContext.WorldState"/>.
        /// </para><para>
        /// <b>SetEntityVariable</b> adds a new element based on <paramref name="variableId"/> and
        /// <paramref name="value"/> to the matching <see cref="VariableContainer"/> of the
        /// indicated <see cref="Entity"/> if no instance value of <paramref name="variableId"/> is
        /// found.
        /// </para><para>
        /// The specified <paramref name="value"/> is restricted to the legal range for the new or
        /// changed element, as defined by its <see cref="Variable.Minimum"/> and <see
        /// cref="Variable.Maximum"/> values.</para></remarks>

        public void SetEntityVariable(string entityId, string variableId, int value) {

            var instruction = new SetEntityVariableInstruction(entityId, variableId, value);
            instruction.Results = new InstructionResults();

            if (ExecuteInstruction(instruction)) {
                Entity entity = instruction.Results.Entity;

                // notify entity of variable change
                entity.OnVariableChanged(this, instruction.Results.VariableClass, value, false);
            }

            instruction.Results = null;
        }

        #endregion
        #region SetEntityVariableInitial

        /// <summary>
        /// Executes a <see cref="SetEntityVariableInitialInstruction"/> with the specified <see
        /// cref="Entity"/> and <see cref="Variable"/> identifiers and initial basic value.
        /// </summary>
        /// <param name="entityId">
        /// The <see cref="Entity.Id"/> string of the <see cref="Entity"/> to manipulate.</param>
        /// <param name="variableId">
        /// The <see cref="VariableClass.Id"/> string of the <see cref="VariableClass"/> whose
        /// instance to add or change in the matching <see cref="VariableContainer"/> of the
        /// indicated <see cref="Entity"/>.</param>
        /// <param name="value">
        /// The new initial basic value to associate with the indicated <see cref="VariableClass"/>.
        /// </param>
        /// <exception cref="InvalidCommandException"><para>
        /// <paramref name="entityId"/> is not a valid entity identifier.
        /// </para><para>-or-</para><para>
        /// <paramref name="variableId"/> is not a valid variable identifier.</para></exception>
        /// <exception cref="PropertyValueException">
        /// <see cref="Context"/> is a null reference.</exception>
        /// <remarks><para>
        /// <b>SetEntityVariableInitial</b> calls <see cref="Entity.UpdateSelfAndUnitAttributes"/>
        /// (only if the indicated <see cref="VariableClass"/> is an <see cref="AttributeClass"/>)
        /// and then <see cref="Entity.OnVariableChanged"/> on the indicated <see cref="Entity"/> if
        /// instruction execution has changed the current <see cref="ExecutionContext.WorldState"/>.
        /// </para><para>
        /// <b>SetEntityVariableInitial</b> adds a new element based on <paramref
        /// name="variableId"/> and <paramref name="value"/> to the matching <see
        /// cref="VariableContainer"/> of the indicated <see cref="Entity"/> if no instance value of
        /// <paramref name="variableId"/> is found.
        /// </para><para>
        /// <b>SetEntityVariableInitial</b> may reset the current <see cref="Variable.Value"/> of an
        /// existing <see cref="Variable"/>, as described in <see cref="Variable.SetInitialValue"/>.
        /// </para><para>
        /// The specified <paramref name="value"/> is restricted to the legal range for the new or
        /// changed element, as defined by its <see cref="Variable.Minimum"/> and <see
        /// cref="Variable.Maximum"/> values, but ignoring its current <see
        /// cref="Variable.InitialValue"/>.</para></remarks>

        public void SetEntityVariableInitial(string entityId, string variableId, int value) {

            var instruction = new SetEntityVariableInitialInstruction(entityId, variableId, value);
            instruction.Results = new InstructionResults();

            if (ExecuteInstruction(instruction)) {
                Entity entity = instruction.Results.Entity;
                VariableClass variable = instruction.Results.VariableClass;

                // reapply any relevant attribute modifiers
                if (variable.Category == VariableCategory.Attribute)
                    entity.UpdateSelfAndUnitAttributes(this);

                // notify entity of variable change
                entity.OnVariableChanged(this, variable, value, false);
            }

            instruction.Results = null;
        }

        #endregion
        #region SetEntityVariableModifier

        /// <summary>
        /// Executes a <see cref="SetEntityVariableModifierInstruction"/> with the specified <see
        /// cref="Entity"/> and <see cref="Variable"/> identifiers, <see cref="ModifierTarget"/>,
        /// and modifier value.</summary>
        /// <param name="entityId">
        /// The <see cref="Entity.Id"/> string of the <see cref="Entity"/> to manipulate.</param>
        /// <param name="variableId">
        /// The <see cref="VariableClass.Id"/> string of the <see cref="VariableClass"/> whose
        /// instance to add or change in the matching <see cref="VariableContainer"/> of the
        /// indicated <see cref="Entity"/>.</param>
        /// <param name="target">
        /// A <see cref="ModifierTarget"/> value indicating which <see cref="VariableClass"/>
        /// modifier to change.</param>
        /// <param name="value">
        /// The new modifier value to associate with the indicated <see cref="VariableClass"/> and
        /// specified <paramref name="target"/>.</param>
        /// <exception cref="InvalidCommandException"><para>
        /// <paramref name="entityId"/> is not a valid entity identifier.
        /// </para><para>-or-</para><para>
        /// <paramref name="variableId"/> is not a valid variable identifier.</para></exception>
        /// <exception cref="PropertyValueException">
        /// <see cref="Context"/> is a null reference.</exception>
        /// <remarks><para>
        /// <b>SetEntityVariableModifier</b> calls <see cref="Entity.OnVariableChanged"/> on the
        /// indicated <see cref="Entity"/> if instruction execution has changed the current <see
        /// cref="ExecutionContext.WorldState"/>.
        /// </para><para>
        /// <b>SetEntityVariableModifier</b> adds a new element based on <paramref
        /// name="variableId"/>, <paramref name="target"/>, and <paramref name="value"/> to the
        /// matching <see cref="VariableContainer"/> of the indicated <see cref="Entity"/> if no
        /// instance value of <paramref name="variableId"/> is found.
        /// </para><para>
        /// The specified <paramref name="value"/> is restricted to the legal range for the new or
        /// changed element, as defined by its <see cref="Variable.Minimum"/> and <see
        /// cref="Variable.Maximum"/> values.</para></remarks>

        public void SetEntityVariableModifier(string entityId,
            string variableId, ModifierTarget target, int value) {

            var instruction = new SetEntityVariableModifierInstruction(
                entityId, variableId, target, value);
            instruction.Results = new InstructionResults();

            if (ExecuteInstruction(instruction)) {
                Entity entity = instruction.Results.Entity;

                // notify entity of variable change
                entity.OnVariableChanged(this, instruction.Results.VariableClass, value, true);
            }

            instruction.Results = null;
        }

        #endregion
        #region SetFactionResigned

        /// <summary>
        /// Executes a <see cref="SetFactionResignedInstruction"/> with the specified <see
        /// cref="World.Faction"/> identifier and resignation flag.</summary>
        /// <param name="factionId">
        /// The <see cref="World.Faction.Id"/> string of the <see cref="World.Faction"/> to
        /// manipulate.</param>
        /// <param name="resigned">
        /// The new value for the <see cref="World.Faction.IsResigned"/> property of the indicated
        /// <see cref="World.Faction"/>.</param>
        /// <exception cref="InvalidCommandException">
        /// <paramref name="factionId"/> is not a valid faction identifier.</exception>
        /// <exception cref="PropertyValueException">
        /// <see cref="Context"/> is a null reference.</exception>

        public void SetFactionResigned(string factionId, bool resigned) {
            ExecuteInstruction(new SetFactionResignedInstruction(factionId, resigned));
        }

        #endregion
        #region SetFactionUnitsCanAttack

        /// <summary>
        /// Executes a <see cref="SetFactionUnitsCanAttackInstruction"/> with the specified <see
        /// cref="World.Faction"/> and <see cref="UnitClass"/> identifiers and attack flag.
        /// </summary>
        /// <param name="factionId">
        /// The <see cref="World.Faction.Id"/> string of the <see cref="World.Faction"/> to
        /// manipulate.</param>
        /// <param name="classId">
        /// The <see cref="EntityClass.Id"/> string of the <see cref="UnitClass"/> whose instances
        /// to manipulate.</param>
        /// <param name="canAttack">
        /// The new value for the <see cref="Unit.CanAttack"/> flag of all specified <see
        /// cref="Unit"/> objects.</param>
        /// <exception cref="InvalidCommandException"><para>
        /// <paramref name="factionId"/> is not a valid faction identifier.
        /// </para><para>-or-</para><para>
        /// <paramref name="classId"/> is not a valid unit class identifier.</para></exception>
        /// <exception cref="PropertyValueException">
        /// <see cref="Context"/> is a null reference.</exception>
        /// <remarks>
        /// Specify a null reference or an empty string for <paramref name="classId"/> to manipulate
        /// all <see cref="World.Faction.Units"/> of the indicated <see cref="World.Faction"/>,
        /// regardless of their <see cref="Entity.EntityClass"/>.</remarks>

        public void SetFactionUnitsCanAttack(string factionId, string classId, bool canAttack) {
            ExecuteInstruction(new SetFactionUnitsCanAttackInstruction(factionId, classId, canAttack));
        }

        #endregion
        #region SetFactionUnitsCanMove

        /// <summary>
        /// Executes a <see cref="SetFactionUnitsCanMoveInstruction"/> with the specified <see
        /// cref="World.Faction"/> and <see cref="UnitClass"/> identifiers and movement flag.
        /// </summary>
        /// <param name="factionId">
        /// The <see cref="World.Faction.Id"/> string of the <see cref="World.Faction"/> to
        /// manipulate.</param>
        /// <param name="classId">
        /// The <see cref="EntityClass.Id"/> string of the <see cref="UnitClass"/> whose instances
        /// to manipulate.</param>
        /// <param name="canMove">
        /// The new value for the <see cref="Unit.CanMove"/> flag of all specified <see
        /// cref="Unit"/> objects.</param>
        /// <exception cref="InvalidCommandException"><para>
        /// <paramref name="factionId"/> is not a valid faction identifier.
        /// </para><para>-or-</para><para>
        /// <paramref name="classId"/> is not a valid unit class identifier.</para></exception>
        /// <exception cref="PropertyValueException">
        /// <see cref="Context"/> is a null reference.</exception>
        /// <remarks>
        /// Specify a null reference or an empty string for <paramref name="classId"/> to manipulate
        /// all <see cref="World.Faction.Units"/> of the indicated <see cref="World.Faction"/>,
        /// regardless of their <see cref="Entity.EntityClass"/>.</remarks>

        public void SetFactionUnitsCanMove(string factionId, string classId, bool canMove) {
            ExecuteInstruction(new SetFactionUnitsCanMoveInstruction(factionId, classId, canMove));
        }

        #endregion
        #region SetFactionVariable

        /// <summary>
        /// Executes a <see cref="SetFactionVariableInstruction"/> with the specified <see
        /// cref="World.Faction"/> and <see cref="Variable"/> identifiers and basic value.
        /// </summary>
        /// <param name="factionId">
        /// The <see cref="World.Faction.Id"/> string of the <see cref="World.Faction"/> to
        /// manipulate.</param>
        /// <param name="variableId">
        /// The <see cref="VariableClass.Id"/> string of the <see cref="VariableClass"/> whose
        /// instance to add or change in the matching <see cref="VariableContainer"/> of the
        /// indicated <see cref="World.Faction"/>.</param>
        /// <param name="value">
        /// The new basic value to associate with the indicated <see cref="VariableClass"/>.</param>
        /// <exception cref="InvalidCommandException"><para>
        /// <paramref name="factionId"/> is not a valid faction identifier.
        /// </para><para>-or-</para><para>
        /// <paramref name="variableId"/> is not a valid variable identifier.</para></exception>
        /// <exception cref="PropertyValueException">
        /// <see cref="Context"/> is a null reference.</exception>
        /// <remarks><para>
        /// <b>SetFactionVariable</b> calls <see cref="World.Faction.OnVariableChanged"/> on the
        /// indicated <see cref="World.Faction"/> if instruction execution has changed the current
        /// <see cref="ExecutionContext.WorldState"/>.
        /// </para><para>
        /// <b>SetFactionVariable</b> adds a new element based on <paramref name="variableId"/> and
        /// <paramref name="value"/> to the matching <see cref="VariableContainer"/> of the
        /// indicated <see cref="World.Faction"/> if no instance value of <paramref
        /// name="variableId"/> is found.
        /// </para><para>
        /// The specified <paramref name="value"/> is restricted to the legal range for the new or
        /// changed element, as defined by its <see cref="Variable.Minimum"/> and <see
        /// cref="Variable.Maximum"/> values.</para></remarks>

        public void SetFactionVariable(string factionId, string variableId, int value) {

            var instruction = new SetFactionVariableInstruction(factionId, variableId, value);
            instruction.Results = new InstructionResults();

            if (ExecuteInstruction(instruction)) {
                Faction faction = instruction.Results.Faction;

                // notify faction of variable change
                faction.OnVariableChanged(this, instruction.Results.VariableClass, value, false);
            }

            instruction.Results = null;
        }

        #endregion
        #region SetFactionVariableInitial

        /// <summary>
        /// Executes a <see cref="SetFactionVariableInitialInstruction"/> with the specified <see
        /// cref="World.Faction"/> and <see cref="Variable"/> identifiers and initial basic value.
        /// </summary>
        /// <param name="factionId">
        /// The <see cref="World.Faction.Id"/> string of the <see cref="World.Faction"/> to
        /// manipulate.</param>
        /// <param name="variableId">
        /// The <see cref="VariableClass.Id"/> string of the <see cref="VariableClass"/> whose
        /// instance to add or change in the matching <see cref="VariableContainer"/> of the
        /// indicated <see cref="World.Faction"/>.</param>
        /// <param name="value">
        /// The new initial basic value to associate with the indicated <see cref="VariableClass"/>.
        /// </param>
        /// <exception cref="InvalidCommandException"><para>
        /// <paramref name="factionId"/> is not a valid faction identifier.
        /// </para><para>-or-</para><para>
        /// <paramref name="variableId"/> is not a valid variable identifier.</para></exception>
        /// <exception cref="PropertyValueException">
        /// <see cref="Context"/> is a null reference.</exception>
        /// <remarks><para>
        /// <b>SetFactionVariableInitial</b> calls <see cref="World.Faction.OnVariableChanged"/> on
        /// the indicated <see cref="World.Faction"/> if instruction execution has changed the
        /// current <see cref="ExecutionContext.WorldState"/>.
        /// </para><para>
        /// <b>SetFactionVariableInitial</b> adds a new element based on <paramref
        /// name="variableId"/> and <paramref name="value"/> to the matching <see
        /// cref="VariableContainer"/> of the indicated <see cref="World.Faction"/> if no instance
        /// value of <paramref name="variableId"/> is found.
        /// </para><para>
        /// <b>SetFactionVariableInitial</b> may reset the current <see cref="Variable.Value"/> of
        /// an existing <see cref="Variable"/>, as described in <see
        /// cref="Variable.SetInitialValue"/>.
        /// </para><para>
        /// The specified <paramref name="value"/> is restricted to the legal range for the new or
        /// changed element, as defined by its <see cref="Variable.Minimum"/> and <see
        /// cref="Variable.Maximum"/> values, but ignoring its current <see
        /// cref="Variable.InitialValue"/>.</para></remarks>

        public void SetFactionVariableInitial(string factionId, string variableId, int value) {

            var instruction = new SetFactionVariableInitialInstruction(factionId, variableId, value);
            instruction.Results = new InstructionResults();

            if (ExecuteInstruction(instruction)) {
                Faction faction = instruction.Results.Faction;

                // notify faction of variable change
                faction.OnVariableChanged(this, instruction.Results.VariableClass, value, false);
            }

            instruction.Results = null;
        }

        #endregion
        #region SetFactionVariableModifier

        /// <summary>
        /// Executes a <see cref="SetFactionVariableModifierInstruction"/> with the specified <see
        /// cref="World.Faction"/> and <see cref="Variable"/> identifiers and modifier value.
        /// </summary>
        /// <param name="factionId">
        /// The <see cref="World.Faction.Id"/> string of the <see cref="World.Faction"/> to
        /// manipulate.</param>
        /// <param name="variableId">
        /// The <see cref="VariableClass.Id"/> string of the <see cref="VariableClass"/> whose
        /// instance to add or change in the matching <see cref="VariableContainer"/> of the
        /// indicated <see cref="World.Faction"/>.</param>
        /// <param name="value">
        /// The new modifier value to associate with the indicated <see cref="VariableClass"/>.
        /// </param>
        /// <exception cref="InvalidCommandException"><para>
        /// <paramref name="factionId"/> is not a valid faction identifier.
        /// </para><para>-or-</para><para>
        /// <paramref name="variableId"/> is not a valid variable identifier.</para></exception>
        /// <exception cref="PropertyValueException">
        /// <see cref="Context"/> is a null reference.</exception>
        /// <remarks><para>
        /// <b>SetFactionVariableModifier</b> calls <see cref="World.Faction.OnVariableChanged"/> on
        /// the indicated <see cref="World.Faction"/> if instruction execution has changed the
        /// current <see cref="ExecutionContext.WorldState"/>.
        /// </para><para>
        /// <b>SetFactionVariableModifier</b> adds a new element based on <paramref
        /// name="variableId"/> and <paramref name="value"/> to the matching <see
        /// cref="VariableContainer"/> of the indicated <see cref="World.Faction"/> if no instance
        /// value of <paramref name="variableId"/> is found.
        /// </para><para>
        /// The specified <paramref name="value"/> is restricted to the legal range for the new or
        /// changed element, as defined by its <see cref="Variable.Minimum"/> and <see
        /// cref="Variable.Maximum"/> values.</para></remarks>

        public void SetFactionVariableModifier(string factionId, string variableId, int value) {

            var instruction = new SetFactionVariableModifierInstruction(factionId, variableId, value);
            instruction.Results = new InstructionResults();

            if (ExecuteInstruction(instruction)) {
                Faction faction = instruction.Results.Faction;

                // notify faction of variable change
                faction.OnVariableChanged(this, instruction.Results.VariableClass, value, true);
            }

            instruction.Results = null;
        }

        #endregion
        #region SetSiteOwner

        /// <summary>
        /// Executes a <see cref="SetSiteOwnerInstruction"/> the specified map location and <see
        /// cref="World.Faction"/> identifier.</summary>
        /// <param name="location">
        /// The <see cref="Site.Location"/> of the <see cref="Site"/> to manipulate.</param>
        /// <param name="factionId">
        /// The <see cref="World.Faction.Id"/> string of the new value for the <see
        /// cref="Site.Owner"/> property of the indicated <see cref="Site"/>.</param>
        /// <exception cref="InvalidCommandException"><para>
        /// <paramref name="location"/> is not a valid map location.
        /// </para><para>-or-</para><para>
        /// <paramref name="factionId"/> is a non-empty string that is not a valid faction
        /// identifier.</para></exception>
        /// <exception cref="PropertyValueException">
        /// <see cref="Context"/> is a null reference.</exception>
        /// <remarks><para>
        /// <b>SetSiteOwner</b> calls <see cref="Entity.OnOwnerChanged"/> on all <see
        /// cref="Site.Terrains"/> and <see cref="Site.Effects"/> on the indicated <see
        /// cref="Site"/> if instruction execution has changed the current <see
        /// cref="ExecutionContext.WorldState"/>.
        /// </para><para>
        /// Specify a null reference or an empty string for <paramref name="factionId"/> to set the
        /// <see cref="Site.Owner"/> property of the indicated <see cref="Site"/> to a null
        /// reference.</para></remarks>

        public void SetSiteOwner(PointI location, string factionId) {

            var instruction = new SetSiteOwnerInstruction(location, factionId);
            instruction.Results = new InstructionResults();

            if (ExecuteInstruction(instruction)) {
                Site site = instruction.Results.Site;
                Faction oldOwner = instruction.Results.Faction;

                // broadcast owner change to all terrains on site
                for (int i = 0; i < site.Terrains.Count; i++)
                    site.Terrains[i].OnOwnerChanged(this, oldOwner);

                // broadcast owner change to all effects on site
                for (int i = 0; i < site.Effects.Count; i++)
                    site.Effects[i].OnOwnerChanged(this, oldOwner);
            }

            instruction.Results = null;
        }

        #endregion
        #region SetSiteUnitOwner

        /// <summary>
        /// Executes a <see cref="SetSiteUnitOwnerInstruction"/> with the specified map location and
        /// <see cref="World.Faction"/> identifier.</summary>
        /// <param name="location">
        /// The <see cref="Site.Location"/> of the <see cref="Site"/> to manipulate.</param>
        /// <param name="factionId">
        /// The <see cref="World.Faction.Id"/> string of the new value for the <see
        /// cref="Entity.Owner"/> property of all <see cref="Site.Units"/> contained in the
        /// indicated <see cref="Site"/>.</param>
        /// <exception cref="InvalidCommandException"><para>
        /// <paramref name="location"/> is not a valid map location.
        /// </para><para>-or-</para><para>
        /// <paramref name="factionId"/> is a non-empty string that is not a valid faction
        /// identifier.</para></exception>
        /// <exception cref="PropertyValueException">
        /// <see cref="Context"/> is a null reference.</exception>
        /// <remarks><para>
        /// <b>SetSiteUnitOwner</b> calls <see cref="Entity.OnOwnerChanged"/> on all <see
        /// cref="Site.Units"/> on the indicated <see cref="Site"/> if instruction execution has
        /// changed the current <see cref="ExecutionContext.WorldState"/>.
        /// </para><para>
        /// Specify a null reference or an empty string for <paramref name="factionId"/> to set the
        /// <see cref="Entity.Owner"/> property of all <see cref="Site.Units"/> contained in the
        /// indicated <see cref="Site"/> to a null reference.</para></remarks>

        public void SetSiteUnitOwner(PointI location, string factionId) {

            var instruction = new SetSiteUnitOwnerInstruction(location, factionId);
            instruction.Results = new InstructionResults();

            if (ExecuteInstruction(instruction)) {
                Site site = instruction.Results.Site;
                Faction oldOwner = instruction.Results.Faction;

                // broadcast owner change to all units on site
                for (int i = 0; i < site.Units.Count; i++)
                    site.Units[i].OnOwnerChanged(this, oldOwner);
            }

            instruction.Results = null;
        }

        #endregion
        #region SetUnitCanAttack

        /// <summary>
        /// Executes a <see cref="SetUnitCanAttackInstruction"/> with the specified <see
        /// cref="Unit"/> identifier and attack flag.</summary>
        /// <param name="unitId">
        /// The <see cref="Entity.Id"/> string of the <see cref="Unit"/> to manipulate.</param>
        /// <param name="canAttack">
        /// The new value for the <see cref="Unit.CanAttack"/> flag of the specified <see
        /// cref="Unit"/>.</param>
        /// <exception cref="InvalidCommandException">
        /// <paramref name="unitId"/> is not a valid unit identifier.</exception>
        /// <exception cref="PropertyValueException">
        /// <see cref="Context"/> is a null reference.</exception>

        public void SetUnitCanAttack(string unitId, bool canAttack) {
            ExecuteInstruction(new SetUnitCanAttackInstruction(unitId, canAttack));
        }

        #endregion
        #region SetUnitCanMove

        /// <summary>
        /// Executes a <see cref="SetUnitCanMoveInstruction"/> with the specified <see cref="Unit"/>
        /// identifier and movement flag.</summary>
        /// <param name="unitId">
        /// The <see cref="Entity.Id"/> string of the <see cref="Unit"/> to manipulate.</param>
        /// <param name="canMove">
        /// The new value for the <see cref="Unit.CanMove"/> flag of the specified <see
        /// cref="Unit"/>.</param>
        /// <exception cref="InvalidCommandException">
        /// <paramref name="unitId"/> is not a valid unit identifier.</exception>
        /// <exception cref="PropertyValueException">
        /// <see cref="Context"/> is a null reference.</exception>

        public void SetUnitCanMove(string unitId, bool canMove) {
            ExecuteInstruction(new SetUnitCanMoveInstruction(unitId, canMove));
        }

        #endregion
        #region SetWinningFaction

        /// <summary>
        /// Executes a <see cref="SetWinningFactionInstruction"/> with the specified <see
        /// cref="World.Faction"/> identifier.</summary>
        /// <param name="factionId">
        /// The <see cref="World.Faction.Id"/> string of the victorious <see cref="World.Faction"/>.
        /// </param>
        /// <exception cref="InvalidCommandException">
        /// <paramref name="factionId"/> is a non-empty string that is not a valid faction
        /// identifier.</exception>
        /// <exception cref="PropertyValueException">
        /// <see cref="Context"/> is a null reference.</exception>
        /// <remarks>
        /// Specify a null reference or an empty string for <paramref name="factionId"/> to set the
        /// <see cref="WorldState.WinningFaction"/> property of the current <see cref="WorldState"/>
        /// to a null reference.</remarks>

        internal void SetWinningFaction(string factionId) {
            ExecuteInstruction(new SetWinningFactionInstruction(factionId));
        }

        #endregion
        #region ShowImage

        /// <summary>
        /// Executes a <see cref="ShowImageInstruction"/> with the specified <see
        /// cref="EntityClass"/> identifier, map locations, and display delay.</summary>
        /// <param name="classId">
        /// The <see cref="EntityClass.Id"/> string of the <see cref="EntityClass"/> whose image to
        /// show.</param>
        /// <param name="sites">
        /// An <see cref="Array"/> containing the coordinates of each <see cref="Site"/> to show the
        /// image on.</param>
        /// <param name="delay">
        /// The duration, in milliseconds, for which the image is shown on each <see cref="Site"/>.
        /// If this argument is non-positive, a default value of 250 msec is assumed.</param>
        /// <exception cref="PropertyValueException">
        /// <see cref="Context"/> is a null reference.</exception>
        /// <remarks>
        /// <b>ShowImage</b> does nothing if the specified <paramref name="classId"/> is not a valid
        /// entity class identifier; or if the specified <paramref name="sites"/> array is a null
        /// reference or an empty array, or contains any invalid map locations.</remarks>

        public void ShowImage(string classId, PointI[] sites, int delay) {
            ExecuteInstruction(new ShowImageInstruction(classId, sites, delay));
        }

        #endregion
        #region ShowMessage

        /// <summary>
        /// Executes a <see cref="ShowMessageInstruction"/> with the specified summary and detail
        /// text, <see cref="World.Faction"/> identifier, and display names.</summary>
        /// <param name="summary">
        /// A summary description of the message event.</param>
        /// <param name="details">
        /// A detailed description of the message event.</param>
        /// <param name="factionId">
        /// The <see cref="World.Faction.Id"/> string of the <see cref="World.Faction"/> primarily
        /// affected by the message event.</param>
        /// <param name="names">
        /// An <see cref="Array"/> containing the display names of all entities affected by the
        /// message event.</param>
        /// <exception cref="PropertyValueException">
        /// <see cref="Context"/> is a null reference.</exception>
        /// <remarks>
        /// <b>ShowMessage</b> does nothing if the specified <paramref name="summary"/> is a null
        /// reference or an empty string. The other parameters are ignored if they are null
        /// references, empty strings, or empty arrays.</remarks>

        public void ShowMessage(string summary, string details, string factionId, string[] names) {
            ExecuteInstruction(new ShowMessageInstruction(summary, details, factionId, names));
        }

        #endregion
        #region ShowMessageDialog

        /// <summary>
        /// Executes a <see cref="ShowMessageDialogInstruction"/> with the specified summary and
        /// detail text, <see cref="World.Faction"/> identifier, and display names.</summary>
        /// <param name="summary">
        /// A summary description of the message event.</param>
        /// <param name="details">
        /// A detailed description of the message event.</param>
        /// <param name="factionId">
        /// The <see cref="World.Faction.Id"/> string of the <see cref="World.Faction"/> primarily
        /// affected by the message event.</param>
        /// <param name="names">
        /// An <see cref="Array"/> containing the display names of all entities affected by the
        /// message event.</param>
        /// <exception cref="PropertyValueException">
        /// <see cref="Context"/> is a null reference.</exception>
        /// <remarks>
        /// <b>ShowMessageDialog</b> does nothing if the specified <paramref name="summary"/> is a
        /// null reference or an empty string. The other parameters are ignored if they are null
        /// references, empty strings, or empty arrays.</remarks>

        public void ShowMessageDialog(string summary,
            string details, string factionId, string[] names) {

            ExecuteInstruction(new ShowMessageDialogInstruction(summary, details, factionId, names));
        }

        #endregion
        #endregion
        #region XmlSerializable Members
        #region XmlName

        /// <summary>
        /// Gets the name of the XML element associated with the <see cref="Command"/>.</summary>
        /// <value>
        /// The name of the XML element to which the data of this <see cref="Command"/> object is
        /// serialized.</value>
        /// <exception cref="NotImplementedException">
        /// The property was accessed on an abstract base class.</exception>
        /// <remarks><para>
        /// <b>XmlName</b> returns the <see cref="System.Reflection.MemberInfo.Name"/> of the actual
        /// concrete <see cref="Type"/> of this object, without the suffix "Command" if present.
        /// </para><para>
        /// Please refer to class <see cref="XmlSerializable"/> for a complete description of this
        /// property.</para></remarks>

        internal override string XmlName {
            get {
                Type type = GetType();
                if (type.IsAbstract)
                    ThrowHelper.ThrowNotImplementedException(Tektosyne.Strings.PropertyAbstract);

                // remove suffix "Command" if present
                string name = type.Name;
                int suffix = name.IndexOf("Command", StringComparison.Ordinal);
                if (suffix > 0) name = name.Remove(suffix);

                return name;
            }
        }

        #endregion
        #region ReadXmlAttributes

        /// <summary>
        /// Reads XML attribute data into the <see cref="Command"/> object using the specified <see
        /// cref="XmlReader"/>.</summary>
        /// <param name="reader">
        /// The <see cref="XmlReader"/> from which to read.</param>
        /// <exception cref="XmlException">
        /// An error occurred while parsing the XML data provided by <paramref name="reader"/>.
        /// </exception>
        /// <exception cref="System.Xml.Schema.XmlSchemaException">
        /// <paramref name="reader"/> is an <see cref="XmlValidatingReader"/>, and the XML data did
        /// not conform to <see cref="FilePaths.SessionSchema"/>.</exception>
        /// <remarks>
        /// Please refer to class <see cref="XmlSerializable"/> for a complete description of this
        /// method.</remarks>

        protected override void ReadXmlAttributes(XmlReader reader) {
            XmlUtility.ReadAttributeAsInt32(reader, "turn", ref this._turn);

            string idRef = reader["faction"];
            if (!String.IsNullOrEmpty(idRef))
                this._faction = new FactionReference(idRef);
        }

        #endregion
        #region ReadXmlElements

        /// <summary>
        /// Reads XML element data into the <see cref="Command"/> object using the specified <see
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
        /// <remarks><para>
        /// Please refer to class <see cref="XmlSerializable"/> for a complete description of this
        /// method.
        /// </para><para>
        /// If the specified <paramref name="reader"/> is positioned on an XML element named
        /// "program", <b>ReadXmlElement</b> sets the <see cref="HasProgram"/> property to
        /// <c>true</c>, regardless of whether any nested elements are found.</para></remarks>

        protected override bool ReadXmlElements(XmlReader reader) {

            // Command only holds "program"
            if (reader.Name != "program")
                return false;

            // HCL program may be empty
            this._hasProgram = true;
            if (reader.IsEmptyElement)
                return true;

            // prepare for deserializing HCL program
            Assembly assembly = Assembly.GetExecutingAssembly();
            string prefix = "Hexkit.World.Instructions.";
            string suffix = "Instruction";
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;

            // read HCL instruction sequence
            while (reader.Read() && reader.IsStartElement()) {

                // attempt to instantiate instruction
                Instruction instruction = (Instruction) assembly.CreateInstance(
                    prefix + reader.Name + suffix, false, flags, null, null, null, null);

                if (instruction != null) {
                    // read instruction and add to program
                    instruction.ReadXml(reader);
                    this._program.Add(instruction);
                }
                else {
                    // skip to end tag of unknown element
                    XmlUtility.MoveToEndElement(reader);
                }
            }

            return true;
        }

        #endregion
        #region WriteXmlAttributes

        /// <summary>
        /// Writes all current data of the <see cref="Command"/> object that is serialized to XML
        /// attributes to the specified <see cref="XmlWriter"/>.</summary>
        /// <param name="writer">
        /// The <see cref="XmlWriter"/> to which to write.</param>
        /// <remarks>
        /// Please refer to class <see cref="XmlSerializable"/> for a complete description of this
        /// method.</remarks>

        protected override void WriteXmlAttributes(XmlWriter writer) {
            writer.WriteAttributeString("turn", XmlConvert.ToString(Turn));

            if (Faction.Id != null)
                writer.WriteAttributeString("faction", Faction.Id);
        }

        #endregion
        #region WriteXmlElements

        /// <summary>
        /// Writes all current data of the <see cref="Command"/> object that is serialized to nested
        /// XML elements to the specified <see cref="XmlWriter"/>.</summary>
        /// <param name="writer">
        /// The <see cref="XmlWriter"/> to which to write.</param>
        /// <remarks>
        /// Please refer to class <see cref="XmlSerializable"/> for a complete description of this
        /// method.</remarks>

        protected override void WriteXmlElements(XmlWriter writer) {
            writer.WriteStartElement("program");

            foreach (Instruction instruction in Program)
                instruction.WriteXml(writer);

            writer.WriteEndElement();
        }

        #endregion
        #endregion
    }
}
