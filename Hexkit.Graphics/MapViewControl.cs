using System;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;

using Tektosyne;
using Tektosyne.Geometry;

using Hexkit.Options;
using Hexkit.Scenario;
using Hexkit.World;

namespace Hexkit.Graphics {

    /// <summary>
    /// Manages input and output for a single <see cref="MapView"/>.</summary>
    /// <remarks><para>
    /// <b>MapViewControl</b> handles sizing, scrolling, and mouse input for the associated <see
    /// cref="MapView"/>. The actual graphical display is produced by a hosted <see
    /// cref="MapViewRenderer"/> that covers the current viewport.
    /// </para><para>
    /// Clients access <b>MapViewControl</b> functionality only through the methods and properties
    /// of the associated <see cref="MapView"/>. Accordingly, most <b>MapViewControl</b> methods and
    /// properties do not check their parameters or the current object state.</para></remarks>

    internal sealed class MapViewControl: ScrollViewer {
        #region MapViewControl(...)

        /// <summary>
        /// Initializes a new instance of the <see cref="MapViewControl"/> class.</summary>
        /// <param name="mapView">
        /// The initial value for the <see cref="MapView"/> property.</param>
        /// <param name="onMouseDown">
        /// An optional handler for the <see cref="UIElement.MouseDown"/> event. This argument may
        /// be a null reference.</param>
        /// <param name="onMouseWheel">
        /// An optional handler for the <see cref="UIElement.MouseWheel"/> event. This argument may
        /// be a null reference.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="mapView"/> is a null reference.</exception>
        /// <remarks>
        /// Use <see cref="MapViewManager.CreateView"/> to create a new <see
        /// cref="Graphics.MapView"/> object and all associated controls.</remarks>

        public MapViewControl(MapView mapView,
            MouseButtonEventHandler onMouseDown, MouseWheelEventHandler onMouseWheel) {

            if (mapView == null)
                ThrowHelper.ThrowArgumentNullException("mapView");

            // background color for borders
            Background = new SolidColorBrush(Color.FromRgb(63, 63, 63));

            // always show both scroll bars
            HorizontalScrollBarVisibility = ScrollBarVisibility.Visible;
            VerticalScrollBarVisibility = ScrollBarVisibility.Visible;

            this._mapView = mapView;
            this._onMouseDown = onMouseDown;
            this._onMouseWheel = onMouseWheel;

            // overlay image below renderer
            this._overlay = new Image();
            this._overlay.Stretch = Stretch.Fill;

            // renderer for actual map display
            this._renderer = new MapViewRenderer(mapView, this);

            // overlay image above renderer (Hexkit Editor only)
            this._editorOverlay = new Image();
            this._editorOverlay.Stretch = Stretch.Fill;

            // helper for attack & move arrows
            this._arrowDrawer = new ArrowDrawer(mapView);
            
            // site marker for selected polygon
            this._siteMarker = new Polygon();
            this._siteMarker.StrokeThickness = 3.0;
            this._siteMarker.Visibility = Visibility.Collapsed;
            UpdateMarker();

            // canvas covering map extent
            Canvas canvas = new Canvas();
            canvas.Children.Add(this._overlay);
            canvas.Children.Add(this._renderer);
            canvas.Children.Add(this._editorOverlay);
            canvas.Children.Add(this._siteMarker);
            Content = canvas;
        }

        #endregion
        #region Private Fields

        // construction arguments
        private readonly MapView _mapView;
        private readonly MouseButtonEventHandler _onMouseDown;
        private readonly MouseWheelEventHandler _onMouseWheel;

        // property backers
        private readonly ArrowDrawer _arrowDrawer;
        private readonly Image _overlay, _editorOverlay;
        private readonly MapViewRenderer _renderer;
        private readonly Polygon _siteMarker;

        #endregion
        #region ArrowDrawer

        /// <summary>
        /// Gets the <see cref="Graphics.ArrowDrawer"/> for the <see cref="MapViewControl"/>.
        /// </summary>
        /// <value>
        /// The <see cref="Graphics.ArrowDrawer"/> that is permanently associated with the <see
        /// cref="MapViewControl"/>.</value>

        public ArrowDrawer ArrowDrawer {
            [DebuggerStepThrough]
            get { return this._arrowDrawer; }
        }

        #endregion
        #region EditorOverlay

