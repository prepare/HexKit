using System;

using Tektosyne;
using Tektosyne.Geometry;
using Hexkit.Global;

namespace Hexkit.World.Instructions {

    /// <summary>
    /// Sets the <see cref="Entity.Site"/> of an <see cref="Entity"/>.</summary>
    /// <remarks>
    /// <b>SetEntitySiteInstruction</b> is serialized to the XML element "SetEntitySite" defined in
    /// <see cref="FilePaths.SessionSchema"/>.</remarks>

    public sealed class SetEntitySiteInstruction: PointInstruction {
        #region SetEntitySiteInstruction()

        /// <overloads>
        /// Initializes a new instance of the <see cref="SetEntitySiteInstruction"/> class.
        /// </overloads>
        /// <summary>
        /// Initializes a new instance of the <see cref="SetEntitySiteInstruction"/> class with
        /// default properties.</summary>

        internal SetEntitySiteInstruction(): base() { }

        #endregion
        #region SetEntitySiteInstruction(String, PointI)

        /// <summary>
        /// Initializes a new instance of the <see cref="SetEntitySiteInstruction"/> class with the
        /// specified <see cref="Entity"/> identifier and map location.</summary>
        /// <param name="entityId">
        /// The initial value for the <see cref="Instruction.Id"/> property, indicating the <see
        /// cref="Entity.Id"/> string of the <see cref="Entity"/> to manipulate.</param>
        /// <param name="location">
        /// The initial value for the <see cref="PointInstruction.Location"/> property, indicating
        /// the <see cref="Site.Location"/> of the new value for the <see cref="Entity.Site"/>
        /// property of the specified <see cref="Entity"/>.</param>
        /// <remarks>
        /// Specify an invalid <paramref name="location"/> to set the <see cref="Entity.Site"/>
        /// property of the indicated <see cref="Entity"/> to a null reference.</remarks>

        internal SetEntitySiteInstruction(string entityId, PointI location):
            base(entityId, location) { }

        #endregion
        #region Execute

        /// <summary>
        /// Executes the <see cref="Instruction"/> on the specified <see cref="WorldState"/> and
        /// indicates whether the <see cref="WorldState"/> has changed.</summary>
        /// <param name="worldState">
        /// The <see cref="WorldState"/> on which to execute the <see
        /// cref="SetEntitySiteInstruction"/>.</param>
        /// <returns>
        /// <c>true</c> if execution of the <see cref="SetEntitySiteInstruction"/> has changed the
        /// data of the specified <paramref name="worldState"/>; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="worldState"/> is a null reference.</exception>
        /// <exception cref="InvalidCommandException">
        /// The <see cref="SetEntitySiteInstruction"/> contains data that is invalid with respect to
        /// the specified <paramref name="worldState"/>.</exception>
        /// <remarks><para>
        /// <b>Execute</b> performs the following actions:
        /// </para><list type="number"><item>
        /// Set the <see cref="InstructionResults.Entity"/> property of the <see
        /// cref="Instruction.Results"/> cache to the <see cref="Entity"/> identified by the <see
        /// cref="Instruction.Id"/> property.
        /// </item><item>
        /// Set the <see cref="InstructionResults.Site"/> property of the <b>Results</b> cache to
        /// the <em>original</em> <see cref="Entity.Site"/> of the <b>Entity</b>.
        /// </item><item>
        /// Invoke <see cref="Entity.UpdateModifierMaps"/> on the <see cref="Entity"/> to remove its
        /// effects from all modifier maps in the specified <paramref name="worldState"/>.
        /// </item><item>
        /// Set the <see cref="Entity.Site"/> property of the <see cref="Entity"/> to the <see
        /// cref="Site"/> indicated by the <see cref="PointInstruction.Location"/> property, or to a
        /// null reference if <b>Location</b> contains invalid coordinates.
        /// </item><item>
        /// Again invoke <see cref="Entity.UpdateModifierMaps"/> on the <see cref="Entity"/> to add
        /// its effects to all modifier maps in the specified <paramref name="worldState"/>.
        /// </item></list></remarks>

        internal override bool Execute(WorldState worldState) {
            if (worldState == null)
                ThrowHelper.ThrowArgumentNullException("worldState");

            // GetEntity throws exception for invalid Id
            Entity entity = GetEntity(worldState, Id);

            // GetSite returns null for invalid Location
            Site site = worldState.GetSite(Location);

            // return entity and original site
            if (Results != null) {
                Results.Entity = entity;
                Results.Site = entity.Site;
            }

            if (entity.Site == site)
                return false;

            entity.UpdateModifierMaps(worldState, false);
            entity.SetSite(site, true);
            entity.UpdateModifierMaps(worldState, true);

            return true;
        }

        #endregion
    }
}
