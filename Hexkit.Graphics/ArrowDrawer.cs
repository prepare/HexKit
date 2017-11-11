using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

using Tektosyne;
using Tektosyne.Geometry;
using Hexkit.Options;

namespace Hexkit.Graphics {

    /// <summary>
    /// Draws the attack and movement arrows requested by <see cref="MapView.AttackArrows"/> and
    /// <see cref="MapView.MoveArrows"/>.</summary>

    internal class ArrowDrawer {
        #region ArrowDrawer(MapView)

        /// <summary>
        /// Initializes a new instance of the <see cref="ArrowDrawer"/> class.</summary>
        /// <param name="mapView">
        /// The <see cref="MapView"/> providing geometric, visual, and world data.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="mapView"/> is a null reference.</exception>

        public ArrowDrawer(MapView mapView) {
            if (mapView == null)
                ThrowHelper.ThrowArgumentNullException("mapView");

            this._mapView = mapView;
        }

        #endregion
        #region Private Fields

        // construction arguments
        private readonly MapView _mapView;

        // brushes for attack & move arrows
        private LinearGradientBrush _attackBrush, _moveBrush;

        #endregion
        #region AttackBrush

        /// <summary>
        /// Gets the <see cref="Brush"/> used to draw all <see cref="MapView.AttackArrows"/>.
        /// </summary>
        /// <value>
        /// A <see cref="LinearGradientBrush"/> with appropriate colors for all <see
        /// cref="MapView.AttackArrows"/>.</value>
        /// <remarks>
        /// <b>AttackBrush</b> is created on first access and cached for subsequent retrievals.
        /// </remarks>

        public LinearGradientBrush AttackBrush {
            get {
                if (this._attackBrush == null)
                    this._attackBrush = CreateBrush(Colors.Red);

                return this._attackBrush;
            }
        }

        #endregion
        #region MoveBrush

        /// <summary>
        /// Gets the <see cref="Brush"/> used to draw all <see cref="MapView.MoveArrows"/>.
        /// </summary>
        /// <value>
        /// A <see cref="LinearGradientBrush"/> with appropriate colors for all <see
        /// cref="MapView.MoveArrows"/>.</value>
        /// <remarks>
        /// <b>MoveBrush</b> is created on first access and cached for subsequent retrievals.
        /// </remarks>

        public LinearGradientBrush MoveBrush {
            get {
                if (this._moveBrush == null)
                    this._moveBrush = CreateBrush(Colors.Blue);

                return this._moveBrush;
            }
        }

        #endregion
        #region CreateBrush

        /// <summary>
        /// Creates a <see cref="LinearGradientBrush"/> with the specified <see cref="Color"/>.
        /// </summary>
        /// <param name="color">
        /// The <see cref="Color"/> for the brush.</param>
        /// <returns>
        /// A new <see cref="LinearGradientBrush"/> based on the specified <paramref name="color"/>.
        /// </returns>
        /// <remarks>
        /// The returned <see cref="LinearGradientBrush"/> uses color animation if the current <see
        /// cref="ViewOptions.StaticArrows"/> option is disabled.</remarks>

        private static LinearGradientBrush CreateBrush(Color color) {
            LinearGradientBrush brush;

            if (ApplicationOptions.Instance.View.StaticArrows) {
                // create simple gradient brush without animation
                brush = new LinearGradientBrush(Colors.Ivory, color,
                    new Point(0.0, 0.5), new Point(1.0, 0.5));
                brush.Freeze();
            }
            else {
                // create brush with animated ivory gradient stop
                brush = new LinearGradientBrush(new GradientStopCollection() {
                    new GradientStop(Colors.Ivory, 0.0),
                    new GradientStop(color, 0.0),
                    new GradientStop(color, 1.0),
                }, 0.0);

                // fade ivory color in, then out again, while offset moves
                Duration duration = new Duration(TimeSpan.FromSeconds(0.6));
                var colorAnimation = new ColorAnimation(color, Colors.Ivory, duration);
                colorAnimation.AutoReverse = true;
                colorAnimation.RepeatBehavior = RepeatBehavior.Forever;

                // move ivory color in direction of arrow
                var offsetAnimation = new DoubleAnimation(0.0, 1.0, duration + duration);
                offsetAnimation.RepeatBehavior = RepeatBehavior.Forever;

                // animate color and offset simultaneously
                var gradientStop = brush.GradientStops[0];
                gradientStop.BeginAnimation(GradientStop.ColorProperty, colorAnimation);
                gradientStop.BeginAnimation(GradientStop.OffsetProperty, offsetAnimation);
            }

            return brush;
        }

