using System;
using System.Collections.Generic;
using System.Windows;

using Tektosyne;
using Tektosyne.Collections;
using Tektosyne.Geometry;

using Hexkit.Global;
using Hexkit.Graphics;
using Hexkit.Scenario;
using Hexkit.World;
using Hexkit.World.Commands;

namespace Hexkit.Game {

    using EntityList = KeyedList<String, Entity>;

    /// <summary>
    /// Performs actions taken by a local human player.</summary>
    /// <remarks><para>
    /// <b>HumanAction</b>, which has nothing to do with Ludwig von Mises, provides a number of
    /// static methods that implement actions taken in response to a local human player's commands.
    /// </para><para>
    /// All <b>HumanAction</b> methods operate on the current <see cref="Session"/> instance and
    /// require that the current session <see cref="Session.State"/> is <see
    /// cref="SessionState.Human"/>.</para></remarks>

    public static class HumanAction {
        #region Private Fields

        // backer for SkippedUnits property
        private readonly static List<String> _skippedUnits = new List<String>();

        #endregion
        #region SkippedUnits

        /// <summary>
        /// Gets a list of all <see cref="Unit"/> identifiers that are skipped by <see
        /// cref="SelectUnit"/>.</summary>
        /// <value>
        /// A <see cref="List{String}"/> containing the <see cref="Entity.Id"/> strings of all <see
        /// cref="Unit"/> objects that are skipped by <see cref="SelectUnit"/>.</value>
        /// <remarks>
        /// The <b>SkippedUnits</b> collection is cleared at every <see cref="Session.Dispatch"/>
        /// call. <see cref="SelectUnit"/> may add identifiers to this collection, depending on the
        /// specified arguments.</remarks>

        public static List<String> SkippedUnits {
            get { return HumanAction._skippedUnits; }
        }

        #endregion
        #region Private Methods
        #region CheckSessionState

        /// <summary>
        /// Checks that the session <see cref="Session.State"/> equals <see
        /// cref="SessionState.Human"/>.</summary>
        /// <exception cref="PropertyValueException">
        /// The current session <see cref="Session.State"/> is not <see cref="SessionState.Human"/>.
        /// </exception>

        private static void CheckSessionState() {
            if (Session.State != SessionState.Human)
                ThrowHelper.ThrowPropertyValueExceptionWithFormat("Session.State",
                    Session.State, Tektosyne.Strings.PropertyNotValue, SessionState.Human);
        }

        #endregion
        #endregion
        #region Attack

        /// <summary>
        /// Allows units of the <see cref="WorldState.ActiveFaction"/>, which must be controlled by
        /// a local human player, to perform an attack from or on the selected <see cref="Site"/>.
        /// </summary>
        /// <exception cref="PropertyValueException">
        /// The current session <see cref="Session.State"/> is not <see cref="SessionState.Human"/>.
        /// </exception>
        /// <remarks><para>
        /// <b>Attack</b> shows the <see cref="Dialog.AttackFromSite"/> dialog if the selected site
        /// contains any units owned by the <see cref="WorldState.ActiveFaction"/> that report
        /// possible attack targets. If no owned units are present, the <see
        /// cref="Dialog.AttackSite"/> dialog is shown instead.
        /// </para><para>
        /// In either case, <b>Attack</b> issues an <see cref="AttackCommand"/> with the entered
        /// data for the <see cref="WorldState.ActiveFaction"/>.</para></remarks>

