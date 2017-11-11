using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using Tektosyne;
using Tektosyne.Geometry;
using Tektosyne.Windows;
using Hexkit.Scenario;

namespace Hexkit.Graphics {

    /// <summary>
    /// Manages scalable bitmap catalogs and map dimensions.</summary>
    /// <remarks><para>
    /// <b>CatalogManager</b> provides scalable bitmap catalogs and map dimensions for the derived
    /// classes <see cref="MapViewManager"/> and <see cref="MapView"/>.
    /// </para><list type="bullet"><item>
    /// <see cref="CatalogManager.MapGrid"/> describes the geometric structure of the game map. Its
    /// display size is scaled to the tile size of the associated <b>Catalog</b>.
    /// </item><item>
    /// <see cref="CatalogManager.Catalog"/> contains all bitmap tiles used by the current scenario,
    /// scaled to any desired size.
    /// </item><item>
    /// <see cref="CatalogManager.TileCopyBuffer"/> and <see cref="CatalogManager.TileDrawBuffer"/>
    /// serves as intermediate buffers for drawing or copying single bitmap tiles.
    /// </item></list><para>
    /// All bitmaps managed by <b>CatalogManager</b> use a <see cref="BitmapSource.Format"/> of <see
    /// cref="PixelFormats.Pbgra32"/>.</para></remarks>

    public abstract class CatalogManager: IDisposable {
        #region Private Fields

        // property backers
        private bool _isDisposed;
        private PolygonGrid _mapGrid;
        private WriteableBitmap _catalog;

        // bitmap catalog test window
        private Window _catalogWindow;

        #endregion
        #region Internal Properties
        #region GridElementGeometry

        /// <summary>
        /// Gets a <see cref="PathGeometry"/> for the outline of one polygonal <see
        /// cref="PolygonGrid.Element"/> of the scaled <see cref="MapGrid"/>.</summary>
        /// <value>
        /// A frozen <see cref="PathGeometry"/> created from the polygon outline of one <see
        /// cref="PolygonGrid.Element"/> of the current <see cref="MapGrid"/>.</value>
        /// <remarks>
        /// <b>GridElementGeometry</b> is set by <see cref="ResizePolygon"/>.</remarks>

        internal PathGeometry GridElementGeometry { get; private set; }

        #endregion
        #region RowBits

        /// <summary>
        /// The number of bits required to represent <see cref="RowSize"/>.</summary>
        /// <remarks>
        /// <see cref="RowSize"/> equals 2^<b>RowBits</b>.</remarks>

        internal const int RowBits = 4;

        #endregion
        #region RowCount

        /// <summary>
        /// Gets the number of tile rows in the <see cref="Catalog"/>.</summary>
        /// <value>
        /// The number of tile rows in the current <see cref="Catalog"/>. This value is at least
        /// one.</value>
        /// <remarks>
        /// Each <see cref="Catalog"/> row contains <see cref="RowSize"/> tiles, except for the
        /// bottom row which may contain between one and <b>RowSize</b> tiles.</remarks>

        internal int RowCount {
            get { return GetTileRow(TileCount - 1) + 1; }
        }

        #endregion
        #region RowSize

        /// <summary>
        /// The number of bitmap tiles in a <see cref="Catalog"/> row.</summary>
        /// <remarks>
        /// The <see cref="Catalog"/> bitmap contains 16 tiles per row, except for the last row
        /// which may be incomplete.</remarks>

        internal const int RowSize = 1 << RowBits;

        #endregion
        #region RowMask

        /// <summary>
        /// The bit mask required to isolate numbers up to <see cref="RowSize"/>.</summary>
        /// <remarks>
        /// The expression (<em>n</em> &amp; <b>RowMask</b>) evaluates to a number in [0, <see
        /// cref="RowSize"/>[ for any integer <em>n</em>.</remarks>

        internal const int RowMask = RowSize - 1;

        #endregion
        #region TileCopyBuffer

