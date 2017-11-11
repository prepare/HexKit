using System;
using System.Diagnostics;
using System.Xml;

using Tektosyne;
using Tektosyne.Collections;

using Hexkit.Global;
using Hexkit.World;

namespace Hexkit.Players {
    #region Type Aliases

    using IdentifierList = ListEx<String>;

    #endregion

    /// <summary>
    /// Manages common data for human and computer players.</summary>
    /// <remarks>
    /// <b>Player</b> corresponds to the complex XML type "player" defined in <see
    /// cref="FilePaths.SessionSchema"/>.</remarks>

    public abstract class Player: XmlSerializable, IKeyedValue<String> {
        #region Player(Int32, String)

        /// <summary>
        /// Initializes a new instance of the <see cref="Player"/> class with the specified index
        /// and default name.</summary>
        /// <param name="index">
        /// The initial value for the <see cref="Index"/> property.</param>
        /// <param name="name">
        /// A <see cref="String"/> that is prepended to the specified <paramref name="index"/>. The
        /// complete string is stored as the initial value for the <see cref="DefaultName"/>
        /// property.</param>
        /// <exception cref="ArgumentNullOrEmptyException">
        /// <paramref name="name"/> is a null reference or an empty string.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="index"/> is negative.</exception>
        /// <remarks><para>
        /// The following table shows the initial property values for the new instance of the <see
        /// cref="Player"/> class:
        /// </para><list type="table"><listheader>
        /// <term>Property</term><description>Value</description>
        /// </listheader><item>
        /// <term><see cref="DefaultName"/></term>
        /// <description>The specified <paramref name="name"/>, followed by the value of the
        /// specified <paramref name="index"/> plus one.</description>
        /// </item><item>
        /// <term><see cref="Factions"/></term>
        /// <description>An empty collection.</description>
        /// </item><item>
        /// <term><see cref="Index"/></term>
        /// <description>The specified <paramref name="index"/>.</description>
        /// </item><item>
        /// <term><see cref="Name"/></term>
        /// <description>The value of <see cref="DefaultName"/>.</description>
        /// </item></list></remarks>

        protected Player(int index, string name) {
            if (index < 0)
                ThrowHelper.ThrowArgumentOutOfRangeException(
                    "index", index, Tektosyne.Strings.ArgumentNegative);

            if (String.IsNullOrEmpty(name))
                ThrowHelper.ThrowArgumentNullOrEmptyException("name");

            // default name shows player index
            this._defaultName = String.Format(ApplicationInfo.Culture, "{0} {1}", name, index + 1);
            this._index = index;
        }

        #endregion
        #region Private Fields

        // property backers
        private readonly int _index;
        private readonly string _defaultName;
        private readonly IdentifierList _factions = new IdentifierList(true);
        private string _name;

        #endregion
        #region DefaultName

        /// <summary>
        /// Gets the default name of the <see cref="Player"/>.</summary>
        /// <value>
        /// A localized string indicating the exact type of this <see cref="Player"/> object and the
        /// value of the <see cref="Index"/> property.</value>
        /// <remarks><para>
        /// <b>DefaultName</b> returns the localized string "Human Player <em>n</em>" if this <see
        /// cref="Player"/> object is an instance of <see cref="HumanPlayer"/>, and "Computer Player
        /// <em>n</em>" if this object is an instance of <see cref="ComputerPlayer"/>, where
        /// <em>n</em> indicates the value of the <see cref="Index"/> property plus one.
        /// </para><para>
        /// <b>DefaultName</b> never changes once the object has been constructed.</para></remarks>

        public string DefaultName {
            [DebuggerStepThrough]
            get { return this._defaultName; }
        }

        #endregion
        #region Factions

        /// <summary>
        /// Gets a list of all factions controlled by the <see cref="Player"/>.</summary>
        /// <value>
        /// A read-only <see cref="IdentifierList"/> containing the <see cref="Faction.Id"/> strings
        /// of all factions controlled by the <see cref="Player"/>. The default is an empty
        /// collection.</value>
        /// <remarks><para>
        /// <b>Factions</b> never returns a null reference, and its elements are never empty string
        /// or null references. All elements are unique. This property holds the values of all "ids"
        /// attributes of all "factions" XML elements.
        /// </para><para>
        /// Use the <see cref="PlayerManager.SetPlayer"/> method of the current <see
        /// cref="PlayerManager"/> to modify this collection during a game.</para></remarks>

        public IdentifierList Factions {
            [DebuggerStepThrough]
            get { return this._factions.AsReadOnly(); }
        }

        #endregion
        #region Index