        public static void Attack() {
            CheckSessionState();

            Session session = Session.Instance;
            WorldState world = session.WorldState;
            Faction faction = world.ActiveFaction;

            // retrieve selected source site
            Site source = world.GetSite(Session.MapView.SelectedSite);
            if (source == null) return;

            // retrieve owned units in source site
            List<Entity> present = new List<Entity>();
            foreach (Entity unit in source.Units)
                if (unit.Owner == faction) present.Add(unit);

            // variables used by both "if" branches
            var eligible = new EntityList();
            PointI target = Site.InvalidLocation;
            IList<PointI> targets = null;
            EntityList units = null;

            // prepare single-unit collection
            var singleUnit = new List<Entity>(1) { null };

            // check if source site contains owned units
            if (present.Count > 0) {

                // determine units eligible for attack
                foreach (Unit unit in present) {
                    singleUnit[0] = unit;
                    targets = Finder.FindAttackTargets(world, singleUnit);
                    if (targets.Count > 0) eligible.Add(unit);
                }

                // check if any present units may attack
                if (eligible.Count == 0) {
                    MessageBox.Show(MainWindow.Instance,
                        Global.Strings.DialogAttackSourceNone,
                        Global.Strings.TitleAttackSite + faction.Name,
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // show "Attack From Site" dialog for active faction
                using (var dialog = new Dialog.AttackFromSite(source, eligible)) {
                    dialog.Owner = MainWindow.Instance;
                    if (dialog.ShowDialog() != true) return;

                    // retrieve user selections
                    target = dialog.Target;
                    units = dialog.Units;
                }
            }
            else {
                // retrieve placed units of active faction
                var placed = faction.GetEntities(EntityCategory.Unit, true);

                // check if faction owns any placed units
                if (placed.Count == 0) {
                    MessageBox.Show(MainWindow.Instance,
                        Global.Strings.DialogAttackPlacedNone,
                        Global.Strings.TitleAttackSite + faction.Name,
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // target selected site
                target = source;

                // determine units eligible for attack
                foreach (Unit unit in placed) {
                    singleUnit[0] = unit;
                    targets = Finder.FindAttackTargets(world, singleUnit);
                    if (targets.Contains(target)) eligible.Add(unit);
                }

                // check if any units may attack
                if (eligible.Count == 0) {
                    MessageBox.Show(MainWindow.Instance,
                        Global.Strings.DialogAttackTargetNone,
                        Global.Strings.TitleAttackSite + faction.Name,
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // show "Attack Site" dialog for active faction
                using (var dialog = new Dialog.AttackSite(target, eligible)) {
                    dialog.Owner = MainWindow.Instance;
                    if (dialog.ShowDialog() != true) return;

                    // retrieve user selection
                    units = dialog.Units;
                }
            }

            // issue Attack command for valid selections
            if (target != Site.InvalidLocation && units != null && units.Count > 0)
                AsyncAction.Run(() => session.Executor.ExecuteAttack(world, units, target));
        }

        #endregion
        #region Build

        /// <summary>
        /// Allows the <see cref="WorldState.ActiveFaction"/>, which must be controlled by a local
        /// human player, to convert resources into new entities.</summary>
        /// <exception cref="PropertyValueException">
        /// The current session <see cref="Session.State"/> is not <see cref="SessionState.Human"/>.
        /// </exception>
        /// <remarks><para>
        /// <b>Build</b> shows the <see cref="Dialog.BuildEntities"/> dialog, allowing the local
        /// human player to build new entities. This method may issue one or more <see
        /// cref="BuildCommand"/> for the <see cref="WorldState.ActiveFaction"/>.
        /// </para><para>
        /// <b>Build</b> will enter <see cref="SessionState.Selection"/> mode if the user requests
        /// immediate placement, allowing the local human player to select a target site for the
        /// desired <see cref="EntityClass"/>. Successful completion will issue a <see
        /// cref="BuildCommand"/> and a <see cref="PlaceCommand"/>.</para></remarks>

        public static void Build() {
            CheckSessionState();

            Session session = Session.Instance;
            WorldState world = session.WorldState;
            Faction faction = world.ActiveFaction;

            // get build counts for all categories
            int unitCount = faction.GetBuildableClasses(EntityCategory.Unit).Count;
            int terrainCount = faction.GetBuildableClasses(EntityCategory.Terrain).Count;
            int upgradeCount = faction.GetBuildableClasses(EntityCategory.Upgrade).Count;

            // check if active faction may build any entities
            if (unitCount == 0 && terrainCount == 0 && upgradeCount == 0) {
                MessageBox.Show(MainWindow.Instance,
                    Global.Strings.DialogBuildAllowedNone,
                    Global.Strings.TitleBuildEntities + faction.Name,
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // determine initially shown category
            EntityCategory category = EntityCategory.Unit;
            if (unitCount == 0)
                category = (upgradeCount > 0 ? EntityCategory.Upgrade : EntityCategory.Terrain);

            // show "Build Entities" dialog for active faction
            var dialog = new Dialog.BuildEntities(category) { Owner = MainWindow.Instance };
            dialog.ShowDialog();

            // retrieve entity class to build, if any
            EntityClass buildingClass = dialog.BuildingClass;
            if (buildingClass == null) return;

            // get valid target sites for entity class
            var targets = faction.GetPlaceTargets(world, buildingClass);
            if (targets.Count == 0) return;

            // create marker array for map view
            bool[,] region = Finder.MapGrid.CreateArray<Boolean>();

            // mark all valid target sites
            foreach (PointI site in targets)
                region[site.X, site.Y] = true;

            // start target selection for Build command
            session.TargetSelection.BeginCommand(typeof(BuildCommand), region, buildingClass);
        }

        #endregion
        #region EndTurn

        /// <summary>
        /// Ends the turn for the <see cref="WorldState.ActiveFaction"/>, which must be controlled
        /// by a local human player, and activates subsequent factions.</summary>
        /// <exception cref="PropertyValueException">
        /// The current session <see cref="Session.State"/> is not <see cref="SessionState.Human"/>.
        /// </exception>
        /// <remarks><para>
        /// <b>EndTurn</b> should be called whenever a local human player wishes to end his turn.
        /// This method performs the following actions:
        /// </para><list type="number"><item>
        /// Save the current <see cref="Session.WorldState"/> to the predefined <see
        /// cref="SessionFileType.Auto"/> session file.
        /// </item><item>
        /// Issue an <see cref="EndTurnCommand"/> for the <see cref="WorldState.ActiveFaction"/>.
        /// </item><item>
        /// Call <see cref="Session.Dispatch"/> on the current <see cref="Session"/> to dispatch the
        /// game to the player controlling the faction that was activated in the previous step.
        /// </item></list><para>
        /// <b>EndTurn</b> calls <see cref="Session.Close"/> without confirmation if an error
        /// occurred, or if a faction controlled by a remote human player was activated.
        /// </para></remarks>

        public static void EndTurn() {
            CheckSessionState();
            Session session = Session.Instance;

            // autosave game before executing command
            string path = FilePaths.GetSessionFile(SessionFileType.Auto).AbsolutePath;
            session.Save(ref path, false);

            AsyncAction.BeginRun(delegate {
                Action postAction;

                // issue EndTurn command and dispatch game to next player
                if (session.Executor.ExecuteEndTurn(session.WorldState))
                    postAction = session.Dispatch;
                else
                    postAction = () => Session.Close(false);

                AsyncAction.BeginInvoke(delegate { postAction(); AsyncAction.EndRun(); });
            });
        }

        #endregion
        #region ManageEntities

        /// <summary>
        /// Allows the <see cref="WorldState.ActiveFaction"/>, which must be controlled by a local
        /// human player, to rename, place, and destroy existing entities.</summary>
        /// <param name="mode">
        /// A <see cref="Dialog.ShowEntitiesMode"/> value indicating the initial display mode of the
        /// <see cref="Dialog.ShowEntities"/> dialog. The <b>Command</b> flag is added automatically
        /// if not specified.</param>
        /// <exception cref="PropertyValueException">
        /// The current session <see cref="Session.State"/> is not <see cref="SessionState.Human"/>.
        /// </exception>
        /// <remarks><para>
        /// <b>ManageEntities</b> shows the <see cref="Dialog.ShowEntities"/> dialog, allowing the
        /// local human player to inspect, rename, place, and destroy existing entities. This dialog
        /// may issue one or more <see cref="DestroyCommand"/> and/or <see cref="RenameCommand"/>
        /// for the <see cref="WorldState.ActiveFaction"/>.
        /// </para><para>
        /// <b>ManageEntities</b> will enter <see cref="SessionState.Selection"/> mode if the user
        /// requests a <see cref="PlaceCommand"/>, allowing the local human player to select a
        /// target site for the desired <see cref="Entity"/>.</para></remarks>

        public static void ManageEntities(Dialog.ShowEntitiesMode mode) {
            CheckSessionState();

            Session session = Session.Instance;
            WorldState world = session.WorldState;
            Faction faction = world.ActiveFaction;

            mode |= Dialog.ShowEntitiesMode.Command;
            EntityCategory category = EntityCategory.Unit;

            if ((mode & Dialog.ShowEntitiesMode.Site) != 0) {

                // retrieve selected site
                Site selected = world.GetSite(Session.MapView.SelectedSite);

                // check if any site selected
                if (selected == null) {
                    MessageBox.Show(MainWindow.Instance,
                        Global.Strings.DialogSiteUnselected,
                        Global.Strings.TitleSelectedEntities + faction.Name,
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // check if any owned units are present
                if (selected.Units.Count == 0 || selected.Units[0].Owner != faction) {

                    // check if any owned terrains are present
                    if (selected.Owner != faction) {
                        MessageBox.Show(MainWindow.Instance,
                            Global.Strings.DialogSiteEmpty,
                            Global.Strings.TitleSelectedEntities + faction.Name,
                            MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }

                    // no units, show terrains initially
                    category = EntityCategory.Terrain;
                }
            }

            // show "Faction Entities" dialog for active faction
            var dialog = new Dialog.ShowEntities(Session.MapView, faction, category, mode);
            dialog.Owner = MainWindow.Instance;
            dialog.ShowDialog();

            // retrieve entity to be placed, if any
            Entity placingEntity = dialog.PlacingEntity;
            if (placingEntity == null) return;

            // get valid target sites for entity class
            var targets = faction.GetPlaceTargets(world, placingEntity.EntityClass);
            if (targets.Count == 0) return;

            // create marker array for map view
            bool[,] region = Finder.MapGrid.CreateArray<Boolean>();

            // mark all valid target sites
            foreach (PointI site in targets)
                region[site.X, site.Y] = true;

            // start target selection for Place command
            session.TargetSelection.BeginCommand(typeof(PlaceCommand),
                region, new List<Entity>(1) { placingEntity });
        }

        #endregion
        #region Move

        /// <summary>
        /// Allows selected units of the <see cref="WorldState.ActiveFaction"/>, which must be
        /// controlled by a local human player, to move to another <see cref="Site"/>.</summary>
        /// <exception cref="PropertyValueException">
        /// The current session <see cref="Session.State"/> is not <see cref="SessionState.Human"/>.
        /// </exception>
        /// <remarks>
        /// <b>Move</b> shows the <see cref="Dialog.MoveUnits"/> dialog if the selected site
        /// contains any units owned by the <see cref="WorldState.ActiveFaction"/> that report any
        /// possible movement targets, and issues a <b>Move</b> command for the <see
        /// cref="WorldState.ActiveFaction"/> with the entered data.</remarks>

        public static void Move() {
            CheckSessionState();

            Session session = Session.Instance;
            WorldState world = session.WorldState;
            Faction faction = world.ActiveFaction;

            // retrieve selected source site
            Site source = world.GetSite(Session.MapView.SelectedSite);
            if (source == null) return;

            // retrieve owned units in source site
            var present = new EntityList();
            foreach (Entity unit in source.Units)
                if (unit.Owner == faction) present.Add(unit);

            // check if source site contains owned units
            if (present.Count == 0) {
                MessageBox.Show(MainWindow.Instance,
                    Global.Strings.DialogMoveSourceEmpty,
                    Global.Strings.TitleMoveUnits + faction.Name,
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var eligible = new EntityList();
            IList<PointI> targets = null;

            // prepare single-unit collection
            var singleUnit =  new List<Entity>(1) { null };

            // determine units eligible for movement
            foreach (Unit unit in present) {
                singleUnit[0] = unit;
                targets = Finder.FindMoveTargets(world, singleUnit);
                if (targets.Count > 0) eligible.Add(unit);
            }

            // check if any present units may move
            if (eligible.Count == 0) {
                MessageBox.Show(MainWindow.Instance,
                    Global.Strings.DialogMoveSourceNone,
                    Global.Strings.TitleMoveUnits + faction.Name,
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            PointI target = Site.InvalidLocation;
            EntityList units = null;

            // show "Move Units" dialog for active faction
            using (var dialog = new Dialog.MoveUnits(source, eligible)) {
                dialog.Owner = MainWindow.Instance;
                if (dialog.ShowDialog() != true) return;

                // retrieve user selections
                target = dialog.Target;
                units = dialog.Units;
            }

            // issue Move command for valid selections
            if (target != Site.InvalidLocation && units != null && units.Count > 0)
                AsyncAction.Run(() => session.Executor.ExecuteMove(world, units, target));
        }

        #endregion
        #region Resign

        /// <summary>
        /// Resigns the game for the <see cref="WorldState.ActiveFaction"/>, which must be
        /// controlled by a local human player.</summary>
        /// <exception cref="PropertyValueException">
        /// The current session <see cref="Session.State"/> is not <see cref="SessionState.Human"/>.
        /// </exception>
        /// <remarks><para>
        /// <b>Resign</b> should be called whenever a local human player wishes to resign the game.
        /// This method performs the following actions:
        /// </para><list type="number"><item>
        /// Ask the user to confirm the operation, and immediately return <c>true</c> if the user
        /// declines.
        /// </item><item>
        /// Issue a <see cref="ResignCommand"/> for the <see cref="WorldState.ActiveFaction"/>.
        /// </item><item>
        /// Invoke <see cref="EndTurn"/> to immediately end the active faction's turn if command
        /// execution was successful.
        /// </item></list><para>
        /// <b>Resign</b> calls <see cref="Session.Close"/> without confirmation if an error
        /// occurred, or if a faction controlled by a remote human player was activated.
        /// </para></remarks>

        public static void Resign() {
            CheckSessionState();

            WorldState world = Session.Instance.WorldState;
            string name = world.ActiveFaction.Name;

            // ask user to confirm resignation
            MessageBoxResult result = MessageBox.Show(MainWindow.Instance,
                Global.Strings.DialogResignFaction, name, MessageBoxButton.OKCancel,
                MessageBoxImage.Question, MessageBoxResult.Cancel);

            if (result == MessageBoxResult.Cancel) return;

            AsyncAction.BeginRun(delegate {
                Action postAction;

                // issue Resign command and end faction's turn
                if (Session.Instance.Executor.ExecuteResign(world))
                    postAction = EndTurn;
                else
                    postAction = () => Session.Close(false);

                AsyncAction.BeginInvoke(delegate { postAction(); AsyncAction.EndRun(); });
            });
        }

        #endregion
        #region SelectUnit

        /// <summary>
        /// Selects the next active unit of the <see cref="WorldState.ActiveFaction"/>, which must
        /// be controlled by a local human player.</summary>
        /// <param name="unitId">
        /// The <see cref="Entity.Id"/> string of the <see cref="Unit"/> to select, if possible.
        /// This argument may be a null reference or an empty string.</param>
        /// <param name="skipUnit">
        /// <c>true</c> to add the <see cref="MainWindow.SelectedEntity"/> to the <see
        /// cref="SkippedUnits"/> collection; otherwise, <c>false</c>. This argument is ignored if
        /// <paramref name="unitId"/> is a valid identifier.</param>
        /// <param name="showDialog">
        /// <c>true</c> to show an informational message if no active units were found; otherwise,
        /// <c>false</c>.</param>
        /// <exception cref="PropertyValueException">
        /// The current session <see cref="Session.State"/> is not <see cref="SessionState.Human"/>.
        /// </exception>
        /// <remarks><para>
        /// <b>SelectUnit</b> attempts to find a unit owned by the <see
        /// cref="WorldState.ActiveFaction"/> that has valid targets for an <see
        /// cref="AttackCommand"/> or a <see cref="MoveCommand"/>.
        /// </para><para>
        /// <b>SelectUnit</b> searches a matching unit in the index sequence established by <see
        /// cref="Faction.SortEntities"/>, as follows:
        /// </para><list type="bullet"><item>
        /// If the specified <paramref name="unitId"/> matches a unit of the <see
        /// cref="WorldState.ActiveFaction"/>, the search begins with that unit.
        /// </item><item>
        /// Otherwise, if the <see cref="MainWindow.SelectedEntity"/> matches a unit of the
        /// <b>ActiveFaction</b>, the search begins with the <em>next</em> unit after that unit.
        /// </item><item>
        /// Otherwise, the search begins with the first unit in the index sequence.
        /// </item><item>
        /// The search skips any units whose <see cref="Entity.Id"/> string is found in the <see
        /// cref="SkippedUnits"/> collection.
        /// </item></list><para>
        /// <b>SelectUnit</b> notifies the user if the <see cref="WorldState.ActiveFaction"/> does
        /// not own any active units and <paramref name="showDialog"/> is <c>true</c>.
        /// </para></remarks>

        public static void SelectUnit(string unitId, bool skipUnit, bool showDialog) {
            CheckSessionState();
            WorldState world = Session.Instance.WorldState;
            Faction faction = world.ActiveFaction;

            // check if faction owns any units at all
            if (faction.Units.Count == 0) goto failure;

            // sort units for intuitive progression
            faction.SortEntities(EntityCategory.Unit);

            int index = -1, first = -1;
            if (String.IsNullOrEmpty(unitId)) {

                // start search after selected unit
                unitId = MainWindow.Instance.SelectedEntity;
                if (unitId.Length > 0) index = faction.Units.IndexOfKey(unitId);
                first = (index + 1) % faction.Units.Count;

                // mark unit as skipped if desired
                if (skipUnit && index >= 0 && !SkippedUnits.Contains(unitId))
                    SkippedUnits.Add(unitId);
            }
            else {
                // start search with specified or first unit
                index = faction.Units.IndexOfKey(unitId);
                first = (index < 0 ? 0 : index);
            }

            int next = first;

            // prepare single-unit collection
            var units = new List<Entity>(1) { null };

            do {
                // find next active unit, if any
                units[0] = faction.Units[next];

                // check for valid attack or movement targets
                if (!SkippedUnits.Contains(units[0].Id) &&
                    (Finder.FindAttackTargets(world, units).Count > 0
                    || Finder.FindMoveTargets(world, units).Count > 0)) {

                    // scroll active unit's site into view
                    MapView mapView = Session.MapView;
                    PointI site = units[0].Site.Location;
                    mapView.ScrollIntoView(site);

                    /*
                     * Only select active unit's site if not already selected
                     * because reselection is both slow and resorts the unit
                     * in the selected site which is fairly irritating.
                     */

                    if (mapView.SelectedSite != site)
                        mapView.SelectedSite = site;

                    // select next active unit
                    MainWindow.Instance.SelectedEntity = units[0].Id;
                    return;
                }

                // loop until we return to first index
                next = (next + 1) % faction.Units.Count;
            } while (next != first);

        failure:
            // specified faction owns no active units
            if (showDialog)
                MessageBox.Show(MainWindow.Instance,
                    Global.Strings.DialogActiveUnitsNone,
                    Global.Strings.TitleActiveUnits + faction.Name,
                    MessageBoxButton.OK, MessageBoxImage.Information);
        }

        #endregion
    }
}
