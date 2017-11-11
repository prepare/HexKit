using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using Tektosyne.Geometry;
using Tektosyne.Windows;

using Hexkit.Global;
using Hexkit.Scenario;

namespace Hexkit.Graphics {

    /// <summary>
    /// Provides a <see cref="ListBoxItem"/> that shows entity images or image frames.</summary>
    /// <remarks><para>
    /// <b>ImageListBoxItem</b> represents an item of an <see cref="ImageListBox"/> and provides
    /// custom rendering for three types of <see cref="ContentControl.Content"/> objects:
    /// </para><list type="bullet"><item>
    /// For <see cref="EntityImage"/> contents, the first <see cref="EntityImage.Frames"/> element
    /// is drawn, labelled with and sorted in natural order by the image's <see
    /// cref="EntityImage.Id"/> string. This option is intended for showing a series of images.
    /// </item><item>
    /// For <see cref="ImageFrame"/> contents, the region indicated by the frame's <see
    /// cref="ImageFrame.Bounds"/> within the associated <see cref="ImageFrame.Source"/> bitmap is
    /// drawn. This option is intended for showing the frames of a single image.
    /// </item><item>
    /// For <see cref="RectI"/> contents, the indicated region within the current <see
    /// cref="ImageListBox.FrameBitmap"/> is drawn. This option is intended for showing the combined
    /// frames of a single entity class.
    /// </item></list><para>
    /// All drawn <see cref="ItemsControl.Items"/> are surrounded with a black outline of the
    /// current <see cref="ImageListBox.Polygon"/>. For <see cref="ImageFrame"/> and <see
    /// cref="RectI"/> items, the <see cref="ImageListBox.ScalingX"/> and <see
    /// cref="ImageListBox.ScalingY"/> properties determine if and how the images are scaled to fit
    /// the current <see cref="ImageListBox.Polygon"/>.
    /// </para><note type="implementnotes">
    /// <b>ImageListBoxItem</b> requires valid <see cref="ImageFile"/> bitmaps to display <see
    /// cref="EntityImage"/> or <see cref="ImageFrame"/> items. It will only show "invalid" icons if
    /// the image files have not yet been loaded, or if they have already been unloaded.
    /// </note></remarks>

    public sealed class ImageListBoxItem: ListBoxItem {
        #region ImageListBoxItem()

        /// <overloads>
        /// Initializes a new instance of the <see cref="ImageListBoxItem"/> class.</overloads>
        /// <summary>
        /// Initializes a new instance of the <see cref="ImageListBoxItem"/> class with default
        /// properties.</summary>

        public ImageListBoxItem() {
            this._labelTypeface = WindowsExtensions.GetTypeface(this);
            this._labelEmSize = FontSize;
        }

        #endregion
        #region ImageListBoxItem(EntityImage)

        /// <summary>
        /// Initializes a new instance of the <see cref="ImageListBoxItem"/> class with the
        /// specified <see cref="EntityImage"/> content.</summary>
        /// <param name="image">
        /// The <see cref="EntityImage"/> that is the initial value for the <see
        /// cref="ContentControl.Content"/> property.</param>

        public ImageListBoxItem(EntityImage image): this() {
            Content = image;
            if (image != null) ToolTip = image.Id;
        }

        #endregion
        #region ImageListBoxItem(ImageFrame)

        /// <summary>
        /// Initializes a new instance of the <see cref="ImageListBoxItem"/> class with the
        /// specified <see cref="ImageFrame"/> content.</summary>
        /// <param name="frame">
        /// The <see cref="ImageFrame"/> that is the initial value for the <see
        /// cref="ContentControl.Content"/> property.</param>

        public ImageListBoxItem(ImageFrame frame): this() {
            Content = frame;
        }

        #endregion
        #region ImageListBoxItem(RectI)

        /// <summary>
        /// Initializes a new instance of the <see cref="ImageListBoxItem"/> class with the
        /// specified <see cref="RectI"/> content.</summary>
        /// <param name="rectangle">
        /// The <see cref="RectI"/> instance that is the initial value for the <see
        /// cref="ContentControl.Content"/> property.</param>

