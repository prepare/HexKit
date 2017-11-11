using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Xml;

using Tektosyne;
using Tektosyne.Collections;
using Tektosyne.Xml;

using Hexkit.Global;

namespace Hexkit.Players {
    #region Type Aliases

    using ComputerOptionsDictionary = SortedListEx<String, String>;

    #endregion

    /// <summary>
    /// Manages optional settings for a computer player algorithm.</summary>
    /// <remarks><para>
    /// <b>AlgorithmOptions</b> associates a <see cref="ComputerPlayer"/> with one of the algorithms
    /// provided by the current <see cref="PlayerManager"/> instance, and also manages optional
    /// settings that customize the algorithm's behavior on a per-player basis.
    /// </para><para>
    /// <b>AlgorithmOptions</b> defines several basic settings that are generally useful. An
    /// <b>Algorithm</b> implementation that provides additional options should derive a new public
    /// class from <b>AlgorithmOptions</b> that contains any required data, and override <see
    /// cref="XmlSerializable.ReadXmlElements"/> and <see cref="XmlSerializable.WriteXmlElements"/>
    /// to serialize that data.
    /// </para><para>
    /// <b>AlgorithmOptions</b> objects are serialized to "options" elements in a session XML file.
    /// </para></remarks>

    public class AlgorithmOptions: XmlSerializable, IKeyedValue<Algorithm> {
        #region AlgorithmOptions(String)

        /// <summary>
        /// Initializes a new instance of the <see cref="AlgorithmOptions"/> class for the specified
        /// computer player algorithm.</summary>
        /// <param name="algorithmId">
        /// The <see cref="Players.Algorithm.Id"/> string of the element in the <see
        /// cref="PlayerManager.Algorithms"/> collection of the current <see cref="PlayerManager"/>
        /// instance that is the initial value for the <see cref="Algorithm"/> property.</param>
        /// <exception cref="KeyNotFoundException">
        /// <paramref name="algorithmId"/> was not found in the <see
        /// cref="PlayerManager.Algorithms"/> collection of the current <see cref="PlayerManager"/>
        /// instance.</exception>
        /// <remarks>
        /// Clients should use one of the <see cref="Create"/> overloads to create a new instance of
        /// the <see cref="AlgorithmOptions"/> class, or one of its derived classes that matches the
        /// specified <paramref name="algorithmId"/>.</remarks>

        protected AlgorithmOptions(string algorithmId) {
            this._algorithm = PlayerManager.Instance.Algorithms[algorithmId];
        }

        #endregion
        #region Private Fields

        // construction arguments
        private readonly Algorithm _algorithm;

        // property backers
        private bool _useRandomBuild = false, _useRandomPlace = false, _useScripting = true;
        private int _targetLimit = 8;

        #endregion
        #region Algorithm

        /// <summary>
        /// Gets the computer player <see cref="Players.Algorithm"/> associated with the <see
        /// cref="AlgorithmOptions"/>.</summary>
        /// <value>
        /// The <see cref="Players.Algorithm"/> whose optional settings are managed by the <see
        /// cref="AlgorithmOptions"/> object.</value>
        /// <remarks>
        /// <b>Algorithm</b> never returns a null reference. This property never changes once the
        /// object has been constructed.</remarks>

        [Browsable(false)]
        public Algorithm Algorithm {
            [DebuggerStepThrough]
            get { return this._algorithm; }
        }

        #endregion
        #region TargetLimit

        /// <summary>
        /// Gets or sets the maximum number of potential targets that the <see cref="Algorithm"/>
        /// should examine.</summary>
        /// <value>
        /// The maximum number of potential targets that the <see cref="Algorithm"/> should examine
        /// for each unit. The default is eight.</value>
        /// <remarks>
        /// When set, the new value of <b>TargetLimit</b> is automatically restricted to the
        /// interval [1, 1000].</remarks>

        [Description("Maximum number of potential targets to examine for each unit.")]
        public int TargetLimit {
            [DebuggerStepThrough]
            get { return this._targetLimit; }
            [DebuggerStepThrough]
            set { this._targetLimit = Math.Max(1, Math.Min(1000, value)); }
        }

        #endregion
        #region UseRandomBuild

        /// <summary>
        /// Gets or sets a value indicating whether the <see cref="Algorithm"/> should randomly
        /// build entities.</summary>
        /// <value>
        /// <c>true</c> if the associated <see cref="Algorithm"/> should call <see
        /// cref="AlgorithmGeneral.BuildRandom"/> to build entities; otherwise, <c>false</c>.
        /// The default is <c>false</c>.</value>

