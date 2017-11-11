using System;

using Tektosyne;
using Hexkit.Global;

namespace Hexkit.World.Instructions {

    /// <summary>
    /// Sets the <see cref="Faction.IsResigned"/> flag of a <see cref="Faction"/>.</summary>
    /// <remarks>
    /// <b>SetFactionResignedInstruction</b> is serialized to the XML element "SetFactionResigned"
    /// defined in <see cref="FilePaths.SessionSchema"/>.</remarks>

    public sealed class SetFactionResignedInstruction: BooleanInstruction {
        #region SetFactionResignedInstruction()

        /// <overloads>
        /// Initializes a new instance of the <see cref="SetFactionResignedInstruction"/> class.
        /// </overloads>
        /// <summary>
        /// Initializes a new instance of the <see cref="SetFactionResignedInstruction"/> class with
        /// default properties.</summary>

        internal SetFactionResignedInstruction(): base() { }

        #endregion
        #region SetFactionResignedInstruction(String, Boolean)

        /// <summary>
        /// Initializes a new instance of the <see cref="SetFactionResignedInstruction"/> class with
        /// the specified <see cref="Faction"/> identifier and resignation flag.</summary>
        /// <param name="factionId">
        /// The initial value for the <see cref="Instruction.Id"/> property, indicating the <see
        /// cref="Faction.Id"/> string of the <see cref="Faction"/> to manipulate.</param>
        /// <param name="resigned">
        /// The initial value for the <see cref="BooleanInstruction.Value"/> property, indicating
        /// the new value for the <see cref="Faction.IsResigned"/> property of the specified <see
        /// cref="Faction"/>.</param>

        internal SetFactionResignedInstruction(string factionId, bool resigned):
            base(factionId, resigned) { }

        #endregion
        #region Execute

        /// <summary>
        /// Executes the <see cref="Instruction"/> on the specified <see cref="WorldState"/> and
        /// indicates whether the <see cref="WorldState"/> has changed.</summary>
        /// <param name="worldState">
        /// The <see cref="WorldState"/> on which to execute the <see
        /// cref="SetFactionResignedInstruction"/>.</param>
        /// <returns>
        /// <c>true</c> if execution of the <see cref="SetFactionResignedInstruction"/> has changed
        /// the data of the specified <paramref name="worldState"/>; otherwise, <c>false</c>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="worldState"/> is a null reference.</exception>
        /// <exception cref="InvalidCommandException">
        /// The <see cref="SetFactionResignedInstruction"/> contains data that is invalid with
        /// respect to the specified <paramref name="worldState"/>.</exception>
        /// <remarks>
        /// <b>Execute</b> sets the <see cref="Faction.IsResigned"/> property of the <see
        /// cref="Faction"/> identified by the <see cref="Instruction.Id"/> property to the value of
        /// the <see cref="BooleanInstruction.Value"/> property.</remarks>

        internal override bool Execute(WorldState worldState) {
            if (worldState == null)
                ThrowHelper.ThrowArgumentNullException("worldState");

            // GetFaction throws exception for invalid Id
            Faction faction = GetFaction(worldState, Id);

            if (faction.IsResigned == Value)
                return false;

            faction.IsResigned = Value;
            return true;
        }

        #endregion
    }
}