        #endregion
        #region DrawArrows

        /// <summary>
        /// Draws all arrows in the specified collection.</summary>
        /// <param name="arrows">
        /// A <see cref="List{LineI}"/> containing the arrows to draw.</param>
        /// <param name="brush">
        /// The <see cref="Brush"/> with which to draw all <paramref name="arrows"/>.</param>

        private void DrawArrows(List<LineI> arrows, LinearGradientBrush brush) {
            Debug.Assert(arrows != null);
            Debug.Assert(brush != null);

            Canvas canvas = this._mapView.Control.MapCanvas;
            PolygonGrid mapGrid = this._mapView.MapGrid;

            // draw all arrows defined by map view
            for (int i = 0; i < arrows.Count; i++) {
                PointI start = arrows[i].Start;
                PointI end = arrows[i].End;

                // skip arrows with zero length
                if (start == end) continue;

                // skip invalid map coordinates
                if (!mapGrid.Contains(start) || !mapGrid.Contains(end))
                    continue;

                // create arrow with specified brush
                Polygon arrow = new Polygon();
                arrow.Stroke = Brushes.White;
                arrow.StrokeThickness = 1.0;
                arrow.Fill = brush;

                // convert map to display coordinates
                PointD viewStart = this._mapView.SiteToView(start);
                PointD viewEnd = this._mapView.SiteToView(end);
                LineD viewArrow = new LineD(viewStart, viewEnd);

                // scale arrow thickness to current polygon size
                double dx = Math.Max(2.0, this._mapView.MapGrid.Element.OuterRadius * 0.4);
                double length = viewArrow.Length;

                arrow.Points = new PointCollection {
                    new Point(dx * 1.4, 0),
                    new Point(dx * 0.8, dx * 0.8),
                    new Point(length - dx * 1.4, dx * 0.4),
                    new Point(length - dx * 1.6, dx),
                    new Point(length - dx * 0.4, 0),
                    new Point(length - dx * 1.6, -dx),
                    new Point(length - dx * 1.4, -dx * 0.4),
                    new Point(dx * 0.8, -dx * 0.8),
                };

                // rotate into direction of arrow
                arrow.RenderTransform = new RotateTransform(viewArrow.Angle * Angle.RadiansToDegrees);

                // show arrow at display coordinates
                canvas.Children.Add(arrow);
                Canvas.SetLeft(arrow, viewStart.X);
                Canvas.SetTop(arrow, viewStart.Y);
            }
        }

        #endregion
        #region Draw

        /// <summary>
        /// Draws all <see cref="MapView.AttackArrows"/> and <see cref="MapView.MoveArrows"/>
        /// defined by the associated <see cref="MapView"/>.</summary>
        /// <remarks>
        /// <b>Draw</b> always draws all elements in the <see cref="MapView.AttackArrows"/> and <see
        /// cref="MapView.MoveArrows"/> collections. Currently obscured arrows may become visible
        /// when the scroll position of the <see cref="MapView"/> changes.</remarks>

        public void Draw() {

            // remove any previously drawn arrows
            this._mapView.Control.RemoveCustomChildren();

            // draw all move arrows, if any
            List<LineI> arrows = this._mapView.MoveArrows;
            if (arrows.Count > 0)
                DrawArrows(arrows, MoveBrush);

            // draw all attack arrows, if any
            arrows = this._mapView.AttackArrows;
            if (arrows.Count > 0)
                DrawArrows(arrows, AttackBrush);
        }

        #endregion
        #region Update

        /// <summary>
        /// Updates all <see cref="MapView.AttackArrows"/> and <see cref="MapView.MoveArrows"/> to
        /// reflect the current <see cref="ViewOptions"/>.</summary>
        /// <remarks>
        /// <b>Update</b> enables or disables color animation for all <see
        /// cref="MapView.AttackArrows"/> and <see cref="MapView.MoveArrows"/>, depending on the
        /// current <see cref="ViewOptions"/>.</remarks>

        public void Update() {
            this._attackBrush = null;
            this._moveBrush = null;
            Draw();
        }

        #endregion
    }
}
