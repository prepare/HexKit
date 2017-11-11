using System;
using System.Diagnostics;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml;

using Tektosyne;
using Tektosyne.Collections;
using Tektosyne.IO;
using Tektosyne.Windows;
using Tektosyne.Xml;
using Hexkit.Global;

namespace Hexkit.Scenario {

    /// <summary>
    /// Manages the bitmap data of an image file.</summary>
    /// <remarks>
    /// <b>ImageFile</b> is serialized to the XML element "file" defined in <see
    /// cref="FilePaths.ScenarioSchema"/>.</remarks>

    public sealed class ImageFile: ScenarioElement, IDisposable, IMutableKeyedValue<String> {
        #region ImageFile()

        /// <overloads>
        /// Initializes a new instance of the <see cref="ImageFile"/> class.</overloads>
        /// <summary>
        /// Initializes a new instance of the <see cref="ImageFile"/> class with default properties.
        /// </summary>

        internal ImageFile() { }

        #endregion
        #region ImageFile(String, String)

        /// <summary>
        /// Initializes a new instance of the <see cref="ImageFile"/> class with the specified
        /// identifier and file path.</summary>
        /// <param name="id">
        /// The initial value for the <see cref="Id"/> property.</param>
        /// <param name="path">
        /// The initial value for the <see cref="Path"/> property.</param>
        /// <exception cref="ArgumentNullOrEmptyException">
        /// <paramref name="id"/> is a null reference or an empty string.</exception>

        public ImageFile(string id, string path) {
            if (String.IsNullOrEmpty(id))
                ThrowHelper.ThrowArgumentNullOrEmptyException("id");

            this._id = String.Intern(id);
            this._path = FilePaths.CreateCommonPath(path);
        }

        #endregion
        #region Private Fields

        // property backers
        private string _id;
        private RootedPath _path = FilePaths.CreateCommonPath();
        private WriteableBitmap _bitmap;

        #endregion
        #region Bitmap

        /// <summary>
        /// Gets the <see cref="WriteableBitmap"/> created from the file located at <see
        /// cref="Path"/>.</summary>
        /// <value>
        /// The <see cref="WriteableBitmap"/> created from the file located at <see cref="Path"/>.
        /// </value>
        /// <remarks><para>
        /// <b>Bitmap</b> holds a null reference until <see cref="Load"/> was successfully invoked
        /// on the <see cref="ImageFile"/>, and again after <see cref="Unload"/> or <see
        /// cref="Dispose"/> have been called.
        /// </para><para>
        /// <b>Bitmap</b> always uses the default resolution of 96 dpi in both dimensions and the
        /// <see cref="PixelFormats.Pbgra32"/> format, regardless of the resolution and format of
        /// the file located at <see cref="Path"/>.</para></remarks>

        public WriteableBitmap Bitmap {
            [DebuggerStepThrough]
            get { return this._bitmap; }
        }

        #endregion
        #region Id

        /// <summary>
        /// Gets or sets the identifier of the <see cref="ImageFile"/>.</summary>
        /// <value>
        /// The identifier of the <see cref="ImageFile"/>. The default is an empty string.</value>
        /// <exception cref="InvalidOperationException">
        /// The property is set, and <see cref="ApplicationInfo.IsEditor"/> is <c>false</c>.
        /// </exception>
        /// <remarks>
        /// <b>Id</b> returns an empty string when set to a null reference. This property holds the
        /// value of the "id" XML attribute which must be unique among all identifiers defined by
        /// the scenario.</remarks>

        public string Id {
            [DebuggerStepThrough]
            get { return this._id ?? ""; }
            [DebuggerStepThrough]
            set {
                ApplicationInfo.CheckEditor();
                if (value != null) value = String.Intern(value);
                this._id = value;
            }
        }

        #endregion
        #region Path

        /// <summary>
        /// Gets or sets the path to the physical image file.</summary>
        /// <value>
        /// The path to the physical image file, relative to the current root folder for Hexkit user
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
        #region Load

        /// <summary>
        /// Loads the <see cref="Bitmap"/> from the file located at <see cref="Path"/>, using the
        /// specified masking color.</summary>
        /// <param name="mask">
        /// A <see cref="Color"/> value indicating the color to make transparent, or <see
        /// cref="Colors.Transparent"/> to use only the transparency information embedded in the
        /// image file.</param>
        /// <exception cref="DetailException">
        /// An error occurred while loading the image file. All exceptions are converted to a
        /// <b>DetailException</b> whose <see cref="DetailException.Detail"/> property holds the
        /// message text provided by the original exception.</exception>
        /// <remarks><para>
        /// <b>Load</b> attempts to open the physical image file located at <see cref="Path"/>. On
        /// success, the <see cref="Bitmap"/> property is set to a new <see cref="WriteableBitmap"/>
        /// created from the <see cref="BitmapImage"/> that was loaded from <see cref="Path"/>.
        /// </para><para>
        /// If <paramref name="mask"/> does not equal <see cref="Colors.Transparent"/>, <b>Load</b>
        /// also calls <see cref="BitmapUtility.MakeTransparent"/> with the specified <paramref
        /// name="mask"/> on the new <b>Bitmap</b>. Any transparency information embedded in the
        /// image file is always in effect, regardless of the value of <paramref name="mask"/>.
        /// </para></remarks>

