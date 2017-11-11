using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml;

using Tektosyne;
using Tektosyne.Geometry;
using Tektosyne.IO;
using Tektosyne.Windows;
using Tektosyne.Xml;
using Hexkit.Global;

namespace Hexkit.Scenario {

    /// <summary>
    /// Represents a fixed image that is overlayed on the entire map display.</summary>
    /// <remarks><para>
    /// <b>OverlayImage</b> is serialized to the XML element "overlay" defined in <see
    /// cref="FilePaths.ScenarioSchema"/> or <see cref="FilePaths.OptionsSchema"/>, depending on
    /// which of two images it represents:
    /// </para><list type="bullet"><item>
    /// A permanent opaque image that appears behind the map display and replaces transparent
    /// terrain images. This variant is saved to the <see cref="AreaSection"/> XML file.
    /// </item><item>
    /// A temporary image with variable opacity that appears on top of the map display within Hexkit
    /// Editor only. This variant is saved to the Hexkit Editor options file.
    /// </item></list></remarks>

    public sealed class OverlayImage: XmlSerializable, ICloneable {
        #region OverlayImage(Double)

        /// <overloads>
        /// Initializes a new instance of the <see cref="OverlayImage"/> class.</overloads>
        /// <summary>
        /// Initializes a new instance of the <see cref="OverlayImage"/> class with the specified
        /// opacity.</summary>
        /// <param name="opacity">
        /// The initial value for the <see cref="Opacity"/> property.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="opacity"/> is less than zero or greater than one.</exception>

        internal OverlayImage(double opacity) {

            if (opacity < 0.0 || opacity > 1.0)
                ThrowHelper.ThrowArgumentOutOfRangeExceptionWithFormat(
                    "opacity", opacity, Tektosyne.Strings.ArgumentLessOrGreater, 0, 1);

            this._bounds = new RectI(0, 0, 1, 1);
            this._opacity = opacity;
            this._path = FilePaths.CreateCommonPath();
        }

        #endregion
        #region OverlayImage(OverlayImage)

        /// <summary>
        /// Initializes a new instance of the <see cref="OverlayImage"/> class with property values
        /// copied from the specified instance.</summary>
        /// <param name="overlay">
        /// The <see cref="OverlayImage"/> instance whose property values are copied to the new
        /// instance.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="overlay"/> is a null reference.</exception>
        /// <remarks>
        /// This constructor is called by <see cref="Clone"/> to perform a deep copy of the
        /// specified <paramref name="overlay"/>, including a deep copy of all mutable objects that
        /// are owned by the <paramref name="overlay"/>.</remarks>

        public OverlayImage(OverlayImage overlay) {
            if (overlay == null)
                ThrowHelper.ThrowArgumentNullException("overlay");

            this._bitmap = overlay._bitmap;
            this._bounds = overlay._bounds;
            this._opacity = overlay._opacity;
            this._path = (RootedPath) overlay._path.Clone();
            this._preserveAspectRatio = overlay._preserveAspectRatio;
        }

        #endregion
        #region Private Fields

        // property backers
        private BitmapSource _bitmap;
        private RectI _bounds;
        private double _opacity;
        private RootedPath _path;
        private bool _preserveAspectRatio;

        #endregion
        #region AspectRatio

        /// <summary>
        /// Gets the original aspect ratio of the associated <see cref="Bitmap"/>.</summary>
        /// <value><para>
        /// The <see cref="BitmapSource.PixelWidth"/> of the associated <see cref="Bitmap"/>,
        /// divided by its <see cref="BitmapSource.PixelHeight"/>.
        /// </para><para>-or-</para><para>
        /// Zero if <see cref="Bitmap"/> is a null reference, or its <see
        /// cref="BitmapSource.PixelHeight"/> is zero.</para></value>

        public double AspectRatio {
            get {
                var bitmap = Bitmap;
                if (bitmap == null) return 0.0;

                int pixelHeight = bitmap.PixelHeight;
                if (pixelHeight > 0)
                    return bitmap.PixelWidth / (double) pixelHeight;

                return 0.0;
            }
        }

