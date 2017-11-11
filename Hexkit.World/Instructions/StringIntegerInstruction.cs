using System;
using System.Diagnostics;
using System.Xml;

using Tektosyne.Xml;
using Hexkit.Global;

namespace Hexkit.World.Instructions {

    /// <summary>
    /// Represents an instruction that takes a <see cref="String"/> parameter and an <see
    /// cref="Int32"/> parameter.</summary>
    /// <remarks><para>
    /// <b>StringIntegerInstruction</b> adds the <see cref="StringIntegerInstruction.Value"/>
    /// instruction parameter to the functionality provided by the <see cref="StringInstruction"/>
    /// class.
    /// </para><para>
    /// Derived classes should add the remaining members and any other functionality that is
    /// required for specific instructions.
    /// </para><para>
    /// <b>StringIntegerInstruction</b> corresponds to the complex XML type
    /// "stringIntegerInstruction" defined in <see cref="FilePaths.SessionSchema"/>.
    /// </para></remarks>

    public abstract class StringIntegerInstruction: StringInstruction {
        #region StringIntegerInstruction()

        /// <overloads>
        /// Initializes a new instance of the <see cref="StringIntegerInstruction"/> class.
        /// </overloads>
        /// <summary>
        /// Initializes a new instance of the <see cref="StringIntegerInstruction"/> class with
        /// default properties.</summary>

        protected StringIntegerInstruction(): base() { }

        #endregion
        #region StringIntegerInstruction(String, String, Int32)

        /// <summary>
        /// Initializes a new instance of the <see cref="StringIntegerInstruction"/> class with the
        /// specified identifier, <see cref="String"/> value, and <see cref="Int32"/> value.
        /// </summary>
        /// <param name="id">
        /// The initial value for the <see cref="Instruction.Id"/> property.</param>
        /// <param name="text">
        /// The initial value for the <see cref="StringInstruction.Text"/> property.</param>
        /// <param name="value">
        /// The initial value for the <see cref="Value"/> property.</param>

        protected StringIntegerInstruction(string id, string text, int value): base(id, text) {
            this._value = value;
        }

        #endregion
        #region Private Fields

        // property backers
        private int _value;

        #endregion
        #region Value

        /// <summary>
        /// Gets the <see cref="Int32"/> value associated with the <see cref="Instruction"/>.
        /// </summary>
        /// <value>
        /// The <see cref="Int32"/> value associated with the <see
        /// cref="StringIntegerInstruction"/>. The default is zero.</value>
        /// <remarks>
        /// <b>Value</b> holds the value of the "value" XML attribute.</remarks>

        public int Value {
            [DebuggerStepThrough]
            get { return this._value; }
        }

        #endregion
        #region XmlSerializable Members
        #region ReadXmlAttributes

        /// <summary>
        /// Reads XML attribute data into the <see cref="StringIntegerInstruction"/> object using
        /// the specified <see cref="XmlReader"/>.</summary>
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

            XmlUtility.ReadAttributeAsInt32(reader, "value", ref this._value);
        }

        #endregion
        #region WriteXmlAttributes

        /// <summary>
        /// Writes all current data of the <see cref="StringIntegerInstruction"/> object that is
        /// serialized to XML attributes to the specified <see cref="XmlWriter"/>.</summary>
        /// <param name="writer">
        /// The <see cref="XmlWriter"/> to which to write.</param>
        /// <remarks>
        /// Please refer to class <see cref="XmlSerializable"/> for a complete description of this
        /// method.</remarks>

        protected override void WriteXmlAttributes(XmlWriter writer) {
            base.WriteXmlAttributes(writer);

            writer.WriteAttributeString("value", XmlConvert.ToString(Value));
        }

        #endregion
        #endregion
    }
}
