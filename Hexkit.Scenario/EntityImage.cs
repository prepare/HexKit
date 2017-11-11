using System;
using System.Diagnostics;
using System.Xml;

using Tektosyne;
using Tektosyne.Collections;
using Tektosyne.Geometry;
using Tektosyne.Xml;
using Hexkit.Global;

namespace Hexkit.Scenario {
    #region Type Aliases

    using ImageFrameList = ListEx<ImageFrame>;

    #endregion

    /// <summary>
    /// Describes a bitmap image that represents an entity class.</summary>
    /// <remarks>
    /// <b>EntityImage</b> is serialized to the XML element "image" defined in <see
    /// cref="FilePaths.ScenarioSchema"/>.</remarks>

    public sealed class EntityImage: ScenarioElement, ICloneable, IMutableKeyedValue<String> {
        #region EntityImage()

        /// <overloads>
        /// Initializes a new instance of the <see cref="EntityImage"/> class.</overloads>
        /// <summary>
        /// Initializes a new instance of the <see cref="EntityImage"/> class with default
        /// properties.</summary>

        public EntityImage() {
            this._frames = new ImageFrameList();
        }

        #endregion
        #region EntityImage(EntityImage)

        /// <summary>
        /// Initializes a new instance of the <see cref="EntityImage"/> class with property values
        /// copied from the specified instance.</summary>
        /// <param name="image">
        /// The <see cref="EntityImage"/> instance whose property values are copied to the new
        /// instance.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="image"/> is a null reference.</exception>
        /// <remarks>
        /// This constructor is called by <see cref="Clone"/> to perform a deep copy of the
        /// specified <paramref name="image"/>, including a deep copy of all mutable objects that
        /// are owned by the <paramref name="image"/>.</remarks>

        public EntityImage(EntityImage image) {
            if (image == null)
                ThrowHelper.ThrowArgumentNullException("image");

            this._id = image._id;
            this._animation = image._animation;
            this._sequence = image._sequence;
            this._scalingX = image._scalingX;
            this._scalingY = image._scalingY;
            this._sourceId = image._sourceId;

            // create deep copy of owned objects
            this._frames = image._frames.Copy();
        }

        #endregion
        #region Private Fields

        // property backers
        private string _id;
        private readonly ImageFrameList _frames;
        private AnimationMode _animation = AnimationMode.None;
        private AnimationSequence _sequence = AnimationSequence.Random;
        private ImageScaling _scalingX = ImageScaling.None, _scalingY = ImageScaling.None;

        // source file ID while reading frames
        private string _sourceId;

        #endregion
        #region Animation

        /// <summary>
        /// Gets or sets a value indicating the animation mode for the <see cref="EntityImage"/>.
        /// </summary>
        /// <value>
        /// An <see cref="AnimationMode"/> value indicating whether and how to play back the
        /// animation <see cref="Sequence"/> of the <see cref="EntityImage"/>. The default is <see
        /// cref="AnimationMode.None"/>.</value>
        /// <exception cref="InvalidOperationException">
        /// The property is set, and <see cref="ApplicationInfo.IsEditor"/> is <c>false</c>.
        /// </exception>
        /// <remarks>
        /// <b>Animation</b> holds the value of the "animation" XML attribute. Animated display
        /// requires the <see cref="Frames"/> collection to hold more than one element.</remarks>

        public AnimationMode Animation {
            [DebuggerStepThrough]
            get { return this._animation; }
            [DebuggerStepThrough]
            set {
                ApplicationInfo.CheckEditor();
                this._animation = value;
            }
        }

        #endregion
        #region Frames

        /// <summary>
        /// Gets a list of all image frames defined for the <see cref="EntityImage"/>.</summary>
        /// <value>
        /// A <see cref="ImageFrameList"/> containing all <see cref="ImageFrame"/> objects defined
        /// for the <see cref="EntityImage"/>. The default is an empty collection.</value>
        /// <remarks><para>
        /// <b>Frames</b> never returns a null reference. The collection is read-only if <see
        /// cref="ApplicationInfo.IsEditor"/> is <c>false</c>.
        /// </para><para>
        /// This property holds the values of all "frame" XML elements.</para></remarks>

