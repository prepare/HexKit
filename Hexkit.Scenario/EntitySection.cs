using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Xml;

using Tektosyne;
using Tektosyne.Collections;
using Hexkit.Global;

namespace Hexkit.Scenario {
    #region Type Aliases

    using EntityClassDictionary = SortedListEx<String, EntityClass>;
    using EntityTemplateList = ListEx<EntityTemplate>;

    #endregion

    /// <summary>
    /// Represents the Entities section of a scenario.</summary>
    /// <remarks>
    /// <b>EntitySection</b> is serialized to the XML element "entities" defined in <see
    /// cref="FilePaths.ScenarioSchema"/>.</remarks>

    public sealed class EntitySection: ScenarioElement {
        #region Private Fields

        // property backers
        private readonly EntityClassDictionary
            _units = new EntityClassDictionary(),
            _terrains = new EntityClassDictionary(),
            _effects = new EntityClassDictionary(),
            _upgrades = new EntityClassDictionary();

        #endregion
        #region AllCategories

        /// <summary>
        /// An <see cref="Array"/> comprising all values in <see cref="EntityCategory"/>.</summary>
        /// <remarks>
        /// <b>AllCategories</b> facilitates iterating through all values of the <see
        /// cref="EntityCategory"/> enumeration. This field holds the result of
        /// <c>Enum.GetValues(typeof(EntityCategory))</c>.</remarks>

        public static readonly EntityCategory[] AllCategories =
            (EntityCategory[]) Enum.GetValues(typeof(EntityCategory));

        #endregion
        #region Effects

        /// <summary>
        /// Gets a list of all effect classes defined in the <see cref="EntitySection"/>.</summary>
        /// <value>
        /// A <see cref="EntityClassDictionary"/> that maps <see cref="EntityClass.Id"/> strings to
        /// the corresponding <see cref="EffectClass"/> objects. The default is an empty collection.
        /// </value>
        /// <remarks>
        /// <b>Effects</b> never returns a null reference, and its <see
        /// cref="EntityClassDictionary.Values"/> are never null references. The collection is
        /// read-only if <see cref="ApplicationInfo.IsEditor"/> is <c>false</c>.</remarks>

        public EntityClassDictionary Effects {
            [DebuggerStepThrough]
            get {
                return (ApplicationInfo.IsEditor ?
                    this._effects : this._effects.AsReadOnly());
            }
        }

        #endregion
        #region Terrains

        /// <summary>
        /// Gets a list of all terrain classes defined in the <see cref="EntitySection"/>.</summary>
        /// <value>
        /// A <see cref="EntityClassDictionary"/> that maps <see cref="EntityClass.Id"/> strings to
        /// the corresponding <see cref="TerrainClass"/> objects. The default is an empty
        /// collection.</value>
        /// <remarks>
        /// <b>Terrains</b> never returns a null reference, and its <see
        /// cref="EntityClassDictionary.Values"/> are never null references. The collection is
        /// read-only if <see cref="ApplicationInfo.IsEditor"/> is <c>false</c>.</remarks>

        public EntityClassDictionary Terrains {
            [DebuggerStepThrough]
            get {
                return (ApplicationInfo.IsEditor ?
                  this._terrains : this._terrains.AsReadOnly());
            }
        }

        #endregion
        #region Units

        /// <summary>
        /// Gets a list of all unit classes defined in the <see cref="EntitySection"/>.</summary>
        /// <value>
        /// A <see cref="EntityClassDictionary"/> that maps <see cref="EntityClass.Id"/> strings to
        /// the corresponding <see cref="UnitClass"/> objects. The default is an empty collection.
        /// </value>
        /// <remarks>
        /// <b>Units</b> never returns a null reference, and its <see
        /// cref="EntityClassDictionary.Values"/> are never null references. The collection is
        /// read-only if <see cref="ApplicationInfo.IsEditor"/> is <c>false</c>.</remarks>

        public EntityClassDictionary Units {
            [DebuggerStepThrough]
            get {
                return (ApplicationInfo.IsEditor ?
                    this._units : this._units.AsReadOnly());
            }
        }

        #endregion
        #region Upgrades

