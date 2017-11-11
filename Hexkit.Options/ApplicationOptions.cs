using System;
using System.IO;
using System.Windows;
using System.Xml;
using System.Xml.Schema;

using Tektosyne;
using Tektosyne.IO;
using Tektosyne.Windows;
using Tektosyne.Xml;
using Hexkit.Global;

namespace Hexkit.Options {

    /// <summary>
    /// Manages user options for both Hexkit applications.</summary>
    /// <remarks><para>
    /// Only a single instance of the <b>ApplicationOptions</b> class can be created during the
    /// lifetime of the program. Use <see cref="ApplicationOptions.CreateInstance"/> to instantiate
    /// the class, and <see cref="ApplicationOptions.Instance"/> to retrieve the current instance.
    /// </para><para>
    /// <b>ApplicationOptions</b> is serialized to the XML element "options" defined in <see
    /// cref="FilePaths.OptionsSchema"/>. This is the root node of a Hexkit options description.
    /// </para></remarks>

    public sealed class ApplicationOptions: XmlSerializable {
        #region Private Fields

        // action for ShowInitMessage
        private Action _initMessage;

        #endregion
        #region ApplicationOptions()

        /// <summary>
        /// Initializes a new instance of the <see cref="ApplicationOptions"/> class.</summary>
        /// <remarks>
        /// Please refer to <see cref="CreateInstance"/> for details.</remarks>

        private ApplicationOptions() {
            Version = new Version(1, 0, 0, 0);

            // instantiate all members with default values
            View = new ViewOptions(OnOptionsChanged);
            if (ApplicationInfo.IsEditor)
                Editor = new EditorOptions(OnOptionsChanged);
            else
                Game = new GameOptions(OnOptionsChanged);
        }

        #endregion
        #region Editor

        /// <summary>
        /// Gets all user settings specific to Hexkit Editor.</summary>
        /// <value>
        /// The <see cref="GameOptions"/> object that manages all user settings specific to Hexkit
        /// Editor.</value>
        /// <remarks>
        /// <b>Editor</b> never returns a null reference if <see cref="ApplicationInfo.IsEditor"/>
        /// is <c>true</c>, and always returns a null reference otherwise. This property never
        /// changes once the object has been constructed.</remarks>

        public EditorOptions Editor { get; private set; }

        #endregion
        #region Game

        /// <summary>
        /// Gets all user settings specific to Hexkit Game.</summary>
        /// <value>
        /// The <see cref="GameOptions"/> object that manages all user settings specific to Hexkit
        /// Game.</value>
        /// <remarks>
        /// <b>Game</b> never returns a null reference if <see cref="ApplicationInfo.IsEditor"/> is
        /// <c>false</c>, and always returns a null reference otherwise. This property never changes
        /// once the object has been constructed.</remarks>

        public GameOptions Game { get; private set; }

        #endregion
        #region Instance

        /// <summary>
        /// Gets the current instance of the <see cref="ApplicationOptions"/> class.</summary>
        /// <value>
        /// The current instance of the <see cref="ApplicationOptions"/> class if one was
        /// successfully initialized; otherwise, a null reference. The default is a null reference.
        /// </value>
        /// <remarks>
        /// <b>Instance</b> is set by the <see cref="CreateInstance"/> method.</remarks>

        public static ApplicationOptions Instance { get; private set; }

        #endregion
        #region Version

        /// <summary>
        /// Gets the original application version of an existing XML options description file.
        /// </summary>
        /// <value>
        /// The <see cref="Version"/> of the Hexkit Game or Hexkit Editor application that
        /// originally wrote an existing XML options description file. The default is 1.0.0.0, 
        /// indicating that no existing options file was found.</value>
        /// <remarks>
        /// <b>Version</b> is set to 1.0.0.0 during construction. If an options file exists, <see
        /// cref="CreateInstance"/> will assign its application version to <b>Version</b> before any
        /// other <b>Hexkit.Options</b> classes begin reading the options file. <b>Version</b> thus
        /// allows those classes to adjust their default property values for backwards compatibility
        /// with older Hexkit releases.</remarks>

        public Version Version { get; private set; }

        #endregion
        #region View

        /// <summary>
        /// Gets all user settings related to visual appearance.</summary>
        /// <value>
        /// The <see cref="ViewOptions"/> object that manages all user settings related to visual
        /// appearance.</value>
        /// <remarks>
        /// <b>View</b> never returns a null reference. This property never changes once the object
        /// has been constructed.</remarks>

        public ViewOptions View { get; private set; }

        #endregion
        #region CreateInstance

