using System;

using Tektosyne;
using Hexkit.Global;
using Hexkit.Scenario;

namespace Hexkit.World.Instructions {

    /// <summary>
    /// Sets the <see cref="Entity.DisplayClass"/> of an <see cref="Entity"/>.</summary>
    /// <remarks>
    /// <b>SetEntityDisplayClassInstruction</b> is serialized to the XML element
    /// "SetEntityDisplayClass" defined in <see cref="FilePaths.SessionSchema"/>.</remarks>

    public sealed class SetEntityDisplayClassInstruction: StringInstruction {
        #region SetEntityDisplayClassInstruction()

        /// <overloads>
        /// Initializes a new instance of the <see cref="SetEntityDisplayClassInstruction"/> class.
        /// </overloads>
        /// <summary>
        /// Initializes a new instance of the <see cref="SetEntityDisplayClassInstruction"/> class
        /// with default properties.</summary>

        internal SetEntityDisplayClassInstruction(): base() { }

        #endregion
        #region SetEntityDisplayClassInstruction(String, String)

        /// <summary>
        /// Initializes a new instance of the <see cref="SetEntityDisplayClassInstruction"/> class
        /// with the specified entity and class identifiers.</summary>
        /// <param name="entityId">
        /// The initial value for the <see cref="Instruction.Id"/> property, indicating the <see
        /// cref="Entity.Id"/> string of the <see cref="Entity"/> to manipulate.</param>
        /// <param name="classId">
        /// The initial value for the <see cref="StringInstruction.Text"/> property, indicating the
        /// <see cref="EntityClass.Id"/> string of the new value for the <see
        /// cref="Entity.DisplayClass"/> property of the specified <see cref="Entity"/>.</param>
        /// <remarks>
        /// Specify a null reference or an empty string for <paramref name="classId"/> to set the
        /// <see cref="Entity.DisplayClass"/> property of the indicated <see cref="Entity"/> to a
        /// null reference.</remarks>

        internal SetEntityDisplayClassInstruction(string entityId, string classId):
            base(entityId, classId) { }

        #endregion
        #region Execute

        /// <summary>
        /// Executes the <see cref="Instruction"/> on the specified <see cref="WorldState"/> and
        /// indicates whether the <see cref="WorldState"/> has changed.</summary>
        /// <param name="worldState">
        /// The <see cref="WorldState"/> on which to execute the <see
        /// cref="SetEntityDisplayClassInstruction"/>.</param>
        /// <returns>
        /// <c>true</c> if execution of the <see cref="SetEntityDisplayClassInstruction"/> has
        /// changed the data of the specified <paramref name="worldState"/>; otherwise,
        /// <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="worldState"/> is a null reference.</exception>
        /// <exception cref="InvalidCommandException">
        /// The <see cref="SetEntityDisplayClassInstruction"/> contains data that is invalid with
        /// respect to the specified <paramref name="worldState"/>.</exception>
        /// <remarks>
        /// <b>Execute</b> sets the <see cref="Entity.DisplayClass"/> property of the <see
        /// cref="Entity"/> identified by the <see cref="Instruction.Id"/> property to the <see
        /// cref="EntityClass"/> indicated by the <see cref="StringInstruction.Text"/> property, or
        /// to a null reference if <b>Text</b> is a null reference or an empty string.</remarks>

        internal override bool Execute(WorldState worldState) {
            if (worldState == null)
                ThrowHelper.ThrowArgumentNullException("worldState");

            // GetEntity throws exception for invalid Id
            Entity entity = GetEntity(worldState, Id);
            EntityClass entityClass = null;

            // GetEntityClass throws exception for invalid Text
            if (!String.IsNullOrEmpty(Text))
                entityClass = GetEntityClass(Text);

            if (entity.DisplayClass == entityClass)
                return false;

            entity.DisplayClass = entityClass;
            return true;
        }

        #endregion
    }
}
