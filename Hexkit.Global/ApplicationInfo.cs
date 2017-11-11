using System;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Xml;

using Tektosyne;

namespace Hexkit.Global {

    /// <summary>
    /// Provides information about the application.</summary>

    public static class ApplicationInfo {
        #region Private Fields

        // property backers
        private static CultureInfo _culture;
        private readonly static bool _isEditor =
            (ConfigurationManager.AppSettings["Application"] == "Editor");

        // public key for Kynosarges
        private readonly static byte[] _publicKey = {
              0,  36,   0,   0,   4, 128,   0,   0, 148,   0,   0,   0,
              6,   2,   0,   0,   0,  36,   0,   0,  82,  83,  65,  49,
              0,   4,   0,   0,   1,   0,   1,   0, 165, 117, 251,  22,
             83,   2,  42, 251,  80,  84,  59, 167, 219,  21, 154, 156,
            151, 248, 211, 224, 216,  92, 202,  74, 229,  92,  62, 195,
            141, 120, 227,  12, 163,  52, 167, 236, 164, 182, 193,  88,
            166, 217,  52, 233,  75, 162, 188, 233,  69,  47,   8,  84,
            201, 255,  69, 207, 250,  70,  50, 123, 201,  51,  52,  87,
            218, 112, 207, 248, 148, 248, 225,  61, 202, 159,  74, 196,
            107, 243,  66, 235, 143, 251, 167, 131,  38, 254,  11, 133,
             72,  21, 198, 164, 234, 133,   2, 147, 150,  31, 253, 112,
             43, 158, 122, 210, 132, 237,  92, 161,  97, 219,   7,  44,
            159,  22, 134, 181, 208,  65,  63,  16, 232, 104, 254,  65,
             33,  92,  79, 221 };

        #endregion
        #region Company

        /// <summary>
        /// Gets the company name for the application.</summary>
        /// <value>
        /// The value of <see cref="AssemblyExtensions.Company"/> for the application.</value>

        public static string Company {
            get { return Assembly.GetExecutingAssembly().Company(); }
        }

        #endregion
        #region Culture

        /// <summary>
        /// Gets the user-specific <see cref="CultureInfo"/> for the application.</summary>
        /// <value>
        /// The value of the <see cref="CultureInfo.CurrentCulture"/> property.</value>
        /// <remarks>
        /// <b>Culture</b> caches the value of the <see cref="CultureInfo.CurrentCulture"/> property
        /// when first accessed, and then returns the cached value on subsequent accesses.</remarks>

        public static CultureInfo Culture {
            get {
                if (ApplicationInfo._culture == null)
                    ApplicationInfo._culture = CultureInfo.CurrentCulture;

                return ApplicationInfo._culture;
            }
        }

        #endregion
        #region Email

        /// <summary>
        /// Gets the contact e-mail address for the application.</summary>
        /// <value>
        /// The e-mail address that should be used to contact the developer of the application.
        /// </value>

        public static string Email {
            [DebuggerStepThrough]
            get { return "christoph.nahr@kynosarges.org"; }
        }

        #endregion
        #region IsEditor

        /// <summary>
        /// Gets a value indicating whether Hexit Editor is running.</summary>
        /// <value>
        /// <c>true</c> if the <see cref="ConfigurationSettings.AppSettings"/> collection associates
        /// the "Application" key with a value of "Editor"; otherwise, <c>false</c>.</value>
        /// <remarks>
        /// Call <see cref="CheckEditor"/> to raise an exception if Hexkit Editor is not running.
        /// </remarks>

        public static bool IsEditor {
            [DebuggerStepThrough]
            get { return ApplicationInfo._isEditor; }
        }

        #endregion
        #region IsSigned

        /// <summary>
        /// Gets a value indicating whether the application is signed with the Kynosarges key.
        /// </summary>
        /// <value>
        /// <c>true</c> if the application is signed with the public key of the Kynosarges domain;
        /// otherwise, <c>false</c>.</value>

        public static bool IsSigned {
            get {
                // get public key of entry assembly, if present
                AssemblyName name = Assembly.GetEntryAssembly().GetName();
                byte[] publicKey = name.GetPublicKey();
                if (publicKey == null) return false;

                // compare to public key of Kynosarges
                if (publicKey.Length != _publicKey.Length)
                    return false;

                for (int i = 0; i < _publicKey.Length; i++)
                    if (publicKey[i] != ApplicationInfo._publicKey[i])
                        return false;

                return true;
            }
        }

