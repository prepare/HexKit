namespace Hexkit.Scenario {

    /// <summary>
    /// Specifies the animation sequence for an <see cref="EntityImage"/>.</summary>
    /// <remarks>
    /// <b>AnimationSequence</b> specifies the sequence in which the <see
    /// cref="EntityImage.Frames"/> of an <see cref="EntityImage"/> should be played back, assuming
    /// that the associated <see cref="AnimationMode"/> enables animation.</remarks>

    public enum AnimationSequence {

        /// <summary>
        /// Specifies random animation. The sequence shows a single random frame, then terminates.
        /// </summary>

        Random,

        /// <summary>
        /// Specifies forward animation. The sequence proceeds from first to last frame.</summary>

        Forward,

        /// <summary>
        /// Specifies backward animation. The sequence proceeds from last to first frame.</summary>

        Backward,

        /// <summary>
        /// Specifies cyclic animation. The sequence proceeds from first to last frame, then back to
        /// the first frame.</summary>

        Cycle
    }
}
