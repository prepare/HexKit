using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

using Tektosyne;
using Tektosyne.Windows;
using Hexkit.Scenario;

namespace Hexkit.Editor {
    #region Type Aliases

    // avoid confusion with System.Windows.Condition
    using Condition = Hexkit.Scenario.Condition;

    #endregion

    /// <summary>
    /// Provides the "Factions" tab page for the Hexkit Editor application.</summary>
    /// <remarks>
    /// Please refer to the "Factions Page" page of the "Editor Display" section in the application
    /// help file for details on this tab page.</remarks>

    public partial class FactionsTabContent: UserControl, IEditorTabContent {
        #region FactionsTabContent()

        /// <summary>
        /// Initializes a new instance of the <see cref="FactionsTabContent"/> class.</summary>

        public FactionsTabContent() {
            InitializeComponent();

            // adjust column widths of Faction list view
            DependencyPropertyDescriptor.FromProperty(
                ListView.ActualWidthProperty, typeof(ListView))
                .AddValueChanged(FactionList, OnFactionWidthChanged);
        }

        #endregion
        #region Private Fields

        // property backers
        private SectionTabItem _sectionTab;

        #endregion
        #region Private Methods
        #region ChangeFaction

        /// <summary>
        /// Allows the user to change the specified <see cref="FactionClass"/>.</summary>
        /// <param name="faction">
        /// The <see cref="FactionClass"/> to change.</param>
        /// <remarks><para>
        /// <b>ChangeFaction</b> displays a <see cref="Dialog.ChangeFaction"/> dialog for the
        /// specified <paramref name="faction"/>.
        /// </para><para>
        /// If the user made any changes, <b>ChangeFaction</b> propagates them to the faction
        /// collection of the current <see cref="FactionSection"/> and sets the <see
        /// cref="SectionTabItem.DataChanged"/> flag.</para></remarks>

        private void ChangeFaction(FactionClass faction) {
            if (faction == null) return;

            // show dialog and let user make changes
            var dialog = new Dialog.ChangeFaction(faction) { Owner = MainWindow.Instance };
            dialog.ShowDialog();

            // broadcast data changes, if any
            if (dialog.DataChanged) {
                FactionList.Items.Refresh();
                SectionTab.DataChanged = true;
            }
        }

        #endregion
        #region EnableListButtons

        /// <summary>
        /// Enables or disables the "Change ID", "Change Faction", "Remove Faction", "Move Up",
        /// "Move Down", "Change Home Sites", and "Change Colors" <see cref="Button"/> controls,
        /// depending on whether the "Faction" <see cref="ListView"/> contains any items.</summary>

        private void EnableListButtons() {
            bool anyFactions = (FactionList.Items.Count > 0);

            // enable or disable list control buttons
            ChangeIdButton.IsEnabled = anyFactions;
            ChangeFactionButton.IsEnabled = anyFactions;
            RemoveFactionButton.IsEnabled = anyFactions;
            MoveUpButton.IsEnabled = anyFactions;
            MoveDownButton.IsEnabled = anyFactions;

            // enable or disable Change Homes/Colors buttons
            ChangeHomesButton.IsEnabled = anyFactions;
            ChangeColorsButton.IsEnabled = anyFactions;
        }

        #endregion
        #endregion
        #region Event Handlers
        #region OnChangeColors

        /// <summary>
        /// Handles the <see cref="ButtonBase.Click"/> event for the "Change Colors" <see
        /// cref="Button"/>.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="RoutedEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnChangeColors</b> displays a <see cref="Dialog.ChangeColors"/> dialog allowing the
        /// user to change the color codes for all factions, and sets the <see
        /// cref="SectionTabItem.DataChanged"/> flag if the user made any changes.</remarks>

