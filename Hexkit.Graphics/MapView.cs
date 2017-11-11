using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Threading;

using Tektosyne;
using Tektosyne.Collections;
using Tektosyne.Geometry;
using Tektosyne.Windows;

using Hexkit.Global;
using Hexkit.Options;
using Hexkit.Scenario;
using Hexkit.World;

namespace Hexkit.Graphics {

    /// <summary>
    /// Manages a single map view for a specific <see cref="WorldState"/>.</summary>
    /// <remarks><para>
    /// <b>MapView</b> manages the scaled bitmap catalog and related data required by a specific
    /// <see cref="WorldState"/>, as well as the <see cref="ScrollViewer"/> containing the actual
    /// map display.
    /// </para><para>
    /// <b>MapView</b> objects are always attached to the current <see cref="MapViewManager"/>
    /// instance. They are instantiated only through the <see cref="MapViewManager.CreateView"/>
    /// method of the <b>MapViewManager</b> class.</para></remarks>

    public sealed class MapView: CatalogManager, IKeyedValue<String> {
        #region MapView(...)

        /// <summary>
        /// Initializes a new instance of the <see cref="MapView"/> class with the specified
        /// parameters.</summary>
        /// <param name="id">
        /// The initial value for the <see cref="Id"/> property.</param>
        /// <param name="worldState">
        /// The initial value for the <see cref="WorldState"/> property.</param>
        /// <param name="parent">
        /// The <see cref="ContentControl"/> hosting the associated <see cref="MapViewControl"/>.
        /// </param>
        /// <param name="onMouseDown">
        /// An optional handler for <see cref="UIElement.MouseDown"/> events raised by the <see
        /// cref="MapViewControl"/>. This argument may be a null reference.</param>
        /// <param name="onMouseWheel">
        /// An optional handler for <see cref="UIElement.MouseWheel"/> events raised by the <see
        /// cref="MapViewControl"/>. This argument may be a null reference.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="worldState"/> or <paramref name="parent"/> is a null reference.
        /// </exception>
        /// <exception cref="ArgumentNullOrEmptyException">
        /// <paramref name="id"/> is a null reference or an empty string.</exception>
        /// <remarks>
        /// Clients should call the <see cref="MapViewManager.CreateView"/> method of the <see
        /// cref="MapViewManager"/> class to create a new <see cref="MapView"/> object.</remarks>

        internal MapView(string id, WorldState worldState, ContentControl parent,
            MouseButtonEventHandler onMouseDown, MouseWheelEventHandler onMouseWheel) {

            if (String.IsNullOrEmpty(id))
                ThrowHelper.ThrowArgumentNullOrEmptyException("id");
            if (worldState == null)
                ThrowHelper.ThrowArgumentNullException("worldState");

            this._id = String.Intern(id);
            this._worldState = worldState;

            // get current options for this map view
            MapViewOptions options = ApplicationOptions.Instance.View.LoadMapView(id);
            this._animation = (options.Animation && MapViewManager.Instance.Animation);
            this._showFlags = options.ShowFlags;
            this._showGrid = options.ShowGrid;
            this._showOwner = options.ShowOwner;

            GaugeResource = options.GaugeResource;
            GaugeResourceFlags = options.GaugeResourceFlags;

            // try to locate variable whose values are shown
            if (!String.IsNullOrEmpty(options.ShownVariable))
                ShownVariable = MasterSection.Instance.Variables.GetVariable(options.ShownVariable);
            ShownVariableFlags = options.ShownVariableFlags;

            // reset both variable options if either is invalid
            if (ShownVariable == null || ShownVariableFlags == 0) {
                ShownVariable = null;
                ShownVariableFlags = 0;
            }

            // clone manager's map grid for future scaling
            SetMapGrid(MapViewManager.Instance.MapGrid);

            // create output control with optional mouse handlers
            Control = new MapViewControl(this, onMouseDown, onMouseWheel);
            parent.Content = Control;

            // scale map grid to desired zoom level
            SetScale(options.Scale);

            // show overlay images
            Window owner = Window.GetWindow(parent);
            ShowOverlay(owner, false);
            if (ApplicationInfo.IsEditor) ShowOverlay(owner, true);
        }

        #endregion
        #region Private Fields

        // property backers
        private readonly string _id;
        private static PointD _mapBorder = new PointD(20, 20);
        private WorldState _worldState;

        // current display options
        private bool _animation = false;
        private int _scale = -100;
        private bool _showFlags = true;
        private bool _showGrid = true;
        private bool _showOwner = false;

        // currently highlighted map sites
        private PointI _selectedSite = Site.InvalidLocation;
        private bool[,] _selectedRegion = null;

        private readonly List<LineI> _attackArrows = new List<LineI>();
        private readonly List<LineI> _moveArrows = new List<LineI>();
        private readonly DrawingVisual _tileVisual = new DrawingVisual();

        #endregion
        #region Internal Properties
        #region Control

        /// <summary>
        /// Gets the associated <see cref="MapViewControl"/>.</summary>
        /// <value>
        /// The <see cref="MapViewControl"/> containing the display of the <see cref="MapView"/>.
        /// </value>
        /// <remarks>
        /// <b>Control</b> never returns a null reference while the <see cref="MapView"/> is valid,
        /// but reverts to a null reference after the <see cref="MapView"/> has been disposed of.
        /// </remarks>

        internal MapViewControl Control { get; private set; }

        #endregion
        #region ElementGeometry

        /// <summary>
        /// Gets a <see cref="StreamGeometry"/> for a single <see cref="PolygonGrid.Element"/> in
        /// the scaled <see cref="CatalogManager.MapGrid"/>.</summary>
        /// <value>
        /// A frozen <see cref="StreamGeometry"/> created from a single filled <see
        /// cref="RegularPolygon"/> outline in the current <see cref="CatalogManager.MapGrid"/>.
        /// </value>
        /// <remarks>
        /// <b>ElementGeometry</b> calls <see cref="MapViewManager.GetElementGeometry"/> to obtain a
        /// <see cref="StreamGeometry"/> that matches the current display <see cref="Scale"/>.
        /// </remarks>

        internal StreamGeometry ElementGeometry {
            get { return MapViewManager.Instance.GetElementGeometry(this); }
        }

        #endregion
        #region Extent

        /// <summary>
        /// Gets the display size of the <see cref="MapView"/>.</summary>
        /// <value>
        /// The display size of the entire <see cref="MapView"/>, in device-independent pixels.
        /// </value>
        /// <remarks>
        /// <b>Extent</b> equals the <see cref="PolygonGrid.DisplayBounds"/> of the asssociated <see
        /// cref="CatalogManager.MapGrid"/> plus twice the corresponding <see cref="MapBorder"/>.
        /// These dimensions equal the <see cref="ScrollViewer.ExtentWidth"/> and <see
        /// cref="ScrollViewer.ExtentHeight"/> of the associated <see cref="Control"/>.</remarks>

        internal Size Extent { get; private set; }

        #endregion
        #region GridGeometry

        /// <summary>
        /// Gets a <see cref="StreamGeometry"/> for the outline of the entire scaled <see
        /// cref="CatalogManager.MapGrid"/>.</summary>
        /// <value>
        /// A frozen <see cref="StreamGeometry"/> created from all <see cref="RegularPolygon"/>
        /// outlines contained in the current <see cref="CatalogManager.MapGrid"/>.</value>
        /// <remarks>
        /// <b>GridGeometry</b> calls <see cref="MapViewManager.GetGridGeometry"/> to obtain a <see
        /// cref="StreamGeometry"/> that matches the current display <see cref="Scale"/>.</remarks>

        internal StreamGeometry GridGeometry {
            get { return MapViewManager.Instance.GetGridGeometry(this); }
        }

        #endregion
        #region TileVisual

        /// <summary>
        /// Gets the <see cref="DrawingVisual"/> used for drawing catalog tiles.</summary>
        /// <value>
        /// The <see cref="DrawingVisual"/> used as a temporary buffer while drawing the bitmap
        /// tiles of the scaled <see cref="CatalogManager.Catalog"/>.</value>

        private DrawingVisual TileVisual {
            [DebuggerStepThrough]
            get { return this._tileVisual; }
        }

