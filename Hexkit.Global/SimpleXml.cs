using System;
using System.Xml;
using System.Windows;
using System.Windows.Media;

using Tektosyne.Geometry;
using Tektosyne.Windows;
using Tektosyne.Xml;

namespace Hexkit.Global {

    /// <summary>
    /// Provides methods for reading and writing simple XML data.</summary>

    public static class SimpleXml {
        #region MaxPointIValue

        /// <summary>
        /// The maximum value for <see cref="PointI"/> components of map coordinates, in either
        /// dimension, including the locations of <see cref="RectI"/> instances.</summary>
        /// <remarks>
        /// This value must equal the "maxInclusive" property of the simple type "coordinate"
        /// defined in <see cref="FilePaths.ScenarioSchema"/>. The minimum coordinate is always
        /// zero.</remarks>

        public const int MaxPointIValue = 9999;

        #endregion
        #region MaxSizeIValue

        /// <summary>
        /// The maximum value for <see cref="SizeI"/> components of map coordinates, in either
        /// dimension, including the sizes of <see cref="RectI"/> instances.</summary>
        /// <remarks>
        /// This value must equal the "maxInclusive" property of the simple type "extension" defined
        /// in <see cref="FilePaths.ScenarioSchema"/>. The minimum size is always one.</remarks>

        public const int MaxSizeIValue = 10000;

        #endregion
        #region ReadColor

        /// <summary>
        /// Reads XML data into an instance of <see cref="Color"/> using the specified <see
        /// cref="XmlReader"/>.</summary>
        /// <param name="reader">
        /// The <see cref="XmlReader"/> from which to read.</param>
        /// <returns>
        /// A <see cref="Color"/> instance created from the XML data provided by <paramref
        /// name="reader"/>.</returns>
        /// <exception cref="XmlException">
        /// An error occurred while parsing the XML data provided by <paramref name="reader"/>.
        /// </exception>
        /// <exception cref="System.Xml.Schema.XmlSchemaException">
        /// <paramref name="reader"/> is an <see cref="XmlValidatingReader"/>, and the XML data did
        /// not conform to the current schema.</exception>
        /// <remarks><para>
        /// <b>ReadColor</b> attempts to read three attributes named "red", "green", and "blue" from
        /// the specified <paramref name="reader"/>, each containing a <see cref="Byte"/> value.
        /// </para><para>
        /// If an attribute is present, its value is read into the corresponding color channel of
        /// the returned <see cref="Color"/> instance; otherwise, the channel defaults to zero. The
        /// alpha channel is always fully opaque (255).</para></remarks>

        public static Color ReadColor(XmlReader reader) {
            byte red = 0, green = 0, blue = 0;

            XmlUtility.ReadAttributeAsByte(reader, "red", ref red);
            XmlUtility.ReadAttributeAsByte(reader, "green", ref green);
            XmlUtility.ReadAttributeAsByte(reader, "blue", ref blue);

            return Color.FromArgb(255, red, green, blue);
        }

        #endregion
        #region ReadColorVector

        /// <summary>
        /// Reads XML data into an instance of <see cref="ColorVector"/> using the specified <see
        /// cref="XmlReader"/>.</summary>
        /// <param name="reader">
        /// The <see cref="XmlReader"/> from which to read.</param>
        /// <returns>
        /// A <see cref="ColorVector"/> instance created from the XML data provided by <paramref
        /// name="reader"/>.</returns>
        /// <exception cref="XmlException">
        /// An error occurred while parsing the XML data provided by <paramref name="reader"/>.
        /// </exception>
        /// <exception cref="System.Xml.Schema.XmlSchemaException">
        /// <paramref name="reader"/> is an <see cref="XmlValidatingReader"/>, and the XML data did
        /// not conform to the current schema.</exception>
        /// <remarks><para>
        /// <b>ReadColor</b> attempts to read three attributes named "red", "green", and "blue" from
        /// the specified <paramref name="reader"/>, each containing an <see cref="Int16"/> value.
        /// </para><para>
        /// If an attribute is present, its value is read into the corresponding color channel of
        /// the returned <see cref="ColorVector"/> instance; otherwise, the channel defaults to
        /// zero.</para></remarks>

