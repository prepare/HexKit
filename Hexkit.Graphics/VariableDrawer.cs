using System;
using System.Windows;
using System.Windows.Media;

using Tektosyne;
using Tektosyne.Geometry;
using Tektosyne.Windows;

using Hexkit.Global;
using Hexkit.Options;
using Hexkit.Scenario;
using Hexkit.World;

namespace Hexkit.Graphics {

    /// <summary>
    /// Draws the <see cref="Variable"/> values requested by the <see cref="MapView.ShownVariable"/>
    /// and <see cref="MapView.ShownVariableFlags"/> options.</summary>

    internal class VariableDrawer {
        #region VariableDrawer(MapViewRenderer)

        /// <summary>
        /// Initializes a new instance of the <see cref="VariableDrawer"/> class.</summary>
        /// <param name="renderer">
        /// The <see cref="MapViewRenderer"/> containing the map display to decorate.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="renderer"/> is a null reference.</exception>

        public VariableDrawer(MapViewRenderer renderer) {
            if (renderer == null)
                ThrowHelper.ThrowArgumentNullException("renderer");

            this._renderer = renderer;

            // trigger initial update
            ValueRangeChanged = true;
        }

        #endregion
        #region Private Fields

        // construction arguments
        private readonly MapViewRenderer _renderer;

        // cached typography of map view control
        private Typeface _typeface;
        private double _emSize;

        // input and output values for last site
        private PointI _lastValue;
        private VariableDisplay _lastFlags;
        private FormattedText _lastText;
        private Point _lastTextOrigin;

        // minimum and maximum values for all sites
        private PointI _minimum, _maximum;

        #endregion
        #region ValueRangeChanged

        /// <summary>
        /// Gets or sets a value indicating whether the value range of the <see
        /// cref="MapView.ShownVariable"/> have changed.</summary>
        /// <value>
        /// <c>true</c> if the <see cref="Draw"/> method should assume that the value range of the
        /// <see cref="MapView.ShownVariable"/> has changed; otherwise, <c>false</c>.</value>
        /// <remarks><para>
        /// <b>ValueRangeChanged</b> is relevant to the <see cref="VariableDisplay.Shades"/> option.
        /// Shading of individual map sites depends on the total range of <see
        /// cref="MapView.ShownVariable"/> values across the entire map, so <see cref="Draw"/> must
        /// recalculate that range whenever that range have changed.
        /// </para><para>
        /// <b>ValueRangeChanged</b> is cleared by the <see cref="FindValueRange"/> method, and set
        /// by clients either because the <see cref="MapView.ShownVariable"/> itself, the <see
        /// cref="MapView.ShownVariableFlags"/>, or variable values on the map have changed.
        /// </para></remarks>

        public bool ValueRangeChanged { get; set; }

        #endregion
        #region FindValueRange

        /// <summary>
        /// Finds the total range of values for the specified <see cref="VariableClass"/> across all
        /// map sites.</summary>
        /// <param name="variable">
        /// The <see cref="VariableClass"/> whose values to examine.</param>
        /// <param name="sample">
        /// A current sample value for the specified <paramref name="variable"/>.</param>
        /// <remarks>
        /// <b>FindValueRange</b> separately determines the minimum and maximum for both the basic
        /// and modifier values of the specified <paramref name="variable"/>. Before returning
        /// <b>FindValueRange</b> clears the <see cref="ValueRangeChanged"/> flag.</remarks>

        private void FindValueRange(VariableClass variable, PointI sample) {

            int minX = sample.X, maxX = sample.X;
            int minY = sample.Y, maxY = sample.Y;

            WorldState world = this._renderer.MapView.WorldState;

            foreach (Site site in world.Sites) {
                PointI value = site.AddVariables(world, variable);

                if (minX > value.X) minX = value.X;
                if (minY > value.Y) minY = value.Y;
                if (maxX < value.X) maxX = value.X;
                if (maxY < value.Y) maxY = value.Y;
            }

            this._minimum = new PointI(minX, minY);
            this._maximum = new PointI(maxX, maxY);
            ValueRangeChanged = false;
        }

