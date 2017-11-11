using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Windows;

using Tektosyne;
using Tektosyne.IO;
using Tektosyne.Windows;
using Hexkit.Scenario;

namespace Hexkit.Global {

    /// <summary>
    /// Provides file paths for the application.</summary>
    /// <remarks><para>
    /// Some Hexkit files are stored in user-specific directories. Clients must invoke <see
    /// cref="FilePaths.CheckUserFolders"/> before calling any other method in <b>FilePaths</b> or
    /// <see cref="FileDialogs"/> to check that these directories exist and can be written to.
    /// </para><para>
    /// Once initialized, most other public <b>FilePaths</b> methods return <see cref="RootedPath"/>
    /// objects whose <see cref="RootedPath.RootFolder"/> equals either the current <see
    /// cref="FilePaths.CommonFolder"/> or the current <see cref="FilePaths.UserFolder"/>.
    /// </para><para>
    /// Call <see cref="FilePaths.CreateCommonPath"/> or <see cref="FilePaths.CreateUserPath"/> to
    /// create an arbitrary file path rooted in <see cref="FilePaths.CommonFolder"/> or <see
    /// cref="FilePaths.UserFolder"/>, respectively. Call one of the other methods to obtain
    /// absolute paths to options, scenario, or session files.
    /// </para><para>
    /// <b>FilePaths</b> also provides the method <see cref="FindArgumentFile"/> to determine
    /// whether arguments specified on the command line or via drag &amp; drop constitute valid
    /// Hexkit input file paths.</para></remarks>

    public static class FilePaths {
        #region Files & Folders
        #region ApplicationFolder

        /// <summary>
        /// The application home directory.</summary>
        /// <remarks>
        /// <b>ApplicationFolder</b> holds the directory where the executable that started the
        /// current application is located. This is the Hexkit installation directory.</remarks>

        public readonly static string ApplicationFolder =
            System.Windows.Forms.Application.StartupPath;

        #endregion
        #region CommonFolder

        /// <summary>
        /// The root folder for common data.</summary>
        /// <remarks><para>
        /// <b>CommonFolder</b> holds the root folder for all common data, including scenario and
        /// image bitmap files.
        /// </para><para>
        /// <b>CommonFolder</b> currently equals <see cref="ApplicationFolder"/>, but may change to
        /// the system's <see cref="Environment.SpecialFolder.CommonApplicationData"/> folder in a
        /// future Hexkit release.</para></remarks>

        public readonly static string CommonFolder = ApplicationFolder;

        #endregion
        #region HelpFile

        /// <summary>
        /// The location of the application help file.</summary>
        /// <remarks>
        /// <b>HelpFile</b> holds the absolute path to the localized HTML Help file that provides
        /// online help for Hexkit Game and Hexkit Editor. This file is located below <see
        /// cref="ApplicationFolder"/>.</remarks>

        public readonly static string HelpFile = Path.Combine(ApplicationFolder, Strings.HelpFile);

        #endregion
        #region ScenarioFolder

        /// <summary>
        /// The root folder for scenario files.</summary>
        /// <remarks><para>
        /// <b>ScenarioFolder</b> holds a subdirectory of <see cref="CommonFolder"/>.
        /// </para><para>
        /// Subsection files are stored in various subdirectories of <b>ScenarioFolder</b> or <see
        /// cref="CommonFolder"/>. Call <see cref="GetScenarioFile"/> and <see
        /// cref="GetSectionFile"/> to obtain a complete scenario file path.</para></remarks>

        public readonly static string ScenarioFolder = Path.Combine(CommonFolder, "Scenario");

        #endregion
        #region RulesFolder

        /// <summary>
        /// The default folder for scenario rule script files.</summary>
        /// <remarks>
        /// <b>RulesFolder</b> holds a subdirectory of <see cref="ScenarioFolder"/>. Call <see
        /// cref="GetScenarioFile"/> to obtain a complete file path to the default rule script.
        /// </remarks>

