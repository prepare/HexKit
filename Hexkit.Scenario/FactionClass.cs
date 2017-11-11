using System;
using System.Diagnostics;
using System.Windows.Media;
using System.Xml;

using Tektosyne;
using Tektosyne.Collections;
using Tektosyne.Geometry;
using Tektosyne.Xml;
using Hexkit.Global;

namespace Hexkit.Scenario {
    #region Type Aliases

    using ConditionList = KeyedList<ConditionParameter, Condition>;
    using EntityClassDictionary = SortedListEx<String, EntityClass>;
    using EntityTemplateList = ListEx<EntityTemplate>;
    using IdentifierList = ListEx<String>;
    using ParagraphList = ListEx<String>;
    using VariableValueDictionary = SortedListEx<String, Int32>;

    #endregion

    /// <summary>
    /// Represents a class for a player faction.</summary>
    /// <remarks>
    /// <b>FactionClass</b> is serialized to the XML element "factionClass" defined in <see
    /// cref="FilePaths.ScenarioSchema"/>.</remarks>

    public sealed class FactionClass: ScenarioElement, ICloneable, IMutableKeyedValue<String> {
        #region FactionClass()

        /// <overloads>
        /// Initializes a new instance of the <see cref="FactionClass"/> class.</overloads>
        /// <summary>
        /// Initializes a new instance of the <see cref="FactionClass"/> class with default
        /// properties.</summary>

        internal FactionClass() {

            // determine default color for the new faction
            int count = MasterSection.Instance.Factions.Collection.Count;
            this._color = DefaultColors[count % DefaultColors.Length];
        }

        #endregion
        #region FactionClass(String)

        /// <summary>
        /// Initializes a new instance of the <see cref="FactionClass"/> class with the specified
        /// identifier.</summary>
        /// <param name="id">
        /// The initial value for the <see cref="Id"/> property.</param>

        public FactionClass(string id): this() {
            if (id != null) this._id = String.Intern(id);
        }

        #endregion
        #region FactionClass(FactionClass)

        /// <summary>
        /// Initializes a new instance of the <see cref="FactionClass"/> class with property values
        /// copied from the specified instance.</summary>
        /// <param name="faction">
        /// The <see cref="FactionClass"/> instance whose property values are copied to the new
        /// instance.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="faction"/> is a null reference.</exception>
        /// <remarks><para>
        /// This constructor is called by <see cref="Clone"/> to perform a deep copy of the
        /// specified <paramref name="faction"/>, including a deep copy of all mutable objects that
        /// are owned by the <paramref name="faction"/>.
        /// </para><para>
        /// The value of the <see cref="Color"/> property is <em>not</em> copied but left at its
        /// default value which is obtained from the <see cref="DefaultColors"/> array.
        /// </para></remarks>

        public FactionClass(FactionClass faction): this() {
            if (faction == null)
                ThrowHelper.ThrowArgumentNullException("faction");

            this._id = faction._id;
            this._name = faction._name;
            this._computer = (CustomComputer) faction._computer.Clone();

            this._counters.AddRange(faction._counters);
            this._resources.AddRange(faction._resources);
            this._resourceModifiers.AddRange(faction._resourceModifiers);

            this._buildableUnits.AddRange(faction._buildableUnits);
            this._buildableTerrains.AddRange(faction._buildableTerrains);
            this._buildableUpgrades.AddRange(faction._buildableUpgrades);

            this._victoryConditions.AddRange(faction._victoryConditions);
            this._defeatConditions.AddRange(faction._defeatConditions);

            this._paragraphs.AddRange(faction._paragraphs);
            this._supplyResources.AddRange(faction._supplyResources);

            // create deep copy of owned objects
            this._units = (EntityTemplateList) faction._units.Copy();
            this._terrains = (EntityTemplateList) faction._terrains.Copy();
            this._upgrades = (EntityTemplateList) faction._upgrades.Copy();
        }

        #endregion
        #region Private Fields

        // property backers
        private string _id, _name;
        private Color _color;
        private readonly CustomComputer _computer = new CustomComputer();
        private readonly ParagraphList _paragraphs = new ParagraphList();
        private readonly IdentifierList _supplyResources = new IdentifierList(true);

        private readonly ConditionList
            _defeatConditions = new ConditionList(true),
            _victoryConditions = new ConditionList(true);

        private readonly EntityClassDictionary
            _buildableUnits = new EntityClassDictionary(),
            _buildableTerrains = new EntityClassDictionary(),
            _buildableUpgrades = new EntityClassDictionary();

        private readonly EntityTemplateList
            _units = new EntityTemplateList(),
            _terrains = new EntityTemplateList(),
            _upgrades = new EntityTemplateList();

        private readonly VariableValueDictionary
            _counters = new VariableValueDictionary(),
            _resources = new VariableValueDictionary(),
            _resourceModifiers = new VariableValueDictionary();

        #endregion
        #region AllConditionParameters

