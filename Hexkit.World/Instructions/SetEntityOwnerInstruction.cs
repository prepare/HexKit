using System;

using Tektosyne;
using Hexkit.Global;

namespace Hexkit.World.Instructions {

    /// <summary>
    /// Sets the <see cref="Entity.Owner"/> of an <see cref="Entity"/>.</summary>
    /// <remarks>
    /// <b>SetEntityOwnerInstruction</b> is serialized to the XML element "SetEntityOwner" defined
    /// in <see cref="FilePaths.SessionSchema"/>.</remarks>

    public sealed class SetEntityOwnerInstruction: StringInstruction {
        #region SetEntityOwnerInstruction()

        /// <overloads>
        /// Initializes a new instance of the <see cref="SetEntityOwnerInstruction"/> class.
        /// </overloads>
        /// <summary>
        /// Initializes a new instance of the <see cref="SetEntityOwnerInstruction"/> class with
        /// default properties.</summary>

        internal SetEntityOwnerInstruction(): base() { }

        #endregion
        #region SetEntityOwnerInstruction(String, String)

        /// <summary>
        /// Initializes a new instance of the <see cref="SetEntityOwnerInstruction"/> class with the
        /// specified entity and faction identifiers.</summary>
        /// <param name="entityId">
        /// The initial value for the <see cref="Instruction.Id"/> property, indicating the <see
        /// cref="Entity.Id"/> string of the <see cref="Entity"/> to manipulate.</param>
        /// <param name="factionId">
        /// The initial value for the <see cref="StringInstruction.Text"/> property, indicating the
        /// <see cref="Faction.Id"/> string of the new value for the <see cref="Entity.Owner"/>
        /// property of the specified <see cref="Entity"/>.</param>
        /// <remarks>
        /// Specify a null reference or an empty string for <paramref name="factionId"/> to set the
        /// <see cref="Entity.Owner"/> property of the indicated <see cref="Entity"/> to a null
        /// reference.</remarks>

        internal SetEntityOwnerInstruction(string entityId, string factionId): base(entityId, factionId) { }

        #endregion
        #region Execute

        /// <summary>
        /// Executes the <see cref="Instruction"/> on the specified <see cref="WorldState"/> and
        /// indicates whether the <see cref="WorldState"/> has changed.</summary>
        /// <param name="worldState">
        /// The <see cref="WorldState"/> on which to execute the <see
        /// cref="SetEntityOwnerInstruction"/>.</param>
        /// <returns>
        /// <c>true</c> if execution of the <see cref="SetEntityOwnerInstruction"/> has changed the
        /// data of the specified <paramref name="worldState"/>; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="worldState"/> is a null reference.</exception>
        /// <exception cref="InvalidCommandException">
        /// The <see cref="SetEntityOwnerInstruction"/> contains data that is invalid with respect
        /// to the specified <paramref name="worldState"/>.</exception>
        /// <remarks><para>
        /// <b>Execute</b> performs the following actions:
        /// </para><list type="number"><item>
        /// Set the <see cref="InstructionResults.Entity"/> property of the <see
        /// cref="Instruction.Results"/> cache to the <see cref="Entity"/> identified by the <see
        /// cref="Instruction.Id"/> property.
        /// </item><item>
        /// Set the <see cref="InstructionResults.Faction"/> property of the <b>Results</b> cache to
        /// the <em>original</em> <see cref="Entity.Owner"/> of the <b>Entity</b>.
        /// </item><item>
        /// Invoke <see cref="Entity.UpdateModifierMaps"/> on the <see cref="Entity"/> to remove its
        /// effects from all modifier maps in the specified <paramref name="worldState"/>.
        /// </item><item>
        /// Set the <see cref="Entity.Owner"/> property of the <b>Entity</b> to the <see
        /// cref="Faction"/> indicated by the <see cref="StringInstruction.Text"/> property, or to a
        /// null reference if <b>Text</b> is a null reference or an empty string.
        /// </item><item>
        /// Again invoke <see cref="Entity.UpdateModifierMaps"/> on the <see cref="Entity"/> to add
        /// its effects to all modifier maps in the specified <paramref name="worldState"/>.
        /// </item></list></remarks>

        internal override bool Execute(WorldState worldState) {
            if (worldState == null)
                ThrowHelper.ThrowArgumentNullException("worldState");

            // GetEntity throws exception for invalid Id
            Entity entity = GetEntity(worldState, Id);
            Faction faction = null;

            // GetFaction throws exception for invalid Text
            if (!String.IsNullOrEmpty(Text))
                faction = GetFaction(worldState, Text);

            // return entity and original owner
            if (Results != null) {
                Results.Entity = entity;
                Results.Faction = entity.Owner;
            }

            if (entity.Owner == faction)
                return false;

            entity.UpdateModifierMaps(worldState, false);
            entity.SetOwner(faction, true);
            entity.UpdateModifierMaps(worldState, true);

            return true;
        }

        #endregion
    }
}
