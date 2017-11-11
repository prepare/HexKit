using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Tektosyne;
using Tektosyne.Collections;
using Tektosyne.Geometry;

using Hexkit.Scenario;
using Hexkit.World;
using Hexkit.World.Commands;

namespace Hexkit.Players {
    #region Type Aliases

    using InstanceCountDictionary = SortedListEx<String, Int32>;
    using InstanceCountPair = KeyValuePair<String, Int32>;

    #endregion

    /// <summary>
    /// Provides default implementations for general tasks performed by computer player algorithms.
    /// </summary>
    /// <remarks><para>
    /// <b>AlgorithmGeneral</b> provides default implementations for various tasks that most
    /// computer player algorithms are likely to perform, such as grouping units to achieve required
    /// minimum attack odds.
    /// </para><para>
    /// Some methods may be too simple for a strong computer player, but may still prove helpful
    /// when a customized algorithm is not yet available. All methods are intended to be called by
    /// an override of <see cref="Algorithm.FindBestCommands"/> in a derived class.</para></remarks>

    public abstract class AlgorithmGeneral: Algorithm {
        #region AttemptAttack

        /// <summary>
        /// Issues an <see cref="AttackCommand"/> with the specified units on the specified target
        /// if the specified odds are met.</summary>
        /// <param name="worldState">
        /// The <see cref="WorldState"/> on which to execute the <see cref="AttackCommand"/>.
        /// </param>
        /// <param name="attackers">
        /// An <see cref="IList{Entity}"/> containing the <see cref="Unit"/> objects that perform
        /// the attack.</param>
        /// <param name="target">
        /// The coordinates of the <see cref="Site"/> that is the target of the <see
        /// cref="AttackCommand"/>.</param>
        /// <param name="odds">
        /// A positive <see cref="Int32"/> value indicating the odds at which the attack will be
        /// executed. Specify zero or a negative value to attack unconditionally.</param>
        /// <param name="activeUnits">
        /// An <see cref="IList{Entity}"/> containing all elements in <paramref name="attackers"/>,
        /// and possibly other <see cref="Unit"/> objects whose <see cref="Unit.IsActive"/> flag is
        /// <c>true</c>.</param>
        /// <returns>
        /// <c>true</c> if an <see cref="AttackCommand"/> was executed successfully; otherwise,
        /// <c>false</c>.</returns>
        /// <remarks><para>
        /// <b>AttemptAttack</b> directly returns <c>false</c> if the specified <paramref
        /// name="attackers"/> collection is empty, or if the specified <paramref name="odds"/> are
        /// greater than zero and one of the following conditions holds:
        /// </para><list type="bullet"><item>
        /// The estimated percentage losses of the <paramref name="attackers"/> exceed those of the
        /// defenders in the specified <paramref name="target"/> divided by <paramref name="odds"/>.
        /// </item><item>
        /// The estimated percentage losses of the <paramref name="attackers"/> are greater than
        /// 10%, and the estimated percentage losses of the defenders are less than <paramref
        /// name="odds"/> times ten.
        /// </item></list><para>
        /// Otherwise, <b>AttemptAttack</b> calls <see cref="PerformAttack"/> with the specified
        /// arguments and returns the result.</para></remarks>

        protected bool AttemptAttack(WorldState worldState, IList<Entity> attackers,
            PointI target, int odds, IList<Entity> activeUnits) {

            Debug.Assert(worldState != null);
            Debug.Assert(attackers != null);
            Debug.Assert(Finder.MapGrid.Contains(target));
            Debug.Assert(activeUnits != null);

            // do nothing if no attackers specified
            if (attackers.Count == 0) return false;

            if (odds > 0) {
                // compute estimated combat losses
                Unit firstAttacker = (Unit) attackers[0];
                CombatResults results = firstAttacker.EstimateLosses(
                    worldState, attackers, target, true);

                int attackerPercent = results.AttackerPercent,
                    defenderPercent = results.DefenderPercent;

                // only attack with favorable odds
                if (attackerPercent > defenderPercent / odds ||
                    (attackerPercent > 10 && defenderPercent < odds * 10))
                    return false;
            }

            return PerformAttack(worldState, attackers, target, activeUnits);
        }

        #endregion
        #region AttemptGroupAttack

        /// <summary>
        /// Issues an <see cref="AttackCommand"/> with the specified leading unit and zero or more
        /// support units on the specified target if the specified odds are met.</summary>
        /// <param name="worldState">
        /// The <see cref="WorldState"/> on which to execute the <see cref="AttackCommand"/>.
        /// </param>
        /// <param name="unit">
        /// The <see cref="Unit"/> that leads the attack.</param>
        /// <param name="target">
        /// The coordinates of the <see cref="Site"/> that is the target of the <see
        /// cref="AttackCommand"/>.</param>
        /// <param name="odds">
        /// A positive <see cref="Int32"/> value indicating the odds at which the attack will be
        /// executed. Specify zero or a negative value to attack unconditionally.</param>
        /// <param name="activeUnits">
        /// An <see cref="IList{Entity}"/> containing the specified <paramref name="unit"/>, and
        /// possibly other <see cref="Unit"/> objects whose <see cref="Unit.IsActive"/> flag is
        /// <c>true</c>.</param>
        /// <returns>
        /// <c>true</c> if an <see cref="AttackCommand"/> was executed successfully; otherwise,
        /// <c>false</c>.</returns>
        /// <remarks><para>
        /// <b>AttemptGroupAttack</b> first calls <see cref="AttemptAttack"/> with the specified
        /// <paramref name="unit"/> only. If that fails, <b>AttemptGroupAttack</b> then adds
        /// elements from the specified <paramref name="activeUnits"/> collection to improve the
        /// odds.
        /// </para><para>
        /// <b>AttemptGroupAttack</b> returns <c>true</c> if and when <b>AttemptAttack</b> succeeds,
        /// and <c>false</c> if the <paramref name="activeUnits"/> collection was exhausted.
        /// </para><para>
        /// If the specified <paramref name="odds"/> are zero or negative, <b>AttemptGroupAttack</b>
        /// calls <see cref="PerformGroupAttack"/> with the specified arguments and returns the
        /// result.</para></remarks>

        protected bool AttemptGroupAttack(WorldState worldState, Unit unit,
            PointI target, int odds, IList<Entity> activeUnits) {

            Debug.Assert(worldState != null);
            Debug.Assert(unit != null);
            Debug.Assert(Finder.MapGrid.Contains(target));
            Debug.Assert(activeUnits != null);

            Debug.Assert(unit.IsAlive);
            Debug.Assert(unit.CanAttack);
            Debug.Assert(activeUnits.Contains(unit));

            // attack unconditionally if desired
            if (odds <= 0)
                return PerformGroupAttack(worldState, unit, target, activeUnits);

            // create collection containing all attacking units
            List<Entity> attackers = new List<Entity>();
            attackers.Add(unit);

            // first attempt: attack with specified unit only
            if (AttemptAttack(worldState, attackers, target, odds, activeUnits))
                return true;

            // failed, try adding support units
            for (int i = 0; i < activeUnits.Count; i++) {
                Unit support = (Unit) activeUnits[i];
                Debug.Assert(support.IsActive);

                // skip original and pacifist units
                if (unit == support || !support.CanAttack)
                    continue;

                // see if support unit can join in the attack
                int index = attackers.Count;
                attackers.Add(support);
                Debug.Assert(attackers.IndexOf(support) == index);

                IList<PointI> targets = Finder.FindAttackTargets(worldState, attackers);
                if (!targets.Contains(target)) {
                    attackers.RemoveAt(index);
                    continue;
                }

                // attempt attack with added support unit
                if (AttemptAttack(worldState, attackers, target, odds, activeUnits))
                    return true;
            }

            return false;
        }

        #endregion
        #region AttemptPlace

