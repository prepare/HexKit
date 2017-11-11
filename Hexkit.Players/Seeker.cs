using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;

using Tektosyne.Geometry;
using Tektosyne.Graph;
using Tektosyne.Windows;

using Hexkit.Scenario;
using Hexkit.World;

namespace Hexkit.Players {

    /// <summary>
    /// Provides the "Seeker" computer player algorithm.</summary>
    /// <remarks><para>
    /// <b>Seeker</b> provides an implementation of <see cref="Algorithm"/> that sends units
    /// individually against the nearest enemy units and enemy-held sites.
    /// </para><para>
    /// Please refer to the <em>User's Guide</em> for a detailed description of this computer player
    /// algorithm.</para></remarks>

    public sealed class Seeker: AlgorithmGeneral {
        #region FindBestCommands

        /// <summary>
        /// Performs the actual computer player calculations for the <see cref="Seeker"/> algorithm.
        /// </summary>
        /// <param name="worldState">
        /// The <see cref="WorldState"/> whose active faction should issue commands.</param>
        /// <param name="options">
        /// An <see cref="AlgorithmOptions"/> object containing optional settings for the <see
        /// cref="Seeker"/> algorithm.</param>
        /// <param name="events">
        /// An optional <see cref="TaskEvents"/> object used for progress display.</param>
        /// <remarks>
        /// Please refer to <see cref="Algorithm.FindBestCommands"/> and to the <em>User's
        /// Guide</em> for details.</remarks>