        /// <summary>
        /// An <see cref="Array"/> comprising all values in <see cref="ConditionParameter"/>.
        /// </summary>
        /// <remarks>
        /// <b>AllConditionParameters</b> facilitates iterating through all values of the <see
        /// cref="ConditionParameter"/> enumeration. This field holds the result of
        /// <c>Enum.GetValues(typeof(ConditionParameter))</c>.</remarks>

        public static readonly ConditionParameter[] AllConditionParameters =
            (ConditionParameter[]) Enum.GetValues(typeof(ConditionParameter));

        #endregion
        #region DefaultColors

        /// <summary>
        /// An <see cref="Array"/> of default colors for <see cref="FactionClass"/> objects.
        /// </summary>
        /// <remarks>
        /// <b>DefaultColors</b> provides eight predefined <see cref="System.Windows.Media.Color"/>
        /// values that are used to initialize the <see cref="Color"/> property of newly created
        /// <see cref="FactionClass"/> objects.</remarks>

        public static readonly Color[] DefaultColors = {
            Color.FromArgb(255, 255,   0,   0),  // red
            Color.FromArgb(255,   0, 128, 255),  // blue
            Color.FromArgb(255, 144,   0, 144),  // purple
            Color.FromArgb(255,   0, 144,   0),  // green
            Color.FromArgb(255, 255, 216,   0),  // yellow
            Color.FromArgb(255, 192, 192, 192),  // gray
            Color.FromArgb(255,  64, 216, 216),  // cyan
            Color.FromArgb(255, 128,  96,   0),  // brown
        };

        #endregion
        #region Public Properties
        #region BuildableTerrains

        /// <summary>
        /// Gets a list of all terrain classes that the <see cref="FactionClass"/> may build.
        /// </summary>
        /// <value>
        /// A <see cref="EntityClassDictionary"/> that maps <see cref="EntityClass.Id"/> strings to
        /// the corresponding <see cref="TerrainClass"/> objects. The default is an empty
        /// collection.</value>
        /// <remarks><para>
        /// <b>BuildableTerrains</b> never returns a null reference. The collection is read-only if
        /// <see cref="ApplicationInfo.IsEditor"/> is <c>false</c>.
        /// </para><para>
        /// This property holds the values of all "buildableTerrains" XML elements, with the <see
        /// cref="EntityClassDictionary.Keys"/> holding the values of the "ids" attributes, and the
        /// <see cref="EntityClassDictionary.Values"/> holding null references until successful
        /// validation.</para></remarks>

        public EntityClassDictionary BuildableTerrains {
            [DebuggerStepThrough]
            get {
                return (ApplicationInfo.IsEditor ?
                    this._buildableTerrains : this._buildableTerrains.AsReadOnly());
            }
        }

        #endregion
        #region BuildableUnits

        /// <summary>
        /// Gets a list of all unit classes that the <see cref="FactionClass"/> may build.</summary>
        /// <value>
        /// A <see cref="EntityClassDictionary"/> that maps <see cref="EntityClass.Id"/> strings to
        /// the corresponding <see cref="UnitClass"/> objects. The default is an empty collection.
        /// </value>
        /// <remarks><para>
        /// <b>BuildableUnits</b> never returns a null reference. The collection is read-only if
        /// <see cref="ApplicationInfo.IsEditor"/> is <c>false</c>.
        /// </para><para>
        /// This property holds the values of all "buildableUnits" XML elements, with the <see
        /// cref="EntityClassDictionary.Keys"/> holding the values of the "ids" attributes, and the
        /// <see cref="EntityClassDictionary.Values"/> holding null references until successful
        /// validation.</para></remarks>

        public EntityClassDictionary BuildableUnits {
            [DebuggerStepThrough]
            get {
                return (ApplicationInfo.IsEditor ?
                    this._buildableUnits : this._buildableUnits.AsReadOnly());
            }
        }

        #endregion
        #region BuildableUpgrades

        /// <summary>
        /// Gets a list of all upgrade classes that the <see cref="FactionClass"/> may build.
        /// </summary>
        /// <value>
        /// A <see cref="EntityClassDictionary"/> that maps <see cref="EntityClass.Id"/> strings to
        /// the corresponding <see cref="UpgradeClass"/> objects. The default is an empty
        /// collection. </value>
        /// <remarks><para>
        /// <b>BuildableUpgrades</b> never returns a null reference. The collection is read-only if
        /// <see cref="ApplicationInfo.IsEditor"/> is <c>false</c>.
        /// </para><para>
        /// This property holds the values of all "buildableUpgrades" XML elements, with the <see
        /// cref="EntityClassDictionary.Keys"/> holding the values of the "ids" attributes, and the
        /// <see cref="EntityClassDictionary.Values"/> holding null references until successful
        /// validation.</para></remarks>

        public EntityClassDictionary BuildableUpgrades {
            [DebuggerStepThrough]
            get {
                return (ApplicationInfo.IsEditor ?
                    this._buildableUpgrades : this._buildableUpgrades.AsReadOnly());
            }
        }

        #endregion
        #region Color