        /// <summary>
        /// Issues a <see cref="PlaceCommand"/> with the specified maximum number of the specified
        /// entities and the specified target.</summary>
        /// <param name="worldState">
        /// The <see cref="WorldState"/> on which to execute the <see cref="PlaceCommand"/>.</param>
        /// <param name="entities">
        /// An <see cref="IList{T}"/> containing the <see cref="Entity"/> objects to place.</param>
        /// <param name="target">
        /// The coordinates of the <see cref="Site"/> that is the target of the <see
        /// cref="PlaceCommand"/>.</param>
        /// <param name="count"><para>
        /// The maximum number of <paramref name="entities"/> to place.
        /// </para><para>-or-</para><para>
        /// A negative <see cref="Int32"/> value to indicate that all <paramref name="entities"/>
        /// should be placed.</para></param>
        /// <returns>
        /// The actual number of <paramref name="entities"/> that were placed.</returns>
        /// <remarks><para>
        /// <b>AttemptPlace</b> issues at most one <see cref="PlaceCommand"/> that places the first
        /// zero to <paramref name="count"/> elements in the specified <paramref name="entities"/>
        /// collection on the specified <paramref name="target"/> site.
        /// </para><para>
        /// The actual number of entities placed depends on the result of <see
        /// cref="Faction.CanPlace"/>. If this method returns <c>false</c> for a collection
        /// containing <paramref name="count"/> entities, <b>AttemptPlace</b> strips off one entity
        /// after another, starting at the highest index position, until <b>CanPlace</b> succeeds,
        /// and then issues a <see cref="PlaceCommand"/> with the remaining entities.
        /// </para><para>
        /// Finally, <b>AttemptPlace</b> removes all placed entities from the specified <paramref
        /// name="entities"/> collection, and returns the actual number of removed (i.e. placed)
        /// entities.</para></remarks>

        protected int AttemptPlace(WorldState worldState,
            List<Entity> entities, PointI target, int count) {

            Debug.Assert(worldState != null);
            Debug.Assert(entities != null);
            Debug.Assert(Finder.MapGrid.Contains(target));

            // default to entire collection
            if (count < 0 || count > entities.Count)
                count = entities.Count;

            // quit if no entities to place
            if (count == 0) return 0;

            Faction faction = entities[0].Owner;

            // check for trivial case first
            if (count == entities.Count) {

                // attempt to place all specified entities
                if (faction.CanPlace(worldState, entities, target)) {
                    Executor.ExecutePlace(worldState, entities, target);

                    // all entities were placed
                    entities.Clear();
                    return count;
                }

                // quit if nothing to remove
                if (--count == 0) return 0;
            }

            List<Entity> placeEntities = new List<Entity>(count);

            // prepare to place first "count" entities
            for (int i = 0; i < count; i++) {
                Entity entity = entities[0];

                Debug.Assert(entity != null);
                Debug.Assert(!entity.IsPlaced);

                // maintain entity order
                placeEntities.Add(entity);
                entities.RemoveAt(0);
            }

            // remove candidate entities until successful
            while (placeEntities.Count > 0) {

                // attempt placement with current collection
                if (faction.CanPlace(worldState, placeEntities, target)) {
                    Executor.ExecutePlace(worldState, placeEntities, target);
                    break;
                }

                // shift last entity back to original collection
                Entity entity = placeEntities[placeEntities.Count - 1];
                placeEntities.RemoveAt(placeEntities.Count - 1);
                entities.Insert(0, entity);
            }

            return placeEntities.Count;
        }

        #endregion
        #region BuildByValue

        /// <summary>
        /// Issues as many <see cref="BuildCommand"/> as desirable, according to contextual <see
        /// cref="UnitClass"/> valuations.</summary>
        /// <param name="worldState">
        /// The <see cref="WorldState"/> on which to execute any <see cref="BuildCommand"/>.</param>
        /// <remarks><para>
        /// <b>BuildByValue</b> issues a <see cref="BuildCommand"/> with some or all <see
        /// cref="Faction.BuildableUnits"/> of the <see cref="WorldState.ActiveFaction"/> of the
        /// specified <paramref name="worldState"/>.
        /// </para><para>
        /// The number of units built for each <b>BuildableUnits</b> element equals the maximum
        /// number returned by <see cref="WorldUtility.GetAllBuildCounts"/>, multiplied by the
        /// result of <see cref="Faction.Evaluate"/> for the same <see cref="UnitClass"/> and
        /// rounded up.
        /// </para><para>
        /// <b>BuildByValue</b> never builds classes without valid placement sites, as indicated by
        /// the result of <see cref="Finder.FindAllPlaceTargets"/>, or whose <b>Evaluate</b> result
        /// is exactly zero.</para></remarks>

        protected void BuildByValue(WorldState worldState) {
            Debug.Assert(worldState != null);

            // retrieve buildable classes for active faction
            Faction faction = worldState.ActiveFaction;
            IList<EntityClass> classes = faction.BuildableUnits.Values;

            // quit if nothing to build
            int classCount = classes.Count;
            if (classCount == 0) return;

            // determine place targets for all classes
            var placeTargets = Finder.FindAllPlaceTargets(worldState, faction, EntityCategory.Unit);

            // quit if nowhere to place
            if (placeTargets.Count == 0) return;

            // determine build counts for all classes
            int[] buildCounts = WorldUtility.GetAllBuildCounts(worldState, faction, classes);

            // prepare valuations for all classes
            double[] values = new double[classCount];
            bool anyValue = false;

            /*
             * The values array must not be normalized because that would
             * expend as many resources as possible, even when the faction
             * deliberately returns a low valuation for buildable classes
             * because it wants to save up resources for future turns.
             */

            for (int i = 0; i < classCount; i++) {
                // only evaluate buildable and placeable classes
                if (buildCounts[i] > 0 && placeTargets.ContainsKey(classes[i].Id)) {

                    values[i] = faction.Evaluate(worldState, classes[i]);
                    if (!anyValue && values[i] > 0.0) anyValue = true;
                }
            }

            // quit if all valuations are zero
            if (!anyValue) return;

            // prepare array for sorting by valuation
            var classBuildCounts = new InstanceCountPair[classCount];

            // store weighted build counts with class IDs
            for (int i = 0; i < classCount; i++) {
                string id = classes[i].Id;
                int count = 0;

                // round up for non-zero valuation
                if (values[i] > 0.0)
                    count = Fortran.Ceiling(buildCounts[i] * values[i]);

                classBuildCounts[i] = new InstanceCountPair(id, count);
            }

            // sort classes by increasing value
            Array.Sort(values, classBuildCounts);

            // traverse classes by decreasing value
            for (int i = classCount - 1;  i >= 0; i--) {
                string id = classBuildCounts[i].Key;
                int count = classBuildCounts[i].Value;

                // recheck actual limit after first command
                if (i < classCount - 1) {
                    int limit = faction.GetBuildCount(worldState, faction.BuildableUnits[id]);
                    if (count > limit) count = limit;
                }

                if (count > 0)
                    Executor.ExecuteBuild(worldState, id, count);
            }
        }

        #endregion
        #region BuildRandom

        /// <summary>
        /// Issues as many <see cref="BuildCommand"/> as possible, using random <see
        /// cref="UnitClass"/> objects and build counts.</summary>
        /// <param name="worldState">
        /// The <see cref="WorldState"/> on which to execute any <see cref="BuildCommand"/>.</param>
        /// <remarks><para>
        /// <b>BuildRandom</b> issues as many <see cref="BuildCommand"/> with <see
        /// cref="Faction.BuildableUnits"/> of the <see cref="WorldState.ActiveFaction"/> of the
        /// specified <paramref name="worldState"/> as possible.
        /// </para><para>
        /// The <b>BuildableUnits</b> elements to build are chosen randomly, and so are the build
        /// counts for each <see cref="UnitClass"/>.
        /// </para><para>
        /// <b>RandomBuild</b> never builds classes without valid placement sites, as indicated by
        /// the result of <see cref="Finder.FindAllPlaceTargets"/>.</para></remarks>