        public readonly static string RulesFolder = Path.Combine(ScenarioFolder, "Rules");

        #endregion
        #region UserFolder

        /// <summary>
        /// The root folder for user-specific data.</summary>
        /// <remarks><para>
        /// <b>UserFolder</b> holds the root folder for all user-specific data, including saved
        /// games and application options. This folder is named "Hexkit" and located below the
        /// current user's <see cref="Environment.SpecialFolder.MyDocuments"/> folder.
        /// </para><para>
        /// Call <see cref="CreateUserPath"/> to create an arbitrary file path that is rooted in
        /// <b>UserFolder</b>.</para></remarks>

        public readonly static string UserFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Hexkit");

        #endregion
        #region OptionsFolder

        /// <summary>
        /// The root folder for application options files.</summary>
        /// <remarks>
        /// <b>OptionsFolder</b> holds a subdirectory of <see cref="UserFolder"/>. Call <see
        /// cref="GetOptionsFile"/> to obtain a complete options file path for the current
        /// application (Hexkit Game or Hexkit Editor).</remarks>

        public readonly static string OptionsFolder = Path.Combine(UserFolder, "Options");

        #endregion
        #region SessionFolder

        /// <summary>
        /// The root folder for session files (saved games).</summary>
        /// <remarks>
        /// <b>SessionFolder</b> holds a subdirectory of <see cref="UserFolder"/>. Call <see
        /// cref="GetSessionFile"/> to obtain a complete session file path.</remarks>

        public readonly static string SessionFolder = Path.Combine(UserFolder, "Games");

        #endregion
        #endregion
        #region Namespaces & Schemas
        #region OptionsNamespace

        /// <summary>
        /// The target namespace for <see cref="OptionsSchema"/>.</summary>

        public const string OptionsNamespace = "http://www.kynosarges.de/Hexkit.Options";

        #endregion
        #region OptionsSchema

        /// <summary>
        /// The location of the XML schema used to validate application options files.</summary>
        /// <remarks>
        /// <b>OptionsSchema</b> holds the absolute path to the XML schema file used to validate
        /// application options files when starting Hexkit Game or Hexkit Editor.</remarks>

        public readonly static string OptionsSchema =
            Path.Combine(ApplicationFolder, "Hexkit.Options.xsd");

        #endregion
        #region ScenarioNamespace

        /// <summary>
        /// The target namespace for <see cref="ScenarioSchema"/>.</summary>

        public const string ScenarioNamespace = "http://www.kynosarges.de/Hexkit.Scenario";

        #endregion
        #region ScenarioSchema

        /// <summary>
        /// The location of the XML schema used to validate scenario files.</summary>
        /// <remarks>
        /// <b>ScenarioSchema</b> holds the absolute path to the XML schema file used to validate
        /// the temporary scenario <see cref="ScenarioFileType.Start"/> file.</remarks>

        public readonly static string ScenarioSchema =
            Path.Combine(ApplicationFolder, "Hexkit.Scenario.xsd");

        #endregion
        #region SessionNamespace

        /// <summary>
        /// The target namespace for <see cref="SessionSchema"/>.</summary>

        public const string SessionNamespace = "http://www.kynosarges.de/Hexkit.Session";

        #endregion
        #region SessionSchema

        /// <summary>
        /// The location of the XML schema used to validate session files.</summary>
        /// <remarks>
        /// <b>SessionSchema</b> holds the absolute path to the XML schema file used to validate
        /// session files when loading a saved game.</remarks>

        public readonly static string SessionSchema =
            Path.Combine(ApplicationFolder, "Hexkit.Session.xsd");

        #endregion
        #endregion
        #region Private Methods
        #region CreateTestFile

