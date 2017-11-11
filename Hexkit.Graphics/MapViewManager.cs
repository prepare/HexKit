using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

using Tektosyne;
using Tektosyne.Collections;
using Tektosyne.Geometry;
using Tektosyne.Windows;

using Hexkit.Options;
using Hexkit.Scenario;
using Hexkit.World;

namespace Hexkit.Graphics {

    using MapViewList = KeyedList<String, MapView>;

    /// <summary>
    /// Manages all map views for the current scenario.</summary>
    /// <remarks><para>
    /// <b>MapViewManager</b> manages the unscaled bitmap catalog and related data required by the
    /// current scenario, as well as a collection of <see cref="MapView"/> objects based on
    /// individual <see cref="WorldState"/> objects.
    /// </para><para>
    /// Only a single instance of the <b>MapViewManager</b> class can be created at a time. Use <see
    /// cref="MapViewManager.CreateInstance"/> to instantiate the class, <see
    /// cref="MapViewManager.Instance"/> to retrieve the current instance, and <see
    /// cref="MapViewManager.Dispose"/> to delete the instance.</para></remarks>

    public sealed class MapViewManager: CatalogManager {
        #region MapViewManager(Dispatcher)

        /// <summary>
        /// Initializes a new instance of the <see cref="MapViewManager"/> class.</summary>
        /// <param name="dispatcher">
        /// A <see cref="Dispatcher"/> used to marshal event handler calls to the application's
        /// foreground thread.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="dispatcher"/> is a null reference.</exception>
        /// <exception cref="InvalidOperationException">
        /// The current scenario <see cref="MasterSection.Instance"/> contains invalid data.
        /// </exception>

        private MapViewManager(Dispatcher dispatcher) {
            if (dispatcher == null)
                ThrowHelper.ThrowArgumentNullException("dispatcher");

            // freeze drop shadow for entities
            this._dropShadow.Freeze();

            // clone map grid at original dimensions
            SetMapGrid(MasterSection.Instance.Areas.MapGrid);

            // create standard bitmap catalog
            int frames = CreateCatalog();

            // create animation timer if required
            if (frames > 1) {
                AnimationTimer = new DispatcherTimer(TimeSpan.FromSeconds(1),
                    DispatcherPriority.Background, OnAnimation, dispatcher);

                AnimationTimer.IsEnabled = true;
            }
        }

        #endregion
        #region Private Fields

        // property backers
        private readonly MapViewList _mapViews = new MapViewList(true);
        private readonly DropShadowEffect _dropShadow = new DropShadowEffect();

        // cache for entity classes vs catalog tile index
        private EntityClass[] _catalogClasses;

        // cache for grid outlines sorted by map view scale
        private readonly SortedList<Int32, StreamGeometry>
            _gridGeometries = new SortedList<Int32, StreamGeometry>();

        // cache for element outlines sorted by map view scale
        private readonly SortedList<Int32, StreamGeometry>
            _elementGeometries = new SortedList<Int32, StreamGeometry>();

        #endregion
        #region Internal Properties
        #region AnimationResponse

        /// <summary>
        /// Gets or sets a value indicating how to handle the next <see cref="AnimationTimer"/>
        /// event.</summary>
        /// <value>
        /// A <see cref="TimerResponse"/> value indicating how to handle the next <see
        /// cref="DispatcherTimer.Tick"/> event of the <see cref="AnimationTimer"/>. The default is
        /// <see cref="TimerResponse.Normal"/>.</value>
        /// <remarks>
        /// A <see cref="MapViewControl"/> should set <b>AnimationResponse</b> to <see
        /// cref="TimerResponse.SkipOne"/> or <see cref="TimerResponse.Suspend"/> to stop idle
        /// animation during some lenghty display action.</remarks>

        internal TimerResponse AnimationResponse { get; set; }

        #endregion
        #region AnimationTicks