        #endregion
        #endregion
        #region Public Properties
        #region Animation

        /// <summary>
        /// Gets or sets a value indicating whether the <see cref="MapView"/> is animated.</summary>
        /// <value>
        /// <c>true</c> if the <see cref="MapView"/> shows animated graphics; otherwise,
        /// <c>false</c>.</value>
        /// <exception cref="ObjectDisposedException">
        /// The <see cref="MapView"/> object has been disposed of.</exception>
        /// <remarks><para>
        /// <b>Animation</b> automatically reverts to <c>false</c> if set to <c>true</c> while the
        /// <see cref="MapViewManager.Animation"/> flag of the current <see cref="MapViewManager"/>
        /// is <c>false</c>.
        /// </para><para>
        /// Setting <b>Animation</b> to <c>false</c> when it was <c>true</c> redraws the entire
        /// visible <see cref="MapView"/>.</para></remarks>

        public bool Animation {
            [DebuggerStepThrough]
            get {
                if (IsDisposed)
                    ThrowHelper.ThrowObjectDisposedException(Id);

                return this._animation;
            }
            set {
                if (IsDisposed)
                    ThrowHelper.ThrowObjectDisposedException(Id);

                value &= MapViewManager.Instance.Animation;
                bool redraw = (this._animation && !value);
                this._animation = value;

                if (redraw) Redraw();
                SaveOptions();
            }
        }

        #endregion
        #region AttackArrows

        /// <summary>
        /// Gets a list of all pairs of map locations that appear connected with arrows indicating
        /// attacks.</summary>
        /// <value>
        /// A <see cref="List{LineI}"/> containing the coordinates of every <see cref="Site"/> pair
        /// that is connected by an arrow. The default is an empty collection.</value>
        /// <remarks><para>
        /// <b>AttackArrows</b> never returns a null reference. All elements are unique.
        /// </para><note type="implementnotes">
        /// You must manually call <see cref="ShowArrows"/> to update the map display after adding
        /// or removing <b>AttackArrows</b> elements.</note></remarks>

        public List<LineI> AttackArrows {
            [DebuggerStepThrough]
            get { return this._attackArrows; }
        }

        #endregion
        #region GaugeResource

        /// <summary>
        /// Gets the identifier of the <see cref="ResourceClass"/> whose depletion status is shown
        /// by the <see cref="MapView"/>.</summary>
        /// <value><para>
        /// The <see cref="VariableClass.Id"/> string of the <see cref="ResourceClass"/> whose 
        /// current values for each unit stack are shown in depletion gauges. Possible values
        /// include the pseudo-resources <see cref="ResourceClass.StandardMorale"/> and <see
        /// cref="ResourceClass.StandardStrength"/>.
        /// </para><para>-or-</para><para>
        /// A null reference if the <see cref="MapView"/> does not show any depletion gauges. The
        /// default is a null reference.</para></value>
        /// <remarks><para>
        /// <see cref="GaugeResourceFlags"/> determines the appearance of all depletion gauges that
        /// are shown for the <b>GaugeResource</b>.
        /// </para><para>
        /// Call <see cref="ShowGauges"/> to set this property.</para></remarks>

        public string GaugeResource { get; private set; }

        #endregion
        #region GaugeResourceFlags

        /// <summary>
        /// Gets the display flags for the current <see cref="GaugeResource"/>.</summary>
        /// <value>
        /// A <see cref="GaugeDisplay"/> value containing display flags for the current <see
        /// cref="GaugeResource"/>.</value>
        /// <remarks>
        /// Call <see cref="ShowGauges"/> to set this property.</remarks>

        public GaugeDisplay GaugeResourceFlags { get; private set; }

        #endregion
        #region Id

        /// <summary>
        /// Gets the identifier of the <see cref="MapView"/>.</summary>
        /// <value>
        /// The identifier of the <see cref="MapView"/>.</value>
        /// <remarks><para>
        /// <b>Id</b> never returns a null reference or an empty string. This property never changes
        /// once the object has been constructed.
        /// </para><para>
        /// The value of the <b>Id</b> property uniquely identifies all elements in the <see
        /// cref="MapViewManager.MapViews"/> collection of a <see cref="MapViewManager"/>. This
        /// identifier is also used to match <see cref="MapView"/> objects against user settings
        /// managed by <b>Hexkit.Options.MapViewOptions</b>.</para></remarks>

        public string Id {
            [DebuggerStepThrough]
            get { return this._id; }
        }

        #endregion
        #region IsMouseOver

        /// <summary>
        /// Gets a value indicating whether the <see cref="Mouse"/> pointer is over the <see
        /// cref="MapView"/>.</summary>
        /// <value>
        /// <c>true</c> if the <see cref="Mouse"/> pointer is over an unobscured part of the <see
        /// cref="MapView"/>; otherwise, <c>false</c>.</value>
        /// <remarks>
        /// <b>IsMouseOver</b> returns <c>false</c> if the <see cref="Mouse"/> pointer is currently
        /// over another <see cref="UIElement"/> that obscures the <see cref="MapView"/>.</remarks>

        public bool IsMouseOver {
            get {
                /*
                 * MapCanvas.IsMouseOver excludes the ScrollViewer's scrollbars
                 * but includes any superimposed decoration (site marker, arrows).
                 */

                var canvas = Control.MapCanvas;
                return (canvas == null ? false : canvas.IsMouseOver);
            }
        }

        #endregion
        #region MapBorder

        /// <summary>
        /// Gets the dimensions of the visible map border.</summary>
        /// <value>
        /// A <see cref="PointD"/> indicating the number of device-independent pixels to leave blank
        /// on each side around the map.</value>
        /// <remarks>
        /// <b>MapBorder</b> returns the constant value 20 in both dimensions.</remarks>

        public static PointD MapBorder {
            [DebuggerStepThrough]
            get { return _mapBorder; }
        }

        #endregion
        #region MoveArrows

        /// <summary>
        /// Gets a list of all pairs of map locations that appear connected with arrows indicating
        /// movement.</summary>
        /// <value>
        /// A <see cref="List{LineI}"/> containing the coordinates of every <see cref="Site"/> pair
        /// that is connected by an arrow. The default is an empty collection.</value>
        /// <remarks><para>
        /// <b>MoveArrows</b> never returns a null reference. All elements are unique.
        /// </para><note type="implementnotes">
        /// You must manually call <see cref="ShowArrows"/> to update the map display after adding
        /// or removing <b>MoveArrows</b> elements.</note></remarks>

        public List<LineI> MoveArrows {
            [DebuggerStepThrough]
            get { return this._moveArrows; }
        }

        #endregion
        #region Scale

        /// <summary>
        /// Gets or sets the display scale of the <see cref="MapView"/>.</summary>
        /// <value>
        /// A positive <see cref="Int32"/> value indicating the current multiplier, in percent, for
        /// all display sizes used by the <see cref="MapView"/>.</value>
        /// <exception cref="ArgumentException">
        /// The property is set to zero or to a negative value.</exception>
        /// <exception cref="ObjectDisposedException">
        /// The property is set, and the <see cref="MapView"/> object has been disposed of.
        /// </exception>
        /// <remarks>
        /// Changing the <b>Scale</b> property may take up to a second or longer. The <see
        /// cref="MapView"/> stays centered on the same <see cref="Site"/>, if possible.</remarks>

        public int Scale {
            [DebuggerStepThrough]
            get { return this._scale; }
            set {
                if (IsDisposed)
                    ThrowHelper.ThrowObjectDisposedException(Id);

                if (this._scale != value) {
                    SetScale(value);
                    SaveOptions();
                }
            }
        }

        #endregion
        #region ScrollPosition

        /// <summary>
        /// Gets or sets the scroll position of the <see cref="MapView"/>.</summary>
        /// <value>
        /// The scroll position, in device-independent pixels, of the <see cref="MapView"/>. The
        /// default is (0,0).</value>
        /// <exception cref="ObjectDisposedException">
        /// The <see cref="MapView"/> object has been disposed of.</exception>