        protected void BuildRandom(WorldState worldState) {
            Debug.Assert(worldState != null);

            // retrieve buildable classes for active faction
            Faction faction = worldState.ActiveFaction;
            IList<EntityClass> classes = faction.BuildableUnits.Values;

            // quit if nothing to build
            if (classes.Count == 0) return;

            // determine place targets for all classes
            var placeTargets = Finder.FindAllPlaceTargets(worldState, faction, EntityCategory.Unit);

            // quit if nowhere to place
            if (placeTargets.Count == 0) return;

            // prepare build counts for all classes
            var buildCounts = new InstanceCountDictionary(classes.Count);

            while (true) {
                int count;
                buildCounts.Clear();

                // determine classes with nonzero build counts
                for (int i = 0; i < classes.Count; i++) {
                    EntityClass entityClass = classes[i];
                    count = faction.GetBuildCount(worldState, entityClass);

                    if (count > 0) {
                        // only build classes if we can place units
                        if (placeTargets.ContainsKey(entityClass.Id))
                            buildCounts.Add(entityClass.Id, count);
                    }
                }

                if (buildCounts.Count == 0) break;

                // select random class and build count
                int index = MersenneTwister.Default.Next(buildCounts.Count - 1);
                string id = buildCounts.GetKey(index);
                count = MersenneTwister.Default.Next(1, buildCounts.GetByIndex(index));

                Executor.ExecuteBuild(worldState, id, count);
            }
        }

        #endregion
        #region CombatComparisonAbsolute

        /// <summary>
        /// Compares two <see cref="CombatResults"/> instances with respect to their desirability
        /// for the attacking side, based on absolute losses.</summary>
        /// <param name="x">
        /// The first <see cref="CombatResults"/> instance to compare.</param>
        /// <param name="y">
        /// The second <see cref="CombatResults"/> instance to compare.</param>
        /// <returns><para>
        /// An <see cref="Int32"/> value indicating the relative order of <paramref name="x"/> and
        /// <paramref name="y"/>, as follows:
        /// </para><list type="table"><listheader>
        /// <term>Value</term><description>Condition</description>
        /// </listheader><item>
        /// <term>Less than zero</term>
        /// <description><paramref name="x"/> represents a less desirable outcome for the attacking
        /// side than <paramref name="y"/>.</description>
        /// </item><item>
        /// <term>Zero</term>
        /// <description><paramref name="x"/> and <paramref name="y"/> represent equally desirable
        /// outcomes for the attacking side.</description>
        /// </item><item>
        /// <term>Greater than zero</term>
        /// <description><paramref name="x"/> represents a more desirable outcome for the attacking
        /// side than <paramref name="y"/>.</description>
        /// </item></list></returns>
        /// <remarks><para>
        /// <b>CombatComparisonAbsolute</b> is compatible with the <see cref="Comparison{T}"/>
        /// delegate and can be passed to various sorting methods.
        /// </para><para>
        /// The desirability of a specific <see cref="CombatResults"/> instance is determined by
        /// subtracting its <see cref="CombatResults.AttackerLosses"/> value from its <see
        /// cref="CombatResults.DefenderLosses"/> value. The greater this difference, the more
        /// desirable the outcome for the attacking side.</para></remarks>

        protected static int CombatComparisonAbsolute(CombatResults x, CombatResults y) {

            int deltaX = x.DefenderLosses - x.AttackerLosses;
            int deltaY = y.DefenderLosses - y.AttackerLosses;

            return (deltaX - deltaY);
        }

        #endregion
        #region CombatComparisonRelative

        /// <summary>
        /// Compares two <see cref="CombatResults"/> instances with respect to their desirability
        /// for the attacking side, based on relative losses.</summary>
        /// <param name="x">
        /// The first <see cref="CombatResults"/> instance to compare.</param>
        /// <param name="y">
        /// The second <see cref="CombatResults"/> instance to compare.</param>
        /// <returns><para>
        /// An <see cref="Int32"/> value indicating the relative order of <paramref name="x"/> and
        /// <paramref name="y"/>, as follows:
        /// </para><list type="table"><listheader>
        /// <term>Value</term><description>Condition</description>
        /// </listheader><item>
        /// <term>Less than zero</term>
        /// <description><paramref name="x"/> represents a less desirable outcome for the attacking
        /// side than <paramref name="y"/>.</description>
        /// </item><item>
        /// <term>Zero</term>
        /// <description><paramref name="x"/> and <paramref name="y"/> represent equally desirable
        /// outcomes for the attacking side.</description>
        /// </item><item>
        /// <term>Greater than zero</term>
        /// <description><paramref name="x"/> represents a more desirable outcome for the attacking
        /// side than <paramref name="y"/>.</description>
        /// </item></list></returns>
        /// <remarks><para>
        /// <b>CombatComparisonRelative</b> is compatible with the <see cref="Comparison{T}"/>
        /// delegate and can be passed to various sorting methods.
        /// </para><para>
        /// The desirability of a specific <see cref="CombatResults"/> instance is determined by
        /// subtracting its <see cref="CombatResults.AttackerPercent"/> value from its <see
        /// cref="CombatResults.DefenderPercent"/> value. The greater this difference, the more
        /// desirable the outcome for the attacking side.</para></remarks>

        protected static int CombatComparisonRelative(CombatResults x, CombatResults y) {

            int deltaX = x.DefenderPercent - x.AttackerPercent;
            int deltaY = y.DefenderPercent - y.AttackerPercent;

            return (deltaX - deltaY);
        }

        #endregion
        #region SelectCombatComparison

        /// <summary>
        /// Selects the best <see cref="Comparison{CombatResults}"/> delegate for the current
        /// scenario and the specified <see cref="WorldState"/>.</summary>
        /// <param name="worldState">
        /// The <see cref="WorldState"/> on which any <see cref="AttackCommand"/> is are executed.
        /// </param>
        /// <returns>
        /// The <see cref="Comparison{CombatResults}"/> delegate best suited to the current scenario
        /// and the specified <paramref name="worldState"/>.</returns>
        /// <remarks><para>
        /// <b>SelectCombatComparison</b> returns a <see cref="Comparison{CombatResults}"/> delegate
        /// for use with the <see cref="SelectAttackTarget"/> methods. The method wrapped by this
        /// delegate is chosen as follows:
        /// </para><list type="bullet"><item>
        /// <see cref="CombatComparisonAbsolute"/> if <see cref="UnitClass.CanHeal"/> is
        /// <c>false</c> for all <see cref="EntitySection.Units"/> in the current scenario.
        /// </item><item>
        /// <see cref="CombatComparisonRelative"/> if <see cref="UnitClass.CanHeal"/> is <c>true</c>
        /// for all <see cref="EntitySection.Units"/> in the current scenario.
        /// </item><item>
        /// Otherwise, <see cref="CombatComparisonAbsolute"/> if <see cref="Unit.CanHeal"/> is
        /// <c>false</c> for a majority of all <see cref="Faction.Units"/> owned by any <see
        /// cref="Faction"/> in the specified <paramref name="worldState"/>.
        /// </item><item>
        /// Otherwise, <see cref="CombatComparisonRelative"/>.</item></list></remarks>

        protected static Comparison<CombatResults> SelectCombatComparison(WorldState worldState) {

            // count unit classes with healing ability
            int canHealCount = 0;
            var unitClasses = MasterSection.Instance.Entities.Units;
            foreach (UnitClass unitClass in unitClasses.Values)
                if (unitClass.CanHeal) ++canHealCount;

            // use absolute losses if no units can heal
            if (canHealCount == 0)
                return CombatComparisonAbsolute;

            // use relative losses if all units can heal
            if (canHealCount == unitClasses.Count)
                return CombatComparisonRelative;

            // count existing units with healing ability
            canHealCount = 0;
            int cannotHealCount = 0, unitCount = 0;
            foreach (Faction faction in worldState.Factions)
                foreach (Unit unit in faction.Units) {

                    // count total units and healing ability
                    ++unitCount;
                    if (unit.CanHeal) ++canHealCount;
                    else ++cannotHealCount;

                    // stop when a majority is established
                    if (canHealCount > unitCount / 2 ||
                        cannotHealCount > unitCount / 2)
                        goto finished;
                }

        finished:
            // select comparison based on majority ability
            if (canHealCount < cannotHealCount)
                return CombatComparisonAbsolute;
            else
                return CombatComparisonRelative;
        }

