using System;
using System.Collections.Generic;
using System.Diagnostics;

using Hexkit.Scenario;
using Hexkit.World.Commands;

using Tektosyne.Collections;
using Tektosyne.Geometry;
using Tektosyne.Graph;

namespace Hexkit.World {

    /// <summary>
    /// Provides proxies and helper methods for pathfinding algorithms.</summary>
    /// <remarks><para>
    /// <b>Finder</b> contains proxies for all pathfinding-related properties of the current <see
    /// cref="AreaSection"/> instance, and numerous helper methods for pathfinding and other tasks
    /// that process map locations.
    /// </para><para>
    /// All <b>Finder</b> members require a valid scenario <see cref="MasterSection.Instance"/>. 
    /// Release builds perform no argument checking for performance reasons. Debug builds are
    /// guarded by assertions, however.</para></remarks>

    public static class Finder {
        #region AStar

        /// <summary>
        /// Gets an A* pathfinding algorithm for the <see cref="MapGrid"/>.</summary>
        /// <value>
        /// The value of the <see cref="AreaSection.AStar"/> property associated with the current
        /// scenario <see cref="MasterSection.Instance"/>.</value>

        public static AStar<PointI> AStar {
            [DebuggerStepThrough]
            get { return MasterSection.Instance.Areas.AStar; }
        }

        #endregion
        #region Coverage

        /// <summary>
        /// Gets a path coverage algorithm for the <see cref="MapGrid"/>.</summary>
        /// <value>
        /// The value of the <see cref="AreaSection.Coverage"/> property associated with the current
        /// scenario <see cref="MasterSection.Instance"/>.</value>

        public static Coverage<PointI> Coverage {
            [DebuggerStepThrough]
            get { return MasterSection.Instance.Areas.Coverage; }
        }

        #endregion
        #region FloodFill

        /// <summary>
        /// Gets a flood fill algorithm for the <see cref="MapGrid"/>.</summary>
        /// <value>
        /// The value of the <see cref="AreaSection.FloodFill"/> property associated with the
        /// current scenario <see cref="MasterSection.Instance"/>.</value>

        public static FloodFill<PointI> FloodFill {
            [DebuggerStepThrough]
            get { return MasterSection.Instance.Areas.FloodFill; }
        }

        #endregion
        #region MapGrid

        /// <summary>
        /// Gets the <see cref="PolygonGrid"/> that describes the game map.</summary>
        /// <value>
        /// The value of the <see cref="AreaSection.MapGrid"/> property associated with the current
        /// scenario <see cref="MasterSection.Instance"/>.</value>

        public static PolygonGrid MapGrid {
            [DebuggerStepThrough]
            get { return MasterSection.Instance.Areas.MapGrid; }
        }

        #endregion
        #region Visibility

        /// <summary>
        /// Gets a line-of-sight algorithm for the <see cref="MapGrid"/>.</summary>
        /// <value>
        /// The value of the <see cref="AreaSection.Visibility"/> property associated with the
        /// current scenario <see cref="MasterSection.Instance"/>.</value>

        public static Visibility<PointI> Visibility {
            [DebuggerStepThrough]
            get { return MasterSection.Instance.Areas.Visibility; }
        }

        #endregion
        #region AreUnitsInAttackRange(..., PointI)

