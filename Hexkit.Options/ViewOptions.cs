using System;
using System.Diagnostics;
using System.Windows;
using System.Xml;

using Tektosyne;
using Tektosyne.Collections;
using Tektosyne.Geometry;
using Tektosyne.Windows;
using Tektosyne.Xml;

using Hexkit.Global;
using Hexkit.Scenario;

using Rectangle = System.Drawing.Rectangle;
using SystemInformation = System.Windows.Forms.SystemInformation;

namespace Hexkit.Options {
    #region Type Aliases

    using DesktopOptionsDictionary = DictionaryEx<Rectangle, DesktopOptions>;
    using MapViewOptionsDictionary = DictionaryEx<String, MapViewOptions>;
    using ScaleList = ListEx<Int32>;

    #endregion

    /// <summary>
    /// Manages all user settings related to visual appearance.</summary>
    /// <remarks>
    /// <b>ViewOptions</b> is serialized to the XML element "view" defined in <see
    /// cref="FilePaths.OptionsSchema"/>.</remarks>

    public sealed class ViewOptions: XmlSerializable {
        #region ViewOptions(EventHandler)

        /// <summary>
        /// Initializes a new instance of the <see cref="ViewOptions"/> class with the specified
        /// event handler.</summary>
        /// <param name="onOptionsChanged">
        /// An <see cref="EventHandler"/> to be invoked whenever an option managed by the new <see
        /// cref="ViewOptions"/> instance changes.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="onOptionsChanged"/> is a null reference.</exception>
        /// <remarks><para>
        /// The following table shows the initial property values for the new instance of <see
        /// cref="ViewOptions"/>, where not specifically noted in the property description:
        /// </para><list type="table"><listheader>
        /// <term>Property</term><description>Value</description>
        /// </listheader><item>
        /// <term><see cref="Desktops"/></term>
        /// <description>A single <see cref="DesktopOptions"/> object initialized with the value
        /// returned by <see cref="SystemInformation.VirtualScreen"/>.</description>
        /// </item><item>
        /// <term><see cref="MapScales"/></term>
        /// <description>The seven <see cref="Int32"/> values 40, 60, 80, 100, 125, 160, 200.
        /// </description></item><item>
        /// <term><see cref="MapViews"/></term>
        /// <description>An empty collection. Use <see cref="LoadMapView"/> to add new elements with
        /// default property values.</description>
        /// </item></list></remarks>

        internal ViewOptions(EventHandler onOptionsChanged) {
            if (onOptionsChanged == null)
                ThrowHelper.ThrowArgumentNullException("onOptionsChanged");

            this._onOptionsChanged = onOptionsChanged;

            // create default window bounds
            Rectangle screen = SystemInformation.VirtualScreen;
            DesktopOptions desktop = new DesktopOptions(screen);
            this._desktops.Add(desktop.VirtualScreen, desktop);

            // create default map scales
            this._mapScales.Add(40);
            this._mapScales.Add(60);
            this._mapScales.Add(80);
            this._mapScales.Add(100);
            this._mapScales.Add(125);
            this._mapScales.Add(160);
            this._mapScales.Add(200);
        }

        #endregion
        #region Private Fields

        // event to raise when an option has changed
        private readonly EventHandler _onOptionsChanged;

        // property backers
        private bool _bitmapGrid, _opaqueImages, _staticArrows, _staticMarker;
        private DefaultTheme _theme = DefaultTheme.System;
        private readonly ScaleList _mapScales = new ScaleList(true);
        private readonly DesktopOptionsDictionary _desktops = new DesktopOptionsDictionary();
        private readonly MapViewOptionsDictionary _mapViews = new MapViewOptionsDictionary();

        #endregion
        #region BitmapGrid

        /// <summary>
        /// Gets or sets a value indicating whether grid outlines are drawn using bitmaps.</summary>
        /// <value>
        /// <c>true</c> to draw the grid outlines requested by <see cref="MapViewOptions.ShowGrid"/>
        /// using fast but inaccurate bitmaps; <c>false</c> to use slow but accurate WPF drawing
        /// commands. The default is <c>false</c>.</value>
        /// <remarks><para>
        /// <b>BitmapGrid</b> holds the value of the "bitmapGrid" XML attribute.
        /// </para><para>
        /// Setting this property calls <see cref="ApplicationOptions.OnOptionsChanged"/> to save
        /// the current settings to the options file.</para></remarks>