        /// <summary>
        /// Gets the <see cref="DateTime.Ticks"/> for the last animation phase.</summary>
        /// <value>
        /// The <see cref="DateTime.Ticks"/> when the last animation phase was triggered. The
        /// default is zero.</value>
        /// <remarks><para>
        /// <b>AnimationTicks</b> is set to the current <see cref="DateTime.Ticks"/> count whenever
        /// the <see cref="DispatcherTimer.Tick"/> event for the <see cref="AnimationTimer"/>
        /// occurs.
        /// </para><para>
        /// A <see cref="MapViewControl"/> compares the value of this property to the <see
        /// cref="World.Entity.AnimationTicks"/> values of any animated entities being drawn to
        /// determine whether a new animation phase should be drawn. If so, the entity's
        /// <b>AnimationTicks</b> property is also set to the value of this property.
        /// </para></remarks>

        internal long AnimationTicks { get; private set; }

        #endregion
        #region AnimationTimer

        /// <summary>
        /// The timer that drives <see cref="MapView"/> animations.</summary>
        /// <value>
        /// The <see cref="DispatcherTimer"/> whose <see cref="DispatcherTimer.Tick"/> event drives
        /// animated graphics on all attached <see cref="MapViews"/>.</value>
        /// <remarks>
        /// <b>AnimationTimer</b> returns a null reference if no <see cref="EntityClass"/> defined
        /// by the current <see cref="EntitySection"/> references an <see cref="EntityImage"/> with
        /// two or more animation frames.</remarks>

        internal DispatcherTimer AnimationTimer { get; private set; }

        #endregion
        #region DropShadow

        /// <summary>
        /// Gets the <see cref="DropShadowEffect"/> for all <see cref="MapViews"/>.</summary>
        /// <value>
        /// The <see cref="DropShadowEffect"/> that is applied to any <see cref="Entity"/> whose
        /// <see cref="EntityClass.HasDropShadow"/> flag is <c>true</c>.</value>

        internal DropShadowEffect DropShadow {
            get { return _dropShadow; }
        }

        #endregion
        #endregion
        #region Animation

        /// <summary>
        /// Gets a value indicating whether the <see cref="MapViewManager"/> supports animated
        /// graphics.</summary>
        /// <value>
        /// <c>true</c> if the <see cref="MapViewManager"/> supports animated graphics; otherwise,
        /// <c>false</c>.</value>
        /// <remarks>
        /// <b>Animation</b> returns <c>true</c> exactly if <see cref="AnimationTimer"/> returns a
        /// valid object.</remarks>

        public bool Animation {
            [DebuggerStepThrough]
            get { return (AnimationTimer != null); }
        }

        #endregion
        #region Instance

        /// <summary>
        /// Gets the current instance of the <see cref="MapViewManager"/> class.</summary>
        /// <value>
        /// The current instance of the <see cref="MapViewManager"/> class if one was successfully
        /// initialized and has not yet been disposed of; otherwise, a null reference. The default
        /// is a null reference.</value>
        /// <remarks>
        /// <b>Instance</b> is set by the <see cref="CreateInstance"/> method and cleared by the
        /// <see cref="Dispose"/> method.</remarks>

        public static MapViewManager Instance { get; private set; }

        #endregion
        #region MapViews

        /// <summary>
        /// Gets a list of all available map views.</summary>
        /// <value>
        /// A read-only <see cref="MapViewList"/> containing all available map views. The default is
        /// an empty collection.</value>
        /// <remarks><para>
        /// <b>MapViews</b> never returns a null reference, and its elements are never null
        /// references. The <see cref="MapView.Id"/> strings of all elements are unique.
        /// </para><para>
        /// Call <see cref="CreateView"/> to add elements to the <b>MapViews</b> collection. All
        /// <see cref="MapView"/> objects in this collection will be disposed of automatically when
        /// this <see cref="MapViewManager"/> object is disposed of.</para></remarks>

        public MapViewList MapViews {
            [DebuggerStepThrough]
            get { return this._mapViews.AsReadOnly(); }
        }

        #endregion
        #region Private Methods
        #region CopyEntityClasses