        public ImageListBoxItem(RectI rectangle): this() {
            Content = rectangle;
        }

        #endregion
        #region Private Fields

        // parameters for text label
        private readonly Typeface _labelTypeface;
        private readonly double _labelEmSize;
        private double _labelHeight;

        // total item layout size
        private Size _layoutSize;

        #endregion
        #region SortKey

        /// <summary>
        /// Gets the sorting key for the <see cref="ImageListBoxItem"/>.</summary>
        /// <value>
        /// The <see cref="EntityImage.Id"/> string of the contained <see cref="EntityImage"/>, if
        /// any; otherwise, an empty string.</value>
        /// <remarks>
        /// <b>SortKey</b> is used by the <see cref="ImageListBox.Insert"/> method of the <see
        /// cref="ImageListBox"/> class to determine the index position of the <see
        /// cref="ImageListBoxItem"/>.</remarks>

        public string SortKey {
            get {
                EntityImage image = Content as EntityImage;
                return (image == null ? "" : image.Id);
            }
        }

        #endregion
        #region ArrangeOverride

        /// <summary>
        /// Determines the final layout size of the <see cref="ImageListBoxItem"/>.</summary>
        /// <param name="finalSize">
        /// The layout size available to the <see cref="ImageListBoxItem"/>.</param>
        /// <returns>
        /// The actual layout size used by the <see cref="ImageListBoxItem"/>.</returns>
        /// <remarks>
        /// <b>ArrangeOverride</b> ignores the specified <paramref name="finalSize"/> and always
        /// returns the <see cref="Size"/> determined by <see cref="MeasureOverride"/>.</remarks>

        protected override Size ArrangeOverride(Size finalSize) {
            return (this._layoutSize == new Size() ?
                base.ArrangeOverride(finalSize) : this._layoutSize);
        }

        #endregion
        #region FormatLabel

        /// <summary>
        /// Formats the specified text as a label for the <see cref="ImageListBoxItem"/>.</summary>
        /// <param name="label">
        /// The text to format.</param>
        /// <param name="size">
        /// The maximum width and height for the label.</param>
        /// <returns>
        /// A <see cref="FormattedText"/> label that contains the specified <paramref name="label"/>
        /// with the specified maximum <paramref name="size"/>.</returns>

        private FormattedText FormatLabel(string label, Size size) {

            FormattedText text = new FormattedText(label, ApplicationInfo.Culture,
                FlowDirection.LeftToRight, this._labelTypeface, this._labelEmSize, Foreground);

            // center text within label bounds
            text.MaxTextHeight = size.Height;
            text.MaxTextWidth = size.Width;
            text.TextAlignment = TextAlignment.Center;
            text.Trimming = TextTrimming.CharacterEllipsis;

            return text;
        }

        #endregion
        #region MeasureOverride

        /// <summary>
        /// Measures the desired layout size of the <see cref="ImageListBoxItem"/>.</summary>
        /// <param name="availableSize">
        /// The layout size available to the <see cref="ImageListBoxItem"/>.</param>
        /// <returns>
        /// The layout size desired by the <see cref="ImageListBoxItem"/>.</returns>
        /// <remarks>
        /// <b>MeasureOverride</b> considers the <see cref="ContentControl.Content"/> type and the
        /// bounding box of the current <see cref="ImageListBox.Polygon"/>. <see
        /// cref="EntityImage"/> items show an identifier below the picture, other items only the
        /// picture itself.</remarks>

        protected override Size MeasureOverride(Size availableSize) {

            // default to base class if not in an ImageListBox
            ImageListBox listBox = Parent as ImageListBox;
            if (listBox == null) return base.MeasureOverride(availableSize);

            // start with polygon bounds plus padding
            Size size = new Size(
                listBox.Polygon.Bounds.Width + 8,
                listBox.Polygon.Bounds.Height + 8);

            if (Content is EntityImage) {
                // items should hold polygon plus label
                this._labelHeight = 3 * this._labelEmSize;
                this._layoutSize = new Size(
                    Math.Max(size.Width, 8 * this._labelEmSize),
                    size.Height + this._labelHeight);
            } else {
                // items should hold polygon
                this._labelHeight = 0;
                this._layoutSize = size;
            }

            return this._layoutSize;
        }