        public bool BitmapGrid {
            [DebuggerStepThrough]
            get { return this._bitmapGrid; }
            set {
                this._bitmapGrid = value;
                this._onOptionsChanged(this, EventArgs.Empty);
            }
        }

        #endregion
        #region Desktops

        /// <summary>
        /// Gets a list of all user settings related to specific desktop sizes.</summary>
        /// <value>
        /// A read-only <see cref="DesktopOptionsDictionary"/> that maps <see
        /// cref="DesktopOptions.VirtualScreen"/> values to the corresponding <see
        /// cref="DesktopOptions"/> objects.</value>
        /// <remarks><para>
        /// <b>Desktops</b> never returns a null reference, and its <see
        /// cref="DesktopOptionsDictionary.Values"/> are never null references.
        /// </para><para>
        /// Settings for a given desktop size are stored with a <see cref="RectI"/> key that equals
        /// the corresponding value of the <see cref="SystemInformation.VirtualScreen"/> property of
        /// the <see cref="SystemInformation"/> class.
        /// </para><para>
        /// Use <see cref="LoadDesktop"/> to automatically create a new <see cref="DesktopOptions"/>
        /// object with default properties for nonexistent screen dimensions, and <see
        /// cref="SaveDesktop"/> to store specific <see cref="DesktopOptions"/>.</para></remarks>

        public DesktopOptionsDictionary Desktops {
            [DebuggerStepThrough]
            get { return this._desktops.AsReadOnly(); }
        }

        #endregion
        #region MapScales

        /// <summary>
        /// Gets a list of all possible map view scales.</summary>
        /// <value>
        /// A read-only <see cref="ScaleList"/> whose elements indicate the percentages by which to
        /// scale the display of a map view.</value>
        /// <remarks><para>
        /// <b>MapScales</b> never returns a null reference. All elements are unique, and also 
        /// conform to the following restrictions:
        /// </para><list type="bullet"><item>
        /// The values are sorted in ascending order.
        /// </item><item>
        /// There is exactly one entry whose value is 100, indicating an unscaled map view.
        /// </item><item>
        /// There is exactly one entry per unique <see cref="MapViewOptions.Scale"/> value among all
        /// <see cref="MapViews"/> elements.</item></list></remarks>

        public ScaleList MapScales {
            [DebuggerStepThrough]
            get { return this._mapScales.AsReadOnly(); }
        }

        #endregion
        #region MapViews

        /// <summary>
        /// Gets a list of all user settings related to specific map views.</summary>
        /// <value>
        /// A read-only <see cref="MapViewOptionsDictionary"/> that maps <see
        /// cref="MapViewOptions.Id"/> values to the corresponding <see cref="MapViewOptions"/>
        /// objects.</value>
        /// <remarks><para>
        /// <b>MapViews</b> never returns a null reference, and its <see
        /// cref="MapViewOptionsDictionary.Values"/> are never null references.
        /// </para><para>
        /// Settings for a given map view are stored with a <see cref="String"/> key that equals the
        /// value of its identifier.
        /// </para><para>
        /// Use <see cref="LoadMapView"/> to automatically create a new <see cref="MapViewOptions"/>
        /// object with default properties for a nonexistent identifier, and <see
        /// cref="SaveMapView"/> to store specific <see cref="MapViewOptions"/>.</para></remarks>

        public MapViewOptionsDictionary MapViews {
            [DebuggerStepThrough]
            get { return this._mapViews.AsReadOnly(); }
        }

        #endregion
        #region OpaqueImages

        /// <summary>
        /// Gets or sets a value indicating whether images are considered opaque by default.
        /// </summary>
        /// <value>
        /// <c>true</c> to consider the image stack of any <see cref="EntityClass"/> without the
        /// <see cref="EntityClass.IsTranslucent"/> flag as fully opaque; <c>false</c> to ignore
        /// this flag and always use alpha blending. The default is <c>false</c>.</value>
        /// <remarks><para>
        /// <b>OpaqueImages</b> holds the value of the "opaqueImages" XML attribute.
        /// </para><para>
        /// Setting this property calls <see cref="ApplicationOptions.OnOptionsChanged"/> to save
        /// the current settings to the options file.</para></remarks>

