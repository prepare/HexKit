using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;

using Tektosyne;
using Tektosyne.Collections;
using Tektosyne.Geometry;

using Hexkit.Global;
using Hexkit.Scenario;
using Hexkit.World.Commands;

namespace Hexkit.World {
    #region Type Aliases

    using EntityList = KeyedList<String, Entity>;
    using SiteList = KeyedList<PointI, Site>;

    #endregion

    /// <summary>
    /// Represents a map site with a fixed location that contains one or more entities.</summary>
    /// <remarks><para>
    /// <b>Site</b> represents an atomic location on the game map, equivalent to one polygonal <see
    /// cref="PolygonGrid.Element"/> in the <see cref="AreaSection.MapGrid"/>. All <see
    /// cref="Entity"/> objects that are placed on the map are associated with a <b>Site</b>.
    /// </para><para>
    /// The ownership of a <b>Site</b> changes by placing a unit with the <see
    /// cref="Unit.CanCapture"/> ability on the area, which is only possible if the site does not
    /// contain any units with a different <see cref="Entity.Owner"/> property. Units with different
    /// owners cannot co-exist on the same <b>Site</b>.
    /// </para><para>
    /// <b>Site</b> objects retain their ownership even after all occupying units have moved away,
    /// until the site is captured by enemy units. As always, the rule script may override these
    /// mechanics.</para></remarks>

    public class Site: ICloneable, IKeyedValue<PointI>, IValuable {
        #region Site(PointI)

        /// <overloads>
        /// Initializes a new instance of the <see cref="Site"/> class.</overloads>
        /// <summary>
        /// Initializes a new instance of the <see cref="Site"/> class with the specified map
        /// location.</summary>
        /// <param name="location">
        /// The initial value for the <see cref="Location"/> property.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="location"/> specifies one or two negative coordinates.</exception>
        /// <remarks>
        /// The remaining properties of the new instance of the <see cref="Site"/> class are
        /// initialized to null references or empty collections, as appropriate.</remarks>

        internal Site(PointI location) {

            // all sites must have a valid location
            if (location.X < 0 || location.Y < 0)
                ThrowHelper.ThrowArgumentOutOfRangeException(
                    "location", location, Tektosyne.Strings.ArgumentContainsNegative);

            this._location = location;
            TerrainChanged = true;
#if DEBUG
            // ensure unique elements in debug mode
            const bool isUnique = true;
#else
            const bool isUnique = false;
#endif
            // create collections for local objects
            this._units = new EntityList(0, isUnique);
            this._terrains = new EntityList(2, isUnique);
            this._effects = new EntityList(0, isUnique);
        }

        #endregion
        #region Site(Site)

        /// <summary>
        /// Initializes a new instance of the <see cref="Site"/> class that is a deep copy of the
        /// specified instance.</summary>
        /// <param name="site">
        /// The <see cref="Site"/> object whose properties should be copied to the new instance.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="site"/> is a null reference.</exception>
        /// <remarks>
        /// This constructor is called by <see cref="Clone"/> to perform a deep copy of the
        /// specified <paramref name="site"/>. Please refer to that method for further details.
        /// </remarks>

        private Site(Site site) {
            if (site == null)
                ThrowHelper.ThrowArgumentNullException("site");

            this._difficulty = site._difficulty;
            this._elevation = site._elevation;
            this._location = site._location;
            TerrainChanged = site.TerrainChanged;
            this._valuation = site._valuation;

            this._units = Entity.CopyCollection(site._units, this);
            this._terrains = Entity.CopyCollection(site._terrains, this);
            this._effects = Entity.CopyCollection(site._effects, this);
        }

        #endregion
        #region Private Fields

        // property backers
        private readonly PointI _location = InvalidLocation;
        private readonly EntityList _units, _terrains, _effects;

        // cached while TerrainChanged is false
        private int _difficulty, _elevation;
        private double _valuation;

        #endregion
        #region InvalidLocation

        /// <summary>
        /// Represents invalid map coordinates.</summary>
        /// <remarks><para>
        /// <b>InvalidLocation</b> holds a <see cref="PointI"/> value whose <see cref="PointI.X"/>
        /// and <see cref="PointI.Y"/> components are both -1. This is the same value as the <see
        /// cref="PolygonGrid.InvalidLocation"/> field of the <see cref="PolygonGrid"/> class.
        /// </para><para>
        /// Hexkit interprets any map location with one or two negative coordinates as invalid. 
        /// Clients should use either this read-only field or the <see cref="PolygonGrid"/>
        /// equivalent to indicate the absence of a valid <see cref="Site"/>.</para></remarks>

        public static readonly PointI InvalidLocation = PolygonGrid.InvalidLocation;

        #endregion
        #region Internal Properties
        #region TerrainChanged

        /// <summary>
        /// Gets a value indicating whether any <see cref="Terrains"/> have changed.</summary>
        /// <value>
        /// <c>true</c> if the <see cref="Terrains"/> collection has changed, including any changes
        /// to the data of individual <see cref="Terrain"/> objects; otherwise, <c>false</c>. The
        /// default is <c>true</c>.</value>
        /// <remarks><para>
        /// When a terrain-dependent value is requested while <b>TerrainChanged</b> is <c>true</c>,
        /// all such values are recalculated and the property is reset to <c>false</c>.
        /// </para><para>
        /// When a terrain-dependent value is requested while <b>TerrainChanged</b> is <c>false</c>,
        /// the buffered result of the last calculation is returned instead.
        /// </para><para>
        /// Call <see cref="SetTerrainChanged"/> to set this property to <c>true</c>.
        /// </para></remarks>

        internal bool TerrainChanged { get; private set; }

        #endregion
        #region WritableEffects

        /// <summary>
        /// Gets a writable list of all <see cref="Effects"/> placed on the <see cref="Site"/>.
        /// </summary>
        /// <value>
        /// The collection that is backing the <see cref="Effects"/> property.</value>

        internal EntityList WritableEffects {
            [DebuggerStepThrough]
            get { return this._effects; }
        }

        #endregion
        #region WritableTerrains

        /// <summary>
        /// Gets a writable list of all <see cref="Terrains"/> placed on the <see cref="Site"/>.
        /// </summary>
        /// <value>
        /// The collection that is backing the <see cref="Terrains"/> property.</value>

        internal EntityList WritableTerrains {
            [DebuggerStepThrough]
            get { return this._terrains; }
        }

        #endregion
        #region WritableUnits

        /// <summary>
        /// Gets a writable list of all <see cref="Units"/> placed on the <see cref="Site"/>.
        /// </summary>
        /// <value>
        /// The collection that is backing the <see cref="Units"/> property.</value>

        internal EntityList WritableUnits {
            [DebuggerStepThrough]
            get { return this._units; }
        }

        #endregion
        #endregion
        #region Public Properties
        #region BlocksAttack

