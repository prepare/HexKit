using System;
using System.Diagnostics;
using System.Xml;

using Tektosyne.Xml;
using Hexkit.Global;

namespace Hexkit.World.Instructions {

    /// <summary>
    /// Represents an instruction that takes a <see cref="String"/> parameter.</summary>
    /// <remarks><para>
    /// <b>StringInstruction</b> adds the <see cref="StringInstruction.Text"/> instruction parameter
    /// to the functionality provided by the <see cref="Instruction"/> class.
    /// </para><para>
    /// Derived classes should add the remaining members and any other functionality that is
    /// required for specific instructions.
    /// </para><para>
    /// <b>StringInstruction</b> corresponds to the the complex XML type "stringInstruction" defined
    /// in <see cref="FilePaths.SessionSchema"/>.</para></remarks>

    public abstract class StringInstruction: Instruction {
        #region StringInstruction()

        /// <overloads>
        /// Initializes a new instance of the <see cref="StringInstruction"/> class.</overloads>
        /// <summary>
        /// Initializes a new instance of the <see cref="StringInstruction"/> class with default
        /// properties.</summary>

        protected StringInstruction(): base() { }

        #endregion
        #region StringInstruction(String, String)

        /// <summary>
        /// Initializes a new instance of the <see cref="StringInstruction"/> class with the
        /// specified identifier and <see cref="String"/> value.</summary>
        /// <param name="id">
        /// The initial value for the <see cref="Instruction.Id"/> property.</param>
        /// <param name="text">
        /// The initial value for the <see cref="Text"/> property.</param>

        protected StringInstruction(string id, string text): base(id) {
            if (text != null)
                this._text = String.Intern(text);
        }

        #endregion
        #region Private Fields

        // backer for Text property
        private string _text;

        #endregion
        #region Text

        /// <summary>
        /// Gets the <see cref="String"/> value associated with the <see cref="Instruction"/>.
        /// </summary>
        /// <value>
        /// The <see cref="String"/> value associated with the <see cref="StringInstruction"/>. The
        /// default is a null reference.</value>
        /// <remarks>
        /// <b>Text</b> holds the value of the "text" XML attribute.</remarks>

        public string Text {
            [DebuggerStepThrough]
            get { return this._text; }
        }

        #endregion
        #region XmlSerializable Members
        #region ReadXmlAttributes

        /// <summary>
        /// Reads XML attribute data into the <see cref="StringInstruction"/> object using the
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

            XmlUtility.ReadAttributeAsString(reader, "text", ref this._text);
        }

        #endregion
        #region WriteXmlAttributes

        /// <summary>
        /// Writes all current data of the <see cref="StringInstruction"/> object that is serialized
        /// to XML attributes to the specified <see cref="XmlWriter"/>.</summary>
        /// <param name="writer">
        /// The <see cref="XmlWriter"/> to which to write.</param>
        /// <remarks>
        /// Please refer to class <see cref="XmlSerializable"/> for a complete description of this
        /// method.</remarks>

        protected override void WriteXmlAttributes(XmlWriter writer) {
            base.WriteXmlAttributes(writer);

            if (!String.IsNullOrEmpty(Text))
                writer.WriteAttributeString("text", Text);
        }

        #endregion
        #endregion
    }
}
