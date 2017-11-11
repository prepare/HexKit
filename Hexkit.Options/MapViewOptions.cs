using System;
using System.Diagnostics;
using System.Xml;

using Tektosyne;
using Tektosyne.Xml;

using Hexkit.Global;
using Hexkit.Scenario;

namespace Hexkit.Options {

    /// <summary>
    /// Manages user settings related to a specific map view.</summary>
    /// <remarks><para>
    /// A given <b>MapViewOptions</b> object manages all user settings that are related to the map
    /// view identified by its <see cref="MapViewOptions.Id"/> property.
    /// </para><para>
    /// All properties of this class are read-only. Call <see cref="MapViewOptions.Save"/> to set
    /// all mutable <b>MapViewOptions</b> properties to the specified display parameters.
    /// </para><para>
    /// <b>MapViewOptions</b> is serialized to the XML element "mapView" defined in <see
    /// cref="FilePaths.OptionsSchema"/>.</para></remarks>

    public sealed class MapViewOptions: XmlSerializable {
        #region MapViewOptions(String)

        /// <summary>
        /// Initializes a new instance of the <see cref="MapViewOptions"/> class with the specified
        /// identifier.</summary>
        /// <param name="id">
        /// The initial value for the <see cref="Id"/> property.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="id"/> is a null reference.</exception>
        /// <remarks>
        /// The specified <paramref name="id"/> may be an empty string if the new <see
        /// cref="MapViewOptions"/> object is to be initialized by the <see
        /// cref="ReadXmlAttributes"/> method.</remarks>

        internal MapViewOptions(string id) {
            if (id == null)
                ThrowHelper.ThrowArgumentNullException("id");

            this._id = String.Intern(id);
        }

        #endregion
        #region Private Fields

        // property backers
        private string _id;
        private int _scale = 100;

        private bool _animation = true;
        private bool _showFlags = true;
        private bool _showGrid = true;
        private bool _showOwner = false;

        private string _gaugeResource = ResourceClass.StandardStrength.Id;
        private GaugeDisplay _gaugeResourceFlags;

        private string _shownVariable;
        private VariableDisplay _shownVariableFlags;

        #endregion
        #region AllVariableDisplays

        /// <summary>
        /// An <see cref="Array"/> comprising all values in <see cref="VariableDisplay"/>.
        /// </summary>
        /// <remarks>
        /// <b>AllVariableDisplays</b> facilitates iterating through all values of the <see
        /// cref="VariableDisplay"/> enumeration. This field holds the result of
        /// <c>Enum.GetValues(typeof(VariableDisplay))</c>.</remarks>

        public static readonly VariableDisplay[] AllVariableDisplays =
            (VariableDisplay[]) Enum.GetValues(typeof(VariableDisplay));

        #endregion
        #region Animation

        /// <summary>
        /// Gets a value indicating whether animation is enabled by default.</summary>
        /// <value>
        /// The default value for the <b>Animation</b> property of the map view identified by the
        /// <see cref="Id"/> string. The default is <c>true</c>.</value>
        /// <remarks>
        /// <b>Animation</b> holds the value of the "animation" XML attribute.</remarks>

        public bool Animation {
            [DebuggerStepThrough]
            get { return this._animation; }
        }

        #endregion
        #region GaugeResource

        /// <summary>
        /// Gets the identifier of the <see cref="ResourceClass"/> whose depletion status is shown
        /// by default.</summary>
        /// <value><para>
        /// The identifier of the <see cref="ResourceClass"/> that is the default value for the 
        /// <b>GaugeResource</b> property of the map view identified by the <see cref="Id"/> string.
        /// </para><para>
        /// The default is a null reference if an XML options description file with a <see
        /// cref="ApplicationOptions.Version"/> of 4.1.6 or later is present; otherwise, the
        /// identifier of the <see cref="ResourceClass.StandardStrength"/> pseudo-resource.
        /// </para></value>
        /// <remarks>
        /// <b>GaugeResource</b> holds the value of the "gaugeResource" XML attribute.</remarks>

        public string GaugeResource {
            [DebuggerStepThrough]
            get { return this._gaugeResource; }
        }

        #endregion
        #region GaugeResourceFlags

        /// <summary>
        /// Gets the default display flags for the <see cref="GaugeResource"/>.</summary>
        /// <value>
        /// The default value for the <b>GaugeResourceFlags</b> property of the map view identified
        /// by the <see cref="Id"/> string. The default is zero.</value>
        /// <remarks>
        /// <b>GaugeResourceFlags</b> holds the value of the "gaugeResourceFlags" XML attribute.
        /// </remarks>

        public GaugeDisplay GaugeResourceFlags {
            [DebuggerStepThrough]
            get { return this._gaugeResourceFlags; }
        }

        #endregion
        #region Id

