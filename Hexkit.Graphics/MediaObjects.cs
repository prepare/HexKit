using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;

using Tektosyne;

namespace Hexkit.Graphics {

    /// <summary>
    /// Provides shared <b>System.Windows.Media</b> objects for the application.</summary>
    /// <remarks>
    /// <b>MediaObjects</b> provides map views and other Hexkit display elements with any required
    /// <see cref="Pen"/> and <see cref="Brush"/> objects. All objects are created on demand and
    /// frozen. They are never deleted since they are identical for all possible usage scenarios.
    /// </remarks>

    public static class MediaObjects {
        #region Private Fields

        // color sequence for danger brushes
        private static Color[] _dangerColors;

        // gradient brushes created from specific colors
        private readonly static Dictionary<Color, LinearGradientBrush>
            _gradientBrushes = new Dictionary<Color, LinearGradientBrush>();

        // solid color brushes created from specific colors
        private readonly static Dictionary<Color, SolidColorBrush>
            _opaqueBrushes = new Dictionary<Color, SolidColorBrush>(),
            _shadeBrushes = new Dictionary<Color, SolidColorBrush>();

        // other property backers
        private static Pen _thickPen, _thinPen;
        private static LinearGradientBrush[] _dangerBrushes, _dangerFadeBrushes;
        private static SolidColorBrush _shadeDarkBrush, _shadeLightBrush;
        private static SolidColorBrush[] _shadeGrayBrushes;

        #endregion
        #region DangerBrushes

        /// <summary>
        /// Gets a collection of <see cref="LinearGradientBrush"/> objects that represent various
        /// danger levels.</summary>
        /// <value>
        /// An <see cref="Array"/> of frozen <see cref="LinearGradientBrush"/> objects whose colors
        /// change abruptly from a <see cref="Color"/> that represents a specific danger level to
        /// <see cref="Colors.Transparent"/>.</value>
        /// <remarks><para>
        /// <see cref="DangerBrushes"/> always contains at least three elements, although the exact
        /// number may vary between Hexkit versions.
        /// </para><para>
        /// The indicator colors range from <see cref="Colors.Red"/> at the first index position to
        /// <see cref="Colors.Green"/> at the last index position. The x-coordinate of the final
        /// <see cref="GradientStop"/> also increases with each index position.</para></remarks>

        public static LinearGradientBrush[] DangerBrushes {
            [DebuggerStepThrough]
            get {
                if (_dangerBrushes == null)
                    _dangerBrushes = CreateDangerBrushes(true);

                return _dangerBrushes;
            }
        }

        #endregion
        #region DangerFadeBrushes

        /// <summary>
        /// Gets a collection of <see cref="LinearGradientBrush"/> objects that represent various
        /// danger levels, fading gradually to <see cref="Colors.Transparent"/>.</summary>
        /// <value>
        /// An <see cref="Array"/> of frozen <see cref="LinearGradientBrush"/> objects whose colors
        /// fade gradually from a <see cref="Color"/> that represents a specific danger level to
        /// <see cref="Colors.Transparent"/>.</value>
        /// <remarks><para>
        /// <see cref="DangerBrushes"/> always contains at least three elements, although the exact
        /// number may vary between Hexkit versions.
        /// </para><para>
        /// The indicator colors range from <see cref="Colors.Red"/> at the first index position to
        /// <see cref="Colors.Green"/> at the last index position. The x-coordinate of the final
        /// <see cref="GradientStop"/> also increases with each index position.</para></remarks>

        public static LinearGradientBrush[] DangerFadeBrushes {
            [DebuggerStepThrough]
            get {
                if (_dangerFadeBrushes == null)
                    _dangerFadeBrushes = CreateDangerBrushes(false);

                return _dangerFadeBrushes;
            }
        }

        #endregion
        #region ShadeDarkBrush

        /// <summary>
        /// Gets a dark semi-transparent <see cref="SolidColorBrush"/>.</summary>
        /// <value>
        /// A frozen black <see cref="SolidColorBrush"/> with an alpha value of 80.</value>