        /// <summary>
        /// Copies all image frames for all entity classes to the bitmap catalog.</summary>
        /// <param name="copying">
        /// <c>true</c> to perform the actual copy; <c>false</c> to set catalog indices and count
        /// animation frames only.</param>
        /// <param name="frames">
        /// Returns the maximum number of animation frames (i.e. frames of animated images) copied
        /// for any single entity class.</param>
        /// <returns>
        /// The total number of catalog tiles used or required to hold all image frames for all
        /// entity classes, plus one tile for the "invalid" icon at index position zero.</returns>
        /// <remarks>
        /// <b>CopyEntityClasses</b> invokes <see cref="CopyEntityClass"/> for each entity class, of
        /// any category, defined in the current <see cref="EntitySection"/>. Please refer to
        /// <b>CopyEntityClass</b> for details on the copying process.</remarks>

        private int CopyEntityClasses(bool copying, out int frames) {

            // reset counters
            frames = 0;
            int index = (int) CatalogIndex.Images;

            // iterate through all entity categories
            EntitySection entitySection = MasterSection.Instance.Entities;
            foreach (EntityCategory category in EntitySection.AllCategories) {
                IList<EntityClass> entities = entitySection.GetEntities(category).Values;

                // copy all image frames for each entity class
                foreach (EntityClass entity in entities) {
                    int copied = CopyEntityClass(copying, entity, ref index);
                    frames = Math.Max(frames, copied);
                }
            }

            // return tile count
            return index;
        }

        #endregion
        #region CopyEntityClass

        /// <summary>
        /// Copies and counts all image frames for the specified <see cref="EntityClass"/>, starting
        /// at the specified catalog tile.</summary>
        /// <param name="copying">
        /// <c>true</c> to perform the actual copy; <c>false</c> to set catalog indices and count
        /// animation frames only.</param>
        /// <param name="entityClass">
        /// The <see cref="EntityClass"/> whose frames to copy.</param>
        /// <param name="index">
        /// The catalog tile index where to copy the first frame of the specified <paramref
        /// name="entityClass"/>. On return, this parameter contains the tile index following the
        /// last frame.</param>
        /// <returns>
        /// The <see cref="EntityClass.FrameCount"/> of the specified <paramref name="entityClass"/>
        /// if its <see cref="EntityClass.ImageAnimation"/> value does not equal <see
        /// cref="AnimationMode.None"/>; otherwise, one. This value is always at least one.
        /// </returns>
        /// <remarks><para>
        /// <b>CopyEntityClass</b> sets the <see cref="EntityClass.FrameIndex"/> property of the
        /// specified <paramref name="entityClass"/> to the specified <paramref name="index"/>.
        /// </para><para>
        /// If the <see cref="EntityClass.FrameCount"/> of the specified <paramref
        /// name="entityClass"/> is zero or if copying fails, <b>CopyEntityClass</b> sets
        /// <b>FrameIndex</b> to zero and returns a value of one. This will cause map views to show
        /// an "invalid" icon for <paramref name="entityClass"/>.</para></remarks>

        private int CopyEntityClass(bool copying, EntityClass entityClass, ref int index) {

            // require at least one frame per entity
            if (entityClass.FrameCount == 0) {
                entityClass.FrameIndex = 0;
                return 1;
            }

            // set catalog tile index
            entityClass.FrameIndex = index;

            // one catalog tile per frame used
            index += entityClass.FrameCount;

            // determine animation frame count
            bool animation = (entityClass.ImageAnimation != AnimationMode.None);
            int animationFrames = (animation ? entityClass.FrameCount : 1);

            // quit immediately if no copying desired
            if (!copying) return animationFrames;

            // overlay image frames with identical indices
            foreach (ImageStackEntry entry in entityClass.ImageStack) {
                EntityImage image = entry.Image.Value;
                if (image == null) continue;

                // iterate over all catalog tiles for this class
                int frameCount = image.Frames.Count;
                for (int i = 0; i < entityClass.FrameCount; i++) {

                    // copy single frame if specified, else current frame
                    int sourceIndex = (entry.SingleFrame < 0 ? i : entry.SingleFrame);
                    ImageFrame frame = image.Frames[sourceIndex % frameCount];

                    // store entity class for current tile index
                    int targetIndex = entityClass.FrameIndex + i;
                    this._catalogClasses[targetIndex] = entityClass;

                    // copy to tile, default to "invalid" icon on failure
                    if (!CopyImageFrame(targetIndex, entry, frame)) {
                        entityClass.FrameIndex = 0;
                        return 1;
                    }
                }
            }

            return animationFrames;
        }

