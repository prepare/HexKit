using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml;

using Tektosyne;
using Hexkit.Global;

namespace Hexkit.Players {

    /// <summary>
    /// Manages all data for a computer player.</summary>
    /// <remarks>
    /// <b>ComputerPlayer</b> is serialized to the XML element "computer" defined in <see
    /// cref="FilePaths.SessionSchema"/>.</remarks>

    public sealed class ComputerPlayer: Player {
        #region ComputerPlayer(Int32)

        /// <summary>
        /// Initializes a new instance of the <see cref="ComputerPlayer"/> class with the specified
        /// index.</summary>
        /// <param name="index">
        /// The initial value for the <see cref="Player.Index"/> property.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="index"/> is negative.</exception>
        /// <remarks><para>
        /// The following table shows the initial property values for the new instance of <see
        /// cref="ComputerPlayer"/>:
        /// </para><list type="table"><listheader>
        /// <term>Property</term><description>Value</description>
        /// </listheader><item>
        /// <term><see cref="Player.DefaultName"/></term>
        /// <description>The localized string "Computer Player", followed by the value of <paramref
        /// name="index"/> plus one.</description>
        /// </item><item>
        /// <term><see cref="Player.Factions"/></term>
        /// <description>An empty collection.</description>
        /// </item><item>
        /// <term><see cref="Player.Index"/></term>
        /// <description>The value of <paramref name="index"/>.</description>
        /// </item><item>
        /// <term><see cref="Player.Name"/></term>
        /// <description>The value of <see cref="Player.DefaultName"/>.</description>
        /// </item><item>
        /// <term><see cref="Options"/></term>
        /// <description>A new <see cref="AlgorithmOptions"/> object for the default computer player
        /// <see cref="Algorithm"/>.</description>
        /// </item></list></remarks>

        internal ComputerPlayer(int index): base(index, Global.Strings.LabelPlayerComputer) {
            this._options = AlgorithmOptions.Create();
        }

        #endregion
        #region Private Fields

        // property backers
        private AlgorithmOptions _options;

        #endregion
        #region Options

        /// <summary>
        /// Gets or sets the algorithm and optional settings used by the <see
        /// cref="ComputerPlayer"/>.</summary>
        /// <value>
        /// An <see cref="AlgorithmOptions"/> object containing the algorithm and optional settings
        /// for the <see cref="ComputerPlayer"/>.</value>
        /// <exception cref="ArgumentNullException">
        /// The property is set to a null reference.</exception>
        /// <remarks>
        /// <b>Options</b> never returns a null reference. This property defaults to the <see
        /// cref="AlgorithmOptions"/> object created by the parameterless <see
        /// cref="AlgorithmOptions.Create()"/> method.</remarks>

        public AlgorithmOptions Options {
            [DebuggerStepThrough]
            get { return this._options; }
            set {
                if (value == null)
                    ThrowHelper.ThrowArgumentNullException("value");

                this._options = value;
            }
        }

        #endregion
        #region XmlSerializable Members
        #region ConstXmlName

        /// <summary>
        /// The name of the XML element associated with the <see cref="ComputerPlayer"/> class.
        /// </summary>
        /// <remarks>
        /// <b>ConstXmlName</b> holds the value "computer", indicating the XML element in <see
        /// cref="FilePaths.SessionSchema"/> whose data is managed by the <see
        /// cref="ComputerPlayer"/> class.</remarks>

        public const string ConstXmlName = "computer";

        #endregion
        #region ReadXmlElements

        /// <summary>
        /// Reads XML element data into the <see cref="ComputerPlayer"/> object using the specified
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

            // accept only algorithm options at this point
            if (reader.Name != AlgorithmOptions.ConstXmlName)
                return false;

            // try reading algorithm ID
            string id = reader["algorithm"];
            if (!String.IsNullOrEmpty(id)) {
                try {
                    // create & read algorithm options
                    Options = AlgorithmOptions.Create(id);
                    Options.ReadXml(reader);
                }
                catch (KeyNotFoundException) {
                    ThrowHelper.ThrowXmlExceptionWithFormat(
                        Global.Strings.XmlAlgorithmInvalid, AlgorithmOptions.ConstXmlName, id);
                }
            }

            return true;
        }

        #endregion
        #region WriteXmlElements

        /// <summary>
        /// Writes all current data of the <see cref="ComputerPlayer"/> object that is serialized to
        /// nested XML elements to the specified <see cref="XmlWriter"/>.</summary>
        /// <param name="writer">
        /// The <see cref="XmlWriter"/> to which to write.</param>
        /// <remarks>
        /// Please refer to class <see cref="XmlSerializable"/> for a complete description of this
        /// method.</remarks>

        protected override void WriteXmlElements(XmlWriter writer) {
            base.WriteXmlElements(writer);

            Options.WriteXml(writer);
        }

        #endregion
        #endregion
    }
}
