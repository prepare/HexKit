using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml;

using Tektosyne.Collections;
using Hexkit.Global;

namespace Hexkit.Scenario {
    #region Type Aliases

    using FactionClassDictionary = KeyValueList<String, FactionClass>;
    
    #endregion

    /// <summary>
    /// Represents the Factions section of a scenario.</summary>
    /// <remarks>
    /// <b>FactionSection</b> is serialized to the XML element "factions" defined in <see
    /// cref="FilePaths.ScenarioSchema"/>.</remarks>

    public sealed class FactionSection: ScenarioElement {
        #region Private Fields

        // property backers
        private readonly FactionClassDictionary _collection = new FactionClassDictionary();

        #endregion
        #region Collection

        /// <summary>
        /// Gets a list of all factions defined in the <see cref="FactionSection"/>.</summary>
        /// <value>
        /// A <see cref="FactionClassDictionary"/> that maps <see cref="FactionClass.Id"/> strings
        /// to the corresponding <see cref="FactionClass"/> objects. The default is an empty
        /// collection.</value>
        /// <remarks><para>
        /// <b>Collection</b> never returns a null reference, and its <see
        /// cref="FactionClassDictionary.Values"/> are never null references. All keys are unique.
        /// The collection is read-only if <see cref="ApplicationInfo.IsEditor"/> is <c>false</c>.
        /// </para><para>
        /// The order in which <see cref="FactionClass"/> objects appear in the <b>Collection</b>
        /// determines the turn sequence of the corresponding factions within Hexkit Game.
        /// </para></remarks>

        public FactionClassDictionary Collection {
            [DebuggerStepThrough]
            get {
                return (ApplicationInfo.IsEditor ?
                    this._collection : this._collection.AsReadOnly());
            }
        }

        #endregion
        #region ScenarioElement Members
        #region ProcessIdentifier

        /// <summary>
        /// Counts, changes, or deletes all occurrences of the specified identifier in the <see
        /// cref="FactionSection"/>.</summary>
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
        /// cref="FactionSection"/>.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="oldId"/> is a null reference.</exception>
        /// <remarks><para>
        /// <b>ProcessIdentifier</b> processes <see cref="FactionSection"/> properties as follows:
        /// </para><list type="table"><listheader>
        /// <term>Property</term><description>Processing</description>
        /// </listheader><item>
        /// <term><see cref="Collection"/></term><description>By key and by value</description>
        /// </item></list></remarks>

        internal override int ProcessIdentifier(string oldId, string newId) {
            int count = 0;

            // process IDs in faction table
            count += CollectionsUtility.ProcessKey(
                (IDictionary<String, FactionClass>) this._collection, oldId, newId);

            foreach (FactionClass faction in Collection.Values)
                count += faction.ProcessIdentifier(oldId, newId);

            return count;
        }

        #endregion
        #region Validate

        /// <summary>
        /// Validates the current data of the <see cref="FactionSection"/>.</summary>
        /// <remarks><para>
        /// <b>Validate</b> processes <see cref="FactionSection"/> properties as follows:
        /// </para><list type="table"><listheader>
        /// <term>Property</term><description>Processing</description>
        /// </listheader><item>
        /// <term><see cref="Collection"/></term>
        /// <description>Invoke <see cref="FactionClass.Validate"/> on all elements</description>
        /// </item></list></remarks>

        internal override void Validate() {

            // set references for IDs in faction table
            foreach (FactionClass faction in Collection.Values)
                faction.Validate();
        }

        #endregion
        #endregion
        #region XmlSerializable Members
        #region ConstXmlName

        /// <summary>
        /// The name of the XML element associated with the <see cref="FactionSection"/> class.
        /// </summary>
        /// <remarks>
        /// <b>ConstXmlName</b> holds the value "factions", indicating the XML element in <see
        /// cref="FilePaths.ScenarioSchema"/> whose data is managed by the <see
        /// cref="FactionSection"/> class.</remarks>

        public const string ConstXmlName = "factions";

        #endregion
        #region ReadXmlElements

        /// <summary>
        /// Reads XML element data into the <see cref="FactionSection"/> object using the specified
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

                case FactionClass.ConstXmlName: {
                    FactionClass faction = new FactionClass();
                    faction.ReadXml(reader);
                    this._collection.Add(faction.Id, faction);
                    return true;
                }

                default: return false;
            }
        }

        #endregion
        #region WriteXmlElements

        /// <summary>
        /// Writes all current data of the <see cref="FactionSection"/> object that is serialized to
        /// nested XML elements to the specified <see cref="XmlWriter"/>.</summary>
        /// <param name="writer">
        /// The <see cref="XmlWriter"/> to which to write.</param>
        /// <remarks>
        /// Please refer to class <see cref="XmlSerializable"/> for a complete description of this
        /// method.</remarks>

        protected override void WriteXmlElements(XmlWriter writer) {

            foreach (FactionClass faction in Collection.Values)
                faction.WriteXml(writer);
        }

        #endregion
        #endregion
    }
}
