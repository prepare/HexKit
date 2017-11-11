using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

using Tektosyne;
using Tektosyne.Collections;
using Tektosyne.Geometry;
using Tektosyne.Windows;

using Hexkit.Global;
using Hexkit.Graphics;
using Hexkit.Scenario;

namespace Hexkit.Editor.Dialog {
    #region Type Aliases

    using NumericUpDown = System.Windows.Forms.NumericUpDown;
    using ImageFilePair = KeyValuePair<String, ImageFile>;

    #endregion

    /// <summary>
    /// Shows a dialog allowing the user to change an <see cref="EntityImage"/>.</summary>
    /// <remarks>
    /// Please refer to the "Change Image" page of the "Editor Dialogs" section in the application
    /// help file for details on this dialog.</remarks>

    public partial class ChangeImage: Window {
        #region ChangeImage(EntityImage)

        /// <summary>
        /// Initializes a new instance of the <see cref="ChangeImage"/> class with the specified
        /// <see cref="EntityImage"/>.</summary>
        /// <param name="image">
        /// The <see cref="EntityImage"/> whose data to change.</param>
        /// <exception cref="PropertyValueException">
        /// The current <see cref="ImageSection"/> contains an empty <see
        /// cref="ImageSection.ImageFiles"/> collection.</exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="image"/> is a null reference.</exception>
        /// <remarks>
        /// The data of the specified <paramref name="image"/> may be changed in the dialog, as
        /// indicated by the value of the <see cref="DataChanged"/> property.</remarks>

        public ChangeImage(EntityImage image) {
            if (image == null)
                ThrowHelper.ThrowArgumentNullException("image");

            // require non-empty ImageFiles for simplicity
            if (MasterSection.Instance.Images.ImageFiles.Count == 0)
                ThrowHelper.ThrowPropertyValueException(
                    "MasterSection.Instance.Images.ImageFiles", Tektosyne.Strings.PropertyEmpty);

            this._image = image;
            InitializeComponent();
            Title += image.Id;

            // initialize frame control buttons
            AddFrameButton.ShowSymbol(Symbols.BoxEmpty);
            RemoveFrameButton.ShowSymbol(Symbols.BoxCrossed);
            MoveLeftButton.ShowSymbol(Symbols.ArrowLeft);
            MoveRightButton.ShowSymbol(Symbols.ArrowRight);

            // set maximum ranges for frame dimensions
            LeftUpDown.Maximum = Int32.MaxValue;
            TopUpDown.Maximum = Int32.MaxValue;
            WidthUpDown.Minimum = 1;
            WidthUpDown.Maximum = Int32.MaxValue;
            HeightUpDown.Minimum = 1;
            HeightUpDown.Maximum = Int32.MaxValue;

            // add image files to combo box
            foreach (string id in MasterSection.Instance.Images.ImageFiles.Keys)
                FileCombo.Items.Add(id);

            // add animation options to combo boxes
            AnimationCombo.ItemsSource = Enum.GetValues(typeof(AnimationMode));
            SequenceCombo.ItemsSource = Enum.GetValues(typeof(AnimationSequence));

            // add scaling options to combo boxes
            var scalingValues = Enum.GetValues(typeof(ImageScaling));
            ScalingXCombo.ItemsSource = scalingValues;
            ScalingYCombo.ItemsSource = scalingValues;

            // set animation & scaling options
            AnimationCombo.SelectedItem = image.Animation;
            SequenceCombo.SelectedItem = image.Sequence;
            ScalingXCombo.SelectedItem = image.ScalingX;
            ScalingYCombo.SelectedItem = image.ScalingY;

            // enable sequence control if animation enabled
            SequenceCombo.IsEnabled = (image.Animation != AnimationMode.None);

            // initialize list box parameters
            FrameList.Polygon = MasterSection.Instance.Areas.MapGrid.Element;
            FrameList.ScalingX = image.ScalingX;
            FrameList.ScalingY = image.ScalingY;

            // add default rectangle if none defined
            if (image.Frames.Count == 0)
                image.Frames.Add(CreateFrame());

            // add frame bounds to list box
            foreach (ImageFrame frame in image.Frames) {
                RectI bounds = frame.Bounds;

                // correct any illegal location values
                int maxPoint = SimpleXml.MaxPointIValue;
                int left = Math.Max(0, Math.Min(maxPoint, bounds.Left));
                int top = Math.Max(0, Math.Min(maxPoint, bounds.Top));

                // correct any illegal size values
                int maxSize = SimpleXml.MaxSizeIValue;
                int width = Math.Max(1, Math.Min(maxSize, bounds.Width));
                int height = Math.Max(1, Math.Min(maxSize, bounds.Height));

                // add frame with corrected bounds to list box
                RectI newBounds = new RectI(left, top, width, height);
                ImageFrame newFrame = new ImageFrame(frame);
                newFrame.Bounds = newBounds;

                FrameList.Items.Add(new ImageListBoxItem(newFrame));

                // add invalid file identifier to combo box
                string fileId = newFrame.Source.Key;
                if (!String.IsNullOrEmpty(fileId) && !FileCombo.Items.Contains(fileId))
                    FileCombo.Items.Add(fileId);
            }

            // disable Remove if only one frame defined
            RemoveFrameButton.IsEnabled = (FrameList.Items.Count > 1);

            // animate dashed outline of frame marker
            DoubleAnimation animation = new DoubleAnimation(10, 0, Duration.Automatic);
            animation.RepeatBehavior = RepeatBehavior.Forever;
            FrameMarker.BeginAnimation(Shape.StrokeDashOffsetProperty, animation);

            // construction completed
            this._initialized = true;
        }