        /// <summary>
        /// Gets or sets the display color of the <see cref="FactionClass"/>.</summary>
        /// <value>
        /// A <see cref="System.Windows.Media.Color"/> value indicating how the faction based on the
        /// <see cref="FactionClass"/> should be represented on a color-coded display.</value>
        /// <exception cref="InvalidOperationException">
        /// The property is set, and <see cref="ApplicationInfo.IsEditor"/> is <c>false</c>.
        /// </exception>
        /// <remarks><para>
        /// <b>Color</b> holds the value of the "color" XML element, if specified; or an element of
        /// the <see cref="DefaultColors"/> array otherwise.
        /// </para><para>
        /// The default value of the <b>Color</b> property is the element of the <see
        /// cref="DefaultColors"/> array whose index equals the size of the <see
        /// cref="FactionSection.Collection"/> of the current <see cref="FactionSection"/> at the
        /// time when this <see cref="FactionClass"/> instance is initialized.</para></remarks>

        public Color Color {
            [DebuggerStepThrough]
            get { return this._color; }
            [DebuggerStepThrough]
            set {
                ApplicationInfo.CheckEditor();
                this._color = value;
            }
        }

        #endregion
        #region Computer

        /// <summary>
        /// Gets customized settings for the computer player that controls the <see
        /// cref="FactionClass"/>.</summary>
        /// <value>
        /// A <see cref="CustomComputer"/> object containing customized settings for the computer
        /// player that should control the faction based on the <see cref="FactionClass"/>.</value>
        /// <remarks><para>
        /// <b>Computer</b> never returns a null reference. This property never changes once the
        /// object has been constructed.
        /// </para><para>
        /// Hexkit Game will assign the faction based on the <see cref="FactionClass"/> to a
        /// computer player using the customized settings indicated by <b>Computer</b> if its <see
        /// cref="CustomComputer.IsValid"/> flag is <c>true</c>. Otherwise, Hexkit will assign the
        /// faction to its default human or computer player.</para></remarks>

        public CustomComputer Computer {
            [DebuggerStepThrough]
            get { return this._computer; }
        }

        #endregion
        #region Counters

        /// <summary>
        /// Gets a list of all initial counters of the <see cref="FactionClass"/>.</summary>
        /// <value>
        /// A <see cref="VariableValueDictionary"/> that maps <see cref="VariableClass.Id"/> strings
        /// of <see cref="CounterClass"/> objects to <see cref="Int32"/> values. The default is an
        /// empty collection.</value>
        /// <remarks><para>
        /// <b>Counters</b> never returns a null reference. The collection is read-only if <see
        /// cref="ApplicationInfo.IsEditor"/> is <c>false</c>.
        /// </para><para>
        /// This property holds the values of all "counter" XML elements, with the <see
        /// cref="VariableValueDictionary.Keys"/> holding the values of the "id" attributes, and the
        /// <see cref="VariableValueDictionary.Values"/> holding the corresponding element values.
        /// </para></remarks>

        public VariableValueDictionary Counters {
            [DebuggerStepThrough]
            get {
                return (ApplicationInfo.IsEditor ?
                    this._counters : this._counters.AsReadOnly());
            }
        }

        #endregion
        #region DefeatConditions

        /// <summary>
        /// Gets a list of all defeat conditions for the <see cref="FactionClass"/>.</summary>
        /// <value>
        /// A <see cref="ConditionList"/> containing all defeat conditions that are representable as
        /// <see cref="Condition"/> objects. The default is an empty collection.</value>
        /// <remarks>
        /// <b>DefeatConditions</b> never returns a null reference. All elements are unique. The
        /// collection is read-only if <see cref="ApplicationInfo.IsEditor"/> is <c>false</c>. This
        /// property holds the values of all "defeat" XML elements.</remarks>

        public ConditionList DefeatConditions {
            [DebuggerStepThrough]
            get {
                return (ApplicationInfo.IsEditor ?
                    this._defeatConditions : this._defeatConditions.AsReadOnly());
            }
        }

        #endregion
        #region HomeSite

        /// <summary>
        /// Gets or sets the home site of the <see cref="FactionClass"/>.</summary>
        /// <value>
        /// The map coordinates of the home site of the faction based on the <see
        /// cref="FactionClass"/>. The default is <see cref="PolygonGrid.InvalidLocation"/>.</value>
        /// <exception cref="InvalidOperationException">
        /// The property is set, and <see cref="ApplicationInfo.IsEditor"/> is <c>false</c>.
        /// </exception>
        /// <remarks>
        /// <b>HomeSite</b> maps to the <see cref="AreaSection.HomeSites"/> value that corresponds
        /// to the <see cref="Id"/> string of the <see cref="FactionClass"/>. Negative coordinates
        /// indicate that the faction does not have a <b>HomeSite</b>.</remarks>

        public PointI HomeSite {
            get {
                PointI site;
                if (MasterSection.Instance.Areas.HomeSites.TryGetValue(Id, out site))
                    return site;

                return PolygonGrid.InvalidLocation;
            }
            set {
                ApplicationInfo.CheckEditor();

                if (value.X < 0 || value.Y < 0)
                    MasterSection.Instance.Areas.HomeSites.Remove(Id);
                else
                    MasterSection.Instance.Areas.HomeSites[Id] = value;
            }
        }