        #endregion
        #region EvaluateThreats

        /// <summary>
        /// Evaluates the threat posed by enemy units to the specified map locations.</summary>
        /// <param name="worldState">
        /// The <see cref="WorldState"/> whose <see cref="WorldState.ActiveFaction"/> performs the
        /// evaluation.</param>
        /// <param name="targets">
        /// An <see cref="IList{PointI}"/> containing the coordinates of all <see
        /// cref="WorldState.Sites"/> that are potential targets.</param>
        /// <returns>
        /// An <see cref="Array"/> containing one <see cref="Double"/> value for each element in
        /// <paramref name="targets"/>, indicating the threat assessment for the <paramref
        /// name="targets"/> element at the same index position.</returns>
        /// <remarks><para>
        /// <b>EvaluateThreats</b> calls <see cref="Faction.Evaluate"/> to compute the contextual
        /// valuation for each placed unit that is not owned by the <see
        /// cref="WorldState.ActiveFaction"/> in the specified <paramref name="worldState"/>.
        /// </para><para>
        /// For each element in <paramref name="targets"/>, that valuation is divided by the
        /// distance to the unit's location, plus one; and all resulting quotients are summed up to
        /// obtain the threat assessment.</para></remarks>

        protected static double[] EvaluateThreats(WorldState worldState, IList<PointI> targets) {

            Debug.Assert(worldState != null);
            Debug.Assert(targets != null);
            Debug.Assert(targets.Count > 0);

            double[] threats = new double[targets.Count];
            double[] distance = new double[targets.Count];

            // cache map grid reference
            PolygonGrid mapGrid = Finder.MapGrid;
            SizeI mapSize = mapGrid.Size;
            Faction faction = worldState.ActiveFaction;

            // traverse all map sites
            for (int x = 0; x < mapSize.Width; x++)
                for (int y = 0; y < mapSize.Height; y++) {
                    PointI location = new PointI(x, y);

                    // skip sites without enemy units
                    Site site = worldState.GetSite(location);
                    if (!site.HasAlienUnits(faction)) continue;

                    // compute current distance to all targets
                    for (int i = 0; i < targets.Count; i++)
                        distance[i] = 1.0 + mapGrid.GetDistance(location, targets[i]);

                    // add unit valuations divided by distance
                    IList<Entity> units = site.Units;
                    for (int i = 0; i < units.Count; i++) {
                        double value = faction.Evaluate(worldState, units[i]);

                        for (int j = 0; j < targets.Count; j++)
                            threats[j] += value / distance[j];
                    }
                }

            return threats;
        }

        #endregion
        #region PerformAttack

        /// <summary>
        /// Issues an <see cref="AttackCommand"/> with the specified units on the specified target.
        /// </summary>
        /// <param name="worldState">
        /// The <see cref="WorldState"/> on which to execute the <see cref="AttackCommand"/>.
        /// </param>
        /// <param name="attackers">
        /// An <see cref="IList{Entity}"/> containing the <see cref="Unit"/> objects that perform
        /// the attack.</param>
        /// <param name="target">
        /// The coordinates of the <see cref="Site"/> that is the target of the <see
        /// cref="AttackCommand"/>.</param>
        /// <param name="activeUnits">
        /// An <see cref="IList{Entity}"/> containing all elements in <paramref name="attackers"/>,
        /// and possibly other <see cref="Unit"/> objects whose <see cref="Unit.IsActive"/> flag is
        /// <c>true</c>.</param>
        /// <returns>
        /// <c>true</c> if the <see cref="AttackCommand"/> was executed successfully; otherwise,
        /// <c>false</c>.</returns>
        /// <remarks>
        /// <b>PerformAttack</b> issues an <see cref="AttackCommand"/> with the specified arguments,
        /// and then removes all elements in <paramref name="attackers"/> whose <see
        /// cref="Unit.IsActive"/> flag is now <c>false</c> from the specified <paramref
        /// name="activeUnits"/> collection.</remarks>

        protected bool PerformAttack(WorldState worldState,
            IList<Entity> attackers, PointI target, IList<Entity> activeUnits) {

            Debug.Assert(worldState != null);
            Debug.Assert(attackers != null);
            Debug.Assert(Finder.MapGrid.Contains(target));
            Debug.Assert(activeUnits != null);
#if DEBUG
            for (int i = 0; i < attackers.Count; i++) {
                Unit unit = (Unit) attackers[i];
                Debug.Assert(unit.IsAlive);
                Debug.Assert(unit.CanAttack);
                Debug.Assert(activeUnits.Contains(unit));
            }
#endif
            // do nothing if no attackers specified
            if (attackers.Count == 0) return false;

            if (!Executor.ExecuteAttack(worldState, attackers, target))
                return false;

            // remove inactive or dead units
            for (int i = 0; i < attackers.Count; i++) {
                Unit unit = (Unit) attackers[i];
                if (!unit.IsActive)
                    activeUnits.Remove(unit);
            }

            return true;
        }

        #endregion
        #region PerformGroupAttack

        /// <summary>
        /// Issues an <see cref="AttackCommand"/> with the specified leading unit and all available
        /// support units on the specified target.</summary>
        /// <param name="worldState">
        /// The <see cref="WorldState"/> on which to execute the <see cref="AttackCommand"/>.
        /// </param>
        /// <param name="unit">
        /// The <see cref="Unit"/> that leads the attack.</param>
        /// <param name="target">
        /// The coordinates of the <see cref="Site"/> that is the target of the <see
        /// cref="AttackCommand"/>.</param>
        /// <param name="activeUnits">
        /// An <see cref="IList{Entity}"/> containing the specified <paramref name="unit"/>, and
        /// possibly other <see cref="Unit"/> objects whose <see cref="Unit.IsActive"/> flag is
        /// <c>true</c>.</param>
        /// <returns>
        /// <c>true</c> if the <see cref="AttackCommand"/> was executed successfully; otherwise,
        /// <c>false</c>.</returns>
        /// <remarks>
        /// <b>PerformGroupAttack</b> combines the specified leading <paramref name="unit"/> with
        /// all elements from the specified <paramref name="activeUnits"/> collection that can
        /// participate in an attack on the specified <paramref name="target"/>, and then calls <see
        /// cref="PerformAttack"/> with the resulting collection.</remarks>

        protected bool PerformGroupAttack(WorldState worldState,
            Unit unit, PointI target, IList<Entity> activeUnits) {

            Debug.Assert(worldState != null);
            Debug.Assert(unit != null);
            Debug.Assert(Finder.MapGrid.Contains(target));
            Debug.Assert(activeUnits != null);

            Debug.Assert(unit.IsAlive);
            Debug.Assert(unit.CanAttack);
            Debug.Assert(activeUnits.Contains(unit));

            // create collection containing all attacking units
            List<Entity> attackers = new List<Entity>();
            attackers.Add(unit);

            // add all available support units
            for (int i = 0; i < activeUnits.Count; i++) {
                Unit support = (Unit) activeUnits[i];
                Debug.Assert(support.IsActive);

                // skip original and pacifist units
                if (unit == support || !support.CanAttack)
                    continue;

                // see if support unit can join in the attack
                int index = attackers.Count;
                attackers.Add(support);
                Debug.Assert(attackers.IndexOf(support) == index);

                IList<PointI> targets = Finder.FindAttackTargets(worldState, attackers);
                if (!targets.Contains(target))
                    attackers.RemoveAt(index);
            }

            // perform attack with added support unit
            return PerformAttack(worldState, attackers, target, activeUnits);
        }

        #endregion
        #region PlaceByThreat

