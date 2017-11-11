using System;
using System.Diagnostics;
using System.Xml;

using Tektosyne.Xml;
using Hexkit.Global;

namespace Hexkit.Scenario {

    /// <summary>
    /// Represents a class of unit entities.</summary>
    /// <remarks>
    /// <b>UnitClass</b> corresponds to the complex XML type "unitClass" defined in <see
    /// cref="FilePaths.ScenarioSchema"/>. Instances are serialized to the XML element "unit".
    /// </remarks>

    public sealed class UnitClass: EntityClass {
        #region UnitClass()

        /// <overloads>
        /// Initializes a new instance of the <see cref="UnitClass"/> class.</overloads>
        /// <summary>
        /// Initializes a new instance of the <see cref="UnitClass"/> class with default properties.
        /// </summary>
        /// <remarks>
        /// The <see cref="EntityClass.Category"/> property is initialized to a value of <see
        /// cref="EntityCategory.Unit"/>.</remarks>

        internal UnitClass(): base("", EntityCategory.Unit) { }

        #endregion
        #region UnitClass(String)

        /// <summary>
        /// Initializes a new instance of the <see cref="UnitClass"/> class with the specified
        /// identifier.</summary>
        /// <param name="id">
        /// The initial value for the <see cref="EntityClass.Id"/> property.</param>
        /// <remarks>
        /// The <see cref="EntityClass.Category"/> property is initialized to a value of <see
        /// cref="EntityCategory.Unit"/>.</remarks>

        public UnitClass(string id): base(id, EntityCategory.Unit) { }

        #endregion
        #region UnitClass(UnitClass)

        /// <summary>
        /// Initializes a new instance of the <see cref="UnitClass"/> class with property values
        /// copied from the specified instance.</summary>
        /// <param name="unit">
        /// The <see cref="UnitClass"/> instance whose property values are copied to the new
        /// instance.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="unit"/> is a null reference.</exception>
        /// <remarks>
        /// This constructor is called by <see cref="Clone"/> to perform a deep copy of the
        /// specified <paramref name="unit"/>, including a deep copy of all mutable objects that are
        /// owned by the <paramref name="unit"/>.</remarks>

        public UnitClass(UnitClass unit): base(unit) {

            this._canCapture = unit._canCapture;
            this._canDefendOnly = unit._canDefendOnly;
            this._canHeal = unit._canHeal;
            this._rangedAttack = unit._rangedAttack;

            this._attackRangeAttribute = unit._attackRangeAttribute;
            this._movementAttribute = unit._movementAttribute;
            this._moraleResource = unit._moraleResource;
            this._strengthResource = unit._strengthResource;
        }

        #endregion
        #region Private Fields

        // property backers
        private bool _canCapture, _canDefendOnly, _canHeal;
        private TargetMode _rangedAttack = TargetMode.Any;

        private string _attackRangeAttribute, _movementAttribute;
        private string _moraleResource, _strengthResource;

        #endregion
        #region AttackRangeAttribute

        /// <summary>
        /// Gets or sets the identifier of the <see cref="EntityClass.Attributes"/> element that
        /// represents the maximum attack range for units based on the <see cref="UnitClass"/>.
        /// </summary>
        /// <value>
        /// The identifier of the <see cref="EntityClass.Attributes"/> element that represents the
        /// maximum attack range for units based on the <see cref="UnitClass"/>. The default is an
        /// empty string, indicating that no such <see cref="EntityClass.Attributes"/> element
        /// exists.</value>
        /// <exception cref="InvalidOperationException">
        /// The property is set, and <see cref="ApplicationInfo.IsEditor"/> is <c>false</c>.
        /// </exception>
        /// <remarks>
        /// <b>AttackRangeAttribute</b> returns an empty string when set to a null reference. This
        /// property holds the value of the "id" attribute of the "attackRangeAttribute" XML
        /// element.</remarks>

        public string AttackRangeAttribute {
            [DebuggerStepThrough]
            get { return this._attackRangeAttribute ?? ""; }
            [DebuggerStepThrough]
            set {
                ApplicationInfo.CheckEditor();
                if (value != null) value = String.Intern(value);
                this._attackRangeAttribute = value;
            }
        }

        #endregion
        #region CanCapture