        public bool OpaqueImages {
            [DebuggerStepThrough]
            get { return this._opaqueImages; }
            set {
                this._opaqueImages = value;
                this._onOptionsChanged(this, EventArgs.Empty);
            }
        }

        #endregion
        #region StaticArrows

        /// <summary>
        /// Gets or sets a value indicating whether map view arrows use static colors.</summary>
        /// <value>
        /// <c>true</c> to use static colors when drawing attack or movement arrows in a map view;
        /// <c>false</c> to use animated colors. The default is <c>false</c>.</value>
        /// <remarks><para>
        /// <b>StaticArrows</b> holds the value of the "staticArrows" XML attribute.
        /// </para><para>
        /// Setting this property calls <see cref="ApplicationOptions.OnOptionsChanged"/> to save
        /// the current settings to the options file.</para></remarks>

        public bool StaticArrows {
            [DebuggerStepThrough]
            get { return this._staticArrows; }
            set {
                this._staticArrows = value;
                this._onOptionsChanged(this, EventArgs.Empty);
            }
        }

        #endregion
        #region StaticMarker

        /// <summary>
        /// Gets or sets a value indicating whether map view markers use static colors.</summary>
        /// <value>
        /// <c>true</c> to use static colors when marking the selected site in a map view;
        /// <c>false</c> to use animated colors. The default is <c>false</c>.</value>
        /// <remarks><para>
        /// <b>StaticMarker</b> holds the value of the "staticMarker" XML attribute.
        /// </para><para>
        /// Setting this property calls <see cref="ApplicationOptions.OnOptionsChanged"/> to save
        /// the current settings to the options file.</para></remarks>

        public bool StaticMarker {
            [DebuggerStepThrough]
            get { return this._staticMarker; }
            set {
                this._staticMarker = value;
                this._onOptionsChanged(this, EventArgs.Empty);
            }
        }

        #endregion
        #region Theme

        /// <summary>
        /// Gets or sets the <see cref="DefaultTheme"/> for the current WPF application.</summary>
        /// <value>
        /// A <see cref="DefaultTheme"/> value indicating the desired combination of Windows theme
        /// and color variant. The default is <see cref="DefaultTheme.System"/>.</value>
        /// <remarks><para>
        /// <b>Theme</b> holds the value of the "theme" XML attribute.
        /// </para><para>
        /// Setting this property calls <see cref="ApplicationOptions.OnOptionsChanged"/> to save
        /// the current settings to the options file.</para></remarks>

        public DefaultTheme Theme {
            [DebuggerStepThrough]
            get { return this._theme; }
            set {
                this._theme = value;
                this._onOptionsChanged(this, EventArgs.Empty);
            }
        }

        #endregion
        #region LoadMapView

        /// <summary>
        /// Loads the <see cref="MapViews"/> element with the specified identifier.</summary>
        /// <param name="id">
        /// The map view identifier whose <see cref="MapViewOptions"/> to return.</param>
        /// <returns>
        /// The <see cref="MapViews"/> element whose <see cref="MapViewOptions.Id"/> string matches
        /// the specified <paramref name="id"/>.</returns>
        /// <exception cref="ArgumentNullOrEmptyException">
        /// <paramref name="id"/> is a null reference or an empty string.</exception>
        /// <remarks>
        /// If the specified <paramref name="id"/> is not found in the <see cref="MapViews"/>
        /// collection, <b>LoadMapView</b> creates a new <see cref="MapViewOptions"/> object with
        /// default properties and the specified <paramref name="id"/>. The new object is added to
        /// the <see cref="MapViews"/> collection before the method returns.</remarks>

        public MapViewOptions LoadMapView(string id) {
            if (String.IsNullOrEmpty(id))
                ThrowHelper.ThrowArgumentNullOrEmptyException("id");

            // retrieve options for specified ID
            MapViewOptions options;
            if (!MapViews.TryGetValue(id, out options)) {

                // add options with new identifier
                options = new MapViewOptions(id);
                this._mapViews.Add(id, options);
            }

            return options;
        }

        #endregion
        #region LoadDesktop

