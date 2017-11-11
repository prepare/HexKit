using System;
using System.Diagnostics;
using System.Windows.Media;
using System.Xml;

using Tektosyne.Collections;
using Hexkit.Global;

namespace Hexkit.Scenario {
    #region Type Aliases

    using EntityImageDictionary = SortedListEx<String, EntityImage>;
    using ImageFileDictionary = SortedListEx<String, ImageFile>;

    #endregion

    /// <summary>
    /// Represents the Images section of a scenario.</summary>
    /// <remarks>
    /// <b>ImageSection</b> is serialized to the XML element "images" defined in <see
    /// cref="FilePaths.ScenarioSchema"/>.</remarks>

    public sealed class ImageSection: ScenarioElement, IDisposable {
        #region ImageSection()

        /// <summary>
        /// Initializes a new instance of the <see cref="ImageSection"/> class.</summary>

        public ImageSection() {
            Information = new Information();
        }

        #endregion
        #region Private Fields

        // property backers
        private readonly EntityImageDictionary _collection = new EntityImageDictionary();
        private readonly ImageFileDictionary _imageFiles = new ImageFileDictionary();
        private Color _maskColor = Colors.Transparent;

        #endregion
        #region Collection

        /// <summary>
        /// Gets a list of all images defined in the <see cref="ImageSection"/>.</summary>
        /// <value>
        /// A <see cref="EntityImageDictionary"/> that maps <see cref="EntityImage.Id"/> strings to
        /// the corresponding <see cref="EntityImage"/> objects. The default is an empty collection.
        /// </value>
        /// <remarks>
        /// <b>Collection</b> never returns a null reference, and its <see
        /// cref="EntityImageDictionary.Values"/> are never null references. The collection is
        /// read-only if <see cref="ApplicationInfo.IsEditor"/> is <c>false</c>.</remarks>

        public EntityImageDictionary Collection {
            [DebuggerStepThrough]
            get {
                return (ApplicationInfo.IsEditor ?
                    this._collection : this._collection.AsReadOnly());
            }
        }

        #endregion
        #region ImageFiles

        /// <summary>
        /// Gets a list of all image files defined in the <see cref="ImageSection"/>.</summary>
        /// <value>
        /// A <see cref="ImageFileDictionary"/> that maps <see cref="ImageFile.Id"/> strings to the
        /// corresponding <see cref="ImageFile"/> objects. The default is an empty collection.
        /// </value>
        /// <remarks>
        /// <b>ImageFiles</b> never returns a null reference, and its <see
        /// cref="ImageFileDictionary.Values"/> are never null references. The collection is
        /// read-only if <see cref="ApplicationInfo.IsEditor"/> is <c>false</c>.</remarks>

        public ImageFileDictionary ImageFiles {
            [DebuggerStepThrough]
            get {
                return (ApplicationInfo.IsEditor ?
                    this._imageFiles : this._imageFiles.AsReadOnly());
            }
        }

        #endregion
        #region Information

        /// <summary>
        /// Gets the <see cref="Scenario.Information"/> block for the <see cref="ImageSection"/>.
        /// </summary>
        /// <value>
        /// The <see cref="Scenario.Information"/> block for the <see cref="ImageSection"/> and for
        /// the graphical tileset providing the actual bitmap data for all images.</value>
        /// <remarks>
        /// <b>Information</b> never returns a null reference. The <see cref="ImageSection"/> class
        /// carries an <see cref="Scenario.Information"/> property of its own because graphical
        /// tilesets and the associated <b>ImageSection</b> data are likely to be reused by many
        /// scenario authors. Use this property to credit the artist(s) who created your tileset.
        /// </remarks>

        public Information Information { get; private set; }

        #endregion
        #region MaskColor

        /// <summary>
        /// Gets or sets the masking color used when loading <see cref="ImageFiles"/>.</summary>
        /// <value>
        /// A <see cref="Color"/> value indicating which color to make transparent in all bitmap
        /// files, in addition to any embedded transparency information. The default is
        /// <b>Color.Transparent</b>, implying no additional transparency.</value>
        /// <exception cref="InvalidOperationException">
        /// The property is set, and <see cref="ApplicationInfo.IsEditor"/> is <c>false</c>.
        /// </exception>
        /// <remarks><para>
        /// <b>MaskColor</b> holds the value of the "maskColor" XML element. This value is supplied
        /// as a parameter to <see cref="ImageFile.Load"/> when <see cref="Load"/> is invoked to
        /// load each bitmap file in the <see cref="ImageFiles"/> collection.
        /// </para><para>
        /// <b>MaskColor</b> and its associated XML element are provided for compatibility with
        /// existing bitmap files that are lacking embedded transparency information. In most cases,
        /// the scenario should not need to set this value.</para></remarks>

        public Color MaskColor {
            [DebuggerStepThrough]
            get { return this._maskColor; }
            [DebuggerStepThrough]
            set {
                ApplicationInfo.CheckEditor();
                this._maskColor = value;
            }
        }

        #endregion
        #region Load

        /// <summary>
        /// Loads the bitmaps of all elements in the <see cref="ImageFiles"/> collection.</summary>
        /// <remarks><para>
        /// <b>Load</b> invokes <see cref="ImageFile.Load"/> on all <see cref="ImageFile"/> objects
        /// in the <see cref="ImageFiles"/> collection with the current <see cref="MaskColor"/>.
        /// This attempts to load all image file bitmaps from disk.
        /// </para><para>
        /// Call <see cref="Unload"/> to reduce memory consumption when you no longer need the
        /// bitmaps.</para></remarks>

