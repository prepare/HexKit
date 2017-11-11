using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Media;

using Tektosyne;
using Tektosyne.Collections;
using Tektosyne.Geometry;

using Hexkit.Global;
using Hexkit.Scenario;
using Hexkit.World.Commands;
using Hexkit.World.Instructions;

using FormsSortOrder = System.Windows.Forms.SortOrder;

namespace Hexkit.World {
    #region Type Aliases

    using CategorizedValueDictionary = KeyValueList<String, CategorizedValue>;
    using EntityClassDictionary = SortedListEx<String, EntityClass>;
    using EntityClassList = KeyedList<String, EntityClass>;
    using EntityList = KeyedList<String, Entity>;
    using FactionList = KeyedList<String, Faction>;
    using IdentifierList = ListEx<String>;
    using InstanceCountDictionary = SortedListEx<String, Int32>;
    using SiteList = KeyedList<PointI, Site>;
    using VariableValueDictionary = SortedListEx<String, Int32>;

    #endregion

    /// <summary>
    /// Represents a player faction.</summary>
    /// <remarks><para>
    /// <b>Faction</b> represents one of the competing sides in a game. <b>Faction</b> objects are
    /// singleton "instances" of <see cref="FactionClass"/> objects defined by the current <see
    /// cref="FactionSection"/>.
    /// </para><para>
    /// The agents of Hexkit Game are called "factions" rather than "players" because a single
    /// (human or computer) player can control multiple factions. This allows players to take over
    /// defeated sides, or simulating allies that do not share organization and equipment.
    /// </para></remarks>

    public class Faction: ICloneable, IKeyedValue<String> {
        #region Faction()

        /// <summary>
        /// Partially initializes a new instance of the <see cref="Faction"/> class.</summary>
        /// <remarks>
        /// This constructor performs initialization tasks that are common to both protected
        /// constructors. Always use one of these constructors to initialize a new instance of the
        /// <see cref="Faction"/> class!</remarks>

        private Faction() {
#if DEBUG
            // ensure unique elements in debug mode
            const bool isUnique = true;
#else
            const bool isUnique = false;
#endif
            // create empty collections for owned entities
            this._sites = new SiteList(isUnique);
            this._units = new EntityList(isUnique);
            this._terrains = new EntityList(isUnique);
            this._upgrades = new EntityList(isUnique);

            // create empty arrays for modifier maps
            var size = Finder.MapGrid.Size;
            UnitAttributeModifiers = new VariableValueDictionary[size.Width, size.Height];
            UnitResourceModifiers = new VariableValueDictionary[size.Width, size.Height];

        }

        #endregion
        #region Faction(Faction)

        /// <overloads>
        /// Initializes a new instance of the <see cref="Faction"/> class.</overloads>
        /// <summary>
        /// Initializes a new instance of the <see cref="Faction"/> class that is a deep copy of the
        /// specified instance.</summary>
        /// <param name="faction">
        /// The <see cref="Faction"/> object whose properties should be copied to the new instance.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="faction"/> is a null reference.</exception>
        /// <remarks><para>
        /// This constructor is called by <see cref="Clone"/> to perform a deep copy of the
        /// specified <paramref name="faction"/>, whose property values are processed as follows:
        /// </para><list type="table"><listheader>
        /// <term>Property</term><description>Action</description>
        /// </listheader><item>
        /// <term><see cref="FactionClass"/></term>
        /// <description>Reference copied to the new instance.</description>
        /// </item><item>
        /// <term><see cref="IsResigned"/></term>
        /// <description>Value copied to the new instance.</description>
        /// </item><item>
        /// <term><see cref="Counters"/><br/> <see cref="InstanceCounts"/><br/> <see
        /// cref="Resources"/><br/> <see cref="ResourceModifiers"/><br/> <see
        /// cref="UnitAttributeModifiers"/><br/> <see cref="UnitResourceModifiers"/></term>
        /// <description>Deep copies assigned to the new instance.</description>
        /// </item><item>
        /// <term><see cref="BuildableTerrains"/><br/> <see cref="BuildableUnits"/><br/> <see
        /// cref="BuildableUpgrades"/><br/> <see cref="Color"/><br/> <see cref="HomeSite"/><br/>
        /// <see cref="Id"/><br/> <see cref="Name"/><br/> <see cref="SupplyResources"/></term>
        /// <description>Values provided by <b>FactionClass</b>.</description>
        /// </item><item>
        /// <term><see cref="Sites"/><br/> <see cref="Terrains"/><br/> <see cref="Units"/><br/> <see
        /// cref="Upgrades"/></term>
        /// <description>Ignored; set by <see cref="WorldState(WorldState)"/>.</description>
        /// </item></list></remarks>

        protected Faction(Faction faction): this() {
            if (faction == null)
                ThrowHelper.ThrowArgumentNullException("faction");

            IsResigned = faction.IsResigned;
            this._factionClass = faction._factionClass;
            this._instanceCounts = new InstanceCountDictionary(faction._instanceCounts);

            this._counters = (VariableContainer) faction._counters.Clone();
            this._resources = (VariableContainer) faction._resources.Clone();
            this._resourceModifiers = (VariableContainer) faction._resourceModifiers.Clone();

            CopyUnitModifiers(faction, VariableCategory.Attribute);
            CopyUnitModifiers(faction, VariableCategory.Resource);
        }

        #endregion
        #region Faction(FactionClass)

        /// <summary>
        /// Initializes a new instance of the <see cref="Faction"/> class based on the specified
        /// <see cref="Scenario.FactionClass"/>.</summary>
        /// <param name="factionClass">
        /// The initial value for the <see cref="FactionClass"/> property.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="factionClass"/> is a null reference.</exception>
        /// <remarks><para>
        /// Clients should use factory methods to instantiate the <see cref="Faction"/> class,
        /// either <see cref="CreateFaction"/> or an equivalent method defined by the rule script.
        /// </para><para>
        /// Note that construction does not fully initialize the new instance of the <b>Faction</b>
        /// class. Additional initialization tasks are performed by the <see cref="WorldState"/>
        /// constructors, and possibly by the <see cref="Initialize"/> method.</para></remarks>

        protected Faction(FactionClass factionClass): this() {
            if (factionClass == null)
                ThrowHelper.ThrowArgumentNullException("factionClass");

            this._factionClass = factionClass;
            this._instanceCounts = new InstanceCountDictionary();

            const VariablePurpose purpose = (VariablePurpose.Faction | VariablePurpose.Scenario);
            const VariablePurpose basic = (purpose | VariablePurpose.Basic);
            const VariablePurpose modifier = (purpose | VariablePurpose.Modifier);

            if (ApplicationInfo.IsEditor) {
                // initialize counters to empty container
                this._counters = new VariableContainer(VariableCategory.Counter, basic);

                // initialize resources to empty container
                this._resources = new VariableContainer(VariableCategory.Resource, basic);
                this._resourceModifiers = new VariableContainer(VariableCategory.Resource, modifier);
            }
            else {
                // initialize counters with scenario values
                this._counters = new VariableContainer(
                    VariableCategory.Counter, basic, factionClass.Counters);

                // initialize resources with scenario values
                this._resources = new VariableContainer(
                    VariableCategory.Resource, basic, factionClass.Resources);

                this._resourceModifiers = new VariableContainer(
                    VariableCategory.Resource, modifier, factionClass.ResourceModifiers);
            }

            // initialize modifier maps to empty collections
            var size = Finder.MapGrid.Size;
            for (int x = 0; x < size.Width; x++)
                for (int y = 0; y < size.Height; y++) {
                    UnitAttributeModifiers[x, y] = new VariableValueDictionary();
                    UnitResourceModifiers[x, y] = new VariableValueDictionary();
                }
        }

        #endregion
        #region Private Fields

        // property backers
        private readonly FactionClass _factionClass;
        private readonly VariableContainer _counters, _resources, _resourceModifiers;
        private readonly InstanceCountDictionary _instanceCounts;
        private readonly SiteList _sites;
        private readonly EntityList _units, _terrains, _upgrades;

        #endregion
        #region Internal Properties
        #region UnitAttributeModifiers

        /// <summary>
        /// Gets the modifier map for all <see cref="Entity.Attributes"/> of all placed <see
        /// cref="Units"/> owned by the <see cref="Faction"/>.</summary>
        /// <value>
        /// A two-dimensional <see cref="Array"/> indexed by map coordinates whose elements are <see
        /// cref="VariableValueDictionary"/> collections that map <see cref="AttributeClass"/>
        /// identifiers to the corresponding aggregate modifier values.</value>
        /// <remarks><para>
        /// <b>UnitAttributeModifiers</b> contains all modifier values that affect the <see
        /// cref="Entity.Attributes"/> of all <see cref="Site.Units"/> in the <see cref="Site"/>
        /// corresponding to each <see cref="Array"/> element, provided they are owned by this <see
        /// cref="Faction"/>.
        /// </para><para>
        /// Each <see cref="VariableValueDictionary"/> contains the sum of all applicable modifier
        /// values for each <see cref="AttributeClass"/>, aggregating the effect of all <see
        /// cref="WorldState.Entities"/> in the current <see cref="WorldState"/>. Only <see
        /// cref="ModifierTarget.Self"/> modifiers are excluded.</para></remarks>

        internal VariableValueDictionary[,] UnitAttributeModifiers { get; private set; }

        #endregion
        #region UnitResourceModifiers

        /// <summary>
        /// Gets the modifier map for all <see cref="Entity.Resources"/> of all placed <see
        /// cref="Units"/> owned by the <see cref="Faction"/>.</summary>
        /// <value>
        /// A two-dimensional <see cref="Array"/> indexed by map coordinates whose elements are <see
        /// cref="VariableValueDictionary"/> collections that map <see cref="ResourceClass"/>
        /// identifiers to the corresponding aggregate modifier values.</value>
        /// <remarks>
        /// <b>UnitResourceModifiers</b> is identical to <see cref="UnitAttributeModifiers"/> but
        /// aggregates modifier values for <see cref="ResourceClass"/> variables.</remarks>

        internal VariableValueDictionary[,] UnitResourceModifiers { get; private set; }

        #endregion
        #region WritableSites

        /// <summary>
        /// Gets a writable list of all <see cref="Sites"/> owned by the <see cref="Faction"/>.
        /// </summary>
        /// <value>
        /// The collection that is backing the <see cref="Sites"/> property.</value>

        internal SiteList WritableSites {
            [DebuggerStepThrough]
            get { return this._sites; }
        }

        #endregion
        #region WritableTerrains

        /// <summary>
        /// Gets a writable list of all <see cref="Terrains"/> owned by the <see cref="Faction"/>.
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
        /// Gets a writable list of all <see cref="Units"/> owned by the <see cref="Faction"/>.
        /// </summary>
        /// <value>
        /// The collection that is backing the <see cref="Units"/> property.</value>

        internal EntityList WritableUnits {
            [DebuggerStepThrough]
            get { return this._units; }
        }

        #endregion
        #region WritableUpgrades

        /// <summary>
        /// Gets a writable list of all <see cref="Upgrades"/> owned by the <see cref="Faction"/>.
        /// </summary>
        /// <value>
        /// The collection that is backing the <see cref="Upgrades"/> property.</value>

        internal EntityList WritableUpgrades {
            [DebuggerStepThrough]
            get { return this._upgrades; }
        }

        #endregion
        #endregion
        #region Public Properties
        #region BuildableTerrains

        /// <summary>
        /// Gets a list of all terrain classes that the <see cref="Faction"/> may build.</summary>
        /// <value>
        /// The <see cref="Scenario.FactionClass.BuildableTerrains"/> collection of the underlying
        /// <see cref="FactionClass"/>.</value>

        public EntityClassDictionary BuildableTerrains {
            [DebuggerStepThrough]
            get { return FactionClass.BuildableTerrains; }
        }

        #endregion
        #region BuildableUnits

        /// <summary>
        /// Gets a list of all unit classes that the <see cref="Faction"/> may build.</summary>
        /// <value>
        /// The <see cref="Scenario.FactionClass.BuildableUnits"/> collection of the underlying <see
        /// cref="FactionClass"/>.</value>

        public EntityClassDictionary BuildableUnits {
            [DebuggerStepThrough]
            get { return FactionClass.BuildableUnits; }
        }

        #endregion
        #region BuildableUpgrades

        /// <summary>
        /// Gets a list of all upgrade classes that the <see cref="Faction"/> may build.</summary>
        /// <value>
        /// The <see cref="Scenario.FactionClass.BuildableUpgrades"/> collection of the underlying
        /// <see cref="FactionClass"/>.</value>

        public EntityClassDictionary BuildableUpgrades {
            [DebuggerStepThrough]
            get { return FactionClass.BuildableUpgrades; }
        }

        #endregion
        #region Color

        /// <summary>
        /// Gets the display color of the <see cref="Faction"/>.</summary>
        /// <value>
        /// The value of the <see cref="Scenario.FactionClass.Color"/> property of the underlying
        /// <see cref="FactionClass"/>.</value>
        /// <remarks>
        /// <b>Color</b> returns a <see cref="System.Windows.Media.Color"/> value indicating how the
        /// <see cref="Faction"/> should be represented on a color-coded display.</remarks>

        public Color Color {
            [DebuggerStepThrough]
            get { return FactionClass.Color; }
        }

        #endregion
        #region Counters

        /// <summary>
        /// Gets a <see cref="VariableContainer"/> containing all counters of the <see
        /// cref="Faction"/>.</summary>
        /// <value>
        /// A <see cref="VariableContainer"/> containing the counters initially and currently
        /// defined for the <see cref="Faction"/>.</value>
        /// <remarks><para>
        /// <b>Counters</b> never returns a null reference. This property is initialized with the
        /// values of the <see cref="Scenario.FactionClass.Counters"/> collection of the underlying
        /// <see cref="FactionClass"/>.
        /// </para><para>
        /// Use the HCL instruction <see cref="Command.SetFactionVariable"/> to add or change <see
        /// cref="Variable"/> values while executing a game command.</para></remarks>

        public VariableContainer Counters {
            [DebuggerStepThrough]
            get { return this._counters; }
        }

        #endregion
        #region FactionClass

