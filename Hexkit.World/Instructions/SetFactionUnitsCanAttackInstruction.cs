using System;

using Tektosyne;
using Hexkit.Global;
using Hexkit.Scenario;

namespace Hexkit.World.Instructions {

    /// <summary>
    /// Sets the <see cref="Unit.CanAttack"/> flag of all <see cref="Faction.Units"/> of a <see
    /// cref="Faction"/> that are based on a specific <see cref="UnitClass"/>.</summary>
    /// <remarks>
    /// <b>SetFactionUnitsCanAttackInstruction</b> is serialized to the XML element
    /// "SetFactionUnitsCanAttack" defined in <see cref="FilePaths.SessionSchema"/>.</remarks>

    public sealed class SetFactionUnitsCanAttackInstruction: StringBooleanInstruction {
        #region SetFactionUnitsCanAttackInstruction()

        /// <overloads>
        /// Initializes a new instance of the <see cref="SetFactionUnitsCanAttackInstruction"/>
        /// class.</overloads>
        /// <summary>
        /// Initializes a new instance of the <see cref="SetFactionUnitsCanAttackInstruction"/>
        /// class with default properties.</summary>

        internal SetFactionUnitsCanAttackInstruction(): base() { }

        #endregion
        #region SetFactionUnitsCanAttackInstruction(String, String, Boolean)

        /// <summary>
        /// Initializes a new instance of the <see cref="SetFactionUnitsCanAttackInstruction"/>
        /// class with the specified <see cref="Faction"/> and <see cref="UnitClass"/> identifiers
        /// and attack flag.</summary>
        /// <param name="factionId">
        /// The initial value for the <see cref="Instruction.Id"/> property, indicating the <see
        /// cref="Faction.Id"/> string of the <see cref="Faction"/> to manipulate.</param>
        /// <param name="classId">
        /// The initial value for the <see cref="StringInstruction.Text"/> property, indicating the
        /// <see cref="EntityClass.Id"/> string of the <see cref="UnitClass"/> whose instances to
        /// manipulate.</param>
        /// <param name="canAttack">
        /// The initial value for the <see cref="BooleanInstruction.Value"/> property, indicating
        /// the new value for the <see cref="Unit.CanAttack"/> flag of all specified <see
        /// cref="Unit"/> objects.</param>

        internal SetFactionUnitsCanAttackInstruction(string factionId, string classId, bool canAttack):
            base(factionId, classId, canAttack) { }

        #endregion
        #region Execute

        /// <summary>
        /// Executes the <see cref="Instruction"/> on the specified <see cref="WorldState"/> and
        /// indicates whether the <see cref="WorldState"/> has changed.</summary>
        /// <param name="worldState">
        /// The <see cref="WorldState"/> on which to execute the <see
        /// cref="SetFactionUnitsCanAttackInstruction"/>.</param>
        /// <returns>
        /// <c>true</c> if execution of the <see cref="SetFactionUnitsCanAttackInstruction"/> has
        /// changed the data of the specified <paramref name="worldState"/>; otherwise,
        /// <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="worldState"/> is a null reference.</exception>
        /// <exception cref="InvalidCommandException">
        /// The <see cref="SetFactionUnitsCanAttackInstruction"/> contains data that is invalid with
        /// respect to the specified <paramref name="worldState"/>.</exception>
        /// <remarks><para>
        /// <b>Execute</b> invokes <see cref="Faction.SetUnitsCanAttack"/> on the <see
        /// cref="Faction"/> identified by the <see cref="Instruction.Id"/> property with the value
        /// of the <see cref="BooleanInstruction.Value"/> property.
        /// </para><para>
        /// The supplied <see cref="UnitClass"/> parameter is the <see cref="UnitClass"/> indicated
        /// by the <see cref="StringInstruction.Text"/> property, or a null reference if <b>Text</b>
        /// is a null reference or an empty string.</para></remarks>

        internal override bool Execute(WorldState worldState) {
            if (worldState == null)
                ThrowHelper.ThrowArgumentNullException("worldState");

            // GetFaction throws exception for invalid Id
            Faction faction = GetFaction(worldState, Id);
            UnitClass unitClass = null;

            // GetEntityClass throws exception for invalid Text
            if (!String.IsNullOrEmpty(Text))
                unitClass = (UnitClass) GetEntityClass(Text, EntityCategory.Unit);

            return faction.SetUnitsCanAttack(unitClass, Value);
        }

        #endregion
    }
}
