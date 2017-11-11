using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

using Tektosyne;
using Tektosyne.Net;
using Tektosyne.Windows;

using Hexkit.Global;
using Hexkit.Players;
using Hexkit.Scenario;
using Hexkit.World;

namespace Hexkit.Game.Dialog {
    #region Type Aliases

    // faction, controlling player, and details text
    using FactionListItem = Tuple<Faction, Player, String>;

    #endregion

    /// <summary>
    /// Shows a dialog allowing the user to change the player setup.</summary>
    /// <remarks>
    /// Please refer to the "Player Setup" page of the "Game Dialogs" section in the application
    /// help file for details on this dialog.</remarks>

    public partial class ChangePlayers: Window {
        #region ChangePlayers()

        /// <summary>
        /// Initializes a new instance of the <see cref="ChangePlayers"/> class.</summary>
        /// <remarks>
        /// The data of the current <see cref="PlayerManager"/> instance may be changed in this
        /// dialog, as indicated by the <see cref="DataChanged"/> property.</remarks>

        public ChangePlayers() {
            InitializeComponent();

            // adjust column widths of Faction list view
            DependencyPropertyDescriptor.FromProperty(
                ListView.ActualWidthProperty, typeof(ListView))
                .AddValueChanged(FactionList, OnFactionWidthChanged);

            // add algorithms to Algorithm combo box
            AlgorithmCombo.ItemsSource = PlayerManager.Instance.Algorithms;

            // add factions to Faction list view
            foreach (Faction faction in Session.Instance.WorldState.Factions)
                FactionList.Items.Add(CreateFactionRow(faction));

            // select first faction, if any
            if (FactionList.Items.Count > 0)
                FactionList.SelectedIndex = 0;
        }

        #endregion
        #region Private Fields

        // ignore control input events?
        private bool _ignoreEvents;

        #endregion
        #region DataChanged

        /// <summary>
        /// Gets a value indicating whether any data was changed in the dialog.</summary>
        /// <value>
        /// <c>true</c> if the data of the current <see cref="PlayerManager"/> instance has been
        /// modified; otherwise, <c>false</c>.</value>

        public bool DataChanged { get; private set; }

        #endregion
        #region Private Members
        #region SelectedComputerPlayer

        /// <summary>
        /// Gets the controlling <see cref="ComputerPlayer"/> of the selected item in the "Faction"
        /// <see cref="ListView"/>.</summary>
        /// <value>
        /// The <see cref="ComputerPlayer"/> associated with the selected item in the "Faction" <see
        /// cref="ListView"/>, if any; otherwise, a null reference.</value>

        private ComputerPlayer SelectedComputerPlayer {
            get {
                object item = FactionList.SelectedItem;
                if (item == null) return null;
                return ((FactionListItem) item).Item2 as ComputerPlayer;
            }
        }

        #endregion
        #region SelectedHumanPlayer

        /// <summary>
        /// Gets the controlling <see cref="HumanPlayer"/> of the selected item in the "Faction"
        /// <see cref="ListView"/>.</summary>
        /// <value>
        /// The <see cref="HumanPlayer"/> associated with the selected item in the "Faction" <see
        /// cref="ListView"/>, if any; otherwise, a null reference.</value>

        private HumanPlayer SelectedHumanPlayer {
            get {
                object item = FactionList.SelectedItem;
                if (item == null) return null;
                return ((FactionListItem) item).Item2 as HumanPlayer;
            }
        }

        #endregion
        #region CreateFactionRow

        /// <summary>
        /// Creates a new <see cref="FactionListItem"/> for the specified <see cref="Faction"/>.
        /// </summary>
        /// <param name="faction">
        /// The <see cref="Faction"/> to process.</param>
        /// <returns>
        /// A new <see cref="FactionListItem"/> containing the specified <paramref name="faction"/>,
        /// its controlling <see cref="Player"/>, and details on the latter.</returns>
        /// <remarks>
        /// <b>CreateFactionRow</b> shows the e-mail address or computer player algorithm of the
        /// <see cref="Player"/> that controls the specified <paramref name="faction"/>, or an empty
        /// string if no details are available.</remarks>