        /// <summary>
        /// Gets the fixed <see cref="Image"/> that is overlaid on the <see cref="Renderer"/>.
        /// </summary>
        /// <value>
        /// An arbitrary <see cref="Image"/> that appears above the <see cref="Renderer"/> within
        /// the <see cref="MapCanvas"/>.</value>
        /// <remarks><para>
        /// <b>EditorOverlay</b> always returns the same valid <see cref="Image"/> which is empty by
        /// default. The bounds of a non-empty <see cref="Image"/> are modified automatically by
        /// <see cref="OnScaleChanged"/> and <see cref="ScaleOverlay"/>.
        /// </para><para>
        /// The <see cref="FrameworkElement.Tag"/> property of the returned <see cref="Image"/>
        /// holds the associated <see cref="OverlayImage"/> object, if any. Unlike the standard <see
        /// cref="Overlay"/> image, the <b>EditorOverlay</b> image is always empty unless Hexkit
        /// Editor is running.</para></remarks>

        public Image EditorOverlay {
            [DebuggerStepThrough]
            get { return this._editorOverlay; }
        }

        #endregion
        #region MapCanvas

        /// <summary>
        /// Gets the <see cref="Canvas"/> that covers the entire associated <see cref="MapView"/>.
        /// </summary>
        /// <value>
        /// The <see cref="Canvas"/> that is the permanent <see cref="ContentControl.Content"/> of
        /// the <see cref="MapViewControl"/>.</value>
        /// <remarks>
        /// <b>MapCanvas</b> contains the entire <see cref="Graphics.MapView.Extent"/> of the
        /// associated <see cref="MapView"/>, including the bitmap created by the associated <see
        /// cref="Renderer"/>, any superimposed WPF decoration, and the surrounding map borders.
        /// </remarks>

        public Canvas MapCanvas {
            get { return Content as Canvas; }
        }

        #endregion
        #region MapView

        /// <summary>
        /// Gets the <see cref="Graphics.MapView"/> whose data is shown.</summary>
        /// <value>
        /// The <see cref="Graphics.MapView"/> associated with the <see cref="MapViewControl"/>.
        /// </value>
        /// <remarks>
        /// <b>MapView</b> always returns the same valid reference.</remarks>

        public MapView MapView {
            [DebuggerStepThrough]
            get { return this._mapView; }
        }

        #endregion
        #region Overlay

        /// <summary>
        /// Gets the fixed <see cref="Image"/> on which the <see cref="Renderer"/> is overlaid.
        /// </summary>
        /// <value>
        /// An arbitrary <see cref="Image"/> that appears below the <see cref="Renderer"/> within
        /// the <see cref="MapCanvas"/>.</value>
        /// <remarks><para>
        /// <b>Overlay</b> always returns the same valid <see cref="Image"/> which is empty by
        /// default. The bounds of a non-empty <see cref="Image"/> are modified automatically by
        /// <see cref="OnScaleChanged"/> and <see cref="ScaleOverlay"/>.
        /// </para><para>
        /// The <see cref="FrameworkElement.Tag"/> property of the returned <see cref="Image"/>
        /// holds the associated <see cref="OverlayImage"/> object, if any.</para></remarks>

        public Image Overlay {
            [DebuggerStepThrough]
            get { return this._overlay; }
        }

        #endregion
        #region Renderer

        /// <summary>
        /// Gets the <see cref="MapViewRenderer"/> that renders the map display.</summary>
        /// <value>
        /// The <see cref="MapViewRenderer"/> hosted by the <see cref="MapViewControl"/>.</value>
        /// <remarks>
        /// <b>Renderer</b> always returns the same valid reference.</remarks>

        public MapViewRenderer Renderer {
            [DebuggerStepThrough]
            get { return this._renderer; }
        }

        #endregion
        #region SiteMarker

        /// <summary>
        /// Gets the <see cref="Polygon"/> shape used to mark the selected <see
        /// cref="Graphics.MapView.SelectedSite"/>.</summary>
        /// <value>
        /// A <see cref="Polygon"/> that highlights the outline of a map polygon.</value>
        /// <remarks>
        /// <b>SiteMarker</b> always returns the same valid <see cref="Polygon"/>. Its properties
        /// are modified automatically by <see cref="OnScaleChanged"/> and <see cref="ShowMarker"/>.
        /// </remarks>

        public Polygon SiteMarker {
            [DebuggerStepThrough]
            get { return this._siteMarker; }
        }

        #endregion
        #region Viewport