        /// <summary>
        /// Gets a buffer that can hold one bitmap tile in the <see cref="Catalog"/>, as a <see
        /// cref="WriteableBitmap"/>.</summary>
        /// <value>
        /// A <see cref="WriteableBitmap"/> that can hold one bitmap tile in the current <see
        /// cref="Catalog"/>.</value>
        /// <remarks>
        /// <b>TileCopyBuffer</b> always holds <see cref="TileWidth"/> by <see cref="TileHeight"/>
        /// pixels, i.e. exactly one bitmap tile in the current <see cref="Catalog"/>. It is
        /// intended as a transfer buffer for single bitmap tiles.</remarks>

        internal WriteableBitmap TileCopyBuffer { get; private set; }

        #endregion
        #region TileCount

        /// <summary>
        /// Gets the total number of bitmap tiles in the <see cref="Catalog"/>.</summary>
        /// <value>
        /// The total number of bitmap tiles in the current <see cref="Catalog"/>. This value is at
        /// least one.</value>
        /// <remarks>
        /// <b>TileCount</b> returns at most the product of <see cref="RowSize"/> and <see
        /// cref="RowCount"/>; possibly less if the bottom tile row is incomplete.</remarks>

        internal int TileCount { get; private set; }

        #endregion
        #region TileDrawBuffer

        /// <summary>
        /// Gets a buffer that can hold one bitmap tile in the <see cref="Catalog"/>, as a <see
        /// cref="RenderTargetBitmap"/>.</summary>
        /// <value>
        /// A <see cref="RenderTargetBitmap"/> that can hold one bitmap tile in the current <see
        /// cref="Catalog"/>.</value>
        /// <remarks>
        /// <b>TileDrawBuffer</b> always holds <see cref="TileWidth"/> by <see cref="TileHeight"/>
        /// pixels, i.e. exactly one bitmap tile in the current <see cref="Catalog"/>. It is
        /// intended as a transfer buffer for single bitmap tiles.</remarks>

        internal RenderTargetBitmap TileDrawBuffer { get; private set; }

        #endregion
        #region TileHeight

        /// <summary>
        /// Gets the height of one bitmap tile in the <see cref="Catalog"/>.</summary>
        /// <value>
        /// The visual height of one bitmap tile in the current <see cref="Catalog"/>.</value>
        /// <remarks>
        /// <b>TileHeight</b> returns the <see cref="Rect.Height"/> of an <see
        /// cref="PolygonGrid.Element"/> in the current <see cref="MapGrid"/>, rounded up to the
        /// nearest integer.</remarks>

        internal int TileHeight {
            [DebuggerStepThrough]
            get { return Fortran.Ceiling(MapGrid.Element.Bounds.Height); }
        }

        #endregion
        #region TileWidth

        /// <summary>
        /// Gets the width of one bitmap tile in the <see cref="Catalog"/>.</summary>
        /// <value>
        /// The visual width of one bitmap tile in the current <see cref="Catalog"/>.</value>
        /// <remarks>
        /// <b>TileWidth</b> returns the <see cref="Rect.Width"/> of an <see
        /// cref="PolygonGrid.Element"/> in the current <see cref="MapGrid"/>, rounded up to the
        /// nearest integer.</remarks>

        internal int TileWidth {
            [DebuggerStepThrough]
            get { return Fortran.Ceiling(MapGrid.Element.Bounds.Width); }
        }

        #endregion
        #endregion
        #region Catalog

        /// <summary>
        /// Gets the catalog of bitmap tiles for the current scenario.</summary>
        /// <value>
        /// A <see cref="WriteableBitmap"/> containing all tiles referenced by any <see
        /// cref="Scenario.EntityClass"/> in the current <see cref="Scenario.EntitySection"/>.
        /// </value>
        /// <remarks><para>
        /// The <b>Catalog</b> bitmap is divided into rectangular bitmap tiles of equal size, namely
        /// <see cref="TileWidth"/> by <see cref="TileHeight"/> pixels. The tiles are arranged into
        /// <see cref="RowSize"/> columns and an unlimited number of rows. Tile indices start with
        /// zero at the top left tile and increase first across columns, then across rows.
        /// </para><para>
        /// When inherited by a <see cref="MapViewManager"/> object, <b>Catalog</b> returns a <see
        /// cref="WriteableBitmap"/> that contains all tiles at their original size.
        /// </para><para>
        /// When inherited by a <see cref="MapView"/> object, <b>Catalog</b> returns a <see
        /// cref="WriteableBitmap"/> that contains the same tiles at the current <see
        /// cref="MapView.Scale"/>.
        /// </para><para>
        /// The <b>Catalog</b> bitmap is created by <see cref="CreateCatalog"/> and initially
        /// unfrozen, but will be frozen once all tiles have been created.</para></remarks>

