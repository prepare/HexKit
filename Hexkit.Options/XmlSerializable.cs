using System;
using System.Reflection;
using System.Xml;

using Tektosyne;
using Tektosyne.Xml;
using Hexkit.Global;

namespace Hexkit.Options {

    /// <summary>
    /// Provides XML serialization and deserialization to and from elements defined in <see
    /// cref="FilePaths.OptionsSchema"/>.</summary>
    /// <remarks><para>
    /// The abstract base class <b>XmlSerializable</b> is modelled on the interface <see
    /// cref="IXmlSerializable"/>, but offers useful default implementations and the correct
    /// (internal) level of visibility for XML serialization tasks.
    /// </para><para>
    /// <b>XmlSerializable</b> adds the following qualifications to the contract defined by <see
    /// cref="IXmlSerializable"/>:
    /// </para><list type="bullet"><item>
    /// <see cref="XmlSerializable.XmlName"/> specifies an XML element defined in <see
    /// cref="FilePaths.OptionsSchema"/>.
    /// </item><item>
    /// <see cref="XmlSerializable.ReadXml"/> expects an XML document or fragment that conforms to
    /// <see cref="FilePaths.OptionsSchema"/>.
    /// </item><item>
    /// <see cref="XmlSerializable.WriteXml"/> creates an XML document or fragment that conforms to
    /// <see cref="FilePaths.OptionsSchema"/>.</item></list></remarks>

    public abstract class XmlSerializable {
        #region XmlName

        /// <summary>
        /// Gets the name of the XML element associated with the <see cref="XmlSerializable"/>
        /// object.</summary>
        /// <value>
        /// The name of the XML element to which the data of this <see cref="XmlSerializable"/>
        /// object is serialized. The default is the value of the constant field
        /// <b>ConstXmlName</b>.</value>
        /// <exception cref="NotImplementedException">
        /// The property was accessed on a type that does not define a public constant field named
        /// <b>ConstXmlName</b>.</exception>
        /// <remarks><para>
        /// <b>XmlName</b> specifies the name of the XML element defined in <see
        /// cref="FilePaths.OptionsSchema"/> that is expected by <see cref="ReadXml"/> and created
        /// by <see cref="WriteXml"/>.
        /// </para><para>
        /// Derived classes may override <b>XmlName</b> to return the desired value. Alternatively,
        /// derived classes may define a public constant field named <b>ConstXmlName</b> that holds
        /// their XML element name. The base class implementation of <b>XmlName</b> will return the
        /// value of this field, which is obtained via reflection.</para></remarks>

        internal virtual string XmlName {
            get {
                FieldInfo field = GetType().GetField("ConstXmlName",
                    BindingFlags.Public | BindingFlags.Static);

                if (field == null)
                    ThrowHelper.ThrowNotImplementedException(Tektosyne.Strings.PropertyDerived);

                return (string) field.GetValue(null);
            }
        }

        #endregion
        #region ReadXml

        /// <summary>
        /// Reads XML data into the <see cref="XmlSerializable"/> object using the specified <see
        /// cref="XmlReader"/>.</summary>
        /// <param name="reader">
        /// The <see cref="XmlReader"/> from which to read.</param>
        /// <exception cref="XmlException">
        /// An error occurred while parsing the XML data provided by <paramref name="reader"/>.
        /// </exception>
        /// <exception cref="System.Xml.Schema.XmlSchemaException">
        /// <paramref name="reader"/> is an <see cref="XmlValidatingReader"/>, and the XML data did
        /// not conform to <see cref="FilePaths.OptionsSchema"/>.</exception>
        /// <remarks><para>
        /// <b>ReadXml</b> replaces the data of this <see cref="XmlSerializable"/> object with any
        /// matching data read from the specified <paramref name="reader"/>. Any instance data that
        /// <paramref name="reader"/> fails to supply is left unchanged.
        /// </para><para>
        /// The current node of the specified <paramref name="reader"/> must be either an element
        /// start tag named <see cref="XmlName"/>, or a node from which such a start tag can be
        /// reached by a single call to <see cref="XmlReader.MoveToContent"/>. The provided XML data
        /// is assumed to conform to <see cref="FilePaths.OptionsSchema"/>.
        /// </para><para>
        /// <b>ReadXml</b> reads the contents of the <b>XmlName</b> element by calling the <see
        /// cref="ReadXmlAttributes"/> and <see cref="ReadXmlElements"/> methods, which should be
        /// overridden by derived classes to read any desired object data.
        /// </para><para>
        /// <b>ReadXml</b> itself should be overridden only to perform additional actions before or
        /// after the XML deserialization itself has taken place. When overriding <b>ReadXml</b>,
        /// call this base class implementation to perform the deserialization.</para></remarks>