        #endregion
        #region Private Fields

        // construction parameters
        private readonly EntityImage _image;

        // was construction completed?
        private readonly bool _initialized;

        #region _ignoreEvents

        /// <summary>
        /// A value indicating whether control events should be ignored.</summary>
        /// <remarks><para>
        /// The controls of the <see cref="ChangeImage"/> dialog are connected by a web of
        /// interrelated events. While they should be consistent unless the code is buggy, any
        /// programmatic control data change will cause a lot of event-driven changes to other
        /// controls that are either redundant or soon to be overwritten.
        /// </para><para>
        /// To speed up complex interface updates, control event handlers should check the
        /// <b>_ignoreEvents</b> counter and return immediately if it is greater than zero. Increase
        /// this counter before programmatically changing one or more control properties, then
        /// decrease it again when done, or before applying a change that should be propagated.
        /// </para></remarks>

        private uint _ignoreEvents;

        #endregion
        #endregion
        #region DataChanged

        /// <summary>
        /// Gets a value indicating whether any data was changed in the dialog.</summary>
        /// <value>
        /// <c>true</c> if any of the objects supplied to the constructor have been modified;
        /// otherwise, <c>false</c>.</value>
        /// <remarks>
        /// <b>DataChanged</b> returns <c>false</c> if the objects supplied to the constructor were
        /// not modified in any detectable way. However, the original data may have been overwritten
        /// with a copy that is not detectably different, namely if the user clicked <b>OK</b>
        /// without making any changes.</remarks>

        public bool DataChanged { get; private set; }

        #endregion
        #region Private Members
        #region FileBitmap

        /// <summary>
        /// Gets or sets the <see cref="WriteableBitmap"/> shown by the "Source File" <see
        /// cref="ScrollViewer"/>.</summary>
        /// <value>
        /// The <see cref="WriteableBitmap"/> currently shown by the "Source File" viewer, if any;
        /// otherwise, a null reference.</value>

        private WriteableBitmap FileBitmap {
            get { return FileImage.Source as WriteableBitmap; }
            set {
                FileImage.Source = value;
                if (value != null) {
                    FileCanvas.Width = value.Width;
                    FileCanvas.Height = value.Height;
                }
            }
        }

        #endregion
        #region CenterFrame

        /// <summary>
        /// Centers the "Source File" <see cref="ScrollViewer"/> on the specified <see
        /// cref="ImageFrame"/>.</summary>
        /// <param name="frame">
        /// The <see cref="ImageFrame"/> to center on.</param>
        /// <remarks>
        /// <b>CenterFrame</b> scrolls the "Source File" viewer as far as possible so that the
        /// bounds of the specified <paramref name="frame"/> are centered.</remarks>

