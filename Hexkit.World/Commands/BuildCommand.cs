using System;
using System.Collections.Generic;

using Tektosyne;
using Hexkit.Global;
using Hexkit.Scenario;

namespace Hexkit.World.Commands {

    /// <summary>
    /// Represents a "Build" command.</summary>
    /// <remarks><para>
    /// <b>BuildCommand</b> adds the <see cref="Command.Entities"/> command parameter to the
    /// functionality provided by the <see cref="ClassesCommand"/> class.
    /// </para><para>
    /// <b>Entities</b> is an <em>output</em> argument which is neither provided by clients nor
    /// serialized to session XML files. Instead, <see cref="BuildCommand.GenerateProgram"/> sets
    /// this property to a collection of all <see cref="Entity"/> objects created by the command.
    /// </para><para>
    /// <b>BuildCommand</b> is serialized to the XML element "Build" defined in <see
    /// cref="FilePaths.SessionSchema"/>.</para></remarks>

    public sealed class BuildCommand: ClassesCommand {
        #region BuildCommand()

        /// <overloads>
        /// Initializes a new instance of the <see cref="BuildCommand"/> class.</overloads>
        /// <summary>
        /// Initializes a new instance of the <see cref="BuildCommand"/> class with default
        /// properties.</summary>

        internal BuildCommand(): base() { }

        #endregion
        #region BuildCommand(Faction, String[])

        /// <summary>
        /// Initializes a new instance of the <see cref="BuildCommand"/> class with the specified
        /// <see cref="WorldState"/> and <see cref="EntityClass"/> identifiers.</summary>
        /// <param name="faction">
        /// The initial value for the <see cref="Command.Faction"/> property.</param>
        /// <param name="classIds">
        /// The initial values for the <b>Keys</b> of the <see cref="ClassesCommand.EntityClasses"/>
        /// collection.</param>
        /// <exception cref="ArgumentException">
        /// <paramref name="classIds"/> contains a null reference or an empty string.</exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="faction"/> is a null reference.</exception>
        /// <exception cref="ArgumentNullOrEmptyException">
        /// <paramref name="classIds"/> is a null reference or an empty array.</exception>

        public BuildCommand(Faction faction, string[] classIds): base(faction, classIds) { }

        #endregion
        #region GenerateProgram

        /// <summary>
        /// Generates and executes the HCL <see cref="Command.Program"/> for the <see
        /// cref="BuildCommand"/>.</summary>
        /// <exception cref="InvalidCommandException">
        /// The <see cref="BuildCommand"/> contains data that is invalid with respect to the current
        /// <see cref="Command.Context"/>.</exception>
        /// <exception cref="PropertyValueException">
        /// <see cref="Command.Context"/> is a null reference.</exception>
        /// <remarks>
        /// <b>GenerateProgram</b> invokes <see cref="Faction.Build"/> on the associated <see
        /// cref="Command.Faction"/>, and then stores the returned <see cref="Entity"/> objects in
        /// the <see cref="Command.Entities"/> array.</remarks>

        protected override void GenerateProgram() {
            IList<Entity> entities = Faction.Value.Build(this);
            Entities = EntityReference.CreateArray(entities);
        }

        #endregion
        #region Validate

        /// <summary>
        /// Validates the current data of the <see cref="BuildCommand"/> against the specified <see
        /// cref="WorldState"/>.</summary>
        /// <param name="worldState">
        /// The <see cref="WorldState"/> to validate the <see cref="BuildCommand"/> against.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="worldState"/> is a null reference.</exception>
        /// <exception cref="InvalidCommandException">
        /// One of the conditions listed under <see cref="ClassesCommand.Validate"/>.</exception>
        /// <remarks>
        /// Please refer to <see cref="ClassesCommand.Validate"/> for details. This override
        /// performs additional validation specific to the <see cref="BuildCommand"/>.</remarks>

        public override void Validate(WorldState worldState) {
            base.Validate(worldState);

            Faction faction = Faction.Value;

            for (int i = 0; i < EntityClasses.Count; i++) {
                EntityClass entityClass = EntityClasses.GetByIndex(i);

                if (faction.GetBuildCount(worldState, entityClass) == 0)
                    InvalidCommandException.ThrowNewWithFormat(
                        Global.Strings.CommandEntityClassBuild, Name, entityClass.Id, faction.Id);
            }
        }

        #endregion
    }
}