        public static SolidColorBrush ShadeDarkBrush {
            [DebuggerStepThrough]
            get {
                if (_shadeDarkBrush == null) {
                    _shadeDarkBrush = new SolidColorBrush(Color.FromArgb(80, 0, 0, 0));
                    _shadeDarkBrush.Freeze();
                }

                return _shadeDarkBrush;
            }
        }

        #endregion
        #region ShadeGrayBrushes

        /// <summary>
        /// Gets a collection of semi-transparent <see cref="SolidColorBrush"/> objects with
        /// ascending gray levels.</summary>
        /// <value>
        /// An <see cref="Array"/> of frozen <see cref="SolidColorBrush"/> objects with ascending
        /// gray levels and an alpha value of 128.</value>
        /// <remarks>
        /// The gray levels range from <see cref="Colors.Black"/> at the first index position to
        /// <see cref="Colors.White"/> for the last index position.</remarks>

        public static SolidColorBrush[] ShadeGrayBrushes {
            [DebuggerStepThrough]
            get {
                if (_shadeGrayBrushes == null) {
                    _shadeGrayBrushes = new SolidColorBrush[9];

                    for (int i = 0; i < 9; i++) {
                        byte c = (byte) Math.Min(255, i * 32);
                        var brush = new SolidColorBrush(Color.FromArgb(128, c, c, c));
                        brush.Freeze();
                        _shadeGrayBrushes[i] = brush;
                    }
                }

                return _shadeGrayBrushes;
            }
        }

        #endregion
        #region ShadeLightBrush

        /// <summary>
        /// Gets a light semi-transparent <see cref="SolidColorBrush"/>.</summary>
        /// <value>
        /// A frozen white <see cref="SolidColorBrush"/> with an alpha value of 80.</value>

        public static SolidColorBrush ShadeLightBrush {
            [DebuggerStepThrough]
            get {
                if (_shadeLightBrush == null) {
                    _shadeLightBrush = new SolidColorBrush(Color.FromArgb(80, 255, 255, 255));
                    _shadeLightBrush.Freeze();
                }

                return _shadeLightBrush;
            }
        }

        #endregion
        #region ThickPen

        /// <summary>
        /// Gets a <see cref="Pen"/> for drawing thick black lines.</summary>
        /// <value>
        /// A frozen black <see cref="Pen"/> of width two.</value>
        /// <remarks>
        /// <b>ThickPen</b> is used to draw the polygon grid.</remarks>

        public static Pen ThickPen {
            [DebuggerStepThrough]
            get {
                if (_thickPen == null) {
                    _thickPen = new Pen(Brushes.Black, 2.0);
                    _thickPen.Freeze();
                }

                return _thickPen;
            }
        }

        #endregion
        #region ThinPen

        /// <summary>
        /// Gets a <see cref="Pen"/> for drawing thin black lines.</summary>
        /// <value>
        /// A frozen black <see cref="Pen"/> of width one.</value>

        public static Pen ThinPen {
            [DebuggerStepThrough]
            get {
                if (_thinPen == null) {
                    _thinPen = new Pen(Brushes.Black, 1.0);
                    _thinPen.Freeze();
                }

                return _thinPen;
            }
        }

        #endregion
        #region CreateDangerBrushes

        /// <summary>
        /// Creates a collection of <see cref="LinearGradientBrush"/> objects that represent various
        /// danger levels.</summary>
        /// <param name="abrupt">
        /// <c>true</c> to change abruptly to <see cref="Colors.Transparent"/>; <c>false</c> to fade
        /// slowly to <see cref="Colors.Transparent"/>.</param>
        /// <returns>
        /// An <see cref="Array"/> of frozen <see cref="LinearGradientBrush"/> objects whose colors
        /// either change abruptly or fade gradually from a <see cref="Color"/> that represents a
        /// specific danger level to <see cref="Colors.Transparent"/>.</returns>