        /// <summary>
        /// Gets a value indicating whether any <see cref="Entity"/> on the <see cref="Site"/>
        /// obstructs the line of sight for ranged attacks.</summary>
        /// <value>
        /// <c>true</c> if any present <see cref="Entity"/> obstructs the line of sight for ranged
        /// attacks; otherwise, <c>false</c>.</value>
        /// <remarks>
        /// <b>BlocksAttack</b> returns <c>true</c> exactly if <see cref="Entity.BlocksAttack"/> is
        /// <c>true</c> for at least one element in the <see cref="Terrains"/>, <see cref="Units"/>,
        /// or <see cref="Effects"/> collection.</remarks>

        public bool BlocksAttack {
            get {
                /*
                 * We don't use EntitySection.AllCategories here because checking
                 * Terrains before Units should result in the earliest returns.
                 */

                for (int i = 0; i < Terrains.Count; i++)
                    if (Terrains[i].BlocksAttack) return true;

                for (int i = 0; i < Units.Count; i++)
                    if (Units[i].BlocksAttack) return true;

                for (int i = 0; i < Effects.Count; i++)
                    if (Effects[i].BlocksAttack) return true;

                return false;
            }
        }

        #endregion
        #region CanCapture

            /// <summary>
            /// Gets a value indicating whether the <see cref="Site"/> may be captured by units.
            /// </summary>
            /// <value>
            /// <c>true</c> if at least one element in the <see cref="Terrains"/> collection has the
            /// <see cref="Terrain.CanCapture"/> ability; otherwise, <c>false</c>.</value>
            /// <remarks>
            /// If <b>CanCapture</b> is <c>true</c>, the <see cref="Site"/> acquires the <see
            /// cref="Entity.Owner"/> of any occupying <see cref="Units"/>, provided that this contains
            /// at least one element whose <see cref="Unit.CanCapture"/> flag is <c>true</c>. Otherwise,
            /// the current <see cref="Owner"/> remains unchanged.</remarks>

            public bool CanCapture {
            get {
                for (int i = 0; i < Terrains.Count; i++)
                    if (((Terrain) Terrains[i]).CanCapture)
                        return true;

                return false;
            }
        }

        #endregion
        #region Difficulty

        /// <summary>
        /// Gets the total difficulty of moving across the <see cref="Site"/>.</summary>
        /// <value>
        /// The difficulty, as incremental path cost, that crossing the <see cref="Site"/> adds to a
        /// <see cref="MoveCommand"/>. This value is never negative.</value>
        /// <remarks><para>
        /// <b>Difficulty</b> returns the sum of all <see cref="Terrain.Difficulty"/> values for all
        /// elements in the <see cref="Terrains"/> collection.
        /// </para><para>
        /// <b>Difficulty</b> only recalculates the total difficulty if <see cref="TerrainChanged"/>
        /// is <c>true</c>, and returns a buffered value otherwise.
        /// </para><note type="caution">
        /// <b>Difficulty</b> may return zero, but pathfinding algorithms like A* require non-zero
        /// step costs. When <b>Difficulty</b> returns zero in this context, you must either
        /// substitute a positive value or end the current path.</note></remarks>

        public int Difficulty {
            [DebuggerStepThrough]
            get {
                CheckTerrainChanged();
                return this._difficulty;
            }
        }

        #endregion
        #region Distance

        /// <summary>
        /// Gets or sets the distance between the current <see cref="Location"/> and an arbitrary
        /// map location.</summary>
        /// <value>
        /// An <see cref="Int32"/> value indicating the distance between the current <see
        /// cref="Location"/> and an arbitrary map location. The default is zero.</value>
        /// <remarks>
        /// <b>Distance</b> is not accessed by the <see cref="Site"/> class, and is not required for
        /// managing the <see cref="WorldState"/>. Use this property to temporarily store arbitrary
        /// values, such as the distance from a source site during pathfinding.</remarks>

        public int Distance { get; set; }

        #endregion
        #region Effects

        /// <summary>
        /// Gets a list of all effects placed on the <see cref="Site"/>.</summary>
        /// <value>
        /// A read-only <see cref="EntityList"/> containing all <see cref="Effect"/> objects placed
        /// on the <see cref="Site"/>. The default is an empty collection.</value>
        /// <remarks><para>
        /// <b>Effects</b> never returns a null reference, and its elements are never null
        /// references. All keys and elements are unique. Use the <see cref="Entity.Site"/> property
        /// to add or remove <see cref="Effect"/> objects.
        /// </para><para>
        /// Entities are stored in the order in which they are overlaid on the map view, with the
        /// highest index on top.</para></remarks>

        public EntityList Effects {
            [DebuggerStepThrough]
            get { return this._effects.AsReadOnly(); }
        }

        #endregion
        #region Elevation

        /// <summary>
        /// Gets the total elevation of the <see cref="Site"/>.</summary>
        /// <value>
        /// The elevation of the <see cref="Site"/> compared to a scenario-specific standard, e.g.
        /// meters above sea level. This value may be negative.</value>
        /// <remarks><para>
        /// <b>Elevation</b> returns the sum of all <see cref="Terrain.Elevation"/> values for all
        /// elements in the <see cref="Terrains"/> collection.
        /// </para><para>
        /// <b>Elevation</b> only recalculates the total elevation if <see cref="TerrainChanged"/>
        /// is <c>true</c>, and returns a buffered value otherwise.</para></remarks>

        public int Elevation {
            [DebuggerStepThrough]
            get {
                CheckTerrainChanged();
                return this._elevation;
            }
        }

        #endregion
        #region Location

        /// <summary>
        /// Gets the map coordinates of the <see cref="Site"/>.</summary>
        /// <value>
        /// A <see cref="PointI"/> value indicating the coordinates of the <see cref="Site"/> within
        /// the <see cref="AreaSection.MapGrid"/>.</value>
        /// <remarks>
        /// <b>Location</b> always returns a <see cref="PointI"/> whose <see cref="PointI.X"/> and
        /// <see cref="PointI.Y"/> coordinates are both non-negative and within the corresponding
        /// dimension of the size of the <see cref="AreaSection.MapGrid"/>. This property never
        /// changes once the object has been constructed.</remarks>

        public PointI Location {
            [DebuggerStepThrough]
            get { return this._location; }
        }

        #endregion
        #region Owner

        /// <summary>
        /// Gets the owner of the <see cref="Site"/>.</summary>
        /// <value><para>
        /// The <see cref="Faction"/> that owns the <see cref="Site"/>.
        /// </para><para>-or-</para><para>
        /// A null reference to indicate an unowned <b>Site</b>. The default is a null reference.
        /// </para></value>
        /// <remarks>
        /// <b>Owner</b> is set by the method <see cref="SetOwner"/>. Use the HCL instruction <see
        /// cref="Command.SetSiteOwner"/> to set this property while executing a game command.
        /// </remarks>

        public Faction Owner { get; private set; }

