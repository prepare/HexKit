using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

using Tektosyne;
using Tektosyne.Collections;
using Tektosyne.Geometry;
using Tektosyne.Windows;

using Hexkit.Global;
using Hexkit.Graphics;
using Hexkit.World;

namespace Hexkit.Game.Dialog {
    #region Type Aliases

    using EntityList = KeyedList<String, Entity>;
    
    // attacking unit and attack check box
    using UnitListItem = Tuple<Entity, Boolean>;

    #endregion

    /// <summary>
    /// Shows a dialog allowing the <see cref="WorldState.ActiveFaction"/> to attack from a
    /// specified <see cref="Site"/>.</summary>
    /// <remarks>
    /// Please refer to the "Attack From Site" page of the "Game Dialogs" section in the application
    /// help file for details on this dialog.</remarks>

    public partial class AttackFromSite: Window, IDisposable {
        #region AttackFromSite(PointI, EntityList)

        /// <summary>
        /// Initializes a new instance of the <see cref="AttackFromSite"/> class with the specified
        /// source location and eligible units.</summary>
        /// <param name="source">
        /// The coordinates of the <see cref="Site"/> from which to attack.</param>
        /// <param name="eligible">
        /// A <see cref="EntityList"/> containing all <see cref="Unit"/> objects that may be
        /// selected.</param>
        /// <exception cref="ArgumentException">
        /// <paramref name="eligible"/> contains units that are not placed on the <paramref
        /// name="source"/> site.</exception>
        /// <exception cref="ArgumentNullOrEmptyException">
        /// <paramref name="eligible"/> is a null reference or an empty collection.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="source"/> is not a valid map location.</exception>
        /// <remarks><para>
        /// <b>AttackFromSite</b> sets the <see cref="Window.DialogResult"/> to <c>false</c> if the
        /// user did not select a valid target site and at least one unit to participate in the
        /// attack.
        /// </para><para>
        /// Otherwise, the selected units are stored in the <see cref="Units"/> collection, and the
        /// selected target site is stored in the <see cref="Target"/> property.</para></remarks>

        public AttackFromSite(PointI source, EntityList eligible) {
            if (eligible == null || eligible.Count == 0)
                ThrowHelper.ThrowArgumentNullOrEmptyException("eligible");

            // retrieve current world state
            WorldState world = Session.Instance.WorldState;

            // check source site coordinates
            if (!Finder.MapGrid.Contains(source))
                ThrowHelper.ThrowArgumentOutOfRangeException(
                    "source", source, Tektosyne.Strings.ArgumentCoordinatesInvalid);

            // check placement of eligible units
            foreach (Entity unit in eligible)
                if (unit.Site == null || unit.Site.Location != source)
                    ThrowHelper.ThrowArgumentException(
                        "eligible", Global.Strings.ArgumentContainsUnitsNotInSource);

            this._source = source;
            this._eligible = eligible;
            Target = Site.InvalidLocation;

            InitializeComponent();
            Title += Site.Format(source);
            this._lossesFormat = (string) LossesInfo.Content;

            // adjust column width of Unit list view
            DependencyPropertyDescriptor.FromProperty(
                ListView.ActualWidthProperty, typeof(ListView))
                .AddValueChanged(UnitList, OnUnitWidthChanged);

            // create map view with default properties
            this._mapView = MapViewManager.Instance.CreateView(
                "attackFromSite", world, MapViewHost, OnMapMouseDown, null);

            // attach handler to update dialog controls
            this._mapView.SelectedSiteChanged += ((sender, args) => ShowSelection(false));

            // highlight all reachable sites
            this._mapView.SelectedSite = source;
            this._mapView.SelectedRegion = GetAttackRegion();

            // show all eligible units in source site
            Site site = world.Sites[source.X, source.Y];
            foreach (Entity unit in site.Units)
                if (this._eligible.Contains(unit)) {
                    this._selected.Add(unit);
                    UnitList.Items.Add(new UnitListItem(unit, true));
                }

            // select first unit if present
            if (UnitList.Items.Count > 0) {
                UnitList.SelectedIndex = 0;
                ShowSelection(true);
            }
        }

