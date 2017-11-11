using System;

using Tektosyne;
using Hexkit.Global;

namespace Hexkit.World.Instructions {

    /// <summary>
    /// Sets the <see cref="Entity.Name"/> of an <see cref="Entity"/> to a value that is unique
    /// within the possessions of a specific <see cref="Faction"/>.</summary>
    /// <remarks>
    /// <b>SetEntityUniqueNameInstruction</b> is serialized to the XML element "SetEntityUniqueName"
    /// defined in <see cref="FilePaths.SessionSchema"/>.</remarks>

    public sealed class SetEntityUniqueNameInstruction: StringInstruction {
        #region SetEntityUniqueNameInstruction()

        /// <overloads>
        /// Initializes a new instance of the <see cref="SetEntityUniqueNameInstruction"/> class.
        /// </overloads>
        /// <summary>
        /// Initializes a new instance of the <see cref="SetEntityUniqueNameInstruction"/> class
        /// with default properties.</summary>

        internal SetEntityUniqueNameInstruction(): base() { }

        #endregion
        #region SetEntityUniqueNameInstruction(String, String)

        /// <summary>
        /// Initializes a new instance of the <see cref="SetEntityUniqueNameInstruction"/> class
        /// with the specified entity and faction identifiers.</summary>
        /// <param name="entityId">
        /// The initial value for the <see cref="Instruction.Id"/> property, indicating the <see
        /// cref="Entity.Id"/> string of the <see cref="Entity"/> to manipulate.</param>
        /// <param name="factionId">
        /// The initial value for the <see cref="StringInstruction.Text"/> property, indicating the
        /// <see cref="Faction.Id"/> string of the <see cref="Faction"/> within whose possessions
        /// the new <see cref="Entity.Name"/> should be unique.</param>

        internal SetEntityUniqueNameInstruction(string entityId, string factionId):
            base(entityId, factionId) { }

        #endregion
        #region Execute

        /// <summary>
        /// Executes the <see cref="Instruction"/> on the specified <see cref="WorldState"/> and
        /// indicates whether the <see cref="WorldState"/> has changed.</summary>
        /// <param name="worldState">
        /// The <see cref="WorldState"/> on which to execute the <see
        /// cref="SetEntityUniqueNameInstruction"/>.</param>
        /// <returns>
        /// <c>true</c> if execution of the <see cref="SetEntityUniqueNameInstruction"/> has changed
        /// the data of the specified <paramref name="worldState"/>; otherwise, <c>false</c>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="worldState"/> is a null reference.</exception>
        /// <exception cref="InvalidCommandException">
        /// The <see cref="SetEntityUniqueNameInstruction"/> contains data that is invalid with
        /// respect to the specified <paramref name="worldState"/>.</exception>
        /// <remarks><para>
        /// <b>Execute</b> sets the <see cref="Entity.Name"/> property of the <see cref="Entity"/>
        /// identified by the <see cref="Instruction.Id"/> property to a name that is unique within
        /// the possessions of the <see cref="Faction"/> indicated by the <see
        /// cref="StringInstruction.Text"/> property.
        /// </para><para>
        /// <b>Execute</b> always generates a new <b>Name</b> with each invocation, and therefore
        /// always returns <c>true</c>.</para></remarks>

        internal override bool Execute(WorldState worldState) {
            if (worldState == null)
                ThrowHelper.ThrowArgumentNullException("worldState");

            // GetEntity throws exception for invalid Id
            Entity entity = GetEntity(worldState, Id);

            // GetFaction throws exception for invalid Text
            Faction faction = GetFaction(worldState, Text);

            entity.SetUniqueName(faction);
            return true;
        }

        #endregion
    }
}
