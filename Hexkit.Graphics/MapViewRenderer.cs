using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using Tektosyne;
using Tektosyne.Geometry;
using Tektosyne.Windows;

using Hexkit.Options;
using Hexkit.Scenario;
using Hexkit.World;

namespace Hexkit.Graphics {

    /// <summary>
    /// Manages the graphical display of a single <see cref="MapView"/>.</summary>
    /// <remarks><para>
    /// <b>MapViewRenderer</b> renders the viewport of the associated <see cref="MapViewControl"/>.
    /// This provides the graphical display for a given scroll position of a <see cref="MapView"/>.
    /// </para><para>
    /// Clients access <b>MapViewRenderer</b> functionality only through the methods and properties
    /// of the associated <see cref="MapView"/>. Accordingly, most <b>MapViewRenderer</b> methods
    /// and properties do not check their parameters or the current object state.</para></remarks>

    internal sealed class MapViewRenderer: FrameworkElement {
        #region MapViewRenderer(...)

        /// <summary>
        /// Initializes a new instance of the <see cref="MapViewRenderer"/> class.</summary>
        /// <param name="mapView">
        /// The initial value for the <see cref="MapView"/> property.</param>
        /// <param name="control">
        /// The initial value for the <see cref="Control"/> property.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="control"/> is a null reference.</exception>
        /// <remarks>
        /// Use <see cref="MapViewManager.CreateView"/> to create a new <see
        /// cref="Graphics.MapView"/> object and all associated controls.</remarks>

        public MapViewRenderer(MapView mapView, MapViewControl control) {
            if (control == null)
                ThrowHelper.ThrowArgumentNullException("control");

            this._mapView = mapView;
            this._control = control;

            // create helper objects for vector decoration
            this._unitDecorator = new UnitDecorator(this);
            this._variableDrawer = new VariableDrawer(this);
        }

        #endregion
        #region Private Fields

        // construction arguments
        private readonly MapView _mapView;
        private readonly MapViewControl _control;

        // property backers
        private readonly UnitDecorator _unitDecorator;
        private readonly VariableDrawer _variableDrawer;

        // cached performance options
        private bool _bitmapGrid, _opaqueImages;

        // generator for random frame changes
        private readonly MersenneTwister _random = new MersenneTwister();

        #endregion
        #region Control

        /// <summary>
        /// Gets the <see cref="MapViewControl"/> hosting the <see cref="MapViewRenderer"/>.
        /// </summary>
        /// <value>
        /// The <see cref="MapViewControl"/> hosting the <see cref="MapViewRenderer"/>.</value>
        /// <remarks>
        /// <b>Control</b> never returns a null reference. This property never changes once the
        /// object has been constructed.</remarks>

        public MapViewControl Control {
            [DebuggerStepThrough]
            get { return this._control; }
        }

        #endregion
        #region MapView

        /// <summary>
        /// Gets the <see cref="Graphics.MapView"/> whose data is shown.</summary>
        /// <value>
        /// The <see cref="Graphics.MapView"/> associated with the <see cref="MapViewRenderer"/>.
        /// </value>
        /// <remarks>
        /// <b>MapView</b> never returns a null reference. This property never changes once the
        /// object has been constructed.</remarks>

        public MapView MapView {
            [DebuggerStepThrough]
            get { return this._mapView; }
        }

        #endregion
        #region PaintBuffer

        /// <summary>
        /// Gets the paint buffer bitmap on which catalog tiles are drawn.</summary>
        /// <value>
        /// A <see cref="WriteableBitmap"/> for drawing catalog tiles that covers the entire <see
        /// cref="MapViewControl.Viewport"/>.</value>
        /// <remarks>
        /// <see cref="OnRender"/> draws all visible catalog tiles on the <see cref="PaintBuffer"/>
        /// bitmap, then draws this bitmap to the current <see cref="MapViewControl.Viewport"/> and
        /// superimposes any other decoration.</remarks>

        public WriteableBitmap PaintBuffer { get; private set; }

        #endregion
        #region UnitDecorator