        public WriteableBitmap Catalog {
            [DebuggerStepThrough]
            get { return this._catalog; }
        }

        #endregion
        #region MapGrid

        /// <summary>
        /// Gets the <see cref="PolygonGrid"/> that represents the game map.</summary>
        /// <value>
        /// The read-only <see cref="PolygonGrid"/> that describes the geometric structure of the
        /// game map.</value>
        /// <remarks><para>
        /// The <see cref="RegularPolygon.Bounds"/> of an <see cref="PolygonGrid.Element"/> in the
        /// <b>MapGrid</b> equal the dimensions of each tile in the <see cref="Catalog"/> bitmap.
        /// The dimensions are rounded up to the nearest integer, as returned by the <see
        /// cref="TileWidth"/> and <see cref="TileHeight"/> properties.
        /// </para><para>
        /// When inherited by the <see cref="MapViewManager"/> class, an <b>Element</b> in the
        /// <b>MapGrid</b> has the same size as the standard <see cref="AreaSection.MapGrid"/>.
        /// </para><para>
        /// When inherited by the <see cref="MapView"/> class, the size of an <b>Element</b> is
        /// scaled by the current <see cref="MapView.Scale"/>.</para></remarks>

        public PolygonGrid MapGrid {
            [DebuggerStepThrough]
            get { return this._mapGrid.AsReadOnly(); }
        }

        #endregion
        #region Protected Methods
        #region CreateCatalog

        /// <summary>
        /// Creates a new <see cref="Catalog"/> bitmap for the specified <see cref="TileCount"/>.
        /// </summary>
        /// <param name="tileCount">
        /// The new value for the <see cref="TileCount"/> property.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="tileCount"/> is equal to or less than zero.</exception>
        /// <exception cref="PropertyValueException">
        /// <see cref="MapGrid"/> is a null reference. Use <see cref="SetMapGrid"/> to initialize
        /// this property.</exception>
        /// <remarks>
        /// <b>CreateCatalog</b> updates the <see cref="TileCount"/> and <see cref="RowCount"/>
        /// properties with the specified <paramref name="tileCount"/>, and then recreates the <see
        /// cref="Catalog"/> bitmap at the required size. Any existing bitmap contents are lost.
        /// </remarks>

        protected void CreateCatalog(int tileCount) {
            if (tileCount <= 0)
                ThrowHelper.ThrowArgumentOutOfRangeException(
                    "tileCount", tileCount, Tektosyne.Strings.ArgumentNotPositive);

            if (MapGrid == null)
                ThrowHelper.ThrowPropertyValueException("MapGrid", Tektosyne.Strings.PropertyNull);

            // hide catalog window if shown
            if (Catalog != null) HideCatalog();

            // set tile count (also sets RowCount)
            TileCount = tileCount;

            // recreate catalog bitmap
            this._catalog = new WriteableBitmap(
                TileWidth * RowSize, TileHeight * RowCount, 96, 96, PixelFormats.Pbgra32, null);
        }

        #endregion
        #region GetTileColumn

        /// <summary>
        /// Returns the column of the specified <see cref="Catalog"/> tile index.</summary>
        /// <param name="index">
        /// The tile index in the <see cref="Catalog"/> bitmap whose column to return.</param>
        /// <returns>
        /// The <see cref="Catalog"/> column of the specified <paramref name="index"/>.</returns>
        /// <remarks>
        /// <b>GetTileColumn</b> always returns a value in [0, <see cref="RowSize"/>[.</remarks>

