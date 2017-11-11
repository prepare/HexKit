using System;
using System.Diagnostics;

using Hexkit.Global;

namespace Hexkit.World.Instructions {

    /// <summary>
    /// Selects an <see cref="Entity"/> in the map view and data view.</summary>
    /// <remarks><para>
    /// <b>SelectEntityInstruction</b> represents a command event that selects a specific <see
    /// cref="Entity"/> in the default map view and in the data view.
    /// </para><para>
    /// <b>SelectEntityInstruction</b> is serialized to the XML element "SelectEntity" defined in
    /// <see cref="FilePaths.SessionSchema"/>.</para></remarks>

    public sealed class SelectEntityInstruction: Instruction {
        #region SelectEntityInstruction()

        /// <overloads>
        /// Initializes a new instance of the <see cref="SelectEntityInstruction"/> class.
        /// </overloads>
        /// <summary>
        /// Initializes a new instance of the <see cref="SelectEntityInstruction"/> class with
        /// default properties.</summary>

        internal SelectEntityInstruction(): base() { }

        #endregion
        #region SelectEntityInstruction(String)

        /// <summary>
        /// Initializes a new instance of the <see cref="SelectEntityInstruction"/> class with the
        /// specified entity identifier.</summary>
        /// <param name="entityId">
        /// The initial value for the <see cref="Instruction.Id"/> property, indicating the <see
        /// cref="Entity.Id"/> string of the <see cref="Entity"/> to select.</param>

        internal SelectEntityInstruction(string entityId): base(entityId) { }

        #endregion
        #region Category

        /// <summary>
        /// Gets the category of the <see cref="Instruction"/>.</summary>
        /// <value>
        /// The category <see cref="InstructionCategory.Event"/>, indicating that the <see
        /// cref="SelectEntityInstruction"/> class represents a map view event.</value>

        public override InstructionCategory Category {
            [DebuggerStepThrough]
            get { return InstructionCategory.Event; }
        }

        #endregion
    }
}
