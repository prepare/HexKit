using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Tektosyne;
using Tektosyne.Collections;
using Tektosyne.Geometry;
using Tektosyne.Graph;

using Hexkit.Global;
using Hexkit.Scenario;
using Hexkit.World.Commands;
using Hexkit.World.Instructions;

namespace Hexkit.World {

    using EntityList = KeyedList<String, Entity>;

    /// <summary>
    /// Represents a unit that belongs to a <see cref="Faction"/> and may also appear on a <see
    /// cref="Site"/>.</summary>
    /// <remarks><para>
    /// <b>Unit</b> may be overridden by the rule script to define actual unit semantics.
    /// </para><para>
    /// The default semantics allow units to be placed on the map or reside in a faction's
    /// inventory. Placed units can move across the map, capture sites for their faction, and fight
    /// other units that belong to a different faction.
    /// </para><para>
    /// Units are always owned by a faction while they are still in the game. However, the <see
    /// cref="Entity.Owner"/> property returns a null reference when a unit has been destroyed in
    /// battle.</para></remarks>

    public class Unit: Entity {
        #region Unit(Unit)

        /// <overloads>
        /// Initializes a new instance of the <see cref="Unit"/> class.</overloads>
        /// <summary>
        /// Initializes a new instance of the <see cref="Unit"/> class that is a deep copy of the
        /// specified instance.</summary>
        /// <param name="unit">
        /// The <see cref="Unit"/> object whose properties should be copied to the new instance.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="unit"/> is a null reference.</exception>
        /// <remarks><para>
        /// This constructor is called by <see cref="Clone"/> to perform a deep copy of the
        /// specified <paramref name="unit"/>. Please refer to <see cref="Entity(Entity)"/> for
        /// details.
        /// </para><para>
        /// Of the additional properties of the <see cref="Unit"/> class, the values of <see
        /// cref="CanAttack"/> and <see cref="CanMove"/> are copied to the new instance. The
        /// remaining property values are either provided by the underlying <see cref="UnitClass"/>
        /// or derived from other properties.</para></remarks>

        protected Unit(Unit unit): base(unit) {

            this._canAttack = unit._canAttack;
            this._canMove = unit._canMove;
        }

        #endregion
        #region Unit(UnitClass)

        /// <summary>
        /// Initializes a new instance of the <see cref="Unit"/> class based on the specified <see
        /// cref="Scenario.UnitClass"/>.</summary>
        /// <param name="unitClass">
        /// The initial value for the <see cref="UnitClass"/> property.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="unitClass"/> is a null reference.</exception>
        /// <remarks>
        /// Clients should use factory methods to instantiate the <see cref="Unit"/> class, either
        /// <see cref="Entity.CreateEntity"/> or an equivalent method defined by the rule script.
        /// </remarks>

        public Unit(UnitClass unitClass): base(unitClass) { }

        #endregion
        #region Private Fields

        // property backers
        private bool _canAttack = true, _canMove = true;

        #endregion
        #region Public Properties
        #region AttackRange

        /// <summary>
        /// Gets the maximum range for an <see cref="AttackCommand"/>.</summary>
        /// <value>
        /// The maximum range, in map sites, over which the <see cref="Unit"/> could possibly
        /// perform an <see cref="AttackCommand"/>. This value is never negative.</value>
        /// <remarks><para>
        /// <b>AttackRange</b> returns one of the following values:
        /// </para><list type="bullet"><item>
        /// One if the <see cref="Scenario.UnitClass.AttackRangeAttribute"/> identifier of the
        /// underlying <see cref="UnitClass"/> is an empty string.
        /// </item><item>
        /// Zero if <see cref="Scenario.UnitClass.AttackRangeAttribute"/> is a valid string that
        /// does not match any identifier in the <see cref="Entity.Attributes"/> collection.
        /// </item><item>
        /// Otherwise, the value of the <see cref="Entity.Attributes"/> element identified by <see
        /// cref="Scenario.UnitClass.AttackRangeAttribute"/>.
        /// </item></list><para>
        /// An <b>AttackRange</b> of zero indicates that the <see cref="Unit"/> cannot join the
        /// attacking side in any <see cref="AttackCommand"/>. It can still be the target of an
        /// <b>AttackCommand</b> and thus join the defending side, however.
        /// </para><para>
        /// Use the <see cref="SetAttackRange"/> method to set this property, assuming it is backed
        /// by an <see cref="Entity.Attributes"/> element.</para></remarks>

        public int AttackRange {
            get {
                if (!String.IsNullOrEmpty(UnitClass.AttackRangeAttribute))
                    return Attributes.GetValue(UnitClass.AttackRangeAttribute);

                return 1;
            }
        }

        #endregion
        #region CanAttack

        /// <summary>
        /// Gets a value indicating whether the <see cref="Unit"/> can currently attack.</summary>
        /// <value>
        /// <c>true</c> if the <see cref="Unit"/> can currently perform commands that involve
        /// attacking another <see cref="Site"/>; otherwise, <c>false</c>. The default is
        /// <c>true</c>.</value>
        /// <remarks><para>
        /// <b>CanAttack</b> returns <c>false</c> in the following cases:
        /// </para><list type="bullet"><item>
        /// <b>CanAttack</b> has been set to <c>false</c> using <see cref="SetCanAttack"/>.
        /// </item><item>
        /// <see cref="IsCombat"/> is <c>false</c>.
        /// </item><item>
        /// <see cref="Morale"/> is zero or negative.
        /// </item></list><para>
        /// Under the default rules, <b>CanAttack</b> is set to <c>false</c> by any <see
        /// cref="AttackCommand"/> and reset to <c>true</c> by any <see cref="EndTurnCommand"/>.
        /// </para><para>
        /// <b>CanAttack</b> is set by the method <see cref="SetCanAttack"/>. Use the HCL
        /// instruction <see cref="Command.SetUnitCanAttack"/> to set this property while executing
        /// a game command.</para></remarks>

        public bool CanAttack {
            get { return (this._canAttack && IsCombat && Morale > 0); }
        }

        #endregion
        #region CanBuild

        /// <summary>
        /// Gets a value indicating whether the <see cref="Entity.Owner"/> can build new <see
        /// cref="Unit"/> objects based on the same <see cref="UnitClass"/>.</summary>
        /// <value>
        /// <c>true</c> if <see cref="Entity.Owner"/> is not a null reference, and its <see
        /// cref="Faction.BuildableUnits"/> collection contains the underlying <see
        /// cref="UnitClass"/>; otherwise, <c>false</c>.</value>
        /// <remarks>
        /// If <b>CanBuild</b> is <c>true</c>, the current <see cref="Entity.Owner"/> can issue a
        /// <see cref="BuildCommand"/> to build new units of the same <see cref="UnitClass"/> as the
        /// current <see cref="Unit"/>. Otherwise, only the scenario and the rule script can assign
        /// such units to the <b>Owner</b>.</remarks>

        public bool CanBuild {
            get {
                return (Owner == null ? false :
                    Owner.BuildableUnits.ContainsKey(UnitClass.Id));
            }
        }

        #endregion
        #region CanCapture

        /// <summary>
        /// Gets a value indicating whether the <see cref="Unit"/> may capture map sites for its
        /// <see cref="Entity.Owner"/>.</summary>
        /// <value>
        /// The value of the <see cref="Scenario.UnitClass.CanCapture"/> flag of the underlying <see
        /// cref="UnitClass"/>.</value>
        /// <remarks>
        /// If <b>CanCapture</b> is <c>true</c>, the <see cref="Unit"/> transfers any <see
        /// cref="Entity.Site"/> it occupies to its <see cref="Entity.Owner"/>, provided that the
        /// <see cref="Site"/> contains at least one <see cref="Site.Terrains"/> element whose <see
        /// cref="Terrain.CanCapture"/> flag is also <c>true</c>. Otherwise, the <see
        /// cref="Site.Owner"/> of the <see cref="Site"/> remains unchanged.</remarks>

        public bool CanCapture {
            [DebuggerStepThrough]
            get { return UnitClass.CanCapture; }
        }

        #endregion
        #region CanDefendOnly

        /// <summary>
        /// Gets a value indicating whether the <see cref="Unit"/> is limited to passive defense.
        /// </summary>
        /// <value>
        /// <c>true</c> if <see cref="IsCombat"/> is <c>false</c>; otherwise, the value of the <see
        /// cref="Scenario.UnitClass.CanDefendOnly"/> flag of the underlying <see
        /// cref="UnitClass"/>.</value>
        /// <remarks><para>
        /// If <b>CanDefendOnly</b> is <c>true</c>, the <see cref="Unit"/> cannot join the attacking
        /// side in any <see cref="AttackCommand"/>. It can still be the target of an
        /// <b>AttackCommand</b> and thus join the defending side, however.
        /// </para><para>
        /// All <see cref="Faction.Units"/> of a <see cref="Faction"/> whose <b>CanDefendOnly</b>
        /// and <see cref="IsCombat"/> flags are both <c>true</c> have their <see cref="CanAttack"/>
        /// flag cleared during the faction's turn. This prevents such units from attacking on their
        /// own initiative, but still allows them to counter-attack while defending.
        /// </para></remarks>

