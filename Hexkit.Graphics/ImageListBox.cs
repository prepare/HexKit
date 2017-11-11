using System;
using System.Collections;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media.Imaging;

using Tektosyne;
using Tektosyne.Geometry;

using Hexkit.Global;
using Hexkit.Scenario;

namespace Hexkit.Graphics {

    /// <summary>
    /// Provides a <see cref="ListBox"/> that shows a collection of frames or images.</summary>
    /// <remarks>
    /// <b>ImageListBox</b> provides several properties to control the rendering of any contained
    /// <see cref="ImageListBoxItem"/> elements. Please see there for details.</remarks>

    public sealed class ImageListBox: ListBox {
        #region ImageListBox()

        /// <summary>
        /// Initializes a new instance of the <see cref="ImageListBox"/> class.</summary>

        public ImageListBox() {
            ApplicationUtility.ApplyDefaultStyle(this);
        }

        #endregion
        #region Private Fields

        // property backers
        private WriteableBitmap _frameBitmap;
        private RegularPolygon _polygon = AreaSection.DefaultPolygon;
        private ImageScaling _scalingX = ImageScaling.None, _scalingY = ImageScaling.None;

        #endregion
        #region FrameBitmap

        /// <summary>
        /// Gets or sets the bitmap providing images for <see cref="RectI"/> items.</summary>
        /// <value><para>
        /// A <see cref="WriteableBitmap"/> providing images for all <see cref="RectI"/> elements in
        /// the <see cref="ItemsControl.Items"/> collection.
        /// </para><para>-or-</para><para>
        /// A null reference if the <see cref="ItemsControl.Items"/> collection contains <see
        /// cref="EntityImage"/> or <see cref="ImageFrame"/> elements. The default is a null
        /// reference.</para></value>
        /// <remarks>
        /// Setting <b>FrameBitmap</b> redraws the <see cref="ImageListBox"/> and selects the first
        /// element in the <see cref="ItemsControl.Items"/> collection, if any.</remarks>

        public WriteableBitmap FrameBitmap {
            [DebuggerStepThrough]
            get { return this._frameBitmap; }
            set {
                this._frameBitmap = value;
                if (Items.Count > 0) SelectedIndex = 0;
                Redraw();
            }
        }

        #endregion
        #region Polygon

        /// <summary>
        /// Gets or sets the bounding <see cref="RegularPolygon"/> for each item in the <see
        /// cref="ImageListBox"/>.</summary>
        /// <value>
        /// A <see cref="RegularPolygon"/> providing the maximum size and outline for all elements
        /// in the <see cref="ItemsControl.Items"/> collection. The default is <see
        /// cref="AreaSection.DefaultPolygon"/>.</value>
        /// <exception cref="ArgumentNullException">
        /// The property is set to a null reference.</exception>
        /// <remarks>
        /// <b>Polygon</b> never returns a null reference. Setting this property redraws the <see
        /// cref="ImageListBox"/> and selects the first element in the <see
        /// cref="ItemsControl.Items"/> collection, if any.</remarks>

        public RegularPolygon Polygon {
            [DebuggerStepThrough]
            get { return this._polygon; }
            set {
                if (value == null)
                    ThrowHelper.ThrowArgumentNullException("value");

                this._polygon = value;
                if (Items.Count > 0) SelectedIndex = 0;
                Redraw();
            }
        }

        #endregion
        #region ScalingX

        /// <summary>
        /// Gets or sets a value indicating the horizontal scaling of images for <see
        /// cref="ImageFrame"/> and <see cref="RectI"/> items to fit the current <see
        /// cref="Polygon"/>.</summary>
        /// <value>
        /// An <see cref="ImageScaling"/> value indicating if and how <see cref="ImageFrame"/> and
        /// <see cref="RectI"/> elements in the <see cref="ItemsControl.Items"/> collection are
        /// scaled horizontally to fit the current <see cref="Polygon"/>. The default is <see
        /// cref="ImageScaling.None"/>.</value>
        /// <remarks>
        /// Setting <b>ScalingX</b> redraws the <see cref="ImageListBox"/>. The value of this
        /// property is ignored if the <see cref="ItemsControl.Items"/> collection contains <see
        /// cref="EntityImage"/> elements.</remarks>

