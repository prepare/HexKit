using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Xml;
using System.Xml.Schema;

using Tektosyne;
using Tektosyne.IO;
using Tektosyne.Xml;
using Hexkit.Global;

namespace Hexkit.Scenario {

    /// <summary>
    /// Represents an entire Hexkit scenario.</summary>
    /// <remarks><para>
    /// <b>MasterSection</b> directly implements the Master section of a Hexkit scenario, and
    /// aggregates the subsections as properties.
    /// </para><para>
    /// Only a single instance of the <b>MasterSection</b> class can be created at a time. Use <see
    /// cref="MasterSection.CreateInstance"/> to instantiate the class, <see
    /// cref="MasterSection.Instance"/> to retrieve the current instance, and <see
    /// cref="MasterSection.Dispose"/> to delete the instance.
    /// </para><para>
    /// <b>MasterSection</b> is serialized to the XML element "scenario" defined in <see
    /// cref="FilePaths.ScenarioSchema"/>. This is the root node of a Hexkit scenario description.
    /// </para></remarks>

    public sealed class MasterSection: ScenarioElement, IDisposable {
        #region MasterSection()

        /// <summary>
        /// Initializes a new instance of the <see cref="MasterSection"/> class.</summary>
        /// <remarks>
        /// The <see cref="MasterSection"/> constructor invokes <see cref="Clear"/> to reset all
        /// properties to default values. For reference types, these are not null references but
        /// rather default-constructed objects of the appropriate type.</remarks>

        private MasterSection() { Clear(); }

        #endregion
        #region Private Fields

        // property backers
        private string _hash, _title;
        private readonly SectionPaths _sectionPaths = new SectionPaths();

        private AreaSection _areas;
        private EntitySection _entities;
        private FactionSection _factions;
        private ImageSection _images;
        private VariableSection _variables;

        #endregion
        #region Public Properties
        #region Areas

        /// <summary>
        /// Gets or sets the <see cref="AreaSection"/> of the scenario.</summary>
        /// <value>
        /// The <see cref="AreaSection"/> of the current scenario.</value>
        /// <exception cref="InvalidOperationException">
        /// The property is set, and <see cref="ApplicationInfo.IsEditor"/> is <c>false</c>.
        /// </exception>
        /// <remarks>
        /// <b>Areas</b> never returns a null reference. The implementation of the Hexkit map editor
        /// requires that <b>Areas</b> can be set from without the <see cref="MasterSection"/>
        /// class, unlike the other subsection properties.</remarks>

        public AreaSection Areas {
            [DebuggerStepThrough]
            get { return this._areas; }
            [DebuggerStepThrough]
            set {
                ApplicationInfo.CheckEditor();
                this._areas = value;
            }
        }

        #endregion
        #region Entities

        /// <summary>
        /// Gets the <see cref="EntitySection"/> of the scenario.</summary>
        /// <value>
        /// The <see cref="EntitySection"/> of the current scenario.</value>
        /// <remarks>
        /// <b>Entities</b> never returns a null reference.</remarks>

        public EntitySection Entities {
            [DebuggerStepThrough]
            get { return this._entities; }
        }

        #endregion
        #region Factions

        /// <summary>
        /// Gets the <see cref="FactionSection"/> of the scenario.</summary>
        /// <value>
        /// The <see cref="FactionSection"/> of the current scenario.</value>
        /// <remarks>
        /// <b>Factions</b> never returns a null reference.</remarks>

        public FactionSection Factions {
            [DebuggerStepThrough]
            get { return this._factions; }
        }

        #endregion
        #region Hash

        /// <summary>
        /// Gets a <see cref="String"/> representation of the hash code for the scenario.</summary>
        /// <value>
        /// A <see cref="String"/> representing the hash code for the temporary scenario <see
        /// cref="ScenarioFileType.Start"/> file from which the <see cref="MasterSection"/> was
        /// created. The default is an empty string.</value>
        /// <remarks>
        /// <b>Hash</b> never returns a null reference.</remarks>

        public string Hash {
            [DebuggerStepThrough]
            get { return this._hash ?? ""; }
        }

        #endregion
        #region Images

        /// <summary>
        /// Gets the <see cref="ImageSection"/> of the scenario.</summary>
        /// <value>
        /// The <see cref="ImageSection"/> of the current scenario.</value>
        /// <remarks>
        /// <b>Images</b> never returns a null reference.</remarks>

        public ImageSection Images {
            [DebuggerStepThrough]
            get { return this._images; }
        }

        #endregion
        #region Information

