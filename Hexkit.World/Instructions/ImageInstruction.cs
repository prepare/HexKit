using System;
using System.Diagnostics;
using System.Xml;

using Tektosyne.Collections;
using Tektosyne.Geometry;
using Tektosyne.Xml;

using Hexkit.Global;
using Hexkit.Scenario;

namespace Hexkit.World.Instructions {

    using PointIList = ListEx<PointI>;

    /// <summary>
    /// Represents an image event instruction.</summary>
    /// <remarks><para>
    /// <b>ImageInstruction</b> adds the <see cref="ImageInstruction.Delay"/> and <see
    /// cref="ImageInstruction.Sites"/> instruction parameters to the functionality provided by the
    /// <see cref="Instruction"/> class.
    /// </para><para>
    /// Derived classes should add the remaining members and any other functionality that is
    /// required for specific instructions.
    /// </para><para>
    /// <b>ImageInstruction</b> corresponds to the the complex XML type "imageInstruction" defined
    /// in <see cref="FilePaths.SessionSchema"/>.</para></remarks>

    public abstract class ImageInstruction: Instruction {
        #region ImageInstruction()

        /// <overloads>
        /// Initializes a new instance of the <see cref="ImageInstruction"/> class.</overloads>
        /// <summary>
        /// Initializes a new instance of the <see cref="ImageInstruction"/> class with default
        /// properties.</summary>

        protected ImageInstruction(): base() {
            this._sites = new PointIList();
        }

        #endregion
        #region ImageInstruction(String, PointI[], Int32, Boolean)

        /// <summary>
        /// Initializes a new instance of the <see cref="ImageInstruction"/> class with the
        /// specified <see cref="Scenario.EntityClass"/> identifier, map locations, and display
        /// delay.</summary>
        /// <param name="classId">
        /// The initial value for the <see cref="Instruction.Id"/> property, indicating the <see
        /// cref="Scenario.EntityClass"/> whose image to show.</param>
        /// <param name="sites">
        /// An <see cref="Array"/> containing the initial elements for the <see cref="Sites"/>
        /// collection.</param>
        /// <param name="delay">
        /// The initial value for the <see cref="Delay"/> property.</param>

        protected ImageInstruction(string classId, PointI[] sites, int delay): base(classId) {
            this._delay = delay;

            if (sites != null && sites.Length > 0)
                this._sites = new PointIList(sites);
            else
                this._sites = new PointIList(0);
        }

        #endregion
        #region Private Fields

        // property backers
        private readonly PointIList _sites;
        private int _delay;

        #endregion
        #region Category

        /// <summary>
        /// Gets the category of the <see cref="Instruction"/>.</summary>
        /// <value>
        /// The category <see cref="InstructionCategory.Event"/>, indicating that the <see
        /// cref="ImageInstruction"/> class represents a map view event.</value>

        public override InstructionCategory Category {
            [DebuggerStepThrough]
            get { return InstructionCategory.Event; }
        }

        #endregion
        #region EntityClass

        /// <summary>
        /// Gets the <see cref="Scenario.EntityClass"/> whose image to show.</summary>
        /// <value>
        /// The <see cref="Scenario.EntityClass"/> defined by the current <see
        /// cref="EntitySection"/> whose <see cref="Scenario.EntityClass.Id"/> string equals the
        /// value of the <see cref="Instruction.Id"/> property.</value>
        /// <remarks>
        /// <b>EntityClass</b> returns a null reference if <see cref="Instruction.Id"/> is a null
        /// reference or an empty string, or if the Entities section does not contain a matching
        /// <see cref="Scenario.EntityClass"/>.</remarks>

        public EntityClass EntityClass {
            get {
                return (String.IsNullOrEmpty(Id) ? null :
                    MasterSection.Instance.Entities.GetEntity(Id));
            }
        }

        #endregion
        #region Delay

        /// <summary>
        /// Gets the duration of the map view event.</summary>
        /// <value>
        /// The duration, in milliseconds, for which the map view event affects each <see
        /// cref="Sites"/> element. The default is zero.</value>
        /// <remarks>
        /// If <b>Delay</b> is non-positive, a default value of 250 msec is assumed. This property
        /// holds the value of the "delay" XML attribute.</remarks>

        public int Delay {
            [DebuggerStepThrough]
            get { return this._delay; }
        }

        #endregion
        #region Sites

        /// <summary>
        /// Gets a list of all map locations affected by the map view event.</summary>
        /// <value>
        /// A read-only <see cref="PointIList"/> containing the coordinates of each <see
        /// cref="Site"/> that is affected by the map view event represented by the <see
        /// cref="ImageInstruction"/>. The default is an empty collection.</value>
        /// <remarks>
        /// <b>Sites</b> never returns a null reference. This property holds the values of all
        /// "site" XML elements.</remarks>

        public PointIList Sites {
            [DebuggerStepThrough]
            get { return this._sites; }
        }

        #endregion
        #region XmlSerializable Members
        #region ReadXmlAttributes

        /// <summary>
        /// Reads XML attribute data into the <see cref="ImageInstruction"/> object using the
        /// specified <see cref="XmlReader"/>.</summary>
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

            XmlUtility.ReadAttributeAsInt32(reader, "delay", ref this._delay);
        }

        #endregion
        #region ReadXmlElements

        /// <summary>
        /// Reads XML element data into the <see cref="ImageInstruction"/> object using the
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

                case "site":
                    this._sites.Add(SimpleXml.ReadPointI(reader));
                    return true;

                default: return false;
            }
        }

        #endregion
        #region WriteXmlAttributes

        /// <summary>
        /// Writes all current data of the <see cref="ImageInstruction"/> object that is serialized
        /// to XML attributes to the specified <see cref="XmlWriter"/>.</summary>
        /// <param name="writer">
        /// The <see cref="XmlWriter"/> to which to write.</param>
        /// <remarks>
        /// Please refer to class <see cref="XmlSerializable"/> for a complete description of this
        /// method.</remarks>

        protected override void WriteXmlAttributes(XmlWriter writer) {
            base.WriteXmlAttributes(writer);

            if (Delay > 0)
                writer.WriteAttributeString("delay", XmlConvert.ToString(Delay));
        }

        #endregion
        #region WriteXmlElements

        /// <summary>
        /// Writes all current data of the <see cref="ImageInstruction"/> object that is serialized
        /// to nested XML elements to the specified <see cref="XmlWriter"/>.</summary>
        /// <param name="writer">
        /// The <see cref="XmlWriter"/> to which to write.</param>
        /// <remarks>
        /// Please refer to class <see cref="XmlSerializable"/> for a complete description of this
        /// method.</remarks>

        protected override void WriteXmlElements(XmlWriter writer) {
            base.WriteXmlElements(writer);

            foreach (PointI site in Sites) {
                writer.WriteStartElement("site");
                SimpleXml.WritePointI(writer, site);
                writer.WriteEndElement();
            }
        }

        #endregion
        #endregion
    }
}
