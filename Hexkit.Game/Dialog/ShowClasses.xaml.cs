using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using Tektosyne;
using Tektosyne.Windows;

using Hexkit.Global;
using Hexkit.Graphics;
using Hexkit.Scenario;
using Hexkit.World;

namespace Hexkit.Game.Dialog {

    /// <summary>
    /// Shows a dialog with information on all entity classes in the game.</summary>
    /// <remarks>
    /// Please refer to the "Entity Classes" page of the "Game Dialogs" section in the application
    /// help file for details on this dialog.</remarks>

    public partial class ShowClasses: Window {
        #region ShowClasses()

        /// <overloads>
        /// Initializes a new instance of the <see cref="ShowClasses"/> class.</overloads>
        /// <summary>
        /// Initializes a new instance of the <see cref="ShowClasses"/> class with the the first
        /// <see cref="UnitClass"/> initially selected.</summary>

        public ShowClasses() {
            InitializeComponent();

            // create tile buffer for entity class preview
            this._tileBuffer = MapViewManager.Instance.CreateTileDrawBuffer(2, 2);
            EntityPreview.Source = this._tileBuffer;

            // adjust column width of Entity list view
            DependencyPropertyDescriptor.FromProperty(
                ListView.ActualWidthProperty, typeof(ListView))
                .AddValueChanged(EntityList, OnEntityWidthChanged);
        }

        #endregion
        #region ShowClasses(Entity)

        /// <summary>
        /// Initializes a new instance of the <see cref="ShowClasses"/> class with the specified
        /// <see cref="Entity"/>.</summary>
        /// <param name="entity">
        /// The <see cref="Entity"/> whose <see cref="Entity.EntityClass"/> and <see
        /// cref="Entity.FrameOffset"/> to select initially.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="entity"/> is a null reference.</exception>
        /// <remarks>
        /// The <see cref="Entity.FrameOffset"/> of the specified <paramref name="entity"/> is 
        /// ignored if its <see cref="Entity.EntityClass"/> does not equal its <see
        /// cref="Entity.DisplayClass"/>.</remarks>

        public ShowClasses(Entity entity): this() {
            if (entity == null)
                ThrowHelper.ThrowArgumentNullException("entity");

            this._entityClass = entity.EntityClass;
            if (entity.EntityClass == entity.DisplayClass)
                this._frameOffset = Math.Abs(entity.FrameOffset) % entity.EntityClass.FrameCount;
        }

        #endregion
        #region ShowClasses(EntityClass)

        /// <summary>
        /// Initializes a new instance of the <see cref="ShowClasses"/> class with the specified
        /// initially selected <see cref="EntityClass"/>.</summary>
        /// <param name="entityClass">
        /// The <see cref="EntityClass"/> to select initially.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="entityClass"/> is a null reference.</exception>

        public ShowClasses(EntityClass entityClass): this() {
            if (entityClass == null)
                ThrowHelper.ThrowArgumentNullException("entityClass");

            this._entityClass = entityClass;
        }

        #endregion
        #region Private Fields

        // construction parameters
        private readonly EntityClass _entityClass = null;
        private readonly int _frameOffset = 0;

        // tile buffer for entity class preview
        private readonly RenderTargetBitmap _tileBuffer;

        #endregion
        #region Private Methods
        #region SelectEntity

        /// <summary>
        /// Checks the "Category" <see cref="RadioButton"/> that corresponds to the specified <see
        /// cref="EntityClass"/>, which is also selected in the "Entity Class" <see
        /// cref="ListView"/>.</summary>
        /// <param name="entityClass">
        /// The <see cref="EntityClass"/> to select.</param>
        /// <exception cref="InvalidEnumArgumentException">
        /// <paramref name="entityClass"/> specifies an invalid <see cref="EntityClass.Category"/>
        /// value.</exception>
        /// <remarks>
        /// Checking a "Category" radio button automatically shows the corresponding <see
        /// cref="EntityClass"/> objects in the "Entity Class" list view, via <see
        /// cref="OnCategoryChecked"/>.</remarks>

        private void SelectEntity(EntityClass entityClass) {
            switch (entityClass.Category) {

                case EntityCategory.Unit:
                    UnitToggle.IsChecked = true;
                    break;

                case EntityCategory.Terrain:
                    TerrainToggle.IsChecked = true;
                    break;

                case EntityCategory.Effect:
                    EffectToggle.IsChecked = true;
                    break;

                case EntityCategory.Upgrade:
                    UpgradeToggle.IsChecked = true;
                    break;

                default:
                    ThrowHelper.ThrowInvalidEnumArgumentException("entityClass.Category",
                        (int) entityClass.Category, typeof(EntityCategory));
                    break;
            }

            // list view was initialized by checking radio button
            EntityList.SelectAndShow(entityClass);
        }

        #endregion
        #region ShowFrame

        /// <summary>
        /// Shows the specified frame of the specified <see cref="EntityClass"/> in the "Image" <see
        /// cref="GroupBox"/>.</summary>
        /// <param name="entityClass">
        /// The <see cref="EntityClass"/> to show.</param>
        /// <param name="frame">
        /// The offset to add to the <see cref="EntityClass.FrameIndex"/> of the specified <paramref
        /// name="entityClass"/>.</param>

