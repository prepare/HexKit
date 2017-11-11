using System;

using Tektosyne;
using Tektosyne.Geometry;
using Hexkit.Global;

namespace Hexkit.World.Instructions {

    /// <summary>
    /// Sets the <see cref="Site.Owner"/> of a <see cref="Site"/>.</summary>
    /// <remarks>
    /// <b>SetSiteOwnerInstruction</b> is serialized to the XML element "SetSiteOwner" defined in
    /// <see cref="FilePaths.SessionSchema"/>.</remarks>

    public sealed class SetSiteOwnerInstruction: PointInstruction {
        #region SetSiteOwnerInstruction()

        /// <overloads>
        /// Initializes a new instance of the <see cref="SetSiteOwnerInstruction"/> class.
        /// </overloads>
        /// <summary>
        /// Initializes a new instance of the <see cref="SetSiteOwnerInstruction"/> class with
        /// default properties.</summary>

        internal SetSiteOwnerInstruction(): base() { }

        #endregion
        #region SetSiteOwnerInstruction(PointI, String)

        /// <summary>
        /// Initializes a new instance of the <see cref="SetSiteOwnerInstruction"/> class with the
        /// specified map location and <see cref="Faction"/> identifier.</summary>
        /// <param name="location">
        /// The initial value for the <see cref="PointInstruction.Location"/> property, indicating
        /// the <see cref="Site.Location"/> of the <see cref="Site"/> to manipulate.</param>
        /// <param name="factionId">
        /// The initial value for the <see cref="Instruction.Id"/> property, indicating the <see
        /// cref="Faction.Id"/> string of the new value for the <see cref="Site.Owner"/> property of
        /// the specified <see cref="Site"/>.</param>
        /// <remarks>
        /// Specify a null reference or an empty string for <paramref name="factionId"/> to set the
        /// <see cref="Site.Owner"/> property of the indicated <see cref="Site"/> to a null
        /// reference.</remarks>

        internal SetSiteOwnerInstruction(PointI location, string factionId):
            base(factionId, location) { }

        #endregion
        #region Execute

        /// <summary>
        /// Executes the <see cref="Instruction"/> on the specified <see cref="WorldState"/> and
        /// indicates whether the <see cref="WorldState"/> has changed.</summary>
        /// <param name="worldState">
        /// The <see cref="WorldState"/> on which to execute the <see
        /// cref="SetSiteOwnerInstruction"/>.</param>
        /// <returns>
        /// <c>true</c> if execution of the <see cref="SetSiteOwnerInstruction"/> has changed the
        /// data of the specified <paramref name="worldState"/>; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="worldState"/> is a null reference.</exception>
        /// <exception cref="InvalidCommandException">
        /// The <see cref="SetSiteOwnerInstruction"/> contains data that is invalid with respect to
        /// the specified <paramref name="worldState"/>.</exception>
        /// <remarks><para>
        /// <b>Execute</b> performs the following actions:
        /// </para><list type="number"><item>
        /// Set the <see cref="InstructionResults.Site"/> property of the <see
        /// cref="Instruction.Results"/> cache to the <see cref="Site"/> identified by the <see
        /// cref="PointInstruction.Location"/> property.
        /// </item><item>
        /// Set the <see cref="InstructionResults.Faction"/> property of the <b>Results</b> cache to
        /// the <em>original</em> <see cref="Site.Owner"/> of the <b>Site</b>.
        /// </item><item>
        /// Invoke <see cref="Entity.UpdateModifierMaps"/> on all <see cref="Site.Terrains"/> and
        /// <see cref="Site.Effects"/> contained in the <see cref="Site"/> to remove their influence
        /// from all modifier maps in the specified <paramref name="worldState"/>.
        /// </item><item>
        /// Set the <see cref="Site.Owner"/> property of the <b>Site</b> to the <see
        /// cref="Faction"/> indicated by the <see cref="Instruction.Id"/> property, or to a null
        /// reference if <b>Id</b> is a null reference or an empty string.
        /// </item><item>
        /// Again invoke <see cref="Entity.UpdateModifierMaps"/> on all <see cref="Site.Terrains"/>
        /// and <see cref="Site.Effects"/> contained in the <see cref="Site"/> to add their
        /// influence to all modifier maps in the specified <paramref name="worldState"/>.
        /// </item></list></remarks>

        internal override bool Execute(WorldState worldState) {
            if (worldState == null)
                ThrowHelper.ThrowArgumentNullException("worldState");

            // GetSite throws exception for invalid Location
            Site site = GetSite(worldState, Location);
            Faction faction = null;

            // GetFaction throws exception for invalid Id
            if (!String.IsNullOrEmpty(Id))
                faction = GetFaction(worldState, Id);

            // return site and original owner
            if (Results != null) {
                Results.Site = site;
                Results.Faction = site.Owner;
            }

            if (site.Owner == faction)
                return false;

            // remove influence from modifier maps
            for (int i = 0; i < site.Terrains.Count; i++)
                site.Terrains[i].UpdateModifierMaps(worldState, false);
            for (int i = 0; i < site.Effects.Count; i++)
                site.Effects[i].UpdateModifierMaps(worldState, false);

            site.SetOwner(faction);

            // add influence to modifier maps
            for (int i = 0; i < site.Terrains.Count; i++)
                site.Terrains[i].UpdateModifierMaps(worldState, true);
            for (int i = 0; i < site.Effects.Count; i++)
                site.Effects[i].UpdateModifierMaps(worldState, true);

            return true;
        }

        #endregion
    }
}