        /// <summary>
        /// Gets the identifier of the map view associated with the <see cref="MapViewOptions"/>
        /// object.</summary>
        /// <value>
        /// The identifier of the map view to which the settings stored in this <see
        /// cref="MapViewOptions"/> object apply.</value>
        /// <remarks><para>
        /// <b>Id</b> never returns a null reference, and never returns an empty string once the
        /// <see cref="MapViewOptions"/> object has been fully initialized.
        /// </para><para>
        /// This property holds the value of the "id" XML attribute which must be unique among all
        /// identifiers defined by the XML options description file.</para></remarks>

        public string Id {
            [DebuggerStepThrough]
            get { return this._id; }
        }

        #endregion
        #region Scale

        /// <summary>
        /// Gets the default display scale.</summary>
        /// <value>
        /// The default value for the <b>Scale</b> property of the map view identified by the <see
        /// cref="Id"/> string. The default is 100, indicating an unscaled map view.</value>
        /// <remarks>
        /// <b>Scale</b> holds the value of the "scale" XML attribute.</remarks>

        public int Scale {
            [DebuggerStepThrough]
            get { return this._scale; }
        }

        #endregion
        #region ShowFlags

        /// <summary>
        /// Gets a value indicating whether unit flags are shown by default.</summary>
        /// <value>
        /// The default value for the <b>ShowFlags</b> property of the map view identified by the
        /// <see cref="Id"/> string. The default is <c>true</c>.</value>
        /// <remarks>
        /// <b>ShowFlags</b> holds the value of the "showFlags" XML attribute.</remarks>

        public bool ShowFlags {
            [DebuggerStepThrough]
            get { return this._showFlags; }
        }

        #endregion
        #region ShowGrid

        /// <summary>
        /// Gets a value indicating whether a grid outline is shown by default.</summary>
        /// <value>
        /// The default value for the <b>ShowGrid</b> property of the map view identified by the
        /// <see cref="Id"/> string. The default is <c>true</c>.</value>
        /// <remarks>
        /// <b>ShowGrid</b> holds the value of the "showGrid" XML attribute.</remarks>

        public bool ShowGrid {
            [DebuggerStepThrough]
            get { return this._showGrid; }
        }

        #endregion
        #region ShownVariable

        /// <summary>
        /// Gets the identifier of the <see cref="VariableClass"/> whose values are shown by
        /// default.</summary>
        /// <value>
        /// The identifier of the <see cref="VariableClass"/> that is the default value for the 
        /// <b>ShownVariable</b> property of the map view identified by the <see cref="Id"/> string.
        /// The default is a null reference.</value>
        /// <remarks>
        /// <b>ShownVariable</b> holds the value of the "shownVariable" XML attribute.</remarks>

        public string ShownVariable {
            [DebuggerStepThrough]
            get { return this._shownVariable; }
        }

        #endregion
        #region ShownVariableFlags

        /// <summary>
        /// Gets the default display flags for the <see cref="ShownVariable"/>.</summary>
        /// <value>
        /// The default value for the <b>ShownVariableFlags</b> property of the map view identified
        /// by the <see cref="Id"/> string. The default is zero.</value>
        /// <remarks>
        /// <b>ShownVariableFlags</b> holds the value of the "shownVariableFlags" XML attribute.
        /// </remarks>

        public VariableDisplay ShownVariableFlags {
            [DebuggerStepThrough]
            get { return this._shownVariableFlags; }
        }

        #endregion
        #region ShowOwner

        /// <summary>
        /// Gets a value indicating whether owner colors are shown by default.</summary>
        /// <value>
        /// The default value for the <b>ShowOwner</b> property of the map view identified by the
        /// <see cref="Id"/> string. The default is <c>false</c>.</value>
        /// <remarks>
        /// <b>ShowOwner</b> holds the value of the "showOwner" XML attribute.</remarks>

        public bool ShowOwner {
            [DebuggerStepThrough]
            get { return this._showOwner; }
        }

        #endregion
        #region Save

        /// <summary>
        /// Saves the specified display parameters to the corresponding <see cref="MapViewOptions"/>
        /// properties.</summary>
        /// <param name="scale">
        /// The new value for the <see cref="Scale"/> property.</param>
        /// <param name="animation">
        /// The new value for the <see cref="Animation"/> property.</param>
        /// <param name="showFlags">
        /// The new value for the <see cref="ShowFlags"/> property.</param>
        /// <param name="showGrid">
        /// The new value for the <see cref="ShowGrid"/> property.</param>
        /// <param name="showOwner">
        /// The new value for the <see cref="ShowOwner"/> property.</param>
        /// <param name="gaugeResource">
        /// The new value for the <see cref="GaugeResource"/> property.</param>
        /// <param name="gaugeResourceFlags">
        /// The new value for the <see cref="GaugeResourceFlags"/> property.</param>
        /// <param name="shownVariable">
        /// The <see cref="VariableClass"/> whose <see cref="VariableClass.Id"/> string is the new
        /// value for the <see cref="ShownVariable"/> property.</param>
        /// <param name="shownVariableFlags">
        /// The new value for the <see cref="ShownVariableFlags"/> property.</param>