        /// <overloads>
        /// Determines whether the specified units are within attack range of the specified target
        /// location.</overloads>
        /// <summary>
        /// Determines whether the specified units are within attack range of the specified target
        /// location, given their current locations.</summary>
        /// <param name="worldState">
        /// The <see cref="WorldState"/> on which to execute the <see cref="AttackCommand"/>.
        /// </param>
        /// <param name="units">
        /// An <see cref="IList{T}"/> containing all <see cref="Unit"/> objects that participate in
        /// the attack.</param>
        /// <param name="target">
        /// The coordinates of the <see cref="Site"/> that is the target of the attack.</param>
        /// <returns>
        /// <c>true</c> if <paramref name="target"/> is within attack range of all specified
        /// <paramref name="units"/>, given their current locations; otherwise, <c>false</c>.
        /// </returns>
        /// <remarks><para>
        /// <b>AreUnitsInAttackRange</b> returns <c>true</c> exactly if the distance, in map sites,
        /// between the current <see cref="Entity.Site"/> of each <paramref name="units"/> element
        /// and the specified <paramref name="target"/> does not exceed the unit's <see
        /// cref="Unit.AttackRange"/>.
        /// </para><para>
        /// For any <paramref name="units"/> element whose distance to the specified <paramref
        /// name="target"/> is greater than one, and whose <see cref="Unit.RangedAttack"/> mode is
        /// <see cref="TargetMode.Line"/>, <b>AreUnitsInAttackRange</b> also runs the <see
        /// cref="Visibility"/> algorithm and returns <c>true</c> only if a clear line of sight
        /// exists between all such <paramref name="units"/> and the the specified <paramref
        /// name="target"/>.</para></remarks>

        public static bool AreUnitsInAttackRange(
            WorldState worldState, IList<Entity> units, PointI target) {

            Debug.Assert(worldState != null);
            Debug.Assert(units != null);
            Debug.Assert(units.Count > 0);
            Debug.Assert(MapGrid.Contains(target));

            // cache map grid reference
            PolygonGrid mapGrid = Finder.MapGrid;

            // target must be in range of all units
            for (int i = 0; i < units.Count; i++) {
                Unit unit = (Unit) units[i];
                PointI source = unit.Site.Location;

                int distance = mapGrid.GetStepDistance(source, target);
                if (distance > unit.AttackRange)
                    return false;

                // some units require clear line of sight to target
                if (distance > 1 && unit.RangedAttack == TargetMode.Line) {
                    Predicate<PointI> isOpaque = (p => worldState.GetSite(p).BlocksAttack);
                    double worldDistance = 2 * distance * MapGrid.Element.OuterRadius;

                    Visibility.FindVisible(isOpaque, source, worldDistance);
                    if (!Visibility.Nodes.Contains(target))
                        return false;
                }
            }

            return true;
        }

        #endregion
        #region AreUnitsInAttackRange(..., PointI, PointI, Int32)

        /// <summary>
        /// Determines whether the specified units would be within attack range of the specified
        /// target location, given the specified source location.</summary>
        /// <param name="worldState">
        /// The <see cref="WorldState"/> on which to execute the <see cref="AttackCommand"/>.
        /// </param>
        /// <param name="units">
        /// An <see cref="IList{T}"/> containing all <see cref="Unit"/> objects that participate in
        /// the attack.</param>
        /// <param name="source">
        /// The coordinates of the <see cref="Site"/> from which the specified <paramref
        /// name="units"/> would attack.</param>
        /// <param name="target">
        /// The coordinates of the <see cref="Site"/> that is the target of the attack.</param>
        /// <param name="distance">
        /// The distance, in map sites, between <paramref name="source"/> and <paramref
        /// name="target"/>. This argument may be negative if the distance is unknown.</param>
        /// <returns>
        /// <c>true</c> if <paramref name="target"/> would be within attack range of all specified
        /// <paramref name="units"/>, assuming they were all placed on <paramref name="source"/>;
        /// otherwise, <c>false</c>.</returns>
        /// <remarks><para>
        /// <b>AreUnitsInAttackRange</b> returns <c>true</c> exactly if the specified <paramref
        /// name="distance"/> (which is calculated if negative) does not exceed the <see
        /// cref="Unit.AttackRange"/> value of any <paramref name="units"/> element.
        /// </para><para>
        /// If the specified or calculated <paramref name="distance"/> is greater than one, and the
        /// <see cref="Unit.RangedAttack"/> mode of any <paramref name="units"/> element is <see
        /// cref="TargetMode.Line"/>, <b>AreUnitsInAttackRange</b> also runs the <see
        /// cref="Visibility"/> algorithm and returns <c>true</c> only if a clear line of sight
        /// exists between the specified <paramref name="source"/> and <paramref name="target"/>
        /// locations.</para></remarks>