        #endregion
        #region CopyImageFrame

        /// <summary>
        /// Copies the specified <see cref="ImageFrame"/> to the specified catalog tile.</summary>
        /// <param name="index">
        /// The catalog tile index where to copy the specified <paramref name="frame"/>.</param>
        /// <param name="entry">
        /// The <see cref="ImageStackEntry"/> whose <see cref="ImageStackEntry.Image"/> contains the
        /// specified <paramref name="frame"/>.</param>
        /// <param name="frame">
        /// The <see cref="ImageFrame"/> specifying the <see cref="ImageFrame.Source"/> bitmap and
        /// the <see cref="ImageFrame.Bounds"/> to copy.</param>
        /// <returns>
        /// <c>true</c> if copying succeeded; otherwise, <c>false</c>.</returns>

        private bool CopyImageFrame(int index, ImageStackEntry entry, ImageFrame frame) {

            // retrieve image file bitmap
            WriteableBitmap bitmap = frame.Source.Value.Bitmap;
            if (bitmap == null) return false;

            // check for invalid frame bounds
            RectI source = frame.Bounds;
            if (source.X < 0 || source.Y < 0 || source.Width < 1 || source.Height < 1 ||
                source.X + source.Width > bitmap.Width ||
                source.Y + source.Height > bitmap.Height)
                return false;

            // get desired image scaling, unless disabled
            ImageScaling scalingX = ImageScaling.None, scalingY = ImageScaling.None;
            if (!entry.UseUnscaled) {
                scalingX = entry.Image.Value.ScalingX;
                scalingY = entry.Image.Value.ScalingY;
            }

            // determine bounds of catalog tile
            RectI target = GetTileBounds(index);
            RectI tileBounds = new RectI(0, 0, target.Width, target.Height);

            // draw frame with specified display parameters
            TileCopyBuffer.Clear();
            ImageUtility.DrawFrame(TileCopyBuffer, null, tileBounds, bitmap, source,
                scalingX, scalingY, entry.ColorShift, entry.Offset, entry.ScalingVector);

            // perform alpha blending with catalog bitmap
            Catalog.Overlay(target.X, target.Y, TileCopyBuffer, tileBounds);
            return true;
        }

        #endregion
        #region CreateCatalog

        /// <summary>
        /// Creates an unscaled bitmap catalog for the current scenario.</summary>
        /// <returns>
        /// The maximum number of animation frames for any single entity class. This value is at
        /// least one. Animation is only possible if it is greater than one.</returns>
        /// <remarks>
        /// <b>CreateCatalog</b> defines the <see cref="CatalogManager.TileCount"/>, <see
        /// cref="CatalogManager.RowCount"/>, and <see cref="CatalogManager.Catalog"/> properties.
        /// </remarks>

        private int CreateCatalog() {

            // count catalog tiles and set indices
            int frames;
            int countedTiles = CopyEntityClasses(false, out frames);
            this._catalogClasses = new EntityClass[countedTiles];

            // create catalog bitmap
            CreateCatalog(countedTiles);
            Catalog.Lock(); Catalog.Clear();
            TileCopyBuffer.Lock();

            /*
             * The animation frame count may change when attempting to create tiles,
             * as entity classes whose tiles could not be created are mapped to the
             * "invalid" icon with its single frame. So we check only the tile count.
             */

            // create catalog images
            int copiedTiles = CopyEntityClasses(true, out frames);
            Debug.Assert(countedTiles == copiedTiles, "Tile count changed");

            TileCopyBuffer.Unlock();
            Catalog.Unlock();
            Catalog.Freeze();

            // return max frame count
            return frames;
        }

