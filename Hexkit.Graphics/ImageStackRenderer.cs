using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;

using Tektosyne;
using Tektosyne.Collections;
using Tektosyne.Geometry;

using Hexkit.Global;
using Hexkit.Scenario;

namespace Hexkit.Graphics {
    #region Type Aliases

    using ImageStackEntryList = ListEx<ImageStackEntry>;
    using IndexList = ListEx<Int32>;

    #endregion

    /// <summary>
    /// Shows a stack of <see cref="EntityImage"/> objects.</summary>
    /// <remarks><para>
    /// <b>ImageStackRenderer</b> shows a stack of <see cref="EntityImage"/> objects outside of a
    /// map view display. All images are transparently superimposed from first to last, as on a map
    /// polygon.
    /// </para><para>
    /// <b>ImageStackRenderer</b> can display two kinds of items that wrap <see cref="EntityImage"/>
    /// objects:
    /// </para><list type="bullet"><item>
    /// For <see cref="ImageStackEntry"/> items, the <see cref="ImageStackEntry.SingleFrame"/> is
    /// shown, if valid; otherwise, the first <see cref="EntityImage.Frames"/> element.
    /// </item><item>
    /// For <see cref="EntityTemplate"/> items, the <see cref="ImageStackEntry.SingleFrame"/> of
    /// each <see cref="ImageStackEntry"/> is shown, if valid; otherwise, the <see
    /// cref="EntityImage.Frames"/> element at the template's <see
    /// cref="EntityTemplate.FrameOffset"/>.
    /// </item></list><note type="implementnotes">
    /// <b>ImageStackRenderer</b> requires valid <see cref="ImageFile"/> bitmaps. It will only show
    /// "invalid" icons if the image files have not yet been loaded, or if they have already been
    /// unloaded.</note></remarks>

    public sealed class ImageStackRenderer: FrameworkElement {
        #region Private Fields

        // property backers
        private readonly ImageStackEntryList _imageStack = new ImageStackEntryList();
        private readonly IndexList _frames = new IndexList();
        private RegularPolygon _polygon = AreaSection.DefaultPolygon;

        #endregion
        #region Frames

        /// <summary>
        /// Gets the indices of all image frames shown by the <see cref="ImageStackRenderer"/>.
        /// </summary>
        /// <value>
        /// A read-only <see cref="IndexList"/> containing the indices of all image frames shown for
        /// the <see cref="ImageStack"/> element at the same index position. The default is an empty
        /// collection.</value>
        /// <remarks><para>
        /// <b>Frames</b> never returns a null reference. This property is set by the <see
        /// cref="Clear"/> and <see cref="Show"/> methods.
        /// </para><para>
        /// When a given <b>Frames</b> element indicates a frame index that exceeds the number of
        /// available <see cref="EntityImage.Frames"/> for the corresponding <see
        /// cref="ImageStack"/> element, its value is taken <em>modulo</em> the number of available
        /// <see cref="EntityImage.Frames"/>.</para></remarks>

        public IndexList Frames {
            [DebuggerStepThrough]
            get { return this._frames.AsReadOnly(); }
        }

        #endregion
        #region ImageStack

        /// <summary>
        /// Gets a list of all images shown by the <see cref="ImageStackRenderer"/>.</summary>
        /// <value>
        /// A read-only <see cref="ImageStackEntryList"/> containing all <see
        /// cref="ImageStackEntry"/> instances whose frames are shown by the <see
        /// cref="ImageStackRenderer"/>. The default is an empty collection.</value>
        /// <remarks><para>
        /// <b>ImageStack</b> never returns a null reference. This property is set by the <see
        /// cref="Clear"/> and <see cref="Show"/> methods.
        /// </para><para>
        /// If <b>ImageStack</b> is an empty collection, only the "invalid" icon returned by <see
        /// cref="Images.Invalid"/> is shown.</para></remarks>

        public ImageStackEntryList ImageStack {
            [DebuggerStepThrough]
            get { return this._imageStack.AsReadOnly(); }
        }

        #endregion
        #region Polygon

        /// <summary>
        /// Gets or sets the bounding <see cref="RegularPolygon"/> for the <see
        /// cref="ImageStackRenderer"/>.</summary>
        /// <value>
        /// A <see cref="RegularPolygon"/> providing the maximum size and outline for all images.
        /// The default is <see cref="AreaSection.DefaultPolygon"/>.</value>
        /// <exception cref="ArgumentNullException">
        /// The property is set to a null reference.</exception>
        /// <remarks>
        /// Setting <b>Polygon</b> redraws the <see cref="ImageStackRenderer"/> if the new value
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
        #region Private Methods
        #region AddEntityTemplate