        private void OnChangeColors(object sender, RoutedEventArgs args) {
            args.Handled = true;
            if (FactionList.Items.Count == 0) return;

            // show "Change Colors" dialog
            var dialog = new Dialog.ChangeColors() { Owner = MainWindow.Instance };
            dialog.ShowDialog();

            // broadcast data changes, if any
            if (dialog.DataChanged)
                SectionTab.DataChanged = true;
        }

        #endregion
        #region OnChangeHomes

        /// <summary>
        /// Handles the <see cref="ButtonBase.Click"/> event for the "Change Home Sites" <see
        /// cref="Button"/>.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="RoutedEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnChangeHomes</b> displays a <see cref="Dialog.ChangeHomes"/> dialog allowing the
        /// user to change the home sites of all factions, and sets the <see
        /// cref="SectionTabItem.DataChanged"/> flag of the "Areas" tab page if the user made any
        /// changes.</remarks>

        private void OnChangeHomes(object sender, RoutedEventArgs args) {
            args.Handled = true;
            if (FactionList.Items.Count == 0) return;

            // show "Change Home Sites" dialog
            using (var dialog = new Dialog.ChangeHomes()) {
                dialog.Owner = MainWindow.Instance;
                dialog.ShowDialog();

                // broadcast data changes, if any
                if (dialog.DataChanged)
                    MainWindow.Instance.AreasTab.DataChanged = true;
            }
        }

        #endregion
        #region OnFactionActivate

        /// <summary>
        /// Handles the <see cref="Control.MouseDoubleClick"/> event for a <see
        /// cref="ListViewItem"/> of the "Faction" <see cref="ListView"/>.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="MouseButtonEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnFactionActivate</b> calls <see cref="ChangeFaction"/> with the double-clicked item
        /// in the "Faction" list view.</remarks>

        private void OnFactionActivate(object sender, MouseButtonEventArgs args) {
            args.Handled = true;

            // retrieve double-clicked item, if any
            var source = args.OriginalSource as DependencyObject;
            var item = ItemsControl.ContainerFromElement(FactionList, source) as ListViewItem;
            if (item != null) ChangeFaction(item.Content as FactionClass);
        }

        #endregion
        #region OnFactionAdd

        /// <summary>
        /// Handles the <see cref="ButtonBase.Click"/> event for the "Add Faction" <see
        /// cref="Button"/>.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="RoutedEventArgs"/> object containing event data.</param>
        /// <remarks><para>
        /// <b>OnFactionAdd</b> displays a <see cref="Dialog.ChangeIdentifier"/> dialog, followed by
        /// a <see cref="Dialog.ChangeFaction"/> dialog, allowing the user to define a new faction.
        /// The new faction copies the properties of the first selected item in the "Faction" list
        /// view, if any; otherwise, it is created with default properties.
        /// </para><para>
        /// If the user confirmed both dialogs, <b>OnFactionAdd</b> adds the new faction to the
        /// "Faction" list view and to the current <see cref="FactionSection"/>, and sets the <see
        /// cref="SectionTabItem.DataChanged"/> flag.</para></remarks>

        private void OnFactionAdd(object sender, RoutedEventArgs args) {
            args.Handled = true;

            // ask user for new faction ID
            var factions = MasterSection.Instance.Factions.Collection;
            var dialog = new Dialog.ChangeIdentifier("faction-id",
                Global.Strings.TitleFactionIdEnter, factions.ContainsKey, false);
            dialog.Owner = MainWindow.Instance;
            if (dialog.ShowDialog() != true) return;

            // retrieve new faction ID
            string id = String.Intern(dialog.Identifier);

            // create new faction based on selected faction, if any
            FactionClass faction, selection = FactionList.SelectedItem as FactionClass;
            if (selection == null) {
                faction = new FactionClass(id);
                // add default defeat condition (site loss)
                faction.DefeatConditions.Add(new Condition());
            } else {
                faction = (FactionClass) selection.Clone();
                faction.Id = id;
            }

            // let user make changes to new faction
            var factionDialog = new Dialog.ChangeFaction(faction) { Owner = MainWindow.Instance };
            if (factionDialog.ShowDialog() != true) return;

            // add faction to section table
            factions.Add(id, faction);

            // update list view and select new item
            FactionList.Items.Refresh();
            FactionList.SelectAndShow(faction);

            // broadcast data changes
            EnableListButtons();
            SectionTab.DataChanged = true;
        }

