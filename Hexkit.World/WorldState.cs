using System;
using System.Collections.Generic;
using System.Diagnostics;

using Tektosyne;
using Tektosyne.Collections;
using Tektosyne.Geometry;
using Tektosyne.Windows;

using Hexkit.Global;
using Hexkit.Scenario;
using Hexkit.World.Commands;

namespace Hexkit.World {
    #region Type Aliases

    using EntityDictionary = DictionaryEx<String, Entity>;
    using EntityList = KeyedList<String, Entity>;
    using EntityTemplateList = ListEx<EntityTemplate>;
    using FactionList = KeyedList<String, Faction>;
    using InstanceCountDictionary = SortedListEx<String, Int32>;
    using SiteList = KeyedList<PointI, Site>;

    #endregion

    /// <summary>
    /// Represents the state of an entire Hexkit game world.</summary>
    /// <remarks><para>
    /// <b>WorldState</b> manages all data that constitutes the state of the game world at a given
    /// time. This includes the following items:
    /// </para><list type="bullet"><item>
    /// All <see cref="WorldState.Sites"/> that constitute the game map.
    /// </item><item>
    /// All surviving <see cref="WorldState.Factions"/> that represent the competing sides.
    /// </item><item>
    /// All <see cref="WorldState.Entities"/> that are placed on a site or owned by a faction.
    /// </item><item>
    /// The <see cref="WorldState.History"/> of all game commands whose execution led to the current
    /// world state.
    /// </item><item>
    /// Various housekeeping data, such as the current turn count and the active faction.
    /// </item></list><para>
    /// <b>WorldState</b> objects are not serialized directly. Instead, the associated command <see
    /// cref="WorldState.History"/> is stored as part of a Hexkit session description.
    /// </para></remarks>

    public class WorldState: ICloneable {
        #region WorldState()

        /// <overloads>
        /// Initializes a new instance of the <see cref="WorldState"/> class.</overloads>
        /// <summary>
        /// Initializes a new instance of the <see cref="WorldState"/> class with default
        /// properties.</summary>
        /// <remarks>
        /// The <see cref="WorldState"/> instance returned by this constructor is not immediately
        /// usable. Clients must subsequently invoke <see cref="Initialize"/> on the new instance to
        /// acquire the data of the current <see cref="MasterSection"/> instance.</remarks>

        public WorldState() {
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Optimized);

            this._history = new History();
            NewTurnStarted = true;
            TerrainChanged = true;

            this._factions = new FactionList(true);
            this._entities = new EntityDictionary();
            this._instanceCounts = new InstanceCountDictionary();
        }

        #endregion
        #region WorldState(WorldState)

        /// <summary>
        /// Initializes a new instance of the <see cref="WorldState"/> class that is a deep copy of
        /// the specified instance.</summary>
        /// <param name="worldState">
        /// The <see cref="WorldState"/> object whose properties should be copied to the new
        /// instance.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="worldState"/> is a null reference.</exception>
        /// <remarks>
        /// This constructor is called by <see cref="Clone"/> to perform a deep copy of the
        /// specified <paramref name="worldState"/>. Please refer to that method for further
        /// details.</remarks>

        private WorldState(WorldState worldState) {
            if (worldState == null)
                ThrowHelper.ThrowArgumentNullException("worldState");

            GC.Collect(GC.MaxGeneration, GCCollectionMode.Optimized);

            ActiveFactionIndex = worldState.ActiveFactionIndex;
            CurrentTurn = worldState.CurrentTurn;
            NewTurnStarted = worldState.NewTurnStarted;
            this._maxSiteValuation = worldState._maxSiteValuation;
            UnitAttributeModifiersChanged = worldState.UnitAttributeModifiersChanged;
            TerrainChanged = worldState.TerrainChanged;

            this._gameOver = worldState.GameOver;
            this._history = (History) worldState._history.Clone();

            this._entities = new EntityDictionary(worldState._entities.Count);
            this._instanceCounts = new InstanceCountDictionary(worldState._instanceCounts);

            // Copy calls Clone on each element
            this._factions = worldState._factions.Copy();

            // copy winning faction if already defined
            Faction faction = worldState._winningFaction;
            if (faction != null)
                this._winningFaction = this._factions[faction.Id];

            // create array of sites
            int width = worldState.Sites.GetLength(0);
            int height = worldState.Sites.GetLength(1);
            this._sites = new ArrayEx<Site>(width, height);

            // create deep copy of each site
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++) {
                    Site newSite = (Site) worldState.Sites[x,y].Clone();
                    this._sites[x,y] = newSite;

                    // add all placed entities to global entity table
                    foreach (EntityCategory category in EntitySection.AllCategories) {
                        EntityList entities = newSite.GetEntities(category);
                        for (int i = 0; i < entities.Count; i++) {
                            Entity entity = entities[i];
                            WritableEntities.Add(entity.Id, entity);
                        }
                    }
                }

