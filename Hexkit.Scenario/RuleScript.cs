using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Xml;

using Tektosyne;
using Tektosyne.IO;
using Tektosyne.Xml;
using Hexkit.Global;

namespace Hexkit.Scenario {

    /// <summary>
    /// Manages the rule script associated with a scenario.</summary>
    /// <remarks>
    /// <b>RuleScript</b> is serialized to the XML element "rules" defined in <see
    /// cref="FilePaths.ScenarioSchema"/>.</remarks>

    public sealed class RuleScript: ScenarioElement, IDisposable {
        #region Private Fields

        // property backers
        private RootedPath _path = FilePaths.CreateCommonPath();

        #endregion
        #region Factory

        /// <summary>
        /// Gets the <b>IRulesFactory</b> instance created from the file located at <see
        /// cref="Path"/>.</summary>
        /// <value>
        /// An <b>IRulesFactory</b> instance created from the file located at <see cref="Path"/>.
        /// </value>
        /// <remarks><para>
        /// <b>Factory</b> may return a null reference if the rule script has not yet been loaded or
        /// compiled.
        /// </para><para>
        /// The returned object must implement the interface <b>Hexkit.World.IRulesFactory</b>.
        /// However, <b>Factory</b> returns an <see cref="IDisposable"/> instance because
        /// <b>Hexkit.World</b> types are not known to the <b>Hexkit.Scenario</b> assembly.
        /// </para></remarks>

        public IDisposable Factory { get; private set; }

        #endregion
        #region Path

        /// <summary>
        /// Gets or sets the path to the rule script file.</summary>
        /// <value>
        /// The path to the rule script file, relative to the current root folder for Hexkit user
        /// files. The default is an empty string.</value>
        /// <exception cref="InvalidOperationException">
        /// The property is set, and <see cref="ApplicationInfo.IsEditor"/> is <c>false</c>.
        /// </exception>
        /// <remarks>
        /// <b>Path</b> returns an empty string when set to a null reference. This property holds
        /// the value of the "href" XML attribute.</remarks>

        public string Path {
            get { return this._path.RelativePath; }
            set {
                ApplicationInfo.CheckEditor();
                this._path = this._path.Change(value);
            }
        }

        #endregion
        #region Compile

        /// <summary>
        /// Compiles the rule script file located at <see cref="Path"/>, using the specified CodeDOM
        /// provider and compiler options.</summary>
        /// <param name="provider">
        /// The <see cref="CodeDomProvider"/> that should be used to compile the file located at
        /// <see cref="Path"/>.</param>
        /// <param name="options">
        /// The value for the <see cref="CompilerParameters.CompilerOptions"/> property of the <see
        /// cref="CompilerParameters"/> that are supplied to the specified <paramref
        /// name="provider"/>.</param>
        /// <returns>
        /// The <see cref="Assembly"/> that resulted from compilation. This value is never a null
        /// reference.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="provider"/> is a null reference.</exception>
        /// <exception cref="DetailException">
        /// An error occurred while compiling the rule script. All such exceptions are converted to
        /// a <b>DetailException</b> whose <see cref="DetailException.Detail"/> property holds the
        /// message text provided by the original exception.</exception>
        /// <exception cref="PropertyValueException">
        /// <see cref="Path"/> holds an empty string.</exception>

