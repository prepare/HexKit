using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

using Tektosyne;
using Tektosyne.Geometry;
using Tektosyne.IO;
using Tektosyne.Windows;

using Hexkit.Global;
using Hexkit.Graphics;
using Hexkit.Options;
using Hexkit.Scenario;

namespace Hexkit.Editor.Dialog {

    using NumericUpDown = System.Windows.Forms.NumericUpDown;

    /// <summary>
    /// Shows a dialog allowing the user to change an <see cref="OverlayImage"/>.</summary>
    /// <remarks>
    /// Please refer to the "Change Overlay" page of the "Editor Dialogs" section in the application
    /// help file for details on this dialog.</remarks>

    public partial class ChangeOverlay: Window {
        #region ChangeOverlay(Boolean)

        /// <summary>
        /// Initializes a new instance of the <see cref="ChangeOverlay"/> class with the specified
        /// <see cref="OverlayImage"/>.</summary>
        /// <param name="editing">
        /// <c>true</c> to edit the <see cref="EditorOptions.Overlay"/> defined by the current <see
        /// cref="EditorOptions"/>; <c>false</c> to edit the <see cref="AreaSection.Overlay"/>
        /// defined by the current <see cref="AreaSection"/>.</param>
        /// <exception cref="InvalidOperationException">
        /// <paramref name="editing"/> is <c>true</c>, and <see cref="ApplicationInfo.IsEditor"/> is
        /// <c>false</c>.</exception>
        /// <remarks>
        /// The data of the specified <see cref="OverlayImage"/> may be changed in the dialog, as
        /// indicated by the value of the <see cref="DataChanged"/> property.</remarks>

        public ChangeOverlay(bool editing) {

            if (editing) {
                ApplicationInfo.CheckEditor();
                this._overlay = ApplicationOptions.Instance.Editor.Overlay;
            } else
                this._overlay = MasterSection.Instance.Areas.Overlay;

            // copy original data for user cancellation
            this._originalOverlay = (OverlayImage) this._overlay.Clone();
            this._editing = editing;

            InitializeComponent();

            // set maximum ranges for image coordinates
            LeftUpDown.Minimum = Int32.MinValue;
            LeftUpDown.Maximum = Int32.MaxValue;
            TopUpDown.Minimum = Int32.MinValue;
            TopUpDown.Maximum = Int32.MaxValue;

            WidthUpDown.Minimum = 1;
            WidthUpDown.Maximum = Int32.MaxValue;
            HeightUpDown.Minimum = 1;
            HeightUpDown.Maximum = Int32.MaxValue;

            // show current map dimensions at 100% scale
            var mapBounds = MapViewManager.Instance.MapGrid.DisplayBounds;

            MapWidthInfo.Text = mapBounds.Width.ToString("N0", ApplicationInfo.Culture);
            MapHeightInfo.Text = mapBounds.Height.ToString("N0", ApplicationInfo.Culture);

            BorderWidthInfo.Text = MapView.MapBorder.X.ToString("N0", ApplicationInfo.Culture);
            BorderHeightInfo.Text = MapView.MapBorder.Y.ToString("N0", ApplicationInfo.Culture);

            // initialize size if at default values
            if (this._overlay.Bounds.Width == 1 && this._overlay.Bounds.Height == 1)
                this._overlay.Bounds = new RectI(
                    this._overlay.Bounds.Left, this._overlay.Bounds.Top,
                    Fortran.NInt(mapBounds.Width), Fortran.NInt(mapBounds.Height));

            // show current overlay data
            ShowImagePath();
            ShowImageSize();
            AspectToggle.IsChecked = this._overlay.PreserveAspectRatio;

            LeftUpDown.Value = this._overlay.Bounds.Left;
            TopUpDown.Value = this._overlay.Bounds.Top;
            WidthUpDown.Value = this._overlay.Bounds.Width;
            HeightUpDown.Value = this._overlay.Bounds.Height;

            // construction completed
            this._initialized = true;
        }

        #endregion
        #region Private Fields

        // construction arguments
        private readonly bool _editing;

        // edited & original overlay image
        private readonly OverlayImage _overlay, _originalOverlay;

        // was construction completed?
        private readonly bool _initialized = false;

        // ignore control input events?
        private bool _ignoreEvents;

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
        #region ShowImagePath

        /// <summary>
        /// Shows the file name and file path of the edited <see cref="OverlayImage"/> in the "File"
        /// <see cref="TextBox"/> and its <see cref="ToolTip"/>.</summary>