        /// <summary>
        /// Gets the <see cref="Scenario.Information"/> block for the scenario.</summary>
        /// <value>
        /// The <see cref="Scenario.Information"/> block for the current scenario.</value>
        /// <remarks>
        /// <b>Information</b> never returns a null reference.</remarks>

        public Information Information { get; private set; }

        #endregion
        #region Instance

        /// <summary>
        /// Gets the current instance of the <see cref="MasterSection"/> class.</summary>
        /// <value>
        /// The current instance of the <see cref="MasterSection"/> class if one was successfully
        /// initialized and has not yet been disposed of; otherwise, a null reference. The default
        /// is a null reference.</value>
        /// <remarks>
        /// <b>Instance</b> is set by the <see cref="CreateInstance"/> method and cleared by the
        /// <see cref="Dispose"/> method.</remarks>

        public static MasterSection Instance { get; private set; }

        #endregion
        #region Path

        /// <summary>
        /// Gets the path to the original scenario file.</summary>
        /// <value>
        /// A <see cref="RootedPath"/> wrapping the absolute path to the scenario file (not the
        /// temporary <see cref="ScenarioFileType.Start"/> file) from which the <see
        /// cref="MasterSection"/> was created. The default is an empty path.</value>
        /// <remarks>
        /// <b>Path</b> never returns a null reference. This property is a shortcut to the <see
        /// cref="Scenario.SectionPaths.Master"/> component of the <see cref="SectionPaths"/>
        /// property.</remarks>

        public RootedPath Path {
            [DebuggerStepThrough]
            get { return this._sectionPaths.Master; }
        }

        #endregion
        #region Rules

        /// <summary>
        /// Gets the <see cref="RuleScript"/> for the scenario.</summary>
        /// <value>
        /// The <see cref="RuleScript"/> for the current scenario.</value>
        /// <remarks>
        /// <b>Rules</b> never returns a null reference, but the code for the actual rule script
        /// will not be available until <see cref="RuleScript.Load"/> has succeeded.</remarks>

        public RuleScript Rules { get; private set; }

        #endregion
        #region SectionPaths

        /// <summary>
        /// Gets the file paths for all scenario sections.</summary>
        /// <value>
        /// A <see cref="Scenario.SectionPaths"/> object containing the file paths to all sections
        /// of the current scenario.</value>
        /// <remarks>
        /// <b>SectionPaths</b> never returns a null reference.</remarks>

        public SectionPaths SectionPaths {
            [DebuggerStepThrough]
            get { return this._sectionPaths; }
        }

        #endregion
        #region Title

        /// <summary>
        /// Gets or sets the title of the scenario.</summary>
        /// <value>
        /// The title of the current scenario. The default is an empty string.</value>
        /// <exception cref="InvalidOperationException">
        /// The property is set, and <see cref="ApplicationInfo.IsEditor"/> is <c>false</c>.
        /// </exception>
        /// <remarks>
        /// <b>Title</b> returns an empty string when set to a null reference. This property holds
        /// the value of the "title" XML element.</remarks>

        public string Title {
            [DebuggerStepThrough]
            get { return this._title ?? ""; }
            [DebuggerStepThrough]
            set {
                ApplicationInfo.CheckEditor();
                this._title = value;
            }
        }

        #endregion
        #region Variables

        /// <summary>
        /// Gets the <see cref="VariableSection"/> of the scenario.</summary>
        /// <value>
        /// The <see cref="VariableSection"/> of the current scenario.</value>
        /// <remarks>
        /// <b>Variables</b> never returns a null reference.</remarks>

        public VariableSection Variables {
            [DebuggerStepThrough]
            get { return this._variables; }
        }

        #endregion
        #endregion
        #region Public Methods
        #region Clear

        /// <summary>
        /// Initializes all scenario sections to default values.</summary>
        /// <remarks>
        /// <b>Clear</b> initializes all <see cref="MasterSection"/> data, including all subsection
        /// data, by invoking the appropriate default constructors or assigning empty values. No
        /// property will return a null reference after <b>Clear</b> has returned.</remarks>

        public void Clear() {

            // clear MasterSection data
            SectionPaths.SetPathUnchecked(ScenarioSection.Master, "");
            this._title = "";
            Information = new Information();
            Rules = new RuleScript();

            // clear all subsection data
            foreach (ScenarioSection section in SectionUtility.Subsections)
                ClearSection(section);
        }

        #endregion
        #region ClearSection

