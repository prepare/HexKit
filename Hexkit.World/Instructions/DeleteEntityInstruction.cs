using System;

using Tektosyne;
using Hexkit.Global;

namespace Hexkit.World.Instructions {

    /// <summary>
    /// Deletes an <see cref="Entity"/>.</summary>
    /// <remarks>
    /// <b>DeleteEntityInstruction</b> is serialized to the XML element "DeleteEntity" defined in
    /// <see cref="FilePaths.SessionSchema"/>.</remarks>

    public sealed class DeleteEntityInstruction: Instruction {
        #region DeleteEntityInstruction()

        /// <overloads>
        /// Initializes a new instance of the <see cref="DeleteEntityInstruction"/> class.
        /// </overloads>
        /// <summary>
        /// Initializes a new instance of the <see cref="DeleteEntityInstruction"/> class with
        /// default properties.</summary>

        internal DeleteEntityInstruction(): base() { }

        #endregion
        #region DeleteEntityInstruction(String)

        /// <summary>
        /// Initializes a new instance of the <see cref="DeleteEntityInstruction"/> class with the
        /// specified entity identifier.</summary>
        /// <param name="entityId">
        /// The initial value for the <see cref="Instruction.Id"/> property, indicating the <see
        /// cref="Entity.Id"/> string of the <see cref="Entity"/> to delete.</param>

        internal DeleteEntityInstruction(string entityId): base(entityId) { }

        #endregion
        #region Execute

        /// <summary>
        /// Executes the <see cref="Instruction"/> on the specified <see cref="WorldState"/> and
        /// indicates whether the <see cref="WorldState"/> has changed.</summary>
        /// <param name="worldState">
        /// The <see cref="WorldState"/> on which to execute the <see
        /// cref="DeleteEntityInstruction"/>.</param>
        /// <returns>
        /// <c>true</c> if execution of the <see cref="DeleteEntityInstruction"/> has changed the
        /// data of the specified <paramref name="worldState"/>; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="worldState"/> is a null reference.</exception>
        /// <exception cref="InvalidCommandException">
        /// The <see cref="DeleteEntityInstruction"/> contains data that is invalid with respect to
        /// the specified <paramref name="worldState"/>.</exception>
        /// <remarks><para>
        /// <b>Execute</b> performs the following actions:
        /// </para><list type="number"><item>
        /// Set the <see cref="InstructionResults.Entity"/> property of the <see
        /// cref="Instruction.Results"/> cache to the <see cref="Entity"/> identified by the <see
        /// cref="Instruction.Id"/> property.
        /// </item><item>
        /// Set the <see cref="InstructionResults.Faction"/> and <see
        /// cref="InstructionResults.Site"/> properties of the <b>Results</b> cache to the <see
        /// cref="Entity.Owner"/> and <see cref="Entity.Site"/>, respectively, of the <b>Entity</b>.
        /// </item><item>
        /// Invoke <see cref="Entity.UpdateModifierMaps"/> on the <see cref="Entity"/> to remove its
        /// effects from all modifier maps in the specified <paramref name="worldState"/>.
        /// </item><item>
        /// Set the <see cref="Entity.Site"/> and <see cref="Entity.Owner"/> properties of the <see
        /// cref="Entity"/> to null references.
        /// </item><item>
        /// Delete the <see cref="Entity"/> from the <see cref="WorldState.WritableEntities"/>
        /// collection of the specified <paramref name="worldState"/>.
        /// </item></list><para>
        /// <b>Execute</b> always returns <c>true</c>.</para></remarks>

        internal override bool Execute(WorldState worldState) {
            if (worldState == null)
                ThrowHelper.ThrowArgumentNullException("worldState");

            // GetEntity throws exception for invalid Id
            Entity entity = GetEntity(worldState, Id);

            // return original entity, owner, and site
            if (Results != null) {
                Results.Entity = entity;
                Results.Faction = entity.Owner;
                Results.Site = entity.Site;
            }

            // remove effects from modifier maps
            entity.UpdateModifierMaps(worldState, false);

            // clear properties without validation
            entity.SetOwner(null, false);
            entity.SetSite(null, false);

            worldState.WritableEntities.Remove(Id);

            // end history of deleted entity
            worldState.History.Entities[Id].Delete(worldState);

            return true;
        }

        #endregion
    }
}