        /// <summary>
        /// Gets the scenario class of the <see cref="Faction"/>.</summary>
        /// <value>
        /// The <see cref="Scenario.FactionClass"/> on which the <see cref="Faction"/> is based.
        /// </value>
        /// <remarks><para>
        /// <b>FactionClass</b> never returns a null reference. This property never changes once the
        /// object has been constructed.
        /// </para><para>
        /// Each <see cref="Scenario.FactionClass"/> defined by a scenario is the basis for exactly
        /// one <see cref="Faction"/> instance; or for none, if the corresponding <b>Faction</b> has
        /// already been eliminated.</para></remarks>

        public FactionClass FactionClass {
            [DebuggerStepThrough]
            get { return this._factionClass; }
        }

        #endregion
        #region GetPlayerSettings

        /// <summary>
        /// Gets or sets the <see cref="GetPlayerSettingsCallback"/> delegate that retrieves the
        /// current <see cref="World.PlayerSettings"/> for the specified <see cref="Faction"/>.
        /// </summary>
        /// <value>
        /// The <see cref="GetPlayerSettingsCallback"/> delegate invoked by <see
        /// cref="PlayerSettings"/>. The default is a null reference.</value>
        /// <remarks><para>
        /// <b>GetPlayerSettings</b> is set to a valid delegate when Hexkit Game creates a new
        /// session, and reset to a null reference when the session is closed. Other clients should
        /// never change this property.
        /// </para><para>
        /// <b>GetPlayerSettings</b> is always a null reference if <see
        /// cref="ApplicationInfo.IsEditor"/> is <c>true</c>.</para></remarks>

        public static GetPlayerSettingsCallback GetPlayerSettings { get; set; }

        #endregion
        #region HomeSite

        /// <summary>
        /// Gets the home site of the <see cref="Faction"/>.</summary>
        /// <value>
        /// The value of the <see cref="Scenario.FactionClass.HomeSite"/> property of the underlying
        /// <see cref="FactionClass"/>.</value>
        /// <remarks><para>
        /// <b>HomeSite</b> returns a <see cref="PointI"/> value indicating the coordinates of the
        /// principal or default <see cref="Site"/> for the <see cref="Faction"/>, if valid.
        /// </para><para>
        /// <b>HomeSite</b> may return <see cref="Site.InvalidLocation"/> or otherwise invalid
        /// coordinates, indicating that the <see cref="Faction"/> does not have a home site.
        /// </para></remarks>

        public PointI HomeSite {
            [DebuggerStepThrough]
            get { return FactionClass.HomeSite; }
        }

        #endregion
        #region Id

        /// <summary>
        /// Gets the identifier of the <see cref="Faction"/>.</summary>
        /// <value>
        /// The value of the <see cref="Scenario.FactionClass.Id"/> property of the underlying <see
        /// cref="FactionClass"/>.</value>
        /// <remarks><para>
        /// <b>Id</b> never returns a null reference or an empty string.
        /// </para><para>
        /// This property returns a unique internal identifier that is not intended for display in
        /// Hexkit Game.</para></remarks>

        public string Id {
            [DebuggerStepThrough]
            get { return FactionClass.Id; }
        }

        #endregion
        #region InstanceCounts

        /// <summary>
        /// Gets the number of owned <see cref="Entity"/> objects based on each <see
        /// cref="EntityClass"/>.</summary>
        /// <value>
        /// A read-only <see cref="InstanceCountDictionary"/> that maps <see cref="EntityClass.Id"/>
        /// strings of <see cref="EntityClass"/> objects to the number of corresponding <see
        /// cref="Entity"/> objects owned by the <see cref="Faction"/>.</value>
        /// <remarks>
        /// <b>InstanceCounts</b> never returns a null reference. Use the <see cref="AddInstance"/>
        /// method to increment the value associated with an entity class.</remarks>

        public InstanceCountDictionary InstanceCounts {
            [DebuggerStepThrough]
            get { return this._instanceCounts.AsReadOnly(); }
        }

        #endregion
        #region IsResigned

        /// <summary>
        /// Gets or sets a value indicating whether the <see cref="Faction"/> has resigned.
        /// </summary>
        /// <value>
        /// <c>true</c> if the <see cref="Faction"/> is to be treated as defeated when the next <see
        /// cref="EndTurnCommand"/> is executed; otherwise, <c>false</c>.</value>
        /// <remarks>
        /// Use the HCL instruction <see cref="Command.SetFactionResigned"/> to set this property
        /// while executing a game command.</remarks>

        public bool IsResigned { get; internal set; }

        #endregion
        #region Name

        /// <summary>
        /// Gets the display name of the <see cref="Faction"/>.</summary>
        /// <value>
        /// The value of the <see cref="Scenario.FactionClass.Name"/> property of the underlying
        /// <see cref="FactionClass"/>.</value>
        /// <remarks><para>
        /// <b>Name</b> never returns a null reference.
        /// </para><para>
        /// This property returns the name that should be used to represent the <see
        /// cref="Faction"/> within Hexkit Game.</para></remarks>

        public string Name {
            [DebuggerStepThrough]
            get { return FactionClass.Name; }
        }

        #endregion
        #region PlayerSettings

        /// <summary>
        /// Gets the current <see cref="World.PlayerSettings"/> for the <see cref="Faction"/>.
        /// </summary>
        /// <value>
        /// The current <see cref="World.PlayerSettings"/> for the <see cref="Faction"/>.</value>
        /// <exception cref="PropertyValueException">
        /// <see cref="GetPlayerSettings"/> is a null reference.</exception>
        /// <remarks>
        /// <b>PlayerSettings</b> invokes <see cref="GetPlayerSettings"/> to obtain the current <see
        /// cref="World.PlayerSettings"/> for the <see cref="Faction"/>.</remarks>

        public PlayerSettings PlayerSettings {
            [DebuggerStepThrough]
            get {
                if (GetPlayerSettings == null)
                    ThrowHelper.ThrowPropertyValueException(
                        "GetPlayerSettings", Tektosyne.Strings.PropertyNull);

                return GetPlayerSettings(this);
            }
        }

        #endregion
        #region Resources

        /// <summary>
        /// Gets a <see cref="VariableContainer"/> containing all resources of the <see
        /// cref="Faction"/>.</summary>
        /// <value>
        /// A <see cref="VariableContainer"/> containing the resources initially and currently
        /// available to the <see cref="Faction"/>.</value>
        /// <remarks><para>
        /// <b>Resources</b> never returns a null reference. This property is initialized with the
        /// values of the <see cref="Scenario.FactionClass.Resources"/> collection of the underlying
        /// <see cref="FactionClass"/>.
        /// </para><para>
        /// Use the HCL instruction <see cref="Command.SetFactionVariable"/> to add or change <see
        /// cref="Variable"/> values while executing a game command.</para></remarks>

        public VariableContainer Resources {
            [DebuggerStepThrough]
            get { return this._resources; }
        }

        #endregion
        #region ResourceModifiers

        /// <summary>
        /// Gets a <see cref="VariableContainer"/> containing all resource modifiers of the <see
        /// cref="Faction"/>.</summary>
        /// <value>
        /// A <see cref="VariableContainer"/> containing the resource modifiers initially and
        /// currently defined for the <see cref="Faction"/>.</value>
        /// <remarks><para>
        /// <b>ResourceModifiers</b> never returns a null reference. This property is initialized
        /// with the values of the <see cref="Scenario.FactionClass.ResourceModifiers"/> collection
        /// of the underlying <see cref="FactionClass"/>.
        /// </para><para>
        /// Use the HCL instruction <see cref="Command.SetFactionVariableModifier"/> to add or
        /// change <see cref="Variable"/> values while executing a game command.</para></remarks>

        public VariableContainer ResourceModifiers {
            [DebuggerStepThrough]
            get { return this._resourceModifiers; }
        }

        #endregion
        #region Sites

        /// <summary>
        /// Gets a list of all map sites owned by the <see cref="Faction"/>.</summary>
        /// <value>
        /// A read-only <see cref="SiteList"/> containing all <see cref="Site"/> objects owned by
        /// the <see cref="Faction"/>.</value>
        /// <remarks>
        /// <b>Sites</b> never returns a null reference, and its elements are never null references.
        /// All keys and elements are unique. Use the <see cref="Site.Owner"/> property of the <see
        /// cref="Site"/> class to add or remove <b>Sites</b>.</remarks>

        public SiteList Sites {
            [DebuggerStepThrough]
            get { return this._sites.AsReadOnly(); }
        }

        #endregion
        #region SupplyResources

        /// <summary>
        /// Gets a list of resource identifiers indicating all unit resources that can be
        /// resupplied.</summary>
        /// <value>
        /// The <see cref="Scenario.FactionClass.SupplyResources"/> collection of the underlying
        /// <see cref="FactionClass"/>.</value>
        /// <remarks>
        /// <b>SupplyResources</b> never returns a null reference, but it may return an empty
        /// collection to indicate that the units available to the <see cref="Faction"/> do not use
        /// any resources that can be resupplied.</remarks>

        public IdentifierList SupplyResources {
            [DebuggerStepThrough]
            get { return FactionClass.SupplyResources; }
        }

        #endregion
        #region Terrains

        /// <summary>
        /// Gets a list of all terrains owned by the <see cref="Faction"/>.</summary>
        /// <value>
        /// A read-only <see cref="EntityList"/> containing all <see cref="Terrain"/> objects owned
        /// by the <see cref="Faction"/>.</value>
        /// <remarks><para>
        /// <b>Terrains</b> never returns a null reference, and its elements are never null
        /// references. All keys and elements are unique. Use the <see cref="Entity.Owner"/>
        /// property of the <see cref="Entity"/> class to add or remove <b>Terrains</b>.
        /// </para><para>
        /// <b>Terrains</b> is initialized to a collection containing all <see cref="Terrain"/>
        /// objects assigned to the underlying <see cref="FactionClass"/>, as well as all
        /// <b>Terrain</b> objects placed for the <b>FactionClass</b> in the current <see
        /// cref="AreaSection"/>.</para></remarks>

        public EntityList Terrains {
            [DebuggerStepThrough]
            get { return this._terrains.AsReadOnly(); }
        }

        #endregion
        #region Units

        /// <summary>
        /// Gets a list of all units owned by the <see cref="Faction"/>.</summary>
        /// <value>
        /// A read-only <see cref="EntityList"/> containing all <see cref="Unit"/> objects owned by
        /// the <see cref="Faction"/>.</value>
        /// <remarks><para>
        /// <b>Units</b> never returns a null reference, and its elements are never null references.
        /// All keys and elements are unique. Use the <see cref="Entity.Owner"/> property of the
        /// <see cref="Entity"/> class to add or remove <b>Units</b>.
        /// </para><para>
        /// <b>Units</b> is initialized to a collection containing all <see cref="Unit"/> objects
        /// assigned to the underlying <see cref="FactionClass"/>, as well as all <b>Unit</b>
        /// objects placed for the <b>FactionClass</b> in the current <see cref="AreaSection"/>.
        /// </para></remarks>

        public EntityList Units {
            [DebuggerStepThrough]
            get { return this._units.AsReadOnly(); }
        }

        #endregion
        #region UnitStrength

        /// <summary>
        /// Gets the total <see cref="Unit.Strength"/> of all <see cref="Units"/>.</summary>
        /// <value>
        /// The sum of the current <see cref="Unit.Strength"/> values of all elements in the <see
        /// cref="Units"/> collection. This value is never negative.</value>
        /// <remarks>
        /// <b>UnitStrength</b> is recalculated on every access, so you should buffer the returned
        /// value if you intend to use it repeatedly.</remarks>

        public int UnitStrength {
            get {
                int strength = 0;

                foreach (Unit unit in Units)
                    strength += unit.Strength;

                return strength;
            }
        }

        #endregion
        #region Upgrades

        /// <summary>
        /// Gets a list of all upgrades owned by the <see cref="Faction"/>.</summary>
        /// <value>
        /// A read-only <see cref="EntityList"/> containing all <see cref="Upgrade"/> objects owned
        /// by the <see cref="Faction"/>.</value>
        /// <remarks><para>
        /// <b>Upgrades</b> never returns a null reference, and its elements are never null
        /// references. All keys and elements are unique. Use the <see cref="Entity.Owner"/>
        /// property of the <see cref="Entity"/> class to add or remove <b>Upgrades</b>.
        /// </para><para>
        /// <b>Upgrades</b> is initialized to a collection containing all <see cref="Upgrade"/>
        /// objects assigned to the underlying <see cref="FactionClass"/>.</para></remarks>

        public EntityList Upgrades {
            [DebuggerStepThrough]
            get { return this._upgrades.AsReadOnly(); }
        }

        #endregion
        #endregion
        #region Protected Methods
        #region AutoDestroyUnits

        /// <summary>
        /// Automatically disbands any unsupported <see cref="Units"/> during the specified <see
        /// cref="BeginTurnCommand"/>.</summary>
        /// <param name="command">
        /// The <see cref="BeginTurnCommand"/> being executed.</param>
        /// <remarks><para>
        /// <b>AutoDestroyUnits</b> invokes <see cref="GetUnsupportedUnits"/> to determine if any
        /// unsupported units must be disbanded. If so, <b>AutoDestroyUnits</b> adds a message event
        /// showing the names of all unsupported units, and an inline <see cref="DestroyCommand"/>
        /// that disbands them, to the specified <paramref name="command"/>.
        /// </para><para>
        /// Derived classes may override <b>AutoDestroyUnits</b> to implement different semantics
        /// for <see cref="BeginTurnCommand"/>.</para></remarks>

        protected virtual void AutoDestroyUnits(BeginTurnCommand command) {
            WorldState world = command.Context.WorldState;

            // determine units to disband, if any
            EntityList units = GetUnsupportedUnits(world);
            if (units.Count == 0) return;

            // create Destroy command
            Command destroy = new DestroyCommand(this, units);

            // create message event
            command.ShowMessageDialog(Global.Strings.EventResourcesGone,
                Global.Strings.EventUnitsAutoDestroy, Id,
                EntityReference.GetNames(destroy.Entities));

            // inline Destroy command
            command.InlineCommand(destroy);
        }

        #endregion
        #region GetUnsupportedUnits

