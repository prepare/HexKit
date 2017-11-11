using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Xml;

using Tektosyne;
using Tektosyne.Collections;
using Hexkit.Global;

namespace Hexkit.Scenario {
    #region Type Aliases

    using VariableClassDictionary = SortedListEx<String, VariableClass>;
    using VariableModifierDictionary = SortedListEx<String, VariableModifier>;
    using VariableValueDictionary = SortedListEx<String, Int32>;

    #endregion

    /// <summary>
    /// Represents the Variables section of a scenario.</summary>
    /// <remarks>
    /// <b>VariableSection</b> is serialized to the XML element "variables" defined in <see
    /// cref="FilePaths.ScenarioSchema"/>.</remarks>

    public sealed class VariableSection: ScenarioElement {
        #region Private Fields

        // property backers
        private readonly VariableClassDictionary
            _attributes = new VariableClassDictionary(),
            _counters = new VariableClassDictionary(),
            _resources = new VariableClassDictionary();

        #endregion
        #region AllCategories

        /// <summary>
        /// An <see cref="Array"/> comprising all values in <see cref="VariableCategory"/>.
        /// </summary>
        /// <remarks>
        /// <b>AllCategories</b> facilitates iterating through all values of the <see
        /// cref="VariableCategory"/> enumeration. This field holds the result of
        /// <c>Enum.GetValues(typeof(VariableCategory))</c>.</remarks>

        public static readonly VariableCategory[] AllCategories =
            (VariableCategory[]) Enum.GetValues(typeof(VariableCategory));

        #endregion
        #region Attributes

        /// <summary>
        /// Gets a list of all attributes defined in the <see cref="VariableSection"/>.</summary>
        /// <value>
        /// A <see cref="VariableClassDictionary"/> that maps <see cref="VariableClass.Id"/> strings
        /// to the corresponding <see cref="AttributeClass"/> objects. The default is an empty
        /// collection.</value>
        /// <remarks>
        /// <b>Attributes</b> never returns a null reference, and its <see
        /// cref="VariableClassDictionary.Values"/> are never null references. The collection is
        /// read-only if <see cref="ApplicationInfo.IsEditor"/> is <c>false</c>.</remarks>

        public VariableClassDictionary Attributes {
            [DebuggerStepThrough]
            get {
                return (ApplicationInfo.IsEditor ?
                    this._attributes : this._attributes.AsReadOnly());
            }
        }

        #endregion
        #region Counters

        /// <summary>
        /// Gets a list of all counters defined in the <see cref="VariableSection"/>.</summary>
        /// <value>
        /// A <see cref="VariableClassDictionary"/> that maps <see cref="VariableClass.Id"/> strings
        /// to the corresponding <see cref="CounterClass"/> objects. The default is an empty
        /// collection.</value>
        /// <remarks>
        /// <b>Counters</b> never returns a null reference, and its <see
        /// cref="VariableClassDictionary.Values"/> are never null references. The collection is
        /// read-only if <see cref="ApplicationInfo.IsEditor"/> is <c>false</c>.</remarks>

        public VariableClassDictionary Counters {
            [DebuggerStepThrough]
            get {
                return (ApplicationInfo.IsEditor ?
                    this._counters : this._counters.AsReadOnly());
            }
        }

        #endregion
        #region Resources

        /// <summary>
        /// Gets a list of all resources defined in the <see cref="VariableSection"/>.</summary>
        /// <value>
        /// A <see cref="VariableClassDictionary"/> that maps <see cref="VariableClass.Id"/> strings
        /// to the corresponding <see cref="ResourceClass"/> objects. The default is an empty
        /// collection.</value>
        /// <remarks>
        /// <b>Resources</b> never returns a null reference, and its <see
        /// cref="VariableClassDictionary.Values"/> are never null references. The collection is
        /// read-only if <see cref="ApplicationInfo.IsEditor"/> is <c>false</c>.</remarks>

