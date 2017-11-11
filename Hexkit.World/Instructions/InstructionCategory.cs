namespace Hexkit.World.Instructions {

    /// <summary>
    /// Specifies the instruction categories of the Hexkit Command Language.</summary>
    /// <remarks>
    /// <b>InstructionCategory</b> specifies the valid values for the <see
    /// cref="Instruction.Category"/> property of the <see cref="Instruction"/> class.</remarks>

    public enum InstructionCategory {

        /// <summary>
        /// Specifies a normal <see cref="Instruction"/>.</summary>
        /// <remarks>
        /// <b>Normal</b> instructions may change the current <see cref="WorldState"/>. They may be
        /// optimized away if their execution did not result in any data changes.</remarks>

        Normal,

        /// <summary>
        /// Specifies that the <see cref="Instruction"/> is a command event.</summary>
        /// <remarks>
        /// <b>Event</b> instructions cannot change the current <see cref="WorldState"/>. They are
        /// never optimized away, but they are only executed when the client supplies a valid <see
        /// cref="ShowEventCallback"/> method.</remarks>

        Event
    }
}