        /// <summary>
        /// Gets or sets a value indicating whether units based on the <see cref="UnitClass"/> may
        /// capture map sites for their owner.</summary>
        /// <value>
        /// <c>true</c> if units based on the <see cref="UnitClass"/> may capture map sites for
        /// their owner; otherwise, <c>false</c>. The default is <c>false</c>.</value>
        /// <exception cref="InvalidOperationException">
        /// The property is set, and <see cref="ApplicationInfo.IsEditor"/> is <c>false</c>.
        /// </exception>
        /// <remarks>
        /// <b>CanCapture</b> holds the value of the "capture" XML attribute. If this property is
        /// <c>true</c>, units based on the <see cref="UnitClass"/> transfer any site they occupy to
        /// the faction that owns the unit, provided that the site contains at least one terrain
        /// based on a <see cref="TerrainClass"/> whose <see cref="TerrainClass.CanCapture"/>
        /// property is also <c>true</c>. Otherwise, the site ownership remains unchanged.</remarks>

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
        #region CanDefendOnly

        /// <summary>
        /// Gets or sets a value indicating whether units based on the <see cref="UnitClass"/> are
        /// limited to passive defense.</summary>
        /// <value>
        /// <c>true</c> if units based on the <see cref="UnitClass"/> are limited to passive
        /// defense; otherwise, <c>false</c>. The default is <c>false</c>.</value>
        /// <exception cref="InvalidOperationException">
        /// The property is set, and <see cref="ApplicationInfo.IsEditor"/> is <c>false</c>.
        /// </exception>
        /// <remarks>
        /// <b>CanDefendOnly</b> holds the value of the "defendOnly" XML attribute. If this property
        /// is <c>true</c>, units based on the <see cref="UnitClass"/> may not actively attack other
        /// sites, but only passively defend themselves when attacked, assuming they are capable of
        /// combat in the first place.</remarks>

        public bool CanDefendOnly {
            [DebuggerStepThrough]
            get { return this._canDefendOnly; }
            [DebuggerStepThrough]
            set {
                ApplicationInfo.CheckEditor();
                this._canDefendOnly = value;
            }
        }

        #endregion
        #region CanDestroy

        /// <summary>
        /// Gets or sets a value indicating whether units based on the <see cref="UnitClass"/> may
        /// be destroyed by their owner.</summary>
        /// <value>
        /// Always <c>true</c>.</value>
        /// <exception cref="InvalidOperationException">
        /// The property is set.</exception>
        /// <remarks>
        /// The <see cref="UnitClass"/> implementation of <b>CanDestroy</b> always returns
        /// <c>true</c> and cannot be set. Only instances of <see cref="TerrainClass"/> may change
        /// this property.</remarks>

        public override bool CanDestroy {
            [DebuggerStepThrough]
            get { return true; }
        }

        #endregion
        #region CanHeal

        /// <summary>
        /// Gets or sets a value indicating whether units based on the <see cref="UnitClass"/> may
        /// replenish their strength after taking damage.</summary>
        /// <value>
        /// <c>true</c> if units based on the <see cref="UnitClass"/> may replenish their strength
        /// after taking damage; otherwise, <c>false</c>. The default is <c>false</c>.</value>
        /// <exception cref="InvalidOperationException">
        /// The property is set, and <see cref="ApplicationInfo.IsEditor"/> is <c>false</c>.
        /// </exception>
        /// <remarks>
        /// <b>CanHeal</b> holds the value of the "healing" XML attribute. If this property is
        /// <c>true</c>, the scenario provides some way for units based on the <see
        /// cref="UnitClass"/> to replenish their strength after taking damage. Otherwise, their
        /// strength can only decrease but never increase.</remarks>

        public bool CanHeal {
            [DebuggerStepThrough]
            get { return this._canHeal; }
            [DebuggerStepThrough]
            set {
                ApplicationInfo.CheckEditor();
                this._canHeal = value;
            }
        }

        #endregion
        #region MoraleResource

