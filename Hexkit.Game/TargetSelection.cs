using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Controls.Primitives;

using Tektosyne;
using Tektosyne.Geometry;

using Hexkit.Global;
using Hexkit.Graphics;
using Hexkit.Scenario;
using Hexkit.World;
using Hexkit.World.Commands;

namespace Hexkit.Game {

    /// <summary>
    /// Manages interactive target selection for the current game session.</summary>
    /// <remarks><para>
    /// <b>TargetSelection</b> allows the user to select a command's target <see cref="Site"/>
    /// directly on the default <see cref="Session.MapView"/>.
    /// </para><para>
    /// When <see cref="Session.State"/> equals <see cref="SessionState.Human"/>, target selection
    /// is <em>implicit.</em> All possible target sites for an <see cref="AttackCommand"/> or <see
    /// cref="MoveCommand"/> from the current <see cref="Session.SelectedSite"/> are permanently
    /// highlighted, and the <b>TargetSelection</b> class deduces the intended command from the
    /// contents of the target site.
    /// </para><para>
    /// When <see cref="Session.State"/> equals <see cref="SessionState.Selection"/>, target
    /// selection is <em>explicit.</em> The user first issues a specific <see cref="Command"/>, and
    /// the <b>TargetSelection</b> class then highlights the possible targets sites for that command
    /// only, until the user selects one or cancels <see cref="SessionState.Selection"/> mode.
    /// </para></remarks>

    public class TargetSelection {
        #region TargetSelection()

        /// <summary>
        /// Initializes a new instance of the <see cref="TargetSelection"/> class.</summary>

        public TargetSelection() {
            this._attackUnits = new List<Entity>();
            this._moveUnits = new List<Entity>();
        }

        #endregion
        #region Private Fields

        // units and possible targets for implicit command
        private readonly List<Entity> _attackUnits, _moveUnits;
        private IList<PointI> _attackTargets, _moveTargets;
        private bool[,] _targetRegion;
        private bool _stackMode;

        // explicit command and parameters awaiting target
        private Type _commandType;
        private IList<Entity> _entities;
        private EntityClass _entityClass;

        #endregion
        #region Private Methods
        #region BeginCommand

        /// <summary>
        /// Begins <see cref="SessionState.Selection"/> mode with the specified pending command that
        /// operates on <see cref="Entity"/> or <see cref="EntityClass"/> objects.</summary>
        /// <param name="commandType">
        /// The actual concrete <see cref="Type"/> of the <see cref="Command"/> object pending
        /// completion.</param>
        /// <param name="region">
        /// A two-dimensional <see cref="Array"/> of <see cref="Boolean"/> values indicating the
        /// coordinates of all possible target sites.</param>
        /// <param name="entities">
        /// An <see cref="IList{T}"/> containing all <see cref="Entity"/> objects that perform the
        /// command. This argument may be a null reference.</param>
        /// <param name="entityClass">
        /// The <see cref="EntityClass"/> on which to perform the command. This argument may be a
        /// null reference.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="commandType"/> or <paramref name="region"/> is a null reference.
        /// </exception>
        /// <remarks>
        /// <b>BeginCommand</b> implements common functionality for the two public <see
        /// cref="BeginCommand"/> overloads.</remarks>

        private void BeginCommand(Type commandType, bool[,] region,
            IList<Entity> entities, EntityClass entityClass) {

            if (commandType == null)
                ThrowHelper.ThrowArgumentNullException("commandType");
            if (region == null)
                ThrowHelper.ThrowArgumentNullException("region");

            // check for active Selection mode
            if (Session.State == SessionState.Selection)
                return;

            // store command type and arguments
            this._commandType = commandType;
            this._entities = entities;
            this._entityClass = entityClass;

            // highlight sites in specified region
            Session.MapView.SelectedRegion = region;

            // start explicit Selection mode
            MainWindow.Instance.StatusMessage.Push(Global.Strings.StatusCommandSelection);
            Session.State = SessionState.Selection;
        }

        #endregion
        #endregion
        #region StackMode