        public static bool AreUnitsInAttackRange(WorldState worldState,
            IList<Entity> units, PointI source, PointI target, int distance) {

            Debug.Assert(worldState != null);
            Debug.Assert(units != null);
            Debug.Assert(units.Count > 0);
            Debug.Assert(MapGrid.Contains(source));
            Debug.Assert(MapGrid.Contains(target));

            // compute distance if not specified
            if (distance < 0)
                distance = MapGrid.GetStepDistance(source, target);

            // target must be in range of all units
            if (distance > WorldUtility.MinimumAttackRange(units))
                return false;

            if (distance > 1) {
                bool checkVisibility = false;

                // check if any unit requires clear line of sight
                for (int i = 0; i < units.Count; i++) {
                    Unit unit = (Unit) units[i];
                    if (unit.RangedAttack == TargetMode.Line) {
                        checkVisibility = true;
                        break;
                    }
                }

                // some units require clear line of sight to target
                if (checkVisibility) {
                    Predicate<PointI> isOpaque = (p => worldState.GetSite(p).BlocksAttack);
                    double worldDistance = 2 * distance * MapGrid.Element.OuterRadius;

                    Visibility.FindVisible(isOpaque, source, worldDistance);
                    if (!Visibility.Nodes.Contains(target))
                        return false;
                }
            }

            return true;
        }

        #endregion
        #region FindAllPlaceTargets(...)

        /// <overloads>
        /// Finds all valid target locations for any possible <see cref="PlaceCommand"/>.
        /// </overloads>
        /// <summary>
        /// Finds all valid target locations for any <see cref="PlaceCommand"/> with entities of any
        /// available class of the specified category.</summary>
        /// <param name="worldState">
        /// The <see cref="WorldState"/> on which to execute any <see cref="PlaceCommand"/>.</param>
        /// <param name="faction">
        /// The <see cref="Faction"/> that issues the <see cref="PlaceCommand"/>.</param>
        /// <param name="category">
        /// An <see cref="EntityCategory"/> value indicating which entity classes to process.
        /// </param>
        /// <returns>
        /// A dictionary that maps the <see cref="EntityClass.Id"/> string of each <see
        /// cref="EntityClass"/> of the specified <paramref name="category"/> available to the
        /// specified <paramref name="faction"/> to a <see cref="List{PointI}"/> containing the
        /// coordinates of all valid placement sites for that <b>EntityClass</b>.</returns>
        /// <remarks><para>
        /// <b>FindAllPlaceTargets</b> calls <see cref="Faction.GetAvailableClasses"/> to determine
        /// the entity classes of the specified <paramref name="category"/> that are available to
        /// the specified <paramref name="faction"/>, and then calls <see
        /// cref="Faction.GetPlaceTargets"/> to determine the valid placement sites for all returned
        /// <see cref="EntityClass"/> objects. Entity classes without valid placement sites are not
        /// added to the returned collection.
        /// </para><para>
        /// <b>FindAllPlaceTargets</b> never returns a null reference, but it returns an empty
        /// collection if no entity classes of the specified <paramref name="category"/> are
        /// available to the specified <paramref name="faction"/>, or if none of its available 
        /// entity classes have any valid placement sites.
        /// </para><para>
        /// The keys and values of the returned dictionary are never null references, and the <see
        /// cref="List{PointI}"/> values are never empty collections.</para></remarks>

        public static SortedList<String, IList<PointI>> FindAllPlaceTargets(
            WorldState worldState, Faction faction, EntityCategory category) {

            Debug.Assert(worldState != null);
            Debug.Assert(faction != null);

            // get entity classes available to faction
            var classes = faction.GetAvailableClasses(category);
            var classTargets = new SortedList<String, IList<PointI>>(classes.Count);

            // add targets for all available classes
            foreach (EntityClass entityClass in classes) {

                // add valid targets for current class
                IList<PointI> targets = faction.GetPlaceTargets(worldState, entityClass);
                if (targets.Count > 0)
                    classTargets.Add(entityClass.Id, targets);
            }

            return classTargets;
        }

        #endregion
        #region FindAllPlaceTargets(.., IList<Entity>, ...)