        /// <summary>
        /// Attempts to create a test file in the specified directory.</summary>
        /// <param name="directory">
        /// The directory in which to create the test file.</param>
        /// <returns>
        /// <c>true</c> if a test file could be created in the specified <paramref
        /// name="directory"/>; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullOrEmptyException">
        /// <paramref name="directory"/> is a null reference or an empty string.</exception>
        /// <remarks>
        /// <b>CreateTestFile</b> attempts to create a file named "HexkitTest.tmp" in the specified
        /// <paramref name="directory"/>. If successful, the file is deleted before returning. Any
        /// existing file with the same name will be overwritten, if possible. On failure,
        /// <b>CreateTestFile</b> shows an error message.</remarks>

        private static bool CreateTestFile(string directory) {
            if (String.IsNullOrEmpty(directory))
                ThrowHelper.ThrowArgumentNullOrEmptyException("directory");

            FileStream stream = null;
            string path = Path.Combine(directory, "HexkitTest.tmp");

            try {
                stream = File.Create(path);
                return true;
            }
            catch (Exception e) {
                string message = String.Format(ApplicationInfo.Culture,
                    Strings.DialogFileCreateError, path);

                MessageDialog.Show(null, message, Strings.TitleFileAccessError,
                    e, MessageBoxButton.OK, Images.Error);

                return false;
            }
            finally {
                if (stream != null) {
                    stream.Close();
                    if (File.Exists(path)) File.Delete(path);
                }
            }
        }

        #endregion
        #region CreateUserFolder

        /// <summary>
        /// Attempts to create the specified folder and checks for write permission.</summary>
        /// <param name="directory">
        /// The absolute path to the folder to create.</param>
        /// <returns>
        /// <c>true</c> if the specified <paramref name="directory"/> exists or was successfully
        /// created, and can be written to; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullOrEmptyException">
        /// <paramref name="directory"/> is a null reference or an empty string.</exception>
        /// <remarks><para>
        /// <b>CreateUserFolder</b> attempts to locate the specified <paramref name="directory"/>,
        /// and attempts to create the <paramref name="directory"/> if it does not already exist.
        /// </para><para>
        /// On success, <b>CreateUserFolder</b> then calls <see cref="CreateTestFile"/> to check
        /// that the <paramref name="directory"/> can be written to. If any part of this operation
        /// fails, <b>CreateUserFolder</b> shows an error message and aborts.</para></remarks>

        private static bool CreateUserFolder(string directory) {
            if (String.IsNullOrEmpty(directory))
                ThrowHelper.ThrowArgumentNullOrEmptyException("directory");

            try {
                // create folder if it doesn't exist
                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                // attempt to create test file
                return CreateTestFile(directory);
            }
            catch (Exception e) {
                string message = String.Format(ApplicationInfo.Culture,
                    Strings.DialogFileCreateError, directory);

                MessageDialog.Show(null, message, Strings.TitleFileAccessError,
                    e, MessageBoxButton.OK, Images.Error);

                return false;
            }
        }

        #endregion
        #endregion
        #region Internal Methods
        #region CreateCommonPath(String, String)

        /// <summary>
        /// Creates a <see cref="RootedPath"/> that is rooted in the <see cref="CommonFolder"/> and
        /// wraps the specified directory and file.</summary>
        /// <param name="directory">
        /// The directory containing the file to wrap. This argument may be a null reference or an
        /// empty string.</param>
        /// <param name="file">
        /// The name of the file to wrap. This argument may be a null reference or an empty string.
        /// </param>
        /// <returns>
        /// A <see cref="RootedPath"/> whose <see cref="RootedPath.RootFolder"/> equals <see
        /// cref="CommonFolder"/> and that wraps the combination of the specified <paramref
        /// name="directory"/> and <paramref name="file"/>.</returns>

        internal static RootedPath CreateCommonPath(string directory, string file) {
            string filePath = Path.Combine(directory ?? "", file ?? "");
            return new RootedPath(CommonFolder, filePath);
        }

        #endregion
        #region CreateUserPath(String, String)

