using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;

using Tektosyne.Geometry;
using Hexkit.Global;
using Hexkit.Scenario;

namespace Hexkit.Editor.Dialog {

    using NumericUpDown = System.Windows.Forms.NumericUpDown;

    /// <summary>
    /// Shows a dialog allowing the user to change the size and structure of the <see
    /// cref="AreaSection.MapGrid"/>.</summary>
    /// <remarks>
    /// Please refer to the "Change Map Geometry" page of the "Editor Dialogs" section in the
    /// application help file for details on this dialog.</remarks>

    public partial class ChangeGeometry: Window {
        #region ChangeGeometry()

        /// <summary>
        /// Initializes a new instance of the <see cref="ChangeGeometry"/> class.</summary>

        public ChangeGeometry() {
            InitializeComponent();

            // default to current map grid
            this._mapGrid = (PolygonGrid) MasterSection.Instance.Areas.MapGrid.Clone();
            this._originalMapSize = MapGrid.Size;

            // prevent control input events
            this._ignoreEvents = true;

            // show old and new map size
            OldWidthBox.Text = this._originalMapSize.Width.ToString("N0", ApplicationInfo.Culture);
            OldHeightBox.Text = this._originalMapSize.Height.ToString("N0", ApplicationInfo.Culture);
            ShowMapSize(this._originalMapSize);

            // set maximum range for polygon side length
            LengthUpDown.Minimum = (decimal) AreaSection.MinPolygonLength;
            LengthUpDown.Maximum = (decimal) AreaSection.MaxPolygonLength;

            // add image sizes to combo box
            AddImageSizes();

            // update "Structure" page to reflect map grid
            UpdateStructure();

            // allow control input events
            this._ignoreEvents = false;
        }

        #endregion
        #region Private Fields

        // ignore control input events?
        private bool _ignoreEvents;

        // original map size
        private readonly SizeI _originalMapSize;

        // property backers
        private readonly PolygonGrid _mapGrid;

        #endregion
        #region DataChanged

        /// <summary>
        /// Gets a value indicating whether any structure data was changed in the dialog.</summary>
        /// <value>
        /// <c>true</c> if the <see cref="MapGrid"/> contains any modified data and the user did not
        /// cancel the dialog; otherwise, <c>false</c>.</value>
        /// <remarks><para>
        /// <b>DataChanged</b> may return <c>true</c> even if the <see cref="MapGrid"/> contains
        /// data that is identical with the current <see cref="AreaSection.MapGrid"/>, namely if the
        /// user made and then reset a change before clicking <b>OK</b>.
        /// </para><para>
        /// <b>DataChanged</b> does not reflect changes to the map bounds. Clients should examine
        /// <see cref="EdgeMoved"/> and the four <b>Move</b> properties to determine whether such
        /// changes were made.</para></remarks>

        public bool DataChanged { get; private set; }

        #endregion
        #region EdgeMoved

        /// <summary>
        /// Gets a value indicating whether any map edges were moved in the dialog.</summary>
        /// <value>
        /// <c>true</c> if the user has moved at least one map edge and did not cancel the dialog;
        /// otherwise, <c>false</c>.</value>
        /// <remarks>
        /// <b>EdgeMoved</b> returns <c>true</c> if at least one of the <see cref="MoveLeft"/>, <see
        /// cref="MoveRight"/>, <see cref="MoveTop"/>, and <see cref="MoveBottom"/> properties does
        /// not equal zero.</remarks>

        public bool EdgeMoved {
            get { return (MoveLeft != 0 || MoveRight != 0 || MoveTop != 0 || MoveBottom != 0); }
        }

        #endregion
        #region MapGrid

        /// <summary>
        /// Gets the <see cref="PolygonGrid"/> that describes the changed game map.</summary>
        /// <value>
        /// The read-only <see cref="PolygonGrid"/> that describes the geometric structure of the
        /// game map, incorporating any changes that the user has made in the dialog.</value>
        /// <remarks>
        /// <b>MapGrid</b> is initialized to a copy of the current <see
        /// cref="AreaSection.MapGrid"/>, and then modified to reflect any changes that the user is
        /// making to the hosted controls.</remarks>