        [Description("Build entities randomly, rather than evaluating each entity.")]
        public bool UseRandomBuild {
            [DebuggerStepThrough]
            get { return this._useRandomBuild; }
            [DebuggerStepThrough]
            set { this._useRandomBuild = value; }
        }

        #endregion
        #region UseRandomPlace

        /// <summary>
        /// Gets or sets a value indicating whether the <see cref="Algorithm"/> should randomly
        /// place entities.</summary>
        /// <value>
        /// <c>true</c> if the associated <see cref="Algorithm"/> should call <see
        /// cref="AlgorithmGeneral.PlaceRandom"/> to place entities; otherwise, <c>false</c>.
        /// The default is <c>false</c>.</value>

        [Description("Place entities randomly on the map, rather than evaluating each placement site.")]
        public bool UseRandomPlace {
            [DebuggerStepThrough]
            get { return this._useRandomPlace; }
            [DebuggerStepThrough]
            set { this._useRandomPlace = value; }
        }

        #endregion
        #region UseScripting

        /// <summary>
        /// Gets or sets a value indicating whether the <see cref="Algorithm"/> should use scripted
        /// behavior.</summary>
        /// <value>
        /// <c>true</c> if the associated <see cref="Algorithm"/> should use scripted behavior for
        /// computer-controlled factions; otherwise, <c>false</c>. The default is <c>true</c>.
        /// </value>

        [Description("Use scripted rather than dynamic behavior for factions and entities.")]
        public bool UseScripting {
            [DebuggerStepThrough]
            get { return this._useScripting; }
            [DebuggerStepThrough]
            set { this._useScripting = value; }
        }

        #endregion
        #region Create()

        /// <overloads>
        /// Creates a new instance of the <see cref="AlgorithmOptions"/> class.</overloads>
        /// <summary>
        /// Creates a new instance of the <see cref="AlgorithmOptions"/> class with the default
        /// computer player <see cref="Players.Algorithm"/>.</summary>
        /// <returns>
        /// A new <see cref="AlgorithmOptions"/> instance for the default <see
        /// cref="Players.Algorithm"/>.</returns>
        /// <remarks><para>
        /// <b>Create</b> returns a new instance of <see cref="AlgorithmOptions"/> or of a derived
        /// class, depending on what is currently considered the default <see
        /// cref="Players.Algorithm"/>.
        /// </para><para>
        /// Currently, the <see cref="Algorithm"/> property of the returned instance is set to the
        /// element in the <see cref="PlayerManager.Algorithms"/> collection with the key "Seeker". 
        /// The optional settings are not adjusted. Future Hexkit versions may change this behavior.
        /// </para></remarks>

        public static AlgorithmOptions Create() {
            return new AlgorithmOptions("Seeker");
        }

        #endregion
        #region Create(String)

        /// <summary>
        /// Initializes a new instance of the <see cref="AlgorithmOptions"/> class with the
        /// specified computer player <see cref="Players.Algorithm"/>.</summary>
        /// <param name="algorithmId">
        /// The <see cref="Players.Algorithm.Id"/> string of the element in the <see
        /// cref="PlayerManager.Algorithms"/> collection of the current <see cref="PlayerManager"/>
        /// instance that is the initial value for the <see cref="Algorithm"/> property.</param>
        /// <returns>
        /// A new <see cref="AlgorithmOptions"/> instance for the <see cref="Players.Algorithm"/>
        /// indicated by the specified <paramref name="algorithmId"/>.</returns>
        /// <exception cref="ArgumentNullOrEmptyException">
        /// <paramref name="algorithmId"/> is a null reference or an empty string.</exception>
        /// <exception cref="KeyNotFoundException">
        /// <paramref name="algorithmId"/> was not found in the <see
        /// cref="PlayerManager.Algorithms"/> collection of the current <see cref="PlayerManager"/>
        /// instance.</exception>
        /// <remarks>
        /// <b>Create</b> returns a new instance of <see cref="AlgorithmOptions"/> or of a derived
        /// class, depending on the specified <paramref name="algorithmId"/>.</remarks>

        public static AlgorithmOptions Create(string algorithmId) {
            return new AlgorithmOptions(algorithmId);
        }

        #endregion
        #region Load