        /// <summary>
        /// Creates a <see cref="RootedPath"/> that is rooted in the <see cref="UserFolder"/> and
        /// wraps the specified directory and file.</summary>
        /// <param name="directory">
        /// The directory containing the file to wrap. This argument may be a null reference or an
        /// empty string.</param>
        /// <param name="file">
        /// The name of the file to wrap. This argument may be a null reference or an empty string.
        /// </param>
        /// <returns>
        /// A <see cref="RootedPath"/> whose <see cref="RootedPath.RootFolder"/> equals <see
        /// cref="UserFolder"/> and that wraps the combination of the specified <paramref
        /// name="directory"/> and <paramref name="file"/>.</returns>

        internal static RootedPath CreateUserPath(string directory, string file) {
            string filePath = Path.Combine(directory ?? "", file ?? "");
            return new RootedPath(UserFolder, filePath);
        }

        #endregion
        #region GetSectionFolder

        /// <summary>
        /// Gets the default folder for the specified <see cref="ScenarioSection"/>.</summary>
        /// <param name="section">
        /// A <see cref="ScenarioSection"/> value indicating the scenario section whose default
        /// folder to return.</param>
        /// <returns>
        /// The absolute path to the default folder for the specified <paramref name="section"/>.
        /// </returns>
        /// <exception cref="InvalidEnumArgumentException">
        /// <paramref name="section"/> is not a valid <see cref="ScenarioSection"/> value.
        /// </exception>
        /// <remarks>
        /// All <see cref="ScenarioSection"/> default folders are located below <see
        /// cref="ScenarioFolder"/>, except for the <see cref="ScenarioSection.Images"/> section
        /// which is located directly below <see cref="CommonFolder"/>.</remarks>

        internal static string GetSectionFolder(ScenarioSection section) {
            switch (section) {

                case ScenarioSection.Areas:     return Path.Combine(ScenarioFolder, "Areas");
                case ScenarioSection.Entities:  return Path.Combine(ScenarioFolder, "Entities");
                case ScenarioSection.Factions:  return Path.Combine(ScenarioFolder, "Factions");
                case ScenarioSection.Images:    return Path.Combine(CommonFolder, "Images");
                case ScenarioSection.Master:    return ScenarioFolder;
                case ScenarioSection.Variables: return Path.Combine(ScenarioFolder, "Variables");

                default:
                    ThrowHelper.ThrowInvalidEnumArgumentException(
                        "section", (int) section, typeof(ScenarioSection));
                    return null;
            }
        }

        #endregion
        #endregion
        #region Public Methods
        #region CheckUserFolders

        /// <summary>
        /// Checks that all required user-specific folders exist and can be written to.</summary>
        /// <returns>
        /// <c>true</c> if all required user-specific folders were successfully created and can be
        /// written to; otherwise, <c>false</c>.</returns>
        /// <remarks><para>
        /// Hexkit Game and Hexkit Editor require a writable directory for user-specific data,
        /// indicated by <see cref="UserFolder"/>, and two subdirectories indicated by <see
        /// cref="OptionsFolder"/> and <see cref="SessionFolder"/>.
        /// </para><para>
        /// Both applications should call <b>CheckUserFolders</b> during startup, before doing
        /// anything else, and quit immediately if the call fails. <b>CheckUserFolders</b> will show
        /// an error message on failure, but will never propagate exceptions to the caller.
        /// </para></remarks>

        public static bool CheckUserFolders() {

            // create root folder for user files
            if (!CreateUserFolder(UserFolder))
                return false;

            // create folder for options files
            if (!CreateUserFolder(OptionsFolder))
                return false;

            // create folder for session files
            if (!CreateUserFolder(SessionFolder))
                return false;

            return true;
        }

        #endregion
        #region CreateCommonPath()

        /// <overloads>
        /// Creates a new <see cref="RootedPath"/> that is rooted in the <see cref="CommonFolder"/>.
        /// </overloads>
        /// <summary>
        /// Creates an empty <see cref="RootedPath"/> that is rooted in the <see
        /// cref="CommonFolder"/>.</summary>
        /// <returns>
        /// A <see cref="RootedPath"/> whose <see cref="RootedPath.RootFolder"/> equals <see
        /// cref="CommonFolder"/>.</returns>

