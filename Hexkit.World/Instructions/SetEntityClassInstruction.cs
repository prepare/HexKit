using System;

using Tektosyne;
using Hexkit.Global;
using Hexkit.Scenario;

namespace Hexkit.World.Instructions {

    /// <summary>
    /// Sets the <see cref="Entity.EntityClass"/> of an <see cref="Entity"/>.</summary>
    /// <remarks>
    /// <b>SetEntityClassInstruction</b> is serialized to the XML element "SetEntityClass" defined
    /// in <see cref="FilePaths.SessionSchema"/>.</remarks>

    public sealed class SetEntityClassInstruction: StringInstruction {
        #region SetEntityClassInstruction()

        /// <overloads>
        /// Initializes a new instance of the <see cref="SetEntityClassInstruction"/> class.
        /// </overloads>
        /// <summary>
        /// Initializes a new instance of the <see cref="SetEntityClassInstruction"/> class with
        /// default properties.</summary>

        internal SetEntityClassInstruction(): base() { }

        #endregion
        #region SetEntityClassInstruction(String, String)

        /// <summary>
        /// Initializes a new instance of the <see cref="SetEntityClassInstruction"/> class with the
        /// specified entity and class identifiers.</summary>
        /// <param name="entityId">
        /// The initial value for the <see cref="Instruction.Id"/> property, indicating the <see
        /// cref="Entity.Id"/> string of the <see cref="Entity"/> to manipulate.</param>
        /// <param name="classId">
        /// The initial value for the <see cref="StringInstruction.Text"/> property, indicating the
        /// <see cref="EntityClass.Id"/> string of the new value for the <see
        /// cref="Entity.EntityClass"/> property of the specified <see cref="Entity"/>.</param>

        internal SetEntityClassInstruction(string entityId, string classId):
            base(entityId, classId) { }

        #endregion
        #region Execute

        /// <summary>
        /// Executes the <see cref="Instruction"/> on the specified <see cref="WorldState"/> and
        /// indicates whether the <see cref="WorldState"/> has changed.</summary>
        /// <param name="worldState">
        /// The <see cref="WorldState"/> on which to execute the <see
        /// cref="SetEntityClassInstruction"/>.</param>
        /// <returns>
        /// <c>true</c> if execution of the <see cref="SetEntityClassInstruction"/> has changed the
        /// data of the specified <paramref name="worldState"/>; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="worldState"/> is a null reference.</exception>
        /// <exception cref="InvalidCommandException">
        /// The <see cref="SetEntityClassInstruction"/> contains data that is invalid with respect
        /// to the specified <paramref name="worldState"/>.</exception>
        /// <remarks><para>
        /// <b>Execute</b> performs the following actions:
        /// </para><list type="number"><item>
        /// Set the <see cref="InstructionResults.Entity"/> property of the <see
        /// cref="Instruction.Results"/> cache to the <see cref="Entity"/> identified by the <see
        /// cref="Instruction.Id"/> property.
        /// </item><item>
        /// Set the <see cref="InstructionResults.EntityClass"/> property of the <b>Results</b>
        /// cache to the <em>original</em> <see cref="Entity.EntityClass"/> of the <b>Entity</b>.
        /// </item><item>
        /// Invoke <see cref="Entity.UpdateModifierMaps"/> on the <see cref="Entity"/> to remove its
        /// effects from all modifier maps in the specified <paramref name="worldState"/>.
        /// </item><item>
        /// Set the <see cref="Entity.EntityClass"/> property of the <b>Entity</b> to the <see
        /// cref="EntityClass"/> indicated by the <see cref="StringInstruction.Text"/> property.
        /// </item><item>
        /// Again invoke <see cref="Entity.UpdateModifierMaps"/> on the <see cref="Entity"/> to add
        /// its effects to all modifier maps in the specified <paramref name="worldState"/>.
        /// </item></list></remarks>

        internal override bool Execute(WorldState worldState) {
            if (worldState == null)
                ThrowHelper.ThrowArgumentNullException("worldState");

            // GetEntity throws exception for invalid Id
            Entity entity = GetEntity(worldState, Id);

            // GetEntityClass throws exception for invalid Text
            EntityClass entityClass = GetEntityClass(Text);

            // return entity and original entity class
            if (Results != null) {
                Results.Entity = entity;
                Results.EntityClass = entity.EntityClass;
            }

            if (entity.EntityClass == entityClass)
                return false;

            entity.UpdateModifierMaps(worldState, false);

            // SetEntityClass throws exception for invalid argument
            entity.SetEntityClass(entityClass);

            // record class change of entity
            worldState.History.Entities[Id].SetClass(worldState, Text);

            entity.UpdateModifierMaps(worldState, true);
            return true;
        }

        #endregion
    }
}