        /// <summary>
        /// Loads the size and location of the specified <see cref="Window"/> from the <see
        /// cref="Desktops"/> element matching the current desktop size.</summary>
        /// <param name="window">
        /// The <see cref="Window"/> whose size and location to load.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="window"/> is a null reference.</exception>
        /// <remarks><para>
        /// <b>LoadDesktop</b> attempts to locate a <see cref="Desktops"/> element whose <see
        /// cref="DesktopOptions.VirtualScreen"/> property equals the current <see
        /// cref="SystemInformation.VirtualScreen"/> dimensions.
        /// </para><para>
        /// On success, <em>and</em> if the <see cref="DesktopOptions.WindowBounds"/> property of
        /// the matching <see cref="Desktops"/> element is not an empty <see cref="Rect"/>,
        /// <b>LoadDesktop</b> sets the size and location of the specified <paramref name="window"/>
        /// to the value of this property.
        /// </para><para>
        /// Also on success, <em>and</em> if the <see cref="DesktopOptions.WindowMaximized"/>
        /// property of the matching <see cref="Desktops"/> element is <c>true</c>,
        /// <b>LoadDesktop</b> sets the <see cref="Window.WindowState"/> property of the specified
        /// <paramref name="window"/> to <see cref="WindowState.Maximized"/>; otherwise, <see
        /// cref="Window.WindowState"/> is left unchanged.</para></remarks>

        public void LoadDesktop(Window window) {
            if (window == null)
                ThrowHelper.ThrowArgumentNullException("window");

            // retrieve options for current desktop size
            Rectangle screen = SystemInformation.VirtualScreen;
            DesktopOptions desktop = Desktops[screen];
            if (desktop == null) return;

            // restore saved window bounds, if any
            if (desktop.WindowBounds != new Rect()) {
                window.Left = desktop.WindowBounds.X;
                window.Top = desktop.WindowBounds.Y;
                window.Width = desktop.WindowBounds.Width;
                window.Height = desktop.WindowBounds.Height;
            }

            // restore maximized state if specified
            if (desktop.WindowMaximized)
                window.WindowState = WindowState.Maximized;
        }

        #endregion
        #region SaveDesktop

        /// <summary>
        /// Saves the size and location of the specified <see cref="Window"/> to the <see
        /// cref="Desktops"/> element matching the current desktop size.</summary>
        /// <param name="window">
        /// The <see cref="Window"/> whose size and location to save.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="window"/> is a null reference.</exception>
        /// <remarks><para>
        /// <b>SaveDesktop</b> saves the values of the <see cref="Window.RestoreBounds"/> and <see
        /// cref="Window.WindowState"/> properties of the specified <paramref name="window"/> to the
        /// <see cref="Desktops"/> element whose <see cref="DesktopOptions.VirtualScreen"/> property
        /// equals the current <see cref="SystemInformation.VirtualScreen"/> dimensions.
        /// </para><para>
        /// <b>SaveDesktop</b> creates a new <see cref="Desktops"/> element if no match was found.
        /// In any case, <b>SaveDesktop</b> calls <see cref="ApplicationOptions.OnOptionsChanged"/>
        /// to save the current settings to the options file.</para></remarks>

        public void SaveDesktop(Window window) {
            if (window == null)
                ThrowHelper.ThrowArgumentNullException("window");

            // retrieve options for current desktop size
            Rectangle screen = SystemInformation.VirtualScreen;
            DesktopOptions desktop;
            if (!Desktops.TryGetValue(screen, out desktop)) {

                // add options for new desktop size
                desktop = new DesktopOptions(screen);
                this._desktops.Add(screen, desktop);
            }

            // save parameters of specified window
            desktop.WindowMaximized = (window.WindowState == WindowState.Maximized);
            desktop.WindowBounds = window.RestoreBounds;

            // propagate changes
            this._onOptionsChanged(this, EventArgs.Empty);
        }

        #endregion
        #region SaveMapView

