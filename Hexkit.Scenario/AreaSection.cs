using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml;

using Tektosyne;
using Tektosyne.Collections;
using Tektosyne.Geometry;
using Tektosyne.Graph;
using Tektosyne.Xml;
using Hexkit.Global;

namespace Hexkit.Scenario {
    #region Type Aliases

    using AreaList = ListEx<Area>;
    using EntityTemplateList = ListEx<EntityTemplate>;
    using PointIDictionary = SortedListEx<String, PointI>;
    using PointIList = ListEx<PointI>;

    #endregion

    /// <summary>
    /// Represents the Areas section of a scenario.</summary>
    /// <remarks>
    /// <b>AreaSection</b> is serialized to the XML element "areas" defined in <see
    /// cref="FilePaths.ScenarioSchema"/>.</remarks>

    public sealed class AreaSection: ScenarioElement {
        #region AreaSection()

        /// <overloads>
        /// Initializes a new instance of the <see cref="AreaSection"/> class.</overloads>
        /// <summary>
        /// Initializes a new instance of the <see cref="AreaSection"/> class with default
        /// properties.</summary>

        internal AreaSection() {

            this._mapGrid = new PolygonGrid(DefaultPolygon);
            this._overlay = new OverlayImage(1.0);
            this._terrains = new EntityTemplateList();
            this._homeSites = new PointIDictionary();
            this._allPlaceSites = new SortedList<String, PointIList>();
        }

        #endregion
        #region AreaSection(AreaSection)

        /// <summary>
        /// Initializes a new instance of the <see cref="AreaSection"/> class with the properties of
        /// an existing instance.</summary>
        /// <param name="areas">
        /// Another instance of <see cref="AreaSection"/> whose property values, except for <see
        /// cref="Collection"/>, are shared with the new instance.</param>
        /// <remarks><para>
        /// This constructor creates a new <see cref="AreaSection"/> object whose properties, except
        /// for <see cref="Collection"/>, reference the same data as the specified <paramref
        /// name="areas"/> object.
        /// </para><para>
        /// Subsequent data changes in the newly created instance will be reflected in the data of
        /// the specified <paramref name="areas"/> object, and vice versa.
        /// </para><para>
        /// The <see cref="Collection"/> property is initialized to an empty collection, however.
        /// The client might subsequently call <see cref="PackAreas"/> to add elements from another
        /// source.</para></remarks>

        public AreaSection(AreaSection areas) {

            this._mapGrid = areas._mapGrid;
            this._overlay = areas._overlay;
            this._terrains = areas._terrains;
            this._homeSites = areas._homeSites;
            this._allPlaceSites = areas._allPlaceSites;
        }

        #endregion
        #region Private Fields

        // property backers
        private PolygonGrid _mapGrid;
        private AStar<PointI> _aStar;
        private Coverage<PointI> _coverage;
        private FloodFill<PointI> _floodFill;
        private Visibility<PointI> _visibility;

        private readonly AreaList _collection = new AreaList();
        private readonly OverlayImage _overlay;
        private readonly PointIDictionary _homeSites;
        private readonly SortedList<String, PointIList> _allPlaceSites;
        private readonly EntityTemplateList _terrains;

        #endregion
        #region Public Fields
        #region DefaultPolygon

        /// <summary>
        /// The default value for the <see cref="PolygonGrid.Element"/> property of the <see
        /// cref="MapGrid"/>.</summary>
        /// <remarks>
        /// <b>DefaultPolygon</b> holds a <see cref="RegularPolygon"/> with six sides, a side length
        /// of <see cref="MinPolygonLength"/>, and <see cref="PolygonOrientation.OnVertex"/>
        /// orientation.</remarks>

        public static readonly RegularPolygon DefaultPolygon =
            new RegularPolygon(MinPolygonLength, 6, PolygonOrientation.OnVertex);

        #endregion
        #region MaxPolygonLength

        /// <summary>
        /// The maximum unscaled side length of a <see cref="MapGrid"/> element.</summary>
        /// <remarks><para>
        /// This value must equal the "maxInclusive" property of the simple XML type "polygonLength"
        /// defined in <see cref="FilePaths.ScenarioSchema"/>.
        /// </para><para>
        /// <b>MaxPolygonLength</b> is the upper limit for the <see cref="RegularPolygon.Length"/>
        /// value of any <see cref="PolygonGrid.Element"/> of the <see cref="MapGrid"/>, measured in
        /// screen pixels at the default zoom level.</para></remarks>

