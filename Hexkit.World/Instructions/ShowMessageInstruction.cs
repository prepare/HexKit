using System;
using Hexkit.Global;

namespace Hexkit.World.Instructions {

    /// <summary>
    /// Shows a message in the event view panel.</summary>
    /// <remarks><para>
    /// <b>ShowMessageInstruction</b> represents a command event that shows a text message in the
    /// event view panel.
    /// </para><para>
    /// <b>ShowMessageInstruction</b> is serialized to the XML element "ShowMessage" defined in <see
    /// cref="FilePaths.SessionSchema"/>.</para></remarks>

    public sealed class ShowMessageInstruction: MessageInstruction {
        #region ShowMessageInstruction()

        /// <overloads>
        /// Initializes a new instance of the <see cref="ShowMessageInstruction"/> class.
        /// </overloads>
        /// <summary>
        /// Initializes a new instance of the <see cref="ShowMessageInstruction"/> class with
        /// default properties.</summary>

        internal ShowMessageInstruction(): base() { }

        #endregion
        #region ShowMessageInstruction(String, String, String, String[])

        /// <summary>
        /// Initializes a new instance of the <see cref="ShowMessageInstruction"/> class with the
        /// specified summary and detail text, faction identifier, and display names.</summary>
        /// <param name="summary">
        /// The initial value for the <see cref="StringInstruction.Text"/> property, providing a
        /// summary description of the message event.</param>
        /// <param name="details">
        /// The initial value for the <see cref="MessageInstruction.Details"/> property.</param>
        /// <param name="factionId">
        /// The initial value for the <see cref="Instruction.Id"/> property, indicating the <see
        /// cref="Faction"/> primarily affected by the message event.</param>
        /// <param name="names">
        /// An <see cref="Array"/> containing the initial elements for the <see
        /// cref="MessageInstruction.Names"/> collection.</param>

        internal ShowMessageInstruction(string summary,
            string details, string factionId, string[] names):

            base(summary, details, factionId, names) { }

        #endregion
    }
}