        public VariableClassDictionary Resources {
            [DebuggerStepThrough]
            get {
                return (ApplicationInfo.IsEditor ?
                    this._resources : this._resources.AsReadOnly());
            }
        }

        #endregion
        #region GetVariable(String)

        /// <overloads>
        /// Returns the <see cref="VariableClass"/> with the specified identifier.</overloads>
        /// <summary>
        /// Returns the <see cref="VariableClass"/> with the specified identifier.</summary>
        /// <param name="id">
        /// The <see cref="VariableClass.Id"/> string of the <see cref="VariableClass"/> to locate.
        /// </param>
        /// <returns><para>
        /// The <see cref="VariableClass"/> whose <see cref="VariableClass.Id"/> property equals the
        /// specified <paramref name="id"/>.
        /// </para><para>-or-</para><para>
        /// A null reference if no matching <see cref="VariableClass"/> was found.</para></returns>
        /// <exception cref="ArgumentNullOrEmptyException">
        /// <paramref name="id"/> is a null reference or an empty string.</exception>

        public VariableClass GetVariable(string id) {
            if (String.IsNullOrEmpty(id))
                ThrowHelper.ThrowArgumentNullOrEmptyException("id");

            VariableClass variable;
            foreach (VariableCategory category in AllCategories)
                if (GetVariables(category).TryGetValue(id, out variable))
                    return variable;

            return null;
        }

        #endregion
        #region GetVariable(String, VariableCategory)

        /// <summary>
        /// Returns the <see cref="VariableClass"/> with the specified identifier and category.
        /// </summary>
        /// <param name="id">
        /// The <see cref="VariableClass.Id"/> string of the <see cref="VariableClass"/> to locate.
        /// </param>
        /// <param name="category">
        /// The <see cref="VariableClass.Category"/> of the <see cref="VariableClass"/> to locate.
        /// </param>
        /// <returns><para>
        /// The <see cref="VariableClass"/> whose <see cref="VariableClass.Id"/> and <see
        /// cref="VariableClass.Category"/> properties equal the specified <paramref name="id"/> and
        /// <paramref name="category"/>.
        /// </para><para>-or-</para><para>
        /// A null reference if <see cref="ApplicationInfo.IsEditor"/> is <c>false</c> and no
        /// matching <see cref="VariableClass"/> was found.</para></returns>
        /// <exception cref="ArgumentNullOrEmptyException">
        /// <paramref name="id"/> is a null reference or an empty string.</exception>
        /// <exception cref="InvalidEnumArgumentException">
        /// <paramref name="category"/> is not a valid <see cref="VariableCategory"/> value.
        /// </exception>
        /// <remarks>
        /// If <see cref="ApplicationInfo.IsEditor"/> is <c>true</c> and no matching <see
        /// cref="VariableClass"/> was found, <b>GetVariable</b> creates and returns a new <see
        /// cref="VariableClass"/> object.</remarks>

        public VariableClass GetVariable(string id, VariableCategory category) {
            if (String.IsNullOrEmpty(id))
                ThrowHelper.ThrowArgumentNullException("id");

            // return existing variable class if found
            VariableClass variable;
            VariableClassDictionary variables = GetVariables(category);
            if (variables.TryGetValue(id, out variable))
                return variable;

            // create new variable class if editing
            if (ApplicationInfo.IsEditor)
                return VariableClass.Create(id, category);

            return null;
        }

        #endregion
        #region GetVariables

        /// <summary>
        /// Gets a list of all variables of the specified category defined in the <see
        /// cref="VariableSection"/>.</summary>
        /// <param name="category">
        /// A <see cref="VariableCategory"/> value indicating which <see cref="VariableClass"/> list
        /// to return.</param>
        /// <returns>
        /// A <see cref="VariableClassDictionary"/> that maps <see cref="VariableClass.Id"/> strings
        /// to the corresponding <see cref="VariableClass"/> objects, all of which are of the
        /// specified <paramref name="category"/>.</returns>
        /// <exception cref="InvalidEnumArgumentException">
        /// <paramref name="category"/> is not a valid <see cref="VariableCategory"/> value.
        /// </exception>
        /// <remarks>
        /// <b>GetVariables</b> returns the value of one of the following properties, depending on
        /// the specified <paramref name="category"/>: <see cref="Attributes"/>, <see
        /// cref="Counters"/>, or <see cref="Resources"/>.</remarks>