        #endregion
        #region Bitmap

        /// <summary>
        /// Gets the <see cref="BitmapSource"/> created from the file located at <see cref="Path"/>.
        /// </summary>
        /// <value>
        /// The <see cref="BitmapSource"/> created from the file located at <see cref="Path"/>. The
        /// default is a null reference.</value>
        /// <remarks><para>
        /// <b>Bitmap</b> holds a null reference until <see cref="Load"/> was successfully invoked
        /// on the <see cref="OverlayImage"/>, and again whenever <see cref="Path"/> has changed.
        /// </para><para>
        /// <b>Bitmap</b> always uses the <see cref="PixelFormats.Pbgra32"/> format, regardless of
        /// the format of the file located at <see cref="Path"/>.</para></remarks>

        public BitmapSource Bitmap {
            [DebuggerStepThrough]
            get { return this._bitmap; }
        }

        #endregion
        #region Bounds

        /// <summary>
        /// Gets or sets the bounds of the <see cref="OverlayImage"/>.</summary>
        /// <value>
        /// A <see cref="RectI"/> indicating bounds of the <see cref="OverlayImage"/>, relative to
        /// the entire map display area. The default is a <see cref="RectI.Location"/> of (0,0) and
        /// a <see cref="RectI.Size"/> of (1,1).</value>
        /// <exception cref="ArgumentException">
        /// The property is set to a value whose <see cref="RectI.Width"/> or <see
        /// cref="RectI.Height"/> is equal to or less than zero.</exception>
        /// <exception cref="InvalidOperationException">
        /// The property is set, and <see cref="ApplicationInfo.IsEditor"/> is <c>false</c>.
        /// </exception>
        /// <remarks>
        /// <b>Bounds</b> holds the value of the "bounds" XML element. The coordinates are relative
        /// to the entire map display area, comprising the current <see cref="AreaSection.MapGrid"/>
        /// plus any surrounding borders.</remarks>

        public RectI Bounds {
            [DebuggerStepThrough]
            get { return this._bounds; }
            set {
                ApplicationInfo.CheckEditor();

                if (value.Width <= 0)
                    ThrowHelper.ThrowArgumentException(
                        "value.Width", Tektosyne.Strings.ArgumentNotPositive);

                if (value.Height <= 0)
                    ThrowHelper.ThrowArgumentException(
                        "value.Height", Tektosyne.Strings.ArgumentNotPositive);

                this._bounds = value;
            }
        }

        #endregion
        #region IsEmpty

        /// <summary>
        /// Gets a value indicating whether the current <see cref="Path"/> is empty.</summary>
        /// <value>
        /// <c>true</c> if <see cref="Path"/> is an empty string; otherwise, <c>false</c>.</value>
        /// <remarks>
        /// If <b>IsEmpty</b> is <c>true</c>, the <see cref="OverlayImage"/> has no visual effect
        /// and is not serialized to the associated XML file.</remarks>

        public bool IsEmpty {
            [DebuggerStepThrough]
            get { return this._path.IsEmpty; }
        }

        #endregion
        #region Opacity

        /// <summary>
        /// Gets or sets the opacity of the <see cref="OverlayImage"/>.</summary>
        /// <value>
        /// The opacity of the <see cref="OverlayImage"/>, expressed as a value within the closed
        /// interval [0, 1].</value>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The property is set to a value that is less than zero or greater than one.</exception>
        /// <exception cref="InvalidOperationException">
        /// The property is set, and <see cref="ApplicationInfo.IsEditor"/> is <c>false</c>.
        /// </exception>
        /// <remarks>
        /// <b>Opacity</b> holds the value of the "opacity" XML attribute.</remarks>

