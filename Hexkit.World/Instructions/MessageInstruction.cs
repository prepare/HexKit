using System;
using System.Diagnostics;
using System.Xml;

using Tektosyne.Collections;
using Tektosyne.Xml;

using Hexkit.Global;
using Hexkit.Scenario;

namespace Hexkit.World.Instructions {
    #region Type Aliases

    using NameList = ListEx<String>;

    #endregion

    /// <summary>
    /// Represents a message event instruction.</summary>
    /// <remarks><para>
    /// <b>MessageInstruction</b> adds the <see cref="MessageInstruction.Details"/> and <see
    /// cref="MessageInstruction.Names"/> instruction parameters to the functionality provided by
    /// the <see cref="StringInstruction"/> class.
    /// </para><para>
    /// Derived classes should add the remaining members and any other functionality that is
    /// required for specific instructions.
    /// </para><para>
    /// <b>MessageInstruction</b> corresponds to the the complex XML type "messageInstruction"
    /// defined in <see cref="FilePaths.SessionSchema"/>.</para></remarks>

    public abstract class MessageInstruction: StringInstruction {
        #region MessageInstruction()

        /// <overloads>
        /// Initializes a new instance of the <see cref="MessageInstruction"/> class.</overloads>
        /// <summary>
        /// Initializes a new instance of the <see cref="MessageInstruction"/> class with default
        /// properties.</summary>

        protected MessageInstruction(): base() {
            this._names = new NameList();
        }

        #endregion
        #region MessageInstruction(String, String, String, String[])

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageInstruction"/> class with the
        /// specified summary and detail text, <see cref="World.Faction"/> identifier, and display
        /// names.</summary>
        /// <param name="summary">
        /// The initial value for the <see cref="StringInstruction.Text"/> property, providing a
        /// summary description of the message event.</param>
        /// <param name="details">
        /// The initial value for the <see cref="Details"/> property.</param>
        /// <param name="factionId">
        /// The initial value for the <see cref="Instruction.Id"/> property, indicating the <see
        /// cref="Faction"/> primarily affected by the message event.</param>
        /// <param name="names">
        /// An <see cref="Array"/> containing the initial elements for the <see cref="Names"/>
        /// collection.</param>

        protected MessageInstruction(string summary, string details,
            string factionId, string[] names): base(factionId, summary) {

            Details = details;

            if (names != null && names.Length > 0)
                this._names = new NameList(names);
            else
                this._names = new NameList(0);
        }

        #endregion
        #region Private Fields

        // property backers
        private readonly NameList _names;

        #endregion
        #region Category

        /// <summary>
        /// Gets the category of the <see cref="Instruction"/>.</summary>
        /// <value>
        /// The category <see cref="InstructionCategory.Event"/>, indicating that the <see
        /// cref="MessageInstruction"/> class represents a message event.</value>

        public override InstructionCategory Category {
            [DebuggerStepThrough]
            get { return InstructionCategory.Event; }
        }

        #endregion
        #region Details

        /// <summary>
        /// Gets a detailed description of the message event.</summary>
        /// <value>
        /// A <see cref="String"/> containing a detailed description of the message event
        /// represented by the <see cref="MessageInstruction"/>. The default is a null reference.
        /// </value>
        /// <remarks>
        /// <b>Details</b> may contain one or more paragraphs of text, including line breaks. This
        /// property holds the value of the "details" XML element.</remarks>

        public string Details { get; private set; }

        #endregion
        #region Faction

        /// <summary>
        /// Gets the faction that is primarily affected by the message event.</summary>
        /// <value>
        /// The <see cref="FactionClass"/> defined by the current <see cref="FactionSection"/> whose
        /// <see cref="FactionClass.Id"/> string equals the value of the <see
        /// cref="Instruction.Id"/> property.</value>
        /// <remarks><para>
        /// <b>Faction</b> returns a null reference if <see cref="Instruction.Id"/> is a null
        /// reference or an empty string, or if the current <see cref="FactionSection"/> does not
        /// contain a matching <see cref="FactionClass"/>.
        /// </para><para>
        /// <b>Faction</b> returns a <b>FactionClass</b> rather than a <see cref="World.Faction"/>
        /// object because the message event may be shown after the referenced <b>Faction</b> has
        /// been eliminated.</para></remarks>

        public FactionClass Faction {
            get {
                if (String.IsNullOrEmpty(Id))
                    return null;

                FactionClass faction;
                MasterSection.Instance.Factions.Collection.TryGetValue(Id, out faction);
                return faction;
            }
        }

        #endregion
        #region Names

        /// <summary>
        /// Gets the display names of all entities affected by the message event.</summary>
        /// <value>
        /// A read-only <see cref="NameList"/> containing the display names of all entities that are
        /// affected by the message event represented by the <see cref="MessageInstruction"/>. The
        /// default is an empty collection.</value>
        /// <remarks><para>
        /// <b>Names</b> never returns a null reference. This property holds the values of all
        /// "name" XML elements.
        /// </para><para>
        /// Depending on the specific message event, <b>Names</b> might contain the display names of
        /// factions, entity classes, entities, attributes, or resources.
        /// </para><note type="implementnotes">
        /// The name of the faction <em>primarily</em> affected by the event is derived from the
        /// <see cref="Instruction.Id"/> property, and should not be repeated in the <b>Names</b>
        /// collection.</note></remarks>

        public NameList Names {
            [DebuggerStepThrough]
            get { return this._names.AsReadOnly(); }
        }

        #endregion
        #region XmlSerializable Members
        #region ReadXmlElements

        /// <summary>
        /// Reads XML element data into the <see cref="MessageInstruction"/> object using the
        /// specified <see cref="XmlReader"/>.</summary>
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

                case "details":
                    Details = XmlUtility.ReadTextElement(reader);
                    return true;

                case "name": {
                    string name = XmlUtility.ReadTextElement(reader);

                    if (!String.IsNullOrEmpty(name))
                        this._names.Add(name);

                    return true;
                }

                default: return false;
            }
        }

        #endregion
        #region WriteXmlElements

        /// <summary>
        /// Writes all current data of the <see cref="MessageInstruction"/> object that is
        /// serialized to nested XML elements to the specified <see cref="XmlWriter"/>.</summary>
        /// <param name="writer">
        /// The <see cref="XmlWriter"/> to which to write.</param>
        /// <remarks>
        /// Please refer to class <see cref="XmlSerializable"/> for a complete description of this
        /// method.</remarks>

        protected override void WriteXmlElements(XmlWriter writer) {
            base.WriteXmlElements(writer);

            if (!String.IsNullOrEmpty(Details))
                writer.WriteElementString("details", Details);

            foreach (string name in Names)
                writer.WriteElementString("name", name);
        }

        #endregion
        #endregion
   }
}