        #endregion
        #region Private Fields

        // construction parameters
        private PointI _source = Site.InvalidLocation;
        private readonly EntityList _eligible;

        // format string for projected losses
        private readonly string _lossesFormat;

        // hosted map view and current selection
        private readonly MapView _mapView;
        private readonly EntityList _selected = new EntityList(true);

        #endregion
        #region Target

        /// <summary>
        /// Gets the coordinates of the <see cref="Site"/> that is the target of the attack.
        /// </summary>
        /// <value>
        /// The coordinates of the <see cref="Site"/> which all <see cref="Units"/> should attack.
        /// </value>
        /// <remarks>
        /// <b>Target</b> returns <see cref="Site.InvalidLocation"/> if the user cancelled the
        /// dialog.</remarks>

        public PointI Target { get; private set; }

        #endregion
        #region Units

        /// <summary>
        /// Gets a list of all units that should participate in the attack.</summary>
        /// <value>
        /// A read-only <see cref="EntityList"/> containing all <see cref="Unit"/> objects that
        /// should participate in the attack.</value>
        /// <remarks>
        /// <b>Units</b> returns a null reference if the user cancelled the dialog. Otherwise, all
        /// collection elements are unique.</remarks>

        public EntityList Units { get; private set; }

        #endregion
        #region Private Methods
        #region GetAttackRegion

        /// <summary>
        /// Gets an array indicating the combined attack range of the currently selected units.
        /// </summary>
        /// <returns>
        /// A two-dimensional <see cref="Array"/> of <see cref="Boolean"/> values indicating which
        /// sites are reachable by the units that are currently selected by the user.</returns>
        /// <remarks><para>
        /// The returned array can be assigned to the <see cref="MapView.SelectedRegion"/> property
        /// of the hosted <see cref="MapView"/>.
        /// </para><para>
        /// <b>GetAttackRegion</b> sets only those elements of the returned array to <c>true</c>
        /// whose corresponding sites can be reached by <em>all</em> currently selected units.
        /// </para></remarks>

        private bool[,] GetAttackRegion() {
            bool[,] region = Finder.MapGrid.CreateArray<Boolean>();

            // return empty region if no units selected
            if (this._selected.Count == 0) return region;

            // retrieve attack range for all selected units
            var targets = Finder.FindAttackTargets(Session.Instance.WorldState, this._selected);

            // mark all reachable coordinates
            int width = region.GetLength(0), height = region.GetLength(1);
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    region[x, y] = targets.Contains(new PointI(x, y));

            return region;
        }

        #endregion
        #region ShowLosses

        /// <summary>
        /// Shows projected losses for attacking and defending units.</summary>
        /// <param name="target">
        /// The coordinates of the <see cref="Site"/> that is the target of the attack.</param>
        /// <remarks><para>
        /// <b>ShowLosses</b> invokes <see cref="Unit.EstimateLosses"/> with all selected units and
        /// updates the "Projected Losses" message with the resulting percentage losses.
        /// </para><para>
        /// <b>ShowLosses</b> shows two zeroes if no units are selected, and a notification if the
        /// specified <paramref name="target"/> is an invalid map location.</para></remarks>

        private void ShowLosses(PointI target) {

            // show notification for invalid selection
            if (!this._mapView.InSelectedRegion(target)) {
                LossesInfo.Visibility = Visibility.Collapsed;
                LossesInfoNone.Visibility = Visibility.Visible;
                return;
            }

            CombatResults results = new CombatResults();
            LossesInfo.Visibility = Visibility.Visible;
            LossesInfoNone.Visibility = Visibility.Collapsed;

            // compute estimated combat losses
            if (this._selected.Count > 0) {
                Unit unit = (Unit) this._selected[0];
                results = unit.EstimateLosses(
                    Session.Instance.WorldState, this._selected, target, true);
            }

            // show attacker & defender losses
            LossesInfo.Content = String.Format(ApplicationInfo.Culture,
                this._lossesFormat, results.AttackerPercent, results.DefenderPercent);
        }