        /// <summary>
        /// Gets a list of all <see cref="Units"/> that the <see cref="Faction"/> can no longer
        /// support.</summary>
        /// <param name="worldState">
        /// The <see cref="WorldState"/> on which to base the evaluation.</param>
        /// <returns><para>
        /// A <see cref="EntityList"/> containing all elements in the <see cref="Units"/> collection
        /// that the <see cref="Faction"/> can no longer support.
        /// </para><para>-or-</para><para>
        /// An empty collection if the <see cref="Faction"/> can afford to support all its <see
        /// cref="Units"/>.</para></returns>
        /// <remarks><para>
        /// <b>GetUnsupportedUnits</b> invokes <see cref="ComputeResourceModifiers"/> to calculate
        /// all modifiers to accumulating resources, and performs the following steps for each
        /// resulting modifier:
        /// </para><list type="number"><item>
        /// Proceed to the next modifier if the modifier and the matching <see cref="Resources"/>
        /// value add up to at least the <see cref="VariableClass.Minimum"/> value of the resource.
        /// </item><item>
        /// Otherwise, select an element of the <see cref="Units"/> collection, as described below.
        /// Proceed to the next modifier if there is no appropriate element left.
        /// </item><item>
        /// Add the <see cref="Units"/> element to the returned collection.
        /// </item><item>
        /// Add the <see cref="Entity.ResourceModifiers"/> values of the unit to all matching
        /// modifiers, if any.
        /// </item><item>
        /// Add the result of <see cref="Entity.GetDestroyResources"/> invoked on the unit to all
        /// matching modifiers, if any.
        /// </item><item>
        /// Return to step 1.
        /// </item></list><para>
        /// Step 2 selects the <see cref="Units"/> element with the lowest contextual valuation,
        /// according to <see cref="Evaluate"/>, that has a positive <see
        /// cref="Entity.ResourceModifiers"/> value with an <see cref="ModifierTarget.Owner"/>
        /// target that matches the current resource modifier.
        /// </para><para>
        /// Derived classes may override <b>GetUnsupportedUnits</b> to implement different unit
        /// maintenance rules.</para></remarks>

        protected virtual EntityList GetUnsupportedUnits(WorldState worldState) {

            // collection receiving units to disband
            var destroy = new EntityList();

            // sorted unit list (delayed initialization)
            EntityList units = null;

            // compute total accumulating resource modifiers
            var modifiers = CategorizedValue.CreateTotalDictionary(ComputeResourceModifiers(false));

            for (int i = 0; i < modifiers.Count; i++) {
                string id = modifiers.GetKey(i);
                Debug.Assert(Resources.ContainsId(id));

                int modifier = modifiers.GetByIndex(i);
                Variable current = Resources[id];

            balancing:
                // skip already balanced resource
                if (current.Value + modifier >= current.Minimum)
                    continue;

                if (units == null) {
                    // create unit list sorted by valuation
                    var comparer = new ValuableComparer<Entity>(worldState, this);
                    comparer.SortOrder = FormsSortOrder.Descending;

                    units = new EntityList(Units);
                    units.Sort(comparer);
                }

                // delete least valuable matching unit
                for (int j = units.Count - 1; j >= 0; j--) {
                    Unit unit = (Unit) units[j];

                    // skip units that cost nothing
                    if (unit.ResourceModifiers.GetValue(id, ModifierTarget.Owner) >= 0)
                        continue;

                    // mark unit for deletion
                    units.RemoveAt(j);
                    destroy.Add(unit);

                    // subtract saved maintentance resources
                    var unitCosts = unit.ResourceModifiers.ToDictionary(ModifierTarget.Owner);
                    WorldUtility.SubtractDictionary(modifiers, unitCosts);

                    // add resources gained by deleting unit
                    var unitGains = unit.GetDestroyResources(worldState);
                    WorldUtility.AddDictionary(modifiers, unitGains);

                    goto balancing; // recheck condition
                }
            }

            return destroy;
        }

        #endregion
        #region UpdateResources

        /// <summary>
        /// Computes and applies all modifiers to the specified subset of the <see
        /// cref="Resources"/> of the <see cref="Faction"/>.</summary>
        /// <param name="command">
        /// The <see cref="Command"/> being executed.</param>
        /// <param name="reset">
        /// <c>true</c> to reset and modify all <see cref="Resources"/> whose <see
        /// cref="Variable.IsResettingResource"/> flag is <c>true</c>; <c>false</c> to modify all
        /// <b>Resources</b> whose <b>IsResettingResource</b> flag is <c>false</c>.</param>
        /// <remarks><para>
        /// <b>UpdateResources</b> updates all values in the <see cref="Resources"/> collection
        /// whose <see cref="Variable.IsResettingResource"/> flag equals the specified <paramref
        /// name="reset"/> value, as follows:
        /// </para><list type="number"><item>
        /// Call <see cref="ComputeResourceModifiers"/> with the specified <paramref name="reset"/>
        /// value to compute all required current resource modifiers.
        /// </item><item>
        /// If <paramref name="reset"/> is <c>true</c>, reset the indicated <b>Resources</b> values
        /// to their corresponding <see cref="Variable.InitialValue"/>.
        /// </item><item>
        /// Add the <see cref="CategorizedValue.Total"/> properties of the resulting collection to
        /// the corresponding <b>Resources</b> values.
        /// </item><item>
        /// Call <see cref="AddUpgradeResources"/> if <paramref name="reset"/> is <c>false</c> and
        /// the <see cref="Upgrades"/> collection is not empty.</item></list></remarks>

        protected void UpdateResources(Command command, bool reset) {
            Debug.Assert(command != null);

            // compute total specified resource modifiers
            var modifiers = ComputeResourceModifiers(reset);

            for (int i = 0; i < modifiers.Count; i++) {
                string id = modifiers.GetKey(i);
                Debug.Assert(Resources.ContainsId(id));

                // retrieve desired initial resource value
                int initial = (reset ? Resources[id].InitialValue : Resources[id].Value);

                // add matching total resource modifier
                int total = modifiers.GetByIndex(i).Total;
                command.SetFactionVariable(Id, id, initial + total);
            }

            // add matching upgrade resources, if any
            if (!reset && Upgrades.Count > 0)
                AddUpgradeResources(command);
        }

        #endregion
        #endregion
        #region Internal Methods
        #region AddInstance

        /// <summary>
        /// Adds a new instance of the specified entity class to the <see cref="InstanceCounts"/>
        /// collection.</summary>
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
        #region AddUpgradeResources

        /// <summary>
        /// Acquires the <see cref="Entity.Resources"/> provided by any <see cref="Upgrades"/>.
        /// </summary>
        /// <param name="command">
        /// The <see cref="Command"/> being executed.</param>
        /// <remarks><para>
        /// <b>AddUpgradeResources</b> modifies the values in the <see cref="Resources"/> collection
        /// by adding some or all of the <see cref="Entity.Resources"/> provided by owned <see
        /// cref="Upgrades"/> whose <see cref="EntityClass.ResourceTransfer"/> property does not
        /// equal <see cref="ResourceTransferMode.None"/>, as follows:
        /// </para><list type="bullet"><item>
        /// Compute the transfer amount. This is the matching <see cref="Entity.Resources"/> value
        /// of the current <see cref="Upgrade"/>, restricted to the difference between the current
        /// faction resource and its <see cref="Variable.Minimum"/> or <see
        /// cref="Variable.Maximum"/> value, depending on the sign of the current upgrade resource.
        /// </item><item>
        /// Add the transfer amount to the current faction resource and subtract it from the
        /// matching upgrade resource.
        /// </item><item>
        /// If the matching upgrade resource is now zero, invoke <see cref="Entity.CheckDepletion"/>
        /// on the <see cref="Upgrade"/>.</item></list></remarks>

        internal void AddUpgradeResources(Command command) {

            // do nothing if no upgrades
            if (Upgrades.Count == 0) return;

            // update current value of all resources
            foreach (Variable resource in Resources.Variables) {
                int value = resource.Value;
                string id = resource.Id;

                // check all upgrades for matching resources
                foreach (Upgrade upgrade in Upgrades) {
                    if (upgrade.EntityClass.ResourceTransfer == ResourceTransferMode.None)
                        continue;

                    // check for matching resource value
                    int available = upgrade.Resources.GetValue(id);
                    if (available == 0) continue;

                    // compute maximum transfer amount
                    int transfer = (available > 0 ?
                        Math.Min(available, resource.Maximum - value) :
                        Math.Max(available, resource.Minimum - value));
                    if (transfer == 0) continue;

                    // transfer maximum amount to faction
                    value += transfer;
                    command.SetEntityVariable(upgrade.Id, id, available - transfer);

                    // check upgrade for depletion on zero resource
                    if (available == transfer)
                        upgrade.CheckDepletion(command, this);
                }

                // update current resource value
                command.SetFactionVariable(Id, id, value);
            }
        }

        #endregion
        #region CopyUnitModifiers

        /// <summary>
        /// Copies the unit modifier map for the specified <see cref="VariableCategory"/> from the
        /// specified <see cref="Faction"/>.</summary>
        /// <param name="faction">
        /// The <see cref="Faction"/> whose modifier map to copy.</param>
        /// <param name="category">
        /// A <see cref="VariableCategory"/> value indicating which modifier map to copy.</param>
        /// <exception cref="InvalidEnumArgumentException">
        /// <paramref name="category"/> is <see cref="VariableCategory.Counter"/> or an invalid <see
        /// cref="VariableCategory"/> value.</exception>
        /// <remarks><para>
        /// <b>CopyUnitModifiers</b> copies either all <see cref="UnitAttributeModifiers"/> or all
        /// <see cref="UnitResourceModifiers"/> from the specified <paramref name="faction"/> to
        /// this <see cref="Faction"/>, depending on the specified <paramref name="category"/>.
        /// </para><para>
        /// Modifier values that equal zero are omitted rather than copied. Any existing <see
        /// cref="UnitAttributeModifiers"/> or <see cref="UnitResourceModifiers"/> data is lost.
        /// </para></remarks>

        private void CopyUnitModifiers(Faction faction, VariableCategory category) {

            VariableValueDictionary[,] source, target;
            switch (category) {

                case VariableCategory.Attribute:
                    source = faction.UnitAttributeModifiers;
                    target = UnitAttributeModifiers;
                    break;

                case VariableCategory.Resource:
                    source = faction.UnitResourceModifiers;
                    target = UnitResourceModifiers;
                    break;

                default:
                    ThrowHelper.ThrowInvalidEnumArgumentException(
                        "category", (int) category, typeof(VariableCategory));
                    return;
            }

            var size = Finder.MapGrid.Size;
            for (int x = 0; x < size.Width; x++)
                for (int y = 0; y < size.Height; y++) {

                    VariableValueDictionary sourceList = source[x, y];
                    var targetList = new VariableValueDictionary(sourceList.Count);

                    // copy only non-zero values to target list
                    for (int i = 0; i < sourceList.Count; i++) {
                        int value = sourceList.GetByIndex(i);
                        if (value != 0)
                            targetList.Add(sourceList.GetKey(i), value);
                    }

                    target[x, y] = targetList;
                }
        }

        #endregion
        #region CreateFaction

        /// <summary>
        /// Creates a new <see cref="Faction"/> object from the specified <see
        /// cref="Scenario.FactionClass"/>.</summary>
        /// <param name="factionClass">
        /// The <see cref="Scenario.FactionClass"/> to instantiate.</param>
        /// <returns>
        /// A new <see cref="Faction"/> object based on the specified <paramref
        /// name="factionClass"/>.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="factionClass"/> is a null reference.</exception>
        /// <remarks><para>
        /// <b>CreateFaction</b> is the default factory method for <see cref="Faction"/> objects.
        /// All it does is return the result of the <b>Faction</b> constructor.
        /// </para><para>
        /// <b>CreateFaction</b> conforms to the <see cref="CreateFactionCallback"/> delegate.
        /// </para></remarks>

        internal static Faction CreateFaction(FactionClass factionClass) {
            return new Faction(factionClass);
        }

        #endregion
        #region GetUnitModifiers

        /// <summary>
        /// Gets the variable modifier map of the specified category for the <see cref="Units"/>
        /// owned by the <see cref="Faction"/> that are placed on the specified <see cref="Site"/>.
        /// </summary>
        /// <param name="x">
        /// The x-coordinate of the <see cref="Site"/> whose modifiers to return.</param>
        /// <param name="y">
        /// The y-coordinate of the <see cref="Site"/> whose modifiers to return.</param>
        /// <param name="category">
        /// A <see cref="VariableCategory"/> value indicating which modifiers to return.</param>
        /// <returns><para>
        /// The <see cref="VariableValueDictionary"/> that maps <see cref="VariableClass"/>
        /// identifiers of the specified <paramref name="category"/> to the corresponding aggregate
        /// modifier values in the specified <see cref="Site"/>.
        /// </para><para>-or-</para><para>
        /// A null reference if there is no <see cref="VariableValueDictionary"/> that matches the
        /// specified <paramref name="category"/>.</para></returns>
        /// <exception cref="IndexOutOfRangeException">
        /// <paramref name="x"/> or <paramref name="y"/> is not a valid map coordinate.</exception>
        /// <exception cref="InvalidEnumArgumentException">
        /// <paramref name="category"/> is not a valid <see cref="VariableCategory"/> value.
        /// </exception>
        /// <remarks>
        /// <b>GetUnitModifiers</b> returns the collection at index position (<paramref name="x"/>,
        /// <paramref name="y"/>) of either <see cref="UnitAttributeModifiers"/> or <see
        /// cref="UnitResourceModifiers"/>, depending on the specified <paramref name="category"/>.
        /// </remarks>

        internal VariableValueDictionary GetUnitModifiers(int x, int y, VariableCategory category) {
            switch (category) {

                case VariableCategory.Attribute:
                    return UnitAttributeModifiers[x, y];

                case VariableCategory.Counter:
                    return null;

                case VariableCategory.Resource:
                    return UnitResourceModifiers[x, y];

                default:
                    ThrowHelper.ThrowInvalidEnumArgumentException(
                        "category", (int) category, typeof(VariableCategory));
                    return null;
            }
        }

        #endregion
        #region GetWritableEntities

