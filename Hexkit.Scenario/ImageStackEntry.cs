using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml;

using Tektosyne;
using Tektosyne.Collections;
using Tektosyne.Geometry;
using Tektosyne.Windows;
using Tektosyne.Xml;
using Hexkit.Global;

namespace Hexkit.Scenario {

    using EntityImagePair = KeyValuePair<String, EntityImage>;

    /// <summary>
    /// Represents an entry within the image stack of an <see cref="EntityClass"/>.</summary>
    /// <remarks><para>
    /// <b>ImageStackEntry</b> corresponds to the XML element "imageStack" defined in <see
    /// cref="FilePaths.ScenarioSchema"/>.
    /// </para><para>
    /// The <see cref="EntityClass.ImageStack"/> of an <see cref="EntityClass"/> is an ordered
    /// sequence of <b>ImageStackEntry</b> instances which associate <see cref="EntityImage"/>
    /// objects with display parameters. The same <see cref="EntityImage"/> may be referenced by
    /// multiple stack entries, with identical or different display parameters.</para></remarks>

    public sealed class ImageStackEntry: ScenarioElement, ICloneable {
        #region ImageStackEntry()

        /// <overloads>
        /// Initializes a new instance of the <see cref="ImageStackEntry"/> class.</overloads>
        /// <summary>
        /// Initializes a new instance of the <see cref="ImageStackEntry"/> class with
        /// default properties.</summary>

        public ImageStackEntry() { }

        #endregion
        #region ImageStackEntry(EntityImage)

        /// <summary>
        /// Initializes a new instance of the <see cref="ImageStackEntry"/> class with the specified
        /// <see cref="EntityImage"/>.</summary>
        /// <param name="image">
        /// The initial value for the <see cref="Image"/> property.</param>
        /// <remarks>
        /// The specified <paramref name="image"/> may be a null reference, in which case <see
        /// cref="Image"/> remains a pair of null references. The remaining properties are always
        /// initialized to their default values.</remarks>

        public ImageStackEntry(EntityImage image) {
            if (image != null)
                this._image = new EntityImagePair(image.Id, image);
        }

        #endregion
        #region ImageStackEntry(ImageStackEntry)

        /// <summary>
        /// Initializes a new instance of the <see cref="ImageStackEntry"/> class with property
        /// values copied from the specified instance.</summary>
        /// <param name="entry">
        /// The <see cref="ImageStackEntry"/> instance whose property values are copied to the new
        /// instance.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="entry"/> is a null reference.</exception>
        /// <remarks>
        /// This constructor is called by <see cref="Clone"/> to perform a deep copy of the
        /// specified <paramref name="entry"/>, including a deep copy of all mutable objects that
        /// are owned by the <paramref name="entry"/>.</remarks>

        public ImageStackEntry(ImageStackEntry entry) {
            if (entry == null)
                ThrowHelper.ThrowArgumentNullException("entry");

            this._image = entry._image;
            this._colorShift = entry._colorShift;
            this._offset = entry._offset;
            this._scalingVector = entry._scalingVector;

            this._singleFrame = entry._singleFrame;
            this._useUnconnected = entry._useUnconnected;
            this._useUnscaled = entry._useUnscaled;
        }

        #endregion
        #region Private Fields

        // property backers
        private EntityImagePair _image;
        private ColorVector _colorShift;
        private PointI _offset, _scalingVector;
        private int _singleFrame = -1;
        private bool _useUnconnected, _useUnscaled;

        #endregion
        #region ColorShift

        /// <summary>
        /// Gets or sets the color channel shift for the associated <see cref="Image"/>.</summary>
        /// <value>
        /// A <see cref="ColorVector"/> value indicating the amounts by which to shift the sRGB
        /// color channels of all pixels within the associated <see cref="Image"/>. The default is
        /// <see cref="ColorVector.Empty"/>.</value>
        /// <exception cref="InvalidOperationException">
        /// The property is set, and <see cref="ApplicationInfo.IsEditor"/> is <c>false</c>.
        /// </exception>
        /// <remarks><para>
        /// <b>ColorShift</b> holds the contents of the nested "colorShift" XML element.
        /// </para><para>
        /// The values of all <b>ColorShift</b> components assume full opacity. For translucent <see
        /// cref="Image"/> pixels, the absolute magnitude of each component decreases in proportion
        /// with the alpha channel of each pixel.</para></remarks>