        /// <summary>
        /// Initializes a new <see cref="Instance"/> of the <see cref="ApplicationOptions"/> class.
        /// </summary>
        /// <returns>
        /// The new value of the <see cref="Instance"/> property.</returns>
        /// <exception cref="PropertyValueException">
        /// <see cref="Instance"/> is not a null reference.</exception>
        /// <remarks><para>
        /// <b>CreateInstance</b> sets the <see cref="Instance"/> property to a newly created
        /// instance of the <see cref="ApplicationOptions"/> class. Only a single instance of the
        /// <b>ApplicationOptions</b> class can be created during the lifetime of the program, and
        /// it cannot be deleted.
        /// </para><para>
        /// The new <b>Instance</b> is initialized with the data of the XML options description file
        /// returned by <see cref="FilePaths.GetOptionsFile"/>. The actual file path varies
        /// depending on the current application and the current user. If the file does not exist,
        /// all properties remain at default values.
        /// </para><para>
        /// An existing options file is validated against <see cref="FilePaths.OptionsSchema"/>
        /// while parsing. Validation errors, as well as any other XML parsing errors, generate an
        /// error message and cause all properties for which no data has been read to remain at
        /// default values. However, <b>CreateInstance</b> will never throw an exception.
        /// </para><para>
        /// <b>CreateInstance</b> does not actually display its error messages. Clients must call
        /// <see cref="ShowInitMessage"/> at a later time, as documented there.</para></remarks>

        public static ApplicationOptions CreateInstance() {

            // check for existing instance
            if (Instance != null)
                ThrowHelper.ThrowPropertyValueException(
                    "Instance", Tektosyne.Strings.PropertyNotNull);

            // set singleton reference
            Instance = new ApplicationOptions();

            // quit immediately if no options file exists
            RootedPath path = FilePaths.GetOptionsFile();
            if (path.IsEmpty || !File.Exists(path.AbsolutePath))
                return Instance;

            try {
                // validate against Options schema
                var settings = XmlUtility.CreateReaderSettings(FilePaths.OptionsSchema);

                // read Options data from current user's file
                using (XmlReader reader = XmlReader.Create(path.AbsolutePath, settings))
                    Instance.ReadXml(reader);
            }
            catch (Exception e) {
                // wrap XML error information if present
                if (e is XmlException)
                    e = new DetailException(Global.Strings.XmlError, e);
                else if (e is XmlSchemaException)
                    e = new DetailException(Global.Strings.XmlErrorSchema, e);

                // show message that file exists but was not read
                string message = String.Format(ApplicationInfo.Culture,
                    Global.Strings.DialogOptionsLoadError, path.AbsolutePath);

                // prepare for later call to ShowInitMessage
                Instance._initMessage = () => 
                    MessageDialog.Show(null, message, Global.Strings.TitleOptionsError,
                        e, MessageBoxButton.OK, Images.Error);
            }

            return Instance;
        }

        #endregion
        #region OptionsChanged

        /// <summary>
        /// Occurs when options are changed.</summary>
        /// <remarks>
        /// <b>OptionsChanged</b> is raised when one or more options managed by this <see
        /// cref="ApplicationOptions"/> object are changed after the object has been constructed.
        /// </remarks>

        public event EventHandler OptionsChanged;

        #endregion
        #region OnOptionsChanged

        /// <summary>
        /// Raises the <see cref="OptionsChanged"/> event and saves all current <see
        /// cref="ApplicationOptions"/> data.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> sending the event.</param>
        /// <param name="args">
        /// An <see cref="EventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnOptionsChanged</b> first raises the <see cref="OptionsChanged"/> event if any
        /// listeners are attached, and then invokes <see cref="Save"/> to save all serializable
        /// data of this <see cref="ApplicationOptions"/> object.</remarks>

        internal void OnOptionsChanged(object sender, EventArgs args) {

            // broadcast event to all listeners
            var handler = OptionsChanged;
            if (handler != null) handler(sender, args);

            Save();
        }

        #endregion
        #region Save

        /// <summary>
        /// Saves all current <see cref="ApplicationOptions"/> data to an options file which will
        /// conform to <see cref="FilePaths.OptionsSchema"/>.</summary>
        /// <remarks><para>
        /// <b>Save</b> creates a monolithic XML document containing all serializable data of this
        /// <see cref="ApplicationOptions"/> object. The document is saved to the location returned
        /// by <see cref="FilePaths.GetOptionsFile"/>. Any existing file at this location will be
        /// overwritten.
        /// </para><para>
        /// If any errors occur, an error message is shown and the resulting options file may be
        /// incomplete or unreadable. However, <b>Save</b> will never propagate an exception to the
        /// caller.</para></remarks>