        private static LinearGradientBrush[] CreateDangerBrushes(bool abrupt) {
            const int count = 33;

            /*
             * Colors are created as two linear ramps: red to yellow, then yellow to green.
             * The ramps occupy index positions 1 to (count-2)/2 and (count-2/2) to count-2.
             * 
             * The first and last index positions (0 and count-1) duplicate red & green
             * because those index positions are not visible in resource gauges anyway.
             */

            if (_dangerColors == null) {
                _dangerColors = new Color[count];
                const int rampCount = (count - 2) / 2;

                // first color is red (FF0000)
                _dangerColors[0] = Color.FromRgb(255, 0, 0);

                // ramp from red (FF0000) to nearly yellow
                for (int i = 0; i < rampCount; i++) {
                    int green = (i * 255) / rampCount;
                    _dangerColors[i + 1] = Color.FromRgb(255, (byte) green, 0);
                }

                // ramp from yellow (FFFF00) to nearly green
                for (int i = rampCount; i < 2 * rampCount; i++) {
                    int red = ((2 * rampCount - i) * 255) / rampCount;
                    int green = 128 + red / 2;
                    _dangerColors[i + 1] = Color.FromRgb((byte) red, (byte) green, 0);
                }

                // last two colors are green (008000)
                _dangerColors[count - 2] = Color.FromRgb(0, 128, 0);
                _dangerColors[count - 1] = Color.FromRgb(0, 128, 0);
            }

            // color change moves right with each step
            double firstX = 0.2, lastX = 0.8;
            if (abrupt) { firstX = 0.0; lastX = 1.0; }
            double stepX = (lastX - firstX) / (count - 1);

            // end color is always transparent
            Color endColor = Colors.Transparent;
            Point start = new Point(0.0, 0.5), end = new Point(1.0, 0.5);
            var brushes = new LinearGradientBrush[count];

            for (int i = 0; i < count; i++) {
                double stop = firstX + i * stepX;

                // create gradient from danger color to end color
                var stops = new GradientStopCollection();
                stops.Add(new GradientStop(_dangerColors[i], 0.0));
                if (abrupt) stops.Add(new GradientStop(_dangerColors[i], stop));
                stops.Add(new GradientStop(endColor, stop));

                var brush = new LinearGradientBrush(stops, start, end);
                brush.Freeze();
                brushes[i] = brush;
            }

            return brushes;
        }

        #endregion
        #region GetBrush

        /// <summary>
        /// Gets the <see cref="Brush"/> within the specified <see cref="Array"/> whose index
        /// corresponds to the specified value.</summary>
        /// <param name="brushes">
        /// An <see cref="Array"/> of <see cref="Brush"/> objects.</param>
        /// <param name="value">
        /// An <see cref="Int32"/> value indicating a <paramref name="brushes"/> element.</param>
        /// <param name="min">
        /// The lower limit for <paramref name="value"/>.</param>
        /// <param name="max">
        /// The upper limit for <paramref name="value"/>.</param>
        /// <returns><para>
        /// The <paramref name="brushes"/> element that corresponds to the specified <paramref
        /// name="value"/>, given a possible range from <paramref name="min"/> to <paramref
        /// name="max"/>.
        /// </para><para>-or-</para><para>
        /// A null reference if <paramref name="max"/> is less than or equal to <paramref
        /// name="min"/>.</para></returns>
        /// <exception cref="ArgumentNullOrEmptyException">
        /// <paramref name="brushes"/> is a null reference or an empty <see cref="Array"/>.
        /// </exception>
        /// <remarks><para>
        /// <b>GetBrush</b> converts the specified <paramref name="value"/>, ranging from <paramref
        /// name="min"/> to <paramref name="max"/>, to an index within the specified <paramref
        /// name="brushes"/>, ranging from zero to <see cref="Array.Length"/> minus one, and returns
        /// the <paramref name="brushes"/> element with that index.
        /// </para><para>
        /// <b>GetBrush</b> returns the first element if the specified <paramref name="value"/> is
        /// less than <paramref name="min"/>, and the last element if <paramref name="value"/> is
        /// greater than <paramref name="max"/>.</para></remarks>