        private static FactionListItem CreateFactionRow(Faction faction) {

            // retrieve controlling player
            Player player = PlayerManager.Instance.GetPlayer(faction);
            HumanPlayer human = player as HumanPlayer;
            ComputerPlayer computer = player as ComputerPlayer;

            string details = "";
            if (human != null) {
                // show e-mail or "Hotseat" for human players
                details = StringUtility.Validate(human.Email, Global.Strings.LabelLocalHotseat);
            }
            else if (computer != null) {
                // show algorithm for computer players
                details = computer.Options.Algorithm.Name;
            }

            return new FactionListItem(faction, player, details);
        }

        #endregion
        #region UpdatePlayer

        /// <summary>
        /// Updates the "Selected Human Player" or "Selected Computer Player" <see cref="GroupBox"/>
        /// with the data of the specified <see cref="Player"/>.</summary>
        /// <param name="player">
        /// The <see cref="Player"/> whose data to show.</param>
        /// <remarks>
        /// <b>UpdatePlayer</b> shows only the "Selected Player" group box that corresponds to the
        /// concrete type of the specified <paramref name="player"/>, and hides the other.</remarks>

        private void UpdatePlayer(Player player) {

            // determine concrete player type
            HumanPlayer human = player as HumanPlayer;
            ComputerPlayer computer = player as ComputerPlayer;

            if (human != null) {
                HumanGroup.Visibility = Visibility.Visible;
                ComputerGroup.Visibility = Visibility.Hidden;

                // show current player name
                NameBox.Text = human.Name;
            }
            else if (computer != null) {
                ComputerGroup.Visibility = Visibility.Visible;
                HumanGroup.Visibility = Visibility.Hidden;

                // show current algorithm
                AlgorithmCombo.SelectedItem = computer.Options.Algorithm;

                // show current algorithm options
                TargetLimitUpDown.Value = computer.Options.TargetLimit;
                RandomBuildToggle.IsChecked = computer.Options.UseRandomBuild;
                RandomPlaceToggle.IsChecked = computer.Options.UseRandomPlace;
                ScriptingToggle.IsChecked = computer.Options.UseScripting;
            }
        }

        #endregion
        #region UpdatePlayers

        /// <summary>
        /// Updates all dialog controls to reflect the current player assignments.</summary>
        /// <remarks>
        /// <b>OnPlayersChanged</b> updates the "Faction" list view and the "Selected Faction" and
        /// "Selected Player" group boxes to reflect the current player assignments, and sets the
        /// <see cref="DataChanged"/> flag.</remarks>