        /// <summary>
        /// Issues as many <see cref="PlaceCommand"/> as possible, selecting placement sites by
        /// threat levels.</summary>
        /// <param name="worldState">
        /// The <see cref="WorldState"/> on which to execute any <see cref="PlaceCommand"/>.</param>
        /// <remarks><para>
        /// <b>PlaceByThreat</b> issues a <see cref="PlaceCommand"/> for all unplaced <see
        /// cref="Faction.Units"/> of the <see cref="WorldState.ActiveFaction"/> of the specified
        /// <paramref name="worldState"/> that have at least one valid placement site, as indicated
        /// by the result of <see cref="Finder.FindAllPlaceTargets"/>.
        /// </para><para>
        /// For all unplaced units of a given class, the number that is placed on a given placement
        /// site equals the share of the location's <see cref="EvaluateThreats"/> result out of the
        /// total threat sum, multiplied by the <see cref="Faction.Evaluate"/> result for the site
        /// and rounded up.</para></remarks>

        protected void PlaceByThreat(WorldState worldState) {
            Debug.Assert(worldState != null);

            // determine place targets for all unit classes
            Faction faction = worldState.ActiveFaction;
            var placeTargets = Finder.FindAllPlaceTargets(worldState, faction, EntityCategory.Unit);

            // quit if nowhere to place
            if (placeTargets.Count == 0) return;

            // get unplaced units and prepare placement buffer
            var units = faction.GetEntities(EntityCategory.Unit, false);
            var classUnits = new List<Entity>(units.Count);

            foreach (var pair in placeTargets) {
                IList<PointI> targets = pair.Value;
                Debug.Assert(targets.Count > 0);

                // get all units of this class
                classUnits.Clear();
                foreach (Entity unit in units)
                    if (unit.EntityClass.Id == pair.Key)
                        classUnits.Add(unit);

                // skip classes without unplaced units
                if (classUnits.Count == 0) continue;

                // place all units on single target
                if (targets.Count == 1) {
                    AttemptPlace(worldState, classUnits, targets[0], -1);
                    continue;
                }

                // compute normalized threats to all targets
                double[] threats = EvaluateThreats(worldState, targets);
                MathUtility.Normalize(threats);

                // scale threats by target valuation
                for (int j = 0; j < targets.Count; j++) {
                    Site site = worldState.GetSite(targets[j]);
                    double value = faction.Evaluate(worldState, site);
                    threats[j] *= value;
                }

                // renormalize scaled threat values
                MathUtility.Normalize(threats);

                // sort targets by increasing threat
                PointI[] targetArray = targets.ToArray();
                Array.Sort(threats, targetArray);

                // remember original unit count
                int classCount = classUnits.Count;

                // traverse targets by decreasing threat
                for (int j = targetArray.Length - 1; j >= 0; j--) {

                    // round up to place at least one unit
                    int count = Fortran.NInt(classCount * threats[j] + 0.5);
                    AttemptPlace(worldState, classUnits, targetArray[j], count);

                    // quit if all units placed
                    if (classUnits.Count == 0) break;
                }
            }
        }

        #endregion
        #region PlaceRandom

        /// <summary>
        /// Issues as many <see cref="PlaceCommand"/> as possible, using random placement sites.
        /// </summary>
        /// <param name="worldState">
        /// The <see cref="WorldState"/> on which to execute any <see cref="PlaceCommand"/>.</param>
        /// <remarks><para>
        /// <b>PlaceRandom</b> issues a <see cref="PlaceCommand"/> for all unplaced <see
        /// cref="Faction.Units"/> of the <see cref="WorldState.ActiveFaction"/> of the specified
        /// <paramref name="worldState"/> that have at least one valid placement site, as indicated
        /// by the result of <see cref="Finder.FindAllPlaceTargets"/>.
        /// </para><para>
        /// <b>PlaceRandom</b> repeatedly cycles through all unplaced units whose class defines more
        /// than one valid placement site, calling <see cref="AttemptPlace"/> with random unit
        /// counts and target sites until either no unplaced units of that class are left, or no
        /// <see cref="PlaceCommand"/> could be issued.</para></remarks>

        protected void PlaceRandom(WorldState worldState) {
            Debug.Assert(worldState != null);

            // determine place targets for all unit classes
            Faction faction = worldState.ActiveFaction;
            var placeTargets = Finder.FindAllPlaceTargets(worldState, faction, EntityCategory.Unit);

            // quit if nowhere to place
            if (placeTargets.Count == 0) return;

            // get unplaced units and prepare placement buffer
            var units = faction.GetEntities(EntityCategory.Unit, false);
            var classUnits = new List<Entity>(units.Count);

            foreach (var pair in placeTargets) {
                IList<PointI> targets = pair.Value;
                Debug.Assert(targets.Count > 0);

                // get all units of this class
                classUnits.Clear();
                foreach (Entity unit in units)
                    if (unit.EntityClass.Id == pair.Key)
                        classUnits.Add(unit);

                // skip classes without unplaced units
                if (classUnits.Count == 0) continue;

                // place all units on single target
                if (targets.Count == 1) {
                    AttemptPlace(worldState, classUnits, targets[0], -1);
                    continue;
                }

                int placed = 0;
                do {
                    // select random target and unit count
                    int index = MersenneTwister.Default.Next(targets.Count - 1);
                    int count = MersenneTwister.Default.Next(1, classUnits.Count);

                    placed = AttemptPlace(worldState, classUnits, targets[index], count);

                } while (placed > 0 && classUnits.Count > 0);
            }
        }

        #endregion
        #region SelectAttackTarget(...)

        /// <overloads>
        /// Selects the nearest or most attractive target for an <see cref="AttackCommand"/>
        /// performed by the specified units.</overloads>
        /// <summary>
        /// Selects the most attractive target within range for an <see cref="AttackCommand"/>
        /// performed by the specified units.</summary>
        /// <param name="worldState">
        /// The <see cref="WorldState"/> on which to execute the <see cref="AttackCommand"/>.
        /// </param>
        /// <param name="units">
        /// An <see cref="IList{Entity}"/> containing all <see cref="Unit"/> objects that
        /// participate in the attack. All elements reside on the same <see cref="Entity.Site"/>.
        /// </param>
        /// <param name="targets">
        /// An <see cref="IList{Site}"/> containing the <see cref="WorldState.Sites"/> that are the
        /// potential targets of the attack. This collection must be writable and may be reordered.
        /// </param>
        /// <param name="targetLimit">
        /// The maximum number of <paramref name="targets"/> to examine, starting with the target
        /// that is closest to the specified <paramref name="units"/>.</param>
        /// <param name="combatComparison">
        /// A <see cref="Comparison{CombatResults}"/> delegate that compares two <see
        /// cref="CombatResults"/> based on their desirability for the attacking side.</param>
        /// <returns>
        /// The most attractive element in <paramref name="targets"/>, as described below.</returns>
        /// <remarks><para>
        /// <b>SelectAttackTarget</b> returns an element in the specified <paramref name="targets"/>
        /// collection that is also present in the collection returned by <see
        /// cref="Finder.FindAttackTargets"/> for the specified <paramref name="units"/>.
        /// </para><para>
        /// When choosing between multiple <paramref name="targets"/> that are within attack range,
        /// <b>SelectAttackTarget</b> applies the following heuristics:
        /// </para><list type="number"><item>
        /// If one target contains one or more defenders whose <see cref="Unit.IsMobile"/> flag is
        /// <c>true</c> while the other does not, choose the target with the mobile defenders.
        /// </item><item>
        /// If the two targets contain defenders that are likely to generate different <see
        /// cref="CombatResults"/>, as determined by <see cref="Unit.EstimateLosses"/>, choose the
        /// target with the more favorable outcome, as determined by the specified <paramref
        /// name="combatComparison"/> delegate.
        /// </item><item>
        /// Call <see cref="SelectValuable"/> to choose between the two targets if they are equal in
        /// all other respects.
        /// </item></list><para>
        /// <b>SelectAttackTarget</b> returns a null reference only if no <paramref name="targets"/>
        /// element is within attack range.</para></remarks>