        public ColorVector ColorShift {
            [DebuggerStepThrough]
            get { return this._colorShift; }
            set {
                ApplicationInfo.CheckEditor();
                this._colorShift = value;
            }
        }

        #endregion
        #region Image

        /// <summary>
        /// Gets or sets the <see cref="EntityImage"/> that the <see cref="ImageStackEntry"/>
        /// represents.</summary>
        /// <value>
        /// A <see cref="EntityImagePair"/> that maps an <see cref="EntityImage.Id"/> string to the
        /// corresponding <see cref="EntityImage"/>. The default is a pair of null references.
        /// </value>
        /// <exception cref="InvalidOperationException">
        /// The property is set, and <see cref="ApplicationInfo.IsEditor"/> is <c>false</c>.
        /// </exception>
        /// <remarks>
        /// <b>Image.Key</b> holds the value of the "id" XML attribute, and <b>Image.Value</b> holds
        /// a null reference until successful validation.</remarks>

        public EntityImagePair Image {
            [DebuggerStepThrough]
            get { return this._image; }
            [DebuggerStepThrough]
            set {
                ApplicationInfo.CheckEditor();
                this._image = value;
            }
        }

        #endregion
        #region IsImageUnconnected

        /// <summary>
        /// Gets a value indicating whether the associated <see cref="Image"/> is unconnected.
        /// </summary>
        /// <value>
        /// <c>true</c> if <see cref="Image"/> holds a null reference, or if all its <see
        /// cref="EntityImage.Frames"/> or the selected <see cref="SingleFrame"/>, if valid, contain
        /// an empty <see cref="ImageFrame.Connections"/> list; otherwise, <c>false</c>.</value>
        /// <remarks>
        /// <b>IsImageUnconnected</b> checks only the selected <see cref="SingleFrame"/> if valid,
        /// otherwise all <see cref="EntityImage.Frames"/> of the associated <see cref="Image"/>.
        /// </remarks>

        public bool IsImageUnconnected {
            get {
                EntityImage image = Image.Value;
                if (image == null) return true;

                if (SingleFrame >= 0) {
                    int index = SingleFrame % image.Frames.Count;
                    return (image.Frames[index].Connections.Count == 0);
                }

                foreach (ImageFrame frame in image.Frames)
                    if (frame.Connections.Count > 0) return false;

                return true;
            }
        }

        #endregion
        #region IsImageUnscaled

        /// <summary>
        /// Gets a value indicating whether the associated <see cref="Image"/> is unscaled.
        /// </summary>
        /// <value>
        /// <c>true</c> if <see cref="Image"/> holds a null reference, or if its <see
        /// cref="EntityImage.ScalingX"/> and <see cref="EntityImage.ScalingY"/> values both equal
        /// <see cref="ImageScaling.None"/>; otherwise, <c>false</c>.</value>
        /// <remarks>
        /// <b>IsImageUnscaled</b> does not consider the value of the associated <see
        /// cref="ScalingVector"/>.</remarks>

        public bool IsImageUnscaled {
            get {
                EntityImage image = Image.Value;
                if (image == null) return true;

                return (image.ScalingX == ImageScaling.None && 
                    image.ScalingY == ImageScaling.None);
            }
        }

        #endregion
        #region Offset

        /// <summary>
        /// Gets or sets the pixel offset for the associated <see cref="Image"/>.</summary>
        /// <value>
        /// The pixel offset for the associated <see cref="Image"/>. The default is <see
        /// cref="PointI.Empty"/>.</value>
        /// <exception cref="InvalidOperationException">
        /// The property is set, and <see cref="ApplicationInfo.IsEditor"/> is <c>false</c>.
        /// </exception>
        /// <remarks><para>
        /// <b>Offset</b> holds the contents of the nested "offset" XML element.
        /// </para><para>
        /// <b>Offset</b> indicates a shift relative to the central position within a polygon of the 
        /// current <see cref="AreaSection.MapGrid"/> at the default zoom level.</para></remarks>

