using System;

using Tektosyne;
using Hexkit.Global;
using Hexkit.Scenario;

namespace Hexkit.World.Instructions {

    /// <summary>
    /// Adds or changes a basic value of a <see cref="Faction"/> variable.</summary>
    /// <remarks>
    /// <b>SetFactionVariableInstruction</b> is serialized to the XML element "SetFactionVariable"
    /// defined in <see cref="FilePaths.SessionSchema"/>.</remarks>

    public sealed class SetFactionVariableInstruction: StringIntegerInstruction {
        #region SetFactionVariableInstruction()

        /// <overloads>
        /// Initializes a new instance of the <see cref="SetFactionVariableInstruction"/> class.
        /// </overloads>
        /// <summary>
        /// Initializes a new instance of the <see cref="SetFactionVariableInstruction"/> class with
        /// default properties.</summary>

        internal SetFactionVariableInstruction(): base() { }

        #endregion
        #region SetFactionVariableInstruction(String, String, Int32)

        /// <summary>
        /// Initializes a new instance of the <see cref="SetFactionVariableInstruction"/> class with
        /// the specified <see cref="Faction"/> and variable identifiers and basic value.</summary>
        /// <param name="factionId">
        /// The initial value for the <see cref="Instruction.Id"/> property, indicating the <see
        /// cref="Faction.Id"/> string of the <see cref="Faction"/> to manipulate.</param>
        /// <param name="variableId">
        /// The initial value for the <see cref="StringInstruction.Text"/> property, indicating the
        /// <see cref="VariableClass.Id"/> string of the <see cref="VariableClass"/> whose instance
        /// to add or change in the matching <see cref="VariableContainer"/> of the indicated <see
        /// cref="Faction"/>.</param>
        /// <param name="value">
        /// The initial value for the <see cref="StringIntegerInstruction.Value"/> property,
        /// indicating the new basic value to associate with the indicated <see
        /// cref="VariableClass"/>.</param>

        internal SetFactionVariableInstruction(string factionId, string variableId, int value):
            base(factionId, variableId, value) { }

        #endregion
        #region Execute

        /// <summary>
        /// Executes the <see cref="Instruction"/> on the specified <see cref="WorldState"/> and
        /// indicates whether the <see cref="WorldState"/> has changed.</summary>
        /// <param name="worldState">
        /// The <see cref="WorldState"/> on which to execute the <see
        /// cref="SetFactionVariableInstruction"/>.</param>
        /// <returns>
        /// <c>true</c> if execution of the <see cref="SetFactionVariableInstruction"/> has changed
        /// the data of the specified <paramref name="worldState"/>; otherwise, <c>false</c>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="worldState"/> is a null reference.</exception>
        /// <exception cref="InvalidCommandException">
        /// The <see cref="SetFactionVariableInstruction"/> contains data that is invalid with
        /// respect to the specified <paramref name="worldState"/>.</exception>
        /// <remarks><para>
        /// <b>Execute</b> performs the following actions:
        /// </para><list type="number"><item>
        /// Set the <see cref="InstructionResults.Faction"/> property of the <see
        /// cref="Instruction.Results"/> cache to the <see cref="Faction"/> identified by the <see
        /// cref="Instruction.Id"/> property.
        /// </item><item>
        /// Set the <see cref="InstructionResults.VariableClass"/> property of the <b>Results</b>
        /// cache to the <see cref="VariableClass"/> identified by the <see
        /// cref="StringInstruction.Text"/> property.
        /// </item><item>
        /// Set the <see cref="Faction.Counters"/> or <see cref="Faction.Resources"/> element of the
        /// indicated <b>Faction</b> that corresponds to the indicated <b>VariableClass</b> to the
        /// value of the <see cref="StringIntegerInstruction.Value"/> property. A new element is
        /// added if no matching identifier is found.</item></list></remarks>

        internal override bool Execute(WorldState worldState) {
            if (worldState == null)
                ThrowHelper.ThrowArgumentNullException("worldState");

            // GetFaction throws exception for invalid Id
            Faction faction = GetFaction(worldState, Id);

            // GetVariable throws exception for invalid Text
            VariableClass variableClass = GetVariable(Text);

            // return faction and variable class
            if (Results != null) {
                Results.Faction = faction;
                Results.VariableClass = variableClass;
            }

            // set basic value of appropriate category
            switch (variableClass.Category) {

                case VariableCategory.Counter:
                    return faction.Counters.SetValue(variableClass, Value, false);

                case VariableCategory.Resource:
                    return faction.Resources.SetValue(variableClass, Value, false);

                default:
                    InvalidCommandException.ThrowNewWithFormat(
                        Global.Strings.InstructionVariableCategoryInvalid,
                        Name, variableClass.Category);
                    return false;
            }
        }

        #endregion
    }
}
