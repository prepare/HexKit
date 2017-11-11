using System;
using System.Collections.Generic;
using System.Diagnostics;

using Tektosyne;
using Tektosyne.Geometry;
using Tektosyne.Graph;

namespace Hexkit.World {

    /// <summary>
    /// Provides an implementation of the <see cref="IGraphAgent{T}"/> interface with <see
    /// cref="Unit"/> objects as agents.</summary>
    /// <remarks><para>
    /// <b>UnitAgent</b> acts as a proxy that translates <see cref="IGraphAgent{T}"/> methods into
    /// the equivalent <see cref="Unit"/> methods. The <see cref="Unit"/> class cannot directly
    /// implement the <see cref="IGraphAgent{T}"/> interface because its methods require additional
    /// <see cref="WorldState"/> and <see cref="IList{T}"/> parameters.
    /// </para><para>
    /// <b>UnitAgent</b> operates on the <see cref="IGraph2D{T}"/> provided by the current <see
    /// cref="Scenario.AreaSection.MapGrid"/>.</para></remarks>

    public class UnitAgent: IGraphAgent<PointI> {
        #region UnitAgent(WorldState, IList<Entity>)

        /// <summary>
        /// Initializes a new instance of the <see cref="UnitAgent"/> class.</summary>
        /// <param name="worldState">
        /// The initial value for the <see cref="WorldState"/> property.</param>
        /// <param name="units">
        /// The initial value for the <see cref="Units"/> property.</param>
        /// <exception cref="ArgumentException">
        /// <paramref name="units"/> contains elements that are null references.</exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="worldState"/> is a null reference.</exception>
        /// <exception cref="ArgumentNullOrEmptyException">
        /// <paramref name="units"/> is a null reference or an empty collection.</exception>

        public UnitAgent(WorldState worldState, IList<Entity> units) {

            if (worldState == null)
                ThrowHelper.ThrowArgumentNullException("worldState");
            if (units == null || units.Count == 0)
                ThrowHelper.ThrowArgumentNullOrEmptyException("units");

            for (int i = 0; i < units.Count; i++)
                if (units[i] == null)
                    ThrowHelper.ThrowArgumentException(
                        "units", Tektosyne.Strings.ArgumentContainsNull);

            this._worldState = worldState;
            this._units = units;
            this._firstUnit = (Unit) units[0];

            // remember original location if valid
            OriginalSource = this._firstUnit.Site;

            // query rule script for pathfinding parameters
            RelaxedRange = FirstUnit.GetRelaxedRange(worldState, units);
        }

        #endregion
        #region Private Fields

        // construction arguments
        private readonly WorldState _worldState;
        private readonly IList<Entity> _units;
        private readonly Unit _firstUnit;

        #endregion
        #region Attacking

        /// <summary>
        /// Gets or sets a value indicating whether the <see cref="Units"/> are moving towards an
        /// attack target.</summary>
        /// <value>
        /// <c>true</c> if <see cref="IsNearTarget"/> should return the result of <see
        /// cref="Unit.CanAttackTarget"/>; <c>false</c> if <b>IsNearTarget</b> should compare its
        /// <em>distance</em> parameter to zero. The default is <c>false</c>.</value>

        public bool Attacking { get; set; }

        #endregion
        #region FirstUnit

        /// <summary>
        /// Gets the first element in the <see cref="Units"/> collection.</summary>
        /// <value>
        /// The first element in the <see cref="Units"/> collection, cast to type <see
        /// cref="Unit"/>.</value>
        /// <remarks><para>
        /// <b>FirstUnit</b> never returns a null reference. This property never changes once the
        /// object has been constructed.
        /// </para><para>
        /// <see cref="UnitAgent"/> translates <see cref="IGraphAgent{T}"/> methods into various
        /// <see cref="Unit"/> methods which are invoked on the <b>FirstUnit</b>.</para></remarks>

        public Unit FirstUnit {
            [DebuggerStepThrough]
            get { return this._firstUnit; }
        }

        #endregion
        #region OriginalSource

        /// <summary>
        /// Gets or sets the coordinates of the <see cref="Site"/> where the move starts.</summary>
        /// <value><para>
        /// The coordinates of the <see cref="Site"/> that originally contains all moving <see
        /// cref="Units"/>.
        /// </para><para>
        /// The default are the coordinates of the <see cref="Entity.Site"/> of the <see
        /// cref="FirstUnit"/>, if valid; otherwise, <see cref="Site.InvalidLocation"/>.
        /// </para></value>

        public PointI OriginalSource { get; set; }

        #endregion
        #region Units

        /// <summary>
        /// Gets a list of all units that participate in the move.</summary>
        /// <value>
        /// An <see cref="IList{T}"/> containing all <see cref="Unit"/> objects that participate in
        /// the movements of the <see cref="UnitAgent"/>.</value>
        /// <remarks>
        /// <b>Units</b> never returns a null reference or an empty collection. This property never
        /// changes once the object has been constructed.</remarks>

        public IList<Entity> Units {
            [DebuggerStepThrough]
            get { return this._units; }
        }

        #endregion
        #region WorldState

        /// <summary>
        /// Gets the <see cref="World.WorldState"/> on which the move is performed.</summary>
        /// <value>
        /// The <see cref="World.WorldState"/> that contains all moving <see cref="Units"/>.</value>
        /// <remarks>
        /// <b>WorldState</b> never returns a null reference. This property never changes once the
        /// object has been constructed.</remarks>

        public WorldState WorldState {
            [DebuggerStepThrough]
            get { return this._worldState; }
        }

        #endregion
        #region IGraphAgent<T> Members
        #region RelaxedRange

