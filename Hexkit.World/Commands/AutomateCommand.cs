using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml;

using Tektosyne;
using Tektosyne.Xml;
using Hexkit.Global;

namespace Hexkit.World.Commands {

    /// <summary>
    /// Represents an "Automate" command.</summary>
    /// <remarks>
    /// <b>AutomateCommand</b> is serialized to the XML element "Automate" defined in <see
    /// cref="FilePaths.SessionSchema"/>.</remarks>

    public sealed class AutomateCommand: EntitiesCommand {
        #region AutomateCommand()

        /// <overloads>
        /// Initializes a new instance of the <see cref="AutomateCommand"/> class.</overloads>
        /// <summary>
        /// Initializes a new instance of the <see cref="AutomateCommand"/> class with default
        /// properties.</summary>

        internal AutomateCommand(): base() { }

        #endregion
        #region AutomateCommand(Faction, IList<Entity>, String)

        /// <summary>
        /// Initializes a new instance of the <see cref="AutomateCommand"/> class with the specified
        /// <see cref="WorldState"/>, entities, and internal text.</summary>
        /// <param name="faction">
        /// The initial value for the <see cref="Command.Faction"/> property.</param>
        /// <param name="entities">
        /// An <see cref="IList{T}"/> whose <see cref="Entity"/> elements provide the initial value
        /// for the <see cref="Command.Entities"/> property.</param>
        /// <param name="text">
        /// The initial value for the <see cref="Text"/> property.</param>
        /// <exception cref="ArgumentException">
        /// <paramref name="entities"/> contains a null reference.</exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="faction"/> is a null reference.</exception>
        /// <exception cref="ArgumentNullOrEmptyException"><para>
        /// <paramref name="entities"/> is a null reference or an empty collection.
        /// </para><para>-or-</para><para>
        /// <paramref name="text"/> is a null reference or an empty string.</para></exception>

        public AutomateCommand(Faction faction, IList<Entity> entities, string text):
            base(faction, entities) {

            if (String.IsNullOrEmpty(text))
                ThrowHelper.ThrowArgumentNullOrEmptyException("text");

            this._text = text;
        }

        #endregion
        #region Private Fields

        // backer for Text property
        private string _text;

        #endregion
        #region Text

        /// <summary>
        /// Gets the internal text associated with the <see cref="AutomateCommand"/>.</summary>
        /// <value>
        /// A <see cref="String"/> containing arbitrary internal information associated with the
        /// <see cref="AutomateCommand"/>.</value>
        /// <remarks><para>
        /// <b>Text</b> communicates internal data defined by the rule script, and does not appear
        /// in the string representation created by <see cref="EntitiesCommand.ToString"/>.
        /// </para><para>
        /// This property holds the value of the "text" XML element.</para></remarks>

        public String Text {
            [DebuggerStepThrough]
            get { return this._text; }
        }

        #endregion
        #region GenerateProgram

        /// <summary>
        /// Generates and executes the HCL <see cref="Command.Program"/> for the <see
        /// cref="AutomateCommand"/>.</summary>
        /// <exception cref="InvalidCommandException">
        /// The <see cref="AutomateCommand"/> contains data that is invalid with respect to the
        /// current <see cref="Command.Context"/>.</exception>
        /// <exception cref="PropertyValueException">
        /// <see cref="Command.Context"/> is a null reference.</exception>
        /// <remarks>
        /// <b>GenerateProgram</b> invokes <see cref="Faction.Automate"/> on the associated <see
        /// cref="Command.Faction"/>.</remarks>

        protected override void GenerateProgram() {
            Faction.Value.Automate(this);
        }

        #endregion
        #region Validate

        /// <summary>
        /// Validates the current data of the <see cref="AutomateCommand"/> against the specified
        /// <see cref="WorldState"/>.</summary>
        /// <param name="worldState">
        /// The <see cref="WorldState"/> to validate the <see cref="AutomateCommand"/> against.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="worldState"/> is a null reference.</exception>
        /// <exception cref="InvalidCommandException"><para>
        /// One of the conditions listed under <see cref="EntitiesCommand.Validate"/>.
        /// </para><para>-or-</para><para>
        /// <see cref="Text"/> is a null reference or an empty string.</para></exception>
        /// <remarks>
        /// Please refer to <see cref="EntitiesCommand.Validate"/> for details.</remarks>

        public override void Validate(WorldState worldState) {
            base.Validate(worldState);

            if (String.IsNullOrEmpty(Text))
                InvalidCommandException.ThrowNewWithFormat(Global.Strings.CommandTextNone, Name);
        }

        #endregion
        #region XmlSerializable Members
        #region ReadXmlElements

        /// <summary>
        /// Reads XML element data into the <see cref="AutomateCommand"/> object using the specified
        /// <see cref="XmlReader"/>.</summary>
        /// <param name="reader">
        /// The <see cref="XmlReader"/> from which to read.</param>
        /// <returns>
        /// <c>true</c> if the current node of the specified <paramref name="reader"/> contained any
        /// matching data; otherwise, <c>false</c>.</returns>
        /// <exception cref="XmlException">
        /// An error occurred while parsing the XML data provided by <paramref name="reader"/>.
        /// </exception>
        /// <exception cref="System.Xml.Schema.XmlSchemaException">
        /// <paramref name="reader"/> is an <see cref="XmlValidatingReader"/>, and the XML data did
        /// not conform to <see cref="FilePaths.SessionSchema"/>.</exception>
        /// <remarks>
        /// Please refer to class <see cref="XmlSerializable"/> for a complete description of this
        /// method.</remarks>

        protected override bool ReadXmlElements(XmlReader reader) {
            if (base.ReadXmlElements(reader))
                return true;

            switch (reader.Name) {

                case "text":
                    this._text = XmlUtility.ReadTextElement(reader);
                    return true;

                default: return false;
            }
        }

        #endregion
        #region WriteXmlElements

        /// <summary>
        /// Writes all current data of the <see cref="AutomateCommand"/> object that is serialized
        /// to nested XML elements to the specified <see cref="XmlWriter"/>.</summary>
        /// <param name="writer">
        /// The <see cref="XmlWriter"/> to which to write.</param>
        /// <remarks>
        /// Please refer to class <see cref="XmlSerializable"/> for a complete description of this
        /// method.</remarks>

        protected override void WriteXmlElements(XmlWriter writer) {
            base.WriteXmlElements(writer);
            writer.WriteElementString("text", Text);
        }

        #endregion
        #endregion
    }
}
