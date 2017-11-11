using System;
using System.ComponentModel;

using Tektosyne;
using Hexkit.Global;
using Hexkit.Scenario;

namespace Hexkit.World.Instructions {

    /// <summary>
    /// Adds or changes a basic value of an <see cref="Entity"/> variable.</summary>
    /// <remarks>
    /// <b>SetEntityVariableInstruction</b> is serialized to the XML element "SetEntityVariable"
    /// defined in <see cref="FilePaths.SessionSchema"/>.</remarks>

    public sealed class SetEntityVariableInstruction: StringIntegerInstruction {
        #region SetEntityVariableInstruction()

        /// <overloads>
        /// Initializes a new instance of the <see cref="SetEntityVariableInstruction"/> class.
        /// </overloads>
        /// <summary>
        /// Initializes a new instance of the <see cref="SetEntityVariableInstruction"/> class with
        /// default properties.</summary>

        internal SetEntityVariableInstruction(): base() { }

        #endregion
        #region SetEntityVariableInstruction(String, String, Int32)

        /// <summary>
        /// Initializes a new instance of the <see cref="SetEntityVariableInstruction"/> class with
        /// the specified entity and variable identifiers and basic value.</summary>
        /// <param name="entityId">
        /// The initial value for the <see cref="Instruction.Id"/> property, indicating the <see
        /// cref="Entity.Id"/> string of the <see cref="Entity"/> to manipulate.</param>
        /// <param name="variableId">
        /// The initial value for the <see cref="StringInstruction.Text"/> property, indicating the
        /// <see cref="VariableClass.Id"/> string of the <see cref="VariableClass"/> whose instance
        /// to add or change in the matching <see cref="VariableContainer"/> of the specified <see
        /// cref="Entity"/>.</param>
        /// <param name="value">
        /// The initial value for the <see cref="StringIntegerInstruction.Value"/> property,
        /// indicating the new basic value to associate with the indicated <see
        /// cref="VariableClass"/>.</param>

        internal SetEntityVariableInstruction(string entityId, string variableId, int value):
            base(entityId, variableId, value) { }

        #endregion
        #region Execute

        /// <summary>
        /// Executes the <see cref="Instruction"/> on the specified <see cref="WorldState"/> and
        /// indicates whether the <see cref="WorldState"/> has changed.</summary>
        /// <param name="worldState">
        /// The <see cref="WorldState"/> on which to execute the <see
        /// cref="SetEntityVariableInstruction"/>.</param>
        /// <returns>
        /// <c>true</c> if execution of the <see cref="SetEntityVariableInstruction"/> has changed
        /// the data of the specified <paramref name="worldState"/>; otherwise, <c>false</c>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="worldState"/> is a null reference.</exception>
        /// <exception cref="InvalidCommandException">
        /// The <see cref="SetEntityVariableInstruction"/> contains data that is invalid with
        /// respect to the specified <paramref name="worldState"/>.</exception>
        /// <remarks><para>
        /// <b>Execute</b> performs the following actions:
        /// </para><list type="number"><item>
        /// Set the <see cref="InstructionResults.Entity"/> property of the <see
        /// cref="Instruction.Results"/> cache to the <see cref="Entity"/> identified by the <see
        /// cref="Instruction.Id"/> property.
        /// </item><item>
        /// Set the <see cref="InstructionResults.VariableClass"/> property of the <b>Results</b>
        /// cache to the <see cref="VariableClass"/> identified by the <see
        /// cref="StringInstruction.Text"/> property.
        /// </item><item>
        /// Set the <see cref="Entity.Attributes"/>, <see cref="Entity.Counters"/>, or <see
        /// cref="Entity.Resources"/> element of the indicated <b>Entity</b> that corresponds to the
        /// indicated <b>VariableClass</b> to the value of the <see
        /// cref="StringIntegerInstruction.Value"/> property. A new element is added if no matching
        /// identifier is found.</item></list></remarks>

        internal override bool Execute(WorldState worldState) {
            if (worldState == null)
                ThrowHelper.ThrowArgumentNullException("worldState");

            // GetEntity throws exception for invalid Id
            Entity entity = GetEntity(worldState, Id);

            // GetVariable throws exception for invalid Text
            VariableClass variableClass = GetVariable(Text);

            // return entity and variable class
            if (Results != null) {
                Results.Entity = entity;
                Results.VariableClass = variableClass;
            }

            // set basic value of appropriate category
            switch (variableClass.Category) {

                case VariableCategory.Attribute:
                    return entity.Attributes.SetValue(variableClass, Value, false);

                case VariableCategory.Counter:
                    return entity.Counters.SetValue(variableClass, Value, false);

                case VariableCategory.Resource:
                    return entity.Resources.SetValue(variableClass, Value, false);

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