        /// <summary>
        /// Gets the <see cref="Graphics.UnitDecorator"/> for the <see cref="MapViewRenderer"/>.
        /// </summary>
        /// <value>
        /// The <see cref="Graphics.UnitDecorator"/> that is permanently associated with the <see
        /// cref="MapViewRenderer"/>.</value>

        public UnitDecorator UnitDecorator {
            [DebuggerStepThrough]
            get { return this._unitDecorator; }
        }

        #endregion
        #region VariableDrawer

        /// <summary>
        /// Gets the <see cref="Graphics.VariableDrawer"/> for the <see cref="MapViewRenderer"/>.
        /// </summary>
        /// <value>
        /// The <see cref="Graphics.VariableDrawer"/> that is permanently associated with the <see
        /// cref="MapViewRenderer"/>.</value>

        public VariableDrawer VariableDrawer {
            [DebuggerStepThrough]
            get { return this._variableDrawer; }
        }

        #endregion
        #region Private Members
        #region AnimateEntity

        /// <summary>
        /// Advances the animation sequence for the specified <see cref="Entity"/>.</summary>
        /// <param name="entity">
        /// The <see cref="Entity"/> whose image to animate.</param>
        /// <returns>
        /// The catalog tile index for the image frame that reflects the new animation phase for the
        /// specified <paramref name="entity"/>.</returns>
        /// <remarks>
        /// <b>AnimateEntity</b> may update the <see cref="Entity.AnimationTicks"/> and <see
        /// cref="Entity.FrameOffset"/> properties of the specified <paramref name="entity"/> to
        /// reflect its new animation phase.</remarks>

        private int AnimateEntity(Entity entity) {

            // retrieve image frame data
            EntityClass displayClass = entity.DisplayClass;
            int index = displayClass.FrameIndex;
            int count = displayClass.FrameCount;

            // show first frame for single-frame entity
            if (count < 2) return index;

            // get currently displayed frame
            int frame = entity.FrameOffset;

            // show selected frame for non-animated entity
            if (displayClass.ImageAnimation == AnimationMode.None)
                return index + Math.Abs(frame) % count;

            // show first frame for non-animated map view
            if (!MapView.Animation) return index;

            // does manager require a frame update?
            long ticks = MapViewManager.Instance.AnimationTicks;
            if (entity.AnimationTicks != ticks) {

                // is non-random animation sequence in progress?
                AnimationSequence sequence = displayClass.ImageSequence;
                bool progress = (frame != 0 && sequence != AnimationSequence.Random);

                // should we continue or restart the sequence?
                if (progress || displayClass.ImageAnimation == AnimationMode.Continuous
                    || _random.Next(3) == 1) {
                    /*
                     * Random sequence selects one random frame, then stops.
                     * For any other sequence, zero indicates a stopped sequence.
                     * 
                     * Forward proceeds 1, 2, .., n-2, n-1, 0.
                     * Backward proceeds -(n-1), -(n-2), .., -2, -1, 0.
                     * Cycle proceeds 1, 2, .., n-2, n-1, -(n-2), -(n-3), .., -2, -1, 0.
                     * 
                     * Negative values are used to signify backward movement.
                     * When displaying such a frame we take its absolute value.
                     */

                    if (sequence == AnimationSequence.Random) {
                        // choose random frame to display next
                        frame = _random.Next(count - 1);
                    } else if (frame == 0) {
                        // start sequence with last or second frame
                        frame = (sequence == AnimationSequence.Backward ? 1 - count : 1);
                    } else if (frame >= count - 1) {
                        // reverse cycle progress or stop sequence
                        frame = (sequence == AnimationSequence.Cycle ? 2 - count : 0);
                    } else
                        ++frame; // proceed in current sequence
                }

                // update entity with new animation state
                entity.AnimationTicks = ticks;
                entity.FrameOffset = frame;
            }

            return index + Math.Abs(frame) % count;
        }

        #endregion
        #region ClipTile

