using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

using Tektosyne;
using Tektosyne.Windows;

using Hexkit.Global;
using Hexkit.Graphics;
using Hexkit.Scenario;

namespace Hexkit.Editor {

    /// <summary>
    /// Shows a dialog allowing the user to select a frame of an <see cref="EntityImage"/>.
    /// </summary>
    /// <remarks><para>
    /// Please refer to the "Change Frame" page of the "Editor Dialogs" section in the application
    /// help file for details on this dialog.
    /// </para><note type="implementnotes">
    /// <b>ChangeFrame</b> requires valid <see cref="ImageFile"/> bitmaps.</note></remarks>

    public partial class ChangeFrame: Window {
        #region ChangeFrame(ImageStackEntry)

        /// <summary>
        /// Initializes a new instance of the <see cref="ChangeFrame"/> class with the specified
        /// <see cref="ImageStackEntry"/>.</summary>
        /// <param name="entry">
        /// The <see cref="ImageStackEntry"/> whose <see cref="ImageStackEntry.SingleFrame"/> value
        /// to change.</param>
        /// <exception cref="ArgumentException">
        /// <paramref name="entry"/> contains an <see cref="ImageStackEntry.Image"/> that is a null
        /// reference or contains less than two <see cref="EntityImage.Frames"/>.</exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="entry"/> is a null reference.</exception>
        /// <remarks>
        /// The data of the specified <paramref name="entry"/> may be changed in the dialog, as
        /// indicated by the value of the <see cref="DataChanged"/> property.</remarks>

        public ChangeFrame(ImageStackEntry entry) {
            if (entry == null)
                ThrowHelper.ThrowArgumentNullException("entry");

            EntityImage image = entry.Image.Value;
            if (image == null)
                ThrowHelper.ThrowArgumentExceptionWithFormat("entry",
                    Tektosyne.Strings.ArgumentSpecifiesInvalid, "Image");

            if (image.Frames.Count < 2)
                ThrowHelper.ThrowArgumentExceptionWithFormat("entry",
                    Global.Strings.ArgumentSpecifiesSingleFrame, "Image");

            this._entry = entry;
            InitializeComponent();
            Title += image.Id;

            // prepare list box for image frames
            FrameList.Polygon = MasterSection.Instance.Areas.MapGrid.Element;

            // use image scaling unless disabled
            if (!entry.UseUnscaled) {
                FrameList.ScalingX = image.ScalingX;
                FrameList.ScalingY = image.ScalingY;
            }

            // add image frames to list box
            Debug.Assert(image.Frames.Count >= 2);
            foreach (ImageFrame frame in image.Frames)
                FrameList.Items.Add(new ImageListBoxItem(frame));

            // select initial item in Frames list box
            int index = Math.Max(0, this._entry.SingleFrame);
            FrameList.SelectAndShow(Math.Min(index, FrameList.Items.Count - 1));
        }

        #endregion
        #region Private Fields

        // construction arguments
        private readonly ImageStackEntry _entry;

        #endregion
        #region DataChanged

        /// <summary>
        /// Gets a value indicating whether any data was changed in the dialog.</summary>
        /// <value>
        /// <c>true</c> if the <see cref="ImageStackEntry"/> supplied to the dialog constructor has
        /// been modified; otherwise, <c>false</c>.</value>

        public bool DataChanged { get; private set; }

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
        /// cref="ChangeFrame"/> dialog.</remarks>

        private void HelpExecuted(object sender, ExecutedRoutedEventArgs args) {
            args.Handled = true;
            ApplicationUtility.ShowHelp("DlgChangeFrame.html");
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
        #region OnFrameActivate

        /// <summary>
        /// Handles the <see cref="Control.MouseDoubleClick"/> event for an <see
        /// cref="ImageListBoxItem"/> of the <see cref="ImageListBox"/> containing the image frames.
        /// </summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="MouseButtonEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnFrameActivate</b> selects the double-clicked item in the <see
        /// cref="ImageListBox"/>, and sets the <see cref="Window.DialogResult"/> of the <see
        /// cref="ChangeFrame"/> dialog to <c>true</c> to confirm that selection.</remarks>

        private void OnFrameActivate(object sender, MouseButtonEventArgs args) {
            args.Handled = true;

            // retrieve double-clicked item, if any
            var source = args.OriginalSource as DependencyObject;
            var item = ItemsControl.ContainerFromElement(FrameList, source) as ImageListBoxItem;

            if (item != null) {
                FrameList.SelectAndShow(item);
                DialogResult = true;
            }
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
        /// Otherwise, <b>OnClosing</b> sets the <see cref="ImageStackEntry.SingleFrame"/> property 
        /// of the edited <see cref="ImageStackEntry"/> to the currently selected image frame in the
        /// hosted <see cref="ImageListBox"/>, and sets the <see cref="DataChanged"/> flag.
        /// </para></remarks>

        protected override void OnClosing(CancelEventArgs args) {
            base.OnClosing(args);
            if (args.Cancel) return;

            // user cancelled dialog, ignore changes
            if (DialogResult != true) {
                DataChanged = false;
                return;
            }

            // check for different frame selection
            int index = FrameList.SelectedIndex;
            if (this._entry.SingleFrame != index) {
                this._entry.SingleFrame = index;
                DataChanged = true;
            }
        }

        #endregion
    }
}