        /// <summary>
        /// Finds all valid target locations for any <see cref="PlaceCommand"/> with any of the
        /// specified entities.</summary>
        /// <param name="worldState">
        /// The <see cref="WorldState"/> on which to execute any <see cref="PlaceCommand"/>.</param>
        /// <param name="faction">
        /// The <see cref="Faction"/> that issues the <see cref="PlaceCommand"/>.</param>
        /// <param name="entities">
        /// An <see cref="IList{T}"/> containing the <see cref="Entity"/> objects to place.</param>
        /// <returns>
        /// A dictionary that maps the <see cref="EntityClass.Id"/> string of each distinct <see
        /// cref="Entity.EntityClass"/> value in <paramref name="entities"/> to a <see
        /// cref="List{PointI}"/> containing the coordinates of all valid placement sites for that
        /// <b>EntityClass</b>.</returns>
        /// <remarks><para>
        /// <b>FindAllPlaceTargets</b> calls <see cref="Faction.GetPlaceTargets"/> on the specified
        /// <paramref name="faction"/> to determine the valid placement sites for each <see
        /// cref="Entity.EntityClass"/>. Entity classes without valid placement sites are not added
        /// to the returned collection.
        /// </para><para>
        /// <b>FindAllPlaceTargets</b> never returns a null reference, but it returns an empty
        /// collection if the specified <paramref name="entities"/> collection is empty, or if none
        /// of its elements have any valid placement sites.
        /// </para><para>
        /// The keys and values of the returned dictionary are never null references, and the <see
        /// cref="List{PointI}"/> values are never empty collections.</para></remarks>

        public static SortedList<String, IList<PointI>> FindAllPlaceTargets(
            WorldState worldState, Faction faction, IList<Entity> entities) {

            Debug.Assert(worldState != null);
            Debug.Assert(faction != null);
            Debug.Assert(entities != null);

            var classTargets = new SortedList<String, IList<PointI>>();

            // add targets for all unique classes
            for (int i = 0; i < entities.Count; i++) {
                EntityClass entityClass = entities[i].EntityClass;
                if (classTargets.ContainsKey(entityClass.Id)) continue;

                // add valid targets for current class
                IList<PointI> targets = faction.GetPlaceTargets(worldState, entityClass);
                if (targets.Count > 0)
                    classTargets.Add(entityClass.Id, targets);
            }

            return classTargets;
        }

        #endregion
        #region FindAttackTargets

        /// <summary>
        /// Finds all valid target locations for an <see cref="AttackCommand"/> performed by the
        /// specified units.</summary>
        /// <param name="worldState">
        /// The <see cref="WorldState"/> on which to execute the <see cref="AttackCommand"/>.
        /// </param>
        /// <param name="units">
        /// An <see cref="IList{T}"/> containing all <see cref="Unit"/> objects that participate in
        /// the attack.</param>
        /// <returns>
        /// An <see cref="IList{PointI}"/> containing the coordinates of any valid target <see
        /// cref="Site"/> for an <see cref="AttackCommand"/> executed by the specified <paramref
        /// name="units"/>.</returns>
        /// <remarks><para>
        /// <b>FindAttackTargets</b> never returns a null reference, but it returns an empty
        /// collection if <see cref="Unit.CanAttackTarget"/> returns <c>false</c> for any element in
        /// the specified <paramref name="units"/> collection.
        /// </para><para>
        /// Otherwise, <b>FindAttackTargets</b> returns a collection containing all map locations
        /// that are within <see cref="Unit.AttackRange"/> of the current <see cref="Entity.Site"/>
        /// of all <paramref name="units"/>, and for which <b>CanAttackTarget</b> returns
        /// <c>true</c>.</para></remarks>