        /// <summary>
        /// Clips the bounds of an image tile to the specified display location and clipping region.
        /// </summary>
        /// <param name="dest"><para>
        /// On input, the location within the entire <see cref="MapView"/> where the upper-left
        /// corner of an image tile should appear.
        /// </para><para>
        /// On output, the location within and relative to the specified <paramref name="region"/>
        /// where the upper-left corner of the returned image tile portion should be drawn.
        /// </para></param>
        /// <param name="region"><para>
        /// On input, the display region within the entire <see cref="MapView"/> to which drawing is
        /// clipped.
        /// </para><para>
        /// On output, the portion of the image tile that should be drawn at the adjusted <paramref
        /// name="dest"/> location when returning <c>true</c>; otherwise, unchanged.</para></param>
        /// <returns>
        /// <c>true</c> if <paramref name="region"/> contains the portion of the image tile to draw;
        /// <c>false</c> if nothing should be drawn.</returns>
        /// <remarks>
        /// The specified <paramref name="region"/> should indicate the portion of the entire <see
        /// cref="MapView"/> that is covered by the current contents of the paint buffer.</remarks>

        private bool ClipTile(ref PointI dest, ref RectI region) {

            // shift destination to clipping region
            dest -= region.Location;

            /*
             * We draw at most the area of one image tile. If the destination
             * is negative, we actually draw only the lower and/or right part
             * of an image tile, and reduce the returned bounds accordingly.
             */

            // negative offset to clipping region
            PointI neg = new PointI(Math.Max(0, -dest.X), Math.Max(0, -dest.Y));

            // reduce destination to positive offset
            dest = new PointI(Math.Max(0, dest.X), Math.Max(0, dest.Y));

            // clip image tile to destination and clipping region
            int width = Math.Min(MapView.TileWidth - neg.X, region.Width - dest.X);
            int height = Math.Min(MapView.TileHeight - neg.Y, region.Height - dest.Y);

            // check if there is anything to draw
            if (width <= 0 || height <= 0) return false;
            region = new RectI(neg.X, neg.Y, width, height);
            return true;
        }

        #endregion
        #region DrawDecoration

        /// <summary>
        /// Draws all decoration except arrows on all map sites in the specified region.</summary>
        /// <param name="context">
        /// The <see cref="DrawingContext"/> for the drawing.</param>
        /// <param name="siteRegion">
        /// The map region containing the coordinates of every <see cref="Site"/> to decorate.
        /// </param>
        /// <remarks>
        /// <b>DrawDecoration</b> draws grid outlines, selection highlights, owner shading, unit
        /// flags, resource gauges, and variable values on every <see cref="Site"/> within the
        /// specified <paramref name="siteRegion"/>, as appropriate.</remarks>

        private void DrawDecoration(DrawingContext context, RectI siteRegion) {

            var sites = MapView.WorldState.Sites;
            var geometry = MapView.ElementGeometry;

            // traverse all specified sites
            for (int x = siteRegion.Left; x < siteRegion.Right; x++)
                for (int y = siteRegion.Top; y < siteRegion.Bottom; y++) {
                    Site site = sites[x, y];

                    // shift origin to center of current site
                    PointD pixel = MapView.SiteToView(site.Location);
                    context.PushTransform(new TranslateTransform(pixel.X, pixel.Y));

                    // brighten polygon in selected region
                    if (MapView.SelectedRegion != null && MapView.SelectedRegion[x, y])
                        context.DrawGeometry(MediaObjects.ShadeLightBrush, null, geometry);

                    // tint polygon with owner color
                    if (MapView.ShowOwner) {
                        Color color = (site.Owner == null ? Colors.Black : site.Owner.Color);
                        context.DrawGeometry(MediaObjects.GetShadeBrush(color), null, geometry);
                    }

                    // draw unit flag near unit stack
                    if (MapView.ShowFlags)
                        UnitDecorator.DrawFlag(context, site);

                    // draw resource gauge below unit stack
                    if (MapView.GaugeResource != null)
                        UnitDecorator.DrawGauge(context, site);

                    // draw variable values as numbers or shades
                    if (MapView.ShownVariable != null && MapView.ShownVariableFlags != 0)
                        VariableDrawer.Draw(context, site);

                    // reset transformation matrix
                    context.Pop();
                }

            // draw polygon outlines for entire grid
            if (!this._bitmapGrid && MapView.ShowGrid)
                context.DrawGeometry(null, MediaObjects.ThickPen, MapView.GridGeometry);
        }