        internal void Save(int scale,
            bool animation, bool showFlags, bool showGrid, bool showOwner,
            string gaugeResource, GaugeDisplay gaugeResourceFlags,
            VariableClass shownVariable, VariableDisplay shownVariableFlags) {

            this._scale = scale;
            this._animation = animation;
            this._showFlags = showFlags;
            this._showGrid = showGrid;
            this._showOwner = showOwner;

            this._gaugeResource = gaugeResource;
            this._gaugeResourceFlags = gaugeResourceFlags;

            // save ShownVariable/Flags only if valid
            if (shownVariable != null) {
                this._shownVariable = shownVariable.Id;
                this._shownVariableFlags = shownVariableFlags;
            } else {
                this._shownVariable = null;
                this._shownVariableFlags = 0;
            }
        }

        #endregion
        #region XmlSerializable Members
        #region ConstXmlName

        /// <summary>
        /// The name of the XML element associated with the <see cref="MapViewOptions"/> class.
        /// </summary>
        /// <remarks>
        /// <b>ConstXmlName</b> holds the value "mapView", indicating the XML element in <see
        /// cref="FilePaths.OptionsSchema"/> whose data is managed by the <see
        /// cref="MapViewOptions"/> class.</remarks>

        public const string ConstXmlName = "mapView";

        #endregion
        #region ReadXmlAttributes

        /// <summary>
        /// Reads XML attribute data into the <see cref="MapViewOptions"/> object using the
        /// specified <see cref="XmlReader"/>.</summary>
        /// <param name="reader">
        /// The <see cref="XmlReader"/> from which to read.</param>
        /// <exception cref="XmlException">
        /// An error occurred while parsing the XML data provided by <paramref name="reader"/>.
        /// </exception>
        /// <exception cref="System.Xml.Schema.XmlSchemaException">
        /// <paramref name="reader"/> is an <see cref="XmlValidatingReader"/>, and the XML data did
        /// not conform to <see cref="FilePaths.OptionsSchema"/>.</exception>
        /// <remarks>
        /// Please refer to class <see cref="XmlSerializable"/> for a complete description of this
        /// method.</remarks>

        protected override void ReadXmlAttributes(XmlReader reader) {
            XmlUtility.ReadAttributeAsString(reader, "id", ref this._id);
            XmlUtility.ReadAttributeAsInt32(reader, "scale", ref this._scale);

            XmlUtility.ReadAttributeAsBoolean(reader, "animation", ref this._animation);
            XmlUtility.ReadAttributeAsBoolean(reader, "showFlags", ref this._showFlags);
            XmlUtility.ReadAttributeAsBoolean(reader, "showGrid", ref this._showGrid);
            XmlUtility.ReadAttributeAsBoolean(reader, "showOwner", ref this._showOwner);

            // Hexkit versions before 4.1.6 keep default value StandardStrength
            if (ApplicationOptions.Instance.Version >= new Version(4, 1, 6))
                this._gaugeResource = null;

            XmlUtility.ReadAttributeAsString(reader, "gaugeResource", ref this._gaugeResource);
            XmlUtility.ReadAttributeAsEnum(reader, "gaugeResourceFlags", ref this._gaugeResourceFlags);

            XmlUtility.ReadAttributeAsString(reader, "shownVariable", ref this._shownVariable);
            XmlUtility.ReadAttributeAsEnum(reader, "shownVariableFlags", ref this._shownVariableFlags);
        }

        #endregion
        #region WriteXmlAttributes

        /// <summary>
        /// Writes all current data of the <see cref="MapViewOptions"/> object that is serialized to
        /// XML attributes to the specified <see cref="XmlWriter"/>.</summary>
        /// <param name="writer">
        /// The <see cref="XmlWriter"/> to which to write.</param>
        /// <remarks>
        /// Please refer to class <see cref="XmlSerializable"/> for a complete description of this
        /// method.</remarks>

        protected override void WriteXmlAttributes(XmlWriter writer) {
            writer.WriteAttributeString("id", Id);
            writer.WriteAttributeString("scale", XmlConvert.ToString(Scale));

            writer.WriteAttributeString("animation", XmlConvert.ToString(Animation));
            writer.WriteAttributeString("showFlags", XmlConvert.ToString(ShowFlags));
            writer.WriteAttributeString("showGrid", XmlConvert.ToString(ShowGrid));
            writer.WriteAttributeString("showOwner", XmlConvert.ToString(ShowOwner));

            if (!String.IsNullOrEmpty(GaugeResource))
                writer.WriteAttributeString("gaugeResource", GaugeResource);
            if (GaugeResourceFlags != 0)
                writer.WriteAttributeString("gaugeResourceFlags",
                    GaugeResourceFlags.ToString().Replace(", ", " "));

            if (!String.IsNullOrEmpty(ShownVariable))
                writer.WriteAttributeString("shownVariable", ShownVariable);
            if (ShownVariableFlags != 0)
                writer.WriteAttributeString("shownVariableFlags",
                    ShownVariableFlags.ToString().Replace(", ", " "));
        }

        #endregion
        #endregion
    }
}
