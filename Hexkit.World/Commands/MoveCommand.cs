using System;
using System.Collections.Generic;
using System.Diagnostics;

using Tektosyne;
using Tektosyne.Geometry;

using Hexkit.Global;
using Hexkit.Scenario;

namespace Hexkit.World.Commands {

    /// <summary>
    /// Represents a "Move" command.</summary>
    /// <remarks>
    /// <b>MoveCommand</b> is serialized to the XML element "Move" defined in <see
    /// cref="FilePaths.SessionSchema"/>.</remarks>

    public sealed class MoveCommand: EntitiesTargetCommand {
        #region MoveCommand()

        /// <overloads>
        /// Initializes a new instance of the <see cref="MoveCommand"/> class.</overloads>
        /// <summary>
        /// Initializes a new instance of the <see cref="MoveCommand"/> class with default
        /// properties.</summary>

        internal MoveCommand(): base() { }

        #endregion
        #region MoveCommand(Faction, IList<Entity>, PointI)

        /// <summary>
        /// Initializes a new instance of the <see cref="MoveCommand"/> class with the specified
        /// <see cref="WorldState"/>, entities, and target location.</summary>
        /// <param name="faction">
        /// The initial value for the <see cref="Command.Faction"/> property.</param>
        /// <param name="entities">
        /// An <see cref="IList{T}"/> whose <see cref="Unit"/> elements provide the initial value
        /// for the <see cref="Command.Entities"/> property.</param>
        /// <param name="target">
        /// The initial value for the <b>Location</b> component of the <see cref="Command.Target"/>
        /// property.</param>
        /// <exception cref="ArgumentException">
        /// <paramref name="entities"/> contains a null reference.</exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="faction"/> is a null reference.</exception>
        /// <exception cref="ArgumentNullOrEmptyException">
        /// <paramref name="entities"/> is a null reference or an empty collection.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="target"/> specifies one or two negative coordinates.</exception>

        public MoveCommand(Faction faction, IList<Entity> entities, PointI target):
            base(faction, entities, target) { }

        #endregion
        #region GenerateProgram

        /// <summary>
        /// Generates and executes the HCL <see cref="Command.Program"/> for the <see
        /// cref="MoveCommand"/>.</summary>
        /// <exception cref="InvalidCommandException">
        /// The <see cref="MoveCommand"/> contains data that is invalid with respect to the current
        /// <see cref="Command.Context"/>.</exception>
        /// <exception cref="PropertyValueException">
        /// <see cref="Command.Context"/> is a null reference.</exception>
        /// <remarks>
        /// <b>GenerateProgram</b> invokes <see cref="Unit.Move"/> on the first <see
        /// cref="Command.Entities"/> element.</remarks>

        protected override void GenerateProgram() {
            Unit firstUnit = (Unit) Entities[0].Value;
            firstUnit.Move(this);
        }

        #endregion
        #region Validate

        /// <summary>
        /// Validates the current data of the <see cref="MoveCommand"/> against the specified <see
        /// cref="WorldState"/>.</summary>
        /// <param name="worldState">
        /// The <see cref="WorldState"/> to validate the <see cref="MoveCommand"/> against.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="worldState"/> is a null reference.</exception>
        /// <exception cref="InvalidCommandException">
        /// One of the conditions listed under <see cref="EntitiesTargetCommand.Validate"/>.
        /// </exception>
        /// <remarks>
        /// Please refer to <see cref="EntitiesTargetCommand.Validate"/> for details. This override
        /// performs additional validation specific to the <see cref="MoveCommand"/>.</remarks>

        public override void Validate(WorldState worldState) {
            base.Validate(worldState);

            // check that all entities are placed units
            IList<Entity> entities = EntityReference.GetEntities(Entities);
            for (int i = 0; i < entities.Count; i++) {
                Entity entity = entities[i];

                if (entity.Category != EntityCategory.Unit)
                    InvalidCommandException.ThrowNewWithFormat(
                        Global.Strings.CommandEntityCategory, Name, entity.Id, entity.Category);

                if (!entity.IsPlaced)
                    InvalidCommandException.ThrowNewWithFormat(
                        Global.Strings.CommandEntityUnplaced, Name, entity.Id);
            }

            // check that target is valid for unit stack
            IList<PointI> targets = Finder.FindMoveTargets(worldState, entities);
            if (!targets.Contains(Target.Location))
                InvalidCommandException.ThrowNewWithFormat(
                    Global.Strings.CommandTargetEntities, Name, Target);
        }

        #endregion
    }
}
