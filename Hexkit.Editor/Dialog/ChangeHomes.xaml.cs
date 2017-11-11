using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

using Tektosyne.Geometry;
using Tektosyne.Windows;

using Hexkit.Global;
using Hexkit.Graphics;
using Hexkit.Scenario;
using Hexkit.World;

namespace Hexkit.Editor.Dialog {
    #region Type Aliases

    // faction class ID, formatted and actual home site
    using FactionListItem = Tuple<String, String, PointI>;

    #endregion

    /// <summary>
    /// Shows a dialog allowing the user to change the home sites of all factions.</summary>
    /// <remarks>
    /// Please refer to the "Change Home Sites" page of the "Editor Dialogs" section in the
    /// application help file for details on this dialog.</remarks>

    public partial class ChangeHomes: Window, IDisposable {
        #region ChangeHomes()

        /// <summary>
        /// Initializes a new instance of the <see cref="ChangeHomes"/> class.</summary>
        /// <remarks>
        /// The data of the current <see cref="AreaSection"/> may be changed in the dialog, as
        /// indicated by the value of the <see cref="DataChanged"/> property.</remarks>

        public ChangeHomes() {
            InitializeComponent();

            // get world state shown in Areas tab page
            var areasContent = MainWindow.Instance.AreasTab.SectionContent;
            WorldState world = ((AreasTabContent) areasContent).WorldState;

            // create map view with default properties
            this._mapView = MapViewManager.Instance.CreateView(
                "changeHomes", world, MapViewHost, OnMapMouseDown, null);

            // prepare to highlight home sites
            this._mapView.SelectedRegion = Finder.MapGrid.CreateArray<Boolean>();

            FactionSection factions = MasterSection.Instance.Factions;
            foreach (FactionClass faction in factions.Collection.Values) {

                // add faction and home site
                PointI home = faction.HomeSite;
                FactionListItem item = new FactionListItem(faction.Id, Site.Format(home), home);
                FactionList.Items.Add(item);

                // highlight home site if valid
                if (Finder.MapGrid.Contains(home))
                    this._mapView.SelectedRegion[home.X, home.Y] = true;
            }

            // adjust column width of Faction list view
            DependencyPropertyDescriptor.FromProperty(
                ListView.ActualWidthProperty, typeof(ListView))
                .AddValueChanged(FactionList, OnFactionWidthChanged);
        }

        #endregion
        #region Private Fields

        // map view hosted by this dialog
        private readonly MapView _mapView;

        #endregion
        #region DataChanged

        /// <summary>
        /// Gets a value indicating whether any data was changed in the dialog.</summary>
        /// <value>
        /// <c>true</c> if the current <see cref="AreaSection"/> has been modified; otherwise,
        /// <c>false</c>.</value>
        /// <remarks>
        /// <b>DataChanged</b> returns <c>false</c> if no detectable changes were made. However, the
        /// original data may have been overwritten with a copy that is not detectably different,
        /// namely if the user clicked <b>OK</b> without making any changes.</remarks>

        public bool DataChanged { get; private set; }

        #endregion
        #region Private Methods
        #region IsHomeSite

        /// <summary>
        /// Determines whether the "Faction" <see cref="ListView"/> contains a <see
        /// cref="FactionListItem"/> for the specified map location.</summary>
        /// <param name="location">
        /// The coordinates of the <see cref="Site"/> whose <see cref="FactionListItem"/> to find.
        /// </param>
        /// <returns>
        /// <c>true</c> if the "Faction" <see cref="ListView"/> contains a <see
        /// cref="FactionListItem"/> that corresponds to the specified <paramref name="location"/>;
        /// otherwise, <c>false</c>.</returns>

        private bool IsHomeSite(PointI location) {

            // search coordinates stored in Item3 property
            foreach (FactionListItem item in FactionList.Items)
                if (item.Item3 == location) return true;

            return false;
        }

        #endregion
        #region SelectSite

        /// <summary>
        /// Selects the specified map location in the map view and shows its data in the "Site" <see
        /// cref="GroupBox"/>.</summary>
        /// <param name="location">
        /// The coordinates of the <see cref="Site"/> to select, if any.</param>
        /// <param name="isHome">
        /// <c>true</c> if the "Faction" <see cref="ListView"/> contains a <see
        /// cref="FactionListItem"/> with the specified <paramref name="location"/>; otherwise,
        /// <c>false</c>.</param>
        /// <remarks><para>
        /// <b>SelectSite</b> calls <see cref="ShowButtons"/> to show or hide the "Set As Home Site"
        /// and "Clear Home Site" buttons, depending on the specified arguments.
        /// </para><para>
        /// <b>SelectSite</b> also centers the <see cref="MapView"/> on the specified <paramref
        /// name="location"/> if <paramref name="isHome"/> is <c>true</c>.</para></remarks>