        /// <summary>
        /// Gets or sets the identifier of the <see cref="EntityClass.Resources"/> element that
        /// represents the current morale of units based on the <see cref="UnitClass"/>.</summary>
        /// <value>
        /// The identifier of the <see cref="EntityClass.Resources"/> element that represents the
        /// current morale of units based on the <see cref="UnitClass"/>. The default is an empty
        /// string, indicating that no such <see cref="EntityClass.Resources"/> element exists.
        /// </value>
        /// <exception cref="InvalidOperationException">
        /// The property is set, and <see cref="ApplicationInfo.IsEditor"/> is <c>false</c>.
        /// </exception>
        /// <remarks>
        /// <b>MoraleResource</b> returns an empty string when set to a null reference. This
        /// property holds the value of the "id" attribute of the "moraleResource" XML element.
        /// </remarks>

        public string MoraleResource {
            [DebuggerStepThrough]
            get { return this._moraleResource ?? ""; }
            [DebuggerStepThrough]
            set {
                ApplicationInfo.CheckEditor();
                if (value != null) value = String.Intern(value);
                this._moraleResource = value;
            }
        }

        #endregion
        #region MovementAttribute

        /// <summary>
        /// Gets or sets the identifier of the <see cref="EntityClass.Attributes"/> element that
        /// represents the maximum movement cost for units based on the <see cref="UnitClass"/>.
        /// </summary>
        /// <value>
        /// The identifier of the <see cref="EntityClass.Attributes"/> element that represents the
        /// maximum movement cost for units based on the <see cref="UnitClass"/>. The default is an
        /// empty string, indicating that no such <see cref="EntityClass.Attributes"/> element
        /// exists.</value>
        /// <exception cref="InvalidOperationException">
        /// The property is set, and <see cref="ApplicationInfo.IsEditor"/> is <c>false</c>.
        /// </exception>
        /// <remarks>
        /// <b>MovementAttribute</b> returns an empty string when set to a null reference. This
        /// property holds the value of the "id" attribute of the "movementAttribute" XML element.
        /// </remarks>

        public string MovementAttribute {
            [DebuggerStepThrough]
            get { return this._movementAttribute ?? ""; }
            [DebuggerStepThrough]
            set {
                ApplicationInfo.CheckEditor();
                if (value != null) value = String.Intern(value);
                this._movementAttribute = value;
            }
        }

        #endregion
        #region RangedAttack

        /// <summary>
        /// Gets or sets a value indicating valid targets for ranged attacks by units based on the
        /// <see cref="UnitClass"/>.</summary>
        /// <value>
        /// A <see cref="TargetMode"/> value indicating valid targets for ranged attacks by units
        /// based on the <see cref="UnitClass"/>. The default is <see cref="TargetMode.Any"/>.
        /// </value>
        /// <exception cref="InvalidOperationException">
        /// The property is set, and <see cref="ApplicationInfo.IsEditor"/> is <c>false</c>.
        /// </exception>
        /// <remarks>
        /// <b>RangedAttack</b> holds the value of the "rangedAttack" XML attribute.</remarks>

        public TargetMode RangedAttack {
            [DebuggerStepThrough]
            get { return this._rangedAttack; }
            [DebuggerStepThrough]
            set {
                ApplicationInfo.CheckEditor();
                this._rangedAttack = value;
            }
        }

        #endregion
        #region StrengthResource

        /// <summary>
        /// Gets or sets the identifier of the <see cref="EntityClass.Resources"/> element that
        /// represents the current strength of units based on the <see cref="UnitClass"/>.</summary>
        /// <value>
        /// The identifier of the <see cref="EntityClass.Resources"/> element that represents the
        /// current strength of units based on the <see cref="UnitClass"/>. The default is an empty
        /// string, indicating that no such <see cref="EntityClass.Resources"/> element exists.
        /// </value>
        /// <exception cref="InvalidOperationException">
        /// The property is set, and <see cref="ApplicationInfo.IsEditor"/> is <c>false</c>.
        /// </exception>
        /// <remarks>
        /// <b>StrengthResource</b> returns an empty string when set to a null reference. This
        /// property holds the value of the "id" attribute of the "strengthResource" XML element.
        /// </remarks>

        public string StrengthResource {
            [DebuggerStepThrough]
            get { return this._strengthResource ?? ""; }
            [DebuggerStepThrough]
            set {
                ApplicationInfo.CheckEditor();
                if (value != null) value = String.Intern(value);
                this._strengthResource = value;
            }
        }

        #endregion
        #region ICloneable Members