        /// <summary>
        /// Initializes all data of the specified <see cref="ScenarioSection"/> to default values.
        /// </summary>
        /// <param name="section">
        /// A <see cref="ScenarioSection"/> value indicating the subsection to clear.</param>
        /// <exception cref="InvalidEnumArgumentException">
        /// <paramref name="section"/> is not a valid <see cref="ScenarioSection"/> value.
        /// </exception>
        /// <remarks><para>
        /// <b>ClearSection</b> invokes the default constructor for the <see cref="MasterSection"/> 
        /// property that corresponds to the specified <paramref name="section"/> value, and also
        /// clears its file path stored in the corresponding <see cref="SectionPaths"/> component.
        /// </para><para>
        /// If <paramref name="section"/> is <b>ScenarioSection.Images</b>, <b>ClearSection</b>
        /// invokes <see cref="ImageSection.Dispose"/> on the current <see cref="ImageSection"/>
        /// object before it is recreated.
        /// </para><para>
        /// If <paramref name="section"/> is <b>ScenarioSection.Master</b>, <b>ClearSection</b>
        /// invokes <see cref="Clear"/> to initialize all data of <em>all</em> subsections to
        /// default values.</para></remarks>

        public void ClearSection(ScenarioSection section) {

            // clear section file path
            SectionPaths.SetPathUnchecked(section, "");

            switch (section) {

                case ScenarioSection.Areas:
                    this._areas = new AreaSection();
                    break;

                case ScenarioSection.Entities:
                    this._entities = new EntitySection();
                    break;

                case ScenarioSection.Factions:
                    this._factions = new FactionSection();
                    break;

                case ScenarioSection.Images:
                    // release image file bitmaps
                    if (this._images != null) this._images.Dispose();
                    this._images = new ImageSection();
                    break;

                case ScenarioSection.Master:
                    Clear(); // clear all scenario data
                    break;

                case ScenarioSection.Variables:
                    this._variables = new VariableSection();
                    break;

                default:
                    ThrowHelper.ThrowInvalidEnumArgumentException(
                        "section", (int) section, typeof(ScenarioSection));
                    break;
            }
        }

        #endregion
        #region CreateInstance

        /// <summary>
        /// Initializes a new <see cref="Instance"/> of the <see cref="MasterSection"/> class.
        /// </summary>
        /// <exception cref="PropertyValueException">
        /// <see cref="Instance"/> is not a null reference.</exception>
        /// <remarks><para>
        /// <b>CreateInstance</b> sets the <see cref="Instance"/> property to a newly created
        /// instance of the <see cref="MasterSection"/> class that has been initialized to default
        /// values using <see cref="Clear"/>.
        /// </para><para>
        /// Only a single instance of the <b>MasterSection</b> class can be created at a time.
        /// Calling the <see cref="Dispose"/> method on the current <b>Instance</b> clears this
        /// property and allows the creation of another <b>MasterSection</b> instance.
        /// </para></remarks>

        public static void CreateInstance() {

            // check for existing instance
            if (Instance != null)
                ThrowHelper.ThrowPropertyValueException(
                    "Instance", Tektosyne.Strings.PropertyNotNull);

            // set singleton reference
            MasterSection.Instance = new MasterSection();
        }

        #endregion
        #region GetScenarioElement

        /// <summary>
        /// Returns the <see cref="ScenarioElement"/> that represents the specified <see
        /// cref="ScenarioSection"/>.</summary>
        /// <param name="section">
        /// The <see cref="ScenarioSection"/> value whose associated <see cref="ScenarioElement"/>
        /// to return.</param>
        /// <returns>
        /// The <see cref="ScenarioElement"/> object representing the specified <paramref
        /// name="section"/> contained in the <see cref="MasterSection"/>.</returns>
        /// <exception cref="InvalidEnumArgumentException">
        /// <paramref name="section"/> is not a valid <see cref="ScenarioSection"/> value.
        /// </exception>
        /// <remarks>
        /// <b>GetScenarioElement</b> never returns a null reference.</remarks>

        public ScenarioElement GetScenarioElement(ScenarioSection section) {
            switch (section) {

                case ScenarioSection.Areas:     return Areas;
                case ScenarioSection.Entities:  return Entities;
                case ScenarioSection.Factions:  return Factions;
                case ScenarioSection.Images:    return Images;
                case ScenarioSection.Master:    return this;
                case ScenarioSection.Variables: return Variables;

                default:
                    ThrowHelper.ThrowInvalidEnumArgumentException(
                        "section", (int) section, typeof(ScenarioSection));
                    return null;
            }
        }

        #endregion
        #region Load

