using System;
using System.Diagnostics;
using System.Xml;

using Tektosyne;
using Tektosyne.Xml;

using Hexkit.Global;
using Hexkit.Scenario;

namespace Hexkit.World.Instructions {

    /// <summary>
    /// Adds or changes a modifier value of an <see cref="Entity"/> variable.</summary>
    /// <remarks>
    /// <b>SetEntityVariableModifierInstruction</b> is serialized to the XML element
    /// "SetEntityVariableModifier" defined in <see cref="FilePaths.SessionSchema"/>.</remarks>

    public sealed class SetEntityVariableModifierInstruction: StringIntegerInstruction {
        #region SetEntityVariableModifierInstruction()

        /// <overloads>
        /// Initializes a new instance of the <see cref="SetEntityVariableModifierInstruction"/>
        /// class.</overloads>
        /// <summary>
        /// Initializes a new instance of the <see cref="SetEntityVariableModifierInstruction"/>
        /// class with default properties.</summary>

        internal SetEntityVariableModifierInstruction(): base() { }

        #endregion
        #region SetEntityVariableModifierInstruction(String, String, ModifierTarget, Int32)

        /// <summary>
        /// Initializes a new instance of the <see cref="SetEntityVariableModifierInstruction"/>
        /// class with the specified entity and variable identifiers, <see cref="ModifierTarget"/>,
        /// and modifier value.</summary>
        /// <param name="entityId">
        /// The initial value for the <see cref="Instruction.Id"/> property, indicating the <see
        /// cref="Entity.Id"/> string of the <see cref="Entity"/> to manipulate.</param>
        /// <param name="variableId">
        /// The initial value for the <see cref="StringInstruction.Text"/> property, indicating the
        /// <see cref="VariableClass.Id"/> string of the <see cref="VariableClass"/> whose instance
        /// to add or change in the matching <see cref="VariableModifierContainer"/> of the
        /// indicated <see cref="Entity"/>.</param>
        /// <param name="target">
        /// The initial value for the <see cref="Target"/> property.</param>
        /// <param name="value">
        /// The initial value for the <see cref="StringIntegerInstruction.Value"/> property,
        /// indicating the new modifier value to associate with the indicated <see
        /// cref="VariableClass"/>.</param>

        internal SetEntityVariableModifierInstruction(string entityId, string variableId,
            ModifierTarget target, int value): base(entityId, variableId, value) {

            this._target = target;
        }

        #endregion
        #region Private Fields

        // property backers
        private ModifierTarget _target;

        #endregion
        #region Target

        /// <summary>
        /// Gets the <see cref="ModifierTarget"/> affected by the <see cref="Instruction"/>.
        /// </summary>
        /// <value>
        /// A <see cref="ModifierTarget"/> value indicating which <see cref="VariableClass"/>
        /// modifier to change.</value>
        /// <remarks>
        /// <b>Target</b> holds the value of the "target" XML attribute.</remarks>

        public ModifierTarget Target {
            [DebuggerStepThrough]
            get { return this._target; }
        }

        #endregion
        #region Execute

        /// <summary>
        /// Executes the <see cref="Instruction"/> on the specified <see cref="WorldState"/> and
        /// indicates whether the <see cref="WorldState"/> has changed.</summary>
        /// <param name="worldState">
        /// The <see cref="WorldState"/> on which to execute the <see
        /// cref="SetEntityVariableModifierInstruction"/>.</param>
        /// <returns>
        /// <c>true</c> if execution of the <see cref="SetEntityVariableModifierInstruction"/> has
        /// changed the data of the specified <paramref name="worldState"/>; otherwise,
        /// <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="worldState"/> is a null reference.</exception>
        /// <exception cref="InvalidCommandException">
        /// The <see cref="SetEntityVariableModifierInstruction"/> contains data that is invalid
        /// with respect to the specified <paramref name="worldState"/>.</exception>
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
        /// Invoke <see cref="Entity.UpdateModifierMaps"/> on the <see cref="Entity"/> to remove its
        /// effects from all modifier maps in the specified <paramref name="worldState"/>.
        /// </item><item>
        /// Set the <see cref="Entity.AttributeModifiers"/> or <see
        /// cref="Entity.ResourceModifiers"/> element of the indicated <b>Entity</b> that
        /// corresponds to the indicated <b>VariableClass</b> and <see cref="Target"/> to the value
        /// of the <see cref="StringIntegerInstruction.Value"/> property. A new element is added if
        /// no matching identifier is found.
        /// </item><item>
        /// Again invoke <see cref="Entity.UpdateModifierMaps"/> on the <see cref="Entity"/> to add
        /// its effects to all modifier maps in the specified <paramref name="worldState"/>.
        /// </item></list></remarks>

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

            bool result = false;

            // check if we should update modifier maps
            bool updateMaps = (Target != ModifierTarget.Self && Target != ModifierTarget.Owner);
            if (updateMaps)
                entity.UpdateModifierMaps(worldState, false);

            // set modifier value of appropriate category
            switch (variableClass.Category) {

                case VariableCategory.Attribute:
                    result = entity.AttributeModifiers.SetValue(variableClass, Target, Value);
                    break;

                case VariableCategory.Resource:
                    result = entity.ResourceModifiers.SetValue(variableClass, Target, Value);
                    break;

                default:
                    InvalidCommandException.ThrowNewWithFormat(
                        Global.Strings.InstructionVariableCategoryInvalid,
                        Name, variableClass.Category);
                    break;
            }

            if (updateMaps)
                entity.UpdateModifierMaps(worldState, true);

            return result;
        }

        #endregion
        #region XmlSerializable Members
        #region ReadXmlAttributes

        /// <summary>
        /// Reads XML attribute data into the <see cref="SetEntityVariableModifierInstruction"/>
        /// object using the specified <see cref="XmlReader"/>.</summary>
        /// <param name="reader">
        /// The <see cref="XmlReader"/> from which to read.</param>
        /// <exception cref="XmlException">
        /// An error occurred while parsing the XML data provided by <paramref name="reader"/>.
        /// </exception>
        /// <exception cref="System.Xml.Schema.XmlSchemaException">
        /// <paramref name="reader"/> is an <see cref="XmlValidatingReader"/>, and the XML data did
        /// not conform to <see cref="FilePaths.SessionSchema"/>.</exception>
        /// <remarks>
        /// Please refer to class <see cref="XmlSerializable"/> for a complete description of this
        /// method.</remarks>

        protected override void ReadXmlAttributes(XmlReader reader) {
            base.ReadXmlAttributes(reader);

            XmlUtility.ReadAttributeAsEnum(reader, "target", ref this._target);
        }

        #endregion
        #region WriteXmlAttributes

        /// <summary>
        /// Writes all current data of the <see cref="SetEntityVariableModifierInstruction"/> object
        /// that is serialized to XML attributes to the specified <see cref="XmlWriter"/>.</summary>
        /// <param name="writer">
        /// The <see cref="XmlWriter"/> to which to write.</param>
        /// <remarks>
        /// Please refer to class <see cref="XmlSerializable"/> for a complete description of this
        /// method.</remarks>

        protected override void WriteXmlAttributes(XmlWriter writer) {
            base.WriteXmlAttributes(writer);

            writer.WriteAttributeString("target", Target.ToString());
        }

        #endregion
        #endregion
    }
}