        /// <summary>
        /// Gets a writable list of all entities of the specified category that are owned by the
        /// <see cref="Faction"/>.</summary>
        /// <param name="category">
        /// A <see cref="EntityCategory"/> value indicating which entities to return.</param>
        /// <returns>
        /// A writable <see cref="EntityList"/> containing all entities in the specified <paramref
        /// name="category"/> that belong to the <see cref="Faction"/>.</returns>
        /// <exception cref="InvalidEnumArgumentException">
        /// <paramref name="category"/> is not a valid <see cref="EntityCategory"/> value.
        /// </exception>
        /// <remarks><para>
        /// <b>GetWritableEntities</b> returns a null reference if the specified <paramref
        /// name="category"/> equals <see cref="EntityCategory.Effect"/>.
        /// </para><para>
        /// Otherwise, <b>GetWritableEntities</b> returns the value of either the <see
        /// cref="WritableUnits"/>, the <see cref="WritableTerrains"/>, or the <see
        /// cref="WritableUpgrades"/> property, depending on the value of <paramref
        /// name="category"/>.</para></remarks>

        internal EntityList GetWritableEntities(EntityCategory category) {
            switch (category) {

                case EntityCategory.Unit:    return WritableUnits;
                case EntityCategory.Terrain: return WritableTerrains;
                case EntityCategory.Effect:  return null;
                case EntityCategory.Upgrade: return WritableUpgrades;

                default:
                    ThrowHelper.ThrowInvalidEnumArgumentException(
                        "category", (int) category, typeof(EntityCategory));
                    return null;
            }
        }

        #endregion
        #region SetUnitsCanAttack

        /// <summary>
        /// Sets the <see cref="Unit.CanAttack"/> flag of all <see cref="Units"/> with the specified
        /// <see cref="UnitClass"/> to the specified value.</summary>
        /// <param name="unitClass"><para>
        /// The <see cref="Entity.EntityClass"/> of all <see cref="Units"/> elements to manipulate.
        /// </para><para>-or-</para><para>
        /// A null reference to manipulate all <see cref="Units"/> elements, regardless of their
        /// <b>EntityClass</b>.</para></param>
        /// <param name="value">
        /// The new value for the <see cref="Unit.CanAttack"/> flag of all specified <see
        /// cref="Units"/> elements.</param>
        /// <returns>
        /// <c>true</c> if the <see cref="Unit.CanAttack"/> flag of one or more <see cref="Units"/>
        /// elements was changed; otherwise, <c>false</c>.</returns>
        /// <remarks>
        /// <b>SetUnitsCanAttack</b> leaves the <see cref="Unit.CanAttack"/> flag of a given <see
        /// cref="Units"/> element unchanged, regardless of the specified <paramref name="value"/>,
        /// if its <see cref="Unit.IsCombat"/> flag is <c>false</c>.</remarks>

        internal bool SetUnitsCanAttack(UnitClass unitClass, bool value) {
            bool changed = false;

            for (int i = 0; i < Units.Count; i++) {
                Unit unit = (Unit) Units[i];
                if (unitClass == null || unit.EntityClass == unitClass)
                    changed |= unit.SetCanAttack(value);
            }

            return changed;
        }

        #endregion
        #region SetUnitsCanMove

        /// <summary>
        /// Sets the <see cref="Unit.CanMove"/> flag of all <see cref="Units"/> with the specified
        /// <see cref="UnitClass"/> to the specified value.</summary>
        /// <param name="unitClass"><para>
        /// The <see cref="Entity.EntityClass"/> of all <see cref="Units"/> elements to manipulate.
        /// </para><para>-or-</para><para>
        /// A null reference to manipulate all <see cref="Units"/> elements, regardless of their
        /// <b>EntityClass</b>.</para></param>
        /// <param name="value">
        /// The new value for the <see cref="Unit.CanMove"/> flag of all specified <see
        /// cref="Units"/> elements.</param>
        /// <returns>
        /// <c>true</c> if the <see cref="Unit.CanMove"/> flag of one or more <see cref="Units"/>
        /// elements was changed; otherwise, <c>false</c>.</returns>
        /// <remarks>
        /// <b>SetUnitsCanMove</b> leaves the <see cref="Unit.CanMove"/> flag of a given <see
        /// cref="Units"/> element unchanged, regardless of the specified <paramref name="value"/>,
        /// if its <see cref="Unit.IsMobile"/> flag is <c>false</c>.</remarks>

        internal bool SetUnitsCanMove(UnitClass unitClass, bool value) {
            bool changed = false;

            for (int i = 0; i < Units.Count; i++) {
                Unit unit = (Unit) Units[i];
                if (unitClass == null || unit.EntityClass == unitClass)
                    changed |= unit.SetCanMove(value);
            }

            return changed;
        }

        #endregion
        #endregion
        #region Public Methods
        #region Automate

        /// <summary>
        /// Executes the specified <see cref="AutomateCommand"/>.</summary>
        /// <param name="command">
        /// The <see cref="AutomateCommand"/> to execute.</param>
        /// <remarks>
        /// The <see cref="Faction"/> implementation of <b>Automate</b> does nothing. Derived
        /// classes should override this method to perform the desired automated action.</remarks>

        public virtual void Automate(AutomateCommand command) {
            Debug.Assert(command.Faction.Value == this);
        }

        #endregion
        #region BeginTurn

        /// <summary>
        /// Executes the specified <see cref="BeginTurnCommand"/>.</summary>
        /// <param name="command">
        /// The <see cref="BeginTurnCommand"/> to execute.</param>
        /// <remarks><para>
        /// <b>BeginTurn</b> performs the following actions:
        /// </para><list type="number"><item>
        /// For all <see cref="Units"/> of this <see cref="Faction"/> whose <see
        /// cref="Unit.UnitClass"/> has the <see cref="UnitClass.CanDefendOnly"/> restriction, set
        /// their <see cref="Unit.CanAttack"/> ability to <c>false</c>.
        /// </item><item>
        /// If the <see cref="WorldState.CurrentTurn"/> of the current <see
        /// cref="ExecutionContext.WorldState"/> is still zero and its <see
        /// cref="WorldState.NewTurnStarted"/> flag is <c>true</c>, update the resetting <see
        /// cref="Resources"/> of all <see cref="WorldState.Factions"/> and quit.
        /// </item><item>
        /// Otherwise, if the <b>CurrentTurn</b> is still zero, do nothing and quit.
        /// </item><item>
        /// Otherwise, if the <b>NewTurnStarted</b> flag is <c>true</c>, update the <see
        /// cref="Entity.Resources"/> of <em>all</em> <see cref="WorldState.Entities"/> in the
        /// current <b>WorldState</b> that are not units.
        /// </item><item>
        /// Call <see cref="AutoDestroyUnits"/> to automatically disband any unsupported <see
        /// cref="Units"/>.
        /// </item><item>
        /// Update all accumulating <b>Resources</b>.
        /// </item><item>
        /// Update the <see cref="Entity.Resources"/> of all remaining <b>Units</b>.
        /// </item></list><para>
        /// Derived classes may override <b>BeginTurn</b> to implement different semantics for <see
        /// cref="BeginTurnCommand"/>.</para></remarks>

        public virtual void BeginTurn(BeginTurnCommand command) {

            Debug.Assert(command.Faction.Value == this);
            WorldState world = command.Context.WorldState;

            // disable Attack ability for all passive defenders
            foreach (UnitClass unitClass in MasterSection.Instance.Entities.Units.Values)
                if (unitClass.CanDefendOnly)
                    command.SetFactionUnitsCanAttack(Id, unitClass.Id, false);

            // special actions during first turn
            if (world.CurrentTurn == 0) {

                // update resetting resources for all factions
                if (world.NewTurnStarted)
                    foreach (Faction faction in world.Factions)
                        faction.UpdateResources(command, true);

                return;
            }

            // special actions once per turn
            if (world.NewTurnStarted) {

                // update resources for all non-unit entities
                foreach (Entity entity in world.Entities.Values)
                    if (entity.Category != EntityCategory.Unit)
                        entity.UpdateResources(command);
            }

            // disband unsupported units
            AutoDestroyUnits(command);

            // update accumulating resources
            UpdateResources(command, false);

            // update resources of owned units
            for (int i = 0; i < Units.Count; i++)
                Units[i].UpdateResources(command);
        }

        #endregion
        #region Build

        /// <summary>
        /// Executes the specified <see cref="BuildCommand"/>.</summary>
        /// <param name="command">
        /// The <see cref="BuildCommand"/> to execute.</param>
        /// <returns>
        /// An <see cref="IList{T}"/> containing all created <see cref="Entity"/> objects. The
        /// collection may be read-only.</returns>
        /// <remarks><para>
        /// <b>Build</b> performs the following actions for all elements in the <see
        /// cref="ClassesCommand.EntityClasses"/> collection of the specified <paramref
        /// name="command"/>:
        /// </para><list type="number"><item>
        /// Invoke <see cref="GetBuildResources"/> with the current <b>EntityClasses</b> element to
        /// determine the required resources.
        /// </item><item>
        /// Subtract the values of all resulting resources from the matching <see cref="Resources"/>
        /// elements.
        /// </item><item>
        /// Create a new <see cref="Entity"/> object based on the current <b>EntityClasses</b>
        /// element, and set its <see cref="Entity.Owner"/> property to this <b>Faction</b>.
        /// </item><item>
        /// Assign a unique name to the new <b>Entity</b> if its <see cref="Entity.Category"/>
        /// equals <see cref="EntityCategory.Unit"/>.
        /// </item></list><para>
        /// Derived classes may override <b>Build</b> to implement different semantics for <see
        /// cref="BuildCommand"/>.</para></remarks>

        public virtual IList<Entity> Build(BuildCommand command) {

            Debug.Assert(command.Faction.Value == this);
            WorldState world = command.Context.WorldState;

            // array holding created entities
            var classes = command.EntityClasses;
            Entity[] entities = new Entity[classes.Count];

            // create temporary copy of faction resources
            var resources = Resources.ToDictionary();

            // create one instance of each entity class
            for (int i = 0; i < classes.Count; i++) {
                EntityClass entityClass = classes.GetByIndex(i);

                // subtract resources required for building
                var buildResources = GetBuildResources(world, entityClass);
                WorldUtility.SubtractDictionary(resources, buildResources);

                // create entity owned by this faction
                Entity entity = command.CreateEntity(entityClass.Id);
                command.SetEntityOwner(entity.Id, Id);

                // assign unique name to newly built units
                if (entity.Category == EntityCategory.Unit)
                    command.SetEntityUniqueName(entity.Id, Id);

                // return created entity
                entities[i] = entity;
            }

            // assign final results to faction resources
            for (int i = 0; i < resources.Count; i++)
                command.SetFactionVariable(Id, resources.GetKey(i), resources.GetByIndex(i));

            return entities;
        }

        #endregion
        #region CanPlace(WorldState, IList<Entity>, PointI)

        /// <overloads>
        /// Determines whether existing or hypothetical entities can be placed on the specified
        /// target location.</overloads>
        /// <summary>
        /// Determines whether all specified entities can be placed on the specified target
        /// location.</summary>
        /// <param name="worldState">
        /// The <see cref="WorldState"/> on which to execute the <see cref="PlaceCommand"/>.</param>
        /// <param name="entities">
        /// An <see cref="IList{T}"/> containing all <see cref="Entity"/> objects to place.</param>
        /// <param name="target">
        /// The coordinates of the <see cref="Site"/> on which to place the specified <paramref
        /// name="entities"/>.</param>
        /// <returns>
        /// <c>true</c> if all specified <paramref name="entities"/> can be placed on the specified
        /// <paramref name="target"/>; otherwise, <c>false</c>.</returns>
        /// <remarks><para>
        /// <b>CanPlace</b> merely creates an <see cref="EntityClass"/> collection from the <see
        /// cref="Entity.EntityClass"/> values of the specified <paramref name="entities"/>, and
        /// then returns the result of the virtual <see cref="CanPlace(WorldState,
        /// IList{EntityClass}, PointI)"/> overload for that collection.
        /// </para><para>
        /// The individual properties of the specified <paramref name="entities"/>, other than their
        /// <b>EntityClass</b> values, are ignored. All entities based on the same
        /// <b>EntityClass</b> must have the same set of valid placement sites.</para></remarks>

        public bool CanPlace(WorldState worldState, IList<Entity> entities, PointI target) {

            Debug.Assert(worldState != null);
            Debug.Assert(entities != null);
            Debug.Assert(entities.Count > 0);

            // extract participating entity classes
            EntityClass[] entityClasses = new EntityClass[entities.Count];
            for (int i = 0; i < entities.Count; i++)
                entityClasses[i] = entities[i].EntityClass;

            return CanPlace(worldState, entityClasses, target);
        }

        #endregion
        #region CanPlace(WorldState, IList<EntityClass>, PointI)

        /// <summary>
        /// Determines whether all entities of the specified classes could be placed on the
        /// specified target location.</summary>
        /// <param name="worldState">
        /// The <see cref="WorldState"/> on which to execute the <see cref="PlaceCommand"/>.</param>
        /// <param name="entityClasses">
        /// An <see cref="IList{T}"/> containing the <see cref="Entity.EntityClass"/> values of all
        /// <see cref="Entity"/> objects to place. Multiple identical values are possible, and
        /// indicate multiple entities of the same class.</param>
        /// <param name="target">
        /// The coordinates of the <see cref="Site"/> on which to place the entities created from
        /// the specified <paramref name="entityClasses"/>.</param>
        /// <returns>
        /// <c>true</c> if all entities created from the specified <paramref name="entityClasses"/>
        /// could be placed on the specified <paramref name="target"/>; otherwise, <c>false</c>.
        /// </returns>
        /// <remarks><para>
        /// <b>CanPlace</b> returns <c>true</c> exactly if the <see cref="Site.Owner"/> of the
        /// specified <paramref name="target"/> site equals this <see cref="Faction"/>, and the
        /// <paramref name="target"/> site does not contain enemy <see cref="Site.Units"/>.
        /// </para><para>
        /// Derived classes may override <b>CanPlace</b> to implement different semantics for <see
        /// cref="PlaceCommand"/>.
        /// </para><para>
        /// <b>CanPlace</b> should only check whether the current contents of the specified
        /// <paramref name="target"/> site agree with the combined presence of all entities created
        /// from the specified <paramref name="entityClasses"/>. You should ignore all other
        /// considerations, such as the valid placement sites defined by the scenario.
        /// </para><note type="caution">
        /// Do not change any reference parameters or their contents! Create a copy if you need to
        /// change them.</note></remarks>