        /// <summary>
        /// Gets the viewport bounds of the <see cref="MapViewControl"/>.</summary>
        /// <value>
        /// A <see cref="Rect"/> comprising the viewport of the <see cref="MapViewControl"/>, in
        /// device-independent pixels.</value>
        /// <remarks>
        /// <b>Viewport</b> starts at the current <see cref="ScrollViewer.HorizontalOffset"/> and
        /// <see cref="ScrollViewer.VerticalOffset"/>, and extends across the current <see
        /// cref="ScrollViewer.ViewportWidth"/> and <see cref="ScrollViewer.ViewportHeight"/>.
        /// </remarks>

        public Rect Viewport {
            [DebuggerStepThrough]
            get {
                return new Rect(HorizontalOffset, VerticalOffset, ViewportWidth, ViewportHeight);
            }
        }

        #endregion
        #region OnMouseDown

        /// <summary>
        /// Raises the <see cref="UIElement.MouseDown"/> event.</summary>
        /// <param name="args">
        /// A <see cref="MouseButtonEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnMouseDown</b> raises the <see cref="UIElement.MouseDown"/> event by calling the
        /// event handler that was supplied to the constructor, if any, but does not call the base
        /// class implementation of <see cref="UIElement.OnMouseDown"/> to avoid unintended event
        /// handling.</remarks>

        protected override void OnMouseDown(MouseButtonEventArgs args) {
            args.Handled = true;
            if (MapView == null || MapView.IsDisposed)
                return;

            // invoke external handler if present
            if (this._onMouseDown != null)
                this._onMouseDown(this, args);
        }

        #endregion
        #region OnMouseWheel

        /// <summary>
        /// Raises the <see cref="UIElement.MouseWheel"/> event.</summary>
        /// <param name="args">
        /// A <see cref="MouseWheelEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnMouseWheel</b> raises the <see cref="UIElement.MouseWheel"/> event by calling the
        /// event handler that was supplied to the constructor, if any, but does not call the base
        /// class implementation of <see cref="UIElement.OnMouseWheel"/> to avoid unintended event
        /// handling.</remarks>

        protected override void OnMouseWheel(MouseWheelEventArgs args) {
            args.Handled = true;
            if (MapView == null || MapView.IsDisposed)
                return;

            // invoke external handler if present
            if (this._onMouseWheel != null)
                this._onMouseWheel(this, args);
        }

        #endregion
        #region OnScaleChanged

        /// <summary>
        /// Handles changes to the <see cref="Graphics.MapView.Scale"/> property of the associated
        /// <see cref="MapView"/>.</summary>
        /// <remarks>
        /// <b>OnScaleChanged</b> adopts the current <see cref="Graphics.MapView.Extent"/> and <see
        /// cref="PolygonGrid.Element"/> shape of the associated <see cref="MapView"/>, rescales the
        /// <see cref="Overlay"/> and <see cref="EditorOverlay"/> images, and redraws the hosted
        /// <see cref="Renderer"/>.</remarks>

        public void OnScaleChanged() {

            // resize canvas to scroll dimensions
            MapCanvas.Width = MapView.Extent.Width;
            MapCanvas.Height = MapView.Extent.Height;

            // rescale overlay images
            ScaleOverlay(false);
            ScaleOverlay(true);

            // update shape & position of marker polygon
            SiteMarker.Points = MapView.MapGrid.Element.Vertices.ToWpfPoints();
            ShowMarker(MapView.SelectedSite);

            // update shape & position of any arrows
            ArrowDrawer.Draw();

            Renderer.InvalidateVisual();
        }

        #endregion
        #region OnScrollChanged

        /// <summary>
        /// Raises and handles the <see cref="ScrollViewer.ScrollChanged"/> event.</summary>
        /// <param name="args">
        /// A <see cref="ScrollChangedEventArgs"/> object containing event data.</param>
        /// <remarks><para>
        /// <b>OnScrollChanged</b> first raises the <see cref="ScrollViewer.ScrollChanged"/> event
        /// by calling the base class implementation of <see cref="ScrollViewer.OnScrollChanged"/>.
        /// </para><para>
        /// <b>OnScrollChanged</b> then handles the <see cref="ScrollViewer.ScrollChanged"/> event
        /// by redrawing the associated <see cref="Renderer"/> if the specified <paramref
        /// name="args"/> indicate a resized <see cref="Viewport"/> or changed scroll positions.
        /// </para><para>
        /// When updating the associated <see cref="Renderer"/>, <b>OnScrollChanged</b> also sets
        /// the <see cref="MapViewManager.AnimationResponse"/> flag of the current <see
        /// cref="MapViewManager"/> to <see cref="TimerResponse.SkipOne"/>. This avoids sluggish
        /// response to repeated <see cref="ScrollViewer.ScrollChanged"/> events.</para></remarks>