        /// <summary>
        /// Adds <see cref="ImageStack"/> and <see cref="Frames"/> elements from the specified <see
        /// cref="EntityTemplate"/>.</summary>
        /// <param name="template">
        /// The <see cref="EntityTemplate"/> to show.</param>
        /// <remarks><para>
        /// <b>AddEntityTemplate</b> adds the entire <see cref="EntityClass.ImageStack"/> and the
        /// <see cref="EntityTemplate.FrameOffset"/> value of the specified <paramref
        /// name="template"/> to the <see cref="ImageStack"/> and <see cref="Frames"/> collections,
        /// respectively.
        /// </para><para>
        /// If the <see cref="ImageStackEntry.SingleFrame"/> value of an <see cref="ImageStack"/>
        /// element is non-negative, that value is added to the corresponding index positions in the
        /// <see cref="Frames"/> collection instead.
        /// </para><para>
        /// <b>AddEntityTemplate</b> adds an empty <see cref="ImageStackEntry"/> to
        /// <b>ImageStack</b> and the value zero to <b>Frames</b> if <paramref name="template"/>
        /// does not reference a valid entity class.</para></remarks>

        private void AddEntityTemplate(EntityTemplate template) {

            // get associated entity class
            EntityClass entityClass = MasterSection.Instance.Entities.GetEntity(template);

            // empty stack entry shows "invalid" icon
            if (entityClass == null) {
                this._imageStack.Add(new ImageStackEntry());
                this._frames.Add(0);
                return;
            }

            // show all images with frame offset
            foreach (ImageStackEntry entry in entityClass.ImageStack) {
                this._imageStack.Add(entry); // Image may be null
                this._frames.Add(entry.SingleFrame < 0 ? template.FrameOffset : entry.SingleFrame);
            }
        }

        #endregion
        #region DrawImageStack

        /// <summary>
        /// Draws the <see cref="ImageStack"/> to the specified rectangle.</summary>
        /// <param name="context">
        /// The <see cref="DrawingContext"/> for the drawing.</param>
        /// <param name="target">
        /// The drawing region within <paramref name="context"/>, in device-independent pixels.
        /// </param>
        /// <returns>
        /// <c>true</c> if all <see cref="ImageStack"/> elements were drawn without errors;
        /// otherwise, <c>false</c>.</returns>
        /// <remarks>
        /// <b>DrawImageStack</b> draws the selected <see cref="Frames"/> of all <see
        /// cref="ImageStack"/> elements to the specified <paramref name="target"/> rectangle within
        /// the specified <paramref name="context"/>.</remarks>

        private bool DrawImageStack(DrawingContext context, Rect target) {
            Debug.Assert(ImageStack.Count == Frames.Count);

            // fail if no images are available
            if (ImageStack.Count == 0) return false;

            // draw selected frames of all images
            for (int i = 0; i < ImageStack.Count; i++) {
                ImageStackEntry entry = ImageStack[i];

                // quit on first unavailable image
                EntityImage image = entry.Image.Value;
                if (image == null) return false;

                // determine selected frame
                int frameIndex = Frames[i] % image.Frames.Count;
                ImageFrame frame = image.Frames[frameIndex];

                // quit on first unavailable bitmap
                if (frame.Source.Value == null) return false;

                // get desired image scaling, unless disabled
                ImageScaling scalingX = ImageScaling.None, scalingY = ImageScaling.None;
                if (!entry.UseUnscaled) {
                    scalingX = image.ScalingX;
                    scalingY = image.ScalingY;
                }

                // draw frame with specified display parameters
                if (!ImageUtility.DrawOutlineFrame(context, Brushes.Black, target,
                    frame.Source.Value.Bitmap, frame.Bounds, Polygon, scalingX, scalingY,
                    entry.ColorShift, entry.Offset, entry.ScalingVector))
                    return false;
            }

            return true;
        }

        #endregion
        #endregion
        #region Clear

        /// <summary>
        /// Clears the display of the <see cref="ImageStackRenderer"/>.</summary>
        /// <remarks>
        /// <b>Clear</b> clears the <see cref="ImageStack"/> and <see cref="Frames"/> collections,
        /// causing the <see cref="ImageStackRenderer"/> to show an "invalid" icon.</remarks>

        public void Clear() {
            this._imageStack.Clear();
            this._frames.Clear();
            InvalidateVisual();
        }

        #endregion
        #region OnRender

        /// <summary>
        /// Renders the visual content of the <see cref="ImageStackRenderer"/>.</summary>
        /// <param name="context">
        /// The <see cref="DrawingContext"/> for the rendering.</param>
        /// <remarks><para>
        /// <b>OnRender</b> attempts to draw all <see cref="ImageStack"/> elements within the <see
        /// cref="RegularPolygon.Bounds"/> of the current <see cref="Polygon"/>. The drawing is
        /// centered within the <see cref="ImageStackRenderer"/>.
        /// </para><para>
        /// If drawing fails for any reason, <b>OnRender</b> adds an "invalid" icon on top of
        /// whatever <see cref="ImageStack"/> elements that are already present.</para></remarks>