        /// <summary>
        /// Indicates whether the <see cref="UnitAgent"/> can enter <see cref="IGraph2D{T}"/> nodes
        /// that exceed the maximum path cost for a movement.</summary>
        /// <value>
        /// <c>true</c> if the <see cref="UnitAgent"/> may end a movement on an <see
        /// cref="IGraph2D{T}"/> node that exceeds the maximum path cost for the movement;
        /// otherwise, <c>false</c>.</value>
        /// <remarks>
        /// <b>RelaxedRange</b> returns the (cached) result of <see cref="Unit.GetRelaxedRange"/>,
        /// invoked on <see cref="FirstUnit"/> with the current <see cref="WorldState"/> and <see
        /// cref="Units"/>.</remarks>

        public bool RelaxedRange { get; private set; }

        #endregion
        #region CanMakeStep

        /// <summary>
        /// Determines whether the <see cref="UnitAgent"/> can move from one specified <see
        /// cref="IGraph2D{T}"/> node to another neighboring node.</summary>
        /// <param name="source">
        /// The <see cref="IGraph2D{T}"/> node where the move starts.</param>
        /// <param name="target">
        /// The <see cref="IGraph2D{T}"/> node where the move ends. This node must be a neighbor of
        /// <paramref name="source"/>.</param>
        /// <returns>
        /// <c>true</c> if the <see cref="UnitAgent"/> can move from <paramref name="source"/> to
        /// <paramref name="target"/>; otherwise, <c>false</c>.</returns>
        /// <remarks>
        /// <b>CanMakeStep</b> returns the result of <see cref="Unit.CanMakeStep"/>, invoked on <see
        /// cref="FirstUnit"/> with the current <see cref="WorldState"/>, <see cref="Units"/>, <see
        /// cref="OriginalSource"/>, and the specified arguments.</remarks>

        public bool CanMakeStep(PointI source, PointI target) {
            return FirstUnit.CanMakeStep(WorldState, Units, OriginalSource, source, target);
        }

        #endregion
        #region CanOccupy

        /// <summary>
        /// Determines whether the <see cref="UnitAgent"/> can permanently occupy the specified <see
        /// cref="IGraph2D{T}"/> node.</summary>
        /// <param name="target">
        /// The <see cref="IGraph2D{T}"/> node to occupy.</param>
        /// <returns>
        /// <c>true</c> if the <see cref="UnitAgent"/> can permanently occupy <paramref
        /// name="target"/>; otherwise, <c>false</c>.</returns>
        /// <remarks>
        /// <b>CanOccupy</b> returns the result of <see cref="Unit.CanOccupy"/>, invoked on <see
        /// cref="FirstUnit"/> with the current <see cref="WorldState"/>, <see cref="Units"/>, <see
        /// cref="OriginalSource"/>, and the specified argument.</remarks>

        public bool CanOccupy(PointI target) {
            return FirstUnit.CanOccupy(WorldState, Units, OriginalSource, target);
        }

        #endregion
        #region GetStepCost

        /// <summary>
        /// Returns the cost for moving the <see cref="UnitAgent"/> from one specified <see
        /// cref="IGraph2D{T}"/> node to another neighboring node.</summary>
        /// <param name="source">
        /// The <see cref="IGraph2D{T}"/> node where the move starts.</param>
        /// <param name="target">
        /// The <see cref="IGraph2D{T}"/> node where the move ends. This node must be a neighbor of
        /// <paramref name="source"/>.</param>
        /// <returns>
        /// The cost for moving the <see cref="UnitAgent"/> from <paramref name="source"/> to
        /// <paramref name="target"/>. This value is always positive.</returns>
        /// <remarks>
        /// <b>GetStepCost</b> returns the result of <see cref="Unit.GetStepCost"/>, invoked on <see
        /// cref="FirstUnit"/> with the current <see cref="WorldState"/>, <see cref="Units"/>, and
        /// the specified arguments.</remarks>

        public double GetStepCost(PointI source, PointI target) {
            return FirstUnit.GetStepCost(WorldState, Units, source, target);
        }

        #endregion
        #region IsNearTarget

        /// <summary>
        /// Determines whether the specified <see cref="IGraph2D{T}"/> node is near enough to the
        /// specified target node to be considered equivalent.</summary>
        /// <param name="source">
        /// The <see cref="IGraph2D{T}"/> node to consider.</param>
        /// <param name="target">
        /// The target node within the <see cref="IGraph2D{T}"/>.</param>
        /// <param name="distance">
        /// The distance, in map sites, between <paramref name="source"/> and <paramref
        /// name="target"/>. This argument may be negative if the distance is unknown.</param>
        /// <returns>
        /// <c>true</c> if a movement towards <paramref name="target"/> should be considered
        /// complete when <paramref name="source"/> is reached; otherwise, <c>false</c>.</returns>
        /// <remarks><para>
        /// If <see cref="Attacking"/> is <c>true</c>, <b>IsNearTarget</b> returns the result of
        /// <see cref="Unit.CanAttackTarget"/>, invoked on <see cref="FirstUnit"/> with the current
        /// <see cref="WorldState"/>, <see cref="Units"/>, and the specified arguments.
        /// </para><para>
        /// Otherwise, <b>IsNearTarget</b> returns <c>true</c> exactly if the specified <paramref
        /// name="distance"/> is zero, or <paramref name="source"/> equals <paramref
        /// name="target"/>.</para></remarks>

        public bool IsNearTarget(PointI source, PointI target, double distance) {
            if (Attacking)
                return FirstUnit.CanAttackTarget(WorldState, Units, source, target, (int) distance);

            if (distance > 0) return false;
            return (distance == 0 || source == target);
        }

        #endregion
        #endregion
    }
}