        public Point ScrollPosition {
            [DebuggerStepThrough]
            get {
                if (IsDisposed)
                    ThrowHelper.ThrowObjectDisposedException(Id);

                return new Point(Control.HorizontalOffset, Control.VerticalOffset);
            }
            set {
                if (IsDisposed)
                    ThrowHelper.ThrowObjectDisposedException(Id);

                Control.ScrollToHorizontalOffset(value.X);
                Control.ScrollToVerticalOffset(value.Y);
            }
        }

        #endregion
        #region SelectedSite

        /// <summary>
        /// Gets or sets the map location that is highlighted on the <see cref="MapView"/>.
        /// </summary>
        /// <value><para>
        /// The coordinates of the <see cref="Site"/> that is marked by a white frame on the <see
        /// cref="MapView"/>.
        /// </para><para>-or-</para><para>
        /// The value <see cref="Site.InvalidLocation"/> to indicate that no <see cref="Site"/> is
        /// highlighted. The default is <b>InvalidLocation</b>.</para></value>
        /// <exception cref="ObjectDisposedException">
        /// The property is set, and the <see cref="MapView"/> object has been disposed of.
        /// </exception>
        /// <remarks><para>
        /// Setting <b>SelectedSite</b> to an invalid location clears the current selection, if any,
        /// and stores <see cref="Site.InvalidLocation"/> as the new property value.
        /// </para><para>
        /// Setting <b>SelectedSite</b> to a valid location calls <see cref="ScrollIntoView"/> to
        /// ensure that the highlighted <see cref="Site"/> is fully visible.
        /// </para><para>
        /// Setting this property raises the <see cref="SelectedSiteChanged"/> event. Note that all
        /// invalid locations are interpreted as the same value, <see cref="Site.InvalidLocation"/>.
        /// </para><para>
        /// Setting this property does not check for redundant assignments. It is therefore possible
        /// to scroll the current selection into view and to raise the <see
        /// cref="SelectedSiteChanged"/> event by assigning the <b>SelectedSite</b> property its
        /// current value.</para></remarks>

        public PointI SelectedSite {
            [DebuggerStepThrough]
            get { return this._selectedSite; }
            set { SetSelectedSite(value, false); }
        }

        #endregion
        #region SelectedRegion

        /// <summary>
        /// Gets or sets an array that determines which map locations are highlighted on the <see
        /// cref="MapView"/>.</summary>
        /// <value><para>
        /// A two-dimensional <see cref="Array"/> of <see cref="Boolean"/> values indicating any
        /// <see cref="Site"/> that is marked by a bright hue on the <see cref="MapView"/>.
        /// </para><para>-or-</para><para>
        /// A null reference to indicate that no <see cref="Site"/> is highlighted. The default is a
        /// null reference.</para></value>
        /// <exception cref="ArgumentException">
        /// The property is set, and the dimensions of the <see cref="Array"/> differ from those of
        /// the current <see cref="CatalogManager.MapGrid"/>.</exception>
        /// <exception cref="ObjectDisposedException">
        /// The property is set, and the <see cref="MapView"/> object has been disposed of.
        /// </exception>
        /// <remarks><para>
        /// A <b>SelectedRegion</b> element that is <c>true</c> indicates that the <see
        /// cref="Site"/> with the same column and row indices should be highlighted.
        /// </para><para>
        /// Clients should invoke <see cref="PolygonGrid.CreateArray{Boolean}"/> on the <see
        /// cref="CatalogManager.MapGrid"/> to create an array that can be assigned to this
        /// property. The <see cref="MapView"/> is redrawn automatically when the property is set,
        /// but you must call <see cref="Redraw"/> explicitly when changing elements in the current
        /// <b>SelectedRegion</b> without reassigning the property.</para></remarks>

        public bool[,] SelectedRegion {
            [DebuggerStepThrough]
            get { return this._selectedRegion; }
            set {
                if (IsDisposed)
                    ThrowHelper.ThrowObjectDisposedException(Id);

                // check for correct map dimensions
                if (value != null) {
                    if (value.GetLength(0) != MapGrid.Size.Width ||
                        value.GetLength(1) != MapGrid.Size.Height)
                        ThrowHelper.ThrowArgumentExceptionWithFormat(
                            "value", Tektosyne.Strings.ArgumentPropertyConflict, MapGrid.Size);
                }

                // redraw with new selection
                this._selectedRegion = value;
                Redraw();
            }
        }

        #endregion
        #region ShowFlags

        /// <summary>
        /// Gets or sets a value indicating whether the <see cref="MapView"/> shows unit flags.
        /// </summary>
        /// <value>
        /// <c>true</c> if the <see cref="MapView"/> adds a colored flag to all unit stacks;
        /// otherwise, <c>false</c>.</value>
        /// <exception cref="ObjectDisposedException">
        /// The <see cref="MapView"/> object has been disposed of.</exception>

        public bool ShowFlags {
            [DebuggerStepThrough]
            get {
                if (IsDisposed)
                    ThrowHelper.ThrowObjectDisposedException(Id);

                return this._showFlags;
            }
            set {
                if (IsDisposed)
                    ThrowHelper.ThrowObjectDisposedException(Id);

                if (this._showFlags != value) {
                    this._showFlags = value;
                    Redraw();
                    SaveOptions();
                }
            }
        }

        #endregion
        #region ShowGrid

        /// <summary>
        /// Gets or sets a value indicating whether the <see cref="MapView"/> shows a grid outline.
        /// </summary>
        /// <value>
        /// <c>true</c> if the <see cref="MapView"/> shows a black outline around each <see
        /// cref="Site"/>; otherwise, <c>false</c>.</value>
        /// <exception cref="ObjectDisposedException">
        /// The <see cref="MapView"/> object has been disposed of.</exception>

        public bool ShowGrid {
            [DebuggerStepThrough]
            get {
                if (IsDisposed)
                    ThrowHelper.ThrowObjectDisposedException(Id);

                return this._showGrid;
            }
            set {
                if (IsDisposed)
                    ThrowHelper.ThrowObjectDisposedException(Id);

                if (this._showGrid != value) {
                    this._showGrid = value;
                    Redraw();
                    SaveOptions();
                }
            }
        }

        #endregion
        #region ShownVariable

        /// <summary>
        /// Gets the <see cref="VariableClass"/> whose current values are shown by the <see
        /// cref="MapView"/>.</summary>
        /// <value><para>
        /// The <see cref="VariableClass"/> whose current values are summed up over all entities on
        /// each site. The resulting sums are shown on each site.
        /// </para><para>-or-</para><para>
        /// A null reference if the <see cref="MapView"/> does not show any variable values. The
        /// default is a null reference.</para></value>
        /// <remarks><para>
        /// <see cref="ShownVariableFlags"/> determines whether basic or modifier values are shown.
        /// <b>ShownVariable</b> has no effect if <see cref="ShownVariableFlags"/> is zero.
        /// </para><para>
        /// Call <see cref="ShowVariable"/> to set this property.</para></remarks>

        public VariableClass ShownVariable { get; private set; }

        #endregion
        #region ShownVariableFlags

        /// <summary>
        /// Gets the display flags for the current <see cref="ShownVariable"/>.</summary>
        /// <value>
        /// A <see cref="VariableDisplay"/> value containing display flags for the current <see
        /// cref="ShownVariable"/>.</value>
        /// <remarks><para>
        /// Setting <b>ShowVariableFlags</b> to zero entirely disables the variable display, as if
        /// <see cref="ShownVariable"/> were a null reference.
        /// </para><para>
        /// Call <see cref="ShowVariable"/> to set this property.</para></remarks>

        public VariableDisplay ShownVariableFlags { get; private set; }

        #endregion
        #region ShowOwner

        /// <summary>
        /// Gets or sets a value indicating whether the <see cref="MapView"/> shows owner colors.
        /// </summary>
        /// <value>
        /// <c>true</c> if the <see cref="MapView"/> adds a colored hue to all owned sites,
        /// indicating the <see cref="Faction"/> that owns them; otherwise, <c>false</c>.</value>
        /// <exception cref="ObjectDisposedException">
        /// The <see cref="MapView"/> object has been disposed of.</exception>