        public static ColorVector ReadColorVector(XmlReader reader) {
            short red = 0, green = 0, blue = 0;

            XmlUtility.ReadAttributeAsInt16(reader, "red", ref red);
            XmlUtility.ReadAttributeAsInt16(reader, "green", ref green);
            XmlUtility.ReadAttributeAsInt16(reader, "blue", ref blue);

            return new ColorVector(red, green, blue);
        }

        #endregion
        #region ReadIdentifier

        /// <summary>
        /// Reads XML data into an identifier using the specified <see cref="XmlReader"/>.
        /// </summary>
        /// <param name="reader">
        /// The <see cref="XmlReader"/> from which to read.</param>
        /// <returns>
        /// An identifier read from the XML data provided by <paramref name="reader"/>.</returns>
        /// <exception cref="XmlException">
        /// An error occurred while parsing the XML data provided by <paramref name="reader"/>.
        /// </exception>
        /// <exception cref="System.Xml.Schema.XmlSchemaException">
        /// <paramref name="reader"/> is an <see cref="XmlValidatingReader"/>, and the XML data did
        /// not conform to the current schema.</exception>
        /// <remarks><para>
        /// <b>ReadIdentifier</b> attempts to read an attribute named "id" from the specified
        /// <paramref name="reader"/>, containing an identifier.
        /// </para><para>
        /// If an attribute is present, <b>ReadIdentifier</b> interns and returns its value;
        /// otherwise, a null reference.</para></remarks>

        public static string ReadIdentifier(XmlReader reader) {

            string id = reader["id"];
            if (id != null) id = String.Intern(id);
            return id;
        }

        #endregion
        #region ReadPointI

        /// <summary>
        /// Reads XML data into an instance of <see cref="PointI"/> using the specified <see
        /// cref="XmlReader"/>.</summary>
        /// <param name="reader">
        /// The <see cref="XmlReader"/> from which to read.</param>
        /// <returns>
        /// A <see cref="PointI"/> instance created from the XML data provided by <paramref
        /// name="reader"/>.</returns>
        /// <exception cref="XmlException">
        /// An error occurred while parsing the XML data provided by <paramref name="reader"/>.
        /// </exception>
        /// <exception cref="System.Xml.Schema.XmlSchemaException">
        /// <paramref name="reader"/> is an <see cref="XmlValidatingReader"/>, and the XML data did
        /// not conform to the current schema.</exception>
        /// <remarks><para>
        /// <b>ReadPointI</b> attempts to read two attributes named "x" and "y" from the specified
        /// <paramref name="reader"/>, each containing an <see cref="Int32"/> value.
        /// </para><para>
        /// If an attribute is present, its value is read into the corresponding component of the
        /// returned <see cref="PointI"/> instance; otherwise, the component defaults to zero.
        /// </para></remarks>

        public static PointI ReadPointI(XmlReader reader) {
            int x = 0, y = 0;

            XmlUtility.ReadAttributeAsInt32(reader, "x", ref x);
            XmlUtility.ReadAttributeAsInt32(reader, "y", ref y);

            return new PointI(x, y);
        }

        #endregion
        #region ReadSizeI

