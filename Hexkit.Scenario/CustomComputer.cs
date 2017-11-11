using System;
using System.Diagnostics;
using System.Xml;

using Tektosyne;
using Tektosyne.Collections;
using Tektosyne.Xml;
using Hexkit.Global;

namespace Hexkit.Scenario {
    #region Type Aliases

    using ComputerOptionsDictionary = SortedListEx<String, String>;
    
    #endregion

    /// <summary>
    /// Represents customized settings for a computer player.</summary>
    /// <remarks>
    /// <b>CustomComputer</b> is serialized to the XML element "computer" defined in <see
    /// cref="FilePaths.ScenarioSchema"/>.</remarks>

    public sealed class CustomComputer: ScenarioElement, ICloneable {
        #region CustomComputer()

        /// <overloads>
        /// Initializes a new instance of the <see cref="CustomComputer"/> class.</overloads>
        /// <summary>
        /// Initializes a new instance of the <see cref="CustomComputer"/> class with default
        /// properties.</summary>

        public CustomComputer() { }

        #endregion
        #region CustomComputer(CustomComputer)

        /// <summary>
        /// Initializes a new instance of the <see cref="CustomComputer"/> class with property
        /// values copied from the specified instance.</summary>
        /// <param name="computer">
        /// The <see cref="CustomComputer"/> instance whose property values are copied to the new
        /// instance.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="computer"/> is a null reference.</exception>
        /// <remarks>
        /// This constructor is called by <see cref="Clone"/> to perform a deep copy of the
        /// specified <paramref name="computer"/>, including a deep copy of all mutable objects that
        /// are owned by the <paramref name="computer"/>.</remarks>

        public CustomComputer(CustomComputer computer) {
            if (computer == null)
                ThrowHelper.ThrowArgumentNullException("computer");

            this._algorithm = computer._algorithm;
            this._options.AddRange(computer._options);
        }

        #endregion
        #region Private Fields

        // property backers
        private string _algorithm;
        private readonly ComputerOptionsDictionary _options = new ComputerOptionsDictionary();

        #endregion
        #region Algorithm

        /// <summary>
        /// Gets or sets the algorithm used by the <see cref="CustomComputer"/>.</summary>
        /// <value>
        /// The identifier of the computer player algorithm whose settings are controlled by the
        /// <see cref="CustomComputer"/>. The default is an empty string.</value>
        /// <exception cref="InvalidOperationException">
        /// The property is set, and <see cref="ApplicationInfo.IsEditor"/> is <c>false</c>.
        /// </exception>
        /// <remarks>
        /// <b>Algorithm</b> returns an empty string when set to a null reference. This property
        /// holds the value of the "algorithm" XML attribute.</remarks>

        public string Algorithm {
            [DebuggerStepThrough]
            get { return this._algorithm ?? ""; }
            [DebuggerStepThrough]
            set {
                ApplicationInfo.CheckEditor();
                if (value != null) value = String.Intern(value);
                this._algorithm = value;
            }
        }

        #endregion
        #region IsValid

        /// <summary>
        /// Gets a value indicating whether the <see cref="CustomComputer"/> contains valid data.
        /// </summary>
        /// <value>
        /// <c>true</c> if <see cref="Algorithm"/> is neither a null reference nor an empty string;
        /// otherwise, <c>false</c>. The default is <c>false</c>.</value>
        /// <remarks>
        /// Clients should check <b>IsValid</b> before attempting to assign the data stored in the 
        /// <see cref="CustomComputer"/> to a computer player.</remarks>

        public bool IsValid {
            [DebuggerStepThrough]
            get { return !String.IsNullOrEmpty(Algorithm); }
        }

        #endregion
        #region Options

        /// <summary>
        /// Gets a list of all <see cref="Algorithm"/> options defined for the <see
        /// cref="CustomComputer"/>.</summary>
        /// <value>
        /// A <see cref="ComputerOptionsDictionary"/> that maps the names of optional settings for
        /// the associated <see cref="Algorithm"/> to the XML representations of their values. The
        /// default is an empty collection.</value>
        /// <remarks><para>
        /// <b>Options</b> never returns a null reference. The collection is read-only if <see
        /// cref="ApplicationInfo.IsEditor"/> is <c>false</c>.
        /// </para><para>
        /// This property holds the values of all "option" XML elements. The range of valid names
        /// and values depends on the associated <see cref="Algorithm"/>.</para></remarks>