        public bool ShowOwner {
            [DebuggerStepThrough]
            get {
                if (IsDisposed)
                    ThrowHelper.ThrowObjectDisposedException(Id);

                return this._showOwner;
            }
            set {
                if (IsDisposed)
                    ThrowHelper.ThrowObjectDisposedException(Id);

                if (this._showOwner != value) {
                    this._showOwner = value;
                    Redraw();
                    SaveOptions();
                }
            }
        }

        #endregion
        #region WorldState

        /// <summary>
        /// Gets or sets the <see cref="World.WorldState"/> shown by the <see cref="MapView"/>.
        /// </summary>
        /// <value>
        /// The <see cref="World.WorldState"/> whose data is shown by the <see cref="MapView"/>.
        /// </value>
        /// <exception cref="ArgumentNullException">
        /// The property is set to a null reference.</exception>
        /// <exception cref="ObjectDisposedException">
        /// The property is set, and the <see cref="MapView"/> object has been disposed of.
        /// </exception>
        /// <remarks>
        /// <b>WorldState</b> never returns a null reference. The map display is automatically
        /// redrawn at the current <see cref="Scale"/> when this property changes.</remarks>

        public WorldState WorldState {
            [DebuggerStepThrough]
            get { return this._worldState; }
            set {
                if (IsDisposed)
                    ThrowHelper.ThrowObjectDisposedException(Id);

                if (value == null)
                    ThrowHelper.ThrowArgumentNullException("value");

                this._worldState = value;
                Redraw();
            }
        }

        #endregion
        #endregion
        #region Private Methods
        #region DrawTile(Int32)

        /// <overloads>
        /// Draws one tile to the specified index in the scaled bitmap catalog.</overloads>
        /// <summary>
        /// Draws the tile contained in the <see cref="TileVisual"/> to the specified index in the
        /// scaled bitmap catalog.</summary>
        /// <param name="index">
        /// The index of the <see cref="CatalogManager.Catalog"/> tile to draw.</param>
        /// <remarks>
        /// <b>DrawTile</b> overwrites the current contents of the <see
        /// cref="CatalogManager.TileDrawBuffer"/>.</remarks>

        private void DrawTile(int index) {
            TileDrawBuffer.Clear();
            TileDrawBuffer.Render(TileVisual);

            int x = GetTileColumn(index) * TileWidth;
            int y = GetTileRow(index) * TileHeight;
            Catalog.Read(x, y, TileDrawBuffer);
        }

        #endregion
        #region DrawTile(Int32, Brush, Pen, Geometry)

        /// <summary>
        /// Draws the tile defined by the specified <see cref="Geometry"/> to the specified index in
        /// the scaled bitmap catalog.</summary>
        /// <param name="index">
        /// The index of the <see cref="CatalogManager.Catalog"/> tile to draw.</param>
        /// <param name="brush">
        /// The <see cref="Brush"/> used to fill the specified <paramref name="geometry"/>.</param>
        /// <param name="pen">
        /// The <see cref="Pen"/> used to draw the outline of the specified <paramref
        /// name="geometry"/>.</param>
        /// <param name="geometry">
        /// The <see cref="Geometry"/> to draw at the specified index.</param>
        /// <remarks>
        /// <b>DrawTile</b> overwrites the current contents of the <see cref="TileVisual"/> and the
        /// <see cref="CatalogManager.TileDrawBuffer"/>.</remarks>

        private void DrawTile(int index, Brush brush, Pen pen, Geometry geometry) {

            DrawingContext context = TileVisual.RenderOpen();
            context.DrawGeometry(brush, pen, geometry);
            context.Close();

            DrawTile(index);
        }

        #endregion
        #region DrawTile(Int32, ImageSource, Rect)

        /// <summary>
        /// Draws the tile contained in the specified <see cref="ImageSource"/> to the specified
        /// index in the scaled <see cref="CatalogManager.Catalog"/>.</summary>
        /// <param name="index">
        /// The index of the <see cref="CatalogManager.Catalog"/> tile to draw.</param>
        /// <param name="image">
        /// The <see cref="ImageSource"/> to draw at the specified index.</param>
        /// <param name="target">
        /// The region where the specified <paramref name="image"/> is drawn within the scaled <see
        /// cref="CatalogManager.Catalog"/>.</param>
        /// <param name="shadow">
        /// <c>true</c> to add a <see cref="DropShadowEffect"/> to the drawing; <c>false</c> to draw
        /// without any effects.</param>
        /// <remarks>
        /// <b>DrawTile</b> overwrites the current contents of the <see cref="TileVisual"/> and the
        /// <see cref="CatalogManager.TileDrawBuffer"/>.</remarks>

        private void DrawTile(int index, ImageSource image, Rect target, bool shadow) {

            DrawingContext context = TileVisual.RenderOpen();
            TileVisual.Effect = (shadow ? MapViewManager.Instance.DropShadow : null);
            context.DrawImage(image, target);
            context.Close();

            DrawTile(index);
        }

        #endregion
        #region GetScrollRectangle

        /// <summary>
        /// Determines the range of scroll positions that would cause the specified map location to
        /// be fully visible.</summary>
        /// <param name="site">
        /// The coordinates of the <see cref="Site"/> to scroll into view.</param>
        /// <returns>
        /// A <see cref="RectD"/> indicating the range of scroll positions, in device-independent
        /// pixels, that would cause the specified <paramref name="site"/> to be fully visible.
        /// </returns>
        /// <remarks>
        /// <b>GetScrollRectangle</b> performs no range checking on the specified <paramref
        /// name="site"/>, and may return a <see cref="RectD"/> with negative <see cref="RectD.X"/>
        /// or <see cref="RectD.Y"/> coordinates. Moreover, <b>GetScrollRectangle</b> returns <see
        /// cref="RectD.Empty"/> if the computed <see cref="RectD.Width"/> or <see
        /// cref="RectD.Height"/> is less than zero.</remarks>

        private RectD GetScrollRectangle(PointI site) {

            // compute display location of specified site
            PointD pixel = SiteToView(site);
            SizeD element = MapGrid.Element.Bounds.Size;

            // compute visible scrolling range
            double width = Control.ViewportWidth - element.Width;
            double height = Control.ViewportHeight - element.Height;

            // compute minimum scroll location
            double x = pixel.X - element.Width / 2.0 - width;
            double y = pixel.Y - element.Height / 2.0 - height;

            return (width < 0 || height < 0) ? RectD.Empty : new RectD(x, y, width, height);
        }

        #endregion
        #region SaveOptions

        /// <summary>
        /// Saves all persistent display options of the <see cref="MapView"/> to the associated <see
        /// cref="MapViewOptions"/>.</summary>
        /// <remarks><para>
        /// <b>SaveOptions</b> is called automatically whenever a property that represents a display
        /// option changes after the <see cref="MapView"/> has been constructed.
        /// </para><para>
        /// <b>SaveOptions</b> forwards the <see cref="Id"/> string and the values of all such 
        /// properties to the <see cref="Options.ViewOptions.SaveMapView"/> method which updates the
        /// associated <see cref="MapViewOptions"/> and saves them to disk.</para></remarks>

        private void SaveOptions() {
            ApplicationOptions.Instance.View.SaveMapView(Id,
                Scale, Animation, ShowFlags, ShowGrid, ShowOwner,
                GaugeResource, GaugeResourceFlags, ShownVariable, ShownVariableFlags);
        }

        #endregion
        #region SetScale

        /// <summary>
        /// Sets the value of the <see cref="Scale"/> property.</summary>
        /// <param name="scale">
        /// The new value for the <see cref="Scale"/> property.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="scale"/> is zero or negative.</exception>
        /// <remarks><para>
        /// <b>SetScale</b> recalculates all scaled dimensions, creates a copy of the bitmap catalog
        /// of the current <see cref="MapViewManager"/> at the specified <paramref name="scale"/>,
        /// and redraws the map display.
        /// </para><para>
        /// In addition to copying the bitmap tiles defined by the <see cref="MapViewManager"/>,
        /// <b>SetScale</b> copies an "invalid" icon to index position zero of the bitmap catalog.
        /// </para><para>
        /// The <see cref="ScrollPosition"/> is adjusted so that the <see cref="MapView"/> remains
        /// centered on the same <see cref="Site"/>, if possible.</para></remarks>