        /// <summary>
        /// Gets a list of all upgrade classes defined in the <see cref="EntitySection"/>.</summary>
        /// <value>
        /// A <see cref="EntityClassDictionary"/> that maps <see cref="EntityClass.Id"/> strings to
        /// the corresponding <see cref="UpgradeClass"/> objects. The default is an empty
        /// collection.</value>
        /// <remarks>
        /// <b>Upgrades</b> never returns a null reference, and its <see
        /// cref="EntityClassDictionary.Values"/> are never null references. The collection is
        /// read-only if <see cref="ApplicationInfo.IsEditor"/> is <c>false</c>.</remarks>

        public EntityClassDictionary Upgrades {
            [DebuggerStepThrough]
            get {
                return (ApplicationInfo.IsEditor ?
                    this._upgrades : this._upgrades.AsReadOnly());
            }
        }

        #endregion
        #region GetEntity(EntityTemplate)

        /// <overloads>
        /// Returns the <see cref="EntityClass"/> matching a specified <see cref="EntityTemplate"/>,
        /// bitmap catalog index, or identifier.</overloads>
        /// <summary>
        /// Returns the <see cref="EntityClass"/> that is instantiated by the specified <see
        /// cref="EntityTemplate"/>.</summary>
        /// <param name="template">
        /// The <see cref="EntityTemplate"/> whose associated <see cref="EntityClass"/> to return.
        /// </param>
        /// <returns><para>
        /// The <see cref="EntityClass"/> whose <see cref="EntityClass.Category"/> and <see
        /// cref="EntityClass.Id"/> properties match the <see cref="EntityTemplate.Category"/> and
        /// <see cref="EntityTemplate.EntityClass"/> of the specified <paramref name="template"/>.
        /// </para><para>-or-</para><para>
        /// A null reference if no matching <see cref="EntityClass"/> was found.</para></returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="template"/> is a null reference.</exception>

        public EntityClass GetEntity(EntityTemplate template) {
            if (template == null)
                ThrowHelper.ThrowArgumentNullException("template");

            EntityClass entity;
            GetEntities(template.Category).TryGetValue(template.EntityClass, out entity);
            return entity; // may be null
        }

        #endregion
        #region GetEntity(Int32)

        /// <summary>
        /// Returns the <see cref="EntityClass"/> with the specified bitmap catalog index.</summary>
        /// <param name="index">
        /// One of the bitmap catalog indices of the <see cref="EntityClass"/> to locate.</param>
        /// <returns><para>
        /// The <see cref="EntityClass"/> whose range of bitmap catalog indices, starting at <see
        /// cref="EntityClass.FrameIndex"/> and continuing for <see cref="EntityClass.FrameCount"/>
        /// indices, contains the specified <paramref name="index"/>.
        /// </para><para>-or-</para><para>
        /// A null reference if no matching <see cref="EntityClass"/> was found. </para></returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="index"/> is equal to or less than zero.</exception>
        /// <remarks>
        /// <b>GetEntity</b> ignores any <see cref="EntityClass"/> with non-positive <see
        /// cref="EntityClass.FrameIndex"/> or <see cref="EntityClass.FrameCount"/> values.
        /// </remarks>

        public EntityClass GetEntity(int index) {
            if (index <= 0)
                ThrowHelper.ThrowArgumentOutOfRangeException(
                    "index", index, Tektosyne.Strings.ArgumentNotPositive);

            foreach (EntityCategory category in AllCategories)
                foreach (EntityClass entity in GetEntities(category).Values) {

                    // skip entities with invalid frame index
                    int frameIndex = entity.FrameIndex;
                    if (frameIndex <= 0) continue;

                    // check if specified index is in entity's range
                    if (index >= frameIndex && index < frameIndex + entity.FrameCount)
                        return entity;
                }

            return null;
        }

        #endregion
        #region GetEntity(String)