        public ImageScaling ScalingX {
            [DebuggerStepThrough]
            get { return this._scalingX; }
            set {
                this._scalingX = value;
                Redraw();
            }
        }

        #endregion
        #region ScalingY

        /// <summary>
        /// Gets or sets a value indicating the vertical scaling of images for <see
        /// cref="ImageFrame"/> and <see cref="RectI"/> items to fit the current <see
        /// cref="Polygon"/>.</summary>
        /// <value>
        /// An <see cref="ImageScaling"/> value indicating if and how <see cref="ImageFrame"/> and
        /// <see cref="RectI"/> elements in the <see cref="ItemsControl.Items"/> collection are
        /// scaled vertically to fit the current <see cref="Polygon"/>. The default is <see
        /// cref="ImageScaling.None"/>.</value>
        /// <remarks>
        /// Setting <b>ScalingY</b> redraws the <see cref="ImageListBox"/>. The value of this
        /// property is ignored if the <see cref="ItemsControl.Items"/> collection contains <see
        /// cref="EntityImage"/> elements.</remarks>

        public ImageScaling ScalingY {
            [DebuggerStepThrough]
            get { return this._scalingY; }
            set {
                this._scalingY = value;
                Redraw();
            }
        }

        #endregion
        #region Insert

        /// <summary>
        /// Inserts the specified <see cref="ImageListBoxItem"/> into the <see
        /// cref="ItemsControl.Items"/> collection at an index position determined by its <see
        /// cref="ImageListBoxItem.SortKey"/>.</summary>
        /// <param name="item">
        /// The <see cref="ImageListBoxItem"/> to insert.</param>
        /// <returns>
        /// The index position within the <see cref="ItemsControl.Items"/> collection at which the
        /// specified <paramref name="item"/> was inserted.</returns>
        /// <remarks><para>
        /// <b>Insert</b> tests the specified <paramref name="item"/> against the existing <see
        /// cref="ItemsControl.Items"/> of type <see cref="ImageListBox"/> to determine its correct
        /// index position, according to their <see cref="ImageListBoxItem.SortKey"/> values.
        /// </para><para>
        /// The algorithm is a stable insertion sort, moving backward through the <see
        /// cref="ItemsControl.Items"/> collection for optimal performance when the added <paramref
        /// name="item"/> typically has the highest <see cref="ImageListBoxItem.SortKey"/>.
        /// </para></remarks>

        public int Insert(ImageListBoxItem item) {

            for (int i = Items.Count - 1; i >= 0; i--) {
                ImageListBoxItem cursor = Items[i] as ImageListBoxItem;
                if (cursor == null || item.SortKey.CompareOrdinal(cursor.SortKey) > 0) {
                    Items.Insert(i + 1, item);
                    return i + 1;
                }
            }

            Items.Insert(0, item);
            return 0;
        }

        #endregion
        #region Redraw()

        /// <overloads>
        /// Redraws all specified <see cref="ImageListBox"/> items.</overloads>
        /// <summary>
        /// Redraws all <see cref="ItemsControl.Items"/> in the <see cref="ImageListBox"/>.
        /// </summary>
        /// <remarks>
        /// <b>Redraw</b> calls <see cref="Redraw(IList)"/> with the current collection of <see
        /// cref="ItemsControl.Items"/> in the <see cref="ImageListBox"/>.</remarks>

        public void Redraw() {
            Redraw(Items);
        }

        #endregion
        #region Redraw(IList)

        /// <summary>
        /// Redraws all elements of the specified <see cref="IList"/> collection that are of type
        /// <see cref="ImageListBoxItem"/>.</summary>
        /// <param name="items">
        /// An <see cref="IList"/> containing the <see cref="ImageListBoxItem"/> items to redraw.
        /// This argument may be a null reference.</param>
        /// <remarks>
        /// <b>Redraw</b> invokes <see cref="UIElement.InvalidateVisual"/> on any specified
        /// <paramref name="items"/> that can be cast to <see cref="ImageListBoxItem"/>. Any other
        /// items are ignored.</remarks>

        public static void Redraw(IList items) {
            if (items == null) return;

            for (int i = 0; i < items.Count; i++) {
                ImageListBoxItem item = items[i] as ImageListBoxItem;
                if (item != null) item.InvalidateVisual();
            }
        }

        #endregion
        #region OnSelectionChanged