        #endregion
        #region SupplyResources

        /// <summary>
        /// Gets the resources that are currently available for supplying <see cref="Units"/> on the
        /// <see cref="Site"/>.</summary>
        /// <value>
        /// An array of <see cref="Int32"/> values indicating the amount of various resources that
        /// the <see cref="Site"/> provides to its <see cref="Units"/> elements. The default is a
        /// null reference.</value>
        /// <remarks><para>
        /// <b>SupplyResources</b> is set by <see cref="SetSupplyResources"/> which is usually
        /// called by the <see cref="Faction"/> method <see cref="Faction.GetSupplyTargets"/>.
        /// </para><para>
        /// When set to a valid <see cref="Array"/>, each <see cref="Int32"/> value corresponds to
        /// the <see cref="ResourceClass"/> identifier at the same index position in the <see
        /// cref="Faction.SupplyResources"/> collection of the calling <see cref="Faction"/>.
        /// </para></remarks>

        public int[] SupplyResources { get; private set; }

        #endregion
        #region Terrains

        /// <summary>
        /// Gets a list of all terrains placed on the <see cref="Site"/>.</summary>
        /// <value>
        /// A read-only <see cref="EntityList"/> containing all <see cref="Terrain"/> objects placed
        /// on the <see cref="Site"/>. The default is an empty collection.</value>
        /// <remarks><para>
        /// <b>Terrains</b> never returns a null reference, and its elements are never null
        /// references. All keys and elements are unique. Use the <see cref="Entity.Site"/> property
        /// to add or remove <see cref="Terrain"/> objects.
        /// </para><para>
        /// Entities are stored in the order in which they are overlaid on the map view, with the
        /// highest index on top.</para></remarks>

        public EntityList Terrains {
            [DebuggerStepThrough]
            get { return this._terrains.AsReadOnly(); }
        }

        #endregion
        #region Units

        /// <summary>
        /// Gets a list of all units placed on the <see cref="Site"/>.</summary>
        /// <value>
        /// A read-only <see cref="EntityList"/> containing all <see cref="Unit"/> objects placed on
        /// the <see cref="Site"/>. The default is an empty collection.</value>
        /// <remarks><para>
        /// <b>Units</b> never returns a null reference, and its elements are never null references.
        /// All keys and elements are unique. Use the <see cref="Entity.Site"/> property to add or
        /// remove <see cref="Unit"/> objects.
        /// </para><para>
        /// Entities are stored in the order in which they are overlaid on the map view, with the
        /// highest index on top.</para></remarks>

        public EntityList Units {
            [DebuggerStepThrough]
            get { return this._units.AsReadOnly(); }
        }

        #endregion
        #endregion
        #region Private Methods
        #region CheckTerrainChanged

        /// <summary>
        /// Checks whether <see cref="TerrainChanged"/> is set and if so, recalculates all buffered
        /// values and clears this flag.</summary>
        /// <remarks>
        /// <b>CheckTerrainChanged</b> does nothing if <see cref="TerrainChanged"/> is <c>false</c>.
        /// Otherwise, the <see cref="Difficulty"/>, <see cref="Elevation"/>, and <see
        /// cref="Valuation"/> properties are recalculated and <see cref="TerrainChanged"/> is reset
        /// to <c>false</c>.</remarks>

        private void CheckTerrainChanged() {
            if (!TerrainChanged) return;

            int difficulty = 0, elevation = 0;
            double valuation = 0.0;

            // recalculate buffered property values
            for (int i = 0; i < Terrains.Count; i++) {
                Terrain terrain = (Terrain) Terrains[i];

                difficulty += terrain.Difficulty;
                elevation += terrain.Elevation;
                valuation += terrain.Valuation;
            }

            // store new property values
            this._difficulty = difficulty;
            this._elevation = elevation;
            this._valuation = valuation;

            TerrainChanged = false;
        }

        #endregion
        #region IsConnectedCore

        /// <summary>
        /// Determines whether this <see cref="Site"/> contains a specified <see cref="Entity"/>
        /// that is visually connected to an adjacent <see cref="Site"/>.</summary>
        /// <param name="site">
        /// Another <see cref="Site"/> that shares a common edge or vertex with this instance.
        /// </param>
        /// <param name="category">
        /// An <see cref="EntityCategory"/> indicating the entity stack to search for the specified
        /// <paramref name="classId"/>.</param>
        /// <param name="classId">
        /// The entity class <see cref="EntityClass.Id"/> of the <see cref="Entity"/> whose visual
        /// connections to examine.</param>
        /// <returns>
        /// <c>true</c> if this <see cref="Site"/> contains the indicated <see cref="Entity"/>, and
        /// the <see cref="EntityClass.GetConnections"/> value for its current <see
        /// cref="Entity.FrameOffset"/> matches the shared edge or vertex; otherwise, <c>false</c>.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// The specified <paramref name="site"/> does not share a common edge or vertex with this
        /// instance.</exception>
        /// <exception cref="InvalidEnumArgumentException">
        /// <paramref name="category"/> is not a valid <see cref="EntityCategory"/> value.
        /// </exception>
        /// <remarks>
        /// <b>IsConnectedCore</b> calls <see cref="WorldUtility.GetEntityByClass"/> to allow prefix
        /// matches on the specified <paramref name="classId"/>, denoted by a trailing asterisk.
        /// </remarks>

        private bool IsConnectedCore(Site site, EntityCategory category, string classId) {

            // locate class instance on current site
            EntityList entities = GetEntities(category);
            Entity entity = WorldUtility.GetEntityByClass(entities, classId);
            if (entity == null) return false;

            // retrieve connections of selected frame
            var connections = entity.EntityClass.GetConnections(entity.FrameOffset);

            // check if edge is connected to neighbor
            PolygonGrid mapGrid = Finder.MapGrid;
            int index = mapGrid.GetNeighborIndex(Location, site.Location);
            Compass compass = mapGrid.Element.IndexToCompass(index);

            return connections.Contains(compass);
        }

        #endregion
        #endregion
        #region Internal Methods
        #region GetWritableEntities

        /// <summary>
        /// Gets a writable list of all entities in the specified category that are placed on the
        /// <see cref="Site"/>.</summary>
        /// <param name="category">
        /// An <see cref="EntityCategory"/> value indicating which entities to return.</param>
        /// <returns>
        /// A writable <see cref="EntityList"/> containing all entities in the specified <paramref
        /// name="category"/> that are placed on the <see cref="Site"/>.</returns>
        /// <exception cref="InvalidEnumArgumentException">
        /// <paramref name="category"/> is not a valid <see cref="EntityCategory"/> value.
        /// </exception>
        /// <remarks><para>
        /// <b>GetWritableEntities</b> returns a null reference if the specified <paramref
        /// name="category"/> equals <see cref="EntityCategory.Upgrade"/>.
        /// </para><para>
        /// Otherwise, <b>GetWritableEntities</b> returns the value of either the <see
        /// cref="WritableUnits"/>, <see cref="WritableTerrains"/>, or <see cref="WritableEffects"/>
        /// property, depending on the value of <paramref name="category"/>.</para></remarks>

