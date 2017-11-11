using System;
using System.Diagnostics;
using System.Xml;

using Tektosyne.Xml;
using Hexkit.Global;

namespace Hexkit.Scenario {

    /// <summary>
    /// Represents a class of resource variables.</summary>
    /// <remarks>
    /// <b>ResourceClass</b> corresponds to the complex XML type "resourceClass" defined in <see
    /// cref="FilePaths.ScenarioSchema"/>. Instances are serialized to the XML element "resource".
    /// </remarks>

    public sealed class ResourceClass: VariableClass {
        #region ResourceClass()

        /// <overloads>
        /// Initializes a new instance of the <see cref="ResourceClass"/> class.</overloads>
        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceClass"/> class with default
        /// properties.</summary>
        /// <remarks>
        /// The <see cref="VariableClass.Category"/> property is initialized to a value of <see
        /// cref="VariableCategory.Resource"/>.</remarks>

        internal ResourceClass(): base("", VariableCategory.Resource) { }

        #endregion
        #region ResourceClass(String)

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceClass"/> class with the specified
        /// identifier.</summary>
        /// <param name="id">
        /// The initial value for the <see cref="VariableClass.Id"/> property.</param>
        /// <remarks>
        /// The <see cref="VariableClass.Category"/> property is initialized to a value of <see
        /// cref="VariableCategory.Resource"/>.</remarks>

        public ResourceClass(string id): base(id, VariableCategory.Resource) { }

        #endregion
        #region ResourceClass(String, String)

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceClass"/> class with the specified
        /// identifier and display name.</summary>
        /// <param name="id">
        /// The initial value for the <see cref="VariableClass.Id"/> property.</param>
        /// <param name="name">
        /// The initial value for the <see cref="VariableClass.Name"/> property.</param>
        /// <remarks>
        /// The <see cref="VariableClass.Category"/> property is initialized to a value of <see
        /// cref="VariableCategory.Resource"/>.</remarks>

        private ResourceClass(string id, string name):
            base(id, VariableCategory.Resource, name) { }

        #endregion
        #region ResourceClass(ResourceClass)

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceClass"/> class with property values
        /// copied from the specified instance.</summary>
        /// <param name="resource">
        /// The <see cref="ResourceClass"/> instance whose property values are copied to the new
        /// instance.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="resource"/> is a null reference.</exception>
        /// <remarks>
        /// This constructor is called by <see cref="Clone"/> to perform a deep copy of the
        /// specified <paramref name="resource"/>, including a deep copy of all mutable objects that
        /// are owned by the <paramref name="resource"/>.</remarks>

        public ResourceClass(ResourceClass resource): base(resource) {

            this._defeat = resource._defeat;
            this._victory = resource._victory;

            this._isDepletable = resource._isDepletable;
            this._isLimited = resource._isLimited;
            this._isResetting = resource._isResetting;
        }

        #endregion
        #region Private Fields

        // property backers
        private int _defeat = Int32.MinValue;
        private int _victory = Int32.MaxValue;
        private bool _isDepletable, _isLimited, _isResetting;

        #endregion
        #region StandardMorale

        /// <summary>
        /// A read-only <see cref="ResourceClass"/> that represents the standard morale resource for
        /// units.</summary>
        /// <remarks><para>
        /// <b>StandardMorale</b> represents a pseudo-resource for user interfaces and does not
        /// appear in the <see cref="VariableSection.Resources"/> collection.
        /// </para><para>
        /// All <b>StandardMorale</b> properties have default values, except for <see
        /// cref="VariableClass.Id"/> which returns "standard-morale", and <see
        /// cref="VariableClass.Name"/> which returns the localized string "Standard Morale".
        /// </para></remarks>

        public static readonly ResourceClass StandardMorale =
            new ResourceClass("standard-morale", Global.Strings.LabelStandardMorale);

        #endregion
        #region StandardStrength

        /// <summary>
        /// A read-only <see cref="ResourceClass"/> that represents the standard strength resource
        /// for units.</summary>
        /// <remarks><para>
        /// <b>StandardStrength</b> represents a pseudo-resource for user interfaces and does not
        /// appear in the <see cref="VariableSection.Resources"/> collection.
        /// </para><para>
        /// All <b>StandardStrength</b> properties have default values, except for <see
        /// cref="VariableClass.Id"/> which returns "standard-strength", and <see
        /// cref="VariableClass.Name"/> which returns the localized string "Standard Strength".
        /// </para></remarks>

        public static readonly ResourceClass StandardStrength =
            new ResourceClass("standard-strength", Global.Strings.LabelStandardStrength);

        #endregion
        #region Defeat