        private void CenterFrame(ImageFrame frame) {

            // scroll to selected image frame, if possible
            RectI bounds = frame.Bounds;
            double x = bounds.Left + (bounds.Width - FileViewer.ViewportWidth) / 2.0;
            double y = bounds.Top + (bounds.Height - FileViewer.ViewportHeight) / 2.0;

            FileViewer.ScrollToHorizontalOffset(x);
            FileViewer.ScrollToVerticalOffset(y);
        }

        #endregion
        #region CreateFrame

        /// <summary>
        /// Creates a new <see cref="ImageFrame"/> with a valid source file.</summary>
        /// <returns>
        /// A new <see cref="ImageFrame"/> whose <see cref="ImageFrame.Source"/> property references
        /// the first item in the "Source File" <see cref="ComboBox"/>.</returns>

        private ImageFrame CreateFrame() {
            ImageFrame frame = new ImageFrame();

            // get first available image file, if any
            if (FileCombo.Items.Count > 0) {
                string fileId = FileCombo.Items[0] as String;

                ImageFile file = null;
                if (!String.IsNullOrEmpty(fileId))
                    MasterSection.Instance.Images.ImageFiles.TryGetValue(fileId, out file);

                frame.Source = new ImageFilePair(fileId, file);
            }

            return frame;
        }

        #endregion
        #region MarkFrame

        /// <summary>
        /// Marks the specified <see cref="ImageFrame"/> in the "Source File" <see
        /// cref="ScrollViewer"/>.</summary>
        /// <param name="frame">
        /// The <see cref="ImageFrame"/> to mark.</param>
        /// <remarks>
        /// <b>MarkFrame</b> surrounds the bounds of the specified <paramref name="frame"/> with a
        /// red marker rectangle in the "Source File" viewer.</remarks>

        private void MarkFrame(ImageFrame frame) {

            // surround selected frame with marker rectangle
            RectI bounds = frame.Bounds;
            Canvas.SetLeft(FrameMarker, bounds.Left - 1);
            Canvas.SetTop(FrameMarker, bounds.Top - 1);
            FrameMarker.Width = bounds.Width + 2;
            FrameMarker.Height = bounds.Height + 2;
        }

        #endregion
        #endregion
        #region Event Handlers
        #region HelpExecuted

        /// <summary>
        /// Handles the <see cref="CommandBinding.Executed"/> event for the <see
        /// cref="ApplicationCommands.Help"/> command.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// An <see cref="ExecutedRoutedEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>HelpExecuted</b> opens the application help file on the help page for the <see
        /// cref="ChangeImage"/> dialog.</remarks>

        private void HelpExecuted(object sender, ExecutedRoutedEventArgs args) {
            args.Handled = true;
            ApplicationUtility.ShowHelp("DlgChangeImage.html");
        }

        #endregion
        #region OnAccept

        /// <summary>
        /// Handles the <see cref="ButtonBase.Click"/> event for the "OK" <see cref="Button"/>.
        /// </summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="RoutedEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnAccept</b> sets the <see cref="Window.DialogResult"/> property to <c>true</c>. This
        /// also triggers the <see cref="Window.Closing"/> event.</remarks>

        private void OnAccept(object sender, RoutedEventArgs args) {
            args.Handled = true;
            DialogResult = true;
        }

        #endregion
        #region OnAnimationSelected

        /// <summary>
        /// Handles the <see cref="Selector.SelectionChanged"/> event for the "Animation" <see
        /// cref="ComboBox"/>.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="SelectionChangedEventArgs"/> object containing event data.</param>
        /// <remarks><para>
        /// <b>OnAnimationSelected</b> disables the "Sequence" combo box if the new <see
        /// cref="AnimationMode"/> value equals <see cref="AnimationMode.None"/>.
        /// </para><para>
        /// <b>OnAnimationSelected</b> also sets the <see cref="DataChanged"/> flag if the new <see
        /// cref="AnimationMode"/> value is different from the current <see
        /// cref="EntityImage.Animation"/> value.</para></remarks>