        protected static Site SelectAttackTarget(WorldState worldState,
            IList<Entity> units, IList<Site> targets, int targetLimit,
            Comparison<CombatResults> combatComparison) {

            Debug.Assert(worldState != null);
            Debug.Assert(units != null);
            Debug.Assert(units.Count > 0);
            Debug.Assert(targets != null);
            Debug.Assert(combatComparison != null);

            // cannot attack without targets
            if (targets.Count == 0)
                return null;

            Unit unit = (Unit) units[0];
            Debug.Assert(unit.IsPlaced);
#if DEBUG
            for (int i = 1; i < units.Count; i++)
                Debug.Assert(units[i].Site == unit.Site);
#endif
            Site target = null;
            bool targetMobile = false;
            CombatResults targetResults = new CombatResults();

            // maximum attack range for entire stack
            int attackRange = WorldUtility.MinimumAttackRange(units);

            // compute targets in attack range
            IList<PointI> inRange = Finder.FindAttackTargets(worldState, units);

            // sort targets by distance from unit stack
            Site.SortByDistance(unit.Site.Location, targets);

            // process targets only up to specified limit
            int targetCount = Math.Min(targets.Count, targetLimit);

            // look for targets within attack range
            for (int i = 0; i < targetCount; i++) {
                Site newTarget = targets[i];

                // stop when targets are too distant
                if (newTarget.Distance > attackRange)
                    break;

                // skip out-of-range targets
                if (!inRange.Contains(newTarget.Location))
                    continue;

                // check for mobile units
                bool newTargetMobile = (newTarget.CountMobileUnits() > 0);

                // estimate combat results
                CombatResults newTargetResults = unit.EstimateLosses(
                    worldState, units, newTarget.Location, true);

                // switch to first target
                if (target == null) goto switchTarget;

                // switch to mobile target
                if (newTargetMobile && !targetMobile)
                    goto switchTarget;

                // ignore immobile target
                if (!newTargetMobile && targetMobile)
                    continue;

                // compare combat results for both targets
                int compareResults = combatComparison(newTargetResults, targetResults);

                // switch to weaker target
                if (compareResults > 0)
                    goto switchTarget;

                // ignore stronger target
                if (compareResults < 0)
                    continue;

                // ignore less valuable target, all else being equal
                if (SelectValuable(worldState, target, newTarget) == target)
                    continue;

            switchTarget:
                // switch to new target
                target = newTarget;
                targetMobile = newTargetMobile;
                targetResults = newTargetResults;
            }

            return target;
        }

        #endregion
        #region SelectAttackTarget(..., IList<PointI>, Int32)

        /// <summary>
        /// Selects the nearest or most attractive target, at any range, for an <see
        /// cref="AttackCommand"/> performed by the specified units.</summary>
        /// <param name="worldState">
        /// The <see cref="WorldState"/> on which to execute the <see cref="AttackCommand"/>.
        /// </param>
        /// <param name="units">
        /// An <see cref="IList{Entity}"/> containing all <see cref="Unit"/> objects that
        /// participate in the attack. All elements reside on the same <see cref="Entity.Site"/>.
        /// </param>
        /// <param name="targets">
        /// An <see cref="IList{Site}"/> containing the <see cref="WorldState.Sites"/> that are the
        /// potential targets of the attack. This collection must be writable and may be reordered.
        /// </param>
        /// <param name="targetLimit">
        /// The maximum number of <paramref name="targets"/> to examine, starting with the target
        /// that is closest to the specified <paramref name="units"/>.</param>
        /// <param name="preferred">
        /// An <see cref="IList{Site}"/> containing the <see cref="WorldState.Sites"/> that are the
        /// preferred targets, all else being equal. This argument may be a null reference.</param>
        /// <param name="combatComparison">
        /// A <see cref="Comparison{CombatResults}"/> delegate that compares two <see
        /// cref="CombatResults"/> based on their desirability for the attacking side.</param>
        /// <param name="neighbors">
        /// An <see cref="IList{PointI}"/> containing the coordinates of all nearby <see
        /// cref="WorldState.Sites"/> whose path costs should be ignored. This argument may be a
        /// null reference, and then may be set to the result of <see
        /// cref="Finder.FindMoveTargets"/> for the specified <paramref name="units"/>, but only if
        /// needed.</param>
        /// <param name="pathCost">
        /// Returns the total cost of the movement path from the site of the first <paramref
        /// name="units"/> element to a site that is within attack range of the returned <paramref
        /// name="targets"/> element.</param>
        /// <returns>
        /// The nearest or most attractive element in <paramref name="targets"/>, as described
        /// below.</returns>
        /// <remarks><para>
        /// <b>SelectAttackTarget</b> first examines if any <paramref name="targets"/> elements are
        /// within attack range, as determined by <see cref="Unit.CanAttackTarget"/> – either of the
        /// current location of the specified <paramref name="units"/>, or of an element of the
        /// specified or calculated <paramref name="neighbors"/> collection.
        /// </para><para>
        /// If a match is found, the <paramref name="targets"/> element is returned and <paramref
        /// name="pathCost"/> is set to zero if the element is already in range, or to one if it is
        /// in range of a <paramref name="neighbors"/> element.
        /// </para><para>
        /// If no match is found, <b>SelectAttackTarget</b> returns the <paramref name="targets"/>
        /// element with the lowest path cost, and sets <paramref name="pathCost"/> to the actual
        /// path cost of the returned element. Movement paths are computed using the <see
        /// cref="Finder.AStar"/> algorithm for the current <see cref="Finder.MapGrid"/>.
        /// </para><para>
        /// When choosing between multiple <paramref name="targets"/> that are within range or that
        /// have identical path costs, <b>SelectAttackTarget</b> applies the following heuristics:
        /// </para><list type="number"><item>
        /// If <paramref name="preferred"/> is valid, and one target is an element of this
        /// collection while the other is not, choose the <paramref name="preferred"/> target.
        /// </item><item>
        /// If one target contains one or more defenders whose <see cref="Unit.IsMobile"/> flag is
        /// <c>true</c> while the other does not, choose the target with the mobile defenders.
        /// </item><item>
        /// If the two targets contain defenders that are likely to generate different <see
        /// cref="CombatResults"/>, as determined by <see cref="Unit.EstimateLosses"/>, choose the
        /// target with the more favorable outcome, as determined by the specified <paramref
        /// name="combatComparison"/> delegate.
        /// </item><item>
        /// If one target is already within attack range while the other target requires moving to a
        /// <paramref name="neighbors"/> element, choose the target that can be attacked without
        /// moving.
        /// </item><item>
        /// If both targets require moving to a <paramref name="neighbors"/> element, choose the
        /// target at the shorter distance, in map sites, if the distances are different.
        /// </item><item>
        /// Call <see cref="SelectValuable"/> to choose between the two targets if they are equal in
        /// all other respects.
        /// </item></list><para>
        /// If no nearby or reachable <paramref name="targets"/> element is found,
        /// <b>SelectAttackTarget</b> sets <paramref name="pathCost"/> to -1 and returns a null
        /// reference.</para></remarks>

