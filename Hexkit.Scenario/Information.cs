using System;
using System.Diagnostics;
using System.Xml;

using Tektosyne;
using Tektosyne.Collections;
using Hexkit.Global;

namespace Hexkit.Scenario {
    #region Type Aliases

    using ParagraphList = ListEx<String>;

    #endregion

    /// <summary>
    /// Provides information about a <see cref="MasterSection"/> or an <see cref="ImageSection"/>.
    /// </summary>
    /// <remarks>
    /// <b>Information</b> is serialized to the XML element "info" defined in <see
    /// cref="FilePaths.ScenarioSchema"/>.</remarks>

    public sealed class Information: ScenarioElement {
        #region Private Fields

        // property backers
        private string _author, _version, _legal;
        private readonly ParagraphList _paragraphs = new ParagraphList();

        #endregion
        #region Author

        /// <summary>
        /// Gets or sets the author of the containing <see cref="ScenarioElement"/>.</summary>
        /// <value>
        /// A <see cref="String"/> indicating the author(s) of the containing <see
        /// cref="ScenarioElement"/>. The default is an empty string.</value>
        /// <exception cref="InvalidOperationException">
        /// The property is set, and <see cref="ApplicationInfo.IsEditor"/> is <c>false</c>.
        /// </exception>
        /// <remarks>
        /// <b>Author</b> returns an empty string when set to a null reference. This property holds
        /// the value of the "author" XML element.</remarks>

        public string Author {
            [DebuggerStepThrough]
            get { return this._author ?? ""; }
            [DebuggerStepThrough]
            set {
                ApplicationInfo.CheckEditor();
                this._author = value;
            }
        }

        #endregion
        #region Legal

        /// <summary>
        /// Gets or sets legal information for the containing <see cref="ScenarioElement"/>.
        /// </summary>
        /// <value>
        /// A <see cref="String"/> with legal information for the containing <see
        /// cref="ScenarioElement"/>. The default is an empty string.</value>
        /// <exception cref="InvalidOperationException">
        /// The property is set, and <see cref="ApplicationInfo.IsEditor"/> is <c>false</c>.
        /// </exception>
        /// <remarks>
        /// <b>Legal</b> returns an empty string when set to a null reference. This property holds
        /// the value of the "legal" XML element. It should contain any applicable copyright or
        /// trademark notices.</remarks>

        public string Legal {
            [DebuggerStepThrough]
            get { return this._legal ?? ""; }
            [DebuggerStepThrough]
            set {
                ApplicationInfo.CheckEditor();
                this._legal = value;
            }
        }

        #endregion
        #region Paragraphs

        /// <summary>
        /// Gets a list of paragraphs describing the containing <see cref="ScenarioElement"/>.
        /// </summary>
        /// <value>
        /// A <see cref="ParagraphList"/> containing a sequence of paragraphs with additional
        /// information about the containing <see cref="ScenarioElement"/>. The default is an empty
        /// collection.</value>
        /// <remarks><para>
        /// <b>Paragraphs</b> never returns a null reference. The collection is read-only if <see
        /// cref="ApplicationInfo.IsEditor"/> is <c>false</c>.
        /// </para><para>
        /// This property holds the values of all "para" XML elements. Empty XML elements are stored
        /// as empty strings, and should be displayed as blank lines between paragraphs.
        /// </para></remarks>

        public ParagraphList Paragraphs {
            [DebuggerStepThrough]
            get {
                return (ApplicationInfo.IsEditor ?
                    this._paragraphs : this._paragraphs.AsReadOnly());
            }
        }

        #endregion
        #region Version

        /// <summary>
        /// Gets or sets version information for the containing <see cref="ScenarioElement"/>.
        /// </summary>
        /// <value>
        /// A <see cref="String"/> with version information for the containing <see
        /// cref="ScenarioElement"/>. The default is an empty string.</value>
        /// <exception cref="InvalidOperationException">
        /// The property is set, and <see cref="ApplicationInfo.IsEditor"/> is <c>false</c>.
        /// </exception>
        /// <remarks>
        /// <b>Version</b> returns an empty string when set to a null reference. This property holds
        /// the value of the "version" XML element. It might contain a creation date in addition to,
        /// or instead of a version number.</remarks>

        public string Version {
            [DebuggerStepThrough]
            get { return this._version ?? ""; }
            [DebuggerStepThrough]
            set {
                ApplicationInfo.CheckEditor();
                this._version = value;
            }
        }

        #endregion
        #region XmlSerializable Members
        #region ConstXmlName

        /// <summary>
        /// The name of the XML element associated with the <see cref="Information"/> class.
        /// </summary>
        /// <remarks>
        /// <b>ConstXmlName</b> holds the value "info", indicating the XML element in <see
        /// cref="FilePaths.ScenarioSchema"/> whose data is managed by the <see
        /// cref="Information"/> class.</remarks>

        public const string ConstXmlName = "info";

        #endregion
        #region ReadXmlElements

        /// <summary>
        /// Reads XML element data into the <see cref="Information"/> object using the specified
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
            string element = null;
            switch (reader.Name) {

                case "author":
                    element = reader.ReadString();
                    this._author = element.PackSpace();
                    return true;

                case "version":
                    element = reader.ReadString();
                    this._version = element.PackSpace();
                    return true;

                case "legal":
                    element = reader.ReadString();
                    this._legal = element.PackSpace();
                    return true;

                case "para":
                    element = reader.ReadString();
                    this._paragraphs.Add(element.PackSpace());
                    return true;

                default: return false;
            }
        }

        #endregion
        #region WriteXmlElements

        /// <summary>
        /// Writes all current data of the <see cref="Information"/> object that is serialized to
        /// nested XML elements to the specified <see cref="XmlWriter"/>.</summary>
        /// <param name="writer">
        /// The <see cref="XmlWriter"/> to which to write.</param>
        /// <remarks>
        /// Please refer to class <see cref="XmlSerializable"/> for a complete description of this
        /// method.</remarks>

        protected override void WriteXmlElements(XmlWriter writer) {

            writer.WriteElementString("author", Author);
            writer.WriteElementString("version", Version);
            writer.WriteElementString("legal", Legal);

            WriteXmlParagraphs(writer, Paragraphs);
        }

        #endregion
        #region WriteXmlParagraphs

        /// <summary>
        /// Writes the specified collection of paragraphs to the specified <see cref="XmlWriter"/>.
        /// </summary>
        /// <param name="writer">
        /// The <see cref="XmlWriter"/> to which to write.</param>
        /// <param name="paragraphs">
        /// A <see cref="ParagraphList"/> containing the paragraphs to write.</param>
        /// <remarks><para>
        /// <b>WriteXmlParagraphs</b> writes the contents of the specified <paramref
        /// name="paragraphs"/> collection to the specified <paramref name="writer"/>. Each element
        /// of the collection is written as the text content of an XML element named "para".
        /// </para><para>
        /// <b>WriteXmlParagraphs</b> does not write anything if all elements of the specified
        /// <paramref name="paragraphs"/> collection are null references or empty strings.
        /// </para></remarks>

        internal static void WriteXmlParagraphs(XmlWriter writer, ParagraphList paragraphs) {

            bool nonEmptyParas = false;
            foreach (string para in paragraphs)
                nonEmptyParas |= !String.IsNullOrEmpty(para);

            if (nonEmptyParas)
                foreach (string para in paragraphs)
                    writer.WriteElementString("para", para);
        }

        #endregion
        #endregion
    }
}
