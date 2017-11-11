using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

using Tektosyne;
using Tektosyne.Geometry;
using Tektosyne.Windows;

using Hexkit.Global;
using Hexkit.Graphics;
using Hexkit.Scenario;
using Hexkit.World;

namespace Hexkit.Editor.Dialog {
    #region Type Aliases

    // site owner ID, formatted and actual place site
    using SiteListItem = Tuple<String, String, PointI>;

    #endregion

    /// <summary>
    /// Shows a dialog allowing the user to change the placement sites for an <see
    /// cref="EntityClass"/>.</summary>
    /// <remarks>
    /// Please refer to the "Change Placement Sites" page of the "Editor Dialogs" section in the
    /// application help file for details on this dialog.</remarks>

    public partial class ChangePlacements: Window, IDisposable {
        #region ChangePlacements(EntityClass)

        /// <summary>
        /// Initializes a new instance of the <see cref="ChangePlacements"/> class with the
        /// specified <see cref="EntityClass"/>.</summary>
        /// <param name="entityClass">
        /// The <see cref="EntityClass"/> whose <see cref="EntityClass.PlaceSites"/> to change.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="entityClass"/> is a null reference.</exception>
        /// <remarks>
        /// The data of the current <see cref="AreaSection"/> and/or <see cref="EntitySection"/> may
        /// be changed in the dialog, as indicated by the values of the <see cref="AreasChanged"/>
        /// and <see cref="EntitiesChanged"/> properties.</remarks>

        public ChangePlacements(EntityClass entityClass) {
            if (entityClass == null)
                ThrowHelper.ThrowArgumentNullException("entityClass");

            this._entityClass = entityClass;
            InitializeComponent();
            Title += entityClass.Id;

            // show default placement option
            DefaultSitesToggle.IsChecked = entityClass.UseDefaultPlace;

            // get world state shown in Areas tab page
            var areasContent = MainWindow.Instance.AreasTab.SectionContent;
            WorldState world = ((AreasTabContent) areasContent).WorldState;

            // create map view with default properties
            this._mapView = MapViewManager.Instance.CreateView(
                "changePlacements", world, MapViewHost, OnMapMouseDown, null);

            // prepare to highlight home sites
            this._mapView.SelectedRegion = Finder.MapGrid.CreateArray<Boolean>();

            // add placement sites to Site list view
            foreach (PointI target in entityClass.PlaceSites)
                AddSite(target, false);

            // adjust column width of Site list view
            DependencyPropertyDescriptor.FromProperty(
                ListView.ActualWidthProperty, typeof(ListView))
                .AddValueChanged(SiteList, OnSiteWidthChanged);

            // construction completed
            this._initialized = true;
        }

        #endregion
        #region Private Fields

        // construction parameters
        private readonly EntityClass _entityClass;

        // map view hosted by this dialog
        private readonly MapView _mapView;

        // was construction completed?
        private readonly bool _initialized = false;

        #endregion
        #region AreasChanged

        /// <summary>
        /// Gets a value indicating whether any <see cref="AreaSection"/> data was changed in the
        /// dialog.</summary>
        /// <value>
        /// <c>true</c> if the current <see cref="AreaSection"/> has been modified; otherwise,
        /// <c>false</c>.</value>
        /// <remarks>
        /// <b>AreasChanged</b> returns <c>false</c> if no detectable changes were made. However,
        /// the original data may have been overwritten with a copy that is not detectably
        /// different, namely if the user clicked <b>OK</b> without making any changes.</remarks>

        public bool AreasChanged { get; private set; }

        #endregion
        #region EntitiesChanged

        /// <summary>
        /// Gets a value indicating whether any <see cref="EntitySection"/> data was changed in the
        /// dialog.</summary>
        /// <value>
        /// <c>true</c> if the current <see cref="EntitySection"/> has been modified; otherwise,
        /// <c>false</c>.</value>
        /// <remarks>
        /// <b>EntitiesChanged</b> returns <c>false</c> if no detectable changes were made. However,
        /// the original data may have been overwritten with a copy that is not detectably
        /// different, namely if the user clicked <b>OK</b> without making any changes.</remarks>

        public bool EntitiesChanged { get; private set; }

        #endregion
        #region Private Methods
        #region AddSite

        /// <summary>
        /// Adds an item for the specified map location to the "Site" <see cref="ListView"/>.
        /// </summary>
        /// <param name="location">
        /// The coordinates of the <see cref="Site"/> to store in the new <see
        /// cref="SiteListItem"/>.</param>
        /// <param name="select">
        /// <c>true</c> if the new <see cref="SiteListItem"/> should be selected; otherwise,
        /// <c>false</c>.</param>
        /// <returns>
        /// The <see cref="SiteListItem"/> that was added to the "Site" <see cref="ListView"/>.
        /// </returns>
        /// <remarks>
        /// <b>AddSite</b> also adds the specified <paramref name="location"/> to the <see
        /// cref="MapView.SelectedRegion"/> of the hosted <see cref="MapView"/>, and sets the <see
        /// cref="AreasChanged"/> flag unless the dialog is still initializing.</remarks>

