using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using Tektosyne;
using Tektosyne.Geometry;
using Tektosyne.Windows;

using Hexkit.Global;
using Hexkit.Scenario;

namespace Hexkit.Graphics {

    /// <summary>
    /// Shows a single <see cref="ImageFrame"/>.</summary>
    /// <remarks>
    /// <b>ImageFrameRenderer</b> shows a single <see cref="ImageFrame"/> outside of a map view
    /// display. The pixel data for the <see cref="ImageFrame"/> is taken from an arbitrary
    /// specified <see cref="WriteableBitmap"/>. The <b>ImageFrameRenderer</b> will show an
    /// "invalid" icon for invalid frames or bitmaps.</remarks>

    public sealed class ImageFrameRenderer: FrameworkElement {
        #region Private Fields

        // property backers
        private RegularPolygon _polygon = AreaSection.DefaultPolygon;
        private ImageScaling _scalingX, _scalingY;

        #endregion
        #region Bitmap

        /// <summary>
        /// Gets the <see cref="WriteableBitmap"/> that provides the pixel data for the shown <see
        /// cref="Frame"/>.</summary>
        /// <value>
        /// A <see cref="WriteableBitmap"/> providing pixel data for the <see cref="Frame"/>. The
        /// default is null reference.</value>
        /// <remarks><para>
        /// <b>Bitmap</b> is set by the <see cref="Clear"/> and <see cref="Show"/> methods.
        /// </para><para>
        /// If <b>Bitmap</b> is a null reference, only the "invalid" icon returned by <see
        /// cref="Images.Invalid"/> is shown.</para></remarks>

        public WriteableBitmap Bitmap { get; private set; }

        #endregion
        #region Frame

        /// <summary>
        /// Gets the <see cref="ImageFrame"/> shown by the <see cref="ImageFrameRenderer"/>.
        /// </summary>
        /// <value>
        /// The <see cref="ImageFrame"/> shown by the <see cref="ImageFrameRenderer"/>. The default
        /// is null reference.</value>
        /// <remarks><para>
        /// <b>Frame</b> is set by the <see cref="Clear"/> and <see cref="Show"/> methods.
        /// </para><para>
        /// If <b>Frame</b> is a null reference, only the "invalid" icon returned by <see
        /// cref="Images.Invalid"/> is shown.</para></remarks>

        public ImageFrame Frame { get; private set; }

        #endregion
        #region Polygon

        /// <summary>
        /// Gets or sets the bounding <see cref="RegularPolygon"/> for the <see
        /// cref="ImageFrameRenderer"/>.</summary>
        /// <value>
        /// A <see cref="RegularPolygon"/> providing the maximum size and outline for the <see
        /// cref="Frame"/>. The default is <see cref="AreaSection.DefaultPolygon"/>.</value>
        /// <exception cref="ArgumentNullException">
        /// The property is set to a null reference.</exception>
        /// <remarks>
        /// Setting <b>Polygon</b> redraws the <see cref="ImageFrameRenderer"/> if the new value
        /// differs from the old one.</remarks>

        public RegularPolygon Polygon {
            [DebuggerStepThrough]
            get { return this._polygon; }
            set {
                if (value == null)
                    ThrowHelper.ThrowArgumentNullException("value");

                if (this._polygon != value) {
                    this._polygon = value;
                    InvalidateVisual();
                }
            }
        }

        #endregion
        #region ScalingX

        /// <summary>
        /// Gets or sets a value indicating the horizontal scaling of the <see cref="Frame"/> to fit
        /// the current <see cref="Polygon"/>.</summary>
        /// <value>
        /// An <see cref="ImageScaling"/> value indicating if and how the shown <see cref="Frame"/>
        /// is scaled horizontally to fit the current <see cref="Polygon"/>. The default is <see
        /// cref="ImageScaling.None"/>.</value>
        /// <remarks>
        /// Setting <b>ScalingX</b> redraws the <see cref="ImageFrameRenderer"/> if the new value
        /// differs from the old one.</remarks>