        #endregion
        #region ShowSelection

        /// <summary>
        /// Updates the dialog to reflect the current <see cref="MapView.SelectedSite"/>.</summary>
        /// <param name="updateRegion">
        /// <c>true</c> to first recalculate the <see cref="MapView.SelectedRegion"/> to reflect the
        /// combined attack range of all selected units; <c>false</c> to leave the
        /// <b>SelectedRegion</b> unchanged.</param>
        /// <remarks>
        /// <b>ShowSelection</b> updates the arrows shown in the hosted <see cref="MapView"/>, and
        /// the data shown in the "Projected Losses" message.</remarks>

        private void ShowSelection(bool updateRegion) {

            // update selected region if requested
            if (updateRegion)
                this._mapView.SelectedRegion = GetAttackRegion();

            PointI target = this._mapView.SelectedSite;
            GameUtility.DrawAttackArrows(this._mapView, this._selected, target, true);
            ShowLosses(target);
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
        /// cref="AttackFromSite"/> dialog.</remarks>

        private void HelpExecuted(object sender, ExecutedRoutedEventArgs args) {
            args.Handled = true;
            ApplicationUtility.ShowHelp("DlgAttackFromSite.html");
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
        /// handles the <see cref="Window.Closing"/> event by checking if the user selected at least
        /// one attacking <see cref="Unit"/> and a valid target <see cref="Site"/>.
        /// </para><para>
        /// On success, <b>OnClosing</b> sets the <see cref="Target"/> and <see cref="Units"/>
        /// properties to the user's selections. On failure, <b>OnClosing</b> shows a message box
        /// requesting valid selections and cancels the <see cref="Window.Closing"/> event.
        /// </para></remarks>

        protected override void OnClosing(CancelEventArgs args) {
            base.OnClosing(args);
            if (args.Cancel) return;

            // ignore selection if dialog was cancelled
            if (DialogResult != true) return;

            // check that units are selected
            if (this._selected.Count == 0) {
                MessageBox.Show(this,
                    Global.Strings.DialogUnitUnselected, Global.Strings.TitleSelectionInvalid,
                    MessageBoxButton.OK, MessageBoxImage.Information);
                args.Cancel = true;
                return;
            }

            // check that a target is selected
            if (!this._mapView.InSelectedRegion(this._mapView.SelectedSite)) {
                MessageBox.Show(this,
                    Global.Strings.DialogTargetUnselected, Global.Strings.TitleSelectionInvalid,
                    MessageBoxButton.OK, MessageBoxImage.Information);
                args.Cancel = true;
                return;
            }

            // store user selections
            Target = this._mapView.SelectedSite;
            Units = this._selected.AsReadOnly();
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
        /// centering the hosted <see cref="MapView"/> on the source <see cref="Site"/> that was
        /// supplied to the constructor, if valid.</para></remarks>

        protected override void OnContentRendered(EventArgs args) {
            base.OnContentRendered(args);

            // center on source site if possible
            if (this._mapView != null && this._source != Site.InvalidLocation)
                this._mapView.CenterOn(this._source);
        }

        #endregion
        #region OnStackCheck

        /// <summary>
        /// Handles the <see cref="ButtonBase.Click"/> event for the "Check Stack" <see
        /// cref="Button"/>.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="RoutedEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnStackCheck</b> checks all unchecked items in the "Unit" list view.</remarks>

        private void OnStackCheck(object sender, RoutedEventArgs args) {
            args.Handled = true;
            int index = UnitList.SelectedIndex;

            // check all unchecked items
            for (int i = 0; i < UnitList.Items.Count; i++) {
                UnitListItem item = (UnitListItem) UnitList.Items[i];
                if (!item.Item2)
                    UnitList.Items[i] = new UnitListItem(item.Item1, true);

                // update selection list
                if (!this._selected.Contains(item.Item1))
                    this._selected.Add(item.Item1);
            }

            // update list view & map view
            UnitList.Items.Refresh();
            UnitList.SelectAndShow(index);
            ShowSelection(true);
        }

        #endregion
        #region OnStackUncheck

        /// <summary>
        /// Handles the <see cref="ButtonBase.Click"/> event for the "Uncheck Stack" <see
        /// cref="Button"/>.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="RoutedEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnStackUncheck</b> unchecks all checked items in the "Unit" list view.</remarks>

        private void OnStackUncheck(object sender, RoutedEventArgs args) {
            args.Handled = true;
            int index = UnitList.SelectedIndex;

            // uncheck all checked items
            for (int i = 0; i < UnitList.Items.Count; i++) {
                UnitListItem item = (UnitListItem) UnitList.Items[i];
                if (item.Item2)
                    UnitList.Items[i] = new UnitListItem(item.Item1, false);

                // update selection list
                if (this._selected.Contains(item.Item1))
                    this._selected.Remove(item.Item1);
            }

            // update list view & map view
            UnitList.Items.Refresh();
            UnitList.SelectAndShow(index);
            ShowSelection(true);
        }

        #endregion
        #region OnUnitActivate

        /// <summary>
        /// Handles the <see cref="Control.MouseDoubleClick"/> event for a <see
        /// cref="ListViewItem"/> of the "Unit" <see cref="ListView"/>.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="MouseButtonEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnUnitActivate</b> displays a <see cref="ShowClasses"/> dialog containing information
        /// on the double-clicked item in the "Unit" list view.</remarks>

        private void OnUnitActivate(object sender, MouseButtonEventArgs args) {
            args.Handled = true;

            // retrieve double-clicked item, if any
            var source = args.OriginalSource as DependencyObject;
            var listItem = ItemsControl.ContainerFromElement(UnitList, source) as ListViewItem;
            if (listItem == null) return;
            UnitListItem item = (UnitListItem) listItem.Content;

            // show info dialog for this unit
            var dialog = new ShowClasses(item.Item1) { Owner = this };
            dialog.ShowDialog();
        }

        #endregion
        #region OnUnitChecked

        /// <summary>
        /// Handles the <see cref="ToggleButton.Checked"/> event for the "Unit" <see
        /// cref="ListView"/>.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="RoutedEventArgs"/> object containing event data.</param>
        /// <remarks><para>
        /// <b>OnUnitChecked</b> synchronizes the collection of selected units with the contents of
        /// the "Unit" list view by adding the newly checked unit.
        /// </para><para>
        /// <b>OnUnitChecked</b> then calls <see cref="ShowSelection"/> to recalculate the combined
        /// attack range of all currently selected units, and to update the display accordingly.
        /// </para></remarks>

        private void OnUnitChecked(object sender, RoutedEventArgs args) {
            args.Handled = true;

            // retrieve checked unit, if any
            var listItem = TreeHelper.FindParentListItem(args.Source);
            if (listItem == null) return;
            UnitListItem item = (UnitListItem) listItem.Content;
            int index = UnitList.Items.IndexOf(item);

            // update selection list
            if (!this._selected.Contains(item.Item1))
                this._selected.Add(item.Item1);

            // update list view & map view
            item = new UnitListItem(item.Item1, true);
            UnitList.Items[index] = item;
            UnitList.SelectAndShow(index);

            ShowSelection(true);
        }

        #endregion
        #region OnUnitUnchecked

        /// <summary>
        /// Handles the <see cref="ToggleButton.Unchecked"/> event for the "Unit" <see
        /// cref="ListView"/>.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="RoutedEventArgs"/> object containing event data.</param>
        /// <remarks><para>
        /// <b>OnUnitUnchecked</b> synchronizes the collection of selected units with the contents
        /// of the "Unit" list view by removing the newly unchecked unit.
        /// </para><para>
        /// <b>OnUnitUnchecked</b> then calls <see cref="ShowSelection"/> to recalculate the
        /// combined attack range of all currently selected units, and to update the display
        /// accordingly.</para></remarks>

        private void OnUnitUnchecked(object sender, RoutedEventArgs args) {
            args.Handled = true;

            // retrieve unchecked unit, if any
            var listItem = TreeHelper.FindParentListItem(args.Source);
            if (listItem == null) return;
            UnitListItem item = (UnitListItem) listItem.Content;
            int index = UnitList.Items.IndexOf(item);

            // update selection list
            if (this._selected.Contains(item.Item1))
                this._selected.Remove(item.Item1);

            // update list view & map view
            item = new UnitListItem(item.Item1, false);
            UnitList.Items[index] = item;
            UnitList.SelectAndShow(index);

            ShowSelection(true);
        }

        #endregion
        #region OnUnitSelected

        /// <summary>
        /// Handles the <see cref="Selector.SelectionChanged"/> event for the "Unit" <see
        /// cref="ListView"/>.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="SelectionChangedEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnUnitSelected</b> updates the "Property" list view to reflect the selected item in
        /// the "Unit" list view. That <see cref="Unit"/> is also moved to the top of its <see
        /// cref="Site"/> stack.</remarks>

        private void OnUnitSelected(object sender, SelectionChangedEventArgs args) {
            args.Handled = true;

            // clear property list view
            PropertyList.ShowEntity(null);

            // retrieve selected unit, if any
            int index = UnitList.SelectedIndex;
            if (index < 0) return;
            UnitListItem item = (UnitListItem) UnitList.Items[index];

            // show unit in Property list & map view
            PropertyList.ShowEntity(item.Item1);
            this._mapView.ShowEntity(item.Item1);
        }

        #endregion
        #region OnUnitWidthChanged

        /// <summary>
        /// Handles changes to the <see cref="FrameworkElement.ActualWidth"/> property of the "Unit"
        /// <see cref="ListView"/>.</summary>
        /// <param name="sender">
        /// The <see cref="ListView"/> control sending the event.</param>
        /// <param name="args">
        /// An <see cref="EventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnUnitWidthChanged</b> resizes the only column of the "Unit" list view to the current
        /// list view width.</remarks>

        private void OnUnitWidthChanged(object sender, EventArgs args) {

            double width = UnitList.ActualWidth - 28;
            if (width > 0) UnitColumn.Width = width;
        }

        #endregion
        #region OnMapMouseDown

        /// <summary>
        /// Handles the <see cref="UIElement.MouseDown"/> event for the hosted <see
        /// cref="MapView"/>.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="MouseButtonEventArgs"/> object containing event data.</param>
        /// <remarks><para>
        /// <b>OnMapMouseDown</b> changes the <see cref="MapView.SelectedSite"/> on left button
        /// clicks, and closes the dialog with a <see cref="Window.DialogResult"/> of <c>true</c> if
        /// the user double-clicked.
        /// </para><para>
        /// Single or double left clicks outside the map area deselect the currently selected site
        /// instead.</para></remarks>

        private void OnMapMouseDown(object sender, MouseButtonEventArgs args) {
            args.Handled = true;

            // left mouse button moves selection
            if (args.ChangedButton != MouseButton.Left)
                return;

            // determine site clicked on, if any
            PointI site = this._mapView.MouseToSite(args);

            // select clicked site in map view
            this._mapView.SelectedSite = site;

            // close dialog on left double-click
            if (args.ClickCount >= 2)
                DialogResult = true;
        }

        #endregion
        #endregion
        #region IDisposable Members

        /// <summary>
        /// Releases any resources used by the <see cref="AttackFromSite"/> dialog.</summary>
        /// <remarks>
        /// <b>Dispose</b> closes the <see cref="MapView"/> hosted by this dialog.</remarks>

        public void Dispose() {
            if (this._mapView != null)
                MapViewManager.Instance.CloseView(this._mapView.Id);
        }

        #endregion
    }
}