        private void OnAnimationSelected(object sender, SelectionChangedEventArgs args) {
            args.Handled = true;
            if (!this._initialized) return;

            // get new animation mode, if any
            AnimationMode animation = this._image.Animation;
            if (AnimationCombo.SelectedItem != null)
                animation = (AnimationMode) AnimationCombo.SelectedItem;

            // enable sequence control if animation enabled
            SequenceCombo.IsEnabled = (animation != AnimationMode.None);

            // broadcast data change, if any
            if (animation != this._image.Animation)
                DataChanged = true;
        }

        #endregion
        #region OnBoundsChanged

        /// <summary>
        /// Handles the <see cref="NumericUpDown.ValueChanged"/> event for the "Left", "Top",
        /// "Width", and "Height" <see cref="NumericUpDown"/> controls.</summary>
        /// <param name="sender">
        /// The <see cref="NumericUpDown"/> control sending the event.</param>
        /// <param name="args">
        /// An <see cref="EventArgs"/> object containing event data.</param>
        /// <remarks><para>
        /// <b>OnBoundsChanged</b> checks if the bounds specified by the "Selected Frame" controls
        /// are actually different from the bounds specified by the selected item in the "Image
        /// Frames" list box. (This may not be case for programmatic changes.)
        /// </para><para>
        /// If so, <b>OnBoundsChanged</b> updates that item's data and marker in the "Source File"
        /// viewer to reflect the new bounds, and sets the <see cref="DataChanged"/> flag.
        /// </para></remarks>

        private void OnBoundsChanged(object sender, EventArgs args) {
            if (this._ignoreEvents > 0) return;

            // retrieve selected image frame, if any
            int index = FrameList.SelectedIndex;
            if (index < 0) return;
            var item = (ImageListBoxItem) FrameList.Items[index];
            var frame = (ImageFrame) item.Content;

            // retrieve new bounds from numeric controls
            RectI newBounds = new RectI(
                (int) LeftUpDown.Value, (int) TopUpDown.Value,
                (int) WidthUpDown.Value, (int) HeightUpDown.Value);

            // update controls only if bounds have changed
            if (frame.Bounds != newBounds) {
                frame.Bounds = newBounds;

                // broadcast data changes
                MarkFrame(frame);
                item.InvalidateVisual();
                if (this._initialized) DataChanged = true;
            }
        }

        #endregion
        #region OnChangeConnections

        /// <summary>
        /// Handles the <see cref="ButtonBase.Click"/> event for the "Change Connections" <see
        /// cref="Button"/>.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="RoutedEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnChangeConnections</b> displays a <see cref="ChangeConnections"/> dialog for the
        /// selected item in the "Image Frames" list box.</remarks>

        private void OnChangeConnections(object sender, RoutedEventArgs args) {
            args.Handled = true;

            // retrieve selected image frame, if any
            int index = FrameList.SelectedIndex;
            if (index < 0) return;
            var item = (ImageListBoxItem) FrameList.Items[index];
            var frame = (ImageFrame) item.Content;

            // show Change Connections dialog
            string info = String.Format(ApplicationInfo.Culture,
                "{0}/{1}", index + 1, FrameList.Items.Count);

            var dialog = new ChangeConnections(frame, FileBitmap, info,
                FrameList.ScalingX, FrameList.ScalingY) { Owner = this };

            dialog.ShowDialog();

            // broadcast data changes by dialog
            if (dialog.DataChanged) DataChanged = true;
        }

        #endregion
        #region OnClosing

        /// <summary>
        /// Raises and handles the <see cref="Window.Closing"/> event.</summary>
        /// <param name="args">
        /// A <see cref="CancelEventArgs"/> object containing event data.</param>
        /// <remarks><para>
        /// <b>OnClosing</b> raises the <see cref="Window.Closing"/> event by calling the base class
        /// implementation of <see cref="Window.OnClosing"/> with the specified <paramref
        /// name="args"/>.
        /// </para><para>
        /// If the event was not requested to <see cref="CancelEventArgs.Cancel"/>, <b>OnClosing</b>
        /// handles the <see cref="Window.Closing"/> event by clearing the <see cref="DataChanged"/>
        /// flag if the <see cref="Window.DialogResult"/> is not <c>true</c>, indicating that the
        /// user cancelled the dialog and wants to discard all changes.
        /// </para><para>
        /// Otherwise, <b>OnClosing</b> reads the control contents of this dialog into the <see
        /// cref="EntityImage"/> supplied to the constructor.</para></remarks>