        public PolygonGrid MapGrid {
            [DebuggerStepThrough]
            get { return this._mapGrid.AsReadOnly(); }
        }

        #endregion
        #region MoveBottom

        /// <summary>
        /// Gets the distance by which the bottom map edge was moved.</summary>
        /// <value><para>
        /// A positive value to add the specified number of rows to the bottom map edge.
        /// </para><para>-or-</para><para>
        /// A negative value to delete the specified number of rows from the bottom map edge.
        /// </para><para>-or-</para><para>
        /// Zero to leave the bottom map edge unchanged.</para></value>

        public int MoveBottom { get; private set; }

        #endregion
        #region MoveLeft

        /// <summary>
        /// Gets the distance by which the left map edge was moved.</summary>
        /// <value><para>
        /// A positive value to delete the specified number of columns from the left map edge.
        /// </para><para>-or-</para><para>
        /// A negative value to add the specified number of columns to the left map edge.
        /// </para><para>-or-</para><para>
        /// Zero to leave the left map edge unchanged.</para></value>

        public int MoveLeft { get; private set; }

        #endregion
        #region MoveRight

        /// <summary>
        /// Gets the distance by which the right map edge was moved.</summary>
        /// <value><para>
        /// A positive value to add the specified number of columns to the right map edge.
        /// </para><para>-or-</para><para>
        /// A negative value to delete the specified number of columns from the right map edge.
        /// </para><para>-or-</para><para>
        /// Zero to leave the right map edge unchanged.</para></value>

        public int MoveRight { get; private set; }

        #endregion
        #region MoveTop

        /// <summary>
        /// Gets the distance by which the top map edge was moved.</summary>
        /// <value><para>
        /// A positive value to delete the specified number of rows from the top map edge.
        /// </para><para>-or-</para><para>
        /// A negative value to add the specified number of rows to the top map edge.
        /// </para><para>-or-</para><para>
        /// Zero to leave the top map edge unchanged.</para></value>

        public int MoveTop { get; private set; }

        #endregion
        #region Private Methods
        #region AddImageSizes

        /// <summary>
        /// Adds all defined <see cref="ImageFrame"/> sizes to the "Image Frame Sizes" <see
        /// cref="ComboBox"/> on the <see cref="StructureTab"/> page.</summary>
        /// <remarks><para>
        /// <b>AddImageSizes</b> iterates through all <see cref="EntityImage.Frames"/> of every <see
        /// cref="EntityImage"/> in the current <see cref="ImageSection"/>, and adds all unique <see
        /// cref="ImageFrame.Bounds"/> sizes to the "Image Frame Sizes" combo box.
        /// </para><para>
        /// The combo box and the "Circumscribe" button are disabled if the <see
        /// cref="ImageSection"/> does not define any <see cref="ImageFrame"/> objects.
        /// </para></remarks>

        private void AddImageSizes() {

            // extract all distinct sizes of image frames
            List<SizeI> sizes = new List<SizeI>();
            foreach (EntityImage image in MasterSection.Instance.Images.Collection.Values)
                if (image != null) {
                    for (int i = 0; i < image.Frames.Count; i++) {
                        SizeI size = image.Frames[i].Bounds.Size;
                        if (!sizes.Contains(size)) sizes.Add(size);
                    }
                }

            if (sizes.Count == 0) {
                // disable controls if no image sizes defined
                CircumscribeButton.IsEnabled = false;
                ImageSizeCombo.IsEnabled = false;
                ImageSizeCombo.Items.Add(Global.Strings.LabelNone);
            } else {
                // sort image sizes by width, then by height
                sizes.Sort((x, y) => {
                    int delta = x.Width - y.Width;
                    return (delta != 0 ? delta : x.Height - y.Height);
                });

                // add image sizes to combo box
                foreach (SizeI size in sizes) {
                    ImageSizeItem item = new ImageSizeItem(size);
                    ImageSizeCombo.Items.Add(item);
                }
            }

            Debug.Assert(ImageSizeCombo.Items.Count > 0);
            ImageSizeCombo.SelectedIndex = 0;
        }

        #endregion
        #region ShowMapSize

        /// <summary>
        /// Shows the specified new map size and updates the ranges of all <see
        /// cref="NumericUpDown"/> controls on the <see cref="SizeTab"/> page.</summary>
        /// <param name="newSize">
        /// The dimensions to show in the "New Size" <see cref="GroupBox"/>.</param>