        public bool CanDefendOnly {
            get { return (UnitClass.CanDefendOnly || !IsCombat); }
        }

        #endregion
        #region CanHeal

        /// <summary>
        /// Gets a value indicating whether the <see cref="Unit"/> may replenish its <see
        /// cref="Strength"/> after taking damage.</summary>
        /// <value>
        /// The value of the <see cref="Scenario.UnitClass.CanHeal"/> flag of the underlying <see
        /// cref="UnitClass"/>.</value>
        /// <remarks><para>
        /// If <b>CanHeal</b> is <c>true</c>, the scenario provides some way for the <see
        /// cref="Unit"/> to replenish its <see cref="Strength"/> after taking damage. Otherwise,
        /// its <see cref="Strength"/> can only decrease but never increase.
        /// </para><note type="implementnotes">
        /// The actual healing mechanism must be implemented by the scenario author, typically using
        /// <see cref="Entity.Resources"/> and <see cref="Entity.ResourceModifiers"/>.
        /// <b>CanHeal</b> merely informs computer player algorithms that such a mechanism exists.
        /// </note></remarks>

        public bool CanHeal {
            [DebuggerStepThrough]
            get { return UnitClass.CanHeal; }
        }

        #endregion
        #region CanMove

        /// <summary>
        /// Gets a value indicating whether the <see cref="Unit"/> can currently move.</summary>
        /// <value>
        /// <c>true</c> if the <see cref="Unit"/> can currently perform commands that involve
        /// movement to another <see cref="Site"/>; otherwise, <c>false</c>. The default is
        /// <c>true</c>.</value>
        /// <remarks><para>
        /// <b>CanMove</b> returns <c>false</c> in the following cases:
        /// </para><list type="bullet"><item>
        /// <b>CanMove</b> has been set to <c>false</c> using <see cref="SetCanMove"/>.
        /// </item><item>
        /// <see cref="IsMobile"/> is <c>false</c>.
        /// </item></list><para>
        /// Under the default rules, <b>CanMove</b> is set to <c>false</c> by any <see
        /// cref="MoveCommand"/> and reset to <c>true</c> by any <see cref="EndTurnCommand"/>.
        /// </para><para>
        /// <b>CanMove</b> is set by the method <see cref="SetCanMove"/>. Use the HCL instruction
        /// <see cref="Command.SetUnitCanMove"/> to set this property while executing a game
        /// command.</para></remarks>

        public bool CanMove {
            get { return (this._canMove && IsMobile); }
        }

        #endregion
        #region IsActive

        /// <summary>
        /// Gets a value indicating whether the <see cref="Unit"/> is still in the game, placed on
        /// the map, and can currently attack or move.</summary>
        /// <value>
        /// <c>true</c> if <see cref="IsAlive"/>, <see cref="Entity.IsPlaced"/>, and either or both
        /// of <see cref="CanAttack"/> and <see cref="CanMove"/> are <c>true</c>; otherwise,
        /// <c>false</c>.</value>
        /// <remarks>
        /// <b>IsActive</b> provides a shortcut for a test that is frequently performed by computer
        /// players.</remarks>

        public bool IsActive {
            get { return (IsAlive && IsPlaced && (CanAttack || CanMove)); }
        }

        #endregion
        #region IsAlive

        /// <summary>
        /// Gets a value indicating whether the <see cref="Unit"/> is still in the game.</summary>
        /// <value>
        /// <c>true</c> if <see cref="Entity.Owner"/> does not return a null reference; otherwise,
        /// <c>false</c>.</value>
        /// <remarks>
        /// <b>IsAlive</b> returns <c>false</c> exactly if the <see cref="Unit"/> has been deleted
        /// from the game.</remarks>

        public bool IsAlive {
            [DebuggerStepThrough]
            get { return (Owner != null); }
        }

        #endregion
        #region IsCombat

        /// <summary>
        /// Gets a value indicating whether the <see cref="Unit"/> is capable of attacking.
        /// </summary>
        /// <value>
        /// <c>true</c> if <see cref="AttackRange"/> is greater than zero; otherwise, <c>false</c>.
        /// </value>
        /// <remarks>
        /// <b>IsCombat</b> indicates whether the <see cref="Unit"/> is at all capable of attacking,
        /// including defensive counter-attacks. This property may return <c>true</c> even when <see
        /// cref="CanAttack"/> is <c>false</c>, or when <see cref="CanDefendOnly"/> is <c>true</c>.
        /// </remarks>

        public bool IsCombat {
            [DebuggerStepThrough]
            get { return (AttackRange > 0); }
        }

        #endregion
        #region IsMobile

        /// <summary>
        /// Gets a value indicating whether the <see cref="Unit"/> is capable of movement.</summary>
        /// <value>
        /// <c>true</c> if <see cref="Movement"/> is greater than zero; otherwise, <c>false</c>.
        /// </value>
        /// <remarks>
        /// <b>IsMobile</b> indicates whether the <see cref="Unit"/> is at all capable of movement.
        /// This property may return <c>true</c> even when <see cref="CanMove"/> returns
        /// <c>false</c>.</remarks>

        public bool IsMobile {
            [DebuggerStepThrough]
            get { return (Movement > 0); }
        }

        #endregion
        #region Morale

        /// <summary>
        /// Gets the current morale of the <see cref="Unit"/> with respect to an <see
        /// cref="AttackCommand"/>.</summary>
        /// <value>
        /// A non-negative <see cref="Int32"/> value indicating the current morale of the <see
        /// cref="Unit"/>.</value>
        /// <remarks><para>
        /// <b>Morale</b> returns one of the following values:
        /// </para><list type="bullet"><item>
        /// One if the <see cref="Scenario.UnitClass.MoraleResource"/> identifier of the underlying
        /// <see cref="UnitClass"/> is an empty string.
        /// </item><item>
        /// Zero if <see cref="Scenario.UnitClass.MoraleResource"/> is a valid string that does not
        /// match any identifier in the <see cref="Entity.Resources"/> collection.
        /// </item><item>
        /// Otherwise, the value of the <see cref="Entity.Resources"/> element identified by <see
        /// cref="Scenario.UnitClass.MoraleResource"/>.
        /// </item></list><para>
        /// Use the <see cref="SetMorale"/> method to set this property, assuming it is backed by a
        /// <see cref="Entity.Resources"/> element.</para></remarks>

        public int Morale {
            get {
                if (!String.IsNullOrEmpty(UnitClass.MoraleResource))
                    return Resources.GetValue(UnitClass.MoraleResource);

                return 1;
            }
        }

        #endregion
        #region Movement

        /// <summary>
        /// Gets the maximum range for a <see cref="MoveCommand"/>.</summary>
        /// <value>
        /// The maximum range, as total path cost, that the <see cref="Unit"/> could possibly cover
        /// in one <see cref="MoveCommand"/>. This value is never negative.</value>
        /// <remarks><para>
        /// <b>Movement</b> returns one of the following values:
        /// </para><list type="bullet"><item>
        /// One if the <see cref="Scenario.UnitClass.MovementAttribute"/> identifier of the
        /// underlying <see cref="UnitClass"/> is an empty string.
        /// </item><item>
        /// Zero if <see cref="Scenario.UnitClass.MovementAttribute"/> is a valid string that does
        /// not match any identifier in the <see cref="Entity.Attributes"/> collection.
        /// </item><item>
        /// Otherwise, the value of the <see cref="Entity.Attributes"/> element identified by <see
        /// cref="Scenario.UnitClass.MovementAttribute"/>.
        /// </item></list><para>
        /// Use the <see cref="SetMovement"/> method to set this property, assuming it is backed by
        /// an <see cref="Entity.Attributes"/> element.</para></remarks>

        public int Movement {
            get {
                if (!String.IsNullOrEmpty(UnitClass.MovementAttribute))
                    return Attributes.GetValue(UnitClass.MovementAttribute);

                return 1;
            }
        }

        #endregion
        #region RangedAttack

        /// <summary>
        /// Gets a value indicating valid targets for ranged attacks by the <see cref="Unit"/>.
        /// </summary>
        /// <value>
        /// The value of the <see cref="Scenario.UnitClass.RangedAttack"/> property of the
        /// underlying <see cref="UnitClass"/>.</value>
        /// <remarks>
        /// <b>RangedAttack</b> affects the default behavior of <see cref="CanAttackTarget"/>. The
        /// value <see cref="TargetMode.Line"/> requires the use of the <see cref="Visibility{T}"/>
        /// algorithm and the <see cref="Entity.BlocksAttack"/> flag to determine valid targets;
        /// otherwise, only the distance to each target is considered.</remarks>

