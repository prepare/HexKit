using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Threading;

using Tektosyne;
using Tektosyne.Geometry;
using Tektosyne.IO;
using Tektosyne.Win32Api;
using Tektosyne.Windows;

using Hexkit.Global;
using Hexkit.Graphics;
using Hexkit.Options;
using Hexkit.Players;
using Hexkit.Scenario;
using Hexkit.World;
using Hexkit.World.Commands;

using SystemInformation = System.Windows.Forms.SystemInformation;

namespace Hexkit.Game {
    #region Type Aliases

    using MessageEventArgs = EventArgs<String>;

    // entity identifier & name
    using EntityListItem = Tuple<String, String>;

    #endregion

    /// <summary>
    /// Provides the top-level window for the Hexkit Game application.</summary>
    /// <remarks>
    /// Use the <see cref="MainWindow.Instance"/> property to retrieve the <see cref="MainWindow"/>
    /// instance that is associated with the current WPF <see cref="Application"/> instance.
    /// </remarks>

    public partial class MainWindow: Window {
        #region MainWindow()

        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindow"/> class.</summary>

        public MainWindow() {
            InitializeComponent();
            if (DesignerProperties.GetIsInDesignMode(this))
                return;

            // adjust column width of Entity list view
            DependencyPropertyDescriptor.FromProperty(
                ListView.ActualWidthProperty, typeof(ListView))
                .AddValueChanged(EntityList, OnEntityWidthChanged);

            // show default title and status message
            Title = ApplicationInfo.Title;
            StatusMessage.DefaultText = ApplicationInfo.Signature;
            MapViewHost.Background = SystemColors.ControlDarkBrush;

            // show options initialization message, if any
            var options = ApplicationOptions.Instance;
            options.ShowInitMessage();

            // restore saved window bounds, if any
            options.View.LoadDesktop(this);

            // handle options changes and reflect current state
            options.OptionsChanged += OnOptionsChanged;
            OnOptionsChanged(this, EventArgs.Empty);

            // handle session state changes and reflect current state
            Session.StateChanged += OnSessionChanged;
            OnSessionChanged(null, EventArgs.Empty);

            // prepare main menu for selection change handling
            this._menuMessages = ApplicationUtility.InitializeMenu(MainMenu, OnHighlightChanged);

            // create timer to show mouse position
            this._idleTimer = new DispatcherTimer(TimeSpan.FromMilliseconds(100),
                DispatcherPriority.ApplicationIdle, OnIdleTimer, Dispatcher);
        }

        #endregion
        #region Private Fields

        // property backers
        private PointI _statusPosition = Site.InvalidLocation;

        // commands without associated KeyGesture
        private Dictionary<Key, RoutedUICommand> _commands;

        // timer for mouse position updates
        private readonly DispatcherTimer _idleTimer;

        // currently highlighted menu items
        private readonly List<MenuItem> _menuItems = new List<MenuItem>();

        // help links and status bar messages for menu items
        private readonly Dictionary<String, String> _menuMessages;

        // depth of current wait stack
        private uint _waitCalls;

        #endregion
        #region Public Properties
        #region Instance

        /// <summary>
        /// Gets the current instance of the <see cref="MainWindow"/> class.</summary>
        /// <value>
        /// The instance of the <see cref="MainWindow"/> class that is associated with the current
        /// WPF <see cref="Application"/> instance.</value>
        /// <remarks>
        /// <b>MainWindow</b> returns the value of the <see cref="Application.MainWindow"/> property
        /// of the current WPF <see cref="Application"/> instance, cast to type <see
        /// cref="MainWindow"/> for convenience.</remarks>

        public static MainWindow Instance {
            [DebuggerStepThrough]
            get { return (MainWindow) Application.Current.MainWindow; }
        }

        #endregion
        #region IsComputerStopped

        /// <summary>
        /// Gets a value indicating whether <see cref="OnComputerComplete"/> was suspended and waits
        /// to be called back by the <see cref="UserAction"/> class.</summary>
        /// <value>
        /// <c>true</c> if <see cref="OnComputerComplete"/> started executing but returned because a
        /// <see cref="UserAction"/> was executing; otherwise, <c>false</c>.</value>
        /// <remarks><para>
        /// The <see cref="UserAction"/> class notes whether a <see cref="ComputerThread"/> is
        /// currently executing, but needs to know whether the thread has already called <see
        /// cref="OnComputerComplete"/> so that it can repeat that call as necessary.
        /// </para><para>
        /// <b>IsComputerStopped</b> provides this information. This flag is set by <see
        /// cref="OnComputerComplete"/> if that method was suspended by an executing <see
        /// cref="UserAction"/>, and cleared either by an unblocked <see cref="OnComputerComplete"/>
        /// call or whenever the <see cref="Session.State"/> changes.</para></remarks>

        public bool IsComputerStopped { get; private set; }

        #endregion
        #region SelectedEntity

        /// <summary>
        /// Gets or sets the identifier of the selected <see cref="Entity"/>.</summary>
        /// <value><para>
        /// The <see cref="Entity.Id"/> string of the selected <see cref="Entity"/> in the "Site
        /// Contents" <see cref="ListView"/>.
        /// </para><para>-or-</para><para>
        /// An empty string if no <see cref="Entity"/> is selected.</para></value>
        /// <remarks><para>
        /// <b>SelectedEntity</b> defaults to the identifier of the first unit, effect, or terrain
        /// in the "Site Contents" list view, in that order.
        /// </para><para>
        /// Setting <b>SelectedEntity</b> to an empty string, to a null reference, or to the
        /// identifier of an <see cref="Entity"/> that is not present in the "Site Contents" list
        /// view clears the selection and returns an empty string.
        /// </para><note type="implementnotes">
        /// <b>SelectedEntity</b> and the "Site Contents" list view store identifiers rather than
        /// <see cref="Entity"/> references because the current <see cref="Session.WorldState"/>
        /// that supplies such references is replaced by a deep copy of itself after every computer
        /// player's turn.</note></remarks>

        public string SelectedEntity {
            get {
                object item = EntityList.SelectedItem;
                if (item == null || !(item is EntityListItem))
                    return "";

                return ((EntityListItem) item).Item1;
            }
            set {
                // HACK: prevents occasional scroll jitter
                EntityList.Items.Refresh();

                EntityList.SelectedIndex = -1;
                if (String.IsNullOrEmpty(value)) return;

                for (int i = 0; i < EntityList.Items.Count; i++) {
                    object item = EntityList.Items[i];
                    if (!(item is EntityListItem)) continue;

                    if (((EntityListItem) item).Item1 == value) {
                        EntityList.SelectedIndex = i;
                        EntityList.ScrollIntoView(item);
                        return;
                    }
                }
            }
        }

        #endregion
        #region StatusPosition

        /// <summary>
        /// Gets or sets the coordinates shown in the "Position" panel of the hosted <see
        /// cref="StatusBar"/>.</summary>
        /// <value>
        /// The coordinates that are shown in the "Position" panel of the hosted <see
        /// cref="StatusBar"/>. The default is <see cref="Site.InvalidLocation"/>.</value>
        /// <remarks>
        /// <b>StatusPosition</b> clears the "Position" panel if either or both coordinates are
        /// negative.</remarks>

        public PointI StatusPosition {
            [DebuggerStepThrough]
            get { return this._statusPosition; }
            set {
                this._statusPosition = value;

                if (value.X < 0 || value.Y < 0)
                    StatusPositionFormat.Text = "";
                else
                    StatusPositionFormat.Show(ApplicationInfo.Culture, value.X, value.Y);
            }
        }

        #endregion
        #endregion
        #region Private Methods
        #region CreateEntityRows

        /// <summary>
        /// Adds one row for each <see cref="Entity"/> in the specified collection to the "Entity"
        /// <see cref="ListView"/>.</summary>
        /// <param name="entities">
        /// An <see cref="IList{Entity}"/> containing the entities to show.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="entities"/> is a null reference.</exception>
        /// <remarks>
        /// <b>CreateEntityRows</b> adds one <see cref="EntityListItem"/> for each of the specified
        /// <paramref name="entities"/> to the "Entity" list view, but first adds a <see
        /// cref="Separator"/> if the list view already contains any items.</remarks>

        private void CreateEntityRows(IList<Entity> entities) {
            if (entities == null)
                ThrowHelper.ThrowArgumentNullException("entities");

            // insert separator before first entity?
            bool addSeparator = (entities.Count > 0);

            // process all variables defined by the scenario
            for (int i = 0; i < entities.Count; i++) {
                Entity entity = entities[i];

                // insert separator if required
                if (addSeparator) {
                    ApplicationUtility.AddSeparator(EntityList);
                    addSeparator = false;
                }

                EntityList.Items.Add(new EntityListItem(entity.Id, entity.Name));
            }
        }

        #endregion
        #region CreateOwnerLinks

        /// <summary>
        /// Shows the owner and unit owner of the specified <see cref="Site"/> in the "Site Owner"
        /// and "Unit Owner" <see cref="Hyperlink"/> controls.</summary>
        /// <param name="site">
        /// The <see cref="Site"/> whose owners to show.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="site"/> is a null reference.</exception>
        /// <remarks>
        /// The <see cref="FrameworkContentElement.Tag"/> property of each <see cref="Hyperlink"/>
        /// control holds the <see cref="FactionClass"/> that owns the specified <paramref
        /// name="site"/> and its <see cref="Site.Units"/>, respectively.</remarks>