        private void ShowMapSize(SizeI newSize) {

            // show new map size
            NewWidthBox.Text = newSize.Width.ToString("N0", ApplicationInfo.Culture);
            NewHeightBox.Text = newSize.Height.ToString("N0", ApplicationInfo.Culture);

            // original and maximum map size
            SizeI oldSize = this._originalMapSize;
            int maxSize = SimpleXml.MaxSizeIValue;

            // compute new edge offsets
            int leftEdge = -oldSize.Width + MoveLeft;
            int rightEdge = oldSize.Width + MoveRight;
            int topEdge = -oldSize.Height + MoveTop;
            int bottomEdge = oldSize.Height + MoveBottom;

            // update range for left edge
            LeftUpDown.Minimum = rightEdge - maxSize;
            LeftUpDown.Maximum = rightEdge - 1;

            // update range for right edge
            RightUpDown.Minimum = leftEdge + 1;
            RightUpDown.Maximum = leftEdge + maxSize;

            // update range for top edge
            TopUpDown.Minimum = bottomEdge - maxSize;
            TopUpDown.Maximum = bottomEdge - 1;

            // update range for bottom edge
            BottomUpDown.Minimum = topEdge + 1;
            BottomUpDown.Maximum = topEdge + maxSize;
        }

        #endregion
        #region UpdateMapSize

        /// <summary>
        /// Updates the <see cref="PolygonGrid.Size"/> of the <see cref="MapGrid"/>.</summary>
        /// <returns>
        /// The new <see cref="PolygonGrid.Size"/> of the <see cref="MapGrid"/>.</returns>
        /// <remarks>
        /// <b>UpdateMapSize</b> sets the size of the current <see cref="MapGrid"/> to the size of
        /// the original <see cref="AreaSection.MapGrid"/>, modified by the current edge movements
        /// on the <see cref="SizeTab"/> page.</remarks>

        private SizeI UpdateMapSize() {

            this._mapGrid.Size = new SizeI(
                this._originalMapSize.Width - MoveLeft + MoveRight,
                this._originalMapSize.Height - MoveTop + MoveBottom);

            return this._mapGrid.Size;
        }

        #endregion
        #region UpdateStructure

        /// <summary>
        /// Updates all controls on the <see cref="StructureTab"/> to reflect the current <see
        /// cref="MapGrid"/>.</summary>

