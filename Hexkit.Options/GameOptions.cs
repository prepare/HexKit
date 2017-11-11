using System;
using System.Diagnostics;
using System.Xml;

using Tektosyne;
using Hexkit.Global;

namespace Hexkit.Options {

    /// <summary>
    /// Manages all user settings specific to Hexkit Game.</summary>
    /// <remarks>
    /// <b>GameOptions</b> is serialized to the XML element "game" defined in <see
    /// cref="FilePaths.OptionsSchema"/>.</remarks>

    public sealed class GameOptions: XmlSerializable {
        #region GameOptions(EventHandler)

        /// <summary>
        /// Initializes a new instance of the <see cref="GameOptions"/> class with the specified
        /// event handler.</summary>
        /// <param name="onOptionsChanged">
        /// An <see cref="EventHandler"/> to be invoked whenever an option managed by the new <see
        /// cref="GameOptions"/> instance changes.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="onOptionsChanged"/> is a null reference.</exception>

        internal GameOptions(EventHandler onOptionsChanged) {
            if (onOptionsChanged == null)
                ThrowHelper.ThrowArgumentNullException("onOptionsChanged");

            this._replay = new ReplayOptions(onOptionsChanged);
        }

        #endregion
        #region Private Fields

        // property backers
        private readonly ReplayOptions _replay;

        #endregion
        #region Replay

        /// <summary>
        /// Gets all user settings related to interactive command replay.</summary>
        /// <value>
        /// The <see cref="ReplayOptions"/> object that manages all user settings related to
        /// interactive replays.</value>
        /// <remarks>
        /// <b>Replay</b> never returns a null reference. This property never changes once the
        /// object has been constructed.</remarks>

        public ReplayOptions Replay {
            [DebuggerStepThrough]
            get { return this._replay; }
        }

        #endregion
        #region XmlSerializable Members
        #region ConstXmlName

        /// <summary>
        /// The name of the XML element associated with the <see cref="GameOptions"/> class.
        /// </summary>
        /// <remarks>
        /// <b>ConstXmlName</b> holds the value "game", indicating the XML element in <see
        /// cref="FilePaths.OptionsSchema"/> whose data is managed by the <see cref="GameOptions"/>
        /// class.</remarks>

        public const string ConstXmlName = "game";

        #endregion
        #region ReadXmlElements

        /// <summary>
        /// Reads XML element data into the <see cref="GameOptions"/> object using the specified
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
        /// not conform to <see cref="FilePaths.OptionsSchema"/>.</exception>
        /// <remarks>
        /// Please refer to class <see cref="XmlSerializable"/> for a complete description of this
        /// method.</remarks>

        protected override bool ReadXmlElements(XmlReader reader) {
            switch (reader.Name) {

                case ReplayOptions.ConstXmlName:
                    Replay.ReadXml(reader);
                    return true;

                default: return false;
            }
        }

        #endregion
        #region WriteXmlElements

        /// <summary>
        /// Writes all current data of the <see cref="GameOptions"/> object that is serialized to
        /// nested XML elements to the specified <see cref="XmlWriter"/>.</summary>
        /// <param name="writer">
        /// The <see cref="XmlWriter"/> to which to write.</param>
        /// <remarks>
        /// Please refer to class <see cref="XmlSerializable"/> for a complete description of this
        /// method.</remarks>

        protected override void WriteXmlElements(XmlWriter writer) {
            Replay.WriteXml(writer);
        }

        #endregion
        #endregion
    }
}