        public virtual bool CanPlace(WorldState worldState,
            IList<EntityClass> entityClasses, PointI target) {

            Debug.Assert(worldState != null);
            Debug.Assert(entityClasses != null);
            Debug.Assert(entityClasses.Count > 0);

            // enemy owner or units prevent occupation
            // TODO: remove owner check after revising EntityClass.PlaceSites
            Site site = worldState.GetSite(target);
            return (site.Owner == this && !site.HasAlienUnits(this));
        }

        #endregion
        #region CaptureSite

        /// <summary>
        /// Captures the specified <see cref="Site"/>.</summary>
        /// <param name="command">
        /// The <see cref="Command"/> being executed.</param>
        /// <param name="site">
        /// The <see cref="Site"/> to capture.</param>
        /// <remarks><para>
        /// <b>CaptureSite</b> performs the following actions:
        /// </para><list type="number"><item>
        /// Check that the specified <paramref name="site"/> is not a null reference, does not
        /// already belong to this <see cref="Faction"/>, and has the <see cref="Site.CanCapture"/>
        /// ability.
        /// </item><item>
        /// Check that the specified <paramref name="site"/> contains at least one <see
        /// cref="Site.Units"/> element that belongs to this <see cref="Faction"/> and whose <see
        /// cref="Unit.CanCapture"/> flag is <c>true</c>.
        /// </item><item>
        /// Set the <see cref="Site.Owner"/> of the <paramref name="site"/> to this <see
        /// cref="Faction"/>.
        /// </item><item>
        /// Create a message event stating that the <paramref name="site"/> was captured.
        /// </item></list><para>
        /// Derived classes may override <b>CaptureSite</b> to implement different semantics for
        /// capturing sites.</para></remarks>

        public virtual void CaptureSite(Command command, Site site) {

            // do nothing if site already owned or can't be captured
            if (site == null || site.Owner == this || !site.CanCapture)
                return;

            // check for owned units with the Capture ability
            if (!site.HasOwnedUnits(this) || !site.HasCaptureUnits())
                return;

            // transfer site to faction
            command.SetSiteOwner(site.Location, Id);

            // create capture event
            command.ShowMessage(Global.Strings.EventSiteCaptured, null, Id, null);
        }

        #endregion
        #region CheckDefeat

        /// <summary>
        /// Checks whether the <see cref="Faction"/> meets a defeat condition.</summary>
        /// <param name="command">
        /// The <see cref="Command"/> being executed.</param>
        /// <returns>
        /// <c>true</c> if the <see cref="Faction"/> meets a defeat condition; otherwise,
        /// <c>false</c>.</returns>
        /// <remarks><para>
        /// <b>CheckDefeat</b> returns <c>true</c> in the following cases:
        /// </para><list type="bullet"><item>
        /// Any one of the explicit <see cref="Scenario.FactionClass.DefeatConditions"/> of the
        /// underlying <see cref="FactionClass"/> is met.
        /// </item><item>
        /// Any <see cref="Resources"/> value is equal to or less than the <see
        /// cref="ResourceClass.Defeat"/> threshold of the matching <see
        /// cref="VariableSection.Resources"/> element defined by the current <see
        /// cref="VariableSection"/>.
        /// </item></list><para>
        /// <b>CheckDefeat</b> adds a message event to the specified <paramref name="command"/> when
        /// returning <c>true</c>, but does not generate an event when returning <c>false</c>.
        /// </para><para>
        /// Derived classes may override <b>CheckDefeat</b> to implement other scenario-specific
        /// defeat conditions.</para></remarks>

        public virtual bool CheckDefeat(Command command) {
            WorldState world = command.Context.WorldState;

            // check for explicit defeat conditions
            foreach (Condition condition in FactionClass.DefeatConditions)
                switch (condition.Parameter) {

                    case ConditionParameter.Sites:
                        if (Sites.Count <= condition.Threshold) {
                            command.ShowMessageDialog(Global.Strings.EventDefeat,
                                Global.Strings.EventDefeatSites, Id, null);

                            return true;
                        }
                        break;

                    case ConditionParameter.Units:
                        if (Units.Count <= condition.Threshold) {
                            command.ShowMessageDialog(Global.Strings.EventDefeat,
                                Global.Strings.EventDefeatUnits, Id, null);

                            return true;
                        }
                        break;

                    case ConditionParameter.UnitStrength:
                        if (UnitStrength <= condition.Threshold) {
                            command.ShowMessageDialog(Global.Strings.EventDefeat,
                                Global.Strings.EventDefeatUnitStrength, Id, null);

                            return true;
                        }
                        break;

                    case ConditionParameter.Turns:
                        if (world.CurrentTurn > condition.Threshold) {
                            command.ShowMessageDialog(Global.Strings.EventDefeat,
                                Global.Strings.EventDefeatTurns, Id, null);

                            return true;
                        }
                        break;
                }

            // check for resource-based defeat conditions
            for (int i = 0; i < Resources.Count; i++) {
                Variable resource = Resources[i];
                ResourceClass resourceClass = (ResourceClass) resource.VariableClass;

                if (resource.Value <= resourceClass.Defeat) {
                    string message = String.Format(ApplicationInfo.Culture,
                        Global.Strings.EventDefeatResource, resource.Name);

                    command.ShowMessageDialog(Global.Strings.EventDefeat, message, Id, null);
                    return true;
                }
            }

            return false;
        }

        #endregion
        #region CheckVictory

        /// <summary>
        /// Checks whether the <see cref="Faction"/> meets a victory condition.</summary>
        /// <param name="command">
        /// The <see cref="Command"/> being executed.</param>
        /// <returns>
        /// <c>true</c> if the <see cref="Faction"/> meets a victory condition; otherwise,
        /// <c>false</c>.</returns>
        /// <remarks><para>
        /// <b>CheckVictory</b> returns <c>true</c> in the following cases:
        /// </para><list type="bullet"><item>
        /// The <see cref="WorldState.Factions"/> collection of the current <see
        /// cref="ExecutionContext.WorldState"/> has exactly one element.
        /// </item><item>
        /// Any one of the explicit <see cref="Scenario.FactionClass.VictoryConditions"/> of the
        /// underlying <see cref="FactionClass"/> is met.
        /// </item><item>
        /// Any <see cref="Resources"/> value is equal to or greater than the <see
        /// cref="ResourceClass.Victory"/> threshold of the matching <see
        /// cref="VariableSection.Resources"/> element defined by the current <see
        /// cref="VariableSection"/>.
        /// </item></list><para>
        /// <b>CheckVictory</b> adds a message event to the specified <paramref name="command"/>
        /// when returning <c>true</c>, but does not generate an event when returning <c>false</c>.
        /// </para><para>
        /// Derived classes may override <b>CheckVictory</b> to implement other scenario-specific
        /// victory conditions.</para></remarks>

        public virtual bool CheckVictory(Command command) {
            WorldState world = command.Context.WorldState;

            // check for sole survivor
            if (world.Factions.Count == 1) {
                Debug.Assert(world.Factions[0] == this);

                command.ShowMessageDialog(Global.Strings.EventVictory,
                    Global.Strings.EventVictorySurvivor, Id, null);

                return true;
            }

            // check for explicit victory conditions
            foreach (Condition condition in FactionClass.VictoryConditions)
                switch (condition.Parameter) {

                    case ConditionParameter.Sites:
                        if (Sites.Count >= condition.Threshold) {
                            command.ShowMessageDialog(Global.Strings.EventVictory,
                                Global.Strings.EventVictorySites, Id, null);

                            return true;
                        }
                        break;

                    case ConditionParameter.Units:
                        if (Units.Count >= condition.Threshold) {
                            command.ShowMessageDialog(Global.Strings.EventVictory,
                                Global.Strings.EventVictoryUnits, Id, null);

                            return true;
                        }
                        break;

                    case ConditionParameter.UnitStrength:
                        if (UnitStrength >= condition.Threshold) {
                            command.ShowMessageDialog(Global.Strings.EventVictory,
                                Global.Strings.EventVictoryUnitStrength, Id, null);

                            return true;
                        }
                        break;

                    case ConditionParameter.Turns:
                        if (world.CurrentTurn > condition.Threshold) {
                            command.ShowMessageDialog(Global.Strings.EventVictory,
                                Global.Strings.EventVictoryTurns, Id, null);

                            return true;
                        }
                        break;
                }

            // check for resource-based victory conditions
            for (int i = 0; i < Resources.Count; i++) {

                Variable resource = Resources[i];
                ResourceClass resourceClass = (ResourceClass) resource.VariableClass;

                if (resource.Value >= resourceClass.Victory) {
                    string message = String.Format(ApplicationInfo.Culture,
                        Global.Strings.EventVictoryResource, resource.Name);

                    command.ShowMessageDialog(Global.Strings.EventVictory, message, Id, null);
                    return true;
                }
            }

            return false;
        }

        #endregion
        #region ComputeResourceModifiers

        /// <summary>
        /// Computes all modifiers to the specified <see cref="Resources"/> of the <see
        /// cref="Faction"/>.</summary>
        /// <param name="reset">
        /// The value of the <see cref="Variable.IsResettingResource"/> flag of all <see
        /// cref="Resources"/> to process.</param>
        /// <returns>
        /// A <see cref="CategorizedValueDictionary"/> that maps <see cref="VariableClass.Id"/>
        /// strings of <see cref="ResourceClass"/> objects to <see cref="CategorizedValue"/> objects
        /// indicating the modifiers for each corresponding <see cref="Resources"/> element.
        /// </returns>
        /// <remarks><para>
        /// <b>ComputeResourceModifiers</b> calculates the modifier values that are later applied by
        /// <see cref="UpdateResources"/>. Hexkit also displays these values to the user, which is
        /// why they are subdivided by entity categories.
        /// </para><para>
        /// For all elements in the <see cref="Resources"/> collection whose <see
        /// cref="Variable.IsResettingResource"/> flag equals the specified <paramref name="reset"/>
        /// value, an element with the same key is added to the returned collection.
        /// </para><para>
        /// The <see cref="CategorizedValue"/> stored with the key has the following components:
        /// </para><list type="table"><listheader>
        /// <term>Property</term><description>Value</description>
        /// </listheader><item>
        /// <term><see cref="CategorizedValue.Effect"/></term>
        /// <description>The sum of all matching <b>ResourceModifiers</b> values of all <see
        /// cref="Site.Effects"/> of all owned <see cref="Sites"/>.</description>
        /// </item><item>
        /// <term><see cref="CategorizedValue.Other"/></term>
        /// <description>The matching <see cref="ResourceModifiers"/> value of this <see
        /// cref="Faction"/> itself.</description>
        /// </item><item>
        /// <term><see cref="CategorizedValue.Terrain"/></term>
        /// <description>The sum of all matching <b>ResourceModifiers</b> values of all <see
        /// cref="Site.Terrains"/> of all owned <b>Sites</b>.</description>
        /// </item><item>
        /// <term><see cref="CategorizedValue.Unit"/></term>
        /// <description>The sum of all matching <b>ResourceModifiers</b> values of all owned <see
        /// cref="Units"/> whose <see cref="Entity.IsPlaced"/> flag is <c>true</c>.</description>
        /// </item><item>
        /// <term><see cref="CategorizedValue.Upgrade"/></term>
        /// <description>The sum of all matching <see cref="Entity.ResourceModifiers"/> values of
        /// all owned <see cref="Upgrades"/>.</description>
        /// </item></list><para>
        /// The four entity components accumulate the <see cref="ModifierTarget.Owner"/> values of
        /// any matching <see cref="Entity.ResourceModifiers"/>.</para></remarks>

        public CategorizedValueDictionary ComputeResourceModifiers(bool reset) {
            var modifiers = new CategorizedValueDictionary();

            // check all resources for matching modifiers
            foreach (Variable resource in Resources.Variables) {

                // only process desired resources
                if (reset != resource.IsResettingResource)
                    continue;

                // start with zero values for each modifier
                CategorizedValue value = new CategorizedValue();
                string id = resource.Id;

                // add matching faction modifier
                value.Other += ResourceModifiers.GetValue(id);

                // add modifiers for owned upgrades
                foreach (Entity upgrade in Upgrades)
                    value.Upgrade += upgrade.ResourceModifiers.GetValue(id, ModifierTarget.Owner);

                // add modifiers for owned & placed units
                foreach (Entity unit in Units)
                    if (unit.IsPlaced)
                        value.Unit += unit.ResourceModifiers.GetValue(id, ModifierTarget.Owner);

                // add modifiers for terrains & effects on owned sites
                foreach (Site site in Sites) {
                    foreach (Entity terrain in site.Terrains)
                        value.Terrain += terrain.ResourceModifiers.GetValue(id, ModifierTarget.Owner);
                    foreach (Entity effect in site.Effects)
                        value.Effect += effect.ResourceModifiers.GetValue(id, ModifierTarget.Owner);
                }

                modifiers.Add(id, value);
            }

            return modifiers;
        }

        #endregion
        #region Delete

        /// <summary>
        /// Deletes the <see cref="Faction"/> from the game.</summary>
        /// <param name="command">
        /// The <see cref="Command"/> being executed.</param>
        /// <remarks><para>
        /// <b>Delete</b> performs the following actions:
        /// </para><list type="number"><item>
        /// Set the <see cref="Site.Owner"/> property of all <see cref="Sites"/> to a null
        /// reference, and delete all local <see cref="Site.Units"/>. All other local entities
        /// become unowned along with the <see cref="Site"/> but remain placed.
        /// </item><item>
        /// Delete all remaining unplaced <see cref="Units"/> and <see cref="Terrains"/>.
        /// </item><item>
        /// Delete all <see cref="Upgrades"/>, all of which are unplaced by definition.
        /// </item><item>
        /// Delete the <see cref="Faction"/> itself from the global <see
        /// cref="WorldState.Factions"/> collection.
        /// </item></list><para>
        /// Derived classes may override <b>Delete</b> to perform additional actions when a faction
        /// is eliminated.</para></remarks>