        public TargetMode RangedAttack {
            [DebuggerStepThrough]
            get { return UnitClass.RangedAttack; }
        }

        #endregion
        #region Strength

        /// <summary>
        /// Gets the current strength of the <see cref="Unit"/> with respect to an <see
        /// cref="AttackCommand"/>.</summary>
        /// <value>
        /// A non-negative <see cref="Int32"/> value indicating the current strength of the <see
        /// cref="Unit"/>.</value>
        /// <remarks><para>
        /// <b>Strength</b> returns one of the following values:
        /// </para><list type="bullet"><item>
        /// One if the <see cref="Scenario.UnitClass.StrengthResource"/> identifier of the
        /// underlying <see cref="UnitClass"/> is an empty string.
        /// </item><item>
        /// Zero if <see cref="Scenario.UnitClass.StrengthResource"/> is a valid string that does
        /// not match any identifier in the <see cref="Entity.Resources"/> collection.
        /// </item><item>
        /// Otherwise, the value of the <see cref="Entity.Resources"/> element identified by <see
        /// cref="Scenario.UnitClass.StrengthResource"/>.
        /// </item></list><para>
        /// Use the <see cref="SetStrength"/> method to set this property, assuming it is backed by
        /// a <see cref="Entity.Resources"/> element.</para></remarks>

        public int Strength {
            get {
                if (!String.IsNullOrEmpty(UnitClass.StrengthResource))
                    return Resources.GetValue(UnitClass.StrengthResource);

                return 1;
            }
        }

        #endregion
        #region UnitClass

        /// <summary>
        /// Gets the scenario class of the <see cref="Unit"/>.</summary>
        /// <value>
        /// The <see cref="Scenario.UnitClass"/> on which the <see cref="Unit"/> is based.</value>
        /// <remarks>
        /// <b>UnitClass</b> returns the value of the <see cref="Entity.EntityClass"/> property,
        /// cast to type <see cref="Scenario.UnitClass"/> for convenience.</remarks>

        public UnitClass UnitClass {
            [DebuggerStepThrough]
            get { return (UnitClass) EntityClass; }
        }

        #endregion
        #endregion
        #region CreateAttackEvent

        /// <summary>
        /// Creates a message event describing the results of the specified <see
        /// cref="AttackCommand"/>.</summary>
        /// <param name="command">
        /// The <see cref="AttackCommand"/> that receives the message event.</param>
        /// <param name="faction">
        /// A <see cref="Faction"/> whose <see cref="Faction.Units"/> participated in the attack,
        /// either as attackers or as defenders.</param>
        /// <param name="units">
        /// An <see cref="IList{T}"/> containing all <see cref="Unit"/> objects that participated in
        /// the attack on the side of <paramref name="faction"/>.</param>
        /// <param name="results">
        /// A <see cref="CombatResults"/> object containing the original total <see
        /// cref="Strength"/> and actual losses of all attacking and defending units.</param>
        /// <param name="attacker">
        /// <c>true</c> if <paramref name="faction"/> and <paramref name="units"/> describe the
        /// attackers; <c>false</c> if they describe the defenders.</param>
        /// <remarks><para>
        /// <b>CreateAttackEvent</b> adds a <see cref="ShowMessageInstruction"/> to the specified
        /// <paramref name="command"/> that shows the loss percentage stored in <paramref
        /// name="results"/> for the side indicated by <paramref name="attacker"/>.
        /// </para><para>
        /// The message event also contains the names of those <paramref name="units"/> elements
        /// that were killed in combat, if any. This includes all <see cref="Unit"/> objects whose
        /// <see cref="Entity.Owner"/> is now a null reference.
        /// </para><para>
        /// Derived classes may override <b>CreateAttackEvent</b> to generate different events for
        /// the <see cref="AttackCommand"/>.</para></remarks>

        protected virtual void CreateAttackEvent(AttackCommand command, Faction faction,
            IList<Entity> units, CombatResults results, bool attacker) {

            // summary shows percentage losses
            string summary = (attacker ?
                String.Format(ApplicationInfo.Culture,
                    Global.Strings.EventLossesAttacker, results.AttackerPercent) :
                String.Format(ApplicationInfo.Culture,
                    Global.Strings.EventLossesDefender, results.DefenderPercent));

            // details show killed units, if any
            IList<Entity> killed = WorldUtility.GetUnownedEntities(units);
            string details = (killed == null ? null : Global.Strings.EventUnitsKilled);

            command.ShowMessage(summary, details, faction.Id, WorldUtility.GetEntityNames(killed));
        }

        #endregion
        #region CreateMoveEvent

        /// <summary>
        /// Creates a map view event visualizing the results of the specified <see
        /// cref="MoveCommand"/>.</summary>
        /// <param name="command">
        /// The <see cref="MoveCommand"/> that receives the map view event.</param>
        /// <param name="units">
        /// An <see cref="IList{T}"/> containing all <see cref="Unit"/> objects that participate in
        /// the move.</param>
        /// <param name="source">
        /// The <see cref="Site"/> where the move starts.</param>
        /// <param name="target">
        /// The <see cref="Site"/> where the move ends.</param>
        /// <remarks><para>
        /// <b>CreateMoveEvent</b> calls <see cref="Finder.FindMovePath"/> to determine the actual
        /// path taken by the specified <paramref name="units"/> while moving from the specified
        /// <paramref name="source"/> to the specified <paramref name="target"/>.
        /// </para><para>
        /// If a path is found, <b>CreateMoveEvent</b> adds a <see cref="MoveImageInstruction"/> to
        /// the specified <paramref name="command"/> that shows the topmost <paramref name="units"/>
        /// element moving along the path.
        /// </para><para>
        /// Derived classes may override <b>CreateMoveEvent</b> to generate different events for the
        /// <see cref="MoveCommand"/>.
        /// </para><note type="implementnotes">
        /// The specified <paramref name="units"/> are removed from the map before
        /// <b>CreateMoveEvents</b> is called. Ignore their <see cref="Entity.Site"/> properties and
        /// use the specified <paramref name="source"/> instead.</note></remarks>

        protected virtual void CreateMoveEvent(MoveCommand command,
            IList<Entity> units, Site source, Site target) {

            // calculate traversed movement path
            WorldState world = command.Context.WorldState;
            var path = Finder.FindMovePath(world, units, source, target, false);

            if (path != null) {
                // get display class of topmost unit
                EntityClass displayClass = units[units.Count - 1].DisplayClass;

                // move display class smoothly along path
                PointI[] sites = path.Nodes.ToArray();
                command.MoveImage(displayClass.Id, sites, 0);
            }
        }

        #endregion
        #region Internal Methods
        #region AddSiteResources

        /// <summary>
        /// Acquires the <see cref="Entity.Resources"/> provided by any other entities on the
        /// current <see cref="Site"/>.</summary>
        /// <param name="command">
        /// The <see cref="Command"/> being executed.</param>
        /// <remarks><para>
        /// <b>AddSiteResources</b> modifies the values in the <see cref="Entity.Resources"/>
        /// collection by adding some or all of the resources provided by other entities located on
        /// the same <see cref="Entity.Site"/>, if any.
        /// </para><para>
        /// <b>AddSiteResources</b> performs the following actions for all elements of the <see
        /// cref="Entity.Resources"/> collection, and for all <see cref="Site.Terrains"/> and <see
        /// cref="Site.Effects"/> on the current <see cref="Entity.Site"/> whose <see
        /// cref="EntityClass.ResourceTransfer"/> property does not equal <see
        /// cref="ResourceTransferMode.None"/>:
        /// </para><list type="bullet"><item>
        /// Compute the transfer amount. This is the matching <see cref="Entity.Resources"/> value
        /// of the current entity, restricted to the difference between the current unit resource
        /// and its <see cref="Variable.Minimum"/> or effective <see cref="Variable.Maximum"/>
        /// value, depending on the sign of the current entity resource.
        /// </item><item>
        /// Add the transfer amount to the current <see cref="Unit"/> resource and subtract it from
        /// the matching resource of the current entity.
        /// </item><item>
        /// If the matching resource of the current entity is now zero, invoke <see
        /// cref="Entity.CheckDepletion"/> on the entity.</item></list></remarks>