        private void CreateOwnerLinks(Site site) {
            if (site == null)
                ThrowHelper.ThrowArgumentNullException("site");

            // determine site owner
            Faction siteOwner = site.Owner;
            if (siteOwner == null)
                SiteOwnerInfo.Text = Global.Strings.LabelOwnerNone;
            else {
                // show site owner
                SiteOwnerLink.Inlines.Add(siteOwner.ToString());
                SiteOwnerLink.Tag = siteOwner.FactionClass;
            }

            // determine unit owner
            if (site.Units.Count == 0)
                UnitOwnerInfo.Text = Global.Strings.LabelPresentNone;
            else {
                Faction unitOwner = site.Units[0].Owner;
                if (unitOwner == siteOwner)
                    UnitOwnerInfo.Text = Global.Strings.LabelOwnerSame;
                else if (unitOwner != null) {
                    // show unit owner
                    UnitOwnerLink.Inlines.Add(unitOwner.ToString());
                    UnitOwnerLink.Tag = unitOwner.FactionClass;
                }
            }
        }

        #endregion
        #region ShowSite

        /// <summary>
        /// Updates the data view to reflect the specified <see cref="Site"/>.</summary>
        /// <param name="location">
        /// The coordinates of the <see cref="Site"/> whose data to show.</param>
        /// <remarks><para>
        /// <b>ShowSite</b> updates all data view controls with the data of the <see cref="Site"/>
        /// at the specified <paramref name="location"/>. If the current <see cref="Session.State"/>
        /// equals <see cref="SessionState.Human"/>, <b>ShowSite</b> also updates implicit <see
        /// cref="TargetSelection"/> to reflect the new entity selection.
        /// </para><para>
        /// <b>ShowSite</b> clears the data view if the current <see cref="Session"/> instance is a
        /// null reference, or if <paramref name="location"/> is not a valid map location.
        /// </para></remarks>

        private void ShowSite(PointI location) {
            Session session = Session.Instance;
            Site site = null;

            // get site at specified coordinates
            if (session != null)
                site = session.WorldState.GetSite(location);

            // map illegal values to InvalidLocation
            if (site == null) location = Site.InvalidLocation;

            // show selected site
            SelectionGroup.Header = Site.FormatLabel(location);

            // clear site & unit owner
            SiteOwnerInfo.Text = null;
            SiteOwnerLink.Inlines.Clear();
            UnitOwnerInfo.Text = null;
            UnitOwnerLink.Inlines.Clear();

            // clear site list views
            EntityList.Items.Clear();
            PropertyList.ShowEntity(null);

            if (site == null) {
                SelectionGroup.IsEnabled = false;

                // update implicit target selection
                if (Session.State == SessionState.Human)
                    Session.Instance.TargetSelection.Begin(false);
            }
            else {
                SelectionGroup.IsEnabled = true;

                // show site & unit owner
                CreateOwnerLinks(site);

                // show all site contents
                CreateEntityRows(site.Terrains);
                CreateEntityRows(site.Effects);
                CreateEntityRows(site.Units);

                int count = EntityList.Items.Count;
                if (count > 0) {
                    // refresh scroll range for new item count
                    EntityList.ScrollIntoView(EntityList.Items[0]);
                    EntityList.UpdateLayout();

                    // select topmost unit/effect/terrain
                    EntityList.SelectAndShow(count - 1);
                }
            }
        }

        #endregion
        #endregion
        #region Public Methods
        #region BeginWait

        /// <summary>
        /// Shows the specified <see cref="StatusBar"/> message and enters wait mode.</summary>
        /// <param name="message">
        /// The message to show in the "Message" panel of the hosted <see cref="StatusBar"/>.
        /// </param>
        /// <remarks><para>
        /// <b>BeginWait</b> pushes the specified <paramref name="message"/> on the stack of the
        /// <see cref="StackTextBlock"/> that represents the "Message" panel, and sets the <see
        /// cref="Mouse.OverrideCursor"/> to the <see cref="Cursors.Wait"/> cursor.
        /// </para><para>
        /// Call <b>BeginWait</b> at the start of lengthy non-interactive operations, such as
        /// loading a scenario file, and call <see cref="EndWait"/> when the operation has finished.
        /// You may call <b>BeginWait</b> repeatedly, as long as there is a matching or greater
        /// number of <see cref="EndWait"/> calls.</para></remarks>

        public void BeginWait(string message) {

            // show hourglass cursor on first call
            if (++this._waitCalls == 1) {
                Mouse.OverrideCursor = Cursors.Wait;
                CommandManager.InvalidateRequerySuggested();
            }

            // show new message
            StatusMessage.Push(message);
            Dispatcher.DoEvents();
        }

        #endregion
        #region ComputerEndTurn

        /// <summary>
        /// Handles the end of the interactive replay of a computer player's turn.</summary>
        /// <remarks><para>
        /// If a <see cref="UserAction"/> is currently executing, <b>ComputerEndTurn</b> immediately
        /// returns and waits to be called back.
        /// </para><para>
        /// Otherwise, <b>ComputerEndTurn</b> performs the following actions to end the active
        /// computer player's turn once its commands have been replayed:
        /// </para><list type="number"><item>
        /// Issue an <see cref="World.Commands.EndTurnCommand"/> for the active faction.
        /// </item><item>
        /// Save the resulting <see cref="Session.WorldState"/> to the predefined <see
        /// cref="SessionFileType.Computer"/> session file.
        /// </item><item>
        /// Call <see cref="Session.Dispatch"/> to activate the next faction.
        /// </item></list><para>
        /// Steps 1 and 2 are skipped if the <see cref="WorldState.GameOver"/> property of the
        /// current <see cref="Session.WorldState"/> is already <c>true</c>.</para></remarks>

        public static void ComputerEndTurn() {
            if (Session.State != SessionState.Computer) return;

            // wait for callback if busy
            if (UserAction.IsBusy) return;

            AsyncAction.BeginRun(delegate {
                Action postAction;

                Session session = Session.Instance;
                if (!session.WorldState.GameOver) {

                    // issue EndTurn command
                    if (!session.Executor.ExecuteEndTurn(session.WorldState)) {
                        postAction = () => Session.Close(false);
                        goto finished;
                    }

                    // autosave game after executing command
                    string path = FilePaths.GetSessionFile(SessionFileType.Computer).AbsolutePath;
                    AsyncAction.Invoke(() => session.Save(ref path, false));
                }

                // dispatch game to next player
                postAction = session.Dispatch;

            finished:
                AsyncAction.BeginInvoke(delegate { postAction(); AsyncAction.EndRun(); });
            });
        }

        #endregion
        #region EndWait

        /// <summary>
        /// Shows the previous <see cref="StatusBar"/> message and leaves wait mode.</summary>
        /// <remarks><para>
        /// <b>EndWait</b> pops the current message from the stack of the <see
        /// cref="StackTextBlock"/> that represents the "Message" panel and clears the <see
        /// cref="Mouse.OverrideCursor"/>.
        /// </para><para>
        /// You must call <b>EndWait</b> at least once for every <see cref="BeginWait"/> call. Any
        /// additional <b>EndWait</b> calls are ignored.</para></remarks>

        public void EndWait() {

            // ignore excess calls
            if (this._waitCalls == 0) return;

            // restore previous message
            StatusMessage.Pop();

            // restore default cursor on last call
            if (--this._waitCalls == 0) {
                Mouse.OverrideCursor = null;
                CommandManager.InvalidateRequerySuggested();
            }
        }

        #endregion
        #region SelectNextUnit

        /// <summary>
        /// Sets the <see cref="SelectedEntity"/> property to the next <see cref="Unit"/> in the
        /// "Site Contents" <see cref="ListView"/>.</summary>
        /// <remarks><para>
        /// <b>SelectNextUnit</b> selects the first <see cref="Unit"/> in the "Site Contents" list
        /// view if there is no current selection, or if the current selection is an <see
        /// cref="Entity"/> of another <see cref="EntityCategory"/>.
        /// </para><para>
        /// Otherwise, <b>SelectNextUnit</b> selects the next <see cref="Unit"/> after the currently
        /// selected unit, cycling back to the first unit after the last unit in the list view.
        /// </para><para>
        /// <b>SelectNextUnit</b> does nothing if the "Site Contents" list view does not contain any
        /// <see cref="Unit"/> entries.</para></remarks>

        public void SelectNextUnit() {
            if (Session.Instance == null) return;

            // get selected site
            Site site = Session.Instance.SelectedSite;

            // determine index of first unit entry
            int firstUnit = -1;
            for (int i = 0; i < EntityList.Items.Count; i++) {
                object item = EntityList.Items[i];
                if (!(item is EntityListItem)) continue;

                // get current entity reference
                Entity entity = site.GetEntity(((EntityListItem) item).Item1);
                Debug.Assert(entity != null);

                if (entity.Category == EntityCategory.Unit) {
                    firstUnit = i;
                    break;
                }
            }

            // quit if no units present
            if (firstUnit < 0) return;

            // retrieve selected index, if any
            int selected = EntityList.SelectedIndex;

            // select first or next unit entry
            if (selected < firstUnit)
                selected = firstUnit;
            else {
                int range = EntityList.Items.Count - firstUnit;
                selected = firstUnit + ((selected - firstUnit + 1) % range);
            }

            // ensure selected unit is visible
            EntityList.SelectAndShow(selected);
        }

        #endregion
        #region UpdateSelection

        /// <summary>
        /// Updates the data view to reflect the current <see cref="Session.SelectedSite"/> and <see
        /// cref="SelectedEntity"/>.</summary>
        /// <remarks><para>
        /// <b>UpdateSelection</b> rebuilds the "Site Contents" and "Property" list views from the
        /// current <see cref="Session.SelectedSite"/> and <see cref="SelectedEntity"/> values.
        /// </para><para>
        /// Call this method to reflect changes in the current <see cref="Session.WorldState"/> that
        /// were not the result of user input, e.g. during a replay.</para></remarks>

        public void UpdateSelection() {

            if (Session.Instance != null &&
                Session.Instance.SelectedSite != Site.InvalidLocation) {

                string entity = SelectedEntity;
                ShowSite(Session.Instance.SelectedSite);
                SelectedEntity = entity;
            }
        }

        #endregion
        #region UpdateTurnFaction