        public void Load() {
            foreach (ImageFile file in ImageFiles.Values)
                file.Load(MaskColor);
        }

        #endregion
        #region Unload

        /// <summary>
        /// Unloads the bitmaps of all elements in the <see cref="ImageFiles"/> collection.
        /// </summary>
        /// <remarks>
        /// <b>Unload</b> invokes <see cref="ImageFile.Unload"/> on all <see cref="ImageFile"/>
        /// objects stored in the <see cref="ImageFiles"/> collection. This unloads all image file
        /// bitmaps previously loaded by <see cref="Load"/>. Call this method to reduce memory
        /// consumption when you no longer need the bitmaps.</remarks>

        public void Unload() {
            foreach (ImageFile file in ImageFiles.Values)
                file.Unload();
        }

        #endregion
        #region IDisposable Members

        /// <summary>
        /// Releases all resources used by the <see cref="ImageSection"/> object.</summary>
        /// <remarks>
        /// <b>Dispose</b> invokes <see cref="Unload"/> to release the bitmaps of all elements in
        /// the <see cref="ImageFiles"/> collection.</remarks>

        public void Dispose() { Unload(); }

        #endregion
        #region ScenarioElement Members
        #region ProcessIdentifier

        /// <summary>
        /// Counts, changes, or deletes all occurrences of the specified identifier in the <see
        /// cref="ImageSection"/>.</summary>
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
        /// The number of occurrences of <paramref name="oldId"/> in this <see
        /// cref="ImageSection"/>.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="oldId"/> is a null reference.</exception>
        /// <remarks><para>
        /// <b>ProcessIdentifier</b> processes <see cref="ImageSection"/> properties as follows:
        /// </para><list type="table"><listheader>
        /// <term>Property</term><description>Processing</description>
        /// </listheader><item>
        /// <term><see cref="Collection"/></term><description>By key and by value</description>
        /// </item><item>
        /// <term><see cref="ImageFiles"/></term><description>By key only</description>
        /// </item></list></remarks>

        internal override int ProcessIdentifier(string oldId, string newId) {
            int count = 0;

            // process IDs in file & image tables
            count += CollectionsUtility.ProcessKey(this._imageFiles, oldId, newId);
            count += CollectionsUtility.ProcessKey(this._collection, oldId, newId);

            foreach (EntityImage image in Collection.Values)
                count += image.ProcessIdentifier(oldId, newId);

            return count;
        }

        #endregion
        #region Validate

        /// <summary>
        /// Validates the current data of the <see cref="ImageSection"/>.</summary>
        /// <remarks><para>
        /// <b>Validate</b> processes <see cref="ImageSection"/> properties as follows:
        /// </para><list type="table"><listheader>
        /// <term>Property</term><description>Processing</description>
        /// </listheader><item>
        /// <term><see cref="Collection"/></term>
        /// <description>Invoke <see cref="EntityImage.Validate"/> on all elements</description>
        /// </item></list></remarks>

        internal override void Validate() {

            // set references for IDs in image table
            foreach (EntityImage image in Collection.Values)
                image.Validate();
        }

        #endregion
        #endregion
        #region XmlSerializable Members
        #region ConstXmlName

        /// <summary>
        /// The name of the XML element associated with the <see cref="ImageSection"/> class.
        /// </summary>
        /// <remarks>
        /// <b>ConstXmlName</b> holds the value "images", indicating the XML element in <see
        /// cref="FilePaths.ScenarioSchema"/> whose data is managed by the <see
        /// cref="ImageSection"/> class.</remarks>

        public const string ConstXmlName = "images";

        #endregion
        #region ReadXmlElements

        /// <summary>
        /// Reads XML element data into the <see cref="ImageSection"/> object using the specified
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

                case Information.ConstXmlName:
                    Information = new Information();
                    Information.ReadXml(reader);
                    return true;

                case "maskColor":
                    this._maskColor = SimpleXml.ReadColor(reader);
                    return true;

                case ImageFile.ConstXmlName: {
                    ImageFile imageFile = new ImageFile();
                    imageFile.ReadXml(reader);
                    this._imageFiles.Add(imageFile.Id, imageFile);
                    return true;
                }

                case EntityImage.ConstXmlName: {
                    EntityImage image = new EntityImage();
                    image.ReadXml(reader);
                    this._collection.Add(image.Id, image);
                    return true;
                }

                default: return false;
            }
        }

        #endregion
        #region WriteXmlElements

        /// <summary>
        /// Writes all current data of the <see cref="ImageSection"/> object that is serialized to
        /// nested XML elements to the specified <see cref="XmlWriter"/>.</summary>
        /// <param name="writer">
        /// The <see cref="XmlWriter"/> to which to write.</param>
        /// <remarks>
        /// Please refer to class <see cref="XmlSerializable"/> for a complete description of this
        /// method.</remarks>

        protected override void WriteXmlElements(XmlWriter writer) {
            Information.WriteXml(writer);

            if (MaskColor != Colors.Transparent) {
                writer.WriteStartElement("maskColor");
                SimpleXml.WriteColor(writer, MaskColor);
                writer.WriteEndElement();
            }

            foreach (ImageFile file in ImageFiles.Values)
                file.WriteXml(writer);

            foreach (EntityImage image in Collection.Values)
                image.WriteXml(writer);
        }

        #endregion
        #endregion
    }
}
