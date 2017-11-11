namespace Hexkit.Scenario {

    /// <summary>
    /// Specifies which targets are valid.</summary>
    /// <remarks>
    /// <b>TargetMode</b> specifies which targets are considered valid for an active unit, for
    /// example during ranged attacks.</remarks>

    public enum TargetMode {

        /// <summary>
        /// Specifies that any target is valid, including those hidden by fog of war.</summary>

        Any,

        /// <summary>
        /// Specifies that targets must be visible, though not necessarily to the active unit.
        /// </summary>

        View,

        /// <summary>
        /// Specifies that targets must share an unobstructed line of sight with the active unit.
        /// </summary>

        Line
    }
}