        /// <summary>
        /// Gets or sets the defeat threshold associated with faction resources.</summary>
        /// <value>
        /// An <see cref="Int32"/> value indicating the defeat threshold for faction resources that
        /// are instance values of the <see cref="ResourceClass"/>. The default is <see
        /// cref="Int32.MinValue"/>.</value>
        /// <exception cref="InvalidOperationException">
        /// The property is set, and <see cref="ApplicationInfo.IsEditor"/> is <c>false</c>.
        /// </exception>
        /// <remarks><para>
        /// <b>Defeat</b> holds the value of the "defeat" attribute of the nested "condition" XML
        /// element. The default value indicates the absence of that element. Any non-default value
        /// lies within <see cref="VariableClass.AbsoluteMinimum"/> and <see
        /// cref="VariableClass.AbsoluteMaximum"/>, including both limits.
        /// </para><para>
        /// A faction is considered defeated if one of its resources is equal to or less than the
        /// corresponding <b>Defeat</b> value. The default value allows comparisons against faction
        /// resources without first checking whether <b>Defeat</b> has a non-default value.
        /// </para></remarks>

        public int Defeat {
            [DebuggerStepThrough]
            get { return this._defeat; }
            [DebuggerStepThrough]
            set {
                ApplicationInfo.CheckEditor();
                this._defeat = value;
            }
        }

        #endregion
        #region IsDepletable

        /// <summary>
        /// Gets or sets a value indicating whether entity resources are considered depletable.
        /// </summary>
        /// <value>
        /// <c>true</c> if entity resources that are basic instance values of the <see
        /// cref="ResourceClass"/> are considered depletable; otherwise, <c>false</c>. The default
        /// is <c>false</c>.</value>
        /// <exception cref="InvalidOperationException">
        /// The property is set, and <see cref="ApplicationInfo.IsEditor"/> is <c>false</c>.
        /// </exception>
        /// <remarks><para>
        /// <b>IsDepletable</b> holds the value of the "deplete" attribute of the nested "modifier"
        /// XML element.
        /// </para><para>
        /// Depletable entity resources are expected to oscillate between their <see
        /// cref="VariableClass.Minimum"/> value and the maximum value indicated by <see
        /// cref="IsLimited"/>. Fully replenished resources are considered desirable, and fully
        /// depleted resources are considered undesirable.</para></remarks>

        public bool IsDepletable {
            [DebuggerStepThrough]
            get { return this._isDepletable; }
            [DebuggerStepThrough]
            set {
                ApplicationInfo.CheckEditor();
                this._isDepletable = value;
            }
        }

        #endregion
        #region IsLimited

        /// <summary>
        /// Gets or sets a value indicating whether entity resources are limited by their initial
        /// values.</summary>
        /// <value>
        /// <c>true</c> if entity resources that are basic instance values of the <see
        /// cref="ResourceClass"/> are limited by their initial values; <c>false</c> if they are
        /// only limited by the <see cref="VariableClass.Maximum"/> value. The default is
        /// <c>false</c>.</value>
        /// <exception cref="InvalidOperationException">
        /// The property is set, and <see cref="ApplicationInfo.IsEditor"/> is <c>false</c>.
        /// </exception>
        /// <remarks><para>
        /// <b>IsLimited</b> holds the value of the "limit" attribute of the nested "modifier" XML
        /// element.
        /// </para><para>
        /// The initial value of an entity resource is its corresponding <see
        /// cref="EntityClass.Resources"/> value, if one is defined; otherwise, the first value
        /// assigned by the rule script.</para></remarks>

        public bool IsLimited {
            [DebuggerStepThrough]
            get { return this._isLimited; }
            [DebuggerStepThrough]
            set {
                ApplicationInfo.CheckEditor();
                this._isLimited = value;
            }
        }

        #endregion
        #region IsResetting

        /// <summary>
        /// Gets or sets a value indicating whether faction resources are reset each turn.</summary>
        /// <value>
        /// <c>true</c> if faction resources that are basic instance values of the <see
        /// cref="ResourceClass"/> should be reset to their corresponding initial values each turn,
        /// before adding modifiers; <c>false</c> to let modifiers accumulate across turns. The
        /// default is <c>false</c>.</value>
        /// <exception cref="InvalidOperationException">
        /// The property is set, and <see cref="ApplicationInfo.IsEditor"/> is <c>false</c>.
        /// </exception>
        /// <remarks><para>
        /// <b>IsResetting</b> holds the value of the "reset" attribute of the nested "modifier" XML
        /// element.
        /// </para><para>
        /// The initial value of a faction resource is its corresponding <see
        /// cref="FactionClass.Resources"/> value, if one is defined; otherwise, the first value
        /// assigned by the rule script.</para></remarks>

        public bool IsResetting {
            [DebuggerStepThrough]
            get { return this._isResetting; }
            [DebuggerStepThrough]
            set {
                ApplicationInfo.CheckEditor();
                this._isResetting = value;
            }
        }

