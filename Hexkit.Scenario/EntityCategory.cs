using Hexkit.Global;

namespace Hexkit.Scenario {

    /// <summary>
    /// Specifies the possible categories of entity classes.</summary>
    /// <remarks>
    /// <b>EntityCategory</b> specifies the categories that correspond to each of the classes
    /// derived from <see cref="EntityClass"/>, as well as to each of the XML elements in <see
    /// cref="FilePaths.ScenarioSchema"/> derived from the complex type "entityClass".</remarks>

    public enum EntityCategory: byte {

        /// <summary>
        /// Specifies the category represented by <see cref="UnitClass"/>.</summary>

        Unit,

        /// <summary>
        /// Specifies the category represented by <see cref="TerrainClass"/>.</summary>

        Terrain,

        /// <summary>
        /// Specifies the category represented by <see cref="EffectClass"/>.</summary>

        Effect,

        /// <summary>
        /// Specifies the category represented by <see cref="UpgradeClass"/>.</summary>

        Upgrade
    }
}