        /// <summary>
        /// Saves the specified display parameters for a single map view to the <see
        /// cref="MapViews"/> element matching the specified identifier.</summary>
        /// <param name="id">
        /// The map view identifier under which to save the display parameters.</param>
        /// <param name="scale">
        /// The new value for the <see cref="MapViewOptions.Scale"/> property.</param>
        /// <param name="animation">
        /// The new value for the <see cref="MapViewOptions.Animation"/> property.</param>
        /// <param name="showFlags">
        /// The new value for the <see cref="MapViewOptions.ShowFlags"/> property.</param>
        /// <param name="showGrid">
        /// The new value for the <see cref="MapViewOptions.ShowGrid"/> property.</param>
        /// <param name="showOwner">
        /// The new value for the <see cref="MapViewOptions.ShowOwner"/> property.</param>
        /// <param name="gaugeResource">
        /// The new value for the <see cref="MapViewOptions.GaugeResource"/> property.</param>
        /// <param name="gaugeResourceFlags">
        /// The new value for the <see cref="MapViewOptions.GaugeResourceFlags"/> property.</param>
        /// <param name="shownVariable">
        /// The <see cref="VariableClass"/> whose <see cref="VariableClass.Id"/> string is the new
        /// value for the <see cref="MapViewOptions.ShownVariable"/> property.</param>
        /// <param name="shownVariableFlags">
        /// The new value for the <see cref="MapViewOptions.ShownVariableFlags"/> property.</param>
        /// <exception cref="ArgumentNullOrEmptyException">
        /// <paramref name="id"/> is a null reference or an empty string.</exception>
        /// <remarks><para>
        /// <b>SaveMapView</b> calls <see cref="MapViewOptions.Save"/> to save the specified display
        /// parameters to the <see cref="MapViews"/> element that matches the specified <paramref
        /// name="id"/>.
        /// </para><para>
        /// <b>SaveMapView</b> creates a new <b>MapViewOptions</b> object and adds it to the
        /// <b>MapViews</b> collection if no matching element is found. Before returning,
        /// <b>SaveMapView</b> calls <see cref="ApplicationOptions.OnOptionsChanged"/> to save the
        /// current settings to the options file.
        /// </para><para>
        /// If the specified <paramref name="scale"/> is not in the <see cref="MapScales"/>
        /// collection, <b>SaveMapView</b> will add the value and re-sort the collection.
        /// </para></remarks>

        public void SaveMapView(string id, int scale,
            bool animation, bool showFlags, bool showGrid, bool showOwner,
            string gaugeResource, GaugeDisplay gaugeResourceFlags, 
            VariableClass shownVariable, VariableDisplay shownVariableFlags) {

            if (String.IsNullOrEmpty(id))
                ThrowHelper.ThrowArgumentNullOrEmptyException("id");

            // retrieve options for specified ID
            MapViewOptions options = LoadMapView(id);

            // save specified map view parameters
            options.Save(scale, animation, showFlags, showGrid, showOwner,
                gaugeResource, gaugeResourceFlags, shownVariable, shownVariableFlags);

            // add scale to collection if not present
            if (!MapScales.Contains(options.Scale)) {
                this._mapScales.Add(options.Scale);
                this._mapScales.Sort();
            }

            // propagate changes
            this._onOptionsChanged(this, EventArgs.Empty);
        }

        #endregion
        #region XmlSerializable Members
        #region ConstXmlName

        /// <summary>
        /// The name of the XML element associated with the <see cref="ViewOptions"/> class.
        /// </summary>
        /// <remarks>
        /// <b>ConstXmlName</b> holds the value "view", indicating the XML element in <see
        /// cref="FilePaths.OptionsSchema"/> whose data is managed by the <see cref="ViewOptions"/>
        /// class.</remarks>

        public const string ConstXmlName = "view";

        #endregion
        #region ReadXml

        /// <summary>
        /// Reads XML data into the <see cref="ViewOptions"/> object using the specified <see
        /// cref="XmlReader"/>.</summary>
        /// <param name="reader">
        /// The <see cref="XmlReader"/> from which to read.</param>
        /// <exception cref="XmlException">
        /// An error occurred while parsing the XML data provided by <paramref name="reader"/>.
        /// </exception>
        /// <exception cref="System.Xml.Schema.XmlSchemaException">
        /// <paramref name="reader"/> is an <see cref="XmlValidatingReader"/>, and the XML data did
        /// not conform to <see cref="FilePaths.OptionsSchema"/>.</exception>
        /// <remarks>
        /// <b>ReadXml</b> enhances the <see cref="XmlSerializable"/> implementation of this method
        /// by ensuring that the <see cref="MapScales"/> collection contains a default element
        /// (100%), that its elements are sorted in ascending order, and that there are no duplicate
        /// elements.</remarks>

