using System;

using Tektosyne;
using Hexkit.Global;

namespace Hexkit.World.Instructions {

    /// <summary>
    /// Completes the creation of a <see cref="Faction"/>.</summary>
    /// <remarks><para>
    /// <b>CreateFactionInstruction</b> does not actually create a new <see cref="Faction"/> object,
    /// but rather completes the initialization of an existing <see cref="Faction"/> object.
    /// </para><para>
    /// <b>CreateFactionInstruction</b> is serialized to the XML element "CreateFaction" defined in
    /// <see cref="FilePaths.SessionSchema"/>.</para></remarks>

    public sealed class CreateFactionInstruction: Instruction {
        #region CreateFactionInstruction()

        /// <overloads>
        /// Initializes a new instance of the <see cref="CreateFactionInstruction"/> class.
        /// </overloads>
        /// <summary>
        /// Initializes a new instance of the <see cref="CreateFactionInstruction"/> class with
        /// default properties.</summary>

        internal CreateFactionInstruction(): base() { }

        #endregion
        #region CreateFactionInstruction(String)

        /// <summary>
        /// Initializes a new instance of the <see cref="CreateFactionInstruction"/> class with the
        /// specified <see cref="Faction"/> identifier.</summary>
        /// <param name="factionId">
        /// The initial value for the <see cref="Instruction.Id"/> property, indicating the <see
        /// cref="Faction.Id"/> string of the created <see cref="Faction"/>.</param>

        internal CreateFactionInstruction(string factionId): base(factionId) { }

        #endregion
        #region Execute

        /// <summary>
        /// Executes the <see cref="Instruction"/> on the specified <see cref="WorldState"/> and
        /// indicates whether the <see cref="WorldState"/> has changed.</summary>
        /// <param name="worldState">
        /// The <see cref="WorldState"/> on which to execute the <see
        /// cref="CreateFactionInstruction"/>.</param>
        /// <returns>
        /// <c>true</c> if execution of the <see cref="CreateFactionInstruction"/> has changed the
        /// data of the specified <paramref name="worldState"/>; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="worldState"/> is a null reference.</exception>
        /// <exception cref="InvalidCommandException">
        /// The <see cref="CreateFactionInstruction"/> contains data that is invalid with respect to
        /// the specified <paramref name="worldState"/>.</exception>
        /// <remarks><para>
        /// <b>Execute</b> performs the following actions:
        /// </para><list type="number"><item>
        /// Call <see cref="Instruction.GetFactionHistory"/> to obtain the <see
        /// cref="FactionHistory"/> identified by the <see cref="Instruction.Id"/> property.
        /// </item><item>
        /// Invoke <see cref="FactionHistory.Create"/> on the resulting <b>FactionHistory</b>.
        /// </item></list><para>
        /// <b>Execute</b> always returns <c>true</c>.</para></remarks>

        internal override bool Execute(WorldState worldState) {
            if (worldState == null)
                ThrowHelper.ThrowArgumentNullException("worldState");

            // GetFactionHistory throws exception for invalid Id
            FactionHistory history = GetFactionHistory(worldState, Id);

            // mark faction as created
            history.Create(worldState);

            return true;
        }

        #endregion
    }
}