        public static IList<PointI> FindAttackTargets(WorldState worldState, IList<Entity> units) {

            Debug.Assert(worldState != null);
            Debug.Assert(units != null);
            Debug.Assert(units.Count > 0);

            // exhausted units cannot attack
            for (int i = 0; i < units.Count; i++)
                if (!((Unit) units[i]).CanAttack)
                    return new List<PointI>(0);

            IList<PointI> targets = null;
            for (int i = 0; i < units.Count; i++) {
                Unit unit = (Unit) units[i];

                // compute all targets for this unit
                var restriction = MapGrid.GetNeighbors(unit.Site, unit.AttackRange);

                // initialize or restrict target collection
                if (targets == null)
                    targets = restriction;
                else
                    CollectionsUtility.Restrict(targets, restriction);

                if (targets.Count == 0) return targets;
            }

            Debug.Assert(targets != null);
            Debug.Assert(targets.Count > 0);

            // remove sites that the units cannot attack
            Unit firstUnit = (Unit) units[0];
            for (int i = targets.Count - 1; i >= 0; i--)
                if (!firstUnit.CanAttackTarget(worldState, units, targets[i]))
                    targets.RemoveAt(i);

            // return remaining sites
            return targets;
        }

        #endregion
        #region FindFactionSite

        /// <summary>
        /// Finds a <see cref="Site"/> associated with the specified <see cref="Faction"/>.
        /// </summary>
        /// <param name="worldState">
        /// The <see cref="WorldState"/> that contains the specified <paramref name="faction"/>.
        /// This argument may be a null reference.</param>
        /// <param name="faction">
        /// The <see cref="Faction"/> for which to find a <see cref="Site"/>. This argument may be a
        /// null reference.</param>
        /// <returns>
        /// The coordinates of a <see cref="Site"/> associated with the specified <paramref
        /// name="faction"/>, if any.</returns>
        /// <remarks><para>
        /// <b>FindFactionSite</b> returns one of the following values:
        /// </para><list type="number"><item>
        /// The value <see cref="Site.InvalidLocation"/> if the specified <paramref name="faction"/>
        /// is a null reference.
        /// </item><item>
        /// Otherwise, the <see cref="Faction.HomeSite"/> of the specified <paramref
        /// name="faction"/>, if valid according to the current <see cref="MapGrid"/>.
        /// </item><item>
        /// Otherwise, the <see cref="Entity.Site"/> of the first active placed <see cref="Unit"/>
        /// owned by the <paramref name="faction"/>, if any.
        /// </item><item>
        /// Otherwise, the <see cref="Entity.Site"/> of the first inactive placed <see cref="Unit"/>
        /// owned by the <paramref name="faction"/>, if any.
        /// </item><item>
        /// Otherwise, the first <see cref="Site"/> owned by the <paramref name="faction"/>, if any.
        /// </item><item>
        /// Otherwise, the value <see cref="Site.InvalidLocation"/>.
        /// </item></list><para>
        /// If the specified <paramref name="worldState"/> is a null reference, a <see cref="Unit"/>
        /// is considered active if its <see cref="Unit.IsActive"/> flag is <c>true</c>; otherwise,
        /// the <see cref="Unit"/> must also have valid targets for an <see cref="AttackCommand"/>
        /// or a <see cref="MoveCommand"/>.</para></remarks>

        public static PointI FindFactionSite(WorldState worldState, Faction faction) {
            if (faction == null)
                return Site.InvalidLocation;

            // use home site if possible
            if (MapGrid.Contains(faction.HomeSite))
                return faction.HomeSite;

            // use site of first active unit, if any
            if (worldState == null) {
                for (int i = 0; i < faction.Units.Count; i++) {
                    Unit unit = (Unit) faction.Units[i];
                    if (unit.IsActive)
                        return unit.Site.Location;
                }
            } else {
                // prepare single-unit collection
                List<Entity> units = new List<Entity>() { null };

                for (int i = 0; i < faction.Units.Count; i++) {
                    units[0] = faction.Units[i];

                    // check for valid attack or movement targets
                    if (units[0].IsPlaced &&
                        (Finder.FindAttackTargets(worldState, units).Count > 0
                        || Finder.FindMoveTargets(worldState, units).Count > 0))
                        return units[0].Site.Location;
                }
            }

            // use site of first inactive unit, if any
            for (int i = 0; i < faction.Units.Count; i++) {
                Entity unit = faction.Units[i];
                if (unit.Site != null)
                    return unit.Site.Location;
            }

            // use first owned site, if any
            if (faction.Sites.Count > 0)
                return faction.Sites[0].Location;

            return Site.InvalidLocation;
        }

