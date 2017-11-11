using System;

using Tektosyne;
using Hexkit.Global;

namespace Hexkit.World.Instructions {

    /// <summary>
    /// Moves an <see cref="Entity"/> to the top of its <see cref="Site"/> stack.</summary>
    /// <remarks><para>
    /// <b>MoveEntityToTopInstruction</b> moves a specific <see cref="Entity"/> to the top of its
    /// <see cref="Site"/> stack, thus making it visible on the map view (assuming it is not
    /// obscured by other stacks).
    /// </para><para>
    /// <b>MoveEntityToTopInstruction</b> is serialized to the XML element "MoveEntityToTop" defined
    /// in <see cref="FilePaths.SessionSchema"/>.</para></remarks>

    public sealed class MoveEntityToTopInstruction: Instruction {
        #region MoveEntityToTopInstruction()

        /// <overloads>
        /// Initializes a new instance of the <see cref="MoveEntityToTopInstruction"/> class.
        /// </overloads>
        /// <summary>
        /// Initializes a new instance of the <see cref="MoveEntityToTopInstruction"/> class with
        /// default properties.</summary>

        internal MoveEntityToTopInstruction(): base() { }

        #endregion
        #region MoveEntityToTopInstruction(String)

        /// <summary>
        /// Initializes a new instance of the <see cref="MoveEntityToTopInstruction"/> class with
        /// the specified entity identifier.</summary>
        /// <param name="entityId">
        /// The initial value for the <see cref="Instruction.Id"/> property, indicating the <see
        /// cref="Entity.Id"/> string of the <see cref="Entity"/> to manipulate.</param>

        internal MoveEntityToTopInstruction(string entityId): base(entityId) { }

        #endregion
        #region Execute

        /// <summary>
        /// Executes the <see cref="Instruction"/> on the specified <see cref="WorldState"/> and
        /// indicates whether the <see cref="WorldState"/> has changed.</summary>
        /// <param name="worldState">
        /// The <see cref="WorldState"/> on which to execute the <see
        /// cref="MoveEntityToTopInstruction"/>.</param>
        /// <returns>
        /// <c>true</c> if execution of the <see cref="MoveEntityToTopInstruction"/> has changed the
        /// data of the specified <paramref name="worldState"/>; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="worldState"/> is a null reference.</exception>
        /// <exception cref="InvalidCommandException">
        /// The <see cref="MoveEntityToTopInstruction"/> contains data that is invalid with respect
        /// to the specified <paramref name="worldState"/>.</exception>
        /// <remarks>
        /// <b>Execute</b> invokes <see cref="Entity.MoveToTop"/> on the <see cref="Entity"/> 
        /// identified by the <see cref="Instruction.Id"/> property.</remarks>

        internal override bool Execute(WorldState worldState) {
            if (worldState == null)
                ThrowHelper.ThrowArgumentNullException("worldState");

            // GetEntity throws exception for invalid Id
            Entity entity = GetEntity(worldState, Id);

            return entity.MoveToTop();
        }

        #endregion
    }
}