        private void SetScale(int scale) {
            if (scale <= 0)
                ThrowHelper.ThrowArgumentOutOfRangeException(
                    "scale", scale, Tektosyne.Strings.ArgumentNotPositive);

            // store new scale
            this._scale = scale;

            // hide scaled catalog if shown
            HideCatalog();

            // retrieve central map location
            PointI centerSite = ViewToSite(new PointD(
                Control.HorizontalOffset + Control.ViewportWidth / 2.0,
                Control.VerticalOffset + Control.ViewportHeight / 2.0));

            // get current map view manager
            MapViewManager manager = MapViewManager.Instance;

            // compute scaled polygon size
            ResizePolygon((manager.MapGrid.Element.Length * Scale) / 100.0);

            // create scaled bitmap catalog
            CreateCatalog(manager.TileCount);

            // set virtual extent to new map size
            Extent = new Size(
                MapGrid.DisplayBounds.Width + 2 * MapBorder.X,
                MapGrid.DisplayBounds.Height + 2 * MapBorder.Y);

            /*
             * The scaled catalog is created from the manager's standard bitmap catalog
             * by scaled copy. We copy each bitmap tile individually, both to avoid
             * rounding errors and to apply a polygonal mask around each tile.
             */

            Catalog.Lock();
            Catalog.Clear();

            // get scaled polygonal outline, centered on tile
            PointD offset = new PointD(TileWidth / 2.0, TileHeight / 2.0);
            var geometry = new PathGeometry() { Figures = { MapGrid.Element.ToFigure(offset) } };
            geometry.Freeze();

            // set polygonal clipping region
            TileVisual.Clip = geometry;

            // compute scaled bounds for "invalid" icon
            double diameter = 1.6 * MapGrid.Element.InnerRadius;
            Rect iconBounds = new Rect(
                (TileWidth - diameter) / 2.0, (TileHeight - diameter) / 2.0, diameter, diameter);

            // draw "invalid" icon, polygon outline & shading
            DrawTile((int) CatalogIndex.Invalid, Images.Invalid, iconBounds, false);
            DrawTile((int) CatalogIndex.Outline, null, MediaObjects.ThickPen, geometry);
            DrawTile((int) CatalogIndex.Shading, MediaObjects.ShadeLightBrush, null, geometry);

            // get target tile size
            Rect target = new Rect(0, 0, TileWidth, TileHeight);

            // copy tiles from master catalog to scaled catalog
            for (int n = (int) CatalogIndex.Images; n < manager.TileCount; n++) {
                manager.DrawTileToBuffer(n);

                // determine whether to apply a drop shadow effect
                EntityClass entityClass = manager.GetEntityClass(n);
                bool shadow = (entityClass != null && entityClass.HasDropShadow);
                
                DrawTile(n, manager.TileCopyBuffer, target, shadow);
            }

            Catalog.Unlock();
            Catalog.Freeze();

            // update map view display
            Control.OnScaleChanged();

            // restore central map location, if any
            if (centerSite != Site.InvalidLocation)
                CenterOn(centerSite);
        }

        #endregion
        #region SetSelectedSite

        /// <summary>
        /// Sets the value of the <see cref="SelectedSite"/> property and optionally centers the
        /// <see cref="MapView"/> on the same location.</summary>
        /// <param name="site">
        /// The new value for the <see cref="SelectedSite"/> property.</param>
        /// <param name="center">
        /// <c>true</c> to center the <see cref="MapView"/> on the specified <paramref
        /// name="site"/>; <c>false</c> to merely scroll the <paramref name="site"/> into view.
        /// </param>
        /// <exception cref="ObjectDisposedException">
        /// The <see cref="MapView"/> object has been disposed of.</exception>
        /// <remarks>
        /// Please refer to <see cref="SelectedSite"/> and <see cref="CenterOn"/> for details.
        /// </remarks>

        private void SetSelectedSite(PointI site, bool center) {
            if (IsDisposed)
                ThrowHelper.ThrowObjectDisposedException(Id);

            // ensure selection is visible
            if (MapGrid.Contains(site)) {
                if (center)
                    CenterOn(site);
                else
                    ScrollIntoView(site);
            } else
                site = Site.InvalidLocation;

            // show marker on selection
            Control.ShowMarker(site);
            this._selectedSite = site;

            // broadcast event to all listeners
            var handler = SelectedSiteChanged;
            if (handler != null) handler(this, EventArgs.Empty);
        }

        #endregion
        #endregion
        #region Public Methods
        #region CenterAndSelect

        /// <summary>
        /// Centers the <see cref="MapView"/> on the specified map location and highlights it.
        /// </summary>
        /// <param name="site">
        /// The coordinates of the <see cref="Site"/> to center in the <see cref="MapView"/> and to
        /// set as the new <see cref="SelectedSite"/>.</param>
        /// <exception cref="ObjectDisposedException">
        /// The <see cref="MapView"/> object has been disposed of.</exception>
        /// <remarks><para>
        /// <b>CenterAndSelect</b> sets the <see cref="SelectedSite"/> to the specified <paramref
        /// name="site"/> and centers the <see cref="MapView"/> on the same location, just as if
        /// <see cref="CenterOn"/> had been called.
        /// </para><para>
        /// The specified <paramref name="site"/> may be invalid, in which case <b>SelectedSite</b>
        /// will be set to <see cref="Site.InvalidLocation"/> and the scroll position of the <see
        /// cref="MapView"/> will not change.</para></remarks>

        public void CenterAndSelect(PointI site) {
            SetSelectedSite(site, true);
        }

        #endregion
        #region CenterOn

        /// <summary>
        /// Centers the <see cref="MapView"/> on the specified map location.</summary>
        /// <param name="site">
        /// The coordinates of the <see cref="Site"/> to center in the <see cref="MapView"/>.
        /// </param>
        /// <exception cref="ObjectDisposedException">
        /// The <see cref="MapView"/> object has been disposed of.</exception>
        /// <remarks><para>
        /// <b>CenterOn</b> scrolls the <see cref="MapView"/> to center it on the specified
        /// <paramref name="site"/>, or as close to that location as possible.
        /// </para><para>
        /// <b>CenterOn</b> does nothing if the specified <paramref name="site"/> is invalid.
        /// </para></remarks>

        public void CenterOn(PointI site) {
            if (IsDisposed)
                ThrowHelper.ThrowObjectDisposedException(Id);

            // quit immediately if site invalid
            if (!MapGrid.Contains(site)) return;

            // compute display location of specified site
            PointD pixel = SiteToView(site);

            // move site to center of output control
            Control.ScrollToHorizontalOffset(pixel.X - Control.ViewportWidth / 2.0);
            Control.ScrollToVerticalOffset(pixel.Y - Control.ViewportHeight / 2.0);
        }

        #endregion
        #region InSelectedRegion

        /// <summary>
        /// Determines whether the specified map location is within the <see
        /// cref="SelectedRegion"/>.</summary>
        /// <param name="site">
        /// The coordinates of the <see cref="Site"/> to examine.</param>
        /// <returns>
        /// <c>true</c> if the current <see cref="SelectedRegion"/> is not a null reference, and the
        /// specified <paramref name="site"/> corresponds to a <b>SelectedRegion</b> element which
        /// is <c>true</c>; otherwise, <c>false</c>.</returns>
        /// <remarks>
        /// <b>InSelectedRegion</b> returns <c>false</c> if the specified <paramref name="site"/> is
        /// outside of the current <see cref="CatalogManager.MapGrid"/>.</remarks>

        public bool InSelectedRegion(PointI site) {
            if (SelectedRegion == null) return false;

            if (site.X < 0 || site.X >= SelectedRegion.GetLength(0) ||
                site.Y < 0 || site.Y >= SelectedRegion.GetLength(1))
                return false;

            return SelectedRegion[site.X, site.Y];
        }

        #endregion
        #region MouseToSite()

