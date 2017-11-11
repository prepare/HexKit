using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Xml;

using Tektosyne;
using Tektosyne.Collections;
using Tektosyne.Geometry;
using Hexkit.Global;

namespace Hexkit.Scenario {
    #region Type Aliases

    using CompassList = ListEx<Compass>;
    using ImageFilePair = KeyValuePair<String, ImageFile>;

    #endregion

    /// <summary>
    /// Describes a single frame of an <see cref="EntityImage"/>.</summary>
    /// <remarks>
    /// <b>ImageFrame</b> is serialized to the XML element "frame" defined in <see
    /// cref="FilePaths.ScenarioSchema"/>.</remarks>

    public sealed class ImageFrame: ScenarioElement, ICloneable {
        #region ImageFrame()

        /// <overloads>
        /// Initializes a new instance of the <see cref="ImageFrame"/> class.</overloads>
        /// <summary>
        /// Initializes a new instance of the <see cref="ImageFrame"/> class with default
        /// properties.</summary>

        public ImageFrame() { }

        #endregion
        #region ImageFrame(ImageFrame)

        /// <summary>
        /// Initializes a new instance of the <see cref="ImageFrame"/> class with property values
        /// copied from the specified instance.</summary>
        /// <param name="frame">
        /// The <see cref="ImageFrame"/> instance whose property values are copied to the new
        /// instance.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="frame"/> is a null reference.</exception>
        /// <remarks>
        /// This constructor is called by <see cref="Clone"/> to perform a deep copy of the
        /// specified <paramref name="frame"/>, including a deep copy of all mutable objects that
        /// are owned by the <paramref name="frame"/>.</remarks>

        public ImageFrame(ImageFrame frame) {
            if (frame == null)
                ThrowHelper.ThrowArgumentNullException("frame");

            this._bounds = frame._bounds;
            this._connections.AddRange(frame._connections);
            this._source = frame._source;
        }

        #endregion
        #region ImageFrame(String)

        /// <summary>
        /// Initializes a new instance of the <see cref="ImageFrame"/> class with the specified
        /// source file identifier.</summary>
        /// <param name="sourceId">
        /// The initial value for the <see cref="ImageFilePair.Key"/> component of the <see
        /// cref="Source"/> property.</param>

        internal ImageFrame(string sourceId) {
            if (sourceId != null) sourceId = String.Intern(sourceId);
            this._source = new ImageFilePair(sourceId, null);
        }

        #endregion
        #region Private Fields

        // property backers
        private RectI _bounds = new RectI(0, 0, 1, 1);
        private CompassList _connections = new CompassList(0);
        private ImageFilePair _source = new ImageFilePair();

        #endregion
        #region Bounds

        /// <summary>
        /// Gets or sets the bounds of the frame within an image file.</summary>
        /// <value>
        /// A <see cref="RectI"/> indicating the area within an <see cref="ImageFile"/> that is
        /// covered by the <see cref="ImageFrame"/>. The default is a <see cref="RectI.Location"/>
        /// of (0,0) and a <see cref="RectI.Size"/> of (1,1).</value>
        /// <exception cref="ArgumentException">
        /// The property is set to a value that contains a <see cref="RectI.Location"/> with a
        /// negative coordinate, or a <see cref="RectI.Size"/> with a non-positive component.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// The property is set, and <see cref="ApplicationInfo.IsEditor"/> is <c>false</c>.
        /// </exception>
        /// <remarks>
        /// <b>Bounds</b> holds the values of the "left", "top", "width", and "height" XML
        /// attributes.</remarks>

        public RectI Bounds {
            [DebuggerStepThrough]
            get { return this._bounds; }
            [DebuggerStepThrough]
            set {
                ApplicationInfo.CheckEditor();

                if (value.Left < 0)
                    ThrowHelper.ThrowArgumentException(
                        "value.Left", Tektosyne.Strings.ArgumentNegative);

                if (value.Top < 0)
                    ThrowHelper.ThrowArgumentException(
                        "value.Top", Tektosyne.Strings.ArgumentNegative);

                if (value.Width <= 0)
                    ThrowHelper.ThrowArgumentException(
                        "value.Width", Tektosyne.Strings.ArgumentNotPositive);

                if (value.Height <= 0)
                    ThrowHelper.ThrowArgumentException(
                        "value.Height", Tektosyne.Strings.ArgumentNotPositive);

                this._bounds = value;
            }
        }