        public VariableClassDictionary GetVariables(VariableCategory category) {
            switch (category) {

                case VariableCategory.Attribute: return Attributes;
                case VariableCategory.Counter:   return Counters;
                case VariableCategory.Resource:  return Resources;

                default:
                    ThrowHelper.ThrowInvalidEnumArgumentException(
                        "category", (int) category, typeof(VariableCategory));
                    return null;
            }
        }

        #endregion
        #region ScenarioElement Members
        #region ProcessIdentifier

        /// <summary>
        /// Counts, changes, or deletes all occurrences of the specified identifier in the <see
        /// cref="VariableSection"/>.</summary>
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
        /// cref="VariableSection"/>.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="oldId"/> is a null reference.</exception>
        /// <remarks><para>
        /// <b>ProcessIdentifier</b> processes <see cref="VariableSection"/> properties as follows:
        /// </para><list type="table"><listheader>
        /// <term>Property</term><description>Processing</description>
        /// </listheader><item>
        /// <term><see cref="Attributes"/><br/> <see cref="Counters"/><br/> <see cref="Resources"/>
        /// </term><description>By key only</description>
        /// </item></list></remarks>

        internal override int ProcessIdentifier(string oldId, string newId) {
            int count = 0;

            // process IDs for all variable categories
            count += CollectionsUtility.ProcessKey(this._attributes, oldId, newId);
            count += CollectionsUtility.ProcessKey(this._counters, oldId, newId);
            count += CollectionsUtility.ProcessKey(this._resources, oldId, newId);

            return count;
        }

        #endregion
        #region ValidateCollection(VariableValueDictionary)

        /// <overloads>
        /// Validates all <see cref="VariableClass"/> identifiers in the specified dictionary.
        /// </overloads>
        /// <summary>
        /// Validates all <see cref="VariableClass"/> identifiers in the specified <see
        /// cref="VariableValueDictionary"/>.</summary>
        /// <param name="dictionary">
        /// The <see cref="VariableValueDictionary"/> whose <see
        /// cref="VariableValueDictionary.Keys"/> to validate.</param>
        /// <param name="category">
        /// The <see cref="VariableCategory"/> of all elements in <paramref name="dictionary"/>.
        /// </param>
        /// <param name="tag">
        /// A <see cref="String"/> used to customize the exception <see
        /// cref="XmlException.Message"/> if validation fails.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="dictionary"/> is a null reference.</exception>
        /// <exception cref="InvalidEnumArgumentException">
        /// <paramref name="category"/> is not a valid <see cref="VariableCategory"/> value.
        /// </exception>
        /// <exception cref="XmlException">
        /// Validation of <paramref name="dictionary"/> failed.</exception>
        /// <remarks>
        /// <b>ValidateCollection</b> attempts to find all keys in the specified <paramref
        /// name="dictionary"/> in the variable list for the specified <paramref name="category"/>,
        /// as returned by <see cref="GetVariables"/>, and throws an <see cref="XmlException"/> if
        /// unsuccessful.</remarks>

        internal void ValidateCollection(VariableValueDictionary dictionary,
            VariableCategory category, string tag) {

            if (dictionary == null)
                ThrowHelper.ThrowArgumentNullException("dictionary");

            // get variable classes for specified category
            VariableClassDictionary variables = GetVariables(category);

            // validate all keys against classes
            foreach (string id in dictionary.Keys)
                if (!variables.ContainsKey(id))
                    ThrowHelper.ThrowXmlExceptionWithFormat(Global.Strings.XmlReferenceInvalid,
                        tag, VariableClass.GetXmlName(category), id);
        }