        protected static int GetTileColumn(int index) {
            return (index & RowMask);
        }

        #endregion
        #region GetTileRow

        /// <summary>
        /// Returns the row of the specified <see cref="Catalog"/> tile index.</summary>
        /// <param name="index">
        /// The tile index in the <see cref="Catalog"/> bitmap whose row to return.</param>
        /// <returns>
        /// The <see cref="Catalog"/> row of the specified <paramref name="index"/>.</returns>
        /// <remarks>
        /// <b>GetTileRow</b> always returns a non-negative value.</remarks>

        protected static int GetTileRow(int index) {
            return (index >> RowBits);
        }

        #endregion
        #region ResizePolygon

        /// <summary>
        /// Sets the specified side length for a <see cref="MapGrid"/> polygon.</summary>
        /// <param name="length">
        /// The new value for the <see cref="RegularPolygon.Length"/> property of an <see
        /// cref="PolygonGrid.Element"/> in the <see cref="MapGrid"/>.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="length"/> is equal to or less than zero.</exception>
        /// <exception cref="PropertyValueException">
        /// <see cref="MapGrid"/> is a null reference. Use <see cref="SetMapGrid"/> to initialize
        /// this property.</exception>
        /// <remarks><para>
        /// <b>ResizePolygon</b> sets the <see cref="PolygonGrid.Element"/> property of the current
        /// <see cref="MapGrid"/> to a new <see cref="RegularPolygon"/> instance with the specified
        /// <paramref name="length"/> and otherwise identical property values.
        /// </para><para>
        /// <b>ResizePolygon</b> also sets the <see cref="GridElementGeometry"/> property, and
        /// recreates the <see cref="TileCopyBuffer"/> and <see cref="TileDrawBuffer"/> bitmaps with
        /// the resulting <see cref="TileWidth"/> and <see cref="TileHeight"/>. Any existing bitmap
        /// contents are lost.</para></remarks>

        protected void ResizePolygon(double length) {
            if (MapGrid == null)
                ThrowHelper.ThrowPropertyValueException("MapGrid", Tektosyne.Strings.PropertyNull);

            // create identical but resized polygon
            this._mapGrid.Element = MapGrid.Element.Resize(length);

            // create element outline, starting at top left corner
            GridElementGeometry = new PathGeometry() { Figures = { MapGrid.Element.ToFigure() } };
            GridElementGeometry.Freeze();

            // create tile buffers for one grid element
            TileCopyBuffer = CreateTileCopyBuffer(0, 0);
            TileDrawBuffer = CreateTileDrawBuffer(0, 0);
        }

        #endregion
        #region SetMapGrid

        /// <summary>
        /// Sets the <see cref="MapGrid"/> property.</summary>
        /// <param name="grid">
        /// The new value for the <see cref="MapGrid"/> property.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="grid"/> is a null reference.</exception>
        /// <remarks><para>
        /// <b>SetMapGrid</b> assigns a copy of the specified <paramref name="grid"/> to the <see
        /// cref="MapGrid"/> property, so that both objects can be changed independently.
        /// </para><para>
        /// <b>SetMapGrid</b> also recreates the <see cref="TileCopyBuffer"/> and <see
        /// cref="TileDrawBuffer"/> bitmaps with the resulting <see cref="TileWidth"/> and <see
        /// cref="TileHeight"/>. Any existing bitmap contents are lost.</para></remarks>

        protected void SetMapGrid(PolygonGrid grid) {
            if (grid == null)
                ThrowHelper.ThrowArgumentNullException("grid");

            this._mapGrid = new PolygonGrid(grid);

            // create tile buffers for one grid element
            TileCopyBuffer = CreateTileCopyBuffer(0, 0);
            TileDrawBuffer = CreateTileDrawBuffer(0, 0);
        }

        #endregion
        #endregion
        #region CreateTileCopyBuffer