        #endregion
        #region DrawEntities

        /// <summary>
        /// Draws all entities in the specified <see cref="Site"/> to the <see cref="PaintBuffer"/>.
        /// </summary>
        /// <param name="dest">
        /// The location within the entire <see cref="MapView"/> where to draw.</param>
        /// <param name="region">
        /// The display region within the entire <see cref="MapView"/> to which drawing is clipped.
        /// </param>
        /// <param name="site">
        /// The <see cref="Site"/> whose entities to draw.</param>

        private void DrawEntities(PointI dest, RectI region, Site site) {

            // compute image tile portion to draw
            if (!ClipTile(ref dest, ref region)) return;

            // draw all terrains on top of each other
            IList<Entity> entities = site.Terrains;
            for (int i = 0; i < entities.Count; i++)
                if (entities[i].IsVisible)
                    DrawFrame(dest, region, entities[i]);

            // draw topmost unit, if any
            entities = site.Units;
            for (int i = entities.Count - 1; i >= 0; i--)
                if (entities[i].IsVisible) {
                    DrawFrame(dest, region, entities[i]);
                    break;
                }

            // draw all effects on top of all else
            entities = site.Effects;
            for (int i = 0; i < entities.Count; i++)
                if (entities[i].IsVisible)
                    DrawFrame(dest, region, entities[i]);

#if BITMAP_DECORATION
            /*
             * Bitmap decoration was an experimental feature to increase drawing performance.
             * As it turned out, WPF drawing (in DrawDecoration) is exactly as fast as direct
             * bitmap manipulation in these cases, both in debug and release mode.
             */

            // brighten polygon in selected region
            if (MapView.SelectedRegion != null &&
                MapView.SelectedRegion[site.Location.X, site.Location.Y])
                DrawTile(dest, region, (int) CatalogIndex.Shading);

            // tint polygon with owner color
            if (MapView.ShowOwner) {
                Color color = (site.Owner == null ? Colors.Black : site.Owner.Color);
                DrawTile(dest, region, (int) CatalogIndex.Shading, color);
            }
#endif
            // draw polygon outline on top of all entities
            if (this._bitmapGrid && MapView.ShowGrid) {
                if (this._opaqueImages)
                    DrawTile(dest, region, (int) CatalogIndex.Outline, 1);
                else
                    DrawTile(dest, region, (int) CatalogIndex.Outline);
            }
        }

        #endregion
        #region DrawFrame

        /// <summary>
        /// Draws the current animation frame for the specified <see cref="Entity"/> to the <see
        /// cref="PaintBuffer"/>.</summary>
        /// <param name="dest">
        /// The location within the <see cref="PaintBuffer"/> where to draw.</param>
        /// <param name="region">
        /// The bitmap region within the <see cref="CatalogManager.Catalog"/> to copy, relative to
        /// the current animation frame for the specified <paramref name="entity"/>.</param>
        /// <param name="entity">
        /// The <see cref="Entity"/> whose current animation frame to draw.</param>
        /// <remarks>
        /// <b>DrawFrame</b> calls <see cref="AnimateEntity"/> to advance the animation sequence for
        /// the specified <paramref name="entity"/> if necessary.</remarks>

        private void DrawFrame(PointI dest, RectI region, Entity entity) {
            int index = AnimateEntity(entity);

            // use alpha blending only if necessary
            if (!this._opaqueImages || entity.DisplayClass.IsTranslucent)
                DrawTile(dest, region, index);
            else
                DrawTile(dest, region, index, 1);
        }

        #endregion
        #region DrawTile(PointI, RectI, Int32)