        #endregion
        #region ValidateCollection(VariableModifierDictionary)

        /// <summary>
        /// Validates all <see cref="VariableClass"/> identifiers in the specified <see
        /// cref="VariableModifierDictionary"/>.</summary>
        /// <param name="dictionary">
        /// The <see cref="VariableModifierDictionary"/> whose <see
        /// cref="VariableModifierDictionary.Keys"/> to validate.</param>
        /// <param name="category">
        /// The <see cref="VariableCategory"/> of all elements in <paramref name="dictionary"/>.
        /// </param>
        /// <param name="tag">
        /// A <see cref="String"/> used to customize the exception <see
        /// cref="XmlException.Message"/> if validation fails.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="dictionary"/> is a null reference.</exception>
        /// <exception cref="InvalidEnumArgumentException">
        /// <paramref name="category"/> is not a valid <see cref="VariableCategory"/> value.
        /// </exception>
        /// <exception cref="XmlException">
        /// Validation of <paramref name="dictionary"/> failed.</exception>
        /// <remarks>
        /// <b>ValidateCollection</b> attempts to find all keys in the specified <paramref
        /// name="dictionary"/> in the variable list for the specified <paramref name="category"/>,
        /// as returned by <see cref="GetVariables"/>, and throws an <see cref="XmlException"/> if
        /// unsuccessful.</remarks>

        internal void ValidateCollection(VariableModifierDictionary dictionary,
            VariableCategory category, string tag) {

            if (dictionary == null)
                ThrowHelper.ThrowArgumentNullException("dictionary");

            // get variable classes for specified category
            VariableClassDictionary variables = GetVariables(category);

            // validate all keys against classes
            foreach (string id in dictionary.Keys)
                if (!variables.ContainsKey(id))
                    ThrowHelper.ThrowXmlExceptionWithFormat(Global.Strings.XmlReferenceInvalid,
                        tag, VariableClass.GetXmlName(category), id);
        }

        #endregion
        #endregion
        #region XmlSerializable Members
        #region ConstXmlName

        /// <summary>
        /// The name of the XML element associated with the <see cref="VariableSection"/> class.
        /// </summary>
        /// <remarks>
        /// <b>ConstXmlName</b> holds the value "variables", indicating the XML element in <see
        /// cref="FilePaths.ScenarioSchema"/> whose data is managed by the <see
        /// cref="VariableSection"/> class.</remarks>

        public const string ConstXmlName = "variables";

        #endregion
        #region ReadXmlElements

        /// <summary>
        /// Reads XML element data into the <see cref="VariableSection"/> object using the specified
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
            VariableClass variable = null;
            switch (reader.Name) {

                case AttributeClass.ConstXmlName:
                    variable = new AttributeClass();
                    variable.ReadXml(reader);
                    this._attributes.Add(variable.Id, variable);
                    return true;

                case CounterClass.ConstXmlName:
                    variable = new CounterClass();
                    variable.ReadXml(reader);
                    this._counters.Add(variable.Id, variable);
                    return true;

                case ResourceClass.ConstXmlName:
                    variable = new ResourceClass();
                    variable.ReadXml(reader);
                    this._resources.Add(variable.Id, variable);
                    return true;

                default: return false;
            }
        }

        #endregion
        #region WriteXmlElements

        /// <summary>
        /// Writes all current data of the <see cref="VariableSection"/> object that is serialized
        /// to nested XML elements to the specified <see cref="XmlWriter"/>.</summary>
        /// <param name="writer">
        /// The <see cref="XmlWriter"/> to which to write.</param>
        /// <remarks>
        /// Please refer to class <see cref="XmlSerializable"/> for a complete description of this
        /// method.</remarks>

        protected override void WriteXmlElements(XmlWriter writer) {

            foreach (VariableCategory category in AllCategories)
                foreach (VariableClass variable in GetVariables(category).Values)
                    variable.WriteXml(writer);
        }

        #endregion
        #endregion
    }
}
