using System;
using System.Windows;
using System.Windows.Media;

using Tektosyne;
using Tektosyne.Geometry;

using Hexkit.Global;
using Hexkit.Options;
using Hexkit.Scenario;
using Hexkit.World;

namespace Hexkit.Graphics {

    /// <summary>
    /// Draws the <see cref="Unit"/> decoration requested by the <see cref="MapView.ShowFlags"/> and
    /// <see cref="MapView.GaugeResource"/> options.</summary>

    internal class UnitDecorator {
        #region UnitDecorator(MapViewRenderer)

        /// <summary>
        /// Initializes a new instance of the <see cref="UnitDecorator"/> class.</summary>
        /// <param name="renderer">
        /// The <see cref="MapViewRenderer"/> containing the map display to decorate.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="renderer"/> is a null reference.</exception>

        public UnitDecorator(MapViewRenderer renderer) {
            if (renderer == null)
                ThrowHelper.ThrowArgumentNullException("renderer");

            this._renderer = renderer;
        }

        #endregion
        #region Private Fields

        // construction arguments
        private readonly MapViewRenderer _renderer;

        // current polygon shape
        private RegularPolygon _polygon;

        // outline of resource gauge
        private Rect _gaugeBox;

        // outline & center of unit flag
        private Rect _flagBox;
        private PointD _flagCenter;

        // size of marks in unit flag
        private double _flagMark;

        #endregion
        #region Private Methods
        #region DrawFlagBar

        /// <summary>
        /// Draws a vertical bar in the unit flag.</summary>
        /// <param name="context">
        /// The <see cref="DrawingContext"/> for the drawing.</param>
        /// <param name="align">
        /// A <see cref="HorizontalAlignment"/> value indicating the position of the bar within the
        /// unit flag.</param>
        /// <param name="count">
        /// The total number of bars drawn in the unit flag.</param>

        private void DrawFlagBar(DrawingContext context, HorizontalAlignment align, int count) {

            double x = this._flagCenter.X + GetFlagOffset(align, count);
            double y = this._flagCenter.Y;
            double size = 2 * this._flagMark;

            context.DrawLine(MediaObjects.ThickPen,
                new Point(x, y - size), new Point(x, y + size));
        }

        #endregion
        #region DrawFlagCross

        /// <summary>
        /// Draws a diagonal cross in the unit flag.</summary>
        /// <param name="context">
        /// The <see cref="DrawingContext"/> for the drawing.</param>

        private void DrawFlagCross(DrawingContext context) {

            double x = this._flagCenter.X, y = this._flagCenter.Y;
            double size = 2 * this._flagMark;

            context.DrawLine(MediaObjects.ThickPen,
                new Point(x - size, y - size), new Point(x + size, y + size));

            context.DrawLine(MediaObjects.ThickPen,
                new Point(x - size, y + size), new Point(x + size, y - size));
        }

        #endregion
        #region DrawFlagDot

        /// <summary>
        /// Draws a round dot in the unit flag.</summary>
        /// <param name="context">
        /// The <see cref="DrawingContext"/> for the drawing.</param>
        /// <param name="align">
        /// A <see cref="HorizontalAlignment"/> value indicating the position of the dot within the
        /// unit flag.</param>
        /// <param name="count">
        /// The total number of dots drawn in the unit flag.</param>

        private void DrawFlagDot(DrawingContext context, HorizontalAlignment align, int count) {

            double x = this._flagCenter.X, y = this._flagCenter.Y;
            double size = this._flagMark;

            /*
             * The outer dots in a a row of three need some extra spacing,
             * or they will appear to meld together.
             */

            if (align != HorizontalAlignment.Center) {
                double offset = GetFlagOffset(align, count);
                if (count == 3) offset *= 1.1;
                x += offset;
            }

            context.DrawEllipse(Brushes.Black, null, new Point(x, y), size, size);
        }

        #endregion
        #region GetFlagOffset

        /// <summary>
        /// Returns the horizontal offset of a stack size indicator, relative to the center of the
        /// unit flag.</summary>
        /// <param name="align">
        /// A <see cref="HorizontalAlignment"/> value indicating the position of the indicator
        /// within the unit flag.</param>
        /// <param name="count">
        /// The total number of indicators drawn in the unit flag.</param>
        /// <returns>
        /// The horizontal offset, in device-independent pixels, that should be added to the center
        /// of the unit flag before drawing the indicator.</returns>

        private double GetFlagOffset(HorizontalAlignment align, int count) {

            // no offset for centered indicators
            if (align == HorizontalAlignment.Center)
                return 0.0;

            // compute absolute horizontal offset
            if (count % 2 == 0) count *= 2;
            double offset = this._flagBox.Width / (count + 1);

            // invert sign for left-side indicators
            if (align == HorizontalAlignment.Left)
                offset = -offset;

            return offset;
        }

        #endregion
        #region Initialize