        protected static Site SelectAttackTarget(WorldState worldState,
            IList<Entity> units, IList<Site> targets, int targetLimit,
            IList<Site> preferred, Comparison<CombatResults> combatComparison,
            ref IList<PointI> neighbors, ref int pathCost) {

            Debug.Assert(worldState != null);
            Debug.Assert(units != null);
            Debug.Assert(units.Count > 0);
            Debug.Assert(targets != null);
            Debug.Assert(combatComparison != null);

            // default to invalid value
            pathCost = -1;

            // cannot attack without targets
            if (targets.Count == 0)
                return null;

            Unit unit = (Unit) units[0];
            PointI source = unit.Site.Location;
#if DEBUG
            for (int i = 1; i < units.Count; i++)
                Debug.Assert(units[i].Site == unit.Site);
#endif
            /*
             * Calculate path costs and select the cheapest path.
             * Select by heuristics between identical path costs.
             *
             * Targets in range are set to a path cost of zero,
             * and nearby targets are set to a path cost of one.
             */

            Site target = null;
            RangeCategory targetRange = RangeCategory.Long;
            bool targetMobile = false;
            CombatResults targetResults = new CombatResults();

            // maximum attack range for entire stack
            int attackRange = WorldUtility.MinimumAttackRange(units);

            // maximum movement range for entire stack
            int movement = WorldUtility.MinimumMovement(units);

            // create proxy agent for A* algorithm
            UnitAgent agent = new UnitAgent(worldState, units);
            agent.Attacking = true;
            Debug.Assert(agent.OriginalSource == source);

            // sort targets by distance from source
            Site.SortByDistance(source, targets);

            // compute nearby sites if not supplied
            if (neighbors == null)
                neighbors = Finder.FindMoveTargets(worldState, units);

            // process targets only up to specified limit
            int targetCount = Math.Min(targets.Count, targetLimit);

            for (int i = 0; i < targetCount; i++) {
                Site newTarget = targets[i];

                if (target != null) {
                    if (targetRange == RangeCategory.Long) {
                        // stop if distance exceeds path cost
                        if (newTarget.Distance > pathCost)
                            break;
                    } else {
                        // stop if distance exceeds one-turn range
                        if (newTarget.Distance > movement + attackRange)
                            break;
                    }
                }

                // compute best path if possible
                bool result = Finder.AStar.FindBestPath(agent, source, newTarget);

                // ignore unreachable targets
                if (!result) continue;

                int newPathCost = (int) Finder.AStar.TotalCost;
                RangeCategory newTargetRange = RangeCategory.Long;

                // adjust category of new target
                if (newPathCost == 0) {
                    newTargetRange = RangeCategory.Short;
                }
                else if (newPathCost <= movement) {
                    /*
                     * We may not be able to reach the target site itself
                     * as it is likely occupied by enemy units, so we have
                     * to check the last location in the best path instead.
                     */

                    int last = Finder.AStar.Nodes.Count - 1;
                    if (neighbors.Contains(Finder.AStar.Nodes[last])) {
                        newPathCost = 1;
                        newTargetRange = RangeCategory.Medium;
                    }
                }

                // check for mobile units
                bool newTargetMobile = (newTarget.CountMobileUnits() > 0);

                // estimate combat losses
                CombatResults newTargetResults = unit.EstimateLosses(worldState,
                    units, newTarget, (newTargetRange == RangeCategory.Short));

                // switch to first target
                if (target == null) goto switchTarget;

                // compare path costs at long range
                if (targetRange == RangeCategory.Long) {

                    // switch to close target
                    if (newTargetRange != RangeCategory.Long)
                        goto switchTarget;

                    // switch to cheaper target
                    if (newPathCost < pathCost)
                        goto switchTarget;

                    // ignore more expensive target
                    if (newPathCost > pathCost)
                        continue;
                }
                else {
                    // ignore remote target
                    if (newTargetRange == RangeCategory.Long)
                        continue;
                }

                // examine preference if specified
                if (preferred != null) {

                    if (preferred.Contains(target)) {
                        // ignore non-preferred target
                        if (!preferred.Contains(newTarget))
                            continue;
                    } else {
                        // switch to preferred target
                        if (preferred.Contains(newTarget))
                            goto switchTarget;
                    }
                }

                // switch to mobile close target
                if (newTargetMobile && !targetMobile)
                    goto switchTarget;

                // ignore immobile close target
                if (!newTargetMobile && targetMobile)
                    continue;

                // compare combat results for both targets
                int compareResults = combatComparison(newTargetResults, targetResults);

                // switch to weaker close target
                if (compareResults > 0)
                    goto switchTarget;

                // ignore stronger close target
                if (compareResults < 0)
                    continue;

                if (targetRange == RangeCategory.Medium) {

                    // switch to shorter range at equal strength
                    if (newTargetRange == RangeCategory.Short)
                        goto switchTarget;

                    // switch to smaller distance
                    if (newTarget.Distance < target.Distance)
                        goto switchTarget;

                    // ignore greater distance
                    if (newTarget.Distance > target.Distance)
                        continue;
                }
                else if (targetRange == RangeCategory.Short) {

                    // ignore longer range at equal strength
                    if (newTargetRange == RangeCategory.Medium)
                        continue;
                }

                // ignore less valuable target, all else being equal
                if (SelectValuable(worldState, target, newTarget) == target)
                    continue;

            switchTarget:
                // switch to new target
                target = newTarget;
                pathCost = newPathCost;
                targetRange = newTargetRange;
                targetMobile = newTargetMobile;
                targetResults = newTargetResults;
            }
#if DEBUG
            if (target == null)
                Debug.Assert(pathCost < 0);
            else if (targetRange == RangeCategory.Short)
                Debug.Assert(pathCost == 0);
            else if (targetRange == RangeCategory.Medium)
                Debug.Assert(pathCost == 1);
            else {
                Debug.Assert(targetRange == RangeCategory.Long);
                Debug.Assert(pathCost > 0);
            }
#endif
            return target;
        }

        #endregion
        #region SelectMoveTarget

        /// <summary>
        /// Selects the nearest or most attractive element from a list of potential target sites for
        /// a <see cref="MoveCommand"/> performed by the specified units.</summary>
        /// <param name="worldState">
        /// The <see cref="WorldState"/> on which to execute the <see cref="MoveCommand"/>.</param>
        /// <param name="units">
        /// An <see cref="IList{Entity}"/> containing all <see cref="Unit"/> objects that
        /// participate in the move. All elements reside on the same <see cref="Entity.Site"/>.
        /// </param>
        /// <param name="targets">
        /// An <see cref="IList{Site}"/> containing the <see cref="WorldState.Sites"/> that are the
        /// potential targets of the move. This collection must be writable and may be reordered.
        /// </param>
        /// <param name="targetLimit">
        /// The maximum number of <paramref name="targets"/> to examine, starting with the target
        /// that is closest to the specified <paramref name="units"/>.</param>
        /// <param name="supplyIndex"><para>
        /// The non-negative index of the <see cref="Site.SupplyResources"/> element to maximize.
        /// </para><para>-or-</para><para>
        /// A negative value to ignore all <b>SupplyResources</b> arrays.</para></param>
        /// <param name="neighbors">
        /// An <see cref="IList{PointI}"/> containing the coordinates of all nearby <see
        /// cref="WorldState.Sites"/> whose path costs should be ignored. This argument may be a
        /// null reference, and then may be set to the result of <see
        /// cref="Finder.FindMoveTargets"/> for the specified <paramref name="units"/>, but only if
        /// needed.</param>
        /// <param name="pathCost">
        /// Returns the total cost of the movement path from the site of the first <paramref
        /// name="units"/> element to the returned <paramref name="targets"/> element.</param>
        /// <returns>
        /// The nearest or most attractive element in <paramref name="targets"/>, as described
        /// below.</returns>
        /// <remarks><para>
        /// <b>SelectMoveTarget</b> first examines if any <paramref name="targets"/> elements are
        /// within movement range; that is, either equal to the current location of the specified
        /// <paramref name="units"/>, or equal to an element of the specified or calculated
        /// <paramref name="neighbors"/> collection.
        /// </para><para>
        /// If a match is found, the <paramref name="targets"/> element is returned and <paramref
        /// name="pathCost"/> is set to zero if the element equals the current site, or to one if it
        /// equals a <paramref name="neighbors"/> element.
        /// </para><para>
        /// If no match is found, <b>SelectMoveTarget</b> returns the <paramref name="targets"/>
        /// element with the lowest path cost, and sets <paramref name="pathCost"/> to the actual
        /// path cost of the returned element. Movement paths are computed using the <see
        /// cref="Finder.AStar"/> algorithm for the current <see cref="Finder.MapGrid"/>.
        /// </para><para>
        /// If the specified <paramref name="supplyIndex"/> is non-negative, <b>SelectMoveTarget</b>
        /// ignores all <paramref name="targets"/> whose <see cref="Site.SupplyResources"/> value at
        /// the specified index position is equal to or less than zero.
        /// </para><para>
        /// When choosing between multiple <paramref name="targets"/> that are within range or that
        /// have identical path costs, <b>SelectMoveTarget</b> applies the following heuristics:
        /// </para><list type="number"><item>
        /// If <paramref name="supplyIndex"/> is non-negative, and the two targets have different
        /// <b>SupplyResources</b> values at the specified index position, choose the one with the
        /// greater value.
        /// </item><item>
        /// If one target is the current site while the other target is a <paramref
        /// name="neighbors"/> element, choose the target that equals the current site.
        /// </item><item>
        /// If both targets require moving to a <paramref name="neighbors"/> element, choose the
        /// target at the shorter distance, in map sites, if the distances are different.
        /// </item><item>
        /// Call <see cref="SelectValuable"/> to choose between the two targets if they are equal in
        /// all other respects.
        /// </item></list><para>
        /// If no nearby or reachable <paramref name="targets"/> element is found,
        /// <b>SelectMoveTarget</b> sets <paramref name="pathCost"/> to -1 and returns a null
        /// reference.</para></remarks>