        internal virtual void ReadXml(XmlReader reader) {
            XmlUtility.MoveToStartElement(reader, XmlName);

            ReadXmlAttributes(reader);
            if (reader.IsEmptyElement) return;

            while (reader.Read() && reader.IsStartElement())
                if (!ReadXmlElements(reader)) {
                    // skip to end tag of unknown element
                    XmlUtility.MoveToEndElement(reader);
                }
        }

        #endregion
        #region ReadXmlAttributes

        /// <summary>
        /// Reads XML attribute data into the <see cref="XmlSerializable"/> object using the
        /// specified <see cref="XmlReader"/>.</summary>
        /// <param name="reader">
        /// The <see cref="XmlReader"/> from which to read.</param>
        /// <exception cref="XmlException">
        /// An error occurred while parsing the XML data provided by <paramref name="reader"/>.
        /// </exception>
        /// <exception cref="System.Xml.Schema.XmlSchemaException">
        /// <paramref name="reader"/> is an <see cref="XmlValidatingReader"/>, and the XML data did
        /// not conform to <see cref="FilePaths.OptionsSchema"/>.</exception>
        /// <remarks><para>
        /// <b>ReadXmlAttributes</b> replaces the data of this <see cref="XmlSerializable"/> object
        /// with any matching data read from the XML attributes of the specified <paramref
        /// name="reader"/>. Any instance data that <paramref name="reader"/> fails to supply is
        /// left unchanged.
        /// </para><para>
        /// The current node of the specified <paramref name="reader"/> must be the start tag of an
        /// XML element named <see cref="XmlName"/> defined in <see
        /// cref="FilePaths.OptionsSchema"/>. <b>ReadXmlAttributes</b> does not check the element
        /// name of the start tag or move the <paramref name="reader"/>.
        /// </para><para>
        /// The base class implementation of <b>ReadXmlAttributes</b> does nothing. Derived classes
        /// should override this method to read any desired object data from the specified <paramref
        /// name="reader"/>.</para></remarks>

        protected virtual void ReadXmlAttributes(XmlReader reader) { }

        #endregion
        #region ReadXmlElements

        /// <summary>
        /// Reads XML element data into the <see cref="XmlSerializable"/> object using the specified
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
        /// not conform to <see cref="FilePaths.OptionsSchema"/>.</exception>
        /// <remarks><para>
        /// <b>ReadXmlElements</b> replaces the data of this <see cref="XmlSerializable"/> object
        /// with any matching data read from the current XML element of the specified <paramref
        /// name="reader"/>. Any instance data that the <paramref name="reader"/> fails to supply is
        /// left unchanged.
        /// </para><para>
        /// The current node of the specified <paramref name="reader"/> must be a nested element tag
        /// of the XML element named <see cref="XmlName"/> defined in <see
        /// cref="FilePaths.OptionsSchema"/>. <b>ReadXmlElements</b> reads only the first XML
        /// element for which a match is found, and does not move the <paramref name="reader"/>
        /// otherwise.
        /// </para><para>
        /// The base class implementation of <b>ReadXmlElements</b> always returns <c>false</c>.
        /// Derived classes should override this method to read any desired object data from the
        /// specified <paramref name="reader"/>.</para></remarks>