        /// <summary>
        /// Loads optional settings from the specified dictionary.</summary>
        /// <param name="dictionary">
        /// A <see cref="ComputerOptionsDictionary"/> that maps the names of optional settings for
        /// the associated <see cref="Algorithm"/> to the XML representations of their values.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="dictionary"/> is a null reference.</exception>
        /// <remarks><para>
        /// <b>Load</b> sets the <see cref="TargetLimit"/>, <see cref="UseRandomBuild"/>, <see
        /// cref="UseRandomPlace"/>, and <see cref="UseScripting"/> properties to the values stored
        /// under the eponymous keys in the specified <paramref name="dictionary"/>, if present.
        /// </para><para>
        /// Derived classes should override <b>Load</b> to read any additional property values that
        /// represent optional settings for the associated <see cref="Algorithm"/>.</para></remarks>

        public virtual void Load(ComputerOptionsDictionary dictionary) {
            if (dictionary == null)
                ThrowHelper.ThrowArgumentNullException("dictionary");

            string value;
            if (dictionary.TryGetValue("TargetLimit", out value))
                TargetLimit = XmlConvert.ToInt32(value);

            if (dictionary.TryGetValue("UseRandomBuild", out value))
                UseRandomBuild = XmlConvert.ToBoolean(value);

            if (dictionary.TryGetValue("UseRandomPlace", out value))
                UseRandomPlace = XmlConvert.ToBoolean(value);

            if (dictionary.TryGetValue("UseScripting", out value))
                UseScripting = XmlConvert.ToBoolean(value);
        }

        #endregion
        #region Save

        /// <summary>
        /// Saves optional settings to the specified dictionary.</summary>
        /// <param name="dictionary">
        /// A <see cref="ComputerOptionsDictionary"/> that maps the names of optional settings for
        /// the associated <see cref="Algorithm"/> to the XML representations of their values.
        /// </param>
        /// <exception cref="ArgumentException">
        /// <paramref name="dictionary"/> is read-only.</exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="dictionary"/> is a null reference.</exception>
        /// <remarks><para>
        /// <b>Save</b> clears the specified <paramref name="dictionary"/>, and then saves the
        /// values of the <see cref="TargetLimit"/>, <see cref="UseRandomBuild"/>, <see
        /// cref="UseRandomPlace"/>, and <see cref="UseScripting"/> properties under eponymous keys.
        /// </para><para>
        /// Derived classes should override <b>Save</b> to save any additional property values that
        /// represent optional settings for the associated <see cref="Algorithm"/>.</para></remarks>

        public virtual void Save(ComputerOptionsDictionary dictionary) {
            if (dictionary == null)
                ThrowHelper.ThrowArgumentNullException("dictionary");
            if (dictionary.IsReadOnly)
                ThrowHelper.ThrowArgumentException("dictionary", Tektosyne.Strings.ArgumentReadOnly);

            dictionary.Clear();
            dictionary["TargetLimit"] = XmlConvert.ToString(TargetLimit);
            dictionary["UseRandomBuild"] = XmlConvert.ToString(UseRandomBuild);
            dictionary["UseRandomPlace"] = XmlConvert.ToString(UseRandomPlace);
            dictionary["UseScripting"] = XmlConvert.ToString(UseScripting);
        }

        #endregion
        #region IKeyedValue Members

        /// <summary>
        /// Gets the computer player algorithm whose optional settings are managed by the <see
        /// cref="AlgorithmOptions"/> object.</summary>
        /// <value>
        /// The value of the <see cref="Algorithm"/> property.</value>

        Algorithm IKeyedValue<Algorithm>.Key {
            [DebuggerStepThrough]
            get { return Algorithm; }
        }

        #endregion
        #region XmlSerializable Members
        #region ConstXmlName

        /// <summary>
        /// The name of the XML element associated with the <see cref="AlgorithmOptions"/> class.
        /// </summary>
        /// <remarks>
        /// <b>ConstXmlName</b> holds the value "options", indicating the XML element in <see
        /// cref="FilePaths.SessionSchema"/> whose data is managed by the <see
        /// cref="AlgorithmOptions"/> class.</remarks>

        public const string ConstXmlName = "options";

        #endregion
        #region ReadXmlAttributes