        public virtual void Delete(Command command) {
            WorldState world = command.Context.WorldState;

            // remove owned sites and placed units
            foreach (Site site in world.Sites) {

                // delete owned units placed here
                for (int i = site.Units.Count - 1; i >= 0; i--) {
                    Entity unit = site.Units[i];
                    if (unit.Owner == this) unit.Delete(command);
                }

                // clear ownership of site & other stacks
                if (site.Owner == this)
                    command.SetSiteOwner(site.Location, null);
            }

            // delete unplaced units
            for (int i = Units.Count - 1; i >= 0; i--)
                Units[i].Delete(command);

            // delete unplaced terrains
            for (int i = Terrains.Count - 1; i >= 0; i--)
                Terrains[i].Delete(command);

            // delete upgrades (always unplaced)
            for (int i = Upgrades.Count - 1; i >= 0; i--)
                Upgrades[i].Delete(command);

            // delete faction itself
            command.DeleteFaction(Id);
        }

        #endregion
        #region Destroy

        /// <summary>
        /// Executes the specified <see cref="DestroyCommand"/>.</summary>
        /// <param name="command">
        /// The <see cref="DestroyCommand"/> to execute.</param>
        /// <remarks><para>
        /// <b>Destroy</b> performs the following actions:
        /// </para><list type="number"><item>
        /// Invoke <see cref="Entity.GetDestroyResources"/> on all elements of the <see
        /// cref="Command.Entities"/> collection of the specified <paramref name="command"/>.
        /// </item><item>
        /// Add the resulting values to all matching <see cref="Resources"/> elements.
        /// </item><item>
        /// Invoke <see cref="Delete"/> on all specified <b>Entities</b>.
        /// </item></list><para>
        /// Derived classes may override <b>Destroy</b> to implement different semantics for <see
        /// cref="DestroyCommand"/>.</para></remarks>

        public virtual void Destroy(DestroyCommand command) {

            Debug.Assert(command.Faction.Value == this);
            WorldState world = command.Context.WorldState;

            // create temporary copy of faction resources
            var resources = Resources.ToDictionary();

            // destroy all specified entities
            foreach (EntityReference entity in command.Entities) {
                Debug.Assert(entity.Value.Owner == this);

                // add resources gained by destruction
                var gainedResources = entity.Value.GetDestroyResources(world);
                WorldUtility.AddDictionary(resources, gainedResources);

                // delete entity
                entity.Value.Delete(command);
            }

            // assign final results to faction resources
            for (int i = 0; i < resources.Count; i++)
                command.SetFactionVariable(Id, resources.GetKey(i), resources.GetByIndex(i));
        }

        #endregion
        #region EndTurn

        /// <summary>
        /// Executes the specified <see cref="EndTurnCommand"/>.</summary>
        /// <param name="command">
        /// The <see cref="EndTurnCommand"/> to execute.</param>
        /// <remarks><para>
        /// <b>EndTurn</b> performs the following actions:
        /// </para><list type="number"><item>
        /// Update the resetting <see cref="Resources"/> of all surviving <see
        /// cref="WorldState.Factions"/>.
        /// </item><item>
        /// For all <see cref="Units"/> of this <see cref="Faction"/>, set their <see
        /// cref="Unit.CanAttack"/> and <see cref="Unit.CanMove"/> abilities to <c>true</c>.
        /// </item></list><note type="implementnotes">
        /// Unit abilities must be reset before another faction is activated because units without
        /// the <b>CanAttack</b> ability cannot counter-attack.
        /// </note><para>
        /// Derived classes may override <b>EndTurn</b> to implement different semantics for <see
        /// cref="EndTurnCommand"/>.</para></remarks>

        public virtual void EndTurn(EndTurnCommand command) {

            Debug.Assert(command.Faction.Value == this);
            WorldState world = command.Context.WorldState;

            // update resetting resources for all factions
            foreach (Faction faction in world.Factions)
                faction.UpdateResources(command, true);

            // reset abilities for all owned units
            command.SetFactionUnitsCanAttack(Id, null, true);
            command.SetFactionUnitsCanMove(Id, null, true);
        }

        #endregion
        #region Evaluate(WorldState, IValuable)

        /// <overloads>
        /// Returns a contextual valuation of the specified object.</overloads>
        /// <summary>
        /// Returns a contextual valuation of the specified <see cref="IValuable"/> instance.
        /// </summary>
        /// <param name="worldState">
        /// The <see cref="WorldState"/> on which to base the valuation.</param>
        /// <param name="valuable">
        /// The <see cref="IValuable"/> instance to evaluate.</param>
        /// <returns>
        /// A non-negative <see cref="Double"/> value, indicating the desirability of the specified
        /// <paramref name="valuable"/> instance to a computer player controlling the <see
        /// cref="Faction"/>. Higher values indicate greater desirability.</returns>
        /// <exception cref="ArgumentException">
        /// <paramref name="valuable"/> is not compatible with <see cref="Entity"/>, <see
        /// cref="EntityClass"/>, or <see cref="Site"/>.</exception>
        /// <remarks>
        /// <b>Evaluate</b> returns the result of another overload of this method, depending on the
        /// base type of the specified <paramref name="valuable"/> instance.</remarks>

        public double Evaluate(WorldState worldState, IValuable valuable) {

            Debug.Assert(worldState != null);
            Debug.Assert(valuable != null);

            Entity entity = valuable as Entity;
            if (entity != null) return Evaluate(worldState, entity);

            EntityClass entityClass = valuable as EntityClass;
            if (entityClass != null) return Evaluate(worldState, entityClass);

            Site site = valuable as Site;
            if (site != null) return Evaluate(worldState, site);

            ThrowHelper.ThrowArgumentException(
                "valuable", Tektosyne.Strings.ArgumentNotInTypes +
                String.Join(", ", typeof(Entity), typeof(EntityClass), typeof(Site)));

            return 0.0;
        }

        #endregion
        #region Evaluate(WorldState, Entity)

        /// <summary>
        /// Returns a contextual valuation of the specified <see cref="Entity"/>.</summary>
        /// <param name="worldState">
        /// The <see cref="WorldState"/> on which to base the valuation.</param>
        /// <param name="entity">
        /// The <see cref="Entity"/> to evaluate.</param>
        /// <returns>
        /// A <see cref="Double"/> value in the standard interval [0,1], indicating the desirability
        /// of the specified <paramref name="entity"/> to a computer player controlling this <see
        /// cref="Faction"/>. Higher values indicate greater desirability.</returns>
        /// <remarks><para>
        /// <b>Evaluate</b> returns a contextual valuation of the specified <paramref
        /// name="entity"/>. That is, <b>Evaluate</b> may consider the current state of this <see
        /// cref="Faction"/> and of the specified <paramref name="worldState"/>, in addition to the
        /// context-free <see cref="Entity.Valuation"/> provided by the <paramref name="entity"/>
        /// itself.
        /// </para><para>
        /// This base class implementation of <b>Evaluate</b> always returns the context-free
        /// <b>Valuation</b> of the <paramref name="entity"/>.
        /// </para><para>
        /// Derived classes may override <b>Evaluate</b> to change the contextual valuation of
        /// <b>Entity</b> objects.</para></remarks>

        public virtual double Evaluate(WorldState worldState, Entity entity) {

            Debug.Assert(worldState != null);
            Debug.Assert(entity != null);

            return entity.Valuation;
        }

        #endregion
        #region Evaluate(WorldState, EntityClass)

        /// <summary>
        /// Returns a contextual valuation of the specified <see cref="EntityClass"/> for the
        /// purpose of a <see cref="BuildCommand"/>.</summary>
        /// <param name="worldState">
        /// The <see cref="WorldState"/> on which to base the valuation.</param>
        /// <param name="entityClass">
        /// The <see cref="EntityClass"/> to evaluate.</param>
        /// <returns>
        /// A <see cref="Double"/> value in the standard interval [0,1], indicating the desirability
        /// of the specified <paramref name="entityClass"/> to a computer player controlling the
        /// <see cref="Faction"/>. Higher values indicate greater desirability.</returns>
        /// <remarks><para>
        /// <b>Evaluate</b> returns a contextual valuation of the specified <paramref
        /// name="entityClass"/>. That is, <b>Evaluate</b> may consider the current state of this
        /// <see cref="Faction"/> and of the specified <paramref name="worldState"/>, in addition to
        /// the context-free <see cref="EntityClass.Valuation"/> provided by <paramref
        /// name="entityClass"/> itself.
        /// </para><para>
        /// This base class implementation of <b>Evaluate</b> returns the context-free <see
        /// cref="EntityClass.Valuation"/> of the specified <paramref name="entityClass"/> if the
        /// context-free <b>Valuation</b> is zero, or if the <b>Faction</b> cannot build the
        /// <paramref name="entityClass"/>, as determined by <see cref="GetBuildableClasses"/>.
        /// </para><para>
        /// Otherwise, <b>Evaluate</b> divides the the context-free <b>Valuation</b> of <paramref
        /// name="entityClass"/> by the sum of the context-free valuations of all buildable entity
        /// classes of the same <see cref="EntityClass.Category"/>.
        /// </para><para>
        /// <b>Evaluate</b> then computes the share of entitites based on the specified <paramref
        /// name="entityClass"/> among all buildable entities of the same <b>Category</b> that
        /// already belong to the <see cref="Faction"/>, if any. The valuation is doubled and this
        /// share subtracted. This adjusts the valuation upward or downward if the faction owns too
        /// few or too many entities based on the <paramref name="entityClass"/>, respectively.
        /// </para><para>
        /// The final valuation is restricted to the standard interval [0,1].
        /// </para><para>
        /// Derived classes may override <b>Evaluate</b> to change the contextual valuation of
        /// <b>EntityClass</b> objects. Return zero to prevent <paramref name="entityClass"/> from
        /// being built.</para></remarks>

        public virtual double Evaluate(WorldState worldState, EntityClass entityClass) {

            Debug.Assert(worldState != null);
            Debug.Assert(entityClass != null);

            // start with context-free valuation
            double value = entityClass.Valuation;
            if (value == 0.0) return value;

            // check if we can build this entity class
            var buildable = GetBuildableClasses(entityClass.Category);
            if (!buildable.ContainsKey(entityClass.Id))
                return entityClass.Valuation;

            // sum up valuations of all buildable classes
            double buildableSum = 0.0;
            for (int i = 0; i < buildable.Count; i++)
                buildableSum += buildable.GetByIndex(i).Valuation;

            // relative value compared to all classes
            Debug.Assert(buildableSum > 0.0);
            value /= buildableSum;

            // count instances owned by the faction
            int specifiedCount = 0, buildableCount = 0;
            EntityList entities = GetEntities(entityClass.Category);

            for (int i = 0; i < entities.Count; i++) {
                EntityClass instanceClass = entities[i].EntityClass;

                // count instances of buildable classes
                if (buildable.ContainsKey(instanceClass.Id)) {
                    ++buildableCount;

                    // count instances of specified class
                    if (entityClass == instanceClass)
                        ++specifiedCount;
                }
            }

            // adjust value by share of instances
            double instanceShare = 0.0;
            if (buildableCount > 0)
                instanceShare = (double) specifiedCount / buildableCount;
            value = 2 * value - instanceShare;

            // limit to standard interval
            if (value < 0.0) value = 0.0;
            else if (value > 1.0) value = 1.0;

            return value;
        }

        #endregion
        #region Evaluate(WorldState, Site)

        /// <summary>
        /// Returns a contextual valuation of the specified <see cref="Site"/>.</summary>
        /// <param name="worldState">
        /// The <see cref="WorldState"/> on which to base the valuation.</param>
        /// <param name="site">
        /// The <see cref="Site"/> to evaluate.</param>
        /// <returns>
        /// A <see cref="Double"/> value in the standard interval [0,1], indicating the desirability
        /// of the specified <paramref name="site"/> to a computer player controlling the <see
        /// cref="Faction"/>. Higher values indicate greater desirability.</returns>
        /// <remarks><para>
        /// <b>Evaluate</b> returns a contextual valuation of the specified <paramref name="site"/>.
        /// That is, <b>Evaluate</b> may consider the  current state of this <see cref="Faction"/>
        /// and of the specified <paramref name="worldState"/>, in addition to the context-free <see
        /// cref="Site.Valuation"/> provided by <paramref name="site"/> itself.
        /// </para><para>
        /// This base class implementation of <b>Evaluate</b> returns a contextual valuation that
        /// considers the home sites of all surviving factions, as follows:
        /// </para><list type="bullet"><item>
        /// 0.0 if the <see cref="WorldState.MaxSiteValuation"/> of the specified <paramref
        /// name="worldState"/> is zero, indicating that the current scenario does not use site
        /// valuations at all.
        /// </item><item>
        /// 1.0 if <paramref name="site"/> is the <see cref="HomeSite"/> of this <b>Faction</b>.
        /// </item><item>
        /// 0.9 if <paramref name="site"/> is the <b>HomeSite</b> of another element in the <see
        /// cref="WorldState.Factions"/> collection of the specified <paramref name="worldState"/>.
        /// </item><item>
        /// Otherwise, the context-free <see cref="Site.Valuation"/> of the specified <paramref
        /// name="site"/>, divided by the <see cref="WorldState.MaxSiteValuation"/> of the specified
        /// <paramref name="worldState"/> and multiplied by 0.8. The resulting value lies in the
        /// interval [0, 0.8].
        /// </item></list><para>
        /// Derived classes may override <b>Evaluate</b> to change the contextual valuation of
        /// <b>Site</b> objects.</para></remarks>

        public virtual double Evaluate(WorldState worldState, Site site) {

            Debug.Assert(worldState != null);
            Debug.Assert(site != null);

            // check if scenario uses site valuation at all
            double maxSiteValuation = worldState.MaxSiteValuation;
            if (maxSiteValuation == 0.0) return 0.0;

            // check for own home site
            if (site.Location == HomeSite) return 1.0;

            // check for other faction's home site
            FactionList factions = worldState.Factions;
            for (int i = 0; i < factions.Count; i++) {
                Faction faction = factions[i];
                if (site.Location == faction.HomeSite)
                    return 0.9;
            }

            // compute site valuation
            double value = site.Valuation;
            Debug.Assert(value >= 0.0);

            // scale to standard interval [0,1]
            value /= maxSiteValuation;
            Debug.Assert(value <= 1.0);

            // rescale to interval [0, 0.8]
            return (0.8 * value);
        }