        internal EntityList GetWritableEntities(EntityCategory category) {
            switch (category) {

                case EntityCategory.Unit:    return WritableUnits;
                case EntityCategory.Terrain: return WritableTerrains;
                case EntityCategory.Effect:  return WritableEffects;
                case EntityCategory.Upgrade: return null;

                default:
                    ThrowHelper.ThrowInvalidEnumArgumentException(
                        "category", (int) category, typeof(EntityCategory));
                    return null;
            }
        }

        #endregion
        #region SetEntityOwner

        /// <summary>
        /// Sets the <see cref="Entity.Owner"/> property of all entities in the specified stack.
        /// </summary>
        /// <param name="category">
        /// An <see cref="EntityCategory"/> value indicating the entity stack to manipulate.</param>
        /// <param name="owner">
        /// The new value for the <see cref="Entity.Owner"/> property of all entities in the
        /// specified <paramref name="category"/>.</param>
        /// <returns>
        /// <c>true</c> if the <see cref="Entity.Owner"/> of any <see cref="Entity"/> in the
        /// specified <paramref name="category"/> was changed; otherwise, <c>false</c>.</returns>
        /// <exception cref="InvalidEnumArgumentException">
        /// <paramref name="category"/> is not a valid <see cref="EntityCategory"/> value.
        /// </exception>
        /// <remarks>
        /// <b>SetEntityOwner</b> sets the <see cref="Entity.Owner"/> properties of all elements of
        /// the entity stack with the specified <paramref name="category"/> to the specified
        /// <paramref name="owner"/> while retaining the elements' index order.</remarks>

        internal bool SetEntityOwner(EntityCategory category, Faction owner) {
            EntityList entities = GetEntities(category);

            /*
             * We only need to check the owner of the first entity thanks to the stack
             * invariant, i.e. all entities in the same stack must have the same owner.
             *
             * For the same reason, the entity owner must be changed without checking
             * against the stack invariant. Otherwise we would get an exception due to
             * different entity owners in the same stack.
             */

            if (entities.Count == 0 || entities[0].Owner == owner)
                return false;

            for (int i = 0; i < entities.Count; i++)
                entities[i].SetOwner(owner, false);

            return true;
        }

        #endregion
        #region SetOwner

        /// <summary>
        /// Sets the <see cref="Owner"/> property to the specified value.</summary>
        /// <param name="owner">
        /// The new value for the <see cref="Owner"/> property.</param>
        /// <remarks><para>
        /// <b>SetOwner</b> performs the following actions:
        /// </para><list type="number"><item>
        /// Remove the <see cref="Site"/> from the <see cref="Faction.Sites"/> collection of the
        /// existing <see cref="Owner"/>, if any.
        /// </item><item>
        /// Set the <see cref="Owner"/> property to the specified <paramref name="owner"/>.
        /// </item><item>
        /// Add the <see cref="Site"/> to the <b>Sites</b> collection of the specified <paramref
        /// name="owner"/>, if any.
        /// </item><item>
        /// Transfer all <see cref="Terrains"/> and <see cref="Effects"/> to the specified <paramref
        /// name="owner"/>.</item></list></remarks>

        internal void SetOwner(Faction owner) {

            // remove site from old owner, if any
            if (Owner != null) {
                SiteList sites = Owner.WritableSites;

                // remove by index to provoke exception if missing
                int index = sites.IndexOf(this);
                sites.RemoveAt(index);
            }

            Owner = owner;

            // add site to new owner, if any
            if (owner != null)
                owner.WritableSites.Add(this);

            // transfer terrains and effects
            SetEntityOwner(EntityCategory.Terrain, owner);
            SetEntityOwner(EntityCategory.Effect, owner);
        }

        #endregion
        #region SetOwnerUnchecked

        /// <summary>
        /// Sets the <see cref="Owner"/> property to the specified value, without removing the <see
        /// cref="Site"/> from its previous owner.</summary>
        /// <param name="owner">
        /// The new value for the <see cref="Owner"/> property. This argument cannot be a null
        /// reference.</param>
        /// <remarks><para>
        /// <b>SetOwnerUnchecked</b> is a fast, but unsafe, helper method for the <see
        /// cref="WorldState(WorldState)"/> copy constructor and should not normally be called by
        /// other clients.
        /// </para><para>
        /// <b>SetOwnerUnchecked</b> sets the <see cref="Owner"/> property to the specified
        /// <paramref name="owner"/> and then adds the <see cref="Site"/> to that <see
        /// cref="Faction"/>. All other actions performed by the <b>Owner</b> setter are skipped.
        /// </para></remarks>

        internal void SetOwnerUnchecked(Faction owner) {
            Debug.Assert(owner != null);
            Owner = owner;
            owner.WritableSites.Add(this);
        }

        #endregion
        #region SetTerrainChanged

        /// <summary>
        /// Sets the <see cref="TerrainChanged"/> property of the <see cref="Site"/> and of the
        /// specified <see cref="WorldState"/>.</summary>
        /// <param name="worldState">
        /// The <see cref="WorldState"/> that contains the <see cref="Site"/>.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="worldState"/> is a null reference.</exception>
        /// <remarks><para>
        /// <b>SetTerrainChanged</b> sets the <see cref="TerrainChanged"/> property to <c>true</c>
        /// and invokes <see cref="WorldState.SetTerrainChanged"/> on the specified <paramref
        /// name="worldState"/>, causing the recalculation of all terrain-dependent values upon the
        /// next request for any such value.
        /// </para><para>
        /// <b>SetTerrainChanged</b> is called by the <see cref="Terrain"/> overrides of the <see
        /// cref="Terrain.OnSiteChanged"/> and <see cref="Terrain.OnVariableChanged"/> methods.
        /// </para></remarks>

        internal void SetTerrainChanged(WorldState worldState) {
            if (worldState == null)
                ThrowHelper.ThrowArgumentNullException("worldState");

            TerrainChanged = true;
            worldState.SetTerrainChanged();
        }

        #endregion
        #endregion
        #region Public Methods
        #region AddVariables