        internal void AddSiteResources(Command command) {

            // do nothing if unplaced
            if (!IsPlaced) return;
            EntityCategory[] categories = { EntityCategory.Terrain, EntityCategory.Effect };

            // check all resources for matching entity resources
            for (int i = 0; i < Resources.Count; i++) {
                Variable resource = Resources[i];

                // start with current value
                string id = resource.Id;
                int value = resource.Value;

                // process all local terrain & effects
                foreach (EntityCategory category in categories) {
                    EntityList entities = Site.GetEntities(category);

                    // check all entities for matching resources
                    for (int j = 0; j < entities.Count; j++) {
                        Entity entity = entities[j];
                        if (entity.EntityClass.ResourceTransfer == ResourceTransferMode.None)
                            continue;

                        // check for matching resource value
                        int available = entity.Resources.GetValue(id);
                        if (available == 0) continue;

                        // compute maximum transfer amount
                        int transfer = (available > 0 ?
                            Math.Min(available, resource.Maximum - value) :
                            Math.Max(available, resource.Minimum - value));
                        if (transfer == 0) continue;

                        // transfer maximum amount to unit
                        value += transfer;
                        command.SetEntityVariable(entity.Id, id, available - transfer);

                        // check entity for depletion on zero resource
                        if (available == transfer)
                            entity.CheckDepletion(command, Owner);
                    }
                }

                // update current resource value
                command.SetEntityVariable(Id, id, value);
            }
        }

        #endregion
        #region SetCanAttack

        /// <summary>
        /// Sets the <see cref="CanAttack"/> property to the specified value.</summary>
        /// <param name="value">
        /// The new value for the <see cref="CanAttack"/> property.</param>
        /// <returns>
        /// <c>true</c> if the <see cref="CanAttack"/> property was changed; otherwise,
        /// <c>false</c>.</returns>
        /// <remarks>
        /// <b>SetCanAttack</b> leaves the <see cref="CanAttack"/> property unchanged, regardless of
        /// the specified <paramref name="value"/>, if <see cref="IsCombat"/> is <c>false</c>.
        /// </remarks>

        internal bool SetCanAttack(bool value) {

            if (IsCombat && this._canAttack != value) {
                this._canAttack = value;
                return true;
            } else
                return false;
        }

        #endregion
        #region SetCanMove

        /// <summary>
        /// Sets the <see cref="CanMove"/> property to the specified value.</summary>
        /// <param name="value">
        /// The new value for the <see cref="CanMove"/> property.</param>
        /// <returns>
        /// <c>true</c> if the <see cref="CanMove"/> property was changed; otherwise, <c>false</c>.
        /// </returns>
        /// <remarks>
        /// <b>SetCanMove</b> leaves the <see cref="CanMove"/> property unchanged, regardless of the
        /// specified <paramref name="value"/>, if <see cref="IsMobile"/> is <c>false</c>.</remarks>

        internal bool SetCanMove(bool value) {

            if (IsMobile && this._canMove != value) {
                this._canMove = value;
                return true;
            } else
                return false;
        }

        #endregion
        #region ValidateOwner

        /// <summary>
        /// Validates the specified <see cref="Faction"/> as the new value of the <see
        /// cref="Entity.Owner"/> property.</summary>
        /// <param name="faction">
        /// The <see cref="Faction"/> to validate against the invariants of the <see cref="Unit"/>
        /// class.</param>
        /// <exception cref="InvalidCommandException"><para>
        /// <paramref name="faction"/> is a null reference. Units must alway be owned.
        /// </para><para>-or-</para><para>
        /// <see cref="Entity.Site"/> is valid and contains other <see cref="Site.Units"/> whose
        /// <see cref="Entity.Owner"/> differs from the specified <paramref name="faction"/>. All
        /// stacked units must have the same owner.</para></exception>
        /// <remarks>
        /// <b>ValidateOwner</b> does nothing if validation succeeds.</remarks>

        internal override sealed void ValidateOwner(Faction faction) {

            // units must have valid owner
            if (faction == null)
                InvalidCommandException.ThrowNewWithFormat(Global.Strings.ErrorOwnerUnitNone, Id);

            if (Site == null) return;

            EntityList units = Site.Units;
            Debug.Assert(units.Count >= 1);
            if (units.Count < 2) return;

            // stacked units must have same owner
            for (int i = 0; i < units.Count; i++) {
                Entity unit = units[i];
                if (unit != this && unit.Owner != faction)
                    InvalidCommandException.ThrowNewWithFormat(
                        Global.Strings.ErrorOwnerUnitConflict, Id);
            }
        }

        #endregion
        #region ValidateSite

        /// <summary>
        /// Validates the specified <see cref="Site"/> as the new value of the <see
        /// cref="Entity.Site"/> property.</summary>
        /// <param name="site">
        /// The <see cref="Site"/> to validate against the invariants of the <see cref="Unit"/>
        /// class.</param>
        /// <exception cref="InvalidCommandException">
        /// <paramref name="site"/> and <see cref="Entity.Owner"/> are both valid, and the specified
        /// <paramref name="site"/> contains other <see cref="Site.Units"/> whose <see
        /// cref="Entity.Owner"/> differs from the <b>Owner</b> of the current <see cref="Unit"/>.
        /// All stacked units must have the same owner.</exception>
        /// <remarks>
        /// <b>ValidateSite</b> does nothing if validation succeeds.</remarks>

        internal override sealed void ValidateSite(Site site) {
            if (site == null || Owner == null) return;
            EntityList units = site.Units;

            // stacked units must have same owner
            for (int i = 0; i < units.Count; i++)
                if (units[i].Owner != Owner)
                    InvalidCommandException.ThrowNewWithFormat(
                        Global.Strings.ErrorSiteUnitConflict, Id);
        }

        #endregion
        #endregion
        #region Public Methods
        #region Attack

        /// <summary>
        /// Executes the specified <see cref="AttackCommand"/>.</summary>
        /// <param name="command">
        /// The <see cref="AttackCommand"/> to execute.</param>
        /// <remarks><para>
        /// <b>Attack</b> performs the following actions:
        /// </para><list type="number"><item>
        /// Call <see cref="AttackCombat"/> to perform the actual combat described by the specified
        /// <paramref name="command"/>.
        /// </item><item>
        /// Call <see cref="CreateAttackEvent"/> twice to add message events with the combat results
        /// to the <paramref name="command"/>.
        /// </item><item>
        /// Set <see cref="CanAttack"/> to <c>false</c> for all elements in the command's <see
        /// cref="Command.Entities"/> collection.
        /// </item></list><para>
        /// Derived classes may override <b>Attack</b> to implement different semantics for <see
        /// cref="AttackCommand"/>.
        /// </para><para>
        /// The command's <see cref="Command.Entities"/> collection always contains at least one
        /// element, and the <b>Value</b> of the first element equals this <see cref="Unit"/>.
        /// </para></remarks>

        public virtual void Attack(AttackCommand command) {
            Debug.Assert(command.Entities[0].Value == this);

            /*
             * defenders and attacker/defenderOwner are necessary copies
             * to retain references beyond the AttackCombat call which will
             * delete defenders from the Target site if they are killed,
             * and set the Owner of any killed unit to a null reference.
             */

            EntityList attackers = EntityReference.GetEntities(command.Entities);
            EntityList defenders = new EntityList(command.Target.Value.Units);

            Faction attackerOwner = Owner;
            Faction defenderOwner = defenders[0].Owner;

            // compute original strength of both sides
            CombatResults results = new CombatResults();
            results.AttackerStrength = WorldUtility.AddStrength(attackers);
            results.DefenderStrength = WorldUtility.AddStrength(defenders);

            // perform actual combat
            AttackCombat(command);

            // compute remaining strength of both sides
            results.AttackerRemainder = WorldUtility.AddStrength(attackers);
            results.DefenderRemainder = WorldUtility.AddStrength(defenders);

            // create event messages for both sides
            CreateAttackEvent(command, attackerOwner, attackers, results, true);
            CreateAttackEvent(command, defenderOwner, defenders, results, false);

            // mark all surviving attackers as exhausted
            for (int i = 0; i < attackers.Count; i++) {
                Unit unit = (Unit) attackers[i];
                if (unit.IsAlive)
                    command.SetUnitCanAttack(unit.Id, false);
            }
        }

        #endregion
        #region AttackCombat

        /// <summary>
        /// Performs the actual combat implied by the specified <see cref="AttackCommand"/>.
        /// </summary>
        /// <param name="command">
        /// The <see cref="AttackCommand"/> being executed.</param>
        /// <remarks><para>
        /// <b>AttackCombat</b> performs combat in two stages: attack and counter-attack.
        /// </para><para>
        /// During the attack, <b>AttackCombat</b> invokes <see cref="SelectDefender"/> on all
        /// elements in the <see cref="Command.Entities"/> collection of the specified <paramref
        /// name="command"/>, and then <see cref="AttackUnit"/> with the result, unless it is a null
        /// reference.
        /// </para><para>
        /// During the counter-attack, <b>AttackCombat</b> invokes <see cref="SelectAttacker"/> on
        /// all remaining elements in the <see cref="Site.Units"/> collection of the <see
        /// cref="Command.Target"/> site of the specified <paramref name="command"/>, and then
        /// <b>AttackUnit</b> with the result, unless it is a null reference.
        /// </para><para>
        /// Derived classes may override <b>AttackCombat</b> to implement different semantics for
        /// <see cref="AttackCommand"/>.
        /// </para><para>
        /// The command's <see cref="Command.Entities"/> collection always contains at least one
        /// element, and the <b>Value</b> of the first element equals this <see cref="Unit"/>.
        /// </para></remarks>