        public void Load(Color mask) {
            string path = this._path.AbsolutePath;

            try {
                // create file bitmap from specified path
                Uri source = new Uri(path, UriKind.RelativeOrAbsolute);
                BitmapSource fileBitmap = new BitmapImage(source);

                // convert file bitmap to standard color format
                fileBitmap = new FormatConvertedBitmap(fileBitmap, PixelFormats.Pbgra32, null, 0.0);

                // create writeable bitmap in standard format
                this._bitmap = new WriteableBitmap(
                    fileBitmap.PixelWidth, fileBitmap.PixelHeight,
                    96, 96, PixelFormats.Pbgra32, null);

                // copy file bitmap to writeable bitmap
                this._bitmap.Lock();
                this._bitmap.Read(0, 0, fileBitmap);

                // making masking color transparent
                if (mask != Colors.Transparent)
                    this._bitmap.MakeTransparent(mask);

                this._bitmap.Unlock();
                this._bitmap.Freeze();
            }
            catch (Exception e) {
                if (this._bitmap != null)
                    this._bitmap = null;

                // rethrow exception as DetailException
                ThrowHelper.ThrowDetailExceptionWithFormat(
                    Global.Strings.ErrorImageFileOpen, path, e);
            }
        }

        #endregion
        #region ToString

        /// <summary>
        /// Returns a <see cref="String"/> that represents the <see cref="ImageFile"/>.</summary>
        /// <returns>
        /// The value of the <see cref="Id"/> property.</returns>

        public override string ToString() {
            return Id;
        }

        #endregion
        #region Unload

        /// <summary>
        /// Unloads the current <see cref="Bitmap"/>.</summary>
        /// <remarks><para>
        /// <b>Unload</b> sets the <see cref="Bitmap"/> property to a null reference. It is safe to
        /// call <b>Unload</b> repeatedly.
        /// </para><para>
        /// The <see cref="WriteableBitmap"/> does not require explicit disposal but releasing the
        /// object reference should considerably reduce the application's memory consumption.
        /// </para></remarks>

        public void Unload() {
            this._bitmap = null;
        }

        #endregion
        #region IDisposable Members

        /// <summary>
        /// Releases all resources used by the <see cref="ImageFile"/> object.</summary>
        /// <remarks>
        /// <b>Dispose</b> invokes <see cref="Unload"/> to release the memory allocated for the 
        /// current <see cref="Bitmap"/>.</remarks>

        public void Dispose() { Unload(); }

        #endregion
        #region IMutableKeyedValue Members

        /// <summary>
        /// Gets the identifier of the <see cref="ImageFile"/>.</summary>
        /// <value>
        /// The value of the <see cref="Id"/> property.</value>

        string IKeyedValue<String>.Key {
            [DebuggerStepThrough]
            get { return Id; }
        }

        /// <summary>
        /// Sets the identifier of the <see cref="ImageFile"/>.</summary>
        /// <param name="key">
        /// The new value for the <see cref="Id"/> property.</param>
        /// <exception cref="InvalidOperationException">
        /// <see cref="ApplicationInfo.IsEditor"/> is <c>false</c>.</exception>

        void IMutableKeyedValue<String>.SetKey(string key) {
            Id = key;
        }

        #endregion
        #region XmlSerializable Members
        #region ConstXmlName

        /// <summary>
        /// The name of the XML element associated with the <see cref="ImageFile"/> class.
        /// </summary>
        /// <remarks>
        /// <b>ConstXmlName</b> holds the value "file", indicating the XML element in <see
        /// cref="FilePaths.ScenarioSchema"/> whose data is managed by the <see cref="ImageFile"/>
        /// class.</remarks>

        public const string ConstXmlName = "file";

        #endregion
        #region ReadXmlAttributes

        /// <summary>
        /// Reads XML attribute data into the <see cref="ImageFile"/> object using the specified
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
            XmlUtility.ReadAttributeAsString(reader, "id", ref this._id);

            string path = null;
            XmlUtility.ReadAttributeAsString(reader, "href", ref path);
            this._path = this._path.Change(path);
        }

        #endregion
        #region WriteXmlAttributes

        /// <summary>
        /// Writes all current data of the <see cref="ImageFile"/> object that is serialized to XML
        /// attributes to the specified <see cref="XmlWriter"/>.</summary>
        /// <param name="writer">
        /// The <see cref="XmlWriter"/> to which to write.</param>
        /// <remarks>
        /// Please refer to class <see cref="XmlSerializable"/> for a complete description of this
        /// method.</remarks>

        protected override void WriteXmlAttributes(XmlWriter writer) {

            writer.WriteAttributeString("id", Id);
            writer.WriteAttributeString("href", Path);
        }

        #endregion
        #endregion
    }
}
