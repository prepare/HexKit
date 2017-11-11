using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Xml;

using Tektosyne;
using Tektosyne.Xml;
using Hexkit.Global;

namespace Hexkit.Options {

    /// <summary>
    /// Manages user settings related to interactive command replay.</summary>
    /// <remarks>
    /// <b>ReplayOptions</b> is serialized to the XML element "replay" defined in <see
    /// cref="FilePaths.OptionsSchema"/>.</remarks>

    public sealed class ReplayOptions: XmlSerializable {
        #region ReplayOptions(EventHandler)

        /// <summary>
        /// Initializes a new instance of the <see cref="ReplayOptions"/> class with the specified
        /// event handler.</summary>
        /// <param name="onOptionsChanged">
        /// An <see cref="EventHandler"/> to be invoked whenever an option managed by the new <see
        /// cref="ReplayOptions"/> instance changes.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="onOptionsChanged"/> is a null reference.</exception>

        internal ReplayOptions(EventHandler onOptionsChanged) {
            if (onOptionsChanged == null)
                ThrowHelper.ThrowArgumentNullException("onOptionsChanged");

            this._onOptionsChanged = onOptionsChanged;
        }

        #endregion
        #region Private Fields

        // event to raise when an option has changed
        private readonly EventHandler _onOptionsChanged;

        // property backers
        private bool _scroll = true;
        private ReplaySpeed _speed = ReplaySpeed.Medium;

        #endregion
        #region Delay

        /// <summary>
        /// Gets the delay for the current replay speed.</summary>
        /// <value>
        /// The delay, in milliseconds, that corresponds to the current value of the <see
        /// cref="Speed"/> property.</value>
        /// <remarks><para>
        /// <b>Delay</b> indicates the duration of map site higlights and pauses between replayed
        /// commands that correspond to the current <see cref="Speed"/>, as follows:
        /// </para><list type="table"><listheader>
        /// <term><b>Speed</b></term><description><b>Delay</b></description>
        /// </listheader><item>
        /// <term><see cref="ReplaySpeed.Slow"/></term><description>1000 msec</description>
        /// </item><item>
        /// <term><see cref="ReplaySpeed.Medium"/></term><description>500 msec</description>
        /// </item><item>
        /// <term><see cref="ReplaySpeed.Fast"/></term><description>100 msec</description>
        /// </item><item>
        /// <term><see cref="ReplaySpeed.Turbo"/></term><description>100 msec</description>
        /// </item></list><para>
        /// <b>Delay</b> also returns 500 if <see cref="Speed"/> is not a valid <see
        /// cref="ReplaySpeed"/> value.</para></remarks>

        public int Delay {
            get {
                switch (Speed) {

                    case ReplaySpeed.Slow:
                        return 1000;

                    case ReplaySpeed.Medium:
                        return 500;

                    case ReplaySpeed.Fast:
                    case ReplaySpeed.Turbo:
                        return 100;

                    default:
                        Debug.Fail("ReplayOptions.Delay: Invalid Speed value");
                        return 500;
                }
            }
        }

        #endregion
        #region Scroll

        /// <summary>
        /// Gets or sets a value indicating whether the map view should be scrolled during a
        /// replay.</summary>
        /// <value>
        /// <c>true</c> to scroll the map view so that the effects of each command are visible;
        /// otherwise, <c>false</c>. The default is <c>true</c>.</value>
        /// <remarks><para>
        /// <b>Scroll</b> holds the value of the "scroll" XML attribute.
        /// </para><para>
        /// Setting this property calls <see cref="ApplicationOptions.OnOptionsChanged"/> to save
        /// the current settings to the options file.</para></remarks>

        public bool Scroll {
            [DebuggerStepThrough]
            get { return this._scroll; }
            set {
                this._scroll = value;
                this._onOptionsChanged(this, EventArgs.Empty);
            }
        }

        #endregion
        #region Speed

        /// <summary>
        /// Gets or sets the speed at which commands are replayed.</summary>
        /// <value>
        /// A <see cref="ReplaySpeed"/> value indicating the speed at which commands are replayed.
        /// The default is <see cref="ReplaySpeed.Medium"/>.</value>
        /// <remarks><para>
        /// <b>Speed</b> holds the value of the "speed" XML attribute.
        /// </para><para>
        /// Setting this property calls <see cref="ApplicationOptions.OnOptionsChanged"/> to save
        /// the current settings to the options file.</para></remarks>

        public ReplaySpeed Speed {
            [DebuggerStepThrough]
            get { return this._speed; }
            set {
                this._speed = value;
                this._onOptionsChanged(this, EventArgs.Empty);
            }
        }

        #endregion
        #region XmlSerializable Members
        #region ConstXmlName

        /// <summary>
        /// The name of the XML element associated with the <see cref="ReplayOptions"/> class.
        /// </summary>
        /// <remarks>
        /// <b>ConstXmlName</b> holds the value "replay", indicating the XML element in <see
        /// cref="FilePaths.OptionsSchema"/> whose data is managed by the <see
        /// cref="ReplayOptions"/> class.</remarks>

        public const string ConstXmlName = "replay";

        #endregion
        #region ReadXmlAttributes

        /// <summary>
        /// Reads XML attribute data into the <see cref="ReplayOptions"/> object using the specified
        /// <see cref="XmlReader"/>.</summary>
        /// <param name="reader">
        /// The <see cref="XmlReader"/> from which to read.</param>
        /// <exception cref="XmlException">
        /// An error occurred while parsing the XML data provided by <paramref name="reader"/>.
        /// </exception>
        /// <exception cref="System.Xml.Schema.XmlSchemaException">
        /// <paramref name="reader"/> is an <see cref="XmlValidatingReader"/>, and the XML data did
        /// not conform to <see cref="FilePaths.OptionsSchema"/>.</exception>
        /// <remarks>
        /// Please refer to class <see cref="XmlSerializable"/> for a complete description of this
        /// method.</remarks>

        protected override void ReadXmlAttributes(XmlReader reader) {
            XmlUtility.ReadAttributeAsEnum(reader, "speed", ref this._speed);
            XmlUtility.ReadAttributeAsBoolean(reader, "scroll", ref this._scroll);
        }

        #endregion
        #region WriteXmlAttributes

        /// <summary>
        /// Writes all current data of the <see cref="ReplayOptions"/> object that is serialized to
        /// XML attributes to the specified <see cref="XmlWriter"/>.</summary>
        /// <param name="writer">
        /// The <see cref="XmlWriter"/> to which to write.</param>
        /// <remarks>
        /// Please refer to class <see cref="XmlSerializable"/> for a complete description of this
        /// method.</remarks>

        protected override void WriteXmlAttributes(XmlWriter writer) {

            writer.WriteAttributeString("speed", Speed.ToString());
            writer.WriteAttributeString("scroll", XmlConvert.ToString(Scroll));
        }

        #endregion
        #endregion
    }
}