        #endregion
        #region FindMovePath(..., Site, ...)

        /// <overloads>
        /// Finds all traversed map locations for a <see cref="MoveCommand"/> with the specified
        /// moving units, source <see cref="Site"/>, and one or more target sites.</overloads>
        /// <summary>
        /// Finds all traversed map locations for a <see cref="MoveCommand"/> with the specified
        /// moving units, source <see cref="Site"/>, and single target site.</summary>
        /// <param name="worldState">
        /// The <see cref="WorldState"/> on which to execute the <see cref="MoveCommand"/>.</param>
        /// <param name="units">
        /// An <see cref="IList{T}"/> containing all <see cref="Unit"/> objects that participate in
        /// the move.</param>
        /// <param name="source">
        /// The <see cref="Site"/> where the move starts.</param>
        /// <param name="target">
        /// The <see cref="Site"/> where the move ends.</param>
        /// <param name="attacking">
        /// <c>true</c> if <paramref name="target"/> should be attacked; <c>false</c> if <paramref
        /// name="target"/> should be occupied.</param>
        /// <returns><para>
        /// An <see cref="IGraphPath{T}"/> instance containing the coordinates of each <see
        /// cref="Site"/> that the specified <paramref name="units"/> would traverse when moving
        /// from the specified <paramref name="source"/> to the specified <paramref name="target"/>.
        /// </para><para>-or-</para><para>
        /// A null reference if there is no unblocked path between <paramref name="source"/> and
        /// <paramref name="target"/>.</para></returns>
        /// <remarks><para>
        /// <b>FindMovePath</b> ignores the <see cref="Entity.Site"/> properties of all specified
        /// <paramref name="units"/>. The moving units may be unplaced or placed on different sites.
        /// </para><para>
        /// On success, <b>FindMovePath</b> returns the path found by the <see cref="AStar"/>
        /// algorithm for the current <see cref="MapGrid"/>.
        /// </para><note type="caution">
        /// The returned <see cref="IGraphPath{T}"/> instance is <em>not</em> thread-safe! All calls
        /// to <b>FindMovePath</b> during a single game session will return references to the same
        /// <see cref="AStar"/> object. Consecutive invocations will overwrite the data returned by
        /// previous invocations.</note></remarks>

        public static IGraphPath<PointI> FindMovePath(WorldState worldState,
            IList<Entity> units, Site source, Site target, bool attacking) {

            Debug.Assert(worldState != null);
            Debug.Assert(units != null);
            Debug.Assert(units.Count > 0);
            Debug.Assert(source != null);
            Debug.Assert(target != null);

            // create proxy agent for A* algorithm
            UnitAgent agent = new UnitAgent(worldState, units);
            agent.Attacking = attacking;
            agent.OriginalSource = source;

            // compute best path if possible
            bool result = AStar.FindBestPath(agent, source, target);
            return (result ? AStar : null);
        }

        #endregion
        #region FindMovePath(..., List<Site>, ...)

        /// <summary>
        /// Finds all traversed map locations for a <see cref="MoveCommand"/> with the specified
        /// moving units, source <see cref="Site"/>, and list of possible target sites.</summary>
        /// <param name="worldState">
        /// The <see cref="WorldState"/> on which to execute the <see cref="MoveCommand"/>.</param>
        /// <param name="units">
        /// An <see cref="IList{T}"/> containing all <see cref="Unit"/> objects that participate in
        /// the move.</param>
        /// <param name="source">
        /// The <see cref="Site"/> where the move starts.</param>
        /// <param name="targets">
        /// An <see cref="IList{Site}"/> containing all possible end points for the move. This
        /// collection must be writable and will be sorted by <see cref="Site.Distance"/> from 
        /// <paramref name="source"/>.</param>
        /// <param name="attacking">
        /// <c>true</c> if <paramref name="targets"/> should be attacked; <c>false</c> if <paramref
        /// name="targets"/> should be occupied.</param>
        /// <returns><para>
        /// An <see cref="IGraphPath{T}"/> instance containing the coordinates of each <see
        /// cref="Site"/> that the specified <paramref name="units"/> would traverse when moving
        /// from the specified <paramref name="source"/> to the nearest of the specified <paramref
        /// name="targets"/>.
        /// </para><para>-or-</para><para>
        /// A null reference if there is no unblocked path between <paramref name="source"/> and any
        /// <paramref name="targets"/> element.</para></returns>
        /// <remarks><para>
        /// <b>FindMovePath</b> ignores the <see cref="Entity.Site"/> properties of all specified
        /// <paramref name="units"/>. The moving units may be unplaced or placed on different sites.
        /// </para><para>
        /// On success, <b>FindMovePath</b> returns the path found by the <see cref="AStar"/>
        /// algorithm for the current <see cref="MapGrid"/>.
        /// </para><note type="caution">
        /// The returned <see cref="IGraphPath{T}"/> instance is <em>not</em> thread-safe! All calls
        /// to <b>FindMovePath</b> during a single game session will return references to the same
        /// <see cref="AStar"/> object. Consecutive invocations will overwrite the data returned by
        /// previous invocations.</note></remarks>

