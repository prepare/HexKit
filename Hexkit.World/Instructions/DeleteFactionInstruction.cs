using System;

using Tektosyne;
using Hexkit.Global;

namespace Hexkit.World.Instructions {

    /// <summary>
    /// Deletes a <see cref="Faction"/>.</summary>
    /// <remarks>
    /// <b>DeleteFactionInstruction</b> is serialized to the XML element "DeleteFaction" defined in
    /// <see cref="FilePaths.SessionSchema"/>.</remarks>

    public sealed class DeleteFactionInstruction: Instruction {
        #region DeleteFactionInstruction()

        /// <overloads>
        /// Initializes a new instance of the <see cref="DeleteFactionInstruction"/> class.
        /// </overloads>
        /// <summary>
        /// Initializes a new instance of the <see cref="DeleteFactionInstruction"/> class with
        /// default properties.</summary>

        internal DeleteFactionInstruction(): base() { }

        #endregion
        #region DeleteFactionInstruction(String)

        /// <summary>
        /// Initializes a new instance of the <see cref="DeleteFactionInstruction"/> class with the
        /// specified <see cref="Faction"/> identifier.</summary>
        /// <param name="factionId">
        /// The initial value for the <see cref="Instruction.Id"/> property, indicating the <see
        /// cref="Faction.Id"/> string of the <see cref="Faction"/> to delete.</param>

        internal DeleteFactionInstruction(string factionId): base(factionId) { }

        #endregion
        #region Execute

        /// <summary>
        /// Executes the <see cref="Instruction"/> on the specified <see cref="WorldState"/> and
        /// indicates whether the <see cref="WorldState"/> has changed.</summary>
        /// <param name="worldState">
        /// The <see cref="WorldState"/> on which to execute the <see
        /// cref="DeleteFactionInstruction"/>.</param>
        /// <returns>
        /// <c>true</c> if execution of the <see cref="DeleteFactionInstruction"/> has changed the
        /// data of the specified <paramref name="worldState"/>; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="worldState"/> is a null reference.</exception>
        /// <exception cref="InvalidCommandException">
        /// The <see cref="DeleteFactionInstruction"/> contains data that is invalid with respect to
        /// the specified <paramref name="worldState"/>.</exception>
        /// <remarks><para>
        /// <b>Execute</b> invokes <see cref="WorldState.DeleteFaction"/> on the specified <paramref
        /// name="worldState"/> with the <see cref="Faction"/> identified by the <see
        /// cref="Instruction.Id"/> property.
        /// </para><para>
        /// <b>Execute</b> always returns <c>true</c>.</para></remarks>

        internal override bool Execute(WorldState worldState) {
            if (worldState == null)
                ThrowHelper.ThrowArgumentNullException("worldState");

            // GetFaction throws exception for invalid Id
            Faction faction = GetFaction(worldState, Id);

            worldState.DeleteFaction(faction);

            // end history of deleted faction
            worldState.History.Factions[Id].Delete(worldState);

            return true;
        }

        #endregion
    }
}
