namespace Hexkit.Graphics {

    /// <summary>
    /// Specifies the index sequence of bitmap catalog tiles.</summary>
    /// <remarks>
    /// Before the start of the regular image tiles, each bitmap catalog contains a number of
    /// special-purpose tiles. The <b>CatalogIndex</b> enumeration indicates the purpose and
    /// location of all such tiles, followed by <see cref="CatalogIndex.Images"/> to indicate the
    /// start of the regular image tiles.</remarks>

    public enum CatalogIndex {

        /// <summary>
        /// The index of the "invalid" icon shown when no valid image exists.</summary>

        Invalid,

        /// <summary>
        /// The index of the standard polygon outline for each map site.</summary>

        Outline,

        /// <summary>
        /// The index of a filled translucent polygon using for color shading.</summary>

        Shading,

        /// <summary>
        /// The index of the first image tile, followed by the remaining image tiles.</summary>

        Images
    }
}