        public ImageScaling ScalingX {
            [DebuggerStepThrough]
            get { return this._scalingX; }
            set {
                if (this._scalingX != value) {
                    this._scalingX = value;
                    InvalidateVisual();
                }
            }
        }

        #endregion
        #region ScalingY

        /// <summary>
        /// Gets or sets a value indicating the vertical scaling of the <see cref="Frame"/> to fit
        /// the current <see cref="Polygon"/>.</summary>
        /// <value>
        /// An <see cref="ImageScaling"/> value indicating if and how the shown <see cref="Frame"/>
        /// is scaled vertically to fit the current <see cref="Polygon"/>. The default is <see
        /// cref="ImageScaling.None"/>.</value>
        /// <remarks>
        /// Setting <b>ScalingY</b> redraws the <see cref="ImageFrameRenderer"/> if the new value
        /// differs from the old one.</remarks>

        public ImageScaling ScalingY {
            [DebuggerStepThrough]
            get { return this._scalingY; }
            set {
                if (this._scalingY != value) {
                    this._scalingY = value;
                    InvalidateVisual();
                }
            }
        }

        #endregion
        #region OnRender

        /// <summary>
        /// Renders the visual content of the <see cref="ImageFrameRenderer"/>.</summary>
        /// <param name="context">
        /// The <see cref="DrawingContext"/> for the rendering.</param>
        /// <remarks><para>
        /// <b>OnRender</b> attempts to draw the shown <see cref="Frame"/> within the <see
        /// cref="RegularPolygon.Bounds"/> of the current <see cref="Polygon"/>. The drawing is
        /// centered within the <see cref="ImageFrameRenderer"/>.
        /// </para><para>
        /// If drawing fails for any reason, <b>OnRender</b> shows an "invalid" icon instead.
        /// </para></remarks>

        protected override void OnRender(DrawingContext context) {
            base.OnRender(context);

            // target rectangle centered on viewport
            Rect target = new Rect(
                (ActualWidth - Polygon.Bounds.Width) / 2.0,
                (ActualHeight - Polygon.Bounds.Height) / 2.0,
                Polygon.Bounds.Width, Polygon.Bounds.Height);

            // attempt to draw image frame
            bool success = false;
            if (Frame != null)
                success = ImageUtility.DrawOutlineFrame(context, Brushes.Black,
                    target, Bitmap, Frame.Bounds, Polygon, ScalingX, ScalingY,
                    ColorVector.Empty, PointI.Empty, PointI.Empty);

            // draw "invalid" icon on failure
            if (!success)
                ImageUtility.DrawInvalidIcon(context, Brushes.Black, target, Polygon);
        }

        #endregion
        #region Clear

        /// <summary>
        /// Clears the display of the <see cref="ImageFrameRenderer"/>.</summary>
        /// <remarks>
        /// <b>Clear</b> clears the <see cref="Bitmap"/> and <see cref="Frame"/> properties, causing
        /// the <see cref="ImageFrameRenderer"/> to show an "invalid" icon.</remarks>

        public void Clear() {
            Bitmap = null;
            Frame = null;
            InvalidateVisual();
        }

        #endregion
        #region Show

        /// <summary>
        /// Shows the specified <see cref="ImageFrame"/>.</summary>
        /// <param name="frame">
        /// The <see cref="ImageFrame"/> to show.</param>
        /// <param name="bitmap">
        /// The <see cref="WriteableBitmap"/> providing pixel data for the <paramref name="frame"/>.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="frame"/> is a null reference.</exception>
        /// <remarks>
        /// <b>Show</b> sets the <see cref="Frame"/> and <see cref="Bitmap"/> properties to the
        /// specified arguments and redraws the <see cref="ImageFrameRenderer"/>.</remarks>

        public void Show(ImageFrame frame, WriteableBitmap bitmap) {
            if (frame == null)
                ThrowHelper.ThrowArgumentNullException("frame");

            Frame = frame;
            Bitmap = bitmap;
            InvalidateVisual();
        }

        #endregion
    }
}