        /// <summary>
        /// Updates the contents of the "Turn/Faction" <see cref="GroupBox"/> to reflect the current
        /// <see cref="Session"/>.</summary>
        /// <remarks>
        /// <b>UpdateTurnFaction</b> shows the data of the current <see cref="Session"/> instance,
        /// if any, in the "Turn/Faction" group box, and clears its contents if no data exists.
        /// </remarks>

        public void UpdateTurnFaction() {

            // retrieve current world state, if any
            WorldState world = (Session.State == SessionState.Invalid ?
                null : Session.Instance.WorldState);

            // clear controls if no session active
            if (world == null || world.ActiveFaction == null) {
                TurnFactionGroup.Header = Global.Strings.InfoTurnFactionNone;
                FactionInfo.Content = "";
                FactionColorInfo.Fill = Background;
                return;
            }

            // show turn and faction count
            TurnFactionGroup.Header = String.Format(
                ApplicationInfo.Culture, Global.Strings.InfoTurnFaction,
                world.CurrentTurn + 1, world.History.FullTurns + 1,
                world.ActiveFactionIndex + 1, world.Factions.Count);

            // show faction name and color
            FactionInfo.Content = world.ActiveFaction.Name;
            FactionColorInfo.Fill = MediaObjects.GetGradientBrush(world.ActiveFaction.Color);
        }

        #endregion
        #endregion
        #region Command Event Handlers
        #region CommandCanExecute

        /// <summary>
        /// Handles the <see cref="CommandBinding.CanExecute"/> event for any <see
        /// cref="RoutedUICommand"/> that is always available.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="CanExecuteRoutedEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>CommandCanExecute</b> sets the <see cref="CanExecuteRoutedEventArgs.CanExecute"/>
        /// property of the specified <paramref name="args"/> to <c>true</c> exactly if there are no
        /// pending <see cref="EndWait"/> calls.</remarks>

        private void CommandCanExecute(object sender, CanExecuteRoutedEventArgs args) {
            args.Handled = true;
            args.CanExecute = (this._waitCalls == 0);
        }

        #endregion
        #region Session...CanExecute

        private void SessionValidCanExecute(object sender, CanExecuteRoutedEventArgs args) {
            args.Handled = true;
            args.CanExecute = (this._waitCalls == 0 && Session.State != SessionState.Invalid);
        }

        private void SessionClosedCanExecute(object sender, CanExecuteRoutedEventArgs args) {
            args.Handled = true;
            args.CanExecute = (this._waitCalls == 0 &&
                (Session.State == SessionState.Human || Session.State == SessionState.Closed));
        }

        private void SessionComputerCanExecute(object sender, CanExecuteRoutedEventArgs args) {
            args.Handled = true;
            args.CanExecute = (this._waitCalls == 0 && Session.State == SessionState.Computer);
        }

        private void SessionHumanCanExecute(object sender, CanExecuteRoutedEventArgs args) {
            args.Handled = true;
            args.CanExecute = (this._waitCalls == 0 && Session.State == SessionState.Human);
        }

        private void SessionReplayCanExecute(object sender, CanExecuteRoutedEventArgs args) {
            args.Handled = true;
            args.CanExecute = (this._waitCalls == 0 && Session.State == SessionState.Replay);
        }

        private void SessionReplayStopCanExecute(object sender, CanExecuteRoutedEventArgs args) {
            args.Handled = true;
            args.CanExecute = (this._waitCalls == 0 &&
                (Session.State == SessionState.Command || Session.State == SessionState.Replay));
        }

        private void SessionSelectionCanExecute(object sender, CanExecuteRoutedEventArgs args) {
            args.Handled = true;
            args.CanExecute = (this._waitCalls == 0 &&
                (Session.State == SessionState.Human || Session.State == SessionState.Selection));
        }

        #endregion
        #region File..Executed

        private void FileNewExecuted(object sender, ExecutedRoutedEventArgs args) {
            args.Handled = true;
            UserAction.TryRun(() => Session.Create(null));
        }

        private void FileOpenExecuted(object sender, ExecutedRoutedEventArgs args) {
            args.Handled = true;
            UserAction.TryRun(() => Session.Open(null));
        }

        private void FileCloseExecuted(object sender, ExecutedRoutedEventArgs args) {
            args.Handled = true;
            UserAction.TryRun(() => Session.Close(true));
        }

        private void FileSaveExecuted(object sender, ExecutedRoutedEventArgs args) {
            args.Handled = true;
            if (Session.State == SessionState.Invalid) return;

            UserAction.TryRun(delegate {
                WorldState world = Session.Instance.WorldState;

                // default name shows scenario title, turn & faction
                string path = String.Format(ApplicationInfo.Culture,
                    "{0} (T{1} F{2})", MasterSection.Instance.Title,
                    world.CurrentTurn + 1, world.ActiveFactionIndex + 1);

                // allow user to change this name
                Session.Instance.Save(ref path, true);
            });
        }

        private void FileExitExecuted(object sender, ExecutedRoutedEventArgs args) {
            args.Handled = true;
            Close();
        }

        #endregion
        #region Game...Executed

        private void GamePlayersExecuted(object sender, ExecutedRoutedEventArgs args) {
            args.Handled = true;
            if (Session.State == SessionState.Invalid) return;

            // ChangePlayers may call Dispatch
            UserAction.TryRun(Session.Instance.ChangePlayers);
        }

