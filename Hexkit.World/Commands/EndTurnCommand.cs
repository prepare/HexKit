using System;
using System.Diagnostics;

using Tektosyne;
using Hexkit.Global;
using Hexkit.World.Instructions;

namespace Hexkit.World.Commands {

    /// <summary>
    /// Represents an "End Turn" command.</summary>
    /// <remarks>
    /// <b>EndTurnCommand</b> is serialized to the XML element "EndTurn" defined in <see
    /// cref="FilePaths.SessionSchema"/>.</remarks>

    public sealed class EndTurnCommand: Command {
        #region EndTurnCommand()

        /// <overloads>
        /// Initializes a new instance of the <see cref="EndTurnCommand"/> class.</overloads>
        /// <summary>
        /// Initializes a new instance of the <see cref="EndTurnCommand"/> class with default
        /// properties.</summary>

        internal EndTurnCommand(): base() { }

        #endregion
        #region EndTurnCommand(Faction)

        /// <summary>
        /// Initializes a new instance of the <see cref="EndTurnCommand"/> class with the specified
        /// <see cref="WorldState"/>.</summary>
        /// <param name="faction">
        /// The initial value for the <see cref="Command.Faction"/> property.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="faction"/> is a null reference.</exception>

        public EndTurnCommand(Faction faction): base(faction) { }

        #endregion
        #region Name

        /// <summary>
        /// Gets the display name of the <see cref="EndTurnCommand"/>.</summary>
        /// <value>
        /// The display name of this <see cref="EndTurnCommand"/>.</value>
        /// <remarks>
        /// <b>Name</b> returns the constant literal string "End Turn". which should be used to
        /// represent the <see cref="EndTurnCommand"/> within Hexkit Game.</remarks>

        public override string Name {
            [DebuggerStepThrough]
            get { return "End Turn"; }
        }

        #endregion
        #region GenerateProgram

        /// <summary>
        /// Generates and executes the HCL <see cref="Command.Program"/> for the <see
        /// cref="EndTurnCommand"/>.</summary>
        /// <exception cref="InvalidCommandException">
        /// The <see cref="EndTurnCommand"/> contains data that is invalid with respect to the
        /// current <see cref="Command.Context"/>.</exception>
        /// <exception cref="PropertyValueException">
        /// <see cref="Command.Context"/> is a null reference.</exception>
        /// <remarks><para>
        /// <b>GenerateProgram</b> performs the following actions:
        /// </para><list type="number"><item>
        /// Invoke <see cref="Faction.EndTurn"/> on the associated <see cref="Command.Faction"/>.
        /// </item><item>
        /// Generate and execute an <see cref="AdvanceFactionInstruction"/>.
        /// </item><item>
        /// Invoke <see cref="WorldState.CheckDefeat"/> on the current <see
        /// cref="ExecutionContext.WorldState"/>.</item></list></remarks>

        protected override void GenerateProgram() {
            Faction.Value.EndTurn(this);

            /*
             * AdvanceFaction must be called before CheckDefeat
             * because the ActiveFaction might be defeated.
             *
             * In this case, CheckDefeat deletes the ActiveFaction,
             * and the (unchanged) ActiveFactionIndex then indicates
             * the next surviving faction.
             *
             * But AdvanceFaction increments the ActiveFactionIndex
             * which would skip a faction in this case.
             */

            AdvanceFaction();
            Context.WorldState.CheckDefeat(this);
        }

        #endregion
        #region Validate

        /// <summary>
        /// Validates the current data of the <see cref="EndTurnCommand"/> against the specified
        /// <see cref="WorldState"/>.</summary>
        /// <param name="worldState">
        /// The <see cref="WorldState"/> to validate the <see cref="EndTurnCommand"/> against.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="worldState"/> is a null reference.</exception>
        /// <exception cref="InvalidCommandException">
        /// One of the conditions listed under <see cref="Command.Validate"/>.</exception>
        /// <remarks>
        /// Please refer to <see cref="Command.Validate"/> for details. This override performs
        /// additional validation specific to the <see cref="EndTurnCommand"/>.</remarks>

        public override void Validate(WorldState worldState) {
            base.Validate(worldState);

            if (worldState.ActiveFaction.Id != Faction.Id)
                InvalidCommandException.ThrowNewWithFormat(Global.Strings.CommandFactionConflict,
                    Name, Faction.Id, worldState.ActiveFaction.Id);
        }

        #endregion
    }
}
