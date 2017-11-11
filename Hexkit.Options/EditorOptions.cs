using System;
using System.Diagnostics;
using System.Xml;

using Tektosyne;
using Hexkit.Global;
using Hexkit.Scenario;

namespace Hexkit.Options {

    /// <summary>
    /// Manages all user settings specific to Hexkit Editor.</summary>
    /// <remarks>
    /// <b>EditorOptions</b> is serialized to the XML element "editor" defined in <see
    /// cref="FilePaths.OptionsSchema"/>.</remarks>

    public sealed class EditorOptions: XmlSerializable {
        #region EditorOptions(EventHandler)

        /// <summary>
        /// Initializes a new instance of the <see cref="EditorOptions"/> class with the specified
        /// event handler.</summary>
        /// <param name="onOptionsChanged">
        /// An <see cref="EventHandler"/> to be invoked whenever an option managed by the new <see
        /// cref="EditorOptions"/> instance changes.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="onOptionsChanged"/> is a null reference.</exception>

        internal EditorOptions(EventHandler onOptionsChanged) {
            if (onOptionsChanged == null)
                ThrowHelper.ThrowArgumentNullException("onOptionsChanged");

            this._onOptionsChanged = onOptionsChanged;
            this._overlay = new OverlayImage(0.5);
        }

        #endregion
        #region Private Fields

        // event to raise when an option has changed
        private readonly EventHandler _onOptionsChanged;

        // property backers
        private readonly OverlayImage _overlay;

        #endregion
        #region Overlay

        /// <summary>
        /// Gets the <see cref="OverlayImage"/> that appears above the game map.</summary>
        /// <value>
        /// The <see cref="OverlayImage"/> that appears above the game map, but only within Hexkit
        /// Editor.</value>
        /// <remarks><para>
        /// <b>Overlay</b> always returns the same valid reference. The <see
        /// cref="OverlayImage.Opacity"/> of the <see cref="OverlayImage"/> defaults to 0.5.
        /// </para><note type="caution">
        /// Changing the returned <see cref="OverlayImage"/> does <em>not</em> automatically invoke
        /// the <see cref="EventHandler"/> supplied to the constructor. You must explicitly call
        /// <see cref="ApplicationOptions.Save"/> after changing any property values.
        /// </note></remarks>

        public OverlayImage Overlay {
            [DebuggerStepThrough]
            get { return this._overlay; }
        }

        #endregion
        #region XmlSerializable Members
        #region ConstXmlName

        /// <summary>
        /// The name of the XML element associated with the <see cref="EditorOptions"/> class.
        /// </summary>
        /// <remarks>
        /// <b>ConstXmlName</b> holds the value "editor", indicating the XML element in <see
        /// cref="FilePaths.OptionsSchema"/> whose data is managed by the <see
        /// cref="EditorOptions"/> class.</remarks>

        public const string ConstXmlName = "editor";

        #endregion
        #region ReadXmlElements

        /// <summary>
        /// Reads XML element data into the <see cref="EditorOptions"/> object using the specified
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

                case OverlayImage.ConstXmlName:
                    Overlay.ReadXml(reader);
                    return true;

                default: return false;
            }
        }

        #endregion
        #region WriteXmlElements

        /// <summary>
        /// Writes all current data of the <see cref="EditorOptions"/> object that is serialized to
        /// nested XML elements to the specified <see cref="XmlWriter"/>.</summary>
        /// <param name="writer">
        /// The <see cref="XmlWriter"/> to which to write.</param>
        /// <remarks>
        /// Please refer to class <see cref="XmlSerializable"/> for a complete description of this
        /// method.</remarks>

        protected override void WriteXmlElements(XmlWriter writer) {
            if (!Overlay.IsEmpty)
                Overlay.WriteXml(writer);
        }

        #endregion
        #endregion
    }
}