        public const float MaxPolygonLength = 100f;

        #endregion
        #region MinPolygonLength

        /// <summary>
        /// The minimum unscaled side length of a <see cref="MapGrid"/> element.</summary>
        /// <remarks><para>
        /// This value must equal the "minInclusive" property of the simple XML type "polygonLength"
        /// defined in <see cref="FilePaths.ScenarioSchema"/>.
        /// </para><para>
        /// <b>MinPolygonLength</b> is the lower limit for the <see cref="RegularPolygon.Length"/>
        /// value of any <see cref="PolygonGrid.Element"/> of the <see cref="MapGrid"/>, measured in
        /// screen pixels at the default zoom level.</para></remarks>

        public const float MinPolygonLength = 10f;

        #endregion
        #endregion
        #region AllPlaceSites

        /// <summary>
        /// Gets a list of all placement sites for entity classes.</summary>
        /// <value>
        /// A <see cref="SortedList{String, PointIList}"/> that maps <see cref="EntityClass.Id"/>
        /// strings of <see cref="EntityClass"/> objects to <see cref="PointIList"/> collections,
        /// containing the coordinates of all valid placement sites. The default is an empty
        /// collection.</value>
        /// <remarks><para>
        /// <b>AllPlaceSites</b> never returns a null reference, and its keys are never empty
        /// strings. This property holds the values of all "placeSite" XML elements nested into any
        /// "entitySites" XML elements.
        /// </para><para>
        /// External clients should use the <see cref="EntityClass.PlaceSites"/> property of the
        /// desired <see cref="EntityClass"/> to access the corresponding value of the
        /// <b>AllPlaceSites</b> collection.</para></remarks>

        internal SortedList<String, PointIList> AllPlaceSites {
            [DebuggerStepThrough]
            get { return this._allPlaceSites; }
        }

        #endregion
        #region AStar

        /// <summary>
        /// Gets an A* pathfinding algorithm for the <see cref="MapGrid"/>.</summary>
        /// <value>
        /// An <see cref="Tektosyne.Graph.AStar{T}"/> algorithm that is based on the geometric
        /// structure of the current <see cref="MapGrid"/>.</value>
        /// <remarks>
        /// <b>AStar</b> never returns a null reference. This property is automatically updated to
        /// reflect the current <see cref="MapGrid"/>.</remarks>

        public AStar<PointI> AStar {
            [DebuggerStepThrough]
            get {
                if (this._aStar == null || this._aStar.Graph != MapGrid)
                    this._aStar = new AStar<PointI>(MapGrid);

                return this._aStar;
            }
        }

        #endregion
        #region Collection

        /// <summary>
        /// Gets a list of all map areas defined in the <see cref="AreaSection"/>.</summary>
        /// <value>
        /// An <see cref="AreaList"/> containing all defined <see cref="Area"/> objects. The default
        /// is an empty collection.</value>
        /// <remarks><para>
        /// <b>Collection</b> never returns a null reference, and its elements are never null
        /// references. The collection is read-only if <see cref="ApplicationInfo.IsEditor"/> is
        /// <c>false</c>.
        /// </para><para>
        /// The <see cref="Area.Bounds"/> specified by different <see cref="Area"/> objects may
        /// overlap, in which case their data accumulates on the intersecting map sites.
        /// Incompatible data will generate an exception when Hexkit Game initializes a new world
        /// state.</para></remarks>

        public AreaList Collection {
            [DebuggerStepThrough]
            get {
                return (ApplicationInfo.IsEditor ?
                    this._collection : this._collection.AsReadOnly());
            }
        }

        #endregion
        #region Coverage

        /// <summary>
        /// Gets a path coverage algorithm for the <see cref="MapGrid"/>.</summary>
        /// <value>
        /// A <see cref="Tektosyne.Graph.Coverage{T}"/> algorithm that is based on the geometric
        /// structure of the current <see cref="MapGrid"/>.</value>
        /// <remarks>
        /// <b>Coverage</b> never returns a null reference. This property is automatically updated
        /// to reflect the current <see cref="MapGrid"/>.</remarks>

        public Coverage<PointI> Coverage {
            [DebuggerStepThrough]
            get {
                if (this._coverage == null || this._coverage.Graph != MapGrid)
                    this._coverage = new Coverage<PointI>(MapGrid);

                return this._coverage;
            }
        }

        #endregion
        #region FloodFill