        /// <summary>
        /// Initializes the <see cref="UnitDecorator"/> with the specified <see
        /// cref="RegularPolygon"/>.</summary>
        /// <param name="polygon">
        /// The <see cref="RegularPolygon"/> on which to base the drawing parameters.</param>
        /// <exception cref="ArgumentException">
        /// <paramref name="polygon"/> is neither a square nor a hexagon.</exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="polygon"/> is a null reference.</exception>

        private void Initialize(RegularPolygon polygon) {
            if (polygon == null)
                ThrowHelper.ThrowArgumentNullException("polygon");

            this._polygon = polygon;
            bool onEdge = (polygon.Orientation == PolygonOrientation.OnEdge);
            SizeD centerSize, polygonSize = polygon.Bounds.Size;

            // compute central box within polygon
            switch (polygon.Sides) {

                case 4:
                    centerSize = (onEdge ?
                        new SizeD(polygonSize.Width, polygonSize.Height) :
                        new SizeD(polygonSize.Width / 2.0, polygonSize.Height / 2.0));
                    break;

                case 6:
                    centerSize = (onEdge ?
                        new SizeD(3 * polygonSize.Width / 4.0, polygonSize.Height / 2.0) :
                        new SizeD(polygonSize.Width, polygonSize.Height / 2.0));
                    break;

                default:
                    ThrowHelper.ThrowArgumentExceptionWithFormat(
                        "polygon", Tektosyne.Strings.ArgumentPropertyInvalid, "Sides");
                    centerSize = SizeD.Empty;
                    break;
            }

            // shrink central box to reveal selection outline
            centerSize = new SizeD(centerSize.Width * 0.9, centerSize.Height * 0.9);
            RectD centerBox = new RectD(
                -centerSize.Width / 2.0, -centerSize.Height / 2.0,
                centerSize.Width, centerSize.Height);

            /*
             * Compute resource gauge outline, depending on the size of the polygon's central box.
             * 
             * Width  1-26: Gauge is hidden since there is no room for it.
             * Width 27-oo: Gauge occupies the lower side of the central box and grows with it.
             *              For hexagons lying on edge, we also lower and shorten the gauge.
             */

            // scale gauge width to central box width
            double gaugeWidth = centerBox.Width;
            if (gaugeWidth < 26.0)
                this._gaugeBox = new Rect(0, 0, 0, 0);
            else {
                const double gaugeHeight = 4.0;
                double gaugeLeft = centerBox.Left;
                double gaugeTop = centerBox.Bottom - gaugeHeight;

                // lower & shorten gauge if necessary
                if (polygon.Sides == 6 && onEdge) {
                    gaugeLeft += gaugeHeight / 2.0;
                    gaugeTop += gaugeHeight;
                    gaugeWidth -= gaugeHeight;
                }

                this._gaugeBox = new Rect(gaugeLeft, gaugeTop, gaugeWidth, gaugeHeight);
            }

            /*
             * Compute unit flag outline, depending on the size of the polygon's central box.
             *
             * Width  1-18: Unit flag occupies entire central box and shrinks with it.
             * Width 19-26: Unit flag is 18 x 12.6 pixels and centered on central box.
             * Width 27-oo: Unit flag occupies upper-right corner of central box and grows with it.
             */

            // scale flag width to central box width
            double flagWidth = centerBox.Width;
            if (flagWidth > 18.0)
                flagWidth = Math.Max(18.0, flagWidth * 0.35);

            // set flag height with fixed aspect ratio
            double flagHeight = flagWidth * 0.7;
            if (flagHeight > centerBox.Height) {
                flagHeight = centerBox.Height;
                flagWidth = flagHeight / 0.7;
            }

            // center unit flag on central box by default
            double flagLeft = -flagWidth / 2.0;
            double flagTop = -flagHeight / 2.0;

            // place flag in upper-right corner or large box
            if (centerBox.Width > 26.0) {
                flagLeft = centerBox.Right - flagWidth;
                flagTop = centerBox.Top;
            }

            // set outline and center of flag
            this._flagBox = new Rect(flagLeft, flagTop, flagWidth, flagHeight);
            this._flagCenter = new PointD(flagLeft + flagWidth / 2.0, flagTop + flagHeight / 2.0);

            // compute size of dots in flag
            this._flagMark = flagWidth / 8.4;
        }

        #endregion
        #endregion
        #region DrawFlag

        /// <summary>
        /// Draws a unit flag on the specified <see cref="Site"/>.</summary>
        /// <param name="context">
        /// The <see cref="DrawingContext"/> for the drawing.</param>
        /// <param name="site">
        /// The <see cref="Site"/> to receive the unit flag.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="context"/> is a null reference.</exception>
        /// <remarks>
        /// The origin of the specified <paramref name="context"/> must have been centered on the
        /// specified <paramref name="site"/>.</remarks>