        #endregion
        #region Draw

        /// <summary>
        /// Draws variable values on the specified <see cref="Site"/>.</summary>
        /// <param name="context">
        /// The <see cref="DrawingContext"/> for the drawing.</param>
        /// <param name="site">
        /// The <see cref="Site"/> to receive the variable values.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="context"/> or <paramref name="site"/> is a null reference.</exception>
        /// <remarks>
        /// The origin of the specified <paramref name="context"/> must have been centered on the
        /// specified <paramref name="site"/>.</remarks>

        public void Draw(DrawingContext context, Site site) {
            if (context == null)
                ThrowHelper.ThrowArgumentNullException("context");
            if (site == null)
                ThrowHelper.ThrowArgumentNullException("site");

            // get current variable and display flags
            MapView mapView = this._renderer.MapView;
            VariableClass variable = mapView.ShownVariable;
            PointI value = site.AddVariables(mapView.WorldState, variable);
            VariableDisplay flags = mapView.ShownVariableFlags;

            // draw value as shade if desired
            if ((flags & VariableDisplay.Shades) != 0) {

                int index, shadeMinimum, shadeMaximum, shadeValue;
                Brush[] brushes = MediaObjects.ShadeGrayBrushes;

                // update minimum and maximum values if necessary
                if (ValueRangeChanged) FindValueRange(variable, value);

                // default to basic values unless only Modifier is selected
                if ((flags & VariableDisplay.BasicAndModifier) == VariableDisplay.Modifier) {
                    shadeMinimum = this._minimum.Y;
                    shadeMaximum = this._maximum.Y;
                    shadeValue = value.Y;
                } else {
                    shadeMinimum = this._minimum.X;
                    shadeMaximum = this._maximum.X;
                    shadeValue = value.X;
                }

                /*
                 * Always use a medium shade if the total range is one.
                 * Otherwise, calculate appropriate shade and restrict index to legal range,
                 * just in case ValueRangeChanged should have been set but wasn't.
                 */

                if (shadeMinimum == shadeMaximum)
                    index = brushes.Length / 2;
                else {
                    double range = shadeMaximum - shadeMinimum + 1;
                    index = (int) ((shadeValue - shadeMinimum) / range * brushes.Length);
                    index = Math.Max(0, Math.Min(brushes.Length - 1, index));
                }

                context.DrawGeometry(brushes[index], null, mapView.ElementGeometry);
            }

            // draw value as number if desired
            if ((flags & VariableDisplay.Numbers) != 0) {

                // check if output must be recreated
                if (this._lastText == null || value != this._lastValue ||
                    (flags & VariableDisplay.BasicAndModifier) !=
                    (this._lastFlags & VariableDisplay.BasicAndModifier)) {

                    // store current input and display flags
                    this._lastValue = value;
                    this._lastFlags = flags;

                    // default to no output
                    this._lastText = null;
                    string text = "";

                    // always output basic value if desired
                    if ((flags & VariableDisplay.Basic) != 0)
                        text = variable.Format(value.X, false);

                    // output modifier value only if non-zero
                    if ((flags & VariableDisplay.Modifier) != 0 && value.Y != 0) {
                        if (text.Length > 0)
                            text += Environment.NewLine + variable.Format(value.Y, true);
                        else
                            text = variable.Format(value.Y, true);
                    }

                    // create formatted text if required
                    if (text.Length > 0) {
                        if (this._typeface == null) {
                            this._typeface = this._renderer.Control.GetTypeface();
                            this._emSize = this._renderer.Control.FontSize;
                        }

                        this._lastText = new FormattedText(text,
                            ApplicationInfo.Culture, FlowDirection.LeftToRight,
                            this._typeface, this._emSize, Brushes.White);

                        // center text horizontally & vertically
                        this._lastText.TextAlignment = TextAlignment.Center;
                        this._lastTextOrigin = new Point(0, -this._lastText.Height / 2.0);
                    }
                }

                // draw formatted output text, if any
                if (this._lastText != null)
                    context.DrawText(this._lastText, this._lastTextOrigin);
            }
        }

        #endregion
    }
}