        public virtual void AttackCombat(AttackCommand command) {
            Debug.Assert(command.Entities[0].Value == this);

            // let all attackers attack
            foreach (EntityReference unit in command.Entities) {
                Unit attacker = (Unit) unit.Value;
                Unit defender = attacker.SelectDefender(command);
                if (defender != null) attacker.AttackUnit(command, defender);
            }

            EntityList defenders = command.Target.Value.Units;

            // let surviving defenders counter-attack
            for (int i = 0; i < defenders.Count; i++) {
                Unit defender = (Unit) defenders[i];
                Unit attacker = defender.SelectAttacker(command);
                if (attacker != null) defender.AttackUnit(command, attacker);
            }
        }

        #endregion
        #region AttackUnit

        /// <summary>
        /// Attacks the specified <see cref="Unit"/> during the specified <see
        /// cref="AttackCommand"/>.</summary>
        /// <param name="command">
        /// The <see cref="AttackCommand"/> being executed.</param>
        /// <param name="defender">
        /// Another <see cref="Unit"/> instance that is attacked by this instance.</param>
        /// <remarks><para>
        /// <b>AttackUnit</b> invokes <see cref="Entity.Delete"/> on the specified <paramref
        /// name="defender"/>.
        /// </para><para>
        /// Derived classes may override <b>AttackUnit</b> to implement different semantics for <see
        /// cref="AttackCommand"/>. The default implementation of <see cref="AttackCombat"/> calls
        /// this method to perform both attacks and counter-attacks.
        /// </para><para>
        /// The command's <see cref="Command.Entities"/> collection always contains at least one
        /// element. The collection contains either the current <see cref="Unit"/> or the specified
        /// <paramref name="defender"/>, but never both.</para></remarks>

        public virtual void AttackUnit(AttackCommand command, Unit defender) {

            // always kill target
            defender.Delete(command);
        }

        #endregion
        #region CanAttackTarget(..., PointI)

        /// <overloads>
        /// Determines whether the specified units can attack the specified target location.
        /// </overloads>
        /// <summary>
        /// Determines whether the specified units can attack the specified target location from
        /// their current locations.</summary>
        /// <param name="worldState">
        /// The <see cref="WorldState"/> on which to execute the <see cref="AttackCommand"/>.
        /// </param>
        /// <param name="units">
        /// An <see cref="IList{T}"/> containing all <see cref="Unit"/> objects that participate in
        /// the attack. The first element equals this <b>Unit</b>.</param>
        /// <param name="target">
        /// The coordinates of the <see cref="Site"/> that is the target of the attack.</param>
        /// <returns>
        /// <c>true</c> if the specified <paramref name="units"/> can attack the specified <paramref
        /// name="target"/> from their current locations; otherwise, <c>false</c>.</returns>
        /// <remarks><para>
        /// <b>CanAttackTarget</b> returns <c>true</c> exactly if the specified <paramref
        /// name="target"/> site contains at least one unit owned by another faction, and <see
        /// cref="Finder.AreUnitsInAttackRange"/> returns <c>true</c> for the specified arguments.
        /// </para><para>
        /// Derived classes may override <b>CanAttackTarget</b> to implement different semantics for
        /// <see cref="AttackCommand"/>. Note that this overload is called during command execution.
        /// It should be <em>pessimistic</em> and only succeed if the specified <paramref
        /// name="units"/> can actually attack the specified <paramref name="target"/>.
        /// </para><note type="caution">
        /// Do not change any reference parameters or their contents! Create a copy if you need to
        /// change them.</note></remarks>

        public virtual bool CanAttackTarget(
            WorldState worldState, IList<Entity> units, PointI target) {

            // target must contain enemy units
            Site site = worldState.GetSite(target);
            if (!site.HasAlienUnits(Owner))
                return false;

            // target must be within range of all units
            return Finder.AreUnitsInAttackRange(worldState, units, target);
        }

        #endregion
        #region CanAttackTarget(..., PointI, PointI, Int32)

        /// <summary>
        /// Determines whether the specified units could attack the specified target location from
        /// another specified location.</summary>
        /// <param name="worldState">
        /// The <see cref="WorldState"/> on which to execute the <see cref="AttackCommand"/>.
        /// </param>
        /// <param name="units">
        /// An <see cref="IList{T}"/> containing all <see cref="Unit"/> objects that participate in
        /// the attack. The first element equals this <b>Unit</b>.</param>
        /// <param name="source">
        /// The coordinates of the <see cref="Site"/> from which the specified <paramref
        /// name="units"/> would attack.</param>
        /// <param name="target">
        /// The coordinates of the <see cref="Site"/> that is the target of the attack.</param>
        /// <param name="distance">
        /// The distance, in map sites, between <paramref name="source"/> and <paramref
        /// name="target"/>. This argument may be negative if the distance is unknown.</param>
        /// <returns>
        /// <c>true</c> if the specified <paramref name="units"/> could attack <paramref
        /// name="target"/> from <paramref name="source"/>; otherwise, <c>false</c>.</returns>
        /// <remarks><para>
        /// <b>CanAttackTarget</b> returns <c>true</c> exactly if the specified <paramref
        /// name="target"/> site contains at least one unit owned by another faction, and <see
        /// cref="Finder.AreUnitsInAttackRange"/> returns <c>true</c> for the specified arguments.
        /// </para><para>
        /// Derived classes may override <b>CanAttackTarget</b> to implement different semantics for
        /// <see cref="AttackCommand"/>. Note that this overload is called to select eligible units.
        /// It should be <em>optimistic</em> and succeed if there is any chance that the specified
        /// <paramref name="units"/> could attack <paramref name="target"/> from <paramref
        /// name="source"/>, even if that would require the help of other units.
        /// </para><note type="caution">
        /// Do not change any reference parameters or their contents! Create a copy if you need to
        /// change them.</note></remarks>

        public virtual bool CanAttackTarget(WorldState worldState,
            IList<Entity> units, PointI source, PointI target, int distance) {

            // target must contain enemy units
            Site site = worldState.GetSite(target);
            if (!site.HasAlienUnits(Owner))
                return false;

            // target must be within range of source site
            return Finder.AreUnitsInAttackRange(worldState, units, source, target, distance);
        }

        #endregion
        #region CanMakeStep

        /// <summary>
        /// Determines whether the specified units can move from one specified map location to
        /// another neighboring map location.</summary>
        /// <param name="worldState">
        /// The <see cref="WorldState"/> on which to execute the <see cref="MoveCommand"/>.</param>
        /// <param name="units">
        /// An <see cref="IList{T}"/> containing all <see cref="Unit"/> objects that participate in
        /// the move. The first element equals this <b>Unit</b>.</param>
        /// <param name="originalSource">
        /// The coordinates of the <see cref="Site"/> where the specified <paramref name="units"/>
        /// are originally located, before the <see cref="MoveCommand"/> is executed.</param>
        /// <param name="source">
        /// The coordinates of the <see cref="Site"/> where the current movement step starts.
        /// </param>
        /// <param name="target">
        /// The coordinates of the <see cref="Site"/> where the current movement step ends. This
        /// location must be a neighbor of <paramref name="source"/>.</param>
        /// <returns>
        /// <c>true</c> if the specified <paramref name="units"/> can move from <paramref
        /// name="source"/> to <paramref name="target"/>; otherwise, <c>false</c>.</returns>
        /// <remarks><para>
        /// <b>CanMakeStep</b> returns <c>false</c> if either of the following conditions holds,
        /// otherwise <c>true</c>:
        /// </para><list type="bullet"><item>
        /// The total <see cref="Site.Difficulty"/> of the specified <paramref name="target"/> site
        /// is zero, indicating that this site does not allow movement.
        /// </item><item>
        /// The specified <paramref name="target"/> site contains enemy <see cref="Site.Units"/>.
        /// </item></list><para>
        /// Derived classes may override <b>CanMakeStep</b> to implement different semantics for 
        /// <see cref="MoveCommand"/>. When doing so, observe the following guidelines:
        /// </para><list type="bullet"><item>
        /// Check whether the current contents of the specified <paramref name="target"/> site allow
        /// the <em>temporary</em> presence of the specified <paramref name="units"/> during a <see
        /// cref="MoveCommand"/>.
        /// </item><item>
        /// Ignore the current <see cref="Entity.Site"/> values of the specified <paramref
        /// name="units"/> since they might be invalid. Use the specified <paramref
        /// name="originalSource"/> site instead.
        /// </item><item>
        /// Assume that <paramref name="target"/> equals neither <paramref name="originalSource"/>
        /// nor the current <see cref="Entity.Site"/> of the specified <paramref name="units"/>.
        /// </item><item>
        /// Override <see cref="CanOccupy"/> if you wish to test for additional restrictions
        /// concerning the permanent occupation of the specified <paramref name="target"/> site.
        /// </item></list><note type="caution">
        /// Do not change any reference parameters or their contents! Create a copy if you need to
        /// change them.</note></remarks>