        public void DrawFlag(DrawingContext context, Site site) {
            if (context == null)
                ThrowHelper.ThrowArgumentNullException("context");

            var units = site.Units;
            int count = units.Count;
            if (count == 0) return;

            // initialize geometric data if changed
            MapView mapView = this._renderer.MapView;
            RegularPolygon polygon = mapView.MapGrid.Element;
            if (polygon != this._polygon) Initialize(polygon);

            // retrieve flag color for unit owner
            Faction owner = units[0].Owner;
            var brush = MediaObjects.GetOpaqueBrush(owner.Color);

            // use ivory instead if active units present
            if (!ApplicationInfo.IsEditor && owner == mapView.WorldState.ActiveFaction)
                for (int i = 0; i < count; i++)
                    if (((Unit) units[i]).CanMove) {
                        brush = Brushes.Ivory;
                        break;
                    }

            // draw flag rectangle
            context.DrawRectangle(brush, MediaObjects.ThinPen, this._flagBox);

            if (count == 1) {
                // one unit: single dot
                DrawFlagDot(context, HorizontalAlignment.Center, 1);
            }
            else if (count == 2) {
                // two units: two dots
                DrawFlagDot(context, HorizontalAlignment.Left, 2);
                DrawFlagDot(context, HorizontalAlignment.Right, 2);
            }
            else if (count == 3) {
                // three units: three dots
                DrawFlagDot(context, HorizontalAlignment.Left, 3);
                DrawFlagDot(context, HorizontalAlignment.Center, 3);
                DrawFlagDot(context, HorizontalAlignment.Right, 3);
            }
            else if (count >= 16) {
                // 16+ entities: diagonal cross
                DrawFlagCross(context);
            }
            else if (count >= 12) {
                // 12-15 units: three bars
                DrawFlagBar(context, HorizontalAlignment.Left, 3);
                DrawFlagBar(context, HorizontalAlignment.Center, 3);
                DrawFlagBar(context, HorizontalAlignment.Right, 3);
            }
            else if (count >= 8) {
                // 8-11 units: two bars
                DrawFlagBar(context, HorizontalAlignment.Left, 2);
                DrawFlagBar(context, HorizontalAlignment.Right, 2);
            }
            else if (count >= 4) {
                // 4-7 units: one bar
                DrawFlagBar(context, HorizontalAlignment.Center, 1);
            }
        }

        #endregion
        #region DrawGauge

        /// <summary>
        /// Draws a resource gauge on the specified <see cref="Site"/>.</summary>
        /// <param name="context">
        /// The <see cref="DrawingContext"/> for the drawing.</param>
        /// <param name="site">
        /// The <see cref="Site"/> to receive the resource gauge.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="context"/> is a null reference.</exception>
        /// <remarks>
        /// The origin of the specified <paramref name="context"/> must have been centered on the
        /// specified <paramref name="site"/>.</remarks>

        public void DrawGauge(DrawingContext context, Site site) {
            if (context == null)
                ThrowHelper.ThrowArgumentNullException("context");

            var units = site.Units;
            int count = units.Count;
            if (count == 0) return;

            // initialize geometric data if changed
            MapView mapView = this._renderer.MapView;
            RegularPolygon polygon = mapView.MapGrid.Element;
            if (polygon != this._polygon) Initialize(polygon);

            // quit if gauge invisible
            if (this._gaugeBox.Width == 0.0) return;

            // get current resource and display flags
            string resource = mapView.GaugeResource;
            if (String.IsNullOrEmpty(resource)) return;
            GaugeDisplay flags = mapView.GaugeResourceFlags;

            int min = 0, max = 0, value = 0;

            // traverse unit stack from top to bottom
            for (int i = count - 1; i >= 0; i--) {
                Unit unit = (Unit) units[i];
                string id = resource;

                // map standard pseudo-resources to actual unit resources
                if (id == ResourceClass.StandardStrength.Id)
                    id = unit.UnitClass.StrengthResource;
                else if (id == ResourceClass.StandardMorale.Id)
                    id = unit.UnitClass.MoraleResource;

                // get resource values if valid and present
                if (!String.IsNullOrEmpty(id)) {
                    Variable variable = null;
                    if (unit.Resources.Variables.TryGetValue(id, out variable)) {
                        min += variable.Minimum;
                        max += variable.Maximum;
                        value += variable.Value;
                    }
                }

                // stop after topmost unit unless showing stack
                if ((flags & GaugeDisplay.Stack) == 0) break;
            }
            
            // quit if no valid resource found
            if (min == max) return;

            // show resource if depleted or always shown
            if (value < max || (flags & GaugeDisplay.Always) != 0) {
                var brush = MediaObjects.GetBrush(MediaObjects.DangerBrushes, value, min, max);
                context.DrawRectangle(brush, MediaObjects.ThinPen, this._gaugeBox);
            }
        }

        #endregion
    }
}
