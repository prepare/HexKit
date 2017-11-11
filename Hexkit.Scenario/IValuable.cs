using System;

namespace Hexkit.Scenario {

    /// <summary>
    /// Provides an object that is valuable to computer players.</summary>
    /// <remarks><para>
    /// <b>IValuable</b> provides a context-free heuristic that allows computer player algorithms to
    /// choose between different objects that implement this interface.
    /// </para><para>
    /// This heuristic is merely intended as a starting point for the evaluation process. Computer
    /// players should also consider the current world state and the faction making the decision.
    /// </para></remarks>

    public interface IValuable {
        #region Valuation

        /// <summary>
        /// Gets the valuation of the <see cref="IValuable"/> instance.</summary>
        /// <value>
        /// A non-negative <see cref="Double"/> value, indicating the desirability of this <see
        /// cref="IValuable"/> instance to computer players. Higher values indicate greater
        /// desirability.</value>
        /// <remarks>
        /// <b>Valuation</b> should return a value in the standard interval [0,1] if possible.
        /// However, the upper bound may be exceeded if the value cannot easily be normalized.
        /// </remarks>

        double Valuation { get; }

        #endregion
    }
}