        #endregion
        #region Id

        /// <summary>
        /// Gets or sets the identifier of the <see cref="FactionClass"/>.</summary>
        /// <value>
        /// The identifier of the <see cref="FactionClass"/>. The default is an empty string.
        /// </value>
        /// <exception cref="InvalidOperationException">
        /// The property is set, and <see cref="ApplicationInfo.IsEditor"/> is <c>false</c>.
        /// </exception>
        /// <remarks>
        /// <b>Id</b> returns an empty string when set to a null reference. This property holds the
        /// value of the "id" XML attribute which must be unique among all identifiers defined by
        /// the scenario.</remarks>

        public string Id {
            [DebuggerStepThrough]
            get { return this._id ?? ""; }
            [DebuggerStepThrough]
            set {
                ApplicationInfo.CheckEditor();
                if (value != null) value = String.Intern(value);
                this._id = value;
            }
        }

        #endregion
        #region Name

        /// <summary>
        /// Gets or sets the display name of the <see cref="FactionClass"/>.</summary>
        /// <value>
        /// The display name of the <see cref="FactionClass"/>. The default is an empty string.
        /// </value>
        /// <exception cref="InvalidOperationException">
        /// The property is set, and <see cref="ApplicationInfo.IsEditor"/> is <c>false</c>.
        /// </exception>
        /// <remarks>
        /// <b>Name</b> returns an empty string when set to a null reference. This property holds
        /// the value of the "name" XML attribute which is used by Hexkit Game when presenting
        /// scenario data to the player.</remarks>

        public string Name {
            [DebuggerStepThrough]
            get { return this._name ?? ""; }
            [DebuggerStepThrough]
            set {
                ApplicationInfo.CheckEditor();
                if (value != null) value = String.Intern(value);
                this._name = value;
            }
        }

        #endregion
        #region Paragraphs

        /// <summary>
        /// Gets a list of paragraphs with additional information about the <see
        /// cref="FactionClass"/>.</summary>
        /// <value>
        /// A <see cref="ParagraphList"/> containing a sequence of paragraphs with additional
        /// information about the <see cref="FactionClass"/>. The default is an empty collection.
        /// </value>
        /// <remarks><para>
        /// <b>Paragraphs</b> never returns a null reference. The collection is read-only if <see
        /// cref="ApplicationInfo.IsEditor"/> is <c>false</c>.
        /// </para><para>
        /// This property holds the values of all "para" XML elements. Empty XML elements are stored
        /// as empty strings, and should be displayed as blank lines between paragraphs.
        /// </para></remarks>

        public ParagraphList Paragraphs {
            [DebuggerStepThrough]
            get {
                return (ApplicationInfo.IsEditor ?
                    this._paragraphs : this._paragraphs.AsReadOnly());
            }
        }

        #endregion
        #region Resources

        /// <summary>
        /// Gets a list of all initial resources of the <see cref="FactionClass"/>.</summary>
        /// <value>
        /// A <see cref="VariableValueDictionary"/> that maps <see cref="VariableClass.Id"/> strings
        /// of <see cref="ResourceClass"/> objects to <see cref="Int32"/> values. The default is an
        /// empty collection.</value>
        /// <remarks><para>
        /// <b>Resources</b> never returns a null reference. The collection is read-only if <see
        /// cref="ApplicationInfo.IsEditor"/> is <c>false</c>.
        /// </para><para>
        /// This property holds the values of all "resource" XML elements, with the <see
        /// cref="VariableValueDictionary.Keys"/> holding the values of the "id" attributes, and the
        /// <see cref="VariableValueDictionary.Values"/> holding the corresponding element values.
        /// </para></remarks>

        public VariableValueDictionary Resources {
            [DebuggerStepThrough]
            get {
                return (ApplicationInfo.IsEditor ?
                    this._resources : this._resources.AsReadOnly());
            }
        }

        #endregion
        #region ResourceModifiers

        /// <summary>
        /// Gets a list of all resource modifiers of the <see cref="FactionClass"/>.</summary>
        /// <value>
        /// A <see cref="VariableValueDictionary"/> that maps <see cref="VariableClass.Id"/> strings
        /// of <see cref="ResourceClass"/> objects to <see cref="Int32"/> values. The default is an
        /// empty collection.</value>
        /// <remarks><para>
        /// <b>ResourceModifiers</b> never returns a null reference. The collection is read-only if
        /// <see cref="ApplicationInfo.IsEditor"/> is <c>false</c>.
        /// </para><para>
        /// This property holds the values of all "resourceModifier" XML elements, with the <see
        /// cref="VariableValueDictionary.Keys"/> holding the values of the "id" attributes, and the
        /// <see cref="VariableValueDictionary.Values"/> holding the corresponding element values.
        /// </para></remarks>

        public VariableValueDictionary ResourceModifiers {
            [DebuggerStepThrough]
            get {
                return (ApplicationInfo.IsEditor ?
                    this._resourceModifiers : this._resourceModifiers.AsReadOnly());
            }
        }

