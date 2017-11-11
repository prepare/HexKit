namespace Hexkit.Scenario {

    /// <summary>
    /// Specifies the animation mode for an <see cref="EntityImage"/>.</summary>
    /// <remarks>
    /// <b>AnimationMode</b> specifies whether and how to play back the <see
    /// cref="AnimationSequence"/> of an <see cref="EntityImage"/>.</remarks>

    public enum AnimationMode {

        /// <summary>
        /// Specifies no animation. The <see cref="AnimationSequence"/> never starts.</summary>

        None,

        /// <summary>
        /// Specifies random animation. The <see cref="AnimationSequence"/> has a random chance in
        /// each time slice to start when it is not already in progress.</summary>

        Random,

        /// <summary>
        /// Specifies continuous animation. The <see cref="AnimationSequence"/> always starts 
        /// whenever it is not already in progress.</summary>

        Continuous
    }
}