        private void UpdateStructure() {

            // prevent control input events
            this._ignoreEvents = true;

            RegularPolygon element = MapGrid.Element;
            LengthUpDown.Value = (decimal) element.Length;
            VertexNeighborsToggle.IsChecked = element.VertexNeighbors;

            bool isSquare = (element.Sides == 4);
            VertexNeighborsToggle.IsEnabled = isSquare;

            bool onEdge = (element.Orientation == PolygonOrientation.OnEdge);
            RowLeftToggle.IsEnabled = !onEdge;
            RowRightToggle.IsEnabled = !onEdge;

            if (isSquare) {
                if (onEdge)
                    SquareOnEdgeToggle.IsChecked = true;
                else
                    SquareOnVertexToggle.IsChecked = true;

                ShiftNoneToggle.IsEnabled = onEdge;
                ColumnUpToggle.IsEnabled = !onEdge;
                ColumnDownToggle.IsEnabled = !onEdge;
            } else {
                if (onEdge)
                    HexagonOnEdgeToggle.IsChecked = true;
                else
                    HexagonOnVertexToggle.IsChecked = true;

                ShiftNoneToggle.IsEnabled = false;
                ColumnUpToggle.IsEnabled = onEdge;
                ColumnDownToggle.IsEnabled = onEdge;
            }

            switch (MapGrid.GridShift) {

                case PolygonGridShift.None:
                    ShiftNoneToggle.IsChecked = true;
                    break;

                case PolygonGridShift.ColumnUp:
                    ColumnUpToggle.IsChecked = true;
                    break;

                case PolygonGridShift.ColumnDown:
                    ColumnDownToggle.IsChecked = true;
                    break;

                case PolygonGridShift.RowLeft:
                    RowLeftToggle.IsChecked = true;
                    break;

                case PolygonGridShift.RowRight:
                    RowRightToggle.IsChecked = true;
                    break;
            }

            // allow control input events
            this._ignoreEvents = false;
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
        /// <b>HelpExecuted</b> opens the application help file on the help page for the current tab
        /// page of the <see cref="ChangeGeometry"/> dialog.</remarks>

        private void HelpExecuted(object sender, ExecutedRoutedEventArgs args) {
            args.Handled = true;

            // default to dialog help page
            string helpPage = "DlgChangeGeometry.html";

            // show help for specific tab page
            if (SizeTab.IsSelected)
                helpPage = "DlgChangeGeometrySize.html";
            else if (StructureTab.IsSelected)
                helpPage = "DlgChangeGeometryStructure.html";

            ApplicationUtility.ShowHelp(helpPage);
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
        /// flag and resetting all <b>Move</b> properties to zero if the <see
        /// cref="Window.DialogResult"/> is not <c>true</c>, indicating that the user cancelled the
        /// dialog and wants to discard all changes.
        /// </para><para>
        /// Otherwise, <b>OnClosing</b> checks whether the current values of the <b>Move</b>
        /// properties would result in a map size that is less than one or greater than <see
        /// cref="SimpleXml.MaxSizeIValue"/> in either dimension, and cancels the event if so.
        /// </para></remarks>

        protected override void OnClosing(CancelEventArgs args) {
            base.OnClosing(args);
            if (args.Cancel) return;

            // user cancelled dialog, ignore changes
            if (DialogResult != true) {
                DataChanged = false;
                MoveLeft = MoveRight = MoveTop = MoveBottom = 0;
                return;
            }

            // check against minimum and maximum size
            SizeI mapSize = MapGrid.Size;
            if (mapSize.Width < 1 || mapSize.Width > SimpleXml.MaxSizeIValue ||
                mapSize.Height < 1 || mapSize.Height > SimpleXml.MaxSizeIValue) {

                string message = String.Format(ApplicationInfo.Culture,
                    Global.Strings.DialogMapBoundsInvalid, SimpleXml.MaxSizeIValue);

                MessageBox.Show(this, message, Global.Strings.TitleMapBoundsInvalid,
                    MessageBoxButton.OK, MessageBoxImage.Information);

                args.Cancel = true;
            }
        }

        #endregion
        #region OnBoundsChanged

        /// <summary>
        /// Handles the <see cref="NumericUpDown.ValueChanged"/> event for the "Left", "Top",
        /// "Right", and "Bottom" <see cref="NumericUpDown"/> controls on the <see cref="SizeTab"/>
        /// page.</summary>
        /// <param name="sender">
        /// The <see cref="NumericUpDown"/> control sending the event.</param>
        /// <param name="args">
        /// An <see cref="EventArgs"/> object containing event data.</param>
        /// <remarks><para>
        /// <b>OnBoundsChanged</b> sets the <see cref="MoveLeft"/>, <see cref="MoveTop"/>, <see
        /// cref="MoveRight"/>, and <see cref="MoveBottom"/> properties to the current values of the
        /// corresponding <see cref="NumericUpDown"/> controls.
        /// </para><para>
        /// <b>OnBoundsChanged</b> then calls <see cref="UpdateMapSize"/> and <see
        /// cref="ShowMapSize"/> to compute and show the resulting new map size, and to update the
        /// ranges of all <see cref="NumericUpDown"/> controls.</para></remarks>

        private void OnBoundsChanged(object sender, EventArgs args) {

            // retrieve edge modifiers from numeric controls
            MoveLeft = (int) LeftUpDown.Value;
            MoveTop = (int) TopUpDown.Value;
            MoveRight = (int) RightUpDown.Value;
            MoveBottom = (int) BottomUpDown.Value;

            // compute & show new map size
            SizeI newSize = UpdateMapSize();
            ShowMapSize(newSize);
        }

        #endregion
        #region OnCircumscribe

        /// <summary>
        /// Handles the <see cref="ButtonBase.Click"/> event for the "Circumscribe" <see
        /// cref="Button"/> on the <see cref="StructureTab"/> page.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="RoutedEventArgs"/> object containing event data.</param>
        /// <remarks><para>
        /// <b>OnCircumscribe</b> sets the "Side Length" control to a value that circumscribes <see
        /// cref="PolygonGrid.Element"/> of the <see cref="MapGrid"/> around the selected item, if
        /// any, in the "Image Frame Sizes" combo box.
        /// </para><para>
        /// Changing the "Side Length" control triggers <see cref="OnLengthChanged"/> which resizes
        /// the <see cref="PolygonGrid.Element"/> accordingly and sets the <see cref="DataChanged"/>
        /// flag.</para></remarks>

        private void OnCircumscribe(object sender, RoutedEventArgs args) {
            args.Handled = true;
            if (this._ignoreEvents) return;

            // get selected image size, if any
            ImageSizeItem item = ImageSizeCombo.SelectedItem as ImageSizeItem;
            if (item == null) return;

            // circumscribe polygon around image size
            RegularPolygon element = MapGrid.Element;
            var circumscribed = element.Circumscribe(item.Value.Width, item.Value.Height);

            // restrict side length to legal range
            double length = circumscribed.Length;
            if (length < AreaSection.MinPolygonLength) length = AreaSection.MinPolygonLength;
            if (length > AreaSection.MaxPolygonLength) length = AreaSection.MaxPolygonLength;

            // show new side length if changed (triggers OnLengthChanged)
            if (length != element.Length)
                LengthUpDown.Value = (decimal) length;
        }

        #endregion
        #region OnLengthChanged

        /// <summary>
        /// Handles the <see cref="NumericUpDown.ValueChanged"/> event for the "Side Length" <see
        /// cref="NumericUpDown"/> control on the <see cref="StructureTab"/> page.</summary>
        /// <param name="sender">
        /// The <see cref="NumericUpDown"/> control sending the event.</param>
        /// <param name="args">
        /// An <see cref="EventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnLengthChanged</b> resizes the <see cref="PolygonGrid.Element"/> of the <see
        /// cref="MapGrid"/> to the value of the "Side Length" control, and sets the <see
        /// cref="DataChanged"/> flag if the value has changed.</remarks>

        private void OnLengthChanged(object sender, EventArgs args) {
            if (this._ignoreEvents) return;

            // get new polygon side length
            double length = (float) LengthUpDown.Value;
            RegularPolygon element = MapGrid.Element;

            // create polygon with new side length if changed
            if (length != element.Length) {
                this._mapGrid.Element = element.Resize(length);

                // broadcast data changes
                DataChanged = true;
            }
        }

        #endregion
        #region OnShapeChecked

        /// <summary>
        /// Handles the <see cref="ToggleButton.Checked"/> event for the "Element Shape" <see
        /// cref="RadioButton"/> controls on the <see cref="StructureTab"/> page.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="RoutedEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnShapeChecked</b> changes the side count and orientation of the <see
        /// cref="PolygonGrid.Element"/> of the <see cref="MapGrid"/> to the values indicated by the
        /// checked "Element Shape" radio button, and sets the <see cref="DataChanged"/> flag if
        /// either value has changed.</remarks>

        private void OnShapeChecked(object sender, RoutedEventArgs args) {
            args.Handled = true;
            if (this._ignoreEvents) return;

            // get new side count and polygon orientation
            int sides; PolygonOrientation orientation;
            if (SquareOnEdgeToggle.IsChecked == true) {
                sides = 4; orientation = PolygonOrientation.OnEdge;
            } else if (SquareOnVertexToggle.IsChecked == true) {
                sides = 4; orientation = PolygonOrientation.OnVertex;
            } else if (HexagonOnEdgeToggle.IsChecked == true) {
                sides = 6; orientation = PolygonOrientation.OnEdge;
            } else if (HexagonOnVertexToggle.IsChecked == true) {
                sides = 6; orientation = PolygonOrientation.OnVertex;
            } else return;

            // create polygon with new shape if changed
            RegularPolygon element = MapGrid.Element;
            if (sides != element.Sides || orientation != element.Orientation) {

                // only squares can enable vertex neighbors
                bool vertexNeighbors = (element.VertexNeighbors && (sides == 4));

                this._mapGrid.Element = new RegularPolygon(
                    element.Length, sides, orientation, vertexNeighbors);

                // broadcast data changes
                UpdateStructure();
                DataChanged = true;
            }
        }

        #endregion
        #region OnShiftChecked

        /// <summary>
        /// Handles the <see cref="ToggleButton.Checked"/> event for the "Grid Shift" <see
        /// cref="RadioButton"/> controls on the <see cref="StructureTab"/> page.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="RoutedEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnShiftChecked</b> changes the <see cref="PolygonGrid.GridShift"/> value of the <see
        /// cref="MapGrid"/> to that indicated by the checked "Grid Shift" radio button, and sets
        /// the <see cref="DataChanged"/> flag if the value has changed.</remarks>

        private void OnShiftChecked(object sender, RoutedEventArgs args) {
            args.Handled = true;
            if (this._ignoreEvents) return;

            // get new grid shifting
            PolygonGridShift gridShift;
            if (ShiftNoneToggle.IsChecked == true)
                gridShift = PolygonGridShift.None;
            else if (ColumnUpToggle.IsChecked == true)
                gridShift = PolygonGridShift.ColumnUp;
            else if (ColumnDownToggle.IsChecked == true)
                gridShift = PolygonGridShift.ColumnDown;
            else if (RowLeftToggle.IsChecked == true)
                gridShift = PolygonGridShift.RowLeft;
            else if (RowRightToggle.IsChecked == true)
                gridShift = PolygonGridShift.RowRight;
            else return;

            // sanity check for element shape
            if (!PolygonGrid.AreCompatible(MapGrid.Element, gridShift))
                return;

            // assign new grid shifting
            if (gridShift != MapGrid.GridShift) {
                this._mapGrid.GridShift = gridShift;

                // broadcast data changes
                DataChanged = true;
            }
        }

        #endregion
        #region OnVertexNeighbors

        /// <summary>
        /// Handles the <see cref="ButtonBase.Click"/> event for the "Vertex Neighbors" <see
        /// cref="CheckBox"/> on the <see cref="StructureTab"/> page.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="RoutedEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnVertexNeighbors</b> updates the <see cref="PolygonGrid.Element"/> of the <see
        /// cref="MapGrid"/> to the value of the "Vertex Neighbors" check box, and sets the <see
        /// cref="DataChanged"/> flag if the value has changed.</remarks>

        private void OnVertexNeighbors(object sender, RoutedEventArgs args) {
            args.Handled = true;
            if (this._ignoreEvents) return;

            // get new vertex neighbors option
            bool vertexNeighbors = (VertexNeighborsToggle.IsChecked == true);

            // sanity check for side count
            RegularPolygon element = MapGrid.Element;
            if (vertexNeighbors && element.Sides != 4) return;

            // create polygon with new option if changed
            if (vertexNeighbors != element.VertexNeighbors) {
                this._mapGrid.Element = new RegularPolygon(element.Length,
                    element.Sides, element.Orientation, vertexNeighbors);

                // broadcast data changes
                DataChanged = true;
            }
        }

        #endregion
        #endregion
        #region Class ImageSizeItem

        /// <summary>
        /// Wraps a <see cref="SizeI"/> value for display in a <see cref="ComboBox"/>.</summary>

        private class ImageSizeItem {
            #region ImageSizeItem(SizeI)

            /// <summary>
            /// Initializes a new instance of the <see cref="ImageSizeItem"/> class.</summary>
            /// <param name="value">
            /// The initial value for the <see cref="Value"/> field.</param>

            internal ImageSizeItem(SizeI value) {
                Value = value;
            }

            #endregion
            #region Value

            /// <summary>
            /// The <see cref="SizeI"/> value to display in the <see cref="ComboBox"/>.</summary>

            internal readonly SizeI Value;

            #endregion
            #region ToString

            /// <summary>
            /// Returns a <see cref="String"/> that represents the <see cref="ImageSizeItem"/>.
            /// </summary>
            /// <returns>
            /// The <see cref="SizeI.Width"/> and <see cref="SizeI.Height"/> of the <see
            /// cref="Value"/> property.</returns>

            public override string ToString() {
                return String.Format(ApplicationInfo.Culture,
                    "{0} x {1}", Value.Width, Value.Height);
            }

            #endregion
        }

        #endregion
    }
}