        protected static Site SelectMoveTarget(WorldState worldState,
            IList<Entity> units, IList<Site> targets, int targetLimit,
            int supplyIndex, ref IList<PointI> neighbors, ref int pathCost) {

            Debug.Assert(worldState != null);
            Debug.Assert(units != null);
            Debug.Assert(units.Count > 0);
            Debug.Assert(targets != null);

            // default to invalid value
            pathCost = -1;

            // cannot move without targets
            if (targets.Count == 0)
                return null;

            Unit unit = (Unit) units[0];
            PointI source = unit.Site.Location;
#if DEBUG
            for (int i = 1; i < units.Count; i++)
                Debug.Assert(units[i].Site == unit.Site);
#endif
            // sort targets by distance from source
            Site.SortByDistance(source, targets);

            // check if current site is valid target
            if (targets[0].Distance == 0 && supplyIndex < 0) {
                pathCost = 0;
                return targets[0];
            }

            /*
             * Calculate path costs and select the cheapest path.
             * Select by heuristics between identical path costs.
             *
             * The source site is set to a path cost of zero,
             * and nearby targets are set to a path cost of one.
             */

            Site target = null;
            RangeCategory targetRange = RangeCategory.Long;
            int targetSupply = 0;

            // maximum movement range for entire stack
            int movement = WorldUtility.MinimumMovement(units);

            // create proxy agent for A* algorithm
            UnitAgent agent = new UnitAgent(worldState, units);
            agent.Attacking = false;
            Debug.Assert(agent.OriginalSource == source);

            // compute nearby sites if not supplied
            if (neighbors == null)
                neighbors = Finder.FindMoveTargets(worldState, units);

            // process targets only up to specified limit
            int targetCount = Math.Min(targets.Count, targetLimit);

            for (int i = 0; i < targetCount; i++) {
                Site newTarget = targets[i];
                int newTargetSupply = 0;

                if (supplyIndex >= 0) {

                    // ignore target without any supplies
                    if (newTarget.SupplyResources == null)
                        continue;

                    // ignore target without desired supplies
                    newTargetSupply = newTarget.SupplyResources[supplyIndex];
                    if (newTargetSupply <= 0)
                        continue;
                }

                if (target != null) {
                    if (targetRange == RangeCategory.Long) {
                        // stop if distance exceeds path cost
                        if (newTarget.Distance > pathCost)
                            break;
                    } else {
                        // stop if distance exceeds one-turn range
                        if (newTarget.Distance > movement)
                            break;
                    }
                }

                RangeCategory newTargetRange = RangeCategory.Short;
                int newPathCost = 0;

                // adjust category of new target
                if (newTarget.Location != source) {

                    if (neighbors.Contains(newTarget.Location)) {
                        newPathCost = 1;
                        newTargetRange = RangeCategory.Medium;
                    }
                    else {
                        // compute best path if possible
                        bool result = Finder.AStar.FindBestPath(agent, source, newTarget);

                        // ignore unreachable targets
                        if (!result) continue;

                        newPathCost = (int) Finder.AStar.TotalCost;
                        newTargetRange = RangeCategory.Long;
                    }
                }

                // switch to first target
                if (target == null) goto switchTarget;

                // compare path costs at long range
                if (targetRange == RangeCategory.Long) {

                    // switch to close target
                    if (newTargetRange != RangeCategory.Long)
                        goto switchTarget;

                    // switch to cheaper target
                    if (newPathCost < pathCost)
                        goto switchTarget;

                    // ignore more expensive target
                    if (newPathCost > pathCost)
                        continue;
                }
                else {
                    // ignore remote target
                    if (newTargetRange == RangeCategory.Long)
                        continue;
                }

                if (supplyIndex >= 0) {

                    // switch to higher supply rating
                    if (newTargetSupply > targetSupply)
                        goto switchTarget;

                    // ignore lower supply rating
                    if (newTargetSupply < targetSupply)
                        continue;
                }

                if (targetRange == RangeCategory.Medium) {

                    // switch to shorter range at equal supply
                    if (newTargetRange == RangeCategory.Short)
                        goto switchTarget;

                    // switch to smaller distance
                    if (newTarget.Distance < target.Distance)
                        goto switchTarget;

                    // ignore greater distance
                    if (newTarget.Distance > target.Distance)
                        continue;
                }
                else if (targetRange == RangeCategory.Short) {

                    // ignore longer range at equal supply
                    if (newTargetRange == RangeCategory.Medium)
                        continue;
                }

                // ignore less valuable target, all else being equal
                if (SelectValuable(worldState, target, newTarget) == target)
                    continue;

            switchTarget:
                // switch to new target
                target = newTarget;
                pathCost = newPathCost;
                targetRange = newTargetRange;
                targetSupply = newTargetSupply;
            }
#if DEBUG
            if (target == null)
                Debug.Assert(pathCost < 0);
            else if (targetRange == RangeCategory.Short)
                Debug.Assert(pathCost == 0);
            else if (targetRange == RangeCategory.Medium)
                Debug.Assert(pathCost == 1);
            else {
                Debug.Assert(targetRange == RangeCategory.Long);
                Debug.Assert(pathCost > 0);
            }
#endif
            return target;
        }

        #endregion
        #region SelectValuable

        /// <summary>
        /// Selects the more desirable of two specified <see cref="IValuable"/> instances.</summary>
        /// <param name="worldState">
        /// The <see cref="WorldState"/> whose <see cref="WorldState.ActiveFaction"/> performs the
        /// evaluation.</param>
        /// <param name="first">
        /// The first <see cref="IValuable"/> instance to evaluate.</param>
        /// <param name="second">
        /// The second <see cref="IValuable"/> instance to evaluate.</param>
        /// <returns>
        /// An <see cref="IValuable"/> instance that equals either <paramref name="first"/> or
        /// <paramref name="second"/>.</returns>
        /// <remarks><para>
        /// <b>SelectValuable</b> selects one of the two specified <see cref="IValuable"/> instances
        /// in three steps:
        /// </para><list type="number"><item>
        /// Return the <b>IValuable</b> instance with the higher contextual valuation if they
        /// differ, as determined by calling <see cref="Faction.Evaluate"/> on the <see
        /// cref="WorldState.ActiveFaction"/> of the specified <paramref name="worldState"/>.
        /// </item><item>
        /// Return the <b>IValuable</b> instance with the higher context-free <see
        /// cref="IValuable.Valuation"/> if they differ.
        /// </item><item>
        /// Randomly return either instance with an equal chance.</item></list></remarks>

        protected static IValuable SelectValuable(
            WorldState worldState, IValuable first, IValuable second) {

            Debug.Assert(worldState != null);
            Debug.Assert(first != null);
            Debug.Assert(second != null);

            // calculate contextual valuations
            Faction faction = worldState.ActiveFaction;
            double firstValue = faction.Evaluate(worldState, first);
            double secondValue = faction.Evaluate(worldState, second);

            // return higher-valued instance
            if (firstValue > secondValue) return first;
            if (firstValue < secondValue) return second;

            // calculate context-free valuations
            firstValue = first.Valuation;
            secondValue = second.Valuation;

            // return higher-valued instance
            if (firstValue > secondValue) return first;
            if (firstValue < secondValue) return second;

            // choose randomly among equally valued instances
            return (MersenneTwister.Default.Next(1) == 0 ? first : second);
        }

        #endregion
    }
}