        #endregion
        #region OnRender

        /// <summary>
        /// Renders the visual content of the <see cref="ImageListBoxItem"/>.</summary>
        /// <param name="context">
        /// The <see cref="DrawingContext"/> for the rendering.</param>
        /// <remarks><para>
        /// <b>OnRender</b> calls the base class implementation of <see cref="UIElement.OnRender"/>,
        /// and then attempts to display the current <see cref="ContentControl.Content"/> of the
        /// <see cref="ImageListBoxItem"/>, depending on its type:
        /// </para><list type="bullet"><item>
        /// If the element is an <see cref="EntityImage"/>, its first image frame is drawn above a
        /// text label showing its identifier.
        /// </item><item>
        /// If the element is an <see cref="ImageFrame"/>, the frame is drawn without a label.
        /// </item><item>
        /// If the element is a <see cref="RectI"/>, the corresponding region of the <see
        /// cref="ImageListBox.FrameBitmap"/> is drawn without a label.
        /// </item></list><para>
        /// If the element is of any other type, or if an error occurs, the "invalid" icon returned
        /// by <see cref="Images.Invalid"/> is drawn instead.</para></remarks>

        protected override void OnRender(DrawingContext context) {
            base.OnRender(context);

            // default to base class if not in an ImageListBox
            ImageListBox listBox = Parent as ImageListBox;
            if (listBox == null) return;

            // get drawing parameters of containing ImageListBox
            WriteableBitmap bitmap = listBox.FrameBitmap;
            ImageScaling scalingX = listBox.ScalingX, scalingY = listBox.ScalingY;

            // source within bitmap and target within context
            RectI source = RectI.Empty;
            Rect target = new Rect(0, 0, ActualWidth, ActualHeight);

            /*
             * The default Background brush of a ListBoxItem is transparent, meaning that an item
             * will not register clicks on anything but the non-transparent image and text label.
             * 
             * This is very inconvenient, so we always draw a fully opaque background. We use the
             * ListBox.Background brush for regular items, and the system-defined HighlightBrush
             * for selected items. The Foreground brush changes automatically as appropriate.
             * 
             * NOTE: The HighlightBrush must be drawn explicitly anyway, or else the Foreground
             * of a selected item would be invisible against the Background of the ListBox!
             */

            Brush background = (listBox.SelectedItem == this ?
                SystemColors.HighlightBrush : listBox.Background);
            context.DrawRectangle(background, null, target);

            // check which content we're drawing
            if (Content is RectI)
                source = (RectI) Content;
            else {
                ImageFrame frame = Content as ImageFrame;
                if (frame == null) {
                    EntityImage image = Content as EntityImage;
                    if (image == null) goto failure;

                    // get first image frame and scaling
                    frame = image.Frames[0];
                    scalingX = image.ScalingX;
                    scalingY = image.ScalingY;

                    // compute bounds of image label
                    Point textLocation = new Point(0, ActualHeight - this._labelHeight);
                    Size textSize = new Size(ActualWidth, this._labelHeight);

                    // reduce image bounds accordingly
                    target.Height -= textSize.Height;

                    // draw text for image label
                    FormattedText text = FormatLabel(image.Id, textSize);
                    context.DrawText(text, textLocation);
                }

                // final sanity check for valid image frame
                if (frame == null || frame.Source.Value == null)
                    goto failure;

                // draw image frame with associated bitmap
                bitmap = frame.Source.Value.Bitmap;
                source = frame.Bounds;
            }

            // draw specified rectangle with desired bitmap & scaling
            if (ImageUtility.DrawOutlineFrame(context, Foreground, target, bitmap, source,
                listBox.Polygon, scalingX, scalingY, ColorVector.Empty, PointI.Empty, PointI.Empty))
                return;

        failure:
            // draw "invalid" icon on failure
            ImageUtility.DrawInvalidIcon(context, Foreground, target, listBox.Polygon);
        }

        #endregion
    }
}