        /// <summary>
        /// Reads XML data into an instance of <see cref="SizeI"/> using the specified <see
        /// cref="XmlReader"/>.</summary>
        /// <param name="reader">
        /// The <see cref="XmlReader"/> from which to read.</param>
        /// <returns>
        /// A <see cref="SizeI"/> instance created from the XML data provided by <paramref
        /// name="reader"/>.</returns>
        /// <exception cref="XmlException">
        /// An error occurred while parsing the XML data provided by <paramref name="reader"/>.
        /// </exception>
        /// <exception cref="System.Xml.Schema.XmlSchemaException">
        /// <paramref name="reader"/> is an <see cref="XmlValidatingReader"/>, and the XML data did
        /// not conform to the current schema.</exception>
        /// <remarks><para>
        /// <b>ReadSizeI</b> attempts to read two attributes named "width" and "height" from the
        /// specified <paramref name="reader"/>, each containing an <see cref="Int32"/> value.
        /// </para><para>
        /// If an attribute is present, its value is read into the corresponding component of the
        /// returned <see cref="SizeI"/> instance; otherwise, the component defaults to one
        /// (<em>not</em> zero!).</para></remarks>

        public static SizeI ReadSizeI(XmlReader reader) {
            int width = 1, height = 1;

            XmlUtility.ReadAttributeAsInt32(reader, "width", ref width);
            XmlUtility.ReadAttributeAsInt32(reader, "height", ref height);

            return new SizeI(width, height);
        }

        #endregion
        #region ReadRectI

        /// <summary>
        /// Reads XML data into an instance of <see cref="RectI"/> using the specified <see
        /// cref="XmlReader"/>.</summary>
        /// <param name="reader">
        /// The <see cref="XmlReader"/> from which to read.</param>
        /// <returns>
        /// A <see cref="RectI"/> instance created from the XML data provided by <paramref
        /// name="reader"/>.</returns>
        /// <exception cref="XmlException">
        /// An error occurred while parsing the XML data provided by <paramref name="reader"/>.
        /// </exception>
        /// <exception cref="System.Xml.Schema.XmlSchemaException">
        /// <paramref name="reader"/> is an <see cref="XmlValidatingReader"/>, and the XML data did
        /// not conform to the current schema.</exception>
        /// <remarks><para>
        /// <b>ReadRectI</b> attempts to read four attributes named "left", "top", "width", and
        /// "height" from the specified <paramref name="reader"/>, each containing an <see
        /// cref="Int32"/> value.
        /// </para><para>
        /// If an attribute is present, its value is read into the corresponding component of the
        /// returned <see cref="RectI"/> instance; otherwise, left and top default to zero, and
        /// width and height default to one.</para></remarks>

        public static RectI ReadRectI(XmlReader reader) {
            int left = 0, top = 0, width = 1, height = 1;

            XmlUtility.ReadAttributeAsInt32(reader, "left", ref left);
            XmlUtility.ReadAttributeAsInt32(reader, "top", ref top);
            XmlUtility.ReadAttributeAsInt32(reader, "width", ref width);
            XmlUtility.ReadAttributeAsInt32(reader, "height", ref height);

            return new RectI(left, top, width, height);
        }

        #endregion
        #region ReadWpfRect

        /// <summary>
        /// Reads XML data into an instance of <see cref="Rect"/> using the specified <see
        /// cref="XmlReader"/>.</summary>
        /// <param name="reader">
        /// The <see cref="XmlReader"/> from which to read.</param>
        /// <returns>
        /// A <see cref="Rect"/> instance created from the XML data provided by <paramref
        /// name="reader"/>.</returns>
        /// <exception cref="XmlException">
        /// An error occurred while parsing the XML data provided by <paramref name="reader"/>.
        /// </exception>
        /// <exception cref="System.Xml.Schema.XmlSchemaException">
        /// <paramref name="reader"/> is an <see cref="XmlValidatingReader"/>, and the XML data did
        /// not conform to the current schema.</exception>
        /// <remarks><para>
        /// <b>ReadRect</b> attempts to read four attributes named "left", "top", "width", and
        /// "height" from the specified <paramref name="reader"/>, each containing a <see
        /// cref="Double"/> value.
        /// </para><para>
        /// If an attribute is present, its value is read into the corresponding component of the
        /// returned <see cref="Rect"/> instance; otherwise, left and top default to zero, and width
        /// and height default to one.</para></remarks>