        private void GameStopComputerExecuted(object sender, ExecutedRoutedEventArgs args) {
            args.Handled = true;
            if (Session.State != SessionState.Computer) return;

            // Stop returns true if active thread was stopped
            if (PlayerManager.Instance.ComputerThread.Stop())
                MessageBox.Show(this, Global.Strings.DialogComputerStopped,
                    Session.Instance.WorldState.ActiveFaction.Name,
                    MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void GameWaitCycleExecuted(object sender, ExecutedRoutedEventArgs args) {
            args.Handled = true;
            if (Session.State == SessionState.Human)
                HumanAction.SelectUnit(null, false, true);
        }

        private void GameSkipCycleExecuted(object sender, ExecutedRoutedEventArgs args) {
            args.Handled = true;
            if (Session.State == SessionState.Human)
                HumanAction.SelectUnit(null, true, true);
        }

        private void GameUnskipAllExecuted(object sender, ExecutedRoutedEventArgs args) {
            args.Handled = true;
            if (Session.State == SessionState.Human)
                HumanAction.SkippedUnits.Clear();
        }

        #endregion
        #region Command...Executed

        private void CommandAttackExecuted(object sender, ExecutedRoutedEventArgs args) {
            args.Handled = true;
            if (Session.State == SessionState.Human)
                HumanAction.Attack();
        }

        private void CommandMoveExecuted(object sender, ExecutedRoutedEventArgs args) {
            args.Handled = true;
            if (Session.State == SessionState.Human)
                HumanAction.Move();
        }

        private void CommandBuildExecuted(object sender, ExecutedRoutedEventArgs args) {
            args.Handled = true;
            if (Session.State == SessionState.Human)
                HumanAction.Build();
        }

        private void CommandEntitiesExecuted(object sender, ExecutedRoutedEventArgs args) {
            args.Handled = true;
            if (Session.State == SessionState.Human)
                HumanAction.ManageEntities(0);
        }

        private void CommandSiteExecuted(object sender, ExecutedRoutedEventArgs args) {
            args.Handled = true;
            if (Session.State == SessionState.Human)
                HumanAction.ManageEntities(Dialog.ShowEntitiesMode.Site);
        }

        private void CommandEndTurnExecuted(object sender, ExecutedRoutedEventArgs args) {
            args.Handled = true;
            if (Session.State == SessionState.Human)
                HumanAction.EndTurn();
        }

        private void CommandResignExecuted(object sender, ExecutedRoutedEventArgs args) {
            args.Handled = true;
            if (Session.State == SessionState.Human)
                HumanAction.Resign();
        }

        #endregion
        #region Info...Executed

        private void InfoScenarioExecuted(object sender, ExecutedRoutedEventArgs args) {
            args.Handled = true;
            if (MasterSection.Instance == null) return;

            // show "About Scenario" dialog
            var dialog = new Dialog.AboutScenario() { Owner = this };
            dialog.ShowDialog();
        }

        private void InfoClassesExecuted(object sender, ExecutedRoutedEventArgs args) {
            args.Handled = true;
            if (MasterSection.Instance == null) return;

            // show "Entity Classes" dialog
            var dialog = new Dialog.ShowClasses() { Owner = this };
            dialog.ShowDialog();
        }

        private void InfoVariablesExecuted(object sender, ExecutedRoutedEventArgs args) {
            args.Handled = true;
            if (MasterSection.Instance == null) return;

            // check if scenario defines any variables
            var variables = MasterSection.Instance.Variables;
            if (variables.Attributes.Count == 0 && variables.Resources.Count == 0) {
                MessageBox.Show(this,
                    Global.Strings.DialogVariablesNone, Global.Strings.TitleVariables,
                    MessageBoxButton.OK, MessageBoxImage.Information);
            } else {
                // show "Variables" dialog
                var dialog = new Dialog.ShowVariables(null) { Owner = this };
                dialog.ShowDialog();
            }
        }

        private void InfoFactionsExecuted(object sender, ExecutedRoutedEventArgs args) {
            args.Handled = true;
            if (Session.State == SessionState.Invalid) return;

            // show "Faction Status" dialog
            UserAction.TryRun(delegate {
                var dialog = new Dialog.ShowFactions(Session.MapView, null) { Owner = this };
                dialog.ShowDialog();
            });
        }

        private void InfoRankingExecuted(object sender, ExecutedRoutedEventArgs args) {
            args.Handled = true;
            if (Session.State == SessionState.Invalid) return;

            // show "Faction Ranking" dialog
            UserAction.TryRun(delegate {
                var dialog = new Dialog.ShowRanking(Session.MapView, false);
                dialog.Owner = this;
                dialog.ShowDialog();
            });
        }

        private void InfoCommandsExecuted(object sender, ExecutedRoutedEventArgs args) {
            args.Handled = true;
            if (Session.State == SessionState.Invalid) return;

            // show "Command History" dialog
            UserAction.TryRun(delegate {
                var dialog = new Dialog.ShowCommands(Session.Instance.WorldState.History);
                dialog.Owner = this;
                dialog.ShowDialog();
            });
        }

        private void InfoPlacementsExecuted(object sender, ExecutedRoutedEventArgs args) {
            args.Handled = true;
            if (Session.State != SessionState.Human) return;

            // show "Placement Sites" dialog for active faction
            UserAction.TryRun(delegate {
                WorldState world = Session.Instance.WorldState;
                using (var dialog = new Dialog.ShowPlacements(
                    world, world.ActiveFaction, EntityCategory.Unit)) {
                    dialog.Owner = this;
                    dialog.ShowDialog();
                }
            });
        }

        #endregion
        #region Replay...Executed

        private void ReplayAllExecuted(object sender, ExecutedRoutedEventArgs args) {
            args.Handled = true;

            // replay from first turn
            if (Session.State == SessionState.Human || Session.State == SessionState.Closed)
                Session.Instance.Replay.Start(0, 0);
        }

        private void ReplayLastExecuted(object sender, ExecutedRoutedEventArgs args) {
            args.Handled = true;
            if (Session.State != SessionState.Human && Session.State != SessionState.Closed)
                return;

            // start with first faction in current turn
            int turn = Session.Instance.WorldState.CurrentTurn;
            int faction = 0;

            if (turn > 0) {
                // start with next faction in previous turn
                --turn;
                faction = Session.Instance.WorldState.ActiveFactionIndex + 1;
            }

            Session.Instance.Replay.Start(turn, faction);
        }

        private void ReplayFromExecuted(object sender, ExecutedRoutedEventArgs args) {
            args.Handled = true;

            // replay from specified turn
            if (Session.State == SessionState.Human || Session.State == SessionState.Closed)
                Session.Instance.Replay.Start(-1, 0);
        }

        private void ReplayPauseExecuted(object sender, ExecutedRoutedEventArgs args) {
            args.Handled = true;

            // pause or unpause ongoing replay
            ReplayManager replay = Session.Instance.Replay;
            switch (replay.CurrentState) {

                case ReplayState.Play:
                    replay.RequestState(ReplayState.Pause);
                    break;

                case ReplayState.Pause:
                    replay.RequestState(ReplayState.Play);
                    break;
            }
        }

        private void ReplaySkipExecuted(object sender, ExecutedRoutedEventArgs args) {
            args.Handled = true;
            Session.Instance.Replay.RequestState(ReplayState.Skip);
        }

        private void ReplayStopExecuted(object sender, ExecutedRoutedEventArgs args) {
            args.Handled = true;

            if (Session.State == SessionState.Command)
                UserAction.TryRun(delegate { });
            else if (Session.State == SessionState.Replay)
                Session.Instance.Replay.RequestState(ReplayState.Stop);
        }

        private void ReplayScrollExecuted(object sender, ExecutedRoutedEventArgs args) {
            args.Handled = true;
            bool scroll = ApplicationOptions.Instance.Game.Replay.Scroll;
            ApplicationOptions.Instance.Game.Replay.Scroll = !scroll;
        }

        #endregion
        #region ReplaySpeed...Executed

        private void ReplaySpeedSlowExecuted(object sender, ExecutedRoutedEventArgs args) {
            args.Handled = true;
            ApplicationOptions.Instance.Game.Replay.Speed = ReplaySpeed.Slow;
        }

        private void ReplaySpeedMediumExecuted(object sender, ExecutedRoutedEventArgs args) {
            args.Handled = true;
            ApplicationOptions.Instance.Game.Replay.Speed = ReplaySpeed.Medium;
        }

        private void ReplaySpeedFastExecuted(object sender, ExecutedRoutedEventArgs args) {
            args.Handled = true;
            ApplicationOptions.Instance.Game.Replay.Speed = ReplaySpeed.Fast;
        }

        private void ReplaySpeedTurboExecuted(object sender, ExecutedRoutedEventArgs args) {
            args.Handled = true;
            ApplicationOptions.Instance.Game.Replay.Speed = ReplaySpeed.Turbo;
        }

        #endregion
        #region ViewCommandCanExecute

        /// <summary>
        /// Handles the <see cref="CommandBinding.CanExecute"/> event for any <see
        /// cref="RoutedUICommand"/> in the "View" <see cref="Menu"/> that operates on the default
        /// <see cref="Session.MapView"/>.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="CanExecuteRoutedEventArgs"/> object containing event data.</param>

        private void ViewCommandCanExecute(object sender, CanExecuteRoutedEventArgs args) {
            args.Handled = true;

            // enable map view commands if default map view exists
            args.CanExecute = (Session.State != SessionState.Command
                && this._waitCalls == 0 && Session.MapView != null);

            // enable Animation only if supported by map view manager
            if (args.CanExecute && args.Command == Resources["viewAnimationCommand"])
                args.CanExecute = MapViewManager.Instance.Animation;
        }

        #endregion
        #region View...Executed (MapView)

        private void ViewAnimationExecuted(object sender, ExecutedRoutedEventArgs args) {
            args.Handled = true;
            MapView mapView = Session.MapView;
            if (mapView != null)
                mapView.Animation = !mapView.Animation;
        }

        private void ViewShowFlagsExecuted(object sender, ExecutedRoutedEventArgs args) {
            args.Handled = true;
            MapView mapView = Session.MapView;
            if (mapView != null)
                mapView.ShowFlags = !mapView.ShowFlags;
        }

        private void ViewShowGridExecuted(object sender, ExecutedRoutedEventArgs args) {
            args.Handled = true;
            MapView mapView = Session.MapView;
            if (mapView != null)
                mapView.ShowGrid = !mapView.ShowGrid;
        }

        private void ViewShowOwnerExecuted(object sender, ExecutedRoutedEventArgs args) {
            args.Handled = true;
            MapView mapView = Session.MapView;
            if (mapView != null)
                mapView.ShowOwner = !mapView.ShowOwner;
        }

        private void ViewShowGaugesExecuted(object sender, ExecutedRoutedEventArgs args) {
            args.Handled = true;
            MapView mapView = Session.MapView;
            if (mapView == null) return;

            // display "Show Gauges" dialog
            var dialog = new ShowGauges(mapView.GaugeResource, mapView.GaugeResourceFlags);
            dialog.Owner = this;
            if (dialog.ShowDialog() == true)
                mapView.ShowGauges(dialog.Resource, dialog.ResourceFlags);
        }

        private void ViewShowVariableExecuted(object sender, ExecutedRoutedEventArgs args) {
            args.Handled = true;
            MapView mapView = Session.MapView;
            if (mapView == null) return;

            // display "Show Variable" dialog
            var dialog = new ShowVariable(mapView.ShownVariable, mapView.ShownVariableFlags);
            dialog.Owner = this;
            if (dialog.ShowDialog() == true)
                mapView.ShowVariable(dialog.Variable, dialog.VariableFlags);
        }

        private void ViewCenterSiteExecuted(object sender, ExecutedRoutedEventArgs args) {
            args.Handled = true;
            MapView mapView = Session.MapView;
            if (mapView != null)
                mapView.CenterOn(mapView.SelectedSite);
        }

        #endregion
        #region ViewZoom...Executed

        private void ViewZoomStdExecuted(object sender, RoutedEventArgs args) {
            args.Handled = true;
            MapView mapView = Session.MapView;

            // set standard map scale of 100%
            if (mapView != null && mapView.Scale != 100) {
                BeginWait(Global.Strings.StatusZoomChanging);
                mapView.Scale = 100;
                EndWait();
            }
        }

        private void ViewZoomInExecuted(object sender, RoutedEventArgs args) {
            args.Handled = true;
            MapView mapView = Session.MapView;
            if (mapView == null) return;

            // determine index of active map scale
            IList<Int32> scales = ApplicationOptions.Instance.View.MapScales;
            int index = scales.IndexOf(mapView.Scale);

            // set next higher map scale if possible
            if (index >= 0 && index < scales.Count - 1) {
                BeginWait(Global.Strings.StatusZoomChanging);
                mapView.Scale = scales[index + 1];
                EndWait();
            }
        }

        private void ViewZoomOutExecuted(object sender, RoutedEventArgs args) {
            args.Handled = true;
            MapView mapView = Session.MapView;
            if (mapView == null) return;

            // determine index of active map scale
            IList<Int32> scales = ApplicationOptions.Instance.View.MapScales;
            int index = scales.IndexOf(mapView.Scale);

            // set next lower map scale if possible
            if (index > 0) {
                BeginWait(Global.Strings.StatusZoomChanging);
                mapView.Scale = scales[index - 1];
                EndWait();
            }
        }

        #endregion
        #region ViewTheme...Executed

        private void ViewThemeSystemExecuted(object sender, ExecutedRoutedEventArgs args) {
            args.Handled = true;
            ApplicationOptions.Instance.View.Theme = DefaultTheme.System;
        }

        private void ViewThemeClassicExecuted(object sender, ExecutedRoutedEventArgs args) {
            args.Handled = true;
            ApplicationOptions.Instance.View.Theme = DefaultTheme.Classic;
        }

        private void ViewThemeLunaExecuted(object sender, ExecutedRoutedEventArgs args) {
            args.Handled = true;
            ApplicationOptions.Instance.View.Theme = DefaultTheme.Luna;
        }

        private void ViewThemeLunaHomesteadExecuted(object sender, ExecutedRoutedEventArgs args) {
            args.Handled = true;
            ApplicationOptions.Instance.View.Theme = DefaultTheme.LunaHomestead;
        }

        private void ViewThemeLunaMetallicExecuted(object sender, ExecutedRoutedEventArgs args) {
            args.Handled = true;
            ApplicationOptions.Instance.View.Theme = DefaultTheme.LunaMetallic;
        }

        private void ViewThemeRoyaleExecuted(object sender, ExecutedRoutedEventArgs args) {
            args.Handled = true;
            ApplicationOptions.Instance.View.Theme = DefaultTheme.Royale;
        }

        private void ViewThemeAeroExecuted(object sender, ExecutedRoutedEventArgs args) {
            args.Handled = true;
            ApplicationOptions.Instance.View.Theme = DefaultTheme.Aero;
        }

        private void ViewThemeAero2Executed(object sender, ExecutedRoutedEventArgs args) {
            args.Handled = true;
            ApplicationOptions.Instance.View.Theme = DefaultTheme.Aero2;
        }

        private void ViewThemeAeroLiteExecuted(object sender, ExecutedRoutedEventArgs args) {
            args.Handled = true;
            ApplicationOptions.Instance.View.Theme = DefaultTheme.AeroLite;
        }

        #endregion
        #region View...Executed (Other)

        private void ViewBitmapGridExecuted(object sender, ExecutedRoutedEventArgs args) {
            args.Handled = true;
            ApplicationOptions.Instance.View.BitmapGrid = !ApplicationOptions.Instance.View.BitmapGrid;
            if (MapViewManager.Instance != null)
                MapViewManager.Instance.RedrawAll();
        }

        private void ViewOpaqueImagesExecuted(object sender, ExecutedRoutedEventArgs args) {
            args.Handled = true;
            ApplicationOptions.Instance.View.OpaqueImages = !ApplicationOptions.Instance.View.OpaqueImages;
            if (MapViewManager.Instance != null)
                MapViewManager.Instance.RedrawAll();
        }

        private void ViewStaticArrowsExecuted(object sender, ExecutedRoutedEventArgs args) {
            args.Handled = true;
            ApplicationOptions.Instance.View.StaticArrows = !ApplicationOptions.Instance.View.StaticArrows;
            if (MapViewManager.Instance != null)
                MapViewManager.Instance.UpdateAllArrows();
        }

        private void ViewStaticMarkerExecuted(object sender, ExecutedRoutedEventArgs args) {
            args.Handled = true;
            ApplicationOptions.Instance.View.StaticMarker = !ApplicationOptions.Instance.View.StaticMarker;
            if (MapViewManager.Instance != null)
                MapViewManager.Instance.UpdateAllMarkers();
        }

        private void ViewSaveWindowExecuted(object sender, ExecutedRoutedEventArgs args) {
            args.Handled = true;
            ApplicationOptions.Instance.View.SaveDesktop(this);
        }

        #endregion
        #region Debug...Executed

        private void DebugAreasExecuted(object sender, ExecutedRoutedEventArgs args) {
            args.Handled = true;
            if (Session.State == SessionState.Invalid) return;
            UserAction.TryRun(delegate {

                // file name shows scenario title, turn & faction
                WorldState world = Session.Instance.WorldState;
                string file = String.Format(ApplicationInfo.Culture,
                    "{0} (T{1} F{2})", MasterSection.Instance.Title,
                    world.CurrentTurn + 1, world.ActiveFactionIndex + 1);

                // save session map to Areas section file
                BeginWait(Global.Strings.StatusAreasSaving);
                GameUtility.SaveAreas(file);
                EndWait();
            });
        }

        private void DebugScenarioExecuted(object sender, ExecutedRoutedEventArgs args) {
            args.Handled = true;
            if (Session.State == SessionState.Invalid) return;

            // save scenario description to debug file
            UserAction.TryRun(delegate {
                RootedPath path = FilePaths.GetScenarioFile(Global.ScenarioFileType.Debug);
                BeginWait(Global.Strings.StatusScenarioSaving);
                GameUtility.SaveScenario(path.AbsolutePath);
                EndWait();
            });
        }

        private void DebugBitmapsExecuted(object sender, ExecutedRoutedEventArgs args) {
            args.Handled = true;

            // show unscaled map view manager catalog
            if (MapViewManager.Instance != null)
                MapViewManager.Instance.ShowCatalog(Global.Strings.TitleCatalogOriginal);

            // show scaled default map view catalog
            if (Session.MapView != null)
                Session.MapView.ShowCatalog(String.Format(ApplicationInfo.Culture,
                    Global.Strings.TitleCatalogScaled, Session.MapView.Scale));
        }

        private void DebugCountersExecuted(object sender, ExecutedRoutedEventArgs args) {
            args.Handled = true;

            // show "Command Counters" dialog
            UserAction.TryRun(delegate {
                string message = String.Format(ApplicationInfo.Culture,
                    Global.Strings.DialogCommandCounters, Command.Counters.ToArray());

                MessageBox.Show(this, message,
                    Global.Strings.TitleCommandCounters, MessageBoxButton.OK);
            });
        }

        private void DebugEventsExecuted(object sender, ExecutedRoutedEventArgs args) {
            args.Handled = true;
            if (Session.State == SessionState.Invalid) return;

            // show "Event History" dialog
            UserAction.TryRun(delegate {
                var dialog = new Dialog.ShowEvents(Session.Instance.WorldState.History);
                dialog.Owner = this;
                dialog.ShowDialog();
            });
        }

        #endregion
        #region Help...Executed

        private void HelpExecuted(object sender, ExecutedRoutedEventArgs args) {
            args.Handled = true;
            string page = null;

            // show help for current menu item, if any
            int index = this._menuItems.Count - 1;
            if (index >= 0)
                page = (string) this._menuItems[index].Tag;

            ApplicationUtility.ShowHelp(page);
        }

        private void HelpReadMeExecuted(object sender, ExecutedRoutedEventArgs args) {
            args.Handled = true;
            ApplicationUtility.StartFile(this, "ReadMe.html");
        }

        private void HelpWhatsNewExecuted(object sender, ExecutedRoutedEventArgs args) {
            args.Handled = true;
            ApplicationUtility.StartFile(this, "WhatsNew.html");
        }

        private void HelpAboutExecuted(object sender, ExecutedRoutedEventArgs args) {
            args.Handled = true;
            var dialog = new AboutDialog() { Owner = this };
            dialog.ShowDialog();
        }

        #endregion
        #endregion
        #region Event Handlers
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
        /// If the event was not requested to <see cref="CancelEventArgs.Cancel"/> and there is a
        /// current <see cref="Session"/>, <b>OnClosing</b> handles the <see cref="Window.Closing"/>
        /// event by asking the user to confirm losing any unsaved changes to the current <see
        /// cref="Session"/>. 
        /// </para><para>
        /// If <see cref="UserAction.IsSessionBusy"/> is <c>true</c>, <b>OnClosing</b> always
        /// initially cancels the event and later re-raises it via <see cref="UserAction"/> if the
        /// user confirms losing unsaved changes.</para></remarks>

        protected override void OnClosing(CancelEventArgs args) {
            base.OnClosing(args);
            if (args.Cancel) return;

            // cancel event if session busy...
            if (UserAction.IsSessionBusy) {
                args.Cancel = true;

                // ... then ask user and re-raise event
                UserAction.TryRun(delegate {
                    if (Session.Close(true)) Close();
                });
            }
            else if (Session.State != SessionState.Invalid)
                UserAction.TryRun(delegate {
                    if (!Session.Close(true))
                        args.Cancel = true;
                });
        }

        #endregion
        #region OnComputerComplete

        /// <summary>
        /// Handles the <see cref="TaskEvents.TaskComplete"/> event for the <see
        /// cref="PlayerManager.ComputerThread"/>.</summary>
        /// <param name="sender">
        /// The <see cref="ComputerThread"/> object sending the event.</param>
        /// <param name="args">
        /// An <see cref="EventArgs"/> object containing event data.</param>
        /// <remarks><para>
        /// <b>OnComputerComplete</b> is called asynchronously by the background thread managed by
        /// the <see cref="PlayerManager.ComputerThread"/> object just before the thread terminates.
        /// </para><para>
        /// If a <see cref="UserAction"/> is currently executing, <b>OnComputerComplete</b> sets
        /// <see cref="IsComputerStopped"/> to <c>true</c> and immediately returns, waiting to be
        /// called back.
        /// </para><para>
        /// If a valid <see cref="Players.Algorithm.BestWorld"/> exists, <b>OnComputerComplete</b>
        /// starts an interactive replay of all commands generated by the computer player and
        /// returns. The next faction will be activated by <see cref="ComputerEndTurn"/> once the
        /// replay has finished.
        /// </para><para>
        /// Otherwise, it is assumed that the calculation was aborted, either deliberately or due to
        /// an error, and control of the active faction reverts to the (next) local human player.
        /// </para></remarks>

        public void OnComputerComplete(object sender, EventArgs args) {
            if (Session.State != SessionState.Computer) return;

            // wait for callback if busy
            if (UserAction.IsBusy) {
                IsComputerStopped = true;
                return;
            }

            // callback arrived or not required
            IsComputerStopped = false;

            // retrieve calculation results
            PlayerManager players = PlayerManager.Instance;
            WorldState world = players.ComputerThread.BestWorld;

            // replay computer player commands
            Session session = Session.Instance;
            if (world != null) {
                session.Replay.Start(world, true);
                return;
            }

            // error: get local human player
            HumanPlayer human = players.GetLocalHuman(session.WorldState, true);

            // revert control to local human player
            players.SetPlayer(session.WorldState.ActiveFaction, human);

            // player change requires saving
            session.SetWorldChanged();
            session.Dispatch();
        }

        #endregion
        #region OnComputerException

        /// <summary>
        /// Handles the <see cref="TaskEvents.TaskException"/> event for the <see
        /// cref="PlayerManager.ComputerThread"/>.</summary>
        /// <param name="sender">
        /// The <see cref="ComputerThread"/> object sending the event.</param>
        /// <param name="args">
        /// An <see cref="EventArgs{Exception}"/> object containing event data.</param>
        /// <remarks><para>
        /// <b>OnComputerException</b> is called synchronously by the background thread managed by
        /// the <see cref="PlayerManager.ComputerThread"/> object when an exception occurs.
        /// </para><para>
        /// <b>OnComputerException</b> displays the <see cref="Exception"/> value wrapped by the
        /// specified <paramref name="args"/> and announces that control of the active faction will
        /// revert to the local human player. The latter task is performed by <see
        /// cref="OnComputerComplete"/>.</para></remarks>

        public void OnComputerException(object sender, EventArgs<Exception> args) {
            if (Session.State != SessionState.Computer) return;

            MessageDialog.Show(this, Global.Strings.DialogComputerError,
                Session.Instance.WorldState.ActiveFaction.Name,
                args.Value, MessageBoxButton.OK, Images.Error);
        }

        #endregion
        #region OnComputerMessage

        /// <summary>
        /// Handles the <see cref="TaskEvents.TaskMessage"/> event for an <see cref="Algorithm"/>.
        /// </summary>
        /// <param name="sender">
        /// The <see cref="Algorithm"/> object sending the event.</param>
        /// <param name="args">
        /// A <see cref="MessageEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnComputerMessage</b> shows the <see cref="String"/> value wrapped by the specified
        /// <paramref name="args"/> in the <see cref="StatusBar"/>.</remarks>

        public void OnComputerMessage(object sender, MessageEventArgs args) {
            ComputerMessage.Text = args.Value;
        }

        #endregion
        #region OnContentRendered

        /// <summary>
        /// Raises and handles the <see cref="Window.ContentRendered"/> event.</summary>
        /// <param name="args">
        /// An <see cref="EventArgs"/> object containing event data.</param>
        /// <remarks><para>
        /// <b>OnContentRendered</b> raises the <see cref="Window.ContentRendered"/> event by
        /// calling the base class implementation of <see cref="Window.OnContentRendered"/> with the
        /// specified <paramref name="args"/>.
        /// </para><para>
        /// If any command line arguments were supplied to the <see cref="Application"/>,
        /// <b>OnContentRendered</b> then handles the event by calling <see
        /// cref="FilePaths.FindArgumentFile"/> on the first command line argument.
        /// </para><para>
        /// If <see cref="FilePaths.FindArgumentFile"/> recognizes a scenario or session XML
        /// file, that file is opened as usual. Otherwise, an error message is shown to the user.
        /// All other command line arguments are ignored.</para></remarks>

        protected override void OnContentRendered(EventArgs args) {
            base.OnContentRendered(args);

            // return immediately if command line empty
            string[] commandLine = Environment.GetCommandLineArgs();
            if (commandLine == null || commandLine.Length < 2)
                return;

            // dialog for invalid file type
            string dialog = Global.Strings.DialogFileInvalid;

            // attempt to process first argument
            string path = commandLine[1];
            switch (FilePaths.FindArgumentFile(ref path)) {

                case ArgumentFileType.Scenario:
                    Session.Create(path);
                    break;

                case ArgumentFileType.Session:
                    Session.Open(path);
                    goto default;

                case ArgumentFileType.Missing:
                    dialog = Global.Strings.DialogFileMissing;
                    goto default;

                default:
                    MessageBox.Show(this,
                        String.Format(ApplicationInfo.Culture, dialog, path),
                        Global.Strings.TitleCommandLineError,
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    break;
            }
        }

        #endregion
        #region OnDrop

        /// <summary>
        /// Raises and handles the <see cref="UIElement.Drop"/> event.</summary>
        /// <param name="args">
        /// A <see cref="DragEventArgs"/> object containing event data.</param>
        /// <remarks><para>
        /// <b>OnDrop</b> raises the <see cref="UIElement.Drop"/> event by calling the base class
        /// implementation of <see cref="UIElement.OnDrop"/> with the specified <paramref
        /// name="args"/>.
        /// </para><para>
        /// <b>OnDrop</b> then handles the <see cref="UIElement.Drop"/> event by calling <see
        /// cref="FilePaths.FindArgumentFile"/> with the first <see cref="IDataObject"/> element
        /// of format <see cref="DataFormats.FileDrop"/>, if any.
        /// </para><para>
        /// If <see cref="FilePaths.FindArgumentFile"/> recognizes a scenario or session XML
        /// file, that file is opened as usual. Otherwise, an error message is displayed. All other
        /// drap &amp; drop arguments are ignored.</para></remarks>

        protected override void OnDrop(DragEventArgs args) {
            base.OnDrop(args);
            args.Handled = true;

            // we accept only Windows shell file drags
            IDataObject data = args.Data;
            if (!data.GetDataPresent(DataFormats.FileDrop)) return;

            // retrieve file(s) dragged & dropped on this form
            string[] files = data.GetData(DataFormats.FileDrop) as string[];

            // sanity check for empty array
            if (files == null || files.Length < 1) return;

            // retrieve first file argument
            string path = files[0];

            // make sure this window is active
            Show(); Activate();

            // dialog for invalid file type
            string dialog = Global.Strings.DialogFileInvalid;

            // attempt to process this argument
            switch (FilePaths.FindArgumentFile(ref path)) {

                case ArgumentFileType.Scenario:
                    UserAction.TryRun(() => Session.Create(path));
                    break;

                case ArgumentFileType.Session:
                    UserAction.TryRun(() => Session.Open(path));
                    break;

                case ArgumentFileType.Missing:
                    dialog = Global.Strings.DialogFileMissing;
                    goto default;

                default:
                    MessageBox.Show(this,
                        String.Format(ApplicationInfo.Culture, dialog, path),
                        Global.Strings.TitleDragDropError,
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    break;
            }
        }

        #endregion
        #region OnEntityActivate

        /// <summary>
        /// Handles the <see cref="Control.MouseDoubleClick"/> event for a <see
        /// cref="ListViewItem"/> of the "Site Contents" <see cref="ListView"/>.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="MouseButtonEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnEntityActivate</b> displays a <see cref="Dialog.ShowClasses"/> dialog containing
        /// information on the double-clicked item in the "Site Contents" list view.</remarks>

        private void OnEntityActivate(object sender, MouseButtonEventArgs args) {
            args.Handled = true;
            if (this._waitCalls != 0) return;

            // retrieve double-clicked item, if any
            var source = args.OriginalSource as DependencyObject;
            var listItem = ItemsControl.ContainerFromElement(EntityList, source) as ListViewItem;
            if (listItem == null) return;

            // retrieve supported content, if any
            object content = listItem.Content;
            if (content == null || !(content is EntityListItem)) return;
            EntityListItem item = (EntityListItem) content;

            // retrieve selected site, if any
            Site site = Session.Instance.SelectedSite;
            if (site == null) return;

            // get double-clicked entity in selected site
            Entity entity = site.GetEntity(item.Item1);
            Debug.Assert(entity != null);

            // show info dialog for this entity
            var dialog = new Dialog.ShowClasses(entity) { Owner = this };
            dialog.ShowDialog();
        }

        #endregion
        #region OnEntitySelected

        /// <summary>
        /// Handles the <see cref="Selector.SelectionChanged"/> event for the "Site Contents" <see
        /// cref="ListView"/>. </summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="SelectionChangedEventArgs"/> object containing event data.</param>
        /// <remarks><para>
        /// <b>OnEntitySelected</b> updates the "Property" list view to reflect the selected item in
        /// the "Site Contents" list view. For units, that <see cref="Entity"/> is also moved to the
        /// top of its <see cref="Site"/> stack.
        /// </para><para>
        /// If the current <see cref="Session.State"/> equals <see cref="SessionState.Human"/>,
        /// <b>OnEntitySelected</b> also updates implicit <see cref="TargetSelection"/> to reflect
        /// the new entity selection.</para></remarks>

        private void OnEntitySelected(object sender, SelectionChangedEventArgs args) {
            args.Handled = true;
            if (Session.State == SessionState.Invalid) return;

            // clear property list view
            PropertyList.ShowEntity(null);

            // retrieve selected item, if any
            string id = SelectedEntity;
            if (String.IsNullOrEmpty(id)) return;

            // retrieve selected site, if any
            Site site = Session.Instance.SelectedSite;
            if (site == null) return;

            // show properties of selected entity
            Entity entity = site.GetEntity(id);
            Debug.Assert(entity != null);
            PropertyList.ShowEntity(entity);

            // show selected unit on the map view
            if (entity.Category == EntityCategory.Unit)
                Session.MapView.ShowEntity(entity);

            // update implicit target selection
            if (Session.State == SessionState.Human)
                Session.Instance.TargetSelection.Begin(false);
        }

        #endregion
        #region OnEntityWidthChanged

        /// <summary>
        /// Handles changes to the <see cref="FrameworkElement.ActualWidth"/> property of the
        /// "Entity" <see cref="ListView"/>.</summary>
        /// <param name="sender">
        /// The <see cref="ListView"/> control sending the event.</param>
        /// <param name="args">
        /// An <see cref="EventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnEntityWidthChanged</b> resizes the "Entity" column of the "Entity" list view to the
        /// current list view width.</remarks>

        private void OnEntityWidthChanged(object sender, EventArgs args) {
            double width = EntityList.ActualWidth - 28;
            if (width > 0) EntityColumn.Width = width;
        }

        #endregion
        #region OnHighlightChanged

        /// <summary>
        /// Handles changes to the <see cref="MenuItem.IsHighlighted"/> property of any <see
        /// cref="MenuItem"/> in the main <see cref="Menu"/>.</summary>
        /// <param name="sender">
        /// The <see cref="MenuItem"/> control sending the event.</param>
        /// <param name="args">
        /// An <see cref="EventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnHighlightChanged</b> shows the original <see cref="FrameworkElement.ToolTip"/> text
        /// of the specified <paramref name="sender"/> as the new <see cref="StatusBar"/> message if
        /// its <see cref="MenuItem.IsHighlighted"/> flag is <c>true</c>, and otherwise restores the
        /// previous message.</remarks>

        private void OnHighlightChanged(object sender, EventArgs args) {
            MenuItem item = sender as MenuItem;
            if (item == null) return;

            // check if menu item was already highlighted
            int index = this._menuItems.IndexOf(item);

            if (index < 0 && item.IsHighlighted) {
                // add newly highlighted item to list
                if (this._menuItems.Count == 0) StatusMessage.Push();
                this._menuItems.Add(item);
            }
            else if (index >= 0 && !item.IsHighlighted) {
                // remove no longer highlighted item from list
                this._menuItems.RemoveAt(index);
                if (this._menuItems.Count == 0) StatusMessage.Pop();
            }

            // show message for most recent menu item, if any
            index = this._menuItems.Count - 1;
            if (index >= 0) {
                string tag = (string) this._menuItems[index].Tag;
                StatusMessage.Text = this._menuMessages[tag];
            }
        }

        #endregion
        #region OnIdleTimer

        /// <summary>
        /// Handles the <see cref="DispatcherTimer.Tick"/> event for the <see
        /// cref="DispatcherPriority.ApplicationIdle"/> timer.</summary>
        /// <param name="sender">
        /// The <see cref="DispatcherTimer"/> object sending the event.</param>
        /// <param name="args">
        /// An <see cref="EventArgs"/> object containing event data.</param>
        /// <remarks><para>
        /// <b>OnIdleTimer</b> performs the following actions:
        /// </para><list type="bullet"><item>
        /// Show the coordinates of the <see cref="Site"/> under the mouse cursor in the "Position"
        /// field of the <see cref="StatusBar"/>.
        /// </item><item>
        /// Show the projected losses for an <see cref="AttackCommand"/> on the <see cref="Site"/>
        /// under the mouse cursor during implicit <see cref="TargetSelection"/>.
        /// </item><item>
        /// Autoscroll the default <see cref="Session.MapView"/> if the <see cref="MainWindow"/> is
        /// maximized, and the mouse cursor is at an edge of the current <see
        /// cref="SystemInformation.VirtualScreen"/>.</item></list></remarks>

        private void OnIdleTimer(object sender, EventArgs args) {

            // no idle processing while waiting or browsing menu
            if (this._waitCalls != 0 || this._menuItems.Count > 0)
                return;

            MapView mapView = Session.MapView;
            if (mapView == null) return;

            // autoscroll map view if cursor is on window edges
            if (MainWindow.Instance.WindowState == WindowState.Maximized) {
                PointI cursor;
                User.GetCursorPos(out cursor);
                var screen = SystemInformation.VirtualScreen;

                if (cursor.X <= 0)
                    mapView.ScrollStep(ScrollDirection.Left);
                else if (cursor.X >= screen.Width - 1)
                    mapView.ScrollStep(ScrollDirection.Right);

                if (cursor.Y <= 0)
                    mapView.ScrollStep(ScrollDirection.Up);
                else if (cursor.Y >= screen.Height - 1)
                    mapView.ScrollStep(ScrollDirection.Down);
            }

            // show site under cursor in status bar
            StatusPosition = (mapView.IsMouseOver ? mapView.MouseToSite() : Site.InvalidLocation);

            // show projected losses if target selected
            if (Session.State == SessionState.Human)
                Session.Instance.TargetSelection.ShowLosses(StatusPosition);
        }

        #endregion
        #region OnKeyDown

        /// <summary>
        /// Raises and handles the <see cref="UIElement.KeyDown"/> event.</summary>
        /// <param name="args">
        /// A <see cref="KeyEventArgs"/> object containing event data.</param>
        /// <remarks><para>
        /// <b>OnKeyDown</b> raises the <see cref="UIElement.KeyDown"/> event by calling the base
        /// class implementation of <see cref="UIElement.OnKeyDown"/> with the specified <paramref
        /// name="args"/>.
        /// </para><para>
        /// <b>OnKeyDown</b> then handles the <see cref="UIElement.KeyDown"/> event as follows:
        /// </para><list type="bullet"><item>
        /// If the current <see cref="Session.State"/> equals <see cref="SessionState.Human"/> and
        /// <see cref="Key.LeftShift"/> or <see cref="Key.RightShift"/>was pressed, enable <see
        /// cref="TargetSelection.StackMode"/> for implicit <see cref="TargetSelection"/>.
        /// </item><item>
        /// If the current <see cref="Session.State"/> equals <see cref="SessionState.Selection"/>
        /// and <see cref="Key.Escape"/> was pressed, cancel explicit <see cref="TargetSelection"/>.
        /// </item><item>
        /// Otherwise, invoke the <see cref="RoutedUICommand"/> that corresponds to the unmodified
        /// <see cref="KeyEventArgs.Key"/> of the specified <paramref name="args"/>, if any.
        /// </item></list></remarks>

        protected override void OnKeyDown(KeyEventArgs args) {
            base.OnKeyDown(args);

            // Shift toggles implicit stack selection
            if (Session.State == SessionState.Human &&
                (args.Key == Key.LeftShift || args.Key == Key.RightShift)) {
                Session.Instance.TargetSelection.StackMode = true;
                return;
            }

            // only handle unmodified keys otherwise
            if (this._waitCalls != 0 || Keyboard.Modifiers != ModifierKeys.None)
                return;

            // Escape cancels explicit Selection mode
            if (Session.State == SessionState.Selection && args.Key == Key.Escape) {
                args.Handled = true;
                Session.Instance.TargetSelection.Cancel();
                return;
            }

            // prepare dictionary for commands invoked by keys
            if (this._commands == null)
                this._commands = new Dictionary<Key, RoutedUICommand>() {
                    { Key.W, (RoutedUICommand) Resources["gameWaitCycleCommand"] },
                    { Key.Space, (RoutedUICommand) Resources["gameSkipCycleCommand"] },
                    { Key.A, (RoutedUICommand) Resources["commandAttackCommand"] },
                    { Key.M, (RoutedUICommand) Resources["commandMoveCommand"] },
                    { Key.B, (RoutedUICommand) Resources["commandBuildCommand"] },
                    { Key.E, (RoutedUICommand) Resources["commandEntitiesCommand"] },
                    { Key.H, (RoutedUICommand) Resources["commandSiteCommand"] },
                    { Key.C, (RoutedUICommand) Resources["viewCenterSiteCommand"] },
                    { Key.X, (RoutedUICommand) Resources["viewZoomOutCommand"] },
                    { Key.Z, (RoutedUICommand) Resources["viewZoomInCommand"] },
                };

            // execute command invoked by key, if any
            RoutedUICommand command;
            if (this._commands.TryGetValue(args.Key, out command)) {
                args.Handled = true;
                command.Execute(null, this);
            }
        }

        #endregion
        #region OnKeyUp

        /// <summary>
        /// Raises and handles the <see cref="UIElement.KeyUp"/> event.</summary>
        /// <param name="args">
        /// A <see cref="KeyEventArgs"/> object containing event data.</param>
        /// <remarks><para>
        /// <b>OnKeyUp</b> raises the <see cref="UIElement.KeyUp"/> event by calling the base class
        /// implementation of <see cref="UIElement.OnKeyUp"/> with the specified <paramref
        /// name="args"/>.
        /// </para><para>
        /// <b>OnKeyUp</b> then handles the <see cref="UIElement.KeyUp"/> event as follows:
        /// </para><list type="bullet"><item>
        /// If the current <see cref="Session.State"/> equals <see cref="SessionState.Human"/> and
        /// <see cref="Key.LeftShift"/> or <see cref="Key.RightShift"/>was pressed, disable <see
        /// cref="TargetSelection.StackMode"/> for implicit <see cref="TargetSelection"/>.
        /// </item></list></remarks>

        protected override void OnKeyUp(KeyEventArgs args) {
            base.OnKeyDown(args);

            // Shift toggles implicit stack selection
            if (Session.State == SessionState.Human &&
                (args.Key == Key.LeftShift || args.Key == Key.RightShift))
                Session.Instance.TargetSelection.StackMode = false;
        }

        #endregion
        #region OnLinkOwner

        /// <summary>
        /// Handles the <see cref="Hyperlink.Click"/> event for the "Site Owner" and "Unit Owner"
        /// <see cref="Hyperlink"/> controls.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="RoutedEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnLinkOwner</b> displays a <see cref="Dialog.ShowFactions"/> dialog containing
        /// information on the <see cref="FactionClass"/> whose hyperlink was clicked.</remarks>

        private void OnLinkOwner(object sender, RoutedEventArgs args) {
            args.Handled = true;
            if (this._waitCalls != 0 || Session.Instance == null)
                return;

            // retrieved clicked hyperlink, if any
            Hyperlink link = args.Source as Hyperlink;
            if (link == null) return;

            // retrieved associated faction, if any
            FactionClass faction = link.Tag as FactionClass;
            if (faction == null) return;

            // show info dialog for this faction
            UserAction.TryRun(delegate {
                var dialog = new Dialog.ShowFactions(Session.MapView, faction) { Owner = this };
                dialog.ShowDialog();
            });
        }

        #endregion
        #region OnMapMouseDown

        /// <summary>
        /// Handles the <see cref="UIElement.MouseDown"/> event for the default <see
        /// cref="Session.MapView"/>.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="MouseButtonEventArgs"/> object containing event data.</param>
        /// <remarks><para>
        /// <b>OnMapMouseDown</b> performs the following actions, depending on the specified
        /// <paramref name="args"/> and the current <see cref="Session.State"/>:
        /// </para><list type="bullet"><item>
        /// On <see cref="MouseButton.Left"/> clicks, invoke <see cref="TargetSelection.Complete"/>
        /// with the clicked site if the current <see cref="Session.State"/> equals <see
        /// cref="SessionState.Selection"/>; otherwise, rotate through all units in the clicked site
        /// if equal to the current <see cref="MapView.SelectedSite"/>; otherwise, change the <see
        /// cref="MapView.SelectedSite"/> to the clicked site.
        /// </item><item>
        /// On <see cref="MouseButton.Right"/> clicks, invoke <see cref="TargetSelection.Complete"/>
        /// with the clicked site.
        /// </item><item>
        /// On <see cref="MouseButton.Middle"/> clicks, select the standard zoom level.
        /// </item></list></remarks>

        internal void OnMapMouseDown(object sender, MouseButtonEventArgs args) {
            args.Handled = true;
            MapView mapView = Session.MapView;
            if (this._waitCalls != 0 || mapView == null)
                return;

            // determine clicked site, if any
            PointI site = mapView.MouseToSite(args);
            switch (args.ChangedButton) {

                case MouseButton.Left:
                    if (Session.State == SessionState.Selection) {
                        // select clicked site as target
                        Session.Instance.TargetSelection.Complete(site);
                    }
                    else if (mapView.SelectedSite == site) {
                        // cycle through units in selected site
                        SelectNextUnit();
                    }
                    else {
                        // select clicked site in map view
                        mapView.SelectedSite = site;
                    }
                    break;

                case MouseButton.Right:
                    Session.Instance.TargetSelection.Complete(site);
                    break;

                case MouseButton.Middle:
                    ViewZoomStdExecuted(this, args);
                    break;
            }
        }

        #endregion
        #region OnMapMouseWheel

        /// <summary>
        /// Handles the <see cref="UIElement.MouseWheel"/> event for the default <see
        /// cref="Session.MapView"/>.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="MouseWheelEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnMapMouseWheel</b> zooms in on forward wheel rotation, and zooms out on backward
        /// wheel rotation. Clicking the wheel triggers <see cref="UIElement.MouseDown"/>.</remarks>

        public void OnMapMouseWheel(object sender, MouseWheelEventArgs args) {
            args.Handled = true;
            if (this._waitCalls != 0 || Session.MapView == null) 
                return;

            // change zoom level if map view exists
            if (args.Delta > 0)
                ViewZoomInExecuted(this, args);
            else if (args.Delta < 0)
                ViewZoomOutExecuted(this, args);
        }

        #endregion
        #region OnOptionsChanged

        /// <summary>
        /// Handles the <see cref="ApplicationOptions.OptionsChanged"/> event for the current <see
        /// cref="ApplicationOptions"/>.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> sending the event.</param>
        /// <param name="args">
        /// An <see cref="EventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnOptionsChanged</b> updates the main <see cref="Menu"/> to reflect the current user
        /// settings.</remarks>

        private void OnOptionsChanged(object sender, EventArgs args) {
            ApplicationOptions options = ApplicationOptions.Instance;

            // check/uncheck Replay Speed options
            ReplaySpeed speed = options.Game.Replay.Speed;
            MenuReplaySpeedSlow.IsChecked = (speed == ReplaySpeed.Slow);
            MenuReplaySpeedMedium.IsChecked = (speed == ReplaySpeed.Medium);
            MenuReplaySpeedFast.IsChecked = (speed == ReplaySpeed.Fast);
            MenuReplaySpeedTurbo.IsChecked = (speed == ReplaySpeed.Turbo);

            // check/uncheck Scroll Map View option
            MenuReplayScroll.IsChecked = options.Game.Replay.Scroll;

            // retrieve current settings for default map view
            MapViewOptions mapView = options.View.LoadMapView("default");

            // check/uncheck boolean map view options
            MenuViewAnimation.IsChecked = mapView.Animation;
            MenuViewShowFlags.IsChecked = mapView.ShowFlags;
            MenuViewShowGrid.IsChecked = mapView.ShowGrid;
            MenuViewShowOwner.IsChecked = mapView.ShowOwner;

            // determine index of active display scale
            IList<Int32> scales = options.View.MapScales;
            int index = scales.IndexOf(mapView.Scale);

            // enable/disable and check/uncheck Zoom options
            MenuViewZoomStd.IsChecked = (mapView.Scale == 100);
            MenuViewZoomIn.IsEnabled = (index >= 0 && index < scales.Count - 1);
            MenuViewZoomOut.IsEnabled = (index > 0);

            // check/uncheck boolean performance options
            MenuViewBitmapGrid.IsChecked = options.View.BitmapGrid;
            MenuViewOpaqueImages.IsChecked = options.View.OpaqueImages;
            MenuViewStaticArrows.IsChecked = options.View.StaticArrows;
            MenuViewStaticMarker.IsChecked = options.View.StaticMarker;

            // check/uncheck display theme options
            DefaultTheme theme = options.View.Theme;
            MenuViewThemeSystem.IsChecked = (theme == DefaultTheme.System);
            MenuViewThemeClassic.IsChecked = (theme == DefaultTheme.Classic);
            MenuViewThemeLuna.IsChecked = (theme == DefaultTheme.Luna);
            MenuViewThemeLunaHomestead.IsChecked = (theme == DefaultTheme.LunaHomestead);
            MenuViewThemeLunaMetallic.IsChecked = (theme == DefaultTheme.LunaMetallic);
            MenuViewThemeRoyale.IsChecked = (theme == DefaultTheme.Royale);
            MenuViewThemeAero.IsChecked = (theme == DefaultTheme.Aero);
            MenuViewThemeAero2.IsChecked = (theme == DefaultTheme.Aero2);
            MenuViewThemeAeroLite.IsChecked = (theme == DefaultTheme.AeroLite);
        }

        #endregion
        #region OnReplayStateChanged

        /// <summary>
        /// Handles changes to the <see cref="ReplayManager.CurrentState"/> property of the current
        /// <see cref="ReplayManager"/> instance.</summary>
        /// <param name="state">
        /// The new value for the <see cref="ReplayManager.CurrentState"/> property.</param>
        /// <remarks>
        /// <b>OnReplayStateChanged</b> checks or unchecks the "Pause Replay" item of the main <see
        /// cref="Menu"/> and updates the <see cref="StatusBar"/> message to reflect the specified
        /// <paramref name="state"/>, assuming a replay is still ongoing.</remarks>

        public void OnReplayStateChanged(ReplayState state) {
            switch (state) {

                case ReplayState.Pause:
                    MenuReplayPause.IsChecked = true;
                    StatusMessage.Text = Global.Strings.StatusReplayPaused;
                    break;

                case ReplayState.Play:
                    StatusMessage.Text = Global.Strings.StatusReplay;
                    goto default;

                default:
                    MenuReplayPause.IsChecked = false;
                    break;
            }
        }

        #endregion
        #region OnSessionChanged

        /// <summary>
        /// Handles the <see cref="Session.StateChanged"/> event.</summary>
        /// <param name="sender">
        /// The <see cref="Session"/> object sending the event. This argument may be a null
        /// reference.</param>
        /// <param name="args">
        /// An <see cref="EventArgs"/> object containing event data.</param>
        /// <remarks><para>
        /// <b>OnSessionChanged</b> the resets <see cref="IsComputerStopped"/> flag and updates all
        /// controls of the <see cref="MainWindow"/> to reflect the <see cref="Session.State"/> of
        /// the current <see cref="Session"/>.
        /// </para><para>
        /// <b>OnSessionChanged</b> also starts implicit <see cref="TargetSelection"/> if the
        /// current <see cref="Session.State"/> equals <see cref="SessionState.Human"/>, and
        /// otherwise clears all <see cref="TargetSelection"/> data, except during explicit <see
        /// cref="TargetSelection"/>.</para></remarks>

        private void OnSessionChanged(object sender, EventArgs args) {
            Debug.Assert((Session) sender == Session.Instance);
            CommandManager.InvalidateRequerySuggested();

            // prepare for next computer turn
            IsComputerStopped = false;

            // update main menu to reflect session state
            bool inSession = (Session.State != SessionState.Invalid);
            MenuGame.IsEnabled = inSession;
            MenuCommand.IsEnabled = (Session.State == SessionState.Human);
            MenuInfo.IsEnabled = inSession;
            MenuReplay.IsEnabled = inSession;
            MenuViewZoom.IsEnabled = inSession;
            MenuDebug.IsEnabled = inSession;

            // update remaining display to reflect session state
            UpdateTurnFaction();
            EventMessage.IsEnabled = inSession;
            ComputerMessage.Text = null;

            // clear remaining display if no session
            if (!inSession) {
                Title = ApplicationInfo.Title;
                MapViewHost.Content = QuickMenuGroup;
                EventMessage.Clear();
                ShowSite(Site.InvalidLocation);
                StatusMessage.Clear();
                StatusPosition = Site.InvalidLocation;
                return;
            }

            // set title bar to current scenario title
            Title = String.Format(ApplicationInfo.Culture,
                "{0} - {1}", ApplicationInfo.Title, StringUtility.Validate(
                    MasterSection.Instance.Title, Global.Strings.TitleScenarioUntitled));

            // enable or disable implicit target selection
            if (Session.State == SessionState.Human)
                Session.Instance.TargetSelection.Begin(false);
            else if (Session.State != SessionState.Selection)
                Session.Instance.TargetSelection.Clear();
        }

        #endregion
        #region OnSiteSelected

        /// <summary>
        /// Handles the <see cref="MapView.SelectedSiteChanged"/> event for the specified <see
        /// cref="MapView"/>.</summary>
        /// <param name="sender">
        /// The <see cref="MapView"/> object sending the event.</param>
        /// <param name="args">
        /// An <see cref="EventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnSiteSelected</b> updates the data view to reflect the <see
        /// cref="MapView.SelectedSite"/> of the specified <paramref name="sender"/>.</remarks>

        internal void OnSiteSelected(object sender, EventArgs args) {
            MapView mapView = sender as MapView;
            Debug.Assert(mapView == Session.MapView);

            // synchronize data view with selected site
            ShowSite(mapView.SelectedSite);
        }

        #endregion
        #endregion
    }
}