        #endregion
        #region SupplyResources

        /// <summary>
        /// Gets a list of resource identifiers indicating all unit resources that can be
        /// resupplied.</summary>
        /// <value>
        /// A <see cref="IdentifierList"/> containing <see cref="VariableClass.Id"/> strings of <see
        /// cref="ResourceClass"/> objects which are used by any units available to this faction,
        /// and which can be resupplied while a unit is placed on the map. The default is an empty
        /// collection.</value>
        /// <remarks><para>
        /// <b>SupplyResources</b> never returns a null reference. All elements are unique. The
        /// collection is read-only if <see cref="ApplicationInfo.IsEditor"/> is <c>false</c>.
        /// </para><para>
        /// This property holds the values of the "resources" attributes of all "supplyResources"
        /// XML elements.</para></remarks>

        public IdentifierList SupplyResources {
            [DebuggerStepThrough]
            get {
                return (ApplicationInfo.IsEditor ?
                    this._supplyResources : this._supplyResources.AsReadOnly());
            }
        }

        #endregion
        #region Terrains

        /// <summary>
        /// Gets a list of all unplaced terrains that belong to the <see cref="FactionClass"/>.
        /// </summary>
        /// <value>
        /// A <see cref="EntityTemplateList"/> whose elements are of the <see
        /// cref="EntityCategory.Terrain"/> category. The default is an empty collection.</value>
        /// <remarks><para>
        /// <b>Terrains</b> never returns a null reference. The collection is read-only if <see
        /// cref="ApplicationInfo.IsEditor"/> is <c>false</c>. This property holds the values of
        /// all "terrain" XML elements.
        /// </para><para>
        /// <b>Terrains</b> defines the <em>unplaced</em> terrains initially owned by the faction
        /// represented by the <see cref="FactionClass"/>. The faction may also own <em>placed</em>
        /// terrains, as defined by the <see cref="AreaSection"/>.</para></remarks>

        public EntityTemplateList Terrains {
            [DebuggerStepThrough]
            get {
                return (ApplicationInfo.IsEditor ?
                    this._terrains : this._terrains.AsReadOnly());
            }
        }

        #endregion
        #region Units

        /// <summary>
        /// Gets a list of all unplaced units that belong to the <see cref="FactionClass"/>.
        /// </summary>
        /// <value>
        /// A <see cref="EntityTemplateList"/> whose elements are of the <see
        /// cref="EntityCategory.Unit"/> category. The default is an empty collection.</value>
        /// <remarks><para>
        /// <b>Units</b> never returns a null reference. The collection is read-only if <see
        /// cref="ApplicationInfo.IsEditor"/> is <c>false</c>. This property holds the values of
        /// all "unit" XML elements.
        /// </para><para>
        /// <b>Units</b> defines the <em>unplaced</em> units initially owned by the faction
        /// represented by the <see cref="FactionClass"/>. The faction may also own <em>placed</em>
        /// units, as defined by the <see cref="AreaSection"/>.</para></remarks>

        public EntityTemplateList Units {
            [DebuggerStepThrough]
            get {
                return (ApplicationInfo.IsEditor ?
                    this._units : this._units.AsReadOnly());
            }
        }

        #endregion
        #region Upgrades

        /// <summary>
        /// Gets a list of all upgrades owned by the <see cref="FactionClass"/>.</summary>
        /// <value>
        /// A <see cref="EntityTemplateList"/> whose elements are of the <see
        /// cref="EntityCategory.Upgrade"/> category. The default is an empty collection.</value>
        /// <remarks><para>
        /// <b>Upgrades</b> never returns a null reference. The collection is read-only if <see
        /// cref="ApplicationInfo.IsEditor"/> is <c>false</c>. This property holds the values of all
        /// "upgrade" XML elements.
        /// </para><para>
        /// <b>Upgrades</b> defines the upgrades initially owned by the faction represented by the
        /// <see cref="FactionClass"/>. Upgrades are always unplaced, so the <see
        /// cref="AreaSection"/> never defines any additional upgrades.</para></remarks>

        public EntityTemplateList Upgrades {
            [DebuggerStepThrough]
            get {
                return (ApplicationInfo.IsEditor ?
                    this._upgrades : this._upgrades.AsReadOnly());
            }
        }

        #endregion
        #region VictoryConditions

        /// <summary>
        /// Gets a list of all victory conditions for the <see cref="FactionClass"/>.</summary>
        /// <value>
        /// A <see cref="ConditionList"/> containing all victory conditions that are representable
        /// as <see cref="Condition"/> objects. The default is an empty collection.</value>
        /// <remarks>
        /// <b>VictoryConditions</b> never returns a null reference. All elements are unique. The
        /// collection is read-only if <see cref="ApplicationInfo.IsEditor"/> is <c>false</c>. This
        /// property holds the values of all "victory" XML elements.</remarks>

        public ConditionList VictoryConditions {
            [DebuggerStepThrough]
            get {
                return (ApplicationInfo.IsEditor ?
                    this._victoryConditions : this._victoryConditions.AsReadOnly());
            }
        }