        #endregion
        #region OnFactionChange

        /// <summary>
        /// Handles the <see cref="ButtonBase.Click"/> event for the "Change Faction" <see
        /// cref="Button"/>.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="RoutedEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnFactionChange</b> calls <see cref="ChangeFaction"/> with the first selected item in
        /// the "Faction" list view.</remarks>

        private void OnFactionChange(object sender, RoutedEventArgs args) {
            args.Handled = true;
            ChangeFaction(FactionList.SelectedItem as FactionClass);
        }

        #endregion
        #region OnFactionDown

        /// <summary>
        /// Handles the <see cref="ButtonBase.Click"/> event for the "Move Down" <see
        /// cref="Button"/>.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="RoutedEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnFactionDown</b> swaps the first selected item in the "Faction" list view with its
        /// lower neighbour, propagates the change to the current <see cref="FactionSection"/>, and
        /// sets the <see cref="SectionTabItem.DataChanged"/> flag.</remarks>

        private void OnFactionDown(object sender, RoutedEventArgs args) {
            args.Handled = true;

            // retrieve selected faction, if any
            if (FactionList.Items.Count < 2) return;
            FactionClass faction = FactionList.SelectedItem as FactionClass;
            if (faction == null) return;

            // move faction down in section table
            var factions = MasterSection.Instance.Factions.Collection;
            int index = factions.IndexOfKey(faction.Id);
            if (index < factions.Count - 1) {
                var pair = factions[index + 1];
                factions[index + 1] = factions[index];
                factions[index] = pair;
            }

            // broadcast data changes
            FactionList.Items.Refresh();
            FactionList.SelectAndShow(index + 1);
            SectionTab.DataChanged = true;
        }

        #endregion
        #region OnFactionId

        /// <summary>
        /// Handles the <see cref="ButtonBase.Click"/> event for the "Change ID" <see
        /// cref="Button"/>.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="RoutedEventArgs"/> object containing event data.</param>
        /// <remarks><para>
        /// <b>OnFactionId</b> displays a <see cref="Dialog.ChangeIdentifier"/> dialog for the first
        /// selected item in the "Faction" list view.
        /// </para><para>
        /// If the user made any changes, <b>OnFactionId</b> propagates them to the current <see
        /// cref="FactionSection"/> and sets the <see cref="SectionTabItem.DataChanged"/> flag.
        /// </para></remarks>

        private void OnFactionId(object sender, RoutedEventArgs args) {
            args.Handled = true;

            // retrieve selected faction, if any
            FactionClass faction = FactionList.SelectedItem as FactionClass;
            if (faction == null) return;

            // let user enter new faction ID
            var factions = MasterSection.Instance.Factions.Collection;
            var dialog = new Dialog.ChangeIdentifier(faction.Id,
                Global.Strings.TitleFactionIdChange, factions.ContainsKey, true);
            dialog.Owner = MainWindow.Instance;
            if (dialog.ShowDialog() != true) return;

            // retrieve new faction ID
            string id = String.Intern(dialog.Identifier);

            // change existing ID references
            if (!SectionTabItem.ProcessAllIdentifiers(factions, faction.Id, id))
                return;

            // broadcast data changes
            FactionList.Items.Refresh();
            SectionTab.DataChanged = true;
        }

        #endregion
        #region OnFactionRemove

        /// <summary>
        /// Handles the <see cref="ButtonBase.Click"/> event for the "Remove Faction" <see
        /// cref="Button"/>.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="RoutedEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnFactionRemove</b> removes the first selected item in the "Faction" list view, if
        /// any, from that list view and from the current <see cref="FactionSection"/>, and sets the
        /// <see cref="SectionTabItem.DataChanged"/> flag.</remarks>