        public PointI Offset {
            [DebuggerStepThrough]
            get { return this._offset; }
            [DebuggerStepThrough]
            set {
                ApplicationInfo.CheckEditor();
                this._offset = value;
            }
        }

        #endregion
        #region ScalingVector

        /// <summary>
        /// Gets or sets the scaling vector for the associated <see cref="Image"/>.</summary>
        /// <value>
        /// The scaling vector for the associated <see cref="Image"/>. The default is <see
        /// cref="PointI.Empty"/>, indicating that no scaling vector should be applied.</value>
        /// <exception cref="InvalidOperationException">
        /// The property is set, and <see cref="ApplicationInfo.IsEditor"/> is <c>false</c>.
        /// </exception>
        /// <remarks><para>
        /// <b>ScalingVector</b> holds the contents of the nested "scaling" XML element.
        /// </para><para>
        /// Unless it equals <see cref="PointI.Empty"/>, the <b>ScalingVector</b> is applied to the
        /// transformation matrix used when drawing the <see cref="Image"/>.
        /// </para><para>
        /// Hexkit does not currently support arbitrary scaling vectors. When set, the 
        /// <b>ScalingVector</b> is therefore normalized using <see cref="NormalizeScalingVector"/>;
        /// please see there for details.</para></remarks>

        public PointI ScalingVector {
            [DebuggerStepThrough]
            get { return this._scalingVector; }
            set {
                ApplicationInfo.CheckEditor();
                this._scalingVector = NormalizeScalingVector(value);
            }
        }

        #endregion
        #region SingleFrame

        /// <summary>
        /// Gets or sets the index of the single <see cref="ImageFrame"/> defined by the associated
        /// <see cref="Image"/> that the <see cref="ImageStackEntry"/> represents.</summary>
        /// <value><para>
        /// The zero-based index of the single <see cref="EntityImage.Frames"/> element defined by
        /// the associated <see cref="Image"/> that the <see cref="ImageStackEntry"/> represents.
        /// </para><para>-or-</para><para>
        /// A negative value if the <see cref="ImageStackEntry"/> represents the entire <see
        /// cref="EntityImage.Frames"/> collection. The default is -1.</para></value>
        /// <exception cref="InvalidOperationException">
        /// The property is set, and <see cref="ApplicationInfo.IsEditor"/> is <c>false</c>.
        /// </exception>
        /// <remarks><para>
        /// <b>SingleFrame</b> returns -1 when set to any negative value. This property holds the
        /// value of the "frame" XML attribute.
        /// </para><para>
        /// <b>SingleFrame</b> allows clients to use one specific <see cref="ImageFrame"/> rather
        /// than the entire <see cref="EntityImage.Frames"/> collection defined by the associated
        /// <see cref="Image"/>. This effectively disables animation for the <see cref="Image"/>,
        /// and may also reduce the total <see cref="EntityClass.FrameCount"/> of the owning <see
        /// cref="EntityClass"/>.</para></remarks>

        public int SingleFrame {
            [DebuggerStepThrough]
            get { return this._singleFrame; }
            set {
                ApplicationInfo.CheckEditor();
                this._singleFrame = (value < 0 ? -1 : value);
            }
        }

        #endregion
        #region UseUnconnected

        /// <summary>
        /// Gets or sets a value indicating whether any <see cref="ImageFrame.Connections"/> defined
        /// by the associated <see cref="Image"/> should be ignored.</summary>
        /// <value>
        /// <c>true</c> if the <see cref="ImageFrame.Connections"/> defined by the <see
        /// cref="EntityImage.Frames"/> of the associated <see cref="Image"/> should be ignored;
        /// otherwise, <c>false</c>. The default is <c>false</c>.</value>
        /// <exception cref="InvalidOperationException">
        /// The property is set, and <see cref="ApplicationInfo.IsEditor"/> is <c>false</c>.
        /// </exception>
        /// <remarks>
        /// <b>UseUnconnected</b> holds the value of the "unconnected" XML attribute.</remarks>

        public bool UseUnconnected {
            [DebuggerStepThrough]
            get { return this._useUnconnected; }
            [DebuggerStepThrough]
            set {
                ApplicationInfo.CheckEditor();
                this._useUnconnected = value;
            }
        }