        protected override void OnClosing(CancelEventArgs args) {
            base.OnClosing(args);
            if (args.Cancel) return;

            // user cancelled dialog, ignore changes
            if (DialogResult != true) {
                DataChanged = false;
                return;
            }

            // read image frames into collection
            this._image.Frames.Clear();
            foreach (ImageListBoxItem item in FrameList.Items)
                this._image.Frames.Add((ImageFrame) item.Content);

            // read animation options
            if (AnimationCombo.SelectedItem != null)
                this._image.Animation = (AnimationMode) AnimationCombo.SelectedItem;
            if (SequenceCombo.SelectedItem != null)
                this._image.Sequence = (AnimationSequence) SequenceCombo.SelectedItem;

            // read scaling options
            if (ScalingXCombo.SelectedItem != null)
                this._image.ScalingX = (ImageScaling) ScalingXCombo.SelectedItem;
            if (ScalingYCombo.SelectedItem != null)
                this._image.ScalingY = (ImageScaling) ScalingYCombo.SelectedItem;
        }

        #endregion
        #region OnContentRendered

        /// <summary>
        /// Raises and handles the <see cref="Window.ContentRendered"/> event.</summary>
        /// <param name="args">
        /// An <see cref="EventArgs"/> object containing event data.</param>
        /// <remarks><para>
        /// <b>OnContentRendered</b> raises the <see cref="Window.ContentRendered"/> event by
        /// calling the base class implementation of <see cref="Window.OnContentRendered"/>.
        /// </para><para>
        /// <b>OnContentRendered</b> then handles the <see cref="Window.ContentRendered"/> event by
        /// selecting the first item in the "Image Frames" list box, if any.</para></remarks>

        protected override void OnContentRendered(EventArgs args) {
            base.OnContentRendered(args);

            // select first image frame, if any
            if (FrameList.Items.Count > 0)
                FrameList.SelectedIndex = 0;
        }

        #endregion
        #region OnFileMouse

        /// <summary>
        /// Handles the <see cref="UIElement.MouseDown"/> event for the "Source File" <see
        /// cref="Image"/> control.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="MouseButtonEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnFileMouse</b> changes the location controls of the "Selected Frame" box to the
        /// coordinates of a left click, and the size controls to the distance between the
        /// coordinates of a right click and the current location.</remarks>

        private void OnFileMouse(object sender, MouseButtonEventArgs args) {
            args.Handled = true;

            // adjust click location by scroll position
            Point cursor = args.GetPosition(FileImage);
            switch (args.ChangedButton) {

                case MouseButton.Left:
                    // left button changes frame location
                    LeftUpDown.Value = (decimal) cursor.X;
                    TopUpDown.Value = (decimal) cursor.Y;
                    break;

                case MouseButton.Right:
                    // right button changes frame size (if valid)
                    WidthUpDown.Value = Math.Max(1m, (decimal) cursor.X - LeftUpDown.Value + 1m);
                    HeightUpDown.Value = Math.Max(1m, (decimal) cursor.Y - TopUpDown.Value + 1m);
                    break;
            }
        }

        #endregion
        #region OnFileSelected

        /// <summary>
        /// Handles the <see cref="Selector.SelectionChanged"/> event for the "Source File" <see
        /// cref="ComboBox"/>.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="SelectionChangedEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnFileSelected</b> shows the contents of the new file, updates the selected <see
        /// cref="ImageFrame"/>, and sets the <see cref="DataChanged"/> flag if the new file is
        /// different from the one specified by the selected <see cref="ImageFrame"/>.</remarks>