        #endregion
        #region Connections

        /// <summary>
        /// Gets a list of all visual connections to neighboring map elements.</summary>
        /// <value>
        /// A <see cref="CompassList"/> containing the <see cref="Compass"/> values of any
        /// neighboring map elements to which the <see cref="ImageFrame"/> provides a visual
        /// connection. The default is an empty collection.</value>
        /// <remarks><para>
        /// <b>Connections</b> never returns a null reference. The collection is read-only if <see
        /// cref="ApplicationInfo.IsEditor"/> is <c>false</c>.
        /// </para><para>
        /// This property holds the values of the "connections" XML attribute. <b>Connections</b>
        /// indicates all edges and vertices of this <see cref="ImageFrame"/> that show a visual
        /// connection to a neighboring <see cref="AreaSection.MapGrid"/> polygon, such as roads or
        /// rivers leading to the edge or vertex.
        /// </para><para>
        /// The <b>Connections</b> values are not used by Hexkit or by the default rules. A rule
        /// script may define semantics for visually connected map elements, however.
        /// </para></remarks>

        public CompassList Connections {
            [DebuggerStepThrough]
            get {
                return (ApplicationInfo.IsEditor ?
                    this._connections : this._connections.AsReadOnly());
            }
        }

        #endregion
        #region Source

        /// <summary>
        /// Gets or sets the <see cref="ImageFile"/> that provides bitmap data for the <see
        /// cref="ImageFrame"/>.</summary>
        /// <value>
        /// A <see cref="ImageFilePair"/> that maps an <see cref="ImageFile.Id"/> string to the
        /// corresponding <see cref="ImageFile"/>. The default is a pair of null references.</value>
        /// <exception cref="InvalidOperationException">
        /// The property is set, and <see cref="ApplicationInfo.IsEditor"/> is <c>false</c>.
        /// </exception>
        /// <remarks><para>
        /// <b>Source.Key</b> holds the value of the "id" attribute of the last "source" XML element
        /// that precedes the "frame" element corresponding to this <see cref="ImageFrame"/>.
        /// <b>Source.Value</b> holds a null reference until successful validation.
        /// </para><para>
        /// The <see cref="ImageFile"/> object returned by this property specifies the physical
        /// image file providing bitmap data for this <see cref="ImageFrame"/>. The <see
        /// cref="Bounds"/> coordinates are relative to this file.</para></remarks>

        public ImageFilePair Source {
            [DebuggerStepThrough]
            get { return this._source; }
            [DebuggerStepThrough]
            set {
                ApplicationInfo.CheckEditor();
                this._source = value;
            }
        }

        #endregion
        #region ICloneable Members

        /// <summary>
        /// Creates a new <see cref="ImageFrame"/> object that is a deep copy of the current
        /// instance.</summary>
        /// <returns>
        /// A new <see cref="ImageFrame"/> object that is a deep copy of the current instance.
        /// </returns>
        /// <remarks>
        /// <b>Clone</b> calls the <see cref="ImageFrame(ImageFrame)"/> copy constructor with this
        /// <see cref="ImageFrame"/> object.</remarks>

        public object Clone() {
            return new ImageFrame(this);
        }

        #endregion
        #region ScenarioElement Members
        #region ProcessIdentifier

        /// <summary>
        /// Counts, changes, or deletes all occurrences of the specified identifier in the <see
        /// cref="ImageFrame"/>.</summary>
        /// <param name="oldId">
        /// The identifier to count, change, or delete.</param>
        /// <param name="newId"><para>
        /// The same value as <paramref name="oldId"/> to count the occurrences of <paramref
        /// name="oldId"/>.
        /// </para><para>-or-</para><para>
        /// A different value than <paramref name="oldId"/> to change all occurrences of <paramref
        /// name="oldId"/> to <paramref name="newId"/>.
        /// </para><para>-or-</para><para>
        /// A null reference to delete all elements with <paramref name="oldId"/>.</para></param>
        /// <returns>
        /// The number of occurrences of <paramref name="oldId"/> in the <see cref="ImageFrame"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="oldId"/> is a null reference.</exception>
        /// <remarks><para>
        /// <b>ProcessIdentifier</b> processes <see cref="ImageFrame"/> properties as follows:
        /// </para><list type="table"><listheader>
        /// <term>Property</term><description>Processing</description>
        /// </listheader><item>
        /// <term><see cref="Source"/></term><description>By key only</description>
        /// </item></list></remarks>