        /// <summary>
        /// Adds all values of the specified <see cref="VariableClass"/> that are defined by any
        /// <see cref="Entity"/> placed on or affecting the <see cref="Site"/>.</summary>
        /// <param name="worldState">
        /// The <see cref="WorldState"/> containing the <see cref="Site"/>.</param>
        /// <param name="variable">
        /// A <see cref="VariableClass"/> indicating which variable values to add.</param>
        /// <returns><para>
        /// A <see cref="PointI"/> containing sums of all values of the specified <paramref
        /// name="variable"/> that are defined by any <see cref="Entity"/> placed on or affecting
        /// the <see cref="Site"/>.
        /// </para><para>
        /// The <see cref="PointI.X"/> component contains the sum of all basic values, and the <see
        /// cref="PointI.Y"/> component contains the sum of all modifier values.</para></returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="variable"/> is a null reference.</exception>
        /// <remarks><para>
        /// <b>AddVariables</b> computes the <see cref="PointI.Y"/> component of the returned <see
        /// cref="PointI"/> as follows:
        /// </para><list type="number"><item>
        /// Add all matching <see cref="ModifierTarget.Self"/> and <see
        /// cref="ModifierTarget.Owner"/> modifiers of any <see cref="Entity"/> placed on the <see
        /// cref="Site"/>.
        /// </item><item>
        /// Add the matching aggregate modifiers returned by <see cref="Faction.GetUnitModifiers"/>
        /// for the <see cref="Entity.Owner"/> of the local <see cref="Units"/>, if any; otherwise,
        /// for the <see cref="WorldState.ActiveFaction"/> of the specified <paramref
        /// name="worldState"/>.
        /// </item></list><para>
        /// <b>AddVariables</b> should be used only for overview displays, not for the planning of
        /// computer player actions, since the returned aggregate modifier values may be very
        /// different from the ones actually affecting any given unit.</para></remarks>

        public PointI AddVariables(WorldState worldState, VariableClass variable) {
            if (worldState == null)
                ThrowHelper.ThrowArgumentNullException("worldState");
            if (variable == null)
                ThrowHelper.ThrowArgumentNullException("variable");

            int basic = 0, modifier = 0;

            // traverse all entity stacks on this site
            foreach (EntityCategory category in EntitySection.AllCategories) {
                EntityList entities = GetEntities(category);

                // traverse all entities in this site stack
                for (int i = 0; i < entities.Count; i++) {
                    Entity entity = entities[i];

                    // traverse all basic values defined by the entity
                    var variables = entity.GetVariables(variable.Category);
                    if (variables != null)
                        for (int j = 0; j < variables.Count; j++) {
                            Variable instance = variables[j];
                            if (instance.Id == variable.Id) basic += instance.Value;
                        }

                    // traverse all self-modifier values defined by the entity
                    variables = entity.GetVariableModifiers(variable.Category, ModifierTarget.Self);
                    if (variables != null)
                        for (int j = 0; j < variables.Count; j++) {
                            Variable instance = variables[j];
                            if (instance.Id == variable.Id) modifier += instance.Value;
                        }

                    // traverse all owner modifier values defined by the entity
                    variables = entity.GetVariableModifiers(variable.Category, ModifierTarget.Owner);
                    if (variables != null)
                        for (int j = 0; j < variables.Count; j++) {
                            Variable instance = variables[j];
                            if (instance.Id == variable.Id) modifier += instance.Value;
                        }
                }
            }

            // aggregate unit modifier for local unit owner or active faction
            Faction faction = (Units.Count > 0 ? Units[0].Owner : worldState.ActiveFaction);
            if (faction != null) {
                int value;
                var modifiers = faction.GetUnitModifiers(Location.X, Location.Y, variable.Category);
                if (modifiers != null && modifiers.TryGetValue(variable.Id, out value))
                    modifier += value;
            }

            return new PointI(basic, modifier);
        }

        #endregion
        #region CountCombatUnits

        /// <summary>
        /// Counts the <see cref="Units"/> on the <see cref="Site"/> that are capable of attacking.
        /// </summary>
        /// <returns>
        /// The number of <see cref="Units"/> whose <see cref="Unit.IsCombat"/> flag is <c>true</c>.
        /// </returns>

        public int CountCombatUnits() {
            int combatCount = 0;

            for (int i = 0; i < Units.Count; i++)
                if (((Unit) Units[i]).IsCombat)
                    ++combatCount;

            return combatCount;
        }

        #endregion
        #region CountMobileUnits

        /// <summary>
        /// Counts the <see cref="Units"/> on the <see cref="Site"/> that are capable of movement.
        /// </summary>
        /// <returns>
        /// The number of <see cref="Units"/> whose <see cref="Unit.IsMobile"/> flag is <c>true</c>.
        /// </returns>

        public int CountMobileUnits() {
            int mobileCount = 0;

            for (int i = 0; i < Units.Count; i++)
                if (((Unit) Units[i]).IsMobile)
                    ++mobileCount;

            return mobileCount;
        }

        #endregion
        #region DistanceComparison

        /// <summary>
        /// Compares two <see cref="Site"/> objects with respect to their <see cref="Distance"/>
        /// values.</summary>
        /// <param name="x">
        /// The first <see cref="Site"/> to compare.</param>
        /// <param name="y">
        /// The second <see cref="Site"/> to compare.</param>
        /// <returns><para>
        /// An <see cref="Int32"/> value indicating the relative order of <paramref name="x"/> and
        /// <paramref name="y"/>, as follows:
        /// </para><list type="table"><listheader>
        /// <term>Value</term><description>Condition</description>
        /// </listheader><item>
        /// <term>Less than zero</term>
        /// <description><paramref name="x"/> has a smaller <see cref="Distance"/> value than
        /// <paramref name="y"/>.</description>
        /// </item><item>
        /// <term>Zero</term>
        /// <description><paramref name="x"/> and <paramref name="y"/> have the same <see
        /// cref="Distance"/> value.</description>
        /// </item><item>
        /// <term>Greater than zero</term>
        /// <description><paramref name="x"/> has a greater <see cref="Distance"/> value than
        /// <paramref name="y"/>.</description>
        /// </item></list></returns>
        /// <remarks><para>
        /// <b>DistanceComparison</b> is compatible with the <see cref="Comparison{T}"/> delegate
        /// and can be passed to various sorting methods.
        /// </para><para>
        /// Either or both arguments may be null references. A null reference is considered smaller
        /// than a valid <see cref="Site"/> object, and two null references are considered equal.
        /// </para></remarks>

        public static int DistanceComparison(Site x, Site y) {

            if (x == null)
                return (y == null ? 0 : -1);
            else if (y == null)
                return 1;

            return (x.Distance - y.Distance);
        }

        #endregion
        #region Format

        /// <summary>
        /// Returns a <see cref="String"/> that represents the specified <see cref="Site"/>
        /// coordinates.</summary>
        /// <param name="site">
        /// The coordinates of the map site to represent.</param>
        /// <returns>
        /// A <see cref="String"/> containing the components of the specified <paramref
        /// name="site"/>, with each coordinate formatted to three digits, separated by commas and
        /// surrounded by parentheses.</returns>
        /// <remarks><para>
        /// <b>Format</b> prepares a <see cref="PointI"/> instance for display to the user. Use <see
        /// cref="SimpleXml.WritePointI"/> for XML serialization.
        /// </para><para>
        /// If either coordinate of the specified <paramref name="site"/> is negative, the
        /// parentheses will contain two en dashes (–,–) instead of the actual coordinates.
        /// </para></remarks>
        /// <example>
        /// If the specified <paramref name="site"/> had an <see cref="PointI.X"/> coordinate of 35
        /// and a <see cref="PointI.Y"/> coordinate of 4, <b>Format</b> would return the literal
        /// string "(035,004)".</example>