        /// <overloads>
        /// Converts a mouse cursor position to the corresponding map location.</overloads>
        /// <summary>
        /// Converts the current <see cref="Mouse"/> position to the corresponding map location.
        /// </summary>
        /// <returns><para>
        /// The coordinates of the <see cref="Site"/> under the mouse cursor.
        /// </para><para>-or-</para><para>
        /// <see cref="Site.InvalidLocation"/> if there is no such <see cref="Site"/>.
        /// </para></returns>
        /// <exception cref="ObjectDisposedException">
        /// The <see cref="MapView"/> object has been disposed of.</exception>
        /// <remarks><para>
        /// <b>MouseToSite</b> invokes <see cref="ViewToSite(Double, Double)"/> with the current
        /// <see cref="Mouse"/> position, relative to the associated map view display.
        /// </para><para>
        /// Check <see cref="IsMouseOver"/> before calling <b>MouseToSite</b> to ensure that the
        /// <see cref="Mouse"/> pointer is not over another <see cref="UIElement"/> that obscures
        /// the <see cref="MapView"/>.</para></remarks>

        public PointI MouseToSite() {
            if (IsDisposed)
                ThrowHelper.ThrowObjectDisposedException(Id);

            Point pixel = Mouse.GetPosition(Control.Renderer);
            return ViewToSite(pixel.X, pixel.Y);
        }

        #endregion
        #region MouseToSite(MouseEventArgs)

        /// <summary>
        /// Converts the position of the specified <see cref="MouseEventArgs"/> to the corresponding
        /// map location.</summary>
        /// <param name="args">
        /// A <see cref="MouseEventArgs"/> object containing the position to convert.</param>
        /// <returns><para>
        /// The coordinates of the <see cref="Site"/> under the mouse cursor.
        /// </para><para>-or-</para><para>
        /// <see cref="Site.InvalidLocation"/> if there is no such <see cref="Site"/>.
        /// </para></returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="args"/> is a null reference.</exception>
        /// <exception cref="ObjectDisposedException">
        /// The <see cref="MapView"/> object has been disposed of.</exception>
        /// <remarks>
        /// <b>MouseToSite</b> invokes <see cref="ViewToSite(Double, Double)"/> with the mouse
        /// position indicated by the specified <paramref name="args"/>, relative to the associated
        /// map view display.</remarks>

        public PointI MouseToSite(MouseEventArgs args) {
            if (IsDisposed)
                ThrowHelper.ThrowObjectDisposedException(Id);
            if (args == null)
                ThrowHelper.ThrowArgumentNullException("args");

            Point pixel = args.GetPosition(Control.Renderer);
            return ViewToSite(pixel.X, pixel.Y);
        }

        #endregion
        #region Redraw

        /// <summary>
        /// Redraws the entire <see cref="MapView"/>.</summary>
        /// <exception cref="ObjectDisposedException">
        /// The <see cref="MapView"/> object has been disposed of.</exception>

        public void Redraw() {
            if (IsDisposed)
                ThrowHelper.ThrowObjectDisposedException(Id);

            Control.Renderer.InvalidateVisual();
        }

        #endregion
        #region ScrollIntoView

        /// <summary>
        /// Scrolls the <see cref="MapView"/> so that the specified map locations are visible.
        /// </summary>
        /// <param name="sites">
        /// An <see cref="Array"/> containing the coordinates of every <see cref="Site"/> that
        /// should be scrolled into view.</param>
        /// <exception cref="ObjectDisposedException">
        /// The <see cref="MapView"/> object has been disposed of.</exception>
        /// <remarks><para>
        /// <b>ScrollIntoView</b> attempts to set a <see cref="ScrollPosition"/> that causes all of
        /// the specified <paramref name="sites"/> to be visible at the same time.
        /// </para><para>
        /// If that is not possible, the new <b>ScrollPosition</b> will show the first <see
        /// cref="Site"/>, and as many of the remaining <paramref name="sites"/> as can be made
        /// visible at the same time.
        /// </para><para>
        /// Any <paramref name="sites"/> that specify invalid coordinates are ignored. The scroll
        /// position remains unchanged if the array is empty, or if all array elements are invalid.
        /// </para></remarks>

        public void ScrollIntoView(params PointI[] sites) {
            if (IsDisposed)
                ThrowHelper.ThrowObjectDisposedException(Id);

            // default to maximum scroll region
            RectD visible = new RectD(0, 0, Control.ExtentWidth, Control.ExtentHeight);

            /*
             * To determine the visible scroll region, we restrict the maximum scroll region
             * by each scroll region that would cause a specified site to be visible.
             *
             * Sites whose scroll region does not intersect with the region established 
             * so far are ignored, but the remaining sites are still considered.
             */

            bool intersection = false;

            foreach (PointI site in sites) {
                if (!MapGrid.Contains(site))
                    continue;

                // get scroll region to make site visible
                RectD pixels = GetScrollRectangle(site);

                // restrict visible region if possible
                RectD visibleIntersection;
                if (visible.Intersect(pixels, out visibleIntersection)) {
                    visible = visibleIntersection;
                    intersection = true;
                }
            }

            // quit if nothing to do
            if (!intersection) return;

            bool scrollX = false, scrollY = false;

            // compute horizontal distance
            double x = Control.HorizontalOffset;
            double dx0 = visible.Left - x;
            double dx1 = x - visible.Right;

            // adjust scroll position
            if (dx0 > 0) { x += dx0; scrollX = true; }
            else if (dx1 > 0) { x -= dx1; scrollX = true; }

            // compute vertical distance
            double y = Control.VerticalOffset;
            double dy0 = visible.Top - y;
            double dy1 = y - visible.Bottom;

            // adjust scroll position
            if (dy0 > 0) { y += dy0; scrollY = true; }
            else if (dy1 > 0) { y -= dy1; scrollY = true; }

            if (scrollX || scrollY) {
                // scroll output control if position changed
                if (scrollX) Control.ScrollToHorizontalOffset(x);

                // scroll output control if position changed
                if (scrollY) Control.ScrollToVerticalOffset(y);

                // HACK: ScrollViewer may simply ignore scroll position changes
                // as a performance optimization, unless forced by UpdateLayout
                Control.UpdateLayout();
            }
        }

        #endregion
        #region ScrollStep

        /// <summary>
        /// Scrolls the <see cref="MapView"/> by one <see cref="Site"/> in the specified direction.
        /// </summary>
        /// <param name="direction">
        /// A <see cref="ScrollDirection"/> value indicating the direction in which to scroll the
        /// <see cref="MapView"/>.</param>
        /// <exception cref="InvalidEnumArgumentException">
        /// <paramref name="direction"/> is not a valid <see cref="ScrollDirection"/> value.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// The <see cref="MapView"/> object has been disposed of.</exception>
        /// <remarks><para>
        /// <b>ScrollStep</b> scrolls the <see cref="MapView"/> in the specified <paramref
        /// name="direction"/>. The scroll distance, in screen pixels, equals the width or height,
        /// depending on <paramref name="direction"/>, of one <see cref="PolygonGrid.Element"/> in
        /// the scaled <see cref="CatalogManager.MapGrid"/>.
        /// </para><para>
        /// <b>ScrollStep</b> scrolls as far as possible if it cannot scroll the entire distance.
        /// </para></remarks>

        public void ScrollStep(ScrollDirection direction) {
            if (IsDisposed)
                ThrowHelper.ThrowObjectDisposedException(Id);

            // scroll by one polygon in the specified direction
            Control.ScrollStep(direction,
                MapGrid.Element.Bounds.Width, 
                MapGrid.Element.Bounds.Height);
        }

        #endregion
        #region ShowArrows

        /// <summary>
        /// Shows the current <see cref="AttackArrows"/> and <see cref="MoveArrows"/> on the <see
        /// cref="MapView"/>.</summary>
        /// <exception cref="ObjectDisposedException">
        /// The <see cref="MapView"/> object has been disposed of.</exception>
        /// <remarks>
        /// If the <see cref="AttackArrows"/> or <see cref="MoveArrows"/> collection is empty,
        /// <b>ShowArrows</b> removes any currently shown arrows.</remarks>

        public void ShowArrows() {
            if (IsDisposed)
                ThrowHelper.ThrowObjectDisposedException(Id);

            Control.ArrowDrawer.Draw();
        }

        #endregion
        #region ShowEntity