        public ImageFrameList Frames {
            [DebuggerStepThrough]
            get {
                return (ApplicationInfo.IsEditor ?
                    this._frames : this._frames.AsReadOnly());
            }
        }

        #endregion
        #region Id

        /// <summary>
        /// Gets or sets the identifier of the <see cref="EntityImage"/>.</summary>
        /// <value>
        /// The identifier of the <see cref="EntityImage"/>. The default is an empty string.</value>
        /// <exception cref="InvalidOperationException">
        /// The property is set, and <see cref="ApplicationInfo.IsEditor"/> is <c>false</c>.
        /// </exception>
        /// <remarks>
        /// <b>Id</b> returns an empty string when set to a null reference. This property holds the
        /// value of the "id" XML attribute which must be unique among all identifiers defined by
        /// the scenario.</remarks>

        public string Id {
            [DebuggerStepThrough]
            get { return this._id ?? ""; }
            [DebuggerStepThrough]
            set {
                ApplicationInfo.CheckEditor();
                if (value != null) value = String.Intern(value);
                this._id = value;
            }
        }

        #endregion
        #region ScalingX

        /// <summary>
        /// Gets or sets a value indicating the horizontal scaling of the <see cref="EntityImage"/>
        /// to fit the current map geometry.</summary>
        /// <value>
        /// An <see cref="ImageScaling"/> value indicating if and how all <see cref="Frames"/> are
        /// scaled horizontally to fit a polygonal <see cref="PolygonGrid.Element"/> of the <see
        /// cref="AreaSection.MapGrid"/>. The default is <see cref="ImageScaling.None"/>.</value>
        /// <exception cref="InvalidOperationException">
        /// The property is set, and <see cref="ApplicationInfo.IsEditor"/> is <c>false</c>.
        /// </exception>
        /// <remarks>
        /// <b>ScalingX</b> holds the value of the "scalingX" XML attribute.</remarks>

        public ImageScaling ScalingX {
            [DebuggerStepThrough]
            get { return this._scalingX; }
            [DebuggerStepThrough]
            set {
                ApplicationInfo.CheckEditor();
                this._scalingX = value;
            }
        }

        #endregion
        #region ScalingY

        /// <summary>
        /// Gets or sets a value indicating the vertical scaling of the <see cref="EntityImage"/> to
        /// fit the current map geometry.</summary>
        /// <value>
        /// An <see cref="ImageScaling"/> value indicating if and how all <see cref="Frames"/> are
        /// scaled vertically to fit a polygonal <see cref="PolygonGrid.Element"/> of the <see
        /// cref="AreaSection.MapGrid"/>. The default is <see cref="ImageScaling.None"/>.</value>
        /// <exception cref="InvalidOperationException">
        /// The property is set, and <see cref="ApplicationInfo.IsEditor"/> is <c>false</c>.
        /// </exception>
        /// <remarks>
        /// <b>ScalingY</b> holds the value of the "scalingY" XML attribute.</remarks>

        public ImageScaling ScalingY {
            [DebuggerStepThrough]
            get { return this._scalingY; }
            [DebuggerStepThrough]
            set {
                ApplicationInfo.CheckEditor();
                this._scalingY = value;
            }
        }

        #endregion
        #region Sequence

        /// <summary>
        /// Gets or sets a value indicating the animation sequence for the <see
        /// cref="EntityImage"/>.</summary>
        /// <value>
        /// An <see cref="AnimationSequence"/> value indicating the sequence in which the <see
        /// cref="Frames"/> of the <see cref="EntityImage"/> should be played back. The default is
        /// <see cref="AnimationSequence.Random"/>.</value>
        /// <exception cref="InvalidOperationException">
        /// The property is set, and <see cref="ApplicationInfo.IsEditor"/> is <c>false</c>.
        /// </exception>
        /// <remarks>
        /// <b>Sequence</b> holds the value of the "sequence" XML attribute. Animated display
        /// requires the <see cref="Frames"/> collection to hold more than one element, and that
        /// <see cref="Animation"/> does not equal <see cref="AnimationMode.None"/>.</remarks>

