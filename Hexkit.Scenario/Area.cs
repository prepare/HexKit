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

    using EntityTemplateList = ListEx<EntityTemplate>;
    using RectIList = ListEx<RectI>;

    #endregion

    /// <summary>
    /// Represents a map area bounded by one or more rectangles.</summary>
    /// <remarks>
    /// <b>Area</b> is serialized to the XML element "area" defined in <see
    /// cref="FilePaths.ScenarioSchema"/>.</remarks>

    public sealed class Area: ScenarioElement, IEquatable<Area> {
        #region Area()

        /// <overloads>
        /// Initializes a new instance of the <see cref="Area"/> class.</overloads>
        /// <summary>
        /// Initializes a new instance of the <see cref="Area"/> class with default properties.
        /// </summary>

        internal Area() { }

        #endregion
        #region Area(Int32, Int32, String, String)

        /// <summary>
        /// Initializes a new instance of the <see cref="Area"/> class with the specified
        /// coordinates, terrain owner, and unit owner.</summary>
        /// <param name="x">
        /// The x-coordinate of the first element of the <see cref="Bounds"/> collection.</param>
        /// <param name="y">
        /// The y-coordinate of the first element of the <see cref="Bounds"/> collection.</param>
        /// <param name="owner">
        /// The initial value for the <see cref="Owner"/> property.</param>
        /// <param name="unitOwner">
        /// The initial value for the <see cref="UnitOwner"/> property.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="x"/> or <paramref name="y"/> is less than zero.</exception>
        /// <remarks>
        /// This constructor adds one <see cref="RectI"/> to the <see cref="Bounds"/> collection
        /// whose location equals the specified (<paramref name="x"/>,<paramref name="y"/>)
        /// coordinates and whose size equals (1,1).</remarks>

        public Area(int x, int y, string owner, string unitOwner): this() {
            if (x < 0)
                ThrowHelper.ThrowArgumentOutOfRangeException(
                    "x", x, Tektosyne.Strings.ArgumentNegative);
            if (y < 0)
                ThrowHelper.ThrowArgumentOutOfRangeException(
                    "y", y, Tektosyne.Strings.ArgumentNegative);

            this._bounds.Add(new RectI(x, y, 1, 1));
            this._owner = owner;
            this._unitOwner = unitOwner;
        }

        #endregion
        #region Private Fields

        // property backers
        private string _owner, _unitOwner;
        private readonly RectIList _bounds = new RectIList();

        private readonly EntityTemplateList
            _units = new EntityTemplateList(),
            _terrains = new EntityTemplateList(),
            _effects = new EntityTemplateList();

        #endregion
        #region Bounds

        /// <summary>
        /// Gets a list of all map areas covered by the <see cref="Area"/>.</summary>
        /// <value>
        /// A <see cref="RectIList"/> whose elements indicate the map areas covered by the <see
        /// cref="Area"/>. The default is an empty collection.</value>
        /// <remarks><para>
        /// <b>Bounds</b> never returns a null reference. This property holds the values of all
        /// "bounds" XML elements.
        /// </para><para>
        /// Each <see cref="RectI"/> instance specifies a set of map locations within the bounds of
        /// the <see cref="AreaSection.MapGrid"/>. The <see cref="Area"/> data is copied to all map
        /// sites at any location within any <b>Bounds</b> element.</para></remarks>

        public RectIList Bounds {
            [DebuggerStepThrough]
            get { return this._bounds; }
        }

        #endregion
        #region IsEmpty

        /// <summary>
        /// Gets a value indicating whether the <see cref="Area"/> holds no contents.</summary>
        /// <value>
        /// <c>true</c> if the <see cref="Units"/>, <see cref="Terrains"/>, and <see
        /// cref="Effects"/> collections are all empty and <see cref="Owner"/> is an empty string;
        /// otherwise, <c>false</c>.</value>
        /// <remarks><para>
        /// <b>IsEmpty</b> ignores the <see cref="Bounds"/> property, and also the <see
        /// cref="UnitOwner"/> property which is irrelevant without any present <see cref="Units"/>.
        /// </para><para>
        /// <b>IsEmpty</b> is a helper property for <see cref="AreaSection.PackAreas"/>.
        /// </para></remarks>

        internal bool IsEmpty {
            get {
                return (Owner.Length == 0
                    && Terrains.Count == 0
                    && Units.Count == 0
                    && Effects.Count == 0);
            }
        }

        #endregion
        #region Owner

        /// <summary>
        /// Gets or sets the identifier of the <see cref="FactionClass"/> that represents the owner
        /// of the <see cref="Area"/>.</summary>
        /// <value><para>
        /// The <see cref="FactionClass.Id"/> string of the <see cref="FactionClass"/> that
        /// represents the owner of the <see cref="Area"/>.
        /// </para><para>-or-</para><para>
        /// An empty string to indicate an unowned <see cref="Area"/>. The default is an empty
        /// string.</para></value>
        /// <exception cref="InvalidOperationException">
        /// The property is set, and <see cref="ApplicationInfo.IsEditor"/> is <c>false</c>.
        /// </exception>
        /// <remarks><para>
        /// <b>Owner</b> holds the value of the "owner" XML attribute, or an empty string if the
        /// attribute is missing. This property returns an empty string when set to a null
        /// reference.
        /// </para><para>
        /// <b>Owner</b> indicates the faction that should own all map sites covered by the <see
        /// cref="Bounds"/> collection. The local <see cref="Units"/> may be assigned a different
        /// owner, as indicated by the <see cref="UnitOwner"/> property.</para></remarks>

        public string Owner {
            [DebuggerStepThrough]
            get { return this._owner ?? ""; }
            [DebuggerStepThrough]
            set {
                ApplicationInfo.CheckEditor();
                if (value != null) value = String.Intern(value);
                this._owner = value;
            }
        }

        #endregion
        #region Effects

        /// <summary>
        /// Gets the effect stack for the <see cref="Area"/>.</summary>
        /// <value>
        /// A <see cref="EntityTemplateList"/> whose elements are of the <see
        /// cref="EntityCategory.Effect"/> category. The default is an empty collection.</value>
        /// <remarks><para>
        /// <b>Effects</b> never returns a null reference. This property holds the values of all
        /// "effect" XML elements.
        /// </para><para>
        /// <b>Effects</b> defines the effect stack for all map sites covered by the <see
        /// cref="Bounds"/> collection.</para></remarks>

        public EntityTemplateList Effects {
            [DebuggerStepThrough]
            get { return this._effects; }
        }

        #endregion
        #region Terrains

        /// <summary>
        /// Gets the terrain stack for the <see cref="Area"/>.</summary>
        /// <value>
        /// A <see cref="EntityTemplateList"/> whose elements are of the <see
        /// cref="EntityCategory.Terrain"/> category. The default is an empty collection.</value>
        /// <remarks><para>
        /// <b>Terrains</b> never returns a null reference. This property holds the values of all
        /// "terrain" XML elements.
        /// </para><para>
        /// <b>Terrains</b> defines the terrain stack for all map sites covered by the <see
        /// cref="Bounds"/> collection.</para></remarks>

        public EntityTemplateList Terrains {
            [DebuggerStepThrough]
            get { return this._terrains; }
        }

        #endregion
        #region UnitOwner

        /// <summary>
        /// Gets or sets the identifier of the <see cref="FactionClass"/> that represents the owner
        /// of all <see cref="Units"/>.</summary>
        /// <value><para>
        /// The <see cref="FactionClass.Id"/> string of the <see cref="FactionClass"/> that
        /// represents the owner of all elements in the <see cref="Units"/> collection.
        /// </para><para>-or-</para><para>
        /// An empty string to indicate that the elements in the <see cref="Units"/> collection
        /// belong to the <see cref="Owner"/> of the <see cref="Area"/>. The default is an empty
        /// string.</para></value>
        /// <exception cref="InvalidOperationException">
        /// The property is set, and <see cref="ApplicationInfo.IsEditor"/> is <c>false</c>.
        /// </exception>
        /// <remarks>
        /// <b>Owner</b> holds the value of the "unitOwner" XML attribute, or an empty string if the
        /// attribute is missing. This property returns an empty string when set to a null
        /// reference.</remarks>

        public string UnitOwner {
            [DebuggerStepThrough]
            get { return this._unitOwner ?? ""; }
            [DebuggerStepThrough]
            set {
                ApplicationInfo.CheckEditor();
                if (value != null) value = String.Intern(value);
                this._unitOwner = value;
            }
        }

        #endregion
        #region Units

        /// <summary>
        /// Gets the unit stack for the <see cref="Area"/>.</summary>
        /// <value>
        /// A <see cref="EntityTemplateList"/> whose elements are of the <see
        /// cref="EntityCategory.Unit"/> category. The default is an empty collection.</value>
        /// <remarks><para>
        /// <b>Units</b> never returns a null reference. This property holds the values of all
        /// "unit" XML elements.
        /// </para><para>
        /// <b>Units</b> defines the unit stack for all map sites covered by the <see
        /// cref="Bounds"/> collection.</para></remarks>

        public EntityTemplateList Units {
            [DebuggerStepThrough]
            get { return this._units; }
        }

        #endregion
        #region Clear

        /// <summary>
        /// Clears the contents of the <see cref="Area"/>.</summary>
        /// <remarks><para>
        /// <b>Clear</b> clears the <see cref="Units"/>, <see cref="Terrains"/>, and <see
        /// cref="Effects"/> collections, and also resets <see cref="Owner"/> and <see
        /// cref="UnitOwner"/> to empty strings. The <see cref="Bounds"/> property remains
        /// unaffected.
        /// </para><para>
        /// <b>Clear</b> is a helper method for <see cref="AreaSection.PackAreas"/>.
        /// </para></remarks>

        internal void Clear() {
            this._owner = this._unitOwner = null;

            this._units.Clear();
            this._terrains.Clear();
            this._effects.Clear();
        }

        #endregion
        #region GetHashCode

        /// <summary>
        /// Returns the hash code for this <see cref="Area"/> instance.</summary>
        /// <returns>
        /// An <see cref="Int32"/> hash code.</returns>
        /// <remarks>
        /// <b>GetHashCode</b> returns the result of <see cref="String.GetHashCode"/> for the <see
        /// cref="Owner"/> property of this <see cref="Area"/> instance.</remarks>

        public override int GetHashCode() {
            return Owner.GetHashCode();
        }

        #endregion
        #region IEquatable Members
        #region Equals(Object)

        /// <overloads>
        /// Determines whether two <see cref="Area"/> instances have the same value.</overloads>
        /// <summary>
        /// Determines whether this <see cref="Area"/> instance and a specified object, which must
        /// be a <see cref="Area"/> object, have the same value.</summary>
        /// <param name="obj">
        /// An <see cref="Object"/> to compare to this <see cref="Area"/> instance.</param>
        /// <returns>
        /// <c>true</c> if <paramref name="obj"/> is another <see cref="Area"/> instance and its
        /// value is the same as this instance; otherwise, <c>false</c>.</returns>
        /// <remarks>
        /// If the specified <paramref name="obj"/> is another <see cref="Area"/> instance, this
        /// method invokes the strongly typed <see cref="Equals(Area)"/> method to test the two
        /// instances for value equality.</remarks>

        public override bool Equals(object obj) {

            Area area = obj as Area;
            if (Object.ReferenceEquals(area, null))
                return false;

            return Equals(area);
        }

        #endregion
        #region Equals(Area)

        /// <summary>
        /// Determines whether this instance and a specified <see cref="Area"/> object have the same
        /// value.</summary>
        /// <param name="area">
        /// Another <see cref="Area"/> object to compare to this instance.</param>
        /// <returns>
        /// <c>true</c> if the value of <paramref name="area"/> is the same as this instance;
        /// otherwise, <c>false</c>.</returns>
        /// <remarks><para>
        /// This method compares the values of the <see cref="Owner"/> and <see cref="UnitOwner"/>
        /// properties and the contents of the <see cref="Terrains"/>, <see cref="Units"/>, and <see
        /// cref="Effects"/> collections of the two <see cref="Area"/> objects to test for value
        /// equality.
        /// </para><para>
        /// The contents of the <see cref="Bounds"/> collections are not compared because the
        /// intention is to test for identical data in different map locations.</para></remarks>

        public bool Equals(Area area) {
            if (Object.ReferenceEquals(area, null))
                return false;

            if (Owner != area.Owner || UnitOwner != area.UnitOwner)
                return false;

            if (Terrains.Count != area.Terrains.Count ||
                Units.Count != area.Units.Count ||
                Effects.Count != area.Effects.Count)
                return false;

            for (int i = 0; i < Terrains.Count; i++)
                if (!Terrains[i].Equals(area.Terrains[i]))
                    return false;

            for (int i = 0; i < Units.Count; i++)
                if (!Units[i].Equals(area.Units[i]))
                    return false;

            for (int i = 0; i < Effects.Count; i++)
                if (!Effects[i].Equals(area.Effects[i]))
                    return false;

            return true;
        }

        #endregion
        #region Equals(Area, Area)

        /// <summary>
        /// Determines whether two specified <see cref="Area"/> objects have the same value.
        /// </summary>
        /// <param name="x">
        /// The first <see cref="Area"/> object to compare.</param>
        /// <param name="y">
        /// The second <see cref="Area"/> object to compare.</param>
        /// <returns>
        /// <c>true</c> if the value of <paramref name="x"/> is the same as the value of <paramref
        /// name="y"/>; otherwise, <c>false</c>.</returns>
        /// <remarks>
        /// This method invokes the <see cref="Equals(Area)"/> instance method to test the two <see
        /// cref="Area"/> objects for value equality.</remarks>

        public static bool Equals(Area x, Area y) {

            if (Object.ReferenceEquals(x, null))
                return Object.ReferenceEquals(y, null);

            if (Object.ReferenceEquals(y, null))
                return false;

            return x.Equals(y);
        }

        #endregion
        #endregion
        #region ScenarioElement Members
        #region ProcessIdentifier

        /// <summary>
        /// Counts, changes, or deletes all occurrences of the specified identifier in the <see
        /// cref="Area"/>.</summary>
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
        /// The number of occurrences of <paramref name="oldId"/> in the <see cref="Area"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="oldId"/> is a null reference.</exception>
        /// <remarks><para>
        /// <b>ProcessIdentifier</b> processes the <see cref="Area"/> properties as follows:
        /// </para><list type="table"><listheader>
        /// <term>Property</term><description>Processing</description>
        /// </listheader><item>
        /// <term><see cref="Owner"/><br/> <see cref="UnitOwner"/></term>
        /// <description>As identifier</description>
        /// </item><item>
        /// <term><see cref="Effects"/><br/> <see cref="Terrains"/><br/> <see cref="Units"/>
        /// </term><description>By value</description>
        /// </item></list></remarks>

        internal override int ProcessIdentifier(string oldId, string newId) {
            int count = 0;

            // process area owner ID
            if (Owner == oldId) {
                ++count;
                if (newId != oldId) this._owner = newId;
            }

            // process unit owner ID
            if (UnitOwner == oldId) {
                ++count;
                if (newId != oldId) this._unitOwner = newId;
            }

            // process IDs in entity lists
            foreach (EntityTemplate entity in Units)
                count += entity.ProcessIdentifier(oldId, newId);

            foreach (EntityTemplate entity in Terrains)
                count += entity.ProcessIdentifier(oldId, newId);

            foreach (EntityTemplate entity in Effects)
                count += entity.ProcessIdentifier(oldId, newId);

            return count;
        }

        #endregion
        #region Validate

        /// <summary>
        /// Validates the current data of the <see cref="Area"/>.</summary>
        /// <exception cref="XmlException">
        /// Validation failed, and <see cref="ApplicationInfo.IsEditor"/> is <c>false</c>.
        /// </exception>
        /// <remarks><para>
        /// <b>Validate</b> processes the <see cref="Area"/> properties as follows:
        /// </para><list type="table"><listheader>
        /// <term>Property</term><description>Processing</description>
        /// </listheader><item>
        /// <term><see cref="Bounds"/></term>
        /// <description>Check against <see cref="AreaSection.MapGrid"/></description>
        /// </item><item>
        /// <term><see cref="Owner"/><br/> <see cref="UnitOwner"/></term>
        /// <description>Check identifier</description>
        /// </item><item>
        /// <term><see cref="Effects"/><br/> <see cref="Units"/></term>
        /// <description>Check identifiers</description>
        /// </item><item>
        /// <term><see cref="Terrains"/></term>
        /// <description>Check identifiers and optimize background terrain</description>
        /// </item></list><para>
        /// Checks and background terrain optimization are only performed if <see
        /// cref="ApplicationInfo.IsEditor"/> is <c>false</c>. Please refer to <see
        /// cref="EntitySection.ValidateCollection"/> for details on terrain optimization.
        /// </para></remarks>

        internal override void Validate() {

            // return immediately if editing
            if (ApplicationInfo.IsEditor) return;

            AreaSection areas = MasterSection.Instance.Areas;
            EntitySection entities = MasterSection.Instance.Entities;
            var factions = MasterSection.Instance.Factions.Collection;

            // check area owner ID
            if (Owner.Length > 0 && !factions.ContainsKey(Owner))
                ThrowHelper.ThrowXmlExceptionWithFormat(Global.Strings.XmlOwnerInvalid, Owner);

            // check unit owner ID
            if (UnitOwner.Length > 0 && !factions.ContainsKey(UnitOwner))
                ThrowHelper.ThrowXmlExceptionWithFormat(
                    Global.Strings.XmlUnitOwnerInvalid, UnitOwner);

            // check area size vs map size
            foreach (RectI bounds in Bounds)
                if (!areas.MapGrid.Contains(bounds))
                    ThrowHelper.ThrowXmlException(Global.Strings.XmlAreaBounds);

            // validate IDs of unit & effect templates
            entities.ValidateTemplates(Units);
            entities.ValidateTemplates(Effects);

            // validate and optimize terrain templates
            entities.ValidateTemplates(this._terrains);
        }

        #endregion
        #endregion
        #region XmlSerializable Members
        #region ConstXmlName

        /// <summary>
        /// The name of the XML element associated with the <see cref="Area"/> class.</summary>
        /// <remarks>
        /// <b>ConstXmlName</b> holds the value "area", indicating the XML element in <see
        /// cref="FilePaths.ScenarioSchema"/> whose data is managed by the <see cref="Area"/>
        /// class.</remarks>

        public const string ConstXmlName = "area";

        #endregion
        #region ReadXmlAttributes

        /// <summary>
        /// Reads XML attribute data into the <see cref="Area"/> object using the specified <see
        /// cref="XmlReader"/>.</summary>
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

            XmlUtility.ReadAttributeAsString(reader, "owner", ref this._owner);
            XmlUtility.ReadAttributeAsString(reader, "unitOwner", ref this._unitOwner);
        }

        #endregion
        #region ReadXmlElements

        /// <summary>
        /// Reads XML element data into the <see cref="Area"/> object using the specified <see
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
            EntityTemplate entity = null;
            switch (reader.Name) {

                case "bounds": {
                    RectI bounds = SimpleXml.ReadRectI(reader);
                    this._bounds.Add(bounds);
                    return true;
                }

                case UnitClass.ConstXmlName:
                    entity = new EntityTemplate(EntityCategory.Unit);
                    entity.ReadXml(reader);
                    this._units.Add(entity);
                    return true;

                case TerrainClass.ConstXmlName:
                    entity = new EntityTemplate(EntityCategory.Terrain);
                    entity.ReadXml(reader);
                    this._terrains.Add(entity);
                    return true;

                case EffectClass.ConstXmlName:
                    entity = new EntityTemplate(EntityCategory.Effect);
                    entity.ReadXml(reader);
                    this._effects.Add(entity);
                    return true;

                default: return false;
            }
        }

        #endregion
        #region WriteXmlAttributes

        /// <summary>
        /// Writes all current data of the <see cref="Area"/> object that is serialized to XML
        /// attributes to the specified <see cref="XmlWriter"/>.</summary>
        /// <param name="writer">
        /// The <see cref="XmlWriter"/> to which to write.</param>
        /// <remarks>
        /// Please refer to class <see cref="XmlSerializable"/> for a complete description of this
        /// method.</remarks>

        protected override void WriteXmlAttributes(XmlWriter writer) {

            if (Owner.Length > 0)
                writer.WriteAttributeString("owner", Owner);

            if (UnitOwner.Length > 0)
                writer.WriteAttributeString("unitOwner", UnitOwner);
        }

        #endregion
        #region WriteXmlElements

        /// <summary>
        /// Writes all current data of the <see cref="Area"/> object that is serialized to nested
        /// XML elements to the specified <see cref="XmlWriter"/>.</summary>
        /// <param name="writer">
        /// The <see cref="XmlWriter"/> to which to write.</param>
        /// <remarks>
        /// Please refer to class <see cref="XmlSerializable"/> for a complete description of this
        /// method.</remarks>

        protected override void WriteXmlElements(XmlWriter writer) {

            foreach (RectI bounds in Bounds) {
                writer.WriteStartElement("bounds");
                SimpleXml.WriteRectI(writer, bounds);
                writer.WriteEndElement();
            }

            foreach (EntityTemplate entity in Units)
                entity.WriteXml(writer);

            foreach (EntityTemplate entity in Terrains)
                entity.WriteXml(writer);

            foreach (EntityTemplate entity in Effects)
                entity.WriteXml(writer);
        }

        #endregion
        #endregion
    }
}