        private void OnFileSelected(object sender, SelectionChangedEventArgs args) {
            args.Handled = true;

            // clear old source file
            FileBitmap = null;

            string fileId = (string) FileCombo.SelectedItem;
            ImageFile file = null;

            // display new source file, if any
            if (fileId.Length != 0) {
                MasterSection.Instance.Images.ImageFiles.TryGetValue(fileId, out file);
                if (file != null) FileBitmap = file.Bitmap;
            }

            // retrieve selected image frame, if any
            var item = FrameList.SelectedItem as ImageListBoxItem;
            if (item == null) return;
            var frame = (ImageFrame) item.Content;

            // broadcast source file change, if any
            if (this._initialized && frame.Source.Key != fileId) {
                frame.Source = new ImageFilePair(fileId, file);
                CenterFrame(frame);
                item.InvalidateVisual();
                DataChanged = true;
            }
        }

        #endregion
        #region OnFrameAdd

        /// <summary>
        /// Handles the <see cref="ButtonBase.Click"/> event for the "Add New Frame" <see
        /// cref="Button"/>.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="RoutedEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnFrameAdd</b> adds a new frame to the "Image Frames" list box and sets the <see
        /// cref="DataChanged"/> flag. The new frame copies the properties of the selected frame, if
        /// any; otherwise, it is created with default properties.</remarks>

        private void OnFrameAdd(object sender, RoutedEventArgs args) {
            args.Handled = true;

            // create new frame based on selected frame, if any
            var item = FrameList.SelectedItem as ImageListBoxItem;
            ImageFrame frame = (item == null ? CreateFrame() :
                new ImageFrame((ImageFrame) item.Content));

            // add and select default frame bounds
            item = new ImageListBoxItem(frame);
            int index = FrameList.Items.Add(item);
            FrameList.SelectAndShow(index);

            // enable Remove if two or more items present
            RemoveFrameButton.IsEnabled = (index > 0);

            // frame list has changed
            DataChanged = true;
        }

        #endregion
        #region OnFrameLeft

        /// <summary>
        /// Handles the <see cref="ButtonBase.Click"/> event for the "Move Frame Left" <see
        /// cref="Button"/>.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="RoutedEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnFrameLeft</b> swaps the selected item in the "Image Frames" list box with its left
        /// neighbor and sets the <see cref="DataChanged"/> flag.</remarks>

        private void OnFrameLeft(object sender, RoutedEventArgs args) {
            args.Handled = true;

            // retrieve selected item
            object item = FrameList.SelectedItem;
            if (FrameList.Items.Count < 2 || item == null)
                return;

            // move item left and re-select it
            int index = CollectionsUtility.MoveItemUntyped(FrameList.Items, item, -1);
            FrameList.SelectAndShow(index);

            // frame list has changed
            DataChanged = true;
        }

        #endregion
        #region OnFrameRemove

        /// <summary>
        /// Handles the <see cref="ButtonBase.Click"/> event for the "Remove Frame" <see
        /// cref="Button"/>.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="RoutedEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnFrameRemove</b> removes the selected item from the "Image Frames" list box and sets
        /// the <see cref="DataChanged"/> flag.</remarks>

        private void OnFrameRemove(object sender, RoutedEventArgs args) {
            args.Handled = true;

            // cannot remove last item in list box
            if (FrameList.Items.Count < 2) return;

            // remove selected item
            int index = FrameList.SelectedIndex;
            if (index < 0) return;
            FrameList.Items.RemoveAt(index);

            // select next or last item in list box
            FrameList.SelectAndShow(Math.Min(index, FrameList.Items.Count - 1));

            // disable Remove if only one item left
            RemoveFrameButton.IsEnabled = (FrameList.Items.Count > 1);

            // frame list has changed
            DataChanged = true;
        }

        #endregion
        #region OnFrameRight

        /// <summary>
        /// Handles the <see cref="ButtonBase.Click"/> event for the "Move Frame Right" <see
        /// cref="Button"/>.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="RoutedEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnFrameRight</b> swaps the selected item in the "Image Frames" list box with its
        /// right neighbor and sets the <see cref="DataChanged"/> flag.</remarks>