        internal override int ProcessIdentifier(string oldId, string newId) {

            // process source file ID
            ImageFilePair pair = this._source;
            int count = CollectionsUtility.ProcessKey(ref pair, oldId, newId);
            this._source = pair;

            return count;
        }

        #endregion
        #region Validate

        /// <summary>
        /// Validates the current data of the <see cref="ImageFrame"/>.</summary>
        /// <exception cref="XmlException">
        /// Validation failed, and <see cref="ApplicationInfo.IsEditor"/> is <c>false</c>.
        /// </exception>
        /// <remarks><para>
        /// <b>Validate</b> processes <see cref="ImageFrame"/> properties as follows:
        /// </para><list type="table"><listheader>
        /// <term>Property</term><description>Processing</description>
        /// </listheader><item>
        /// <term><see cref="Source"/></term>
        /// <description>Check identifier and set reference</description>
        /// </item></list><para>
        /// Checks are only performed if <see cref="ApplicationInfo.IsEditor"/> is <c>false</c>.
        /// </para></remarks>

        internal override void Validate() {

            // set reference for source file ID
            if (String.IsNullOrEmpty(Source.Key))
                this._source = new ImageFilePair("", null);
            else {
                // try to locate file in file table
                ImageFile file;
                MasterSection.Instance.Images.ImageFiles.TryGetValue(Source.Key, out file);
                this._source = new ImageFilePair(Source.Key, file);

                // check for nonexistent file if not editing
                if (!ApplicationInfo.IsEditor && file == null)
                    ThrowHelper.ThrowXmlExceptionWithFormat(
                        Global.Strings.XmlSourceInvalid, Source.Key);
            }
        }

        #endregion
        #endregion
        #region XmlSerializable Members
        #region ConstXmlName

        /// <summary>
        /// The name of the XML element associated with the <see cref="ImageFrame"/> class.
        /// </summary>
        /// <remarks>
        /// <b>ConstXmlName</b> holds the value "frame", indicating the XML element in <see
        /// cref="FilePaths.ScenarioSchema"/> whose data is managed by the <see cref="ImageFrame"/>
        /// class.</remarks>

        public const string ConstXmlName = "frame";

        #endregion
        #region ReadXmlAttributes

        /// <summary>
        /// Reads XML attribute data into the <see cref="ImageFrame"/> object using the specified
        /// <see cref="XmlReader"/>.</summary>
        /// <param name="reader">
        /// The <see cref="XmlReader"/> from which to read.</param>
        /// <exception cref="XmlException">
        /// An error occurred while parsing the XML data provided by <paramref name="reader"/>.
        /// </exception>
        /// <exception cref="System.Xml.Schema.XmlSchemaException">
        /// <paramref name="reader"/> is an <see cref="XmlValidatingReader"/>, and the XML data did
        /// not conform to <see cref="FilePaths.ScenarioSchema"/>.</exception>
        /// <remarks>
        /// Please refer to class <see cref="XmlSerializable"/> for a complete description of this
        /// method.</remarks>

        protected override void ReadXmlAttributes(XmlReader reader) {
            this._bounds = SimpleXml.ReadRectI(reader);

            // read Compass values for any Connections
            string attribute = reader["connections"];
            if (attribute != null)
                foreach (string token in attribute.Split(null))
                    this._connections.Add((Compass) Enum.Parse(typeof(Compass), token));
        }

        #endregion
        #region WriteXmlAttributes

        /// <summary>
        /// Writes all current data of the <see cref="ImageFrame"/> object that is serialized to XML
        /// attributes to the specified <see cref="XmlWriter"/>.</summary>
        /// <param name="writer">
        /// The <see cref="XmlWriter"/> to which to write.</param>
        /// <remarks>
        /// Please refer to class <see cref="XmlSerializable"/> for a complete description of this
        /// method.</remarks>

        protected override void WriteXmlAttributes(XmlWriter writer) {
            SimpleXml.WriteRectI(writer, Bounds);

            // write Compass names for any Connections
            StringBuilder sb = null;
            foreach (Compass connection in Connections) {
                if (sb == null)
                    sb = new StringBuilder();
                else
                    sb.Append(" ");

                sb.Append(connection.ToString());
            }

            if (sb != null)
                writer.WriteAttributeString("connections", sb.ToString());
        }

        #endregion
        #endregion
    }
}