        /// <summary>
        /// Raises and handles the <see cref="Selector.SelectionChanged"/> event.</summary>
        /// <param name="args">
        /// A <see cref="SelectionChangedEventArgs"/> object containing event data.</param>
        /// <remarks><para>
        /// <b>OnSelectionChanged</b> raises the <see cref="Selector.SelectionChanged"/> event by
        /// calling the base class implementation of <see cref="ListBox.OnSelectionChanged"/>.
        /// </para><para>
        /// <b>OnSelectionChanged</b> then handles the <see cref="Selector.SelectionChanged"/> event
        /// by calling <see cref="Redraw"/> on both the <see
        /// cref="SelectionChangedEventArgs.RemovedItems"/> and the <see
        /// cref="SelectionChangedEventArgs.AddedItems"/> of the specified <paramref name="args"/>.
        /// </para><note type="implementnotes">
        /// Manually redrawing selected or deselected <see cref="ItemsControl.Items"/> is necessary
        /// because the <see cref="ImageListBox"/> may otherwise not correctly update the selection
        /// marker when the <see cref="Selector.SelectedItem"/> is changed programmatically.
        /// </note></remarks>

        protected override void OnSelectionChanged(SelectionChangedEventArgs args) {
            base.OnSelectionChanged(args);

            // redraw all affected items
            Redraw(args.RemovedItems);
            Redraw(args.AddedItems);
        }

        #endregion
#if VIRTUAL_SCROLLING

        // optional virtual item scrolling
        private ScrollViewer _scrollViewer;
        private bool _scrollChangedAttached;

        #region ScrollViewer

        /// <summary>
        /// Gets the <see cref="System.Windows.Controls.ScrollViewer"/> that contains the <see
        /// cref="ItemsControl.Items"/> of the <see cref="ImageListBox"/>. </summary>
        /// <value>
        /// The <see cref="System.Windows.Controls.ScrollViewer"/> that is nested within the <see
        /// cref="ImageListBox"/> to provide scrolling functionality when a large number of <see
        /// cref="ItemsControl.Items"/> is present.</value>
        /// <remarks><para>
        /// <b>ScrollViewer</b> uses reflection to retrieve the value of the internal property
        /// <b>ScrollHost</b> of the <see cref="ItemsControl"/> class. A non-null value is cached
        /// for repeated access.
        /// </para><note type="caution">
        /// <b>ScrollHost</b> returns a null reference until the containing <see
        /// cref="ItemsControl"/> has been fully rendered.</note></remarks>

        public ScrollViewer ScrollViewer {
            get {
                if (this._scrollViewer == null) {
                    var prop = typeof(ItemsControl).GetProperty("ScrollHost",
                        BindingFlags.Instance | BindingFlags.NonPublic);
                    object scrollHost = prop.GetValue(this, null);
                    this._scrollViewer = scrollHost as ScrollViewer;
                }

                return this._scrollViewer;
            }
        }

        #endregion
        #region InvalidateOnScrolling

        /// <summary>
        /// Ensures that all <see cref="ItemsControl.Items"/> are invalidated whenever the <see
        /// cref="System.Windows.Controls.ScrollViewer.ScrollChanged"/> event of the hosted <see
        /// cref="ScrollViewer"/> occurs.</summary>
        /// <remarks><para>
        /// Call <b>InvalidateOnScrolling</b> to enable virtual item scrolling when the hosted <see
        /// cref="ScrollViewer"/> has been created, e.g. in the <see cref="UIElement.OnRender"/>
        /// method of an <see cref="ImageListBoxItem"/>.
        /// </para><para>
        /// <b>InvalidateOnScrolling</b> does nothing if the hosted <see cref="ScrollViewer"/> is
        /// still a null reference, or if an event handler has already been attached. Repeated calls
        /// are therefore safe.</para></remarks>

        internal void InvalidateOnScrolling() {

            // do nothing if already attached or no scroll viewer yet
            if (this._scrollChangedAttached || ScrollViewer == null)
                return;

            // invalidate all items (nothing else works!)
            ScrollViewer.ScrollChanged += delegate {
                foreach (ImageListBoxItem item in Items)
                    item.InvalidateVisual();
            };

            this._scrollChangedAttached = true;
        }

        #endregion
#endif
    }
}