        public AnimationSequence Sequence {
            [DebuggerStepThrough]
            get { return this._sequence; }
            [DebuggerStepThrough]
            set {
                ApplicationInfo.CheckEditor();
                this._sequence = value;
            }
        }

        #endregion
        #region ToString

        /// <summary>
        /// Returns a <see cref="String"/> that represents the <see cref="EntityImage"/>.</summary>
        /// <returns>
        /// The value of the <see cref="Id"/> property.</returns>

        public override string ToString() {
            return Id;
        }

        #endregion
        #region ICloneable Members

        /// <summary>
        /// Creates a new <see cref="EntityImage"/> object that is a deep copy of the current
        /// instance.</summary>
        /// <returns>
        /// A new <see cref="EntityImage"/> object that is a deep copy of the current instance.
        /// </returns>
        /// <remarks>
        /// <b>Clone</b> calls the <see cref="EntityImage(EntityImage)"/> copy constructor with this
        /// <see cref="EntityImage"/> object.</remarks>

        public object Clone() {
            return new EntityImage(this);
        }

        #endregion
        #region IMutableKeyedValue Members

        /// <summary>
        /// Gets the identifier of the <see cref="EntityImage"/>.</summary>
        /// <value>
        /// The value of the <see cref="Id"/> property.</value>

        string IKeyedValue<String>.Key {
            [DebuggerStepThrough]
            get { return Id; }
        }

        /// <summary>
        /// Sets the identifier of the <see cref="EntityImage"/>.</summary>
        /// <param name="key">
        /// The new value for the <see cref="Id"/> property.</param>
        /// <exception cref="InvalidOperationException">
        /// <see cref="ApplicationInfo.IsEditor"/> is <c>false</c>.</exception>

        void IMutableKeyedValue<String>.SetKey(string key) {
            Id = key;
        }

        #endregion
        #region ScenarioElement Members
        #region ProcessIdentifier

        /// <summary>
        /// Counts, changes, or deletes all occurrences of the specified identifier in the <see
        /// cref="EntityImage"/>.</summary>
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
        /// The number of occurrences of <paramref name="oldId"/> in the <see cref="EntityImage"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="oldId"/> is a null reference.</exception>
        /// <remarks><para>
        /// <b>ProcessIdentifier</b> processes <see cref="EntityImage"/> properties as follows:
        /// </para><list type="table"><listheader>
        /// <term>Property</term><description>Processing</description>
        /// </listheader><item>
        /// <term><see cref="Frames"/></term><description>By value</description>
        /// </item></list></remarks>

        internal override int ProcessIdentifier(string oldId, string newId) {
            int count = 0;

            // process IDs in frame list
            foreach (ImageFrame frame in Frames)
                count += frame.ProcessIdentifier(oldId, newId);

            return count;
        }

        #endregion
        #region Validate

        /// <summary>
        /// Validates the current data of the <see cref="EntityImage"/>.</summary>
        /// <exception cref="XmlException">
        /// Validation failed, and <see cref="ApplicationInfo.IsEditor"/> is <c>false</c>.
        /// </exception>
        /// <remarks><para>
        /// <b>Validate</b> processes <see cref="EntityImage"/> properties as follows:
        /// </para><list type="table"><listheader>
        /// <term>Property</term><description>Processing</description>
        /// </listheader><item>
        /// <term><see cref="Frames"/></term>
        /// <description>Check that collection contains at least one element, and invoke <see
        /// cref="ImageFrame.Validate"/> on each element.</description>
        /// </item></list><para>
        /// Checks are only performed if <see cref="ApplicationInfo.IsEditor"/> is <c>false</c>.
        /// </para></remarks>

