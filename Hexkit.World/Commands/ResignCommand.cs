using System;

using Tektosyne;
using Hexkit.Global;

namespace Hexkit.World.Commands {

    /// <summary>
    /// Represents a "Resign" command.</summary>
    /// <remarks>
    /// <b>ResignCommand</b> is serialized to the XML element "Resign" defined in <see
    /// cref="FilePaths.SessionSchema"/>.</remarks>

    public sealed class ResignCommand: Command {
        #region ResignCommand()

        /// <overloads>
        /// Initializes a new instance of the <see cref="ResignCommand"/> class.</overloads>
        /// <summary>
        /// Initializes a new instance of the <see cref="ResignCommand"/> class with default
        /// properties.</summary>

        internal ResignCommand(): base() { }

        #endregion
        #region ResignCommand(Faction)

        /// <summary>
        /// Initializes a new instance of the <see cref="ResignCommand"/> class with the specified
        /// <see cref="WorldState"/>.</summary>
        /// <param name="faction">
        /// The initial value for the <see cref="Command.Faction"/> property.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="faction"/> is a null reference.</exception>

        public ResignCommand(Faction faction): base(faction) { }

        #endregion
        #region GenerateProgram

        /// <summary>
        /// Generates and executes the HCL <see cref="Command.Program"/> for the <see
        /// cref="ResignCommand"/>.</summary>
        /// <exception cref="InvalidCommandException">
        /// The <see cref="ResignCommand"/> contains data that is invalid with respect to the
        /// current <see cref="Command.Context"/>.</exception>
        /// <exception cref="PropertyValueException">
        /// <see cref="Command.Context"/> is a null reference.</exception>
        /// <remarks>
        /// <b>GenerateProgram</b> invokes <see cref="Faction.Resign"/> on the associated <see
        /// cref="Command.Faction"/>.</remarks>

        protected override void GenerateProgram() {
            Faction.Value.Resign(this);
        }

        #endregion
    }
}