        internal void UpdatePlayers() {

            // remember selected faction, if any
            int index = FactionList.SelectedIndex;
            FactionList.SelectedIndex = -1;

            // update Faction list view
            FactionList.Items.Clear();
            foreach (Faction faction in Session.Instance.WorldState.Factions)
                FactionList.Items.Add(CreateFactionRow(faction));

            // reselect remembered faction, if any
            if (index >= 0 && index < FactionList.Items.Count)
                FactionList.SelectAndShow(index);
            else if (FactionList.Items.Count > 0)
                FactionList.SelectedIndex = 0;

            // broadcast data change
            DataChanged = true;
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
        /// cref="ChangePlayers"/> dialog.</remarks>

        private void HelpExecuted(object sender, ExecutedRoutedEventArgs args) {
            args.Handled = true;
            ApplicationUtility.ShowHelp("DlgChangePlayers.html");
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
        /// <remarks><para>
        /// <b>OnAccept</b> calls <see cref="Window.Close"/> on this dialog, and also <see
        /// cref="Window.Activate"/> on its <see cref="Window.Owner"/> if that <see cref="Window"/>
        /// is the <see cref="MainWindow"/>.
        /// </para><para>
        /// This event handler is necessary because the <see cref="Window.Hide"/> and <see
        /// cref="Window.Show"/> calls potentially performed by an owning <see cref="ShowFactions"/>
        /// dialog prevent the "OK" button from closing the dialog by setting the <see
        /// cref="Window.DialogResult"/> property, and also deactivate the <see cref="MainWindow"/>.
        /// </para></remarks>

        private void OnAccept(object sender, RoutedEventArgs args) {
            args.Handled = true;
            Close();

            if (Owner == MainWindow.Instance)
                Owner.Activate();
        }

        #endregion
        #region OnAlgorithmSelected

        /// <summary>
        /// Handles the <see cref="Selector.SelectionChanged"/> event for the "Algorithm" <see
        /// cref="ComboBox"/>.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="SelectionChangedEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnAlgorithmSelected</b> sets the algorithm for selected <see cref="ComputerPlayer"/>
        /// to the selected item in the "Algorithm" combo box, and sets the <see
        /// cref="DataChanged"/> flag if any changes were made.</remarks>

        private void OnAlgorithmSelected(object sender, SelectionChangedEventArgs args) {
            args.Handled = true;
            if (this._ignoreEvents) return;

            // retrieve selected algorithm, if any
            Algorithm algorithm = (Algorithm) AlgorithmCombo.SelectedItem;
            if (algorithm == null) return;

            // retrieve selected computer player, if any
            ComputerPlayer computer = SelectedComputerPlayer;
            if (computer == null) return;

            // broadcast data changes, if any
            if (algorithm != computer.Options.Algorithm) {
                computer.Options = AlgorithmOptions.Create(algorithm.Id);
                UpdatePlayers();
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
        /// handles the <see cref="Window.Closing"/> event by checking if all human players that are
        /// assigned to factions use the same mode, i.e. hotseat or PBEM. If not, <b>OnClosing</b>
        /// requests the user to change the dialog data accordingly, and the event is cancelled.
        /// </para></remarks>

        protected override void OnClosing(CancelEventArgs args) {
            base.OnClosing(args);
            if (args.Cancel) return;

            bool email = false, hotseat = false;
            foreach (Faction faction in Session.Instance.WorldState.Factions) {

                // only consider human players
                HumanPlayer human = PlayerManager.Instance.GetPlayer(faction) as HumanPlayer;
                if (human == null) continue;

                // set flag for human player mode
                if (human.Email.Length == 0) hotseat = true;
                else email = true;

                // check for conflicting flags
                if (email && hotseat) {
                    MessageBox.Show(this,
                        Global.Strings.DialogPlayerConflict, Global.Strings.TitlePlayerConflict,
                        MessageBoxButton.OK, MessageBoxImage.Information);

                    args.Cancel = true;
                    return;
                }
            }
        }

        #endregion
        #region OnEmailChange

        /// <summary>
        /// Handles the <see cref="ButtonBase.Click"/> event for the "Change E-mail Address" <see
        /// cref="Button"/>.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="RoutedEventArgs"/> object containing event data.</param>
        /// <remarks><para>
        /// <b>OnEmailChange</b> displays a <see cref="ChangeEmail"/> dialog, allowing the user the
        /// enter a new e-mail address for the selected <see cref="HumanPlayer"/>.
        /// </para><para>
        /// If the user confirmed (which implies that the changed e-mail address is valid),
        /// <b>OnEmailChange</b> updates the player's e-mail address, and sets the <see
        /// cref="DataChanged"/> flag if the previous address was different.</para></remarks>

        private void OnEmailChange(object sender, RoutedEventArgs args) {
            args.Handled = true;

            // retrieve selected human player, if any
            HumanPlayer human = SelectedHumanPlayer;
            if (human == null) return;

            // ask user for new e-mail address
            var dialog = new ChangeEmail(human.Email, human.Name) { Owner = this };
            if (dialog.ShowDialog() != true) return;

            // update e-mail address if different
            if (human.Email != dialog.Address) {
                human.Email = dialog.Address;
                UpdatePlayers();
            }
        }

        #endregion
        #region OnEmailClear

        /// <summary>
        /// Handles the <see cref="ButtonBase.Click"/> event for the "Clear" <see cref="Button"/>.
        /// </summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="RoutedEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnEmailClear</b> sets the e-mail address of the selected <see cref="HumanPlayer"/> to
        /// an empty string, indicating hotseat mode, and sets the <see cref="DataChanged"/> flag if
        /// the previous e-mail address had ben valid.</remarks>

        private void OnEmailClear(object sender, RoutedEventArgs args) {
            args.Handled = true;

            // retrieve selected human player, if any
            HumanPlayer human = SelectedHumanPlayer;
            if (human == null) return;

            // clear e-mail address if currently valid
            if (!String.IsNullOrEmpty(human.Email)) {
                human.Email = "";
                UpdatePlayers();
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
        /// <b>OnFactionActivate</b> displays a <see cref="ShowFactions"/> dialog containing
        /// information on the double-clicked item in the "Faction" list view.</remarks>

        private void OnFactionActivate(object sender, MouseButtonEventArgs args) {
            args.Handled = true;

            // retrieve double-clicked item, if any
            var source = args.OriginalSource as DependencyObject;
            var listItem = ItemsControl.ContainerFromElement(FactionList, source) as ListViewItem;
            if (listItem == null) return;

            // show info dialog for faction class
            FactionListItem item = (FactionListItem) listItem.Content;
            var dialog = new ShowFactions(Session.MapView, item.Item1.FactionClass);
            dialog.Owner = this;
            dialog.ShowDialog();
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
        /// <b>OnFactionSelected</b> updates all dialog controls to reflect the selected item in the
        /// "Faction" list view.</remarks>

        private void OnFactionSelected(object sender, SelectionChangedEventArgs args) {
            args.Handled = true;
            this._ignoreEvents = true;

            // retrieve selected player, if any
            object item = FactionList.SelectedItem;
            if (item == null) return;
            Player player = ((FactionListItem) item).Item2;

            // determine type of controlling player
            bool isHuman = (player is HumanPlayer);

            // update Selected Faction controls
            HumanToggle.IsChecked = isHuman;
            ComputerToggle.IsChecked = !isHuman;

            // update Player combo box
            PlayerCombo.Items.Clear();
            if (isHuman) {
                // show all human player names
                foreach (HumanPlayer human in PlayerManager.Instance.Humans)
                    PlayerCombo.Items.Add(human);
            } else {
                // show all computer player names
                foreach (ComputerPlayer computer in PlayerManager.Instance.Computers)
                    PlayerCombo.Items.Add(computer);
            }

            // update Selected Player group boxes
            PlayerCombo.SelectedItem = player;
            UpdatePlayer(player);
            this._ignoreEvents = false;
        }

        #endregion
        #region OnFactionStatus

        /// <summary>
        /// Handles the <see cref="ButtonBase.Click"/> event for the "Faction Status" <see
        /// cref="Button"/>.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="RoutedEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnFactionStatus</b> displays a <see cref="ShowFactions"/> dialog containing
        /// information on the selected item in the "Faction" list view, if any.</remarks>

        private void OnFactionStatus(object sender, RoutedEventArgs args) {
            args.Handled = true;

            // retrieve selected faction, if any
            object item = FactionList.SelectedItem;
            if (item == null) return;
            Faction faction = ((FactionListItem) item).Item1;

            // show info dialog for faction class
            var dialog = new ShowFactions(Session.MapView, faction.FactionClass);
            dialog.Owner = this;
            dialog.ShowDialog();
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
        /// <b>OnFactionWidthChanged</b> resizes all columns of the "Faction" list view to equal
        /// shares of the current list view width.</remarks>

        private void OnFactionWidthChanged(object sender, EventArgs args) {

            double width = (FactionList.ActualWidth - 28) / 3.0;
            if (width > 0) {
                FactionColumn.Width = width;
                FactionPlayerColumn.Width = width;
                FactionDetailsColumn.Width = width;
            }
        }

        #endregion
        #region OnMapiBrowse

        /// <summary>
        /// Handles the <see cref="ButtonBase.Click"/> event for the "Browse Address Book" <see
        /// cref="Button"/>.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="RoutedEventArgs"/> object containing event data.</param>
        /// <remarks><para>
        /// <b>OnMapiBrowse</b> invokes <see cref="MapiMail.Address"/> to show the Simple MAPI
        /// address book, allowing the user to select one or more "To" recipients as players.
        /// </para><para>
        /// On success, <b>OnMapiBrowse</b> sets the name and e-mail address of the selected <see
        /// cref="HumanPlayer"/> to the corresponding values of the first selected recipient, and
        /// sets the <see cref="DataChanged"/> flag if any data was changed.
        /// </para><para>
        /// <b>OnMapiBrowse</b> shows an error message if <see cref="MapiMail.Address"/> fails, and
        /// returns silently if the user cancelled the dialog.</para></remarks>

        private void OnMapiBrowse(object sender, RoutedEventArgs args) {
            args.Handled = true;

            // retrieve selected human player, if any
            HumanPlayer human = SelectedHumanPlayer;
            if (human == null) return;

            MapiAddress[] entries = null;
            try {
                // let user select recipients
                entries = MapiMail.Address();
            }
            catch (MapiException e) {
                // show error message unless user cancelled
                if (e.Code != MapiException.Abort)
                    MessageDialog.Show(this,
                        Global.Strings.DialogMailErrorMapi, Global.Strings.TitleMailErrorMapi,
                        e, MessageBoxButton.OK, Images.Warning);

                return;
            }

            // quit if no recipients selected
            if (entries == null || entries.Length == 0)
                return;

            // check for broken MAPI client
            if (!entries[0].Address.IsValidEmail()) {
                MessageBox.Show(this,
                    Global.Strings.DialogMailInvalidMapi, Global.Strings.TitleMailInvalid,
                    MessageBoxButton.OK, MessageBoxImage.Information);

                entries[0] = new MapiAddress(entries[0].Name, "");
            }

            // broadcast data changes, if any
            if (human.Name != entries[0].Name || human.Email != entries[0].Address) {
                NameBox.Text = human.Name = entries[0].Name;
                human.Email = entries[0].Address;
                UpdatePlayers();
            }
        }

        #endregion
        #region OnMapiFind

        /// <summary>
        /// Handles the <see cref="ButtonBase.Click"/> event for the "Find" <see cref="Button"/>.
        /// </summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="RoutedEventArgs"/> object containing event data.</param>
        /// <remarks><para>
        /// <b>OnMapiFind</b> invokes <see cref="MapiMail.ResolveName"/> to retrieve the Simple MAPI
        /// address book data for the player name shown in the "Name" text box.
        /// </para><para>
        /// On success, <b>OnMapiFind</b> sets the name and e-mail address of the selected <see
        /// cref="HumanPlayer"/> to the retrieved name and e-mail address, and sets the <see
        /// cref="DataChanged"/> flag if any data was changed.
        /// </para><para>
        /// <b>OnMapiFind</b> shows an error message if <see cref="MapiMail.ResolveName"/> fails,
        /// and returns silently if the user cancelled any dialog that was shown.</para></remarks>

        private void OnMapiFind(object sender, RoutedEventArgs args) {
            args.Handled = true;

            // retrieve selected human player, if any
            HumanPlayer human = SelectedHumanPlayer;
            if (human == null) return;

            // do nothing if name is empty
            if (NameBox.Text.Length == 0) return;

            MapiAddress resolved;
            try {
                // ask Simple MAPI to resolve player name
                resolved = MapiMail.ResolveName(NameBox.Text);
            }
            catch (MapiException e) {
                // show error message unless user cancelled
                if (e.Code != MapiException.Abort)
                    MessageDialog.Show(this,
                        Global.Strings.DialogMailErrorMapi, Global.Strings.TitleMailErrorMapi,
                        e, MessageBoxButton.OK, Images.Warning);
                return;
            }

            // check for broken MAPI client
            if (!resolved.Address.IsValidEmail()) {
                MessageBox.Show(this,
                    Global.Strings.DialogMailInvalidMapi, Global.Strings.TitleMailInvalid,
                    MessageBoxButton.OK, MessageBoxImage.Information);

                resolved = new MapiAddress(resolved.Name, "");
            }

            // broadcast data changes, if any
            if (human.Name != resolved.Name || human.Email != resolved.Address) {
                NameBox.Text = human.Name = resolved.Name;
                human.Email = resolved.Address;
                UpdatePlayers();
            }
        }

        #endregion
        #region OnNameChanged

        /// <summary>
        /// Handles the <see cref="TextBoxBase.TextChanged"/> event for the "Name" <see
        /// cref="TextBox"/>.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="TextChangedEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnNameChanged</b> sets the <see cref="Player.Name"/> of the selected <see
        /// cref="HumanPlayer"/> to the current contents of the "Name" text box, and sets the <see
        /// cref="DataChanged"/> flag if any changes were made.</remarks>

        private void OnNameChanged(object sender, TextChangedEventArgs args) {
            args.Handled = true;
            if (this._ignoreEvents) return;

            // retrieve selected human player, if any
            HumanPlayer human = SelectedHumanPlayer;
            if (human == null) return;

            // broadcast data changes, if any
            if (human.Name != NameBox.Text) {
                human.Name = NameBox.Text;
                UpdatePlayers();
            }
        }

        #endregion
        #region OnOptionsChanged

        /// <summary>
        /// Handles all <see cref="Control"/> events related to changing algorithm options.
        /// </summary>
        /// <param name="sender">
        /// The <see cref="CheckBox"/> control sending the event.</param>
        /// <param name="args">
        /// An <see cref="EventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnOptionsChanged</b> adjusts the algorithm options used by the selected <see
        /// cref="ComputerPlayer"/> to reflect the current contents of the "Selected Computer
        /// Player" <see cref="GroupBox"/>, and sets the <see cref="DataChanged"/> flag if any
        /// changes were made.</remarks>

        private void OnOptionsChanged(object sender, EventArgs args) {
            if (this._ignoreEvents) return;

            // retrieve selected computer player, if any
            ComputerPlayer computer = SelectedComputerPlayer;
            if (computer == null) return;

            // retrieve selected algorithm options
            int targetLimit = (int) TargetLimitUpDown.Value;
            bool useRandomBuild = (RandomBuildToggle.IsChecked == true);
            bool useRandomPlace = (RandomPlaceToggle.IsChecked == true);
            bool useScripting = (ScriptingToggle.IsChecked == true);

            // check selected options against current values
            bool anyChanges =
                (computer.Options.TargetLimit != targetLimit) ||
                (computer.Options.UseRandomBuild != useRandomBuild) ||
                (computer.Options.UseRandomPlace != useRandomPlace) ||
                (computer.Options.UseScripting != useScripting);

            // update current values with selected options
            computer.Options.TargetLimit = targetLimit;
            computer.Options.UseRandomBuild = useRandomBuild;
            computer.Options.UseRandomPlace = useRandomPlace;
            computer.Options.UseScripting = useScripting;

            // broadcast data changes, if any
            if (anyChanges) UpdatePlayers();
        }

        #endregion
        #region OnPlayerChecked

        /// <summary>
        /// Handles the <see cref="ToggleButton.Checked"/> event for the "Human" and "Computer" <see
        /// cref="RadioButton"/> controls.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="RoutedEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnPlayerChecked</b> sets the controlling player of the selected item in the "Faction"
        /// list view to the faction's default player of the selected type, updates all dialog
        /// controls, and sets the <see cref="DataChanged"/> flag if any changes were made.
        /// </remarks>

        private void OnPlayerChecked(object sender, RoutedEventArgs args) {
            args.Handled = true;
            if (this._ignoreEvents) return;

            // retrieve selected faction, if any
            object item = FactionList.SelectedItem;
            if (item == null) return;
            Faction faction = ((FactionListItem) item).Item1;

            // retrieve faction's scenario index and current player
            int index = MasterSection.Instance.Factions.Collection.IndexOfKey(faction.Id);
            Player oldPlayer = PlayerManager.Instance.GetPlayer(faction);

            // get default player of checked type
            Player player = null;
            if (HumanToggle.IsChecked == true && oldPlayer is ComputerPlayer)
                player = PlayerManager.Instance.Humans[index];
            else if (ComputerToggle.IsChecked == true && oldPlayer is HumanPlayer)
                player = PlayerManager.Instance.Computers[index];

            // broadcast data changes, if any
            if (player != null) {
                PlayerManager.Instance.SetPlayer(faction, player);
                UpdatePlayers();
            }
        }

        #endregion
        #region OnPlayerSelected

        /// <summary>
        /// Handles the <see cref="Selector.SelectionChanged"/> event for the "Player" <see
        /// cref="ComboBox"/>.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="SelectionChangedEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnPlayerSelected</b> sets the controlling player of the selected item in the
        /// "Faction" list view to the selected item in the "Player" combo box, and sets the <see
        /// cref="DataChanged"/> flag if any changes were made.</remarks>

        private void OnPlayerSelected(object sender, SelectionChangedEventArgs args) {
            args.Handled = true;
            if (this._ignoreEvents) return;

            // retrieve selected player, if any
            object playerItem = PlayerCombo.SelectedItem;
            if (playerItem == null) return;

            // retrieve selected faction, if any
            object factionItem = FactionList.SelectedItem;
            if (factionItem == null) return;
            Faction faction = ((FactionListItem) factionItem).Item1;

            // retrieve current controlling player
            Player oldPlayer = PlayerManager.Instance.GetPlayer(faction);

            // retrieve new controlling player, if any
            Player player = playerItem as Player;
            if (player != null && player != oldPlayer) {
                PlayerManager.Instance.SetPlayer(faction, player);
                UpdatePlayers();
            }
        }

        #endregion
        #endregion
    }
}