        #endregion
        #region UseUnscaled

        /// <summary>
        /// Gets or sets a value indicating whether any <see cref="ImageScaling"/> values specified
        /// by the associated <see cref="Image"/> should be ignored.</summary>
        /// <value>
        /// <c>true</c> if the <see cref="EntityImage.ScalingX"/> and <see
        /// cref="EntityImage.ScalingY"/> values of the associated <see cref="Image"/> should be
        /// ignored; otherwise, <c>false</c>. The default is <c>false</c>.</value>
        /// <exception cref="InvalidOperationException">
        /// The property is set, and <see cref="ApplicationInfo.IsEditor"/> is <c>false</c>.
        /// </exception>
        /// <remarks><para>
        /// <b>UseUnscaled</b> holds the value of the "unscaled" XML attribute.
        /// </para><para>
        /// The associated <see cref="ScalingVector"/> is never ignored, even when
        /// <b>UseUnscaled</b> is <c>true</c>.</para></remarks>

        public bool UseUnscaled {
            [DebuggerStepThrough]
            get { return this._useUnscaled; }
            [DebuggerStepThrough]
            set {
                ApplicationInfo.CheckEditor();
                this._useUnscaled = value;
            }
        }

        #endregion
        #region AreParametersEqual

        /// <summary>
        /// Determines whether this instance and a specified <see cref="ImageStackEntry"/> have the
        /// same display parameters.</summary>
        /// <param name="entry">
        /// Another <see cref="ImageStackEntry"/> to compare to this instance.</param>
        /// <returns>
        /// <c>true</c> if the specified <paramref name="entry"/> has the same display parameters as
        /// this instance; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="entry"/> is a null reference.</exception>
        /// <remarks>
        /// <b>AreParametersEqual</b> compares all properties of the two <see
        /// cref="ImageStackEntry"/> instances except <see cref="Image"/>, and returns <c>true</c>
        /// exactly if their values are equal. This determines whether the two instances specify the
        /// same display parameters for their respective images.</remarks>

        public bool AreParametersEqual(ImageStackEntry entry) {
            if (entry == null)
                ThrowHelper.ThrowArgumentNullException("entry");

            return (ColorShift == entry.ColorShift &&
                Offset == entry.Offset &&
                ScalingVector == entry.ScalingVector &&
                SingleFrame == entry.SingleFrame &&
                UseUnconnected == entry.UseUnconnected &&
                UseUnscaled == entry.UseUnscaled);
        }

        #endregion
        #region NormalizeScalingVector

        /// <summary>
        /// Normalizes the specified new value for the <see cref="ScalingVector"/> property.
        /// </summary>
        /// <param name="scalingVector">
        /// The potential new value for the <see cref="ScalingVector"/> property.</param>
        /// <returns>
        /// The specified <paramref name="scalingVector"/>, normalized so that it can be assigned or
        /// compared to the <see cref="ScalingVector"/> property.</returns>
        /// <remarks><para>
        /// <b>NormalizeScalingVector</b> transforms the specified <paramref name="scalingVector"/>
        /// as follows:
        /// </para><list type="number"><item>
        /// Set the x-coordinate to -1 if negative, else to +1.
        /// </item><item>
        /// Set the y-coordinate to -1 if negative, else to +1.
        /// </item><item>
        /// Set both coordinates to zero if both now equal +1.
        /// </item></list><para>
        /// In other words, the specified <paramref name="scalingVector"/> is mapped to either
        /// inversion or identity on either axis, and identity on both axes is mapped to <see
        /// cref="PointI.Empty"/> which is the default value of the <see cref="ScalingVector"/>
        /// property.
        /// </para><para>
        /// Call <b>NormalizeScalingVector</b> on any potential new value for the <see
        /// cref="ScalingVector"/> property before attempting any comparison to the current value.
        /// Setting the property automatically invokes this method.</para></remarks>

        public static PointI NormalizeScalingVector(PointI scalingVector) {

            int x = (scalingVector.X < 0 ? -1 : +1);
            int y = (scalingVector.Y < 0 ? -1 : +1);

            return (x == 1 && y == 1 ? PointI.Empty : new PointI(x, y));
        }

        #endregion
        #region ICloneable Members