        /// <summary>
        /// Gets a flood fill algorithm for the <see cref="MapGrid"/>.</summary>
        /// <value>
        /// A <see cref="Tektosyne.Graph.FloodFill{T}"/> algorithm that is based on the geometric
        /// structure of the current <see cref="MapGrid"/>.</value>
        /// <remarks>
        /// <b>FloodFill</b> never returns a null reference. This property is automatically updated
        /// to reflect the current <see cref="MapGrid"/>.</remarks>

        public FloodFill<PointI> FloodFill {
            [DebuggerStepThrough]
            get {
                if (this._floodFill == null || this._floodFill.Graph != MapGrid)
                    this._floodFill = new FloodFill<PointI>(MapGrid);

                return this._floodFill;
            }
        }

        #endregion
        #region HomeSites

        /// <summary>
        /// Gets a list of all home sites for factions.</summary>
        /// <value>
        /// A <see cref="PointIDictionary"/> that maps <see cref="FactionClass.Id"/> strings of <see
        /// cref="FactionClass"/> objects to <see cref="PointI"/> values, indicating the coordinates
        /// of each home site. The default is an empty collection.</value>
        /// <remarks><para>
        /// <b>HomeSites</b> never returns a null reference, and its keys are never empty strings.
        /// This property holds the values of all "homeSite" XML elements nested into any
        /// "factionSites" XML elements.
        /// </para><para>
        /// External clients should use the <see cref="FactionClass.HomeSite"/> property of the
        /// desired <see cref="FactionClass"/> to access the corresponding value of the
        /// <b>HomeSites</b> collection.</para></remarks>

        internal PointIDictionary HomeSites {
            [DebuggerStepThrough]
            get { return this._homeSites; }
        }

        #endregion
        #region MapGrid

        /// <summary>
        /// Gets or sets the <see cref="PolygonGrid"/> that describes the game map.</summary>
        /// <value>
        /// The read-only <see cref="PolygonGrid"/> that describes the geometric structure of the
        /// game map. The default grid contains a single <see cref="DefaultPolygon"/>.</value>
        /// <exception cref="ArgumentNullException">
        /// The property is set to a null reference.</exception>
        /// <exception cref="InvalidOperationException">
        /// The property is set, and <see cref="ApplicationInfo.IsEditor"/> is <c>false</c>.
        /// </exception>
        /// <remarks>
        /// <b>MapGrid</b> holds the value of the "mapGrid" XML element. Its <see
        /// cref="PolygonGrid.Size"/> restricts all coordinates specified by the <see
        /// cref="Area.Bounds"/> elements of any <see cref="Area"/>.</remarks>

        public PolygonGrid MapGrid {
            [DebuggerStepThrough]
            get { return this._mapGrid.AsReadOnly(); }
            [DebuggerStepThrough]
            set {
                if (value == null)
                    ThrowHelper.ThrowArgumentNullException("value");

                ApplicationInfo.CheckEditor();
                this._mapGrid = value;
            }
        }

        #endregion
        #region Overlay

        /// <summary>
        /// Gets the <see cref="OverlayImage"/> that appears below the game map.</summary>
        /// <value>
        /// The <see cref="OverlayImage"/> that appears below the game map, both within Hexkit Game
        /// and Hexkit Editor.</value>
        /// <remarks>
        /// <b>Overlay</b> always returns the same valid reference. The <see
        /// cref="OverlayImage.Opacity"/> of the <see cref="OverlayImage"/> defaults to one.
        /// </remarks>

        public OverlayImage Overlay {
            [DebuggerStepThrough]
            get { return this._overlay; }
        }

        #endregion
        #region Terrains

        /// <summary>
        /// Gets the default terrain stack for the game map.</summary>
        /// <value>
        /// A <see cref="EntityTemplateList"/> whose elements are of the <see
        /// cref="EntityCategory.Terrain"/> category. The default is an empty collection.</value>
        /// <remarks><para>
        /// <b>Terrains</b> never returns a null reference. The collection is read-only if <see
        /// cref="ApplicationInfo.IsEditor"/> is <c>false</c>. This property holds the values of all
        /// "terrain" XML elements.
        /// </para><para>
        /// <b>Terrains</b> defines the default terrain stack for all map sites that are
        /// <em>not</em> covered by a <see cref="Collection"/> element specifying a different
        /// terrain stack.</para></remarks>

        public EntityTemplateList Terrains {
            [DebuggerStepThrough]
            get {
                return (ApplicationInfo.IsEditor ?
                    this._terrains : this._terrains.AsReadOnly());
            }
        }

        #endregion
        #region Visibility