        /// <overloads>
        /// Draws the specified <see cref="CatalogManager.Catalog"/> tile to the <see
        /// cref="PaintBuffer"/>.</overloads>
        /// <summary>
        /// Draws the specified <see cref="CatalogManager.Catalog"/> tile to the <see
        /// cref="PaintBuffer"/>, with alpha blending.</summary>
        /// <param name="dest">
        /// The location within the <see cref="PaintBuffer"/> where to draw.</param>
        /// <param name="region">
        /// The bitmap region within the <see cref="CatalogManager.Catalog"/> to copy, relative to
        /// the tile with the specified <paramref name="index"/>.</param>
        /// <param name="index">
        /// The <see cref="CatalogManager.Catalog"/> index of the tile to draw.</param>

        private void DrawTile(PointI dest, RectI region, int index) {

            // add upper-left corner of catalog tile
            region = region.Offset(MapView.GetTileLocation(index));

            // copy visible part of catalog tile to buffer
            PaintBuffer.Overlay(dest.X, dest.Y, MapView.Catalog, region);
        }

        #endregion
        #region DrawTile(PointI, RectI, Int32, Byte)

        /// <summary>
        /// Draws the specified <see cref="CatalogManager.Catalog"/> tile to the <see
        /// cref="PaintBuffer"/>, with the specified alpha channel threshold.</summary>
        /// <param name="dest">
        /// The location within the <see cref="PaintBuffer"/> where to draw.</param>
        /// <param name="region">
        /// The bitmap region within the <see cref="CatalogManager.Catalog"/> to copy, relative to
        /// the tile with the specified <paramref name="index"/>.</param>
        /// <param name="index">
        /// The <see cref="CatalogManager.Catalog"/> index of the tile to draw.</param>
        /// <param name="alpha">
        /// The alpha channel threshold below which <see cref="CatalogManager.Catalog"/> pixels will
        /// be ignored.</param>

        private void DrawTile(PointI dest, RectI region, int index, byte alpha) {

            // add upper-left corner of catalog tile
            region = region.Offset(MapView.GetTileLocation(index));

            // copy visible part of catalog tile to buffer
            PaintBuffer.Overlay(dest.X, dest.Y, MapView.Catalog, region, alpha);
        }

        #endregion
        #region DrawTile(PointI, RectI, Int32, Color)

        /// <summary>
        /// Draws the specified <see cref="CatalogManager.Catalog"/> tile to the <see
        /// cref="PaintBuffer"/>, with alpha blending and color substitution.</summary>
        /// <param name="dest">
        /// The location within the <see cref="PaintBuffer"/> where to draw.</param>
        /// <param name="region">
        /// The bitmap region within the <see cref="CatalogManager.Catalog"/> to copy, relative to
        /// the tile with the specified <paramref name="index"/>.</param>
        /// <param name="index">
        /// The <see cref="CatalogManager.Catalog"/> index of the tile to draw.</param>
        /// <param name="color">
        /// The <see cref="Color"/> whose color channels are substituted for those of all <see
        /// cref="CatalogManager.Catalog"/> pixels, retaining only their alpha channel.</param>

        private void DrawTile(PointI dest, RectI region, int index, Color color) {

            // add upper-left corner of catalog tile
            region = region.Offset(MapView.GetTileLocation(index));

            // copy visible part of catalog tile to buffer
            PaintBuffer.Overlay(dest.X, dest.Y, MapView.Catalog, region, color);
        }

        #endregion
        #region EnsureBufferSize

        /// <summary>
        /// Ensures that the <see cref="PaintBuffer"/> covers the specified minimum size.</summary>
        /// <param name="required">
        /// The new minimum size, in device-independent pixels, for the <see cref="PaintBuffer"/>.
        /// </param>
        /// <returns>
        /// <c>true</c> if the <see cref="PaintBuffer"/> must be cleared by the caller; otherwise,
        /// <c>false</c>.</returns>
        /// <remarks>
        /// To avoid frequent buffer reallocations, <b>EnsureBufferSize</b> grows the <see
        /// cref="PaintBuffer"/> to cover twice its current area or to the specified <paramref
        /// name="required"/> size, whichever is bigger. However, <b>EnsureBufferSize</b> will not
        /// grow the <see cref="PaintBuffer"/> beyond the device-independent size of the current
        /// virtual screen, unless the <paramref name="required"/> size is greater.</remarks>