        /// <summary>
        /// Returns the <see cref="EntityClass"/> with the specified identifier.</summary>
        /// <param name="id">
        /// The <see cref="EntityClass.Id"/> string of the <see cref="EntityClass"/> to locate.
        /// </param>
        /// <returns><para>
        /// The <see cref="EntityClass"/> whose <see cref="EntityClass.Id"/> property equals the
        /// specified <paramref name="id"/>.
        /// </para><para>-or-</para><para>
        /// A null reference if no matching <see cref="EntityClass"/> was found.</para></returns>
        /// <exception cref="ArgumentNullOrEmptyException">
        /// <paramref name="id"/> is a null reference or an empty string.</exception>

        public EntityClass GetEntity(string id) {
            if (String.IsNullOrEmpty(id))
                ThrowHelper.ThrowArgumentNullOrEmptyException("id");

            EntityClass entity;
            foreach (EntityCategory category in AllCategories)
                if (GetEntities(category).TryGetValue(id, out entity))
                    return entity;

            return null;
        }

        #endregion
        #region GetEntities

        /// <summary>
        /// Gets a list of all entity classes of the specified category defined in the <see
        /// cref="EntitySection"/>.</summary>
        /// <param name="category">
        /// An <see cref="EntityCategory"/> value indicating which <see cref="EntityClass"/> list to
        /// return.</param>
        /// <returns>
        /// A <see cref="EntityClassDictionary"/> that maps <see cref="EntityClass.Id"/> strings to
        /// the corresponding <see cref="EntityClass"/> objects, all of which are of the specified
        /// <paramref name="category"/>.</returns>
        /// <exception cref="InvalidEnumArgumentException">
        /// <paramref name="category"/> is not a valid <see cref="EntityCategory"/> value.
        /// </exception>
        /// <remarks>
        /// <b>GetEntities</b> returns the value of one of the following properties, depending on
        /// the specified <paramref name="category"/>: <see cref="Units"/>, <see cref="Terrains"/>,
        /// <see cref="Effects"/>, or <see cref="Upgrades"/>.</remarks>

        public EntityClassDictionary GetEntities(EntityCategory category) {
            switch (category) {

                case EntityCategory.Unit:    return Units;
                case EntityCategory.Terrain: return Terrains;
                case EntityCategory.Effect:  return Effects;
                case EntityCategory.Upgrade: return Upgrades;

                default:
                    ThrowHelper.ThrowInvalidEnumArgumentException(
                        "category", (int) category, typeof(EntityCategory));
                    return null;
            }
        }

        #endregion
        #region ScenarioElement Members
        #region ProcessIdentifier

        /// <summary>
        /// Counts, changes, or deletes all occurrences of the specified identifier in the <see
        /// cref="EntitySection"/>.</summary>
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
        /// The number of occurrences of <paramref name="oldId"/> in the <see
        /// cref="EntitySection"/>.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="oldId"/> is a null reference.</exception>
        /// <remarks><para>
        /// <b>ProcessIdentifier</b> processes <see cref="EntitySection"/> properties as follows:
        /// </para><list type="table"><listheader>
        /// <term>Property</term><description>Processing</description>
        /// </listheader><item>
        /// <term><see cref="Effects"/><br/> <see cref="Terrains"/><br/> <see cref="Units"/><br/>
        /// <see cref="Upgrades"/></term><description>By key and by value</description>
        /// </item></list></remarks>

        internal override int ProcessIdentifier(string oldId, string newId) {
            int count = 0;

            // process IDs for all entity categories
            count += ProcessIdentifierInCollection(this._units, oldId, newId);
            count += ProcessIdentifierInCollection(this._terrains, oldId, newId);
            count += ProcessIdentifierInCollection(this._effects, oldId, newId);
            count += ProcessIdentifierInCollection(this._upgrades, oldId, newId);

            return count;
        }

        #endregion
        #region ProcessIdentifierInCollection

        /// <summary>
        /// Counts, changes, or deletes all occurrences of the specified identifier in the specified
        /// <see cref="EntityClass"/> dictionary.</summary>
        /// <param name="dictionary">
        /// The <see cref="EntityClassDictionary"/> to process.</param>
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
        /// The number of occurrences of <paramref name="oldId"/> in the specified <paramref
        /// name="dictionary"/>.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="dictionary"/> or <paramref name="oldId"/> is a null reference.
        /// </exception>
        /// <remarks>
        /// All entries in the specified <paramref name="dictionary"/> are processed by key and by
        /// value. Please refer to <see cref="ProcessIdentifier"/> for further details.</remarks>