            // update site owners and copy unplaced entities
            for (int i = 0; i < this._factions.Count; i++) {

                // get old and new faction at this index
                Faction oldFaction = worldState._factions[i];
                Faction newFaction = this._factions[i];
                Debug.Assert(newFaction.Id == oldFaction.Id);

                // add owned sites to new faction
                SiteList sites = oldFaction.Sites;
                for (int j = 0; j < sites.Count; j++) {

                    // get new site at old site's location
                    PointI location = sites[j].Location;
                    Site site = Sites[location.X, location.Y];

                    // set owner and add to collection
                    site.SetOwnerUnchecked(newFaction);
                }

                // add owned entities to faction, copy unplaced entities
                CopyFactionEntities(oldFaction, newFaction, EntityCategory.Unit);
                CopyFactionEntities(oldFaction, newFaction, EntityCategory.Terrain);
                CopyFactionEntities(oldFaction, newFaction, EntityCategory.Upgrade);
            }
        }

        #endregion
        #region Private Fields

        // property backers
        private readonly FactionList _factions;
        private readonly EntityDictionary _entities;
        private readonly History _history;
        private readonly InstanceCountDictionary _instanceCounts;

        private bool _gameOver;
        private double _maxSiteValuation;
        private ArrayEx<Site> _sites;
        private Faction _winningFaction;

        #endregion
        #region Internal Properties
        #region TerrainChanged

        /// <summary>
        /// Gets a value indicating whether the <see cref="Site.Terrains"/> of any <see
        /// cref="Site"/> have changed.</summary>
        /// <value>
        /// <c>true</c> if the <see cref="Site.Terrains"/> collection of any <see cref="Sites"/>
        /// element has changed; otherwise, <c>false</c>. The default is <c>true</c>.</value>
        /// <remarks><para>
        /// When a terrain-dependent value is requested while <b>TerrainChanged</b> is <c>true</c>,
        /// all such values are recalculated and the property is reset to <c>false</c>.
        /// </para><para>
        /// When a terrain-dependent value is requested while <b>TerrainChanged</b> is <c>false</c>,
        /// the buffered result of the last calculation is returned instead.
        /// </para><para>
        /// Use the method <see cref="SetTerrainChanged"/> to set this property to <c>true</c>.
        /// </para></remarks>

        internal bool TerrainChanged { get; private set; }

        #endregion
        #region UnitAttributeModifiersChanged

        /// <summary>
        /// Gets a value indicating whether any <see cref="Faction.UnitAttributeModifiers"/> have
        /// changed since they were last applied to all matching <see cref="Entities"/>.</summary>
        /// <value>
        /// <c>true</c> if the <see cref="Faction.UnitAttributeModifiers"/> of any <see
        /// cref="Factions"/> element have changed since they were last applied to all matching <see
        /// cref="Entities"/>; otherwise, <c>false</c>. The default is <c>false</c>.</value>
        /// <remarks>
        /// <b>UnitAttributeModifiersChanged</b> is set to <c>true</c> whenever the <see
        /// cref="Entity.UpdateModifierMaps"/> method of the <see cref="Entity"/> class changes one
        /// or more <see cref="Faction.UnitAttributeModifiers"/> elements, and reset to <c>false</c>
        /// by the next call to <see cref="Entity.UpdateSelfAndUnitAttributes"/>.</remarks>

        internal bool UnitAttributeModifiersChanged { get; set; }

        #endregion
        #region WritableEntities

        /// <summary>
        /// Gets a writable list of all <see cref="Entities"/> in the <see cref="WorldState"/>.
        /// </summary>
        /// <value>
        /// The collection that is backing the <see cref="Entities"/> property.</value>

        internal EntityDictionary WritableEntities {
            [DebuggerStepThrough]
            get { return this._entities; }
        }

        #endregion
        #region WritableFactions

        /// <summary>
        /// Gets a writable list of all <see cref="Factions"/> in the <see cref="WorldState"/>.
        /// </summary>
        /// <value>
        /// The collection that is backing the <see cref="Factions"/> property.</value>

        internal FactionList WritableFactions {
            [DebuggerStepThrough]
            get { return this._factions; }
        }

        #endregion
        #endregion
        #region Public Properties
        #region ActiveFaction

        /// <summary>
        /// Gets the active <see cref="Faction"/>.</summary>
        /// <value>
        /// The <see cref="Faction"/> that corresponds to the current value of the <see
        /// cref="ActiveFactionIndex"/> property.</value>
        /// <remarks>
        /// <b>ActiveFaction</b> may return a null reference if all factions have been defeated.
        /// </remarks>

        public Faction ActiveFaction {
            [DebuggerStepThrough]
            get { return (Factions.Count == 0 ? null : Factions[ActiveFactionIndex]); }
        }

        #endregion
        #region ActiveFactionIndex

        /// <summary>
        /// Gets the index of the active <see cref="Faction"/>.</summary>
        /// <value>
        /// The zero-based index of the <see cref="WorldState.Factions"/> element that may issue the
        /// next command. The default is zero.</value>
        /// <remarks>
        /// <b>ActiveFactionIndex</b> usually indicates the same faction as the last element in the
        /// <see cref="World.History.Commands"/> collection, if any, but will indicate the
        /// subsequent faction if that element is an <see cref="EndTurnCommand"/>.</remarks>

        public int ActiveFactionIndex { get; private set; }

        #endregion
        #region CurrentTurn

        /// <summary>
        /// Gets the index of the current game turn.</summary>
        /// <value>
        /// The zero-based index of the current game turn. The default is zero.</value>
        /// <remarks>
        /// <b>CurrentTurn</b> usually indicates the same turn as the last command in the <see
        /// cref="World.History.Commands"/> collection, if any, but will indicate the next turn if
        /// that is an <see cref="EndTurnCommand"/> that caused the <see cref="ActiveFactionIndex"/>
        /// to revert to zero.</remarks>

        public int CurrentTurn { get; private set; }

        #endregion
        #region Entities

        /// <summary>
        /// Gets a list of all entities in the <see cref="WorldState"/>.</summary>
        /// <value>
        /// A read-only <see cref="EntityDictionary"/> that maps <see cref="Entity.Id"/> strings to
        /// all corresponding <see cref="Entity"/> objects in the <see cref="WorldState"/>.</value>
        /// <remarks><para>
        /// <b>Entities</b> never returns a null reference, and its <see cref="Entity"/> values are
        /// never null references.
        /// </para><para>
        /// <b>Entities</b> elements are added and removed by the <see cref="Entity"/> methods <see
        /// cref="Entity.SetUniqueName"/> and <see cref="Entity.Delete"/>, respectively.
        /// </para></remarks>

        public EntityDictionary Entities {
            [DebuggerStepThrough]
            get { return this._entities.AsReadOnly(); }
        }

        #endregion
        #region Factions

        /// <summary>
        /// Gets a list of all factions in the <see cref="WorldState"/>.</summary>
        /// <value>
        /// A read-only <see cref="FactionList"/> containing all surviving <see cref="Faction"/>
        /// objects in the <see cref="WorldState"/>, in the order in which they are activated during
        /// a turn.</value>
        /// <remarks><para>
        /// <b>Factions</b> never returns a null reference, and its elements are never null
        /// references. All keys and elements are unique.
        /// </para><para>
        /// <b>Factions</b> is initialized with the contents of the current <see
        /// cref="FactionSection"/>. As factions are eliminated, they are removed from the
        /// collection.</para></remarks>

        public FactionList Factions {
            [DebuggerStepThrough]
            get { return this._factions.AsReadOnly(); }
        }

        #endregion
        #region GameOver

        /// <summary>
        /// Gets a value indicating whether the game has ended.</summary>
        /// <value>
        /// <c>true</c> if the game has ended, either with a faction victory or with the defeat of
        /// all factions; otherwise, <c>false</c>. The default is <c>false</c>.</value>
        /// <remarks><para>
        /// <b>GameOver</b> does not change once it has been set to <c>true</c>. When the property
        /// is <c>true</c>, use the <see cref="WinningFaction"/> property to determine the
        /// victorious faction.
        /// </para><para>
        /// The <b>GameOver</b> and <see cref="WinningFaction"/> properties are set by the <see
        /// cref="CheckDefeat"/> and <see cref="CheckVictory"/> methods.</para></remarks>

        public bool GameOver {
            [DebuggerStepThrough]
            get { return this._gameOver; }
        }

        #endregion
        #region History

        /// <summary>
        /// Gets the command history that resulted in the <see cref="WorldState"/>.</summary>
        /// <value>
        /// The <see cref="World.History"/> containing the commands that resulted in the current
        /// <see cref="WorldState"/>, starting with the initial data of the current scenario.
        /// </value>
        /// <remarks>
        /// <b>History</b> never returns a null reference.</remarks>

        public History History {
            [DebuggerStepThrough]
            get { return this._history; }
        }

        #endregion
        #region InstanceCounts

        /// <summary>
        /// Gets the number of <see cref="Entity"/> objects based on each <see cref="EntityClass"/>.
        /// </summary>
        /// <value>
        /// A read-only <see cref="InstanceCountDictionary"/> that maps <see cref="EntityClass.Id"/>
        /// strings of <see cref="EntityClass"/> objects to the corresponding number of <see
        /// cref="Entity"/> objects that were created during the entire <see cref="History"/>.
        /// </value>
        /// <remarks>
        /// <b>InstanceCounts</b> never returns a null reference. Use the <see cref="AddInstance"/>
        /// method to increment the value associated with an entity class.</remarks>

        public InstanceCountDictionary InstanceCounts {
            [DebuggerStepThrough]
            get { return this._instanceCounts.AsReadOnly(); }
        }

        #endregion
        #region MaxSiteValuation

        /// <summary>
        /// Computes the maximum valuation of any <see cref="Site"/></summary>
        /// <value>
        /// The maximum <see cref="Site.Valuation"/> result for any <see cref="Sites"/> element.
        /// </value>
        /// <remarks><para>
        /// Dividing the <see cref="Site.Valuation"/> of any given <see cref="Sites"/> element by
        /// <b>MaxSiteValuation</b> scales the valuation to the standard interval [0,1].
        /// </para><para>
        /// <b>MaxSiteValuation</b> only recalculates the maximum valuation if <see
        /// cref="TerrainChanged"/> is <c>true</c>, and returns a buffered value otherwise.
        /// </para></remarks>

        public double MaxSiteValuation {
            get {
                if (TerrainChanged) {
                    double value = 0.0;

                    foreach (Site site in Sites)
                        value = Math.Max(value, site.Valuation);

                    this._maxSiteValuation = value;
                    TerrainChanged = false;
                }

                return this._maxSiteValuation;
            }
        }

        #endregion
        #region NewTurnStarted

        /// <summary>
        /// Gets a value indicating whether the last <see cref="AdvanceFaction"/> call has
        /// incremented <see cref="CurrentTurn"/>.</summary>
        /// <value>
        /// <c>true</c> if the last <see cref="AdvanceFaction"/> call has incremented <see
        /// cref="CurrentTurn"/>, or if <b>AdvanceFaction</b> has not yet been called; otherwise,
        /// <c>false</c>.</value>
        /// <remarks><para>
        /// <b>NewTurnStarted</b> defaults to <c>true</c> and is changed only by <see
        /// cref="AdvanceFaction"/>. Whenever this method executes, <b>NewTurnStarted</b> is set to
        /// <c>false</c>, except when <see cref="CurrentTurn"/> was incremented, in which case this
        /// property is reset to <c>true</c>.
        /// </para><para>
        /// Clients may use <b>NewTurnStarted</b> to determine whether the <see
        /// cref="ActiveFaction"/> was the first faction to become active during a new game turn.
        /// This method is more reliable than comparing <see cref="ActiveFactionIndex"/> to zero
        /// since the first faction might be defeated, in which case <b>ActiveFactionIndex</b>
        /// would be zero twice (or more) in a row during the same game turn.</para></remarks>

        public bool NewTurnStarted { get; private set; }

        #endregion
        #region Sites

        /// <summary>
        /// Gets an array of all map sites in the <see cref="WorldState"/>.</summary>
        /// <value>
        /// A read-only two-dimensional <see cref="ArrayEx{Site}"/> whose <see cref="Site"/> objects
        /// constitute the game map of the <see cref="WorldState"/>.</value>
        /// <remarks><para>
        /// <b>Sites</b> never returns a null reference, and its dimensions never change once the
        /// object has been constructed.
        /// </para><para>
        /// <b>Sites</b> returns an array whose dimensions equal those of the current <see
        /// cref="AreaSection.MapGrid"/>, and whose contents are initialized with the elements of
        /// the <see cref="AreaSection.Collection"/> of the current <see cref="AreaSection"/>.
        /// </para></remarks>

        public ArrayEx<Site> Sites {
            [DebuggerStepThrough]
            get { return this._sites.AsReadOnly(); }
        }

        #endregion
        #region WinningFaction

        /// <summary>
        /// Gets the <see cref="Faction"/> that has won the game.</summary>
        /// <value><para>
        /// The <see cref="Factions"/> element that has won the game.
        /// </para><para>-or-</para><para>
        /// A null reference to indicate that all <b>Factions</b> were defeated.</para></value>
        /// <exception cref="PropertyValueException">
        /// <see cref="GameOver"/> is still <c>false</c>.</exception>
        /// <remarks>
        /// The <see cref="GameOver"/> and <b>WinningFaction</b> properties are set by the <see
        /// cref="CheckVictory"/> method.</remarks>

        public Faction WinningFaction {
            [DebuggerStepThrough]
            get {
                if (!GameOver)
                    ThrowHelper.ThrowPropertyValueException(
                        "GameOver", Tektosyne.Strings.PropertyFalse);

                return this._winningFaction;
            }
        }

        #endregion
        #endregion
        #region Private Methods
        #region CopyFactionEntities

        /// <summary>
        /// Copies all entities of the specified <see cref="EntityCategory"/> from one specified
        /// <see cref="Faction"/> to another.</summary>
        /// <param name="oldFaction">
        /// The <see cref="Faction"/> whose entities to copy.</param>
        /// <param name="newFaction">
        /// The <see cref="Faction"/> to receive the entities copied from <paramref
        /// name="oldFaction"/>.</param>
        /// <param name="category">
        /// An <see cref="EntityCategory"/> value indicating which entities to copy.</param>
        /// <remarks><para>
        /// <b>CopyFactionEntities</b> creates a deep copy of any unplaced entities of the specified
        /// <paramref name="category"/> and adds the new <see cref="Entity"/> objects to the <see
        /// cref="Entities"/> collection. Placed entities are assumed to already have been copied, 
        /// and the new objects are retrieved from the <see cref="Sites"/> array.
        /// </para><para>
        /// In either case, <b>CopyFactionEntities</b> sets the owner of the new <see
        /// cref="Entity"/> objects to the specified <paramref name="newFaction"/>, which also adds
        /// the new objects to the corresponding entity collection of that faction.</para></remarks>

        private void CopyFactionEntities(Faction oldFaction,
            Faction newFaction, EntityCategory category) {

            foreach (Entity oldEntity in oldFaction.GetEntities(category)) {
                Entity newEntity = null;
                Site site = oldEntity.Site;

                if (site == null) {
                    // create deep copy of unplaced entity
                    newEntity = (Entity) oldEntity.Clone();

                    // add entity to global entity table
                    WritableEntities.Add(newEntity.Id, newEntity);
                }
                else {
                    // get new entity at old entity's site
                    PointI location = site.Location;
                    site = Sites[location.X, location.Y];
                    newEntity = site.GetEntities(category)[oldEntity.Id];
                }

                // set owner and add to collection
                newEntity.SetOwnerUnchecked(newFaction);
            }
        }

        #endregion
        #region CreateAreaEntities

        /// <summary>
        /// Creates the entities at the specified <see cref="Site"/> from the specified <see
        /// cref="Area"/>, using the specified factory method.</summary>
        /// <param name="site">
        /// The <see cref="Site"/> whose entity stacks to create.</param>
        /// <param name="area">
        /// An <see cref="Area"/> indicating the entities to create on <paramref name="site"/>.
        /// </param>
        /// <param name="createEntity">
        /// The <see cref="CreateEntityCallback"/> method that is invoked to create <see
        /// cref="Entity"/> objects.</param>
        /// <exception cref="ArgumentException">
        /// <paramref name="area"/> contains <see cref="Area.Units"/> but defines neither an <see
        /// cref="Area.Owner"/> nor an <see cref="Area.UnitOwner"/>.</exception>

        private void CreateAreaEntities(Site site,
            Area area, CreateEntityCallback createEntity) {

            // assign site owner
            if (area.Owner.Length > 0) {

                // find faction that owns the area
                Faction faction;
                Factions.TryGetValue(area.Owner, out faction);

                // create placeholder faction for editor if necessary
                if (faction == null && ApplicationInfo.IsEditor) {
                    FactionClass factionClass = new FactionClass(area.Owner);
                    faction = Faction.CreateFaction(factionClass);
                }

                // change site ownership
                Debug.Assert(faction != null);
                site.SetOwner(faction);
            }

            /*
             * Each site must contain exactly one background terrain as its first terrain.
             * We enforce this invariant by deleting any present terrains whenever a new
             * background terrain is encountered.
             */

            EntityTemplateList source = area.Terrains;
            if (source.Count > 0) {

                // determine whether area specifies new background
                string id = source[0].EntityClass;
                EntityClass firstTerrain;
                MasterSection.Instance.Entities.Terrains.TryGetValue(id, out firstTerrain);

                // null value checks required for editing mode!
                if (firstTerrain != null && ((TerrainClass) firstTerrain).IsBackground) {

                    // delete existing terrains if new background
                    for (int i = site.Terrains.Count - 1; i >= 0; i--) {
                        Entity terrain = site.Terrains[i];

                        // clear properties without validation
                        terrain.SetOwner(null, false);
                        terrain.SetSite(null, false);

                        WritableEntities.Remove(terrain.Id);
                    }
                }

                // add terrains to site and site owner
                for (int i = 0; i < source.Count; i++)
                    CreateEntity(source[i], createEntity, site, site.Owner);
            }

            // unit owner defaults to area owner
            Faction unitOwner = site.Owner;

            // check if different unit owner specified
            if (area.UnitOwner.Length > 0 &&
                (unitOwner == null || unitOwner.Id != area.UnitOwner)) {

                // find faction that owns the area
                Factions.TryGetValue(area.UnitOwner, out unitOwner);

                // create placeholder faction for editor if necessary
                if (unitOwner == null && ApplicationInfo.IsEditor) {
                    FactionClass factionClass = new FactionClass(area.UnitOwner);
                    unitOwner = Faction.CreateFaction(factionClass);
                }
            }

            // instantiate unit classes
            source = area.Units;
            for (int i = 0; i < source.Count; i++) {
                EntityTemplate template = source[i];

                // check if valid unit owner specified
                if (!ApplicationInfo.IsEditor && unitOwner == null)
                    ThrowHelper.ThrowArgumentExceptionWithFormat(
                        "area", Global.Strings.ErrorOwnerAreaNone, site);

                // add unit to site and unit owner
                CreateEntity(template, createEntity, site, unitOwner);
            }

            // add effects to site and site owner
            source = area.Effects;
            for (int i = 0; i < source.Count; i++)
                CreateEntity(source[i], createEntity, site, site.Owner);
        }

        #endregion
        #region CreateEntity

        /// <summary>
        /// Creates a new <see cref="Entity"/> instance based on the specified <see
        /// cref="EntityTemplate"/> with the specified owning <see cref="Faction"/> and <see
        /// cref="Site"/>.</summary>
        /// <param name="template">
        /// The <see cref="EntityTemplate"/> providing initial data for the new instance.</param>
        /// <param name="createEntity">
        /// The <see cref="CreateEntityCallback"/> method that is invoked to create the new
        /// instance.</param>
        /// <param name="site">
        /// The initial value for the <see cref="Entity.Site"/> property of the new <see
        /// cref="Entity"/>.</param>
        /// <param name="owner">
        /// The initial value for the <see cref="Entity.Owner"/> property of the new <see
        /// cref="Entity"/>.</param>
        /// <remarks>
        /// If <see cref="ApplicationInfo.IsEditor"/> is <c>true</c>, <b>CreateEntity</b> creates a
        /// placeholder <see cref="EntityClass"/> if the <see cref="EntityTemplate.EntityClass"/> of
        /// the specified <paramref name="template"/> is not found in the current <see
        /// cref="EntitySection"/>.</remarks>

        private void CreateEntity(EntityTemplate template,
            CreateEntityCallback createEntity, Site site, Faction owner) {

            // retrieve entity class for entity template
            EntityClass entityClass = MasterSection.Instance.Entities.GetEntity(template);

            // create placeholder class for editor if necessary
            if (entityClass == null && ApplicationInfo.IsEditor)
                entityClass = EntityClass.Create(template.EntityClass, template.Category);

            // create entity with specified properties
            Entity entity = createEntity(entityClass);
            entity.SetUniqueIdentifier(this);
            entity.SetEntityTemplate(template);

            // add entity to site and owner
            if (site != null) entity.SetSite(site, false);
            if (owner != null) entity.SetOwner(owner, false);
        }

        #endregion
        #endregion
        #region Internal Methods
        #region AddInstance

        /// <summary>
        /// Adds a new instance of the specified <see cref="EntityClass"/> to the <see
        /// cref="InstanceCounts"/> collection.</summary>
        /// <param name="entityClass">
        /// The <see cref="EntityClass"/> to instantiate.</param>
        /// <returns>
        /// The old value that the <see cref="InstanceCounts"/> collection had associated with the
        /// specified <paramref name="entityClass"/>.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="entityClass"/> is a null reference.</exception>
        /// <remarks>
        /// <b>AddInstance</b> increments the value that the <see cref="InstanceCounts"/> collection
        /// associates with the specified <paramref name="entityClass"/> but returns the value
        /// <em>before</em> incrementation.</remarks>

        internal int AddInstance(EntityClass entityClass) {
            if (entityClass == null)
                ThrowHelper.ThrowArgumentNullException("entityClass");

            int count;
            this._instanceCounts.TryGetValue(entityClass.Id, out count);
            this._instanceCounts[entityClass.Id] = count + 1;

            return count;
        }

        #endregion
        #region AdvanceFaction

        /// <summary>
        /// Activates the next <see cref="Faction"/>.</summary>
        /// <remarks><para>
        /// <b>AdvanceFaction</b> increments <see cref="ActiveFactionIndex"/> and sets <see
        /// cref="NewTurnStarted"/> to <c>false</c> if there are any <see cref="Factions"/> left to
        /// activate during the current game turn.
        /// </para><para>
        /// Otherwise, <b>AdvanceFaction</b> resets <b>ActiveFactionIndex</b> to zero, increments
        /// <see cref="CurrentTurn"/>, and sets <b>NewTurnStarted</b> to <c>true</c> instead.
        /// </para></remarks>

        internal void AdvanceFaction() {
            Debug.Assert(Factions.Count > 0);

            if (++ActiveFactionIndex >= Factions.Count) {
                ActiveFactionIndex = 0;
                ++CurrentTurn;
                NewTurnStarted = true;
            } else
                NewTurnStarted = false;
        }

        #endregion
        #region CheckDefeat

        /// <summary>
        /// Checks all <see cref="Factions"/> for resignation and defeat conditions.</summary>
        /// <param name="command">
        /// The <see cref="Command"/> being executed.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="command"/> is a null reference.</exception>
        /// <exception cref="PropertyValueException">
        /// <see cref="GameOver"/> is already <c>true</c>.</exception>
        /// <remarks><para>
        /// <b>CheckDefeat</b> checks all surviving <see cref="Factions"/> for resignation or
        /// defeat. Any <b>Factions</b> element whose <see cref="Faction.IsResigned"/> flag or <see
        /// cref="Faction.CheckDefeat"/> method returns <c>true</c> is deleted from the game.
        /// </para><para>
        /// If no surviving <b>Factions</b> remain, <b>CheckDefeat</b> also sets <see
        /// cref="GameOver"/> to <c>true</c> and <see cref="WinningFaction"/> to a null reference.
        /// Otherwise, these properties remain unchanged.</para></remarks>

        internal void CheckDefeat(Command command) {
            if (command == null)
                ThrowHelper.ThrowArgumentNullException("command");
            if (GameOver)
                ThrowHelper.ThrowPropertyValueException("GameOver", Tektosyne.Strings.PropertyTrue);

            Debug.Assert(command.Context.WorldState == this);
            FactionList defeated = null;

            // check for resigned and defeated factions
            for (int i = 0; i < Factions.Count; i++) {
                Faction faction  = Factions[i];

                if (faction.IsResigned || faction.CheckDefeat(command)) {
                    if (defeated == null)
                        defeated = new FactionList(Factions.Count);
                    defeated.Add(faction);
                }
            }

            // remove defeated factions
            if (defeated != null)
                for (int i = 0; i < defeated.Count; i++)
                    defeated[i].Delete(command);

            // check for no survivors
            if (Factions.Count == 0)
                command.SetWinningFaction(null);
        }

        #endregion
        #region CheckVictory

        /// <summary>
        /// Checks the <see cref="ActiveFaction"/> for victory conditions.</summary>
        /// <param name="command">
        /// The <see cref="Command"/> being executed.</param>
        /// <returns>
        /// The new value of the <see cref="GameOver"/> property.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="command"/> is a null reference.</exception>
        /// <exception cref="PropertyValueException">
        /// <see cref="GameOver"/> is already <c>true</c>.</exception>
        /// <remarks>
        /// <b>CheckVictory</b> invokes <see cref="Faction.CheckVictory"/> on the <see
        /// cref="ActiveFaction"/>. If the result is <c>true</c>, <b>CheckVictory</b> sets <see
        /// cref="GameOver"/> to <c>true</c> and <see cref="WinningFaction"/> to
        /// <b>ActiveFaction</b>. Otherwise, these properties remain unchanged.</remarks>

        internal void CheckVictory(Command command) {
            if (command == null)
                ThrowHelper.ThrowArgumentNullException("command");
            if (GameOver)
                ThrowHelper.ThrowPropertyValueException("GameOver", Tektosyne.Strings.PropertyTrue);

            Debug.Assert(command.Context.WorldState == this);

            // check if active faction has won
            if (ActiveFaction.CheckVictory(command))
                command.SetWinningFaction(ActiveFaction.Id);
        }

        #endregion
        #region DeleteFaction

        /// <summary>
        /// Deletes the specified <see cref="Faction"/> from the <see cref="Factions"/> collection.
        /// </summary>
        /// <param name="faction">
        /// The <see cref="Faction"/> to delete from the <see cref="Factions"/> collection.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="faction"/> is a null reference.</exception>
        /// <remarks><para>
        /// <b>DeleteFaction</b> sets the <see cref="ActiveFactionIndex"/> to the position of the
        /// next surviving <see cref="Factions"/> element after <paramref name="faction"/> has been
        /// removed from the <b>Factions</b> collection.
        /// </para><para>
        /// <b>DeleteFaction</b> does nothing if the specified <paramref name="faction"/> is not
        /// found in the <see cref="Factions"/> collection.</para></remarks>

        internal void DeleteFaction(Faction faction) {
            if (faction == null)
                ThrowHelper.ThrowArgumentNullException("faction");

            int index = Factions.IndexOf(faction);
            if (index < 0) return;

            // adjust active faction index if necessary
            if (ActiveFactionIndex > index)
                --ActiveFactionIndex;
            else if (ActiveFactionIndex == index && index == Factions.Count - 1)
                ActiveFactionIndex = 0;

            WritableFactions.RemoveAt(index);
        }

        #endregion
        #region SetTerrainChanged

        /// <summary>
        /// Sets the <see cref="TerrainChanged"/> property.</summary>
        /// <remarks><para>
        /// <b>SetTerrainChanged</b> sets the <see cref="TerrainChanged"/> property to <c>true</c>,
        /// causing the recalculation of all terrain-dependent values upon the next request for any
        /// such value.
        /// </para><para>
        /// <b>SetTerrainChanged</b> is called by the <see cref="Site.SetTerrainChanged"/> method of
        /// the <see cref="Site"/> class and should not be called by other clients.</para></remarks>

        internal void SetTerrainChanged() {
            TerrainChanged = true;
        }

        #endregion
        #region SetWinningFaction

        /// <summary>
        /// Sets the <see cref="WinningFaction"/> property to the specified value and ends the game.
        /// </summary>
        /// <param name="faction">
        /// The new value for the <see cref="WinningFaction"/> property. This argument may be a null
        /// reference.</param>
        /// <returns>
        /// <c>true</c> if the <see cref="WinningFaction"/> property was changed; otherwise,
        /// <c>false</c>.</returns>
        /// <remarks>
        /// <b>SetWinningFaction</b> also sets the <see cref="GameOver"/> property to <c>true</c>.
        /// </remarks>

        internal bool SetWinningFaction(Faction faction) {

            if (GameOver && WinningFaction == faction)
                return false;

            this._winningFaction = faction;
            this._gameOver = true;

            return true;
        }

        #endregion
        #endregion
        #region CreateArea(PointI)

        /// <overloads>
        /// Creates a new <see cref="Area"/> object from a single <see cref="Sites"/> element.
        /// </overloads>
        /// <summary>
        /// Creates a new <see cref="Area"/> object from the <see cref="Sites"/> element at the
        /// specified coordinates.</summary>
        /// <param name="location">
        /// The coordinates of a <see cref="Sites"/> element and of the new <see cref="Area"/>
        /// object.</param>
        /// <returns>
        /// A new <see cref="Area"/> object containing all data of the <see cref="Sites"/> element
        /// at the specified <paramref name="location"/> that can be represented with
        /// <b>Scenario</b> types.</returns>
        /// <remarks>
        /// Please refer to the four-parameter overload of <see cref="CreateArea(Int32, Int32,
        /// Int32, Int32)"/> for details.</remarks>

        public Area CreateArea(PointI location) {
            return CreateArea(location.X, location.Y, location.X, location.Y);
        }

        #endregion
        #region CreateArea(Int32, Int32, Int32, Int32)

        /// <summary>
        /// Creates a new <see cref="Area"/> object with the specified coordinates from the <see
        /// cref="Sites"/> element at the specified coordinates.</summary>
        /// <param name="sourceX">
        /// The x-coordinate of a <see cref="Sites"/> element.</param>
        /// <param name="sourceY">
        /// The y-coordinate of a <see cref="Sites"/> element.</param>
        /// <param name="targetX">
        /// The x-coordinate of the new <see cref="Area"/> object.</param>
        /// <param name="targetY">
        /// The y-coordinate of the new <see cref="Area"/> object.</param>
        /// <returns>
        /// A new <see cref="Area"/> object containing all data of the <see cref="Sites"/> element
        /// at location (<paramref name="sourceX"/>, <paramref name="sourceY"/>) that can be
        /// represented with <b>Scenario</b> types.</returns>
        /// <remarks><para>
        /// <b>CreateArea</b> returns a new <see cref="Area"/> object whose <see
        /// cref="Area.Bounds"/> collection contains a single element with a <see
        /// cref="RectI.Location"/> of (<paramref name="targetX"/>, <paramref name="targetY"/>) and
        /// a <see cref="RectI.Size"/> of (1,1). All other properties are set to the data of the
        /// <see cref="Site"/> at location (<paramref name="sourceX"/>, <paramref name="sourceY"/>)
        /// within the current <see cref="Sites"/> array.
        /// </para><para>
        /// <b>CreateArea</b> does not copy those elements of the site's terrain stack that are
        /// identical with the default terrain stack, as defined by the current <see
        /// cref="AreaSection"/>. All <b>Hexkit.World</b> references are converted to the
        /// corresponding <b>Hexkit.Scenario</b> identifiers.
        /// </para><para>
        /// <b>CreateArea</b> returns a null reference if the specified <paramref name="sourceX"/>
        /// and <paramref name="sourceY"/> values denote an invalid location in the <see
        /// cref="Sites"/> array.</para></remarks>

        public Area CreateArea(int sourceX, int sourceY, int targetX, int targetY) {

            // retrieve site at specified coordinates
            Site site = GetSite(new PointI(sourceX, sourceY));
            if (site == null) return null;

            // copy site owner if present
            string owner = (site.Owner == null ? null : site.Owner.Id);

            // copy unit owner if present
            string unitOwner = null;
            if (site.Units.Count > 0) {
                unitOwner = site.Units[0].Owner.Id;
                if (owner == unitOwner) unitOwner = null;
            }

            // create new area at specified coordinates
            Area area = new Area(targetX, targetY, owner, unitOwner);

            // copy terrain if different from default terrain
            EntityTemplateList defaultTerrains = MasterSection.Instance.Areas.Terrains;
            EntityList source = site.Terrains;
            EntityTemplateList target = area.Terrains;

            for (int i = 0; i < source.Count; i++) {
                var template = source[i].ToEntityTemplate();
                var defaultTemplate = (i < defaultTerrains.Count ? defaultTerrains[i] : null);

                // copy terrain with non-default properties
                if (!template.Equals(defaultTemplate))
                    target.Add(template);
            }

            // copy all classes of non-terrain entities
            source = site.Units; target = area.Units;
            for (int i = 0; i < source.Count; i++)
                target.Add(source[i].ToEntityTemplate());

            source = site.Effects; target = area.Effects;
            for (int i = 0; i < source.Count; i++)
                target.Add(source[i].ToEntityTemplate());

            return area;
        }

        #endregion
        #region CreateAreaSection

        /// <summary>
        /// Creates a new <see cref="AreaSection"/> object from the entire <see cref="Sites"/>
        /// array, shifted by the specified coordinate offsets.</summary>
        /// <param name="left">
        /// The number of columns to add or remove at the left map edge.</param>
        /// <param name="top">
        /// The number of rows to add or remove at the top map edge.</param>
        /// <param name="right">
        /// The number of columns to add or remove at the right map edge.</param>
        /// <param name="bottom">
        /// The number of rows to add or remove at the bottom map edge.</param>
        /// <returns>
        /// A new <see cref="AreaSection"/> object, containing all the data of the <see
        /// cref="Sites"/> array that can be represented with <b>Hexkit.Scenario</b> types.
        /// </returns>
        /// <remarks><para>
        /// <b>CreateAreaSection</b> serves two purposes: acquire user changes made on the Areas
        /// page of Hexkit Editor, and create "snapshots" of the current game map within Hexkit
        /// Game.
        /// </para><para>
        /// <b>CreateAreaSection</b> copies only a subset of the <see cref="Sites"/> array if
        /// <paramref name="left"/> or <paramref name="top"/> is positive, and if <paramref
        /// name="right"/> or <paramref name="bottom"/> is negative.
        /// </para><para>
        /// Conversely, the copy is padded with empty <see cref="Area"/> objects if <paramref
        /// name="left"/> or <paramref name="top"/> is negative, and if <paramref name="right"/> or
        /// <paramref name="bottom"/> is positive.
        /// </para><para>
        /// <b>CreateAreaSection</b> does not copy those elements of a site's terrain stack that are
        /// identical with the default terrain stack, as defined by the current <see
        /// cref="AreaSection"/>. All <b>Hexkit.World</b> references are converted to the
        /// corresponding <b>Hexkit.Scenario</b> identifiers.</para></remarks>

        public AreaSection CreateAreaSection(int left, int top, int right, int bottom) {

            // compute horizontal adjustments
            int sourceLeft = (left > 0 ? left : 0);
            int targetLeft = (left < 0 ? -left : 0);
            int sourceRight = (right < 0 ? right : 0);

            // compute vertical adjustments
            int sourceTop = (top > 0 ? top : 0);
            int targetTop = (top < 0 ? -top : 0);
            int sourceBottom = (bottom < 0 ? bottom : 0);

            // get current map dimensions
            int width = Sites.GetLength(0);
            int height = Sites.GetLength(1);

            // compute size of map section to copy
            int sourceWidth = width - sourceLeft + sourceRight;
            int sourceHeight = height - sourceTop + sourceBottom;

            // compute size of target area buffer
            int targetWidth = width - left + right;
            int targetHeight = height - top + bottom;

            // create area buffer for terrain packing
            Area[,] areas = new Area[targetWidth, targetHeight];

            // translate existing site contents
            for (int x = 0; x < sourceWidth; x++)
                for (int y = 0; y < sourceHeight; y++) {

                    int sourceX = x + sourceLeft, sourceY = y + sourceTop;
                    int targetX = x + targetLeft, targetY = y + targetTop;

                    areas[targetX, targetY] = CreateArea(sourceX, sourceY, targetX, targetY);
                }

            // create empty areas for new sites
            for (int x = 0; x < targetWidth; x++)
                for (int y = 0; y < targetHeight; y++)
                    if (areas[x,y] == null)
                        areas[x,y] = new Area(x, y, "", "");

            // create new Areas section with packed terrain
            AreaSection section = new AreaSection(MasterSection.Instance.Areas);
            section.PackAreas(areas);

            return section;
        }

        #endregion
        #region CreateSite

        /// <summary>
        /// Creates a new <see cref="Site"/> from the specified <see cref="Area"/>.</summary>
        /// <param name="area">
        /// An <see cref="Area"/> providing the location and contents of the new <see cref="Site"/>.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="area"/> is a null reference.</exception>
        /// <exception cref="InvalidOperationException">
        /// <see cref="ApplicationInfo.IsEditor"/> is <c>false</c>.</exception>
        /// <remarks><para>
        /// <b>CreateSite</b> creates a new <see cref="Site"/> whose location within the <see
        /// cref="Sites"/> array is indicated by the <see cref="RectI.Location"/> property of the
        /// first element of the <see cref="Area.Bounds"/> collection of the specified <paramref
        /// name="area"/>.
        /// </para><para>
        /// The site receives the default terrain stack defined by the current <see
        /// cref="AreaSection"/>, and then any entity stacks and other data defined by the specified
        /// <paramref name="area"/>. All <see cref="Entity"/> objects are created using the default
        /// factory method, <see cref="Entity.CreateEntity"/>.
        /// </para><para>
        /// <b>CreateSite</b> does nothing if the <b>Bounds</b> collection of the specified
        /// <paramref name="area"/> is empty, or if the <b>Location</b> property of the first
        /// element is not a valid location within the current <b>Sites</b> array.</para></remarks>

        public void CreateSite(Area area) {
            ApplicationInfo.CheckEditor();

            if (area == null)
                ThrowHelper.ThrowArgumentNullException("area");

            // retrieve target coordinates
            if (area.Bounds.Count == 0) return;
            RectI bounds = area.Bounds[0];

            // remove old site from its owner
            Site oldSite = GetSite(bounds.Location);
            if (oldSite == null) return;
            oldSite.SetOwner(null);

            // use default entity factory
            CreateEntityCallback createEntity = new CreateEntityCallback(Entity.CreateEntity);

            // create new site at this location
            Site site = new Site(bounds.Location);
            this._sites[bounds.X, bounds.Y] = site;

            // place default terrain stack on this site
            EntityTemplateList source = MasterSection.Instance.Areas.Terrains;
            for (int i = 0; i < source.Count; i++)
                CreateEntity(source[i], createEntity, site, null);

            // transfer area contents and owner to site
            CreateAreaEntities(site, area, createEntity);
        }

        #endregion
        #region GetSite

        /// <summary>
        /// Returns the <see cref="Sites"/> element with the specified map coordinates.</summary>
        /// <param name="location">
        /// The coordinates of the <see cref="Sites"/> element to return.</param>
        /// <returns><para>
        /// The <see cref="Site"/> at the specified <paramref name="location"/> within the <see
        /// cref="Sites"/> array.
        /// </para><para>-or-</para><para>
        /// A null reference if the specified <paramref name="location"/> is not a valid <see
        /// cref="Sites"/> index.</para></returns>

        public Site GetSite(PointI location) {
            Site site = Sites.GetValueOrDefault(location.X, location.Y);
            Debug.Assert(site == null || site.Location == location);
            return site;
        }

        #endregion
        #region Initialize

        /// <summary>
        /// Initializes all <see cref="WorldState"/> data from the data of the current scenario.
        /// </summary>
        /// <param name="events">
        /// An optional <see cref="TaskEvents"/> object used for progress display.</param>
        /// <exception cref="PropertyValueException"><para>
        /// The current scenario <see cref="MasterSection.Instance"/> is a null reference.
        /// </para><para>-or-</para><para>
        /// <see cref="ApplicationInfo.IsEditor"/> is <c>false</c>, and the current <see
        /// cref="RuleScript.Factory"/> is a null reference.</para></exception>
        /// <remarks><para>
        /// If <see cref="ApplicationInfo.IsEditor"/> is <c>true</c>, <b>Initialize</b> uses the
        /// default factory methods provided by the <see cref="Faction"/> and <see cref="Entity"/>
        /// classes to create new factions and entities, respectively. When <b>Initialize</b> has
        /// returned, the <see cref="WorldState"/> will be suitable to back a map view in Hexkit
        /// Editor, but some of the data required to play a game will be missing.
        /// </para><para>
        /// If <see cref="ApplicationInfo.IsEditor"/> is <c>false</c>, <b>Initialize</b> uses the
        /// factory methods provided by the current rule script's <see cref="RuleScript.Factory"/>
        /// to create new factions and entities, respectively. When <b>Initialize</b> has returned,
        /// the <see cref="WorldState"/> will be suitable to start a new game or to replay any
        /// stored history commands.
        /// </para><para>
        /// <b>Initialize</b> raises the <see cref="TaskEvents.TaskProgress"/> event of the
        /// <paramref name="events"/> object seven times with an increment of one.</para></remarks>

        public void Initialize(TaskEvents events) {

            if (MasterSection.Instance == null)
                ThrowHelper.ThrowPropertyValueException(
                    "MasterSection.Instance", Tektosyne.Strings.PropertyNull);

            if (events != null)
                events.OnTaskMessage(this, Global.Strings.StatusWorldInitializing);

            CreateFactionCallback createFaction = null;
            CreateEntityCallback createEntity = null;

            if (ApplicationInfo.IsEditor) {
                // use default factory methods
                createFaction = new CreateFactionCallback(Faction.CreateFaction);
                createEntity = new CreateEntityCallback(Entity.CreateEntity);
            }
            else {
                // retrieve rule script factory
                IRulesFactory factory = (IRulesFactory) MasterSection.Instance.Rules.Factory;
                if (factory == null)
                    ThrowHelper.ThrowPropertyValueException(
                        "MasterSection.Instance.Rules.Factory", Tektosyne.Strings.PropertyNull);

                // use rule script factory methods
                createFaction = new CreateFactionCallback(factory.CreateFaction);
                createEntity = new CreateEntityCallback(factory.CreateEntity);
            }

            // create or refresh entity class cache
            if (EntityClassCache.IsEmpty || ApplicationInfo.IsEditor)
                EntityClassCache.Load();

            // fetch references to scenario sections
            AreaSection areas = MasterSection.Instance.Areas;
            FactionSection factions = MasterSection.Instance.Factions;

            // buffer for entity template collections
            EntityTemplateList source = null;

            // create all factions specified by scenario
            for (int i = 0; i < factions.Collection.Count; i++) {
                FactionClass factionClass = factions.Collection.GetByIndex(i);

                Faction faction = createFaction(factionClass);
                WritableFactions.Add(faction);

                // Hexkit Editor only needs placed objects
                if (!ApplicationInfo.IsEditor) {

                    // create initially owned unplaced units
                    source = factionClass.Units;
                    for (int j = 0; j < source.Count; j++)
                        CreateEntity(source[j], createEntity, null, faction);

                    // create initially owned unplaced terrains
                    source = factionClass.Terrains;
                    for (int j = 0; j < source.Count; j++)
                        CreateEntity(source[j], createEntity, null, faction);

                    // create initially owned unplaced upgrades
                    source = factionClass.Upgrades;
                    for (int j = 0; j < source.Count; j++)
                        CreateEntity(source[j], createEntity, null, faction);
                }
            }

            // create site array with map dimensions
            this._sites = areas.MapGrid.CreateArrayEx<Site>();
            int count = Sites.Length;

            // compute thresholds for progress counter
            int progressIncrement = (count - 1) / 4;
            int progress = progressIncrement;
            string progressMessage = Global.Strings.StatusSitesCreatingCount;

            if (events != null) {
                events.Timer.Restart();
                events.OnTaskMessage(this, Global.Strings.StatusSitesCreating);
                events.OnTaskProgress(this, 1);
            }

            // create default terrain in each site
            SizeI mapSize = areas.MapGrid.Size;
            for (int x = 0; x < mapSize.Width; x++)
                for (int y = 0; y < mapSize.Height; y++) {

                    if (events != null) {
                        int index = x * mapSize.Height + y;

                        // advance progress counter
                        if (index >= progress) {
                            events.OnTaskProgress(this, 1);
                            progress += progressIncrement;
                        }

                        // show running count every 0.5 seconds
                        if (events.RestartTimer(500L))
                            events.OnTaskMessage(this, progressMessage, index, count);
                    }

                    // create new site at this location
                    Site site = new Site(new PointI(x,y));
                    this._sites[x,y] = site;

                    // create default terrain stack
                    source = areas.Terrains;
                    for (int i = 0; i < source.Count; i++)
                        CreateEntity(source[i], createEntity, site, null);
                }

            if (events != null)
                events.OnTaskMessage(this, Global.Strings.StatusEntitiesCreating);

            // create entities as defined by Areas section
            for (int i = 0; i < areas.Collection.Count; i++) {
                Area area = areas.Collection[i];

                // iterate through all bounds of this area
                for (int j = 0; j < area.Bounds.Count; j++) {
                    RectI bounds = area.Bounds[j];

                    // iterate through all columns of these bounds
                    for (int x = bounds.Left; x < bounds.Right; x++) {

                        // column check required for editing mode!
                        if (x < 0 || x >= mapSize.Width) continue;

                        // iterate through all rows of current column
                        for (int y = bounds.Top; y < bounds.Bottom; y++) {

                            // row check required for editing mode!
                            if (y < 0 || y >= mapSize.Height) continue;

                            // create area entities on current site
                            CreateAreaEntities(Sites[x,y], area, createEntity);
                        }
                    }
                }
            }

            if (events != null) events.OnTaskProgress(this, 1);

            // assign unique names to units where necessary
            if (!ApplicationInfo.IsEditor) {
                var classCounts = new Dictionary<EntityClass, Int32>();

                // traverse owned units of all factions
                foreach (Faction faction in Factions) {
                    classCounts.Clear();

                    // establish owned unit count for each class
                    foreach (Entity unit in faction.Units) {
                        int classCount;
                        if (classCounts.TryGetValue(unit.EntityClass, out classCount))
                            classCounts[unit.EntityClass] = classCount + 1;
                        else
                            classCounts.Add(unit.EntityClass, 1);
                    }

                    /*
                     * Assign faction-unique name to owned units with class default name
                     * whose class is either buildable (i.e. may have multiple instances
                     * in the future) or already has multiple existing instances.
                     */

                    foreach (Unit unit in faction.Units)
                        if (unit.Name == unit.DefaultName &&
                            (unit.CanBuild || classCounts[unit.EntityClass] > 1))
                            unit.SetUniqueName(faction);
                }

                // initialize all modifier maps
                foreach (Entity entity in Entities.Values)
                    entity.UpdateModifierMaps(this, true);
            }

            if (events != null) events.OnTaskProgress(this, 1);
        }

        #endregion
        #region ICloneable Members

        /// <summary>
        /// Creates a new <see cref="WorldState"/> object that is a deep copy of the current
        /// instance.</summary>
        /// <returns>
        /// A new <see cref="WorldState"/> object that is a deep copy of the current instance.
        /// </returns>
        /// <remarks><para>
        /// <b>Clone</b> processes the properties of the current instance as follows:
        /// </para><list type="table"><listheader>
        /// <term>Property</term><description>Action</description>
        /// </listheader><item>
        /// <term><see cref="ActiveFactionIndex"/><br/> <see cref="CurrentTurn"/><br/> <see
        /// cref="GameOver"/><br/> <see cref="MaxSiteValuation"/><br/> <see
        /// cref="NewTurnStarted"/><br/> <see cref="TerrainChanged"/> <see
        /// cref="UnitAttributeModifiersChanged"/></term>
        /// <description>Values copied to the new instance.</description>
        /// </item><item>
        /// <term><see cref="InstanceCounts"/></term>
        /// <description>Shallow copy assigned to the new instance.</description>
        /// </item><item>
        /// <term><see cref="Entities"/><br/> <see cref="Factions"/><br/> <see cref="History"/><br/>
        /// <see cref="Sites"/></term>
        /// <description>Deep copies assigned to the new instance.</description>
        /// </item><item>
        /// <term><see cref="ActiveFaction"/><br/> <see cref="WinningFaction"/></term>
        /// <description>Values provided by <b>Factions</b>.</description>
        /// </item></list></remarks>

        public object Clone() {
            return new WorldState(this);
        }

        #endregion
    }
}
