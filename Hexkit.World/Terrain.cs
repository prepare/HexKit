using System;
using System.Diagnostics;

using Tektosyne.Collections;
using Hexkit.Scenario;
using Hexkit.World.Commands;

namespace Hexkit.World {
    #region Type Aliases

    using EntityList = KeyedList<String, Entity>;

    #endregion

    /// <summary>
    /// Represents a terrain feature that appears on a <see cref="Site"/>.</summary>
    /// <remarks><para>
    /// <b>Terrain</b> may be overridden by the rule script to define actual terrain semantics.
    /// </para><para>
    /// The default semantics allow terrains to be placed on map sites (each must contain exactly
    /// one background terrain) or reside in a faction's inventory. Placed terrains always change
    /// ownership along with their map site. They can modify the variables of placed units and of
    /// their owning faction. Unplaced terrains have no effect.</para></remarks>

    public class Terrain: Entity {
        #region Terrain(Terrain)

        /// <overloads>
        /// Initializes a new instance of the <see cref="Terrain"/> class.</overloads>
        /// <summary>
        /// Initializes a new instance of the <see cref="Terrain"/> class that is a deep copy of the
        /// specified instance.</summary>
        /// <param name="terrain">
        /// The <see cref="Terrain"/> object whose properties should be copied to the new instance.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="terrain"/> is a null reference.</exception>
        /// <remarks>
        /// This constructor is called by <see cref="Clone"/> to perform a deep copy of the
        /// specified <paramref name="terrain"/>. Please refer to <see cref="Entity(Entity)"/> for
        /// details.</remarks>

        protected Terrain(Terrain terrain): base(terrain) { }

        #endregion
        #region Terrain(TerrainClass)

        /// <summary>
        /// Initializes a new instance of the <see cref="Terrain"/> class based on the specified
        /// <see cref="Scenario.TerrainClass"/>.</summary>
        /// <param name="terrainClass">
        /// The initial value for the <see cref="TerrainClass"/> property.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="terrainClass"/> is a null reference.</exception>
        /// <remarks>
        /// Clients should use factory methods to instantiate the <see cref="Terrain"/> class,
        /// either <see cref="Entity.CreateEntity"/> or an equivalent method defined by the rule
        /// script.</remarks>

        public Terrain(TerrainClass terrainClass): base(terrainClass) { }

        #endregion
        #region CanCapture

        /// <summary>
        /// Gets a value indicating whether any sites that contain the <see cref="Terrain"/> may be
        /// captured by units.</summary>
        /// <value>
        /// The value of the <see cref="Scenario.TerrainClass.CanCapture"/> flag of the underlying
        /// <see cref="TerrainClass"/>.</value>
        /// <remarks>
        /// If <b>CanCapture</b> is <c>true</c>, any <see cref="Site"/> that contains the <see
        /// cref="Terrain"/> acquires the <see cref="Entity.Owner"/> of any occupying <see
        /// cref="Site.Units"/>, provided that this collection contains at least one element whose
        /// <see cref="Unit.CanCapture"/> flag is also <c>true</c>. Otherwise, the <see
        /// cref="Site.Owner"/> of the <see cref="Site"/> remains unchanged.</remarks>

        public bool CanCapture {
            [DebuggerStepThrough]
            get { return TerrainClass.CanCapture; }
        }

        #endregion
        #region Difficulty

