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

    // attacking or defending unit, attack check box value & visibility
    using UnitListItem = Tuple<Entity, Boolean, Visibility>;

    #endregion

    /// <summary>
    /// Shows a dialog allowing the <see cref="WorldState.ActiveFaction"/> to attack a specified 
    /// <see cref="Site"/>.</summary>
    /// <remarks>
    /// Please refer to the "Attack Site" page of the "Game Dialogs" section in the application help
    /// file for details on this dialog.</remarks>

    public partial class AttackSite: Window, IDisposable {
        #region AttackSite(PointI, EntityList)

        /// <summary>
        /// Initializes a new instance of the <see cref="AttackSite"/> class with the specified
        /// target location and eligible units.</summary>
        /// <param name="target">
        /// The coordinates of the <see cref="Site"/> to attack.</param>
        /// <param name="eligible">
        /// A <see cref="EntityList"/> containing all <see cref="Unit"/> objects that may be
        /// selected.</param>
        /// <exception cref="ArgumentNullOrEmptyException">
        /// <paramref name="eligible"/> is a null reference or an empty collection.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="target"/> is not a valid map location.</exception>
        /// <remarks><para>
        /// <b>AttackSite</b> sets the <see cref="Window.DialogResult"/> to <c>false</c> if the user
        /// did not select at least one unit to participate in the attack.
        /// </para><para>
        /// Otherwise, the selected units are stored in the <see cref="Units"/> collection.
        /// </para></remarks>

        public AttackSite(PointI target, EntityList eligible) {
            if (eligible == null || eligible.Count == 0)
                ThrowHelper.ThrowArgumentNullOrEmptyException("eligible");

            // retrieve current world state
            WorldState world = Session.Instance.WorldState;

            // check target site coordinates
            if (!Finder.MapGrid.Contains(target))
                ThrowHelper.ThrowArgumentOutOfRangeException(
                    "target", target, Tektosyne.Strings.ArgumentCoordinatesInvalid);

            // check placement of eligible units
            foreach (Entity unit in eligible)
                if (!unit.IsPlaced)
                    ThrowHelper.ThrowArgumentException(
                        "eligible", Global.Strings.ArgumentContainsUnplacedUnits);

            this._target = target;
            this._eligible = eligible;

            InitializeComponent();
            Title += Site.Format(target);
            this._lossesFormat = (string) LossesInfo.Content;

            // adjust column width of Unit list view
            DependencyPropertyDescriptor.FromProperty(
                ListView.ActualWidthProperty, typeof(ListView))
                .AddValueChanged(UnitList, OnUnitWidthChanged);

            // create map view with default properties
            this._mapView = MapViewManager.Instance.CreateView(
                "attackSite", world, MapViewHost, OnMapMouseDown, null);

            // attach handler to update dialog controls
            this._mapView.SelectedSiteChanged += OnSiteSelected;

            // highlight target and all sites with eligible units
            this._mapView.SelectedRegion = GetAttackRegion();

            // initially select all eligible units
            this._selected.AddRange(eligible);
            ShowSelection();
        }

        #endregion
        #region Private Fields

        // construction parameters
        private PointI _target = Site.InvalidLocation;
        private readonly EntityList _eligible;

        // format string for projected losses
        private readonly string _lossesFormat;

        // hosted map view and current selection
        private readonly MapView _mapView;
        private readonly EntityList _selected = new EntityList(true);

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
        /// Gets an array indicating the target location and all map locations that contain units
        /// eligible for the attack.</summary>
        /// <returns>
        /// A two-dimensional <see cref="Array"/> of <see cref="Boolean"/> values indicating which
        /// sites contain units that are either attack targets, or that the user may select for
        /// participation in the attack.</returns>
        /// <remarks>
        /// The returned array can be assigned to the <see cref="MapView.SelectedRegion"/> property
        /// of the hosted <see cref="MapView"/>.</remarks>

        private bool[,] GetAttackRegion() {
            bool[,] region = Finder.MapGrid.CreateArray<Boolean>();

            // mark site containing target
            region[this._target.X, this._target.Y] = true;

            // mark sites containing eligible units
            foreach (Entity unit in this._eligible) {
                PointI site = unit.Site.Location;
                region[site.X, site.Y] = true;
            }

            return region;
        }

        #endregion
        #region ShowLosses

        /// <summary>
        /// Shows projected losses for attacking and defending units.</summary>
        /// <remarks><para>
        /// <b>ShowLosses</b> invokes <see cref="Unit.EstimateLosses"/> with all selected units and
        /// updates the "Projected Losses" message with the resulting percentage losses.
        /// </para><para>
        /// <b>ShowLosses</b> shows two zeroes if no units are selected.</para></remarks>

        private void ShowLosses() {
            CombatResults results = new CombatResults();

            // compute estimated combat losses
            if (this._selected.Count > 0) {
                Unit unit = (Unit) this._selected[0];
                results = unit.EstimateLosses(Session.Instance.WorldState,
                    this._selected, this._target, true);
            }

            // show attacker & defender losses
            LossesInfo.Content = String.Format(ApplicationInfo.Culture, 
                this._lossesFormat, results.AttackerPercent, results.DefenderPercent);
        }

        #endregion
        #region ShowSelection

        /// <summary>
        /// Updates the dialog to reflect the current unit selection.</summary>
        /// <remarks>
        /// <b>ShowSelection</b> updates the arrows shown in the hosted <see cref="MapView"/>, and
        /// the data shown in the "Projected Losses" message.</remarks>

        private void ShowSelection() {
            GameUtility.DrawAttackArrows(this._mapView, this._selected, this._target, true);
            ShowLosses();
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
        /// cref="AttackSite"/> dialog.</remarks>

        private void HelpExecuted(object sender, ExecutedRoutedEventArgs args) {
            args.Handled = true;
            ApplicationUtility.ShowHelp("DlgAttackSite.html");
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
        #region OnAllStacksCheck

        /// <summary>
        /// Handles the <see cref="ButtonBase.Click"/> event for the "Check All Stacks" <see
        /// cref="Button"/>.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="RoutedEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnAllStacksCheck</b> calls <see cref="OnStackCheck"/>, adds all remaining eligible
        /// units to the collection of selected units, and calls <see cref="ShowSelection"/> to
        /// reflect the new selection.</remarks>

        private void OnAllStacksCheck(object sender, RoutedEventArgs args) {
            args.Handled = true;

            OnStackCheck(sender, args);

            foreach (Unit unit in this._eligible)
                if (!this._selected.Contains(unit))
                    this._selected.Add(unit);

            ShowSelection();
        }

        #endregion
        #region OnAllStacksUncheck

        /// <summary>
        /// Handles the <see cref="ButtonBase.Click"/> event for the "Uncheck All Stacks" <see
        /// cref="Button"/>.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="RoutedEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnAllStacksUncheck</b> calls <see cref="OnStackUncheck"/>, removes all remaining
        /// units from the collection of selected units, and calls <see cref="ShowSelection"/> to
        /// reflect the new selection.</remarks>

        private void OnAllStacksUncheck(object sender, RoutedEventArgs args) {
            args.Handled = true;

            OnStackUncheck(sender, args);
            this._selected.Clear();
            ShowSelection();
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
        /// one attacking <see cref="Unit"/>.
        /// </para><para>
        /// On success, <b>OnClosing</b> sets the <see cref="Units"/> property to the user's
        /// selections. On failure, <b>OnClosing</b> shows a message box requesting a valid
        /// selection and cancels the <see cref="Window.Closing"/> event.</para></remarks>

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

            // check that selected units can attack target
            var targets = Finder.FindAttackTargets(Session.Instance.WorldState, this._selected);
            if (!targets.Contains(this._target)) {
                MessageBox.Show(this,
                    Global.Strings.DialogUnitAttackInvalid, Global.Strings.TitleSelectionInvalid,
                    MessageBoxButton.OK, MessageBoxImage.Information);
                args.Cancel = true;
                return;
            }

            // store user selections
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
        /// centering the hosted <see cref="MapView"/> on the target <see cref="Site"/> that was
        /// supplied to the constructor, if valid.</para></remarks>

        protected override void OnContentRendered(EventArgs args) {
            base.OnContentRendered(args);

            // center on source site if possible
            if (this._mapView != null && this._target != Site.InvalidLocation)
                this._mapView.CenterAndSelect(this._target);
        }

        #endregion
        #region OnSiteSelected

        /// <summary>
        /// Handles the <see cref="MapView.SelectedSiteChanged"/> event.</summary>
        /// <param name="sender">
        /// The <see cref="MapView"/> object sending the event.</param>
        /// <param name="args">
        /// An <see cref="EventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnSiteSelected</b> updates all dialog controls to reflect the <see
        /// cref="MapView.SelectedSite"/> of the specified <paramref name="sender"/>.</remarks>

        private void OnSiteSelected(object sender, EventArgs args) {
            MapView mapView = sender as MapView;
            if (mapView == null) return;

            // retrieve new map view selection
            PointI location = mapView.SelectedSite;

            // clear Unit and Property list views
            UnitList.Items.Clear();
            PropertyList.ShowEntity(null);

            // quit if selection invalid
            if (location != this._target && !mapView.InSelectedRegion(location)) {
                UnitColumn.Header = Global.Strings.LabelSiteInvalid;
                return;
            }

            // selection shows attackers or defenders
            Site site = Session.Instance.WorldState.Sites[location.X, location.Y];

            if (location == this._target) {
                // show defending unit stack
                UnitColumn.Header = Global.Strings.LabelUnitsDefending;

                // disable buttons for defending units
                StackCheckButton.IsEnabled = false;
                StackUncheckButton.IsEnabled = false;

                // show all defending units for examination
                foreach (Entity unit in site.Units)
                    UnitList.Items.Add(new UnitListItem(unit, false, Visibility.Collapsed));
            }
            else {
                // show attacking unit stack
                UnitColumn.Header = Global.Strings.LabelUnitsAttacking;

                // enable buttons for attacking units
                StackCheckButton.IsEnabled = true;
                StackUncheckButton.IsEnabled = true;

                // show all units eligible for attack
                foreach (Entity unit in site.Units)
                    if (this._eligible.Contains(unit)) {
                        bool isChecked = this._selected.Contains(unit);
                        UnitList.Items.Add(new UnitListItem(unit, isChecked, Visibility.Visible));
                    }
            }

            // select first unit, if any
            if (UnitList.Items.Count > 0)
                UnitList.SelectedIndex = 0;
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
        /// <b>OnStackCheck</b> checks all unchecked items in the "Unit" list view if it currently
        /// shows units eligible for attack.</remarks>

        private void OnStackCheck(object sender, RoutedEventArgs args) {
            args.Handled = true;
            int index = UnitList.SelectedIndex;

            // check all unchecked items
            for (int i = 0; i < UnitList.Items.Count; i++) {
                UnitListItem item = (UnitListItem) UnitList.Items[i];
                if (!item.Item2)
                    UnitList.Items[i] = new UnitListItem(item.Item1, true, Visibility.Visible);

                // update selection list
                if (!this._selected.Contains(item.Item1))
                    this._selected.Add(item.Item1);
            }

            // update list view & map view
            UnitList.Items.Refresh();
            UnitList.SelectAndShow(index);
            ShowSelection();
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
        /// <b>OnStackUncheck</b> unchecks all checked items in the "Unit" list view if it currently
        /// shows units eligible for attack.</remarks>

        private void OnStackUncheck(object sender, RoutedEventArgs args) {
            args.Handled = true;
            int index = UnitList.SelectedIndex;

            // uncheck all checked items
            for (int i = 0; i < UnitList.Items.Count; i++) {
                UnitListItem item = (UnitListItem) UnitList.Items[i];
                if (item.Item2)
                    UnitList.Items[i] = new UnitListItem(item.Item1, false, Visibility.Visible);

                // update selection list
                if (this._selected.Contains(item.Item1))
                    this._selected.Remove(item.Item1);
            }

            // update list view & map view
            UnitList.Items.Refresh();
            UnitList.SelectAndShow(index);
            ShowSelection();
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
        /// <remarks>
        /// <b>OnUnitChecked</b> synchronizes the collection of selected units with the contents of
        /// the "Unit" list view by adding the newly checked unit, and then calls <see
        /// cref="ShowSelection"/> to update the display accordingly.</remarks>

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
            item = new UnitListItem(item.Item1, true, Visibility.Visible);
            UnitList.Items[index] = item;
            UnitList.SelectAndShow(index);

            ShowSelection();
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
        /// <remarks>
        /// <b>OnUnitUnchecked</b> synchronizes the collection of selected units with the contents
        /// of the "Unit" list view by removing the newly unchecked unit, and then calls <see
        /// cref="ShowSelection"/> to update the display accordingly.</remarks>

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
            item = new UnitListItem(item.Item1, false, Visibility.Visible);
            UnitList.Items[index] = item;
            UnitList.SelectAndShow(index);

            ShowSelection();
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
        /// <remarks>
        /// <b>OnMapMouseDown</b> changes the <see cref="MapView.SelectedSite"/> on left button
        /// clicks. Left clicks outside the map area deselect the currently selected site instead.
        /// </remarks>

        private void OnMapMouseDown(object sender, MouseButtonEventArgs args) {
            args.Handled = true;

            // left mouse button moves selection
            if (args.ChangedButton != MouseButton.Left)
                return;

            // determine site clicked on, if any
            PointI site = this._mapView.MouseToSite(args);

            // select clicked site in map view
            this._mapView.SelectedSite = site;
        }

        #endregion
        #endregion
        #region IDisposable Members

        /// <summary>
        /// Releases any resources used by the <see cref="AttackSite"/> dialog.</summary>
        /// <remarks>
        /// <b>Dispose</b> closes the <see cref="MapView"/> hosted by this dialog.</remarks>

        public void Dispose() {
            if (this._mapView != null)
                MapViewManager.Instance.CloseView(this._mapView.Id);
        }

        #endregion
    }
}