        private static int ProcessIdentifierInCollection(
            EntityClassDictionary dictionary, string oldId, string newId) {

            int count = 0;

            // process dictionary by key
            count += CollectionsUtility.ProcessKey(dictionary, oldId, newId);

            // process dictionary by value
            foreach (EntityClass entity in dictionary.Values)
                count += entity.ProcessIdentifier(oldId, newId);

            return count;
        }

        #endregion
        #region Validate

        /// <summary>
        /// Validates the current data of the <see cref="EntitySection"/>.</summary>
        /// <remarks><para>
        /// <b>Validate</b> processes <see cref="EntitySection"/> properties as follows:
        /// </para><list type="table"><listheader>
        /// <term>Property</term><description>Processing</description>
        /// </listheader><item>
        /// <term><see cref="Effects"/><br/> <see cref="Terrains"/><br/> <see cref="Units"/><br/>
        /// <see cref="Upgrades"/></term>
        /// <description>Invoke <see cref="EntityClass.Validate"/> on all elements</description>
        /// </item></list></remarks>

        internal override void Validate() {

            // set references for IDs in entity class tables
            foreach (EntityCategory category in AllCategories)
                foreach (EntityClass entity in GetEntities(category).Values)
                    entity.Validate();
        }

        #endregion
        #region ValidateCollection

        /// <summary>
        /// Validates all identifiers in the specified <see cref="EntityClass"/> dictionary.
        /// </summary>
        /// <param name="dictionary">
        /// The <see cref="EntityClassDictionary"/> whose <see cref="EntityClassDictionary.Keys"/>
        /// to validate.</param>
        /// <param name="category">
        /// The <see cref="EntityCategory"/> of all elements in <paramref name="dictionary"/>.
        /// </param>
        /// <param name="tag">
        /// A <see cref="String"/> used to customize the exception <see
        /// cref="XmlException.Message"/> if validation fails.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="dictionary"/> is a null reference.</exception>
        /// <exception cref="InvalidEnumArgumentException">
        /// <paramref name="category"/> is not a valid <see cref="EntityCategory"/> value.
        /// </exception>
        /// <exception cref="XmlException">
        /// Validation of <paramref name="dictionary"/> failed.</exception>
        /// <remarks><para>
        /// <b>ValidateCollection</b> attempts to find all keys in the specified <paramref
        /// name="dictionary"/> in the <see cref="EntityClass"/> table for the specified <paramref
        /// name="category"/>, as returned by <see cref="GetEntities"/>
        /// </para><para>
        /// <b>ValidateCollection</b> sets all values to the corresponding <see cref="EntityClass"/>
        /// if successful, and throws an <see cref="XmlException"/> otherwise.</para></remarks>

        internal void ValidateCollection(EntityClassDictionary dictionary,
            EntityCategory category, string tag) {

            if (dictionary == null)
                ThrowHelper.ThrowArgumentNullException("dictionary");

            // get entity classes for specified category
            var entities = GetEntities(category);
            EntityClass entityClass;

            for (int i = 0; i < dictionary.Count; i++) {

                // try to locate class in entity table
                string id = dictionary.GetKey(i);
                entities.TryGetValue(id, out entityClass);

                // check for nonexistent entity class if not editing
                if (!ApplicationInfo.IsEditor && entityClass == null)
                    ThrowHelper.ThrowXmlExceptionWithFormat(
                        Global.Strings.XmlEntityClassInvalid, tag, id);

                // set class reference in dictionary
                dictionary.SetByIndex(i, entityClass);
            }
        }

        #endregion
        #region ValidateTemplates

