using System;
using System.Collections.Generic;

using Tektosyne;
using Hexkit.Global;

namespace Hexkit.World.Commands {

    /// <summary>
    /// Represents a "Destroy" command.</summary>
    /// <remarks>
    /// <b>DestroyCommand</b> is serialized to the XML element "Destroy" defined in <see
    /// cref="FilePaths.SessionSchema"/>.</remarks>

    public sealed class DestroyCommand: EntitiesCommand {
        #region DestroyCommand()

        /// <overloads>
        /// Initializes a new instance of the <see cref="DestroyCommand"/> class.</overloads>
        /// <summary>
        /// Initializes a new instance of the <see cref="DestroyCommand"/> class with default
        /// properties.</summary>

        internal DestroyCommand(): base() { }

        #endregion
        #region DestroyCommand(Faction, IList<Entity>)

        /// <summary>
        /// Initializes a new instance of the <see cref="DestroyCommand"/> class with the specified
        /// <see cref="WorldState"/> and entities.</summary>
        /// <param name="faction">
        /// The initial value for the <see cref="Command.Faction"/> property.</param>
        /// <param name="entities">
        /// An <see cref="IList{T}"/> whose <see cref="Entity"/> elements provide the initial value
        /// for the <see cref="Command.Entities"/> property.</param>
        /// <exception cref="ArgumentException">
        /// <paramref name="entities"/> contains a null reference.</exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="faction"/> is a null reference.</exception>
        /// <exception cref="ArgumentNullOrEmptyException">
        /// <paramref name="entities"/> is a null reference or an empty collection.</exception>

        public DestroyCommand(Faction faction, IList<Entity> entities): base(faction, entities) { }

        #endregion
        #region GenerateProgram

        /// <summary>
        /// Generates and executes the HCL <see cref="Command.Program"/> for the <see
        /// cref="DestroyCommand"/>.</summary>
        /// <exception cref="InvalidCommandException">
        /// The <see cref="DestroyCommand"/> contains data that is invalid with respect to the
        /// current <see cref="Command.Context"/>.</exception>
        /// <exception cref="PropertyValueException">
        /// <see cref="Command.Context"/> is a null reference.</exception>
        /// <remarks>
        /// <b>GenerateProgram</b> invokes <see cref="Faction.Destroy"/> on the associated <see
        /// cref="Command.Faction"/>.</remarks>

        protected override void GenerateProgram() {
            Faction.Value.Destroy(this);
        }

        #endregion
        #region Validate

        /// <summary>
        /// Validates the current data of the <see cref="DestroyCommand"/> against the specified
        /// <see cref="WorldState"/>.</summary>
        /// <param name="worldState">
        /// The <see cref="WorldState"/> to validate the <see cref="DestroyCommand"/> against.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="worldState"/> is a null reference.</exception>
        /// <exception cref="InvalidCommandException">
        /// One of the conditions listed under <see cref="EntitiesCommand.Validate"/>.</exception>
        /// <remarks>
        /// Please refer to <see cref="EntitiesCommand.Validate"/> for details. This override
        /// performs additional validation specific to the <see cref="DestroyCommand"/>.</remarks>

        public override void Validate(WorldState worldState) {
            base.Validate(worldState);

            // check that all entities can be destroyed
            foreach (EntityReference entity in Entities)
                if (!entity.Value.CanDestroy)
                    InvalidCommandException.ThrowNewWithFormat(
                        Global.Strings.CommandEntityDestroy, Name, entity.Id);
        }

        #endregion
    }
}