        protected virtual bool ReadXmlElements(XmlReader reader) {
            return false;
        }

        #endregion
        #region WriteXml

        /// <summary>
        /// Writes the current data of the <see cref="XmlSerializable"/> object to the specified
        /// <see cref="XmlWriter"/>.</summary>
        /// <param name="writer">
        /// The <see cref="XmlWriter"/> to which to write.</param>
        /// <remarks><para>
        /// <b>WriteXml</b> writes all data of this <see cref="XmlSerializable"/> object for which
        /// <see cref="FilePaths.OptionsSchema"/> defines an XML representation to the specified
        /// <paramref name="writer"/>. The resulting data stream is an XML fragment comprising an
        /// XML element named <see cref="XmlName"/> which conforms to the corresponding element of
        /// <b>OptionsSchema</b>.
        /// </para><para>
        /// <b>WriteXml</b> writes the contents of the <b>XmlName</b> element by calling the <see
        /// cref="WriteXmlAttributes"/> and <see cref="WriteXmlElements"/> methods, which should be
        /// overridden by derived classes to write any desired object data.
        /// </para><para>
        /// <b>WriteXml</b> itself should be overridden only to change the way the start element
        /// itself is written, e.g. to include an XML declaration or to specify a default namespace.
        /// </para></remarks>

        internal virtual void WriteXml(XmlWriter writer) {
            writer.WriteStartElement(XmlName);

            WriteXmlAttributes(writer);
            WriteXmlElements(writer);

            writer.WriteEndElement();
        }

        #endregion
        #region WriteXmlAttributes

        /// <summary>
        /// Writes all current data of the <see cref="XmlSerializable"/> object that is serialized
        /// to XML attributes to the specified <see cref="XmlWriter"/>.</summary>
        /// <param name="writer">
        /// The <see cref="XmlWriter"/> to which to write.</param>
        /// <remarks><para>
        /// <b>WriteXmlAttributes</b> writes all data of this <see cref="XmlSerializable"/> object
        /// for which <see cref="FilePaths.OptionsSchema"/> defines an XML attribute to the
        /// specified <paramref name="writer"/>.
        /// </para><para>
        /// The resulting data stream is an XML fragment which conforms to the attributes of the XML
        /// element named <see cref="XmlName"/> defined in <b>OptionsSchema</b>.
        /// </para><para>
        /// The base class implementation of <b>WriteXmlAttributes</b> does nothing. Derived classes
        /// should override this method to write any desired object data to the specified <paramref
        /// name="writer"/>.</para></remarks>

        protected virtual void WriteXmlAttributes(XmlWriter writer) { }

        #endregion
        #region WriteXmlElements

        /// <summary>
        /// Writes all current data of the <see cref="XmlSerializable"/> object that is serialized
        /// to nested XML elements to the specified <see cref="XmlWriter"/>.</summary>
        /// <param name="writer">
        /// The <see cref="XmlWriter"/> to which to write.</param>
        /// <remarks><para>
        /// <b>WriteXmlElements</b> writes all data of this <see cref="XmlSerializable"/> object for
        /// which <see cref="FilePaths.OptionsSchema"/> defines a nested XML element to the
        /// specified <paramref name="writer"/>.
        /// </para><para>
        /// The resulting data stream is an XML fragment which conforms to the nested element tags
        /// of the XML element named <see cref="XmlName"/> defined in <b>OptionsSchema</b>.
        /// </para><para>
        /// The base class implementation of <b>WriteXmlElements</b> does nothing. Derived classes
        /// should override this method to write any desired object data to the specified <paramref
        /// name="writer"/>.</para></remarks>

        protected virtual void WriteXmlElements(XmlWriter writer) { }

        #endregion
    }
}
