using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using Tektosyne;
using Tektosyne.Geometry;

using Hexkit.Global;
using Hexkit.Scenario;

namespace Hexkit.Editor.Dialog {

    /// <summary>
    /// Shows a dialog allowing the user to define <see cref="ImageFrame"/> connections across <see
    /// cref="RegularPolygon"/> edges and vertices.</summary>
    /// <remarks>
    /// Please refer to the "Change Connections" page of the "Editor Dialogs" section in the
    /// application help file for details on this dialog.</remarks>

    public partial class ChangeConnections: Window {
        #region ChangeConnections(...)

        /// <summary>
        /// Initializes a new instance of the <see cref="ChangeConnections"/> class.</summary>
        /// <param name="frame">
        /// The <see cref="ImageFrame"/> whose <see cref="ImageFrame.Connections"/> to edit.</param>
        /// <param name="bitmap">
        /// The <see cref="WriteableBitmap"/> containing the <see cref="ImageFrame.Bounds"/> of the
        /// specified <paramref name="frame"/>. This argument may be a null reference.</param>
        /// <param name="info">
        /// Informational text about the <paramref name="frame"/> to display in the dialog.</param>
        /// <param name="scalingX">
        /// An <see cref="ImageScaling"/> value indicating the horizontal scaling of the specified
        /// <paramref name="frame"/>.</param>
        /// <param name="scalingY">
        /// An <see cref="ImageScaling"/> value indicating the vertical scaling of the specified
        /// <paramref name="frame"/>.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="frame"/> is a null reference.</exception>
        /// <exception cref="ArgumentNullOrEmptyException">
        /// <paramref name="info"/> is a null reference or an empty string.</exception>

        public ChangeConnections(ImageFrame frame, WriteableBitmap bitmap,
            string info, ImageScaling scalingX, ImageScaling scalingY) {

            if (frame == null)
                ThrowHelper.ThrowArgumentNullException("frame");
            if (String.IsNullOrEmpty(info))
                ThrowHelper.ThrowArgumentNullOrEmptyException("info");

            this._frame = frame;
            InitializeComponent();

            // show specified frame information
            FrameInfo.Content = (string) FrameInfo.Content + " " + info;

            // show specified image frame
            FramePreview.Polygon = MasterSection.Instance.Areas.MapGrid.Element;
            FramePreview.ScalingX = scalingX;
            FramePreview.ScalingY = scalingY;
            FramePreview.Show(frame, bitmap);

            // initialize directional toggle buttons
            this._compassControls = new CompassControl[] {
                new CompassControl(NorthToggle, Compass.North, Symbols.ArrowUp),
                new CompassControl(NorthEastToggle, Compass.NorthEast, Symbols.ArrowRightUp),
                new CompassControl(EastToggle, Compass.East, Symbols.ArrowRight),
                new CompassControl(SouthEastToggle, Compass.SouthEast, Symbols.ArrowRightDown),
                new CompassControl(SouthToggle, Compass.South, Symbols.ArrowDown),
                new CompassControl(SouthWestToggle, Compass.SouthWest, Symbols.ArrowLeftDown),
                new CompassControl(WestToggle, Compass.West, Symbols.ArrowLeft),
                new CompassControl(NorthWestToggle, Compass.NorthWest, Symbols.ArrowLeftUp)
            };

            // set checked states to Connections values
            foreach (Compass compass in frame.Connections) {
                int index = CompassToIndex(compass);
                this._compassControls[index].Button.IsChecked = true;
            }

            // disable controls invalid for current polygon
            EnableControls();
        }

        #endregion
        #region Private Fields

        // construction parameters
        private readonly ImageFrame _frame;

        // initializers for Compass controls
        private readonly CompassControl[] _compassControls;

        #endregion
        #region DataChanged

        /// <summary>
        /// Gets a value indicating whether any data was changed in the dialog.</summary>
        /// <value>
        /// <c>true</c> if the user has changed any connections and did not cancel the dialog;
        /// otherwise, <c>false</c>.</value>
        /// <remarks>
        /// <b>DataChanged</b> returns <c>false</c> if the objects supplied to the constructor were
        /// not modified in any detectable way. However, the original data may have been overwritten
        /// with a copy that is not detectably different, namely if the user clicked <b>OK</b>
        /// without making any changes.</remarks>

        public bool DataChanged { get; private set; }

        #endregion
        #region Private Methods
        #region CompassToIndex

        /// <summary>
        /// Converts the specified <see cref="Compass"/> direction to the corresponding <see
        /// cref="ToggleButton"/> index.</summary>
        /// <param name="compass">
        /// The <see cref="Compass"/> direction to convert.</param>
        /// <returns>
        /// The index of the <see cref="ToggleButton"/> that represents the specified <paramref
        /// name="compass"/> direction.</returns>
        /// <exception cref="InvalidEnumArgumentException">
        /// <paramref name="compass"/> is not a valid <see cref="Compass"/> value.</exception>