        #endregion
        #region GetAvailableClasses

        /// <summary>
        /// Gets a list of all entity classes of the specified category that are available to the
        /// <see cref="Faction"/>.</summary>
        /// <param name="category">
        /// An <see cref="EntityCategory"/> value indicating which entity classes to return.</param>
        /// <returns>
        /// A <see cref="EntityClassList"/> containing all <see cref="EntityClass"/> objects of the
        /// specified <paramref name="category"/> that are available to the <see cref="Faction"/>.
        /// </returns>
        /// <exception cref="InvalidEnumArgumentException">
        /// <paramref name="category"/> is not a valid <see cref="EntityCategory"/> value.
        /// </exception>
        /// <remarks><para>
        /// <b>GetAvailableClasses</b> returns an empty read-only collection if the specified
        /// <paramref name="category"/> equals <see cref="EntityCategory.Effect"/>.
        /// </para><para>
        /// Otherwise, <b>GetAvailableClasses</b> returns a collection that contains all <see
        /// cref="EntityClass"/> objects of the specified <paramref name="category"/> that this <see
        /// cref="Faction"/> may build, and additionally the <see cref="Entity.EntityClass"/>
        /// objects of any <see cref="Entity"/> objects of the same <paramref name="category"/> that
        /// the <b>Faction</b> owns but may not build. Each <b>EntityClass</b> is only added once.
        /// </para></remarks>

        public EntityClassList GetAvailableClasses(EntityCategory category) {

            EntityClassDictionary buildable = null;
            EntityList instances = null;

            switch (category) {

                case EntityCategory.Unit:
                    buildable = BuildableUnits;
                    instances = Units;
                    break;

                case EntityCategory.Terrain:
                    buildable = BuildableTerrains;
                    instances = Terrains;
                    break;

                case EntityCategory.Effect:
                    return EntityClassList.Empty;

                case EntityCategory.Upgrade:
                    buildable = BuildableUpgrades;
                    instances = Upgrades;
                    break;

                default:
                    ThrowHelper.ThrowInvalidEnumArgumentException(
                        "category", (int) category, typeof(EntityCategory));
                    return null;
            }

            var available = new EntityClassList(buildable.Count);

            // all buildable classes are available
            for (int i = 0; i < buildable.Count; i++)
                available.Add(buildable.GetByIndex(i));

            // all classes of existing instances are available
            for (int i = 0; i < instances.Count; i++) {
                EntityClass entityClass = instances[i].EntityClass;
                if (!available.Contains(entityClass))
                    available.Add(entityClass);
            }

            return available;
        }

        #endregion
        #region GetBuildableClasses

        /// <summary>
        /// Gets a list of all entity classes of the specified category that the <see
        /// cref="Faction"/> may build.</summary>
        /// <param name="category">
        /// An <see cref="EntityCategory"/> value indicating which entity classes to return.</param>
        /// <returns>
        /// A <see cref="EntityClassDictionary"/> containing all <see cref="EntityClass"/> objects
        /// of the specified <paramref name="category"/> that the <see cref="Faction"/> may build.
        /// </returns>
        /// <exception cref="InvalidEnumArgumentException">
        /// <paramref name="category"/> is not a valid <see cref="EntityCategory"/> value.
        /// </exception>
        /// <remarks><para>
        /// <b>GetBuildableClasses</b> returns an empty read-only collection if the specified
        /// <paramref name="category"/> equals <see cref="EntityCategory.Effect"/>.
        /// </para><para>
        /// Otherwise, <b>GetBuildableClasses</b> returns the value of either the <see
        /// cref="BuildableUnits"/>, the <see cref="BuildableTerrains"/>, or the <see
        /// cref="BuildableUpgrades"/> property, depending on the value of <paramref
        /// name="category"/>.</para></remarks>

        public EntityClassDictionary GetBuildableClasses(EntityCategory category) {
            switch (category) {

                case EntityCategory.Unit:    return BuildableUnits;
                case EntityCategory.Terrain: return BuildableTerrains;
                case EntityCategory.Effect:  return EntityClassDictionary.Empty;
                case EntityCategory.Upgrade: return BuildableUpgrades;

                default:
                    ThrowHelper.ThrowInvalidEnumArgumentException(
                        "category", (int) category, typeof(EntityCategory));
                    return null;
            }
        }

        #endregion
        #region GetBuildCount

        /// <summary>
        /// Gets the number of times the specified <see cref="EntityClass"/> may be instantiated
        /// with a <see cref="BuildCommand"/>.</summary>
        /// <param name="worldState">
        /// The <see cref="WorldState"/> on which to execute the <see cref="BuildCommand"/>.</param>
        /// <param name="entityClass">
        /// The <see cref="EntityClass"/> whose build count to return.</param>
        /// <returns>
        /// The number of times the specified <paramref name="entityClass"/> may be instantiated
        /// with a <see cref="BuildCommand"/>.</returns>
        /// <remarks><para>
        /// <b>GetBuildCount</b> returns the lowest quotient of any two matching resources in the
        /// <see cref="Resources"/> collection and in the collection returned by <see
        /// cref="GetBuildResources"/> for the specified <paramref name="worldState"/> and <paramref
        /// name="entityClass"/>, provided that the values in both collections are positive.
        /// </para><para>
        /// <b>GetBuildCount</b> may also return the following special values:
        /// </para><list type="bullet"><item>
        /// Zero if the <see cref="Faction"/> may not build the specified <paramref
        /// name="entityClass"/>, as determined by <see cref="GetBuildableClasses"/>.
        /// </item><item>
        /// Zero if <b>Resources</b> defines no value, or a value of zero or less than zero, for a
        /// resource for which <b>GetBuildResources</b> returns a positive value, indicating that a
        /// required resource is unavailable.
        /// </item><item>
        /// 999 if the lowest quotient is 1,000 or greater.
        /// </item><item>
        /// 999 if the collection returned by <b>GetBuildResources</b> is empty or contains only
        /// negative values. In this case, there is no limit on how many times the specified
        /// <paramref name="entityClass"/> may be instantiated.
        /// </item></list><para>
        /// Derived classes may override <b>GetBuildCount</b> to implement different semantics for
        /// <see cref="BuildCommand"/>.</para></remarks>

        public virtual int GetBuildCount(WorldState worldState, EntityClass entityClass) {

            // cannot build unless allowed
            var buildable = GetBuildableClasses(entityClass.Category);
            if (!buildable.ContainsKey(entityClass.Id))
                return 0;

            int count = 999; // default maximum build count

            // determine resources required for specified class
            var buildResources = GetBuildResources(worldState, entityClass);

            // restrict build count by any required resources
            for (int i = 0; i < buildResources.Count; i++) {

                // get required amount of current resource
                int required = buildResources.GetByIndex(i);
                if (required <= 0) continue;

                // get available amount of current resource
                string id = buildResources.GetKey(i);
                int available = Resources.GetValue(id);

                // cannot build if required resource is unavailable
                if (available <= 0) return 0;

                // restrict by quotient of available and required resources
                count = Math.Min(count,  available / required);
            }

            return count;
        }

        #endregion
        #region GetBuildResources

        /// <summary>
        /// Gets the resources required to execute a <see cref="BuildCommand"/> for the specified
        /// <see cref="EntityClass"/>.</summary>
        /// <param name="worldState">
        /// The <see cref="WorldState"/> on which to execute the <see cref="BuildCommand"/>.</param>
        /// <param name="entityClass">
        /// The <see cref="EntityClass"/> whose required resources to return.</param>
        /// <returns>
        /// A <see cref="VariableValueDictionary"/> that maps <see cref="VariableClass.Id"/> strings
        /// of <see cref="VariableClass"/> objects to the amount of each resource required to
        /// execute the <see cref="BuildCommand"/>.</returns>
        /// <remarks><para>
        /// <b>GetBuildResources</b> returns a copy of the <see cref="EntityClass.BuildResources"/>
        /// collection of the specified <paramref name="entityClass"/>.
        /// </para><note type="caution">
        /// The returned collection must be mutable. Do not simply return a reference to <see
        /// cref="EntityClass.BuildResources"/>; always create a copy.
        /// </note><para>
        /// Derived classes may override <b>GetBuildResources</b> to implement different semantics
        /// for <see cref="BuildCommand"/>.</para></remarks>

        public virtual VariableValueDictionary GetBuildResources(
            WorldState worldState, EntityClass entityClass) {

            return new VariableValueDictionary(entityClass.BuildResources);
        }

        #endregion
        #region GetConditionValue

        /// <summary>
        /// Gets the current value of the specified <see cref="ConditionParameter"/> for the <see
        /// cref="Faction"/> and the specified <see cref="WorldState"/>.</summary>
        /// <param name="worldState">
        /// The <see cref="WorldState"/> that contains the <see cref="Faction"/>.</param>
        /// <param name="parameter">
        /// A <see cref="ConditionParameter"/> value indicating which value to return.</param>
        /// <returns>
        /// The current value of the specified <paramref name="parameter"/>.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="worldState"/> is a null reference.</exception>
        /// <exception cref="InvalidEnumArgumentException">
        /// <paramref name="parameter"/> is not a valid <see cref="ConditionParameter"/> value.
        /// </exception>
        /// <remarks><para>
        /// <b>GetConditionValue</b> returns one of the following values, depending on the specified
        /// <paramref name="parameter"/>:
        /// </para><list type="table"><listheader>
        /// <term>Parameter</term><description>Value</description>
        /// </listheader><item>
        /// <term><see cref="ConditionParameter.Sites"/></term>
        /// <description>Number of <see cref="Sites"/> owned by the <see cref="Faction"/>.
        /// </description></item><item>
        /// <term><see cref="ConditionParameter.Units"/></term>
        /// <description>Number of <see cref="Units"/> owned by the <see cref="Faction"/>.
        /// </description></item><item>
        /// <term><see cref="ConditionParameter.UnitStrength"/></term>
        /// <description>Total <see cref="UnitStrength"/> of the <see cref="Faction"/>.
        /// </description></item><item>
        /// <term><see cref="ConditionParameter.Turns"/></term>
        /// <description><see cref="WorldState.CurrentTurn"/> of the specified <paramref
        /// name="worldState"/>.</description>
        /// </item></list></remarks>

        public int GetConditionValue(WorldState worldState, ConditionParameter parameter) {
            if (worldState == null)
                ThrowHelper.ThrowArgumentNullException("worldState");

            switch (parameter) {

                case ConditionParameter.Sites:
                    return Sites.Count;

                case ConditionParameter.Units:
                    return Units.Count;

                case ConditionParameter.UnitStrength:
                    return UnitStrength;

                case ConditionParameter.Turns:
                    return worldState.CurrentTurn;

                default:
                    ThrowHelper.ThrowInvalidEnumArgumentException(
                        "parameter", (int) parameter, typeof(ConditionParameter));
                    return 0;
            }
        }

        #endregion
        #region GetEntities(EntityCategory)

        /// <overloads>
        /// Gets a list of all entities of the specified category that are owned by the <see
        /// cref="Faction"/>.</overloads>
        /// <summary>
        /// Gets a list of all entities of the specified category that are owned by the <see
        /// cref="Faction"/>.</summary>
        /// <param name="category">
        /// A <see cref="EntityCategory"/> value indicating which entities to return.</param>
        /// <returns>
        /// A read-only <see cref="EntityList"/> containing all entities in the specified <paramref
        /// name="category"/> that belong to the <see cref="Faction"/>.</returns>
        /// <exception cref="InvalidEnumArgumentException">
        /// <paramref name="category"/> is not a valid <see cref="EntityCategory"/> value.
        /// </exception>
        /// <remarks><para>
        /// <b>GetEntities</b> returns an empty read-only collection if the specified <paramref
        /// name="category"/> equals <see cref="EntityCategory.Effect"/>.
        /// </para><para>
        /// Otherwise, <b>GetEntities</b> returns the value of either the <see cref="Units"/>, the
        /// <see cref="Terrains"/>, or the <see cref="Upgrades"/> property, depending on the value
        /// of <paramref name="category"/>.</para></remarks>

        public EntityList GetEntities(EntityCategory category) {
            switch (category) {

                case EntityCategory.Unit:    return Units;
                case EntityCategory.Terrain: return Terrains;
                case EntityCategory.Effect:  return EntityList.Empty;
                case EntityCategory.Upgrade: return Upgrades;

                default:
                    ThrowHelper.ThrowInvalidEnumArgumentException(
                        "category", (int) category, typeof(EntityCategory));
                    return null;
            }
        }

        #endregion
        #region GetEntities(EntityCategory, Boolean)

        /// <summary>
        /// Gets a list of all entities of the specified category that are owned by the <see
        /// cref="Faction"/> and that are either placed or unplaced.</summary>
        /// <param name="category">
        /// A <see cref="EntityCategory"/> value indicating which entities to return.</param>
        /// <param name="isPlaced">
        /// <c>true</c> to return only placed entities of the specified <paramref name="category"/>;
        /// <c>false</c> to return only unplaced entities.</param>
        /// <returns>
        /// A <see cref="EntityList"/> containing all entities in the specified <paramref
        /// name="category"/> that belong to the <see cref="Faction"/>, and whose <see
        /// cref="Entity.IsPlaced"/> flag equals the specified <paramref name="isPlaced"/> value.
        /// </returns>
        /// <exception cref="InvalidEnumArgumentException">
        /// <paramref name="category"/> is not a valid <see cref="EntityCategory"/> value.
        /// </exception>
        /// <remarks><para>
        /// <b>GetEntities</b> returns an empty collection if the specified <paramref
        /// name="category"/> equals <see cref="EntityCategory.Effect"/>. Otherwise,
        /// <b>GetEntities</b> returns the desired subset of either the <see cref="Units"/>, the
        /// <see cref="Terrains"/>, or the <see cref="Upgrades"/> collection, depending on the value
        /// of <paramref name="category"/>.
        /// </para><note type="implementnotes">
        /// The returned collection will not reflect subsequent changes to the <see cref="Units"/>,
        /// <see cref="Terrains"/>, or <see cref="Upgrades"/> collection, or to the <see
        /// cref="Entity.IsPlaced"/> properties of its elements.</note></remarks>