        private void ShowFrame(EntityClass entityClass, int frame) {

            // determine catalog index of specified frame
            int index = entityClass.FrameIndex + (frame % entityClass.FrameCount);

            // render catalog tile to drawing visual
            DrawingVisual visual = new DrawingVisual();
            DrawingContext context = visual.RenderOpen();
            MapViewManager.Instance.DrawTile(context, index);
            context.Close();

            // render drawing visual to tile buffer
            this._tileBuffer.Clear();
            this._tileBuffer.Render(visual);
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
        /// cref="ShowClasses"/> dialog.</remarks>

        private void HelpExecuted(object sender, ExecutedRoutedEventArgs args) {
            args.Handled = true;
            ApplicationUtility.ShowHelp("DlgShowClasses.html");
        }

        #endregion
        #region OnCategoryChecked

        /// <summary>
        /// Handles the <see cref="ToggleButton.Checked"/> event for the "Category" <see
        /// cref="RadioButton"/> controls.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="RoutedEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnCategoryChecked</b> updates the "Entity Class" list view to reflect the selected
        /// radio button and selects the first item, if any, which automatically updates all other
        /// dialog controls.</remarks>

        private void OnCategoryChecked(object sender, RoutedEventArgs args) {
            args.Handled = true;
            IList<EntityClass> entities = null;

            if (UnitToggle.IsChecked == true)
                entities = MasterSection.Instance.Entities.Units.Values;
            else if (TerrainToggle.IsChecked == true)
                entities = MasterSection.Instance.Entities.Terrains.Values;
            else if (EffectToggle.IsChecked == true)
                entities = MasterSection.Instance.Entities.Effects.Values;
            else if (UpgradeToggle.IsChecked == true)
                entities = MasterSection.Instance.Entities.Upgrades.Values;
            else {
                Debug.Fail("ShowClasses.OnCategoryChecked: No Category button checked.");
                entities = MasterSection.Instance.Entities.Units.Values;
            }

            // show classes of selected category
            EntityList.ItemsSource = entities;

            // select first list entry, if any
            if (EntityList.Items.Count > 0)
                EntityList.SelectedIndex = 0;
            else {
                // inform user why list is empty
                EntityInfo.Text = Global.Strings.InfoEntityCategoryEmpty;
            }
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
        /// selecting either the first <see cref="UnitClass"/> in the "Entity Class" list view, or
        /// the entity class that was supplied to the constructor, if any.</para></remarks>

        protected override void OnContentRendered(EventArgs args) {
            base.OnContentRendered(args);

            if (this._entityClass == null) {
                // select first unit class
                UnitToggle.IsChecked = true;
            } else {
                // select specified entity class
                SelectEntity(this._entityClass);
            }
        }

        #endregion
        #region OnEntitySelected

        /// <summary>
        /// Handles the <see cref="Selector.SelectionChanged"/> event for the "Entity Class" <see
        /// cref="ListView"/>.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="SelectionChangedEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnEntitySelected</b> updates all dialog controls to reflect the selected item in the
        /// "Entity Class" list view.</remarks>

        private void OnEntitySelected(object sender, SelectionChangedEventArgs args) {
            args.Handled = true;

            // clear data display
            this._tileBuffer.Clear();
            EntityInfo.Clear();
            PropertyList.ShowEntityClass(null);

            // retrieve selected item, if any
            EntityClass entityClass = EntityList.SelectedItem as EntityClass;
            if (entityClass == null) return;

            // set new frame index range
            FrameScrollBar.Minimum = 0;
            FrameScrollBar.Maximum = Math.Max(entityClass.FrameCount - 1, 0);
            FrameScrollBar.SmallChange = 1;

            // show first or specified frame
            int value = (entityClass == this._entityClass ? this._frameOffset : 0);

            // change index, or redraw manually if unchanged
            if (FrameScrollBar.Value != value)
                FrameScrollBar.Value = value;
            else
                ShowFrame(entityClass, value);

            // show entity class properties
            PropertyList.ShowEntityClass(entityClass);

            // show associated informational text
            EntityInfo.Text = String.Join(Environment.NewLine, entityClass.Paragraphs);
        }

        #endregion
        #region OnEntityWidthChanged

        /// <summary>
        /// Handles changes to the <see cref="FrameworkElement.ActualWidth"/> property of the
        /// "Entity Class" <see cref="ListView"/>.</summary>
        /// <param name="sender">
        /// The <see cref="ListView"/> control sending the event.</param>
        /// <param name="args">
        /// An <see cref="EventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnEntityWidthChanged</b> resizes the only column of the "Entity Class" list view to
        /// the current list view width.</remarks>

        private void OnEntityWidthChanged(object sender, EventArgs args) {

            double width = EntityList.ActualWidth - 28;
            if (width > 0) EntityColumn.Width = width;
        }

        #endregion
        #region OnFrameChanged

        /// <summary>
        /// Handles the <see cref="RangeBase.ValueChanged"/> event for the "Image" <see
        /// cref="ScrollBar"/>.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="RoutedPropertyChangedEventArgs{Double}"/> object containing event data.
        /// </param>
        /// <remarks>
        /// <b>OnFrameChanged</b> updates the "Image" group box with the selected image frame of the
        /// selected item in the "Entity Class" list view, if any.</remarks>

        private void OnFrameChanged(object sender, RoutedPropertyChangedEventArgs<Double> args) {

            // retrieve selected entity class, if any
            EntityClass entityClass = EntityList.SelectedItem as EntityClass;
            if (entityClass == null) return;

            // show selected image frame for entity class
            int frame = Fortran.NInt(FrameScrollBar.Value);
            ShowFrame(entityClass, frame);
        }

        #endregion
        #endregion
    }
}
