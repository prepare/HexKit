using System;

using Tektosyne.Geometry;
using Hexkit.Global;
using Hexkit.Scenario;

namespace Hexkit.World.Instructions {

    /// <summary>
    /// Shows an <see cref="EntityClass"/> image on the map view.</summary>
    /// <remarks><para>
    /// <b>ShowImageInstruction</b> represents a command event that shows an <see
    /// cref="EntityClass"/> image on one or more map locations.
    /// </para><para>
    /// Please refer to method <b>Hexkit.Graphics.MapView.ShowImage</b> for the exact meaning of all
    /// <b>ShowImageInstruction</b> properties.
    /// </para><para>
    /// <b>ShowImageInstruction</b> is serialized to the XML element "ShowImage" defined in <see
    /// cref="FilePaths.SessionSchema"/>.</para></remarks>

    public sealed class ShowImageInstruction: ImageInstruction {
        #region ShowImageInstruction()

        /// <overloads>
        /// Initializes a new instance of the <see cref="ShowImageInstruction"/> class.</overloads>
        /// <summary>
        /// Initializes a new instance of the <see cref="ShowImageInstruction"/> class with default
        /// properties.</summary>

        internal ShowImageInstruction(): base() { }

        #endregion
        #region ShowImageInstruction(String, PointI[], Int32)

        /// <summary>
        /// Initializes a new instance of the <see cref="ShowImageInstruction"/> class with the
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

        internal ShowImageInstruction(string classId, PointI[] sites, int delay):
            base(classId, sites, delay) { }

        #endregion
    }
}
