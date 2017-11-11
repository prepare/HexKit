using System;
using System.Diagnostics;
using System.Xml;

using Tektosyne.Xml;
using Hexkit.Global;

namespace Hexkit.Scenario {

    /// <summary>
    /// Represents a class of terrain entities.</summary>
    /// <remarks>
    /// <b>TerrainClass</b> corresponds to the complex XML type "terrainClass" defined in <see
    /// cref="FilePaths.ScenarioSchema"/>. Instances are serialized to the XML element "terrain".
    /// </remarks>

    public sealed class TerrainClass: EntityClass {
        #region TerrainClass()

        /// <overloads>
        /// Initializes a new instance of the <see cref="TerrainClass"/> class.</overloads>
        /// <summary>
        /// Initializes a new instance of the <see cref="TerrainClass"/> class with default
        /// properties.</summary>
        /// <remarks>
        /// The <see cref="EntityClass.Category"/> property is initialized to a value of <see
        /// cref="EntityCategory.Terrain"/>.</remarks>

        internal TerrainClass(): base("", EntityCategory.Terrain) { }

        #endregion
        #region TerrainClass(String)

        /// <summary>
        /// Initializes a new instance of the <see cref="TerrainClass"/> class with the specified
        /// identifier.</summary>
        /// <param name="id">
        /// The initial value for the <see cref="EntityClass.Id"/> property.</param>
        /// <remarks>
        /// The <see cref="EntityClass.Category"/> property is initialized to a value of <see
        /// cref="EntityCategory.Terrain"/>.</remarks>

        public TerrainClass(string id): base(id, EntityCategory.Terrain) { }

        #endregion
        #region TerrainClass(TerrainClass)

        /// <summary>
        /// Initializes a new instance of the <see cref="TerrainClass"/> class with property values
        /// copied from the specified instance.</summary>
        /// <param name="terrain">
        /// The <see cref="TerrainClass"/> instance whose property values are copied to the new
        /// instance.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="terrain"/> is a null reference.</exception>
        /// <remarks>
        /// This constructor is called by <see cref="Clone"/> to perform a deep copy of the
        /// specified <paramref name="terrain"/>, including a deep copy of all mutable objects that
        /// are owned by the <paramref name="terrain"/>.</remarks>

        public TerrainClass(TerrainClass terrain): base(terrain) {

            this._canCapture = terrain._canCapture;
            this._canDestroy = terrain._canDestroy;
            this._isBackground = terrain._isBackground;

            this._difficultyAttribute = terrain._difficultyAttribute;
            this._elevationAttribute = terrain._elevationAttribute;
        }

        #endregion
        #region Private Fields

        // property backers
        private bool _canCapture, _canDestroy, _isBackground;
        private string _difficultyAttribute, _elevationAttribute;

        #endregion
        #region CanCapture

        /// <summary>
        /// Gets or sets a value indicating whether map sites that contain terrains based on the
        /// <see cref="TerrainClass"/> may be captured by units.</summary>
        /// <value>
        /// <c>true</c> if map sites that contain terrains based on the <see cref="TerrainClass"/>
        /// may be captured by units; otherwise, <c>false</c>. The default is <c>false</c>.</value>
        /// <exception cref="InvalidOperationException">
        /// The property is set, and <see cref="ApplicationInfo.IsEditor"/> is <c>false</c>.
        /// </exception>
        /// <remarks>
        /// <b>CanCapture</b> holds the value of the "capture" XML attribute. If this property is
        /// <c>true</c>, sites that contain at least one terrain based on the <see
        /// cref="TerrainClass"/> acquire the faction ownership of occupying units, provided that at
        /// least one unit is based on a <see cref="UnitClass"/> whose <see
        /// cref="UnitClass.CanCapture"/> flag is also <c>true</c>. Otherwise, the site ownership
        /// remains unchanged.</remarks>

        public bool CanCapture {
            [DebuggerStepThrough]
            get { return this._canCapture; }
            [DebuggerStepThrough]
            set {
                ApplicationInfo.CheckEditor();
                this._canCapture = value;
            }
        }

        #endregion
        #region CanDestroy