        private void SelectSite(PointI location, bool isHome) {

            // (de)select site in map view
            if (isHome)
                this._mapView.CenterAndSelect(location);
            else
                this._mapView.SelectedSite = location;

            // show coordinates in Site group box
            Site site = this._mapView.WorldState.GetSite(location);
            SiteGroup.IsEnabled = (site != null);
            SiteGroup.Header = Site.FormatLabel(location);

            // show site owner in Owner text box
            if (site == null || site.Owner == null)
                OwnerBox.Text = Global.Strings.LabelOwnerNone;
            else
                OwnerBox.Text = site.Owner.Id;

            // enable or disable Capture check box
            CaptureToggle.IsChecked = (site != null && site.CanCapture);

            // show or hide buttons
            ShowButtons(location, isHome);
        }

        #endregion
        #region SetHome

        /// <summary>
        /// Sets the <see cref="FactionClass.HomeSite"/> of the selected faction to the specified
        /// location.</summary>
        /// <param name="newHome">
        /// The new <see cref="FactionClass.HomeSite"/> for the selected faction.</param>
        /// <remarks>
        /// <b>SetHome</b> sets the <see cref="FactionClass.HomeSite"/> stored with the first
        /// selected item in the "Faction" list view, if any, to the specified <paramref
        /// name="newHome"/>, updates the dialog accordingly, and sets the <see cref="DataChanged"/>
        /// flag if the <b>HomeSite</b> has changed.</remarks>

        private void SetHome(PointI newHome) {

            // retrieve selected faction, if any
            int index = FactionList.SelectedIndex;
            if (index < 0) return;
            FactionListItem item = (FactionListItem) FactionList.Items[index];

            // quit if coordinates unchanged
            PointI oldHome = item.Item3;
            if (newHome == oldHome) return;

            // highlight new home site if valid
            if (Finder.MapGrid.Contains(newHome))
                this._mapView.SelectedRegion[newHome.X, newHome.Y] = true;

            // show new home site and store coordinates
            item = new FactionListItem(item.Item1, Site.Format(newHome), newHome);
            FactionList.Items[index] = item;
            FactionList.SelectAndShow(index);

            DataChanged = true;

            // remove highlight on old home site if not used by another faction
            if (!IsHomeSite(oldHome) && Finder.MapGrid.Contains(oldHome))
                this._mapView.SelectedRegion[oldHome.X, oldHome.Y] = false;

            // show or hide buttons
            ShowButtons(newHome, true);
            this._mapView.Redraw();
        }

        #endregion
        #region ShowButtons

        /// <summary>
        /// Shows or hides the "Clear Home" and "Set As Home" <see cref="Button"/> controls.
        /// </summary>
        /// <param name="location">
        /// The coordinates of the selected <see cref="Site"/>, if any.</param>
        /// <param name="isHome">
        /// <c>true</c> if the "Faction" <see cref="ListView"/> contains a <see
        /// cref="FactionListItem"/> with the specified <paramref name="location"/>; otherwise,
        /// <c>false</c>.</param>