        #endregion
        #region OnAnimation

        /// <summary>
        /// Handles the <see cref="DispatcherTimer.Tick"/> event for the <see
        /// cref="AnimationTimer"/>.</summary>
        /// <param name="sender">
        /// The <see cref="DispatcherTimer"/> object sending the event.</param>
        /// <param name="args">
        /// An <see cref="EventArgs"/> object containing event data.</param>
        /// <remarks><para>
        /// <b>OnAnimation</b> returns immediately if <see cref="AnimationResponse"/> does not equal
        /// <see cref="TimerResponse.Normal"/>. For a value of <see cref="TimerResponse.SkipOne"/>,
        /// <b>OnAnimation</b> also resets this property to <see cref="TimerResponse.Normal"/>.
        /// </para><para>
        /// Otherwise, <b>OnAnimation</b> sets the <see cref="AnimationTicks"/> property to the
        /// current <see cref="DateTime.Ticks"/> count, and calls <see cref="MapView.Redraw"/> on
        /// all <see cref="MapViews"/>.</para></remarks>

        private void OnAnimation(object sender, EventArgs args) {

            // skip one or all ticks if desired
            switch (AnimationResponse) {
                case TimerResponse.Suspend:
                    return;

                case TimerResponse.SkipOne:
                    AnimationResponse = TimerResponse.Normal;
                    return;
            }

            // sanity check for animation
            if (!Animation) return;

            // store event time for map view panel
            AnimationTicks = DateTime.Now.Ticks;

            foreach (MapView mapView in MapViews) {

                // skip invalid or non-animated map views
                if (mapView == null || !mapView.Animation)
                    continue;

                // skip invalid or hidden map views
                if (mapView.Control == null || !mapView.Control.IsVisible)
                    continue;

                // skip map views in minimized windows
                Window window = Window.GetWindow(mapView.Control);
                if (window.WindowState == WindowState.Minimized)
                    continue;

                // redraw entire visible map view
                mapView.Redraw();
            }
        }

        #endregion
        #endregion
        #region Internal Methods
        #region GetElementGeometry

        /// <summary>
        /// Gets an <see cref="MapView.ElementGeometry"/> for the specified <see cref="MapView"/>.
        /// </summary>
        /// <param name="mapView">
        /// The <see cref="MapView"/> whose <see cref="MapView.ElementGeometry"/> to return.</param>
        /// <returns>
        /// A frozen <see cref="StreamGeometry"/> created from a single filled <see
        /// cref="RegularPolygon"/> outline of the <see cref="CatalogManager.MapGrid"/> of the
        /// specified <paramref name="mapView"/>.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="mapView"/> is a null reference.</exception>
        /// <remarks><para>
        /// <b>GetElementGeometry</b> assumes that during the lifetime of a <see
        /// cref="MapViewManager"/>, the <see cref="CatalogManager.MapGrid"/> of any given <paramref
        /// name="mapView"/> is identical except for the current <see cref="MapView.Scale"/>.
        /// </para><para>
        /// Once a <see cref="StreamGeometry"/> has been created for a given <paramref
        /// name="mapView"/>, it is cached with the the corresponding <see cref="MapView.Scale"/>.
        /// The cached value is returned for subsequent <paramref name="mapView"/> arguments using
        /// the same <see cref="MapView.Scale"/>.</para></remarks>

        internal StreamGeometry GetElementGeometry(MapView mapView) {
            if (mapView == null)
                ThrowHelper.ThrowArgumentNullException("mapView");

            // return existing geometry, if any
            StreamGeometry geometry;
            if (this._elementGeometries.TryGetValue(mapView.Scale, out geometry))
                return geometry;

            // create new geometry at specified scale
            geometry = new StreamGeometry();
            StreamGeometryContext context = geometry.Open();
            mapView.MapGrid.Element.Draw(context, PointD.Empty, true);
            context.Close();
            geometry.Freeze();

            // store geometry with specified scale
            this._elementGeometries.Add(mapView.Scale, geometry);
            return geometry;
        }

        #endregion
        #region GetEntityClass