        public static IGraphPath<PointI> FindMovePath(WorldState worldState,
            IList<Entity> units, Site source, IList<Site> targets, bool attacking) {

            Debug.Assert(worldState != null);
            Debug.Assert(units != null);
            Debug.Assert(units.Count > 0);
            Debug.Assert(source != null);
            Debug.Assert(targets != null);

            // create proxy agent for A* algorithm
            UnitAgent agent = new UnitAgent(worldState, units);
            agent.Attacking = attacking;
            agent.OriginalSource = source;

            // find best path to nearest target
            Site.SortByDistance(source, targets);
            foreach (Site target in targets)
                if (AStar.FindBestPath(agent, source, target))
                    return AStar;

            return null;
        }

        #endregion
        #region FindMoveTargets

        /// <summary>
        /// Finds all valid target locations for a <see cref="MoveCommand"/> performed by the
        /// specified units.</summary>
        /// <param name="worldState">
        /// The <see cref="WorldState"/> on which to execute the <see cref="MoveCommand"/>.</param>
        /// <param name="units">
        /// An <see cref="IList{T}"/> containing all <see cref="Unit"/> objects that participate in
        /// the move. All elements must reside on the same valid <see cref="Entity.Site"/>.</param>
        /// <returns>
        /// A read-only <see cref="IList{PointI}"/> containing the coordinates of any valid target
        /// <see cref="Site"/> for a <see cref="MoveCommand"/> executed by the specified <paramref
        /// name="units"/>.</returns>
        /// <remarks><para>
        /// <b>FindMoveTargets</b> never returns a null reference, but it returns an empty
        /// collection if <see cref="Unit.CanMove"/> returns <c>false</c> for any element in the
        /// specified <paramref name="units"/> collection.
        /// </para><para>
        /// Otherwise, <b>FindMoveTargets</b> returns the map locations found by the <see
        /// cref="Coverage"/> algorithm for the current <see cref="MapGrid"/>, using the smallest
        /// <see cref="Unit.Movement"/> value of any <paramref name="units"/> element as the path
        /// cost limit.
        /// </para><note type="caution">
        /// The returned <see cref="IList{T}"/> collection is <em>not</em> thread-safe! All calls to
        /// <b>FindMoveTargets</b> during a single game session will return references to the same
        /// <see cref="Coverage"/> object. Consecutive invocations will overwrite the data returned
        /// by previous invocations.</note></remarks>

        public static IList<PointI> FindMoveTargets(WorldState worldState, IList<Entity> units) {

            Debug.Assert(worldState != null);
            Debug.Assert(units != null);
            Debug.Assert(units.Count > 0);

            Site source = units[0].Site;
            Debug.Assert(source != null);

            // exhausted units cannot move
            for (int i = 0; i < units.Count; i++) {
                Unit unit = (Unit) units[i];
                Debug.Assert(unit.Site == source);

                if (!unit.CanMove)
                    return new List<PointI>(0);
            }

            // create proxy agent for coverage algorithm
            UnitAgent agent = new UnitAgent(worldState, units);
            Debug.Assert(agent.OriginalSource == source);

            // find shortest range for all units
            int movement = WorldUtility.MinimumMovement(units);

            // compute all (partially) reachable sites
            Coverage.FindReachable(agent, source, movement);
            return Coverage.Nodes;
        }