        /// <summary>
        /// Gets or sets a value indicating whether implicit target selection considers all eligible
        /// units in the <see cref="Session.SelectedSite"/>.</summary>
        /// <value>
        /// <c>true</c> to consider all eligible units in the <see cref="Session.SelectedSite"/>;
        /// <c>false</c> to consider the <see cref="MainWindow.SelectedEntity"/> only.</value>
        /// <remarks>
        /// <b>StackMode</b> always reflects the value of the corresponding flag supplied to the
        /// last call to <see cref="Begin(Boolean)"/>. Setting this property does not immediately
        /// change its value but rather invokes <see cref="Begin(Boolean)"/> with the new value, if
        /// different from the current value.</remarks>

        public bool StackMode {
            [DebuggerStepThrough]
            get { return this._stackMode; }
            set {
                if (value != this._stackMode)
                    Begin(value);
            }
        }

        #endregion
        #region Begin(Boolean)

        /// <summary>
        /// Begins implicit target selection for the selected units.</summary>
        /// <param name="stackMode">
        /// <c>true</c> to consider all eligible units in the <see cref="Session.SelectedSite"/>;
        /// <c>false</c> to consider the <see cref="MainWindow.SelectedEntity"/> only.</param>
        /// <remarks><para>
        /// <b>Begin</b> first resets the current <see cref="Session.State"/> to <see
        /// cref="SessionState.Human"/> if it was <see cref="SessionState.Selection"/>, which calls
        /// <see cref="Clear"/> implicitly, and otherwise calls <see cref="Clear"/> explicitly to
        /// clear all data managed by the <see cref="TargetSelection"/> class.
        /// </para><para>
        /// <b>Begin</b> then establishes a <see cref="MapView.SelectedRegion"/> on the default <see
        /// cref="Session.MapView"/> that combines all valid targets for either an <see
        /// cref="AttackCommand"/> or a <see cref="MoveCommand"/> by the <see cref="Site.Units"/> in
        /// the current <see cref="Session.SelectedSite"/>.
        /// </para><para>
        /// Only units owned by the <see cref="WorldState.ActiveFaction"/> are considered. Of those,
        /// only units with the <see cref="Unit.CanAttack"/> flag are considered for <see
        /// cref="AttackCommand"/> targets, and only units with the <see cref="Unit.CanMove"/> flag
        /// are considered for <see cref="MoveCommand"/> targets.
        /// </para><para>
        /// <b>Begin</b> also sets the <see cref="StackMode"/> property to the specified <paramref
        /// name="stackMode"/>, so that other methods and clients can determine the basis for the
        /// current target calculations.</para></remarks>

        public void Begin(bool stackMode) {

            // cancel explicit Selection mode if enabled
            if (Session.State == SessionState.Selection)
                Session.State = SessionState.Human;
            else
                Clear();

            WorldState world = Session.Instance.WorldState;
            Faction faction = world.ActiveFaction;

            Site sourceSite = Session.Instance.SelectedSite;
            if (sourceSite == null) return;

            // update StackMode property
            this._stackMode = stackMode;

            if (stackMode) {
                // consider all present owned units
                foreach (Unit unit in sourceSite.Units)
                    if (unit.Owner == faction) {
                        if (unit.CanAttack) this._attackUnits.Add(unit);
                        if (unit.CanMove) this._moveUnits.Add(unit);
                    }
            } else {
                // consider selected entity if owned unit
                string id = MainWindow.Instance.SelectedEntity;
                Unit unit = sourceSite.GetEntity(id) as Unit;

                if (unit != null && unit.Owner == faction) {
                    if (unit.CanAttack) this._attackUnits.Add(unit);
                    if (unit.CanMove) this._moveUnits.Add(unit);
                }
            }

            // check if source site contains eligible units
            if (this._attackUnits.Count == 0 && this._moveUnits.Count == 0)
                return;

            // determine all possible attack targets
            if (this._attackUnits.Count > 0) {
                this._attackTargets = Finder.FindAttackTargets(world, this._attackUnits);
                foreach (PointI target in this._attackTargets)
                    this._targetRegion[target.X, target.Y] = true;
            }

            // determine all possible move targets
            if (this._moveUnits.Count > 0) {
                this._moveTargets = Finder.FindMoveTargets(world, this._moveUnits);
                foreach (PointI target in this._moveTargets)
                    this._targetRegion[target.X, target.Y] = true;
            }

            // highlight all possible targets on map view
            Session.MapView.SelectedRegion = this._targetRegion;
        }