        public EntityList GetEntities(EntityCategory category, bool isPlaced) {

            EntityList entities = GetEntities(category);
            EntityList filtered = new EntityList(entities.Count);

            // add entities with matching IsPlaced flag
            for (int i = 0; i < entities.Count; i++) {
                Entity entity = entities[i];

                if (entity.IsPlaced == isPlaced)
                    filtered.Add(entity);
            }

            return filtered;
        }

        #endregion
        #region GetEntity

        /// <summary>
        /// Gets the <see cref="Entity"/> with the specified identifier that is owned by the <see
        /// cref="Faction"/>.</summary>
        /// <param name="id">
        /// An <see cref="Entity.Id"/> string indicating the <see cref="Entity"/> to locate.</param>
        /// <returns><para>
        /// The <see cref="Entity"/> owned by the <see cref="Faction"/> whose <see
        /// cref="Entity.Id"/> property equals the specified <paramref name="id"/>.
        /// </para><para>-or-</para><para>
        /// A null reference if no matching <see cref="Entity"/> was found.</para></returns>
        /// <exception cref="ArgumentNullOrEmptyException">
        /// <paramref name="id"/> is a null reference or an empty string.</exception>

        public Entity GetEntity(string id) {
            if (String.IsNullOrEmpty(id))
                ThrowHelper.ThrowArgumentNullOrEmptyException("id");

            Entity entity;
            foreach (EntityCategory category in EntitySection.AllCategories)
                if (GetEntities(category).TryGetValue(id, out entity))
                    return entity;

            return null;
        }

        #endregion
        #region GetPlaceTargets

        /// <summary>
        /// Gets all valid target locations for a <see cref="PlaceCommand"/> with entities of the
        /// specified <see cref="EntityClass"/>.</summary>
        /// <param name="worldState">
        /// The <see cref="WorldState"/> on which to execute the <see cref="PlaceCommand"/>.</param>
        /// <param name="entityClass">
        /// The <see cref="Entity.EntityClass"/> of the <see cref="Entity"/> to place.</param>
        /// <returns>
        /// A <see cref="List{PointI}"/> containing the coordinates of all valid target sites for a
        /// <see cref="PlaceCommand"/> executed on an <see cref="Entity"/> of the specified
        /// <paramref name="entityClass"/>.</returns>
        /// <remarks><para>
        /// <b>GetPlaceTargets</b> never returns a null reference, but it may return an empty
        /// collection if there are no valid targets for a <see cref="PlaceCommand"/>.
        /// </para><para>
        /// To determine the list of valid placement sites, <b>GetPlaceTargets</b> first selects a
        /// list of candidate sites, as follows:
        /// </para><list type="bullet"><item>
        /// All valid elements in the <see cref="EntityClass.PlaceSites"/> collection of the
        /// specified <paramref name="entityClass"/> are always candidates.
        /// </item><item>
        /// Skip the remaining steps if the <see cref="EntityClass.UseDefaultPlace"/> flag of the
        /// specified <paramref name="entityClass"/> is <c>false</c>.
        /// </item><item>
        /// If the <see cref="WorldState.CurrentTurn"/> index of the specified <paramref
        /// name="worldState"/> is zero, all <see cref="Sites"/> are additional candidates.
        /// </item><item>
        /// Otherwise, the only additional candidate is the <see cref="HomeSite"/> if valid.
        /// </item></list><para>
        /// <b>GetPlaceTargets</b> then returns all unique candidate sites for which <see
        /// cref="CanPlace"/> returns <c>true</c>. The resulting collection may be empty.
        /// </para><para>
        /// The results of <b>GetPlaceTargets</b> are only valid for a <em>single</em> <see
        /// cref="PlaceCommand"/> with a <em>single</em> <see cref="Entity"/> of the specified
        /// <paramref name="entityClass"/>.
        /// </para><list type="bullet"><item>
        /// After issuing a <see cref="PlaceCommand"/>, you must always call <see cref="CanPlace"/>
        /// to re-check your arguments before issuing another command, as the changed <see
        /// cref="WorldState"/> might cause <b>CanPlace</b> to fail.
        /// </item><item>
        /// Before issuing a <see cref="PlaceCommand"/> for <em>multiple</em> entities, you must
        /// always call <see cref="CanPlace"/> with <em>all</em> participants, as
        /// <b>GetPlaceTargets</b> only checks <b>CanPlace</b> for individual entities.
        /// </item></list><para>
        /// Derived classes may override <b>GetPlaceTargets</b> to implement different semantics for
        /// <see cref="PlaceCommand"/>.
        /// </para><note type="caution">
        /// Do not change any reference parameters or their contents! Create a copy if you need to
        /// change them.</note></remarks>

        public virtual List<PointI> GetPlaceTargets(
            WorldState worldState, EntityClass entityClass) {

            var sites = new List<Site>();

            // add all valid class-specific targets
            var placeSites = entityClass.PlaceSites;
            for (int i = 0; i < placeSites.Count; i++) {
                Site site = worldState.GetSite(placeSites[i]);
                if (site != null) sites.Add(site);
            }

            // add default targets if enabled
            if (entityClass.UseDefaultPlace) {
                if (worldState.CurrentTurn == 0) {
                    // can place on all owned sites on first turn
                    sites.AddRange(Sites);
                } else {
                    // can only place on home after first turn
                    Site site = worldState.GetSite(HomeSite);
                    if (site != null) sites.Add(site);
                }
            }

            // extract unique site coordinates
            List<PointI> targets = WorldUtility.GetSiteLocations(sites);

            // remove any sites for which CanPlace fails
            EntityClass[] entityClasses = { entityClass };
            for (int i = targets.Count - 1; i >= 0; i--)
                if (!CanPlace(worldState, entityClasses, targets[i]))
                    targets.RemoveAt(i);

            return targets;
        }

        #endregion
        #region GetSupplyTargets

        /// <summary>
        /// Calculates the <see cref="Site.SupplyResources"/> of all <see cref="WorldState.Sites"/>,
        /// and returns a list of those that can resupply <see cref="Units"/>.</summary>
        /// <param name="worldState">
        /// The <see cref="WorldState"/> whose <see cref="WorldState.Sites"/> to process.</param>
        /// <returns>
        /// A <see cref="List{Site}"/> containing all <see cref="WorldState.Sites"/> elements of the
        /// specified <paramref name="worldState"/> whose <see cref="Site.SupplyResources"/>
        /// property is not a null reference and contains at least one positive value.</returns>
        /// <remarks><para>
        /// <b>GetSupplyTargets</b> never returns a null reference, but it returns an empty
        /// collection if <see cref="SupplyResources"/> is an empty collection.
        /// </para><para>
        /// Otherwise, <b>GetSupplyTargets</b> processes all <see cref="WorldState.Sites"/> in the
        /// specified <paramref name="worldState"/>, as follows:
        /// </para><list type="number"><item>
        /// If the <see cref="Site"/> contains enemy <see cref="Site.Units"/>, sets its <see
        /// cref="Site.SupplyResources"/> property to a null reference.
        /// </item><item>
        /// Otherwise, invoke <see cref="Site.SetSupplyResources"/> on the <see cref="Site"/>.
        /// </item><item>
        /// Add the <see cref="Site"/> to the returned collection if <see
        /// cref="Site.HasPositiveSupplies"/> now returns <c>true</c>.
        /// </item></list><para>
        /// Derived classes may override <b>GetSupplyTargets</b> to indicate which map sites can
        /// increase the <see cref="Entity.Resources"/> of some or all <see cref="Units"/> owned by
        /// the faction.</para></remarks>

        public virtual List<Site> GetSupplyTargets(WorldState worldState) {
            Debug.Assert(worldState != null);

            // check if supply enabled
            if (SupplyResources.Count == 0)
                return new List<Site>(0);

            var targets = new List<Site>();

            // check all sites for available supplies
            foreach (Site site in worldState.Sites) {

                // skip inaccessible sites
                if (site.HasAlienUnits(this)) {
                    site.SetSupplyResources(null);
                    continue;
                }

                // compute supply resources on site
                site.SetSupplyResources(this);

                // add sites with positive supplies
                if (site.HasPositiveSupplies())
                    targets.Add(site);
            }

            return targets;
        }

        #endregion
        #region Initialize

        /// <summary>
        /// Completes initialization of the <see cref="Faction"/>.</summary>
        /// <param name="command">
        /// The <see cref="BeginTurnCommand"/> being executed.</param>
        /// <remarks><para>
        /// <b>Initialize</b> does nothing. This method is called once for <em>all</em> <see
        /// cref="WorldState.Factions"/> of the current <see cref="ExecutionContext.WorldState"/>
        /// when the first faction executes its first <see cref="BeginTurnCommand"/>.
        /// </para><para>
        /// Derived classes may override <b>Initialize</b> to perform custom initialization of <see
        /// cref="Faction"/> objects at the start of a new scenario.</para></remarks>

        public virtual void Initialize(BeginTurnCommand command) { }

        #endregion
        #region OnVariableChanged

        /// <summary>
        /// Executes when a <see cref="Variable"/> value of the <see cref="Faction"/> has changed.
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
        /// <b>OnVariableChanged</b> does nothing. This method is called by the <see
        /// cref="Command"/> methods <see cref="Command.SetFactionVariable"/> and <see
        /// cref="Command.SetFactionVariableModifier"/> after the indicated <see cref="Variable"/>
        /// has been changed to the specified <paramref name="value"/>.
        /// </para><para>
        /// Derived classes may override <b>OnVariableChanged</b> to perform additional actions when
        /// changing a <see cref="Variable"/>. Overrides should not change the same <b>Variable</b>
        /// of this <see cref="Faction"/> again, either directly or indirectly, to avoid recursion.
        /// </para><note type="caution">
        /// The specified <paramref name="value"/> is the one that was supplied to the <see
        /// cref="SetFactionVariableInstruction"/> or <see
        /// cref="SetFactionVariableModifierInstruction"/>. The actual new value of the indicated
        /// <see cref="Variable"/> differs if the specified <paramref name="value"/> was outside the
        /// legal range for the <see cref="Variable"/>.</note></remarks>

        public virtual void OnVariableChanged(Command command,
            VariableClass variableClass, int value, bool isModifier) { }

        #endregion
        #region Resign

        /// <summary>
        /// Executes the specified <see cref="ResignCommand"/>.</summary>
        /// <param name="command">
        /// The <see cref="ResignCommand"/> to execute.</param>
        /// <remarks><para>
        /// <b>Resign</b> sets the <see cref="IsResigned"/> property to <c>true</c> and adds a
        /// message event to the specified <paramref name="command"/>.
        /// </para><para>
        /// The caller should immediately issue an <see cref="EndTurnCommand"/> for this <see
        /// cref="Faction"/> if it is the <see cref="WorldState.ActiveFaction"/> and the <see
        /// cref="IsResigned"/> property is indeed <c>true</c> upon return.
        /// </para><para>
        /// Derived classes may override <b>Resign</b> to implement different semantics for <see
        /// cref="ResignCommand"/>.</para></remarks>

        public virtual void Resign(ResignCommand command) {
            Debug.Assert(command.Faction.Value == this);

            command.SetFactionResigned(Id, true);

            // create resignation event
            command.ShowMessageDialog(Global.Strings.EventDefeat,
                Global.Strings.EventResigned, Id, null);
        }

        #endregion
        #region SortEntities

        /// <summary>
        /// Sorts the list of all entities of the specified category that are owned by the <see
        /// cref="Faction"/>.</summary>
        /// <param name="category">
        /// A <see cref="EntityCategory"/> value indicating which entities to return.</param>
        /// <exception cref="InvalidEnumArgumentException">
        /// <paramref name="category"/> is not a valid <see cref="EntityCategory"/> value.
        /// </exception>
        /// <remarks><para>
        /// <b>SortEntities</b> does nothing if the specified <paramref name="category"/> equals
        /// <see cref="EntityCategory.Effect"/>. Otherwise, <b>SortEntities</b> sorts either the
        /// <see cref="Units"/>, the <see cref="Terrains"/>, or the <see cref="Upgrades"/>
        /// collection, depending on the value of <paramref name="category"/>.
        /// </para><para>
        /// The sorting is based on the <see cref="Entity.Id"/> strings of each <see cref="Entity"/>
        /// which are compared using the <see cref="StringUtility.CompareOrdinal"/> method.
        /// </para></remarks>

        public void SortEntities(EntityCategory category) {

            // use natural sorting with ordinal rules on identifiers
            EntityList entities = GetWritableEntities(category);
            if (entities != null)
                entities.Sort((x, y) => StringUtility.CompareOrdinal(x.Id, y.Id));
        }

        #endregion
        #region ToString

        /// <summary>
        /// Returns a <see cref="String"/> that represents the <see cref="Faction"/>.</summary>
        /// <returns>
        /// The value of the <see cref="Name"/> property, if it is not an empty string; otherwise,
        /// the value of the <see cref="Id"/> property.</returns>

        public override string ToString() {
            return (Name.Length == 0 ? Id: Name);
        }

        #endregion
        #endregion
        #region ICloneable Members

        /// <summary>
        /// Creates a new <see cref="Faction"/> object that is a deep copy of the current instance.
        /// </summary>
        /// <returns>
        /// A new <see cref="Faction"/> object that is a deep copy of the current instance.
        /// </returns>
        /// <remarks><para>
        /// <b>Clone</b> calls the <see cref="Faction(Faction)"/> copy constructor with this <see
        /// cref="Faction"/> object.
        /// </para><para>
        /// Derived class implementations of <b>Clone</b> should call the <see
        /// cref="Faction(Faction)"/> copy constructor with this instance, and then perform any
        /// additional copying operations required by the derived class.</para></remarks>

        public virtual object Clone() {
            return new Faction(this);
        }

        #endregion
        #region IKeyedValue Members

        /// <summary>
        /// Gets the identifier of the <see cref="Faction"/>.</summary>
        /// <value>
        /// The value of the <see cref="Id"/> property.</value>

        string IKeyedValue<String>.Key {
            [DebuggerStepThrough]
            get { return Id; }
        }

        #endregion
    }
}
