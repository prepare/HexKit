using System;

using Tektosyne;
using Hexkit.Global;

namespace Hexkit.World.Instructions {

    /// <summary>
    /// Sets the <see cref="Entity.Name"/> of an <see cref="Entity"/>.</summary>
    /// <remarks>
    /// <b>SetEntityNameInstruction</b> is serialized to the XML element "SetEntityName" defined in
    /// <see cref="FilePaths.SessionSchema"/>.</remarks>

    public sealed class SetEntityNameInstruction: StringInstruction {
        #region SetEntityNameInstruction()

        /// <overloads>
        /// Initializes a new instance of the <see cref="SetEntityNameInstruction"/> class.
        /// </overloads>
        /// <summary>
        /// Initializes a new instance of the <see cref="SetEntityNameInstruction"/> class with
        /// default properties.</summary>

        internal SetEntityNameInstruction(): base() { }

        #endregion
        #region SetEntityNameInstruction(String, String)

        /// <summary>
        /// Initializes a new instance of the <see cref="SetEntityNameInstruction"/> class with the
        /// specified entity identifier and display name.</summary>
        /// <param name="entityId">
        /// The initial value for the <see cref="Instruction.Id"/> property, indicating the <see
        /// cref="Entity.Id"/> string of the <see cref="Entity"/> to manipulate.</param>
        /// <param name="name">
        /// The initial value for the <see cref="StringInstruction.Text"/> property, indicating the
        /// new value for the <see cref="Entity.Name"/> property of the specified <see
        /// cref="Entity"/>.</param>
        /// <remarks>
        /// Specify a null reference or an empty string for <paramref name="name"/> to reset the
        /// <see cref="Entity.Name"/> property of the indicated <see cref="Entity"/> to its default
        /// value, i.e. the value of the <see cref="Entity.DefaultName"/> property.</remarks>

        internal SetEntityNameInstruction(string entityId, string name): base(entityId, name) { }

        #endregion
        #region Execute

        /// <summary>
        /// Executes the <see cref="Instruction"/> on the specified <see cref="WorldState"/> and
        /// indicates whether the <see cref="WorldState"/> has changed.</summary>
        /// <param name="worldState">
        /// The <see cref="WorldState"/> on which to execute the <see
        /// cref="SetEntityNameInstruction"/>.</param>
        /// <returns>
        /// <c>true</c> if execution of the <see cref="SetEntityNameInstruction"/> has changed the
        /// data of the specified <paramref name="worldState"/>; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="worldState"/> is a null reference.</exception>
        /// <exception cref="InvalidCommandException">
        /// The <see cref="SetEntityNameInstruction"/> contains data that is invalid with respect to
        /// the specified <paramref name="worldState"/>.</exception>
        /// <remarks>
        /// <b>Execute</b> sets the <see cref="Entity.Name"/> property of the <see cref="Entity"/>
        /// identified by the <see cref="Instruction.Id"/> property to the value of the <see
        /// cref="StringInstruction.Text"/> property.</remarks>

        internal override bool Execute(WorldState worldState) {
            if (worldState == null)
                ThrowHelper.ThrowArgumentNullException("worldState");

            // GetEntity throws exception for invalid Id
            Entity entity = GetEntity(worldState, Id);

            if (entity.Name == Text) return false;
            entity.Name = Text;

            // record name change of entity
            worldState.History.Entities[Id].SetName(worldState, Text);

            return true;
        }

        #endregion
    }
}