        #endregion
        #endregion
        #region ToString

        /// <summary>
        /// Returns a <see cref="String"/> that represents the <see cref="FactionClass"/>.</summary>
        /// <returns>
        /// The value of the <see cref="Name"/> property, if it is not an empty string; otherwise,
        /// the value of the <see cref="Id"/> property.</returns>

        public override string ToString() {
            return (Name.Length == 0 ? Id : Name);
        }

        #endregion
        #region ICloneable Members

        /// <summary>
        /// Creates a new <see cref="FactionClass"/> object that is a deep copy of the current
        /// instance.</summary>
        /// <returns>
        /// A new <see cref="FactionClass"/> object that is a deep copy of the current instance.
        /// </returns>
        /// <remarks>
        /// <b>Clone</b> calls the <see cref="FactionClass(FactionClass)"/> copy constructor with
        /// this <see cref="FactionClass"/> object.</remarks>

        public object Clone() {
            return new FactionClass(this);
        }

        #endregion
        #region IMutableKeyedValue Members

        /// <summary>
        /// Gets the identifier of the <see cref="FactionClass"/>.</summary>
        /// <value>
        /// The value of the <see cref="Id"/> property.</value>

        string IKeyedValue<String>.Key {
            [DebuggerStepThrough]
            get { return Id; }
        }

        /// <summary>
        /// Sets the identifier of the <see cref="FactionClass"/>.</summary>
        /// <param name="key">
        /// The new value for the <see cref="Id"/> property.</param>
        /// <exception cref="InvalidOperationException">
        /// <see cref="ApplicationInfo.IsEditor"/> is <c>false</c>.</exception>

        void IMutableKeyedValue<String>.SetKey(string key) {
            Id = key;
        }

        #endregion
        #region ScenarioElement Members
        #region ProcessIdentifier

        /// <summary>
        /// Counts, changes, or deletes all occurrences of the specified identifier in the <see
        /// cref="FactionClass"/>.</summary>
        /// <param name="oldId">
        /// The identifier to count, change, or delete.</param>
        /// <param name="newId"><para>
        /// The same value as <paramref name="oldId"/> to count the occurrences of <paramref
        /// name="oldId"/>.
        /// </para><para>-or-</para><para>
        /// A different value than <paramref name="oldId"/> to change all occurrences of <paramref
        /// name="oldId"/> to <paramref name="newId"/>.
        /// </para><para>-or-</para><para>
        /// A null reference to delete all elements with <paramref name="oldId"/>.</para></param>
        /// <returns>
        /// The number of occurrences of <paramref name="oldId"/> in the <see cref="FactionClass"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="oldId"/> is a null reference.</exception>
        /// <remarks><para>
        /// <b>ProcessIdentifier</b> processes <see cref="FactionClass"/> properties as follows:
        /// </para><list type="table"><listheader>
        /// <term>Property</term><description>Processing</description>
        /// </listheader><item>
        /// <term><see cref="BuildableTerrains"/><br/> <see cref="BuildableUnits"/><br/> <see
        /// cref="BuildableUpgrades"/><br/> <see cref="Counters"/><br/> <see cref="Resources"/><br/>
        /// <see cref="ResourceModifiers"/></term>
        /// <description>By key</description>
        /// </item><item>
        /// <term><see cref="Terrains"/><br/> <see cref="Units"/><br/> <see cref="Upgrades"/></term>
        /// <description>By value</description>
        /// </item></list></remarks>

        internal override int ProcessIdentifier(string oldId, string newId) {
            int count = 0;

            // process ID in variable tables
            count += CollectionsUtility.ProcessKey(this._counters, oldId, newId);
            count += CollectionsUtility.ProcessKey(this._resources, oldId, newId);
            count += CollectionsUtility.ProcessKey(this._resourceModifiers, oldId, newId);

            // process IDs in entity class tables
            count += CollectionsUtility.ProcessKey(this._buildableUnits, oldId, newId);
            count += CollectionsUtility.ProcessKey(this._buildableTerrains, oldId, newId);
            count += CollectionsUtility.ProcessKey(this._buildableUpgrades, oldId, newId);

            // process IDs in entity template lists
            foreach (EntityTemplate entity in Units)
                count += entity.ProcessIdentifier(oldId, newId);

            foreach (EntityTemplate entity in Terrains)
                count += entity.ProcessIdentifier(oldId, newId);

            foreach (EntityTemplate entity in Upgrades)
                count += entity.ProcessIdentifier(oldId, newId);

            return count;
        }

        #endregion
        #region Validate

