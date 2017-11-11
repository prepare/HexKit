namespace Hexkit.Scenario {

    /// <summary>
    /// Specifies the game parameter that is evaluated by a <see cref="Condition"/>.</summary>
    /// <remarks>
    /// <b>ConditionParameter</b> specifies the possible values for the <see
    /// cref="Condition.Parameter"/> property of the <see cref="Condition"/> class.</remarks>

    public enum ConditionParameter {

        /// <summary>
        /// Specifies a condition based on the number of sites owned by the faction.</summary>

        Sites,

        /// <summary>
        /// Specifies a condition based on the number of units owned by the faction.</summary>

        Units,

        /// <summary>
        /// Specifies a condition based on the total strength of all units owned by the faction.
        /// </summary>

        UnitStrength,

        /// <summary>
        /// Specifies a condition based on the number of turns completed in the game.</summary>

        Turns
    }
}