        protected override void OnRender(DrawingContext context) {
            base.OnRender(context);

            // target rectangle centered on viewport
            Rect target = new Rect(
                (ActualWidth - Polygon.Bounds.Width) / 2.0,
                (ActualHeight - Polygon.Bounds.Height) / 2.0,
                Polygon.Bounds.Width, Polygon.Bounds.Height);

            // draw image stack, or "invalid" icon on failure
            if (!DrawImageStack(context, target))
                ImageUtility.DrawInvalidIcon(context, Brushes.Black, target, Polygon);
        }

        #endregion
        #region Show(EntityTemplate)

        /// <overloads>
        /// Shows the specified entities or images.</overloads>
        /// <summary>
        /// Shows the specified <see cref="EntityTemplate"/>.</summary>
        /// <param name="template">
        /// The <see cref="EntityTemplate"/> to show.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="template"/> is a null reference.</exception>
        /// <remarks><para>
        /// This <b>Show</b> overload performs the following actions:
        /// </para><list type="bullet"><item>
        /// Clear the <see cref="ImageStack"/> and <see cref="Frames"/> collections.
        /// </item><item>
        /// Add all <see cref="ImageStackEntry"/> instances associated with the specified <paramref
        /// name="template"/> to the <b>ImageStack</b>.
        /// </item><item>
        /// Add its <see cref="EntityTemplate.FrameOffset"/> value to all corresponding index
        /// positions in <b>Frames</b>; unless the <see cref="ImageStackEntry.SingleFrame"/> value
        /// of an <see cref="ImageStackEntry"/> is non-negative, in which case that value is added.
        /// </item></list><para>
        /// <b>Show</b> adds a null reference to the <b>ImageStack</b> and the value zero to
        /// <b>Frames</b> if <paramref name="template"/> does not reference a valid entity class.
        /// </para></remarks>

        public void Show(EntityTemplate template) {
            if (template == null)
                ThrowHelper.ThrowArgumentNullException("template");

            this._imageStack.Clear();
            this._frames.Clear();

            AddEntityTemplate(template);
            InvalidateVisual();
        }

        #endregion
        #region Show(ICollection<EntityTemplate>)

        /// <summary>
        /// Shows the specified <see cref="EntityTemplate"/> collection.</summary>
        /// <param name="templates">
        /// A <see cref="ICollection{T}"/> containing the <see cref="EntityTemplate"/> objects to
        /// show.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="templates"/> is a null reference.</exception>
        /// <remarks><para>
        /// This <b>Show</b> overload performs the following actions:
        /// </para><list type="bullet"><item>
        /// Clear the <see cref="ImageStack"/> and <see cref="Frames"/> collections.
        /// </item><item>
        /// Add all <see cref="ImageStackEntry"/> instances associated with all specified <paramref
        /// name="templates"/> to the <b>ImageStack</b>.
        /// </item><item>
        /// Add the <see cref="EntityTemplate.FrameOffset"/> value of all <paramref
        /// name="templates"/> to all corresponding index positions in <b>Frames</b>; unless the
        /// <see cref="ImageStackEntry.SingleFrame"/> value of an <see cref="ImageStackEntry"/> is
        /// non-negative, in which case that value is added.
        /// </item></list><para>
        /// <b>Show</b> adds a null reference to the <b>ImageStack</b> and the value zero to
        /// <b>Frames</b> for all <paramref name="templates"/> that do not reference a valid <see
        /// cref="EntityTemplate.EntityClass"/>.</para></remarks>

        public void Show(ICollection<EntityTemplate> templates) {
            if (templates == null)
                ThrowHelper.ThrowArgumentNullException("templates");

            this._imageStack.Clear();
            this._frames.Clear();

            foreach (EntityTemplate template in templates)
                AddEntityTemplate(template);

            InvalidateVisual();
        }

        #endregion
        #region Show(ICollection<ImageStackEntry>)

        /// <summary>
        /// Shows the specified <see cref="ImageStackEntry"/> collection.</summary>
        /// <param name="entries">
        /// An <see cref="ICollection{T}"/> containing the <see cref="ImageStackEntry"/> objects to
        /// show.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="entries"/> is a null reference.</exception>
        /// <remarks><para>
        /// This <b>Show</b> overload performs the following actions:
        /// </para><list type="bullet"><item>
        /// Clear the <see cref="ImageStack"/> and <see cref="Frames"/> collections.
        /// </item><item>
        /// Add all specified <paramref name="entries"/> to the <b>ImageStack</b>.
        /// </item><item>
        /// Add the value zero to all corresponding index positions in <b>Frames</b>; unless the
        /// <see cref="ImageStackEntry.SingleFrame"/> value of an element is non-negative, in which
        /// case that value is added.</item></list></remarks>

        public void Show(ICollection<ImageStackEntry> entries) {
            if (entries == null)
                ThrowHelper.ThrowArgumentNullException("entries");

            this._imageStack.Clear();
            this._frames.Clear();

            foreach (ImageStackEntry entry in entries) {
                this._imageStack.Add(entry); // Image may be null
                this._frames.Add(entry.SingleFrame < 0 ? 0 : entry.SingleFrame);
            }

            InvalidateVisual();
        }

        #endregion
    }
}