        private void OnFrameRight(object sender, RoutedEventArgs args) {
            args.Handled = true;

            // retrieve selected item
            object item = FrameList.SelectedItem;
            if (FrameList.Items.Count < 2 || item == null)
                return;

            // move item right and re-select it
            int index = CollectionsUtility.MoveItemUntyped(FrameList.Items, item, +1);
            FrameList.SelectAndShow(index);

            // frame list has changed
            DataChanged = true;
        }

        #endregion
        #region OnFrameSelected

        /// <summary>
        /// Handles the <see cref="Selector.SelectionChanged"/> event for the "Image Frames" <see
        /// cref="ImageListBox"/>.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="SelectionChangedEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnFrameSelected</b> updates the "Source File" and "Selected Frame" controls to
        /// reflect the selected item in the "Image Frames" list box.</remarks>

        private void OnFrameSelected(object sender, SelectionChangedEventArgs args) {
            args.Handled = true;
            if (this._ignoreEvents > 0) return;

            // retrieve selected image frame, if any
            var item = FrameList.SelectedItem as ImageListBoxItem;
            if (item == null) return;
            var frame = (ImageFrame) item.Content;

            this._ignoreEvents++;

            // update numeric controls with frame bounds
            RectI bounds = frame.Bounds;
            LeftUpDown.Value = bounds.Left;
            TopUpDown.Value = bounds.Top;
            WidthUpDown.Value = bounds.Width;
            HeightUpDown.Value = bounds.Height;

            this._ignoreEvents--;

            // select associated image file, if any
            FileCombo.SelectedItem = frame.Source.Key;
            if (FileCombo.SelectedIndex < 0)
                FileCombo.SelectedIndex = 0;

            // center & mark selected frame
            CenterFrame(frame);
            MarkFrame(frame);
        }

        #endregion
        #region OnScalingSelected

        /// <summary>
        /// Handles the <see cref="Selector.SelectionChanged"/> event for the "X-Scaling" and
        /// "Y-Scaling" <see cref="ComboBox"/> controls.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="SelectionChangedEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnScalingSelected</b> updates the "Image Frames" list box with the new <see
        /// cref="ImageScaling"/> value for the selected dimension, and sets the <see
        /// cref="DataChanged"/> flag if it is different from the current <see
        /// cref="EntityImage.ScalingX"/> or <see cref="EntityImage.ScalingY"/> value.</remarks>

        private void OnScalingSelected(object sender, SelectionChangedEventArgs args) {
            args.Handled = true;
            if (!this._initialized) return;

            // retrieve current scaling option
            ComboBox combo = (ComboBox) sender;
            bool isScalingX = (combo == ScalingXCombo);
            ImageScaling oldScaling = (isScalingX ? this._image.ScalingX : this._image.ScalingY);

            // get new scaling option, if any
            ImageScaling scaling = oldScaling;
            if (combo.SelectedItem != null)
                scaling = (ImageScaling) combo.SelectedItem;

            // update list box with new option
            if (isScalingX)
                FrameList.ScalingX = scaling;
            else
                FrameList.ScalingY = scaling;

            // broadcast data changes
            if (scaling != oldScaling)
                DataChanged = true;
        }

        #endregion
        #region OnSequenceSelected

        /// <summary>
        /// Handles the <see cref="Selector.SelectionChanged"/> event for the "Sequence" <see
        /// cref="ComboBox"/>.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="SelectionChangedEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnSequenceSelected</b> sets the <see cref="DataChanged"/> flag if the new <see
        /// cref="AnimationSequence"/> value is different from the current <see
        /// cref="EntityImage.Sequence"/> value.</remarks>

        private void OnSequenceSelected(object sender, SelectionChangedEventArgs args) {
            args.Handled = true;
            if (!this._initialized) return;

            // get new animation sequence, if any
            AnimationSequence sequence = this._image.Sequence;
            if (SequenceCombo.SelectedItem != null)
                sequence = (AnimationSequence) SequenceCombo.SelectedItem;

            // broadcast data change, if any
            if (sequence != this._image.Sequence)
                DataChanged = true;
        }

        #endregion
        #endregion
    }
}