        /// <summary>
        /// Returns the <see cref="EntityClass"/> with the specified <see
        /// cref="CatalogManager.Catalog"/> index.</summary>
        /// <param name="index">
        /// One of the <see cref="CatalogManager.Catalog"/> indices of the <see cref="EntityClass"/>
        /// to locate.</param>
        /// <returns><para>
        /// The <see cref="EntityClass"/> whose range of <see cref="CatalogManager.Catalog"/>
        /// indices, starting at <see cref="EntityClass.FrameIndex"/> and continuing for <see
        /// cref="EntityClass.FrameCount"/> indices, contains the specified <paramref
        /// name="index"/>.
        /// </para><para>-or-</para><para>
        /// A null reference if the <see cref="CatalogManager.Catalog"/> was not yet created, or no
        /// matching <see cref="EntityClass"/> was found.</para></returns>
        /// <remarks>
        /// <b>GetEntityClass</b> has the same effect as <see
        /// cref="EntitySection.GetEntity(Int32)"/> but uses an internal cache for O(1) performance.
        /// Moreover, <b>GetEntityClass</b> does not throw exceptions but returns a null reference
        /// for an invalid <paramref name="index"/>.</remarks>

        internal EntityClass GetEntityClass(int index) {

            if (this._catalogClasses == null ||
                index < 0 || index >= this._catalogClasses.Length)
                return null;

            return this._catalogClasses[index];
        }

        #endregion
        #region GetGridGeometry

        /// <summary>
        /// Gets a <see cref="MapView.GridGeometry"/> for the specified <see cref="MapView"/>.
        /// </summary>
        /// <param name="mapView">
        /// The <see cref="MapView"/> whose <see cref="MapView.GridGeometry"/> to return.</param>
        /// <returns>
        /// A frozen optimized <see cref="StreamGeometry"/> created from all <see
        /// cref="RegularPolygon"/> outlines contained in the <see cref="CatalogManager.MapGrid"/>
        /// of the specified <paramref name="mapView"/>.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="mapView"/> is a null reference.</exception>
        /// <remarks><para>
        /// <b>GetGridGeometry</b> assumes that during the lifetime of a <see
        /// cref="MapViewManager"/>, the <see cref="CatalogManager.MapGrid"/> of any given <paramref
        /// name="mapView"/> is identical except for the current <see cref="MapView.Scale"/>.
        /// </para><para>
        /// Once a <see cref="StreamGeometry"/> has been created for a given <paramref
        /// name="mapView"/>, it is cached with the the corresponding <see cref="MapView.Scale"/>.
        /// The cached value is returned for subsequent <paramref name="mapView"/> arguments using
        /// the same <see cref="MapView.Scale"/>.</para></remarks>

        internal StreamGeometry GetGridGeometry(MapView mapView) {
            if (mapView == null)
                ThrowHelper.ThrowArgumentNullException("mapView");

            // return existing geometry, if any
            StreamGeometry geometry;
            if (this._gridGeometries.TryGetValue(mapView.Scale, out geometry))
                return geometry;

            // create new geometry at specified scale
            geometry = new StreamGeometry();
            StreamGeometryContext context = geometry.Open();
            mapView.MapGrid.DrawOptimized(context, MapView.MapBorder);
            context.Close();
            geometry.Freeze();

            // store geometry with specified scale
            this._gridGeometries.Add(mapView.Scale, geometry);
            return geometry;
        }

        #endregion
        #endregion
        #region CloseView

        /// <summary>
        /// Closes the specified <see cref="MapView"/>.</summary>
        /// <param name="id">
        /// The <see cref="MapView.Id"/> string of the <see cref="MapView"/> to close.</param>
        /// <exception cref="ArgumentNullOrEmptyException">
        /// <paramref name="id"/> is a null reference or an empty string.</exception>
        /// <remarks><para>
        /// <b>CloseView</b> disposes of the <see cref="MapViews"/> element that matches the
        /// specified <paramref name="id"/>, and removes it from the <b>MapViews</b> collection. Use
        /// this method to close a <see cref="MapView"/> instead of directly calling its <see
        /// cref="MapViewManager.Dispose"/> method.
        /// </para><para>
        /// <b>CloseView</b> is safe to call repeatedly with the same identifier, or with an
        /// identifier that does not exist in the <b>MapViews</b> collection.</para></remarks>