        /// <summary>
        /// Loads all scenario sections from the specified XML file which is validated against <see
        /// cref="FilePaths.ScenarioSchema"/>.</summary>
        /// <param name="file">
        /// The XML scenario description file from which to read <see cref="MasterSection"/> data.
        /// </param>
        /// <exception cref="ArgumentNullOrEmptyException">
        /// <paramref name="file"/> is a null reference or an empty string.</exception>
        /// <exception cref="DetailException">
        /// An <see cref="XmlException"/> or <see cref="XmlSchemaException"/> occurred while loading
        /// <paramref name="file"/>. All such exceptions will be converted to a
        /// <b>DetailException</b> whose <see cref="DetailException.Detail"/> property contains
        /// technical details provided by the XML parser.</exception>
        /// <remarks><para>
        /// <b>Load</b> invokes <see cref="Clear"/> before attempting to load the specified
        /// <paramref name="file"/>. On success, <see cref="ScenarioElement.Validate"/> is called
        /// for all subsections of the <see cref="MasterSection"/>. On failure, <b>Clear</b> is
        /// called again, returning the <b>MasterSection</b> to an empty state.
        /// </para><para>
        /// The specified <paramref name="file"/> is transformed into a monolithic XML document,
        /// known as the <see cref="ScenarioFileType.Start"/> file, before parsing is attempted.
        /// This document is validated against <see cref="FilePaths.ScenarioSchema"/> during
        /// parsing. Validation errors, as well as any other XML parsing errors, generate an
        /// exception.
        /// </para><para>
        /// The <b>Start</b> file is retained in case of error to facilitate debugging by the
        /// programmer or scenario designer, but deleted in case of success.
        /// </para><para>
        /// The <see cref="Path"/> property is set directly to the specified <paramref
        /// name="file"/>. However, the <see cref="Hash"/> property is computed based on the
        /// contents of the <b>Start</b> file to ensure that the entire scenario description file
        /// participates in the hash code calculation.</para></remarks>

        public void Load(string file) {
            if (String.IsNullOrEmpty(file))
                ThrowHelper.ThrowArgumentNullOrEmptyException("file");

            // scenario path must differ from temporary path
            string startFile = FilePaths.GetScenarioFile(ScenarioFileType.Start).AbsolutePath;
            if (PathEx.Equals(file, startFile))
                ThrowHelper.ThrowArgumentException("file", Global.Strings.ErrorScenarioTemporary);

            try {
                // combine scenario sections into monolithic file
                SectionUtility.CombineSections(file, startFile);

                // compute MD5 hash code for temporary file
                using (Stream stream = new FileStream(startFile, FileMode.Open)) {
                    using (MD5 md5 = new MD5CryptoServiceProvider()) {
                        byte[] md5hash = md5.ComputeHash(stream);
                        this._hash = BitConverter.ToString(md5hash);
                    }
                }

                // validate against Scenario schema
                XmlReaderSettings settings =
                    XmlUtility.CreateReaderSettings(FilePaths.ScenarioSchema);

                // read MasterSection data from temporary file
                using (XmlReader reader = XmlReader.Create(startFile, settings))
                    ReadXmlSection(reader, ScenarioSection.Master, file);
            }
            catch (XmlSchemaException e) {
                ThrowHelper.ThrowDetailException(Global.Strings.XmlErrorSchema, e);
            }
            catch (XmlException e) {
                ThrowHelper.ThrowDetailException(Global.Strings.XmlError, e);
            }

            // delete temporary file on success only
            File.Delete(startFile);
        }

        #endregion
        #region LoadSection

        /// <summary>
        /// Loads the specified <see cref="ScenarioSection"/> from the XML file stored in <see
        /// cref="SectionPaths"/> without XML schema validation.</summary>
        /// <param name="section">
        /// A <see cref="ScenarioSection"/> value indicating the subsection to load.</param>
        /// <exception cref="DetailException">
        /// An <see cref="XmlException"/> occurred while loading the XML file. All such exceptions
        /// will be converted to a <b>DetailException</b> whose <see cref="DetailException.Detail"/>
        /// property contains technical details provided by the XML parser.</exception>
        /// <exception cref="InvalidEnumArgumentException">
        /// <paramref name="section"/> is not a valid <see cref="ScenarioSection"/> value.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// <see cref="ApplicationInfo.IsEditor"/> is <c>false</c>.</exception>
        /// <remarks><para>
        /// <b>LoadSection</b> invokes <see cref="ClearSection"/> with the specified <paramref
        /// name="section"/> before attempting to read its data from the XML file indicated by the
        /// corresponding <see cref="SectionPaths"/> component.
        /// </para><para>
        /// On success, <see cref="ScenarioElement.Validate"/> is called for the specified <paramref
        /// name="section"/>. On failure, <b>Clear</b> is called again, returning the subsection to
        /// an empty state.
        /// </para><para>
        /// If <paramref name="section"/> is <b>ScenarioSection.Master</b>, subsection data will be
        /// read only if embedded in the <see cref="MasterSection"/> file. Otherwise, the
        /// corresponding <b>SectionPaths</b> component will be set to the "href" attribute of the
        /// "include" tag for that subsection, but the subsection data in memory will remain
        /// unchanged. The client must call <b>LoadSection</b> again to read the actual subsection
        /// data from the file referenced by <b>SectionPaths</b>.
        /// </para><para>
        /// If the <b>SectionPaths</b> component for the specified <paramref name="section"/> is
        /// empty, <b>LoadSection</b> merely checks that <see cref="ApplicationInfo.IsEditor"/>
        /// is <c>true</c> and leaves the subsection data in memory unchanged.</para></remarks>