        /// <summary>
        /// Creates a buffer that can hold at least one bitmap tile in the <see cref="Catalog"/>, as
        /// a <see cref="WriteableBitmap"/>.</summary>
        /// <param name="deltaWidth">
        /// An optional increment to the <see cref="BitmapSource.PixelWidth"/> of the returned <see
        /// cref="WriteableBitmap"/>.</param>
        /// <param name="deltaHeight">
        /// An optional increment to the <see cref="BitmapSource.PixelHeight"/> of the returned <see
        /// cref="WriteableBitmap"/>.</param>
        /// <returns>
        /// A <see cref="WriteableBitmap"/> that can hold one bitmap tile in the current <see
        /// cref="Catalog"/>, plus the specified <paramref name="deltaWidth"/> and <paramref
        /// name="deltaHeight"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="deltaWidth"/> or <paramref name="deltaHeight"/> is negative.</exception>
        /// <remarks><para>
        /// <b>CreateTileCopyBuffer</b> creates a <see cref="WriteableBitmap"/> whose pixel
        /// dimensions equal exactly one tile in the current <see cref="Catalog"/>, plus the
        /// specified <paramref name="deltaWidth"/> and <paramref name="deltaHeight"/>.
        /// </para><para>
        /// The resolution of the returned bitmap is 96 dpi in both dimensions, its <see
        /// cref="BitmapSource.Format"/> is <see cref="PixelFormats.Pbgra32"/>, and its <see
        /// cref="BitmapSource.Palette"/> is a null reference.</para></remarks>

        public WriteableBitmap CreateTileCopyBuffer(int deltaWidth, int deltaHeight) {
            if (deltaWidth < 0)
                ThrowHelper.ThrowArgumentOutOfRangeException(
                    "deltaWidth", deltaWidth, Tektosyne.Strings.ArgumentNegative);

            if (deltaHeight < 0)
                ThrowHelper.ThrowArgumentOutOfRangeException(
                    "deltaHeight", deltaHeight, Tektosyne.Strings.ArgumentNegative);

            return new WriteableBitmap(TileWidth + deltaWidth,
                TileHeight + deltaHeight, 96, 96, PixelFormats.Pbgra32, null);
        }

        #endregion
        #region CreateTileDrawBuffer

        /// <summary>
        /// Creates a buffer that can hold at least one bitmap tile in the <see cref="Catalog"/>, as
        /// a <see cref="RenderTargetBitmap"/>.</summary>
        /// <param name="deltaWidth">
        /// An optional increment to the <see cref="BitmapSource.PixelWidth"/> of the returned <see
        /// cref="WriteableBitmap"/>.</param>
        /// <param name="deltaHeight">
        /// An optional increment to the <see cref="BitmapSource.PixelHeight"/> of the returned <see
        /// cref="WriteableBitmap"/>.</param>
        /// <returns>
        /// A <see cref="RenderTargetBitmap"/> that can hold one bitmap tile in the current <see
        /// cref="Catalog"/>, plus the specified <paramref name="deltaWidth"/> and <paramref
        /// name="deltaHeight"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="deltaWidth"/> or <paramref name="deltaHeight"/> is negative.</exception>
        /// <remarks><para>
        /// <b>CreateTileCopyBuffer</b> creates a <see cref="RenderTargetBitmap"/> whose pixel
        /// dimensions equal exactly one tile in the current <see cref="Catalog"/>, plus the
        /// specified <paramref name="deltaWidth"/> and <paramref name="deltaHeight"/>.
        /// </para><para>
        /// The resolution of the returned bitmap is 96 dpi in both dimensions, and its <see
        /// cref="BitmapSource.Format"/> is <see cref="PixelFormats.Pbgra32"/>.</para></remarks>

        public RenderTargetBitmap CreateTileDrawBuffer(int deltaWidth, int deltaHeight) {
            if (deltaWidth < 0)
                ThrowHelper.ThrowArgumentOutOfRangeException(
                    "deltaWidth", deltaWidth, Tektosyne.Strings.ArgumentNegative);

            if (deltaHeight < 0)
                ThrowHelper.ThrowArgumentOutOfRangeException(
                    "deltaHeight", deltaHeight, Tektosyne.Strings.ArgumentNegative);

            return new RenderTargetBitmap(TileWidth + deltaWidth,
                TileHeight + deltaHeight, 96, 96, PixelFormats.Pbgra32);
        }

