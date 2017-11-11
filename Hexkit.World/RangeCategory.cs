namespace Hexkit.World {

    /// <summary>
    /// Specifies range categories for map distances.</summary>
    /// <remarks><para>
    /// <b>RangeCategory</b> provides a general-purpose categorization for distances between map
    /// locations.
    /// </para><para>
    /// The individual categories are not associated with any particular distance by default, but
    /// lower-numbered categories should always designate shorter distances than higher-numbered
    /// categories.</para></remarks>

    public enum RangeCategory {

        /// <summary>Specifies a short range.</summary>
        Short,

        /// <summary>Specifies a medium range.</summary>
        Medium,

        /// <summary>Specifies a long range.</summary>
        Long
    }
}