        public static string Format(PointI site) {
            return (site.X < 0 || site.Y < 0 ? "(–,–)" :
                String.Format(ApplicationInfo.Culture, "({0:D3},{1:D3})", site.X, site.Y));
        }

        #endregion
        #region FormatLabel

        /// <summary>
        /// Returns a <see cref="String"/> that represents the specified <see cref="Site"/>
        /// coordinates, prefixed with the localized label "Site".</summary>
        /// <param name="site">
        /// The coordinates of the map site to represent.</param>
        /// <returns>
        /// A <see cref="String"/> containing the localized label "Site", followed by the components
        /// of the specified <paramref name="site"/>, with each coordinate formatted to three
        /// digits, separated by commas and surrounded by parentheses.</returns>
        /// <remarks><para>
        /// <b>FormatLabel</b> prepares a <see cref="PointI"/> instance for display to the user. Use
        /// <see cref="SimpleXml.WritePointI"/> for XML serialization.
        /// </para><para>
        /// If either coordinate of the specified <paramref name="site"/> is negative, the
        /// parentheses will contain two en dashes (–,–) instead of the actual coordinates.
        /// </para></remarks>
        /// <example>
        /// If the specified <paramref name="site"/> had an <see cref="PointI.X"/> coordinate of 35
        /// and a <see cref="PointI.Y"/> coordinate of 4, <b>FormatLabel</b> would return the
        /// localized literal string "Site (035,004)".</example>

        public static string FormatLabel(PointI site) {
            return Global.Strings.LabelSite + " " + Format(site);
        }

        #endregion
        #region GetEntity

        /// <summary>
        /// Returns the <see cref="Entity"/> with the specified identifier that is placed on the
        /// <see cref="Site"/>.</summary>
        /// <param name="id">
        /// An <see cref="Entity.Id"/> string indicating the <see cref="Entity"/> to locate.</param>
        /// <returns><para>
        /// An <see cref="Entity"/> that is placed on the <see cref="Site"/> and whose <see
        /// cref="Entity.Id"/> string equals <paramref name="id"/>.
        /// </para><para>-or-</para><para>
        /// A null reference if there is no matching <see cref="Entity"/>.</para></returns>
        /// <remarks>
        /// <b>GetEntity</b> returns a null reference if the specified <paramref name="id"/> is a
        /// null reference or an empty string.</remarks>

        public Entity GetEntity(string id) {
            if (String.IsNullOrEmpty(id)) return null;

            Entity entity;
            foreach (EntityCategory category in EntitySection.AllCategories)
                if (GetEntities(category).TryGetValue(id, out entity))
                    return entity;

            return null;
        }

        #endregion
        #region GetEntities

        /// <summary>
        /// Gets a list of all entities in the specified category that are placed on the <see
        /// cref="Site"/>.</summary>
        /// <param name="category">
        /// An <see cref="EntityCategory"/> value indicating which entities to return.</param>
        /// <returns>
        /// A read-only <see cref="EntityList"/> containing all entities in the specified <paramref
        /// name="category"/> that are placed on the <see cref="Site"/>.</returns>
        /// <exception cref="InvalidEnumArgumentException">
        /// <paramref name="category"/> is not a valid <see cref="EntityCategory"/> value.
        /// </exception>
        /// <remarks><para>
        /// <b>GetEntities</b> returns an empty read-only collection if the specified <paramref
        /// name="category"/> equals <see cref="EntityCategory.Upgrade"/>.
        /// </para><para>
        /// Otherwise, <b>GetEntities</b> returns the value of either the <see cref="Units"/>, <see
        /// cref="Terrains"/>, or <see cref="Effects"/> property, depending on the value of
        /// <paramref name="category"/>.</para></remarks>

        public EntityList GetEntities(EntityCategory category) {
            switch (category) {

                case EntityCategory.Unit:    return Units;
                case EntityCategory.Terrain: return Terrains;
                case EntityCategory.Effect:  return Effects;
                case EntityCategory.Upgrade: return EntityList.Empty;

                default:
                    ThrowHelper.ThrowInvalidEnumArgumentException(
                        "category", (int) category, typeof(EntityCategory));
                    return null;
            }
        }

        #endregion
        #region HasAlienUnits

        /// <summary>
        /// Determines whether the <see cref="Site"/> contains any <see cref="Units"/> that are
        /// <em>not</em> owned by the specified <see cref="Faction"/>.</summary>
        /// <param name="faction">
        /// The <see cref="Faction"/> to test for ownership.</param>
        /// <returns>
        /// <c>true</c> if the <see cref="Units"/> collection contains at least one element whose
        /// <see cref="Entity.Owner"/> property differs from the specified <paramref
        /// name="faction"/>; otherwise, <c>false</c>.</returns>
        /// <remarks>
        /// <b>HasAlienUnits</b> only tests the <see cref="Entity.Owner"/> property of the first
        /// <see cref="Units"/> element, if any, as all units on the same <see cref="Site"/> must
        /// have the same owner.</remarks>

        public bool HasAlienUnits(Faction faction) {
            if (Units.Count == 0) return false;
            return (Units[0].Owner != faction);
        }

        #endregion
        #region HasCaptureUnits

        /// <summary>
        /// Determines whether the <see cref="Site"/> contains any <see cref="Units"/> that have the
        /// <see cref="Unit.CanCapture"/> ability.</summary>
        /// <returns>
        /// <c>true</c> if the <see cref="Units"/> collection contains at least one element whose
        /// <see cref="Unit.CanCapture"/> flag is <c>true</c>; otherwise, <c>false</c>.</returns>

        public bool HasCaptureUnits() {

            for (int i = 0; i < Units.Count; i++)
                if (((Unit) Units[i]).CanCapture)
                    return true;

            return false;
        }

        #endregion
        #region HasCustomEntities

        /// <summary>
        /// Determines whether the <see cref="Site"/> contains any entities other than the default
        /// terrain stack.</summary>
        /// <returns>
        /// <c>true</c> if the <see cref="Units"/> or <see cref="Effects"/> collections are not
        /// empty, or if the <see cref="Terrains"/> collection differs from the default <see
        /// cref="AreaSection.Terrains"/> stack; otherwise, <c>false</c>.</returns>