        #endregion
        #region GetTileBounds

        /// <summary>
        /// Gets the bounding rectangle of the specified <see cref="Catalog"/> tile.</summary>
        /// <param name="index">
        /// The tile index in the <see cref="Catalog"/> bitmap whose bounds to return.</param>
        /// <returns>
        /// The bounding rectangle of the <see cref="Catalog"/> tile at the specified <paramref
        /// name="index"/>.</returns>

        public RectI GetTileBounds(int index) {
            int width = TileWidth, height = TileHeight;
            return new RectI(
                width * GetTileColumn(index),
                height * GetTileRow(index), width, height);
        }

        #endregion
        #region GetTileLocation

        /// <summary>
        /// Gets the location of the specified <see cref="Catalog"/> tile.</summary>
        /// <param name="index">
        /// The tile index in the <see cref="Catalog"/> bitmap whose location to return.</param>
        /// <returns>
        /// The upper-left corner of the <see cref="Catalog"/> tile at the specified <paramref
        /// name="index"/>.</returns>

        public PointI GetTileLocation(int index) {
            return new PointI(
                TileWidth * GetTileColumn(index),
                TileHeight * GetTileRow(index));
        }

        #endregion
        #region DrawTile

        /// <summary>
        /// Draws the <see cref="Catalog"/> tile with the specified index.</summary>
        /// <param name="context">
        /// The <see cref="DrawingContext"/> for the drawing.</param>
        /// <param name="index">
        /// The tile index in the <see cref="Catalog"/> bitmap whose contents to draw.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="context"/> is a null reference.</exception>
        /// <exception cref="ObjectDisposedException">
        /// The <see cref="CatalogManager"/> object has been disposed of.</exception>
        /// <remarks><para>
        /// <b>DrawTile</b> draws the <see cref="Catalog"/> tile at the specified <paramref
        /// name="index"/> to pixel location (1,1) within the specified <paramref name="context"/>.
        /// </para><para>
        /// The tile is clipped by and surrounded with the <see cref="RegularPolygon"/> outline of
        /// an <see cref="PolygonGrid.Element"/> in the current <see cref="MapGrid"/>.
        /// </para></remarks>

        public void DrawTile(DrawingContext context, int index) {
            if (IsDisposed)
                ThrowHelper.ThrowObjectDisposedException(null);
            if (context == null)
                ThrowHelper.ThrowArgumentNullException("context");

            // compute source & target rectangles
            RectI source = GetTileBounds(index);
            Rect target = new Rect(1, 1, source.Width, source.Height);

            // copy catalog tile to buffer
            TileCopyBuffer.Lock();
            TileCopyBuffer.Read(0, 0, Catalog, source);
            TileCopyBuffer.Unlock();

            // get scaled polygonal outline, centered on tile
            PointD offset = new PointD((source.Width + 2) / 2.0, (source.Height + 2) / 2.0);
            var geometry = new PathGeometry() { Figures = { MapGrid.Element.ToFigure(offset) } };
            geometry.Freeze();

            // draw catalog tile, clipped to polygonal region
            context.PushClip(geometry);
            context.DrawImage(TileCopyBuffer, target);
            context.Pop();

            // draw black polygon outline
            context.DrawGeometry(null, new Pen(Brushes.Black, 1.0), geometry);
        }

        #endregion
        #region DrawTileToBuffer

        /// <summary>
        /// Draws the <see cref="Catalog"/> tile with the specified index to the <see
        /// cref="TileCopyBuffer"/>.</summary>
        /// <param name="index">
        /// The tile index in the <see cref="Catalog"/> bitmap whose contents to draw.</param>
        /// <exception cref="ObjectDisposedException">
        /// The <see cref="CatalogManager"/> object has been disposed of.</exception>
        /// <remarks>
        /// <b>DrawTile</b> draws the <see cref="Catalog"/> tile at the specified <paramref
        /// name="index"/> to the <see cref="TileCopyBuffer"/>. Any existing buffer contents are
        /// overwritten.</remarks>