        /// <summary>
        /// Gets the index of the <see cref="Player"/> in the corresponding <see cref="Players"/>
        /// list.</summary>
        /// <value><para>
        /// The zero-based index of the <see cref="Player"/> in the corresponding list of the <see
        /// cref="Players"/> container to which it belongs.
        /// </para><para>
        /// That is <see cref="PlayerManager.Humans"/> if this object is an instance of <see
        /// cref="HumanPlayer"/>, and <see cref="PlayerManager.Computers"/> if this object is an
        /// instance of <see cref="ComputerPlayer"/>.</para></value>
        /// <remarks>
        /// <b>Index</b> never changes once the object has been constructed.</remarks>

        public int Index {
            [DebuggerStepThrough]
            get { return this._index; }
        }

        #endregion
        #region Name

        /// <summary>
        /// Gets or sets the display name of the <see cref="Player"/>.</summary>
        /// <value>
        /// The display name of the <see cref="Player"/>. The default is the value of the <see
        /// cref="DefaultName"/> property.</value>
        /// <remarks>
        /// <b>Name</b> returns the value of the <see cref="DefaultName"/> property when set to a
        /// null reference or an empty string. This property holds the value of the "name" XML
        /// attribute.</remarks>

        public string Name {
            [DebuggerStepThrough]
            get { return StringUtility.Validate(this._name, DefaultName); }
            [DebuggerStepThrough]
            set { this._name = value; }
        }

        #endregion
        #region WritableFactions

        /// <summary>
        /// Gets a writable list of all <see cref="Factions"/> controlled by the <see
        /// cref="Player"/>.</summary>
        /// <value>
        /// The collection that is backing the <see cref="Factions"/> property.</value>

        internal IdentifierList WritableFactions {
            [DebuggerStepThrough]
            get { return this._factions; }
        }

        #endregion
        #region ToString

        /// <summary>
        /// Returns a <see cref="String"/> that represents the <see cref="Player"/>.</summary>
        /// <returns>
        /// The value of the <see cref="Name"/> property.</returns>

        public override string ToString() {
            return Name;
        }

        #endregion
        #region IKeyedValue Members

        /// <summary>
        /// Gets the default name of the player.</summary>
        /// <value>
        /// The value of the <see cref="DefaultName"/> property.</value>

        string IKeyedValue<String>.Key {
            [DebuggerStepThrough]
            get { return DefaultName; }
        }

        #endregion
        #region XmlSerializable Members
        #region ReadXmlAttributes

        /// <summary>
        /// Reads XML attribute data into the <see cref="Player"/> object using the specified <see
        /// cref="XmlReader"/>.</summary>
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

        protected override void ReadXmlAttributes(XmlReader reader) {

            // string may be null or empty
            Name = reader["name"];
        }

        #endregion
        #region ReadXmlElements

        /// <summary>
        /// Reads XML element data into the <see cref="Player"/> object using the specified <see
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
        /// not conform to <see cref="FilePaths.SessionSchema"/>.</exception>
        /// <remarks>
        /// Please refer to class <see cref="XmlSerializable"/> for a complete description of this
        /// method.</remarks>

        protected override bool ReadXmlElements(XmlReader reader) {
            switch (reader.Name) {

                case "factions": {
                    string idRefs = reader["ids"];
                    if (idRefs != null) {

                        // add identifiers to faction list
                        foreach (string token in idRefs.Split(null))
                            if (!WritableFactions.Contains(token))
                                WritableFactions.Add(String.Intern(token));
                    }
                    return true;
                }

                default: return false;
            }
        }

        #endregion
        #region WriteXmlAttributes

        /// <summary>
        /// Writes all current data of the <see cref="Player"/> object that is serialized to XML
        /// attributes to the specified <see cref="XmlWriter"/>.</summary>
        /// <param name="writer">
        /// The <see cref="XmlWriter"/> to which to write.</param>
        /// <remarks>
        /// Please refer to class <see cref="XmlSerializable"/> for a complete description of this
        /// method.</remarks>

        protected override void WriteXmlAttributes(XmlWriter writer) {
            writer.WriteAttributeString("name", Name);
        }

        #endregion
        #region WriteXmlElements

        /// <summary>
        /// Writes all current data of the <see cref="Player"/> object that is serialized to nested
        /// XML elements to the specified <see cref="XmlWriter"/>.</summary>
        /// <param name="writer">
        /// The <see cref="XmlWriter"/> to which to write.</param>
        /// <remarks>
        /// Please refer to class <see cref="XmlSerializable"/> for a complete description of this
        /// method.</remarks>

        protected override void WriteXmlElements(XmlWriter writer) {

            if (Factions.Count > 0) {
                writer.WriteStartElement("factions");
                writer.WriteAttributeString("ids", String.Join(" ", Factions));
                writer.WriteEndElement();
            }
        }

        #endregion
        #endregion
    }
}
