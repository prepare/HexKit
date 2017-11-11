using System;
using Hexkit.Global;

namespace Hexkit.World.Instructions {

    /// <summary>
    /// Shows a message in the event view panel and in a modal dialog.</summary>
    /// <remarks><para>
    /// <b>ShowMessageDialogInstruction</b> represents a command event that shows a text message in
    /// the event view panel, and also in a modal dialog.
    /// </para><para>
    /// <b>ShowMessageDialogInstruction</b> is serialized to the XML element "ShowMessageDialog"
    /// defined in <see cref="FilePaths.SessionSchema"/>.</para></remarks>

    public sealed class ShowMessageDialogInstruction: MessageInstruction {
        #region ShowMessageDialogInstruction()

        /// <overloads>
        /// Initializes a new instance of the <see cref="ShowMessageDialogInstruction"/> class.
        /// </overloads>
        /// <summary>
        /// Initializes a new instance of the <see cref="ShowMessageDialogInstruction"/> class with
        /// default properties.</summary>

        internal ShowMessageDialogInstruction(): base() { }

        #endregion
        #region ShowMessageDialogInstruction(String, String, String, String[])

        /// <summary>
        /// Initializes a new instance of the <see cref="ShowMessageDialogInstruction"/> class with
        /// the specified summary and detail text, faction identifier, and display names.</summary>
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

        internal ShowMessageDialogInstruction(string summary,
            string details, string factionId, string[] names):

            base(summary, details, factionId, names) { }

        #endregion
    }
}