        public static RootedPath CreateCommonPath() {
            return new RootedPath(CommonFolder);
        }

        #endregion
        #region CreateCommonPath(String)

        /// <summary>
        /// Creates a <see cref="RootedPath"/> that is rooted in the <see cref="CommonFolder"/> and
        /// wraps the specified file path.</summary>
        /// <param name="filePath">
        /// The absolute or relative file path to wrap. This argument may be a null reference or an
        /// empty string.</param>
        /// <returns>
        /// A <see cref="RootedPath"/> whose <see cref="RootedPath.RootFolder"/> equals <see
        /// cref="CommonFolder"/> and that wraps the specified <paramref name="filePath"/>.
        /// </returns>

        public static RootedPath CreateCommonPath(string filePath) {
            return new RootedPath(CommonFolder, filePath);
        }

        #endregion
        #region CreateUserPath()

        /// <overloads>
        /// Creates a new <see cref="RootedPath"/> that is rooted in the <see cref="UserFolder"/>.
        /// </overloads>
        /// <summary>
        /// Creates an empty <see cref="RootedPath"/> that is rooted in the <see
        /// cref="UserFolder"/>.</summary>
        /// <returns>
        /// A <see cref="RootedPath"/> whose <see cref="RootedPath.RootFolder"/> equals <see
        /// cref="UserFolder"/>.</returns>

        public static RootedPath CreateUserPath() {
            return new RootedPath(UserFolder);
        }

        #endregion
        #region CreateUserPath(String)

        /// <summary>
        /// Creates a <see cref="RootedPath"/> that is rooted in the <see cref="UserFolder"/> and
        /// wraps the specified file path.</summary>
        /// <param name="filePath">
        /// The absolute or relative file path to wrap. This argument may be a null reference or an
        /// empty string.</param>
        /// <returns>
        /// A <see cref="RootedPath"/> whose <see cref="RootedPath.RootFolder"/> equals <see
        /// cref="UserFolder"/> and that wraps the specified <paramref name="filePath"/>.</returns>

        public static RootedPath CreateUserPath(string filePath) {
            return new RootedPath(UserFolder, filePath);
        }

        #endregion
        #region FindArgumentFile

        /// <summary>
        /// Determines the <see cref="ArgumentFileType"/> of the specified file.</summary>
        /// <param name="file"><para>
        /// The name of the file whose <see cref="ArgumentFileType"/> to determine, optionally
        /// prefixed with a relative or absolute directory path.
        /// </para><para>
        /// On return, <paramref name="file"/> holds the fully qualified path to the specified file
        /// if it could be located; otherwise, <paramref name="file"/> remains unchanged.
        /// </para></param>
        /// <returns>
        /// A <see cref="ArgumentFileType"/> value indicating whether the specified <paramref
        /// name="file"/> is a valid Hexkit input file.</returns>
        /// <exception cref="ArgumentNullOrEmptyException">
        /// <paramref name="file"/> is a null reference or an empty string.</exception>
        /// <remarks><para>
        /// If the specified <paramref name="file"/> does not contain an extension,
        /// <b>FindArgumentFile</b> appends ".xml*" for the search.
        /// </para><para>
        /// If the specified <paramref name="file"/> contains a relative or absolute directory path,
        /// <b>FindArgumentFile</b> only checks that path (relative to the current directory for
        /// relative paths).
        /// </para><para>
        /// Otherwise, <b>FindArgumentFile</b> first searches the current directory, and then the
        /// directory trees below <see cref="ScenarioFolder"/> and <see cref="SessionFolder"/>.
        /// </para><para>
        /// If a match was found, <b>FindArgumentFile</b> stores it in the <paramref name="file"/>
        /// parameter and checks that its extension equals ".xml" or ".xml.gz". If so,
        /// <b>FindArgumentFile</b> attempts to read the first 256 Unicode characters from the file,
        /// using GZip decompression if the extension is ".xml.gz".
        /// </para><para>
        /// If the file contains a <see cref="ScenarioNamespace"/> or <see cref="SessionNamespace"/>
        /// declaration, <b>FindArgumentFile</b> returns <see cref="ArgumentFileType.Scenario"/> or
        /// <see cref="ArgumentFileType.Session"/>, respectively.
        /// </para><para>
        /// <c>FindArgumentFile</c> returns <see cref="ArgumentFileType.Missing"/> if the specified
        /// <paramref name="file"/> could not be found, and <see cref="ArgumentFileType.Invalid"/>
        /// if <paramref name="file"/> was found but has an invalid extension, could not be read, or
        /// does not contain a valid XML namespace declaration.</para></remarks>

