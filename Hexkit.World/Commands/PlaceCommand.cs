using System;
using System.Collections.Generic;

using Tektosyne;
using Tektosyne.Geometry;

using Hexkit.Global;
using Hexkit.Scenario;

namespace Hexkit.World.Commands {

    /// <summary>
    /// Represents a "Place" command.</summary>
    /// <remarks>
    /// <b>PlaceCommand</b> is serialized to the XML element "Place" defined in <see
    /// cref="FilePaths.SessionSchema"/>.</remarks>

    public sealed class PlaceCommand: EntitiesTargetCommand {
        #region PlaceCommand()

        /// <overloads>
        /// Initializes a new instance of the <see cref="PlaceCommand"/> class.</overloads>
        /// <summary>
        /// Initializes a new instance of the <see cref="PlaceCommand"/> class with default
        /// properties.</summary>

        internal PlaceCommand(): base() { }

        #endregion
        #region PlaceCommand(Faction, IList<Entity>, PointI)

        /// <summary>
        /// Initializes a new instance of the <see cref="PlaceCommand"/> class with the specified
        /// <see cref="WorldState"/>, entities, and target location.</summary>
        /// <param name="faction">
        /// The initial value for the <see cref="Command.Faction"/> property.</param>
        /// <param name="entities">
        /// An <see cref="IList{T}"/> whose <see cref="Entity"/> elements provide the initial value
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

        public PlaceCommand(Faction faction, IList<Entity> entities, PointI target):
            base(faction, entities, target) { }

        #endregion
        #region GenerateProgram

        /// <summary>
        /// Generates and executes the HCL <see cref="Command.Program"/> for the <see
        /// cref="PlaceCommand"/>.</summary>
        /// <exception cref="InvalidCommandException">
        /// The <see cref="PlaceCommand"/> contains data that is invalid with respect to the current
        /// <see cref="Command.Context"/>.</exception>
        /// <exception cref="PropertyValueException">
        /// <see cref="Command.Context"/> is a null reference.</exception>
        /// <remarks>
        /// <b>GenerateProgram</b> invokes <see cref="Entity.Place"/> on the first <see
        /// cref="Command.Entities"/> element.</remarks>

        protected override void GenerateProgram() {
            Entities[0].Value.Place(this);
        }

        #endregion
        #region Validate

        /// <summary>
        /// Validates the current data of the <see cref="PlaceCommand"/> against the specified <see
        /// cref="WorldState"/>.</summary>
        /// <param name="worldState">
        /// The <see cref="WorldState"/> to validate the <see cref="PlaceCommand"/> against.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="worldState"/> is a null reference.</exception>
        /// <exception cref="InvalidCommandException">
        /// One of the conditions listed under <see cref="EntitiesTargetCommand.Validate"/>.
        /// </exception>
        /// <remarks>
        /// Please refer to <see cref="EntitiesTargetCommand.Validate"/> for details. This override
        /// performs additional validation specific to the <see cref="PlaceCommand"/>.</remarks>

        public override void Validate(WorldState worldState) {
            base.Validate(worldState);

            Faction faction = Faction.Value;
            IList<Entity> entities = EntityReference.GetEntities(Entities);

            // check that all entities are unplaced
            for (int i = 0; i < entities.Count; i++) {
                Entity entity = entities[i];
                if (entity.IsPlaced)
                    InvalidCommandException.ThrowNewWithFormat(
                        Global.Strings.CommandEntityPlaced, Name, entity.Id);
            }

            // get valid target sites for all classes
            var classTargets = Finder.FindAllPlaceTargets(worldState, faction, entities);

            // check for invalid target sites
            foreach (var pair in classTargets)
                if (!pair.Value.Contains(Target.Location))
                    InvalidCommandException.ThrowNewWithFormat(
                        Global.Strings.CommandTargetClass, Name, pair.Key, Target);

            // check for combined placement on target site
            if (entities.Count > 1 && !faction.CanPlace(worldState, entities, Target.Location))
                InvalidCommandException.ThrowNewWithFormat(
                    Global.Strings.CommandTargetEntities, Name, Target);
        }

        #endregion
    }
}