        public void DrawTileToBuffer(int index) {
            if (IsDisposed)
                ThrowHelper.ThrowObjectDisposedException(null);

            // compute source rectangle
            RectI source = GetTileBounds(index);

            // copy catalog tile to buffer
            TileCopyBuffer.Lock();
            TileCopyBuffer.Read(0, 0, Catalog, source);
            TileCopyBuffer.Unlock();
        }

        #endregion
        #region HideCatalog

        /// <summary>
        /// Closes the <see cref="Window"/> created by <see cref="ShowCatalog"/>.</summary>
        /// <remarks>
        /// <b>HideCatalog</b> is safe to call repeatedly without a preceding <see
        /// cref="ShowCatalog"/> call.</remarks>

        public void HideCatalog() {

            // close & dispose of catalog window
            if (this._catalogWindow != null) {
                this._catalogWindow.Close();
                this._catalogWindow = null;
            }
        }

        #endregion
        #region ShowCatalog

        /// <summary>
        /// Shows the <see cref="Catalog"/> bitmap in an independent scrollable <see cref="Window"/>
        /// with the specified caption.</summary>
        /// <param name="caption">
        /// The text to display in the title bar of the <see cref="Window"/>.</param>
        /// <exception cref="ObjectDisposedException">
        /// The <see cref="CatalogManager"/> object has been disposed of.</exception>
        /// <remarks><para>
        /// <b>ShowCatalog</b> displays an independent <see cref="Window"/> with a scrollable client
        /// area containing the entire <see cref="Catalog"/> bitmap. This method is intended as a
        /// debugging aid.
        /// </para><para>
        /// Use <see cref="HideCatalog"/> to close the catalog window. Calling <b>ShowCatalog</b>
        /// repeatedly activates the existing catalog window, rather than creating a new one.
        /// </para></remarks>

        public void ShowCatalog(string caption) {
            if (IsDisposed)
                ThrowHelper.ThrowObjectDisposedException(null);

            if (this._catalogWindow != null) {

                // active existing catalog window
                if (this._catalogWindow.IsVisible) {
                    this._catalogWindow.WindowState = WindowState.Normal;
                    this._catalogWindow.Activate();
                    return;
                }

                // user already closed the window
                this._catalogWindow.Close();
            }

            // create & show new catalog window
            this._catalogWindow = new ShowImage(Catalog);
            if (!String.IsNullOrEmpty(caption))
                this._catalogWindow.Title = caption;
            this._catalogWindow.Icon = Application.Current.MainWindow.Icon;
            this._catalogWindow.Show();
        }

        #endregion
        #region IDisposable Members
        #region IsDisposed

        /// <summary>
        /// Gets a value indicating whether the <see cref="CatalogManager"/> object has been
        /// disposed of.</summary>
        /// <value>
        /// <c>true</c> if this <see cref="CatalogManager"/> object has been disposed of; otherwise,
        /// <c>false</c>. The default is <c>false</c>.</value>
        /// <remarks>
        /// Any property or method in <b>CatalogManager</b> or derived classes that requires
        /// unmanaged resources will throw an <see cref="ObjectDisposedException"/> if <see
        /// cref="IsDisposed"/> returns <c>true</c>.</remarks>

        public bool IsDisposed {
            [DebuggerStepThrough]
            get { return this._isDisposed; }
        }

        #endregion
        #region Dispose

        /// <summary>
        /// Releases all resources used by the <see cref="CatalogManager"/> object.</summary>
        /// <remarks>
        /// <b>Dispose</b> sets the <see cref="IsDisposed"/> flag, calls <see cref="HideCatalog"/>,
        /// and clears the <see cref="Catalog"/> property.</remarks>

        public virtual void Dispose() {
            this._isDisposed = true;

            HideCatalog();
            this._catalog = null;
        }

        #endregion
        #endregion
    }
}