        #endregion
        #region Victory

        /// <summary>
        /// Gets or sets the victory threshold associated with faction resources.</summary>
        /// <value>
        /// An <see cref="Int32"/> value indicating the victory threshold for faction resources that
        /// are instance values of the <see cref="ResourceClass"/>. The default is <see
        /// cref="Int32.MaxValue"/>.</value>
        /// <exception cref="InvalidOperationException">
        /// The property is set, and <see cref="ApplicationInfo.IsEditor"/> is <c>false</c>.
        /// </exception>
        /// <remarks><para>
        /// <b>Victory</b> holds the value of the "victory" attribute of the nested "condition" XML
        /// element. The default value indicates the absence of that element. Any non-default value
        /// lies within <see cref="VariableClass.AbsoluteMinimum"/> and <see
        /// cref="VariableClass.AbsoluteMaximum"/>, including both limits.
        /// </para><para>
        /// A faction is considered victorious if one of its resources is equal to or greater than
        /// the corresponding <b>Victory</b> value. The default value allows comparisons against
        /// faction resources without first checking whether <b>Victory</b> has a non-default value.
        /// </para></remarks>

        public int Victory {
            [DebuggerStepThrough]
            get { return this._victory; }
            [DebuggerStepThrough]
            set {
                ApplicationInfo.CheckEditor();
                this._victory = value;
            }
        }

        #endregion
        #region ICloneable Members

        /// <summary>
        /// Creates a new <see cref="ResourceClass"/> object that is a deep copy of the current
        /// instance.</summary>
        /// <returns>
        /// A new <see cref="ResourceClass"/> object that is a deep copy of the current instance.
        /// </returns>
        /// <remarks>
        /// <b>Clone</b> calls the <see cref="ResourceClass(ResourceClass)"/> copy constructor with
        /// this <see cref="ResourceClass"/> object.</remarks>

        public override object Clone() {
            return new ResourceClass(this);
        }

        #endregion
        #region XmlSerializable Members
        #region ConstXmlName

        /// <summary>
        /// The name of the XML element associated with the <see cref="ResourceClass"/> class.
        /// </summary>
        /// <remarks>
        /// <b>ConstXmlName</b> holds the value "resource", indicating the XML element in <see
        /// cref="FilePaths.ScenarioSchema"/> whose data is managed by the <see
        /// cref="ResourceClass"/> class.</remarks>

        public const string ConstXmlName = "resource";

        #endregion
        #region ReadXmlElements

        /// <summary>
        /// Reads XML element data into the <see cref="ResourceClass"/> object using the specified
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

                case "condition":
                    XmlUtility.ReadAttributeAsInt32(reader, "defeat", ref this._defeat);
                    XmlUtility.ReadAttributeAsInt32(reader, "victory", ref this._victory);
                    return true;

                case "modifier":
                    XmlUtility.ReadAttributeAsBoolean(reader, "deplete", ref this._isDepletable);
                    XmlUtility.ReadAttributeAsBoolean(reader, "limit", ref this._isLimited);
                    XmlUtility.ReadAttributeAsBoolean(reader, "reset", ref this._isResetting);
                    return true;

                default: return false;
            }
        }

        #endregion
        #region WriteXmlElements

        /// <summary>
        /// Writes all current data of the <see cref="ResourceClass"/> object that is serialized to
        /// nested XML elements to the specified <see cref="XmlWriter"/>.</summary>
        /// <param name="writer">
        /// The <see cref="XmlWriter"/> to which to write.</param>
        /// <remarks>
        /// Please refer to class <see cref="XmlSerializable"/> for a complete description of this
        /// method.</remarks>

        protected override void WriteXmlElements(XmlWriter writer) {
            base.WriteXmlElements(writer);

            // write "condition" element if necessary
            if (Defeat != Int32.MinValue || Victory != Int32.MaxValue) {
                writer.WriteStartElement("condition");

                if (Defeat != Int32.MinValue)
                    writer.WriteAttributeString("defeat", XmlConvert.ToString(Defeat));
                if (Victory != Int32.MaxValue)
                    writer.WriteAttributeString("victory", XmlConvert.ToString(Victory));

                writer.WriteEndElement();
            }

            // write "modifier" element if necessary
            if (IsDepletable || IsLimited || IsResetting) {
                writer.WriteStartElement("modifier");

                if (IsDepletable)
                    writer.WriteAttributeString("deplete", XmlConvert.ToString(IsDepletable));
                if (IsLimited)
                    writer.WriteAttributeString("limit", XmlConvert.ToString(IsLimited));
                if (IsResetting)
                    writer.WriteAttributeString("reset", XmlConvert.ToString(IsResetting));

                writer.WriteEndElement();
            }
        }

        #endregion
        #endregion
    }
}
