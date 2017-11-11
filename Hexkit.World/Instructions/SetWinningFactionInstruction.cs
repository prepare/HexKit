using System;

using Tektosyne;
using Hexkit.Global;

namespace Hexkit.World.Instructions {

    /// <summary>
    /// Declares a <see cref="Faction"/> victorious and ends the game.</summary>
    /// <remarks>
    /// <b>SetWinningFactionInstruction</b> is serialized to the XML element "SetWinningFaction"
    /// defined in <see cref="FilePaths.SessionSchema"/>.</remarks>

    public sealed class SetWinningFactionInstruction: Instruction {
        #region SetWinningFactionInstruction()

        /// <overloads>
        /// Initializes a new instance of the <see cref="SetWinningFactionInstruction"/> class.
        /// </overloads>
        /// <summary>
        /// Initializes a new instance of the <see cref="SetWinningFactionInstruction"/> class with
        /// default properties.</summary>

        internal SetWinningFactionInstruction(): base() { }

        #endregion
        #region SetWinningFactionInstruction(String)

        /// <summary>
        /// Initializes a new instance of the <see cref="SetWinningFactionInstruction"/> class with
        /// the specified <see cref="Faction"/> identifier.</summary>
        /// <param name="factionId">
        /// The initial value for the <see cref="Instruction.Id"/> property, indicating the <see
        /// cref="Faction.Id"/> string of the victorious <see cref="Faction"/>.</param>
        /// <remarks>
        /// Specify a null reference or an empty string for <paramref name="factionId"/> to set the
        /// <see cref="WorldState.WinningFaction"/> property of the current <see cref="WorldState"/>
        /// to a null reference.</remarks>

        internal SetWinningFactionInstruction(string factionId): base(factionId) { }

        #endregion
        #region Execute

        /// <summary>
        /// Executes the <see cref="Instruction"/> on the specified <see cref="WorldState"/> and
        /// indicates whether the <see cref="WorldState"/> has changed.</summary>
        /// <param name="worldState">
        /// The <see cref="WorldState"/> on which to execute the <see
        /// cref="SetWinningFactionInstruction"/>.</param>
        /// <returns>
        /// <c>true</c> if execution of the <see cref="SetWinningFactionInstruction"/> has changed
        /// the data of the specified <paramref name="worldState"/>; otherwise, <c>false</c>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="worldState"/> is a null reference.</exception>
        /// <exception cref="InvalidCommandException">
        /// The <see cref="SetWinningFactionInstruction"/> contains data that is invalid with
        /// respect to the specified <paramref name="worldState"/>.</exception>
        /// <remarks>
        /// <b>Execute</b> invokes <see cref="WorldState.SetWinningFaction"/> on the specified
        /// <paramref name="worldState"/> with the <see cref="Faction"/> identified by the <see
        /// cref="Instruction.Id"/> property, or with a null reference if <b>Id</b> is a null
        /// reference or an empty string.</remarks>

        internal override bool Execute(WorldState worldState) {
            if (worldState == null)
                ThrowHelper.ThrowArgumentNullException("worldState");

            Faction faction = null;

            // GetFaction throws exception for invalid Id
            if (!String.IsNullOrEmpty(Id))
                faction = GetFaction(worldState, Id);

            if (worldState.SetWinningFaction(faction)) {

                // end history of victorious faction
                if (!String.IsNullOrEmpty(Id))
                    worldState.History.Factions[Id].Victory(worldState);

                return true;
            }

            return false;
        }

        #endregion
    }
}
