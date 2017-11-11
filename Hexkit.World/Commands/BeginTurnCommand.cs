using System;
using System.Diagnostics;

using Tektosyne;
using Hexkit.Global;
using Hexkit.World.Instructions;

namespace Hexkit.World.Commands {

    /// <summary>
    /// Represents a "Begin Turn" command.</summary>
    /// <remarks>
    /// <b>BeginTurnCommand</b> is serialized to the XML element "BeginTurn" defined in <see
    /// cref="FilePaths.SessionSchema"/>.</remarks>

    public sealed class BeginTurnCommand: Command {
        #region BeginTurnCommand()

        /// <overloads>
        /// Initializes a new instance of the <see cref="BeginTurnCommand"/> class.</overloads>
        /// <summary>
        /// Initializes a new instance of the <see cref="BeginTurnCommand"/> class with default
        /// properties.</summary>

        internal BeginTurnCommand(): base() { }

        #endregion
        #region BeginTurnCommand(Faction)

        /// <summary>
        /// Initializes a new instance of the <see cref="BeginTurnCommand"/> class with the
        /// specified <see cref="WorldState"/>.</summary>
        /// <param name="faction">
        /// The initial value for the <see cref="Command.Faction"/> property.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="faction"/> is a null reference.</exception>

        public BeginTurnCommand(Faction faction): base(faction) { }

        #endregion
        #region Name

        /// <summary>
        /// Gets the display name of the <see cref="BeginTurnCommand"/>.</summary>
        /// <value>
        /// The display name of this <see cref="BeginTurnCommand"/>.</value>
        /// <remarks>
        /// <b>Name</b> returns the constant literal string "Begin Turn" which should be used to
        /// represent the <see cref="BeginTurnCommand"/> within Hexkit Game.</remarks>

        public override string Name {
            [DebuggerStepThrough]
            get { return "Begin Turn"; }
        }

        #endregion
        #region GenerateProgram

        /// <summary>
        /// Generates and executes the HCL <see cref="Command.Program"/> for the <see
        /// cref="BeginTurnCommand"/>.</summary>
        /// <exception cref="InvalidCommandException">
        /// The <see cref="BeginTurnCommand"/> contains data that is invalid with respect to the
        /// current <see cref="Command.Context"/>.</exception>
        /// <exception cref="PropertyValueException">
        /// <see cref="Command.Context"/> is a null reference.</exception>
        /// <remarks><para>
        /// <b>GenerateProgram</b> first performs the following actions if the associated <see
        /// cref="WorldState.History"/> does not yet contain any <see cref="History.Commands"/>:
        /// </para><list type="number"><item>
        /// Set the <see cref="WorldState.UnitAttributeModifiersChanged"/> flag of the current <see
        /// cref="ExecutionContext.WorldState"/> to <c>false</c>, due to the next step.
        /// </item><item>
        /// Invoke <see cref="Entity.UpdateAttributes"/> on all <see cref="WorldState.Entities"/> of
        /// the current <see cref="ExecutionContext.WorldState"/>. This sets all entity <see
        /// cref="Entity.Attributes"/> to their correct initial values.
        /// </item><item>
        /// Invoke <see cref="Faction.Initialize"/> on all <see cref="WorldState.Factions"/> in the
        /// current <see cref="ExecutionContext.WorldState"/>. This allows the rule script to
        /// perform custom <see cref="Faction"/> initialization.
        /// </item><item>
        /// Generate and execute a <see cref="CreateFactionInstruction"/> for all <see
        /// cref="WorldState.Factions"/> in the current <b>WorldState</b>.
        /// </item></list><para>
        /// <b>GenerateProgram</b> always performs the following actions:
        /// </para><list type="number"><item>
        /// Invoke <see cref="Faction.BeginTurn"/> on the associated <see cref="Command.Faction"/>.
        /// </item><item>
        /// Invoke <see cref="WorldState.CheckVictory"/> on the current <see
        /// cref="ExecutionContext.WorldState"/>.</item></list></remarks>

        protected override void GenerateProgram() {

            WorldState world = Context.WorldState;
            if (world.History.Commands.Count == 0) {
                world.UnitAttributeModifiersChanged = false;

                // initialize attributes of all entities
                foreach (Entity entity in world.Entities.Values)
                    entity.UpdateAttributes(this);

                // complete initialization of all factions
                foreach (Faction faction in world.Factions) {
                    faction.Initialize(this);
                    CreateFaction(faction.Id);
                }
            }

            Faction.Value.BeginTurn(this);
            world.CheckVictory(this);
        }

        #endregion
        #region Validate

        /// <summary>
        /// Validates the current data of the <see cref="BeginTurnCommand"/> against the specified
        /// <see cref="WorldState"/>.</summary>
        /// <param name="worldState">
        /// The <see cref="WorldState"/> to validate the <see cref="BeginTurnCommand"/> against.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="worldState"/> is a null reference.</exception>
        /// <exception cref="InvalidCommandException">
        /// One of the conditions listed under <see cref="Command.Validate"/>.</exception>
        /// <remarks>
        /// Please refer to <see cref="Command.Validate"/> for details. This override performs
        /// additional validation specific to the <see cref="BeginTurnCommand"/>.</remarks>

        public override void Validate(WorldState worldState) {
            base.Validate(worldState);

            if (worldState.ActiveFaction.Id != Faction.Id)
                InvalidCommandException.ThrowNewWithFormat(Global.Strings.CommandFactionConflict,
                    Name, Faction.Id, worldState.ActiveFaction.Id);
        }

        #endregion
    }
}
