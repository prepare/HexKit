using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

using Tektosyne;
using Tektosyne.Geometry;
using Tektosyne.IO;
using Tektosyne.Windows;

using Hexkit.Global;
using Hexkit.Graphics;
using Hexkit.Scenario;

namespace Hexkit.Editor {

    /// <summary>
    /// Provides the "Images" tab page for the Hexkit Editor application.</summary>
    /// <remarks>
    /// Please refer to the "Images Page" page of the "Editor Display" section in the application
    /// help file for details on this tab page.</remarks>

    public partial class ImagesTabContent: UserControl, IEditorTabContent {
        #region ImagesTabContent()

        /// <summary>
        /// Initializes a new instance of the <see cref="ImagesTabContent"/> class.</summary>

        public ImagesTabContent() {
            InitializeComponent();

            // adjust column widths of File list view
            DependencyPropertyDescriptor.FromProperty(
                ListView.ActualWidthProperty, typeof(ListView))
                .AddValueChanged(FileList, OnFileWidthChanged);
        }

        #endregion
        #region Private Fields

        // property backers
        private SectionTabItem _sectionTab;

        #endregion
        #region Private Methods
        #region ChangeImage

        /// <summary>
        /// Allows the user to change the <see cref="EntityImage"/> associated with the item at the
        /// specified index in the <see cref="ImageListBox"/>.</summary>
        /// <param name="index">
        /// The index of the <see cref="ImageListBoxItem"/> whose image to change.</param>
        /// <remarks><para>
        /// <b>ChangeImage</b> shows an error message if the "Image File" list view is empty.
        /// Otherwise, <b>ChangeImage</b> displays a <see cref="Dialog.ChangeImage"/> dialog for the
        /// <see cref="ImageListBoxItem"/> with the specified <paramref name="index"/>.
        /// </para><para>
        /// If the user made any changes, <b>ChangeImage</b> propagates them to the current <see
        /// cref="ImageSection"/>, redisplays all images, and sets the <see
        /// cref="SectionTabItem.DataChanged"/> flag.</para></remarks>

