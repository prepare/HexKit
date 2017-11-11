using System;
using System.Collections.Generic;

using Tektosyne;
using Hexkit.Scenario;

using FormsSortOrder = System.Windows.Forms.SortOrder;

namespace Hexkit.World {

    /// <summary>
    /// Provides a generic <see cref="IComparer{T}"/> that compares two <see cref="IValuable"/>
    /// instances by their contextual valuations.</summary>
    /// <typeparam name="T">
    /// The actual type of the <see cref="IValuable"/> instances to compare.</typeparam>
    /// <remarks><para>
    /// <b>ValuableComparer</b> orders <see cref="IValuable"/> instances by their contextual
    /// valuation, which is calculated by the <see cref="Faction.Evaluate(WorldState, IValuable)"/>
    /// method of the <see cref="Faction"/> class. The calculation is based on the context-free <see
    /// cref="IValuable.Valuation"/> of the <b>IValuable</b> instance and on the current <see
    /// cref="WorldState"/>.
    /// </para><para>
    /// Clients specify the desired <see cref="WorldState"/> and the evaluating <see
    /// cref="Faction"/> when creating a new instance of the <b>ValuableComparer</b> class.
    /// Moreover, they may use the <see cref="ValuableComparer{T}.SortOrder"/> property to choose
    /// between ascending and descending sort order.</para></remarks>

    public class ValuableComparer<T>: IComparer<T> where T: IValuable {
        #region ValuableComparer(WorldState, Faction)

        /// <summary>
        /// Initializes a new instance of the <see cref="ValuableComparer{T}"/> class with the
        /// specified <see cref="WorldState"/> and <see cref="Faction"/>.</summary>
        /// <param name="worldState">
        /// The <see cref="WorldState"/> on which to base the valuations.</param>
        /// <param name="faction">
        /// The <see cref="Faction"/> performing the valuations.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="worldState"/> or <paramref name="faction"/> is a null reference.
        /// </exception>

        public ValuableComparer(WorldState worldState, Faction faction) {

            if (worldState == null)
                ThrowHelper.ThrowArgumentNullException("worldState");
            if (faction == null)
                ThrowHelper.ThrowArgumentNullException("faction");

            this._worldState = worldState;
            this._faction = faction;
            SortOrder = FormsSortOrder.Ascending;
        }

        #endregion
        #region Private Fields

        // construction arguments
        private readonly WorldState _worldState;
        private readonly Faction _faction;

        #endregion
        #region SortOrder

        /// <summary>
        /// Gets or sets the sort order of the <see cref="ValuableComparer{T}"/>.</summary>
        /// <value>
        /// A <see cref="SortOrder"/> value indicating the order in which the <see
        /// cref="IValuable"/> instances are sorted. The default is <see
        /// cref="FormsSortOrder.Ascending"/>.</value>
        /// <remarks>
        /// The <see cref="Compare"/> method will compare all <see cref="IValuable"/> instances as
        /// equal if <b>SortOrder</b> is <see cref="FormsSortOrder.None"/>.</remarks>

        public FormsSortOrder SortOrder { get; set; }

        #endregion
        #region IComparer<T> Members

        /// <summary>
        /// Compares two <see cref="IValuable"/> instances and returns an indication of their
        /// relative values.</summary>
        /// <param name="x">
        /// The first <see cref="IValuable"/> instance to compare.</param>
        /// <param name="y">
        /// The second <see cref="IValuable"/> instance to compare.</param>
        /// <returns>
        /// An <see cref="Int32"/> value indicating the relative order of <paramref name="x"/> and
        /// <paramref name="y"/>, depending on their contextual valuation.</returns>
        /// <remarks><para>
        /// <b>Compare</b> invokes <see cref="Faction.Evaluate"/> on the <see cref="Faction"/>
        /// supplied to the constructor to compute the contextual valuation for both arguments, and
        /// compares the valuations using <see cref="Double.CompareTo"/>. The sign of the result is
        /// inverted if <see cref="SortOrder"/> equals <see cref="FormsSortOrder.Descending"/>.
        /// </para><para>
        /// <b>Compare</b> returns zero without attempting to compare the specified arguments if
        /// <b>SortOrder</b> equals <see cref="FormsSortOrder.None"/>.
        /// </para><para>
        /// Either or both arguments may be null references. Two null references compare as equal,
        /// and a single null reference compares as smaller than the other argument using ascending
        /// sorting, and as greater using descending sorting.</para></remarks>

        public int Compare(T x, T y) {

            // quit if sorting disabled
            if (SortOrder == FormsSortOrder.None)
                return 0;

            // buffer desired sort order
            bool ascending = (SortOrder == FormsSortOrder.Ascending);

            // standard behavior for null references
            if (x == null) {
                if (y == null) return 0;
                return (ascending ? -1 : 1);
            } else if (y == null)
                return (ascending ? 1 : -1);

            // determine order according to valuation
            double valueX = this._faction.Evaluate(this._worldState, x);
            double valueY = this._faction.Evaluate(this._worldState, y);
            int result = valueX.CompareTo(valueY);

            // invert result for descending sort
            return (ascending ? result : -result);
        }

        #endregion
    }
}