        /// <summary>
        /// Shows the specified <see cref="Entity"/> on its current map location.</summary>
        /// <param name="entity">
        /// The <see cref="Entity"/> to show.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="entity"/> is a null reference.</exception>
        /// <exception cref="ObjectDisposedException">
        /// The <see cref="MapView"/> object has been disposed of.</exception>
        /// <remarks><para>
        /// <b>ShowEntity</b> invokes <see cref="Entity.MoveToTop"/> on the specified <paramref
        /// name="entity"/>, and redraws its <see cref="Entity.Site"/> on the <see cref="MapView"/>
        /// if <see cref="Entity.MoveToTop"/> returns <c>true</c>.
        /// </para><para>
        /// For units, the effect is to make the specified <paramref name="entity"/> the only
        /// visible unit in its current <see cref="Entity.Site"/>.</para></remarks>

        public void ShowEntity(Entity entity) {
            if (entity == null)
                ThrowHelper.ThrowArgumentNullException("entity");

            if (entity.MoveToTop()) Redraw();
        }

        #endregion
        #region ShowGauges

        /// <summary>
        /// Sets the <see cref="GaugeResource"/> and <see cref="GaugeResourceFlags"/> properties to
        /// the specified values.</summary>
        /// <param name="resource">
        /// The new value for the <see cref="GaugeResource"/> property.</param>
        /// <param name="flags">
        /// The new value for the <see cref="GaugeResourceFlags"/> property.</param>
        /// <exception cref="ObjectDisposedException">
        /// The <see cref="MapView"/> object has been disposed of.</exception>
        /// <remarks>
        /// <b>ShowGauges</b> also redraws the <see cref="MapView"/> and saves all persistent
        /// display options to the associated <see cref="MapViewOptions"/>.</remarks>

        public void ShowGauges(string resource, GaugeDisplay flags) {
            if (IsDisposed)
                ThrowHelper.ThrowObjectDisposedException(Id);

            if (resource != null) resource = String.Intern(resource);
            GaugeResource = resource;
            GaugeResourceFlags = flags;

            Redraw();
            SaveOptions();
        }

        #endregion
        #region ShowImage

        /// <summary>
        /// Shows the image of the specified <see cref="EntityClass"/> on the specified map
        /// locations, delaying the specified time for each step.</summary>
        /// <param name="entityClass">
        /// The <see cref="EntityClass"/> whose image to show.</param>
        /// <param name="sites">
        /// An <see cref="Array"/> containing the coordinates of each <see cref="Site"/> to show the
        /// image on.</param>
        /// <param name="move">
        /// <c>true</c> to smoothly move the image from one <see cref="Site"/> to the next, without
        /// pausing at the start or end of each movement; <c>false</c> to show the image on each
        /// <see cref="Site"/>, moving abruptly to the next.</param>
        /// <param name="delay">
        /// The duration, in milliseconds, for which the image is shown on each <see cref="Site"/>.
        /// If this argument is non-positive, a default value of 250 msec is assumed.</param>
        /// <param name="abortSignal">
        /// A <see cref="WaitHandle"/> to signal that all display actions should be aborted.</param>
        /// <exception cref="InvalidOperationException">
        /// The <see cref="Thread.CurrentThread"/> is identical to the <see
        /// cref="DispatcherObject.Dispatcher"/> thread of the associated <see cref="Control"/>.
        /// <b>ShowImage</b> must run on a background thread.</exception>
        /// <exception cref="ObjectDisposedException">
        /// The <see cref="MapView"/> object has been disposed of.</exception>
        /// <remarks><para>
        /// <b>ShowImage</b> silently returns if <see cref="CatalogManager.IsDisposed"/> is
        /// <c>true</c>; if the specified <paramref name="entityClass"/> is a null reference; if the
        /// specified <paramref name="sites"/> array is a null reference or an empty array, or
        /// contains any invalid coordinates; or if the specified <paramref name="abortSignal"/> is
        /// a null reference or is already set.
        /// </para><para>
        /// Otherwise, <b>ShowImage</b> shows the first image frame of the specified <paramref
        /// name="entityClass"/> on all specified <paramref name="sites"/>.
        /// </para><para>
        /// If <paramref name="move"/> is <c>true</c>, the specified <paramref name="delay"/>
        /// dictates the duration of each movement from one <see cref="Site"/> to the next.
        /// Otherwise, the image simply rests on each <see cref="Site"/> for that duration, and is
        /// then instantly placed on the next one.
        /// </para><para>
        /// Regardless of the value of the <paramref name="move"/> flag, you may specify identical
        /// consecutive <paramref name="sites"/> to rest the image on the corresponding <see
        /// cref="Site"/> for one or more <paramref name="delay"/> periods.
        /// </para><para>
        /// At any time, <b>ShowImage</b> aborts all display actions and returns immediately when
        /// the specified <paramref name="abortSignal"/> is set.</para></remarks>

        public void ShowImage(EntityClass entityClass, PointI[] sites,
            bool move, int delay, WaitHandle abortSignal) {

            if (IsDisposed)
                ThrowHelper.ThrowObjectDisposedException(Id);

            Control.ShowImage(entityClass, sites, move, delay, abortSignal);
        }

        #endregion
        #region ShowOverlay

        /// <summary>
        /// Shows an <see cref="OverlayImage"/> above or below the <see cref="MapView"/>.</summary>
        /// <param name="owner">
        /// The parent <see cref="Window"/> for any dialogs.</param>
        /// <param name="editing">
        /// <c>true</c> to show the <see cref="EditorOptions.Overlay"/> defined by the current <see
        /// cref="EditorOptions"/>; <c>false</c> to show the <see cref="AreaSection.Overlay"/>
        /// defined by the current <see cref="AreaSection"/>.</param>
        /// <returns>
        /// <c>true</c> if the indicated <see cref="OverlayImage"/> contains a valid <see
        /// cref="OverlayImage.Bitmap"/>; otherwise, <c>false</c>.</returns>
        /// <exception cref="InvalidOperationException">
        /// <paramref name="editing"/> is <c>true</c>, and <see cref="ApplicationInfo.IsEditor"/> is
        /// <c>false</c>.</exception>
        /// <exception cref="ObjectDisposedException">
        /// The <see cref="MapView"/> object has been disposed of.</exception>
        /// <remarks>
        /// <b>ShowOverlay</b> attempts to load the <see cref="OverlayImage.Bitmap"/> of the
        /// indicated <see cref="OverlayImage"/> if necessary. The user is notified if an error
        /// occurs, but execution continues in any case.</remarks>

        public bool ShowOverlay(Window owner, bool editing) {
            if (IsDisposed)
                ThrowHelper.ThrowObjectDisposedException(Id);

            Image image = null;
            OverlayImage overlay = null;

            if (editing) {
                ApplicationInfo.CheckEditor();
                image = Control.EditorOverlay;
                overlay = ApplicationOptions.Instance.Editor.Overlay;
            } else {
                image = Control.Overlay;
                overlay = MasterSection.Instance.Areas.Overlay;
            }

            // try loading overlay bitmap
            if (!overlay.IsEmpty && overlay.Bitmap == null)
                overlay.Load(owner);

            if (overlay.Bitmap == null) {
                image.Tag = null;
                image.Source = null;
                return false;
            }
            else {
                image.Tag = overlay;
                image.Source = overlay.Bitmap;
                image.Opacity = overlay.Opacity;

                Control.ScaleOverlay(editing);
                return true;
            }
        }

        #endregion
        #region ShowVariable

        /// <summary>
        /// Sets the <see cref="ShownVariable"/> and <see cref="ShownVariableFlags"/> properties to
        /// the specified values.</summary>
        /// <param name="variable">
        /// The new value for the <see cref="ShownVariable"/> property.</param>
        /// <param name="flags">
        /// The new value for the <see cref="ShownVariableFlags"/> property.</param>
        /// <exception cref="ObjectDisposedException">
        /// The <see cref="MapView"/> object has been disposed of.</exception>
        /// <remarks><para>
        /// <b>ShowVariable</b> sets <see cref="ShownVariable"/> to a null reference and <see
        /// cref="ShownVariableFlags"/> to zero in the following cases:
        /// </para><list type="bullet"><item>
        /// <paramref name="variable"/> is a null reference.
        /// </item><item>
        /// <paramref name="flags"/> contains neither the <see cref="VariableDisplay.Basic"/> flag
        /// nor the <see cref="VariableDisplay.Modifier"/> flag.
        /// </item><item>
        /// <paramref name="flags"/> contains neither the <see cref="VariableDisplay.Numbers"/> flag
        /// nor the <see cref="VariableDisplay.Shades"/> flag.
        /// </item></list><para>
        /// In any case, <b>ShowVariable</b> redraws the <see cref="MapView"/> and saves all
        /// persistent display options to the associated <see cref="MapViewOptions"/>.
        /// </para></remarks>

