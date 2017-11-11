using System;

namespace Hexkit.World.Commands {

    /// <summary>
    /// Counts executed commands and instructions.</summary>
    /// <remarks><para>
    /// <b>ExecutionCounters</b> is a simple data container that allows the <see cref="Command"/>
    /// class to keep track of the number of executed commands and HCL instructions.
    /// </para><para>
    /// These numbers are not used by Hexkit but may be of interest for debugging and optimization
    /// purposes.</para></remarks>

    public class ExecutionCounters {

        /// <summary>
        /// The total number of executed commands.</summary>

        public int Commands;

        /// <summary>
        /// The total number of generated HCL instructions, including ineffectual instructions.
        /// </summary>

        public int Instructions;

        /// <summary>
        /// The total number of generated event instructions, which are never considered
        /// ineffectual.</summary>

        public int EventInstructions;

        /// <summary>
        /// The total number of generated instructions that were not recorded because they did not
        /// change the current <see cref="WorldState"/>.</summary>

        public int IneffectualInstructions;

        #region ToArray

        /// <summary>
        /// Copies all counter values to a new <see cref="Object"/> array.</summary>
        /// <returns>
        /// An <see cref="Array"/> containing the boxed values of all <see cref="Int32"/> fields in
        /// the <see cref="ExecutionCounters"/> object.</returns>
        /// <remarks>
        /// <b>ToArray</b> provides a convenient way to match <see cref="ExecutionCounters"/> data
        /// against string format items in a <see cref="String.Format"/> call.</remarks>

        public object[] ToArray() {
            return new object[] {
                Commands,
                Instructions,
                EventInstructions,
                IneffectualInstructions
            };
        }

        #endregion
    }
}