        internal Assembly Compile(CodeDomProvider provider, string options) {
            if (provider == null)
                ThrowHelper.ThrowArgumentNullException("provider");

            // file path must be defined by now
            if (Path.Length == 0)
                ThrowHelper.ThrowInvalidOperationException(Global.Strings.ErrorRulesPathNull);

            try {
                // create parameters for script compilation
                CompilerParameters param = new CompilerParameters();
                if (!String.IsNullOrEmpty(options))
                    param.CompilerOptions = options;

                // generate library, enable all warnings
                param.GenerateExecutable = false;
                param.WarningLevel = 4;

                /*
                 * When debugging, the rule script assembly is written to a temporary
                 * DLL file on disk, plus associated PDB file. This enables source-level
                 * debugging of the rule script in Visual Studio.
                 *
                 * We must use uniquely named temporary files because .NET cannot unload
                 * assemblies from the default AppDomain once loaded, so any created files
                 * remain locked and cannot be overwritten while Hexkit Game is running.
                 *
                 * This also means that running Hexkit Game in debug mode will litter the
                 * Windows directory for temporary files with DLL and PDB files that we
                 * cannot delete from within the program. They must be deleted manually
                 * once Hexkit Game is closed.
                 *
                 * Note that we must explicitly set OutputAssembly to a temporary file name.
                 * Merely setting GenerateInMemory to false writes the assembly to a temporary
                 * DLL file just as well, but it does not create the required PDB file!
                 * 
                 * We only treat warnings as errors in debug mode. The C# compiler issues
                 * so many pointless warnings, such as for a malformed LIB path (CS1668),
                 * that users might otherwise be unable to run even the demo scenarios.
                 */
#if DEBUG
                param.GenerateInMemory = false;
                param.IncludeDebugInformation = true;
                param.OutputAssembly = PathEx.GetTempFileName(".dll");
                param.TreatWarningsAsErrors = true;
#else
                param.GenerateInMemory = true;
                param.IncludeDebugInformation = false;
                param.TreatWarningsAsErrors = false;
#endif
                // reference system assemblies
                param.ReferencedAssemblies.Add("System.dll");

                // reference assemblies with type definitions
                param.ReferencedAssemblies.Add("Hexkit.Scenario.dll");
                param.ReferencedAssemblies.Add("Hexkit.World.dll");
                param.ReferencedAssemblies.Add("Tektosyne.Core.dll");

                // attempt to compile the source code file
                CompilerResults results = provider.CompileAssemblyFromFile(param, Path);

                // throw exception if compilation failed
                if (results.NativeCompilerReturnValue != 0)
                    ThrowHelper.ThrowTypeLoadException(
                        String.Join(Environment.NewLine, results.Output));

                // retrieve compiled assembly
                Assembly assembly = results.CompiledAssembly;
                if (assembly == null)
                    ThrowHelper.ThrowTypeLoadException(Global.Strings.ErrorRulesAssembly);

                return assembly;
            }
            catch (Exception e) {
                // rethrow exception as DetailException
                ThrowHelper.ThrowDetailExceptionWithFormat(
                    Global.Strings.ErrorRulesCompile, Path, e);

                return null;
            }
        }

        #endregion
        #region CreateFactory

        /// <summary>
        /// Creates an instance of the <b>RulesFactory</b> class defined by the specified assembly,
        /// or by the one located at <see cref="Path"/>.</summary>
        /// <param name="assembly"><para>
        /// The <see cref="Assembly"/> whose class to instantiate.
        /// </para><para>-or-</para><para>
        /// A null reference to load a precompiled assembly from the file indicated by the <see
        /// cref="Path"/> property.</para></param>
        /// <returns>
        /// The <see cref="IDisposable"/> instance created from the assembly's <b>RulesFactory</b>
        /// class. This value is never a null reference.</returns>
        /// <exception cref="DetailException">
        /// An error occurred while loading the assembly. All such exceptions are converted to a
        /// <b>DetailException</b> whose <see cref="DetailException.Detail"/> property holds the
        /// message text provided by the original exception.</exception>
        /// <exception cref="InvalidOperationException">
        /// <paramref name="assembly"/> is a null reference, and <see cref="Path"/> holds an empty
        /// string.</exception>
        /// <remarks><para>
        /// <b>CreateFactory</b> attempts to load a precompiled .NET assembly from the file
        /// indicated by the <see cref="Path"/> property if the specified <paramref
        /// name="assembly"/> is a null reference.
        /// </para><para>
        /// Regardless, the assembly must define a type called <b>Hexkit.World.RulesFactory</b> that
        /// defines a public default constructor. <b>CreateFactory</b> invokes that constructor and
        /// returns the created object.
        /// </para><para>
        /// <b>RulesFactory</b> must also implement the interface <b>Hexkit.World.IRulesFactory</b>.
        /// However, <b>CreateFactory</b> returns the new object as an instance of <see
        /// cref="IDisposable"/> because <b>Hexkit.World</b> types are not known to the
        /// <b>Hexkit.Scenario</b> assembly.</para></remarks>