        #endregion
        #region Product

        /// <summary>
        /// Gets the product name for the application.</summary>
        /// <value>
        /// The value of <see cref="AssemblyExtensions.Product"/> for the application.</value>

        public static string Product {
            get { return Assembly.GetExecutingAssembly().Product(); }
        }

        #endregion
        #region PublicKeyToken

        /// <summary>
        /// Gets the public key token for the application.</summary>
        /// <value>
        /// The value of <see cref="AssemblyExtensions.PublicKeyToken"/> for the application.
        /// </value>
        /// <remarks>
        /// <b>PublicKeyToken</b> returns a null reference if the application is not signed with a
        /// strong name.</remarks>

        public static string PublicKeyToken {
            get { return Assembly.GetExecutingAssembly().PublicKeyToken(); }
        }

        #endregion
        #region Signature

        /// <summary>
        /// Gets the application signature.</summary>
        /// <value>
        /// The concatenated values of <see cref="Title"/> and <see cref="Version"/>, separated by
        /// a space character.</value>

        public static string Signature {
            get { return String.Concat(Title, " ", Version); }
        }

        #endregion
        #region Title

        /// <summary>
        /// Gets the application title.</summary>
        /// <value>
        /// The value of <see cref="Product"/>, concatenated with the literal string “ Editor” if
        /// <see cref="IsEditor"/> is <c>true</c>; otherwise, the literal string “ Game”.</value>

        public static string Title {
            [DebuggerStepThrough]
            get { return String.Concat(Product, (IsEditor ? " Editor" : " Game")); }
        }

        #endregion
        #region Version

        /// <summary>
        /// Gets the version number for the application.</summary>
        /// <value>
        /// The value of <see cref="AssemblyExtensions.InformationalVersion"/> for the application.
        /// </value>

        public static string Version {
            get { return Assembly.GetExecutingAssembly().InformationalVersion(); }
        }

        #endregion
        #region Website

        /// <summary>
        /// Gets the product website for the application.</summary>
        /// <value>
        /// The URL of the website where information and updates for the application can be found.
        /// </value>

        public static string Website {
            [DebuggerStepThrough]
            get { return "http://www.kynosarges.org/"; }
        }

        #endregion
        #region CheckEditor

        /// <summary>
        /// Throws an exception if <see cref="IsEditor"/> is <c>false</c>.</summary>
        /// <exception cref="InvalidOperationException">
        /// <see cref="IsEditor"/> is <c>false</c>.</exception>
        /// <remarks>
        /// Call <b>CheckEditor</b> before executing an operation that is only valid within Hexkit
        /// Editor, such as changing scenario data from without the <b>Scenario</b> assembly.
        /// </remarks>

        public static void CheckEditor() {
            if (!IsEditor)
                ThrowHelper.ThrowInvalidOperationException(Global.Strings.ErrorEditorOnly);
        }

        #endregion
        #region WriteXml

        /// <summary>
        /// Writes an "application" XML element to the specified <see cref="XmlWriter"/>.</summary>
        /// <param name="writer">
        /// The <see cref="XmlWriter"/> to which to write.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="writer"/> is a null reference.</exception>
        /// <exception cref="XmlException">
        /// An error occurred while writing to the specified <paramref name="writer"/>.</exception>
        /// <remarks><para>
        /// <b>WriteXml</b> writes an empty XML element named "application" to the specified
        /// <paramref name="writer"/>, with two attributes named "product" and "version" containing
        /// the values of the <see cref="Title"/> and <see cref="Version"/> properties,
        /// respectively.
        /// </para><para>
        /// The resulting XML fragment conforms to the "application" XML element defined identically
        /// in all Hexkit XML schemas.</para></remarks>

        public static void WriteXml(XmlWriter writer) {
            if (writer == null)
                ThrowHelper.ThrowArgumentNullException("writer");

            writer.WriteStartElement("application");
            writer.WriteAttributeString("product", Title);
            writer.WriteAttributeString("version", Version);
            writer.WriteEndElement();
        }

        #endregion
    }
}