        protected override void OnScrollChanged(ScrollChangedEventArgs args) {
            base.OnScrollChanged(args);

            if (args.ViewportWidthChange != 0.0 || args.ViewportWidthChange != 0.0 ||
                args.HorizontalChange != 0.0 || args.VerticalChange != 0.0) {

                // redraw entire viewport
                Renderer.InvalidateVisual();

                // skip next animation timer tick
                MapViewManager.Instance.AnimationResponse = TimerResponse.SkipOne;
            }
        }

        #endregion
        #region RemoveCustomChildren

        /// <summary>
        /// Removes any <see cref="Panel.Children"/> from the <see cref="MapCanvas"/> that were
        /// added after the <see cref="MapViewControl"/> was created.</summary>
        /// <remarks>
        /// <b>RemoveCustomChildren</b> removes any temporary <see cref="MapView"/> decoration that
        /// was added after construction, such as attack and movement arrows.</remarks>

        public void RemoveCustomChildren() {

            // default number of children
            const int defaultCount = 4;

            // remove any non-default children
            int count = MapCanvas.Children.Count;
            if (count > defaultCount)
                MapCanvas.Children.RemoveRange(defaultCount, count - defaultCount);
        }

        #endregion
        #region ScaleOverlay

        /// <summary>
        /// Scales the <see cref="Overlay"/> or <see cref="EditorOverlay"/> image to the current
        /// <see cref="Graphics.MapView.Scale"/>.</summary>
        /// <param name="editing">
        /// <c>true</c> to scale the <see cref="EditorOverlay"/> image; <c>false</c> to scale the
        /// <see cref="Overlay"/> image.</param>
        /// <remarks>
        /// <b>ScaleOverlay</b> sets the bounds of the indicated <see cref="Image"/> to the <see
        /// cref="OverlayImage.Bounds"/> of its associated <see cref="OverlayImage"/>, adjusted by
        /// the <see cref="Graphics.MapView.Scale"/> of the associated <see cref="MapView"/>.
        /// </remarks>