        /// <summary>
        /// Gets a line-of-sight algorithm for the <see cref="MapGrid"/>.</summary>
        /// <value>
        /// A <see cref="Tektosyne.Graph.Visibility{T}"/> algorithm that is based on the geometric
        /// structure of the current <see cref="MapGrid"/>.</value>
        /// <remarks>
        /// <b>Visibility</b> never returns a null reference. This property is automatically updated
        /// to reflect the current <see cref="MapGrid"/>.</remarks>

        public Visibility<PointI> Visibility {
            [DebuggerStepThrough]
            get {
                if (this._visibility == null || this._visibility.Graph != MapGrid)
                    this._visibility = new Visibility<PointI>(MapGrid);

                return this._visibility;
            }
        }

        #endregion
        #region PackAreas

        /// <summary>
        /// Adds a packed transformation of the specified two-dimensional <see cref="Area"/> array
        /// to the <see cref="Collection"/>.</summary>
        /// <param name="array">
        /// A two-dimensional <see cref="Array"/> of <see cref="Area"/> objects whose data should be
        /// added to the <see cref="Collection"/> of the <see cref="AreaSection"/>.</param>
        /// <exception cref="ArgumentException">
        /// The dimensions of the specified <paramref name="array"/> differ from those of the
        /// current <see cref="MapGrid"/>.</exception>
        /// <remarks><para>
        /// Each <paramref name="array"/> element is assumed to hold the data of a single map site,
        /// implying a single <see cref="Area.Bounds"/> element whose location equals the index
        /// position of the element, and whose size is (1,1).
        /// </para><para>
        /// Multiple adjacent <paramref name="array"/> elements with identical contents are packed
        /// into a single <b>Collection</b> element with adequately increased <b>Bounds</b>. Other
        /// <paramref name="array"/> elements, or groups of adjacent elements, with the same
        /// contents as an existing packed <b>Collection</b> element, are stored by merely adding a
        /// new <b>Bounds</b> entry to the existing <b>Collection</b> element. The effect is an
        /// extremely compact representation of contiguous map areas with identical contents.
        /// </para><para>
        /// <b>PackAreas</b> does not check terrain stacks against the default terrain stack
        /// specified by the <see cref="Terrains"/> property. Clients should avoid adding such
        /// terrain stacks to the specified <paramref name="array"/> in the first place.
        /// </para><para>
        /// Moreover, <b>PackAreas</b> does not create copies of the supplied <see cref="Area"/>
        /// objects. They are directly manipulated and/or added to the <b>Collection</b> property as
        /// needed. Clients should not attempt to read or change any <paramref name="array"/>
        /// elements after <b>PackAreas</b> has returned.</para></remarks>

        public void PackAreas(Area[,] array) {

            // retrieve and check map dimensions
            int width = array.GetLength(0), height = array.GetLength(1);
            if (width != MapGrid.Size.Width || height != MapGrid.Size.Height)
                ThrowHelper.ThrowArgumentExceptionWithFormat(
                    "array", Tektosyne.Strings.ArgumentPropertyConflict, "MapGrid");

            // iterate over single-site areas
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++) {
                    Area area = array[x,y];

                    // skip empty areas
                    if (area.IsEmpty) continue;

                    // construct rectangle with identical terrains
                    int right, bottom;

                    // extend block as far to the right as possible
                    for (right = x + 1; right < width; right++) {
                        Area next = array[right, y];

                        // clear equal areas, abort if one isn't
                        if (!area.Equals(next)) break;
                        next.Clear();
                    }

                    // extend block as far to the bottom as possible
                    for (bottom = y + 1; bottom < height; bottom++) {

                        // accept row only if all areas are equal
                        for (int dx = x; dx < right; dx++)
                            if (!area.Equals(array[dx, bottom]))
                                goto packed;

                        // clear current row and continue
                        for (int dx = x; dx < right; dx++)
                            array[dx, bottom].Clear();
                    }

                packed:
                    // adjust bounds to packing rectangle
                    area.Bounds[0] = new RectI(x, y, right-x, bottom-y);

                    // look for existing area with same entities
                    for (int i = 0; i < this._collection.Count; i++) {
                        Area existing = this._collection[i];

                        // add new bounds to existing area
                        if (area.Equals(existing)) {
                            existing.Bounds.Add(area.Bounds[0]);
                            goto finished;
                        }
                    }

                    // not found, create new area
                    this._collection.Add(area);

                finished:
                    continue;
                }
        }