        #endregion
        #region BeginCommand(..., EntityClass)

        /// <overloads>
        /// Begins explicit target selection with the specified pending command.</overloads>
        /// <summary>
        /// Starts <see cref="SessionState.Selection"/> mode with the specified pending command that
        /// operates on an <see cref="EntityClass"/>.</summary>
        /// <param name="commandType">
        /// The actual concrete <see cref="Type"/> of the <see cref="Command"/> object pending
        /// completion.</param>
        /// <param name="region">
        /// A two-dimensional <see cref="Array"/> of <see cref="Boolean"/> values indicating the
        /// coordinates of all possible target sites.</param>
        /// <param name="entityClass">
        /// The <see cref="EntityClass"/> on which to perform the command.</param>
        /// <exception cref="ArgumentException">
        /// <paramref name="commandType"/> is not a valid <see cref="Command"/> type.</exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="commandType"/>, <paramref name="region"/>, or <paramref
        /// name="entityClass"/> is a null reference.</exception>
        /// <remarks><para>
        /// <b>BeginCommand</b> changes the current <see cref="Session.State"/> to <see
        /// cref="SessionState.Selection"/>, and the <see cref="MapView.SelectedRegion"/> of the
        /// default <see cref="Session.MapView"/> to the specified <paramref name="region"/>.
        /// </para><para>
        /// This allows the local human player to select a target site that completes the <see
        /// cref="Command"/> indicated by the specified <paramref name="commandType"/>.
        /// </para><para>
        /// <b>BeginCommand</b> does nothing if the current <see cref="Session.State"/> already
        /// equals <see cref="SessionState.Selection"/>.</para></remarks>

        public void BeginCommand(Type commandType, bool[,] region, EntityClass entityClass) {

            if (commandType != typeof(BuildCommand))
                ThrowHelper.ThrowArgumentExceptionWithFormat(
                    "commandType", Tektosyne.Strings.ArgumentNotEquals, "typeof(BuildCommand)");

            if (entityClass == null)
                ThrowHelper.ThrowArgumentNullException("entityClass");

            BeginCommand(commandType, region, null, entityClass);
        }

        #endregion
        #region BeginCommand(..., IList<Entity>)

        /// <summary>
        /// Starts <see cref="SessionState.Selection"/> mode with the specified pending command that
        /// operates on a list of <see cref="Entity"/> objects.</summary>
        /// <param name="commandType">
        /// The actual concrete <see cref="Type"/> of the <see cref="Command"/> object pending
        /// completion.</param>
        /// <param name="region">
        /// A two-dimensional <see cref="Array"/> of <see cref="Boolean"/> values indicating the
        /// coordinates of all possible target sites.</param>
        /// <param name="entities">
        /// An <see cref="IList{T}"/> containing all <see cref="Entity"/> objects that perform the
        /// command.</param>
        /// <exception cref="ArgumentException">
        /// <paramref name="commandType"/> is not a valid <see cref="Command"/> type.</exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="commandType"/> or <paramref name="region"/> is a null reference.
        /// </exception>
        /// <exception cref="ArgumentNullOrEmptyException">
        /// <paramref name="entities"/> is a null reference or an empty collection.</exception>
        /// <remarks><para>
        /// <b>BeginCommand</b> changes the current <see cref="Session.State"/> to <see
        /// cref="SessionState.Selection"/>, and the <see cref="MapView.SelectedRegion"/> of the
        /// default <see cref="Session.MapView"/> to the specified <paramref name="region"/>.
        /// </para><para>
        /// This allows the local human player to select a target site that completes the <see
        /// cref="Command"/> indicated by the specified <paramref name="commandType"/>.
        /// </para><para>
        /// <b>BeginCommand</b> does nothing if the current <see cref="Session.State"/> already
        /// equals <see cref="SessionState.Selection"/>.</para></remarks>

        public void BeginCommand(Type commandType, bool[,] region, IList<Entity> entities) {

            if (commandType != typeof(PlaceCommand))
                ThrowHelper.ThrowArgumentExceptionWithFormat(
                    "commandType", Tektosyne.Strings.ArgumentNotEquals, "typeof(PlaceCommand)");

            if (entities == null || entities.Count == 0)
                ThrowHelper.ThrowArgumentNullOrEmptyException("entities");

            BeginCommand(commandType, region, entities, null);
        }