        public void LoadSection(ScenarioSection section) {
            ApplicationInfo.CheckEditor();

            // retrieve stored path for indicated section
            RootedPath path = SectionPaths.GetPath(section);

            // silently return if path undefined
            if (path.IsEmpty) return;

            try {
                // read section data from section file
                XmlReaderSettings settings = XmlUtility.CreateReaderSettings();
                using (XmlReader reader = XmlReader.Create(path.AbsolutePath, settings))
                    ReadXmlSection(reader, section, path.RelativePath);
            }
            catch (XmlException e) {
                ThrowHelper.ThrowDetailException(Global.Strings.XmlError, e);
            }
        }

        #endregion
        #region ProcessIdentifierBySection

        /// <summary>
        /// Counts, changes, or deletes all occurrences of the specified identifier in all scenario
        /// sections.</summary>
        /// <param name="oldId">
        /// The identifier to count, change, or delete.</param>
        /// <param name="newId"><para>
        /// The same value as <paramref name="oldId"/> to count the occurrences of <paramref
        /// name="oldId"/>.
        /// </para><para>-or-</para><para>
        /// A different value than <paramref name="oldId"/> to change all occurrences of <paramref
        /// name="oldId"/> to <paramref name="newId"/>.
        /// </para><para>-or-</para><para>
        /// A null reference to delete all elements with <paramref name="oldId"/>.</para></param>
        /// <returns>
        /// An <see cref="Array"/> of <see cref="Int32"/> values indicating the number of
        /// occurrences of <paramref name="oldId"/> in each scenario subsection.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="oldId"/> is a null reference.</exception>
        /// <exception cref="InvalidOperationException">
        /// <paramref name="newId"/> is not a null reference, and <see
        /// cref="ApplicationInfo.IsEditor"/> is <c>false</c>.</exception>
        /// <remarks><para>
        /// <b>ProcessIdentifierBySection</b> invokes the <see
        /// cref="ScenarioElement.ProcessIdentifier"/> method on each subsection to process all
        /// occurrences of the specified <paramref name="oldId"/>.
        /// </para><para>
        /// The count for each subsection is placed in that element of the returned <see
        /// cref="Array"/> whose index equals the corresponding <see cref="ScenarioSection"/> value
        /// cast to <see cref="Int32"/>.
        /// </para><para>
        /// The total sum of all occurrences is placed in the first array element, corresponding to
        /// <b>ScenarioSection.Master</b>, as the <see cref="MasterSection"/> class does not define
        /// any identifiers to be processed.</para></remarks>

        public int[] ProcessIdentifierBySection(string oldId, string newId) {
            if (oldId == null)
                ThrowHelper.ThrowArgumentNullException("oldId");

            // change/delete requires editing mode
            if (newId != oldId) ApplicationInfo.CheckEditor();

            // array to hold all subsection counts
            int[] count = new int[SectionUtility.AllSections.Length];

            // process IDs in all subsections
            foreach (ScenarioSection section in SectionUtility.Subsections)
                count[(int) section] =
                    GetScenarioElement(section).ProcessIdentifier(oldId, newId);

            // store total count in Master element
            count[(int) ScenarioSection.Master] = Fortran.Sum(count);

            return count;
        }

        #endregion
        #region SaveAll