        private bool EnsureBufferSize(SizeI required) {
            int width, height;
            SizeI estimate = required;

            if (PaintBuffer != null) {
                width = PaintBuffer.PixelWidth;
                height = PaintBuffer.PixelHeight;

                // quit if existing buffer is large enough
                if (width >= required.Width && height >= required.Height)
                    return true;

                // new estimate: double current buffer area
                estimate = new SizeD(width * 1.4, height * 1.4).Round();
            }

            // default to virtual screen size
            SizeI screen = new SizeI(
                Fortran.Ceiling(SystemParameters.VirtualScreenWidth),
                Fortran.Ceiling(SystemParameters.VirtualScreenHeight));

            SizeI minimum = new SizeI(
                Math.Min(screen.Width, estimate.Width),
                Math.Min(screen.Height, estimate.Height));

            // choose maximum of screen size or required size
            width = Math.Max(minimum.Width, required.Width);
            height = Math.Max(minimum.Height, required.Height);

            // reallocate buffer with new size
            PaintBuffer = new WriteableBitmap(width, height, 96, 96, PixelFormats.Pbgra32, null);
            return false;
        }

        #endregion
        #endregion
        #region OnRender

        /// <summary>
        /// Renders the visual content of the <see cref="MapViewRenderer"/>.</summary>
        /// <param name="context">
        /// The <see cref="DrawingContext"/> for the rendering.</param>
        /// <remarks><para>
        /// <b>OnRender</b> calls the base class implementation of <see cref="UIElement.OnRender"/>,
        /// and then immediately returns if the associated <see cref="MapView"/> has not been fully
        /// initialized yet, or already been disposed of.
        /// </para><para>
        /// Otherwise, <b>OnRender</b> redraws the current <see cref="MapViewControl.Viewport"/>.
        /// The <see cref="Graphics.MapView.Extent"/> of the associated <see cref="MapView"/>
        /// outside the current <see cref="MapViewControl.Viewport"/> remains empty.
        /// </para></remarks>

        protected override void OnRender(DrawingContext context) {
            base.OnRender(context);
            if (MapView.Catalog == null || MapView.IsDisposed)
                return;

            // check if there is anything to render
            Rect viewRegion = Control.Viewport;
            if (viewRegion.Width == 0 || viewRegion.Height == 0)
                return;

            // distance from center point to upper-left corner
            double centerX = MapView.TileWidth / 2.0;
            double centerY = MapView.TileHeight / 2.0;

            // compute sites covered by viewport
            RectD region = viewRegion.ToRectD();
            RectI intRegion = region.Circumscribe();
            RectI siteRegion = MapView.ViewToSite(region);

            // resize & clear paint buffer
            bool mustClear = EnsureBufferSize(intRegion.Size);
            PaintBuffer.Lock();
            if (mustClear) PaintBuffer.Clear();

            // cache performance options
            this._bitmapGrid = ApplicationOptions.Instance.View.BitmapGrid;
            this._opaqueImages = ApplicationOptions.Instance.View.OpaqueImages;

            // draw entities of all sites within region
            for (int x = siteRegion.Left; x < siteRegion.Right; x++)
                for (int y = siteRegion.Top; y < siteRegion.Bottom; y++) {
                    Site site = MapView.WorldState.Sites[x, y];

                    // compute upper-left corner of bitmap tile
                    PointD display = MapView.SiteToView(site.Location);
                    PointI pixel = new PointI(
                        Fortran.NInt(display.X - centerX),
                        Fortran.NInt(display.Y - centerY));

                    // draw entities in current site
                    DrawEntities(pixel, intRegion, site);
                }

            // copy paint buffer to viewport
            PaintBuffer.Unlock();
            context.DrawImage(PaintBuffer,
                new Rect(region.Left, region.Top, PaintBuffer.Width, PaintBuffer.Height));

            // draw optional decoration on all sites
            DrawDecoration(context, siteRegion);
        }

        #endregion
    }
}