        public double Opacity {
            [DebuggerStepThrough]
            get { return this._opacity; }
            set {
                ApplicationInfo.CheckEditor();

                if (value < 0.0 || value > 1.0)
                    ThrowHelper.ThrowArgumentOutOfRangeExceptionWithFormat(
                        "value", value, Tektosyne.Strings.ArgumentLessOrGreater, 0, 1);

                this._opacity = value;
            }
        }

        #endregion
        #region Path

        /// <summary>
        /// Gets or sets the file path for the <see cref="OverlayImage"/>.</summary>
        /// <value>
        /// The file path for the <see cref="OverlayImage"/>, relative to the current root folder
        /// for Hexkit user files. The default is an empty string.</value>
        /// <exception cref="InvalidOperationException">
        /// The property is set, and <see cref="ApplicationInfo.IsEditor"/> is <c>false</c>.
        /// </exception>
        /// <remarks><para>
        /// <b>Path</b> returns an empty string when set to a null reference. This property holds
        /// the value of the "href" XML attribute.
        /// </para><para>
        /// Changing <b>Path</b> sets the <see cref="Bitmap"/> property to a null reference. Clients
        /// must explicitly call <see cref="Load"/> to load the new <see cref="Bitmap"/>.
        /// </para></remarks>

        public string Path {
            get { return this._path.RelativePath; }
            set {
                ApplicationInfo.CheckEditor();
                this._path = this._path.Change(value);
                this._bitmap = null;
            }
        }

        #endregion
        #region PreserveAspectRatio

        /// <summary>
        /// Gets or sets a value indicating whether the original <see cref="AspectRatio"/> should be
        /// preserved.</summary>
        /// <value>
        /// <c>true</c> to preserve the <see cref="AspectRatio"/> when changing the size of the <see
        /// cref="Bounds"/>; otherwise, <c>false</c>. The default is <c>false</c>.</value>
        /// <exception cref="InvalidOperationException">
        /// The property is set, and <see cref="ApplicationInfo.IsEditor"/> is <c>false</c>.
        /// </exception>
        /// <remarks><para>
        /// <b>PreserveAspectRatio</b> holds the value of the "preserveAspectRatio" XML attribute.
        /// </para><para>
        /// Setting this property to <c>true</c> has no immediate effect on the current <see
        /// cref="Bounds"/> and does not constrain future changes to the <see cref="Bounds"/>
        /// property. Clients must examine <b>PreserveAspectRatio</b> and apply the appropriate
        /// constraints manually.</para></remarks>

        public bool PreserveAspectRatio {
            [DebuggerStepThrough]
            get { return this._preserveAspectRatio; }
            set {
                ApplicationInfo.CheckEditor();
                this._preserveAspectRatio = value;
            }
        }

        #endregion
        #region Load

        /// <summary>
        /// Attempts to load the <see cref="Bitmap"/> located at <see cref="Path"/>.</summary>
        /// <param name="owner">
        /// The parent <see cref="Window"/> for any dialogs.</param>
        /// <returns>
        /// <c>true</c> if <see cref="Bitmap"/> is valid; otherwise, <c>false</c>.</returns>
        /// <remarks><para>
        /// <b>Load</b> returns <c>false</c> and sets the <see cref="Bitmap"/> property to a null
        /// reference if <see cref="IsEmpty"/> is <c>true</c> or an error occurred during loading.
        /// </para><para>
        /// On error, <b>Load</b> also clears the <see cref="Path"/> property and informs the user.
        /// No exceptions are propagated to the caller.</para></remarks>

        public bool Load(Window owner) {

            if (IsEmpty) {
                this._bitmap = null;
                return false;
            }

            try {
                // create bitmap from specified path
                Uri source = new Uri(Path, UriKind.RelativeOrAbsolute);
                this._bitmap = new BitmapImage(source);

                // convert bitmap to standard color format
                // (necessary to prevent exceptions during WPF composition)
                this._bitmap = new FormatConvertedBitmap(
                    this._bitmap, PixelFormats.Pbgra32, null, 0.0);

                this._bitmap.Freeze();
                return true;
            }
            catch (Exception e) {
                if (this._bitmap != null)
                    this._bitmap = null;

                Mouse.OverrideCursor = null;
                MessageDialog.Show(owner,
                    String.Format(ApplicationInfo.Culture, Global.Strings.ErrorImageFileOpen, Path),
                    Global.Strings.TitleImageError, e, MessageBoxButton.OK, Global.Images.Error);

                this._path.Clear();
                return false;
            }
        }