        /// <summary>
        /// Creates a new <see cref="ImageStackEntry"/> object that is a deep copy of the current
        /// instance.</summary>
        /// <returns>
        /// A new <see cref="ImageStackEntry"/> object that is a deep copy of the current instance.
        /// </returns>
        /// <remarks>
        /// <b>Clone</b> calls the <see cref="ImageStackEntry(ImageStackEntry)"/> copy constructor
        /// with this <see cref="ImageStackEntry"/> object.</remarks>

        public object Clone() {
            return new ImageStackEntry(this);
        }

        #endregion
        #region ScenarioElement Members
        #region ProcessIdentifier

        /// <summary>
        /// Counts, changes, or deletes all occurrences of the specified identifier in the <see
        /// cref="ImageStackEntry"/>.</summary>
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
        /// <b>ProcessIdentifier</b> processes <see cref="ImageStackEntry"/> properties as follows:
        /// </para><list type="table"><listheader>
        /// <term>Property</term><description>Processing</description>
        /// </listheader><item>
        /// <term><see cref="Image"/></term><description>By key only</description>
        /// </item></list></remarks>

        internal override int ProcessIdentifier(string oldId, string newId) {

            // process image ID
            EntityImagePair pair = this._image;
            int count = CollectionsUtility.ProcessKey(ref pair, oldId, newId);
            this._image = pair;

            return count;
        }

        #endregion
        #region Validate

        /// <summary>
        /// Validates the current data of the <see cref="ImageStackEntry"/>.</summary>
        /// <exception cref="XmlException">
        /// Validation failed, and <see cref="ApplicationInfo.IsEditor"/> is <c>false</c>.
        /// </exception>
        /// <remarks><para>
        /// <b>Validate</b> processes <see cref="ImageStackEntry"/> properties as follows:
        /// </para><list type="table"><listheader>
        /// <term>Property</term><description>Processing</description>
        /// </listheader><item>
        /// <term><see cref="Image"/></term>
        /// <description>Check identifier and set reference</description>
        /// </item></list><para>
        /// Checks are only performed if <see cref="ApplicationInfo.IsEditor"/> is <c>false</c>.
        /// </para></remarks>

        internal override void Validate() {
            string id = Image.Key;

            // set reference for source file ID
            if (String.IsNullOrEmpty(id))
                this._image = new EntityImagePair("", null);
            else {
                // try to locate image in image table
                EntityImage image;
                MasterSection.Instance.Images.Collection.TryGetValue(id, out image);
                this._image = new EntityImagePair(id, image);

                // check for nonexistent image if not editing
                if (image == null && !ApplicationInfo.IsEditor)
                    ThrowHelper.ThrowXmlExceptionWithFormat(Global.Strings.XmlImageStackInvalid, id);
            }
        }

        #endregion
        #endregion
        #region XmlSerializable Members
        #region ConstXmlName

        /// <summary>
        /// The name of the XML element associated with the <see cref="ImageStackEntry"/> class.
        /// </summary>
        /// <remarks>
        /// <b>ConstXmlName</b> holds the value "imageStack", indicating the XML element in <see
        /// cref="FilePaths.ScenarioSchema"/> whose data is managed by the <see
        /// cref="ImageStackEntry"/> class.</remarks>

        public const string ConstXmlName = "imageStack";

        #endregion
        #region ReadXmlAttributes

        /// <summary>
        /// Reads XML attribute data into the <see cref="ImageStackEntry"/> object using the
        /// specified <see cref="XmlReader"/>.</summary>
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

            // attempt to read image identifier
            string id = null;
            XmlUtility.ReadAttributeAsString(reader, "id", ref id);
            if (id != null) id = String.Intern(id);
            this._image = new EntityImagePair(id, null);

            XmlUtility.ReadAttributeAsInt32(reader, "frame", ref this._singleFrame);
            XmlUtility.ReadAttributeAsBoolean(reader, "unconnected", ref this._useUnconnected);
            XmlUtility.ReadAttributeAsBoolean(reader, "unscaled", ref this._useUnscaled);
        }

        #endregion
        #region ReadXmlElements

