using System;
using System.Diagnostics;
using System.Xml;

using Hexkit.Global;

namespace Hexkit.Players {

    /// <summary>
    /// Manages all data for a human player.</summary>
    /// <remarks>
    /// <b>HumanPlayer</b> is serialized to the XML element "human" defined in <see
    /// cref="FilePaths.SessionSchema"/>.</remarks>

    public sealed class HumanPlayer: Player {
        #region HumanPlayer(Int32)

        /// <summary>
        /// Initializes a new instance of the <see cref="HumanPlayer"/> class with the specified
        /// index.</summary>
        /// <param name="index">
        /// The initial value for the <see cref="Player.Index"/> property.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="index"/> is negative.</exception>
        /// <remarks><para>
        /// The following table shows the initial property values for the new instance of <see
        /// cref="HumanPlayer"/>:
        /// </para><list type="table"><listheader>
        /// <term>Property</term><description>Value</description>
        /// </listheader><item>
        /// <term><see cref="Player.DefaultName"/></term>
        /// <description>The localized string "Human Player ", followed by the value of <paramref
        /// name="index"/> plus one.</description>
        /// </item><item>
        /// <term><see cref="Email"/></term>
        /// <description>An empty string.</description>
        /// </item><item>
        /// <term><see cref="Player.Factions"/></term>
        /// <description>An empty collection.</description>
        /// </item><item>
        /// <term><see cref="Player.Index"/></term>
        /// <description>The value of <paramref name="index"/>.</description>
        /// </item><item>
        /// <term><see cref="Player.Name"/></term>
        /// <description>The value of <see cref="Player.DefaultName"/>.</description>
        /// </item></list></remarks>

        internal HumanPlayer(int index): base(index, Global.Strings.LabelPlayerHuman) { }

        #endregion
        #region Private Fields

        // property backers
        private string _email;

        #endregion
        #region Email

        /// <summary>
        /// Gets or sets the e-mail address of the <see cref="HumanPlayer"/>.</summary>
        /// <value>
        /// The e-mail address of the remote human player managed by the <see cref="HumanPlayer"/>,
        /// or an empty string if the game is played in "hotseat" mode. The default is an empty
        /// string.</value>
        /// <remarks>
        /// <b>Email</b> returns an empty string when set to a null reference. This property holds
        /// the value of the "email" XML element.</remarks>

        public string Email {
            [DebuggerStepThrough]
            get { return this._email ?? ""; }
            [DebuggerStepThrough]
            set { this._email = value; }
        }

        #endregion
        #region XmlSerializable Members
        #region ConstXmlName

        /// <summary>
        /// The name of the XML element associated with the <see cref="HumanPlayer"/> class.
        /// </summary>
        /// <remarks>
        /// <b>ConstXmlName</b> holds the value "human", indicating the XML element in <see
        /// cref="FilePaths.SessionSchema"/> whose data is managed by the <see cref="HumanPlayer"/>
        /// class.</remarks>

        public const string ConstXmlName = "human";

        #endregion
        #region ReadXmlElements

        /// <summary>
        /// Reads XML element data into the <see cref="HumanPlayer"/> object using the specified
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
        /// not conform to <see cref="FilePaths.SessionSchema"/>.</exception>
        /// <remarks>
        /// Please refer to class <see cref="XmlSerializable"/> for a complete description of this
        /// method.</remarks>

        protected override bool ReadXmlElements(XmlReader reader) {
            if (base.ReadXmlElements(reader))
                return true;

            switch (reader.Name) {

                case "email":
                    // may be an empty string
                    Email = reader.ReadString();
                    return true;

                default: return false;
            }
        }

        #endregion
        #region WriteXmlElements

        /// <summary>
        /// Writes all current data of the <see cref="HumanPlayer"/> object that is serialized to
        /// nested XML elements to the specified <see cref="XmlWriter"/>.</summary>
        /// <param name="writer">
        /// The <see cref="XmlWriter"/> to which to write.</param>
        /// <remarks>
        /// Please refer to class <see cref="XmlSerializable"/> for a complete description of this
        /// method.</remarks>

        protected override void WriteXmlElements(XmlWriter writer) {
            base.WriteXmlElements(writer);

            if (!String.IsNullOrEmpty(Email))
                writer.WriteElementString("email", Email);
        }

        #endregion
        #endregion
    }
}