        /// <summary>
        /// Saves all scenario sections to the specified monolithic XML file which will conform to
        /// <see cref="FilePaths.ScenarioSchema"/>.</summary>
        /// <param name="file">
        /// The monolithic XML scenario description file to which to save all <see
        /// cref="MasterSection"/> data.</param>
        /// <exception cref="ArgumentNullOrEmptyException">
        /// <paramref name="file"/> is a null reference or an empty string.</exception>
        /// <remarks><para>
        /// <b>SaveAll</b> creates a single monolithic XML document, containing all subsections as
        /// embedded XML elements, regardless of the current subsection paths stored in <see
        /// cref="SectionPaths"/>. Any existing contents of the specified <paramref name="file"/>
        /// will be overwritten. The <see cref="Path"/> property and all <b>SectionPaths</b>
        /// components remain unchanged.
        /// </para><para>
        /// Internally, <b>SaveAll</b> clears all subsection file paths before saving all <see
        /// cref="MasterSection"/> data to the specified <paramref name="file"/>, and restores the
        /// original subsection file paths before returning.
        /// </para><para>
        /// <see cref="ApplicationUtility.OnUnhandledException"/> may call <b>SaveAll</b> to write
        /// the current scenario contents to an XML file.</para></remarks>

        public void SaveAll(string file) {
            if (String.IsNullOrEmpty(file))
                ThrowHelper.ThrowArgumentNullOrEmptyException("file");

            string[] paths = new string[SectionUtility.AllSections.Length];

            // backup & clear subsection file paths
            foreach (ScenarioSection section in SectionUtility.Subsections) {
                paths[(int) section] = SectionPaths.GetPath(section).RelativePath;
                SectionPaths.SetPathUnchecked(section, "");
            }

            try {
                // save scenario object tree
                Save(file);
            }
            finally {
                // restore subsection file paths
                foreach (ScenarioSection section in SectionUtility.Subsections)
                    SectionPaths.SetPathUnchecked(section, paths[(int) section]);
            }
        }

        #endregion
        #region SaveSection

        /// <summary>
        /// Saves the specified <see cref="ScenarioSection"/> to the corresponding XML file stored
        /// in <see cref="SectionPaths"/>.</summary>
        /// <param name="section">
        /// A <see cref="ScenarioSection"/> value indicating the subsection to save.</param>
        /// <exception cref="InvalidEnumArgumentException">
        /// <paramref name="section"/> is not a valid <see cref="ScenarioSection"/> value.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// <see cref="ApplicationInfo.IsEditor"/> is <c>false</c>.</exception>
        /// <remarks><para>
        /// <b>SaveSection</b> invokes <see cref="XmlSerializable.WriteXml"/> on the <see
        /// cref="ScenarioElement"/> representing the specified <paramref name="section"/>, with the
        /// file path indicated by the corresponding <see cref="SectionPaths"/> component.
        /// </para><para>
        /// If the <b>SectionPaths</b> component for the specified <paramref name="section"/> is
        /// empty, <b>SaveSection</b> merely checks that <see cref="ApplicationInfo.IsEditor"/>
        /// is <c>true</c> and does not write any file data.</para></remarks>

        public void SaveSection(ScenarioSection section) {
            ApplicationInfo.CheckEditor();

            // retrieve stored path for indicated section
            RootedPath path = SectionPaths.GetPath(section);

            // write section data if path defined
            if (!path.IsEmpty)
                GetScenarioElement(section).Save(path.AbsolutePath);
        }

        #endregion
        #region ValidateSection

        /// <summary>
        /// Validates the current data of the specified <see cref="ScenarioSection"/>.</summary>
        /// <param name="section">
        /// A <see cref="ScenarioSection"/> value indicating the subsection to validate.</param>
        /// <exception cref="InvalidEnumArgumentException">
        /// <paramref name="section"/> is not a valid <see cref="ScenarioSection"/> value.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// <see cref="ApplicationInfo.IsEditor"/> is <c>false</c>.</exception>
        /// <remarks>
        /// <b>ValidateSection</b> invokes <see cref="ScenarioElement.Validate"/> on the <see
        /// cref="ScenarioElement"/> representing the specified <paramref name="section"/>, and
        /// likewise for all subsections whose validity depends on that <paramref name="section"/>
        /// because they might contain direct references to its objects.</remarks>

        public void ValidateSection(ScenarioSection section) {
            ApplicationInfo.CheckEditor();

            // validate specified section
            GetScenarioElement(section).Validate();

            // validate any dependent section(s)
            switch (section) {

                case ScenarioSection.Entities:
                    Factions.Validate();
                    goto case ScenarioSection.Factions;

                case ScenarioSection.Factions:
                    Areas.Validate();
                    break;

                case ScenarioSection.Images:
                    Entities.Validate();
                    break;

                case ScenarioSection.Variables:
                    Entities.Validate();
                    Factions.Validate();
                    break;
            }
        }

        #endregion
        #endregion
        #region IDisposable Members

