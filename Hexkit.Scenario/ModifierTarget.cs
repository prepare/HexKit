namespace Hexkit.Scenario {

    /// <summary>
    /// Specifies the possible targets for a <see cref="VariableModifier"/>.</summary>
    /// <remarks>
    /// <b>ModifierTarget</b> specifies the possible modifier values stored by a <see
    /// cref="VariableModifier"/> instance.</remarks>

    public enum ModifierTarget {

        /// <summary>
        /// Specifies that the <see cref="VariableModifier"/> affects the defining entity itself.
        /// </summary>

        Self,

        /// <summary>
        /// Specifies that the <see cref="VariableModifier"/> affects the faction that owns the
        /// defining entity.</summary>

        Owner,

        /// <summary>
        /// Specifies that the <see cref="VariableModifier"/> affects local units, relative to the
        /// defining entity.</summary>

        Units,

        /// <summary>
        /// Specifies that the <see cref="VariableModifier"/> affects units within modifier range of
        /// the defining entity.</summary>

        UnitsRanged,

        /// <summary>
        /// Specifies that the <see cref="VariableModifier"/> affects local units with the same
        /// owner as the defining entity.</summary>

        OwnerUnits,

        /// <summary>
        /// Specifies that the <see cref="VariableModifier"/> affects units within modifier range
        /// and with the same owner as the defining entity.</summary>

        OwnerUnitsRanged,
    }
}