        #endregion
        #region ICloneable Members

        /// <summary>
        /// Creates a new <see cref="OverlayImage"/> object that is a deep copy of the current
        /// instance.</summary>
        /// <returns>
        /// A new <see cref="OverlayImage"/> object that is a deep copy of the current instance.
        /// </returns>
        /// <remarks>
        /// <b>Clone</b> calls the <see cref="OverlayImage(OverlayImage)"/> copy constructor with
        /// this <see cref="OverlayImage"/> object.</remarks>

        public object Clone() {
            return new OverlayImage(this);
        }

        #endregion
        #region XmlSerializable Members
        #region ConstXmlName

        /// <summary>
        /// The name of the XML element associated with the <see cref="OverlayImage"/> class.
        /// </summary>
        /// <remarks>
        /// <b>ConstXmlName</b> holds the value "overlay", indicating the XML element in <see
        /// cref="FilePaths.ScenarioSchema"/> whose data is managed by the <see
        /// cref="OverlayImage"/> class.</remarks>

        public const string ConstXmlName = "overlay";

        #endregion
        #region ReadXmlAttributes

        /// <summary>
        /// Reads XML attribute data into the <see cref="OverlayImage"/> object using the specified
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
            reader.ReadAttributeAsString("href", ref path);
            this._path = this._path.Change(path);

            reader.ReadAttributeAsDouble("opacity", ref this._opacity);
            reader.ReadAttributeAsBoolean("preserveAspectRatio", ref this._preserveAspectRatio);
        }

        #endregion
        #region ReadXmlElements

        /// <summary>
        /// Reads XML element data into the <see cref="OverlayImage"/> object using the specified
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
        /// <remarks>
        /// Please refer to class <see cref="XmlSerializable"/> for a complete description of this
        /// method.</remarks>

        protected override bool ReadXmlElements(XmlReader reader) {
            switch (reader.Name) {

                case "bounds":
                    this._bounds = SimpleXml.ReadRectI(reader);
                    return true;

                default: return false;
            }
        }

        #endregion
        #region WriteXmlAttributes

        /// <summary>
        /// Writes all current data of the <see cref="OverlayImage"/> object that is serialized to
        /// XML attributes to the specified <see cref="XmlWriter"/>.</summary>
        /// <param name="writer">
        /// The <see cref="XmlWriter"/> to which to write.</param>
        /// <remarks>
        /// Please refer to class <see cref="XmlSerializable"/> for a complete description of this
        /// method.</remarks>

        protected override void WriteXmlAttributes(XmlWriter writer) {

            writer.WriteAttributeString("href", Path);
            if (Opacity != 1.0)
                writer.WriteAttributeString("opacity", XmlConvert.ToString(Opacity));

            if (PreserveAspectRatio)
                writer.WriteAttributeString("preserveAspectRatio",
                    XmlConvert.ToString(PreserveAspectRatio));
        }

        #endregion
        #region WriteXmlElements

        /// <summary>
        /// Writes all current data of the <see cref="OverlayImage"/> object that is serialized to
        /// nested XML elements to the specified <see cref="XmlWriter"/>.</summary>
        /// <param name="writer">
        /// The <see cref="XmlWriter"/> to which to write.</param>
        /// <remarks>
        /// Please refer to class <see cref="XmlSerializable"/> for a complete description of this
        /// method.</remarks>

        protected override void WriteXmlElements(XmlWriter writer) {

            writer.WriteStartElement("bounds");
            SimpleXml.WriteRectI(writer, Bounds);
            writer.WriteEndElement();
        }

        #endregion
        #endregion
    }
}