        public void CloseView(string id) {
            if (String.IsNullOrEmpty(id))
                ThrowHelper.ThrowArgumentNullOrEmptyException("id");

            // dispose of and remove map view if found
            int index = MapViews.IndexOfKey(id);
            if (index >= 0) {
                if (!MapViews[index].IsDisposed)
                    MapViews[index].Dispose();

                this._mapViews.RemoveAt(index);
            }
        }

        #endregion
        #region CreateView

        /// <summary>
        /// Creates a new <see cref="MapView"/> with the specified parameters.</summary>
        /// <param name="id">
        /// The initial value for the <see cref="MapView.Id"/> property of the new <see
        /// cref="MapView"/>.</param>
        /// <param name="worldState">
        /// The initial value for the <see cref="MapView.WorldState"/> property of the new <see
        /// cref="MapView"/>.</param>
        /// <param name="parent">
        /// The <see cref="ContentControl"/> hosting the <see cref="MapViewControl"/> of the new
        /// <see cref="MapView"/>.</param>
        /// <param name="onMouseDown">
        /// An optional handler for <see cref="UIElement.MouseDown"/> events raised by the <see
        /// cref="MapViewControl"/>. This argument may be a null reference.</param>
        /// <param name="onMouseWheel">
        /// An optional handler for <see cref="UIElement.MouseWheel"/> events raised by the <see
        /// cref="MapViewControl"/>. This argument may be a null reference.</param>
        /// <returns>
        /// A new <see cref="MapView"/> object created with the specified parameters.</returns>
        /// <exception cref="ArgumentException">
        /// <paramref name="id"/> already exists as an <see cref="MapView.Id"/> value in the <see
        /// cref="MapViews"/> collection.</exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="worldState"/> or <paramref name="parent"/> is a null reference.
        /// </exception>
        /// <exception cref="ArgumentNullOrEmptyException">
        /// <paramref name="id"/> is a null reference or an empty string.</exception>
        /// <exception cref="ObjectDisposedException">
        /// The <see cref="MapViewManager"/> object has been disposed of.</exception>
        /// <remarks>
        /// <b>CreateView</b> adds the newly created <see cref="MapView"/> to the <see
        /// cref="MapViews"/> collection. It will be automatically disposed of when this <see
        /// cref="MapViewManager"/> object is disposed of.</remarks>

        public MapView CreateView(string id, WorldState worldState, ContentControl parent,
            MouseButtonEventHandler onMouseDown, MouseWheelEventHandler onMouseWheel) {

            if (IsDisposed)
                ThrowHelper.ThrowObjectDisposedException(null);
            if (String.IsNullOrEmpty(id))
                ThrowHelper.ThrowArgumentNullOrEmptyException("id");

            if (MapViews.ContainsKey(id))
                ThrowHelper.ThrowArgumentException("id", Tektosyne.Strings.ArgumentInCollection);

            // create new map view with supplied parameters
            MapView mapView = new MapView(String.Intern(id),
                worldState, parent, onMouseDown, onMouseWheel);

            // add map view to manager's list
            this._mapViews.Add(mapView);

            return mapView;
        }

        #endregion
        #region CreateInstance