        private void ChangeImage(int index) {
            if (index < 0) return;
            var item = (ImageListBoxItem) ImageList.Items[index];
            var image = (EntityImage) item.Content;

            // abort if there are no image files
            if (FileList.Items.Count == 0) {
                MessageBox.Show(MainWindow.Instance,
                    Global.Strings.DialogImageFileNone, Global.Strings.TitleImageFileNone,
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // show dialog and let user make changes
            var dialog = new Dialog.ChangeImage(image) { Owner = MainWindow.Instance };
            dialog.ShowDialog();

            // broadcast data changes, if any
            if (dialog.DataChanged) {
                item = new ImageListBoxItem(image);
                ImageList.Items[index] = item;
                ImageList.Items.Refresh();
                ImageList.SelectAndShow(index);

                SectionTab.DataChanged = true;
            }
        }

        #endregion
        #region EnableListButtons

        /// <summary>
        /// Enables or disables the "Change ID", "Change Path/Image", and "Remove File/Image" <see
        /// cref="Button"/> controls, depending on whether the "Image File" <see cref="ListView"/>
        /// or <see cref="ImageListBox"/>, respectively, contain any items.</summary>

        private void EnableListButtons() {

            // enable or disable file list buttons
            bool anyFiles = (FileList.Items.Count > 0);

            ChangeFileIdButton.IsEnabled = anyFiles;
            ChangeFileButton.IsEnabled = anyFiles;
            RemoveFileButton.IsEnabled = anyFiles;

            // enable or disable image list buttons
            bool anyImages = (ImageList.Items.Count > 0);

            ChangeImageIdButton.IsEnabled = anyImages;
            ChangeImageButton.IsEnabled = anyImages;
            RemoveImageButton.IsEnabled = anyImages;
        }

        #endregion
        #region LoadImageFile

        /// <summary>
        /// Loads the disk file associated with the specified <see cref="ImageFile"/> using the
        /// specified masking <see cref="Color"/>.</summary>
        /// <param name="imageFile">
        /// The <see cref="ImageFile"/> object whose disk file to load.</param>
        /// <param name="mask"><para>
        /// A <see cref="Color"/> value indicating the color to make transparent.
        /// </para><para>-or-</para><para>
        /// <see cref="Colors.Transparent"/> to use only the transparency information embedded in
        /// the <paramref name="imageFile"/>.</para></param>
        /// <returns>
        /// <c>true</c> if the disk file was loaded successfully; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="imageFile"/> is a null reference.</exception>
        /// <remarks><para>
        /// <b>LoadImageFile</b> invokes <see cref="ImageFile.Load"/> on the specified <paramref
        /// name="imageFile"/> with specified color <paramref name="mask"/>.
        /// </para><para>
        /// If an exception occurs, <b>LoadImageFile</b> shows an error message, calls <see
        /// cref="ImageFile.Unload"/> on the specified <paramref name="imageFile"/>, and returns
        /// <c>false</c>. However, such exceptions are never propagated to the caller.
        /// </para></remarks>

        private static bool LoadImageFile(ImageFile imageFile, Color mask) {
            if (imageFile == null)
                ThrowHelper.ThrowArgumentNullException("imageFile");

            try {
                // try loading image file
                imageFile.Load(mask);
            }
            catch (Exception e) {
                MessageDialog.Show(MainWindow.Instance, null,
                    Global.Strings.TitleSectionError, e, MessageBoxButton.OK, Images.Error);

                // unload image file and abort
                imageFile.Unload();
                return false;
            }

            return true;
        }

        #endregion
        #endregion
        #region UpdateGeometry

        /// <summary>
        /// Updates the "Images" tab page with the current map geometry.</summary>
        /// <remarks>
        /// <b>UpdateGeometry</b> assigns the <see cref="PolygonGrid.Element"/> shape of the current
        /// <see cref="AreaSection.MapGrid"/> to the hosted <see cref="ImageListBox"/>. The contents
        /// of the list box remain otherwise unchanged.</remarks>

        public void UpdateGeometry() {
            if (ImageList != null)
                ImageList.Polygon = MasterSection.Instance.Areas.MapGrid.Element;
        }

        #endregion
        #region Event Handlers
        #region OnColorChange

        /// <summary>
        /// Handles the <see cref="ButtonBase.Click"/> event for the "Change Color" <see
        /// cref="Button"/>.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="RoutedEventArgs"/> object containing event data.</param>
        /// <remarks><para>
        /// <b>OnColorChange</b> displays a <see cref="CustomColorDialog"/> allowing the user to
        /// change the masking color.
        /// </para><para>
        /// If the user made any changes, <b>OnColorChange</b> sets the <see
        /// cref="ImageSection.MaskColor"/> of the current <see cref="ImageSection"/> to the new
        /// masking color, calls <see cref="Initialize"/> to reload all image file bitmaps and to
        /// redisplay all images with the new masking color, and sets the <see
        /// cref="SectionTabItem.DataChanged"/> flag.</para></remarks>

        private void OnColorChange(object sender, RoutedEventArgs args) {
            args.Handled = true;
            ImageSection images = MasterSection.Instance.Images;

            // retrieve masking color and let user change it
            Color maskColor = images.MaskColor;
            bool result = CustomColorDialog.Show(MainWindow.Instance, ref maskColor);

            // update color and brodcast changes, if any
            if (result && images.MaskColor != maskColor) {
                images.MaskColor = maskColor;
                Initialize();
                SectionTab.DataChanged = true;
            }
        }

        #endregion
        #region OnFileActivate

        /// <summary>
        /// Handles the <see cref="Control.MouseDoubleClick"/> event for a <see
        /// cref="ListViewItem"/> of the "Image File" <see cref="ListView"/>.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="MouseButtonEventArgs"/> object containing event data.</param>
        /// <remarks><para>
        /// <b>OnFileActivate</b> shows an error message if the <see cref="ImageFile.Bitmap"/>
        /// property of the double-clicked item in the "Image File" list view is a null reference.
        /// </para><para>
        /// Otherwise, <b>OnFileActivate</b> displays an independent form containing a <see
        /// cref="ScrollViewer"/> that shows the image file bitmap.</para></remarks>

        private void OnFileActivate(object sender, MouseButtonEventArgs args) {
            args.Handled = true;

            // retrieve double-clicked item, if any
            var source = args.OriginalSource as DependencyObject;
            var item = ItemsControl.ContainerFromElement(FileList, source) as ListViewItem;
            if (item == null) return;

            // retrieve corresponding image file, if any
            ImageFile file = item.Content as ImageFile;
            if (file == null) return;

            // abort if no image file bitmap present
            if (file.Bitmap == null) {
                string message = String.Format(ApplicationInfo.Culture,
                    Global.Strings.DialogImageError, file.Path);

                MessageBox.Show(MainWindow.Instance, message,
                    Global.Strings.TitleImageError, MessageBoxButton.OK, MessageBoxImage.Error);

                return;
            }

            Window window = null;
            try {
                // show image file bitmap in separate window
                window = new ShowImage(file.Bitmap);
                window.Title = file.Path;
                window.Icon = MainWindow.Instance.Icon;
                window.Show();
            }
            catch (Exception e) {
                string message = String.Format(ApplicationInfo.Culture,
                    Global.Strings.DialogImageError, file.Path);

                MessageDialog.Show(MainWindow.Instance, message,
                    Global.Strings.TitleImageError, e, MessageBoxButton.OK, Images.Error);

                if (window != null) window.Close();
            }
        }

        #endregion
        #region OnFileAdd

        /// <summary>
        /// Handles the <see cref="ButtonBase.Click"/> event for the "Add File" <see
        /// cref="Button"/>.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="RoutedEventArgs"/> object containing event data.</param>
        /// <remarks><para>
        /// <b>OnFileAdd</b> displays a <see cref="Dialog.ChangeIdentifier"/> dialog, followed by an
        /// <see cref="FileDialogs.OpenImageDialog"/>, allowing the user to define a new image file.
        /// </para><para>
        /// If the user confirmed both dialogs, <b>OnFileAdd</b> adds the new image file to the
        /// "Image File" list view and to the current <see cref="ImageSection"/>, redisplays all
        /// images, and sets the <see cref="SectionTabItem.DataChanged"/> flag.</para></remarks>

        private void OnFileAdd(object sender, RoutedEventArgs args) {
            args.Handled = true;

            // ask user for new image file ID
            var files = MasterSection.Instance.Images.ImageFiles;
            var dialog = new Dialog.ChangeIdentifier("file-id",
                Global.Strings.TitleImageFileIdEnter, files.ContainsKey, false);
            dialog.Owner = MainWindow.Instance;
            if (dialog.ShowDialog() != true) return;

            // retrieve new image file ID
            string id = String.Intern(dialog.Identifier);

            // ask user to select an existing image file
            RootedPath path = FileDialogs.OpenImageDialog(null);

            // return immediately if user cancelled
            if (path.IsEmpty) return;

            // construct new image file
            ImageFile file = new ImageFile(id, path.AbsolutePath);

            // try to load image file from disk
            if (!LoadImageFile(file, MasterSection.Instance.Images.MaskColor))
                return;

            // add image file to section table
            files.Add(id, file);

            // update list view and select new item
            FileList.Items.Refresh();
            FileList.SelectAndShow(file);

            // broadcast data changes
            ImageList.Redraw();
            EnableListButtons();
            SectionTab.DataChanged = true;
        }

        #endregion
        #region OnFileChange

        /// <summary>
        /// Handles the <see cref="ButtonBase.Click"/> event for the "Change Path" <see
        /// cref="Button"/>.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="RoutedEventArgs"/> object containing event data.</param>
        /// <remarks><para>
        /// <b>OnFileChange</b> displays an <see cref="FileDialogs.OpenImageDialog"/> for the first
        /// selected item in the "Image File" list view.
        /// </para><para>
        /// If the user made any changes, <b>OnFileChange</b> propagates them to the current <see
        /// cref="ImageSection"/>, redisplays all class images, and sets the <see
        /// cref="SectionTabItem.DataChanged"/> flag.</para></remarks>

        private void OnFileChange(object sender, RoutedEventArgs args) {
            args.Handled = true;

            // retrieve selected image, if any
            ImageFile file = FileList.SelectedItem as ImageFile;
            if (file == null) return;

            // ask user to select an existing image file
            RootedPath path = FileDialogs.OpenImageDialog(file.Path);

            // return immediately if user cancelled
            if (path.IsEmpty) return;

            // construct new image file object
            ImageFile newFile = new ImageFile(file.Id, path.AbsolutePath);

            // try to load image file from disk
            var imageSection = MasterSection.Instance.Images;
            if (!LoadImageFile(newFile, imageSection.MaskColor))
                return;

            // replace image file in scenario
            file.Unload();
            Debug.Assert(file.Id == newFile.Id);
            imageSection.ImageFiles[newFile.Id] = newFile;

            // broadcast data changes
            FileList.Items.Refresh();
            ImageList.Redraw();
            SectionTab.DataChanged = true;
        }

        #endregion
        #region OnFileId

        /// <summary>
        /// Handles the <see cref="ButtonBase.Click"/> event for the "Change ID" <see
        /// cref="Button"/> that is associated with the "Image File" list view.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="RoutedEventArgs"/> object containing event data.</param>
        /// <remarks><para>
        /// <b>OnFileId</b> displays a <see cref="Dialog.ChangeIdentifier"/> dialog for the first
        /// selected item in the "Image File" list view.
        /// </para><para>
        /// If the user made any changes, <b>OnFileId</b> propagates them to the current <see
        /// cref="ImageSection"/>, redisplays all images, and sets the <see
        /// cref="SectionTabItem.DataChanged"/> flag.</para></remarks>

        private void OnFileId(object sender, RoutedEventArgs args) {
            args.Handled = true;

            // retrieve selected image file, if any
            ImageFile file = FileList.SelectedItem as ImageFile;
            if (file == null) return;

            // let user enter new image file ID
            var files = MasterSection.Instance.Images.ImageFiles;
            var dialog = new Dialog.ChangeIdentifier(file.Id,
                Global.Strings.TitleImageIdChange, files.ContainsKey, true);
            dialog.Owner = MainWindow.Instance;
            if (dialog.ShowDialog() != true) return;

            // retrieve new image file ID
            string id = String.Intern(dialog.Identifier);

            // change existing ID references
            if (!SectionTabItem.ProcessAllIdentifiers(files, file.Id, id))
                return;

            // broadcast data changes
            FileList.Items.Refresh();
            SectionTab.DataChanged = true;
        }

        #endregion
        #region OnFileRemove

        /// <summary>
        /// Handles the <see cref="ButtonBase.Click"/> event for the "Remove File" <see
        /// cref="Button"/>.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="RoutedEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnFileRemove</b> removes the first selected item in the "Image File" list view from
        /// that list view and from the current <see cref="ImageSection"/>, redisplays all images,
        /// and sets the <see cref="SectionTabItem.DataChanged"/> flag.</remarks>

        private void OnFileRemove(object sender, RoutedEventArgs args) {
            args.Handled = true;

            // retrieve selected image file, if any
            int index = FileList.SelectedIndex;
            if (index < 0) return;
            ImageFile file = (ImageFile) FileList.Items[index];

            // delete existing ID references
            var files = MasterSection.Instance.Images.ImageFiles;
            if (!SectionTabItem.ProcessAllIdentifiers(files, file.Id, null))
                return;

            file.Unload();

            // select item in the same position
            FileList.Items.Refresh();
            if (FileList.Items.Count > 0)
                FileList.SelectAndShow(Math.Min(FileList.Items.Count - 1, index));

            // broadcast data changes
            ImageList.Redraw();
            EnableListButtons();
            SectionTab.DataChanged = true;
        }

        #endregion
        #region OnFileWidthChanged

        /// <summary>
        /// Handles changes to the <see cref="FrameworkElement.ActualWidth"/> property of the "File"
        /// <see cref="ListView"/>.</summary>
        /// <param name="sender">
        /// The <see cref="ListView"/> control sending the event.</param>
        /// <param name="args">
        /// An <see cref="EventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnFileWidthChanged</b> resizes both columns of the "File" list view so that each
        /// occupies the same share of the current list view width.</remarks>

        private void OnFileWidthChanged(object sender, EventArgs args) {

            double width = (FileList.ActualWidth - 28) / 2.0;
            if (width > 0) {
                FileIdColumn.Width = width;
                FilePathColumn.Width = width;
            }
        }

        #endregion
        #region OnImageActivate

        /// <summary>
        /// Handles the <see cref="Control.MouseDoubleClick"/> event for an <see
        /// cref="ImageListBoxItem"/> of the <see cref="ImageListBox"/>.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="MouseButtonEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnImageActivate</b> calls <see cref="ChangeImage"/> with the double-clicked item in
        /// the <see cref="ImageListBox"/>.</remarks>

        private void OnImageActivate(object sender, MouseButtonEventArgs args) {
            args.Handled = true;

            // retrieve double-clicked item, if any
            var source = args.OriginalSource as DependencyObject;
            var item = ItemsControl.ContainerFromElement(ImageList, source) as ImageListBoxItem;
            if (item != null) ChangeImage(ImageList.Items.IndexOf(item));
        }

        #endregion
        #region OnImageAdd

        /// <summary>
        /// Handles the <see cref="ButtonBase.Click"/> event for the "Add Image" <see
        /// cref="Button"/>.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="RoutedEventArgs"/> object containing event data.</param>
        /// <remarks><para>
        /// <b>OnImageAdd</b> shows an error message if the "Image File" list view is empty.
        /// Otherwise, <b>OnImageAdd</b> displays a <see cref="Dialog.ChangeIdentifier"/> dialog,
        /// followed by a <see cref="Dialog.ChangeImage"/> dialog, allowing the user to define a new
        /// image. The new image copies the properties of the selected image, if any; otherwise, it
        /// is created with default properties.
        /// </para><para>
        /// If the user confirmed both dialogs, <b>OnImageAdd</b> adds the new image to the <see
        /// cref="ImageListBox"/> and to the current <see cref="ImageSection"/>, and sets the <see
        /// cref="SectionTabItem.DataChanged"/> flag.</para></remarks>

        private void OnImageAdd(object sender, RoutedEventArgs args) {
            args.Handled = true;

            // abort if there are no image files
            if (FileList.Items.Count == 0) {
                MessageBox.Show(MainWindow.Instance,
                    Global.Strings.DialogImageFileNone, Global.Strings.TitleImageFileNone,
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // ask user for new image ID
            var images = MasterSection.Instance.Images.Collection;
            var dialog = new Dialog.ChangeIdentifier("image-id",
                Global.Strings.TitleImageIdEnter, images.ContainsKey, false);
            dialog.Owner = MainWindow.Instance;
            if (dialog.ShowDialog() != true) return;

            // retrieve new image ID
            string id = String.Intern(dialog.Identifier);

            // create new image based on selected image, if any
            var selection = ImageList.SelectedItem as ImageListBoxItem;
            EntityImage image = (selection == null ? new EntityImage() :
                (EntityImage) ((EntityImage) selection.Content).Clone());
            image.Id = id;

            // let user make changes to new image
            var imageDialog = new Dialog.ChangeImage(image) { Owner = MainWindow.Instance };
            if (imageDialog.ShowDialog() != true) return;

            // add image to section table
            images.Add(id, image);

            // update list box and select new item
            var item = new ImageListBoxItem(image);
            int index = ImageList.Insert(item);
            ImageList.SelectAndShow(index);

            // broadcast data changes
            EnableListButtons();
            SectionTab.DataChanged = true;
        }

        #endregion
        #region OnImageChange

        /// <summary>
        /// Handles the <see cref="ButtonBase.Click"/> event for the "Change Image" <see
        /// cref="Button"/>.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="RoutedEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnImageChange</b> calls <see cref="ChangeImage"/> with the first selected item in the
        /// <see cref="ImageListBox"/>.</remarks>

        private void OnImageChange(object sender, RoutedEventArgs args) {
            args.Handled = true;
            ChangeImage(ImageList.SelectedIndex);
        }

        #endregion
        #region OnImageId

        /// <summary>
        /// Handles the <see cref="ButtonBase.Click"/> event for the "Change ID" <see
        /// cref="Button"/> that is associated with the <see cref="ImageListBox"/>.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="RoutedEventArgs"/> object containing event data.</param>
        /// <remarks><para>
        /// <b>OnImageId</b> displays a <see cref="Dialog.ChangeIdentifier"/> dialog for the
        /// selected item in the <see cref="ImageListBox"/>.
        /// </para><para>
        /// If the user made any changes, <b>OnImageId</b> propagates them to the current <see
        /// cref="ImageSection"/>, redisplays all images, and sets the <see
        /// cref="SectionTabItem.DataChanged"/> flag.</para></remarks>

        private void OnImageId(object sender, RoutedEventArgs args) {
            args.Handled = true;

            // retrieve selected image, if any
            int index = ImageList.SelectedIndex;
            if (index < 0) return;
            var item = (ImageListBoxItem) ImageList.SelectedItem;
            var image = (EntityImage) item.Content;

            // let user enter new image ID
            var images = MasterSection.Instance.Images.Collection;
            var dialog = new Dialog.ChangeIdentifier(image.Id,
                Global.Strings.TitleImageIdChange, images.ContainsKey, true);
            dialog.Owner = MainWindow.Instance;
            if (dialog.ShowDialog() != true) return;

            // retrieve new image ID
            string id = String.Intern(dialog.Identifier);

            // change existing ID references
            if (!SectionTabItem.ProcessAllIdentifiers(images, image.Id, id))
                return;

            // change item in Image list
            ImageList.Items.RemoveAt(index);
            item = new ImageListBoxItem(image);
            index = ImageList.Insert(item);
            ImageList.Items.Refresh();
            ImageList.SelectAndShow(index);

            // broadcast data changes
            SectionTab.DataChanged = true;
        }

        #endregion
        #region OnImageRemove

        /// <summary>
        /// Handles the <see cref="ButtonBase.Click"/> event for the "Remove Image" <see
        /// cref="Button"/>.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="RoutedEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnImageRemove</b> removes the selected item in the <see cref="ImageListBox"/> from
        /// that list box and from the current <see cref="ImageSection"/>, and sets the <see
        /// cref="SectionTabItem.DataChanged"/> flag.</remarks>

        private void OnImageRemove(object sender, RoutedEventArgs args) {
            args.Handled = true;

            // retrieve selected image, if any
            int index = ImageList.SelectedIndex;
            if (index < 0) return;
            var item = (ImageListBoxItem) ImageList.SelectedItem;
            var image = (EntityImage) item.Content;

            // delete existing ID references
            var images = MasterSection.Instance.Images.Collection;
            if (!SectionTabItem.ProcessAllIdentifiers(images, image.Id, null))
                return;

            // select item in the same position
            ImageList.Items.RemoveAt(index);
            ImageList.Items.Refresh();
            if (ImageList.Items.Count > 0)
                ImageList.SelectAndShow(Math.Min(ImageList.Items.Count - 1, index));

            // broadcast data changes
            EnableListButtons();
            SectionTab.DataChanged = true;
        }

        #endregion
        #region OnGraphicsInfo

        /// <summary>
        /// Handles the <see cref="ButtonBase.Click"/> event for the "Graphics Information" <see
        /// cref="Button"/>.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="RoutedEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnGraphicsInfo</b> displays a <see cref="Dialog.ChangeInformation"/> dialog for the
        /// <see cref="ImageSection.Information"/> block of the current <see cref="ImageSection"/>,
        /// and sets the <see cref="SectionTabItem.DataChanged"/> flag if the user made any changes.
        /// </remarks>

        private void OnGraphicsInfo(object sender, RoutedEventArgs args) {
            args.Handled = true;

            // retrieve current graphics information
            Information info = MasterSection.Instance.Images.Information;

            // show dialog and let user make changes
            var dialog = new Dialog.ChangeInformation(info, Global.Strings.TitleChangeGraphics);
            dialog.Owner = MainWindow.Instance;
            dialog.ShowDialog();

            // broadcast data changes, if any
            if (dialog.DataChanged)
                SectionTab.DataChanged = true;
        }

        #endregion
        #region OnTransparency

        /// <summary>
        /// Handles the <see cref="ButtonBase.Click"/> event for the "Add Transparency" <see
        /// cref="CheckBox"/>.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="RoutedEventArgs"/> object containing event data.</param>
        /// <remarks><para>
        /// Checking the "Add Transparency" check box calls <see cref="OnColorChange"/> to let the
        /// user select a valid masking color. The check box is cleared again if the <see
        /// cref="ImageSection.MaskColor"/> of the current <see cref="ImageSection"/> remains at
        /// <see cref="Colors.Transparent"/>.
        /// </para><para>
        /// Unchecking "Add Transparency" resets <see cref="ImageSection.MaskColor"/> to <see
        /// cref="Colors.Transparent"/>, calls <see cref="Initialize"/> to reload all image file
        /// bitmaps and redisplay all images, and sets the <see cref="SectionTabItem.DataChanged"/>
        /// flag.</para></remarks>

        private void OnTransparency(object sender, RoutedEventArgs args) {
            args.Handled = true;
            ImageSection images = MasterSection.Instance.Images;

            if (ColorToggle.IsChecked == true) {

                // select valid masking color
                if (images.MaskColor == Colors.Transparent)
                    OnColorChange(sender, args);

                // uncheck option if no masking color selected
                if (images.MaskColor == Colors.Transparent)
                    ColorToggle.IsChecked = false;

                return;
            }

            // reset to embedded color and broadcast changes
            if (images.MaskColor != Colors.Transparent) {
                images.MaskColor = Colors.Transparent;
                Initialize();
                SectionTab.DataChanged = true;
            }
        }

        #endregion
        #endregion
        #region IEditorTabContent Members
        #region Section

        /// <summary>
        /// Gets the <see cref="ScenarioSection"/> managed by the tab page.</summary>
        /// <value>
        /// The constant value <see cref="ScenarioSection.Images"/>, indicating the Hexkit scenario
        /// section managed by the "Images" tab page.</value>

        public ScenarioSection Section {
            get { return ScenarioSection.Images; }
        }

        #endregion
        #region SectionTab

        /// <summary>
        /// Gets or sets the <see cref="SectionTabItem"/> for the tab page.</summary>
        /// <value>
        /// The <see cref="SectionTabItem"/> that contains the <see cref="ImagesTabContent"/>
        /// control, i.e. the "Images" tab page of the Hexkit Editor application.</value>
        /// <exception cref="ArgumentNullException">
        /// The property is set to a null reference.</exception>
        /// <exception cref="InvalidOperationException">
        /// The property is set more than once.</exception>

        public SectionTabItem SectionTab {
            [DebuggerStepThrough]
            get { return this._sectionTab; }
            set {
                if (value == null)
                    ThrowHelper.ThrowArgumentNullException("value");
                if (this._sectionTab != null)
                    ThrowHelper.ThrowInvalidOperationException(Tektosyne.Strings.PropertySetOnce);

                this._sectionTab = value;
            }
        }

        #endregion
        #region Initialize

        /// <summary>
        /// Initializes the section-specific controls of the tab page.</summary>
        /// <remarks><para>
        /// <b>Initialize</b> initializes all controls that are specific to the "Images" tab page.
        /// </para><para>
        /// <b>Initialize</b> attempts to load all disk files defined by the items of the "Image
        /// File" list view but allows this operation to fail for any or all list view items. The
        /// user is notified of the failure. Clients should take care not to assume that any given
        /// <see cref="ImageFile"/> bitmap is valid.</para></remarks>

        public void Initialize() {
            ImageSection images = MasterSection.Instance.Images;

            // check custom color option if not transparent
            ColorToggle.IsChecked = (images.MaskColor != Colors.Transparent);

            // reload image files from scenario section
            if (FileList.ItemsSource != images.ImageFiles.Values)
                FileList.ItemsSource = images.ImageFiles.Values;
            else
                FileList.Items.Refresh();

            // select first item by default
            if (FileList.Items.Count > 0)
                FileList.SelectedIndex = 0;

            // try loading image files from disk
            foreach (ImageFile file in images.ImageFiles.Values)
                LoadImageFile(file, images.MaskColor);

            // acquire polygon geometry
            UpdateGeometry();

            // remember selected index in image list
            int selectedIndex = ImageList.SelectedIndex;

            // reload images from scenario section
            ImageList.Items.Clear();
            foreach (EntityImage image in images.Collection.Values)
                ImageList.Insert(new ImageListBoxItem(image));
            ImageList.Items.Refresh();

            // reselect previously selected index, if possible
            if (selectedIndex >= 0 && selectedIndex < ImageList.Items.Count)
                ImageList.SelectAndShow(selectedIndex);
            else if (ImageList.Items.Count > 0)
                ImageList.SelectedIndex = 0;

            // update list button status
            EnableListButtons();
        }

        #endregion
        #endregion
    }
}