        public bool HasCustomEntities() {

            // default non-terrain stacks are all empty
            if (Units.Count > 0 || Effects.Count > 0)
                return true;

            // check terrain stack size against default stack
            AreaSection areas = MasterSection.Instance.Areas;
            if (Terrains.Count != areas.Terrains.Count)
                return true;

            // check stack elements against default stack
            for (int i = 0; i < Terrains.Count; i++) {
                Entity terrain = Terrains[i];

                // check for custom terrain name
                if (terrain.Name != terrain.DefaultName)
                    return true;

                // check for custom terrain class
                if (terrain.EntityClass.Id != areas.Terrains[i].EntityClass)
                    return true;
            }

            return false;
        }

        #endregion
        #region HasOwnedUnits

        /// <summary>
        /// Determines whether the <see cref="Site"/> contains any <see cref="Units"/> owned by the
        /// specified <see cref="Faction"/>.</summary>
        /// <param name="faction">
        /// The <see cref="Faction"/> to test for ownership.</param>
        /// <returns>
        /// <c>true</c> if the <see cref="Units"/> collection contains at least one element whose
        /// <see cref="Entity.Owner"/> property equals the specified <paramref name="faction"/>;
        /// otherwise, <c>false</c>.</returns>
        /// <remarks>
        /// <b>HasOwnedUnits</b> only tests the <see cref="Entity.Owner"/> property of the first
        /// <see cref="Units"/> element, if any, as all units on the same <see cref="Site"/> must
        /// have the same owner.</remarks>

        public bool HasOwnedUnits(Faction faction) {
            if (Units.Count == 0) return false;
            return (Units[0].Owner == faction);
        }

        #endregion
        #region HasPositiveSupplies

        /// <summary>
        /// Determines whether the <see cref="Site"/> contains any positive <see
        /// cref="SupplyResources"/>.</summary>
        /// <returns>
        /// <c>true</c> if <see cref="SupplyResources"/> is a valid <see cref="Array"/> and contains
        /// at least one element that is greater than zero; otherwise, <c>false</c>.</returns>

        public bool HasPositiveSupplies() {

            if (SupplyResources != null)
                foreach (int value in SupplyResources)
                    if (value > 0) return true;

            return false;
        }

        #endregion
        #region IsConnected

        /// <summary>
        /// Determines whether this <see cref="Site"/> and an adjacent <see cref="Site"/> are
        /// visually connected by a specified <see cref="Entity"/>.</summary>
        /// <param name="site">
        /// Another <see cref="Site"/> that shares a common edge or vertex with this instance.
        /// </param>
        /// <param name="category">
        /// An <see cref="EntityCategory"/> indicating the entity stack to search for the specified
        /// <paramref name="classId"/>.</param>
        /// <param name="classId">
        /// The entity class <see cref="EntityClass.Id"/> of the <see cref="Entity"/> whose visual
        /// connections to examine.</param>
        /// <param name="connection">
        /// A <see cref="SiteConnection"/> value indicating whether to check only this <see
        /// cref="Site"/> or also the specified <paramref name="site"/> for the indicated <see
        /// cref="Entity"/>.</param>
        /// <returns>
        /// <c>true</c> if this <see cref="Site"/> and/or the specified <paramref name="site"/>, 
        /// depending on the specified <paramref name="connection"/> option, contains the indicated
        /// <see cref="Entity"/>, and the <see cref="EntityClass.GetConnections"/> value for its 
        /// current <see cref="Entity.FrameOffset"/> matches the shared edge or vertex; otherwise,
        /// <c>false</c>.</returns>
        /// <exception cref="ArgumentException">
        /// The specified <paramref name="site"/> does not share a common edge or vertex with this
        /// instance.</exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="site"/> is a null reference.</exception>
        /// <exception cref="ArgumentNullOrEmptyException">
        /// <paramref name="classId"/> is a null reference or an empty string.</exception>
        /// <exception cref="InvalidEnumArgumentException"><para>
        /// <paramref name="category"/> is not a valid <see cref="EntityCategory"/> value.
        /// </para><para>-or-</para><para>
        /// <paramref name="connection"/> is not a valid <see cref="SiteConnection"/> value.
        /// </para></exception>
        /// <remarks>
        /// If the last character of the specified <paramref name="classId"/> is an asterisk ("*"),
        /// <b>IsConnected</b> examines the first <see cref="Entity"/> whose <see
        /// cref="Entity.EntityClass"/> identifier starts with the specified <paramref
        /// name="classId"/> minus the asterisk.</remarks>

        public bool IsConnected(Site site, EntityCategory category,
            string classId, SiteConnection connection) {

            if (site == null)
                ThrowHelper.ThrowArgumentNullException("site");
            if (String.IsNullOrEmpty(classId))
                ThrowHelper.ThrowArgumentNullOrEmptyException("classId");

            switch (connection) {

                case SiteConnection.Local:
                    return IsConnectedCore(site, category, classId);

                case SiteConnection.LocalOrNeighbor:
                    return (IsConnectedCore(site, category, classId) ||
                        site.IsConnectedCore(this, category, classId));

                case SiteConnection.LocalAndNeighbor:
                    return (IsConnectedCore(site, category, classId) &&
                        site.IsConnectedCore(this, category, classId));

                default:
                    ThrowHelper.ThrowInvalidEnumArgumentException(
                        "connection", (int) connection, typeof(SiteConnection));
                    return false;
            }
        }

        #endregion
        #region SetSupplyResources

        /// <summary>
        /// Sets the <see cref="SupplyResources"/> property to the amounts of supply resources that
        /// are available to the specified <see cref="Faction"/>.</summary>
        /// <param name="faction">
        /// The <see cref="Faction"/> whose supply resources to compute.</param>
        /// <remarks><para>
        /// <b>SetSupplyResources</b> sets the <see cref="SupplyResources"/> property to a null
        /// reference if <paramref name="faction"/> is a null reference, or the <see
        /// cref="Faction.SupplyResources"/> property of the specified <paramref name="faction"/> is
        /// a null reference or an empty collection.
        /// </para><para>
        /// Otherwise, <b>SetSupplyResources</b> adds up the following values for all <see
        /// cref="Faction.SupplyResources"/> defined by the <paramref name="faction"/>:
        /// </para><list type="bullet"><item>
        /// All matching <see cref="Faction.UnitResourceModifiers"/> for the current <see
        /// cref="Location"/> that are defined by the specified <paramref name="faction"/>.
        /// </item><item>
        /// All matching <see cref="Entity.Resources"/> of all local <see cref="Terrains"/> and <see
        /// cref="Effects"/> whose <see cref="EntityClass.ResourceTransfer"/> property does not
        /// equal <see cref="ResourceTransferMode.None"/>.
        /// </item></list><para>
        /// If at least one sum does not equal zero, <b>SetSupplyResources</b> sets the <see
        /// cref="SupplyResources"/> property to an <see cref="Array"/> that mirrors the faction's
        /// <see cref="Faction.SupplyResources"/>, containing the the sum for each resource
        /// identifier at the same index position.</para></remarks>

