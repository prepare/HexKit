using System;
using System.Diagnostics;
using System.Xml;

using Tektosyne;
using Tektosyne.Collections;

using Tektosyne.Xml;
using Hexkit.Global;

namespace Hexkit.Players {

    /// <summary>
    /// Manages persistent computer player data for a <see cref="Faction"/>.</summary>
    /// <remarks><para>
    /// <b>FactionState</b> provides a mechanism that allows <see cref="Algorithm"/> classes to
    /// persist computer player data between game turns and between game sessions.
    /// </para><para>
    /// Any such data is associated with a faction through the <see cref="FactionState.Faction"/>
    /// property, and also stores the <see cref="FactionState.Turn"/> index when it was last updated
    /// to detect outdated data when the controlling player or <b>Algorithm</b> has changed.
    /// </para><para>
    /// An <b>Algorithm</b> implementation should derive a private class from <b>FactionState</b>
    /// that contains any required data, and override <see cref="XmlSerializable.ReadXmlElements"/>
    /// and <see cref="XmlSerializable.WriteXmlElements"/> to serialize that data.
    /// <b>FactionState</b> objects are serialized to "state" elements in a session XML file.
    /// </para></remarks>

    public class FactionState: XmlSerializable, IKeyedValue<String> {
        #region FactionState(String)

        /// <summary>
        /// Initializes a new instance of the <see cref="FactionState"/> class with the specified
        /// <see cref="World.Faction"/> identifier.</summary>
        /// <param name="faction">
        /// The initial value for the <see cref="Faction"/> property.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="faction"/> is a null reference or an empty string.</exception>

        internal FactionState(string faction) {
            if (String.IsNullOrEmpty(faction))
                ThrowHelper.ThrowArgumentNullException("faction");

            this._faction = faction;
        }

        #endregion
        #region Private Fields

        // property backers
        private readonly string _faction;
        private int _turn = 0;

        #endregion
        #region Faction

        /// <summary>
        /// Gets the identifier of the <see cref="World.Faction"/> whose computer player data is
        /// managed by the <see cref="FactionState"/>.</summary>
        /// <value>
        /// The <see cref="World.Faction.Id"/> string of the <see cref="World.Faction"/> whose
        /// computer player data is managed by the <see cref="FactionState"/>.</value>
        /// <remarks><para>
        /// <b>Faction</b> never returns a null reference or an empty string. This property never
        /// changes once the object has been constructed.</para></remarks>

        public string Faction {
            [DebuggerStepThrough]
            get { return this._faction; }
        }

        #endregion
        #region Turn

        /// <summary>
        /// Gets or sets the index of the game turn when the <see cref="FactionState"/> was last
        /// updated.</summary>
        /// <value>
        /// The zero-based index of the full game turn when the <see cref="FactionState"/> was last
        /// updated. The default is zero.</value>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The property is set to a value that is less than its current value.</exception>
        /// <remarks>
        /// <b>Turn</b> is updated to the current game turn by the <see
        /// cref="Algorithm.GetFactionState"/> method.</remarks>

        public int Turn {
            [DebuggerStepThrough]
            get { return this._turn; }
            [DebuggerStepThrough]
            set {
                if (value < this._turn)
                    ThrowHelper.ThrowArgumentOutOfRangeExceptionWithFormat(
                        "value", value, Tektosyne.Strings.ArgumentLessValue, this._turn);

                this._turn = value;
            }
        }

        #endregion
        #region IKeyedValue Members

        /// <summary>
        /// Gets the identifier of the <see cref="World.Faction"/> whose computer player data is
        /// managed by the <see cref="FactionState"/>.</summary>
        /// <value>
        /// The value of the <see cref="Faction"/> property.</value>

        string IKeyedValue<String>.Key {
            [DebuggerStepThrough]
            get { return Faction; }
        }

        #endregion
        #region XmlSerializable Members
        #region ConstXmlName

        /// <summary>
        /// The name of the XML element associated with the <see cref="FactionState"/> class.
        /// </summary>
        /// <remarks>
        /// <b>ConstXmlName</b> holds the value "state", indicating the XML element in <see
        /// cref="FilePaths.SessionSchema"/> whose data is managed by the <see
        /// cref="FactionState"/> class.</remarks>

        public const string ConstXmlName = "state";

        #endregion
        #region ReadXmlAttributes

        /// <summary>
        /// Reads XML attribute data into the <see cref="FactionState"/> object using the specified
        /// <see cref="XmlReader"/>.</summary>
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
            XmlUtility.ReadAttributeAsInt32(reader, "turn", ref this._turn);
        }

        #endregion
        #region WriteXmlAttributes

        /// <summary>
        /// Writes all current data of the <see cref="FactionState"/> object that is serialized to
        /// XML attributes to the specified <see cref="XmlWriter"/>.</summary>
        /// <param name="writer">
        /// The <see cref="XmlWriter"/> to which to write.</param>
        /// <remarks>
        /// Please refer to class <see cref="XmlSerializable"/> for a complete description of this
        /// method.</remarks>

        protected override void WriteXmlAttributes(XmlWriter writer) {

            writer.WriteAttributeString("faction", Faction);
            writer.WriteAttributeString("turn", XmlConvert.ToString(Turn));
        }

        #endregion
        #endregion
    }
}