        /// <summary>
        /// Creates a new <see cref="Instance"/> of the <see cref="MapViewManager"/> class.
        /// </summary>
        /// <param name="dispatcher">
        /// A <see cref="Dispatcher"/> used to marshal event handler calls to the application's
        /// foreground thread.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="dispatcher"/> is a null reference.</exception>
        /// <exception cref="InvalidOperationException">
        /// The current scenario <see cref="MasterSection.Instance"/> contains invalid data.
        /// </exception>
        /// <exception cref="PropertyValueException">
        /// <see cref="Instance"/> is not a null reference.</exception>
        /// <remarks><para>
        /// <b>CreateInstance</b> sets the <see cref="Instance"/> property to a newly created
        /// instance of the <see cref="MapViewManager"/> class that has been initialized to default
        /// values.
        /// </para><para>
        /// <b>CreateInstance</b> does not create any <see cref="MapView"/> objects. Invoke <see
        /// cref="CreateView"/> on the new <b>Instance</b> to create the <see cref="MapViews"/>.
        /// </para><para>
        /// Only a single instance of the <b>MapViewManager</b> class can be created at a time.
        /// Calling the <see cref="Dispose"/> method on the current <b>Instance</b> clears this
        /// property and allows the creation of another <b>MapViewManager</b> instance.
        /// </para></remarks>

        public static void CreateInstance(Dispatcher dispatcher) {
            if (dispatcher == null)
                ThrowHelper.ThrowArgumentNullException("dispatcher");

            // check for existing instance
            if (Instance != null)
                ThrowHelper.ThrowPropertyValueException(
                    "Instance", Tektosyne.Strings.PropertyNotNull);

            // set singleton reference
            Instance = new MapViewManager(dispatcher);
        }

        #endregion
        #region RedrawAll

        /// <summary>
        /// Redraws all <see cref="MapViews"/>.</summary>
        /// <remarks>
        /// <b>RedrawAll</b> sets the <see cref="AnimationResponse"/> flag to <see
        /// cref="TimerResponse.SkipOne"/> and calls <see cref="MapView.Redraw"/> on all valid <see
        /// cref="MapViews"/>.</remarks>

        public void RedrawAll() {

            // skip next animation timer tick
            AnimationResponse = TimerResponse.SkipOne;

            // redraw all valid map views
            foreach (MapView mapView in MapViews)
                if (mapView != null && !mapView.IsDisposed)
                    mapView.Redraw();
        }

        #endregion
        #region UpdateAllArrows

        /// <summary>
        /// Updates the attack and movement arrows of all <see cref="MapViews"/> to reflect the
        /// current <see cref="ViewOptions"/>.</summary>

        public void UpdateAllArrows() {
            foreach (MapView mapView in MapViews)
                if (mapView != null && !mapView.IsDisposed)
                    mapView.Control.ArrowDrawer.Update();
        }

        #endregion
        #region UpdateAllMarkers

        /// <summary>
        /// Updates the site markers of all <see cref="MapViews"/> to reflect the current <see
        /// cref="ViewOptions"/>.</summary>

        public void UpdateAllMarkers() {
            foreach (MapView mapView in MapViews)
                if (mapView != null && !mapView.IsDisposed)
                    mapView.Control.UpdateMarker();
        }

        #endregion
        #region IDisposable Members

        /// <summary>
        /// Releases all resources used by the <see cref="MapViewManager"/> object.</summary>
        /// <remarks><para>
        /// <b>Dispose</b> disposes of all <see cref="MapView"/> objects contained in the <see
        /// cref="MapViews"/> collection, stops the <see cref="AnimationTimer"/> if it is running,
        /// and finally calls <see cref="CatalogManager.Dispose"/> to dispose of the bitmap catalog
        /// and to set the <see cref="CatalogManager.IsDisposed"/> flag.
        /// </para><para>
        /// <b>Dispose</b> also resets the <see cref="Instance"/> property to a null reference,
        /// allowing the creation of another instance of the <see cref="MapViewManager"/> class.
        /// </para></remarks>

        public override void Dispose() {
            try {
                // guaranteed by CreateInstance
                Debug.Assert(Instance == this);

                // clear singleton reference
                Instance = null;

                // stop animation timer if running
                if (AnimationTimer != null) {
                    AnimationTimer.Stop();
                    AnimationTimer = null;
                }

                // close all active map views
                for (int i = 0; i < MapViews.Count; i++) {
                    MapView mapView = MapViews[i];
                    if (!mapView.IsDisposed) mapView.Dispose();
                }

                this._mapViews.Clear();
            }
            finally {
                // complete disposal
                base.Dispose();
            }
        }

        #endregion
    }
}