        public static Rect ReadWpfRect(XmlReader reader) {
            double left = 0, top = 0, width = 1, height = 1;

            XmlUtility.ReadAttributeAsDouble(reader, "left", ref left);
            XmlUtility.ReadAttributeAsDouble(reader, "top", ref top);
            XmlUtility.ReadAttributeAsDouble(reader, "width", ref width);
            XmlUtility.ReadAttributeAsDouble(reader, "height", ref height);

            return new Rect(left, top, width, height);
        }

        #endregion
        #region WriteColor

        /// <summary>
        /// Writes the data of the specified <see cref="Color"/> instance to the specified <see
        /// cref="XmlWriter"/>.</summary>
        /// <param name="writer">
        /// The <see cref="XmlWriter"/> to which to write.</param>
        /// <param name="color">
        /// The <see cref="Color"/> instance whose data to write to <paramref name="writer"/>.
        /// </param>
        /// <remarks>
        /// <b>WriteColor</b> writes the color channels of the specified <paramref name="color"/> as
        /// XML attributes named "red", "green", and "blue" to the specified <paramref
        /// name="writer"/>. The alpha channel is not written.</remarks>

        public static void WriteColor(XmlWriter writer, Color color) {

            writer.WriteAttributeString("red", XmlConvert.ToString(color.R));
            writer.WriteAttributeString("green", XmlConvert.ToString(color.G));
            writer.WriteAttributeString("blue", XmlConvert.ToString(color.B));
        }

        #endregion
        #region WriteColorVector

        /// <summary>
        /// Writes the data of the specified <see cref="ColorVector"/> instance to the specified
        /// <see cref="XmlWriter"/>.</summary>
        /// <param name="writer">
        /// The <see cref="XmlWriter"/> to which to write.</param>
        /// <param name="vector">
        /// The <see cref="ColorVector"/> instance whose data to write to <paramref name="writer"/>.
        /// </param>
        /// <remarks>
        /// <b>WriteColor</b> writes the color channels of the specified <paramref name="vector"/>
        /// as XML attributes named "red", "green", and "blue" to the specified <paramref
        /// name="writer"/>.</remarks>

        public static void WriteColorVector(XmlWriter writer, ColorVector vector) {

            writer.WriteAttributeString("red", XmlConvert.ToString(vector.R));
            writer.WriteAttributeString("green", XmlConvert.ToString(vector.G));
            writer.WriteAttributeString("blue", XmlConvert.ToString(vector.B));
        }

        #endregion
        #region WriteIdentifier

        /// <summary>
        /// Writes the specified identifier to the specified <see cref="XmlWriter"/>, using the
        /// specified element name.</summary>
        /// <param name="writer">
        /// The <see cref="XmlWriter"/> to which to write.</param>
        /// <param name="name">
        /// The name of the XML element to write to <paramref name="writer"/>.</param>
        /// <param name="id">
        /// The identifier to write to <paramref name="writer"/>.</param>
        /// <remarks><para>
        /// <b>WriteIdentifier</b> writes an empty XML element to the specified <paramref
        /// name="writer"/> that has the specified <paramref name="name"/> and contains a single
        /// attribute named "id" whose value equals the specified <paramref name="id"/>.
        /// </para><para>
        /// <b>WriteIdentifier</b> does nothing if <paramref name="id"/> is a null reference or an
        /// empty string.</para></remarks>

        public static void WriteIdentifier(XmlWriter writer, string name, string id) {

            if (!String.IsNullOrEmpty(id)) {
                writer.WriteStartElement(name);
                writer.WriteAttributeString("id", id);
                writer.WriteEndElement();
            }
        }

        #endregion
        #region WritePointI

