using System;

using Tektosyne;
using Tektosyne.Geometry;

using Hexkit.Global;
using Hexkit.Scenario;

namespace Hexkit.World.Instructions {

    /// <summary>
    /// Sets the <see cref="Entity.Owner"/> of all <see cref="Site.Units"/> on a specific <see
    /// cref="Site"/>.</summary>
    /// <remarks>
    /// <b>SetSiteUnitOwnerInstruction</b> is serialized to the XML element "SetSiteUnitOwner"
    /// defined in <see cref="FilePaths.SessionSchema"/>.</remarks>

    public sealed class SetSiteUnitOwnerInstruction: PointInstruction {
        #region SetSiteUnitOwnerInstruction()

        /// <overloads>
        /// Initializes a new instance of the <see cref="SetSiteUnitOwnerInstruction"/> class.
        /// </overloads>
        /// <summary>
        /// Initializes a new instance of the <see cref="SetSiteUnitOwnerInstruction"/> class with
        /// default properties.</summary>

        internal SetSiteUnitOwnerInstruction(): base() { }

        #endregion
        #region SetSiteUnitOwnerInstruction(PointI, String)

        /// <summary>
        /// Initializes a new instance of the <see cref="SetSiteUnitOwnerInstruction"/> class with
        /// the specified map location and <see cref="Faction"/> identifier.</summary>
        /// <param name="location">
        /// The initial value for the <see cref="PointInstruction.Location"/> property, indicating
        /// the <see cref="Site.Location"/> of the <see cref="Site"/> to manipulate.</param>
        /// <param name="factionId">
        /// The initial value for the <see cref="Instruction.Id"/> property, indicating the <see
        /// cref="Faction.Id"/> string of the new value for the <see cref="Entity.Owner"/> property
        /// of all <see cref="Site.Units"/> contained in the specified <see cref="Site"/>.</param>
        /// <remarks>
        /// Specify a null reference or an empty string for <paramref name="factionId"/> to set the
        /// <see cref="Entity.Owner"/> property of all <see cref="Site.Units"/> contained in the
        /// indicated <see cref="Site"/> to a null reference.</remarks>

        internal SetSiteUnitOwnerInstruction(PointI location, string factionId):
            base(factionId, location) { }

        #endregion
        #region Execute

        /// <summary>
        /// Executes the <see cref="Instruction"/> on the specified <see cref="WorldState"/> and
        /// indicates whether the <see cref="WorldState"/> has changed.</summary>
        /// <param name="worldState">
        /// The <see cref="WorldState"/> on which to execute the <see
        /// cref="SetSiteUnitOwnerInstruction"/>.</param>
        /// <returns>
        /// <c>true</c> if execution of the <see cref="SetSiteUnitOwnerInstruction"/> has changed
        /// the data of the specified <paramref name="worldState"/>; otherwise, <c>false</c>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="worldState"/> is a null reference.</exception>
        /// <exception cref="InvalidCommandException">
        /// The <see cref="SetSiteUnitOwnerInstruction"/> contains data that is invalid with respect
        /// to the specified <paramref name="worldState"/>.</exception>
        /// <remarks><para>
        /// <b>Execute</b> performs the following actions:
        /// </para><list type="number"><item>
        /// Set the <see cref="InstructionResults.Site"/> property of the <see
        /// cref="Instruction.Results"/> cache to the <see cref="Site"/> identified by the <see
        /// cref="PointInstruction.Location"/> property.
        /// </item><item>
        /// Set the <see cref="InstructionResults.Faction"/> property of the <b>Results</b> cache to
        /// the <em>original</em> <see cref="Entity.Owner"/> of the first <see cref="Site.Units"/>
        /// element, if any, contained in the indicated <see cref="Site"/>.
        /// </item><item>
        /// Invoke <see cref="Entity.UpdateModifierMaps"/> on all <see cref="Site.Units"/> contained
        /// in the <see cref="Site"/> to remove their effects from all modifier maps in the
        /// specified <paramref name="worldState"/>.
        /// </item><item>
        /// Set the <see cref="Entity.Owner"/> property of all <see cref="Site.Units"/> contained in
        /// the <see cref="Site"/> to the <see cref="Faction"/> indicated by the <see
        /// cref="Instruction.Id"/> property, or to a null reference if <b>Id</b> is a null
        /// reference or an empty string.
        /// </item><item>
        /// Again invoke <see cref="Entity.UpdateModifierMaps"/> on all <see cref="Site.Units"/>
        /// contained in the <see cref="Site"/> to add their effects to all modifier maps in the
        /// specified <paramref name="worldState"/>.</item></list></remarks>

        internal override bool Execute(WorldState worldState) {
            if (worldState == null)
                ThrowHelper.ThrowArgumentNullException("worldState");

            // GetSite throws exception for invalid Location
            Site site = GetSite(worldState, Location);
            Faction faction = null;

            // GetFaction throws exception for invalid Id
            if (!String.IsNullOrEmpty(Id))
                faction = GetFaction(worldState, Id);

            // return site and original unit owner
            if (Results != null) {
                Results.Site = site;
                if (site.Units.Count > 0)
                    Results.Faction = site.Units[0].Owner;
            }

            // remove effects from modifier maps
            for (int i = 0; i < site.Units.Count; i++)
                site.Units[i].UpdateModifierMaps(worldState, false);

            bool result = site.SetEntityOwner(EntityCategory.Unit, faction);

            // add effects to modifier maps
            for (int i = 0; i < site.Units.Count; i++)
                site.Units[i].UpdateModifierMaps(worldState, true);

            return result;
        }

        #endregion
    }
}
