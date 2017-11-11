using System;

using Tektosyne;
using Hexkit.Global;

namespace Hexkit.World.Instructions {

    /// <summary>
    /// Activates the next <see cref="Faction"/>.</summary>
    /// <remarks>
    /// <b>AdvanceFactionInstruction</b> is serialized to the XML element "AdvanceFaction" defined
    /// in <see cref="FilePaths.SessionSchema"/>.</remarks>

    public sealed class AdvanceFactionInstruction: Instruction {
        #region AdvanceFactionInstruction()

        /// <summary>
        /// Initializes a new instance of the <see cref="AdvanceFactionInstruction"/> class.
        /// </summary>

        internal AdvanceFactionInstruction(): base() { }

        #endregion
        #region Execute

        /// <summary>
        /// Executes the <see cref="Instruction"/> on the specified <see cref="WorldState"/> and
        /// indicates whether the <see cref="WorldState"/> has changed.</summary>
        /// <param name="worldState">
        /// The <see cref="WorldState"/> on which to execute the <see
        /// cref="AdvanceFactionInstruction"/>.</param>
        /// <returns>
        /// <c>true</c> if execution of the <see cref="AdvanceFactionInstruction"/> has changed the
        /// data of the specified <paramref name="worldState"/>; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="worldState"/> is a null reference.</exception>
        /// <exception cref="InvalidCommandException">
        /// The <see cref="AdvanceFactionInstruction"/> contains data that is invalid with respect
        /// to the specified <paramref name="worldState"/>.</exception>
        /// <remarks><para>
        /// <b>Execute</b> invokes <see cref="WorldState.AdvanceFaction"/> on the specified
        /// <paramref name="worldState"/>.
        /// </para><para>
        /// <b>Execute</b> always returns <c>true</c>.</para></remarks>

        internal override bool Execute(WorldState worldState) {
            if (worldState == null)
                ThrowHelper.ThrowArgumentNullException("worldState");

            // continue history for active faction
            string id = worldState.ActiveFaction.Id;
            worldState.History.Factions[id].Advance(worldState);

            worldState.AdvanceFaction();
            return true;
        }

        #endregion
    }
}