        /// <summary>
        /// Validates the specified list of <see cref="EntityTemplate"/> objects.</summary>
        /// <param name="entities">
        /// The <see cref="EntityTemplateList"/> to validate.</param>
        /// <exception cref="ArgumentException">
        /// <paramref name="entities"/> contains elements whose <see
        /// cref="EntityTemplate.Category"/> values differ.</exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="entities"/> is a null reference.</exception>
        /// <exception cref="XmlException">
        /// Validation of <paramref name="entities"/> failed.</exception>
        /// <remarks><para>
        /// <b>ValidateTemplates</b> invokes <see cref="Validate"/> on each element of the specified
        /// <paramref name="entities"/> collection, and checks that all elements have the same <see
        /// cref="EntityTemplate.Category"/>.
        /// </para><para>
        /// If that <see cref="EntityTemplate.Category"/> is <see cref="EntityCategory.Terrain"/>,
        /// <b>ValidateTemplates</b> also optimizes the collection by removing all elements that
        /// would be obscured by subsequent terrains whose <see cref="TerrainClass.IsBackground"/>
        /// flag is <c>true</c>.</para></remarks>

        internal void ValidateTemplates(EntityTemplateList entities) {
            if (entities == null)
                ThrowHelper.ThrowArgumentNullException("entities");

            // quit if nothing to validate
            if (entities.Count == 0) return;

            EntityCategory category = entities[0].Category;

            // check categories and validate elements
            foreach (EntityTemplate entity in entities) {

                if (entity.Category != category)
                    ThrowHelper.ThrowArgumentExceptionWithFormat(
                        "entities", Tektosyne.Strings.ArgumentContainsDifferent, "Category");

                entity.Validate();
            }

            // nothing else to do for non-terrains
            if (category != EntityCategory.Terrain)
                return;

            // clear terrain stack up to this index
            int clearIndex = -1;

            // optimize terrain stack
            for (int i = 0; i < entities.Count; i++) {
                string id = entities[i].EntityClass;

                // get associated terrain class
                TerrainClass terrain = Terrains[id] as TerrainClass;
                Debug.Assert(terrain != null);

                // new background resets terrain table
                if (terrain.IsBackground)
                    clearIndex = i - 1;
            }

            // remove terrains hidden by background
            while (clearIndex >= 0)
                entities.RemoveAt(clearIndex--);
        }

        #endregion
        #endregion
        #region XmlSerializable Members
        #region ConstXmlName

        /// <summary>
        /// The name of the XML element associated with the <see cref="EntitySection"/> class.
        /// </summary>
        /// <remarks>
        /// <b>ConstXmlName</b> holds the value "entities", indicating the XML element in <see
        /// cref="FilePaths.ScenarioSchema"/> whose data is managed by the <see
        /// cref="EntitySection"/> class.</remarks>

        public const string ConstXmlName = "entities";

        #endregion
        #region ReadXmlElements

        /// <summary>
        /// Reads XML element data into the <see cref="EntitySection"/> object using the specified
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
            EntityClass entity = null;
            switch (reader.Name) {

                case UnitClass.ConstXmlName:
                    entity = new UnitClass();
                    entity.ReadXml(reader);
                    this._units.Add(entity.Id, entity);
                    return true;

                case TerrainClass.ConstXmlName:
                    entity = new TerrainClass();
                    entity.ReadXml(reader);
                    this._terrains.Add(entity.Id, entity);
                    return true;

                case EffectClass.ConstXmlName:
                    entity = new EffectClass();
                    entity.ReadXml(reader);
                    this._effects.Add(entity.Id, entity);
                    return true;

                case UpgradeClass.ConstXmlName:
                    entity = new UpgradeClass();
                    entity.ReadXml(reader);
                    this._upgrades.Add(entity.Id, entity);
                    return true;

                default: return false;
            }
        }

        #endregion
        #region WriteXmlElements

        /// <summary>
        /// Writes all current data of the <see cref="EntitySection"/> object that is serialized to
        /// nested XML elements to the specified <see cref="XmlWriter"/>.</summary>
        /// <param name="writer">
        /// The <see cref="XmlWriter"/> to which to write.</param>
        /// <remarks>
        /// Please refer to class <see cref="XmlSerializable"/> for a complete description of this
        /// method.</remarks>

        protected override void WriteXmlElements(XmlWriter writer) {

            foreach (EntityCategory category in AllCategories)
                foreach (EntityClass entity in GetEntities(category).Values)
                    entity.WriteXml(writer);
        }

        #endregion
        #endregion
    }
}