        /// <summary>
        /// Creates a new <see cref="UnitClass"/> object that is a deep copy of the current
        /// instance.</summary>
        /// <returns>
        /// A new <see cref="UnitClass"/> object that is a deep copy of the current instance.
        /// </returns>
        /// <remarks>
        /// <b>Clone</b> calls the <see cref="UnitClass(UnitClass)"/> copy constructor with this
        /// <see cref="UnitClass"/> object.</remarks>

        public override object Clone() {
            return new UnitClass(this);
        }

        #endregion
        #region XmlSerializable Members
        #region ConstXmlName

        /// <summary>
        /// The name of the XML element associated with the <see cref="UnitClass"/> class.
        /// </summary>
        /// <remarks>
        /// <b>ConstXmlName</b> holds the value "unit", indicating the XML element in <see
        /// cref="FilePaths.ScenarioSchema"/> whose data is managed by the <see cref="UnitClass"/>
        /// class.</remarks>

        public const string ConstXmlName = "unit";

        #endregion
        #region ReadXmlAttributes

        /// <summary>
        /// Reads XML attribute data into the <see cref="UnitClass"/> object using the specified
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

            XmlUtility.ReadAttributeAsBoolean(reader, "capture", ref this._canCapture);
            XmlUtility.ReadAttributeAsBoolean(reader, "defendOnly", ref this._canDefendOnly);
            XmlUtility.ReadAttributeAsBoolean(reader, "healing", ref this._canHeal);
            XmlUtility.ReadAttributeAsEnum(reader, "rangedAttack", ref this._rangedAttack);
        }

        #endregion
        #region ReadXmlElements

        /// <summary>
        /// Reads XML element data into the <see cref="UnitClass"/> object using the specified <see
        /// cref="XmlReader"/>.</summary>
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

                case "attackRangeAttribute":
                    this._attackRangeAttribute = SimpleXml.ReadIdentifier(reader);
                    return true;

                case "movementAttribute":
                    this._movementAttribute = SimpleXml.ReadIdentifier(reader);
                    return true;

                case "moraleResource":
                    this._moraleResource = SimpleXml.ReadIdentifier(reader);
                    return true;

                case "strengthResource":
                    this._strengthResource = SimpleXml.ReadIdentifier(reader);
                    return true;

                default: return false;
            }
        }

        #endregion
        #region WriteXmlAttributes

        /// <summary>
        /// Writes all current data of the <see cref="UnitClass"/> object that is serialized to XML
        /// attributes to the specified <see cref="XmlWriter"/>.</summary>
        /// <param name="writer">
        /// The <see cref="XmlWriter"/> to which to write.</param>
        /// <remarks>
        /// Please refer to class <see cref="XmlSerializable"/> for a complete description of this
        /// method.</remarks>

        protected override void WriteXmlAttributes(XmlWriter writer) {
            base.WriteXmlAttributes(writer);

            if (CanCapture)
                writer.WriteAttributeString("capture", XmlConvert.ToString(CanCapture));
            if (CanDefendOnly)
                writer.WriteAttributeString("defendOnly", XmlConvert.ToString(CanDefendOnly));
            if (CanHeal)
                writer.WriteAttributeString("healing", XmlConvert.ToString(CanHeal));
            if (RangedAttack != TargetMode.Any)
                writer.WriteAttributeString("rangedAttack", RangedAttack.ToString());
        }

        #endregion
        #region WriteXmlElements

        /// <summary>
        /// Writes all current data of the <see cref="UnitClass"/> object that is serialized to
        /// nested XML elements to the specified <see cref="XmlWriter"/>.</summary>
        /// <param name="writer">
        /// The <see cref="XmlWriter"/> to which to write.</param>
        /// <remarks>
        /// Please refer to class <see cref="XmlSerializable"/> for a complete description of this
        /// method.</remarks>

        protected override void WriteXmlElements(XmlWriter writer) {
            base.WriteXmlElements(writer);

            SimpleXml.WriteIdentifier(writer, "attackRangeAttribute", AttackRangeAttribute);
            SimpleXml.WriteIdentifier(writer, "movementAttribute", MovementAttribute);
            SimpleXml.WriteIdentifier(writer, "moraleResource", MoraleResource);
            SimpleXml.WriteIdentifier(writer, "strengthResource", StrengthResource);
        }

        #endregion
        #endregion
    }
}