        internal IDisposable CreateFactory(Assembly assembly) {
            try {
                // load assembly from path if not defined
                if (assembly == null) {

                    // file path must be defined by now
                    if (Path.Length == 0)
                        ThrowHelper.ThrowInvalidOperationException(
                            Global.Strings.ErrorRulesPathNull);

                    // attempt to load the assembly
                    assembly = Assembly.LoadFrom(Path);
                    if (assembly == null)
                        ThrowHelper.ThrowTypeLoadException(Global.Strings.ErrorRulesAssembly);
                }

                // retrieve handle to RulesFactory class
                Type factory = assembly.GetType("Hexkit.World.RulesFactory");
                if (factory== null)
                    ThrowHelper.ThrowTypeLoadException(Global.Strings.ErrorRulesFactory);

                // retrieve handle to parameterless constructor
                ConstructorInfo ctor = factory.GetConstructor(Type.EmptyTypes);
                if (ctor == null)
                    ThrowHelper.ThrowTypeLoadException(Global.Strings.ErrorRulesFactoryCtor);

                // instantiate IRulesFactory object
                // (unknown type, so we substitute IDisposable)
                IDisposable instance = ctor.Invoke(null) as IDisposable;
                if (instance == null)
                    ThrowHelper.ThrowTypeLoadException(Global.Strings.ErrorRulesFactoryObject);

                return instance;
            }
            catch (Exception e) {
                // rethrow exception as DetailException
                ThrowHelper.ThrowDetailException(Global.Strings.ErrorRulesLoad, e);
                return null;
            }
        }

        #endregion
        #region Load

        /// <summary>
        /// Loads the rule script file located at <see cref="Path"/>.</summary>
        /// <exception cref="DetailException">
        /// <see cref="Path"/> specifies an unknown file type, or an error occurred while loading or
        /// compiling the rule script. All such exceptions are converted to a <b>DetailException</b>
        /// whose <see cref="DetailException.Detail"/> property holds the message text provided by
        /// the original exception.</exception>
        /// <exception cref="PropertyValueException">
        /// <see cref="Path"/> holds an empty string.</exception>
        /// <remarks><para>
        /// <b>Load</b> takes the following action, based on the extension of the file located at
        /// <see cref="Path"/> (using case-insensitive comparison):
        /// </para><list type="table"><listheader>
        /// <term>Extension</term><description>Action</description>
        /// </listheader><item>
        /// <term>".cs"</term><description>Compile as C# source file.</description>
        /// </item><item>
        /// <term>".dll"</term><description>Load as precompiled .NET assembly.</description>
        /// </item><item>
        /// <term>".js"</term><description>Compile as JScript .NET source file.</description>
        /// </item><item>
        /// <term>".vb"</term><description>Compile as Visual Basic .NET source file.</description>
        /// </item></list><para>
        /// Any other file extension results in a <see cref="DetailException"/>.
        /// </para><para>
        /// All rule scripts must define a type named <b>RulesFactory</b>. After the rule script
        /// assembly was successfully loaded or compiled, <b>Load</b> attempts to instantiate this
        /// type and stores the new object in the <see cref="Factory"/> property.</para></remarks>