        /// <summary>
        /// Gets the difficulty of crossing the <see cref="Terrain"/> in a <see
        /// cref="MoveCommand"/>.</summary>
        /// <value>
        /// The difficulty, as incremental path cost, that crossing the <see cref="Terrain"/> adds
        /// to a <see cref="MoveCommand"/>. This value is never negative.</value>
        /// <remarks><para>
        /// <b>Difficulty</b> returns one of the following values:
        /// </para><list type="bullet"><item>
        /// Zero if the <see cref="Scenario.TerrainClass.DifficultyAttribute"/> identifier of the
        /// underlying <see cref="TerrainClass"/> is an empty string.
        /// </item><item>
        /// Zero if <see cref="Scenario.TerrainClass.DifficultyAttribute"/> is a valid string that
        /// does not match any identifier in the <see cref="Entity.Attributes"/> collection.
        /// </item><item>
        /// Otherwise, the value of the <see cref="Entity.Attributes"/> element identified by <see
        /// cref="Scenario.TerrainClass.DifficultyAttribute"/>.
        /// </item></list><para>
        /// Use the <see cref="SetDifficulty"/> method to set this property, assuming it is backed
        /// by an <see cref="Entity.Attributes"/> element.</para></remarks>

        public int Difficulty {
            get {
                if (!String.IsNullOrEmpty(TerrainClass.DifficultyAttribute))
                    return Attributes.GetValue(TerrainClass.DifficultyAttribute);

                return 0;
            }
        }

        #endregion
        #region Elevation

        /// <summary>
        /// Gets the elevation of the <see cref="Terrain"/>.</summary>
        /// <value>
        /// The elevation of the <see cref="Terrain"/> compared to a scenario-specific standard,
        /// e.g. meters above sea level. This value may be negative.</value>
        /// <remarks><para>
        /// <b>Elevation</b> returns one of the following values:
        /// </para><list type="bullet"><item>
        /// Zero if the <see cref="Scenario.TerrainClass.ElevationAttribute"/> identifier of the
        /// underlying <see cref="TerrainClass"/> is an empty string.
        /// </item><item>
        /// Zero if <see cref="Scenario.TerrainClass.ElevationAttribute"/> is a valid string that
        /// does not match any identifier in the <see cref="Entity.Attributes"/> collection.
        /// </item><item>
        /// Otherwise, the value of the <see cref="Entity.Attributes"/> element identified by <see
        /// cref="Scenario.TerrainClass.ElevationAttribute"/>.
        /// </item></list><para>
        /// Use the <see cref="SetElevation"/> method to set this property, assuming it is backed by
        /// an <see cref="Entity.Attributes"/> element.</para></remarks>

        public int Elevation {
            get {
                if (!String.IsNullOrEmpty(TerrainClass.ElevationAttribute))
                    return Attributes.GetValue(TerrainClass.ElevationAttribute);

                return 0;
            }
        }

        #endregion
        #region TerrainClass

        /// <summary>
        /// Gets the scenario class of the <see cref="Terrain"/>.</summary>
        /// <value>
        /// The <see cref="Scenario.TerrainClass"/> on which the <see cref="Terrain"/> is based.
        /// </value>
        /// <remarks>
        /// <b>TerrainClass</b> returns the value of the <see cref="Entity.EntityClass"/> property,
        /// cast to type <see cref="Scenario.TerrainClass"/> for convenience.</remarks>

        public TerrainClass TerrainClass {
            [DebuggerStepThrough]
            get { return (TerrainClass) EntityClass; }
        }

        #endregion
        #region ValidateOwner

        /// <summary>
        /// Validates the specified <see cref="Faction"/> as the new value of the <see
        /// cref="Entity.Owner"/> property.</summary>
        /// <param name="faction">
        /// The <see cref="Faction"/> to validate against the invariants of the <see
        /// cref="Terrain"/> class.</param>
        /// <exception cref="InvalidCommandException">
        /// <see cref="Entity.Site"/> is valid, and its <see cref="Site.Owner"/> differs from the
        /// specified <paramref name="faction"/>. Terrains must share the owner of their site when
        /// placed.</exception>
        /// <remarks>
        /// <b>ValidateOwner</b> does nothing if validation succeeds.</remarks>

        internal override sealed void ValidateOwner(Faction faction) {
            if (Site != null && faction != Site.Owner)
                InvalidCommandException.ThrowNewWithFormat(
                    Global.Strings.ErrorOwnerTerrainConflict, Id);
        }

