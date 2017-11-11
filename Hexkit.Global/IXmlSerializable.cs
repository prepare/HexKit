using System;
using System.Xml;

namespace Hexkit.Global {

    /// <summary>
    /// Allows the XML serialization and deserialization of an object.</summary>
    /// <remarks><para>
    /// <b>IXmlSerializable</b> defines a simple mechanism that implementing classes can use to
    /// serialize any desired object data to an <see cref="XmlWriter"/>, and deserialize object data
    /// from an <see cref="XmlReader"/>.
    /// </para><para>
    /// The .NET Framework provides several built-in serialization mechanisms, all of which are
    /// unfortunately unsuited to the task of creating compact, human-readable XML files:
    /// </para><list type="bullet"><item>
    /// <b>BinaryFormatter</b> creates binary files, not XML files.
    /// </item><item>
    /// <b>SoapFormatter</b> and the newer <b>NetDataContractSerializer</b> are intended for
    /// remoting, like <b>BinaryFormatter</b>, and create very large XML files that are
    /// incomprehensible both to human readers and to other XML parsers.
    /// </item><item>
    /// <b>XmlSerializer</b> is very slow and requires a zoo of attribute annotations if anything
    /// but the default behavior is desired. Alternatively, it can use the standard <see
    /// cref="System.Xml.Serialization.IXmlSerializable"/> interface which does almost exactly what
    /// we need but requires an obsolete <b>GetSchema</b> method and lacks our <see
    /// cref="IXmlSerializable.XmlName"/> property.
    /// </item></list><para>
    /// Our <b>IXmlSerializable</b> interface adopts the signatures of the standard interface’s two
    /// useful methods, <see cref="System.Xml.Serialization.IXmlSerializable.ReadXml"/> and <see
    /// cref="System.Xml.Serialization.IXmlSerializable.WriteXml"/>, to give the client complete
    /// control over the serialization and deserialization process. The <b>XmlReader</b> and
    /// <b>XmlWriter</b> classes are so efficient and so easy to use that this approach produces
    /// simpler and faster code than any of the built-in serialization mechanisms.
    /// </para><para>
    /// (Some of the above information was taken from Dino Esposito, <em>Applied XML Programming for
    /// Microsoft .NET,</em> Microsoft Press 2003. See page 488 and Chapter 11 in general.)
    /// </para></remarks>

    public interface IXmlSerializable {
        #region XmlName

        /// <summary>
        /// Gets the name of the XML element associated with the <see cref="IXmlSerializable"/>
        /// instance.</summary>
        /// <value>
        /// A <see cref="String"/> indicating the name of the XML element to which the data of the
        /// <see cref="IXmlSerializable"/> instance is serialized.</value>
        /// <remarks><para>
        /// <b>XmlName</b> specifies the name of the XML element (<b>XmlNodeType.Element</b>) that
        /// is expected by <see cref="ReadXml"/> and created by <see cref="WriteXml"/>.
        /// </para><para>
        /// The value of <b>XmlName</b> is usually identical for all instances of a particular class
        /// that implements <see cref="IXmlSerializable"/>. Unfortunately, C# interfaces cannot
        /// specify static methods or properties, so <b>XmlName</b> must be specified as an instance
        /// member. Implementing classes that require static XML element names should provide a
        /// constant field or static property called <b>ConstXmlName</b> that returns the same value
        /// as <b>XmlName</b>.</para></remarks>

        string XmlName { get; }

        #endregion
        #region ReadXml

        /// <summary>
        /// Reads XML data into the <see cref="IXmlSerializable"/> instance using the specified <see
        /// cref="XmlReader"/>.</summary>
        /// <param name="reader">
        /// The <see cref="XmlReader"/> from which to read.</param>
        /// <exception cref="XmlException">
        /// An error occurred while parsing the XML data provided by <paramref name="reader"/>.
        /// </exception>
        /// <exception cref="System.Xml.Schema.XmlSchemaException">
        /// <paramref name="reader"/> is an <see cref="XmlValidatingReader"/>, and the XML data did
        /// not conform to the applicable XML schema.</exception>
        /// <remarks><para>
        /// <b>ReadXml</b> replaces the data of the <see cref="IXmlSerializable"/> instance with any
        /// matching data read from the specified <paramref name="reader"/>. Any instance data that
        /// <paramref name="reader"/> fails to supply is left unchanged. The caller should
        /// initialize all instance data to default values before invoking <b>ReadXml</b>, and
        /// validate the new instance data after the call returns.
        /// </para><para>
        /// The current node of the specified <paramref name="reader"/> must be either an element
        /// start tag (<b>XmlNodeType.Element</b>) named <see cref="XmlName"/>, or a node from which
        /// such a start tag can be reached by a single call to <see
        /// cref="XmlReader.MoveToContent"/>. <b>ReadXml</b> may either ignore unexpected data or
        /// throw an exception.</para></remarks>

        void ReadXml(XmlReader reader);

        #endregion
        #region WriteXml

        /// <summary>
        /// Writes the current data of the <see cref="IXmlSerializable"/> instance to the specified
        /// <see cref="XmlWriter"/>.</summary>
        /// <param name="writer">
        /// The <see cref="XmlWriter"/> to which to write.</param>
        /// <remarks><para>
        /// <b>WriteXml</b> writes all data of the <see cref="IXmlSerializable"/> instance for which
        /// an XML representation is defined to the specified <paramref name="writer"/>.
        /// </para><para>
        /// Writing begins with an XML element start tag (<b>XmlNodeType.Element</b>) named <see
        /// cref="XmlName"/>, continues with any required attributes, text, or sub-nodes, and ends
        /// with an XML element end tag (<b>XmlNodeType.EndElement</b>) matching the initial start
        /// tag. An empty element may be written if no data is available.</para></remarks>

        void WriteXml(XmlWriter writer);

        #endregion
    }
}