        /// <summary>
        /// Reads XML element data into the <see cref="ImageStackEntry"/> object using the specified
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
        /// not conform to <see cref="FilePaths.ScenarioSchema"/>.</exception>
        /// <remarks>
        /// Please refer to class <see cref="XmlSerializable"/> for a complete description of this
        /// method.</remarks>

        protected override bool ReadXmlElements(XmlReader reader) {
            switch (reader.Name) {

                case "colorShift":
                    this._colorShift = SimpleXml.ReadColorVector(reader);
                    return true;

                case "offset":
                    this._offset = SimpleXml.ReadPointI(reader);
                    return true;

                case "scaling":
                    this._scalingVector = SimpleXml.ReadPointI(reader);
                    return true;

                default: return false;
            }
        }

        #endregion
        #region ReadXmlObsolete

        /// <summary>
        /// Reads XML data into the <see cref="ImageStackEntry"/> object using the specified <see
        /// cref="XmlReader"/> and an obsolete XML element name.</summary>
        /// <param name="reader">
        /// The <see cref="XmlReader"/> from which to read.</param>
        /// <exception cref="XmlException">
        /// An error occurred while parsing the XML data provided by <paramref name="reader"/>.
        /// </exception>
        /// <exception cref="System.Xml.Schema.XmlSchemaException">
        /// <paramref name="reader"/> is an <see cref="XmlValidatingReader"/>, and the XML data did
        /// not conform to <see cref="FilePaths.ScenarioSchema"/>.</exception>
        /// <remarks>
        /// <b>ReadXmlObsolete</b> is identical to <see cref="XmlSerializable.ReadXml"/> but expects
        /// a current XML node with the obsolete name "images" rather than "imageStack". This method
        /// exists for backward compability only.</remarks>

        internal void ReadXmlObsolete(XmlReader reader) {
            XmlUtility.MoveToStartElement(reader, "images");

            ReadXmlAttributes(reader);
            if (reader.IsEmptyElement) return;

            while (reader.Read() && reader.IsStartElement())
                if (!ReadXmlElements(reader)) {
                    // skip to end tag of unknown element
                    XmlUtility.MoveToEndElement(reader);
                }
        }

        #endregion
        #region WriteXmlAttributes

        /// <summary>
        /// Writes all current data of the <see cref="ImageStackEntry"/> object that is serialized
        /// to XML attributes to the specified <see cref="XmlWriter"/>.</summary>
        /// <param name="writer">
        /// The <see cref="XmlWriter"/> to which to write.</param>
        /// <remarks>
        /// Please refer to class <see cref="XmlSerializable"/> for a complete description of this
        /// method.</remarks>

        protected override void WriteXmlAttributes(XmlWriter writer) {
            writer.WriteAttributeString("id", Image.Key);

            if (SingleFrame >= 0)
                writer.WriteAttributeString("frame", XmlConvert.ToString(SingleFrame));
            if (UseUnconnected)
                writer.WriteAttributeString("unconnected", XmlConvert.ToString(UseUnconnected));
            if (UseUnscaled)
                writer.WriteAttributeString("unscaled", XmlConvert.ToString(UseUnscaled));
        }

        #endregion
        #region WriteXmlElements

        /// <summary>
        /// Writes all current data of the <see cref="ImageStackEntry"/> object that is serialized
        /// to nested XML elements to the specified <see cref="XmlWriter"/>.</summary>
        /// <param name="writer">
        /// The <see cref="XmlWriter"/> to which to write.</param>
        /// <remarks>
        /// Please refer to class <see cref="XmlSerializable"/> for a complete description of this
        /// method.</remarks>

        protected override void WriteXmlElements(XmlWriter writer) {

            if (!ColorShift.IsEmpty) {
                writer.WriteStartElement("colorShift");
                SimpleXml.WriteColorVector(writer, ColorShift);
                writer.WriteEndElement();
            }

            if (Offset != PointI.Empty) {
                writer.WriteStartElement("offset");
                SimpleXml.WritePointI(writer, Offset);
                writer.WriteEndElement();
            }

            if (ScalingVector != PointI.Empty) {
                writer.WriteStartElement("scaling");
                SimpleXml.WritePointI(writer, ScalingVector);
                writer.WriteEndElement();
            }
        }

        #endregion
        #endregion
    }
}