        #endregion
        #region ValidateSite

        /// <summary>
        /// Validates the specified <see cref="Site"/> as the new value of the <see
        /// cref="Entity.Site"/> property.</summary>
        /// <param name="site">
        /// The <see cref="Site"/> to validate against the invariants of the <see cref="Terrain"/>
        /// class.</param>
        /// <exception cref="InvalidCommandException">
        /// <paramref name="site"/> is valid, and its <see cref="Site.Owner"/> differs from the
        /// current <see cref="Entity.Owner"/>. Terrains must share the owner of their site when
        /// placed.</exception>
        /// <remarks>
        /// <b>ValidateSite</b> does nothing if validation succeeds.</remarks>

        internal override sealed void ValidateSite(Site site) {
            if (site != null && Owner != site.Owner)
                InvalidCommandException.ThrowNewWithFormat(
                    Global.Strings.ErrorSiteTerrainConflict, Id);
        }

        #endregion
        #region CheckDepletion

        /// <summary>
        /// Checks the <see cref="Terrain"/> for depletion of all its <see
        /// cref="Entity.Resources"/>.</summary>
        /// <param name="command">
        /// The <see cref="Command"/> being executed.</param>
        /// <param name="faction">
        /// The <see cref="Faction"/> to include in any message events. This argument may be a null
        /// reference.</param>
        /// <remarks>
        /// <b>CheckDepletion</b> calls the base class implementation of <see
        /// cref="Entity.CheckDepletion"/>, but only if <see
        /// cref="Scenario.TerrainClass.IsBackground"/> is <c>false</c> for the underlying <see
        /// cref="TerrainClass"/>. This prevents deletion of background terrains.</remarks>

        public override void CheckDepletion(Command command, Faction faction) {
            if (!TerrainClass.IsBackground)
                base.CheckDepletion(command, faction);
        }

        #endregion
        #region OnSiteChanged

        /// <summary>
        /// Executes when the <see cref="Entity.Site"/> property has changed.</summary>
        /// <param name="command">
        /// The <see cref="Command"/> being executed.</param>
        /// <param name="oldSite">
        /// The value of the <see cref="Entity.Site"/> property before it was changed.</param>
        /// <remarks><para>
        /// <b>OnSiteChanged</b> performs the following actions:
        /// </para><list type="number"><item>
        /// Call the base class implementation of <see cref="Entity.OnSiteChanged"/>.
        /// </item><item>
        /// Invoke <see cref="Site.SetTerrainChanged"/> on the specified <paramref name="oldSite"/>,
        /// if valid.
        /// </item><item>
        /// Invoke <see cref="Site.SetTerrainChanged"/> on the new <see cref="Entity.Site"/>, if
        /// valid.</item></list></remarks>

        public override void OnSiteChanged(Command command, Site oldSite) {
            base.OnSiteChanged(command, oldSite);

            // notify old site of terrain change
            if (oldSite != null)
                oldSite.SetTerrainChanged(command.Context.WorldState);

            // notify new site of terrain change
            if (Site != null)
                Site.SetTerrainChanged(command.Context.WorldState);
        }

        #endregion
        #region OnVariableChanged

        /// <summary>
        /// Executes when a <see cref="Variable"/> value of the <see cref="Terrain"/> has changed.
        /// </summary>
        /// <param name="command">
        /// The <see cref="Command"/> being executed.</param>
        /// <param name="variableClass">
        /// The <see cref="VariableClass"/> whose instance value has changed.</param>
        /// <param name="value">
        /// The new instance value of the specified <paramref name="variableClass"/>.</param>
        /// <param name="isModifier">
        /// <c>true</c> if a modifier value of the specified <parmaref name="variableClass"/> has
        /// changed; <c>false</c> if a basic value has changed.</param>
        /// <remarks><para>
        /// <b>OnVariableChanged</b> performs the following actions:
        /// </para><list type="number"><item>
        /// Call the base class implementation of <see cref="Entity.OnVariableChanged"/>.
        /// </item><item>
        /// Invoke <see cref="Site.SetTerrainChanged"/> on the current <see cref="Entity.Site"/>,
        /// if valid.</item></list></remarks>