        #endregion
        #region Cancel

        /// <summary>
        /// Cancels <see cref="SessionState.Selection"/> mode without completing the pending
        /// command.</summary>
        /// <remarks><para>
        /// <b>Cancel</b> resets the current <see cref="Session.State"/> to <see
        /// cref="SessionState.Human"/> which implicitly calls <see cref="Clear"/> to clear all data
        /// managed by the <see cref="TargetSelection"/> class.
        /// </para><para>
        /// If the pending command was a <see cref="BuildCommand"/> or a <see cref="PlaceCommand"/>,
        /// <b>Cancel</b> then calls <see cref="HumanAction.Build"/> or <see
        /// cref="HumanAction.ManageEntities"/>, allowing the local human player to continue
        /// building or placing entities, respectively.
        /// </para><para>
        /// <b>Cancel</b> does nothing if the current <see cref="Session.State"/> does not equal
        /// <see cref="SessionState.Selection"/>. Thus, implicit target selection is unaffected.
        /// </para></remarks>

        public void Cancel() {

            // check for active Selection mode
            if (Session.State != SessionState.Selection)
                return;

            // remember command type for continuation
            Type commandType = this._commandType;

            // cancel explicit Selection mode
            Session.State = SessionState.Human;

            // let user build or place more entities
            if (commandType == typeof(BuildCommand))
                HumanAction.Build();
            else if (commandType == typeof(PlaceCommand))
                HumanAction.ManageEntities(Dialog.ShowEntitiesMode.Unplaced);
        }

        #endregion
        #region Clear

        /// <summary>
        /// Clears all data managed by the <see cref="TargetSelection"/> class.</summary>
        /// <remarks><para>
        /// <b>Clear</b> performs the following actions:
        /// </para><list type="bullet"><item>
        /// Clear any current <see cref="StatusBar"/> message.
        /// </item><item>
        /// Clear the current <see cref="MapView.SelectedRegion"/> of the default <see
        /// cref="Session.MapView"/>.
        /// </item><item>
        /// Clear any current data of the <see cref="TargetSelection"/> object, including the <see
        /// cref="StackMode"/> flag.
        /// </item></list><para>
        /// <b>Clear</b> is called automatically whenever the current <see cref="Session.State"/>
        /// changes to any <see cref="SessionState"/> value other than <see
        /// cref="SessionState.Selection"/>.</para></remarks>

        public void Clear() {
            MainWindow.Instance.StatusMessage.Clear();

            MapView mapView = Session.MapView;
            if (mapView != null) mapView.SelectedRegion = null;

            this._attackUnits.Clear();
            this._moveUnits.Clear();
            this._attackTargets = this._moveTargets = null;
            this._stackMode = false;

            // create or clear target region
            if (this._targetRegion != null)
                Array.Clear(this._targetRegion, 0, this._targetRegion.Length);
            else if (mapView != null)
                this._targetRegion = mapView.MapGrid.CreateArray<Boolean>();

            this._commandType = null;
            this._entities = null;
            this._entityClass = null;
        }

        #endregion
        #region Complete

        /// <summary>
        /// Completes implicit or explicit target selection with the specified target location.
        /// </summary>
        /// <param name="target">
        /// The coordinates of the selected target <see cref="Site"/>.</param>
        /// <returns>
        /// <c>true</c> if a command was successfully executed; otherwise, <c>false</c>.</returns>
        /// <remarks><para>
        /// <b>Complete</b> calls <see cref="Cancel"/> and fails immediately if the specified
        /// <paramref name="target"/> is not in the <see cref="MapView.SelectedRegion"/> of the
        /// default <see cref="Session.MapView"/>.
        /// </para><para>
        /// Otherwise, if the current <see cref="Session.State"/> equals <see
        /// cref="SessionState.Human"/>, <b>Complete</b> issues either an <see
        /// cref="AttackCommand"/> or a <see cref="MoveCommand"/> with the specified <paramref
        /// name="target"/> site, depending on its contents. <b>Complete</b> then invokes <see
        /// cref="HumanAction.SelectUnit"/> to cycle to the next active unit.
        /// </para><para>
        /// Otherwise, if the current <see cref="Session.State"/> equals <see
        /// cref="SessionState.Selection"/>, <b>Complete</b> issues the pending command with the
        /// specified <paramref name="target"/> site. Depending on the pending command,
        /// <b>Complete</b> then calls <see cref="HumanAction.Build"/> or <see
        /// cref="HumanAction.ManageEntities"/>, allowing the local human player to continue
        /// building or placing entities, respectively.
        /// </para><para>
        /// The execution of any command implicitly resets the <see cref="Session.State"/> to <see
        /// cref="SessionState.Human"/> and calls <see cref="Clear"/> to clear all data managed by
        /// the <see cref="TargetSelection"/> class. Finally, <b>Complete</b> returns the return
        /// value of the <see cref="Session.Executor"/> method invoked for command execution.
        /// </para></remarks>