        private SiteListItem AddSite(PointI location, bool select) {

            // get site at specified coordinates
            Site site = this._mapView.WorldState.GetSite(location);

            // get owner of this site, if any
            string owner = Global.Strings.LabelOwnerNone;
            if (site != null && site.Owner != null)
                owner = site.Owner.Id;

            // add new site to list view
            var item = new SiteListItem(owner, Site.Format(location), location);
            int index = SiteList.Items.Add(item);
            if (select) SiteList.SelectAndShow(index);

            // add site to selected region
            if (Finder.MapGrid.Contains(location)) {
                this._mapView.SelectedRegion[location.X, location.Y] = true;
                this._mapView.Redraw();
            }

            // broadcast data changes, if any
            if (this._initialized) AreasChanged = true;
            return item;
        }

        #endregion
        #region FindSite

        /// <summary>
        /// Finds the item for the specified map location in the "Site" <see cref="ListView"/>.
        /// </summary>
        /// <param name="location">
        /// The coordinates of the <see cref="Site"/> whose <see cref="SiteListItem"/> to find.
        /// </param>
        /// <returns><para>
        /// The <see cref="SiteListItem"/> in the "Site" <see cref="ListView"/> that corresponds to
        /// the specified <paramref name="location"/>.
        /// </para><para>-or-</para><para>
        /// A null reference if the specified <paramref name="location"/> was not found in the <see
        /// cref="ListView"/>.</para></returns>

        private SiteListItem FindSite(PointI location) {

            // search coordinates stored in Item3 property
            foreach (SiteListItem item in SiteList.Items)
                if (item.Item3 == location) return item;

            return null;
        }

        #endregion
        #region RemoveSite

        /// <summary>
        /// Removes the specified item from the "Site" <see cref="ListView"/>.</summary>
        /// <param name="item">
        /// The <see cref="SiteListItem"/> to remove.</param>
        /// <remarks>
        /// <b>RemoveSite</b> also removes the map location stored with the specified <paramref
        /// name="item"/> from the <see cref="MapView.SelectedRegion"/> of the hosted <see
        /// cref="MapView"/>, and sets the <see cref="AreasChanged"/> flag unless the dialog is
        /// still initializing.</remarks>

        private void RemoveSite(SiteListItem item) {

            // remove site from list view
            SiteList.Items.Remove(item);
            PointI site = item.Item3;
            SelectSite(site, false);

            // remove site from selected region
            if (Finder.MapGrid.Contains(site)) {
                this._mapView.SelectedRegion[site.X, site.Y] = false;
                this._mapView.Redraw();
            }

            // broadcast data changes, if any
            if (this._initialized) AreasChanged = true;
        }

        #endregion
        #region SelectSite

        /// <summary>
        /// Selects the specified map location in the map view and shows its data in the "Site" <see
        /// cref="GroupBox"/>.</summary>
        /// <param name="location">
        /// The coordinates of the <see cref="Site"/> to select.</param>
        /// <param name="isPlace">
        /// <c>true</c> if the "Site" <see cref="ListView"/> contains a <see cref="SiteListItem"/>
        /// for the specified <paramref name="location"/>; otherwise, <c>false</c>.</param>
        /// <remarks><para>
        /// <b>SelectSite</b> enables or disables the controls related to the selected <see
        /// cref="Site"/>, depending on the specified arguments.
        /// </para><para>
        /// <b>SelectSite</b> also centers the <see cref="MapView"/> on the specified <paramref
        /// name="location"/> if <paramref name="isPlace"/> is <c>true</c>.</para></remarks>