        #endregion
        #region ScenarioElement Members
        #region ProcessIdentifier

        /// <summary>
        /// Counts, changes, or deletes all occurrences of the specified identifier in the <see
        /// cref="AreaSection"/>.</summary>
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
        /// The number of occurrences of <paramref name="oldId"/> in the <see cref="AreaSection"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="oldId"/> is a null reference.</exception>
        /// <remarks><para>
        /// <b>ProcessIdentifier</b> processes the <see cref="AreaSection"/> properties as follows:
        /// </para><list type="table"><listheader>
        /// <term>Property</term><description>Processing</description>
        /// </listheader><item>
        /// <term><see cref="Collection"/><br/> <see cref="Terrains"/></term>
        /// <description>By value</description>
        /// </item></list></remarks>

        internal override int ProcessIdentifier(string oldId, string newId) {
            int count = 0;

            // process IDs in terrain list
            foreach (EntityTemplate entity in Terrains)
                count += entity.ProcessIdentifier(oldId, newId);

            // process IDs in area list
            foreach (Area area in Collection)
                count += area.ProcessIdentifier(oldId, newId);

            return count;
        }

        #endregion
        #region Validate

        /// <summary>
        /// Validates the current data of the <see cref="AreaSection"/>.</summary>
        /// <exception cref="XmlException">
        /// Validation failed, and <see cref="ApplicationInfo.IsEditor"/> is <c>false</c>.
        /// </exception>
        /// <remarks><para>
        /// <b>Validate</b> processes <see cref="AreaSection"/> properties as follows:
        /// </para><list type="table"><listheader>
        /// <term>Property</term><description>Processing</description>
        /// </listheader><item>
        /// <term><see cref="MapGrid"/></term>
        /// <description>Check that width and height are less than <see cref="Int16.MaxValue"/>
        /// </description></item><item>
        /// <term><see cref="AllPlaceSites"/><br/> <see cref="HomeSites"/></term>
        /// <description>Check coordinates against size of <see cref="MapGrid"/></description>
        /// </item><item>
        /// <term><see cref="Terrains"/></term>
        /// <description>Check identifiers and presence of a first element whose underlying <see
        /// cref="TerrainClass.IsBackground"/> flag is <c>true</c>.</description>
        /// </item><item>
        /// <term><see cref="Collection"/></term>
        /// <description>Invoke <see cref="Area.Validate"/> on all elements</description>
        /// </item></list><para>
        /// In addition, <b>Validate</b> checks all map locations stored with <see
        /// cref="FactionClass"/> or <see cref="EntityClass"/> objects against the size of the
        /// current <b>MapGrid</b>.
        /// </para><para>
        /// Checks are only performed if <see cref="ApplicationInfo.IsEditor"/> is <c>false</c>.
        /// </para></remarks>

        internal override void Validate() {

            // set references for IDs in area list
            foreach (Area area in Collection)
                area.Validate();

            // skip remaining checks if editing
            if (ApplicationInfo.IsEditor) return;

            // check maximum map grid size
            if (MapGrid.Size.Width >= Int16.MaxValue || MapGrid.Size.Height >= Int16.MaxValue)
                ThrowHelper.ThrowXmlException(Global.Strings.XmlMapGridSize);

            // validate faction home sites
            FactionSection factions = MasterSection.Instance.Factions;
            foreach (var pair in HomeSites) {

                // check for indicated faction
                if (!factions.Collection.ContainsKey(pair.Key))
                    continue;

                // validate home site
                if (!MapGrid.Contains(pair.Value))
                    ThrowHelper.ThrowXmlExceptionWithFormat(Global.Strings.XmlHomeInvalid, pair.Key);
            }

            // validate entity placement sites
            EntitySection entities = MasterSection.Instance.Entities;
            foreach (var pair in AllPlaceSites) {

                // retrieve indicated entity class
                EntityClass entityClass = entities.GetEntity(pair.Key);
                if (entityClass == null) continue;

                // validate placement sites
                foreach (PointI site in pair.Value)
                    if (!MapGrid.Contains(site))
                        ThrowHelper.ThrowXmlExceptionWithFormat(
                            Global.Strings.XmlPlaceInvalid, pair.Key);
            }

            // validate and optimize terrain templates
            entities.ValidateTemplates(this._terrains);

            /*
             * If the terrains tag specified any background terrain,
             * validation will have placed it at the start of Terrains.
             */

            bool isBackground = false;

            if (Terrains.Count > 0) {
                string id = Terrains[0].EntityClass;
                TerrainClass terrainClass = (TerrainClass) entities.Terrains[id];
                isBackground = terrainClass.IsBackground;
            }

            if (!isBackground)
                ThrowHelper.ThrowXmlException(Global.Strings.XmlTerrainDefault);
        }