        public void ScaleOverlay(bool editing) {

            Image image = (editing ? EditorOverlay : Overlay);
            OverlayImage overlay = image.Tag as OverlayImage;
            if (overlay == null) return;

            RectI bounds = overlay.Bounds;
            double scale = MapView.Scale / 100.0;

            Canvas.SetLeft(image, bounds.X * scale + MapView.MapBorder.X);
            Canvas.SetTop(image, bounds.Y * scale + MapView.MapBorder.Y);

            image.Width = bounds.Width * scale;
            image.Height = bounds.Height * scale;
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
        /// <b>ShowImage</b> was called on the <see cref="DispatcherObject.Dispatcher"/> thread of
        /// the <see cref="MapViewControl"/>, rather than on a background thread.</exception>
        /// <remarks>
        /// Please refer to <see cref="Graphics.MapView.ShowImage"/> for details.</remarks>

        public void ShowImage(EntityClass entityClass, PointI[] sites,
            bool move, int delay, WaitHandle abortSignal) {

            if (Dispatcher.CheckAccess())
                ThrowHelper.ThrowInvalidOperationException(Tektosyne.Strings.ThreadForeground);

            // silently ignore invalid state or arguments
            if (MapView.IsDisposed || entityClass == null || sites == null
                || abortSignal == null || abortSignal.WaitOne(0, false))
                return;

            // check that all coordinates are valid
            for (int i = 0; i < sites.Length; i++)
                if (!MapView.MapGrid.Contains(sites[i]))
                    return;

            // moving drops last site
            int siteCount = sites.Length;
            if (move) --siteCount;
            if (siteCount <= 0) return;

            // default delay is 250 msec
            if (delay <= 0) delay = 250;

            // prevent interference by idle animation
            var manager = MapViewManager.Instance;
            manager.AnimationResponse = TimerResponse.Suspend;

            Image tile = null; int tileIndex = -1;
            Dispatcher.Invoke(delegate {
                // ensure map view is up-to-date
                Renderer.InvalidateVisual();

                // draw first frame of entity class
                MapView.DrawTileToBuffer(entityClass.FrameIndex);
                tile = new Image() { Source = MapView.TileCopyBuffer };
            });

            // get distance between center and upper-left corner
            SizeD elementSize = MapView.MapGrid.Element.Bounds.Size;
            double radiusX = elementSize.Width / 2.0;
            double radiusY = elementSize.Height / 2.0;

            for (int index = 0; index < siteCount; index++) {

                // get display location for current site
                PointD sourceCenter = MapView.SiteToView(sites[index]);
                PointD source = new PointD(sourceCenter.X - radiusX, sourceCenter.Y - radiusY);

                // show animated move to next target, if any
                if (move && sites[index] != sites[index + 1]) {
                    PointD targetCenter = MapView.SiteToView(sites[index + 1]);
                    PointD target = new PointD(targetCenter.X - radiusX, targetCenter.Y - radiusY);

                    // move from source to target within specified delay
                    Duration duration = new Duration(TimeSpan.FromMilliseconds(delay));
                    var leftAnimation = new DoubleAnimation(source.X, target.X, duration);
                    var topAnimation = new DoubleAnimation(source.Y, target.Y, duration);
                    leftAnimation.Freeze(); topAnimation.Freeze();

                    Dispatcher.Invoke(delegate {
                        // add tile to canvas when first shown
                        if (tileIndex < 0) tileIndex = MapCanvas.Children.Add(tile);

                        // animate both coordinates simultaneously
                        tile.BeginAnimation(Canvas.LeftProperty, leftAnimation);
                        tile.BeginAnimation(Canvas.TopProperty, topAnimation);
                    });
                }
                else
                    Dispatcher.Invoke(delegate {
                        // add tile to canvas when first shown
                        if (tileIndex < 0) tileIndex = MapCanvas.Children.Add(tile);

                        // show image tile on current site
                        Canvas.SetLeft(tile, source.X);
                        Canvas.SetTop(tile, source.Y);
                    });

                // wait for requested delay or abort signal
                if (abortSignal.WaitOne(delay, false))
                    break;
            }

            // allow idle animation but skip one tick
            manager.AnimationResponse = TimerResponse.SkipOne;

            // remove image tile when done (BeginInvoke avoids flickering)
            Dispatcher.BeginInvoke(delegate {
                if (tileIndex >= 0 && tileIndex < MapCanvas.Children.Count)
                    MapCanvas.Children.RemoveAt(tileIndex);
            });
        }

        #endregion
        #region ShowMarker

        /// <summary>
        /// Moves the <see cref="SiteMarker"/> to the specified map location.</summary>
        /// <param name="site">
        /// The coordinates of the new <see cref="Site"/> for the <see cref="SiteMarker"/>.</param>
        /// <remarks>
        /// <b>ShowMarker</b> hides the <see cref="SiteMarker"/> if the specified <paramref
        /// name="site"/> equals <see cref="Site.InvalidLocation"/>.</remarks>

        public void ShowMarker(PointI site) {

            if (site == Site.InvalidLocation)
                SiteMarker.Visibility = Visibility.Collapsed;
            else {
                SiteMarker.Visibility = Visibility.Visible;
                PointD location = MapView.SiteToView(site);
                Canvas.SetLeft(SiteMarker, location.X);
                Canvas.SetTop(SiteMarker, location.Y);
            }
        }

        #endregion
        #region UpdateMarker

        /// <summary>
        /// Updates the <see cref="SiteMarker"/> to reflect the current <see cref="ViewOptions"/>.
        /// </summary>
        /// <remarks>
        /// <b>UpdateMarker</b> enables or disables color animation for the <see
        /// cref="SiteMarker"/>, depending on the current <see cref="ViewOptions"/>.</remarks>

        public void UpdateMarker() {

            if (ApplicationOptions.Instance.View.StaticMarker)
                this._siteMarker.Stroke = Brushes.White;
            else {
                // animation to cycle between colors
                var animation = new DoubleAnimation(0.3, 0.6, Duration.Automatic);
                animation.AutoReverse = true;
                animation.RepeatBehavior = RepeatBehavior.Forever;

                // radial brush with animated color spectrum
                var brush = new RadialGradientBrush(Colors.Yellow, Colors.White);
                brush.SpreadMethod = GradientSpreadMethod.Reflect;
                brush.BeginAnimation(RadialGradientBrush.RadiusXProperty, animation);
                brush.BeginAnimation(RadialGradientBrush.RadiusYProperty, animation);

                this._siteMarker.Stroke = brush;
            }
        }

        #endregion
    }
}