        protected override void FindBestCommands(WorldState worldState,
            AlgorithmOptions options, TaskEvents events) {

            // parameters for message events
            long messageTicks = 1000L;
            string messageFormat = Global.Strings.StatusUnitProcessing;

            Faction faction = worldState.ActiveFaction;
            SizeI mapSize = Finder.MapGrid.Size;

            // collections for all potential target sites
            var attackTargets = new List<Site>();
            var engagedTargets = new List<Site>();
            var captureTargets = new List<Site>();
            var freeCaptureTargets = new List<Site>();
            var garrisonTargets = new List<Site>();

            // find potential target sites
            for (int x = 0; x < mapSize.Width; x++)
                for (int y = 0; y < mapSize.Height; y++) {
                    Site site = worldState.Sites[x,y];

                    bool hasAlienUnits = site.HasAlienUnits(faction);
                    if (hasAlienUnits) attackTargets.Add(site);

                    if (site.CanCapture) {
                        if (site.Owner != faction) {
                            captureTargets.Add(site);
                            if (!hasAlienUnits) freeCaptureTargets.Add(site);
                        } else
                            garrisonTargets.Add(site);
                    }
                }

            // get all placed units of this faction
            var units = faction.GetEntities(EntityCategory.Unit, true);

            // get all placed units that are active
            var activeUnits = new List<Entity>(units.Count);
            foreach (Unit unit in units)
                if (unit.IsActive) activeUnits.Add(unit);

            // select method to compare combat results
            Comparison<CombatResults> combatComparison = SelectCombatComparison(worldState);

            int index = 0; // current element in activeUnits
            int odds = 4;  // initially required attack odds

            // maximum number of targets to examine
            int targetLimit = options.TargetLimit;

            // any commands in current loop?
            bool commandIssued = false;

            // any target found in current loop?
            bool targetFound = false;

            // units without target waiting for world state to change
            var waitingUnits = new List<Entity>(units.Count);

            // prepare one-element collection
            units.Clear(); units.Add(null);

            while (activeUnits.Count > 0) {
                Unit unit = (Unit) activeUnits[index];
                Debug.Assert(unit.IsActive);
                units[0] = unit; // store in one-element collection

                // show current unit for long calculations
                if (events != null && events.RestartTimer(messageTicks)) {
                    messageTicks = 250L; // update more frequently
                    events.OnTaskMessage(this, messageFormat,
                        index + 1, activeUnits.Count, unit.Name);
                }

                Site source = unit.Site;
                Site attackTarget = null, moveTarget = null;
                int attackDistance = -1, moveDistance = -1;
                IList<PointI> neighbors = null;

                // skip units waiting without target
                if (waitingUnits.Contains(unit))
                    goto finished;

                /*
                 * Determine the nearest attack target.
                 *
                 * If CanMove is true we try to find an attack target even if CanAttack is false,
                 * so that we can start moving towards the closest enemy for a later attack.
                 * 
                 * However, we have nowhere to move if such a target is already within range,
                 * so we delete the useless target if CanAttack is false.
                 */

                if (unit.IsCombat) {

                    if (unit.CanMove) {
                        attackTarget = SelectAttackTarget(worldState, units,
                            attackTargets, targetLimit, engagedTargets,
                            combatComparison, ref neighbors, ref attackDistance);

                        // delete useless target that induces no action
                        if (attackDistance == 0 && !unit.CanAttack) {
                            attackTarget = null;
                            attackDistance = -1;
                        }
                    }
                    else if (unit.CanAttack) {
                        attackTarget = SelectAttackTarget(worldState, units,
                            attackTargets, targetLimit, combatComparison);

                        // targets found here are always within range
                        if (attackTarget != null) attackDistance = 0;
                    }

                    /*
                     * Attacks during the last cycle (odds == 0) are usually suicidal.
                     * 
                     * Thus, if we are in the last cycle and have already moved, we check whether
                     * all defending units are limited to passive defense. If so, we call off the
                     * attack and wait for support units or better targets to show up.
                     * 
                     * Otherwise, we go ahead with the attack since this overwhelming force would
                     * attack our unit anyway once its owner becomes active.
                     */

                    if (attackTarget != null && odds == 0 && !unit.CanMove) {
                        bool canDefendOnly = true;

                        foreach (Unit defender in attackTarget.Units)
                            canDefendOnly &= defender.CanDefendOnly;

                        if (canDefendOnly) {
                            attackTarget = null;
                            attackDistance = -1;
                        }
                    }

                    // remember that we engaged this target
                    if (attackTarget != null && !engagedTargets.Contains(attackTarget))
                        engagedTargets.Add(attackTarget);
                }

                /*
                 * Determine the nearest move target.
                 *
                 * We may want to ignore a move target in favor of an attack target or vice versa;
                 * this is covered by a series of heuristics once we have two valid targets.
                 *
                 * We skip this block if there is an attack target in range and we do not yet
                 * accept even odds, as the move target likely must be recalculated anyway
                 * after initial attacks.
                 */

                if ((attackDistance != 0 || odds <= 1) && unit.CanMove) {
                    List<Site> selected = null;

                    // highest priority: capture terrain
                    if (moveTarget == null && unit.CanCapture) {
                        selected = freeCaptureTargets;
                        moveTarget = SelectMoveTarget(worldState, units, selected,
                            targetLimit, -1, ref neighbors, ref moveDistance);
                    }

                    /*
                     * Check if we need to resupply. All supply targets must be recalculated
                     * when needed because available supplies might have been spent already,
                     * or changed unpredictably by other actions.
                     */

                    PointI[] supplies = unit.GetRequiredSupplies();
                    if (supplies != null) {
                        int supplyIndex = -1, priority = 0;

                        // determine highest supply priority
                        for (int i = 0; i < supplies.Length; i++)
                            if (supplies[i].Y > priority) {
                                supplyIndex = i;
                                priority = supplies[i].Y;
                            }

                        // ignore priority below 60%
                        if (priority >= 60) {
                            Debug.Assert(supplyIndex >= 0);

                            // prefer nearby capture targets
                            if (moveTarget != null && moveDistance == 1)
                                goto executeMove;

                            // select supply target if possible
                            selected = faction.GetSupplyTargets(worldState);
                            moveTarget = SelectMoveTarget(worldState, units, selected,
                                targetLimit, supplyIndex, ref neighbors, ref moveDistance);

                            // skip remaining heuristics
                            if (moveTarget != null) {

                                // no suicide attacks while resupplying!
                                if (odds == 0) goto executeMove;

                                goto executeAttack;
                            }
                        }
                    }

                    // try garrison duty if all else fails
                    if (moveTarget == null) {
                        selected = garrisonTargets;
                        moveTarget = SelectMoveTarget(worldState, units, selected,
                            targetLimit, -1, ref neighbors, ref moveDistance);
                    }

                    // no move target, use attack target
                    if (moveTarget == null) {
                        selected = null;

                        // no targets at all, skip unit
                        if (attackTarget == null)
                            goto finished;

                        goto switchTarget;
                    }

                    // no attack target, move unit
                    if (attackTarget == null)
                        goto executeMove;

                    Debug.Assert(attackTarget != null);
                    Debug.Assert(moveTarget != null);

                    // identical targets or no movement required
                    if (attackTarget == moveTarget || (attackDistance == 0 && moveDistance == 0))
                        goto executeAttack;

                    /*
                     * Check if occupying a move target would bring us within attack range
                     * of a nearby attack target.
                     *
                     * First, see if the current move target happens to meet this criterion,
                     * and perform movement if so.
                     *
                     * Otherwise, if the move target is either a garrison target or a remote
                     * capture target, see if another garrison target is reachable in one turn
                     * and within attack range, and use that target if so.
                     */

                    if (attackDistance <= 1) {
                        PointI target = attackTarget.Location;

                        if (moveDistance <= 1) {
                            // check if target is attack position
                            if (unit.CanAttackTarget(worldState, units, moveTarget, target, -1))
                                goto executeMove;
                        }

                        if (selected == garrisonTargets ||
                            (moveDistance > 1 && selected == freeCaptureTargets)) {

                            // sort by distance from unit
                            Site.SortByDistance(source.Location, garrisonTargets);

                            // look for better attack position
                            for (int i = 0; i < garrisonTargets.Count; i++) {
                                Site cursorSite = garrisonTargets[i];

                                // stop if distance exceeds move range
                                if (cursorSite.Distance > unit.Movement)
                                    break;

                                // we already checked move target
                                if (cursorSite == moveTarget) continue;

                                // check reachable garrison target
                                PointI cursor = cursorSite.Location;
                                if (cursorSite.Distance == 0 || neighbors.Contains(cursor)) {
                                    if (unit.CanAttackTarget(worldState, units, cursor, target, -1)) {

                                        // no movement necessary
                                        if (cursorSite.Distance == 0) {
                                            Debug.Assert(attackDistance == 0);
                                            goto switchTarget;
                                        }

                                        moveTarget = cursorSite;
                                        moveDistance = 1;
                                        goto executeMove;
                                    }
                                }
                            }
                        }
                    }

                    // ignore low-priority garrison target
                    if (selected == garrisonTargets)
                        goto switchTarget;

                    /*
                     * Usually, we adopt the attack target if it is closer than the move target
                     * (by range category or path cost), and keep the move target otherwise.
                     *
                     * As a special case, we keep the move target if it can be reached within one
                     * turn, even if the attack target is already within range, so that our units
                     * don't stand around stupidly in front of an empty target site.
                     *
                     * As an exception to the special case, we ignore the move target if our unit
                     * is the last combat unit in its stack; we are on a site that can be captured;
                     * there are enemy units on the attack target that can capture our site;
                     * and the move target is less valuable than the unit's current site.
                     */

                    Debug.Assert(selected == freeCaptureTargets);

                    if (attackDistance >= moveDistance) {
                        Debug.Assert(attackDistance > 0);
                        goto executeMove;
                    }

                    if (moveDistance == 1) {
                        Debug.Assert(attackDistance == 0);

                        if (source.CanCapture && source.CountCombatUnits() == 1) {
                            bool captureThreat = false;

                            foreach (Unit defender in attackTarget.Units)
                                if (defender.CanCapture) {
                                    captureThreat = true;
                                    break;
                                }

                            if (captureThreat) {
                                double sourceValue = faction.Evaluate(worldState, source);
                                double targetValue = faction.Evaluate(worldState, moveTarget);

                                // ignore worthless target
                                if (sourceValue > targetValue)
                                    goto switchTarget;
                            }
                        }

                        goto executeAttack;
                    }

                switchTarget:
                    // move towards attack target
                    moveTarget = attackTarget;
                    moveDistance = attackDistance;
                }

            executeAttack:
                // attack if target already in range
                if (attackDistance == 0 && unit.CanAttack) {
                    PointI target = attackTarget.Location;
                    IList<PointI> targets = Finder.FindAttackTargets(worldState, units);

                    // sanity check for valid target
                    if (!targets.Contains(target)) {
                        string message = String.Format(CultureInfo.InvariantCulture,
                            "Seeker(turn {0}, {1}): Invalid attack {2} from {3} to {4}",
                            worldState.CurrentTurn, faction.Id, unit.Id, source.Location, target);

                        Debug.WriteLine(message, "Pathfinding");
                        goto executeMove;
                    }

                    bool eliminated = false;

                    // attempt group attack on target
                    if (AttemptGroupAttack(worldState, unit, target, odds, activeUnits)) {
                        commandIssued = true;

                        // remove target if all enemies dead
                        if (!attackTarget.HasAlienUnits(faction)) {
                            attackTargets.Remove(attackTarget);
                            engagedTargets.Remove(attackTarget);

                            // site can now be captured
                            if (captureTargets.Contains(attackTarget))
                                freeCaptureTargets.Add(attackTarget);

                            eliminated = true;
                        }

                        // restart cycle if unit inactive
                        if (!unit.IsActive) {

                            // guaranteed by AttemptGroupAttack
                            Debug.Assert(!activeUnits.Contains(unit));

                            index = -1;
                            goto finished;
                        }

                        // recalculate active unit index
                        index = activeUnits.IndexOf(unit);
                        Debug.Assert(index >= 0);

                        // perform follow-up movement
                        if (eliminated && unit.CanMove)
                            continue;
                    }
                }

            executeMove:
                // move towards target if necessary
                if (moveDistance > 0 && unit.CanMove) {
                    Debug.Assert(moveTarget != null);

                    bool attacking = (attackTarget == moveTarget);
                    var path = Finder.FindMovePath(worldState, units, source, moveTarget, attacking);

                    // sanity check for valid path
                    if (path == null) {
                        string message = String.Format(CultureInfo.InvariantCulture,
                            "Seeker(turn {0}, {1}): Invalid move {2} from {3} to {4}",
                            worldState.CurrentTurn, faction.Id, unit.Id,
                            source.Location, moveTarget.Location);

                        Debug.WriteLine(message, "Pathfinding");
                        goto finished;
                    }

                    // move as close to target as possible
                    PointI moveLocation = path.GetLastNode(unit.Movement);

                    // delete target if no move possible
                    if (moveLocation == path.Nodes[0]) {
                        moveTarget = null;
                        moveDistance = -1;
                        goto finished;
                    }

                    // execute partial move towards target
                    Executor.ExecuteMove(worldState, units, moveLocation);
                    commandIssued = true;

                    // update target for partial move
                    if (moveLocation != moveTarget.Location)
                        moveTarget = worldState.GetSite(moveLocation);

                    // remove capture target if now owned
                    if (moveTarget.CanCapture && moveTarget.Owner == faction) {
                        captureTargets.Remove(moveTarget);
                        freeCaptureTargets.Remove(moveTarget);

                        // target should be garrisoned now
                        if (!garrisonTargets.Contains(moveTarget))
                            garrisonTargets.Add(moveTarget);
                    }

                    // restart cycle if unit inactive
                    if (!unit.IsActive) {
                        activeUnits.Remove(unit);
                        index = -1;
                        goto finished;
                    }

                    // perform follow-up attack
                    if (unit.CanAttack) continue;
                }

            finished:
                // quit if no active units left
                if (activeUnits.Count == 0) break;

                /*
                 * Check if we found any targets for the current unit.
                 * 
                 * We check attackDistance rather than attackTarget because the latter
                 * is always valid if there is any enemy on the entire map, but we only
                 * care if the unit can actually perform some action to reach that enemy.
                 * 
                 * That is the case if we can move towards the enemy (moveTarget != null)
                 * or if the enemy is already within attack range (attackDistance == 0).
                 * 
                 * We remember whether any targets were found during the current loop,
                 * and let units without a target wait for the world state to change.
                 */

                if (attackDistance == 0 || moveTarget != null)
                    targetFound = true;
                else
                    waitingUnits.Add(unit);

                // increment index with wraparound
                index = (index + 1) % activeUnits.Count;

                /*
                 * Zero index means we have finished one loop through all active units.
                 * 
                 * If any commands were issued, we reactivate all units that waited for
                 * the world state to change, and restart the loop with the current odds.
                 * 
                 * Otherwise, if no targets at all were found for any unit, we quit.
                 * Otherwise, we reduce the odds and quit if they are now negative.
                 * Otherwise, we restart the loop with the reduced odds.
                 */

                if (index == 0) {
                    if (commandIssued)
                        waitingUnits.Clear();
                    else if (!targetFound || --odds < 0)
                        break;

                    commandIssued = false;
                    targetFound = false;
                }
            }

            // build random or most valued entities
            if (options.UseRandomBuild)
                BuildRandom(worldState);
            else
                BuildByValue(worldState);

            // place entities randomly or near threats
            if (options.UseRandomPlace)
                PlaceRandom(worldState);
            else
                PlaceByThreat(worldState);
        }

        #endregion
    }
}
