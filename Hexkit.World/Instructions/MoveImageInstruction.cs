using System;

using Tektosyne.Geometry;
using Hexkit.Global;
using Hexkit.Scenario;

namespace Hexkit.World.Instructions {

    /// <summary>
    /// Moves an <see cref="EntityClass"/> image across the map view.</summary>
    /// <remarks><para>
    /// <b>MoveImageInstruction</b> represents a command event that smoothly moves an <see
    /// cref="EntityClass"/> image across a sequence of map locations.
    /// </para><para>
    /// Please refer to method <b>Hexkit.Graphics.MapView.MoveImage</b> for the exact meaning of all
    /// <b>MoveImageInstruction</b> properties.
    /// </para><para>
    /// <b>MoveImageInstruction</b> is serialized to the XML element "MoveImage" defined in <see
    /// cref="FilePaths.SessionSchema"/>.</para></remarks>

    public sealed class MoveImageInstruction: ImageInstruction {
        #region MoveImageInstruction()

        /// <overloads>
        /// Initializes a new instance of the <see cref="MoveImageInstruction"/> class. </overloads>
        /// <summary>
        /// Initializes a new instance of the <see cref="MoveImageInstruction"/> class with default
        /// properties.</summary>

        internal MoveImageInstruction(): base() { }

        #endregion
        #region MoveImageInstruction(String, PointI[], Int32)

        /// <summary>
        /// Initializes a new instance of the <see cref="MoveImageInstruction"/> class with the
        /// specified <see cref="EntityClass"/> identifier, map locations, and display delay.
        /// </summary>
        /// <param name="classId">
        /// The initial value for the <see cref="Instruction.Id"/> property, indicating the <see
        /// cref="EntityClass"/> whose image to show.</param>
        /// <param name="sites">
        /// An <see cref="Array"/> containing the initial elements for the <see
        /// cref="ImageInstruction.Sites"/> collection.</param>
        /// <param name="delay">
        /// The initial value for the <see cref="ImageInstruction.Delay"/> property.</param>

        internal MoveImageInstruction(string classId, PointI[] sites, int delay):
            base(classId, sites, delay) { }

        #endregion
    }
}