        /// <summary>
        /// Writes the data of the specified <see cref="PointI"/> instance to the specified <see
        /// cref="XmlWriter"/>.</summary>
        /// <param name="writer">
        /// The <see cref="XmlWriter"/> to which to write.</param>
        /// <param name="point">
        /// The <see cref="PointI"/> instance whose data to write to <paramref name="writer"/>.
        /// </param>
        /// <remarks>
        /// <b>WritePointI</b> writes the components of the specified <paramref name="point"/> as
        /// XML attributes named "x" and "y" to the specified <paramref name="writer"/>.</remarks>

        public static void WritePointI(XmlWriter writer, PointI point) {

            writer.WriteAttributeString("x", XmlConvert.ToString(point.X));
            writer.WriteAttributeString("y", XmlConvert.ToString(point.Y));
        }

        #endregion
        #region WriteSizeI

        /// <summary>
        /// Writes the data of the specified <see cref="SizeI"/> instance to the specified <see
        /// cref="XmlWriter"/>.</summary>
        /// <param name="writer">
        /// The <see cref="XmlWriter"/> to which to write.</param>
        /// <param name="size">
        /// The <see cref="SizeI"/> instance whose data to write to <paramref name="writer"/>.
        /// </param>
        /// <remarks>
        /// <b>WriteSizeI</b> writes the components of the specified <paramref name="size"/> as
        /// attributes named "width" and "height" to the specified <paramref name="writer"/>.
        /// </remarks>

        public static void WriteSizeI(XmlWriter writer, SizeI size) {

            writer.WriteAttributeString("width", XmlConvert.ToString(size.Width));
            writer.WriteAttributeString("height", XmlConvert.ToString(size.Height));
        }

        #endregion
        #region WriteRectI

        /// <summary>
        /// Writes the data of the specified <see cref="RectI"/> instance to the specified <see
        /// cref="XmlWriter"/>.</summary>
        /// <param name="writer">
        /// The <see cref="XmlWriter"/> to which to write.</param>
        /// <param name="rectangle">
        /// The <see cref="RectI"/> value whose data to write to <paramref name="writer"/>.</param>
        /// <remarks>
        /// <b>WriteRectI</b> writes the components of the specified <paramref name="rectangle"/> as
        /// XML attributes named "left", "top", "width", and "height" to the specified <paramref
        /// name="writer"/>. The width and height components are only written if different from the
        /// default value of one.</remarks>

        public static void WriteRectI(XmlWriter writer, RectI rectangle) {

            writer.WriteAttributeString("left", XmlConvert.ToString(rectangle.Left));
            writer.WriteAttributeString("top", XmlConvert.ToString(rectangle.Top));

            if (rectangle.Width != 1)
                writer.WriteAttributeString("width", XmlConvert.ToString(rectangle.Width));

            if (rectangle.Height != 1)
                writer.WriteAttributeString("height", XmlConvert.ToString(rectangle.Height));
        }

        #endregion
        #region WriteWpfRect

        /// <summary>
        /// Writes the data of the specified <see cref="Rect"/> instance to the specified <see
        /// cref="XmlWriter"/>.</summary>
        /// <param name="writer">
        /// The <see cref="XmlWriter"/> to which to write.</param>
        /// <param name="rectangle">
        /// The <see cref="Rect"/> value whose data to write to <paramref name="writer"/>.</param>
        /// <remarks>
        /// <b>WriteRect</b> writes the components of the specified <paramref name="rectangle"/> as
        /// XML attributes named "left", "top", "width", and "height" to the specified <paramref
        /// name="writer"/>. The width and height components are only written if different from the
        /// default value of one.</remarks>

        public static void WriteWpfRect(XmlWriter writer, Rect rectangle) {

            writer.WriteAttributeString("left", XmlConvert.ToString(rectangle.Left));
            writer.WriteAttributeString("top", XmlConvert.ToString(rectangle.Top));

            if (rectangle.Width != 1)
                writer.WriteAttributeString("width", XmlConvert.ToString(rectangle.Width));

            if (rectangle.Height != 1)
                writer.WriteAttributeString("height", XmlConvert.ToString(rectangle.Height));
        }

        #endregion
    }
}