        private void ShowButtons(PointI location, bool isHome) {

            bool isValid = (location.X >= 0 && location.Y >= 0);
            bool isVisible = (isValid && isHome);

            // show or hide Clear Home button
            ClearHomeButton.Visibility = (isVisible ? Visibility.Visible : Visibility.Collapsed);
            ClearHomeButton.IsEnabled = isVisible;

            // show or hide Set As Home button
            SetHomeButton.Visibility = (isVisible ? Visibility.Collapsed : Visibility.Visible);
            SetHomeButton.IsEnabled = (isValid && !isHome);
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
        /// cref="ChangeHomes"/> dialog.</remarks>

        private void HelpExecuted(object sender, ExecutedRoutedEventArgs args) {
            args.Handled = true;
            ApplicationUtility.ShowHelp("DlgChangeHomes.html");
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
        /// flag if the <see cref="Window.DialogResult"/> is not <c>true</c>, indicating that the
        /// user cancelled the dialog and wants to discard all changes.
        /// </para><para>
        /// Otherwise, <b>OnClosing</b> reads the control contents of this dialog into the current
        /// <see cref="AreaSection"/>.</para></remarks>

        protected override void OnClosing(CancelEventArgs args) {
            base.OnClosing(args);
            if (args.Cancel) return;

            // user cancelled dialog, ignore changes
            if (DialogResult != true) {
                DataChanged = false;
                return;
            }

            // read coordinates stored in Faction list view
            FactionSection factions = MasterSection.Instance.Factions;
            foreach (FactionListItem item in FactionList.Items) {
                FactionClass faction = factions.Collection[item.Item1];
                faction.HomeSite = item.Item3;
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
        /// selecting the first item in the "Faction" list view, if any.</para></remarks>

        protected override void OnContentRendered(EventArgs args) {
            base.OnContentRendered(args);

            // select first item in Faction list view
            if (FactionList.Items.Count > 0)
                FactionList.SelectedIndex = 0;
        }

        #endregion
        #region OnFactionSelected

        /// <summary>
        /// Handles the <see cref="Selector.SelectionChanged"/> event for the "Faction" <see
        /// cref="ListView"/>.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="SelectionChangedEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnFactionSelected</b> calls <see cref="SelectSite"/> with the <see
        /// cref="FactionClass.HomeSite"/> associated with the first selected item in the "Faction"
        /// list view, if any.</remarks>

        private void OnFactionSelected(object sender, SelectionChangedEventArgs args) {
            args.Handled = true;

            // retrieve selected faction, if any
            int index = FactionList.SelectedIndex;
            if (index < 0) return;
            FactionListItem item = (FactionListItem) FactionList.Items[index];

            // update controls for home site
            SelectSite(item.Item3, true);
        }

        #endregion
        #region OnFactionWidthChanged

        /// <summary>
        /// Handles changes to the <see cref="FrameworkElement.ActualWidth"/> property of the
        /// "Faction" <see cref="ListView"/>.</summary>
        /// <param name="sender">
        /// The <see cref="ListView"/> control sending the event.</param>
        /// <param name="args">
        /// An <see cref="EventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnFactionWidthChanged</b> resizes the first column of the "Faction" list view to the
        /// available list view width.</remarks>

        private void OnFactionWidthChanged(object sender, EventArgs args) {

            double width = FactionList.ActualWidth - FactionHomeColumn.ActualWidth - 28;
            if (width > 0) FactionColumn.Width = width;
        }

        #endregion
        #region OnHomeClear

        /// <summary>
        /// Handles the <see cref="ButtonBase.Click"/> event for the "Clear Home Site" <see
        /// cref="Button"/>.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="RoutedEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnHomeClear</b> sets the <see cref="FactionClass.HomeSite"/> stored with the first
        /// selected item in the "Faction" list view, if any, to <see
        /// cref="PolygonGrid.InvalidLocation"/>, and sets the <see cref="DataChanged"/> flag if the
        /// <b>HomeSite</b> has changed.</remarks>

        private void OnHomeClear(object sender, RoutedEventArgs args) {
            args.Handled = true;
            SetHome(PolygonGrid.InvalidLocation);
        }

        #endregion
        #region OnHomeSet

        /// <summary>
        /// Handles the <see cref="ButtonBase.Click"/> event for the "Set As Home Site" <see
        /// cref="Button"/>.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="RoutedEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnHomeSet</b> sets the <see cref="FactionClass.HomeSite"/> stored with the first
        /// selected item in the "Faction" list view, if any, to the <see
        /// cref="MapView.SelectedSite"/> of the hosted <see cref="MapView"/>, and sets the <see
        /// cref="DataChanged"/> flag if the <b>HomeSite</b> has changed.</remarks>

        private void OnHomeSet(object sender, RoutedEventArgs args) {
            args.Handled = true;
            SetHome(this._mapView.SelectedSite);
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
        /// <b>OnMapMouseDown</b> changes the selected <see cref="Site"/> on left button clicks and
        /// selects the corresponding item in the "Faction" list view if it exists.
        /// </para><para>
        /// If the user double-clicked, <b>OnMapMouseDown</b> sets the selected <see cref="Site"/>
        /// as the new <see cref="FactionClass.HomeSite"/> for the first selected item in the
        /// "Faction" list view, if different.
        /// </para><para>
        /// Single or double left clicks outside the map area deselect the currently selected <see
        /// cref="Site"/> instead.</para></remarks>

        private void OnMapMouseDown(object sender, MouseButtonEventArgs args) {
            args.Handled = true;

            // left mouse button moves selection
            if (args.ChangedButton != MouseButton.Left)
                return;

            // determine site clicked on, if any
            PointI site = this._mapView.MouseToSite(args);

            // update controls for selected site
            bool isHome = IsHomeSite(site);
            SelectSite(site, isHome);

            // set home site on double click
            if (!isHome && args.ClickCount >= 2)
                SetHome(site);
        }

        #endregion
        #endregion
        #region IDisposable Members

        /// <summary>
        /// Releases any resources used by the <see cref="ChangeHomes"/> dialog.</summary>
        /// <remarks>
        /// <b>Dispose</b> closes the <see cref="MapView"/> hosted by this dialog.</remarks>

        public void Dispose() {
            if (this._mapView != null)
                MapViewManager.Instance.CloseView(this._mapView.Id);
        }

        #endregion
    }
}