        /// <summary>
        /// Gets or sets a value indicating whether terrains based on the <see cref="TerrainClass"/>
        /// may be destroyed by their owner.</summary>
        /// <value>
        /// <c>true</c> if terrains based on the <see cref="TerrainClass"/> may be destroyed at will
        /// by their owner; otherwise, <c>false</c>. The default is <c>false</c>.</value>
        /// <exception cref="InvalidOperationException">
        /// The property is set, and <see cref="ApplicationInfo.IsEditor"/> is <c>false</c>.
        /// </exception>
        /// <remarks>
        /// <b>CanDestroy</b> holds the value of the "destroy" XML attribute. This property always
        /// returns <c>false</c> if <see cref="IsBackground"/> is <c>true</c>, regardless of the
        /// value it was set to.</remarks>

        public override bool CanDestroy {
            [DebuggerStepThrough]
            get { return (this._canDestroy && !IsBackground); }
            [DebuggerStepThrough]
            set {
                ApplicationInfo.CheckEditor();
                this._canDestroy = value;
            }
        }

        #endregion
        #region DifficultyAttribute

        /// <summary>
        /// Gets or sets the identifier of the <see cref="EntityClass.Attributes"/> element that
        /// represents the difficulty of moving across terrains based on the <see
        /// cref="TerrainClass"/>.</summary>
        /// <value>
        /// The identifier of the <see cref="EntityClass.Attributes"/> element that represents the
        /// difficulty of moving across terrains based on the <see cref="TerrainClass"/>. The
        /// default is an empty string, indicating that no such <see cref="EntityClass.Attributes"/>
        /// element exists.</value>
        /// <exception cref="InvalidOperationException">
        /// The property is set, and <see cref="ApplicationInfo.IsEditor"/> is <c>false</c>.
        /// </exception>
        /// <remarks>
        /// <b>DifficultyAttribute</b> returns an empty string when set to a null reference. This
        /// property holds the value of the "id" attribute of the "difficultyAttribute" XML element.
        /// </remarks>

        public string DifficultyAttribute {
            [DebuggerStepThrough]
            get { return this._difficultyAttribute ?? ""; }
            [DebuggerStepThrough]
            set {
                ApplicationInfo.CheckEditor();
                if (value != null) value = String.Intern(value);
                this._difficultyAttribute = value;
            }
        }

        #endregion
        #region ElevationAttribute

        /// <summary>
        /// Gets or sets the identifier of the <see cref="EntityClass.Attributes"/> element that
        /// represents the elevation of terrains based on the <see cref="TerrainClass"/>.</summary>
        /// <value>
        /// The identifier of the <see cref="EntityClass.Attributes"/> element that represents the
        /// elevation of terrains based on the <see cref="TerrainClass"/>. The default is an empty
        /// string, indicating that no such <see cref="EntityClass.Attributes"/> element exists.
        /// </value>
        /// <exception cref="InvalidOperationException">
        /// The property is set, and <see cref="ApplicationInfo.IsEditor"/> is <c>false</c>.
        /// </exception>
        /// <remarks>
        /// <b>ElevationAttribute</b> returns an empty string when set to a null reference. This
        /// property holds the value of the "id" attribute of the "elevationAttribute" XML element.
        /// </remarks>

        public string ElevationAttribute {
            [DebuggerStepThrough]
            get { return this._elevationAttribute ?? ""; }
            [DebuggerStepThrough]
            set {
                ApplicationInfo.CheckEditor();
                if (value != null) value = String.Intern(value);
                this._elevationAttribute = value;
            }
        }

        #endregion
        #region IsBackground

        /// <summary>
        /// Gets or sets a value indicating whether the <see cref="TerrainClass"/> represents
        /// background terrain.</summary>
        /// <value>
        /// <c>true</c> if the <see cref="TerrainClass"/> represents background terrain; otherwise,
        /// <c>false</c>. The default is <c>false</c>.</value>
        /// <exception cref="InvalidOperationException">
        /// The property is set, and <see cref="ApplicationInfo.IsEditor"/> is <c>false</c>.
        /// </exception>
        /// <remarks>
        /// <b>IsBackground</b> holds the value of the "background" XML attribute. This property
        /// must be <c>true</c> for at least one <see cref="TerrainClass"/> instance on each map
        /// site. When creating the game map, Hexkit deletes all <see cref="TerrainClass"/>
        /// instances that precede the first background terrain.</remarks>