        private void OnFactionRemove(object sender, RoutedEventArgs args) {
            args.Handled = true;

            // retrieve selected faction, if any
            int index = FactionList.SelectedIndex;
            if (index < 0) return;
            FactionClass faction = (FactionClass) FactionList.Items[index];

            // delete existing ID references
            var factions = MasterSection.Instance.Factions.Collection;
            if (!SectionTabItem.ProcessAllIdentifiers(factions, faction.Id, null))
                return;

            // select item in the same position
            FactionList.Items.Refresh();
            if (FactionList.Items.Count > 0)
                FactionList.SelectAndShow(Math.Min(FactionList.Items.Count - 1, index));

            // broadcast data changes
            EnableListButtons();
            SectionTab.DataChanged = true;
        }

        #endregion
        #region OnFactionUp

        /// <summary>
        /// Handles the <see cref="ButtonBase.Click"/> event for the "Move Up" <see cref="Button"/>.
        /// </summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="RoutedEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnFactionUp</b> swaps the first selected item in the "Faction" list view with its
        /// upper neighbour, propagates the change to the current <see cref="FactionSection"/>, and
        /// sets the <see cref="SectionTabItem.DataChanged"/> flag.</remarks>

        private void OnFactionUp(object sender, RoutedEventArgs args) {
            args.Handled = true;

            // retrieve selected faction, if any
            if (FactionList.Items.Count < 2) return;
            FactionClass faction = FactionList.SelectedItem as FactionClass;
            if (faction == null) return;

            // move item up in section table
            var factions = MasterSection.Instance.Factions.Collection;
            int index = factions.IndexOfKey(faction.Id);
            if (index > 0) {
                var pair = factions[index - 1];
                factions[index - 1] = factions[index];
                factions[index] = pair;
            }

            // broadcast data changes
            FactionList.Items.Refresh();
            FactionList.SelectAndShow(index - 1);
            SectionTab.DataChanged = true;
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
        /// <b>OnFactionWidthChanged</b> resizes both columns of the "Faction" list view so that
        /// each occupies the same share of the current list view width.</remarks>

        private void OnFactionWidthChanged(object sender, EventArgs args) {

            double width = (FactionList.ActualWidth - 28) / 2.0;
            if (width > 0) {
                FactionIdColumn.Width = width;
                FactionNameColumn.Width = width;
            }
        }

        #endregion
        #endregion
        #region IEditorTabContent Members
        #region Section

        /// <summary>
        /// Gets the <see cref="ScenarioSection"/> managed by the tab page.</summary>
        /// <value>
        /// The constant value <see cref="ScenarioSection.Factions"/>, indicating the Hexkit
        /// scenario section managed by the "Factions" tab page.</value>

        public ScenarioSection Section {
            get { return ScenarioSection.Factions; }
        }

        #endregion
        #region SectionTab

        /// <summary>
        /// Gets or sets the <see cref="SectionTabItem"/> for the tab page.</summary>
        /// <value>
        /// The <see cref="SectionTabItem"/> that contains the <see cref="FactionsTabContent"/>
        /// control, i.e. the "Factions" tab page of the Hexkit Editor application.</value>
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
        /// <remarks>
        /// <b>Initialize</b> initializes all controls that are specific to the "Factions" tab page.
        /// </remarks>

        public void Initialize() {

            // reload factions from scenario section
            var factions = MasterSection.Instance.Factions.Collection.Values;
            if (FactionList.ItemsSource != factions)
                FactionList.ItemsSource = factions;
            else
                FactionList.Items.Refresh();

            // update list button status
            EnableListButtons();

            // select first item by default
            if (FactionList.Items.Count > 0)
                FactionList.SelectedIndex = 0;
        }

        #endregion
        #endregion
    }
}
