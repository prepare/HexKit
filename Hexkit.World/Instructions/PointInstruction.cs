using System;
using System.Diagnostics;
using System.Xml;

using Tektosyne.Geometry;
using Tektosyne.Xml;
using Hexkit.Global;

namespace Hexkit.World.Instructions {

    /// <summary>
    /// Represents an instruction that takes a <see cref="PointI"/> parameter.</summary>
    /// <remarks><para>
    /// <b>PointInstruction</b> adds the <see cref="PointInstruction.Location"/> instruction
    /// parameter to the functionality provided by the <see cref="Instruction"/> class.
    /// </para><para>
    /// Derived classes should add the remaining members and any other functionality that is
    /// required for specific instructions.
    /// </para><para>
    /// <b>PointInstruction</b> corresponds to the the complex XML type "pointInstruction" defined
    /// in <see cref="FilePaths.SessionSchema"/>.</para></remarks>

    public abstract class PointInstruction: Instruction {
        #region PointInstruction()

        /// <overloads>
        /// Initializes a new instance of the <see cref="PointInstruction"/> class.</overloads>
        /// <summary>
        /// Initializes a new instance of the <see cref="PointInstruction"/> class with default
        /// properties.</summary>

        protected PointInstruction(): base() {
            Location = Site.InvalidLocation;
        }

        #endregion
        #region PointInstruction(String, PointI)

        /// <summary>
        /// Initializes a new instance of the <see cref="PointInstruction"/> class with the
        /// specified identifier and <see cref="Site"/> coordinates.</summary>
        /// <param name="id">
        /// The initial value for the <see cref="Instruction.Id"/> property.</param>
        /// <param name="location">
        /// The initial value for the <see cref="Location"/> property.</param>

        protected PointInstruction(string id, PointI location): base(id) {
            Location = location;
        }

        #endregion
        #region Location

        /// <summary>
        /// Gets the coordinates of the <see cref="Site"/> associated with the <see
        /// cref="Instruction"/>.</summary>
        /// <value>
        /// The coordinates of the <see cref="Site"/> associated with the <see
        /// cref="PointInstruction"/>. The default is <see cref="Site.InvalidLocation"/>.</value>
        /// <remarks>
        /// <b>Location</b> holds the values of the "x" and "y" XML attributes.</remarks>

        public PointI Location { get; private set; }

        #endregion
        #region XmlSerializable Members
        #region ReadXmlAttributes

        /// <summary>
        /// Reads XML attribute data into the <see cref="PointInstruction"/> object using the
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

            int x = Location.X, y = Location.Y;

            XmlUtility.ReadAttributeAsInt32(reader, "x", ref x);
            XmlUtility.ReadAttributeAsInt32(reader, "y", ref y);

            Location = new PointI(x, y);
        }

        #endregion
        #region WriteXmlAttributes

        /// <summary>
        /// Writes all current data of the <see cref="PointInstruction"/> object that is serialized
        /// to XML attributes to the specified <see cref="XmlWriter"/>.</summary>
        /// <param name="writer">
        /// The <see cref="XmlWriter"/> to which to write.</param>
        /// <remarks>
        /// Please refer to class <see cref="XmlSerializable"/> for a complete description of this
        /// method.</remarks>

        protected override void WriteXmlAttributes(XmlWriter writer) {
            base.WriteXmlAttributes(writer);

            writer.WriteAttributeString("x", XmlConvert.ToString(Location.X));
            writer.WriteAttributeString("y", XmlConvert.ToString(Location.Y));
        }

        #endregion
        #endregion
    }
}