        public bool IsBackground {
            [DebuggerStepThrough]
            get { return this._isBackground; }
            [DebuggerStepThrough]
            set {
                ApplicationInfo.CheckEditor();
                this._isBackground = value;
            }
        }

        #endregion
        #region ICloneable Members

        /// <summary>
        /// Creates a new <see cref="TerrainClass"/> object that is a deep copy of the current
        /// instance.</summary>
        /// <returns>
        /// A new <see cref="TerrainClass"/> object that is a deep copy of the current instance.
        /// </returns>
        /// <remarks>
        /// <b>Clone</b> calls the <see cref="TerrainClass(TerrainClass)"/> copy constructor with
        /// this <see cref="TerrainClass"/> object.</remarks>

        public override object Clone() {
            return new TerrainClass(this);
        }

        #endregion
        #region XmlSerializable Members
        #region ConstXmlName

        /// <summary>
        /// The name of the XML element associated with the <see cref="TerrainClass"/> class.
        /// </summary>
        /// <remarks>
        /// <b>ConstXmlName</b> holds the value "terrain", indicating the XML element in <see
        /// cref="FilePaths.ScenarioSchema"/> whose data is managed by the <see
        /// cref="TerrainClass"/> class.</remarks>

        public const string ConstXmlName = "terrain";

        #endregion
        #region ReadXmlAttributes

        /// <summary>
        /// Reads XML attribute data into the <see cref="TerrainClass"/> object using the specified
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
            base.ReadXmlAttributes(reader);

            XmlUtility.ReadAttributeAsBoolean(reader, "background", ref this._isBackground);
            XmlUtility.ReadAttributeAsBoolean(reader, "capture", ref this._canCapture);
            XmlUtility.ReadAttributeAsBoolean(reader, "destroy", ref this._canDestroy);
        }

        #endregion
        #region ReadXmlElements

        /// <summary>
        /// Reads XML element data into the <see cref="TerrainClass"/> object using the specified
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
            if (base.ReadXmlElements(reader))
                return true;

            switch (reader.Name) {

                case "difficultyAttribute":
                    this._difficultyAttribute = SimpleXml.ReadIdentifier(reader);
                    return true;

                case "elevationAttribute":
                    this._elevationAttribute = SimpleXml.ReadIdentifier(reader);
                    return true;

                default: return false;
            }
        }

        #endregion
        #region WriteXmlAttributes

        /// <summary>
        /// Writes all current data of the <see cref="TerrainClass"/> object that is serialized to
        /// XML attributes to the specified <see cref="XmlWriter"/>.</summary>
        /// <param name="writer">
        /// The <see cref="XmlWriter"/> to which to write.</param>
        /// <remarks>
        /// Please refer to class <see cref="XmlSerializable"/> for a complete description of this
        /// method.</remarks>

        protected override void WriteXmlAttributes(XmlWriter writer) {
            base.WriteXmlAttributes(writer);

            if (IsBackground)
                writer.WriteAttributeString("background", XmlConvert.ToString(IsBackground));
            if (CanCapture)
                writer.WriteAttributeString("capture", XmlConvert.ToString(CanCapture));
            if (CanDestroy)
                writer.WriteAttributeString("destroy", XmlConvert.ToString(CanDestroy));
        }

        #endregion
        #region WriteXmlElements

        /// <summary>
        /// Writes all current data of the <see cref="TerrainClass"/> object that is serialized to
        /// nested XML elements to the specified <see cref="XmlWriter"/>.</summary>
        /// <param name="writer">
        /// The <see cref="XmlWriter"/> to which to write.</param>
        /// <remarks>
        /// Please refer to class <see cref="XmlSerializable"/> for a complete description of this
        /// method.</remarks>

        protected override void WriteXmlElements(XmlWriter writer) {
            base.WriteXmlElements(writer);

            SimpleXml.WriteIdentifier(writer, "difficultyAttribute", DifficultyAttribute);
            SimpleXml.WriteIdentifier(writer, "elevationAttribute", ElevationAttribute);
        }

        #endregion
        #endregion
    }
}