        private void ShowImagePath() {

            if (this._overlay.IsEmpty) {
                PathInfo.Text = Global.Strings.LabelImageNone;
                PathInfo.ToolTip = null;
            } else {
                string path = this._overlay.Path;
                PathInfo.Text = Path.GetFileName(path);
                PathInfo.ToolTip = new ToolTip() { Content = path };
            }
        }

        #endregion
        #region ShowImageSize

        /// <summary>
        /// Shows the original dimensions of the edited <see cref="OverlayImage"/> in the
        /// "References" <see cref="GroupBox"/>.</summary>

        private void ShowImageSize() {
            var bitmap = this._overlay.Bitmap;

            if (bitmap == null) {
                ImageWidthInfo.Text = "—";
                ImageHeightInfo.Text = "—";
            } else {
                ImageWidthInfo.Text = bitmap.PixelWidth.ToString("N0", ApplicationInfo.Culture);
                ImageHeightInfo.Text = bitmap.PixelHeight.ToString("N0", ApplicationInfo.Culture);
            }
        }

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
        /// cref="ChangeOverlay"/> dialog.</remarks>

        private void HelpExecuted(object sender, ExecutedRoutedEventArgs args) {
            args.Handled = true;
            ApplicationUtility.ShowHelp("DlgChangeOverlay.html");
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
        #region OnAspectChanged

        /// <summary>
        /// Handles the <see cref="ButtonBase.Click"/> event for the "Preserve Original Aspect
        /// Ratio" <see cref="CheckBox"/>.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="RoutedEventArgs"/> object containing event data.</param>
        /// <remarks><para>
        /// <b>OnAspectChanged</b> updates the <see cref="OverlayImage.PreserveAspectRatio"/> flag
        /// and sets the <see cref="DataChanged"/> flag if its value has changed.
        /// </para><para>
        /// If <see cref="OverlayImage.PreserveAspectRatio"/> is now <c>true</c>,
        /// <b>OnAspectChanged</b> also restores the original <see cref="OverlayImage.AspectRatio"/>
        /// of the edited <see cref="OverlayImage"/> by shrinking either its width or its height.
        /// </para></remarks>

        private void OnAspectChanged(object sender, RoutedEventArgs args) {
            args.Handled = true;
            if (this._ignoreEvents || !this._initialized)
                return;

            bool preserve = (AspectToggle.IsChecked == true);
            if (this._overlay.PreserveAspectRatio == preserve)
                return;

            // broadcast data changes
            this._overlay.PreserveAspectRatio = preserve;
            DataChanged = true;
            if (!preserve) return;

            // retrieve bounds from numeric controls
            int left = (int) LeftUpDown.Value, top = (int) TopUpDown.Value;
            int width = (int) WidthUpDown.Value, height = (int) HeightUpDown.Value;

            double ratio = this._overlay.AspectRatio;
            if (ratio <= 0.0) return;

            // compute dimensions at original aspect ratio
            int aspectWidth = Fortran.NInt(height * ratio);
            int aspectHeight = Fortran.NInt(width / ratio);

            if (aspectWidth < width || aspectHeight < height) {
                this._ignoreEvents = true;

                // shrink image to original aspect ratio
                if (aspectWidth < width) {
                    width = aspectWidth;
                    WidthUpDown.Value = aspectWidth;
                }
                else if (aspectHeight < height) {
                    height = aspectHeight;
                    HeightUpDown.Value = aspectHeight;
                }

                this._ignoreEvents = false;

                // show new image bounds
                this._overlay.Bounds = new RectI(left, top, width, height);
                AreasTabContent.MapView.ShowOverlay(this, this._editing);
            }
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
        /// <b>OnBoundsChanged</b> checks if the bounds specified by the "Coordinates" controls are
        /// actually different from the current <see cref="OverlayImage.Bounds"/> of the edited <see
        /// cref="OverlayImage"/>. (This may not be case for programmatic changes.)
        /// </para><para>
        /// If so, <b>OnBoundsChanged</b> updates the <see cref="OverlayImage"/> and the default
        /// <see cref="AreasTabContent.MapView"/> to reflect the new bounds, and sets the <see
        /// cref="DataChanged"/> flag.
        /// </para><para>
        /// If the "Preserve Original Aspect Ratio" option is enabled, <b>OnBoundsChanged</b> also
        /// adjusts the height of the <see cref="OverlayImage"/> if its width was changed, and vice
        /// versa, to preserve its original <see cref="OverlayImage.AspectRatio"/>.</para></remarks>

        private void OnBoundsChanged(object sender, EventArgs args) {
            if (this._ignoreEvents || !this._initialized)
                return;

            // retrieve bounds from numeric controls
            RectI bounds = new RectI(
                (int) LeftUpDown.Value, (int) TopUpDown.Value,
                (int) WidthUpDown.Value, (int) HeightUpDown.Value);

            // update overlay only if bounds have changed
            if (this._overlay.Bounds != bounds) {

                if (AspectToggle.IsChecked == true) {
                    bool widthChanged = (sender == WidthUpDown.HostedControl);
                    bool heightChanged = (sender == HeightUpDown.HostedControl);

                    // adjust width or height to preserve aspect ratio
                    if (widthChanged || heightChanged) {
                        double ratio = this._overlay.AspectRatio;
                        if (ratio > 0.0) {
                            this._ignoreEvents = true;
                            int width = bounds.Width, height = bounds.Height;

                            if (widthChanged) {
                                height = Fortran.NInt(width / ratio);
                                HeightUpDown.Value = height;
                            } else {
                                Debug.Assert(heightChanged);
                                width = Fortran.NInt(height * ratio);
                                WidthUpDown.Value = width;
                            }

                            bounds = new RectI(bounds.Left, bounds.Top, width, height);
                            this._ignoreEvents = false;
                        }
                    }
                }

                // broadcast data changes
                this._overlay.Bounds = bounds;
                AreasTabContent.MapView.ShowOverlay(this, this._editing);
                DataChanged = true;
            }
        }

        #endregion
        #region OnCancel

        /// <summary>
        /// Handles the <see cref="ButtonBase.Click"/> event for the "Cancel" <see cref="Button"/>.
        /// </summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="RoutedEventArgs"/> object containing event data.</param>
        /// <remarks><para>
        /// <b>OnCancel</b> sets the <see cref="Window.DialogResult"/> property to <c>false</c>.
        /// This also triggers the <see cref="Window.Closing"/> event.
        /// </para><para>
        /// <b>OnCancel</b> also sets the <see cref="DataChanged"/> property to <c>false</c>,
        /// restores the edited <see cref="OverlayImage"/> to its original property values, and
        /// updates the default <see cref="AreasTabContent.MapView"/> accordingly.</para></remarks>

        private void OnCancel(object sender, RoutedEventArgs args) {
            args.Handled = true;
            DialogResult = false;
            DataChanged = false;

            // restore edited overlay image to original values
            this._overlay.Bounds = this._originalOverlay.Bounds;
            this._overlay.Path = this._originalOverlay.Path;

            AreasTabContent.MapView.ShowOverlay(this, this._editing);
        }

        #endregion
        #region OnPathBrowse

        /// <summary>
        /// Handles the <see cref="ButtonBase.Click"/> event for the "Browse…" <see cref="Button"/>.
        /// </summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="RoutedEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnPathBrowse</b> shows an <see cref="FileDialogs.OpenImageDialog"/> allowing the user
        /// to select a new <see cref="OverlayImage.Path"/>, and sets the <see cref="DataChanged"/>
        /// flag if the user selects a valid new file path.</remarks>

        private void OnPathBrowse(object sender, RoutedEventArgs args) {
            args.Handled = true;

            // ask user to enter an existing image
            RootedPath path = FileDialogs.OpenImageDialog(this._overlay.Path);

            // quit if user cancelled, or entered old or invalid path
            if (path == null || path.IsEmpty || path.Equals(this._overlay.Path)
                || !File.Exists(path.AbsolutePath))
                return;

            // disable dialog while image loads
            IsEnabled = false;
            Dispatcher.DoEvents();

            // try loading overlay image
            this._overlay.Path = path.AbsolutePath;
            if (!AreasTabContent.MapView.ShowOverlay(this, this._editing))
                path.Clear();

            // broadcast data changes
            ShowImagePath();
            ShowImageSize();
            DataChanged = true;

            IsEnabled = true; // reenable dialog
        }

        #endregion
        #region OnPathClear

        /// <summary>
        /// Handles the <see cref="ButtonBase.Click"/> event for the "Clear" <see cref="Button"/>.
        /// </summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="RoutedEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnPathClear</b> clears the current <see cref="TextBox.Text"/> of the "Path" text box
        /// and the <see cref="OverlayImage.Path"/> of the edited <see cref="OverlayImage"/>, and
        /// sets the <see cref="DataChanged"/> flag if the previous path was not empty.</remarks>

        private void OnPathClear(object sender, RoutedEventArgs args) {
            args.Handled = true;

            // broadcast data changes
            if (!this._overlay.IsEmpty) {
                this._overlay.Path = "";
                AreasTabContent.MapView.ShowOverlay(this, this._editing);

                ShowImagePath();
                ShowImageSize();
                DataChanged = true;
            }
        }

        #endregion
        #endregion
    }
}