        public static Brush GetBrush(Brush[] brushes, int value, int min, int max) {
            if (brushes == null || brushes.Length == 0)
                ThrowHelper.ThrowArgumentNullOrEmptyException("brushes");

            // check for invalid range
            int range = max - min;
            if (range <= 0) return null;
            int maxBrush = brushes.Length - 1;

            // handle edge cases and invalid indices
            if (value >= max) return brushes[maxBrush];
            if (value <= min) return brushes[0];

            double index = maxBrush * (double) (value - min) / (double) range;
            return brushes[Fortran.NInt(index)];
        }

        #endregion
        #region GetGradientBrush

        /// <summary>
        /// Gets a <see cref="LinearGradientBrush"/> that ranges from <see
        /// cref="Colors.Transparent"/> to the specified <see cref="Color"/>.</summary>
        /// <param name="color">
        /// The final <see cref="Color"/> of the returned <see cref="LinearGradientBrush"/>.</param>
        /// <returns>
        /// A frozen <see cref="LinearGradientBrush"/> that ranges from <see
        /// cref="Colors.Transparent"/> to the specified <paramref name="color"/>.</returns>
        /// <remarks>
        /// <b>GetGradientBrush</b> returns a cached <see cref="LinearGradientBrush"/> if one has
        /// already been created for the specified <paramref name="color"/>.</remarks>

        public static LinearGradientBrush GetGradientBrush(Color color) {

            // retrieve existing brush
            LinearGradientBrush brush;
            if (!_gradientBrushes.TryGetValue(color, out brush)) {

                // create & buffer brush if not found
                brush = new LinearGradientBrush(Colors.Transparent, color, 0.0);
                brush.Freeze();
                _gradientBrushes.Add(color, brush);
            }

            return brush;
        }

        #endregion
        #region GetOpaqueBrush

        /// <summary>
        /// Gets an opaque <see cref="SolidColorBrush"/> with the specified <see cref="Color"/>.
        /// </summary>
        /// <param name="color">
        /// The <see cref="Color"/> of the returned <see cref="SolidColorBrush"/>.</param>
        /// <returns>
        /// A frozen opaque <see cref="SolidColorBrush"/> with the specified <paramref
        /// name="color"/>.</returns>
        /// <remarks>
        /// <b>GetOpaqueBrush</b> returns a cached <see cref="SolidColorBrush"/> if one has already
        /// been created for the specified <paramref name="color"/>.</remarks>

        public static SolidColorBrush GetOpaqueBrush(Color color) {

            // retrieve existing brush
            SolidColorBrush brush;
            if (!_opaqueBrushes.TryGetValue(color, out brush)) {

                // create & buffer brush if not found
                brush = new SolidColorBrush(Color.FromRgb(color.R, color.G, color.B));
                brush.Freeze();
                _opaqueBrushes.Add(color, brush);
            }

            return brush;
        }

        #endregion
        #region GetShadeBrush

        /// <summary>
        /// Gets a semi-transparent <see cref="SolidColorBrush"/> with the specified <see
        /// cref="Color"/>.</summary>
        /// <param name="color">
        /// The <see cref="Color"/> of the returned <see cref="SolidColorBrush"/>.</param>
        /// <returns>
        /// A frozen <see cref="SolidColorBrush"/> with an alpha value of 80 and the specified
        /// <paramref name="color"/>.</returns>
        /// <remarks>
        /// <b>GetShadeBrush</b> returns a cached <see cref="SolidColorBrush"/> if one has already
        /// been created for the specified <paramref name="color"/>.</remarks>

        public static SolidColorBrush GetShadeBrush(Color color) {

            // retrieve existing brush
            SolidColorBrush brush;
            if (!_shadeBrushes.TryGetValue(color, out brush)) {

                // create & buffer brush if not found
                brush = new SolidColorBrush(Color.FromArgb(80, color.R, color.G, color.B));
                brush.Freeze();
                _shadeBrushes.Add(color, brush);
            }

            return brush;
        }

        #endregion
    }
}