        public void ShowVariable(VariableClass variable, VariableDisplay flags) {
            if (IsDisposed)
                ThrowHelper.ThrowObjectDisposedException(Id);

            if (variable == null || flags == 0) {
                ShownVariable = null;
                ShownVariableFlags = 0;
            } else {
                ShownVariable = variable;
                ShownVariableFlags = flags;
            }

            // VariableDrawer must reacquire value range
            Control.Renderer.VariableDrawer.ValueRangeChanged = true;

            Redraw();
            SaveOptions();
        }

        #endregion
        #region SiteToView

        /// <summary>
        /// Converts the specified map location to the corresponding display location.</summary>
        /// <param name="site">
        /// The coordinated of the <see cref="Site"/> to convert.</param>
        /// <returns>
        /// The display location at the center of the specified <paramref name="site"/>, in
        /// device-independent pixels, relative to the upper-left corner of the entire map display.
        /// </returns>
        /// <remarks>
        /// <b>SiteToView</b> adjusts the returned display location for the <see cref="MapBorder"/>
        /// but not for the current scroll position.</remarks>

        public PointD SiteToView(PointI site) {
            return MapGrid.GridToDisplay(site) + MapBorder;
        }

        #endregion
        #region ViewToSite(Double, Double)

        /// <overloads>
        /// Converts display coordinates to the corresponding map coordinates.</overloads>
        /// <summary>
        /// Converts the specified display coordinates to the corresponding map location.</summary>
        /// <param name="x">
        /// The horizontal display coordinate to convert, in device-independent pixels.</param>
        /// <param name="y">
        /// The vertical display coordinate to convert, in device-independent pixels.</param>
        /// <returns><para>
        /// The coordinates of the <see cref="Site"/> at the specified <paramref name="x"/> and
        /// <paramref name="y"/> coordinates.
        /// </para><para>-or-</para><para>
        /// <see cref="Site.InvalidLocation"/> if there is no such <see cref="Site"/>.
        /// </para></returns>
        /// <remarks><para>
        /// <b>ViewToSite</b> adjusts the specified <paramref name="x"/> and <paramref name="y"/>
        /// coordinates for the <see cref="MapBorder"/> but not for the current scroll position.
        /// </para><para>
        /// <paramref name="x"/> and <paramref name="y"/> must be specified relative to the
        /// upper-left corner of the entire map display. Negative or otherwise invalid coordinates
        /// are acceptable.</para></remarks>

        public PointI ViewToSite(double x, double y) {
            return MapGrid.DisplayToGrid(x - MapBorder.X, y - MapBorder.Y);
        }

        #endregion
        #region ViewToSite(PointD)

        /// <overloads>
        /// Converts display coordinates to the corresponding map coordinates.</overloads>
        /// <summary>
        /// Converts the specified display location to the corresponding map location.</summary>
        /// <param name="pixel">
        /// The display location to convert, in device-independent pixels, relative to the
        /// upper-left corner of the entire map display. Negative or otherwise invalid coordinates
        /// are acceptable.</param>
        /// <returns><para>
        /// The coordinates of the <see cref="Site"/> at the specified <paramref name="pixel"/>.
        /// </para><para>-or-</para><para>
        /// <see cref="Site.InvalidLocation"/> if there is no such <see cref="Site"/>.
        /// </para></returns>
        /// <remarks>
        /// <b>ViewToSite</b> adjusts the specified <paramref name="pixel"/> location for the <see
        /// cref="MapBorder"/> but not for the current scroll position.</remarks>

        public PointI ViewToSite(PointD pixel) {
            return MapGrid.DisplayToGrid(pixel - MapBorder);
        }

        #endregion
        #region ViewToSite(RectD)

        /// <summary>
        /// Converts the specified display region to the corresponding map region.</summary>
        /// <param name="pixels">
        /// The display region to convert, in device-independent pixels, relative to the upper-left
        /// corner of the entire map display. Negative or otherwise invalid coordinates are
        /// acceptable.</param>
        /// <returns>
        /// A <see cref="RectI"/> containing the coordinates of every <see cref="Site"/> that is
        /// fully or partially covered by the specified <paramref name="pixels"/>. The rectangle may
        /// be empty if the <paramref name="pixels"/> region does not cover any sites.</returns>
        /// <remarks><para>
        /// <b>ViewToSite</b> adjusts the specified <paramref name="pixels"/> area for the <see
        /// cref="MapBorder"/> but not for the current scroll position.
        /// </para><para>
        /// To ensure that the returned <see cref="RectI"/> includes any sites that are only
        /// partially covered by the specified <paramref name="pixels"/>, <b>ViewToSite</b> extends
        /// the computed map region by one row or column in all four directions.</para></remarks>

        public RectI ViewToSite(RectD pixels) {

            // compute corners of grid region, adjusted by border
            PointI upperLeft = MapGrid.DisplayToGridClipped(pixels.TopLeft - MapBorder);
            PointI lowerRight = MapGrid.DisplayToGridClipped(pixels.BottomRight - MapBorder);

            // extend region for partially covered polygons,
            // then restrict the result to current map size
            int left = Math.Max(upperLeft.X - 1, 0);
            int top = Math.Max(upperLeft.Y - 1, 0);
            int width = Math.Min(lowerRight.X + 2, MapGrid.Size.Width) - left;
            int height = Math.Min(lowerRight.Y + 2, MapGrid.Size.Height) - top;

            return new RectI(left, top, width, height);
        }

        #endregion
        #endregion
        #region SelectedSiteChanged

        /// <summary>
        /// Occurs when the <see cref="SelectedSite"/> property is changed.</summary>
        /// <remarks><para>
        /// <b>SelectedSiteChanged</b> is raised when the <see cref="SelectedSite"/> property is
        /// changed after the object has been constructed.
        /// </para><para>
        /// This event is raised even if the <b>SelectedSite</b> property is set to a value that is
        /// identical to its old value. Note that all invalid locations are interpreted as the same
        /// value, namely <see cref="Site.InvalidLocation"/>.</para></remarks>

        public event EventHandler SelectedSiteChanged;

        #endregion
        #region IDisposable Members

        /// <summary>
        /// Releases all resources used by the <see cref="MapView"/> object.</summary>
        /// <remarks><para>
        /// <b>Dispose</b> removes the associated <see cref="Control"/> from its current <see
        /// cref="FrameworkElement.Parent"/> and calls <see cref="CatalogManager.Dispose"/> to
        /// dispose of the bitmap catalog and to set the <see cref="CatalogManager.IsDisposed"/>
        /// flag.
        /// </para><para>
        /// <b>Dispose</b> does <em>not</em> remove this <see cref="MapView"/> object from the <see
        /// cref="MapViewManager.MapViews"/> collection of the current <see cref="MapViewManager"/>,
        /// as that would interfere with the automatic disposal of all <b>MapViews</b> when the <see
        /// cref="MapViewManager"/> instance is disposed of.
        /// </para><para>
        /// Use <see cref="MapViewManager.CloseView"/> to close a map view, rather than calling this
        /// method directly.</para></remarks>

        public override void Dispose() {
            try {
                // remove output control from parent, if any
                ContentControl parent = Control.Parent as ContentControl;
                if (parent != null) parent.Content = null;
            }
            finally {
                // complete disposal
                base.Dispose();
            }
        }

        #endregion
        #region IKeyedValue Members

        /// <summary>
        /// Gets the identifier of the <see cref="MapView"/>.</summary>
        /// <value>
        /// The value of the <see cref="Id"/> property.</value>

        string IKeyedValue<String>.Key {
            [DebuggerStepThrough]
            get { return Id; }
        }

        #endregion
    }
}
