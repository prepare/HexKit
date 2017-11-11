using System;

using Tektosyne;
using Hexkit.Global;

namespace Hexkit.World.Instructions {

    /// <summary>
    /// Sets the <see cref="Entity.FrameOffset"/> index of an <see cref="Entity"/>.</summary>
    /// <remarks>
    /// <b>SetEntityFrameOffsetInstruction</b> is serialized to the XML element "SetEntityFrameOffset" defined
    /// in <see cref="FilePaths.SessionSchema"/>.</remarks>

    public sealed class SetEntityFrameOffsetInstruction: IntegerInstruction {
        #region SetEntityFrameOffsetInstruction()

        /// <overloads>
        /// Initializes a new instance of the <see cref="SetEntityFrameOffsetInstruction"/> class.
        /// </overloads>
        /// <summary>
        /// Initializes a new instance of the <see cref="SetEntityFrameOffsetInstruction"/> class
        /// with default properties.</summary>

        internal SetEntityFrameOffsetInstruction(): base() { }

        #endregion
        #region SetEntityFrameOffsetInstruction(String, Int32)

        /// <summary>
        /// Initializes a new instance of the <see cref="SetEntityFrameOffsetInstruction"/> class
        /// with the specified entity identifier and frame index offset.</summary>
        /// <param name="entityId">
        /// The initial value for the <see cref="Instruction.Id"/> property, indicating the <see
        /// cref="Entity.Id"/> string of the <see cref="Entity"/> to manipulate.</param>
        /// <param name="frameOffset">
        /// The initial value for the <see cref="IntegerInstruction.Value"/> property, indicating
        /// the new value for the <see cref="Entity.FrameOffset"/> property of the specified <see
        /// cref="Entity"/>.</param>

        internal SetEntityFrameOffsetInstruction(string entityId, int frameOffset):
            base(entityId, frameOffset) { }

        #endregion
        #region Execute

        /// <summary>
        /// Executes the <see cref="Instruction"/> on the specified <see cref="WorldState"/> and
        /// indicates whether the <see cref="WorldState"/> has changed.</summary>
        /// <param name="worldState">
        /// The <see cref="WorldState"/> on which to execute the <see
        /// cref="SetEntityFrameOffsetInstruction"/>.</param>
        /// <returns>
        /// <c>true</c> if execution of the <see cref="SetEntityFrameOffsetInstruction"/> has
        /// changed the data of the specified <paramref name="worldState"/>; otherwise,
        /// <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="worldState"/> is a null reference.</exception>
        /// <exception cref="InvalidCommandException">
        /// The <see cref="SetEntityFrameOffsetInstruction"/> contains data that is invalid with
        /// respect to the specified <paramref name="worldState"/>.</exception>
        /// <remarks>
        /// <b>Execute</b> sets the <see cref="Entity.FrameOffset"/> property of the <see
        /// cref="Entity"/> identified by the <see cref="Instruction.Id"/> property to the value of
        /// the <see cref="IntegerInstruction.Value"/> property.</remarks>

        internal override bool Execute(WorldState worldState) {
            if (worldState == null)
                ThrowHelper.ThrowArgumentNullException("worldState");

            // GetEntity throws exception for invalid Id
            Entity entity = GetEntity(worldState, Id);

            if (Value < 0)
                InvalidCommandException.ThrowNewWithFormat(
                    Global.Strings.InstructionValueNegative, Value);

            if (entity.FrameOffset == Value)
                return false;

            entity.FrameOffset = Value;
            return true;
        }

        #endregion
    }
}