        public ComputerOptionsDictionary Options {
            [DebuggerStepThrough]
            get {
                return (ApplicationInfo.IsEditor ?
                    this._options : this._options.AsReadOnly());
            }
        }

        #endregion
        #region Clear

        /// <summary>
        /// Clears all data stored in the <see cref="CustomComputer"/>.</summary>
        /// <exception cref="InvalidOperationException">
        /// <see cref="ApplicationInfo.IsEditor"/> is <c>false</c>.</exception>
        /// <remarks>
        /// <b>Clear</b> resets <see cref="Algorithm"/> to a null reference and removes all elements
        /// from the <see cref="Options"/> collection. When <b>Clear</b> has returned, <see
        /// cref="IsValid"/> will return <c>false</c>.</remarks>

        public void Clear() {
            ApplicationInfo.CheckEditor();

            this._algorithm = null;
            this._options.Clear();
        }

        #endregion
        #region ICloneable Members

        /// <summary>
        /// Creates a new <see cref="CustomComputer"/> object that is a deep copy of the current
        /// instance.</summary>
        /// <returns>
        /// A new <see cref="CustomComputer"/> object that is a deep copy of the current instance.
        /// </returns>
        /// <remarks>
        /// <b>Clone</b> calls the <see cref="CustomComputer(CustomComputer)"/> copy constructor
        /// with this <see cref="CustomComputer"/> object.</remarks>

        public object Clone() {
            return new CustomComputer(this);
        }

        #endregion
        #region XmlSerializable Members
        #region ConstXmlName

        /// <summary>
        /// The name of the XML element associated with the <see cref="CustomComputer"/> class.
        /// </summary>
        /// <remarks>
        /// <b>ConstXmlName</b> holds the value "computer", indicating the XML element in <see
        /// cref="FilePaths.ScenarioSchema"/> whose data is managed by the <see
        /// cref="CustomComputer"/> class.</remarks>

        public const string ConstXmlName = "computer";

        #endregion
        #region ReadXmlAttributes

        /// <summary>
        /// Reads XML attribute data into the <see cref="CustomComputer"/> object using the
        /// specified <see cref="XmlReader"/>.</summary>
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
            XmlUtility.ReadAttributeAsString(reader, "algorithm", ref this._algorithm);
        }

        #endregion
        #region ReadXmlElements

        /// <summary>
        /// Reads XML element data into the <see cref="CustomComputer"/> object using the specified
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

                case "option":
                    string name = reader["name"];
                    string value = reader["value"];

                    if (!String.IsNullOrEmpty(name))
                        this._options[name] = value;

                    return true;

                default: return false;
            }
        }

        #endregion
        #region WriteXmlAttributes

        /// <summary>
        /// Writes all current data of the <see cref="CustomComputer"/> object that is serialized to
        /// XML attributes to the specified <see cref="XmlWriter"/>.</summary>
        /// <param name="writer">
        /// The <see cref="XmlWriter"/> to which to write.</param>
        /// <remarks>
        /// Please refer to class <see cref="XmlSerializable"/> for a complete description of this
        /// method.</remarks>

        protected override void WriteXmlAttributes(XmlWriter writer) {
            if (!IsValid) return;

            writer.WriteAttributeString("algorithm", Algorithm);
        }

        #endregion
        #region WriteXmlElements

        /// <summary>
        /// Writes all current data of the <see cref="CustomComputer"/> object that is serialized to
        /// nested XML elements to the specified <see cref="XmlWriter"/>.</summary>
        /// <param name="writer">
        /// The <see cref="XmlWriter"/> to which to write.</param>
        /// <remarks>
        /// Please refer to class <see cref="XmlSerializable"/> for a complete description of this
        /// method.</remarks>

        protected override void WriteXmlElements(XmlWriter writer) {
            if (!IsValid) return;

            foreach (var pair in Options) {
                writer.WriteStartElement("option");
                writer.WriteAttributeString("name", pair.Key);
                writer.WriteAttributeString("value", pair.Value);
                writer.WriteEndElement();
            }
        }

        #endregion
        #endregion
    }
}