        public void Save() {

            // quit immediately if no path exists
            RootedPath path = FilePaths.GetOptionsFile();
            if (path.IsEmpty) return;

            try {
                // write Options data to current user's file
                XmlWriterSettings settings = XmlUtility.CreateWriterSettings();
                using (XmlWriter writer = XmlWriter.Create(path.AbsolutePath, settings))
                    WriteXml(writer);
            }
            catch (Exception e) {
                // wrap XML error information if present
                if (e is XmlException)
                    e = new DetailException(Global.Strings.XmlError, e);

                // show message that file exists but was not written
                string message = String.Format(ApplicationInfo.Culture,
                    Global.Strings.DialogOptionsSaveError, path.AbsolutePath);

                MessageDialog.Show(null, message, Global.Strings.TitleOptionsError,
                    e, MessageBoxButton.OK, Images.Error);
            }
        }

        #endregion
        #region ShowInitMessage

        /// <summary>
        /// Shows any message that occurred during the last call to <see cref="CreateInstance"/>.
        /// </summary>
        /// <remarks><para>
        /// <b>ShowInitMessage</b> shows any message, typically an error notification, that was
        /// generated by <see cref="CreateInstance"/>, and then deletes that message. Call this
        /// method during or after creation of the <see cref="Application.MainWindow"/> of the
        /// current WPF <see cref="Application"/>.
        /// </para><para>
        /// <see cref="CreateInstance"/> cannot show its messages directly because it is called in
        /// the <see cref="Application"/> constructor which will shut down the <b>Application</b> if
        /// any premature WPF display is attempted.</para></remarks>

        public void ShowInitMessage() {

            if (this._initMessage != null) {
                this._initMessage();
                this._initMessage = null;
            }
        }

        #endregion
        #region XmlSerializable Members
        #region ConstXmlName

        /// <summary>
        /// The name of the XML element associated with the <see cref="ApplicationOptions"/> class.
        /// </summary>
        /// <remarks>
        /// <b>ConstXmlName</b> holds the value "options", indicating the XML element in <see
        /// cref="FilePaths.OptionsSchema"/> whose data is managed by the <see
        /// cref="ApplicationOptions"/> class. This element is the root node of a Hexkit options
        /// description.</remarks>

        public const string ConstXmlName = "options";

        #endregion
        #region ReadXmlElements

        /// <summary>
        /// Reads XML element data into the <see cref="ApplicationOptions"/> object using the
        /// specified <see cref="XmlReader"/>.</summary>
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

                case "application":
                    // read application version of existing file
                    string version = reader["version"];
                    if (version != null) Version = new Version(version);
                    return true;

                case EditorOptions.ConstXmlName:
                    // read Hexkit Editor options if required
                    if (Editor != null) Editor.ReadXml(reader);
                    return true;

                case GameOptions.ConstXmlName:
                    // read Hexkit Game options if required
                    if (Game != null) Game.ReadXml(reader);
                    return true;

                case ViewOptions.ConstXmlName:
                    // read display options
                    View.ReadXml(reader);
                    return true;

                default: return false;
            }
        }

        #endregion
        #region WriteXml

        /// <summary>
        /// Writes the current data of the <see cref="ApplicationOptions"/> object to the specified
        /// <see cref="XmlWriter"/>.</summary>
        /// <param name="writer">
        /// The <see cref="XmlWriter"/> to which to write.</param>
        /// <remarks>
        /// The XML data written to the specified <paramref name="writer"/> comprises an XML
        /// document declaration; a root element named <see cref="XmlName"/> with a default XML
        /// namespace of <see cref="FilePaths.OptionsNamespace"/>; and all data written out by <see
        /// cref="WriteXmlElements"/>.</remarks>

        internal override void WriteXml(XmlWriter writer) {

            // write XML declaration and options namespace
            writer.WriteStartDocument(true);
            writer.WriteStartElement(XmlName, FilePaths.OptionsNamespace);

            WriteXmlAttributes(writer);
            WriteXmlElements(writer);

            // close root node and document
            writer.WriteEndElement();
            writer.WriteEndDocument();
        }

        #endregion
        #region WriteXmlElements

        /// <summary>
        /// Writes all current data of the <see cref="ApplicationOptions"/> object that is
        /// serialized to nested XML elements to the specified <see cref="XmlWriter"/>.</summary>
        /// <param name="writer">
        /// The <see cref="XmlWriter"/> to which to write.</param>
        /// <remarks>
        /// Please refer to class <see cref="XmlSerializable"/> for a complete description of this
        /// method.</remarks>

        protected override void WriteXmlElements(XmlWriter writer) {

            // write application info element
            ApplicationInfo.WriteXml(writer);

            // write Hexkit Game options if present
            if (Game != null) Game.WriteXml(writer);

            // write Hexkit Editor options if present
            if (Editor != null) Editor.WriteXml(writer);

            // write view options
            View.WriteXml(writer);
        }

        #endregion
        #endregion
    }
}