        /// <summary>
        /// Reads XML attribute data into the <see cref="AlgorithmOptions"/> object using the
        /// specified <see cref="XmlReader"/>.</summary>
        /// <param name="reader">
        /// The <see cref="XmlReader"/> from which to read.</param>
        /// <exception cref="XmlException">
        /// An error occurred while parsing the XML data provided by <paramref name="reader"/>.
        /// </exception>
        /// <exception cref="System.Xml.Schema.XmlSchemaException">
        /// <paramref name="reader"/> is an <see cref="XmlValidatingReader"/>, and the XML data did
        /// not conform to <see cref="FilePaths.SessionSchema"/>.</exception>
        /// <remarks>
        /// Please refer to class <see cref="XmlSerializable"/> for a complete description of this
        /// method.</remarks>

        protected override sealed void ReadXmlAttributes(XmlReader reader) {
            /*
             * We do nothing in this method since all XML attributes of the
             * <options> element are read by ComputerPlayer.ReadXmlElements.
             * 
             * This is necessary to properly construct an AlgorithmOptions
             * object before we can call ReadXmlElements on that object.
             */
        }

        #endregion
        #region ReadXmlElements

        /// <summary>
        /// Reads XML element data into the <see cref="AlgorithmOptions"/> object using the
        /// specified <see cref="XmlReader"/>.</summary>
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
        /// not conform to <see cref="FilePaths.SessionSchema"/>.</exception>
        /// <remarks>
        /// Please refer to class <see cref="XmlSerializable"/> for a complete description of this
        /// method.</remarks>

        protected override bool ReadXmlElements(XmlReader reader) {
            switch (reader.Name) {

                case "targetLimit":
                    XmlUtility.ReadAttributeAsInt32(reader, "value", ref this._targetLimit);
                    return true;

                case "useRandomBuild":
                    XmlUtility.ReadAttributeAsBoolean(reader, "value", ref this._useRandomBuild);
                    return true;

                case "useRandomPlace":
                    XmlUtility.ReadAttributeAsBoolean(reader, "value", ref this._useRandomPlace);
                    return true;

                case "useScripting":
                    XmlUtility.ReadAttributeAsBoolean(reader, "value", ref this._useScripting);
                    return true;

                default: return false;
            }
        }

        #endregion
        #region WriteXmlAttributes

        /// <summary>
        /// Writes all current data of the <see cref="AlgorithmOptions"/> object that is serialized
        /// to XML attributes to the specified <see cref="XmlWriter"/>.</summary>
        /// <param name="writer">
        /// The <see cref="XmlWriter"/> to which to write.</param>
        /// <remarks>
        /// Please refer to class <see cref="XmlSerializable"/> for a complete description of this
        /// method.</remarks>

        protected override sealed void WriteXmlAttributes(XmlWriter writer) {
            writer.WriteAttributeString("algorithm", Algorithm.Id);
        }

        #endregion
        #region WriteXmlElements

        /// <summary>
        /// Writes all current data of the <see cref="AlgorithmOptions"/> object that is serialized
        /// to nested XML elements to the specified <see cref="XmlWriter"/>.</summary>
        /// <param name="writer">
        /// The <see cref="XmlWriter"/> to which to write.</param>
        /// <remarks>
        /// Please refer to class <see cref="XmlSerializable"/> for a complete description of this
        /// method.</remarks>

        protected override void WriteXmlElements(XmlWriter writer) {

            WriteXmlOption(writer, "targetLimit", XmlConvert.ToString(TargetLimit));
            WriteXmlOption(writer, "useRandomBuild", XmlConvert.ToString(UseRandomBuild));
            WriteXmlOption(writer, "useRandomPlace", XmlConvert.ToString(UseRandomPlace));
            WriteXmlOption(writer, "useScripting", XmlConvert.ToString(UseScripting));
        }

        #endregion
        #region WriteXmlOption

        /// <summary>
        /// Writes the specified algorithm option to the specified <see cref="XmlWriter"/>, using
        /// the specified element name.</summary>
        /// <param name="writer">
        /// The <see cref="XmlWriter"/> to which to write.</param>
        /// <param name="name">
        /// The name of the XML element to write to <paramref name="writer"/>.</param>
        /// <param name="value">
        /// A <see cref="String"/> representation of the algorithm option to write to <paramref
        /// name="writer"/>.</param>
        /// <remarks>
        /// <b>WriteStringValue</b> writes an empty XML element to the specified <paramref
        /// name="writer"/> that has the specified <paramref name="name"/> and contains a single
        /// attribute named "value" that holds the specified <paramref name="value"/>.</remarks>

        private static void WriteXmlOption(XmlWriter writer, string name, string value) {

            writer.WriteStartElement(name);
            writer.WriteAttributeString("value", value);
            writer.WriteEndElement();
        }

        #endregion
        #endregion
    }
}