        /// <summary>
        /// Validates the current data of the <see cref="FactionClass"/>.</summary>
        /// <exception cref="XmlException">
        /// Validation failed, and <see cref="ApplicationInfo.IsEditor"/> is <c>false</c>.
        /// </exception>
        /// <remarks><para>
        /// <b>Validate</b> processes <see cref="FactionClass"/> properties as follows:
        /// </para><list type="table"><listheader>
        /// <term>Property</term><description>Processing</description>
        /// </listheader><item>
        /// <term><see cref="BuildableTerrains"/><br/> <see cref="BuildableUnits"/><br/> <see
        /// cref="BuildableUpgrades"/></term>
        /// <description>Check identifiers and set references</description>
        /// </item><item>
        /// <term><see cref="Counters"/><br/> <see cref="Resources"/><br/> <see
        /// cref="ResourceModifiers"/><br/> <see cref="Terrains"/><br/> <see cref="Units"/><br/>
        /// <see cref="Upgrades"/></term>
        /// <description>Check identifiers</description>
        /// </item></list><para>
        /// Checks are only performed if <see cref="ApplicationInfo.IsEditor"/> is <c>false</c>.
        /// </para></remarks>

        internal override void Validate() {

            // check variable identifiers if not editing
            if (!ApplicationInfo.IsEditor) {
                VariableSection variables = MasterSection.Instance.Variables;
                variables.ValidateCollection(Counters, VariableCategory.Counter, "Counter");
                variables.ValidateCollection(Resources, VariableCategory.Resource, "Resource");
                variables.ValidateCollection(ResourceModifiers, VariableCategory.Resource, "ResourceModifier");
            }

            // set references for IDs in entity class tables
            EntitySection entities = MasterSection.Instance.Entities;
            entities.ValidateCollection(this._buildableUnits, EntityCategory.Unit, "BuildableUnits");
            entities.ValidateCollection(this._buildableTerrains, EntityCategory.Terrain, "BuildableTerrains");
            entities.ValidateCollection(this._buildableUpgrades, EntityCategory.Upgrade, "BuildableUpgrades");

            // validate IDs of entity templates if not editing
            if (!ApplicationInfo.IsEditor) {
                entities.ValidateTemplates(Units);
                entities.ValidateTemplates(Terrains);
                entities.ValidateTemplates(Upgrades);
            }
        }

        #endregion
        #endregion
        #region XmlSerializable Members
        #region ConstXmlName

        /// <summary>
        /// The name of the XML element associated with the <see cref="FactionClass"/> class.
        /// </summary>
        /// <remarks>
        /// <b>ConstXmlName</b> holds the value "factionClass", indicating the XML element in <see
        /// cref="FilePaths.ScenarioSchema"/> whose data is managed by the <see
        /// cref="FactionClass"/> class.</remarks>

        public const string ConstXmlName = "factionClass";

        #endregion
        #region ReadXmlAttributes

        /// <summary>
        /// Reads XML attribute data into the <see cref="FactionClass"/> object using the specified
        /// <see cref="XmlReader"/>.</summary>
        /// <param name="reader">
        /// The <see cref="XmlReader"/> from which to read.</param>
        /// <exception cref="XmlException">
        /// An error occurred while parsing the XML data provided by <paramref name="reader"/>.
        /// </exception>
        /// <exception cref="System.Xml.Schema.XmlSchemaException">
        /// <paramref name="reader"/> is an <see cref="XmlValidatingReader"/>, and the XML data did
        /// not conform to <see cref="FilePaths.ScenarioSchema"/>.</exception>
        /// <remarks>
        /// Please refer to class <see cref="XmlSerializable"/> for a complete description of this
        /// method.</remarks>

        protected override void ReadXmlAttributes(XmlReader reader) {

            XmlUtility.ReadAttributeAsString(reader, "id", ref this._id);
            XmlUtility.ReadAttributeAsString(reader, "name", ref this._name);
        }

        #endregion
        #region ReadXmlElements

        /// <summary>
        /// Reads XML element data into the <see cref="FactionClass"/> object using the specified
        /// <see cref="XmlReader"/>.</summary>
        /// <param name="reader">
        /// The <see cref="XmlReader"/> from which to read.</param>
        /// <returns>
        /// <c>true</c> if the current node of the specified <paramref name="reader"/> contained any
        /// matching data; otherwise, <c>false</c>.</returns>
        /// <exception cref="XmlException">
        /// An error occurred while parsing the XML data provided by <paramref name="reader"/>.
        /// </exception>
        /// <exception cref="System.Xml.Schema.XmlSchemaException">
        /// <paramref name="reader"/> is an <see cref="XmlValidatingReader"/>, and the XML data did
        /// not conform to <see cref="FilePaths.ScenarioSchema"/>.</exception>
        /// <remarks>
        /// Please refer to class <see cref="XmlSerializable"/> for a complete description of this
        /// method.</remarks>