        public override void OnVariableChanged(Command command,
            VariableClass variableClass, int value, bool isModifier) {

            base.OnVariableChanged(command, variableClass, value, isModifier);

            // notify site of terrain change
            if (Site != null)
                Site.SetTerrainChanged(command.Context.WorldState);
        }

        #endregion
        #region SetDifficulty

        /// <summary>
        /// Sets the <see cref="Difficulty"/> property to the specified value.</summary>
        /// <param name="command">
        /// The <see cref="Command"/> being executed.</param>
        /// <param name="value">
        /// The new value for the <see cref="Difficulty"/> property.</param>
        /// <exception cref="InvalidCommandException">
        /// The <see cref="Scenario.TerrainClass.DifficultyAttribute"/> identifier of the underlying
        /// <see cref="TerrainClass"/> is not a valid variable identifier.</exception>
        /// <remarks><para>
        /// <b>SetDifficulty</b> sets the <see cref="Entity.Attributes"/> element identified by <see
        /// cref="Scenario.TerrainClass.DifficultyAttribute"/> to the specified <paramref
        /// name="value"/>. This effectively changes the value of the <see cref="Difficulty"/>
        /// property which is backed by that <see cref="Entity.Attributes"/> element.
        /// </para><para>
        /// <b>SetDifficulty</b> sets the <see cref="Variable.InitialValue"/> of the backing <see
        /// cref="Variable"/> to the specified <paramref name="value"/>. This also recalculates its
        /// current <see cref="Variable.Value"/> using all applicable modifiers.</para></remarks>

        public void SetDifficulty(Command command, int value) {
            command.SetEntityVariableInitial(Id, TerrainClass.DifficultyAttribute, value);
        }

        #endregion
        #region SetElevation

        /// <summary>
        /// Sets the <see cref="Elevation"/> property to the specified value.</summary>
        /// <param name="command">
        /// The <see cref="Command"/> being executed.</param>
        /// <param name="value">
        /// The new value for the <see cref="Elevation"/> property.</param>
        /// <exception cref="InvalidCommandException">
        /// The <see cref="Scenario.TerrainClass.ElevationAttribute"/> identifier of the underlying
        /// <see cref="TerrainClass"/> is not a valid variable identifier.</exception>
        /// <remarks><para>
        /// <b>SetElevation</b> sets the <see cref="Entity.Attributes"/> element identified by <see
        /// cref="Scenario.TerrainClass.ElevationAttribute"/> to the specified <paramref
        /// name="value"/>. This effectively changes the value of the <see cref="Elevation"/>
        /// property which is backed by that <see cref="Entity.Attributes"/> element.
        /// </para><para>
        /// <b>SetElevation</b> sets the <see cref="Variable.InitialValue"/> of the backing <see
        /// cref="Variable"/> to the specified <paramref name="value"/>. This also recalculates its
        /// current <see cref="Variable.Value"/> using all applicable modifiers.</para></remarks>

        public void SetElevation(Command command, int value) {
            command.SetEntityVariableInitial(Id, TerrainClass.ElevationAttribute, value);
        }

        #endregion
        #region ICloneable Members

        /// <summary>
        /// Creates a new <see cref="Terrain"/> object that is a deep copy of the current instance.
        /// </summary>
        /// <returns>
        /// A new <see cref="Terrain"/> object that is a deep copy of the current instance.
        /// </returns>
        /// <remarks>
        /// <b>Clone</b> calls the <see cref="Terrain(Terrain)"/> copy constructor with this <see
        /// cref="Terrain"/> object.</remarks>

        public override object Clone() {
            return new Terrain(this);
        }

        #endregion
    }
}
