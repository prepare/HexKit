using Tektosyne.Geometry;

namespace Hexkit.Scenario {

    /// <summary>
    /// Specifies how an <see cref="EntityImage"/> is adapted to the current map geometry.</summary>
    /// <remarks>
    /// <b>ImageScaling</b> specifies if and how all <see cref="EntityImage.Frames"/> of an <see
    /// cref="EntityImage"/> are scaled to fit a polygonal <see cref="PolygonGrid.Element"/> of the
    /// <see cref="AreaSection.MapGrid"/>.</remarks>

    public enum ImageScaling {

        /// <summary>
        /// Specifies no scaling. Each frame is copied once, at its original size.</summary>

        None,

        /// <summary>
        /// Specifies scaling by repeatedly copying each frame, at its original size, until the
        /// polygon is filled.</summary>

        Repeat,

        /// <summary>
        /// Specifies scaling by growing or shrinking each frame to the polygon's bounding box.
        /// </summary>

        Stretch
    }
}
