using System;
using System.ComponentModel;
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
    /// Provides auxiliary methods for <b>Hexkit.Graphics</b>.</summary>
    /// <remarks>
    /// <b>ImageUtility</b> provides methods to draw image frames without necessarily requiring a
    /// catalog bitmap. These methods are used to create the original bitmap catalog, and to show
    /// image frames (with or without a catalog) outside of map views.</remarks>

    public static class ImageUtility {
        #region Private Methods
        #region DrawFrameCore(DrawingContext, ...)

        /// <overloads>
        /// Draws the specified image frame to the specified <see cref="DrawingContext"/> or <see
        /// cref="WriteableBitmap"/>.</overloads>
        /// <summary>
        /// Draws the specified image frame to the specified <see cref="DrawingContext"/>.</summary>
        /// <param name="targetContext">
        /// The <see cref="DrawingContext"/> for the drawing.</param>
        /// <param name="target">
        /// The region within <paramref name="targetContext"/> on which the copied image frame is
        /// centered.</param>
        /// <param name="frameBitmap">
        /// A <see cref="WriteableBitmap"/> containing exactly the image frame to copy.</param>
        /// <param name="scalingX">
        /// An <see cref="ImageScaling"/> value indicating the horizontal scaling of the <paramref
        /// name="frameBitmap"/> to fit the specified <paramref name="target"/> region.</param>
        /// <param name="scalingY">
        /// An <see cref="ImageScaling"/> value indicating the vertical scaling of the <paramref
        /// name="frameBitmap"/> fit the specified <paramref name="target"/> region.</param>
        /// <param name="offset">
        /// An optional pixel offset that is added to the centered location within the <paramref
        /// name="target"/> region.</param>
        /// <param name="scalingVector">
        /// An optional scaling vector that is applied to the transformation matrix of the specified
        /// <paramref name="targetContext"/>. Specify <see cref="PointI.Empty"/> if no scaling
        /// vector should be applied.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="targetContext"/> or <paramref name="frameBitmap"/> is a null reference.
        /// </exception>
        /// <remarks>
        /// <b>DrawFrameCore</b> is called by <see cref="DrawFrame"/> when drawing to a <see
        /// cref="DrawingContext"/>, or to a <see cref="WriteableBitmap"/> with <see
        /// cref="ImageScaling.Stretch"/> scaling.</remarks>

        private static void DrawFrameCore(DrawingContext targetContext, RectD target,
            WriteableBitmap frameBitmap, ImageScaling scalingX, ImageScaling scalingY,
            PointI offset, PointI scalingVector) {

            if (targetContext == null)
                ThrowHelper.ThrowArgumentNullException("targetContext");
            if (frameBitmap == null)
                ThrowHelper.ThrowArgumentNullException("frameBitmap");

            // compute upper left corner of centered & offset frame
            SizeI source = new SizeI(frameBitmap.PixelWidth, frameBitmap.PixelHeight);
            double x = target.X + offset.X + (target.Width - source.Width) / 2.0;
            double y = target.Y + offset.Y + (target.Height - source.Height) / 2.0;

            // default coordinates for single unscaled frame
            Rect frame = new Rect(0, 0, source.Width, source.Height);
            Point frameStart = new Point(x, y);
            Point frameStop = new Point(x + source.Width, y + source.Height);

            // adjust horizontal coordinates for desired scaling
            switch (scalingX) {

                case ImageScaling.Repeat:
                    while (frameStart.X > target.X) frameStart.X -= source.Width;
                    frameStop.X = target.Right;
                    break;

                case ImageScaling.Stretch:
                    frameStart.X = target.X + offset.X;
                    frame.Width = target.Width;
                    break;
            }

            // adjust vertical coordinates for desired scaling
            switch (scalingY) {

                case ImageScaling.Repeat:
                    while (frameStart.Y > target.Y) frameStart.Y -= source.Height;
                    frameStop.Y = target.Bottom;
                    break;

                case ImageScaling.Stretch:
                    frameStart.Y = target.Y + offset.Y;
                    frame.Height = target.Height;
                    break;
            }

            // apply scaling vector if specified
            double mx = 0, my = 0;
            if (scalingVector != PointI.Empty) {

                Matrix matrix = new Matrix();
                matrix.Scale(scalingVector.X, scalingVector.Y);
                targetContext.PushTransform(new MatrixTransform(matrix));

                // compensate for any coordinate inversion
                if (scalingVector.X < 0) mx = -2 * x - source.Width;
                if (scalingVector.Y < 0) my = -2 * y - source.Height;
            }

            // repeatedly copy frame to fill target bounds
            for (double dx = frameStart.X; dx < frameStop.X; dx += frame.Width)
                for (double dy = frameStart.Y; dy < frameStop.Y; dy += frame.Height) {
                    frame.Location = new Point(dx + mx, dy + my);
                    targetContext.DrawImage(frameBitmap, frame);
                }

            // pop scaling transformation, if any
            if (scalingVector != PointI.Empty)
                targetContext.Pop();
        }

        #endregion
        #region DrawFrameCore(WriteableBitmap, ...)

        /// <summary>
        /// Draws the specified image frame to the specified <see cref="WriteableBitmap"/>.
        /// </summary>
        /// <param name="targetBitmap">
        /// The <see cref="WriteableBitmap"/> for the drawing.</param>
        /// <param name="target">
        /// The region within <paramref name="targetBitmap"/> on which the copied image frame is
        /// centered.</param>
        /// <param name="frameBitmap">
        /// A <see cref="WriteableBitmap"/> containing exactly the image frame to copy.</param>
        /// <param name="scalingX">
        /// An <see cref="ImageScaling"/> value indicating the horizontal scaling of the <paramref
        /// name="frameBitmap"/> to fit the specified <paramref name="target"/> region.</param>
        /// <param name="scalingY">
        /// An <see cref="ImageScaling"/> value indicating the vertical scaling of the <paramref
        /// name="frameBitmap"/> fit the specified <paramref name="target"/> region.</param>
        /// <param name="offset">
        /// An optional pixel offset that is added to the centered location within the <paramref
        /// name="target"/> region.</param>
        /// <param name="scalingVector">
        /// An optional scaling vector whose negative components indicate the specified <paramref
        /// name="frameBitmap"/> should be mirrored along the corresponding axis. Specify zero or
        /// positive components for no mirroring.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="targetBitmap"/> or <paramref name="frameBitmap"/> is a null reference.
        /// </exception>
        /// <exception cref="InvalidEnumArgumentException">
        /// <paramref name="scalingX"/> or <paramref name="scalingY"/> equals <see
        /// cref="ImageScaling.Stretch"/>.</exception>
        /// <remarks>
        /// <b>DrawFrameCore</b> is called by <see cref="DrawFrame"/> when drawing to a <see
        /// cref="WriteableBitmap"/> without <see cref="ImageScaling.Stretch"/> scaling.</remarks>

        private static void DrawFrameCore(WriteableBitmap targetBitmap, RectI target,
            WriteableBitmap frameBitmap, ImageScaling scalingX, ImageScaling scalingY,
            PointI offset, PointI scalingVector) {

            if (targetBitmap == null)
                ThrowHelper.ThrowArgumentNullException("targetBitmap");
            if (frameBitmap == null)
                ThrowHelper.ThrowArgumentNullException("frameBitmap");

            // compute coordinates of centered & offset frame
            SizeI source = new SizeI(frameBitmap.PixelWidth, frameBitmap.PixelHeight);
            int x = Fortran.NInt(target.X + offset.X + (target.Width - source.Width) / 2.0);
            int y = Fortran.NInt(target.Y + offset.Y + (target.Height - source.Height) / 2.0);
            int endX = x + source.Width, endY = y + source.Height;

            // adjust horizontal coordinates for desired scaling
            switch (scalingX) {

                case ImageScaling.Repeat:
                    while (x > target.X) x -= source.Width;
                    endX = target.Right;
                    break;

                case ImageScaling.Stretch:
                    ThrowHelper.ThrowInvalidEnumArgumentException(
                        "scalingX", (int) scalingX, typeof(ImageScaling));
                    break;
            }

            // adjust vertical coordinates for desired scaling
            switch (scalingY) {

                case ImageScaling.Repeat:
                    while (y > target.Y) y -= source.Height;
                    endY = target.Bottom;
                    break;

                case ImageScaling.Stretch:
                    ThrowHelper.ThrowInvalidEnumArgumentException(
                        "scalingY", (int) scalingY, typeof(ImageScaling));
                    break;
            }

            // check scaling vector for mirroring
            bool mirrorX = (scalingVector.X < 0);
            bool mirrorY = (scalingVector.Y < 0);

            // repeatedly copy frame to fill target bounds
            for (int dx = x; dx < endX; dx += source.Width)
                for (int dy = y; dy < endY; dy += source.Height) {

                    // adjust for obscured upper & left edges
                    int sourceX = 0, sourceY = 0, targetX = 0, targetY = 0;
                    if (dx < 0) sourceX = -dx; else targetX = dx;
                    if (dy < 0) sourceY = -dy; else targetY = dy;

                    // adjust for obscured lower & right edges
                    int width = Math.Min(source.Width - sourceX, target.Right - targetX);
                    int height = Math.Min(source.Height - sourceY, target.Bottom - targetY);
                    if (width <= 0 || height <= 0) continue;

                    // call mirroring overload only if necessary
                    RectI bounds = new RectI(sourceX, sourceY, width, height);
                    if (mirrorX || mirrorY)
                        targetBitmap.Read(targetX, targetY, frameBitmap, bounds, mirrorX, mirrorY);
                    else
                        targetBitmap.Read(targetX, targetY, frameBitmap, bounds);
                }
        }

        #endregion
        #region DrawOutline

        /// <summary>
        /// Draws a <see cref="RegularPolygon"/> outline centered on the specified region.</summary>
        /// <param name="context">
        /// The <see cref="DrawingContext"/> receiving the outline.</param>
        /// <param name="brush">
        /// The <see cref="Brush"/> used to draw the outline.</param>
        /// <param name="target">
        /// The region within <paramref name="context"/> on which to center the outline.</param>
        /// <param name="polygon">
        /// The <see cref="RegularPolygon"/> whose <see cref="RegularPolygon.Vertices"/> to draw.
        /// </param>

        private static void DrawOutline(DrawingContext context,
            Brush brush, Rect target, RegularPolygon polygon) {

            // shift origin to center of target bounds
            PointD offset = new PointD(
                target.X + target.Width / 2.0,
                target.Y + target.Height / 2.0);

            // draw polygon outline around frame
            var geometry = new PathGeometry() { Figures = { polygon.ToFigure(offset) } };
            context.DrawGeometry(null, new Pen(brush, 1.0), geometry);
        }

        #endregion
        #region MaskOutline

        /// <summary>
        /// Sets a <see cref="RegularPolygon"/> clipping mask centered on the specified region.
        /// </summary>
        /// <param name="context">
        /// The <see cref="DrawingContext"/> receiving the mask.</param>
        /// <param name="target">
        /// The region within <paramref name="context"/> on which to center the mask.</param>
        /// <param name="polygon">
        /// The <see cref="RegularPolygon"/> whose <see cref="RegularPolygon.Vertices"/> describe
        /// the mask.</param>

        private static void MaskOutline(DrawingContext context,
            Rect target, RegularPolygon polygon) {

            // shift origin to center of target bounds
            PointD offset = new PointD(
                target.X + target.Width / 2.0,
                target.Y + target.Height / 2.0);

            // apply polygon mask around frame
            var geometry = new PathGeometry() { Figures = { polygon.ToFigure(offset) } };
            context.PushClip(geometry);
        }

        #endregion
        #endregion
        #region CheckFrame

        /// <summary>
        /// Checks the specified region against the specified source bitmap.</summary>
        /// <param name="bitmap">
        /// The <see cref="ImageSource"/> containing the image frame to copy.</param>
        /// <param name="source">
        /// The region within <paramref name="bitmap"/> to copy.</param>
        /// <param name="polygon">
        /// The <see cref="RegularPolygon"/> whose <see cref="RegularPolygon.Vertices"/> provide the
        /// clipping region and black outline for the copied frame.</param>
        /// <returns>
        /// <c>true</c> if all arguments are valid; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="polygon"/> is a null reference.</exception>
        /// <remarks><para>
        /// <b>CheckFrame</b> returns <c>true</c> if all of the following conditions are met:
        /// </para><list type="bullet"><item>
        /// <paramref name="bitmap"/> is not a null reference.
        /// </item><item>
        /// <paramref name="source"/> has a positive <see cref="RectI.Width"/> and <see
        /// cref="RectI.Height"/>.
        /// </item><item>
        /// <paramref name="source"/> indicates a region within <paramref name="bitmap"/>.
        /// </item></list></remarks>

        public static bool CheckFrame(ImageSource bitmap, RectI source, RegularPolygon polygon) {
            if (polygon == null)
                ThrowHelper.ThrowArgumentNullException("polygon");

            // check for valid bitmap
            if (bitmap == null) return false;

            // check frame bounds against source bitmap
            return (source.Left >= 0 && source.Width > 0 && source.Right <= bitmap.Width &&
                source.Top >= 0 && source.Height > 0 && source.Bottom <= bitmap.Height);
        }

        #endregion
        #region DrawFrame

        /// <summary>
        /// Draws an image frame that is centered within the specified <see cref="RectD"/>.
        /// </summary>
        /// <param name="targetBitmap">
        /// The <see cref="WriteableBitmap"/> for the drawing, assuming that <paramref
        /// name="targetContext"/> is a null reference.</param>
        /// <param name="targetContext">
        /// The <see cref="DrawingContext"/> for the drawing, assuming that <paramref
        /// name="targetBitmap"/> is a null reference.</param>
        /// <param name="target">
        /// The region within <paramref name="targetContext"/> on which the copied image frame is
        /// centered.</param>
        /// <param name="sourceBitmap">
        /// A <see cref="WriteableBitmap"/> containing the image frame to copy.</param>
        /// <param name="source">
        /// The region within <paramref name="sourceBitmap"/> that covers the image frame to copy.
        /// </param>
        /// <param name="scalingX">
        /// An <see cref="ImageScaling"/> value indicating the horizontal scaling of the <paramref
        /// name="source"/> region to fit the specified <paramref name="target"/> region.</param>
        /// <param name="scalingY">
        /// An <see cref="ImageScaling"/> value indicating the vertical scaling of the <paramref
        /// name="source"/> region to fit the specified <paramref name="target"/> region.</param>
        /// <param name="colorShift">
        /// An optional <see cref="ColorVector"/> applied to all pixels within the drawing. Specify
        /// <see cref="ColorVector.Empty"/> if colors should remain unchanged.</param>
        /// <param name="offset">
        /// An optional pixel offset that is added to the centered location within the <paramref
        /// name="target"/> region.</param>
        /// <param name="scalingVector">
        /// An optional scaling vector for the drawing. Specify <see cref="PointI.Empty"/> if no
        /// scaling vector should be applied.</param>
        /// <exception cref="ArgumentException">
        /// <paramref name="targetBitmap"/> and <paramref name="targetContext"/> are both valid or
        /// both null references.</exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="sourceBitmap"/> is a null reference.</exception>
        /// <remarks><para>
        /// <b>DrawFrame</b> expects that the specified <paramref name="target"/> region equals the
        /// <see cref="RegularPolygon.Bounds"/> of the desired <see cref="RegularPolygon"/> shape.
        /// <b>DrawFrame</b> does not apply a mask or draw an outline around the frame.
        /// </para><para>
        /// Either the specified <paramref name="targetBitmap"/> or the specified <paramref
        /// name="targetContext"/> must be valid, but not both. <b>DrawFrame</b> internally draws to
        /// a <see cref="WriteableBitmap"/> whenever possible to preserve visual quality, then
        /// copies the result to the specified drawing target if different. Alpha blending is
        /// performed only when drawing to a valid <paramref name="targetContext"/>.
        /// </para><para>
        /// If <paramref name="targetBitmap"/> is valid, it must be locked before <b>DrawFrame</b>
        /// is called. If <paramref name="targetContext"/> is valid, the specified <paramref
        /// name="scalingVector"/> is applied to its transformation matrix; otherwise, negative
        /// components indicate mirroring along the corresponding axis of the <paramref
        /// name="targetBitmap"/>.</para></remarks>

        internal static void DrawFrame(WriteableBitmap targetBitmap,
            DrawingContext targetContext, RectD target,
            WriteableBitmap sourceBitmap, RectI source,
            ImageScaling scalingX, ImageScaling scalingY,
            ColorVector colorShift, PointI offset, PointI scalingVector) {

            // exactly one drawing target must be valid
            if ((targetContext == null && targetBitmap == null) ||
                (targetContext != null && targetBitmap != null))
                ThrowHelper.ThrowArgumentExceptionWithFormat(
                    "targetBitmap", Tektosyne.Strings.ArgumentConflict, targetContext);

            if (sourceBitmap == null)
                ThrowHelper.ThrowArgumentNullException("sourceBitmap");

            // copy source tile to buffer bitmap
            var frameBitmap = new WriteableBitmap(
                source.Width, source.Height, 96, 96, PixelFormats.Pbgra32, null);
            frameBitmap.Lock();
            frameBitmap.Read(0, 0, sourceBitmap, source);

            // shift color channels if desired
            if (!colorShift.IsEmpty)
                frameBitmap.Shift(colorShift.R, colorShift.G, colorShift.B);

            frameBitmap.Unlock();

            // round target coordinates for bitmaps
            RectI targetI = target.Round();

            // use WPF drawing for Stretch scaling, else bitmap copying
            if (scalingX == ImageScaling.Stretch || scalingY == ImageScaling.Stretch) {

                // create intermediate context if necessary
                DrawingVisual visual = null;
                if (targetContext == null) {
                    visual = new DrawingVisual();
                    targetContext = visual.RenderOpen();
                }

                // draw frame to target or intermediate context
                DrawFrameCore(targetContext, target, frameBitmap,
                    scalingX, scalingY, offset, scalingVector);

                // copy intermediate context to target bitmap
                if (visual != null) {
                    targetContext.Close();
                    var bitmap = new RenderTargetBitmap(
                        targetI.Right, targetI.Bottom, 96, 96, PixelFormats.Pbgra32);
                    bitmap.Render(visual);
                    targetBitmap.Read(targetI.X, targetI.Y, bitmap, targetI);
                }
            } else {
                // create intermediate bitmap if necessary
                if (targetContext != null) {
                    Debug.Assert(targetBitmap == null);
                    targetBitmap = new WriteableBitmap(
                        targetI.Right, targetI.Bottom, 96, 96, PixelFormats.Pbgra32, null);
                    targetBitmap.Lock();
                }

                // draw frame to target or intermediate bitmap
                DrawFrameCore(targetBitmap, targetI, frameBitmap,
                    scalingX, scalingY, offset, scalingVector);

                // copy intermediate bitmap to target context
                if (targetContext != null) {
                    targetBitmap.Unlock();
                    targetContext.DrawImage(targetBitmap,
                        new Rect(0, 0, targetBitmap.Width, targetBitmap.Height));
                }
            }
        }

        #endregion
        #region DrawInvalidIcon

        /// <summary>
        /// Draws an "invalid" icon that is centered within the specified <see
        /// cref="RegularPolygon"/>.</summary>
        /// <param name="context">
        /// The <see cref="DrawingContext"/> for the drawing.</param>
        /// <param name="brush">
        /// The <see cref="Brush"/> used to draw the <paramref name="polygon"/> outline.</param>
        /// <param name="target">
        /// The region within <paramref name="context"/> on which the copied "invalid" icon is
        /// centered.</param>
        /// <param name="polygon">
        /// The <see cref="RegularPolygon"/> whose <see cref="RegularPolygon.Vertices"/> provide the
        /// drawing size and black outline for the "invalid" icon.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="context"/> or <paramref name="polygon"/> is a null reference.
        /// </exception>
        /// <remarks>
        /// <b>DrawInvalidIcon</b> draws the icon returned by <see cref="Images.Invalid"/>,
        /// surrounded by an outline of the specified <paramref name="polygon"/> drawn using the
        /// specified <paramref name="brush"/>.</remarks>

        internal static void DrawInvalidIcon(DrawingContext context,
            Brush brush, Rect target, RegularPolygon polygon) {

            if (context == null)
                ThrowHelper.ThrowArgumentNullException("context");
            if (polygon == null)
                ThrowHelper.ThrowArgumentNullException("polygon");

            // compute scaled bounds for "invalid" icon
            double diameter = 1.6f * polygon.InnerRadius;
            double x = target.X + (target.Width - diameter) / 2f;
            double y = target.Y + (target.Height - diameter) / 2f;

            // draw icon with polygon outline
            context.DrawImage(Images.Invalid, new Rect(x, y, diameter, diameter));
            DrawOutline(context, brush, target, polygon);
        }

        #endregion
        #region DrawOutlineFrame

        /// <summary>
        /// Draws an image frame that is centered within, masked and outlined by the specified <see
        /// cref="RegularPolygon"/>.</summary>
        /// <param name="context">
        /// The <see cref="DrawingContext"/> for the drawing.</param>
        /// <param name="brush">
        /// The <see cref="Brush"/> used to draw the <paramref name="polygon"/> outline.</param>
        /// <param name="target">
        /// The region within <paramref name="context"/> on which the copied image frame is
        /// centered.</param>
        /// <param name="sourceBitmap">
        /// A <see cref="WriteableBitmap"/> containing the image frame to copy.</param>
        /// <param name="source">
        /// The region within <paramref name="sourceBitmap"/> that covers the image frame to copy.
        /// </param>
        /// <param name="polygon">
        /// The <see cref="RegularPolygon"/> whose <see cref="RegularPolygon.Vertices"/> provide the
        /// clipping region and outline for the copied frame.</param>
        /// <param name="scalingX">
        /// An <see cref="ImageScaling"/> value indicating the horizontal scaling of the <paramref
        /// name="source"/> region to fit the specified <paramref name="polygon"/>.</param>
        /// <param name="scalingY">
        /// An <see cref="ImageScaling"/> value indicating the vertical scaling of the <paramref
        /// name="source"/> region to fit the specified <paramref name="polygon"/>.</param>
        /// <param name="colorShift">
        /// An optional <see cref="ColorVector"/> applied to all pixels within the drawing. Specify
        /// <see cref="ColorVector.Empty"/> if colors should remain unchanged.</param>
        /// <param name="offset">
        /// An optional pixel offset that is added to the centered location within the <paramref
        /// name="target"/> region.</param>
        /// <param name="scalingVector">
        /// An optional scaling vector that is applied to the transformation matrix of the specified
        /// <paramref name="context"/> object. Specify <see cref="PointI.Empty"/> if no scaling
        /// vector should be applied.</param>
        /// <returns>
        /// <c>true</c> if <see cref="CheckFrame"/> succeeded with the specified arguments;
        /// otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="context"/> or <paramref name="polygon"/> is a null reference.
        /// </exception>
        /// <remarks>
        /// <b>DrawOutlineFrame</b> always masks off the specified <paramref name="context"/> with
        /// the specified <paramref name="polygon"/>, and superimposes its outline on the drawn
        /// frame using the specified <paramref name="brush"/>.</remarks>

        public static bool DrawOutlineFrame(DrawingContext context,
            Brush brush, Rect target, WriteableBitmap sourceBitmap, RectI source,
            RegularPolygon polygon, ImageScaling scalingX, ImageScaling scalingY,
            ColorVector colorShift, PointI offset, PointI scalingVector) {

            // check drawing parameters
            if (!CheckFrame(sourceBitmap, source, polygon))
                return false;

            // compute frame bounds centered on target region
            RectD frameTarget = new RectD(
                target.X + (target.Width - polygon.Bounds.Width) / 2.0,
                target.Y + (target.Height - polygon.Bounds.Height) / 2.0,
                polygon.Bounds.Width, polygon.Bounds.Height);

            // mask off region outside polygon outline
            MaskOutline(context, target, polygon);

            // draw frame with specified display parameters
            DrawFrame(null, context, frameTarget, sourceBitmap, source,
                scalingX, scalingY, colorShift, offset, scalingVector);

            // draw polygon outline around frame
            context.Pop();
            DrawOutline(context, brush, target, polygon);

            return true;
        }

        #endregion
    }
}
