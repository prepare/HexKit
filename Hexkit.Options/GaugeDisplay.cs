using System;
using Hexkit.World;

namespace Hexkit.Options {

    /// <summary>
    /// Specifies the display mode for resource gauges on the map view.</summary>
    /// <remarks>
    /// <b>GaugeDisplay</b> specifies the possible values for the <see
    /// cref="MapViewOptions.GaugeResourceFlags"/> property of the <see cref="MapViewOptions"/>
    /// class. Any bitwise combination of <b>GaugeDisplay</b> flags is legal.</remarks>

    [Flags]
    public enum GaugeDisplay {

        /// <summary>
        /// Specifies that resource gauges are always shown. By default, resource gauges are shown
        /// only when partially depleted.</summary>

        Always = 1,

        /// <summary>
        /// Specifies that resource gauges show the combined status of an entire <see cref="Unit"/>
        /// stack. By default, only the status of the topmost <see cref="Unit"/> is shown.</summary>

        Stack = 2,
    }
}