        public void SetSupplyResources(Faction faction) {

            // delete old array, if any
            SupplyResources = null;

            // quit if no faction specified
            if (faction == null) return;

            // quit if faction defines no supply resources
            var resources = faction.SupplyResources;
            if (resources == null || resources.Count == 0)
                return;

            var modifiers = faction.UnitResourceModifiers[Location.X, Location.Y];

            // add all resources supplied by modifier map
            for (int j = 0; j < resources.Count; j++) {
                int value;
                modifiers.TryGetValue(resources[j], out value);

                if (value != 0) {
                    // perform lazy allocation if necessary
                    if (SupplyResources == null)
                        SupplyResources = new int[resources.Count];

                    SupplyResources[j] += value;
                }
            }

            // process all local terrains & effects
            EntityCategory[] categories = { EntityCategory.Terrain, EntityCategory.Effect };
            foreach (EntityCategory category in categories) {
                EntityList entities = GetEntities(category);

                // traverse all entities in current stack
                for (int i = 0; i < entities.Count; i++) {
                    Entity entity = entities[i];

                    // ignore entities that cannot transfer resources
                    if (entity.EntityClass.ResourceTransfer == ResourceTransferMode.None)
                        continue;

                    // add all resources supplied by current entity
                    for (int j = 0; j < resources.Count; j++) {
                        int value = entity.Resources.GetValue(resources[j]);

                        if (value != 0) {
                            // perform lazy allocation if necessary
                            if (SupplyResources == null)
                                SupplyResources = new int[resources.Count];

                            SupplyResources[j] += value;
                        }
                    }
                }
            }
        }

        #endregion
        #region SortByDistance

        /// <summary>
        /// Sorts the specified <see cref="Site"/> collection by distance from the specified map
        /// location.</summary>
        /// <param name="source">
        /// The coordinates of the <see cref="Site"/> from which to calculate distances.</param>
        /// <param name="targets">
        /// The <see cref="IList{Site}"/> to sort.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="targets"/> is a null reference.</exception>
        /// <remarks><para>
        /// <b>SortByDistance</b> performs the following actions:
        /// </para><list type="bullet"><item>
        /// Set the <see cref="Distance"/> property of all <paramref name="targets"/> to the
        /// distance, in map sites, between their respective <see cref="Location"/> and the
        /// specified <paramref name="source"/> site.
        /// </item><item>
        /// Sort the <paramref name="targets"/> collection using the <see
        /// cref="DistanceComparison"/> method.</item></list></remarks>

        public static void SortByDistance(PointI source, IList<Site> targets) {
            /*
             * We cannot quit immediately if there is only one target because the
             * caller expects us to set the Distance property of all elements.
             */

            if (targets.Count == 0) return;

            // cache map grid reference
            PolygonGrid mapGrid = Finder.MapGrid;

            // store distance with each target
            for (int i = 0; i < targets.Count; i++) {
                Site site = targets[i];
                site.Distance = mapGrid.GetStepDistance(source, site.Location);
            }

            // sort targets by distance from source
            if (targets.Count > 1)
                targets.BestQuickSort(DistanceComparison);
        }

        #endregion
        #region ToString

        /// <summary>
        /// Returns a <see cref="String"/> that represents the <see cref="Site"/>.</summary>
        /// <returns>
        /// A <see cref="String"/> containing the value of the <see cref="Location"/> property, with
        /// each coordinate formatted to three digits, separated by commas and surrounded by
        /// parentheses.</returns>
        /// <remarks>
        /// <b>ToString</b> returns the result of <see cref="Format"/> for the current value of the
        /// <see cref="Location"/> property.</remarks>

        public override string ToString() {
            return Format(Location);
        }

        #endregion
        #endregion
        #region PointI(Site)

        /// <summary>
        /// Converts a <see cref="Site"/> to a <see cref="PointI"/> that equals its <see
        /// cref="Location"/>.</summary>
        /// <param name="site">
        /// The <see cref="Site"/> instance to convert into a <see cref="PointI"/> instance.</param>
        /// <returns><para>
        /// The <see cref="Location"/> of the specified <paramref name="site"/>.
        /// </para><para>-or-</para><para>
        /// <see cref="InvalidLocation"/> if <paramref name="site"/> is a null reference.
        /// </para></returns>

        public static implicit operator PointI(Site site) {
            return (site == null ? Site.InvalidLocation : site.Location);
        }

        #endregion
        #region ICloneable Members

        /// <summary>
        /// Creates a new <see cref="Site"/> object that is a deep copy of the current instance.
        /// </summary>
        /// <returns>
        /// A new <see cref="Site"/> object that is a deep copy of the current instance.</returns>
        /// <remarks><para>
        /// <b>Clone</b> processes the properties of the current instance as follows:
        /// </para><list type="table"><listheader>
        /// <term>Property</term><description>Action</description>
        /// </listheader><item>
        /// <term><see cref="Difficulty"/><br/> <see cref="Elevation"/><br /> <see
        /// cref="Location"/><br/> <see cref="TerrainChanged"/><br/> <see cref="Valuation"/></term>
        /// <description>Values copied to the new instance.</description>
        /// </item><item>
        /// <term><see cref="Effects"/><br/> <see cref="Terrains"/><br/> <see cref="Units"/></term>
        /// <description>Deep copies assigned to the new instance.</description>
        /// </item><item>
        /// <term><see cref="Owner"/></term>
        /// <description>Ignored; set by <see cref="WorldState(WorldState)"/>.</description>
        /// </item><item>
        /// <term><see cref="Distance"/><br/> <see cref="SupplyResources"/></term>
        /// <description>Ignored; initialized to zero and a null reference, respectively, in the new
        /// instance.</description>
        /// </item></list></remarks>

        public object Clone() {
            return new Site(this);
        }

        #endregion
        #region IKeyedValue Members

        /// <summary>
        /// Gets the map coordinates of the <see cref="Site"/>.</summary>
        /// <value>
        /// The value of the <see cref="Location"/> property.</value>

        PointI IKeyedValue<PointI>.Key {
            [DebuggerStepThrough]
            get { return Location; }
        }

        #endregion
        #region IValuable Members

        /// <summary>
        /// Gets the total valuation of the <see cref="Site"/>.</summary>
        /// <value>
        /// A non-negative <see cref="Double"/> value, indicating the desirability of the <see
        /// cref="Site"/> to computer players. Higher values indicate greater desirability.</value>
        /// <remarks><para>
        /// <b>Valuation</b> returns the sum of all <see cref="Entity.Valuation"/> values for all
        /// elements in the <see cref="Terrains"/> collection. This value is never less than zero,
        /// but it may be greater than one.
        /// </para><para>
        /// <b>Valuation</b> only recalculates the total valuation if <see cref="TerrainChanged"/>
        /// is <c>true</c>, and returns a buffered value otherwise.</para></remarks>

        public double Valuation {
            [DebuggerStepThrough]
            get {
                CheckTerrainChanged();
                return this._valuation;
            }
        }

        #endregion
    }
}