        internal override void ReadXml(XmlReader reader) {

            // clear existing map scales
            this._mapScales.Clear();

            base.ReadXml(reader);

            // ensure we have default scale
            if (!MapScales.Contains(100))
                this._mapScales.Add(100);

            // ensure we have each map view's scale
            foreach (MapViewOptions mapView in MapViews.Values)
                if (!MapScales.Contains(mapView.Scale))
                    this._mapScales.Add(mapView.Scale);

            // sort for Zoom In/Out feature
            this._mapScales.Sort();
        }

        #endregion
        #region ReadXmlAttributes

        /// <summary>
        /// Reads XML attribute data into the <see cref="ViewOptions"/> object using the specified
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

            XmlUtility.ReadAttributeAsBoolean(reader, "bitmapGrid", ref this._bitmapGrid);
            XmlUtility.ReadAttributeAsBoolean(reader, "opaqueImages", ref this._opaqueImages);
            XmlUtility.ReadAttributeAsBoolean(reader, "staticArrows", ref this._staticArrows);
            XmlUtility.ReadAttributeAsBoolean(reader, "staticMarker", ref this._staticMarker);
            XmlUtility.ReadAttributeAsEnum(reader, "theme", ref this._theme);
        }

        #endregion
        #region ReadXmlElements

        /// <summary>
        /// Reads XML element data into the <see cref="ViewOptions"/> object using the specified
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

                case DesktopOptions.ConstXmlName: {
                    DesktopOptions desktop = new DesktopOptions(Rectangle.Empty);
                    desktop.ReadXml(reader);
                    this._desktops[desktop.VirtualScreen] = desktop;
                    return true;
                }

                case "mapScale": {
                    int scale = XmlConvert.ToInt32(reader.ReadString());
                    if (!MapScales.Contains(scale))
                        this._mapScales.Add(scale);
                    return true;
                }

                case MapViewOptions.ConstXmlName: {
                    MapViewOptions mapView = new MapViewOptions("");
                    mapView.ReadXml(reader);
                    this._mapViews[mapView.Id] = mapView;
                    return true;
                }

                default: return false;
            }
        }

        #endregion
        #region WriteXmlAttributes

        /// <summary>
        /// Writes all current data of the <see cref="ViewOptions"/> object that is serialized to
        /// XML attributes to the specified <see cref="XmlWriter"/>.</summary>
        /// <param name="writer">
        /// The <see cref="XmlWriter"/> to which to write.</param>
        /// <remarks>
        /// Please refer to class <see cref="XmlSerializable"/> for a complete description of this
        /// method.</remarks>

        protected override void WriteXmlAttributes(XmlWriter writer) {
            if (BitmapGrid)
                writer.WriteAttributeString("bitmapGrid", XmlConvert.ToString(BitmapGrid));
            if (OpaqueImages)
                writer.WriteAttributeString("opaqueImages", XmlConvert.ToString(OpaqueImages));
            if (StaticArrows)
                writer.WriteAttributeString("staticArrows", XmlConvert.ToString(StaticArrows));
            if (StaticMarker)
                writer.WriteAttributeString("staticMarker", XmlConvert.ToString(StaticMarker));

            if (Theme != DefaultTheme.System)
                writer.WriteAttributeString("theme", Theme.ToString());
        }

        #endregion
        #region WriteXmlElements

        /// <summary>
        /// Writes all current data of the <see cref="ViewOptions"/> object that is serialized to
        /// nested XML elements to the specified <see cref="XmlWriter"/>.</summary>
        /// <param name="writer">
        /// The <see cref="XmlWriter"/> to which to write.</param>
        /// <remarks>
        /// Please refer to class <see cref="XmlSerializable"/> for a complete description of this
        /// method.</remarks>

        protected override void WriteXmlElements(XmlWriter writer) {

            foreach (DesktopOptions desktop in Desktops.Values)
                desktop.WriteXml(writer);

            foreach (int scale in MapScales)
                writer.WriteElementString("mapScale", XmlConvert.ToString(scale));

            foreach (MapViewOptions mapView in MapViews.Values)
                mapView.WriteXml(writer);
        }

        #endregion
        #endregion
    }
}