        public static ArgumentFileType FindArgumentFile(ref string file) {
            if (String.IsNullOrEmpty(file))
                ThrowHelper.ThrowArgumentNullOrEmptyException("file");

            try {
                string foundFile, searchFile = file;

                // append wildcard extension if missing
                if (String.IsNullOrEmpty(Path.GetExtension(file)))
                    searchFile += ".xml*";

                // check if file contains a directory prefix
                string dirName = Path.GetDirectoryName(file);
                if (!String.IsNullOrEmpty(dirName)) {

                    // search file in specified directory only
                    searchFile = Path.GetFileName(searchFile);
                    foundFile = IOUtility.SearchDirectory(dirName, searchFile);
                } else {
                    // search file in current directory
                    foundFile = IOUtility.SearchDirectory(".", searchFile);

                    // try scenario tree if not found
                    if (foundFile == null)
                        foundFile = IOUtility.SearchDirectoryTree(ScenarioFolder, searchFile);

                    // try session tree if not found
                    if (foundFile == null)
                        foundFile = IOUtility.SearchDirectoryTree(SessionFolder, searchFile);
                }

                // abort search if file not found
                if (foundFile == null)
                    return ArgumentFileType.Missing;

                // record found file
                file = foundFile;
            }
            catch {
                // error during file search
                return ArgumentFileType.Missing;
            }

            bool compressed = false;

            // check for valid extensions
            if (file.EndsWith(".xml.gz", StringComparison.OrdinalIgnoreCase))
                compressed = true;
            else if (!file.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                return ArgumentFileType.Invalid;

            Stream stream = null;
            try {
                // create stream for uncompressed data
                stream = new FileStream(file, FileMode.Open);

                // create stream with GZip decompression
                if (compressed)
                    stream = new GZipStream(stream, CompressionMode.Decompress);

                // search for XML namespace declaration
                using (StreamReader reader = new StreamReader(stream, Encoding.UTF8)) {

                    // sample first 256 characters
                    char[] buffer = new char[256];
                    int count = reader.Read(buffer, 0, buffer.Length);
                    string sample = new String(buffer, 0, count);

                    // identify scenario file by scenario namespace
                    if (sample.IndexOf(ScenarioNamespace, StringComparison.Ordinal) >= 0)
                        return ArgumentFileType.Scenario;

                    // identify session file by session namespace
                    if (sample.IndexOf(SessionNamespace, StringComparison.Ordinal) >= 0)
                        return ArgumentFileType.Session;
                }

                // unknown file contents
                return ArgumentFileType.Invalid;
            }
            catch {
                // error during file access
                return ArgumentFileType.Invalid;
            }
            finally {
                if (stream != null) stream.Close();
            }
        }

        #endregion
        #region GetOptionsFile

        /// <summary>
        /// Returns the absolute path to the XML options description file for the current
        /// application.</summary>
        /// <returns>
        /// A <see cref="RootedPath"/> wrapping the absolute path to the XML options description
        /// file for the current application.</returns>
        /// <remarks>
        /// <b>GetOptionsFile</b> returns a file located in the <see cref="OptionsFolder"/> whose
        /// name depends on the current application (Hexkit Game or Hexkit Editor).</remarks>

        public static RootedPath GetOptionsFile() {

            string file = (ApplicationInfo.IsEditor ?
                "Hexkit.Editor.Options.xml" : "Hexkit.Game.Options.xml");

            return CreateUserPath(OptionsFolder, file);
        }

        #endregion
        #region GetScenarioFile(ScenarioFileType)

        /// <overloads>
        /// Returns the absolute path to a predefined or specified XML scenario description file.
        /// </overloads>
        /// <summary>
        /// Returns the absolute path to a predefined XML scenario description file.</summary>
        /// <param name="type">
        /// A <see cref="ScenarioFileType"/> value indicating the predefined XML scenario
        /// description file whose path to return.</param>
        /// <returns>
        /// A <see cref="RootedPath"/> wrapping the absolute path to the predefined XML scenario
        /// description file of the specified <paramref name="type"/>.</returns>
        /// <exception cref="InvalidEnumArgumentException">
        /// <paramref name="type"/> specifies an invalid <see cref="ScenarioFileType"/> value.
        /// </exception>
        /// <remarks>
        /// <b>GetScenarioFile</b> returns a file path below <see cref="CommonFolder"/> or <see
        /// cref="UserFolder"/>, depending on the specified <paramref name="type"/>. Please refer to
        /// <see cref="ScenarioFileType"/> for details.</remarks>

        public static RootedPath GetScenarioFile(ScenarioFileType type) {
            switch (type) {

                case ScenarioFileType.Debug:
                    return CreateUserPath("Scenario.Debug.xml");

                case ScenarioFileType.Rules:
                    return CreateCommonPath(RulesFolder, "Default Rules.cs");

                case ScenarioFileType.Start:
                    return CreateUserPath("Scenario.Start.xml");

                default:
                    ThrowHelper.ThrowInvalidEnumArgumentException(
                        "type", (int) type, typeof(ScenarioFileType));
                    return null;
            }
        }

        #endregion
        #region GetScenarioFile(String)

        /// <summary>
        /// Returns the absolute path to the specified XML scenario description file.</summary>
        /// <param name="file">
        /// An absolute or relative file path to an XML scenario description file.</param>
        /// <returns>
        /// A <see cref="RootedPath"/> wrapping the absolute path to the specified XML scenario
        /// description <paramref name="file"/>.</returns>
        /// <remarks>
        /// <b>GetScenarioFile</b> returns the specified <paramref name="file"/> if it contains an
        /// absolute path; otherwise, a file path below <see cref="ScenarioFolder"/> whose relative
        /// path and file name match the specified <paramref name="file"/>.</remarks>

        public static RootedPath GetScenarioFile(string file) {
            return CreateCommonPath(ScenarioFolder, file);
        }

        #endregion
        #region GetSectionFile

        /// <summary>
        /// Returns the absolute path to the specified XML scenario section file.</summary>
        /// <param name="section">
        /// A <see cref="ScenarioSection"/> value indicating the scenario section associated with
        /// the specified <paramref name="file"/>.</param>
        /// <param name="file">
        /// An absolute or relative file path to an XML scenario section file.</param>
        /// <returns>
        /// A <see cref="RootedPath"/> wrapping the absolute path to the specified XML scenario
        /// section <paramref name="file"/>.</returns>
        /// <exception cref="InvalidEnumArgumentException">
        /// <paramref name="section"/> is not a valid <see cref="ScenarioSection"/> value.
        /// </exception>
        /// <remarks>
        /// <b>GetSectionFile</b> returns the specified <paramref name="file"/> if it contains an
        /// absolute path; otherwise, a file path below the default folder for the specified
        /// <paramref name="section"/>.</remarks>

        public static RootedPath GetSectionFile(ScenarioSection section, string file) {
            string directory = GetSectionFolder(section);
            return CreateCommonPath(directory, file);
        }

        #endregion
        #region GetSessionFile(SessionFileType)

        /// <overloads>
        /// Returns the absolute path to a predefined or specified XML session description file.
        /// </overloads>
        /// <summary>
        /// Returns the absolute path to a predefined XML session description file.</summary>
        /// <param name="type">
        /// A <see cref="SessionFileType"/> value indicating the predefined XML session description
        /// file whose path to return.</param>
        /// <returns>
        /// A <see cref="RootedPath"/> wrapping the absolute path to the predefined XML session
        /// description file of the specified <paramref name="type"/>.</returns>
        /// <exception cref="InvalidEnumArgumentException">
        /// <paramref name="type"/> specifies an invalid <see cref="SessionFileType"/> value.
        /// </exception>
        /// <remarks>
        /// <b>GetSessionFile</b> always returns a file path below <see cref="UserFolder"/>. Please
        /// refer to <see cref="SessionFileType"/> for details.</remarks>

        public static RootedPath GetSessionFile(SessionFileType type) {
            switch (type) {

                case SessionFileType.Auto:
                    return GetSessionFile("AutoSave.xml.gz");

                case SessionFileType.Computer:
                    return GetSessionFile("ComputerSave.xml.gz");

                case SessionFileType.Debug:
                    return CreateUserPath("Session.Debug.xml.gz");

                case SessionFileType.Email:
                    return GetSessionFile("EmailSave.xml.gz");

                default:
                    ThrowHelper.ThrowInvalidEnumArgumentException(
                        "type", (int) type, typeof(SessionFileType));
                    return null;
            }
        }

        #endregion
        #region GetSessionFile(String)

        /// <summary>
        /// Returns the absolute path to the specified XML session description file.</summary>
        /// <param name="file">
        /// An absolute or relative file path to an XML session description file.</param>
        /// <returns>
        /// A <see cref="RootedPath"/> wrapping the absolute path to the specified XML session
        /// description <paramref name="file"/>.</returns>
        /// <remarks>
        /// <b>GetSessionFile</b> returns the specified <paramref name="file"/> if it contains an
        /// absolute path; otherwise, a file path below <see cref="SessionFolder"/> whose relative
        /// path and file name match the specified <paramref name="file"/>.</remarks>

        public static RootedPath GetSessionFile(string file) {
            return CreateUserPath(SessionFolder, file);
        }

        #endregion
        #region SearchScenarioTree

        /// <summary>
        /// Attempts to find the specified XML scenario description file, either at its original
        /// location or below <see cref="ScenarioFolder"/>.</summary>
        /// <param name="file">
        /// An absolute or relative file path to an XML scenario description file.</param>
        /// <returns>
        /// A <see cref="RootedPath"/> wrapping the absolute path at which the specified <paramref
        /// name="file"/> was found.</returns>
        /// <exception cref="FileNotFoundException">
        /// The specified scenario <paramref name="file"/> could not be found anywhere.</exception>
        /// <exception cref="ArgumentNullOrEmptyException">
        /// <paramref name="file"/> is a null reference or an empty string.</exception>
        /// <remarks>
        /// <b>SearchScenarioTree</b> prepends <see cref="ScenarioFolder"/> to the specified
        /// <paramref name="file"/> if it contains a relative path. If no file exists at the
        /// resulting absolute path, <b>SearchScenarioTree</b> then attempts to find the file name
        /// anywhere in the subdirectory tree below <see cref="ScenarioFolder"/>.</remarks>

        public static RootedPath SearchScenarioTree(string file) {
            if (String.IsNullOrEmpty(file))
                ThrowHelper.ThrowArgumentNullOrEmptyException("file");

            // prepend default scenario path to relative paths
            RootedPath path = CreateCommonPath(ScenarioFolder, file);

            // check if file exists at specified location
            if (!File.Exists(path.AbsolutePath)) {

                // look for file name in scenario directory tree
                string search = IOUtility.SearchDirectoryTree(ScenarioFolder, path.FileName);
                path = path.Change(search);

                // file not found anywhere, abort operation
                if (path.IsEmpty)
                    ThrowHelper.ThrowFileNotFoundException(file, Strings.ErrorGameScenario);
            }

            Debug.Assert(File.Exists(path.AbsolutePath));
            return path;
        }

        #endregion
        #endregion
    }
}