        private void SelectSite(PointI location, bool isPlace) {

            // (de)select site in map view
            if (isPlace)
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

            // enable or disable Add/Remove buttons
            AddSiteButton.IsEnabled = (site != null && !isPlace);
            RemoveSiteButton.IsEnabled = (site != null && isPlace);
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
        /// cref="ChangePlacements"/> dialog.</remarks>

        private void HelpExecuted(object sender, ExecutedRoutedEventArgs args) {
            args.Handled = true;
            ApplicationUtility.ShowHelp("DlgChangePlacements.html");
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
        /// handles the <see cref="Window.Closing"/> event by clearing the <see
        /// cref="AreasChanged"/> and <see cref="EntitiesChanged"/> flags if the <see
        /// cref="Window.DialogResult"/> is not <c>true</c>, indicating that the user cancelled the
        /// dialog and wants to discard all changes.
        /// </para><para>
        /// Otherwise, <b>OnClosing</b> reads the control contents of this dialog into the <see
        /// cref="EntityClass"/> supplied to the constructor.</para></remarks>

        protected override void OnClosing(CancelEventArgs args) {
            base.OnClosing(args);
            if (args.Cancel) return;

            // user cancelled dialog, ignore changes
            if (DialogResult != true) {
                AreasChanged = EntitiesChanged = false;
                return;
            }

            // read default placement option
            this._entityClass.UseDefaultPlace = (DefaultSitesToggle.IsChecked == true);

            // read coordinates stored in Site list view
            this._entityClass.PlaceSites.Clear();
            foreach (SiteListItem item in SiteList.Items)
                this._entityClass.PlaceSites.Add(item.Item3);
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
        /// selecting the first item in the "Site" list view, if any.</para></remarks>

        protected override void OnContentRendered(EventArgs args) {
            base.OnContentRendered(args);

            // select first item in Site list view
            if (SiteList.Items.Count > 0)
                SiteList.SelectedIndex = 0;
        }

        #endregion
        #region OnDefaultSites

        /// <summary>
        /// Handles the <see cref="ButtonBase.Click"/> event for the "Allow Default Sites" <see
        /// cref="CheckBox"/>.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="RoutedEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnDefaultSites</b> sets the <see cref="EntitiesChanged"/> flag, unless the dialog is
        /// still initializing.</remarks>

        private void OnDefaultSites(object sender, RoutedEventArgs args) {
            args.Handled = true;
            if (this._initialized) EntitiesChanged = true;
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
        /// selects the corresponding item in the "Site" list view if it exists.
        /// </para><para>
        /// If the user double-clicked, <b>OnMapMouseDown</b> removes the corresponding item from
        /// the "Site" list view if one is found, and adds a new item otherwise.
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

            // select corresponding item in Faction list view
            SiteListItem selected = FindSite(site);

            // add or remove site on double click
            if (selected == null) {
                if (args.ClickCount >= 2)
                    AddSite(site, true);
                else
                    SelectSite(site, false);
            } else {
                if (args.ClickCount >= 2)
                    RemoveSite(selected);
                else
                    SiteList.SelectAndShow(selected);
            }
        }

        #endregion
        #region OnSiteAdd

        /// <summary>
        /// Handles the <see cref="ButtonBase.Click"/> event for the "Add Site" <see
        /// cref="Button"/>.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="RoutedEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnSiteAdd</b> calls <see cref="AddSite"/> to add the <see
        /// cref="MapView.SelectedSite"/> of the hosted <see cref="MapView"/> to the "Site" list
        /// view, if the coordinates are valid.</remarks>

        private void OnSiteAdd(object sender, RoutedEventArgs args) {
            args.Handled = true;

            // add selection to Site list view
            PointI site = this._mapView.SelectedSite;
            if (Finder.MapGrid.Contains(site))
                AddSite(site, true);
        }

        #endregion
        #region OnSiteRemove

        /// <summary>
        /// Handles the <see cref="ButtonBase.Click"/> event for the "Remove Entry" <see
        /// cref="Button"/>.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="RoutedEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnSiteRemove</b> calls <see cref="RemoveSite"/> with the first selected item in the
        /// "Site" list view, if any.</remarks>

        private void OnSiteRemove(object sender, RoutedEventArgs args) {
            args.Handled = true;

            // retrieve selected site, if any
            int index = SiteList.SelectedIndex;
            if (index < 0) return;
            SiteListItem item = (SiteListItem) SiteList.Items[index];

            RemoveSite(item);
        }

        #endregion
        #region OnSiteSelected

        /// <summary>
        /// Handles the <see cref="Selector.SelectionChanged"/> event for the "Site" <see
        /// cref="ListView"/>.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="SelectionChangedEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnSiteSelected</b> calls <see cref="SelectSite"/> with the map location associated
        /// with the first selected item in the "Site" list view, if any.</remarks>

        private void OnSiteSelected(object sender, SelectionChangedEventArgs args) {
            args.Handled = true;

            // retrieve selected site, if any
            int index = SiteList.SelectedIndex;
            if (index < 0) return;
            SiteListItem item = (SiteListItem) SiteList.Items[index];

            // update map view and Site group box
            SelectSite(item.Item3, true);

            // enable Remove Site button
            RemoveSiteButton.IsEnabled = true;
        }

        #endregion
        #region OnSiteWidthChanged

        /// <summary>
        /// Handles changes to the <see cref="FrameworkElement.ActualWidth"/> property of the "Site"
        /// <see cref="ListView"/>.</summary>
        /// <param name="sender">
        /// The <see cref="ListView"/> control sending the event.</param>
        /// <param name="args">
        /// An <see cref="EventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnSiteWidthChanged</b> resizes the second column of the "Site" list view to the
        /// available list view width.</remarks>

        private void OnSiteWidthChanged(object sender, EventArgs args) {

            double width = SiteList.ActualWidth - SiteLocationColumn.ActualWidth - 28;
            if (width > 0) SiteOwnerColumn.Width = width;
        }

        #endregion
        #endregion
        #region IDisposable Members

        /// <summary>
        /// Releases any resources used by the <see cref="ChangePlacements"/> dialog.</summary>
        /// <remarks>
        /// <b>Dispose</b> closes the <see cref="MapView"/> hosted by this dialog.</remarks>

        public void Dispose() {
            if (this._mapView != null)
                MapViewManager.Instance.CloseView(this._mapView.Id);
        }

        #endregion
    }
}