        #endregion
        #region FindNearestTarget

        /// <summary>
        /// Finds the <see cref="Site"/> among a list of potential target sites that is nearest to
        /// the specified source location.</summary>
        /// <param name="source">
        /// The coordinates of the source <see cref="Site"/>.</param>
        /// <param name="targets">
        /// An <see cref="IList{T}"/> containing all <see cref="Site"/> objects that are potential
        /// targets.</param>
        /// <param name="distance">
        /// Returns the site distance between <paramref name="source"/> and the nearest <paramref
        /// name="targets"/> element.</param>
        /// <returns>
        /// The element in <paramref name="targets"/> that is nearest to the specified <paramref
        /// name="source"/> site.</returns>
        /// <remarks><para>
        /// <b>FindNearestTarget</b> returns a null reference if <paramref name="targets"/> is an
        /// empty collection. The specified <paramref name="distance"/> remains unchanged in that
        /// case.
        /// </para><para>
        /// If <paramref name="targets"/> contains multiple elements with the same distance to the
        /// specified <paramref name="source"/>, <b>FindNearestTarget</b> chooses the element with
        /// the lower index position in <paramref name="targets"/>.</para></remarks>

        public static Site FindNearestTarget(PointI source, IList<Site> targets, ref int distance) {

            Debug.Assert(MapGrid.Contains(source));
            Debug.Assert(targets != null);

            if (targets.Count == 0) return null;

            // cache map grid reference
            PolygonGrid mapGrid = MapGrid;

            // start with first target
            Site target = targets[0];
            distance = mapGrid.GetStepDistance(source, target.Location);

            // look for better target
            for (int i = 1; i < targets.Count; i++) {
                Site newTarget = targets[i];
                int newDistance = mapGrid.GetStepDistance(source, newTarget.Location);

                // switch to better target
                if (newDistance < distance) {
                    target = newTarget;
                    distance = newDistance;
                }
            }

            return target;
        }

        #endregion
        #region FindUnitsInAttackRange

        /// <summary>
        /// Finds all units in the specified collection that are individually within attack range of
        /// the specified target location.</summary>
        /// <param name="worldState">
        /// The <see cref="WorldState"/> on which to execute any <see cref="AttackCommand"/>.
        /// </param>
        /// <param name="units">
        /// An <see cref="IList{T}"/> containing the <see cref="Unit"/> objects that perform any
        /// <see cref="AttackCommand"/>.</param>
        /// <param name="target">
        /// The coordinates of the <see cref="Site"/> that is the target of any <see
        /// cref="AttackCommand"/>.</param>
        /// <returns>
        /// A <see cref="List{T}"/> containing all elements in <paramref name="units"/> that can
        /// individually perform any <see cref="AttackCommand"/> on the specified <paramref
        /// name="target"/> from their current location.</returns>
        /// <remarks>
        /// <b>FindUnitsInAttackRange</b> returns those elements in the specified <paramref
        /// name="units"/> collection for whom <see cref="FindAttackTargets"/> returns a collection
        /// that contains the specified <paramref name="target"/>.</remarks>

        public static List<Entity> FindUnitsInAttackRange(
            WorldState worldState, IList<Entity> units, PointI target) {

            Debug.Assert(worldState != null);
            Debug.Assert(units != null);

            // prepare result collection
            List<Entity> unitsInRange = new List<Entity>(units.Count);

            // prepare single-unit collection
            IList<Entity> singleUnit = new List<Entity>() { null };

            // add all units that can attack the target
            for (int i = 0; i < units.Count; i++) {
                singleUnit[0] = units[i];

                IList<PointI> targets = FindAttackTargets(worldState, singleUnit);
                if (targets.Contains(target))
                    unitsInRange.Add(units[i]);
            }

            return unitsInRange;
        }

        #endregion
    }
}