        internal override void Validate() {

            // check for empty frame list if not editing
            if (!ApplicationInfo.IsEditor && Frames.Count == 0)
                ThrowHelper.ThrowXmlExceptionWithFormat(Global.Strings.XmlImageNoFrame, Id);

            // set references for IDs in frame list
            foreach (ImageFrame frame in Frames)
                frame.Validate();
        }

        #endregion
        #endregion
        #region XmlSerializable Members
        #region ConstXmlName

        /// <summary>
        /// The name of the XML element associated with the <see cref="EntityImage"/> class.
        /// </summary>
        /// <remarks>
        /// <b>ConstXmlName</b> holds the value "image", indicating the XML element in <see
        /// cref="FilePaths.ScenarioSchema"/> whose data is managed by the <see
        /// cref="EntityImage"/> class.</remarks>

        public const string ConstXmlName = "image";

        #endregion
        #region ReadXmlAttributes

        /// <summary>
        /// Reads XML attribute data into the <see cref="EntityImage"/> object using the specified
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
            XmlUtility.ReadAttributeAsString(reader, "id", ref this._id);

            XmlUtility.ReadAttributeAsEnum(reader, "animation", ref this._animation);
            XmlUtility.ReadAttributeAsEnum(reader, "sequence", ref this._sequence);
            XmlUtility.ReadAttributeAsEnum(reader, "scalingX", ref this._scalingX);
            XmlUtility.ReadAttributeAsEnum(reader, "scalingY", ref this._scalingY);
        }

        #endregion
        #region ReadXmlElements

        /// <summary>
        /// Reads XML element data into the <see cref="EntityImage"/> object using the specified
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

                case "source":
                    // update current source file ID
                    this._sourceId = SimpleXml.ReadIdentifier(reader);
                    return true;

                case "frame": {
                    // create frame with current source file ID
                    ImageFrame frame = new ImageFrame(this._sourceId);
                    frame.ReadXml(reader);
                    this._frames.Add(frame);
                    return true;
                }

                default: return false;
            }
        }

        #endregion
        #region WriteXmlAttributes

        /// <summary>
        /// Writes all current data of the <see cref="EntityImage"/> object that is serialized to
        /// XML attributes to the specified <see cref="XmlWriter"/>.</summary>
        /// <param name="writer">
        /// The <see cref="XmlWriter"/> to which to write.</param>
        /// <remarks>
        /// Please refer to class <see cref="XmlSerializable"/> for a complete description of this
        /// method.</remarks>

        protected override void WriteXmlAttributes(XmlWriter writer) {
            writer.WriteAttributeString("id", Id);

            if (Animation != AnimationMode.None)
                writer.WriteAttributeString("animation", Animation.ToString());
            if (Sequence != AnimationSequence.Random)
                writer.WriteAttributeString("sequence", Sequence.ToString());
            if (ScalingX != ImageScaling.None)
                writer.WriteAttributeString("scalingX", ScalingX.ToString());
            if (ScalingY != ImageScaling.None)
                writer.WriteAttributeString("scalingY", ScalingY.ToString());
        }

        #endregion
        #region WriteXmlElements

        /// <summary>
        /// Writes all current data of the <see cref="EntityImage"/> object that is serialized to
        /// nested XML elements to the specified <see cref="XmlWriter"/>.</summary>
        /// <param name="writer">
        /// The <see cref="XmlWriter"/> to which to write.</param>
        /// <remarks>
        /// Please refer to class <see cref="XmlSerializable"/> for a complete description of this
        /// method.</remarks>

        protected override void WriteXmlElements(XmlWriter writer) {
            string sourceId = null;

            foreach (ImageFrame frame in Frames) {
                if (sourceId != frame.Source.Key) {
                    sourceId = frame.Source.Key;
                    SimpleXml.WriteIdentifier(writer, "source", sourceId);
                }

                frame.WriteXml(writer);
            }
        }

        #endregion
        #endregion
    }
}
