using System;

using Tektosyne;
using Hexkit.Global;
using Hexkit.Scenario;

namespace Hexkit.World.Instructions {

    /// <summary>
    /// Sets the <see cref="Unit.CanAttack"/> flag of a <see cref="Unit"/>.</summary>
    /// <remarks>
    /// <b>SetUnitCanAttackInstruction</b> is serialized to the XML element "SetUnitCanAttack"
    /// defined in <see cref="FilePaths.SessionSchema"/>.</remarks>

    public sealed class SetUnitCanAttackInstruction: BooleanInstruction {
        #region SetUnitCanAttackInstruction()

        /// <overloads>
        /// Initializes a new instance of the <see cref="SetUnitCanAttackInstruction"/> class.
        /// </overloads>
        /// <summary>
        /// Initializes a new instance of the <see cref="SetUnitCanAttackInstruction"/> class with
        /// default properties.</summary>

        internal SetUnitCanAttackInstruction(): base() { }

        #endregion
        #region SetUnitCanAttackInstruction(String, Boolean)

        /// <summary>
        /// Initializes a new instance of the <see cref="SetUnitCanAttackInstruction"/> class with
        /// the specified <see cref="Unit"/> identifier and attack flag.</summary>
        /// <param name="unitId">
        /// The initial value for the <see cref="Instruction.Id"/> property, indicating the <see
        /// cref="Entity.Id"/> string of the <see cref="Unit"/> to manipulate.</param>
        /// <param name="canAttack">
        /// The initial value for the <see cref="BooleanInstruction.Value"/> property, indicating
        /// the new value for the <see cref="Unit.CanAttack"/> flag of the specified <see
        /// cref="Unit"/>.</param>

        internal SetUnitCanAttackInstruction(string unitId, bool canAttack):
            base(unitId, canAttack) { }

        #endregion
        #region Execute

        /// <summary>
        /// Executes the <see cref="Instruction"/> on the specified <see cref="WorldState"/> and
        /// indicates whether the <see cref="WorldState"/> has changed.</summary>
        /// <param name="worldState">
        /// The <see cref="WorldState"/> on which to execute the <see
        /// cref="SetUnitCanAttackInstruction"/>.</param>
        /// <returns>
        /// <c>true</c> if execution of the <see cref="SetUnitCanAttackInstruction"/> has changed
        /// the data of the specified <paramref name="worldState"/>; otherwise, <c>false</c>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="worldState"/> is a null reference.</exception>
        /// <exception cref="InvalidCommandException">
        /// The <see cref="SetUnitCanAttackInstruction"/> contains data that is invalid with respect
        /// to the specified <paramref name="worldState"/>.</exception>
        /// <remarks>
        /// <b>Execute</b> invokes <see cref="Unit.SetCanAttack"/> on the <see cref="Unit"/> 
        /// identified by the <see cref="Instruction.Id"/> property with the value of the <see
        /// cref="BooleanInstruction.Value"/> property.</remarks>

        internal override bool Execute(WorldState worldState) {
            if (worldState == null)
                ThrowHelper.ThrowArgumentNullException("worldState");

            // GetEntity throws exception for invalid Id
            Unit unit = (Unit) GetEntity(worldState, Id, EntityCategory.Unit);

            return unit.SetCanAttack(Value);
        }

        #endregion
    }
}