        public void Complete(PointI target) {

            // check for valid target
            if (!Session.MapView.InSelectedRegion(target)) {
                Cancel();
                return;
            }

            // remember currently selected entity, if any
            string id = MainWindow.Instance.SelectedEntity;

            AsyncAction.BeginRun(delegate {
                Action action = null;
                SessionExecutor executor = Session.Instance.Executor;
                WorldState world = Session.Instance.WorldState;

                if (Session.State == SessionState.Human) {

                    // issue implicit Attack or Move command, then cycle to next active unit
                    if (this._attackTargets != null && this._attackTargets.Contains(target)) {
                        executor.ExecuteAttack(world, this._attackUnits, target);
                        action = () => HumanAction.SelectUnit(id, false, false);
                    }
                    else if (this._moveTargets != null && this._moveTargets.Contains(target)) {
                        executor.ExecuteMove(world, this._moveUnits, target);
                        action = () => HumanAction.SelectUnit(id, false, false);
                    }
                }
                else if (Session.State == SessionState.Selection) {

                    // issue Build or Place command, then let user build or place more entities
                    if (this._commandType == typeof(BuildCommand)) {
                        executor.ExecuteBuildPlace(world, this._entityClass.Id, 1, target);
                        action = HumanAction.Build;
                    }
                    else if (this._commandType == typeof(PlaceCommand)) {
                        executor.ExecutePlace(world, this._entities, target);
                        action = () => HumanAction.ManageEntities(Dialog.ShowEntitiesMode.Unplaced);
                    }
                }

                // execute automatic user action, if any
                if (action == null)
                    AsyncAction.EndRun();
                else
                    AsyncAction.BeginInvoke(delegate { action(); AsyncAction.EndRun(); });
            });
        }

        #endregion
        #region ShowLosses

        /// <summary>
        /// Shows projected losses for an implicit <see cref="AttackCommand"/> with the specified
        /// target location.</summary>
        /// <param name="target">
        /// The coordinates of the target <see cref="Site"/> for the <see cref="AttackCommand"/>.
        /// </param>
        /// <remarks><para>
        /// <b>ShowLosses</b> clears the current <see cref="StatusBar"/> message if there are no
        /// candidate units awaiting implicit target selection for an <see cref="AttackCommand"/>,
        /// or if the specified <paramref name="target"/> is not valid for these units.
        /// </para><para>
        /// <b>ShowLosses</b> then invokes <see cref="Unit.EstimateLosses"/> and shows the resulting
        /// percentage losses for an <see cref="AttackCommand"/> with the current candidate units
        /// and the specified <paramref name="target"/> site in the <see cref="StatusBar"/>.
        /// </para></remarks>

        public void ShowLosses(PointI target) {

            // clear status bar if target invalid
            if (this._attackUnits.Count == 0 || !this._attackTargets.Contains(target)) {
                MainWindow.Instance.StatusMessage.Clear();
                return;
            }

            // compute estimated combat losses
            Unit unit = (Unit) this._attackUnits[0];
            CombatResults results = unit.EstimateLosses(
                Session.Instance.WorldState, this._attackUnits, target, true);

            // show estimates in status bar
            MainWindow.Instance.StatusMessage.Text = String.Format(ApplicationInfo.Culture,
                Global.Strings.InfoAttackLosses, results.AttackerPercent, results.DefenderPercent);
        }

        #endregion
    }
}