        #endregion
        #endregion
        #region XmlSerializable Members
        #region ConstXmlName

        /// <summary>
        /// The name of the XML element associated with the <see cref="AreaSection"/> class.
        /// </summary>
        /// <remarks>
        /// <b>ConstXmlName</b> holds the value "areas", indicating the XML element in <see
        /// cref="FilePaths.ScenarioSchema"/> whose data is managed by the <see
        /// cref="AreaSection"/> class.</remarks>

        public const string ConstXmlName = "areas";

        #endregion
        #region ReadXmlElements

        /// <summary>
        /// Reads XML element data into the <see cref="AreaSection"/> object using the specified
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
            switch (reader.Name) {

                case "mapGrid":
                    ReadXmlMapGrid(reader);
                    return true;

                case "overlay":
                    Overlay.ReadXml(reader);
                    return true;

                case "factionSites":
                    ReadXmlFactionSites(reader);
                    return true;

                case "entitySites":
                    ReadXmlEntitySites(reader);
                    return true;

                case TerrainClass.ConstXmlName: {
                    EntityTemplate entity = new EntityTemplate(EntityCategory.Terrain);
                    entity.ReadXml(reader);
                    this._terrains.Add(entity);
                    return true;
                }

                case Area.ConstXmlName: {
                    Area area = new Area();
                    area.ReadXml(reader);
                    this._collection.Add(area);
                    return true;
                }

                default: return false;
            }
        }

        #endregion
        #region ReadXmlEntitySites

        /// <summary>
        /// Reads XML data into the <see cref="AllPlaceSites"/> collection using the specified <see
        /// cref="XmlReader"/>.</summary>
        /// <param name="reader">
        /// The <see cref="XmlReader"/> from which to read.</param>
        /// <exception cref="XmlException">
        /// An error occurred while parsing the XML data provided by <paramref name="reader"/>.
        /// </exception>
        /// <exception cref="System.Xml.Schema.XmlSchemaException">
        /// <paramref name="reader"/> is an <see cref="XmlValidatingReader"/>, and the XML data did
        /// not conform to <see cref="FilePaths.ScenarioSchema"/>.</exception>
        /// <remarks>
        /// The specified <paramref name="reader"/> must be positioned on an XML element named
        /// "entitySites".</remarks>

        private void ReadXmlEntitySites(XmlReader reader) {
            XmlUtility.MoveToStartElement(reader, "entitySites");

            string id = null;
            XmlUtility.ReadAttributeAsString(reader, "id", ref id);
            if (id == null || reader.IsEmptyElement)
                return;

            // get place sites for this entity class
            PointIList placeSites;
            if (!AllPlaceSites.TryGetValue(id, out placeSites)) {

                // create new collection if not present
                placeSites = new PointIList();
                AllPlaceSites[id] = placeSites;
            }

            while (reader.Read() && reader.IsStartElement()) {
                switch (reader.Name) {

                    case "placeSite":
                        placeSites.Add(SimpleXml.ReadPointI(reader));
                        break;

                    default:
                        // skip to end tag of unknown element
                        XmlUtility.MoveToEndElement(reader);
                        break;
                }
            }
        }

        #endregion
        #region ReadXmlFactionSites

        /// <summary>
        /// Reads XML data into the <see cref="HomeSites"/> collection using the specified <see
        /// cref="XmlReader"/>.</summary>
        /// <param name="reader">
        /// The <see cref="XmlReader"/> from which to read.</param>
        /// <exception cref="XmlException">
        /// An error occurred while parsing the XML data provided by <paramref name="reader"/>.
        /// </exception>
        /// <exception cref="System.Xml.Schema.XmlSchemaException">
        /// <paramref name="reader"/> is an <see cref="XmlValidatingReader"/>, and the XML data did
        /// not conform to <see cref="FilePaths.ScenarioSchema"/>.</exception>
        /// <remarks>
        /// The specified <paramref name="reader"/> must be positioned on an XML element named
        /// "factionSites".</remarks>

        private void ReadXmlFactionSites(XmlReader reader) {
            XmlUtility.MoveToStartElement(reader, "factionSites");

            string id = null;
            XmlUtility.ReadAttributeAsString(reader, "id", ref id);
            if (id == null || reader.IsEmptyElement)
                return;

            while (reader.Read() && reader.IsStartElement()) {
                switch (reader.Name) {

                    case "homeSite":
                        HomeSites[id] = SimpleXml.ReadPointI(reader);
                        break;

                    default:
                        // skip to end tag of unknown element
                        XmlUtility.MoveToEndElement(reader);
                        break;
                }
            }
        }

        #endregion
        #region ReadXmlMapGrid

        /// <summary>
        /// Reads XML data into the <see cref="MapGrid"/> using the specified <see
        /// cref="XmlReader"/>.</summary>
        /// <param name="reader">
        /// The <see cref="XmlReader"/> from which to read.</param>
        /// <exception cref="XmlException">
        /// An error occurred while parsing the XML data provided by <paramref name="reader"/>.
        /// </exception>
        /// <exception cref="System.Xml.Schema.XmlSchemaException">
        /// <paramref name="reader"/> is an <see cref="XmlValidatingReader"/>, and the XML data did
        /// not conform to <see cref="FilePaths.ScenarioSchema"/>.</exception>
        /// <remarks>
        /// The specified <paramref name="reader"/> must be positioned on an XML element named
        /// "mapGrid".</remarks>

        private void ReadXmlMapGrid(XmlReader reader) {
            XmlUtility.MoveToStartElement(reader, "mapGrid");

            SizeI size = SimpleXml.ReadSizeI(reader);
            PolygonGridShift gridShift = this._mapGrid.GridShift;
            XmlUtility.ReadAttributeAsEnum(reader, "gridShift", ref gridShift);

            /*
             * Size and GridShift are set after Element because the legal
             * GridShift values depend on the Element shape, and setting
             * Size before Element needlessly duplicates calculations.
             */

            if (!reader.IsEmptyElement)
                while (reader.Read() && reader.IsStartElement()) {
                    switch (reader.Name) {

                        case "polygon":
                            this._mapGrid.Element = ReadXmlPolygon(reader);
                            break;

                        default:
                            // skip to end tag of unknown element
                            XmlUtility.MoveToEndElement(reader);
                            break;
                    }
                }

            this._mapGrid.Size = size;
            this._mapGrid.GridShift = gridShift;
        }

        #endregion
        #region ReadXmlPolygon

        /// <summary>
        /// Reads XML data into an instance of <see cref="RegularPolygon"/> using the specified <see
        /// cref="XmlReader"/>.</summary>
        /// <param name="reader">
        /// The <see cref="XmlReader"/> from which to read.</param>
        /// <returns>
        /// A <see cref="RegularPolygon"/> instance created from the XML data provided by <paramref
        /// name="reader"/>.</returns>
        /// <exception cref="XmlException">
        /// An error occurred while parsing the XML data provided by <paramref name="reader"/>.
        /// </exception>
        /// <exception cref="System.Xml.Schema.XmlSchemaException">
        /// <paramref name="reader"/> is an <see cref="XmlValidatingReader"/>, and the XML data did
        /// not conform to <see cref="FilePaths.ScenarioSchema"/>.</exception>
        /// <remarks>
        /// <b>ReadXmlPolygon</b> attempts to read a "polygon" XML element defined by <see
        /// cref="FilePaths.ScenarioSchema"/> from the specified <paramref name="reader"/>, and
        /// creates a <see cref="RegularPolygon"/> instance from the provided XML data. Any
        /// unspecified attributes default to the corresponding property values of <see
        /// cref="DefaultPolygon"/>.</remarks>

        private static RegularPolygon ReadXmlPolygon(XmlReader reader) {
            XmlUtility.MoveToStartElement(reader, "polygon");

            // load all parameters with default values
            double length = DefaultPolygon.Length;
            int sides = DefaultPolygon.Sides;
            PolygonOrientation orientation = DefaultPolygon.Orientation;
            bool vertexNeighbors = DefaultPolygon.VertexNeighbors;

            // read any specified polygon parameters
            XmlUtility.ReadAttributeAsDouble(reader, "length", ref length);
            XmlUtility.ReadAttributeAsInt32(reader, "sides", ref sides);
            XmlUtility.ReadAttributeAsEnum(reader, "orientation", ref orientation);
            XmlUtility.ReadAttributeAsBoolean(reader, "vertexNeighbors", ref vertexNeighbors);

            return new RegularPolygon(length, sides, orientation, vertexNeighbors);
        }

        #endregion
        #region WriteXmlElements

        /// <summary>
        /// Writes all current data of the <see cref="AreaSection"/> object that is serialized to
        /// nested XML elements to the specified <see cref="XmlWriter"/>.</summary>
        /// <param name="writer">
        /// The <see cref="XmlWriter"/> to which to write.</param>
        /// <remarks>
        /// Please refer to class <see cref="XmlSerializable"/> for a complete description of this
        /// method.</remarks>

        protected override void WriteXmlElements(XmlWriter writer) {

            WriteXmlMapGrid(writer);
            if (!Overlay.IsEmpty)
                Overlay.WriteXml(writer);

            WriteXmlFactionSites(writer);
            WriteXmlEntitySites(writer);

            foreach (EntityTemplate entity in Terrains)
                entity.WriteXml(writer);

            foreach (Area area in Collection)
                area.WriteXml(writer);
        }

        #endregion
        #region WriteXmlEntitySites

        /// <summary>
        /// Writes all collections in <see cref="AllPlaceSites"/> to the specified <see
        /// cref="XmlWriter"/>.</summary>
        /// <param name="writer">
        /// The <see cref="XmlWriter"/> to which to write.</param>

        private void WriteXmlEntitySites(XmlWriter writer) {

            foreach (var pair in AllPlaceSites) {
                if (pair.Value.Count == 0) continue;

                writer.WriteStartElement("entitySites");
                writer.WriteAttributeString("id", pair.Key);

                foreach (PointI target in pair.Value) {
                    writer.WriteStartElement("placeSite");
                    SimpleXml.WritePointI(writer, target);
                    writer.WriteEndElement();
                }

                writer.WriteEndElement();
            }
        }

        #endregion
        #region WriteXmlFactionSites

        /// <summary>
        /// Writes all <see cref="HomeSites"/> to the specified <see cref="XmlWriter"/>.</summary>
        /// <param name="writer">
        /// The <see cref="XmlWriter"/> to which to write.</param>

        private void WriteXmlFactionSites(XmlWriter writer) {
            foreach (var pair in HomeSites) {

                // skip invalid locations
                if (pair.Value.X < 0 || pair.Value.Y < 0)
                    continue;

                writer.WriteStartElement("factionSites");
                writer.WriteAttributeString("id", pair.Key);

                writer.WriteStartElement("homeSite");
                SimpleXml.WritePointI(writer, pair.Value);
                writer.WriteEndElement();

                writer.WriteEndElement();
            }
        }

        #endregion
        #region WriteXmlMapGrid

        /// <summary>
        /// Writes all <see cref="MapGrid"/> data to the specified <see cref="XmlWriter"/>.
        /// </summary>
        /// <param name="writer">
        /// The <see cref="XmlWriter"/> to which to write.</param>

        private void WriteXmlMapGrid(XmlWriter writer) {
            writer.WriteStartElement("mapGrid");

            SimpleXml.WriteSizeI(writer, MapGrid.Size);
            writer.WriteAttributeString("gridShift", MapGrid.GridShift.ToString());

            WriteXmlPolygon(writer, MapGrid.Element);
            writer.WriteEndElement();
        }

        #endregion
        #region WriteXmlPolygon

        /// <summary>
        /// Writes the data of the specified <see cref="RegularPolygon"/> instance to the specified
        /// <see cref="XmlWriter"/>.</summary>
        /// <param name="writer">
        /// The <see cref="XmlWriter"/> to which to write.</param>
        /// <param name="polygon">
        /// The <see cref="RegularPolygon"/> instance whose data to write to <paramref
        /// name="writer"/>.</param>
        /// <remarks>
        /// <b>WriteXmlPolygon</b> writes the data of the specified <paramref name="polygon"/> to
        /// the specified <paramref name="writer"/>, according to the "polygon" XML element defined
        /// by <see cref="FilePaths.ScenarioSchema"/>.</remarks>

        private static void WriteXmlPolygon(XmlWriter writer, RegularPolygon polygon) {
            writer.WriteStartElement("polygon");

            writer.WriteAttributeString("length", XmlConvert.ToString(polygon.Length));
            writer.WriteAttributeString("sides", XmlConvert.ToString(polygon.Sides));
            writer.WriteAttributeString("orientation", polygon.Orientation.ToString());

            if (polygon.VertexNeighbors)
                writer.WriteAttributeString("vertexNeighbors",
                    XmlConvert.ToString(polygon.VertexNeighbors));

            writer.WriteEndElement();
        }

        #endregion
        #endregion
    }
}
