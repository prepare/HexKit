using System.Diagnostics;
using System.Windows;
using System.Xml;

using Tektosyne.Geometry;
using Tektosyne.Xml;
using Hexkit.Global;

using Rectangle = System.Drawing.Rectangle;
using SystemInformation = System.Windows.Forms.SystemInformation;

namespace Hexkit.Options {

    /// <summary>
    /// Manages user settings related to a specific desktop size.</summary>
    /// <remarks><para>
    /// A given <b>DesktopOptions</b> object manages all user settings that are related to the
    /// desktop size indicated by its <see cref="DesktopOptions.VirtualScreen"/> property.
    /// </para><para>
    /// All properties of this class are read-only for external clients. Call <see
    /// cref="ViewOptions.SaveDesktop"/> to set the properties of a <b>DesktopOptions</b> object.
    /// </para><para>
    /// <b>DesktopOptions</b> is serialized to the XML element "desktop" defined in <see
    /// cref="FilePaths.OptionsSchema"/>.</para></remarks>

    public sealed class DesktopOptions: XmlSerializable {
        #region DesktopOptions(Rectangle)

        /// <summary>
        /// Initializes a new instance of the <see cref="DesktopOptions"/> class with the specified
        /// virtual screen dimensions.</summary>
        /// <param name="virtualScreen">
        /// The initial value for the <see cref="VirtualScreen"/> property.</param>

        internal DesktopOptions(Rectangle virtualScreen) {
            VirtualScreen = virtualScreen;
        }

        #endregion
        #region Private Fields

        // property backers
        private bool _windowMaximized = false;

        #endregion
        #region VirtualScreen

        /// <summary>
        /// Gets the virtual screen dimensions associated with the <see cref="DesktopOptions"/>
        /// object.</summary>
        /// <value>
        /// A <see cref="Rectangle"/> indicating the virtual screen dimensions to which the settings
        /// stored in this <see cref="DesktopOptions"/> object apply.</value>
        /// <remarks><para>
        /// <b>VirtualScreen</b> holds the value of the "bounds" XML element.
        /// </para><para>
        /// <see cref="ViewOptions.LoadDesktop"/> and <see cref="ViewOptions.SaveDesktop"/> compare
        /// the value of the <see cref="SystemInformation.VirtualScreen"/> property of the <see
        /// cref="SystemInformation"/> class with the value returned by this property to determine
        /// which <see cref="DesktopOptions"/> object to read from or to write to.</para></remarks>

        public Rectangle VirtualScreen { get; private set; }

        #endregion
        #region WindowBounds

        /// <summary>
        /// Gets or sets the default <see cref="Window.RestoreBounds"/> for the application <see
        /// cref="Window"/>.</summary>
        /// <value>
        /// A <see cref="Rect"/> indicating the default value for the <see
        /// cref="Window.RestoreBounds"/> property of the application <see cref="Window"/>. The
        /// default is an empty <see cref="Rect"/>, indicating that the application should use the
        /// default size and location.</value>
        /// <remarks><para>
        /// <b>WindowBounds</b> holds the value of the "bounds" child element of the "window" XML
        /// element.
        /// </para><para>
        /// <b>WindowBounds</b> reflects the normal (non-maximized) state of the application window,
        /// so that the user can return to these bounds from a maximized window. If the window is
        /// maximized when <see cref="ViewOptions.SaveDesktop"/> is called, only the <see
        /// cref="WindowMaximized"/> flag is set but <b>WindowBounds</b> is not changed.
        /// </para></remarks>

        public Rect WindowBounds { get; internal set; }

        #endregion
        #region WindowMaximized

        /// <summary>
        /// Gets or sets a value indicating whether the application window is maximized by default.
        /// </summary>
        /// <value>
        /// <c>true</c> if the application window is maximized by default; otherwise, <c>false</c>.
        /// The default is <c>false</c>.</value>
        /// <remarks>
        /// <b>WindowMaximized</b> holds the value of the "maximized" attribute of the "window" XML
        /// element.</remarks>

        public bool WindowMaximized {
            [DebuggerStepThrough]
            get { return this._windowMaximized; }
            [DebuggerStepThrough]
            internal set { this._windowMaximized = value; }
        }

        #endregion
        #region XmlSerializable Members
        #region ConstXmlName

        /// <summary>
        /// The name of the XML element associated with the <see cref="DesktopOptions"/> class.
        /// </summary>
        /// <remarks>
        /// <b>ConstXmlName</b> holds the value "desktop", indicating the XML element in <see
        /// cref="FilePaths.OptionsSchema"/> whose data is managed by the <see
        /// cref="DesktopOptions"/> class.</remarks>

        public const string ConstXmlName = "desktop";

        #endregion
        #region ReadXmlElements

        /// <summary>
        /// Reads XML element data into the <see cref="DesktopOptions"/> object using the specified
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

                case "bounds":
                    VirtualScreen = SimpleXml.ReadRectI(reader).ToGdiRect();
                    return true;

                case "window": {
                    XmlUtility.ReadAttributeAsBoolean(reader,
                        "maximized", ref this._windowMaximized);

                    // read "bounds" element nested into "window" element
                    if (!reader.IsEmptyElement)
                        while (reader.Read() && reader.IsStartElement())
                            if (reader.Name == "bounds")
                                WindowBounds = SimpleXml.ReadWpfRect(reader);
                            else
                                XmlUtility.MoveToEndElement(reader);

                    return true;
                }

                default: return false;
            }
        }

        #endregion
        #region WriteXmlElements

        /// <summary>
        /// Writes all current data of the <see cref="DesktopOptions"/> object that is serialized to
        /// nested XML elements to the specified <see cref="XmlWriter"/>.</summary>
        /// <param name="writer">
        /// The <see cref="XmlWriter"/> to which to write.</param>
        /// <remarks>
        /// Please refer to class <see cref="XmlSerializable"/> for a complete description of this
        /// method.</remarks>

        protected override void WriteXmlElements(XmlWriter writer) {

            writer.WriteStartElement("bounds");
            SimpleXml.WriteRectI(writer, VirtualScreen.ToRectI());
            writer.WriteEndElement();

            writer.WriteStartElement("window");
            writer.WriteAttributeString("maximized", XmlConvert.ToString(WindowMaximized));

            writer.WriteStartElement("bounds");
            SimpleXml.WriteWpfRect(writer, WindowBounds);
            writer.WriteEndElement();

            writer.WriteEndElement();
        }

        #endregion
        #endregion
    }
}
