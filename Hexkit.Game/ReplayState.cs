namespace Hexkit.Game {

    /// <summary>
    /// Specifies the possible states of the <see cref="ReplayManager"/>.</summary>
    /// <remarks>
    /// <b>ReplayState</b> specifies the possible values for the <see
    /// cref="ReplayManager.CurrentState"/> and <see cref="ReplayManager.RequestedState"/>
    /// properties of the <see cref="ReplayManager"/> class.</remarks>

    public enum ReplayState {

        /// <summary>
        /// Specifies that the <see cref="ReplayManager"/> is idle.</summary>

        Stop,

        /// <summary>
        /// Specifies that the <see cref="ReplayManager"/> is active and working normally.</summary>

        Play,

        /// <summary>
        /// Specifies that the <see cref="ReplayManager"/> is active but skipping ahead. Requesting
        /// <b>Skip</b> always skips ahead to the next faction.</summary>

        Skip,

        /// <summary>
        /// Specifies that the <see cref="ReplayManager"/> is active but paused.</summary>

        Pause
    }
}