        public virtual bool CanMakeStep(WorldState worldState, IList<Entity> units,
            PointI originalSource, PointI source, PointI target) {

            // cannot move without difficulty value
            Site site = worldState.GetSite(target);
            if (site.Difficulty == 0) return false;

            // enemy units prevent movement
            return !site.HasAlienUnits(Owner);
        }

        #endregion
        #region CanOccupy

        /// <summary>
        /// Determines whether the specified units can occupy the specified target location.
        /// </summary>
        /// <param name="worldState">
        /// The <see cref="WorldState"/> on which to execute the <see cref="MoveCommand"/>.</param>
        /// <param name="units">
        /// An <see cref="IList{T}"/> containing all <see cref="Unit"/> objects that participate in
        /// the move. The first element equals this <b>Unit</b>.</param>
        /// <param name="originalSource">
        /// The coordinates of the <see cref="Site"/> where the specified <paramref name="units"/>
        /// are originally located, before the <see cref="MoveCommand"/> is executed.</param>
        /// <param name="target">
        /// The coordinates of the <see cref="World.Site"/> that the specified <paramref
        /// name="units"/> should occupy.</param>
        /// <returns>
        /// <c>true</c> if the specified <paramref name="units"/> can occupy the specified <paramref
        /// name="target"/>; otherwise, <c>false</c>.</returns>
        /// <remarks><para>
        /// <b>CanOccupy</b> always returns <c>true</c>.
        /// </para><para>
        /// Derived classes may override <b>CanOccupy</b> to implement different semantics for <see
        /// cref="MoveCommand"/>. When doing so, observe the following guidelines:
        /// </para><list type="bullet"><item>
        /// Check whether the current contents of the specified <paramref name="target"/> site allow
        /// the <em>permanent</em> presence of the specified <paramref name="units"/> after the <see
        /// cref="MoveCommand"/> is complete.
        /// </item><item>
        /// Assume that the specified <paramref name="target"/> site allows the temporary presence
        /// of the specified <paramref name="units"/> during a <see cref="MoveCommand"/>. Assume
        /// that any related tests in <see cref="CanMakeStep"/> have already succeeded.
        /// </item><item>
        /// Ignore the current <see cref="Entity.Site"/> values of the specified <paramref
        /// name="units"/> since they might be invalid. Use the specified <paramref
        /// name="originalSource"/> site instead.
        /// </item><item>
        /// Assume that <paramref name="target"/> equals neither <paramref name="originalSource"/>
        /// nor the current <see cref="Entity.Site"/> of the specified <paramref name="units"/>.
        /// </item></list><note type="caution">
        /// Do not change any reference parameters or their contents! Create a copy if you need to
        /// change them.</note></remarks>

        public virtual bool CanOccupy(WorldState worldState,
            IList<Entity> units, PointI originalSource, PointI target) {

            Debug.Assert(worldState != null);
            Debug.Assert(units != null);
            Debug.Assert(units.Count > 0);
            Debug.Assert(units[0] == this);

            return true;
        }

        #endregion
        #region EstimateLosses

        /// <summary>
        /// Estimates the losses for an <see cref="AttackCommand"/> with the specified target
        /// location and attacking units.</summary>
        /// <param name="worldState">
        /// The <see cref="WorldState"/> on which to execute the <see cref="AttackCommand"/>.
        /// </param>
        /// <param name="attackers">
        /// An <see cref="IList{T}"/> containing all <see cref="Unit"/> objects that participate in
        /// the attack. The first element equals this <b>Unit</b>.</param>
        /// <param name="target">
        /// The coordinates of the <see cref="Site"/> that is the target of the attack.</param>
        /// <param name="validSites">
        /// <c>true</c> if all <paramref name="attackers"/> occupy the actual <see
        /// cref="Entity.Site"/> from which they would attack <paramref name="target"/>;
        /// <c>false</c> if their current <b>Site</b> should be ignored.</param>
        /// <returns>
        /// A <see cref="CombatResults"/> object containing the current total <see cref="Strength"/>
        /// and likely losses of all attacking and defending units.</returns>
        /// <remarks><para>
        /// <b>EstimateLosses</b> initializes the returned <see cref="CombatResults"/> object as
        /// follows:
        /// </para><list type="bullet"><item>
        /// Set <see cref="CombatResults.AttackerStrength"/> to the sum of all <see
        /// cref="Strength"/> values of the specified <paramref name="attackers"/>.
        /// </item><item>
        /// Set <see cref="CombatResults.DefenderStrength"/> to the sum of all <see
        /// cref="Strength"/> values of the <see cref="Site.Units"/> in the specified <paramref
        /// name="target"/> site.
        /// </item><item>
        /// Set <see cref="CombatResults.DefenderLosses"/> to <b>AttackerStrength</b>, indicating
        /// the estimated attack losses. This value is limited to the interval [0,
        /// <b>DefenderStrength</b>].
        /// </item><item>
        /// Set <see cref="CombatResults.AttackerLosses"/> to <b>DefenderStrength</b> minus
        /// <b>AttackerStrength</b>, indicating the estimated counter-attack losses. This value is
        /// limited to the interval [0, <b>AttackerStrength</b>].
        /// </item></list><para>
        /// Derived classes may override <b>EstimateLosses</b> to implement different semantics for
        /// <see cref="AttackCommand"/>.
        /// </para><para>
        /// <b>EstimateLosses</b> may return inaccurate values, but it should return identical
        /// values for identical parameters. This method should execute as fast as possible, as it
        /// is likely to be called very frequently during computer player calculations.
        /// </para><note type="caution">
        /// Do not change any reference parameters or their contents! Create a copy if you need to
        /// change them.</note></remarks>

        public virtual CombatResults EstimateLosses(WorldState worldState,
            IList<Entity> attackers, PointI target, bool validSites) {

            Debug.Assert(worldState != null);
            Debug.Assert(attackers != null);
            Debug.Assert(attackers.Count > 0);
            Debug.Assert(attackers[0] == this);

            // retrieve defenders in target site
            IList<Entity> defenders = worldState.GetSite(target).Units;

            // compute current combined strengths
            CombatResults results = new CombatResults();
            results.AttackerStrength = WorldUtility.AddStrength(attackers);
            results.DefenderStrength = WorldUtility.AddStrength(defenders);

            // estimate strength losses by attack and counter-attack
            results.DefenderLosses = results.AttackerStrength;
            results.AttackerLosses = Math.Max(0, results.DefenderStrength - results.AttackerStrength);

            return results;
        }

        #endregion
        #region GetRelaxedRange

        /// <summary>
        /// Gets the <see cref="UnitAgent.RelaxedRange"/> pathfinding parameter for a <see
        /// cref="MoveCommand"/> performed by the specified units.</summary>
        /// <param name="worldState">
        /// The <see cref="WorldState"/> on which to execute the <see cref="MoveCommand"/>.</param>
        /// <param name="units">
        /// An <see cref="IList{T}"/> containing all <see cref="Unit"/> objects that participate in
        /// the move.</param>
        /// <returns>
        /// The <see cref="UnitAgent.RelaxedRange"/> value to be used by the <see cref="UnitAgent"/>
        /// that performs pathfinding for the specified <paramref name="units"/>.</returns>
        /// <remarks><para>
        /// <b>GetRelaxedRange</b> always returns <c>false</c>. Please refer to <see
        /// cref="IGraphAgent{T}.RelaxedRange"/> for a description of this pathfinding parameter.
        /// </para><para>
        /// Derived classes may override <b>GetRelaxedRange</b> to implement different semantics for
        /// <see cref="MoveCommand"/>.
        /// </para><note type="caution">
        /// Do not change any reference parameters or their contents! Create a copy if you need to
        /// change them.</note></remarks>

        public virtual bool GetRelaxedRange(WorldState worldState, IList<Entity> units) {

            Debug.Assert(worldState != null);
            Debug.Assert(units != null);
            Debug.Assert(units.Count > 0);
            Debug.Assert(units[0] == this);

            return false;
        }

        #endregion
        #region GetRequiredSupplies

