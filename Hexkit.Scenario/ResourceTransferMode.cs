namespace Hexkit.Scenario {

    /// <summary>
    /// Specifies the possible modes for automatic resource transfer from <see cref="EntityClass"/>
    /// instances.</summary>
    /// <remarks>
    /// <b>ResourceTransferMode</b> specifies whether an entity's <see
    /// cref="EntityClass.Resources"/> are automatically transferred to other entities or factions,
    /// and whether an entity is automatically deleted when such a transfer has exhausted all its
    /// <see cref="EntityClass.Resources"/>.</remarks>

    public enum ResourceTransferMode {

        /// <summary>
        /// Specifies that the entity does not automatically transfer resources.</summary>

        None,

        /// <summary>
        /// Specifies that the entity automatically transfers resources, and is retained when its
        /// resources are exhausted.</summary>

        Retain,

        /// <summary>
        /// Specifies that the entity automatically transfers resources, and is deleted when its
        /// resources are exhausted.</summary>

        Delete
    }
}