        protected override bool ReadXmlElements(XmlReader reader) {

            EntityTemplate entity = null;
            ConditionList conditions = null;
            EntityClassDictionary classes = null;

            switch (reader.Name) {

                case "buildableUnits":
                    classes = this._buildableUnits;
                    goto case "anyClass";

                case "buildableTerrains":
                    classes = this._buildableTerrains;
                    goto case "anyClass";

                case "buildableUpgrades":
                    classes = this._buildableUpgrades;
                    goto case "anyClass";

                case "anyClass": {
                    string idRefs = reader["ids"];
                    if (idRefs != null) {

                        // add IDs with null references to class list
                        foreach (string token in idRefs.Split(null))
                            classes.Add(String.Intern(token), null);
                    }
                    return true;
                }

                case "color":
                    this._color = SimpleXml.ReadColor(reader);
                    return true;

                case "computer":
                    this._computer.ReadXml(reader);
                    return true;

                case "counter":
                    VariableClass.ReadXmlValue(reader, this._counters);
                    return true;

                case "resource":
                    VariableClass.ReadXmlValue(reader, this._resources);
                    return true;

                case "resourceModifier":
                    VariableClass.ReadXmlValue(reader, this._resourceModifiers);
                    return true;

                case "supplyResources": {
                    string idRefs = reader["ids"];
                    if (idRefs != null) {

                        // add resource identifiers to supply list
                        foreach (string token in idRefs.Split(null))
                            this._supplyResources.Add(String.Intern(token));
                    }
                    return true;
                }

                case UnitClass.ConstXmlName:
                    entity = new EntityTemplate(EntityCategory.Unit);
                    entity.ReadXml(reader);
                    this._units.Add(entity);
                    return true;

                case TerrainClass.ConstXmlName:
                    entity = new EntityTemplate(EntityCategory.Terrain);
                    entity.ReadXml(reader);
                    this._terrains.Add(entity);
                    return true;

                case UpgradeClass.ConstXmlName:
                    entity = new EntityTemplate(EntityCategory.Upgrade);
                    entity.ReadXml(reader);
                    this._upgrades.Add(entity);
                    return true;

                case "victory":
                    conditions = this._victoryConditions;
                    goto case "anyCondition";

                case "defeat":
                    conditions = this._defeatConditions;
                    goto case "anyCondition";

                case "anyCondition": {
                    Condition condition = new Condition();
                    condition.ReadXml(reader);

                    // add condition only if not present
                    if (!conditions.Contains(condition))
                        conditions.Add(condition);

                    return true;
                }

                case "para": {
                    string element = reader.ReadString();
                    this._paragraphs.Add(element.PackSpace());
                    return true;
                }

                default: return false;
            }
        }

        #endregion
        #region WriteXmlAttributes

        /// <summary>
        /// Writes all current data of the <see cref="FactionClass"/> object that is serialized to
        /// XML attributes to the specified <see cref="XmlWriter"/>.</summary>
        /// <param name="writer">
        /// The <see cref="XmlWriter"/> to which to write.</param>
        /// <remarks>
        /// Please refer to class <see cref="XmlSerializable"/> for a complete description of this
        /// method.</remarks>

        protected override void WriteXmlAttributes(XmlWriter writer) {

            writer.WriteAttributeString("id", Id);
            writer.WriteAttributeString("name", Name);
        }

        #endregion
        #region WriteXmlElements

        /// <summary>
        /// Writes all current data of the <see cref="FactionClass"/> object that is serialized to
        /// nested XML elements to the specified <see cref="XmlWriter"/>.</summary>
        /// <param name="writer">
        /// The <see cref="XmlWriter"/> to which to write.</param>
        /// <remarks>
        /// Please refer to class <see cref="XmlSerializable"/> for a complete description of this
        /// method.</remarks>

        protected override void WriteXmlElements(XmlWriter writer) {

            writer.WriteStartElement("color");
            SimpleXml.WriteColor(writer, Color);
            writer.WriteEndElement();

            if (Computer.IsValid)
                Computer.WriteXml(writer);

            VariableClass.WriteXmlValues(writer, "counter", Counters);
            VariableClass.WriteXmlValues(writer, "resource", Resources);
            VariableClass.WriteXmlValues(writer, "resourceModifier", ResourceModifiers);

            if (SupplyResources.Count > 0) {
                writer.WriteStartElement("supplyResources");
                writer.WriteAttributeString("ids", String.Join(" ", SupplyResources));
                writer.WriteEndElement();
            }

            if (BuildableUnits.Count > 0) {
                writer.WriteStartElement("buildableUnits");
                writer.WriteAttributeString("ids", String.Join(" ", BuildableUnits.Keys));
                writer.WriteEndElement();
            }

            if (BuildableTerrains.Count > 0) {
                writer.WriteStartElement("buildableTerrains");
                writer.WriteAttributeString("ids", String.Join(" ", BuildableTerrains.Keys));
                writer.WriteEndElement();
            }

            if (BuildableUpgrades.Count > 0) {
                writer.WriteStartElement("buildableUpgrades");
                writer.WriteAttributeString("ids", String.Join(" ", BuildableUpgrades.Keys));
                writer.WriteEndElement();
            }

            foreach (EntityTemplate entity in Units)
                entity.WriteXml(writer);

            foreach (EntityTemplate entity in Terrains)
                entity.WriteXml(writer);

            foreach (EntityTemplate entity in Upgrades)
                entity.WriteXml(writer);

            foreach (Condition condition in VictoryConditions)
                condition.WriteXml(writer, "victory");

            foreach (Condition condition in DefeatConditions)
                condition.WriteXml(writer, "defeat");

            Information.WriteXmlParagraphs(writer, Paragraphs);
        }

        #endregion
        #endregion
    }
}
