using System;
using System.Collections.Generic;
using System.Xml;

using Tektosyne;
using Tektosyne.Collections;
using Tektosyne.Xml;
using Hexkit.Global;

namespace Hexkit.Scenario {

    /// <summary>
    /// Provides basic functionality for managing scenario elements.</summary>
    /// <remarks><para>
    /// <b>ScenarioElement</b> is the base class for all scenario elements, and provides empty
    /// default implementations for managing identifiers and validating data. <b>ScenarioElement</b>
    /// also inherits from <see cref="XmlSerializable"/> to support XML serialization and
    /// deserialization of scenario data.
    /// </para><para>
    /// All operations on the data of a <b>ScenarioElement</b> implicitly include all data stored in
    /// any instances of <b>ScenarioElement</b> that are subordinate to the current instance,
    /// according to the hierarchy defined by <see cref="FilePaths.ScenarioSchema"/>. Calling a
    /// <b>ScenarioElement</b> method on the <see cref="MasterSection"/> should process the entire
    /// scenario object tree.</para></remarks>

    public abstract class ScenarioElement: XmlSerializable {
        #region ProcessIdentifier

        /// <summary>
        /// Counts, changes, or deletes all occurrences of the specified identifier in the <see
        /// cref="ScenarioElement"/>.</summary>
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
        /// cref="ScenarioElement"/>.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="oldId"/> is a null reference.</exception>
        /// <remarks><para>
        /// <b>ProcessIdentifier</b> processes all XML identifiers stored in this <see
        /// cref="ScenarioElement"/>, including identifiers contained in the <see
        /// cref="KeyValuePair{String, TValue}.Key"/> components of <see cref="KeyValuePair{String,
        /// TValue}"/> instances, or collections thereof.
        /// </para><para>
        /// <b>ScenarioElement</b> properties that associate an XML identifier with another
        /// <b>ScenarioElement</b> instance may be processed <em>by key</em> or <em>by value</em>.
        /// Processing by key implies that the property is a <b>DictionaryEntry</b> value, or a
        /// collection of <b>DictionaryEntry</b> values, that is handled by one of the <see
        /// cref="CollectionsUtility.ProcessKey"/> overloads.
        /// </para><note type="implementnotes">
        /// This will also replace the <see cref="IKeyedValue{String}.Key"/> strings of all <see
        /// cref="IMutableKeyedValue{String}"/> instances stored as <see cref="KeyValuePair{String,
        /// TValue}.Value"/> components. Therefore, any <b>ScenarioElement</b> that implements <see
        /// cref="IMutableKeyedValue{String}"/>, and which is contained in another
        /// <b>ScenarioElement</b>, does not need to process its own <b>Key</b> string.
        /// </note><para>
        /// Processing by value implies that <b>ProcessIdentifier</b> is invoked on any and all
        /// instances of <see cref="ScenarioElement"/> stored in the specified property. When an
        /// instance of <b>ScenarioElement</b> is stored in multiple properties, only one of them
        /// should be processed by value, so as not to process the same object twice.
        /// </para></remarks>

        internal virtual int ProcessIdentifier(string oldId, string newId) {
            return 0;
        }

        #endregion
        #region Save

        /// <summary>
        /// Saves the <see cref="ScenarioElement"/> to the specified XML file.</summary>
        /// <param name="file">
        /// The XML file to which to save the current data of the <see cref="ScenarioElement"/>.
        /// </param>
        /// <exception cref="ArgumentNullOrEmptyException">
        /// <paramref name="file"/> is a null reference or an empty string.</exception>
        /// <remarks>
        /// <b>Save</b> invokes <see cref="XmlSerializable.WriteXml"/> with an <see
        /// cref="XmlWriter"/> created from the specified <paramref name="file"/>. Any existing
        /// contents of the specified <paramref name="file"/> will be overwritten.</remarks>

        public void Save(string file) {
            if (String.IsNullOrEmpty(file))
                ThrowHelper.ThrowArgumentNullOrEmptyException("file");

            // Auto allows manual writing of processing instruction
            XmlWriterSettings settings = XmlUtility.CreateWriterSettings();
            settings.ConformanceLevel = ConformanceLevel.Auto;

            // write element data to specified file
            using (XmlWriter writer = XmlWriter.Create(file, settings))
                WriteXml(writer);
        }

        #endregion
        #region Validate

        /// <summary>
        /// Validates the current data of the <see cref="ScenarioElement"/>.</summary>
        /// <exception cref="XmlException">
        /// Validation failed, and <see cref="ApplicationInfo.IsEditor"/> is <c>false</c>.
        /// </exception>
        /// <remarks><para>
        /// <b>Validate</b> should be called whenever instance data was changed by external input,
        /// either through <see cref="XmlSerializable.ReadXml"/> or by user interaction with Hexkit
        /// Editor. This method checks all data of this <see cref="ScenarioElement"/> for validity
        /// and performs other housekeeping tasks as required.
        /// </para><para>
        /// In particular, any XML identifiers used as the <see cref="KeyValuePair{TKey,
        /// TValue}.Key"/> components of <see cref="KeyValuePair{TKey, TValue}"/> instances will be
        /// matched against the identifiers of other <b>ScenarioElement</b> objects attached to the
        /// current <see cref="MasterSection"/> instance.
        /// </para><para>
        /// If an object with a matching identifier is found, its reference will be stored as the
        /// new <see cref="KeyValuePair{TKey, TValue}.Value"/> component of the <b>KeyValuePair</b>;
        /// otherwise, <b>Value</b> will be set to a null reference.
        /// </para><para>
        /// <b>Validate</b> may cause different results, depending on the current value of <see
        /// cref="ApplicationInfo.IsEditor"/>. In particular, most validation errors should be
        /// ignored if <b>IsEditor</b> is <c>true</c> but should generate an <see
        /// cref="XmlException"/> otherwise.</para></remarks>

        internal virtual void Validate() { }

        #endregion
    }
}