        public void Load() {

            // file path must be defined by now
            if (Path.Length == 0)
                ThrowHelper.ThrowInvalidOperationException(Global.Strings.ErrorRulesPathNull);

            CodeDomProvider provider = null;
            var providerOptions = new Dictionary<String, String>(1);
            providerOptions.Add("CompilerVersion", "v4.0");
            string options = "";

            // determine how to process the rule script
            string extension = System.IO.Path.GetExtension(Path);
            switch (extension.ToUpperInvariant()) {

                case ".CS":
                    // use Microsoft's C# compiler
                    provider = new Microsoft.CSharp.CSharpCodeProvider(providerOptions);
#if DEBUG
                    options = " /d:TRACE";
#else
                    options = " /d:TRACE /optimize+";
#endif
                    goto case "anySource";

                case ".DLL":
                    // use precompiled rule script
                    Factory = CreateFactory(null);
                    break;

                case ".JS":
                    // use Microsoft's JScript .NET compiler
                    provider = new Microsoft.JScript.JScriptCodeProvider();
                    options = " /autoref+ /fast+";
                    goto case "anySource";

                case ".VB":
                    // use Microsoft's Visual Basic compiler
                    provider = new Microsoft.VisualBasic.VBCodeProvider(providerOptions);
#if DEBUG
                    options = " /d:TRACE=TRUE";
#else
                    options = " /d:TRACE=TRUE /optimize+";
#endif
                    goto case "anySource";

                case "anySource": {
                    // compile rule script in source file format
                    Assembly assembly = Compile(provider, options);
                    Factory = CreateFactory(assembly);
                    break;
                }

                default:
                    // unrecognized file extension
                    ThrowHelper.ThrowDetailExceptionWithFormat(Global.Strings.ErrorRulesFile, Path);
                    break;
            }

            Debug.Assert(Factory != null);
        }

        #endregion
        #region IDisposable Members

        /// <summary>
        /// Releases all resources used by the <see cref="RuleScript"/> object.</summary>
        /// <remarks>
        /// <b>Dispose</b> clears the <see cref="Path"/> and <see cref="Factory"/> properties and
        /// also invokes <see cref="IDisposable.Dispose"/> on the current <see cref="Factory"/>, if
        /// any. This allows the rule script to release any unmanaged resources that it might have
        /// allocated.</remarks>

        public void Dispose() {
            this._path = this._path.Clear();

            if (Factory != null) {
                Factory.Dispose();
                Factory = null;
            }
        }

        #endregion
        #region XmlSerializable Members
        #region ConstXmlName

        /// <summary>
        /// The name of the XML element associated with the <see cref="RuleScript"/> class.
        /// </summary>
        /// <remarks>
        /// <b>ConstXmlName</b> holds the value "rules", indicating the XML element in <see
        /// cref="FilePaths.ScenarioSchema"/> whose data is managed by the <see cref="RuleScript"/>
        /// class.</remarks>

        public const string ConstXmlName = "rules";

        #endregion
        #region ReadXmlAttributes

        /// <summary>
        /// Reads XML attribute data into the <see cref="RuleScript"/> object using the specified
        /// <see cref="XmlReader"/>.</summary>
        /// <param name="reader">
        /// The <see cref="XmlReader"/> from which to read.</param>
        /// <exception cref="XmlException">
        /// An error occurred while parsing the XML data provided by <paramref name="reader"/>.
        /// </exception>
        /// <exception cref="System.Xml.Schema.XmlSchemaException">
        /// <paramref name="reader"/> is an <see cref="XmlValidatingReader"/>, and the XML data did
        /// not conform to <see cref="FilePaths.ScenarioSchema"/>.</exception>
        /// <remarks>
        /// Please refer to class <see cref="XmlSerializable"/> for a complete description of this
        /// method.</remarks>

        protected override void ReadXmlAttributes(XmlReader reader) {

            string path = null;
            XmlUtility.ReadAttributeAsString(reader, "href", ref path);
            this._path = this._path.Change(path);
        }

        #endregion
        #region WriteXmlAttributes

        /// <summary>
        /// Writes all current data of the <see cref="RuleScript"/> object that is serialized to XML
        /// attributes to the specified <see cref="XmlWriter"/>.</summary>
        /// <param name="writer">
        /// The <see cref="XmlWriter"/> to which to write.</param>
        /// <remarks>
        /// Please refer to class <see cref="XmlSerializable"/> for a complete description of this
        /// method.</remarks>

        protected override void WriteXmlAttributes(XmlWriter writer) {
            writer.WriteAttributeString("href", Path);
        }

        #endregion
        #endregion
    }
}