        /// <summary>
        /// Gets a list of all <see cref="Entity.Resources"/> that should be resupplied.</summary>
        /// <returns>
        /// An array of <see cref="PointI"/> values whose <see cref="PointI.X"/> components indicate
        /// the required amount of the <see cref="Faction.SupplyResources"/> element at the same
        /// index position, and whose <see cref="PointI.Y"/> component indicates the corresponding
        /// resupply priority.</returns>
        /// <remarks><para>
        /// <b>GetRequiredSupplies</b> always returns a null reference if <see
        /// cref="Faction.SupplyResources"/> is an empty collection.
        /// </para><para>
        /// Otherwise, <b>GetRequiredSupplies</b> returns a <see cref="PointI"/> array of the same
        /// length as <b>SupplyResources</b>. The <see cref="PointI.X"/> component of each element
        /// is set to the difference between the corresponding <see cref="Variable.InitialValue"/>
        /// and current <see cref="Variable.Value"/> in the <see cref="Entity.Resources"/>
        /// collection. The <see cref="PointI.Y"/> component is set to the <b>X</b> component,
        /// multiplied by 100 and divided by the <b>InitialValue</b>, rounded down.
        /// </para><para>
        /// For any <b>SupplyResources</b> element, both <see cref="PointI"/> components are set to
        /// zero if no corresponding <b>Resources</b> element exists, or if the <b>InitialValue</b>
        /// or its difference to the current <b>Value</b> is equal to or less than zero.
        /// </para><para>
        /// Derived classes may override <b>GetRequiredSupplies</b> to indicate which <see
        /// cref="Entity.Resources"/> can or should be resupplied.
        /// </para><para>
        /// All <b>Y</b> components in the returned array should range from zero to 100, with zero
        /// indicating the lowest priority and 100 indicating the highest priority for resupply.
        /// </para></remarks>

        public virtual PointI[] GetRequiredSupplies() {

            // check if supply is enabled
            if (Owner.SupplyResources.Count == 0)
                return null;

            int count = Owner.SupplyResources.Count;
            PointI[] resources = new PointI[count];

            for (int i = 0; i < count; i++) {
                string id = Owner.SupplyResources[i];

                // skip nonexistent resources
                Variable resource = Resources[id];
                if (resource == null) continue;

                // skip non-positive initial values
                int initial = resource.InitialValue;
                if (initial <= 0) continue;

                // skip non-positive requirements
                int current = resource.Value;
                int supply = initial - current;
                if (supply <= 0) continue;

                resources[i] = new PointI(supply, (100 * supply) / initial);
            }

            return resources;
        }

        #endregion
        #region GetStepCost

        /// <summary>
        /// Returns the cost for moving the specified units from one specified map location to
        /// another neighboring map location.</summary>
        /// <param name="worldState">
        /// The <see cref="WorldState"/> on which to execute the <see cref="MoveCommand"/>.</param>
        /// <param name="units">
        /// An <see cref="IList{T}"/> containing all <see cref="Unit"/> objects that participate in
        /// the move. The first element equals this <b>Unit</b>. All elements reside on the same
        /// <see cref="Entity.Site"/>.</param>
        /// <param name="source">
        /// The coordinates of the <see cref="Site"/> where the move starts.</param>
        /// <param name="target">
        /// The coordinates of the <see cref="Site"/> where the move ends. This location must be a
        /// neighbor of <paramref name="source"/>.</param>
        /// <returns>
        /// The cost for moving the specified <paramref name="units"/> from <paramref
        /// name="source"/> to <paramref name="target"/>. This value is always positive.</returns>
        /// <remarks><para>
        /// <b>GetStepCost</b> returns the total <see cref="Site.Difficulty"/> of the <see
        /// cref="Site"/> at the specified <paramref name="target"/> coordinates, if positive;
        /// otherwise, the constant value one.
        /// </para><para>
        /// Derived classes may override <b>GetStepCost</b> to implement different semantics for 
        /// <see cref="MoveCommand"/>. Note that the specified <paramref name="source"/> site may be
        /// different from any current <see cref="Entity.Site"/> of the specified <paramref
        /// name="units"/>.
        /// </para><note type="caution">
        /// Do not change any reference parameters or their contents! Create a copy if you need to
        /// change them.</note></remarks>

        public virtual int GetStepCost(WorldState worldState,
            IList<Entity> units, PointI source, PointI target) {

            Debug.Assert(worldState != null);
            Debug.Assert(units != null);
            Debug.Assert(units.Count > 0);
            Debug.Assert(units[0] == this);

            Site targetSite = worldState.GetSite(target);
            return Math.Max(1, targetSite.Difficulty);
        }

        #endregion
        #region Move

        /// <summary>
        /// Executes the specified <see cref="MoveCommand"/>.</summary>
        /// <param name="command">
        /// The <see cref="MoveCommand"/> to execute.</param>
        /// <remarks><para>
        /// <b>Move</b> performs the following actions:
        /// </para><list type="number"><item>
        /// Set the <see cref="Entity.Site"/> property of all elements in the <see
        /// cref="Command.Entities"/> collection of the specified <paramref name="command"/> to a
        /// null reference.
        /// </item><item>
        /// Call <see cref="CreateMoveEvent"/> to add a map view event showing the movement
        /// described by the <paramref name="command"/>.
        /// </item><item>
        /// Assign the <see cref="Command.Target"/> site of the specified <paramref name="command"/>
        /// to the <see cref="Entity.Site"/> property of all elements in the command's <see
        /// cref="Command.Entities"/> collection.
        /// </item><item>
        /// Set the <see cref="CanMove"/> flag of all elements in the command's <b>Units</b>
        /// collection to <c>false</c>.
        /// </item></list><para>
        /// Derived classes may override <b>Move</b> to implement different semantics for <see
        /// cref="MoveCommand"/>.
        /// </para><para>
        /// The command's <b>Units</b> collection always contains at least one element, and the
        /// <b>Value</b> of the first element equals this <see cref="Unit"/>.</para></remarks>

        public virtual void Move(MoveCommand command) {
            Debug.Assert(command.Entities[0].Value == this);

            Site target = command.Target.Value;
            EntityList units = EntityReference.GetEntities(command.Entities);

            // remember original site
            Site source = units[0].Site;

            // remove all units from original site
            for (int i = 0; i < units.Count; i++)
                command.SetEntitySite(units[i].Id, Site.InvalidLocation);

            // create map view event for the movement
            CreateMoveEvent(command, units, source, target);

            // place all units and mark them as moved
            for (int i = 0; i < units.Count; i++) {
                Unit unit = (Unit) units[i];
                if (!unit.IsAlive) continue;

                command.SetEntitySite(unit.Id, target.Location);
                command.SetUnitCanMove(unit.Id, false);
            }
        }

        #endregion
        #region OnSiteChanged

        /// <summary>
        /// Executes when the <see cref="Entity.Site"/> property has changed.</summary>
        /// <param name="command">
        /// The <see cref="Command"/> being executed.</param>
        /// <param name="oldSite">
        /// The value of the <see cref="Entity.Site"/> property before it was changed.</param>
        /// <remarks><para>
        /// <b>OnSiteChanged</b> performs the following actions:
        /// </para><list type="number"><item>
        /// Call the base class implementation of <see cref="Entity.OnSiteChanged"/>.
        /// </item><item>
        /// Invoke <see cref="Faction.CaptureSite"/> on the current <see cref="Entity.Owner"/> to
        /// attempt capturing the new <see cref="Entity.Site"/> if its <see cref="Site.Owner"/> is
        /// different.</item></list></remarks>

        public override void OnSiteChanged(Command command, Site oldSite) {
            base.OnSiteChanged(command, oldSite);

            // attempt to capture new site
            if (Site != null && Site.Owner != Owner)
                Owner.CaptureSite(command, Site);
        }

        #endregion
        #region Place

        /// <summary>
        /// Executes the specified <see cref="PlaceCommand"/>.</summary>
        /// <param name="command">
        /// The <see cref="PlaceCommand"/> to execute.</param>
        /// <remarks>
        /// <b>Place</b> invokes the base class implementation of <see cref="Entity.Place"/> with
        /// the specified <paramref name="command"/>, and then sets the <see cref="CanAttack"/> and
        /// <see cref="CanMove"/> flags of all elements in the command's <see
        /// cref="Command.Entities"/> collection to <c>false</c>.</remarks>

        public override void Place(PlaceCommand command) {
            base.Place(command);

            // disable all newly placed units
            foreach (EntityReference unit in command.Entities) {
                Debug.Assert(unit.Value.Owner == command.Target.Value.Owner);

                command.SetUnitCanAttack(unit.Id, false);
                command.SetUnitCanMove(unit.Id, false);
            }
        }

        #endregion
        #region SelectAttacker