        /// <summary>
        /// Releases all resources used by the <see cref="MasterSection"/> object.</summary>
        /// <remarks><para>
        /// <b>Dispose</b> invokes <see cref="ImageSection.Dispose"/> on the current <see
        /// cref="ImageSection"/> to release all image file bitmaps. However, clients should
        /// explicitly call <see cref="ImageSection.Unload"/> as soon as possible to reduce memory
        /// consumption.
        /// </para><para>
        /// <b>Dispose</b> also calls <see cref="RuleScript.Dispose"/> on the current <see
        /// cref="Rules"/> object and resets the <see cref="Instance"/> property to a null
        /// reference, allowing the creation of another instance of the <see cref="MasterSection"/>
        /// class.</para></remarks>

        public void Dispose() {

            // guaranteed by CreateInstance
            Debug.Assert(Instance == this);

            // clear singleton reference
            MasterSection.Instance = null;

            // RuleScript may need disposing
            if (Rules != null) {
                Rules.Dispose();
                Rules = null;
            }

            // release image file bitmaps
            if (this._images != null) {
                this._images.Dispose();
                this._images = null;
            }
        }

        #endregion
        #region XmlSerializable Members
        #region ConstXmlName

        /// <summary>
        /// The name of the XML element associated with the <see cref="MasterSection"/> class.
        /// </summary>
        /// <remarks>
        /// <b>ConstXmlName</b> holds the value "scenario", indicating the XML element in <see
        /// cref="FilePaths.ScenarioSchema"/> whose data is managed by the <see
        /// cref="MasterSection"/> class. This element is the root node of a Hexkit scenario
        /// description.</remarks>

        public const string ConstXmlName = "scenario";

        #endregion
        #region ReadXmlElements

        /// <summary>
        /// Reads XML element data into the <see cref="MasterSection"/> object using the specified
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
        /// <remarks><para>
        /// Clients should invoke <see cref="Clear"/> before calling <b>ReadXmlElements</b>, and
        /// <see cref="ScenarioElement.Validate"/> after the call returns.
        /// </para><para>
        /// The XML data provided by the specified <paramref name="reader"/> may include Master
        /// section data and all subsections. The latter may be represented by a single element
        /// named "include", containing an attribute named "element" indicating the <see
        /// cref="XmlSerializable.XmlName"/> of the subsection, and an attribute named "href"
        /// indicating its file path. In this case, the file path is recorded in <see
        /// cref="SectionPaths"/> but the data of the corresponding subsection is not changed.
        /// </para><para>
        /// Alternatively, subsections may be represented by an element whose name matches their
        /// <b>XmlName</b>. In this case, new subsection data is read from <paramref name="reader"/>
        /// but the corresponding <see cref="SectionPaths"/> entry is not changed.
        /// </para><para>
        /// <b>ReadXmlElements</b> invokes <see cref="ClearSection"/> for any subsection before
        /// attempting to read it. On success, <see cref="ScenarioElement.Validate"/> is called for
        /// the subsection. On failure, <b>ClearSection</b> is called again, returning the
        /// subsection to an empty state.</para></remarks>

        protected override bool ReadXmlElements(XmlReader reader) {
            switch (reader.Name) {

                case "title": {
                    string element = reader.ReadString();
                    this._title = element.PackSpace();
                    return true;
                }

                case Information.ConstXmlName:
                    Information = new Information();
                    Information.ReadXml(reader);
                    return true;

                case RuleScript.ConstXmlName:
                    Rules = new RuleScript();
                    Rules.ReadXml(reader);
                    return true;

                case "include": {
                    // retrieve section and file path
                    string element = reader["element"];
                    string path = reader["href"];

                    if (element != null && path != null) {
                        // set new file path for this section
                        ScenarioSection section = SectionUtility.GetSection(element);
                        SectionPaths.SetPathUnchecked(section, path);
                    }
                    return true;
                }

                case AreaSection.ConstXmlName:
                case EntitySection.ConstXmlName:
                case FactionSection.ConstXmlName:
                case ImageSection.ConstXmlName:
                case VariableSection.ConstXmlName: {

                    // parse inline section without file path
                    ScenarioSection section = SectionUtility.GetSection(reader.Name);
                    ReadXmlSection(reader, section, null);
                    return true;
                }

                default: return false;
            }
        }

        #endregion
        #region ReadXmlSection