        private static int CompassToIndex(Compass compass) {
            switch (compass) {

                case Compass.North:     return 0;
                case Compass.NorthEast: return 1;
                case Compass.East:      return 2;
                case Compass.SouthEast: return 3;
                case Compass.South:     return 4;
                case Compass.SouthWest: return 5;
                case Compass.West:      return 6;
                case Compass.NorthWest: return 7;

                default:
                    ThrowHelper.ThrowInvalidEnumArgumentException(
                        "compass", (int) compass, typeof(Compass));
                    return -1;
            }
        }

        #endregion
        #region EnableControls

        /// <summary>
        /// Enables or disables all hosted controls based on the current <see
        /// cref="AreaSection.MapGrid"/>.</summary>
        /// <remarks>
        /// <b>EnableControls</b> disables any arrow buttons whose directions are not supported by
        /// the <see cref="PolygonGrid.Element"/> shape of the current <see
        /// cref="AreaSection.MapGrid"/>.</remarks>

        private void EnableControls() {
            RegularPolygon element = MasterSection.Instance.Areas.MapGrid.Element;

            // vertex neighbors enable all directions
            if (element.VertexNeighbors) return;

            bool isSquare = (element.Sides == 4);
            bool onEdge = (element.Orientation == PolygonOrientation.OnEdge);

            // connections are symmetrical in all directions
            bool vertical = onEdge, diagonal = !isSquare || !onEdge;
            bool horizontal = (isSquare && onEdge) || (!isSquare && !onEdge);

            NorthToggle.IsEnabled = vertical;
            NorthEastToggle.IsEnabled = diagonal;
            EastToggle.IsEnabled = horizontal;
            SouthEastToggle.IsEnabled = diagonal;
            SouthToggle.IsEnabled = vertical;
            SouthWestToggle.IsEnabled = diagonal;
            WestToggle.IsEnabled = horizontal;
            NorthWestToggle.IsEnabled = diagonal;
        }

        #endregion
        #endregion
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
        /// cref="ChangeConnections"/> dialog.</remarks>

        private void HelpExecuted(object sender, ExecutedRoutedEventArgs args) {
            args.Handled = true;
            ApplicationUtility.ShowHelp("DlgChangeConnections.html");
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
        /// handles the <see cref="Window.Closing"/> event by copying the <see
        /// cref="ToggleButton.IsChecked"/> values of the six directional buttons to the
        /// corresponding elements of the <see cref="ImageFrame.Connections"/> property of the <see
        /// cref="ImageFrame"/> being edited.
        /// </para><para>
        /// <b>OnClosing</b> also sets the <see cref="DataChanged"/> flag if any <see
        /// cref="ImageFrame.Connections"/> values were changed.</para></remarks>

        protected override void OnClosing(CancelEventArgs args) {
            base.OnClosing(args);
            if (args.Cancel) return;

            // user cancelled dialog, ignore changes
            if (DialogResult != true) {
                DataChanged = false;
                return;
            }

            // read checked states into Connections
            foreach (CompassControl control in this._compassControls) {
                Compass direction = (Compass) control.Button.Tag;
                bool isConnected = this._frame.Connections.Contains(direction);

                // notify client if values have changed
                if (control.Button.IsChecked != isConnected) {
                    DataChanged = true;

                    if (isConnected)
                        this._frame.Connections.Remove(direction);
                    else
                        this._frame.Connections.Add(direction);
                }
            }
        }

        #endregion
        #region Class CompassControl

        /// <summary>
        /// Combines a <see cref="Compass"/> direction with a <see cref="ToggleButton"/> control
        /// that allows the user to select that direction.</summary>

        private class CompassControl {
            #region CompassControl(...)

            /// <summary>
            /// Initializes a new instance of the <see cref="CompassControl"/> class with the
            /// specified <see cref="ToggleButton"/>, <see cref="Compass"/> direction, and symbolic
            /// character.</summary>
            /// <param name="button">
            /// The initial value for the <see cref="Button"/> property.</param>
            /// <param name="direction">
            /// The initial value for the <see cref="FrameworkElement.Tag"/> property of the
            /// specified <paramref name="button"/>.</param>
            /// <param name="symbol">
            /// The symbolic character to show in the specified <paramref name="button"/>.</param>

            public CompassControl(ToggleButton button, Compass direction, string symbol) {
                Button = button;
                button.Tag = direction;
                button.ShowSymbol(symbol);
            }

            #endregion
            #region Button

            /// <summary>
            /// Gets the associated <see cref="ToggleButton"/> control.</summary>
            /// <value>
            /// The <see cref="ToggleButton"/> associated with the <see cref="CompassControl"/>.
            /// </value>

            public ToggleButton Button { get; private set; }

            #endregion
        }

        #endregion
    }
}
