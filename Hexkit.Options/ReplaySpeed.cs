namespace Hexkit.Options {

    /// <summary>
    /// Specifies the speed settings for interactive command replay.</summary>
    /// <remarks><para>
    /// <b>ReplaySpeed</b> specifies the possible values for the <see cref="ReplayOptions.Speed"/>
    /// property of the <see cref="ReplayOptions"/> class.
    /// </para><para>
    /// The <see cref="ReplayOptions.Delay"/> property returns the duration of map site highlights
    /// and pauses between replayed commands that correspond to the current <b>Speed</b> value.
    /// </para></remarks>

    public enum ReplaySpeed {

        /// <summary>
        /// Specifies that sites are highlighted for one second, and events are shown at half speed.
        /// </summary>

        Slow,

        /// <summary>
        /// Specifies that sites are highlighted for 500 msec, and events are shown at the default
        /// speed of 250 msec.</summary>

        Medium,

        /// <summary>
        /// Specifies that sites are highlighted for 100 msec, and events are shown at double speed.
        /// </summary>

        Fast,

        /// <summary>
        /// Specifies that sites are highlighted for 100 msec, and events are skipped entirely.
        /// </summary>

        Turbo
    }
}