        /// <summary>
        /// Reads XML data into the specified <see cref="ScenarioSection"/> using the specified <see
        /// cref="XmlReader"/>.</summary>
        /// <param name="reader">
        /// The <see cref="XmlReader"/> from which to read.</param>
        /// <param name="section">
        /// A <see cref="ScenarioSection"/> value indicating the subsection to read.</param>
        /// <param name="path">
        /// The section file path to be stored in <see cref="SectionPaths"/>, or a null reference to
        /// leave <b>SectionPaths</b> unchanged.</param>
        /// <remarks><para>
        /// <b>ReadXmlSection</b> implements reading scenario sections from an XML data stream for
        /// the methods <see cref="Load"/>, <see cref="LoadSection"/>, and <see
        /// cref="ReadXmlElements"/>.
        /// </para><para>
        /// <b>ReadXmlSection</b> invokes <see cref="ClearSection"/> for the specified <paramref
        /// name="section"/>, followed by <see cref="XmlSerializable.ReadXml"/> to read new section
        /// data from the specified <paramref name="reader"/>.
        /// </para><para>
        /// On success, <see cref="ScenarioElement.Validate"/> is called for the specified <paramref
        /// name="section"/>. On failure, <see cref="ClearSection"/> is called again, returning the
        /// subsection to an empty state.
        /// </para><para>
        /// After successful reading and validation, the <see cref="SectionPaths"/> component
        /// corresponding to <paramref name="section"/> is set to the specified <paramref
        /// name="path"/> if it is not a null reference.</para></remarks>

        private void ReadXmlSection(XmlReader reader, ScenarioSection section, string path) {

            // clear target section contents
            // (including section file path)
            ClearSection(section);

            try {
                // read XML data and validate references
                GetScenarioElement(section).ReadXml(reader);
                GetScenarioElement(section).Validate();

                // set (or restore) section file path
                if (path != null)
                    SectionPaths.SetPathUnchecked(section, path);
            }
            catch {
                // clear partly read data (including section file path) and rethrow
                ClearSection(section);
                throw;
            }
        }

        #endregion
        #region WriteXml

        /// <summary>
        /// Writes the current data of the <see cref="MasterSection"/> object to the specified <see
        /// cref="XmlWriter"/>.</summary>
        /// <param name="writer">
        /// The <see cref="XmlWriter"/> to which to write.</param>
        /// <remarks>
        /// The XML data written to the specified <paramref name="writer"/> comprises an XML
        /// document declaration; a root element named <see cref="XmlName"/> with a default XML
        /// namespace of <see cref="FilePaths.ScenarioNamespace"/>; and all data written out by <see
        /// cref="WriteXmlElements"/>.</remarks>

        internal override void WriteXml(XmlWriter writer) {

            // write XML declaration and scenario namespace
            writer.WriteStartDocument(true);
            writer.WriteStartElement(XmlName, FilePaths.ScenarioNamespace);

            WriteXmlAttributes(writer);
            WriteXmlElements(writer);

            // close root node and document
            writer.WriteEndElement();
            writer.WriteEndDocument();
        }

        #endregion
        #region WriteXmlElements

        /// <summary>
        /// Writes all current data of the <see cref="MasterSection"/> object that is serialized to
        /// nested XML elements to the specified <see cref="XmlWriter"/>.</summary>
        /// <param name="writer">
        /// The <see cref="XmlWriter"/> to which to write.</param>
        /// <remarks><para>
        /// The XML data written to the specified <paramref name="writer"/> comprises all data
        /// associated with the Master section, as well as all subsections defined by <see
        /// cref="FilePaths.ScenarioSchema"/>.
        /// </para><para>
        /// If the <see cref="SectionPaths"/> property defines a file path for a given subsection,
        /// the section is represented only by a single element named "include", containing an
        /// attribute named "element" indicating the <c>XmlName</c> of the section, and an attribute
        /// named "href" indicating the file path. Otherwise, the entire section is written to
        /// <paramref name="writer"/>.</para></remarks>

        protected override void WriteXmlElements(XmlWriter writer) {

            // write application info element
            ApplicationInfo.WriteXml(writer);

            // write scenario information block
            writer.WriteElementString("title", Title);
            Information.WriteXml(writer);
            Rules.WriteXml(writer);

            // write all scenario subsections
            foreach (ScenarioSection section in SectionUtility.Subsections) {

                // retrieve file path for this section
                RootedPath path = SectionPaths.GetPath(section);

                if (path.IsEmpty) {
                    // write embedded data section
                    GetScenarioElement(section).WriteXml(writer);
                }
                else {
                    // write section "include" tag only
                    writer.WriteStartElement("include");
                    writer.WriteAttributeString("element", SectionUtility.GetXmlName(section));
                    writer.WriteAttributeString("href", path.RelativePath);
                    writer.WriteEndElement();
                }
            }
        }

        #endregion
        #endregion
    }
}