        /// <summary>
        /// Selects the next attacker to counter-attack during the specified <see
        /// cref="AttackCommand"/>.</summary>
        /// <param name="command">
        /// The <see cref="AttackCommand"/> being executed.</param>
        /// <returns><para>
        /// An element in the <see cref="Command.Entities"/> collection of the specified <paramref
        /// name="command"/>, indicating the attacker that the <see cref="Unit"/> should
        /// counter-attack.
        /// </para><para>-or-</para><para>
        /// A null reference if there are no attackers that the <b>Unit</b> can counter-attack.
        /// </para></returns>
        /// <remarks><para>
        /// <b>SelectAttacker</b> randomly returns an element in the <see cref="Command.Entities"/>
        /// collection of the specified <paramref name="command"/> whose <see cref="IsAlive"/>
        /// property is <c>true</c> and whose <see cref="Entity.Site"/> is in the collection 
        /// returned by <see cref="Finder.FindAttackTargets"/>. <b>SelectAttacker</b> returns a null
        /// reference if no such element exists, for whatever reason.
        /// </para><para>
        /// Derived classes may override <b>SelectAttacker</b> to implement different semantics for
        /// <see cref="AttackCommand"/>.
        /// </para><para>
        /// The <see cref="Site.Units"/> collection of the <see cref="Command.Target"/> of the
        /// specified <paramref name="command"/> always contains at least one element. One of its
        /// <b>Values</b> equals this <see cref="Unit"/>, although it cannot be predicted which one.
        /// </para></remarks>

        public virtual Unit SelectAttacker(AttackCommand command) {
            WorldState world = command.Context.WorldState;

            // get defender's potential targets
            IList<PointI> targets = Finder.FindAttackTargets(world, new List<Entity>() { this });

            if (targets.Count == 0) return null;

            // determine all surviving attackers in range
            EntityList inRange = null;
            foreach (EntityReference unit in command.Entities) {
                Unit attacker = (Unit) unit.Value;

                // perform lazy allocation and add attacker
                if (attacker.IsAlive && targets.Contains(attacker.Site.Location)) {
                    if (inRange == null)
                        inRange = new EntityList(command.Entities.Length);
                    inRange.Add(attacker);
                }
            }

            // select random attacker in range, if any
            if (inRange != null) {
                int index = MersenneTwister.Default.Next(inRange.Count - 1);
                return (Unit) inRange[index];
            }

            return null;
        }

        #endregion
        #region SelectDefender

        /// <summary>
        /// Selects the next defender to attack during the specified <see cref="AttackCommand"/>.
        /// </summary>
        /// <param name="command">
        /// The <see cref="AttackCommand"/> being executed.</param>
        /// <returns><para>
        /// An element in the <see cref="Site.Units"/> collection of the <see
        /// cref="Command.Target"/> site of the specified <paramref name="command"/>, indicating the
        /// defender that the <see cref="Unit"/> should attack.
        /// </para><para>-or-</para><para>
        /// A null reference if there are no defenders that the <b>Unit</b> can attack.
        /// </para></returns>
        /// <remarks><para>
        /// <b>SelectDefender</b> returns a random element in the <see cref="Site.Units"/>
        /// collection of the <see cref="Command.Target"/> site of the specified <paramref
        /// name="command"/>. <b>SelectDefender</b> returns a null reference if the <b>Units</b>
        /// collection is empty.
        /// </para><para>
        /// Derived classes may override <b>SelectDefender</b> to implement different semantics for
        /// <see cref="AttackCommand"/>.
        /// </para><para>
        /// The <see cref="Command.Entities"/> collection of the specified <paramref
        /// name="command"/> always contains at least one element. One of its <b>Values</b> equals
        /// this <see cref="Unit"/>, although it cannot be predicted which one.</para></remarks>

        public virtual Unit SelectDefender(AttackCommand command) {
            EntityList defenders = command.Target.Value.Units;

            // no surviving defenders left
            if (defenders.Count == 0) return null;

            // randomly select surviving defender
            int index = MersenneTwister.Default.Next(defenders.Count - 1);
            return (Unit) defenders[index];
        }

        #endregion
        #region SetAttackRange

        /// <summary>
        /// Sets the <see cref="AttackRange"/> property to the specified value.</summary>
        /// <param name="command">
        /// The <see cref="Command"/> being executed.</param>
        /// <param name="value">
        /// The new value for the <see cref="AttackRange"/> property.</param>
        /// <exception cref="InvalidCommandException">
        /// The <see cref="Scenario.UnitClass.AttackRangeAttribute"/> identifier of the underlying
        /// <see cref="UnitClass"/> is not a valid variable identifier.</exception>
        /// <remarks><para>
        /// <b>SetAttackRange</b> sets the <see cref="Entity.Attributes"/> element identified by
        /// <see cref="Scenario.UnitClass.AttackRangeAttribute"/> to the specified <paramref
        /// name="value"/>. This effectively changes the value of the <see cref="AttackRange"/>
        /// property which is backed by that <see cref="Entity.Attributes"/> element.
        /// </para><para>
        /// <b>SetAttackRange</b> sets the <see cref="Variable.InitialValue"/> of the backing <see
        /// cref="Variable"/> to the specified <paramref name="value"/>. This also recalculates its
        /// current <see cref="Variable.Value"/> using all applicable modifiers.</para></remarks>

        public void SetAttackRange(Command command, int value) {
            command.SetEntityVariableInitial(Id, UnitClass.AttackRangeAttribute, value);
        }

        #endregion
        #region SetMorale

        /// <summary>
        /// Sets the <see cref="Morale"/> property to the specified value.</summary>
        /// <param name="command">
        /// The <see cref="Command"/> being executed.</param>
        /// <param name="value">
        /// The new value for the <see cref="Morale"/> property.</param>
        /// <exception cref="InvalidCommandException">
        /// The <see cref="Scenario.UnitClass.MoraleResource"/> identifier of the underlying <see
        /// cref="UnitClass"/> is not a valid variable identifier.</exception>
        /// <remarks><para>
        /// <b>SetMorale</b> sets the <see cref="Entity.Resources"/> element identified by <see
        /// cref="Scenario.UnitClass.MoraleResource"/> to the specified <paramref name="value"/>.
        /// This effectively changes the value of the <see cref="Morale"/> property which is backed
        /// by that <see cref="Entity.Resources"/> element.</para></remarks>

        public void SetMorale(Command command, int value) {
            command.SetEntityVariable(Id, UnitClass.MoraleResource, value);
        }

        #endregion
        #region SetMovement

        /// <summary>
        /// Sets the <see cref="Movement"/> property to the specified value.</summary>
        /// <param name="command">
        /// The <see cref="Command"/> being executed.</param>
        /// <param name="value">
        /// The new value for the <see cref="Movement"/> property.</param>
        /// <exception cref="InvalidCommandException">
        /// The <see cref="Scenario.UnitClass.MovementAttribute"/> identifier of the underlying <see
        /// cref="UnitClass"/> is not a valid variable identifier.</exception>
        /// <remarks><para>
        /// <b>SetMovement</b> sets the <see cref="Entity.Attributes"/> element identified by <see
        /// cref="Scenario.UnitClass.MovementAttribute"/> to the specified <paramref name="value"/>.
        /// This effectively changes the value of the <see cref="Movement"/> property which is
        /// backed by that <see cref="Entity.Attributes"/> element.
        /// </para><para>
        /// <b>SetMovement</b> sets the <see cref="Variable.InitialValue"/> of the backing <see
        /// cref="Variable"/> to the specified <paramref name="value"/>. This also recalculates its
        /// current <see cref="Variable.Value"/> using all applicable modifiers.</para></remarks>

        public void SetMovement(Command command, int value) {
            command.SetEntityVariableInitial(Id, UnitClass.MovementAttribute, value);
        }

        #endregion
        #region SetStrength

        /// <summary>
        /// Sets the <see cref="Strength"/> property to the specified value.</summary>
        /// <param name="command">
        /// The <see cref="Command"/> being executed.</param>
        /// <param name="value">
        /// The new value for the <see cref="Strength"/> property.</param>
        /// <exception cref="InvalidCommandException">
        /// The <see cref="Scenario.UnitClass.StrengthResource"/> identifier of the underlying <see
        /// cref="UnitClass"/> is not a valid variable identifier.</exception>
        /// <remarks><para>
        /// <b>SetStrength</b> sets the <see cref="Entity.Resources"/> element identified by <see
        /// cref="Scenario.UnitClass.StrengthResource"/> to the specified <paramref name="value"/>.
        /// This effectively changes the value of the <see cref="Strength"/> property which is
        /// backed by that <see cref="Entity.Resources"/> element.</para></remarks>

        public  void SetStrength(Command command, int value) {
            command.SetEntityVariable(Id, UnitClass.StrengthResource, value);
        }

        #endregion
        #endregion
        #region ICloneable Members

        /// <summary>
        /// Creates a new <see cref="Unit"/> object that is a deep copy of the current instance.
        /// </summary>
        /// <returns>
        /// A new <see cref="Unit"/> object that is a deep copy of the current instance.</returns>
        /// <remarks>
        /// <b>Clone</b> calls the <see cref="Unit(Unit)"/> copy constructor with this <see
        /// cref="Unit"/> object.</remarks>

        public override object Clone() {
            return new Unit(this);
        }

        #endregion
    }
}
